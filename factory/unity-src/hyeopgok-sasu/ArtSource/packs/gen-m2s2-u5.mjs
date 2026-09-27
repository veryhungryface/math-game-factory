#!/usr/bin/env node
// m2s2-u5 피타고라스 정리 ([9수03-15]) — schema v3 (전 문항 4지선다).
// 중2는 제곱근(√) 이전이므로 길이 답은 피타고라스 수(3·4·5, 5·12·13, 8·15·17, 7·24·25, 20·21·29,
// 12·35·37, 9·40·41)와 그 자연수 배만 쓴다. 제곱수가 아닌 값은 길이 대신 「정사각형의 넓이」나
// 「x²의 값」으로 묻는다(expression_traps). 붓기 패드(60) 상한이 사라져 빗변 100 cm 까지 교과서
// 크기를 그대로 쓴다. 오답은 모두 정수이고 아래 오개념 규칙으로만 만든다(무작위 수 없음):
//   lin     a+b=c 로 본다(제곱 없이 더하기·빼기)          sq    x² 에서 멈춘다 / 넓이와 변을 혼동
//   hyp     빗변이 아닌 변을 빗변 자리에(더할 때 빼기·뺄 때 더하기, 빗변을 높이·한 변으로)
//   longest 판별·역에서 가장 긴 변을 빗변으로 두지 않는다   nonright 직각삼각형이 아닌데도 적용
//   halving 삼각형 넓이의 ÷2 누락·밑변 전체 사용            once  두 번 적용할 것을 한 번만 적용
//   outer   정당화 그림에서 바깥 정사각형 넓이를 그대로 답   step  구한 변을 마지막 식에 넣지 않음
import {amount,choice,W,writeGeoPack,drops} from './geo-kit.mjs';
const T='m2s2-u5.';
const MC={hyp:T+'hypotenuse-misplaced',lin:T+'sum-without-squares',nonright:T+'applied-to-non-right',longest:T+'converse-longest-side-ignored',sq:T+'square-root-step-missed',
  half:T+'triangle-halving-missed',once:T+'second-application-missed',outer:T+'outer-square-taken',step:T+'final-step-missed',height:T+'height-difference-ignored'};
const pools={};const add=(k,it)=>{if(it)(pools[k]??=[]).push(it);};
const BASE=[[3,4,5],[5,12,13],[8,15,17],[7,24,25],[20,21,29],[12,35,37],[9,40,41]];
const TR=[];for(const [a,b,c] of BASE)for(let k=1;c*k<=100;k++)TR.push([a*k,b*k,c*k]);
TR.sort((p,q)=>p[2]-q[2]||p[0]-q[0]);
const PY='피타고라스 정리',CV='피타고라스 정리의 역',APP='피타고라스 정리의 활용',JUS='피타고라스 정리의 정당화';
const isSq=n=>Number.isInteger(Math.sqrt(n));const rt=n=>isSq(n)?Math.sqrt(n):0;
const rot=(arr,i)=>arr.map((_,k)=>arr[(k+i)%arr.length]); // rotate priorities so option sets vary
// geo-kit spreads the value rank of the answer per kind (first wrong fixed, two more from the next five).
const amt=amount;
// 두 변에서 빗변 c 구하기(legs x<y 또는 y<x): 오답 후보
const hypWrongs=(x,y,c,i)=>rot([W(x+y,MC.lin,'sum'),W(Math.abs(y-x),MC.hyp,'difference'),W(c*c,MC.sq,'x2-not-rooted'),W(Math.max(x,y),MC.hyp,'longest-leg-as-hypotenuse'),W(rt(Math.abs(y*y-x*x)),MC.hyp,'subtracted-squares'),W(Math.abs(y*y-x*x),MC.hyp,'subtracted-squares-not-rooted')],i%2?1:0);
// 빗변 c 와 한 변 x 에서 나머지 y 구하기
const legWrongs=(c,x,y,i)=>rot([W(c-x,MC.lin,'difference'),W(c+x,MC.hyp,'added-instead'),W(y*y,MC.sq,'x2-not-rooted'),W((c-x)**2,MC.lin,'square-of-difference'),W(c*c+x*x,MC.hyp,'added-squares-not-rooted')],i%3);

