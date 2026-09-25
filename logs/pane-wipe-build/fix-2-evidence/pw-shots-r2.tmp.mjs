import puppeteer from 'puppeteer';
import http from 'node:http'; import fs from 'node:fs'; import path from 'node:path';
const ROOT='/Users/sitpo/math-game-factory/public';
const MIME={'.html':'text/html','.js':'text/javascript','.json':'application/json','.png':'image/png','.css':'text/css','.woff2':'font/woff2'};
const server=http.createServer((q,r)=>{const p=path.join(ROOT,decodeURIComponent(q.url.split('?')[0]));if(!fs.existsSync(p)||fs.statSync(p).isDirectory()){r.writeHead(404);return r.end('x');}r.writeHead(200,{'content-type':MIME[path.extname(p)]||'application/octet-stream'});fs.createReadStream(p).pipe(r);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));
const base=`http://127.0.0.1:${server.address().port}`;
const OUT='/Users/sitpo/math-game-factory/logs/pane-wipe-build/fix-2-evidence'; fs.mkdirSync(OUT,{recursive:true});
const browser=await puppeteer.launch({headless:'new',executablePath:process.env.PUPPETEER_EXECUTABLE_PATH,args:['--no-sandbox']});
const page=await browser.newPage();
await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
const errs=[];page.on('pageerror',e=>errs.push(e.message));page.on('console',m=>{if(m.type()==='error')errs.push(m.text())});
await page.goto(`${base}/g/pane-wipe/index.html`,{waitUntil:'networkidle0'});
await page.waitForFunction('window.__GAME_TEST__?.ready===true',{timeout:15000});
const bb=await(await page.$('.main-btn')).boundingBox();
await page.mouse.move(bb.x+bb.width/2,bb.y+bb.height/2);await page.mouse.down();await new Promise(r=>setTimeout(r,60));await page.mouse.up();
await new Promise(r=>setTimeout(r,600));
async function drag(a,b,steps=18){await page.mouse.move(a.x,a.y);await page.mouse.down();for(let i=1;i<=steps;i++){await page.mouse.move(a.x+(b.x-a.x)*i/steps,a.y+(b.y-a.y)*i/steps);await new Promise(r=>setTimeout(r,7));}await page.mouse.up();}
// clear the tutorial with the true perpendicular
for(let pane=0;pane<4;pane++){
  const t=await page.evaluate(()=>window.__GAME_TEST__.getInputTargets());
  const st=await page.evaluate(()=>window.__GAME_TEST__.getState());
  await page.screenshot({path:`${OUT}/pane-${pane}-${st.tutorial?'tutorial':'scored'}-theta.png`});
  const info=await page.evaluate(()=>{const s=window.__GAME_TEST__.getState();return {id:s.problemId,tut:s.tutorial};});
  console.log(pane, info.id, 'tutorial='+info.tut);
  if(!t.handle||!t.lines?.length){console.log('no target');break;}
  const L0=t.lines[0];
  const ang=l=>Math.atan2(l.b.y-l.a.y,l.b.x-l.a.x);
  const th=ang(L0)*180/Math.PI;
  const offAxis=Math.min(Math.abs(((th%180)+180)%180-0),Math.abs(((th%180)+180)%180-90),Math.abs(((th%180)+180)%180-180));
  console.log('   gasket angle on screen =',th.toFixed(1)+'°','offAxis='+offAxis.toFixed(1)+'°');
  const u={x:Math.cos(ang(L0)),y:Math.sin(ang(L0))},n={x:-u.y,y:u.x};
  const w={x:t.handle.x-L0.a.x,y:t.handle.y-L0.a.y},proj=w.x*n.x+w.y*n.y;
  const foot={x:t.handle.x-n.x*proj,y:t.handle.y-n.y*proj};
  await drag(t.handle,foot,20);
  await new Promise(r=>setTimeout(r,1400));
}
console.log('errs',JSON.stringify(errs));
await browser.close();server.close();
