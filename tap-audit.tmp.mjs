import http from 'http'; import fs from 'fs'; import path from 'path';
import puppeteer from 'puppeteer';
const root='/Users/sitpo/math-game-factory/public';
const types={'.html':'text/html','.js':'text/javascript','.json':'application/json','.png':'image/png','.css':'text/css'};
const srv=http.createServer((q,r)=>{let p=path.join(root,decodeURIComponent(q.url.split('?')[0]));if(p.endsWith('/'))p+='index.html';
 fs.readFile(p,(e,d)=>{if(e){r.writeHead(404);r.end('x');return}r.writeHead(200,{'content-type':types[path.extname(p)]||'application/octet-stream'});r.end(d)})});
await new Promise(r=>srv.listen(0,'127.0.0.1',r));
const port=srv.address().port;
const b=await puppeteer.launch({headless:true,executablePath:process.env.PUPPETEER_EXECUTABLE_PATH});
const pg=await b.newPage();
await pg.setViewport({width:Number(process.argv[2]||390),height:Number(process.argv[3]||844),deviceScaleFactor:2,isMobile:true,hasTouch:true});
await pg.goto(`http://127.0.0.1:${port}/g/one-can/`,{waitUntil:'networkidle0'});
await pg.waitForFunction('window.__GAME_TEST__&&window.__GAME_TEST__.ready');
await pg.evaluate(()=>window.__GAME_TEST__.start());   // skip tutorial
await new Promise(r=>setTimeout(r,400));
let mismatch=0,total=0,geom=null;
for(let round=0;round<24;round++){
  const st=await pg.evaluate(()=>{const g=window.__GAME_TEST__;const s=g.getState();
    if(s.animating||s.phase!=='playing')return null;
    return {tg:g.getInputTargets(),rail:s.rail.map(c=>c.ml),
      rects:[...document.querySelectorAll('.cupTarget')].map(x=>{const r=x.getBoundingClientRect();return{x:r.x,y:r.y,w:r.width,h:r.height,hidden:x.hidden}})}});
  if(!st){await new Promise(r=>setTimeout(r,400));continue}
  if(!geom)geom={rects:st.rects,tg:st.tg};
  const i=round%st.tg.length; const t=st.tg[i];
  // aim at the visual centre of cup i (getInputTargets reports the cup centre)
  const before=await pg.evaluate(()=>window.__GAME_TEST__.getState().canMl);
  await pg.mouse.click(t.x,t.y+6);
  await new Promise(r=>setTimeout(r,1500));
  const got=await pg.evaluate(()=>{const s=window.__GAME_TEST__.getState();return {canMl:s.canMl,lives:s.lives,shipped:s.shipped}});
  const delta=got.canMl-before;
  total++;
  // a correct hit consumes exactly the aimed cup: canMl advanced by its ml, or a fail/lid occurred
  const aimed=t.ml;
  if(delta!==aimed && !(got.canMl===0)) { mismatch++; console.log('MISS aim',aimed,'delta',delta,'before',before,'after',got.canMl); }
  if(got.lives<3||got.shipped>0){await pg.evaluate(()=>window.__GAME_TEST__.start());await new Promise(r=>setTimeout(r,400))}
}
console.log('geom rects',JSON.stringify(geom.rects),'\ncentres',JSON.stringify(geom.tg));
const rr=geom.rects.filter(r=>!r.hidden);
let overlap=0;for(let i=1;i<rr.length;i++){const a=rr[i-1],c=rr[i];overlap=Math.max(overlap,Math.min(a.x+a.w,c.x+c.w)-Math.max(a.x,c.x))}
console.log('max target overlap px =',overlap.toFixed(1),' | ambiguous taps',mismatch,'/',total);
await b.close();srv.close();
