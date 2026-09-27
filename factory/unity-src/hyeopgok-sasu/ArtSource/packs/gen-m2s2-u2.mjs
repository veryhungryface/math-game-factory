#!/usr/bin/env node
// m2s2-u2 사각형의 성질 ([9수03-11]) — schema v3 (every item a four-option choice).
// 대각선 성질 위주(교육과정 해설), 사각형 사이의 포함 관계, 평행사변형이 되는 조건, 평행선과 넓이.
// Numeric items: three wrong options, each the result of a named misconception (tag + rule).
// The answer is never a number printed in the prompt (e.g. 「∠C=∠A」 is asked as ∠B+∠D instead).
// Choice truth is re-derived by the checker on a lattice.
import {amount,choice,W,writeGeoPack,drops} from './geo-kit.mjs';
const T='m2s2-u2.';
const MC={hier:T+'exclusive-classification',pgDiag:T+'parallelogram-diagonals-equal',cond:T+'parallelogram-condition-mismatched-pair',perp:T+'perpendicular-diagonals-rhombus',
  supp:T+'consecutive-angles-not-supplementary',opp:T+'opposite-angles-confused',half:T+'half-missed',alt:T+'alternate-angle-missed',area:T+'equal-area-missed',
  bisect:T+'diagonals-bisect-overgeneralized',pa:T+'perimeter-area-confusion'};
const pools={};const add=(k,it)=>(pools[k]??=[]).push(it);
const Q=['A','B','C','D'];const next=v=>Q[(Q.indexOf(v)+1)%4],prev=v=>Q[(Q.indexOf(v)+3)%4],oppV=v=>Q[(Q.indexOf(v)+2)%4];
const PG='평행사변형의 성질',PGC='평행사변형이 되는 조건',SP='여러 가지 사각형의 성질',REL='사각형 사이의 관계',AREA='평행선과 넓이';
const A179={lo:1,hi:179};
const isInt=Number.isInteger;

// ── 평행사변형: 각 ───────────────────────────────────────────
// 이웃한 두 각(보각). Natural direction both ways (∠A=70° → ∠B=110°, ∠A=110° → ∠B=70°).
const adjWrongs=x=>[W(x,MC.opp,'adjacent-equal'),W(360-2*x,MC.half,'pair-sum-not-halved'),W(Math.abs(90-x),MC.supp,'sum-taken-as-90'),W(360-3*x,MC.opp,'three-angles-equal'),W((180-x)/2,MC.bisect,'halved-by-diagonal')].filter(w=>isInt(w.value));
for(const G of Q)for(let x=20;x<=160;x++){if(x===90)continue;const ask=x%2?next(G):prev(G);
  add('pgAdj',amount({kind:'pg-angle-adjacent',params:{given:G,x,ask},prompt:`평행사변형 ABCD에서 ∠${G}=${x}°일 때, ∠${ask}의 크기는 몇 도인지 구하시오.`,answer:180-x,
    explain:`이웃한 두 각의 크기의 합은 180°이므로 ∠${ask}=180°−${x}°=${180-x}°입니다.`,concept:PG,d:1,wrongs:adjWrongs(x),...A179}));}
// 대각: ∠A가 주어지면 나머지 두 대각의 합 ∠B+∠D (∠C=∠A 는 답이 발문에 그대로 있어 묻지 않는다).
for(const G of Q)for(let x=20;x<=160;x+=2){if(x===90)continue;const [u,v]=[next(G),prev(G)].sort();
  add('pgSum',amount({kind:'pg-angle-other-pair',params:{given:G,x},prompt:`평행사변형 ABCD에서 ∠${G}=${x}°일 때, ∠${u}+∠${v}의 크기는 몇 도인지 구하시오.`,answer:360-2*x,
    explain:`∠${u}=∠${v}=180°−${x}°=${180-x}°이므로 ∠${u}+∠${v}=${360-2*x}°입니다.`,concept:PG,d:1,wrongs:[W(2*x,MC.opp,'took-the-given-pair'),W(180-x,MC.half,'one-angle-only'),W(180,MC.supp,'opposite-pair-supplementary'),W(360-x,MC.half,'quad-sum-minus-one')],lo:1,hi:359}));}
