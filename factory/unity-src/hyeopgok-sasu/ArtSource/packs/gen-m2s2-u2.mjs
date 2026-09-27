#!/usr/bin/env node
// m2s2-u2 사각형의 성질 ([9수03-11]). 대각선 성질 위주(교육과정 해설), 사각형 사이의 포함 관계,
// 평행사변형이 되는 조건, 평행선과 넓이. Choice truth is re-derived by the checker on a lattice.
import {amount,choice,W,writeGeoPack} from './geo-kit.mjs';
const T='m2s2-u2.';
const MC={hier:T+'exclusive-classification',pgDiag:T+'parallelogram-diagonals-equal',cond:T+'parallelogram-condition-mismatched-pair',perp:T+'perpendicular-diagonals-rhombus',
  supp:T+'consecutive-angles-not-supplementary',opp:T+'opposite-angles-confused',half:T+'half-missed',alt:T+'alternate-angle-missed',area:T+'equal-area-missed'};
const pools={};const add=(k,it)=>(pools[k]??=[]).push(it);
const Q=['A','B','C','D'];const next=v=>Q[(Q.indexOf(v)+1)%4],prev=v=>Q[(Q.indexOf(v)+3)%4],oppV=v=>Q[(Q.indexOf(v)+2)%4];
const PG='평행사변형의 성질',PGC='평행사변형이 되는 조건',SP='여러 가지 사각형의 성질',REL='사각형 사이의 관계',AREA='평행선과 넓이';

// ── 평행사변형: 각 ───────────────────────────────────────────
for(const G of Q){
  for(let x=20;x<=59;x++)add('pgAngle',amount({kind:'pg-angle-opposite',params:{given:G,x},prompt:`평행사변형 ABCD에서 ∠${G}=${x}°일 때, ∠${oppV(G)}의 크기는 몇 도인지 구하시오.`,answer:x,
    explain:`평행사변형의 두 쌍의 대각의 크기는 각각 같으므로 ∠${oppV(G)}=∠${G}=${x}°입니다.`,concept:PG,d:1,wrongs:[W(180-x,MC.opp,'supplement')]}));
  for(let x=121;x<=170;x++){const ask=x%2?next(G):prev(G);
    add('pgAngle',amount({kind:'pg-angle-adjacent',params:{given:G,x,ask},prompt:`평행사변형 ABCD에서 ∠${G}=${x}°일 때, ∠${ask}의 크기는 몇 도인지 구하시오.`,answer:180-x,
      explain:`이웃한 두 각의 크기의 합은 180°이므로 ∠${ask}=180°−${x}°=${180-x}°입니다.`,concept:PG,d:1,wrongs:[W(x>59?0:x,MC.opp,'copy'),W(360-2*x>0?360-2*x:0,MC.supp,'quad-sum')]}));}
}
for(const [P1,P2] of [['A','B'],['B','C'],['C','D'],['D','A']])for(let m=1;m<=13;m++)for(let n=1;n<=40;n++){
  if(m===n||gcd(m,n)!==1||180%(m+n))continue;const a1=180*m/(m+n),a2=180*n/(m+n);
  for(const [V,val] of [[P1,a1],[P2,a2],[oppV(P1),a1],[oppV(P2),a2]]){if(val<10||val>59)continue;if((m+n+Q.indexOf(V))%2)continue;
    add('pgRatio',amount({kind:'pg-ratio',params:{pair:[P1,P2],m,n,ask:V},prompt:`평행사변형 ABCD에서 ∠${P1}:∠${P2}=${m}:${n}일 때, ∠${V}의 크기는 몇 도인지 구하시오.`,answer:val,
      explain:`∠${P1}+∠${P2}=180°이므로 ∠${P1}=180°×${m}÷${m+n}=${a1}°, ∠${P2}=${a2}°이고 ∠${V}=${val}°입니다.`,concept:PG,d:3,wrongs:[W(360*m%(m+n)?0:360*m/(m+n),MC.supp,'use-360'),W(val===a1?a2:a1,MC.opp,'other')]}));}}
