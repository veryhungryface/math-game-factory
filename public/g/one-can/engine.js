/* OneCan: the rendering-free, integer-mL play state used by the actual rail.
 * Textbook 3-2, unit 5, lessons 1/2/4. Expression traps copied as rules:
 * Use L and mL (never alternate case), a space between numbers and units,
 * and 1 L = 1000 mL. Carry/borrow at 1000, never 10 or 100. Use 들이.
 * All task prompts use the workbook instruction ending ~해 보세요.
 * No rounding questions; rounding would require the TO_PLACE convention.
 * Inclusive <= is the physical fit test. A legal cup that cannot be finished
 * with the printed cup allowance strands the can; that is a resource failure,
 * not false math. Noncanonical amounts such as 1 L 1100 mL are never answers.
 *
 * Rack contract (2026-09-08, replaces the rank-readable rack):
 *   Every can is filled by exactly one unordered pair of cups. The third cup
 *   never completes the can with either partner, and its value sits below /
 *   between / above the pair with equal probability, so the answer carries no
 *   rank signal. The replacement cup after a pour is placed on the far side of
 *   the leftover cup two thirds of the time, which makes the answer's rank at
 *   the last drop uniform as well. A policy that reads only the three numbers
 *   therefore scores the chance rate 2/9 per can, never better.
 */
