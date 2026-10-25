#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const publicDir = path.join(root, 'public');
const output = path.join(here, 'bot-results.json');

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing not found');
}

function parse(problem) {
  const m = problem.answer.match(/DE=(\d+);DF=(\d+);EF=(\d+)/);
  const target = problem.answer.match(/target=\(([-\d.]+),([-\d.]+)\)/);
  if (!m) throw new Error(`lengths missing: ${problem.id}`);
  if (!target) throw new Error(`target missing: ${problem.id}`);
  return { id: problem.id, de: Number(m[1]), df: Number(m[2]), ef: Number(m[3]), target: { x: Number(target[1]), y: Number(target[2]) } };
}

function correct(problem, point) {
  if (point.y < 0) return false;
  const df = Math.hypot(point.x, point.y);
  const ef = Math.hypot(point.x - problem.de, point.y);
  return Math.abs(df - problem.df) <= 0.25 && Math.abs(ef - problem.ef) <= 0.25;
}

function rng(seed) {
  let x = seed >>> 0;
  return () => {
    x += 0x6D2B79F5;
    let t = x;
    t = Math.imul(t ^ t >>> 15, t | 1);
    t ^= t + Math.imul(t ^ t >>> 7, t | 61);
    return ((t ^ t >>> 14) >>> 0) / 4294967296;
  };
}

function initialPoint(problem) {
  const serial = Number(problem.id.replace('bb-', ''));
  const group = Math.floor((serial - 1) / 6);
  const band = group < 20 ? 1 : group < 40 ? 2 : 3;
  const lastDigitEven = Number(problem.id.at(-1)) % 2 === 0;
  if (band === 3 || (band === 2 && lastDigitEven)) {
    return { x: problem.de - problem.target.x, y: problem.target.y };
  }
  const targetAngle = Math.atan2(problem.target.y, problem.target.x) * 180 / Math.PI;
  for (const offset of [20, -20, 35, -35, 50, -50, 70, -70, 90, -90]) {
    const angle = (targetAngle + offset) * Math.PI / 180;
    const candidate = { x: Math.cos(angle) * problem.df, y: Math.sin(angle) * problem.df };
    if (candidate.y > 0 && candidate.x >= -1 && candidate.x <= 11 && candidate.y <= 10 &&
        Math.hypot(candidate.x - problem.target.x, candidate.y - problem.target.y) >= 1) return candidate;
  }
  throw new Error(`no initial point for ${problem.id}`);
}

const server = await serveStatic(publicDir);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  await page.goto(`${server.url}/g/bingmak-bonghap/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
  const bank = (await page.evaluate(() => window.__GAME_TEST__.sampleProblems(1000))).map(parse);
  if (bank.length !== 360) throw new Error(`expected 360 problems, got ${bank.length}`);

  const attempts = 200;
  const cyclePoints = [
    { x: -0.5, y: 0.5 }, { x: 10.5, y: 0.5 },
    { x: -0.5, y: 9.5 }, { x: 10.5, y: 9.5 },
  ];
  const random = rng(20261025);
  const policies = {
    // 실제 연타/놓기: 문제마다 주어진 오답 초기 F를 움직이지 않고 반복해서 놓는다.
    spam: (_i, problem) => initialPoint(problem),
    cycle: i => cyclePoints[i % cyclePoints.length],
    random: () => ({ x: -1 + random() * 12, y: random() * 10 }),
  };
  const measured = {};
  for (const [name, policy] of Object.entries(policies)) {
    let firstCorrect = 0;
    for (let i = 0; i < attempts; i++) {
      const problem = bank[i % bank.length];
      if (correct(problem, policy(i, problem))) firstCorrect++;
    }
    measured[name] = { firstCorrect, firstTotal: attempts, accuracy: firstCorrect / attempts };
  }

  const chanceRng = rng(90304);
  const chanceSamples = 1_000_000;
  let chanceHits = 0;
  for (let i = 0; i < chanceSamples; i++) {
    const p = bank[Math.floor(chanceRng() * bank.length)];
    const point = { x: -1 + chanceRng() * 12, y: chanceRng() * 10 };
    if (correct(p, point)) chanceHits++;
  }

  await new Promise(resolve => setTimeout(resolve, 5000));
  const noInputState = await page.evaluate(() => window.__GAME_TEST__.getState());
  const result = {
    artifact: fs.readdirSync(path.join(publicDir, 'g', 'bingmak-bonghap', 'Build')).sort(),
    bankSize: bank.length,
    contract: 'abs(DF-targetDF)<=0.25 && abs(EF-targetEF)<=0.25 && marked half-plane',
    chance: { hits: chanceHits, samples: chanceSamples, rate: chanceHits / chanceSamples },
    policies: { ...measured, noInput: { waitedSeconds: 5, phase: noInputState.phase, solved: noInputState.solved } },
  };
  fs.writeFileSync(output, JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result, null, 2));
} finally {
  await browser.close();
  await server.close();
}
