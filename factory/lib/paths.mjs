import { fileURLToPath } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';

export const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');

// 학교급 (2026-09-26 중학교 확장). 크론이 SCHOOL=middle 로 부르면 슬롯이 중학교 교육과정에서 나온다.
// 허브·QA 는 학교급과 무관하게 두 파일을 합쳐 읽는다(loadAllCurricula) — 한 카탈로그에 공존하기 때문이다.
export const CURRICULUM_FILES = {
  elementary: path.join(ROOT, 'curriculum/2022-elementary-math.json'),
  middle: path.join(ROOT, 'curriculum/2022-middle-math.json'),
};
export const SCHOOL = process.env.SCHOOL === 'middle' ? 'middle' : 'elementary';
export const SCHOOL_LABEL = { elementary: '초', middle: '중' };
/** meta·unit·queue 항목의 학교급. 필드가 없던 기존 초등 게임은 elementary 로 본다. */
export const schoolOf = (x) => (x?.school === 'middle' || /^m\d/.test(x?.unit?.id || x?.id || '') ? 'middle' : 'elementary');

export const P = {
  root: ROOT,
  curriculum: CURRICULUM_FILES[SCHOOL],
  references: path.join(ROOT, 'references/game-references.json'),
  queue: path.join(ROOT, 'factory/state/queue.json'),
  games: path.join(ROOT, 'public/g'),
  publicDir: path.join(ROOT, 'public'),
  catalog: path.join(ROOT, 'public/catalog.json'),
  hub: path.join(ROOT, 'public/index.html'),
  work: path.join(ROOT, 'factory/work'),
  logs: path.join(ROOT, 'logs'),
};

const NO_FALLBACK = Symbol('no-fallback');

// fallback 을 안 주면 실패 시 던진다. fallback 으로 null 을 명시적으로 줘도(예: readMeta)
// 그 null 이 그대로 반환돼야 한다 — `fallback !== null` 로 비교하면 이 케이스에서
// 깨진다 (실제로 진행 중인 게임 폴더 때문에 pick-slot.mjs 가 죽은 적이 있다).
export function readJSON(file, fallback = NO_FALLBACK) {
  try {
    return JSON.parse(fs.readFileSync(file, 'utf8'));
  } catch (err) {
    if (fallback !== NO_FALLBACK) return fallback;
    throw new Error(`JSON 읽기 실패: ${file} — ${err.message}`);
  }
}

export function writeJSON(file, data) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, JSON.stringify(data, null, 2) + '\n');
}

export function listGames() {
  if (!fs.existsSync(P.games)) return [];
  return fs
    .readdirSync(P.games, { withFileTypes: true })
    .filter((d) => d.isDirectory() && !d.name.startsWith('.'))
    .map((d) => d.name)
    .sort();
}

export function readMeta(slug) {
  return readJSON(path.join(P.games, slug, 'meta.json'), null);
}

/** 두 학교급 교육과정을 합친 뷰 — 성취기준·단원 id 는 학교급 간에 겹치지 않는다([6수..] vs [9수..], g5s2 vs m2s2). */
export function loadAllCurricula() {
  const out = { standards: [], units: [], domains: [], meta: {} };
  for (const [school, file] of Object.entries(CURRICULUM_FILES)) {
    const c = readJSON(file, null);
    if (!c) continue;
    out.standards.push(...(c.standards || []).map((s) => ({ ...s, school })));
    out.units.push(...(c.units || []).map((u) => ({ ...u, school })));
    if (school === 'elementary') { out.domains = c.domains || []; out.meta = c.meta || {}; }
  }
  return out;
}

/** 한국 시간 ISO 문자열 */
export function nowKST() {
  const d = new Date(Date.now() + 9 * 3600 * 1000);
  return d.toISOString().replace('Z', '+09:00');
}
