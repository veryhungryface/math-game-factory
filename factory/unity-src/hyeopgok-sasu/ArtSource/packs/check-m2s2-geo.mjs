#!/usr/bin/env node
// Independent oracle for m2s2-u1~u5 (geometry), schema v3 (all items four-option choice).
// Imports NO generator module. Every answer is recomputed from values parsed back out of the
// student-facing prompt text (the proofs ledger only names the item kind), by a different route
// than the generator: brute-force search over candidate answers, angle-sum systems, exact
// rational coordinates, lattice enumeration of quadrilaterals. Integer / rational arithmetic only.
// Per-unit oracle tables live in check-m2s2-geo-u1..u5.mjs; shared primitives in check-m2s2-geo-lib.mjs.
// Probability / counting packs (u6, u7) are checked by check-m2s2-prob.mjs.
//
// For every numeric item the oracle value is compared against ALL four options: exactly one option
// must equal it (the one marked answer), the other three must differ in value from it and from each
// other (fractions/ratios compared after reduction).
import fs from 'node:fs';
import path from 'node:path';
import {here,out,curriculum,failures,stats,fail,gcd} from './check-m2s2-geo-lib.mjs';
import {validatePackV3,optionValue,valueKey} from './validate-pack-v3-m2s2.mjs';
import U1T from './check-m2s2-geo-u1.mjs';
import U2T from './check-m2s2-geo-u2.mjs';
import U3T from './check-m2s2-geo-u3.mjs';
import U4T from './check-m2s2-geo-u4.mjs';
import U5T from './check-m2s2-geo-u5.mjs';

const UNITS={'m2s2-u1':U1T,'m2s2-u2':U2T,'m2s2-u3':U3T,'m2s2-u4':U4T,'m2s2-u5':U5T};
const FORBIDDEN=/√|근호|가정|결론|다시 넣지 않|약분하지 말고|약분하지 않아도|붓|닢/;
// Unit on the options ↔ the quantity word the prompt asks for (「∠B의 크기를 구하시오.」 → 「70°」).
const WORD_UNIT={'°':['크기'],' cm':['길이','거리','높이','반지름의 길이','둘레의 길이'],' m':['길이','거리','높이'],' cm²':['넓이'],' cm³':['부피']};
const LEGACY_UNIT={'°':'도',' cm':'cm',' m':'m',' cm²':'cm²',' cm³':'cm³'};
const hasFinal=w=>{const c=w.slice(-1).charCodeAt(0);return c>=0xac00&&c<=0xd7a3&&(c-0xac00)%28!==0;};
// Rebuild the unit-explicit sentence the oracle tables parse (「…의 크기는 몇 도인지 구하시오.」).
function legacyPrompt(q,unit){
  const m=/(\S+)(을|를) 구하시오\./.exec(q.prompt);if(!m)throw Error(`unit ${JSON.stringify(unit)} on options but no 「…을/를 구하시오」 ending`);
  const word=m[1];if(!WORD_UNIT[unit].some(w=>word.endsWith(w)))throw Error(`option unit ${JSON.stringify(unit)} does not fit the asked quantity 「${word}」`);
  if((m[2]==='을')!==hasFinal(word))throw Error(`josa 을/를 after 「${word}」`);
  if(unit===' cm'||unit===' m'){const u=unit.trim();const data=q.prompt.replace(m[0],'');if(!new RegExp(`\\d ${u}(?![²³a-z])`).test(data))throw Error(`answer unit ${u} not used by the given data`);}
  return q.prompt.replace(m[0],`${word}${hasFinal(word)?'은':'는'} 몇 ${LEGACY_UNIT[unit]}인지 구하시오.`);
}
function promptNums(prompt){const r=prompt.replace(/\{frac:(\d+)\/(\d+)\}/g,' ');return new Set([...r.matchAll(/\d+/g)].map(m=>Number(m[0])));}
const eqv=(a,b)=>a[0]*b[1]===b[0]*a[1];

