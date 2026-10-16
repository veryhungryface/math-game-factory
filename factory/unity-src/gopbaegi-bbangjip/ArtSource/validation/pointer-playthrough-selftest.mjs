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
const FRAMES=path.join(HERE,'pointer-playthrough');
fs.mkdirSync(FRAMES,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));

function artifactHash(){
  const rels=['index.html',...fs.readdirSync(path.join(GAME,'Build')).sort().map(name=>`Build/${name}`)];
  const h=crypto.createHash('sha256');
  for(const rel of rels){h.update(rel);h.update(fs.readFileSync(path.join(GAME,rel)));}
  return h.digest('hex');
}

function chromePath(){
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  if(!fs.existsSync(base))return undefined;
  for(const build of fs.readdirSync(base).sort().reverse())for(const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing']){
      const p=path.join(base,build,rel);if(fs.existsSync(p))return p;
    }
}

function targetFromPrompt(prompt){
  const m=prompt.match(/작은 틀의 부피 (\d+) cm³, 닮음비 (\d+):(\d+)/);
  if(!m)throw new Error(`발문 파싱 실패: ${prompt}`);
  const v0=Number(m[1]),a=Number(m[2]),b=Number(m[3]);
  const num=v0*b*b*b,den=a*a*a;
  if(num%den)throw new Error(`정수 부피가 아님: ${prompt}`);
  return num/den;
}

const getState=p=>p.evaluate(()=>window.__GAME_TEST__.getState());
async function holdUntil(page,absolute,label,log){
  const before=await getState(page);const at=[Math.round(390*.72),Math.round(844*.845)];
  await page.mouse.move(...at);await page.mouse.down();
  await page.waitForFunction(v=>window.__GAME_TEST__.getState().poured>=v,{timeout:30000},absolute);
  await page.mouse.up();
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase==='reveal',{timeout:5000});
  const judged=await getState(page);log.push({label,at,beforePoured:before.poured,release:judged.poured,
    lives:[before.lives,judged.lives],solved:[before.solved,judged.solved],misconceptionId:judged.misconceptionId});
  return judged;
}

const release=await acquireMachineLock('gpu',{label:'gopbaegi-bbangjip 실제 포인터 완주'});
const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),
  args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio','--use-gl=angle','--use-angle=metal']});
let exitCode=0;
try{
  const page=await browser.newPage();await page.setViewport({width:390,height:844,deviceScaleFactor:1});
  const errors=[];page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(String(e)));
  await page.goto(`${server.url}/g/gopbaegi-bbangjip/`,{waitUntil:'load',timeout:45000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});await sleep(500);
  await page.screenshot({path:path.join(FRAMES,'00-title.png')});
  const log=[];

  // 타이틀의 실제 압력 레버를 눌러 고정 연습으로 진입한다.
  await page.mouse.move(Math.round(390*.72),Math.round(844*.845));await page.mouse.down();await sleep(110);await page.mouse.up();
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().onboarding,{timeout:4000});
  await page.screenshot({path:path.join(FRAMES,'01-practice.png')});
  await holdUntil(page,200,'practice-correct',log);
  await sleep(850);
  await page.screenshot({path:path.join(FRAMES,'02-practice-proof.png')});
  await page.waitForFunction(()=>{const s=window.__GAME_TEST__.getState();return s.actionPhase==='playing'&&!s.onboarding;},{timeout:8000});

  // 첫 일반 주문에서 선형 오개념값 50 cm³로 놓고, 같은 컵에 실제로 덧부어 회복한다.
  await holdUntil(page,50,'order1-linear-wrong',log);
  await page.screenshot({path:path.join(FRAMES,'03-linear-empty-cells.png')});
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase==='playing',{timeout:8000});
  const same=await getState(page);if(same.poured<45||same.problemId==='')throw new Error('모자람 덧붓기 상태가 유지되지 않음');
  await holdUntil(page,targetFromPrompt(same.prompt),'order1-recovery',log);
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase==='playing',{timeout:8000});

  // 남은 8주문도 정답 숫자를 화면 발문에서 독립 계산하고 실제 hold/release로 처리한다.
  for(let guard=0;guard<12;guard++){
    const s=await getState(page);if(s.phase!=='playing')break;
    await holdUntil(page,targetFromPrompt(s.prompt),`order${s.solved+1}-correct`,log);
    await page.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase!=='reveal',{timeout:8000});
  }
  const final=await getState(page);await page.screenshot({path:path.join(FRAMES,'04-recovery-clear.png')});
  const result={run_id:`${path.basename(process.env.MGF_WORK||'20261017-000511-L3')}-fix1-recovery-${new Date().toISOString()}`,
    artifact_hash:artifactHash(),generated_at:new Date().toISOString(),
    method:'최종 WebGL Chrome 실제 mouse down/up. 화면 발문을 독립 계산했고 훅 정답/오답 함수는 사용하지 않았다.',log,final,consoleErrors:errors,
    passed:errors.length===0&&final.phase==='clear'&&final.solved===9&&final.lives===2&&final.firstAttemptCorrect===8&&final.finalBandFirstCorrect===3};
  fs.writeFileSync(path.join(HERE,'pointer-playthrough-results.json'),JSON.stringify(result,null,2)+'\n');
  console.log(JSON.stringify(result,null,2));if(!result.passed)exitCode=1;
}catch(error){console.error(error.stack||error);exitCode=1;}
finally{await browser.close();await server.close();await release();}
process.exit(exitCode);
