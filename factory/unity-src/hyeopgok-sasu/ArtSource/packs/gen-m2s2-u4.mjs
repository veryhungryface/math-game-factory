#!/usr/bin/env node
// m2s2-u4 평행선과 선분의 길이의 비 ([9수03-14]) — 삼각형과 평행선, 두 변의 중점을 연결한 선분,
// 평행선 사이의 선분의 비, 사다리꼴, 삼각형의 무게중심(중선을 꼭짓점으로부터 2:1).
import {amount,fraction,choice,W,gcd,writeGeoPack} from './geo-kit.mjs';
const T='m2s2-u4.';
const MC={part:T+'part-whole-proportion-mixed',bisect:T+'centroid-line-bisects-area',ratio:T+'centroid-ratio-reversed',mid:T+'midsegment-length-wrong',add:T+'additive-reasoning',sq:T+'area-ratio-not-squared'};
const pools={};const add=(k,it)=>(pools[k]??=[]).push(it);
const RAT=[];for(let p=1;p<=7;p++)for(let q=1;q<=7;q++)if(p!==q&&gcd(p,q)===1)RAT.push([p,q]);
const TP='삼각형에서 평행선과 선분의 길이의 비',MID='삼각형의 두 변의 중점을 연결한 선분의 성질',PL='평행선 사이의 선분의 길이의 비',TR='사다리꼴에서 평행선과 선분의 길이의 비',CG='삼각형의 무게중심',CGA='무게중심과 넓이';
const deStem='△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다.';

// ── 삼각형과 평행선 ─────────────────────────────────────────
for(const [p,q] of RAT)for(let u=1;u<=8;u++)for(let v=1;v<=9;v++){const a=p*u,b=q*u,c=p*v,e=q*v;if(a<2||b<2||c<2||a===c||e>59||e<2||c>59||(u+v)%2)continue;
  add('tp1',amount({kind:'de-ec',params:{AD:a,DB:b,AE:c},prompt:`${deStem} AD=${a} cm, DB=${b} cm, AE=${c} cm일 때, EC의 길이는 몇 cm인지 구하시오.`,answer:e,
    explain:`AD:DB=AE:EC이므로 ${a}:${b}=${c}:EC, EC=${b}×${c}÷${a}=${e} cm입니다.`,concept:TP,d:1,wrongs:[W(c+b-a>0?c+b-a:0,MC.add,'additive'),W(a*c%b?0:a*c/b,MC.part,'inverted')]}));
  add('tp1',amount({kind:'de-ae',params:{AD:a,DB:b,EC:e},prompt:`${deStem} AD=${a} cm, DB=${b} cm, EC=${e} cm일 때, AE의 길이는 몇 cm인지 구하시오.`,answer:c,
    explain:`AD:DB=AE:EC이므로 ${a}:${b}=AE:${e}, AE=${a}×${e}÷${b}=${c} cm입니다.`,concept:TP,d:1,wrongs:[W(e+a-b>0?e+a-b:0,MC.add,'additive'),W(b*e%a?0:b*e/a,MC.part,'inverted')]}));}
for(const [p,q] of RAT){if(p>=q)continue;for(let u=1;u<=8;u++)for(let v=1;v<=9;v++){const ad=p*u,ab=q*u,ac=q*v,ae=p*v;if(ad<2||ae<2||ae>59||(u*3+v)%3)continue;
  add('tp2',amount({kind:'de-ae-whole',params:{AD:ad,AB:ab,AC:ac},prompt:`${deStem} AD=${ad} cm, AB=${ab} cm, AC=${ac} cm일 때, AE의 길이는 몇 cm인지 구하시오.`,answer:ae,
    explain:`AD:AB=AE:AC이므로 ${ad}:${ab}=AE:${ac}, AE=${ad}×${ac}÷${ab}=${ae} cm입니다.`,concept:TP,d:2,wrongs:[W(ad*ac%(ab-ad)?0:ad*ac/(ab-ad),MC.part,'part-part'),W(ac-(ab-ad),MC.add,'additive')]}));
  for(const c of [q*v]){const de=p*v;if(de<2||de>59)continue;
    add('tp2',amount({kind:'de-length',params:{AD:ad,AB:ab,BC:c},prompt:`${deStem} AD=${ad} cm, AB=${ab} cm, BC=${c} cm일 때, DE의 길이는 몇 cm인지 구하시오.`,answer:de,
      explain:`△ADE∽△ABC (AA 닮음)이므로 AD:AB=DE:BC, DE=${ad}×${c}÷${ab}=${de} cm입니다.`,concept:TP,d:2,wrongs:[W(ad*c%(ab-ad)?0:ad*c/(ab-ad),MC.part,'AD:DB=DE:BC'),W(c-(ab-ad),MC.add,'additive')]}));
    const db=ab-ad;if(db>=1&&(u+v)%2)add('tp2',amount({kind:'de-length-part',params:{AD:ad,DB:db,BC:c},prompt:`${deStem} AD=${ad} cm, DB=${db} cm, BC=${c} cm일 때, DE의 길이는 몇 cm인지 구하시오.`,answer:de,
      explain:`AD:AB=${ad}:${ab}이므로 DE=${c}×${ad}÷${ab}=${de} cm입니다(AD:DB가 아니라 AD:AB를 씁니다).`,concept:TP,d:2,wrongs:[W(ad*c%db?0:ad*c/db,MC.part,'AD:DB=DE:BC')]}));}}}
