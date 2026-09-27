"""Render all three building stages from the final AO-baked source scene."""
import bpy, math
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(HERE/'hyeopgok-assets.blend'))
for obj in bpy.data.objects:
 if obj.type=='MESH':obj.hide_render=True
for stage in [1,2,3]:
 for kind,depth in [('barracks',0),('tower_base',2.3)]:
  obj=bpy.data.objects[kind+'_l'+str(stage)];obj.hide_render=False
  obj.location=((stage-2)*2.55,-depth,0);obj.scale=(1,1,1)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.009))
ob=bpy.context.object;mat=bpy.data.materials.new('Study ground');mat.use_nodes=True
mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.045,.37,.21,1);ob.data.materials.append(mat)
scene=bpy.context.scene;cam=scene.camera;cam.location=(5,-12,10);target=Vector((0,-.9,.55));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=9
scene.render.resolution_x=1080;scene.render.resolution_y=700;scene.cycles.samples=24
scene.render.filepath=str(HERE/'art-r1-building-study.png');bpy.ops.render.render(write_still=True)
