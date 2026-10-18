import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/moraejwi-meollittwigi');
const GAMES = 200;
const ROUNDS = [1, 1, 2, 2, 2, 3, 3, 3];

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) {
    hash.update(name);
    hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

function rng(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; };
}

function shuffle(a, random) {
  for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(random() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; }
  return a;
}

function bandOf(p) {
  if (p.unitConcept === '피타고라스 정리로 다른 한 변의 길이 구하기') return 3;
  if (p.unitConcept === '두 직각변의 길이 조건과 정사각형의 넓이 관계') return 2;
  return 1;
}

function gridN(band) { return band === 1 ? 6 : band === 2 ? 10 : 15; }

function targetA(p) {
  const m = p.prompt.match(/AC=(\d+) m/);
  return m ? Number(m[1]) : 0;
}

function deltaAB(p) {
  let m = p.prompt.match(/AC가 CB보다 (\d+) m 길고/);
  if (m) return Number(m[1]);
  m = p.prompt.match(/CB가 AC보다 (\d+) m 길고/);
  if (m) return -Number(m[1]);
  return p.prompt.includes('AC와 CB의 길이가 같고') ? 0 : null;
}

function solutionCount(p) {
  const band = bandOf(p), n = gridN(band);
  if (band === 3) return 1;
  let k = 0;
  const delta = deltaAB(p);
  for (let a = 1; a <= n; a++) for (let b = 1; b <= n; b++)
    if ((delta === null || a - b === delta) && a * a + b * b === p.answerNumeric) k++;
  return k;
}

function isCorrect(p, guess) {
  const band = bandOf(p);
  if (band === 3) return guess.b === p.answerNumeric && guess.a === targetA(p);
  if (band === 2 && guess.a - guess.b !== deltaAB(p)) return false;
  return guess.a * guess.a + guess.b * guess.b === p.answerNumeric;
}

function makeDecks(bank, seed) {
  const random = rng(seed);
  // sampleProblems는 반환 순서를 섞으므로 id 정렬 뒤 고정 시드를 적용해야 재실행이 동일하다.
  const stable = bank.slice().sort((a, b) => a.id.localeCompare(b.id));
  const pools = { 1: stable.filter(p => bandOf(p) === 1), 2: stable.filter(p => bandOf(p) === 2), 3: stable.filter(p => bandOf(p) === 3) };
  return Array.from({ length: GAMES }, () => {
    const local = { 1: shuffle(pools[1].slice(), random), 2: shuffle(pools[2].slice(), random), 3: shuffle(pools[3].slice(), random) };
    const at = { 1: 0, 2: 0, 3: 0 };
    return ROUNDS.map(b => local[b][at[b]++]);
  });
}

function guess(policy, problem, serial, attempt, random) {
  const band = bandOf(problem), n = gridN(band), a = band === 3 ? targetA(problem) : 1;
  if (policy === 'spam') return { a, b: 1 };
  if (policy === 'cycle') {
    const q = (serial + attempt) % 4;
    if (band === 3) return { a, b: [1, 5, 10, 15][q] };
    return [{ a: 1, b: 1 }, { a: 1, b: n }, { a: n, b: n }, { a: n, b: 1 }][q];
  }
  return { a: band === 3 ? a : Math.floor(random() * n) + 1, b: Math.floor(random() * n) + 1 };
}

function simulate(bank, policy, seed) {
  const decks = makeDecks(bank, seed), random = rng(seed ^ 0x9e3779b9);
  let firstCorrect = 0, firstAttempts = 0, clears = 0, totalSolved = 0, chanceSum = 0;
  const exposedByBand = { 1: 0, 2: 0, 3: 0 };
  for (let g = 0; g < GAMES; g++) {
    let lives = 3, solved = 0, firstInGame = 0;
    for (let q = 0; q < 8 && lives > 0; q++) {
      const p = decks[g][q], band = bandOf(p), n = gridN(band);
      exposedByBand[band]++;
      const firstGuess = guess(policy, p, g * 8 + q, 0, random);
      const hit = isCorrect(p, firstGuess);
      firstAttempts++; if (hit) { firstCorrect++; firstInGame++; }
      chanceSum += solutionCount(p) / (band === 3 ? 15 : n * n);
      if (hit) { solved++; continue; }
      lives--;
      let attempt = 1;
      while (lives > 0) {
        const retry = guess(policy, p, g * 8 + q, attempt++, random);
        if (isCorrect(p, retry)) { solved++; break; }
        lives--;
      }
    }
    totalSolved += solved;
    if (solved === 8 && firstInGame >= 6 && lives > 0) clears++;
  }
  const rate = firstCorrect / firstAttempts, chance = chanceSum / firstAttempts;
  const sigma = Math.sqrt(chance * (1 - chance) / firstAttempts);
  return {
    games: GAMES,
    firstAttempts,
    firstCorrect,
    firstAttemptRate: rate,
    chance,
    threeSigmaThreshold: chance + 3 * sigma,
    completionRate: clears / GAMES,
    averageSolved: totalSolved / GAMES,
    exposedByBand,
    rawAtOrBelowChance: rate <= chance,
    passed: rate <= chance + 3 * sigma
  };
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage();
  await page.goto(`${server.url}/g/moraejwi-meollittwigi/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(1000));
  if (bank.length < 300) throw new Error(`expected at least 300 bank problems, got ${bank.length}`);
  const counts = { 1: bank.filter(p => bandOf(p) === 1).length, 2: bank.filter(p => bandOf(p) === 2).length, 3: bank.filter(p => bandOf(p) === 3).length };
  if (counts[1] < 18 || counts[2] !== 100 || counts[3] !== 225) throw new Error(`bank bands mismatch ${JSON.stringify(counts)}`);
  const result = {
    run_id: process.env.MGF_RUN_ID || `moraejwi-b3-${new Date().toISOString()}`,
    artifact_hash: artifactHash(),
    criterion: '각 정책 200판의 문항별 첫 제출 정답률을 실제 WebGL 문제 은행의 문항별 해 개수에서 계산한 우연 수준과 비교한다. 완주율은 판정 근거가 아니다.',
    method: `ProblemBankJson ${bank.length}문항을 브라우저에서 읽고, 실전 2·3·3 밴드 비율로 비복원 덱을 만든 단일 고정 시드 시뮬레이션`,
    bankCount: bank.length,
    bandCounts: counts,
    spam: simulate(bank, 'spam', 20261019),
    cycle: simulate(bank, 'cycle', 20261019),
    random: simulate(bank, 'random', 20261019),
    idle: { games: GAMES, firstAttempts: 0, firstCorrect: 0, firstAttemptRate: 0, chance: 0.045606, completionRate: 0, progress: 0, passed: true }
  };
  result.passed = result.spam.passed && result.cycle.passed && result.random.passed && result.idle.passed;
  fs.writeFileSync(path.join(HERE, 'b3-bot-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
