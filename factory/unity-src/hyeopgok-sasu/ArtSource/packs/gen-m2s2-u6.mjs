#!/usr/bin/env node
// m2s2-u6 경우의 수 ([9수04-05]) — schema v3, every item four-option choice with integer options.
// Scope (curriculum): sums and products of counts only — no permutation/combination notation, no
// inclusion–exclusion formula (overlapping OR is posed as a direct-listing problem), two or more
// objects are always 「서로 다른」. Distractors are the counts a student gets through a named
// misconception (curriculum m2s2-u6 misconceptions + textbook traps).
import {makeQ,wrong,writeProbPack,drops} from './prob-kit-v3.mjs';
const T='m2s2-u6.';
const MC={sp:T+'sum-product-confusion',second:T+'second-stage-omitted',first:T+'first-stage-omitted',both:T+'sum-and-product-both',
  lead:T+'leading-zero',reuse:T+'same-card-reused',order:T+'order-ignored',roles:T+'roles-order-confusion',same:T+'same-person-twice',
  overlap:T+'overlap-double-counted',comp:T+'complement-counted',cond:T+'event-condition-ignored',dbl:T+'doubles-double-counted',
  bound:T+'boundary-inclusion-confusion',partial:T+'partial-listing',path:T+'path-structure-misread',oneRole:T+'one-stage-only'};
// 받침 유무로 조사 선택: 한글은 종성, 숫자는 읽는 소리(0영1일3삼6육7칠8팔 받침).
const josa=(w,a,b)=>{const ch=String(w).slice(-1),c=ch.charCodeAt(0);if(c>=0xac00&&c<=0xd7a3)return w+((c-0xac00)%28?a:b);return w+(/[013678]/.test(ch)?a:b);};
const pools={};const add=(k,it)=>(pools[k]??=[]).push(it);

// ── d1: 곱의 법칙 · 합의 법칙 ─────────────────────────────
const wear=[['윗옷','벌','바지','벌'],['티셔츠','종류','바지','종류'],['모자','종류','목도리','종류']];
const pickOne=[['주스','종류','차','종류'],['소설책','권','만화책','권'],['김밥','종류','샌드위치','종류']];
for(let a=2;a<=12;a++)for(let b=2;b<=12;b++){if(a===b)continue;
  const [x,xu,y,yu]=wear[(a+b)%3],[p,pu,r,ru]=pickOne[(a*b)%3];
  add('product',makeQ({prompt:`서로 다른 ${x} ${josa(a+xu,'과','와')} ${y} ${b}${yu} 중에서 ${josa(x,'과','와')} ${josa(y,'을','를')} 각각 하나씩 골라 짝 짓는 경우의 수를 구하시오.`,n:a*b,
    explain:`${x}마다 ${josa(y,'을','를')} ${b}가지씩 고를 수 있으므로 ${a}×${b}=${a*b}가지입니다.`,unitConcept:'곱의 법칙',difficulty:1,kind:'product',args:{a,b},
    distractors:[wrong(a+b,1,MC.sp,'sum',[a,b]),wrong(a*b+a+b,1,MC.both,'product-plus-sum',[a,b]),wrong(2*a*b,1,MC.order,'pairs-counted-twice',[a,b]),wrong(a,1,MC.second,'first-count',[a]),wrong(b,1,MC.first,'first-count',[b])]}));
  add('sum',makeQ({prompt:`서로 다른 ${p} ${josa(a+pu,'과','와')} ${r} ${b}${ru} 중에서 ${p} 또는 ${josa(r,'을','를')} 하나만 고르는 경우의 수를 구하시오.`,n:a+b,
    explain:`${josa(p,'과','와')} ${josa(r,'을','를')} 함께 고르지 않으므로 ${a}+${b}=${a+b}가지입니다.`,unitConcept:'합의 법칙',difficulty:1,kind:'sum',args:{a,b},
    distractors:[wrong(a*b,1,MC.sp,'product',[a,b]),wrong(a*b+a+b,1,MC.both,'product-plus-sum',[a,b]),wrong(a,1,MC.second,'first-count',[a]),wrong(b,1,MC.first,'first-count',[b])]}));
}
// 합의 법칙: 동시에 일어나지 않는 두 사건 (a·b의 공배수가 n보다 크다)
const lcm=(a,b)=>a*b/((x,y)=>{while(y)[x,y]=[y,x%y];return x;})(a,b);
for(let n=10;n<=30;n++)for(let a=2;a<=9;a++)for(let b=a+1;b<=9;b++){
  const ca=Math.floor(n/a),cb=Math.floor(n/b);const both=Math.floor(n/lcm(a,b));
  const item=(d,kind,unitConcept)=>makeQ({prompt:`1부터 ${n}까지의 자연수가 각각 하나씩 적힌 카드 ${n}장 중에서 한 장을 뽑을 때, ${a}의 배수 또는 ${b}의 배수가 나오는 경우의 수를 구하시오.`,
    n:ca+cb-both,explain:both?`${Array.from({length:n},(_,i)=>i+1).filter(i=>i%a===0||i%b===0).join(', ')}의 ${ca+cb-both}가지입니다(겹치는 ${josa(Array.from({length:both},(_,i)=>(i+1)*lcm(a,b)).join(', '),'은','는')} 한 번만 셉니다).`:`${a}의 배수 ${ca}가지와 ${b}의 배수 ${cb}가지는 겹치지 않으므로 ${ca}+${cb}=${ca+cb}가지입니다.`,
    unitConcept,difficulty:d,kind,args:{n,a,b},
    distractors:[...(both?[wrong(ca+cb,1,MC.overlap,'add-overlapping-counts',[n,a,b])]:[]),wrong(ca*cb,1,MC.sp,'product-of-counts',[n,a,b]),wrong(n-(ca+cb-both),1,MC.comp,'complement-count',[n,a,b]),wrong(ca,1,MC.second,'first-event-only',[n,a]),wrong(cb,1,MC.first,'first-event-only',[n,b])]});
  if(!both&&ca>=2&&cb>=2)add('orDisjoint',item(1,'cards-or','합의 법칙'));
  if(both&&both<Math.min(ca,cb))add('orOverlap',item(4,'cards-or-overlap','동시에 일어날 수 있는 두 사건 — 직접 세기'));
}

