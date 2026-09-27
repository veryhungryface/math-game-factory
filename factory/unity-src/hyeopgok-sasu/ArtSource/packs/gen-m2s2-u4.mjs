#!/usr/bin/env node
// m2s2-u4 평행선과 선분의 길이의 비 ([9수03-14]) — schema v3 (전 문항 4지선다).
// 삼각형과 평행선, 두 변의 중점을 연결한 선분, 평행선 사이의 선분의 비, 사다리꼴,
// 삼각형의 무게중심(중선을 꼭짓점으로부터 2:1). Every wrong option is computed from a named
// misconception (tags below); non-integer results of a wrong rule are simply not offered.
import {amount,fraction,choice,W,gcd,writeGeoPack,drops} from './geo-kit.mjs';
const T='m2s2-u4.';
const MC={part:T+'part-whole-proportion-mixed',inv:T+'ratio-inverted',add:T+'additive-reasoning',mid:T+'midsegment-length-wrong',
  side:T+'corresponding-segment-confused',whole:T+'part-whole-confused',ratio:T+'centroid-ratio-reversed',cmid:T+'centroid-as-midpoint',
  bisect:T+'centroid-line-bisects-area',sq:T+'area-ratio-not-squared',avg:T+'trapezoid-average-only',ang:T+'parallel-angle-confused',six:T+'centroid-area-parts-miscounted'};
const pools={};const add=(k,it)=>(pools[k]??=[]).push(it);
const Q=(n,d)=>n%d===0?n/d:NaN; // exact quotient or "not offered"
const RAT=[];for(let p=1;p<=7;p++)for(let q=1;q<=7;q++)if(p!==q&&gcd(p,q)===1)RAT.push([p,q]);
const TP='삼각형에서 평행선과 선분의 길이의 비',MID='삼각형의 두 변의 중점을 연결한 선분의 성질',PL='평행선 사이의 선분의 길이의 비',TR='사다리꼴에서 평행선과 선분의 길이의 비',CG='삼각형의 무게중심',CGA='무게중심과 넓이';
const deStem='△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다.';
const RATIO='가장 간단한 자연수의 비로 나타내시오.';
// scalene-ish triangles (a=AB, b=BC, c=CA), deterministic thinning
const TRIS=[];for(let a=5;a<=30;a++)for(let b=5;b<=30;b++)for(let c=5;c<=30;c++){if(a===b||b===c||a===c||a+b<=c||b+c<=a||a+c<=b)continue;if((a*7+b*11+c*13)%17)continue;TRIS.push([a,b,c]);}

// ── 삼각형과 평행선 ─────────────────────────────────────────
for(const [p,q] of RAT)for(let u=1;u<=8;u++)for(let v=1;v<=12;v++){const a=p*u,b=q*u,c=p*v,e=q*v;if(a<2||b<2||c<2||e<2||a===c||e>99||c>99||(u+v)%2)continue;
  add('tp1',amount({kind:'de-ec',params:{AD:a,DB:b,AE:c},prompt:`${deStem} AD=${a} cm, DB=${b} cm, AE=${c} cm일 때, EC의 길이는 몇 cm인지 구하시오.`,answer:e,
    explain:`AD:DB=AE:EC이므로 ${a}:${b}=${c}:EC, EC=${b}×${c}÷${a}=${e} cm입니다.`,concept:TP,d:1,
    wrongs:[W(c+b-a,MC.add,'additive'),W(Q(a*c,b),MC.inv,'inverted'),W(Q(c*(a+b),a),MC.part,'AD:AB=AE:EC'),W(Q(c*b,a+b),MC.part,'DB:AB=EC:AE'),W(b,MC.side,'EC=DB')]}));
  add('tp1',amount({kind:'de-ae',params:{AD:a,DB:b,EC:e},prompt:`${deStem} AD=${a} cm, DB=${b} cm, EC=${e} cm일 때, AE의 길이는 몇 cm인지 구하시오.`,answer:c,
    explain:`AD:DB=AE:EC이므로 ${a}:${b}=AE:${e}, AE=${a}×${e}÷${b}=${c} cm입니다.`,concept:TP,d:1,
    wrongs:[W(e+a-b,MC.add,'additive'),W(Q(b*e,a),MC.inv,'inverted'),W(Q(a*e,a+b),MC.part,'AD:AB=AE:EC'),W(Q(e*(a+b),b),MC.part,'AB:DB=AE:EC'),W(a,MC.side,'AE=AD')]}));}