const extStem='변 BA의 연장선 위의 점 D와 변 CA의 연장선 위의 점 E에 대하여 DE∥BC이다.';
for(const [p,q] of RAT)for(let u=1;u<=6;u++)for(let v=1;v<=9;v++){const ad=p*u,ab=q*u,ac=q*v,ae=p*v;if(ad<2||ae<2||ae>59||ac>60||(u+2*v)%3)continue;
  add('tp3',amount({kind:'ext-ae',params:{AD:ad,AB:ab,AC:ac},prompt:`△ABC에서 ${extStem} AD=${ad} cm, AB=${ab} cm, AC=${ac} cm일 때, AE의 길이는 몇 cm인지 구하시오.`,answer:ae,
    explain:`△ADE∽△ABC (AA 닮음, 맞꼭지각과 엇각)이므로 AD:AB=AE:AC, AE=${ad}×${ac}÷${ab}=${ae} cm입니다.`,concept:TP,d:3,wrongs:[W(ac+ad-ab>0?ac+ad-ab:0,MC.add,'additive')]}));
  const de=ae,bc=ac;if((u+v)%2)continue;
  add('tp3',amount({kind:'ext-de',params:{AD:ad,AB:ab,BC:bc},prompt:`△ABC에서 ${extStem} AD=${ad} cm, AB=${ab} cm, BC=${bc} cm일 때, DE의 길이는 몇 cm인지 구하시오.`,answer:de,
    explain:`△ADE∽△ABC이므로 AD:AB=DE:BC, DE=${ad}×${bc}÷${ab}=${de} cm입니다.`,concept:TP,d:3,wrongs:[W(bc*ab%ad?0:bc*ab/ad,MC.part,'inverted')]}));}
// ── 두 변의 중점을 연결한 선분 ──────────────────────────────────
const midStem='△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자.';
for(let k=2;k<=29;k++){
  add('mid1',amount({kind:'mid-mn',params:{BC:2*k},prompt:`${midStem} BC=${2*k} cm일 때, MN의 길이는 몇 cm인지 구하시오.`,answer:k,explain:`MN∥BC이고 MN={frac:1/2}BC이므로 MN=${2*k}÷2=${k} cm입니다.`,concept:MID,d:1,wrongs:[W(2*k,MC.mid,'equal'),W(4*k>59?0:4*k,MC.mid,'double')]}));
  add('mid1',amount({kind:'mid-bc',params:{MN:k},prompt:`${midStem} MN=${k} cm일 때, BC의 길이는 몇 cm인지 구하시오.`,answer:2*k,explain:`BC=2MN=2×${k}=${2*k} cm입니다.`,concept:MID,d:1,wrongs:[W(k,MC.mid,'equal')]}));
  add('mid2',amount({kind:'mid-converse-nc',params:{AC:2*k},prompt:`△ABC에서 변 AB의 중점 M을 지나고 변 BC에 평행한 직선이 변 AC와 만나는 점을 N이라고 하자. AC=${2*k} cm일 때, NC의 길이는 몇 cm인지 구하시오.`,answer:k,
    explain:`중점 M을 지나고 BC에 평행한 직선은 AC의 중점을 지나므로 NC=${2*k}÷2=${k} cm입니다.`,concept:MID,d:2,wrongs:[W(2*k,MC.mid,'whole')]}));
  add('mid2',amount({kind:'mid-converse-mn',params:{BC:2*k},prompt:`△ABC에서 변 AB의 중점 M을 지나고 변 BC에 평행한 직선이 변 AC와 만나는 점을 N이라고 하자. BC=${2*k} cm일 때, MN의 길이는 몇 cm인지 구하시오.`,answer:k,
    explain:`점 N은 AC의 중점이므로 MN={frac:1/2}BC=${k} cm입니다.`,concept:MID,d:2,wrongs:[W(2*k,MC.mid,'equal')]}));}
