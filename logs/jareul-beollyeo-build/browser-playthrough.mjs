import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { pathToFileURL } from 'node:url';
import puppeteer from './browser-pipe.mjs';
import { edgeVertices, visibleTriangle, pairMotion, pointerDrag } from './browser-geometry.mjs';
const fast=process.env.JAREUL_FAST==='1';const out=path.resolve('logs/jareul-beollyeo-build/'+(fast?'browser-playthrough-final':'browser-playthrough'));fs.mkdirSync(out,{recursive:true});
const report={at:new Date().toISOString(),method:'Only visible goal text and rendered geometry select actions. All answer actions use trusted Chromium pointer events; no answerCorrect/answerWrong/start hooks.',hashes:Object.fromEntries(['index.html','engine.js'].map(f=>[f,crypto.createHash('sha256').update(fs.readFileSync('public/g/jareul-beollyeo/'+f)).digest('hex')])),errors:[],requests:[],runs:[],steps:[]};
const browser=await puppeteer.launch({timeout:15000});
const wait=ms=>new Promise(r=>setTimeout(r,ms));
try {
 const page=await browser.newPage();
 await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
 page.on('console',m=>{if(m.type()==='error')report.errors.push(m.text())});page.on('pageerror',e=>report.errors.push(String(e)));page.on('requestfailed',r=>report.errors.push(r.url()+':'+r.failure()?.errorText));page.on('request',r=>report.requests.push(r.url()));
 await page.evaluateOnNewDocument(()=>{window.__trustedQA={down:0,move:0,up:0,untrusted:0};for(const [type,key]of [['pointerdown','down'],['pointermove','move'],['pointerup','up']])document.addEventListener(type,e=>{if(e.isTrusted)window.__trustedQA[key]++;else window.__trustedQA.untrusted++},true)});
 await page.goto(pathToFileURL(path.resolve('public/g/jareul-beollyeo/index.html')).href,{waitUntil:'load'});
 await page.waitForFunction(()=>window.__GAME_TEST__?.ready===true);
 const observe=()=>page.evaluate(()=>({s:window.__GAME_TEST__.getState(),g:window.__GAME_TEST__.getInputTargets(),goal:document.getElementById('goal').textContent,sub:document.getElementById('subgoal').textContent,message:document.getElementById('message').textContent}));
 const shot=name=>page.screenshot({path:path.join(out,name+'.png')});let edgeProxyTested=false;
 const point=(p,g)=>({x:p.x+g.canvas.x,y:p.y+g.canvas.y});
 const squareAim=(sq,theta,g)=>{const a=theta-(sq.handleAngleOffset||0),dx=Math.cos(a),dy=Math.sin(a),x=sq.corner.x,y=sq.corner.y;const rx=Math.abs(dx)<1e-6?Infinity:dx>0?(g.canvas.width-3-x)/dx:(x-3)/-dx,ry=Math.abs(dy)<1e-6?Infinity:dy>0?(g.canvas.height-3-y)/dy:(y-3)/-dy,r=Math.max(8,Math.min(sq.length,rx,ry));return{x:x+dx*r,y:y+dy*r}};
 async function drag(a,b,g,label){const before=await observe();await pointerDrag(page,point(a,g),point(b,g));await wait(120);const after=await observe();report.steps.push({label,before:before.s,after:after.s,goal:after.goal,message:after.message});if(after.s.score>before.s.score||after.s.lives<before.s.lives){if(!fast)await shot(`${report.steps.length}-${label}`);await wait(1150)}console.log(JSON.stringify({step:report.steps.length,label,phase:after.s.phase,solved:after.s.solved,lives:after.s.lives,timeLeft:after.s.timeLeft,measured:after.s.measuredVertices}));return after;}
 async function unlock(o){const r=o.g.unlock;await page.mouse.click(r.x+r.width/2,r.y+r.height/2);await wait(100)}
 async function discard(o,label){const active=o.g.pieces.find(p=>p.active);const b=o.g.bin;return drag(active.center,{x:b.x+b.w/2,y:b.y+b.h/2},o.g,label);}
 async function solveOne(){
  let o=await observe(),v=o.g.vertices,model=visibleTriangle(v);const id=o.s.pieceId;
  if(o.g.square){
   if(!edgeProxyTested){
    const edge={x:o.g.canvas.width-27,y:o.g.canvas.height/2};await drag(o.g.square.corner,edge,o.g,'square-edge-placement');o=await observe();
    await drag(o.g.square.handle,squareAim(o.g.square,0,o.g),o.g,'square-outward-turn');o=await observe();
    assert(o.g.square.handle.x>=24&&o.g.square.handle.x<=o.g.canvas.width-24,'rotation proxy outside viewport');
    const beforeTheta=o.g.square.theta;await drag(o.g.square.handle,squareAim(o.g.square,Math.PI/2,o.g),o.g,'square-proxy-turn');o=await observe();assert(Math.abs(o.g.square.theta-beforeTheta)>.5,'visible proxy did not rotate square');
    report.edgeProxy={passed:true,handle:o.g.square.handle,corner:o.g.square.corner};await shot('edge-proxy-reachable');edgeProxyTested=true;
   }
   const vertex=model.angleClass==='acute'?[0,1,2].find(i=>!o.s.measuredVertices.includes(i)):model.largest;
   assert(vertex!==undefined,'no unmeasured visible angle');
   const c=v[vertex],n1=v[(vertex+1)%3],n2=v[(vertex+2)%3]; const cross=(n1.x-c.x)*(n2.y-c.y)-(n1.y-c.y)*(n2.x-c.x); const next=cross>0?n1:n2;
   const corner=o.g.square.corner;
   if(Math.hypot(corner.x-c.x,corner.y-c.y)>1){await drag(corner,c,o.g,'angle-place');o=await observe();if(o.s.pieceId!==id||o.s.phase!=='playing'||o.s.measuredVertices.includes(vertex))return;}
   const sq=o.g.square;if(!sq)return;
   const theta=Math.atan2(next.y-c.y,next.x-c.x);let target=squareAim(sq,theta,o.g);
   // If already aligned after relocation the action was graded on placement.
   if(Math.hypot(target.x-sq.handle.x,target.y-sq.handle.y)<8){const turn=squareAim(sq,theta+.3,o.g);await drag(sq.handle,turn,o.g,'angle-turn-away');o=await observe();target=squareAim(o.g.square,theta,o.g);}
   await drag(o.g.square.handle,target,o.g,'angle-align');return;
  }
  const equilateral=/정삼각형만/.test(o.goal),combined=/이등변이면서/.test(o.goal),targetAngle=/둔각/.test(o.goal)?'obtuse':'acute';
  const wanted=equilateral?model.equilateral:combined?model.isosceles&&model.angleClass===targetAngle:model.isosceles;
  if(!wanted){await discard(o,'discard-unmatched');return;}
  const pair=model.equalPairs[0];assert(pair,'rendered equal-side pair missing');
  if(o.s.locked>=0&&!model.equilateral&&!pair.includes(o.s.locked)){await unlock(o);o=await observe();}
  if(o.s.locked<0){const move=pairMotion(pair);await drag(v[move.anchor],v[move.first],o.g,'ruler-lock');return;}
  const targetEdge=(model.equilateral?[0,1,2]:pair).find(e=>!o.s.checkedEdges.includes(e));
  assert(targetEdge!==undefined,'no unchecked visible equal edge');
  const r=o.g.ruler;assert(r,'locked ruler missing');
  const near=p=>v.map((q,i)=>({i,d:Math.hypot(q.x-p.x,q.y-p.y)})).sort((a,b)=>a.d-b.d)[0].i;
  const a=near(r.a),b=near(r.b),ends=edgeVertices[targetEdge],anchor=ends.find(i=>i===a||i===b);
  assert(anchor!==undefined,'target edge lacks shared endpoint');
  const free=a===anchor?r.b:r.a,other=ends.find(i=>i!==anchor);
  await drag(free,v[other],o.g,equilateral?'ruler-third-or-second':'ruler-compare');
 }
 async function solveToEnd(name){const wallStart=Date.now();const before=(await observe()).s;let actions=0;while((await observe()).s.phase==='playing'&&actions<75){await solveOne();actions++}const after=(await observe()).s;await shot(name+'-final');report.runs.push({name,before,after,actions,wallSeconds:(Date.now()-wallStart)/1000});assert.equal(after.phase,'clear',name+' did not clear');assert.equal(after.solved,10);return after;}
 await shot('00-title');await page.click('#start');await wait(150);
 let o=await observe();assert(o.s.tutorial&&o.s.frozen);await shot('01-tutorial');
 await page.mouse.click(32,300);await wait(150);await shot('02-invalid-tap');
 for(let i=0;i<3&&(await observe()).s.tutorial;i++)await solveOne();
 o=await observe();assert.equal(o.s.tutorial,false,'tutorial failed to release after both ruler gestures');assert.equal(o.s.frozen,false);await shot('03-after-tutorial');
 await solveToEnd('normal');
 await page.click('#retry');await wait(200);
 // The first ordinary mistake is a mathematical rejection of an unequal pair,
 // or discarding an accepted triangle, selected using the rendered geometry.
 o=await observe();if(o.s.tutorial){while((await observe()).s.tutorial)await solveOne();o=await observe();}
 const m=visibleTriangle(o.g.vertices);
 if(m.isosceles)await discard(o,'first-mistake-discard');else{await drag(o.g.vertices[0],o.g.vertices[1],o.g,'first-mistake-lock');o=await observe();const r=o.g.ruler,free=Math.hypot(r.a.x-o.g.vertices[1].x,r.a.y-o.g.vertices[1].y)<2?r.a:r.b;await drag(free,o.g.vertices[2],o.g,'first-mistake-compare');}
 o=await observe();assert.equal(o.s.lives,2,'real mistake did not cost exactly one lamp');assert.equal(o.s.solved,0);await shot('recovery-first-mistake');
 await solveToEnd('one-mistake-recovery');
 report.trusted=await page.evaluate(()=>window.__trustedQA);report.externalRequests=report.requests.filter(u=>/^https?:/.test(u));report.overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth);
 assert.equal(report.trusted.untrusted,0);assert.deepEqual(report.errors,[]);assert.deepEqual(report.externalRequests,[]);assert.equal(report.overflow,false);
}catch(e){report.failure=e.stack;throw e;}finally{await browser.close();fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2)+'\n');}
console.log(JSON.stringify({runs:report.runs,trusted:report.trusted,errors:report.errors},null,2));