// ── d2: 서로 다른 두 주사위 ─────────────────────────────
const range6=[1,2,3,4,5,6];
const test=(kind,k)=>({'sum-eq':(x,y)=>x+y===k,'sum-le':(x,y)=>x+y<=k,'sum-ge':(x,y)=>x+y>=k,'sum-lt':(x,y)=>x+y<k,'sum-gt':(x,y)=>x+y>k,'product-eq':(x,y)=>x*y===k,
  'difference-eq':(x,y)=>Math.abs(x-y)===k,'product-multiple':(x,y)=>x*y%k===0})[kind];
const cnt=(f)=>{let n=0,u=0,dd=0;for(const x of range6)for(const y of range6)if(f(x,y)){n++;if(x<=y)u++;if(x===y)dd++;}return {n,u,dd};};
const diceText=(kind,k)=>({'sum-eq':`두 눈의 수의 합이 ${k}인`,'sum-le':`두 눈의 수의 합이 ${k} 이하인`,'sum-ge':`두 눈의 수의 합이 ${k} 이상인`,'product-eq':`두 눈의 수의 곱이 ${k}인`,
  'difference-eq':`두 눈의 수의 차가 ${k}인`,'product-multiple':`두 눈의 수의 곱이 ${k}의 배수인`})[kind];
