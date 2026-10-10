// B3 무뇌 봇 자가 테스트 — 빌드된 게임을 ?selftest=1 로 열어 C# BangulBots.Run(200판×봇) 결과를 받는다.
// 같은 BangulRules(생성기·정수 판정)를 쓰므로 실제 판정 경로와 같다. 결과: bot-results.json
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';
const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
function chromePath() {
  const base = path.join(process.env.HOME, '.cache/puppeteer/chrome');
  for (const b of fs.readdirSync(base).sort().reverse()) { const c = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'); if (fs.existsSync(c)) return c; }
}
const srv = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ executablePath: chromePath(), headless: true, protocolTimeout: 300000 });
const page = await browser.newPage();
const got = new Promise((resolve) => page.on('console', m => { const t = m.text(); if (t.startsWith('BANGUL_SELFTEST ')) resolve(t.slice(16).split('\n')[0]); }));
page.on('pageerror', e => console.log('PAGEERR', String(e)));
await page.goto(srv.url + '/g/ttak-matneun-bangul/index.html?selftest=1');
const t0 = Date.now();
const json = await Promise.race([got, new Promise((_, rej) => setTimeout(() => rej(new Error('timeout')), 280000))]);
const res = JSON.parse(json);
res.ranAt = new Date().toISOString(); res.seconds = (Date.now() - t0) / 1000;
fs.writeFileSync(path.join(HERE, 'bot-results.json'), JSON.stringify(res, null, 2));
console.log(JSON.stringify(res.chance));
for (const b of res.bots) console.log(b.bot.padEnd(14), 'first', b.firstTryRate, `(${b.firstOk}/${b.firstTotal})`, 'complete', b.completionRate, 'mastery', b.masteryRate, 'timeouts', b.timeouts, 'heartOuts', b.heartOuts);
console.log('practiceValid', res.practiceValid, 'practiceChance', res.practiceChance, 'bank', JSON.stringify(res.bank), 'secs', res.seconds);
await browser.close(); srv.close();
