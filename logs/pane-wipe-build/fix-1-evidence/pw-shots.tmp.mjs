import puppeteer from 'puppeteer';import http from 'node:http';import fs from 'node:fs';import path from 'node:path';
const ROOT='/Users/sitpo/math-game-factory/public';
const MIME={'.html':'text/html','.js':'text/javascript','.json':'application/json','.png':'image/png','.css':'text/css','.woff2':'font/woff2'};
const server=http.createServer((q,r)=>{const p=path.join(ROOT,decodeURIComponent(q.url.split('?')[0]));if(!fs.existsSync(p)||fs.statSync(p).isDirectory()){r.writeHead(404);return r.end('x');}r.writeHead(200,{'content-type':MIME[path.extname(p)]||'application/octet-stream'});fs.createReadStream(p).pipe(r);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));const base=`http://127.0.0.1:${server.address().port}`;
const b=await puppeteer.launch({headless:'new',executablePath:process.env.PUPPETEER_EXECUTABLE_PATH,args:['--no-sandbox']});
const OUT='/tmp/pw-shots';fs.mkdirSync(OUT,{recursive:true});
const page=await b.newPage();await page.setViewport({width:390,height:844,isMobile:true,hasTouch:true});
await page.goto(`${base}/g/pane-wipe/index.html`,{waitUntil:'networkidle0'});
await page.waitForFunction('window.__GAME_TEST__?.ready===true');
const bb=await(await page.$('.main-btn')).boundingBox();
await page.mouse.move(bb.x+bb.width/2,bb.y+bb.height/2);await page.mouse.down();await new Promise(r=>setTimeout(r,60));await page.mouse.up();
await new Promise(r=>setTimeout(r,500));
async function drag(a,c,st=16){await page.mouse.move(a.x,a.y);await page.mouse.down();for(let i=1;i<=st;i++){await page.mouse.move(a.x+(c.x-a.x)*i/st,a.y+(c.y-a.y)*i/st);await new Promise(r=>setTimeout(r,7));}await page.mouse.up();}
// 1) overshoot the practice gasket -> new message
let t=await page.evaluate(()=>window.__GAME_TEST__.getInputTargets());
await drag(t.handle,{x:t.handle.x,y:t.handle.y-t.unit*5.4});
await new Promise(r=>setTimeout(r,350));
await page.screenshot({path:`${OUT}/a-overshoot-msg.png`});
console.log('overshoot msg:',await page.evaluate(()=>window.__GAME_TEST__.getState().lastEvent.message));
// 2) two more misses -> tutorial release
for(let i=0;i<2;i++){await new Promise(r=>setTimeout(r,1400));t=await page.evaluate(()=>window.__GAME_TEST__.getInputTargets());await drag(t.handle,{x:t.handle.x+70,y:t.handle.y-70});await new Promise(r=>setTimeout(r,350));}
await page.screenshot({path:`${OUT}/b-tutorial-release.png`});
const st=await page.evaluate(()=>window.__GAME_TEST__.getState());
console.log('release msg:',st.lastEvent.message,'| tutorial=',st.tutorial,'| releaseTutorial=',st.lastEvent.releaseTutorial);
await new Promise(r=>setTimeout(r,2200));
await page.screenshot({path:`${OUT}/c-real-pane.png`});
console.log('after release phase=',(await page.evaluate(()=>window.__GAME_TEST__.getState())).phase,'tutorial=',(await page.evaluate(()=>window.__GAME_TEST__.getState())).tutorial);
// 3) jump to a rotated square pane: play the deck correctly until wow
for(let k=0;k<30;k++){
  const info=await page.evaluate(()=>({t:window.__GAME_TEST__.getInputTargets(),s:window.__GAME_TEST__.getState()}));
  const {t:tt,s}=info; if(s.phase==='won'||s.phase==='lost')break;
  if(s.phase==='feedback'||!tt.handle){await new Promise(r=>setTimeout(r,250));continue;}
  if(tt.mode==='wow'){await page.screenshot({path:`${OUT}/d-square-pane.png`});console.log('square pane captured');break;}
  const L0=tt.lines[0],L1=tt.lines[1];const ang=l=>Math.atan2(l.b.y-l.a.y,l.b.x-l.a.x);
  let act='stroke';if(L1){let d=Math.abs(((ang(L1)-ang(L0))*180/Math.PI)%180);if(d>90)d=180-d;if(d>1.5)act='discard';}
  const u={x:Math.cos(ang(L0)),y:Math.sin(ang(L0))},n={x:-u.y,y:u.x};
  const w={x:tt.handle.x-L0.a.x,y:tt.handle.y-L0.a.y},pr=w.x*n.x+w.y*n.y;
  const foot={x:tt.handle.x-n.x*pr,y:tt.handle.y-n.y*pr};
  if(tt.mode==='target'&&Math.round(Math.abs(pr)/tt.unit)!==4)act='discard';
  if(act==='discard'){const c={x:tt.pane.x+tt.pane.w*.5,y:tt.pane.y+tt.pane.h*.12};await drag(c,{x:c.x+110,y:c.y+4},14);}
  else{if(s.marks?.length&&(tt.mode==='error'||tt.mode==='mixed')){await page.mouse.move(tt.handle.x,tt.handle.y);await page.mouse.down();await new Promise(r=>setTimeout(r,80));await page.mouse.up();await new Promise(r=>setTimeout(r,150));}await drag(tt.handle,foot,20);}
  await new Promise(r=>setTimeout(r,900));
}
await b.close();server.close();
