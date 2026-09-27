"""Phase 1 handmade toy-medieval meshes. Executed by build_models.py.
All coordinates are Unity metres; no reference pixels or downloaded geometry.
One colour mesh per model; details are geometry, never extra materials.
"""

def diamond_badge(m,x,y,z,r=.16,color='gold_light'):
 # Survey compass rhombus: deliberately neither a crown nor crossed blades.
 m.face([(x-r,y,z),(x,y-r*1.3,z),(x+r,y,z),(x,y+r*1.3,z)],color)
 m.face([(x,y+r*1.3,z+.002),(x+r,y,z+.002),(x,y-r*1.3,z+.002),(x-r,y,z+.002)],color)
 m.box((x,y,z+.014),(.034,r*1.05,.026),'ivory')

def tiled_roof(m,y,w,d,h,stage=3,enemy=False):
 col='red_shadow' if enemy else ('straw' if stage==1 else 'wood' if stage==2 else 'roof')
 m.wedge((0,y,0),(w,h,d),col)
 # A few broad offset slate courses retain a handmade read when minified.
 for side in [-1,1]:
  for row in range(3):
   xx=side*(row+.48)*w/6
   yy=y+h*.5-(row+.48)*h/3+.024
   for col_i in range(4):
    zz=(col_i-1.5)*d/4+(row%2-.5)*.025
    color='red' if enemy else ('straw_light' if stage==1 else 'wood_light' if stage==2 else 'royal')
    m.box((xx,yy,zz),(w/5.7,.038,d/4-.014),color,Matrix.Rotation(side*math.atan2(-h,w*.5),3,'Z'))
 m.box((0,y+h*.5+.034,0),(.13,.10,d+.10),'red_shadow' if enemy else 'wood_gold')

def tower_stage(stage,name=None,export=True):
 m=MeshMaker(name or 'tower_base_l'+str(stage))
 if stage==1:
  for x in [-.37,.37]:
   for z in [-.37,.37]:
    m.box((x,.75,z),(.16,1.50,.16),'wood')
    m.box((x,.11,z),(.23,.22,.23),'stone_shadow')
  for z in [-.39,.39]:
   m.beam((-.37,.28,z),(.37,1.30,z),.10,.075,'wood_light')
  for x in [-.39,.39]:m.beam((x,.29,-.37),(x,1.26,.37),.075,.09,'wood_light')
  m.box((0,1.45,0),(1.03,.15,1.03),'wood_light')
  for z in [-.39,-.13,.13,.39]:m.box((0,1.535,z),(1.03,.035,.021),'wood')
  for x in [-.46,.46]:m.box((x,1.64,0),(.07,.20,1.03),'wood_gold')
  for i in range(5):m.box((.46,.24+i*.23,.58),(.46,.057,.07),'wood_gold')
  for x in [.25,.67]:m.beam((x,.04,.80),(x,1.47,.43),.047,.047,'wood')
 else:
  m.box((0,.10,0),(1.14,.20,1.08),'stone_shadow')
  m.box((0,.24,0),(1.04,.10,1.0),'stone_light')
  if stage==2:m.box((0,.86,0),(.80,1.16,.78),'stone')
  else:m.cylinder((0,.83,0),.54,1.10,'stone',n=8)
  for y in [.54,.91,1.24]:
   m.box((0,y,.406),(.80,.027,.014),'stone_shadow')
   m.box((.406,y,0),(.014,.027,.79),'stone_shadow')
  for x,y in [(-.21,.36),(.18,.72),(-.17,1.07)]:m.box((x,y,.417),(.026,.31,.018),'stone_shadow')
  m.box((0,1.40,0),(1.11,.18,1.06),'stone_light')
  m.box((0,1.52,0),(1.0,.065,.95),'mortar')
  for x in [-.44,.44]:
   for z in [-.43,.43]:m.box((x,1.65,z),(.22,.24,.22),'stone_light')
  m.box((0,1.00,.433),(.33,.68,.025),'royal')
  m.box((0,.71,.447),(.33,.032,.022),'gold')
  diamond_badge(m,0,1.02,.454,.095)
  if stage==3:
   for x in [-.43,.43]:
    m.box((x,1.45,0),(.10,.065,.91),'gold')
    m.box((x,.38,.43),(.16,.25,.16),'stone_light')
   m.box((0,1.55,-.50),(.48,.26,.12),'royal')
   diamond_badge(m,0,1.56,-.427,.105)
 return emit(m) if export else m