// ── d1 빗변·다른 한 변 ──────────────────────────────────────────
TR.forEach(([a,b,c],i)=>{if(c>50)return;[[a,b],[b,a]].forEach(([x,y],j)=>{
  add('hyp',amt({kind:'hyp',params:{a:x,b:y},prompt:`직각을 낀 두 변의 길이가 ${x} cm, ${y} cm인 직각삼각형의 빗변의 길이는 몇 cm인지 구하시오.`,answer:c,
    explain:`빗변의 길이를 x cm라 하면 x²=${x}²+${y}²=${c*c}, x>0이므로 x=${c}입니다.`,concept:PY,d:1,wrongs:hypWrongs(x,y,c,i+j)}));
  add('leg',amt({kind:'leg',params:{c,a:x},prompt:`빗변의 길이가 ${c} cm이고 다른 한 변의 길이가 ${x} cm인 직각삼각형의 나머지 한 변의 길이는 몇 cm인지 구하시오.`,answer:y,
    explain:`나머지 한 변을 x cm라 하면 x²=${c}²−${x}²=${y*y}, x>0이므로 x=${y}입니다.`,concept:PY,d:1,wrongs:legWrongs(c,x,y,i+j)}));});});
const V3=['A','B','C'];const sideOpp={A:'BC',B:'AC',C:'AB'};
const legsOf=R=>{const [P,Q]=V3.filter(v=>v!==R);return [[P,R].sort().join(''),[Q,R].sort().join('')];};
// ── d1 정사각형의 넓이(빗변 위) — 세 정사각형이 모두 피타고라스 수의 제곱 ──
const sqStem=R=>`∠${R}=90°인 직각삼각형 ABC의 세 변을 각각 한 변으로 하는 정사각형을 그렸다.`;
TR.forEach(([a,b,c],i)=>{if(c>30)return;for(const R of V3){const [l1,l2]=legsOf(R),hyp=sideOpp[R];for(const [p,s] of [[a*a,b*b],[b*b,a*a]]){
  add('sqHyp',amt({kind:'sq-hyp',params:{right:R,[l1]:p,[l2]:s},prompt:`${sqStem(R)} ${l1}, ${l2} 위의 정사각형의 넓이가 각각 ${p} cm², ${s} cm²일 때, ${hyp} 위의 정사각형의 넓이는 몇 cm²인지 구하시오.`,answer:c*c,
    explain:`${hyp}²=${l1}²+${l2}²이므로 ${hyp} 위의 정사각형의 넓이는 ${p}+${s}=${c*c} cm²입니다.`,concept:JUS,d:1,
    wrongs:rot([W(c,MC.sq,'side-not-area'),W(Math.abs(s-p),MC.hyp,'difference'),W((a+b)**2,MC.lin,'square-of-sum'),W(a+b,MC.lin,'sides-added'),W(p*p+s*s,MC.sq,'areas-squared-again')],(i+V3.indexOf(R))%3)}));}}});
// ── d2 이름 붙은 변 ─────────────────────────────────────────────
TR.forEach(([a,b,c],i)=>{if(c>75)return;for(const R of V3){const [l1,l2]=legsOf(R),hyp=sideOpp[R];const r=V3.indexOf(R);
  if((i+r)%2===0)add('named',amt({kind:'named-hyp',params:{right:R,[l1]:a,[l2]:b},prompt:`∠${R}=90°인 직각삼각형 ABC에서 ${l1}=${a} cm, ${l2}=${b} cm일 때, ${hyp}의 길이는 몇 cm인지 구하시오.`,answer:c,
    explain:`∠${R}의 대변 ${hyp}가 빗변이므로 ${hyp}²=${a}²+${b}²=${c*c}, ${hyp}=${c} cm입니다.`,concept:PY,d:2,wrongs:hypWrongs(a,b,c,i+r+1)}));
  const [g,ask,gv,av]=(i+r)%3?[l1,l2,a,b]:[l2,l1,b,a];
  if((i+r)%2)add('named',amt({kind:'named-leg',params:{right:R,hyp:c,[g]:gv},prompt:`∠${R}=90°인 직각삼각형 ABC에서 ${hyp}=${c} cm, ${g}=${gv} cm일 때, ${ask}의 길이는 몇 cm인지 구하시오.`,answer:av,
    explain:`빗변은 ${hyp}이므로 ${ask}²=${c}²−${gv}²=${av*av}, ${ask}=${av} cm입니다.`,concept:PY,d:2,wrongs:legWrongs(c,gv,av,i+r)}));}});
