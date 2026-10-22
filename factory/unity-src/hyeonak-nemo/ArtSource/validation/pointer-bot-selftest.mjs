import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const TRIALS = Number(process.env.BOT_TRIALS || 200);
const CHANCE = 1/100; // 실제 첫 밴드: 두 반직선의 길이 10×10, 정답 1개.
const RAW_REVIEW_CEILING = 0.025;
const sleep = ms => new Promise(r=>setTimeout(r,ms));
function binomialTail(k,n,p){let q=Math.pow(1-p,n),tail=k===0?q:0;for(let i=1;i<=n;i++){q*=((n-i+1)/i)*(p/(1-p));if(i>=k)tail+=q;}return Math.min(1,tail);}

function chromePath(){
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  if(!fs.existsSync(base)) return undefined;
  for(const b of fs.readdirSync(base).sort().reverse()){
    const p=path.join(base,b,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if(fs.existsSync(p)) return p;
  }
}
function rnd(seed){let x=seed>>>0;return()=>{x^=x<<13;x^=x>>>17;x^=x<<5;return(x>>>0)/4294967296;};}
function currentPos(s,which){return which==='B'?{x:s.ringBScreenX,y:s.ringBScreenY}:{x:s.ringDScreenX,y:s.ringDScreenY};}
function radialTarget(s,which,wanted){
  const r=which==='B'?s.ringBRadius:s.ringDRadius,p=currentPos(s,which);
  return{x:s.boardCenterScreenX+(p.x-s.boardCenterScreenX)*wanted/r,y:s.boardCenterScreenY+(p.y-s.boardCenterScreenY)*wanted/r};
}
async function drag(page,from,to){await page.mouse.move(from.x,from.y);await page.mouse.down();await sleep(16);await page.mouse.move(to.x,to.y,{steps:5});await sleep(16);await page.mouse.up();await sleep(24);}

function artifactHash(){
  const dir=path.join(PUBLIC,'g/hyeonak-nemo'),files=[];
  (function walk(d){for(const f of fs.readdirSync(d).sort()){const p=path.join(d,f);fs.statSync(p).isDirectory()?walk(p):files.push(p);}})(dir);
  const h=crypto.createHash('sha256');for(const f of files){h.update(path.relative(dir,f));h.update(fs.readFileSync(f));}return h.digest('hex');
}

const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
async function open(){
  const page=await browser.newPage(),errors=[];await page.setViewport({width:390,height:844,deviceScaleFactor:1});
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(String(e)));
  await page.goto(`${server.url}/g/hyeonak-nemo/`,{waitUntil:'load',timeout:45000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});return{page,errors};
}
async function run(kind){
  const {page,errors}=await open();const random=rnd(kind==='random'?0x51a7c0de:0x1963);let correct=0,submitted=0,complete=0;
  for(let i=0;i<TRIALS;i++){
    const before=await page.evaluate(()=>{window.__GAME_TEST__.start();return window.__GAME_TEST__.getState();});
    let rb,rd;
    if(kind==='fixed'){rb=1;rd=1;}
    else if(kind==='cycle'){const n=i%100;rb=Math.floor(n/10)+1;rd=n%10+1;}
    else {rb=Math.floor(random()*10)+1;rd=Math.floor(random()*10)+1;}
    await drag(page,currentPos(before,'B'),radialTarget(before,'B',rb));
    let mid=await page.evaluate(()=>window.__GAME_TEST__.getState());
    await drag(page,currentPos(mid,'D'),radialTarget(mid,'D',rd));
    let after=await page.evaluate(()=>window.__GAME_TEST__.getState());
    for(let retry=0;after.firstAttempts!==1&&retry<2;retry++){
      const again=after;
      await drag(page,currentPos(again,'B'),radialTarget(again,'B',rb));
      const againMid=await page.evaluate(()=>window.__GAME_TEST__.getState());
      await drag(page,currentPos(againMid,'D'),radialTarget(againMid,'D',rd));
      after=await page.evaluate(()=>window.__GAME_TEST__.getState());
    }
    if(process.env.BOT_DEBUG && i===0) console.error(kind,{before,rb,rd,mid,after});
    if(after.firstAttempts===1){submitted++;if(after.firstCorrect===1)correct++;}
    if(after.phase==='clear')complete++;
  }
  await page.close();const rate=submitted?correct/submitted:0,pValueAboveChance=binomialTail(correct,submitted,CHANCE);
  return{trials:TRIALS,submitted,correct,firstTryRate:rate,chance:CHANCE,rawReviewCeiling:RAW_REVIEW_CEILING,pValueAboveChance,completionRate:complete/TRIALS,consoleErrors:errors,
    passed:submitted===TRIALS&&rate<=RAW_REVIEW_CEILING&&pValueAboveChance>=.05&&errors.length===0};
}
const fixed=await run('fixed'),cycle=await run('cycle'),random=await run('random');
const {page:idlePage,errors:idleErrors}=await open();
const idleBefore=await idlePage.evaluate(()=>{window.__GAME_TEST__.start();return window.__GAME_TEST__.getState();});
await sleep(900);const idleAfter=await idlePage.evaluate(()=>window.__GAME_TEST__.getState());await idlePage.close();
const idle={trials:TRIALS,submitted:0,correct:0,firstTryRate:0,chance:0,completionRate:0,progressBefore:idleBefore.solved,progressAfter:idleAfter.solved,consoleErrors:idleErrors,passed:idleBefore.solved===idleAfter.solved&&idleErrors.length===0};
const result={run_id:`hyeonak-nemo-pointer-bots-${new Date().toISOString()}`,artifact_hash_sha256:artifactHash(),method:'Chrome CDP real mouse down/move/up on both Unity vertex rings; no problem prompt/id/answer hook/sampleProblems read',criterion:'each 200-run policy raw rate <= 2.5% review ceiling AND not significantly above exact 1/100 chance (one-sided binomial p >= .05); idle progress 0',fixed,cycle,random,idle,passed:fixed.passed&&cycle.passed&&random.passed&&idle.passed};
console.log(JSON.stringify(result,null,2));
fs.writeFileSync(path.join(path.dirname(new URL(import.meta.url).pathname),'bot-results.json'),JSON.stringify(result,null,2));
await browser.close();await server.close();if(!result.passed)process.exitCode=1;
