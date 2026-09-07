import puppeteer from 'puppeteer';
import assert from 'node:assert/strict';
import {mkdir,writeFile,readFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {homedir} from 'node:os';
import {serveStatic} from '../../../../factory/lib/static-server.mjs';
import {uiQuantities,screenshotAndRecord,actualClick,keyboardAllocate,precisionAllocate,
  mouseDragAllocation,touchDragAllocation,visibleAnswer} from './ui-helpers.mjs';

// Do not run until the host has explicitly handed over the serialized browser slot.
if(process.env.TWIN_FORTS_BROWSER_SLOT!=='granted')throw new Error('Host browser-slot grant required');
const root=process.cwd(),out=`${root}/scratchpad/twin-forts-validation/ui`;
await mkdir(out,{recursive:true});
const log=[],issues=[],consoleErrors=[],badRequests=[];
const server=await serveStatic(`${root}/public`);
const browser=await puppeteer.launch({headless:true,
 executablePath:`${homedir()}/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing`,
 args:['--disable-background-timer-throttling','--disable-renderer-backgrounding']});
let page;
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
const snap=(name,details={})=>screenshotAndRecord(page,out,name,log,details);
const waitPhase=phases=>page.waitForFunction(ps=>ps.includes(window.__GAME_TEST__?.getState?.().phase),{timeout:15000},phases);
async function allocateCorrect(mode='keyboard'){
 const {visible,calculated}=await visibleAnswer(page);
 if(mode==='touch'){
  await touchDragAllocation(page,calculated.left/visible.total);
  await precisionAllocate(page,calculated.left);
 }else if(mode==='mouse'){
  await mouseDragAllocation(page,calculated.left/visible.total);
  await precisionAllocate(page,calculated.left);
 }else await keyboardAllocate(page,calculated.left);
 const actual=await uiQuantities(page);
 assert.equal(actual.left,calculated.left);assert.equal(actual.right,calculated.right);
 return {input:mode,visible,calculated,actual};
}
async function deployAndWait(){
 await actualClick(page,'#deploy');
 await waitPhase(['battle']);
 await waitPhase(['success','retry','lost']);
 return state();
}
async function playCampaign(name,wrongRound=null,mode='keyboard'){
 const rounds=[];
 for(let r=1;r<=9;r++){
  await waitPhase(['playing']);
  const initial=await state();assert.equal(initial.round,r);
  if(wrongRound===r){
   const {visible,calculated}=await visibleAnswer(page);
   const wrong=(calculated.left+1)%(visible.total+1);
   await precisionAllocate(page,wrong);
   const before=await state();
   await snap(`${name}-r${r}-intentional-error`,{calculated,wrong});
   const after=await deployAndWait();
   assert.equal(after.lives,before.lives-1);assert.equal(after.phase,'retry');
   await snap(`${name}-r${r}-retry-explanation`);
   await actualClick(page,'#deploy');await waitPhase(['playing']);
   const retry=await uiQuantities(page);
   assert.equal(retry.total,visible.total);assert.equal(retry.a,visible.a);assert.equal(retry.b,visible.b);
  }
  const details=await allocateCorrect(mode);
  await snap(`${name}-r${r}-allocated`,details);
  const before=await state();
  await actualClick(page,'#deploy');await waitPhase(['battle']);
  if(r===1||r===9){await new Promise(resolve=>setTimeout(resolve,1700));await snap(`${name}-r${r}-impact`);}
  await waitPhase(['success','retry','lost']);
  const after=await state();
  assert.equal(after.phase,'success');assert.equal(after.solved,r);assert.equal(after.lives,before.lives);
  await snap(`${name}-r${r}-success`);
  rounds.push({round:r,before,after,input:details});
  await actualClick(page,'#deploy');
 }
 await waitPhase(['won']);
 const result=await state();
 assert.equal(result.solved,9);assert.equal(result.firstAttempts,8);
 assert.equal(result.firstTry,wrongRound?7:8);assert.equal(result.lives,wrongRound?2:3);
 await snap(`${name}-victory`);
 console.log(JSON.stringify({event:'campaign-complete',name,result}));
 return {name,rounds,result};
}
const results={};
try{
 page=await browser.newPage();
 page.on('pageerror',error=>consoleErrors.push(error.message));
 page.on('response',r=>{if(r.status()>=400)badRequests.push({url:r.url(),status:r.status()});});
 await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
 await page.goto(`${server.url}/g/twin-forts/`,{waitUntil:'networkidle0'});
 await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});
 await snap('firstplay-00-title');
 await actualClick(page,'#start');await waitPhase(['playing']);
 await snap('firstplay-01-started');
 const arena=await(await page.$('#arena')).boundingBox();
 await page.touchscreen.tap(arena.x+arena.width*.7,arena.y+arena.height*.62);
 await snap('firstplay-02-arena-tap');
 const beforeTouch=await uiQuantities(page);
 await touchDragAllocation(page,.35);
 const afterTouch=await uiQuantities(page);
 assert.notEqual(afterTouch.left,beforeTouch.left);
 await snap('firstplay-03-touch-drag',{beforeTouch,afterTouch});
 results.normal=await playCampaign('normal',null,'touch');
 results.storageBeforeReload=await page.evaluate(()=>({raw:localStorage.getItem('twin-forts-v1'),text:document.querySelector('#home-level').innerText}));
 await page.reload({waitUntil:'networkidle0'});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});
 results.storageAfterReload=await state();
 assert.equal(results.storageAfterReload.wins,1);assert.ok(results.storageAfterReload.visibleDecorations>=1);
 await snap('saved-growth-title');
 await page.setViewport({width:1280,height:800,deviceScaleFactor:1,isMobile:false,hasTouch:false});
 await actualClick(page,'#start');await waitPhase(['playing']);
 results.oneError=await playCampaign('one-error',2,'keyboard');
 assert.equal(results.oneError.result.wins,2);
 await actualClick(page,'#replay');await waitPhase(['playing']);
 // Skip the guided exercise through real UI, then verify three ordinary failures.
 await allocateCorrect('mouse');await deployAndWait();await actualClick(page,'#deploy');await waitPhase(['playing']);
 const failStart=await uiQuantities(page);
 for(let attempt=1;attempt<=3;attempt++){
  const {visible,calculated}=await visibleAnswer(page);
  assert.equal(visible.total,failStart.total);assert.equal(visible.a,failStart.a);assert.equal(visible.b,failStart.b);
  await precisionAllocate(page,(calculated.left+1)%(visible.total+1));
  const after=await deployAndWait();
  assert.equal(after.lives,3-attempt);
  await snap(`failure-${attempt}`);
  if(attempt<3){assert.equal(after.phase,'retry');await actualClick(page,'#deploy');await waitPhase(['playing']);}
  else assert.equal(after.phase,'lost');
 }
 results.failure=await state();
 await actualClick(page,'#replay');await waitPhase(['playing']);
 results.restarted=await state();assert.equal(results.restarted.lives,3);assert.equal(results.restarted.solved,0);assert.equal(results.restarted.round,1);
 await snap('failure-restarted');
 results.layouts=[];
 for(const [width,height]of[[320,640],[390,844],[820,1180],[1280,800],[2000,1045]]){
  await page.setViewport({width,height,deviceScaleFactor:1,isMobile:width<900,hasTouch:width<900});
  await new Promise(resolve=>setTimeout(resolve,150));
  const layout=await page.evaluate(()=>({width:innerWidth,height:innerHeight,scrollWidth:document.documentElement.scrollWidth,
   arena:(()=>{const r=document.querySelector('#viewport').getBoundingClientRect();return{x:r.x,y:r.y,width:r.width,height:r.height};})(),
   controls:[...document.querySelectorAll('#deployment button,#split')].map(el=>{const r=el.getBoundingClientRect();return{id:el.id,width:r.width,height:r.height,x:r.x,y:r.y};})}));
  assert.ok(layout.scrollWidth<=width);results.layouts.push(layout);await snap(`layout-${width}`);
 }
 const noInputBefore=await state();await new Promise(resolve=>setTimeout(resolve,8000));const noInputAfter=await state();
 assert.equal(noInputAfter.allAttempts,noInputBefore.allAttempts);assert.equal(noInputAfter.solved,noInputBefore.solved);assert.equal(noInputAfter.lives,noInputBefore.lives);
 results.noInput={seconds:8,before:noInputBefore,after:noInputAfter};
 results.errors={consoleErrors,badRequests};assert.equal(consoleErrors.length,0);assert.equal(badRequests.length,0);
 results.verdict='pass';
}catch(error){issues.push({message:error.message,stack:error.stack});results.verdict='fail';if(page)await snap('error-current').catch(()=>{});console.error(error.stack);}
finally{
 results.issues=issues;results.created_at=new Date().toISOString();results.uiOnly=true;
 results.artifactHashes={};for(const f of ['index.html','game.js','style.css','math.mjs','meta.json'])results.artifactHashes[f]=createHash('sha256').update(await readFile(`${root}/public/g/twin-forts/${f}`)).digest('hex');
 await writeFile(`${out}/results.json`,JSON.stringify(results,null,2));
 await browser.close();await server.close();
 console.log(JSON.stringify({event:'validation-finished',verdict:results.verdict,issues,output:`${out}/results.json`}));
}
