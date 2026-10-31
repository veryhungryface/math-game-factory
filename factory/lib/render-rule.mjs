#!/usr/bin/env node
/**
 * 중학교 라인의 3D 쏠림을 막는 기계 게이트 (2026-10-31 사용자 지시 「계속 3D 모양으로만 만든다」).
 * 판정은 일부러 이진(flat-2d | mesh-3d)이다 — 스타일 목록을 만들면 그 목록이 새 템플릿이 된다
 * (loop-engineering §7.8). 어떤 평면 스타일로 만들지는 매번 기획자가 새로 짓는다.
 *
 *   node factory/lib/render-rule.mjs check <work폴더>   chosen.json 이 slot.render_rule 을 어기면
 *                                                       flat-2d 기획안으로 교체. 대체안이 없으면 종료코드 1
 *   node factory/lib/render-rule.mjs stamp <work폴더> <slug> <루트>
 *                                                       소스를 실측해 meta.render_dimension 을 기록
 */
import fs from 'node:fs';
import path from 'node:path';

const readJSON = (p) => JSON.parse(fs.readFileSync(p, 'utf8'));
const [cmd, work, slug, root] = process.argv.slice(2);

if (cmd === 'check') {
  const rule = readJSON(path.join(work, 'slot.json')).render_rule;
  const chosenPath = path.join(work, 'chosen.json');
  const chosen = readJSON(chosenPath);
  if (!rule || rule.mesh3d_allowed || chosen.render_dimension === 'flat-2d') {
    console.log(`통과: ${chosen.slug} (${chosen.render_dimension ?? '미기재'})`);
    process.exit(0);
  }
  const alt = fs.readdirSync(work)
    .filter((f) => /^concept-\d+\.json$/.test(f)).sort()
    .map((f) => readJSON(path.join(work, f)))
    .find((c) => c.render_dimension === 'flat-2d');
  if (!alt) {
    console.log(`실격: ${chosen.slug} 은 ${chosen.render_dimension ?? '미기재'} 인데 이번 회차는 flat-2d 만 허용되고 대체 기획안이 없다`);
    process.exit(1);
  }
  fs.writeFileSync(chosenPath, JSON.stringify(alt, null, 2));
  console.log(`교체: ${chosen.slug} → ${alt.slug} (이번 회차는 flat-2d 만 허용)`);
  process.exit(0);
}

if (cmd === 'stamp') {
  const declared = readJSON(path.join(work, 'chosen.json')).render_dimension;
  const scripts = path.join(root, 'factory/unity-src', slug, 'Scripts');
  let lit = 0;
  if (fs.existsSync(scripts)) {
    for (const f of fs.readdirSync(scripts).filter((n) => n.endsWith('.cs'))) {
      const src = fs.readFileSync(path.join(scripts, f), 'utf8');
      lit += (src.match(/MgfLook\.(Lit|Block|RoundedBox)\s*\(/g) || []).length;
    }
  }
  // 기획서가 평면이라 해도 조명 메쉬를 쓰면 3D 로 센다 — 이력이 자기신고로 오염되지 않게.
  const measured = declared === 'flat-2d' && lit === 0 ? 'flat-2d' : 'mesh-3d';
  const metaPath = path.join(root, 'public/g', slug, 'meta.json');
  const meta = readJSON(metaPath);
  meta.render_dimension = measured;
  fs.writeFileSync(metaPath, JSON.stringify(meta, null, 2) + '\n');
  console.log(`${slug}: 기획 ${declared ?? '미기재'} · 조명 메쉬 호출 ${lit}회 → ${measured}`);
  process.exit(declared === 'flat-2d' && measured !== 'flat-2d' ? 3 : 0);
}

console.error('사용법: render-rule.mjs <check|stamp> ...');
process.exit(2);
