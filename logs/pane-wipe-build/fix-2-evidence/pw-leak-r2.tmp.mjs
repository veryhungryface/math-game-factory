import puppeteer from 'puppeteer';
import http from 'node:http'; import fs from 'node:fs'; import path from 'node:path';
const ROOT='/Users/sitpo/math-game-factory/public';
const MIME={'.html':'text/html','.js':'text/javascript','.json':'application/json','.png':'image/png'};
const server=http.createServer((q,r)=>{const p=path.join(ROOT,decodeURIComponent(q.url.split('?')[0]));if(!fs.existsSync(p)||fs.statSync(p).isDirectory()){r.writeHead(404);return r.end('x');}r.writeHead(200,{'content-type':MIME[path.extname(p)]||'application/octet-stream'});fs.createReadStream(p).pipe(r);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));
const base=`http://127.0.0.1:${server.address().port}`;
const OUT='/Users/sitpo/math-game-factory/logs/pane-wipe-build/fix-2-evidence';
const browser=await puppeteer.launch({headless:'new',executablePath:process.env.PUPPETEER_EXECUTABLE_PATH,args:['--no-sandbox']});
const page=await browser.newPage();
await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
const errs=[];page.on('pageerror',e=>errs.push(e.message));
await page.goto(`${base}/g/pane-wipe/index.html`,{waitUntil:'networkidle0'});
await page.waitForFunction('window.__GAME_TEST__?.ready===true',{timeout:15000});
const bb=await(await page.$('.main-btn')).boundingBox();
await page.mouse.move(bb.x+bb.width/2,bb.y+bb.height/2);await page.mouse.down();await new Promise(r=>setTimeout(r,60));await page.mouse.up();
await new Promise(r=>setTimeout(r,600));
async function drag(a,b,steps=18){await page.mouse.move(a.x,a.y);await page.mouse.down();for(let i=1;i<=steps;i++){await page.mouse.move(a.x+(b.x-a.x)*i/steps,a.y+(b.y-a.y)*i/steps);await new Promise(r=>setTimeout(r,7));}await page.mouse.up();}
async function targets(){return page.evaluate(()=>window.__GAME_TEST__.getInputTargets());}
async function st(){return page.evaluate(()=>window.__GAME_TEST__.getState());}
// 1) wrong stroke while still in TUTORIAL -> ghost correct stroke SHOULD show (teaching)
let t=await targets();
await drag(t.handle,{x:t.handle.x+120,y:t.handle.y-40},18);
await new Promise(r=>setTimeout(r,400));
await page.screenshot({path:`${OUT}/leak-tutorial-wrong.png`});
console.log('tutorial wrong ->',(await st()).lastEvent?.kind,(await st()).lastEvent?.message);
// escape tutorial (3 fails)
for(let i=0;i<12;i++){const s0=await st();if(!s0.tutorial&&s0.phase==='playing')break;if(s0.phase!=='playing'&&s0.phase!=='tutorial'){await new Promise(r=>setTimeout(r,400));continue;}const tt=await targets();if(!tt.handle){await new Promise(r=>setTimeout(r,400));continue;}await drag(tt.handle,{x:tt.handle.x+120,y:tt.handle.y-40},18);await new Promise(r=>setTimeout(r,900));}
await new Promise(r=>setTimeout(r,600));
// 2) wrong stroke on a SCORED pane -> ghost correct stroke must NOT show (no answer leak)
let s=await st(); console.log('now tutorial =',s.tutorial,'phase',s.phase);
t=await targets();
if(t.handle){await drag(t.handle,{x:t.handle.x+130,y:t.handle.y-30},18);}
await new Promise(r=>setTimeout(r,400));
await page.screenshot({path:`${OUT}/leak-scored-wrong.png`});
s=await st(); console.log('scored wrong ->',s.lastEvent?.kind,'|',s.lastEvent?.message,'| tutorialFlag',s.lastEvent?.tutorial);
console.log('errs',JSON.stringify(errs));
await browser.close();server.close();
