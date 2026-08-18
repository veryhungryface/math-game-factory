#!/usr/bin/env node
/**
 * 다음에 만들 게임 슬롯을 고른다.
 *
 * 우선순위
 *  1) FOCUS(기본: 5·6학년 2학기) 단원 중 게임이 0개인 단원
 *  2) 게임 수가 가장 적은 단원 (동률이면 학기 순서 앞선 것)
 *  3) 최근 6개 생산물에서 쓰지 않은 메커닉을 붙인다
 *
 * 출력: 슬롯 JSON을 stdout으로. --write 를 주면 factory/work/slot.json 에도 쓴다.
 */
import fs from 'node:fs';
import path from 'node:path';
import { P, readJSON, writeJSON, listGames, readMeta, nowKST } from './paths.mjs';

const args = process.argv.slice(2);
const flag = (name, def = null) => {
  const i = args.indexOf(`--${name}`);
  return i >= 0 ? (args[i + 1]?.startsWith('--') ? true : args[i + 1]) : def;
};

// 기본 포커스: 2학기 시작 시기 → 5·6학년 2학기
const FOCUS = (flag('focus', '5-2,6-2') || '5-2,6-2')
  .split(',')
  .map((s) => s.trim())
  .filter(Boolean)
  .map((s) => {
    const [g, sem] = s.split('-').map(Number);
    return { grade: g, semester: sem };
  });

const curriculum = readJSON(P.curriculum);
const references = readJSON(P.references, { mechanics: [] });
const queue = readJSON(P.queue, { produced: [], failed: [], mechanic_history: [] });

const inFocus = (u) =>
  FOCUS.some((f) => f.grade === u.grade && f.semester === u.semester);

const units = (curriculum.units || []).filter(inFocus);
if (units.length === 0) {
  console.error(`포커스(${FOCUS.map((f) => `${f.grade}-${f.semester}`).join(',')})에 해당하는 단원이 교육과정 파일에 없습니다.`);
  process.exit(2);
}

// 현재 게시된 게임을 단원별로 집계
const publishedMetas = listGames()
  .map(readMeta)
  .filter(Boolean);

const countByUnit = new Map();
for (const u of units) countByUnit.set(u.id, 0);
for (const m of publishedMetas) {
  const id = m?.unit?.id;
  if (countByUnit.has(id)) countByUnit.set(id, countByUnit.get(id) + 1);
}

const sorted = [...units].sort((a, b) => {
  const ca = countByUnit.get(a.id) ?? 0;
  const cb = countByUnit.get(b.id) ?? 0;
  if (ca !== cb) return ca - cb;
  if (a.grade !== b.grade) return a.grade - b.grade;
  return (a.order ?? 0) - (b.order ?? 0);
});

const unit = sorted[0];

// --- 메커닉 선택 -------------------------------------------------------
const allMechanics = references.mechanics || [];
const recent = (queue.mechanic_history || []).slice(-6);
const usedByUnit = new Set(
  publishedMetas.filter((m) => m?.unit?.id === unit.id).map((m) => m.mechanic)
);

// 레퍼런스의 math_fit 은 2015 개정 영역명(도형/측정/규칙성)으로 적힌 것이 섞여 있다.
// 2022 개정 4영역으로 흡수해서 비교한다.
const DOMAIN_ALIASES = {
  '수와 연산': ['수와 연산', '연산', '수'],
  '변화와 관계': ['변화와 관계', '규칙성', '비와 비율', '비례'],
  '도형과 측정': ['도형과 측정', '도형', '측정', '공간'],
  '자료와 가능성': ['자료와 가능성', '자료', '가능성', '확률', '통계'],
};

const domainName = (curriculum.domains || []).find((d) => d.code === unit.domain)?.name || '';
const aliases = DOMAIN_ALIASES[domainName] || [domainName];

const scoreMechanic = (mech) => {
  let s = 100;
  if (recent.includes(mech.name)) s -= 60;            // 최근에 썼으면 강한 감점
  if (usedByUnit.has(mech.name)) s -= 80;             // 같은 단원에서 이미 썼으면 더 강한 감점
  const fits = (mech.math_fit || []).some((f) => aliases.some((a) => f.includes(a) || a.includes(f)));
  if (fits) s += 25;                                  // 단원 영역과 맞으면 가점
  s -= (mech.web_difficulty ?? 3) * 3;                // 구현 난이도 낮을수록 유리
  return s;
};

const candidates = allMechanics
  .map((m) => ({ ...m, _score: scoreMechanic(m) }))
  .sort((a, b) => b._score - a._score);

// 상위 5개를 후보로 넘긴다 — 기획 에이전트가 그 중에서 고르거나 조합한다.
const mechanicPool = candidates.slice(0, 5).map(({ _score, ...m }) => m);

const slot = {
  picked_at: nowKST(),
  focus: FOCUS,
  unit: {
    id: unit.id,
    grade: unit.grade,
    semester: unit.semester,
    order: unit.order,
    title: unit.title,
    standards: unit.standards || [],
    key_concepts: unit.key_concepts || [],
    misconceptions: unit.misconceptions || [],
    problem_types: unit.problem_types || [],
    domain: unit.domain,
    domain_name: domainName,
    prerequisite_units: unit.prerequisite_units || [],
    prerequisite_note: unit.prerequisite_note || '',
  },
  standards_detail: (unit.standards || [])
    .map((code) => (curriculum.standards || []).find((s) => s.code === code))
    .filter(Boolean),
  existing_games_in_unit: publishedMetas
    .filter((m) => m?.unit?.id === unit.id)
    .map((m) => ({ slug: m.slug, title: m.title, mechanic: m.mechanic })),
  mechanic_pool: mechanicPool,
  avoid_mechanics: [...new Set([...recent, ...usedByUnit])],
  // 교육부 고시 '성취기준 적용 시 고려 사항'에서 뽑은 문항 생성 제약(원문 근거).
  // 기획·빌드·검산 프롬프트에 SLOT_CTX 로 통째로 전달된다 — 문제 생성기는 이걸 지켜야 한다.
  generation_constraints: (curriculum.generation_constraints?.rules || []),
  // 최근 게임들이 전부 "어두운 배경 + 네온 시안/마젠타"로 수렴한 적이 있어서 추적한다.
  // 기획 에이전트는 이 목록에 있는 무드/배경색과 겹치지 않는 걸 골라야 한다.
  avoid_palette_moods: (queue.palette_history || []).slice(-6).map((p) => ({ mood: p.mood, bg: p.bg })),
  total_published: publishedMetas.length,
};

if (flag('write') !== null) {
  fs.mkdirSync(P.work, { recursive: true });
  writeJSON(path.join(P.work, 'slot.json'), slot);
}
console.log(JSON.stringify(slot, null, 2));
