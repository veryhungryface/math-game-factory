#!/usr/bin/env node
// m2s2-u7 확률 ([9수04-06]) — schema v3: every item is a four-option choice. Probability answers
// and options are reduced fractions (textbook convention); the only integer-answer family is
// 「빨간 공은 몇 개」 (확률에서 개수 역산). Corpus traps: 「임의로」 + (단, 공의 모양과 크기는 모두 같다.),
// 「서로 다른」 dice/coins, only with-replacement repeated trials (「확인한 후 다시 넣고」), OR events are
// disjoint or posed as direct listing. Distractors come from named misconceptions (curriculum m2s2-u7).
import {makeQ,wrong,probWrongs,writeProbPack,drops,token,gcd,josa} from './prob-kit-v3.mjs';
const T='m2s2-u7.';
const MC={half:T+'equal-likelihood-bias',comp:T+'event-complement-confusion',one:T+'event-count-omitted',space:T+'wrong-sample-space',
  inv:T+'ratio-reversed',count:T+'denominator-omitted',sp:'m2s2-u7.sum-product-confusion',order:T+'order-ignored',single:T+'single-stage-only',
  overlap:T+'overlap-double-counted',basic:T+'basic-property',freq:T+'frequency-equals-theory',numer:T+'numerator-as-count',partpart:T+'part-part-ratio'};
const EQ='(단, 공의 모양과 크기는 모두 같다.)';
const bag=parts=>`${parts}가 들어 있는 주머니에서`;
const pools={};const add=(k,it)=>(pools[k]??=[]).push(it);

// ── d1 ─────────────────────────────────────────────────
for(let r=1;r<=9;r++)for(let b=1;b<=9;b++){const t=r+b;
  add('balls',makeQ({prompt:`${bag(`빨간 공 ${r}개와 파란 공 ${b}개`)} 공 한 개를 임의로 꺼낼 때, 빨간 공이 나올 확률을 구하시오. ${EQ}`,n:r,d:t,probability:true,
    explain:`모든 경우 ${t}가지 중 빨간 공이 나오는 경우는 ${r}가지이므로 @P입니다.`,unitConcept:'경우의 수의 비율로서의 확률',difficulty:1,kind:'ball',args:{r,b,event:'red'},distractors:probWrongs(r,t)}));
  add('complement',makeQ({prompt:`${bag(`빨간 공 ${r}개와 파란 공 ${b}개`)} 공 한 개를 임의로 꺼낼 때, 빨간 공이 나오지 않을 확률을 구하시오. ${EQ}`,n:b,d:t,probability:true,
    explain:`(빨간 공이 나오지 않을 확률)=1−(빨간 공이 나올 확률)=1−${token(r,t)}=@P입니다.`,unitConcept:'어떤 사건이 일어나지 않을 확률',difficulty:2,kind:'ball',args:{r,b,event:'not-red'},
    distractors:[wrong(r,t,MC.comp,'complement',[b,t]),...probWrongs(b,t).filter(w=>w.rule!=='complement')]}));
}
for(let n=6;n<=20;n++)for(let k=2;k<=6;k++){const c=Math.floor(n/k);
  add('multiples',makeQ({prompt:`1부터 ${n}까지의 자연수가 각각 하나씩 적힌 카드 ${n}장 중에서 한 장을 임의로 뽑을 때, ${k}의 배수가 나올 확률을 구하시오.`,n:c,d:n,probability:true,
    explain:`모든 경우 ${n}가지 중 ${k}의 배수는 ${c}가지이므로 @P입니다.`,unitConcept:'경우의 수의 비율로서의 확률',difficulty:1,kind:'multiples',args:{n,k},distractors:probWrongs(c,n,[wrong(1,k,MC.space,'one-over-divisor',[k])])}));
}
for(const [color,other] of [['흰','검은'],['빨간','파란'],['노란','초록']])for(let n=2;n<=9;n++)for(const certain of [true,false]){
  const p=certain?1:0;
  add('basic',makeQ({prompt:`${color} 공만 ${n}개 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, ${certain?color:other} 공이 나올 확률을 구하시오. ${EQ}`,n:p,d:1,probability:true,
    explain:certain?`꺼낸 공은 반드시 ${color} 공이므로 확률은 1입니다.`:`${other} 공은 절대로 나오지 않으므로 확률은 0입니다.`,unitConcept:'확률의 기본 성질',difficulty:1,kind:'single-color',args:{n,certain},
    distractors:[wrong(1-p,1,MC.basic,'certain-impossible-swap',[p]),wrong(1,n,MC.one,'one-outcome',[n]),wrong(1,2,MC.half,'half',[]),wrong(n,1,MC.count,'count-only',[n])]}));
}
// 주사위 한 개
const DIE=[['짝수의',[2,4,6]],['홀수의',[1,3,5]],['3의 배수의',[3,6]],['소수의',[2,3,5]],['6의 약수의',[1,2,3,6]],['4의 약수의',[1,2,4]],['5 이상의',[5,6]],['2 이하의',[1,2]],['4 미만의',[1,2,3]],['4 초과의',[5,6]],['3보다 큰',[4,5,6]]];
DIE.forEach(([txt,set],i)=>{const f=set.length;
  add('dieOne',makeQ({prompt:`주사위 한 개를 던질 때, ${txt} 눈이 나올 확률을 구하시오.`,n:f,d:6,probability:true,explain:`모든 경우 6가지 중 ${set.join(', ')}의 ${f}가지이므로 @P입니다.`,
    unitConcept:'경우의 수의 비율로서의 확률',difficulty:1,kind:'die-one',args:{i,set},distractors:probWrongs(f,6)}));});

