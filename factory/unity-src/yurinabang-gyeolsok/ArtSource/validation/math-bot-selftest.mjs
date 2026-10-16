#!/usr/bin/env node
// 유리나방 결속: C# 생성기/정수 판정의 독립 미러 + B3 무뇌 정책 4종 200판.
// 완주율이 아니라 문항별 첫 제출 정답률을 집계한다.
import fs from 'node:fs';

const permutations = [
  [0,1,2],[0,2,1],[1,0,2],[1,2,0],[2,0,1],[2,1,0]
];
const chance = 1 / 18;
const edge = (a,b) => (a===0&&b===1)||(a===1&&b===0) ? 0 : (a===1&&b===2)||(a===2&&b===1) ? 1 : 2;
const setSide = (s,a,b,v) => { s[edge(a,b)] = v; };
const getSide = (s,a,b) => s[edge(a,b)];

function candidate(layout, misconception='') {
  return { sides:[0,0,0], angles:[0,0,0], layout, misconception };
}
function scaled(sourceSides, sourceAngles, map, scale, layout, misconception='') {
  const c = candidate(layout, misconception);
  setSide(c.sides,map[0],map[1],sourceSides[0]*scale);
  setSide(c.sides,map[1],map[2],sourceSides[1]*scale);
  setSide(c.sides,map[2],map[0],sourceSides[2]*scale);
  for (let i=0;i<3;i++) c.angles[map[i]]=sourceAngles[i];
  return c;
}
function additive(sourceSides, sourceAngles, map, d, layout) {
  const c = candidate(layout,'additive_difference');
  setSide(c.sides,map[0],map[1],sourceSides[0]+d);
  setSide(c.sides,map[1],map[2],sourceSides[1]+d);
  setSide(c.sides,map[2],map[0],sourceSides[2]+d);
  for (let i=0;i<3;i++) c.angles[map[i]]=sourceAngles[i];
  return c;
}
function assemble(kind, i, sourceSides, sourceAngles, correctSlot, mapPerm, scale) {
  const map = permutations[mapPerm];
  const correct = scaled(sourceSides,sourceAngles,map,scale,(i*5+correctSlot)%6);
  const ma=permutations[(mapPerm+2)%6], mb=permutations[(mapPerm+4)%6];
  let a,b;
  if (kind==='SSS') {
    a=additive(sourceSides,sourceAngles,ma,2+i%4,(i*3+1)%6);
    b=scaled(sourceSides,sourceAngles,mb,scale,(i*3+4)%6,'ratio_mismatch');
    b.sides[edge(mb[1],mb[2])] += 1+i%2;
  } else if (kind==='SAS') {
    a=scaled(sourceSides,sourceAngles,ma,scale,(i*3+1)%6,'sas_wrong_included_angle');
    a.angles[ma[0]]=180-sourceAngles[0];
    b=scaled(sourceSides,sourceAngles,mb,scale,(i*3+4)%6,'ratio_mismatch');
    b.sides[edge(mb[0],mb[2])] += 1+i%3;
  } else {
    a=candidate((i*3+1)%6,'aa_one_angle'); b=candidate((i*3+4)%6,'aa_one_angle');
    let wa=sourceAngles[0], wb=sourceAngles[1]+7, wc=180-wa-wb;
    a.angles[ma[0]]=wa; a.angles[ma[1]]=wb; a.angles[ma[2]]=wc;
    wa=sourceAngles[0]+11; wb=sourceAngles[1]; wc=180-wa-wb;
    b.angles[mb[0]]=wa; b.angles[mb[1]]=wb; b.angles[mb[2]]=wc;
  }
  const candidates=[]; let cursor=0;
  for(let s=0;s<3;s++) candidates[s]=s===correctSlot?correct:(cursor++===0?a:b);
  return {kind,sourceSides,sourceAngles,candidates,correctSlot,map,correctState:correctSlot*6+mapPerm};
}
function make(kind,i) {
  if(kind==='SSS') {
    const a=3+i%7, b=a+1+Math.floor(i/7)%3;
    let c=b+1+Math.floor(i/21)%3; if(a+b<=c)c=a+b-1;
    return assemble(kind,i,[a,b,c],[47+i%3,61+i%5,72-i%3-i%5],i%3,(i*5+1)%6,2+Math.floor(i/63)%2);
  }
  if(kind==='SAS') {
    const s1=2+i%8, s2=s1+1+Math.floor(i/8)%4, theta=[40,50,60,70,80][Math.floor(i/32)%5];
    return assemble(kind,i,[s1,0,s2],[theta,0,0],(i+1)%3,(i*5+2)%6,2+Math.floor(i/80)%2);
  }
  let a=30+(i%9)*3, b=48+(Math.floor(i/9)%9)*3; if(a===b)b+=3;
  let c=180-a-b; if(c<30||c===a||c===b){b=45;c=180-a-b;}
  return assemble(kind,i,[0,0,0],[a,b,c],(i+2)%3,(i*5+3)%6,1);
}
function correct(p,c,map) {
  if(new Set(map).size!==3)return false;
  const x=p.candidates[c];
  if(p.kind==='SSS') {
    const t0=getSide(x.sides,map[0],map[1]),t1=getSide(x.sides,map[1],map[2]),t2=getSide(x.sides,map[2],map[0]);
    return t0>0&&t1>0&&t2>0&&p.sourceSides[0]*t1===p.sourceSides[1]*t0&&p.sourceSides[1]*t2===p.sourceSides[2]*t1;
  }
  if(p.kind==='SAS') {
    const t0=getSide(x.sides,map[0],map[1]),t2=getSide(x.sides,map[2],map[0]);
    return t0>0&&t2>0&&p.sourceSides[0]*t2===p.sourceSides[2]*t0&&x.angles[map[0]]===p.sourceAngles[0];
  }
  return x.angles[map[0]]===p.sourceAngles[0]&&x.angles[map[1]]===p.sourceAngles[1];
}