// Checks one item; throws on the first defect. Returns {slot, source}.
function checkItem(packId,S,q,p){
 if(!p)throw Error('no proof entry');
 if(q.answer_mode!=='choice')throw Error('v3: every item is choice');
 if(!Array.isArray(q.choices)||q.choices.length!==4||new Set(q.choices).size!==4)throw Error('exactly four distinct options');
 if(q.choices.filter(c=>c===q.answer).length!==1)throw Error('answer must be exactly one option');
 const text=[q.prompt,q.explain,...q.choices].join(' ');if(FORBIDDEN.test(text))throw Error('forbidden wording (√·가정·결론·비복원·약분 지시·붓기)');
 if(/\d+\s*\/\s*\d+/.test(text.replace(/\{frac:\d+\/\d+\}/g,'')))throw Error('plain fraction');
 if(!q.distractor_tags.every(t=>typeof t==='string'&&t.startsWith(packId+'.')))throw Error('distractor tag namespace');
 if(new Set(q.distractor_tags).size<2)throw Error('need 2+ misconception tags');
 if(p.source==='text'){
  const fn=S.C[p.kind];if(!fn)throw Error(`no choice oracle for ${p.kind}`);
  let opts=q.choices;const labelled=q.choices.join('')==='ㄱㄴㄷㄹ';
  if(labelled){const m=/ ㄱ\. (.+?)  ㄴ\. (.+?)  ㄷ\. (.+?)  ㄹ\. (.+?)(?: \(단[^)]*\))?$/.exec(q.prompt);if(!m)throw Error('labelled options not parseable');opts=[m[1],m[2],m[3],m[4]];}
  if(new Set(opts).size!==4)throw Error('duplicate options');
  const truth=fn(q,opts,p);stats.choiceOptions+=4;
  if(truth.filter(Boolean).length!==1)throw Error(`exactly one option must be correct, oracle says ${truth}`);
  const idx=truth.indexOf(true);if(q.choices[idx]!==q.answer)throw Error(`answer ${q.answer} but oracle picks ${q.choices[idx]}`);
  return {slot:idx,source:'text'};
 }
 // numeric: int (optionally with unit), frac, ratio
 const vals=q.choices.map(optionValue);
 let truthVal,legacy=q;
 if(p.source==='int'){
  if(!vals.every(v=>v.kind==='num'&&v.d===1))throw Error('integer item with non-integer option');
  const units=new Set(vals.map(v=>v.unit));if(units.size!==1)throw Error('mixed units');const unit=[...units][0];
  if(unit)legacy={...q,prompt:legacyPrompt(q,unit)};
  else if(/(크기|길이|넓이|부피|거리|높이)(을|를) 구하시오\./.test(q.prompt))throw Error('quantity asked without a unit on the options');
  if((q.format==='int')!==(unit===''))throw Error('format int ⇔ unitless integers');
  const fn=S.A[p.kind];if(!fn)throw Error(`no integer oracle for ${p.kind}`);truthVal=[fn(legacy,p),1];
  if(!Number.isSafeInteger(truthVal[0]))throw Error('oracle is not an integer');
 } else if(p.source==='frac'||p.source==='ratio'){
  const want=p.source==='frac'?'num':'ratio';if(!vals.every(v=>v.kind===want&&v.reduced||(want==='num'&&v.kind==='num'&&v.d===1&&v.unit==='')))throw Error(`${p.source} options must be reduced ${p.source==='frac'?'{frac}':'a:b'} tokens`);
  if(p.source==='frac'&&q.format!=='frac')throw Error('format frac');if(p.source==='ratio'&&q.format!=='text')throw Error('ratio format text');
  const fn=S.F?.[p.kind];if(!fn)throw Error(`no rational oracle for ${p.kind}`);truthVal=fn(q,p);
  if(!(truthVal[1]>0))throw Error('oracle denominator');
 } else throw Error(`unknown source ${p.source}`);
 stats.numericOptions+=4;
 if(new Set(vals.map(valueKey)).size!==4)throw Error('options equal in value');
 const truth=vals.map(v=>eqv([v.n,v.d],truthVal));
 if(truth.filter(Boolean).length!==1)throw Error(`oracle ${truthVal.join('/')} matches ${truth.filter(Boolean).length} options (${q.choices})`);
 const idx=truth.indexOf(true);if(q.choices[idx]!==q.answer)throw Error(`answer ${q.answer} but oracle value is ${q.choices[idx]}`);
 const a=vals[idx];
 if(p.source!=='ratio'&&q.answerNumeric!==a.n/a.d)throw Error('answerNumeric');
 // answer-by-copying: the correct value must not be printed in the prompt
 if(p.source==='int'&&promptNums(q.prompt).has(a.n))throw Error(`answer ${a.n} is printed in the prompt`);
 if(p.source==='frac'&&q.prompt.includes(q.answer))throw Error('answer fraction printed in the prompt');
 if(p.source==='ratio'&&[...q.prompt.matchAll(/(\d+):(\d+)/g)].some(m=>eqv([+m[1],+m[2]],[a.n,a.d])))throw Error('answer ratio printed in the prompt');
 // explanation states the answer
 const g=gcd(truthVal[0],truthVal[1]);const says=p.source==='int'?new RegExp(`(^|[^0-9])${a.n}([^0-9]|$)`).test(q.explain):p.source==='frac'?q.explain.includes(`{frac:${a.n}/${a.d}}`):q.explain.includes(`${a.n}:${a.d}`);
 if(!says)throw Error('explain does not state the answer');
 return {slot:idx,source:p.source,vals,idx};
}
function loadPack(packId){const file=path.join(out,`${packId}.json`);if(!fs.existsSync(file))return null;
 return {pack:JSON.parse(fs.readFileSync(file,'utf8')),proof:new Map(JSON.parse(fs.readFileSync(path.join(here,`${packId}-proofs.json`),'utf8')).proofs.map(p=>[p.id,p]))};}
