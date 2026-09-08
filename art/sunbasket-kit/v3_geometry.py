# Original Sunbasket v3 geometry. Loaded into the generator's Blender helper namespace.
def mark_since(index,tag):
 for o in parts[index:]:o['asset_part']=tag

def blade(name,start,end,width,col='leaflight',curve=.10,serrate=False):
 start=Vector(start);d=Vector(end)-start;side=d.cross(Vector((0,0,1)))
 if side.length<.001:side=Vector((1,0,0))
 side.normalize();v=[];n=7
 for i in range(n+1):
  t=i/n;middle=start+d*t+Vector((0,0,math.sin(t*math.pi)*curve));w=width*math.sin(t*math.pi)**.78
  if serrate and i%2:w*=.64
  v.extend([middle-side*w,middle+Vector((0,0,w*.20)),middle+side*w])
 f=[]
 for i in range(n):
  k=i*3;f.extend([(k,k+3,k+4,k+1),(k+1,k+4,k+5,k+2)])
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);scene.collection.objects.link(o);finish(o,name,col,smooth=True)
 # glTF material is double-sided: folded midrib needs no invisible thickness geometry.
 return o

def build_carrot(stage='ripe'):
 if stage=='ripe':
  lathe('Carrot tapered curved root',[(0,.012),(.055,.035),(.16,.091),(.32,.155),(.46,.19),(.54,.165),(.58,.075),(.59,.018)],col='orange',n=12)
  for o in parts:
   for v in o.data.vertices:v.co.x+=.05*(1-v.co.z/.6)**2
  for i,z in enumerate([.20,.32,.43]):
   beam('Root shallow growth ridge',(-.055,-(.10+i*.022),z),(.075,-(.10+i*.022),z+.018),.013,col='orangehi',b=0)
 else:
  lathe('Young carrot shoulder',[(0,.013),(.12,.065),(.25,.09),(.31,.045)],col='orange',n=10)
 base=.55 if stage=='ripe' else .26
 for i in range(7 if stage=='ripe' else 4):
  th=i*2.399;reach=.28 if stage=='ripe' else .19;top=base+.34+(i%3)*.055
  end=(math.cos(th)*reach,math.sin(th)*reach,top)
  blade('Feathered carrot frond',(0,0,base),end,.078 if stage=='ripe' else .055,'leaflight' if i%2 else 'leafsun',.16,True)
  if stage=='ripe':
   for t in [.36,.61]:
    p=Vector((0,0,base)).lerp(Vector(end),t);p.z+=.10
    for s in [-1,1]:leaf('Carrot divided leaflet',p,p+Vector((math.cos(th+s*.9)*.12,math.sin(th+s*.9)*.12,.085)),.030,'leafsun' if i%3==0 else 'leaflight',.018)
 close('carrot' if stage=='ripe' else 'carrot-young',normalize=1 if stage=='ripe' else .60)

def build_sprout():
 cyl('Seedling curved stalk',(0,0,.13),.016,.26,'leaf',n=7)
 for i in range(3):
  th=i*2.15;blade('Seedling rounded cotyledon',(0,0,.18),(.24*math.cos(th),.24*math.sin(th),.34+(i%2)*.05),.085,'leaflight',.085)
 close('carrot-sprout',normalize=.25)

def planter(name,x,y,z,w=.56):
 box(name+' timber box',(x,y,z),(w,.27,.18),'woodlight',b=.03)
 box(name+' cream rim',(x,y,z+.10),(w+.035,.29,.045),'ivory',b=.012)
 box(name+' dark soil',(x,y,z+.12),(w-.05,.22,.015),'soil',b=0)
 for i in range(4):
  px=x+(i-1.5)*w*.20
  for k in range(3):blade(name+' folded leaf',(px,y,z+.11),(px+.12*math.cos(k*2.2),y+.12*math.sin(k*2.2),z+.25),.042,'leaflight',.04)
  for k in range(5):sphere(name+' cream petal',(px+.037*math.cos(k*math.tau/5),y-.025+.037*math.sin(k*math.tau/5),z+.28),(.042,.034,.018),'ivory' if i%2 else 'peach',segments=7,rings=4)
  sphere(name+' flower center',(px,y-.025,z+.297),(.022,.022,.012),'gold',segments=7,rings=3)