// ── d2 ─────────────────────────────────────────────────
for(let r=1;r<=5;r++)for(let b=1;b<=5;b++)for(let w=1;w<=4;w++){const t=r+b+w,c=r+b;
  add('colors',makeQ({prompt:`${bag(`빨간 공 ${r}개, 파란 공 ${b}개, 흰 공 ${w}개`)} 공 한 개를 임의로 꺼낼 때, 빨간 공 또는 파란 공이 나올 확률을 구하시오. ${EQ}`,n:c,d:t,probability:true,
    explain:`두 사건은 동시에 일어나지 않으므로 ${token(r,t)}+${token(b,t)}=@P입니다.`,unitConcept:'두 사건 A 또는 B가 일어날 확률',difficulty:2,kind:'colors',args:{r,b,w},
    distractors:[wrong(r*b,t*t,MC.sp,'multiply-disjoint-probabilities',[r,b,t]),wrong(r,t,MC.one,'first-part',[r,t]),...probWrongs(c,t)]}));
}
for(let n=10;n<=50;n+=5)for(let h=2;h<n;h+=3){
  add('experiment',makeQ({prompt:`동전 한 개를 ${n}번 던졌더니 앞면이 ${h}번 나왔다. 이때 앞면이 나온 상대도수를 구하시오.`,n:h,d:n,probability:true,
    explain:`(상대도수)=(앞면이 나온 횟수)÷(전체 던진 횟수)=${h}÷${n}=@P입니다.`,unitConcept:'상대도수와 확률',difficulty:2,kind:'experiment',args:{n,h},
    distractors:[wrong(1,2,MC.freq,'half',[]),wrong(n-h,n,MC.comp,'complement',[h,n]),wrong(h,n-h,MC.partpart,'part-to-part',[h,n]),wrong(n,h,MC.inv,'inverse',[h,n]),wrong(h,1,MC.count,'count-only',[h])]}));
}
// 서로 다른 두 개의 동전 (동전 두 개의 모든 경우를 3가지로 보는 오개념)
for(const [txt,f,uf] of [['모두 앞면이',1,1],['모두 뒷면이',1,1],['앞면과 뒷면이 하나씩',2,1],['적어도 한 개는 앞면이',3,2],['적어도 한 개는 뒷면이',3,2],['서로 같은 면이',2,2]]){
  add('coins2',makeQ({prompt:`서로 다른 두 개의 동전을 동시에 던질 때, ${txt} 나올 확률을 구하시오.`,n:f,d:4,probability:true,
    explain:`모든 경우는 (앞, 앞), (앞, 뒤), (뒤, 앞), (뒤, 뒤)의 4가지이고 그중 ${f}가지이므로 @P입니다.`,unitConcept:'경우의 수의 비율로서의 확률',difficulty:2,kind:'coins',args:{coins:2,txt},
    distractors:[wrong(uf,3,MC.order,'unordered-coins',[uf,3]),wrong(4-f,4,MC.comp,'complement',[f,4]),wrong(1,2,MC.half,'half',[]),wrong(1,4,MC.one,'one-outcome',[4]),wrong(f,1,MC.count,'count-only',[f])]}));
}

