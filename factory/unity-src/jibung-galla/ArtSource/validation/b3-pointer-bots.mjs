#!/usr/bin/env node
// B3: 네 정책 각 200판. start()는 판을 열기만 하고 600개 제출은 모두 실제 pointer 경로다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/jibung-galla');
const W = 390, H = 844, GAMES = 200;
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of ['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-linux64/chrome']) {
    const candidate = path.join(base, build, rel); if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing not found');
}
function artifactHash() { const h = crypto.createHash('sha256'); for (const n of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(f => `Build/${f}`)]) { h.update(n); h.update(fs.readFileSync(path.join(GAME, n))); } return h.digest('hex'); }
function xorshift(seed) { let x = seed >>> 0; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; }; }
async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, pred, label, timeout = 3500) { const began = Date.now(); while (Date.now() - began < timeout) { const s = await state(page); if (pred(s)) return s; await sleep(12); } throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`); }
function browserPoint(x, unityY) { return [x, H - unityY]; }
function rotate(vx, vy, deg) { const a = deg * Math.PI / 180, c = Math.cos(a), s = Math.sin(a); return [vx*c-vy*s, vx*s+vy*c]; }
async function submit(page, angle) {
  for(let retry=0;retry<3;retry++){
    const s = await waitFor(page, x => x.phase === 'playing' && x.apexX > 0 && x.bX > 0, 'roof');
    const [dx, dy] = rotate(s.bX-s.apexX, s.bY-s.apexY, angle); const from = browserPoint(s.apexX,s.apexY), to = browserPoint(s.apexX+dx*1.38,s.apexY+dy*1.38);
    await page.mouse.move(...from); await sleep(24); await page.mouse.down(); await sleep(32); await page.mouse.move(...to,{steps:4}); await sleep(32); await page.mouse.up();
    const began=Date.now();
    while(Date.now()-began<900){const after=await state(page);if(after.phase==='reveal'||after.phase==='gameover'||after.phase==='clear')return after;await sleep(14);}
    await page.mouse.move(8,8);await sleep(40);
  }
  throw new Error(`three pointer retries did not create a valid first submission: ${JSON.stringify(await state(page))}`);
}
async function runPolicy(page, name, choose, chance) {
  let firstCorrect=0, firstAttempts=0, completed=0, progress=0; const inputs={}, answers={};
  for (let game=0; game<GAMES; game++) {
    await page.evaluate(() => window.__GAME_TEST__.start()); const before = await waitFor(page,x=>x.phase==='playing'&&x.apexX>0,'start'); await sleep(60);
    const answer=Number(String(before.currentProblemId).match(/-h(\d+)/)[1]); const input=choose(game);
    answers[answer]=(answers[answer]||0)+1; inputs[input]=(inputs[input]||0)+1;
    const after=await submit(page,input); firstCorrect+=after.firstCorrect; firstAttempts+=after.firstAttemptCount; progress+=after.solved; if(after.phase==='clear')completed++;
    if(after.firstAttemptCount!==1) throw new Error(`${name} did not create one valid first submission: ${JSON.stringify(after)}`);
  }
  const rate=firstCorrect/firstAttempts;
  return {bot:name,games:GAMES,firstAttempts,firstCorrect,firstAttemptRate:rate,chance,completionRate:completed/GAMES,progress,inputs,answers,passed:firstAttempts===GAMES&&rate<=chance&&completed===0};
}
async function idle(page) {
  let progress=0,firstAttempts=0,inputCount=0;
  for(let game=0;game<GAMES;game++){await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(18);const s=await state(page);progress+=s.solved;firstAttempts+=s.firstAttemptCount;inputCount+=s.inputCount;}
  return {bot:'idle',games:GAMES,firstAttempts,firstCorrect:0,firstAttemptRate:0,chance:0,completionRate:0,progress,inputCount,passed:firstAttempts===0&&progress===0&&inputCount===0};
}
const server=await serveStatic(PUBLIC); const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),protocolTimeout:900000,args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']}); const consoleErrors=[];
try{
  const page=await browser.newPage();await page.setViewport({width:W,height:H,deviceScaleFactor:1});page.on('console',m=>{if(m.type()==='error')consoleErrors.push(m.text());});page.on('pageerror',e=>consoleErrors.push(String(e)));
  await page.goto(`${server.url}/g/jibung-galla/`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
  const random=xorshift(0x9e3779b9);
  const repeat=await runPolicy(page,'repeat',()=>40,1/40);
  const cycleValues=[26,32,38,44];const cycle=await runPolicy(page,'cycle',i=>cycleValues[i%4],1/40);
  const randomResult=await runPolicy(page,'random',()=>1+Math.floor(random()*49),25/(40*49));
  const idleResult=await idle(page);const results=[repeat,cycle,randomResult,idleResult];
  const out={generatedAt:new Date().toISOString(),artifactHashSha256:artifactHash(),viewport:`${W}x${H}`,method:'600 submitted first attempts use Puppeteer mouse down/move/up; start() only resets the run; no answer hook is called',criterion:'each policy first-attempt rate <= its uninformed chance; idle progress and first attempts are zero',results,consoleErrors,passed:results.every(r=>r.passed)&&consoleErrors.length===0};
  fs.writeFileSync(path.join(HERE,'b3-pointer-results.json'),`${JSON.stringify(out,null,2)}\n`);console.log(JSON.stringify(out,null,2));if(!out.passed)process.exitCode=1;await page.close();
}finally{await browser.close();await server.close();}
