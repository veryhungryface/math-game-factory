#!/usr/bin/env node
/**
 * 첫 플레이 이해도 감사 — codex(gpt-5.6-sol) 판정기.
 *
 *   node factory/lib/firstplay/judge.mjs <slug> [outRoot]
 *   FIRSTPLAY_OUT=/some/dir node factory/lib/firstplay/judge.mjs <slug>
 *
 * harness.mjs 가 남긴 <out>/<slug>/manifest.json 에서 대표 프레임 12장을 골라
 * `codex exec` 에 이미지로 첨부하고, judge-schema.json 구조로 판정을 받아
 * <out>/<slug>/judgment.json 에 쓴다.
 *
 * ⚠️ Claude 는 배관만 한다 — **판정은 반드시 codex 가 내린다.** 게임을 만든 모델이
 * 자기 게임의 이해도를 채점하면 통과시키는 쪽으로 기운다. 이 도구의 값어치는
 * "게임에 대해 아무것도 모르는 외부 판정자"라는 데 있다.
 *
 * 구독 CLI 를 쓰므로 ANTHROPIC_API_KEY 는 자식 프로세스 환경에서 지운다.
 * 규격은 `docs/onboarding-spec.md`.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawn } from 'node:child_process';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../..');
const SCHEMA = path.join(HERE, 'judge-schema.json');

const slug = process.argv[2];
const OUT_ROOT = process.argv[3] || process.env.FIRSTPLAY_OUT || path.join(ROOT, 'scratchpad/firstplay');
if (!slug) {
  console.error('usage: node factory/lib/firstplay/judge.mjs <slug> [outRoot]');
  process.exit(1);
}

const dir = path.join(OUT_ROOT, slug);
const manifest = JSON.parse(fs.readFileSync(path.join(dir, 'manifest.json'), 'utf8'));

/** 타이틀 2장 + 나머지를 시간축으로 균등 샘플링. */
function pickFrames(frames, want = 12) {
  const titles = frames.filter((f) => f.idx < 2);
  const rest = frames.filter((f) => f.idx >= 2);
  const wantRest = Math.max(1, want - titles.length);
  const chosen = [];
  const seen = new Set();
  for (let i = 0; i < wantRest; i++) {
    const t = rest.length > 1 ? (i * (rest.length - 1)) / (wantRest - 1) : 0;
    const idx = Math.round(t);
    const f = rest[Math.min(idx, rest.length - 1)];
    if (f && !seen.has(f.idx)) { seen.add(f.idx); chosen.push(f); }
  }
  return [...titles, ...chosen].sort((a, b) => a.idx - b.idx);
}

const chosen = pickFrames(manifest.frames, 12);
const images = chosen.map((f) => path.join(dir, f.file));
const captionLines = chosen
  .map((f, i) => `${i + 1}. [${f.file}] t=${f.elapsed_s}s phase=${f.phase}${f.action ? ` action=${f.action}` : ''}`)
  .join('\n');

const prompt = `너는 초등 3~6학년 학생 입장에서 "이 게임을 설명 없이 처음 봤을 때 이해할 수 있는가"를 냉정하게 판정하는 평가자다.
아래는 한 수학 게임의 첫 플레이 세션에서 순서대로 뽑은 대표 스크린샷 ${images.length}장이다. 각 이미지는 명령줄에서
같은 순서로 첨부되어 있고, 아래 목록이 그 순서와 시점(경과 초, 단계, 그 시점에 시뮬레이션이 취한 행동)을 알려준다.
이 행동은 "정답을 아는 사람"이 아니라 "화면에서 눈에 띄는 것을 무작정 눌러보는 순진한 학생"이 한 것이다.

프레임 순서:
${captionLines}

이 게임의 제목·규칙·수학 개념에 대해 너는 아무 사전 정보도 받지 않았다 — 오직 이 이미지들만으로 판단해라.
학생 눈높이 기준(초등 3~6학년, 문해력 낮음)으로 관대하게 봐주지 말고 아래를 판정해라:

1. comprehensible: 이 화면 흐름만 보고 학생이 (a) 목표가 뭔지 (b) 뭘 조작해야 하는지 (c) 수학이 어디에 쓰이는지
   세 가지를 모두 알 수 있으면 "yes", 일부만 알 수 있으면 "partial", 전혀 감을 못 잡으면 "no".
2. confusion_points: 특정 프레임(파일명으로 지칭)에서 구체적으로 무엇이 불명확했는지 나열해라. 최소 1개 이상 —
   완벽한 게임은 없다고 가정하고 사소한 것도 적어라.
3. onboarding_quality: 규칙을 설명하는 화면(온보딩/튜토리얼/도움말)이 존재하는지, 존재한다면 그 설명이
   실제로 조작법과 수학 규칙을 알려주는지 평가해라. 없으면 "온보딩 없음"이라고 명시해라.
4. title_issues: 타이틀 화면(첫 1~2장)의 제목과 태그라인 문구가 실제 게임 내용을 예고하는지, 학생이 이해할 수 있는
   표현인지 평가해라.
5. severity: 위 문제들을 종합했을 때 high(전면 재설명/재설계 필요) / medium / low 중 하나.

JSON 스키마에 맞춰 한국어로 답해라.`;

const args = [
  'exec', '-',
  ...images.flatMap((f) => ['-i', f]),
  '-m', 'gpt-5.6-sol',
  '--sandbox', 'read-only',
  '--skip-git-repo-check',
  '--cd', ROOT,
  '--output-schema', SCHEMA,
  '-o', path.join(dir, 'judgment.raw.json'),
];

function runOnce(timeoutMs) {
  return new Promise((resolve) => {
    const env = { ...process.env };
    delete env.ANTHROPIC_API_KEY;
    const child = spawn('codex', args, { env, stdio: ['pipe', 'pipe', 'pipe'] });
    let stdout = '', stderr = '';
    let timedOut = false;
    const timer = setTimeout(() => {
      timedOut = true;
      child.kill('SIGTERM');
      setTimeout(() => child.kill('SIGKILL'), 3000);
    }, timeoutMs);
    child.stdout.on('data', (d) => { stdout += d; });
    child.stderr.on('data', (d) => { stderr += d; });
    child.on('close', (code) => {
      clearTimeout(timer);
      resolve({ code, timedOut, stdout, stderr });
    });
    child.stdin.write(prompt);
    child.stdin.end();
  });
}

const TIMEOUT_MS = 12 * 60 * 1000;
let result = await runOnce(TIMEOUT_MS);
if (result.timedOut || result.code !== 0) {
  console.error(`[judge] ${slug}: 1차 시도 실패 (timedOut=${result.timedOut} code=${result.code}) — 재시도`);
  result = await runOnce(TIMEOUT_MS);
}

const rawOutPath = path.join(dir, 'judgment.raw.json');
let judgment = null;
if (fs.existsSync(rawOutPath)) {
  try {
    judgment = JSON.parse(fs.readFileSync(rawOutPath, 'utf8'));
  } catch (e) {
    console.error(`[judge] ${slug}: output-last-message JSON 파싱 실패 — ${e}`);
  }
}

const final = {
  slug,
  ok: !!judgment,
  timedOut: result.timedOut,
  exitCode: result.code,
  images_used: chosen.map((f) => f.file),
  judgment,
  stderr_tail: result.stderr.slice(-2000),
};
fs.writeFileSync(path.join(dir, 'judgment.json'), JSON.stringify(final, null, 2));
console.log(`[judge-done] ${slug} ok=${final.ok} comprehensible=${judgment?.comprehensible} severity=${judgment?.severity}`);