// Keep >=2 distinct misconception tags per choice item (analytics contract).
function mix(ws){if(new Set(ws.map(w=>w.tag)).size<2)ws[ws.length-1]={...ws[ws.length-1],tag:ws[0].tag===MC.hier?MC.cond:MC.hier};return ws;}
function gcd(a,b){while(b)[a,b]=[b,a%b];return a;}
// ── 평행사변형: 변·대각선 ─────────────────────────────────────
for(let a=2;a<=27;a++)for(let b=a+1;a+b<=29;b++)add('pgSide',amount({kind:'pg-perimeter',params:{a,b},prompt:`평행사변형 ABCD에서 AB=${a} cm, BC=${b} cm일 때, 평행사변형 ABCD의 둘레의 길이는 몇 cm인지 구하시오.`,answer:2*(a+b),
  explain:`두 쌍의 대변의 길이가 각각 같으므로 둘레는 2×(${a}+${b})=${2*(a+b)} cm입니다.`,concept:PG,d:1,wrongs:[W(a+b,MC.half,'one-pair')]}));
for(let p=20;p<=120;p+=2)for(let a=3;a<p/2-1;a+=3){const b=p/2-a;if(b<2||b>59||b===a)continue;
  add('pgSide2',amount({kind:'pg-side-from-perimeter',params:{p,a},prompt:`평행사변형 ABCD의 둘레의 길이가 ${p} cm이고 AB=${a} cm일 때, AD의 길이는 몇 cm인지 구하시오.`,answer:b,
    explain:`AB+AD=${p}÷2=${p/2}이므로 AD=${p/2}−${a}=${b} cm입니다.`,concept:PG,d:2,wrongs:[W(p-a>59?0:p-a,MC.half,'no-halving'),W(p-2*a>59?0:p-2*a,MC.half,'one-side')]}));}
const pgO='평행사변형 ABCD의 두 대각선의 교점을 O라고 하자.';
for(let m=2;m<=40;m++)for(let n=m+1;m+n<=59;n+=3){
  const pair=[['AO','BO'],['CO','DO'],['AO','DO'],['BO','CO']][(m+n)%4];
  add('pgDiag',amount({kind:'pg-diag-halves',params:{AC:2*m,BD:2*n,pair},prompt:`${pgO} AC=${2*m} cm, BD=${2*n} cm일 때, ${pair[0]}+${pair[1]}의 길이는 몇 cm인지 구하시오.`,answer:m+n,
    explain:`두 대각선은 서로 다른 것을 이등분하므로 ${pair[0]}=${m} cm, ${pair[1]}=${n} cm이고 합은 ${m+n} cm입니다.`,concept:PG,d:2,wrongs:[W(2*(m+n)>59?0:2*(m+n),MC.half,'no-halving'),W(2*m,MC.pgDiag,'equal-diagonals')]}));}
for(let m=3;m<=25;m++)for(let n=m+1;n<=30;n++)for(const a of [n-m+1,n,m+n-1]){if(a<2||a>=m+n||a<=n-m)continue;const ans=m+n+a;if(ans>59)continue;
  add('pgTri',amount({kind:'pg-triangle-perimeter',params:{AC:2*m,BD:2*n,AB:a},prompt:`${pgO} AC=${2*m} cm, BD=${2*n} cm, AB=${a} cm일 때, △OCD의 둘레의 길이는 몇 cm인지 구하시오.`,answer:ans,
    explain:`OC=${2*m}÷2=${m}, OD=${2*n}÷2=${n}, CD=AB=${a}이므로 둘레는 ${m}+${n}+${a}=${ans} cm입니다.`,concept:PG,d:3,wrongs:[W(2*m+2*n+a>59?0:2*m+2*n+a,MC.half,'no-halving')]}));}
// ── 직사각형·마름모·정사각형·등변사다리꼴 ──────────────────────
const rectO='직사각형 ABCD의 두 대각선의 교점을 O라고 하자.',rhO='마름모 ABCD의 두 대각선의 교점을 O라고 하자.';
for(let m=2;m<=29;m++){const [g,a]=[['AC','OD'],['BD','OA'],['AC','OB'],['BD','OC']][m%4];
  add('rectLen',amount({kind:'rect-diag-half',params:{given:g,len:2*m,ask:a},prompt:`${rectO} ${g}=${2*m} cm일 때, ${a}의 길이는 몇 cm인지 구하시오.`,answer:m,
    explain:`직사각형의 두 대각선은 길이가 같고 서로 다른 것을 이등분하므로 ${a}=${2*m}÷2=${m} cm입니다.`,concept:SP,d:1,wrongs:[W(2*m,MC.half,'no-halving')]}));}