for(const [p,q] of RAT){if(p>=q)continue;for(let u=1;u<=8;u++)for(let v=1;v<=12;v++){const ad=p*u,ab=q*u,ac=q*v,ae=p*v,db=ab-ad;if(ad<2||ae<2||ae>99||(u*3+v)%3)continue;
  add('tp2',amount({kind:'de-ae-whole',params:{AD:ad,AB:ab,AC:ac},prompt:`${deStem} AD=${ad} cm, AB=${ab} cm, AC=${ac} cm일 때, AE의 길이는 몇 cm인지 구하시오.`,answer:ae,
    explain:`AD:AB=AE:AC이므로 ${ad}:${ab}=AE:${ac}, AE=${ad}×${ac}÷${ab}=${ae} cm입니다.`,concept:TP,d:2,
    wrongs:[W(Q(ad*ac,db),MC.part,'AD:DB=AE:AC'),W(ac-db,MC.add,'additive'),W(ac-ae,MC.whole,'EC-for-AE'),W(Q(ab*ac,ad),MC.inv,'inverted')]}));
  const bc=q*v,de=p*v;if(de<2||de>99)continue;
  add('tp2',amount({kind:'de-length',params:{AD:ad,AB:ab,BC:bc},prompt:`${deStem} AD=${ad} cm, AB=${ab} cm, BC=${bc} cm일 때, DE의 길이는 몇 cm인지 구하시오.`,answer:de,
    explain:`△ADE∽△ABC (AA 닮음)이므로 AD:AB=DE:BC, DE=${ad}×${bc}÷${ab}=${de} cm입니다.`,concept:TP,d:2,
    wrongs:[W(Q(ad*bc,db),MC.part,'AD:DB=DE:BC'),W(bc-db,MC.add,'additive'),W(Q(bc*db,ab),MC.whole,'DB:AB'),W(Q(bc,2),MC.mid,'midsegment')]}));
  if(db>=1&&(u+v)%2)add('tp2',amount({kind:'de-length-part',params:{AD:ad,DB:db,BC:bc},prompt:`${deStem} AD=${ad} cm, DB=${db} cm, BC=${bc} cm일 때, DE의 길이는 몇 cm인지 구하시오.`,answer:de,
    explain:`AD:AB=${ad}:${ab}이므로 DE=${bc}×${ad}÷${ab}=${de} cm입니다(AD:DB가 아니라 AD:AB를 씁니다).`,concept:TP,d:2,
    wrongs:[W(Q(ad*bc,db),MC.part,'AD:DB=DE:BC'),W(bc-db,MC.add,'additive'),W(Q(bc*db,ab),MC.whole,'DB:AB'),W(Q(bc,2),MC.mid,'midsegment')]}));}}
const extStem='변 BA의 연장선 위의 점 D와 변 CA의 연장선 위의 점 E에 대하여 DE∥BC이다.';
for(const [p,q] of RAT)for(let u=1;u<=6;u++)for(let v=1;v<=12;v++){const ad=p*u,ab=q*u,ac=q*v,ae=p*v;if(ad<2||ae<2||ae>99||ac>99||(u+2*v)%3)continue;
  add('tp3',amount({kind:'ext-ae',params:{AD:ad,AB:ab,AC:ac},prompt:`△ABC에서 ${extStem} AD=${ad} cm, AB=${ab} cm, AC=${ac} cm일 때, AE의 길이는 몇 cm인지 구하시오.`,answer:ae,
    explain:`△ADE∽△ABC (AA 닮음)이므로 AD:AB=AE:AC, AE=${ad}×${ac}÷${ab}=${ae} cm입니다.`,concept:TP,d:3,
    wrongs:[W(ac+ad-ab,MC.add,'additive'),W(Q(ab*ac,ad),MC.inv,'inverted'),W(Q(ad*ac,ad+ab),MC.part,'AD:DB=AE:AC'),W(ad,MC.side,'AE=AD')]}));
  const de=ae,bc=ac;if((u+v)%2)continue;
  add('tp3',amount({kind:'ext-de',params:{AD:ad,AB:ab,BC:bc},prompt:`△ABC에서 ${extStem} AD=${ad} cm, AB=${ab} cm, BC=${bc} cm일 때, DE의 길이는 몇 cm인지 구하시오.`,answer:de,
    explain:`△ADE∽△ABC이므로 AD:AB=DE:BC, DE=${ad}×${bc}÷${ab}=${de} cm입니다.`,concept:TP,d:3,
    wrongs:[W(Q(bc*ab,ad),MC.inv,'inverted'),W(Q(ad*bc,ad+ab),MC.part,'AD:DB=DE:BC'),W(bc+ad-ab,MC.add,'additive'),W(bc,MC.side,'DE=BC')]}));}
