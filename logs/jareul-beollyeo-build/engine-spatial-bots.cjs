/* B3 uses the game's REAL pointer handlers in a DOM-stubbed VM. Drawing is omitted;
 * inputDown/inputMove/inputUp, tool movement, hit tests, and integer grading are
 * the unmodified production code. QA separately drives browser-created pointers.
 * First-try accuracy counts only questions with a submitted math judgment.
 * Presented-but-unanswered inspections are disclosed separately, never added
 * to the accuracy denominator. Zero attempts report zero learning progress.
 */
const fs=require('node:fs'),vm=require('node:vm'),crypto=require('node:crypto');
const engine=fs.readFileSync('public/g/jareul-beollyeo/engine.js','utf8'),html=fs.readFileSync('public/g/jareul-beollyeo/index.html','utf8');
const scripts=[...html.matchAll(/<script(?:\s[^>]*)?>([\s\S]*?)<\/script>/g)].map(x=>x[1]).filter(Boolean);const ui=scripts.at(-1);
const WIDTH=390,HEIGHT=844;
function element(){const rect={x:0,y:0,left:0,top:0,right:WIDTH,bottom:HEIGHT,width:WIDTH,height:HEIGHT,toJSON(){return {...this,toJSON:undefined}}};return {textContent:'',hidden:false,style:{setProperty(){}},classList:{toggle(){},add(){},remove(){}},appendChild(){},addEventListener(){},setAttribute(){},animate(){},setPointerCapture(){},getBoundingClientRect(){return rect},getContext(){return context2d}};}
const gradient={addColorStop(){}};const context2d=new Proxy({}, {get(t,k){if(k==='measureText')return s=>({width:String(s).length*10});if(k.startsWith('create'))return ()=>gradient;return ()=>{}},set(){return true}});
function sandbox(){const elements=new Map();const get=id=>{if(!elements.has(id))elements.set(id,element());return elements.get(id)};const document={getElementById:get,createElement:element,documentElement:element(),querySelector:()=>element(),querySelectorAll:s=>Array.from({length:s==='.lamp'?3:10},element),addEventListener(){}};const x={console,document,innerWidth:WIDTH,innerHeight:HEIGHT,devicePixelRatio:1,matchMedia:()=>({matches:true}),localStorage:{getItem(){return null},setItem(){}},requestAnimationFrame(){},addEventListener(){},setTimeout(){},clearTimeout(){},Image:class{},performance:{now:()=>0}};x.window=x;vm.createContext(x);vm.runInContext(engine,x);vm.runInContext(ui,x);vm.runInContext(`globalThis.driver={
 start(seed){game.seed=seed;game.runCount=0;start(false,true);now=0;holdUntil=0;},
 state(){return game.state},
 targets(){return window.__GAME_TEST__.getInputTargets()},
 advance(dt){now+=dt;game.tick(dt);if(holdUntil&&now>=holdUntil){holdUntil=0;game.next();layout();syncTools();}checkFeedback();},
 drag(a,b){const e=p=>({clientX:p.x,clientY:p.y,pointerType:'touch',pointerId:1,button:0});inputDown(e(a));inputMove(e({x:(a.x+b.x)/2,y:(a.y+b.y)/2}));inputUp(e(b));},
 release(){$('release').onclick();}
};`,x);return x;}
const box=sandbox(),D=box.DividerGame,driver=box.driver;
function chance(p){if(p.mode==='side')return D.acceptsPiece(p)?D.equalPairs(p.sides).length/4:.25;if(p.mode==='equilateral')return D.acceptsPiece(p)?.75:.25;if(p.mode==='angle')return p.angleType==='acute'?1:p.maxVertices.length/3;if(!D.acceptsPiece(p))return .25;return D.equalPairs(p.sides).length/4*(p.angleType==='acute'?1:p.maxVertices.length/3)}
const report={method:'Unmodified production pointer handlers executed in a DOM-stubbed VM; no answer hook or direct grading used by policies. Only inspections with submitted math judgments are the accuracy denominator; presented-but-unanswered counts are separately disclosed. Zero attempts mean zero learning progress. Chance is a conservative upper bound giving blind bots perfect endpoints and angle alignment; repeat/cycle use the uniform four-math-intent reference even with no submissions. Idle chance is exactly zero. See separate engine-bots.json for the stronger semantic-intent model.',sourceSha256:crypto.createHash('sha256').update(html+engine).digest('hex'),viewport:[WIDTH,HEIGHT],results:[]};
for(const policy of ['repeat','cycle','random','idle','always_discard','fixed_full_measure']){
 let presented=0,attempted=0,correct=0,solved=0,completed=0,sumChance=0;const runs=[];
 for(let run=0;run<200;run++){
  const seed=71001+run*7919,r=D.rng(113+run*104729);driver.start(seed);let step=0;const seen=new Map();
  function observe(){for(const p of driver.state().pieces)if(!seen.has(p.id))seen.set(p.id,p)}observe();
  while(driver.state().phase==='playing'&&step<310){
   const t=driver.targets(),p=driver.state().piece;
   if(policy==='repeat'){const q={x:WIDTH/2,y:HEIGHT*.53};driver.drag(q,q);}
   else if(policy==='cycle'){const coords=[{x:WIDTH*.25,y:HEIGHT*.36},{x:WIDTH*.75,y:HEIGHT*.36},{x:WIDTH*.75,y:HEIGHT*.66},{x:WIDTH*.25,y:HEIGHT*.66}];driver.drag(coords[step%4],coords[(step+1)%4]);}
   else if(policy==='random')driver.drag({x:r()*WIDTH,y:r()*HEIGHT},{x:r()*WIDTH,y:r()*HEIGHT});
   else if(policy==='always_discard')driver.drag(t.pieces.find(x=>x.active).center,{x:t.bin.x+t.bin.w/2,y:t.bin.y+t.bin.h/2});
   else if(policy==='fixed_full_measure'){
    // A fixed tool sequence ignores all lengths/types: unlock, edge01, edge12,
    // edge20, vertex0, vertex1, vertex2, discard. No answer-dependent branching.
    const op=step%8,v=t.vertices;
    if(op===0)driver.release();
    else if(op===1)driver.drag(v[0],v[1]);
    else if(op===2&&t.ruler)driver.drag(t.ruler.a,v[2]);
    else if(op===3&&t.ruler)driver.drag(t.ruler.b,v[0]);
    else if(op>=4&&op<=6&&t.square){const j=op-4;driver.drag(t.square.corner,v[j]);const u=driver.targets(),c=u.square.corner,next=v[(j+1)%3];const desired=Math.atan2(next.y-c.y,next.x-c.x)-(u.square.handleAngleOffset||0),dx=Math.cos(desired),dy=Math.sin(desired);const horizontal=dx>0?(WIDTH-8-c.x)/dx:dx<0?(8-c.x)/dx:Infinity,vertical=dy>0?(HEIGHT-8-c.y)/dy:dy<0?(8-c.y)/dy:Infinity,radius=Math.max(8,Math.min(80,horizontal,vertical));driver.drag(u.square.handle,{x:c.x+dx*radius,y:c.y+dy*radius});}
    else if(op===7)driver.drag(t.pieces.find(x=>x.active).center,{x:t.bin.x+t.bin.w/2,y:t.bin.y+t.bin.h/2});
   }
   driver.advance(.3);observe();step++;
  }
  const s=driver.state();for(const p of seen.values())if(p.attempted)sumChance+=chance(p);presented+=seen.size;attempted+=s.firstAttempt.attempted;correct+=s.firstAttempt.correct;solved+=s.solved;if(s.phase==='clear')completed++;
  runs.push({seed,presented:seen.size,attempted:s.firstAttempt.attempted,firstCorrect:s.firstAttempt.correct,solved:s.solved,phase:s.phase});
 }
 const accuracy=attempted?correct/attempted:0,baseline=policy==='idle'?0:attempted?sumChance/attempted:.25;report.results.push({policy,runs:200,presented,attempted,firstCorrect:correct,firstAttemptAccuracy:accuracy,accuracyDenominator:'attempted questions only',presentedAccuracy:correct/presented,chanceUpperBound:baseline,completionRate:completed/200,totalSolved:solved,belowChance:accuracy<=baseline,runResults:runs});
}
fs.writeFileSync('logs/jareul-beollyeo-build/engine-spatial-bots.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report.results.map(({runResults,...r})=>r),null,2));if(report.results.some(r=>!r.belowChance))process.exitCode=1;