// ── d2 정사각형의 넓이(다리 위) · 변의 길이로 넓이 ─────────────────────
TR.forEach(([a,b,c],i)=>{if(c>30)return;for(const R of V3){const [l1,l2]=legsOf(R),hyp=sideOpp[R];for(const [giv,ask,p,q] of [[l1,l2,a,b],[l2,l1,b,a]]){
  add('sqLeg',amt({kind:'sq-leg',params:{right:R,hypArea:c*c,[giv]:p*p},prompt:`${sqStem(R)} ${hyp}, ${giv} 위의 정사각형의 넓이가 각각 ${c*c} cm², ${p*p} cm²일 때, ${ask} 위의 정사각형의 넓이는 몇 cm²인지 구하시오.`,answer:q*q,
    explain:`${hyp}가 빗변이므로 ${ask} 위의 정사각형의 넓이는 ${c*c}−${p*p}=${q*q} cm²입니다.`,concept:JUS,d:2,
    wrongs:rot([W(c*c+p*p,MC.hyp,'added-instead'),W(q,MC.sq,'side-not-area'),W((c-p)**2,MC.lin,'square-of-difference'),W(c-p,MC.sq,'difference-not-squared'),W((c+p)**2,MC.hyp,'square-of-sum')],(i+V3.indexOf(R))%3)}));}}});
for(const R of V3){const [l1,l2]=legsOf(R),hyp=sideOpp[R];
  for(let a=2;a<=12;a++)for(let b=a+1;b<=13;b++){const i=a+b+V3.indexOf(R);if(i%3)continue;const [u,v]=i%2?[a,b]:[b,a];
    add('sqSides',amt({kind:'sq-from-legs',params:{right:R,[l1]:u,[l2]:v},prompt:`${sqStem(R)} ${l1}=${u} cm, ${l2}=${v} cm일 때, ${hyp} 위의 정사각형의 넓이는 몇 cm²인지 구하시오.`,answer:a*a+b*b,
      explain:`${hyp} 위의 정사각형의 넓이는 ${hyp}²=${u}²+${v}²=${a*a+b*b} cm²입니다.`,concept:JUS,d:2,
      wrongs:rot([W((a+b)**2,MC.lin,'square-of-sum'),W(b*b-a*a,MC.hyp,'difference'),W(2*(a+b),MC.sq,'doubled-not-squared'),W(a+b,MC.lin,'sides-added')],i%3)}));}
  for(let c=5;c<=15;c++)for(let a=2;a<c;a++){const i=c+a+V3.indexOf(R);if(i%4)continue;const [giv,ask]=i%8?[l1,l2]:[l2,l1];
    add('sqSides',amt({kind:'sq-from-hyp',params:{right:R,hyp:c,[giv]:a},prompt:`${sqStem(R)} ${hyp}=${c} cm, ${giv}=${a} cm일 때, ${ask} 위의 정사각형의 넓이는 몇 cm²인지 구하시오.`,answer:c*c-a*a,
      explain:`${hyp}가 빗변이므로 ${ask} 위의 정사각형의 넓이는 ${c}²−${a}²=${c*c-a*a} cm²입니다.`,concept:JUS,d:2,
      wrongs:rot([W(c*c+a*a,MC.hyp,'added-instead'),W((c-a)**2,MC.lin,'square-of-difference'),W(c-a,MC.sq,'difference-not-squared'),W((c+a)**2,MC.hyp,'square-of-sum'),W(c*c,MC.hyp,'hyp-square-taken')],i%3)}));}}
// ── d2 직사각형의 대각선 ────────────────────────────────────────
TR.forEach(([a,b,c],i)=>{if(c>100)return;[[a,b],[b,a]].forEach(([x,y],j)=>{
  if((i+j)%2===0)add('rect',amt({kind:'rect-diag',params:{w:x,h:y},prompt:`가로의 길이가 ${x} cm, 세로의 길이가 ${y} cm인 직사각형의 대각선의 길이는 몇 cm인지 구하시오.`,answer:c,
    explain:`대각선은 직각삼각형의 빗변이므로 ${x}²+${y}²=${c*c}=${c}²에서 ${c} cm입니다.`,concept:APP,d:2,wrongs:hypWrongs(x,y,c,i+j+1)}));
  else add('rect',amt({kind:'rect-side',params:{d:c,w:x},prompt:`대각선의 길이가 ${c} cm이고 가로의 길이가 ${x} cm인 직사각형의 세로의 길이는 몇 cm인지 구하시오.`,answer:y,
    explain:`세로²=${c}²−${x}²=${y*y}이므로 세로는 ${y} cm입니다.`,concept:APP,d:2,wrongs:legWrongs(c,x,y,i+j+2)}));});});

