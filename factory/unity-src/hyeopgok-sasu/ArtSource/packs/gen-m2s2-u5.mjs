#!/usr/bin/env node
// m2s2-u5 피타고라스 정리 ([9수03-15]). 중2는 제곱근(√) 이전이므로 길이 답은 피타고라스 수
// (3·4·5, 5·12·13, 8·15·17, 7·24·25, 20·21·29, 12·35·37, 9·40·41)와 그 자연수 배만 쓴다.
// 제곱수가 아닌 값은 길이 대신 「정사각형의 넓이」나 「x²의 값」으로 묻는다(expression_traps).
import {amount,choice,W,writeGeoPack} from './geo-kit.mjs';
const T='m2s2-u5.';
const MC={hyp:T+'hypotenuse-misplaced',lin:T+'sum-without-squares',nonright:T+'applied-to-non-right',longest:T+'converse-longest-side-ignored',sq:T+'square-root-step-missed'};
const pools={};const add=(k,it)=>(pools[k]??=[]).push(it);
const BASE=[[3,4,5],[5,12,13],[8,15,17],[7,24,25],[20,21,29],[12,35,37],[9,40,41]];
const TR=[];for(const [a,b,c] of BASE)for(let k=1;c*k<=60;k++)TR.push([a*k,b*k,c*k]);
const PY='피타고라스 정리',CV='피타고라스 정리의 역',APP='피타고라스 정리의 활용',JUS='피타고라스 정리의 정당화';
const isSq=n=>Number.isInteger(Math.sqrt(n));
// ── 빗변·다른 한 변 ──────────────────────────────────────────
for(const [a,b,c] of TR){if(c<=40)for(const [x,y] of [[a,b],[b,a]])add('hyp',amount({kind:'hyp',params:{a:x,b:y},prompt:`직각을 낀 두 변의 길이가 ${x} cm, ${y} cm인 직각삼각형의 빗변의 길이는 몇 cm인지 구하시오.`,answer:c,
    explain:`빗변의 길이를 x cm라 하면 x²=${x}²+${y}²=${c*c}, x>0이므로 x=${c}입니다.`,concept:PY,d:1,wrongs:[W(x+y,MC.lin,'sum'),W(y,MC.hyp,'longest-leg')]}));
  for(const [x,y] of [[a,b],[b,a]])if(y<=59&&c<=40)add('leg',amount({kind:'leg',params:{c,a:x},prompt:`빗변의 길이가 ${c} cm이고 다른 한 변의 길이가 ${x} cm인 직각삼각형의 나머지 한 변의 길이는 몇 cm인지 구하시오.`,answer:y,
    explain:`나머지 한 변을 x cm라 하면 x²=${c}²−${x}²=${y*y}, x>0이므로 x=${y}입니다.`,concept:PY,d:1,wrongs:[W(c-x,MC.lin,'difference'),W(c+x>59?0:c+x,MC.hyp,'added-squares')]}));}
const V3=['A','B','C'];const sideOpp={A:'BC',B:'AC',C:'AB'};
TR.forEach(([a,b,c],i)=>{for(const R of V3){const [P,Q]=V3.filter(v=>v!==R);const hyp=sideOpp[R],l1=[P,R].sort().join(''),l2=[Q,R].sort().join('');
  if((i+V3.indexOf(R))%2===0&&c<=59)add('named',amount({kind:'named-hyp',params:{right:R,[l1]:a,[l2]:b},prompt:`∠${R}=90°인 직각삼각형 ABC에서 ${l1}=${a} cm, ${l2}=${b} cm일 때, ${hyp}의 길이는 몇 cm인지 구하시오.`,answer:c,
    explain:`∠${R}의 대변 ${hyp}가 빗변이므로 ${hyp}²=${a}²+${b}²=${c*c}, ${hyp}=${c} cm입니다.`,concept:PY,d:2,wrongs:[W(a+b>59?0:a+b,MC.lin,'sum'),W(b,MC.hyp,'leg')]}));
  const [g,ask,gv,av]=(i+V3.indexOf(R))%3?[l1,l2,a,b]:[l2,l1,b,a];if(av<=59&&(i+V3.indexOf(R))%2)add('named',amount({kind:'named-leg',params:{right:R,hyp:c,[g]:gv},prompt:`∠${R}=90°인 직각삼각형 ABC에서 ${hyp}=${c} cm, ${g}=${gv} cm일 때, ${ask}의 길이는 몇 cm인지 구하시오.`,answer:av,
    explain:`빗변은 ${hyp}이므로 ${ask}²=${c}²−${gv}²=${av*av}, ${ask}=${av} cm입니다.`,concept:PY,d:2,wrongs:[W(c-gv,MC.lin,'difference'),W(c+gv>59?0:c+gv,MC.hyp,'hyp-as-leg')]}));}});