// ── 두 변의 중점을 연결한 선분 ──────────────────────────────────
const midStem='△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자.';
const conv='△ABC에서 변 AB의 중점 M을 지나고 변 BC에 평행한 직선이 변 AC와 만나는 점을 N이라고 하자.';
TRIS.forEach(([a,b,c],i)=>{
  if(b%2===0&&i%2===0)add('mid1',amount({kind:'mid-mn',params:{a,b,c},prompt:`${midStem} AB=${a} cm, BC=${b} cm, CA=${c} cm일 때, MN의 길이는 몇 cm인지 구하시오.`,answer:b/2,
    explain:`MN∥BC이고 MN={frac:1/2}BC이므로 MN=${b}÷2=${b/2} cm입니다.`,concept:MID,d:1,
    wrongs:[W(b,MC.mid,'equal'),W(2*b,MC.mid,'double'),W(Q(a,2),MC.side,'half-AB'),W(Q(c,2),MC.side,'half-CA')]}));
  if(i%2===1&&a%2===0&&c%2===0){const am=a/2,an=c/2,mn=Q(b,2);if(Number.isInteger(mn))
    add('mid1',amount({kind:'mid-bc',params:{a,b,c},prompt:`${midStem} AM=${am} cm, AN=${an} cm, MN=${mn} cm일 때, BC의 길이는 몇 cm인지 구하시오.`,answer:b,
      explain:`BC=2MN=2×${mn}=${b} cm입니다.`,concept:MID,d:1,wrongs:[W(Q(mn,2),MC.mid,'halved'),W(3*mn,MC.mid,'AM+MN-style'),W(2*am,MC.side,'double-AM'),W(2*an,MC.side,'double-AN'),W(4*mn,MC.mid,'double-twice')]}));}
  if(c%2===0&&i%3===0)add('mid2',amount({kind:'mid-converse-nc',params:{a,b,c},prompt:`${conv} AB=${a} cm, BC=${b} cm, CA=${c} cm일 때, NC의 길이는 몇 cm인지 구하시오.`,answer:c/2,
    explain:`중점 M을 지나고 BC에 평행한 직선은 AC의 중점을 지나므로 NC=${c}÷2=${c/2} cm입니다.`,concept:MID,d:2,
    wrongs:[W(c,MC.whole,'whole'),W(Q(b,2),MC.side,'MN-for-NC'),W(Q(a,2),MC.side,'MB-for-NC'),W(Q(c,3),MC.mid,'one-third')]}));
  if(b%2===0&&i%3===1)add('mid2',amount({kind:'mid-converse-mn',params:{a,b,c},prompt:`${conv} AB=${a} cm, BC=${b} cm, CA=${c} cm일 때, MN의 길이는 몇 cm인지 구하시오.`,answer:b/2,
    explain:`점 N은 AC의 중점이므로 MN={frac:1/2}BC=${b/2} cm입니다.`,concept:MID,d:2,
    wrongs:[W(b,MC.mid,'equal'),W(Q(c,2),MC.side,'half-CA'),W(Q(a,2),MC.side,'half-AB'),W(2*b,MC.mid,'double')]}));
  const s=a+b+c;if(s%4===0&&i%3===2){const h=s/2;const def=(a+b)%2===1;
    add('mid2',amount({kind:def?'mid-def-perimeter':'mid-amn-perimeter',params:{a,b,c},prompt:def?`△ABC의 세 변 AB, BC, CA의 중점을 각각 D, E, F라고 하자. AB=${a} cm, BC=${b} cm, CA=${c} cm일 때, △DEF의 둘레의 길이는 몇 cm인지 구하시오.`:`${midStem} AB=${a} cm, BC=${b} cm, CA=${c} cm일 때, △AMN의 둘레의 길이는 몇 cm인지 구하시오.`,answer:h,
      explain:def?`△DEF의 각 변은 마주 보는 변의 {frac:1/2}이므로 둘레는 (${a}+${b}+${c})÷2=${h} cm입니다.`:`AM=${a}÷2, AN=${c}÷2, MN=${b}÷2이므로 둘레는 (${a}+${b}+${c})÷2=${h} cm입니다.`,concept:MID,d:2,
      wrongs:def?[W(s,MC.mid,'equal-sides'),W(s/4,MC.mid,'halved-twice'),W(2*s,MC.mid,'double'),W(s-Math.min(a,b,c),MC.side,'two-sides')]
        :[W(Q(a+c,2)+b,MC.mid,'MN=BC'),W(s,MC.mid,'equal-sides'),W(s/4,MC.mid,'halved-twice'),W(Q(a+c,2),MC.side,'MN-omitted')]}));}
});
for(let x=20;x<=85;x++){const odd=x%2;
  add('mid2',amount({kind:odd?'mid-angle-bmn':'mid-angle-cnm',params:{x},prompt:odd?`${midStem} ∠B=${x}°일 때, ∠BMN의 크기는 몇 도인지 구하시오.`:`${midStem} ∠C=${x}°일 때, ∠CNM의 크기는 몇 도인지 구하시오.`,answer:180-x,
    explain:`MN∥BC이므로 ${odd?'∠BMN+∠B':'∠CNM+∠C'}=180°, 구하는 각은 180°−${x}°=${180-x}°입니다.`,concept:MID,d:2,lo:1,hi:179,
    wrongs:[W(x,MC.ang,'corresponding-equal'),W(90-x,MC.ang,'complement'),W(Q(x,2),MC.mid,'halved-with-length'),W(2*x,MC.mid,'doubled-with-length')]}));}
for(let x=30;x<=110;x+=5)for(let y=25;y<=110;y+=5){const z=180-x-y;if(z<15||x===y||y===z||x===z)continue;const askM=(x+y)%10===0;
  add('mid3',amount({kind:askM?'mid-angle-sum-m':'mid-angle-sum',params:{x,y},prompt:askM?`${midStem} ∠A=${x}°, ∠C=${y}°일 때, ∠AMN의 크기는 몇 도인지 구하시오.`:`${midStem} ∠A=${x}°, ∠B=${y}°일 때, ∠ANM의 크기는 몇 도인지 구하시오.`,answer:z,
    explain:askM?`MN∥BC이므로 ∠AMN=∠B=180°−${x}°−${y}°=${z}°입니다.`:`MN∥BC이므로 ∠ANM=∠C=180°−${x}°−${y}°=${z}°입니다.`,concept:MID,d:3,lo:1,hi:179,
    wrongs:[W(y,MC.ang,'wrong-corresponding'),W(180-z,MC.ang,'supplement'),W(x+y,MC.ang,'no-subtraction'),W(Q(180-x,2),MC.side,'isosceles-assumed')]}));}
// ── 평행선 사이의 선분 ─────────────────────────────────────────
const plStem='서로 평행한 세 직선 l, m, n이 직선 p와 만나는 점을 각각 A, B, C, 직선 q와 만나는 점을 각각 D, E, F라고 하자.';
for(const [p,q] of RAT)for(let u=1;u<=8;u++)for(let v=1;v<=10;v++){const ab=p*u,bc=q*u,de=p*v,ef=q*v;if(ab<2||de<2||ef>99||ef<2||ab===de||(u*v+p)%3)continue;
  add('pl',amount({kind:'pl-ef',params:{AB:ab,BC:bc,DE:de},prompt:`${plStem} AB=${ab} cm, BC=${bc} cm, DE=${de} cm일 때, EF의 길이는 몇 cm인지 구하시오.`,answer:ef,
    explain:`평행선 사이의 선분의 길이의 비는 같으므로 AB:BC=DE:EF, EF=${bc}×${de}÷${ab}=${ef} cm입니다.`,concept:PL,d:2,
    wrongs:[W(de+bc-ab,MC.add,'additive'),W(Q(ab*de,bc),MC.inv,'inverted'),W(bc,MC.side,'EF=BC'),W(Q(de*bc,ab+bc),MC.whole,'AC-as-whole')]}));
  const ac=ab+bc,df=de+ef;if(df<=99&&(u+v)%2)add('pl',amount({kind:'pl-df',params:{AB:ab,AC:ac,DE:de},prompt:`${plStem} AB=${ab} cm, AC=${ac} cm, DE=${de} cm일 때, DF의 길이는 몇 cm인지 구하시오.`,answer:df,
    explain:`AB:AC=DE:DF이므로 DF=${ac}×${de}÷${ab}=${df} cm입니다.`,concept:PL,d:2,
    wrongs:[W(de+ac-ab,MC.add,'additive'),W(ef,MC.whole,'EF-for-DF'),W(Q(ab*de,ac),MC.inv,'inverted'),W(ac,MC.side,'DF=AC')]}));}
