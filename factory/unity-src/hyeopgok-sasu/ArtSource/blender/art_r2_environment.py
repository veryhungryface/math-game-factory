"""Art round 2 terrain-only rebuild. Original mesh, fixed game paths/pad bounds.

Blender -b --factory-startup --python ArtSource/blender/art_r2_environment.py
or live MCP exec(compile(Path(...).read_text(), str(path), 'exec')).
Imports constructors without running the all-assets exporter. Export owns only
terrain.fbx and art-r2-environment-report.json; other agents' assets are untouched.
"""
import ast, bpy, math, json, random
from pathlib import Path
SOURCE = Path(__file__).resolve().parent / 'build_models.py'
module = ast.parse(SOURCE.read_text())
# Stop before build_models.py's destructive scene-wide/export entrypoint.
cut = next(i for i,n in enumerate(module.body) if isinstance(n,ast.For))
namespace={'__file__':str(SOURCE),'__name__':'r2_environment_constructors'}
exec(compile(ast.Module(body=module.body[:cut],type_ignores=[]),str(SOURCE),'exec'),namespace)
globals().update({k:v for k,v in namespace.items() if not k.startswith('__')})

def blend(a,b,t):return tuple(a[k]*(1-t)+b[k]*t for k in range(3))+(1,)
def hexcol(s):return tuple(int(s[i:i+2],16)/255 for i in (1,3,5))+(1,)
SAND=hexcol('#E9C9B0');SLATE=hexcol('#838894');BASE=hexcol('#263945')
PAL['cliff']=SLATE;PAL['cliff_light']=SLATE;PAL['cliff_mid']=SLATE
PAL['grass_dark']=hexcol('#198969');PAL['grass']=hexcol('#2DAA74')

def meadow_color(x,z,y,base):
 # Shared vertices have identical deterministic low-frequency field values.
 noise=.034*math.sin(x*.41+z*.17)+.026*math.sin(z*.53-x*.23)
 if y < -1:
  base=blend(hexcol('#208F61'),hexcol('#0D7461'),.37+.15*math.sin(x*.055+z*.04))
 return tint(base,1+noise)

def ground_top(m,poly,y,color,depth=2,variation=1):
 base=PAL[color] if isinstance(color,str) else color
 points=[Vector((x,z,0)) for x,z in poly]
 def face(a,b,c):
  if max((a-b).length,(b-c).length,(c-a).length)>1.30:
   ab=(a+b)*.5;bc=(b+c)*.5;ca=(c+a)*.5
   for tri in ((a,ab,ca),(ab,b,bc),(ca,bc,c),(ab,bc,ca)):face(*tri)
   return
  pts=[(p.x,y,p.y) for p in (a,b,c)]
  if (b-a).cross(c-a).z>0:pts.reverse()
  m.face(pts,base)
  m.colors[-3:]=[meadow_color(p[0],p[2],y,base) for p in pts]
 for tri in tessellate_polygon([points]):face(*[points[p] if isinstance(p,int) else p for p in tri])

def rock_face(m,quad,y0,y1,variation):
 m.face(quad,SLATE)
 m.colors[-len(quad):]=[tint(blend(BASE,SLATE,max(0,min(1,(v[1]-y0)/max(.01,y1-y0)))),variation) for v in quad]