for(let a=4;a<=40;a++)for(let b=a;b<=40;b++)for(let c=b;c<a+b&&c<=44;c++){if((a+b+c)%2||(a+b+c)/2>59||(a*3+b*5+c)%7)continue;const h=(a+b+c)/2;
  add('mid2',amount({kind:(a+b)%2?'mid-def-perimeter':'mid-amn-perimeter',params:{a,b,c},prompt:(a+b)%2?`△ABC의 세 변 AB, BC, CA의 중점을 각각 D, E, F라고 하자. AB=${a} cm, BC=${b} cm, CA=${c} cm일 때, △DEF의 둘레의 길이는 몇 cm인지 구하시오.`:`${midStem} AB=${a} cm, BC=${b} cm, CA=${c} cm일 때, △AMN의 둘레의 길이는 몇 cm인지 구하시오.`,answer:h,
    explain:(a+b)%2?`△DEF의 각 변은 마주 보는 변의 {frac:1/2}이므로 둘레는 (${a}+${b}+${c})÷2=${h} cm입니다.`:`AM=${a}÷2, AN=${c}÷2, MN=${b}÷2이므로 둘레는 (${a}+${b}+${c})÷2=${h} cm입니다.`,concept:MID,d:2,wrongs:[W(a+b+c>59?0:a+b+c,MC.mid,'whole')]}));}
for(let x=20;x<=59;x++){add('mid2',amount({kind:x%2?'mid-angle-b':'mid-angle-c',params:{x},prompt:x%2?`${midStem} ∠B=${x}°일 때, ∠AMN의 크기는 몇 도인지 구하시오.`:`${midStem} ∠C=${x}°일 때, ∠ANM의 크기는 몇 도인지 구하시오.`,answer:x,
  explain:`MN∥BC이므로 동위각으로 ${x%2?'∠AMN=∠B':'∠ANM=∠C'}=${x}°입니다.`,concept:MID,d:2,wrongs:[W(180-x>59?0:180-x,MC.mid,'supplement'),W(90-x>0?90-x:0,MC.mid,'complement')]}));}
for(let x=40;x<=100;x+=5)for(let y=30;y<=100;y+=5){const z=180-x-y;if(z<10||z>59)continue;
  add('mid3',amount({kind:'mid-angle-sum',params:{A:x,B:y},prompt:`${midStem} ∠A=${x}°, ∠B=${y}°일 때, ∠ANM의 크기는 몇 도인지 구하시오.`,answer:z,
    explain:`MN∥BC이므로 ∠ANM=∠C=180°−${x}°−${y}°=${z}°입니다.`,concept:MID,d:3,wrongs:[W(y>59?0:y,MC.mid,'wrong-corresponding')]}));}
// ── 평행선 사이의 선분 ─────────────────────────────────────────
const plStem='서로 평행한 세 직선 l, m, n이 직선 p와 만나는 점을 각각 A, B, C, 직선 q와 만나는 점을 각각 D, E, F라고 하자.';
for(const [p,q] of RAT)for(let u=1;u<=8;u++)for(let v=1;v<=8;v++){const ab=p*u,bc=q*u,de=p*v,ef=q*v;if(ab<2||de<2||ef>59||ef<2||ab===de||(u*v+p)%3)continue;
  add('pl',amount({kind:'pl-ef',params:{AB:ab,BC:bc,DE:de},prompt:`${plStem} AB=${ab} cm, BC=${bc} cm, DE=${de} cm일 때, EF의 길이는 몇 cm인지 구하시오.`,answer:ef,
    explain:`평행선 사이의 선분의 길이의 비는 같으므로 AB:BC=DE:EF, EF=${bc}×${de}÷${ab}=${ef} cm입니다.`,concept:PL,d:2,wrongs:[W(de+bc-ab>0?de+bc-ab:0,MC.add,'additive'),W(ab*de%bc?0:ab*de/bc,MC.part,'inverted')]}));
  const ac=ab+bc,df=de+ef;if(df<=59&&(u+v)%2)add('pl',amount({kind:'pl-df',params:{AB:ab,AC:ac,DE:de},prompt:`${plStem} AB=${ab} cm, AC=${ac} cm, DE=${de} cm일 때, DF의 길이는 몇 cm인지 구하시오.`,answer:df,
    explain:`AB:AC=DE:DF이므로 DF=${ac}×${de}÷${ab}=${df} cm입니다.`,concept:PL,d:2,wrongs:[W(de+ac-ab,MC.add,'additive')]}));}
