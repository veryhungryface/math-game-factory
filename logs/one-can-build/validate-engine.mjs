import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';

const source = fs.readFileSync('public/g/one-can/engine.js', 'utf8');
const context = vm.createContext({});
vm.runInContext(source, context, { filename: 'engine.js' });
const E = context.OneCan;
const GAMES = 200;
const SEED_START = 1001;
const out = { generatedAt: new Date().toISOString(), engineSha256: crypto.createHash('sha256').update(source).digest('hex'), environment: 'Node vm pure-state simulation; not a trusted browser, screenshot, FPS, or real pointer test', gamesPerPolicy: GAMES, seedStart: SEED_START, gradingUnit: 'Whole order first attempt; tutorial excluded; retries never create a second opportunity', policies: {}, checks: [] };
function random32(seed) { let x = seed >>> 0; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; }; }
const policies = {
  repeat: () => 0,
  cycle: (s, turn) => turn % 3,
  random: (s, turn, rand) => Math.floor(rand() * s.rail.length),
  idle: () => -1,
  smallest: s => s.rail.reduce((best,c,i,a) => c.ml < a[best].ml ? i : best, 0),
  tallest: s => s.rail.reduce((best,c,i,a) => c.shapeSeed > a[best].shapeSeed ? i : best, 0),
  promptCopy: s => {
    // Read only visible decimal strings. Deliberately no unit conversion,
    // complement, sum, or access to the hidden target quantity.
    const printed = Array.from(s.problem.prompt.matchAll(/\d+/g), m => Number(m[0]));
    const matching = s.rail.findIndex(c => printed.includes(c.ml));
    return matching < 0 ? 0 : matching;
  }
};
for (const [name, policy] of Object.entries(policies)) {
  let completed = 0, correct = 0, opportunities = 0, expected = 0, variance = 0, progression = 0;
  const runs = [];
  for (let game = 0; game < GAMES; game++) {
    const seed = SEED_START + game, s = E.create(seed, { tutorial: false });
    const rand = random32(seed ^ 0x6ac81f99);
    let turn = 0;
    while (s.phase === 'playing') {
      if (name === 'idle') { E.tick(s, 90); break; }
      const i = policy(s, turn++, rand);
      E.tap(s, i);
      // 1.6 s covers the renderer's 0.32 s fall + longest 1.28 s response
      // approximately; no policy gets engine-only infinite-time progress.
      E.tick(s, 1.6);
      assert.ok(turn < 500, `${name}: failed to terminate`);
    }
    assert.equal(s.firstAttempts.length, s.attemptsOpened, 'All opened orders must have one first attempt result at termination');
    assert.equal(new Set(s.firstAttempts.map(a => a.order)).size, s.firstAttempts.length, 'Retries duplicated denominator');
    const successes = s.firstAttempts.filter(a => a.correct).length;
    const chanceSum = name === 'idle' ? 0 : s.firstAttempts.reduce((sum,a) => sum + a.chance, 0);
    const varianceSum = name === 'idle' ? 0 : s.firstAttempts.reduce((sum,a) => sum + a.chance * (1-a.chance), 0);
    correct += successes; opportunities += s.firstAttempts.length; expected += chanceSum; variance += varianceSum;
    completed += Number(s.phase === 'clear'); progression += s.solved;
    runs.push({ seed, phase: s.phase, shipped: s.shipped, solved: s.solved, lives: s.lives, elapsed: s.elapsed, opportunities: s.firstAttempts.length, firstCorrect: successes, expectedFirstCorrect: chanceSum });
  }
  const z = variance ? (correct - expected) / Math.sqrt(variance) : 0;
  out.policies[name] = { games: GAMES, firstCorrect: correct, opportunities, firstAttemptRate: correct / opportunities, exactRandomChance: expected / opportunities, completionRate: completed / GAMES, completed, solvedOrders: progression, zAboveChance: z, significantLeakage: z > 2.576, runs };
  assert.ok(!(name === 'idle' && progression !== 0), 'No input advanced orders');
}

