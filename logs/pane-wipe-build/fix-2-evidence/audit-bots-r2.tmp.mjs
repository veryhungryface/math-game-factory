import fs from 'node:fs';
import vm from 'node:vm';
import crypto from 'node:crypto';

// Execute the production HTML's registered pointer callbacks and animation
// frame in a DOM/canvas stub. This tests routing but is NOT a trusted browser
// event, rendering, browser hit testing, FPS, or child-comprehension result.
const html=fs.readFileSync('public/g/pane-wipe/index.html','utf8');
const engine=fs.readFileSync('public/g/pane-wipe/engine.js','utf8');
const scripts=[...html.matchAll(/<script(?:\s[^>]*)?>([\s\S]*?)<\/script>/g)].map(x=>x[1]).filter(x=>x.trim());
const sha=s=>crypto.createHash('sha256').update(s).digest('hex');
const report={htmlSha256:sha(html),engineSha256:sha(engine),scope:'Production pointer callbacks in DOM-stub VM; synthetic callback events, not browser trusted input',gamesPerPolicy:200,errors:[],bots:[]};
function world(seed){
  let now=1000,callback=null,eventCount=0;const listeners={},els=new Map();
  const noop=()=>{};
  const ctx=new Proxy({}, {get:(o,k)=>o[k]||(o[k]=k==='measureText'?s=>({width:String(s).length*10}):noop),set:(o,k,v)=>(o[k]=v,true)});
  const elem=id=>{if(els.has(id))return els.get(id);const el={id,hidden:false,textContent:'',innerHTML:'',className:'',style:{setProperty:noop},classList:{add:noop,remove:noop,toggle:noop,contains:()=>false},closest:()=>null,animate:()=>({finished:Promise.resolve(),cancel:noop}),setAttribute:noop,getAttribute:()=>'',addEventListener:(k,f)=>{(listeners[id+':'+k]||(listeners[id+':'+k]=[])).push(f);},getContext:()=>ctx,getBoundingClientRect:()=>({left:0,top:0,width:390,height:844}),setPointerCapture:noop,releasePointerCapture:noop,hasPointerCapture:()=>true,appendChild:noop,focus:noop};els.set(id,el);return el;};
  for(const m of html.matchAll(/<[^>]+\bid="([^"]+)"[^>]*>/g))elem(m[1]).hidden=/\bhidden(?:\s|>|=)/.test(m[0]);
  const stoppers=Array.from({length:3},(_,i)=>elem('stopper'+i));
  const nativeDate=Date;
  function MockDate(...args){return new nativeDate(...(args.length?args:[seed-7919+now-1000]));}
  MockDate.now=()=>seed-7919+now-1000;MockDate.parse=nativeDate.parse;MockDate.UTC=nativeDate.UTC;
  const box={console,Math,Date:MockDate,Intl,JSON,Number,Map,Set,Array,Object,String,Boolean,RegExp,Error,Float32Array,Uint8Array,innerWidth:390,innerHeight:844,devicePixelRatio:1,performance:{now:()=>now},document:{getElementById:elem,documentElement:elem('html'),querySelectorAll:q=>q==='.stopper'?stoppers:[],createElement:tag=>elem('new-'+tag),addEventListener:(k,f)=>{(listeners['document:'+k]||(listeners['document:'+k]=[])).push(f);}},localStorage:{getItem:()=>null,setItem:noop},matchMedia:()=>({matches:false}),requestAnimationFrame:f=>{callback=f;return 1;},addEventListener:noop,setTimeout:noop,clearTimeout:noop};box.window=box;box.globalThis=box;vm.createContext(box);vm.runInContext(engine,box);for(const source of scripts)vm.runInContext(source,box);
  function frame(seconds){now+=seconds*1000;if(callback){const cb=callback;callback=null;cb(now);}}
  const hook=box.__GAME_TEST__;hook.start();frame(.016);
  function emit(type,p){for(const f of [...listeners['game:'+type]||[],...listeners['document:'+type]||[]]){eventCount++;f({pointerId:1,target:elem('game'),clientX:p.x,clientY:p.y,button:0,buttons:type==='pointerup'?0:1,preventDefault:noop,isTrusted:false});}}
  function drag(a,b){emit('pointerdown',a);for(let i=1;i<=6;i++)emit('pointermove',{x:a.x+(b.x-a.x)*i/6,y:a.y+(b.y-a.y)*i/6});emit('pointerup',b);frame(.03);}
  return{frame,hook,drag,get eventCount(){return eventCount;}};
}
function rng(seed){let n=seed;return()=>{n=(Math.imul(n,1664525)+1013904223)>>>0;return n/4294967296;};}
const policies=[
  {name:'repeat',note:'Grab the visible handle and repeat a 104 model-pixel upward stroke.',action:(t,k,r)=>({a:t.handle,b:{x:t.handle.x,y:t.handle.y-104*t.layout.scale}}),chanceUpper:8/180},
  {name:'cycle',note:'Grab the visible handle; cycle up/right/down/left strokes of 104 model pixels.',action:(t,k,r)=>{const a=[-90,0,90,180][k%4]*Math.PI/180,l=104*t.layout.scale;return{a:t.handle,b:{x:t.handle.x+Math.cos(a)*l,y:t.handle.y+Math.sin(a)*l}};},chanceUpper:8/180},
  {name:'random',note:'Grab the visible handle; choose a uniform angle over 360° and length uniformly 60–190 model pixels. It reads no answer, slope, or gap.',action:(t,k,r)=>{const a=r()*Math.PI*2,l=(60+r()*130)*t.layout.scale;return{a:t.handle,b:{x:t.handle.x+Math.cos(a)*l,y:t.handle.y+Math.sin(a)*l}};},chanceUpper:8/360},
  {name:'idle',note:'No input; wait 91 game seconds.',action:()=>null,chanceUpper:0},
];
for(const policy of policies){
  const result={name:policy.name,note:policy.note,games:200,firstCorrect:0,firstAttempts:0,presented:0,completed:0,pointerCallbacks:0,byMode:{},runs:[],chanceUpper:policy.chanceUpper};
  for(let i=1;i<=200;i++){
    const w=world(40000+i),r=rng(78000+i);let k=0;const seen=new Set();
    for(let guard=0;guard<600;guard++){
      const s=w.hook.getState(),t=w.hook.getInputTargets();if(['won','lost'].includes(s.phase))break;
      if(!seen.has(s.problemId)){seen.add(s.problemId);result.presented++;}
      if(s.phase==='feedback'){w.frame(1.3);continue;}
      const action=policy.action(t,k++,r);if(!action){w.frame(91);continue;}
      const firstBefore=s.firstAttempts,correctBefore=s.firstCorrect;w.drag(action.a,action.b);const after=w.hook.getState();
      if(after.firstAttempts>firstBefore){const row=result.byMode[t.mode]||(result.byMode[t.mode]={firstAttempts:0,firstCorrect:0});row.firstAttempts+=after.firstAttempts-firstBefore;row.firstCorrect+=after.firstCorrect-correctBefore;}
      w.frame(.25);
    }
    const s=w.hook.getState();if(!['won','lost'].includes(s.phase))report.errors.push('No terminal '+policy.name+' '+i);
    result.firstAttempts+=s.firstAttempts;result.firstCorrect+=s.firstCorrect;result.completed+=Number(s.phase==='won');result.pointerCallbacks+=w.eventCount;result.runs.push({seed:40000+i,phase:s.phase,solved:s.solved,lives:s.lives,firstAttempts:s.firstAttempts,firstCorrect:s.firstCorrect,elapsed:s.elapsed});
  }
  result.firstRate=result.firstAttempts?result.firstCorrect/result.firstAttempts:0;result.completionRate=result.completed/200;result.z=result.chanceUpper&&result.firstAttempts?(result.firstCorrect-result.firstAttempts*result.chanceUpper)/Math.sqrt(result.firstAttempts*result.chanceUpper*(1-result.chanceUpper)):0;
  result.rawAtOrBelowChance=result.firstRate<=result.chanceUpper;result.chanceFormula=result.chanceUpper?policy.name==='random'?'8° / 360°; random length and endpoint constraints reduce this bound':'80 orientations / 1800 orientations; endpoint and fixed length constraints reduce this bound':'0';
  if(Object.keys(result.byMode).some(m=>!['point','pair','error'].includes(m)))report.errors.push('Additional conditional chance model needed '+policy.name);
  if(result.z>1.645)report.errors.push('Significant excess '+policy.name);
  report.bots.push(result);
}
report.verdict=report.errors.length?'fail':'pass';fs.writeFileSync('logs/pane-wipe-build/audit-pointer-bots.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,bots:report.bots.map(x=>({...x,runs:undefined}))},null,2));if(report.errors.length)process.exitCode=1;