// ── 사다리꼴 ──────────────────────────────────────────────
const trStem='AD∥BC인 사다리꼴 ABCD에서 두 변 AB, DC의 중점을 각각 M, N이라고 하자.';
for(let a=2;a<=50;a++)for(let b=a+2;b<=70;b+=2){const mn=(a+b)/2;if(mn>59||(a+b)%4===2&&a%3)continue;
  add('trMid',amount({kind:'trap-mn',params:{AD:a,BC:b},prompt:`${trStem} AD=${a} cm, BC=${b} cm일 때, MN의 길이는 몇 cm인지 구하시오.`,answer:mn,
    explain:`MN=(AD+BC)÷2=(${a}+${b})÷2=${mn} cm입니다.`,concept:TR,d:3,wrongs:[W(a+b>59?0:a+b,MC.mid,'no-halving'),W((b-a)/2>=2?(b-a)/2:0,MC.mid,'difference')]}));
  const pq=(b-a)/2;if(pq>=2&&pq<=59&&b-a>=4&&a%2===0)add('trPQ',amount({kind:'trap-pq',params:{AD:a,BC:b},prompt:`${trStem} MN이 두 대각선 BD, AC와 만나는 점을 각각 P, Q라고 하자. AD=${a} cm, BC=${b} cm일 때, PQ의 길이는 몇 cm인지 구하시오.`,answer:pq,
    explain:`MQ={frac:1/2}BC=${b/2} cm, MP={frac:1/2}AD=${a/2} cm이므로 PQ=${b/2}−${a/2}=${pq} cm입니다.`,concept:TR,d:4,wrongs:[W(mn,MC.mid,'midline'),W(b-a>59?0:b-a,MC.mid,'no-halving')]}));
  if(b/2<=59&&a%2===0&&(a+b)%3===0)add('trMid',amount({kind:'trap-mq',params:{AD:a,BC:b},prompt:`${trStem} MN이 대각선 AC와 만나는 점을 Q라고 하자. AD=${a} cm, BC=${b} cm일 때, MQ의 길이는 몇 cm인지 구하시오.`,answer:b/2,
    explain:`△ABC에서 M은 AB의 중점이고 MQ∥BC이므로 MQ={frac:1/2}BC=${b/2} cm입니다.`,concept:TR,d:3,wrongs:[W(a/2>=2?a/2:0,MC.mid,'wrong-triangle'),W(mn,MC.mid,'midline')]}));}
for(const [m,n] of [[1,2],[2,1],[1,3],[3,1],[2,3],[3,2],[1,4],[3,4],[4,3],[2,5],[5,2],[1,5]])for(let a=2;a<=40;a++)for(let b=a+1;b<=60;b++){
  const num=n*a+m*b;if(num%(m+n))continue;const ef=num/(m+n);if(ef>59||ef<2||(a+b+m)%5)continue;
  add('trEF',amount({kind:'trap-ef',params:{m,n,AD:a,BC:b},prompt:`AD∥EF∥BC인 사다리꼴 ABCD에서 점 E, F는 각각 변 AB, DC 위에 있고 AE:EB=${m}:${n}이다. AD=${a} cm, BC=${b} cm일 때, EF의 길이는 몇 cm인지 구하시오.`,answer:ef,
    explain:`대각선 AC와 EF의 교점을 G라 하면 EG=${b}×${m}÷${m+n}, GF=${a}×${n}÷${m+n}이므로 EF=${ef} cm입니다.`,concept:TR,d:4,wrongs:[W((a+b)%2?0:(a+b)/2,MC.mid,'midline'),W((m*a+n*b)%(m+n)?0:(m*a+n*b)/(m+n),MC.part,'swapped')]}));}
