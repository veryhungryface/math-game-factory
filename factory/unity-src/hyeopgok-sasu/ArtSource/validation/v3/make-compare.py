#!/usr/bin/env python3
"""Build a fixed reference/current contact sheet for the v3 art review."""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[5]
OUT = HERE / "compare.png"
FINAL = HERE / "final"

SOURCES = [
    (ROOT / "scratchpad/kingshot-ref/img/as_12.jpg", "REFERENCE / portrait"),
    (ROOT / "scratchpad/kingshot-ref/img/gp_1.jpg", "REFERENCE / battle"),
    (ROOT / "scratchpad/kingshot-ref/frames/Vioib7IWvqc/f024.jpg", "REFERENCE / pads"),
    (FINAL / "play-390-8s.png", "V3 / mobile 8 s"),
    (FINAL / "play-1280-8s.png", "V3 / desktop 8 s"),
    (FINAL / "upgrade-pour-1280.png", "V3 / tower upgrade"),
]

for source, _ in SOURCES:
    if not source.is_file():
        raise SystemExit(f"missing compare source: {source}")

canvas_width = 1800
cell_width = 560
cell_height = 720
gap = 30
top = 90
canvas_height = top + cell_height * 2 + gap * 3
canvas = Image.new("RGB", (canvas_width, canvas_height), "#14222b")
draw = ImageDraw.Draw(canvas)

font_candidates = [
    Path("/System/Library/Fonts/Supplemental/Arial Bold.ttf"),
    Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"),
]
font_file = next((candidate for candidate in font_candidates if candidate.is_file()), None)
title_font = ImageFont.truetype(str(font_file), 34) if font_file else ImageFont.load_default()
label_font = ImageFont.truetype(str(font_file), 22) if font_file else ImageFont.load_default()
draw.text((gap, 24), "KINGSHOT REFERENCES  /  HYEOPGOK SASU V3", fill="#f8e9b6", font=title_font)

for index, (source, label) in enumerate(SOURCES):
    row, column = divmod(index, 3)
    x = gap + column * (cell_width + gap)
    y = top + row * (cell_height + gap)
    draw.rounded_rectangle((x, y, x + cell_width, y + cell_height), radius=18, fill="#22333e", outline="#d2b169", width=3)
    image = Image.open(source).convert("RGB")
    image_box = (cell_width - 24, cell_height - 76)
    fitted = ImageOps.contain(image, image_box, Image.Resampling.LANCZOS)
    image_x = x + (cell_width - fitted.width) // 2
    image_y = y + 12 + (image_box[1] - fitted.height) // 2
    canvas.paste(fitted, (image_x, image_y))
    draw.text((x + 16, y + cell_height - 48), label, fill="#ffffff", font=label_font)

canvas.save(OUT, optimize=True)
print(OUT)
