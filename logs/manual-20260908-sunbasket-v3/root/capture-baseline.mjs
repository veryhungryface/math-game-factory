import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../factory/lib/static-server.mjs';

const project=process.cwd(), beforeRoot='/Users/sitpo/math-game-factory';
const out=path.join(project,'logs/manual-20260908-sunbasket-v3/root/before');
fs.mkdirSync(out,{recursive:true});
const game=path.join(beforeRoot,'public/g/sunbasket-farm');
function walk(p){return fs.readdirSync(p,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(p,e.name)):[path.join(p,e.name)]);}
const files=Object.fromEntries(walk(game).map(p=>[path.relative(game,p),crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex')]));
fs.writeFileSync(path.join(out,'files.json'),JSON.stringify(files,null,2));
const server=await serveStatic(path.join(beforeRoot,'public'));
const browser=await puppeteer.launch({headless:true,executablePath:process.env.HOME+'/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',args:['--no-sandbox']});
const report={source:beforeRoot,files,errors:[],requests:[],views:[]};
try {
  for(const width of [390,820,1280]) {
    const page=await browser.newPage();
    await page.setViewport({width,height:width===820?1180:width===1280?900:844,deviceScaleFactor:1,hasTouch:true});
    page.on('pageerror',e=>report.errors.push(e.message));
    page.on('requestfailed',r=>report.requests.push({url:r.url(),failure:r.failure()}));
    await page.goto(server.url+'/g/sunbasket-farm/',{waitUntil:'networkidle0'});
    await page.waitForFunction(()=>window.__GAME_TEST__?.ready);
    await page.screenshot({path:path.join(out,`title-${width}.png`)});
    await page.click('#start');
    await page.screenshot({path:path.join(out,`planning-${width}.png`)});
    await page.keyboard.press('ArrowUp');
    await page.click('#plant');
    await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='harvest');
    await page.screenshot({path:path.join(out,`harvest-${width}.png`)});
    report.views.push(await page.evaluate(()=>({width:innerWidth,state:window.__GAME_TEST__.getState(),layout:window.__GAME_TEST__.getLayout(),overflow:document.documentElement.scrollWidth>innerWidth})));
    await page.close();
  }
} finally {
  await browser.close();await server.close();
  fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2));
}
console.log(JSON.stringify({views:report.views.map(v=>({width:v.width,phase:v.state.phase,overflow:v.overflow})),errors:report.errors,requests:report.requests}));