for(const [kind,lo,hi] of [['sum-eq',3,11],['sum-le',3,11],['sum-ge',3,11],['product-eq',2,30],['difference-eq',1,5],['product-multiple',2,6]])for(let k=lo;k<=hi;k++){
  const c=cnt(test(kind,k));if(c.n<=1||c.n>=36)continue;
  const ws=[wrong(c.u,1,MC.order,'unordered-count',[kind,k])];
  if(kind==='sum-le')ws.push(wrong(cnt(test('sum-lt',k)).n,1,MC.bound,'strict-inequality',[kind,k]));
  if(kind==='sum-ge')ws.push(wrong(cnt(test('sum-gt',k)).n,1,MC.bound,'strict-inequality',[kind,k]));
  if(kind==='sum-le'||kind==='sum-ge')ws.push(wrong(cnt(test('sum-eq',k)).n,1,MC.bound,'equality-only',[kind,k]));
  if(c.dd)ws.push(wrong(2*c.u,1,MC.dbl,'doubles-counted-twice',[kind,k]));
  if(kind==='product-eq'&&k<=12)ws.push(wrong(cnt(test('sum-eq',k)).n,1,MC.sp,'sum-for-product',[kind,k]));
  ws.push(wrong(36-c.n,1,MC.comp,'complement-count',[kind,k]),wrong(c.u-c.dd,1,MC.partial,'distinct-unordered-only',[kind,k]),wrong(12,1,MC.sp,'dice-sum-of-faces',[6,6]),wrong(36,1,MC.cond,'all-outcomes',[6,6]));
  add('dice',makeQ({prompt:`서로 다른 두 개의 주사위를 동시에 던질 때, ${diceText(kind,k)} 경우의 수를 구하시오.`,n:c.n,
    explain:`두 주사위의 눈을 순서쌍 (첫째, 둘째)로 세면 조건에 맞는 경우는 ${c.n}가지입니다.`,unitConcept:'서로 다른 두 주사위의 경우의 수',difficulty:2,kind:'dice',args:{test:kind,k},distractors:ws}));
}

// ── d2~d4: 숫자 카드로 두 자리 자연수 만들기 (0 포함) ─────────────────
function cardCount(digits,m,{zero=false,reuse=false}={}){let c=0;for(const a of digits)for(const b of digits)if((zero||a!==0)&&(reuse||a!==b)&&(a*10+b)%m===0)c++;return c;}
for(let mask=1;mask<512;mask++){
  const digits=[0];for(let n=1;n<=9;n++)if(mask&(1<<(n-1)))digits.push(n);if(digits.length<3||digits.length>5)continue;
  for(const m of [1,2,3]){
    const count=cardCount(digits,m);if(count<2)continue;
    let unordered=0;for(let i=0;i<digits.length;i++)for(let j=i+1;j<digits.length;j++)if((digits[i]!==0&&(digits[i]*10+digits[j])%m===0)||(digits[j]!==0&&(digits[j]*10+digits[i])%m===0))unordered++;
    const target=m===1?'두 자리 자연수':m===2?'두 자리 짝수':'두 자리 자연수 중 3의 배수';const L=digits.length;
    add(`cards${m}`,makeQ({prompt:`${josa(digits.join(', '),'이','가')} 각각 하나씩 적힌 카드 ${L}장 중에서 2장을 뽑아 만들 수 있는 ${target}의 개수를 구하시오.`,n:count,
      explain:m===1?`십의 자리에는 0이 올 수 없으므로 ${L-1}가지, 일의 자리에는 남은 ${L-1}가지이므로 ${L-1}×${L-1}=${count}개입니다.`:`십의 자리에 0을 놓지 않고 같은 카드를 두 번 쓰지 않으면서 조건에 맞는 수를 세면 ${count}개입니다.`,
      unitConcept:'카드로 두 자리 자연수 만들기',difficulty:m+1,kind:'cards',args:{digits,m},
      distractors:[wrong(cardCount(digits,m,{zero:true}),1,MC.lead,'cards-leading-zero',[digits,m]),wrong(unordered,1,MC.order,'cards-unordered',[digits,m]),
        wrong(cardCount(digits,m,{reuse:true}),1,MC.reuse,'cards-reuse',[digits,m]),wrong(cardCount(digits,m,{zero:true,reuse:true}),1,MC.lead,'cards-no-restriction',[digits,m]),
        wrong(2*(L-1),1,MC.sp,'add-card-stages',[L]),wrong(L-1,1,MC.second,'card-first-stage',[L]),...(m===2?[wrong(digits.filter(x=>x%2===0).length,1,MC.first,'ones-digit-only',[digits])]:[])]}));
  }
}

