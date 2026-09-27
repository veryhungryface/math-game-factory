"""Render the three final irregular rock meshes from the saved source scene."""
import bpy, math
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(HERE/'hyeopgok-assets.blend'))
for obj in bpy.data.objects:
 if obj.type=='MESH':obj.hide_render=True
for name,location in [('rock_small',(-2.4,0,0)),('rock_medium',(-.6,0,.2)),('rock_large',(2.0,0,.5))]:
 ob=bpy.data.objects[name];ob.hide_render=False;ob.location=(location[0],-location[2],location[1]);ob.scale=(1,1,1)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.009))
ob=bpy.context.object;mat=bpy.data.materials.new('Study grass');mat.diffuse_color=(.045,.37,.21,1);mat.use_nodes=True;mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.045,.37,.21,1);ob.data.materials.append(mat)
scene=bpy.context.scene;cam=scene.camera;cam.location=(7,11,8);target=Vector((.2,0,.65));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=8.3
scene.render.resolution_x=1080;scene.render.resolution_y=620;scene.cycles.samples=24
scene.render.filepath=str(HERE/'art-r1-rock-study.png');bpy.ops.render.render(write_still=True)
