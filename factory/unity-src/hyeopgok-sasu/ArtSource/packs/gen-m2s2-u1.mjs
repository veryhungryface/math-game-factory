#!/usr/bin/env node
// m2s2-u1 삼각형의 성질 ([9수03-09] 이등변삼각형, [9수03-10] 외심·내심) — schema v3 (전 문항 4지선다).
// Numeric items: the correct value + three wrong values, each the result of a named misconception
// (tag m2s2-u1.*). The 60-coin cap is gone, so large angles (∠BIC=125° …) are asked directly.
// No figure: every needed condition is in the text. Items whose answer is printed in the prompt
// are rejected by the kit (answer-by-copying), so those templates ask a derived quantity instead.
import {amount,choice,W,writeGeoPack} from './geo-kit.mjs';
const T='m2s2-u1.';
const MC={center:T+'center-swap',inside:T+'circumcenter-always-inside',formula:T+'center-formula-swap',apex:T+'apex-by-position',ssa:T+'ssa-congruence',
  half:T+'half-missed',sum:T+'angle-sum-error',ext:T+'exterior-angle-confusion',perp:T+'bisector-perpendicular-missed',hyp:T+'hypotenuse-leg-confusion',equi:T+'equilateral-overgeneralized',right:T+'right-triangle-center-missed',
  eq:T+'equal-sides-equation-error'};
const V=['A','B','C'];const rest=X=>V.filter(v=>v!==X);
const opp={A:'BC',B:'AC',C:'AB'}; // side opposite each vertex
const pools={};const add=(k,it)=>(pools[k]??=[]).push(it);
const inside='(단, 점 O는 △ABC의 내부에 있다.)';
const TRI=179;
const rot=(ws,i)=>ws.map((_,j)=>ws[(j+i)%ws.length]); // an interior angle of a triangle is below 180°

// ── 이등변삼각형 ─────────────────────────────────────────────
for(const X of V){const [Y,Z]=rest(X);
  for(let a=20;a<=116;a+=2){if(a===60)continue;const ask=(a/2)%2?Y:Z,ans=(180-a)/2;
    add('isoBase',amount({kind:'iso-base',params:{apex:X,apexAngle:a,ask},prompt:`${X}${Y}=${X}${Z}인 이등변삼각형 ABC에서 ∠${X}=${a}°일 때, ∠${ask}의 크기는 몇 도인지 구하시오.`,answer:ans,
      explain:`두 밑각 ∠${Y}, ∠${Z}의 크기가 같으므로 ∠${ask}=(180°−${a}°)÷2=${ans}°입니다.`,concept:'이등변삼각형의 성질',d:1,hi:TRI,
      wrongs:rot([W(180-a,MC.half,'no-halving'),W(a/2,MC.perp,'bisected-apex-as-base'),W(a,MC.apex,'apex-as-base'),W(90-a,MC.half,'half-of-180-only'),W(180-2*a,MC.apex,'given-as-base'),W(90+a/2,MC.ext,'exterior-angle')],(a/2)%3)}));}
  for(let b=20;b<=75;b++){if(b===60)continue;const given=b%2?Y:Z,ans=180-2*b;
    add('isoApex',amount({kind:'iso-apex',params:{apex:X,baseAngle:b,given},prompt:`${X}${Y}=${X}${Z}인 이등변삼각형 ABC에서 ∠${given}=${b}°일 때, ∠${X}의 크기는 몇 도인지 구하시오.`,answer:ans,
      explain:`∠${Y}=∠${Z}=${b}°이므로 ∠${X}=180°−2×${b}°=${ans}°입니다.`,concept:'이등변삼각형의 성질',d:1,hi:TRI,
      wrongs:rot([W(b,MC.apex,'apex-equals-base'),W(180-b,MC.half,'one-base-only'),W(90-b,MC.half,'halved'),W((180-b)/2,MC.apex,'given-as-apex'),W(2*b,MC.ext,'exterior-as-apex')],b%3)}));}
}
const extStem='AB=AC인 이등변삼각형 ABC에서 변 BC를 점 C 쪽으로 연장한 직선 위에 점 D를 잡았더니';
for(let z=95;z<=170;z++){if(2*z-180!==180-z)add('isoExt',amount({kind:'iso-ext-apex',params:{z},prompt:`${extStem} ∠ACD=${z}°이었다. ∠A의 크기는 몇 도인지 구하시오.`,answer:2*z-180,
  explain:`∠ACB=180°−${z}°=${180-z}°=∠B이므로 ∠A=180°−2×${180-z}°=${2*z-180}°입니다.`,concept:'이등변삼각형의 성질',d:2,hi:TRI,
  wrongs:[W(180-z,MC.ext,'base-angle'),W(z-90,MC.sum,'minus-right'),W(z/2,MC.apex,'apex-equals-base'),W(z,MC.ext,'exterior-as-interior'),W(360-2*z,MC.ext,'exterior-for-interior')]}));}
for(let z=100;z<=172;z++)add('isoExt',amount({kind:'iso-ext-base',params:{z},prompt:`${extStem} ∠ACD=${z}°이었다. ∠B의 크기는 몇 도인지 구하시오.`,answer:180-z,
  explain:`∠ACB=180°−${z}°=${180-z}°이고 두 밑각이 같으므로 ∠B=${180-z}°입니다.`,concept:'이등변삼각형의 성질',d:2,hi:TRI,
  wrongs:[W(z-90,MC.sum,'minus-right'),W(z/2,MC.ext,'half-exterior'),W(2*z-180,MC.apex,'apex'),W(360-2*z,MC.ext,'apex-exterior')]}));
