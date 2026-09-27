#!/usr/bin/env node
// m2s2-u3 도형의 닮음 ([9수03-12] 닮음의 뜻·성질·닮음비, [9수03-13] 삼각형의 닮음 조건) — schema v3.
// Vertex correspondence is data: △ABC∽△XYZ means A↔X, B↔Y, C↔Z, and every side/angle is
// derived from that array (expression trap: 대응 순서). No √: right-triangle similarity answers
// come from perfect squares (AD²=BD×DC=36 → AD=6).
// v3: every item is a four-option choice. Numeric wrong options are computed from named
// misconceptions (non-integers / non-positive values are discarded by the kit, never patched).
// 닮음비·넓이의 비·부피의 비 are asked in the textbook form 「가장 간단한 자연수의 비로 나타내시오」
// and the area/volume ratio items always keep the 닮음비-as-is option (curriculum trap).
import {amount,fraction,choice,W,gcd,josa,writeGeoPack} from './geo-kit.mjs';
const T='m2s2-u3.';
const MC={linear:T+'linear-area-volume',pos:T+'correspondence-by-position',look:T+'looks-similar',add:T+'additive-reasoning',inv:T+'ratio-inverted',sq:T+'square-missed',sym:T+'symbol-order-ignored',
  power:T+'wrong-power',root:T+'square-root-step-missed',same:T+'similar-means-congruent',sum:T+'angle-sum-error',pyth:T+'pythagoras-confused'};
const pools={};const add=(k,it)=>(pools[k]??=[]).push(it);
const PERMS=['DEF','DFE','EDF','EFD','FDE','FED'];
const mapOf=t2=>({A:t2[0],B:t2[1],C:t2[2]});
const sideOf=(s,t2)=>{const m=mapOf(t2);return [m[s[0]],m[s[1]]].sort().join('');};
const SIDES=['AB','BC','AC'];
const rt=(a,b,p,q)=>a===p?`${p}:${q}`:`${a}:${b}=${p}:${q}`;
const RAT=[];for(let p=1;p<=6;p++)for(let q=1;q<=6;q++)if(p!==q&&gcd(p,q)===1)RAT.push([p,q]);
const SIM='닮음의 성질',RATIO='닮음비',AREA='닮은 도형의 넓이의 비',VOL='닮은 입체도형의 부피의 비',COND='삼각형의 닮음 조건',RT='직각삼각형의 닮음';
const R=(num,den)=>({num,den});
// Wrong-option selection: among the valid misconception values (priority order) pick three so the
// answer's rank among the four options follows a prompt hash — the correct option is not
// systematically the largest/smallest. keepFirst forces the first candidate (e.g. 닮음비 as-is).
const hash=s=>{let h=7;for(const c of s)h=(h*31+c.codePointAt(0))>>>0;return h;};
function bal(ans,ws,seed,{keepFirst=false,lo=1,hi=Infinity}={}){
  const val=v=>typeof v==='object'?v.num/v.den:v;
  const ok=v=>typeof v==='object'?(Number.isSafeInteger(v.num)&&Number.isSafeInteger(v.den)&&v.num>0&&v.den>0):(Number.isSafeInteger(v)&&v>=lo&&v<=hi);
  const A=val(ans),seen=new Set([A]),u=[];for(const w of ws){if(!ok(w.value))continue;const x=val(w.value);if(seen.has(x))continue;seen.add(x);u.push(w);}
  if(u.length<=3)return u;
  const first=keepFirst?[u[0]]:[],rest=keepFirst?u.slice(1):u,need=3-first.length;
  const below=rest.filter(w=>val(w.value)<A),above=rest.filter(w=>val(w.value)>A);
  const fb=first.filter(w=>val(w.value)<A).length;
  const k=Math.min(Math.min(need,below.length),Math.max(Math.max(0,need-above.length),(seed>>>3)%4-fb));
  const pick=new Set([...first,...below.slice(0,k),...above.slice(0,need-k)]);
  return [...u.filter(w=>pick.has(w)),...u.filter(w=>!pick.has(w))];
}
// amount/fraction wrappers that run bal() on the prompt hash
const num=o=>amount({...o,wrongs:bal(o.answer,o.wrongs,hash(o.prompt),{lo:o.lo,hi:o.hi})});
const rat=o=>fraction({...o,as:o.as??'ratio',wrongs:bal(R(o.num,o.den),o.wrongs,hash(o.prompt),{keepFirst:o.keepFirst})});

