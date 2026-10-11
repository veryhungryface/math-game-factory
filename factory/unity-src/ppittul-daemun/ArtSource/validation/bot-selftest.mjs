import fs from 'node:fs';
import path from 'node:path';

const TRIALS = 200;
const PROPERTY_INDICES = new Set([1,3,5,6,8,10]);

function rng32(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; };
}
function snapProperty(d) {
  const values = [1,5,8,10];
  let best = values[0], distance = Math.abs(d-best);
  for (let i=1;i<values.length;i++) {
    const next = Math.abs(d-values[i]);
    if (next < distance) { best=values[i]; distance=next; }
  }
  return best;
}
function propertyChance(target) {
  let hits=0;
  for (let raw=0;raw<11;raw++) if (snapProperty(raw)===target) hits++;
  return hits/11;
}
function detentForCondition(serial,condition) {
  const values=[1,5,8,10];
  return values[(condition+Math.abs(serial)%4)%4];
}
function course(random,game) {
  const v = [0,1,2,3,4,6,7,8,9,10];
  for (let i=v.length-1;i>0;i--) {
    const j=Math.floor(random()*(i+1));
    [v[i],v[j]]=[v[j],v[i]];
  }
  const serial=(index)=> (game+1)*1000+index*20+Math.floor(random()*20);
  return [
    v[0],detentForCondition(serial(1),0),v[1],detentForCondition(serial(3),1),v[2],
    detentForCondition(serial(5),0),detentForCondition(serial(6),2),v[3],
    detentForCondition(serial(8),1),v[4],detentForCondition(serial(10),3)
  ];
}

function run(kind,seed) {
  const courseRandom=rng32(seed), inputRandom=rng32(seed^0x9e3779b9);
  let firstCorrect=0,firstTotal=0,chanceMass=0,varianceMass=0,complete=0,progress=0,cycle=0;
  for (let game=0;game<TRIALS;game++) {
    const deck=course(courseRandom,game);
    let lives=3,solved=0,attempts=0;
    while(lives>0&&solved<11) {
      const raw=kind==='fixed'?0:kind==='cycle'?(cycle++%11):Math.floor(inputRandom()*11);
      const property=PROPERTY_INDICES.has(solved);
      const guess=property?snapProperty(raw):raw;
      const target=deck[solved];
      if(attempts===0) {
        firstTotal++;
        const p=property?propertyChance(target):1/11;
        chanceMass+=p; varianceMass+=p*(1-p);
        if(guess===target)firstCorrect++;
      }
      if(guess===target){solved++;progress++;attempts=0;}else{lives--;attempts++;}
    }
    if(solved===11)complete++;
  }
  const firstTryRate=firstTotal?firstCorrect/firstTotal:0;
  const chance=firstTotal?chanceMass/firstTotal:0;
  const z=varianceMass>0?(firstCorrect-chanceMass)/Math.sqrt(varianceMass):0;
  return {games:TRIALS,firstCorrect,firstTotal,firstTryRate,chance,completed:complete,
    completionRate:complete/TRIALS,solved:progress,z,significantlyAboveChance:z>1.645,
    passed:firstTotal>0&&z<=1.645&&complete===0};
}

const fixed=run('fixed',0x51a7);
const cycle=run('cycle',0x12345678);
const random=run('random',0xbadc0de);
const idle={games:TRIALS,firstCorrect:0,firstTotal:0,firstTryRate:0,chance:0,completed:0,completionRate:0,solved:0,passed:true};
const result={
  runId:`ppittul-daemun-b3-${new Date().toISOString()}`,
  model:'C#과 같은 각 관계 5문 + 대각선·포함 관계 조건 걸쇠 6문, 3바늘 종료. 조건 레일은 실제 입력과 같은 최근접 스냅을 적용.',
  criterion:'각 정책 200판의 첫 시도 정답률이 문항별 걸쇠 우연 수준을 단측 5%로 유의하게 넘지 않고, 무입력 진도 0, 완주 0',
  fixed,cycle,random,idle,passed:fixed.passed&&cycle.passed&&random.passed&&idle.passed
};
fs.writeFileSync(path.join(path.dirname(new URL(import.meta.url).pathname),'bot-results.json'),JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
if(!result.passed)process.exitCode=1;
