#!/usr/bin/env node
/**
 * references/game-references.json → references/game-references.md
 * 기획 에이전트가 훑어보기 좋은 사람용 큐레이션 문서를 만든다.
 */
import fs from 'node:fs';
import path from 'node:path';
import { P, readJSON } from './paths.mjs';

const ref = readJSON(P.references);
const out = path.join(path.dirname(P.references), 'game-references.md');

const byCategory = new Map();
for (const s of ref.sites || []) {
  const c = s.category || '기타';
  if (!byCategory.has(c)) byCategory.set(c, []);
  byCategory.get(c).push(s);
}
const cats = [...byCategory.entries()].sort((a, b) => b[1].length - a[1].length);

const L = [];
L.push('# 레퍼런스 광산');
L.push('');
L.push(`수집일 ${ref.collected_at} · 사이트 ${(ref.sites || []).length} · 메커닉 ${(ref.mechanics || []).length} · 비주얼 ${(ref.visual_refs || []).length}`);
L.push('');
L.push(`> ${ref.note || '메커닉만 차용한다. 상표·캐릭터·에셋 복제 금지.'}`);
L.push('');
L.push('이 문서는 `game-references.json` 에서 자동 생성된다. 직접 고치지 마라.');
L.push('');

L.push('## 메커닉 — 기획의 1차 재료');
L.push('');
L.push('| 메커닉 | 원조 | 핵심 루프 | 웹 난이도 |');
L.push('|---|---|---|---|');
for (const m of ref.mechanics || []) {
  L.push(`| **${m.name}** | ${m.origin} | ${(m.core_loop || '').replace(/\|/g, '/')} | ${m.web_difficulty} |`);
}
L.push('');

L.push('### 메커닉별 수학 접목 아이디어');
L.push('');
for (const m of ref.mechanics || []) {
  L.push(`**${m.name}** — ${m.origin}`);
  for (const idea of m.math_ideas || []) L.push(`- ${idea}`);
  if (m.notes) L.push(`- _구현 메모: ${m.notes}_`);
  L.push('');
}

L.push('## 사이트 — 아이디어를 캐러 갈 곳');
L.push('');
for (const [cat, sites] of cats) {
  L.push(`### ${cat} (${sites.length})`);
  L.push('');
  for (const s of sites) {
    L.push(`- **[${s.name}](${s.url})** — ${s.age || '?'}세 · 난이도 ${s.web_difficulty ?? '?'}`);
    if (s.loop) L.push(`  - 루프: ${s.loop}`);
    if (s.hook) L.push(`  - 중독 요소: ${s.hook}`);
    if ((s.math_fit || []).length) L.push(`  - 수학 접점: ${s.math_fit.join(', ')}`);
    if (s.notes) L.push(`  - ${s.notes}`);
  }
  L.push('');
}

L.push('## 비주얼 레퍼런스 — "와우"의 기준선');
L.push('');
for (const v of ref.visual_refs || []) {
  L.push(`- **[${v.name}](${v.url})** — \`${v.tech}\``);
  if (v.why_wow) L.push(`  - ${v.why_wow}`);
  if (v.perf) L.push(`  - 성능: ${v.perf}`);
  if (v.license) L.push(`  - 라이선스: ${v.license}`);
}
L.push('');

fs.writeFileSync(out, L.join('\n'));
console.log(`레퍼런스 문서 생성 — ${out} (${Math.round(fs.statSync(out).size / 1024)}KB)`);