// ── 사다리꼴 ──────────────────────────────────────────────
const trStem='AD∥BC인 사다리꼴 ABCD에서 두 변 AB, DC의 중점을 각각 M, N이라고 하자.';
for(let a=2;a<=50;a++)for(let b=a+2;b<=80;b+=2){const mn=(a+b)/2;if((a+b)%4===2&&a%3)continue;
  add('trMid',amount({kind:'trap-mn',params:{AD:a,BC:b},prompt:`${trStem} AD=${a} cm, BC=${b} cm일 때, MN의 길이는 몇 cm인지 구하시오.`,answer:mn,
    explain:`MN=(AD+BC)÷2=(${a}+${b})÷2=${mn} cm입니다.`,concept:TR,d:3,
    wrongs:[W(a+b,MC.mid,'no-halving'),W(Q(b-a,2),MC.mid,'difference'),W(Q(b,2),MC.side,'triangle-midsegment-BC'),W(b,MC.side,'equal-BC')]}));
  const pq=(b-a)/2;if(pq>=2&&a%2===0&&pq!==a/2)add('trPQ',amount({kind:'trap-pq',params:{AD:a,BC:b},prompt:`${trStem} MN이 두 대각선 BD, AC와 만나는 점을 각각 P, Q라고 하자. AD=${a} cm, BC=${b} cm일 때, PQ의 길이는 몇 cm인지 구하시오.`,answer:pq,
    explain:`MQ={frac:1/2}BC=${b/2} cm, MP={frac:1/2}AD=${a/2} cm이므로 PQ=${b/2}−${a/2}=${pq} cm입니다.`,concept:TR,d:4,
    wrongs:[W(mn,MC.avg,'midline'),W(b-a,MC.mid,'no-halving'),W(b/2,MC.side,'MQ-for-PQ'),W(a/2,MC.side,'MP-for-PQ')]}));
  if(a%2===0&&(a+b)%3===0)add('trMid',amount({kind:'trap-mq',params:{AD:a,BC:b},prompt:`${trStem} MN이 대각선 AC와 만나는 점을 Q라고 하자. AD=${a} cm, BC=${b} cm일 때, MQ의 길이는 몇 cm인지 구하시오.`,answer:b/2,
    explain:`△ABC에서 M은 AB의 중점이고 MQ∥BC이므로 MQ={frac:1/2}BC=${b/2} cm입니다.`,concept:TR,d:3,
    wrongs:[W(a/2,MC.side,'wrong-triangle'),W(mn,MC.avg,'midline'),W(b,MC.mid,'equal'),W(Q(b-a,2),MC.mid,'difference')]}));}
for(const [m,n] of [[1,2],[2,1],[1,3],[3,1],[2,3],[3,2],[1,4],[3,4],[4,3],[2,5],[5,2],[1,5]])for(let a=2;a<=40;a++)for(let b=a+1;b<=80;b++){
  const num=n*a+m*b;if(num%(m+n))continue;const ef=num/(m+n);if(ef<2||(a+b+m)%5)continue;
  add('trEF',amount({kind:'trap-ef',params:{m,n,AD:a,BC:b},prompt:`AD∥EF∥BC인 사다리꼴 ABCD에서 점 E, F는 각각 변 AB, DC 위에 있고 AE:EB=${m}:${n}이다. AD=${a} cm, BC=${b} cm일 때, EF의 길이는 몇 cm인지 구하시오.`,answer:ef,
    explain:`대각선 AC와 EF의 교점을 G라 하면 EG=${b}×${m}÷${m+n}, GF=${a}×${n}÷${m+n}이므로 EF=${ef} cm입니다.`,concept:TR,d:4,
    wrongs:[W(Q(a+b,2),MC.avg,'midline'),W(Q(m*a+n*b,m+n),MC.inv,'swapped'),W(Q(b*m,m+n),MC.part,'EG-only'),W(Q(a*n,m+n)+a,MC.part,'GF-plus-AD')]}));}
