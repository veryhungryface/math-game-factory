import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';
const root=process.cwd(),out=path.join(root,'factory/unity-src/hyeopgok-sasu/ArtSource/validation');
const server=await serveStatic(path.join(root,'public'));
const browser=await puppeteer.launch({headless:true,executablePath:process.env.PUPPETEER_EXECUTABLE_PATH||path.join(process.env.HOME,'.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'),args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage();const errors=[];page.on('pageerror',e=>errors.push(String(e)));page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});
const wait=ms=>new Promise(r=>setTimeout(r,ms));
try{
 await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
 await page.goto(server.url+'/g/hyeopgok-sasu/',{waitUntil:'networkidle0'});
 await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});
 await wait(1800);await page.screenshot({path:path.join(out,'title-390.png')});
 await page.evaluate(()=>window.__GAME_TEST__.start());await wait(1200);
 await page.screenshot({path:path.join(out,'playing-390.png')});
 console.log(JSON.stringify({state:await page.evaluate(()=>window.__GAME_TEST__.getState()),errors}));
 await page.evaluate(()=>window.__GAME_TEST__.answerCorrect());await wait(300);await page.screenshot({path:path.join(out,'correct-390.png')});
 await page.setViewport({width:1280,height:800,deviceScaleFactor:1,isMobile:false,hasTouch:false});
 await page.goto(server.url+'/g/hyeopgok-sasu/',{waitUntil:'networkidle0'});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});await page.evaluate(()=>window.__GAME_TEST__.start());await wait(1300);await page.screenshot({path:path.join(out,'playing-1280.png')});
 console.log(JSON.stringify({desktop:await page.evaluate(()=>window.__GAME_TEST__.getState()),errors}));
}finally{await browser.close();await server.close();}
