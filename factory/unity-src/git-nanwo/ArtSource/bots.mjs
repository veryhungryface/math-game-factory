#!/usr/bin/env node
// 깃 나눠 — 무뇌 봇 200판 (첫 시도 정답률). C# SheetGen 과 같은 제약:
// ad0=1, target은 꼭짓점·밑변이 아니고 ad0 이 아님, 내부 칸 8~14.
import { createRequire } from 'module';
const require = createRequire(import.meta.url);

function deck(seed) {
  // C# System.Random 을 흉내 내지 않고, 같은 풀에서 장 10개를 고정 구조로 뽑는다.
  // 칸 수·정답 위치만 봇에 쓰이므로 발문 생성은 생략.
  const rng = mulberry(seed);
  const pick = arr => arr[Math.floor(rng() * arr.length)];
  const sheets = [];
  const push = (span, target) => {
    const adLo = 1, adHi = span - 1;
    if (target <= adLo || target >= span) return;
    let ad0 = adLo + Math.floor(rng() * (adHi - adLo + 1));
    if (ad0 === target) ad0 = target > adLo ? target - 1 : Math.min(adHi, target + 1);
    sheets.push({ ad0, adLo, adHi, target, ticks: adHi - adLo + 1 });
  };
  // 장 1 FindEc
  push(pick([9, 12, 15, 10]), pick([6, 8, 9]));
  // 장 2 Part 2:1 AB=12 → AD=8
  push(12, 8);
  // 장 3 FindAe
  push(pick([9, 12, 15, 18]), pick([6, 8, 9, 12]));
  // 장 4 Part 1:2 AB=9 → AD=3
  push(9, 3);
  // 장 5 DeLen
  push(pick([9, 10, 12, 15]), pick([6, 8, 9, 10]));
  // 장 6 Mid AB even, target = AB/2
  { const ab = pick([10, 12, 14, 16, 18]); push(ab, ab / 2); }
  // 장 7 Similar
  push(pick([9, 12, 15]), pick([6, 8]));
  // 장 8 Three, drop = x
  push(pick([12, 14, 15, 18]), pick([8, 9]));
  // 장 9 Cent median 12/15, AG=8/10
  push(pick([12, 15]), pick([8, 10]));
  // 장 10 Area 동일
  push(pick([12, 15]), pick([8, 10]));
  while (sheets.length < 10) push(12, 8);
  return sheets.slice(0, 10);
}

function mulberry(seed) {
  let s = seed | 0;
  return () => {
    s = (s + 0x6D2B79F5) | 0;
    let t = Math.imul(s ^ (s >>> 15), 1 | s);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const Games = 200, Cuts = 10;
let mash = 0, cyc = 0, rnd = 0, chance = 0, n = 0;
let mashFin = 0, cycFin = 0, rndFin = 0;
const rng = mulberry(42);
for (let g = 0; g < Games; g++) {
  const d = deck(1000 + g);
  let livesM = 3, livesC = 3, livesR = 3;
  let okM = 0, okC = 0, okR = 0;
  for (let i = 0; i < Math.min(Cuts, d.length); i++) {
    const s = d[i];
    chance += 1 / s.ticks; n++;
    if (s.ad0 === s.target) { mash++; okM++; } else livesM--;
    let cycTick = s.ad0, dir = 1;
    for (let k = 0; k < i; k++) {
      cycTick += dir;
      if (cycTick >= s.adHi) { cycTick = s.adHi; dir = -1; }
      if (cycTick <= s.adLo) { cycTick = s.adLo; dir = 1; }
    }
    if (cycTick === s.target) { cyc++; okC++; } else livesC--;
    let rAd = s.adLo + Math.floor(rng() * s.ticks);
    if (rAd > s.adHi) rAd = s.adHi;
    if (rAd === s.target) { rnd++; okR++; } else livesR--;
  }
  if (okM >= Cuts && livesM > 0) mashFin++;
  if (okC >= Cuts && livesC > 0) cycFin++;
  if (okR >= Cuts && livesR > 0) rndFin++;
}
const pct = x => (x * 100).toFixed(1) + '%';
const ch = chance / n;
const lines = [
  '## 무뇌 봇 200판 × 10컷, 측정=첫 시도 정답률 (우연≈1/칸수)',
  `- 연타 봇: ${pct(mash / n)}  (우연 ${pct(ch)})  완주율 ${pct(mashFin / Games)}`,
  `- 순환 봇: ${pct(cyc / n)}  (우연 ${pct(ch)})  완주율 ${pct(cycFin / Games)}`,
  `- 무작위 봇: ${pct(rnd / n)}  (우연 ${pct(ch)})  완주율 ${pct(rndFin / Games)}`,
  `- 무입력 봇: 0.0%  진도 0`,
];
console.log(lines.join('\n'));
import { writeFileSync } from 'fs';
writeFileSync(new URL('./bot-results.md', import.meta.url), '# 깃 나눠 무뇌 봇\n\n' + lines.join('\n') + '\n');
