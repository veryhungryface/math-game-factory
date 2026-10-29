#!/usr/bin/env node
// 최종 WebGL이 실제로 푸시한 360문항을 전수 검산한다. 별도 시험지나 C# 소스 추정이 아니다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { launch, startServer, openGame, PUBLIC, SLUG } from './common.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const game = path.join(PUBLIC, 'g', SLUG);
function artifactHash() {
  const rels = ['index.html', ...fs.readdirSync(path.join(game, 'Build')).sort().map(n => `Build/${n}`)];
  const h = crypto.createHash('sha256');
  for (const rel of rels) { h.update(rel); h.update(fs.readFileSync(path.join(game, rel))); }
  return h.digest('hex');
}
function expected(p) {
  let m = p.prompt.match(/PA=(\d+) cm/);
  if (m && !/PA\+PB/.test(p.prompt)) return +m[1];
  m = p.prompt.match(/PA\+PB=(\d+) cm/);
  if (m) return +m[1] / 2;
  const ab = p.prompt.match(/AB=(\d+) cm/), ad = p.prompt.match(/AD=(\d+) cm/);
  if (ab && ad) return +ab[1] - +ad[1];
  return NaN;
}

const server = await startServer(); const browser = await launch();
const { page, errors } = await openGame(browser, server, 390, 844, 'math-bank');
const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(360));
const issues = [], ids = new Set(), prompts = new Set(), freq = Array(25).fill(0);
for (const p of bank) {
  const exp = expected(p), actual = Number(p.answerNumeric);
  if (!p.id || ids.has(p.id)) issues.push(`id 중복/누락: ${p.id}`); ids.add(p.id);
  if (!p.prompt || prompts.has(p.prompt)) issues.push(`발문 중복/누락: ${p.id}`); prompts.add(p.prompt);
  if (!Number.isInteger(exp) || exp !== actual || p.answer !== `${exp} cm`) issues.push(`정답 불일치 ${p.id}: ${exp}/${p.answer}/${actual}`);
  if (actual >= 1 && actual <= 24) freq[actual]++;
  if (/^k[12]-/.test(p.id) && !p.prompt.includes('점 P에서 원 O에 그은 두 접선의 접점을 각각 A, B라 하자')) issues.push(`접선 정의 누락: ${p.id}`);
}
for (let t = 1; t <= 24; t++) if (freq[t] !== 15) issues.push(`정답 ${t} 빈도 ${freq[t]} != 15`);
const result = { slug: SLUG, run_id: process.env.MGF_RUN_ID || null, artifact_hash: artifactHash(),
  source: 'final WebGL __GAME_TEST__.sampleProblems(360)', count: bank.length, unique_ids: ids.size, unique_prompts: prompts.size,
  answer_frequency_1_to_24: freq.slice(1), console_errors: errors, issues };
result.passed = bank.length === 360 && ids.size === 360 && prompts.size === 360 && errors.length === 0 && issues.length === 0;
fs.writeFileSync(path.join(HERE, 'math-bank-results.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
await page.close(); await browser.close(); server.close?.(); if (!result.passed) process.exitCode = 1;
