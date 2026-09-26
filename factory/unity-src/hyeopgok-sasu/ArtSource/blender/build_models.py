"""Original low-poly assets for 협곡 사수. No downloaded assets or textures.

Run with Blender -b --factory-startup --python ArtSource/blender/build_models.py.
All constructor coordinates use Unity axes (x right, y up, z forward), metres.
Export uses one joined, flat shaded, vertex-colour mesh / one material per asset.
COLOR alpha is a team mask: 0 team tint * RGB; 1 fixed RGB.
"""
import bpy, math, json, random, sys
from pathlib import Path
from mathutils import Vector, Matrix

HERE = Path(__file__).resolve().parent
OUT = HERE.parent.parent / 'Resources' / 'HyeopgokSasu' / 'Models'
OUT.mkdir(parents=True, exist_ok=True)
random.seed(29411)

# sRGB palette, encoded into COLOR and retained verbatim in the manifest.
PAL = {
 'team': (1,1,1,0), 'team_dark': (.75,.80,.88,0),
 'steel': (.68,.79,.82,1), 'steel_light': (.89,.94,.89,1),
 'steel_dark': (.22,.32,.36,1), 'skin': (.92,.68,.43,1),
 'leather': (.24,.15,.105,1), 'wood': (.42,.23,.105,1),
 'wood_light': (.69,.43,.21,1), 'wood_gold': (.82,.59,.29,1),
 'gold': (1,.70,.08,1), 'gold_light': (1,.87,.29,1),
 'navy': (.11,.22,.27,1), 'royal': (.06,.32,.68,1),
 'stone': (.66,.69,.63,1), 'stone_light': (.82,.82,.72,1),
 'stone_shadow': (.43,.49,.47,1), 'mortar': (.30,.38,.37,1),
 'red': (.79,.065,.065,1), 'roof': (.64,.21,.11,1),
 'grass': (.06,.64,.36,1), 'grass_light': (.11,.72,.42,1),
 'grass_dark': (.055,.48,.30,1), 'cliff': (.075,.17,.22,1),
 'cliff_light': (.12,.24,.28,1), 'path': (.72,.64,.43,1),
 'path_light': (.78,.69,.47,1), 'pine': (.19,.45,.18,1),
 'pine_light': (.29,.56,.22,1), 'pine_dark': (.11,.34,.16,1),
 'black': (.035,.065,.07,1), 'ivory': (.98,.98,.86,1),
}

def bcoord(p): return (p[0], -p[2], p[1])