def build_house():
 i=len(parts)
 box('House dressed stone foundation',(0,-.10,.14),(2.98,2.72,.28),'stone2',b=.09)
 # Front is assembled around the deep doorway, rather than burying a door on a solid block.
 box('House rear plaster volume',(0,.16,1.47),(2.64,1.87,2.22),'cream',b=.09)
 for x in [-.92,.92]:box('House front wall wing',(x,-.965,1.48),(.80,.34,2.22),'cream',b=.065)
 box('House doorway lintel wall',(0,-.965,2.31),(1.15,.34,.57),'cream',b=.04)
 roof('House gable infill',[(-1.32,2.52),(0,3.57),(1.32,2.52)],2.15,'peach',thick=.70)
 for x in [-1.28,1.28]:box('House corner frame',(x,-1.095,1.50),(.15,.15,2.26),'woodwarm',b=.027)
 for y in [-.55,.20,.93]:box('Right wall framing',(1.32,y,1.45),(.08,.13,2.05),'woodlight',b=.018)
 for z in [.50,.73]:box('Low facade siding',(0,-1.15,z),(2.61,.055,.15),'peach',b=.012)
 mark_since(i,'walls')
 i=len(parts)
 profile=[(-1.73,2.53),(-1.48,2.57),(-.92,3.13),(-.46,3.45),(0,3.66),(.51,3.42),(.99,3.09),(1.50,2.57),(1.73,2.53)]
 roof('Thick swept teal roof',profile,2.82,'roofdark',thick=.19)
 # Four overlapping shingle courses follow the bowed slope, with staggered broad seams.
 for side in [-1,1]:
  for row in range(4):
   xa=row*.415;xb=(row+1)*.415;za=3.69-.48*xa-.13*xa*xa;zb=3.69-.48*xb-.13*xb*xb
   for col in range(6):
    y=-1.39+col*.466+(row%2)*.01
    tile=roof('Overlapping teal shingle',[(side*xa,za+.025),(side*xb,zb+.03)],.444,'rooflight' if (row+col)%7==0 else 'roof',loc=(0,y+.222,0),thick=.055)
 for y in [-1.47,1.47]:
  for j in range(len(profile)-1):beam('Cream swept bargeboard',(profile[j][0],y,profile[j][1]-.09),(profile[j+1][0],y,profile[j+1][1]-.09),.15,.14,'ivory',.022)
 beam('Rounded ridge cap',(0,-1.48,3.70),(0,1.48,3.70),.13,.15,'rooflight',.045)
 mark_since(i,'roof')
 i=len(parts)
 arch('Recessed arched entry shadow',(0,-1.04,.31),1.10,1.28,'dark',.05)
 arch('Deep cream entry arch',(0,-1.22,.31),1.18,1.27,'ivory',.25,trim=.12)
 for x in [-.60,.60]:box('Deep entry stone jamb',(x,-1.30,.99),(.14,.33,1.32),'ivory',b=.025)
 mark_since(i,'entry-frame')
 i=len(parts)
 arch('Door solid arched core',(0,-1.075,.36),.90,1.10,'wood',.055)
 for j in range(5):
  x=(j-2)*.172;h=1.10+math.sqrt(max(0,.445**2-x*x));box('Individual arched door plank',(x,-1.123,.36+h/2),(.160,.045,h),'woodwarm' if j%2 else 'woodlight',b=.012)
 for z in [.66,1.40]:box('Door iron strap',(0,-1.16,z),(.81,.025,.041),'metal',b=.008)
 cyl('Brass door latch',(.29,-1.18,1.07),.047,.035,'gold2',n=10,rot=(math.pi/2,0,0))
 mark_since(i,'door')
 i=len(parts)
 for x in [-.94,.94]:
  box('Window deep dark recess',(x,-1.154,1.74),(.54,.045,.70),'dark',b=.022)
  box('Window glass',(x,-1.182,1.76),(.405,.027,.55),'glass',b=.018,gloss=True)
  for dx in [-.27,.27]:box('Window cream jamb',(x+dx,-1.23,1.76),(.077,.13,.75),'ivory',b=.022)
  for zz in [1.395,2.125]:box('Window cream lintel',(x,-1.23,zz),(.615,.15,.08),'ivory',b=.022)
  for dx in [-.38,.38]:
   box('Teal shutter panel',(x+dx,-1.18,1.76),(.17,.07,.65),'roof',b=.018)
   for zz in [1.56,1.73,1.90]:box('Shutter slat',(x+dx,-1.225,zz),(.17,.04,.03),'rooflight',b=.007)
  box('Window mullion',(x,-1.25,1.76),(.04,.06,.56),'ivory',b=.01);box('Window crossbar',(x,-1.25,1.76),(.44,.06,.04),'ivory',b=.01)
  planter('Window flowers',x,-1.31,1.27,.57)
 cyl('Attic thick window ring',(0,-1.49,2.96),.26,.10,'ivory',n=18,rot=(math.pi/2,0,0),b=.015)
 cyl('Attic blue glass',(0,-1.55,2.96),.19,.03,'glass',n=18,rot=(math.pi/2,0,0))
 for sz in [(.36,.03,.035),(.035,.03,.36)]:box('Attic cream cross',(0,-1.575,2.96),sz,'cream',b=.006)
 mark_since(i,'windows')
 i=len(parts)
 box('Porch lower stone step',(0,-1.69,.095),(1.54,.66,.19),'stone2',b=.065)
 box('Porch upper tread',(0,-1.50,.225),(1.35,.76,.15),'ivory',b=.04)
 # A projecting curved porch changes the whole building silhouette and creates a true shadowed entry.
 for x in [-.63,.63]:
  box('Porch stone post shoe',(x,-1.82,.42),(.23,.24,.28),'cream',b=.033)
  box('Porch shaped timber post',(x,-1.82,1.29),(.14,.16,1.57),'woodlight',b=.025)
  beam('Porch angled support',(x,-1.82,1.69),(x,-1.36,2.07),.09,col='woodwarm',b=.014)
 roof('Small curved porch canopy',[(-.86,2.04),(-.55,2.19),(0,2.43),(.55,2.19),(.86,2.04)],.99,'roof',loc=(0,-1.56,0),thick=.10)
 for j in range(4):
  xs=[-.86,-.55,0,.55,.86];zs=[2.04,2.19,2.43,2.19,2.04];beam('Porch cream fascia',(xs[j],-2.07,zs[j]-.04),(xs[j+1],-2.07,zs[j+1]-.04),.10,.09,'ivory',.015)
 mark_since(i,'porch')
 i=len(parts)
 box('Chimney warm plaster',(.91,.58,3.30),(.40,.43,1.08),'peach',b=.045)
 for zz in [3.10,3.37,3.65]:box('Chimney brick collar',(.91,.58,zz),(.45,.48,.075),'cream',b=.012)
 box('Chimney pale cap',(.91,.58,3.87),(.55,.57,.14),'ivory',b=.04);box('Chimney dark opening',(.91,.58,3.945),(.29,.31,.011),'dark',b=.01)
 mark_since(i,'chimney')
 close('farmhouse')