// ∠A+∠C=y → ∠B (two steps: 대각이 같다 → 이웃각이 보각).
for(const G of Q)for(let y=40;y<=320;y+=4){if(y===180)continue;const ask=(y/4)%2?next(G):prev(G),o=oppV(G),[g1,g2]=[G,o].sort();
  add('pgSumAC',amount({kind:'pg-opposite-sum',params:{pair:[g1,g2],y,ask},prompt:`평행사변형 ABCD에서 ∠${g1}+∠${g2}=${y}°일 때, ∠${ask}의 크기는 몇 도인지 구하시오.`,answer:180-y/2,
    explain:`∠${g1}=∠${g2}=${y}°÷2=${y/2}°이고 이웃한 두 각의 합은 180°이므로 ∠${ask}=180°−${y/2}°=${180-y/2}°입니다.`,concept:PG,d:2,
    wrongs:[W(180-y,MC.half,'sum-used-as-one-angle'),W(y/2,MC.opp,'answered-the-opposite-angle'),W(360-y,MC.half,'pair-sum-not-halved'),W(90-y/4,MC.supp,'halved-twice')],...A179}));}
for(const [P1,P2] of [['A','B'],['B','C'],['C','D'],['D','A']])for(let m=1;m<=13;m++)for(let n=1;n<=40;n++){
  if(m===n||gcd(m,n)!==1||180%(m+n))continue;const a1=180*m/(m+n),a2=180*n/(m+n);
  for(const [V,val,mv] of [[P1,a1,m],[P2,a2,n],[oppV(P1),a1,m],[oppV(P2),a2,n]]){if(val<10||val>170)continue;if((m+n+Q.indexOf(V))%2)continue;
    add('pgRatio',amount({kind:'pg-ratio',params:{pair:[P1,P2],m,n,ask:V},prompt:`평행사변형 ABCD에서 ∠${P1}:∠${P2}=${m}:${n}일 때, ∠${V}의 크기는 몇 도인지 구하시오.`,answer:val,
      explain:`∠${P1}+∠${P2}=180°이므로 ∠${P1}=180°×${m}÷${m+n}=${a1}°, ∠${P2}=${a2}°이고 ∠${V}=${val}°입니다.`,concept:PG,d:3,
      wrongs:[W(val===a1?a2:a1,MC.opp,'other-angle'),W(360*mv/(m+n),MC.supp,'pair-sum-taken-as-360'),W(90*mv/(m+n),MC.supp,'pair-sum-taken-as-90'),W(180-val===val?0:(360-2*val),MC.half,'pair-not-halved')].filter(w=>isInt(w.value)),...A179}));}}
// Keep >=2 distinct misconception tags per choice item (analytics contract).
function mix(ws){if(new Set(ws.map(w=>w.tag)).size<2)ws[ws.length-1]={...ws[ws.length-1],tag:ws[0].tag===MC.hier?MC.cond:MC.hier};return ws;}
function gcd(a,b){while(b)[a,b]=[b,a%b];return a;}
// ── 평행사변형: 변·대각선 ─────────────────────────────────────
for(let a=3;a<=30;a++)for(let b=a+1;b<=a+16&&b<=40;b++)add('pgSide',amount({kind:'pg-perimeter',params:{a,b},prompt:`평행사변형 ABCD에서 AB=${a} cm, BC=${b} cm일 때, 평행사변형 ABCD의 둘레의 길이는 몇 cm인지 구하시오.`,answer:2*(a+b),
  explain:`두 쌍의 대변의 길이가 각각 같으므로 둘레는 2×(${a}+${b})=${2*(a+b)} cm입니다.`,concept:PG,d:1,
  wrongs:[W(a+b,MC.half,'one-pair-only'),W(4*b,MC.hier,'all-sides-equal'),W(2*a+b,MC.half,'three-sides'),W(a*b,MC.pa,'area-instead'),W(a+2*b,MC.half,'three-sides-b'),W(4*(a+b),MC.half,'doubled-twice'),W(2*b,MC.half,'two-sides-only')]}));
