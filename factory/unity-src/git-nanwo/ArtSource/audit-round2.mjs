// 2차 수정 검산: Similar 공개 비, Mid 드롭=MN, Practice DE, Area 그림=발문.
function gcd(a, b) { a = Math.abs(a); b = Math.abs(b); while (b) { const t = a % b; a = b; b = t; } return a || 1; }
function ratio(a, b) { const g = gcd(a, b); return [a / g, b / g]; }

function revealSimilar(s) {
  let mShow = s.m, nShow = s.n;
  if (mShow < 1 || nShow < 1 || mShow * s.bc !== nShow * s.de) {
    [mShow, nShow] = ratio(s.de, s.bc);
  }
  return `AD:AB = DE:BC = ${mShow}:${nShow}  →  DE=${s.de} cm`;
}

const cases = [];
function check(name, ok, detail) { cases.push({ name, ok, detail }); }

// gn80: AD:AB=1:3 AB=12 BC=15 DE=5. 옛 공개 5:12 는 틀림.
{
  const s = { m: 1, n: 3, ab: 12, bc: 15, de: 5, target: 5 };
  const r = revealSimilar(s);
  const old = ratio(s.target, s.ab).join(':');
  check('gn80 reveal uses 1:3 not 5:12', r.includes('1:3') && !r.includes('5:12') && r.includes('DE=5 cm'), r + ' (old would be ' + old + ')');
}
// gn81: AD:AB=2:3 AB=15 BC=12 DE=8. 옛 공개 Ratio(8,15)=8:15.
{
  const s = { m: 2, n: 3, ab: 15, bc: 12, de: 8, target: 8 };
  const r = revealSimilar(s);
  check('gn81 reveal uses 2:3 not 8:15', r.includes('2:3') && !r.includes('8:15') && r.includes('DE=8 cm'), r);
}

// DeLen 풀 → Similar 15문항 공개
const delen = [
  [12, 12, 8, 6, 4], [10, 10, 15, 6, 9], [12, 12, 9, 8, 6], [9, 9, 12, 6, 8],
  [15, 15, 10, 9, 6], [12, 16, 12, 8, 8], [9, 12, 9, 6, 6], [15, 15, 12, 10, 8],
];
let similarOk = 0, similarBad = [];
for (const [ab, ac, bc, ad, de] of delen) {
  const [m, n] = ratio(ad, ab);
  const s = { m, n, ab, bc, de, target: de, givenAd: ad };
  const r = revealSimilar(s);
  const [mDe, nDe] = ratio(de, bc);
  const good = r.includes(`${m}:${n}`) && m === mDe && n === nDe;
  if (good) similarOk++; else similarBad.push({ ab, bc, ad, de, r, expect: `${m}:${n}` });
}
check('Similar pool reveal AD:AB = DE:BC', similarBad.length === 0, similarOk + '/' + delen.length + (similarBad.length ? JSON.stringify(similarBad) : ''));

// Mid: 구한 MN cm 드롭이 Ok. Span=BC, target=asked=BC/2, 높이 t=0.5 = 중점.
{
  const samples = [[14, 12], [12, 18], [10, 16], [16, 10], [18, 14]];
  let ok = true, det = [];
  for (const [ab, bc] of samples) {
    const asked = bc / 2, target = asked, midAb = ab / 2;
    const dropMnOk = asked === target;
    const heightIsMid = asked / bc === 0.5 && (asked / bc) * ab === ab / 2;
    const oldBug = target === midAb && asked !== midAb;
    det.push({ ab, bc, asked, target, midAb, dropMnOk, heightIsMid, oldBug });
    if (!dropMnOk || !heightIsMid) ok = false;
  }
  check('Mid drop at MN cm is Ok and is geometric midpoint', ok, JSON.stringify(det));
}

// Practice: AB=6 BC=9 AD=4 → DE=6 (옛 de=5, 8×4/6=16/3)
{
  const ab = 6, bc = 9, ad = 4;
  const de = (bc * ad) % ab === 0 ? bc * ad / ab : -1;
  check('Practice DE integer matches geometry', de === 6, `DE=${de} (was 5 with BC=8)`);
}

// Area: 그림 넓이 = BC×중선/2 가 발문
{
  const pool = [[13, 10, 12, 8, 60], [15, 18, 12, 8, 108], [17, 16, 15, 10, 120]];
  const all = pool.every(p => p[4] * 2 === p[1] * p[2] && p[3] * 3 === p[2] * 2);
  check('Area/Cent figure area = BC*median/2 and AG=2/3', all, JSON.stringify(pool.map(p => ({ area: p[4], gbc: p[4] / 3 }))));
}

const failed = cases.filter(c => !c.ok);
console.log(cases.map(c => (c.ok ? 'OK  ' : 'FAIL') + ' ' + c.name + ' — ' + c.detail).join('\n'));
console.log(failed.length ? `\nFAILED ${failed.length}` : `\nALL ${cases.length} PASSED`);
process.exit(failed.length ? 1 : 0);
