import fs from 'node:fs';
import assert from 'node:assert/strict';
import {homedir} from 'node:os';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../factory/lib/static-server.mjs';
const slug=process.argv[2],live=process.argv.includes('--live');
assert.ok(['twin-forts','sunbasket-farm'].includes(slug));
const farm=slug==='sunbasket-farm',out=`logs/manual-20260907-two-game-polish/${slug}/${live?'live':'host'}`;
fs.mkdirSync(out,{recursive:true});
const server=live?{url:'https://math-game-factory.vercel.app',close:async()=>{}}:await serveStatic(process.cwd()+'/public');
const browser=await puppeteer.launch({headless:true,executablePath:`${homedir()}/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing`,args:['--hide-scrollbars']});
const pause=ms=>new Promise(r=>setTimeout(r,ms)),records=[];
try{
 for(const [width,height] of live?[[390,844]]:[[390,844],[320,720]]){
  const page=await browser.newPage(),errors=[],requests=[];
  await page.setViewport({width,height,deviceScaleFactor:2,isMobile:width<500,hasTouch:width<500});
  page.on('pageerror',e=>errors.push(String(e)));page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});
  page.on('requestfailed',r=>requests.push({url:r.url(),error:r.failure()?.errorText}));
  const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState()),press=selector=>width<500?page.tap(selector):page.click(selector);
  const shot=tag=>page.screenshot({path:`${out}/${tag}-${width}.png`});
  await page.goto(`${server.url}/g/${slug}/`,{waitUntil:'networkidle0'});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});await pause(150);await shot('title');
  await press('#start');await pause(200);await shot('planning');const before=await state();
  if(farm){assert.equal(before.width,4);assert.equal(before.targetBoxes,12);await press('#height-plus');}
  else{assert.equal(before.left,15);for(let i=0;i<3;i++){await press('#move-right');await pause(70);}}
  const allocation=await state();if(farm)assert.equal(allocation.height,3);else{assert.equal(allocation.left,12);assert.equal(allocation.right,18);}
  await shot('allocated');await press(farm?'#plant':'#deploy');await pause(farm?350:1200);await shot(farm?'growing':'battle');
  let layout=null;
  if(farm){
   await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='harvest',{timeout:15000});await shot('harvest');
   layout=await page.evaluate(()=>window.__GAME_TEST__.getLayout());const points=layout.crops.map(c=>({x:c.x??c.screenX,y:c.y??c.screenY}));
   assert.equal(points.length,12);assert.ok(points.every(p=>Number.isFinite(p.x)&&Number.isFinite(p.y)&&p.x>=0&&p.x<=width&&p.y>=0&&p.y<=height));
   if(width<500){const client=await page.createCDPSession();await client.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...points[0],id:1}]});for(const point of points){await client.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...point,id:1}]});await pause(20);}await client.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await client.detach();}
   else{await page.mouse.move(points[0].x,points[0].y);await page.mouse.down();for(const point of points){await page.mouse.move(point.x,point.y,{steps:2});await pause(20);}await page.mouse.up();}
  }
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='success',{timeout:15000});await shot('success');
  const after=await state();assert.equal(after.solved,1);assert.equal(after.lives,3);assert.equal(after.score,farm?40:50);
  const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth);
  const record={slug,width,height,before,allocation,after,layout,overflow,errors,requests};records.push(record);
  if(live){await Promise.all([page.waitForNavigation({waitUntil:'networkidle0'}),press(farm?'.farm-top .home':'header a')]);record.homeNavigation={url:page.url(),heading:await page.$eval('h1',e=>e.textContent)};assert.equal(new URL(page.url()).pathname,'/');}
  assert.equal(overflow,false);assert.deepEqual(errors,[]);assert.deepEqual(requests,[]);
  console.log(JSON.stringify({slug,width,phase:after.phase,solved:after.solved,errors:errors.length,overflow}));await page.close();
 }
}finally{fs.writeFileSync(`${out}/visual-smoke.json`,JSON.stringify(records,null,2)+'\n');await browser.close();await server.close();}