for(let p=20;p<=160;p+=2)for(let a=3;a<p/2-1;a+=3){const b=p/2-a;if(b<2||b===a)continue;
  add('pgSide2',amount({kind:'pg-side-from-perimeter',params:{p,a},prompt:`평행사변형 ABCD의 둘레의 길이가 ${p} cm이고 AB=${a} cm일 때, AD의 길이는 몇 cm인지 구하시오.`,answer:b,
    explain:`AB+AD=${p}÷2=${p/2}이므로 AD=${p/2}−${a}=${b} cm입니다.`,concept:PG,d:2,
    wrongs:[W(p-a,MC.half,'not-halved'),W(p-2*a,MC.half,'one-pair-subtracted'),W(p/4,MC.hier,'all-sides-equal'),W(p/2,MC.half,'forgot-subtracting-AB'),W(p/2-2*a,MC.half,'AB-subtracted-twice')].filter(w=>w.value>0).filter(w=>isInt(w.value))}));}
const pgO='평행사변형 ABCD의 두 대각선의 교점을 O라고 하자.';
for(let m=2;m<=40;m++)for(let n=m+1;n<=m+30&&n<=45;n+=2){
  const pair=[['AO','BO'],['CO','DO'],['AO','DO'],['BO','CO']][(m+n)%4];
  add('pgDiag',amount({kind:'pg-diag-halves',params:{AC:2*m,BD:2*n,pair},prompt:`${pgO} AC=${2*m} cm, BD=${2*n} cm일 때, ${pair[0]}+${pair[1]}의 길이는 몇 cm인지 구하시오.`,answer:m+n,
    explain:`두 대각선은 서로 다른 것을 이등분하므로 ${pair[0]}=${m} cm, ${pair[1]}=${n} cm이고 합은 ${m+n} cm입니다.`,concept:PG,d:2,
    wrongs:[W(2*(m+n),MC.half,'not-halved'),W(2*m,MC.pgDiag,'diagonals-taken-equal-AC'),W(2*n,MC.pgDiag,'diagonals-taken-equal-BD'),W(m+2*n,MC.half,'one-not-halved'),W(n,MC.half,'one-segment-only'),W((m+n)/2,MC.half,'halved-twice')].filter(w=>isInt(w.value))}));}
for(let m=3;m<=25;m++)for(let n=m+1;n<=30;n++)for(const a of [n-m+1,n,m+n-1]){if(a<2||a>=m+n||a<=n-m)continue;const ans=m+n+a;
  add('pgTri',amount({kind:'pg-triangle-perimeter',params:{AC:2*m,BD:2*n,AB:a},prompt:`${pgO} AC=${2*m} cm, BD=${2*n} cm, AB=${a} cm일 때, △OCD의 둘레의 길이는 몇 cm인지 구하시오.`,answer:ans,
    explain:`OC=${2*m}÷2=${m}, OD=${2*n}÷2=${n}, CD=AB=${a}이므로 둘레는 ${m}+${n}+${a}=${ans} cm입니다.`,concept:PG,d:3,
    wrongs:[W(2*m+2*n+a,MC.half,'not-halved'),W(2*m+a,MC.pgDiag,'OD-taken-equal-OC'),W(2*n+a,MC.pgDiag,'OC-taken-equal-OD'),W(m+n,MC.half,'CD-omitted')]}));}
// ── 직사각형·마름모·정사각형·등변사다리꼴 ──────────────────────
const rectO='직사각형 ABCD의 두 대각선의 교점을 O라고 하자.',rhO='마름모 ABCD의 두 대각선의 교점을 O라고 하자.';
for(let m=3;m<=30;m++)for(let a=2;a<2*m;a+=3){if(a===m)continue;
  add('rectTri',amount({kind:'rect-oab-perimeter',params:{AC:2*m,AB:a},prompt:`${rectO} AC=${2*m} cm, AB=${a} cm일 때, △OAB의 둘레의 길이는 몇 cm인지 구하시오.`,answer:2*m+a,
    explain:`직사각형의 두 대각선은 길이가 같고 서로 다른 것을 이등분하므로 OA=OB=${m} cm, 둘레는 ${m}+${m}+${a}=${2*m+a} cm입니다.`,concept:SP,d:2,
    wrongs:[W(4*m+a,MC.half,'not-halved'),W(m+a,MC.pgDiag,'OB-not-known-equal'),W(2*m,MC.half,'AB-omitted'),W(3*m+a,MC.half,'one-not-halved'),W(4*m,MC.half,'AB-omitted-diagonal-whole')]}));}
