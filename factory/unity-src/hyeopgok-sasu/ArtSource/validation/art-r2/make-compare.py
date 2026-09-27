"""Evidence contact sheet: rescale/letterbox only; never repaint game pixels."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import hashlib, json, sys

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[5]
FONT='/System/Library/Fonts/AppleSDGothicNeo.ttc'
title=ImageFont.truetype(FONT,29)
small=ImageFont.truetype(FONT,21)
tag=sys.argv[1] if len(sys.argv)>1 else 'round1'
rows=[
 ('scratchpad/kingshot-ref/img/as_12.jpg',tag+'/play-390-8s.png',940,'초반 전투 · 390×844'),
 ('scratchpad/kingshot-ref/img/gp_1.jpg',tag+'/play-390-15s.png',940,'전투 15초 · 390×844'),
 ('scratchpad/kingshot-ref/frames/Vioib7IWvqc/f024.jpg',tag+'/play-1280-8s.png',760,'전투 8초 · 1280×800'),
]
sheet=Image.new('RGB',(2160,sum(r[2] for r in rows)+76),'#142e36')
draw=ImageDraw.Draw(sheet)
draw.text((28,20),'아트 라운드 2 · 참고 원본과 실제 WebGL 화면',font=title,fill='#fff1cf')
y=76; sources=[]
for ref,ours,height,label in rows:
 for col,file in enumerate([ROOT/ref,HERE/ours]):
  im=Image.open(file).convert('RGB')
  w,h=im.size
  im.thumbnail((1044,height-86),Image.Resampling.LANCZOS)
  x=col*1080+18+(1044-im.width)//2
  yy=y+66+(height-86-im.height)//2
  sheet.paste(im,(x,yy))
  draw.text((col*1080+24,y+16),'킹샷 · 참고' if col==0 else '협곡 사수 · '+label,font=title,fill='#fff1cf')
  sources.append({'path':str(file.relative_to(ROOT)),'original':[w,h],
                  'sha256':hashlib.sha256(file.read_bytes()).hexdigest(),
                  'operation':'aspect-preserving rescale and letterbox only'})
 y+=height
sheet.save(HERE/'compare.png',optimize=True)
(HERE/'compare-sources.json').write_text(json.dumps(sources,ensure_ascii=False,indent=2)+'\n')
print(HERE/'compare.png')
