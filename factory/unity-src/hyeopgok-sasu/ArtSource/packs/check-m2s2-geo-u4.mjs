// m2s2-u4 oracle tables for check-m2s2-geo.mjs. Imports NO generator module.
import {here,root,out,curriculum,failures,stats,fail,gcd,M,N,solve,isqrt} from './check-m2s2-geo-lib.mjs';
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
const triOK=(a,b,c)=>{const t=[a,b,c].sort((x,y)=>x-y);if(t[0]+t[1]<=t[2])throw Error('not a triangle');};
const MNOVERBC=()=>ratioVec(ps(TRI.C,TRI.B),ps(mid(TRI.A,TRI.C),mid(TRI.A,TRI.B)));
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
 'mid-mn'(q){const m=M(/^△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자\. AB=(\d+) cm, BC=(\d+) cm, CA=(\d+) cm일 때, MN의 길이는 몇 cm인지 구하시오\.$/,q.prompt);triOK(N(m[1]),N(m[2]),N(m[3]));return solveR(x=>req(rm(MNOVERBC(),I(N(m[2]))),x));},
 'mid-bc'(q){const m=M(/^△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자\. AM=(\d+) cm, AN=(\d+) cm, MN=(\d+) cm일 때, BC의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  triOK(N(m[1]),N(m[2]),N(m[3]));const bc=solveR(x=>req(rm(MNOVERBC(),x),I(N(m[3]))));triOK(2*N(m[1]),2*N(m[2]),bc);return bc;},
 'mid-converse-nc'(q){const m=M(/^△ABC에서 변 AB의 중점 M을 지나고 변 BC에 평행한 직선이 변 AC와 만나는 점을 N이라고 하자\. AB=(\d+) cm, BC=(\d+) cm, CA=(\d+) cm일 때, NC의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  triOK(N(m[1]),N(m[2]),N(m[3]));const Np=lineHit(mid(TRI.A,TRI.B),ps(TRI.C,TRI.B),TRI.A,TRI.C);const s=along(TRI.C,TRI.A,Np);return solveR(x=>req(rm(s,I(N(m[3]))),x));},
 'mid-converse-mn'(q){const m=M(/^△ABC에서 변 AB의 중점 M을 지나고 변 BC에 평행한 직선이 변 AC와 만나는 점을 N이라고 하자\. AB=(\d+) cm, BC=(\d+) cm, CA=(\d+) cm일 때, MN의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  triOK(N(m[1]),N(m[2]),N(m[3]));const Mp=mid(TRI.A,TRI.B),Np=lineHit(Mp,ps(TRI.C,TRI.B),TRI.A,TRI.C);const t=ratioVec(ps(TRI.C,TRI.B),ps(Np,Mp));return solveR(x=>req(rm(t,I(N(m[2]))),x));},
 'mid-def-perimeter'(q){const m=M(/^△ABC의 세 변 AB, BC, CA의 중점을 각각 D, E, F라고 하자\. AB=(\d+) cm, BC=(\d+) cm, CA=(\d+) cm일 때, △DEF의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const s=[1,2,3].map(i=>N(m[i])).sort((a,b)=>a-b);if(s[0]+s[1]<=s[2])throw Error('not a triangle');return solve(x=>2*x===s[0]+s[1]+s[2],1,400);},
 'mid-amn-perimeter'(q){const m=M(/^△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자\. AB=(\d+) cm, BC=(\d+) cm, CA=(\d+) cm일 때, △AMN의 둘레의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const s=[1,2,3].map(i=>N(m[i]));const t=[...s].sort((a,b)=>a-b);if(t[0]+t[1]<=t[2])throw Error('not a triangle');return solve(x=>2*x===s[0]+s[1]+s[2],1,400);},
 // MN∥BC: ∠BMN and ∠B (∠CNM and ∠C) are interior angles on the same side of transversal AB (AC).
 'mid-angle-bmn'(q){const x=N(M(/^△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자\. ∠B=(\d+)°일 때, ∠BMN의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);const amn=x;/* corresponding */return solve(t=>t+amn===180,1,179);},
 'mid-angle-cnm'(q){const x=N(M(/^△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자\. ∠C=(\d+)°일 때, ∠CNM의 크기는 몇 도인지 구하시오\.$/,q.prompt)[1]);const anm=x;return solve(t=>t+anm===180,1,179);},
 'mid-angle-sum-m'(q){const m=M(/^△ABC에서 두 변 AB, AC의 중점을 각각 M, N이라고 하자\. ∠A=(\d+)°, ∠C=(\d+)°일 때, ∠AMN의 크기는 몇 도인지 구하시오\.$/,q.prompt);const B=solve(b=>b+N(m[1])+N(m[2])===180,1,179);return B;},
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
 'cg-median-from-vertex'(q){const m=M(/^점 G가 △ABC의 무게중심이고 (AD|BE|CF)가 중선이다\. ([ABC])G=(\d+) cm일 때, \1의 길이는 몇 cm인지 구하시오\.$/,q.prompt);if(m[2]!==m[1][0])throw Error('vertex');const t=medT(m[1]);return solveR(x=>req(rm(x,t),I(N(m[3]))));},
 'cg-midpoint'(q){const m=M(/^점 G가 △ABC의 무게중심이고 (AD|BE|CF)가 중선이다\. \1=(\d+) cm, ([ABC]{2})=(\d+) cm일 때, ([ABC])([DEF])의 길이는 몇 cm인지 구하시오\.$/,q.prompt);
  const [V,X,Y]=MEDV[m[1]];if([X,Y].sort().join('')!==m[3]||m[6]!==m[1][1]||!m[3].includes(m[5]))throw Error('segment');return solveR(x=>req(rm(along(TRI[m[5]],TRI[m[5]===X?Y:X],mid(TRI[X],TRI[Y])),I(N(m[4]))),x));},
 'cg-two-medians'(q){const m=M(/^△ABC의 두 중선 AD, BE의 교점을 G라고 하자\. AD=(\d+) cm, BE=(\d+) cm일 때, AG\+BG의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return solveR(x=>req(ra(rm(medT('AD'),I(N(m[1]))),rm(medT('BE'),I(N(m[2])))),x));},
 'cg-two-medians-gd'(q){const m=M(/^△ABC의 두 중선 AD, BE의 교점을 G라고 하자\. AD=(\d+) cm, BE=(\d+) cm일 때, GD\+GE의 길이는 몇 cm인지 구하시오\.$/,q.prompt);return solveR(x=>req(ra(rm(rs(I(1),medT('AD')),I(N(m[1]))),rm(rs(I(1),medT('BE')),I(N(m[2])))),x));},
 'cg-parallel-ef'(q){const bc=N(M(/^점 G가 △ABC의 무게중심이고, 점 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 하자\. BC=(\d+) cm일 때, EF의 길이는 몇 cm인지 구하시오\.$/,q.prompt)[1]);
  const v=ps(TRI.C,TRI.B);const E=lineHit(CEN,v,TRI.A,TRI.B),F=lineHit(CEN,v,TRI.A,TRI.C);return solveR(x=>req(rm(ratioVec(v,ps(F,E)),I(bc)),x));},
 'cg-parallel-ae'(q){const ab=N(M(/^점 G가 △ABC의 무게중심이고, 점 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 하자\. AB=(\d+) cm, BC=\d+ cm일 때, AE의 길이는 몇 cm인지 구하시오\.$/,q.prompt)[1]);
  const E=lineHit(CEN,ps(TRI.C,TRI.B),TRI.A,TRI.B);return solveR(x=>req(rm(along(TRI.A,TRI.B,E),I(ab)),x));},
 'cg-area-third'(q){const m=M(/^넓이가 (\d+) cm²인 △ABC의 무게중심을 G라고 할 때, △(G[ABC]{2})의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);const r=rd(area2([CEN,TRI[m[2][1]],TRI[m[2][2]]]),area2([TRI.A,TRI.B,TRI.C]));return solveR(x=>req(rm(r,I(N(m[1]))),x));},
 'cg-area-sixth'(q){const S=N(M(/^넓이가 (\d+) cm²인 △ABC의 무게중심을 G, 변 BC의 중점을 D라고 할 때, △GBD의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt)[1]);const r=rd(area2([CEN,TRI.B,mid(TRI.B,TRI.C)]),area2([TRI.A,TRI.B,TRI.C]));return solveR(x=>req(rm(r,I(S)),x));},
 'cg-area-whole'(q){const m=M(/^△ABC의 무게중심을 G라고 하자\. △G([ABC])([ABC])의 넓이가 (\d+) cm²일 때, △ABC의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt);if(m[1]===m[2])throw Error('triangle');const s=N(m[3]);const r=rd(area2([TRI.A,TRI.B,TRI.C]),area2([CEN,TRI[m[1]],TRI[m[2]]]));return solveR(x=>req(rm(r,I(s)),x));},
 'cg-parallel-area'(q){const S=N(M(/^넓이가 (\d+) cm²인 △ABC의 무게중심 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 할 때, △AEF의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt)[1]);
  const v=ps(TRI.C,TRI.B);const r=rd(area2([TRI.A,lineHit(CEN,v,TRI.A,TRI.B),lineHit(CEN,v,TRI.A,TRI.C)]),area2([TRI.A,TRI.B,TRI.C]));return solveR(x=>req(rm(r,I(S)),x));},
 'cg-parallel-trap'(q){const S=N(M(/^넓이가 (\d+) cm²인 △ABC의 무게중심 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 할 때, □EBCF의 넓이는 몇 cm²인지 구하시오\.$/,q.prompt)[1]);
  const v=ps(TRI.C,TRI.B);const E=lineHit(CEN,v,TRI.A,TRI.B),F=lineHit(CEN,v,TRI.A,TRI.C);const r=rd(area2([E,TRI.B,TRI.C,F]),area2([TRI.A,TRI.B,TRI.C]));return solveR(x=>req(rm(r,I(S)),x));},
};
const RAT_ASK=' 가장 간단한 자연수의 비로 나타내시오\\.';
const U4F={
 'f-de-bc'(q){const m=M(new RegExp('^△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다\\. AD=(\\d+) cm, DB=(\\d+) cm일 때, DE:BC를'+RAT_ASK+'$'),q.prompt);return deRatio(Rq(N(m[1]),N(m[1])+N(m[2]))).de;},
 'f-ade-area'(q){const m=M(new RegExp('^△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다\\. AD:DB=(\\d+):(\\d+)일 때, △ADE와 △ABC의 넓이의 비를'+RAT_ASK+'$'),q.prompt);
  const [A,B,C]=[P(0,0),P(7,1),P(2,5)];const D=lerp(A,B,Rq(N(m[1]),N(m[1])+N(m[2])));const E=lineHit(D,ps(C,B),A,C);return rd(area2([A,D,E]),area2([A,B,C]));},
 'f-trap-area'(q){const m=M(/^△ABC에서 변 AB, AC 위의 점 D, E에 대하여 DE∥BC이다\. AD:DB=(\d+):(\d+)일 때, □DBCE의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오\.$/,q.prompt);
  const [A,B,C]=[P(0,0),P(7,1),P(2,5)];const D=lerp(A,B,Rq(N(m[1]),N(m[1])+N(m[2])));const E=lineHit(D,ps(C,B),A,C);return rd(area2([D,B,C,E]),area2([A,B,C]));},
 'f-pl-ratio'(q){const m=M(new RegExp('^서로 평행한 세 직선 l, m, n이 직선 p와 만나는 점을 각각 A, B, C, 직선 q와 만나는 점을 각각 D, E, F라고 하자\\. AB=(\\d+) cm, BC=(\\d+) cm일 때, DE:EF를'+RAT_ASK+'$'),q.prompt);
  // l, m, n are the horizontal lines y=0, AB, AB+BC (p vertical); q is a slanted transversal through D=(1,0) — measure DE, EF on q.
  const ab=N(m[1]),bc=N(m[2]);const D=P(1,0),dir=P(3,1);const at=h=>pa(D,pm(dir,I(h)));const E=at(ab),F=at(ab+bc);stats.coordinateChecks++;return rd(I(1),ratioVec(ps(E,D),ps(F,E)));},
 'f-centroid'(q){const p=q.prompt;const v=ps(TRI.C,TRI.B);const E=lineHit(CEN,v,TRI.A,TRI.B),F=lineHit(CEN,v,TRI.A,TRI.C);const T=area2([TRI.A,TRI.B,TRI.C]);
  const L=(X,Y)=>{const pt={...TRI,G:CEN,D:mid(TRI.B,TRI.C),E:mid(TRI.A,TRI.C),F:mid(TRI.A,TRI.B)};return d2(pt[X],pt[Y]);};
  const lenRatio=(s,t)=>{const r=rd(L(s[0],s[1]),L(t[0],t[1]));const a=isqrt(r[0]),b=isqrt(r[1]);if(!(a>0&&b>0))throw Error('irrational ratio');return Rq(a,b);};
  let m;
  if((m=/^점 G가 △ABC의 무게중심이고 (AD|BE|CF)가 중선이다\. ([A-G]{2}):([A-G]{2})를 가장 간단한 자연수의 비로 나타내시오\.$/.exec(p))){for(const s of [m[2],m[3]])if(![...s].every(c=>c==='G'||m[1].includes(c)))throw Error('segment off the median');return lenRatio(m[2],m[3]);}
  if((m=/^점 G가 △ABC의 무게중심이고 (AD|BE|CF)가 중선이다\. ([A-G]{2})의 길이는 ([A-G]{2})의 길이의 몇 배인지 기약분수로 구하시오\.$/.exec(p)))return lenRatio(m[2],m[3]);
  if(/^점 G가 △ABC의 무게중심이고, 점 G를 지나고 변 BC에 평행한 직선이 AB, AC와 만나는 점을 각각 E, F라고 하자\. EF:BC를 가장 간단한 자연수의 비로 나타내시오\.$/.test(p))return ratioVec(v,ps(F,E));
  if(/F라고 하자\. △AEF와 △ABC의 넓이의 비를 가장 간단한 자연수의 비로 나타내시오\.$/.test(p))return rd(area2([TRI.A,E,F]),T);
  if(/F라고 하자\. △AEF와 □EBCF의 넓이의 비를 가장 간단한 자연수의 비로 나타내시오\.$/.test(p))return rd(area2([TRI.A,E,F]),area2([E,TRI.B,TRI.C,F]));
  if(/^점 G가 △ABC의 무게중심이고 D가 변 BC의 중점일 때, △GBD의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오\.$/.test(p))return rd(area2([CEN,TRI.B,mid(TRI.B,TRI.C)]),T);
  if(/F라고 하자\. □EBCF의 넓이는 △ABC의 넓이의 몇 배인지 기약분수로 구하시오\.$/.test(p))return rd(area2([E,TRI.B,TRI.C,F]),T);throw Error('unknown centroid fraction');},
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

export default {A:U4,F:U4F,C:U4C};
