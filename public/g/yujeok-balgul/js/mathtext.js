// 팩 문자열의 수식 표기 → 조각 배열. 인식하는 것은 {frac:a/b} 와 <sup>…</sup> 둘뿐이다.
// 나머지 「{」「<」 는 문장 그대로다(예: 「a<b일 때」, 「{b−(4a−2)}」).
const TOKEN = /\{frac:(-?\d+)\/(\d+)\}|<sup>(.*?)<\/sup>/g;

export function parseMath(str) {
  const s = String(str ?? '');
  const out = [];
  let last = 0;
  TOKEN.lastIndex = 0;
  let m;
  while ((m = TOKEN.exec(s))) {
    if (m.index > last) out.push({ t: 'text', s: s.slice(last, m.index) });
    if (m[1] !== undefined) {
      let n = m[1];
      if (n.startsWith('-')) {
        out.push({ t: 'text', s: '−' });
        n = n.slice(1);
      }
      out.push({ t: 'frac', n, d: m[2] });
    } else out.push({ t: 'sup', s: m[3] });
    last = TOKEN.lastIndex;
  }
  if (last < s.length) out.push({ t: 'text', s: s.slice(last) });
  return out;
}

const esc = (s) => s.replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);

export function mathHTML(str) {
  return parseMath(str)
    .map((p) =>
      p.t === 'text' ? esc(p.s) : p.t === 'sup' ? `<sup>${esc(p.s)}</sup>` : `<span class="fr"><span>${esc(p.n)}</span><span>${esc(p.d)}</span></span>`
    )
    .join('');
}

/** 화면 낭독·검산용 평문: {frac:3/4} → 3/4, <sup>2</sup> → ^2 */
export function mathPlain(str) {
  return parseMath(str)
    .map((p) => (p.t === 'text' ? p.s : p.t === 'sup' ? `^${p.s}` : `${p.n}/${p.d}`))
    .join('');
}

// ───────────────── 캔버스(석판)용 배치 ─────────────────
const FONT = '"Apple SD Gothic Neo","Malgun Gothic",-apple-system,"Noto Sans KR",sans-serif';
const font = (px, w = 900) => `${w} ${Math.round(px)}px ${FONT}`;

function measureSeg(ctx, p, px) {
  if (p.t === 'text') {
    ctx.font = font(px);
    return { w: ctx.measureText(p.s).width, up: px * 0.78, dn: px * 0.26 };
  }
  if (p.t === 'sup') {
    ctx.font = font(px * 0.62);
    return { w: ctx.measureText(p.s).width + px * 0.04, up: px * 1.02, dn: 0 };
  }
  ctx.font = font(px * 0.74);
  const w = Math.max(ctx.measureText(p.n).width, ctx.measureText(p.d).width) + px * 0.22;
  return { w: w + px * 0.12, up: px * 0.98, dn: px * 0.86 };
}

function lineBox(ctx, segs, px) {
  let w = 0;
  let up = px * 0.78;
  let dn = px * 0.26;
  for (const p of segs) {
    const m = measureSeg(ctx, p, px);
    w += m.w;
    up = Math.max(up, m.up);
    dn = Math.max(dn, m.dn);
  }
  return { w, up, dn, h: up + dn };
}

/** 텍스트 조각을 공백/쉼표 위치에서 두 줄로 나누는 후보들 */
function splitCandidates(segs) {
  const out = [];
  segs.forEach((p, i) => {
    if (p.t !== 'text') return;
    for (let k = 1; k < p.s.length; k++) {
      const ch = p.s[k - 1];
      if (ch === ' ' || ch === ',') {
        const a = [...segs.slice(0, i), { t: 'text', s: p.s.slice(0, k).trimEnd() }].filter((x) => x.t !== 'text' || x.s);
        const b = [{ t: 'text', s: p.s.slice(k).trimStart() }, ...segs.slice(i + 1)].filter((x) => x.t !== 'text' || x.s);
        if (a.length && b.length) out.push([a, b]);
      }
    }
  });
  return out;
}

/** 상자(w×h) 안에 들어가는 가장 큰 배치를 고른다. */
export function layoutMath(ctx, str, boxW, boxH, maxPx, minPx) {
  const segs = parseMath(str);
  for (let px = maxPx; px >= minPx; px -= 3) {
    const b = lineBox(ctx, segs, px);
    if (b.w <= boxW && b.h <= boxH) return { px, lines: [segs], boxes: [b] };
  }
  const cands = splitCandidates(segs);
  let best = null;
  for (const [a, b] of cands) {
    const wa = lineBox(ctx, a, 100).w;
    const wb = lineBox(ctx, b, 100).w;
    const score = Math.max(wa, wb);
    if (!best || score < best.score) best = { a, b, score };
  }
  if (best) {
    for (let px = Math.min(maxPx, minPx + 30); px >= minPx * 0.7; px -= 2) {
      const ba = lineBox(ctx, best.a, px);
      const bb = lineBox(ctx, best.b, px);
      if (Math.max(ba.w, bb.w) <= boxW && ba.h + bb.h + px * 0.1 <= boxH) return { px, lines: [best.a, best.b], boxes: [ba, bb] };
    }
  }
  // 최후: 한 줄을 가로 압축
  let px = minPx * 0.7;
  const b = lineBox(ctx, segs, px);
  return { px, lines: [segs], boxes: [b], squeeze: Math.min(1, boxW / b.w) };
}

/** 배치를 (cx, cy) 중심으로 그린다. paint(ctx, kind, text, x, y) 콜백 대신 fill 스타일을 바꿔 여러 번 그려 음각을 낸다. */
export function drawMathLayout(ctx, L, cx, cy, passes) {
  const gap = L.px * 0.1;
  const total = L.boxes.reduce((a, b) => a + b.h, 0) + gap * (L.lines.length - 1);
  let y = cy - total / 2;
  L.lines.forEach((segs, li) => {
    const box = L.boxes[li];
    const base = y + box.up;
    const sq = L.squeeze || 1;
    for (const pass of passes) {
      ctx.save();
      ctx.translate(cx, 0);
      ctx.scale(sq, 1);
      ctx.translate(-cx, 0);
      let x = cx - box.w / 2;
      ctx.fillStyle = pass.color;
      ctx.textBaseline = 'alphabetic';
      ctx.textAlign = 'left';
      for (const p of segs) {
        const m = measureSeg(ctx, p, L.px);
        const ox = pass.dx;
        const oy = pass.dy;
        if (p.t === 'text') {
          ctx.font = font(L.px);
          ctx.fillText(p.s, x + ox, base + oy);
        } else if (p.t === 'sup') {
          ctx.font = font(L.px * 0.62);
          ctx.fillText(p.s, x + ox + L.px * 0.02, base - L.px * 0.42 + oy);
        } else {
          const fx = x + m.w / 2 + ox;
          const midY = base - L.px * 0.3 + oy;
          ctx.font = font(L.px * 0.74);
          ctx.textAlign = 'center';
          ctx.fillText(p.n, fx, midY - L.px * 0.14);
          ctx.textBaseline = 'top';
          ctx.fillText(p.d, fx, midY + L.px * 0.12);
          ctx.textBaseline = 'alphabetic';
          ctx.textAlign = 'left';
          ctx.font = font(L.px * 0.74);
          const ink = Math.max(ctx.measureText(p.n).width, ctx.measureText(p.d).width) + L.px * 0.16;
          const th = Math.max(3, L.px * 0.075);
          ctx.fillRect(fx - ink / 2, midY - th / 2, ink, th);
        }
        x += m.w;
      }
      ctx.restore();
    }
    y += box.h + gap;
  });
}