for(let x=31;x<=80;x++)add('rectAng',amount({kind:'rect-oad',params:{OAB:x},prompt:`${rectO} ∠OAB=${x}°일 때, ∠OAD의 크기는 몇 도인지 구하시오.`,answer:90-x,
  explain:`∠DAB=90°이므로 ∠OAD=90°−${x}°=${90-x}°입니다.`,concept:SP,d:2,wrongs:[W(x,MC.pgDiag,'copy'),W(180-2*x,MC.half,'apex')]}));
for(let x=61;x<=85;x++)add('rectAng3',amount({kind:'rect-aob',params:{OAB:x},prompt:`${rectO} ∠OAB=${x}°일 때, ∠AOB의 크기는 몇 도인지 구하시오.`,answer:180-2*x,
  explain:`OA=OB이므로 ∠OBA=${x}°, ∠AOB=180°−2×${x}°=${180-2*x}°입니다.`,concept:SP,d:3,wrongs:[W(90-x,MC.pgDiag,'right-angle'),W(180-x>59?0:180-x,MC.half,'one-base')]}));
for(let y=62;y<=160;y+=2)add('rectAng3',amount({kind:'rect-oda',params:{AOD:y},prompt:`${rectO} ∠AOD=${y}°일 때, ∠ODA의 크기는 몇 도인지 구하시오.`,answer:(180-y)/2,
  explain:`OA=OD이므로 ∠ODA=(180°−${y}°)÷2=${(180-y)/2}°입니다.`,concept:SP,d:3,wrongs:[W(180-y,MC.half,'no-halving'),W(90-(180-y)/2,MC.pgDiag,'complement')]}));
for(let x=31;x<=80;x++)add('rhAng',amount({kind:'rh-oba',params:{OAB:x},prompt:`${rhO} ∠OAB=${x}°일 때, ∠OBA의 크기는 몇 도인지 구하시오.`,answer:90-x,
  explain:`마름모의 두 대각선은 서로 수직이므로 ∠AOB=90°, ∠OBA=90°−${x}°=${90-x}°입니다.`,concept:SP,d:2,wrongs:[W(x,MC.perp,'isosceles-OAB'),W(180-2*x,MC.perp,'no-right-angle')]}));
for(let x=20;x<=118;x+=2)add('rhAng',amount({kind:'rh-bac',params:{BAD:x},prompt:`마름모 ABCD에서 ∠BAD=${x}°일 때, ∠BAC의 크기는 몇 도인지 구하시오.`,answer:x/2,
  explain:`마름모의 대각선 AC는 ∠BAD를 이등분하므로 ∠BAC=${x}°÷2=${x/2}°입니다.`,concept:SP,d:2,wrongs:[W(x>59?0:x,MC.half,'no-halving'),W(90-x/2,MC.perp,'complement')]}));
for(let x=62;x<=160;x+=2)add('rhAng3',amount({kind:'rh-abd',params:{A:x},prompt:`마름모 ABCD에서 ∠A=${x}°일 때, ∠ABD의 크기는 몇 도인지 구하시오.`,answer:(180-x)/2,
  explain:`AB=AD이므로 △ABD는 이등변삼각형, ∠ABD=(180°−${x}°)÷2=${(180-x)/2}°입니다.`,concept:SP,d:3,wrongs:[W(180-x,MC.half,'no-halving'),W(x/2>59?0:x/2,MC.perp,'bisect-A')]}));
for(let k=2;k<=59;k++){const s=['AB','BC','CD','DA'][k%4];add('rhLen',amount({kind:'rh-side-from-perimeter',params:{p:4*k,ask:s},prompt:`마름모 ABCD의 둘레의 길이가 ${4*k} cm일 때, ${s}의 길이는 몇 cm인지 구하시오.`,answer:k,
  explain:`마름모의 네 변의 길이는 모두 같으므로 ${s}=${4*k}÷4=${k} cm입니다.`,concept:SP,d:1,wrongs:[W(2*k,MC.hier,'two-pairs')]}));}
