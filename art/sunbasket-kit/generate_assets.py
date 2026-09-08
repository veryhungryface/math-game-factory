import bpy, math, os, json, random, argparse, sys
from mathutils import Vector, Matrix
ROOT=os.path.abspath(os.path.join(os.path.dirname(__file__),'../..'))
p=argparse.ArgumentParser();p.add_argument('--models-only',action='store_true');p.add_argument('--palette');p.add_argument('--output-dir',default=os.path.join(ROOT,'public/g/sunbasket-farm'));p.add_argument('--source-dir',default=os.path.dirname(__file__));p.add_argument('--font-path',default='/System/Library/Fonts/AppleSDGothicNeo.ttc');a=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
GAME=os.path.abspath(a.output_dir);OUT=os.path.abspath(a.source_dir);ASSET=GAME+'/assets';os.makedirs(ASSET,exist_ok=True);os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.name='Sunbasket Farm original orchard studio'
parts=[];assets={};anchor_objects={};random.seed(1709)
ANCHORS={'farmhouse':{'anchor-doorstep':(0,-2.18,.10),'anchor-pot-left':(-1.49,-1.50,.28),'anchor-pot-right':(1.49,-1.50,.28)},'market-stall':{'anchor-display':(0,-.12,.98),'anchor-sign':(0,-1.00,1.58)},'apple-tree':{'anchor-trunk-top':(0,0,1.65)}}
C={'cream':(.87,.76,.52),'ivory':(.99,.94,.75),'plaster':(.57,.19,.074),'peach':(.91,.53,.28),'wood':(.32,.135,.038),'woodlight':(.59,.31,.105),'woodwarm':(.74,.41,.16),'dark':(.055,.042,.025),'roof':(.012,.30,.30),'rooflight':(.024,.45,.40),'roofdark':(.009,.15,.18),'soil':(.26,.112,.034),'stone':(.52,.46,.30),'stone2':(.68,.62,.43),'leaf':(.036,.21,.009),'leaflight':(.12,.31,.037),'leafsun':(.22,.40,.055),'orange':(.97,.225,.016),'orangehi':(1,.44,.025),'red':(.78,.018,.014),'redhi':(1,.065,.021),'gold':(1,.63,.025),'gold2':(.84,.41,.018),'straw':(.80,.61,.24),'metal':(.14,.18,.15),'sky':(.22,.53,.65),'glass':(.08,.34,.45),'white':(.95,.98,.80),'grass':(.18,.38,.025),'grasslight':(.25,.43,.045)}
if a.palette:
 with open(a.palette) as palette_file:C.update({k:tuple(v) for k,v in json.load(palette_file).items()})
def mat(name,rough=.72):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes.new('ShaderNodeVertexColor');n.layer_name='Color';b=m.node_tree.nodes.get('Principled BSDF');m.node_tree.links.new(n.outputs['Color'],b.inputs['Base Color']);b.inputs['Roughness'].default_value=rough;return m
body=mat('FARM_VERTEX_COLOR');glass=mat('WINDOW_VERTEX_COLOR',.18);glass.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value=.3

def finish(o,name,col='wood',bevel=0,seg=2,smooth=False,gloss=False,tag=None):
 o.name=name
 if bevel:
  mod=o.modifiers.new('Soft hand-shaped corners','BEVEL');mod.width=bevel;mod.segments=seg;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 o.data.materials.append(glass if gloss else body);ca=o.data.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER');c=C.get(col,col)
 for d in ca.data:d.color=(*c,1)
 for f in o.data.polygons:f.use_smooth=bool(bevel) or smooth
 if bevel:
  try:
   mod=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');mod.keep_sharp=True;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
  except:pass
 if tag:o['asset_part']=tag
 parts.append(o);return o

def box(name,loc,size,col='wood',b=.035,seg=2,gloss=False,tag=None):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,col,b,seg,gloss=gloss,tag=tag)
def cyl(name,loc,r,d,col='wood',n=12,rot=None,b=0,tag=None):
 bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=d,location=loc,rotation=rot or (0,0,0));return finish(bpy.context.object,name,col,b,smooth=True,tag=tag)
