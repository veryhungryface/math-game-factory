#!/usr/bin/env node
/**
 * 기획 프롬프트에 넣을 트렌드 요약을 stdout 으로 낸다 (2026-10-31).
 * 스카우트 결과는 사람 승인 전까지 game-references.json 에 안 들어가는데, 승인이 8/20 이후 한 번도 없어
 * 수집한 내용이 기획자에게 전혀 닿지 않았다. 승인 게이트는 그대로 두고 「참고용 후보」로만 보여 준다.
 */
import fs from 'node:fs';
import path from 'node:path';

const root = process.argv[2] || '.';
const N_FILES = 3;
const dir = path.join(root, 'references/pending');
const clip = (s, n) => String(s || '').replace(/\s+/g, ' ').slice(0, n);

const files = fs.readdirSync(dir).filter((f) => /^\d{4}-\d{2}-\d{2}.*\.json$/.test(f)).sort().slice(-N_FILES).reverse();
const lines = [];
for (const f of files) {
  let c;
  try { c = JSON.parse(fs.readFileSync(path.join(dir, f), 'utf8')); } catch { continue; }
  lines.push(`### ${c.scouted_at || f} — ${clip(c.category, 70)}`);
  for (const t of c.trend_notes || []) lines.push(`- 동향: ${clip(t.note, 160)} (${clip(t.source, 80)})`);
  for (const m of c.mechanics || []) lines.push(`- 메커닉 ${clip(m.name, 40)}: ${clip(m.core_loop, 110)}`);
  for (const v of c.visual_refs || []) lines.push(`- 비주얼 ${clip(v.name, 40)}: ${clip(v.why_wow, 90)}`);
}
console.log(lines.join('\n'));
