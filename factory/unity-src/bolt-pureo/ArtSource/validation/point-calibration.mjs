import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const sleep = ms => new Promise(r => setTimeout(r, ms));
function chrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
  ]) { const p=path.join(base,build,rel); if(fs.existsSync(p)) return p; }
}
const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chrome(),args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage(); await page.setViewport({width:390,height:844,deviceScaleFactor:1});
await page.goto(`${server.url}/g/bolt-pureo/?calibrate=1`,{waitUntil:'load',timeout:45000});
await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
await page.evaluate(()=>window.__GAME_TEST__.start()); await sleep(80);
const found=new Map();
for(let y=185;y<=720;y+=16) for(let x=45;x<=345;x+=16){
  await page.mouse.move(x,y); await page.mouse.down(); await sleep(24);
  const s=await page.evaluate(()=>window.__GAME_TEST__.getState()); await page.mouse.up(); await sleep(3);
  if(s.heldBolt>=0){ const a=found.get(s.heldBolt)||[]; a.push({x,y}); found.set(s.heldBolt,a); }
}
console.log(JSON.stringify([...found.entries()].sort((a,b)=>a[0]-b[0]).map(([heldBolt,v])=>({
  heldBolt, x:Math.round(v.reduce((s,p)=>s+p.x,0)/v.length), y:Math.round(v.reduce((s,p)=>s+p.y,0)/v.length), samples:v.length
})),null,2));
await page.close(); await browser.close(); await server.close();
