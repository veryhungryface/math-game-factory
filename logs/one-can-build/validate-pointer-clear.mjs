import http from 'http'; import fs from 'fs'; import path from 'path';
import puppeteer from 'puppeteer';
const root='/Users/sitpo/math-game-factory/public';
const types={'.html':'text/html','.js':'text/javascript','.json':'application/json','.png':'image/png'};
const srv=http.createServer((q,r)=>{let p=path.join(root,decodeURIComponent(q.url.split('?')[0]));if(p.endsWith('/'))p+='index.html';
 fs.readFile(p,(e,d)=>{if(e){r.writeHead(404);r.end('x');return}r.writeHead(200,{'content-type':types[path.extname(p)]||'application/octet-stream'});r.end(d)})});
await new Promise(r=>srv.listen(0,'127.0.0.1',r));const port=srv.address().port;
const b=await puppeteer.launch({headless:true,executablePath:process.env.PUPPETEER_EXECUTABLE_PATH});
const pg=await b.newPage();
await pg.setViewport({width:390,height:844,deviceScaleFactor:2,isMobile:true,hasTouch:true});
const errs=[],net=[];pg.on('console',m=>{if(m.type()==='error')errs.push(m.text())});pg.on('pageerror',e=>errs.push(String(e)));
pg.on('requestfailed',r=>net.push(r.url()));
await pg.goto(`http://127.0.0.1:${port}/g/one-can/`,{waitUntil:'networkidle0'});
await pg.waitForFunction('window.__GAME_TEST__&&window.__GAME_TEST__.ready');
// real pointer: title CTA, tutorial, then a full timed run — NO answerCorrect() hook
await pg.click('#startButton');
await new Promise(r=>setTimeout(r,600));
let clicks=0, misfires=0;
const t0=Date.now();
while(Date.now()-t0<120000){
  const s=await pg.evaluate(()=>{const g=window.__GAME_TEST__,s=g.getState();
    if(s.phase!=='playing'||s.animating)return {wait:true,phase:s.phase};
    const tg=g.getInputTargets();
    // pick the exact fill when possible, else the largest that still fits
    const cap=Math.min(1000,s.problem.targetMl-s.orderShipped*1000);const req=cap-s.canMl;
    const fits=tg.map((t,i)=>({...t,i})).filter(t=>t.ml<=req);
    const exact=fits.find(t=>t.ml===req);
    // with drops left, take a partial that keeps the remainder reachable; else the exact cup
    const pick=exact||(s.dropsLeft>1?fits.sort((a,b)=>b.ml-a.ml)[0]:null)||fits[0]||tg[0];
    return {pick,req,canMl:s.canMl,shipped:s.shipped,lives:s.lives,dropsLeft:s.dropsLeft,phase:s.phase};});
  if(s.wait){ if(s.phase!=='playing')break; await new Promise(r=>setTimeout(r,120)); continue; }
  const before=s.canMl;
  await pg.mouse.click(s.pick.x,s.pick.y+8); clicks++;
  await new Promise(r=>setTimeout(r,1500));
  const after=await pg.evaluate(()=>window.__GAME_TEST__.getState());
  const expect=(before+s.pick.ml)%1000;
  if(after.canMl!==expect && after.lives===s.lives) misfires++;
}
const fin=await pg.evaluate(()=>window.__GAME_TEST__.getState());
fs.mkdirSync('/tmp/ocp',{recursive:true});
await pg.screenshot({path:'/tmp/ocp/end.png'});
console.log(JSON.stringify({clicks,misfires,phase:fin.phase,shipped:fin.shipped,solved:fin.solved,lives:fin.lives,score:fin.score,elapsed:fin.elapsed,errs,net}));
await b.close();srv.close();
