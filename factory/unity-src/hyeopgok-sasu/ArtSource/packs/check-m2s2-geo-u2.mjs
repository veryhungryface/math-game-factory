// m2s2-u2 oracle tables for check-m2s2-geo.mjs. Imports NO generator module.
import {here,root,out,curriculum,failures,stats,fail,gcd,M,N,solve,isqrt} from './check-m2s2-geo-lib.mjs';
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
 'pg-angle-other-pair'(q){const m=M(/^평행사변형 ABCD에서 ∠([A-D])=(\d+)°일 때, ∠([A-D])\+∠([A-D])의 크기는 몇 도인지 구하시오\.$/,q.prompt);
  if(!adjacent(m[1],m[3])||!adjacent(m[1],m[4])||m[3]===m[4])throw Error('asked pair must be the two neighbours of the given angle');const r=pgAngles(m[1],N(m[2]));return r[m[3]]+r[m[4]];},
 'pg-opposite-sum'(q){const m=M(/^평행사변형 ABCD에서 ∠([A-D])\+∠([A-D])=(\d+)°일 때, ∠([A-D])의 크기는 몇 도인지 구하시오\.$/,q.prompt);
  if(qOpp(m[1])!==m[2])throw Error('given pair must be opposite');const x=solve(t=>t+t===N(m[3]),1,179);return pgAngles(m[1],x)[m[4]];},
 'rect-oab-perimeter'(q){const m=M(/^직사각형 ABCD의 두 대각선의 교점을 O라고 하자\. AC=(\d+) cm, AB=(\d+) cm일 때, △OAB의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const L=N(m[1]),ab=N(m[2]);if(ab>=L)throw Error('side longer than diagonal');const oa=halfOf('OA',{A:L}),ob=halfOf('OB',{B:L});/* AC=BD in a rectangle */return oa+ob+ab;},
 'sq-pcd'(q){const x=N(M(/^정사각형 ABCD의 대각선 BD 위에 점 P가 있다\. ∠BAP=(\d+)°일 때, ∠PCD의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);if(x>=90)throw Error('P outside BD');
  // reflection in BD swaps A↔C and fixes P: ∠BCP=∠BAP; ∠BCD=90°
  return solve(t=>t+x===90,1,89);},
 'isotrap-b-to-d'(q){const x=N(M(/^AD∥BC인 등변사다리꼴 ABCD에서 ∠B=(\d+)°일 때, ∠D의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);
  // ∠B=∠C (base angles), ∠C+∠D=180 (co-interior, AD∥BC), angle sum 360 as a cross-check
  const C=x,D=solve(t=>t+C===180,1,179),A=solve(t=>t+x===180,1,179);if(A+x+C+D!==360)throw Error('angle sum');return D;},
 'isotrap-ob'(q){const m=M(/^AD∥BC인 등변사다리꼴 ABCD의 두 대각선의 교점을 O라고 하자\. AC=(\d+) cm, OA=(\d+) cm일 때, OB의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const bd=N(m[1]),od=N(m[2]);/* BD=AC; △ABC≡△DCB gives ∠OBC=∠OCB, so OB=OC and OA=OD */if(2*od>=bd)throw Error('O must be nearer AD');return solve(x=>x+od===bd,1,400);},
 'pg-two-quarters'(q){const m=M(/^넓이가 (\d+) cm²인 평행사변형 ABCD의 두 대각선의 교점을 O라고 할 때, △(ABO|BCO|CDO|DAO)\+△(ABO|BCO|CDO|DAO)의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  if(m[2]===m[3])throw Error('same triangle');const k=solve(t=>4*t===N(m[1]),1,2000);return k+k;},
 'pg-inner-rest'(q){const m=M(/^넓이가 (\d+) cm²인 평행사변형 ABCD의 내부의 한 점 P에 대하여 △(P[A-D]{2})의 넓이가 (\d+) cm²일 때, △(P[A-D]{2})의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  const s1=m[2].slice(1),s2=m[4].slice(1);if([...s1,...s2].sort().join('')!=='ABCD'||!['AB','BC','CD','AD'].includes([...s1].sort().join('')))throw Error('need opposite sides');
  const S=N(m[1]),a=N(m[3]);const x=solve(x=>2*(a+x)===S,1,2000);if(x<=0)throw Error('area');return x;},
 'trap-dbc-sum'(q){const m=M(/^AD∥BC인 사다리꼴 ABCD의 두 대각선의 교점을 O라고 하자\. △ABO의 넓이가 (\d+) cm², △OBC의 넓이가 (\d+) cm²일 때, △DBC의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  const s=N(m[1]),t=N(m[2]);/* △ABC=△DBC (same base BC, equal heights) ⇒ △DCO=△ABO */const abc=s+t;return solve(x=>x===abc,1,2000);},
 'trap-dco-from'(q){const m=M(/^AD∥BC인 사다리꼴 ABCD의 두 대각선의 교점을 O라고 하자\. △ABC의 넓이가 (\d+) cm², △OBC의 넓이가 (\d+) cm²일 때, △DCO의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);
  const S=N(m[1]),t=N(m[2]);if(!(t>0&&t<S))throw Error('area order');const dbc=S;return solve(x=>x+t===dbc,1,2000);},
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

export default {A:U2,C:U2C};
