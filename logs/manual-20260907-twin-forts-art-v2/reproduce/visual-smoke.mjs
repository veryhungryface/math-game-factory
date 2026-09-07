import fs from 'node:fs';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../factory/lib/static-server.mjs';
const out='logs/manual-20260907-twin-forts-art-v2/host';fs.mkdirSync(out,{recursive:true});
const server=await serveStatic(process.cwd()+'/public');
const browser=await puppeteer.launch({headless:true,executablePath:'/Users/sitpo/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',args:['--no-sandbox','--hide-scrollbars']});
const report=[];const sleep=ms=>new Promise(r=>setTimeout(r,ms));
try{
 for(const [width,height] of [[390,844],[320,720],[820,1180],[1280,800]]){
  const page=await browser.newPage(),errors=[];
  await page.setViewport({width,height,deviceScaleFactor:2,isMobile:width<500,hasTouch:width<500});
  page.on('pageerror',e=>errors.push(String(e)));
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});
  page.on('requestfailed',r=>errors.push(r.url()+': '+r.failure()?.errorText));
  await page.goto(server.url+'/g/twin-forts/',{waitUntil:'networkidle0'});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});
  await page.screenshot({path:`${out}/title-${width}.png`});
  if(width<500)await page.tap('#start');else await page.click('#start');
  await sleep(250);
  await page.screenshot({path:`${out}/planning-${width}.png`});
  const before=await page.evaluate(()=>window.__GAME_TEST__.getState());
  for(let i=0;i<3;i++){if(width<500)await page.tap('#move-right');else await page.click('#move-right');await sleep(70)}
  const allocation=await page.evaluate(()=>window.__GAME_TEST__.getState());
  await page.screenshot({path:`${out}/allocation-${width}.png`});
  if(width<500)await page.tap('#deploy');else await page.click('#deploy');
  await sleep(1200);await page.screenshot({path:`${out}/battle-${width}.png`});
  await sleep(3200);await page.screenshot({path:`${out}/success-${width}.png`});
  const after=await page.evaluate(()=>window.__GAME_TEST__.getState());
  const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth);
  const layout=await page.evaluate(()=>{const r=document.getElementById("viewport").getBoundingClientRect();return {arenaWidth:r.width,arenaHeight:r.height,arenaAreaFraction:r.width*r.height/(innerWidth*innerHeight),viewport: {width:innerWidth,height:innerHeight}}}); report.push({width,height,errors,overflow,layout,before,allocation,after});
  console.log(JSON.stringify({width,errors,overflow,allocation,after}));
  await page.close();
 }
 fs.writeFileSync(`${out}/visual-smoke.json`,JSON.stringify(report,null,2));
}finally{await browser.close();await server.close()}
