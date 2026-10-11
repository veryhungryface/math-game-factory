import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const HERE = path.dirname(new URL(import.meta.url).pathname);
const FRAMES = path.join(HERE, 'second-fix');
fs.mkdirSync(FRAMES, {recursive: true});
const sleep = ms => new Promise(r => setTimeout(r, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
}
function artifactHash() {
  const dir = path.join(PUBLIC, 'g/ppittul-daemun'), files = [];
  (function walk(d) { for (const f of fs.readdirSync(d).sort()) { const p = path.join(d, f); fs.statSync(p).isDirectory() ? walk(p) : files.push(p); } })(dir);
  const h = crypto.createHash('sha256');
  for (const f of files) { h.update(path.relative(dir, f)); h.update(fs.readFileSync(f)); }
  return h.digest('hex');
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless:true, executablePath:resolveChrome(), args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const page = await browser.newPage();
const consoleErrors = [];
page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); });
page.on('pageerror', e => consoleErrors.push(String(e)));
await page.setViewport({width:390,height:844,deviceScaleFactor:1});
await page.goto(`${server.url}/g/ppittul-daemun/`, {waitUntil:'load',timeout:45000});
await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout:60000});

const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(352));
const conceptCounts = {};
const bankErrors = [];
for (const p of bank) {
  conceptCounts[p.unitConcept] = (conceptCounts[p.unitConcept] || 0) + 1;
  if (!Number.isInteger(p.answerNumeric) || p.answerNumeric < 40 || p.answerNumeric > 140 || p.answerNumeric % 10)
    bankErrors.push(`${p.id}: answerNumeric ${p.answerNumeric}`);
  const m = /^∠A=(\d+)°$/.exec(p.answer);
  if (m && Number(m[1]) !== p.answerNumeric) bankErrors.push(`${p.id}: ${p.answer} != ${p.answerNumeric}`);
  if (/√|가정|결론/.test(p.prompt)) bankErrors.push(`${p.id}: scope trap`);
}
const expectedConcepts = {
  '평행사변형의 대각':96,
  '평행사변형의 이웃각':64,
  '평행사변형의 대각선 이등분':64,
  '직사각형의 대각선':64,
  '마름모의 대각선':32,
  '사각형의 포함 관계':32
};
for (const [name,count] of Object.entries(expectedConcepts)) {
  if (conceptCounts[name] !== count) bankErrors.push(`concept ${name}: ${conceptCounts[name]} != ${count}`);
}

const viewports = [
  {name:'mobile-390',width:390,height:844},
  {name:'desktop-1280',width:1280,height:800},
  {name:'desktop-1440',width:1440,height:900},
  {name:'wide-2000',width:2000,height:1045}
];
const layouts = [];
for (const v of viewports) {
  await page.setViewport({width:v.width,height:v.height,deviceScaleFactor:1});
  await sleep(350);
  const state = await page.evaluate(() => window.__GAME_TEST__.getState());
  const ok = v.width < 1024
    ? state.hudNeedlesVisible === true
    : state.promptLeftPx >= 24 && state.relationRightPx <= v.width - 24;
  layouts.push({...v,promptLeftPx:state.promptLeftPx,relationRightPx:state.relationRightPx,hudNeedlesVisible:state.hudNeedlesVisible,ok});
  await page.screenshot({path:path.join(FRAMES, `${v.name}.png`)});
}

const result = {
  runId:`ppittul-daemun-second-fix-${new Date().toISOString()}`,
  artifactHashSha256:artifactHash(),
  bank:{count:bank.length,conceptCounts,errors:bankErrors,propertyCount:
    (conceptCounts['평행사변형의 대각선 이등분']||0)+(conceptCounts['직사각형의 대각선']||0)+
    (conceptCounts['마름모의 대각선']||0)+(conceptCounts['사각형의 포함 관계']||0)},
  layouts,
  consoleErrors,
  passed:bank.length===352&&bankErrors.length===0&&layouts.every(x=>x.ok)&&consoleErrors.length===0
};
fs.writeFileSync(path.join(HERE,'second-fix-results.json'),JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
await page.close(); await browser.close(); await server.close();
if (!result.passed) process.exitCode=1;
