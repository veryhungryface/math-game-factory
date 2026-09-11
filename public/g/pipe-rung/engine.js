/* 파이프 잇기 (pipe-rung) — 순수 로직 엔진
 *
 * 좌표는 전부 정수 서브유닛이다. 1 cm = 20 서브유닛(SUB).
 * 파이프 방향은 길이가 정수인 원시 벡터만 쓴다 — (1,0) (0,1) (4,3) (3,4) (4,-3) (3,-4).
 * 그래서 「1 cm 만큼 법선으로 민 점」이 항상 정수 좌표가 되고, 두 평행 파이프 사이의
 * 거리는 부동소수점이 아니라 **정수 cm** 로 확정된다. 화면에 뜨는 cm 는 획에서 잰 값이
 * 아니라 이 정수 정의값이다.
 *
 * ── 교과서 대조 (아이스크림미디어 2022개정 4-2 4단원 「사각형」 1~4차시) ──
 *  · 발문은 익힘책 지시형 「~해 보세요」로 통일한다. 청유형(~해 봅시다)과 섞지 않는다.
 *  · 수선은 어느 직선에 대한 수선인지 밝힌다 → 「직선 ㄴㄷ에 대한 수선」.
 *  · 평행선 사이의 거리는 반드시 수선의 길이로 잰다. 비스듬히 이은 선분은 거리가 아니다.
 *  · ★포함관계★ 사다리꼴은 「평행한 변이 한 쌍이라도 있는 사각형」이다. 한 쌍'만'으로
 *    좁혀 정의하지 않는다. 직사각형·정사각형도 사다리꼴이고 평행사변형이다.
 *    배타적 분류로 채점하면 교과서와 어긋난다.
 *  · 어림(올림/버림/반올림) 발문은 이 단원에 없다. 생성하지 않는다.
 *  · 꼭짓점·점 이름은 ㄱㄴㄷㄹ, 길이는 「4 cm」처럼 숫자와 단위를 띄어 쓴다.
 */