def build_market():
 i=len(parts)
 for x in [-1.27,1.27]:
  for y in [-.57,.54]:
   box('Market tapered foot',(x,y,.10),(.26,.27,.20),'wood',b=.035)
   box('Market solid timber post',(x,y,1.18),(.17,.18,2.18),'woodlight',b=.035)
  beam('Market side diagonal',(x,-.56,.28),(x,.54,.91),.095,col='woodwarm',b=.02)
 box('Market slatted counter',(0,-.02,.84),(2.94,1.36,.18),'woodwarm',b=.045)
 for j in range(7):box('Market front tongue-groove board',(-1.20+j*.40,-.60,.46),(.38,.09,.62),'woodlight' if j%3 else 'woodwarm',b=.017)
 for z in [.22,.74]:box('Market broad horizontal rail',(0,-.665,z),(2.73,.09,.10),'woodwarm',b=.02)
 for y,z in [(-.90,1.97),(.26,2.51)]:beam('Awning crossbar',(-1.55,y,z),(1.55,y,z),.09,col='woodlight',b=.018)
 mark_since(i,'frame')
 i=len(parts)
 # Generous fabric catenary and gently bowed width replace the taut segmented cap.
 for stripe in range(8):
  x0=-1.53+stripe*.3825;v=[];ny=12;nx=3
  for ix in range(nx+1):
   x=x0+ix*.3825/nx
   for j in range(ny+1):
    t=j/ny;y=-.96+t*1.78;z=2.06+.56*math.sin(t*math.pi*.91)-.09*(1-(x/1.53)**2)
    v.append((x,y,z))
  faces=[]
  for ix in range(nx):
   for j in range(ny):
    k=ix*(ny+1)+j;faces.append((k,k+ny+1,k+ny+2,k+1))
  me=bpy.data.meshes.new('Woven curved stripe');me.from_pydata(v,[],faces);me.update();o=bpy.data.objects.new('Woven curved stripe',me);scene.collection.objects.link(o);finish(o,'Soft curved canvas','ivory' if stripe%2 else 'peach',smooth=True)
  mod=o.modifiers.new('Canvas thickness','SOLIDIFY');mod.thickness=.024;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
  # Circular hanging scallops, with stitched rolled lips.
  pts=[(x0,-.965,1.99),(x0+.3825,-.965,1.99)]+[(x0+.19125+.19125*math.cos(k*math.pi/8),-.965,1.93-.125*math.sin(k*math.pi/8)) for k in range(9)]
  me=bpy.data.meshes.new('Canvas scallop');me.from_pydata(pts,[],[tuple(range(len(pts)))]);me.update();o=bpy.data.objects.new('Canvas scallop',me);scene.collection.objects.link(o);finish(o,'Fabric scallop','ivory' if stripe%2 else 'peach')
 mark_since(i,'awning')
 i=len(parts)
 for k in range(3):
  x=-.91+k*.91
  box('Produce tray dark bottom',(x,-.15,.984),(.83,.91,.10),'wood',b=.02)
  for dx in [-.405,.405]:box('Produce tray raised sides',(x+dx,-.15,1.09),(.055,.90,.17),'woodlight',b=.012)
  for y in [-.61,.30]:box('Produce tray low edge',(x,y,1.06),(.85,.055,.13),'woodwarm',b=.012)
  for row in range(2):
   for j in range(3):
    px=x+(j-1)*.22;py=-.39+row*.30
    if k==0:
     o=lathe('Displayed carrot',[(0,.015),(.11,.075),(.28,.065),(.31,.025)],(px,py,1.01),'orange',n=8);o.rotation_euler=(.55,.1,0)
     leaf('Displayed carrot greens',(px,py-.14,1.29),(px+.10,py-.18,1.42),.045,'leaflight',.025)
    elif k==1:sphere('Displayed ripe apple',(px,py,1.16),(.12,.12,.13),'redhi' if j%2 else 'red',segments=10,rings=6)
    else:lathe('Displayed golden corn',[(0,.055),(.04,.085),(.25,.08),(.32,.018)],(px,py,1.00),'gold',n=10)
 mark_since(i,'produce')
 i=len(parts)
 for x in [-.25,.25]:beam('Sun sign hanging cord',(x,-.96,1.99),(x,-.99,1.61),.02,col='wood',b=0)
 box('Cream hanging sun plaque',(0,-1.0,1.58),(.84,.085,.31),'cream',b=.07)
 cyl('Original sun emblem',(0,-1.05,1.59),.096,.028,'gold',n=12,rot=(math.pi/2,0,0))
 for k in range(8):
  th=k*math.tau/8;beam('Sun emblem ray',(.13*math.cos(th),-1.065,1.59+.13*math.sin(th)),(.17*math.cos(th),-1.065,1.59+.17*math.sin(th)),.018,col='gold2',b=0)
 mark_since(i,'sign')
 close('market-stall')

