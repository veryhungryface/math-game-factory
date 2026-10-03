// 첫 판(일반 오답 1회→같은 릴레이 교정→9/9)과 자발적 두 번째 판을
// 실제 pointer down/move/up만으로 검증한다. 정답 훅은 호출하지 않는다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/bitbyeon-cheolgil');
const OUT = path.join(path.dirname(new URL(import.meta.url).pathname), 'second-play');
fs.mkdirSync(OUT, {recursive:true});
const sleep = ms => new Promise(r => setTimeout(r, ms));
const answers = {r01:5,r02:10,r03:15,r04:13,r05:17,r06:25,r07:12,r08:8,r09:24};

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base,b,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
}
function artifactHash() {
  const files=[];
  (function walk(d){ for(const f of fs.readdirSync(d).sort()){const p=path.join(d,f);fs.statSync(p).isDirectory()?walk(p):files.push(p);}})(GAME);
  const h=crypto.createHash('sha256');
  for(const f of files){h.update(path.relative(GAME,f));h.update(fs.readFileSync(f));}
  return h.digest('hex');
}

const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:resolveChrome(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const page=await browser.newPage();
await page.setViewport({width:390,height:844,deviceScaleFactor:1});
const errors=[];
page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
page.on('pageerror',e=>errors.push(String(e)));
await page.goto(`${server.url}/g/bitbyeon-cheolgil/`,{waitUntil:'load',timeout:45000});
await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
const bankPrompts=new Set((await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(360))).map(p=>p.prompt));
let shot=0;
const snap=async tag=>page.screenshot({path:path.join(OUT,`${String(shot++).padStart(2,'0')}-${tag}.png`)});

async function rotateTo(target) {
  for(let pass=0;pass<3;pass++){
    const before=await state();
    const delta=target-before.currentTick;
    if(delta===0)return;
    const dir=Math.sign(delta), cx=195, cy=636, radius=96;
    await page.mouse.move(cx+radius,cy); await page.mouse.down();
    for(let i=1;i<=100;i++){
      // Puppeteer의 y축은 아래가 +, Unity 포인터 y축은 위가 +이므로 부호를 뒤집는다.
      const angle=-dir*i*5*Math.PI/180;
      await page.mouse.move(cx+Math.cos(angle)*radius,cy+Math.sin(angle)*radius,{steps:3});
      await sleep(24);
      if((await state()).currentTick===target)break;
    }
    await page.mouse.up(); await sleep(90);
    if((await state()).currentTick===target)return;
  }
  throw new Error(`pointer crank could not reach ${target}: ${(await state()).currentTick}`);
}
async function lock() {
  const tick=(await state()).currentTick;
  const angle=(tick-4)*Math.PI*2/22;
  // 회전 바퀴 안에서 브레이크 트랙의 위치가 함께 도는 실제 화면 좌표.
  const x=195+43*Math.sin(angle), y=609.3+43*Math.cos(angle);
  await page.mouse.move(x,y); await page.mouse.down();
  await page.mouse.move(x,y+88,{steps:8}); await sleep(90); await page.mouse.up();
}

const log=[];
async function playRun(run,injectWrong){
  let wrongDone=false, wrongProblem='';
  for(let guard=0;guard<24;guard++){
    const s=await state();
    if(s.phase!=='playing')return {end:s,wrongProblem};
    if(!s.currentPrompt||!bankPrompts.has(s.currentPrompt))errors.push(`screen prompt not found in sampleProblems: ${s.problemId} ${s.currentPrompt}`);
    const answer=answers[s.problemId];
    if(!answer)throw new Error(`unknown problem ${s.problemId}`);
    const shouldWrong=injectWrong&&!wrongDone&&!s.onboarding&&s.attemptIndex===0;
    const target=shouldWrong?(answer===25?24:answer+1):answer;
    await rotateTo(target); await lock(); await sleep(180);
    const feedback=await state();
    if(shouldWrong){wrongDone=true;wrongProblem=s.problemId;await snap(`run${run}-wrong-${s.problemId}`);}
    log.push({run,problem:s.problemId,onboarding:s.onboarding,attempt:s.attemptIndex,independentAnswer:answer,pointerTick:target,
      before:{cargo:s.cargo,lives:s.lives,solved:s.solved},feedback:{cargo:feedback.cargo,lives:feedback.lives,solved:feedback.solved,misconceptionId:feedback.misconceptionId}});
    await sleep(1550);
    if(shouldWrong){const retry=await state();if(retry.problemId!==s.problemId||retry.cargo!==s.cargo)throw new Error('wrong answer did not retry same relay');}
  }
  throw new Error('run guard exceeded');
}

await snap('title');
await page.mouse.click(195,422); await sleep(700);
await snap('first-problem');
const run1=await playRun(1,true); await sleep(350); await snap('run1-end-9of9');
await page.mouse.click(195,680); await sleep(700);
const run2=await playRun(2,false); await sleep(350); await snap('run2-end-9of9');

const result={
  run_id:`bitbyeon-second-play-${new Date().toISOString()}`,
  artifact_hash_sha256:artifactHash(),
  method:'real Chrome pointer only; answers independently mapped from the nine visible integer Pythagorean tuples; no answer hooks',
  run1:{phase:run1.end.phase,cargo:run1.end.cargo,solved:run1.end.solved,lives:run1.end.lives,firstAttemptCorrect:run1.end.firstAttemptCorrect,firstAttemptTotal:run1.end.firstAttemptTotal,wrongProblem:run1.wrongProblem},
  run2:{phase:run2.end.phase,cargo:run2.end.cargo,solved:run2.end.solved,lives:run2.end.lives,firstAttemptCorrect:run2.end.firstAttemptCorrect,firstAttemptTotal:run2.end.firstAttemptTotal},
  changed_strategy:'run 1 corrected the same relay after one deliberate off-by-one lock; run 2 used the learned exact ticks with no correction',
  screen_prompt_matches_bank:!errors.some(e=>e.startsWith('screen prompt not found')),
  voluntary_retry:'result-screen pointer tap started run 2',
  log,errors
};
result.passed=result.run1.phase==='clear'&&result.run1.cargo===9&&result.run1.solved===9&&result.run1.firstAttemptCorrect===8&&result.run1.firstAttemptTotal===9&&
  result.run2.phase==='clear'&&result.run2.cargo===9&&result.run2.solved===9&&result.run2.firstAttemptCorrect===9&&result.run2.firstAttemptTotal===9&&errors.length===0;
fs.writeFileSync(path.join(path.dirname(OUT),'second-play-results.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify({...result,log:`${log.length} pointer submissions`},null,2));
await page.close();await browser.close();await server.close();
if(!result.passed)process.exitCode=1;
