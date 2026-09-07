#!/usr/bin/env node
/**
 * public/g/*​/meta.json 을 훑어 catalog.json 과 허브 index.html 을 생성한다.
 *   node factory/lib/build-index.mjs
 */
import fs from 'node:fs';
import path from 'node:path';
import { P, readJSON, writeJSON, listGames, readMeta, nowKST } from './paths.mjs';

const curriculum = readJSON(P.curriculum, { units: [], standards: [] });
const unitById = new Map((curriculum.units || []).map((u) => [u.id, u]));

const games = listGames()
  .map((slug) => {
    const m = readMeta(slug);
    if (!m) return null;
    const dir = path.join(P.games, slug);
    return {
      ...m,
      slug,
      url: `/g/${slug}/`,
      thumb: fs.existsSync(path.join(dir, 'thumb.png')) ? `/g/${slug}/thumb.png` : null,
      thumb_square: fs.existsSync(path.join(dir, 'square.png')) ? `/g/${slug}/square.png` : null,
    };
  })
  .filter(Boolean)
  .filter((g) => g.qa?.passed !== false || g.qa?.manual_release?.approved === true || process.env.INCLUDE_UNPUBLISHED === '1')
  .sort((a, b) => String(b.created_at || '').localeCompare(String(a.created_at || '')));

const catalog = {
  generated_at: nowKST(),
  curriculum_revision: curriculum.meta?.revision || '2022 개정',
  count: games.length,
  games,
};
writeJSON(P.catalog, catalog);

// ── 학년/학기/단원 그룹핑 ────────────────────────────────
const groups = [];
for (const g of games) {
  const key = `${g.grade}-${g.semester}`;
  let grp = groups.find((x) => x.key === key);
  if (!grp) {
    grp = { key, grade: g.grade, semester: g.semester, units: [] };
    groups.push(grp);
  }
  const uid = g.unit?.id || 'etc';
  let u = grp.units.find((x) => x.id === uid);
  if (!u) {
    const curriculumUnit = unitById.get(uid);
    u = {
      id: uid,
      order: curriculumUnit?.order ?? g.unit?.order ?? 99,
      title: curriculumUnit?.title || g.unit?.title || '기타',
      games: [],
    };
    grp.units.push(u);
  }
  u.games.push(g);
}
groups.sort((a, b) => a.grade - b.grade || a.semester - b.semester);
for (const g of groups) g.units.sort((a, b) => a.order - b.order);

// 단원마다 새 행을 만들지 않고, 학기·단원 순서를 유지한 채 학년별 격자로 이어 붙인다.
const grades = [];
for (const grp of groups) {
  let grade = grades.find((x) => x.grade === grp.grade);
  if (!grade) {
    grade = { grade: grp.grade, semesters: [], count: 0 };
    grades.push(grade);
  }
  grade.semesters.push(grp);
  grade.count += grp.units.reduce((sum, u) => sum + u.games.length, 0);
}

const esc = (s) =>
  String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

const card = (g, u, semesterAnchor = '') => `
        <a class="card"${semesterAnchor ? ` id="${esc(semesterAnchor)}"` : ''} href="${esc(g.url)}" data-grade="${esc(g.grade)}" data-sem="${esc(g.semester)}" data-unit="${esc(g.unit?.id)}" data-unit-order="${esc(u.order)}">
          <div class="card-unit">
            <span class="unit-order">${esc(g.semester)}학기 · ${u.order === 99 ? '단원 미지정' : `${esc(u.order)}단원`}</span>
            <span class="unit-title">${esc(u.title)}</span>
          </div>
          <div class="thumb">${
            g.thumb
              ? `<img src="${esc(g.thumb)}" alt="" loading="lazy" width="1200" height="630">`
              : `<div class="thumb-fallback" aria-hidden="true">${esc(String(g.title || '').slice(0, 2))}</div>`
          }
          </div>
          <div class="card-body">
            <h3>${esc(g.title)}</h3>
            <p class="tagline">${esc(g.tagline)}</p>
          </div>
        </a>`;

