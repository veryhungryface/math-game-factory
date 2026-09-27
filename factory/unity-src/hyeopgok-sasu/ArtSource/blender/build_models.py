"""Original low-poly assets for 협곡 사수. No downloaded assets or textures.

Run with Blender -b --factory-startup --python ArtSource/blender/build_models.py.
All constructor coordinates use Unity axes (x right, y up, z forward), metres.
Export uses one joined, flat shaded, vertex-colour mesh / one material per asset.
COLOR RGB is base colour, alpha is ray-baked AO. UV0.x is team mask; UV0.y=1.
"""
import bpy, bmesh, math, json, random, sys
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.geometry import tessellate_polygon

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
 'gold': (.949,.718,.020,1), 'gold_light': (1,.87,.29,1), 'gold_shadow': (.867,.604,.118,1),
 'navy': (.11,.22,.27,1), 'royal': (.047,.451,.835,1),
 'stone': (.66,.69,.63,1), 'stone_light': (.82,.82,.72,1),
 'stone_shadow': (.43,.49,.47,1), 'mortar': (.30,.38,.37,1),
 'red': (.816,.024,.047,1), 'roof': (.025,.286,.58,1),
 'grass': (.176,.667,.455,1), 'grass_light': (.184,.706,.478,1),
 'grass_dark': (.125,.561,.380,1), 'cliff': (.075,.17,.22,1),
 'cliff_light': (.12,.24,.28,1), 'path': (.784,.710,.612,1),
 'path_light': (.812,.761,.682,1), 'pine': (.122,.353,.29,1),
 'pine_light': (.18,.427,.165,1), 'pine_dark': (.085,.29,.22,1),
 'leaf': (.21,.47,.31,1), 'leaf_light': (.29,.56,.35,1),
 'leaf_dark': (.16,.43,.20,1), 'cliff_mid': (.149,.224,.271,1),
 'cliff_rim': (.132,.245,.286,1), 'grass_edge': (.24,.706,.47,1),
 'black': (.035,.065,.07,1), 'ivory': (.98,.98,.91,1),
 'red_shadow': (.549,.102,.125,1), 'straw': (.72,.54,.29,1), 'straw_light': (.88,.70,.40,1),
 'plaster': (.82,.78,.66,1), 'giant_skin': (.88,.53,.48,1), 'blade_glow': (1,.15,.34,1),
 'sandstone': (.914,.788,.690,1), 'sandstone_side': (.514,.533,.580,1), 'water': (.27,.59,.72,1),
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
 def capsule_box(self,center,scale,color,rot=None):
  # Three six-sided sections keep the original box envelope while replacing
  # sharp hands/boots with a soft, weighted toy silhouette (32 triangles).
  c=Vector(center);sx,sy,sz=[v*.5 for v in scale];rows=[]
  for h,r in [(-1,.68),(0,1),(1,.68)]:
   row=[]
   for i in range(6):
    a=i*math.tau/6;p=Vector((math.cos(a)*sx*r,h*sy,math.sin(a)*sz*r/.866025403784))
    row.append(c+(rot@p if rot is not None else p))
   rows.append(row)
  self.face(rows[0],color);self.face(list(reversed(rows[-1])),color)
  for j in range(2):
   for i in range(6):self.face([rows[j][i],rows[j+1][i],rows[j+1][(i+1)%6],rows[j][(i+1)%6]],color)
 def capsule_beam(self,a,b,width,depth,color):
  a,b=Vector(a),Vector(b);d=b-a;rot=Vector((0,1,0)).rotation_difference(d).to_matrix()
  self.capsule_box((a+b)*.5,(width,d.length,depth),color,rot)
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
 def rings(self,center,rings,color,n=8,aspect=(1,1),phase=0):
  x,y,z=center
  rows=[[(x+math.cos(i*math.tau/n+phase)*r*aspect[0],y+h,z+math.sin(i*math.tau/n+phase)*r*aspect[1]) for i in range(n)] for h,r in rings]
  self.face(rows[0],color)
  for j in range(len(rows)-1):
   for i in range(n):self.face([rows[j][i],rows[j+1][i],rows[j+1][(i+1)%n],rows[j][(i+1)%n]],color)
  self.face(list(reversed(rows[-1])),color)
 def foliage(self,center,scale,color='leaf',seed=0):
  rng=random.Random(seed);x,y,z=center;sx,sy,sz=scale;n=7
  rows=[]
  for j,(h,r) in enumerate([(-.70,.38),(-.26,.92),(.27,1),(.72,.61),(.94,.19)]):
   rows.append([(x+math.cos(i*math.tau/n+.11*j)*r*sx*(.90+rng.random()*.17),y+h*sy,z+math.sin(i*math.tau/n+.11*j)*r*sz) for i in range(n)])
  self.face(rows[0],'leaf_dark')
  base=PAL[color]
  for j in range(4):
   for i in range(n):
    v=.90+j*.033+rng.random()*.06
    c=tuple(min(1,t*v) for t in base[:3])+(1,)
    self.face([rows[j][i],rows[j+1][i],rows[j+1][(i+1)%n],rows[j][(i+1)%n]],c)
  self.face(list(reversed(rows[-1])),'leaf_light')
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
  mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(.047,.435,.827,1)
  links.new(col.outputs['Color'],mix.inputs[1])
  uv=nodes.new('ShaderNodeTexCoord');separate=nodes.new('ShaderNodeSeparateXYZ');links.new(uv.outputs['UV'],separate.inputs[0])
  tint=nodes.new('ShaderNodeMixRGB');links.new(separate.outputs['X'],tint.inputs[0]);links.new(mix.outputs[0],tint.inputs[1]);links.new(col.outputs['Color'],tint.inputs[2])
  ao=nodes.new('ShaderNodeMixRGB');ao.name='Vertex AO x palette';ao.blend_type='MULTIPLY';ao.inputs[0].default_value=1;links.new(tint.outputs[0],ao.inputs[1]);links.new(col.outputs['Alpha'],ao.inputs[2]);links.new(ao.outputs[0],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.85
  me.materials.append(mat)
  return ob

exec(compile((HERE/'ao_bake.py').read_text(),str(HERE/'ao_bake.py'),'exec'))
ASSETS=[]
def emit(m):
 ob=m.object();bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
 # Triangulate by modifier so GPU triangle count is explicit and exported deterministic.
 tri=ob.modifiers.new('FlatTriangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
 ao_stats=bake_vertex_ao(ob)
 file=OUT/(m.name+'.fbx')
 bpy.ops.export_scene.fbx(filepath=str(file),use_selection=True,object_types={'MESH'},global_scale=1.0,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_space_transform=True,use_mesh_modifiers=True,mesh_smooth_type='OFF',use_tspace=False,add_leaf_bones=False,bake_anim=False,path_mode='AUTO',colors_type='SRGB',use_custom_props=False)
 # Source mesh has zero object transform. World coordinates already baked.
 vs=[(v.co.x,v.co.z,-v.co.y) for v in ob.data.vertices]
 info={'id':m.name,'resource':'HyeopgokSasu/Models/'+m.name,'fbx_bytes':file.stat().st_size,'triangles':len(ob.data.polygons),'vertices':len(ob.data.vertices),'ao_bake':ao_stats,'bounds_min':[round(min(p[i] for p in vs),4) for i in range(3)],'bounds_max':[round(max(p[i] for p in vs),4) for i in range(3)]}
 ASSETS.append(info);print('MODEL',json.dumps(info))
 ob.hide_render=True;ob.hide_viewport=True
 return ob

def soldier(blue=False):
 m=MeshMaker('soldier_blue' if blue else 'soldier')
 # Each army is one shared mesh: broader silhouettes, still under 250 triangles.
 m.box((0,.30,0),(.25,.23,.16),'team')
 m.box((-.066,.135,0),(.072,.17,.095),'team_dark');m.box((.066,.135,0),(.072,.17,.095),'team_dark')
 m.box((-.066,.035,.025),(.083,.07,.14),'leather');m.box((.066,.035,.025),(.083,.07,.14),'leather')
 m.box((0,.275,.076),(.24,.027,.016),'leather')
 m.cylinder((0,.475,.007),.101,.12,'skin',n=6)
 if blue:
  # Octagonal bevels on a broad square helmet; the bright brow survives minification.
  m.rings((0,0,-.015),[(.46,.182),(.61,.182),(.678,.134)],'team',n=4,aspect=(1,.91),phase=math.pi*.25)
  m.box((0,.528,.104),(.224,.045,.025),'team_dark')
  m.box((0,.553,.112),(.195,.022,.018),'steel')
 else:
  # Low domed round helmets, with a darker continuous rim above the face.
  m.rings((0,0,-.01),[(.46,.169),(.535,.180),(.637,.133),(.68,.049)],'team',n=8,aspect=(1,.94))
  m.box((0,.51,.108),(.198,.029,.027),'team_dark')
 # Two quiet visor/eye slots sit under the helmet: four triangles total.
 for eye in [-1,1]:
  x=eye*.049;m.face([(x-.016,.431,.104),(x+.016,.431,.104),(x+.016,.453,.104),(x-.016,.453,.104)],'black')
 # Elbows imply motion without an animated skeleton.
 m.beam((-.138,.37,0),(-.168,.24,.065),.065,.067,'team')
 m.beam((.138,.37,0),(.16,.27,.11),.065,.067,'team')
 # Face-on shields make a readable silhouette at the game's small army scale.
 if blue:
  shield=[(-.288,.388,.147),(-.108,.388,.147),(-.108,.218,.147),(-.196,.151,.147),(-.288,.218,.147)]
  m.face(shield,'team_dark');m.face([(x,y,z+.026) for x,y,z in reversed(shield)],'team')
  for i in range(5):
   a,b=shield[i],shield[(i+1)%5];m.face([a,b,(b[0],b[1],b[2]+.026),(a[0],a[1],a[2]+.026)],'steel')
  m.box((-.196,.287,.184),(.036,.148,.018),'steel_light')
 else:
  cx,cy,cz=-.188,.292,.154
  shield=[(cx+math.cos(i*math.tau/8)*.094,cy+math.sin(i*math.tau/8)*.105,cz) for i in range(8)]
  m.face(shield,'team_dark');m.face(list(reversed(shield)),'team_dark')
  m.box((cx,cy,cz+.012),(.037,.055,.022),'steel')
 # Sword tilted into the march; eight triangles in a diamond spear blade.
 a=(.157,.315,.145);b=(.17,.53,.19);w=.032
 pts=[(a[0]-w,a[1],a[2]),(a[0]+w,a[1],a[2]),(a[0],a[1],a[2]-.016),(a[0],a[1],a[2]+.016),b]
 for f in [(0,2,4),(2,1,4),(1,3,4),(3,0,4),(0,3,1,2)]:m.face([pts[i] for i in f],'steel_light')
 return emit(m)

def king():
 m=MeshMaker('king')
 m.capsule_box((-.105,.075,0),(.15,.15,.24),'leather');m.capsule_box((.105,.075,0),(.15,.15,.24),'leather')
 m.box((0,.22,0),(.30,.21,.19),'ivory')
 m.cylinder((0,.46,0),.245,.37,'royal',n=6,radius_top=.20)
 m.box((0,.41,.18),(.35,.05,.025),'gold')
 m.box((0,.51,.18),(.11,.15,.025),'gold_light')
 m.capsule_beam((-.22,.58,0),(-.27,.31,.1),.15,.15,'steel_light')
 m.capsule_beam((.22,.58,0),(.28,.36,.10),.15,.15,'steel_light')
 for x in [-.224,.224]:m.capsule_box((x,.556,.018),(.205,.16,.205),'steel_light')
 m.capsule_box((-.2739,.31,.10),(.20,.15,.17),'steel_light')
 m.capsule_box((.2799,.36,.10),(.20,.15,.17),'steel_light')
 m.cylinder((0,.75,0),.175,.22,'skin',n=8)
 m.capsule_box((0,.7,.145),(.26,.085,.095),'leather')
 # Original survey-king silhouette: ivory sallet and a single brass compass fin,
 # not the reference's six-point gold crown + plain royal mantle.
 m.rings((0,0,0),[(.80,.202),(.89,.202),(.952,.184),(.995,.11)],'steel_light',n=8)
 m.box((0,.858,.180),(.30,.045,.040),'navy')
 m.wedge((0,1.01,-.02),(.075,.19,.25),'gold')
 m.box((0,.94,.183),(.069,.073,.03),'royal')
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
 m.cylinder((0,.49,0),.09,.98,'wood_light',n=5,radius_top=.055)
 m.rings((0,0,0),[(.48,.23),(.61,.52),(.87,.42),(1.35,.05)],'pine',n=7,aspect=(1,.89))
 m.rings((.025,0,0),[(1.02,.22),(1.12,.43),(1.48,.27),(1.80,.02)],'pine_light',n=7,aspect=(1,.91))
 m.rings((0,0,0),[(1.52,.19),(1.65,.29),(2.07,0)],'pine',n=7,aspect=(1,.87))
 return emit(m)

def broadleaf():
 m=MeshMaker('tree_broadleaf')
 m.cylinder((0,.52,0),.115,1.04,'wood_light',n=6,radius_top=.065)
 m.beam((0,.62,0),(-.32,1.04,.04),.09,.09,'wood')
 m.beam((0,.74,0),(.28,1.12,.04),.075,.075,'wood')
 m.foliage((-.25,1.04,.03),(.53,.57,.50),'leaf',21)
 m.foliage((.24,1.25,.06),(.51,.58,.52),'leaf',67)
 m.foliage((-.04,1.57,-.03),(.51,.51,.49),'leaf_light',11)
 return emit(m)

def rock():
 m=MeshMaker('rock')
 rings=[]
 for y,r in [(0,.51),(.28,.59),(.64,.33)]:
  rings.append([(math.cos(i*math.tau/7)*r*(.87+random.random()*.23),y,math.sin(i*math.tau/7)*r) for i in range(7)])
 m.face(rings[0],'cliff')
 for j in range(2):
  for i in range(7):m.face([rings[j][i],rings[j+1][i],rings[j+1][(i+1)%7],rings[j][(i+1)%7]],'cliff_light' if i%2 else 'cliff')
 m.face(list(reversed(rings[2])),'cliff_rim')
 # One moss shoulder and a low chipped facet, baked in the same static mesh.
 m.face([rings[2][0],rings[2][1],rings[2][2],(.12,.66,.05)],'grass_dark')
 m.poly_extrude([(-.42,-.23),(-.15,-.33),(-.08,-.16),(-.28,-.07)],.0,.21,'cliff_rim','cliff')
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

def tint(base,value):
 c=PAL[base] if isinstance(base,str) else base
 return tuple(max(0,min(1,v*value)) for v in c[:3])+(c[3],)

def ground_top(m,poly,y,color,depth=2,variation=1):
 # Small triangulated colour facets remove flat slabs without texture downloads.
 points=[Vector((x,z,0)) for x,z in poly]
 triangles=tessellate_polygon([points])
 def face(a,b,c,level):
  if level:
   ab=(a+b)*.5;bc=(b+c)*.5;ca=(c+a)*.5
   for tri in [(a,ab,ca),(ab,b,bc),(ca,bc,c),(ab,bc,ca)]:face(*tri,level-1)
  else:
   pts=[(p.x,y,p.y) for p in (a,b,c)]
   if (b-a).cross(c-a).z>0:pts.reverse()
   m.face(pts,color)
   # Shared positions receive identical values across all neighbouring faces:
   # continuous meadow mottling instead of one flat tint per large triangle.
   for j,p in enumerate(pts):
    v=1+variation*(.030*math.sin(p[0]*.73+p[2]*.31)+.020*math.sin(p[2]*1.37-p[0]*.84))
    m.colors[-3+j]=tint(color,v)
 for tri in triangles:face(*[points[p] if isinstance(p,int) else p for p in tri],depth)

def cliff_bank(m,poly,y0,y1,top='grass',depth=2,variation=1):
 # Exact gameplay polygon is the upper perimeter. Only the decorative lower
 # rings retreat into the cliff, so the fixed road/pad clearance cannot shrink.
 if sum(poly[i][0]*poly[(i+1)%len(poly)][1]-poly[(i+1)%len(poly)][0]*poly[i][1] for i in range(len(poly)))<0:poly=list(reversed(poly))
 centre=Vector((sum(p[0] for p in poly)/len(poly),sum(p[1] for p in poly)/len(poly)))
 edge=[]
 for i,a in enumerate(poly):
  b=poly[(i+1)%len(poly)];av,bv=Vector(a),Vector(b);n=max(1,math.ceil((bv-av).length/1.05))
  for j in range(n):edge.append(av.lerp(bv,j/n))
 # Broad broken faces use staggered heights and inward shoulders; no repeated
 # horizontal colour rings. The final edge stays exactly on the logical top.
 profile=[(y0,.32),(y0+(y1-y0)*.43,.09),(y1-.09,.026),(y1,0)]
 rings=[]
 for j,(height,inset) in enumerate(profile):
  row=[]
  for i,p in enumerate(edge):
   rough=.68+.55*(.5+.5*math.sin(i*1.63+j*.83))
   q=p+(centre-p).normalized()*inset*rough
   h=height if j in (0,len(profile)-1) else height+(y1-y0)*.12*math.sin(i*1.49+j*.63)
   if j==len(profile)-2:h=min(y1-.038,h)
   row.append((q.x,h,q.y))
  rings.append(row)
 for j in range(len(rings)-1):
  for i in range(len(edge)):
   k=(i+1)%len(edge)
   if j==len(rings)-2:
    col=tint('grass_edge',.96+.025*math.sin(i*1.41))
   else:
    base=PAL['cliff_mid'];value=.94+.16*(.5+.5*math.sin(i*1.23+j*.73))
    col=tuple(v*value for v in base[:3])+(1,)
   q=[rings[j][i],rings[j+1][i],rings[j+1][k],rings[j][k]]
   # Triangular chips alternate diagonals; lower points retreat into the cliff.
   if i%2:
    m.face([q[0],q[1],q[3]],col);m.face([q[1],q[2],q[3]],tint(col,1.025))
   else:
    m.face([q[0],q[1],q[2]],col);m.face([q[0],q[2],q[3]],tint(col,.975))
 ground_top(m,poly,y1,top,depth,variation)

def convex_boulder(m,x,z,w,d,bottom,top,seed):
 # A convex hull over offset, rotated, uneven shoulders produces irregular
 # fractured planes. No bands or repeated concentric rock cylinders remain.
 rng=random.Random(seed);height=top-bottom;n=8+(seed%3);points=[]
 phase=rng.uniform(-.5,.5);lean=rng.uniform(-.14,.14)
 for j,(level,radius) in enumerate([(0,.61),(.23,1),(.72,.91),(1,.54)]):
  for i in range(n):
   angle=phase+i*math.tau/n+j*.23+rng.uniform(-.11,.11)
   r=radius*rng.uniform(.88,1.12)
   yy=bottom if j==0 else bottom+height*(level+rng.uniform(-.065,.025))
   points.append((x+math.cos(angle)*w*.5*r+lean*w*level,
                  min(top,yy),z+math.sin(angle)*d*.5*r+math.sin(seed)*level*d*.055))
 bm=bmesh.new()
 for point in points:bm.verts.new(point)
 bmesh.ops.convex_hull(bm,input=list(bm.verts),use_existing_faces=False)
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
 for f in bm.faces:
  normal=f.normal;up=max(0,min(1,(normal.y-.18)/.58));noise=.95+rng.random()*.075
  cool=PAL['sandstone_side'];warm=PAL['sandstone']
  # Broad warm sloping upper facets transition to cool slate sides by direction,
  # not by an artificial horizontal height boundary.
  col=tuple((cool[k]*(1-up)+warm[k]*up)*noise for k in range(3))+(1,)
  m.face([tuple(v.co) for v in f.verts],col)
 bm.free()

def rounded_sandstone(m,x,z,w,d,bottom,top,seed):
 convex_boulder(m,x,z,w,d,bottom,top,seed)

def terrain():
 m=MeshMaker('terrain')
 # Wide desktop exposes this 60 m background: keep its facets almost invisible.
 cliff_bank(m,[(-80,-80),(80,-80),(80,80),(-80,80)],-2,-1.52,'grass_dark',5,.65)
 cliff_bank(m,PLATEAU,-1.5,1.20,'grass',3)
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
  # The logical foot plane stays fixed; a runtime wooden bridge spans this opening.
  if -1.35 < (path[i][0]+path[i+1][0])*.5 < 1.35 and (path[i][1]+path[i+1][1])*.5 < -6.3:continue
  a,b,c,d=left[i],right[i],right[i+1],left[i+1]
  # Lighter compacted track in the centre, darker weathered shoulders.
  spans=[(-1,-.94),(-.94,-.82),(-.82,-.59),(-.59,-.55),(-.55,-.51),(-.51,0),(0,.51),(.51,.55),(.55,.59),(.59,.82),(.82,.94),(.94,1)]
  def road_color(t):
   feather=max(0,min(1,(abs(t)-.82)/.18))
   r=PAL['path'];g=PAL['grass_light']
   centre=1.025-.028*abs(t)
   rut=1-.09*max(0,1-abs(abs(t)-.55)/.04)
   return tuple((r[k]*centre*rut*(1-feather)+g[k]*feather) for k in range(3))+(1,)
  for lo,hi in spans:
   av,bv,cv,dv=Vector(a),Vector(b),Vector(c),Vector(d)
   q=[av.lerp(bv,(lo+1)*.5),av.lerp(bv,(hi+1)*.5),dv.lerp(cv,(hi+1)*.5),dv.lerp(cv,(lo+1)*.5)]
   m.face([(p.x,.25,p.y) for p in reversed(q)],'path')
   m.colors[-4:]=[road_color(lo),road_color(hi),road_color(hi),road_color(lo)]
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
  if -1.55 < (path[i][0]+path[i+1][0])*.5 < 1.55 and (path[i][1]+path[i+1][1])*.5 < -6.3:continue
  # With clockwise traversal, left lies outside of the U.
  a,b,c,d=left[i],left[i+1],outer[i+1],outer[i]
  m.face([(d[0],.48,d[1]),(c[0],.48,c[1]),(b[0],.48,b[1]),(a[0],.48,a[1])],tint('grass_light',.975+.023*math.sin(i*.84)))
  # Two asymmetric strata form broad diagonal cliff planes. Colours vary by
  # segment, rather than making dark/bright ribbons around the entire island.
  def rim_y(level,k):
   return (.43 if level==.42 else level) if level in (-1.52,.42,.48) else level+.24*math.sin(k*.91)+.11*math.sin(k*1.87)
  for j,(bottom,upper) in enumerate([(-1.52,-.54),(-.54,.42),(.42,.48)]):
   col=tint('grass_edge' if j==2 else 'cliff_mid',.92+.14*(.5+.5*math.sin(i*1.37+j*.19)))
   q=[(d[0],rim_y(bottom,i),d[1]),(c[0],rim_y(bottom,i+1),c[1]),(c[0],rim_y(upper,i+1),c[1]),(d[0],rim_y(upper,i),d[1])]
   if j==2:q=[(d[0],.43,d[1]),(c[0],.43,c[1]),(c[0],.48,c[1]),(d[0],.48,d[1])]
   m.face([q[0],q[1],q[2]],col);m.face([q[0],q[2],q[3]],tint(col,1.018))
 # Background cliff masses frame the two entry gates without covering the map.
 cliff_bank(m,[(-10,4),(-5.7,4),(-5.65,5.25),(-3.5,7),(-.5,9),(-1,14),(-10,14)],-1.5,3.1,'grass_dark',2)
 cliff_bank(m,[(7.1,5.0),(9,5),(13,9),(13,16),(6.9,16)],-1.5,1.2,'grass_dark',2)
 # Far rock groups frame the map with several rounded, stepped masses each.
 # Their footprint stays outside the playable roads; only decorative silhouettes change.
 for group,(x,z,w,d,h) in enumerate([(-12,10,4,6,4.8),(-9,14,5,5,5.3),(-4,17,6,5,4.7),(3,18,6,5,4.2),(10,15,5,5,4.8),(14,8,4,6,4.1),(-15,2,4,7,3.2),(-15,-6,4,5,2.8),(15,-2,4,6,2.8)]):
  for part,(dx,dz,sx,sz,sy) in enumerate([(-.12,.12,.91,.78,.90),(.22,-.25,.62,.60,.56),(-.27,-.31,.56,.48,.41),(.31,.20,.48,.54,.70)]):
   rounded_sandstone(m,x+dx*w,z+dz*d,w*sx,d*sz,-1.50,-1.50+(h+1.5)*sy,8321+group*17+part*139)
 # Sparse tiny sandy chips, flattened into the road mesh: no decal materials,
 # no alpha overdraw and no runtime particles needed for static surface detail.
 rng=random.Random(77531)
 for i in range(220):
  j=rng.randrange(len(path)-1);p=Vector(path[j]).lerp(Vector(path[j+1]),rng.random())
  direction=(Vector(path[j+1])-Vector(path[j])).normalized();side=Vector((-direction.y,direction.x))
  p+=side*rng.uniform(-.92,.92);r=rng.uniform(.022,.059);angle=rng.random()*math.tau
  if -1.35<p.x<1.35 and p.y<-6.3:continue
  pts=[(p.x+math.cos(angle+k*math.tau/3)*r,.254,p.y+math.sin(angle+k*math.tau/3)*r*.65) for k in range(3)]
  m.face(list(reversed(pts)),tint('path',rng.uniform(.88,1.11)))
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
 for ob in bpy.data.objects:
  if ob.type=='MESH':ob.hide_render=True;ob.hide_viewport=True
 terrain_ob=bpy.data.objects.get('terrain');terrain_ob.hide_render=False;terrain_ob.hide_viewport=False;terrain_ob.scale.x=-1
 def copy_asset(name,x,y,z,scale=1,turn=0):
  src=bpy.data.objects[name];ob=src.copy();ob.data=src.data;bpy.context.collection.objects.link(ob);ob.hide_render=False;ob.hide_viewport=False;ob.location=bcoord((-x,y,z));ob.scale=(-scale,scale,scale);ob.rotation_euler.z=math.radians(-turn);return ob
 copy_asset('barracks',-4.1,.25,-.30,.85,180)
 copy_asset('castle_gate',-4.4,.25,2.0,.90,180)
 copy_asset('enemy_gate',3.1,.25,8.8,.9,180);copy_asset('enemy_gate',5.35,.25,8.8,.9,180)
 copy_asset('tower',1.45,1.2,-2.5,.95,110);copy_asset('tower',1.40,1.2,1.05,.95,100)
 copy_asset('king',-1.2,1.2,-1.2,1.1,150)
 for x,z,s in [(-5.7,-7,.8),(-6.5,-6,.8),(6,-4,.9),(6.8,-2,.8),(7.8,1,1),(-5,4,1),(-5.8,5.3,.8),(-1,9,1.2),(1,10,.9),(7,8,1)]:copy_asset('tree',x,-1.4,z,s)
 for x,z,s in [(-6.2,-7.8,.8),(-6.2,-4.4,1),(6.6,-5.3,.9),(7.0,-.4,.8),(8.0,2.5,1),(-7,2,.8),(1.7,9.6,1.1)]:copy_asset('tree_broadleaf',x,-1.45,z,s)
 for x,z,s in [(-2.7,-3.95,.55),(-2.9,3.9,.61),(-2.4,5.0,.54)]:copy_asset('tree_broadleaf',x,1.20,z,s)
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
  t=(i//4)/13;copy_asset('soldier_blue',-4.0+(i%4-1.5)*.15,.25,-2.6-t*3.0,1,180)
 world=bpy.context.scene.world;world.color=(.3,.3,.3);world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.38,.50,.56,1);world.node_tree.nodes['Background'].inputs[1].default_value=.65
 sun=bpy.data.lights.new('Sun','SUN');sun.energy=2.0;sun.color=(1,.91,.76);sun.angle=.16;light=bpy.data.objects.new('Sun',sun);bpy.context.collection.objects.link(light);light.rotation_euler=(math.radians(25),math.radians(-25),math.radians(-25))
 camd=bpy.data.cameras.new('Camera');cam=bpy.data.objects.new('Camera',camd);bpy.context.collection.objects.link(cam);target=Vector(bcoord((0,0,0)));cam.location=bcoord((2.0,27.8,-19.46));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();camd.type='PERSP';camd.lens_unit='FOV';camd.angle=math.radians(32);bpy.context.scene.camera=cam
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.render.resolution_x=820;scene.render.resolution_y=1180;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(HERE/'world-preview.png');scene.render.film_transparent=False
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'hyeopgok-assets.blend'))
 if '--no-render' not in sys.argv:bpy.ops.render.render(write_still=True)

