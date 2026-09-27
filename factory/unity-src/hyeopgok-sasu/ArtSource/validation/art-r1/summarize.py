"""Refresh final evidence from completed reports; run from the repository root."""
import gzip
import hashlib
import json
import pathlib
import sys
from datetime import datetime, timezone

root = pathlib.Path.cwd()
here = pathlib.Path(__file__).resolve().parent
game = root / 'public/g/hyeopgok-sasu'
tag = sys.argv[1] if len(sys.argv) > 1 else 'round5'
qa_tag = sys.argv[2] if len(sys.argv) > 2 else 'qa-round5'

def read(path):
    return json.loads(path.read_text())

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

capture = read(here / tag / 'report.json')
qa = read(here / qa_tag / 'report.json')
score = read(here / 'self-score.json')
summary = read(here / 'summary.json')
files = []
for path in sorted(game.rglob('*')):
    if not path.is_file() or path.name.startswith('.'):
        continue
    data = path.read_bytes()
    compressed = data[:2] == b'\x1f\x8b' or path.suffix.lower() in ('.png', '.jpg', '.webp')
    files.append(dict(file=str(path.relative_to(game)), bytes=len(data),
                      transferBytes=len(data) if compressed else len(gzip.compress(data, compresslevel=9, mtime=0)),
                      sha256=sha(path)))
steady = [m for m in capture['metrics'] if m['label'].endswith('steady15s')]
summary.update(at=datetime.now(timezone.utc).isoformat(),
    status=f"로컬 아트 구현/검증, 내부 시각평균{score['mean']:.3f}; 사용자목표8.5 달성={score['meanTargetMet']}",
    finalCapture=tag, finalQa=qa_tag, build=capture['build'],
    qa={k:qa[k] for k in ('passed','total','failed','fatal','auto_pass','render','perf')},
    qaNonfatal=[c for c in qa.get('checks',[]) if not c['ok'] and not c.get('fatal')],
    files=files)
summary['draws']['maximum'] = max(m['draws']['max'] for m in capture['metrics'])
summary['allocation']['steady'] = steady
summary['allocation']['steadyZero'] = all(m['managedAllocatedBytes']['totals'][:3] == [0,0,0] for m in steady)
summary['allocation']['eventEvidence'] = tag + '/report.json'
summary['size'].update(rawBytes=sum(f['bytes'] for f in files), gzipTransferBytes=sum(f['transferBytes'] for f in files))
summary['size']['gzipPass'] = summary['size']['gzipTransferBytes'] < summary['size']['gzipGateBytes']
summary['sourcesUnchanged'] = {p:sha(root/p)==h for p,h in read(here/'source-baseline.json').items()}
summary['externalPackChanges'] = {
    'policy':'아트 작업은 팩/생성기/index를 읽기만 했다. 외부 동시 수정은 되돌리거나 커밋에 포함하지 않는다.',
    'changedFromInitialSnapshot':[p for p,ok in summary['sourcesUnchanged'].items() if not ok and '/packs/' in p],
    'currentCapture':{k:capture.get(k) for k in ('packsAtStart','packsAtEnd','packsStable')},
    'compatibility':'bots-final/source-after.json stable + regression-round5-isolated passed with current u7; final capture also checks consumed pack hashes.'}
(here/'summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:summary[k] for k in ('status','draws','size','sourcesUnchanged')},ensure_ascii=False,indent=2))