// ── d3 ─────────────────────────────────────────────────
const range6=[1,2,3,4,5,6];
const test=(kind,k)=>({'sum-eq':(x,y)=>x+y===k,'sum-le':(x,y)=>x+y<=k,'sum-ge':(x,y)=>x+y>=k,'product-eq':(x,y)=>x*y===k,'difference-eq':(x,y)=>Math.abs(x-y)===k,'product-multiple':(x,y)=>x*y%k===0,'same':(x,y)=>x===y})[kind];
const diceText=(kind,k)=>({'sum-eq':`두 눈의 수의 합이 ${k}일`,'sum-le':`두 눈의 수의 합이 ${k} 이하일`,'sum-ge':`두 눈의 수의 합이 ${k} 이상일`,'product-eq':`두 눈의 수의 곱이 ${k}일`,
  'difference-eq':`두 눈의 수의 차가 ${k}일`,'product-multiple':`두 눈의 수의 곱이 ${k}의 배수일`,'same':'두 눈의 수가 같을'})[kind];
for(const [kind,lo,hi] of [['sum-eq',2,12],['sum-le',3,11],['sum-ge',3,11],['product-eq',2,30],['difference-eq',1,5],['product-multiple',2,6],['same',0,0]])for(let k=lo;k<=hi;k++){
  let n=0,u=0;for(const x of range6)for(const y of range6)if(test(kind,k)(x,y)){n++;if(x<=y)u++;}if(n===0||n===36)continue;
  add('dice',makeQ({prompt:`서로 다른 두 개의 주사위를 동시에 던질 때, ${diceText(kind,k)} 확률을 구하시오.`,n,d:36,probability:true,
    explain:`모든 경우 6×6=36가지 중 사건이 일어나는 경우는 ${n}가지이므로 @P입니다.`,unitConcept:'서로 다른 두 주사위의 확률',difficulty:3,kind:'dice',args:{test:kind,k},
    distractors:[wrong(u,21,MC.order,'unordered-dice',[kind,k]),...(n<=6?[wrong(n,6,MC.space,'one-die-denominator',[n])]:[]),wrong(n,12,MC.space,'sum-of-faces-denominator',[n]),...probWrongs(n,36)]}));
}
// 동시에 일어날 수 있는 두 사건: 직접 나열
for(const [a,b] of [[2,3],[2,5],[3,4],[2,7],[3,5],[4,6],[2,9],[3,6]])for(let n=6;n<=20;n++){
  const hits=[],both=[];for(let i=1;i<=n;i++){if(i%a===0||i%b===0)hits.push(i);if(i%a===0&&i%b===0)both.push(i);}if(!both.length)continue;
  const ca=Math.floor(n/a),cb=Math.floor(n/b);
  add('overlap',makeQ({prompt:`1부터 ${n}까지의 자연수가 각각 하나씩 적힌 카드 ${n}장 중에서 한 장을 임의로 뽑을 때, ${a}의 배수 또는 ${b}의 배수가 나올 확률을 구하시오.`,n:hits.length,d:n,probability:true,
    explain:`${hits.join(', ')}의 ${hits.length}가지(겹치는 ${josa(both.join(', '),'은','는')} 한 번만 셉니다)이므로 @P입니다.`,unitConcept:'동시에 일어날 수 있는 두 사건 — 직접 세기',difficulty:3,kind:'multiples-or',args:{n,a,b},
    distractors:[wrong(ca+cb,n,MC.overlap,'add-overlapping-counts',[a,b,n]),...probWrongs(hits.length,n)]}));
}
// 확률에서 개수 역산 (정수 답)
for(let t=6;t<=30;t++)for(let r=2;r<t;r++){if(gcd(r,t)===1)continue;const [a,d]=[r/gcd(r,t),t/gcd(r,t)];
  add('reverse',makeQ({prompt:`빨간 공과 파란 공이 합하여 ${t}개 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, 빨간 공이 나올 확률이 {frac:${a}/${d}}이다. 빨간 공은 몇 개인지 구하시오. ${EQ}`,n:r,
    explain:`(빨간 공의 개수)=${t}×{frac:${a}/${d}}=${r}이므로 빨간 공은 ${r}개입니다.`,unitConcept:'확률에서 경우의 수 구하기',difficulty:3,kind:'reverse',args:{t,a,d},
    distractors:[wrong(a,1,MC.numer,'numerator-as-count',[a]),wrong(t-r,1,MC.comp,'complement-count',[r,t]),wrong(t/d,1,MC.numer,'unit-fraction-share',[t,d]),wrong(d,1,MC.count,'denominator-as-count',[d]),wrong(t,1,MC.count,'total-only',[t])]}));
}

