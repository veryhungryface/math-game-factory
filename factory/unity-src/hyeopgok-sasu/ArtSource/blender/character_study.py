"""Inspect the king and two heavy units from final AO-baked model sources."""
import bpy
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(HERE/'hyeopgok-assets.blend'))
for obj in bpy.data.objects:
 if obj.type=='MESH':obj.hide_render=True
for name,x in [('king',-1.9),('giant',0),('boss',2.05)]:
 obj=bpy.data.objects[name];obj.hide_render=False;obj.location=(x,0,0);obj.scale=(1,1,1)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.009))
ob=bpy.context.object;mat=bpy.data.materials.new('Study ground');mat.use_nodes=True
mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.045,.37,.21,1);ob.data.materials.append(mat)
scene=bpy.context.scene;cam=scene.camera;cam.location=(5,-11,6.4);target=Vector((.25,0,1.0));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=6.6
scene.render.resolution_x=1100;scene.render.resolution_y=680;scene.cycles.samples=24
scene.render.filepath=str(HERE/'art-r1-character-study.png');bpy.ops.render.render(write_still=True)
