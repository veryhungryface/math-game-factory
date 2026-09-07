import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../factory/lib/static-server.mjs';
import {oracle,checkSamples} from './verify-math.mjs';

// Do not execute until the parent grants the serialized browser slot.
const OUT=path.dirname(new URL(import.meta.url).pathname);
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
export async function state(page){return page.evaluate(()=>window.__GAME_TEST__.getState());}
export async function waitState(page,predicate,label,timeout=20000){
  const until=Date.now()+timeout;
  while(Date.now()<until){const s=await state(page);if(predicate(s))return s;await sleep(70);}
  throw new Error(`Timeout: ${label}; state=${JSON.stringify(await state(page))}`);
}
export async function clickVisible(page,selector){
  const el=await page.waitForSelector(selector,{visible:true,timeout:15000});
  await el.click();
}
export async function loadCargo(page,value,trace){
  let s=await state(page);
  let actions=0;
  while(s.cargo!==value){
    if(++actions>150)throw new Error('Exceeded cargo recovery action budget');
    if(s.cargo>value){
    const before=s.cargo;
    await clickVisible(page,'#undo');
    s=await waitState(page,t=>t.cargo!==before,'cargo undo');
    } else {
      const place=[100,10,1].find(v=>s.cargo+v<=value);
      const before=s.cargo;
      await clickVisible(page,`[data-place="${place}"]`);
      s=await waitState(page,t=>t.cargo!==before,'cargo pickup');
      trace?.push({event:'pickup',place,before,after:s.cargo,position:s.position});
      if(s.cargo>before&&s.cargo!==before+place)throw new Error(`Single pickup mismatch ${before}+${place} -> ${s.cargo}`);
    }
  }
  if(s.cargo!==value)throw new Error(`Cargo target ${value} not constructed; got ${s.cargo}`);
  return s;
}
export async function submit(page,trace){
  const before=await state(page);
  await clickVisible(page,'#dock');
  let after=before,until=Date.now()+90000;
  while(Date.now()<until){
    after=await state(page);
    if(after.attempts>before.attempts)break;
    if(!after.target){
      trace?.push({event:'flight-interrupted',state:after});
      await loadCargo(page,before.cargo,trace);
      await clickVisible(page,'#dock');
    }
    await sleep(70);
  }
  if(after.attempts<=before.attempts)throw new Error('Docking could not complete within 90 seconds');
  trace?.push({event:'submit',before,after});
  return after;
}
export async function solveCurrent(page,trace){
  const s=await state(page);
  const value=Number(oracle(s.operands.a,s.operands.b));
  await loadCargo(page,value,trace);
  return submit(page,trace);
}
export async function start(page){
  // Start via visible UI, not __GAME_TEST__.start().
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready===true);
  await clickVisible(page,'#start');
  await waitState(page,s=>s.operands&&s.phase!=='title','game start');
}
export async function collectSamples(page){
  const samples=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(2000));
  fs.writeFileSync(path.join(OUT,'samples.json'),JSON.stringify(samples,null,2));
  const result=checkSamples(samples);
  fs.writeFileSync(path.join(OUT,'math-samples-result.json'),JSON.stringify(result,null,2));
  if(result.errors.length)throw new Error(JSON.stringify(result.errors.slice(0,5)));
  return result;
}
export async function openGame(){
  if(!process.env.ORBIT_BROWSER_SLOT)throw new Error('ORBIT_BROWSER_SLOT=1 required after parent grants slot');
  const server=await serveStatic(path.resolve('public'));
  const browser=await puppeteer.launch({headless:true,executablePath:process.env.PUPPETEER_EXECUTABLE_PATH,args:['--no-sandbox']});
  const page=await browser.newPage();
  await page.setViewport({width:1280,height:800,deviceScaleFactor:1});
  const errors=[];page.on('pageerror',e=>errors.push(String(e)));page.on('response',r=>{if(r.status()>=400)errors.push(`${r.status()} ${r.url()}`);});
  await page.goto(`${server.url}/g/orbit-courier/`,{waitUntil:'networkidle0'});
  return {page,browser,server,errors,close:async()=>{await browser.close();await server.close();}};
}
