import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname);
const PUBLIC=path.join(ROOT,'public');
const GAME=path.join(PUBLIC,'g/semo-sonagi');
const TRIALS=200;
const CHANCE=1/6; // 네 대상에서 정확히 두 개를 고르는 집합은 4C2=6가지.
const PAIRS=[[0,1],[0,2],[0,3],[1,2],[1,3],[2,3]];
const CENTERS=[[105,346],[285,346],[105,557],[285,557]]; // 브라우저 좌상단 원점, 390×844.
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));

function resolveChrome(){
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  if(!fs.existsSync(base))return undefined;
  for(const build of fs.readdirSync(base).sort().reverse())for(const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-linux64/chrome']){
    const candidate=path.join(base,build,rel);if(fs.existsSync(candidate))return candidate;
  }
}

function randomSource(seed){let x=seed>>>0;return()=>{x^=x<<13;x^=x>>>17;x^=x<<5;return(x>>>0)/4294967296;};}

function artifactHash(){
  const files=[];(function walk(dir){for(const name of fs.readdirSync(dir).sort()){const file=path.join(dir,name);fs.statSync(file).isDirectory()?walk(file):files.push(file);}})(GAME);
  const hash=crypto.createHash('sha256');for(const file of files){hash.update(path.relative(GAME,file));hash.update(fs.readFileSync(file));}return hash.digest('hex');
}

async function swipePair(page,pairIndex){
  const [ai,bi]=PAIRS[pairIndex],a=CENTERS[ai],b=CENTERS[bi];
  await page.mouse.move(a[0],a[1]);await page.mouse.down();await sleep(22);
  await page.mouse.move(b[0],b[1],{steps:12});await sleep(22);await page.mouse.up();
}

const server=await serveStatic(PUBLIC);
const launch=()=>puppeteer.launch({headless:true,executablePath:resolveChrome(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});

async function openPage(browser){
  const page=await browser.newPage(),errors=[];await page.setViewport({width:390,height:844,deviceScaleFactor:1});
  page.on('console',message=>{if(message.type()==='error')errors.push(message.text());});page.on('pageerror',error=>errors.push(String(error)));
  await page.goto(`${server.url}/g/semo-sonagi/?bot=${Date.now()}`,{waitUntil:'load',timeout:45000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});return {page,errors};
}

async function run(kind){
  const browser=await launch();
  try{
    const {page,errors}=await openPage(browser);const random=randomSource(2);let correct=0,completed=0,total=0;
    for(let trial=0;trial<TRIALS;trial++){
      await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(32);
      // cycle은 공간 대상 1→2→3→4→1을 이어, 연속 인접쌍 네 종류를 반복한다.
      const cyclePairs=[0,3,5,2];
      const pairIndex=kind==='fixed'?0:kind==='cycle'?cyclePairs[trial%cyclePairs.length]:Math.floor(random()*6);
      await swipePair(page,pairIndex);
      await page.waitForFunction(()=>window.__GAME_TEST__.getState().firstAttemptTotal===1,{timeout:3000});
      const s=await page.evaluate(()=>{const x=window.__GAME_TEST__.getState();return {firstAttemptTotal:x.firstAttemptTotal,firstAttemptCorrect:x.firstAttemptCorrect,phase:x.phase};});
      total+=s.firstAttemptTotal;correct+=s.firstAttemptCorrect;if(s.phase==='clear')completed++;
    }
    await page.close();return {games:TRIALS,firstAttemptCorrect:correct,firstAttemptTotal:total,firstAttemptRate:correct/total,chance:CHANCE,completionRate:completed/TRIALS,consoleErrors:errors,passed:total===TRIALS&&correct/total<=CHANCE&&errors.length===0};
  }finally{await browser.close();}
}

const fixed=await run('fixed');console.log('fixed',JSON.stringify(fixed));
const cycle=await run('cycle');console.log('cycle',JSON.stringify(cycle));
const random=await run('random');console.log('random',JSON.stringify(random));
const idleBrowser=await launch();let idleProgressed=0,idleErrors=[];
try{
  const opened=await openPage(idleBrowser),idlePage=opened.page;idleErrors=opened.errors;
  for(let trial=0;trial<TRIALS;trial++){
    const changed=await idlePage.evaluate(async()=>{window.__GAME_TEST__.start();const a=window.__GAME_TEST__.getState();await new Promise(r=>setTimeout(r,34));const b=window.__GAME_TEST__.getState();return b.solved!==a.solved||b.firstAttemptTotal!==a.firstAttemptTotal;});
    if(changed)idleProgressed++;
  }
  await idlePage.close();
}finally{await idleBrowser.close();}
const idle={games:TRIALS,firstAttemptCorrect:0,firstAttemptTotal:0,firstAttemptRate:0,chance:CHANCE,completionRate:0,progressed:idleProgressed,consoleErrors:idleErrors,passed:idleProgressed===0&&idleErrors.length===0};
const result={runId:`semo-sonagi-pointer-bots-${new Date().toISOString()}`,artifactHashSha256:artifactHash(),criterion:'각 200판의 원시 첫 시도 정답률이 1/6 이하, 무입력 진도 0',method:'390×844 Chrome의 실제 canvas mouse down/move/up. fixed=ㄱ→ㄴ, cycle=1→2→3→4→1 인접쌍, random=xorshift seed 2의 여섯 위치쌍. 정답·correctMask·answerCorrect/answerWrong을 읽거나 호출하지 않음.',fixed,cycle,random,idle,passed:fixed.passed&&cycle.passed&&random.passed&&idle.passed};
console.log(JSON.stringify(result,null,2));fs.writeFileSync(new URL('./bot-results.json',import.meta.url),JSON.stringify(result,null,2));
await server.close();if(!result.passed)process.exitCode=1;