for(let x=10;x<=80;x++){if(x===45)continue;add('rectAng',amount({kind:'rect-oad',params:{OAB:x},prompt:`${rectO} ∠OAB=${x}°일 때, ∠OAD의 크기는 몇 도인지 구하시오.`,answer:90-x,
  explain:`∠DAB=90°이므로 ∠OAD=90°−${x}°=${90-x}°입니다.`,concept:SP,d:2,
  wrongs:[W(x,MC.bisect,'diagonal-bisects-angle'),W(180-2*x,MC.half,'took-angle-AOB'),W(180-x,MC.supp,'supplement-instead'),W(45,MC.hier,'square-assumed')],...A179}));}
for(let x=10;x<=85;x++){if(x===45||x===60)continue;add('rectAng3',amount({kind:'rect-aob',params:{OAB:x},prompt:`${rectO} ∠OAB=${x}°일 때, ∠AOB의 크기는 몇 도인지 구하시오.`,answer:180-2*x,
  explain:`OA=OB이므로 ∠OBA=${x}°, ∠AOB=180°−2×${x}°=${180-2*x}°입니다.`,concept:SP,d:3,
  wrongs:[W(90,MC.perp,'perpendicular-assumed'),W(180-x,MC.half,'one-base-angle-only'),W(2*x,MC.alt,'took-angle-AOD'),W(90-x,MC.bisect,'took-angle-OAD')],...A179}));}
for(let y=20;y<=160;y+=2){if(y===90||y===60)continue;add('rectAng3',amount({kind:'rect-oda',params:{AOD:y},prompt:`${rectO} ∠AOD=${y}°일 때, ∠ODA의 크기는 몇 도인지 구하시오.`,answer:(180-y)/2,
  explain:`OA=OD이므로 ∠ODA=(180°−${y}°)÷2=${(180-y)/2}°입니다.`,concept:SP,d:3,
  wrongs:[W(180-y,MC.half,'not-halved'),W(y/2,MC.alt,'took-angle-ODC'),W(180-y/2,MC.supp,'used-360'),W(90,MC.perp,'perpendicular-assumed'),W((180-y)/4,MC.half,'halved-twice')].filter(w=>isInt(w.value)),...A179}));}
for(let x=10;x<=80;x++){if(x===45)continue;add('rhAng',amount({kind:'rh-oba',params:{OAB:x},prompt:`${rhO} ∠OAB=${x}°일 때, ∠OBA의 크기는 몇 도인지 구하시오.`,answer:90-x,
  explain:`마름모의 두 대각선은 서로 수직이므로 ∠AOB=90°, ∠OBA=90°−${x}°=${90-x}°입니다.`,concept:SP,d:2,
  wrongs:[W(x,MC.perp,'triangle-OAB-taken-isosceles'),W(180-2*x,MC.perp,'right-angle-at-O-missed'),W(45,MC.hier,'square-assumed'),W(180-x,MC.supp,'supplement-instead')],...A179}));}
for(let x=20;x<=170;x+=2){if(x===90)continue;add('rhAng',amount({kind:'rh-bac',params:{BAD:x},prompt:`마름모 ABCD에서 ∠BAD=${x}°일 때, ∠BAC의 크기는 몇 도인지 구하시오.`,answer:x/2,
  explain:`마름모의 대각선 AC는 ∠BAD를 이등분하므로 ∠BAC=${x}°÷2=${x/2}°입니다.`,concept:SP,d:2,
  wrongs:[W(90-x/2,MC.perp,'took-angle-ABD'),W(180-x,MC.supp,'took-adjacent-angle'),W(45,MC.hier,'square-assumed'),W(x/4,MC.half,'halved-twice')].filter(w=>isInt(w.value)),...A179}));}
for(let x=20;x<=170;x+=2){if(x===90||x===60)continue;add('rhAng3',amount({kind:'rh-abd',params:{A:x},prompt:`마름모 ABCD에서 ∠A=${x}°일 때, ∠ABD의 크기는 몇 도인지 구하시오.`,answer:(180-x)/2,
  explain:`AB=AD이므로 △ABD는 이등변삼각형, ∠ABD=(180°−${x}°)÷2=${(180-x)/2}°입니다.`,concept:SP,d:3,
  wrongs:[W(180-x,MC.half,'took-angle-B'),W(x/2,MC.perp,'took-angle-BAC'),W(90,MC.perp,'perpendicular-at-B'),W(180-x/2,MC.supp,'used-360'),W((180-x)/4,MC.half,'halved-twice')].filter(w=>isInt(w.value)),...A179}));}
