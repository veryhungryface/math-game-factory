#!/usr/bin/env node
// Teacher-facing structural validator. It deliberately does not claim to prove
// a new unit's mathematics; use an independent oracle appropriate to that unit.
import fs from 'node:fs';
const file=process.argv[2];if(!file){console.error('사용법: node ArtSource/packs/validate-pack.mjs <팩.json>');process.exit(2);}
const pack=JSON.parse(fs.readFileSync(file,'utf8'));const errors=[];
function assert(ok,msg){if(!ok)errors.push(msg);}
function gcd(a,b){return b?gcd(b,a%b):Math.abs(a)||1;}
function numeric(s){const m=/^\{frac:(-?\d+)\/(\d+)\}$/.exec(s);if(m){const n=Number(m[1]),d=Number(m[2]);assert(d>0&&gcd(n,d)===1,`기약분수가 아님: ${s}`);return `${n}/${d}`;}if(/^-?\d+$/.test(s))return `${Number(s)}/1`;return s;}
assert(/^[a-z0-9][a-z0-9-]*$/.test(pack.pack_id),'pack_id: 영문 소문자·숫자·하이픈');
assert(typeof pack.title==='string'&&pack.title.length>0,'title 필요');assert(['elementary','middle'].includes(pack.school),'school: elementary 또는 middle');
assert(Number.isInteger(pack.grade)&&pack.grade>=1,'grade 필요');assert([1,2].includes(pack.semester),'semester: 1 또는 2');assert(typeof pack.unit_id==='string','unit_id 필요');assert(Array.isArray(pack.standards)&&pack.standards.length>0,'standards 필요');assert(Array.isArray(pack.items)&&pack.items.length>=300,'고유 문항 300개 이상 필요');
const ids=new Set(),prompts=new Set(),bands=new Set();
for(const q of pack.items??[]){const id=q.id;assert(typeof id==='string'&&!ids.has(id),`중복 또는 없는 id: ${id}`);ids.add(id);const prompt=q.prompt??'';const norm=prompt.replace(/\([^)]*\)/g,'').replace(/\s+/g,' ').trim();assert(norm&&!prompts.has(norm),`${id}: 중복 또는 빈 발문`);prompts.add(norm);assert(['frac','int','text'].includes(q.format),`${id}: format 필요`);assert(q.choices?.length===4,`${id}: 보기 4개 필요`);const choices=q.choices??[];assert(new Set(choices.map(numeric)).size===4,`${id}: 같거나 동치인 보기`);assert(choices.filter(v=>v===q.answer).length===1,`${id}: answer는 보기 중 정확히 하나`);assert(q.explain?.length>0&&q.unitConcept?.length>0,`${id}: explain/unitConcept 필요`);assert(new Set(q.distractor_tags??[]).size>=2,`${id}: 서로 다른 오개념 태그 2개 이상 필요`);assert([1,2,3,4].includes(q.difficulty),`${id}: difficulty 1~4 필요`);bands.add(q.difficulty);assert(!/\d+\s*\/\s*\d+/.test([prompt,q.explain,...choices].join(' ').replace(/\{frac:-?\d+\/\d+\}/g,'')),`${id}: 평문 분수 금지`);if(q.format!=='text')assert(Number.isFinite(q.answerNumeric),`${id}: 수치 QA용 answerNumeric 필요`);}
assert([1,2,3,4].every(n=>bands.has(n)),'난이도 1~4를 모두 넣어 주세요');
console.log(JSON.stringify({pack_id:pack.pack_id,items:pack.items?.length,structural_errors:errors.length,errors,mathematics:'새 단원은 별도의 정수·유리수 전수 검산이 필요합니다.'},null,2));if(errors.length)process.exitCode=1;