// ── 정사각형의 넓이 · x² ──────────────────────────────────────
for(const R of V3){const [P,Q]=V3.filter(v=>v!==R);const hyp=sideOpp[R],l1=[P,R].sort().join(''),l2=[Q,R].sort().join('');
  const stem=`∠${R}=90°인 직각삼각형 ABC의 세 변을 각각 한 변으로 하는 정사각형을 그렸다.`;
  for(let p=2;p<=40;p++)for(let s=p+1;p+s<=59;s+=2)if((p+s+V3.indexOf(R))%3===0)add('sqHyp',amount({kind:'sq-hyp',params:{right:R,[l1]:p,[l2]:s},prompt:`${stem} ${l1}, ${l2} 위의 정사각형의 넓이가 각각 ${p} cm², ${s} cm²일 때, ${hyp} 위의 정사각형의 넓이는 몇 cm²인지 구하시오.`,answer:p+s,
    explain:`${hyp}²=${l1}²+${l2}²이므로 ${hyp} 위의 정사각형의 넓이는 ${p}+${s}=${p+s} cm²입니다.`,concept:JUS,d:1,wrongs:[W(s-p,MC.hyp,'difference')]}));
  for(let h=6;h<=90;h++)for(let p=2;p<h-1;p+=3){const r=h-p;if(r>59||(h+p)%4)continue;const giv=(h+p)%8?l1:l2,ask=giv===l1?l2:l1;
    add('sqArea',amount({kind:'sq-leg',params:{right:R,hypArea:h,[giv]:p},prompt:`${stem} ${hyp}, ${giv} 위의 정사각형의 넓이가 각각 ${h} cm², ${p} cm²일 때, ${ask} 위의 정사각형의 넓이는 몇 cm²인지 구하시오.`,answer:r,
      explain:`${hyp}가 빗변이므로 ${ask} 위의 정사각형의 넓이는 ${h}−${p}=${r} cm²입니다.`,concept:JUS,d:2,wrongs:[W(h+p>59?0:h+p,MC.hyp,'added')]}));}}
for(let a=1;a<=7;a++)for(let b=a;a*a+b*b<=59;b++){const s=a*a+b*b;if(isSq(s)||s<2)continue;
  add('xsq',amount({kind:'xsq-hyp',params:{a,b},prompt:`직각을 낀 두 변의 길이가 ${a} cm, ${b} cm인 직각삼각형의 빗변의 길이를 x cm라고 할 때, x²의 값을 구하시오.`,answer:s,
    explain:`x²=${a}²+${b}²=${a*a}+${b*b}=${s}입니다.`,concept:PY,d:3,wrongs:[W(a+b,MC.lin,'sum'),W((a+b)**2>59?0:(a+b)**2,MC.sq,'square-of-sum')]}));}
for(let c=2;c<=31;c++)for(let a=1;a<c;a++){const s=c*c-a*a;if(s>59||s<2||isSq(s))continue;
  add('xsq',amount({kind:'xsq-leg',params:{c,a},prompt:`빗변의 길이가 ${c} cm이고 다른 한 변의 길이가 ${a} cm인 직각삼각형의 나머지 한 변의 길이를 x cm라고 할 때, x²의 값을 구하시오.`,answer:s,
    explain:`x²=${c}²−${a}²=${c*c}−${a*a}=${s}입니다.`,concept:PY,d:3,wrongs:[W(c*c+a*a>59?0:c*c+a*a,MC.hyp,'added'),W(c-a,MC.lin,'difference')]}));}
