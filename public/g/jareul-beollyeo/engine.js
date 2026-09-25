/* 자를 벌려 — exact model shared by the playable bench and QA.
 * Textbook expression_traps, applied here:
 * - Workbook instructions use 해 보세요, never mix 봅시다 / 하시오.
 * - Label vertices ㄱ ㄴ ㄷ; distinct objects 가 나 다. Always identify 그림.
 * - SI length has a space: 5 cm. No comma grouping for numbers.
 * - Error-finding says 잘못 댄 자, never 틀린 것을 고르세요.
 * - Answer form must be explicit; this activity answers with measured edges/angles.
 * - No rounding is used: if added it must be 반올림하여 ○의 자리까지 (TO_PLACE),
 *   never 자리에서. Bounds 이상/이하 include endpoints, 초과/미만 exclude them.
 * - No fractions, decimal arithmetic, division stories, ratios, probability names,
 *   or circle/pi questions are in this lesson pool; their expression traps do not apply.
 * Unit caveats: equilateral IS isosceles; acute requires ALL THREE positive dot signs;
 * right is separate from acute/obtuse; strict integer triangle inequalities hold.
 * Pixels and Math.sqrt below are drawing only. Equality and angle sign use integers.
 */
(function (global) {
  'use strict';
  const LABELS = ['ㄱ', 'ㄴ', 'ㄷ'];
  const EDGE_VERTICES = [[0, 1], [1, 2], [2, 0]];
  const MISCONCEPTIONS = {
    equilateralExcluded: '정삼각형은 이등변삼각형이 아니라고 생각한다',
    oneAcute: '예각이 하나만 있으면 예각삼각형이라고 생각한다',
    orientation: '방향이나 길쭉함으로 삼각형의 종류를 판단한다',
    wrongApex: '밑변을 같은 두 변 중 하나라고 생각한다'
  };
  function rng(seed) { let a = Number(seed) >>> 0; return function () { a += 0x6D2B79F5; let t = a; t = Math.imul(t ^ t >>> 15, t | 1); t ^= t + Math.imul(t ^ t >>> 7, t | 61); return ((t ^ t >>> 14) >>> 0) / 4294967296; }; }
  function shuffle(a, random) { const b = a.slice(); for (let i = b.length - 1; i > 0; i--) { const j = Math.floor(random() * (i + 1)); [b[i], b[j]] = [b[j], b[i]]; } return b; }
  function signs(s) { return [s[0] * s[0] + s[2] * s[2] - s[1] * s[1], s[0] * s[0] + s[1] * s[1] - s[2] * s[2], s[1] * s[1] + s[2] * s[2] - s[0] * s[0]]; }
  function classify(s) { const d = signs(s); return { side: s[0] === s[1] && s[1] === s[2] ? 'equilateral' : (s[0] === s[1] || s[1] === s[2] || s[2] === s[0] ? 'isosceles' : 'scalene'), angle: d.some(n => n < 0) ? 'obtuse' : d.some(n => n === 0) ? 'right' : 'acute' }; }
  const NAMES = { acute: '예각삼각형', right: '직각삼각형', obtuse: '둔각삼각형', equilateral: '정삼각형', isosceles: '이등변삼각형', scalene: '세 변의 길이가 모두 다른 삼각형' };
  const POOLS = { iso: [], scalene: [], equilateral: [], acute: [], right: [], obtuse: [], isoObtuse: [], isoAcute: [] };
  // Finite constraint-satisfaction pool: choose the repeated length before the base.
  // Every base is in 2..2*equal-1, so no invalid random triangle is ever retried.
  for (let equal = 4; equal <= 14; equal++) {
    for (let base = Math.max(3, Math.ceil(equal * 0.65)); base <= Math.floor(equal * 1.7); base++) {
      if (base !== equal) POOLS.iso.push([equal, base, equal]);
    }
    POOLS.equilateral.push([equal, equal, equal]);
  }
  // Ascending unequal side lengths and c<a+b structurally guarantee validity.
  for (let a = 3; a <= 11; a++) for (let b = a + 1; b <= 14; b++) for (let c = b + 1; c <= Math.min(17, a + b - 1); c++) POOLS.scalene.push([a, b, c]);
  // Triangle inequality alone still admits slivers (3-14-16 has an 8.6도 corner):
  // they draw as a line, so the learner cannot see three sides to compare, and the
  // set square cannot be seated in a corner narrower than the +-8도 alignment window.
  // Keep every corner at 18도 or wider: still clearly three sides, and the acute pool
  // (the only mode that seats the square in every corner) then bottoms out at 23.6도.
  const MIN_CORNER_DEG = 18;
  function minCorner(s) {
    const d = signs(s), adj = [[s[0], s[2]], [s[0], s[1]], [s[1], s[2]]];
    return Math.min(...d.map((n, v) => Math.acos(n / (2 * adj[v][0] * adj[v][1])) * 180 / Math.PI));
  }
  POOLS.iso = POOLS.iso.filter(s => minCorner(s) >= MIN_CORNER_DEG);
  POOLS.scalene = POOLS.scalene.filter(s => minCorner(s) >= MIN_CORNER_DEG);
  for (const s of [...POOLS.iso, ...POOLS.scalene, ...POOLS.equilateral]) {
    const k = classify(s); POOLS[k.angle].push(s);
    if (k.side === 'isosceles') POOLS[k.angle === 'obtuse' ? 'isoObtuse' : 'isoAcute'].push(s);
  }
  function geometry(s) {
    const x = (s[0] * s[0] - s[2] * s[2]) / (2 * s[1]);
    const h = Math.sqrt(s[0] * s[0] - (x + s[1] / 2) ** 2);
    const centerX = x / 3;
    return [{ x: x - centerX, y: -2 * h / 3 }, { x: -s[1] / 2 - centerX, y: h / 3 }, { x: s[1] / 2 - centerX, y: h / 3 }];
  }
  function equalPairs(s) { const out = []; for (let a = 0; a < 3; a++) for (let b = a + 1; b < 3; b++) if (s[a] === s[b]) out.push([a, b]); return out; }
  function acceptsPiece(p) { const k = classify(p.sides); if (p.mode === 'equilateral') return k.side === 'equilateral'; if (p.mode === 'combined') return k.side !== 'scalene' && k.angle === p.angleTarget; return p.mode === 'angle' || k.side !== 'scalene'; }
  function edgeName(i) { return LABELS[EDGE_VERTICES[i][0]] + LABELS[EDGE_VERTICES[i][1]]; }
  function promptFor(p) {
    if (p.mode === 'combined') return `그림을 보고, 이등변이면서 ${p.angleTarget === 'obtuse' ? '둔각' : '예각'}인 삼각형을 자와 삼각자로 확인해 보세요`;
    if (p.mode === 'angle') return '그림을 보고, 큰 각부터 삼각자로 확인해 보세요';
    if (p.mode === 'equilateral') return '그림을 보고, 정삼각형의 세 변에 같은 너비의 자를 대 보세요';
    if (p.variant === 'reverse') return '같은 각 표시를 보고, 같은 두 변에 자를 대 보세요';
    if (p.variant === 'error_find') return '그림을 보고, 잘못 댄 자를 고쳐 보세요';
    return '그림을 보고, 같은 두 변에 자를 대 보세요';
  }
  function makePiece(sides, cfg) {
    const s = sides.slice(), k = classify(s), pairs = equalPairs(s), d = signs(s);
    const opposite = [s[1], s[2], s[0]], longest = Math.max(...opposite);
    const arcs = pairs.length ? pairs.length === 3 ? [0, 1, 2] : pairs[0].map(e => (e + 2) % 3) : [];
    const p = Object.assign({ id: '', sides: s, vertices: geometry(s), labels: LABELS.slice(), rotation: 0, flipped: false, mode: 'side', variant: 'normal', level: 1, markings: false, arcs: [], sideType: k.side, angleType: k.angle, angleSigns: d, maxVertices: [0, 1, 2].filter(v => opposite[v] === longest), equalPairs: pairs, angleTarget: k.angle, locked: -1, checkedEdges: [], measuredVertices: [], lengthVerified: false, teeth: 0, attempted: false, failed: false, complete: false }, cfg || {});
    if (p.variant === 'reverse') p.arcs = arcs;
    if (p.variant === 'error_find') p.locked = pairs.length === 1 ? [0, 1, 2].find(e => !pairs[0].includes(e)) : 0;
    p.prompt = promptFor(p); return p;
  }
  function fixedPiece() { return makePiece([5, 4, 5], { id: 'tutorial-5-5-4', markings: true, tutorial: true }); }
  class Source {
    constructor(random) { this.random = random; this.bags = {}; this.serial = 0; }
    take(name) { let b = this.bags[name]; if (!b || !b.length) b = this.bags[name] = shuffle(POOLS[name], this.random); return b.pop().slice(); }
    piece(pool, cfg) { const sides = shuffle(this.take(pool), this.random); return makePiece(sides, Object.assign({ id: 'timber-' + (++this.serial), rotation: 15 * Math.floor(this.random() * 24), flipped: this.random() < 0.5 }, cfg)); }
    deck() {
      // Draw each side job independently at 3:1 rather than dealing a fixed 3-iso,
      // 1-scalene bag. A bag is sampled without replacement, so solving one job tells
      // a blind player what is left: after a correct pair compare on an isosceles the
      // next job is scalene with probability 1/3, not 1/4, and the fixed
      // lock->compare->compare->discard cycle lands its discard exactly there. That
      // coupling, not any single route, is what pushed cyclic input measurably above
      // chance (rate 27.05% vs 25.96%, z=+4.81 over 20,000 runs). Independent draws
      // keep the same 3:1 long-run mix while making every job uninformative about the
      // next. Edge order and job position stay randomized; the tutorial stays fixed.
      const early = [0, 1, 2, 3].map(() => this.random() < 0.75 ? 'iso' : 'scalene');
      let isoSeen = 0;
      const out = early.map((pool, i) => {
        const cfg = { task: i + 1, level: i < 2 ? 1 : 2, mode: 'side', variant: 'normal' };
        if (pool === 'iso') {
          if (isoSeen === 0) cfg.markings = true;
          else cfg.variant = isoSeen === 1 ? 'reverse' : 'error_find';
          isoSeen++;
        }
        return this.piece(pool, cfg);
      });
      // A blind "measure all three edges" routine completes every equilateral piece,
      // so its share is what a brainless bot scores here. Uniform intent over the four
      // routes (three pairs + discard) scores 3/4 on an accepted piece and 1/4 on a
      // rejected one, so staying at or under chance needs equilateral <= 1/2. A true
      // 2:2 bag sits exactly on that ceiling and matches the lesson ("정삼각형 6·6·6과
      // 이등변 6·6·4가 동시에 올라온다") better than the old 3:1 bag did.
      // Same reasoning: two jobs dealt from a four-card bag would leak across the pair.
      const equalJob = () => this.random() < 0.5 ? 'equilateral' : 'iso';
      out.push(this.piece(equalJob(), { task: 5, level: 3, mode: 'equilateral' }));
      out.push(this.piece(equalJob(), { task: 6, level: 3, mode: 'equilateral' }));
      // Include relation: three equal sides must be accepted by a two-equal-sides task.
      // Always shipping an equilateral here made every pair route correct and discard
      // always wrong, so "never discard" scored 100% against a 75% chance ceiling --
      // the single largest brainless-bot leak in the deck. Pairing it 1:1 with a
      // scalene keeps the inclusion case a real decision (accept or push away) and
      // puts the band exactly on chance; the misconception still gets its own job.
      const includeJob = this.random() < 0.5 ? 'equilateral' : 'scalene';
      out.push(this.piece(includeJob, { task: 7, level: 3, mode: 'side', variant: 'include' }));
      const anglePool = ['acute', 'right', 'obtuse'][Math.floor(this.random() * 3)];
      out.push(this.piece(anglePool, { task: 8, level: 4, mode: 'angle' }));
      for (let i = 9; i <= 10; i++) {
        const target = i === 9 ? 'obtuse' : 'acute';
        const good = this.random() < 0.875;
        const pool = good ? (target === 'obtuse' ? 'isoObtuse' : 'isoAcute') : 'scalene';
        out.push(this.piece(pool, { task: i, level: 5, mode: 'combined', angleTarget: target }));
      }
      return out;
    }
  }
  function solutionSequences(p) {
    if (!acceptsPiece(p)) return [[{ type: 'discard' }]];
    const angleSteps = p.angleType === 'acute' ? [0, 1, 2].map(vertex => ({ type: 'measureAngle', vertex })) : [{ type: 'measureAngle', vertex: p.maxVertices[0] }];
    if (p.mode === 'angle') return [angleSteps];
    const pairs = p.mode === 'equilateral' ? [[0, 1], [0, 2], [1, 2]] : p.equalPairs;
    const routes = [];
    for (const pair of pairs) for (const order of [pair, pair.slice().reverse()]) {
      const route = p.locked >= 0 ? [{ type: 'unlock' }] : [];
      route.push({ type: 'lock', edge: order[0] }, { type: 'compare', edge: order[1] });
      if (p.mode === 'equilateral') route.push({ type: 'compare', edge: [0, 1, 2].find(e => !order.includes(e)) });
      if (p.mode === 'combined') route.push(...angleSteps);
      routes.push(route);
    }
    return routes;
  }
  function sampleOne(p, random) {
    const pairs = equalPairs(p.sides), k = classify(p.sides);
    let answer, numeric;
    if (!acceptsPiece(p)) { answer = '삼각형을 통으로 밀기'; numeric = 0; }
    else if (p.mode === 'angle') { answer = k.angle === 'acute' ? '세 각을 모두 삼각자로 재기' : `${LABELS[p.maxVertices[0]]}의 가장 큰 각에 삼각자 대기`; numeric = k.angle === 'acute' ? 3 : 1; }
    else if (p.mode === 'equilateral') { answer = '같은 너비로 세 변 모두 확인하기'; numeric = p.sides[0]; }
    else { answer = `${pairs.map(pair => pair.map(edgeName).join('·')).join(' 또는 ')}에 같은 너비의 자 대기${p.mode === 'combined' ? ' 후 삼각자로 각 확인하기' : ''}`; numeric = p.sides[pairs[0][0]]; }
    const correct = { value: answer, misconceptionId: null, truth: true };
    // Each distractor is a concrete false assertion derived from a known misconception.
    // Remove correct -> validate -> deduplicate -> fill -> shuffle, in that order.
    const raw = [
      { value: k.side === 'equilateral' ? '세 변이 같아도 이등변삼각형에서 빼기' : k.side === 'isosceles' ? '같은 두 변만 확인하고 정삼각형으로 정하기' : '세 변이 달라도 이등변삼각형으로 정하기', misconceptionId: 'equilateralExcluded', truth: false },
      { value: k.angle !== 'acute' ? '예각 하나만 보고 예각삼각형으로 정하기' : '세 각이 예각인데 둔각삼각형으로 정하기', misconceptionId: 'oneAcute', truth: false },
      { value: '돌리기만 하면 삼각형의 종류가 바뀐다고 정하기', misconceptionId: 'orientation', truth: false },
      { value: '서로 다른 길이인 두 변을 같다고 정하기', misconceptionId: 'wrongApex', truth: false }
    ];
    const seen = new Set([answer]);
    const wrong = raw.filter(x => x.value !== answer).filter(x => typeof x.value === 'string' && x.value.length > 0 && x.truth === false).filter(x => { if (seen.has(x.value)) return false; seen.add(x.value); return true; }).slice(0, 3);
    const choices = shuffle([correct, ...wrong], random);
    return { id: p.id, prompt: `${p.prompt}. 변 ㄱㄴ ${p.sides[0]} cm, 변 ㄴㄷ ${p.sides[1]} cm, 변 ㄷㄱ ${p.sides[2]} cm.`, choices: choices.map(x => x.value), answer, answerNumeric: numeric, unitConcept: p.mode === 'combined' ? '두 가지 기준으로 삼각형 분류하기' : p.mode === 'angle' ? '예각삼각형과 직각삼각형과 둔각삼각형' : '이등변삼각형과 정삼각형', distractors: wrong, actionSolutions: solutionSequences(p), piece: p };
  }
  class DividerGame {
    constructor(options) {
      this.options = options || {}; this.seed = this.options.seed === undefined ? Date.now() : this.options.seed; this.random = rng(this.seed); this.source = new Source(this.random); this.deck = []; this.cursor = 0; this.serial = 0; this.runCount = 0;
      const s = this.state = { phase: 'title', tutorial: true, frozen: true, score: 0, lives: 3, solved: 0, level: 1, timeLeft: 90, elapsed: 0, combo: 0, bestCombo: 0, pieces: [], piece: null, selectedIndex: 0, awaitingNext: false, feedback: { kind: 'idle', text: '같은 변에 자를 옮겨 대요', serial: 0 }, firstAttempt: { attempted: 0, correct: 0 }, attempts: [], scarCount: 0, eventSerial: 0 };
      for (const key of ['locked', 'checkedEdges', 'measuredVertices', 'lengthVerified', 'teeth']) Object.defineProperty(s, key, { enumerable: true, get() { return s.piece ? s.piece[key] : key === 'locked' ? -1 : ['checkedEdges', 'measuredVertices'].includes(key) ? [] : 0; }, set(v) { if (s.piece) s.piece[key] = v; } });
    }
    start(opts) {
      const s = this.state, skip = opts === true || !!(opts && opts.skipTutorial);
      Object.assign(s, { phase: 'playing', tutorial: !skip, frozen: !skip, score: 0, lives: 3, solved: 0, level: 1, timeLeft: 90, elapsed: 0, combo: 0, bestCombo: 0, pieces: [], piece: null, selectedIndex: 0, awaitingNext: false, firstAttempt: { attempted: 0, correct: 0 }, attempts: [], scarCount: 0 });
      this.random = rng((Number(this.seed) + this.runCount++ * 0x9E3779B9) >>> 0); this.source = new Source(this.random); this.deck = this.source.deck(); this.cursor = 0;
      if (skip) this.spawn(); else { s.pieces = [fixedPiece()]; this.selectPiece(0); }
      return this.notice('start', skip ? s.piece.prompt : '같은 변에 자를 옮겨 대요');
    }
    group(p) { return p.task <= 4 ? 'side' : p.task <= 6 ? 'equilateral' : p.task === 7 ? 'include' : p.task === 8 ? 'angle' : 'combined'; }
    refill() {
      const s = this.state; if (s.tutorial || s.phase !== 'playing' || !s.pieces.length) return;
      const group = this.group(s.pieces[0]);
      const capacity = group === 'equilateral' || group === 'combined' || (group === 'side' && (s.elapsed >= 20 || s.scarCount > 0 || s.pieces.some(p => p.level >= 2))) ? 2 : 1;
      while (s.pieces.length < capacity && this.cursor < this.deck.length && this.group(this.deck[this.cursor]) === group) {
        s.pieces.push(this.deck[this.cursor++]); s.eventSerial++;
      }
    }
    spawn() {
      const s = this.state;
      if (!s.pieces.length && this.cursor < this.deck.length) s.pieces.push(this.deck[this.cursor++]);
      this.refill(); this.selectPiece(0); return s.piece;
    }
    selectPiece(index) {
      const s = this.state;
      if (s.awaitingNext || !s.pieces[index] || s.pieces[index].complete) return false;
      s.selectedIndex = index;
      s.piece = s.pieces[index];
      s.level = s.piece.level;
      // Feedback is part of the visible prompt. Keeping the previous piece's
      // text here can ask for an obtuse triangle while grading an acute one.
      this.notice('ready', s.piece.prompt);
      return true;
    }
    notice(kind, text, detail) { const s = this.state; s.feedback = Object.assign({ kind, text, serial: ++this.serial }, detail || {}); s.eventSerial++; return Object.assign({ ok: kind !== 'wrong' && kind !== 'refuse', kind }, detail || {}); }
    canAct() { const s = this.state; return s.phase === 'playing' && s.piece && !s.awaitingNext; }
    lock(edge) {
      const s = this.state, p = s.piece; if (!this.canAct()) return { ok: false, kind: 'busy' };
      if (!Number.isInteger(edge) || edge < 0 || edge > 2) return this.notice('refuse', '자 끝을 꼭짓점에 대 보세요');
      if (p.mode === 'angle' || (p.mode === 'combined' && p.lengthVerified)) return this.notice('refuse', '지금은 삼각자를 대 보세요');
      if (p.locked !== -1) return this.notice('refuse', '나사를 풀면 너비를 다시 잴 수 있어요');
      p.locked = edge; p.checkedEdges = [edge]; return this.notice('locked', `${p.sides[edge]} cm로 잠겼어요 · 같은 길이의 다른 변에 옮겨 대 보세요`, { edge });
    }
    unlock() { if (!this.canAct()) return { ok: false, kind: 'busy' }; const p = this.state.piece; p.locked = -1; p.checkedEdges = []; p.lengthVerified = false; return this.notice('unlocked', '다시 한 변에 자를 대 보세요'); }
    beginAttempt() { const s = this.state, p = s.piece; if (!s.tutorial && !p.attempted) { p.attempted = true; s.firstAttempt.attempted++; s.attempts.push({ id: p.id, task: p.task, firstCorrect: null }); } }
    compare(edge) {
      const s = this.state, p = s.piece; if (!this.canAct()) return { ok: false, kind: 'busy' };
      if (p.mode === 'angle' || (p.mode === 'combined' && p.lengthVerified)) return this.notice('refuse', '지금은 삼각자를 대 보세요');
      if (p.locked < 0) return this.notice('refuse', '먼저 한 변에 자를 대 보세요');
      if (!Number.isInteger(edge) || edge < 0 || edge > 2) return this.notice('refuse', '두 끝을 꼭짓점에 대 보세요');
      if (edge === p.locked || p.checkedEdges.includes(edge)) return this.notice('refuse', '아직 재지 않은 다른 변에 옮겨 대 보세요');
      this.beginAttempt();
      if (p.sides[p.locked] !== p.sides[edge]) return this.wrong(`${p.sides[p.locked]} cm와 ${p.sides[edge]} cm는 달라요 · 나사를 풀고 다시 재 보세요`, 'wrongApex');
      p.checkedEdges.push(edge);
      if (p.mode === 'equilateral') {
        if (p.checkedEdges.length < 3) return this.notice('partial', '두 변은 같아요 · 정삼각형은 남은 한 변도 같아야 해요', { edge });
        return this.correct(`세 변이 ${p.sides[0]} cm로 같아요 · 정삼각형의 세 각은 60°예요`);
      }
      p.lengthVerified = true;
      if (p.mode === 'combined') return this.notice('partial', '같은 두 변을 확인했어요 · 이제 삼각자로 각을 재 보세요', { edge, tool: 'set-square' });
      return this.correct(`${p.sides[p.locked]} cm와 ${p.sides[edge]} cm가 같아요 · 이등변삼각형${p.sideType === 'equilateral' ? '에 정삼각형도 포함돼요' : ''}`, { edge });
    }
    discard() {
      if (!this.canAct()) return { ok: false, kind: 'busy' }; this.beginAttempt(); const p = this.state.piece;
      if (acceptsPiece(p)) return this.wrong(p.sideType === 'equilateral' && p.mode === 'side' ? '세 변이 같으면 두 변도 같아요 · 정삼각형도 이등변삼각형이에요' : '이 삼각형은 남겨서 자로 확인해 보세요', 'equilateralExcluded');
      return this.correct(p.mode === 'combined' ? `이 삼각형은 ${NAMES[p.angleType]}이고 ${NAMES[p.sideType]}이에요 · 두 조건을 모두 만족하지 않아요` : p.mode === 'equilateral' ? '세 변의 길이가 모두 같지는 않아요 · 정삼각형이 아니에요' : '세 변의 길이가 모두 달라요');
    }
    measureAngle(vertex) {
      const s = this.state, p = s.piece; if (!this.canAct()) return { ok: false, kind: 'busy' };
      if (p.mode !== 'angle' && p.mode !== 'combined') return this.notice('refuse', '지금은 같은 두 변을 자로 재 보세요');
      if (p.mode === 'combined' && !p.lengthVerified) return this.notice('refuse', '먼저 같은 두 변에 자를 옮겨 대 보세요');
      if (!Number.isInteger(vertex) || vertex < 0 || vertex > 2) return this.notice('refuse', '삼각자의 직각 모서리를 꼭짓점에 대 보세요');
      if (p.measuredVertices.includes(vertex)) return this.notice('refuse', '아직 재지 않은 각에 삼각자를 대 보세요');
      this.beginAttempt();
      if (p.angleType !== 'acute' && !p.maxVertices.includes(vertex)) return this.wrong('이 각은 예각이에요 · 가장 큰 각을 다시 살펴보세요', 'oneAcute');
      p.measuredVertices.push(vertex);
      if (p.mode === 'combined' && !acceptsPiece(p)) return this.wrong(`이 삼각형은 ${NAMES[p.angleType]}이에요 · 두 조건을 모두 확인해 보세요`, 'oneAcute');
      if (p.angleType === 'acute' && p.measuredVertices.length < 3) return this.notice('partial', `이 각은 직각보다 작아요 · 남은 ${3 - p.measuredVertices.length}개의 각도 재 보세요`, { vertex, tool: 'set-square' });
      return this.correct(p.angleType === 'acute' ? '세 각이 모두 직각보다 작아요 · 예각삼각형' : p.angleType === 'right' ? '가장 큰 각이 직각에 꼭 맞아요 · 직각삼각형' : '가장 큰 각이 직각보다 커요 · 둔각삼각형', { vertex });
    }
    wrong(reason, misconceptionId) {
      const s = this.state, p = s.piece; if (!this.canAct()) return { ok: false, kind: 'busy' };
      if (s.tutorial) { p.locked = -1; p.checkedEdges = []; return this.notice('guide', reason || '같은 길이의 두 변에 다시 대 보세요', { reveal: true }); }
      this.beginAttempt(); p.failed = true; p.teeth++; s.scarCount++; s.lives--; s.combo = 0; this.refill();
      const record = s.attempts.find(x => x.id === p.id); if (record && record.firstCorrect === null) record.firstCorrect = false;
      s.awaitingNext = true;
      this.notice('wrong', reason || '길이가 맞지 않아요 · 같은 삼각형을 다시 잴 수 있어요', { misconceptionId: misconceptionId || 'wrongApex', reveal: true });
      if (s.lives <= 0) { s.lives = 0; s.phase = 'gameover'; s.frozen = true; s.awaitingNext = false; }
      return { ok: false, kind: 'wrong' };
    }
    correct(text, detail) {
      const s = this.state, p = s.piece; if (!this.canAct()) return { ok: false, kind: 'busy' };
      this.beginAttempt();
      if (s.tutorial) { p.complete = true; s.score += 100; s.awaitingNext = true; return this.notice('correct', text, Object.assign({ tutorial: true, reveal: true }, detail)); }
      if (!p.failed) { s.firstAttempt.correct++; const record = s.attempts.find(x => x.id === p.id); if (record) record.firstCorrect = true; }
      p.complete = true; s.solved++; s.combo++; s.bestCombo = Math.max(s.bestCombo, s.combo); s.score += 100 * s.combo; s.awaitingNext = true;
      this.notice('correct', text, Object.assign({ reveal: true, stamp: s.combo % 3 === 0 }, detail));
      if (s.solved >= 10) { s.phase = 'clear'; s.frozen = true; s.awaitingNext = false; }
      return { ok: true, kind: 'correct' };
    }
    next() {
      const s = this.state; if (s.phase !== 'playing' || !s.awaitingNext) return false;
      s.awaitingNext = false;
      if (s.tutorial && s.piece.complete) { s.tutorial = false; s.frozen = false; s.pieces = []; this.spawn(); }
      else if (s.piece.complete) { s.pieces.splice(s.selectedIndex, 1); if (s.pieces.length) { this.refill(); this.selectPiece(0); } else this.spawn(); }
      else { s.piece.locked = -1; s.piece.checkedEdges = []; s.piece.measuredVertices = []; s.piece.lengthVerified = false; }
      this.notice('ready', s.piece.prompt); return true;
    }
    tick(dt) { const s = this.state; if (s.phase !== 'playing' || s.frozen) return; const seconds = Math.max(0, Number(dt) || 0); s.elapsed += seconds; s.timeLeft = Math.max(0, 90 - s.elapsed); this.refill(); if (s.timeLeft <= 0) { const preserveReveal = s.awaitingNext && (s.feedback.kind === 'wrong' || s.feedback.kind === 'correct'); s.phase = 'gameover'; s.frozen = true; s.awaitingNext = false; if (!preserveReveal) this.notice('timeout', '시간이 끝났어요 · 확인한 삼각형만 기록해요'); } }
    forceCorrect() {
      if (this.state.phase === 'title') this.start({ skipTutorial: true });
      if (this.state.phase !== 'playing') return false;
      if (this.state.awaitingNext) this.next();
      const p = this.state.piece; if (!acceptsPiece(p)) return this.discard();
      if (p.mode !== 'angle') { this.unlock(); const pair = p.equalPairs[0]; this.lock(pair[0]); this.compare(pair[1]); if (p.mode === 'equilateral' && !p.complete) this.compare([0, 1, 2].find(e => !p.checkedEdges.includes(e))); }
      if (!p.complete && (p.mode === 'angle' || p.mode === 'combined')) { if (p.angleType === 'acute') for (const v of [0, 1, 2]) { if (!p.measuredVertices.includes(v)) this.measureAngle(v); } else this.measureAngle(p.maxVertices[0]); }
      return this.state.score;
    }
    forceWrong() { if (this.state.phase === 'title') this.start({ skipTutorial: true });
      if (this.state.phase !== 'playing') return false; if (this.state.awaitingNext) this.next(); if (this.state.tutorial) { this.state.tutorial = false; this.state.frozen = false; } return this.wrong('두 변의 길이가 맞지 않아요 · 다시 재 보세요'); }
    sampleProblems(n) { const count = Math.max(0, Math.floor(Number(n) || 0)); const random = rng((this.seed ^ 0xABC012) + this.serial++); const src = new Source(random); const rows = []; while (rows.length < count) for (const p of src.deck()) { rows.push(sampleOne(p, random)); if (rows.length === count) break; } return rows; }
    getState() { const s = this.state; return { score: s.score, lives: s.lives, level: s.level, phase: s.phase, solved: s.solved, tutorial: s.tutorial, frozen: s.frozen, timeLeft: s.timeLeft, combo: s.combo, firstAttempt: Object.assign({}, s.firstAttempt), pieceId: s.piece && s.piece.id, locked: s.locked, checkedEdges: s.checkedEdges.slice(), measuredVertices: s.measuredVertices.slice(), eventSerial: s.eventSerial }; }
  }
  Object.assign(DividerGame, { POOLS, MISCONCEPTIONS, EDGE_VERTICES, LABELS, signs, classify, geometry, equalPairs, acceptsPiece, makePiece, fixedPiece, Source, sampleOne, solutionSequences, rng, shuffle });
  global.DividerGame = DividerGame;
})(typeof window !== 'undefined' ? window : globalThis);
