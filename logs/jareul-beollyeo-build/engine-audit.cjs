const fs = require('node:fs');
require('../../public/g/jareul-beollyeo/engine.js');
const D = globalThis.DividerGame;
function assert(x, message) { if (!x) throw new Error(message); }
const result = { fixedLoop: false, poolShapes: 0, parameterModeCombinations: 0, completeSolutionPaths: 0, accidentalTrueDistractors: 0, invalidTriangles: 0, normalRuns: 0, firstErrorThenWin: 0, samples: [], inclusionChecks: 0, acuteAllThreeChecks: 0 };
const fixed = new D({seed: 1}); fixed.start(); fixed.tick(180); assert(fixed.state.timeLeft === 90 && fixed.state.lives === 3, 'Tutorial timer/lives freeze'); fixed.lock(0); fixed.compare(2); assert(fixed.state.score === 100 && fixed.state.solved === 0, 'fixed tutorial comparison');fixed.next();assert(!fixed.state.frozen,'tutorial opens'); result.fixedLoop = true;
const configs = [{ mode: 'side' }, { mode: 'side', variant: 'reverse' }, { mode: 'side', variant: 'error_find' }, { mode: 'equilateral' }, { mode: 'angle' }, { mode: 'combined', angleTarget: 'acute' }, { mode: 'combined', angleTarget: 'obtuse' }];
for (const shape of [...D.POOLS.iso, ...D.POOLS.scalene, ...D.POOLS.equilateral]) {
  result.poolShapes++; const [a,b,c]=shape;
  if (!(a+b>c && b+c>a && c+a>b)) result.invalidTriangles++;
  for (const order of [[0,1,2],[0,2,1],[1,0,2],[1,2,0],[2,0,1],[2,1,0]]) for (const cfg of configs) {
    result.parameterModeCombinations++;
    const p=D.makePiece(order.map(i=>shape[i]),cfg), row=D.sampleOne(p,D.rng(42));
    assert(row.choices.includes(row.answer),'answer missing');assert(new Set(row.choices).size===row.choices.length,'duplicate choices');
    assert(row.distractors.length>=2 && row.distractors.every(d=>D.MISCONCEPTIONS[d.misconceptionId]),'misconception ids');
    for(const route of row.actionSolutions){const h=new D({seed:44});h.start({skipTutorial:true});h.state.pieces=[D.makePiece(p.sides,cfg)];h.selectPiece(0);for(const action of route)h[action.type](action.edge===undefined?action.vertex:action.edge);assert(h.state.solved===1&&h.state.score>0,'incomplete emitted action solution '+JSON.stringify({cfg,route}));result.completeSolutionPaths++;} 
    for (const d of row.distractors) {
      const k=D.classify(p.sides);
      let accidental;
      if (d.value.includes('이등변삼각형에서 빼기')) accidental=k.side!=='equilateral';
      else if(d.value.includes('같은 두 변만')) accidental=k.side==='equilateral';
      else if(d.value.includes('세 변이 달라도')) accidental=k.side!=='scalene';
      else if(d.value.includes('예각 하나만')) accidental=k.angle==='acute';
      else if(d.value.includes('세 각이 예각인데')) accidental=k.angle!=='acute';
      else if(d.value.includes('돌리기만')) accidental=false;
      else if(d.value.includes('서로 다른 길이')) accidental=false;
      else throw new Error('Unrecognized distractor: '+d.value);
      if(accidental) result.accidentalTrueDistractors++;
    }
  }
}
for(let seed=1;seed<=500;seed++) {
  for (const injectError of [false,true]) {
    const g=new D({seed});g.start({skipTutorial:true});if(injectError){g.forceWrong();g.next();}
    for(let i=0;i<10;i++){g.forceCorrect();g.tick(4);g.next();}
    assert(g.state.phase==='clear' && g.state.solved===10 && g.state.lives===(injectError?2:3),'normal/recoverable win '+seed);
    if(injectError)result.firstErrorThenWin++;else result.normalRuns++;
  }
}
for(const n of [40,17,63]) { const g=new D({seed:334});const rows=g.sampleProblems(n);const distinct=new Set(rows.map(r=>r.piece.sides.slice().sort((a,b)=>a-b).join('-')+r.piece.mode+r.piece.variant)).size;assert(rows.length===n&&distinct/n>=.7,'sample diversity');result.samples.push({requested:n,returned:rows.length,distinctNumbersAndTypes:distinct,ratio:distinct/n}); }
for (const shape of D.POOLS.equilateral) { const p=D.makePiece(shape,{mode:'side'});assert(D.acceptsPiece(p),'equilateral excluded');result.inclusionChecks++; }
for (const shape of D.POOLS.acute) { const g=new D({seed:44});g.start({skipTutorial:true});const p=D.makePiece(shape,{mode:'angle'});g.state.pieces=[p];g.selectPiece(0);g.measureAngle(0);assert(g.state.solved===0,'one acute auto solved');g.measureAngle(1);assert(g.state.solved===0,'two acute auto solved');g.measureAngle(2);assert(g.state.solved===1,'all acute not solved');result.acuteAllThreeChecks++; }
for(let seed=1;seed<=100;seed++) {
 const g=new D({seed});g.start({skipTutorial:true});g.tick(20);assert(g.state.pieces.length===2,'elapsed20 second piece');const first=g.state.piece;g.lock(0);g.selectPiece(1);g.forceCorrect();g.next();assert(g.state.pieces.includes(first)&&first.locked===0,'piece switching lost ruler');assert(g.state.pieces.length===2,'completed side slot not refilled');
 const h=new D({seed});h.start({skipTutorial:true});const scar=h.state.piece;h.forceWrong();assert(h.state.pieces.length===2&&h.state.pieces.includes(scar)&&scar.teeth===1,'wrong did not retain scar and narrow next slot');h.next();assert(h.state.lives===2,'wrong recovered life');
 for(const p of h.deck){if(p.variant==='reverse')assert(p.arcs.length===2&&p.sideType==='isosceles','reverse has no equal arcs');}
}
result.concurrencyRuns=100;
const endClock=new D({seed:51});endClock.start({skipTutorial:true});endClock.tick(89.9);endClock.forceWrong();endClock.tick(.2);assert(endClock.state.phase==='gameover'&&endClock.state.timeLeft===0&&endClock.state.feedback.kind==='wrong'&&endClock.state.feedback.reveal,'90-second timeout erased error math reveal');result.endClockReveal=true;
const fail=new D({seed:7});fail.start({skipTutorial:true});for(let i=0;i<3;i++){fail.forceWrong();fail.next();}assert(fail.state.phase==='gameover'&&fail.state.lives===0,'lives zero did not end');fail.tick(90);fail.lock(0);assert(fail.state.phase==='gameover'&&fail.state.lives===0,'auto revive');fail.forceCorrect();fail.forceWrong();assert(fail.state.phase==='gameover'&&fail.state.lives===0,'test hook revived ended run');
assert(result.invalidTriangles===0&&result.accidentalTrueDistractors===0,'math audit failed');
fs.writeFileSync('logs/jareul-beollyeo-build/engine-audit.json',JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result,null,2));