function check(name, fn) { fn(); out.checks.push({ name, passed: true }); }
check('normal route and recovery after a first mistake for all 200 seeds', () => {
  for (let seed=SEED_START; seed<SEED_START+GAMES; seed++) for (const wrongFirst of [false,true]) {
    const s=E.create(seed,{tutorial:false});
    if (wrongFirst) { const i=s.rail.findIndex(c=>c.ml>E.need(s)); assert.ok(i>=0); E.tap(s,i); assert.equal(s.lives,2); }
    let taps=0;
    while(s.phase==='playing') { const i=E.correctIndex(s); assert.ok(i>=0); E.tap(s,i); E.tick(s,1.6); assert.ok(++taps<120); }
    assert.equal(s.phase,'clear',`seed ${seed}, wrongFirst ${wrongFirst}, elapsed ${s.elapsed}`);
    assert.ok(s.shipped>=8); assert.equal(s.lives,wrongFirst?2:3);
    if(wrongFirst) assert.equal(s.firstAttempts[0].correct,false);
  }
});
check('fixed tutorial advances only through two actual taps and never consumes time or life',()=>{
  const s=E.create(1); E.tick(s,90); assert.equal(s.elapsed,0); assert.equal(s.frozen,true);
  assert.equal(s.rail.length,1); assert.equal(s.rail[0].ml,500); E.tap(s,0);
  assert.equal(s.canMl,500); assert.equal(s.frozen,true); E.tick(s,90); assert.equal(s.elapsed,0);
  E.tap(s,0); assert.equal(s.frozen,false); assert.equal(s.shipped,1); assert.equal(s.lives,3); assert.equal(s.firstAttempts.length,0);
});
check('three failures end the run; terminal real input never revives it',()=>{
  const s=E.create(42,{tutorial:false});
  for(let t=0;t<3;t++) E.tap(s,s.rail.findIndex(c=>c.ml>E.need(s)));
  assert.equal(s.phase,'gameover'); assert.equal(s.lives,0);
  const before=JSON.stringify(s); for(let t=0;t<20;t++){E.tap(s,t%3);E.tick(s,10);} assert.equal(JSON.stringify(s),before);
});
check('sampleProblems respects 40/17/63, answer inclusion, unique choices, >=70% distinct, shared pool',()=>{
  const ids = new Set(E.pool.map(p=>p.id));
  for(const n of [40,17,63]) { const ps=E.sampleProblems(n); assert.equal(ps.length,n); assert.ok(new Set(ps.map(p=>p.id)).size>=n*.7);
    for(const p of ps){ assert.ok(ids.has(p.id));assert.ok(p.choices.includes(p.answer));assert.equal(new Set(p.choices).size,p.choices.length);assert.equal(p.answerNumeric,E.answerValue(p)); }
  }
});
check('full mathematical pool: integer quantities, exact add/subtract/complement and no coincident wrong candidate',()=>{
  for(const p of E.pool){ assert.ok(Number.isInteger(p.targetMl)); assert.ok(p.targetMl>p.canStartMl);
    if(p.kind==='add_two')assert.equal(p.targetMl,p.addends[0]+p.addends[1]);
    if(p.kind==='subtract_fill')assert.equal(p.targetMl,p.addends[0]-p.addends[1]);
    const answer=E.answerValue(p), wrong=E.candidates(p);
    assert.ok(wrong.length>=2); assert.equal(new Set(wrong.map(c=>c.value)).size,wrong.length);
    for(const c of wrong){assert.notEqual(c.value,answer);assert.ok(c.misconceptionId);assert.ok(Number.isInteger(c.value)&&c.value>0);}
    assert.ok(E.chanceOfProblem(p)>0&&E.chanceOfProblem(p)<=1);
  }
});
check('all reachable required amounts offer three distinct cups and a mathematical route within the visible budget',()=>{
  for(let required=50;required<=1000;required+=50) for(let drops=1;drops<=3;drops++) for(let pattern=0;pattern<3;pattern++) {
    const cups=E.railValues(required,drops,pattern); assert.equal(cups.length,3);assert.equal(new Set(cups.map(c=>c.ml)).size,3);
    assert.ok(cups.every(c=>Number.isInteger(c.ml)&&c.ml>0));assert.ok(cups.some(c=>c.ml<=required));
    if(drops===1)assert.ok(cups.some(c=>c.ml===required));
  }
});
out.poolSize=E.pool.length;
out.chanceDefinition='Mean exact uniformly random 3-cup whole-order success probability over observed orders. Idle has no action, hence its chance and progression are both zero; a counterfactual tap on the initial order would have chance 0.148148.';
fs.writeFileSync('logs/one-can-build/bot-report.json',JSON.stringify(out,null,2)+'\n');
const summary = Object.entries(out.policies).map(([name,p])=>`${name}: ${(p.firstAttemptRate*100).toFixed(2)}% first (${p.firstCorrect}/${p.opportunities}), ${(p.exactRandomChance*100).toFixed(2)}% chance, ${(p.completionRate*100).toFixed(1)}% completion, z=${p.zAboveChance.toFixed(2)}, ${p.significantLeakage?'LEAK':'no significant leakage'}`).join('\n');
fs.writeFileSync('logs/one-can-build/bot-summary.txt',summary+'\n'+out.checks.length+' engine checks passed; no browser claims.\n');
console.log(summary);console.log(out.checks.length+' engine checks passed; no browser claims.');
