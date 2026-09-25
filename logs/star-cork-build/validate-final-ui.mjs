import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';
import puppeteer from './puppeteer-pipe.mjs';
const out='logs/star-cork-build/final-ui';fs.mkdirSync(out,{recursive:true});
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const report={at:new Date().toISOString(),htmlSha256:hash('public/g/star-cork/index.html'),engineSha256:hash('public/g/star-cork/engine.js'),errors:[],requests:[],shots:[],checks:[]};
const sleep=ms=>new Promise(r=>setTimeout(r,ms));let browser;
try{
browser=await puppeteer.launch();const page=await browser.newPage();
page.on('console',m=>{if(m.type()==='error')report.errors.push(m.text());});page.on('pageerror',e=>report.errors.push(String(e)));page.on('requestfailed',r=>report.errors.push(r.failure()?.errorText+' '+r.url()));page.on('request',r=>report.requests.push(r.url()));
await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
await page.goto(pathToFileURL(path.resolve('public/g/star-cork/index.html')).href,{waitUntil:'load'});await page.waitForFunction(()=>window.__GAME_TEST__?.ready);
await page.evaluate(()=>{window.__finalPointer=[];document.addEventListener('pointerdown',e=>window.__finalPointer.push({trusted:e.isTrusted,x:e.clientX,y:e.clientY}),true);});
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
const shot=async name=>{await page.screenshot({path:path.join(out,name+'.png')});report.shots.push(name);};
const tapTarget=async id=>{const t=await page.evaluate(id=>{const r=document.querySelector('canvas').getBoundingClientRect(),t=window.__STAR_UI__.getTargets().belt.find(x=>x.id===id);return t?{x:t.x+r.x,y:t.y+r.y}:null;},id);assert(t);await page.mouse.click(t.x,t.y);await sleep(130);};
async function tutorial(){for(let i=0;i<3;i++){const s=await state();const c=s.candies.find(x=>x.status==='belt');await tapTarget(c.id);}await page.waitForFunction(()=>window.__GAME_TEST__.getState().orderIndex===0&&window.__GAME_TEST__.getState().phase==='playing');}
await shot('title-390');const titleMetrics=await page.evaluate(()=>{const box=id=>{const r=document.getElementById(id).getBoundingClientRect();return{w:r.width,h:r.height,top:r.top,bottom:r.bottom};};return{start:box('start'),help:box('titleHelp'),bestHidden:document.getElementById('best').hidden};});assert(titleMetrics.start.h>=64);assert(titleMetrics.help.h>=44);assert(titleMetrics.bestHidden);await page.click('#titleHelp');await sleep(80);assert.equal(await page.$eval('#help',el=>el.hidden),false);assert.equal(await page.$eval('#titleScreen',el=>el.hidden),false);await shot('title-help-390');await page.click('#closeHelp');assert.equal(await page.$eval('#help',el=>el.hidden),true);report.checks.push({name:'title CTA/help sizes, zero-record hiding, and help round trip',pass:true,titleMetrics});await page.click('#start');await sleep(120);await shot('guide-390');await sleep(1280);await shot('guide-route-390');
await page.mouse.click(375,592);await sleep(150);await shot('invalid-390');
await tutorial();const before=await state(),wallStart=Date.now();await sleep(5000);const after=await state();const wallSeconds=(Date.now()-wallStart)/1000;
report.timer={wallSeconds,gameSeconds:after.elapsed-before.elapsed,beforeRemaining:before.remaining,afterRemaining:after.remaining};assert(Math.abs(report.timer.gameSeconds-wallSeconds)<.6,JSON.stringify(report.timer));report.checks.push({name:'real wall clock drives 90 second timer',pass:true});
// The public start hook skips title/onboarding and enters the first ordinary order.
await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(120);assert.equal((await state()).frozen,false);assert.equal((await state()).orderIndex,0);
let clicks=0;const target=(await state()).problem.target;
function value(type,p){return type==='big'?p.legendBig:type==='half'?p.legendBig/2:p.legendSmall;}
function desired(s){const dp=new Map([[0,[]]]);for(const c of s.candies.filter(c=>c.status!=='gone'))for(const [n,ids]of [...dp].sort((a,b)=>b[0]-a[0])){const sum=n+value(c.type,s.problem);if(sum<=target&&!dp.has(sum))dp.set(sum,[...ids,c.id]);}return dp.get(target);}
const started=Date.now();let equalityWasSilent=false;
while((await state()).solved===0&&Date.now()-started<18000){const s=await state();assert.equal(s.lives,3);const ids=desired(s);assert(ids,'first order remains attainable');const c=s.candies.find(x=>ids.includes(x.id)&&x.status==='belt'&&x.progress>=0&&x.progress<=1);if(c){await tapTarget(c.id);clicks++;}else await sleep(45);const latest=await state();const total=latest.plate.reduce((n,id)=>n+value(latest.candies.find(c=>c.id===id).type,latest.problem),0);if(total===target&&latest.phase==='playing'&&!equalityWasSilent){equalityWasSilent=true;await shot('equal-value-before-wave-end');}}
const completed=await state();assert.equal(completed.solved,1);assert.equal(completed.lives,3);assert(equalityWasSilent,'equal amount was held silently until the finite wave ended');report.order={clicks,target,elapsed:completed.elapsed,phase:completed.phase,score:completed.score,silentEquality:equalityWasSilent};await shot('first-order-delivered');
report.checks.push({name:'trusted taps collect correct value; ordinary equality waits until wave end',pass:true});
await page.waitForFunction(()=>window.__GAME_TEST__.getState().orderIndex===1&&window.__GAME_TEST__.getState().phase==='playing');
for(const [width,height]of [[320,720],[820,1180],[1280,800],[2000,1000]]){await page.setViewport({width,height,deviceScaleFactor:1,isMobile:true,hasTouch:true});await page.waitForFunction(w=>window.__STAR_UI__.getLayout().belt.w===Math.min(w,1280),{},width);const layout=await page.evaluate(()=>({innerWidth,overflow:document.documentElement.scrollWidth>innerWidth,land:document.documentElement.classList.contains('land'),layout:window.__STAR_UI__.getLayout()}));assert(!layout.overflow);if(width>=1024)assert(layout.land);report.checks.push({name:`layout ${width}x${height}`,pass:true,layout});await shot(`layout-${width}`);}
await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});await page.evaluate(()=>window.__GAME_TEST__.start());for(let i=0;i<3;i++)await page.evaluate(()=>window.__GAME_TEST__.answerWrong());let terminal=await state();assert.equal(terminal.lives,0);assert.equal(terminal.phase,'feedback');assert.equal(await page.$eval('#resultScreen',el=>el.hidden),true);await shot('terminal-answer-reveal');await sleep(1400);terminal=await state();assert.equal(terminal.phase,'lost');assert.equal(await page.$eval('#resultScreen',el=>el.hidden),false);report.checks.push({name:'third wrong keeps 1.15s answer reveal before terminal result',pass:true});
report.pointerEvents=await page.evaluate(()=>window.__finalPointer);assert(report.pointerEvents.every(x=>x.trusted));report.externalRequests=report.requests.filter(x=>/^(https?|wss?):/.test(x));assert.deepEqual(report.errors,[]);assert.deepEqual(report.externalRequests,[]);report.pass=true;
}catch(e){report.pass=false;report.error=e.stack;}finally{if(browser)await browser.close();fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2)+'\n');}
console.log(JSON.stringify(report,null,2));if(!report.pass)process.exitCode=1;
