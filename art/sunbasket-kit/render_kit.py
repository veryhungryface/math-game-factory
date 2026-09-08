import bpy,sys,argparse,os,math
from pathlib import Path
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--source',default=str(Path(__file__).parent/'sunbasket-farm-assets.blend'));p.add_argument('--output',default='logs/manual-20260908-sunbasket-v3/assets');p.add_argument('--samples',type=int,default=20);p.add_argument('--only',default='');a=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
bpy.ops.wm.open_mainfile(filepath=a.source);scene=bpy.context.scene;out=Path(a.output);out.mkdir(parents=True,exist_ok=True)
for o in scene.objects:o.hide_render=True
orig=bpy.data.collections['Original export meshes'];created=[]
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
def show(name,pos=(0,0,0),s=1,angle=0,hide_parts=(),tint=None):
 root=bpy.data.objects.new('Kit placement '+name,None);scene.collection.objects.link(root);root.location=pos;root.scale=(s,s,s);root.rotation_euler.z=angle;created.append(root)
 for src in orig.objects:
  if src.get('kit_asset')!=name and not(src.name.startswith(name+'_') or name=='windmill' and src.name=='windmill-rotor'):continue
  if src.get('kit_part') in hide_parts:continue
  o=src.copy();o.data=src.data;scene.collection.objects.link(o);o.hide_render=False;o.hide_set(False);o.parent=root;created.append(o)
  if tint and src.get('kit_part') in tint:
   o.data=o.data.copy();ca=o.data.color_attributes.get('Color')
   for d in ca.data:d.color=(*(d.color[i]*tint[src.get('kit_part')][i] for i in range(3)),1)
 return root
ma=bpy.data.materials.new('Kit neutral buttercream');ma.use_nodes=True;ma.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.70,.72,.60,1);ma.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.85
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.015));floor=bpy.context.object;floor.data.materials.append(ma)
world=bpy.data.worlds.new('Kit sky');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.64,.73,.74,1);world.node_tree.nodes['Background'].inputs[1].default_value=.45;scene.world=world
for name,pos,power,size,col in [('Soft key',(-5,-7,12),1800,7,(1,.88,.73)),('Cool fill',(6,-1,8),1000,7,(.79,.89,1)),('Leaf edge',(0,7,10),1300,6,(1,.94,.74))]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=col;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos;aim(o,(0,0,1))
d=bpy.data.cameras.new('Kit orthographic camera');cam=bpy.data.objects.new('Kit orthographic camera',d);scene.collection.objects.link(cam);scene.camera=cam;d.type='ORTHO'
scene.render.engine='CYCLES';scene.cycles.samples=a.samples;scene.cycles.use_denoising=True;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
def render(name,pos,target,scale,width=1200,height=850,save=False):
 if a.only and a.only!=name:return
 cam.location=pos;aim(cam,target);d.ortho_scale=scale;scene.render.resolution_x=width;scene.render.resolution_y=height;scene.render.filepath=str(out/(name+'.png'))
 if save:bpy.ops.wm.save_as_mainfile(filepath=str(Path(a.source).parent/(name+'.blend')),compress=True)
 bpy.ops.render.render(write_still=True)
def clear():
 for o in created:bpy.data.objects.remove(o,do_unlink=True)
 created.clear()
show('farmhouse',(-2.3,.4,0));show('market-stall',(1.7,-.3,0),1,.06);show('cart',(.0,-2.6,0),.75,-.3)
render('buildings-v3',(8,-15,10),(0,-.2,1.55),10.8)
clear();show('apple-tree',(-1.85,.50,0),1);show('carrot',(0.25,-.3,0),1.8);show('carrot-young',(1.40,-.4,0),1.8);show('carrot-sprout',(2.4,-.4,0),1.8);show('strawberry',(1.2,1.4,0),1);show('corn',(2.4,1.5,0),1.2)
render('orchard-crops-v3',(6,-11,7),(0,.3,1.40),8.6)
clear()
for x,y in [(-1,-1),(0,-1),(1,-1),(-1,0),(0,0),(1,0)]:show('carrot',(x,y,.02),.64)
show('farmhouse',(-2,2,0),.95);show('market-stall',(2,1,0),.75,-.35);show('apple-tree',(3,3,0),.9);show('apple-tree',(-3,-1,0),.75);show('cart',(1,-2,0),.7,.4)
for x in [-2,0,2]:show('fence',(x,4,0),1)
render('sample-small-farm',(10,-16,12),(0,1,1.15),12.4,1200,900,True)
clear();show('market-stall',(-2,1,0),1,-.1);show('market-stall',(2,1,0),1,.1,tint={'awning':(.80,.95,1.12)})
show('farmhouse',(0,4,0),.82,hide_parts=('porch',),tint={'roof':(1.35,.73,.62)})
for x,y,s in [(-4,2,.75),(4,2,.82),(3,-2,.63)]:show('apple-tree',(x,y,0),s,hide_parts=('fruit',) if x==4 else ())
for x,y in [(-1,-.8),(1,-1.0),(-2,-2)]:show('crate',(x,y,0),.8)
show('cart',(2,-2,0),.8,.45)
render('sample-village-market',(10,-16,12),(0,1,1.20),13.0,1200,900,True)
print('KIT_RENDER_DONE',flush=True)