for(let m=2;m<=14;m++)for(let n=m+1;m*n*2<=59;n++)add('rhArea',amount({kind:'rh-area',params:{AC:2*m,BD:2*n},prompt:`마름모 ABCD의 두 대각선의 길이가 AC=${2*m} cm, BD=${2*n} cm일 때, 마름모 ABCD의 넓이는 몇 cm²인지 구하시오.`,answer:2*m*n,
  explain:`두 대각선이 서로 다른 것을 수직이등분하므로 넓이는 ${2*m}×${2*n}÷2=${2*m*n} cm²입니다.`,concept:SP,d:2,wrongs:[W(4*m*n>59?0:4*m*n,MC.half,'no-halving')]}));
for(let m=2;m<=29;m++)add('sqLen',amount({kind:'sq-diag-half',params:{BD:2*m},prompt:`정사각형 ABCD의 두 대각선의 교점을 O라고 하자. BD=${2*m} cm일 때, OA의 길이는 몇 cm인지 구하시오.`,answer:m,
  explain:`정사각형의 두 대각선은 길이가 같고 서로 다른 것을 수직이등분하므로 OA={frac:1/2}AC={frac:1/2}BD=${m} cm입니다.`,concept:SP,d:1,wrongs:[W(2*m,MC.half,'no-halving')]}));
const sqP='정사각형 ABCD의 대각선 BD 위에 점 P가 있다.';
for(let x=10;x<=59;x++)add('sqP',amount({kind:'sq-bcp',params:{BAP:x},prompt:`${sqP} ∠BAP=${x}°일 때, ∠BCP의 크기는 몇 도인지 구하시오.`,answer:x,
  explain:`AB=CB, ∠ABP=∠CBP=45°, BP는 공통이므로 △ABP≡△CBP (SAS 합동), ∠BCP=∠BAP=${x}°입니다.`,concept:SP,d:3,wrongs:[W(90-x,MC.hier,'complement'),W(45,MC.hier,'diagonal-angle')]}));
for(let x=5;x<=14;x++)add('sqP4',amount({kind:'sq-apd',params:{BAP:x},prompt:`${sqP} ∠BAP=${x}°일 때, ∠APD의 크기는 몇 도인지 구하시오.`,answer:45+x,
  explain:`∠ABP=45°이므로 ∠APD는 △ABP의 외각, ∠APD=45°+${x}°=${45+x}°입니다.`,concept:SP,d:4,wrongs:[W(90-x,MC.hier,'complement'),W(45-x,MC.alt,'subtract')]}));
for(let x=76;x<=85;x++)add('sqP4',amount({kind:'sq-apb',params:{BAP:x},prompt:`${sqP} ∠BAP=${x}°일 때, ∠APB의 크기는 몇 도인지 구하시오.`,answer:135-x,
  explain:`∠ABP=45°이므로 ∠APB=180°−45°−${x}°=${135-x}°입니다.`,concept:SP,d:4,wrongs:[W(90-x,MC.hier,'right-angle'),W(x-45,MC.alt,'subtract')]}));
const itr='AD∥BC인 등변사다리꼴 ABCD에서';
for(let x=20;x<=59;x++)add('isoTrap',amount({kind:'isotrap-base',params:{B:x},prompt:`${itr} ∠B=${x}°일 때, ∠C의 크기는 몇 도인지 구하시오.`,answer:x,
  explain:`등변사다리꼴은 밑변의 양 끝 각의 크기가 같으므로 ∠C=∠B=${x}°입니다.`,concept:SP,d:1,wrongs:[W(180-x,MC.supp,'supplement')]}));
for(let y=121;y<=170;y++)add('isoTrap2',amount({kind:'isotrap-a-to-c',params:{A:y},prompt:`${itr} ∠A=${y}°일 때, ∠C의 크기는 몇 도인지 구하시오.`,answer:180-y,
  explain:`AD∥BC이므로 ∠B=180°−${y}°=${180-y}°, 밑변의 양 끝 각이 같아 ∠C=∠B=${180-y}°입니다.`,concept:SP,d:2,wrongs:[W(y>59?0:y,MC.opp,'parallelogram-opposite'),W(360-2*y>0?360-2*y:0,MC.supp,'quad')]}));