// ── d4 ─────────────────────────────────────────────────
const again='공 한 개를 임의로 꺼내 확인한 후 다시 넣고 또 한 개를 꺼낼 때,';
for(let r=1;r<=8;r++)for(let b=1;b<=8;b++){const t=r+b;
  add('both',makeQ({prompt:`${bag(`빨간 공 ${r}개와 파란 공 ${b}개`)} ${again} 두 번 모두 빨간 공이 나올 확률을 구하시오. ${EQ}`,n:r*r,d:t*t,probability:true,
    explain:`두 번의 시행은 서로 영향을 끼치지 않으므로 {frac:${r}/${t}}×{frac:${r}/${t}}=@P입니다.`,unitConcept:'두 사건 A와 B가 동시에 일어날 확률',difficulty:4,kind:'replacement',args:{r,b,event:'both'},
    distractors:[wrong(r,t,MC.single,'single-stage',[r,t]),wrong(2*r,t,MC.sp,'add-instead-multiply-probabilities',[r,t]),wrong(r*r,t,MC.space,'count-only-denominator',[r,t]),wrong(r*r,t+t,MC.space,'add-denominators',[r,t]),...probWrongs(r*r,t*t)]}));
  const some=t*t-b*b;
  add('atLeast',makeQ({prompt:`${bag(`빨간 공 ${r}개와 파란 공 ${b}개`)} ${again} 적어도 한 번은 빨간 공이 나올 확률을 구하시오. ${EQ}`,n:some,d:t*t,probability:true,
    explain:`1−(두 번 모두 파란 공이 나올 확률)=1−{frac:${b}/${t}}×{frac:${b}/${t}}=@P입니다.`,unitConcept:'적어도 하나가 일어날 확률',difficulty:4,kind:'replacement',args:{r,b,event:'at-least-one'},
    distractors:[wrong(b*b,t*t,MC.comp,'complement-not-subtracted',[r,t]),wrong(r*r,t*t,MC.comp,'both-red',[r,t]),wrong(r,t,MC.single,'single-stage',[r,t]),wrong(2*r,t,MC.sp,'add-instead-multiply-probabilities',[r,t])]}));
}
// 동전 한 개와 주사위 한 개
DIE.forEach(([txt,set],i)=>{for(const face of ['앞면','뒷면']){const f=set.length;
  add('coinDie',makeQ({prompt:`동전 한 개와 주사위 한 개를 동시에 던질 때, 동전은 ${face}이 나오고 주사위는 ${txt} 눈이 나올 확률을 구하시오.`,n:f,d:12,probability:true,
    explain:`두 사건은 서로 영향을 끼치지 않으므로 {frac:1/2}×{frac:${f}/6}=@P입니다.`,unitConcept:'두 사건 A와 B가 동시에 일어날 확률',difficulty:4,kind:'coin-die',args:{i,set,face},
    distractors:[wrong(3+f,6,MC.sp,'add-instead-multiply-probabilities',[f]),wrong(f,6,MC.single,'coin-ignored',[f]),wrong(f,8,MC.space,'add-sample-spaces',[f]),wrong(1,12,MC.one,'one-outcome',[12]),wrong(12-f,12,MC.comp,'complement',[f,12])]}));}});
