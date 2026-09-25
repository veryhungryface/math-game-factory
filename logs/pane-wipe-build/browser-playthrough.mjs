import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from './browser-pipe.mjs';
import {wait,url,hashes,installProbe,pointerDrag} from './browser-utils.mjs';
const out=path.resolve('logs/pane-wipe-build/browser-playthrough-final');fs.mkdirSync(out,{recursive:true});
const report={at:new Date().toISOString(),method:'Real Chromium trusted mouse/touch actions. Read-only visible gasket endpoints, handle and grid unit; project a perpendicular from the visible handle. Never call answer/start/correctAction hooks or read hidden answers to select input.',hashes:hashes(),errors:[],requests:[],runs:[],steps:[]};
const browser=await puppeteer.launch({timeout:15000});
try{
 const page=await browser.newPage();await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});await installProbe(page,report);
 await page.goto(url,{waitUntil:'load'});await page.waitForFunction(()=>window.__GAME_TEST__?.ready===true);
 const read=()=>page.evaluate(()=>({s:window.__GAME_TEST__.getState(),g:window.__GAME_TEST__.getInputTargets(),feedback:document.getElementById('feedback').textContent}));
 const shot=name=>page.screenshot({path:path.join(out,name+'.png')});
 async function settle(){await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase!=='feedback',{timeout:5000});return read();}
 function visibleGeometry(g){const a=g.lines[0].a,b=g.lines[0].b,u={x:b.x-a.x,y:b.y-a.y},norm=Math.hypot(u.x,u.y),q=g.handle,t=((q.x-a.x)*u.x+(q.y-a.y)*u.y)/(norm*norm),foot={x:a.x+t*u.x,y:a.y+t*u.y},second=g.lines[1]||g.lines[0],v={x:second.b.x-second.a.x,y:second.b.y-second.a.y},cross=Math.abs(u.x*v.y-u.y*v.x)/(norm*Math.hypot(v.x,v.y));return {foot,parallel:cross<1e-6,distanceCm:Math.hypot(q.x-foot.x,q.y-foot.y)/g.unit,u:{x:u.x/norm,y:u.y/norm}};}
 async function drag(a,b,label,touch=true){const before=await read();await pointerDrag(page,a,b,touch);await wait(140);const after=await read();report.steps.push({label,pointer:touch?'touch':'mouse',a,b,before:before.s,after:after.s,feedback:after.feedback});console.log(JSON.stringify({label,phase:after.s.phase,solved:after.s.solved,lives:after.s.lives,time:after.s.timeLeft}));if(after.s.eventSerial!==before.s.eventSerial)await shot(String(report.steps.length).padStart(2,'0')+'-'+label);await settle();return read();}
 function blank(g){return{x:g.layout.x+160*g.layout.scale,y:g.layout.y+335*g.layout.scale};}
 async function discard(label){const {g}=await read(),a=blank(g);return drag(a,{x:a.x+90*g.layout.scale,y:a.y},label);}
 let stainRoute='lift',blockedChecked=false;
 async function solveOne(){let o=await settle();if(['error','mixed'].includes(o.g.mode)&&!o.s.lifted){
  if(o.g.mode==='error'&&!blockedChecked){const before=o.s;await drag(o.g.handle,visibleGeometry(o.g).foot,'dirty-source-refusal');o=await read();assert.equal(o.s.lastEvent.kind,'blocked');assert.equal(o.s.lives,before.lives);assert.equal(o.s.firstAttempts,before.firstAttempts);blockedChecked=true;report.stainBlocked={before,after:o.s};}
  const before=o.s;if(stainRoute==='relocate'&&o.g.mode==='error'){
   const line=o.g.lines[1],candidates=[.15,.85].map(t=>({x:line.a.x+(line.b.x-line.a.x)*t,y:line.a.y+(line.b.y-line.a.y)*t}));candidates.sort((a,b)=>Math.hypot(b.x-o.g.handle.x,b.y-o.g.handle.y)-Math.hypot(a.x-o.g.handle.x,a.y-o.g.handle.y));const target=candidates[0];await page.touchscreen.tap(target.x,target.y);await wait(180);o=await read();assert(Math.hypot(o.g.handle.x-target.x,o.g.handle.y-target.y)<3,'blank gasket tap did not move tool');report.steps.push({label:'relocate-away-from-stain',before,after:o.s,target,feedback:o.feedback});
  }else{await page.touchscreen.tap(o.g.handle.x,o.g.handle.y);await wait(180);o=await read();report.steps.push({label:'lift-away-from-visible-stain',before,after:o.s,feedback:o.feedback});}await shot(String(report.steps.length).padStart(2,'0')+'-stain-route');}
  const m=visibleGeometry(o.g);const discardNeeded=o.g.mode!=='point'&&(!m.parallel||(/4 cm/.test(o.g.prompt)&&Math.abs(m.distanceCm-4)>.05));if(discardNeeded)return discard('discard-unmatched');return drag(o.g.handle,m.foot,'wipe-'+o.g.mode,true);}
 async function solveEnd(name){const before=(await read()).s,t=Date.now();let n=0;while(['playing','tutorial','feedback'].includes((await read()).s.phase)&&n<18){await solveOne();n++;}await wait(450);const after=(await read()).s;report.runs.push({name,before,after,actions:n,wallSeconds:(Date.now()-t)/1000});await shot(name+'-final');assert.equal(after.phase,'won',name+' failed');assert.equal(after.solved,8);return after;}
 await shot('00-title');await page.click('#startBtn');await wait(250);await shot('01-tutorial');
 let o=await read();assert(o.s.frozen&&o.s.tutorial);const frozenTime=o.s.timeLeft;
 await page.touchscreen.tap(28,285);await wait(120);await shot('02-invalid-tap');assert.equal((await read()).s.timeLeft,frozenTime);
 o=await read();const tutorialHandle={...o.g.handle},lower=o.g.lines[1],offSource={x:lower.a.x+(lower.b.x-lower.a.x)*.15,y:lower.a.y+(lower.b.y-lower.a.y)*.15};
 await page.touchscreen.tap(offSource.x,offSource.y);await wait(180);o=await read();assert(Math.hypot(o.g.handle.x-tutorialHandle.x,o.g.handle.y-tutorialHandle.y)<.01,'tutorial gasket tap moved the tool away from its fixed guidance');assert.equal(o.s.timeLeft,90);assert.equal(o.s.lives,3);report.tutorialRelocationGuard={tap:offSource,beforeHandle:tutorialHandle,afterHandle:o.g.handle,feedback:o.feedback};await shot('02b-tutorial-gasket-refusal');
 await solveOne();o=await read();assert.equal(o.s.tutorial,false);assert.equal(o.s.solved,0);await shot('03-after-tutorial');await solveEnd('normal');
 await page.click('#retryBtn');await wait(200);o=await read();if(o.s.tutorial)await solveOne();o=await read();
 const m=visibleGeometry(o.g),targetLine=o.g.lines[0],mid={x:(targetLine.a.x+targetLine.b.x)/2,y:(targetLine.a.y+targetLine.b.y)/2},towardCenter=((m.foot.x-mid.x)*m.u.x+(m.foot.y-mid.y)*m.u.y)>0?-1:1;
 const wrong={x:m.foot.x+towardCenter*m.u.x*55*o.g.layout.scale,y:m.foot.y+towardCenter*m.u.y*55*o.g.layout.scale};
 await drag(o.g.handle,wrong,'first-oblique-mistake');o=await read();assert.equal(o.s.lives,2);assert.equal(o.s.solved,0);
 await page.touchscreen.tap(o.g.handle.x,o.g.handle.y);await wait(180);o=await read();assert.equal(o.s.retryCredit,true,'visible handle tap did not lift and grant displayed retry');await shot('recovery-lifted');stainRoute='relocate';await solveEnd('one-mistake-recovery');
 await page.click('#retryBtn');await wait(200);o=await read();if(o.s.tutorial)await solveOne();
 for(let i=1;i<=3;i++){await discard('terminal-wrong-'+i);assert.equal((await read()).s.lives,3-i);}
 o=await read();assert.equal(o.s.phase,'lost');const terminal=o.s;
 await page.touchscreen.tap(180,240);await wait(10000);o=await read();assert.equal(o.s.phase,'lost');assert.equal(o.s.lives,0);assert.equal(o.s.solved,terminal.solved);report.terminal={atZero:terminal,afterInputAndTenSeconds:o.s};await shot('terminal-no-revival');
 report.trusted=await page.evaluate(()=>window.__trustedQA);report.externalRequests=report.requests.filter(u=>/^https?:/.test(u));report.overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth);assert.equal(report.trusted.untrusted,0);assert(report.trusted.touch>0);assert.deepEqual(report.errors,[]);assert.deepEqual(report.externalRequests,[]);assert.equal(report.overflow,false);
}catch(e){report.failure=e.stack;throw e;}finally{await browser.close();fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2)+'\n');}
console.log(JSON.stringify({runs:report.runs,trusted:report.trusted,terminal:report.terminal?.afterInputAndTenSeconds.phase,errors:report.errors},null,2));