// ── d3 x² 의 값 ────────────────────────────────────────────────
for(let a=2;a<=12;a++)for(let b=a+1;b<=14;b++){const s=a*a+b*b;if(isSq(s))continue;const [u,v]=(a+b)%2?[a,b]:[b,a];
  add('xsq',amt({kind:'xsq-hyp',params:{a:u,b:v},prompt:`직각을 낀 두 변의 길이가 ${u} cm, ${v} cm인 직각삼각형의 빗변의 길이를 x cm라고 할 때, x²의 값을 구하시오.`,answer:s,
    explain:`x²=${u}²+${v}²=${u*u}+${v*v}=${s}입니다.`,concept:PY,d:3,wrongs:rot([W(a+b,MC.lin,'sum'),W((a+b)**2,MC.lin,'square-of-sum'),W(b*b-a*a,MC.hyp,'difference'),W(2*(a+b),MC.sq,'doubled-not-squared')],(a*b)%3)}));}
for(let c=3;c<=16;c++)for(let a=1;a<c;a++){const s=c*c-a*a;if(isSq(s))continue;
  add('xsq',amt({kind:'xsq-leg',params:{c,a},prompt:`빗변의 길이가 ${c} cm이고 다른 한 변의 길이가 ${a} cm인 직각삼각형의 나머지 한 변의 길이를 x cm라고 할 때, x²의 값을 구하시오.`,answer:s,
    explain:`x²=${c}²−${a}²=${c*c}−${a*a}=${s}입니다.`,concept:PY,d:3,wrongs:rot([W(c*c+a*a,MC.hyp,'added-instead'),W(c-a,MC.lin,'difference'),W((c-a)**2,MC.lin,'square-of-difference'),W(2*(c-a),MC.sq,'doubled-not-squared'),W((c+a)**2,MC.hyp,'square-of-sum')],(c+a)%3)}));}
// ── d3 활용: 직사각형의 넓이, 이등변삼각형의 높이, 생활 장면, 역 ───────────────
TR.forEach(([a,b,c],i)=>{[[a,b],[b,a]].forEach(([x,y],j)=>{if(c>65)return;
  add('rect3',amt({kind:'rect-area',params:{d:c,w:x},prompt:`대각선의 길이가 ${c} cm이고 가로의 길이가 ${x} cm인 직사각형의 넓이는 몇 cm²인지 구하시오.`,answer:x*y,
    explain:`세로²=${c}²−${x}²=${y*y}, 세로=${y} cm이므로 넓이는 ${x}×${y}=${x*y} cm²입니다.`,concept:APP,d:3,
    wrongs:rot([W(x*c,MC.hyp,'diagonal-as-side'),W(x*(c-x),MC.lin,'difference-side'),W(x*y%2?0:x*y/2,MC.half,'halved-rectangle'),W(2*(x+y),MC.step,'perimeter-for-area')],(i+j)%3)}));});});
TR.forEach(([a,b,c],i)=>{[[a,b],[b,a]].forEach(([x,y],j)=>{if(c>70)return;
  add('iso',amt({kind:'iso-height',params:{side:c,base:2*x},prompt:`AB=AC=${c} cm, BC=${2*x} cm인 이등변삼각형 ABC의 꼭짓점 A에서 BC에 내린 수선의 발을 D라고 할 때, AD의 길이는 몇 cm인지 구하시오.`,answer:y,
    explain:`BD=${2*x}÷2=${x} cm이므로 AD²=${c}²−${x}²=${y*y}, AD=${y} cm입니다.`,concept:APP,d:3,
    wrongs:rot([W(c>2*x?rt(c*c-4*x*x):0,MC.half,'whole-base'),W(c-x,MC.lin,'difference'),W(y*y,MC.sq,'x2-not-rooted'),W(c+x,MC.hyp,'added-instead')],(i+j)%2)}));});});