def ico(name,loc,size,col='leaf',sub=2,smooth=True):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc);o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,col,smooth=smooth)
def sphere(name,loc,size,col='red',segments=12,rings=6):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=loc);o=bpy.context.object;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,name,col,smooth=True)
def lathe(name,profile,loc=(0,0,0),col='wood',n=12):
 v=[(r*math.cos(j*math.tau/n),r*math.sin(j*math.tau/n),z) for z,r in profile for j in range(n)]
 f=[(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j) for i in range(len(profile)-1) for j in range(n)]+[tuple(reversed(range(n))),tuple((len(profile)-1)*n+j for j in range(n))]
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);scene.collection.objects.link(o);o.location=loc;return finish(o,name,col,smooth=True)
def beam(name,start,end,width,depth=None,col='wood',b=.022,tag=None):
 delta=Vector(end)-Vector(start);o=box(name,(Vector(start)+Vector(end))/2,(width,depth or width,delta.length),col,b,tag=tag);o.rotation_euler=delta.to_track_quat('Z','Y').to_euler();return o

def leaf(name,start,end,width,col='leaflight',bend=.08):
 start=Vector(start);end=Vector(end);d=end-start;side=d.cross(Vector((0,0,1)))
 if side.length<.001:side=Vector((1,0,0))
 side.normalize();mid=start+d*.48+Vector((0,0,bend));ridge=mid+Vector((0,0,.022));v=[start,mid-side*width,end,mid+side*width,ridge,mid-Vector((0,0,.025))];f=[(0,1,4),(1,2,4),(2,3,4),(3,0,4),(0,5,1),(1,5,2),(2,5,3),(3,5,0)];me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);scene.collection.objects.link(o);return finish(o,name,col,smooth=True)

def arch(name,loc,w,stem,col='wood',depth=.08,trim=0):
 r=w/2;outline=[(-r,0),(r,0),(r,stem)]+[(r*math.cos(t*math.pi/12),stem+r*math.sin(t*math.pi/12)) for t in range(1,13)]
 if not trim:
  n=len(outline);v=[(x,y,z) for y in [-depth/2,depth/2] for x,z in outline];f=[tuple(reversed(range(n))),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 else:
  inner=[(x*(w-2*trim)/w,z+trim if z==0 else z-trim*.3) for x,z in outline];n=len(outline);v=[(x,y,z) for y in [-depth/2,depth/2] for path in [outline,inner] for x,z in path];f=[]
  for i in range(n):
   j=(i+1)%n;f.extend([(i,j,n+j,n+i),(i+2*n,3*n+i,3*n+j,2*n+j),(i,i+2*n,j+2*n,j),(i+n,j+n,j+3*n,i+3*n)])
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);scene.collection.objects.link(o);o.location=loc;return finish(o,name,col,.012,2)

def roof(name,profile,depth,col='roof',loc=(0,0,0),thick=.12):
 n=len(profile);v=[(x,y,z+dz) for dz in [0,-thick] for y in [-depth/2,depth/2] for x,z in profile];f=[]
 for i in range(n-1):f.extend([(i,i+1,n+i+1,n+i),(2*n+i,3*n+i,3*n+i+1,2*n+i+1),(i,2*n+i,2*n+i+1,i+1),(n+i,n+i+1,3*n+i+1,3*n+i)])
 f.extend([(0,n,3*n,2*n),(n-1,3*n-1,4*n-1,2*n-1)])
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);scene.collection.objects.link(o);o.location=loc;return finish(o,name,col,.03,2,smooth=True)

