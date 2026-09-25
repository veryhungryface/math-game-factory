import puppeteer from 'puppeteer';
import fs from 'node:fs'; import path from 'node:path';
import { serveStatic } from '../../../factory/lib/static-server.mjs';
function resolveChrome(){const base=path.join(process.env.HOME,'.cache/puppeteer/chrome');
for(const b of fs.readdirSync(base).sort().reverse())for(const rel of ['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing']){const p=path.join(base,b,rel);if(fs.existsSync(p))return p;}}
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const OUT='logs/star-cork-build/fix2';
const srv=await serveStatic('public');
const br=await puppeteer.launch({executablePath:resolveChrome(),headless:'new'});
const out={};
for(const [name,w,h] of [['p390',390,844],['t820',820,1180],['l1280',1280,800],['uw2000',2000,900]]){
  const page=await br.newPage();
  const errs=[];
  page.on('console',m=>{if(m.type()==='error')errs.push(m.text());});
  page.on('pageerror',e=>errs.push(String(e)));
  await page.setViewport({width:w,height:h,deviceScaleFactor:1,isMobile:w<700,hasTouch:w<700});
  await page.goto(srv.url+'/g/star-cork/',{waitUntil:'networkidle2'});
  await page.evaluate(()=>localStorage.clear());
  await page.reload({waitUntil:'networkidle2'});
  await page.waitForFunction('window.__GAME_TEST__&&window.__GAME_TEST__.ready===true');
  await page.click('#start'); await sleep(1200);
  await page.screenshot({path:`${OUT}/layout-${name}.png`});
  const m=await page.evaluate(()=>({scrollW:document.documentElement.scrollWidth,inner:innerWidth,
    stageW:getComputedStyle(document.documentElement).getPropertyValue('--stageW')}));
  out[name]={...m,hScroll:m.scrollW>m.inner+1,errs};
  console.log(name,JSON.stringify(out[name]));
  await page.close();
}
await br.close(); await srv.close();
fs.writeFileSync(`${OUT}/layouts.json`,JSON.stringify(out,null,2));