const CTX=[
  (a,b,c)=>[`길이가 ${c} m인 사다리를 벽에 기대어 세웠더니 사다리의 아래쪽 끝이 벽에서 ${a} m 떨어져 있었다. 바닥에서 사다리의 위쪽 끝까지의 높이는 몇 m인지 구하시오. (단, 벽은 바닥과 수직이다.)`,b,'ladder',legWrongs(c,a,b,a+b)],
  (a,b,c)=>[`가로가 ${a} m, 세로가 ${b} m인 직사각형 모양의 공원을 한 꼭짓점에서 마주 보는 꼭짓점까지 대각선을 따라 곧게 가로질러 간 거리는 몇 m인지 구하시오.`,c,'park',hypWrongs(a,b,c,a+b)],
  (a,b,c)=>[`높이가 ${b} m인 깃대의 꼭대기에서 땅 위의 한 점까지 줄을 팽팽하게 연결했다. 그 점이 깃대의 밑에서 ${a} m 떨어져 있을 때, 줄의 길이는 몇 m인지 구하시오. (단, 깃대는 땅과 수직이다.)`,c,'pole',hypWrongs(a,b,c,a+b+1)],
  (a,b,c)=>[`평평한 땅 위에서 ${a} m 떨어진 두 기둥의 높이가 각각 ${b+1} m, 1 m이다. 두 기둥의 꼭대기를 곧게 잇는 줄의 길이는 몇 m인지 구하시오. (단, 두 기둥은 땅과 수직이다.)`,c,'poles',
    rot([W(a+b,MC.lin,'sum'),W(c*c,MC.sq,'x2-not-rooted'),W(a+b+1,MC.height,'full-height-linear'),W(rt(a*a+(b+1)**2),MC.height,'full-height')],(a+b)%2)],
];
for(const [a,b,c] of TR){if(c>40)continue;CTX.forEach(f=>{for(const [x,y] of [[a,b],[b,a]]){const [prompt,ans,kind,wrongs]=f(x,y,c);
  add('ctx',amt({kind:`ctx-${kind}`,params:{x,y,c},prompt,answer:ans,explain:ans===c?(kind==='poles'?`높이의 차가 ${y+1}−1=${y} m이므로 줄²=${x}²+${y}²=${c*c}, ${c} m입니다.`:`직각삼각형의 빗변이므로 ${x}²+${y}²=${c*c}=${c}², ${c} m입니다.`):`높이를 h m라 하면 h²=${c}²−${x}²=${y*y}, h=${y} m입니다.`,concept:APP,d:3,wrongs}));}});}
TR.forEach(([a,b,c],i)=>{if(c>80)return;
  add('conv',amt({kind:'conv-hyp',params:{a,b},prompt:`세 변의 길이가 ${a} cm, ${b} cm, x cm인 삼각형이 직각삼각형이고 x cm가 가장 긴 변의 길이일 때, x의 값을 구하시오.`,answer:c,
    explain:`가장 긴 변이 빗변이므로 x²=${a}²+${b}²=${c*c}, x=${c}입니다.`,concept:CV,d:3,wrongs:rot([W(a+b,MC.lin,'sum'),W(c*c,MC.sq,'x2-not-rooted'),W(b-a,MC.longest,'difference'),W(b*b-a*a,MC.longest,'subtracted-squares-not-rooted'),W(b,MC.longest,'longest-given-as-x')],i%3)}));
  add('conv',amt({kind:'conv-leg',params:{a,c},prompt:`세 변의 길이가 ${a} cm, ${c} cm, x cm인 삼각형이 직각삼각형이고 ${c} cm가 가장 긴 변의 길이일 때, x의 값을 구하시오.`,answer:b,
    explain:`${c} cm가 빗변이므로 x²=${c}²−${a}²=${b*b}, x=${b}입니다.`,concept:CV,d:3,wrongs:rot([W(c+a,MC.longest,'x-as-hypotenuse-linear'),W(c-a,MC.lin,'difference'),W(b*b,MC.sq,'x2-not-rooted'),W(c*c+a*a,MC.longest,'x-as-hypotenuse-not-rooted')],i%3)}));});

// ── d4 여러 단계 ────────────────────────────────────────────────
TR.forEach(([a,b,c],i)=>{if(c>65)return;[[a,b],[b,a]].forEach(([x,y],j)=>{const k=i+j;
  if(k%3===0)add('app4',amt({kind:'rect-perimeter',params:{d:c,w:x},prompt:`대각선의 길이가 ${c} cm이고 가로의 길이가 ${x} cm인 직사각형의 둘레의 길이는 몇 cm인지 구하시오.`,answer:2*(x+y),
    explain:`세로²=${c}²−${x}²=${y*y}, 세로=${y} cm이므로 둘레는 2×(${x}+${y})=${2*(x+y)} cm입니다.`,concept:APP,d:4,
    wrongs:rot([W(2*(x+c),MC.hyp,'diagonal-as-side'),W(x+y,MC.step,'half-perimeter'),W(2*c,MC.lin,'difference-side'),W(x+y+c,MC.step,'triangle-perimeter'),W(x*y,MC.step,'area-for-perimeter')],k%3)}));
  if(k%3===1&&x*y%2===0)add('app4',amt({kind:'right-area',params:{c,a:x},prompt:`빗변의 길이가 ${c} cm이고 다른 한 변의 길이가 ${x} cm인 직각삼각형의 넓이는 몇 cm²인지 구하시오.`,answer:x*y/2,
    explain:`나머지 한 변은 ${y} cm(${c}²−${x}²=${y*y})이므로 넓이는 ${x}×${y}÷2=${x*y/2} cm²입니다.`,concept:APP,d:4,
    wrongs:rot([W(x*y,MC.half,'halving-missed'),W(x*c%2?0:x*c/2,MC.hyp,'hyp-as-height'),W(x*(c-x)%2?0:x*(c-x)/2,MC.lin,'difference-side'),W(x*c,MC.hyp,'hyp-as-height-no-halving'),W(x+y+c,MC.step,'perimeter-for-area')],k%2)}));
  if(k%3===2)add('app4',amt({kind:'right-perimeter',params:{c,a:x},prompt:`빗변의 길이가 ${c} cm이고 다른 한 변의 길이가 ${x} cm인 직각삼각형의 둘레의 길이는 몇 cm인지 구하시오.`,answer:x+y+c,
    explain:`나머지 한 변은 ${y} cm(${c}²−${x}²=${y*y})이므로 둘레는 ${x}+${y}+${c}=${x+y+c} cm입니다.`,concept:APP,d:4,
    wrongs:rot([W(2*c,MC.lin,'difference-side'),W(x+y,MC.step,'hypotenuse-left-out'),W(x+c+c+x,MC.hyp,'added-instead'),W(x+c,MC.step,'third-side-missing'),W(x*y%2?0:x*y/2,MC.step,'area-for-perimeter')],k%3)}));});});