// ── 무게중심 ─────────────────────────────────────────────
const MED=[['AD','A','D','BC'],['BE','B','E','AC'],['CF','C','F','AB']];
const cgStem=(med)=>`점 G가 △ABC의 무게중심이고 ${med[0]}가 중선이다.`;
for(let k=2;k<=29;k++){const med=MED[k%3];const [s,V,Pt]=med;
  if(3*k<=150)add('cg2',amount({kind:'cg-vertex-part',params:{med:s,len:3*k},prompt:`${cgStem(med)} ${s}=${3*k} cm일 때, ${V}G의 길이는 몇 cm인지 구하시오.`,answer:2*k,
    explain:`무게중심은 중선을 꼭짓점으로부터 2:1로 나누므로 ${V}G=${3*k}×{frac:2/3}=${2*k} cm입니다.`,concept:CG,d:2,wrongs:[W(k,MC.ratio,'reversed'),W(3*k%2?0:3*k/2,MC.ratio,'midpoint')]}));
  if(k<=59)add('cg2',amount({kind:'cg-side-part',params:{med:s,len:3*k},prompt:`${cgStem(med)} ${s}=${3*k} cm일 때, G${Pt}의 길이는 몇 cm인지 구하시오.`,answer:k,
    explain:`G${Pt}=${s}×{frac:1/3}=${3*k}÷3=${k} cm입니다.`,concept:CG,d:2,wrongs:[W(2*k,MC.ratio,'reversed')]}));
  if(k<=29)add('cg2',amount({kind:'cg-from-vertex-part',params:{med:s,vg:2*k},prompt:`${cgStem(med)} ${V}G=${2*k} cm일 때, G${Pt}의 길이는 몇 cm인지 구하시오.`,answer:k,
    explain:`${V}G:G${Pt}=2:1이므로 G${Pt}=${2*k}÷2=${k} cm입니다.`,concept:CG,d:2,wrongs:[W(4*k>59?0:4*k,MC.ratio,'reversed'),W(2*k,MC.ratio,'equal')]}));
  if(3*k<=59)add('cg2',amount({kind:'cg-median',params:{med:s,gp:k},prompt:`${cgStem(med)} G${Pt}=${k} cm일 때, ${s}의 길이는 몇 cm인지 구하시오.`,answer:3*k,
    explain:`${V}G=2×${k}=${2*k} cm이므로 ${s}=${2*k}+${k}=${3*k} cm입니다.`,concept:CG,d:2,wrongs:[W(2*k,MC.ratio,'forgot-part'),W(k+k/2===Math.floor(k+k/2)?k+k/2:0,MC.ratio,'reversed')]}));
  add('cg1',amount({kind:'cg-midpoint',params:{med:s,side:med[3],len:2*k},prompt:`${cgStem(med)} ${med[3]}=${2*k} cm일 때, ${med[3][0]}${Pt}의 길이는 몇 cm인지 구하시오.`,answer:k,
    explain:`중선 ${s}의 점 ${Pt}는 변 ${med[3]}의 중점이므로 ${med[3][0]}${Pt}=${2*k}÷2=${k} cm입니다.`,concept:CG,d:1,wrongs:[W(Math.round(4*k/3),MC.ratio,'two-thirds')]}));}
for(let a=2;a<=15;a++)for(let b=a+1;b<=20;b++){if(2*a+2*b>59||(a+b)%2)continue;
  add('cg4',amount({kind:'cg-two-medians',params:{AD:3*a,BE:3*b},prompt:`△ABC의 두 중선 AD, BE의 교점을 G라고 하자. AD=${3*a} cm, BE=${3*b} cm일 때, AG+BG의 길이는 몇 cm인지 구하시오.`,answer:2*a+2*b,
    explain:`G는 무게중심이므로 AG=${3*a}×{frac:2/3}=${2*a}, BG=${3*b}×{frac:2/3}=${2*b}, 합은 ${2*a+2*b} cm입니다.`,concept:CG,d:4,wrongs:[W(a+b,MC.ratio,'reversed'),W(3*a+3*b>59?0:3*a+3*b,MC.ratio,'whole')]}));
  if(a+b<=59)add('cg4',amount({kind:'cg-two-medians-gd',params:{AD:3*a,BE:3*b},prompt:`△ABC의 두 중선 AD, BE의 교점을 G라고 하자. AD=${3*a} cm, BE=${3*b} cm일 때, GD+GE의 길이는 몇 cm인지 구하시오.`,answer:a+b,
    explain:`GD=${3*a}÷3=${a}, GE=${3*b}÷3=${b}이므로 합은 ${a+b} cm입니다.`,concept:CG,d:4,wrongs:[W(2*a+2*b>59?0:2*a+2*b,MC.ratio,'reversed')]}));}