// ── 대응변의 길이 ────────────────────────────────────────────
let seq=0;
for(const t2 of PERMS)for(const [p,q] of RAT)for(let si=0;si<3;si++){const s1=SIDES[si],s2=SIDES[(si+1+(p+q)%2)%3];
  for(const u of [2,3,4])for(const v of [2,3,4,5,7]){seq++;if(seq%3)continue;const a=p*u,b=q*u,c=p*v,ans=q*v;if(a===c)continue;
    const S1=sideOf(s1,t2),S2=sideOf(s2,t2);
    add(t2==='DEF'?'side1':'side2',num({kind:'sim-side',params:{t2,p,q,given:[s1,a,S1,b,s2,c],ask:S2},prompt:`△ABC∽△${t2}이고 ${s1}=${a} cm, ${S1}=${b} cm, ${s2}=${c} cm일 때, ${S2}의 길이는 몇 cm인지 구하시오.`,answer:ans,
      explain:`${s1}의 대응변이 ${S1}이므로 닮음비는 ${rt(a,b,p,q)}, ${S2}=${c}×${q}÷${p}=${ans} cm입니다.`,concept:SIM,d:t2==='DEF'?1:2,
      wrongs:[W(c+b-a,MC.add,'additive'),W(c*p/q,MC.inv,'inverted'),W(c*q*q/(p*p),MC.power,'area-ratio-for-length'),W(c,MC.same,'same-length'),W(c+a-b,MC.add,'additive-reversed')]}));}}
for(const [p,q] of RAT)for(let k=2;k*Math.max(p,q)<=80;k++){const s=SIDES[k%3],S=sideOf(s,'DEF');
  add('side1',num({kind:'sim-ratio-given',params:{p,q,side:s,len:p*k},prompt:`△ABC∽△DEF이고 닮음비가 ${p}:${q}이다. ${s}=${p*k} cm일 때, ${S}의 길이는 몇 cm인지 구하시오.`,answer:q*k,
    explain:`${s}:${S}=${p}:${q}이므로 ${S}=${p*k}×${q}÷${p}=${q*k} cm입니다.`,concept:RATIO,d:1,
    wrongs:[W(p*k+q-p,MC.add,'additive'),W(p*k*p/q,MC.inv,'inverted'),W(p*k,MC.same,'same-length'),W(p*k*q*q/(p*p),MC.power,'area-ratio-for-length')]}));}
// ── 대응각 ───────────────────────────────────────────────────
for(const t2 of PERMS)for(let x=30;x<=100;x+=5)for(let y=35;y<=110;y+=5){const z=180-x-y;if(z<10||x===y||z===x||z===y)continue;
  const m=mapOf(t2),V=m.C,posV='ABC'['DEF'.indexOf(V)],ang={A:x,B:y,C:z};
  add('angle',num({kind:'sim-angle',params:{t2,A:x,B:y,ask:V},prompt:`△ABC∽△${t2}이고 ∠A=${x}°, ∠B=${y}°일 때, ∠${V}의 크기는 몇 도인지 구하시오.`,answer:z,
    explain:`∠${V}의 대응각은 ∠C이고 ∠C=180°−${x}°−${y}°=${z}°이므로 ∠${V}=${z}°입니다.`,concept:SIM,d:2,hi:179,
    wrongs:[W(ang[posV],MC.pos,'by-position'),W(x+y,MC.sum,'exterior-angle'),W(180-x,MC.sum,'one-angle-subtracted'),W(180-y,MC.sum,'one-angle-subtracted'),W(x,MC.pos,'given-angle-copied'),W(y,MC.pos,'given-angle-copied')]}));
  if((x+y)%10===0)add('angle',num({kind:'sim-angle-mixed',params:{t2,A:x,B:y},prompt:`△ABC∽△${t2}이고 ∠A=${x}°, ∠${m.B}=${y}°일 때, ∠C의 크기는 몇 도인지 구하시오.`,answer:z,
    explain:`∠${m.B}의 대응각은 ∠B이므로 ∠B=${y}°, ∠C=180°−${x}°−${y}°=${z}°입니다.`,concept:SIM,d:2,hi:179,
    wrongs:[W(y,MC.pos,'given-angle-copied'),W(x+y,MC.sum,'exterior-angle'),W(180-x,MC.sum,'one-angle-subtracted'),W(180-y,MC.sum,'one-angle-subtracted')]}));}
// ── 둘레·넓이·부피 ──────────────────────────────────────────
for(const [p,q] of RAT)for(let k=2;q*k<=120;k++){if(p*k<6)continue;add('perim',num({kind:'sim-perimeter',params:{p,q,P:p*k},prompt:`△ABC∽△DEF이고 닮음비가 ${p}:${q}이다. △ABC의 둘레의 길이가 ${p*k} cm일 때, △DEF의 둘레의 길이는 몇 cm인지 구하시오.`,answer:q*k,
  explain:`둘레의 길이의 비는 닮음비와 같은 ${p}:${q}이므로 ${p*k}×${q}÷${p}=${q*k} cm입니다.`,concept:SIM,d:2,
  wrongs:[W(p*k*q*q/(p*p),MC.power,'area-ratio-for-perimeter'),W(p*k,MC.same,'same-perimeter'),W(p*k+q-p,MC.add,'additive'),W(p*k*p/q,MC.inv,'inverted')]}));}
