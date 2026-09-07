import fs from 'node:fs';
import assert from 'node:assert/strict';
import {homedir} from 'node:os';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../factory/lib/static-server.mjs';
const out='logs/manual-20260907-sunbasket-farm/host';fs.mkdirSync(out,{recursive:true});
const server=await serveStatic(process.cwd()+'/public');
const browser=await puppeteer.launch({headless:true,executablePath:`${homedir()}/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing`,args:['--hide-scrollbars']});
const records=[],pause=ms=>new Promise(r=>setTimeout(r,ms));
try{
 for(const [width,height] of [[390,844],[320,720],[820,1180],[1280,800]]){
  const page=await browser.newPage(),errors=[],requests=[];
  await page.setViewport({width,height,deviceScaleFactor:2,isMobile:width<500,hasTouch:width<500});
  page.on('pageerror',e=>errors.push(String(e)));
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});
  page.on('requestfailed',r=>requests.push({url:r.url(),error:r.failure()?.errorText}));
  const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
  const shot=tag=>page.screenshot({path:`${out}/${tag}-${width}.png`});
  const press=selector=>width<500?page.tap(selector):page.click(selector);
  await page.goto(server.url+'/g/sunbasket-farm/',{waitUntil:'networkidle0'});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});await shot('title');
  await press('#start');await pause(200);await shot('planning');
  const before=await state();assert.equal(before.width,4);assert.equal(before.targetBoxes,12);
  await press('#height-plus');const allocation=await state();assert.equal(allocation.height,3);
  await shot('allocated');await press('#plant');await pause(350);await shot('growing');
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='harvest',{timeout:15000});await shot('harvest');
  const layout=await page.evaluate(()=>window.__GAME_TEST__.getLayout());
  const points=layout.crops.map(c=>({x:c.x??c.screenX,y:c.y??c.screenY}));
  assert.ok(points.length>0,'Visible harvest coordinates are required');
  assert.ok(points.every(p=>Number.isFinite(p.x)&&Number.isFinite(p.y)&&p.x>=0&&p.x<=width&&p.y>=0&&p.y<=height),'Every crop must be on screen');
  if(width<500){
   const client=await page.createCDPSession();
   await client.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...points[0],id:1}]});
   for(const point of points){await client.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{...point,id:1}]});await pause(20);}
   await client.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await client.detach();
  }else{
   await page.mouse.move(points[0].x,points[0].y);await page.mouse.down();
   for(const point of points){await page.mouse.move(point.x,point.y,{steps:2});await pause(20);}
   await page.mouse.up();
  }
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='success',{timeout:15000});await shot('success');
  const after=await state();assert.equal(after.solved,1);assert.ok(after.score>0);assert.equal(after.lives,3);
  const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth);
  records.push({width,height,before,allocation,after,layout,overflow,errors,requests});
  assert.equal(overflow,false);assert.deepEqual(errors,[]);assert.deepEqual(requests,[]);
  console.log(JSON.stringify({width,phase:after.phase,harvested:after.harvested,solved:after.solved,overflow,errors}));
  await page.close();
 }
}finally{fs.writeFileSync(`${out}/visual-smoke.json`,JSON.stringify(records,null,2));await browser.close();await server.close();}
