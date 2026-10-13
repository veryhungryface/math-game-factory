import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/byeolkan-gwancheukso');
const LABEL = process.env.DISCOVERY_LABEL || 'after';
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing not found');
}

function buildHash() {
  const hash = crypto.createHash('sha256');
  for (const rel of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(name => `Build/${name}`)]) {
    hash.update(rel);
    hash.update(fs.readFileSync(path.join(GAME, rel)));
  }
  return hash.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function tap(page, x, y) {
  await page.mouse.move(x, y); await page.mouse.down(); await sleep(40); await page.mouse.up(); await sleep(120);
}
async function drag(page, from, to) {
  await page.mouse.move(...from); await page.mouse.down(); await sleep(45);
  await page.mouse.move(...to, { steps: 12 }); await sleep(55); await page.mouse.up(); await sleep(180);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  const errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));
  await page.goto(`${server.url}/g/byeolkan-gwancheukso/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });

  await tap(page, 195, 616);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().onboarding === true, { timeout: 7000 });
  const before = await state(page);

  // 같은 입력 사례: 보이는 A 랙의 중앙에서 교차 홈의 아래쪽 가장자리까지 드래그한다.
  // 기존 빌드는 홈 경계에서 놓침 처리했고, 수정 빌드는 첫 연습에 한해 근접 스냅해야 한다.
  const gesture = { from: [86, 701], to: [195, 610] };
  await drag(page, gesture.from, gesture.to);
  const after = await state(page);
  const screenshot = path.join(HERE, `discoverability-${LABEL}.png`);
  await page.screenshot({ path: screenshot });

  const result = {
    generated_at: new Date().toISOString(),
    label: LABEL,
    build_sha256: buildHash(),
    gesture,
    before,
    after,
    rackAPlaced: /^crossed\+/.test(after.rackTopology),
    errors,
    result: /^crossed\+/.test(after.rackTopology) && errors.length === 0 ? 'pass' : 'fail'
  };
  fs.writeFileSync(path.join(HERE, `discoverability-${LABEL}-results.json`), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result, null, 2));
} finally {
  await browser.close();
  await server.close();
}