for(let a=3;a<=12;a++)for(let b=a;b<=14;b++)for(let c=b;c<a+b&&c<=16;c++){for(const [p,q] of [[1,2],[2,3],[3,2],[2,1],[3,4],[4,3]]){
  if(a%p||b%p||c%p||a===c)continue;const per=(a+b+c)*q/p;if(!Number.isInteger(per)||per>150)continue;const de=a*q/p;if((a*7+b*3+c+p)%4)continue;
  add('perim3',num({kind:'sim-sss-perimeter',params:{a,b,c,de},prompt:`△ABC∽△DEF이고 AB=${a} cm, BC=${b} cm, AC=${c} cm, DE=${de} cm일 때, △DEF의 둘레의 길이는 몇 cm인지 구하시오.`,answer:per,
    explain:`닮음비 AB:DE=${rt(a,de,p,q)}이므로 △DEF의 둘레는 (${a}+${b}+${c})×${q}÷${p}=${per} cm입니다.`,concept:SIM,d:3,
    wrongs:[W(a+b+c+3*(de-a),MC.add,'same-difference-each-side'),W(a+b+c+de-a,MC.add,'additive-once'),W(a+b+c,MC.same,'same-perimeter'),W((a+b+c)*p/q,MC.inv,'inverted'),W(de+b+c,MC.pos,'one-side-replaced')]}));}}
const FIG=[['△ABC∽△DEF','△ABC','△DEF'],['□ABCD∽□EFGH','□ABCD','□EFGH']];
for(const [p,q] of RAT)for(let k=1;k<=60;k++)for(const [rel,f1,f2] of FIG){const small=p*p*k,big=q*q*k;if(big>300||small>300||small<3)continue;if((k+p)%2&&rel.startsWith('□'))continue;
  add('area',num({kind:'sim-area',params:{fig:f1,p,q,S:small},prompt:`${rel}이고 닮음비가 ${p}:${q}이다. ${f1}의 넓이가 ${small} cm²일 때, ${f2}의 넓이는 몇 cm²인지 구하시오.`,answer:big,
    explain:`넓이의 비는 ${p}²:${q}²=${p*p}:${q*q}이므로 ${f2}의 넓이는 ${small}×${q*q}÷${p*p}=${big} cm²입니다.`,concept:AREA,d:3,
    wrongs:[W(small*q/p,MC.linear,'linear'),W(small*q**3/p**3,MC.power,'cubed'),W(small*p*p/(q*q),MC.inv,'inverted'),W(small,MC.same,'same-area')]}));}
for(const [p,q] of RAT)for(let u=2;u<=5;u++)for(let k=1;k<=30;k++){const small=p*p*k,big=q*q*k;if(big>300||small<2)continue;if((u+k)%2)continue;
  add('area4',num({kind:'sim-area-sides',params:{AB:p*u,DE:q*u,S:small},prompt:`△ABC∽△DEF이고 AB=${p*u} cm, DE=${q*u} cm이다. △ABC의 넓이가 ${small} cm²일 때, △DEF의 넓이는 몇 cm²인지 구하시오.`,answer:big,
    explain:`닮음비는 ${rt(p*u,q*u,p,q)}, 넓이의 비는 ${p*p}:${q*q}이므로 ${small}×${q*q}÷${p*p}=${big} cm²입니다.`,concept:AREA,d:4,
    wrongs:[W(small*q/p,MC.linear,'linear'),W(small*q**3/p**3,MC.power,'cubed'),W(small*p*p/(q*q),MC.inv,'inverted'),W(small+q*u-p*u,MC.add,'additive')]}));}
for(const [p,q] of RAT.filter(([p,q])=>p<q))for(let k=1;k<=20;k++)for(let m=1;m<=9;m++){if((k+m)%3||p*p*k<2||p*m<2)continue;
  add('area4',num({kind:'sim-area-inverse',params:{S1:p*p*k,S2:q*q*k,side:p*m},prompt:`넓이가 각각 ${p*p*k} cm², ${q*q*k} cm²인 닮은 두 삼각형이 있다. 작은 삼각형의 한 변의 길이가 ${p*m} cm일 때, 이에 대응하는 큰 삼각형의 변의 길이는 몇 cm인지 구하시오.`,answer:q*m,
    explain:`넓이의 비 ${p*p*k}:${q*q*k}=${p*p}:${q*q}=${p}²:${q}²이므로 닮음비는 ${p}:${q}, 대응변은 ${p*m}×${q}÷${p}=${q*m} cm입니다.`,concept:AREA,d:4,
    wrongs:[W(q*q*m/p,MC.sq,'area-ratio-as-length'),W(p*m*p/q,MC.inv,'inverted'),W(p*m+q-p,MC.add,'additive'),W(p*m+(q*q-p*p)*k,MC.add,'area-difference-added')]}));}
