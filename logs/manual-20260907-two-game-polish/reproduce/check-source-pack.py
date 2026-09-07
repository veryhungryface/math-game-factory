from pathlib import Path
from hashlib import sha256
import json
import sys
import zipfile

slug = sys.argv[1]
assert slug in ('twin-forts', 'sunbasket-farm')
game = Path('public/g') / slug
bundles = list((game / 'assets').glob('*source*.zip'))
assert len(bundles) == 1
bundle = bundles[0]
report = {'slug': slug, 'zip': str(bundle), 'sha256': sha256(bundle.read_bytes()).hexdigest(), 'models': [], 'runtime': []}
with zipfile.ZipFile(bundle) as z:
    assert z.testzip() is None, 'ZIP CRC mismatch'
    names = z.namelist()
    assert any(n.endswith('.blend') for n in names), 'Blender source missing'
    assert any(n.endswith('.py') for n in names), 'Portable generator missing'
    for glb in sorted((game / 'assets').glob('*.glb')):
        matches = [n for n in names if Path(n).name == glb.name]
        assert len(matches) == 1, f'{glb.name}: missing/duplicate packed model'
        actual = z.read(matches[0])
        assert actual == glb.read_bytes(), f'{glb.name}: packed model differs'
        report['models'].append({'path': glb.name, 'entry': matches[0], 'sha256': sha256(actual).hexdigest(), 'matches': True})
    runtime_files = sorted([p for p in game.iterdir() if p.suffix in ('.html', '.js', '.mjs', '.css')] + [p for p in (game / 'assets').iterdir() if p.suffix in ('.js', '.mjs', '.css')])
    for runtime_file in runtime_files:
        filename = runtime_file.name
        matches = [n for n in names if Path(n).name == filename]
        assert len(matches) == 1, f'{filename}: missing/duplicate runtime source'
        actual = z.read(matches[0])
        assert actual == runtime_file.read_bytes(), f'{filename}: stale runtime source'
        report['runtime'].append({'path': str(runtime_file.relative_to(game)), 'entry': matches[0], 'sha256': sha256(actual).hexdigest(), 'matches': True})
    report['entries'] = names
report['verdict'] = 'pass'
out = Path('logs/manual-20260907-two-game-polish') / slug
out.mkdir(exist_ok=True)
(out / 'source-pack-check.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({'slug': slug, 'verdict': 'pass', 'models': len(report['models']), 'runtime': len(report['runtime'])}))