const bisStem='AB=AC인 이등변삼각형 ABC에서 ∠A의 이등분선이 변 BC와 만나는 점을 D라고 하자.';
for(let k=2;k<=30;k++)for(const s of [k+1+(k*5)%9,k+3+(k*7)%11]){const P=k%2?'BD':'CD';
  add('isoBisLen',amount({kind:'iso-bis-half',params:{ab:s,bc:2*k,ask:P},prompt:`${bisStem} AB=${s} cm, BC=${2*k} cm일 때, ${P}의 길이는 몇 cm인지 구하시오.`,answer:k,
    explain:`꼭지각의 이등분선은 밑변을 수직이등분하므로 ${P}=${2*k}÷2=${k} cm입니다.`,concept:'이등변삼각형의 성질',d:1,
    wrongs:rot([W(2*k,MC.perp,'no-halving'),W(s,MC.apex,'equal-side-as-half'),W(k/2,MC.perp,'halved-twice'),W(s/2,MC.perp,'half-of-equal-side'),W(4*k,MC.perp,'doubled-instead')],k%2)}));
  add('isoBisLen',amount({kind:'iso-bis-double',params:{ab:s,bd:k,given:P},prompt:`${bisStem} AB=${s} cm, ${P}=${k} cm일 때, BC의 길이는 몇 cm인지 구하시오.`,answer:2*k,
    explain:`점 D는 BC의 중점이므로 BC=2×${k}=${2*k} cm입니다.`,concept:'이등변삼각형의 성질',d:1,
    wrongs:rot([W(k,MC.perp,'no-doubling'),W(4*k,MC.perp,'doubled-twice'),W(k/2,MC.perp,'halved-instead'),W(s,MC.apex,'equal-side'),W(2*s,MC.perp,'double-equal-side'),W(s+k,MC.sum,'add-given')],k%3)}));
  add('isoBisPer',amount({kind:'iso-bis-perimeter',params:{ab:s,bd:k,given:P},prompt:`${bisStem} AB=${s} cm, ${P}=${k} cm일 때, △ABC의 둘레의 길이는 몇 cm인지 구하시오.`,answer:2*s+2*k,
    explain:`AC=AB=${s} cm, BC=2×${k}=${2*k} cm이므로 둘레는 ${s}+${s}+${2*k}=${2*s+2*k} cm입니다.`,concept:'이등변삼각형의 성질',d:2,
    wrongs:k%3?[W(2*s+k,MC.perp,'no-doubling'),W(2*s+4*k,MC.perp,'double-twice'),W(s+2*k,MC.apex,'one-equal-side'),W(s+k,MC.sum,'given-only')]:[W(2*s+k,MC.perp,'no-doubling'),W(s+2*k,MC.apex,'one-equal-side'),W(s+k,MC.sum,'given-only'),W(2*s+4*k,MC.perp,'double-twice')]}));
}
for(let b=20;b<=85;b++)add('isoBisAng',amount({kind:'iso-bis-angle',params:{b},prompt:`${bisStem} ∠B=${b}°일 때, ∠BAD의 크기는 몇 도인지 구하시오.`,answer:90-b,
  explain:`AD⊥BC이므로 ∠ADB=90°, ∠BAD=180°−90°−${b}°=${90-b}°입니다.`,concept:'이등변삼각형의 성질',d:2,hi:TRI,
  wrongs:rot([W(180-2*b,MC.perp,'whole-apex'),W(b/2,MC.half,'bisected-base-angle'),W(b,MC.apex,'copy-base'),W(180-b,MC.sum,'no-right-angle'),W(90-b/2,MC.half,'halved-base'),W(45-b/2,MC.half,'halved-twice')],b%2)}));
for(let x=10;x<=80;x++)add('isoBisAng',amount({kind:'iso-bis-angle-rev',params:{x},prompt:`${bisStem} ∠BAD=${x}°일 때, ∠C의 크기는 몇 도인지 구하시오.`,answer:90-x,
  explain:`∠A=2×${x}°=${2*x}°이므로 ∠C=(180°−${2*x}°)÷2=${90-x}°입니다.`,concept:'이등변삼각형의 성질',d:2,hi:TRI,
  wrongs:[W(2*x,MC.apex,'apex-as-base'),W(x,MC.perp,'copy-half'),W(180-2*x,MC.half,'no-halving'),W(180-x,MC.sum,'no-right-angle')]}));
// 두 내각이 같은 삼각형은 이등변삼각형: the equal angles are hidden behind an angle sum, and the
// equal side is given as an expression (so the answer is never a printed number).
for(const P of V)for(const Q of V){if(P===Q)continue;const R=V.find(v=>v!==P&&v!==Q);
  for(let p=20;p<=85;p+=5){if(p===60)continue;const r=180-2*p;const k=9+((p*7+V.indexOf(P)*11+V.indexOf(Q)*5)%40);
    let c=1+((k+p)%(k-2));if((k-c)%2)c++;if(c>=k)c-=2;if(c<1)continue;const minus=(p/5+V.indexOf(Q))%2===1,x=minus?(k+c)/2:(k-c)/2,sg=minus?'−':'+';
    const [g1,g2]=[P,R].sort();const ang={[P]:p,[R]:r};
    add('isoConv',amount({kind:'iso-converse',params:{angles:{[P]:p,[Q]:p,[R]:r},given:[g1,g2],side:opp[P],ask:opp[Q],k,c,sign:sg},
      prompt:`△ABC에서 ∠${g1}=${ang[g1]}°, ∠${g2}=${ang[g2]}°이고 ${opp[P]}=${k} cm, ${opp[Q]}=(2x${sg}${c}) cm일 때, x의 값을 구하시오.`,answer:x,
      explain:`∠${Q}=180°−${p}°−${r}°=${p}°=∠${P}이므로 ${opp[Q]}=${opp[P]}, 2x${sg}${c}=${k}에서 x=${x}입니다.`,concept:'이등변삼각형이 되는 조건',d:4,
      wrongs:minus?rot([W((k-c)/2,MC.eq,'sign-error'),W(k+c,MC.half,'no-halving'),W(k/2,MC.eq,'constant-dropped'),W(k/2+c,MC.eq,'partial-division'),W(k-c,MC.half,'sign-error-no-halving'),W(k,MC.eq,'side-as-x')],p%3):[W(k/2-c,MC.eq,'partial-division'),W(k-c,MC.half,'no-halving'),W((k+c)/2,MC.eq,'sign-error'),W(k/2,MC.eq,'constant-dropped'),W(k,MC.eq,'side-as-x')]}));}}