def cliff_bank(m,poly,y0,y1,top='grass',depth=2,variation=1):
 if sum(poly[i][0]*poly[(i+1)%len(poly)][1]-poly[(i+1)%len(poly)][0]*poly[i][1] for i in range(len(poly)))<0:poly=list(reversed(poly))
 centre=Vector((sum(p[0] for p in poly)/len(poly),sum(p[1] for p in poly)/len(poly)))
 edge=[]
 for i,a in enumerate(poly):
  av,bv=Vector(a),Vector(poly[(i+1)%len(poly)]);count=max(1,math.ceil((bv-av).length/.92))
  for j in range(count):edge.append(av.lerp(bv,j/count))
 for i,a in enumerate(edge):
  b=edge[(i+1)%len(edge)];da=(centre-a).normalized();db=(centre-b).normalized()
  topa=(a.x,y1,a.y);topb=(b.x,y1,b.y)
  # Fractured light sandstone lip, deliberately distinct from the grass.
  inset=.08+.10*(.5+.5*math.sin(i*1.79));pa=a+da*inset;pb=b+db*(.09+.08*(.5+.5*math.sin((i+1)*1.79)))
  m.face([(pa.x,y1+.002,pa.y),(pb.x,y1+.002,pb.y),topb,topa],tint(SAND,.96+.05*math.sin(i)))
  ma=a+da*(.11+.25*(i%3));mb=b+db*(.11+.25*((i+1)%3))
  ha=y0+(y1-y0)*(.41+.11*math.sin(i*1.71));hb=y0+(y1-y0)*(.41+.11*math.sin((i+1)*1.71))
  ba=a+da*.18;bb=b+db*.18
  middlea=(ma.x,ha,ma.y);middleb=(mb.x,hb,mb.y)
  for q in [[(ba.x,y0,ba.y),middlea,middleb,(bb.x,y0,bb.y)],[middlea,topa,topb,middleb]]:
   value=.94+.10*math.sin(i*1.77)
   rock_face(m,[q[0],q[1],q[2]],y0,y1,value)
   rock_face(m,[q[0],q[2],q[3]],y0,y1,value*.965)
  # Sparse hanging grass blades interrupt the mineral edge, not a green tube.
  if i%4==0:
   c=a.lerp(b,.42);d=(b-a).normalized()*.10
   m.face([(c.x-d.x,y1+.013,c.y-d.y),(c.x+d.x,y1+.013,c.y+d.y),(c.x-da.x*.07,y1-.16,c.y-da.y*.07)],'grass_dark')
 ground_top(m,poly,y1,top,depth,variation)


ROCK_LOW=hexcol('#56657A');ROCK_HIGH=hexcol('#A7ACB6');ROCK_TOP=hexcol('#EDD3BC');ROCK_BEVEL=hexcol('#CDBBAE')

def mesa_block(m,x,z,r,bottom,top,seed,aspect=1.0):
 """One convex faceted block: flat warm top, bevel, two-band pale slate sides."""
 rng=random.Random(seed);h=top-bottom;n=6+seed%3;phase=rng.uniform(0,math.tau)
 bevel=min(.20,h*.14);pts=[]
 for level,(yy,rad,jit) in enumerate([(bottom,1.0,.10),(bottom+h*rng.uniform(.38,.52),.97,.07),(top-bevel,.93,.05),(top,.76,.05)]):
  for i in range(n):
   a=phase+i*math.tau/n+rng.uniform(-.22,.22)+level*.09
   rr=r*rad*rng.uniform(1-jit,1+jit)
   pts.append((x+math.cos(a)*rr,yy,z+math.sin(a)*rr*aspect))
 bm=bmesh.new()
 for q in pts:bm.verts.new(q)
 bmesh.ops.convex_hull(bm,input=list(bm.verts),use_existing_faces=False)
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
 for f in bm.faces:
  ny=f.normal.y;cy=sum(v.co.y for v in f.verts)/len(f.verts);noise=.95+rng.random()*.08
  if ny>.93:col=tint(ROCK_TOP,.97+rng.random()*.05)
  elif ny>.35:col=tint(ROCK_BEVEL,noise)
  else:
   t=max(0,min(1,(cy-bottom)/max(.01,h)))
   col=tint(blend(ROCK_LOW,ROCK_HIGH,.18+.82*t),noise)
  m.face([tuple(v.co) for v in f.verts],col)
 bm.free()

def block_ridge(m,points,bottom,hmin,hmax,rmin,rmax,seed,inward=None,step=1.15):
 """Overlapping stepped blocks along a polyline; optional lower steps inward."""
 rng=random.Random(seed);k=0
 for (ax,az),(bx,bz) in zip(points,points[1:]):
  a,b=Vector((ax,az)),Vector((bx,bz));count=max(1,math.ceil((b-a).length/step))
  d=(b-a).normalized();side=Vector((-d.y,d.x))
  for j in range(count):
   p=a.lerp(b,j/count)+side*rng.uniform(-.25,.25);k+=1
   wave=.5+.5*math.sin(k*1.37+seed)
   r=rng.uniform(rmin,rmax);h=hmin+(hmax-hmin)*(.35+.65*wave)*rng.uniform(.85,1.0)
   mesa_block(m,p.x,p.y,r,bottom,bottom+h,seed*131+k,rng.uniform(.8,1.15))
   if inward is not None and k%2==0:
    q=p+Vector(inward)*(r*rng.uniform(.75,1.0))+d*rng.uniform(-.3,.3)
    mesa_block(m,q.x,q.y,r*rng.uniform(.55,.72),bottom,bottom+h*rng.uniform(.38,.55),seed*977+k,rng.uniform(.85,1.2))

