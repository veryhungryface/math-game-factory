#!/usr/bin/env node
// 화면 OCR로 자료만 읽고 직접 표준편차를 계산한 뒤 실제 Chrome pointer로 3판을 검증한다.
// 의사결정에 activeProblemId·sampleProblems·answerCorrect/Wrong·start 훅을 쓰지 않는다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const GAME=path.join(ROOT,'public/g/poksinhan-kkokdaegi');
const OUT=path.join(process.env.TMPDIR||'/tmp','poksin-screen-eval');
const EYE=process.env.SCREEN_OCR||'/tmp/poksin-fix/screen-ocr';
const W=390,H=844,DPR=3;
const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
fs.rmSync(OUT,{recursive:true,force:true});fs.mkdirSync(OUT,{recursive:true});

function chromePath(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const b of fs.readdirSync(base).sort().reverse()){const p=path.join(base,b,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');if(fs.existsSync(p))return p;}}
function artifactHash(){const rels=['index.html',...fs.readdirSync(path.join(GAME,'Build')).sort().map(n=>`Build/${n}`)];const h=crypto.createHash('sha256');for(const rel of rels){h.update(rel);h.update(fs.readFileSync(path.join(GAME,rel)));}return h.digest('hex');}
const eye=(...a)=>JSON.parse(execFileSync(EYE,a,{encoding:'utf8'}));

const hashBefore=artifactHash();
const server=await serveStatic(path.join(ROOT,'public'));
const browser=await puppeteer.launch({headless:'new',executablePath:chromePath(),protocolTimeout:120000,args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const page=await browser.newPage();await page.setViewport({width:W,height:H,deviceScaleFactor:DPR});
const consoleErrors=[],pageErrors=[],inputs=[],calculations=[];
page.on('console',m=>{if(m.type()==='error')consoleErrors.push(m.text());});page.on('pageerror',e=>pageErrors.push(String(e)));
await page.goto(`${server.url}/g/poksinhan-kkokdaegi/`,{waitUntil:'load',timeout:45000});
await page.waitForFunction(()=>window.__GAME_TEST__?.ready===true,{timeout:60000});await sleep(1000);

async function publicState(){return page.evaluate(()=>{const s=window.__GAME_TEST__.getState();return {phase:s.phase,round:s.round,solved:s.solved,lives:s.lives,firstCorrect:s.firstCorrect,firstAttempts:s.firstAttempts,remainingFirstMistakes:s.remainingFirstMistakes,onboarding:s.onboarding,repairMode:s.repairMode,feedbackKind:s.feedbackKind};});}
async function shot(name){const file=path.join(OUT,name);await page.screenshot({path:file});return file;}
function readVisibleRaw(file){
  const words=eye('ocr',file);
  const candidates=words.map(w=>w.text.replace(/[Oo]/g,'0')).filter(t=>t.includes('기록')&&(t.split('기록').pop().match(/\d+/g)||[]).length>=4);
  if(!candidates.length)throw new Error(`화면 자료 OCR 실패: ${words.map(w=>w.text).join(' | ')}`);
  const raw=(candidates[0].split('기록').pop().split('평균')[0].match(/\d+/g)||[]).map(Number);
  const n=raw.length,sum=raw.reduce((a,b)=>a+b,0);
  if((n!==4&&n!==6)||sum%n!==0)throw new Error(`자료 해석 실패: ${candidates[0]}`);
  const mean=sum/n,ss=raw.reduce((a,x)=>a+(x-mean)*(x-mean),0);
  if(ss%n!==0)throw new Error(`분산 정수 불변식 실패: ${raw}`);
  const variance=ss/n,side=Math.sqrt(variance);
  if(!Number.isInteger(side)||side<1||side>8)throw new Error(`표준편차 눈금 불변식 실패: ${raw} -> ${side}`);
  return {ocr:candidates[0],raw,mean,sumSquares:ss,variance,side};
}
// 눈금 위치는 화면 좌표(모서리 기하)만 쓴다. 정답·문항 id 는 읽지 않는다.
async function corner(k){return page.evaluate((k)=>{const s=window.__GAME_TEST__.getState();return [s.cornerXPermille[k]/1000*innerWidth,s.cornerYPermille[k]/1000*innerHeight];},k);}
async function dragSide(side,label){
  const a=await corner(0),b=await corner(side);
  await page.mouse.move(a[0],a[1]);await page.mouse.down();await page.mouse.move(b[0],b[1],{steps:14});await page.mouse.up();
  inputs.push({label,side,path_css:[a.map(v=>+v.toFixed(1)),b.map(v=>+v.toFixed(1))],kind:'real Chrome pointer drag'});
}
async function enterRun(label){
  await page.mouse.click(W*.50,H*.72);await sleep(1100);
  const s0=await publicState();if(!s0.onboarding)return;
  await shot(`${label}-practice-guide.png`);
  await dragSide(3,`${label}-practice`);await sleep(2700);
  const s=await publicState();if(s.onboarding||s.round!==1)throw new Error(`연습 전이 실패 ${JSON.stringify(s)}`);
}
async function playRun(label,recovery){
  await enterRun(label);let madeMistake=false,serial=0;
  while(true){
    const before=await publicState();if(before.phase==='clear'||before.phase==='gameover')break;
    const file=await shot(`${label}-${String(serial).padStart(2,'0')}-round${before.round}-before.png`);
    const calc=readVisibleRaw(file);let selected=calc.side;
    if(recovery&&!madeMistake&&before.round===1&&!before.repairMode){selected=calc.side===8?7:calc.side+1;madeMistake=true;}
    await dragSide(selected,`${label}-round${before.round}${before.repairMode?'-repair':''}`);await sleep(600);
    await shot(`${label}-${String(serial).padStart(2,'0')}-round${before.round}-reveal.png`);await sleep(2100);
    const after=await publicState();calculations.push({run:label,round:before.round,selected,deliberateMistake:selected!==calc.side,rationale:`화면 자료 ${calc.raw.join(', ')}의 평균 ${calc.mean}, 편차 제곱합 ${calc.sumSquares}, 분산 ${calc.variance}, 표준편차 ${calc.side}`,before,after});
    serial++;if(serial>10)throw new Error(`${label} 반복 상한 초과`);
  }
  const end=await publicState();await sleep(800);await shot(`${label}-end-${end.phase}.png`);
  if(end.phase!=='clear'||end.solved!==6)throw new Error(`${label} 완주 실패 ${JSON.stringify(end)}`);
  if(recovery&&(end.firstCorrect!==5||end.lives!==2))throw new Error(`${label} 회복 장부 실패 ${JSON.stringify(end)}`);
  if(!recovery&&(end.firstCorrect!==6||end.lives!==3))throw new Error(`${label} 정상 장부 실패 ${JSON.stringify(end)}`);
  return end;
}

await shot('00-title.png');
const normal=await playRun('run1-normal',false);
const recovery=await playRun('run2-recovery',true);
const second=await playRun('run3-independent',false);
const hashAfter=artifactHash();
const result={run_id:`poksin-screen-eval-${new Date().toISOString()}`,artifact_hash:hashBefore,artifact_unchanged:hashBefore===hashAfter,
  method:'macOS Vision OCR로 화면의 기록 자료를 읽어 평균·편차 제곱합·분산·표준편차를 독립 계산하고 실제 Chrome pointer drag만 사용',
  forbidden_calls:{activeProblemId:0,sampleProblems:0,answerCorrect:0,answerWrong:0,start:0},normal,recovery,second,calculations,inputs,consoleErrors,pageErrors,
  passed:hashBefore===hashAfter&&normal.phase==='clear'&&recovery.phase==='clear'&&second.phase==='clear'&&consoleErrors.length===0&&pageErrors.length===0};
fs.writeFileSync(path.join(HERE,'screen-eval-results.json'),JSON.stringify(result,null,2)+'\n');
fs.writeFileSync(path.join(HERE,'pointer-playthrough-results.json'),JSON.stringify(result,null,2)+'\n');
fs.rmSync(OUT,{recursive:true,force:true});
console.log(JSON.stringify({run_id:result.run_id,artifact_hash:result.artifact_hash,normal,recovery,second,calculations:calculations.length,inputs:inputs.length,errors:[...consoleErrors,...pageErrors],passed:result.passed},null,2));
await page.close();await browser.close();await server.close();if(!result.passed)process.exitCode=1;
