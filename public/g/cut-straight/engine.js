/* 곧게 베어 (cut-straight) — 순수 로직 엔진
 *
 * 답은 오직 **손가락 궤적의 각**이다.
 *  · 줄무늬(호스 축)와 직각(90°±7°)으로 가르면 「벤다」
 *  · 줄무늬와 나란히(0°±7°) 쓸면 「흘려보낸다」
 * 각은 정수 도(°)로 접어서 비교하고(0~180 로 접은 뒤 90 과의 차), 거리 cm 는
 * 「줄무늬 ㄷㄹ 은 줄무늬 ㄱㄴ 을 폭 방향으로 d cm 옮긴 것」이라는 정의에서 나오는 정수다.
 * 화면에 찍히는 cm 는 궤적에서 잰 값이 아니라 이 정의값이다. 1 cm = 20 서브유닛.
 *
 * ── 교과서 대조 (아이스크림미디어 2022개정 4-2 4단원 「사각형」 1~4차시) ──
 *  · 발문은 익힘책 지시형 「~해 보세요」로 통일. 청유형(~해 봅시다)과 섞지 않는다.
 *  · 수선은 어느 직선에 대한 수선인지 밝힌다 → 「줄무늬 ㄱㄴ에 대한 수선」.
 *  · 평행선 사이의 거리는 수선의 길이로만 잰다. 비스듬한 박음질은 거리가 아니다.
 *  · ★포함관계★ 사다리꼴 = 평행한 변이 한 쌍이라도 있는 사각형. 평행사변형·직사각형
 *    조각도 사다리꼴로 인정한다. 배타 분류로 채점하지 않는다.
 *  · 어림(올림·버림·반올림) 발문은 이 단원에 없다. 생성하지 않는다.
 */
