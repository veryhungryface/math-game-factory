#!/usr/bin/env python3
"""Compose screenshot-only covers and labelled visual comparisons.

No gameplay content is repainted or generated. Wide cover keeps the native
viewport; square cover crops the same real desktop battlefield and uniformly
rescales it below a game-palette title bar.
"""
from pathlib import Path
import argparse
import hashlib
import json
import shutil
from PIL import Image, ImageDraw, ImageFont, ImageOps

HERE = Path(__file__).resolve().parent
GAME = HERE.parents[2]
ROOT = GAME.parents[2]
FONT = ROOT / 'factory/unity/kit/MgfKit/Resources/MgfKit/Fonts/MgfKR-Bold.otf'
NAVY = '#142e35'
CREAM = '#fff1ce'
GOLD = '#ffd05a'


def font(size):
    return ImageFont.truetype(str(FONT), size)


def label(draw, position, text, size=26, fill=CREAM):
    draw.text(position, text, font=font(size), fill=fill, anchor='lt')


def fit_panel(src, dimensions, background=NAVY):
    frame = Image.new('RGB', dimensions, background)
    scaled = ImageOps.contain(src, dimensions, Image.Resampling.LANCZOS)
    frame.paste(scaled, ((dimensions[0]-scaled.width)//2, (dimensions[1]-scaled.height)//2))
    return frame


def cover(src_path, destination, dimensions, crop=None):
    source = Image.open(src_path).convert('RGB')
    if crop:
        im = Image.new('RGB',dimensions,NAVY)
        im.paste(fit_panel(source.crop(crop),(dimensions[0],dimensions[1]-78)),(0,78))
    else:
        assert source.size == dimensions, f'Capture must be native-size: {source.size} != {dimensions}'
        im = source
    draw = ImageDraw.Draw(im)
    height = 76 if dimensions[1] == 630 else 78
    draw.rectangle((0, 0, dimensions[0], height), fill=NAVY)
    draw.rectangle((0, height-4, dimensions[0], height), fill=GOLD)
    label(draw, (28, 6), '협곡 사수', 57)
    label(draw, (dimensions[0]-338, 26), '왕을 끌어 협곡을 지켜라', 25, GOLD)
    im.save(destination, optimize=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--phase', default='final')
    parser.add_argument('--impact', default='impact-mid')
    parser.add_argument('--comparisons-only', action='store_true')
    parser.add_argument('--install-public', action='store_true', help='Copy completed covers to this game only')
    args = parser.parse_args()
    capture_dir = HERE / args.phase
    out = HERE / 'artifacts'
    out.mkdir(exist_ok=True)
    sources = {}
    cover_specs = [] if args.comparisons_only else [('thumb', 'cover-wide', (1200,630),None), ('square', '1280', (1080,1080),(425,58,1205,799))]
    for name, label_name, dimensions,crop in cover_specs:
        src = capture_dir / f'{args.impact}-{label_name}.png'
        cover(src, out / f'{name}.png', dimensions,crop)
        sources[name] = str(src.relative_to(ROOT))

    # Comparable battlefield crops remove social-video chrome and game HUD only.
    ref_dir = GAME / 'ArtSource/ref'
    refs = []
    for i in (1,2):
        ref = Image.open(ref_dir / f'ref-{i}.png').convert('RGB')
        refs.append(ref.crop((0,round(ref.height*.174),ref.width,round(ref.height*.739))))
    actual = Image.open(capture_dir / f'{args.impact}-390.png').convert('RGB').crop((0,198,390,735))
    panel = (600,825)
    comp = Image.new('RGB',(1832,907),NAVY)
    draw = ImageDraw.Draw(comp)
    for i, (im, heading) in enumerate(zip(refs+[actual], ['레퍼런스 1 · 전장 부분','레퍼런스 2 · 전장 부분','협곡 사수 · 실제 게임 렌더'])):
        x = 8+i*608
        label(draw,(x+12,16),heading,25)
        comp.paste(fit_panel(im,panel),(x,62))
    comp.save(out/'reference-comparison.png',optimize=True)

    baseline = HERE / 'baseline'
    comp = Image.new('RGB',(800,906),NAVY)
    draw = ImageDraw.Draw(comp)
    for i, (directory, heading) in enumerate([(baseline,'개선 전 · 390 × 844'),(capture_dir,'개선 후 · 390 × 844')]):
        label(draw,(i*400+14,17),heading,25)
        comp.paste(Image.open(directory / 'battle-390.png').convert('RGB'),(i*400+5,57))
    comp.save(out/'before-after-mobile.png',optimize=True)

    comp = Image.new('RGB',(1280,1700),NAVY)
    draw = ImageDraw.Draw(comp)
    for i,(directory,heading) in enumerate([(baseline,'개선 전 · 1280 × 800'),(capture_dir,'개선 후 · 1280 × 800')]):
        label(draw,(24,i*850+12),heading,27)
        comp.paste(Image.open(directory/'battle-1280.png').convert('RGB'),(0,i*850+50))
    comp.save(out/'before-after-desktop.png',optimize=True)

    provenance = {
        'method': 'Actual Unity WebGL screenshot. Wide: native screenshot + title strip. Square: uniform-scale crop + title strip. No generated/painted gameplay assets.',
        'source_crop_xyxy': {'thumb':None,'square':[425,58,1205,799]},
        'palette': {'navy':NAVY,'cream':CREAM,'gold':GOLD},
        'font':str(FONT.relative_to(ROOT)),
        'sources':sources,
        'source_sha256':{name:hashlib.sha256((ROOT/src).read_bytes()).hexdigest() for name,src in sources.items()},
        'outputs':{p.name:{'bytes':p.stat().st_size,'dimensions':Image.open(p).size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in out.glob('*.png')},
    }
    if args.install_public:
        if args.comparisons_only:
            raise ValueError('--install-public requires cover generation')
        public = ROOT/'public/g/hyeopgok-sasu'
        provenance['installed_public'] = {}
        for filename in ('thumb.png','square.png'):
            destination = public/filename
            shutil.copyfile(out/filename,destination)
            provenance['installed_public'][filename] = {'path':str(destination.relative_to(ROOT)), 'sha256':hashlib.sha256(destination.read_bytes()).hexdigest()}
    if not args.comparisons_only:
        (out/'provenance.json').write_text(json.dumps(provenance,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(provenance,ensure_ascii=False,indent=2))


if __name__ == '__main__':
    main()
