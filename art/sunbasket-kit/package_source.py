"""Freeze the source download after the game/runtime files have finished changing."""
import argparse,hashlib,json,struct,zipfile
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--game',default='public/g/sunbasket-farm');p.add_argument('--kit',default=str(Path(__file__).resolve().parent));p.add_argument('--report',default='logs/manual-20260908-sunbasket-v3/assets/source-package.json');a=p.parse_args()
game=Path(a.game);kit=Path(a.kit);assets=game/'assets';archive=assets/'sunbasket-farm-source.zip';prefix='sunbasket-farm-source/'
def sha(data):return hashlib.sha256(data).hexdigest()
def png_size(p):return list(struct.unpack('>II',p.read_bytes()[16:24]))
man=json.loads((assets/'manifest.json').read_text());man['artRevision']=3;man['runtimeRevision']=3
for name,info in man['assets'].items():
 data=(assets/info['file']).read_bytes();info['bytes']=len(data);info['sha256']=sha(data)
man['rasterAssets']={p.name:{'size':png_size(p),'bytes':p.stat().st_size,'sha256':sha(p.read_bytes()),'role':'Exact game crop mesh UI render' if p.name.startswith('icon-') else 'Original wooden Korean title sign'} for p in sorted(assets.glob('*.png'))}
man['coverAssets']={name:{'size':png_size(game/name),'bytes':(game/name).stat().st_size,'sha256':sha((game/name).read_bytes())} for name in ['thumb.png','square.png']}
runtime=[game/name for name in ['index.html','game.js','style.css','math.mjs']]+sorted([p for p in assets.iterdir() if p.suffix in ['.js','.mjs']])
man['runtimeModules']={str(p.relative_to(game)):{'bytes':p.stat().st_size,'sha256':sha(p.read_bytes())} for p in runtime}
source_names=['generate_assets.py','v3_geometry.py','render_details.py','render_kit.py','package_source.py','verify_kit.py','README.md','RUNTIME.md','palette-market.json','sunbasket-farm-assets.blend','sample-small-farm.blend','sample-village-market.blend']
man['source']={'file':archive.name,'contents':source_names+['models/*.glb','manifest.json','runtime/index.html','runtime/game.js','runtime/style.css','runtime/math.mjs','runtime/assets/*.mjs'],'note':'Art revision3 models and source. Runtime snapshot requires the complete game checkout. Models and runtime bytes are compared during packaging.'}
man['tooling']={'blender':'5.2.1 LTS, background CLI','imageGeneration':'None. Covers/icons/studios render the project-original meshes.','cost':'Local modeling/rendering only; no model or image API.'}
encoded=(json.dumps(man,ensure_ascii=False,indent=2)+'\n').encode();(assets/'manifest.json').write_bytes(encoded);(kit/'manifest.json').write_bytes(encoded)
content={prefix+n:(kit/n).read_bytes() for n in source_names};content[prefix+'manifest.json']=encoded
for path in sorted(assets.glob('*.glb')):content[prefix+'models/'+path.name]=path.read_bytes()
for path in runtime:content[prefix+'runtime/'+str(path.relative_to(game))]=path.read_bytes()
with zipfile.ZipFile(archive,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
 for name,data in content.items():z.writestr(name,data)
with zipfile.ZipFile(archive) as z:
 assert z.testzip() is None
 for name,data in content.items():assert z.read(name)==data,name
report={'archive':str(archive),'bytes':archive.stat().st_size,'sha256':sha(archive.read_bytes()),'crc':'pass','filesCompared':len(content),'models':len(list(assets.glob('*.glb'))),'runtimeFiles':[str(p.relative_to(game)) for p in runtime],'sourceFiles':source_names}
out=Path(a.report);out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
