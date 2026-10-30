import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/dalpaengi-parade');
const VIEW = { width: 390, height: 844 };
const TRIALS = Number(process.env.B3_TRIALS || 200);
const CHANCE = 1 / 6; // 실제 노출 첫 밴드: 서로 다른 세 무리의 전체 순열.
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
}

function xorshift(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; };
}

function artifactHash() {
  const hash = crypto.createHash('sha256');
  const rels = ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(name => `Build/${name}`)];
  for (const rel of rels) { hash.update(rel); hash.update(fs.readFileSync(path.join(GAME, rel))); }
  return hash.digest('hex');
}

function wilson(correct, total) {
  if (!total) return [0, 0];
  const z = 1.959963984540054, p = correct / total, d = 1 + z * z / total;
  const mid = (p + z*z/(2*total)) / d;
  const half = z * Math.sqrt((p*(1-p)+z*z/(4*total))/total) / d;
  return [Math.max(0, mid-half), Math.min(1, mid+half)];
}

// 좌표는 답과 무관한 고정 레이아웃 위치다. 카드 아래 실제 달팽이 무리 중심에서
// 세 출발 칸 중심으로 drag한다. 문제 id/prompt/answer는 읽지 않는다.
const homes = [{x:65,y:535},{x:195,y:535},{x:325,y:535}];
const slots = [{x:195,y:335},{x:195,y:385},{x:195,y:435}];

async function drag(page, from, to) {
  await page.mouse.move(from.x, from.y);
  await page.mouse.down();
  await sleep(18);
  await page.mouse.move(to.x, to.y, {steps: 4});
  await sleep(26);
  await page.mouse.up();
  await sleep(32);
}

async function submitPermutation(page, permutation) {
  for (let slot = 0; slot < 3; slot++) {
    const before = await page.evaluate(() => window.__GAME_TEST__.getState().placedCount);
    await drag(page, homes[permutation[slot]], slots[slot]);
    const after = await page.evaluate(() => window.__GAME_TEST__.getState().placedCount);
    // 렌더 프레임 경계에서 release가 유실됐을 때만 같은 수학 무관 좌표를 한 번 재현한다.
    if (after === before) await drag(page, homes[permutation[slot]], slots[slot]);
    if (process.env.DEBUG_B3 === '1') console.log('drag', {slot, group:permutation[slot], before, after:await page.evaluate(() => window.__GAME_TEST__.getState())});
  }
}

const hashBefore = artifactHash();
const server = await serveStatic(PUBLIC);
const launch = () => puppeteer.launch({headless:true, executablePath:resolveChrome(), args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio','--enable-unsafe-swiftshader','--use-gl=angle','--use-angle=swiftshader']});

async function openPage(browser) {
  const page = await browser.newPage(), errors = [];
  await page.setViewport({...VIEW, deviceScaleFactor:1});
  page.on('console', msg => { if (msg.type() === 'error') errors.push(msg.text()); });
  page.on('pageerror', err => errors.push(String(err)));
  await page.goto(`${server.url}/g/dalpaengi-parade/?b3=${Date.now()}`, {waitUntil:'load', timeout:45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout:60000});
  // 브라우저의 첫 제스처로 WebAudio와 Unity 입력 포커스를 깨운 뒤, 각 측정판은 start로 초기화한다.
  await page.mouse.click(12, 800);
  await sleep(120);
  return {page, errors};
}

async function runPolicy(kind) {
  const browser = await launch();
  try {
    const {page, errors} = await openPage(browser);
    const random = xorshift(20261031 + kind.length);
    let correct = 0, attempted = 0, completed = 0, stalled = 0, transportRetries = 0;
    for (let trial = 0; trial < TRIALS; trial++) {
      let p;
      if (kind === 'repeat') p = [0,1,2];
      else if (kind === 'cycle') { const k = trial % 3; p = [k,(k+1)%3,(k+2)%3]; }
      else {
        p = [0,1,2];
        for (let i=2;i>0;i--) { const j=Math.floor(random()*(i+1)); [p[i],p[j]]=[p[j],p[i]]; }
      }
      let submitted = false;
      for (let replay = 0; replay < 3 && !submitted; replay++) {
        await page.evaluate(() => window.__GAME_TEST__.start());
        await sleep(120);
        await submitPermutation(page, p);
        try {
          await page.waitForFunction(() => window.__GAME_TEST__.getState().firstAttempts === 1, {timeout:900});
          submitted = true;
        } catch { transportRetries++; }
      }
      if (!submitted) { stalled++; continue; }
      const state = await page.evaluate(() => window.__GAME_TEST__.getState());
      attempted += state.firstAttempts;
      correct += state.firstCorrect;
      if (state.phase === 'clear') completed++;
    }
    const rate = attempted ? correct/attempted : 0;
    return {
      games:TRIALS, firstAttemptCorrect:correct, firstAttemptTotal:attempted,
      firstAttemptRate:rate, chance:CHANCE, wilson95:wilson(correct,attempted),
      completed, completionRate:completed/TRIALS, stalled, transportRetries, consoleErrors:errors,
      passed:attempted===TRIALS && rate<=CHANCE && stalled===0 && errors.length===0
    };
  } finally { await browser.close(); }
}

const repeat = await runPolicy('repeat'); console.log('repeat', JSON.stringify(repeat));
const cycle = await runPolicy('cycle'); console.log('cycle', JSON.stringify(cycle));
const random = await runPolicy('random'); console.log('random', JSON.stringify(random));

const idleBrowser = await launch();
let idleProgressed=0, idleErrors=[];
try {
  const opened=await openPage(idleBrowser), page=opened.page; idleErrors=opened.errors;
  for(let trial=0;trial<TRIALS;trial++){
    const before=await page.evaluate(()=>{window.__GAME_TEST__.start();return window.__GAME_TEST__.getState();});
    await sleep(35);
    const after=await page.evaluate(()=>window.__GAME_TEST__.getState());
    if(after.solved!==before.solved||after.firstAttempts!==before.firstAttempts||after.placedCount!==0)idleProgressed++;
  }
} finally { await idleBrowser.close(); }
const idle={games:TRIALS,firstAttemptCorrect:0,firstAttemptTotal:0,firstAttemptRate:0,chance:CHANCE,completed:0,completionRate:0,progressed:idleProgressed,consoleErrors:idleErrors,passed:idleProgressed===0&&idleErrors.length===0};

const hashAfter=artifactHash();
const result={
  run_id:`dalpaengi-parade-b3-${new Date().toISOString()}`,
  artifact_hash_before:hashBefore, artifact_hash_after:hashAfter,
  criterion:'각 200판의 첫 노출 3무리 전체 순열 첫 시도 정답률이 우연 1/6 이하이고 무입력 진도가 0',
  method:'390×844 Chrome canvas의 실제 mouse down/move/up. start만 QA 브리지로 독립 판을 열고, 고정·순환·무작위 좌표 순열은 문제·prompt·answer를 읽지 않는다.',
  repeat,cycle,random,idle,
  passed:repeat.passed&&cycle.passed&&random.passed&&idle.passed&&hashBefore===hashAfter
};
fs.writeFileSync(new URL('./b3-pointer-results.json',import.meta.url),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify(result,null,2));
await server.close();
if(!result.passed)process.exitCode=1;