for(let m=2;m<=12;m++)for(let n=m+1;n<=16;n++)add('rhArea',amount({kind:'rh-area',params:{AC:2*m,BD:2*n},prompt:`마름모 ABCD의 두 대각선의 길이가 AC=${2*m} cm, BD=${2*n} cm일 때, 마름모 ABCD의 넓이는 몇 cm²인지 구하시오.`,answer:2*m*n,
  explain:`두 대각선이 서로 다른 것을 수직이등분하므로 넓이는 ${2*m}×${2*n}÷2=${2*m*n} cm²입니다.`,concept:SP,d:2,
  wrongs:[W(4*m*n,MC.half,'not-halved'),W(m*n,MC.half,'halved-twice'),W(2*(m+n),MC.pa,'added-diagonals'),W(4*(m+n),MC.pa,'perimeter-like-sum'),W(8*m*n,MC.half,'doubled-instead')]}));
const sqP='정사각형 ABCD의 대각선 BD 위에 점 P가 있다.';
for(let x=5;x<=85;x++){if(x===45)continue;add('sqP',amount({kind:'sq-pcd',params:{BAP:x},prompt:`${sqP} ∠BAP=${x}°일 때, ∠PCD의 크기는 몇 도인지 구하시오.`,answer:90-x,
  explain:`△ABP≡△CBP (SAS 합동)이므로 ∠BCP=∠BAP=${x}°, ∠PCD=90°−${x}°=${90-x}°입니다.`,concept:SP,d:3,
  wrongs:[W(x,MC.alt,'took-angle-BCP'),W(45,MC.hier,'diagonal-angle'),W(Math.abs(45-x),MC.hier,'subtracted-from-45'),W(180-x,MC.supp,'supplement-instead')],...A179}));}
for(let x=5;x<=85;x++){if(x===45)continue;add('sqP4',amount({kind:'sq-apd',params:{BAP:x},prompt:`${sqP} ∠BAP=${x}°일 때, ∠APD의 크기는 몇 도인지 구하시오.`,answer:45+x,
  explain:`∠ABP=45°이므로 ∠APD는 △ABP의 외각, ∠APD=45°+${x}°=${45+x}°입니다.`,concept:SP,d:4,
  wrongs:[W(135-x,MC.supp,'took-angle-APB'),W(90+x,MC.hier,'angle-ABP-taken-as-90'),W(Math.abs(45-x),MC.alt,'subtracted'),W(90-x,MC.hier,'complement')],...A179}));
  add('sqP4',amount({kind:'sq-apb',params:{BAP:x},prompt:`${sqP} ∠BAP=${x}°일 때, ∠APB의 크기는 몇 도인지 구하시오.`,answer:135-x,
  explain:`∠ABP=45°이므로 ∠APB=180°−45°−${x}°=${135-x}°입니다.`,concept:SP,d:4,
  wrongs:[W(45+x,MC.supp,'took-exterior-angle-APD'),W(90-x,MC.hier,'angle-ABP-taken-as-90'),W(Math.abs(x-45),MC.alt,'subtracted'),W(180-x,MC.half,'angle-ABP-omitted')],...A179}));}
const itr='AD∥BC인 등변사다리꼴 ABCD에서';
const trapW=g=>[W(g,MC.opp,'parallelogram-opposite-equal'),W(Math.abs(90-g),MC.supp,'sum-taken-as-90'),W(360-3*g,MC.opp,'three-angles-equal'),W(360-2*g,MC.half,'pair-sum-not-halved')];
for(let x=20;x<=85;x++)add('isoTrap',amount({kind:'isotrap-b-to-d',params:{B:x},prompt:`${itr} ∠B=${x}°일 때, ∠D의 크기는 몇 도인지 구하시오.`,answer:180-x,
  explain:`밑변의 양 끝 각이 같아 ∠C=∠B=${x}°이고, AD∥BC이므로 ∠D=180°−∠C=${180-x}°입니다.`,concept:SP,d:1,wrongs:trapW(x),...A179}));
for(let y=95;y<=160;y++)add('isoTrap',amount({kind:'isotrap-a-to-c',params:{A:y},prompt:`${itr} ∠A=${y}°일 때, ∠C의 크기는 몇 도인지 구하시오.`,answer:180-y,
  explain:`AD∥BC이므로 ∠B=180°−${y}°=${180-y}°, 밑변의 양 끝 각이 같아 ∠C=∠B=${180-y}°입니다.`,concept:SP,d:1,wrongs:trapW(y),...A179}));
