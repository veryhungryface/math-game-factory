"""Round 2 original crowned survey king; regenerates king.fbx only.
Blender -b --factory-startup --python ArtSource/blender/art_r2_king.py
Coordinates use Unity metres, palette and ray-baked vertex AO share the source kit.
"""
from pathlib import Path
SOURCE=Path(__file__).resolve().parent/'build_models.py'
exec(compile(SOURCE.read_text().split('# Hidden source objects')[0],str(SOURCE),'exec'))
m=MeshMaker('king')
for x in [-.11,.11]:
 m.capsule_box((x,.065,.035),(.16,.13,.25),'leather')
 m.capsule_box((x,.185,0),(.14,.18,.16),'navy')
m.cylinder((0,.43,0),.24,.35,'royal',n=8,radius_top=.20)
m.box((0,.38,.175),(.37,.045,.03),'gold')
m.box((0,.455,.20),(.11,.12,.03),'gold_light')
for side in [-1,1]:
 m.capsule_box((side*.24,.54,.005),(.20,.15,.20),'gold')
 m.capsule_beam((side*.235,.52,.015),(side*.30,.32,.1),.14,.14,'royal')
 m.capsule_box((side*.30,.30,.10),(.15,.14,.15),'skin')
m.cylinder((0,.715,.015),.178,.255,'skin',n=10)
m.rings((0,0,0),[(.73,.183),(.835,.18),(.875,.15)],'leather',n=10)
for x in [-.065,.065]:m.box((x,.715,.184),(.028,.032,.014),'black')
m.capsule_box((0,.64,.158),(.20,.10,.09),'leather')
# A broad hammered circlet with five compass-point teeth and a turquoise stone.
m.rings((0,0,0),[(.825,.203),(.895,.203)],'gold',n=10)
for i in range(5):
 a=i*math.tau/5+.25
 x,z=math.sin(a)*.19,math.cos(a)*.19
 m.wedge((x,.965,z),(.095,.18,.074),'gold_light')
m.box((0,.869,.204),(.063,.075,.029),'royal')
# Wide folded blue mantle, silhouette clear behind the coin satchel.
left=(-.225,.60,-.135);right=(.225,.60,-.135)
m.face([left,right,(.34,.13,-.31),(0,.115,-.42),(-.34,.13,-.31)],'royal')
m.face([left,(-.34,.13,-.31),(0,.115,-.42),(0,.54,-.18)],'roof')
m.face([(0,.54,-.18),(0,.115,-.42),(.34,.13,-.31),right],'royal')
m.beam(left,(-.34,.13,-.31),.035,.025,'gold')
m.beam(right,(.34,.13,-.31),.035,.025,'gold')
emit(m)
(HERE/'art-r2-king-manifest.json').write_text(json.dumps(ASSETS,indent=2)+'\n')
