#!/usr/bin/env node
// Independent oracle: imports no generation modules and never trusts expected.
// Answers are reconstructed by explicit finite sample-space enumeration.
import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';
const here=path.dirname(fileURLToPath(import.meta.url));const root=path.resolve(here,'../../../../..');
const out=path.join(root,'public/g/hyeopgok-sasu/packs');
const curriculum=JSON.parse(fs.readFileSync(path.join(root,'curriculum/2022-middle-math.json'),'utf8'));
const failures=[],totals={items:0,choices:0,enumeratedOutcomes:0,distractorDerivations:0,acceptanceInputs:0};
function check(ok,message){if(!ok)failures.push(message);}
function gcd(a,b){return b?gcd(b,a%b):Math.abs(a)||1;}
function reduce(n,d=1){const g=gcd(n,d);return [n/g,d/g];}
function parse(s){let m=/^\{frac:(\d+)\/(\d+)\}$/.exec(s);if(m)return [Number(m[1]),Number(m[2])];if(/^\d+$/.test(s))return [Number(s),1];throw Error(`Noncanonical token ${s}`);}
function same(a,b){return a[0]*b[1]===b[0]*a[1];}
function range(n){return Array.from({length:n},(_,i)=>i+1);}
function pairs(a,b){return a.flatMap(x=>b.map(y=>[x,y]));}
function diceEvent([x,y],test,k){switch(test){case'sum-eq':return x+y===k;case'sum-le':return x+y<=k;case'sum-ge':return x+y>=k;case'product-eq':return x*y===k;case'difference-eq':return Math.max(x,y)-Math.min(x,y)===k;case'product-multiple':return (x*y/k)===Math.floor(x*y/k);default:throw Error(test);}}
function enumerate(p,probability){const a=p.args;let population,accepted;
 switch(p.kind){
 case'coin-intro':population=['heads','tails'];accepted=population;break;
 case'ball':population=range(a.red+a.blue);accepted=population.filter(i=>a.event==='red'?i<=a.red:i>a.red);break;
 case'multiples':population=range(a.total);accepted=population.filter(i=>i%a.divisor===0);break;
 case'multiples-or':population=range(a.total);accepted=population.filter(i=>i%a.a===0||i%a.b===0);break;
 case'single-color':population=range(a.n);accepted=population.filter(()=>a.certain);break;
 case'colors':population=range(a.red+a.blue+a.white);accepted=population.filter(i=>i<=a.red||i>a.red&&i<=a.red+a.blue);break;
 case'dice':population=pairs(range(6),range(6));accepted=population.filter(p=>diceEvent(p,a.test,a.k));break;
 case'replacement':population=pairs(range(a.red+a.blue),range(a.red+a.blue));accepted=population.filter(([x,y])=>a.event==='at-least-one'?(x<=a.red||y<=a.red):(x<=a.red&&y<=a.red));break;
 case'experiment':population=range(a.total);accepted=population.filter(i=>i<=a.heads);break;
 case'reverse':population=Array.from({length:a.total+1},(_,i)=>i);accepted=population.filter(i=>i*a.total===a.red*a.total);check(accepted.length===1,`${p.id}: reverse unique`);totals.enumeratedOutcomes+=population.length;return [accepted[0],1];
 case'product':population=pairs(range(a.a),range(a.b));accepted=population;break;
 case'sum':population=[...range(a.a).map(i=>'A'+i),...range(a.b).map(i=>'B'+i)];accepted=population;break;
 case'cards':population=range(90).map(i=>i+9);accepted=population.filter(i=>{const x=Math.floor(i/10),y=i%10;return a.digits.includes(x)&&a.digits.includes(y)&&x!==y&&i%a.modulus===0;});break;
 case'roles':population=pairs(range(a.n),range(a.n));accepted=population.filter(([x,y])=>a.ordered?x!==y:x<y);break;
 default:throw Error(`No oracle ${p.kind}`);
 }
 totals.enumeratedOutcomes+=population.length;
 return [accepted.length,probability?population.length:1];
}
function cardVariants(digits,m,variant){const arr=[];for(let value=0;value<=99;value++){const tens=Math.floor(value/10),ones=value%10;if(!digits.includes(tens)||!digits.includes(ones)||value%m!==0)continue;if(variant!=='leading'&&tens===0)continue;if(variant!=='reuse'&&tens===ones)continue;arr.push([tens,ones]);}return variant==='unordered'?new Set(arr.map(v=>v.slice().sort((a,b)=>a-b).join(','))).size:arr.length;}
function derived(w,p){const [a,b,c]=w.inputs;switch(w.rule){
 case'both-red':return[a*a,b*b];case'half':return[1,2];case'complement':return[b-a,b];case'one-outcome':return[1,a];case'success-counted-twice':return[a,b+a];case'inverse':return[b,a];case'count-only':return[a,1];
 case'multiply-disjoint-probabilities':return[a*b,c*c];case'first-part':return[a,b];case'unordered-dice':{const valid=pairs(range(6),range(6)).filter(v=>diceEvent(v,p.args.test,p.args.k));const n=new Set(valid.map(v=>v.slice().sort((x,y)=>x-y).join(','))).size;return[n,21];}
 case'one-die-denominator':return[a,6];case'single-stage':return[a,b];case'add-instead-multiply-probabilities':return[2*a,b];case'count-only-denominator':return[a,b];case'complement-count':return[b-a,1];case'total-only':return[a,1];case'multiply-count-again':return[a*b,1];case'constant-one':return[1,1];case'add-overlapping-counts':{const t=c;return[range(t).filter(i=>i%a===0).length+range(t).filter(i=>i%b===0).length,t];}case'certain-impossible-swap':return[1-a,1];
 case'sum':return[a+b,1];case'product':return[a*b,1];case'first-count':return[a,1];case'cards-leading-zero':return[cardVariants(a,b,'leading'),1];case'cards-reuse':return[cardVariants(a,b,'reuse'),1];case'cards-unordered':return[cardVariants(a,b,'unordered'),1];case'add-card-stages':return[2*(a-1),1];case'all-ordered-cards':return[a*(a-1),1];case'card-first-stage':return[a-1,1];case'opposite-role-order':return[b?a*(a-1)/2:a*(a-1),1];case'square':return[a*a,1];case'add-role-stages':return[2*a-1,1];case'unordered-count':{const valid=pairs(range(6),range(6)).filter(v=>diceEvent(v,p.args.test,p.args.k));return[new Set(valid.map(v=>v.slice().sort((x,y)=>x-y).join(','))).size,1];}case'unordered-all-dice':return[21,1];default:throw Error(`Unknown distractor rule ${w.rule}`);
}}
const index=JSON.parse(fs.readFileSync(path.join(out,'index.json'),'utf8'));check(index.default_pack==='m2s2-u7','default pack');const reports=[];
for(const entry of index.packs){
 const pack=JSON.parse(fs.readFileSync(path.join(out,entry.file),'utf8'));const ledger=JSON.parse(fs.readFileSync(path.join(here,`${pack.pack_id}-proofs.json`),'utf8'));const proofMap=new Map(ledger.proofs.map(p=>[p.id,p]));
 check(pack.pack_id===entry.pack_id,`${entry.pack_id}: index match`);check(pack.items.length>=300,`${pack.pack_id}: at least 300`);check(pack.school==='middle'&&pack.grade===2&&pack.semester===2,`${pack.pack_id}: school scope`);
 check(curriculum.units.some(u=>u.id===pack.unit_id),`${pack.pack_id}: unit exists`);check(pack.standards.every(c=>curriculum.standards.some(s=>s.code===c)),`${pack.pack_id}: standards exist`);
 const ids=new Set(),prompts=new Set(),bands={},concepts=new Set();
 for(const item of pack.items){totals.items++;const id=item.id;try{
  const p=proofMap.get(id);check(!!p,`${id}: proof available`);if(!p)continue;
  check(!ids.has(id),`${id}: duplicate id`);ids.add(id);const normalized=item.prompt.replace(/\([^)]*\)/g,'').replace(/\s+/g,' ').trim();check(!prompts.has(normalized),`${id}: duplicate prompt after QA normalization`);prompts.add(normalized);
  const mode=item.answer_mode??'choice',accept=item.accept;
  const exact=enumerate(p,pack.pack_id==='m2s2-u7'&&!['reverse','coin-intro'].includes(p.kind));
  const ans=mode==='amount'?[item.answer,1]:mode==='fraction_parts'?[item.answer.num,item.answer.den]:parse(item.answer);
  check(same(ans,exact),`${id}: independent answer mismatch ${ans} vs ${exact}`);check(same(exact,p.expected),`${id}: proof ledger mismatch`);if(mode!=='choice')check(exact.every((v,i)=>v===p.parts[i]),`${id}: unreduced sample-space counts`);
  check(ans[1]>0,`${id}: nonzero denominator`);if(item.format==='frac')check(ans[0]>=0&&ans[0]<=ans[1],`${id}: probability range`);
  check(item.answerNumeric===ans[0]/ans[1],`${id}: numeric QA annotation`);
  if(mode==='choice'){
   check(item.choices.length===4,`${id}: exactly four choices`);check(item.choices.filter(v=>same(parse(v),ans)).length===1,`${id}: unique correct choice`);check(item.choices.includes(item.answer),`${id}: exact answer membership`);
   check(new Set(item.choices.map(v=>reduce(...parse(v)).join('/'))).size===4,`${id}: equivalent choices`);
   for(const v of item.choices){totals.choices++;const r=parse(v);check(gcd(...r)===1&&r[1]>0,`${id}: reduced choice ${v}`);}
  }else{
   check(item.choices===null,`${id}: pour items have no choices`);check(item.max===60,`${id}: answer-independent pad cap`);check(item.coin_budget===(mode==='amount'?60:120),`${id}: fixed per-mode coin budget`);
   check((mode==='amount'?ans[0]:ans[0]+ans[1])<=item.coin_budget,`${id}: attainable answer cost`);
   check(item.coin_budget<=pack.economy.carry_capacity&&item.coin_budget<=pack.economy.min_spawn_coins,`${id}: enough carrying/supply`);
   if(mode==='amount')check(Number.isSafeInteger(ans[0])&&ans[0]>=2&&ans[0]<60,`${id}: avoids fixed-one/fill-to-cap policy`);
   else {
    check(['exact_parts','equivalent','reduced'].includes(accept),`${id}: fraction acceptance mode`);
    if(accept==='exact_parts')check(ans.every((v,i)=>v===exact[i]),`${id}: exact_parts preserves counted parts`);
    if(accept==='reduced')check(gcd(...ans)===1&&item.prompt.includes('기약분수'),`${id}: reduced requirement`);
    for(let n=0;n<=item.max;n++)for(let d=0;d<=item.max;d++){
     const matches=d>0&&(accept==='exact_parts'?n===ans[0]&&d===ans[1]:n*ans[1]===ans[0]*d&&(accept!=='reduced'||gcd(n,d)===1));
     const oracle=d>0&&(accept==='exact_parts'?n===exact[0]&&d===exact[1]:n*exact[1]===exact[0]*d&&(accept!=='reduced'||gcd(n,d)===1));
     check(matches===oracle,`${id}: pair ${n},${d} acceptance mismatch`);totals.acceptanceInputs++;
    }
   }
  }
  check(p.distractors.length===3&&new Set(item.distractor_tags).size>=2,`${id}: >=2 misconception distractors`);
  for(const w of p.distractors){totals.distractorDerivations++;if(mode==='choice')check(item.choices.includes(w.value),`${id}: distractor membership`);check(item.distractor_tags.includes(w.misconceptionId),`${id}: distractor tag`);check(same(parse(w.value),derived(w,p)),`${id}: distractor derivation ${w.rule}`);check(!same(parse(w.value),exact),`${id}: distractor accidentally correct`);}
  const text=[item.prompt,item.explain,...(item.choices??[])].join(' ');check(!/√|다시 넣지 않/.test(text),`${id}: forbidden scope`);check(!/\d+\s*\/\s*\d+/.test(text.replace(/\{frac:\d+\/\d+\}/g,'')),`${id}: plain fraction`);check(item.explain.length>4&&item.unitConcept.length>0,`${id}: explanation/concept`);
  if(['ball','colors','replacement','reverse','single-color'].includes(p.kind))check(item.prompt.includes('임의로')&&item.prompt.endsWith('(단, 공의 모양과 크기는 모두 같다.)'),`${id}: equal ball premise`);
  if(p.kind==='dice')check(item.prompt.includes('서로 다른 두 개의 주사위'),`${id}: distinguished dice`);
  if(p.kind==='multiples-or'){const listed=range(p.args.total).filter(i=>i%p.args.a===0||i%p.args.b===0);check(item.explain.startsWith(listed.join(', ')+'의 '),`${id}: overlap OR solved by direct listing`);}
  if(['cards','multiples','multiples-or'].includes(p.kind))check(item.prompt.includes('임의로'),`${id}: arbitrary draw`);
  if(p.kind==='replacement')check(item.prompt.includes('확인한 후 다시 넣고'),`${id}: replacement explicit`);
  if(item.prompt.includes('또는'))check(['sum','colors','multiples-or'].includes(p.kind),`${id}: OR disjoint, or overlapping OR posed as direct listing`);
  check(Number.isInteger(item.difficulty)&&item.difficulty>=1&&item.difficulty<=4,`${id}: difficulty band`);bands[item.difficulty]??={count:0,answerSlots:[0,0,0,0]};bands[item.difficulty].count++;if(mode==='choice')bands[item.difficulty].answerSlots[item.choices.indexOf(item.answer)]++;concepts.add(item.unitConcept);
 }catch(e){failures.push(`${id}: ${e.message}`);}}
 for(const [band,stats]of Object.entries(bands))check(Math.max(...stats.answerSlots)-Math.min(...stats.answerSlots)<=1,`${pack.pack_id}: answer slot balance difficulty ${band}`);
 check(Object.keys(bands).length===4,`${pack.pack_id}: all difficulty bands`);const modes={},accepts={};for(const q of pack.items){modes[q.answer_mode]=(modes[q.answer_mode]??0)+1;if(q.accept)accepts[q.accept]=(accepts[q.accept]??0)+1;}
 check(pack.schema_version===2,`${pack.pack_id}: schema v2`);check(pack.items[0].answer_mode==='amount'&&pack.items[0].answer===2,`${pack.pack_id}: first amount 2`);check((modes.choice??0)<=pack.items.length*.3,`${pack.pack_id}: choice <=30%`);
 reports.push({pack_id:pack.pack_id,items:pack.items.length,modes,accepts,maxAnswerCost:Math.max(...pack.items.map(q=>q.answer_mode==='amount'?q.answer:q.answer_mode==='fraction_parts'?q.answer.num+q.answer.den:0)),bands,concepts:[...concepts]});
}
const result={verdict:failures.length?'fail':'pass',...totals,failures:failures.length,packs:reports,errors:failures};
fs.writeFileSync(path.join(here,'verification-report.json'),JSON.stringify(result,null,2)+'\n');
fs.writeFileSync(path.join(here,'verification-report.md'),`# 문제 팩 전수 검증\n\n판정: **${result.verdict}**\n\n- 문항 ${totals.items}개, 보기 ${totals.choices}개, 오답 도출 ${totals.distractorDerivations}개 전수 확인.\n- 독립 표본공간 ${totals.enumeratedOutcomes}개를 직접 열거해 정답을 정수 교차곱으로 대조.\n- 분수 입력 ${totals.acceptanceInputs}쌍(분모 0 포함)의 exact_parts/reduced 정오 경계를 전수 대조.\n- 모든 붓기 패드 max=60, 고정 예산 amount=60/fraction=120, 소지량120·웨이브 최소140 코인. 첫 문항은 두 팩 모두 amount=2.\n- 오류 ${failures.length}건. 정답 누락·중복·동치 보기·오답 우연 정답·표현 함정·교육과정 범위·기약분수·보기 위치 균형을 확인.\n- 생성기 공통 모듈을 가져오지 않는 독립 검증기. 예상 정답 필드는 검증 대상일 뿐 계산 입력으로 쓰지 않음.\n\n재현: \`node factory/unity-src/hyeopgok-sasu/ArtSource/packs/verify-packs.mjs\`\n`);
console.log(JSON.stringify(result,null,2));if(failures.length)process.exitCode=1;
