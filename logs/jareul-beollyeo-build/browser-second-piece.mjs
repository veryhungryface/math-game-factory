import fs from 'node:fs';import path from 'node:path';import assert from 'node:assert/strict';import {pathToFileURL} from 'node:url';
import puppeteer from './browser-pipe.mjs';import {pointerDrag,visibleTriangle,pairMotion} from './browser-geometry.mjs';
const out=path.resolve('logs/jareul-beollyeo-build/browser-second-piece');fs.mkdirSync(out,{recursive:true});const report={at:new Date().toISOString(),errors:[]};const b=await puppeteer.launch({timeout:15000});const wait=ms=>new Promise(r=>setTimeout(r,ms));
try{
 const p=await b.newPage();await p.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});p.on('pageerror',e=>report.errors.push(String(e)));p.on('console',m=>{if(m.type()==='error')report.errors.push(m.text())});
 await p.goto(pathToFileURL(path.resolve('public/g/jareul-beollyeo/index.html')).href,{waitUntil:'load'});await p.waitForFunction(()=>window.__GAME_TEST__?.ready);
 const see=()=>p.evaluate(()=>({s:window.__GAME_TEST__.getState(),g:window.__GAME_TEST__.getInputTargets()}));await p.click('#start');await wait(120);let o=await see();
 await pointerDrag(p,o.g.vertices[0],o.g.vertices[1]);await wait(120);o=await see();await pointerDrag(p,o.g.vertices[1],o.g.vertices[2]);await wait(1400);o=await see();
 // Actual elapsed time, not an engine hook, creates a second early workpiece.
 await wait(22500);o=await see();assert.equal(o.g.pieces.length,2,'second early piece did not spawn');report.before=o;
 const second=o.g.pieces[1];await p.mouse.click(second.center.x,second.center.y);await wait(150);o=await see();assert.equal(o.g.pieces.find(x=>x.active).index,1);assert.notEqual(o.s.pieceId,report.before.s.pieceId);report.selected=o;await p.screenshot({path:path.join(out,'second-selected.png')});
 if(o.s.locked>=0){const u=o.g.unlock;await p.mouse.click(u.x+u.width/2,u.y+u.height/2);await wait(100);o=await see();}
 const model=visibleTriangle(o.g.vertices);
 if(model.isosceles){const m=pairMotion(model.equalPairs[0]);await pointerDrag(p,o.g.vertices[m.anchor],o.g.vertices[m.first]);await wait(100);o=await see();await pointerDrag(p,o.g.vertices[m.first],o.g.vertices[m.second]);}
 else{const c=o.g.pieces.find(q=>q.active).center,r=o.g.bin;await pointerDrag(p,c,{x:r.x+r.w/2,y:r.y+r.h/2});}
 await wait(1500);o=await see();report.after=o;assert.equal(o.s.solved,1);assert.equal(o.s.lives,3);assert.equal(o.s.pieceId,report.before.s.pieceId);await p.screenshot({path:path.join(out,'second-complete-first-retained.png')});assert.deepEqual(report.errors,[]);
}catch(e){report.failure=e.stack;throw e}finally{await b.close();fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2)+'\n')}
console.log(JSON.stringify({before:report.before.s,selected:report.selected.s,after:report.after.s,errors:report.errors},null,2));
