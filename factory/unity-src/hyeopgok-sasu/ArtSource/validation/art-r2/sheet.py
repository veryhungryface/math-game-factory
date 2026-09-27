"""Contact sheets of a capture tag (inspection only)."""
import sys
from PIL import Image
tag=sys.argv[1];out=sys.argv[2]
n390=['title-390','packs-390','play-390-8s','pour-390-stream','fraction-390','play-390-after4correct','victory-390','geometry-390']
n1280=['title-1280','play-1280-3s','play-1280-15s','pour-1280-stream','fraction-1280','play-1280-after4correct','victory-1280','geometry-1280']
def load(n,size):
  try:return Image.open(f'{tag}/{n}.png').convert('RGB').resize(size)
  except Exception:return Image.new('RGB',size,'#400')
s=Image.new('RGB',(390*4,844*2))
for i,n in enumerate(n390):s.paste(load(n,(390,844)),((i%4)*390,(i//4)*844))
s.save(out+'/s390.png')
s=Image.new('RGB',(640*2,400*4))
for i,n in enumerate(n1280):s.paste(load(n,(640,400)),((i%2)*640,(i//2)*400))
s.save(out+'/s1280.png')
