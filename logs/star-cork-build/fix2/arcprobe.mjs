import puppeteer from 'puppeteer';
import fs from 'node:fs'; import path from 'node:path';
import { serveStatic } from '/Users/sitpo/math-game-factory/factory/lib/static-server.mjs';
function resolveChrome(){const base=path.join(process.env.HOME,'.cache/puppeteer/chrome');
for(const b of fs.readdirSync(base).sort().reverse())for(const rel of ['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing']){const p=path.join(base,b,rel);if(fs.existsSync(p))return p;}}
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const srv=await serveStatic('/Users/sitpo/math-game-factory/public');
const br=await puppeteer.launch({executablePath:resolveChrome(),headless:'new'});
const page=await br.newPage();
await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
await page.evaluateOnNewDocument(()=>{
  const P=CanvasRenderingContext2D.prototype, orig=P.arc;
  window.__ARC__=[];
  P.arc=function(x,y,r,...rest){ if(!(r>=0)){ window.__ARC__.push({x,y,r,stack:new Error().stack}); return; } return orig.call(this,x,y,r,...rest); };
});
await page.goto(srv.url+'/g/star-cork/',{waitUntil:'networkidle2'});
await page.evaluate(()=>localStorage.clear());
await page.reload({waitUntil:'networkidle2'});
await page.waitForFunction('window.__GAME_TEST__&&window.__GAME_TEST__.ready===true');
await page.click('#start');
for(let i=0;i<12;i++){await page.mouse.click(200,620);await sleep(700);}
const a=await page.evaluate(()=>window.__ARC__.slice(0,3));
console.log(JSON.stringify(a,null,2));
await br.close(); await srv.close();
