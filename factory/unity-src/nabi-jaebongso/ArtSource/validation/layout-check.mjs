#!/usr/bin/env node
// 나비 재봉소 화면 배치 회귀: 세로·가로 타이틀(시연판 여러 시점), 연습, 실전 각 단계의 화면을 캡처하고
// 원본 꼭짓점이 발문 패널 아래·대상 밑변이 도구 안내 위에 있는지 좌표로 확인한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const outDir = path.join(here, 'layout-check');
fs.mkdirSync(outDir, { recursive: true });
const chrome = process.env.PUPPETEER_EXECUTABLE_PATH ||
  path.join(process.env.HOME, '.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const server = await serveStatic(path.join(root, 'public'));
const browser = await puppeteer.launch({ headless: true, executablePath: chrome, args: ['--no-sandbox', '--mute-audio'] });
const errors = [];
const results = { portrait: [], tablet: [], landscape: [] };

async function open(width, height) {
  const page = await browser.newPage();
  await page.setViewport({ width, height, deviceScaleFactor: 1, isMobile: width < 600, hasTouch: width < 600 });
  page.on('pageerror', (e) => errors.push(String(e)));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  await page.goto(`${server.url}/g/nabi-jaebongso/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
  return page;
}
const st = (page) => page.evaluate(() => window.__GAME_TEST__.getState());

try {
  // 세로
  let page = await open(390, 844);
  for (const t of [1.0, 2.0, 3.6]) {
    await sleep(t === 1.0 ? 1000 : t === 2.0 ? 1000 : 1600);
    await page.screenshot({ path: path.join(outDir, `portrait-title-${t}s.png`) });
  }
  await page.mouse.click(195, 760); // 타이틀 CTA → 고정 연습
  await sleep(900);
  await page.screenshot({ path: path.join(outDir, 'portrait-practice.png') });
  // 연습 회귀: 안내와 다른 변(A→B)은 폭으로 받지 않고, 안내한 B→C만 받는다.
  {
    const s0 = await st(page);
    const drag = async (a, b) => {
      await page.mouse.move(a.x, a.y); await page.mouse.down();
      await page.mouse.move(b.x, b.y, { steps: 12 }); await page.mouse.up(); await sleep(250);
    };
    const A = { x: s0.sourceAScreenX, y: s0.sourceAScreenY }, B = { x: s0.sourceBScreenX, y: s0.sourceBScreenY }, C = { x: s0.sourceCScreenX, y: s0.sourceCScreenY };
    await drag(A, B);
    const afterWrong = await st(page);
    await page.screenshot({ path: path.join(outDir, 'portrait-practice-wrong-edge.png') });
    await sleep(1400);
    await drag(B, C);
    const afterRight = await st(page);
    results.practiceEdgeGate = {
      wrongEdgeRejected: afterWrong.compassSourceEdge === -1,
      bcAccepted: afterRight.compassSourceEdge === 2,
    };
  }
  results.portrait.push({ phase: 'practice', state: await st(page) });
  for (let i = 0; i < 6; i++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    await sleep(500);
    const s = await st(page);
    const ys = [s.sourceAScreenY, s.sourceBScreenY, s.sourceCScreenY];
    const xs = [s.sourceAScreenX, s.sourceBScreenX, s.sourceCScreenX, s.targetLeftScreenX, s.targetRightScreenX];
    results.portrait.push({
      problemId: s.problemId,
      sourceTopY: Math.min(...ys), sourceBottomY: Math.max(...ys), baseY: s.targetLeftScreenY,
      sourceLabelTopCenterY: s.sourceLabelTopCenterScreenY,
      minX: Math.min(...xs), maxX: Math.max(...xs),
      ok: Math.min(...ys) > 180 && Math.max(...ys) < s.targetLeftScreenY - 20 && s.targetLeftScreenY < 700 &&
        Math.min(...xs) > 10 && Math.max(...xs) < 380,
    });
    if (i < 2) await page.screenshot({ path: path.join(outDir, `portrait-play-${i}.png`) });
  }
  await page.close();

  // 820×1180 태블릿 세로: CanvasScaler가 반영된 발문 하단과 조작 안내 상단을 런타임에서 직접 읽는다.
  page = await open(820, 1180);
  await sleep(2600);
  await page.screenshot({ path: path.join(outDir, 'tablet-title-820x1180.png') });
  await page.mouse.click(410, 1040);
  await page.waitForFunction('window.__GAME_TEST__.getState().phase === "practice"', { timeout: 4000 });
  await sleep(500);
  await page.screenshot({ path: path.join(outDir, 'tablet-practice-820x1180.png') });
  for (let i = 0; i < 6; i++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    await sleep(450);
    const s = await st(page);
    const ys = [s.sourceAScreenY, s.sourceBScreenY, s.sourceCScreenY];
    const xs = [s.sourceAScreenX, s.sourceBScreenX, s.sourceCScreenX, s.targetLeftScreenX, s.targetRightScreenX];
    const row = {
      problemId: s.problemId,
      sourceTopY: Math.min(...ys), sourceBottomY: Math.max(...ys), baseY: s.targetLeftScreenY,
      sourceLabelTopCenterY: s.sourceLabelTopCenterScreenY,
      targetPanelBottomY: s.targetPanelBottomScreenY,
      minX: Math.min(...xs), maxX: Math.max(...xs),
      promptBottomY: s.promptBottomScreenY, toolHintTopY: s.toolHintTopScreenY,
    };
    row.ok = row.sourceTopY >= row.promptBottomY + 36 && row.sourceLabelTopCenterY >= row.promptBottomY + 16 &&
      row.sourceBottomY < row.baseY - 24 &&
      row.targetPanelBottomY <= row.toolHintTopY - 24 && row.minX > 20 && row.maxX < 800;
    results.tablet.push(row);
    await page.screenshot({ path: path.join(outDir, `tablet-play-rotation-${i}.png`) });
  }
  await page.close();

  // 가로
  page = await open(1280, 800);
  await sleep(2600);
  await page.screenshot({ path: path.join(outDir, 'landscape-title.png') });
  await page.evaluate(() => window.__GAME_TEST__.start());
  await sleep(700);
  await page.screenshot({ path: path.join(outDir, 'landscape-play.png') });
  results.landscape.push({ state: await st(page) });
  await page.close();
} finally {
  await browser.close();
  await server.close();
}
const summary = {
  errors,
  portraitAllOk: results.portrait.slice(1).every((r) => r.ok),
  tabletAllOk: results.tablet.every((r) => r.ok),
  results,
};
fs.writeFileSync(path.join(here, 'layout-check-results.json'), JSON.stringify(summary, null, 1));
console.log(JSON.stringify({
  errors,
  portraitAllOk: summary.portraitAllOk,
  tabletAllOk: summary.tabletAllOk,
  practiceEdgeGate: results.practiceEdgeGate,
  portraitRows: results.portrait.slice(1),
  tabletRows: results.tablet,
}, null, 1));
if (errors.length || !summary.portraitAllOk || !summary.tabletAllOk ||
    !results.practiceEdgeGate?.wrongEdgeRejected || !results.practiceEdgeGate?.bcAccepted) process.exitCode = 1;
