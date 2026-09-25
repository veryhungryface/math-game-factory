import puppeteer from 'puppeteer';import http from 'node:http';import fs from 'node:fs';import path from 'node:path';
const ROOT='/Users/sitpo/math-game-factory/public';
const MIME={'.html':'text/html','.js':'text/javascript','.json':'application/json','.png':'image/png','.css':'text/css','.woff2':'font/woff2'};
const server=http.createServer((q,r)=>{const p=path.join(ROOT,decodeURIComponent(q.url.split('?')[0]));if(!fs.existsSync(p)||fs.statSync(p).isDirectory()){r.writeHead(404);return r.end('x');}r.writeHead(200,{'content-type':MIME[path.extname(p)]||'application/octet-stream'});fs.createReadStream(p).pipe(r);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));const base=`http://127.0.0.1:${server.address().port}`;
const b=await puppeteer.launch({headless:'new',executablePath:process.env.PUPPETEER_EXECUTABLE_PATH,args:['--no-sandbox']});
const OUT='/tmp/pw-caption';fs.mkdirSync(OUT,{recursive:true});
const page=await b.newPage();await page.setViewport({width:390,height:844,isMobile:true,hasTouch:true});
const errs=[];page.on('pageerror',e=>errs.push(e.message));page.on('console',m=>{if(m.type()==='error')errs.push(m.text());});
await page.goto(`${base}/g/pane-wipe/index.html`,{waitUntil:'networkidle0'});
await page.waitForFunction('window.__GAME_TEST__?.ready===true');
const bb=await(await page.$('.main-btn')).boundingBox();
await page.mouse.move(bb.x+bb.width/2,bb.y+bb.height/2);await page.mouse.down();await new Promise(r=>setTimeout(r,60));await page.mouse.up();
await new Promise(r=>setTimeout(r,500));
async function drag(a,c,st=16){await page.mouse.move(a.x,a.y);await page.mouse.down();for(let i=1;i<=st;i++){await page.mouse.move(a.x+(c.x-a.x)*i/st,a.y+(c.y-a.y)*i/st);await new Promise(r=>setTimeout(r,7));}await page.mouse.up();}
const cases=[['overshoot',t=>({x:t.handle.x,y:t.handle.y-t.unit*5.4})],['undershoot',t=>({x:t.handle.x,y:t.handle.y-t.unit*2.2})],['oblique',t=>({x:t.handle.x+72,y:t.handle.y-76})]];
for(const [name,fn] of cases){
  const t=await page.evaluate(()=>window.__GAME_TEST__.getInputTargets());
  if(!t.handle){console.log(name,'SKIP no handle');continue;}
  await drag(t.handle,fn(t));
  await new Promise(r=>setTimeout(r,320));
  const s=await page.evaluate(()=>window.__GAME_TEST__.getState());
  await page.screenshot({path:`${OUT}/${name}.png`});
  console.log(name.padEnd(11),'reason=',s.lastEvent?.reason,'| banner=',s.lastEvent?.message);
  await new Promise(r=>setTimeout(r,1500));
}
console.log('errs',errs);
await b.close();server.close();