# Hidden source objects must also be removed when replaying through live MCP.
for ob in list(bpy.data.objects):bpy.data.objects.remove(ob,do_unlink=True)
for datablocks in (bpy.data.meshes,bpy.data.materials):
 for data in list(datablocks):
  if data.users==0:datablocks.remove(data)
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
soldier();soldier(True);king();crossbow()
exec(compile((HERE/'phase1_models.py').read_text(),str(HERE/'phase1_models.py'),'exec'))
build_phase1_models();m=tower_stage(2,'tower',False);crossbow(m,1.62);emit(m)
tree();broadleaf();rock();terrain();bolt()
exec(compile((HERE/'art_r1_models.py').read_text(),str(HERE/'art_r1_models.py'),'exec'))
build_art_r1_models()
# Upgrade aliases deliberately replace legacy IDs; retain only final manifest entries.
ASSETS[:]=list({a['id']:a for a in ASSETS}.values())
(HERE/'model-manifest.json').write_text(json.dumps({'license':'Original procedural art created for this game; no third-party assets','axes':'Unity x right / y up / z forward. FBX -Z forward +Y up bake transforms.','color':'COLOR RGB sRGB authored base; alpha Blender ray-baked AO. UV0.x legacy team mask (0 team / 1 fixed); UV0.y=1 baked marker','palette':PAL,'path_control_points':PATH_POINTS,'plateau_polygon':PLATEAU,'left_approach':{'x':[-5.5,-3.25],'z':[-1.15,3.0],'top_y':.245,'castle':[-4.4,.25,2.0],'castle_scale':.9,'barracks':[-4.1,.25,-.30],'barracks_scale':.85,'building_yaw':180},'assets':ASSETS},indent=2,ensure_ascii=False)+'\n')
make_preview()
