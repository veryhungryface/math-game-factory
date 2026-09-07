import fs from 'node:fs';
import assert from 'node:assert/strict';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../factory/lib/static-server.mjs';
const out='logs/manual-20260907-twin-forts/hint';fs.mkdirSync(out,{recursive:true});
const server=await serveStatic(process.cwd()+'/public');
const browser=await puppeteer.launch({headless:true,executablePath:'/Users/sitpo/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',args:['--no-sandbox']});
const records=[];
try {for(const [width,height] of [[390,844],[1280,800]]){
 const p=await browser.newPage(),errors=[];p.on('pageerror',e=>errors.push(String(e)));await p.setViewport({width,height,deviceScaleFactor:2,isMobile:width<500,hasTouch:width<500});
 await p.goto(server.url+'/g/twin-forts/',{waitUntil:'networkidle0'});await p.waitForFunction(()=>window.__GAME_TEST__?.ready);await p.click('#start');let idle=null;if(width===390){const before=await p.evaluate(()=>window.__GAME_TEST__.getState());await new Promise(r=>setTimeout(r,8000));const after=await p.evaluate(()=>window.__GAME_TEST__.getState());assert.equal(after.solved,0);assert.equal(after.firstAttempts,0);assert.equal(after.allAttempts,0);assert.equal(after.left,before.left);idle={duration_ms:8000,before,after};}await p.click('#move-right');
 const before=await p.evaluate(()=>window.__GAME_TEST__.getState());
 const rect=await p.$eval('#viewport',e=>{const r=e.getBoundingClientRect();return {x:r.x+r.width*.35,y:r.y+r.height*.55}});
 if(width<500)await p.touchscreen.tap(rect.x,rect.y);else await p.mouse.click(rect.x,rect.y);
 const cue=await p.evaluate(()=>({guide:document.getElementById('split-wrap').classList.contains('guide'),note:document.getElementById('order-note').textContent,rippleVisible:!document.querySelector('.arena-touch').hidden}));
 await p.screenshot({path:`${out}/arena-hint-${width}.png`});
 const after=await p.evaluate(()=>window.__GAME_TEST__.getState());assert.equal(before.left,after.left);assert.equal(before.allAttempts,after.allAttempts);assert.equal(before.firstTry,after.firstTry);assert(cue.guide&&cue.rippleVisible);assert(cue.note.includes(width<500?'아래':'오른쪽'));
 await p.click('#deploy');await p.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='retry');
 if(width<500)await p.touchscreen.tap(rect.x,rect.y);else await p.mouse.click(rect.x,rect.y);
 await p.screenshot({path:`${out}/retry-hint-${width}.png`});
 const retry=await p.evaluate(()=>({state:window.__GAME_TEST__.getState(),explanation:document.querySelector('#feedback span').textContent,disabled:document.getElementById('split').disabled}));assert.equal(retry.state.phase,'retry');assert.equal(retry.disabled,false);assert(retry.explanation.includes('12'));
 await p.click('#move-right');await p.click('#move-right');await p.click('#deploy');await p.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='success');const recovered=await p.evaluate(()=>window.__GAME_TEST__.getState());assert.equal(recovered.solved,1);assert.equal(recovered.lives,2);assert.equal(recovered.left,12);assert.equal(errors.length,0);
 records.push({width,idle,before,after,cue,retry,recovered,errors});await p.close();
}fs.writeFileSync(`${out}/report.json`,JSON.stringify(records,null,2));console.log(JSON.stringify(records.map(r=>({width:r.width,cue:r.cue,recovered:r.recovered.phase,errors:r.errors}))));
}finally{await browser.close();await server.close()}