(function (root) {
  'use strict';

  var SUB = 20;          // 1 cm
  var TOL = 6;           // 수선·평행 허용오차 ±6° → 우연 수준 12/180 = 6.67 %
  var HALF_W = 42;       // 호스 반폭 2.1 cm
  var D2R = Math.PI / 180;

  function dirOf(deg) { return { x: Math.cos(deg * D2R), y: Math.sin(deg * D2R) }; }
  function fold180(a) { var d = ((a % 180) + 180) % 180; return d; }
  function angleBetween(a, b) { var d = Math.abs(fold180(a) - fold180(b)); return d > 90 ? 180 - d : d; }
  function segAngle(a, b) { return Math.atan2(b.y - a.y, b.x - a.x) / D2R; }
  function len(v) { return Math.sqrt(v.x * v.x + v.y * v.y); }
  function sub(a, b) { return { x: a.x - b.x, y: a.y - b.y }; }
  function add(p, v, k) { return { x: p.x + v.x * k, y: p.y + v.y * k }; }

  // 선분 ab 가 「점 p 를 지나고 방향 d 인 직선」과 만나는 매개변수 t (없으면 null)
  function hitLine(a, b, p, d) {
    var v = sub(b, a), den = v.x * d.y - v.y * d.x;
    if (Math.abs(den) < 1e-9) return null;
    var w = sub(p, a);
    var t = (w.x * d.y - w.y * d.x) / den;
    if (t < -0.02 || t > 1.02) return null;
    return { t: t, pt: { x: a.x + v.x * t, y: a.y + v.y * t } };
  }
  function segHit(a, b, c, d) {   // 선분 ab 와 선분 cd 의 교차 여부
    function s(p, q, r) { return Math.sign((q.x - p.x) * (r.y - p.y) - (q.y - p.y) * (r.x - p.x)); }
    var d1 = s(a, b, c), d2 = s(a, b, d), d3 = s(c, d, a), d4 = s(c, d, b);
    return d1 * d2 < 0 && d3 * d4 < 0;
  }

  // ── 발문 어휘 ────────────────────────────────────────────────────
  function tiltCue(theta) {
    var t = fold180(theta);
    if (t === 0) return '호스가 가로로 곧게 누워 있어요.';
    if (t === 90) return '호스가 세로로 곧게 서 있어요.';
    if (t < 25) return '호스가 오른쪽 아래로 아주 조금 기울어 있어요.';
    if (t < 48) return '호스가 오른쪽 아래로 조금 기울어 있어요.';
    if (t < 70) return '호스가 오른쪽 아래로 많이 기울어 있어요.';
    if (t < 90) return '호스가 거의 세로로 서 있고 오른쪽 아래로 조금 기울어 있어요.';
    if (t < 112) return '호스가 거의 세로로 서 있고 오른쪽 위로 조금 기울어 있어요.';
    if (t < 133) return '호스가 오른쪽 위로 많이 기울어 있어요.';
    if (t < 156) return '호스가 오른쪽 위로 조금 기울어 있어요.';
    return '호스가 오른쪽 위로 아주 조금 기울어 있어요.';
  }
  var NAMES = ['민준', '서윤', '하은'];

  /* spec:
     { kind:'perp'|'dist'|'decoy'|'reverse'|'quad', theta, d, target, seed, level } */
  function buildRound(spec) {
    var theta = fold180(spec.theta), d = spec.d || 4;
    var r = {
      id: '', kind: spec.kind, level: spec.level || 1, theta: theta, d: d,
      stripes: spec.kind === 'perp' ? 0 : 2, decoy: null, quad: null,
      answer: 'cut', answerCm: d, unitConcept: '', cue: tiltCue(theta), cue2: '', task: '', prompt: ''
    };
    if (spec.kind === 'perp') {
      r.answerCm = null;
      r.unitConcept = '수직과 수선';
      r.task = '줄무늬 ㄱㄴ에 대한 수선이 되게 호스를 베어 보세요.';
      r.cue2 = '호스 가운데에 줄무늬 ㄱㄴ이 한 줄 있어요.';
      r.stripes = 1;
      r.id = 'perp-' + theta;
    } else if (spec.kind === 'dist') {
      r.unitConcept = '평행선 사이의 거리';
      r.task = '평행한 두 줄무늬 ㄱㄴ, ㄷㄹ 사이의 거리를 재도록 베어 보세요.';
      r.cue2 = '줄무늬 두 줄은 서로 평행해요.';
      r.id = 'dist-' + theta + '-' + d;
    } else if (spec.kind === 'decoy') {
      // 오류 찾기 — 비스듬한 박음질이 이미 그려져 있다.
      // 폭 d, 축 방향 m 인 직각삼각형의 빗변이 정수가 되게 (3,4)·(4,3) 만 쓴다 → 5 cm.
      var m = d === 3 ? 4 : 3;
      r.decoy = { m: m, len: Math.round(Math.sqrt(d * d + m * m)) };
      r.who = NAMES[(spec.seed || 0) % 3];
      r.unitConcept = '평행선 사이의 거리';
      r.task = '그 실을 따라가지 말고, 두 줄무늬 사이의 거리를 재도록 베어 보세요.';
      r.cue2 = r.who + '이가 박아 둔 비스듬한 실은 ' + r.decoy.len + ' cm예요.';
      r.id = 'decoy-' + theta + '-' + d;
    } else if (spec.kind === 'reverse') {
      // 역추적 — 주문한 거리인 호스만 벤다. 아니면 줄무늬와 나란히 쓸어 흘려보낸다.
      r.answer = d === spec.target ? 'cut' : 'pass';
      r.target = spec.target;
      r.unitConcept = '평행선 사이의 거리';
      r.task = '두 줄무늬 사이의 거리가 ' + spec.target + ' cm인 호스만 베고, 아니면 줄무늬와 나란히 쓸어 흘려보내세요.';
      // 호스 위에는 눈금이 없다(그 선이 곧 수선 단서라 지웠다). 실제 화면 도구인 기준자를 부른다.
      r.cue2 = '왼쪽 아래 기준자가 주문한 길이예요. 한 칸이 1 cm예요.';
      r.id = 'rev-' + theta + '-' + d + '-' + spec.target;
    } else if (spec.kind === 'quad') {
      // 차시 4 — 변 세 개가 있는 이음쇠. 변 ㄱㄴ과 평행하게 네 번째 변을 벤다.
      var u = dirOf(theta), n = dirOf(theta + 90);
      var half = d * SUB * 0.9;
      var p1 = add(add({ x: 0, y: 0 }, n, d * SUB / 2), u, -half);
      var p2 = add(add({ x: 0, y: 0 }, n, d * SUB / 2), u, half);
      // 두 다리는 변 ㄱㄴ에서 서로 다른 각으로 뻗어 있어(평행하지 않다) 사다리꼴이 된다.
      // ⚠️ 두 다리가 법선 방향으로 같은 높이면 열린 쪽 점선(다리 끝끼리)이 변 ㄱㄴ과 평행해져
      // 「점선 따라 긋기」가 곧 정답이 된다 → 높이를 1.35d / 0.6d 로 달리해 열린 쪽을 약 18° 기울인다.
      var legA = { a: p1, b: add(add(p1, n, -d * SUB * 1.35), u, -d * SUB * 0.28) };
      var legB = { a: p2, b: add(add(p2, n, -d * SUB * 0.6), u, d * SUB * 0.18) };
      r.quad = { base: { a: p1, b: p2 }, legA: legA, legB: legB };
      r.answer = 'cut';
      r.answerCm = null;
      r.unitConcept = '사다리꼴';
      r.task = '열린 쪽을 변 ㄱㄴ과 평행하게 베어 사다리꼴을 만들어 보세요.';
      r.cue2 = '평행한 변이 한 쌍이라도 있으면 사다리꼴이에요.';
      r.cue = tiltCue(theta).replace('호스가', '이음쇠의 변 ㄱㄴ이').replace('누워 있어요', '놓여 있어요');
      r.id = 'quad-' + theta + '-' + d;
    }
    r.prompt = (r.cue + ' ' + r.cue2 + ' ' + r.task).replace(/\s+/g, ' ').trim();
    return r;
  }

  // 현재 호스 위치(center)에서의 기하
  // 호스 반폭 — 줄무늬 두 줄(간격 d cm)이 항상 고무 안에 들어오게 넓힌다
  function hoseHalf(round) { return Math.max(HALF_W, (round.d || 4) * SUB / 2 + 12); }
  function geom(round, center) {
    var c = center || { x: 0, y: 0 };
    var u = dirOf(round.theta), n = dirOf(round.theta + 90);
    var g = { u: u, n: n, center: c, halfW: hoseHalf(round), stripeLines: [] };
    if (round.kind === 'perp') {
      g.stripeLines = [{ p: c, label: 'ㄱㄴ' }];
    } else if (round.quad) {
      g.quad = {
        base: { a: add(c, { x: round.quad.base.a.x, y: round.quad.base.a.y }, 1),
                b: add(c, { x: round.quad.base.b.x, y: round.quad.base.b.y }, 1) },
        legA: { a: add(c, round.quad.legA.a, 1), b: add(c, round.quad.legA.b, 1) },
        legB: { a: add(c, round.quad.legB.a, 1), b: add(c, round.quad.legB.b, 1) }
      };
    } else {
      g.stripeLines = [
        { p: add(c, n, -round.d * SUB / 2), label: 'ㄱㄴ' },
        { p: add(c, n, round.d * SUB / 2), label: 'ㄷㄹ' }
      ];
    }
    if (round.decoy) {
      var a0 = add(add(c, n, -round.d * SUB / 2), u, -round.decoy.m * SUB / 2);
      g.decoy = { a: a0, b: add(add(a0, n, round.d * SUB), u, round.decoy.m * SUB) };
    }
    return g;
  }

  // ── 판정 ────────────────────────────────────────────────────────
  // action = { type:'swipe', a, b, center }
  function judge(round, action) {
    if (!action || action.type !== 'swipe') return { ok: false, reason: 'refuse:none', msg: '호스를 가로질러 베어 보세요.' };
    var a = action.a, b = action.b;
    if (len(sub(b, a)) < 30) return { ok: false, reason: 'refuse:short', msg: '조금 더 길게 그어 보세요.' };
    var g = geom(round, action.center);
    var phi = segAngle(a, b);
    var toAxis = angleBetween(phi, round.theta);        // 0(나란함) ~ 90(직각)
    var isPerp = Math.abs(toAxis - 90) <= TOL;
    var isPara = toAxis <= TOL;

    if (round.quad) {
      var q = g.quad;
      var baseAng = segAngle(q.base.a, q.base.b);
      var crossA = segHit(a, b, q.legA.a, q.legA.b), crossB = segHit(a, b, q.legB.a, q.legB.b);
      if (!crossA || !crossB)
        return { ok: false, reason: 'refuse:notCross', msg: '두 옆변을 모두 가로지르게 베어 보세요.' };
      if (angleBetween(phi, baseAng) <= TOL)
        return { ok: true, reason: 'ok', pairs: 1 };
      return { ok: false, reason: 'miss:notParallel', err: Math.round(angleBetween(phi, baseAng)),
               msg: '변 ㄱㄴ과 평행하지 않아요. 평행한 변이 한 쌍도 없으면 사다리꼴이 아니에요.' };
    }

    // 호스 몸통을 실제로 가르는가 — 궤적이 **호스 축선을 건너면** 가른 것이다.
    // (예전엔 양쪽 가장자리를 모두 지나야 해서, 호스 폭보다 짧은 손가락 궤적은
    //  아무리 정확해도 판정 자체에 들어가지 못했다.)
    var axisHit = hitLine(a, b, g.center, g.u);
    var crossesBody = !!axisHit;

    if (round.answer === 'pass') {
      if (isPara) return { ok: true, reason: 'ok:passed',
                           msg: '줄무늬와 나란히 쓸어 흘려보냈어요. 여기는 ' + round.d + ' cm였어요.' };
      if (isPerp && crossesBody)
        return { ok: false, reason: 'miss:cutWrong',
                 msg: '여기는 ' + round.d + ' cm예요. ' + round.target + ' cm인 호스만 베요.' };
      return { ok: false, reason: 'miss:neither',
               msg: '줄무늬와 나란히 쓸어야 흘러가요. 지금은 비스듬해요.' };
    }
    if (isPara) {
      return { ok: false, reason: 'miss:parallelCut',
               msg: '줄무늬와 나란히 그으면 잘리지 않아요. 직각으로 가로질러 베어 보세요.' };
    }
    if (!crossesBody)
      return { ok: false, reason: 'refuse:notCross', msg: '호스 몸통을 가로질러 베어 보세요.' };
    if (!isPerp)
      return { ok: false, reason: 'miss:notPerp', err: Math.round(90 - toAxis),
               msg: '줄무늬에 직각이 아니에요. 단면이 길쭉하게 찌그러졌어요.' };

    if (round.kind === 'dist' || round.kind === 'decoy') {
      var h1 = hitLine(a, b, g.stripeLines[0].p, g.u), h2 = hitLine(a, b, g.stripeLines[1].p, g.u);
      if (!h1 || !h2)
        return { ok: false, reason: 'miss:notBothStripes', msg: '줄무늬 두 줄을 모두 지나게 베어 보세요.' };
      var L = len(sub(h1.pt, h2.pt)), want = round.d * SUB;
      if (Math.abs(L - want) > want * 0.12)
        return { ok: false, reason: 'miss:length', msg: '두 줄무늬를 가장 짧게 잇는 곳을 베어 보세요.' };
      return { ok: true, reason: 'ok', cm: round.d };
    }
    return { ok: true, reason: 'ok', cm: round.answerCm };
  }

  // ── 정답/오답 궤적 ────────────────────────────────────────────────
  // 이음쇠: 두 다리를 s 만큼 올라간 자리에서 지정한 각으로 가로지르는 궤적
  function quadSwipe(round, c, deg, s) {
    var g = geom(round, c), q = g.quad, dir = dirOf(deg);
    var qa = { x: q.legA.a.x + (q.legA.b.x - q.legA.a.x) * s, y: q.legA.a.y + (q.legA.b.y - q.legA.a.y) * s };
    var hit = hitLine({ x: qa.x - dir.x * 2000, y: qa.y - dir.y * 2000 },
                      { x: qa.x + dir.x * 2000, y: qa.y + dir.y * 2000 },
                      q.legB.a, sub(q.legB.b, q.legB.a));
    var qb = hit ? hit.pt : add(qa, dir, round.d * SUB * 1.8);
    var span = round.d * SUB * 0.5;
    return { type: 'swipe', center: c, a: add(qa, dir, -span), b: add(qb, dir, span) };
  }
  function correctAction(round, center) {
    var c = center || { x: 0, y: 0 }, g = geom(round, c);
    // 왼쪽 다리의 22 % 높이(= 변 ㄱㄴ에서 0.3d) — 짧은 오른쪽 다리(0.6d)의 한가운데를 지난다
    if (round.quad) return quadSwipe(round, c, segAngle(g.quad.base.a, g.quad.base.b), 0.22);
    var dir = round.answer === 'pass' ? dirOf(round.theta) : dirOf(round.theta + 90);
    var reach = round.answer === 'pass' ? round.d * SUB * 1.6 + 60 : hoseHalf(round) + 24;
    return { type: 'swipe', center: c, a: add(c, dir, -reach), b: add(c, dir, reach) };
  }
  function wrongActions(round, center) {
    var c = center || { x: 0, y: 0 }, out = [];
    function swipe(deg, reach) {
      var d = dirOf(deg);
      return { type: 'swipe', center: c, a: add(c, d, -(reach || 120)), b: add(c, d, reach || 120) };
    }
    if (round.quad) {
      var base = segAngle(geom(round, c).quad.base.a, geom(round, c).quad.base.b);
      out.push({ misconceptionId: 'not-parallel-no-trapezoid', action: quadSwipe(round, c, base + 24, 0.22),
                 explanation: '변 ㄱㄴ과 평행하지 않으면 평행한 변이 한 쌍도 없어요.' });
      out.push({ misconceptionId: 'not-parallel-no-trapezoid', action: quadSwipe(round, c, base - 24, 0.22),
                 explanation: '반대쪽으로 기울여도 평행이 아니에요.' });
      return out;
    }
    if (round.answer === 'pass') {
      out.push({ misconceptionId: 'cut-without-measuring', action: swipe(round.theta + 90),
                 explanation: '거리를 재지 않고 베면 주문한 호스가 아니에요.' });
      return out;
    }
    // ① 「가로선·세로선처럼 보일 때만 수직」
    if (angleBetween(0, round.theta + 90) > TOL)
      out.push({ misconceptionId: 'axis-looks-perpendicular', action: swipe(0),
                 explanation: '화면 가로로 그으면 기울어진 줄무늬와 직각이 아니에요.' });
    if (angleBetween(90, round.theta + 90) > TOL)
      out.push({ misconceptionId: 'axis-looks-perpendicular', action: swipe(90),
                 explanation: '화면 세로로 그으면 기울어진 줄무늬와 직각이 아니에요.' });
    // ② 「비스듬히 이은 선분의 길이를 거리로 잰다」
    out.push({ misconceptionId: 'oblique-is-distance', action: swipe(round.theta + 90 - 28),
               explanation: '비스듬히 베면 더 길어요. 거리는 수선의 길이예요.' });
    // ③ 줄무늬와 나란히 긋기 — 잘리지 않는다
    out.push({ misconceptionId: 'parallel-is-not-cut', action: swipe(round.theta),
               explanation: '줄무늬와 나란한 선은 수선이 아니에요.' });
    return out;
  }

  // ── 본판 덱 (8토막) ───────────────────────────────────────────────
  // ⚠️ 채점되는 8장에 축정렬(0°·90°) 호스를 쓰지 않는다. 화면 가로·세로로만 긋는
  // 무뇌 봇이 축정렬 장을 그대로 통과해 첫 시도 정답률이 우연 수준을 넘는다.
  // 축정렬 호스는 채점하지 않는 연습 1장 전용이다.
  // 앞 3장을 모두 「베기」로 두어 무조건 흘려보내는 봇은 토막 3개를 다 잃는다.
  var SLOTS = [
    [{ kind: 'perp', theta: 20, level: 1 }, { kind: 'perp', theta: 155, level: 1 }, { kind: 'perp', theta: 32, level: 1 }],
    [{ kind: 'perp', theta: 55, level: 1 }, { kind: 'perp', theta: 125, level: 1 }, { kind: 'perp', theta: 68, level: 1 }],
    [{ kind: 'dist', theta: 35, d: 4, level: 2 }, { kind: 'dist', theta: 140, d: 3, level: 2 }, { kind: 'dist', theta: 48, d: 4, level: 2 }],
    [{ kind: 'perp', theta: 70, level: 2 }, { kind: 'perp', theta: 110, level: 2 }, { kind: 'perp', theta: 42, level: 2 }],
    [{ kind: 'decoy', theta: 40, d: 4, level: 3, seed: 1 }, { kind: 'decoy', theta: 130, d: 3, level: 3, seed: 2 },
     { kind: 'decoy', theta: 62, d: 4, level: 3, seed: 0 }],
    [{ kind: 'reverse', theta: 25, d: 6, target: 4, level: 3 }, { kind: 'reverse', theta: 150, d: 3, target: 5, level: 3 },
     { kind: 'reverse', theta: 58, d: 4, target: 4, level: 3 }],   // 셋 중 하나는 '베기'가 정답이다
    [{ kind: 'dist', theta: 115, d: 5, level: 3 }, { kind: 'dist', theta: 72, d: 5, level: 3 }, { kind: 'dist', theta: 28, d: 6, level: 3 }],
    [{ kind: 'quad', theta: 30, d: 4, level: 4 }, { kind: 'quad', theta: 145, d: 4, level: 4 }, { kind: 'quad', theta: 58, d: 4, level: 4 }]
  ];
  function practiceRound() {
    var r = buildRound({ kind: 'perp', theta: 0, level: 0 });
    r.practice = true;
    r.task = '줄무늬를 가로질러 직각으로 호스를 베어 보세요.';
    r.prompt = (r.cue + ' ' + r.cue2 + ' ' + r.task).replace(/\s+/g, ' ').trim();
    return r;
  }
  // 연습 2단계 — 「줄무늬와 나란히 쓸어 흘려보내기」도 발문 글자가 아니라 손으로 익힌다.
  // (실전 역추적 칸에서 처음 만나면 조작을 모른 채 토막을 잃는다.)
  function practicePass() {
    var r = buildRound({ kind: 'reverse', theta: 0, d: 5, target: 3, level: 0 });
    r.practice = true;
    r.task = '이 호스는 줄무늬와 나란히 쓸어 흘려보내 보세요.';
    r.cue2 = '왼쪽 아래 기준자가 주문한 길이(3 cm)예요. 줄무늬 사이가 기준자와 다르면 옆으로 흘려보내요.';
    r.prompt = (r.cue + ' ' + r.cue2 + ' ' + r.task).replace(/\s+/g, ' ').trim();
    return r;
  }
  function deck() {
    var out = [];
    for (var i = 0; i < SLOTS.length; i++)
      out.push(buildRound(SLOTS[i][Math.floor(Math.random() * SLOTS[i].length)]));
    return out;
  }

  // ── 표본 ─────────────────────────────────────────────────────────
  function allSpecs() {
    var out = [], thetas = [0, 18, 30, 45, 58, 72, 90, 108, 122, 138, 152, 166], i;
    for (i = 0; i < thetas.length; i++) {
      var th = thetas[i];
      out.push({ kind: 'perp', theta: th, level: 1 });
      for (var d = 3; d <= 6; d++) {
        out.push({ kind: 'dist', theta: th, d: d, level: 2 });
        // 기준자와 눈으로 견주는 칸이라 1 cm 차이(3 대 4 등)는 내지 않는다 — 같거나 2 cm 이상 다르게
        if (Math.abs(d - 4) !== 1) out.push({ kind: 'reverse', theta: th, d: d, target: 4, level: 3, seed: i });
        if (Math.abs(d - 5) !== 1) out.push({ kind: 'reverse', theta: th, d: d, target: 5, level: 3, seed: i + 1 });
        if (d === 3 || d === 4) out.push({ kind: 'decoy', theta: th, d: d, level: 3, seed: i + d });
      }
      out.push({ kind: 'quad', theta: th, d: 4, level: 4 });
      out.push({ kind: 'quad', theta: th, d: 5, level: 4 });
    }
    return out;
  }
  function spreadOrder(specs) {
    var b = {}, i;
    for (i = 0; i < specs.length; i++) { var k = specs[i].kind; (b[k] = b[k] || []).push(specs[i]); }
    var keys = Object.keys(b).sort(), out = [], row = 0, more = true;
    while (more) {
      more = false;
      for (var j = 0; j < keys.length; j++) {
        var arr = b[keys[(j + row) % keys.length]];
        if (row < arr.length) { out.push(arr[row]); more = true; }
      }
      row++;
    }
    return out;
  }
  function normPrompt(p) { return p.replace(/\([^)]*\)/g, '').replace(/\s*\d+$/, '').replace(/\s+/g, ' ').trim(); }
  function toProblem(r) {
    var answer = r.answer === 'pass' ? (r.d + ' cm라서 흘려보내기')
      : r.kind === 'quad' ? '변 ㄱㄴ과 평행하게 베기'
      : r.kind === 'perp' ? '줄무늬에 대한 수선으로 베기'
      : r.d + ' cm';
    return {
      id: r.id, prompt: r.prompt, choices: null, answer: answer,
      answerNumeric: r.kind === 'perp' ? 90 : (r.kind === 'quad' ? 0 : r.d),
      // 한 필드에 각도와 cm 가 섞이지 않게 단위를 함께 낸다(화면에는 나오지 않는 검증용 값이다)
      answerUnit: r.kind === 'perp' ? '도(직각)' : (r.kind === 'quad' ? '없음(평행 판단)' : 'cm'),
      unitConcept: r.unitConcept, level: r.level, kind: r.kind,
      geometry: { hoseAxisDeg: r.theta, perpendicularDeg: fold180(r.theta + 90), stripeGapCm: r.stripes >= 2 ? r.d : null,
                  toleranceDeg: TOL, decoyLengthCm: r.decoy ? r.decoy.len : null,
                  requiredAction: r.answer === 'pass' ? '줄무늬와 나란히 쓸어 흘려보내기' : '줄무늬에 직각으로 베기' },
      correctAction: correctAction(r),
      distractors: wrongActions(r).map(function (w) {
        return { misconceptionId: w.misconceptionId, action: w.action, explanation: w.explanation };
      })
    };
  }
  function sampleProblems(n) {
    n = Math.max(1, Math.min(400, n | 0 || 20));
    var order = spreadOrder(allSpecs()), out = [], seenId = {}, seenP = {}, rest = [];
    for (var i = 0; i < order.length && out.length < n; i++) {
      var r = buildRound(order[i]);
      if (seenId[r.id]) continue;
      seenId[r.id] = 1;
      var np = normPrompt(r.prompt);
      if (seenP[np]) { rest.push(r); continue; }
      seenP[np] = 1; out.push(toProblem(r));
    }
    for (var k = 0; k < rest.length && out.length < n; k++) out.push(toProblem(rest[k]));
    for (var m = 0; out.length < n; m++) out.push(toProblem(buildRound(order[m % order.length])));
    return out.slice(0, n);
  }

  var API = {
    SUB: SUB, TOL: TOL, HALF_W: HALF_W,
    dirOf: dirOf, fold180: fold180, angleBetween: angleBetween, segAngle: segAngle, hoseHalf: hoseHalf,
    buildRound: buildRound, geom: geom, judge: judge, deck: deck, practiceRound: practiceRound, practicePass: practicePass,
    correctAction: correctAction, wrongActions: wrongActions,
    sampleProblems: sampleProblems, allSpecs: allSpecs, toProblem: toProblem
  };
  if (typeof module === 'object' && module.exports) module.exports = API;
  root.CutStraightEngine = API;
})(typeof window !== 'undefined' ? window : globalThis);
