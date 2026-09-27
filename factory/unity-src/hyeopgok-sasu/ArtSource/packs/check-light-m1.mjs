#!/usr/bin/env node
// 중1 경량 팩 9개(m1s1-u1~u4, m1s2-u1~u5) 가벼운 검산 — SPEC-LIGHT-PACKS.md §검증.
// 1) 구조: 30문항·4지선다·정답 1개·값 중복 없음·정답 위치 7~8·난도 분포·표기(frac 토큰·√·평문 분수).
// 2) 계산: 생성 원자료를 가져오지 않고, 문항별로 정답을 정수·유리수 정확 연산으로 다시 계산해
//    보기 중 그 값과 일치하는 것이 정확히 하나이고 그것이 answer 인지 확인한다.
//    개념 문항(용어·성질)은 'concept' 로 표시하고 사람이 다시 읽어 확인했다.
// 실행: node factory/unity-src/hyeopgok-sasu/ArtSource/packs/check-light-m1.mjs
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const ROOT=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../../../../..');
const PACKS=path.join(ROOT,'public/g/hyeopgok-sasu/packs');
const IDS=['m1s1-u1','m1s1-u2','m1s1-u3','m1s1-u4','m1s2-u1','m1s2-u2','m1s2-u3','m1s2-u4','m1s2-u5'];

// ── 유리수 (정수 분자/분모, 부동소수점 비교 없음) ──
const gcd=(a,b)=>{a=Math.abs(a);b=Math.abs(b);while(b)[a,b]=[b,a%b];return a;};
const lcm=(a,b)=>a/gcd(a,b)*b;
class Q{constructor(n,d=1){if(!Number.isInteger(n)||!Number.isInteger(d)||d===0)throw new Error(`Q(${n},${d})`);if(d<0){n=-n;d=-d;}const g=gcd(n,d)||1;this.n=n/g;this.d=d/g;}
  add(o){o=q(o);return new Q(this.n*o.d+o.n*this.d,this.d*o.d);} sub(o){o=q(o);return this.add(new Q(-o.n,o.d));}
  mul(o){o=q(o);return new Q(this.n*o.n,this.d*o.d);} div(o){o=q(o);return new Q(this.n*o.d,this.d*o.n);}
  eq(o){o=q(o);return this.n===o.n&&this.d===o.d;} cmp(o){o=q(o);return Math.sign(this.n*o.d-o.n*this.d);}
  pow(k){let r=new Q(1);for(let i=0;i<k;i++)r=r.mul(this);return r;} key(){return `${this.n}/${this.d}`;} }
const q=(x,d=1)=>x instanceof Q?x:new Q(x,d);
const dec=s=>{const [i,f='']=s.split('.');return new Q(+(i+f),10**f.length);};

// ── 보기 해석기 ──
const UNITS=[' cm²',' cm³',' cm',' °C','°','개','배','가지'];
function splitUnit(s){for(const u of UNITS)if(s.endsWith(u))return [s.slice(0,-u.length),u];return [s,''];}
function scalar(s){ // "−{frac:3/2}", "+3", "0.25", "12"
  let m=/^([+−]?)\{frac:(\d+)\/(\d+)\}$/.exec(s);if(m)return new Q((m[1]==='−'?-1:1)*+m[2],+m[3]);
  m=/^([+−]?)(\d+(?:\.\d+)?)$/.exec(s);if(m){const v=dec(m[2]);return m[1]==='−'?v.mul(-1):v;}
  return null;}
function num(s){const [b,u]=splitUnit(s);const v=scalar(b);return v?{v,u}:null;}
function piExpr(s){ // "6π cm", "(2π+6) cm", "{frac:20/3}π cm³", "4 cm²"
  const [b0,u]=splitUnit(s);let b=b0;if(/^\(.*\)$/.test(b))b=b.slice(1,-1);
  let pi=new Q(0),c=new Q(0);for(const t of b.split('+')){
    if(t.endsWith('π')){const k=t.slice(0,-1);const v=k===''?new Q(1):scalar(k);if(!v)return null;pi=pi.add(v);}
    else{const v=scalar(t);if(!v)return null;c=c.add(v);}}
  return {pi,c,u};}
