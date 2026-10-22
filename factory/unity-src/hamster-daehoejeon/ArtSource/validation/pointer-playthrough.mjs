#!/usr/bin/env node
// 실제 mouse down/move/up만으로 첫 연습, 무실수 완주, 첫 오답 수리 완주, 3오답 종료를 검증한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/hamster-daehoejeon');
const W = 390, H = 844;
const sleep = ms => new Promise(r => setTimeout(r, ms));

function chromePath(){
  if(process.env.PUPPETEER_EXECUTABLE_PATH)return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  for(const build of fs.readdirSync(base).sort().reverse())for(const rel of ['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-linux64/chrome']){const p=path.join(base,build,rel);if(fs.existsSync(p))return p;}
  throw new Error('Chrome for Testing not found');
}
function artifactHash(){const h=crypto.createHash('sha256');for(const n of ['index.html',...fs.readdirSync(path.join(GAME,'Build')).sort().map(f=>`Build/${f}`)]){h.update(n);h.update(fs.readFileSync(path.join(GAME,n)));}return h.digest('hex');}
async function state(page){return page.evaluate(()=>window.__GAME_TEST__.getState());}
async function waitFor(page,pred,label,timeout=5000){const began=Date.now();while(Date.now()-began<timeout){const s=await state(page);if(pred(s))return s;await sleep(15);}throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);}
function answer(s){if(String(s.currentProblemId).startsWith('practice-'))return 70;if(String(s.currentProblemId).startsWith('bridge-'))return 32;const m=String(s.currentProblemId).match(/-k(\d+)/);if(!m)throw new Error(`no k in ${s.currentProblemId}`);const k=Number(m[1]);return s.band===1?20+k:s.band===2?20+2*k:10+k;}
function browserPoint(x,unityY){return [x,H-unityY];}
function targetPoint(s,value,extraRadius=0){const index=(value-s.allowedMin)/s.allowedStep;const deg=(160-(140*index/59))*Math.PI/180;return browserPoint(s.dialCenterX+Math.cos(deg)*(s.dialRadius+extraRadius),s.dialCenterY+Math.sin(deg)*(s.dialRadius+extraRadius));}
async function submit(page,value){
  const s=await waitFor(page,x=>x.phase==='playing'&&x.handleX>0&&x.dialRadius>0,'dial');
  const from=browserPoint(s.handleX,s.handleY),to=targetPoint(s,value);
  const nudge=targetPoint(s,s.allowedMin+s.allowedStep*((Math.round((value-s.allowedMin)/s.allowedStep)+1)%60),8);
  await page.mouse.move(...from);await page.mouse.down();await sleep(28);await page.mouse.move(...nudge,{steps:2});await page.mouse.move(...to,{steps:5});await sleep(28);await page.mouse.up();
  return waitFor(page,x=>x.phase==='reveal'||x.phase==='clear'||x.phase==='gameover',`submit ${value}`);
}
async function titlePractice(page){
  await page.mouse.click(W/2,H*.69); // 검은 손잡이
  const p=await waitFor(page,s=>s.phase==='playing'&&s.onboarding&&s.handleX>0,'first practice');
  const r=await submit(page,70);
  const run=await waitFor(page,s=>s.phase==='playing'&&!s.onboarding&&!s.bridgePractice&&s.round===1,'run after practice');
  return {practice:p,last:r,run,passed:r.lastResult==='correct'&&run.round===1};
}
async function start(page){await page.evaluate(()=>window.__GAME_TEST__.start());return waitFor(page,s=>s.phase==='playing'&&s.round===1&&s.handleX>0,'test start');}
async function finishCorrect(page,startNew=true){
  if(startNew)await start(page);const attempts=[];let guard=0;
  while(guard++<20){const before=await waitFor(page,s=>s.phase==='playing'&&s.handleX>0,'next dial');const a=answer(before);const after=await submit(page,a);attempts.push({id:before.currentProblemId,input:a,result:after.lastResult,solved:after.solved,bridge:before.bridgePractice});if(after.phase==='clear')return {final:after,attempts};await waitFor(page,s=>s.phase==='clear'||(s.phase==='playing'&&(s.currentProblemId!==before.currentProblemId||s.bridgePractice!==before.bridgePractice)),'advance');const now=await state(page);if(now.phase==='clear')return {final:now,attempts};}
  throw new Error('finish guard');
}
async function recovery(page){
  const first=await start(page),a=answer(first),wrong=a===first.allowedMax?a-first.allowedStep:a+first.allowedStep;const attempts=[];
  let after=await submit(page,wrong);attempts.push({id:first.currentProblemId,input:wrong,result:after.lastResult,lives:after.lives,firstCorrect:after.firstCorrect});
  await waitFor(page,s=>s.phase==='playing'&&s.currentProblemId===first.currentProblemId,'same frame repair');after=await submit(page,a);attempts.push({id:first.currentProblemId,input:a,result:after.lastResult,lives:after.lives});
  const rest=await finishCorrect(page,false);return {final:rest.final,attempts:[...attempts,...rest.attempts]};
}
async function gameover(page){
  const first=await start(page),a=answer(first),wrong=a===first.allowedMax?a-first.allowedStep:a+first.allowedStep,attempts=[];let after;
  for(let i=0;i<3;i++){after=await submit(page,wrong);attempts.push({phase:after.phase,lives:after.lives,firstAttempts:after.firstAttempts});if(after.phase==='gameover')break;const next=await waitFor(page,s=>s.phase==='playing'||s.phase==='gameover','retry');if(next.phase==='gameover'){after=next;break;}}
  return {final:after,attempts};
}

