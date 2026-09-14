/* 각목 걸쳐 (gakmok-geolchyeo) — 순수 로직 엔진
 *
 * 판정은 **정수 각도**로만 한다. 담장 방향 θ, 각목 방향 φ 는 전부 정수 도(°)이고
 * 수선 판정은 `angDiff(φ, θ+90) ≤ 4` 라는 정수 비교다. 부동소수점 비교가 없다.
 * 거리 d 는 「담장 ㄷㄹ 은 담장 ㄱㄴ 을 법선으로 d cm 평행이동한 것」이라는 정의에서
 * 나오는 정수라, 화면에 음각되는 cm 는 잰 값이 아니라 정의값이다. 1 cm = 20 서브유닛.
 *
 * ── 교과서 대조 (아이스크림미디어 2022개정 4-2 4단원 「사각형」 1~4차시) ──
 *  · 발문은 익힘책 지시형 「~해 보세요」로 통일. 청유형(~해 봅시다)과 섞지 않는다.
 *  · 그림이 함께 나오는 발문 앞에는 「그림을 보고」를 붙인다.
 *  · 수선은 어느 직선에 대한 수선인지 밝힌다 → 「직선 ㄴㄷ에 대한 수선」.
 *  · 평행선 사이의 거리는 수선의 길이로만 잰다. 비스듬한 각목은 거리가 아니다.
 *  · ★포함관계★ 사다리꼴 = 평행한 변이 한 쌍이라도 있는 사각형. 45° 돌린 정사각형은
 *    사다리꼴·평행사변형·마름모·직사각형·정사각형 다섯 이름을 동시에 가진다.
 *    이름 고르기로 채점하지 않는다(표시 전용). 배타 분류 금지.
 *  · 어림(올림·버림·반올림) 발문은 이 단원에 없다. 생성하지 않는다.
 */