const itrO='AD∥BC인 등변사다리꼴 ABCD의 두 대각선의 교점을 O라고 하자.';
for(let a=8;a<=60;a++)for(let b=2;2*b<a;b+=2){
  add('isoTrapO',amount({kind:'isotrap-ob',params:{AC:a,OA:b},prompt:`${itrO} AC=${a} cm, OA=${b} cm일 때, OB의 길이는 몇 cm인지 구하시오.`,answer:a-b,
    explain:`등변사다리꼴의 두 대각선은 길이가 같고 △OAD는 OA=OD인 이등변삼각형이므로 OB=BD−OD=${a}−${b}=${a-b} cm입니다.`,concept:SP,d:2,
    wrongs:[W(b,MC.bisect,'OB-taken-equal-OA'),W(a/2,MC.bisect,'diagonals-bisect-assumed'),W(a+b,MC.half,'added-instead'),W(a-2*b,MC.half,'subtracted-twice'),W(a,MC.half,'whole-diagonal')].filter(w=>isInt(w.value))}));}
// ── 평행사변형의 각의 이등분선 ─────────────────────────────────
for(let a=3;a<=40;a++)for(let b=a+2;b<=70;b+=3){
  const bw=(seg,pt)=>[W(a,MC.alt,`answered-${seg}`),W(b/2,MC.half,`${pt}-taken-as-midpoint`),W(a+b,MC.alt,'added-instead'),W(2*a,MC.alt,'doubled-AB')].filter(w=>isInt(w.value));
  add('pgBis',amount({kind:'pg-bisector-ec',params:{AB:a,AD:b},prompt:`평행사변형 ABCD에서 ∠A의 이등분선이 변 BC와 만나는 점을 E라고 하자. AB=${a} cm, AD=${b} cm일 때, EC의 길이는 몇 cm인지 구하시오.`,answer:b-a,
    explain:`∠BAE=∠DAE=∠AEB(엇각)이므로 △ABE는 이등변삼각형, BE=AB=${a} cm, EC=${b}−${a}=${b-a} cm입니다.`,concept:PG,d:4,wrongs:bw('BE','E')}));
  add('pgBis',amount({kind:'pg-bisector-fd',params:{AB:a,BC:b},prompt:`평행사변형 ABCD에서 ∠B의 이등분선이 변 AD와 만나는 점을 F라고 하자. AB=${a} cm, BC=${b} cm일 때, FD의 길이는 몇 cm인지 구하시오.`,answer:b-a,
    explain:`∠ABF=∠CBF=∠AFB(엇각)이므로 AF=AB=${a} cm, FD=AD−AF=${b}−${a}=${b-a} cm입니다.`,concept:PG,d:4,wrongs:bw('AF','F')}));}
for(let x=20;x<=160;x+=2){if(x===90||x===60)continue;add('pgBis3',amount({kind:'pg-bisector-aeb',params:{B:x},prompt:`평행사변형 ABCD에서 ∠A의 이등분선이 변 BC와 만나는 점을 E라고 하자. ∠B=${x}°일 때, ∠AEB의 크기는 몇 도인지 구하시오.`,answer:(180-x)/2,
  explain:`∠A=180°−${x}°=${180-x}°, ∠AEB=∠DAE(엇각)={frac:1/2}∠A=${(180-x)/2}°입니다.`,concept:PG,d:3,
  wrongs:[W(180-x,MC.half,'not-halved'),W(x/2,MC.alt,'halved-angle-B'),W(90+x/2,MC.supp,'took-angle-AEC'),W(x,MC.alt,'copied-angle-B'),W((180-x)/4,MC.half,'halved-twice')].filter(w=>isInt(w.value)),...A179}));}
