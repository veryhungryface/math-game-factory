// Narrow real-input regression: tutorial, one ordinary error, direct correction and completion.
import puppeteer from 'puppeteer';
import assert from 'node:assert/strict';
import {mkdir,readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {homedir} from 'node:os';
import {serveStatic} from '../../../factory/lib/static-server.mjs';
import * as fort from '../../manual-20260907-twin-forts/validation/scripts/ui-helpers.mjs';
import * as farm from '../../manual-20260907-sunbasket-farm/validation/ui-helpers.mjs';
if(process.env.POLISH_BROWSER_SLOT!=='granted')throw new Error('Parent browser slot grant required');
const slug=process.argv[2];assert.ok(['twin-forts','sunbasket-farm'].includes(slug));
const output=`logs/manual-20260907-two-game-polish/validation/${slug}-recovery`;
await mkdir(output,{recursive:true});
const sha=b=>createHash('sha256').update(b).digest('hex');
const sourceHash=sha(await readFile(`public/g/${slug}/game.js`)),errors=[],rounds=[],log=[];
const server=await serveStatic(`${process.cwd()}/public`);
const browser=await puppeteer.launch({headless:true,executablePath:process.env.REVIEW_CHROME||`${homedir()}/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing`,args:['--disable-background-timer-throttling','--disable-renderer-backgrounding']});
let result;
try{
 const page=await browser.newPage();page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
 await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
 await page.goto(`${server.url}/g/${slug}/`,{waitUntil:'networkidle0'});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});
 const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
 const wait=phases=>page.waitForFunction(p=>p.includes(window.__GAME_TEST__?.getState().phase),{timeout:15000},phases);
 const snap=(name,details={})=>(slug==='twin-forts'?fort:farm).screenshotAndRecord(page,output,name,log,details);
 await snap('title');await farm.actualClick(page,'#start',{touch:true});
 for(let r=1;r<=2;r++){
  await wait(['playing']);
  const before=await state();let wrong=null;
  if(slug==='twin-forts'){
   const visible=await fort.visibleAnswer(page),target=visible.calculated.left;
   if(r===2){await fort.precisionAllocate(page,target===visible.visible.total?target-1:target+1);await farm.actualClick(page,'#deploy',{touch:true});await wait(['retry']);wrong=await state();assert.equal(wrong.lives,2);assert.equal(wrong.firstTry,0);assert.equal(wrong.firstAttempts,1);await snap('ordinary-wrong',{visible,wrong});}
   await fort.touchDragAllocation(page,target/visible.visible.total);await fort.precisionAllocate(page,target);
   await snap(`r${r}-corrected`,{visible,wrong});await farm.actualClick(page,'#deploy',{touch:true});await wait(['success']);
   const after=await state();assert.equal(after.solved,r);assert.equal(after.lives,r===1?3:2);assert.equal(after.score,r===1?50:130);if(r===2){assert.equal(after.firstTry,0);assert.equal(after.firstAttempts,1);assert.equal(after.allAttempts,3);}
   rounds.push({round:r,visible,before,wrong,after});await snap(`r${r}-success`);
   if(r===1)await farm.actualClick(page,'#deploy',{touch:true});
  }else{
   const visible=await farm.visibleProblem(page),target=visible.calculated.expectedHeight;
   if(r===2){await farm.precisionHeight(page,target===12?11:target+1,{touch:true});await farm.actualClick(page,'#plant',{touch:true});await wait(['retry']);wrong=await state();assert.equal(wrong.lives,2);assert.equal(wrong.firstTry,0);assert.equal(wrong.firstAttempts,1);await snap('ordinary-wrong',{visible,wrong});}
   const current=await farm.visibleProblem(page),layout=await page.evaluate(()=>window.__GAME_TEST__.getLayout());
   const h=layout.handle,c=layout.actualFieldCorners,from={x:h.x+h.width/2,y:h.y+h.height/2},dx=(c[2].x-c[1].x)/current.visible.chosenHeight*(target-current.visible.chosenHeight),dy=(c[2].y-c[1].y)/current.visible.chosenHeight*(target-current.visible.chosenHeight);
   await farm.touchPath(page,Array.from({length:13},(_,i)=>({x:from.x+dx*i/12,y:from.y+dy*i/12})));await farm.precisionHeight(page,target,{touch:true});
   await snap(`r${r}-corrected`,{visible,wrong});await farm.actualClick(page,'#plant',{touch:true});await wait(['harvest']);
   const ripe=await state(),points=await page.evaluate(()=>window.__GAME_TEST__.getLayout().crops);
   const blocked=await page.evaluate(ps=>ps.filter(p=>p.x<0||p.x>=innerWidth||p.y<0||p.y>=innerHeight||document.elementFromPoint(p.x,p.y)?.id!=='farm-canvas'),points);assert.equal(blocked.length,0);await snap(`r${r}-ripe`,{cropCount:points.length,blocked});
   await page.touchscreen.tap(points[0].x,points[0].y);const once=await state();await page.touchscreen.tap(points[0].x,points[0].y);const twice=await state();assert.equal(twice.harvested,once.harvested);
   if(twice.phase==='harvest'){const remaining=await page.evaluate(()=>window.__GAME_TEST__.getLayout().crops);await farm.touchPath(page,remaining.sort((a,b)=>a.row-b.row||(a.row%2?b.col-a.col:a.col-b.col)));}
   await wait(['success']);const after=await state();assert.equal(after.harvested,ripe.harvestTotal);assert.equal(after.solved,r);assert.equal(after.lives,r===1?3:2);assert.equal(after.score,r===1?40:110);assert.equal(after.coins-before.coins,visible.visible.targetBoxes);if(r===2){assert.equal(after.firstTry,0);assert.equal(after.firstAttempts,1);assert.equal(after.allAttempts,3);}
   rounds.push({round:r,visible,before,wrong,ripe,duplicate:{once:once.harvested,twice:twice.harvested},after});await snap(`r${r}-success`);
   if(r===1)await farm.actualClick(page,'#plant',{touch:true});
  }
 }
 assert.equal(errors.length,0);assert.equal(sha(await readFile(`public/g/${slug}/game.js`)),sourceHash,'Source changed during browser test');
 result={slug,verdict:'pass',reviewed_at:new Date().toISOString(),source_sha256:sourceHash,scope:'390px actual touch/button input on tutorial and one ordinary question; one ordinary error followed by direct touch correction. Read-only state/geometry hooks; answers independently derived from visible quantities. No RNG fixture, no full campaign rerun, no claim of maximum-count coverage.',rounds,errors};
}catch(e){result={slug,verdict:'fail',source_sha256:sourceHash,rounds,errors,failure:e.stack};process.exitCode=1;}
finally{await browser.close();await server.close();}
await writeFile(`${output}/results.json`,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({slug,verdict:result.verdict,rounds:rounds.length,errors,failure:result.failure??null,output}));
