// m2s2-u3 oracle tables for check-m2s2-geo.mjs. Imports NO generator module.
import {here,root,out,curriculum,failures,stats,fail,gcd,M,N,solve,isqrt} from './check-m2s2-geo-lib.mjs';
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
 'sim-angle-mixed'(q){const m=M(/^△ABC∽△([DEF]{3})이고 ∠A=(\d+)°, ∠([DEF])=(\d+)°일 때, ∠C의 크기는 몇 도인지 구하시오\.$/,q.prompt);
  const map=corrMap(m[1]);const back=Object.keys(map).find(k=>map[k]===m[3]);if(back!=='B')throw Error('given DEF angle must correspond to ∠B');
  return solve(c=>c+N(m[2])+N(m[4])===180,1,179);},
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
// Ratio kinds return [a,b] meaning a:b (the checker compares by cross-multiplication and demands
// every option in lowest terms). Areas/volumes are measured on concrete scaled figures.
const shoelace2=pts=>{let s=0;for(let i=0;i<pts.length;i++){const [x1,y1]=pts[i],[x2,y2]=pts[(i+1)%pts.length];s+=x1*y2-x2*y1;}return Math.abs(s);};
const areaScaled=k=>{stats.coordinateChecks++;return shoelace2([[0,0],[5*k,0],[7*k,3*k],[2*k,4*k]]);};           // a fixed quadrilateral, scale k
const boxVol=k=>{stats.coordinateChecks++;return (2*k)*(3*k)*(5*k);},boxSurf=k=>2*((2*k)*(3*k)+(3*k)*(5*k)+(2*k)*(5*k));
// coprime n:d with pred(n,d), searched exhaustively
const coprime=(pred,lim=60)=>{const hits=[];for(let n=1;n<=lim;n++)for(let d=1;d<=lim;d++){stats.bruteForce++;if(gcd(n,d)===1&&pred(n,d))hits.push([n,d]);}if(hits.length!==1)throw Error(`ratio search found ${hits.length}`);return hits[0];};
const SIMPLE='가장 간단한 자연수의 비로 나타내시오\\.';
const U3F={
 'r-sim-ratio'(q){const m=M(new RegExp(`^△ABC∽△([DEF]{3})이고 ([ABC]{2})=(\\d+) cm, ([DEF]{2})=(\\d+) cm, ([DEF]{2})=(\\d+) cm일 때, △ABC와 △\\1의 닮음비를 ${SIMPLE}$`),q.prompt);
  const map=corrMap(m[1]),img=mapSide(m[2],map);const c1=same2(img,m[4]),c2=same2(img,m[6]);if(c1===c2)throw Error('exactly one given DEF side must correspond');
  const [a,b]=c1?[N(m[3]),N(m[5])]:[N(m[3]),N(m[7])];return coprime((n,d)=>n*b===d*a);},
 'r-area-ratio'(q){const m=M(new RegExp(`^닮은 두 (\\S+) \\(가\\), \\(나\\)의 닮음비가 (\\d+):(\\d+)일 때, \\(가\\)와 \\(나\\)의 넓이의 비를 ${SIMPLE}$`),q.prompt);
  return [areaScaled(N(m[2])),areaScaled(N(m[3]))];},
 'r-area-sides'(q){const m=M(new RegExp(`^△ABC∽△DEF이고 AB=(\\d+) cm, DE=(\\d+) cm일 때, △ABC와 △DEF의 넓이의 비를 ${SIMPLE}$`),q.prompt);
  return [areaScaled(N(m[1])),areaScaled(N(m[2]))];},
 'r-volume-ratio'(q){const m=M(new RegExp(`^닮은 두 (\\S+) \\(가\\), \\(나\\)의 닮음비가 (\\d+):(\\d+)일 때, \\(가\\)와 \\(나\\)의 부피의 비를 ${SIMPLE}$`),q.prompt);
  return [boxVol(N(m[2])),boxVol(N(m[3]))];},
 'r-surface-ratio'(q){const m=M(new RegExp(`^닮은 두 (\\S+) \\(가\\), \\(나\\)의 닮음비가 (\\d+):(\\d+)일 때, \\(가\\)와 \\(나\\)의 겉넓이의 비를 ${SIMPLE}$`),q.prompt);
  return [boxSurf(N(m[2])),boxSurf(N(m[3]))];},
 'r-ratio-from-area'(q){const m=M(new RegExp(`^넓이가 각각 (\\d+) cm², (\\d+) cm²인 닮은 두 삼각형 \\(가\\), \\(나\\)의 닮음비를 ${SIMPLE}$`),q.prompt);
  const A1=N(m[1]),A2=N(m[2]);return coprime((n,d)=>n*n*A2===d*d*A1);},
 'r-ratio-from-volume'(q){const m=M(new RegExp(`^부피가 각각 (\\d+) cm³, (\\d+) cm³인 닮은 두 (\\S+) \\(가\\), \\(나\\)의 닮음비를 ${SIMPLE}$`),q.prompt);
  const V1=N(m[1]),V2=N(m[2]);return coprime((n,d)=>n**3*V2===d**3*V1);},
 'r-area-from-volume'(q){const m=M(new RegExp(`^닮은 두 (\\S+) \\(가\\), \\(나\\)의 부피의 비가 (\\d+):(\\d+)일 때, \\(가\\)와 \\(나\\)의 겉넓이의 비를 ${SIMPLE}$`),q.prompt);
  const V1=N(m[2]),V2=N(m[3]);const [n,d]=coprime((n,d)=>n**3*V2===d**3*V1);return [boxSurf(n),boxSurf(d)];},
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

export default {A:U3,F:U3F,C:U3C};
