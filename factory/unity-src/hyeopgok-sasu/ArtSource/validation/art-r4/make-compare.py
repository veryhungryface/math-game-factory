"""Build the R4 six-panel evidence sheet by rescale/letterbox only.

Usage from repository root:
  python3 factory/unity-src/hyeopgok-sasu/ArtSource/validation/art-r4/make-compare.py [capture-tag]
"""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import hashlib
import json
import sys

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[5]
TAG = sys.argv[1] if len(sys.argv) > 1 else "final"
if not TAG.replace("-", "").isalnum():
    raise SystemExit("invalid capture tag")

FONT_PATH = "/System/Library/Fonts/AppleSDGothicNeo.ttc"
title_font = ImageFont.truetype(FONT_PATH, 29)

rows = [
    ("scratchpad/kingshot-ref/img/as_12.jpg", f"{TAG}/play-390-8s.png", 940, "초반 전투 · 390×844"),
    ("scratchpad/kingshot-ref/img/gp_1.jpg", f"{TAG}/play-390-15s.png", 940, "전투 15초 · 390×844"),
    ("scratchpad/kingshot-ref/frames/Vioib7IWvqc/f024.jpg", f"{TAG}/play-1280-8s.png", 760, "전투 8초 · 1280×800"),
]

for reference, ours, _, _ in rows:
    for file in (ROOT / reference, HERE / ours):
        if not file.is_file():
            raise SystemExit(f"missing comparison source: {file}")

sheet = Image.new("RGB", (2160, sum(row[2] for row in rows) + 76), "#142e36")
draw = ImageDraw.Draw(sheet)
draw.text((28, 20), "아트 라운드 4 · 킹샷 참고 원본과 실제 WebGL 화면", font=title_font, fill="#fff1cf")

y = 76
sources = []
for reference, ours, row_height, label in rows:
    for column, file in enumerate((ROOT / reference, HERE / ours)):
        with Image.open(file) as source:
            image = source.convert("RGB")
            original = list(image.size)
            image.thumbnail((1044, row_height - 86), Image.Resampling.LANCZOS)
            x = column * 1080 + 18 + (1044 - image.width) // 2
            image_y = y + 66 + (row_height - 86 - image.height) // 2
            sheet.paste(image, (x, image_y))
        draw.text(
            (column * 1080 + 24, y + 16),
            "킹샷 · 참고" if column == 0 else f"협곡 사수 · {label}",
            font=title_font,
            fill="#fff1cf",
        )
        sources.append(
            {
                "path": str(file.relative_to(ROOT)),
                "original": original,
                "sha256": hashlib.sha256(file.read_bytes()).hexdigest(),
                "operation": "aspect-preserving rescale and letterbox only",
            }
        )
    y += row_height

output = HERE / "compare.png"
sheet.save(output, optimize=True)
(HERE / "compare-sources.json").write_text(json.dumps(sources, ensure_ascii=False, indent=2) + "\n")
print(output)