const SOLIDS=['사각뿔','삼각기둥','직육면체','오각기둥','사각기둥','원기둥','원뿔'];
for(const [p,q] of RAT)for(let k=1;k<=60;k++){const vs=p**3*k,vb=q**3*k;if(vb>1000||vs>1000||vs<2)continue;const sol=SOLIDS[(p+q+k)%SOLIDS.length];
  add('vol',num({kind:'sim-volume',params:{p,q,V:vs,solid:sol},prompt:`닮은 두 ${sol} (가), (나)의 닮음비가 ${p}:${q}이다. (가)의 부피가 ${vs} cm³일 때, (나)의 부피는 몇 cm³인지 구하시오.`,answer:vb,
    explain:`부피의 비는 ${p}³:${q}³=${p**3}:${q**3}이므로 (나)의 부피는 ${vs}×${q**3}÷${p**3}=${vb} cm³입니다.`,concept:VOL,d:4,
    wrongs:[W(vs*q/p,MC.linear,'linear'),W(vs*q*q/(p*p),MC.power,'squared'),W(vs*p**3/q**3,MC.inv,'inverted'),W(vs,MC.same,'same-volume')]}));}
for(const [p,q] of RAT)for(let k=1;k<=60;k++){const s1=p*p*k,s2=q*q*k;if(s2>400||s1>400||s1<2||(k+p)%2)continue;const sol=SOLIDS[(p*q+k)%SOLIDS.length];
  add('vol',num({kind:'sim-surface',params:{p,q,S:s1,solid:sol},prompt:`닮은 두 ${sol} (가), (나)의 닮음비가 ${p}:${q}이다. (가)의 겉넓이가 ${s1} cm²일 때, (나)의 겉넓이는 몇 cm²인지 구하시오.`,answer:s2,
    explain:`겉넓이의 비는 ${p}²:${q}²=${p*p}:${q*q}이므로 (나)의 겉넓이는 ${s1}×${q*q}÷${p*p}=${s2} cm²입니다.`,concept:AREA,d:4,
    wrongs:[W(s1*q/p,MC.linear,'linear'),W(s1*q**3/p**3,MC.power,'cubed'),W(s1*p*p/(q*q),MC.inv,'inverted'),W(s1,MC.same,'same-area')]}));}
// ── 직각삼각형의 닮음 (AD²=BD×DC, AB²=BD×BC, AC²=CD×CB) ─────────────
const rtStem='∠A=90°인 직각삼각형 ABC의 꼭짓점 A에서 BC에 내린 수선의 발을 D라고 하자.';
const isq=n=>{const r=Math.round(Math.sqrt(n));return r*r===n?r:NaN;};
for(let a=2;a<=40;a++)for(let b=2;b<=60;b++){const h=isq(a*b);if(!h||a===b)continue;
  add('rt3',num({kind:'rt-altitude',params:{BD:a,DC:b},prompt:`${rtStem} BD=${a} cm, DC=${b} cm일 때, AD의 길이는 몇 cm인지 구하시오.`,answer:h,
    explain:`△DBA∽△DAC (AA 닮음)이므로 AD²=BD×DC=${a}×${b}=${a*b}, AD>0이므로 AD=${h} cm입니다.`,concept:RT,d:3,
    wrongs:[W((a+b)/2,MC.add,'average'),W(a*b,MC.root,'no-root'),W(a+b,MC.pos,'uses-bc'),W(Math.abs(b-a),MC.add,'difference')]}));
  if(h<=40)add('rt3',num({kind:'rt-segment',params:{AD:h,BD:a},prompt:`${rtStem} AD=${h} cm, BD=${a} cm일 때, DC의 길이는 몇 cm인지 구하시오.`,answer:b,
    explain:`AD²=BD×DC이므로 ${h*h}=${a}×DC, DC=${b} cm입니다.`,concept:RT,d:3,
    wrongs:[W(2*h-a,MC.add,'arithmetic-mean'),W(h*h,MC.root,'bd-not-divided'),W(h*h-a*a,MC.pyth,'pythagoras-confused'),W(h,MC.same,'same')]}));}
for(let a=2;a<=50;a++)for(let c=a+1;c<=80;c++){const s=isq(a*c);if(!s||s>40)continue;const other=c-a;
  add('rt4',num({kind:'rt-leg-ab',params:{BD:a,BC:c},prompt:`${rtStem} BD=${a} cm, BC=${c} cm일 때, AB의 길이는 몇 cm인지 구하시오.`,answer:s,
    explain:`△DBA∽△ABC (AA 닮음)이므로 AB²=BD×BC=${a}×${c}=${a*c}, AB>0이므로 AB=${s} cm입니다.`,concept:RT,d:4,
    wrongs:[W(isq(a*other),MC.pos,'uses-dc'),W((a+c)/2,MC.add,'average'),W(a*c,MC.root,'no-root'),W(other,MC.pos,'dc-instead')]}));
  add('rt4',num({kind:'rt-leg-ac',params:{CD:a,BC:c},prompt:`${rtStem} CD=${a} cm, BC=${c} cm일 때, AC의 길이는 몇 cm인지 구하시오.`,answer:s,
    explain:`△DAC∽△ABC (AA 닮음)이므로 AC²=CD×CB=${a}×${c}=${a*c}, AC>0이므로 AC=${s} cm입니다.`,concept:RT,d:4,
    wrongs:[W(isq(a*other),MC.pos,'uses-bd'),W((a+c)/2,MC.add,'average'),W(a*c,MC.root,'no-root'),W(other,MC.pos,'bd-instead')]}));}
