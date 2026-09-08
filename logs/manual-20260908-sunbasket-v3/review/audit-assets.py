"""Static final GLB/source-pack audit. Run in the isolated repository after asset freeze."""
import hashlib, json, pathlib, struct, subprocess, zipfile

ROOT = pathlib.Path(__file__).resolve().parents[3]
OUT = pathlib.Path(__file__).resolve().parent
GAME = ROOT / 'public/g/sunbasket-farm'
assert pathlib.Path.cwd().resolve() == ROOT, 'Run from the isolated checkout root'

def sha(data):
    return hashlib.sha256(data).hexdigest()

def glb(data):
    magic, version, size = struct.unpack_from('<4sII', data)
    assert magic == b'glTF' and version == 2 and size == len(data)
    offset, document, binary = 12, None, b''
    while offset < size:
        length, kind = struct.unpack_from('<II', data, offset)
        content = data[offset + 8:offset + 8 + length]
        assert len(content) == length
        if kind == 0x4E4F534A:
            document = json.loads(content)
        elif kind == 0x004E4942:
            binary = content
        offset += length + 8
    assert offset == size and document
    return document, binary

issues, records = [], []
manifest = json.loads((GAME / 'assets/manifest.json').read_text())
for path in sorted((GAME / 'assets').glob('*.glb')):
    raw = path.read_bytes()
    doc, binary = glb(raw)
    baseline = None
    try:
        old = subprocess.run(['git', 'show', f'76cd9ea:public/g/sunbasket-farm/assets/{path.name}'], cwd=ROOT, check=True, capture_output=True).stdout
        old_doc, old_binary = glb(old)
        baseline = {'file_sha256': sha(old), 'binary_sha256': sha(old_binary), 'same_binary_payload': binary == old_binary}
    except subprocess.CalledProcessError:
        pass
    external = [item['uri'] for key in ('images', 'buffers') for item in doc.get(key, []) if 'uri' in item and not item['uri'].startswith('data:')]
    if external:
        issues.append(f'External GLB resource: {path.name}: {external}')
    primitives = [p for mesh in doc.get('meshes', []) for p in mesh.get('primitives', [])]
    named_nodes = [{'name': n.get('name'), 'mesh': n.get('mesh'), 'extras': n.get('extras'), 'children': n.get('children', [])} for n in doc.get('nodes', []) if n.get('name')]
    triangles = sum(doc['accessors'][p['indices']]['count'] // 3 for p in primitives if p.get('mode', 4) == 4 and 'indices' in p)
    declared = manifest.get('assets', {}).get(path.stem)
    if declared and declared.get('bytes') != len(raw):
        issues.append(f'Manifest byte size mismatch: {path.name}')
    if declared and declared.get('triangles') != triangles:
        issues.append(f'Manifest triangle count mismatch: {path.name}')
    records.append({'file': path.name, 'bytes': len(raw), 'file_sha256': sha(raw), 'binary_sha256': sha(binary), 'triangles': triangles, 'mesh_primitives': len(primitives), 'all_primitives_have_vertex_color': all('COLOR_0' in p.get('attributes', {}) for p in primitives), 'named_nodes': named_nodes, 'baseline': baseline})

archive = GAME / 'assets/sunbasket-farm-source.zip'
with zipfile.ZipFile(archive) as pack:
    bad = pack.testzip()
    if bad:
        issues.append(f'ZIP CRC failure: {bad}')
    matches = []
    for record in records:
        names = [n for n in pack.namelist() if n.endswith('/' + record['file'])]
        matching = [n for n in names if sha(pack.read(n)) == record['file_sha256']]
        if not matching:
            issues.append(f'No byte-identical source ZIP model: {record["file"]}')
        matches.append({'file': record['file'], 'matching_entries': matching})
    runtime = []
    for rel in ['game.js', 'index.html', 'style.css', 'math.mjs', 'assets/farm-polish.mjs', 'assets/crop-growth.mjs']:
        path = GAME / rel
        if not path.exists():
            continue
        raw = path.read_bytes()
        matching = [n for n in pack.namelist() if n.endswith('/' + path.name) and pack.read(n) == raw]
        runtime.append({'file': rel, 'sha256': sha(raw), 'matching_entries': matching})
        if not matching:
            issues.append(f'No byte-identical runtime source ZIP file: {rel}')
    source_entries = [n for n in pack.namelist() if n.endswith(('.blend', '.py', '.md'))]

report = {'verdict': 'fail' if issues else 'pass', 'scope': 'Static GLB headers, component names, primitive/triangle counts, embedded-resource check, binary comparison to v2, and ZIP CRC/byte identity. This does not prove visual quality, correct runtime part manipulation, or performance.', 'baseline_commit': '76cd9ea', 'models': records, 'zip_sha256': sha(archive.read_bytes()), 'zip_models': matches, 'zip_runtime': runtime, 'source_entries': source_entries, 'issues': issues}
(OUT / 'asset-audit.json').write_text(json.dumps(report, indent=2, ensure_ascii=False) + '\n')
print(json.dumps({'verdict': report['verdict'], 'models': len(records), 'changed_binary_models': [r['file'] for r in records if r['baseline'] and not r['baseline']['same_binary_payload']], 'new_models': [r['file'] for r in records if not r['baseline']], 'issues': issues}, ensure_ascii=False))
raise SystemExit(bool(issues))
