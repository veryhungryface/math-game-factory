"""Art round 1: hand-shaped mineral masses and small reusable camp dressing.

Loaded by build_models.py. Every asset shares one colour/AO mesh and material;
scene placement belongs to HyeopgokEnvironment.cs, not these source meshes.
"""

def worn_rock(name, width, height, depth, seed):
 m=MeshMaker(name)
 convex_boulder(m,0,0,width,depth,0,height,seed)
 # Asymmetric fractured shoulder is merged visually with the larger mass.
 if height>1.5:convex_boulder(m,width*.34,depth*.19,width*.42,depth*.48,0,height*.43,seed+907)
 emit(m)

def tiny_dressing():
 m=MeshMaker('grass_tuft')
 for j,(x,z,h,angle) in enumerate([(-.08,0,.23,-.28),(.035,.04,.30,.15),(.10,-.015,.19,.44),(-.01,-.06,.17,-.6)]):
  w=.047;tip=(x+math.sin(angle)*h*.35,h,z+math.cos(angle)*h*.18)
  p=[(x-w,0,z),(x+w,0,z),tip]
  m.face(p,'grass_light');m.face(list(reversed(p)),'grass_dark')
 emit(m)
 m=MeshMaker('flowers')
 for j,(x,z,h) in enumerate([(-.11,-.04,.15),(.07,.01,.21),(-.02,.12,.12)]):
  m.beam((x,0,z),(x,h,z),.010,.010,'pine_light')
  for k in range(5):
   a=k*math.tau/5;r=.041
   p=[(x,h,z),(x+math.cos(a-.4)*r,h-.006,z+math.sin(a-.4)*r),(x+math.cos(a)*r*1.45,h+.009,z+math.sin(a)*r*1.45),(x+math.cos(a+.4)*r,h-.006,z+math.sin(a+.4)*r)]
   m.face(list(reversed(p)),'ivory' if j!=1 else 'gold_light')
  m.cylinder((x,h+.006,z),.016,.016,'gold',n=5)
 emit(m)
 m=MeshMaker('pebbles')
 for j,(x,z,r) in enumerate([(-.19,-.02,.12),(.07,.035,.15),(.0,-.13,.075),(.22,.10,.06)]):
  m.rings((x,0,z),[(0,r*.84),(.065,r),(.11,r*.53)],'sandstone_side',n=5,aspect=(1,.78),phase=j*.71)
 emit(m)

def camp_props():
 m=MeshMaker('sack')
 m.rings((0,0,0),[(0,.18),(.06,.24),(.30,.255),(.46,.18),(.50,.08),(.54,.095),(.57,.062)],'straw',n=9,aspect=(1,.84))
 m.cylinder((0,.504,0),.087,.030,'wood',n=9)
 # Broad cloth crease and hand-stitched seam, visible even at small camp scale.
 m.beam((-.08,.07,.208),(-.09,.35,.189),.015,.015,'straw_light')
 for j in range(4):m.box((-.08,.12+j*.055,.206),(.037,.012,.017),'wood_light')
 emit(m)
 m=MeshMaker('logpile')
 for j,(x,y) in enumerate([(-.25,.16),(.0,.16),(.25,.16),(-.12,.40),(.14,.40),(0,.63)]):
  # Build horizontal logs with ring cross sections in the x/y plane.
  n=7;r=.148;length=.98+(j%3)*.075
  front=[(x+math.cos(k*math.tau/n)*r,y+math.sin(k*math.tau/n)*r,length*.5) for k in range(n)]
  back=[(a,b,-length*.5) for a,b,c in front]
  m.face(front,'wood_gold');m.face(list(reversed(back)),'wood_light')
  for k in range(n):m.face([front[k],back[k],back[(k+1)%n],front[(k+1)%n]],tint('wood',.93+.1*(k%2)))
  inner=[(x+math.cos(k*math.tau/n)*r*.47,y+math.sin(k*math.tau/n)*r*.47,length*.5+.004) for k in range(n)]
  m.face(inner,'wood_light')
 for z in [-.28,.28]:
  m.beam((-.40,.14,z),(0,.80,z),.032,.036,'ivory');m.beam((0,.80,z),(.40,.14,z),.032,.036,'ivory')
 emit(m)
 m=MeshMaker('torch')
 m.cylinder((0,.10,0),.22,.20,'stone_shadow',n=7,radius_top=.17)
 m.cylinder((0,.51,0),.068,.81,'wood',n=6)
 m.cylinder((0,.92,0),.11,.21,'steel_dark',n=7,radius_top=.17)
 m.rings((0,0,0),[(.99,.12),(1.16,.105),(1.32,.022)],'gold',n=5)
 m.rings((-.025,0,.015),[(1.0,.065),(1.17,.063),(1.27,0)],'gold_light',n=5)
 emit(m)

def build_art_r1_models():
 worn_rock('rock_small',.90,.50,.75,129)
 worn_rock('rock_medium',1.65,.92,1.30,713)
 worn_rock('rock_large',3.40,1.95,2.60,413)
 tiny_dressing();camp_props()
