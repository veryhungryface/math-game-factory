import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const html=fs.readFileSync('public/g/stick-a-whole/index.html','utf8');
const source=html.match(/<script>([\s\S]*?)<\/script>/)[1];
// A minimal non-rendering DOM adapter: exercises the actual production state machine.
// This does NOT claim to test layout, browser events, fps, accessibility, or pixels.
let context;
class El{
 constructor(){this.style={setProperty(){}};this.classList={add(){},remove(){},toggle(){}};this.dataset={};this.children=[];this.clientWidth=354;this.parentElement=this;this.offsetWidth=354;this.textContent='';}
 set innerHTML(s){this.html=s}get innerHTML(){return this.html||''}
 querySelector(s){return document.querySelector(s)}querySelectorAll(s){return document.querySelectorAll(s)}
 append(e){this.children.push(e)}addEventListener(){}remove(){}setAttribute(){}getBoundingClientRect(){return {left:18,right:372,top:250,bottom:314,width:354,height:64}}
 getContext(){return new Proxy({},{get:()=>()=>{}})}
}
const nodes=new Map(),document={documentElement:new El(),body:new El(),getElementById(id){if(!nodes.has(id))nodes.set(id,new El());return nodes.get(id)},createElement(){return new El()},querySelector(sel){let e=new El(),m=sel.match(/data-rail="(\d)"/);if(m){const state=vm.runInContext('S',context);const index=Number(m[1]);e.dataset.target=state.room>=6?(state.reverse?(index?state.target:state.target-1):(index?state.target-1:state.target)):state.target}return e},querySelectorAll(sel){if(sel.includes('.cell'))return Array.from({length:8},()=>new El());if(sel==='.railwrap')return [new El(),new El()];return []}};
context=vm.createContext({console,document,window:{},innerWidth:390,innerHeight:844,devicePixelRatio:1,performance:{now:()=>0},requestAnimationFrame(){},addEventListener(){},setTimeout(){},localStorage:{getItem(){return null},setItem(){}},Date,Math});vm.runInContext(source,context);
const run=s=>vm.runInContext(s,context);
const report={kind:'production-state-machine; non-rendering DOM adapter',browserVerified:false,qa:'blocked: listen EPERM 127.0.0.1',checks:[]};
run('start(1)');run('dropCard(3,0,0)');assert.equal(run('S.placed.length'),0);assert.equal(run('S.solved'),0);run('dropCard(0,0,0);dropCard(1,0,1)');assert.equal(run('S.solved'),1);run('tick(1.2)');assert.equal(run('S.room'),2);report.checks.push('Fixed tutorial: 3/4 decoy is refused and returned; 1/4 then 2/4 -> correct -> room 2');
function solve(){run(`{let p=pairs(S.hand,S.target,S.d)[0];let ri=S.room>=6&&S.reverse?1:0;dropCard(p[0],ri,0);dropCard(p[1],ri,S.hand[p[0]].n);if(S.phase==='grouping')setBoundary(S.d);tick(1.2)}`)}
for(let s=1;s<=200;s++){run(`start(${s})`);while(run('S.phase')!=='clear')solve()}
report.checks.push('Knowledge strategy: 200/200 wins through shared placement/boundary paths');
for(let s=1;s<=200;s++){run(`start(${s})`);solve();run('wrong("검증: 첫 일반 실수");tick(1.2)');assert.equal(run('S.lives'),2);while(run('S.phase')!=='clear')solve()}
report.checks.push('First ordinary mistake then all correct: 200/200 wins');
for(const n of [40,17,63]){const ps=run(`window.__GAME_TEST__.sampleProblems(${n})`);assert.equal(ps.length,n);assert.ok(new Set(ps.map(p=>p.id)).size>=n*.7);for(const p of ps){assert.match(p.prompt,/반드시 두 조각/);assert.ok(p.choices.includes(p.answer));assert.equal(new Set(p.choices).size,p.choices.length);assert.equal(p.correctPairs.length,2);assert.equal(p.answerNumeric,p.target.n/p.target.d)}}
report.checks.push('40/17/63 samples: explicit two-piece condition, uniqueness, membership, independent integer pair sums');
const pAudit=run(`pools.map(p=>({d:p.d,t:p.t,nums:p.nums}))`);
let checked=0;for(const p of pAudit){let count=0;for(let i=0;i<5;i++)for(let j=i+1;j<5;j++){if(p.nums[i]+p.nums[j]===p.t)count++}assert.equal(count,2);assert.notEqual(p.t%p.d||1,p.d);checked++}
report.checks.push(`All ${checked} hand/target tuples: exactly 2 correct pairs; wrong whole boundary never true`);
// Bots receive only visible hand availability, cell capacity and phase. No correct
// indices, numerator sum or target comparison is used by their policy.
let botSeed=917;const random=()=>{botSeed=(Math.imul(botSeed,1664525)+1013904223)>>>0;return botSeed/4294967296};
report.bots={};for(const kind of ['repeat','cycle','random','idle']){let wins=0,score=0;for(let game=0;game<200;game++){run(`start(${game+100})`);let cursor=0;for(let step=0;step<500&&['playing','grouping','feedback'].includes(run('S.phase'));step++){
 if(kind==='idle'){run('tick(151)');break}
 const state=JSON.parse(run('JSON.stringify(S)'));
 if(state.phase==='feedback'){run('tick(1.2)');continue}
 if(state.phase==='grouping'){let p=kind==='random'?Math.floor(random()*9):kind==='cycle'?cursor++%9:1;run(`setBoundary(${p})`)}else{
 const i=kind==='repeat'?0:kind==='cycle'?cursor++%5:Math.floor(random()*5);
 const pos=kind==='random'?Math.floor(random()*(state.room>=3?8:4)):state.placed.length?1:0;
 const rail=state.room>=6?(kind==='random'?Math.floor(random()*2):kind==='cycle'?cursor%2:0):0;
 run(`dropCard(${i},${rail},${pos})`)
 }
 run('tick(1)');
 }
 // Fixed simulation budget never gets counted as success.
 if(run('S.phase')==='clear')wins++;score+=run('S.score');
 }report.bots[kind]={games:200,wins,completionRate:wins/200,averageScore:score/200};assert.ok(wins<=50);if(kind==='idle')assert.equal(wins,0)}
// Stronger no-skill bot: grant perfect tutorial, adjacency and correct boundary,
// randomize only the two card indices and comparison rail. Prevents a tutorial
// bottleneck or difficult gesture from making the measured completion deceptively low.
let wins=0,total=0;for(let s=0;s<200;s++){run(`start(${s+900})`);solve();for(let n=0;n<200&&['playing','grouping','feedback'].includes(run('S.phase'));n++){
 if(run('S.phase')==='feedback'){run('tick(1.2)');continue}let i=Math.floor(random()*5),j=Math.floor(random()*4);if(j>=i)j++;let ri=run('S.room>=6')?Math.floor(random()*2):0;run(`dropCard(${i},${ri},0);dropCard(${j},${ri},S.hand[${i}].n);if(S.phase==='grouping')setBoundary(S.d);tick(1.2)`)
 }if(run('S.phase')==='clear')wins++;total+=run('S.score')}
report.strongRandom={games:200,wins,completionRate:wins/200,averageScore:total/200};assert.ok(wins<=50);
fs.writeFileSync('scratchpad/stick-a-whole/validation.json',JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));
