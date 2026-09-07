export const REGIONS = [
  {name:'푸른 달',sub:'첫 번째 항로',color:0x70d8cb,sky:0x071b29},
  {name:'살구빛 고리',sub:'두 번째 항로',color:0xffbc80,sky:0x221b31},
  {name:'별의 등대',sub:'마지막 항로',color:0xa4a2ff,sky:0x111832}
];
export function makeProblem(a,b,id=0){return {id:`orbit-${id}-${a}-${b}`,a,b,prompt:`우주 대원 ${b}명에게 에너지를 한 명당 ${a}씩 배달해요. 모두 얼마가 필요한가요?`,answer:String(a*b),answerNumeric:a*b,choices:null,unitConcept:'세 자리 수와 한 자리 수의 곱셈'};}
export const POOL=[];
for(let a=100;a<=329;a++)for(let b=2;b<=6;b++)if(a*b>=220&&a*b<=999)POOL.push(makeProblem(a,b,POOL.length));
export const MISSIONS = [[112,3],[121,4],[132,3],[126,4],[143,5],[218,3],[157,6],[248,4],[329,3]].map(([a,b],i)=>makeProblem(a,b,i));
export function makeCampaign(random=Math.random){const used=new Set(['112:3']);return Array.from({length:9},(_,i)=>{if(i===0)return makeProblem(112,3,'tutorial');const pool=POOL.filter(p=>!used.has(`${p.a}:${p.b}`)&&(i>=3||(p.a<170&&p.b<=4)));const p=pool[Math.min(pool.length-1,Math.max(0,Math.floor(random()*pool.length)))];used.add(`${p.a}:${p.b}`);return {...p,id:`mission-${i}-${p.a}-${p.b}`}})}
// Sampling and the live campaign share this exact admissible problem pool.
export function sampleProblems(n){const pool=POOL.slice();for(let i=pool.length-1;i>0;i--){const j=Math.floor(Math.random()*(i+1));[pool[i],pool[j]]=[pool[j],pool[i]]}return Array.from({length:Math.max(0,Math.trunc(n))},(_,i)=>({...pool[i%pool.length],id:`sample-${i}-${pool[i%pool.length].id}`}));}
