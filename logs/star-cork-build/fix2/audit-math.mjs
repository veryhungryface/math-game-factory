// Round-2 independent math audit of the CURRENT engine.js (post fix-2 pool edit).
import fs from 'node:fs';
const src = fs.readFileSync('public/g/star-cork/engine.js','utf8');
const mod = {}; const win = { StarCork: null };
new Function('window', src + '\n;window.StarCork=StarCork;')(win);
const E = win.StarCork;
const { allProblems, pools, tutorial, sum, value, evaluate, counts, sampleProblems, Game } = E;

const bad = [];
const note = (id,msg)=>bad.push(`${id}: ${msg}`);
const AT_PLACE = /\d\s*의\s*자리에서\s*(올림|버림|반올림)/;

let n=0;
for (const p of [...allProblems, tutorial]) {
  n++;
  // legend integrity
  if (!Number.isInteger(p.legendBig) || !Number.isInteger(p.legendSmall)) note(p.id,'legend not integer');
  if (p.legendSmall !== p.legendBig/10) note(p.id,`legendSmall ${p.legendSmall} != legendBig/10 (${p.legendBig})`);
  // solution sums to target, computed two ways
  const manual = p.solution.reduce((a,t)=>a + (t==='big'?p.legendBig:t==='small'?p.legendSmall:5), 0);
  const viaSum = sum(p.solution, p);
  if (manual !== p.target) note(p.id,`manual solution sum ${manual} != target ${p.target}`);
  if (viaSum !== p.target) note(p.id,`engine sum ${viaSum} != target ${p.target}`);
  if (!Number.isInteger(p.target) || p.target < 0) note(p.id,'target not a natural number');
  // half stars only meaningful at legendBig 10
  if (p.solution.includes('half') && p.legendBig !== 10) note(p.id,'half star outside legendBig=10');
  // engine accepts its own solution
  if (evaluate(p.solution, p) !== 'correct') note(p.id,'engine rejects its own solution');
  // belt supply must cover what the solution still needs beyond the initial plate
  const need = counts(p.solution), have = counts(p.initial||[]), belt = counts(p.belt||[]);
  for (const t of ['big','small','half']) {
    const missing = (need[t]||0) - (have[t]||0);
    if (missing > 0 && (belt[t]||0) < missing) note(p.id,`belt short of ${t}: needs ${missing}, belt has ${belt[t]||0}`);
  }
  // belt total must exceed the order so mashing every star always overshoots
  const beltAll = sum([...(p.initial||[]), ...(p.belt||[])], p);
  if (beltAll <= p.target) note(p.id,`mashing every star totals ${beltAll} <= target ${p.target} — brainless mashing can win`);
  // textbook expression traps
  if (AT_PLACE.test(p.prompt)) note(p.id,'AT_PLACE rounding phrasing');
  if (/눈금|막대그래프/.test(p.prompt)) note(p.id,'wrong-unit vocabulary');
  if (/하십시오|하시오/.test(p.prompt)) note(p.id,'formal register (하십시오체)');
}

// error_find: the on-screen instruction promises BOTH removing and loading.
for (const p of allProblems.filter(p=>p.type==='error_find')) {
  const start = sum(p.initial, p);
  const removalsOnly = (()=>{ // can any subset of removals reach target?
    const vals = p.initial.map(t=>t==='big'?p.legendBig:t==='small'?p.legendSmall:5);
    let set = new Set([start]);
    for (const v of vals) set = new Set([...set, ...[...set].map(x=>x-v)]);
    return set.has(p.target);
  })();
  if (!removalsOnly && !(p.belt||[]).length) note(p.id,'unsolvable: removals cannot reach target and belt is empty');
}

// distractor / sample contract
for (const n of [1,17,40,63,205]) {
  const s = sampleProblems(n);
  if (s.length !== n) note('sample','n='+n+' returned '+s.length);
  for (const q of s) {
    if (new Set(q.choices).size !== q.choices.length) note(q.id,'duplicate choices');
    if (q.choices.length !== 4) note(q.id,'choices != 4');
    if (q.choices.filter(c=>c===q.answer).length !== 1) note(q.id,'answer not exactly once in choices');
    if (Number(q.answer) !== q.answerNumeric) note(q.id,'answerNumeric mismatch');
    if (!Number.isInteger(q.answerNumeric)) note(q.id,'answerNumeric not integer');
  }
}

console.log('problems audited:', n, '| pools:', Object.fromEntries(Object.entries(pools).map(([k,v])=>[k,v.length])));
console.log(bad.length ? 'VIOLATIONS ('+bad.length+'):\n' + bad.slice(0,40).join('\n') : 'VIOLATIONS: 0');