// 서로 다른 세 개의 동전
for(const [txt,f,uf] of [['적어도 한 개는 앞면이',7,3],['적어도 한 개는 뒷면이',7,3],['모두 앞면이',1,1],['앞면이 2개만',3,1],['앞면이 1개만',3,1],['모두 같은 면이',2,2],['앞면이 2개 이상',4,2]]){
  add('coins3',makeQ({prompt:`서로 다른 세 개의 동전을 동시에 던질 때, ${txt} 나올 확률을 구하시오.`,n:f,d:8,probability:true,
    explain:`모든 경우 2×2×2=8가지 중 ${f}가지이므로 @P입니다.`,unitConcept:f===7?'적어도 하나가 일어날 확률':'경우의 수의 비율로서의 확률',difficulty:4,kind:'coins',args:{coins:3,txt},
    distractors:[wrong(uf,4,MC.order,'unordered-coins',[uf,4]),wrong(8-f,8,MC.comp,'complement',[f,8]),wrong(f,6,MC.space,'add-sample-spaces',[f]),wrong(1,2,MC.half,'half',[]),wrong(1,8,MC.one,'one-outcome',[8])]}));
}
// 카드를 다시 넣고 두 번 뽑기
const CARD=[['홀수가',n=>n%2===1],['짝수가',n=>n%2===0],['3의 배수가',n=>n%3===0],['4의 배수가',n=>n%4===0],['소수가',n=>[2,3,5,7,11].includes(n)]];
for(let n=4;n<=12;n++)CARD.forEach(([txt,ok],i)=>{const e=Array.from({length:n},(_,j)=>j+1).filter(ok).length;if(e<1||e===n)return;
  add('cardsRep',makeQ({prompt:`1부터 ${n}까지의 자연수가 각각 하나씩 적힌 카드 ${n}장 중에서 한 장을 임의로 뽑아 확인하고 다시 넣은 후 또 한 장을 뽑을 때, 두 번 모두 ${txt} 적힌 카드가 나올 확률을 구하시오.`,n:e*e,d:n*n,probability:true,
    explain:`두 번의 시행은 서로 영향을 끼치지 않으므로 {frac:${e}/${n}}×{frac:${e}/${n}}=@P입니다.`,unitConcept:'두 사건 A와 B가 동시에 일어날 확률',difficulty:4,kind:'cards-replacement',args:{n,i},
    distractors:[wrong(e,n,MC.single,'single-stage',[e,n]),wrong(2*e,n,MC.sp,'add-instead-multiply-probabilities',[e,n]),wrong(e*e,n,MC.space,'count-only-denominator',[e,n]),wrong(e*e,2*n,MC.space,'add-denominators',[e,n]),...probWrongs(e*e,n*n)]}));});

const intro=makeQ({prompt:'주사위 한 개를 던질 때, 3의 배수의 눈이 나올 확률을 구하시오.',n:2,d:6,probability:true,explain:'모든 경우 6가지 중 3의 배수의 눈은 3, 6의 2가지이므로 @P입니다.',
  unitConcept:'경우의 수의 비율로서의 확률',difficulty:1,kind:'die-one',args:{i:2,set:[3,6]},distractors:[wrong(2,1,MC.count,'count-only',[2]),wrong(1,6,MC.one,'one-outcome',[6]),wrong(4,6,MC.comp,'complement',[2,6]),wrong(1,2,MC.half,'half',[])]});
const P=pools;const len=k=>P[k].filter(Boolean).length;
if(process.env.POOLS)console.log(Object.fromEntries(Object.keys(P).map(k=>[k,len(k)])),drops);
writeProbPack('m2s2-u7','확률',['[9수04-06]'],intro,[
  [P.balls,55],[P.multiples,35],[P.basic,10],[P.dieOne,len('dieOne')-1],
  [P.complement,45],[P.colors,42],[P.experiment,26-len('coins2')],[P.coins2,len('coins2')],
  [P.dice,50],[P.overlap,30],[P.reverse,30],
  [P.both,28],[P.atLeast,28],[P.coinDie,len('coinDie')],[P.coins3,len('coins3')],[P.cardsRep,110-28-28-len('coinDie')-len('coins3')]]);
