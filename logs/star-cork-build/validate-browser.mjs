import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import { pathToFileURL } from 'node:url';
import puppeteer from './puppeteer-pipe.mjs';
const out='logs/star-cork-build/browser';fs.mkdirSync(out,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const hash=f=>crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const report={at:new Date().toISOString(),transport:'Actual Chromium file URL / CDP pipe / single process, trusted page.mouse events',htmlSha256:hash('public/g/star-cork/index.html'),engineSha256:hash('public/g/star-cork/engine.js'),errors:[],requests:[],runs:[],screenshots:[]};
let browser;
try{
browser=await puppeteer.launch();const page=await browser.newPage();
page.on('console',m=>{if(m.type()==='error')report.errors.push('console: '+m.text());});
page.on('pageerror',e=>report.errors.push('page: '+String(e)));
page.on('requestfailed',r=>report.errors.push('request: '+r.url()+' '+r.failure()?.errorText));
page.on('request',r=>report.requests.push(r.url()));
await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
await page.goto(pathToFileURL(path.resolve('public/g/star-cork/index.html')).href,{waitUntil:'load'});
await page.waitForFunction(()=>window.__GAME_TEST__?.ready===true);
await page.evaluate(()=>{window.__validationPointers=[];document.addEventListener('pointerdown',e=>window.__validationPointers.push({trusted:e.isTrusted,x:e.clientX,y:e.clientY}),true);});
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
const targets=()=>page.evaluate(()=>{const r=document.querySelector('canvas').getBoundingClientRect();const t=window.__STAR_UI__.getTargets();return{belt:t.belt.map(c=>({...c,x:c.x+r.x,y:c.y+r.y})),plate:t.plate.map(c=>({...c,x:c.x+r.x,y:c.y+r.y}))};});
const shot=async name=>{await page.screenshot({path:path.join(out,name+'.png')});report.screenshots.push({name,state:await state()});};
const tap=async t=>{await page.mouse.click(t.x,t.y);await sleep(130);};
const settle=async()=>{for(let i=0;i<100;i++){if((await state()).phase!=='feedback')return;await sleep(70);}throw Error('feedback timed out');};
function worth(type,p){return type==='big'?p.legendBig:type==='small'?p.legendSmall:p.legendBig/2;}
function subset(candies,target,p){const dp=new Map([[0,[]]]);for(const c of candies)for(const [n,ids]of [...dp].sort((a,b)=>b[0]-a[0])){const next=n+worth(c.type,p);if(next<=target&&(!dp.has(next)||dp.get(next).length>ids.length+1))dp.set(next,[...ids,c.id]);}return dp.get(target);}
async function solve(label){let inputs=0,waits=0;const captured=new Set();
  while(['playing','feedback'].includes((await state()).phase)&&inputs<150&&waits<10000){
    let s=await state();if(s.phase==='feedback'){await settle();continue;}
    if(!captured.has(s.problem.type+'-'+s.problem.legendBig)){
      captured.add(s.problem.type+'-'+s.problem.legendBig);await shot(label+'-'+s.problem.type+'-'+s.problem.legendBig);
      if(s.problem.rows.length){await page.setViewport({width:1280,height:800,deviceScaleFactor:1,isMobile:true,hasTouch:true});await page.waitForFunction(()=>window.__STAR_UI__.getLayout().belt.w===1280);await shot(label+'-graph-wide');await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});await page.waitForFunction(()=>window.__STAR_UI__.getLayout().belt.w===390);}
      s=await state();
    }
    const want=subset(s.candies.filter(c=>c.status!=='gone'),s.problem.target,s.problem);
    assert(want,'no remaining solution '+s.problem.id);
    const t=await targets();
    let candidate=t.plate.find(c=>!want.includes(c.id));
    if(!candidate)candidate=t.belt.find(c=>want.includes(c.id));
    if(candidate){const before=s.inputCount;await tap(candidate);const after=await state();if(!(after.inputCount>before||after.phase!=='playing')){report.missedMovingTargets??=[];report.missedMovingTargets.push({candidate,before:s,after});await shot(label+'-moving-target-miss-'+report.missedMovingTargets.length);assert(report.missedMovingTargets.length<10,'repeated trusted target misses');}else inputs++;}
    else {await sleep(60);waits++;}
  }
  const s=await state();assert.equal(s.phase,'won',JSON.stringify({label,inputs,waits,phase:s.phase,p:s.problem.id,lives:s.lives,elapsed:s.elapsed}));
  await sleep(250);await shot(label+'-won');return {label,inputs,waits,elapsed:s.elapsed,score:s.score,lives:s.lives,solved:s.solved,firstAttemptCount:s.firstAttemptCount,firstAttemptCorrect:s.firstAttemptCorrect};
}
await shot('00-title-390');await page.click('#start');await sleep(150);assert((await state()).frozen);await shot('01-tutorial-390');
await page.mouse.click(360,610);await sleep(140);await shot('02-invalid-tap');
let t=await targets();await tap(t.belt.find(c=>c.type==='big'));assert.equal((await state()).plate.length,1);await shot('03-first-real-star');
report.runs.push(await solve('normal'));
await page.click('#restart');await sleep(130);
const restartState=await state();
report.restartState={phase:restartState.phase,frozen:restartState.frozen,tutorial:restartState.tutorial,orderIndex:restartState.orderIndex,problemId:restartState.problem?.id};
assert.equal(restartState.phase,'playing');assert.equal(restartState.frozen,false);assert.equal(restartState.tutorial,false);assert.equal(restartState.orderIndex,0);
for(let i=0;i<200&&(await state()).lives===3;i++){t=await targets();if(t.belt.length)await tap(t.belt[0]);else await sleep(80);}
const recoveryFailure=await state();
report.recoveryFailure={phase:recoveryFailure.phase,lives:recoveryFailure.lives,solved:recoveryFailure.solved,reason:recoveryFailure.lastEvent?.reason};
assert.equal(recoveryFailure.lives,2);assert.equal(recoveryFailure.solved,0);assert.equal(recoveryFailure.lastEvent?.reason,'over');
await shot('recovery-first-real-overflow');await settle();report.runs.push(await solve('recovery'));
report.pointerEvents=await page.evaluate(()=>window.__validationPointers);
report.externalRequests=report.requests.filter(u=>/^(https?|wss?):/i.test(u));
report.horizontalOverflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth);
assert(report.pointerEvents.length>20);assert(report.pointerEvents.every(e=>e.trusted));assert.equal(report.horizontalOverflow,false);assert.deepEqual(report.externalRequests,[]);assert.deepEqual(report.errors,[]);report.pass=true;
}catch(e){report.error=e.stack;report.pass=false;}
finally{if(browser)await browser.close();fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2)+'\n');}
console.log(JSON.stringify({...report,screenshots:report.screenshots.map(x=>({name:x.name,phase:x.state.phase,problem:x.state.problem?.id})),pointerEvents:report.pointerEvents?.length},null,2));if(!report.pass)process.exitCode=1;
