#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {validatePack} from './validate-pack.mjs';
const here=path.dirname(fileURLToPath(import.meta.url)),out=path.resolve(here,'../../../../../public/g/hyeopgok-sasu/packs');
const clone=v=>JSON.parse(JSON.stringify(v)),reports=[];
const old=JSON.parse(fs.readFileSync(path.join(here,'fixtures/v1-legacy.json'),'utf8'));
const p=JSON.parse(fs.readFileSync(path.join(out,'m2s2-u7.json'),'utf8'));
function run(name,pack,valid,minItems=300){const r=validatePack(pack,{minItems});const passed=(r.structural_errors===0)===valid;reports.push({name,passed,expectedValid:valid,errors:r.errors});if(!passed)throw Error(name);}
run('v1 without answer_mode maps all four questions to choice',old,true,1);
run('v2 all 444 canonical union answers',p,true);
let t=clone(p),q=t.items.find(q=>q.answer_mode==='fraction_parts');q.answer.den=0;run('reject denominator zero',t,false);
t=clone(p);q=t.items.find(q=>q.answer_mode==='fraction_parts');{const a=q.answer.num,b=q.answer.den,g=(x,y)=>y?g(y,x%y):Math.abs(x)||1,k=g(a,b);q.accept='reduced';q.answer.num=a/k;q.answer.den=b/k;q.prompt+=' 기약분수로 나타내시오.';}run('reduced remains structurally supported',t,true);
q.answer.num*=2;q.answer.den*=2;run('reject unreduced reduced answer',t,false);
t=clone(p);q=t.items.find(q=>q.answer_mode==='amount');q.answer=61;run('reject amount above max',t,false);
t=clone(p);t.economy.min_spawn_coins=30;run('reject insufficient wave supply',t,false);
t=clone(p);t.economy.min_spawn_coins=100000;run('reject unsupported high supply declaration',t,false);
t=clone(p);t.economy.carry_capacity=241;run('reject unsupported wallet capacity',t,false);
t=clone(p);q=t.items.find(q=>q.answer_mode==='amount');q.max=61;run('reject unsupported max 61',t,false);
t=clone(p);q=t.items.find(q=>q.answer_mode==='fraction_parts');q.coin_budget=1;run('reject insufficient coin budget',t,false);
t=clone(p);q=t.items.find(q=>q.answer_mode==='fraction_parts');q.accept='equivalent';q.num_label='분자';q.den_label='분모';run('equivalent is structurally supported',t,true);
t=clone(p);q=t.items.find(q=>q.answer_mode==='choice');q.answer='{frac:1/2}';q.answerNumeric=.5;q.format='frac';q.choices=['0','1','{frac:2/4}','2'];run('reject equivalent-only choice without exact answer token',t,false);
t=clone(p);q=t.items.find((q,i)=>i>0&&q.answer_mode==='amount');q.answer=0;q.answerNumeric=0;run('amount zero remains supported',t,true);
t=clone(p);q=t.items.find(q=>q.answer_mode==='fraction_parts');q.answer.num=0;q.answerNumeric=0;run('zero numerator remains supported',t,true);
fs.writeFileSync(path.join(here,'schema-report.json'),JSON.stringify({verdict:'pass',checks:reports.length,reports},null,2)+'\n');
console.log(JSON.stringify({verdict:'pass',checks:reports.length}));