def build_block_ridges(m):
 B=-1.52
 # West lower wall: continuous canyon ridge beyond the red/blue road, stepping
 # down toward the road; it hands over to the tall western mesa at z~4.
 block_ridge(m,[(-11.2,-11.0),(-11.6,-7.5),(-11.9,-4.0),(-12.1,.6),(-11.2,3.2),(-11.3,7.5),(-11.2,12.4)],B,1.5,2.5,.95,1.35,11,inward=(1,0))
 # Blocks leaning on the tall west mesa face break its long slab into masses.
 block_ridge(m,[(-9.9,3.55),(-7.8,3.55),(-6.0,4.25),(-4.9,5.5),(-3.8,6.6)],B,1.6,2.5,.85,1.15,23,inward=(.2,-1))
 # Low sandstone lip blocks on the west mesa top edge.
 block_ridge(m,[(-9.2,4.55),(-6.9,4.5),(-5.3,5.6)],3.1,.7,1.15,.55,.85,31)
 # East lower wall and east mesa face.
 block_ridge(m,[(11.0,-11.0),(11.3,-7.0),(11.6,-3.5),(11.7,.9),(11.0,3.6),(9.9,5.0),(8.3,4.75)],B,1.5,2.5,.95,1.35,41,inward=(-1,0))
 block_ridge(m,[(9.3,5.25),(10.9,6.6),(12.4,8.8)],B,1.4,2.2,.8,1.1,47,inward=(-.4,-1))
 block_ridge(m,[(8.9,5.55),(10.2,7.2)],1.2,.6,1.0,.5,.75,53)
 # Far north skyline masses (beyond fog start at portrait).
 block_ridge(m,[(-8.9,14.2),(-5.5,15.6),(-2.2,16.4)],B,1.8,2.5,1.1,1.4,61)
 block_ridge(m,[(1.9,17.0),(5.2,16.4),(8.4,15.2)],B,1.8,2.5,1.1,1.4,67)
 # South of the creek: two low clusters frame the bridge from outside.
 block_ridge(m,[(-9.5,-13.8),(-7.8,-14.6)],B,1.0,1.7,.8,1.1,71)
 block_ridge(m,[(8.2,-14.4),(10.0,-13.6)],B,1.0,1.7,.8,1.1,73)

