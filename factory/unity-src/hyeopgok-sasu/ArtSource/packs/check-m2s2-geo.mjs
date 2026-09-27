#!/usr/bin/env node
// Independent oracle for m2s2-u1~u5 (geometry) and the rewritten m2s2-u7 (probability).
// Imports NO generator module. Every answer is recomputed from values parsed back out of the
// student-facing prompt text (the proofs ledger only names the item kind and is cross-checked
// against the parsed values), by a different route than the generator: brute-force search over
// candidate answers, angle-sum systems, exact rational coordinates, lattice enumeration of
// quadrilaterals, and sample-space enumeration. Integer / rational arithmetic only.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {validatePack} from './validate-pack.mjs';
const here=path.dirname(fileURLToPath(import.meta.url));const root=path.resolve(here,'../../../../..');
const out=path.join(root,'public/g/hyeopgok-sasu/packs');
const curriculum=JSON.parse(fs.readFileSync(path.join(root,'curriculum/2022-middle-math.json'),'utf8'));
const failures=[];const stats={items:0,bruteForce:0,latticeQuads:0,latticeClaims:0,coordinateChecks:0,enumeratedOutcomes:0,acceptancePairs:0,choiceOptions:0};
const fail=(id,msg)=>failures.push(`${id}: ${msg}`);
const gcd=(a,b)=>{a=Math.abs(a);b=Math.abs(b);while(b)[a,b]=[b,a%b];return a||1;};
const M=(re,s,id)=>{const m=re.exec(s);if(!m)throw Error(`prompt does not match ${re}`);return m;};
const N=Number;
// brute force: the unique integer x in [lo,hi] with pred(x); throws if none or several.
function solve(pred,lo=0,hi=400){const hits=[];for(let x=lo;x<=hi;x++){stats.bruteForce++;if(pred(x))hits.push(x);}if(hits.length!==1)throw Error(`brute force found ${hits.length} solutions`);return hits[0];}
const isqrt=n=>{if(n<0)return NaN;let r=Math.floor(Math.sqrt(n));while(r*r>n)r--;while((r+1)*(r+1)<=n)r++;return r*r===n?r:NaN;};

