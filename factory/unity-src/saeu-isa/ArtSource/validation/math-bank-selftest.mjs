#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import { HERE, artifactHash, closeSession, createSession } from './runtime-common.mjs';

const interval = text => {
  const m = String(text).match(/(\d+) 이상 (\d+) 미만/);
  if (!m) throw new Error(`interval parse failed: ${text}`);
  return { lo: Number(m[1]), hi: Number(m[2]) };
};

const session = await createSession();
try {
  const bank = await session.page.evaluate(() => window.__GAME_TEST__.sampleProblems(1000));
  const ids = new Set(), prompts = new Set();
  const counts = { band1: 0, band2: 0, band3: 0 };
  const audit = { uniqueClass: 0, upperBoundaryLeftFalse: 0, wrongOriginFalse: 0, totalDistractorChecks: 0, accidentalTrue: 0 };
  const failures = [];
  for (const p of bank) {
    if (ids.has(p.id)) failures.push(`${p.id}: duplicate id`); ids.add(p.id);
    if (prompts.has(p.prompt)) failures.push(`${p.id}: duplicate prompt`); prompts.add(p.prompt);
    const band = p.id.match(/^hist-b([123])-/)?.[1]; if (!band) failures.push(`${p.id}: bad id`); else counts[`band${band}`]++;
    const value = Number(p.prompt.match(/길이가 (\d+) mm인/)?.[1]);
    if (/표본을 기록의 분류/.test(p.prompt)) failures.push(`${p.id}: doubled object particle remains`);
    if (band === '3' && !/표본의 분류를 고치시오/.test(p.prompt)) failures.push(`${p.id}: repair phrasing mismatch`);
    if (!Number.isInteger(value)) failures.push(`${p.id}: value parse failed`);
    const classPart = p.prompt.split('계급: ')[1] || '';
    const classes = [...classPart.matchAll(/(\d+) 이상 (\d+) 미만/g)].map(m => ({ lo: Number(m[1]), hi: Number(m[2]) }));
    const answer = interval(p.answer);
    const hits = classes.filter(c => c.lo <= value && value < c.hi);
    if (classes.length !== 4 || hits.length !== 1 || hits[0].lo !== answer.lo || hits[0].hi !== answer.hi)
      failures.push(`${p.id}: unique integer class proof failed`); else audit.uniqueClass++;
    if (p.choices !== null) failures.push(`${p.id}: spatial answer must have choices=null`);
    if (p.answerNumeric !== answer.lo) failures.push(`${p.id}: answerNumeric must equal correct lower bound`);

    const correctIndex = classes.findIndex(c => c.lo === answer.lo && c.hi === answer.hi);
    if (correctIndex > 0 && value === answer.lo) {
      audit.totalDistractorChecks++;
      const left = classes[correctIndex - 1];
      if (left.lo <= value && value < left.hi) audit.accidentalTrue++; else audit.upperBoundaryLeftFalse++;
    }
    if (band === '3') {
      const originText = p.prompt.match(/현재 구간: (\d+ 이상 \d+ 미만)/)?.[1];
      if (!originText) failures.push(`${p.id}: repair origin missing`);
      else {
        audit.totalDistractorChecks++;
        const origin = interval(originText);
        if (origin.lo <= value && value < origin.hi) audit.accidentalTrue++; else audit.wrongOriginFalse++;
      }
    }
  }
  const output = {
    generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(),
    source: 'final WebGL ProblemBankJson via sampleProblems(1000)', bankCount: bank.length,
    uniqueIds: ids.size, uniquePrompts: prompts.size, counts, audit, failures,
    passed: bank.length === 740 && ids.size === 740 && prompts.size === 740 && counts.band1 === 180 && counts.band2 === 80 && counts.band3 === 480 && audit.accidentalTrue === 0 && failures.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'math-bank-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally { await closeSession(session); }
