import fs from 'node:fs';

// Dice Caravan B3 self-test. This mirrors the runtime's exact-set invariant:
// a first attempt is correct only when every selected outcomeId and no other outcomeId matches.
const RUNS = 200;
let seed = 0xd1ceca7a;
const rnd = () => ((seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0) / 2 ** 32);

function choose(n, k) {
  let result = 1;
  for (let i = 1; i <= k; i++) result = result * (n - k + i) / i;
  return result;
}
function maskOf(values) { let m = 0n; for (const v of values) m |= 1n << BigInt(v); return m; }
function diceSum(sum) {
  const a = [];
  for (let y = 1; y <= 6; y++) for (let x = 1; x <= 6; x++) if (x + y === sum) a.push((y - 1) * 6 + x - 1);
  return { n: 36, k: a.length, mask: maskOf(a) };
}
function multiples(n, d) { const a=[]; for(let i=1;i<=n;i++) if(i%d===0)a.push(i-1); return {n,k:a.length,mask:maskOf(a)}; }
function union(n,a,b){const v=[];for(let i=1;i<=n;i++)if(i%a===0||i%b===0)v.push(i-1);return{n,k:v.length,mask:maskOf(v)};}
function product(r,c){const cols=c+1,v=[];for(let y=0;y<r;y++)for(let x=0;x<c;x++)v.push(y*cols+x);return{n:(r+1)*(c+1),k:v.length,mask:maskOf(v)};}
function digits(){const v=[];for(let y=0;y<4;y++)for(let x=0;x<4;x++)if(y!==x&&y!==0)v.push(y*4+x);return{n:16,k:v.length,mask:maskOf(v)};}
function representatives(n){const v=[];for(let y=0;y<n;y++)for(let x=0;x<n;x++)if(y<x)v.push(y*n+x);return{n:n*n,k:v.length,mask:maskOf(v)};}

function mission(run, solved) {
  switch (solved) {
    case 0: return multiples(12 + run % 4, 4);
    case 1: return union(15, 4, 7);
    case 2: return product(2 + run % 2, 3);
    case 3: return diceSum(5 + run % 5);
    case 4: return digits();
    default: return representatives(4 + run % 2);
  }
}
function prefixMask(k, start, n) { const v=[]; for(let i=0;i<k;i++)v.push((start+i)%n); return maskOf(v); }
function randomKMask(n,k){const pool=Array.from({length:n},(_,i)=>i);for(let i=n-1;i>0;i--){const j=Math.floor(rnd()*(i+1));[pool[i],pool[j]]=[pool[j],pool[i]];}return maskOf(pool.slice(0,k));}

function runBot(name, makeInput) {
  let firstCorrect=0, firstTotal=0, completed=0, progress=0, inputIndex=0;
  for(let run=0;run<RUNS;run++){
    let lives=3, solved=0;
    while(lives>0&&solved<6){
      const target=mission(run,solved);
      if(!makeInput) break;
      const input=makeInput(inputIndex++,target);
      firstTotal++;
      if(input===target.mask){firstCorrect++;solved++;progress++;} else lives--;
    }
    if(solved===6)completed++;
  }
  return {name,runs:RUNS,first_attempt_correct:firstCorrect,first_attempt_total:firstTotal,
    first_attempt_rate_pct:firstTotal?+(firstCorrect*100/firstTotal).toFixed(3):0,
    completed,completion_rate_pct:+(completed*100/RUNS).toFixed(3),progress};
}

const bots=[
  runBot('fixed-spam',(_i,t)=>prefixMask(t.k,0,t.n)),
  runBot('cycle',(i,t)=>prefixMask(t.k,i%4,t.n)),
  runBot('random',(_i,t)=>randomKMask(t.n,t.k)),
  runBot('idle',null)
];
const result={generated_at:new Date().toISOString(),runs_per_bot:RUNS,
  contract:'exact selected outcomeId set; no auto-completion',
  conservative_chance_pct:+(100/choose(12,3)).toFixed(3),
  note:'The strong random policy is told k and still samples uniformly from C(N,k). The easiest legal board is N=12,k=3, so chance is at most 1/220. Idle makes zero submissions and zero progress.',bots};
fs.writeFileSync(new URL('./bot-results.json',import.meta.url),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify(result,null,2));