for(let s=3;s<=24;s++)for(let a=2;a<s;a++){if((s*s)%a)continue;const c=s*s/a;if(c>150||c<=a)continue;
  add('rt4',num({kind:'rt-hyp',params:{AB:s,BD:a},prompt:`${rtStem} AB=${s} cm, BD=${a} cm일 때, BC의 길이는 몇 cm인지 구하시오.`,answer:c,
    explain:`AB²=BD×BC이므로 ${s*s}=${a}×BC, BC=${c} cm입니다.`,concept:RT,d:4,
    wrongs:[W(2*s-a,MC.add,'arithmetic-mean'),W(s*s,MC.root,'bd-not-divided'),W(c-a,MC.pos,'dc-instead-of-bc'),W(s+a,MC.add,'sum')]}));}
// ── 겹쳐진 삼각형의 닮음 (SAS·AA) ───────────────────────────────
for(let s=1;s<=5;s++)for(let t=s+1;t<=7;t++){if(gcd(s,t)!==1)continue;for(let k=1;k*t*t<=80;k++){const a=k*s*s,m=k*s*t,b=k*t*t;if(a<2)continue;
  for(let c=t*Math.floor((b-m)/t+1);c<b+m;c+=t){if(c<=b-m)continue;const cd=c*s/t;if(cd<2||cd>100)continue;
    add('overlap',num({kind:'sas-overlap',params:{AD:a,AC:m,AB:b,BC:c},prompt:`△ABC에서 변 AB 위의 점 D에 대하여 AD=${a} cm, AC=${m} cm, AB=${b} cm, BC=${c} cm일 때, CD의 길이는 몇 cm인지 구하시오.`,answer:cd,
      explain:`AD:AC=AC:AB=${s}:${t}이고 ∠A는 공통이므로 △ADC∽△ACB (SAS 닮음), CD=${c}×${s}÷${t}=${cd} cm입니다.`,concept:COND,d:4,
      wrongs:[W(c*a/b,MC.pos,'wrong-pair'),W(c-(b-m),MC.add,'additive'),W(c*t/s,MC.inv,'inverted'),W(c,MC.same,'same')]}));}
  if(m<=60&&m>=2)add('overlap',num({kind:'aa-overlap-ac',params:{AD:a,AB:b},prompt:`△ABC에서 변 AB 위의 점 D에 대하여 ∠ACD=∠ABC이고 AD=${a} cm, AB=${b} cm일 때, AC의 길이는 몇 cm인지 구하시오.`,answer:m,
    explain:`∠A는 공통, ∠ACD=∠ABC이므로 △ACD∽△ABC (AA 닮음), AC²=AD×AB=${a*b}, AC>0이므로 AC=${m} cm입니다.`,concept:COND,d:4,
    wrongs:[W((a+b)/2,MC.add,'average'),W(b-a,MC.pos,'db'),W(a*b,MC.root,'no-root'),W(a+b,MC.add,'sum')]}));
  if(b>=2)add('overlap',num({kind:'aa-overlap-ab',params:{AD:a,AC:m},prompt:`△ABC에서 변 AB 위의 점 D에 대하여 ∠ACD=∠ABC이고 AD=${a} cm, AC=${m} cm일 때, AB의 길이는 몇 cm인지 구하시오.`,answer:b,
    explain:`△ACD∽△ABC (AA 닮음)이므로 AD:AC=AC:AB, ${a}:${m}=${m}:AB, AB=${b} cm입니다.`,concept:COND,d:4,
    wrongs:[W(2*m-a,MC.add,'additive'),W(m*m,MC.root,'not-divided'),W(b-a,MC.pos,'db-instead'),W(a+m,MC.add,'sum')]}));}}

// ── 비 — 「가장 간단한 자연수의 비로 나타내시오」 ─────────────────────
const SIMPLE='가장 간단한 자연수의 비로 나타내시오.';
// 닮음비: one DEF side is the true image of the given ABC side, a second one only sits at the same position.
for(const t2 of PERMS.slice(1))for(const [p,q] of RAT)for(const s of SIDES){const S=sideOf(s,t2),X=sideOf(s,'DEF');if(X===S)continue;
  for(let u=1;u<=5;u++)for(const w of [1,2,3,4,5]){if(w===u||(u*7+w*3+p+q+PERMS.indexOf(t2))%5)continue;const a=p*u,b=q*u,c=q*w;
    add('rLen',rat({kind:'r-sim-ratio',params:{t2,s,a,b,X,c},prompt:`△ABC∽△${t2}이고 ${s}=${a} cm, ${S}=${b} cm, ${X}=${c} cm일 때, △ABC와 △${t2}의 닮음비를 ${SIMPLE}`,num:p,den:q,
      explain:`${s}의 대응변은 ${S}이므로 닮음비는 ${rt(a,b,p,q)}입니다.`,concept:RATIO,d:2,
      wrongs:[W(R(a,c),MC.pos,'by-position'),W(R(q,p),MC.inv,'inverted'),W(R(p*p,q*q),MC.power,'squared'),W(R(c,a),MC.pos,'by-position-inverted')]}));}}
