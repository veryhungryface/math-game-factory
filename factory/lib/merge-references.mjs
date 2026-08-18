#!/usr/bin/env node
/**
 * 검토를 마친 레퍼런스 후보를 진짜 풀에 병합한다.
 *
 *   node factory/lib/merge-references.mjs review <파일>   후보를 사람이 읽기 좋게 출력 (병합 안 함)
 *   node factory/lib/merge-references.mjs approve <파일>  game-references.json 에 병합 + .md 재생성
 *
 * 이름이 이미 있는 항목은 건너뛴다(조용히 스킵, 개수만 알려줌) — 중복 병합 방지.
 */
import fs from 'node:fs';
import path from 'node:path';
import { P, readJSON, writeJSON } from './paths.mjs';

const cmd = process.argv[2];
const file = process.argv[3];

if (!cmd || !file) {
  console.error('사용법: node factory/lib/merge-references.mjs <review|approve> <references/pending/파일.json>');
  process.exit(2);
}

const target = path.isAbsolute(file) ? file : path.join(P.root, file);
if (!fs.existsSync(target)) {
  console.error(`파일이 없습니다: ${target}`);
  process.exit(1);
}
const cand = readJSON(target);

if (cmd === 'review') {
  console.log(`━━━ 레퍼런스 후보 검토: ${path.basename(target)} ━━━`);
  console.log(`포커스: ${cand.category}\n`);
  for (const m of cand.mechanics || []) {
    console.log(`[메커닉] ${m.name} ← ${m.origin}`);
    console.log(`  루프: ${m.core_loop}`);
    console.log(`  훅: ${m.hook}`);
    console.log(`  수학 접점: ${(m.math_fit || []).join(', ')} · 난이도 ${m.web_difficulty}`);
    if (m.notes) console.log(`  메모: ${m.notes}`);
    console.log();
  }
  for (const s of cand.sites || []) {
    console.log(`[사이트] ${s.name} (${s.category}) — ${s.url}`);
    console.log(`  루프: ${s.loop}`);
    console.log(`  훅: ${s.hook}`);
    console.log();
  }
  for (const v of cand.visual_refs || []) {
    console.log(`[비주얼] ${v.name} (${v.tech}) — ${v.url}`);
    console.log(`  와우 포인트: ${v.why_wow}`);
    if (v.license) console.log(`  라이선스: ${v.license}`);
    console.log();
  }
  process.exit(0);
}

if (cmd === 'approve') {
  const ref = readJSON(P.references, { mechanics: [], sites: [], visual_refs: [] });
  const existingNames = new Set([
    ...(ref.mechanics || []).map((m) => m.name),
    ...(ref.sites || []).map((s) => s.name),
    ...(ref.visual_refs || []).map((v) => v.name),
  ]);

  const added = { mechanics: 0, sites: 0, visual_refs: 0 };
  const skipped = { mechanics: 0, sites: 0, visual_refs: 0 };

  for (const key of ['mechanics', 'sites', 'visual_refs']) {
    ref[key] = ref[key] || [];
    for (const item of cand[key] || []) {
      if (existingNames.has(item.name)) {
        skipped[key]++;
        continue;
      }
      ref[key].push(item);
      existingNames.add(item.name);
      added[key]++;
    }
  }

  writeJSON(P.references, ref);
  console.log(`병합 완료 → ${path.relative(P.root, P.references)}`);
  console.log(`추가: 메커닉 ${added.mechanics} · 사이트 ${added.sites} · 비주얼 ${added.visual_refs}`);
  const skippedTotal = skipped.mechanics + skipped.sites + skipped.visual_refs;
  if (skippedTotal) console.log(`중복이라 건너뜀: ${skippedTotal}개`);

  // 승인된 파일은 archive 로 옮겨서 재승인 방지
  const archiveDir = path.join(path.dirname(target), 'approved');
  fs.mkdirSync(archiveDir, { recursive: true });
  fs.renameSync(target, path.join(archiveDir, path.basename(target)));

  console.log('\n다음: node factory/lib/build-references-doc.mjs 로 .md 재생성하세요.');
  process.exit(0);
}

console.error(`알 수 없는 명령: ${cmd}`);
process.exit(2);
