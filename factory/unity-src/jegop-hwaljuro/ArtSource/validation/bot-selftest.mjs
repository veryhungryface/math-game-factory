import fs from 'node:fs';

// JegopRules.BuildCatalog/PickSession의 정수 모델을 그대로 옮긴 무뇌 봇 자가 테스트.
// 첫 제출 전에는 어떤 패널도 정오 신호가 없고, 답 공간은 unordered pair 10개다.
const catalog=[]; const seen=new Set(); let serial=0;
const pairs=[[0,1],[0,2],[0,3],[0,4],[1,2],[1,3],[1,4],[2,3],[2,4],[3,4]];
for(let m=2;m<=32&&catalog.length<420;m++) for(let n=1;n<m&&catalog.length<420;n++){
  const a0=m*m-n*n,b0=2*m*n,c0=m*m+n*n,lo=Math.min(a0,b0),hi=Math.max(a0,b0);
  for(let scale=1;scale<=7&&catalog.length<420;scale++){
    const a=lo*scale,b=hi*scale,c=c0*scale;if(c<13||c>600)continue;
    const key=`${a}:${b}:${c}`;if(seen.has(key))continue;seen.add(key);
    const band=serial%7<2?1:serial%7<5?2:3, answerPair=serial%10;
    const sides=Array(5),[ci,cj]=pairs[answerPair];sides[ci]=a;sides[cj]=b;
    const wrong=[];const used=new Set([a,b]),low=Math.min(a,b),high=Math.max(a,b);
    const add=v=>{if(wrong.length>=3||v<=0||v>=c||used.has(v)||low+v<=c)return;for(const s of used)if(s*s+v*v===c*c)return;used.add(v);wrong.push(v);};
    for(let d=1;d<high&&wrong.length<1;d++)add(high-d);
    for(let d=1;high+d<c&&wrong.length<2;d++)add(high+d);
    for(let d=1;d<c&&wrong.length<3;d++){add(high-d);add(high+d);}
    if(wrong.length!==3)throw new Error(`distractors ${key}: ${wrong}`);
    let q=0;for(let i=0;i<5;i++)if(sides[i]==null)sides[i]=wrong[q++];
    let valid=0;for(let i=0;i<5;i++)for(let j=i+1;j<5;j++)if(sides[i]**2+sides[j]**2===c**2)valid++;
    if(valid!==1)throw new Error(`unique-pair ${key}: ${valid}`);
    for(let i=0;i<5;i++)for(let j=i+1;j<5;j++)if(sides[i]+sides[j]<=c)throw new Error(`triangle-choice ${key}: ${sides[i]},${sides[j]}`);
    const ordered=[0,1,2,3,4].sort((x,y)=>sides[x]-sides[y]);
    const policyPair=(x,y)=>sides[x]**2+sides[y]**2===c**2;
    if(policyPair(ordered[0],ordered[1]))throw new Error(`smallest-two ${key}`);
    if(policyPair(ordered[3],ordered[4]))throw new Error(`largest-two ${key}`);
    catalog.push({a,b,c,band,answerPair,sides});serial++;
  }
}
if(catalog.length<300)throw new Error(`bank ${catalog.length}`);
const pools={};for(const b of [1,2,3])pools[b]=catalog.filter(p=>p.band===b&&p.c<=65);
const bands=[1,1,2,2,2,3,3];const mod=(x,n)=>((x%n)+n)%n;
function pick(solved,runArg){
  const pool=pools[bands[Math.min(solved,6)]],base=pool[mod(runArg*43+solved*71,pool.length)],answerPair=mod(runArg+solved*3,10);
  const sides=Array(5),[ci,cj]=pairs[answerPair];sides[ci]=base.a;sides[cj]=base.b;
  const wrong=base.sides.filter(v=>v!==base.a&&v!==base.b);let w=0;
  for(let i=0;i<5;i++)if(sides[i]==null)sides[i]=wrong[w++];
  return {...base,answerPair,sides};
}
function xorshift(seed){let x=seed|0;return()=>{x^=x<<13;x^=x>>>17;x^=x<<5;return(x>>>0)/4294967296;};}

function simulate(name,choose){
  let firstAttempts=0,firstCorrect=0,completed=0,totalProgress=0;
  for(let game=1;game<=200;game++){
    let solved=0,lives=3,action=0,p=pick(0,game),first=true;
    for(let guard=0;guard<80&&lives>0&&solved<7;guard++){
      const guess=choose({game,action,solved,answer:p.answerPair});action++;
      if(guess==null)break;
      const ok=guess===p.answerPair;
      if(first){firstAttempts++;if(ok)firstCorrect++;first=false;}
      if(ok){solved++;if(solved<7){p=pick(solved,game+solved*29);first=true;}}
      else lives--;
    }
    if(solved===7&&firstCorrect>=0)completed++;
    totalProgress+=solved;
  }
  return {bot:name,games:200,first_attempts:firstAttempts,first_correct:firstCorrect,
    first_attempt_rate:firstAttempts?firstCorrect/firstAttempts:0,completion_rate:completed/200,progress:totalProgress};
}

const random=xorshift(0x5eed2026);let bag=[],cursor=0;
function balancedRandom(){if(cursor>=bag.length){bag=Array.from({length:10},(_,i)=>i);for(let i=9;i>0;i--){const j=Math.floor(random()*(i+1));[bag[i],bag[j]]=[bag[j],bag[i]];}cursor=0;}return bag[cursor++];}
const results=[
  simulate('repeat',()=>0),
  simulate('cycle',({action})=>action%10),
  simulate('random',()=>balancedRandom()),
  simulate('smallest-two',({game,solved})=>{
    const p=pick(solved,game+solved*29),order=[0,1,2,3,4].sort((x,y)=>p.sides[x]-p.sides[y]);
    return pairs.findIndex(([i,j])=>i===Math.min(order[0],order[1])&&j===Math.max(order[0],order[1]));
  }),
  simulate('largest-two',({game,solved})=>{
    const p=pick(solved,game+solved*29),order=[0,1,2,3,4].sort((x,y)=>p.sides[x]-p.sides[y]);
    return pairs.findIndex(([i,j])=>i===Math.min(order[3],order[4])&&j===Math.max(order[3],order[4]));
  }),
  simulate('target-nearest',({game,solved})=>{
    const p=pick(solved,game+solved*29),order=[0,1,2,3,4].sort((x,y)=>Math.abs(p.c-p.sides[x])-Math.abs(p.c-p.sides[y]));
    return pairs.findIndex(([i,j])=>i===Math.min(order[0],order[1])&&j===Math.max(order[0],order[1]));
  }),
  simulate('idle',()=>null)
];
const chance=0.10;
for(const r of results)r.pass=r.first_attempt_rate<=chance&&(r.bot!=='idle'||r.progress===0);
const out={generated_at:new Date().toISOString(),catalog_count:catalog.length,play_pool_counts:Object.fromEntries(Object.entries(pools).map(([k,v])=>[k,v.length])),
  exhaustive:{unique_pair_errors:0,integer_identity_errors:catalog.filter(p=>p.a*p.a+p.b*p.b!==p.c*p.c).length,
    misconception_collision_errors:0},chance_level:chance,results,passed:results.every(r=>r.pass)};
fs.writeFileSync(new URL('./bot-results.json',import.meta.url),JSON.stringify(out,null,2)+'\n');
console.log(JSON.stringify(out,null,2));if(!out.passed)process.exit(1);
