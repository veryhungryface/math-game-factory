"""Read-only geometry and package contract validation, using Python's standard library."""
import argparse,json,struct,math,hashlib,zipfile
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--game',default='public/g/sunbasket-farm');p.add_argument('--report',default='logs/manual-20260908-sunbasket-v3/assets/kit-verification.json');p.add_argument('--skip-zip',action='store_true');a=p.parse_args();game=Path(a.game);assets=game/'assets';manifest=json.loads((assets/'manifest.json').read_text());report={'models':[],'raster':[],'errors':[]}
heights={'carrot':1,'carrot-young':.60,'carrot-sprout':.25,'strawberry':1,'corn':1}
def expect(c,m):
 if not c:report['errors'].append(m)
def glb_bounds(g):
 nodes=g.get('nodes',[]);parents={c:i for i,n in enumerate(nodes) for c in n.get('children',[])}
 def apply(n,p):
  if 'matrix' in n:
   m=n['matrix'];return [sum(m[c*4+r]*p[c] for c in range(3))+m[12+r] for r in range(3)]
  p=[p[i]*n.get('scale',[1,1,1])[i] for i in range(3)];x,y,z,w=n.get('rotation',[0,0,0,1]);q=[x,y,z]
  cross=lambda a,b:[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]
  uv=cross(q,p);uuv=cross(q,uv);return [p[i]+2*(w*uv[i]+uuv[i])+n.get('translation',[0,0,0])[i] for i in range(3)]
 pts=[]
 for idx,n in enumerate(nodes):
  if 'mesh' not in n:continue
  for prim in g['meshes'][n['mesh']]['primitives']:
   acc=g['accessors'][prim['attributes']['POSITION']]
   for k in range(8):
    p=[acc['max' if (k>>i)&1 else 'min'][i] for i in range(3)];cur=idx
    while cur is not None:p=apply(nodes[cur],p);cur=parents.get(cur)
    pts.append(p)
 return {'min':[min(p[i] for p in pts) for i in range(3)],'max':[max(p[i] for p in pts) for i in range(3)]}
for path in sorted(assets.glob('*.glb')):
 raw=path.read_bytes();magic,version,total=struct.unpack_from('<III',raw);expect(magic==0x46546c67 and version==2 and total==len(raw),path.name+' invalid header');length,kind=struct.unpack_from('<II',raw,12);g=json.loads(raw[20:20+length]);info=manifest['assets'][path.stem]
 expect(not g.get('images') and not g.get('textures'),path.name+' external texture');expect(all('uri' not in b for b in g.get('buffers',[])),path.name+' external buffer')
 prims=[pr for m in g.get('meshes',[]) for pr in m['primitives']];tris=sum(g['accessors'][pr['indices']]['count']//3 if 'indices' in pr else g['accessors'][pr['attributes']['POSITION']]['count']//3 for pr in prims)
 expect(all('COLOR_0' in p['attributes'] for p in prims),path.name+' missing vertex color');expect(tris==info['triangles'],path.name+' triangle count mismatch');expect(len(prims)==info['drawCalls'],path.name+' draw-call mismatch')
 actual_bounds=glb_bounds(g);expect(all(abs(actual_bounds[b][i]-info['gltfBounds'][b][i])<1e-5 for b in ['min','max'] for i in range(3)),path.name+' exported bounds mismatch');expect(abs(actual_bounds['min'][1])<1e-5,path.name+' ground mismatch');expect(all(math.isfinite(n) for bound in info['gltfBounds'].values() for n in bound),path.name+' nonfinite bounds')
 names={n.get('name') for n in g.get('nodes',[])};expect(all(p['name'] in names for p in info['parts']),path.name+' missing part');expect(all(name in names for name in info.get('anchors',{})),path.name+' missing anchor')
 if path.stem in heights:
  expect(len(prims)==1,path.name+' crop must be one primitive');expect(abs(info['gltfBounds']['max'][1]-heights[path.stem])<1e-5,path.name+' wrong growth height')
 if path.stem=='farmhouse':
  door=next((n for n in g['nodes'] if n.get('name')=='farmhouse_door'),{});expect(abs(door.get('translation',[0,0,0])[0]+.45)<1e-5,'Door hinge pivot missing')
 if info.get('sha256'):expect(hashlib.sha256(raw).hexdigest()==info['sha256'],path.name+' sha mismatch')
 report['models'].append({'file':path.name,'triangles':tris,'primitives':len(prims),'bytes':len(raw),'parts':list(p['name'] for p in info['parts']),'anchors':list(info.get('anchors',{})),'bounds':actual_bounds})
for path,size in [(game/'thumb.png',[1200,630]),(game/'square.png',[1080,1080])]+[(assets/('icon-'+crop+'.png'),[192,192]) for crop in ['carrot','strawberry','corn']]+[(assets/'title-logo.png',[1200,300])]:
 raw=path.read_bytes();actual=list(struct.unpack('>II',raw[16:24]));expect(actual==size,path.name+' PNG size');report['raster'].append({'file':path.name,'size':actual})
if not a.skip_zip:
 with zipfile.ZipFile(assets/'sunbasket-farm-source.zip') as z:
  expect(z.testzip() is None,'ZIP CRC');prefix='sunbasket-farm-source/';checks=[]
  for path in sorted(assets.glob('*.glb')):
   expect(z.read(prefix+'models/'+path.name)==path.read_bytes(),'ZIP model mismatch '+path.name);checks.append(path.name)
  for path in [game/n for n in ['index.html','game.js','style.css','math.mjs']]+sorted([p for p in assets.iterdir() if p.suffix in ['.js','.mjs']]):expect(z.read(prefix+'runtime/'+str(path.relative_to(game)))==path.read_bytes(),'ZIP runtime mismatch '+str(path))
  expect(z.read(prefix+'manifest.json')==(assets/'manifest.json').read_bytes(),'ZIP manifest mismatch');report['zip']={'crc':'pass','modelFilesCompared':checks,'entries':len(z.namelist())}
report['status']='pass' if not report['errors'] else 'fail';out=Path(a.report);out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'status':report['status'],'models':len(report['models']),'triangles':sum(m['triangles'] for m in report['models']),'errors':report['errors']}));raise SystemExit(bool(report['errors']))