// ── 직각삼각형의 합동 · 각의 이등분선 ─────────────────────────
const T1=['A','B','C'],T2=['D','E','F'];
for(let r=0;r<3;r++){const [u,v]=[0,1,2].filter(i=>i!==r);const R1=T1[r],R2=T2[r],U1=T1[u],V1=T1[v],U2=T2[u],V2=T2[v];
  const hyp1=U1+V1,hyp2=U2+V2;
  for(let x=10;x<=80;x++){if(x===45)continue;
    const ws=[W(x,MC.hyp,'wrong-correspondence'),W(180-x,MC.sum,'no-right-angle'),W(90+x,MC.ext,'exterior')];
    // RHA: 빗변 + ∠U. Given ∠V1 → ∠U2 = ∠U1 = 90−x.
    add('rhAngle',amount({kind:'rh-angle',params:{r,cond:'RHA',given:V1,x,ask:U2},prompt:`∠${R1}=∠${R2}=90°인 두 직각삼각형 ABC, DEF에서 ${hyp1}=${hyp2}, ∠${U1}=∠${U2}이다. ∠${V1}=${x}°일 때, ∠${U2}의 크기는 몇 도인지 구하시오.`,answer:90-x,
      explain:`RHA 합동이므로 ∠${U2}=∠${U1}=90°−${x}°=${90-x}°입니다.`,concept:'직각삼각형의 합동 조건',d:2,hi:TRI,wrongs:ws}));
    // RHS: 빗변 + 한 변(R-U). Given ∠U1 → ∠V2 = ∠V1 = 90−x.
    add('rhAngle',amount({kind:'rh-angle',params:{r,cond:'RHS',given:U1,x,ask:V2},prompt:`∠${R1}=∠${R2}=90°인 두 직각삼각형 ABC, DEF에서 ${hyp1}=${hyp2}, ${[R1,U1].sort().join('')}=${[R2,U2].sort((a,b)=>T2.indexOf(a)-T2.indexOf(b)).join('')}이다. ∠${U1}=${x}°일 때, ∠${V2}의 크기는 몇 도인지 구하시오.`,answer:90-x,
      explain:`RHS 합동이므로 ∠${V2}=∠${V1}=90°−${x}°=${90-x}°입니다.`,concept:'직각삼각형의 합동 조건',d:2,hi:TRI,wrongs:ws}));
  }}
const bisPt='∠XOY의 이등분선 위의 한 점 P에서 두 변 OX, OY에 내린 수선의 발을 각각 A, B라고 하자.';
for(let t=10;t<=80;t++)add('angBis',amount({kind:'bis-opa',params:{xoy:2*t},prompt:`${bisPt} ∠XOY=${2*t}°일 때, ∠OPA의 크기는 몇 도인지 구하시오.`,answer:90-t,
  explain:`∠AOP=${2*t}°÷2=${t}°, ∠OAP=90°이므로 ∠OPA=90°−${t}°=${90-t}°입니다.`,concept:'각의 이등분선의 성질',d:2,hi:TRI,
  wrongs:[W(90-2*t,MC.half,'no-halving'),W(t,MC.sum,'copy-half'),W(180-t,MC.sum,'no-right-angle'),W(90+t,MC.ext,'exterior')]}));
for(let t=10;t<=60;t++)add('angBis3',amount({kind:'bis-apb',params:{xoy:2*t},prompt:`${bisPt} ∠XOY=${2*t}°일 때, ∠APB의 크기는 몇 도인지 구하시오.`,answer:180-2*t,
  explain:`□OAPB에서 ∠A=∠B=90°이므로 ∠APB=360°−90°−90°−${2*t}°=${180-2*t}°입니다.`,concept:'각의 이등분선의 성질',d:3,hi:359,
  wrongs:rot([W(90-t,MC.half,'one-triangle'),W(180-t,MC.sum,'one-right-angle'),W(2*t,MC.sum,'opposite-angle-equal'),W(360-2*t,MC.perp,'right-angles-missed'),W(90-2*t,MC.half,'one-right-angle-and-whole-angle')],t%3)}));
for(let t=5;t<=40;t++)add('angBis3',amount({kind:'bis-converse',params:{aop:t},prompt:`∠XOY의 내부의 한 점 P에서 두 변 OX, OY에 내린 수선의 발을 각각 A, B라고 하자. PA=PB이고 ∠AOP=${t}°일 때, ∠XOY의 크기는 몇 도인지 구하시오.`,answer:2*t,
  explain:`△AOP≡△BOP (RHS 합동)이므로 OP는 ∠XOY의 이등분선, ∠XOY=2×${t}°=${2*t}°입니다.`,concept:'각의 이등분선의 성질',d:3,hi:TRI,
  wrongs:[W(t,MC.half,'no-doubling'),W(90-t,MC.sum,'complement'),W(180-2*t,MC.ext,'angle-APB'),W(180-t,MC.sum,'no-right-angle')]}));

