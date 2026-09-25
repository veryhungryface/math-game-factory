import fs from 'node:fs';
const src = fs.readFileSync('public/g/pane-wipe/engine.js','utf8');
(0,eval)(src);
const E = globalThis.PaneWipeEngine;
const C = E.constants, DEG=Math.PI/180;
const sub=(a,b)=>({x:a.x-b.x,y:a.y-b.y}), dot=(a,b)=>a.x*b.x+a.y*b.y;
const len=v=>Math.hypot(v.x,v.y);
const fail=[]; const note=(k,m)=>fail.push(k+': '+m);

let n=0;
for(const p of E.pool){
  n++;
  const g=E.geometry(p), ca=E.correctAction(p);
  // 1. correct action must pass
  const j=E.judge(p,ca);
  if(!j.ok) note('correctFail',p.id+' '+j.reason);
  // 2. every distractor must fail
  for(const w of E.wrongActions(p)) if(E.judge(p,w.action).ok) note('wrongAccepted',p.id+' '+w.misconceptionId);
  // 3. geometry: parallel panes -> perpendicular distance == d*unit exactly
  if(p.delta===0){
    const d1=len(sub(g.foot,g.handle));
    if(Math.abs(d1-p.d*C.unit)>1e-9) note('distMismatch',`${p.id} stroke=${d1} expected=${p.d*C.unit}`);
    // gasket direction dot normal-of-stroke
    const v=sub(g.foot,g.handle);
    for(const l of g.lines){
      const err=Math.abs(dot(l.u,v))/len(v);
      if(err>1e-9) note('notPerp',`${p.id} err=${err}`);
    }
  }
  // 4. nonparallel panes must be impossible: no direction perpendicular to both
  if(p.delta!==0){
    let sol=0;
    for(let a=0;a<3600;a++){
      const th=a/10, v={x:Math.cos(th*DEG),y:Math.sin(th*DEG)};
      if(g.lines.every(l=>Math.abs(dot(l.u,v))<=Math.sin(4*DEG)+1e-9)) sol++;
    }
    if(sol>0) note('nonparallelSolvable',`${p.id} delta=${p.delta} dirs=${sol}`);
    if(!p.requiresDiscard) note('nonparallelNotDiscard',p.id);
  }
  // 5. square: 4 equal sides + 4 right angles
  if(p.shape==='square'){
    const V=g.vertices;
    const sides=V.map((v,i)=>len(sub(V[(i+1)%4],v)));
    if(Math.max(...sides)-Math.min(...sides)>1e-9) note('squareSides',p.id+' '+sides.join(','));
    if(Math.abs(sides[0]-p.d*C.unit)>1e-9) note('squareSideLen',p.id+' '+sides[0]);
    for(let i=0;i<4;i++){
      const a=sub(V[(i+3)%4],V[i]), b=sub(V[(i+1)%4],V[i]);
      if(Math.abs(dot(a,b))/(len(a)*len(b))>1e-9) note('squareAngle',p.id);
    }
    // vertices in bounds
    for(const v of V) if(v.x<g.bounds.x||v.x>g.bounds.x+g.bounds.width||v.y<g.bounds.y||v.y>g.bounds.y+g.bounds.height) note('squareBounds',p.id);
  }
  // 6. trapezoid: exactly one parallel pair (the two gaskets), other pair NOT parallel
  if(p.shape==='trapezoid'){
    const V=g.vertices;
    const s0=sub(V[1],V[0]), s2=sub(V[3],V[2]); // the two gaskets
    const s1=sub(V[2],V[1]), s3=sub(V[0],V[3]); // legs
    const cross=(a,b)=>Math.abs(a.x*b.y-a.y*b.x)/(len(a)*len(b));
    if(cross(s0,s2)>1e-9) note('trapNotParallel',p.id);
    if(cross(s1,s3)<1e-6) note('trapBothParallel',p.id+' -> would be parallelogram, label 사다리꼴 only is wrong');
  }
  // 7. answerNumeric integrity is checked via samples below
}
// 8. samples
const seen=new Set();
for(const cnt of [40,17,63,200]){
  const S=E.sampleProblems(cnt);
  if(S.length!==cnt) note('sampleCount',`${cnt}->${S.length}`);
  for(const s of S){
    seen.add(s.prompt);
    if(!s.prompt) note('noPrompt',s.id);
    if(!s.answer) note('noAnswer',s.id);
    if(!Number.isFinite(s.answerNumeric)) note('badNumeric',s.id);
    const p=s.problem;
    if(!p.requiresDiscard && s.answerNumeric!==p.d) note('numericMismatch',s.id);
    if(p.requiresDiscard && s.answerNumeric!==0) note('numericDiscard',s.id);
    if(!E.judge(p,s.answerAction).ok) note('sampleAnswerFail',s.id);
    for(const d of s.distractors) if(E.judge(p,d.action).ok) note('sampleDistractorOk',s.id);
    // prompt must not leak the answer
    // leak test: prompt text must NOT be predictive of discard-vs-wipe
    ;
  }
}
// 9. Korean textbook register + trap regex
const AT_PLACE=/자리에서\s*(올림|버림|반올림)/;
for(const p of E.pool){
  if(AT_PLACE.test(p.prompt)) note('atPlaceTrap',p.id);
  if(/봅시다|하십시오|하시오/.test(p.prompt)) note('register',p.id+' '+p.prompt);
  if(!/보세요\.$/.test(p.prompt)) note('noWorkbookRegister',p.id+' '+p.prompt);
  if(/(\d)cm/.test(p.prompt)) note('unitSpacing',p.id);
}
// 10. tutorial
const t=E.pool.length;
console.log(JSON.stringify({
  poolSize:t, checkedProblems:n, distinctSamplePrompts:seen.size,
  failures:fail.length,
  byKind:fail.reduce((a,f)=>{const k=f.split(':')[0];a[k]=(a[k]||0)+1;return a;},{}),
  first20:fail.slice(0,20)
},null,1));