// ── 활용 ─────────────────────────────────────────────────
for(const [a,b,c] of TR){if(c<=59)for(const [x,y] of [[a,b],[b,a]])add('rect',amount({kind:'rect-diag',params:{w:x,h:y},prompt:`가로의 길이가 ${x} cm, 세로의 길이가 ${y} cm인 직사각형의 대각선의 길이는 몇 cm인지 구하시오.`,answer:c,
    explain:`대각선은 직각삼각형의 빗변이므로 ${c}²=${x}²+${y}²에서 ${c} cm입니다.`,concept:APP,d:2,wrongs:[W(x+y>59?0:x+y,MC.lin,'sum')]}));
  for(const [x,y] of [[a,b],[b,a]])if(y<=59)add('rect',amount({kind:'rect-side',params:{d:c,w:x},prompt:`대각선의 길이가 ${c} cm이고 가로의 길이가 ${x} cm인 직사각형의 세로의 길이는 몇 cm인지 구하시오.`,answer:y,
    explain:`세로²=${c}²−${x}²=${y*y}이므로 세로는 ${y} cm입니다.`,concept:APP,d:2,wrongs:[W(c-x,MC.lin,'difference')]}));
  if(a*b<=59)for(const [x,y] of [[a,b],[b,a]])add('rect3',amount({kind:'rect-area',params:{d:c,w:x},prompt:`대각선의 길이가 ${c} cm이고 가로의 길이가 ${x} cm인 직사각형의 넓이는 몇 cm²인지 구하시오.`,answer:a*b,
    explain:`세로²=${c}²−${x}²=${y*y}, 세로=${y} cm이므로 넓이는 ${x}×${y}=${a*b} cm²입니다.`,concept:APP,d:3,wrongs:[W(x*c>59?0:x*c,MC.hyp,'diagonal-as-side')]}));}
for(const [a,b,c] of TR)for(const [x,y] of [[a,b],[b,a]]){
  if(2*(x+y)<=59)add('app4',amount({kind:'rect-perimeter',params:{d:c,w:x},prompt:`대각선의 길이가 ${c} cm이고 가로의 길이가 ${x} cm인 직사각형의 둘레의 길이는 몇 cm인지 구하시오.`,answer:2*(x+y),
    explain:`세로²=${c}²−${x}²=${y*y}, 세로=${y} cm이므로 둘레는 2×(${x}+${y})=${2*(x+y)} cm입니다.`,concept:APP,d:4,wrongs:[W(2*(x+c)>59?0:2*(x+c),MC.hyp,'diagonal-as-side')]}));
  if(x*y%2===0&&x*y/2<=59&&x*y/2>=2)add('app4',amount({kind:'right-area',params:{c,a:x},prompt:`빗변의 길이가 ${c} cm이고 다른 한 변의 길이가 ${x} cm인 직각삼각형의 넓이는 몇 cm²인지 구하시오.`,answer:x*y/2,
    explain:`나머지 한 변은 ${y} cm(${c}²−${x}²=${y*y})이므로 넓이는 ${x}×${y}÷2=${x*y/2} cm²입니다.`,concept:APP,d:4,wrongs:[W(x*c%2===0&&x*c/2<=59?x*c/2:0,MC.hyp,'hyp-as-height')]}));
  if(x+y+c<=59)add('app4',amount({kind:'right-perimeter',params:{c,a:x},prompt:`빗변의 길이가 ${c} cm이고 다른 한 변의 길이가 ${x} cm인 직각삼각형의 둘레의 길이는 몇 cm인지 구하시오.`,answer:x+y+c,
    explain:`나머지 한 변은 ${y} cm(${c}²−${x}²=${y*y})이므로 둘레는 ${x}+${y}+${c}=${x+y+c} cm입니다.`,concept:APP,d:4,wrongs:[W(x+c+(c-x),MC.lin,'difference-leg')]}));}
for(let d=2;d<=10;d+=2)add('rect3',amount({kind:'square-from-diag',params:{d},prompt:`대각선의 길이가 ${d} cm인 정사각형의 넓이는 몇 cm²인지 구하시오.`,answer:d*d/2,
  explain:`한 변을 a cm라 하면 a²+a²=${d}²=${d*d}, 넓이 a²=${d*d/2} cm²입니다.`,concept:APP,d:3,wrongs:[W(d*d>59?0:d*d,MC.sq,'no-halving')]}));
for(const [a,b,c] of TR){for(const [x,y] of [[a,b],[b,a]]){if(2*x>60)continue;
  if(y<=59)add('iso',amount({kind:'iso-height',params:{side:c,base:2*x},prompt:`AB=AC=${c} cm, BC=${2*x} cm인 이등변삼각형 ABC의 꼭짓점 A에서 BC에 내린 수선의 발을 D라고 할 때, AD의 길이는 몇 cm인지 구하시오.`,answer:y,
    explain:`BD=${2*x}÷2=${x} cm이므로 AD²=${c}²−${x}²=${y*y}, AD=${y} cm입니다.`,concept:APP,d:3,wrongs:[W(Math.round(Math.sqrt(c*c-4*x*x))**2===c*c-4*x*x&&c>2*x?Math.round(Math.sqrt(c*c-4*x*x)):0,MC.hyp,'whole-base'),W(c-x,MC.lin,'difference')]}));
  if(x*y<=59)add('iso4',amount({kind:'iso-area',params:{side:c,base:2*x},prompt:`AB=AC=${c} cm, BC=${2*x} cm인 이등변삼각형 ABC의 넓이는 몇 cm²인지 구하시오.`,answer:x*y,
    explain:`높이²=${c}²−${x}²=${y*y}, 높이=${y} cm이므로 넓이는 ${2*x}×${y}÷2=${x*y} cm²입니다.`,concept:APP,d:4,wrongs:[W(2*x*y>59?0:2*x*y,MC.sq,'no-halving'),W(x*c>59?0:x*c,MC.hyp,'side-as-height')]}));}}
