// Art round: actual WebGL frames and complete per-Unity-loop draw accounting.
// Capture navigation uses existing test commands, actual pointer pours are recorded separately.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../../lib/static-server.mjs';
const dir=path.dirname(new URL(import.meta.url).pathname),tag=process.argv[2]||'round1';
if(!/^[a-z0-9-]+$/.test(tag))throw Error('Invalid tag');
const out=path.join(dir,tag);fs.mkdirSync(out,{recursive:true});
const hash=f=>crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const packHashes=()=>Object.fromEntries(['index.json','m2s2-u6.json','m2s2-u7.json'].map(f=>[f,hash('public/g/hyeopgok-sasu/packs/'+f)]));
const packsAtStart=packHashes();
const chrome=process.env.PUPPETEER_EXECUTABLE_PATH||path.join(process.env.HOME,'.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const server=await serveStatic(path.join(process.cwd(),'public'));
const browser=await puppeteer.launch({headless:true,executablePath:chrome,args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage(),cdp=await page.createCDPSession(),errors=[],requests=[],frames=[],metrics=[];
page.on('pageerror',e=>errors.length<20&&errors.push(String(e)));page.on('console',m=>{if(m.type()==='error')errors.length<20&&errors.push(m.text());});page.on('requestfailed',r=>requests.push({url:r.url(),error:r.failure()?.errorText}));
await page.evaluateOnNewDocument(()=>{
 const a=window.__ART_DRAW__={total:0,triangles:0,count:0,at:0,calls:new Uint16Array(16384),tris:new Uint32Array(16384),hooked:false};
 document.addEventListener('load',e=>{
  if(e.target.tagName!=='SCRIPT'||!e.target.src.includes('.loader.js'))return;
  const create=window.createUnityInstance;if(!create)return;
  window.createUnityInstance=function(...args){return create.apply(this,args).then(instance=>{
   const m=instance.Module,pre=m.preMainLoop,post=m.postMainLoop;let before=0,tris=0;
   m.preMainLoop=function(){before=a.total;tris=a.triangles;return pre?.apply(this,arguments);};
   m.postMainLoop=function(){post?.apply(this,arguments);const n=a.total-before;if(n){a.calls[a.at]=n;a.tris[a.at]=a.triangles-tris;a.at=(a.at+1)%16384;a.count=Math.min(16384,a.count+1);}};
   a.hooked=true;return instance;
  });};
 },true);
 for(const p of [WebGLRenderingContext.prototype,WebGL2RenderingContext.prototype])for(const key of ['drawArrays','drawElements','drawArraysInstanced','drawElementsInstanced']){
  const d=Object.getOwnPropertyDescriptor(p,key);if(!d)continue;const f=d.value;
  p[key]=function(...args){a.total++;if(args[0]===4){const n=key.includes('Elements')?args[1]:args[2],instances=key.endsWith('Instanced')?args[args.length-1]:1;a.triangles+=Math.floor(n/3)*instances;}return Reflect.apply(f,this,args);};
 }
});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
async function frame(name){const file=name+'.png';await page.screenshot({path:path.join(out,file)});frames.push({file,state:await state()});console.log('frame '+file);}
async function record(label){metrics.push({label,...await page.evaluate(()=>{
 const a=window.__ART_DRAW__,c=Array.from(a.calls.subarray(0,a.count)).sort((x,y)=>x-y),t=Array.from(a.tris.subarray(0,a.count)).sort((x,y)=>x-y);
 const p=window.__HYEOPGOK_ART_PROBE__;const managedAllocatedBytes=p?Object.fromEntries(['counts','zero','totals','max','positive'].map(k=>[k,Array.from(p[k])])):null;
 const histogram={};if(p)for(const x of p.frames.subarray(0,Math.min(8192,p.counts[0])))histogram[x]=(histogram[x]||0)+1;
 return {managedAllocatedBytes,gameUpdateAllocationHistogram:histogram,engineHooked:a.hooked,samples:a.count,draws:{min:c[0],median:c[Math.floor(c.length/2)],p95:c[Math.floor(c.length*.95)],max:c.at(-1)},trianglesIncludingShadows:{median:t[Math.floor(t.length/2)],max:t.at(-1)}};
})});}
async function until(fn,ms=10000){let s;const end=Date.now()+ms;do{s=await state();if(fn(s))return s;await sleep(30);}while(Date.now()<end);throw Error('state wait '+JSON.stringify(s));}
async function tapNorm(pair,w,h,index=0){await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x:pair[index*2]*w,y:pair[index*2+1]*h,id:1}]});await sleep(80);await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await sleep(80);}
try{
 for(const [label,w,h,mobile] of [['390',390,844,true],['1280',1280,800,false]]){
  await page.setViewport({width:w,height:h,deviceScaleFactor:1,isMobile:mobile,hasTouch:true});
  await page.goto(server.url+'/g/hyeopgok-sasu/?pack=m2s2-u7&artprobe=1',{waitUntil:'networkidle0',timeout:60000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});await sleep(400);
  await frame('title-'+label);
  await tapNorm([(w/h>1.2?.23:.5)-108*h/(844*w),w/h>1.2?.83:.85],w,h);await sleep(250);await frame('packs-'+label);
  await tapNorm([.5+(Math.min(370,844*w/h-16)/2-28)*h/(844*w),.5-268/844],w,h);await sleep(100);
  await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(2000);await frame('battle-'+label);
  await record(label+'-startup');
  await page.evaluate(()=>{const p=window.__HYEOPGOK_ART_PROBE__;if(p){for(const k of ['counts','zero','totals','max','positive'])p[k].fill(0);p.at=0;}const a=window.__ART_DRAW__;a.count=a.at=0;});
  await sleep(15000);await record(label+'-steady15s');
  let s=await until(s=>s.coins>=2);await tapNorm(s.padScreen,w,h);await until(s=>s.poured[0]>=1);await frame('pour-'+label);
  s=await state();if(s.poured[0]<2){await tapNorm(s.padScreen,w,h);await until(s=>s.poured[0]===2);}
  s=await state();await tapNorm(s.exitScreen,w,h);await until(s=>s.attempts>=1);await frame('build-'+label);await sleep(1700);
  await frame('fraction-'+label);
  for(let i=0;i<9;i++){
   s=await state();if(s.phase!=='playing')break;
   await page.evaluate(()=>window.__GAME_TEST__.answerCorrect());
   if(s.answerMode==='fraction_parts')await frame('assembled-'+label+'-'+s.level);
   if(i===3){await sleep(1400);await frame('battle-mid-'+label);}
   if(i===7){await sleep(1800);await frame('battle-grown-'+label);}
   await sleep(1700);
  }
  await frame('victory-'+label);await record(label);
 }
}catch(e){errors.length<20&&errors.push(String(e));await frame('failure').catch(()=>{});process.exitCode=1;}
finally{
 const build=Object.fromEntries(fs.readdirSync('public/g/hyeopgok-sasu/Build').map(f=>[f,hash('public/g/hyeopgok-sasu/Build/'+f)]));
 const packsAtEnd=packHashes(),packsStable=JSON.stringify(packsAtStart)===JSON.stringify(packsAtEnd);
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({tag,time:new Date().toISOString(),build,packsAtStart,packsAtEnd,packsStable,metrics,frames,errors,requests},null,2)+'\n');console.log(JSON.stringify({out,metrics,errors,requests,packsStable}));await browser.close();await server.close();
}
