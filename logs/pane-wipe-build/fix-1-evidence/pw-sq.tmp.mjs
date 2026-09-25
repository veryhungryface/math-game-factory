import puppeteer from 'puppeteer';import http from 'node:http';import fs from 'node:fs';import path from 'node:path';
const ROOT='/Users/sitpo/math-game-factory/public';
const MIME={'.html':'text/html','.js':'text/javascript','.json':'application/json','.png':'image/png','.css':'text/css','.woff2':'font/woff2'};
const server=http.createServer((q,r)=>{const p=path.join(ROOT,decodeURIComponent(q.url.split('?')[0]));if(!fs.existsSync(p)||fs.statSync(p).isDirectory()){r.writeHead(404);return r.end('x');}r.writeHead(200,{'content-type':MIME[path.extname(p)]||'application/octet-stream'});fs.createReadStream(p).pipe(r);});
await new Promise(r=>server.listen(0,'127.0.0.1',r));const base=`http://127.0.0.1:${server.address().port}`;
const b=await puppeteer.launch({headless:'new',executablePath:process.env.PUPPETEER_EXECUTABLE_PATH,args:['--no-sandbox']});
const OUT='/tmp/pw-shots';
for(const run of [0,1,2]){
 const page=await b.newPage();await page.setViewport({width:390,height:844,isMobile:true,hasTouch:true});
 await page.goto(`${base}/g/pane-wipe/index.html`,{waitUntil:'networkidle0'});
 await page.waitForFunction('window.__GAME_TEST__?.ready===true');
 await page.evaluate(()=>window.__GAME_TEST__.start());
 for(let i=0;i<8;i++){
   const m=await page.evaluate(()=>window.__GAME_TEST__.getInputTargets().mode);
   if(m==='wow')break;
   await page.evaluate(()=>window.__GAME_TEST__.answerCorrect());
   await new Promise(r=>setTimeout(r,900));
 }
 const t=await page.evaluate(()=>window.__GAME_TEST__.getInputTargets());
 await new Promise(r=>setTimeout(r,400));
 await page.screenshot({path:`${OUT}/e-square-${run}.png`});
 console.log('run',run,'mode',t.mode,'prompt',t.prompt);
 await page.close();
}
await b.close();server.close();