// ── 무게중심 ─────────────────────────────────────────────
const MED=[['AD','A','D','BC'],['BE','B','E','AC'],['CF','C','F','AB']];
const cgStem=(med)=>`점 G가 △ABC의 무게중심이고 ${med[0]}가 중선이다.`;
for(let k=2;k<=40;k++){const med=MED[k%3];const [s,V,Pt]=med;
  add('cg2',amount({kind:'cg-vertex-part',params:{med:s,len:3*k},prompt:`${cgStem(med)} ${s}=${3*k} cm일 때, ${V}G의 길이는 몇 cm인지 구하시오.`,answer:2*k,
    explain:`무게중심은 중선을 꼭짓점으로부터 2:1로 나누므로 ${V}G=${3*k}×{frac:2/3}=${2*k} cm입니다.`,concept:CG,d:2,
    wrongs:[W(k,MC.ratio,'reversed'),W(Q(3*k,2),MC.cmid,'midpoint'),W(Q(3*k,4),MC.ratio,'half-of-half'),W(6*k,MC.ratio,'doubled-median')]}));
  if(k%2===0)add('cg2',amount({kind:'cg-side-part',params:{med:s,len:3*k},prompt:`${cgStem(med)} ${s}=${3*k} cm일 때, G${Pt}의 길이는 몇 cm인지 구하시오.`,answer:k,
    explain:`G${Pt}=${s}×{frac:1/3}=${3*k}÷3=${k} cm입니다.`,concept:CG,d:2,wrongs:[W(2*k,MC.ratio,'reversed'),W(3*k/2,MC.cmid,'midpoint'),W(Q(3*k,4),MC.ratio,'quarter')]}));
  add('cg2',amount({kind:'cg-from-vertex-part',params:{med:s,vg:2*k},prompt:`${cgStem(med)} ${V}G=${2*k} cm일 때, G${Pt}의 길이는 몇 cm인지 구하시오.`,answer:k,
    explain:`${V}G:G${Pt}=2:1이므로 G${Pt}=${2*k}÷2=${k} cm입니다.`,concept:CG,d:2,wrongs:[W(4*k,MC.ratio,'reversed'),W(2*k,MC.cmid,'equal'),W(3*k,MC.whole,'median-for-part')]}));
  add('cg2',amount({kind:'cg-median-from-vertex',params:{med:s,vg:2*k},prompt:`${cgStem(med)} ${V}G=${2*k} cm일 때, ${s}의 길이는 몇 cm인지 구하시오.`,answer:3*k,
    explain:`G${Pt}=${2*k}÷2=${k} cm이므로 ${s}=${2*k}+${k}=${3*k} cm입니다.`,concept:CG,d:2,wrongs:[W(6*k,MC.ratio,'reversed'),W(4*k,MC.cmid,'midpoint'),W(Q(4*k,3),MC.ratio,'two-thirds-applied-wrong-way')]}));}
for(let j=2;j<=20;j++)for(let k=3;k<=24;k++){if(3*j===k||(j*5+k*3)%7)continue;const med=MED[(j+k)%3];const [s,V,Pt,side]=med;const end=side[(j+k)%2];
  add('cg1',amount({kind:'cg-midpoint',params:{med:s,len:3*j,side:2*k},prompt:`${cgStem(med)} ${s}=${3*j} cm, ${side}=${2*k} cm일 때, ${end}${Pt}의 길이는 몇 cm인지 구하시오.`,answer:k,
    explain:`중선 ${s}의 점 ${Pt}는 변 ${side}의 중점이므로 ${end}${Pt}=${2*k}÷2=${k} cm입니다.`,concept:CG,d:1,
    wrongs:[W(2*k,MC.whole,'whole-side'),W(2*j,MC.side,'VG-for-half-side'),W(j,MC.side,'GP-for-half-side'),W(Q(2*k,3),MC.ratio,'one-third')]}));}
for(let a=2;a<=20;a++)for(let b=a+1;b<=24;b++){if((a+b)%2)continue;
  add('cg4',amount({kind:'cg-two-medians',params:{AD:3*a,BE:3*b},prompt:`△ABC의 두 중선 AD, BE의 교점을 G라고 하자. AD=${3*a} cm, BE=${3*b} cm일 때, AG+BG의 길이는 몇 cm인지 구하시오.`,answer:2*a+2*b,
    explain:`G는 무게중심이므로 AG=${3*a}×{frac:2/3}=${2*a}, BG=${3*b}×{frac:2/3}=${2*b}, 합은 ${2*a+2*b} cm입니다.`,concept:CG,d:4,
    wrongs:[W(a+b,MC.ratio,'reversed'),W(3*a+3*b,MC.whole,'whole-medians'),W(Q(3*a+3*b,2),MC.cmid,'midpoint')]}));
  add('cg4',amount({kind:'cg-two-medians-gd',params:{AD:3*a,BE:3*b},prompt:`△ABC의 두 중선 AD, BE의 교점을 G라고 하자. AD=${3*a} cm, BE=${3*b} cm일 때, GD+GE의 길이는 몇 cm인지 구하시오.`,answer:a+b,
    explain:`GD=${3*a}÷3=${a}, GE=${3*b}÷3=${b}이므로 합은 ${a+b} cm입니다.`,concept:CG,d:4,
    wrongs:[W(2*a+2*b,MC.ratio,'reversed'),W(Q(3*a+3*b,2),MC.cmid,'midpoint'),W(3*a+3*b,MC.whole,'whole-medians')]}));}
const cgPar='점 G가 △ABC의 무게중심이고, 점 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 하자.';
for(let k=2;k<=40;k+=2)add('cg3',amount({kind:'cg-parallel-ef',params:{BC:3*k},prompt:`${cgPar} BC=${3*k} cm일 때, EF의 길이는 몇 cm인지 구하시오.`,answer:2*k,
  explain:`AG:AD=2:3이므로 EF:BC=2:3, EF=${3*k}×{frac:2/3}=${2*k} cm입니다.`,concept:CG,d:3,wrongs:[W(k,MC.ratio,'one-third'),W(3*k/2,MC.mid,'midsegment'),W(3*k,MC.side,'EF=BC')]}));
for(let k=2;k<=30;k+=2)for(let j of [k+1,k+3,k+5]){if(j===k)continue;
  add('cg3',amount({kind:'cg-parallel-ae',params:{AB:3*k,BC:3*j},prompt:`${cgPar} AB=${3*k} cm, BC=${3*j} cm일 때, AE의 길이는 몇 cm인지 구하시오.`,answer:2*k,
    explain:`AE:AB=AG:AD=2:3이므로 AE=${3*k}×{frac:2/3}=${2*k} cm입니다.`,concept:CG,d:3,wrongs:[W(k,MC.ratio,'EB-for-AE'),W(3*k/2,MC.cmid,'midpoint'),W(2*j,MC.side,'EF-for-AE')]}));}