const cgPar='점 G가 △ABC의 무게중심이고, 점 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 하자.';
for(let k=2;k<=29;k++){add('cg3',amount({kind:'cg-parallel-ef',params:{BC:3*k},prompt:`${cgPar} BC=${3*k} cm일 때, EF의 길이는 몇 cm인지 구하시오.`,answer:2*k,
  explain:`AG:AD=2:3이므로 EF:BC=2:3, EF=${3*k}×{frac:2/3}=${2*k} cm입니다.`,concept:CG,d:3,wrongs:[W(k,MC.ratio,'one-third'),W(3*k%2?0:3*k/2,MC.ratio,'midsegment')]}));
  if(k<=19)add('cg3',amount({kind:'cg-parallel-ae',params:{AB:3*k},prompt:`${cgPar} AB=${3*k} cm일 때, AE의 길이는 몇 cm인지 구하시오.`,answer:2*k,
    explain:`AE:AB=AG:AD=2:3이므로 AE=${3*k}×{frac:2/3}=${2*k} cm입니다.`,concept:CG,d:3,wrongs:[W(k,MC.ratio,'reversed')]}));}
for(let k=2;k<=59;k++){const tri=['GBC','GCA','GAB'][k%3];
  add('cgArea',amount({kind:'cg-area-third',params:{S:3*k,tri},prompt:`넓이가 ${3*k} cm²인 △ABC의 무게중심을 G라고 할 때, △${tri}의 넓이는 몇 cm²인지 구하시오.`,answer:k,
    explain:`무게중심과 세 꼭짓점을 이으면 넓이가 같은 세 삼각형으로 나뉘므로 △${tri}=${3*k}÷3=${k} cm²입니다.`,concept:CGA,d:3,wrongs:[W(3*k%2?0:3*k/2,MC.bisect,'half'),W(2*k,MC.ratio,'two-thirds')]}));
  if(k<=59)add('cgArea',amount({kind:'cg-area-sixth',params:{S:6*k},prompt:`넓이가 ${6*k} cm²인 △ABC의 무게중심을 G, 변 BC의 중점을 D라고 할 때, △GBD의 넓이는 몇 cm²인지 구하시오.`,answer:k,
    explain:`세 중선은 △ABC를 넓이가 같은 여섯 삼각형으로 나누므로 △GBD=${6*k}÷6=${k} cm²입니다.`,concept:CGA,d:3,wrongs:[W(2*k,MC.ratio,'third'),W(3*k>59?0:3*k,MC.bisect,'half')]}));
  if(3*k<=59)add('cgArea',amount({kind:'cg-area-whole',params:{s:k},prompt:`△ABC의 무게중심을 G라고 하자. △GAB의 넓이가 ${k} cm²일 때, △ABC의 넓이는 몇 cm²인지 구하시오.`,answer:3*k,
    explain:`△GAB=△GBC=△GCA이므로 △ABC=3×${k}=${3*k} cm²입니다.`,concept:CGA,d:3,wrongs:[W(2*k,MC.bisect,'half')]}));}
for(let k=1;k<=14;k++){const S=9*k;
  if(4*k>=2)add('cgPar4',amount({kind:'cg-parallel-area',params:{S},prompt:`넓이가 ${S} cm²인 △ABC의 무게중심 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 할 때, △AEF의 넓이는 몇 cm²인지 구하시오.`,answer:4*k,
    explain:`AE:AB=2:3이므로 넓이의 비는 2²:3²=4:9, △AEF=${S}×4÷9=${4*k} cm²입니다(넓이를 이등분하지 않습니다).`,concept:CGA,d:4,wrongs:[W(S%2?0:S/2,MC.bisect,'half'),W(6*k>59?0:6*k,MC.sq,'linear')]}));
  if(5*k<=59)add('cgPar4',amount({kind:'cg-parallel-trap',params:{S},prompt:`넓이가 ${S} cm²인 △ABC의 무게중심 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 할 때, □EBCF의 넓이는 몇 cm²인지 구하시오.`,answer:5*k,
    explain:`△AEF=${S}×{frac:4/9}=${4*k} cm²이므로 □EBCF=${S}−${4*k}=${5*k} cm²입니다.`,concept:CGA,d:4,wrongs:[W(S%2?0:S/2,MC.bisect,'half'),W(3*k,MC.sq,'linear')]}));}