TR.forEach(([a,b,c],i)=>{[[a,b],[b,a]].forEach(([x,y],j)=>{if(c>60)return;
  add('iso4',amt({kind:'iso-area',params:{side:c,base:2*x},prompt:`AB=AC=${c} cm, BC=${2*x} cm인 이등변삼각형 ABC의 넓이는 몇 cm²인지 구하시오.`,answer:x*y,
    explain:`높이²=${c}²−${x}²=${y*y}, 높이=${y} cm이므로 넓이는 ${2*x}×${y}÷2=${x*y} cm²입니다.`,concept:APP,d:4,
    wrongs:rot([W(2*x*y,MC.half,'halving-missed'),W(x*c,MC.hyp,'side-as-height'),W(x*(c-x),MC.lin,'difference-height'),W(2*x*c,MC.hyp,'side-as-height-no-halving'),W(2*x+2*c,MC.step,'perimeter-for-area')],(i+j)%3)}));});});
// 두 번 적용: 공통인 높이 AD
const legs=new Map();for(const [a,b,c] of TR)for(const [h,x] of [[a,b],[b,a]]){if(!legs.has(h))legs.set(h,[]);legs.get(h).push([x,c]);}
for(const [h,list] of legs)for(let i=0;i<list.length;i++)for(let j=0;j<list.length;j++){if(i===j)continue;const [x1,c1]=list[i],[x2,c2]=list[j];if(c1>75||c2>75)continue;const k=h+i+j;
  add('two',amt({kind:'two-steps-bc',params:{AB:c1,AD:h,AC:c2},prompt:`△ABC의 꼭짓점 A에서 BC에 내린 수선의 발 D가 변 BC 위에 있다. AB=${c1} cm, AD=${h} cm, AC=${c2} cm일 때, BC의 길이는 몇 cm인지 구하시오.`,answer:x1+x2,
    explain:`BD²=${c1}²−${h}²=${x1*x1}, DC²=${c2}²−${h}²=${x2*x2}이므로 BC=${x1}+${x2}=${x1+x2} cm입니다.`,concept:APP,d:4,
    wrongs:rot([W(c1+c2,MC.lin,'add-sides'),W(x1,MC.once,'one-part-only'),W(x2,MC.once,'one-part-only'),W(Math.abs(x1-x2),MC.lin,'foot-outside'),W(c1+c2-2*h,MC.lin,'no-squares')],k%3)}));
  add('two',amt({kind:'two-steps-ac',params:{AB:c1,BD:x1,DC:x2},prompt:`△ABC의 꼭짓점 A에서 BC에 내린 수선의 발 D가 변 BC 위에 있다. AB=${c1} cm, BD=${x1} cm, DC=${x2} cm일 때, AC의 길이는 몇 cm인지 구하시오.`,answer:c2,
    explain:`AD²=${c1}²−${x1}²=${h*h}, AC²=${h*h}+${x2}²=${c2*c2}이므로 AC=${c2} cm입니다.`,concept:APP,d:4,
    wrongs:rot([W(h,MC.once,'stopped-at-AD'),W(c1-x1+x2,MC.lin,'no-squares'),W(c2*c2,MC.sq,'x2-not-rooted'),W(h+x2,MC.lin,'sum')],k%3)}));}
