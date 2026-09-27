// m2s2-u1 oracle tables for check-m2s2-geo.mjs. Imports NO generator module.
import {here,root,out,curriculum,failures,stats,fail,gcd,M,N,solve,isqrt} from './check-m2s2-geo-lib.mjs';
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
 'iso-bis-angle'(q){const b=N(M(/^AB=AC인 이등변삼각형 ABC에서 ∠A의 이등분선이 변 BC와 만나는 점을 D라고 하자\. ∠B=(\d+)°일 때, ∠BAD의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);
  const A=solve(a=>a+2*b===180,1,179);return solve(x=>2*x===A,1,179);},
 'iso-bis-half'(q){const m=M(/^AB=AC인 이등변삼각형 ABC에서 ∠A의 이등분선이 변 BC와 만나는 점을 D라고 하자\. AB=(\d+) cm, BC=(\d+) cm일 때, (BD|CD)의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ab=N(m[1]),bc=N(m[2]);if(2*ab<=bc)throw Error('no triangle');
  // △ABD≡△ACD (SAS): BD=CD and BD+CD=BC.
  return solve(x=>x+x===bc,1,400);},
 'iso-bis-double'(q){const m=M(/^AB=AC인 이등변삼각형 ABC에서 ∠A의 이등분선이 변 BC와 만나는 점을 D라고 하자\. AB=(\d+) cm, (BD|CD)=(\d+) cm일 때, BC의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ab=N(m[1]),d=N(m[3]);if(ab<=d)throw Error('equal side must exceed half base');return solve(x=>x===d+d,1,800);},
 'iso-bis-perimeter'(q){const m=M(/^AB=AC인 이등변삼각형 ABC에서 ∠A의 이등분선이 변 BC와 만나는 점을 D라고 하자\. AB=(\d+) cm, (BD|CD)=(\d+) cm일 때, △ABC의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const ab=N(m[1]),d=N(m[3]);if(ab<=d)throw Error('no triangle');const bc=solve(x=>x===2*d,1,800);return solve(p=>p===ab+ab+bc,1,2000);},
 'iso-bis-angle-rev'(q){const x=N(M(/^AB=AC인 이등변삼각형 ABC에서 ∠A의 이등분선이 변 BC와 만나는 점을 D라고 하자\. ∠BAD=(\d+)°일 때, ∠C의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);
  // △ADC: ∠CAD=∠BAD (bisector), ∠ADC=90° (bisector ⊥ base).
  return solve(c=>x+90+c===180,1,179);},
 'iso-converse'(q){const m=M(/^△ABC에서 ∠([ABC])=(\d+)°, ∠([ABC])=(\d+)°이고 (AB|BC|AC)=(\d+) cm, (AB|BC|AC)=\(2x([+−])(\d+)\) cm일 때, x의 값을 구하시오\.$/,q.prompt);
  const ang={[m[1]]:N(m[2]),[m[3]]:N(m[4])};const third=['A','B','C'].find(v=>!(v in ang));ang[third]=solve(x=>x+ang[m[1]]+ang[m[3]]===180,1,179);
  const g=oppVertex(m[5]),t=oppVertex(m[7]);if(g===t)throw Error('same side twice');if(ang[g]!==ang[t])throw Error('opposite angles differ: sides not equal');
  if(new Set(Object.values(ang)).size!==2)throw Error('must be isosceles, not equilateral');
  const k=N(m[6]),c=(m[8]==='−'?-1:1)*N(m[9]);return solve(x=>2*x+c===k,1,400);},
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
 'circ-right-len'(q){const m=M(/^∠([ABC])=90°인 직각삼각형 ABC의 외심을 O라고 하자\. ([ABC]{2})=(\d+) cm, ([ABC]{2})=(\d+) cm일 때, O([ABC])의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  if(m[2].includes(m[1]))throw Error('first side must be the hypotenuse');if(!m[4].includes(m[1]))throw Error('second side must be a leg');if(m[6]!==m[1])throw Error('asked distance');
  const c=N(m[3]),a=N(m[5]);const b=isqrt(c*c-a*a);if(!(b>0))throw Error('legs not integral / not a right triangle');
  // coordinates: R=(0,0), legs on the axes; the circumcenter is equidistant from all three vertices.
  const R=[0,0],P=[2*a,0],Q=[0,2*b];const Ox=a,Oy=b; // scaled by 2 to stay integral
  const d2=(u,v)=>(u[0]-v[0])**2+(u[1]-v[1])**2;if(d2([Ox,Oy],R)!==d2([Ox,Oy],P)||d2([Ox,Oy],R)!==d2([Ox,Oy],Q))throw Error('circumcenter');
  return solve(x=>4*x*x===d2([Ox,Oy],R)*1,1,400);},
 'circ-right-angle'(q){const m=M(/^∠([ABC])=90°인 직각삼각형 ABC의 외심을 O라고 하자\. ∠([ABC])=(\d+)°일 때, ∠O([ABC])([ABC])의 크기는 몇 도인지 구하시오\.$/,q.prompt);
  const R=m[1],P=m[2],x=N(m[3]);if(P===R||m[4]!==R||m[5]===R||m[5]===P)throw Error('angle roles');if(x>=90)throw Error('acute');
  // O = midpoint of hypotenuse PQ: OR=OQ, so ∠ORQ=∠Q, and ∠Q = 90°−∠P by the angle sum.
  const Qa=solve(t=>t+x+90===180,1,89);return Qa;},
 'circ-right-exterior'(q){const m=M(/^∠([ABC])=90°인 직각삼각형 ABC의 외심을 O라고 하자\. ∠([ABC])=(\d+)°일 때, ∠([ABC])O([ABC])의 크기는 몇 도인지 구하시오\.$/,q.prompt);
  const R=m[1],P=m[2],x=N(m[3]);if(m[4]!==R||m[5]===R||m[5]===P||P===R)throw Error('angle roles');
  // △OPR is isosceles (OP=OR): ∠POR = 180−2x, and ∠ROQ is its supplement on the line PQ.
  const por=solve(t=>t+2*x===180,1,179);return solve(t=>t+por===180,1,179);},
 'circ-right-apex'(q){const m=M(/^∠([ABC])=90°인 직각삼각형 ABC의 외심을 O라고 하자\. ∠([ABC])=(\d+)°일 때, ∠([ABC])O([ABC])의 크기는 몇 도인지 구하시오\.$/,q.prompt);
  const R=m[1],P=m[2],x=N(m[3]);if(m[4]!==P||m[5]!==R||P===R)throw Error('angle roles');
  // ∠OQR = ∠ORQ = 90−x (OQ=OR), the exterior angle ∠POR = 2(90−x).
  const q2=solve(t=>t+x+90===180,1,89);return q2+q2;},
 'circ-obc-perimeter'(q){const m=M(/^점 O가 △ABC의 외심이고 OA=(\d+) cm, BC=(\d+) cm일 때, △OBC의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const r=N(m[1]),bc=N(m[2]);if(bc>=2*r)throw Error('chord longer than diameter');return solve(p=>p===r+r+bc,1,2000);},
 'in-bic'(q){const A=N(M(/^점 I가 △ABC의 내심이고 ∠A=(\d+)°일 때, ∠BIC의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);
  // ∠IBC+∠ICB = (∠B+∠C)/2 = (180−A)/2; ∠BIC closes △IBC.
  const half=solve(s=>2*s===180-A,1,179);return solve(t=>t+half===180,1,179);},
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
export default {A:U1,C:U1C};