// ── 분수(기약분수) ───────────────────────────────────────────
for(let a=2;a<=12;a++)for(let b=1;b<=12;b++){const g=gcd(a,a+b),n=a/g,d=(a+b)/g;if(d<2||d>60||a===b&&a>1)continue;if((a*b)%3===1)continue;
  add('fDE',fraction({kind:'f-de-bc',params:{AD:a,DB:b},prompt:`${deStem} AD=${a} cm, DB=${b} cm일 때, DE의 길이는 BC의 길이의 몇 배인지 기약분수로 구하시오.`,num:n,den:d,
    explain:`AD:AB=${a}:${a+b}이므로 DE는 BC의 {frac:${n}/${d}}배입니다.`,concept:TP,d:3,wrongs:[W({num:a/gcd(a,b),den:b/gcd(a,b)},MC.part,'AD:DB')]}));}
for(let m=1;m<=6;m++)for(let n=1;n<=6;n++){if(gcd(m,n)!==1||(m+n)**2>60)continue;const d=(m+n)**2;
  add('fArea',fraction({kind:'f-ade-area',params:{m,n},prompt:`${deStem} AD:DB=${m}:${n}일 때, △ADE의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오.`,num:m*m,den:d,
    explain:`닮음비 AD:AB=${m}:${m+n}이므로 넓이는 ${m}²:${m+n}²=${m*m}:${d}, {frac:${m*m}/${d}}배입니다.`,concept:TP,d:4,wrongs:[W({num:m,den:m+n},MC.sq,'linear'),W({num:m*m,den:n*n},MC.part,'AD:DB')]}));
  if(n*(2*m+n)<=60&&d<=60)add('fArea',fraction({kind:'f-trap-area',params:{m,n},prompt:`${deStem} AD:DB=${m}:${n}일 때, □DBCE의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오.`,num:(d-m*m)/gcd(d-m*m,d),den:d/gcd(d-m*m,d),
    explain:`△ADE는 △ABC의 {frac:${m*m}/${d}}배이므로 □DBCE는 1−{frac:${m*m}/${d}}={frac:${(d-m*m)/gcd(d-m*m,d)}/${d/gcd(d-m*m,d)}}배입니다.`,concept:TP,d:4,wrongs:[W({num:n,den:m+n},MC.sq,'linear')]}));}
for(const [p,q] of RAT)for(let u=1;u<=4;u++){if(p===1||(u+p+q)%2)continue;
  add('fPL',fraction({kind:'f-pl-ratio',params:{AB:p*u,BC:q*u},prompt:`${plStem} AB=${p*u} cm, BC=${q*u} cm일 때, EF의 길이는 DE의 길이의 몇 배인지 기약분수로 구하시오.`,num:q,den:p,
    explain:`AB:BC=DE:EF=${p}:${q}이므로 EF는 DE의 {frac:${q}/${p}}배입니다.`,concept:PL,d:3,wrongs:[W({num:p,den:q},MC.part,'inverted')]}));}
const CGF=[[`${cgStem(MED[0])} GD의 길이는 AD의 길이의 몇 배인지 기약분수로 구하시오.`,1,3,'GD=AD×{frac:1/3}이므로 {frac:1/3}배입니다.'],[`${cgStem(MED[1])} BG의 길이는 BE의 길이의 몇 배인지 기약분수로 구하시오.`,2,3,'BG:GE=2:1이므로 BG는 BE의 {frac:2/3}배입니다.'],
  [`${cgStem(MED[2])} GF의 길이는 CG의 길이의 몇 배인지 기약분수로 구하시오.`,1,2,'CG:GF=2:1이므로 GF는 CG의 {frac:1/2}배입니다.'],[`${cgPar} EF의 길이는 BC의 길이의 몇 배인지 기약분수로 구하시오.`,2,3,'EF:BC=AG:AD=2:3이므로 {frac:2/3}배입니다.'],
  [`${cgPar} △AEF의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오.`,4,9,'닮음비 2:3이므로 넓이의 비는 4:9, {frac:4/9}배입니다(넓이의 이등분이 아닙니다).'],[`점 G가 △ABC의 무게중심이고 D가 변 BC의 중점일 때, △GBD의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오.`,1,6,'세 중선이 넓이가 같은 여섯 삼각형을 만들므로 {frac:1/6}배입니다.'],
  [`${cgPar} □EBCF의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오.`,5,9,'△AEF가 {frac:4/9}배이므로 □EBCF는 {frac:5/9}배입니다.']];
