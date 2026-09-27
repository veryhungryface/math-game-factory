#!/usr/bin/env node
// Independent oracle for m2s2-u6 (경우의 수) and m2s2-u7 (확률), schema v3.
// Imports NO generator/kit module. Each prompt is parsed back into its data; the answer is
// recomputed by explicitly enumerating the sample space (labelled balls, cards, ordered dice/coin
// tuples, roads, candidates) and counting; every distractor is re-derived from the PARSED data
// through the named misconception rule (the ledger's own inputs are ignored). Then all four options
// are compared: exactly one equals the truth (the one keyed as answer), the rest differ from it and
// from each other (fractions compared after reduction), and each wrong is the recorded misconception.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {validatePackV3,optionValue,valueKey} from './validate-pack-v3-m2s2.mjs';
const here=path.dirname(fileURLToPath(import.meta.url));const root=path.resolve(here,'../../../../..');
const out=path.join(root,'public/g/hyeopgok-sasu/packs');
const curriculum=JSON.parse(fs.readFileSync(path.join(root,'curriculum/2022-middle-math.json'),'utf8'));
const failures=[];const stats={items:0,enumeratedOutcomes:0,options:0,distractorsRederived:0};
const gcd=(a,b)=>{a=Math.abs(a);b=Math.abs(b);while(b)[a,b]=[b,a%b];return a||1;};
const M=(re,s)=>{const m=re.exec(s);if(!m)throw Error(`prompt does not match ${re}`);return m;};
const N=Number;const range=(a,b)=>Array.from({length:b-a+1},(_,i)=>a+i);
const EQ=' (단, 공의 모양과 크기는 모두 같다.)';const EQR=' \\(단, 공의 모양과 크기는 모두 같다\\.\\)';
function count(pop,pred){stats.enumeratedOutcomes+=pop.length;return pop.filter(pred).length;}
const tuples=(k,faces)=>k===0?[[]]:tuples(k-1,faces).flatMap(t=>faces.map(f=>[...t,f]));
// ── one-die events, read from the words ──
function dieEvent(txt){let m;
  if(txt==='짝수의')return v=>v%2===0;if(txt==='홀수의')return v=>v%2===1;if(txt==='소수의')return v=>range(2,v-1).every(d=>v%d)&&v>1;
  if((m=/^(\d+)의 배수의$/.exec(txt)))return v=>v%N(m[1])===0;if((m=/^(\d+)의 약수의$/.exec(txt)))return v=>N(m[1])%v===0;
  if((m=/^(\d+) 이상의$/.exec(txt)))return v=>v>=N(m[1]);if((m=/^(\d+) 이하의$/.exec(txt)))return v=>v<=N(m[1]);
  if((m=/^(\d+) 미만의$/.exec(txt)))return v=>v<N(m[1]);if((m=/^(\d+) 초과의$/.exec(txt)))return v=>v>N(m[1]);
  if((m=/^(\d+)보다 큰$/.exec(txt)))return v=>v>N(m[1]);throw Error(`die event ${txt}`);}
// ── two distinguished dice ──
function diceEvent(txt){let m;
  if((m=/^두 눈의 수의 합이 (\d+)(?:인|일)$/.exec(txt)))return {kind:'sum-eq',k:N(m[1]),f:([x,y])=>x+y===N(m[1])};
  if((m=/^두 눈의 수의 합이 (\d+) 이하(?:인|일)$/.exec(txt)))return {kind:'sum-le',k:N(m[1]),f:([x,y])=>x+y<=N(m[1])};
  if((m=/^두 눈의 수의 합이 (\d+) 이상(?:인|일)$/.exec(txt)))return {kind:'sum-ge',k:N(m[1]),f:([x,y])=>x+y>=N(m[1])};
  if((m=/^두 눈의 수의 곱이 (\d+)(?:인|일)$/.exec(txt)))return {kind:'product-eq',k:N(m[1]),f:([x,y])=>x*y===N(m[1])};
  if((m=/^두 눈의 수의 곱이 (\d+)의 배수(?:인|일)$/.exec(txt)))return {kind:'product-multiple',k:N(m[1]),f:([x,y])=>(x*y)%N(m[1])===0};
  if((m=/^두 눈의 수의 차가 (\d+)(?:인|일)$/.exec(txt)))return {kind:'difference-eq',k:N(m[1]),f:([x,y])=>Math.max(x,y)-Math.min(x,y)===N(m[1])};
  if(txt==='두 눈의 수가 같을')return {kind:'same',k:0,f:([x,y])=>x===y};throw Error(`dice event ${txt}`);}
