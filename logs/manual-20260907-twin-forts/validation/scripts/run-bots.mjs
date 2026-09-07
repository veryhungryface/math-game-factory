import puppeteer from 'puppeteer';
import assert from 'node:assert/strict';
import {mkdir,writeFile} from 'node:fs/promises';
import {homedir} from 'node:os';
import {serveStatic} from '../../../../factory/lib/static-server.mjs';
import {oracle,accepts} from './oracle.mjs';
import {uiQuantities,actualClick,precisionAllocate,mouseDragAllocation} from './ui-helpers.mjs';
if(process.env.TWIN_FORTS_BROWSER_SLOT!=='granted')throw new Error('Host browser-slot grant required');
const root=process.cwd(),out=`${root}/scratchpad/twin-forts-validation/bots`;
await mkdir(out,{recursive:true});
const server=await serveStatic(`${root}/public`);
const browser=await puppeteer.launch({headless:true,
 executablePath:`${homedir()}/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing`,
 args:['--disable-background-timer-throttling','--disable-renderer-backgrounding']});
const page=await browser.newPage();await page.setViewport({width:1280,height:800,deviceScaleFactor:1});
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
const phase=phases=>page.waitForFunction(ps=>ps.includes(window.__GAME_TEST__?.getState?.().phase),{timeout:12000},phases);
let seed=72912;
const random=()=>{seed^=seed<<13;seed^=seed>>>17;seed^=seed<<5;return(seed>>>0)/4294967296;};
const policies={
 'always-half-floor':p=>Math.floor(p.total/2),
 'always-left-12':p=>Math.min(12,p.total),
 'always-left-40-percent':p=>Math.round(p.total*.4),
 'always-left-end':()=>0,
 'always-right-end':p=>p.total,
 'uniform-divider-integer-0-N':p=>Math.floor(random()*(p.total+1)),
 'repeat-sweep-end-left':()=>0,
};
const results={created_at:new Date().toISOString(),source:'Real mouse/keyboard UI events; no answer hooks, no state mutation',
 method:'Each policy is tested on three ordinary first attempts. Correct UI recovery only advances to the next problem and is excluded. Large-sample policy baselines are in math-source-report.json; these small UI samples test policy-to-input fidelity, not population-rate significance.',policies:[],issues:[]};
async function submit(){await actualClick(page,'#deploy');await phase(['battle']);await phase(['success','retry','lost']);return state();}
async function correctVisible(){const p=await uiQuantities(page);await precisionAllocate(page,oracle(p.total,p.a,p.b).left);return submit();}
try{
 for(const [name,select]of Object.entries(policies)){
  await page.goto(`${server.url}/g/twin-forts/`,{waitUntil:'networkidle0'});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});
  await actualClick(page,'#start');await phase(['playing']);await correctVisible();await actualClick(page,'#deploy');await phase(['playing']);
  const trials=[];
  for(let r=0;r<3;r++){
   const p=await uiQuantities(page),target=select(p),expected=accepts(p.total,p.a,p.b,target);
   if(name==='repeat-sweep-end-left'){
    for(const ratio of [1,0,1,0])await mouseDragAllocation(page,ratio);
   }else await precisionAllocate(page,target);
   const allocated=await uiQuantities(page);assert.equal(allocated.left,target);
   const before=await state();assert.equal(before.attempts,0);assert.equal(before.tutorial,false);
   const after=await submit(),actual=after.phase==='success';
   assert.equal(actual,expected);assert.equal(after.firstAttempts,before.firstAttempts+1);
   assert.equal(after.firstTry,before.firstTry+(expected?1:0));
   trials.push({problem:{total:p.total,a:p.a,b:p.b},target,expected,actual,before,after});
   if(!actual&&r<2){
    assert.equal(after.phase,'retry');await actualClick(page,'#deploy');await phase(['playing']);await correctVisible();
   }
   if(r<2){await actualClick(page,'#deploy');await phase(['playing']);}
  }
  const row={name,trials,firstTryCorrect:trials.filter(x=>x.actual).length,expectedCorrect:trials.filter(x=>x.expected).length,total:trials.length};
  results.policies.push(row);console.log(JSON.stringify({event:'policy-tested',name,correct:row.firstTryCorrect,total:row.total}));
  await writeFile(`${out}/report.json`,JSON.stringify(results,null,2));
 }
 results.verdict='pass';
}catch(error){results.verdict='fail';results.issues.push({message:error.message,stack:error.stack});console.error(error.stack);await page.screenshot({path:`${out}/error.png`}).catch(()=>{});}
finally{await writeFile(`${out}/report.json`,JSON.stringify(results,null,2));await browser.close();await server.close();console.log(JSON.stringify({event:'bots-finished',verdict:results.verdict,output:`${out}/report.json`}));}