CGF.forEach(([prompt,n,d,explain],i)=>add('fCG',fraction({kind:'f-centroid',params:{i},prompt,num:n,den:d,explain,concept:i>=4?CGA:CG,d:i>=4?4:3,wrongs:[W({num:1,den:2},MC.bisect,'half'),W({num:d-n,den:d},MC.ratio,'reversed')]})));

// ── 선택형 ──────────────────────────────────────────────────
const PT=['AD:DB=AE:EC','AD:AB=AE:AC','AD:AB=DE:BC','AE:AC=DE:BC','DB:AB=EC:AC'],PF=['AD:DB=DE:BC','AD:AB=AE:EC','AE:EC=DE:BC','DB:AB=DE:BC','AD:DB=AE:AC'];
for(let i=0;i<PT.length;i++)for(let j=0;j<2;j++){const ws=[0,1,2].map(t=>PF[(i+j*2+t)%PF.length]);
  add('cProp',choice({kind:'prop-true',params:{t:PT[i],f:ws},style:'labels',stem:`${deStem.replace('이다.','일 때,')} 옳은 비례식을 고르시오.`,correct:PT[i],wrongs:ws.map((w,k)=>({text:w,tag:k%2?MC.part:MC.add})),explain:L=>`${L}. ${PT[i]}가 성립합니다. 부분과 전체를 섞은 비례식에 주의합니다.`,concept:TP,d:2}));}
for(let i=0;i<PF.length;i++)for(let j=0;j<2;j++){const ws=[0,1,2].map(t=>PT[(i+j+2*t)%PT.length]);if(new Set(ws).size<3)continue;
  add('cProp',choice({kind:'prop-false',params:{f:PF[i],t:ws},style:'labels',stem:`${deStem.replace('이다.','일 때,')} 옳지 않은 비례식을 고르시오.`,correct:PF[i],wrongs:ws.map((w,k)=>({text:w,tag:k%2?MC.part:MC.add})),explain:L=>`${L}. ${PF[i]}는 성립하지 않습니다.`,concept:TP,d:2}));}
const cgT=[['삼각형의 세 중선의 교점을 무엇이라고 하는지 고르시오.','무게중심',[['외심',MC.mid],['내심',MC.mid],['중점',MC.ratio]],'세 중선의 교점은 무게중심입니다.'],
  [`${cgStem(MED[0])} AG:GD를 고르시오.`,'2:1',[['1:2',MC.ratio],['1:1',MC.mid],['3:1',MC.ratio]],'무게중심은 중선을 꼭짓점으로부터 2:1로 나눕니다.'],
  [`${cgStem(MED[1])} GE:BG를 고르시오.`,'1:2',[['2:1',MC.ratio],['1:1',MC.mid],['1:3',MC.ratio]],'BG:GE=2:1이므로 GE:BG=1:2입니다.'],
  [`${cgStem(MED[2])} CG:CF를 고르시오.`,'2:3',[['1:3',MC.ratio],['1:2',MC.mid],['3:2',MC.ratio]],'CG:GF=2:1이므로 CG:CF=2:3입니다.'],
  [`${cgStem(MED[0])} GD:AD를 고르시오.`,'1:3',[['1:2',MC.mid],['2:3',MC.ratio],['2:1',MC.ratio]],'GD는 AD의 {frac:1/3}이므로 1:3입니다.']];
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

const intro=amount({kind:'mid-mn',params:{BC:4},prompt:`${midStem} BC=4 cm일 때, MN의 길이는 몇 cm인지 구하시오.`,answer:2,explain:'MN∥BC이고 MN={frac:1/2}BC이므로 MN=4÷2=2 cm입니다. 2닢을 붓습니다.',concept:MID,d:1,wrongs:[W(4,MC.mid,'equal')]});
const P=pools;
if(process.env.POOLS)console.log(Object.fromEntries(Object.entries(P).map(([k,v])=>[k,v.length])));
writeGeoPack({id:'m2s2-u4',title:'평행선과 선분의 길이의 비',standards:['[9수03-14]'],intro,groups:[
  [P.tp1,40],[P.mid1,24],[P.cg1,14],[P.cCG,5],
  [P.tp2,26],[P.mid2,24],[P.pl,20],[P.cg2,18],[P.cProp,14],
  [P.tp3,14],[P.mid3,8],[P.trMid,14],[P.cg3,12],[P.cgArea,16],[P.fDE,12],[P.fPL,10],[P.fCG,5],[P.cBis,3],[P.cJudge,12],
  [P.trPQ,16],[P.trEF,20],[P.cg4,14],[P.cgPar4,18],[P.fArea,12]]});