(function (root) {
  'use strict';

  var SUB = 20;                 // 1 cm
  var GH = 24 * SUB;            // 놀이판 높이 24 cm
  var SIN6 = 0.10452846326765347;   // sin 6° — 수선/평행 허용오차 ±6°
  var EPS_END = 20;             // 획 끝점이 파이프에서 벗어나도 되는 한계 1.0 cm
  var EPS_POINT = 20;           // 획 시작점이 점 ㄱ 에서 벗어나도 되는 한계 1.0 cm
  var EPS_SPAN = 16;            // 실제로 건넌 폭이 거리와 어긋나도 되는 한계 0.8 cm
  var MIN_LEN = 16;             // 이보다 짧은 획은 획으로 치지 않는다 0.8 cm

  // 길이가 정수인 방향 벡터만 쓴다 (화면 y 는 아래로 증가).
  var DIRS = [
    { d: [1, 0], L: 1, cue: '파이프가 가로로 곧게 뻗어 있어요.' },
    { d: [0, 1], L: 1, cue: '파이프가 세로로 곧게 서 있어요.' },
    { d: [4, 3], L: 5, cue: '파이프가 오른쪽 아래로 조금 기울어 있어요.' },
    { d: [3, 4], L: 5, cue: '파이프가 오른쪽 아래로 많이 기울어 있어요.' },
    { d: [4, -3], L: 5, cue: '파이프가 오른쪽 위로 조금 기울어 있어요.' },
    { d: [3, -4], L: 5, cue: '파이프가 오른쪽 위로 많이 기울어 있어요.' }
  ];

  // ── 기본 벡터 도구 ────────────────────────────────────────────────
  function sub(a, b) { return { x: a.x - b.x, y: a.y - b.y }; }
  function len(v) { return Math.sqrt(v.x * v.x + v.y * v.y); }
  function dotD(d, v) { return d[0] * v.x + d[1] * v.y; }
  function crossD(d, v) { return d[0] * v.y - d[1] * v.x; }

  // 1 cm 만큼 법선으로 민 정수 벡터. 위 6개 방향에서 항상 정수다.
  function normStep(D) {
    return [(SUB * -D.d[1]) / D.L, (SUB * D.d[0]) / D.L];
  }
  function unitNormal(D) { return { x: -D.d[1] / D.L, y: D.d[0] / D.L }; }

  function mkLine(dirIdx, offCm, base, label) {
    var D = DIRS[dirIdx], s = normStep(D);
    return {
      dirIdx: dirIdx, d: D.d, L: D.L, offCm: offCm, label: label || '',
      p: { x: base.x + s[0] * offCm, y: base.y + s[1] * offCm }
    };
  }
  function lineDist(line, q) {
    return Math.abs(crossD(line.d, sub(q, line.p))) / line.L;
  }
  // 두 방향이 평행한가 (±6°)
  function parallelDirs(d1, L1, d2, L2) {
    return Math.abs(d1[0] * d2[1] - d1[1] * d2[0]) <= SIN6 * L1 * L2 + 1e-9;
  }
  function perpToDir(d, L, v) {
    var lv = len(v);
    if (lv < MIN_LEN) return false;
    return Math.abs(dotD(d, v)) <= SIN6 * L * lv + 1e-9;
  }
  function parallelVecs(v1, v2) {
    var l1 = len(v1), l2 = len(v2);
    if (l1 < MIN_LEN || l2 < MIN_LEN) return false;
    return Math.abs(v1.x * v2.y - v1.y * v2.x) <= SIN6 * l1 * l2 + 1e-9;
  }
  function segCross(a1, a2, b1, b2) {
    function s(p, q, r) { return Math.sign((q.x - p.x) * (r.y - p.y) - (q.y - p.y) * (r.x - p.x)); }
    var d1 = s(a1, a2, b1), d2 = s(a1, a2, b2), d3 = s(b1, b2, a1), d4 = s(b1, b2, a2);
    return d1 * d2 < 0 && d3 * d4 < 0;
  }

  // ── 문제 만들기 ──────────────────────────────────────────────────
  // 「제약 만족 풀에서 샘플링」: 방향·거리·위치를 먼저 고르고 좌표를 역으로 만든다.
  // 무작위로 뽑아 사후 필터링하지 않는다.

  // 「위 파이프와 평행하지 않은 아래 파이프」 — 파이프 ㄱㄴ 방향을 26.57° 돌린 방향.
  // 회전이라 어느 기울기에서 시작해도 배치가 놀이판 안에 똑같이 들어온다.
  function tiltedDir(D) {
    var dx = D.d[0], dy = D.d[1];
    return { d: [2 * dx + dy, -dx + 2 * dy], L: D.L * Math.sqrt(5), cue: '' };
  }

  // 파이프를 따라 얼마나 옮겼는지를 **화면에서 실제로 움직인 방향**으로 말한다.
  // (세로 파이프에서 along 을 「왼쪽/오른쪽」이라고 하면 화면과 어긋난다)
  function alongCue(D, along) {
    if (!along) return '점 ㄱ은 파이프의 가운데 곁에 있어요.';
    var dx = (SUB * D.d[0] / D.L) * along, dy = (SUB * D.d[1] / D.L) * along;
    if (Math.abs(dx) >= Math.abs(dy)) return dx > 0 ? '점 ㄱ은 파이프의 오른쪽 부분 곁에 있어요.' : '점 ㄱ은 파이프의 왼쪽 부분 곁에 있어요.';
    return dy > 0 ? '점 ㄱ은 파이프의 아래쪽 부분 곁에 있어요.' : '점 ㄱ은 파이프의 위쪽 부분 곁에 있어요.';
  }
  var GAP_TASKS = [
    '곧은 막대로 이어 두 파이프 사이의 거리를 재 보세요.',
    '직선 ㄱㄴ에 대한 수선을 그어 두 파이프 사이의 거리를 구해 보세요.',
    '두 파이프 사이의 거리가 몇 cm인지 곧은 막대로 재 보세요.'
  ];

  function sideCue(D, side) {
    var s = normStep(D);
    var dx = s[0] * side, dy = s[1] * side;
    if (dy > 0) return '점 ㄱ은 파이프보다 아래쪽에 있어요.';
    if (dy < 0) return '점 ㄱ은 파이프보다 위쪽에 있어요.';
    return dx > 0 ? '점 ㄱ은 파이프보다 오른쪽에 있어요.' : '점 ㄱ은 파이프보다 왼쪽에 있어요.';
  }

  function slideBase(D, alongCm, base) {
    return { x: base.x + (SUB * D.d[0] / D.L) * alongCm, y: base.y + (SUB * D.d[1] / D.L) * alongCm };
  }
  // 파이프 뭉치가 놀이판 한가운데에 오게 기준점을 법선 방향으로 미리 민다.
  // normStep 성분이 전부 짝수라서 반 칸(0.5 cm)을 밀어도 정수 좌표가 유지된다.
  function centerBase(base, D, cmShift) {
    var s = normStep(D);
    return { x: base.x + s[0] * cmShift, y: base.y + s[1] * cmShift };
  }

  /* spec:
     { kind:'perp'|'gap'|'gapError'|'choose'|'shape',
       dirIdx, cm, side, along, gaps:[a,b], target, slantM, goal, dirIdx2, level }
  */
  function buildRound(spec) {
    var D = DIRS[spec.dirIdx];
    var base = { x: 0, y: Math.round(GH / 2) };
    if (spec.shiftY) base = { x: base.x, y: base.y + spec.shiftY };
    var r = {
      id: '', kind: spec.kind, level: spec.level || 1, dirIdx: spec.dirIdx,
      lines: [], point: null, planted: [], goal: null, answerCm: null,
      unitConcept: '', cue: D.cue, task: '', prompt: '', targetLine: 0
    };

    if (spec.kind === 'perp') {
      // 차시 1 · 수직과 수선 — 점 ㄱ 에서 직선 ㄴㄷ 에 수선을 그어 내린다.
      base = centerBase(base, D, (-spec.cm / 2) * spec.side);
      var line = mkLine(spec.dirIdx, 0, base, 'ㄴㄷ');
      var s = normStep(D);
      var foot = slideBase(D, spec.along, base);
      r.lines = [line];
      r.point = { x: foot.x + s[0] * spec.cm * spec.side, y: foot.y + s[1] * spec.cm * spec.side, label: 'ㄱ' };
      r.answerCm = spec.cm;
      r.unitConcept = '수직과 수선';
      r.task = '점 ㄱ에서 직선 ㄴㄷ에 대한 수선을 그어 보세요.';
      r.cue2 = sideCue(D, spec.side) + ' ' + alongCue(D, spec.along);
      r.id = 'perp-' + spec.dirIdx + '-' + spec.cm + '-' + spec.side + '-' + spec.along;
    } else if (spec.kind === 'gap' || spec.kind === 'gapError') {
      // 차시 2~3 · 평행선 사이의 거리 — 거리는 수선의 길이다.
      base = centerBase(base, D, -spec.cm / 2);
      r.lines = [mkLine(spec.dirIdx, 0, base, 'ㄱㄴ'), mkLine(spec.dirIdx, spec.cm, base, 'ㄷㄹ')];
      r.answerCm = spec.cm;
      r.unitConcept = '평행선 사이의 거리';
      if (spec.kind === 'gapError') {
        // 비스듬히 꽂힌 막대를 미리 한 자루 둔다. 길이는 √(cm²+m²) 가 정수가 되게 고른다.
        var m = spec.slantM;
        var st = normStep(D), dv = [SUB * D.d[0] / D.L, SUB * D.d[1] / D.L];
        var a0 = slideBase(D, spec.along, base);
        var b0 = { x: a0.x + st[0] * spec.cm + dv[0] * m, y: a0.y + st[1] * spec.cm + dv[1] * m };
        var slantCm = Math.round(Math.sqrt(spec.cm * spec.cm + m * m));
        r.planted = [{ a: a0, b: b0, kind: 'slant', cm: slantCm }];
        r.slantCm = slantCm;
        r.task = '비스듬히 꽂힌 막대 옆에 곧은 막대를 꽂아 두 파이프 사이의 거리를 재 보세요.';
        r.cue2 = '이미 꽂힌 막대는 비스듬하고 길이가 ' + slantCm + ' cm예요.';
        r.id = 'err-' + spec.dirIdx + '-' + spec.cm + '-' + m + '-' + spec.along;
      } else {
        r.task = GAP_TASKS[((spec.along % 3) + 3) % 3];
        r.cue2 = '두 파이프는 평행해요.';
        r.id = 'gap-' + spec.dirIdx + '-' + spec.cm + '-' + spec.along;
      }
    } else if (spec.kind === 'choose') {
      // 차시 3 · 역추적 — 파이프 3개 중 거리가 주문한 cm 인 두 개를 골라 잇는다.
      var g1 = spec.gaps[0], g2 = spec.gaps[1];
      base = centerBase(base, D, -(g1 + g2) / 2);
      r.lines = [
        mkLine(spec.dirIdx, 0, base, 'ㄱㄴ'),
        mkLine(spec.dirIdx, g1, base, 'ㄷㄹ'),
        mkLine(spec.dirIdx, g1 + g2, base, 'ㅁㅂ')
      ];
      r.answerCm = spec.target;
      r.unitConcept = '평행선 사이의 거리';
      r.task = '거리가 ' + spec.target + ' cm인 두 파이프를 곧은 막대로 이어 보세요.';
      r.cue2 = '파이프 세 개가 모두 평행해요.';
      r.id = 'choose-' + spec.dirIdx + '-' + g1 + '-' + g2 + '-' + spec.target;
    } else if (spec.kind === 'shape') {
      // 차시 4 · 사다리꼴 / (맛보기) 평행사변형 — 막대 한 자루를 더 꽂아 사각형을 닫는다.
      var D2 = spec.dirIdx2 === 6 ? tiltedDir(D) : DIRS[spec.dirIdx2];
      base = centerBase(base, D, -spec.bOffCm / 2);
      var ns = normStep(D);
      var lineA = mkLine(spec.dirIdx, 0, base, 'ㄱㄴ');
      var lineB = { dirIdx: spec.dirIdx2, d: D2.d, L: D2.L,
                    offCm: spec.dirIdx2 === spec.dirIdx ? spec.bOffCm : null, label: 'ㄷㄹ',
                    p: { x: base.x + ns[0] * spec.bOffCm, y: base.y + ns[1] * spec.bOffCm } };
      r.lines = [lineA, lineB];
      // 미리 꽂힌 막대: lineA 위의 점에서 직각으로 내려가 lineB 에 닿는 「기준 막대」.
      var pa = slideBase(D, spec.along, base);
      var qb = pointOnLineAtRay(lineB, pa, ns);
      r.planted = [{ a: pa, b: qb, kind: 'brace' }];
      r.goal = spec.goal;
      r.answerCm = null;
      r.unitConcept = spec.goal === '평행사변형' ? '평행사변형' : '사다리꼴';
      r.task = '막대를 하나 더 꽂아 ' + spec.goal + '을 만들어 보세요.';
      r.cue2 = (spec.dirIdx2 === 6 ? '두 파이프는 평행하지 않아요.' : '두 파이프는 평행해요.') +
               ' 이미 꽂힌 막대가 한 변이에요.';
      r.id = 'shape-' + spec.dirIdx + '-' + spec.dirIdx2 + '-' + spec.goal + '-' + spec.along;
    }
    r.prompt = r.cue + ' ' + (r.cue2 || '') + ' ' + r.task;
    r.prompt = r.prompt.replace(/\s+/g, ' ').trim();
    return r;
  }

  // 점 p 에서 방향 dir 로 쏜 반직선이 line 과 만나는 점 (정수 보장 안 됨 — 화면용)
  function pointOnLineAtRay(line, p, dir) {
    var w = sub(line.p, p);
    var den = dir[0] * line.d[1] - dir[1] * line.d[0];
    var t = (w.x * line.d[1] - w.y * line.d[0]) / den;
    return { x: p.x + dir[0] * t, y: p.y + dir[1] * t };
  }

  // ── 판정 ────────────────────────────────────────────────────────
  // 반환 reason:
  //   ok            — 정답
  //   refuse:*      — 답으로 치지 않는 거절(밴드 안 깎임). 조작 안내가 필요한 경우
  //   miss:*        — 오답 (밴드 1개 소모)
  function judge(round, stroke) {
    var a = stroke.a, b = stroke.b;
    var v = sub(b, a);
    if (len(v) < MIN_LEN) return { ok: false, reason: 'refuse:short', msg: '조금 더 길게 이어 보세요.' };

    if (round.kind === 'perp') {
      var line = round.lines[0];
      var da = dist(a, round.point), db = dist(b, round.point);
      if (db < da && db <= EPS_POINT) { var t = a; a = b; b = t; v = sub(b, a); }
      if (dist(a, round.point) > EPS_POINT)
        return { ok: false, reason: 'refuse:startPoint', msg: '점 ㄱ에서 시작해 보세요.' };
      if (lineDist(line, b) > EPS_END)
        return { ok: false, reason: 'miss:notReach', msg: '파이프까지 닿지 않았어요.' };
      if (!perpToDir(line.d, line.L, v))
        return { ok: false, reason: 'miss:notPerp', msg: '직선 ㄴㄷ에 직각이 아니에요.' };
      var span = Math.abs(v.x * unitNormal(DIRS[line.dirIdx]).x + v.y * unitNormal(DIRS[line.dirIdx]).y);
      if (Math.abs(span - round.answerCm * SUB) > EPS_SPAN)
        return { ok: false, reason: 'miss:span', msg: '점 ㄱ에서 파이프까지 곧게 이어 보세요.' };
      return { ok: true, reason: 'ok', cm: round.answerCm };
    }

    if (round.kind === 'gap' || round.kind === 'gapError' || round.kind === 'choose') {
      var ia = nearestLine(round, a), ib = nearestLine(round, b);
      if (ia < 0) return { ok: false, reason: 'refuse:startPipe', msg: '파이프 위에서 시작해 보세요.' };
      if (ib < 0) return { ok: false, reason: 'miss:notReach', msg: '다른 파이프까지 닿지 않았어요.' };
      if (ia === ib) return { ok: false, reason: 'refuse:samePipe', msg: '다른 파이프까지 이어 보세요.' };
      var la = round.lines[ia], lb = round.lines[ib];
      var lo = Math.min(la.offCm, lb.offCm), hi = Math.max(la.offCm, lb.offCm);
      for (var i = 0; i < round.lines.length; i++) {
        var o = round.lines[i].offCm;
        if (o > lo && o < hi) return { ok: false, reason: 'miss:skipped', msg: '가운데 파이프를 지나쳤어요.' };
      }
      if (!perpToDir(la.d, la.L, v))
        return { ok: false, reason: 'miss:notPerp', msg: '파이프에 직각이 아니에요. 비스듬한 막대는 거리가 아니에요.' };
      var gap = hi - lo;                                  // 정수 cm — 여기가 정답의 원천이다
      var n = unitNormal(DIRS[la.dirIdx]);
      var spn = Math.abs(v.x * n.x + v.y * n.y);
      if (Math.abs(spn - gap * SUB) > EPS_SPAN)
        return { ok: false, reason: 'miss:span', msg: '두 파이프에 양 끝이 닿게 이어 보세요.' };
      if (round.kind === 'choose' && gap !== round.answerCm)
        return { ok: false, reason: 'miss:wrongGap', cm: gap,
                 msg: '여기는 ' + gap + ' cm 예요. ' + round.answerCm + ' cm인 곳을 찾아보세요.' };
      return { ok: true, reason: 'ok', cm: gap };
    }

    if (round.kind === 'shape') {
      var A = round.lines[0], B = round.lines[1];
      var onA = lineDist(A, a) <= EPS_END, onB = lineDist(B, b) <= EPS_END;
      if (!onA && lineDist(B, a) <= EPS_END && lineDist(A, b) <= EPS_END) {
        var tmp = a; a = b; b = tmp; v = sub(b, a); onA = true; onB = true;
      }
      if (!onA) return { ok: false, reason: 'refuse:startPipe', msg: '파이프 위에서 시작해 보세요.' };
      if (!onB) return { ok: false, reason: 'miss:notReach', msg: '맞은편 파이프까지 닿지 않았어요.' };
      var pl = round.planted[0];
      if (dist(a, pl.a) < SUB || dist(b, pl.b) < SUB)
        return { ok: false, reason: 'refuse:tooClose', msg: '이미 꽂힌 막대와 떨어진 곳에 꽂아 보세요.' };
      if (segCross(a, b, pl.a, pl.b))
        return { ok: false, reason: 'miss:crossed', msg: '두 막대가 엇갈렸어요.' };
      // 파이프가 만나는 점 너머에 꽂으면 나비넥타이 모양이 되어 사각형이 아니다
      if (segCross(pl.a, a, b, pl.b))
        return { ok: false, reason: 'miss:bowtie', msg: '사각형이 닫히지 않고 꼬였어요. 두 파이프가 만나는 쪽 반대편에 꽂아 보세요.' };
      var bracesPar = parallelVecs(v, sub(pl.b, pl.a));
      var pipesPar = parallelDirs(A.d, A.L, B.d, B.L);
      var pairs = (bracesPar ? 1 : 0) + (pipesPar ? 1 : 0);
      // ★포함관계★ 사다리꼴 = 평행한 변이 한 쌍이라도 있는 사각형.
      // 평행사변형·직사각형·정사각형도 전부 사다리꼴이다. 배타 채점하지 않는다.
      var isTrapezoid = pairs >= 1, isParallelogram = pairs === 2;
      if (round.goal === '사다리꼴') {
        if (isTrapezoid) return { ok: true, reason: 'ok', pairs: pairs, parallelogram: isParallelogram };
        return { ok: false, reason: 'miss:noPair', msg: '평행한 변이 한 쌍도 없어요.' };
      }
      if (round.goal === '평행사변형') {
        if (isParallelogram) return { ok: true, reason: 'ok', pairs: 2, parallelogram: true };
        return { ok: false, reason: 'miss:onePair', msg: '평행한 변이 한 쌍뿐이에요. 사다리꼴이지만 평행사변형은 아니에요.' };
      }
    }
    return { ok: false, reason: 'refuse:unknown', msg: '다시 이어 보세요.' };
  }

  function dist(p, q) { return len(sub(p, q)); }
  function nearestLine(round, q) {
    var best = -1, bd = EPS_END + 1;
    for (var i = 0; i < round.lines.length; i++) {
      var d = lineDist(round.lines[i], q);
      if (d < bd) { bd = d; best = i; }
    }
    return best;
  }

  // ── 정답 획 / 오답 획 (검증·훅·오답 해설용) ────────────────────────
  function footOf(line, q) {
    var w = sub(q, line.p), t = (w.x * line.d[0] + w.y * line.d[1]) / (line.L * line.L);
    return { x: line.p.x + line.d[0] * t, y: line.p.y + line.d[1] * t };
  }
  function correctStroke(round) {
    if (round.kind === 'perp') return { a: { x: round.point.x, y: round.point.y }, b: footOf(round.lines[0], round.point) };
    if (round.kind === 'gap' || round.kind === 'gapError') {
      var A = round.lines[0], B = round.lines[1];
      var s = round.kind === 'gapError' ? slideAlong(A, round.planted[0].a, 4) : pointOnLine(A, 0);
      return { a: s, b: footOf(B, s) };
    }
    if (round.kind === 'choose') {
      var pick = pickTargetPair(round);
      var s2 = pointOnLine(round.lines[pick[0]], 0);
      return { a: s2, b: footOf(round.lines[pick[1]], s2) };
    }
    if (round.kind === 'shape') {
      var pl = round.planted[0], vv = sub(pl.b, pl.a);
      var start = slideAlong(round.lines[0], pl.a, 5);
      return { a: start, b: pointOnLineAtRay(round.lines[1], start, [vv.x, vv.y]) };
    }
    return null;
  }
  function pickTargetPair(round) {
    for (var i = 0; i < round.lines.length; i++)
      for (var j = i + 1; j < round.lines.length; j++) {
        if (Math.abs(round.lines[j].offCm - round.lines[i].offCm) === round.answerCm) {
          var mid = round.lines.filter(function (l) {
            return l.offCm > Math.min(round.lines[i].offCm, round.lines[j].offCm) &&
                   l.offCm < Math.max(round.lines[i].offCm, round.lines[j].offCm);
          });
          if (!mid.length) return [i, j];
        }
      }
    return [0, 1];
  }
  function pointOnLine(line, alongCm) {
    return { x: line.p.x + (SUB * line.d[0] / line.L) * alongCm, y: line.p.y + (SUB * line.d[1] / line.L) * alongCm };
  }
  function slideAlong(line, q, alongCm) {
    var f = footOf(line, q);
    return { x: f.x + (SUB * line.d[0] / line.L) * alongCm, y: f.y + (SUB * line.d[1] / line.L) * alongCm };
  }

  // 오답 획은 오개념을 역산해 만든다 (전부 misconceptionId 를 달아 둔다).
  function wrongStrokes(round) {
    var out = [], c = correctStroke(round);
    if (!c) return out;
    var v = sub(c.b, c.a), L = len(v);
    var dirIdx = round.dirIdx, D = DIRS[dirIdx];
    // ① 「가로선·세로선처럼 보일 때만 수직이라고 생각한다」
    if (D.d[0] !== 0 && D.d[1] !== 0) {
      out.push({
        misconceptionId: 'axis-looks-perpendicular',
        stroke: { a: c.a, b: { x: c.a.x, y: c.a.y + (v.y >= 0 ? L : -L) } },
        explanation: '화면 세로로만 그으면 기울어진 파이프와 직각이 아니에요.'
      });
      out.push({
        misconceptionId: 'axis-looks-perpendicular',
        stroke: { a: c.a, b: { x: c.a.x + (v.x >= 0 ? L : -L), y: c.a.y } },
        explanation: '화면 가로로만 그으면 기울어진 파이프와 직각이 아니에요.'
      });
    }
    // ② 「평행선 사이의 거리를 비스듬히 이은 선분의 길이로 잰다」
    var dv = { x: SUB * D.d[0] / D.L, y: SUB * D.d[1] / D.L };
    out.push({
      misconceptionId: 'oblique-is-distance',
      stroke: { a: c.a, b: { x: c.b.x + dv.x * 3, y: c.b.y + dv.y * 3 } },
      explanation: '비스듬히 이은 막대는 더 길어요. 거리는 수선의 길이예요.'
    });
    // ③ 「거리가 주문한 값이 아닌 쌍을 고른다」 (역추적 라운드)
    if (round.kind === 'choose') {
      for (var i = 0; i < round.lines.length - 1; i++) {
        var g = round.lines[i + 1].offCm - round.lines[i].offCm;
        if (g !== round.answerCm) {
          var s = pointOnLine(round.lines[i], 0);
          out.push({
            misconceptionId: 'wrong-gap-picked',
            stroke: { a: s, b: footOf(round.lines[i + 1], s) },
            explanation: '거리가 ' + g + ' cm인 곳이에요. 주문한 곳이 아니에요.'
          });
          break;
        }
      }
    }
    // ④ 「사다리꼴/평행사변형을 배타적으로 분류한다」 — 평행이 아닌 막대로 닫으면
    if (round.kind === 'shape') {
      var pl = round.planted[0], pv = sub(pl.b, pl.a);
      var tilted = { x: pv.x + (SUB * D.d[0] / D.L) * 4, y: pv.y + (SUB * D.d[1] / D.L) * 4 };
      var st = slideAlong(round.lines[0], pl.a, 6);
      out.push({
        misconceptionId: 'exclusive-quad-classification',
        stroke: { a: st, b: pointOnLineAtRay(round.lines[1], st, [tilted.x, tilted.y]) },
        explanation: round.goal === '평행사변형'
          ? '평행한 변이 한 쌍뿐이면 사다리꼴이에요. 평행사변형이 되려면 두 쌍이 평행해야 해요.'
          : '두 막대가 평행하지 않으면 평행한 변이 한 쌍도 없어요.'
      });
    }
    return out;
  }

  // ── 본판 덱 (난이도 밴드를 순서대로 소진한다) ───────────────────────
  // 연습 1 + 본판 10. 유형 배분: 수선 3 / 평행선 거리 3(오류 고치기 1 포함) /
  // 거리 역추적 2 / 사각형 구성 2.
  function practiceRound() {
    var r = buildRound({ kind: 'perp', dirIdx: 0, cm: 4, side: -1, along: 0, level: 1 });
    r.practice = true;
    r.task = '점 ㄱ에서 파이프와 직각이 되게 막대를 한 획에 꽂아 보세요.';
    r.prompt = r.cue + ' ' + r.task;
    return r;
  }
  // ⚠️ 본판 10장에는 **축정렬(가로·세로) 파이프를 쓰지 않는다.**
  // 화면 세로로만 긋는 연타 봇이 축정렬 라운드를 그대로 통과해 첫 시도 정답률이
  // 우연 수준(6.67 %)을 크게 넘었다(2026-09-11 봇 감사 50 %). 축정렬은 연습 1장 전용이다.
  // 이 단원이 겨냥하는 오개념 자체가 「가로·세로처럼 보일 때만 수직」이므로,
  // 채점되는 판은 전부 기울어져 있어야 한다.
  // 판마다 같은 좌표가 나오면 두 번째 판은 규칙이 아니라 순서를 외워 푸는 판이 된다.
  // 밴드(유형 순서)는 기획대로 고정하고, 밴드 **안에서** 기울기·거리·위치만 갈아 끼운다.
  var SLOTS = [
    [{ kind: 'perp', dirIdx: 2, cm: 4, side: -1, along: 0, level: 1 },
     { kind: 'perp', dirIdx: 4, cm: 4, side: 1, along: 2, level: 1 },
     { kind: 'perp', dirIdx: 3, cm: 3, side: -1, along: -2, level: 1 }],
    [{ kind: 'gap', dirIdx: 2, cm: 4, along: 0, level: 1 },
     { kind: 'gap', dirIdx: 5, cm: 3, along: 2, level: 1 },
     { kind: 'gap', dirIdx: 3, cm: 4, along: -2, level: 1 }],
    [{ kind: 'perp', dirIdx: 4, cm: 5, side: 1, along: -2, level: 2 },
     { kind: 'perp', dirIdx: 5, cm: 4, side: 1, along: 0, level: 2 },
     { kind: 'perp', dirIdx: 2, cm: 6, side: -1, along: 2, level: 2 }],
    [{ kind: 'gapError', dirIdx: 3, cm: 4, slantM: 3, along: -2, level: 2 },
     { kind: 'gapError', dirIdx: 2, cm: 3, slantM: 4, along: -2, level: 2 },
     { kind: 'gapError', dirIdx: 5, cm: 4, slantM: 3, along: 0, level: 2 }],
    [{ kind: 'perp', dirIdx: 5, cm: 5, side: -1, along: 1, level: 2 },
     { kind: 'perp', dirIdx: 3, cm: 5, side: 1, along: -2, level: 2 },
     { kind: 'perp', dirIdx: 4, cm: 6, side: -1, along: 0, level: 2 }],
    [{ kind: 'gap', dirIdx: 4, cm: 5, along: 0, level: 2 },
     { kind: 'gap', dirIdx: 2, cm: 6, along: -2, level: 2 },
     { kind: 'gap', dirIdx: 3, cm: 5, along: 2, level: 2 }],
    [{ kind: 'choose', dirIdx: 2, gaps: [4, 6], target: 4, level: 3 },
     { kind: 'choose', dirIdx: 3, gaps: [3, 5], target: 3, level: 3 },
     { kind: 'choose', dirIdx: 4, gaps: [5, 3], target: 5, level: 3 }],
    [{ kind: 'choose', dirIdx: 5, gaps: [3, 5], target: 5, level: 3 },
     { kind: 'choose', dirIdx: 2, gaps: [6, 4], target: 6, level: 3 },
     { kind: 'choose', dirIdx: 5, gaps: [4, 6], target: 6, level: 3 }],
    [{ kind: 'shape', dirIdx: 3, dirIdx2: 6, goal: '사다리꼴', along: -2, bOffCm: 6, level: 3 },
     { kind: 'shape', dirIdx: 2, dirIdx2: 6, goal: '사다리꼴', along: -2, bOffCm: 6, level: 3 },
     { kind: 'shape', dirIdx: 5, dirIdx2: 6, goal: '사다리꼴', along: 2, bOffCm: 6, level: 3 }],
    [{ kind: 'shape', dirIdx: 4, dirIdx2: 4, goal: '평행사변형', along: -2, bOffCm: 7, level: 3 },
     { kind: 'shape', dirIdx: 2, dirIdx2: 2, goal: '평행사변형', along: -2, bOffCm: 7, level: 3 },
     { kind: 'shape', dirIdx: 3, dirIdx2: 3, goal: '평행사변형', along: -2, bOffCm: 6, level: 3 }]
  ];
  function deck() {
    var out = [];
    for (var i = 0; i < SLOTS.length; i++)
      out.push(buildRound(SLOTS[i][Math.floor(Math.random() * SLOTS[i].length)]));
    return out;
  }
  // dirIdx2 = 6 은 「파이프 ㄱㄴ 과 평행하지 않은 파이프」를 뜻하는 별도 방향이다.
  DIRS[6] = { d: [2, -1], L: Math.sqrt(5), cue: '아래 파이프는 위 파이프와 평행하지 않아요.' };

  // ── 표본 뽑기 ────────────────────────────────────────────────────
  // QA 는 n = 40 / 17 / 63 으로 세 번 부른다. n 을 존중한다.
  function allSpecs() {
    var specs = [];
    var alongs = [-2, 0, 2];       // 놀이판(가로 10 cm · 세로 17 cm) 안에 들어오는 범위
    for (var di = 0; di < 6; di++) {
      for (var k = 0; k < alongs.length; k++) {
        for (var cm = 3; cm <= 6; cm++) {
          specs.push({ kind: 'perp', dirIdx: di, cm: cm, side: 1, along: alongs[k], level: 1 });
          specs.push({ kind: 'perp', dirIdx: di, cm: cm, side: -1, along: alongs[k], level: 1 });
          specs.push({ kind: 'gap', dirIdx: di, cm: cm, along: alongs[k], level: 2 });
        }
        specs.push({ kind: 'gapError', dirIdx: di, cm: 4, slantM: 3, along: alongs[k] - 2, level: 2 });
        specs.push({ kind: 'gapError', dirIdx: di, cm: 3, slantM: 4, along: alongs[k] - 2, level: 2 });
      }
      var gapSets = [[3, 4], [4, 6], [3, 5], [5, 3], [6, 4], [4, 3]];
      for (var g = 0; g < gapSets.length; g++) {
        specs.push({ kind: 'choose', dirIdx: di, gaps: gapSets[g], target: gapSets[g][0], level: 3 });
        specs.push({ kind: 'choose', dirIdx: di, gaps: gapSets[g], target: gapSets[g][1], level: 3 });
      }
      specs.push({ kind: 'shape', dirIdx: di, dirIdx2: 6, goal: '사다리꼴', along: -2, bOffCm: 6, level: 3 });
      specs.push({ kind: 'shape', dirIdx: di, dirIdx2: di, goal: '평행사변형', along: -2, bOffCm: 7, level: 3 });
    }
    return specs;
  }

  // 유형·방향이 한쪽으로 몰리지 않게 결정적으로 섞는다(난수 없음).
  function spreadOrder(specs) {
    var buckets = {};
    for (var i = 0; i < specs.length; i++) {
      var k = specs[i].kind + '|' + specs[i].dirIdx;
      (buckets[k] = buckets[k] || []).push(specs[i]);
    }
    var keys = Object.keys(buckets).sort(), out = [], more = true, row = 0;
    while (more) {
      more = false;
      for (var j = 0; j < keys.length; j++) {
        var b = buckets[keys[(j + row) % keys.length]];
        if (row < b.length) { out.push(b[row]); more = true; }
      }
      row++;
    }
    return out;
  }

  function normPrompt(p) {
    return p.replace(/\([^)]*\)/g, '').replace(/\s*\d+$/, '').replace(/\s+/g, ' ').trim();
  }

  function sampleProblems(n) {
    n = Math.max(1, Math.min(400, n | 0 || 20));
    var order = spreadOrder(allSpecs()), out = [], seenId = {}, seenPrompt = {}, leftovers = [];
    // 1차: 서로 다른 발문만 담아 다양성을 먼저 확보한다.
    for (var i = 0; i < order.length && out.length < n; i++) {
      var r = buildRound(order[i]);
      if (seenId[r.id]) continue;
      seenId[r.id] = 1;
      var np = normPrompt(r.prompt);
      if (seenPrompt[np]) { leftovers.push(r); continue; }
      seenPrompt[np] = 1;
      out.push(toProblem(r));
    }
    // 2차: 남은 자리는 같은 발문이라도 수치·배치가 다른 문항으로 채운다.
    for (var k = 0; k < leftovers.length && out.length < n; k++) out.push(toProblem(leftovers[k]));
    for (var m = 0; out.length < n; m++) out.push(toProblem(buildRound(order[m % order.length])));
    return out.slice(0, n);
  }

  function toProblem(r) {
    var answer, numeric;
    if (r.kind === 'shape') { answer = r.goal; numeric = r.goal === '평행사변형' ? 2 : 1; }
    else { answer = r.answerCm + ' cm'; numeric = r.answerCm; }
    return {
      id: r.id,
      prompt: r.prompt,
      choices: null,
      answer: answer,
      answerNumeric: numeric,
      unitConcept: r.unitConcept,
      level: r.level,
      kind: r.kind,
      geometry: {
        pipeDirection: r.lines[0].d,
        lineOffsetsCm: r.lines.map(function (l) { return l.offCm; }),
        cmPerUnit: SUB,
        slantLengthCm: r.slantCm || null
      },
      correctStroke: correctStroke(r),
      distractors: wrongStrokes(r).map(function (w) {
        return { misconceptionId: w.misconceptionId, explanation: w.explanation, stroke: w.stroke };
      })
    };
  }

  var API = {
    SUB: SUB, GH: GH, DIRS: DIRS, SIN6: SIN6,
    buildRound: buildRound, judge: judge, deck: deck, practiceRound: practiceRound,
    correctStroke: correctStroke, wrongStrokes: wrongStrokes,
    sampleProblems: sampleProblems, allSpecs: allSpecs, toProblem: toProblem,
    footOf: footOf, lineDist: lineDist, pointOnLine: pointOnLine, unitNormal: unitNormal,
    pickTargetPair: pickTargetPair, dist: dist
  };
  if (typeof module === 'object' && module.exports) module.exports = API;
  root.PipeRungEngine = API;
})(typeof window !== 'undefined' ? window : globalThis);
