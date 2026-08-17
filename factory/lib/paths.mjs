import { fileURLToPath } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';

export const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
export const P = {
  root: ROOT,
  curriculum: path.join(ROOT, 'curriculum/2022-elementary-math.json'),
  references: path.join(ROOT, 'references/game-references.json'),
  queue: path.join(ROOT, 'factory/state/queue.json'),
  games: path.join(ROOT, 'public/g'),
  publicDir: path.join(ROOT, 'public'),
  catalog: path.join(ROOT, 'public/catalog.json'),
  hub: path.join(ROOT, 'public/index.html'),
  work: path.join(ROOT, 'factory/work'),
  logs: path.join(ROOT, 'logs'),
};

export function readJSON(file, fallback = null) {
  try {
    return JSON.parse(fs.readFileSync(file, 'utf8'));
  } catch (err) {
    if (fallback !== null) return fallback;
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

/** 한국 시간 ISO 문자열 */
export function nowKST() {
  const d = new Date(Date.now() + 9 * 3600 * 1000);
  return d.toISOString().replace('Z', '+09:00');
}