for(let k=2;k<=60;k++){const tri=['GBC','GCA','GAB'][k%3];
  add('cgArea',amount({kind:'cg-area-third',params:{S:3*k,tri},prompt:`넓이가 ${3*k} cm²인 △ABC의 무게중심을 G라고 할 때, △${tri}의 넓이는 몇 cm²인지 구하시오.`,answer:k,
    explain:`무게중심과 세 꼭짓점을 이으면 넓이가 같은 세 삼각형으로 나뉘므로 △${tri}=${3*k}÷3=${k} cm²입니다.`,concept:CGA,d:3,
    wrongs:[W(Q(3*k,2),MC.bisect,'half'),W(2*k,MC.ratio,'two-thirds'),W(Q(k,2),MC.six,'sixth')]}));
  add('cgArea',amount({kind:'cg-area-sixth',params:{S:6*k},prompt:`넓이가 ${6*k} cm²인 △ABC의 무게중심을 G, 변 BC의 중점을 D라고 할 때, △GBD의 넓이는 몇 cm²인지 구하시오.`,answer:k,
    explain:`세 중선은 △ABC를 넓이가 같은 여섯 삼각형으로 나누므로 △GBD=${6*k}÷6=${k} cm²입니다.`,concept:CGA,d:3,
    wrongs:[W(2*k,MC.six,'third'),W(3*k,MC.bisect,'half'),W(4*k,MC.ratio,'two-thirds')]}));
  if(k<=40){const t2=['GAB','GBC','GCA'][k%3];add('cgArea',amount({kind:'cg-area-whole',params:{s:k,tri:t2},prompt:`△ABC의 무게중심을 G라고 하자. △${t2}의 넓이가 ${k} cm²일 때, △ABC의 넓이는 몇 cm²인지 구하시오.`,answer:3*k,
    explain:`△GAB=△GBC=△GCA이므로 △ABC=3×${k}=${3*k} cm²입니다.`,concept:CGA,d:3,wrongs:[W(2*k,MC.bisect,'half'),W(6*k,MC.six,'six-parts'),W(Q(3*k,2),MC.ratio,'two-thirds')]}));}}
for(let k=1;k<=24;k++){const S=9*k;
  add('cgPar4',amount({kind:'cg-parallel-area',params:{S},prompt:`넓이가 ${S} cm²인 △ABC의 무게중심 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 할 때, △AEF의 넓이는 몇 cm²인지 구하시오.`,answer:4*k,
    explain:`AE:AB=2:3이므로 넓이의 비는 2²:3²=4:9, △AEF=${S}×4÷9=${4*k} cm²입니다(넓이를 이등분하지 않습니다).`,concept:CGA,d:4,
    wrongs:[W(Q(S,2),MC.bisect,'half'),W(6*k,MC.sq,'linear'),W(5*k,MC.part,'trapezoid-for-triangle'),W(3*k,MC.ratio,'one-third')]}));
  add('cgPar4',amount({kind:'cg-parallel-trap',params:{S},prompt:`넓이가 ${S} cm²인 △ABC의 무게중심 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 할 때, □EBCF의 넓이는 몇 cm²인지 구하시오.`,answer:5*k,
    explain:`△AEF=${S}×{frac:4/9}=${4*k} cm²이므로 □EBCF=${S}−${4*k}=${5*k} cm²입니다.`,concept:CGA,d:4,
    wrongs:[W(Q(S,2),MC.bisect,'half'),W(3*k,MC.sq,'linear'),W(4*k,MC.part,'triangle-for-trapezoid')]}));}

// ── 비(가장 간단한 자연수의 비)와 분수 ─────────────────────────────
const rw=(n,d,tag,rule)=>W({num:n,den:d},tag,rule);
for(let a=2;a<=15;a++)for(let b=1;b<=15;b++){if(a===b||(a*b)%3===1)continue;const g=gcd(a,a+b);
  add('fDE',fraction({kind:'f-de-bc',params:{AD:a,DB:b},as:'ratio',prompt:`${deStem} AD=${a} cm, DB=${b} cm일 때, DE:BC를 ${RATIO}`,num:a/g,den:(a+b)/g,
    explain:`△ADE∽△ABC이고 AD:AB=${a}:${a+b}이므로 DE:BC=${a/g}:${(a+b)/g}입니다.`,concept:TP,d:3,
    wrongs:[rw(a,b,MC.part,'AD:DB'),rw(b,a+b,MC.whole,'DB:AB'),rw(a+b,a,MC.inv,'inverted'),rw(1,2,MC.mid,'midsegment')]}));}
for(let m=1;m<=6;m++)for(let n=1;n<=6;n++){if(gcd(m,n)!==1||m===n)continue;const d=(m+n)**2;
  add('fArea',fraction({kind:'f-ade-area',params:{m,n},as:'ratio',prompt:`${deStem} AD:DB=${m}:${n}일 때, △ADE와 △ABC의 넓이의 비를 ${RATIO}`,num:m*m,den:d,
    explain:`닮음비 AD:AB=${m}:${m+n}이므로 넓이의 비는 ${m}²:${m+n}²=${m*m}:${d}입니다.`,concept:TP,d:4,
    wrongs:[rw(m,m+n,MC.sq,'linear'),rw(m*m,n*n,MC.part,'AD:DB-squared'),rw(m,n,MC.part,'AD:DB'),rw(d,m*m,MC.inv,'inverted')]}));
  const r=d-m*m,g=gcd(r,d);
  add('fArea',fraction({kind:'f-trap-area',params:{m,n},prompt:`${deStem} AD:DB=${m}:${n}일 때, □DBCE의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오.`,num:r/g,den:d/g,
    explain:`△ADE는 △ABC의 {frac:${m*m}/${d}}배이므로 □DBCE는 1−{frac:${m*m}/${d}}={frac:${r/g}/${d/g}}배입니다.`,concept:TP,d:4,
    wrongs:[rw(n,m+n,MC.sq,'linear'),rw(m*m,d,MC.part,'triangle-for-trapezoid'),rw(n*n,d,MC.sq,'DB-squared'),rw(1,2,MC.bisect,'half')]}));}
