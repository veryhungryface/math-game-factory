#!/usr/bin/env node
/**
 * 레퍼런스 스카우트 트리거 + 후처리.
 *
 *   node factory/lib/scout-references.mjs check     게시된 게임 수 기준으로 이번에 돌려야 하는지 판단, 0/1 종료코드
 *   node factory/lib/scout-references.mjs prepare    focus 카테고리를 골라 factory/work/scout-focus.json 작성
 *   node factory/lib/scout-references.mjs ingest     factory/work/scout-candidates.json 을
 *                                                     references/pending/<날짜>.json 으로 보관 + 상태 갱신
 *
 * 후보는 게임 제작에 바로 안 쓰인다 — references/pending/ 에 쌓이고, 사람이 검토해서
 * `node factory/lib/merge-references.mjs <파일>` 로 승인해야 game-references.json 에 들어간다.
 */
import fs from 'node:fs';
import path from 'node:path';
import { P, readJSON, writeJSON, nowKST } from './paths.mjs';

const STATE_FILE = path.join(P.root, 'references/scout-state.json');
const PENDING_DIR = path.join(P.root, 'references/pending');
const EVERY_N_GAMES = Number(process.env.SCOUT_EVERY || 10);

// 아직 안 판 카테고리 순서대로 로테이션한다. 다 돌면 처음부터 다시.
const CATEGORY_ROTATION = [
  '2025~2026년 바이럴 모바일/웹 미니게임 (틱톡·유튜브쇼츠에서 화제인 것 위주)',
  '보드·카드게임의 디지털 각색 (우노, 도미노, 마작, 체스류 — 규칙 기반 대전)',
  'Awwwards·CSS Design Awards 수상작 (순수 인터랙션·모션 디자인, 게임 아님)',
  '초등학생이 실제로 많이 하는 로블록스/마인크래프트 미니게임 장르',
  '리듬·타이밍 게임의 최신 변형 (모바일 리듬게임, 타이밍바 챌린지)',
  '물리 기반 파괴·건설 게임 (Getting Over It 류, 타워 무너뜨리기)',
  '카드 덱빌딩·로그라이크 최신작 (Balatro 이후 유행한 것들)',
  '숫자/타일 퍼즐의 새로운 변형 (2048 이후 나온 머지·타일 게임들)',
  '게임 사운드 디자인·게임필(juice) 전문 레퍼런스 사이트',
  '한국 초등학생 사이 인기 있는 웹/앱 게임 (실사용 데이터 기반)',
];

function state() {
  return readJSON(STATE_FILE, { last_scouted_count: 0, rotation_idx: 0, history: [] });
}

const cmd = process.argv[2];

if (cmd === 'check') {
  const queue = readJSON(P.queue, { produced: [] });
  const produced = (queue.produced || []).length;
  const s = state();
  const due = produced > 0 && produced - s.last_scouted_count >= EVERY_N_GAMES;
  console.log(due ? 'due' : 'not-due', `produced=${produced} last=${s.last_scouted_count} every=${EVERY_N_GAMES}`);
  process.exit(due ? 0 : 1);
}

if (cmd === 'prepare') {
  const s = state();
  const category = CATEGORY_ROTATION[s.rotation_idx % CATEGORY_ROTATION.length];
  const ref = readJSON(P.references, { mechanics: [], sites: [], visual_refs: [] });
  const already_have = [
    ...(ref.mechanics || []).map((m) => m.name),
    ...(ref.sites || []).map((s2) => s2.name),
    ...(ref.visual_refs || []).map((v) => v.name),
  ];
  fs.mkdirSync(P.work, { recursive: true });
  writeJSON(path.join(P.work, 'scout-focus.json'), { category, already_have });
  console.log(`포커스: ${category}`);
  console.log(`기존 항목 ${already_have.length}개 (중복 방지용으로 전달)`);
  process.exit(0);
}

if (cmd === 'ingest') {
  const candFile = path.join(P.work, 'scout-candidates.json');
  if (!fs.existsSync(candFile)) {
    console.error('scout-candidates.json 이 없습니다 — 스카우트 에이전트가 산출물을 안 남겼습니다.');
    process.exit(1);
  }
  const cand = readJSON(candFile);
  fs.mkdirSync(PENDING_DIR, { recursive: true });
  const stamp = nowKST().slice(0, 10);
  let n = 1;
  let outFile = path.join(PENDING_DIR, `${stamp}.json`);
  while (fs.existsSync(outFile)) outFile = path.join(PENDING_DIR, `${stamp}-${++n}.json`);
  writeJSON(outFile, cand);

  const queue = readJSON(P.queue, { produced: [] });
  const s = state();
  s.last_scouted_count = (queue.produced || []).length;
  s.rotation_idx = (s.rotation_idx || 0) + 1;
  s.history = s.history || [];
  s.history.push({
    at: nowKST(),
    category: cand.category,
    file: path.relative(P.root, outFile),
    counts: {
      mechanics: (cand.mechanics || []).length,
      sites: (cand.sites || []).length,
      visual_refs: (cand.visual_refs || []).length,
    },
  });
  writeJSON(STATE_FILE, s);

  const c = s.history[s.history.length - 1].counts;
  console.log(`보관 완료: ${path.relative(P.root, outFile)}`);
  console.log(`메커닉 ${c.mechanics} · 사이트 ${c.sites} · 비주얼 ${c.visual_refs}`);
  console.log('아직 game-references.json 에는 안 들어갔습니다 — 검토 후 merge-references.mjs 로 승인하세요.');
  process.exit(0);
}

console.error('사용법: node factory/lib/scout-references.mjs <check|prepare|ingest>');
process.exit(2);