const CTX=[
  (a,b,c)=>[`길이가 ${c} m인 사다리를 벽에 기대어 세웠더니 사다리의 아래쪽 끝이 벽에서 ${a} m 떨어져 있었다. 사다리의 위쪽 끝은 바닥에서 몇 m 높이에 있는지 구하시오. (단, 벽은 바닥과 수직이다.)`,b,'ladder'],
  (a,b,c)=>[`가로가 ${a} m, 세로가 ${b} m인 직사각형 모양의 공원을 한 꼭짓점에서 마주 보는 꼭짓점까지 대각선을 따라 곧게 가로질러 간 거리는 몇 m인지 구하시오.`,c,'park'],
  (a,b,c)=>[`높이가 ${b} m인 깃대의 꼭대기에서 땅 위의 한 점까지 줄을 팽팽하게 연결했다. 그 점이 깃대의 밑에서 ${a} m 떨어져 있을 때, 줄의 길이는 몇 m인지 구하시오. (단, 깃대는 땅과 수직이다.)`,c,'pole'],
  (a,b,c)=>[`평평한 땅 위에서 ${a} m 떨어진 두 기둥의 높이가 각각 ${b+1} m, 1 m이다. 두 기둥의 꼭대기를 곧게 잇는 줄의 길이는 몇 m인지 구하시오. (단, 두 기둥은 땅과 수직이다.)`,c,'poles'],
];
for(const [a,b,c] of TR){if(c>30)continue;CTX.forEach((f,ci)=>{for(const [x,y] of [[a,b],[b,a]]){const [prompt,ans,kind]=f(x,y,c);if(ans>59)continue;
  add('ctx',amount({kind:`ctx-${kind}`,params:{x,y,c},prompt,answer:ans,explain:ans===c?`직각삼각형의 빗변이므로 ${x}²+${y}²=${c*c}=${c}², ${c} m입니다.`:`높이를 h m라 하면 h²=${c}²−${x}²=${y*y}, h=${y} m입니다.`,concept:APP,d:3,
    wrongs:[W(ans===c?x+y:c-x,MC.lin,'no-squares')]}));}});}
// 두 번 적용: 공통인 높이 AD
const legs=new Map();for(const [a,b,c] of TR)for(const [h,x] of [[a,b],[b,a]]){if(!legs.has(h))legs.set(h,[]);legs.get(h).push([x,a===h?c:c]);}
for(const [h,list] of legs)for(let i=0;i<list.length;i++)for(let j=0;j<list.length;j++){if(i===j)continue;const [x1,c1]=list[i],[x2,c2]=list[j];if(x1+x2>59)continue;
  add('two',amount({kind:'two-steps-bc',params:{AB:c1,AD:h,AC:c2},prompt:`△ABC의 꼭짓점 A에서 BC에 내린 수선의 발 D가 변 BC 위에 있다. AB=${c1} cm, AD=${h} cm, AC=${c2} cm일 때, BC의 길이는 몇 cm인지 구하시오.`,answer:x1+x2,
    explain:`BD²=${c1}²−${h}²=${x1*x1}, DC²=${c2}²−${h}²=${x2*x2}이므로 BC=${x1}+${x2}=${x1+x2} cm입니다.`,concept:APP,d:4,wrongs:[W(c1+c2>59?0:c1+c2,MC.lin,'add-sides')]}));
  if(c2<=59)add('two',amount({kind:'two-steps-ac',params:{AB:c1,BD:x1,DC:x2},prompt:`△ABC의 꼭짓점 A에서 BC에 내린 수선의 발 D가 변 BC 위에 있다. AB=${c1} cm, BD=${x1} cm, DC=${x2} cm일 때, AC의 길이는 몇 cm인지 구하시오.`,answer:c2,
    explain:`AD²=${c1}²−${x1}²=${h*h}, AC²=${h*h}+${x2}²=${c2*c2}이므로 AC=${c2} cm입니다.`,concept:APP,d:4,wrongs:[W(c1-x1+x2>0?c1-x1+x2:0,MC.lin,'no-squares')]}));}