// ── 외심 ────────────────────────────────────────────────────
// ∠OAB+∠OBC+∠OCA=90°. The asked base angle is the middle one of the three, so a copied given
// angle sits below the answer and 90°−(one angle) / 180°−(two angles) sit above it.
const circNames={a:['OAB','OBA'],b:['OBC','OCB'],c:['OCA','OAC']};
for(let a=8;a<=72;a+=4)for(let b=9;b<=72;b+=3){const c=90-a-b;if(c<8)continue;
  const vals={a,b,c},keys=['a','b','c'].sort((x,y)=>vals[x]-vals[y]);if(vals[keys[0]]===vals[keys[1]]||vals[keys[1]]===vals[keys[2]])continue;
  const askKey=keys[(a+b)%3],givenKeys=['a','b','c'].filter(k=>k!==askKey);const nm=(k,i)=>circNames[k][(a+b+i)%2];
  const ask=circNames[askKey][(a*b)%2],g=givenKeys.map(k=>vals[k]),ans=vals[askKey];
  add('circSum',amount({kind:'circ-sum',params:{a,b,c,ask:askKey},prompt:`점 O가 △ABC의 외심이고 ∠${nm(givenKeys[0],0)}=${g[0]}°, ∠${nm(givenKeys[1],1)}=${g[1]}°일 때, ∠${ask}의 크기는 몇 도인지 구하시오. ${inside}`,answer:ans,
    explain:`OA=OB=OC이므로 ∠OAB+∠OBC+∠OCA=90°, 구하는 각은 90°−${g[0]}°−${g[1]}°=${ans}°입니다.`,concept:'삼각형의 외심',d:3,hi:TRI,
    wrongs:rot([W(180-g[0]-g[1],MC.sum,'sum-to-180'),W(g[0],MC.center,'copy-given'),W(g[1],MC.center,'copy-other-given'),W(90-g[0],MC.sum,'one-term-only'),W(90-g[1],MC.sum,'other-term-only'),W(2*ans,MC.formula,'whole-angle')],(a*b)%2)}));}
for(let x=10;x<=85;x++)add('circCentral',amount({kind:'circ-boc',params:{A:x},prompt:`점 O가 △ABC의 외심이고 ∠A=${x}°일 때, ∠BOC의 크기는 몇 도인지 구하시오. ${inside}`,answer:2*x,
  explain:`외심에서 ∠BOC=2∠A이므로 ∠BOC=2×${x}°=${2*x}°입니다.`,concept:'삼각형의 외심',d:3,hi:359,
  wrongs:[W(90+x/2,MC.formula,'incenter-formula'),W(x,MC.half,'no-doubling'),W(180-2*x,MC.sum,'supplement'),W(360-2*x,MC.ext,'reflex-angle')]}));
for(let y=20;y<=176;y+=2)add('circCentral',amount({kind:'circ-a-from-boc',params:{BOC:y},prompt:`점 O가 △ABC의 외심이고 ∠BOC=${y}°일 때, ∠A의 크기는 몇 도인지 구하시오. ${inside}`,answer:y/2,
  explain:`∠BOC=2∠A이므로 ∠A=${y}°÷2=${y/2}°입니다.`,concept:'삼각형의 외심',d:3,hi:TRI,
  wrongs:[W(2*(y-90),MC.formula,'incenter-formula'),W(90-y/2,MC.sum,'base-angle'),W(y,MC.half,'no-halving'),W(180-y,MC.sum,'supplement'),W(2*y,MC.half,'doubled-instead')]}));
for(let x=10;x<=84;x+=2)add('circCentral',amount({kind:'circ-obc',params:{A:x},prompt:`점 O가 △ABC의 외심이고 ∠A=${x}°일 때, ∠OBC의 크기는 몇 도인지 구하시오. ${inside}`,answer:90-x,
  explain:`∠BOC=2×${x}°=${2*x}°, OB=OC이므로 ∠OBC=(180°−${2*x}°)÷2=${90-x}°입니다.`,concept:'삼각형의 외심',d:3,hi:TRI,
  wrongs:[W((180-x)/2,MC.formula,'no-doubling'),W(180-2*x,MC.half,'no-halving'),W(x,MC.center,'copy-A'),W(45-x/4,MC.formula,'incenter-formula')]}));