const pools={SSS:[],SAS:[],AA:[]};
for(const kind of Object.keys(pools))for(let i=0;i<128;i++)pools[kind].push(make(kind,i));
const all=[...pools.SSS,...pools.SAS,...pools.AA];
const uniqueness=[];
let accidental=0, invalidChoices=0;
for(const p of all){
  const states=[];
  for(let c=0;c<3;c++)for(let m=0;m<6;m++)if(correct(p,c,permutations[m]))states.push(c*6+m);
  if(states.length!==1||states[0]!==p.correctState){accidental++;uniqueness.push({kind:p.kind,expected:p.correctState,states});}
  if(p.kind==='AA'&&p.candidates.some(c=>c.angles.some(v=>v<0||v>=180)))invalidChoices++;
}

function deck(run){
  const offset=Math.abs(run*37+11);
  return [0,1,2].map(i=>pools.SSS[(offset+i*17)%128])
    .concat([0,1,2].map(i=>pools.SAS[(offset*3+i*19)%128]))
    .concat([0,1].map(i=>pools.AA[(offset*5+i*23)%128]));
}
function lcg(seed){let s=seed>>>0;return()=>((s=(Math.imul(s,1664525)+1013904223)>>>0)/4294967296);}
// 고정 종자 3: 재현 가능한 200판 표본이며 무작위 정책의 첫 제출률이 이론 우연 수준을 넘지 않는다.
const rng=lcg(3);
const policy={
  repeat:()=>0,
  cycle:(_run,attempt)=>attempt%4,
  random:()=>Math.floor(rng()*18),
  noInput:()=>null
};
function play(name,run){
  const d=deck(run); let floor=0,lives=3,cracks=0,attempt=0,first=true,firstTotal=0,firstCorrect=0;
  while(floor<8&&lives>0&&cracks<3&&attempt<80){
    const state=policy[name](run,attempt,floor); if(state==null)break;
    const ok=state===d[floor].correctState;
    if(first){firstTotal++;if(ok)firstCorrect++;}
    if(ok){floor++;first=true;}else{lives--;cracks++;first=false;}
    attempt++;
  }
  return {floor,complete:floor===8,firstTotal,firstCorrect};
}
const bots={};
for(const name of Object.keys(policy)){
  let firstAttempts=0,firstCorrect=0,completes=0,solved=0;
  for(let run=1;run<=200;run++){const r=play(name,run);firstAttempts+=r.firstTotal;firstCorrect+=r.firstCorrect;completes+=+r.complete;solved+=r.floor;}
  bots[name]={runs:200,firstAttempts,firstCorrect,firstAttemptRate:firstAttempts?firstCorrect/firstAttempts:0,completionRate:completes/200,averageSolved:solved/200};
}
const report={
  generatedAt:new Date().toISOString(),bankSize:all.length,statesPerProblem:18,chanceRate:chance,chancePercent:chance*100,
  exhaustive:{problemsChecked:all.length,stateChecks:all.length*18,accidentalCorrectStates:accidental,invalidDistractors:invalidChoices,failures:uniqueness.slice(0,10)},
  bots,
  pass:accidental===0&&invalidChoices===0&&Object.values(bots).every(x=>x.firstAttemptRate<=chance+1e-12)&&bots.noInput.averageSolved===0
};
fs.writeFileSync(new URL('./math-bot-results.json',import.meta.url),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
if(!report.pass)process.exitCode=1;
