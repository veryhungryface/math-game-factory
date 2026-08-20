#!/usr/bin/env python3
"""Magenta chroma-key + despill + tight crop. Reusable factory asset tool.

Color-distance key (default #FF00FF):
    dist = sqrt((r-255)^2 + g^2 + (b-255)^2)
    dist <= hard  → alpha 0
    dist >= soft  → alpha 1
    otherwise     → linear ramp

Semi-transparent edges are unmixed against the key color, then any leftover
magenta axis is subtracted so pink fringes do not survive. Opaque interior
pixels are left alone.

Usage:
  python3 factory/lib/chroma-cut.py IN.png OUT.png
  python3 factory/lib/chroma-cut.py IN.png OUT.png --max-bytes 1000000
  python3 factory/lib/chroma-cut.py IN.png OUT.png --optimize-only --max-bytes 1200000
"""
from __future__ import annotations

import argparse
import os
import sys

import numpy as np
from PIL import Image

KEY_DEFAULT = (255, 0, 255)
HARD_DEFAULT = 40.0
SOFT_DEFAULT = 240.0
PAD_DEFAULT = 8
CROP_ALPHA = 12  # bbox uses pixels at least this opaque
INWARD_PASSES = 10
OPAQUE_T = 0.99
SHIFTS_8 = ((-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (-1, 1), (1, -1), (1, 1))
SHIFTS_4 = ((-1, 0), (1, 0), (0, -1), (0, 1))


def parse_key(s: str) -> tuple[int, int, int]:
    s = s.strip().lstrip("#")
    if len(s) != 6:
        raise argparse.ArgumentTypeError("key must be RRGGBB")
    return int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16)


def _shift(a: np.ndarray, dy: int, dx: int) -> np.ndarray:
    """Shift without wrap-around (edges filled with 0)."""
    out = np.zeros_like(a)
    ys = slice(max(0, dy), a.shape[0] + min(0, dy))
    yt = slice(max(0, -dy), a.shape[0] - max(0, dy))
    xs = slice(max(0, dx), a.shape[1] + min(0, dx))
    xt = slice(max(0, -dx), a.shape[1] - max(0, dx))
    out[yt, xt] = a[ys, xs]
    return out


def _erode(mask: np.ndarray, n: int = 1) -> np.ndarray:
    """Binary erode: a pixel stays on only if all 4-neighbors are on."""
    out = mask.copy()
    for _ in range(n):
        m = out
        out = m.copy()
        for dy, dx in SHIFTS_4:
            out &= _shift(m, dy, dx)
    return out


def inward_color_fill(
    r: np.ndarray, g: np.ndarray, b: np.ndarray, alpha: np.ndarray
) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    """Paint silhouette RGB from the eroded interior.

    Fully-opaque fringe pixels can still carry magenta; seeding from the
    morphological interior (1px inside the opaque mask) stops that.
    Transparent pixels stay empty.
    """
    rr, gg, bb = r.copy(), g.copy(), b.copy()
    interior = _erode(alpha >= OPAQUE_T, n=1)
    live = alpha > 0
    if not np.any(interior) or not np.any(live & ~interior):
        return rr, gg, bb

    filled = interior.copy()
    for _ in range(INWARD_PASSES):
        acc_r = np.zeros_like(rr)
        acc_g = np.zeros_like(gg)
        acc_b = np.zeros_like(bb)
        acc_n = np.zeros_like(rr)
        for dy, dx in SHIFTS_8:
            src = filled
            acc_r += _shift(rr, dy, dx) * _shift(src, dy, dx)
            acc_g += _shift(gg, dy, dx) * _shift(src, dy, dx)
            acc_b += _shift(bb, dy, dx) * _shift(src, dy, dx)
            acc_n += _shift(src.astype(np.float32), dy, dx)
        has = (acc_n > 0) & live & (~filled)
        if not np.any(has):
            break
        n = np.maximum(acc_n, 1.0)
        rr = np.where(has, acc_r / n, rr)
        gg = np.where(has, acc_g / n, gg)
        bb = np.where(has, acc_b / n, bb)
        filled = filled | has
    return rr, gg, bb