for(let k=2;k<=59;k++)add('isoTrap',amount({kind:'isotrap-diag',params:{AC:k},prompt:`${itr} AC=${k} cm일 때, BD의 길이는 몇 cm인지 구하시오.`,answer:k,
  explain:`등변사다리꼴의 두 대각선의 길이는 같으므로 BD=AC=${k} cm입니다.`,concept:SP,d:1,wrongs:[W(2*k>59?0:2*k,MC.pgDiag,'double')]}));
// ── 평행사변형의 각의 이등분선 ─────────────────────────────────
for(let a=3;a<=40;a++)for(let b=a+2;b-a<=59&&b<=60;b+=3){
  add('pgBis',amount({kind:'pg-bisector-ec',params:{AB:a,AD:b},prompt:`평행사변형 ABCD에서 ∠A의 이등분선이 변 BC와 만나는 점을 E라고 하자. AB=${a} cm, AD=${b} cm일 때, EC의 길이는 몇 cm인지 구하시오.`,answer:b-a,
    explain:`∠BAE=∠DAE=∠AEB(엇각)이므로 △ABE는 이등변삼각형, BE=AB=${a} cm, EC=${b}−${a}=${b-a} cm입니다.`,concept:PG,d:4,wrongs:[W(a,MC.alt,'be'),W(Math.floor(b/2),MC.half,'midpoint')]}));
  add('pgBis',amount({kind:'pg-bisector-fd',params:{AB:a,BC:b},prompt:`평행사변형 ABCD에서 ∠B의 이등분선이 변 AD와 만나는 점을 F라고 하자. AB=${a} cm, BC=${b} cm일 때, FD의 길이는 몇 cm인지 구하시오.`,answer:b-a,
    explain:`∠ABF=∠CBF=∠AFB(엇각)이므로 AF=AB=${a} cm, FD=AD−AF=${b}−${a}=${b-a} cm입니다.`,concept:PG,d:4,wrongs:[W(a,MC.alt,'af'),W(Math.floor(b/2),MC.half,'midpoint')]}));}
for(let x=62;x<=160;x+=2)add('pgBis3',amount({kind:'pg-bisector-aeb',params:{B:x},prompt:`평행사변형 ABCD에서 ∠A의 이등분선이 변 BC와 만나는 점을 E라고 하자. ∠B=${x}°일 때, ∠AEB의 크기는 몇 도인지 구하시오.`,answer:(180-x)/2,
  explain:`∠A=180°−${x}°=${180-x}°, ∠AEB=∠DAE(엇각)={frac:1/2}∠A=${(180-x)/2}°입니다.`,concept:PG,d:3,wrongs:[W(180-x,MC.half,'no-halving'),W(x/2>59?0:x/2,MC.alt,'half-B')]}));
// ── 평행선과 넓이 ────────────────────────────────────────────
for(let k=2;k<=59;k++){const tri=['ABO','BCO','CDO','DAO'][k%4];
  add('area',amount({kind:'pg-quarter',params:{S:4*k,tri},prompt:`넓이가 ${4*k} cm²인 평행사변형 ABCD의 두 대각선의 교점을 O라고 할 때, △${tri}의 넓이는 몇 cm²인지 구하시오.`,answer:k,
    explain:`두 대각선은 평행사변형을 넓이가 같은 네 삼각형으로 나누므로 △${tri}=${4*k}÷4=${k} cm²입니다.`,concept:AREA,d:3,wrongs:[W(2*k,MC.half,'half'),W(Math.floor(4*k/3),MC.area,'third')]}));}
for(let k=2;k<=59;k++){const tri=['ABC','BCD','CDA','DAB'][k%4];add('area',amount({kind:'pg-half',params:{S:2*k,tri},prompt:`넓이가 ${2*k} cm²인 평행사변형 ABCD에서 △${tri}의 넓이는 몇 cm²인지 구하시오.`,answer:k,
  explain:`대각선은 평행사변형을 합동인 두 삼각형으로 나누므로 △${tri}=${2*k}÷2=${k} cm²입니다.`,concept:AREA,d:3,wrongs:[W(Math.floor(k/2),MC.half,'quarter')]}));}
