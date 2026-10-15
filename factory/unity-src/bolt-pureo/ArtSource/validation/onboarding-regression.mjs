import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname);
const PUBLIC=path.join(ROOT,'public');
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
function chrome(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const b of fs.readdirSync(base).sort().reverse())for(const rel of['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing']){const p=path.join(base,b,rel);if(fs.existsSync(p))return p;}}
const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chrome(),args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage(),errors=[];
await page.setViewport({width:390,height:844,deviceScaleFactor:1});
page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
page.on('pageerror',e=>errors.push(String(e)));
await page.goto(`${server.url}/g/bolt-pureo/?onboarding-regression=1`,{waitUntil:'load',timeout:60000});
await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:90000});
await page.mouse.click(195,422);await sleep(250);
const practiceStart=await page.evaluate(()=>window.__GAME_TEST__.getState());
await sleep(12000);
const afterIdleDemo=await page.evaluate(()=>window.__GAME_TEST__.getState());
const [x,y]=afterIdleDemo.hitMap.split(';')[5].split(',').map(Number);
await page.mouse.move(x,y);await page.mouse.down();await sleep(310);await page.mouse.up();await sleep(1650);
const afterRealHold=await page.evaluate(()=>window.__GAME_TEST__.getState());
const result={
  runId:`bolt-pureo-onboarding-${new Date().toISOString()}`,
  method:'타이틀을 실제 클릭해 연습 진입 → 12초 무입력(5초 시범 2회 이상) → 화면의 안내 대상 볼트에 실제 310ms down/up',
  practiceStart,afterIdleDemo,afterRealHold,consoleErrors:errors,
  passed:practiceStart.onboarding===true&&practiceStart.attempts===0&&afterIdleDemo.onboarding===true&&afterIdleDemo.attempts===0&&afterIdleDemo.solved===0&&afterRealHold.onboarding===false&&errors.length===0
};
fs.writeFileSync(new URL('./onboarding-regression-results.json',import.meta.url),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify(result,null,2));
await page.close();await browser.close();await server.close();if(!result.passed)process.exitCode=1;
