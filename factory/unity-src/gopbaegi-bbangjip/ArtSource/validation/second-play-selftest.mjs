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
const FRAMES=path.join(HERE,'second-play');
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
  const h=crypto.createHash('sha256');
  for(const rel of rels){h.update(rel);h.update(fs.readFileSync(path.join(GAME,rel)));}
  return h.digest('hex');
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
const points={lever:[312,713],minus:[59,713],seal:[148,713],plus:[238,713]};

async function coarseFineSeal(page,target,{label,mode='under',jitterMs=0},log){
  const before=await getState(page);
  const threshold=mode==='under'?Math.max(before.poured+4,target-7):target;
  await page.mouse.move(...points.lever);await page.mouse.down();
  await page.waitForFunction(value=>window.__GAME_TEST__.getState().poured>=value,{timeout:30000},threshold);
  if(mode==='over')await sleep(jitterMs);
  await page.mouse.up();
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase==='fine',{timeout:5000});
  const coarse=await getState(page);let minus=0,plus=0;
  while((await getState(page)).poured>target){await page.mouse.click(...points.minus);minus++;await sleep(33);}
  while((await getState(page)).poured<target){await page.mouse.click(...points.plus);plus++;await sleep(37);}
  const tuned=await getState(page);
  if(tuned.poured!==target||tuned.targetHidden!==1)throw new Error(`${label}: fine=${tuned.poured}, hidden=${tuned.targetHidden}`);
  await page.mouse.click(...points.seal);
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase==='reveal',{timeout:5000});
  const judged=await getState(page);
  log.push({label,problemId:before.problemId,prompt:before.prompt,calculated_target:target,strategy:mode,
    jitterMs,coarse_release:coarse.poured,minus_taps:minus,plus_taps:plus,sealed:judged.poured,
    solved:[before.solved,judged.solved],firstAttemptCorrect:judged.firstAttemptCorrect,lives:judged.lives});
  if(judged.poured!==target)throw new Error(`${label}: sealed ${judged.poured} != ${target}`);
  return judged;
}

const release=await acquireMachineLock('gpu',{label:'gopbaegi-bbangjip 독립 두 번째 플레이'});
const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio','--use-gl=angle','--use-angle=metal']});
let exitCode=0;
try{
  const hashBefore=artifactHash(),errors=[],log=[];
  const page=await browser.newPage();await page.setViewport({width:390,height:844,deviceScaleFactor:1});
  page.on('console',message=>{if(message.type()==='error')errors.push(message.text());});page.on('pageerror',error=>errors.push(String(error)));
  await page.goto(`${server.url}/g/gopbaegi-bbangjip/`,{waitUntil:'load',timeout:45000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});await sleep(500);
  await page.screenshot({path:path.join(FRAMES,'00-title.png')});

  // 같은 실제 레버로 연습을 열고, 화면의 25 cm³·1:2를 독립 계산한다.
  await page.mouse.click(...points.lever);
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().onboarding,{timeout:5000});
  await page.screenshot({path:path.join(FRAMES,'01-hold-coach.png')});
  // 첫 증거의 지연 release와 달리 7 cm³ 일찍 놓고 +1 밸브로 복원한다.
  await coarseFineSeal(page,200,{label:'practice-early-release',mode:'under'},log);
  await sleep(850);
  await page.screenshot({path:path.join(FRAMES,'02-cubic-proof.png')});
  await page.waitForFunction(()=>{const s=window.__GAME_TEST__.getState();return s.actionPhase==='playing'&&!s.onboarding;},{timeout:8000});

  let swapped=false;
  for(let guard=0;guard<12;guard++){
    let state=await getState(page);if(state.phase!=='playing')break;
    // 첫 플레이와 달리 중반 대기 주문을 실제 탭으로 교대해 주문 순서를 바꾼다.
    if(!swapped&&state.pairAvailable&&state.poured===0){
      const beforeId=state.problemId;await page.mouse.click(300,165);await sleep(350);state=await getState(page);
      swapped=state.problemId!==beforeId;log.push({label:'pair-swap',beforeId,afterId:state.problemId,swapped});
      await page.screenshot({path:path.join(FRAMES,'03-pair-swapped.png')});
    }
    const target=targetFromPrompt(state.prompt);
    const mode=state.solved%2===0?'under':'over';
    await coarseFineSeal(page,target,{label:`order-${state.orderIndex}-${mode}`,mode,jitterMs:mode==='over'?130:0},log);
    await page.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase!=='reveal',{timeout:8000});
  }
  const clear=await getState(page);await sleep(900);await page.screenshot({path:path.join(FRAMES,'04-perfect-clear.png')});

  // 결과를 본 뒤 실제 탭으로 자발적 재도전을 선택했는지 별도 상태 전이로 남긴다.
  await page.mouse.click(195,430);
  await page.waitForFunction(()=>{const s=window.__GAME_TEST__.getState();return s.phase==='playing'&&s.solved===0&&s.orderIndex===1;},{timeout:5000});
  const retry=await getState(page);await page.screenshot({path:path.join(FRAMES,'05-voluntary-retry.png')});
  const hashAfter=artifactHash();
  const firstRun=JSON.parse(fs.readFileSync(path.join(HERE,'fix2-human-jitter-results.json'),'utf8'));
  const independentFromFirst=firstRun.passed===true&&firstRun.artifact_hash===hashBefore&&firstRun.run_id!==undefined;
  const passed=hashBefore===hashAfter&&independentFromFirst&&errors.length===0&&swapped&&clear.phase==='clear'&&clear.solved===9&&clear.lives===3&&
    clear.firstAttemptCorrect===9&&clear.finalBandFirstCorrect===3&&retry.phase==='playing'&&retry.solved===0;
  const result={run_id:`${path.basename(process.env.MGF_WORK||'20261017-000511-L3')}-fix3-second-play-${new Date().toISOString()}`,
    artifact_hash:hashBefore,generated_at:new Date().toISOString(),viewport:'390x844',
    method:'현재 WebGL의 실제 mouse down/up과 ±1/봉인 클릭만 사용. 첫 판과 달리 조기 release 복원·교대 주문·번갈아 쓰는 조기/지연 계량으로 독립 완주했으며 answerCorrect/answerWrong은 사용하지 않았다.',
    first_run:{run_id:firstRun.run_id,artifact_hash:firstRun.artifact_hash,final:firstRun.final},
    independent_from_first:independentFromFirst,swapped_order:swapped,log,clear,retry,consoleErrors:errors,passed};
  fs.writeFileSync(path.join(HERE,'second-play-results.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result,null,2));
  if(!passed)exitCode=1;
}catch(error){console.error(error.stack||error);exitCode=1;}
finally{await browser.close();await server.close();await release();}
process.exit(exitCode);