def barracks_stage(stage,name=None):
 m=MeshMaker(name or 'barracks_l'+str(stage))
 m.box((0,.07,0),(1.5,.14,1.30),'stone_shadow')
 m.box((0,.22,0),(1.35,.17,1.15),'wood' if stage==1 else 'stone_light')
 m.box((0,.68,0),(1.18,.81,.94),'plaster' if stage>1 else 'wood_light')
 # Big dark doorway, wooden lintel, short steps and dressed corners.
 m.box((0,.53,.482),(.45,.73,.029),'black')
 for x in [-.27,.27]:m.box((x,.57,.51),(.10,.83,.11),'wood')
 m.box((0,.94,.53),(.66,.11,.12),'wood_gold')
 for j in range(3):m.box((0,.06+j*.058,.67-j*.095),(.62,.12+j*.115,.22),'stone_light')
 for x in [-.55,.55]:
  m.box((x,.66,.50),(.13,.97,.13),'wood')
  for z in [-.36,.05,.37]:m.box((x*1.09,.60,z),(.045,.68,.05),'wood_light')
  # Small side glazing with navy inset and ivory muntin.
  m.box((x*1.10,.73,0),(.026,.30,.26),'navy')
  m.box((x*1.125,.73,0),(.02,.33,.027),'wood_gold')
 tiled_roof(m,1.26,1.54,1.38,.65,stage)
 m.box((0,1.14,.752),(.46,.47,.085),'wood')
 m.box((0,1.14,.800),(.38,.40,.02),'royal' if stage>1 else 'wood_light')
 diamond_badge(m,0,1.14,.817,.135)
 if stage>1:
  m.box((-.43,1.54,-.35),(.18,.50,.20),'stone_shadow')
  m.box((-.43,1.80,-.35),(.25,.09,.26),'stone_light')
  for z in [-.38,-.10,.18,.46]:m.box((.56,1.0,z),(.04,.08,.055),'gold')
 if stage==3:
  # A secondary porch reads clearly as a final upgrade without growing footprint.
  m.wedge((0,1.01,.60),(.78,.26,.58),'roof')
  for x in [-.34,.34]:m.box((x,.55,.80),(.075,.86,.08),'stone_light')
  m.box((.63,1.16,-.22),(.065,.65,.065),'wood_gold')
  m.box((.67,1.28,-.21),(.025,.37,.41),'royal')
 return emit(m)

def castle_stage(stage,name=None):
 m=MeshMaker(name or 'castle_gate_l'+str(stage))
 if stage==1:
  for x in [-.78,-.55,.55,.78]:
   m.cylinder((x,.89,0),.135,1.78,'wood',n=5)
   m.cylinder((x,1.87,0),.145,.25,'wood_gold',n=5,radius_top=0)
  m.box((0,1.50,0),(1.65,.25,.34),'wood_light')
  for x in [-.33,-.16,0,.16,.33]:m.box((x,.68,0),(.15,1.36,.12),'wood')
  for y in [.36,1.12]:m.box((0,y,.10),(.83,.085,.065),'wood_gold')
  diamond_badge(m,0,1.50,.184,.14)
 else:
  for x in [-.69,.69]:
   m.box((x,.11,0),(.65,.22,.82),'stone_shadow')
   m.box((x,.96,0),(.55,1.56,.60),'stone')
   for y in [.50,.92,1.34]:m.box((x,y,.308),(.55,.027,.014),'mortar')
   m.box((x,1.78,0),(.73,.22,.78),'stone_light')
   for dx in [-.245,.245]:
    for dz in [-.24,.24]:m.box((x+dx,1.97,dz),(.19,.21,.20),'stone_light')
   m.box((x,1.15,.327),(.30,.71,.023),'royal')
   diamond_badge(m,x,1.18,.347,.105)
  m.box((0,1.56,0),(1.04,.26,.50),'stone_shadow')
  m.box((0,1.73,0),(1.10,.10,.56),'stone_light')
  for x in [-.34,-.17,0,.17,.34]:m.box((x,.70,-.13),(.155,1.40,.13),'wood')
  for y in [.34,1.1]:m.box((0,y,-.05),(.87,.07,.045),'steel_dark')
  if stage==3:
   for x in [-.69,.69]:
    m.cylinder((x,2.14,0),.33,.25,'roof',n=8,radius_top=.19)
    m.cylinder((x,2.42,0),.28,.43,'royal',n=8,radius_top=0)
    m.cylinder((x,2.66,0),.05,.095,'gold_light',n=6)
   m.box((0,1.99,0),(.83,.37,.14),'royal')
   m.box((0,2.19,0),(.90,.065,.21),'gold')
   diamond_badge(m,0,1.98,.084,.145)
  else:diamond_badge(m,0,1.58,.266,.11)
 return emit(m)