// 직각삼각형의 외심 = 빗변의 중점. A leg is given too (unused data a student may wrongly halve).
const triples=[[3,4,5],[5,12,13],[8,15,17],[7,24,25],[20,21,29],[12,35,37],[9,40,41]];
for(let r=0;r<3;r++){const R=V[r],[P,Q]=rest(R);
  for(const [x,y,z] of triples)for(let k=1;z*k<=100;k++){if((z*k)%2)continue;
    for(const [leg,other,L] of [[x*k,y*k,P],[y*k,x*k,Q]]){const c=z*k;
      add('circRightLen',amount({kind:'circ-right-len',params:{right:R,hyp:c,leg},prompt:`∠${R}=90°인 직각삼각형 ABC의 외심을 O라고 하자. ${P}${Q}=${c} cm, ${[L,R].sort().join('')}=${leg} cm일 때, O${R}의 길이는 몇 cm인지 구하시오.`,answer:c/2,
        explain:`직각삼각형의 외심은 빗변 ${P}${Q}의 중점이므로 O${R}=O${P}=${c}÷2=${c/2} cm입니다.`,concept:'삼각형의 외심',d:2,
        wrongs:rot([W(c,MC.right,'no-halving'),W(leg/2,MC.hyp,'leg-as-diameter'),W(2*c,MC.right,'hypotenuse-as-radius'),W(other/2,MC.hyp,'other-leg-halved'),W(leg,MC.center,'leg-as-radius'),W(other,MC.hyp,'other-leg')],(k+r)%3)}));}}
  // ∠R=90°, ∠P=x: OP=OR=OQ, so △OPR and △OQR are isosceles. The copyable ∠ORP is not asked.
  for(let x=10;x<=80;x++){
    add('circRightAng',amount({kind:'circ-right-exterior',params:{right:R,given:P,x},prompt:`∠${R}=90°인 직각삼각형 ABC의 외심을 O라고 하자. ∠${P}=${x}°일 때, ∠${R}O${Q}의 크기는 몇 도인지 구하시오.`,answer:2*x,
      explain:`O는 빗변의 중점이므로 OP=OR, ∠O${R}${P}=∠${P}=${x}°이고 ∠${R}O${Q}=${x}°+${x}°=${2*x}°입니다.`,concept:'삼각형의 외심',d:3,hi:TRI,
      wrongs:rot([W(x,MC.ext,'one-remote-angle'),W(180-x,MC.sum,'supplement'),W(180-2*x,MC.ext,'interior-for-exterior'),W(90-x,MC.right,'complement'),W(90+x,MC.ext,'right-plus-remote')],x%3)}));
    add('circRightAng',amount({kind:'circ-right-apex',params:{right:R,given:P,x},prompt:`∠${R}=90°인 직각삼각형 ABC의 외심을 O라고 하자. ∠${P}=${x}°일 때, ∠${P}O${R}의 크기는 몇 도인지 구하시오.`,answer:180-2*x,
      explain:`O는 빗변의 중점이므로 OP=OR, △O${P}${R}에서 ∠${P}O${R}=180°−2×${x}°=${180-2*x}°입니다.`,concept:'삼각형의 외심',d:3,hi:TRI,
      wrongs:[W(2*x,MC.ext,'exterior-for-interior'),W(90-x,MC.right,'complement'),W(x,MC.right,'isosceles-missed'),W(180-x,MC.sum,'one-base-angle')]}));
    add('circRightAng',amount({kind:'circ-right-angle',params:{right:R,given:P,x},prompt:`∠${R}=90°인 직각삼각형 ABC의 외심을 O라고 하자. ∠${P}=${x}°일 때, ∠O${R}${Q}의 크기는 몇 도인지 구하시오.`,answer:90-x,
      explain:`OP=OR이므로 ∠O${R}${P}=∠${P}=${x}°, ∠O${R}${Q}=90°−${x}°=${90-x}°입니다.`,concept:'삼각형의 외심',d:3,hi:TRI,
      wrongs:[W(x,MC.right,'copy-given'),W(180-2*x,MC.sum,'apex'),W(2*x,MC.ext,'exterior'),W(180-x,MC.sum,'no-right-angle')]}));
  }}
for(let k=4;k<=40;k+=2)add('circRadius',amount({kind:'circ-radius',params:{oa:k},prompt:`점 O가 △ABC의 외심이고 OA=${k} cm일 때, OB+OC의 길이는 몇 cm인지 구하시오.`,answer:2*k,
  explain:`외심에서 세 꼭짓점까지의 거리는 같으므로 OB+OC=${k}+${k}=${2*k} cm입니다.`,concept:'삼각형의 외심',d:1,
  wrongs:rot([W(k,MC.center,'one'),W(3*k,MC.center,'three'),W(k/2,MC.formula,'diameter-as-radius'),W(4*k,MC.formula,'radius-as-diameter')],(k/2)%2)}));
for(let k=5;k<=30;k++)for(const [m,alt] of [[k+1+(k%5),false],[2*k-1-(k%3),true]]){if(m>=2*k||m===k)continue;
  add('circPer',amount({kind:'circ-obc-perimeter',params:{oa:k,bc:m},prompt:`점 O가 △ABC의 외심이고 OA=${k} cm, BC=${m} cm일 때, △OBC의 둘레의 길이는 몇 cm인지 구하시오.`,answer:2*k+m,
    explain:`OB=OC=OA=${k} cm이므로 △OBC의 둘레는 ${k}+${k}+${m}=${2*k+m} cm입니다.`,concept:'삼각형의 외심',d:2,
    wrongs:alt?[W(k+m,MC.center,'one-radius'),W(2*k,MC.sum,'side-omitted'),W(3*k,MC.equi,'equilateral-assumed')]:[W(k+m,MC.center,'one-radius'),W(2*m+k,MC.center,'distance-to-sides'),W(2*k,MC.sum,'side-omitted'),W(3*k+m,MC.center,'three-radii')]}));}

// ── 내심 ────────────────────────────────────────────────────
for(let x=20;x<=170;x+=2)add('inAngle',amount({kind:'in-bic',params:{A:x},prompt:`점 I가 △ABC의 내심이고 ∠A=${x}°일 때, ∠BIC의 크기는 몇 도인지 구하시오.`,answer:90+x/2,
  explain:`∠BIC=90°+{frac:1/2}∠A=90°+${x/2}°=${90+x/2}°입니다.`,concept:'삼각형의 내심',d:3,hi:TRI,
  wrongs:[W(2*x,MC.formula,'circumcenter-formula'),W(180-x,MC.half,'B+C'),W(90+x,MC.half,'no-halving'),W(90-x/2,MC.sum,'IBC+ICB')]}));
for(let y=95;y<=175;y++)add('inAngle',amount({kind:'in-a-from-bic',params:{BIC:y},prompt:`점 I가 △ABC의 내심이고 ∠BIC=${y}°일 때, ∠A의 크기는 몇 도인지 구하시오.`,answer:2*y-180,
  explain:`∠BIC=90°+{frac:1/2}∠A이므로 ∠A=2×(${y}°−90°)=${2*y-180}°입니다.`,concept:'삼각형의 내심',d:3,hi:TRI,
  wrongs:[W(y-90,MC.half,'no-doubling'),W(y/2,MC.formula,'circumcenter-formula'),W(180-y,MC.sum,'supplement'),W(y,MC.half,'copy-BIC'),W(360-2*y,MC.sum,'B+C')]}));
