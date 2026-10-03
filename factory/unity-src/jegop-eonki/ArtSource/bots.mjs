// 제곱 얹기 — 무뇌 봇 시뮬레이션 (C# Judge/Deck 과 같은 규칙).
function mulberry(a) {
  return function () {
    a |= 0; a = a + 0x6D2B79F5 | 0;
    let t = Math.imul(a ^ a >>> 15, 1 | a);
    t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
    return ((t ^ t >>> 14) >>> 0) / 4294967296;
  };
}
function rndInt(r, n) { return Math.floor(r() * n); }

const Tri = [[3,4,5],[5,12,13],[6,8,10],[8,15,17],[7,24,25],[9,12,15],[20,21,29]];
function isRight(a,b,c) {
  const s = [a,b,c].sort((x,y)=>x-y);
  return s[0]*s[0] + s[1]*s[1] === s[2]*s[2];
}
function completes(s, tray, area) {
  const n = tray===0?s.a:tray===1?s.b:s.c;
  return area === n*n && tray === s.targetSide && area === s.targetArea;
}

function makeDeck(seed) {
  const r = mulberry(seed);
  const pick = (small) => {
    const rows = small ? [0,2,5] : [...Array(Tri.length).keys()];
    const row = rows[rndInt(r, rows.length)];
    const k = small ? 1 : (rndInt(r,3)===0?2:1);
    let a=Tri[row][0]*k, b=Tri[row][1]*k, c=Tri[row][2]*k;
    if (a>b) [a,b]=[b,a];
    return {a,b,c};
  };
  const area = (t) => ({ kind:'AreaHyp', ...t, targetSide:2, targetArea:t.c*t.c, dock:[t.a*t.a,t.b*t.b,t.a+t.b], reverseTrue:false, rightVertex:2 });
  const lenH = (t) => ({ kind:'LenHyp', ...t, targetSide:2, targetArea:t.c*t.c, dock:[t.c*t.c, t.a+t.b, t.a*t.a], reverseTrue:false, rightVertex:2 });
  const lenL = (t) => {
    const g = rndInt(r,2);
    return g===0
      ? { kind:'LenLeg', ...t, targetSide:1, targetArea:t.b*t.b, dock:[t.b*t.b, t.c-t.a, t.c*t.c], reverseTrue:false, rightVertex:2 }
      : { kind:'LenLeg', ...t, targetSide:0, targetArea:t.a*t.a, dock:[t.a*t.a, t.c-t.b, t.c*t.c], reverseTrue:false, rightVertex:2 };
  };
  const rev = (ok, t) => ({ kind:'Reverse', ...t, targetSide:-1, targetArea:0, dock:[t.a*t.a,t.b*t.b,t.c*t.c], reverseTrue:ok, rightVertex:2 });
  const falseT = [[6,8,11],[5,12,14],[7,24,26],[8,15,16],[9,12,16],[3,4,6]][rndInt(r,6)];
  return [
    area(pick(true)), area(pick(true)), area(pick(true)),
    rndInt(r,2)?lenH(pick(false)):lenL(pick(false)),
    lenL(pick(false)), lenH(pick(false)), lenL(pick(false)),
    rev(true, pick(false)),
    rev(false, {a:falseT[0],b:falseT[1],c:falseT[2]}),
    rev(rndInt(r,2)===0, pick(false)),
  ];
}

function chance(s) {
  if (s.kind==='Reverse') return 0.25;
  if (s.kind==='AreaHyp') return 0;
  return 1/9;
}
function firstOk(s, bot, r, i) {
  if (s.kind==='Reverse') {
    const pick = bot==='mash'?0:bot==='cyc'?i%4:bot==='rnd'?rndInt(r,4):-1;
    if (pick<0) return false;
    const correct = s.reverseTrue ? s.rightVertex : 3;
    return pick===correct;
  }
  if (bot==='mash' || bot==='cyc') return false;
  if (bot!=='rnd') return false;
  const tile = rndInt(r,3), tray = rndInt(r,3);
  return completes(s, tray, s.dock[tile]);
}

const Games=200, Cuts=10;
let mash=0,cyc=0,rnd=0,ch=0,n=0;
const rng = mulberry(42);
for (let g=0; g<Games; g++) {
  const deck = makeDeck(1000+g);
  for (let i=0;i<Cuts;i++) {
    const s = deck[i]; n++; ch += chance(s);
    if (firstOk(s,'mash',rng,i)) mash++;
    if (firstOk(s,'cyc',rng,i)) cyc++;
    if (firstOk(s,'rnd',rng,i)) rnd++;
  }
}
const pct = x => (x*100).toFixed(1)+'%';
const line = `mash=${pct(mash/n)} cyc=${pct(cyc/n)} rnd=${pct(rnd/n)} none=0% chance≈${pct(ch/n)}`;
console.log(line);
console.log(JSON.stringify({mash:mash/n,cyc:cyc/n,rnd:rnd/n,none:0,chance:ch/n,n},null,2));