def kill_magenta_silhouette(
    r: np.ndarray, g: np.ndarray, b: np.ndarray, alpha: np.ndarray
) -> tuple[np.ndarray, np.ndarray, np.ndarray, np.ndarray]:
    """Drop leftover magenta on pixels that touch transparency."""
    trans = alpha < 0.08
    border = np.zeros_like(alpha, dtype=bool)
    for dy, dx in SHIFTS_8:
        border |= _shift(trans, dy, dx)
    border &= alpha > 0

    mag_amt = np.maximum(0.0, np.minimum(r, b) - g)
    r = np.where(border, r - mag_amt, r)
    b = np.where(border, b - mag_amt, b)

    still = (
        border
        & (np.minimum(r, b) > g + 22)
        & (np.abs(r - b) < 60)
        & (np.minimum(r, b) > 55)
    )
    alpha = np.where(still, 0.0, alpha)
    r = np.where(still, 0.0, r)
    g = np.where(still, 0.0, g)
    b = np.where(still, 0.0, b)
    return r, g, b, alpha


def chroma_cut(
    im: Image.Image,
    key: tuple[int, int, int] = KEY_DEFAULT,
    hard: float = HARD_DEFAULT,
    soft: float = SOFT_DEFAULT,
    pad: int = PAD_DEFAULT,
    crop: bool = True,
) -> Image.Image:
    if soft <= hard:
        raise ValueError("soft threshold must be > hard threshold")

    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    kr, kg, kb = (float(key[0]), float(key[1]), float(key[2]))

    dist = np.sqrt((r - kr) ** 2 + (g - kg) ** 2 + (b - kb) ** 2)

    alpha = np.zeros(dist.shape, dtype=np.float32)
    alpha[dist >= soft] = 1.0
    ramp = (dist > hard) & (dist < soft)
    alpha[ramp] = (dist[ramp] - hard) / (soft - hard)

    # Recover foreground assuming obs = fg * a + key * (1-a).
    a_safe = np.maximum(alpha, 1e-6)
    fr = (r - kr * (1.0 - alpha)) / a_safe
    fg = (g - kg * (1.0 - alpha)) / a_safe
    fb = (b - kb * (1.0 - alpha)) / a_safe

    # Extra magenta-axis despill on semi-transparent pixels only.
    edge = (alpha > 0) & (alpha < 0.995)
    mag = np.maximum(0.0, np.minimum(fr, fb) - fg)
    extra = mag * (0.55 + 0.45 * (1.0 - alpha))
    fr = np.where(edge, fr - extra, fr)
    fb = np.where(edge, fb - extra, fb)

    fr, fg, fb = inward_color_fill(fr, fg, fb, alpha)
    fr, fg, fb, alpha = kill_magenta_silhouette(fr, fg, fb, alpha)

    # Residual magenta-axis despill on any remaining semi-transparent pixel.
    edge = (alpha > 0) & (alpha < 0.995)
    mag2 = np.maximum(0.0, np.minimum(fr, fb) - fg)
    fr = np.where(edge, fr - mag2, fr)
    fb = np.where(edge, fb - mag2, fb)

    fr = np.clip(fr, 0, 255)
    fg = np.clip(fg, 0, 255)
    fb = np.clip(fb, 0, 255)
    fr = np.where(alpha <= 0, 0, fr)
    fg = np.where(alpha <= 0, 0, fg)
    fb = np.where(alpha <= 0, 0, fb)

    out = np.dstack([fr, fg, fb, np.clip(alpha * 255.0, 0, 255)]).astype(np.uint8)
    result = Image.fromarray(out)  # HxWx4 uint8 → RGBA

    if crop:
        result = tight_crop(result, pad=pad)
    return result


def tight_crop(im: Image.Image, pad: int = PAD_DEFAULT, min_alpha: int = CROP_ALPHA) -> Image.Image:
    """Crop to opaque bbox, then guarantee `pad` px of transparent margin."""
    arr = np.asarray(im)
    if arr.ndim != 3 or arr.shape[2] < 4:
        return im
    ys, xs = np.where(arr[:, :, 3] >= min_alpha)
    if ys.size == 0:
        return im
    y0, y1 = int(ys.min()), int(ys.max()) + 1
    x0, x1 = int(xs.min()), int(xs.max()) + 1
    cropped = im.crop((x0, y0, x1, y1))
    if pad <= 0:
        return cropped
    w, h = cropped.size
    canvas = Image.new("RGBA", (w + pad * 2, h + pad * 2), (0, 0, 0, 0))
    canvas.paste(cropped, (pad, pad))
    return canvas


def _quantize_keep_mode(im: Image.Image) -> Image.Image:
    if im.mode == "RGBA":
        # Palette PNG cannot hold a true alpha channel portably; skip.
        return im
    q = im.convert("RGB").quantize(
        colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.FLOYDSTEINBERG
    )
    return q