function factor(s){ // "2<sup>2</sup>×3" → {value, primeBases, bases}
  let value=1,prime=true;for(const t of s.split('×')){const m=/^(\d+)(?:<sup>(\d+)<\/sup>)?$/.exec(t);if(!m)return null;
    const b=+m[1],e=m[2]?+m[2]:1;value*=b**e;if(!isPrime(b))prime=false;}
  return {value,prime};}
function lin(s){ // 일차식 "4x−3", "−x+4" → [계수, 상수]
  if(/[^0-9x+−.]/.test(s))return null;let a=new Q(0),b=new Q(0);
  for(const t of s.replace(/−/g,'+−').split('+').filter(Boolean)){
    if(t.endsWith('x')){const k=t.slice(0,-1);const v=k===''?new Q(1):k==='−'?new Q(-1):scalar(k);if(!v)return null;a=a.add(v);}
    else{const v=scalar(t);if(!v)return null;b=b.add(v);}}
  return [a,b];}
const sol=s=>{const m=/^x=(.+)$/.exec(s);return m?scalar(m[1]):null;};
const point=s=>{const m=/^\(([^,]+), ([^)]+)\)$/.exec(s);return m?[scalar(m[1]),scalar(m[2])]:null;};
const list=s=>s.split(', ').map(t=>{const v=scalar(t);return v&&v.d===1?v.n:NaN;});
function isPrime(n){if(n<2)return false;for(let i=2;i*i<=n;i++)if(n%i===0)return false;return true;}
function primeFactors(n){const r=[];for(let p=2;p<=n;p++)if(n%p===0&&isPrime(p))r.push(p);return r;}
const setEq=(a,b)=>a.length===b.length&&[...a].sort((x,y)=>x-y).every((v,i)=>v===[...b].sort((x,y)=>x-y)[i]);
const isPF=(s,n)=>{const f=factor(s);return !!f&&f.prime&&f.value===n;};
const quadrant=(x,y)=>x>0&&y>0?1:x<0&&y>0?2:x<0&&y<0?3:x>0&&y<0?4:0;
const qName=s=>{const m=/^제(\d)사분면$/.exec(s);return m?+m[1]:null;};
function yOf(rel,x){ // 관계식에서 x 에 대한 y
  x=q(x);let m;
  if((m=/^xy=(.+)$/.exec(rel)))return scalar(m[1]).div(x);
  if((m=/^y=(.+)$/.exec(rel))){const l=lin(m[1]);return l?l[0].mul(x).add(l[1]):null;}
  if((m=/^x\+y=(.+)$/.exec(rel)))return scalar(m[1]).sub(x);
  if((m=/^2\(x\+y\)=(.+)$/.exec(rel)))return scalar(m[1]).div(2).sub(x);
  if((m=/^(\d+)y=x$/.exec(rel)))return x.div(+m[1]);
  return null;}
const isDirect=r=>{const k=yOf(r,1);return !!k&&!k.eq(0)&&[2,3,-4].every(x=>yOf(r,x).eq(k.mul(x)));};
const isInverse=r=>{const k=yOf(r,1);return !!k&&!k.eq(0)&&[2,3,-4].every(x=>yOf(r,x).mul(x).eq(k));};
const tri=(a,b,c)=>{const [x,y,z]=[a,b,c].sort((p,s)=>p-s);return x+y>z;};
const lens=s=>s.split(', ').map(t=>+t.replace(' cm',''));
const median=a=>{a=[...a].sort((x,y)=>x-y);const n=a.length;return n%2?q(a[(n-1)/2]):q(a[n/2-1]+a[n/2],2);};
const mode=a=>{const c={};a.forEach(v=>c[v]=(c[v]??0)+1);const mx=Math.max(...Object.values(c));const ms=Object.keys(c).filter(k=>c[k]===mx);if(ms.length!==1)throw new Error('mode');return q(+ms[0]);};
const mean=a=>q(a.reduce((s,v)=>s+v,0),a.length);
const POLY={사각형:4,오각형:5,육각형:6,칠각형:7,팔각형:8,구각형:9,십각형:10};
const regN=s=>POLY[s.replace(/^정/,'')];
// 직육면체 모서리 관계 전수 열거 (단위 정육면체 좌표)
function cubeCounts(){
  const V=[];for(let i=0;i<8;i++)V.push([i&1,(i>>1)&1,(i>>2)&1]);
  const E=[];for(let i=0;i<8;i++)for(let j=i+1;j<8;j++)if(V[i].reduce((s,v,k)=>s+Math.abs(v-V[j][k]),0)===1)E.push([i,j]);
  const dir=e=>V[e[0]].findIndex((v,k)=>v!==V[e[1]][k]);const e0=E[0];let par=0,skew=0,perpMeet=0;
  for(const e of E.slice(1)){const meet=e.some(v=>e0.includes(v));if(dir(e)===dir(e0))par++;else if(meet)perpMeet++;else skew++;}
  // 면: 좌표 k 가 0 또는 1 인 6개. 면 x=0 과 평행 = 같은 k, 다른 값
  const faces=[];for(let k=0;k<3;k++)for(const c of [0,1])faces.push([k,c]);const parFaces=faces.filter(f=>f[0]===0&&f[1]!==0).length;
  return {edges:E.length,par,skew,perpMeet,parFaces};}
