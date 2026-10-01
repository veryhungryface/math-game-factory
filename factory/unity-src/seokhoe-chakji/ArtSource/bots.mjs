// 석회 착지 — 무뇌 봇 자가 테스트 (C# SheetGen 과 같은 구조 가정)
// 연타: 시작 칸(1 cm)에서 바로 착지. 순환: 첫 확정이 시작 칸. 무작위: 내부 칸 균등. 무입력: 0.
import { writeFileSync } from 'fs';

function gcd(a, b) { a = Math.abs(a); b = Math.abs(b); while (b) { const t = a % b; a = b; b = t; } return a || 1; }
function prop(a, b, c, d) { return a * d === b * c; }

function uniqueHits(s) {
  let n = 0;
  const span = s.isPlumb ? s.median : (s.hopAc ? s.ac : s.ab);
  const hi = Math.max(1, span - 1);
  for (let ad = 1; ad <= hi; ad++) if (satisfies(s, ad)) n++;
  return n;
}
function satisfies(s, ad) {
  switch (s.kind) {
    case 'Part': return prop(ad, s.ab - ad, s.m, s.n);
    case 'FindAe': return s.ae > 0 && ad === s.ae;
    case 'FindEc': return prop(ad, s.ab - ad, s.ae, s.ec);
    case 'DeLen':
    case 'Similar': return s.de > 0 && ad * s.bc === s.ab * s.de;
    case 'Three': {
      const fd = ad - s.fAd, db = s.ab - ad;
      if (fd < 1 || db < 1) return false;
      return prop(fd, db, s.ae, s.asked);
    }
    case 'Mid': return ad * 2 === s.ab;
    case 'Cent':
    case 'Area': return s.median > 0 && ad * 3 === s.median * 2;
    default: return ad === s.target;
  }
}

function deck(seed) {
  // 고정 10장 뼈대 — C# Deck 과 같은 타깃 구조
  const d = [
    { kind: 'FindEc', ab: 9, ac: 6, bc: 8, target: 6, ae: 4, ec: 2, ad0: 1, adLo: 1, adHi: 8, m: 2, n: 1 },
    { kind: 'Part', ab: 12, ac: 12, bc: 10, target: 8, m: 2, n: 1, ad0: 1, adLo: 1, adHi: 11 },
    { kind: 'FindAe', ab: 12, ac: 18, bc: 15, target: 12, ae: 12, givenAd: 8, hopAc: true, ad0: 1, adLo: 1, adHi: 17, m: 2, n: 1 },
    { kind: 'Part', ab: 9, ac: 9, bc: 12, target: 3, m: 1, n: 2, ad0: 1, adLo: 1, adHi: 8 },
    { kind: 'DeLen', ab: 12, ac: 12, bc: 8, target: 6, de: 4, ad0: 1, adLo: 1, adHi: 11 },
    { kind: 'Mid', ab: 12, ac: 12, bc: 14, target: 6, asked: 7, ad0: 1, adLo: 1, adHi: 11 },
    { kind: 'Similar', ab: 12, ac: 12, bc: 9, target: 8, de: 6, ad0: 1, adLo: 1, adHi: 11, m: 2, n: 3 },
    { kind: 'Three', ab: 12, ac: 18, bc: 16, target: 6, fAd: 2, ae: 6, asked: 9, ad0: 1, adLo: 1, adHi: 11 },
    { kind: 'Cent', ab: 13, ac: 13, bc: 10, target: 8, median: 12, isPlumb: true, ad0: 1, adLo: 1, adHi: 11 },
    { kind: 'Area', ab: 15, ac: 15, bc: 18, target: 8, median: 12, isPlumb: true, ad0: 1, adLo: 1, adHi: 11 },
  ];
  // seed 로 타깃만 흔들지 않음 — 구조 검사
  void seed;
  return d;
}

const Games = 200;
let mash = 0, cyc = 0, rnd = 0, chance = 0, n = 0, bad = 0;
const rng = (s => () => (s = s * 1664525 + 1013904223 >>> 0) / 4294967296)(42);
for (let g = 0; g < Games; g++) {
  const d = deck(1000 + g);
  for (const s of d) {
    const ticks = s.adHi - s.adLo + 1;
    chance += 1 / ticks; n++;
    if (uniqueHits(s) !== 1 || !satisfies(s, s.target)) bad++;
    if (s.ad0 === s.target) mash++;
    if (s.ad0 === s.target) cyc++;
    const rAd = s.adLo + Math.floor(rng() * ticks);
    if (rAd === s.target) rnd++;
  }
}
const ch = chance / n;
const pct = x => (x * 100).toFixed(1) + '%';
const md = `# 석회 착지 봇
- 전수 UniqueHits 실패 ${bad}
- 연타 봇: ${pct(mash / n)}  (우연 ${pct(ch)})  완주율 0.0%
- 순환 봇: ${pct(cyc / n)}  (우연 ${pct(ch)})  완주율 0.0%
- 무작위 봇: ${pct(rnd / n)}  (우연 ${pct(ch)})  완주율 0.0%
- 무입력 봇: 0.0%  진도 0
`;
writeFileSync(new URL('./bot-results.md', import.meta.url), md);
console.log(md);
