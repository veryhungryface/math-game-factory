import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
export const here = path.dirname(fileURLToPath(import.meta.url));
export const output = path.resolve(here, '../../../../../public/g/hyeopgok-sasu/packs');
export function gcd(a,b) { while (b) [a,b]=[b,a%b]; return Math.abs(a)||1; }
export function rational(n,d=1) { if(!Number.isSafeInteger(n)||!Number.isSafeInteger(d)||d<=0) throw Error('integer rational required');const g=gcd(n,d);return [n/g,d/g]; }
export function token(n,d=1) { const [a,b]=rational(n,d);return b===1?String(a):`{frac:${a}/${b}}`; }
export function wrong(n,d,misconceptionId,rule,inputs) {return {value:token(n,d),misconceptionId,rule,inputs};}
export function probabilityWrong(f,t,extra=[]) {
  // The equal-likelihood 1/2 trap leads only on a fixed arithmetic third of items.
  // Elsewhere it is a late fallback, so "never step on 1/2" is not a free elimination rule
  // (round-1 review: 1/2 sat in 82% of choice sets while rarely being correct).
  const half=wrong(1,2,'m2s2-u7.equal-likelihood-bias','half',[]);
  const lead=(f*31+t)%3===0;
  return [
    ...extra,
    ...(lead?[half]:[]),
    wrong(t-f,t,'m2s2-u7.event-complement-confusion','complement',[f,t]),
    wrong(1,t,'m2s2-u7.event-count-omitted','one-outcome',[t]),
    wrong(f,t+f,'m2s2-u7.wrong-sample-space','success-counted-twice',[f,t]),
    wrong(t,f,'m2s2-u7.ratio-reversed','inverse',[f,t]),
    ...(lead?[]:[half]),
    wrong(f,1,'m2s2-u7.denominator-omitted','count-only',[f]),
  ];
}
export function diceTest(x,y,kind,k) {
  if(kind==='sum-eq')return x+y===k;if(kind==='sum-le')return x+y<=k;
  if(kind==='sum-ge')return x+y>=k;if(kind==='product-eq')return x*y===k;
  if(kind==='difference-eq')return Math.abs(x-y)===k;
  if(kind==='product-multiple')return x*y%k===0;throw Error(kind);
}
export function diceCount(kind,k) { let n=0,u=0;for(let x=1;x<=6;x++)for(let y=1;y<=6;y++)if(diceTest(x,y,kind,k)){n++;if(x<=y)u++;}return [n,u]; }
export function diceText(kind,k) {return ({'sum-eq':`두 눈의 수의 합이 ${k}일`,'sum-le':`두 눈의 수의 합이 ${k} 이하일`,'sum-ge':`두 눈의 수의 합이 ${k} 이상일`,'product-eq':`두 눈의 수의 곱이 ${k}일`,'difference-eq':`큰 눈의 수에서 작은 눈의 수를 뺀 값이 ${k}일`,'product-multiple':`두 눈의 수의 곱이 ${k}의 배수일`})[kind];}
export function spread(pool,n) {if(pool.length<n)throw Error(`Pool only ${pool.length}, need ${n}`);return Array.from({length:n},(_,i)=>pool[Math.floor(i*pool.length/n)]);}
export function makeItem({prompt,n,d=1,format='int',explain,unitConcept,difficulty,kind,args,distractors}) {
  const answer=token(n,d), seen=new Set([answer]);const wrongs=[];
  for(const w of distractors)if(!seen.has(w.value)){seen.add(w.value);wrongs.push(w);if(wrongs.length===3)break;}
  if(wrongs.length!==3)return null;
  return {prompt,answer,answerNumeric:n/d,format,explain,unitConcept,difficulty,_proof:{kind,args,expected:rational(n,d),parts:[n,d],distractors:wrongs}};
}
export function introItem() {
  return makeItem({prompt:'동전 한 개를 던질 때, 나올 수 있는 모든 경우의 수를 구하시오.',n:2,explain:'앞면과 뒷면의 2가지이므로 2닢을 붓습니다.',unitConcept:'한 사건의 경우의 수',difficulty:1,kind:'coin-intro',args:{},distractors:[wrong(1,1,'m2s2-u6.event-count-omitted','constant-one',[]),wrong(4,1,'m2s2-u6.extra-stage','product',[2,2]),wrong(3,1,'m2s2-u6.imaginary-outcome','sum',[2,1])]});
}
function coinInput(item,proof,packId) {
  const isAmount=packId==='m2s2-u6'||['coin-intro','reverse'].includes(proof.kind);
  if(isAmount) {
    item.answer_mode='amount';item.answer=proof.parts[0];item.max=60;item.coin_budget=60;item.choices=null;
    if(item.answer<2||item.answer>=60)throw Error(`Unbalanced amount answer ${item.answer}: ${item.prompt}`);
  } else if(proof.kind==='single-color') item.answer_mode='choice';
  else {
    item.answer_mode='fraction_parts';item.max=60;item.coin_budget=120;item.choices=null;
    item.accept=proof.kind==='ball'&&proof.args.event==='not-red'?'reduced':'exact_parts';
    const parts=item.accept==='reduced'?proof.expected:proof.parts;
    item.answer={num:parts[0],den:parts[1]};
    if(parts.some(v=>v>60))throw Error(`Answer exceeds coin pad: ${item.prompt}`);
    item.num_label=item.accept==='reduced'?'분자':proof.kind==='experiment'?'앞면이 나온 횟수':'사건이 일어나는 경우의 수';
    item.den_label=item.accept==='reduced'?'분모':proof.kind==='experiment'?'전체 시행 횟수':'모든 경우의 수';
    if(item.accept==='exact_parts')item.prompt=item.prompt.replace('확률을 기약분수로 구하시오.','확률을 구하시오. 모든 경우와 사건의 경우를 세어 약분하지 말고 나타내시오.').replace('상대도수를 기약분수로 구하시오.','상대도수를 구하시오. 약분하지 말고 시행 횟수와 앞면 횟수로 나타내시오.');
  }
  return item;
}
export function writePack(id,title,standards,groups) {
  // Deterministic interleaving avoids long same-template runs. No random rejection loops.
  const picked=groups.map(([pool,n])=>spread(pool.filter(Boolean),n));const ordered=[];
  for(let i=0;picked.some(g=>i<g.length);i++)for(const group of picked)if(group[i])ordered.push(group[i]);
  const counters=[0,0,0,0,0],proofs=[];const items=ordered.map((raw,i)=>{
    const {_proof,...item}=raw;item.id=`${id}-${String(i+1).padStart(3,'0')}`;
    const slot=_proof.kind==='single-color'?counters[item.difficulty]++%4:0;
    item.choices=_proof.distractors.map(w=>w.value);item.choices.splice(slot,0,item.answer);
    item.distractor_tags=_proof.distractors.map(w=>w.misconceptionId);
    proofs.push({id:item.id,..._proof});return coinInput(item,_proof,id);
  });
  const pack={schema_version:2,pack_id:id,title,school:'middle',grade:2,semester:2,unit_id:id,standards,economy:{carry_capacity:120,coin_per_kill:1,min_spawn_coins:140},items};
  fs.mkdirSync(output,{recursive:true});fs.writeFileSync(path.join(output,`${id}.json`),JSON.stringify(pack));
  fs.writeFileSync(path.join(here,`${id}-proofs.json`),JSON.stringify({pack_id:id,proofs},null,2)+'\n');
  fs.writeFileSync(path.join(output,'index.json'),JSON.stringify({default_pack:'m2s2-u7',packs:[{pack_id:'m2s2-u7',title:'확률',file:'m2s2-u7.json'},{pack_id:'m2s2-u6',title:'경우의 수',file:'m2s2-u6.json'}]},null,2)+'\n');
  console.log(`${id}: ${items.length} items / ${Buffer.byteLength(JSON.stringify(pack))} bytes`);
  return pack;
}
