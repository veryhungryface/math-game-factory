import fs from 'node:fs/promises';
import path from 'node:path';

const here = import.meta.dirname;
const source = await fs.readFile(path.join(here, '../../Scripts/BitSasuRules.cs'), 'utf8');
const outFile = path.join(here, 'math-results.json');

function fail(message) { throw new Error(message); }
function parseArray(name) {
  const m = source.match(new RegExp(`${name}\\s*=\\s*\\{([\\s\\S]*?)\\}`));
  if (!m) fail(`${name} not found`);
  return [...m[1].matchAll(/\d+/g)].map(x => Number(x[0]));
}
function gcd(a, b) { while (b) [a, b] = [b, a % b]; return Math.abs(a) || 1; }

const tables = [parseArray('Sin1000'), parseArray('Cos1000'), parseArray('Tan1000')];
if (tables.some(a => a.length !== 60)) fail('table length must be 60');
for (let i = 0; i < 60; i++) {
  const rad = (15 + i) * Math.PI / 180;
  const expected = [Math.round(Math.sin(rad) * 1000), Math.round(Math.cos(rad) * 1000), Math.round(Math.tan(rad) * 1000)];
  for (let f = 0; f < 3; f++) if (tables[f][i] !== expected[f]) fail(`table mismatch angle=${15 + i} f=${f}`);
}

function nearest(f, n, d) {
  let best = Infinity, answer = 15, tied = false;
  for (let a = 15; a <= 74; a++) {
    const diff = Math.abs(1000 * n - tables[f][a - 15] * d);
    if (diff < best) { best = diff; answer = a; tied = false; }
    else if (diff === best) tied = true;
  }
  return tied ? -1 : answer;
}

const buckets = Array(60).fill(null);
for (let m = 2; m <= 40; m++) for (let n = 1; n < m; n++) {
  let x = m * m - n * n, y = 2 * m * n, r = m * m + n * n;
  const g = gcd(gcd(x, y), r); x /= g; y /= g; r /= g;
  for (const [p, q] of [[y, x], [x, y]]) {
    const a = nearest(2, p, q);
    if (a < 15 || a > 74 || nearest(0, p, r) !== a || nearest(1, q, r) !== a) continue;
    if (Math.abs(Math.atan2(p, q) * 180 / Math.PI - a) > 0.3000001) continue;
    const i = a - 15;
    if (!buckets[i] || r < buckets[i].r) buckets[i] = { a, p, q, r };
  }
}
if (buckets.some(x => !x)) fail('missing angle bucket');

let rawDistractorCollisions = 0;
for (const b of buckets) {
  if (b.p * b.p + b.q * b.q !== b.r * b.r) fail(`not Pythagorean at ${b.a}`);
  if (nearest(0, b.p, b.r) !== b.a || nearest(1, b.q, b.r) !== b.a || nearest(2, b.p, b.q) !== b.a) fail(`nearest row mismatch at ${b.a}`);
  const distractors = [nearest(0, b.q, b.r), nearest(1, b.p, b.r), nearest(2, b.q, b.p), nearest(2, b.p, b.r)];
  rawDistractorCollisions += distractors.filter(a => a === b.a).length;
  for (const scale of [1, 2, 3]) for (const eye of [80, 100, 120, 140, 160]) {
    const total = eye + b.p * scale, distance = b.q * scale;
    if (nearest(2, total, distance) === b.a) rawDistractorCollisions++;
  }
}

if (nearest(1, 4, 5) !== 37) fail('cos 4/5 example');
if (nearest(2, 3, 4) !== 37) fail('tan 3/4 example');
if (nearest(2, 5 - 2, 4) !== 37) fail('height difference example');

// 런타임은 오답 제출에서만 IdentifyMisconception을 호출하므로 정답각과
// 우연히 같은 역산값은 오개념으로 주장될 수 없다.
const report = {
  generatedAt: new Date().toISOString(),
  tableRowsChecked: 180,
  angleBuckets: buckets.length,
  pythagoreanBucketsChecked: buckets.length,
  nearestRowChecks: buckets.length * 3,
  exampleChecks: 3,
  rawDistractorCollisions,
  claimedDistractorCollisions: 0,
  result: 'pass'
};
await fs.writeFile(outFile, JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
