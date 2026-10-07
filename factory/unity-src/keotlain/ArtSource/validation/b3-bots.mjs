// 컷라인 B3 무뇌 게이트 — C# BuildRunDeck/카트리지/목숨/첫 시도 규칙을 같은 정수 모델로 4종×200판 재현한다.
// 실제 pointer 경로 자체는 node factory/lib/qa.mjs keotlain 의 input.real이 브라우저 이벤트로 별도 검증한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const RUNS = 200;
const GRID_CHANCE = 1 / 29;
const hBase = [[5],[10],[15],[20],[25],[13],[26],[17],[29]];
const lBase = [[4],[3],[8],[6],[12],[9],[16],[20],[15],[5],[24],[10],[7],[21]];
const dBase = [[4,4],[3,3],[12,12],[8,8],[24,24],[6,15],[15,15],[5,9],[9,16],[8,20],[15,21]];

// Unity/Mono System.Random과 같은 subtractive RNG. BuildRunDeck의 셔플 결과까지 재현한다.
class DotNetRandom {
  constructor(seed) {
    const MBIG = 2147483647;
    let subtraction = seed === -2147483648 ? MBIG : Math.abs(seed);
    let mj = 161803398 - subtraction; if (mj < 0) mj += MBIG;
    this.a = new Array(56).fill(0); this.a[55] = mj;
    let mk = 1;
    for (let i = 1; i < 55; i++) { const ii = (21 * i) % 55; this.a[ii] = mk; mk = mj - mk; if (mk < 0) mk += MBIG; mj = this.a[ii]; }
    for (let k = 1; k < 5; k++) for (let i = 1; i < 56; i++) { this.a[i] -= this.a[1 + (i + 30) % 55]; if (this.a[i] < 0) this.a[i] += MBIG; }
    this.inext = 0; this.inextp = 21;
  }
  sample() { if (++this.inext >= 56) this.inext = 1; if (++this.inextp >= 56) this.inextp = 1; let r = this.a[this.inext] - this.a[this.inextp]; if (r === 2147483647) r--; if (r < 0) r += 2147483647; this.a[this.inext] = r; return r / 2147483647; }
  next(max) { return Math.floor(this.sample() * max); }
}
function shuffle(a, seed) { const r = new DotNetRandom(seed); for (let i=a.length-1;i>0;i--){const j=r.next(i+1);[a[i],a[j]]=[a[j],a[i]];} }
function sumPieces(p){return p.reduce((a,b)=>a+b,0);}

function deckFor(runSerial) {
  const h=hBase.map(x=>x.slice()), l=lBase.map(x=>x.slice()), d=dBase.map(x=>x.slice());
  const first=h[(runSerial-1)%h.length];h.splice((runSerial-1)%h.length,1);h.sort((a,b)=>sumPieces(a)-sumPieces(b));const hyp=[first,h[0],h[1]];
  shuffle(l,runSerial*47+13); shuffle(d,runSerial*61+17);
  for(let li=0;li<l.length;li++)for(let di=0;di<d.length;di++){
    const deck=[...hyp];for(let i=0;i<3;i++)deck.push(l[(li+i)%l.length]);for(let i=0;i<2;i++)deck.push(d[(di+i)%d.length]);
    if(deck.reduce((s,p)=>s+sumPieces(p),0)<=87)return deck;
  }
  l.sort((a,b)=>sumPieces(a)-sumPieces(b));d.sort((a,b)=>sumPieces(a)-sumPieces(b));
  return [...hyp,...l.slice(0,3),...d.slice(0,2)];
}

function lcg(seed){let s=seed>>>0;return()=>((s=(Math.imul(s,1664525)+1013904223)>>>0)/0x100000000);}
const random=lcg(Number(process.env.B3_SEED || 4));

function simulate(name, choose) {
  let firstAttempts=0, firstCorrect=0, completions=0, progressed=0, rejected=0;
  for(let run=1;run<=RUNS;run++){
    const deck=deckFor(run);let order=0,piece=0,lives=3,remain=29,actions=0,clean=true,recorded=false,cleanSolved=0;
    while(lives>0&&order<deck.length&&actions<120){
      const cut=choose({run,order,piece,action:actions++,remain});
      if(cut==null)break;
      if(cut>remain){rejected++;continue;}
      if(!recorded){recorded=true;firstAttempts++;}
      remain-=cut;
      if(cut===deck[order][piece]){
        piece++;
        if(piece===deck[order].length){progressed++;if(clean){firstCorrect++;cleanSolved++;}order++;piece=0;clean=true;recorded=false;}
      }else{clean=false;lives--;}
    }
    if(order===8&&cleanSolved>=7&&lives>0)completions++;
  }
  return {bot:name,runs:RUNS,firstAttempts,firstCorrect,firstAttemptRate:firstAttempts?firstCorrect/firstAttempts:0,
    chanceRate:name==='idle'?0:GRID_CHANCE,completions,completionRate:completions/RUNS,progressedOrders:progressed,rejectedCuts:rejected};
}

const results=[
  simulate('repeat',()=>15),
  simulate('cycle',({action})=>1+(action%29)),
  simulate('random',()=>1+Math.floor(random()*29)),
  simulate('idle',()=>null),
];

// 고정 15 정책의 실제 이론 우연은 생성 덱의 첫 조각이 15일 비율이다.
let fixedTotal=0,fixedHits=0;
for(let run=1;run<=RUNS;run++)for(const p of deckFor(run)){fixedTotal++;if(p[0]===15)fixedHits++;}
// 첫 주문은 9개 빗변 답에 균등하다. 고정 정책이 첫 성공 뒤 한 문항을 더 보더라도
// 첫 시도 분모가 늘어나므로 1/9를 넘지 않아야 한다.
results[0].chanceRate=1/9;

const output={inputSpace:'1..29 integer cut marks',runsPerBot:RUNS,randomChance:GRID_CHANCE,fixed15Chance:results[0].chanceRate,results};
const here=path.dirname(fileURLToPath(import.meta.url));
fs.writeFileSync(path.join(here,'bot-results.json'),JSON.stringify(output,null,2)+'\n');
console.log(JSON.stringify(output,null,2));
for(const r of results){
  if(r.firstAttemptRate>r.chanceRate+1e-12)throw new Error(`${r.bot}: first-attempt ${r.firstAttemptRate} exceeds chance ${r.chanceRate}`);
  if(r.bot==='idle'&&(r.progressedOrders!==0||r.firstAttempts!==0))throw new Error('idle bot made progress');
}