const RANK_GATE=0.35;const reports=[];
for(const [packId,S] of Object.entries(UNITS)){
 const loaded=loadPack(packId);if(!loaded){fail(packId,'pack file missing');continue;}const {pack,proof}=loaded;
 const structural=validatePackV3(pack);for(const e of structural.errors)fail(packId,`validate-pack-v3: ${e}`);
 const unit=curriculum.units.find(u=>u.id===packId);if(!unit)fail(packId,'unit missing in curriculum');
 if(unit&&JSON.stringify([...pack.standards].sort())!==JSON.stringify([...unit.standards].sort()))fail(packId,`standards ${pack.standards} ≠ unit ${unit.standards}`);
 if(!pack.standards.every(c=>curriculum.standards.some(s=>s.code===c)))fail(packId,'unknown standard code');
 if(pack.school!=='middle'||pack.grade!==2||pack.semester!==2||pack.unit_id!==packId||pack.schema_version!==3)fail(packId,'school/grade/semester/unit_id/schema_version');
 if(pack.items[0].difficulty!==1)fail(packId,'items[0] must be difficulty 1');
 const bands={1:0,2:0,3:0,4:0},sources={},slots={},kinds={},concepts={};const prompts=new Set();
 const heur={numeric:0,rank:[0,0,0,0],answerIsMax:0,answerIsMin:0,optionInPrompt:0,answerOnlyNotInPrompt:0};
 for(const q of pack.items){stats.items++;try{
  const p=proof.get(q.id);bands[q.difficulty]++;if(p){kinds[p.kind]=(kinds[p.kind]??0)+1;sources[p.source]=(sources[p.source]??0)+1;}concepts[q.unitConcept]=(concepts[q.unitConcept]??0)+1;
  const key=q.prompt.replace(/\([^)]*\)/g,'').replace(/\s+/g,' ').trim();if(prompts.has(key))throw Error('duplicate prompt');prompts.add(key);
  const r=checkItem(packId,S,q,p);(slots[q.difficulty]??=[0,0,0,0])[r.slot]++;
  if(r.vals){heur.numeric++;const x=r.vals.map(v=>v.n/v.d);const ans=x[r.idx];heur.rank[x.filter(v=>v<x[r.idx]).length]++;if(ans===Math.max(...x))heur.answerIsMax++;if(ans===Math.min(...x))heur.answerIsMin++;
   if(p.source==='int'){const pn=promptNums(q.prompt);const inP=r.vals.map(v=>pn.has(v.n));if(inP.some(Boolean))heur.optionInPrompt++;if(inP.filter(b=>!b).length===1&&!inP[r.idx])heur.answerOnlyNotInPrompt++;}}
 }catch(e){fail(q.id,e.message);}}
 for(const [b,s] of Object.entries(slots))if(Math.max(...s)-Math.min(...s)>1)fail(packId,`answer slot imbalance in difficulty ${b}: ${s}`);
 // value-rank of the correct option among numeric options (0 smallest … 3 largest): no single rank and no
 // "one of two ranks" rule (e.g. never the largest/smallest pad) may beat chance by much.
 {const R=heur.rank,n=heur.numeric||1;heur.rankShare=R.map(c=>+(c/n).toFixed(3));heur.bestRankStrategy=+Math.max(...R.map(c=>c/n),...[[0,1],[0,2],[0,3],[1,2],[1,3],[2,3]].map(([a,b])=>(R[a]+R[b])/2/n)).toFixed(3);
  if(heur.numeric&&heur.bestRankStrategy>RANK_GATE)fail(packId,`value-rank strategy wins ${heur.bestRankStrategy} > ${RANK_GATE} (ranks ${R})`);}
 if(Object.values(bands).some(n=>n<pack.items.length*.15))fail(packId,`difficulty band too thin ${JSON.stringify(bands)}`);
 if(pack.items.length<300)fail(packId,'fewer than 300 items');
 const slotTotal=[0,0,0,0];for(const s of Object.values(slots))s.forEach((n,i)=>slotTotal[i]+=n);
 reports.push({pack_id:packId,items:pack.items.length,sources,bands,slots,slotTotal,heuristics:heur,kinds:Object.keys(kinds).length,concepts,structural_errors:structural.structural_errors});
}
// ── mutation self-test: every planted defect must be rejected ──
const selftest={answerShift:[0,0],correctCorrupted:[0,0],plantedDuplicateCorrect:[0,0],plantedEquivalent:[0,0],promptNumber:[0,0]};const undetected=[];
if(process.argv.includes('--selftest')){const clone=o=>JSON.parse(JSON.stringify(o));
 const expectFail=(bucket,packId,S,q,p)=>{selftest[bucket][1]++;try{checkItem(packId,S,q,p);}catch{selftest[bucket][0]++;return;}if(bucket==='promptNumber')undetected.push(`${q.id}: ${q.prompt}`);else failures.push(`selftest ${bucket} not detected: ${q.id}`);};
 for(const [packId,S] of Object.entries(UNITS)){const loaded=loadPack(packId);if(!loaded)continue;const {pack,proof}=loaded;
  pack.items.forEach(orig=>{if(orig.answer_mode!=="choice"||!Array.isArray(orig.choices))return;const p=proof.get(orig.id);if(!p)return;const ai=orig.choices.indexOf(orig.answer);
   // 1. the answer key points at a distractor
   let q=clone(orig);q.answer=q.choices[(ai+1)%4];expectFail('answerShift',packId,S,q,p);
   if(p.source!=='text'){
    // 2. the correct option is replaced by a wrong value (no correct option left)
    q=clone(orig);const v=optionValue(orig.answer);let bad;
    for(let k=1;k<50&&!bad;k++){const c=p.source==='int'?`${v.n+k}${v.unit}`:p.source==='frac'?(()=>{const n=v.n+k*v.d+1,d=v.d,g=gcd(n,d);return d/g===1?String(n/g):`{frac:${n/g}/${d/g}}`;})():(()=>{const n=v.n+k,g=gcd(n,v.d);return `${n/g}:${v.d/g}`;})();
     if(!orig.choices.map(x=>valueKey(optionValue(x))).includes(valueKey(optionValue(c))))bad=c;}
    q.choices[ai]=bad;q.answer=bad;q.answerNumeric=(o=>o.n/o.d)(optionValue(bad));expectFail('correctCorrupted',packId,S,q,p);
    // 3. a distractor is overwritten by the correct option (two correct pads)
    q=clone(orig);q.choices[(ai+1)%4]=orig.answer;expectFail('plantedDuplicateCorrect',packId,S,q,p);
    // 4. a distractor is overwritten by an equivalent, unreduced form of the answer
    if(p.source!=='int'){q=clone(orig);q.choices[(ai+2)%4]=p.source==='frac'?`{frac:${2*v.n}/${2*v.d}}`:`${2*v.n}:${2*v.d}`;expectFail('plantedEquivalent',packId,S,q,p);}
    // 5. the first number of the prompt is bumped; the recorded answer must no longer match
    q=clone(orig);const mm=/(^|[^0-9/:])(\d+)(?=[^0-9/:]|$)/.exec(q.prompt.replace(/\{frac:\d+\/\d+\}/g,f=>'#'.repeat(f.length)));
    if(mm){const at=mm.index+mm[1].length;q.prompt=q.prompt.slice(0,at)+String(Number(mm[2])+1)+q.prompt.slice(at+mm[2].length);expectFail('promptNumber',packId,S,q,p);}
   } else {
    // text item: overwrite a distractor with the correct text
    q=clone(orig);if(orig.choices.join('')!=='ㄱㄴㄷㄹ'){q.choices[(ai+1)%4]=orig.answer;expectFail('plantedDuplicateCorrect',packId,S,q,p);}
   }
  });}}
const result={verdict:failures.length?'fail':'pass',...stats,
 selftest:process.argv.includes('--selftest')?{...Object.fromEntries(Object.entries(selftest).map(([k,v])=>[k,`${v[0]}/${v[1]} detected`])),promptNumberUnchangedAnswer:undetected}:'not run (pass --selftest)',
 failures:failures.length,packs:reports,errors:failures.slice(0,300)};
fs.writeFileSync(path.join(here,'check-m2s2-geo-report.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify({verdict:result.verdict,items:stats.items,failures:failures.length,selftest:result.selftest,packs:reports.map(r=>({pack_id:r.pack_id,items:r.items,sources:r.sources,bands:r.bands,slotTotal:r.slotTotal,heuristics:r.heuristics}))},null,1));
if(failures.length){console.error(failures.slice(0,40).join('\n'));process.exitCode=1;}
