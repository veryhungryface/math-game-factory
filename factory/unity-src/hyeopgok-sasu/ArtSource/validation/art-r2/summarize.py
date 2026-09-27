"""Final evidence index. Requires current-build reports; never self-scores art."""
from pathlib import Path
import json,hashlib,gzip,datetime
r=Path.cwd(); here=Path(__file__).resolve().parent; game=r/'public/g/hyeopgok-sasu'
def read(p):return json.loads(p.read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
capture=read(here/'final/report.json');qa=read(here/'qa/report.json');packs=read(here/'packs-final/report.json');economy=read(here/'economy.json');regression=read(here/'regression-final/report.json') if (here/'regression-final/report.json').exists() else read(here/'regression-final.json')
files=[]
for p in sorted(game.rglob('*')):
 if not p.is_file() or p.name.startswith('.'):continue
 b=p.read_bytes();compressed=b[:2]==b'\x1f\x8b' or p.suffix.lower() in ('.png','.jpg','.webp')
 files.append({'file':str(p.relative_to(game)),'rawBytes':len(b),'transferBytes':len(b) if compressed else len(gzip.compress(b,compresslevel=9,mtime=0)),'sha256':sha(p)})
protected={f:sha(r/f)==h for f,h in read(here/'protected-before.json').items()}
steady=[m for m in capture['metrics'] if m['label'].endswith('steady15s')]
summary={'time':datetime.datetime.now(datetime.timezone.utc).isoformat(),'status':'Local art round 2; independent art verdict pending; no push or deploy','build':capture['build'],'qa':{k:qa.get(k) for k in ('passed','total','failed','fatal','auto_pass','render','perf')},'nonfatal':[x for x in qa.get('checks',[]) if not x['ok'] and not x.get('fatal')],'drawsMaximum':max(m['draws']['max'] for m in capture['metrics']),'steadyAllocationZero':all(m['managedAllocatedBytes']['totals'][:3]==[0,0,0] for m in steady),'allocationScope':'15 steady seconds after 4 second UI warmup; Game.Update, Battle.Update, Environment.LateUpdate. Loading/input/question/menu transitions and shared engine/bridge excluded. Raw event allocations retained in final/report.json.','steady':steady,'gzipTransferBytes':sum(f['transferBytes'] for f in files),'rawBytes':sum(f['rawBytes'] for f in files),'files':files,'protectedUnchanged':protected,'captureErrors':capture['errors'],'packCapture':{'frames':len(packs['frames']),'errors':packs['errors'],'fontWarnings':packs['warnings'],'packsUnchanged':packs['packsUnchanged'],'buildMatches':packs['build']==capture['build']},'economy':{'pass':economy['pass'],'cases':len(economy['cases']),'buildMatches':economy['buildHashes']==capture['build']},'regression':{'pass':regression.get('pass'), 'errors':regression.get('errors'),'buildMatches':regression.get('buildHashes',regression.get('build'))==capture['build']},'nativeBots':{k:v for k,v in read(here/'bots/bot-results.json').items() if k.endswith('Passed') or k in ('modelErrors','nominalChanceGate','uniformChanceGate','uniformPaths','failures')}}
(here/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:summary[k] for k in ['qa','drawsMaximum','steadyAllocationZero','gzipTransferBytes','packCapture','economy','regression']},ensure_ascii=False,indent=2))
