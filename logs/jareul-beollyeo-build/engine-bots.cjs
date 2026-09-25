/* B3: 200 full 90-second runs per fixed, cyclic, random, and idle policy.
 * The bots send only input intents accepted by the same engine as the UI.
 * They never inspect side lengths, classifications, solutions, labels, or feedback text.
 * This is an upper-bound endpoint test: every edge intent lands exactly, so no
 * accuracy credit is taken for small pointer hit boxes or angle tolerance.
 */
const fs=require('node:fs');require('../../public/g/jareul-beollyeo/engine.js');const D=globalThis.DividerGame;
function chance(p) {
  // Uniform side intentions = three unordered pairs + one discard intention.
  if(p.mode==='side')return D.acceptsPiece(p)?D.equalPairs(p.sides).length/4:1/4;
  // For three-equal tasks the route must measure the remaining edge too.
  if(p.mode==='equilateral')return D.acceptsPiece(p)?3/4:1/4;
  // Acute requires visiting all three; right/obtuse requires the unique largest.
  if(p.mode==='angle')return p.angleType==='acute'?1:p.maxVertices.length/3;
  if(!D.acceptsPiece(p))return 1/4;
  return D.equalPairs(p.sides).length/4*(p.angleType==='acute'?1:p.maxVertices.length/3);
}
const policies=['repeat','cycle','random','idle'],results=[];
for(const policy of policies){let attempted=0,correct=0,completed=0,solved=0,chanceSum=0;const runs=[];
 for(let run=0;run<200;run++){
  const g=new D({seed:71001+run*7919}),r=D.rng(113+run*104729);g.start({skipTutorial:true});const seen=new Set();let step=0;
  while(g.state.phase==='playing'&&step<360){
   if(g.state.awaitingNext)g.next();
   const p=g.state.piece;
   if(policy==='repeat'){if(g.state.locked<0)g.lock(0);else g.compare(1);}
   else if(policy==='cycle'){const k=step%4;if(k===0)g.lock(0);else if(k===1)g.compare(1);else if(k===2)g.compare(2);else g.discard();}
   else if(policy==='random'){const operation=Math.floor(r()*4),target=Math.floor(r()*3);if(operation===0)g.lock(target);if(operation===1)g.compare(target);if(operation===2)g.measureAngle(target);if(operation===3)g.discard();}
   if(p.attempted&&!seen.has(p.id)){seen.add(p.id);chanceSum+=chance(p);}
   g.tick(.3);step++;
  }
  attempted+=g.state.firstAttempt.attempted;correct+=g.state.firstAttempt.correct;solved+=g.state.solved;if(g.state.phase==='clear')completed++;
  runs.push({seed:71001+run*7919,firstAttempted:g.state.firstAttempt.attempted,firstCorrect:g.state.firstAttempt.correct,solved:g.state.solved,phase:g.state.phase});
 }
 const rate=attempted?correct/attempted:0,baseline=attempted?chanceSum/attempted:0;
 results.push({policy,runs:200,attempted,firstCorrect:correct,firstAttemptAccuracy:rate,chanceLevel:baseline,completionRate:completed/200,totalSolved:solved,belowChance:rate<=baseline,runResults:runs});
}
for(const row of results){const variance=row.attempted*row.chanceLevel*(1-row.chanceLevel);row.excessZ=variance?(row.firstCorrect-row.attempted*row.chanceLevel)/Math.sqrt(variance):0;row.significantAboveChance95=row.excessZ>1.645;row.accuracyDenominator='attempted questions only';}
const report={diagnostic_note:'Optional perfect-endpoint intent stress. Raw excursions are retained; physical input B3 is engine-spatial-bots.json.',method:'Pure-engine input-intent simulation, 200 complete sessions per policy; no correct-answer hook, no math/state solution inspection. Full endpoint precision assumed. Per-reached-question chance computed over three side-pair routes plus discard, with conjunctive angle evidence. Tutorial intentionally skipped so fixed exercise cannot bias B3.',results};fs.writeFileSync('logs/jareul-beollyeo-build/engine-bots.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(results.map(({runResults,...r})=>r),null,2));if(results.some(r=>!r.belowChance))process.exitCode=1;