for(let x=20;x<=170;x+=2)add('inAngle',amount({kind:'in-ibc-icb',params:{A:x},prompt:`점 I가 △ABC의 내심이고 ∠A=${x}°일 때, ∠IBC+∠ICB의 크기는 몇 도인지 구하시오.`,answer:90-x/2,
  explain:`∠IBC+∠ICB={frac:1/2}(∠B+∠C)={frac:1/2}×(180°−${x}°)=${90-x/2}°입니다.`,concept:'삼각형의 내심',d:3,hi:TRI,
  wrongs:[W(180-x,MC.half,'no-halving'),W(90+x/2,MC.formula,'bic'),W(x/2,MC.sum,'half-A'),W(90-x,MC.half,'half-of-180-only')]}));
const inNames={a:['IAB','IAC'],b:['IBC','IBA'],c:['ICA','ICB']};
for(let a=10;a<=70;a+=5)for(let b=8;b<=72;b+=4){const c=90-a-b;if(c<8)continue;
  const vals={a,b,c},keys=['a','b','c'].sort((x,y)=>vals[x]-vals[y]);if(vals[keys[0]]===vals[keys[1]]||vals[keys[1]]===vals[keys[2]])continue;
  const askKey=keys[(a+2*b)%3],givenKeys=['a','b','c'].filter(k=>k!==askKey);const nm=(k,i)=>inNames[k][(a+b+i)%2],g=givenKeys.map(k=>vals[k]),ans=vals[askKey];
  add('inSum',amount({kind:'in-sum',params:{a,b,c,ask:askKey},prompt:`점 I가 △ABC의 내심이고 ∠${nm(givenKeys[0],0)}=${g[0]}°, ∠${nm(givenKeys[1],1)}=${g[1]}°일 때, ∠${inNames[askKey][(a*b)%2]}의 크기는 몇 도인지 구하시오.`,answer:ans,
    explain:`내심은 세 내각의 이등분선의 교점이므로 {frac:1/2}(∠A+∠B+∠C)=90°, 구하는 각은 90°−${g[0]}°−${g[1]}°=${ans}°입니다.`,concept:'삼각형의 내심',d:3,hi:TRI,
    wrongs:rot([W(2*ans,MC.half,'whole-angle'),W(g[0],MC.center,'copy-given'),W(g[1],MC.center,'copy-other-given'),W(180-g[0]-g[1],MC.sum,'sum-to-180'),W(90-g[0],MC.sum,'one-term-only'),W(90-g[1],MC.sum,'other-term-only')],(a*b)%2)}));}
// 내접원의 반지름과 넓이: S = r × (둘레) ÷ 2. A triangle with perimeter p and inradius r exists iff p² ≥ 108r².
for(let r=2;r<=6;r++)for(let p=12;p<=120;p++){if(p*p<=108*r*r)continue;
  if((r*p)%2===0)add('inArea',amount({kind:'in-area',params:{r,p},prompt:`점 I가 △ABC의 내심이고 내접원의 반지름의 길이가 ${r} cm이다. △ABC의 둘레의 길이가 ${p} cm일 때, △ABC의 넓이는 몇 cm²인지 구하시오.`,answer:r*p/2,
    explain:`△ABC=△IAB+△IBC+△ICA={frac:1/2}×${r}×${p}=${r*p/2} cm²입니다.`,concept:'삼각형의 내심',d:4,
    wrongs:rot([W(r*p,MC.half,'no-halving'),W(p/2,MC.formula,'radius-dropped'),W(r*p/4,MC.half,'halved-twice'),W(r*r*p/2,MC.formula,'radius-squared'),W(r+p,MC.formula,'add'),W(p/r,MC.half,'radius-divided'),W(2*r*p,MC.half,'doubled')],p%3)}));
  if(p%2===0)add('inPer',amount({kind:'in-perimeter',params:{r,S:r*p/2},prompt:`점 I가 △ABC의 내심이고 내접원의 반지름의 길이가 ${r} cm이다. △ABC의 넓이가 ${r*p/2} cm²일 때, △ABC의 둘레의 길이는 몇 cm인지 구하시오.`,answer:p,
    explain:`${r*p/2}={frac:1/2}×${r}×(둘레)이므로 둘레는 ${r*p/2}×2÷${r}=${p} cm입니다.`,concept:'삼각형의 내심',d:4,
    wrongs:rot([W(p/2,MC.half,'no-doubling'),W(r*p,MC.formula,'radius-dropped'),W(p/4,MC.half,'halved-twice'),W(r*r*p,MC.formula,'multiply'),W(p/r,MC.formula,'divided-by-radius-twice')],p%3)}));
}
for(const [x,y,z] of triples)for(let k=1;k*z<=100;k++){const a=x*k,b=y*k,c=z*k,r=a*b/(a+b+c);if(r<2)continue;
  const order=k%2?[a,b,c]:[c,a,b];
  add('inRight',amount({kind:'in-right',params:{a,b,c},prompt:`세 변의 길이가 ${order[0]} cm, ${order[1]} cm, ${order[2]} cm인 직각삼각형의 내접원의 반지름의 길이는 몇 cm인지 구하시오.`,answer:r,
    explain:`넓이 {frac:1/2}×${a}×${b}=${a*b/2}={frac:1/2}×r×(${a}+${b}+${c})이므로 r=${r} cm입니다.`,concept:'삼각형의 내심',d:4,
    wrongs:[W(a+b-c,MC.half,'no-halving'),W(r/2,MC.half,'area-not-doubled'),W((b+c-a)/2,MC.hyp,'tangent-from-wrong-vertex'),W(c/2,MC.center,'circumradius'),W((a+c-b)/2,MC.hyp,'tangent-from-other-vertex')]}));}

