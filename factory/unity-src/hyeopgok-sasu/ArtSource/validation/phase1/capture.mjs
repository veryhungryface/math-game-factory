// Actual WebGL renders + draw-call instrumentation; never changes game rules/state.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../../lib/static-server.mjs';

const root=process.cwd(), dir=path.dirname(new URL(import.meta.url).pathname);
const tag=process.argv[2]||'final';
if(!/^[a-z0-9-]+$/.test(tag))throw Error('Invalid output label');
const out=path.join(dir,tag);fs.mkdirSync(out,{recursive:true});
const server=await serveStatic(path.join(root,'public'));
const browser=await puppeteer.launch({headless:true,executablePath:process.env.PUPPETEER_EXECUTABLE_PATH||path.join(process.env.HOME,'.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'),args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage(),errors=[],requests=[],frames=[],draws=[];
page.on('pageerror',e=>errors.push(String(e)));
page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
page.on('requestfailed',r=>requests.push({url:r.url(),error:r.failure()?.errorText}));
await page.evaluateOnNewDocument(()=>{
  const m=window.__DRAW_AUDIT__={calls:0,triangles:0,at:0,count:0,samples:new Uint32Array(8192),tris:new Uint32Array(8192)};
  for(const p of [window.WebGLRenderingContext?.prototype,window.WebGL2RenderingContext?.prototype]){
    if(!p)continue;
    for(const name of ['drawArrays','drawElements','drawArraysInstanced','drawElementsInstanced']){
      const descriptor=Object.getOwnPropertyDescriptor(p,name);
      if(!descriptor||typeof descriptor.value!=='function')continue;
      const original=descriptor.value;
      p[name]=function(...args){
        m.calls++;
        const count=name.includes('Elements')?args[1]:args[2];
        const instances=name.endsWith('Instanced')?args[args.length-1]:1;
        if(args[0]===4)m.triangles+=Math.floor(count/3)*instances;
        return Reflect.apply(original,this,args);
      };
    }
  }
  function sample(){
    if(m.calls){m.samples[m.at]=m.calls;m.tris[m.at]=m.triangles;m.at=(m.at+1)%8192;m.count=Math.min(m.count+1,8192);}
    m.calls=m.triangles=0;requestAnimationFrame(sample);
  }
  requestAnimationFrame(sample);
});
const wait=ms=>new Promise(r=>setTimeout(r,ms));
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
async function capture(name){
  const file=name+'.png';await page.screenshot({path:path.join(out,file)});
  frames.push({file,state:await state()});
}
async function open(label,w,h,mobile){
  await page.setViewport({width:w,height:h,isMobile:mobile,hasTouch:mobile,deviceScaleFactor:1});
  await page.goto(server.url+'/g/hyeopgok-sasu/',{waitUntil:'networkidle0',timeout:60000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
  await wait(1000);await capture('title-'+label);
  await page.evaluate(()=>window.__GAME_TEST__.start());await wait(5000);await capture('battle-'+label);
}
async function metrics(label){
  draws.push({label,...await page.evaluate(()=>{
    const m=window.__DRAW_AUDIT__,a=Array.from(m.samples.subarray(0,m.count)).sort((a,b)=>a-b),t=Array.from(m.tris.subarray(0,m.count)).sort((a,b)=>a-b);
    return {samples:m.count,min:a[0],median:a[Math.floor(a.length*.5)],p95:a[Math.floor(a.length*.95)],max:a.at(-1),trianglesMaxIncludingShadows:t.at(-1)};
  })});
}
try{
  for(const [label,w,h,mobile] of [['390',390,844,true],['1280',1280,800,false],['820',820,1180,true]]){
    await open(label,w,h,mobile);
    if(tag!=='before'){
      await page.evaluate(()=>window.__GAME_TEST__.answerCorrect());await wait(350);await capture('build-'+label);
      await wait(1550);
      for(let wave=2;wave<=9;wave++){
        await page.evaluate(()=>window.__GAME_TEST__.answerCorrect());
        if(wave===3||wave===6){await wait(460);await capture('night-'+wave+'-'+label);await wait(1150);}else await wait(1650);
        if(wave===4||wave===8)await capture('growth-'+wave+'-'+label);
      }
      await wait(2500);await capture('boss-'+label);
      await page.evaluate(()=>window.__GAME_TEST__.answerWrong());await wait(400);await capture('damage-'+label);
    }
    await metrics(label);
  }
  if(tag!=='before')for(const [label,w,h] of [['cover-wide',1200,630],['cover-square',1080,1080]]){
    await open(label,w,h,false);
    for(let i=0;i<5;i++){await page.evaluate(()=>window.__GAME_TEST__.answerCorrect());await wait(1650);}
    await wait(2100);await capture('hero-'+label);await metrics(label);
  }
  const renderer=await page.evaluate(()=>{const c=document.querySelector('canvas'),g=c.getContext('webgl2')||c.getContext('webgl'),d=g.getExtension('WEBGL_debug_renderer_info');return d?g.getParameter(d.UNMASKED_RENDERER_WEBGL):'unknown';});
  const hashes=Object.fromEntries(fs.readdirSync('public/g/hyeopgok-sasu/Build').map(f=>[f,crypto.createHash('sha256').update(fs.readFileSync('public/g/hyeopgok-sasu/Build/'+f)).digest('hex')]));
  fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({tag,time:new Date().toISOString(),renderer,hashes,draws,errors,requests,frames},null,2)+'\n');
  console.log(JSON.stringify({out,renderer,draws,errors,requests,frames:frames.length}));
}finally{await browser.close();await server.close();}
