import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../../lib/static-server.mjs';
const out=path.dirname(new URL(import.meta.url).pathname),wait=ms=>new Promise(r=>setTimeout(r,ms));
const server=await serveStatic(path.join(process.cwd(),'public'));
const browser=await puppeteer.launch({headless:true,executablePath:process.env.PUPPETEER_EXECUTABLE_PATH||path.join(process.env.HOME,'.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'),args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage(),cdp=await page.createCDPSession(),errors=[],rows=[];
page.on('pageerror',e=>errors.push(String(e)));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
await page.evaluateOnNewDocument(()=>{
  const m=window.__EDGES__={count:0,total:0,engineFrames:[],engineHooked:false,callbackDraws:[],insideRaf:false,outsideRafDraws:0,outsideGroups:[],outsidePending:false,outsideCount:0,max:0,colorClears:0,clearDetails:[],normalClears:[],drawSinceClear:0,renderCounts:[],spikes:[],phase:"loading",phases:{}};
  document.addEventListener('load',event=>{
    if(event.target.tagName!=='SCRIPT'||!event.target.src.includes('.loader.js'))return;
    const create=window.createUnityInstance;if(!create)return;
    window.createUnityInstance=function(...args){return create.apply(this,args).then(instance=>{
      const module=instance.Module,pre=module.preMainLoop,post=module.postMainLoop;let before=0;
      module.preMainLoop=function(){before=m.total;return pre?.apply(this,arguments);};
      module.postMainLoop=function(){post?.apply(this,arguments);const calls=m.total-before;if(calls)m.engineFrames.push({phase:m.phase,calls});};
      m.engineHooked=true;return instance;
    });};
  },true);
  const raf=window.requestAnimationFrame.bind(window);
  window.requestAnimationFrame=function(callback){return raf(function(t){const before=m.total;m.insideRaf=true;try{return callback(t);}finally{m.insideRaf=false;const n=m.total-before;if(n)m.callbackDraws.push({phase:m.phase,calls:n});}});};
  for(const p of [WebGLRenderingContext.prototype,WebGL2RenderingContext.prototype])for(const key of ['drawArrays','drawElements','drawArraysInstanced','drawElementsInstanced']){
    const d=Object.getOwnPropertyDescriptor(p,key);if(!d)continue;const f=d.value;
    p[key]=function(...a){m.count++;m.total++;m.drawSinceClear++;if(!m.insideRaf){m.outsideRafDraws++;m.outsideCount++;if(!m.outsidePending){m.outsidePending=true;queueMicrotask(()=>{m.outsideGroups.push({phase:m.phase,calls:m.outsideCount});m.outsideCount=0;m.outsidePending=false;});}}return Reflect.apply(f,this,a);};
  }
  for(const p of [WebGLRenderingContext.prototype,WebGL2RenderingContext.prototype]){
    const d=Object.getOwnPropertyDescriptor(p,'clear');if(!d)continue;const f=d.value;
    p.clear=function(mask){m.clearDetails.push({mask,draws:m.drawSinceClear});if(mask&16384){m.colorClears++;if(m.drawSinceClear)m.renderCounts.push({phase:m.phase,calls:m.drawSinceClear});m.drawSinceClear=0;}return Reflect.apply(f,this,[mask]);};
  }
  function sample(){if(m.count>150)m.spikes.push({phase:m.phase,calls:m.count,colorClears:m.colorClears,clearDetails:m.clearDetails});else if(m.count&&m.normalClears.length<12)m.normalClears.push({calls:m.count,clearDetails:m.clearDetails});m.colorClears=0;m.clearDetails=[];m.max=Math.max(m.max,m.count);const a=m.phases[m.phase]||(m.phases[m.phase]=[]);if(m.count)a.push(m.count);m.count=0;requestAnimationFrame(sample);}requestAnimationFrame(sample);
});
try{
  for(const width of [390,1280]){
    const height=width===390?844:800;
    await page.setViewport({width,height,deviceScaleFactor:1,isMobile:width===390,hasTouch:true});
    await page.goto(server.url+'/g/hyeopgok-sasu/',{waitUntil:'networkidle0'});await page.waitForFunction(()=>window.__GAME_TEST__?.ready);
    await page.evaluate(()=>{window.__EDGES__.phase='start';window.__GAME_TEST__.start();});
    for(let i=0;i<4;i++){await page.evaluate(i=>{window.__EDGES__.phase='wrong-'+i;window.__GAME_TEST__.answerWrong();},i);await wait(1800);}
    await page.evaluate(()=>{window.__EDGES__.phase='fire';});await wait(3000);await page.screenshot({path:path.join(out,'fire-'+width+'.png')});
    const damaged=await page.evaluate(()=>window.__GAME_TEST__.getState());
    if(damaged.hp!==28)throw Error('Unexpected existing gate damage rule');
    await page.evaluate(()=>{window.__EDGES__.phase='ending';});
    await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:width*.65,y:height*.7,id:1}]});await wait(100);
    await page.evaluate(()=>{window.__GAME_TEST__.answerWrong();window.__GAME_TEST__.answerWrong();});await wait(1900);
    await page.evaluate(()=>{window.__EDGES__.phase='restart';window.__GAME_TEST__.start();});await wait(400);
    const restarted=await page.evaluate(()=>window.__GAME_TEST__.getState());
    if(restarted.hp!==100||Math.abs(restarted.kingX+.6)>.001||Math.abs(restarted.kingZ+3.1)>.001)throw Error('Held drag leaked through restart');
    await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
    await page.screenshot({path:path.join(out,'restart-'+width+'.png')});
    rows.push({width,damaged,restarted,maxDraws:await page.evaluate(()=>window.__EDGES__.max),unityMainLoop:await page.evaluate(()=>{const m=window.__EDGES__,a=m.engineFrames;return {hooked:m.engineHooked,samples:a.length,max:Math.max(0,...a.map(x=>x.calls)),above150:a.filter(x=>x.calls>150)};}),perRender:await page.evaluate(()=>{const m=window.__EDGES__,a=m.callbackDraws;return {max:Math.max(...a.map(x=>x.calls)),samples:a.length,totalDraws:m.total,callbackDraws:a.reduce((s,x)=>s+x.calls,0),outsideRafDraws:m.outsideRafDraws,outsideGroups:m.outsideGroups,outsideMax:Math.max(0,...m.outsideGroups.map(x=>x.calls)),above150:a.filter(x=>x.calls>150)};}),spikes:await page.evaluate(()=>window.__EDGES__.spikes),normalClears:await page.evaluate(()=>window.__EDGES__.normalClears),colorClearIntervals:await page.evaluate(()=>{const a=window.__EDGES__.renderCounts;return {max:Math.max(...a.map(x=>x.calls)),above150:a.filter(x=>x.calls>150),samples:a.length};}),phases:await page.evaluate(()=>Object.fromEntries(Object.entries(window.__EDGES__.phases).map(([k,a])=>[k,{samples:a.length,min:Math.min(...a),max:Math.max(...a),p95:a.sort((a,b)=>a-b)[Math.floor(a.length*.95)]}])))});
  }
  for(const row of rows){if(!row.unityMainLoop.hooked||row.unityMainLoop.samples<100||row.unityMainLoop.max>150)throw Error('Unity loop gate failed');if(row.perRender.max>150||row.perRender.outsideMax>150)throw Error('Per-render draw-call gate exceeded');if(row.perRender.callbackDraws+row.perRender.outsideGroups.reduce((s,x)=>s+x.calls,0)!==row.perRender.totalDraws)throw Error('Unaccounted draw calls');}
  if(errors.length)throw Error(errors.join('\n'));
  const report={pass:true,method:'Every draw is counted. Unity Module preMainLoop/postMainLoop brackets the engine render frame. RAF callback deltas measure Unity render submissions; non-RAF synchronous draw tasks are separately grouped by microtask boundary. Aggregate RAF sample spikes are retained, not treated as single engine frames.',rows,errors};fs.writeFileSync(path.join(out,'edgecases.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({pass:true,rows:rows.map(r=>({width:r.width,max:r.maxDraws,unityMainLoop:r.unityMainLoop,perRender:r.perRender,phases:r.phases,spikes:r.spikes,normalClears:r.normalClears,colorClearIntervals:r.colorClearIntervals})),errors}));
}finally{await browser.close();await server.close();}