def close(name,normalize=None,rotor=False):
 global parts
 groups={}
 for o in parts:groups.setdefault((o.data.materials[0].name,o.get('asset_part','static')),[]).append(o)
 merged=[]
 for key,objs in groups.items():
  bpy.ops.object.select_all(action='DESELECT')
  for o in objs:o.select_set(True)
  bpy.context.view_layer.objects.active=objs[0]
  if len(objs)>1:bpy.ops.object.join()
  o=bpy.context.object;bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.name=('windmill-rotor' if key[1]=='rotor' else name+'_'+(key[0] if key[1]=='static' else key[1]) + ('_glass' if 'WINDOW' in key[0] else ''));o['kit_asset']=name;o['kit_part']=key[1];merged.append(o)
 bpy.context.view_layer.update();bottom=min((o.matrix_world@v.co).z for o in merged for v in o.data.vertices);top=max((o.matrix_world@v.co).z for o in merged for v in o.data.vertices);scale=(normalize/(top-bottom)) if normalize else 1
 for o in merged:
  transform=o.matrix_world.copy()
  for v in o.data.vertices:v.co=(transform@v.co-Vector((0,0,bottom)))*scale
  o.matrix_world=Matrix.Identity(4)
  if name=='farmhouse' and o.get('kit_part')=='door':
   pivot=Vector((-.45,-1.10,.36))*scale-Vector((0,0,bottom*scale))
   for v in o.data.vertices:v.co-=pivot
   o.location=pivot
  if o.name=='windmill-rotor':
   pivot=Vector((0,-.86,2.67))*scale-Vector((0,0,bottom*scale))
   for v in o.data.vertices:v.co-=pivot
   o.location=pivot
 if name=='strawberry':
  xs=[v.co.x for o in merged for v in o.data.vertices];width=max(xs)-min(xs);xy=.95/width
  for o in merged:
   for v in o.data.vertices:v.co.x*=xy;v.co.y*=xy
 bpy.ops.object.select_all(action='DESELECT')
 for o in merged:o.select_set(True)
 anchor_objects[name]=[]
 for anchor_name,point in ANCHORS.get(name,{}).items():
  obj=bpy.data.objects.new(anchor_name,None);scene.collection.objects.link(obj);obj.location=(Vector(point)-Vector((0,0,bottom)))*scale;obj['kit_asset']=name;obj['kit_part']='anchor';obj.select_set(True);anchor_objects[name].append(obj)
 bpy.ops.export_scene.gltf(filepath=ASSET+'/'+name+'.glb',export_extras=True,export_format='GLB',use_selection=True,export_yup=True,export_animations=False,export_cameras=False,export_lights=False)
 assets[name]=merged;parts=[]

exec(compile(open(os.path.join(os.path.dirname(__file__),'v3_geometry.py')).read(),'v3_geometry.py','exec'))
build_carrot();build_carrot('young');build_sprout()
for i in range(5):
 th=i*math.tau/5;leaf('Broad strawberry leaf',(0,0,.18),(.38*math.cos(th),.38*math.sin(th),.44+(i%2)*.04),.18,'leaflight' if i%2 else 'leaf',.12)
for i,(x,y,z,r) in enumerate([(-.15,-.10,.27,.22),(.17,.06,.30,.19)]):
 lathe('Ripe strawberry',[(0,.016),(.09,r*.66),(.22,r),(.34,r*.78),(.38,r*.24)],(x,y,z-.19),'red' if i else 'redhi',n=10)
 for k in range(5):
  th=k*math.tau/5;leaf('Berry crown',(x,y,z+.14),(x+.18*math.cos(th),y+.18*math.sin(th),z+.20),.045,'leafsun',.02)
 for dx,dz in [(-.065,.03),(.06,.01),(0,-.08)]:sphere('Golden strawberry seed',(x+dx,y-r*.86,z+dz),(.015,.008,.020),'gold',segments=6,rings=3)
close('strawberry',normalize=1)
cyl('Corn stalk',(0,0,.48),.035,.96,'leaf',n=8)
for i in range(4):
 th=i*2.15;leaf('Long arching corn leaf',(0,0,.22+i*.13),(.37*math.cos(th),.37*math.sin(th),.39+i*.13),.105,'leaflight' if i%2 else 'leafsun',.12)
lathe('Golden corn cob',[(0,.055),(.07,.12),(.35,.12),(.45,.075),(.49,.015)],(.11,-.015,.38),'gold',n=10)
for z in [.48,.57,.66,.75]:
 for ang in [math.pi*1.12,math.pi*1.5,math.pi*1.88]:
  box('Large kernel',(.11+.113*math.cos(ang),-.015+.113*math.sin(ang),z),(.041,.034,.045),'gold2',b=0)