def enemy_portal():
 m=MeshMaker('enemy_gate')
 for x in [-.68,.68]:
  m.box((x,.10,0),(.70,.20,.84),'stone_shadow')
  m.box((x,.91,0),(.53,1.62,.57),'stone')
  for y in [.42,.86,1.27]:m.box((x,y,.30),(.53,.032,.018),'stone_shadow')
  m.box((x,1.65,0),(.69,.18,.75),'wood')
  m.wedge((x,1.98,0),(.88,.52,.95),'red_shadow')
  m.box((x,1.23,.325),(.33,.69,.045),'red')
  diamond_badge(m,x,1.23,.354,.105,'ivory')
 m.box((0,1.60,0),(1.0,.22,.53),'wood')
 m.box((0,1.83,0),(1.18,.20,.67),'wood_light')
 m.wedge((0,2.15,0),(1.50,.56,.92),'red')
 m.box((0,2.45,0),(.13,.16,1.03),'red_shadow')
 for side in [-1,1]:
  for row in range(3):
   m.box((side*(row+.45)*.25,2.42-(row+.45)*.185,.005),(.25,.035,.98),'red',Matrix.Rotation(-side*.65,3,'Z'))
 m.box((0,1.99,.49),(.52,.40,.055),'wood')
 diamond_badge(m,0,1.99,.527,.145,'ivory')
 # Open arch means the two spawn streams have a visually honest entrance.
 m.box((-.36,.57,-.16),(.11,1.08,.10),'wood');m.box((.36,.57,-.16),(.11,1.08,.10),'wood')
 return emit(m)

def archer(blue=False):
 m=MeshMaker('archer_blue' if blue else 'archer_red')
 for x in [-.067,.067]:
  m.box((x,.043,.026),(.088,.085,.14),'leather')
  m.box((x,.16,0),(.074,.18,.087),'team_dark')
 m.box((0,.33,0),(.23,.21,.16),'team')
 m.box((0,.28,.08),(.23,.028,.017),'leather')
 m.cylinder((0,.493,0),.095,.14,'skin',n=6)
 m.rings((0,0,-.01),[(.526,.147),(.646,.133),(.711,.025)],'team',n=6)
 m.box((0,.535,.117),(.18,.027,.020),'team_dark')
 m.beam((-.12,.39,.01),(-.19,.41,.20),.064,.064,'team')
 m.beam((.12,.38,0),(.065,.44,.20),.06,.06,'team')
 # Bow curves in its plane and a visible taut ivory string.
 p=[(-.19,.15,.22),(-.27,.29,.27),(-.29,.44,.285),(-.26,.58,.26),(-.17,.70,.21)]
 for a,b in zip(p,p[1:]):m.beam(a,b,.04,.035,'wood_gold')
 m.beam(p[0],p[-1],.008,.009,'ivory')
 m.beam((-.24,.43,.16),(.22,.43,.50),.02,.02,'wood_light')
 m.face([(.18,.43,.46),(.23,.44,.55),(.26,.43,.49)],'steel_light')
 m.box((.10,.42,-.115),(.11,.28,.10),'leather')
 for x in [.063,.1,.136]:
  m.beam((x,.44,-.13),(x,.66,-.18),.014,.014,'wood_gold')
  m.box((x,.636,-.17),(.032,.069,.012),'ivory')
 return emit(m)

