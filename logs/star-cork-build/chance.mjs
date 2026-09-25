/** Independent null model: for each presented candy choose shoot/pass with
 * probability 1/2. Repair first chooses remove/keep for each existing star,
 * followed by shoot/pass for each belt star in order. Equality ends the attempt
 * immediately; future decisions are marginalized, not counted as extra trials.
 * This is NOT final-subset-count / 2^N: successful prefixes also count.
 */
export function exactPrefixChance({ target, initial = [], belt = [], submitAtEnd = false }) {
  const events = initial.map(value => ({ delta: -value, removing: true }))
    .concat(belt.map(value => ({ delta: value, removing: false })));
  const initialSum = initial.reduce((a, b) => a + b, 0);
  const memo = new Map();
  function walk(i, total) {
    if (i === events.length) return submitAtEnd && total === target ? 1n : 0n;
    const key = `${i}:${total}`;
    if (memo.has(key)) return memo.get(key);
    const event = events[i];
    let yes;
    const next = total + event.delta;
    if (next === target && !submitAtEnd) yes = 1n << BigInt(events.length - i - 1);
    else if (!event.removing && next > target) yes = 0n;
    else yes = walk(i + 1, next);
    const result = walk(i + 1, total) + yes;
    memo.set(key, result);
    return result;
  }
  const numerator = walk(0, initialSum);
  const denominator = 1n << BigInt(events.length);
  return { numerator: String(numerator), denominator: String(denominator), probability: Number(numerator) / Number(denominator), eventCount: events.length, states: memo.size };
}

export function nullSubsetChance({ target, initial = [], belt = [] }) {
  let sums = new Map([[0, 1n]]);
  for (const value of initial.concat(belt)) {
    const next = new Map(sums);
    for (const [sum, n] of sums) next.set(sum + value, (next.get(sum + value) || 0n) + n);
    sums = next;
  }
  const numerator = sums.get(target) || 0n;
  const denominator = 1n << BigInt(initial.length + belt.length);
  return Number(numerator) / Number(denominator);
}