const SHAPES=['삼각형','사각형','오각형','육각형','평행사변형'];
for(const [p,q] of RAT)for(const sh of SHAPES){if((p*3+q+sh.length)%2)continue;
  add('rArea',rat({kind:'r-area-ratio',params:{p,q,shape:sh},prompt:`닮은 두 ${sh} (가), (나)의 닮음비가 ${p}:${q}일 때, (가)와 (나)의 넓이의 비를 ${SIMPLE}`,num:p*p,den:q*q,keepFirst:true,
    explain:`넓이의 비는 닮음비의 제곱이므로 ${p}²:${q}²=${p*p}:${q*q}입니다.`,concept:AREA,d:3,
    wrongs:[W(R(p,q),MC.linear,'linear'),W(R(p**3,q**3),MC.power,'cubed'),W(R(q*q,p*p),MC.inv,'inverted')]}));}
for(const [p,q] of RAT)for(let u=1;u<=6;u++){if(p===1&&q===1||(u*p+q)%2)continue;
  add('rArea',rat({kind:'r-area-sides',params:{AB:p*u,DE:q*u},prompt:`△ABC∽△DEF이고 AB=${p*u} cm, DE=${q*u} cm일 때, △ABC와 △DEF의 넓이의 비를 ${SIMPLE}`,num:p*p,den:q*q,keepFirst:true,
    explain:`닮음비가 ${rt(p*u,q*u,p,q)}이므로 넓이의 비는 ${p}²:${q}²=${p*p}:${q*q}입니다.`,concept:AREA,d:3,
    wrongs:[W(R(p,q),MC.linear,'linear'),W(R(p**3,q**3),MC.power,'cubed'),W(R(q*q,p*p),MC.inv,'inverted')]}));}
for(const [p,q] of RAT)for(const sol of SOLIDS){if((p+q+sol.length)%2)continue;
  add('rVol',rat({kind:'r-volume-ratio',params:{p,q,solid:sol},prompt:`닮은 두 ${sol} (가), (나)의 닮음비가 ${p}:${q}일 때, (가)와 (나)의 부피의 비를 ${SIMPLE}`,num:p**3,den:q**3,keepFirst:true,
    explain:`부피의 비는 닮음비의 세제곱이므로 ${p}³:${q}³=${p**3}:${q**3}입니다.`,concept:VOL,d:4,
    wrongs:[W(R(p,q),MC.linear,'linear'),W(R(p*p,q*q),MC.power,'squared'),W(R(q**3,p**3),MC.inv,'inverted')]}));
  if((p*q+sol.length)%2===0)add('rVol',rat({kind:'r-surface-ratio',params:{p,q,solid:sol},prompt:`닮은 두 ${sol} (가), (나)의 닮음비가 ${p}:${q}일 때, (가)와 (나)의 겉넓이의 비를 ${SIMPLE}`,num:p*p,den:q*q,keepFirst:true,
    explain:`겉넓이의 비는 닮음비의 제곱이므로 ${p}²:${q}²=${p*p}:${q*q}입니다.`,concept:AREA,d:4,
    wrongs:[W(R(p,q),MC.linear,'linear'),W(R(p**3,q**3),MC.power,'cubed'),W(R(q*q,p*p),MC.inv,'inverted')]}));}
for(const [p,q] of RAT)for(const k of [1,2,3,5]){const A1=p*p*k,A2=q*q*k;if(p===1&&k===1)continue;
  add('rVol',rat({kind:'r-ratio-from-area',params:{A1,A2},prompt:`넓이가 각각 ${A1} cm², ${A2} cm²인 닮은 두 삼각형 (가), (나)의 닮음비를 ${SIMPLE}`,num:p,den:q,
    explain:`넓이의 비 ${A1}:${A2}=${p*p}:${q*q}=${p}²:${q}²이므로 닮음비는 ${p}:${q}입니다.`,concept:AREA,d:4,
    wrongs:[W(R(A1,A2),MC.sq,'area-ratio-as-length'),W(R(q,p),MC.inv,'inverted'),W(R(A2,A1),MC.inv,'area-ratio-inverted')]}));}
for(const [p,q] of RAT){if(Math.max(p,q)>5)continue;for(const k of [1,2]){const V1=p**3*k,V2=q**3*k;const sol=SOLIDS[(p+2*q+k)%SOLIDS.length];
  add('rVol',rat({kind:'r-ratio-from-volume',params:{V1,V2,solid:sol},prompt:`부피가 각각 ${V1} cm³, ${V2} cm³인 닮은 두 ${sol} (가), (나)의 닮음비를 ${SIMPLE}`,num:p,den:q,
    explain:`부피의 비 ${V1}:${V2}=${p**3}:${q**3}=${p}³:${q}³이므로 닮음비는 ${p}:${q}입니다.`,concept:VOL,d:4,
    wrongs:[W(R(V1,V2),MC.linear,'volume-ratio-as-length'),W(R(p*p,q*q),MC.power,'squared'),W(R(q,p),MC.inv,'inverted')]}));}
  const sol=SOLIDS[(3*p+q)%SOLIDS.length];
  add('rVol',rat({kind:'r-area-from-volume',params:{p,q,solid:sol},prompt:`닮은 두 ${sol} (가), (나)의 부피의 비가 ${p**3}:${q**3}일 때, (가)와 (나)의 겉넓이의 비를 ${SIMPLE}`,num:p*p,den:q*q,keepFirst:true,
    explain:`${p**3}:${q**3}=${p}³:${q}³이므로 닮음비는 ${p}:${q}, 겉넓이의 비는 ${p}²:${q}²=${p*p}:${q*q}입니다.`,concept:AREA,d:4,
    wrongs:[W(R(p**3,q**3),MC.linear,'volume-ratio-as-area'),W(R(p,q),MC.power,'similarity-ratio-as-area'),W(R(q*q,p*p),MC.inv,'inverted')]}));}