// ── d4 정당화(넓이) ─────────────────────────────────────────────
for(let a=1;a<=12;a++)for(let b=a+1;b<=14;b++)add('jus',amt({kind:'jus-outer',params:{a,b},prompt:`직각을 낀 두 변의 길이가 ${a} cm, ${b} cm인 합동인 직각삼각형 4개를 한 변의 길이가 ${a+b} cm인 정사각형의 네 귀퉁이에 겹치지 않게 놓았더니, 가운데에 네 빗변으로 둘러싸인 정사각형이 생겼다. 이 정사각형의 넓이는 몇 cm²인지 구하시오.`,answer:a*a+b*b,
  explain:`(${a}+${b})²−4×{frac:1/2}×${a}×${b}=${(a+b)**2}−${2*a*b}=${a*a+b*b} cm²이고, 이는 ${a}²+${b}²과 같습니다.`,concept:JUS,d:4,
  wrongs:rot([W((a+b)**2,MC.outer,'outer-square'),W(2*a*b,MC.lin,'triangles-only'),W((b-a)**2,MC.half,'triangle-halving-missed'),W(a+b,MC.sq,'side-not-area'),W((a+b)**2+2*a*b,MC.half,'triangles-added'),W((a+b)**2-a*b,MC.half,'one-triangle-per-pair')],(a+b)%3)}));
for(const [a,b,c] of TR){if(c>65)continue;const s=(b-a)**2;
  add('jus',amt({kind:'jus-inner',params:{a,b,c},prompt:`세 변의 길이가 ${a} cm, ${b} cm, ${c} cm인 합동인 직각삼각형 4개를 빗변이 바깥쪽에 오도록 겹치지 않게 모아 한 변의 길이가 ${c} cm인 정사각형을 만들었더니, 가운데에 작은 정사각형이 생겼다. 작은 정사각형의 넓이는 몇 cm²인지 구하시오.`,answer:s,
    explain:`작은 정사각형의 넓이는 ${c}²−4×{frac:1/2}×${a}×${b}=${c*c}−${2*a*b}=${s} cm²이고, 한 변은 ${b}−${a}=${b-a} cm입니다.`,concept:JUS,d:4,
    wrongs:[W(b-a,MC.sq,'side-not-area'),W(c*c,MC.outer,'outer-square'),W(2*a*b,MC.lin,'triangles-only'),W(c*c-a*b,MC.half,'one-half-used-once')]}));}

// ── 선택형 ──────────────────────────────────────────────────
const tri=(x,y,z)=>{const s=[x,y,z].sort((p,q)=>p-q);return s[0]+s[1]>s[2];};const isRight=(x,y,z)=>{const s=[x,y,z].sort((p,q)=>p-q);return s[0]**2+s[1]**2===s[2]**2;};
const fmt=t=>t.join(', ');
const near=([a,b,c])=>[[a,b,c+1],[a+1,b+1,c+1],[a,b+1,c],[a-1,b,c],[a+1,b,c+1]].filter(t=>t.every(v=>v>=2)&&tri(...t)&&!isRight(...t));
TR.filter(([,,c])=>c<=30).forEach(([a,b,c],i)=>{const order=[[a,b,c],[c,a,b],[b,c,a]][i%3];const ws=near([a,b,c]).slice(i%2,i%2+3);if(ws.length<3)return;
  add('cJudge',choice({kind:'judge-right',params:{t:[a,b,c]},style:'labels',stem:'세 변의 길이가 다음과 같은 삼각형 중 직각삼각형인 것을 고르시오.',tail:'(단, 단위는 cm이다.)',correct:fmt(order),wrongs:ws.map((w,k)=>({text:fmt(w),tag:k===1?MC.lin:MC.longest})),
    explain:L=>`${L}에서 가장 긴 변 ${c}에 대하여 ${a}²+${b}²=${c*c}=${c}²이므로 직각삼각형입니다.`,concept:CV,d:2}));});
const small=TR.filter(([,,c])=>c<=26);small.forEach(([a,b,c],i)=>{const others=[1,2,3].map(t=>small[(i+t*2)%small.length]);if(new Set(others.map(String)).size<3||others.some(o=>String(o)===String([a,b,c])))return;
  const bad=near([a,b,c])[i%2];if(!bad)return;
  add('cJudge',choice({kind:'judge-not-right',params:{bad,right:others},style:'labels',stem:'세 변의 길이가 다음과 같은 삼각형 중 직각삼각형이 아닌 것을 고르시오.',tail:'(단, 단위는 cm이다.)',correct:fmt(bad),wrongs:others.map((w,k)=>({text:fmt(k%2?[w[2],w[0],w[1]]:w),tag:k%2?MC.longest:MC.lin})),
    explain:L=>{const s=[...bad].sort((p,q)=>p-q);return `${L}에서 ${s[0]}²+${s[1]}²=${s[0]**2+s[1]**2}이고 ${s[2]}²=${s[2]**2}이므로 직각삼각형이 아닙니다.`;},concept:CV,d:2}));});
