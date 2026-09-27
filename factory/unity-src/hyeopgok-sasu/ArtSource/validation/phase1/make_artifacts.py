#!/usr/bin/env python3
"""Contact sheet and covers from unretouched real WebGL screenshots only."""
from pathlib import Path
from PIL import Image, ImageOps, ImageDraw, ImageFont
import argparse, json, hashlib, shutil

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[5]
FONT=ROOT/'factory/unity/kit/MgfKit/Resources/MgfKit/Fonts/MgfKR-Bold.otf'
NAVY='#142e35'; GOLD='#efc85a'; CREAM='#fff1ce'
def font(n): return ImageFont.truetype(str(FONT),n)
def panel(image, size):
    result=Image.new('RGB',size,NAVY)
    im=ImageOps.contain(image.convert('RGB'),size,Image.Resampling.LANCZOS)
    result.paste(im,((size[0]-im.width)//2,(size[1]-im.height)//2)); return result
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    ap=argparse.ArgumentParser();ap.add_argument('--tag',default='final');ap.add_argument('--install-public',action='store_true');a=ap.parse_args()
    source=HERE/a.tag
    refs=[ROOT/'scratchpad/kingshot-ref/frames/pl4FYO1T3Iw/f012.jpg',ROOT/'scratchpad/kingshot-ref/frames/pl4FYO1T3Iw/f022.jpg']
    our=[source/'battle-1280.png',source/'boss-1280.png']
    paths=[refs[0],our[0],refs[1],our[1]]
    labels=['킹샷 참고 · 전투','협곡 사수 v2 · 실제 전투','킹샷 참고 · 성장한 기지','협곡 사수 v2 · 성장·마지막 웨이브']
    sheet=Image.new('RGB',(1932,1300),NAVY);d=ImageDraw.Draw(sheet)
    for i,(p,label) in enumerate(zip(paths,labels)):
        x=8+(i%2)*966;y=(i//2)*650
        d.text((x+16,y+17),label,font=font(27),fill=CREAM)
        im=Image.open(p)
        if i%2: im=im.crop((0,56,1280,800))
        sheet.paste(panel(im,(950,585)),(x,y+58))
    sheet.save(HERE/'compare.png',optimize=True)
    provenance={'method':'Native actual WebGL screenshots with a game-title strip; gameplay pixels are unretouched. References appear only in compare.png, never public covers.','sources':{},'outputs':{}}
    for name,size in [('thumb',(1200,630)),('square',(1080,1080))]:
        p=source/('hero-cover-wide.png' if name=='thumb' else 'hero-cover-square.png')
        im=Image.open(p).convert('RGB');assert im.size==size
        d=ImageDraw.Draw(im);d.rectangle((0,0,size[0],76),fill=NAVY);d.rectangle((0,72,size[0],76),fill=GOLD)
        d.text((24,0),'협곡 사수',font=font(56),fill=CREAM)
        d.text((size[0]-342,24),'왕을 끌어 협곡을 지켜라',font=font(25),fill=GOLD)
        target=HERE/(name+'.png');im.save(target,optimize=True)
        provenance['sources'][name]={'path':str(p.relative_to(ROOT)),'sha256':sha(p)}
        provenance['outputs'][name]={'sha256':sha(target),'bytes':target.stat().st_size,'dimensions':size}
        if a.install_public: shutil.copyfile(target,ROOT/'public/g/hyeopgok-sasu'/target.name)
    (HERE/'provenance.json').write_text(json.dumps(provenance,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(provenance,ensure_ascii=False))
if __name__=='__main__':main()