def giant_unit(boss=False):
 m=MeshMaker('boss' if boss else 'giant');s=1.24 if boss else 1
 def p(x,y,z):return(x*s,y*s,z*s)
 def box(c,size,col):m.box(p(*c),tuple(v*s for v in size),col)
 def beam(a,b,w,d,col):m.beam(p(*a),p(*b),w*s,d*s,col)
 for x in [-.20,.20]:
  box((x,.12,.035),(.30,.24,.42),'steel_dark')
  box((x,.38,0),(.26,.31,.28),'red_shadow')
 m.cylinder(p(0,.91,0),.47*s,.69*s,'red_shadow',n=6,radius_top=.53*s)
 box((0,.68,.34),(.72,.10,.08),'leather')
 box((0,.69,.395),(.18,.16,.045),'gold')
 for side in [-1,1]:
  m.cylinder(p(side*.48,1.14,0),.22*s,.27*s,'steel_dark',n=5)
  beam((side*.50,1.09,0),(side*.66,.75,.16),.25,.25,'giant_skin')
  box((side*.66,.72,.18),(.27,.24,.28),'red' if boss else 'giant_skin')
 m.cylinder(p(0,1.51,.02),.265*s,.37*s,'giant_skin',n=8)
 m.rings(p(0,0,0),[(1.67*s,.31*s),(1.86*s,.28*s),(1.95*s,.15*s)],'steel_dark',n=6)
 # Broad split visor differs from the reference's bare head and black hair.
 box((0,1.64,.267),(.50,.12,.045),'red_shadow')
 box((-.115,1.65,.298),(.125,.026,.017),'ivory');box((.115,1.65,.298),(.125,.026,.017),'ivory')
 if boss:
  # Four brass chimney-like horns + heavy six-sided mace, no giant-sword clone.
  for x in [-.24,.24]:
   beam((x,1.79,0),(x*1.6,2.04,-.04),.11,.10,'gold')
   beam((x*1.6,2.04,-.04),(x*1.55,2.22,.03),.09,.08,'gold_light')
  beam((.66,.65,.18),(.74,1.37,.37),.09,.09,'wood')
  m.cylinder(p(.74,1.56,.37),.26*s,.39*s,'steel_dark',n=6)
  for i in range(6):
   a=i*math.tau/6;m.cylinder(p(.74+math.cos(a)*.24,1.56,.37+math.sin(a)*.24),.085*s,.21*s,'gold',n=4,radius_top=0)
  box((0,1.10,.43),(.28,.34,.05),'gold');diamond_badge(m,0,1.10*s,.461*s,.095*s,'ivory')
 else:
  beam((.66,.61,.22),(.73,1.05,.25),.075,.075,'leather')
  box((.73,.95,.25),(.40,.08,.11),'gold')
  # Two pale ridges imply a hot blade without another material or bloom draw.
  m.face([p(.62,1.01,.26),p(.60,1.70,.27),p(.74,1.91,.27),p(.84,1.71,.27),p(.84,1.01,.26)],'blade_glow')
  m.face([p(.84,1.01,.28),p(.84,1.71,.29),p(.74,1.91,.29),p(.60,1.70,.29),p(.62,1.01,.28)],'blade_glow')
  beam((.73,1.02,.298),(.74,1.73,.298),.055,.018,'ivory')
 return emit(m)