const DICE=tuples(2,range(1,6));
function diceStats(ev){const n=count(DICE,ev.f),u=count(DICE,([x,y])=>x<=y&&ev.f([x,y])),dd=count(DICE,([x,y])=>x===y&&ev.f([x,y]));return {n,u,dd};}
// ── coins ──
function coinEvent(txt){let m;const H=t=>t.filter(c=>c==='H').length;
  if(txt==='모두 앞면이')return t=>H(t)===t.length;if(txt==='모두 뒷면이')return t=>H(t)===0;if(txt==='앞면과 뒷면이 하나씩')return t=>t.length===2&&H(t)===1;
  if(txt==='적어도 한 개는 앞면이')return t=>H(t)>=1;if(txt==='적어도 한 개는 뒷면이')return t=>H(t)<t.length;
  if(txt==='서로 같은 면이'||txt==='모두 같은 면이')return t=>H(t)===0||H(t)===t.length;
  if((m=/^앞면이 (\d+)개만$/.exec(txt)))return t=>H(t)===N(m[1]);if((m=/^앞면이 (\d+)개 이상$/.exec(txt)))return t=>H(t)>=N(m[1]);throw Error(`coin event ${txt}`);}
const hangulFinal=w=>{const c=String(w).slice(-1);const k=c.charCodeAt(0);if(k>=0xac00&&k<=0xd7a3)return (k-0xac00)%28!==0;return /[013678]/.test(c);};
const josaOK=(word,j,pair)=>(j===pair[0])===hangulFinal(word);
const unitsEnd=s=>s.match(/(벌|종류|권|개)$/)[1];