leaf('Corn husk',(0,-.07,.41),(.20,-.06,.69),.10,'leaflight',.03)
for i in range(3):beam('Tassel',(0,0,.89),((i-1)*.065,0,1.06-abs(i-1)*.025),.02,col='straw',b=0)
close('corn',normalize=1)

# Crate: broad joinery and open gaps instead of a solid cube.
for i in range(4):box('Crate floor board',(-.36+i*.24,0,.075),(.22,.72,.10),'woodwarm',b=.018)
for x in [-.47,.47]:
 for y in [-.35,.35]:box('Crate corner post',(x,y,.33),(.10,.10,.59),'woodlight',b=.02)
for z in [.23,.46]:
 for y in [-.38,.38]:box('Open crate long plank',(0,y,z),(.99,.08,.16),'woodwarm',b=.022)
 for x in [-.49,.49]:box('Open crate short plank',(x,0,z),(.08,.78,.16),'woodlight',b=.02)
close('crate')

build_house()
build_market()

# Cart with curved handles and open planked body. Front = -Y authoring / +Z in glTF.
for i in range(5):box('Cart bed plank',(-.46+i*.23,.05,.52),(.21,1.29,.10),'woodwarm',b=.018)
for x in [-.61,.61]:
 for y in [-.54,.67]:box('Cart corner brace',(x,y,.78),(.10,.11,.68),'woodlight',b=.025)
 for z in [.73,.99]:box('Cart sideboard',(x,.04,z),(.095,1.39,.22),'woodwarm',b=.035)
for y in [-.59,.71]:
 for z in [.72,.98]:box('Cart endboard',(0,y,z),(1.27,.09,.21),'woodlight',b=.026)
for y in [-.40,.50]:
 beam('Cart iron axle',(-.80,y,.34),(.80,y,.34),.075,col='metal',b=.015)
 for x in [-.74,.74]:
  cyl('Dark rounded wheel',(x,y,.34),.32,.14,'wood',n=16,rot=(0,math.pi/2,0),b=.024)
  cyl('Wheel warm hub',(x+(-.076 if x<0 else .076),y,.34),.09,.04,'woodwarm',n=12,rot=(0,math.pi/2,0),b=.012)
  for i in range(6):
   th=i*math.tau/6;beam('Visible wheel spoke',(x+(-.079 if x<0 else .079),y,.34),(x+(-.079 if x<0 else .079),y+.26*math.cos(th),.34+.26*math.sin(th)),.040,col='woodwarm',b=.010)
for x in [-.43,.43]:
 beam('Swept tow arm',(x,-.49,.51),(x,-1.16,.50),.075,col='woodlight',b=.024)
 beam('Raised handle',(x,-1.16,.50),(x,-1.51,.69),.075,col='woodlight',b=.024)
beam('Cart handle grip',(-.45,-1.51,.69),(.45,-1.51,.69),.092,col='roof',b=.025)
close('cart')

# A small practical windmill, with an individually pivoted rotor exported as its own node.
lathe('Windmill stone footing',[(0,.95),(.18,.95),(.29,.82),(.34,.80)],col='stone2',n=16)
lathe('Tapered buttercream mill',[(.23,.75),(.45,.74),(1.75,.59),(2.66,.48),(2.88,.40)],col='cream',n=16)
for z in [.60,1.15,1.72]:cyl('Wood mill belt',(0,0,z),.73-(z-.45)*.11,.10,'woodlight',n=16,b=.018)
lathe('Teal mill cap',[(2.71,.66),(2.81,.64),(3.11,.39),(3.29,.025)],col='roof',n=16)
cyl('Rounded roof finial',(0,0,3.32),.09,.13,'gold2',n=12,b=.020)
arch('Mill shadowed doorway',(0,-.733,.28),.62,.92,'dark',.05)
arch('Mill timber doorway frame',(0,-.77,.27),.79,.93,'woodwarm',.08,trim=.085)
for i in [-1,0,1]:box('Mill door planks',(i*.17,-.797,.78),(.16,.04,.94),'woodlight',b=.018)
cyl('Mill rotor shaft',(0,-.67,2.67),.14,.50,'wood',n=12,rot=(math.pi/2,0,0),b=.020)
# Sail meshes have object coordinates at the rotor's pivot and rotate around local glTF Z.
for i in range(4):
 th=math.pi/4+i*math.pi/2
 def pt(r,t=0):return (math.cos(th)*r-math.sin(th)*t,-.86,2.67+math.sin(th)*r+math.cos(th)*t)
 beam('Rotor radial beam',pt(.04),pt(1.30),.10,.07,'woodwarm',.018,tag='rotor')
 for r in [.54,.78,1.02,1.25]:
  o=beam('Cream windmill sail',pt(r,-.14),pt(r,.20),.19,.045,'ivory',.025,tag='rotor')
 for t in [-.17,.23]:beam('Sail outside spar',pt(.39,t),pt(1.35,t),.035,.04,'woodlight',.012,tag='rotor')