const section = (grade) => `
      <section class="grade-block" id="g${esc(grade.grade)}" aria-labelledby="grade-${esc(grade.grade)}">
        <div class="grade-heading">
          <h2 id="grade-${esc(grade.grade)}"><span class="grade-num">${esc(grade.grade)}</span>학년 <span class="grade-count">${grade.count}개 게임</span></h2>
          <span class="sort-label">학기 · 단원 순</span>
        </div>
        <div class="grid">${grade.semesters.map((grp) => grp.units.map((u, unitIndex) => u.games.map((g, gameIndex) => card(g, u, unitIndex === 0 && gameIndex === 0 ? `g${grp.grade}s${grp.semester}` : '')).join('')).join('')).join('')}
        </div>
      </section>`;

const html = `<!doctype html>
<html lang="ko">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<title>이것은 게임인가 공부인가</title>
<meta name="description" content="2022 개정 교육과정 초등 수학 단원에 정확히 매핑된 브라우저 게임 모음. 설치 없이 바로 플레이.">
<meta property="og:title" content="이것은 게임인가 공부인가">
<meta property="og:description" content="교육과정에 딱 맞는 초등 수학 게임 ${games.length}종. 지금 바로 플레이.">
<meta property="og:type" content="website">
<link rel="icon" href="data:image/svg+xml,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><text y='.9em' font-size='90'>🎮</text></svg>">
<style>
  *,*::before,*::after{box-sizing:border-box}
  :root{
    --bg:#0e1220; --card:#181f31; --line:#30394f;
    --fg:#f5f6fb; --muted:#a9b4cc; --accent:#9ee8cc; --violet:#b7acff;
  }
  html,body{margin:0;padding:0}
  html{scroll-behavior:smooth;scroll-padding-top:24px}
  body{
    background:radial-gradient(ellipse 900px 480px at 80% -180px,#30335a 0%,transparent 80%),var(--bg);
    color:var(--fg);
    font-family:-apple-system,BlinkMacSystemFont,"Apple SD Gothic Neo","Malgun Gothic",sans-serif;
    -webkit-font-smoothing:antialiased;min-height:100dvh;
  }
  a{-webkit-tap-highlight-color:transparent;touch-action:manipulation}
  a:focus-visible{outline:3px solid var(--accent);outline-offset:4px}
  .wrap{max-width:1280px;margin:0 auto;padding:0 32px 40px}
  .topnav{display:flex;justify-content:space-between;align-items:center;gap:12px;min-height:72px;border-bottom:1px solid var(--line)}
  .site-label{display:flex;align-items:center;gap:10px;font-size:13px;font-weight:700;color:var(--muted)}
  .site-mark{display:grid;place-items:center;width:28px;height:28px;border-radius:9px;background:var(--accent);color:var(--bg);font-size:23px;font-weight:900;line-height:1}
  .topnav a{display:inline-flex;align-items:center;gap:9px;min-height:44px;padding:0 4px;color:var(--fg);text-decoration:none;font-size:13px;font-weight:700}
  .topnav a:hover{color:var(--accent)}
  header{padding:32px 0 26px}
  .eyebrow{font-size:11px;font-weight:800;letter-spacing:.09em;color:var(--accent);margin:0 0 12px}
  h1{font-size:clamp(30px,4.3vw,54px);line-height:1.2;margin:0 0 15px;font-weight:850;letter-spacing:-.055em;word-break:keep-all}
  h1 span{display:inline-block;color:var(--accent)}
  .intro-bottom{display:flex;align-items:center;justify-content:space-between;gap:20px}
  .sub{color:var(--muted);font-size:14px;margin:0;line-height:1.7;word-break:keep-all}
  .stats{display:flex;gap:8px;flex:none}
  .stat{display:flex;gap:5px;align-items:baseline;background:#1d2637;border:1px solid var(--line);border-radius:9px;padding:9px 12px;font-size:12px;color:var(--muted);white-space:nowrap}
  .stat b{font-size:17px;font-weight:800;color:var(--fg)}
  .grade-nav{display:flex;align-items:center;flex-wrap:wrap;gap:8px;padding:16px 0;border-top:1px solid var(--line);border-bottom:1px solid var(--line)}
  .nav-label{margin-right:10px;font-size:12px;color:var(--muted)}
  .grade-nav a{display:inline-flex;align-items:center;justify-content:center;gap:8px;min-height:44px;padding:0 18px;border:1px solid var(--line);border-radius:10px;background:#1a2234;text-decoration:none;color:var(--fg);font-size:14px;font-weight:750}
  .grade-nav a:hover{border-color:var(--accent);background:#233b3c}
  .grade-nav a span{font-size:11px;color:var(--muted);font-weight:500}
  .grade-block{margin-top:34px;scroll-margin-top:24px}
  .grade-heading{display:flex;align-items:center;justify-content:space-between;gap:12px;margin-bottom:16px}
  .grade-heading h2{display:flex;align-items:center;gap:8px;margin:0;font-size:24px;font-weight:800;letter-spacing:-.04em}
  .grade-num{display:grid;place-items:center;width:38px;height:38px;border:1px solid #4e6264;border-radius:11px;background:#233b3c;color:var(--accent);font-size:23px;font-weight:850}
  .grade-count{font-size:12px;font-weight:500;letter-spacing:0;color:var(--muted);margin-left:6px}
  .sort-label{font-size:11px;color:var(--muted)}
  .grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px}
  .card{display:flex;flex-direction:column;min-width:0;background:var(--card);border:1px solid var(--line);border-radius:14px;overflow:hidden;text-decoration:none;color:inherit;scroll-margin-top:24px;transition:transform .18s,border-color .18s,box-shadow .18s}
  .card:hover{transform:translateY(-3px);border-color:#7caaab;box-shadow:0 12px 28px #0003}
  .card:focus-visible{border-color:var(--accent)}
  .card-unit{display:flex;flex-direction:column;gap:6px;min-height:70px;padding:13px 15px 12px;background:#1c2638}
  .unit-order{font-size:11px;font-weight:750;color:var(--accent);line-height:1.4}
  .unit-title{font-size:14px;font-weight:700;line-height:1.4;word-break:keep-all;overflow-wrap:anywhere}
  .thumb{aspect-ratio:1200/630;background:#10182a;overflow:hidden}
  .thumb img{width:100%;height:100%;object-fit:cover;display:block}
  .thumb-fallback{width:100%;height:100%;display:grid;place-items:center;font-size:46px;font-weight:900;background:linear-gradient(140deg,#425a79,#6a568c);color:#fff}
  .card-body{padding:13px 15px 15px;display:flex;flex-direction:column;gap:6px;flex:1}
  .card-body h3{margin:0;font-size:17px;font-weight:800;letter-spacing:-.025em;line-height:1.4;word-break:keep-all;overflow-wrap:anywhere}
  .tagline{margin:0;font-size:12px;color:var(--muted);line-height:1.55;word-break:keep-all;overflow-wrap:anywhere}
  .empty{text-align:center;padding:70px 20px;color:var(--muted);border:1px dashed var(--line);border-radius:14px;margin-top:32px}
  footer{margin-top:48px;padding-top:22px;border-top:1px solid var(--line);color:var(--muted);font-size:11px;line-height:1.8}
  footer a{display:inline-flex;align-items:center;min-height:44px;color:var(--accent);text-decoration:none}
  footer a:hover{text-decoration:underline}
  @media (max-width:1050px){
    .grid{grid-template-columns:repeat(3,minmax(0,1fr))}
    .intro-bottom{align-items:flex-start}
  }
  @media (max-width:700px){
    .wrap{padding:0 18px 28px}
    .topnav{min-height:62px}
    header{padding:26px 0 22px}
    h1{font-size:36px;max-width:520px}
    .intro-bottom{display:block}
    .sub{font-size:13px}
    .stats{margin-top:14px}
    .stat{padding:6px 10px;font-size:11px}
    .stat b{font-size:15px}
    .grade-nav{gap:7px;padding:13px 0}
    .nav-label{display:none}
    .grade-nav a{flex:1;padding:0 10px;font-size:13px;gap:5px}
    .grade-nav a span{font-size:10px}
    .grade-block{margin-top:28px}
    .grade-heading{margin-bottom:13px}
    .grade-heading h2{font-size:21px}
    .grade-num{width:34px;height:34px;font-size:21px;border-radius:10px}
    .grade-count{font-size:11px;margin-left:3px}
    .sort-label{font-size:10px}
    .grid{grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}
    .card{border-radius:12px}
    .card-unit{min-height:72px;padding:11px 11px 10px;gap:5px}
    .unit-order{font-size:10px}
    .unit-title{font-size:12px;line-height:1.45}
    .card-body{padding:11px;gap:5px}
    .card-body h3{font-size:14px;line-height:1.4}
    .tagline{font-size:11px;line-height:1.5}
    footer{margin-top:36px}
  }
  @media (max-width:360px){
    .wrap{padding-right:12px;padding-left:12px}
    h1{font-size:31px}
    .site-label{font-size:11px;gap:7px}
    .topnav a{font-size:12px}
    .grid{gap:10px}
    .grade-nav a{padding:0 6px}
    .grade-nav a span{display:none}
    .card-unit{min-height:80px;padding-right:10px;padding-left:10px}
    .card-body{padding:10px}
    .card-body h3{font-size:13px}
  }
  @media (prefers-reduced-motion:reduce){html{scroll-behavior:auto}.card{transition:none}}
</style>
</head>
<body>
<div class="wrap">
  <nav class="topnav" aria-label="사이트 안내">
    <span class="site-label"><span class="site-mark" aria-hidden="true">＋</span>초등 수학 게임 모음</span>
    <a href="/about/">제작 과정 <span aria-hidden="true">↗</span></a>
  </nav>
  <header>
    <p class="eyebrow">2022 개정 교육과정</p>
    <h1>이것은 게임인가 <span>공부인가</span></h1>
    <div class="intro-bottom">
      <p class="sub">놀다 보면 수학이 된다. 배우고 싶은 단원부터 골라 보세요.<br>설치도 로그인도 없이 바로 시작해요.</p>
      <div class="stats" aria-label="수록 게임 현황">
        <span class="stat">게임 <b>${games.length}</b>개</span>
        <span class="stat">단원 <b>${new Set(games.map((g) => g.unit?.id)).size}</b>개</span>
      </div>
    </div>
  </header>

  ${grades.length ? `<nav class="grade-nav" aria-label="학년 바로가기">
    <span class="nav-label">학년 바로가기</span>
    ${grades.map((grade) => `<a href="#g${esc(grade.grade)}">${esc(grade.grade)}학년 <span>${grade.count}</span></a>`).join('\n    ')}
  </nav>` : ''}

  <main aria-label="학년별 수학 게임">
  ${grades.length ? grades.map(section).join('') : '<div class="empty">첫 번째 게임을 제작하는 중입니다. 곧 만나요! 🎮</div>'}
  </main>

  <footer>
    2022 개정 교육과정 초등 수학 기반 · 마지막 업데이트 ${esc(catalog.generated_at.slice(0, 16).replace('T', ' '))}<br>
    게임 메커닉은 참고하되 상표·캐릭터·에셋은 모두 오리지널 제작입니다.<br>
    <a href="/about/">이 게임들이 만들어지는 과정 보기 →</a>
  </footer>
</div>
</body>
</html>
`;

fs.writeFileSync(P.hub, html);
console.log(`허브 생성 완료 — 게임 ${games.length}종, 단원 ${new Set(games.map((g) => g.unit?.id)).size}개`);
console.log(`  ${P.hub}`);
console.log(`  ${P.catalog}`);
