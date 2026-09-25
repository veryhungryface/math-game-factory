import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';import {pathToFileURL} from 'node:url';
import puppeteer from './browser-pipe.mjs';import {pointerDrag,visibleTriangle} from './browser-geometry.mjs';
const out=path.resolve('logs/jareul-beollyeo-build/browser-final');fs.mkdirSync(out,{recursive:true});
const report={at:new Date().toISOString(),errors:[],failureRun:[],layouts:[]};const b=await puppeteer.launch({timeout:15000});const wait=ms=>new Promise(r=>setTimeout(r,ms));
try{
 const p=await b.newPage();await p.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
 p.on('pageerror',e=>report.errors.push(String(e)));p.on('console',m=>{if(m.type()==='error')report.errors.push(m.text())});p.on('requestfailed',r=>report.errors.push(r.url()));
 await p.goto(pathToFileURL(path.resolve('public/g/jareul-beollyeo/index.html')).href,{waitUntil:'load'});await p.waitForFunction(()=>window.__GAME_TEST__?.ready);
 const see=()=>p.evaluate(()=>({s:window.__GAME_TEST__.getState(),g:window.__GAME_TEST__.getInputTargets()}));
 await p.screenshot({path:path.join(out,'title-390.png')});await p.click('#start');await wait(100);let o=await see();
 await pointerDrag(p,o.g.vertices[0],o.g.vertices[1]);await wait(100);o=await see();await pointerDrag(p,o.g.vertices[1],o.g.vertices[2]);await wait(1400);
 for(let i=0;i<3;i++){
  o=await see();assert.equal(o.s.phase,'playing');assert.equal(o.s.lives,3-i);const model=visibleTriangle(o.g.vertices);
  if(model.isosceles){const c=o.g.pieces.find(q=>q.active).center,bin=o.g.bin;await pointerDrag(p,c,{x:bin.x+bin.w/2,y:bin.y+bin.h/2});}
  else{if(o.s.locked>=0){const r=o.g.unlock;await p.mouse.click(r.x+r.width/2,r.y+r.height/2);await wait(100);o=await see();}await pointerDrag(p,o.g.vertices[0],o.g.vertices[1]);await wait(100);o=await see();await pointerDrag(p,o.g.vertices[1],o.g.vertices[2]);}
  await wait(1450);o=await see();report.failureRun.push(o.s);
 }
 assert.equal(o.s.lives,0);assert.equal(o.s.phase,'gameover');await p.screenshot({path:path.join(out,'gameover-390.png')});
 const before=o.s;await pointerDrag(p,{x:80,y:300},{x:270,y:480});await p.mouse.click(190,540);await wait(10000);const after=(await see()).s;
 assert.equal(after.phase,'gameover');assert.equal(after.lives,0);assert.equal(after.score,before.score);report.noRevival={before,after,waitMs:10000,inputs:'one drag plus tap away from retry'};
 for(const [width,height]of [[390,844],[844,390],[1280,800],[1920,1080]]){
  await p.setViewport({width,height,deviceScaleFactor:1,isMobile:width<1000,hasTouch:width<1000});
  await p.goto(pathToFileURL(path.resolve('public/g/jareul-beollyeo/index.html')).href,{waitUntil:'load'});await p.waitForFunction(()=>window.__GAME_TEST__?.ready);await p.screenshot({path:path.join(out,`title-${width}x${height}.png`)});await p.click('#start');await wait(200);
  await p.screenshot({path:path.join(out,`play-${width}x${height}.png`)});report.layouts.push(await p.evaluate(()=>({width:innerWidth,height:innerHeight,overflow:document.documentElement.scrollWidth>innerWidth,land:document.documentElement.classList.contains('land'),geometry:window.__GAME_TEST__.getInputTargets()})));
 }
 assert.deepEqual(report.errors,[]);
}catch(e){report.failure=e.stack;throw e}finally{await b.close();fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2)+'\n')}
console.log(JSON.stringify({failureRun:report.failureRun,noRevival:report.noRevival,errors:report.errors},null,2));
