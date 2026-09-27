// Art round 2 final evidence: actual WebGL frames at the judge's timings plus
// title, pack browser, real-touch pour (coin arcs), fraction two pads, a geometry
// pack question with ∠△° symbols and the victory card. Packs are read-only; the
// geometry shot uses an HTTP fixture that only moves one unedited item to slot 0.
// Usage (repo root): node factory/unity-src/hyeopgok-sasu/ArtSource/validation/art-r2/final-capture.mjs [tag]
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../../lib/static-server.mjs';
const dir=path.dirname(new URL(import.meta.url).pathname),tag=process.argv[2]||'final';
if(!/^[a-z0-9-]+$/.test(tag))throw Error('Invalid tag');
const only=(process.env.ONLY||'').split(',').filter(Boolean);
const out=path.join(dir,tag);fs.mkdirSync(out,{recursive:true});
const game='public/g/hyeopgok-sasu',packDir=game+'/packs';
const hash=f=>crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const packHashes=()=>Object.fromEntries(fs.readdirSync(packDir).filter(f=>f.endsWith('.json')).sort().map(f=>[f,hash(packDir+'/'+f)]));
const packsAtStart=packHashes();
const chrome=process.env.PUPPETEER_EXECUTABLE_PATH||path.join(process.env.HOME,'.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const server=await serveStatic(path.join(process.cwd(),'public'));
const browser=await puppeteer.launch({headless:true,executablePath:chrome,args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage(),cdp=await page.createCDPSession(),errors=[],requests=[],frames=[],metrics=[];
let overlay=null,overlayId='';
await page.setRequestInterception(true);
page.on('request',r=>{if(overlay&&r.url().split('?')[0].endsWith('/packs/'+overlayId+'.json'))r.respond({status:200,contentType:'application/json',body:JSON.stringify(overlay)});else r.continue();});
page.on('pageerror',e=>errors.length<20&&errors.push(String(e)));page.on('console',m=>{if(m.type()==='error')errors.length<20&&errors.push(m.text());});
page.on('requestfailed',r=>requests.push({url:r.url(),error:r.failure()?.errorText}));
page.on('response',r=>{if(r.status()>=400)requests.push({url:r.url(),status:r.status()});});
await page.evaluateOnNewDocument(()=>{
 const a=window.__ART_DRAW__={total:0,count:0,at:0,calls:new Uint16Array(16384),hooked:false};
 document.addEventListener('load',e=>{
  if(e.target.tagName!=='SCRIPT'||!e.target.src.includes('.loader.js'))return;
  const create=window.createUnityInstance;if(!create)return;
  window.createUnityInstance=function(...args){return create.apply(this,args).then(instance=>{
   const m=instance.Module,pre=m.preMainLoop,post=m.postMainLoop;let before=0;
   m.preMainLoop=function(){before=a.total;return pre?.apply(this,arguments);};
   m.postMainLoop=function(){post?.apply(this,arguments);const n=a.total-before;if(n){a.calls[a.at]=n;a.at=(a.at+1)%16384;a.count=Math.min(16384,a.count+1);}};
   a.hooked=true;return instance;
  });};
 },true);
 for(const p of [WebGLRenderingContext.prototype,WebGL2RenderingContext.prototype])for(const key of ['drawArrays','drawElements','drawArraysInstanced','drawElementsInstanced']){
  const d=Object.getOwnPropertyDescriptor(p,key);if(!d)continue;const f=d.value;
  p[key]=function(...args){a.total++;return Reflect.apply(f,this,args);};
 }
});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
async function frame(name,note){const file=name+'.png';await page.screenshot({path:path.join(out,file)});frames.push({file,note,state:await state().catch(()=>null)});console.log('frame '+file);}
function resetProbe(){return page.evaluate(()=>{const p=window.__HYEOPGOK_ART_PROBE__;if(p){for(const k of ['counts','zero','totals','max','positive'])p[k].fill(0);p.at=0;}const a=window.__ART_DRAW__;a.count=a.at=0;});}
async function record(label){metrics.push({label,...await page.evaluate(()=>{
 const a=window.__ART_DRAW__,c=Array.from(a.calls.subarray(0,a.count)).sort((x,y)=>x-y);
 const p=window.__HYEOPGOK_ART_PROBE__;const managedAllocatedBytes=p?Object.fromEntries(['counts','zero','totals','max','positive'].map(k=>[k,Array.from(p[k])])):null;
 return {managedAllocatedBytes,engineHooked:a.hooked,samples:a.count,draws:{min:c[0],median:c[Math.floor(c.length/2)],p95:c[Math.floor(c.length*.95)],max:c.at(-1)}};
})});}
async function until(fn,ms=15000){let s;const end=Date.now()+ms;do{s=await state();if(fn(s))return s;await sleep(30);}while(Date.now()<end);throw Error('state wait '+JSON.stringify(s));}
async function touch(type,x,y){await cdp.send('Input.dispatchTouchEvent',{type,touchPoints:type==='touchEnd'?[]:[{x,y,id:1}]});}
async function tapNorm(pair,w,h,index=0){await touch('touchStart',pair[index*2]*w,pair[index*2+1]*h);await sleep(80);await touch('touchEnd');await sleep(80);}
async function load(w,h,mobile,query){
 await page.setViewport({width:w,height:h,deviceScaleFactor:mobile?2:1,isMobile:mobile,hasTouch:true});
 await page.goto(server.url+'/g/hyeopgok-sasu/?'+query,{waitUntil:'networkidle0',timeout:90000});
 await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:90000});await sleep(500);
}
const want=k=>!only.length||only.includes(k);
try{
 for(const [label,w,h,mobile] of [['390',390,844,true],['1280',1280,800,false]]){
  const wide=w/h>1.2;
  if(want('judge')){
   await load(w,h,mobile,'pack=m2s2-u7&artprobe=1');
   await frame('title-'+label);
   await tapNorm([(wide?.23:.5)-108*h/(844*w),wide?.83:.85],w,h);await sleep(300);await frame('packs-'+label);
   await tapNorm([.5+(Math.min(370,844*w/h-16)/2-28)*h/(844*w),.5-268/844],w,h);await sleep(150);
   await page.evaluate(()=>window.__GAME_TEST__.start());const t0=Date.now();
   await sleep(3000);await frame('play-'+label+'-3s');
   await sleep(Math.max(0,t0+8000-Date.now()));await frame('play-'+label+'-8s');
   await sleep(Math.max(0,t0+15000-Date.now()));await frame('play-'+label+'-15s');
   await record(label+'-startup15s');
   await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(4000);await resetProbe();
   await sleep(15000);await record(label+'-steady15s');
  }
  if(want('pour')){
   // Real touch: press and hold on the coin pad. The king walks there, stops
   // and pours; frames catch the eight-disc arcs in flight.
   await load(w,h,mobile,'pack=m2s2-u6');await page.evaluate(()=>window.__GAME_TEST__.start());
   let s=await until(s=>s.coins>=8,40000);
   await touch('touchStart',s.padScreen[0]*w,s.padScreen[1]*h);
   await until(s=>s.poured[0]>=1,8000);await sleep(160);await frame('pour-'+label,'real touch hold, first disc');
   await until(s=>s.poured[0]>=3,8000);await sleep(110);await frame('pour-'+label+'-stream','real touch hold, 3+ discs');
   await touch('touchEnd');await sleep(200);
  }
  if(want('run')){
   await load(w,h,mobile,'pack=m2s2-u7');await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(2500);
   for(let i=0;i<10;i++){
    let s=await state();if(s.phase!=='playing')break;
    await page.evaluate(()=>window.__GAME_TEST__.answerCorrect());
    if(i===0){await sleep(1900);await frame('fraction-'+label,'second question: two semantic pads');await sleep(2600);await frame('fraction-'+label+'-folded','after reading time');continue;}
    if(i===3){await sleep(1400);await frame('play-'+label+'-after4correct');continue;}
    await sleep(1700);
   }
   await until(s=>s.phase!=='playing',15000).catch(()=>{});await sleep(1500);await frame('victory-'+label);
  }
  if(want('geo')){
   const pack=JSON.parse(fs.readFileSync(packDir+'/m2s2-u1.json'));
   const rank=q=>(q.prompt.match(/[∠△°]/g)||[]).length*10+(q.prompt.length>60&&q.prompt.length<110?5:0);
   const q=pack.items.reduce((a,b)=>rank(b)>rank(a)?b:a);overlay={...pack,items:[q,...pack.items.filter(x=>x.id!==q.id)]};overlayId=pack.pack_id;
   await load(w,h,mobile,'pack=m2s2-u1');await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(1200);
   await frame('geometry-'+label,{pack:pack.pack_id,id:q.id,prompt:q.prompt,choices:q.choices});
   if(!wide){await sleep(6500);await frame('geometry-'+label+'-folded');const a=await state();await tapNorm([.5,110/844],w,h);await sleep(300);await frame('geometry-'+label+'-tap-open');const b=await state();if(a.moves!==b.moves)errors.push('banner tap moved king');}
   overlay=null;
  }
 }
}catch(e){errors.push(String(e));await frame('failure').catch(()=>{});process.exitCode=1;}
finally{
 const build=Object.fromEntries(fs.readdirSync(game+'/Build').map(f=>[f,hash(game+'/Build/'+f)]));
 const packsAtEnd=packHashes(),packsStable=JSON.stringify(packsAtStart)===JSON.stringify(packsAtEnd);
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({tag,time:new Date().toISOString(),build,packsStable,metrics,frames,errors,requests},null,2)+'\n');
 console.log(JSON.stringify({out,metrics,errors,requests,packsStable}));await browser.close();await server.close();
}