def prop_assets():
 m=MeshMaker('barrel')
 m.rings((0,0,0),[(0,.23),(.07,.265),(.28,.285),(.51,.26),(.56,.225)],'wood_light',n=10)
 for y in [.11,.44]:m.cylinder((0,y,0),.278,.055,'steel_dark',n=10)
 for i in range(10):
  a=i*math.tau/10;m.beam((math.cos(a)*.278,.13,math.sin(a)*.278),(math.cos(a)*.28,.42,math.sin(a)*.28),.012,.014,'wood')
 emit(m)
 m=MeshMaker('crate');m.box((0,.25,0),(.56,.50,.52),'wood_light')
 for x in [-.23,.23]:
  m.box((x,.25,.27),(.068,.50,.048),'wood');m.box((x,.25,-.27),(.068,.50,.048),'wood')
 for y in [.05,.45]:m.box((0,y,.27),(.56,.07,.05),'wood')
 m.beam((-.23,.07,.29),(.23,.43,.29),.055,.032,'wood_gold');emit(m)
 m=MeshMaker('stump');m.cylinder((0,.17,0),.34,.34,'wood',n=7,radius_top=.255)
 m.cylinder((0,.348,0),.254,.018,'wood_gold',n=7)
 m.cylinder((0,.358,0),.15,.005,'wood_light',n=7)
 for i in range(5):
  a=i*math.tau/5;m.beam((math.cos(a)*.17,.19,math.sin(a)*.17),(math.cos(a)*.48,.027,math.sin(a)*.48),.15,.12,'wood')
 emit(m)
 m=MeshMaker('well')
 m.cylinder((0,.05,0),.62,.10,'stone_shadow',n=10)
 for i in range(10):
  a=i*math.tau/10;x,z=math.cos(a)*.43,math.sin(a)*.43;m.box((x,.32,z),(.25,.53,.22),'stone_light',Matrix.Rotation(-a,3,'Y'))
 m.cylinder((0,.29,0),.34,.018,'water',n=10)
 for x in [-.51,.51]:m.box((x,.90,0),(.085,1.17,.085),'wood')
 m.beam((-.6,1.29,0),(.6,1.29,0),.10,.10,'wood_light')
 m.beam((0,1.28,0),(0,.46,0),.018,.018,'ivory')
 m.wedge((0,1.53,0),(1.48,.45,.85),'roof');emit(m)
 m=MeshMaker('bell')
 for x in [-.26,.26]:m.box((x,.60,0),(.10,1.20,.12),'wood')
 m.box((0,1.19,0),(.74,.12,.19),'wood_light')
 m.rings((0,0,0),[(.58,.24),(.68,.22),(.94,.13),(1.01,.075)],'gold',n=8)
 m.cylinder((0,.58,0),.065,.17,'steel_dark',n=6);emit(m)
 for damaged in [False,True]:
  m=MeshMaker('palisade_damaged' if damaged else 'palisade')
  for i in range(7):
   x=(i-3)*.24;h=(.38 if i in (2,3,5) else .69) if damaged else .90+.06*math.sin(i*2)
   m.cylinder((x,h*.5,0),.115,h,'wood' if damaged else 'wood_light',n=5)
   m.cylinder((x,h+.105,0),.116,.21,'wood_gold',n=5,radius_top=0)
  for y in [.22,.57]:m.box((0,y,-.083),(1.64,.07,.09),'wood')
  for x in [-.58,0,.58]:m.beam((x,.1,.43),(x,.49,.14),.11,.105,'wood')
  emit(m)
 m=MeshMaker('enemy_watchtower')
 for x in [-.4,.4]:
  for z in [-.4,.4]:m.box((x,.86,z),(.14,1.72,.14),'wood')
 for z in [-.4,.4]:m.beam((-.4,.2,z),(.4,1.43,z),.10,.075,'wood_light')
 m.box((0,1.65,0),(1.22,.15,1.13),'wood_light')
 for x in [-.50,.50]:m.box((x,1.86,0),(.09,.37,1.1),'wood')
 m.box((0,1.86,-.5),(1.1,.36,.075),'wood')
 m.box((0,1.65,.61),(.39,.70,.023),'red');diamond_badge(m,0,1.55,.63,.11,'ivory')
 emit(m)

def build_phase1_models():
 for stage in [1,2,3]:tower_stage(stage);barracks_stage(stage);castle_stage(stage)
 tower_stage(2,'tower_base');barracks_stage(2,'barracks');castle_stage(2,'castle_gate')
 enemy_portal();archer();archer(True);giant_unit();giant_unit(True);prop_assets()