// Each solver returns {v:[n,d]} (count answers d=1) plus a `rules` map rule → admissible values.
const U6={
 'die-one-count'(q){const m=M(/^주사위 한 개를 던질 때, (.+?) 눈이 나오는 경우의 수를 구하시오\.$/,q.prompt);const f=count(range(1,6),dieEvent(m[1]));
  return {v:[f,1],rules:{'complement-count':[6-f],'all-outcomes':[6],'first-multiple-only':[1]}};},
 product(q){const m=M(/^서로 다른 (\S+) (\d+)(벌|종류|권|개)(과|와) (\S+) (\d+)(벌|종류|권|개) 중에서 (\S+)(과|와) (\S+)(을|를) 각각 하나씩 골라 짝 짓는 경우의 수를 구하시오\.$/,q.prompt);
  if(!josaOK(m[3],m[4],['과','와'])||!josaOK(m[8],m[9],['과','와'])||!josaOK(m[10],m[11],['을','를']))throw Error('josa');if(m[8]!==m[1]||m[10]!==m[5])throw Error('items');
  const A=range(1,N(m[2])).map(i=>'x'+i),B=range(1,N(m[6])).map(i=>'y'+i);const pairs=A.flatMap(x=>B.map(y=>[x,y]));const a=A.length,b=B.length;
  return {v:[count(pairs,()=>true),1],rules:{sum:[a+b],'product-plus-sum':[a*b+a+b],'pairs-counted-twice':[2*a*b],'first-count':[a,b]}};},
 sum(q){const m=M(/^서로 다른 (\S+) (\d+)(벌|종류|권|개)(과|와) (\S+) (\d+)(벌|종류|권|개) 중에서 (\S+) 또는 (\S+)(을|를) 하나만 고르는 경우의 수를 구하시오\.$/,q.prompt);
  if(!josaOK(m[3],m[4],['과','와'])||!josaOK(m[9],m[10],['을','를']))throw Error('josa');const a=N(m[2]),b=N(m[6]);
  const pool=[...range(1,a).map(i=>m[1]+i),...range(1,b).map(i=>m[5]+i)];return {v:[count(pool,()=>true),1],rules:{product:[a*b],'product-plus-sum':[a*b+a+b],'first-count':[a,b]}};},
 'cards-or'(q){return U6['cards-or-overlap'](q,true);},
 'cards-or-overlap'(q,disjoint){const m=M(/^1부터 (\d+)까지의 자연수가 각각 하나씩 적힌 카드 (\d+)장 중에서 한 장을 뽑을 때, (\d+)의 배수 또는 (\d+)의 배수가 나오는 경우의 수를 구하시오\.$/,q.prompt);
  const n=N(m[1]),a=N(m[3]),b=N(m[4]);if(N(m[2])!==n)throw Error('card count');const cards=range(1,n);
  const f=count(cards,x=>x%a===0||x%b===0),ca=count(cards,x=>x%a===0),cb=count(cards,x=>x%b===0),both=count(cards,x=>x%a===0&&x%b===0);
  if(disjoint===true&&both)throw Error('합의 법칙 문항인데 두 사건이 겹친다');if(disjoint!==true&&!both)throw Error('overlap item without overlap');
  if(both&&!q.explain.startsWith(cards.filter(x=>x%a===0||x%b===0).join(', ')+'의 '))throw Error('overlapping OR must be solved by direct listing');
  return {v:[f,1],rules:{'add-overlapping-counts':[ca+cb],'product-of-counts':[ca*cb],'complement-count':[n-f],'first-event-only':[ca,cb]}};},
 dice(q){const m=M(/^서로 다른 두 개의 주사위를 동시에 던질 때, (.+?) 경우의 수를 구하시오\.$/,q.prompt);const ev=diceEvent(m[1]);const s=diceStats(ev);
  const alt=f=>count(DICE,f);const k=ev.k;
  return {v:[s.n,1],rules:{'unordered-count':[s.u],'strict-inequality':[ev.kind==='sum-le'?alt(([x,y])=>x+y<k):alt(([x,y])=>x+y>k)],'equality-only':[alt(([x,y])=>x+y===k)],
   'doubles-counted-twice':[2*s.u],'sum-for-product':[alt(([x,y])=>x+y===k)],'complement-count':[36-s.n],'distinct-unordered-only':[s.u-s.dd],'dice-sum-of-faces':[12],'all-outcomes':[36]}};},
 cards(q){const m=M(/^([0-9, ]+)(이|가) 각각 하나씩 적힌 카드 (\d+)장 중에서 2장을 뽑아 만들 수 있는 (두 자리 자연수|두 자리 짝수|두 자리 자연수 중 3의 배수)의 개수를 구하시오\.$/,q.prompt);
  const digits=m[1].split(', ').map(N);if(!josaOK(m[1],m[2],['이','가']))throw Error('josa');if(digits.length!==N(m[3])||new Set(digits).size!==digits.length)throw Error('cards');
  const md=m[4]==='두 자리 자연수'?1:m[4]==='두 자리 짝수'?2:3;const cards=digits.map((d,i)=>({d,i}));
  const ordered=cards.flatMap(x=>cards.filter(y=>y.i!==x.i).map(y=>[x,y]));const val=([x,y])=>10*x.d+y.d;
  const ok=p=>p[0].d!==0&&val(p)%md===0;const f=count(ordered,ok);
  const lead=count(ordered,p=>val(p)%md===0);const all=cards.flatMap(x=>cards.map(y=>[x,y]));const reuse=count(all,ok);const free=count(all,p=>val(p)%md===0);
  const sets=new Set(ordered.filter(ok).map(([x,y])=>[x.i,y.i].sort().join(',')));const L=digits.length;
  return {v:[f,1],rules:{'cards-leading-zero':[lead],'cards-unordered':[sets.size],'cards-reuse':[reuse],'cards-no-restriction':[free],'add-card-stages':[2*(L-1)],'card-first-stage':[L-1],'ones-digit-only':md===2?[count(digits,d=>d%2===0)]:[]}};},
 paths(q){const m=M(/^A 마을에서 B 마을로 가는 길은 (\d+)가지, B 마을에서 C 마을로 가는 길은 (\d+)가지이고, A 마을에서 C 마을로 바로 가는 길은 (\d+)가지이다\. A 마을에서 C 마을로 가는 경우의 수를 구하시오\. \(단, 한 번 지난 마을은 다시 지나지 않는다\.\)$/,q.prompt);
  const [a,b,c]=[1,2,3].map(i=>N(m[i]));const routes=[...range(1,a).flatMap(i=>range(1,b).map(j=>`AB${i}-BC${j}`)),...range(1,c).map(k=>`AC${k}`)];
  return {v:[count(routes,()=>true),1],rules:{'sum-all-roads':[a+b+c],'direct-road-omitted':[a*b],'product-all-roads':[a*b*c],'direct-road-as-second-leg':[a*(b+c)],'sum-direct-omitted':[a+b]}};},
 roles(q){let m=/^후보 (\d+)명 중에서 회장 1명과 부회장 1명을 뽑는 경우의 수를 구하시오\. \(단, 한 사람이 두 역할을 맡을 수 없다\.\)$/.exec(q.prompt),ordered=true;
  if(!m){m=M(/^후보 (\d+)명 중에서 대표 2명을 뽑는 경우의 수를 구하시오\.$/,q.prompt);ordered=false;}const n=N(m[1]);const P=range(1,n);
  const op=P.flatMap(x=>P.filter(y=>y!==x).map(y=>[x,y]));const un=op.filter(([x,y])=>x<y);const v=ordered?count(op,()=>true):count(un,()=>true);
  return {v:[v,1],rules:{'opposite-role-order':[ordered?un.length:op.length],square:[n*n],'add-role-stages':[2*n-1],'one-stage':[n]}};},
};
// probability: f event outcomes of t equally likely ones → common misconception values
function probRules(f,t){return {'part-to-part':t-f>0?[[f,t-f]]:[],complement:[[t-f,t]],'one-outcome':[[1,t]],'success-counted-twice':[[f,t+f]],half:[[1,2]],inverse:f?[[t,f]]:[],'count-only':[[f,1]]};}
const BALLS=(r,b,w=0)=>[...range(1,r).map(i=>'R'+i),...range(1,b).map(i=>'B'+i),...range(1,w).map(i=>'W'+i)];
const U7={
 'die-one'(q){const m=M(/^주사위 한 개를 던질 때, (.+?) 눈이 나올 확률을 구하시오\.$/,q.prompt);const f=count(range(1,6),dieEvent(m[1]));return {v:[f,6],f,t:6,rules:probRules(f,6)};},
 ball(q){const m=M(new RegExp(`^빨간 공 (\\d+)개와 파란 공 (\\d+)개가 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, 빨간 공이 (나올|나오지 않을) 확률을 구하시오\\.${EQR}$`),q.prompt);
  const pop=BALLS(N(m[1]),N(m[2]));const f=count(pop,x=>m[3]==='나올'?x[0]==='R':x[0]!=='R');return {v:[f,pop.length],f,t:pop.length,rules:probRules(f,pop.length)};},
 multiples(q){const m=M(/^1부터 (\d+)까지의 자연수가 각각 하나씩 적힌 카드 (\d+)장 중에서 한 장을 임의로 뽑을 때, (\d+)의 배수가 나올 확률을 구하시오\.$/,q.prompt);
  const n=N(m[1]),k=N(m[3]);if(N(m[2])!==n)throw Error('cards');const f=count(range(1,n),x=>x%k===0);return {v:[f,n],f,t:n,rules:{...probRules(f,n),'one-over-divisor':[[1,k]]}};},
 'single-color'(q){const m=M(new RegExp(`^(\\S+) 공만 (\\d+)개 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, (\\S+) 공이 나올 확률을 구하시오\\.${EQR}$`),q.prompt);
  const n=N(m[2]);const pop=Array(n).fill(m[1]);const f=count(pop,c=>c===m[3]);const p=[f,n];return {v:p,f,t:n,rules:{'certain-impossible-swap':[[n-f,n]],'one-outcome':[[1,n]],half:[[1,2]],'count-only':[[n,1]]}};},
 colors(q){const m=M(new RegExp(`^빨간 공 (\\d+)개, 파란 공 (\\d+)개, 흰 공 (\\d+)개가 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, 빨간 공 또는 파란 공이 나올 확률을 구하시오\\.${EQR}$`),q.prompt);
  const [r,b,w]=[1,2,3].map(i=>N(m[i]));const pop=BALLS(r,b,w);const t=pop.length;const f=count(pop,x=>x[0]==='R'||x[0]==='B');
  return {v:[f,t],f,t,rules:{...probRules(f,t),'multiply-disjoint-probabilities':[[r*b,t*t]],'first-part':[[r,t]]}};},
 experiment(q){const m=M(/^동전 한 개를 (\d+)번 던졌더니 앞면이 (\d+)번 나왔다\. 이때 앞면이 나온 상대도수를 구하시오\.$/,q.prompt);const n=N(m[1]),h=N(m[2]);if(h>n)throw Error('heads');
  const trials=range(1,n);const f=count(trials,i=>i<=h);return {v:[f,n],f,t:n,rules:probRules(f,n)};},
 coins(q){const m=M(/^서로 다른 (두|세) 개의 동전을 동시에 던질 때, (.+?) 나올 확률을 구하시오\.$/,q.prompt);const k=m[1]==='두'?2:3;const ev=coinEvent(m[2]);
  const all=tuples(k,['H','T']);const f=count(all,ev);const multis=range(0,k).map(h=>[...Array(h).fill('H'),...Array(k-h).fill('T')]);const uf=count(multis,ev);
  return {v:[f,all.length],f,t:all.length,rules:{'unordered-coins':[[uf,k+1]],complement:[[all.length-f,all.length]],half:[[1,2]],'one-outcome':[[1,all.length]],'count-only':[[f,1]],'add-sample-spaces':[[f,2*k]]}};},
 dice(q){const m=M(/^서로 다른 두 개의 주사위를 동시에 던질 때, (.+?) 확률을 구하시오\.$/,q.prompt);const ev=diceEvent(m[1]);const s=diceStats(ev);
  return {v:[s.n,36],f:s.n,t:36,rules:{...probRules(s.n,36),'unordered-dice':[[s.u,21]],'one-die-denominator':[[s.n,6]],'sum-of-faces-denominator':[[s.n,12]]}};},
 'multiples-or'(q){const m=M(/^1부터 (\d+)까지의 자연수가 각각 하나씩 적힌 카드 (\d+)장 중에서 한 장을 임의로 뽑을 때, (\d+)의 배수 또는 (\d+)의 배수가 나올 확률을 구하시오\.$/,q.prompt);
  const n=N(m[1]),a=N(m[3]),b=N(m[4]);const cards=range(1,n);const hit=cards.filter(x=>x%a===0||x%b===0);const f=count(cards,x=>x%a===0||x%b===0);
  if(!q.explain.startsWith(hit.join(', ')+'의 '))throw Error('overlapping OR must be solved by direct listing');
  return {v:[f,n],f,t:n,rules:{...probRules(f,n),'add-overlapping-counts':[[cards.filter(x=>x%a===0).length+cards.filter(x=>x%b===0).length,n]]}};},
 reverse(q){const m=M(new RegExp(`^빨간 공과 파란 공이 합하여 (\\d+)개 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, 빨간 공이 나올 확률이 \\{frac:(\\d+)/(\\d+)\\}이다\\. 빨간 공은 몇 개인지 구하시오\\.${EQR}$`),q.prompt);
  const t=N(m[1]),a=N(m[2]),d=N(m[3]);const hits=range(0,t).filter(r=>{const pop=BALLS(r,t-r);return count(pop,x=>x[0]==='R')*d===a*pop.length;});if(hits.length!==1)throw Error('reverse not unique');const r=hits[0];
  return {v:[r,1],rules:{'numerator-as-count':[[a,1]],'complement-count':[[t-r,1]],'unit-fraction-share':[[t,d]],'denominator-as-count':[[d,1]],'total-only':[[t,1]]}};},
 replacement(q){const m=M(new RegExp(`^빨간 공 (\\d+)개와 파란 공 (\\d+)개가 들어 있는 주머니에서 공 한 개를 임의로 꺼내 확인한 후 다시 넣고 또 한 개를 꺼낼 때, (두 번 모두 빨간 공이|적어도 한 번은 빨간 공이) 나올 확률을 구하시오\\.${EQR}$`),q.prompt);
  const r=N(m[1]),b=N(m[2]),t=r+b;const pop=BALLS(r,b);const pairs=pop.flatMap(x=>pop.map(y=>[x,y]));const both=m[3].startsWith('두 번');
  const f=count(pairs,([x,y])=>both?x[0]==='R'&&y[0]==='R':x[0]==='R'||y[0]==='R');const T=pairs.length;
  return {v:[f,T],f,t:T,rules:{...probRules(f,T),'single-stage':[[r,t]],'add-instead-multiply-probabilities':[[2*r,t]],'count-only-denominator':[[r*r,t]],'add-denominators':[[r*r,2*t]],'complement-not-subtracted':[[b*b,T]],'both-red':[[r*r,T]]}};},
 'coin-die'(q){const m=M(/^동전 한 개와 주사위 한 개를 동시에 던질 때, 동전은 (앞면|뒷면)이 나오고 주사위는 (.+?) 눈이 나올 확률을 구하시오\.$/,q.prompt);const ev=dieEvent(m[2]);
  const all=['H','T'].flatMap(c=>range(1,6).map(v=>[c,v]));const face=m[1]==='앞면'?'H':'T';const f=count(all,([c,v])=>c===face&&ev(v));const e=count(range(1,6),ev);
  return {v:[f,12],f,t:12,rules:{'add-instead-multiply-probabilities':[[3+e,6]],'coin-ignored':[[e,6]],'add-sample-spaces':[[e,8]],'one-outcome':[[1,12]],complement:[[12-f,12]]}};},
 'cards-replacement'(q){const m=M(/^1부터 (\d+)까지의 자연수가 각각 하나씩 적힌 카드 (\d+)장 중에서 한 장을 임의로 뽑아 확인하고 다시 넣은 후 또 한 장을 뽑을 때, 두 번 모두 (홀수|짝수|3의 배수|4의 배수|소수)가 적힌 카드가 나올 확률을 구하시오\.$/,q.prompt);
  const n=N(m[1]);if(N(m[2])!==n)throw Error('cards');const ok=m[3]==='홀수'?x=>x%2===1:m[3]==='짝수'?x=>x%2===0:m[3]==='소수'?x=>x>1&&range(2,x-1).every(d=>x%d):x=>x%N(m[3][0])===0;
  const cards=range(1,n);const e=count(cards,ok);const pairs=cards.flatMap(x=>cards.map(y=>[x,y]));const f=count(pairs,([x,y])=>ok(x)&&ok(y));const T=pairs.length;
  return {v:[f,T],f,t:T,rules:{...probRules(f,T),'single-stage':[[e,n]],'add-instead-multiply-probabilities':[[2*e,n]],'count-only-denominator':[[e*e,n]],'add-denominators':[[e*e,2*n]]}};},
};
const UNITS={'m2s2-u6':{S:U6,prob:false},'m2s2-u7':{S:U7,prob:true}};
const FORBIDDEN=/√|근호|다시 넣지 않|약분하지 말고|약분하지 않아도|붓|닢/;
const same=(a,b)=>a[0]*b[1]===b[0]*a[1];
function checkItem(packId,U,q,p){
 if(!p)throw Error('no proof entry');if(q.answer_mode!=='choice')throw Error('v3: choice only');
 if(!Array.isArray(q.choices)||q.choices.length!==4||new Set(q.choices).size!==4)throw Error('four distinct options');
 if(q.choices.filter(c=>c===q.answer).length!==1)throw Error('answer must be exactly one option');
 const text=[q.prompt,q.explain,...q.choices].join(' ');if(FORBIDDEN.test(text))throw Error('forbidden wording');
 if(/\d+\s*\/\s*\d+/.test(text.replace(/\{frac:\d+\/\d+\}/g,'')))throw Error('plain fraction');
 const fn=U.S[p.kind];if(!fn)throw Error(`no oracle for ${p.kind}`);const o=fn(q);const truth=o.v;
 const vals=q.choices.map(optionValue);if(!vals.every(v=>v.kind==='num'&&v.unit===''&&(v.d===1||v.reduced)))throw Error('options must be integers or reduced {frac} tokens');
 const isProb=U.prob&&p.kind!=='reverse';if(!isProb&&!vals.every(v=>v.d===1))throw Error('counting item with fraction option');
 if(new Set(vals.map(valueKey)).size!==4)throw Error('options equal in value');stats.options+=4;
 const hit=vals.map(v=>same([v.n,v.d],truth));if(hit.filter(Boolean).length!==1)throw Error(`truth ${truth.join('/')} matches ${hit.filter(Boolean).length} options (${q.choices})`);
 const idx=hit.indexOf(true);if(q.choices[idx]!==q.answer)throw Error(`answer ${q.answer} but truth is ${q.choices[idx]}`);
 const a=vals[idx];if(q.answerNumeric!==a.n/a.d)throw Error('answerNumeric');
 if(isProb&&q.format!=='frac')throw Error('probability format frac');if(!isProb&&q.format!=='int')throw Error('count format int');
 if(isProb&&!(a.n<=a.d))throw Error('probability above 1');
 // answer-by-copying
 const printed=new Set([...q.prompt.replace(/\{frac:\d+\/\d+\}/g,' ').matchAll(/\d+/g)].map(m=>N(m[0])));
 if(!isProb&&printed.has(a.n))throw Error(`answer ${a.n} printed in the prompt`);if(a.d>1&&q.prompt.includes(q.answer))throw Error('answer fraction printed in prompt');
 // explanation states the answer
 if(!(a.d===1?new RegExp(`(^|[^0-9/])${a.n}([^0-9/]|$)`).test(q.explain.replace(/\{frac:\d+\/\d+\}/g,' ')):q.explain.includes(q.answer))&&!(a.d===1&&isProb&&q.explain.includes(`확률은 ${a.n}`)))throw Error('explain does not state the answer');
 // distractors: each wrong option is the recorded misconception, re-derived from the parsed data
 if(p.distractors?.length!==3)throw Error('ledger needs 3 distractors');
 const wrongIdx=[0,1,2,3].filter(i=>i!==idx);
 for(const w of p.distractors){stats.distractorsRederived++;
  if(!q.choices.includes(w.value))throw Error(`ledger distractor ${w.value} not an option`);
  if(!w.misconceptionId.startsWith('m2s2-u6.')&&!w.misconceptionId.startsWith('m2s2-u7.'))throw Error('tag namespace');
  if(!q.distractor_tags.includes(w.misconceptionId))throw Error('distractor tag missing on item');
  const adm=(o.rules[w.rule]??[]).map(x=>Array.isArray(x)?x:[x,1]);if(!adm.length)throw Error(`unknown misconception rule ${w.rule} for ${p.kind}`);
  const wv=optionValue(w.value);if(!adm.some(x=>same([wv.n,wv.d],x)))throw Error(`distractor ${w.value} is not rule ${w.rule} (${adm.map(x=>x.join('/'))})`);
 }
 if(new Set(p.distractors.map(w=>w.value)).size!==3||!wrongIdx.every(i=>p.distractors.some(w=>w.value===q.choices[i])))throw Error('ledger distractors ≠ wrong options');
 if(new Set(q.distractor_tags).size<2)throw Error('need 2+ misconception tags');
 // textbook wording traps
 if(/공/.test(q.prompt)&&/꺼낼/.test(q.prompt)&&!(q.prompt.includes('임의로')&&q.prompt.endsWith(EQ)))throw Error('ball draw needs 임의로 + (단, 공의 모양과 크기는 모두 같다.)');
 if(/주머니/.test(q.prompt)===false&&/공 한 개를/.test(q.prompt))throw Error('ball bag');
 if(/(두|세) 개의 (주사위|동전)/.test(q.prompt)&&!/서로 다른 (두|세) 개의/.test(q.prompt))throw Error('서로 다른 missing');
 if(U.prob&&/카드/.test(q.prompt)&&!q.prompt.includes('임의로'))throw Error('probability card draw needs 임의로');
 if(/다시 넣/.test(q.prompt)&&!/확인한 후 다시 넣고|확인하고 다시 넣은 후/.test(q.prompt))throw Error('replacement phrase');
 return {idx,vals,isProb};
}
function loadPack(packId){const f=path.join(out,`${packId}.json`);if(!fs.existsSync(f))return null;return {pack:JSON.parse(fs.readFileSync(f,'utf8')),proof:new Map(JSON.parse(fs.readFileSync(path.join(here,`${packId}-proofs.json`),'utf8')).proofs.map(p=>[p.id,p]))};}
const RANK_GATE=0.35;const reports=[];
for(const [packId,U] of Object.entries(UNITS)){const L=loadPack(packId);if(!L){failures.push(`${packId}: missing`);continue;}const {pack,proof}=L;
 const st=validatePackV3(pack);for(const e of st.errors)failures.push(`${packId}: validate-pack-v3: ${e}`);
 const unit=curriculum.units.find(u=>u.id===packId);if(!unit||JSON.stringify([...pack.standards].sort())!==JSON.stringify([...unit.standards].sort()))failures.push(`${packId}: standards`);
 if(pack.schema_version!==3||pack.school!=='middle'||pack.grade!==2||pack.semester!==2||pack.unit_id!==packId)failures.push(`${packId}: header`);
 if(pack.items[0].difficulty!==1)failures.push(`${packId}: items[0] difficulty 1`);
 const bands={1:0,2:0,3:0,4:0},slots={},kinds={},heur={items:0,rank:[0,0,0,0],answerIsMax:0,answerIsMin:0,optionAboveOne:0,halfOption:0,halfCorrect:0};const prompts=new Set();
 for(const q of pack.items){stats.items++;try{const p=proof.get(q.id);bands[q.difficulty]++;if(p)kinds[p.kind]=(kinds[p.kind]??0)+1;
  const key=q.prompt.replace(/\([^)]*\)/g,'').replace(/\s+/g,' ').trim();if(prompts.has(key))throw Error('duplicate prompt');prompts.add(key);
  const r=checkItem(packId,U,q,p);(slots[q.difficulty]??=[0,0,0,0])[r.idx]++;
  heur.items++;const x=r.vals.map(v=>v.n/v.d);heur.rank[x.filter(v=>v<x[r.idx]).length]++;if(x[r.idx]===Math.max(...x))heur.answerIsMax++;if(x[r.idx]===Math.min(...x))heur.answerIsMin++;
  if(r.isProb){if(x.some(v=>v>1))heur.optionAboveOne++;if(q.choices.includes('{frac:1/2}')){heur.halfOption++;if(q.answer==='{frac:1/2}')heur.halfCorrect++;}}
 }catch(e){failures.push(`${q.id}: ${e.message}`);}}
 for(const [b,s] of Object.entries(slots))if(Math.max(...s)-Math.min(...s)>1)failures.push(`${packId}: slot imbalance d${b} ${s}`);
 // value-rank of the correct option (0 smallest … 3 largest): no rank and no pair of ranks may be a free win
 {const R=heur.rank,n=heur.items;heur.rankShare=R.map(c=>+(c/n).toFixed(3));heur.bestRankStrategy=+Math.max(...R.map(c=>c/n),...[[0,1],[0,2],[0,3],[1,2],[1,3],[2,3]].map(([a,b])=>(R[a]+R[b])/2/n)).toFixed(3);
  if(heur.bestRankStrategy>RANK_GATE)failures.push(`${packId}: value-rank strategy wins ${heur.bestRankStrategy} > ${RANK_GATE} (ranks ${R})`);}
 if(Object.values(bands).some(n=>n<pack.items.length*.2))failures.push(`${packId}: difficulty band too thin ${JSON.stringify(bands)}`);
 if(pack.items.length<300)failures.push(`${packId}: fewer than 300`);
 const slotTotal=[0,0,0,0];for(const s of Object.values(slots))s.forEach((n,i)=>slotTotal[i]+=n);
 reports.push({pack_id:packId,items:pack.items.length,bands,slots,slotTotal,kinds,heuristics:heur});
}
// ── mutation self-test ──
const selftest={answerShift:[0,0],correctCorrupted:[0,0],plantedDuplicateCorrect:[0,0],plantedEquivalent:[0,0],promptNumber:[0,0],distractorRuleSwap:[0,0]};const undetected=[];
if(process.argv.includes('--selftest')){const clone=o=>JSON.parse(JSON.stringify(o));
 const expectFail=(b,packId,U,q,p)=>{selftest[b][1]++;try{checkItem(packId,U,q,p);}catch{selftest[b][0]++;return;}if(b==='promptNumber')undetected.push(`${q.id}: ${q.prompt}`);else failures.push(`selftest ${b} not detected: ${q.id}`);};
 for(const [packId,U] of Object.entries(UNITS)){const L=loadPack(packId);if(!L)continue;const {pack,proof}=L;
  for(const orig of pack.items){const p=proof.get(orig.id);const ai=orig.choices.indexOf(orig.answer);const v=optionValue(orig.answer);
   let q=clone(orig);q.answer=q.choices[(ai+1)%4];expectFail('answerShift',packId,U,q,p);
   q=clone(orig);let bad;const keys=orig.choices.map(c=>valueKey(optionValue(c)));
   for(let k=1;k<60&&!bad;k++){const n=v.n+k,d=v.d,g=gcd(n,d);const c=d/g===1?String(n/g):`{frac:${n/g}/${d/g}}`;if(!keys.includes(valueKey(optionValue(c))))bad=c;}
   q.choices[ai]=bad;q.answer=bad;q.answerNumeric=(o=>o.n/o.d)(optionValue(bad));const p2=clone(p);expectFail('correctCorrupted',packId,U,q,p2);
   q=clone(orig);q.choices[(ai+1)%4]=orig.answer;expectFail('plantedDuplicateCorrect',packId,U,q,p);
   if(v.d>1){q=clone(orig);q.choices[(ai+2)%4]=`{frac:${2*v.n}/${2*v.d}}`;expectFail('plantedEquivalent',packId,U,q,p);}
   q=clone(orig);const pp=clone(p);pp.distractors[0].rule=pp.distractors[1].rule===pp.distractors[0].rule?'half':pp.distractors[1].rule;
   if(!same((o=>[o.n,o.d])(optionValue(pp.distractors[0].value)),(o=>[o.n,o.d])(optionValue(pp.distractors[1].value))))expectFail('distractorRuleSwap',packId,U,q,pp);
   q=clone(orig);const mm=/(^|[^0-9/:])(\d+)(?=[^0-9/:]|$)/.exec(q.prompt.replace(/\{frac:\d+\/\d+\}/g,f=>'#'.repeat(f.length)));
   if(mm){const at=mm.index+mm[1].length;q.prompt=q.prompt.slice(0,at)+String(Number(mm[2])+1)+q.prompt.slice(at+mm[2].length);expectFail('promptNumber',packId,U,q,p);}
  }}}
const result={verdict:failures.length?'fail':'pass',...stats,selftest:process.argv.includes('--selftest')?{...Object.fromEntries(Object.entries(selftest).map(([k,v])=>[k,`${v[0]}/${v[1]} detected`])),promptNumberUnchangedAnswer:undetected}:'not run (pass --selftest)',failures:failures.length,packs:reports,errors:failures.slice(0,300)};
fs.writeFileSync(path.join(here,'check-m2s2-prob-report.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify({verdict:result.verdict,items:stats.items,enumeratedOutcomes:stats.enumeratedOutcomes,distractorsRederived:stats.distractorsRederived,failures:failures.length,selftest:result.selftest,packs:reports.map(r=>({pack_id:r.pack_id,items:r.items,bands:r.bands,slotTotal:r.slotTotal,heuristics:r.heuristics}))},null,1));
if(failures.length){console.error(failures.slice(0,40).join('\n'));process.exitCode=1;}
