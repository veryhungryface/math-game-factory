import puppeteer from 'puppeteer';
import fs from 'node:fs';
import {pathToFileURL} from 'node:url';
import path from 'node:path';
const gpuAttempt=process.env.ONE_CAN_GPU==='1';
const suffix=gpuAttempt?'-gpu':'';
const report={generatedAt:new Date().toISOString(),purpose:'One file:// browser launch attempt, separate from official QA; no localhost listener or server workaround',mode:gpuAttempt?'gpu-attempt':'software-fallback',launched:false,errors:[],requests:[]};
// Prefer the installed Playwright headless shell. Full Chrome aborts before CDP in
// this restricted runner, while the shell survives with single-process flags.
const candidates=[
  path.join(process.env.HOME||'','Library/Caches/ms-playwright/chromium_headless_shell-1234/chrome-headless-shell-mac-arm64/chrome-headless-shell'),
  path.join(process.env.HOME||'','.cache/puppeteer/chrome-headless-shell/mac_arm-151.0.7922.47/chrome-headless-shell-mac-arm64/chrome-headless-shell'),
];
const executablePath=candidates.find(p=>fs.existsSync(p));
report.executablePath=executablePath;
let browser;
try{
  const args=['--no-sandbox','--single-process','--no-zygote','--disable-dev-shm-usage','--disable-background-networking','--disable-default-apps','--disable-breakpad','--disable-crash-reporter','--disable-features=Vulkan,UseChromeOSDirectVideoDecoder,OptimizationHints,MediaRouter','--no-first-run'];
  if(!gpuAttempt)args.push('--disable-gpu','--disable-software-rasterizer');
  browser=await puppeteer.launch({headless:'shell',pipe:true,executablePath,timeout:15000,args});
  report.launched=true;
  const page=await browser.newPage();await page.setViewport({width:390,height:844,deviceScaleFactor:1});
  page.on('pageerror',e=>report.errors.push(e.message));
  page.on('console',m=>{if(m.type()==='error')report.errors.push(m.text());});
  page.on('request',r=>report.requests.push(r.url()));
  await page.goto(pathToFileURL(path.resolve('public/g/one-can/index.html')).href,{waitUntil:'load',timeout:15000});
  await page.screenshot({path:`logs/one-can-build/browser-title-390${suffix}.png`});
  report.renderer=await page.evaluate(()=>{const c=document.createElement('canvas'),gl=c.getContext('webgl2')||c.getContext('webgl');if(!gl)return null;const d=gl.getExtension('WEBGL_debug_renderer_info');return String(d?gl.getParameter(d.UNMASKED_RENDERER_WEBGL):gl.getParameter(gl.RENDERER));});
  report.ready=await page.evaluate(()=>window.__GAME_TEST__?.ready);
  await page.click('#startButton');
  await new Promise(r=>setTimeout(r,100));
  await page.screenshot({path:`logs/one-can-build/browser-guide-390${suffix}.png`});
  const target=await page.evaluate(()=>window.__GAME_TEST__.getInputTargets()[0]);
  await page.mouse.click(target.x,target.y);
  await new Promise(r=>setTimeout(r,700));
  report.afterRealPointer=await page.evaluate(()=>window.__GAME_TEST__.getState());
  await page.screenshot({path:`logs/one-can-build/browser-play-390${suffix}.png`});
  report.horizontalOverflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth);
}catch(e){report.errors.push(e.stack||String(e));}
finally{if(browser)await browser.close();fs.writeFileSync(`logs/one-can-build/browser-probe${suffix}.json`,JSON.stringify(report,null,2)+'\n');}
console.log(JSON.stringify(report,null,2));
