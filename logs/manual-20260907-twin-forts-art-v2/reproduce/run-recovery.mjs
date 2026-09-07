import puppeteer from 'puppeteer';
import assert from 'node:assert/strict';
import {mkdir,writeFile,readFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {homedir} from 'node:os';
import {serveStatic} from '../../../factory/lib/static-server.mjs';
import {uiQuantities,actualClick,precisionAllocate,keyboardAllocate,touchDragAllocation,
 visibleAnswer,screenshotAndRecord} from './ui-helpers.mjs';
if(process.env.TWIN_FORTS_BROWSER_SLOT!=='granted')throw new Error('Host browser-slot grant required');
const root=process.cwd(),out=`${root}/logs/manual-20260907-twin-forts-art-v2/recovery`;
await mkdir(out,{recursive:true});
const log=[],results={inputs:[],issues:[]};
const server=await serveStatic(`${root}/public`);
const browser=await puppeteer.launch({headless:true,
 executablePath:`${homedir()}/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing`,
 args:['--disable-background-timer-throttling','--disable-renderer-backgrounding']});
const page=await browser.newPage();
const errors=[];page.on('pageerror',e=>errors.push(e.message));
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
const phase=phases=>page.waitForFunction(ps=>ps.includes(window.__GAME_TEST__?.getState?.().phase),{timeout:12000},phases);
const snap=(name,details={})=>screenshotAndRecord(page,out,name,log,details);
async function submit(){await actualClick(page,'#deploy');await phase(['battle']);await phase(['success','retry','lost']);return state();}
async function correct(){const {calculated}=await visibleAnswer(page);await precisionAllocate(page,calculated.left);return submit();}
try{
 for(const input of ['touch','precision','keyboard']){
  await page.setViewport(input==='touch'?{width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true}:{width:1280,height:800,deviceScaleFactor:1});
  await page.goto(`${server.url}/g/twin-forts/`,{waitUntil:'networkidle0'});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});
  await actualClick(page,'#start');await phase(['playing']);
  await correct();await actualClick(page,'#deploy');await phase(['playing']);
  const {visible,calculated}=await visibleAnswer(page);
  const wrong=calculated.left+1;
  await precisionAllocate(page,wrong);await submit();await phase(['retry']);
  const before=await state();assert.equal(before.lives,2);
  const disabled=await page.evaluate(()=>({range:document.querySelector('#split').disabled,left:document.querySelector('#move-left').disabled,right:document.querySelector('#move-right').disabled,guide:document.querySelector('#split-wrap').classList.contains('guide')}));
  assert.equal(disabled.range,false);assert.equal(disabled.left,false);assert.equal(disabled.right,false);
  await snap(`${input}-retry-before-direct-input`,{calculated,disabled});
  // Deliberately DO NOT press the intermediate repair button.
  if(input==='touch'){
   await touchDragAllocation(page,calculated.left/visible.total);
   await precisionAllocate(page,calculated.left);
  }else if(input==='precision')await precisionAllocate(page,calculated.left);
  else await keyboardAllocate(page,calculated.left);
  const afterInput=await state();
  assert.equal(afterInput.phase,'playing');assert.equal(afterInput.left,calculated.left);assert.equal(afterInput.right,calculated.right);
  assert.equal(afterInput.lives,2);assert.equal(afterInput.attempts,1);
  await snap(`${input}-directly-repaired`,{before,afterInput,action:'Direct allocation input; no repair-button click'});
  const after=await submit();assert.equal(after.phase,'success');assert.equal(after.solved,2);assert.equal(after.firstTry,0);assert.equal(after.firstAttempts,1);
  results.inputs.push({input,problem:visible,calculated,before,afterInput,after});
  await snap(`${input}-repaired-success`);
  if(input==='touch'){
   for(let round=3;round<=9;round++){
    await actualClick(page,'#deploy');await phase(['playing']);
    const {visible:vp,calculated:answer}=await visibleAnswer(page);
    await touchDragAllocation(page,answer.left/vp.total);await precisionAllocate(page,answer.left);
    await submit();await phase(['success']);
    await snap(`recovered-campaign-r${round}-success`);
   }
   await actualClick(page,'#deploy');await phase(['won']);
   const won=await state();assert.equal(won.solved,9);assert.equal(won.lives,2);assert.equal(won.firstTry,7);assert.equal(won.firstAttempts,8);assert.equal(won.score,970);
   results.fullRecovery=won;await snap('recovered-campaign-victory');
   await page.reload({waitUntil:'networkidle0'});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});
   const persisted=await state();assert.equal(persisted.wins,1);assert.equal(persisted.visibleDecorations,1);results.persisted=persisted;
  }
  console.log(JSON.stringify({event:'direct-recovery-verified',input,solved:after.solved}));
 }
 // Verify actual keyboard focus parity: native range and global keys must both
 // move one soldier in the indicated direction without submitting or leaking.
 results.errors=errors;assert.equal(errors.length,0);results.verdict='pass';
}catch(error){results.verdict='fail';results.issues.push({message:error.message,stack:error.stack});console.error(error.stack);await snap('error').catch(()=>{});}
finally{
 results.created_at=new Date().toISOString();results.artifactHashes={};
 for(const f of ['index.html','game.js','style.css','math.mjs'])results.artifactHashes[f]=createHash('sha256').update(await readFile(`${root}/public/g/twin-forts/${f}`)).digest('hex');
 await writeFile(`${out}/results.json`,JSON.stringify(results,null,2));
 await browser.close();await server.close();console.log(JSON.stringify({event:'recovery-finished',verdict:results.verdict,issues:results.issues}));
}