for(let a=2;a<=27;a++)for(let b=a+1;2*(a+b)<=59;b++){const [s,t]=(a+b)%2?['PAB','PCD']:['PAD','PBC'];
  add('area4',amount({kind:'pg-inner-point',params:{a,b,pair:[s,t]},prompt:`평행사변형 ABCD의 내부의 한 점 P에 대하여 △${s}의 넓이가 ${a} cm², △${t}의 넓이가 ${b} cm²일 때, 평행사변형 ABCD의 넓이는 몇 cm²인지 구하시오.`,answer:2*(a+b),
    explain:`△${s}+△${t}는 평행사변형 넓이의 {frac:1/2}이므로 넓이는 2×(${a}+${b})=${2*(a+b)} cm²입니다.`,concept:AREA,d:4,wrongs:[W(a+b,MC.area,'no-doubling'),W(4*(a+b)>59?0:4*(a+b),MC.area,'quarter')]}));}
const trO='AD∥BC인 사다리꼴 ABCD의 두 대각선의 교점을 O라고 하자.';
for(let S=10;S<=59;S++)add('area',amount({kind:'trap-abc-dbc',params:{S},prompt:`${trO} △ABC의 넓이가 ${S} cm²일 때, △DBC의 넓이는 몇 cm²인지 구하시오.`,answer:S,
  explain:`AD∥BC이므로 밑변 BC가 같은 △ABC와 △DBC는 높이가 같아 넓이가 ${S} cm²로 같습니다.`,concept:AREA,d:3,wrongs:[W(Math.floor(S/2),MC.area,'half')]}));
for(let s=4;s<=59;s++)add('area4',amount({kind:'trap-abo-dco',params:{s},prompt:`${trO} △ABO의 넓이가 ${s} cm²일 때, △DCO의 넓이는 몇 cm²인지 구하시오.`,answer:s,
  explain:`△ABC=△DBC에서 공통인 △OBC를 빼면 △ABO=△DCO=${s} cm²입니다.`,concept:AREA,d:4,wrongs:[W(Math.floor(s/2),MC.area,'half'),W(2*s>59?0:2*s,MC.area,'double')]}));
for(let S=12;S<=90;S+=2)for(let t=S-50;t<S-1;t+=5){if(t<2||2*t===S)continue;const ans=S-t;if(ans>59||ans<2)continue;
  add('area4',amount({kind:'trap-abo-from',params:{S,t},prompt:`${trO} △DBC의 넓이가 ${S} cm², △OBC의 넓이가 ${t} cm²일 때, △ABO의 넓이는 몇 cm²인지 구하시오.`,answer:ans,
    explain:`△ABC=△DBC=${S} cm²이므로 △ABO=${S}−${t}=${ans} cm²입니다.`,concept:AREA,d:4,wrongs:[W(S+t>59?0:S+t,MC.area,'add')]}));}

// ── 선택형 (격자 전수 판정 대상) ──────────────────────────────
const TY=['직사각형','마름모','정사각형','평행사변형이 아니다'];
const rectConds=['∠A=90°','∠B=90°','AC=BD','∠A=∠B','∠B=∠C','OA=OB','OB=OC'],rhombConds=['AB=BC','AB=AD','AC⊥BD','∠AOB=90°','∠ABD=∠ADB','∠BAC=∠DAC','∠ABD=∠CBD'];
for(const [conds,ans] of [[rectConds,'직사각형'],[rhombConds,'마름모']])for(const c of conds){const needO=c.includes('O');
  add('cBecome',choice({kind:'pg-becomes',params:{cond:c},stem:`평행사변형 ABCD${needO?'의 두 대각선의 교점을 O라고 할 때,':'에서'} ${c}이면 □ABCD는 어떤 사각형인지 고르시오.`,correct:ans,
    wrongs:TY.filter(t=>t!==ans).map(t=>({text:t,tag:t==='정사각형'?MC.hier:t==='평행사변형이 아니다'?MC.cond:MC.perp})),explain:`${c}이면 평행사변형 ABCD는 ${ans}입니다(정사각형이라고는 할 수 없습니다).`,concept:SP,d:2}));}