const server=await serveStatic(PUBLIC);const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),protocolTimeout:900000,args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});const consoleErrors=[];
try{
  const page=await browser.newPage();await page.setViewport({width:W,height:H,deviceScaleFactor:1});page.on('console',m=>{if(m.type()==='error')consoleErrors.push(m.text());});page.on('pageerror',e=>consoleErrors.push(String(e)));
  await page.goto(`${server.url}/g/hamster-daehoejeon/`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});await page.screenshot({path:path.join(HERE,'00-title.png')});
  const bank=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(1080));const ids=new Set(bank.map(p=>p.id)),prompts=new Set(bank.map(p=>p.prompt+'|'+p.answer));
  const bankAudit={count:bank.length,uniqueIds:ids.size,uniquePrompts:prompts.size,integerAnswers:bank.every(p=>Number.isInteger(p.answerNumeric)&&p.answer===`${p.answerNumeric}°`),wording:bank.every(p=>p.prompt.endsWith('크기를 구하시오.')&&!/[√]/.test(p.prompt)&&!/(가정|결론)/.test(p.prompt))};
  const onboarding=await titlePractice(page);await page.screenshot({path:path.join(HERE,'01-first-run.png')});
  const clean=await finishCorrect(page,false);await page.screenshot({path:path.join(HERE,'02-clean-clear.png')});
  const second=await finishCorrect(page,true);const differentDeck=clean.attempts.filter(a=>!a.bridge).map(a=>a.id).join('|')!==second.attempts.filter(a=>!a.bridge).map(a=>a.id).join('|');
  const repaired=await recovery(page);await page.screenshot({path:path.join(HERE,'03-recovery-clear.png')});
  const failed=await gameover(page);await page.screenshot({path:path.join(HERE,'04-gameover.png')});
  const passed=bankAudit.count===1080&&bankAudit.uniqueIds===1080&&bankAudit.uniquePrompts===1080&&bankAudit.integerAnswers&&bankAudit.wording&&onboarding.passed&&clean.final.phase==='clear'&&clean.final.solved===8&&clean.final.firstCorrect===8&&clean.final.lives===3&&second.final.phase==='clear'&&differentDeck&&repaired.final.phase==='clear'&&repaired.final.solved===8&&repaired.final.firstCorrect===7&&repaired.final.lives===2&&failed.final.phase==='gameover'&&failed.final.lives===0&&consoleErrors.length===0;
  const out={generatedAt:new Date().toISOString(),artifactHashSha256:artifactHash(),viewport:`${W}x${H}`,method:'every submission uses real Puppeteer mouse down/move/up on the runtime dial; hooks only start fresh runs',bankAudit,onboarding,clean,second,differentDeck,repaired,gameover:failed,consoleErrors,passed};fs.writeFileSync(path.join(HERE,'pointer-playthrough-results.json'),JSON.stringify(out,null,2)+'\n');console.log(JSON.stringify({bankAudit,onboarding:onboarding.passed,clean:clean.final,second:second.final,differentDeck,repaired:repaired.final,gameover:failed.final,consoleErrors,passed},null,2));if(!passed)process.exitCode=1;
}finally{await browser.close();await server.close();}
