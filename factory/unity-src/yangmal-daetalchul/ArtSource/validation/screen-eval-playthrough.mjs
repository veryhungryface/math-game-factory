#!/usr/bin/env node
// 답을 훅·problemId·문제은행에서 읽지 않는 화면 평가자 두 판.
// macOS Vision OCR로 화면 발문을 읽고 접선 길이를 직접 계산한 뒤 Unity 캔버스에 실제 mouse drag를 보낸다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url)),ROOT=path.resolve(HERE,'../../../../..'),PUBLIC=path.join(ROOT,'public'),GAME=path.join(PUBLIC,'g','yangmal-daetalchul'),OUT=path.join(HERE,'screen-eval');
const EYE=process.env.SCREEN_OCR||'/tmp/yangmal-screen-ocr',W=390,H=844,DPR=2,RUN_ID=process.env.MGF_RUN_ID||`yangmal-screen-eval-${new Date().toISOString()}`;
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
fs.rmSync(OUT,{recursive:true,force:true});fs.mkdirSync(OUT,{recursive:true});
function chromePath(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const build of fs.readdirSync(base).sort().reverse())for(const rel of['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-linux64/chrome']){const p=path.join(base,build,rel);if(fs.existsSync(p))return p;}throw new Error('Chrome not found');}
function hash(){const h=crypto.createHash('sha256');for(const rel of['index.html',...fs.readdirSync(path.join(GAME,'Build')).sort().map(n=>'Build/'+n)]){h.update(rel);h.update(fs.readFileSync(path.join(GAME,rel)));}return h.digest('hex');}
const eye=(...args)=>JSON.parse(execFileSync(EYE,args,{encoding:'utf8'}));
const server=await serveStatic(PUBLIC),browser=await puppeteer.launch({headless:true,executablePath:chromePath(),protocolTimeout:180000,args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const page=await browser.newPage();await page.setViewport({width:W,height:H,deviceScaleFactor:DPR});
const errors=[];page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});page.on('pageerror',e=>errors.push(String(e)));
await page.goto(`${server.url}/g/yangmal-daetalchul/`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});await sleep(1200);
const beforeHash=hash(),tmp=path.join(OUT,'_look.png'),log=[];let shot=0;
async function look(){await page.screenshot({path:tmp});const words=eye('ocr',tmp),all=words.map(x=>x.text).join(' | '),norm=all.replace(/[\s|]/g,'').replace(/[，]/g,',');return{words,all,norm,practice:/연습/.test(all),end:/아홉양말탈출|안전주머니닫힘/.test(norm)};}
async function keep(name){const file=`${String(shot++).padStart(2,'0')}-${name}.png`;fs.copyFileSync(tmp,path.join(OUT,file));return file;}
function solve(norm){
  let m=norm.match(/선분PA의길이가(\d+)cm.*선분PO의길이가(\d+)cm/);if(m)return{band:1,answer:+m[1],calculation:`PB=PA=${m[1]}`};
  m=norm.match(/AB=(\d+)cm.*BD=(\d+)cm/);if(m)return{band:2,answer:+m[1]-+m[2],calculation:`AF=AD=AB-BD=${m[1]}-${m[2]}=${+m[1]-+m[2]}`};
  m=norm.match(/AB=(\d+)cm.*BC=(\d+)cm.*CF=(\d+)cm/);if(m){const answer=+m[1]-(+m[2]-+m[3]);return{band:3,answer,calculation:`CE=CF=${m[3]}, BD=BE=${m[2]}-${m[3]}, AF=AD=${m[1]}-(${m[2]}-${m[3]})=${answer}`};}
  throw new Error(`화면 발문 파싱 실패: ${norm}`);
}
async function dragCm(cm,label,startCm=1){
  await look();const box=eye('ruler',tmp),x=(box.x+box.w/2)/DPR,bottom=(box.y+box.h)/DPR,y=bottom-29-(cm-1)/23*292;
  const y0=bottom-29-(startCm-1)/23*292;await page.mouse.move(x,y0);await page.mouse.down();for(let i=1;i<=10;i++){await page.mouse.move(x,y0+(y-y0)*i/10);await sleep(18);}await page.mouse.up();
  log.push({kind:'real_pointer_drag',label,start_cm:startCm,submitted_cm:cm,ruler_px:box,from_css:[+x.toFixed(1),+y0.toFixed(1)],to_css:[+x.toFixed(1),+y.toFixed(1)]});
}
async function waitForPrompt(previous='',timeout=7000){const started=Date.now();while(Date.now()-started<timeout){const s=await look();if(s.end)return s;if(/길이를구하시오|바르게고치시오/.test(s.norm)&&s.norm!==previous)return s;await sleep(180);}throw new Error('새 발문 대기 실패');}

