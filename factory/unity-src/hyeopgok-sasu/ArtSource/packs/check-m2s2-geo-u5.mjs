// m2s2-u5 oracle tables for check-m2s2-geo.mjs. Imports NO generator module.
import {here,root,out,curriculum,failures,stats,fail,gcd,M,N,solve,isqrt} from './check-m2s2-geo-lib.mjs';
// ═════════════════════════ m2s2-u5 피타고라스 정리 ═════════════════════════
const S2=(a,b)=>a*a+b*b;
const hypOf=R=>({A:'BC',B:'AC',C:'AB'})[R];
function rightTri(q,x,y,z){const s=[x,y,z].sort((a,b)=>a-b);if(s[0]*s[0]+s[1]*s[1]!==s[2]*s[2])throw Error('data are not a right triangle');}
const U5={
 hyp(q){const m=M(/^직각을 낀 두 변의 길이가 (\d+) cm, (\d+) cm인 직각삼각형의 빗변의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return solve(x=>x*x===S2(N(m[1]),N(m[2])),1,400);},
 leg(q){const m=M(/^빗변의 길이가 (\d+) cm이고 다른 한 변의 길이가 (\d+) cm인 직각삼각형의 나머지 한 변의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return solve(x=>S2(x,N(m[2]))===N(m[1])**2,1,400);},
 'named-hyp'(q){const m=M(/^∠([ABC])=90°인 직각삼각형 ABC에서 ([ABC]{2})=(\d+) cm, ([ABC]{2})=(\d+) cm일 때, ([ABC]{2})의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  if(m[6]!==hypOf(m[1])||m[2]===m[6]||m[4]===m[6])throw Error('asked side must be the hypotenuse');return solve(x=>x*x===S2(N(m[3]),N(m[5])),1,400);},
 'named-leg'(q){const m=M(/^∠([ABC])=90°인 직각삼각형 ABC에서 ([ABC]{2})=(\d+) cm, ([ABC]{2})=(\d+) cm일 때, ([ABC]{2})의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const h=hypOf(m[1]);if(m[2]!==h||m[4]===h||m[6]===h||m[4]===m[6])throw Error('first given side must be the hypotenuse');return solve(x=>S2(x,N(m[5]))===N(m[3])**2,1,400);},
 'sq-hyp'(q){const m=M(/^∠([ABC])=90°인 직각삼각형 ABC의 세 변을 각각 한 변으로 하는 정사각형을 그렸다\. ([ABC]{2}), ([ABC]{2}) 위의 정사각형의 넓이가 각각 (\d+) cm², (\d+) cm²일 때, ([ABC]{2}) 위의 정사각형의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  if(m[6]!==hypOf(m[1]))throw Error('asked square must stand on the hypotenuse');return solve(x=>x===N(m[4])+N(m[5]),1,2000);},
 'sq-leg'(q){const m=M(/^∠([ABC])=90°인 직각삼각형 ABC의 세 변을 각각 한 변으로 하는 정사각형을 그렸다\. ([ABC]{2}), ([ABC]{2}) 위의 정사각형의 넓이가 각각 (\d+) cm², (\d+) cm²일 때, ([ABC]{2}) 위의 정사각형의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  if(m[2]!==hypOf(m[1]))throw Error('first square must be on the hypotenuse');return solve(x=>x+N(m[5])===N(m[4]),1,2000);},
 'sq-from-legs'(q){const m=M(/^∠([ABC])=90°인 직각삼각형 ABC의 세 변을 각각 한 변으로 하는 정사각형을 그렸다\. ([ABC]{2})=(\d+) cm, ([ABC]{2})=(\d+) cm일 때, ([ABC]{2}) 위의 정사각형의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  const h=hypOf(m[1]);if(m[6]!==h||m[2]===h||m[4]===h||m[2]===m[4])throw Error('given sides must be the two legs, asked square on the hypotenuse');return solve(A=>A===S2(N(m[3]),N(m[5])),1,4000);},
 'sq-from-hyp'(q){const m=M(/^∠([ABC])=90°인 직각삼각형 ABC의 세 변을 각각 한 변으로 하는 정사각형을 그렸다\. ([ABC]{2})=(\d+) cm, ([ABC]{2})=(\d+) cm일 때, ([ABC]{2}) 위의 정사각형의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  const h=hypOf(m[1]);if(m[2]!==h||m[4]===h||m[6]===h||m[4]===m[6])throw Error('first given side must be the hypotenuse');if(N(m[5])>=N(m[3]))throw Error('leg must be shorter than hypotenuse');
  // area of the asked square = (asked leg)² : A + leg² = hyp²
  return solve(A=>A+N(m[5])**2===N(m[3])**2,1,4000);},
 'xsq-hyp'(q){const m=M(/^직각을 낀 두 변의 길이가 (\d+) cm, (\d+) cm인 직각삼각형의 빗변의 길이를 x cm라고 할 때, x²의 값을 구하시오\.$/,q.prompt);return solve(X=>X===S2(N(m[1]),N(m[2])),1,4000);},
 'xsq-leg'(q){const m=M(/^빗변의 길이가 (\d+) cm이고 다른 한 변의 길이가 (\d+) cm인 직각삼각형의 나머지 한 변의 길이를 x cm라고 할 때, x²의 값을 구하시오\.$/,q.prompt);return solve(X=>X+N(m[2])**2===N(m[1])**2,1,4000);},
 'rect-diag'(q){const m=M(/^가로의 길이가 (\d+) cm, 세로의 길이가 (\d+) cm인 직사각형의 대각선의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return solve(x=>x*x===S2(N(m[1]),N(m[2])),1,400);},
 'rect-side'(q){const m=M(/^대각선의 길이가 (\d+) cm이고 가로의 길이가 (\d+) cm인 직사각형의 세로의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return solve(x=>S2(x,N(m[2]))===N(m[1])**2,1,400);},
 'rect-area'(q){const m=M(/^대각선의 길이가 (\d+) cm이고 가로의 길이가 (\d+) cm인 직사각형의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);const w=N(m[2]);const h=solve(x=>S2(x,w)===N(m[1])**2,1,400);return w*h;},
 'rect-perimeter'(q){const m=M(/^대각선의 길이가 (\d+) cm이고 가로의 길이가 (\d+) cm인 직사각형의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);const w=N(m[2]);const h=solve(x=>S2(x,w)===N(m[1])**2,1,400);return 2*w+2*h;},
 'right-area'(q){const m=M(/^빗변의 길이가 (\d+) cm이고 다른 한 변의 길이가 (\d+) cm인 직각삼각형의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);const a=N(m[2]);const b=solve(x=>S2(x,a)===N(m[1])**2,1,400);return solve(S=>2*S===a*b,1,4000);},
 'right-perimeter'(q){const m=M(/^빗변의 길이가 (\d+) cm이고 다른 한 변의 길이가 (\d+) cm인 직각삼각형의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);const a=N(m[2]),c=N(m[1]);return a+c+solve(x=>S2(x,a)===c*c,1,400);},
 'square-from-diag'(q){const d=N(M(/^대각선의 길이가 (\d+) cm인 정사각형의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt)[1]);return solve(A=>A+A===d*d,1,4000);},
 'iso-height'(q){const m=M(/^AB=AC=(\d+) cm, BC=(\d+) cm인 이등변삼각형 ABC의 꼭짓점 A에서 BC에 내린 수선의 발을 D라고 할 때, AD의 길이는 몇 cm인지 구하시오\.$/,q.prompt);const half=solve(h=>2*h===N(m[2]),1,400);return solve(x=>S2(x,half)===N(m[1])**2,1,400);},
 'iso-area'(q){const m=M(/^AB=AC=(\d+) cm, BC=(\d+) cm인 이등변삼각형 ABC의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);const bc=N(m[2]);const half=solve(h=>2*h===bc,1,400);const ht=solve(x=>S2(x,half)===N(m[1])**2,1,400);return solve(S=>2*S===bc*ht,1,4000);},
 'ctx-ladder'(q){const m=M(/^길이가 (\d+) m인 사다리를 벽에 기대어 세웠더니 사다리의 아래쪽 끝이 벽에서 (\d+) m 떨어져 있었다\. 바닥에서 사다리의 위쪽 끝까지의 높이는 몇 m인지 구하시오\. \(단, 벽은 바닥과 수직이다\.\)$/,q.prompt);return solve(x=>S2(x,N(m[2]))===N(m[1])**2,1,400);},
 'ctx-park'(q){const m=M(/^가로가 (\d+) m, 세로가 (\d+) m인 직사각형 모양의 공원을 한 꼭짓점에서 마주 보는 꼭짓점까지 대각선을 따라 곧게 가로질러 간 거리는 몇 m인지 구하시오\.$/,q.prompt);return solve(x=>x*x===S2(N(m[1]),N(m[2])),1,400);},
 'ctx-pole'(q){const m=M(/^높이가 (\d+) m인 깃대의 꼭대기에서 땅 위의 한 점까지 줄을 팽팽하게 연결했다\. 그 점이 깃대의 밑에서 (\d+) m 떨어져 있을 때, 줄의 길이는 몇 m인지 구하시오\. \(단, 깃대는 땅과 수직이다\.\)$/,q.prompt);return solve(x=>x*x===S2(N(m[1]),N(m[2])),1,400);},
 'ctx-poles'(q){const m=M(/^평평한 땅 위에서 (\d+) m 떨어진 두 기둥의 높이가 각각 (\d+) m, (\d+) m이다\. 두 기둥의 꼭대기를 곧게 잇는 줄의 길이는 몇 m인지 구하시오\. \(단, 두 기둥은 땅과 수직이다\.\)$/,q.prompt);
  // coordinates: tops at (0,h1) and (d,h2)
  const d=N(m[1]),dy=N(m[2])-N(m[3]);stats.coordinateChecks++;return solve(x=>x*x===d*d+dy*dy,1,400);},
 'two-steps-bc'(q){const m=M(/^△ABC의 꼭짓점 A에서 BC에 내린 수선의 발 D가 변 BC 위에 있다\. AB=(\d+) cm, AD=(\d+) cm, AC=(\d+) cm일 때, BC의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ab=N(m[1]),ad=N(m[2]),ac=N(m[3]);const bd=solve(x=>S2(x,ad)===ab*ab,1,400),dc=solve(x=>S2(x,ad)===ac*ac,1,400);return bd+dc;},
 'two-steps-ac'(q){const m=M(/^△ABC의 꼭짓점 A에서 BC에 내린 수선의 발 D가 변 BC 위에 있다\. AB=(\d+) cm, BD=(\d+) cm, DC=(\d+) cm일 때, AC의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ab=N(m[1]),bd=N(m[2]),dc=N(m[3]);const ad=solve(x=>S2(x,bd)===ab*ab,1,400);return solve(x=>x*x===S2(ad,dc),1,400);},
 'jus-outer'(q){const m=M(/^직각을 낀 두 변의 길이가 (\d+) cm, (\d+) cm인 합동인 직각삼각형 4개를 한 변의 길이가 (\d+) cm인 정사각형의 네 귀퉁이에 겹치지 않게 놓았더니, 가운데에 네 빗변으로 둘러싸인 정사각형이 생겼다\. 이 정사각형의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  const a=N(m[1]),b=N(m[2]),s=N(m[3]);if(s!==a+b)throw Error('outer side must be a+b');return solve(A=>2*(A+2*a*b)===2*s*s,1,4000);},
 'jus-inner'(q){const m=M(/^세 변의 길이가 (\d+) cm, (\d+) cm, (\d+) cm인 합동인 직각삼각형 4개를 빗변이 바깥쪽에 오도록 겹치지 않게 모아 한 변의 길이가 (\d+) cm인 정사각형을 만들었더니, 가운데에 작은 정사각형이 생겼다\. 작은 정사각형의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  const [a,b,c,s]=[1,2,3,4].map(i=>N(m[i]));rightTri(q,a,b,c);if(s!==c)throw Error('outer side = hypotenuse');const side=Math.abs(b-a);return solve(A=>A===side*side&&A+2*a*b===c*c,1,4000);},
 'conv-hyp'(q){const m=M(/^세 변의 길이가 (\d+) cm, (\d+) cm, x cm인 삼각형이 직각삼각형이고 x cm가 가장 긴 변의 길이일 때, x의 값을 구하시오\.$/,q.prompt);return solve(x=>x>N(m[1])&&x>N(m[2])&&x*x===S2(N(m[1]),N(m[2])),1,400);},
 'conv-leg'(q){const m=M(/^세 변의 길이가 (\d+) cm, (\d+) cm, x cm인 삼각형이 직각삼각형이고 (\d+) cm가 가장 긴 변의 길이일 때, x의 값을 구하시오\.$/,q.prompt);if(m[3]!==m[2])throw Error('longest');const c=N(m[2]);return solve(x=>x<c&&S2(x,N(m[1]))===c*c,1,400);},
};
const tripleOf=o=>{const m=M(/^(\d+), (\d+), (\d+)$/,o);const s=[1,2,3].map(i=>N(m[i])).sort((a,b)=>a-b);if(s[0]+s[1]<=s[2])throw Error(`option ${o} is not a triangle`);return s;};
const U5C={
 'judge-right'(q,opts){return opts.map(o=>{const [a,b,c]=tripleOf(o);return a*a+b*b===c*c;});},
 'judge-not-right'(q,opts){return opts.map(o=>{const [a,b,c]=tripleOf(o);return a*a+b*b!==c*c;});},
 'hyp-name'(q,opts){const m=M(/^∠([A-F])=90°인 직각삼각형 (ABC|DEF)에서 빗변을 고르시오\.$/,q.prompt);const h=[...m[2]].filter(v=>v!==m[1]).join('');return opts.map(o=>o===h);},
 relation(q,opts){const m=M(/^∠([A-F])=90°인 직각삼각형 (ABC|DEF)에서 항상 성립하는 식을 고르시오\.$/,q.prompt);const R=m[1];
  // test every formula on two concrete right triangles with the right angle at R
  return opts.map(o=>{let f;const vals=[[3,4,5],[5,12,13]];return vals.every(([l1,l2,h])=>{const len=s=>!s.includes(R)?h:(s.replace(R,'')<[...m[2]].filter(v=>v!==R).sort()[1]?l1:l2);
    if((f=/^([A-F]{2})²=([A-F]{2})²\+([A-F]{2})²$/.exec(o)))return len(f[1])**2===len(f[2])**2+len(f[3])**2;
    if((f=/^([A-F]{2})=([A-F]{2})\+([A-F]{2})$/.exec(o)))return len(f[1])===len(f[2])+len(f[3]);throw Error(`formula ${o}`);});});},
};

export default {A:U5,C:U5C};