// ── 평행선과 넓이 ────────────────────────────────────────────
for(let k=3;k<=50;k++){const tri=['ABO','BCO','CDO','DAO'][k%4];
  add('area',amount({kind:'pg-quarter',params:{S:4*k,tri},prompt:`넓이가 ${4*k} cm²인 평행사변형 ABCD의 두 대각선의 교점을 O라고 할 때, △${tri}의 넓이는 몇 cm²인지 구하시오.`,answer:k,
    explain:`두 대각선은 평행사변형을 넓이가 같은 네 삼각형으로 나누므로 △${tri}=${4*k}÷4=${k} cm²입니다.`,concept:AREA,d:3,
    wrongs:[W(2*k,MC.half,'halved-once'),W(3*k,MC.area,'remaining-three'),W(4*k/3,MC.area,'split-in-three'),W(k/2,MC.half,'halved-three-times')].filter(w=>isInt(w.value))}));}
for(let k=3;k<=50;k++){const [s,t]=[['ABO','CDO'],['BCO','DAO']][k%2];
  add('area',amount({kind:'pg-two-quarters',params:{S:4*k,pair:[s,t]},prompt:`넓이가 ${4*k} cm²인 평행사변형 ABCD의 두 대각선의 교점을 O라고 할 때, △${s}+△${t}의 넓이는 몇 cm²인지 구하시오.`,answer:2*k,
    explain:`네 삼각형의 넓이가 모두 ${4*k}÷4=${k} cm²로 같으므로 합은 ${k}+${k}=${2*k} cm²입니다.`,concept:AREA,d:3,
    wrongs:[W(k,MC.half,'one-triangle-only'),W(3*k,MC.area,'remaining-three'),W(4*k/3,MC.area,'split-in-three'),W(8*k,MC.half,'doubled-whole')].filter(w=>isInt(w.value))}));}
for(let a=2;a<=30;a++)for(let b=a+1;b<=a+20;b++){const [s,t]=(a+b)%2?['PAB','PCD']:['PAD','PBC'];
  add('area4',amount({kind:'pg-inner-point',params:{a,b,pair:[s,t]},prompt:`평행사변형 ABCD의 내부의 한 점 P에 대하여 △${s}의 넓이가 ${a} cm², △${t}의 넓이가 ${b} cm²일 때, 평행사변형 ABCD의 넓이는 몇 cm²인지 구하시오.`,answer:2*(a+b),
    explain:`△${s}+△${t}는 평행사변형 넓이의 {frac:1/2}이므로 넓이는 2×(${a}+${b})=${2*(a+b)} cm²입니다.`,concept:AREA,d:4,
    wrongs:[W(a+b,MC.area,'not-doubled'),W(4*(a+b),MC.half,'pair-taken-as-quarter'),W((a+b)/2,MC.half,'halved-instead'),W(a*b,MC.pa,'multiplied-areas'),W(3*(a+b),MC.area,'counted-three-triangles'),W(2*b,MC.area,'larger-triangle-doubled')].filter(w=>isInt(w.value))}));}
for(let S=20;S<=160;S+=4)for(let a=2;2*a<S/2;a+=3){const [s,t]=(S/4+a)%2?['PAD','PBC']:['PAB','PCD'];const ans=S/2-a;
  add('area4',amount({kind:'pg-inner-rest',params:{S,a,pair:[s,t]},prompt:`넓이가 ${S} cm²인 평행사변형 ABCD의 내부의 한 점 P에 대하여 △${s}의 넓이가 ${a} cm²일 때, △${t}의 넓이는 몇 cm²인지 구하시오.`,answer:ans,
    explain:`△${s}+△${t}={frac:1/2}×${S}=${S/2} cm²이므로 △${t}=${S/2}−${a}=${ans} cm²입니다.`,concept:AREA,d:4,
    wrongs:[W(S-a,MC.area,'whole-minus-one'),W(S/4,MC.half,'quarter-by-diagonals'),W(S/2,MC.area,'forgot-subtracting'),W(S/4-a,MC.half,'quarter-minus-one'),W(a,MC.area,'taken-equal-to-given')].filter(w=>isInt(w.value))}));}
const trO='AD∥BC인 사다리꼴 ABCD의 두 대각선의 교점을 O라고 하자.';
for(let s=3;s<=40;s++)for(let t=s+2;t<=s+30;t+=3){
  add('trap',amount({kind:'trap-dbc-sum',params:{s,t},prompt:`${trO} △ABO의 넓이가 ${s} cm², △OBC의 넓이가 ${t} cm²일 때, △DBC의 넓이는 몇 cm²인지 구하시오.`,answer:s+t,
    explain:`△ABC=△DBC이므로 △DCO=△ABO=${s} cm², △DBC=${s}+${t}=${s+t} cm²입니다.`,concept:AREA,d:4,
    wrongs:[W(2*s+t,MC.area,'overlap-counted-twice'),W(t-s,MC.area,'subtracted-instead'),W(2*t,MC.area,'DCO-taken-equal-OBC'),W(2*s,MC.half,'doubled-ABO'),W(t,MC.area,'OBC-only')].filter(w=>w.value>0)}));}