const CUBE=cubeCounts();
const corr=(from,to)=>ch=>to[from.indexOf(ch)];
const cmpStr=s=>{const m=/^(.+)([<>])(.+)$/.exec(s);if(!m)return null;const a=scalar(m[1]),b=scalar(m[3]);if(!a||!b)return null;return m[2]==='>'?a.cmp(b)>0:a.cmp(b)<0;};

// ── 문항별 검산표 ── v: 기대값(Q/수) [u: 단위], pi: {pi,c}, pick: 보기 판정 함수, concept: 개념 문항
const C=s=>({concept:s});
const V=(v,u='')=>({v:q(v),u});
const P=f=>({pick:f});
const PI=(pi,c=0,u)=>({pi:{pi:q(pi),c:q(c),u}});
const LIN=(a,b)=>P(c=>{const l=lin(c);return !!l&&l[0].eq(a)&&l[1].eq(b);});
const SOL=v=>P(c=>{const s=sol(c);return !!s&&s.eq(v);});
const solve=([a1,b1],[a2,b2])=>q(b2).sub(b1).div(q(a1).sub(a2)); // a1x+b1=a2x+b2
const CHECKS={
'm1s1-u1':[
 P(c=>isPrime(+c)), P(c=>factor(c)?.value===8), V(3**4), V('5×5×5×5'.split('×').length), P(c=>isPF(c,12)), P(c=>isPF(c,18)),
 P(c=>setEq(list(c),primeFactors(20))), P(c=>+c>1&&!isPrime(+c)), C('2는 짝수인 소수'), V(2**3*3), P(c=>isPF(c,30)),
 P(c=>factor(c)?.value===gcd(12,18)), P(c=>factor(c)?.value===lcm(12,18)), P(c=>factor(c)?.value===gcd(30,70)),
 V(gcd(20,30)), V(lcm(8,12)), V(2**2*5), P(c=>{const [n,f]=c.split('=');return isPF(f,+n);}),
 P(c=>isPF(c,72)), P(c=>setEq(list(c),primeFactors(90))), P(c=>factor(c)?.value===gcd(72,108)), P(c=>factor(c)?.value===lcm(20,30)),
 V(gcd(36,48)), P(c=>12%+c!==0), V([...Array(10).keys()].find(a=>2**a*5===40)), P(c=>!isPrime(+c)), P(c=>isPF(c,54)),
 P(c=>{const [a,b]=c.split(', ').map(t=>factor(t).value);return gcd(a,b)===12;}), P(c=>factor(c)?.value===lcm(60,36)),
 V([...Array(100).keys()].slice(1).find(k=>Number.isInteger(Math.sqrt(84*k)))),
],
'm1s1-u2':[
 P(c=>num(c).v.cmp(0)<0), V(q(3).add(-5)), V(q(-4).add(-6)), V(q(2).sub(7)), V(q(-3).sub(-8)), V(q(-3).mul(5)), V(q(-4).mul(-6)),
 V(q(-12).div(3)), V(5), P(c=>['−1','−8','−3','−5'].every(o=>num(c).v.cmp(num(o).v)>=0)),
 P(c=>['0','−2','+1','−{frac:1/2}'].every(o=>num(c).v.cmp(num(o).v)<=0)), P(c=>setEq(list(c),[3,-3])),
 P(c=>num(c).v.d!==1), V(-3,' °C'), V(q(6).div(-2)), V(q(-3).pow(2)), V(dec('1.5').sub(dec('2.5'))), V(new Q(-1,3)),
 V(q(-2).mul(-3).mul(-2)), V([...Array(20).keys()].map(i=>i-10).filter(x=>x>-2&&x<5).length), V(new Q(1,2).sub(new Q(1,3))),
 V(new Q(-2,3).mul(new Q(3,4))), V(q(-8).div(new Q(-2,3))), V(q(3).pow(2).mul(-1)), V(q(3).sub(q(-2).mul(4))),
 V(q(-12).div(4).mul(-3)), V([...Array(20).keys()].map(i=>i-10).filter(x=>Math.abs(x)<=4).length), P(c=>cmpStr(c)===true),
 V(new Q(-1,2).pow(2).mul(-8).div(2)), V(q(5).add(q(-3).mul(q(2).sub(-4)))),
],
'm1s1-u3':[
 C('곱셈 기호 생략: 수를 문자 앞에'), C('같은 문자의 곱은 거듭제곱'), V(2*3+1), V(3*-2), C('x의 계수 3'), C('상수항 7'),
 LIN(7,0), LIN(-5,0), LIN(2,6), LIN(-1,4), LIN(4,-3), LIN(1,4), SOL(5), SOL(4),
 P(c=>{const [l,r]=c.split('=').map(lin);return !!l&&!!r&&l[0].mul(2).add(l[1]).eq(r[0].mul(2).add(r[1]));}),
 C('일차방정식: 등식이고 일차'), C('500×x원'), V(q(0).sub(-4)) /* x−4+k=x */,
 SOL(solve([2,-3],[0,7])), SOL(solve([3,4],[1,-6])), SOL(solve([5,-5],[2,7])),
 V(q(-2).pow(2).sub(q(3).mul(-2))), SOL(solve([new Q(1,2),1],[0,4])),
 LIN(q(3).mul(2).sub(2),q(3).mul(-1).sub(q(2).mul(2))), V(solve([3,-5],[0,16])), SOL(solve([dec('0.2'),dec('0.5')],[0,dec('1.3')])),
 V(q(7).add(2).div(3)), LIN(new Q(1,2).mul(4).sub(1),new Q(1,2).mul(-6).add(1)), SOL(solve([dec('0.3'),dec('0.3').mul(-2)],[dec('0.5'),-1])),
 V(solve([3,3],[0,48]).add(2)),
],
'm1s1-u4':[
 V(3), V(5), P(c=>qName(c)===quadrant(2,3)), P(c=>qName(c)===quadrant(-3,1)), P(c=>qName(c)===quadrant(-2,-5)), P(c=>qName(c)===quadrant(4,-1)),
 P(c=>{const p=point(c);return p[0].eq(5)&&p[1].eq(0);}), C('일정한 빠르기 → 증가하는 직선'),
 P(c=>{const p=point(c);return quadrant(p[0].n,p[1].n)===0;}), V(yOf('y=2x',4)), V(yOf('xy=12',3)), P(isDirect), P(isInverse),
 P(c=>isDirect(c)&&yOf(c,2).eq(10)), P(c=>isInverse(c)&&yOf(c,2).eq(6)),
 P(c=>{const p=point(c);return [1,2,-3].every(a=>p[1].eq(p[0].mul(a)));}), P(c=>{const p=point(c);return yOf('y=2x',p[0]).eq(p[1]);}),
 V(yOf('y=−3x',-2)), P(c=>setEq(c.split(', ').map(qName),[...new Set([1,-1].map(x=>quadrant(x,yOf('y=−2x',x).n)))])),
 P(c=>{const p=point(c);return p[0].mul(p[1]).eq(8);}), V(q(3).div(3)), V(q(0).sub(1)), P(c=>qName(c)===quadrant(-1,1)),
 P(c=>qName(c)===quadrant(-1,1)/* a=1,b=−1 → (b,a)=(−1,1) */), V(yOf('y=5x',2).div(yOf('y=5x',1)),'배'), V(yOf('xy=6',3).div(yOf('xy=6',1)),'배'),
 P(c=>[1,2,3].every(x=>yOf(c,x)?.eq(60*x))), P(c=>[1,2,3].every(x=>yOf(c,x)?.mul(x).eq(24))), V(q(3).mul(-4)),
 V(q(-9).div(q(6).div(2))),
],
'm1s2-u1':[
 C('두 점을 지나는 직선은 하나'), V(180,'°'), V(90,'°'), C('맞꼭지각은 같다'), V(50,'°'), V(180-70,'°'), V(q(10).div(2),' cm'), C('평행선의 동위각은 같다'),
 V(65,'°'), V(110,'°'), C('꼬인 위치는 공간에서만'), C('꼬인 위치의 정의'), C('점과 직선 사이의 거리'), V(180-40,'°'), C('수직 기호 ⊥'),
 P(c=>c===(120<90?'예각':120===90?'직각':120<180?'둔각':'평각')), V(CUBE.par,'개'), V(2*4,' cm'),
 V(solve([2,0],[1,30])), V(solve([2,10],[0,70])), C('엇각이 같으면 평행'), V(CUBE.skew,'개'), V(CUBE.perpMeet,'개'), V(CUBE.parFaces,'개'),
 V(3*2/2,'개'), V(1,'개'), V(180-40-65,'°'), V(12-12/2/2,' cm'),
 V([50,130,50,130,50,130,50,130].filter(a=>a===130).length,'개'), V(360-290,'°'),
],
'm1s2-u2':[
 C('작도 도구: 눈금 없는 자와 컴퍼스'), C('컴퍼스의 쓰임'), C('눈금 없는 자의 쓰임'), P(c=>tri(...lens(c))), P(c=>!tri(...lens(c))),
 P(c=>c==='변 '+'AB'.split('').map(corr('ABC','DEF')).join('')), P(c=>c==='∠'+corr('ABC','DEF')('B')), C('합동 기호 ≡'),
 V({DE:5,EF:7,FD:6}['AB'.split('').map(corr('ABC','DEF')).join('')],' cm'), V({A:60,B:80,C:40}[corr('DEF','ABC')('F')],'°'),
 C('SSS'), C('SAS'), C('ASA'), C('AAA 는 합동 조건 아님'), C('세 변 → 하나로 정해짐'), C('합동: 대응변·대응각 같음'),
 P(c=>c==='꼭짓점 '+corr('ABC','PQR')('C')), C('합동이면 넓이 같음'), P(c=>{const x=+c.replace(' cm','');return x>7-3&&x<7+3;}),
 C('SAS (끼인각 ∠B)'), C('ASA (변 BC 양 끝 각)'), C('SSS'), C('∠A 는 끼인각 아님'), V(180-50-70,'°'), V(4+6+5,' cm'),
 V([...Array(20).keys()].filter(x=>x>0&&tri(4,6,x)).length,'개'), C('작도: 컴퍼스로 길이 옮김'), V(solve([2,10],[0,50])),
 V([[2,3,4],[2,3,5],[2,4,5],[3,4,5]].filter(t=>tri(...t)).length,'개'), V([...Array(20).keys()].find(x=>x>0&&tri(x,5,9))),
],
'm1s2-u3':[
 V(180,'°'), V(180*2,'°'), V(180*3,'°'), C('외각의 합 360°'), V(180-50-60,'°'), V(40+75,'°'), V(6-3,'개'), V(180*4/6,'°'), V(360/8,'°'),
 C('호'), C('현'), C('호의 길이 ∝ 중심각'), PI(2*3,0,' cm'), PI(2*2,0,' cm²'), V(q(3).mul(60).div(30),' cm'), V(q(8).mul(80).div(40),' cm²'),
 V(5-2,'개'), V(540/5,'°'), V(6*3/2,'개'), P(c=>180*(POLY[c]-2)===720), P(c=>360/regN(c)===45), V(q(180).mul(3).div(6),'°'), V(110-45,'°'),
 PI(q(2*6).mul(60).div(360),0,' cm'), PI(q(36).mul(60).div(360),0,' cm²'), PI(new Q(1,2).mul(4).mul(2),0,' cm²'), C('같은 중심각 → 호·현 같음'),
 V(9-3,'개'), P(c=>POLY[c]*(POLY[c]-3)/2===20), PI(q(2*3).mul(120).div(360),3+3,' cm'),
],
'm1s2-u4':[
 C('원기둥은 회전체'), V(5+2,'개'), V(4+1,'개'), V(3*3,'개'), C('정다면체 5가지'), C('정육면체 한 꼭짓점에 3면'), C('정육면체 면은 정사각형'),
 C('직사각형 회전 → 원기둥'), C('직각삼각형 회전 → 원뿔'), C('반원 회전 → 구'), C('수직 단면은 원'), C('원뿔 축 단면은 이등변삼각형'),
 V(10*6,' cm³'), V(q(12*5).div(3),' cm³'), V(6*3*3,' cm²'), PI(2**2*5,0,' cm³'), PI(4*2**2,0,' cm²'), C('정사면체 면은 정삼각형'),
 V(2*6,'개'), V(3*(8-2),'개'), PI(new Q(4,3).mul(3**3),0,' cm³'), PI(new Q(1,3).mul(3**2*4),0,' cm³'), PI(2*2**2+2*2*5,0,' cm²'),
 V(8*3/4,'개'), C('정십이면체 면은 정오각형'), V(2*3*4+(3+4)*2*5,' cm²'), C('수직 단면은 원'), V(5+2,'개'),
 PI(q(3**2).add(new Q(1,2).mul(5).mul(2*3)),0,' cm²'), V((2*2)**3/2**3,'배'),
],
'm1s2-u5':[
 V(mode([3,3,4,8,12])), V(median([2,6,7,10,15])), V(median([8,2,11,4,5])), V(median([1,3,5,7])), C('도수는 대푯값 아님'), C('극단값 → 중앙값'),
 C('범주형 → 최빈값'), C('계급'), C('도수'), V(20-10), P(c=>+c>=10&&+c<20), V(q(5,20)), V(1), V(3*10+5), V(q(30).mul(dec('0.2'))),
 C('가로 = 계급의 크기'), C('세로 = 도수'), V(mode([4,4,4,6,7,9,10])), V(median([3,5,6,8,9,11])), V(q(5).mul(2).sub(3)), V(mean([2,3,3,4,98])),
 V(q(40).mul(dec('0.35'))), P(c=>c===(q(5,25).cmp(q(5,20))>0?'A반':'B반')), V(q(9).div(dec('0.3'))), C('넓이 같음'), V(5*4), V(15-3-7),
 V(Math.max(...[12,15,18,20,23])), V(q(5*6+12,6)),
 P(c=>{const a=q(6,20),b=q(7,25),d=a.sub(b);return c===`${d.cmp(0)>0?'A':'B'}반이 ${(Math.abs(d.n)/d.d).toString()} 더 크다.`;}),
],
};