def terrain_r2():
 m=MeshMaker('terrain')
 # 1 m cells across all camera-visible ground; coarse continuation beyond fog.
 steps=[-80,-64,-48,-32]+list(range(-24,25))+[32,48,64,80]
 for i in range(len(steps)-1):
  for j in range(len(steps)-1):
   x0,x1=steps[i:i+2];z0,z1=steps[j:j+2]
   pts=[(x0,-1.52,z0),(x0,-1.52,z1),(x1,-1.52,z1),(x1,-1.52,z0)]
   m.face(pts,'grass_dark');m.colors[-4:]=[meadow_color(x,z,-1.52,PAL['grass_dark']) for x,y,z in pts]
 cliff_bank(m,PLATEAU,-1.5,1.20,'grass')
 cliff_bank(m,[(2.05,6.35),(6.45,6.35),(6.45,11),(2.05,11)],-1.5,.245,'path')
 cliff_bank(m,[(-5.5,-1.15),(-3.25,-1.15),(-3.25,3),(-5.5,3)],-1.5,.245,'path')
 path=catmull(PATH_POINTS,7);left=[];right=[];outer=[]
 for i,p in enumerate(path):
  direction=(Vector(path[min(len(path)-1,i+1)])-Vector(path[max(0,i-1)])).normalized();normal=Vector((-direction.y,direction.x));v=Vector(p)
  left.append(v+normal*1.02);right.append(v-normal*1.02)
  # Rock shoulders change every ~1m, forming broken straight mineral faces.
  rough=.075*math.sin((i//3)*2.19)+.03*math.sin((i//3)*4.2)
  outer.append(v+normal*(1.65+rough))
 for i in range(len(path)-1):
  if -1.35<(path[i][0]+path[i+1][0])*.5<1.35 and (path[i][1]+path[i+1][1])*.5< -6.3:continue
  a,b,c,d=left[i],right[i],right[i+1],left[i+1]
  for lo,hi in [(-1,-.86),(-.86,-.62),(-.62,-.53),(-.53,0),(0,.53),(.53,.62),(.62,.86),(.86,1)]:
   q=[a.lerp(b,(lo+1)*.5),a.lerp(b,(hi+1)*.5),d.lerp(c,(hi+1)*.5),d.lerp(c,(lo+1)*.5)]
   def col(t):
    edge=max(0,min(1,(abs(t)-.86)/.14));rut=1+.065*max(0,1-abs(abs(t)-.575)/.055)
    return blend(tint('path',rut),PAL['grass'],edge)
   m.face([(p.x,.25,p.y) for p in reversed(q)],'path');m.colors[-4:]=[col(lo),col(hi),col(hi),col(lo)]
  for side in [left,right]:
   v,w=side[i],side[i+1];rock_face(m,[(v.x,-1.50,v.y),(w.x,-1.50,w.y),(w.x,.25,w.y),(v.x,.25,v.y)],-1.5,.25,1)
 for i in range(len(path)-1):
  if -1.55<(path[i][0]+path[i+1][0])*.5<1.55 and (path[i][1]+path[i+1][1])*.5< -6.3:continue
  a,b,c,d=left[i],left[i+1],outer[i+1],outer[i]
  # Narrow grass inside, broad sandstone outward shoulder.
  innera=a.lerp(d,.33);innerb=b.lerp(c,.33)
  m.face([(innera.x,.48,innera.y),(innerb.x,.48,innerb.y),(b.x,.48,b.y),(a.x,.48,a.y)],'grass')
  m.face([(d.x,.48,d.y),(c.x,.48,c.y),(innerb.x,.48,innerb.y),(innera.x,.48,innera.y)],tint(SAND,.97+.035*math.sin(i*.81)))
  midA=-.56+.18*math.sin((i//3)*1.4);midB=-.56+.18*math.sin(((i+1)//3)*1.4)
  for q in [[(d.x,-1.52,d.y),(c.x,-1.52,c.y),(c.x,midB,c.y),(d.x,midA,d.y)],[(d.x,midA,d.y),(c.x,midB,c.y),(c.x,.48,c.y),(d.x,.48,d.y)]]:rock_face(m,q,-1.52,.48,.98+.035*math.sin((i//3)*1.91))
  if i%8==0:
   p=d.lerp(c,.5);m.face([(p.x-.10,.49,p.y),(p.x+.10,.49,p.y),(p.x,.27,p.y-.06)],'grass_dark')
 cliff_bank(m,[(-10,4),(-5.7,4),(-5.65,5.25),(-3.5,7),(-.5,9),(-1,14),(-10,14)],-1.5,3.1,'grass_dark')
 cliff_bank(m,[(7.1,5),(9,5),(13,9),(13,16),(6.9,16)],-1.5,1.2,'grass_dark')
 # Round 2b: Kingshot-style massed cliff blocks. Flat sandstone tops, faceted
 # pale-slate faces, clustered along ridges so rocks read as canyon walls,
 # never as isolated polka-dot boulders. Heights <= 2.5m (1.5x barracks).
 build_block_ridges(m)
 return emit(m)

# Clear this asset only in the shared live Blender scene.
for ob in list(bpy.data.objects):
 if ob.name=='terrain' or ob.name.startswith('terrain.'):
  bpy.data.objects.remove(ob,do_unlink=True)
terrain_r2()
report={'round':2,'assets':ASSETS,'surface_grid_m':1,'grass_variation':'+/-6% continuous low-frequency vertex RGB','cliff_top':'#E9C9B0','cliff_side_gradient':['#263945','#838894'],'lower_plain':['#208F61','#0D7461'],'max_baked_outcrop_height_m':2.5,'rock_style':'clustered flat-top sandstone blocks (round 2b)','rules_path_and_pad_coordinates':'unchanged'}
(HERE/'art-r2-environment-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('ART_R2_ENVIRONMENT_COMPLETE',json.dumps(report))