for(const [p,q] of RAT)for(let u=2;u<=6;u++){if((u+p+q)%2)continue;
  add('fPL',fraction({kind:'f-pl-ratio',params:{AB:p*u,BC:q*u},as:'ratio',prompt:`${plStem} AB=${p*u} cm, BC=${q*u} cm일 때, DE:EF를 ${RATIO}`,num:p,den:q,
    explain:`평행선 사이의 선분의 길이의 비는 같으므로 DE:EF=AB:BC=${p}:${q}입니다.`,concept:PL,d:3,
    wrongs:[rw(q,p,MC.inv,'inverted'),rw(p,p+q,MC.whole,'AB:AC'),rw(q,p+q,MC.whole,'BC:AC'),rw(1,1,MC.side,'equal-segments')]}));}
const CGF=[
  [`${cgStem(MED[0])} AG:GD를 ${RATIO}`,2,1,'ratio','무게중심은 중선을 꼭짓점으로부터 2:1로 나누므로 AG:GD=2:1입니다.',CG,3,[[1,2,MC.ratio],[1,1,MC.cmid],[3,1,MC.whole]]],
  [`${cgStem(MED[1])} GE:BE를 ${RATIO}`,1,3,'ratio','BG:GE=2:1이므로 GE:BE=1:3입니다.',CG,3,[[2,3,MC.ratio],[1,2,MC.cmid],[1,1,MC.whole]]],
  [`${cgStem(MED[2])} CG:CF를 ${RATIO}`,2,3,'ratio','CG:GF=2:1이므로 CG:CF=2:3입니다.',CG,3,[[1,3,MC.ratio],[1,2,MC.cmid],[2,1,MC.whole]]],
  [`${cgStem(MED[0])} GD의 길이는 AD의 길이의 몇 배인지 기약분수로 구하시오.`,1,3,'frac','GD=AD×{frac:1/3}이므로 {frac:1/3}배입니다.',CG,3,[[2,3,MC.ratio],[1,2,MC.cmid],[1,4,MC.whole]]],
  [`${cgStem(MED[2])} GF의 길이는 CG의 길이의 몇 배인지 기약분수로 구하시오.`,1,2,'frac','CG:GF=2:1이므로 GF는 CG의 {frac:1/2}배입니다.',CG,3,[[2,1,MC.ratio],[1,3,MC.whole],[2,3,MC.cmid]]],
  [`${cgPar} EF:BC를 ${RATIO}`,2,3,'ratio','EF:BC=AG:AD=2:3입니다.',CG,3,[[1,2,MC.mid],[1,3,MC.ratio],[4,9,MC.sq]]],
  [`${cgPar} △AEF와 △ABC의 넓이의 비를 ${RATIO}`,4,9,'ratio','닮음비 2:3이므로 넓이의 비는 2²:3²=4:9입니다(넓이의 이등분이 아닙니다).',CGA,4,[[2,3,MC.sq],[1,2,MC.bisect],[4,5,MC.part]]],
  [`${cgPar} △AEF와 □EBCF의 넓이의 비를 ${RATIO}`,4,5,'ratio','△AEF:△ABC=4:9이므로 △AEF:□EBCF=4:(9−4)=4:5입니다.',CGA,4,[[4,9,MC.part],[1,1,MC.bisect],[2,1,MC.sq]]],
  [`점 G가 △ABC의 무게중심이고 D가 변 BC의 중점일 때, △GBD의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오.`,1,6,'frac','세 중선이 넓이가 같은 여섯 삼각형을 만들므로 {frac:1/6}배입니다.',CGA,4,[[1,3,MC.six],[1,2,MC.bisect],[1,9,MC.sq]]],
  [`${cgPar} □EBCF의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오.`,5,9,'frac','△AEF가 {frac:4/9}배이므로 □EBCF는 {frac:5/9}배입니다.',CGA,4,[[1,3,MC.sq],[1,2,MC.bisect],[4,9,MC.part]]]];
CGF.forEach(([prompt,n,d,as,explain,concept,dd,ws],i)=>add('fCG',fraction({kind:'f-centroid',params:{i},as,prompt,num:n,den:d,explain,concept,d:dd,wrongs:ws.map(([a,b,tag])=>rw(a,b,tag,tag.slice(T.length)))})));

// ── 선택형 ──────────────────────────────────────────────────
const PT=['AD:DB=AE:EC','AD:AB=AE:AC','AD:AB=DE:BC','AE:AC=DE:BC','DB:AB=EC:AC'],PF=['AD:DB=DE:BC','AD:AB=AE:EC','AE:EC=DE:BC','DB:AB=DE:BC','AD:DB=AE:AC'];
for(let i=0;i<PT.length;i++)for(let j=0;j<2;j++){const ws=[0,1,2].map(t=>PF[(i+j*2+t)%PF.length]);
  add('cProp',choice({kind:'prop-true',params:{t:PT[i],f:ws},style:'labels',stem:`${deStem.replace('이다.','일 때,')} 옳은 비례식을 고르시오.`,correct:PT[i],wrongs:ws.map((w,k)=>({text:w,tag:k%2?MC.part:MC.add})),explain:L=>`${L}. ${PT[i]}가 성립합니다. 부분과 전체를 섞은 비례식에 주의합니다.`,concept:TP,d:2}));}