cyl('Rotor round hub',(0,-.91,2.67),.205,.13,'rooflight',n=16,rot=(math.pi/2,0,0),b=.03,tag='rotor')
close('windmill',rotor=True)

build_tree()
# Open cream timber fence segment; no fortress silhouette or solid border.
for x in [-.94,.94]:
 box('Rounded fence post',(x,0,.49),(.16,.20,.98),'ivory',b=.055,seg=3)
 sphere('Fence cap',(x,0,1.0),(.12,.13,.09),'cream',segments=10,rings=5)
for z in [.38,.74]:box('Fence horizontal rail',(0,.045,z),(1.95,.12,.13),'cream',b=.03)
beam('Fence diagonal brace',(-.82,-.024,.27),(.82,-.024,.84),.08,.065,'ivory',.022)
close('fence')

manifest={'title':'햇살 바구니 / Sunbasket Farm','artRevision':3,'runtimeRevision':3,'provenance':'Original Blender meshes, hand-shaped from custom profiles, curved roof surfaces, leaves, arches and beveled timber. No downloaded geometry, textures, or game IP. No image-generation used.','axis':'glTF +Y up, +Z forward, y=0 ground. Mature crops height 1; carrot young .60 and sprout .25. Each crop is one primitive for instancing.','materials':'FARM_VERTEX_COLOR and WINDOW_VERTEX_COLOR use COLOR_0. Keep vertex colors. Windmill rotor has name windmill-rotor; rotate local Z, preserve initial position.','assets':{}}
for name,objs in assets.items():
 pts=[o.matrix_world@Vector(v) for o in objs for v in o.bound_box];lo=[min(p[i] for p in pts) for i in range(3)];hi=[max(p[i] for p in pts) for i in range(3)];tris=0
 for o in objs:o.data.calc_loop_triangles();tris+=len(o.data.loop_triangles)
 manifest['assets'][name]={'file':name+'.glb','bytes':os.path.getsize(ASSET+'/'+name+'.glb'),'triangles':tris,'drawCalls':len(objs),'parts':[{'name':o.name,'role':o.get('kit_part','static'),'pivot':[o.location.x,o.location.z,-o.location.y]} for o in objs],'anchors':{o.name:[o.location.x,o.location.z,-o.location.y] for o in anchor_objects.get(name,[])},'gltfBounds':{'min':[lo[0],lo[2],-hi[1]],'max':[hi[0],hi[2],-lo[1]]}}
with open(ASSET+'/manifest.json','w') as f:json.dump(manifest,f,ensure_ascii=False,indent=2)
print('MODELS_READY '+json.dumps(manifest),flush=True)
# Save a model studio first so the builder can immediately start consuming assets.
orig=bpy.data.collections.new('Original export meshes');scene.collection.children.link(orig)
for objs in assets.values():
 for o in objs:
  for c in list(o.users_collection):c.objects.unlink(o)
  orig.objects.link(o);o.hide_render=True;o.hide_set(True)

for anchor_list in anchor_objects.values():
 for o in anchor_list:
  for c in list(o.users_collection):c.objects.unlink(o)
  orig.objects.link(o);o.hide_render=True;o.hide_set(True)

