#!/usr/bin/env node
// 최종 WebGL 런타임 타이틀을 그대로 표지로 캡처한다. 생성 원화와 런타임이 다른 게임처럼 보이지 않게 한다.
import path from 'node:path';
import { launch, startServer, openGame, PUBLIC, SLUG, sleep } from './common.mjs';

const server = await startServer();
const browser = await launch();
const game = path.join(PUBLIC, 'g', SLUG);
const captures = [
  { w: 1200, h: 630, file: 'thumb.png', tag: 'cover-wide' },
  { w: 1080, h: 1080, file: 'square.png', tag: 'cover-square' },
];
const report = [];
for (const c of captures) {
  const { page, errors } = await openGame(browser, server, c.w, c.h, c.tag);
  await sleep(900);
  const target = path.join(game, c.file);
  await page.screenshot({ path: target });
  report.push({ file: c.file, width: c.w, height: c.h, console_errors: errors });
  await page.close();
}
await browser.close(); server.close?.();
console.log(JSON.stringify(report, null, 2));
if (report.some(x => x.console_errors.length)) process.exitCode = 1;
