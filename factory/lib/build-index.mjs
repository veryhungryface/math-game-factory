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
  .filter((g) => g.qa?.passed !== false || process.env.INCLUDE_UNPUBLISHED === '1')
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
    u = { id: uid, order: unitById.get(uid)?.order ?? 99, title: g.unit?.title || '기타', games: [] };
    grp.units.push(u);
  }
  u.games.push(g);
}
groups.sort((a, b) => a.grade - b.grade || a.semester - b.semester);
for (const g of groups) g.units.sort((a, b) => a.order - b.order);

const esc = (s) =>
  String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

const card = (g) => `
        <a class="card" href="${esc(g.url)}" data-grade="${g.grade}" data-sem="${g.semester}" data-unit="${esc(g.unit?.id)}">
          <div class="thumb">${
            g.thumb
              ? `<img src="${esc(g.thumb)}" alt="${esc(g.title)}" loading="lazy" width="1200" height="630">`
              : `<div class="thumb-fallback">${esc(g.title.slice(0, 2))}</div>`
          }
            <span class="badge">${g.grade}-${g.semester}</span>
          </div>
          <div class="card-body">
            <h3>${esc(g.title)}</h3>
            <p class="tagline">${esc(g.tagline)}</p>
            <div class="chips">
              <span class="chip chip-unit">${esc(g.unit?.title)}</span>
              ${(g.standards || []).slice(0, 2).map((s) => `<span class="chip chip-std">${esc(s)}</span>`).join('')}
            </div>
          </div>
        </a>`;

const section = (grp) => `
      <section class="grade-block" id="g${grp.grade}s${grp.semester}">
        <h2><span class="grade-num">${grp.grade}</span>학년 ${grp.semester}학기</h2>
        ${grp.units
          .map(
            (u) => `
        <div class="unit-block">
          <h4><span class="unit-order">${u.order}단원</span> ${esc(u.title)} <span class="unit-count">${u.games.length}</span></h4>
          <div class="grid">${u.games.map(card).join('')}</div>
        </div>`
          )
          .join('')}
      </section>`;