def save_optimized(
    im: Image.Image,
    path: str,
    max_bytes: int | None = None,
    allow_resize: bool = True,
    allow_quantize: bool = True,
    min_edge: int = 320,
) -> int:
    """Write PNG, shrinking / quantizing until under max_bytes if requested."""
    os.makedirs(os.path.dirname(os.path.abspath(path)) or ".", exist_ok=True)

    def dump(image: Image.Image, dest: str) -> int:
        image.save(dest, format="PNG", optimize=True, compress_level=9)
        return os.path.getsize(dest)

    size = dump(im, path)
    if max_bytes is None or size <= max_bytes:
        return size

    # RGB illustrations compress well as 256-color PNG.
    if allow_quantize and im.mode in ("RGB", "L", "P"):
        q = _quantize_keep_mode(im)
        qsize = dump(q, path)
        if qsize <= max_bytes:
            return qsize
        if qsize < size:
            size = qsize
            im = q.convert("RGB")
        else:
            dump(im, path)

    if not allow_resize:
        return os.path.getsize(path)

    work = im
    scale = 0.92
    while scale >= (min_edge / max(work.size)) and os.path.getsize(path) > max_bytes:
        nw = max(min_edge, int(im.width * scale))
        nh = max(min_edge, int(im.height * scale))
        if nw == work.width and nh == work.height:
            break
        resample = Image.Resampling.LANCZOS
        work = im.resize((nw, nh), resample)
        if allow_quantize and work.mode in ("RGB", "L", "P"):
            cand = _quantize_keep_mode(work)
            dump(cand, path)
            if os.path.getsize(path) <= max_bytes:
                return os.path.getsize(path)
            dump(work, path)
        else:
            dump(work, path)
        scale -= 0.08

    return os.path.getsize(path)


def fringe_stats(im: Image.Image) -> dict:
    """Count leftover magenta-ish edge pixels (debug / QA)."""
    arr = np.asarray(im.convert("RGBA"))
    r, g, b, a = (arr[:, :, 0].astype(np.int16), arr[:, :, 1].astype(np.int16),
                  arr[:, :, 2].astype(np.int16), arr[:, :, 3])
    edge = (a > 16) & (a < 250)
    mag = (r > g + 40) & (b > g + 40) & (np.minimum(r, b) > 80)
    n_edge_mag = int((edge & mag).sum())
    n_any_mag = int(((a > 16) & mag).sum())
    return {
        "size": im.size,
        "opaque": int((a >= 12).sum()),
        "edge_magenta": n_edge_mag,
        "any_magenta": n_any_mag,
    }


def compose_preview(im: Image.Image, bg: tuple[int, int, int] = (18, 12, 16)) -> Image.Image:
    canvas = Image.new("RGBA", im.size, bg + (255,))
    return Image.alpha_composite(canvas, im.convert("RGBA")).convert("RGB")


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(description="Magenta chroma-key, despill, crop, optimize PNG")
    p.add_argument("src")
    p.add_argument("dst")
    p.add_argument("--key", type=parse_key, default=KEY_DEFAULT, help="chroma key hex, default FF00FF")
    p.add_argument("--hard", type=float, default=HARD_DEFAULT, help="dist <= hard → fully transparent")
    p.add_argument("--soft", type=float, default=SOFT_DEFAULT, help="dist >= soft → fully opaque")
    p.add_argument("--pad", type=int, default=PAD_DEFAULT, help="transparent padding around bbox")
    p.add_argument("--max-bytes", type=int, default=None)
    p.add_argument("--optimize-only", action="store_true", help="skip chroma; just size-fit")
    p.add_argument("--no-crop", action="store_true")
    p.add_argument("--preview", default=None, help="optional dark-bg composite path")
    args = p.parse_args(argv)

    if not os.path.isfile(args.src):
        print(f"missing input: {args.src}", file=sys.stderr)
        return 2

    src = Image.open(args.src)
    if args.optimize_only:
        out = src.convert("RGB") if src.mode not in ("RGB", "RGBA", "P") else src
        if out.mode == "P":
            out = out.convert("RGB")
    else:
        out = chroma_cut(
            src,
            key=args.key,
            hard=args.hard,
            soft=args.soft,
            pad=args.pad,
            crop=not args.no_crop,
        )

    size = save_optimized(out, args.dst, max_bytes=args.max_bytes)
    stats = fringe_stats(Image.open(args.dst)) if out.mode == "RGBA" else {"size": out.size}
    print(f"wrote {args.dst}  {size} bytes  {stats}")

    if args.preview:
        prev = compose_preview(Image.open(args.dst) if out.mode == "RGBA" else out.convert("RGBA"))
        prev.save(args.preview, format="PNG", optimize=True)
        print(f"preview {args.preview}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