await look();await keep('title');await page.mouse.click(W*.5,H*.69);await sleep(900);
let screen=await waitForPrompt();let practice=solve(screen.norm);await keep('practice-before');
if(practice.answer!==6)throw new Error(`연습 화면 6 cm 불일치: ${practice.answer}`);
await dragCm(12,'practice-wrong-must-stay-in-practice');await sleep(550);await look();const practiceWrongShot=await keep('practice-12cm-rejected');
const practiceRejected=await page.evaluate(()=>window.__GAME_TEST__.getState());
if(practiceRejected.onboardingComplete||practiceRejected.lastResult!=='wrong'||practiceRejected.attemptsOnCurrentProblem!==1||practiceRejected.selectedLength!==0)throw new Error(`연습 12 cm가 거부되지 않음: ${JSON.stringify(practiceRejected)}`);
await dragCm(practice.answer,'practice-screen-read');await sleep(2200);

async function play(run,deliberateMistake){const rounds=[],seen='';let invalidFeedbackShot=null;for(let round=1;round<=9;round++){
  const s=await waitForPrompt(seen);if(s.end)throw new Error(`run${run} ${round}번 전에 종료`);const math=solve(s.norm);const before=await keep(`run${run}-round${round}-before`);
  if(run===1&&round===1){await page.mouse.click(105,700);await sleep(260);await look();invalidFeedbackShot=await keep('run1-invalid-input-feedback');}
  if(deliberateMistake&&round===1){const wrong=math.answer===24?23:math.answer+1;await dragCm(wrong,'deliberate-first-mistake-from-ruler-grab-zone',12);await sleep(1550);await look();const wrongShot=await keep(`run${run}-round1-wrong`);await sleep(250);await dragCm(math.answer,'repair-from-same-screen-math');await sleep(1800);rounds.push({round,before,screen_text:s.all,...math,first_submission:wrong,repair_submission:math.answer,wrong_shot:wrongShot});}
  else{await dragCm(math.answer,'screen-solved-first-try');await sleep(1800);rounds.push({round,before,screen_text:s.all,...math,first_submission:math.answer});}
  if(round<9)await sleep(180);
}
await look();const endShot=await keep(`run${run}-end`);const audit=await page.evaluate(()=>window.__GAME_TEST__.getState());return{run,rounds,invalid_feedback_shot:invalidFeedbackShot,end_shot:endShot,audit_after_run_not_used_for_decisions:audit};}

const run1=await play(1,true);await page.mouse.click(W*.5,H*.5);await sleep(1100);const run2=await play(2,false);
const result={slug:'yangmal-daetalchul',run_id:RUN_ID,method:'screen evaluator: macOS Vision OCR reads visible Korean prompt; evaluator computes tangent lengths; all submissions are trusted mouse drags; __GAME_TEST__ never supplies an answer and is read only to assert the 12 cm practice rejection and completed-run outcomes',artifact_hash:beforeHash,artifact_unchanged:beforeHash===hash(),viewport:`${W}x${H}@${DPR}`,practice_rejection:{submitted_cm:12,shot:practiceWrongShot,audit:practiceRejected,passed:!practiceRejected.onboardingComplete&&practiceRejected.lastResult==='wrong'&&practiceRejected.attemptsOnCurrentProblem===1&&practiceRejected.selectedLength===0},voluntary_retry:'result banner was tapped to start a fresh second deck',runs:[run1,run2],input_log:log,console_errors:errors};
result.passed=result.artifact_unchanged&&result.practice_rejection.passed&&run1.audit_after_run_not_used_for_decisions.phase==='clear'&&run1.audit_after_run_not_used_for_decisions.firstCorrect===8&&run1.audit_after_run_not_used_for_decisions.lives===2&&run2.audit_after_run_not_used_for_decisions.phase==='clear'&&run2.audit_after_run_not_used_for_decisions.firstCorrect===9&&errors.length===0;
fs.rmSync(tmp,{force:true});fs.writeFileSync(path.join(HERE,'screen-eval-results.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify({passed:result.passed,hash:beforeHash,run1:run1.audit_after_run_not_used_for_decisions,run2:run2.audit_after_run_not_used_for_decisions,errors},null,2));
await page.close();await browser.close();await server.close();if(!result.passed)process.exitCode=1;