// 「몇 배」 — the perimeter comparison reads naturally as a multiple.
for(let lo=2;lo<=7;lo++)for(let hi=lo+1;hi<=7;hi++){if(gcd(lo,hi)!==1)continue;for(const k of [1,2,3]){
  add('rVol',rat({kind:'f-perimeter-from-area',as:'frac',params:{A1:lo*lo*k,A2:hi*hi*k},prompt:`넓이가 각각 ${lo*lo*k} cm², ${hi*hi*k} cm²인 닮은 두 삼각형에서 큰 삼각형의 둘레의 길이는 작은 삼각형의 둘레의 길이의 몇 배인지 기약분수로 구하시오.`,num:hi,den:lo,
    explain:`넓이의 비 ${lo*lo}:${hi*hi}=${lo}²:${hi}²이므로 닮음비는 ${lo}:${hi}, 둘레는 {frac:${hi}/${lo}}배입니다.`,concept:AREA,d:4,
    wrongs:[W(R(hi*hi,lo*lo),MC.sq,'area-ratio-as-length'),W(R(lo,hi),MC.inv,'inverted'),W(R(lo*lo,hi*hi),MC.inv,'area-ratio-inverted')]}));}}

// ── 선택형 ──────────────────────────────────────────────────
const CONDS=['SSS 닮음','SAS 닮음','AA 닮음','닮음이 아니다'];
const condChoice=(kind,data,ans,d,params)=>add(d===2?'cCond2':'cCond3',choice({kind,params,stem:`△ABC와 △DEF에서 ${data}일 때, 두 삼각형이 닮음인지 판별하여 알맞은 것을 고르시오.`,correct:ans,
  wrongs:CONDS.filter(c=>c!==ans).map(c=>({text:c,tag:c==='닮음이 아니다'?MC.look:ans==='닮음이 아니다'?(c==='SSS 닮음'?MC.add:MC.look):MC.pos})),explain:ans==='닮음이 아니다'?'대응하는 세 변의 길이의 비가 같지 않거나 대응하는 두 각의 크기가 같지 않으므로 닮음이 아닙니다.':`${ans}입니다.`,concept:COND,d}));
const tri=[];for(let a=3;a<=9;a++)for(let b=a+1;b<=10;b++)for(let c=b+1;c<a+b&&c<=12;c++)tri.push([a,b,c]);
tri.forEach(([a,b,c],i)=>{if(i%4)return;const k=[2,3][i%2],o=[[1,2,0],[2,0,1],[0,2,1]][i%3];const big=[a*k,b*k,c*k];
  condChoice('sim-sss',`AB=${a} cm, BC=${b} cm, AC=${c} cm, DE=${big[o[0]]} cm, EF=${big[o[1]]} cm, DF=${big[o[2]]} cm`,'SSS 닮음',2,{a,b,c,k});
  const d=[1,2][i%2],add2=[a+d,b+d,c+d];
  condChoice('sim-sss-not',`AB=${a} cm, BC=${b} cm, AC=${c} cm, DE=${add2[0]} cm, EF=${add2[1]} cm, DF=${add2[2]} cm`,'닮음이 아니다',2,{a,b,c,d});});
for(let x=30;x<=120;x+=15)for(const [a,b,k] of [[3,4,2],[4,6,2],[5,8,3],[6,9,2],[4,7,3],[5,6,2]]){if((x/15+a)%2)continue;
  const pairs=[['AB','AC','∠A','DE','DF','∠D'],['BA','BC','∠B','ED','EF','∠E'],['CA','CB','∠C','FD','FE','∠F']][(a+k)%3];
  const [s1,s2,A1,t1,t2,A2]=pairs.map(s=>s.length===2&&!s.startsWith('∠')?s.split('').sort().join(''):s);
  condChoice('sim-sas',`${s1}=${a} cm, ${s2}=${b} cm, ${A1}=${x}°, ${t1}=${a*k} cm, ${t2}=${b*k} cm, ${A2}=${x}°`,'SAS 닮음',3,{a,b,k,x});}
