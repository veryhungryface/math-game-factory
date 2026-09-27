"""Reproducible model review, never used as a gameplay capture.
Blender -b --factory-startup --python ArtSource/blender/render_phase1_models.py
"""
import bpy, sys, runpy, math
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
if '--no-render' not in sys.argv:sys.argv.append('--no-render')
if '--existing' in sys.argv:bpy.ops.wm.open_mainfile(filepath=str(HERE/'hyeopgok-assets.blend'))
else:runpy.run_path(str(HERE/'build_models.py'),run_name='__main__')
for ob in list(bpy.data.objects):
 if ob.type!='MESH':bpy.data.objects.remove(ob,do_unlink=True)
 else:ob.hide_render=True;ob.hide_viewport=True

def place(name,x,y,scale=1,red=False):
 src=bpy.data.objects[name];ob=src.copy();ob.data=src.data.copy();bpy.context.collection.objects.link(ob)
 ob.location=(x,y,0);ob.rotation_euler=(0,0,0);ob.scale=(scale,scale,scale);ob.hide_viewport=False;ob.hide_render=False
 if red:
  mat=ob.data.materials[0].copy();ob.data.materials.clear();ob.data.materials.append(mat)
  for no in mat.node_tree.nodes:
   if no.bl_idname=='ShaderNodeMixRGB' and no.blend_type=='MULTIPLY':no.inputs[2].default_value=(.816,.024,.047,1)
 return ob
for stage in [1,2,3]:
 place('tower_base_l'+str(stage),(stage-1)*3.15,0)
 bow=place('crossbow',(stage-1)*3.15,0);bow.location.z=1.62
 place('barracks_l'+str(stage),(stage-1)*3.15,-3.4)
 place('castle_gate_l'+str(stage),(stage-1)*3.15,-6.7)
for i,name in enumerate(['king','archer_blue','archer_red','giant','boss']):place(name,i*1.6,-10.2,1,red=name=='archer_red')
place('enemy_gate',9.5,-1.0);place('enemy_watchtower',9.5,-4.2)
for i,name in enumerate(['barrel','crate','stump','well','bell','palisade','palisade_damaged']):place(name,9+(i%2)*1.6,-7.0-(i//2)*1.2,.8)
bpy.ops.mesh.primitive_plane_add(size=200,location=(4,-5,-.025));ground=bpy.context.object
mat=bpy.data.materials.new('Review-ground');mat.diffuse_color=(.22,.40,.34,1);ground.data.materials.append(mat)
scene=bpy.context.scene;scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.42,.50,.57,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.55
light_data=bpy.data.lights.new('Review-sun','SUN');light_data.energy=2.4;light_data.angle=.17
light=bpy.data.objects.new('Review-sun',light_data);bpy.context.collection.objects.link(light);light.rotation_euler=(math.radians(26),math.radians(-24),math.radians(-28))
camd=bpy.data.cameras.new('Review-camera');cam=bpy.data.objects.new('Review-camera',camd);bpy.context.collection.objects.link(cam);cam.location=(15,-23,25);target=Vector((4.6,-5,0));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();camd.type='ORTHO';camd.ortho_scale=21.2;scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.view_settings.view_transform='Standard';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(HERE/'phase1-models.png')
bpy.ops.render.render(write_still=True)
