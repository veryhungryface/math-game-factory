#!/usr/bin/env node
// Structural gate for teacher-authored packs. Mathematics still needs an independent oracle.
import fs from 'node:fs';
import {pathToFileURL} from 'node:url';
export function validatePack(pack,{minItems=300}={}) {
 const errors=[],warnings=[],modes={amount:0,fraction_parts:0,choice:0},accepts={exact_parts:0,equivalent:0,reduced:0};
 const assert=(ok,msg)=>{if(!ok)errors.push(msg);};
 const gcd=(a,b)=>b?gcd(b,a%b):Math.abs(a)||1;
 const natural=n=>Number.isSafeInteger(n)&&n>=0;
 function token(s,id){if(typeof s!=='string'){assert(false,`${id}: choice answer/string required`);return String(s);}const m=/^\{frac:(-?\d+)\/(\d+)\}$/.exec(s);if(m){const n=Number(m[1]),d=Number(m[2]);assert(d>0&&gcd(n,d)===1,`${id}: 기약분수가 아님: ${s}`);return `${n}/${d}`;}if(/^-?\d+$/.test(s))return `${Number(s)}/1`;return s;}
 assert(/^[a-z0-9][a-z0-9-]*$/.test(pack.pack_id),'pack_id: 영문 소문자·숫자·하이픈');
 assert(typeof pack.title==='string'&&pack.title.length>0,'title 필요');assert(['elementary','middle'].includes(pack.school),'school: elementary 또는 middle');
 assert(Number.isInteger(pack.grade)&&pack.grade>=1,'grade 필요');assert([1,2].includes(pack.semester),'semester: 1 또는 2');assert(typeof pack.unit_id==='string','unit_id 필요');assert(Array.isArray(pack.standards)&&pack.standards.length>0,'standards 필요');assert(Array.isArray(pack.items)&&pack.items.length>=minItems,`고유 문항 ${minItems}개 이상 필요`);
 assert(pack.schema_version===undefined||[1,2].includes(pack.schema_version),'schema_version: 1 또는 2');
 const economy=pack.economy;
 if(pack.schema_version===2||economy){assert(economy?.carry_capacity===120,'현재 런타임 economy.carry_capacity: 120 고정');assert(economy?.coin_per_kill===1,'현재 런타임 economy.coin_per_kill: 1 고정');assert(economy?.min_spawn_coins===140,'현재 런타임 economy.min_spawn_coins: 140 고정');}
 const ids=new Set(),prompts=new Set(),bands=new Set();
 for(const q of Array.isArray(pack.items)?pack.items:[]){
  const id=q.id,mode=q.answer_mode??'choice';assert(typeof id==='string'&&!ids.has(id),`중복 또는 없는 id: ${id}`);ids.add(id);
  const prompt=q.prompt??'',norm=prompt.replace(/\([^)]*\)/g,'').replace(/\s+/g,' ').trim();assert(norm&&!prompts.has(norm),`${id}: 중복 또는 빈 발문`);prompts.add(norm);
  assert(['amount','fraction_parts','choice'].includes(mode),`${id}: answer_mode 오류`);if(mode in modes)modes[mode]++;
  assert(['frac','int','text'].includes(q.format),`${id}: format 필요`);assert(q.explain?.length>0&&q.unitConcept?.length>0,`${id}: explain/unitConcept 필요`);assert([1,2,3,4].includes(q.difficulty),`${id}: difficulty 1~4 필요`);bands.add(q.difficulty);
  if(mode==='choice'){
   assert(Array.isArray(q.choices)&&q.choices.length===4,`${id}: 보기 4개 필요`);const choices=Array.isArray(q.choices)?q.choices:[];
   assert(new Set(choices.map(v=>token(v,id))).size===4,`${id}: 같거나 동치인 보기`);assert(typeof q.answer==='string'&&choices.filter(v=>v===q.answer).length===1,`${id}: answer는 보기 중 정확히 하나`);assert(new Set(q.distractor_tags??[]).size>=2,`${id}: 서로 다른 오개념 태그 2개 이상 필요`);
  } else if(['amount','fraction_parts'].includes(mode)){
   assert(natural(q.max)&&q.max>0&&q.max<=60,`${id}: max는 1~60 정수`);assert(q.choices===null||q.choices===undefined,`${id}: 붓기 모드는 choices:null`);
   let cost=0;
   if(mode==='amount'){assert(natural(q.answer)&&q.answer<=q.max,`${id}: amount answer는 0~max 정수`);assert(q.format==='int',`${id}: amount format:int`);cost=q.answer;}
   else {
    const a=q.answer,accept=q.accept;assert(a&&typeof a==='object'&&!Array.isArray(a),`${id}: fraction answer:{num,den}`);
    assert(natural(a?.num)&&natural(a?.den)&&a.den>0&&a.num<=q.max&&a.den<=q.max,`${id}: 분자 0~max, 분모 1~max`);
    assert(['exact_parts','equivalent','reduced'].includes(accept),`${id}: accept 필요`);if(accept in accepts)accepts[accept]++;
    if(accept==='reduced')assert(gcd(a?.num,a?.den)===1,`${id}: reduced answer는 기약분수`);
    assert(typeof q.num_label==='string'&&q.num_label.length>0&&typeof q.den_label==='string'&&q.den_label.length>0,`${id}: num_label/den_label 필요`);assert(q.format==='frac',`${id}: fraction_parts format:frac`);cost=(a?.num??0)+(a?.den??0);
    if(accept==='reduced')assert(prompt.includes('기약분수'),`${id}: reduced 발문에 기약분수 명시`);
    if(accept==='exact_parts')assert(prompt.includes('약분하지 말고'),`${id}: exact_parts 발문에 세는 양/약분하지 않음 명시`);
   }
   const supportedBudget=mode==='fraction_parts'?120:60,budget=q.coin_budget??supportedBudget;assert(budget===supportedBudget,`${id}: 현재 런타임 coin_budget은 ${supportedBudget} 고정`);assert(natural(budget)&&budget>=cost,`${id}: coin_budget이 정답 비용보다 작음`);
   if(economy)assert(budget<=economy.carry_capacity&&budget<=economy.min_spawn_coins,`${id}: coin_budget을 소지/처치 공급이 보장하지 않음`);
  }
  assert(!/\d+\s*\/\s*\d+/.test([prompt,q.explain,...(q.choices??[])].join(' ').replace(/\{frac:-?\d+\/\d+\}/g,'')),`${id}: 평문 분수 금지`);
  if(q.format!=='text')assert(Number.isFinite(q.answerNumeric),`${id}: 수치 QA용 answerNumeric 필요`);
 }
 assert([1,2,3,4].every(n=>bands.has(n)),'난이도 1~4를 모두 넣어 주세요');
 if(pack.schema_version===2){const first=pack.items?.[0];assert(first?.answer_mode==='amount'&&first.answer>=1&&first.answer<=4,'첫 문항은 작은 amount(1~4)');if(modes.choice>pack.items.length*.3)warnings.push('choice 비율이 권장 30%를 초과합니다.');}
 return {pack_id:pack.pack_id,items:pack.items?.length,modes,accepts,structural_errors:errors.length,errors,warnings,mathematics:'새 단원은 별도의 정수·유리수 전수 검산이 필요합니다.'};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(process.argv[1]).href){const file=process.argv[2];if(!file){console.error('사용법: node ArtSource/packs/validate-pack.mjs <팩.json> [--fixture]');process.exit(2);}const result=validatePack(JSON.parse(fs.readFileSync(file,'utf8')),{minItems:process.argv.includes('--fixture')?1:300});console.log(JSON.stringify(result,null,2));if(result.structural_errors)process.exitCode=1;}
