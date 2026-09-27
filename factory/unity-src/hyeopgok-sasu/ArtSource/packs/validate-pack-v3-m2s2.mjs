#!/usr/bin/env node
// Structural gate for schema v3 packs (DESIGN-V3 §4: every item is a four-option choice).
// validate-pack.mjs (v1/v2) is left untouched on purpose — another agent owns the m2s1 packs.
// Mathematics is NOT checked here; check-m2s2-geo.mjs / check-m2s2-prob.mjs are the oracles.
import fs from 'node:fs';
import {pathToFileURL} from 'node:url';
const gcd=(a,b)=>{a=Math.abs(a);b=Math.abs(b);while(b)[a,b]=[b,a%b];return a||1;};
// Option value: integer (optionally with a unit suffix), {frac:a/b}, ratio a:b, or plain text.
export function optionValue(s){
  let m=/^\{frac:(\d+)\/(\d+)\}$/.exec(s);if(m)return {kind:'num',n:+m[1],d:+m[2],reduced:gcd(+m[1],+m[2])===1&&+m[2]>1,unit:''};
  m=/^(\d+)(°| cm²| cm³| cm| m)?$/.exec(s);if(m)return {kind:'num',n:+m[1],d:1,reduced:true,unit:m[2]??''};
  m=/^(\d+):(\d+)$/.exec(s);if(m)return {kind:'ratio',n:+m[1],d:+m[2],reduced:gcd(+m[1],+m[2])===1&&+m[2]>0};
  return {kind:'text',s};
}
export const valueKey=v=>v.kind==='text'?`t:${v.s}`:`${v.kind}:${v.n/gcd(v.n,v.d)}/${v.d/gcd(v.n,v.d)}`;
export function validatePackV3(pack,{minItems=300}={}) {
  const errors=[],warnings=[];const assert=(ok,msg)=>{if(!ok)errors.push(msg);};
  assert(pack.schema_version===3,'schema_version: 3');
  assert(/^[a-z0-9][a-z0-9-]*$/.test(pack.pack_id),'pack_id');assert(typeof pack.title==='string'&&pack.title.length>0,'title');
  assert(pack.school==='middle'&&Number.isInteger(pack.grade)&&[1,2].includes(pack.semester)&&typeof pack.unit_id==='string','school/grade/semester/unit_id');
  assert(Array.isArray(pack.standards)&&pack.standards.length>0,'standards');
  const items=Array.isArray(pack.items)?pack.items:[];assert(items.length>=minItems,`고유 문항 ${minItems}개 이상 필요 (${items.length})`);
  const ids=new Set(),prompts=new Set(),bands={};
  for(const q of items){const id=q.id;
    assert(typeof id==='string'&&!ids.has(id),`중복 또는 없는 id: ${id}`);ids.add(id);
    const key=(q.prompt??'').replace(/\([^)]*\)/g,'').replace(/\s+/g,' ').trim();assert(key&&!prompts.has(key),`${id}: 중복 또는 빈 발문`);prompts.add(key);
    assert(q.answer_mode==='choice',`${id}: answer_mode 는 choice 만`);
    assert(['int','frac','text'].includes(q.format),`${id}: format`);
    assert(typeof q.explain==='string'&&q.explain.length>4&&!/\n/.test(q.explain),`${id}: explain 한 줄`);
    assert(typeof q.unitConcept==='string'&&q.unitConcept.length>0,`${id}: unitConcept`);
    assert([1,2,3,4].includes(q.difficulty),`${id}: difficulty 1~4`);
    const ch=Array.isArray(q.choices)?q.choices:[];assert(ch.length===4,`${id}: 보기 정확히 4개`);
    assert(ch.every(c=>typeof c==='string'&&c.length>0),`${id}: 보기는 비어 있지 않은 문자열`);
    assert(typeof q.answer==='string'&&ch.filter(c=>c===q.answer).length===1,`${id}: answer 는 보기 중 정확히 하나`);
    const vals=ch.map(optionValue);
    assert(new Set(vals.map(valueKey)).size===4,`${id}: 값이 같은 보기(분수·비는 기약화 비교)`);
    for(const v of vals){if(v.kind!=='text')assert(v.reduced,`${id}: 기약이 아닌 분수/비 보기 ${ch}`);}
    const kinds=new Set(vals.map(v=>v.kind==='text'?'text':v.kind==='ratio'?'ratio':'num'));assert(kinds.size===1,`${id}: 보기 형식이 섞임 ${ch}`);
    if(vals.every(v=>v.kind==='num'))assert(new Set(vals.map(v=>v.unit)).size===1,`${id}: 보기 단위가 섞임 ${ch}`);
    if(q.format==='int')assert(vals.every(v=>v.kind==='num'&&v.d===1&&v.unit===''),`${id}: format:int 인데 정수 아닌 보기`);
    if(q.format!=='text'||q.answerNumeric!==undefined){const a=optionValue(q.answer);assert(a.kind==='num'&&q.answerNumeric===a.n/a.d,`${id}: answerNumeric`);}
    assert(new Set(q.distractor_tags??[]).size>=2,`${id}: 서로 다른 오개념 태그 2개 이상`);
    const text=[q.prompt,q.explain,...ch].join(' ');
    assert(!/\d+\s*\/\s*\d+/.test(text.replace(/\{frac:\d+\/\d+\}/g,'')),`${id}: 평문 분수 금지`);
    assert(!/약분하지 말고|약분하지 않아도|붓|닢/.test(text),`${id}: 붓기 시대 문구(약분하지 말고/붓/닢) 제거`);
    (bands[q.difficulty]??={n:0,slots:[0,0,0,0]}).n++;if(ch.includes(q.answer))bands[q.difficulty].slots[ch.indexOf(q.answer)]++;
  }
  assert([1,2,3,4].every(d=>bands[d]),'난이도 1~4 모두 필요');
  for(const [d,b] of Object.entries(bands))assert(Math.max(...b.slots)-Math.min(...b.slots)<=1,`난이도 ${d} 정답 위치 불균형 ${b.slots}`);
  assert(items[0]?.difficulty===1,'items[0] 은 쉬운 문항(난이도 1)');
  const all=[0,0,0,0];for(const b of Object.values(bands))b.slots.forEach((n,i)=>all[i]+=n);
  const counts=Object.fromEntries(Object.entries(bands).map(([d,b])=>[d,b.n]));
  const mean=items.length/4;if(Object.values(counts).some(n=>n<mean*.5))warnings.push(`난이도 분포 편중 ${JSON.stringify(counts)}`);
  return {pack_id:pack.pack_id,items:items.length,bands:counts,answerSlots:all,structural_errors:errors.length,errors,warnings};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(process.argv[1]).href){
  const files=process.argv.slice(2).filter(a=>!a.startsWith('--'));if(!files.length){console.error('사용법: node validate-pack-v3-m2s2.mjs <팩.json>...');process.exit(2);}
  let bad=0;for(const f of files){const r=validatePackV3(JSON.parse(fs.readFileSync(f,'utf8')),{minItems:process.argv.includes('--fixture')?1:300});
    console.log(JSON.stringify({...r,errors:r.errors.slice(0,20)}));if(r.structural_errors)bad++;}
  if(bad)process.exitCode=1;
}