for(const names of ['ABC','DEF'])for(const R of names){const [P,Q]=[...names].filter(v=>v!==R);const sd=(x,y)=>[x,y].sort().join('');const hyp=sd(P,Q),l1=sd(P,R),l2=sd(Q,R);
  add('cHyp',choice({kind:'hyp-name',params:{names,R},stem:`∠${R}=90°인 직각삼각형 ${names}에서 빗변을 고르시오.`,correct:hyp,wrongs:[{text:l1,tag:MC.hyp},{text:l2,tag:MC.hyp},{text:'알 수 없다',tag:MC.nonright}],explain:`직각의 대변 ${hyp}가 빗변입니다.`,concept:PY,d:1}));
  add('cHyp',choice({kind:'relation',params:{names,R},stem:`∠${R}=90°인 직각삼각형 ${names}에서 항상 성립하는 식을 고르시오.`,correct:`${hyp}²=${l1}²+${l2}²`,wrongs:[{text:`${l1}²=${hyp}²+${l2}²`,tag:MC.hyp},{text:`${l2}²=${hyp}²+${l1}²`,tag:MC.hyp},{text:`${hyp}=${l1}+${l2}`,tag:MC.lin}],explain:`빗변 ${hyp}의 제곱이 나머지 두 변의 제곱의 합과 같습니다.`,concept:PY,d:1}));}

// 첫 문항: 교과서 첫 예제(정사각형 넓이 9, 16 → 25).
const intro=amount({kind:'sq-hyp',params:{right:'C',BC:9,AC:16},prompt:`${sqStem('C')} BC, AC 위의 정사각형의 넓이가 각각 9 cm², 16 cm²일 때, AB 위의 정사각형의 넓이는 몇 cm²인지 구하시오.`,answer:25,
  explain:'AB가 빗변이므로 AB 위의 정사각형의 넓이는 9+16=25 cm²입니다.',concept:JUS,d:1,wrongs:[W(5,MC.sq,'side-not-area'),W(7,MC.hyp,'difference'),W(49,MC.lin,'square-of-sum')]});
// Rank-aware selection: geo-kit balances the answer's value rank inside each kind's whole pool, but
// a stride subsample can alias with that cycle. Each group therefore takes its n items greedily,
// always preferring the value rank (how many wrongs are smaller than the answer) that is rarest in
// the pack so far, and within that rank the item nearest to the even-stride position.
const gRank=[0,0,0,0];
function sel(pool,n){const items=(pool??[]).filter(it=>it&&it.prompt!==intro.prompt);if(items[0]?.mode!=='int')return items;
  const rank=it=>it.wrongs.filter(w=>w.value<it.answer).length,left=items.map((it,i)=>({it,i,r:rank(it)})),out=[];
  for(let t=0;t<n&&left.length;t++){const pos=t*items.length/n;const avail=[...new Set(left.map(x=>x.r))];const r=avail.sort((x,y)=>gRank[x]-gRank[y]||x-y)[0];
    let best=null;for(const x of left)if(x.r===r&&(!best||Math.abs(x.i-pos)<Math.abs(best.i-pos)))best=x;
    left.splice(left.indexOf(best),1);gRank[r]++;out.push(best.it);}
  return out.sort((x,y)=>items.indexOf(x)-items.indexOf(y));}
const P=pools;
if(process.env.POOLS){console.log(Object.fromEntries(Object.entries(P).map(([k,v])=>[k,v.length])));console.log('drops',drops);}
writeGeoPack({id:'m2s2-u5',title:'피타고라스 정리',standards:['[9수03-15]'],intro,groups:[
  [sel(P.hyp,26),26],[sel(P.leg,26),26],[sel(P.cHyp,12),12],[sel(P.sqHyp,26),26],
  [sel(P.named,24),24],[sel(P.sqLeg,18),18],[sel(P.sqSides,22),22],[sel(P.rect,12),12],[sel(P.cJudge,16),16],
  [sel(P.xsq,34),34],[sel(P.rect3,12),12],[sel(P.iso,12),12],[sel(P.ctx,18),18],[sel(P.conv,14),14],
  [sel(P.app4,26),26],[sel(P.iso4,14),14],[sel(P.two,30),30],[sel(P.jus,22),22]]});