(function (root) {
  'use strict';
  const LITER = 1000;
  const MAX_DROPS = 2;
  const STEP = 50;
  const MIN_CUP = 50;
  const MAX_CUP = 1200;
  function rng(seed) {
    let a = (Number(seed) >>> 0) || 1;
    return function () {
      a += 0x6D2B79F5;
      let t = a;
      t = Math.imul(t ^ (t >>> 15), t | 1);
      t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }
  function shuffle(a, random) {
    const out = a.slice();
    for (let i = out.length - 1; i > 0; i--) {
      const j = Math.floor(random() * (i + 1));
      const v = out[i]; out[i] = out[j]; out[j] = v;
    }
    return out;
  }
  function fmt(ml, mixed) {
    if (!mixed || ml < LITER) return ml + ' mL';
    return Math.floor(ml / LITER) + ' L' + (ml % LITER ? ' ' + (ml % LITER) + ' mL' : '');
  }
  function localTarget(s) { return Math.min(LITER, s.problem.targetMl - s.orderShipped * LITER); }
  function need(s) { return localTarget(s) - s.canMl; }

  // 1. The fixed playable loop precedes the generator: tap 500, tap 500.
  // Two cups is also the real allowance, so the practice can teaches the rule.
  const tutorialProblem = {
    id: 'tutorial-500-500', kind: 'fill', targetMl: LITER, canStartMl: 0,
    addends: [], level: 1, prompt: '1 L를 담아 보세요', unitConcept: '1 L = 1000 mL'
  };
  let cupSerial = 0;
  function create(seed, options) {
    const random = rng(seed);
    const s = {
      seed: Number(seed) >>> 0, random, phase: 'playing', score: 0, lives: 3,
      level: 1, solved: 0, shipped: 0, streak: 0, bestStreak: 0,
      elapsed: 0, maxTime: 90, frozen: !(options && options.tutorial === false),
      tutorialStep: 0, canMl: 0, orderShipped: 0, maxDrops: MAX_DROPS,
      dropsLeft: MAX_DROPS, currentOrderIndex: 0, attemptsOpened: 0,
      firstAttempts: [], currentFirstRecorded: false, firstChance: 0,
      lastFailure: null, lastEvent: null, rail: [],
      decks: {}, deckPositions: {}, problem: tutorialProblem
    };
    if (s.frozen) s.rail = [{ ml: 500, shapeSeed: 0.45, slot: 0, uid: ++cupSerial, role: 'solution' }];
    else openOrder(s);
    return s;
  }
  function recordFirst(s, correct, reason) {
    if (s.frozen || s.currentFirstRecorded) return;
    s.currentFirstRecorded = true;
    s.firstAttempts.push({
      id: s.problem.id, order: s.currentOrderIndex,
      correct: !!correct, reason: reason || (correct ? 'complete' : 'overflow'),
      chance: s.firstChance
    });
  }
  function finish(s, win) {
    if (s.phase !== 'playing') return;
    if (!win) recordFirst(s, false, s.lives === 0 ? 'lives' : 'timeout');
    s.phase = win ? 'clear' : 'gameover';
    s.frozen = false;
  }
  function tick(s, dt) {
    if (s.phase !== 'playing' || s.frozen) return;
    s.elapsed = Math.min(s.maxTime, s.elapsed + Math.max(0, Number(dt) || 0));
    // The first two normal orders remain introductory even after a long pause.
    const available = s.currentOrderIndex < 2 ? 1 : s.currentOrderIndex < 4 ? 2 : 3;
    const timeLevel = s.elapsed >= 60 ? 3 : s.elapsed >= 25 ? 2 : 1;
    s.level = Math.max(s.problem.level, Math.min(available, timeLevel));
    if (s.elapsed >= s.maxTime) finish(s, false);
  }
  function remember(s, event) { s.lastEvent = event; return event; }
  function tap(s, index) {
    if (s.phase !== 'playing' || !Number.isInteger(index) || !s.rail[index]) return { type: 'ignored' };
    const before = s.canMl;
    const amount = s.rail[index].ml;
    if (s.frozen) {
      s.canMl += amount;
      s.tutorialStep++;
      s.score += 10;
      if (s.tutorialStep === 1) {
        s.rail = [{ ml: 500, shapeSeed: 0.65, slot: 0, uid: ++cupSerial, role: 'solution' }];
        return remember(s, { type: 'pour', tutorial: true, before, amount, after: 500,
          message: '반 통이에요. 컵을 한 번 더 넣어요.' });
      }
      s.score += 40;
      s.frozen = false;
      const event = { type: 'lid', tutorial: true, before, amount, after: LITER,
        shipped: false, orderComplete: true,
        message: '500 mL + 500 mL = 1000 mL = 1 L · 연습 통은 출고에 세지 않아요. 이제 8통을 출고해요.' };
      openOrder(s);
      return remember(s, event);
    }
    const capacity = localTarget(s);
    const free = capacity - before;
    if (amount > free) return failure(s, before, amount, 'overflow', free, capacity);
    // Every physically legal pour is accepted as addition, including a legal
    // cup that leaves a gap the remaining allowance cannot close.
    s.canMl += amount;
    s.dropsLeft--;
    s.score += 10;
    let event = { type: 'pour', before, amount, after: s.canMl,
      mathematicalCorrect: true, dropsLeft: s.dropsLeft,
      message: before + ' mL + ' + amount + ' mL = ' + s.canMl + ' mL' };
    if (s.canMl === capacity) {
      const full = capacity === LITER;
      event.type = full ? 'lid' : 'order';
      event.shipped = full;
      if (full) {
        s.shipped++;
        s.orderShipped++;
        s.streak++;
        s.bestStreak = Math.max(s.bestStreak, s.streak);
        s.score += 40 + Math.min(3, s.streak) * 10;
        event.message += ' = 1 L';
      }
      const total = s.orderShipped * LITER + (full ? 0 : s.canMl);
      if (total === s.problem.targetMl) {
        recordFirst(s, true);
        s.solved++;
        event.orderComplete = true;
        event.completedProblem = s.problem;
        event.totalMl = total;
        s.score += 20;
        if (s.shipped >= 8) finish(s, true);
        else { s.currentOrderIndex++; openOrder(s); }
      } else {
        // Closing a can never splits a cup across two cans.
        s.canMl = 0;
        s.dropsLeft = MAX_DROPS;
        fillRail(s);
      }
      return remember(s, event);
    }
    if (s.dropsLeft === 0) return failure(s, before, amount, 'allowance', free, capacity, true);
    advanceRail(s, index);
    return remember(s, event);
  }
  function failure(s, before, amount, reason, free, capacity, legal) {
    recordFirst(s, false, reason);
    s.lives--;
    s.streak = 0;
    const after = legal ? s.canMl : before + amount;
    const missing = legal ? capacity - after : 0;
    const overflow = legal ? 0 : amount - free;
    const message = overflow + ' mL가 넘쳤어요. 담을 수 있는 양은 ' + free + ' mL였어요.';
    const event = { type: 'wrong', reason, mathematicalCorrect: !!legal,
      before, amount, after, free, missing, overflow, capacity,
      totalMissing: legal ? s.problem.targetMl - s.orderShipped * LITER - after : 0,
      message: legal ? '컵 ' + MAX_DROPS + '개를 모두 썼어요. 이번 통에 ' + missing + ' mL가 부족해요.' : message };
    s.lastFailure = Object.assign({ orderId: s.problem.id }, event);
    if (s.lives === 0) finish(s, false);
    else {
      s.canMl = s.orderShipped === 0 ? s.problem.canStartMl : 0;
      s.dropsLeft = MAX_DROPS;
      fillRail(s);
    }
    return remember(s, event);
  }

  // 2. The full pool is constructed from valid integer places, without random
  // generate-and-reject loops. Every target leaves each can at least 300 mL to
  // fill, which is what lets every can use the two-cup rack below.
  const MIN_CAN_NEED = 300;
  const pool = [];
  function addProblem(kind, level, targetMl, canStartMl, addends, extra) {
    let prompt;
    if (kind === 'reverse_complement') prompt = fmt(canStartMl) + '가 담겨 있어요. 1 L가 되게 담아 보세요';
    else if (kind === 'add_two') prompt = fmt(addends[0], true) + '와 ' + fmt(addends[1], true) + '를 한데 담아 보세요';
    else if (kind === 'subtract_fill') prompt = fmt(addends[0], true) + '에서 ' + fmt(addends[1], true) + '를 덜어 낸 뒤 남은 양을 담아 보세요';
    else prompt = fmt(targetMl, kind !== 'fill') + '를 담아 보세요';
    const id = [kind, targetMl, canStartMl, addends.join('-')].join(':');
    pool.push(Object.freeze(Object.assign({ id, kind, level, targetMl, canStartMl,
      addends, prompt,
      unitConcept: kind === 'add_two' ? '들이의 덧셈' : kind === 'subtract_fill' ? '들이의 뺄셈' : kind === 'reverse_complement' ? '1 L가 되는 들이' : '들이의 단위 바꾸기'
    }, extra || {})));
  }
  // Introductory orders always include a real 1 L lid event.
  addProblem('fill', 1, LITER, 0, []);
  for (let hundreds = 1; hundreds <= 7; hundreds++) addProblem('reverse_complement', 1, LITER, hundreds * 100, []);
  for (let hundreds = 3; hundreds <= 9; hundreds++) {
    for (let fifty = 0; fifty <= 1; fifty++) addProblem('convert_fill', 2, LITER + hundreds * 100 + fifty * 50, 0, []);
  }
  // Backward construction guarantees one carry: remainders sum to 1300..1400.
  for (let left = 300; left <= 900; left += 50) {
    for (let extra = 300; extra <= 400; extra += 50) {
      const right = LITER - left + extra;
      if (right <= 950) addProblem('add_two', 3, LITER + left + right, 0, [LITER + left, right]);
    }
  }
  // Borrowing is structural: a remainder r is smaller than removed remainder b.
  for (let r = 0; r <= 350; r += 50) {
    for (let b = r + 100; b <= 850; b += 100) {
      const a = 2 * LITER + r;
      if ((a - b) % LITER >= MIN_CAN_NEED) addProblem('subtract_fill', 3, a - b, 0, [a, b]);
    }
  }
  const groups = {
    fill: pool.filter(p => p.kind === 'fill'),
    reverse: pool.filter(p => p.kind === 'reverse_complement'),
    mixed: pool.filter(p => p.kind === 'convert_fill'),
    add: pool.filter(p => p.kind === 'add_two'),
    subtract: pool.filter(p => p.kind === 'subtract_fill')
  };
  function drawProblem(s, key) {
    if (!s.decks[key] || s.deckPositions[key] >= s.decks[key].length) {
      s.decks[key] = shuffle(groups[key], s.random); s.deckPositions[key] = 0;
    }
    return s.decks[key][s.deckPositions[key]++];
  }
  function openOrder(s) {
    let key;
    const i = s.currentOrderIndex;
    // If only one shipment remains, end on a one-can order. This keeps the
    // visible 8-can win target exact instead of completing a 2-can order at 9/8.
    if (s.shipped === 7) key = 'reverse';
    else if (i === 0) key = 'fill';
    else if (i === 1) key = 'reverse';
    else if (i < 4) key = 'mixed';
    else key = i % 3 === 1 ? 'subtract' : 'add';
    s.problem = drawProblem(s, key);
    s.level = s.problem.level;
    s.canMl = s.problem.canStartMl;
    s.orderShipped = 0;
    s.dropsLeft = MAX_DROPS;
    s.currentFirstRecorded = false;
    s.attemptsOpened++;
    s.firstChance = chanceOfProblem(s.problem);
    fillRail(s);
  }

  // 3. The rack. Every can is closed by exactly one pair of cups; the third cup
  // completes the can with neither partner. Because the odd cup sits below /
  // between / above the pair with equal probability, and because the cup that
  // replaces a poured one is biased to the far side of whatever cup is left,
  // the answer's rank is uniform at both drops. Reading the numbers is the only
  // policy that beats 2/9 per can.
  function ladder(lo, hi) {
    const out = [];
    for (let v = Math.max(MIN_CUP, Math.ceil(lo / STEP) * STEP); v <= Math.min(MAX_CUP, hi); v += STEP) out.push(v);
    return out;
  }
  function pickFrom(list, random) { return list[Math.floor(random() * list.length)]; }
  // x >= 100 keeps a legal-but-stranding value open strictly between y and the
  // requirement; y - x >= 100 keeps one open strictly between x and y.
  function splitOptions(required) {
    const out = [];
    for (let x = 100; x * 2 < required; x += STEP) {
      const y = required - x;
      if (y <= MAX_CUP && y - x >= 100) out.push([x, y]);
    }
    return out;
  }
  function buildRack(required, random) {
    const splits = splitOptions(required);
    const round = splits.filter(p => p[0] % 100 === 0 && required % 100 === 0);
    const opts = (round.length && random() < 0.8) ? round : splits;
    const pair = pickFrom(opts, random);
    const x = pair[0], y = pair[1];
    const place = ['below', 'between', 'above'][Math.floor(random() * 3)];
    let d;
    if (place === 'below') d = pickFrom(ladder(MIN_CUP, x - STEP), random);
    else if (place === 'between') d = pickFrom(ladder(x + STEP, y - STEP), random);
    else {
      // Above both solution cups: half the time still pourable (it strands the
      // can), half the time an overflow cup so the spill lesson stays alive.
      const strand = ladder(y + STEP, required - STEP);
      const spill = ladder(required + 100, required + 500);
      d = (strand.length && random() < 0.7) ? pickFrom(strand, random) : pickFrom(spill, random);
    }
    return [
      { ml: x, role: 'solution' },
      { ml: y, role: 'solution' },
      { ml: d, role: d > required ? 'spill' : 'strand',
        misconceptionId: d > required ? 'ignore-remaining-space' : 'ignore-cup-allowance' }
    ];
  }
  // Replacement cup for the last drop. It never closes the can. Its side is
  // chosen so that, over both first-drop branches, the answer is the smallest /
  // middle / largest cup one third of the time each.
  function refillCup(s) {
    const required = need(s);
    const taken = new Set(s.rail.map(c => c.ml));
    const low = ladder(MIN_CUP, required - STEP).filter(v => !taken.has(v));
    const high = ladder(required + STEP, required + 500).filter(v => !taken.has(v));
    const rivals = s.rail.filter(c => c.ml !== required);
    let wantHigh;
    if (rivals.length === 1) wantHigh = rivals[0].ml < required ? s.random() < 1 / 3 : s.random() >= 1 / 3;
    else wantHigh = s.random() < 0.5;
    let bag = wantHigh ? high : low;
    if (!bag.length) bag = wantHigh ? low : high;
    const ml = bag.length ? pickFrom(bag, s.random) : required + 100;
    return { ml, role: ml > required ? 'spill' : 'strand',
      misconceptionId: ml > required ? 'ignore-remaining-space' : 'ignore-cup-allowance' };
  }
  // Both the array order and the rail slot are shuffled: neither the index a
  // tap uses nor the position a cup rides in tells the answer apart.
  function seat(s, cups) {
    const slots = shuffle([0, 1, 2], s.random);
    return shuffle(cups, s.random)
      .map((c, i) => Object.assign({ shapeSeed: s.random(), uid: ++cupSerial }, c, { slot: slots[i] }));
  }
  function fillRail(s) { s.rail = seat(s, buildRack(need(s), s.random)); }
  // A pour consumes only the cup that was tapped: the other two keep their
  // identity (and their mL) so the rack can be planned before the first tap.
  function advanceRail(s, index) {
    const kept = s.rail.filter((c, i) => i !== index);
    s.rail = kept;
    const fresh = Object.assign({ shapeSeed: s.random(), uid: ++cupSerial }, refillCup(s));
    s.rail = seat(s, kept.concat([fresh]));
  }
  function correctIndex(s) {
    if (s.phase !== 'playing') return -1;
    if (s.frozen) return 0;
    // For QA only; the UI never reads role, correctIndex, or this hint.
    const required = need(s);
    const exact = s.rail.findIndex(c => c.ml === required);
    if (exact >= 0) return exact;
    if (s.dropsLeft < 2) return -1;
    return s.rail.findIndex((c, i) => c.ml < required &&
      s.rail.some((o, j) => j !== i && o.ml === required - c.ml));
  }
  // A can is a two-cup rack: 2 of 3 cups open it, then 1 of 3 closes it.
  const CAN_CHANCE = (2 / 3) * (1 / 3);
  function cansOf(p) {
    const out = [];
    let left = p.targetMl, start = p.canStartMl;
    while (left > 0) { const cap = Math.min(LITER, left); out.push(cap - start); left -= cap; start = 0; }
    return out;
  }
  function chanceOfProblem(p) { return Math.pow(CAN_CHANCE, cansOf(p).length); }
  function chanceOfOrder(s) { return s.firstChance; }

  // These are the ticket answers, not an additional player answer path.
  // The actual rail accepts all <=space pours; sample choices describe the
  // uniquely required total (or missing quantity for a reverse ticket).
  function answerValue(p) { return p.kind === 'reverse_complement' ? p.targetMl - p.canStartMl : p.targetMl; }
  function candidates(p) {
    const answer = answerValue(p);
    const liters = Math.floor(p.targetMl / LITER);
    const remainder = p.targetMl % LITER;
    let raw;
    if (p.kind === 'add_two') {
      const inputLiters = p.addends.reduce((sum, v) => sum + Math.floor(v / LITER), 0);
      const sumMl = p.addends.reduce((sum, v) => sum + v % LITER, 0);
      raw = [
        { value: 100 * inputLiters + sumMl, misconceptionId: 'liter-as-100', formula: '100*inputLiters+sumMl' },
        { value: LITER * inputLiters + sumMl % LITER, misconceptionId: 'discard-carry-liter', formula: '1000*inputLiters+(sumMl%1000)' },
        { value: LITER * (inputLiters + Math.floor(sumMl / 100)) + sumMl % 100, misconceptionId: 'carry-base-100', formula: '1000*(inputLiters+floor(sumMl/100))+(sumMl%100)' },
        { value: answer + LITER, misconceptionId: 'carry-liter-twice', formula: '1000*(inputLiters+2)+(sumMl%1000)' }
      ];
    } else if (p.kind === 'subtract_fill') {
      const aLiters = Math.floor(p.addends[0] / LITER);
      const aRem = p.addends[0] % LITER;
      const b = p.addends[1];
      raw = [
        { value: (aLiters - 1) * LITER + aRem + 100 - b, misconceptionId: 'borrow-base-100', formula: '(aLiters-1)*1000+aRem+100-b' },
        { value: aLiters * LITER + Math.abs(aRem - b), misconceptionId: 'subtract-small-from-large-without-borrow', formula: 'aLiters*1000+abs(aRem-b)' },
        { value: aLiters * LITER + aRem + LITER - b, misconceptionId: 'borrow-without-reducing-liter', formula: 'aLiters*1000+aRem+1000-b' }
      ];
    } else if (p.kind === 'reverse_complement') {
      // Reverse problems require genuine complement misconceptions instead of
      // arbitrary nearby numbers. The first two values are backward-calculated
      // from the chosen plan's 1 L = 100 mL and base-10 misconceptions.
      raw = [
        { value: 100 + p.canStartMl, misconceptionId: 'liter-as-100', formula: '100+start' },
        { value: LITER - p.canStartMl / 10, misconceptionId: 'carry-base-10', formula: '1000-start/10' },
        { value: LITER + p.canStartMl, misconceptionId: 'add-instead-of-complement', formula: '1000+start' },
        { value: LITER, misconceptionId: 'ignore-existing-amount', formula: '1000' },
        { value: p.canStartMl, misconceptionId: 'copy-existing-amount', formula: 'start' }
      ];
    } else {
      raw = [
        { value: 100 * liters + remainder, misconceptionId: 'liter-as-100', formula: '100*L+mL' },
        { value: 10 * liters + remainder, misconceptionId: 'liter-as-10', formula: '10*L+mL' },
        { value: remainder, misconceptionId: 'ignore-liter-part', formula: 'mL' },
        { value: (liters + 1) * LITER + remainder, misconceptionId: 'count-liter-twice', formula: '(L+1)*1000+mL' }
      ];
    }
    // Required order: answer removal -> range -> dedupe -> fill -> shuffle.
    const seen = new Set([answer]);
    const wrong = raw.filter(c => c.value !== answer)
      .filter(c => Number.isInteger(c.value) && c.value > 0 && c.value <= 10000)
      .filter(c => { if (seen.has(c.value)) return false; seen.add(c.value); return true; });
    for (let add = 200; wrong.length < 3; add += 100) {
      if (!seen.has(answer + add)) { seen.add(answer + add); wrong.push({ value: answer + add, misconceptionId: 'extra-amount', formula: 'answer+' + add }); }
    }
    return wrong.slice(0, 3);
  }
  let sampleCounter = 0;
  function sampleProblems(n) {
    n = Math.max(0, Math.floor(Number(n) || 0));
    const random = rng(0xC0A12000 + sampleCounter++);
    let deck = shuffle(pool, random);
    const out = [];
    for (let i = 0; i < n; i++) {
      if (i && i % pool.length === 0) deck = shuffle(pool, random);
      const p = deck[i % pool.length];
      const answerNumeric = answerValue(p);
      const wrong = candidates(p);
      out.push(Object.assign({}, p, {
        prompt: p.prompt,
        choices: shuffle([answerNumeric].concat(wrong.map(c => c.value)), random).map(v => fmt(v)),
        answer: fmt(answerNumeric), answerNumeric,
        distractors: wrong.map(c => Object.assign({ text: fmt(c.value) }, c))
      }));
    }
    return out;
  }
  root.OneCan = Object.freeze({ create, tap, tick, sampleProblems, correctIndex,
    pool: Object.freeze(pool), fmt, need, localTarget, buildRack, splitOptions,
    candidates, answerValue, chanceOfOrder, chanceOfProblem, cansOf,
    MAX_DROPS, CAN_CHANCE, LITER });
})(typeof globalThis !== 'undefined' ? globalThis : this);
