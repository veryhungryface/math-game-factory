import bpy,os,sys,argparse
from pathlib import Path
from mathutils import Vector
base=Path(__file__).resolve().parent
p=argparse.ArgumentParser();p.add_argument('--output-dir',default=str(base.parent.parent/'public/g/sunbasket-farm/assets') if (base.parent.parent/'public').is_dir() else str(base/'generated-assets'));p.add_argument('--source',default=str(base/'sunbasket-farm-assets.blend'));p.add_argument('--preview-dir',default=str(base));p.add_argument('--logo-only',action='store_true');p.add_argument('--font-path',default='/System/Library/Fonts/AppleSDGothicNeo.ttc');a=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
out=Path(a.output_dir);out.mkdir(parents=True,exist_ok=True);preview=Path(a.preview_dir);preview.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=a.source);scene=bpy.context.scene;cam=scene.camera
for o in scene.objects:
 if o.type not in ['LIGHT','CAMERA']:o.hide_render=True
scene.render.film_transparent=True;scene.render.resolution_percentage=100;scene.cycles.samples=24
sign=bpy.data.objects.get('Sunbasket wooden sign');head=bpy.data.objects.get('햇살 바구니 로고');tag=bpy.data.objects.get('밭에서 가게까지')
for o in sign.children_recursive:o.hide_render=False
head.hide_render=False;tag.hide_render=False
# The small subtitle needs a lighter outline than the large carved display type.
if os.path.exists(a.font_path):
 oldtag=tag;oldtag.hide_render=True;cu=bpy.data.curves.new('Clean small subtitle','FONT');cu.body='밭에서 가게까지';cu.font=bpy.data.fonts.load(a.font_path);cu.align_x='CENTER';cu.size=.44;cu.offset=.002;cu.extrude=0;tag=bpy.data.objects.new('Clean small subtitle',cu);scene.collection.objects.link(tag);tag.parent=cam;tag.location=oldtag.location.copy();ma=bpy.data.materials.new('Flat subtitle ink');ma.use_nodes=True;ma.node_tree.nodes.clear();outnode=ma.node_tree.nodes.new('ShaderNodeOutputMaterial');em=ma.node_tree.nodes.new('ShaderNodeEmission');em.inputs['Color'].default_value=(.25,.14,.049,1);ma.node_tree.links.new(em.outputs['Emission'],outnode.inputs['Surface']);cu.materials.append(ma)
# UI lettering and painted sign face use unlit inks. This avoids specular wedges
# from the converted glyph bevels while retaining the original sign geometry.
for obj in list(sign.children_recursive)+[head,tag]:
 if obj.type!='MESH':continue
 obj.data=obj.data.copy()
 for slot in obj.material_slots:
  if not slot.material:continue
  original=slot.material;ma=original.copy();ma.name=original.name+' UI ink';ma.use_nodes=True;nodes=ma.node_tree.nodes;links=ma.node_tree.links
  vertex=next((n for n in nodes if n.type=='VERTEX_COLOR'),None);old=next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None);color=tuple(old.inputs['Base Color'].default_value) if old else (.15,.062,.024,1)
  nodes.clear();outnode=nodes.new('ShaderNodeOutputMaterial');em=nodes.new('ShaderNodeEmission');em.inputs['Strength'].default_value=1
  if vertex:
   vc=nodes.new('ShaderNodeVertexColor');vc.layer_name='Color';links.new(vc.outputs['Color'],em.inputs['Color'])
  else:em.inputs['Color'].default_value=color
  links.new(em.outputs['Emission'],outnode.inputs['Surface']);slot.material=ma
for o in [sign,head,tag]:o.location.y-=9.4
cam.data.ortho_scale=13.6;scene.render.resolution_x=1200;scene.render.resolution_y=300;scene.render.image_settings.color_mode='RGBA';scene.render.filepath=str(out/'title-logo.png');bpy.ops.render.render(write_still=True)
if a.logo_only:sys.exit(0)
for o in sign.children_recursive:o.hide_render=True
head.hide_render=True;tag.hide_render=True

def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
def show(name,pos=(0,0,0),scale=1,angle=0):
 root=bpy.data.objects.new('Detail '+name,None);scene.collection.objects.link(root);root.location=pos;root.scale=(scale,scale,scale);root.rotation_euler.z=angle;created=[]
 for src in bpy.data.collections['Original export meshes'].objects:
  if src.name.startswith(name+'_') or (name=='windmill' and src.name=='windmill-rotor'):
   o=src.copy();o.data=src.data;scene.collection.objects.link(o);o.hide_render=False;o.hide_set(False);o.parent=root;created.append(o)
 return created
for crop in ['carrot','strawberry','corn']:
 objs=show(crop)
 cam.location=(2.0,-4.5,2.6);aim(cam,(0,0,.49));cam.data.ortho_scale=1.38;scene.render.resolution_x=192;scene.render.resolution_y=192;scene.render.filepath=str(out/('icon-'+crop+'.png'));bpy.ops.render.render(write_still=True)
 for o in objs:o.hide_render=True
# Full-resolution close views of the exact models, for source/host visual QA.
scene.render.film_transparent=False;scene.render.resolution_x=1200;scene.render.resolution_y=850;scene.cycles.samples=24
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.015));floor=bpy.context.object;floor.name='Details cream floor';ma=bpy.data.materials.new('Details cream');ma.use_nodes=True;ma.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.81,.75,.57,1);ma.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.8;floor.data.materials.append(ma)
objs=show('farmhouse',(-2.15,.6,0),1,-.10)+show('market-stall',(1.78,-.1,0),1,.14)+show('cart',(.20,-2.4,0),.75,-.3)
cam.location=(8,-14,10);aim(cam,(0,0,1.48));cam.data.ortho_scale=10.8;scene.render.filepath=str(preview/'buildings-studio.png');bpy.ops.render.render(write_still=True)
for o in objs:o.hide_render=True
objs=show('carrot',(-1.4,-.2,0),1.8,-.2)+show('strawberry',(.02,-.15,0),1.7,0)+show('corn',(1.5,-.1,0),1.8,.1)+show('crate',(0,1.25,0),1.05,0)
cam.location=(4.7,-9,6);aim(cam,(0,.1,.85));cam.data.ortho_scale=5.8;scene.render.filepath=str(preview/'crops-studio.png');bpy.ops.render.render(write_still=True)
print('SUNBASKET_DETAILS_DONE',flush=True)
