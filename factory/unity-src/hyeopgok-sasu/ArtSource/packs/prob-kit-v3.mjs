// Schema v3 writer for the counting (m2s2-u6) and probability (m2s2-u7) packs.
// common.mjs is left untouched (its writePack rewrites index.json and emits v2 pour modes).
// Every item is a four-option choice; probability answers and options are reduced fractions.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
export const here=path.dirname(fileURLToPath(import.meta.url));
export const output=path.resolve(here,'../../../../../public/g/hyeopgok-sasu/packs');
export function gcd(a,b){a=Math.abs(a);b=Math.abs(b);while(b)[a,b]=[b,a%b];return a||1;}
export function red(n,d=1){if(!Number.isSafeInteger(n)||!Number.isSafeInteger(d)||d<=0||n<0)throw Error(`bad rational ${n}/${d}`);const g=gcd(n,d);return [n/g,d/g];}
// 받침 유무로 조사 선택: 한글은 종성, 숫자는 읽는 소리(0영1일3삼6육7칠8팔, 10십 등 받침).
export const josa=(w,a,b)=>{const ch=String(w).slice(-1),c=ch.charCodeAt(0);if(c>=0xac00&&c<=0xd7a3)return w+((c-0xac00)%28?a:b);return w+(/[013678]/.test(ch)?a:b);};
export function token(n,d=1){const [a,b]=red(n,d);return b===1?String(a):`{frac:${a}/${b}}`;}
// A distractor: value n/d produced by misconception `tag` through formula `rule` on `inputs`.
export const wrong=(n,d,tag,rule,inputs=[])=>({n,d,tag,rule,inputs});
export const drops={};
// All admissible wrong-triples: the first listed (signature) misconception is always kept, the other two
// come from the next five, and at least two distinct misconception tags appear.
function subsets(c){const r=[];for(let i=1;i<c.length;i++)for(let j=i+1;j<c.length;j++){const pk=[c[0],c[i],c[j]];if(new Set(pk.map(w=>w.tag)).size>=2)r.push({pk,pri:i+j});}return r;}
// Pack-level balance of the correct option's value rank (0 = smallest … 3 = largest among the four):
// walking the selected items in order, each takes the admissible triple whose rank is least used so far
// (ties → listing priority). Without this, "never tap the largest/smallest pad" beats chance.
function balanceRanks(items){const use=[0,0,0,0];for(const it of items){const A=it.n/it.d;let best=null;
  for(const s of subsets(it.cands)){const rank=s.pk.filter(w=>w.n/w.d<A).length,score=use[rank]*100+s.pri;if(!best||score<best.score)best={...s,rank,score};}
  use[best.rank]++;it.wrongs=best.pk;}return use;}const drop=(k,why)=>{drops[`${k}:${why}`]=(drops[`${k}:${why}`]??0)+1;return null;};

// Probability distractors from real misconceptions, most diagnostic first. f = event count, t = all.
export function probWrongs(f,t,extra=[]){const P='m2s2-u7.';return [
  ...extra,
  ...(f<t-f?[wrong(f,t-f,P+'part-part-ratio','part-to-part',[f,t])]:[]),
  wrong(t-f,t,P+'event-complement-confusion','complement',[f,t]),
  wrong(1,t,P+'event-count-omitted','one-outcome',[t]),
  wrong(f,t+f,P+'wrong-sample-space','success-counted-twice',[f,t]),
  ...(f>=t-f&&t-f>0?[wrong(f,t-f,P+'part-part-ratio','part-to-part',[f,t])]:[]),
  wrong(1,2,P+'equal-likelihood-bias','half',[]),
  wrong(t,f||1,P+'ratio-reversed','inverse',[f,t]),
  wrong(f,1,P+'denominator-omitted','count-only',[f]),
];}

