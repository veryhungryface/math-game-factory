const fs = require('fs');
const source = fs.readFileSync('/Users/sitpo/math-game-factory/public/g/ink-print/index.html', 'utf8').match(/<script>\s*([\s\S]*?)<\/script>/)[1];
const noop = () => {};
function classList(){const s=new Set();return{add(...x){x.forEach(v=>s.add(v))},remove(...x){x.forEach(v=>s.delete(v))},toggle(x,on){if(on===undefined){if(s.has(x))s.delete(x);else s.add(x)}else if(on)s.add(x);else s.delete(x)},contains(x){return s.has(x)}}}
function element(id){return{id,textContent:'0',innerHTML:'',className:'',style:{},dataset:{},classList:classList(),onclick:null,onerror:null,offsetWidth:10,addEventListener:noop,setAttribute:noop,getBoundingClientRect(){return{width:id==='boardCanvas'?390:100,height:id==='boardCanvas'?520:50,left:0,top:0}},querySelectorAll(){return[]},closest(){return null},setPointerCapture:noop,getContext(){return ctx}}}
const ctx = new Proxy({createLinearGradient(){return{addColorStop:noop}},beginPath:noop,roundRect:noop,fill:noop,stroke:noop,fillRect:noop,clearRect:noop,save:noop,restore:noop,clip:noop,moveTo:noop,lineTo:noop,arc:noop,strokeRect:noop,translate:noop,rotate:noop,scale:noop,setLineDash:noop,fillText:noop,setTransform:noop}, {set(o,k,v){o[k]=v;return true}});
const els=new Map();
global.window=global;global.innerWidth=390;global.innerHeight=844;global.devicePixelRatio=1;global.addEventListener=noop;global.requestAnimationFrame=noop;global.performance={now:(()=>{let t=1000;return()=>t+=16})()};
global.localStorage={m:new Map(),getItem(k){return this.m.has(k)?this.m.get(k):null},setItem(k,v){this.m.set(k,String(v))}};
global.document={documentElement:{classList:classList(),style:{setProperty:noop}},getElementById(id){if(!els.has(id))els.set(id,element(id));return els.get(id)}};
global.Image=class{set src(v){this._src=v}};
eval(source);
const t=global.__GAME_TEST__;
if(!t?.ready)throw new Error('test hook not ready');
const samples=t.sampleProblems(100);
const unique=new Set(samples.map(p=>p.prompt));
for(const p of samples){if(!p.choices.includes(p.answer))throw new Error('answer missing '+p.id);if(new Set(p.choices).size!==p.choices.length)throw new Error('duplicate choice '+p.id);if(!Number.isFinite(p.answerNumeric))throw new Error('bad numeric '+p.id);const wrong=p.choiceDiagnostics.filter(x=>x.misconceptionId!=='correct');if(wrong.length<2)throw new Error('few distractors '+p.id);if(wrong.some(x=>x.numeric===p.answerNumeric))throw new Error('accidental truth '+p.id)}
if(unique.size/samples.length<.7)throw new Error('low variety');
if(!t.validateDistractors())throw new Error('distractor exhaustive validation failed');
t.start();const before=t.getState();t.answerCorrect();const afterCorrect=t.getState();t.answerWrong();const afterWrong=t.getState();
if(!(afterCorrect.score>before.score))throw new Error('score did not increase');
if(!(afterWrong.lives<afterCorrect.lives))throw new Error('life did not decrease');
const bots=t.botAudit(200);
for(const [name,v] of Object.entries(bots)){if(name==='none'&&v.completionRate!==0)throw new Error('no-input completed');if(v.completionRate>.25)throw new Error(name+' completion too high')}
console.log(JSON.stringify({sampleCount:samples.length,variety:unique.size/samples.length,state:{before,afterCorrect,afterWrong},distractorsAllValid:true,bots},null,2));