// ── 선택형: 용어·위치·합동 조건 ─────────────────────────────
const CH='삼각형의 외심';const CI='삼각형의 내심';
const terms=[
  ['삼각형의 세 변의 수직이등분선의 교점을 무엇이라고 하는지 고르시오.','외심',[['내심',MC.center],['무게중심',MC.formula],['수선의 발',MC.perp]],CH,'세 변의 수직이등분선의 교점은 외심입니다.'],
  ['삼각형의 세 내각의 이등분선의 교점을 무엇이라고 하는지 고르시오.','내심',[['외심',MC.center],['무게중심',MC.formula],['수선의 발',MC.perp]],CI,'세 내각의 이등분선의 교점은 내심입니다.'],
  ['삼각형의 외접원의 중심을 무엇이라고 하는지 고르시오.','외심',[['내심',MC.center],['무게중심',MC.formula],['한 변의 중점',MC.right]],CH,'외접원의 중심은 외심입니다.'],
  ['삼각형의 내접원의 중심을 무엇이라고 하는지 고르시오.','내심',[['외심',MC.center],['무게중심',MC.formula],['한 변의 중점',MC.right]],CI,'내접원의 중심은 내심입니다.'],
  ['삼각형의 외심에서 같은 거리에 있는 것을 고르시오.','세 꼭짓점',[['세 변',MC.center],['세 변의 중점',MC.right],['세 내각의 이등분선',MC.formula]],CH,'외심에서 세 꼭짓점까지의 거리가 같습니다(외접원의 반지름).'],
  ['삼각형의 내심에서 같은 거리에 있는 것을 고르시오.','세 변',[['세 꼭짓점',MC.center],['세 변의 중점',MC.right],['세 변의 수직이등분선',MC.formula]],CI,'내심에서 세 변까지의 거리가 같습니다(내접원의 반지름).'],
];
for(const [stem,correct,ws,concept,explain] of terms)add('cTerm',choice({kind:'term',params:{stem},stem,correct,wrongs:ws.map(([text,tag])=>({text,tag})),explain,concept,d:1}));
const LOC=['삼각형의 내부','삼각형의 외부','빗변의 중점','한 꼭짓점'];
for(let a=15;a<=85;a+=5)for(let b=a;b<=150;b+=5){const c=180-a-b;if(c<b||c>150)continue;
  const kind=c<90?0:c===90?2:1;const shown=[[a,b,c],[b,c,a],[c,a,b]][(a+b)%3];
  const correct=LOC[kind],ws=LOC.filter(t=>t!==correct).map(t=>({text:t,tag:t==='삼각형의 내부'?MC.inside:t==='빗변의 중점'?MC.right:MC.center}));
  add('cLoc',choice({kind:'circ-location',params:{angles:[a,b,c]},stem:`세 내각의 크기가 ${shown[0]}°, ${shown[1]}°, ${shown[2]}°인 삼각형의 외심의 위치를 고르시오.`,correct,wrongs:ws,
    explain:c<90?'예각삼각형의 외심은 삼각형의 내부에 있습니다.':c===90?'직각삼각형의 외심은 빗변의 중점입니다.':'둔각삼각형의 외심은 삼각형의 외부에 있습니다.',concept:CH,d:2}));
  if(c>=90&&(a+b)%10===0)add('cLoc',choice({kind:'in-location',params:{angles:[a,b,c]},stem:`세 내각의 크기가 ${shown[0]}°, ${shown[1]}°, ${shown[2]}°인 삼각형의 내심의 위치를 고르시오.`,correct:LOC[0],
    wrongs:LOC.slice(1).map(t=>({text:t,tag:t==='삼각형의 외부'?MC.center:t==='빗변의 중점'?MC.right:MC.formula})),explain:'내심은 세 내각의 이등분선의 교점이므로 항상 삼각형의 내부에 있습니다.',concept:CI,d:2}));
}
// Congruence conditions. Right triangles: every combination of two extra parts (excluding a leg
// with its opposite acute angle, whose name depends on convention). Non-right: SSA traps vs. SAS/ASA/SSS.
const NAMES={RHA:'RHA 합동',RHS:'RHS 합동',SAS:'SAS 합동',ASA:'ASA 합동',SSS:'SSS 합동',NO:'합동이라고 할 수 없다'};
const congWrongs={RHA:[['RHS',MC.hyp],['ASA',MC.ssa],['NO',MC.hyp]],RHS:[['RHA',MC.hyp],['SAS',MC.ssa],['NO',MC.ssa]],SAS:[['RHS',MC.hyp],['SSS',MC.ssa],['NO',MC.ssa]],ASA:[['RHA',MC.hyp],['SAS',MC.ssa],['NO',MC.ssa]],NO:[['SAS',MC.ssa],['RHS',MC.ssa],['SSS',MC.hyp]],SSS:[['SAS',MC.ssa],['RHS',MC.hyp],['NO',MC.ssa]]};
const seg=(p,q,t)=>[p,q].sort((x,y)=>t.indexOf(x)-t.indexOf(y)).join('');
function congItem(premise,parts,ans,d,params){const w=congWrongs[ans].map(([k,tag])=>({text:NAMES[k],tag}));
  add('cCong',choice({kind:'congruence',params,stem:`${premise}${parts.join(', ')}일 때, 두 삼각형의 합동에 대하여 옳은 것을 고르시오.`,correct:NAMES[ans],wrongs:w,
    explain:ans==='NO'?'두 변과 그 끼인각이 아닌 한 각이 같으면 합동이라고 할 수 없습니다.':`${NAMES[ans]}입니다.`,concept:'직각삼각형의 합동 조건',d}));}