function promptInts(prompt){const r=prompt.replace(/\{frac:(\d+)\/(\d+)\}/g,' ');return new Set([...r.matchAll(/\d+/g)].map(m=>Number(m[0])));}
// Build one item. n/d is the exact answer (reduced on output). maxWrong bounds plausible wrongs
// (e.g. a probability distractor above 1 is only used as a last resort via probability:true ordering).
export function makeQ({prompt,n,d=1,explain,unitConcept,difficulty,kind,args,distractors,probability=false}){
  const [an,ad]=red(n,d);const key=`${an}/${ad}`;
  if(ad===1&&!probability&&promptInts(prompt).has(an))return drop(kind,'answer-in-prompt');
  if(ad>1&&prompt.includes(`{frac:${an}/${ad}}`))return drop(kind,'answer-in-prompt');
  const seen=new Set([key]),ws=[];
  // a probability option above 1 is eliminable without the concept: only as a last resort
  if(probability)distractors=[...distractors.filter(w=>w.n<=w.d),...distractors.filter(w=>w.n>w.d)];
  for(const w of distractors){if(!(Number.isSafeInteger(w.n)&&Number.isSafeInteger(w.d)&&w.n>=0&&w.d>0))continue;const [x,y]=red(w.n,w.d);const k=`${x}/${y}`;if(seen.has(k))continue;
    if(!probability&&y!==1)continue; // counting answers: integer options only
    if(!probability&&x<1)continue;
    seen.add(k);ws.push(w);}
  if(ws.length<3)return drop(kind,'fewer-than-3-wrongs');
  // probability options above 1 only when the ≤1 misconceptions cannot fill the item
  const le1=ws.filter(w=>w.n<=w.d),cands=(probability&&le1.length>=3&&le1[0]===ws[0]&&new Set(le1.map(w=>w.tag)).size>=2?le1:ws).slice(0,6);
  if(!subsets(cands).length)return drop(kind,'one-misconception-only');
  const ex=explain.replace('@P',gcd(n,d)>1&&d>1?`{frac:${n}/${d}}={frac:${an}/${ad}}`:token(an,ad));
  if(/@P|붓|닢|약분하지/.test(prompt+ex))throw Error(`bad wording: ${prompt} / ${ex}`);
  return {prompt,n:an,d:ad,explain:ex,unitConcept,difficulty,kind,args,probability,cands};
}
export function spread(pool,n){pool=pool.filter(Boolean);if(pool.length<n)throw Error(`Pool only ${pool.length}, need ${n} (${pool[0]?.kind})`);return Array.from({length:n},(_,i)=>pool[Math.floor(i*pool.length/n)]);}
const PERMS=(()=>{const r=[];const go=(a,rest)=>{if(!rest.length)return r.push(a);rest.forEach((x,i)=>go([...a,x],rest.filter((_,j)=>j!==i)));};go([],[0,1,2,3]);return r;})();
function slotter(){const c={};return d=>{const i=c[d]=(c[d]??-1)+1;return PERMS[(Math.floor(i/4)*7+d*5)%24][i%4];};}
const norm=p=>p.replace(/\([^)]*\)/g,'').replace(/\s+/g,' ').trim();
export function writeProbPack(id,title,standards,intro,groups){
  if(!intro||intro.difficulty!==1)throw Error('items[0] must be difficulty 1');
  const seen=new Set([norm(intro.prompt)]);
  const picked=groups.map(([pool,n])=>{const u=[];for(const it of pool){if(!it)continue;const k=norm(it.prompt);if(seen.has(k))continue;seen.add(k);u.push(it);}return spread(u,n);});
  const ordered=[intro];for(let i=0;picked.some(g=>i<g.length);i++)for(const g of picked)if(g[i])ordered.push(g[i]);
  const rankUse=balanceRanks(ordered);const slotOf=slotter(),items=[],proofs=[];
  ordered.forEach((it,i)=>{const qid=`${id}-${String(i+1).padStart(3,'0')}`;const slot=slotOf(it.difficulty);
    const correct=token(it.n,it.d);const choices=it.wrongs.map(w=>token(w.n,w.d));choices.splice(slot,0,correct);
    items.push({id:qid,prompt:it.prompt,answer_mode:'choice',answer:correct,choices,format:it.d===1&&!it.probability?'int':'frac',answerNumeric:it.n/it.d,
      explain:it.explain,unitConcept:it.unitConcept,difficulty:it.difficulty,distractor_tags:[...new Set(it.wrongs.map(w=>w.tag))]});
    proofs.push({id:qid,kind:it.kind,args:it.args,expected:[it.n,it.d],correct_index:slot,distractors:it.wrongs.map(w=>({value:token(w.n,w.d),misconceptionId:w.tag,rule:w.rule,inputs:w.inputs}))});
  });
  const pack={schema_version:3,pack_id:id,title,school:'middle',grade:2,semester:2,unit_id:id,standards,economy:{carry_capacity:120,coin_per_kill:1,min_spawn_coins:140},items};
  fs.mkdirSync(output,{recursive:true});fs.writeFileSync(path.join(output,`${id}.json`),JSON.stringify(pack));
  fs.writeFileSync(path.join(here,`${id}-proofs.json`),JSON.stringify({pack_id:id,proofs},null,1)+'\n');
  const b={};for(const q of items)b[q.difficulty]=(b[q.difficulty]??0)+1;
  console.log(`${id}: ${items.length} items`,JSON.stringify(b),'ranks',JSON.stringify(rankUse),`${Buffer.byteLength(JSON.stringify(pack))} bytes`);
  if(process.env.POOLS)console.log('drops',JSON.stringify(drops));
  return pack;
}
