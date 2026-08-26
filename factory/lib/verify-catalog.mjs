#!/usr/bin/env node
/**
 * queue.json, public/g/, catalog.json 사이의 게시 정합성을 검사한다.
 *
 *   node factory/lib/verify-catalog.mjs [--quiet]
 */
import fs from 'node:fs';
import path from 'node:path';
import { P, readJSON, listGames } from './paths.mjs';

const REQUIRED_FILES = ['index.html', 'meta.json', 'thumb.png', 'square.png'];

function isFile(file) {
  try {
    return fs.statSync(file).isFile();
  } catch {
    return false;
  }
}

function diagnoseMissingCatalog(slug) {
  const gameDir = path.join(P.games, slug);
  const metaFile = path.join(gameDir, 'meta.json');
  if (!fs.existsSync(gameDir)) return 'public/g 디렉터리 없음';
  if (!fs.existsSync(metaFile)) return 'meta.json 없음';

  try {
    const meta = readJSON(metaFile);
    if (meta.qa?.passed !== true) return `meta.qa.passed !== true (현재 ${JSON.stringify(meta.qa?.passed)})`;
  } catch (err) {
    return `meta.json 읽기 실패 (${err.message})`;
  }
  return '게시 조건은 충족했지만 catalog.json에 미반영 — 허브 재빌드 필요';
}

function main() {
  const args = process.argv.slice(2);
  const unknown = args.filter((arg) => arg !== '--quiet');
  if (unknown.length > 0) throw new Error(`알 수 없는 옵션입니다: ${unknown.join(', ')}`);
  const quiet = args.includes('--quiet');

  const queue = readJSON(P.queue);
  const catalog = readJSON(P.catalog);
  const produced = Array.isArray(queue.produced) ? queue.produced : [];
  const failed = Array.isArray(queue.failed) ? queue.failed : [];
  const mechanicHistory = Array.isArray(queue.mechanic_history) ? queue.mechanic_history : [];
  const catalogGames = Array.isArray(catalog.games) ? catalog.games : [];
  const gameDirs = listGames();

  const producedSlugs = new Set(produced.map((item) => item?.slug).filter(Boolean));
  const failedSlugs = new Set(failed.map((item) => item?.slug).filter(Boolean));
  const catalogSlugs = new Set(catalogGames.map((item) => item?.slug).filter(Boolean));
  const issues = [];

  for (const slug of [...producedSlugs].sort()) {
    if (!catalogSlugs.has(slug)) {
      issues.push(`queue.produced의 ${slug}가 카탈로그에 없음 — ${diagnoseMissingCatalog(slug)}`);
    }
  }

  for (const slug of [...catalogSlugs].sort()) {
    if (!producedSlugs.has(slug)) {
      issues.push(`catalog.json의 ${slug}가 queue.produced에 없음 — 생산 이력 누락`);
    }
  }

  for (const slug of gameDirs) {
    const gameDir = path.join(P.games, slug);
    const missing = REQUIRED_FILES.filter((file) => !isFile(path.join(gameDir, file)));
    if (missing.length > 0) {
      issues.push(`public/g/${slug} 필수 파일 누락 — ${missing.join(', ')}`);
    }
  }

  for (const slug of gameDirs) {
    if (!producedSlugs.has(slug) && !failedSlugs.has(slug)) {
      issues.push(`public/g/${slug}는 queue.produced/failed 어디에도 없는 고아 디렉터리`);
    }
  }

  if (mechanicHistory.length < produced.length) {
    issues.push(
      `queue.mechanic_history ${mechanicHistory.length}개 < produced ${produced.length}개 — 메커닉 중복 회피가 무력화될 수 있음`,
    );
  }

  const catalogCount = Number.isFinite(Number(catalog.count)) ? Number(catalog.count) : catalogGames.length;
  if (issues.length === 0) {
    if (!quiet) console.log(`✅ 정합성 이상 없음 — 게시 ${produced.length}작 / 카탈로그 ${catalogCount}작`);
    return;
  }

  for (const issue of issues) console.error(`❌ ${issue}`);
  console.error(`❌ 정합성 문제 ${issues.length}건 — 게시 ${produced.length}작 / 카탈로그 ${catalogCount}작`);
  process.exitCode = 1;
}

try {
  main();
} catch (err) {
  console.error(`❌ 정합성 검사 실패 — ${err.message}`);
  process.exit(1);
}
