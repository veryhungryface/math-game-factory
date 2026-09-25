// Round-3 reproduction: can a real pointer produce a STUB stroke that spans far
// less than the true gap, yet be scored as a correct "d cm 수선"?
// Method: tap inside the pane within 24px of the near gasket to relocate the
// blade INWARD, then release the drag short of the far gasket.
import puppeteer from 'puppeteer';
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';

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
const OUT = process.env.OUT || '/tmp/pw-stub';
fs.mkdirSync(OUT,{recursive:true});
const INSET = Number(process.env.INSET||23);   // engine px pulled in at EACH end

const browser = await puppeteer.launch({headless:'new', executablePath: process.env.PUPPETEER_EXECUTABLE_PATH, args:['--no-sandbox']});
const page = await browser.newPage();
await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
const errs=[]; page.on('console',m=>{if(m.type()==='error')errs.push(m.text())}); page.on('pageerror',e=>errs.push('PAGEERR '+e.message));
await page.goto(`${base}/g/pane-wipe/index.html`,{waitUntil:'networkidle0'});
await page.waitForFunction('window.__GAME_TEST__?.ready===true',{timeout:15000});
const btn = await page.$('.main-btn'); const bb = await btn.boundingBox();
await page.mouse.move(bb.x+bb.width/2, bb.y+bb.height/2);
await page.mouse.down(); await new Promise(r=>setTimeout(r,60)); await page.mouse.up();
await new Promise(r=>setTimeout(r,500));

async function drag(a,b,steps=18){
  await page.mouse.move(a.x,a.y); await page.mouse.down();
  for(let i=1;i<=steps;i++){ await page.mouse.move(a.x+(b.x-a.x)*i/steps, a.y+(b.y-a.y)*i/steps); await new Promise(r=>setTimeout(r,8)); }
  await page.mouse.up();
}
async function tap(p){ await page.mouse.move(p.x,p.y); await page.mouse.down(); await new Promise(r=>setTimeout(r,70)); await page.mouse.up(); await new Promise(r=>setTimeout(r,220)); }
const geom = t => {
  const L0=t.lines[0], L1=t.lines[1];
  const ang=l=>Math.atan2(l.b.y-l.a.y,l.b.x-l.a.x);
  const u={x:Math.cos(ang(L0)),y:Math.sin(ang(L0))}, n={x:-u.y,y:u.x};
  const w={x:t.handle.x-L0.a.x,y:t.handle.y-L0.a.y};
  const proj=w.x*n.x+w.y*n.y;                       // signed gap, handle side
  const foot={x:t.handle.x-n.x*proj, y:t.handle.y-n.y*proj};
  let delta=null;
  if(L1){ let dd=Math.abs(((ang(L1)-ang(L0))*180/Math.PI)%180); if(dd>90)dd=180-dd; delta=dd; }
  // inward unit vector: from the handle (on L1) toward L0
  const inward={x:-n.x*Math.sign(proj), y:-n.y*Math.sign(proj)};
  return {L0,L1,n,foot,proj,delta,inward,cm:Math.abs(proj)/t.unit,scale:t.unit/26};
};

const log=[]; let stubTried=0, stubScored=0;
for(let turn=0; turn<50 && stubTried<6; turn++){
  const {t,s} = await page.evaluate(()=>({t:window.__GAME_TEST__.getInputTargets(), s:window.__GAME_TEST__.getState()}));
  if(s.phase==='won'||s.phase==='lost') break;
  if(s.phase==='feedback'){ await new Promise(r=>setTimeout(r,300)); turn--; continue; }
  if(!t.handle||!t.lines?.length){ await new Promise(r=>setTimeout(r,200)); continue; }
  const g = geom(t);
  const cm = Math.round(g.cm);
  const solvable = t.lines.length>1 && g.delta!==null && g.delta<1.5 && !(t.mode==='target'&&cm!==4);
  // Play honestly through tutorial / discard panes / point panes; only attack a
  // scored, parallel, strokeable two-gasket pane.
  if(s.tutorial || t.mode==='point' || !solvable){
    if(!solvable && t.lines.length>1 && !s.tutorial){
      const c={x:t.pane.x+t.pane.w*0.5, y:t.pane.y+t.pane.h*0.12};
      await drag(c,{x:c.x+110,y:c.y+4},14);
    } else {
      if(s.marks?.length){ await tap(t.handle); }
      await drag(t.handle,g.foot,20);
    }
    await new Promise(r=>setTimeout(r,700)); continue;
  }
  if(s.marks?.length){ await tap(t.handle); }
  const px = INSET*g.scale;
  // The blade's grab radius is >=30 engine px, so the relocation tap must sit
  // sideways ALONG the gasket while staying within 24px of it.
  const ux={x:-g.inward.y, y:g.inward.x};
  const side = (t.handle.x-t.pane.x < t.pane.w/2) ? 1 : -1;
  const relocate = {x:t.handle.x+g.inward.x*px+ux.x*46*g.scale*side, y:t.handle.y+g.inward.y*px+ux.y*46*g.scale*side};
  const endpoint0 = {x:g.foot.x-g.inward.x*px,   y:g.foot.y-g.inward.y*px};
  if(process.env.RELOCATE!=='0') await tap(relocate);   // move the blade inward off the gasket
  const t2 = await page.evaluate(()=>window.__GAME_TEST__.getInputTargets());
  const moved = Math.hypot(t2.handle.x-t.handle.x, t2.handle.y-t.handle.y)/g.scale;
  // keep the stroke perpendicular from wherever the blade actually ended up
  const endpoint = {x:endpoint0.x+(t2.handle.x-relocate.x)+ (relocate.x-t.handle.x-g.inward.x*px), y:endpoint0.y+(t2.handle.y-relocate.y)+(relocate.y-t.handle.y-g.inward.y*px)};
  await drag(t2.handle, endpoint, 20);
  await new Promise(r=>setTimeout(r,300));
  const after = await page.evaluate(()=>window.__GAME_TEST__.getState());
  const spanPx = Math.hypot(endpoint.x-t2.handle.x, endpoint.y-t2.handle.y)/g.scale;
  const rec={turn,mode:t.mode,trueCm:cm,truePx:+Math.abs(g.proj/g.scale).toFixed(1),
    bladeMovedPx:+moved.toFixed(1), strokePx:+spanPx.toFixed(1), strokeCm:+(spanPx/26).toFixed(2),
    verdict:after.lastEvent?.kind, msg:after.lastEvent?.message};
  stubTried++; if(after.lastEvent?.kind==='correct') stubScored++;
  log.push(rec); console.log(JSON.stringify(rec));
  await page.screenshot({path:`${OUT}/stub${stubTried}.png`});
  await new Promise(r=>setTimeout(r,700));
}
const s=await page.evaluate(()=>window.__GAME_TEST__.getState());
const out={inset:INSET,stubTried,stubScored,log,final:{phase:s.phase,solved:s.solved,lives:s.lives},errs};
fs.writeFileSync(`${OUT}/stub.json`, JSON.stringify(out,null,1));
console.log('SUMMARY '+JSON.stringify({inset:INSET,stubTried,stubScored,errs:errs.length}));
await browser.close(); server.close();