for(let x=30;x<=90;x+=10)for(let y=35;y<=95;y+=10){const z=180-x-y;if(z<=0||x===y||y===z||x===z)continue;
  condChoice('sim-aa',`∠A=${x}°, ∠B=${y}°, ∠D=${x}°, ∠F=${z}°`,'AA 닮음',3,{x,y,z});
  if((x+y)%20===5&&y!==z+5)condChoice('sim-aa-not',`∠A=${x}°, ∠B=${y}°, ∠D=${x}°, ∠E=${z+5}°`,'닮음이 아니다',3,{x,y,w:z+5});}
const ALWAYS=['두 정사각형','두 원','두 정삼각형','두 직각이등변삼각형','두 정육면체','두 구','중심각의 크기가 같은 두 부채꼴'];
const NOT=['두 직사각형','두 마름모','두 이등변삼각형','두 직각삼각형','두 평행사변형','두 원기둥','두 사다리꼴','두 부채꼴','두 원뿔','두 직육면체'];
ALWAYS.forEach((c,i)=>{for(let j=0;j<2;j++){const ws=[0,1,2].map(t=>NOT[(i*3+j*5+t*3)%NOT.length]);if(new Set(ws).size<3)continue;
  add('cAlways',choice({kind:'always-similar',params:{correct:c,wrong:ws},style:'labels',stem:'다음 중 항상 닮은 도형인 것을 고르시오.',correct:c,wrongs:ws.map((w,k)=>({text:w,tag:k%2?MC.look:MC.add})),explain:L=>`${L}. ${josa(c,'은','는')} 모양이 같고 크기만 다를 수 있어 항상 닮은 도형입니다.`,concept:SIM,d:1}));}});
NOT.forEach((c,i)=>{const ws=[0,1,2].map(t=>ALWAYS[(i+t*2)%ALWAYS.length]);if(new Set(ws).size<3)return;
  add('cAlways',choice({kind:'not-always-similar',params:{correct:c,wrong:ws},style:'labels',stem:'다음 중 항상 닮은 도형이라고 할 수 없는 것을 고르시오.',correct:c,wrongs:ws.map((w,k)=>({text:w,tag:k%2?MC.look:MC.add})),explain:L=>`${L}. ${josa(c,'은','는')} 모양이 달라질 수 있어 항상 닮은 도형이라고 할 수 없습니다.`,concept:SIM,d:1}));});
for(const t2 of PERMS.slice(1)){const m=mapOf(t2);
  for(const s of SIDES){const c=sideOf(s,t2);const pos=sideOf(s,'DEF');const ws=['DE','EF','DF'].filter(x=>x!==c).map(x=>({text:x,tag:x===pos?MC.pos:MC.look}));
    add('cCorr',choice({kind:'corr-side',params:{t2,side:s},stem:`△ABC∽△${t2}일 때, 변 ${s}에 대응하는 변을 고르시오.`,correct:c,wrongs:[...ws,{text:'알 수 없다',tag:MC.sym}],explain:`기호의 순서가 대응 순서이므로 ${s[0]}↔${m[s[0]]}, ${s[1]}↔${m[s[1]]}, ${s}의 대응변은 ${c}입니다.`,concept:SIM,d:1}));}
  for(const v of ['A','B','C']){const c=m[v],pos={A:'D',B:'E',C:'F'}[v];const ws=['D','E','F'].filter(x=>x!==c).map(x=>({text:`∠${x}`,tag:x===pos?MC.pos:MC.look}));
    add('cCorr',choice({kind:'corr-angle',params:{t2,vertex:v},stem:`△ABC∽△${t2}일 때, ∠${v}에 대응하는 각을 고르시오.`,correct:`∠${c}`,wrongs:[...ws,{text:'알 수 없다',tag:MC.sym}],explain:`기호의 순서가 대응 순서이므로 ∠${v}의 대응각은 ∠${c}입니다.`,concept:SIM,d:1}));}}

const intro=num({kind:'sim-ratio-given',params:{p:1,q:2,side:'BC',len:4},prompt:'△ABC∽△DEF이고 닮음비가 1:2이다. BC=4 cm일 때, EF의 길이는 몇 cm인지 구하시오.',answer:8,explain:'BC:EF=1:2이므로 EF=4×2=8 cm입니다.',concept:RATIO,d:1,
  wrongs:[W(5,MC.add,'additive'),W(2,MC.inv,'inverted'),W(4,MC.same,'same-length'),W(16,MC.power,'area-ratio-for-length')]});
const P=pools;
if(process.env.POOLS)console.log(Object.fromEntries(Object.entries(P).map(([k,v])=>[k,[v.length,v.filter(Boolean).length]])));
writeGeoPack({id:'m2s2-u3',title:'도형의 닮음',standards:['[9수03-12]','[9수03-13]'],intro,groups:[
  [P.side1,48],[P.cAlways,14],[P.cCorr,22],
  [P.side2,20],[P.angle,20],[P.perim,16],[P.rLen,18],[P.cCond2,14],
  [P.perim3,12],[P.area,22],[P.rt3,20],[P.rArea,18],[P.cCond3,16],
  [P.area4,16],[P.vol,18],[P.rt4,16],[P.overlap,18],[P.rVol,20]]});
