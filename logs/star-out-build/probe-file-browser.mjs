import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath,pathToFileURL} from 'node:url';
import puppeteer from 'puppeteer';
const here=path.dirname(fileURLToPath(import.meta.url));
const executablePath='/Users/sitpo/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing';
const evidence={at:new Date().toISOString(),method:'single standard Puppeteer launch; file URL only; no server or elevated permissions',executablePath,launched:false,navigated:false,errors:[]};
let browser;
try{
 browser=await puppeteer.launch({headless:true,executablePath,args:['--mute-audio','--hide-scrollbars'],timeout:15000});
 evidence.launched=true;const page=await browser.newPage();
 page.on('pageerror',e=>evidence.errors.push(String(e)));
 await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
 await page.goto(pathToFileURL(path.resolve(here,'../../public/g/star-out/index.html')).href,{waitUntil:'load',timeout:15000});
 evidence.navigated=true;await page.screenshot({path:path.join(here,'file-title.png')});
 evidence.ready=await page.evaluate(()=>window.__GAME_TEST__?.ready===true);
}catch(e){evidence.error=String(e);}
finally{if(browser)await browser.close().catch(()=>{});fs.writeFileSync(path.join(here,'file-browser-probe.json'),JSON.stringify(evidence,null,2)+'\n');console.log(JSON.stringify(evidence,null,2));}
