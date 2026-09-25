import puppeteer from 'puppeteer';
import http from 'node:http'; import fs from 'node:fs'; import path from 'node:path';
const ROOT='/Users/sitpo/math-game-factory/public';
const MIME={'.html':'text/html','.js':'text/javascript','.json':'application/json','.png':'image/png','.css':'text/css','.woff2':'font/woff2'};
const server=http.createServer((q,r)=>{const p=path.join(ROOT,decodeURIComponent(q.url.split('?')[0]));if(!fs.existsSync(p)||fs.statSync(p).isDirectory()){r.writeHead(404);return r.end('x');}r.writeHead(200,{'content-type':MIME[path.extname(p)]||'application/octet-stream'});fs.createReadStream(p).pipe(r);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));
const base=`http://127.0.0.1:${server.address().port}`;
const browser=await puppeteer.launch({headless:'new',executablePath:process.env.PUPPETEER_EXECUTABLE_PATH,args:['--no-sandbox']});
const OUT=process.env.OUT||'/tmp/pw-naive'; fs.mkdirSync(OUT,{recursive:true});
const page=await browser.newPage();
await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
const errs=[];page.on('pageerror',e=>errs.push(e.message));
await page.goto(`${base}/g/pane-wipe/index.html`,{waitUntil:'networkidle0'});
await page.waitForFunction('window.__GAME_TEST__?.ready===true',{timeout:15000});
const bb=await(await page.$('.main-btn')).boundingBox();
await page.mouse.move(bb.x+bb.width/2,bb.y+bb.height/2);await page.mouse.down();await new Promise(r=>setTimeout(r,60));await page.mouse.up();
await new Promise(r=>setTimeout(r,500));
async function drag(a,b,steps=16){await page.mouse.move(a.x,a.y);await page.mouse.down();for(let i=1;i<=steps;i++){await page.mouse.move(a.x+(b.x-a.x)*i/steps,a.y+(b.y-a.y)*i/steps);await new Promise(r=>setTimeout(r,7));}await page.mouse.up();}
// NAIVE BOT: always drags straight up from the handle (screen-vertical misconception)
const log=[]; const t0=Date.now();
for(let i=0;i<60 && Date.now()-t0<70000;i++){
  const {t,s}=await page.evaluate(()=>({t:window.__GAME_TEST__.getInputTargets(),s:window.__GAME_TEST__.getState()}));
  if(s.phase==='won'||s.phase==='lost')break;
  if(s.phase==='feedback'||!t.handle){await new Promise(r=>setTimeout(r,250));continue;}
  await drag(t.handle,{x:t.handle.x,y:t.handle.y-t.unit*4.5},16);
  await new Promise(r=>setTimeout(r,280));
  const a=await page.evaluate(()=>window.__GAME_TEST__.getState());
  log.push({i,mode:t.mode,tut:s.tutorial,ev:a.lastEvent?.kind,solved:a.solved,lives:a.lives,stillTut:a.tutorial,elapsed:Math.round((Date.now()-t0)/1000)});
  await new Promise(r=>setTimeout(r,450));
}
await page.screenshot({path:`${OUT}/final.png`});
const s=await page.evaluate(()=>window.__GAME_TEST__.getState());
console.log(JSON.stringify({final:{phase:s.phase,tutorial:s.tutorial,solved:s.solved,lives:s.lives,timeLeft:Math.round(s.timeLeft),attempts:s.attempts,firstAttempts:s.firstAttempts,firstCorrect:s.firstCorrect},wallSeconds:Math.round((Date.now()-t0)/1000),errs},null,1));
console.log(log.map(r=>`${r.i} ${r.mode} tut=${r.tut}->${r.stillTut} ${r.ev} solved=${r.solved} lives=${r.lives} t=${r.elapsed}s`).join('\n'));
fs.writeFileSync(`${OUT}/log.json`,JSON.stringify({log,final:s},null,1));
await browser.close();server.close();
