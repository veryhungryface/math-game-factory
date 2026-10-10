// 문제 은행 전수 검산 + 실제 화면 대상 간격 실측.
// 1) sampleProblems 로 은행 전체(387)를 받아 발문의 수치만으로 정답 쌍을 독립 계산해 answer/answerNumeric 과 대조한다.
//    - 정삼각형 제외(같은 값 정확히 2개), 삼각형 성립, 각의 합 180°, 중2 범위(√ 없음), 금지어(가정·결론·봅시다·보세요)
//    - 오답 쌍(정답이 아닌 두 쌍)은 모든 파라미터에서 거짓인지(같은 값 쌍이 정확히 하나뿐인지) 전수 확인
// 2) 훅으로 반죽을 계속 공급받아 화면상 대상 3개의 최소 간격(px)과 히트 반경 대비를 390×844·1280×800 에서 잰다.
import fs from 'node:fs';
import path from 'node:path';
import { HERE, launch, openGame, state, sleep } from './lib.mjs';

const { server, browser } = await launch();
const g = await openGame(browser, server, { w: 390, h: 844 });
const bank = await g.page.evaluate(() => window.__GAME_TEST__.sampleProblems(1000));
const errs = [];
const names = ['A', 'B', 'C'];
const sideOf = k => (k === 0 ? 'BC' : k === 1 ? 'AC' : 'AB');
let checked = 0;
for (const p of bank) {
  const t = p.prompt;
  if (/√|가정|결론|봅시다|보세요/.test(t)) errs.push(`${p.id} 금지 표현`);
  let mask = 0, isAngle;
  const sides = {}, angles = {};
  for (const m of t.matchAll(/([ABC]{2})(?==)/g)) {}
  const head = t.split('이다.')[0];
  if (head.includes('cm')) {
    isAngle = true; // 변 길이가 주어지고 각을 잇는다
    // 「AB=AC=7 cm, BC=6 cm」
    const m = head.match(/([ABC]{2})=([ABC]{2})=(\d+) cm, ([ABC]{2})=(\d+) cm/);
    if (!m) { errs.push(`${p.id} 길이 파싱 실패`); continue; }
    sides[m[1]] = +m[3]; sides[m[2]] = +m[3]; sides[m[4]] = +m[5];
    const v = Object.values(sides).sort((a, b) => a - b);
    if (v[0] + v[1] <= v[2]) errs.push(`${p.id} 삼각형 아님`);
    if (v[0] === v[2]) errs.push(`${p.id} 정삼각형`);
    // 같은 두 변의 맞은편 각이 같다: 변 k 의 맞은편 꼭짓점 k
    const eq = [0, 1, 2].filter(k => [0, 1, 2].some(j => j !== k && sides[sideOf(j)] === sides[sideOf(k)]));
    if (eq.length !== 2) errs.push(`${p.id} 같은 쌍이 정확히 하나가 아님`);
    mask = eq.reduce((a, k) => a | (1 << k), 0);
  } else {
    isAngle = false;
    const m = head.match(/∠([ABC])=∠([ABC])=(\d+)°, ∠([ABC])=(\d+)°/);
    if (!m) { errs.push(`${p.id} 각 파싱 실패`); continue; }
    angles[m[1]] = +m[3]; angles[m[2]] = +m[3]; angles[m[4]] = +m[5];
    const sum = Object.values(angles).reduce((a, b) => a + b, 0);
    if (sum !== 180) errs.push(`${p.id} 내각의 합 ${sum}`);
    if (Object.values(angles).every(x => x === 60)) errs.push(`${p.id} 정삼각형`);
    if (Object.values(angles).some(x => x <= 0)) errs.push(`${p.id} 각 0 이하`);
    // 같은 두 각의 맞은편 변: 각 k 와 같은 각이 있으면 변 k(맞은편)가 정답
    const eq = [0, 1, 2].filter(k => [0, 1, 2].some(j => j !== k && angles[names[j]] === angles[names[k]]));
    if (eq.length !== 2) errs.push(`${p.id} 같은 쌍이 정확히 하나가 아님`);
    mask = eq.reduce((a, k) => a | (1 << k), 0);
  }
  const ansNames = [0, 1, 2].filter(k => mask & (1 << k)).map(k => isAngle ? '∠' + names[k] : sideOf(k)).join(', ');
  if (p.answer !== ansNames) errs.push(`${p.id} 정답 불일치 ${p.answer} vs ${ansNames}`);
  if (p.answerNumeric !== mask) errs.push(`${p.id} answerNumeric ${p.answerNumeric} vs ${mask}`);
  if (p.choices) errs.push(`${p.id} choices 가 있음(직접 잇기 문항)`);
  // 단계3: 미리 그려진 연결은 정답이 아니어야 한다
  const pre = t.match(/(∠[ABC]|[ABC]{2})와 (∠[ABC]|[ABC]{2})가 잘못 연결/);
  if (pre) {
    const idx = s => s.startsWith('∠') ? names.indexOf(s[1]) : [0, 1, 2].find(k => sideOf(k) === s);
    const pm = (1 << idx(pre[1])) | (1 << idx(pre[2]));
    if (pm === mask) errs.push(`${p.id} 기존 연결이 정답과 같음`);
    if (!t.includes(isAngle ? '두 각을 고치시오' : '두 변을 고치시오')) errs.push(`${p.id} 고치기 발문 불일치`);
  } else if (!t.includes(isAngle ? '같은 크기의 두 각을 이으시오' : '같은 길이의 두 변을 이으시오')) errs.push(`${p.id} 발문 불일치`);
  checked++;
}
const kinds = bank.reduce((a, p) => { const k = p.id.split('-')[0]; a[k] = (a[k] || 0) + 1; return a; }, {});
const unique = new Set(bank.map(p => p.prompt)).size;

// 2) 화면 대상 간격 실측
async function spacing(w, h, n) {
  const gg = await openGame(browser, server, { w, h });
  const pg = gg.page;
  await pg.evaluate(() => window.__GAME_TEST__.start());
  let min = Infinity, minId = '', samples = 0, seen = new Set();
  for (let i = 0; i < n; i++) {
    let s = await state(pg);
    const until = Date.now() + 4000;
    while ((s.doughs.length === 0 || s.locked) && s.phase === 'playing' && Date.now() < until) { await sleep(40); s = await state(pg); }
    for (const d of s.doughs) {
      const key = d.id + ':' + d.tx.join(',');
      if (seen.has(key)) continue; seen.add(key); samples++;
      for (let a = 0; a < 3; a++) for (let b = a + 1; b < 3; b++) {
        const dx = (d.tx[a] - d.tx[b]) / 1000 * w, dy = (d.ty[a] - d.ty[b]) / 1000 * h;
        const dist = Math.hypot(dx, dy);
        if (dist < min) { min = dist; minId = d.id; }
      }
    }
    await pg.evaluate(() => window.__GAME_TEST__.answerCorrect());
  }
  await pg.close();
  return { viewport: `${w}x${h}`, samples, minTargetGapPx: Math.round(min), minId, errors: gg.errors.length };
}
const sp = [await spacing(390, 844, 160), await spacing(1280, 800, 160)];
const out = { bankSize: bank.length, uniquePrompts: unique, kinds, checked, errors: errs, spacing: sp, passed: errs.length === 0 && bank.length >= 300 };
fs.writeFileSync(path.join(HERE, 'math-recheck-results.json'), JSON.stringify(out, null, 2) + '\n');
console.log(JSON.stringify({ ...out, errors: errs.slice(0, 10) }, null, 1));
await browser.close(); server.close();
