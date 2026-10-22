// 최종 표지는 실제 WebGL 런타임의 동일 재질·캐릭터·극장을 캡처한다.
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname);
const PUBLIC=path.join(ROOT,'public'),GAME=path.join(PUBLIC,'g/hyeonak-nemo');
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
function chromePath(){const b=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const v of fs.readdirSync(b).sort().reverse()){const p=path.join(b,v,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');if(fs.existsSync(p))return p;}}
const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--mute-audio']});
async function shot(w,h,file,play){
  const page=await browser.newPage();await page.setViewport({width:w,height:h,deviceScaleFactor:1});
  await page.goto(`${server.url}/g/hyeonak-nemo/`,{waitUntil:'load',timeout:45000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
  if(play)await page.evaluate(()=>window.__GAME_TEST__.start());
  await sleep(play?1800:2800);await page.screenshot({path:file});await page.close();
}
await shot(1200,630,path.join(GAME,'thumb.png'),false);
await shot(1080,1080,path.join(GAME,'square.png'),false);
await browser.close();await server.close();console.log('runtime covers captured');
