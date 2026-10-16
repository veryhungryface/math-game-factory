#!/usr/bin/env node
import fs from 'node:fs';
import crypto from 'node:crypto';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';
import {acquireMachineLock} from '../../../../lib/machine-lock.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const PUBLIC=path.join(ROOT,'public');
const GAME=path.join(PUBLIC,'g','gopbaegi-bbangjip');
const FRAMES=path.join(HERE,'fix2-human-jitter');
fs.mkdirSync(FRAMES,{recursive:true});
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));

function chromePath(){
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  if(!fs.existsSync(base))return undefined;
  for(const build of fs.readdirSync(base).sort().reverse())for(const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing']){
      const p=path.join(base,build,rel);if(fs.existsSync(p))return p;
    }
}

function artifactHash(){
  const rels=['index.html',...fs.readdirSync(path.join(GAME,'Build')).sort().map(name=>`Build/${name}`)];
  const hash=crypto.createHash('sha256');
  for(const rel of rels){hash.update(rel);hash.update(fs.readFileSync(path.join(GAME,rel)));}
  return hash.digest('hex');
}

function targetFromPrompt(prompt){
  const match=prompt.match(/작은 틀의 부피 (\d+) cm³, 닮음비 (\d+):(\d+)/);
  if(!match)throw new Error(`발문 파싱 실패: ${prompt}`);
  const v0=Number(match[1]),m=Number(match[2]),n=Number(match[3]);
  const numerator=v0*n*n*n,denominator=m*m*m;
  if(numerator%denominator)throw new Error(`정수 부피가 아님: ${prompt}`);
  return numerator/denominator;
}

const getState=page=>page.evaluate(()=>window.__GAME_TEST__.getState());
const mobilePoints={lever:[312,713],minus:[59,713],seal:[148,713],plus:[238,713]};

async function coarseFineSeal(page,wanted,{label,mode='over',jitterMs=160}={},log=[]){
  const before=await getState(page);
  const threshold=mode==='under'?Math.max(before.poured+4,wanted-9):wanted;
  await page.mouse.move(...mobilePoints.lever);await page.mouse.down();
  await page.waitForFunction(value=>window.__GAME_TEST__.getState().poured>=value,{timeout:30000},threshold);
  if(mode==='over')await sleep(jitterMs);
  await page.mouse.up();
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase==='fine',{timeout:5000});
  const released=await getState(page);
  let minus=0,plus=0;
  while((await getState(page)).poured>wanted){await page.mouse.click(...mobilePoints.minus);minus++;await sleep(28+(minus%3)*11);}
  while((await getState(page)).poured<wanted){await page.mouse.click(...mobilePoints.plus);plus++;await sleep(31+(plus%4)*9);}
  const tuned=await getState(page);
  if(tuned.poured!==wanted||tuned.targetHidden!==1)throw new Error(`${label}: fine=${tuned.poured}, hidden=${tuned.targetHidden}`);
  await page.mouse.click(...mobilePoints.seal);
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase==='reveal',{timeout:5000});
  const judged=await getState(page);
  log.push({label,problemId:before.problemId,wanted,mode,jitterMs,coarseRelease:released.poured,
    minusTaps:minus,plusTaps:plus,sealed:judged.poured,lives:[before.lives,judged.lives],
    solved:[before.solved,judged.solved],misconceptionId:judged.misconceptionId});
  return judged;
}

async function openPage(browser,server,width,height,errors){
  const page=await browser.newPage();await page.setViewport({width,height,deviceScaleFactor:1});
  page.on('console',message=>{if(message.type()==='error')errors.push(`${width}x${height}: ${message.text()}`);});
  page.on('pageerror',error=>errors.push(`${width}x${height}: ${String(error)}`));
  await page.goto(`${server.url}/g/gopbaegi-bbangjip/`,{waitUntil:'load',timeout:45000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});await sleep(400);
  return page;
}

const release=await acquireMachineLock('gpu',{label:'gopbaegi-bbangjip fix2 인간형 지터·와이드 검증'});
const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:[
  '--no-sandbox','--disable-dev-shm-usage','--mute-audio','--use-gl=angle','--use-angle=metal']});