// ── 실행 ──
const curriculum=JSON.parse(fs.readFileSync(path.join(ROOT,'curriculum/2022-middle-math.json'),'utf8'));
const G1=new Set(curriculum.standards.filter(s=>s.typical_grade===1).map(s=>s.code));
const errors=[],warnings=[];let calc=0,concept=0;
const err=(id,m)=>errors.push(`${id}: ${m}`);
function valueKey(c){const n=num(c);if(n)return `n:${n.v.key()}${n.u}`;const p=piExpr(c);if(p&&!p.pi.eq(0))return `p:${p.pi.key()}+${p.c.key()}${p.u}`;
  const l=lin(c);if(l)return `l:${l[0].key()},${l[1].key()}`;return `t:${c}`;}
for(const pid of IDS){
  const pack=JSON.parse(fs.readFileSync(path.join(PACKS,`${pid}.json`),'utf8'));
  const [,sem,ord]=/^m1s(\d)-u(\d)$/.exec(pid);
  if(pack.schema_version!==3||pack.pack_id!==pid||pack.unit_id!==pid||pack.school!=='middle'||pack.grade!==1||pack.semester!==+sem||pack.unit_order!==+ord)err(pid,'최상위 메타');
  if(JSON.stringify(pack.economy)!==JSON.stringify({carry_capacity:120,coin_per_kill:1,min_spawn_coins:140}))err(pid,'economy');
  if(!pack.standards.length||pack.standards.some(s=>!G1.has(s)))err(pid,`성취기준 ${pack.standards}`);
  const items=pack.items,checks=CHECKS[pid];
  if(items.length!==30)err(pid,`문항 수 ${items.length}`);if(checks.length!==items.length)err(pid,`검산표 ${checks.length}개`);
  const slots=[0,0,0,0],lv={1:0,2:0,3:0},ids=new Set(),prompts=new Set();
  if(items[0].difficulty!==1)err(pid,'items[0] 난도 1 아님');
  items.forEach((it,i)=>{
    const id=it.id;if(id!==`${pid}-${String(i+1).padStart(3,'0')}`||ids.has(id))err(id,'id');ids.add(id);
    if(prompts.has(it.prompt))err(id,'발문 중복');prompts.add(it.prompt);
    if(it.answer_mode!=='choice'||!['int','frac','text'].includes(it.format))err(id,'answer_mode/format');
    const ch=it.choices;if(!Array.isArray(ch)||ch.length!==4||ch.some(c=>typeof c!=='string'||!c))err(id,'보기 4개');
    if(ch.filter(c=>c===it.answer).length!==1)err(id,'정답이 보기에 정확히 1개가 아님');
    const keys=ch.map(valueKey);if(new Set(keys).size!==4)err(id,`값이 같은 보기 ${ch}`);
    slots[ch.indexOf(it.answer)]++;lv[it.difficulty]=(lv[it.difficulty]??0)+1;
    if(!Array.isArray(it.distractor_tags)||it.distractor_tags.length!==3||new Set(it.distractor_tags).size<2)err(id,'distractor_tags 3개(서로 다른 것 2개 이상)');
    if(typeof it.explain!=='string'||it.explain.length<5||/\n/.test(it.explain)||!it.unitConcept)err(id,'explain/unitConcept');
    if(it.format==='int'&&!/^−?\d+$/.test(it.answer))err(id,'format int');
    if(it.format==='frac'&&!/^−?\{frac:\d+\/\d+\}$/.test(it.answer))err(id,'format frac');
    if(it.answerNumeric!==undefined){const n=num(it.answer);if(!n||n.v.n/n.v.d!==it.answerNumeric)err(id,'answerNumeric');}
    const text=[it.prompt,it.explain,...ch].join(' ');
    if(/√/.test(text))err(id,'√ 금지');
    if(/\{frac:(?!\d+\/\d+\})/.test(text))err(id,'frac 토큰은 숫자/숫자만 (런타임 정규식)');
    if(/\d\s*\/\s*\d/.test(text.replace(/\{frac:\d+\/\d+\}/g,'')))err(id,'평문 분수 금지');
    if(/(^|[\s(=,])-\d|\d-\d/.test(text))err(id,'음수는 − (U+2212)');
    if(/자리에서\s*(올림|버림|반올림)/.test(text))err(id,'AT_PLACE 어림 발문');
    if(/가정|결론/.test(text))err(id,'가정·결론 용어 금지');
    const shown=c=>c.replace(/<\/?sup>/g,'').replace(/\{frac:\d+\/\d+\}/g,'ff');if(ch.some(c=>shown(c).length>18))warnings.push(`${id}: 긴 보기 ${ch.find(c=>shown(c).length>18)}`);
    // 수학 검산
    const k=checks[i];if(!k)return;
    if(k.concept){concept++;return;}
    calc++;let hits;
    if(k.v){hits=ch.filter(c=>{const n=num(c);return !!n&&n.v.eq(k.v);});const n=num(it.answer);if(n&&n.u!==k.u)err(id,`단위 ${n.u}≠${k.u}`);}
    else if(k.pi){hits=ch.filter(c=>{const p=piExpr(c);return !!p&&p.pi.eq(k.pi.pi)&&p.c.eq(k.pi.c)&&p.u===k.pi.u;});}
    else hits=ch.filter(c=>{try{return k.pick(c)===true;}catch{return false;}});
    if(hits.length!==1||hits[0]!==it.answer)err(id,`검산 불일치 — 재계산 일치 보기 [${hits}] / answer ${it.answer}`);
  });
  if(slots.some(n=>n<7||n>8))err(pid,`정답 위치 분포 ${slots}`);
  if(lv[1]<16||lv[1]>20||lv[2]<8||lv[2]>12||(lv[3]??0)>2||Object.keys(lv).some(d=>![1,2,3].includes(+d)))err(pid,`난도 분포 ${JSON.stringify(lv)}`);
  console.log(`${pid} ${pack.title}: ${items.length}문항 · 정답 위치 ${slots.join('/')} · 난도 ${lv[1]}/${lv[2]}/${lv[3]??0}`);
}
console.log(`계산 재검산 ${calc}문항 · 개념(수동 확인) ${concept}문항 · 경고 ${warnings.length} · 오류 ${errors.length}`);
warnings.forEach(w=>console.log('  경고',w));
errors.forEach(e=>console.log('  오류',e));
process.exitCode=errors.length?1:0;