const SQ_OK=[['∠A=90°','AB=BC'],['AC=BD','AC⊥BD'],['∠A=∠B','AB=AD'],['OA=OB','∠AOB=90°'],['AC=BD','AB=BC'],['∠A=90°','AC⊥BD']];
const SQ_NO=[['∠A=90°'],['AB=BC'],['AC=BD'],['AC⊥BD'],['∠A=90°','AC=BD'],['AB=BC','AC⊥BD'],['∠A=∠B','OA=OB'],['AB=AD','∠AOB=90°']];
const fmt=c=>c.join(', ');
for(let i=0;i<SQ_OK.length;i++)for(let j=0;j<4;j++){const ws=[0,1,2].map(t=>SQ_NO[(i+j*3+t*2)%SQ_NO.length]);if(new Set(ws.map(fmt)).size<3)continue;
  add('cSquare',choice({kind:'pg-square-condition',params:{ok:SQ_OK[i],no:ws},style:'labels',stem:'평행사변형 ABCD의 두 대각선의 교점을 O라고 할 때, □ABCD가 정사각형이 되는 조건을 고르시오.',correct:fmt(SQ_OK[i]),
    wrongs:mix(ws.map(w=>({text:fmt(w),tag:w.length===1?MC.hier:MC.perp}))),explain:L=>`${L}(${fmt(SQ_OK[i])})을 만족하면 직사각형이면서 마름모이므로 정사각형이 됩니다. 나머지는 직사각형이나 마름모까지만 보장합니다.`,concept:SP,d:3}));}
const PG_OK=['AB∥DC, AD∥BC','AB=DC, AD=BC','∠A=∠C, ∠B=∠D','OA=OC, OB=OD','AB∥DC, AB=DC','AD∥BC, AD=BC'];
const PG_NO=['AB∥DC, AD=BC','AB=DC, AD∥BC','AB=AD, CB=CD','AC⊥BD, AC=BD','∠A=∠B, ∠C=∠D','OA=OB, OC=OD','AB=BC=CD','∠A+∠B=180°, AB=DC'];
for(let i=0;i<PG_OK.length;i++)for(let j=0;j<3;j++){const ws=[0,1,2].map(t=>PG_NO[(i+2*j+3*t)%PG_NO.length]);if(new Set(ws).size<3)continue;
  add('cPg',choice({kind:'pg-condition-yes',params:{ok:PG_OK[i],no:ws},style:'labels',stem:'두 대각선의 교점이 O인 □ABCD가 평행사변형이 되는 것을 고르시오.',correct:PG_OK[i],
    wrongs:mix(ws.map(w=>({text:w,tag:/∥.*=|=.*∥|\+/.test(w)?MC.cond:/⊥/.test(w)?MC.perp:MC.hier}))),explain:L=>`${L}(${PG_OK[i]})을 만족하면 평행사변형이 됩니다. 나머지는 등변사다리꼴이나 연 모양 같은 반례가 있습니다.`,concept:PGC,d:3}));}
for(let i=0;i<PG_NO.length;i++)for(let j=0;j<2;j++){const ws=[0,1,2].map(t=>PG_OK[(i+j+2*t)%PG_OK.length]);if(new Set(ws).size<3)continue;
  add('cPg',choice({kind:'pg-condition-no',params:{no:PG_NO[i],ok:ws},style:'labels',stem:'두 대각선의 교점이 O인 □ABCD가 평행사변형이 되지 않을 수도 있는 것을 고르시오.',correct:PG_NO[i],
    wrongs:mix(ws.map(w=>({text:w,tag:/∥.*=|=.*∥/.test(w)?MC.cond:/∠/.test(w)?MC.opp:MC.hier}))),explain:L=>`${L}(${PG_NO[i]})만으로는 평행사변형이 아닌 반례(등변사다리꼴·연 모양 등)가 있습니다.`,concept:PGC,d:3}));}