let exitCode=0;
try{
  const hash=artifactHash(),errors=[],log=[];
  const page=await openPage(browser,server,390,844,errors);
  await page.screenshot({path:path.join(FRAMES,'00-title-mobile.png')});
  await page.mouse.click(...mobilePoints.lever);
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().onboarding,{timeout:5000});

  await coarseFineSeal(page,200,{label:'practice-jitter-160ms',mode:'over',jitterMs:160},log);
  await page.screenshot({path:path.join(FRAMES,'01-practice-proof.png')});
  await page.waitForFunction(()=>{const s=window.__GAME_TEST__.getState();return s.actionPhase==='playing'&&!s.onboarding;},{timeout:8000});

  // 계산 자체가 틀린 선형 오개념값 50은 정밀 조정 뒤 봉인해도 반드시 오답이어야 한다.
  const wrong=await coarseFineSeal(page,50,{label:'order1-linear-wrong',mode:'under',jitterMs:0},log);
  if(wrong.misconceptionId!=='linear_ratio'||wrong.lives!==2||wrong.solved!==0)throw new Error('잘못 계산한 50 cm³가 오답으로 유지되지 않음');
  await page.screenshot({path:path.join(FRAMES,'02-wrong-calculation-rejected.png')});
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase==='playing',{timeout:8000});

  const jitters=[220,100,250,140,190,110,230,170,210];
  for(let guard=0;guard<12;guard++){
    const state=await getState(page);if(state.phase!=='playing')break;
    const wanted=targetFromPrompt(state.prompt),index=Math.min(jitters.length-1,state.solved);
    const mode=state.solved%2===0?'over':'under';
    await coarseFineSeal(page,wanted,{label:`order-${state.orderIndex}-${mode}`,mode,jitterMs:jitters[index]},log);
    await page.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase!=='reveal',{timeout:8000});
  }
  const final=await getState(page);await page.screenshot({path:path.join(FRAMES,'03-recovery-clear.png')});

  // 1280×800과 2000×1045에서 타이틀 레이블과 본판 정밀 조정 컨트롤을 실제로 캡처한다.
  const wideCaptures=[];
  for(const [width,height,name] of [[1280,800,'1280'],[2000,1045,'2000']]){
    const p=await openPage(browser,server,width,height,errors);
    await p.screenshot({path:path.join(FRAMES,`04-title-${name}.png`)});
    await p.mouse.click(Math.round(width*.84),Math.round(height*(1-.22)));
    await p.waitForFunction(()=>window.__GAME_TEST__.getState().onboarding,{timeout:5000});
    const lever=[Math.round(width*.84),Math.round(height*(1-.22))];
    await p.mouse.move(...lever);await p.mouse.down();
    await p.waitForFunction(()=>window.__GAME_TEST__.getState().poured>=20,{timeout:5000});await p.mouse.up();
    await p.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase==='fine',{timeout:5000});await sleep(250);
    const file=`05-fine-${name}.png`;await p.screenshot({path:path.join(FRAMES,file)});
    wideCaptures.push({viewport:`${width}x${height}`,file,state:await getState(p)});await p.close();
  }

  const passed=errors.length===0&&final.phase==='clear'&&final.solved===9&&final.lives===2&&
    final.firstAttemptCorrect===8&&final.finalBandFirstCorrect===3&&log.some(x=>x.jitterMs===250)&&
    log.every(x=>x.sealed===x.wanted)&&wideCaptures.every(x=>x.state.actionPhase==='fine');
  const result={run_id:`${path.basename(process.env.MGF_WORK||'20261017-000511-L3')}-fix2-human-jitter-${new Date().toISOString()}`,
    artifact_hash:hash,generated_at:new Date().toISOString(),method:'최종 WebGL의 실제 mouse down/up과 실제 ±1/봉인 클릭만 사용. 100~250ms release 지터, 조기 release, 계산 오답, 9주문 회복 완주, 1280/2000 캡처를 포함하며 테스트 정답 훅은 사용하지 않았다.',
    log,final,wideCaptures,consoleErrors:errors,passed};
  fs.writeFileSync(path.join(HERE,'fix2-human-jitter-results.json'),JSON.stringify(result,null,2)+'\n');
  console.log(JSON.stringify(result,null,2));if(!passed)exitCode=1;
}catch(error){console.error(error.stack||error);exitCode=1;}
finally{await browser.close();await server.close();await release();}
process.exit(exitCode);