// ═════════════════════════ m2s2-u1 삼각형의 성질 ═════════════════════════
const OPP={A:'BC',B:'AC',C:'AB'};
const oppVertex=s=>['A','B','C'].find(v=>!s.includes(v));
const U1={
 'iso-base'(q){const m=M(/^([ABC])([ABC])=([ABC])([ABC])인 이등변삼각형 ABC에서 ∠([ABC])=(\d+)°일 때, ∠([ABC])의 크기는 몇 도인지 구하시오\.$/,q.prompt);
  if(m[1]!==m[3]||m[2]===m[4])throw Error('equal sides must share the apex');const apex=m[1];if(m[5]!==apex||m[7]===apex)throw Error('given/asked vertex roles');
  const a=N(m[6]);return solve(b=>a+2*b===180,1,179);},
 'iso-apex'(q){const m=M(/^([ABC])([ABC])=([ABC])([ABC])인 이등변삼각형 ABC에서 ∠([ABC])=(\d+)°일 때, ∠([ABC])의 크기는 몇 도인지 구하시오\.$/,q.prompt);
  if(m[1]!==m[3])throw Error('apex');const apex=m[1];if(m[5]===apex||m[7]!==apex)throw Error('roles');const b=N(m[6]);return solve(a=>a+2*b===180,1,179);},
 'iso-ext-apex'(q){const z=N(M(/^AB=AC인 이등변삼각형 ABC에서 변 BC를 점 C 쪽으로 연장한 직선 위에 점 D를 잡았더니 ∠ACD=(\d+)°이었다\. ∠A의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);
  const C=solve(c=>c+z===180,1,179);return solve(a=>a+C+C===180,1,179);},
 'iso-ext-base'(q){const z=N(M(/^AB=AC인 이등변삼각형 ABC에서 변 BC를 점 C 쪽으로 연장한 직선 위에 점 D를 잡았더니 ∠ACD=(\d+)°이었다\. ∠B의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);
  const C=solve(c=>c+z===180,1,179);if(C>=90)throw Error('base angle must be acute');return C;},
 'iso-bis-half'(q){const bc=N(M(/^AB=AC인 이등변삼각형 ABC에서 ∠A의 이등분선이 변 BC와 만나는 점을 D라고 하자\. BC=(\d+) cm일 때, BD의 길이는 몇 cm인지 구하시오\.$/,q.prompt)[1]);return solve(x=>2*x===bc,1,200);},
 'iso-bis-double'(q){const bd=N(M(/^AB=AC인 이등변삼각형 ABC에서 ∠A의 이등분선이 변 BC와 만나는 점을 D라고 하자\. BD=(\d+) cm일 때, BC의 길이는 몇 cm인지 구하시오\.$/,q.prompt)[1]);return solve(x=>x===bd+bd,1,200);},
 'iso-bis-angle'(q){const b=N(M(/^AB=AC인 이등변삼각형 ABC에서 ∠A의 이등분선이 변 BC와 만나는 점을 D라고 하자\. ∠B=(\d+)°일 때, ∠BAD의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);
  const A=solve(a=>a+2*b===180,1,179);return solve(x=>2*x===A,1,179);},
 'iso-converse'(q){const m=M(/^△ABC에서 ∠([ABC])=(\d+)°, ∠([ABC])=(\d+)°이고 (AB|BC|AC)=(\d+) cm일 때, (AB|BC|AC)의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ang={[m[1]]:N(m[2]),[m[3]]:N(m[4])};const third=['A','B','C'].find(v=>!(v in ang));ang[third]=solve(x=>x+ang[m[1]]+ang[m[3]]===180,1,179);
  const g=oppVertex(m[5]),t=oppVertex(m[7]);if(g===t)throw Error('asked side equals given side');if(ang[g]!==ang[t])throw Error('opposite angles differ: side not determined');
  if(new Set(Object.values(ang)).size!==2)throw Error('must be isosceles, not equilateral');return N(m[6]);},
 'rh-angle'(q){const m=M(/^∠([ABC])=∠([DEF])=90°인 두 직각삼각형 ABC, DEF에서 ([A-F]{2})=([A-F]{2}), (∠([ABC])=∠([DEF])|([ABC]{2})=([DEF]{2}))이다\. ∠([ABC])=(\d+)°일 때, ∠([DEF])의 크기는 몇 도인지 구하시오\.$/,q.prompt);
  const cor=c=>'DEF'['ABC'.indexOf(c)],back=c=>'ABC'['DEF'.indexOf(c)];
  if(cor(m[1])!==m[2])throw Error('right angles must correspond');
  const h1=m[3],h2=m[4];if(h1.includes(m[1])||[...h1].map(cor).sort().join('')!==[...h2].sort().join(''))throw Error('hypotenuse');
  let cond;if(m[6]){if(cor(m[6])!==m[7]||m[6]===m[1])throw Error('angle pair');cond='RHA';}
  else {const l1=m[8],l2=m[9];if(!l1.includes(m[1])||[...l1].map(cor).sort().join('')!==[...l2].sort().join(''))throw Error('leg pair');cond='RHS';}
  const ang={[m[1]]:90,[m[10]]:N(m[11])};if(m[10]===m[1])throw Error('given angle is right');
  const third=['A','B','C'].find(v=>!(v in ang));ang[third]=solve(x=>x+90+ang[m[10]]===180,1,89);
  q._cond=cond;return ang[back(m[12])];},
 'bis-opa'(q){const v=N(M(/^∠XOY의 이등분선 위의 한 점 P에서 두 변 OX, OY에 내린 수선의 발을 각각 A, B라고 하자\. ∠XOY=(\d+)°일 때, ∠OPA의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);
  const half=solve(x=>2*x===v,1,89);return solve(x=>x+90+half===180,1,179);},
 'bis-apb'(q){const v=N(M(/^∠XOY의 이등분선 위의 한 점 P에서 두 변 OX, OY에 내린 수선의 발을 각각 A, B라고 하자\. ∠XOY=(\d+)°일 때, ∠APB의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);
  // two congruent right triangles OAP, OBP: ∠APB = ∠OPA + ∠OPB
  const opa=solve(x=>x+90+v/2===180,1,179);return 2*opa;},
 'bis-converse'(q){const t=N(M(/^∠XOY의 내부의 한 점 P에서 두 변 OX, OY에 내린 수선의 발을 각각 A, B라고 하자\. PA=PB이고 ∠AOP=(\d+)°일 때, ∠XOY의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);return t+t;},
 'circ-sum'(q){const m=M(/^점 O가 △ABC의 외심이고 ∠(O[ABC]{2})=(\d+)°, ∠(O[ABC]{2})=(\d+)°일 때, ∠(O[ABC]{2})의 크기는 몇 도인지 구하시오\. \(단, 점 O는 △ABC의 내부에 있다\.\)$/,q.prompt);
  // ∠OXY is the base angle of isosceles △OXY (OX=OY): classify by the unordered side XY.
  const cls=n=>[...n.slice(1)].sort().join('');const known={[cls(m[1])]:N(m[2]),[cls(m[3])]:N(m[4])};const ask=cls(m[5]);
  if(Object.keys(known).length!==2||ask in known)throw Error('need three different sides');
  // triangle angles: ∠A = base(AB)+base(AC), etc.; solve the unknown base angle by angle sum.
  return solve(u=>{const b={...known,[ask]:u};const A=b.AB+b.AC,B=b.AB+b.BC,C=b.AC+b.BC;return A+B+C===180&&A<90&&B<90&&C<90;},1,89);},
 'circ-boc'(q){const A=N(M(/^점 O가 △ABC의 외심이고 ∠A=(\d+)°일 때, ∠BOC의 크기는 몇 도인지 구하시오\. \(단, 점 O는 △ABC의 내부에 있다\.\)$/,q.prompt)[1]);
  const obc=90-A;/* ∠OAB+∠OBC+∠OCA=90 with ∠A=∠OAB+∠OCA */return solve(x=>x+2*obc===180,1,179);},
 'circ-a-from-boc'(q){const y=N(M(/^점 O가 △ABC의 외심이고 ∠BOC=(\d+)°일 때, ∠A의 크기는 몇 도인지 구하시오\. \(단, 점 O는 △ABC의 내부에 있다\.\)$/,q.prompt)[1]);
  const obc=solve(x=>2*x+y===180,1,89);return solve(a=>a+obc===90,1,89);},
 'circ-obc'(q){const A=N(M(/^점 O가 △ABC의 외심이고 ∠A=(\d+)°일 때, ∠OBC의 크기는 몇 도인지 구하시오\. \(단, 점 O는 △ABC의 내부에 있다\.\)$/,q.prompt)[1]);return solve(x=>x+A===90,1,89);},
 'circ-right-len'(q){const m=M(/^∠([ABC])=90°인 직각삼각형 ABC의 외심을 O라고 하자\. ([ABC]{2})=(\d+) cm일 때, O([ABC])의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  if(m[2].includes(m[1]))throw Error('given side must be the hypotenuse');if(m[4]!==m[1])throw Error('asked distance');return solve(x=>2*x===N(m[3]),1,200);},
 'circ-right-angle'(q){const m=M(/^∠([ABC])=90°인 직각삼각형 ABC의 외심을 O라고 하자\. ∠([ABC])=(\d+)°일 때, ∠O([ABC])([ABC])의 크기는 몇 도인지 구하시오\.$/,q.prompt);
  if(m[4]!==m[1]||m[5]!==m[2]||m[2]===m[1])throw Error('angle roles');const x=N(m[3]);if(x>=90)throw Error('acute');
  // O = midpoint of hypotenuse: OR = OP, and ∠OPR is ∠P itself (O lies on segment PQ).
  return solve(t=>t===x&&t+x+(180-2*x)===180,1,89);},
 'circ-radius'(q){const k=N(M(/^점 O가 △ABC의 외심이고 OA=(\d+) cm일 때, OB\+OC의 길이는 몇 cm인지 구하시오\.$/,q.prompt)[1]);return k+k;},
 'in-a-from-bic'(q){const y=N(M(/^점 I가 △ABC의 내심이고 ∠BIC=(\d+)°일 때, ∠A의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);return solve(a=>2*(180-y)===180-a,1,179);},
 'in-ibc-icb'(q){const A=N(M(/^점 I가 △ABC의 내심이고 ∠A=(\d+)°일 때, ∠IBC\+∠ICB의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);return solve(s=>2*s+A===180,1,179);},
 'in-sum'(q){const m=M(/^점 I가 △ABC의 내심이고 ∠(I[ABC]{2})=(\d+)°, ∠(I[ABC]{2})=(\d+)°일 때, ∠(I[ABC]{2})의 크기는 몇 도인지 구하시오\.$/,q.prompt);
  const v=n=>n[1];const known={[v(m[1])]:N(m[2]),[v(m[3])]:N(m[4])};const ask=v(m[5]);if(Object.keys(known).length!==2||ask in known)throw Error('need 3 vertices');
  return solve(u=>{const h={...known,[ask]:u};return 2*h.A+2*h.B+2*h.C===180;},1,89);},
 'in-area'(q){const m=M(/^점 I가 △ABC의 내심이고 내접원의 반지름의 길이가 (\d+) cm이다\. △ABC의 둘레의 길이가 (\d+) cm일 때, △ABC의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  const r=N(m[1]),p=N(m[2]);if(p*p<=108*r*r)throw Error('no triangle with this inradius/perimeter');return solve(S=>2*S===r*p,1,2000);},
 'in-perimeter'(q){const m=M(/^점 I가 △ABC의 내심이고 내접원의 반지름의 길이가 (\d+) cm이다\. △ABC의 넓이가 (\d+) cm²일 때, △ABC의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const r=N(m[1]),S=N(m[2]);const p=solve(p=>r*p===2*S,1,2000);if(p*p<=108*r*r)throw Error('impossible triangle');return p;},
 'in-radius'(q){const m=M(/^△ABC의 둘레의 길이가 (\d+) cm이고 넓이가 (\d+) cm²일 때, △ABC의 내접원의 반지름의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const p=N(m[1]),S=N(m[2]);const r=solve(r=>r*p===2*S,1,200);if(p*p<=108*r*r)throw Error('impossible triangle');return r;},
 'in-right'(q){const m=M(/^세 변의 길이가 (\d+) cm, (\d+) cm, (\d+) cm인 직각삼각형의 내접원의 반지름의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const [a,b,c]=[N(m[1]),N(m[2]),N(m[3])].sort((x,y)=>x-y);if(a*a+b*b!==c*c)throw Error('not a right triangle');
  // tangent-length route: r = (a+b−c)/2, cross-checked by the area route r(a+b+c) = ab.
  const r=solve(r=>2*r===a+b-c,1,200);if(r*(a+b+c)!==a*b)throw Error('area route disagrees');return r;},
};
// choice truth tables (independent restatement of the definitions)
const TERM={'삼각형의 세 변의 수직이등분선의 교점을 무엇이라고 하는지 고르시오.':'외심','삼각형의 세 내각의 이등분선의 교점을 무엇이라고 하는지 고르시오.':'내심',
 '삼각형의 외접원의 중심을 무엇이라고 하는지 고르시오.':'외심','삼각형의 내접원의 중심을 무엇이라고 하는지 고르시오.':'내심',
 '삼각형의 외심에서 같은 거리에 있는 것을 고르시오.':'세 꼭짓점','삼각형의 내심에서 같은 거리에 있는 것을 고르시오.':'세 변'};
const U1C={
 term(q,opts){const a=TERM[q.prompt];if(!a)throw Error('unknown term stem');return opts.map(o=>o===a);},
 'circ-location'(q,opts){const m=M(/^세 내각의 크기가 (\d+)°, (\d+)°, (\d+)°인 삼각형의 외심의 위치를 고르시오\.$/,q.prompt);const a=[1,2,3].map(i=>N(m[i]));
  if(a[0]+a[1]+a[2]!==180||a.some(x=>x<=0))throw Error('angles');const mx=Math.max(...a);
  // central angle 2∠X of the largest angle: < 180 inside, = 180 on the side (diameter), > 180 outside.
  const where=2*mx<180?'삼각형의 내부':2*mx===180?'빗변의 중점':'삼각형의 외부';return opts.map(o=>o===where);},
 'in-location'(q,opts){const m=M(/^세 내각의 크기가 (\d+)°, (\d+)°, (\d+)°인 삼각형의 내심의 위치를 고르시오\.$/,q.prompt);if(N(m[1])+N(m[2])+N(m[3])!==180)throw Error('angles');return opts.map(o=>o==='삼각형의 내부');},
 congruence(q,opts){
  const m=M(/^(?:∠([ABC])=∠([DEF])=90°인 두 직각삼각형 ABC, DEF에서 |△ABC와 △DEF에서 )(.+)일 때, 두 삼각형의 합동에 대하여 옳은 것을 고르시오\.$/,q.prompt);
  const cor=c=>'DEF'['ABC'.indexOf(c)];const right=m[1]??null;if(right&&cor(right)!==m[2])throw Error('right angles');
  const sides=[],angles=[];if(right)angles.push(right);
  for(const part of m[3].split(', ')){let p;
   if((p=/^∠([ABC])=∠([DEF])$/.exec(part))){if(cor(p[1])!==p[2])throw Error('angle correspondence');angles.push(p[1]);}
   else if((p=/^([ABC]{2})=([DEF]{2})$/.exec(part))){if([...p[1]].map(cor).join('')!==p[2])throw Error('side correspondence');sides.push(p[1]);}
   else throw Error(`part ${part}`);}
  let name;
  if(right){const hyp=sides.find(s=>!s.includes(right));const legs=sides.filter(s=>s.includes(right));const acute=angles.filter(a=>a!==right);
   if(hyp&&acute.length===1&&sides.length===1)name='RHA 합동';else if(hyp&&legs.length===1)name='RHS 합동';
   else if(legs.length===2)name='SAS 합동';
   else if(legs.length===1&&acute.length===1&&legs[0].includes(acute[0]))name='ASA 합동';else throw Error('unclassified right-triangle parts');}
  else {if(sides.length===3)name='SSS 합동';
   else if(sides.length===2&&angles.length===1){const shared=[...sides[0]].find(c=>sides[1].includes(c));name=angles[0]===shared?'SAS 합동':'합동이라고 할 수 없다';}
   else if(sides.length===1&&angles.length===2&&angles.every(a=>sides[0].includes(a)))name='ASA 합동';else throw Error('unclassified parts');}
  return opts.map(o=>o===name);},
 'iso-equal-angles'(q,opts){const m=M(/^△ABC에서 ([ABC])([ABC])=([ABC])([ABC])일 때, 크기가 같은 두 각을 고르시오\.$/,q.prompt);if(m[1]!==m[3])throw Error('apex');
  // angles opposite the two equal sides
  const eq=[oppVertex(m[1]+m[2]),oppVertex(m[3]+m[4])].sort();return opts.map(o=>o===`∠${eq[0]}와 ∠${eq[1]}`);},
 'iso-equal-sides'(q,opts){const m=M(/^△ABC에서 ∠([ABC])=∠([ABC])일 때, 길이가 같은 두 변을 고르시오\.$/,q.prompt);const s=[OPP[m[1]],OPP[m[2]]].sort();return opts.map(o=>o===`${s[0]}와 ${s[1]}`);},
};
// ═════════════════════════ m2s2-u2 사각형의 성질 ═════════════════════════
const QV=['A','B','C','D'];const qOpp=v=>QV[(QV.indexOf(v)+2)%4];const adjacent=(u,v)=>u!==v&&qOpp(u)!==v;
// Parallelogram angle system: A=C, B=D, A+B+C+D=360. Returns all four angles from one.
function pgAngles(v,x){const y=solve(t=>2*x+2*t===360,1,179);const r={};for(const w of QV)r[w]=w===v||w===qOpp(v)?x:y;return r;}
const halfOf=(seg,diag)=>{const other=seg.replace('O','');if(!diag[other])throw Error(`segment ${seg}`);return solve(h=>2*h===diag[other],1,400);};
const U2={
 'pg-diag-one'(q){const m=M(/^평행사변형 ABCD의 두 대각선의 교점을 O라고 하자\. AC=(\d+) cm일 때, AO의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return solve(h=>h+h===N(m[1]),1,400);},
 'pg-angle-opposite'(q){const m=M(/^평행사변형 ABCD에서 ∠([A-D])=(\d+)°일 때, ∠([A-D])의 크기는 몇 도인지 구하시오\.$/,q.prompt);if(qOpp(m[1])!==m[3])throw Error('not opposite');return pgAngles(m[1],N(m[2]))[m[3]];},
 'pg-angle-adjacent'(q){const m=M(/^평행사변형 ABCD에서 ∠([A-D])=(\d+)°일 때, ∠([A-D])의 크기는 몇 도인지 구하시오\.$/,q.prompt);if(!adjacent(m[1],m[3]))throw Error('not adjacent');return pgAngles(m[1],N(m[2]))[m[3]];},
 'pg-ratio'(q){const m=M(/^평행사변형 ABCD에서 ∠([A-D]):∠([A-D])=(\d+):(\d+)일 때, ∠([A-D])의 크기는 몇 도인지 구하시오\.$/,q.prompt);if(!adjacent(m[1],m[2]))throw Error('ratio of non-adjacent angles');
  const a=N(m[3]),b=N(m[4]);const x=solve(x=>{const y=180-x;return x*b===y*a;},1,179);return pgAngles(m[1],x)[m[5]];},
 'pg-perimeter'(q){const m=M(/^평행사변형 ABCD에서 AB=(\d+) cm, BC=(\d+) cm일 때, 평행사변형 ABCD의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);const s={AB:N(m[1]),BC:N(m[2])};s.CD=s.AB;s.DA=s.BC;return s.AB+s.BC+s.CD+s.DA;},
 'pg-side-from-perimeter'(q){const m=M(/^평행사변형 ABCD의 둘레의 길이가 (\d+) cm이고 AB=(\d+) cm일 때, AD의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return solve(x=>2*N(m[2])+2*x===N(m[1]),1,400);},
 'pg-diag-halves'(q){const m=M(/^평행사변형 ABCD의 두 대각선의 교점을 O라고 하자\. AC=(\d+) cm, BD=(\d+) cm일 때, ([A-D]O)\+([A-D]O)의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const diag={A:N(m[1]),C:N(m[1]),B:N(m[2]),D:N(m[2])};return halfOf(m[3],diag)+halfOf(m[4],diag);},
 'pg-triangle-perimeter'(q){const m=M(/^평행사변형 ABCD의 두 대각선의 교점을 O라고 하자\. AC=(\d+) cm, BD=(\d+) cm, AB=(\d+) cm일 때, △OCD의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const oc=halfOf('OC',{C:N(m[1])}),od=halfOf('OD',{D:N(m[2])}),cd=N(m[3]);if(!(oc+od>cd&&oc+cd>od&&od+cd>oc))throw Error('△OCD impossible');return oc+od+cd;},
 'rect-diag-half'(q){const m=M(/^직사각형 ABCD의 두 대각선의 교점을 O라고 하자\. (AC|BD)=(\d+) cm일 때, (O[A-D])의 길이는 몇 cm인지 구하시오\.$/,q.prompt);const L=N(m[2]);return halfOf(m[3],{A:L,B:L,C:L,D:L});},
 'rect-oad'(q){const x=N(M(/^직사각형 ABCD의 두 대각선의 교점을 O라고 하자\. ∠OAB=(\d+)°일 때, ∠OAD의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);return solve(t=>t+x===90,1,89);},
 'rect-aob'(q){const x=N(M(/^직사각형 ABCD의 두 대각선의 교점을 O라고 하자\. ∠OAB=(\d+)°일 때, ∠AOB의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);return solve(t=>t+x+x===180,1,179);},
 'rect-oda'(q){const y=N(M(/^직사각형 ABCD의 두 대각선의 교점을 O라고 하자\. ∠AOD=(\d+)°일 때, ∠ODA의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);return solve(t=>2*t+y===180,1,89);},
 'rh-oba'(q){const x=N(M(/^마름모 ABCD의 두 대각선의 교점을 O라고 하자\. ∠OAB=(\d+)°일 때, ∠OBA의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);return solve(t=>t+x+90===180,1,89);},
 'rh-bac'(q){const x=N(M(/^마름모 ABCD에서 ∠BAD=(\d+)°일 때, ∠BAC의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);return solve(t=>2*t===x,1,89);},
 'rh-abd'(q){const x=N(M(/^마름모 ABCD에서 ∠A=(\d+)°일 때, ∠ABD의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);return solve(t=>2*t+x===180,1,89);},
 'rh-side-from-perimeter'(q){const m=M(/^마름모 ABCD의 둘레의 길이가 (\d+) cm일 때, (AB|BC|CD|DA)의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return solve(t=>4*t===N(m[1]),1,400);},
 'rh-area'(q){const m=M(/^마름모 ABCD의 두 대각선의 길이가 AC=(\d+) cm, BD=(\d+) cm일 때, 마름모 ABCD의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  // four congruent right triangles with legs AC/2, BD/2
  const a=halfOf('OA',{A:N(m[1])}),b=halfOf('OB',{B:N(m[2])});return solve(S=>2*S===4*a*b,1,2000);},
 'sq-diag-half'(q){const m=M(/^정사각형 ABCD의 두 대각선의 교점을 O라고 하자\. BD=(\d+) cm일 때, OA의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return halfOf('OA',{A:N(m[1])});},
 'sq-bcp'(q){const x=N(M(/^정사각형 ABCD의 대각선 BD 위에 점 P가 있다\. ∠BAP=(\d+)°일 때, ∠BCP의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);if(x>=90)throw Error('P outside BD');
  // BD is an axis of symmetry of the square taking A to C and fixing P.
  return x;},
 'sq-apd'(q){const x=N(M(/^정사각형 ABCD의 대각선 BD 위에 점 P가 있다\. ∠BAP=(\d+)°일 때, ∠APD의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);const apb=solve(t=>t+45+x===180,1,179);return solve(t=>t+apb===180,1,179);},
 'sq-apb'(q){const x=N(M(/^정사각형 ABCD의 대각선 BD 위에 점 P가 있다\. ∠BAP=(\d+)°일 때, ∠APB의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);return solve(t=>t+45+x===180,1,179);},
 'isotrap-base'(q){const x=N(M(/^AD∥BC인 등변사다리꼴 ABCD에서 ∠B=(\d+)°일 때, ∠C의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);return x;},
 'isotrap-a-to-c'(q){const y=N(M(/^AD∥BC인 등변사다리꼴 ABCD에서 ∠A=(\d+)°일 때, ∠C의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);const B=solve(t=>t+y===180,1,179);return B;},
 'isotrap-diag'(q){return N(M(/^AD∥BC인 등변사다리꼴 ABCD에서 AC=(\d+) cm일 때, BD의 길이는 몇 cm인지 구하시오\.$/,q.prompt)[1]);},
 'pg-bisector-ec'(q){const m=M(/^평행사변형 ABCD에서 ∠A의 이등분선이 변 BC와 만나는 점을 E라고 하자\. AB=(\d+) cm, AD=(\d+) cm일 때, EC의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ab=N(m[1]),bc=N(m[2]);const be=ab;/* △ABE isosceles: ∠BAE=∠DAE=∠AEB */if(be>=bc)throw Error('E not on BC');return solve(x=>be+x===bc,1,400);},
 'pg-bisector-fd'(q){const m=M(/^평행사변형 ABCD에서 ∠B의 이등분선이 변 AD와 만나는 점을 F라고 하자\. AB=(\d+) cm, BC=(\d+) cm일 때, FD의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const af=N(m[1]),ad=N(m[2]);if(af>=ad)throw Error('F not on AD');return solve(x=>af+x===ad,1,400);},
 'pg-bisector-aeb'(q){const b=N(M(/^평행사변형 ABCD에서 ∠A의 이등분선이 변 BC와 만나는 점을 E라고 하자\. ∠B=(\d+)°일 때, ∠AEB의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);
  const A=pgAngles('B',b).A;const bae=solve(t=>2*t===A,1,89);return solve(t=>t+bae+b===180,1,179);},
 'pg-quarter'(q){const m=M(/^넓이가 (\d+) cm²인 평행사변형 ABCD의 두 대각선의 교점을 O라고 할 때, △(ABO|BCO|CDO|DAO)의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);return solve(t=>4*t===N(m[1]),1,2000);},
 'pg-half'(q){const m=M(/^넓이가 (\d+) cm²인 평행사변형 ABCD에서 △(ABC|BCD|CDA|DAB)의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);return solve(t=>2*t===N(m[1]),1,2000);},
 'pg-inner-point'(q){const m=M(/^평행사변형 ABCD의 내부의 한 점 P에 대하여 △(P[A-D]{2})의 넓이가 (\d+) cm², △(P[A-D]{2})의 넓이가 (\d+) cm²일 때, 평행사변형 ABCD의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  const s1=m[1].slice(1),s2=m[3].slice(1);if([...s1,...s2].sort().join('')!=='ABCD'||!['AB','BC','CD','AD'].includes([...s1].sort().join('')))throw Error('need opposite sides');return 2*(N(m[2])+N(m[4]));},
 'trap-abc-dbc'(q){return N(M(/^AD∥BC인 사다리꼴 ABCD의 두 대각선의 교점을 O라고 하자\. △ABC의 넓이가 (\d+) cm²일 때, △DBC의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt)[1]);},
 'trap-abo-dco'(q){return N(M(/^AD∥BC인 사다리꼴 ABCD의 두 대각선의 교점을 O라고 하자\. △ABO의 넓이가 (\d+) cm²일 때, △DCO의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt)[1]);},
 'trap-abo-from'(q){const m=M(/^AD∥BC인 사다리꼴 ABCD의 두 대각선의 교점을 O라고 하자\. △DBC의 넓이가 (\d+) cm², △OBC의 넓이가 (\d+) cm²일 때, △ABO의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  const S=N(m[1]),t=N(m[2]);if(!(t>0&&t<S))throw Error('area order');return solve(x=>x+t===S,1,2000);},
};
// ── lattice oracle ──
const LAT=(()=>{const G=6,P=[];for(let x=0;x<=G;x++)for(let y=0;y<=G;y++)P.push([x,y]);const out=[];
 const cr=(o,a,b)=>(a[0]-o[0])*(b[1]-o[1])-(a[1]-o[1])*(b[0]-o[0]);
 for(const a of P)for(const b of P){if(a===b)continue;for(const c of P){if(c===a||c===b||cr(a,b,c)<=0)continue;for(const d of P){if(d===a||d===b||d===c)continue;
  if(cr(b,c,d)>0&&cr(c,d,a)>0&&cr(d,a,b)>0){
   // diagonal intersection O, all points scaled by den so O is integral
   const den=(c[0]-a[0])*(d[1]-b[1])-(c[1]-a[1])*(d[0]-b[0]);const tn=(b[0]-a[0])*(d[1]-b[1])-(b[1]-a[1])*(d[0]-b[0]);
   const S=p=>[p[0]*den,p[1]*den];const O=[a[0]*den+tn*(c[0]-a[0]),a[1]*den+tn*(c[1]-a[1])];
   out.push({raw:{A:a,B:b,C:c,D:d},sc:{A:S(a),B:S(b),C:S(c),D:S(d),O}});}}}}
 stats.latticeQuads=out.length;return out;})();
const sub=(p,q)=>[p[0]-q[0],p[1]-q[1]],dot=(u,v)=>u[0]*v[0]+u[1]*v[1],crs=(u,v)=>u[0]*v[1]-u[1]*v[0];
const QNB={A:['D','B'],B:['A','C'],C:['B','D'],D:['C','A']};
function angParts(Q,s){ // angle named ∠X (quad interior) or ∠XYZ
 const pts=s.includes('O')?Q.sc:Q.raw;const [p,v,r]=s.length===1?[QNB[s][0],s,QNB[s][1]]:[s[0],s[1],s[2]];
 const u=sub(pts[p],pts[v]),w=sub(pts[r],pts[v]);return {d:BigInt(dot(u,w)),n:BigInt(dot(u,u))*BigInt(dot(w,w))};}
const angEq=(Q,s,t)=>{const a=angParts(Q,s),b=angParts(Q,t);return (a.d>0n)===(b.d>0n)&&(a.d===0n)===(b.d===0n)&&a.d*a.d*b.n===b.d*b.d*a.n;};
const sgn=x=>x>0n?1:x<0n?-1:0;
// supplementary ⇔ cos X = −cos Y: opposite signs of the dot products and equal squared cosines
const angSupp=(Q,s,t)=>{const a=angParts(Q,s),b=angParts(Q,t);return sgn(a.d)===-sgn(b.d)&&a.d*a.d*b.n===b.d*b.d*a.n;};
const L2=(Q,s)=>{const pts=Q.sc;const u=sub(pts[s[0]],pts[s[1]]);return dot(u,u);};
const V2=(Q,s)=>sub(Q.sc[s[1]],Q.sc[s[0]]);
function atom(Q,t){let m;
 if((m=/^([A-DO]{2})∥([A-DO]{2})$/.exec(t)))return crs(V2(Q,m[1]),V2(Q,m[2]))===0;
 if((m=/^([A-DO]{2})⊥([A-DO]{2})$/.exec(t)))return dot(V2(Q,m[1]),V2(Q,m[2]))===0;
 if((m=/^∠([A-DO]{1,3})=90°$/.exec(t)))return angParts(Q,m[1]).d===0n;
 if((m=/^∠([A-D])\+∠([A-D])=180°$/.exec(t)))return angSupp(Q,m[1],m[2]);
 if((m=/^∠([A-DO]{1,3})=∠([A-DO]{1,3})$/.exec(t)))return angEq(Q,m[1],m[2]);
 if(/^[A-DO]{2}(=[A-DO]{2})+$/.test(t)){const s=t.split('=');return s.every(x=>L2(Q,x)===L2(Q,s[0]));}
 if((m=/^([A-DO]{2})≠([A-DO]{2})$/.exec(t)))return L2(Q,m[1])!==L2(Q,m[2]);
 throw Error(`unknown condition atom ${t}`);}
const TYPE={사다리꼴:'AB∥DC|AD∥BC',평행사변형:'AB∥DC,AD∥BC',직사각형:'AB∥DC,AD∥BC,∠A=90°',마름모:'AB=BC=CD=DA',정사각형:'AB=BC=CD=DA,∠A=90°',등변사다리꼴:'AD∥BC,∠B=∠C|AB∥DC,∠A=∠B'};
const DPROP={'서로 다른 것을 이등분한다':'OA=OC,OB=OD','길이가 같다':'AC=BD','서로 수직이다':'AC⊥BD','서로 다른 것을 수직이등분한다':'OA=OC,OB=OD,AC⊥BD','길이가 같고 서로 다른 것을 이등분한다':'AC=BD,OA=OC,OB=OD','한 대각선이 다른 대각선보다 항상 길다':'AC≠BD'};
const memo=new Map();
function holds(Q,expr){ // expr: alternatives by '|', conjunction by ',' or ', '
 return expr.split('|').some(alt=>alt.split(/, ?/).every(t=>atom(Q,t)));}
function sat(expr){if(!memo.has(expr)){memo.set(expr,LAT.map(Q=>holds(Q,expr)));}return memo.get(expr);}
function always(premise,claim){stats.latticeClaims++;const P=sat(premise),C=sat(claim);let n=0;for(let i=0;i<P.length;i++)if(P[i]){n++;if(!C[i])return false;}if(!n)throw Error(`no lattice quadrilateral satisfies ${premise}`);return true;}
const U2C={
 'pg-becomes'(q,opts){const m=M(/^평행사변형 ABCD(?:의 두 대각선의 교점을 O라고 할 때,|에서) (.+)이면 □ABCD는 어떤 사각형인지 고르시오\.$/,q.prompt);const prem=`${TYPE.평행사변형},${m[1]}`;
  return opts.map(o=>o==='평행사변형이 아니다'?false:always(prem,TYPE[o]));},
 'pg-square-condition'(q,opts){M(/^평행사변형 ABCD의 두 대각선의 교점을 O라고 할 때, □ABCD가 정사각형이 되는 조건을 고르시오\./,q.prompt);return opts.map(o=>always(`${TYPE.평행사변형},${o}`,TYPE.정사각형));},
 'pg-condition-yes'(q,opts){M(/^두 대각선의 교점이 O인 □ABCD가 평행사변형이 되는 것을 고르시오\./,q.prompt);return opts.map(o=>always(o,TYPE.평행사변형));},
 'pg-condition-no'(q,opts){M(/^두 대각선의 교점이 O인 □ABCD가 평행사변형이 되지 않을 수도 있는 것을 고르시오\./,q.prompt);return opts.map(o=>!always(o,TYPE.평행사변형));},
 'rel-true'(q,opts){M(/^사각형 사이의 관계로 옳은 것을 고르시오\./,q.prompt);return opts.map(o=>{const m=M(/^(\S+?)[은는] (\S+?)이다$/,o);return always(TYPE[m[1]],TYPE[m[2]]);});},
 'rel-false'(q,opts){M(/^사각형 사이의 관계로 옳지 않은 것을 고르시오\./,q.prompt);return opts.map(o=>{const m=M(/^(\S+?)[은는] (\S+?)이다$/,o);return !always(TYPE[m[1]],TYPE[m[2]]);});},
 'diag-property'(q,opts){const m=M(/^(\S+)의 두 대각선에 대한 설명으로 항상 옳은 것을 고르시오\./,q.prompt);return opts.map(o=>{if(!DPROP[o])throw Error(`property ${o}`);return always(TYPE[m[1]],DPROP[o]);});},
};

// ═════════════════════════ m2s2-u3 도형의 닮음 ═════════════════════════
// correspondence from the similarity symbol: △ABC∽△XYZ → A↔X, B↔Y, C↔Z
const corrMap=t2=>({A:t2[0],B:t2[1],C:t2[2]});
const mapSide=(s,m)=>[...s].map(c=>m[c]).sort().join('');
const same2=(s,t)=>[...s].sort().join('')===[...t].sort().join('');
const U3={
 'sim-side'(q){const m=M(/^△ABC∽△([DEF]{3})이고 ([ABC]{2})=(\d+) cm, ([DEF]{2})=(\d+) cm, ([ABC]{2})=(\d+) cm일 때, ([DEF]{2})의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const map=corrMap(m[1]);if(!same2(mapSide(m[2],map),m[4]))throw Error('given pair does not correspond');if(!same2(mapSide(m[6],map),m[8]))throw Error('asked side does not correspond');
  if(same2(m[2],m[6]))throw Error('same side twice');const a=N(m[3]),b=N(m[5]),c=N(m[7]);return solve(x=>a*x===b*c,1,400);},
 'sim-ratio-given'(q){const m=M(/^△ABC∽△DEF이고 닮음비가 (\d+):(\d+)이다\. ([ABC]{2})=(\d+) cm일 때, ([DEF]{2})의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  if(!same2(mapSide(m[3],corrMap('DEF')),m[5]))throw Error('correspondence');return solve(x=>N(m[1])*x===N(m[2])*N(m[4]),1,400);},
 'sim-angle'(q){const m=M(/^△ABC∽△([DEF]{3})이고 ∠A=(\d+)°, ∠B=(\d+)°일 때, ∠([DEF])의 크기는 몇 도인지 구하시오\.$/,q.prompt);
  const map=corrMap(m[1]);const ang={A:N(m[2]),B:N(m[3])};ang.C=solve(c=>c+ang.A+ang.B===180,1,179);const back=Object.keys(map).find(k=>map[k]===m[4]);return ang[back];},
 'sim-perimeter'(q){const m=M(/^△ABC∽△DEF이고 닮음비가 (\d+):(\d+)이다\. △ABC의 둘레의 길이가 (\d+) cm일 때, △DEF의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return solve(x=>N(m[1])*x===N(m[2])*N(m[3]),1,2000);},
 'sim-sss-perimeter'(q){const m=M(/^△ABC∽△DEF이고 AB=(\d+) cm, BC=(\d+) cm, AC=(\d+) cm, DE=(\d+) cm일 때, △DEF의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const [a,b,c,de]=[1,2,3,4].map(i=>N(m[i]));const s=[a,b,c].sort((x,y)=>x-y);if(s[0]+s[1]<=s[2])throw Error('not a triangle');
  // side by side: EF=BC×DE÷AB, DF=AC×DE÷AB; perimeter must be the rational sum
  return solve(x=>a*x===de*a+b*de+c*de,1,2000);},
 'sim-area'(q){const m=M(/^(?:△ABC∽△DEF|□ABCD∽□EFGH)이고 닮음비가 (\d+):(\d+)이다\. (?:△ABC|□ABCD)의 넓이가 (\d+) cm²일 때, (?:△DEF|□EFGH)의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  const p=N(m[1]),r=N(m[2]);return solve(x=>p*p*x===r*r*N(m[3]),1,4000);},
 'sim-area-sides'(q){const m=M(/^△ABC∽△DEF이고 AB=(\d+) cm, DE=(\d+) cm이다\. △ABC의 넓이가 (\d+) cm²일 때, △DEF의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  const ab=N(m[1]),de=N(m[2]);return solve(x=>ab*ab*x===de*de*N(m[3]),1,4000);},
 'sim-area-inverse'(q){const m=M(/^넓이가 각각 (\d+) cm², (\d+) cm²인 닮은 두 삼각형이 있다\. 작은 삼각형의 한 변의 길이가 (\d+) cm일 때, 이에 대응하는 큰 삼각형의 변의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const s1=N(m[1]),s2=N(m[2]),a=N(m[3]);if(s1>=s2)throw Error('order');return solve(x=>x*x*s1===a*a*s2,1,400);},
 'sim-volume'(q){const m=M(/^닮은 두 (\S+) \(가\), \(나\)의 닮음비가 (\d+):(\d+)이다\. \(가\)의 부피가 (\d+) cm³일 때, \(나\)의 부피는 몇 cm³인지 구하시오\.$/,q.prompt);const p=N(m[2]),r=N(m[3]);return solve(x=>p**3*x===r**3*N(m[4]),1,4000);},
 'sim-surface'(q){const m=M(/^닮은 두 (\S+) \(가\), \(나\)의 닮음비가 (\d+):(\d+)이다\. \(가\)의 겉넓이가 (\d+) cm²일 때, \(나\)의 겉넓이는 몇 cm²인지 구하시오\.$/,q.prompt);const p=N(m[2]),r=N(m[3]);return solve(x=>p*p*x===r*r*N(m[4]),1,4000);},
 'rt-altitude'(q){const m=M(/^∠A=90°인 직각삼각형 ABC의 꼭짓점 A에서 BC에 내린 수선의 발을 D라고 하자\. BD=(\d+) cm, DC=(\d+) cm일 때, AD의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  // coordinates: D=(0,0), B=(−BD,0), C=(DC,0), A=(0,h); ∠A=90° ⇔ (B−A)·(C−A)=0
  const a=N(m[1]),b=N(m[2]);stats.coordinateChecks++;return solve(h=>(-a)*b+h*h===0,1,400);},
 'rt-segment'(q){const m=M(/^∠A=90°인 직각삼각형 ABC의 꼭짓점 A에서 BC에 내린 수선의 발을 D라고 하자\. AD=(\d+) cm, BD=(\d+) cm일 때, DC의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const h=N(m[1]),a=N(m[2]);stats.coordinateChecks++;return solve(c=>(-a)*c+h*h===0,1,4000);},
 'rt-leg-ab'(q){const m=M(/^∠A=90°인 직각삼각형 ABC의 꼭짓점 A에서 BC에 내린 수선의 발을 D라고 하자\. BD=(\d+) cm, BC=(\d+) cm일 때, AB의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const bd=N(m[1]),dc=N(m[2])-bd;if(dc<=0)throw Error('D outside BC');const ad2=bd*dc;/* altitude */return solve(x=>x*x===bd*bd+ad2,1,400);},
 'rt-leg-ac'(q){const m=M(/^∠A=90°인 직각삼각형 ABC의 꼭짓점 A에서 BC에 내린 수선의 발을 D라고 하자\. CD=(\d+) cm, BC=(\d+) cm일 때, AC의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const cd=N(m[1]),bd=N(m[2])-cd;if(bd<=0)throw Error('D outside BC');return solve(x=>x*x===cd*cd+cd*bd,1,400);},
 'rt-hyp'(q){const m=M(/^∠A=90°인 직각삼각형 ABC의 꼭짓점 A에서 BC에 내린 수선의 발을 D라고 하자\. AB=(\d+) cm, BD=(\d+) cm일 때, BC의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ab=N(m[1]),bd=N(m[2]);const ad2=ab*ab-bd*bd;if(ad2<=0)throw Error('BD ≥ AB');return solve(x=>bd*(x-bd)===ad2,1,4000);},
 'sas-overlap'(q){const m=M(/^△ABC에서 변 AB 위의 점 D에 대하여 AD=(\d+) cm, AC=(\d+) cm, AB=(\d+) cm, BC=(\d+) cm일 때, CD의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const [ad,ac,ab,bc]=[1,2,3,4].map(i=>N(m[i]));if(!(ad<ab))throw Error('D not on AB');if(!(ab+ac>bc&&ab+bc>ac&&ac+bc>ab))throw Error('not a triangle');
  if(ad*ab!==ac*ac)throw Error('AD:AC≠AC:AB');const db=ab-ad;
  // Stewart's theorem (no similarity): AC²·DB + BC²·AD = AB·(CD² + AD·DB)
  const num=ac*ac*db+bc*bc*ad-ab*ad*db;if(num%ab)throw Error('CD² not integral');return solve(x=>x*x===num/ab,1,400);},
 'aa-overlap-ac'(q){const m=M(/^△ABC에서 변 AB 위의 점 D에 대하여 ∠ACD=∠ABC이고 AD=(\d+) cm, AB=(\d+) cm일 때, AC의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ad=N(m[1]),ab=N(m[2]);if(ad>=ab)throw Error('D not on AB');return solve(x=>ad*ab===x*x,1,400);},
 'aa-overlap-ab'(q){const m=M(/^△ABC에서 변 AB 위의 점 D에 대하여 ∠ACD=∠ABC이고 AD=(\d+) cm, AC=(\d+) cm일 때, AB의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ad=N(m[1]),ac=N(m[2]);const ab=solve(x=>ad*x===ac*ac,1,4000);if(ab<=ad)throw Error('D not on AB');return ab;},
};
const U3F={
 'f-sim-ratio'(q){const m=M(/^△ABC∽△([DEF]{3})이고 ([ABC]{2})=(\d+) cm, ([DEF]{2})=(\d+) cm일 때, △\1의 각 변의 길이는 △ABC의 대응하는 변의 길이의 몇 배인지 기약분수로 구하시오\.$/,q.prompt);
  if(!same2(mapSide(m[2],corrMap(m[1])),m[4]))throw Error('correspondence');return [N(m[5]),N(m[3])];},
 'f-area-ratio'(q){const m=M(/^닮음비가 (\d+):(\d+)인 닮은 두 삼각형에서 (큰|작은) 삼각형의 넓이는 (큰|작은) 삼각형의 넓이의 몇 배인지 기약분수로 구하시오\.$/,q.prompt);
  const lo=Math.min(N(m[1]),N(m[2])),hi=Math.max(N(m[1]),N(m[2]));if(m[3]===m[4])throw Error('compare');return m[3]==='큰'?[hi*hi,lo*lo]:[lo*lo,hi*hi];},
 'f-area-sides'(q){const m=M(/^△ABC∽△DEF이고 AB=(\d+) cm, DE=(\d+) cm일 때, △DEF의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오\.$/,q.prompt);return [N(m[2])**2,N(m[1])**2];},
 'f-volume-ratio'(q){const m=M(/^닮음비가 (\d+):(\d+)인 닮은 두 (\S+)에서 (큰|작은) \3의 부피는 (큰|작은) \3의 부피의 몇 배인지 기약분수로 구하시오\.$/,q.prompt);
  const lo=Math.min(N(m[1]),N(m[2])),hi=Math.max(N(m[1]),N(m[2]));if(m[4]===m[5])throw Error('compare');return m[4]==='큰'?[hi**3,lo**3]:[lo**3,hi**3];},
 'f-surface-ratio'(q){const m=M(/^닮음비가 (\d+):(\d+)인 닮은 두 (\S+)에서 큰 \3의 겉넓이는 작은 \3의 겉넓이의 몇 배인지 기약분수로 구하시오\.$/,q.prompt);
  const lo=Math.min(N(m[1]),N(m[2])),hi=Math.max(N(m[1]),N(m[2]));return [hi*hi,lo*lo];},
 'f-perimeter-from-area'(q){const m=M(/^넓이가 각각 (\d+) cm², (\d+) cm²인 닮은 두 삼각형에서 큰 삼각형의 둘레의 길이는 작은 삼각형의 둘레의 길이의 몇 배인지 기약분수로 구하시오\.$/,q.prompt);
  const A1=N(m[1]),A2=N(m[2]);const hits=[];for(let n=1;n<=60;n++)for(let d=1;d<=60;d++){stats.bruteForce++;if(gcd(n,d)===1&&n*n*A1===d*d*A2)hits.push([n,d]);}if(hits.length!==1)throw Error('ratio search');return hits[0];},
};
const SHAPE_DOF={'두 정사각형':0,'두 원':0,'두 정삼각형':0,'두 직각이등변삼각형':0,'두 정육면체':0,'두 구':0,'중심각의 크기가 같은 두 부채꼴':0,
 '두 직사각형':1,'두 마름모':1,'두 이등변삼각형':1,'두 직각삼각형':1,'두 평행사변형':2,'두 원기둥':1,'두 사다리꼴':3,'두 부채꼴':1,'두 원뿔':1,'두 직육면체':2};
function simData(q){const m=M(/^△ABC와 △DEF에서 (.+)일 때, 두 삼각형이 닮음인지 판별하여 알맞은 것을 고르시오\.$/,q.prompt);const S={},A={};
 for(const part of m[1].split(', ')){let p;if((p=/^([A-F]{2})=(\d+) cm$/.exec(part)))S[p[1]]=N(p[2]);else if((p=/^∠([A-F])=(\d+)°$/.exec(part)))A[p[1]]=N(p[2]);else throw Error(`part ${part}`);}
 const t1=o=>Object.keys(o).filter(k=>/^[ABC]+$/.test(k)),t2=o=>Object.keys(o).filter(k=>/^[DEF]+$/.test(k));
 const s1=t1(S),s2=t2(S),a1=t1(A),a2=t2(A);
 if(s1.length===3&&s2.length===3&&!a1.length){const x=s1.map(k=>S[k]).sort((a,b)=>a-b),y=s2.map(k=>S[k]).sort((a,b)=>a-b);
  if(x[0]+x[1]<=x[2]||y[0]+y[1]<=y[2])throw Error('not triangles');return x[0]*y[1]===x[1]*y[0]&&x[0]*y[2]===x[2]*y[0]?'SSS 닮음':'닮음이 아니다';}
 if(s1.length===2&&s2.length===2&&a1.length===1&&a2.length===1){const cor=corrMap('DEF');const inc=[...s1[0]].find(c=>s1[1].includes(c));if(inc!==a1[0]||cor[inc]!==a2[0])throw Error('angle not included / not corresponding');
  const [u,v]=s1;const U=mapSide(u,cor),V=mapSide(v,cor);if(!(U in S)||!(V in S))throw Error('sides not corresponding');
  if(A[a1[0]]!==A[a2[0]])throw Error('SAS with unequal included angle is not generated');return S[u]*S[V]===S[v]*S[U]?'SAS 닮음':(()=>{throw Error('SAS ratios unequal');})();}
 if(!s1.length&&!s2.length&&a1.length===2&&a2.length===2){const th=(o,k)=>{const v=k.map(x=>o[x]);return [...v,180-v[0]-v[1]].sort((a,b)=>a-b);};const x=th(A,a1),y=th(A,a2);if(x.some(v=>v<=0)||y.some(v=>v<=0))throw Error('angles');return x.join()===y.join()?'AA 닮음':'닮음이 아니다';}
 throw Error('unrecognised data');}
const U3C={
 'always-similar'(q,opts){M(/^다음 중 항상 닮은 도형인 것을 고르시오\./,q.prompt);return opts.map(o=>{if(!(o in SHAPE_DOF))throw Error(o);return SHAPE_DOF[o]===0;});},
 'not-always-similar'(q,opts){M(/^다음 중 항상 닮은 도형이라고 할 수 없는 것을 고르시오\./,q.prompt);return opts.map(o=>{if(!(o in SHAPE_DOF))throw Error(o);return SHAPE_DOF[o]>0;});},
 'corr-side'(q,opts){const m=M(/^△ABC∽△([DEF]{3})일 때, 변 ([ABC]{2})에 대응하는 변을 고르시오\.$/,q.prompt);const c=mapSide(m[2],corrMap(m[1]));return opts.map(o=>o!=='알 수 없다'&&same2(o,c));},
 'corr-angle'(q,opts){const m=M(/^△ABC∽△([DEF]{3})일 때, ∠([ABC])에 대응하는 각을 고르시오\.$/,q.prompt);return opts.map(o=>o===`∠${corrMap(m[1])[m[2]]}`);},
 'sim-sss'(q,opts){const a=simData(q);return opts.map(o=>o===a);},'sim-sss-not'(q,opts){const a=simData(q);return opts.map(o=>o===a);},
 'sim-sas'(q,opts){const a=simData(q);return opts.map(o=>o===a);},'sim-aa'(q,opts){const a=simData(q);return opts.map(o=>o===a);},'sim-aa-not'(q,opts){const a=simData(q);return opts.map(o=>o===a);},
};

// ═════════════════════════ m2s2-u4 평행선과 선분의 길이의 비 ═════════════════════════
// Exact rational geometry: q = [num, den] with den > 0.
const Rq=(n,d=1)=>{if(d<0){n=-n;d=-d;}const g=gcd(n,d);return [n/g,d/g];};
const ra=(a,b)=>Rq(a[0]*b[1]+b[0]*a[1],a[1]*b[1]),rs=(a,b)=>Rq(a[0]*b[1]-b[0]*a[1],a[1]*b[1]),rm=(a,b)=>Rq(a[0]*b[0],a[1]*b[1]),rd=(a,b)=>Rq(a[0]*b[1],a[1]*b[0]);
const req=(a,b)=>a[0]*b[1]===b[0]*a[1];const I=n=>Rq(n);
const P=(x,y)=>[I(x),I(y)];const pa=(p,q)=>[ra(p[0],q[0]),ra(p[1],q[1])],ps=(p,q)=>[rs(p[0],q[0]),rs(p[1],q[1])],pm=(p,k)=>[rm(p[0],k),rm(p[1],k)];
const lerp=(p,q,t)=>pa(p,pm(ps(q,p),t));const mid=(p,q)=>lerp(p,q,Rq(1,2));
const cross=(u,v)=>rs(rm(u[0],v[1]),rm(u[1],v[0]));const d2=(p,q)=>{const v=ps(q,p);return ra(rm(v[0],v[0]),rm(v[1],v[1]));};
// scalar t with (r−p) = t(q−p) for collinear points (throws if not collinear)
function along(p,q,r){const u=ps(q,p),v=ps(r,p);if(!req(cross(u,v),I(0)))throw Error('not collinear');return u[0][0]!==0?rd(v[0],u[0]):rd(v[1],u[1]);}
function ratioVec(u,v){if(!req(cross(u,v),I(0)))throw Error('not parallel');return u[0][0]!==0?rd(v[0],u[0]):rd(v[1],u[1]);}// v = t·u
const area2=pts=>{let s=I(0);for(let i=0;i<pts.length;i++){const a=pts[i],b=pts[(i+1)%pts.length];s=ra(s,rs(rm(a[0],b[1]),rm(b[0],a[1])));}return s[0]<0?Rq(-s[0],s[1]):s;};
function lineHit(p,v,a,b){// intersection of line p+s v with segment ab: returns point or null
 const w=ps(b,a);const den=cross(v,w);if(den[0]===0)return null;const t=rd(cross(ps(a,p),v),den);if(t[0]<0||t[0]>t[1])return null;return lerp(a,b,t);}
// intersection of the full line p+s·v with the full line through a,b
function lineLine(p,v,a,b){const w=ps(b,a);const t=rd(cross(ps(p,a),v),cross(w,v));return lerp(a,b,t);}
function clipArea(tri,p,v){// area of the part of triangle on the left of the directed line p+s v
 const side=q=>cross(v,ps(q,p));const out=[];for(let i=0;i<3;i++){const a=tri[i],b=tri[(i+1)%3],sa=side(a),sb=side(b);if(sa[0]>=0)out.push(a);if((sa[0]>0&&sb[0]<0)||(sa[0]<0&&sb[0]>0))out.push(lerp(a,b,rd(sa,rs(sa,sb))));}
 return out.length>=3?area2(out):I(0);}
const TRI={A:P(0,9),B:P(-6,0),C:P(12,0)};const CEN=pm(pa(pa(TRI.A,TRI.B),TRI.C),Rq(1,3));stats.coordinateChecks++;
const MEDV={AD:['A','B','C'],BE:['B','A','C'],CF:['C','A','B']};
const medT=med=>{const [V,X,Y]=MEDV[med];const foot=mid(TRI[X],TRI[Y]);return along(TRI[V],foot,CEN);};// VG/VP
const solveR=(pred)=>solve(x=>pred(I(x)),1,4000);
// DE∥BC on a scalene triangle: returns DE/BC and AE/AC given AD/AB
function deRatio(adOverAb){const [A,B,C]=[P(0,0),P(7,1),P(2,5)];const D=lerp(A,B,adOverAb);const hit=lineHit(D,ps(C,B),A,C);stats.coordinateChecks++;return {ae:along(A,C,hit),de:ratioVec(ps(C,B),ps(hit,D))};}
const U4={
 'de-ec'(q){const m=M(/^△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다\. AD=(\d+) cm, DB=(\d+) cm, AE=(\d+) cm일 때, EC의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ad=N(m[1]),db=N(m[2]),ae=N(m[3]);const t=deRatio(Rq(ad,ad+db)).ae;return solveR(x=>req(t,rd(I(ae),ra(I(ae),x))));},
 'de-ae'(q){const m=M(/^△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다\. AD=(\d+) cm, DB=(\d+) cm, EC=(\d+) cm일 때, AE의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ad=N(m[1]),db=N(m[2]),ec=N(m[3]);const t=deRatio(Rq(ad,ad+db)).ae;return solveR(x=>req(t,rd(x,ra(x,I(ec)))));},
 'de-ae-whole'(q){const m=M(/^△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다\. AD=(\d+) cm, AB=(\d+) cm, AC=(\d+) cm일 때, AE의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ad=N(m[1]),ab=N(m[2]),ac=N(m[3]);if(ad>=ab)throw Error('D not on AB');const t=deRatio(Rq(ad,ab)).ae;return solveR(x=>req(rm(t,I(ac)),x));},
 'de-length'(q){const m=M(/^△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다\. AD=(\d+) cm, AB=(\d+) cm, BC=(\d+) cm일 때, DE의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ad=N(m[1]),ab=N(m[2]),bc=N(m[3]);if(ad>=ab)throw Error('D not on AB');const t=deRatio(Rq(ad,ab)).de;return solveR(x=>req(rm(t,I(bc)),x));},
 'de-length-part'(q){const m=M(/^△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다\. AD=(\d+) cm, DB=(\d+) cm, BC=(\d+) cm일 때, DE의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ad=N(m[1]),db=N(m[2]),bc=N(m[3]);const t=deRatio(Rq(ad,ad+db)).de;return solveR(x=>req(rm(t,I(bc)),x));},
 'ext-ae'(q){const m=M(/^△ABC에서 변 BA의 연장선 위의 점 D와 변 CA의 연장선 위의 점 E에 대하여 DE∥BC이다\. AD=(\d+) cm, AB=(\d+) cm, AC=(\d+) cm일 때, AE의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ad=N(m[1]),ab=N(m[2]),ac=N(m[3]);const [A,B,C]=[P(0,0),P(7,1),P(2,5)];const D=lerp(A,B,Rq(-ad,ab));const E=lineLine(D,ps(C,B),A,C);
  // E = A + s(C−A) on line through D parallel to BC; s<0 means beyond A
  const s=along(A,C,E);if(s[0]>=0)throw Error('E not on the extension beyond A');if(!req(cross(ps(E,D),ps(C,B)),I(0)))throw Error('DE∦BC');stats.coordinateChecks++;
  return solveR(x=>req(rm(Rq(-s[0],s[1]),I(ac)),x));},
 'ext-de'(q){const m=M(/^△ABC에서 변 BA의 연장선 위의 점 D와 변 CA의 연장선 위의 점 E에 대하여 DE∥BC이다\. AD=(\d+) cm, AB=(\d+) cm, BC=(\d+) cm일 때, DE의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ad=N(m[1]),ab=N(m[2]),bc=N(m[3]);const [A,B,C]=[P(0,0),P(7,1),P(2,5)];const D=lerp(A,B,Rq(-ad,ab)),E=lerp(A,C,Rq(-ad,ab));const t=ratioVec(ps(C,B),ps(E,D));stats.coordinateChecks++;
  return solveR(x=>req(rm(Rq(Math.abs(t[0]),t[1]),I(bc)),x));},
 'mid-mn'(q){const bc=N(M(/^△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자\. BC=(\d+) cm일 때, MN의 길이는 몇 cm인지 구하시오\.$/,q.prompt)[1]);const t=ratioVec(ps(TRI.C,TRI.B),ps(mid(TRI.A,TRI.C),mid(TRI.A,TRI.B)));return solveR(x=>req(rm(t,I(bc)),x));},
 'mid-bc'(q){const mn=N(M(/^△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자\. MN=(\d+) cm일 때, BC의 길이는 몇 cm인지 구하시오\.$/,q.prompt)[1]);const t=ratioVec(ps(TRI.C,TRI.B),ps(mid(TRI.A,TRI.C),mid(TRI.A,TRI.B)));return solveR(x=>req(rm(t,x),I(mn)));},
 'mid-converse-nc'(q){const ac=N(M(/^△ABC에서 변 AB의 중점 M을 지나고 변 BC에 평행한 직선이 변 AC와 만나는 점을 N이라고 하자\. AC=(\d+) cm일 때, NC의 길이는 몇 cm인지 구하시오\.$/,q.prompt)[1]);
  const Np=lineHit(mid(TRI.A,TRI.B),ps(TRI.C,TRI.B),TRI.A,TRI.C);const s=along(TRI.C,TRI.A,Np);return solveR(x=>req(rm(s,I(ac)),x));},
 'mid-converse-mn'(q){const bc=N(M(/^△ABC에서 변 AB의 중점 M을 지나고 변 BC에 평행한 직선이 변 AC와 만나는 점을 N이라고 하자\. BC=(\d+) cm일 때, MN의 길이는 몇 cm인지 구하시오\.$/,q.prompt)[1]);
  const Mp=mid(TRI.A,TRI.B),Np=lineHit(Mp,ps(TRI.C,TRI.B),TRI.A,TRI.C);const t=ratioVec(ps(TRI.C,TRI.B),ps(Np,Mp));return solveR(x=>req(rm(t,I(bc)),x));},
 'mid-def-perimeter'(q){const m=M(/^△ABC의 세 변 AB, BC, CA의 중점을 각각 D, E, F라고 하자\. AB=(\d+) cm, BC=(\d+) cm, CA=(\d+) cm일 때, △DEF의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const s=[1,2,3].map(i=>N(m[i])).sort((a,b)=>a-b);if(s[0]+s[1]<=s[2])throw Error('not a triangle');return solve(x=>2*x===s[0]+s[1]+s[2],1,400);},
 'mid-amn-perimeter'(q){const m=M(/^△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자\. AB=(\d+) cm, BC=(\d+) cm, CA=(\d+) cm일 때, △AMN의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const s=[1,2,3].map(i=>N(m[i]));const t=[...s].sort((a,b)=>a-b);if(t[0]+t[1]<=t[2])throw Error('not a triangle');return solve(x=>2*x===s[0]+s[1]+s[2],1,400);},
 'mid-angle-b'(q){return N(M(/^△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자\. ∠B=(\d+)°일 때, ∠AMN의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);},
 'mid-angle-c'(q){return N(M(/^△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자\. ∠C=(\d+)°일 때, ∠ANM의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);},
 'mid-angle-sum'(q){const m=M(/^△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자\. ∠A=(\d+)°, ∠B=(\d+)°일 때, ∠ANM의 크기는 몇 도인지 구하시오\.$/,q.prompt);return solve(c=>c+N(m[1])+N(m[2])===180,1,179);},
 'pl-ef'(q){const m=M(/^서로 평행한 세 직선 l, m, n이 직선 p와 만나는 점을 각각 A, B, C, 직선 q와 만나는 점을 각각 D, E, F라고 하자\. AB=(\d+) cm, BC=(\d+) cm, DE=(\d+) cm일 때, EF의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  // heights of l,m,n measured along p; q crosses with its own scale: EF/DE = BC/AB
  const ab=N(m[1]),bc=N(m[2]),de=N(m[3]);return solveR(x=>req(rd(x,I(de)),Rq(bc,ab)));},
 'pl-df'(q){const m=M(/^서로 평행한 세 직선 l, m, n이 직선 p와 만나는 점을 각각 A, B, C, 직선 q와 만나는 점을 각각 D, E, F라고 하자\. AB=(\d+) cm, AC=(\d+) cm, DE=(\d+) cm일 때, DF의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ab=N(m[1]),ac=N(m[2]),de=N(m[3]);if(ac<=ab)throw Error('B between A and C');return solveR(x=>req(rd(x,I(de)),Rq(ac,ab)));},
 'trap-mn'(q){const m=M(/^AD∥BC인 사다리꼴 ABCD에서 두 변 AB, DC의 중점을 각각 M, N이라고 하자\. AD=(\d+) cm, BC=(\d+) cm일 때, MN의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const a=N(m[1]),b=N(m[2]);const A=P(0,4),D=P(a,4),B=P(-3,0),C=P(b-3,0);const Mp=mid(A,B),Np=mid(D,C);stats.coordinateChecks++;return solveR(x=>req(rs(Np[0],Mp[0]),x));},
 'trap-pq'(q){const m=M(/^AD∥BC인 사다리꼴 ABCD에서 두 변 AB, DC의 중점을 각각 M, N이라고 하자\. MN이 두 대각선 BD, AC와 만나는 점을 각각 P, Q라고 하자\. AD=(\d+) cm, BC=(\d+) cm일 때, PQ의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const a=N(m[1]),b=N(m[2]);const A=P(0,4),D=P(a,4),B=P(-3,0),C=P(b-3,0);const Mp=mid(A,B),Np=mid(D,C),v=ps(Np,Mp);const Pp=lineHit(Mp,v,B,D),Qp=lineHit(Mp,v,A,C);stats.coordinateChecks++;
  const len=rs(Qp[0],Pp[0]);return solveR(x=>req(Rq(Math.abs(len[0]),len[1]),x));},
 'trap-mq'(q){const m=M(/^AD∥BC인 사다리꼴 ABCD에서 두 변 AB, DC의 중점을 각각 M, N이라고 하자\. MN이 대각선 AC와 만나는 점을 Q라고 하자\. AD=(\d+) cm, BC=(\d+) cm일 때, MQ의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const a=N(m[1]),b=N(m[2]);const A=P(0,4),D=P(a,4),B=P(-3,0),C=P(b-3,0);const Mp=mid(A,B),Np=mid(D,C);const Qp=lineHit(Mp,ps(Np,Mp),A,C);stats.coordinateChecks++;return solveR(x=>req(rs(Qp[0],Mp[0]),x));},
 'trap-ef'(q){const m=M(/^AD∥EF∥BC인 사다리꼴 ABCD에서 점 E, F는 각각 변 AB, DC 위에 있고 AE:EB=(\d+):(\d+)이다\. AD=(\d+) cm, BC=(\d+) cm일 때, EF의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const [e,f,a,b]=[1,2,3,4].map(i=>N(m[i]));const A=P(0,5),D=P(a,5),B=P(-2,0),C=P(b-2,0);const E=lerp(A,B,Rq(e,e+f));const F=lineHit(E,P(1,0),D,C);stats.coordinateChecks++;
  if(!F)throw Error('F not on DC');return solveR(x=>req(rs(F[0],E[0]),x));},
 'cg-vertex-part'(q){const m=M(/^점 G가 △ABC의 무게중심이고 (AD|BE|CF)가 중선이다\. \1=(\d+) cm일 때, ([ABC])G의 길이는 몇 cm인지 구하시오\.$/,q.prompt);if(m[3]!==m[1][0])throw Error('vertex');return solveR(x=>req(rm(medT(m[1]),I(N(m[2]))),x));},
 'cg-side-part'(q){const m=M(/^점 G가 △ABC의 무게중심이고 (AD|BE|CF)가 중선이다\. \1=(\d+) cm일 때, G([DEF])의 길이는 몇 cm인지 구하시오\.$/,q.prompt);if(m[3]!==m[1][1])throw Error('foot');return solveR(x=>req(rm(rs(I(1),medT(m[1])),I(N(m[2]))),x));},
 'cg-from-vertex-part'(q){const m=M(/^점 G가 △ABC의 무게중심이고 (AD|BE|CF)가 중선이다\. ([ABC])G=(\d+) cm일 때, G([DEF])의 길이는 몇 cm인지 구하시오\.$/,q.prompt);const t=medT(m[1]);return solveR(x=>req(rm(x,t),rm(I(N(m[3])),rs(I(1),t))));},
 'cg-median'(q){const m=M(/^점 G가 △ABC의 무게중심이고 (AD|BE|CF)가 중선이다\. G([DEF])=(\d+) cm일 때, \1의 길이는 몇 cm인지 구하시오\.$/,q.prompt);const t=medT(m[1]);return solveR(x=>req(rm(x,rs(I(1),t)),I(N(m[3]))));},
 'cg-midpoint'(q){const m=M(/^점 G가 △ABC의 무게중심이고 (AD|BE|CF)가 중선이다\. ([ABC]{2})=(\d+) cm일 때, ([ABC])([DEF])의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const [V,X,Y]=MEDV[m[1]];if([X,Y].sort().join('')!==m[2]||m[5]!==m[1][1]||!m[2].includes(m[4]))throw Error('segment');return solveR(x=>req(rm(along(TRI[m[4]],TRI[m[4]===X?Y:X],mid(TRI[X],TRI[Y])),I(N(m[3]))),x));},
 'cg-two-medians'(q){const m=M(/^△ABC의 두 중선 AD, BE의 교점을 G라고 하자\. AD=(\d+) cm, BE=(\d+) cm일 때, AG\+BG의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return solveR(x=>req(ra(rm(medT('AD'),I(N(m[1]))),rm(medT('BE'),I(N(m[2])))),x));},
 'cg-two-medians-gd'(q){const m=M(/^△ABC의 두 중선 AD, BE의 교점을 G라고 하자\. AD=(\d+) cm, BE=(\d+) cm일 때, GD\+GE의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return solveR(x=>req(ra(rm(rs(I(1),medT('AD')),I(N(m[1]))),rm(rs(I(1),medT('BE')),I(N(m[2])))),x));},
 'cg-parallel-ef'(q){const bc=N(M(/^점 G가 △ABC의 무게중심이고, 점 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 하자\. BC=(\d+) cm일 때, EF의 길이는 몇 cm인지 구하시오\.$/,q.prompt)[1]);
  const v=ps(TRI.C,TRI.B);const E=lineHit(CEN,v,TRI.A,TRI.B),F=lineHit(CEN,v,TRI.A,TRI.C);return solveR(x=>req(rm(ratioVec(v,ps(F,E)),I(bc)),x));},
 'cg-parallel-ae'(q){const ab=N(M(/^점 G가 △ABC의 무게중심이고, 점 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 하자\. AB=(\d+) cm일 때, AE의 길이는 몇 cm인지 구하시오\.$/,q.prompt)[1]);
  const E=lineHit(CEN,ps(TRI.C,TRI.B),TRI.A,TRI.B);return solveR(x=>req(rm(along(TRI.A,TRI.B,E),I(ab)),x));},
 'cg-area-third'(q){const m=M(/^넓이가 (\d+) cm²인 △ABC의 무게중심을 G라고 할 때, △(G[ABC]{2})의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);const r=rd(area2([CEN,TRI[m[2][1]],TRI[m[2][2]]]),area2([TRI.A,TRI.B,TRI.C]));return solveR(x=>req(rm(r,I(N(m[1]))),x));},
 'cg-area-sixth'(q){const S=N(M(/^넓이가 (\d+) cm²인 △ABC의 무게중심을 G, 변 BC의 중점을 D라고 할 때, △GBD의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt)[1]);const r=rd(area2([CEN,TRI.B,mid(TRI.B,TRI.C)]),area2([TRI.A,TRI.B,TRI.C]));return solveR(x=>req(rm(r,I(S)),x));},
 'cg-area-whole'(q){const s=N(M(/^△ABC의 무게중심을 G라고 하자\. △GAB의 넓이가 (\d+) cm²일 때, △ABC의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt)[1]);const r=rd(area2([TRI.A,TRI.B,TRI.C]),area2([CEN,TRI.A,TRI.B]));return solveR(x=>req(rm(r,I(s)),x));},
 'cg-parallel-area'(q){const S=N(M(/^넓이가 (\d+) cm²인 △ABC의 무게중심 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 할 때, △AEF의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt)[1]);
  const v=ps(TRI.C,TRI.B);const r=rd(area2([TRI.A,lineHit(CEN,v,TRI.A,TRI.B),lineHit(CEN,v,TRI.A,TRI.C)]),area2([TRI.A,TRI.B,TRI.C]));return solveR(x=>req(rm(r,I(S)),x));},
 'cg-parallel-trap'(q){const S=N(M(/^넓이가 (\d+) cm²인 △ABC의 무게중심 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 할 때, □EBCF의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt)[1]);
  const v=ps(TRI.C,TRI.B);const E=lineHit(CEN,v,TRI.A,TRI.B),F=lineHit(CEN,v,TRI.A,TRI.C);const r=rd(area2([E,TRI.B,TRI.C,F]),area2([TRI.A,TRI.B,TRI.C]));return solveR(x=>req(rm(r,I(S)),x));},
};
const U4F={
 'f-de-bc'(q){const m=M(/^△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다\. AD=(\d+) cm, DB=(\d+) cm일 때, DE의 길이는 BC의 길이의 몇 배인지 기약분수로 구하시오\.$/,q.prompt);const t=deRatio(Rq(N(m[1]),N(m[1])+N(m[2]))).de;return t;},
 'f-ade-area'(q){const m=M(/^△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다\. AD:DB=(\d+):(\d+)일 때, △ADE의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오\.$/,q.prompt);
  const [A,B,C]=[P(0,0),P(7,1),P(2,5)];const D=lerp(A,B,Rq(N(m[1]),N(m[1])+N(m[2])));const E=lineHit(D,ps(C,B),A,C);return rd(area2([A,D,E]),area2([A,B,C]));},
 'f-trap-area'(q){const m=M(/^△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다\. AD:DB=(\d+):(\d+)일 때, □DBCE의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오\.$/,q.prompt);
  const [A,B,C]=[P(0,0),P(7,1),P(2,5)];const D=lerp(A,B,Rq(N(m[1]),N(m[1])+N(m[2])));const E=lineHit(D,ps(C,B),A,C);return rd(area2([D,B,C,E]),area2([A,B,C]));},
 'f-pl-ratio'(q){const m=M(/^서로 평행한 세 직선 l, m, n이 직선 p와 만나는 점을 각각 A, B, C, 직선 q와 만나는 점을 각각 D, E, F라고 하자\. AB=(\d+) cm, BC=(\d+) cm일 때, EF의 길이는 DE의 길이의 몇 배인지 기약분수로 구하시오\.$/,q.prompt);
  // put l, m, n at heights 0, AB, AB+BC along p (vertical); q is a slanted transversal
  const ab=N(m[1]),bc=N(m[2]);const D=P(1,0),dir=P(3,1);const at=h=>pa(D,pm(dir,I(h)));const E=at(ab),F=at(ab+bc);return rd(d2(E,F)[0]===0?I(0):rd(rs(F[1],E[1]),I(1)),rs(E[1],D[1]));},
 'f-centroid'(q){const p=q.prompt;const v=ps(TRI.C,TRI.B);const E=lineHit(CEN,v,TRI.A,TRI.B),F=lineHit(CEN,v,TRI.A,TRI.C);const T=area2([TRI.A,TRI.B,TRI.C]);
  if(/AD가 중선이다\. GD의 길이는 AD의 길이의/.test(p))return rs(I(1),medT('AD'));if(/BE가 중선이다\. BG의 길이는 BE의 길이의/.test(p))return medT('BE');
  if(/CF가 중선이다\. GF의 길이는 CG의 길이의/.test(p)){const t=medT('CF');return rd(rs(I(1),t),t);}
  if(/EF의 길이는 BC의 길이의/.test(p))return ratioVec(v,ps(F,E));if(/△AEF의 넓이는 △ABC의 넓이의/.test(p))return rd(area2([TRI.A,E,F]),T);
  if(/△GBD의 넓이는 △ABC의 넓이의/.test(p))return rd(area2([CEN,TRI.B,mid(TRI.B,TRI.C)]),T);if(/□EBCF의 넓이는 △ABC의 넓이의/.test(p))return rd(area2([E,TRI.B,TRI.C,F]),T);throw Error('unknown centroid fraction');},
};
function propHolds(txt){const m=M(/^([A-E]{2}):([A-E]{2})=([A-E]{2}):([A-E]{2})$/,txt);
 return [[P(0,0),P(7,1),P(2,5),Rq(2,5)],[P(1,1),P(9,2),P(3,8),Rq(3,7)]].every(([A,B,C,t])=>{const D=lerp(A,B,t),E=lineHit(D,ps(C,B),A,C);const pt={A,B,C,D,E};const L=s=>d2(pt[s[0]],pt[s[1]]);
  stats.coordinateChecks++;return req(rm(L(m[1]),L(m[4])),rm(L(m[2]),L(m[3])));});}
function ratioText(a,b){const g=gcd(a[0]*b[1],b[0]*a[1]);return `${a[0]*b[1]/g}:${b[0]*a[1]/g}`;}
const U4C={
 'prop-true'(q,opts){return opts.map(propHolds);},'prop-false'(q,opts){return opts.map(o=>!propHolds(o));},
 'cg-term'(q,opts){const p=q.prompt;if(p==='삼각형의 세 중선의 교점을 무엇이라고 하는지 고르시오.')return opts.map(o=>o==='무게중심');
  const m=M(/^점 G가 △ABC의 무게중심이고 (AD|BE|CF)가 중선이다\. ([A-G]{2}):([A-G]{2})를 고르시오\.$/,p);const [V]=MEDV[m[1]];const foot=m[1][1];const t=medT(m[1]);
  const len=s=>{const set=[...s].sort().join('');if(set===[V,'G'].sort().join(''))return t;if(set===['G',foot].sort().join(''))return rs(I(1),t);if(set===[V,foot].sort().join(''))return I(1);throw Error(`segment ${s}`);};
  const want=ratioText(len(m[2]),len(m[3]));return opts.map(o=>o===want);},
 'cg-bisect'(q,opts){const T=area2([TRI.A,TRI.B,TRI.C]);return opts.map(o=>{let m,p,v;
  if((m=/^직선 ([ABC])G$/.exec(o))){p=TRI[m[1]];v=ps(CEN,p);}else if((m=/^점 G를 지나고 변 ([ABC]{2})에 평행한 직선$/.exec(o))){p=CEN;v=ps(TRI[m[1][1]],TRI[m[1][0]]);}else throw Error(`line ${o}`);
  return req(rm(clipArea([TRI.A,TRI.B,TRI.C],p,v),I(2)),T);});},
 'de-parallel-judge'(q,opts){M(/AD, DB, AE, EC의 길이가 차례로 다음과 같을 때, DE∥BC인 것을 고르시오\./,q.prompt);return opts.map(o=>{const m=M(/^(\d+), (\d+), (\d+), (\d+)$/,o);const [A,B,C]=[P(0,0),P(7,1),P(2,5)];const D=lerp(A,B,Rq(N(m[1]),N(m[1])+N(m[2]))),E=lerp(A,C,Rq(N(m[3]),N(m[3])+N(m[4])));stats.coordinateChecks++;return req(cross(ps(E,D),ps(C,B)),I(0));});},
};

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
 'sq-hyp-intro'(q){M(/^∠C=90°인 직각삼각형 ABC의 세 변을 각각 한 변으로 하는 정사각형을 그렸다\. AB, BC 위의 정사각형의 넓이가 각각 5 cm², 3 cm²일 때, AC 위의 정사각형의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);return solve(x=>x+3===5,1,10);},
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
 'ctx-ladder'(q){const m=M(/^길이가 (\d+) m인 사다리를 벽에 기대어 세웠더니 사다리의 아래쪽 끝이 벽에서 (\d+) m 떨어져 있었다\. 사다리의 위쪽 끝은 바닥에서 몇 m 높이에 있는지 구하시오\. \(단, 벽은 바닥과 수직이다\.\)$/,q.prompt);return solve(x=>S2(x,N(m[2]))===N(m[1])**2,1,400);},
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

// ═════════════════════════ m2s2-u7 확률 (rewrite re-verification) ═════════════════════════
const EQB=' (단, 공의 모양과 크기는 모두 같다.)';
const ASKS={reduced:/확률을 기약분수로 구하시오\.$|상대도수를 기약분수로 구하시오\.$/,exact:/확률을 구하시오\. 모든 경우의 수와 그중 사건이 일어나는 경우의 수를 세어 약분하지 말고 나타내시오\.$|상대도수를 구하시오\. 약분하지 말고 전체 시행 횟수와 앞면이 나온 횟수로 나타내시오\.$/,equiv:/확률을 분수로 구하시오\. 약분하지 않아도 된다\.$/};
const range1=n=>Array.from({length:n},(_,i)=>i+1);
function diceEvt(text){let m;
 if((m=/^두 눈의 수의 합이 (\d+)일$/.exec(text)))return (x,y)=>x+y===N(m[1]);if((m=/^두 눈의 수의 합이 (\d+) 이하일$/.exec(text)))return (x,y)=>x+y<=N(m[1]);
 if((m=/^두 눈의 수의 합이 (\d+) 이상일$/.exec(text)))return (x,y)=>x+y>=N(m[1]);if((m=/^두 눈의 수의 곱이 (\d+)일$/.exec(text)))return (x,y)=>x*y===N(m[1]);
 if((m=/^큰 눈의 수에서 작은 눈의 수를 뺀 값이 (\d+)일$/.exec(text)))return (x,y)=>Math.max(x,y)-Math.min(x,y)===N(m[1]);if((m=/^두 눈의 수의 곱이 (\d+)의 배수일$/.exec(text)))return (x,y)=>(x*y)%N(m[1])===0;
 throw Error(`dice event ${text}`);}
// Parse a u7 fraction prompt into an explicit population, an event and the natural attribute
// partitions a student might use as an equally likely sample space.
function u7Space(q){const p=q.prompt;let m,pop,evt,parts=[];
 const strip=p.replace(EQB,'');const ask=Object.entries(ASKS).find(([,re])=>re.test(strip));if(!ask)throw Error('unknown answer-form sentence');
 const body=strip.replace(ASKS[ask[0]],'');
 if((m=/^빨간 공 (\d+)개와 파란 공 (\d+)개가 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, 빨간 공이 (나올|나오지 않을) $/.exec(body))){
  if(!p.endsWith(EQB.trim()))throw Error('equal-ball premise');const r=N(m[1]),b=N(m[2]);pop=[...Array(r).fill('R'),...Array(b).fill('B')].map((c,i)=>({c,i}));evt=o=>m[3]==='나올'?o.c==='R':o.c!=='R';parts=[o=>o.c];}
 else if((m=/^빨간 공 (\d+)개, 파란 공 (\d+)개, 흰 공 (\d+)개가 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, 빨간 공 또는 파란 공이 나올 $/.exec(body))){
  if(!p.endsWith(EQB.trim()))throw Error('equal-ball premise');pop=[...Array(N(m[1])).fill('R'),...Array(N(m[2])).fill('B'),...Array(N(m[3])).fill('W')].map((c,i)=>({c,i}));evt=o=>o.c!=='W';parts=[o=>o.c];}
 else if((m=/^빨간 공 (\d+)개와 파란 공 (\d+)개가 들어 있는 주머니에서 공 한 개를 임의로 꺼내 확인한 후 다시 넣고 또 한 개를 꺼낼 때, (두 번 모두|적어도 한 번) 빨간 공이 나올 $/.exec(body))){
  if(!p.endsWith(EQB.trim()))throw Error('equal-ball premise');const balls=[...Array(N(m[1])).fill('R'),...Array(N(m[2])).fill('B')];pop=balls.flatMap((c1,i)=>balls.map((c2,j)=>({c1,c2,i,j})));
  evt=m[3]==='두 번 모두'?o=>o.c1==='R'&&o.c2==='R':o=>o.c1==='R'||o.c2==='R';parts=[o=>o.c1+o.c2,o=>o.c1,o=>o.c2];q._multi=true;}
 else if((m=/^1부터 (\d+)까지의 자연수가 각각 하나씩 적힌 카드 (\d+)장 중 한 장을 임의로 뽑을 때, (\d+)의 배수(?: 또는 (\d+)의 배수)?일 $/.exec(body))){
  if(m[1]!==m[2])throw Error('card count');const t=N(m[1]),a=N(m[3]),b=m[4]?N(m[4]):null;pop=range1(t).map(v=>({v}));evt=o=>o.v%a===0||(b!==null&&o.v%b===0);
  for(let mod=2;mod<=t;mod++)parts.push(o=>o.v%mod);}
 else if((m=/^서로 다른 두 개의 주사위를 동시에 던질 때, (.+) $/.exec(body))){const f=diceEvt(m[1]);pop=range1(6).flatMap(x=>range1(6).map(y=>({x,y})));evt=o=>f(o.x,o.y);
  const per={id:v=>v,par:v=>v%2,m3:v=>v%3,hl:v=>v>3,all:()=>0};for(const [n1,f1] of Object.entries(per))for(const [n2,f2] of Object.entries(per))if(!(n1==='id'&&n2==='id'))parts.push(o=>`${f1(o.x)}|${f2(o.y)}`);}
 else if((m=/^동전을 (\d+)번 던졌더니 앞면이 (\d+)번 나왔다\. 이 실험에서 앞면의 $/.exec(body))){pop=range1(N(m[1])).map(i=>({i}));evt=o=>o.i<=N(m[2]);parts=null;q._experiment=true;}
 else throw Error('unrecognised u7 prompt');
 stats.enumeratedOutcomes+=pop.length;const f=pop.filter(evt).length;
 // event-vs-complement is itself an equally likely 2-point space when both halves match
 let alt=null;if(parts!==null){if(2*f===pop.length)alt='event-vs-complement';
  for(const key of parts){if(alt)break;const blocks=new Map();for(const o of pop){const k=key(o);if(!blocks.has(k))blocks.set(k,[]);blocks.get(k).push(o);}
   const sizes=[...blocks.values()].map(b=>b.length);if(blocks.size<2||blocks.size>=pop.length||new Set(sizes).size!==1)continue;
   if([...blocks.values()].every(b=>b.every(evt)||!b.some(evt)))alt=`equal blocks ×${blocks.size}`;}}
 return {f,n:pop.length,ask:ask[0],alt};}
const U7={
 'coin-intro'(q){M(/^동전 한 개를 던질 때, 나올 수 있는 모든 경우의 수를 구하시오\.$/,q.prompt);stats.enumeratedOutcomes+=2;return ['앞면','뒷면'].length;},
 reverse(q){const m=M(/^빨간 공과 파란 공이 합하여 (\d+)개 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때 빨간 공이 나올 확률이 \{frac:(\d+)\/(\d+)\}이면, 빨간 공은 몇 개인지 구하시오\. \(단, 공의 모양과 크기는 모두 같다\.\)$/,q.prompt);
  const t=N(m[1]);return solve(r=>r*N(m[3])===N(m[2])*t,0,t);},
};
const U7F=new Proxy({},{get:()=>(q)=>{const s=u7Space(q);q._space=s;return [s.f,s.n];}});
function u7Policy(q,p,[n,d]){const s=q._space,a=q.answer;const want=s.ask==='reduced'?'reduced':s.ask==='exact'?'exact_parts':'equivalent';
 if(q.accept!==want)throw Error(`accept ${q.accept} but prompt asks ${want}`);
 if(want==='exact_parts'&&s.alt)throw Error(`exact_parts although another equally likely sample space exists (${s.alt})`);
 if(q._multi&&want!=='equivalent')throw Error('with-replacement items must accept any equivalent fraction');
 if(want==='exact_parts'&&!(a.num===n&&a.den===d))throw Error(`exact parts ${a.num}/${a.den} ≠ counted ${n}/${d}`);
 if(want!=='exact_parts'&&a.num*d!==n*a.den)throw Error(`value ${a.num}/${a.den} ≠ ${n}/${d}`);
 if(want==='reduced'&&gcd(a.num,a.den)!==1)throw Error('reduced answer not in lowest terms');
 const labels=want==='exact_parts'?(q._experiment?['앞면이 나온 횟수','전체 시행 횟수']:['사건이 일어나는 경우의 수','모든 경우의 수']):['분자','분모'];
 if(q.num_label!==labels[0]||q.den_label!==labels[1])throw Error(`pad labels ${q.num_label}/${q.den_label}`);
 if(q._multi&&!/×/.test(q.explain))throw Error('replacement explain must show the product count');
 stats.u7Policy=(stats.u7Policy??0)+1;}
const U7C={'single-color'(q,opts){const m=M(/^(\S+) 공만 (\d+)개 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, (\S+) 공이 나올 확률을 구하시오\. \(단, 공의 모양과 크기는 모두 같다\.\)$/,q.prompt);
  const n=N(m[2]);stats.enumeratedOutcomes+=n;const f=Array(n).fill(m[1]).filter(c=>c===m[3]).length;return opts.map(o=>{const t=/^\{frac:(\d+)\/(\d+)\}$/.exec(o);const [x,y]=t?[N(t[1]),N(t[2])]:[N(o),1];return x*n===f*y;});}};

// @@UNIT-SOLVERS@@

// ═════════════════════════ runner ═════════════════════════
const UNITS={'m2s2-u1':{A:U1,C:U1C},'m2s2-u2':{A:U2,C:U2C},'m2s2-u3':{A:U3,F:U3F,C:U3C},'m2s2-u4':{A:U4,F:U4F,C:U4C},'m2s2-u5':{A:U5,C:U5C},'m2s2-u7':{A:U7,F:U7F,C:U7C,fractionPolicy:u7Policy}};
const FORBIDDEN=/√|근호|가정|결론|다시 넣지 않/;
const UNIT_WORD=['몇 도','몇 cm²','몇 cm³','몇 cm','몇 m'];
// Checks one item; throws on the first defect. Returns the choice slot (or -1).
function checkItem(packId,S,q,p){
 if(!p)throw Error('no proof entry');
 const text=[q.prompt,q.explain,...(q.choices??[])].join(' ');if(FORBIDDEN.test(text))throw Error('forbidden term (√·가정·결론·비복원)');
 if(/\d+\s*\/\s*\d+/.test(text.replace(/\{frac:\d+\/\d+\}/g,'')))throw Error('plain fraction');
 if(q.answer_mode==='amount'){
  const fn=S.A[p.kind];if(!fn)throw Error(`no amount oracle for ${p.kind}`);const exp=fn(q,p);
  if(q.answer!==exp)throw Error(`answer ${q.answer} ≠ oracle ${exp}`);
  if(!(Number.isSafeInteger(q.answer)&&q.answer>=2&&q.answer<=59))throw Error('amount must be 2..59');
  if(q.max!==60||q.coin_budget!==60||q.answerNumeric!==q.answer||q.format!=='int')throw Error('amount pad contract');
  if(packId!=='m2s2-u7'&&!UNIT_WORD.some(w=>q.prompt.includes(w))&&!/값을 구하시오/.test(q.prompt))throw Error('answer unit not stated');
  if(!new RegExp(`(^|[^0-9])${q.answer}([^0-9]|$)`).test(q.explain))throw Error('explain does not state the answer');
  for(const w of p.wrongs??[])if(w.value===q.answer)throw Error('distractor equals answer');
  return {slot:-1,cost:q.answer};
 }
 if(q.answer_mode==='fraction_parts'){
  const fn=S.F?.[p.kind];if(!fn)throw Error(`no fraction oracle for ${p.kind}`);const [n,d]=fn(q,p);const a=q.answer;
  if(S.fractionPolicy)S.fractionPolicy(q,p,[n,d]);
  else {if(q.accept!=='reduced'||!q.prompt.includes('기약분수'))throw Error('geometry fractions are reduced + 기약분수');
   if(a.num*d!==n*a.den||gcd(a.num,a.den)!==1)throw Error(`fraction ${a.num}/${a.den} ≠ oracle ${n}/${d}`);
   if(a.den<2)throw Error('integer disguised as fraction');
   if(q.num_label!=='분자'||q.den_label!=='분모')throw Error('pad labels');}
  if(a.num>60||a.den>60||q.max!==60||q.coin_budget!==120||q.format!=='frac')throw Error('fraction pad contract');
  if(q.answerNumeric!==a.num/a.den)throw Error('answerNumeric');
  // full acceptance table over the pad domain 0..60 × 0..60 (denominator 0 always rejected)
  for(let x=0;x<=60;x++)for(let y=0;y<=60;y++){stats.acceptancePairs++;const accepted=y>0&&(q.accept==='exact_parts'?x===a.num&&y===a.den:x*a.den===a.num*y&&(q.accept!=='reduced'||gcd(x,y)===1));
   const truth=y>0&&(q.accept==='exact_parts'?x===n&&y===d:x*d===n*y&&(q.accept!=='reduced'||gcd(x,y)===1));if(accepted!==truth)throw Error(`acceptance mismatch at ${x}/${y}`);}
  const g=gcd(n,d);if(!q.explain.includes(`{frac:${a.num}/${a.den}}`)&&!q.explain.includes(`{frac:${n/g}/${d/g}}`))throw Error('explain does not state the fraction');
  return {slot:-1,cost:a.num+a.den};
 }
 const fn=S.C[p.kind];if(!fn)throw Error(`no choice oracle for ${p.kind}`);
 let opts=q.choices;const labelled=q.choices.join('')==='ㄱㄴㄷㄹ';
 if(labelled){const m=/ ㄱ\. (.+?)  ㄴ\. (.+?)  ㄷ\. (.+?)  ㄹ\. (.+?)(?: \(단[^)]*\))?$/.exec(q.prompt);if(!m)throw Error('labelled options not parseable');opts=[m[1],m[2],m[3],m[4]];}
 const truth=fn(q,opts,p);stats.choiceOptions+=4;
 if(truth.filter(Boolean).length!==1)throw Error(`exactly one option must be correct, oracle says ${truth}`);
 const idx=truth.indexOf(true);if(q.choices[idx]!==q.answer)throw Error(`answer ${q.answer} but oracle picks ${q.choices[idx]}`);
 if(new Set(q.choices).size!==4||new Set(opts).size!==4)throw Error('duplicate options');
 if(new Set(q.distractor_tags).size<2)throw Error('need 2+ misconception tags');
 return {slot:idx,cost:0};
}
function loadPack(packId){const file=path.join(out,`${packId}.json`);if(!fs.existsSync(file))return null;
 return {pack:JSON.parse(fs.readFileSync(file,'utf8')),proof:new Map(JSON.parse(fs.readFileSync(path.join(here,`${packId}-proofs.json`),'utf8')).proofs.map(p=>[p.id,p]))};}
const reports=[];
for(const [packId,S] of Object.entries(UNITS)){
 const loaded=loadPack(packId);if(!loaded){fail(packId,'pack file missing');continue;}const {pack,proof}=loaded;
 const structural=validatePack(pack);for(const e of structural.errors)fail(packId,`validate-pack: ${e}`);
 const unit=curriculum.units.find(u=>u.id===packId);if(!unit)fail(packId,'unit missing in curriculum');
 if(unit&&JSON.stringify([...pack.standards].sort())!==JSON.stringify([...unit.standards].sort()))fail(packId,`standards ${pack.standards} ≠ unit ${unit.standards}`);
 if(!pack.standards.every(c=>curriculum.standards.some(s=>s.code===c)))fail(packId,'unknown standard code');
 if(pack.school!=='middle'||pack.grade!==2||pack.semester!==2||pack.unit_id!==packId)fail(packId,'school/grade/semester/unit_id');
 if(pack.items[0].answer_mode!=='amount'||pack.items[0].answer!==2)fail(packId,'items[0] must be the amount-2 guide');
 const bands={1:0,2:0,3:0,4:0},modes={amount:0,fraction_parts:0,choice:0},slots={},kinds={},accepts={},concepts={};let maxCost=0;const prompts=new Set();
 for(const q of pack.items){stats.items++;try{
  const p=proof.get(q.id);bands[q.difficulty]++;modes[q.answer_mode]++;if(p)kinds[p.kind]=(kinds[p.kind]??0)+1;if(q.accept)accepts[q.accept]=(accepts[q.accept]??0)+1;concepts[q.unitConcept]=(concepts[q.unitConcept]??0)+1;
  const key=q.prompt.replace(/\([^)]*\)/g,'').replace(/\s+/g,' ').trim();if(prompts.has(key))throw Error('duplicate prompt');prompts.add(key);
  if(!q.distractor_tags.every(t=>typeof t==='string'&&t.startsWith(packId==='m2s2-u7'?'m2s2-u':packId+'.')))throw Error('distractor tag namespace');
  const r=checkItem(packId,S,q,p);maxCost=Math.max(maxCost,r.cost);if(r.slot>=0)(slots[q.difficulty]??=[0,0,0,0])[r.slot]++;
 }catch(e){fail(q.id,e.message);}}
 for(const [b,s] of Object.entries(slots))if(Math.max(...s)-Math.min(...s)>1)fail(packId,`answer slot imbalance in difficulty ${b}: ${s}`);
 // geometry packs are built with even bands; u7 keeps its original distribution (its d4 = 38 with-replacement items)
 if(Object.values(bands).some(n=>n<(packId==='m2s2-u7'?30:70)))fail(packId,`difficulty band too thin ${JSON.stringify(bands)}`);
 if(modes.choice>pack.items.length*.3)fail(packId,'choice over 30%');
 if(pack.items.length<300)fail(packId,'fewer than 300 items');
 reports.push({pack_id:packId,items:pack.items.length,modes,accepts,bands,maxAnswerCost:maxCost,kinds:Object.keys(kinds).length,concepts,structural_errors:structural.structural_errors});
}
// ── mutation self-test: every corrupted item must be rejected by the oracle ──
const selftest={answer:[0,0],choice:[0,0],promptNumber:[0,0],accept:[0,0]};
if(process.argv.includes('--selftest')){const clone=o=>JSON.parse(JSON.stringify(o));
 const undetected=[];
 // answer/choice/accept corruptions must always be caught. A bumped prompt number can legitimately
 // leave the true answer unchanged (e.g. MQ=BC÷2 does not use AD); those are listed for manual review.
 const expectFail=(bucket,packId,S,q,p)=>{selftest[bucket][1]++;try{checkItem(packId,S,q,p);}catch{selftest[bucket][0]++;return;}if(bucket==='promptNumber')undetected.push(`${q.id}: ${q.prompt}`);else failures.push(`selftest ${bucket} not detected: ${q.id}`);};
 selftest.promptNumberUnchangedAnswer=undetected;
 for(const [packId,S] of Object.entries(UNITS)){const {pack,proof}=loadPack(packId);
  pack.items.forEach((orig,i)=>{const p=proof.get(orig.id);
   let q=clone(orig);if(q.answer_mode==='amount'){q.answer+=1;q.answerNumeric=q.answer;expectFail('answer',packId,S,q,p);}
   else if(q.answer_mode==='fraction_parts'){q.answer.num+=1;q.answerNumeric=q.answer.num/q.answer.den;expectFail('answer',packId,S,q,p);
    q=clone(orig);const alt={exact_parts:'equivalent',equivalent:'exact_parts',reduced:'equivalent'}[q.accept];q.accept=alt;expectFail('accept',packId,S,q,p);}
   else {q.answer=q.choices[(q.choices.indexOf(q.answer)+1)%4];expectFail('choice',packId,S,q,p);}
   // bump the first number in the prompt that is not part of a {frac}; answers must no longer match
   q=clone(orig);const mm=/(^|[^0-9/:])(\d+)(?=[^0-9/:]|$)/.exec(q.prompt.replace(/\{frac:\d+\/\d+\}/g,f=>'#'.repeat(f.length)));
   if(mm&&q.answer_mode!=='choice'){const at=mm.index+mm[1].length;q.prompt=q.prompt.slice(0,at)+String(Number(mm[2])+1)+q.prompt.slice(at+mm[2].length);expectFail('promptNumber',packId,S,q,p);}
  });}}
if(typeof extraChecks==='function')extraChecks(reports);
const result={verdict:failures.length?'fail':'pass',...stats,selftest:process.argv.includes('--selftest')?Object.fromEntries(Object.entries(selftest).map(([k,v])=>[k,Array.isArray(v)&&typeof v[0]==='number'?`${v[0]}/${v[1]} detected`:v])):'not run (pass --selftest)',failures:failures.length,packs:reports,errors:failures.slice(0,200)};
fs.writeFileSync(path.join(here,'check-m2s2-geo-report.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify({verdict:result.verdict,items:stats.items,failures:failures.length,selftest:result.selftest,packs:reports.map(r=>({pack_id:r.pack_id,items:r.items,modes:r.modes,accepts:r.accepts,bands:r.bands,maxAnswerCost:r.maxAnswerCost}))},null,1));
if(failures.length){console.error(failures.slice(0,40).join('\n'));process.exitCode=1;}
