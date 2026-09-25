import puppeteer from 'puppeteer';
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = '/Users/sitpo/math-game-factory/public';
const MIME = {'.html':'text/html','.js':'text/javascript','.json':'application/json','.png':'image/png','.css':'text/css','.woff2':'font/woff2'};
const server = http.createServer((req,res)=>{
  const p = path.join(ROOT, decodeURIComponent(req.url.split('?')[0]));
  if(!fs.existsSync(p)||fs.statSync(p).isDirectory()){res.writeHead(404);return res.end('x');}
  res.writeHead(200,{'content-type':MIME[path.extname(p)]||'application/octet-stream'});
  fs.createReadStream(p).pipe(res);
});
await new Promise(r=>server.listen(0,'127.0.0.1',r));
const base = `http://127.0.0.1:${server.address().port}`;

const OUT = process.env.OUT || '/tmp/pw-play';
fs.mkdirSync(OUT,{recursive:true});
const browser = await puppeteer.launch({headless:'new', executablePath: process.env.PUPPETEER_EXECUTABLE_PATH, args:['--no-sandbox']});
const page = await browser.newPage();
await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
const errs=[]; page.on('console',m=>{if(m.type()==='error')errs.push(m.text())}); page.on('pageerror',e=>errs.push('PAGEERR '+e.message));
await page.goto(`${base}/g/pane-wipe/index.html`,{waitUntil:'networkidle0'});
await page.waitForFunction('window.__GAME_TEST__?.ready===true',{timeout:15000});

// tap the start button with real pointer events
const btn = await page.$('.main-btn');
const bb = await btn.boundingBox();
await page.mouse.move(bb.x+bb.width/2, bb.y+bb.height/2);
await page.mouse.down(); await new Promise(r=>setTimeout(r,60)); await page.mouse.up();
await new Promise(r=>setTimeout(r,500));

async function drag(a,b,steps=18){
  await page.mouse.move(a.x,a.y);
  await page.mouse.down();
  for(let i=1;i<=steps;i++){
    await page.mouse.move(a.x+(b.x-a.x)*i/steps, a.y+(b.y-a.y)*i/steps);
    await new Promise(r=>setTimeout(r,8));
  }
  await page.mouse.up();
}

const log=[];
const t0=Date.now();
let shot=0;
for(let turn=0; turn<40; turn++){
  const info = await page.evaluate(()=>({t:window.__GAME_TEST__.getInputTargets(), s:window.__GAME_TEST__.getState()}));
  const {t,s}=info;
  if(s.phase==='won'||s.phase==='lost'){log.push({turn,phase:s.phase,solved:s.solved,lives:s.lives,firstAttempts:s.firstAttempts,firstCorrect:s.firstCorrect});break;}
  if(s.phase==='feedback'){await new Promise(r=>setTimeout(r,300));turn--;continue;}
  if(!t.handle||!t.lines?.length){await new Promise(r=>setTimeout(r,200));continue;}
  const L0=t.lines[0], L1=t.lines[1];
  const ang=l=>Math.atan2(l.b.y-l.a.y,l.b.x-l.a.x);
  let action='stroke';
  if(L1){
    let d=Math.abs(((ang(L1)-ang(L0))*180/Math.PI)%180); if(d>90)d=180-d;
    if(d>1.5) action='discard';
  }
  // perpendicular distance from handle to L0
  const u={x:Math.cos(ang(L0)),y:Math.sin(ang(L0))};
  const n={x:-u.y,y:u.x};
  const w={x:t.handle.x-L0.a.x,y:t.handle.y-L0.a.y};
  const proj=w.x*n.x+w.y*n.y;
  const foot={x:t.handle.x-n.x*proj, y:t.handle.y-n.y*proj};
  const cm=Math.abs(proj)/t.unit;
  if(t.mode==='target' && Math.round(cm)!==4) action='discard';
  const rec={turn,phase:s.phase,mode:t.mode,tutorial:s.tutorial,solved:s.solved,lives:s.lives,cm:+cm.toFixed(2),action,prompt:t.prompt};
  if(action==='discard'){
    const c={x:t.pane.x+t.pane.w*0.5, y:t.pane.y+t.pane.h*0.12};
    await drag(c,{x:c.x+110,y:c.y+4},14);
  } else {
    // stains: lift first if marks present
    if(s.marks?.length && (t.mode==='error'||t.mode==='mixed')){
      await page.mouse.move(t.handle.x,t.handle.y); await page.mouse.down();
      await new Promise(r=>setTimeout(r,80)); await page.mouse.up();
      await new Promise(r=>setTimeout(r,150));
    }
    await drag(t.handle,foot,20);
  }
  await new Promise(r=>setTimeout(r,260));
  const after=await page.evaluate(()=>window.__GAME_TEST__.getState());
  rec.after={phase:after.phase,solved:after.solved,lives:after.lives,tutorial:after.tutorial,ev:after.lastEvent?.kind,msg:after.lastEvent?.message};
  log.push(rec);
  if(turn%3===0){await page.screenshot({path:`${OUT}/p${String(shot++).padStart(2,'0')}.png`});}
  await new Promise(r=>setTimeout(r,600));
}
await page.screenshot({path:`${OUT}/final.png`});
const s=await page.evaluate(()=>window.__GAME_TEST__.getState());
fs.writeFileSync(`${OUT}/log.json`, JSON.stringify({log,final:s,errs,elapsed:(Date.now()-t0)/1000},null,1));
console.log(JSON.stringify({turns:log.length,final:{phase:s.phase,solved:s.solved,lives:s.lives,timeLeft:Math.round(s.timeLeft),firstAttempts:s.firstAttempts,firstCorrect:s.firstCorrect,blocked:s.blockedAttempts},errs},null,1));
for(const r of log) console.log(r.turn, r.mode, 'tut='+r.tutorial, r.action, 'cm='+r.cm, '->', r.after?.ev, '|', r.after?.msg);
await browser.close(); server.close();
