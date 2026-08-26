#!/usr/bin/env node
/**
 * 최초 게시와 폐기작 부활 게시의 단일 진입점.
 *
 *   node factory/lib/publish-game.mjs <slug> --score <N> [옵션]
 */
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { P, readJSON, writeJSON, nowKST } from './paths.mjs';

const REQUIRED_FILES = ['index.html', 'meta.json', 'thumb.png', 'square.png'];
const VALUE_OPTIONS = new Set(['score', 'gate', 'run', 'unit', 'mechanic', 'mood', 'bg', 'notes-from']);

function isFile(file) {
  try {
    return fs.statSync(file).isFile();
  } catch {
    return false;
  }
}

function fail(message) {
  throw new Error(message);
}

function parseArgs(argv) {
  const options = {};
  const positionals = [];

  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (!arg.startsWith('--')) {
      positionals.push(arg);
      continue;
    }

    const name = arg.slice(2);
    if (name === 'no-build') {
      options.noBuild = true;
      continue;
    }
    if (!VALUE_OPTIONS.has(name)) fail(`알 수 없는 옵션입니다: ${arg}`);

    const value = argv[i + 1];
    if (value === undefined || value.startsWith('--')) fail(`${arg} 옵션에 값이 필요합니다.`);
    options[name.replace(/-([a-z])/g, (_, c) => c.toUpperCase())] = value;
    i += 1;
  }

  if (positionals.length !== 1 || options.score === undefined) {
    fail('사용법: node factory/lib/publish-game.mjs <slug> --score <N> [옵션]');
  }

  const slug = positionals[0];
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(slug)) {
    fail(`잘못된 slug입니다: ${slug}`);
  }
  return { slug, options };
}

function parseNumber(value, label) {
  const number = Number(value);
  if (!Number.isFinite(number) || number < 0 || number > 100) {
    fail(`${label}은 0 이상 100 이하의 숫자여야 합니다: ${value}`);
  }
  return number;
}

function replaceBySlug(items, slug, replacement) {
  const first = items.findIndex((item) => item?.slug === slug);
  const withoutSlug = items.filter((item) => item?.slug !== slug);
  if (first < 0) return [...withoutSlug, replacement];
  withoutSlug.splice(Math.min(first, withoutSlug.length), 0, replacement);
  return withoutSlug;
}

function main() {
  const { slug, options } = parseArgs(process.argv.slice(2));
  const gameDir = path.join(P.games, slug);
  const missing = REQUIRED_FILES.filter((file) => !isFile(path.join(gameDir, file)));
  if (missing.length > 0) {
    fail(`게시 거부: public/g/${slug} 필수 파일 누락 — ${missing.join(', ')}`);
  }

  const metaFile = path.join(gameDir, 'meta.json');
  const meta = readJSON(metaFile);
  const score = parseNumber(options.score, '점수');

  let gate;
  if (options.gate !== undefined) {
    gate = parseNumber(options.gate, '게이트');
    if (gate < 80) fail(`게시 거부: 게이트를 80점 아래로 낮출 수 없습니다 — 요청값 ${gate}점`);
  } else {
    const metaGate = Number(meta.qa?.gate);
    gate = Number.isFinite(metaGate) ? Math.max(80, metaGate) : 80;
  }
  if (score < gate) {
    fail(`게시 거부: ${score}점은 게이트 ${gate}점 미만입니다.`);
  }

  // 게이트를 통과하기 전에는 아래 입력 파일이나 상태 파일도 쓰지 않는다.
  let notes = Array.isArray(meta.qa?.notes) ? meta.qa.notes : [];
  if (options.notesFrom !== undefined) {
    const review = readJSON(path.resolve(P.root, options.notesFrom));
    if (!Array.isArray(review.strengths)) {
      fail(`검수 JSON의 strengths가 배열이 아닙니다: ${options.notesFrom}`);
    }
    notes = review.strengths.slice(0, 3);
  }

  const queue = readJSON(P.queue, {
    produced: [],
    failed: [],
    mechanic_history: [],
    palette_history: [],
  });
  queue.produced = Array.isArray(queue.produced) ? queue.produced : [];
  queue.failed = Array.isArray(queue.failed) ? queue.failed : [];
  queue.mechanic_history = Array.isArray(queue.mechanic_history) ? queue.mechanic_history : [];
  queue.palette_history = Array.isArray(queue.palette_history) ? queue.palette_history : [];

  const previous = queue.produced.find((item) => item?.slug === slug);
  const unit = options.unit ?? meta.unit?.id ?? '';
  const mechanic = options.mechanic ?? meta.mechanic ?? '';
  const publishedAt = nowKST();
  const produced = {
    ...(previous || {}),
    run: options.run ?? previous?.run ?? '',
    slug,
    title: meta.title || slug,
    score,
    unit,
    mechanic,
    at: publishedAt,
  };

  const updatedMeta = {
    ...meta,
    qa: {
      score,
      gate,
      passed: true,
      reviewed_at: publishedAt,
      notes,
    },
  };
  writeJSON(metaFile, updatedMeta);

  queue.produced = replaceBySlug(queue.produced, slug, produced);
  queue.failed = queue.failed.filter((item) => item?.slug !== slug);
  if (mechanic && !queue.mechanic_history.includes(mechanic)) {
    queue.mechanic_history.push(mechanic);
  }

  if (options.mood !== undefined || options.bg !== undefined) {
    const previousPalette = queue.palette_history.find((item) => item?.slug === slug);
    const palette = {
      ...(previousPalette || {}),
      slug,
      mood: options.mood ?? previousPalette?.mood ?? '',
      bg: options.bg ?? previousPalette?.bg ?? '',
    };
    queue.palette_history = replaceBySlug(queue.palette_history, slug, palette);
  }
  writeJSON(P.queue, queue);

  if (!options.noBuild) {
    const buildFile = path.join(P.root, 'factory/lib/build-index.mjs');
    const result = spawnSync(process.execPath, [buildFile], {
      cwd: P.root,
      encoding: 'utf8',
    });
    if (result.status !== 0 || result.error) {
      const detail = [result.error?.message, result.stderr, result.stdout].filter(Boolean).join('\n').trim();
      fail(`허브 재빌드 실패${detail ? ` — ${detail}` : ''}`);
    }
  }

  if (options.noBuild) {
    // 허브를 안 다시 지었으면 카탈로그 개수는 아직 옛날 값이다 — 그걸 그대로 찍으면
    // "게시했는데 개수가 안 늘었다"로 오해한다.
    console.log(`✅ 게시: ${meta.title || slug}(${slug}) ${score}점 · 허브 재빌드는 생략(--no-build)`);
    return;
  }
  const catalog = readJSON(P.catalog);
  const count = Number.isFinite(Number(catalog.count)) ? Number(catalog.count) : (catalog.games || []).length;
  console.log(`✅ 게시: ${meta.title || slug}(${slug}) ${score}점 · 카탈로그 ${count}작`);
}

try {
  main();
} catch (err) {
  console.error(`❌ ${err.message}`);
  process.exit(1);
}