def crown(name,center,scale,col,seed):
 # Deliberately irregular lobed volume, with broad planes and a scalloped leafy lower contour.
 rng=random.Random(seed);n=14;rings=8;v=[]
 for j in range(rings+1):
  phi=math.pi*j/rings
  for k in range(n):
   th=k*math.tau/n;lobes=1+.115*math.sin(th*5+seed)+.07*math.cos(th*3+phi*2)
   r=math.sin(phi)*lobes;z=math.cos(phi)+.065*math.sin(th*4+seed)*math.sin(phi)
   v.append((center[0]+math.cos(th)*r*scale[0],center[1]+math.sin(th)*r*scale[1],center[2]+z*scale[2]))
 f=[]
 for j in range(rings):
  for k in range(n):f.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);scene.collection.objects.link(o);finish(o,name,col,smooth=True)
 ca=o.data.color_attributes['Color'];base=C[col]
 for poly in o.data.polygons:
  height=sum(o.data.vertices[i].co.z for i in poly.vertices)/len(poly.vertices);t=max(0,min(1,(height-(center[2]-scale[2]))/(2*scale[2])));factor=.80+t*.23+(poly.index%5)*.009
  for idx in poly.loop_indices:ca.data[idx].color=(*(c*factor for c in base),1)

def build_tree():
 i=len(parts)
 lathe('Flared orchard trunk',[(0,.24),(.11,.28),(.32,.19),(.92,.14),(1.45,.095),(1.93,.035)],col='woodlight',n=10)
 for s in [-1,1]:beam('Root buttress',(0,0,.24),(s*.37,s*.11,.025),.13,col='woodlight',b=.035)
 for start,end,width in [((0,0,.83),(-.69,.05,1.96),.12),((.01,0,1.12),(.62,.18,2.35),.105),((-.17,.01,1.39),(-.16,-.39,2.63),.085),((.18,.05,1.53),(.89,-.10,2.13),.065)]:beam('Visible branching bough',start,end,width,col='woodlight',b=.025)
 mark_since(i,'trunk')
 i=len(parts)
 clusters=[((-.64,.12,1.99),(.62,.57,.51),'leaflight'),((.57,.21,2.37),(.67,.57,.58),'leaf'),((-.18,.15,2.80),(.61,.55,.51),'leafsun'),((-.17,-.33,2.32),(.49,.41,.39),'leaflight'),((.81,-.02,1.95),(.43,.47,.37),'leaflight')]
 for k,(c,s,col) in enumerate(clusters):
  crown('Lobed orchard leaf cluster',c,s,col,20+k)
  for j in range(7):
   th=j*2.399+k;px=c[0]+math.cos(th)*s[0]*.82;py=c[1]+math.sin(th)*s[1]*.82;pz=c[2]-.11+(j%3)*.16
   blade('Silhouette leaf spray',(px,py,pz),(px+math.cos(th)*.23,py+math.sin(th)*.23,pz+.12),.095,'leafsun' if j%3==0 else col,.05)
 mark_since(i,'canopy')
 i=len(parts)
 for k,(x,y,z) in enumerate([(-.85,-.26,1.80),(-.43,-.46,2.03),(.46,-.40,2.16),(.90,-.25,1.89),(.20,-.36,2.75),(-.38,-.28,2.99),(-1.04,.12,2.00),(.85,.22,2.53)]):
  lathe('Apple with dimple',[(0,.035),(.05,.115),(.17,.15),(.25,.13),(.28,.06),(.265,.019)],(x,y,z-.13),'redhi' if k%3 else 'red',n=10)
  beam('Apple stem',(x,y,z+.13),(x+.014,y,z+.22),.02,col='wood',b=0)
  blade('Apple fruit leaf',(x,y,z+.19),(x+.15,y+.02,z+.23),.044,'leafsun',.025)
 mark_since(i,'fruit')
 close('apple-tree')