for(let r=0;r<3;r++){const [u,v]=[0,1,2].filter(i=>i!==r);const R1=T1[r],R2=T2[r];
  const P=`∠${R1}=∠${R2}=90°인 두 직각삼각형 ABC, DEF에서 `;
  const hyp=`${seg(T1[u],T1[v],T1)}=${seg(T2[u],T2[v],T2)}`,leg=i=>`${seg(T1[r],T1[i],T1)}=${seg(T2[r],T2[i],T2)}`,ang=i=>`∠${T1[i]}=∠${T2[i]}`;
  congItem(P,[hyp,ang(u)],'RHA',2,{right:r,parts:['hyp',`angle${u}`]});congItem(P,[hyp,ang(v)],'RHA',2,{right:r,parts:['hyp',`angle${v}`]});
  congItem(P,[hyp,leg(u)],'RHS',2,{right:r,parts:['hyp',`leg${u}`]});congItem(P,[hyp,leg(v)],'RHS',2,{right:r,parts:['hyp',`leg${v}`]});
  congItem(P,[leg(u),leg(v)],'SAS',2,{right:r,parts:[`leg${u}`,`leg${v}`]});
  congItem(P,[leg(u),ang(u)],'ASA',2,{right:r,parts:[`leg${u}`,`angle${u}`]});congItem(P,[leg(v),ang(v)],'ASA',2,{right:r,parts:[`leg${v}`,`angle${v}`]});
}
const NP='△ABC와 △DEF에서 ';const sd=(i,j)=>`${seg(T1[i],T1[j],T1)}=${seg(T2[i],T2[j],T2)}`,an=i=>`∠${T1[i]}=∠${T2[i]}`;
for(const [s1,s2,common] of [[[0,1],[1,2],1],[[1,2],[2,0],2],[[2,0],[0,1],0]]){
  const others=[0,1,2].filter(i=>i!==common);
  congItem(NP,[sd(...s1),sd(...s2),an(common)],'SAS',3,{right:null,parts:['side','side','included-angle']});
  for(const o of others)congItem(NP,[sd(...s1),sd(...s2),an(o)],'NO',3,{right:null,parts:['side','side','non-included-angle']});
}
for(const [i,j] of [[0,1],[1,2],[2,0]])congItem(NP,[sd(i,j),an(i),an(j)],'ASA',3,{right:null,parts:['side','adjacent-angles']});
congItem(NP,[sd(0,1),sd(1,2),sd(2,0)],'SSS',3,{right:null,parts:['side','side','side']});
const AG=['∠A와 ∠B','∠B와 ∠C','∠A와 ∠C','세 각 모두'],SG=['AB와 AC','AB와 BC','AC와 BC','세 변 모두'];
for(const X of V){const [Y,Z]=rest(X);
  add('cIso',choice({kind:'iso-equal-angles',params:{apex:X},stem:`△ABC에서 ${X}${Y}=${X}${Z}일 때, 크기가 같은 두 각을 고르시오.`,correct:`∠${Y}와 ∠${Z}`,wrongs:AG.filter(t=>t!==`∠${Y}와 ∠${Z}`).map(t=>({text:t,tag:t==='세 각 모두'?MC.equi:MC.apex})),explain:`길이가 같은 두 변 ${X}${Y}, ${X}${Z}의 대각인 ∠${Z}와 ∠${Y}의 크기가 같습니다.`,concept:'이등변삼각형의 성질',d:1}));
  const s=[`${X}${Y}`,`${X}${Z}`].map(p=>p.split('').sort().join(''));const c=`${s[0]}와 ${s[1]}`;
  add('cIso',choice({kind:'iso-equal-sides',params:{apex:X},stem:`△ABC에서 ∠${Y}=∠${Z}일 때, 길이가 같은 두 변을 고르시오.`,correct:c,wrongs:SG.filter(t=>t!==c).map(t=>({text:t,tag:t==='세 변 모두'?MC.equi:MC.apex})),explain:`두 내각이 같은 삼각형은 이등변삼각형이고, ∠${Y}, ∠${Z}의 대변 ${s[1]}, ${s[0]}의 길이가 같습니다.`,concept:'이등변삼각형이 되는 조건',d:1}));
}


const intro=amount({kind:'iso-base',params:{apex:'A',apexAngle:40,ask:'B'},prompt:'AB=AC인 이등변삼각형 ABC에서 ∠A=40°일 때, ∠B의 크기는 몇 도인지 구하시오.',answer:70,
  explain:'두 밑각 ∠B, ∠C의 크기가 같으므로 ∠B=(180°−40°)÷2=70°입니다.',concept:'이등변삼각형의 성질',d:1,hi:TRI,
  wrongs:[W(140,MC.half,'no-halving'),W(40,MC.apex,'apex-as-base'),W(100,MC.apex,'given-as-base')]});
const P=pools;
if(process.env.POOLS)console.log(Object.fromEntries(Object.entries(P).map(([k,v])=>[k,`${v.filter(Boolean).length}/${v.length}`])));
writeGeoPack({id:'m2s2-u1',title:'삼각형의 성질',standards:['[9수03-09]','[9수03-10]'],intro,groups:[
  [P.isoBase,36],[P.isoApex,22],[P.isoBisLen,22],[P.circRadius,12],[P.cTerm,6],[P.cIso,6],
  [P.isoExt,16],[P.isoBisAng,14],[P.isoBisPer,18],[P.rhAngle,8],[P.angBis,8],[P.circRightLen,10],[P.circPer,12],[P.cLoc,12],[P.cCong,20],
  [P.circSum,12],[P.circCentral,24],[P.inAngle,20],[P.inSum,10],[P.angBis3,12],[P.circRightAng,12],
  [P.isoConv,24],[P.inArea,34],[P.inPer,18],[P.inRight,6]]});