(function (root) {
  'use strict';

  var SUB = 20;              // 1 cm
  var TOL = 4;               // 수선 허용오차 ±4° (정수)
  var SWEEP_RANGE = 180;     // 각목이 훑는 각 폭 → 우연 수준 = 2*TOL/180 = 4.44 %

  var D2R = Math.PI / 180;
  function dirOf(deg) { return { x: Math.cos(deg * D2R), y: Math.sin(deg * D2R) }; }
  function angDiff(a, b) { var d = Math.abs(((a - b) % 360 + 360) % 360); return d > 180 ? 360 - d : d; }
  // 방향만 볼 때(직선의 기울기)는 180° 주기로 본다
  function angDiff180(a, b) { var d = angDiff(a, b); return d > 90 ? 180 - d : d; }

  function add(p, v, k) { return { x: p.x + v.x * k, y: p.y + v.y * k }; }

  // ── 발문 어휘 ────────────────────────────────────────────────────
  function tiltCue(theta) {
    var t = ((theta % 180) + 180) % 180;
    if (t === 0) return '담장이 가로로 곧게 서 있어요.';
    if (t === 90) return '담장이 세로로 곧게 서 있어요.';
    // 가로에서 벌어진 각 s 로 판단해 45°(오른쪽 아래)와 135°(오른쪽 위)가 같은 말을 듣게 한다
    var s = t < 90 ? t : 180 - t, side = t < 90 ? '오른쪽 아래로' : '오른쪽 위로';
    var how = s < 22 ? '아주 조금' : s < 45 ? '조금' : s < 68 ? '많이' : '거의 세로에 가깝게';
    return '담장이 ' + side + ' ' + how + ' 기울어 있어요.';
  }
  var NAMES = ['도윤', '하은', '민재'];

  var PAIR_TASK = '그림을 보고 두 담장이 평행하면 각목을 버팀목으로 세우고, 평행하지 않으면 치워 보세요.';
  var TICK_CUE = '각목 눈금 한 칸이 1 cm예요.';
  // 받침 있는 이름에만 「이」를 붙인다 (민재가 / 도윤이가)
  function withSubj(name) {
    var last = name.charCodeAt(name.length - 1);
    var hasBatchim = last >= 0xAC00 && last <= 0xD7A3 && (last - 0xAC00) % 28 !== 0;
    return name + (hasBatchim ? '이가' : '가');
  }

  /* spec 예:
     { kind:'perp'|'gap'|'debris'|'discard'|'order'|'shape',
       theta, d, side, skew, target, quadKind, level, seed } */
  function buildRound(spec) {
    var theta = ((spec.theta % 180) + 180) % 180;
    var d = spec.d;
    var u = dirOf(theta), n = dirOf(theta + 90);
    var C = { x: 0, y: 0 };                          // 놀이판 한가운데
    var r = {
      id: '', kind: spec.kind, level: spec.level || 1, theta: theta, d: d,
      wallA: null, wallB: null, point: null, quad: null, debris: null,
      hinge: null, correctDir: 0, sweepStart: 0, answer: 'lock', answerCm: d,
      labels: null, unitConcept: '', cue: tiltCue(theta), cue2: '', task: '', prompt: ''
    };

    // 담장 한 쌍은 한가운데를 기준으로 법선 방향 ±d/2 에 놓는다
    var pA = add(C, n, -d * SUB / 2), pB = add(C, n, d * SUB / 2);

    if (spec.kind === 'perp') {
      // 차시 1 — 점 ㄱ 에서 직선 ㄴㄷ 에 수선 내리기
      r.wallA = { p: pB, theta: theta, label: 'ㄴㄷ' };
      r.point = { x: pA.x, y: pA.y, label: 'ㄱ' };
      r.hinge = { x: pA.x, y: pA.y };
      r.correctDir = theta + 90;
      r.unitConcept = '수직과 수선';
      r.task = '그림을 보고 점 ㄱ에 걸린 각목이 직선 ㄴㄷ에 대한 수선이 되도록 맞춰 보세요.';
      r.cue2 = '각목은 점 ㄱ에 걸려 있어요.';
      r.id = 'perp-' + theta + '-' + d;
    } else if (spec.kind === 'gap' || spec.kind === 'debris' || spec.kind === 'order') {
      r.wallA = { p: pA, theta: theta, label: 'ㄱㄴ' };
      r.wallB = { p: pB, theta: theta, label: 'ㄷㄹ' };
      r.hinge = { x: pA.x, y: pA.y };
      r.correctDir = theta + 90;
      r.unitConcept = '평행선 사이의 거리';
      if (spec.kind === 'debris') {
        // 오류 찾기 — 이미 비스듬히 걸쳐 둔 각목이 하나 있다.
        // 길이를 정수 cm 로 못 박으려고 (세로 d, 가로 m) 을 피타고라스 쌍으로 잡는다:
        // (3,4)·(4,3) → 빗변 5 cm. 즉 「비스듬히 이으면 5 cm, 수선은 d cm」가 정확하다.
        var m = d === 3 ? 4 : 3;
        var slant = Math.round(Math.sqrt(d * d + m * m));
        var dFrom = add(pA, u, -1.4 * SUB);
        r.debris = { from: dFrom, to: add(add(dFrom, n, d * SUB), u, m * SUB), len: slant };
        r.slantCm = slant;
        r.who = NAMES[(spec.seed || 0) % 3];
        r.task = '그림을 보고 ' + withSubj(r.who) + ' 걸쳐 둔 각목 옆에 새 각목을 수선으로 걸쳐 보세요.';
        r.cue2 = '이미 걸쳐 둔 각목은 비스듬해서 ' + slant + ' cm예요.';
        r.id = 'debris-' + theta + '-' + d;
      } else if (spec.kind === 'order') {
        // 역추적 — 주문한 거리인 담장만 잠근다
        r.answer = d === spec.target ? 'lock' : 'sweep';
        r.target = spec.target;
        r.task = '그림을 보고 두 담장 사이의 거리가 ' + spec.target + ' cm인 곳에만 버팀목을 세우고, 아니면 치워 보세요.';
        r.cue2 = '두 담장은 평행해요. ' + TICK_CUE;
        r.id = 'order-' + theta + '-' + d + '-' + spec.target;
      } else {
        r.task = PAIR_TASK;
        r.cue2 = TICK_CUE;
        r.id = 'gap-' + theta + '-' + d;
      }
    } else if (spec.kind === 'discard') {
      // 평행이 아닌 담장 — 어떤 각으로 떨어뜨려도 양쪽에 동시에 직각일 수 없다
      r.wallA = { p: pA, theta: theta, label: 'ㄱㄴ' };
      r.wallB = { p: pB, theta: theta + spec.skew, label: 'ㄷㄹ' };
      r.hinge = { x: pA.x, y: pA.y };
      r.correctDir = theta + 90;
      r.answer = 'sweep';
      r.answerCm = 0;   // 평행하지 않은 두 직선에는 거리가 없다
      r.skew = spec.skew;
      r.unitConcept = '평행과 평행선';
      r.task = PAIR_TASK;
      r.cue2 = TICK_CUE;
      r.id = 'discard-' + theta + '-' + d + '-' + spec.skew;
    } else if (spec.kind === 'shape') {
      // 차시 4 — 닫힌 사각형 담장. 평행한 한 쌍 사이에 높이를 걸친다.
      var q = makeQuad(theta, d, spec.quadKind);
      r.quad = q.points; r.labels = q.labels;
      r.wallA = { p: q.baseP, theta: theta, label: 'ㄱㄴ' };
      r.wallB = { p: q.topP, theta: theta, label: 'ㄹㄷ' };
      r.hinge = { x: q.baseP.x, y: q.baseP.y };
      r.correctDir = theta + 90;
      r.unitConcept = q.isSquare ? '여러 가지 사각형' : '사다리꼴';
      r.task = '그림을 보고 변 ㄱㄴ과 변 ㄹㄷ 사이에 버팀목을 세워 두 변 사이의 거리를 알아보세요.';
      r.cue2 = '변 ㄱㄴ과 변 ㄹㄷ은 서로 평행해요.';
      r.id = 'shape-' + theta + '-' + d + '-' + spec.quadKind;
    }

    // 각목이 훑는 180° 창의 시작 각을 라운드마다 다르게 둔다 —
    // 정답 각이 늘 왕복의 같은 지점(끝점·한가운데)에 오면 각을 안 읽고 타이밍만 외운다.
    var phase = ((spec.seed || 0) * 47 + theta * 13 + d * 29) % 140 + 20;   // 20~159
    r.sweepStart = ((r.correctDir - phase) % 360 + 360) % 360;
    r.sweepRange = SWEEP_RANGE;
    r.correctDir = ((r.correctDir % 360) + 360) % 360;
    r.prompt = (r.cue + ' ' + r.cue2 + ' ' + r.task).replace(/\s+/g, ' ').trim();
    return r;
  }

  // 닫힌 사각형 — 평행한 한 쌍(밑변·윗변)은 항상 있고, 옆변만 바뀐다.
  function makeQuad(theta, d, quadKind) {
    var u = dirOf(theta), n = dirOf(theta + 90);
    var C = { x: 0, y: 0 };
    var baseP = add(C, n, -d * SUB / 2), topP = add(C, n, d * SUB / 2);
    var halfBase = quadKind === 'square' ? d * SUB / 2 : d * SUB * 0.8;
    var halfTop = quadKind === 'square' ? d * SUB / 2 : (quadKind === 'parallelogram' ? d * SUB * 0.8 : d * SUB * 0.45);
    var shift = quadKind === 'parallelogram' ? d * SUB * 0.45 : 0;
    var p1 = add(baseP, u, -halfBase), p2 = add(baseP, u, halfBase);
    var p3 = add(add(topP, u, halfTop), u, shift), p4 = add(add(topP, u, -halfTop), u, shift);
    var labels = quadKind === 'square'
      ? ['사다리꼴', '평행사변형', '마름모', '직사각형', '정사각형']
      : quadKind === 'parallelogram' ? ['사다리꼴', '평행사변형'] : ['사다리꼴'];
    return { points: [p1, p2, p3, p4], baseP: baseP, topP: topP, labels: labels, isSquare: quadKind === 'square' };
  }

  // ── 판정 ────────────────────────────────────────────────────────
  var GRAB_MSG = '각목을 잡은 채 밀어서 각을 맞추고 손을 떼 보세요.';
  function judge(round, action) {
    if (!action) return { ok: false, reason: 'refuse:none', msg: GRAB_MSG };
    if (action.type === 'sweep') {
      if (round.answer === 'sweep') {
        return { ok: true, reason: 'ok:swept',
                 msg: round.kind === 'discard'
                   ? '두 담장이 평행하지 않아 버팀목을 세울 수 없어요. 치워 냈어요.'
                   : '거리가 ' + round.d + ' cm라 주문한 ' + round.target + ' cm가 아니에요. 치워 냈어요.' };
      }
      return { ok: false, reason: 'miss:sweptGood',
               msg: round.kind === 'perp' ? '치울 담장이 아니에요. 점 ㄱ의 각목을 직선 ㄴㄷ과 직각이 되게 돌려 보세요.'
                  : round.kind === 'order' ? '주문한 ' + round.target + ' cm 골목이에요. 치우지 말고 버팀목을 세워 보세요.'
                  : '두 담장이 평행해서 버팀목을 세울 수 있는 곳이에요.' };
    }
    if (action.type !== 'tap') return { ok: false, reason: 'refuse:unknown', msg: GRAB_MSG };

    var phi = Math.round(action.dir);
    if (round.answer === 'sweep') {
      // 평행이 아닌 담(또는 주문과 다른 거리)에는 정답 각이 없다
      if (round.kind === 'discard') {
        var e1 = angDiff180(phi, round.theta + 90);
        var e2 = angDiff180(phi, round.wallB.theta + 90);
        return { ok: false, reason: 'miss:notParallel', errA: e1, errB: e2,
                 msg: '한쪽 담장에 직각으로 대면 다른 쪽 담장과는 직각에서 ' + Math.round(Math.abs(round.skew)) +
                      '°만큼 어긋나요. 두 담장이 평행하지 않아요.' };
      }
      return { ok: false, reason: 'miss:wrongOrder',
               msg: '이 골목은 ' + round.d + ' cm라 주문한 ' + round.target + ' cm가 아니에요. 빈 곳을 밀어 치워 보세요.' };
    }
    var err = angDiff180(phi, round.correctDir);
    if (err <= TOL) {
      // 직선으로는 수직이어도 각목 끝이 맞은편(직선 ㄴㄷ·담장 ㄷㄹ)이 아니라 제 담장 쪽을 향하면 걸치지 못한다
      if (angDiff(phi, round.correctDir) <= TOL) return { ok: true, reason: 'ok', cm: round.d, err: err };
      return { ok: false, reason: 'miss:reversed', err: err,
               msg: round.kind === 'perp'
                 ? '직각이지만 각목이 직선 ㄴㄷ 반대쪽을 향했어요. 직선 ㄴㄷ 쪽으로 걸쳐 보세요.'
                 : '직각이지만 각목이 반대쪽을 향했어요. 맞은편 담장 쪽으로 걸쳐 보세요.' };
    }
    // 빗나감의 근거는 「닿음」이 아니라 **각**이다 — 화면에 그 각을 호로 그리고 같은 수를 말한다.
    // 각목과 담장이 이루는 두 각(합 180°) 중 큰 쪽 = 90 + err (정수)
    var big = 90 + err;
    if (err >= 86) {
      // 각목이 (거의) 평행하면 「이룬 각」을 말할 수 없거나 화면 밖 먼 곳의 각이다.
      // err=90 은 평행한 두 직선 — 만나지 않으므로 각을 말하지 않는다(4-2 「평행」 정의).
      var far = round.kind === 'perp' ? '직선 ㄴㄷ' : '담장 ' + round.wallB.label;
      var pmsg = err === 90
        ? '각목이 ' + far + '과 평행해요. 평행한 두 직선은 아무리 늘여도 만나지 않아서 이루는 각이 없어요. ' +
          (round.kind === 'perp' ? '직선 ㄴㄷ과' : '두 담장과') + ' 직각이 되게 돌려 보세요.'
        : round.kind === 'perp'
          ? '각목이 직선 ㄴㄷ과 거의 평행해요. 늘이면 화면 밖 먼 곳에서 만나고, 이룬 각은 ' + big + '°예요. 직각이 되게 돌려 보세요.'
          : '각목이 담장과 거의 나란해요. 각목과 담장이 이룬 각은 ' + big + '°예요. 직각이 되게 돌려 보세요.';
      return { ok: false, reason: 'miss:parallel', err: err, angle: err === 90 ? null : big, msg: pmsg };
    }
    return { ok: false, reason: 'miss:notPerp', err: err, angle: big,
             msg: '각목과 담장이 이룬 각은 ' + big + '°예요. 직각(90°)보다 ' + err + '° 커요.' };
  }

  function correctAction(round) {
    if (round.answer === 'sweep') return { type: 'sweep' };
    return { type: 'tap', dir: round.correctDir };
  }
  function wrongActions(round) {
    var out = [];
    if (round.answer === 'sweep') {
      out.push({ misconceptionId: 'visible-noncrossing-is-parallel',
                 action: { type: 'tap', dir: round.correctDir },
                 explanation: round.kind === 'discard'
                   ? '한쪽 담장에 직각이어도 평행이 아니면 반대쪽에는 직각이 아니에요.'
                   : '거리를 재지 않고 잠그면 주문한 담장이 아니에요.' });
      return out;
    }
    // ① 「가로선·세로선처럼 보일 때만 수직」
    if (angDiff180(0, round.correctDir) > TOL)
      out.push({ misconceptionId: 'axis-looks-perpendicular', action: { type: 'tap', dir: 0 },
                 explanation: '화면 가로로 떨어뜨리면 기울어진 담장과 직각이 아니에요.' });
    if (angDiff180(90, round.correctDir) > TOL)
      out.push({ misconceptionId: 'axis-looks-perpendicular', action: { type: 'tap', dir: 90 },
                 explanation: '화면 세로로 떨어뜨리면 기울어진 담장과 직각이 아니에요.' });
    // ② 「비스듬히 이은 선분의 길이를 거리로 잰다」
    out.push({ misconceptionId: 'oblique-is-distance',
               action: { type: 'tap', dir: round.correctDir + 25 },
               explanation: '비스듬히 걸치면 담장과 이루는 각이 직각이 아니에요. 거리는 두 담장에 수직인 선분의 길이예요.' });
    // ③ 잠글 수 있는 담을 치워 버리는 오답
    out.push({ misconceptionId: 'discard-lockable-wall', action: { type: 'sweep' },
               explanation: '평행한 담장은 치우지 말고 걸쳐 잠가야 해요.' });
    return out;
  }

  // ── 본판 덱 ──────────────────────────────────────────────────────
  // ⚠️ 채점되는 8장에는 **축정렬(0°·90°) 담장을 쓰지 않는다.** 화면 가로·세로로만
  // 연타하는 봇이 축정렬 장을 그대로 통과해 첫 시도 정답률이 우연 수준을 넘는다.
  // 축정렬은 채점하지 않는 연습 1장 전용이다.
  // 앞 3장을 모두 「잠금」으로 두어, 무조건 치우는 봇은 걸쇠 3개를 다 잃고 뒤쪽
  // 치우기 장에 도달하지 못한다.
  function practiceRound() {
    var r = buildRound({ kind: 'gap', theta: 90, d: 4, level: 0, seed: 1 });
    r.practice = true;
    r.task = '각목을 잡은 채 밀어서 두 담장과 직각이 되게 맞춘 뒤 손을 떼 보세요.';
    r.cue2 = '두 담장은 평행해요.';
    r.prompt = (r.cue + ' ' + r.cue2 + ' ' + r.task).replace(/\s+/g, ' ').trim();
    return r;
  }
  // 판마다 새 덱 — 같은 담장을 외워 두 번째 판을 넘기지 못하게 한다.
  // 고정: 앞 3장(수선·거리·수선)은 늘 세우는 장, 마지막은 사각형, 축정렬 금지.
  // 4~7번째(거리·치우기·잔해·주문)는 **판마다 순서를 섞고**, 거리형 장은 반반으로 평행하지 않은
  // 담(8~15° 어긋남)으로 바뀐다 → 치우는 장이 한 판에 1~3장, 자리도 매번 달라
  // 「5·7번째는 치운다」 같은 위치 암기로 평행 판단을 건너뛸 수 없다.
  function rng(seed) {
    var a = seed | 0;
    return function () {
      a = a + 0x6D2B79F5 | 0;
      var t = Math.imul(a ^ a >>> 15, 1 | a);
      t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
      return ((t ^ t >>> 14) >>> 0) / 4294967296;
    };
  }
  var LOW = [15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75];       // 오른쪽 아래로 기운 담
  var HIGH = [105, 110, 115, 120, 125, 130, 135, 140, 145, 150, 155, 160, 165];
  function deck(seed) {
    if (seed == null) seed = Math.floor(Math.random() * 2147483647);
    var R = rng(seed), used = {};
    function pick(a) { return a[Math.floor(R() * a.length)]; }
    function th(pool) {                       // 한 판 안에서 같은 기울기가 겹치지 않게
      var t, k = 0;
      do { t = pick(pool); } while (used[t] && ++k < 20);
      used[t] = 1; return t;
    }
    var both = LOW.concat(HIGH), flip = R() < 0.5;
    var d7 = 3 + Math.floor(R() * 4), lock7 = R() < 0.5;
    var alt = [d7 - 2, d7 + 2, d7 - 1, d7 + 1].filter(function (x) { return x >= 3 && x <= 6; });
    var quadKind = pick(['trapezoid', 'parallelogram', 'square', 'square']);
    // 긴 밑변(6.4 cm)·옆으로 밀린 윗변이 세로 화면(390 px ≈ 10 cm 폭)을 넘지 않는 기울기만 쓴다
    // (바깥 벽돌 모서리까지 |x| ≤ 5 cm 인 각을 미리 계산해 둔 목록)
    var shapeTheta = th(quadKind === 'parallelogram' ? LOW.slice(1).concat([105, 110])
      : quadKind === 'trapezoid' ? [55, 60, 65, 70, 75, 105, 110, 115, 120, 125] : both);
    function skewed() {
      return { kind: 'discard', theta: th(both), d: 4 + Math.floor(R() * 3), skew: pick([8, 12, 15]), level: 3 };
    }
    var head = [
      { kind: 'perp', theta: th(flip ? LOW : HIGH), d: 3 + Math.floor(R() * 3), level: 1 },
      { kind: 'gap', theta: th(both), d: 3 + Math.floor(R() * 4), level: 2 },
      { kind: 'perp', theta: th(flip ? HIGH : LOW), d: 3 + Math.floor(R() * 4), level: 1 }
    ];
    var tail = [
      R() < 0.5 ? skewed() : { kind: 'gap', theta: th(both), d: 3 + Math.floor(R() * 4), level: 2 },
      skewed(),
      { kind: 'debris', theta: th(both), d: pick([3, 4]), level: 3 },
      { kind: 'order', theta: th(both), d: d7, target: lock7 ? d7 : pick(alt), level: 3 }
    ];
    for (var k = tail.length - 1; k > 0; k--) {
      var j = Math.floor(R() * (k + 1)), tmp = tail[k]; tail[k] = tail[j]; tail[j] = tmp;
    }
    var specs = head.concat(tail, [{ kind: 'shape', theta: shapeTheta, d: 4, quadKind: quadKind, level: 4 }]);
    return specs.map(function (s, i) {
      s.seed = (seed % 997) + i * 3;
      var r = buildRound(s);
      r.spec = s;
      return r;
    });
  }

  // ── 표본 ─────────────────────────────────────────────────────────
  function allSpecs() {
    var out = [], thetas = [0, 20, 30, 45, 60, 70, 90, 110, 120, 135, 150, 165], i;
    for (i = 0; i < thetas.length; i++) {
      var th = thetas[i];
      for (var d = 3; d <= 6; d++) {
        out.push({ kind: 'perp', theta: th, d: d, level: 1, seed: i + d });
        out.push({ kind: 'gap', theta: th, d: d, level: 2, seed: i + d });
        if (d === 3 || d === 4) out.push({ kind: 'debris', theta: th, d: d, level: 3, seed: i + d });
        out.push({ kind: 'order', theta: th, d: d, target: 4, level: 3, seed: i + d });
        out.push({ kind: 'order', theta: th, d: d, target: 5, level: 3, seed: i + d + 1 });
      }
      for (var s = 0; s < 3; s++)
        out.push({ kind: 'discard', theta: th, d: 4 + s, skew: [8, 12, 15][s], level: 3, seed: i + s });
      out.push({ kind: 'shape', theta: th, d: 4, quadKind: 'trapezoid', level: 4, seed: i });
      out.push({ kind: 'shape', theta: th, d: 4, quadKind: 'parallelogram', level: 4, seed: i + 1 });
      out.push({ kind: 'shape', theta: th, d: 4, quadKind: 'square', level: 4, seed: i + 2 });
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
    var answer = r.answer === 'sweep'
      ? (r.kind === 'discard' ? '평행하지 않으므로 치워 내기' : r.d + ' cm라서 치워 내기')
      : (r.kind === 'perp' ? '직선 ㄴㄷ에 수직인 선분의 길이 ' + r.d + ' cm' : r.d + ' cm');
    return {
      id: r.id, prompt: r.prompt, choices: null,
      answer: answer,
      // 평행하지 않은 담장에는 거리가 없으므로 0 으로 둔다(화면에는 나오지 않는다)
      answerNumeric: r.kind === 'discard' ? 0 : r.d,
      unitConcept: r.unitConcept, level: r.level, kind: r.kind,
      geometry: { wallAngleDeg: r.theta, perpendicularDeg: r.correctDir, gapCm: r.d,
                  skewDeg: r.skew || 0, toleranceDeg: TOL, sweepRangeDeg: SWEEP_RANGE,
                  slantLengthCm: r.slantCm || null, quadNames: r.labels || null },
      correctAction: correctAction(r),
      distractors: wrongActions(r).map(function (w) {
        return { misconceptionId: w.misconceptionId, action: w.action, explanation: w.explanation };
      })
    };
  }
  function sampleProblems(n) {
    n = Math.max(1, Math.min(400, n | 0 || 20));
    var order = spreadOrder(allSpecs()), out = [], seenId = {}, seenP = {}, rest = [];
    // 발문이 같은 유형(gap/discard)이 통째로 빠지지 않게 유형마다 한 개씩 먼저 담는다
    var kinds = {};
    for (var q = 0; q < order.length && out.length < n; q++) {
      if (kinds[order[q].kind]) continue;
      kinds[order[q].kind] = 1;
      var r0 = buildRound(order[q]);
      seenId[r0.id] = 1; seenP[normPrompt(r0.prompt)] = 1;
      out.push(toProblem(r0));
    }
    // 표본의 80 % 는 서로 다른 발문으로 채워 다양성을 확보하고(게이트 70 %),
    // 나머지 20 % 는 유형 균형을 위해 발문이 겹치더라도 남은 유형에서 채운다.
    var uniqQuota = Math.ceil(n * 0.8);
    for (var i = 0; i < order.length && out.length < uniqQuota; i++) {
      var r = buildRound(order[i]);
      if (seenId[r.id]) continue;
      seenId[r.id] = 1;
      var np = normPrompt(r.prompt);
      if (seenP[np]) { rest.push(r); continue; }
      seenP[np] = 1; out.push(toProblem(r));
    }
    for (var i2 = 0; i2 < order.length && out.length < n; i2++) {
      var r2 = buildRound(order[i2]);
      if (seenId[r2.id]) continue;
      seenId[r2.id] = 1;
      out.push(toProblem(r2));
    }
    for (var k = 0; k < rest.length && out.length < n; k++) out.push(toProblem(rest[k]));
    for (var m = 0; out.length < n; m++) out.push(toProblem(buildRound(order[m % order.length])));
    return out.slice(0, n);
  }

  var API = {
    SUB: SUB, TOL: TOL, SWEEP_RANGE: SWEEP_RANGE,
    dirOf: dirOf, angDiff: angDiff, angDiff180: angDiff180,
    buildRound: buildRound, judge: judge, deck: deck, practiceRound: practiceRound,
    correctAction: correctAction, wrongActions: wrongActions,
    sampleProblems: sampleProblems, allSpecs: allSpecs, toProblem: toProblem
  };
  if (typeof module === 'object' && module.exports) module.exports = API;
  root.GakmokEngine = API;
})(typeof window !== 'undefined' ? window : globalThis);
