// Lists nodes / parents / world-space (three.js Y-up) bounding boxes of a GLB.
// Usage: node check_glb.mjs <file.glb> [--all]
import fs from 'node:fs';

const file = process.argv[2];
const showAll = process.argv.includes('--all');
const buf = fs.readFileSync(file);
const jsonLen = buf.readUInt32LE(12);
const gltf = JSON.parse(buf.slice(20, 20 + jsonLen).toString('utf8'));
let binOff = 20 + jsonLen;
const binLen = buf.readUInt32LE(binOff);
const bin = buf.slice(binOff + 8, binOff + 8 + binLen);

const qmul = (q, v) => { // rotate v by quaternion q=[x,y,z,w]
  const [x, y, z, w] = q, [vx, vy, vz] = v;
  const ix = w * vx + y * vz - z * vy, iy = w * vy + z * vx - x * vz, iz = w * vz + x * vy - y * vx, iw = -x * vx - y * vy - z * vz;
  return [ix * w + iw * -x + iy * -z - iz * -y, iy * w + iw * -y + iz * -x - ix * -z, iz * w + iw * -z + ix * -y - iy * -x];
};
const xf = (n, p) => { // node local TRS applied to point
  const s = n.scale || [1, 1, 1], r = n.rotation || [0, 0, 0, 1], t = n.translation || [0, 0, 0];
  const q = qmul(r, [p[0] * s[0], p[1] * s[1], p[2] * s[2]]);
  return [q[0] + t[0], q[1] + t[1], q[2] + t[2]];
};
const parent = {};
gltf.nodes.forEach((n, i) => (n.children || []).forEach(c => (parent[c] = i)));
const toWorld = (i, p) => { let q = p, k = i; while (k !== undefined) { q = xf(gltf.nodes[k], q); k = parent[k]; } return q; };
const nodeOrigin = i => toWorld(i, [0, 0, 0]);

function meshPoints(mi) {
  const pts = [];
  for (const prim of gltf.meshes[mi].primitives) {
    const acc = gltf.accessors[prim.attributes.POSITION];
    const bv = gltf.bufferViews[acc.bufferView];
    const off = (bv.byteOffset || 0) + (acc.byteOffset || 0);
    const stride = bv.byteStride || 12;
    for (let k = 0; k < acc.count; k++) pts.push([0, 1, 2].map(c => bin.readFloatLE(off + k * stride + c * 4)));
  }
  return pts;
}
function tris(mi) {
  let t = 0;
  for (const prim of gltf.meshes[mi].primitives) t += (prim.indices !== undefined ? gltf.accessors[prim.indices].count : gltf.accessors[prim.attributes.POSITION].count) / 3;
  return t;
}
function subtree(i) { const out = [i]; (gltf.nodes[i].children || []).forEach(c => out.push(...subtree(c))); return out; }
function bbox(i) {
  const mn = [1e9, 1e9, 1e9], mx = [-1e9, -1e9, -1e9]; let t = 0;
  for (const k of subtree(i)) {
    const n = gltf.nodes[k];
    if (n.mesh === undefined) continue;
    t += tris(n.mesh);
    for (const p of meshPoints(n.mesh)) { const w = toWorld(k, p); for (let c = 0; c < 3; c++) { mn[c] = Math.min(mn[c], w[c]); mx[c] = Math.max(mx[c], w[c]); } }
  }
  return { mn, mx, t };
}
const f = a => '[' + a.map(v => v.toFixed(3)).join(', ') + ']';
const sceneRoots = gltf.scenes[gltf.scene || 0].nodes;
console.log(`${file}  (${buf.length} bytes)  materials: ${gltf.materials.map(m => m.name).join(',')}`);
let total = 0;
function walk(i, depth) {
  const n = gltf.nodes[i];
  const b = bbox(i);
  if (depth === 0) total += b.t;
  const size = b.mn[0] < 1e8 ? b.mx.map((v, c) => v - b.mn[c]) : null;
  if (depth === 0 || showAll)
    console.log(`${'  '.repeat(depth)}${n.name}  parent=${parent[i] !== undefined ? gltf.nodes[parent[i]].name : '(scene)'}  origin=${f(nodeOrigin(i))}` +
      (size ? `  bbox min=${f(b.mn)} max=${f(b.mx)} size=${f(size)} tris=${b.t}` : ''));
  (n.children || []).forEach(c => walk(c, depth + 1));
}
sceneRoots.forEach(i => walk(i, 0));
console.log('TOTAL TRIS', total);