for(let i=0;i<PF.length;i++)for(let j=0;j<2;j++){const ws=[0,1,2].map(t=>PT[(i+j+2*t)%PT.length]);if(new Set(ws).size<3)continue;
  add('cProp',choice({kind:'prop-false',params:{f:PF[i],t:ws},style:'labels',stem:`${deStem.replace('이다.','일 때,')} 옳지 않은 비례식을 고르시오.`,correct:PF[i],wrongs:ws.map((w,k)=>({text:w,tag:k%2?MC.part:MC.add})),explain:L=>`${L}. ${PF[i]}는 성립하지 않습니다.`,concept:TP,d:2}));}
const cgT=[['삼각형의 세 중선의 교점을 무엇이라고 하는지 고르시오.','무게중심',[['외심',MC.mid],['내심',MC.mid],['중점',MC.cmid]],'세 중선의 교점은 무게중심입니다.'],
  [`${cgStem(MED[0])} AG:GD를 고르시오.`,'2:1',[['1:2',MC.ratio],['1:1',MC.cmid],['3:1',MC.ratio]],'무게중심은 중선을 꼭짓점으로부터 2:1로 나눕니다.'],
  [`${cgStem(MED[1])} GE:BG를 고르시오.`,'1:2',[['2:1',MC.ratio],['1:1',MC.cmid],['1:3',MC.ratio]],'BG:GE=2:1이므로 GE:BG=1:2입니다.'],
  [`${cgStem(MED[2])} CG:CF를 고르시오.`,'2:3',[['1:3',MC.ratio],['1:2',MC.cmid],['3:2',MC.ratio]],'CG:GF=2:1이므로 CG:CF=2:3입니다.'],
  [`${cgStem(MED[0])} GD:AD를 고르시오.`,'1:3',[['1:2',MC.cmid],['2:3',MC.ratio],['2:1',MC.ratio]],'GD는 AD의 {frac:1/3}이므로 1:3입니다.']];
for(const [stem,c,ws,ex] of cgT)add('cCG',choice({kind:'cg-term',params:{stem},stem,correct:c,wrongs:ws.map(([text,tag])=>({text,tag})),explain:ex,concept:CG,d:1}));
for(const V of ['A','B','C']){const others=['A','B','C'].filter(x=>x!==V);const opp=others.join('');
  const lines=[`직선 ${V}G`,`점 G를 지나고 변 ${opp}에 평행한 직선`,`점 G를 지나고 변 ${[V,others[0]].sort().join('')}에 평행한 직선`,`점 G를 지나고 변 ${[V,others[1]].sort().join('')}에 평행한 직선`];
  add('cBis',choice({kind:'cg-bisect',params:{V},style:'labels',stem:'점 G가 △ABC의 무게중심일 때, △ABC의 넓이를 항상 이등분하는 직선을 고르시오.',correct:lines[0],wrongs:lines.slice(1).map((t,k)=>({text:t,tag:k?MC.bisect:MC.sq})),explain:L=>`${L}. 직선 ${V}G는 중선이므로 넓이를 이등분합니다. G를 지나고 한 변에 평행한 직선은 넓이를 4:5로 나눕니다.`,concept:CGA,d:3}));}
const judge=[];for(const [p,q] of RAT)for(const u of [1,2,3])for(const v of [1,2,3,4]){const ad=p*u,db=q*u,ae=p*v,ec=q*v;if(ad<2||ae<2||u===v||ec>30||db>30)continue;judge.push([ad,db,ae,ec]);}
const fmtJ=([a,b,c,d])=>`${a}, ${b}, ${c}, ${d}`;
judge.forEach((t,i)=>{if(i%3)return;const [a,b,c,d]=t;const ws=[[a,b,c+1,d+1],[a,b+a,c,d],[a+1,b+1,c,d]].map(x=>x.map(v=>Math.max(1,v)));
  if(ws.some(w=>w[0]*w[3]===w[1]*w[2]))return;
  add('cJudge',choice({kind:'de-parallel-judge',params:{t,ws},style:'labels',stem:'△ABC에서 변 AB, AC 위의 점 D, E에 대하여 AD, DB, AE, EC의 길이가 차례로 다음과 같을 때, DE∥BC인 것을 고르시오.',tail:'(단, 단위는 cm이다.)',correct:fmtJ(t),wrongs:ws.map((w,k)=>({text:fmtJ(w),tag:k===1?MC.part:MC.add})),
    explain:L=>`${L}에서 AD:DB=AE:EC=${a/gcd(a,b)}:${b/gcd(a,b)}이므로 DE∥BC입니다.`,concept:TP,d:3}));});

const intro=amount({kind:'mid-mn',params:{a:8,b:10,c:12},prompt:`${midStem} AB=8 cm, BC=10 cm, CA=12 cm일 때, MN의 길이는 몇 cm인지 구하시오.`,answer:5,
  explain:'MN∥BC이고 MN={frac:1/2}BC이므로 MN=10÷2=5 cm입니다.',concept:MID,d:1,wrongs:[W(10,MC.mid,'equal'),W(4,MC.side,'half-AB'),W(20,MC.mid,'double'),W(6,MC.side,'half-CA')]});
const P=pools;
if(process.env.POOLS){console.log(Object.fromEntries(Object.entries(P).map(([k,v])=>[k,v.filter(Boolean).length+'/'+v.length])));console.log(drops);}
writeGeoPack({id:'m2s2-u4',title:'평행선과 선분의 길이의 비',standards:['[9수03-14]'],intro,groups:[
  [P.tp1,44],[P.mid1,24],[P.cg1,16],[P.cCG,5],
  [P.tp2,26],[P.mid2,26],[P.pl,20],[P.cg2,20],[P.cProp,14],
  [P.tp3,16],[P.mid3,12],[P.trMid,14],[P.cg3,14],[P.cgArea,18],[P.fDE,10],[P.fPL,8],[P.cBis,3],[P.cJudge,10],
  [P.trPQ,18],[P.trEF,22],[P.cg4,18],[P.cgPar4,18],[P.fArea,16],[P.fCG,10]]});
