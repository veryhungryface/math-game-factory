from pathlib import Path
from hashlib import sha256
import json
import zipfile

game = Path('public/g/sunbasket-farm')
bundle = game / 'assets/sunbasket-farm-source.zip'
report = {'zip': str(bundle), 'sha256': sha256(bundle.read_bytes()).hexdigest(), 'files': []}
with zipfile.ZipFile(bundle) as z:
    assert z.testzip() is None, 'ZIP CRC mismatch'
    names = z.namelist()
    assert any(n.endswith('.blend') for n in names), 'Blender source missing'
    assert any(n.endswith('.py') for n in names), 'Portable generator missing'
    for glb in sorted((game / 'assets').glob('*.glb')):
        matches = [n for n in names if Path(n).name == glb.name]
        assert len(matches) == 1, f'{glb.name}: missing/duplicate packed model'
        expected, actual = glb.read_bytes(), z.read(matches[0])
        assert actual == expected, f'{glb.name}: packed model differs from game'
        report['files'].append({'path': str(glb), 'entry': matches[0], 'bytes': len(actual), 'sha256': sha256(actual).hexdigest(), 'matches': True})
    report['entries'] = names
report['verdict'] = 'pass'
Path('logs/manual-20260907-sunbasket-farm/source-pack-check.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'verdict': report['verdict'], 'models': len(report['files']), 'sha256': report['sha256']}))
