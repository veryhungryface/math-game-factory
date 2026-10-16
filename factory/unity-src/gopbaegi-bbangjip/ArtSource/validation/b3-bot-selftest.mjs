import fs from 'node:fs';

// GopbaegiRules.RunDeck/JudgeRelease의 정수 모델을 그대로 옮긴 B3 무뇌 봇 검증.
// 200판씩 실행하며 완주율이 아니라 각 주문의 첫 release 정답률을 주 판정값으로 쓴다.
const deck = [
  [1,2,200],[1,3,216],[1,2,320],[2,3,216],[2,3,270],
  [3,4,320],[1,2,400],[1,3,432],[3,4,576]
].map(([m,n,target]) => ({m,n,target,capacity:Math.ceil(target*1.5)}));
const games=200;
let seed=0x6d676633;
function rnd(){ seed=(Math.imul(seed,1664525)+1013904223)>>>0; return seed/4294967296; }
const randInt=(a,b)=>a+Math.floor(rnd()*(b-a+1));

function run(name, policy){
  let firstCorrect=0, firstTotal=0, firstChanceSum=0, completed=0, progress=0, action=0;
  for(let g=0;g<games;g++){
    let lives=3, solved=0;
    for(const o of deck){
      let poured=0, first=true;
      while(lives>0){
        const delta=policy({g,action:action++,order:o,poured});
        if(delta==null) break;
        poured=Math.min(o.capacity,poured+delta);
        const ok=poured===o.target;
        if(first){ firstTotal++; firstChanceSum+=1/(o.capacity-3); if(ok)firstCorrect++; first=false; }
        if(ok){ solved++; break; }
        lives--;
        if(poured>o.target) poured=0; // 넘침은 새 컵, 모자람은 같은 컵에 이어 붓는다.
      }
      if(lives<=0 || first) break;
    }
    progress+=solved;
    if(solved===deck.length)completed++;
  }
  return {
    bot:name, games, firstAttemptCorrect:firstCorrect, firstAttemptTotal:firstTotal,
    firstAttemptRate:firstTotal?firstCorrect/firstTotal:0,
    chanceLevel:firstTotal?firstChanceSum/firstTotal:1/(Math.ceil(200*1.5)-3),
    completionRate:completed/games, meanSolved:progress/games
  };
}

const policies={
  spam:()=>4,                                  // 80 ms 경계 연타: 최소 유효량만 반복
  cycle:({action})=>[25,75,125,175][action%4], // 4개 고정 hold 길이를 순환
  random:({order})=>randInt(4,order.capacity), // 수학 정보와 독립인 균일 release
  noinput:()=>null
};
const results=Object.entries(policies).map(([k,v])=>run(k,v));
const meanChance=deck.reduce((s,o)=>s+1/(o.capacity-3),0)/deck.length;
const worstChance=1/(Math.ceil(200*1.5)-3);
const output={
  model:"GopbaegiRules integer tick/release model", gamesPerBot:games,
  chance:{meanUniform:meanChance,worstUniform:worstChance,acceptedIntegers:1}, results,
  pass:results.every(r=>r.firstAttemptRate<=r.chanceLevel+1e-12) && results.find(r=>r.bot==='noinput').meanSolved===0
};
const out=new URL('./b3-bot-results.json',import.meta.url);
fs.writeFileSync(out,JSON.stringify(output,null,2)+'\n');
console.log(JSON.stringify(output,null,2));
if(!output.pass)process.exitCode=1;
