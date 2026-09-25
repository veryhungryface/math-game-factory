import fs from 'node:fs';import vm from 'node:vm';
const s={module:{exports:{}},exports:{}};
vm.runInNewContext(fs.readFileSync('public/g/star-cork/engine.js','utf8'),s);
const E=s.module.exports;
const fail=[],note=[];
const types={};for(const p of E.allProblems)types[p.type]=(types[p.type]||0)+1;

// 1) legend arithmetic is integer-only and solution sums to target
for(const p of E.allProblems){
  const lb=p.legendBig, ls=p.legendSmall;
  if(!Number.isInteger(lb)||!Number.isInteger(ls)) fail.push(['legend not integer',p.id,lb,ls]);
  if(ls*10!==lb) fail.push(['legendSmall != legendBig/10',p.id,lb,ls]);
  if(p.allowHalf && lb!==10) fail.push(['half allowed with legendBig!=10',p.id,lb]);
  const manual=(p.solution||[]).reduce((a,t)=>a+(t==='big'?lb:t==='small'?ls:lb/2),0);
  if(!Number.isInteger(manual)) fail.push(['non-integer solution sum',p.id,manual]);
  if(manual!==p.target) fail.push(['solution sum != target',p.id,manual,p.target]);
  if(E.sum(p.solution,p)!==p.target) fail.push(['engine sum != target',p.id,E.sum(p.solution,p),p.target]);
  // engine must accept its own solution
  const ev=E.evaluate?E.evaluate(p.solution,p):null;
  if(ev && ev.ok===false) fail.push(['engine rejects its own solution',p.id,JSON.stringify(ev)]);
  // stock
  const have=E.counts(p.belt), need=E.counts(p.solution), init=E.counts(p.initial||[]);
  for(const t of ['big','small','half']) if((have[t]||0)+(init[t]||0)<(need[t]||0)) fail.push(['insufficient stock',p.id,t]);
  // belt total must exceed target so mashing everything overshoots
  const beltTotal=E.sum(p.belt,p)+E.sum(p.initial||[],p);
  if(beltTotal<=p.target) fail.push(['belt total <= target (mash-all wins)',p.id,beltTotal,p.target]);
  // distractors
  const d=E.distractors(p), v=d.map(x=>x.value);
  if(v.length!==3||new Set(v).size!==3||v.includes(p.target)) fail.push(['bad distractors',p.id,v,p.target]);
  if(v.some(x=>!Number.isInteger(x)||x<0)) fail.push(['distractor not a natural number',p.id,v]);
  // prompt/target coherence for graph-reading types
  if(p.rows?.length){
    const named=p.rows.filter(r=>p.prompt.includes(r.name));
    const vals=p.rows.map(r=>E.rowValue(r,p));
    if(vals.some(x=>!Number.isInteger(x))) fail.push(['non-integer row value',p.id,vals]);
    if(p.type==='read'){ if(named.length!==1||E.rowValue(named[0],p)!==p.target) fail.push(['read: prompt row != target',p.id,p.prompt,vals,p.target]); }
    if(p.type==='max'){ if(Math.max(...vals)!==p.target||vals.filter(x=>x===p.target).length!==1) fail.push(['max: not a unique maximum',p.id,vals,p.target]); }
    if(p.type==='difference'){
      const two=p.rows.filter(r=>p.prompt.includes(r.name)).map(r=>E.rowValue(r,p));
      const d2=two.length===2?Math.abs(two[0]-two[1]):Math.max(...vals)-Math.min(...vals);
      if(d2!==p.target) fail.push(['difference: prompt pair diff != target',p.id,p.prompt,vals,two,p.target]);
    }
  }
  if(p.sourceTable?.length){
    for(const r of p.sourceTable){ if(!Number.isInteger(r.value)) fail.push(['table value not integer',p.id]); }
    const named=p.sourceTable.filter(r=>p.prompt.includes(r.name));
    if(named.length===1&&named[0].value!==p.target) fail.push(['table: named row value != target',p.id,named[0],p.target]);
    if(!p.prompt.includes(String(p.target))) note.push(['table prompt omits target number',p.id,p.prompt]);
  }
  if(p.type==='error_find'){
    const cur=E.sum(p.initial||[],p);
    if(cur===p.target) fail.push(['error_find starts already correct',p.id]);
    // must be repairable by removing only
    const need2=E.counts(p.solution),ini=E.counts(p.initial||[]);
    const removable=['big','small','half'].every(t=>(ini[t]||0)>=(need2[t]||0));
    if(!removable) note.push(['error_find needs additions, not only removals',p.id]);
  }
  // textbook phrasing traps
  if(/의 자리에서 (올림|버림|반올림)/.test(p.prompt)) fail.push(['AT_PLACE rounding phrasing',p.id,p.prompt]);
  if(/눈금|막대그래프/.test(p.prompt)) fail.push(['forbidden 눈금/막대그래프 wording',p.id,p.prompt]);
  if(/하십시오|하시오/.test(p.prompt)) fail.push(['하십시오체',p.id,p.prompt]);
}

// 2) sampleProblems contract
const sampleReport=[];
for(const n of [40,17,63,1,205]){
  const arr=E.sampleProblems(n);
  const bad=[];
  for(const q of arr){
    if(!q.prompt||!q.answer) bad.push(['missing prompt/answer',q.id]);
    if(!Array.isArray(q.choices)||q.choices.length!==4) bad.push(['choices!=4',q.id]);
    else{
      if(new Set(q.choices).size!==4) bad.push(['dup choices',q.id,q.choices]);
      if(q.choices.filter(c=>c===q.answer).length!==1) bad.push(['answer not exactly once',q.id,q.choices,q.answer]);
    }
    if(Number(q.answer.replace(/[^0-9-]/g,''))!==q.answerNumeric) bad.push(['answerNumeric mismatch',q.id,q.answer,q.answerNumeric]);
    if(!q.unitConcept) bad.push(['no unitConcept',q.id]);
  }
  sampleReport.push({n,received:arr.length,unique:new Set(arr.map(x=>x.prompt)).size,bad:bad.slice(0,5),badCount:bad.length});
  if(arr.length!==n) fail.push(['sampleProblems ignored n',n,arr.length]);
  if(bad.length) fail.push(['sample contract violations',n,bad.length,JSON.stringify(bad[0])]);
}
console.log(JSON.stringify({types,pool:E.allProblems.length,sampleReport,failCount:fail.length,fail:fail.slice(0,25),note:note.slice(0,10)},null,1));