class MeshMaker:
 def __init__(self,name):
  self.name=name; self.verts=[]; self.faces=[]; self.colors=[]
 def face(self,pts,color):
  col=PAL[color] if isinstance(color,str) else color
  start=len(self.verts)
  self.verts.extend(bcoord(p) for p in pts)
  self.faces.append(tuple(range(start,start+len(pts))))
  self.colors.extend([col]*len(pts))
 def box(self,center,scale,color,rot=None):
  c=Vector(center); sx,sy,sz=[v*.5 for v in scale]
  pts=[Vector((x*sx,y*sy,z*sz)) for x,y,z in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
  if rot is not None: pts=[rot@p for p in pts]
  pts=[c+p for p in pts]
  for f in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(0,1,5,4),(3,7,6,2)]:
   self.face([pts[i] for i in f],color)
 def beam(self,a,b,width,depth,color):
  a,b=Vector(a),Vector(b); d=b-a
  rot=Vector((0,1,0)).rotation_difference(d).to_matrix()
  self.box((a+b)*.5,(width,d.length,depth),color,rot)
 def cylinder(self,center,radius,depth,color,n=8,radius_top=None):
  x,y,z=center; rt=radius if radius_top is None else radius_top
  a=[(x+radius*math.cos(i*math.tau/n),y-depth*.5,z+radius*math.sin(i*math.tau/n)) for i in range(n)]
  b=[(x+rt*math.cos(i*math.tau/n),y+depth*.5,z+rt*math.sin(i*math.tau/n)) for i in range(n)]
  self.face(a,color)
  if rt>0: self.face(list(reversed(b)),color)
  for i in range(n): self.face([a[i],b[i],b[(i+1)%n],a[(i+1)%n]],color)
 def wedge(self,center,scale,color):
  x,y,z=center; sx,sy,sz=[t*.5 for t in scale]
  a=[(x-sx,y-sy,z-sz),(x+sx,y-sy,z-sz),(x,y+sy,z-sz),(x-sx,y-sy,z+sz),(x+sx,y-sy,z+sz),(x,y+sy,z+sz)]
  for f in [(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)]:self.face([a[i] for i in f],color)
 def poly_extrude(self,poly,y0,y1,top='grass',side='cliff'):
  # x/z counter-clockwise polygons point DOWN in x/y/z coordinates.
  if sum(poly[i][0]*poly[(i+1)%len(poly)][1]-poly[(i+1)%len(poly)][0]*poly[i][1] for i in range(len(poly)))<0:poly=list(reversed(poly))
  self.face([(x,y1,z) for x,z in reversed(poly)],top)
  self.face([(x,y0,z) for x,z in poly],side)
  for i in range(len(poly)):
   a,b=poly[i],poly[(i+1)%len(poly)]
   sc=side
   if side=='cliff' and i%3==1:sc='cliff_light'
   self.face([(a[0],y0,a[1]),(a[0],y1,a[1]),(b[0],y1,b[1]),(b[0],y0,b[1])],sc)
 def object(self):
  me=bpy.data.meshes.new(self.name+'_mesh'); me.from_pydata(self.verts,[],self.faces);me.update()
  ob=bpy.data.objects.new(self.name,me);bpy.context.collection.objects.link(ob)
  attr=me.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER')
  for p in me.polygons:
   p.use_smooth=False
   for li in p.loop_indices:
    # API .color is linear. Convert authored sRGB once here.
    c=self.colors[me.loops[li].vertex_index]
    attr.data[li].color_srgb=c
  me.color_attributes.active_color=attr
  mat=bpy.data.materials.new(self.name+'_VertexPalette');mat.diffuse_color=(.6,.7,.6,1);mat.use_nodes=True
  nodes=mat.node_tree.nodes;links=mat.node_tree.links; bsdf=nodes.get('Principled BSDF')
  col=nodes.new('ShaderNodeVertexColor');col.layer_name='Color'
  mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(.035,.35,.95,1)
  links.new(col.outputs['Color'],mix.inputs[1])
  tint=nodes.new('ShaderNodeMixRGB');links.new(col.outputs['Alpha'],tint.inputs[0]);links.new(mix.outputs[0],tint.inputs[1]);links.new(col.outputs['Color'],tint.inputs[2]);links.new(tint.outputs[0],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.85
  me.materials.append(mat)
  return ob

ASSETS=[]
def emit(m):
 ob=m.object();bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
 # Triangulate by modifier so GPU triangle count is explicit and exported deterministic.
 tri=ob.modifiers.new('FlatTriangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
 file=OUT/(m.name+'.fbx')
 bpy.ops.export_scene.fbx(filepath=str(file),use_selection=True,object_types={'MESH'},global_scale=1.0,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_space_transform=True,use_mesh_modifiers=True,mesh_smooth_type='OFF',use_tspace=False,add_leaf_bones=False,bake_anim=False,path_mode='AUTO',colors_type='SRGB',use_custom_props=False)
 # Source mesh has zero object transform. World coordinates already baked.
 vs=[(v.co.x,v.co.z,-v.co.y) for v in ob.data.vertices]
 info={'id':m.name,'resource':'HyeopgokSasu/Models/'+m.name,'fbx_bytes':file.stat().st_size,'triangles':len(ob.data.polygons),'vertices':len(ob.data.vertices),'bounds_min':[round(min(p[i] for p in vs),4) for i in range(3)],'bounds_max':[round(max(p[i] for p in vs),4) for i in range(3)]}
 ASSETS.append(info);print('MODEL',json.dumps(info))
 ob.hide_render=True;ob.hide_viewport=True
 return ob

def soldier():
 m=MeshMaker('soldier')
 # Broad shoulders, readable squat helmet, tabard, leather boots; only 184 triangles.
 m.box((0,.30,0),(.23,.23,.14),'team')
 m.box((-.066,.135,0),(.072,.17,.095),'team_dark');m.box((.066,.135,0),(.072,.17,.095),'team_dark')
 m.box((-.066,.035,.025),(.083,.07,.14),'leather');m.box((.066,.035,.025),(.083,.07,.14),'leather')
 m.box((0,.275,.076),(.24,.027,.016),'leather')
 m.cylinder((0,.48,0),.101,.13,'skin',n=6)
 m.cylinder((0,.56,-.013),.135,.103,'team',n=6,radius_top=.105)
 m.box((0,.51,.095),(.17,.031,.025),'team_dark')
 # Elbows imply motion without an animated skeleton.
 m.beam((-.138,.37,0),(-.168,.24,.065),.065,.067,'team')
 m.beam((.138,.37,0),(.16,.27,.11),.065,.067,'team')
 # One low-poly shield with colour team mask and steel boss.
 m.box((-.19,.29,.105),(.032,.20,.15),'team_dark')
 # Sword tilted into the march; eight triangles in a diamond spear blade.
 a=(.157,.315,.145);b=(.17,.53,.19);w=.032
 pts=[(a[0]-w,a[1],a[2]),(a[0]+w,a[1],a[2]),(a[0],a[1],a[2]-.016),(a[0],a[1],a[2]+.016),b]
 for f in [(0,2,4),(2,1,4),(1,3,4),(3,0,4),(0,3,1,2)]:m.face([pts[i] for i in f],'steel_light')
 return emit(m)

def king():
 m=MeshMaker('king')
 m.box((-.105,.075,0),(.15,.15,.24),'leather');m.box((.105,.075,0),(.15,.15,.24),'leather')
 m.box((0,.22,0),(.30,.21,.19),'ivory')
 m.cylinder((0,.46,0),.245,.37,'royal',n=6,radius_top=.20)
 m.box((0,.41,.18),(.35,.05,.025),'gold')
 m.box((0,.51,.18),(.11,.15,.025),'gold_light')
 m.beam((-.22,.58,0),(-.27,.31,.1),.15,.15,'steel_light')
 m.beam((.22,.58,0),(.28,.36,.10),.15,.15,'steel_light')
 m.cylinder((0,.75,0),.175,.22,'skin',n=8)
 m.box((0,.7,.145),(.26,.085,.095),'leather')
 m.cylinder((0,.87,0),.196,.075,'gold',n=8)
 for i in range(6):
  angle=i*math.tau/6
  m.cylinder((math.cos(angle)*.163,.954,math.sin(angle)*.163),.055,.10,'gold_light',n=4,radius_top=0)
 m.box((0,.885,.202),(.064,.074,.035),'royal')
 # Hero cape is a deliberately separate folded silhouette within the same mesh.
 m.face([(-.2,.64,-.17),(.2,.64,-.17),(.29,.16,-.29),(0,.19,-.35),(-.29,.16,-.29)],'royal')
 m.face([(-.2,.64,-.17),(-.29,.16,-.29),(0,.19,-.35),(0,.59,-.2)],'navy')
 return emit(m)

def tower_base(m=None):
 own=m is None;m=m or MeshMaker('tower_base')
 m.box((0,.13,0),(1.12,.26,1.10),'stone_shadow')
 m.box((0,.32,0),(.96,.15,.97),'stone_light')
 m.box((0,.92,0),(.78,1.10,.78),'stone')
 m.box((0,1.45,0),(1.10,.25,1.06),'stone_light')
 for x in [-.39,.39]:
  for z in [-.39,.39]: m.box((x,1.65,z),(.31,.23,.3),'stone')
 m.box((0,1.60,0),(.72,.10,.72),'mortar')
 # Stone shield relief viewed from front.
 m.face(list(reversed([(-.18,1.13,.402),(.18,1.13,.402),(.15,.91,.402),(0,.83,.402),(-.15,.91,.402)])),'stone_shadow')
 m.face(list(reversed([(-.12,1.1,.406),(.12,1.1,.406),(.1,.96,.406),(0,.90,.406),(-.1,.96,.406)])),'stone_light')
 # Two mortar seams prevent the tower reading as one featureless cube.
 m.box((0,.65,.398),(.77,.022,.012),'stone_shadow')
 m.box((-.13,.96,.398),(.022,.56,.012),'stone_shadow')
 return emit(m) if own else m

def crossbow(m=None,y=0):
 own=m is None;m=m or MeshMaker('crossbow')
 def p(x,h,z):return (x,h+y,z)
 m.cylinder(p(0,.13,0),.19,.26,'wood',n=8)
 m.box(p(0,.26,0),(.24,.12,.82),'wood_light')
 m.box(p(0,.33,.05),(.09,.045,.84),'steel_dark')
 m.beam(p(-.57,.26,.17),p(-.29,.30,.4),.12,.10,'wood_gold')
 m.beam(p(-.29,.30,.4),p(0,.28,.29),.12,.10,'wood_light')
 m.beam(p(.57,.26,.17),p(.29,.30,.4),.12,.10,'wood_gold')
 m.beam(p(.29,.30,.4),p(0,.28,.29),.12,.10,'wood_light')
 m.beam(p(-.57,.27,.17),p(0,.33,-.25),.016,.016,'ivory')
 m.beam(p(.57,.27,.17),p(0,.33,-.25),.016,.016,'ivory')
 m.beam(p(0,.37,-.32),p(0,.37,.54),.027,.027,'wood_gold')
 m.face([p(-.10,.37,.40),p(0,.37,.68),p(.10,.37,.40)],'steel_light')
 m.face([p(.10,.37,.40),p(0,.37,.68),p(-.10,.37,.40)],'steel_light')
 m.face([p(0,.37,.40),p(0,.43,.4),p(0,.37,.68)],'steel')
 m.box(p(0,.325,-.24),(.21,.09,.18),'royal')
 for x in [-.40,.40]:m.box(p(x,.3,.29),(.07,.16,.18),'gold')
 return emit(m) if own else m

def sword_emblem(m,cx,cy,cz,scale=1):
 for s in [-1,1]:
  a=(cx-s*.30*scale,cy-.28*scale,cz);b=(cx+s*.23*scale,cy+.30*scale,cz)
  m.beam(a,b,.10*scale,.048*scale,'ivory')
  m.beam((cx-s*.24*scale,cy-.22*scale,cz+.009),(cx-s*.42*scale,cy-.41*scale,cz+.009),.075*scale,.065*scale,'gold')
  m.beam((cx-s*.17*scale,cy-.31*scale,cz+.01),(cx-s*.39*scale,cy-.13*scale,cz+.01),.065*scale,.068*scale,'gold_light')

def barracks():
 m=MeshMaker('barracks')
 m.box((0,.07,0),(1.40,.14,1.22),'stone_shadow')
 m.box((0,.56,0),(1.2,.95,.94),'wood')
 m.box((0,.42,.479),(.44,.72,.026),'black')
 for x in [-.53,.53]:m.box((x,.58,.51),(.13,1.14,.14),'wood_gold')
 m.wedge((0,1.26,0),(1.52,.67,1.35),'wood_light')
 m.box((0,1.56,0),(.13,.10,1.51),'wood')
 for z in [-.45,-.15,.15,.45]:m.box((0,1.60,z),(.22,.07,.025),'wood_gold')
 m.box((0,1.21,.70),(1.07,.84,.10),'wood')
 m.box((0,1.21,.758),(.93,.72,.03),'wood_light')
 sword_emblem(m,0,1.22,.79,.9)
 for x in [-.65,.65]:
  for z in [-.38,.1,.42]:m.box((x,.53,z),(.06,.70,.045),'wood_light')
 return emit(m)

def gate(enemy):
 m=MeshMaker('enemy_gate' if enemy else 'castle_gate')
 for x in [-.68,.68]:
  m.box((x,.11,0),(.68,.22,.82),'stone_shadow')
  m.box((x,.93,0),(.56,1.62,.62),'stone')
  m.box((x,1.77,0),(.72,.20,.78),'stone_light')
  for dx in [-.24,.24]:
   for dz in [-.24,.24]:m.box((x+dx,1.96,dz),(.20,.23,.20),'stone_light')
  m.box((x,1.04,.327),(.34,.94,.025),'red' if enemy else 'royal')
  m.box((x,1.05,.343),(.26,.79,.015),'red' if enemy else 'royal')
  for dx in [-.165,.165]:m.box((x+dx,1.02,.355),(.025,.94,.025),'gold')
 m.box((0,1.61,0),(.98,.27,.50),'stone_shadow')
 m.box((0,1.76,0),(1.04,.10,.58),'stone_light')
 # Recessed entrance and open ground below arch.
 m.box((0,.70,-.20),(.83,1.39,.08),'wood')
 for x in [-.3,-.15,0,.15,.3]:m.box((x,.68,-.15),(.027,1.23,.06),'wood_gold')
 for y in [.35,1.02]:m.box((0,y,-.11),(.85,.075,.065),'steel_dark')
 if enemy:
  m.box((0,2.26,0),(.12,1.0,.12),'wood')
  m.box((0,2.50,.015),(1.04,.72,.11),'wood')
  m.box((0,2.50,.08),(.92,.62,.025),'red')
  sword_emblem(m,0,2.51,.11,.65)
 else:
  m.wedge((0,2.11,0),(1.0,.57,.72),'roof')
  m.box((0,1.70,.317),(.29,.10,.035),'gold')
 return emit(m)

def tree():
 m=MeshMaker('tree')
 m.cylinder((0,.48,0),.10,.96,'wood_light',n=6,radius_top=.065)
 m.cylinder((0,.89,0),.48,.82,'pine',n=6,radius_top=.15)
 m.cylinder((.025,1.3,0),.38,.76,'pine_light',n=6,radius_top=.06)
 m.cylinder((.01,1.67,0),.24,.64,'pine',n=6,radius_top=0)
 return emit(m)

def rock():
 m=MeshMaker('rock')
 rings=[]
 for y,r in [(0,.51),(.28,.59),(.64,.33)]:
  rings.append([(math.cos(i*math.tau/7)*r*(.87+random.random()*.23),y,math.sin(i*math.tau/7)*r) for i in range(7)])
 m.face(rings[0],'cliff')
 for j in range(2):
  for i in range(7):m.face([rings[j][i],rings[j+1][i],rings[j+1][(i+1)%7],rings[j][(i+1)%7]],'cliff_light' if i%2 else 'cliff')
 m.face(list(reversed(rings[2])),'grass_dark')
 return emit(m)

def catmull(points,steps=8):
 out=[]
 pp=[points[0]]+points+[points[-1]]
 for j in range(1,len(pp)-2):
  a,b,c,d=[Vector(p) for p in pp[j-1:j+3]]
  for i in range(steps):
   t=i/steps;out.append(tuple(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t)))
 out.append(points[-1]);return out

PATH_POINTS=[(4.3,9.0),(4.3,5.0),(4.15,1.0),(3.7,-3.5),(2.7,-5.9),(.2,-7.1),(-2.6,-6.6),(-4.0,-4.8),(-4.1,-1.0)]
PLATEAU=[(-3.05,-4.80),(-1.9,-5.45),(.10,-5.60),(1.75,-4.85),(2.35,-3.0),(2.70,0.10),(2.78,3.2),(2.58,5.85),(.70,7.15),(-1.75,6.85),(-3.30,5.3),(-3.40,1.0)]

def terrain():
 m=MeshMaker('terrain')
 m.poly_extrude([(-30,-30),(30,-30),(30,30),(-30,30)],-2,-1.52,'grass_dark','cliff')
 m.poly_extrude(PLATEAU,-1.5,1.20,'grass','cliff')
 # Broad flowing yellow canyon floor. Separate facets at curves; no expensive textures.
 m.poly_extrude([(2.05,6.35),(6.45,6.35),(6.45,11.0),(2.05,11.0)],-1.5,.245,'path','cliff')
 # Left approach joins the defence endpoint z=-1 to the barracks doorway
 # and the castle behind it; keeps both buildings outside the high plateau.
 m.poly_extrude([(-5.5,-1.15),(-3.25,-1.15),(-3.25,3.0),(-5.5,3.0)],-1.5,.245,'path','cliff')
 path=catmull(PATH_POINTS,7);left=[];right=[]
 for i,p in enumerate(path):
  before=Vector(path[max(0,i-1)]);after=Vector(path[min(len(path)-1,i+1)]);d=(after-before).normalized();n=Vector((-d[1],d[0]));v=Vector(p)
  left.append(tuple(v+n*1.02));right.append(tuple(v-n*1.02))
 for i in range(len(path)-1):
  a,b,c,d=left[i],right[i],right[i+1],left[i+1]
  m.face([(d[0],.25,d[1]),(c[0],.25,c[1]),(b[0],.25,b[1]),(a[0],.25,a[1])],'path' if i%7 else 'path_light')
  for side in [left,right]:
   a,b=side[i],side[i+1]
   m.face([(a[0],-.60,a[1]),(b[0],-.60,b[1]),(b[0],.25,b[1]),(a[0],.25,a[1])],'cliff_light')
 # Exterior bank tightly hugs the ochre path on its outside and gives the
 # foreground its tall navy silhouette as in the supplied reference.
 outer=[]
 for i,p in enumerate(path):
  before=Vector(path[max(0,i-1)]);after=Vector(path[min(len(path)-1,i+1)]);d=(after-before).normalized();n=Vector((-d[1],d[0]));v=Vector(p)
  outer.append(tuple(v+n*1.65))
 for i in range(len(path)-1):
  # With clockwise traversal, left lies outside of the U.
  a,b,c,d=left[i],left[i+1],outer[i+1],outer[i]
  m.face([(d[0],.48,d[1]),(c[0],.48,c[1]),(b[0],.48,b[1]),(a[0],.48,a[1])],'grass_light')
  m.face([(d[0],-1.52,d[1]),(c[0],-1.52,c[1]),(c[0],.48,c[1]),(d[0],.48,d[1])],'cliff')
 # Background cliff masses frame the two entry gates without covering the map.
 m.poly_extrude([(-10,4),(-5.7,4),(-5.65,5.25),(-3.5,7),(-.5,9),(-1,14),(-10,14)],-1.5,3.1,'grass_dark','cliff')
 m.poly_extrude([(7.1,5.0),(9,5),(13,9),(13,16),(6.9,16)],-1.5,1.2,'grass_dark','cliff')
 return emit(m)

def bolt():
 m=MeshMaker('bolt')
 m.box((0,0,0),(.035,.035,.65),'wood_gold')
 m.face([(-.10,0,.20),(0,0,.43),(.10,0,.20)],'steel_light')
 m.face([(0,-.10,.20),(0,.10,.20),(0,0,.43)],'steel')
 m.face([(-.10,0,-.34),(0,0,-.15),(.10,0,-.34)],'ivory')
 return emit(m)

def make_preview():
 # World preview is a non-game provenance/QA artifact. Mirror preview X only
 # to match Unity's left-handed camera projection; exported meshes are untouched.
 for ob in bpy.data.objects:ob.hide_viewport=False
 for ob in bpy.data.objects:
  if ob.type=='MESH':ob.hide_render=True
 terrain_ob=bpy.data.objects.get('terrain');terrain_ob.hide_render=False;terrain_ob.scale.x=-1
 def copy_asset(name,x,y,z,scale=1,turn=0):
  src=bpy.data.objects[name];ob=src.copy();ob.data=src.data;bpy.context.collection.objects.link(ob);ob.hide_render=False;ob.location=bcoord((-x,y,z));ob.scale=(-scale,scale,scale);ob.rotation_euler.z=math.radians(-turn);return ob
 copy_asset('barracks',-4.1,.25,-.30,.85,180)
 copy_asset('castle_gate',-4.4,.25,2.0,.90,180)
 copy_asset('enemy_gate',3.1,.25,8.8,.9,180);copy_asset('enemy_gate',5.35,.25,8.8,.9,180)
 copy_asset('tower',1.45,1.2,-2.5,.95,110);copy_asset('tower',1.40,1.2,1.05,.95,100)
 copy_asset('king',-1.2,1.2,-1.2,1.1,150)
 for x,z,s in [(-5.7,-7,.8),(-6.5,-6,.8),(6,-4,.9),(6.8,-2,.8),(7.8,1,1),(-5,4,1),(-5.8,5.3,.8),(-1,9,1.2),(1,10,.9),(7,8,1)]:copy_asset('tree',x,-1.4,z,s)
 for x,z,s in [(5.8,-6,.7),(7.2,3,.6),(-5,7,1.6),(-7,-4,.6)]:copy_asset('rock',x,-1.5,z,s)
 for i in range(180):
  j=i%20;t=j/19
  x=3.95-.12*t+(i//20-4)*.155;z=6.5-t*10.6
  ob=copy_asset('soldier',x,.25,z,.95,180)
  if i==0:
   enemy_mat=ob.data.materials[0].copy();enemy_mat.name='preview_enemy'
   # Change preview team multiplier for the army only; not part of any exported FBX.
   for no in enemy_mat.node_tree.nodes:
    if no.bl_idname=='ShaderNodeMixRGB' and no.blend_type=='MULTIPLY':no.inputs[2].default_value=(.95,.025,.012,1)
  ob.data=ob.data.copy();ob.data.materials.clear();ob.data.materials.append(enemy_mat)
 for i in range(56):
  t=(i//4)/13;copy_asset('soldier',-4.0+(i%4-1.5)*.15,.25,-2.6-t*3.0,1,180)
 world=bpy.context.scene.world;world.color=(.3,.3,.3);world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.38,.50,.56,1);world.node_tree.nodes['Background'].inputs[1].default_value=.65
 sun=bpy.data.lights.new('Sun','SUN');sun.energy=2.0;sun.angle=.10;light=bpy.data.objects.new('Sun',sun);bpy.context.collection.objects.link(light);light.rotation_euler=(math.radians(25),math.radians(-25),math.radians(-25))
 camd=bpy.data.cameras.new('Camera');cam=bpy.data.objects.new('Camera',camd);bpy.context.collection.objects.link(cam);target=Vector(bcoord((0,0,0)));cam.location=bcoord((4.0,20,-18));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();camd.type='ORTHO';camd.ortho_scale=21.5;bpy.context.scene.camera=cam
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=820;scene.render.resolution_y=1180;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(HERE/'world-preview.png');scene.render.film_transparent=False
 bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'hyeopgok-assets.blend'))
 bpy.ops.render.render(write_still=True)

bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
if '--only-terrain' in sys.argv:
 terrain()
 manifest_path=HERE/'model-manifest.json'
 manifest=json.loads(manifest_path.read_text())
 manifest['assets']=[ASSETS[0] if item['id']=='terrain' else item for item in manifest['assets']]
 manifest['left_approach']={'x':[-5.5,-3.25],'z':[-1.15,3.0],'top_y':.245,'castle':[ -4.4,.25,2.0],'castle_scale':.9,'barracks':[-4.1,.25,-.30],'barracks_scale':.85,'building_yaw':180}
 manifest_path.write_text(json.dumps(manifest,indent=2,ensure_ascii=False)+'\n')
 print('Terrain-only export complete')
 sys.exit(0)
soldier();king();tower_base();crossbow();m=MeshMaker('tower');tower_base(m);crossbow(m,1.62);emit(m)
barracks();gate(False);gate(True);tree();rock();terrain();bolt()
(HERE/'model-manifest.json').write_text(json.dumps({'license':'Original procedural art created for this game; no third-party assets','axes':'Unity x right / y up / z forward. FBX -Z forward +Y up bake transforms.','color':'COLOR sRGB authored values; alpha0 teamColor * RGB, alpha1 fixed RGB','palette':PAL,'path_control_points':PATH_POINTS,'plateau_polygon':PLATEAU,'left_approach':{'x':[-5.5,-3.25],'z':[-1.15,3.0],'top_y':.245,'castle':[-4.4,.25,2.0],'castle_scale':.9,'barracks':[-4.1,.25,-.30],'barracks_scale':.85,'building_yaw':180},'assets':ASSETS},indent=2,ensure_ascii=False)+'\n')
make_preview()