for(let S=12;S<=120;S+=2)for(let t=S-40;t<S-1;t+=3){if(t<2||2*t===S)continue;const ans=S-t;
  add('trap',amount({kind:'trap-abo-from',params:{S,t},prompt:`${trO} △DBC의 넓이가 ${S} cm², △OBC의 넓이가 ${t} cm²일 때, △ABO의 넓이는 몇 cm²인지 구하시오.`,answer:ans,
    explain:`△ABC=△DBC=${S} cm²이므로 △ABO=${S}−${t}=${ans} cm²입니다.`,concept:AREA,d:4,
    wrongs:[W(S+t,MC.area,'added-instead'),W(S/2,MC.half,'halved-DBC'),W(S-2*t,MC.area,'subtracted-twice'),W(t/2,MC.half,'halved-OBC'),W(S,MC.area,'whole-DBC')].filter(w=>isInt(w.value)&&w.value>0)}));
  add('trap',amount({kind:'trap-dco-from',params:{S,t},prompt:`${trO} △ABC의 넓이가 ${S} cm², △OBC의 넓이가 ${t} cm²일 때, △DCO의 넓이는 몇 cm²인지 구하시오.`,answer:ans,
    explain:`△DBC=△ABC=${S} cm²이므로 △DCO=△DBC−△OBC=${S}−${t}=${ans} cm²입니다.`,concept:AREA,d:4,
    wrongs:[W(S+t,MC.area,'added-instead'),W(S/2,MC.half,'halved-ABC'),W(S-2*t,MC.area,'subtracted-twice'),W(t/2,MC.half,'halved-OBC'),W(S,MC.area,'whole-ABC')].filter(w=>isInt(w.value)&&w.value>0)}));}

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


// items[0]: the textbook's first parallelogram question (∠A=110° → ∠B).
const intro=amount({kind:'pg-angle-adjacent',params:{given:'A',x:110,ask:'B'},prompt:'평행사변형 ABCD에서 ∠A=110°일 때, ∠B의 크기는 몇 도인지 구하시오.',answer:70,
  explain:'이웃한 두 각의 크기의 합은 180°이므로 ∠B=180°−110°=70°입니다.',concept:PG,d:1,wrongs:adjWrongs(110),...A179});
// Rank balance: interleave each pool by the value rank of the answer among its four options
// (0=smallest … 3=largest) so the evenly spaced pick does not favour one rank.
const rankOf=it=>it.mode==='int'?it.wrongs.filter(w=>w.value<it.answer).length:-1;
function balance(pool){const b={};for(const it of pool.filter(Boolean))(b[rankOf(it)]??=[]).push(it);const ks=Object.keys(b).sort(),out=[];
  for(let i=0;out.length<pool.filter(Boolean).length;i++)for(const k of ks)if(b[k][i])out.push(b[k][i]);return out;}
const P=Object.fromEntries(Object.entries(pools).map(([k,v])=>[k,balance(v)]));
if(process.env.POOLS)console.log(Object.fromEntries(Object.entries(P).map(([k,v])=>[k,`${v.filter(Boolean).length}/${v.length}`])));
writeGeoPack({id:'m2s2-u2',title:'사각형의 성질',standards:['[9수03-11]'],intro,groups:[
  [P.pgAdj,36],[P.pgSum,20],[P.pgSide,18],[P.isoTrap,16],
  [P.pgSumAC,10],[P.pgSide2,9],[P.pgDiag,9],[P.rectTri,8],[P.rectAng,7],[P.rhAng,10],[P.rhArea,7],[P.isoTrapO,8],[P.cBecome,14],[P.cRel,12],[P.cDiag,8],
  [P.pgRatio,12],[P.pgTri,10],[P.rectAng3,12],[P.rhAng3,8],[P.sqP,8],[P.pgBis3,8],[P.area,14],[P.cSquare,10],[P.cPg,16],
  [P.pgBis,28],[P.sqP4,18],[P.area4,22],[P.trap,24]]});
