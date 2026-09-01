#!/usr/bin/env node
/**
 * 첫 플레이 이해도 감사 — 판정 집계기.
 *
 *   node factory/lib/firstplay/aggregate.mjs                       # 기본 out 폴더 전체
 *   node factory/lib/firstplay/aggregate.mjs <outRoot>              # 폴더 지정
 *   node factory/lib/firstplay/aggregate.mjs <outRoot> <summary.json> [slug...]
 *
 * <out>/<slug>/judgment.json 들을 모아 severity 순으로 정렬한 SUMMARY.json 을 만든다.
 * slug 를 안 주면 outRoot 아래 manifest.json 이 있는 폴더를 전부 훑는다.
 * 규격은 `docs/onboarding-spec.md`.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../..');

const OUT_ROOT = process.argv[2] || process.env.FIRSTPLAY_OUT || path.join(ROOT, 'scratchpad/firstplay');
const SUMMARY_PATH = process.argv[3] || path.join(OUT_ROOT, 'SUMMARY.json');
let slugs = process.argv.slice(4);
if (!slugs.length) {
  slugs = fs.existsSync(OUT_ROOT)
    ? fs.readdirSync(OUT_ROOT, { withFileTypes: true })
        .filter((d) => d.isDirectory() && fs.existsSync(path.join(OUT_ROOT, d.name, 'manifest.json')))
        .map((d) => d.name)
        .sort()
    : [];
}
if (!slugs.length) {
  console.error(`[aggregate] ${OUT_ROOT} 안에 녹화 결과가 없다`);
  process.exit(1);
}

const sevRank = { high: 0, medium: 1, low: 2, unknown: 3 };

const rows = slugs.map((slug) => {
  const dir = path.join(OUT_ROOT, slug);
  let manifest = null, judgeFile = null;
  try { manifest = JSON.parse(fs.readFileSync(path.join(dir, 'manifest.json'), 'utf8')); } catch {}
  try { judgeFile = JSON.parse(fs.readFileSync(path.join(dir, 'judgment.json'), 'utf8')); } catch {}
  const j = judgeFile?.judgment || null;
  return {
    slug,
    recorded: !!manifest,
    frames: manifest?.frames?.length || 0,
    readyOk: manifest?.readyOk ?? null,
    consoleErrors: manifest?.consoleErrors?.length || 0,
    judged: !!judgeFile?.ok,
    comprehensible: j?.comprehensible || null,
    severity: j?.severity || 'unknown',
    onboarding_quality: j?.onboarding_quality || null,
    title_issues: j?.title_issues || null,
    confusion_points: j?.confusion_points || [],
  };
});

rows.sort((a, b) => (sevRank[a.severity] ?? 3) - (sevRank[b.severity] ?? 3));

const summary = {
  generated_at: new Date().toISOString(),
  total: rows.length,
  by_comprehensible: {
    yes: rows.filter((r) => r.comprehensible === 'yes').length,
    partial: rows.filter((r) => r.comprehensible === 'partial').length,
    no: rows.filter((r) => r.comprehensible === 'no').length,
    unjudged: rows.filter((r) => !r.judged).length,
  },
  severity_ranking: rows.map((r) => ({ slug: r.slug, severity: r.severity, comprehensible: r.comprehensible })),
  games: rows,
};

fs.mkdirSync(path.dirname(SUMMARY_PATH), { recursive: true });
fs.writeFileSync(SUMMARY_PATH, JSON.stringify(summary, null, 2));
console.log(`[aggregate] wrote ${SUMMARY_PATH} — ${rows.length} games (yes=${summary.by_comprehensible.yes} partial=${summary.by_comprehensible.partial} no=${summary.by_comprehensible.no})`);