const REL_T=['정사각형은 직사각형이다','정사각형은 마름모이다','정사각형은 평행사변형이다','직사각형은 평행사변형이다','마름모는 평행사변형이다','평행사변형은 사다리꼴이다','마름모는 사다리꼴이다'];
const REL_F=['직사각형은 정사각형이다','마름모는 정사각형이다','직사각형은 마름모이다','마름모는 직사각형이다','평행사변형은 직사각형이다','평행사변형은 마름모이다','사다리꼴은 평행사변형이다'];
for(let i=0;i<REL_T.length;i++)for(let j=0;j<2;j++){const ws=[0,1,2].map(t=>REL_F[(i+3*j+2*t)%REL_F.length]);if(new Set(ws).size<3)continue;
  add('cRel',choice({kind:'rel-true',params:{t:REL_T[i],f:ws},style:'labels',stem:'사각형 사이의 관계로 옳은 것을 고르시오.',correct:REL_T[i],wrongs:ws.map((w,k)=>({text:w,tag:k%2?MC.hier:MC.pgDiag})),
    explain:L=>`${L}. ${REL_T[i]}. 정의에 따라 포함 관계로 판단합니다.`,concept:REL,d:2}));}
for(let i=0;i<REL_F.length;i++)for(let j=0;j<2;j++){const ws=[0,1,2].map(t=>REL_T[(i+2*j+3*t)%REL_T.length]);if(new Set(ws).size<3)continue;
  add('cRel',choice({kind:'rel-false',params:{f:REL_F[i],t:ws},style:'labels',stem:'사각형 사이의 관계로 옳지 않은 것을 고르시오.',correct:REL_F[i],wrongs:ws.map((w,k)=>({text:w,tag:k%2?MC.hier:MC.pgDiag})),
    explain:L=>`${L}. ${REL_F[i].replace('이다','이라고 항상 말할 수는 없습니다')}.`,concept:REL,d:2}));}
const DP={P1:'서로 다른 것을 이등분한다',P2:'길이가 같다',P3:'서로 수직이다',P4:'서로 다른 것을 수직이등분한다',P5:'길이가 같고 서로 다른 것을 이등분한다'};
const DIAG=[['평행사변형','P1',['P2','P3','P4']],['평행사변형','P1',['P3','P4','P5']],['등변사다리꼴','P2',['P1','P3','P4']],['등변사다리꼴','P2',['P1','P4','P5']],
  ['직사각형','P5',['P3','P4',null]],['마름모','P4',['P2','P5',null]],['마름모','P3',['P2','P5',null]],['직사각형','P2',['P3','P4',null]]];
for(const [X,c,wl] of DIAG){const ws=wl.map(k=>k?DP[k]:'한 대각선이 다른 대각선보다 항상 길다');
  add('cDiag',choice({kind:'diag-property',params:{shape:X,correct:c},style:'labels',stem:`${X}의 두 대각선에 대한 설명으로 항상 옳은 것을 고르시오.`,correct:DP[c],wrongs:mix(ws.map(w=>({text:w,tag:w===DP.P2||w===DP.P5?MC.pgDiag:MC.perp}))),
    explain:L=>`${L}. ${X}의 두 대각선은 ${DP[c]}.`,concept:SP,d:2}));}

const intro=amount({kind:'pg-diag-one',params:{AC:4},prompt:`${pgO} AC=4 cm일 때, AO의 길이는 몇 cm인지 구하시오.`,answer:2,explain:'평행사변형의 두 대각선은 서로 다른 것을 이등분하므로 AO=4÷2=2 cm입니다. 2닢을 붓습니다.',concept:PG,d:1,wrongs:[W(4,MC.half,'no-halving')]});
const P=pools;
if(process.env.POOLS)console.log(Object.fromEntries(Object.entries(P).map(([k,v])=>[k,v.length])));
writeGeoPack({id:'m2s2-u2',title:'사각형의 성질',standards:['[9수03-11]'],intro,groups:[
  [P.pgAngle,34],[P.pgSide,16],[P.rectLen,12],[P.rhLen,10],[P.sqLen,8],[P.isoTrap,14],
  [P.pgSide2,10],[P.pgDiag,12],[P.rectAng,10],[P.rhAng,12],[P.rhArea,8],[P.isoTrap2,8],[P.cBecome,14],[P.cRel,12],[P.cDiag,8],
  [P.pgRatio,12],[P.pgTri,10],[P.rectAng3,10],[P.rhAng3,10],[P.sqP,8],[P.pgBis3,8],[P.area,14],[P.cSquare,10],[P.cPg,16],
  [P.pgBis,34],[P.sqP4,16],[P.area4,34]]});