if a.models_only:
 bpy.ops.wm.save_as_mainfile(filepath=OUT+'/sunbasket-farm-assets.blend',compress=True)
 print('MODELS_ONLY_DONE',flush=True);sys.exit(0)

def show(name,pos,scale=1,angle=0):
 root=bpy.data.objects.new('Display '+name,None);scene.collection.objects.link(root);root.location=pos;root.scale=(scale,scale,scale);root.rotation_euler.z=angle
 for src in assets[name]:
  o=src.copy();o.data=src.data;scene.collection.objects.link(o);o.hide_render=False;o.hide_set(False);o.parent=root
 return root
# The same orchard used in-game: an open farm, not a rectangular board enclosed by walls.
parts=[]
sphere('Rolling meadow',(0,0,-.53),(9.6,9.0,.65),'grass',segments=64,rings=16)
sphere('Meadow shoulder',(-5,3,-.25),(4,3,.62),'grasslight',segments=32,rings=10)
box('Rich soft earth bed',(-.25,-.55,.16),(5.7,4.7,.28),'soil',b=.24,seg=3)
for ix in range(7):
 x=-2.60+ix*.77
 for iy in range(5):
  y=-2.33+iy*.86
  name='carrot' if ix<3 else ('strawberry' if ix<5 else 'corn')
  show(name,(x,y,.30),.70,.15*math.sin(ix+iy))
# A warm curved lane is laid as overlapping ellipsoids, with irregular stepping stones.
for x,y,s in [(-3.95,-3.30,1.3),(-2.2,-3.78,1.3),(-.2,-4.1,1.5),(2.0,-3.83,1.4),(3.8,-2.65,1.25),(4.35,-.75,1.20),(4.2,1.08,1.2)]:sphere('Soft ochre footpath',(x,y,.06),(s,.73,.065),'straw',segments=24,rings=8)
show('farmhouse',(-4.12,3.14,.07),1.15,.10)
show('windmill',(3.53,4.06,.06),1.13,-.07)
show('market-stall',(4.20,-2.74,.09),1.1,-.17)
show('cart',(.34,-4.05,.1),1.08,.85)
show('crate',(.20,-3.78,.70),.71,.85)
for xyz,sc in [((-6.2,-.65,.04),1.06),((-6.4,2.27,.04),.91),((-.58,5.63,.04),1.10),((1.18,6.25,.04),.88),((6.15,1.6,.04),1.04)]:show('apple-tree',xyz,sc)
for x,y,angle in [(-6.6,-2.5,.18),(-5.8,-4.6,.28),(6.9,-.2,0),(6.75,2.03,0),(-2.0,6.4,math.pi/2)]:show('fence',(x,y,.07),1,angle)
for xyz,sc,ang in [((3.28,-1.2,.12),.8,0),((4.28,-.9,.12),.7,.13),((-5.0,1.45,.1),.70,.15)]:show('crate',xyz,sc,ang)
# A few broad leaves and flowers soften the meadow edge without crowding the farm.
for i in range(22):
 th=random.random()*math.tau;r=random.uniform(6.8,8.4);x=math.cos(th)*r;y=math.sin(th)*r
 for k in range(3):leaf('Meadow clover',(x,y,.09),(x+.23*math.cos(k*2.1),y+.23*math.sin(k*2.1),.31),.065,'leaflight',.04)
 if i%3==0:
  for k in range(5):sphere('Meadow flower petal',(x+.07*math.cos(k*math.tau/5),y+.07*math.sin(k*math.tau/5),.34),(.07,.045,.023),'ivory',segments=8,rings=4)
  sphere('Meadow flower center',(x,y,.36),(.047,.047,.02),'gold',segments=8,rings=4)
# Sunlit cream studio backdrop and large soft sources.
box('Warm infinite backdrop',(0,0,-.82),(200,200,.1),(.83,.77,.57),b=0)
world=bpy.data.worlds.new('Buttercream sky');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.57,.70,.62,1);world.node_tree.nodes['Background'].inputs[1].default_value=.42