// ── 정당화(넓이) ───────────────────────────────────────────
for(let a=1;a<=7;a++)for(let b=a+1;a*a+b*b<=59;b++)add('jus',amount({kind:'jus-outer',params:{a,b},prompt:`직각을 낀 두 변의 길이가 ${a} cm, ${b} cm인 합동인 직각삼각형 4개를 한 변의 길이가 ${a+b} cm인 정사각형의 네 귀퉁이에 겹치지 않게 놓았더니, 가운데에 네 빗변으로 둘러싸인 정사각형이 생겼다. 이 정사각형의 넓이는 몇 cm²인지 구하시오.`,answer:a*a+b*b,
  explain:`(${a}+${b})²−4×{frac:1/2}×${a}×${b}=${(a+b)**2}−${2*a*b}=${a*a+b*b} cm²이고, 이는 ${a}²+${b}²과 같습니다.`,concept:JUS,d:4,wrongs:[W((a+b)**2>59?0:(a+b)**2,MC.sq,'outer-square'),W(2*a*b,MC.lin,'triangles-only')]}));
for(const [a,b,c] of TR){const s=(b-a)**2;if(s<2||s>59)continue;
  add('jus',amount({kind:'jus-inner',params:{a,b,c},prompt:`세 변의 길이가 ${a} cm, ${b} cm, ${c} cm인 합동인 직각삼각형 4개를 빗변이 바깥쪽에 오도록 겹치지 않게 모아 한 변의 길이가 ${c} cm인 정사각형을 만들었더니, 가운데에 작은 정사각형이 생겼다. 작은 정사각형의 넓이는 몇 cm²인지 구하시오.`,answer:s,
    explain:`작은 정사각형의 넓이는 ${c}²−4×{frac:1/2}×${a}×${b}=${c*c}−${2*a*b}=${s} cm²이고, 한 변은 ${b}−${a}=${b-a} cm입니다.`,concept:JUS,d:4,wrongs:[W(b-a>=1?b-a:0,MC.sq,'side-not-area')]}));}
// ── 피타고라스 정리의 역 ──────────────────────────────────────
for(const [a,b,c] of TR){if(c<=59)add('conv',amount({kind:'conv-hyp',params:{a,b},prompt:`세 변의 길이가 ${a} cm, ${b} cm, x cm인 삼각형이 직각삼각형이고 x cm가 가장 긴 변의 길이일 때, x의 값을 구하시오.`,answer:c,
    explain:`가장 긴 변이 빗변이므로 x²=${a}²+${b}²=${c*c}, x=${c}입니다.`,concept:CV,d:3,wrongs:[W(a+b>59?0:a+b,MC.lin,'sum')]}));
  add('conv',amount({kind:'conv-leg',params:{a,c},prompt:`세 변의 길이가 ${a} cm, ${c} cm, x cm인 삼각형이 직각삼각형이고 ${c} cm가 가장 긴 변의 길이일 때, x의 값을 구하시오.`,answer:b,
    explain:`${c} cm가 빗변이므로 x²=${c}²−${a}²=${b*b}, x=${b}입니다.`,concept:CV,d:3,wrongs:[W(c+a>59?0:c+a,MC.longest,'x-as-hypotenuse'),W(c-a,MC.lin,'difference')]}));}

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

const intro=amount({kind:'sq-hyp-intro',params:{},prompt:'∠C=90°인 직각삼각형 ABC의 세 변을 각각 한 변으로 하는 정사각형을 그렸다. AB, BC 위의 정사각형의 넓이가 각각 5 cm², 3 cm²일 때, AC 위의 정사각형의 넓이는 몇 cm²인지 구하시오.',answer:2,
  explain:'AB가 빗변이므로 AC 위의 정사각형의 넓이는 5−3=2 cm²입니다. 2닢을 붓습니다.',concept:JUS,d:1,wrongs:[W(8,MC.hyp,'added')]});
const P=pools;
if(process.env.POOLS)console.log(Object.fromEntries(Object.entries(P).map(([k,v])=>[k,v.length])));
writeGeoPack({id:'m2s2-u5',title:'피타고라스 정리',standards:['[9수03-15]'],intro,groups:[
  [P.hyp,30],[P.leg,30],[P.cHyp,12],[P.sqHyp,14],
  [P.named,36],[P.sqArea,14],[P.rect,20],[P.cJudge,16],
  [P.xsq,24],[P.rect3,8],[P.iso,20],[P.ctx,26],[P.conv,14],
  [P.iso4,4],[P.app4,20],[P.two,40],[P.jus,26]]});
