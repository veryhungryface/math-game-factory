import fs from 'node:fs';
import path from 'node:path';
import {pathToFileURL} from 'node:url';
import puppeteer from 'puppeteer';
import {executablePath,survivalArgs} from './browser-pipe.mjs';
const out=path.resolve('logs/jareul-beollyeo-build/browser-perf-probe.json');
const report={at:new Date().toISOString(),mode:'Single-process shell without disable-gpu; no assertion or FPS threshold changed',samples:[],errors:[]};
let browser;
try{
 browser=await puppeteer.launch({headless:'shell',pipe:true,executablePath,timeout:15000,args:survivalArgs.filter(a=>!['--disable-gpu','--disable-software-rasterizer'].includes(a))});
 const page=await browser.newPage();await page.setViewport({width:390,height:844,deviceScaleFactor:1.5,isMobile:true,hasTouch:true});
 page.on('pageerror',e=>report.errors.push(String(e)));page.on('console',m=>{if(m.type()==='error')report.errors.push(m.text())});
 await page.goto(pathToFileURL(path.resolve('public/g/jareul-beollyeo/index.html')).href,{waitUntil:'load'});
 await page.waitForFunction(()=>window.__GAME_TEST__?.ready);
 report.renderer=await page.evaluate(()=>{const c=document.createElement('canvas'),gl=c.getContext('webgl2')||c.getContext('webgl');if(!gl)return null;const d=gl.getExtension('WEBGL_debug_renderer_info');return String(d?gl.getParameter(d.UNMASKED_RENDERER_WEBGL):gl.getParameter(gl.RENDERER))});
 await page.click('#start');
 const measure=()=>page.evaluate(()=>new Promise(r=>{const start=performance.now();let f=0;const step=()=>{f++;const t=performance.now()-start;if(t<2500)requestAnimationFrame(step);else r({fps:Math.round(f*1000/t),frames:f,ms:t})};requestAnimationFrame(step)}));
 for(let i=0;i<3;i++)report.samples.push(await measure());
 await new Promise(r=>setTimeout(r,15000));report.after15sec=await measure();
 await page.setViewport({width:1280,height:800,deviceScaleFactor:1.5});report.wide=await measure();
}catch(e){report.failure=String(e)}finally{if(browser)await browser.close();fs.writeFileSync(out,JSON.stringify(report,null,2)+'\n')}
console.log(JSON.stringify(report,null,2));