def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
def light(name,pos,power,size,col):
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=col;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos;aim(o,(0,0,0))
light('Golden afternoon key',(-9,-8,16),2600,8,(1,.87,.62));light('Soft orchard bounce',(9,-3,10),1400,9,(.62,.87,.94));light('Warm leaf rim',(1,9,12),1700,8,(1,.94,.69))
d=bpy.data.cameras.new('Sunbasket cover');cam=bpy.data.objects.new('Sunbasket cover',d);scene.collection.objects.link(cam);cam.location=(11,-18,15);aim(cam,(0,.55,2.3));d.type='ORTHO';d.ortho_scale=32.3;scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
font=bpy.data.fonts.load(a.font_path) if os.path.exists(a.font_path) else bpy.data.fonts.get('Bfont')
def title(name,text,size,position,color):
 cu=bpy.data.curves.new(name,'FONT');cu.body=text;cu.font=font;cu.align_x='CENTER';cu.size=size;cu.offset=.028;cu.extrude=.004;cu.bevel_depth=.002;cu.bevel_resolution=1;o=bpy.data.objects.new(name,cu);scene.collection.objects.link(o);o.parent=cam;o.location=position;m=bpy.data.materials.new(name+' ink');m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Emission Color'].default_value=(*color,1);bs.inputs['Emission Strength'].default_value=.7;cu.materials.append(m);return o
sign=bpy.data.objects.new('Sunbasket wooden sign',None);scene.collection.objects.link(sign);sign.parent=cam;sign.location=(0,6.83,-13.12)
for nm,size,col,zz in [('Warm rounded wooden surround',(12.45,2.42,.18),'woodlight',0),('Cream painted sign face',(12.06,2.09,.09),'ivory',.13)]:
 o=box(nm,(0,0,0),size,col,b=.21,seg=3);o.parent=sign;o.location=(0,0,zz)
for xx in [-5.86,5.86]:
 for yy in [-.76,.76]:
  o=sphere('Sign wooden peg',(0,0,0),(.085,.085,.04),'wood',segments=10,rings=5);o.parent=sign;o.location=(xx,yy,.21)
# A simple sun mark recalls the produce-market emblem in the actual farm.
for i in range(8):
 th=i*math.tau/8;o=box('Sign sun ray',(0,0,0),(.09,.22,.035),'gold2',b=.03);o.parent=sign;o.location=(-5.02+.52*math.cos(th),.0+.52*math.sin(th),.23);o.rotation_euler=(0,0,th-math.pi/2)
o=cyl('Sign sun',(0,0,0),.34,.05,'gold',n=20,b=.015);o.parent=sign;o.location=(-5.02,0,.24)
head=title('햇살 바구니 로고','햇살 바구니',2.30,(.55,6.09,-12.82),(.15,.062,.024));tag=title('밭에서 가게까지','밭에서 가게까지',.49,(0,5.17,-12.80),(.25,.14,.049))
scene.render.resolution_x=1200;scene.render.resolution_y=630;scene.render.filepath=GAME+'/thumb.png'
bpy.ops.wm.save_as_mainfile(filepath=OUT+'/sunbasket-farm-assets.blend',compress=True)
bpy.ops.render.render(write_still=True)
cam.location=(10,-18,17);aim(cam,(0,.6,.6));d.ortho_scale=22.6;sign.location=(0,9.52,-13.12);head.location=(.55,8.87,-12.82);head.data.size=2.20;tag.location=(0,7.95,-12.80);tag.data.size=.44
scene.render.resolution_x=1080;scene.render.resolution_y=1080;scene.render.filepath=GAME+'/square.png';bpy.ops.render.render(write_still=True)
for obj in list(scene.objects):
 if obj.type=='FONT':
  bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.convert(target='MESH')
for f in list(bpy.data.fonts):
 if f.users==0 and f.filepath:bpy.data.fonts.remove(f)
bpy.ops.wm.save_as_mainfile(filepath=OUT+'/sunbasket-farm-assets.blend',compress=True)
print('SUNBASKET_RENDER_DONE',flush=True)
