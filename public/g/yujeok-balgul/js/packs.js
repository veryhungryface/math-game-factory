// 협곡 사수 v3 팩 로더 (이 게임 폴더의 사본 packs/ 만 읽는다).
// 런타임은 choice 4지선다만 쓴다. 구형 amount·fraction_parts 문항은 건너뛴다.

let INDEX = null;
const cache = new Map();

export async function loadIndex() {
  if (INDEX) return INDEX;
  const r = await fetch('./packs/index.json');
  INDEX = await r.json();
  return INDEX;
}

export function packEntry(id) {
  return INDEX?.packs.find((p) => p.pack_id === id) || null;
}

function gcd(a, b) {
  a = Math.abs(a);
  b = Math.abs(b);
  while (b) [a, b] = [b, a % b];
  return a || 1;
}

/** 정수 기약 비교용 값(문자열 보기 → 분수/정수 키). 비교 불가면 null */
export function choiceKey(s) {
  const t = String(s).trim();
  let m = t.match(/^\{frac:(-?\d+)\/(\d+)\}$/);
  if (m) {
    const n = +m[1];
    const d = +m[2];
    if (!d) return null;
    const g = gcd(n, d);
    return `${n / g}/${d / g}`;
  }
  m = t.match(/^(-?\d+)$/);
  if (m) return `${+m[1]}/1`;
  return null;
}

function validItem(it) {
  const mode = it.answer_mode || 'choice';
  if (mode !== 'choice') return false;
  if (!Array.isArray(it.choices) || it.choices.length !== 4) return false;
  if (!it.choices.map(String).includes(String(it.answer))) return false;
  if (new Set(it.choices.map(String)).size !== 4) return false;
  // 같은 값의 보기(2/8 과 1/4)가 둘이면 정답이 둘이 된다 → 제외
  const keys = it.choices.map(choiceKey).filter(Boolean);
  if (new Set(keys).size !== keys.length) return false;
  return typeof it.prompt === 'string' && it.prompt.length > 1;
}

export async function loadPack(id) {
  await loadIndex();
  const e = packEntry(id) || packEntry(INDEX.default_pack);
  if (cache.has(e.pack_id)) return cache.get(e.pack_id);
  const r = await fetch('./packs/' + e.file);
  const raw = await r.json();
  const items = (raw.items || []).filter(validItem);
  const pack = { ...raw, entry: e, items };
  cache.set(e.pack_id, pack);
  return pack;
}

const shuffle = (a, rnd = Math.random) => {
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(rnd() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
};

/** 원정 10문항: 1~3번 난이도1, 4~6번 2, 7~8번 3, 9~10번 4 (없으면 가까운 난이도) — 중복 없음 */
export function pickExpedition(pack, n = 10, recent = new Set()) {
  const plan = [1, 1, 1, 2, 2, 2, 3, 3, 4, 4].slice(0, n);
  const used = new Set();
  const pools = {};
  for (const it of pack.items) (pools[it.difficulty || 1] ||= []).push(it);
  for (const k in pools) {
    shuffle(pools[k]);
    // 최근에 본 문항은 뒤로
    pools[k].sort((a, b) => (recent.has(a.id) ? 1 : 0) - (recent.has(b.id) ? 1 : 0));
  }
  const out = [];
  for (const d of plan) {
    let got = null;
    for (const dd of [d, d - 1, d + 1, d - 2, d + 2, d - 3, d + 3]) {
      const pool = pools[dd];
      if (!pool) continue;
      got = pool.find((it) => !used.has(it.id));
      if (got) break;
    }
    if (!got) got = pack.items[Math.floor(Math.random() * pack.items.length)];
    used.add(got.id);
    out.push(got);
  }
  return out;
}

export function easiest(pack) {
  const d1 = pack.items.filter((it) => (it.difficulty || 1) === 1);
  const pool = d1.length ? d1 : pack.items;
  return pool[Math.floor(Math.random() * Math.min(pool.length, 8))];
}

export function sampleItems(pack, n) {
  const pool = shuffle(pack.items.slice());
  const out = [];
  for (let i = 0; i < n; i++) out.push(pool[i % pool.length]);
  return out;
}

export { shuffle };
