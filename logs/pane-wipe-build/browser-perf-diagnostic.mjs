// Diagnostic only: transient visibility/drawing ablations locate cost. These
// samples are never QA scores and never alter saved gameplay or factory gates.
import fs from 'node:fs';
import puppeteer from './browser-pipe.mjs';
import {url,hashes,wait} from './browser-utils.mjs';
const report={at:new Date().toISOString(),hashes:hashes(),diagnosticOnly:true,samples:[]};
const browser=await puppeteer.launch({timeout:15000});
try{
 const page=await browser.newPage();await page.setViewport({width:1280,height:800,deviceScaleFactor:1});await page.goto(url,{waitUntil:'load'});await page.waitForFunction(()=>window.__GAME_TEST__?.ready);await page.click('#startBtn');await wait(1200);
 const cdp=await page.createCDPSession();await cdp.send('Performance.enable');
 const metrics=async()=>Object.fromEntries((await cdp.send('Performance.getMetrics')).metrics.map(x=>[x.name,x.value]));
 async function sample(label){const before=await metrics();const fps=await page.evaluate(()=>new Promise(resolve=>{const t=performance.now();let n=0;function frame(now){n++;if(now-t>=3000)resolve({fps:n*1000/(now-t),frames:n,seconds:(now-t)/1000});else requestAnimationFrame(frame);}requestAnimationFrame(frame);}));const after=await metrics();const m={};for(const k of ['TaskDuration','ScriptDuration','LayoutDuration','RecalcStyleDuration','LayoutCount','RecalcStyleCount'])m[k]=after[k]-before[k];const row={label,...fps,metrics:m};report.samples.push(row);console.log(JSON.stringify(row));}
 await sample('original');
 await page.evaluate(()=>document.getElementById('game').style.visibility='hidden');await sample('canvas-hidden-drawing-still-runs');
 await page.evaluate(()=>{document.getElementById('game').style.visibility='visible';document.getElementById('wall').style.display='none';});await sample('wall-hidden');
 await page.evaluate(()=>{document.getElementById('wall').style.display='';window.__diagnosticDrawing={};for(const k of ['fill','stroke','fillRect','strokeRect','clearRect','drawImage','fillText']){window.__diagnosticDrawing[k]=CanvasRenderingContext2D.prototype[k];CanvasRenderingContext2D.prototype[k]=function(){};}});await sample('drawing-skipped-diagnostic-only');
 await page.evaluate(()=>{Object.assign(CanvasRenderingContext2D.prototype,window.__diagnosticDrawing);});await sample('restored');
}finally{await browser.close();fs.writeFileSync('logs/pane-wipe-build/browser-perf-diagnostic.json',JSON.stringify(report,null,2)+'\n');}
