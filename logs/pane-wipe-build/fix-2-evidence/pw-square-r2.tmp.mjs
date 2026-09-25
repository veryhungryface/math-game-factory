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
await page.evaluate(()=>window.__GAME_TEST__.start());
await new Promise(r=>setTimeout(r,400));
for(let i=0;i<12;i++){
  const s=await page.evaluate(()=>window.__GAME_TEST__.getState());
  if(s.phase==='won'||s.phase==='lost')break;
  if(s.phase!=='playing'&&s.phase!=='intro'){await new Promise(r=>setTimeout(r,250));i--;continue;}
  const id=s.problemId||'';
  if(id.startsWith('target-')||id.startsWith('wow-')){
    await page.evaluate(()=>window.__GAME_TEST__.answerCorrect());
    await new Promise(r=>setTimeout(r,500));
    const now=await page.evaluate(()=>{const s=window.__GAME_TEST__.getState();return{labels:s.labels,ev:s.lastEvent?.problem?.id,kind:s.lastEvent?.kind};});
    await page.screenshot({path:`${OUT}/labels-${id.startsWith('wow-')?'square':'trapezoid-d'+id.split('-')[2]}.png`});
    console.log('captured pane',id,'| lastEvent.problem',now.ev,now.kind,'| labels=',JSON.stringify(now.labels));
    await new Promise(r=>setTimeout(r,1500));
    continue;
  }
  await page.evaluate(()=>window.__GAME_TEST__.answerCorrect());
  await new Promise(r=>setTimeout(r,1400));
}
console.log('errs',JSON.stringify(errs));
await browser.close();server.close();