const html = `<!doctype html>
<html lang="ko">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<title>수학 놀이터 — 초등 수학 게임 모음</title>
<meta name="description" content="2022 개정 교육과정 초등 수학 단원에 정확히 매핑된 브라우저 게임 모음. 설치 없이 바로 플레이.">
<meta property="og:title" content="수학 놀이터">
<meta property="og:description" content="교육과정에 딱 맞는 초등 수학 게임 ${games.length}종. 지금 바로 플레이.">
<meta property="og:type" content="website">
<link rel="icon" href="data:image/svg+xml,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><text y='.9em' font-size='90'>🎮</text></svg>">
<style>
  *,*::before,*::after{box-sizing:border-box}
  :root{
    --bg:#0b1020; --bg2:#121a35; --card:#182244; --line:#2a3766;
    --fg:#f2f5ff; --muted:#9aa8d4;
    --a1:#ff8a3d; --a2:#3ddc97; --a3:#5b8cff; --a4:#ff5fa2;
  }
  html,body{margin:0;padding:0}
  body{
    background:
      radial-gradient(1100px 600px at 12% -10%, #22306b 0%, transparent 60%),
      radial-gradient(900px 500px at 105% 5%, #4a1f5e 0%, transparent 55%),
      var(--bg);
    color:var(--fg);
    font-family:-apple-system,BlinkMacSystemFont,"Apple SD Gothic Neo","Pretendard","Malgun Gothic","Noto Sans KR",sans-serif;
    -webkit-font-smoothing:antialiased; min-height:100dvh;
  }
  .wrap{max-width:1180px;margin:0 auto;padding:0 20px 96px}
  .topnav{display:flex;justify-content:flex-end;align-items:center;gap:10px;padding:18px 0 0}
  .topnav a{
    display:inline-flex;align-items:center;gap:7px;min-height:44px;padding:0 17px;
    border:1px solid var(--line);border-radius:999px;background:rgba(255,255,255,.05);
    color:var(--fg);text-decoration:none;font-size:14px;font-weight:700;
    transition:border-color .18s, background .18s
  }
  .topnav a:hover,.topnav a:focus-visible{border-color:var(--a2);background:rgba(61,220,151,.12);outline:none}
  .topnav a .ico{font-size:15px}
  header{padding:44px 0 36px;text-align:center}
  .eyebrow{
    display:inline-block;font-size:12px;font-weight:800;letter-spacing:.14em;
    color:#0b1020;background:linear-gradient(90deg,var(--a2),var(--a3));
    padding:6px 14px;border-radius:999px;margin-bottom:18px
  }
  h1{
    font-size:clamp(38px,8vw,72px);line-height:1.05;margin:0 0 14px;font-weight:900;letter-spacing:-.03em;
    background:linear-gradient(100deg,#fff 10%,var(--a1) 45%,var(--a4) 75%,var(--a3) 100%);
    -webkit-background-clip:text;background-clip:text;color:transparent
  }
  .sub{color:var(--muted);font-size:clamp(14px,3.4vw,18px);margin:0 auto;max-width:44ch;line-height:1.65}
  .stats{display:flex;gap:10px;justify-content:center;flex-wrap:wrap;margin-top:26px}
  .stat{background:rgba(255,255,255,.05);border:1px solid var(--line);border-radius:14px;padding:10px 18px}
  .stat b{display:block;font-size:22px;font-weight:900;color:var(--a2)}
  .stat span{font-size:11px;color:var(--muted);letter-spacing:.06em}
  .grade-block{margin-top:64px}
  .grade-block h2{
    font-size:clamp(22px,5vw,30px);font-weight:900;margin:0 0 22px;
    display:flex;align-items:center;gap:12px
  }
  .grade-num{
    display:grid;place-items:center;width:42px;height:42px;border-radius:13px;
    background:linear-gradient(140deg,var(--a3),var(--a4));color:#fff;font-size:20px;flex:none
  }
  .unit-block{margin:0 0 40px}
  .unit-block h4{
    font-size:15px;font-weight:700;color:var(--muted);margin:0 0 14px;
    display:flex;align-items:center;gap:10px;padding-bottom:10px;border-bottom:1px solid var(--line)
  }
  .unit-order{color:var(--a1);font-weight:900}
  .unit-count{
    margin-left:auto;background:rgba(255,255,255,.07);border-radius:999px;
    padding:2px 10px;font-size:12px;color:var(--fg)
  }
  .grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(268px,1fr));gap:18px}
  .card{
    display:flex;flex-direction:column;background:var(--card);border:1px solid var(--line);
    border-radius:20px;overflow:hidden;text-decoration:none;color:inherit;
    transition:transform .18s cubic-bezier(.2,.9,.3,1.2), box-shadow .18s, border-color .18s
  }
  .card:hover,.card:focus-visible{
    transform:translateY(-6px);border-color:var(--a3);
    box-shadow:0 18px 40px -18px rgba(91,140,255,.7);outline:none
  }
  .thumb{position:relative;aspect-ratio:1200/630;background:#0e1530;overflow:hidden}
  .thumb img{width:100%;height:100%;object-fit:cover;display:block}
  .thumb-fallback{
    width:100%;height:100%;display:grid;place-items:center;font-size:46px;font-weight:900;
    background:linear-gradient(140deg,var(--a3),var(--a4));color:#fff
  }
  .badge{
    position:absolute;top:10px;left:10px;background:rgba(11,16,32,.82);backdrop-filter:blur(8px);
    border:1px solid var(--line);border-radius:9px;padding:4px 9px;font-size:11px;font-weight:800;letter-spacing:.03em
  }
  .card-body{padding:15px 16px 17px;display:flex;flex-direction:column;gap:7px;flex:1}
  .card-body h3{margin:0;font-size:18px;font-weight:800;letter-spacing:-.01em}
  .tagline{margin:0;font-size:13px;color:var(--muted);line-height:1.5;flex:1}
  .chips{display:flex;gap:6px;flex-wrap:wrap;margin-top:4px}
  .chip{font-size:10.5px;padding:3px 8px;border-radius:7px;border:1px solid var(--line);color:var(--muted)}
  .chip-unit{background:rgba(61,220,151,.12);border-color:rgba(61,220,151,.35);color:var(--a2)}
  .chip-std{font-variant-numeric:tabular-nums}
  .empty{
    text-align:center;padding:80px 20px;color:var(--muted);
    border:1px dashed var(--line);border-radius:20px;margin-top:48px
  }
  footer{margin-top:80px;padding-top:26px;border-top:1px solid var(--line);color:var(--muted);font-size:12.5px;text-align:center;line-height:1.8}
  footer a{display:inline-flex;align-items:center;min-height:44px;padding:0 6px}
  @media (max-width:520px){
    .grid{grid-template-columns:1fr;gap:14px}
    header{padding:30px 0 24px}
    .topnav{justify-content:center}
  }
  @media (prefers-reduced-motion:reduce){.card{transition:none}}
</style>
</head>
<body>
<div class="wrap">
  <nav class="topnav">
    <a href="/about/"><span class="ico">🛠️</span> 제작 과정</a>
  </nav>
  <header>
    <div class="eyebrow">2022 개정 교육과정</div>
    <h1>수학 놀이터</h1>
    <p class="sub">교과서 단원에 정확히 맞춘 초등 수학 게임.<br>설치도 로그인도 없이, 링크만 열면 바로 시작.</p>
    <div class="stats">
      <div class="stat"><b>${games.length}</b><span>게임</span></div>
      <div class="stat"><b>${new Set(games.map((g) => g.unit?.id)).size}</b><span>단원</span></div>
      <div class="stat"><b>${new Set(games.flatMap((g) => g.standards || [])).size}</b><span>성취기준</span></div>
    </div>
  </header>

  ${groups.length ? groups.map(section).join('') : '<div class="empty">첫 번째 게임을 제작하는 중입니다. 곧 만나요! 🎮</div>'}

  <footer>
    2022 개정 교육과정 초등 수학 기반 · 마지막 업데이트 ${esc(catalog.generated_at.slice(0, 16).replace('T', ' '))}<br>
    게임 메커닉은 참고하되 상표·캐릭터·에셋은 모두 오리지널 제작입니다.<br>
    <a href="/about/" style="color:var(--a3)">이 게임들이 만들어지는 과정 보기 →</a>
  </footer>
</div>
</body>
</html>
`;

fs.writeFileSync(P.hub, html);
console.log(`허브 생성 완료 — 게임 ${games.length}종, 단원 ${new Set(games.map((g) => g.unit?.id)).size}개`);
console.log(`  ${P.hub}`);
console.log(`  ${P.catalog}`);