// ── d3: 길 찾기 (곱의 법칙과 합의 법칙을 함께) ─────────────────────
for(let a=2;a<=6;a++)for(let b=2;b<=6;b++)for(let c=1;c<=4;c++){
  add('paths',makeQ({prompt:`A 마을에서 B 마을로 가는 길은 ${a}가지, B 마을에서 C 마을로 가는 길은 ${b}가지이고, A 마을에서 C 마을로 바로 가는 길은 ${c}가지이다. A 마을에서 C 마을로 가는 경우의 수를 구하시오. (단, 한 번 지난 마을은 다시 지나지 않는다.)`,
    n:a*b+c,explain:`B 마을을 거쳐 가는 경우 ${a}×${b}=${a*b}가지, 바로 가는 경우 ${c}가지이므로 ${a*b}+${c}=${a*b+c}가지입니다.`,unitConcept:'합의 법칙과 곱의 법칙',difficulty:3,kind:'paths',args:{a,b,c},
    distractors:[wrong(a+b+c,1,MC.sp,'sum-all-roads',[a,b,c]),wrong(a*b,1,MC.second,'direct-road-omitted',[a,b]),wrong(a*b*c,1,MC.sp,'product-all-roads',[a,b,c]),wrong(a*(b+c),1,MC.path,'direct-road-as-second-leg',[a,b,c]),wrong(a+b,1,MC.second,'sum-direct-omitted',[a,b])]}));
}

// ── d4: 대표 뽑기 (순서 있음/없음) ─────────────────────────
for(let n=4;n<=18;n++)for(const ordered of [true,false]){
  const count=ordered?n*(n-1):n*(n-1)/2;
  add('roles',makeQ({prompt:ordered?`후보 ${n}명 중에서 회장 1명과 부회장 1명을 뽑는 경우의 수를 구하시오. (단, 한 사람이 두 역할을 맡을 수 없다.)`:`후보 ${n}명 중에서 대표 2명을 뽑는 경우의 수를 구하시오.`,n:count,
    explain:ordered?`회장 ${n}가지마다 부회장 ${n-1}가지이므로 ${n}×${n-1}=${count}가지입니다.`:`두 명을 뽑는 순서를 생각하면 ${n}×${n-1}가지이고, 같은 두 명이 두 번씩 세어지므로 ${n}×${n-1}÷2=${count}가지입니다.`,
    unitConcept:ordered?'자격이 다른 대표 뽑기':'자격이 같은 대표 뽑기',difficulty:4,kind:'roles',args:{n,ordered},
    distractors:[wrong(ordered?n*(n-1)/2:n*(n-1),1,MC.roles,'opposite-role-order',[n,ordered]),wrong(n*n,1,MC.same,'square',[n]),wrong(2*n-1,1,MC.sp,'add-role-stages',[n]),wrong(n,1,MC.oneRole,'one-stage',[n])]}));
}

// ── intro (d1) ──────────────────────────────────────────
const intro=makeQ({prompt:'주사위 한 개를 던질 때, 3의 배수의 눈이 나오는 경우의 수를 구하시오.',n:2,explain:'3의 배수의 눈은 3, 6의 2가지입니다.',unitConcept:'한 사건의 경우의 수',difficulty:1,kind:'die-one-count',args:{event:'multiple-3'},
  distractors:[wrong(4,1,MC.comp,'complement-count',[6,2]),wrong(6,1,MC.cond,'all-outcomes',[6]),wrong(1,1,MC.partial,'first-multiple-only',[3])]});
const P=pools;
if(process.env.POOLS)console.log(Object.fromEntries(Object.entries(P).map(([k,v])=>[k,v.filter(Boolean).length])),drops);
writeProbPack('m2s2-u6','경우의 수',['[9수04-05]'],intro,[
  [P.product,36],[P.sum,30],[P.orDisjoint,34],
  [P.dice,P.dice.filter(Boolean).length],[P.cards1,100-P.dice.filter(Boolean).length],
  [P.cards2,70],[P.paths,30],
  [P.roles,30],[P.cards3,P.cards3.filter(Boolean).length],[P.orOverlap,70-P.cards3.filter(Boolean).length]]);
