import * as THREE from 'three';
import { World, Particles, SITES, loadKit, part, groundTexture } from './world.js';
import { Field, TAB, SNAP_AT, READ_AT, PROP_KINDS } from './dig.js';
import { Player } from './player.js';
import { Sfx } from './audio.js';
import { mathHTML } from './mathtext.js';
import * as Packs from './packs.js';

const $ = (id) => document.getElementById(id);
const ARTS = ['도자기 꽃병', '황금 가면', '암모나이트 화석', '청동 거울', '옛 금화 더미', '보석 반지', '풍뎅이 부적', '뼈 피리', '청동 단검',
  '작은 왕관', '토기 인형', '구슬 목걸이', '기름 등잔', '공룡 이빨 화석', '옥 도장'];
const UP = {
  brush: { name: '넓은 붓', desc: '한 번에 더 넓게 쓸어요', icon: 'ic-brush', costs: [40, 90, 160], vals: [0.42, 0.5, 0.58, 0.66] },
  chisel: { name: '단단한 끌', desc: '흙덩이가 더 빨리 깨져요', icon: 'ic-chisel', costs: [40, 90, 160], vals: [900, 1250, 1650, 2100] },
  speed: { name: '가벼운 장화', desc: '더 빨리 걸어요', icon: 'ic-fire', costs: [30, 70, 130], vals: [2.7, 3.0, 3.3, 3.6] },
  glass: { name: '큰 모래시계', desc: '구획마다 시간 +5초', icon: 'ic-glass', costs: [80, 180], vals: [0, 5, 10] },
};
const PLOTS = 10;
const REACH = 1.15;
const SAVE_KEY = 'yujeok-balgul-v1';
const TIME_BY_DIFF = [45, 45, 40, 35, 30];
const LESSONS = [
  { title: '걷고 쓸기', items: ['왼쪽 스틱(또는 WASD)으로 걷기', '붓으로 모래를 쓸어 내기', '석판 두 개의 글자 드러내기'] },
  { title: '끌과 소품', items: ['오른쪽 아래에서 끌로 바꾸기', '굳은 흙덩이 두 개 깨기', '항아리·뼈는 끌로 건드리지 않기'] },
  { title: '완전 발굴로 답하기', items: ['보기 석판 네 개를 모두 읽기', '정답 석판 하나만 끝까지 털어 내기'] },
];

function loadSave() {
  const def = { coins: 0, up: { brush: 0, chisel: 0, speed: 0, glass: 0 }, arts: {}, stars: [0, 0, 0], streak: { last: '', n: 0 }, tut: false, muted: false, pack: null, site: 0, recent: [] };
  try {
    const s = JSON.parse(localStorage.getItem(SAVE_KEY) || 'null');
    if (s && typeof s === 'object') return { ...def, ...s, up: { ...def.up, ...(s.up || {}) }, streak: { ...def.streak, ...(s.streak || {}) } };
  } catch {}
  return def;
}
const josa = (w, a, b) => {
  const c = w.charCodeAt(w.length - 1) - 0xac00;
  return w + (c >= 0 && c < 11172 && c % 28 ? a : b);
};
const dayStr = (d = new Date()) => `${d.getFullYear()}-${d.getMonth() + 1}-${d.getDate()}`;

export class Game {
  constructor() {
    this.save = loadSave();
    this.sfx = new Sfx();
    this.sfx.muted = !!this.save.muted;
    this.mode = 'title'; // title | tutorial | expedition
    this.phase = 'title'; // playing | resolve | results | failed | title
    this.lesson = 0;
    this.lessonDone = [];
    this.keys = new Set();
    this.joy = { id: null, x: 0, z: 0, cx: 0, cy: 0 };
    this.aim = { id: null, held: false, sx: 0, sy: 0, hover: false, world: new THREE.Vector3() };
    this.holdUse = false;
    this.tool = 'brush';
    this.timers = [];
    this.score = 0;
    this.strikes = 0;
    this.solved = 0;
    this.plotIdx = 0;
    this.items = [];
    this.item = null;
    this.correctIdx = 0;
    this.timeLeft = 45;
    this.timeLimit = 45;
    this.timerDelay = 0;
    this.siteIdx = 0;
    this.icons = new Map();
    this.hintCD = 0;
    this.chiselCD = 0;
    this.t = 0;
    this.labelsCache = [];
    this.toolPoint = new THREE.Vector3();
    this.ray = new THREE.Raycaster();
    this.plane = new THREE.Plane(new THREE.Vector3(0, 1, 0), -0.08);
    this.v = new THREE.Vector3();
    this.v2 = new THREE.Vector2();
    this.titleT = 0;
    this.demoPath = 0;
    this.demoReset = 0;
    this.artObj = null;
    this.shards = [];
  }

  persist() {
    try {
      localStorage.setItem(SAVE_KEY, JSON.stringify(this.save));
    } catch {}
  }

  lv() {
    const u = this.save.up;
    return {
      brushR: UP.brush.vals[u.brush],
      brushRate: 950,
      chiselR: 0.17 + u.chisel * 0.015,
      chiselPow: UP.chisel.vals[u.chisel],
      speed: UP.speed.vals[u.speed],
      bonus: UP.glass.vals[u.glass] || 0,
    };
  }

  // ───────────────────────── 초기화 ─────────────────────────
  async init() {
    this.layoutClasses();
    const canvas = $('gl');
    this.world = new World(canvas);
    const kitP = loadKit();
    const packP = this.loadInitialPack();
    await kitP;
    this.groundMat = new THREE.MeshLambertMaterial({ color: SITES[0].ground, map: groundTexture() });
    this.field = new Field(this.world.scene);
    this.player = new Player(this.world.scene);
    this.particles = new Particles(this.world.scene);
    this.makeShards();
    this.setSite(this.save.site || 0, true);
    this.bindUI();
    this.bindInput();
    this.onResize();
    window.addEventListener('resize', () => this.onResize());
    await packP;
    this.enterTitle();
    this.installHook();
    this.last = performance.now();
    const loop = (now) => {
      const dt = Math.max(0, Math.min(0.05, (now - this.last) / 1000));
      this.last = now;
      this.frame(dt);
      requestAnimationFrame(loop);
    };
    requestAnimationFrame(loop);
    window.__GAME_TEST__.ready = true;
  }

  async loadInitialPack() {
    const idx = await Packs.loadIndex();
    const q = new URLSearchParams(location.search).get('pack');
    const id = [q, this.save.pack, idx.default_pack].find((x) => x && Packs.packEntry(x)) || idx.packs[0].pack_id;
    this.pack = await Packs.loadPack(id);
    if (this.pack.items.length < 4) this.pack = await Packs.loadPack(idx.default_pack);
    this.updatePackChip();
  }

  updatePackChip() {
    const e = this.pack.entry;
    const g = e.school === 'middle' ? `중${e.grade}` : `초${e.grade}`;
    $('packName').textContent = `${g}-${e.semester} · ${e.title}`;
  }

  makeShards() {
    let geo = null;
    const sh = part('Shard');
    sh.traverse((o) => {
      if (!geo && o.isMesh) geo = o.geometry;
    });
    geo = geo || new THREE.TetrahedronGeometry(0.06);
    const m = new THREE.MeshLambertMaterial({ color: '#ffffff' });
    this.shardMesh = new THREE.InstancedMesh(geo, m, 30);
    this.shardMesh.instanceMatrix.setUsage(THREE.DynamicDrawUsage);
    this.shardMesh.count = 0;
    this.shardMesh.castShadow = true;
    this.shardMesh.frustumCulled = false;
    this.world.scene.add(this.shardMesh);
    this.shardData = [];
    this._m4 = new THREE.Matrix4();
    this._q = new THREE.Quaternion();
    this._e = new THREE.Euler();
    this._s = new THREE.Vector3();
  }

  setSite(i, force = false) {
    if (!force && i === this.siteIdx) return;
    this.siteIdx = i;
    const s = SITES[i];
    this.groundMat.color.set(s.ground);
    this.world.buildEnvironment(s, this.groundMat);
    this.field.setPalette(s);
    this.site = s;
  }

  layoutClasses() {
    const W = window.innerWidth, H = window.innerHeight;
    const h = document.documentElement;
    const land = W > H && W >= 900;
    h.classList.toggle('land', land);
    h.classList.toggle('port', !land);
    h.classList.toggle('mid', !land && W >= 700);
    const fine = window.matchMedia && matchMedia('(pointer: fine)').matches && !('ontouchstart' in window);
    h.classList.toggle('fine', !!fine);
    this.isLand = land;
  }

  onResize() {
    this.layoutClasses();
    const W = window.innerWidth, H = window.innerHeight;
    this.W = W;
    this.H = H;
    this.world.resize(W, H);
    const pr = this.world.renderer.getPixelRatio();
    this.particles.mat.uniforms.uScale.value = (H * pr) / (2 * Math.tan(THREE.MathUtils.degToRad(this.world.camera.fov / 2)));
    this.fitPlayCamera();
  }

  /** 문제판 아래 남은 영역에 구획을 맞춘다 */
  fitPlayCamera() {
    if (!this.world || this.mode === 'title') return;
    const W = this.W, H = this.H;
    const bb = $('board').getBoundingClientRect();
    let top = bb.bottom + 8;
    const ls = $('lesson');
    let x0 = 8, x1 = W - 8;
    if (!ls.classList.contains('hidden')) {
      const want = `${Math.round(bb.bottom + (this.isLand ? 14 : 8))}px`;
      if (ls.style.top !== want) ls.style.top = want;
      const lr = ls.getBoundingClientRect();
      if (this.isLand) x0 = lr.right + 8;
      else top = lr.bottom + 6;
    }
    const bottom = H - (this.isLand ? 104 : 118);
    const rect = { x: x0, y: top, w: Math.max(100, x1 - x0), h: Math.max(120, bottom - top) };
    this.world.camMode = 'play';
    this.world.fitCamera(rect, { x0: -3.25, x1: 3.25, z0: -2.45, z1: 3.0, y: 0.6 }, this.isLand ? 54 : 60);
  }

  fitTitleCamera(t) {
    const W = this.W, H = this.H;
    if (!this.titleRect || this.titleRectW !== W * 10000 + H) {
      if (this.isLand) this.titleRect = { x: Math.min(470, W * 0.38), y: 30, w: W - Math.min(470, W * 0.38) - 10, h: H - 90 };
      else {
        const top = document.querySelector('.t-top').getBoundingClientRect().bottom - 10;
        const bot = document.querySelector('.t-menu').getBoundingClientRect().top + 30;
        this.titleRect = { x: 0, y: top, w: W, h: Math.max(160, bot - top) };
      }
      this.titleRectW = W * 10000 + H;
    }
    const rect = this.titleRect;
    // 대원을 따라가는 느린 궤도 카메라
    const P = this.player.pos;
    const fc = (this.titleFocus ||= new THREE.Vector3(P.x, 0, P.z));
    fc.lerp(P, 1 - Math.exp(-0.016 * 2.2));
    const fx = THREE.MathUtils.clamp(fc.x, -1.4, 1.4), fz = THREE.MathUtils.clamp(fc.z, -1.0, 1.0);
    this.world.camMode = 'title';
    this.world.fitCamera(rect, { x0: fx - 1.9, x1: fx + 1.9, z0: fz - 1.5, z1: fz + 1.3, y: 1.1 }, 30 + Math.sin(t * 0.21) * 5, Math.sin(t * 0.13) * 32);
    this.world.camBase = null;
  }

  // ───────────────────────── 화면 전환 ─────────────────────────
  clearTimers() {
    for (const id of this.timers) clearTimeout(id);
    this.timers.length = 0;
  }
  later(ms, fn) {
    const id = setTimeout(() => {
      this.timers = this.timers.filter((x) => x !== id);
      fn();
    }, ms);
    this.timers.push(id);
  }

  showScreen(name) {
    $('title').classList.toggle('hidden', name !== 'title');
    $('title').classList.toggle('show', name === 'title');
    $('hud').classList.toggle('hidden', name !== 'hud');
    document.documentElement.classList.toggle('playing', name === 'hud');
  }

  enterTitle() {
    this.clearTimers();
    this.closeModal();
    this.mode = 'title';
    this.phase = 'title';
    this.sfx.setBgm(true);
    this.showScreen('title');
    this.refreshTitle();
    this.titleRect = null;
    this.field.reset({ labels: ['유', '적', '발', '굴'], crust: 'none', props: ['pot', 'lizard'], numbered: false, armed: false, demo: true, seed: 7 });
    this.player.pos.set(-1.3, 0, 0.1);
    this.player.setTool('brush');
    this.demoPath = 0;
    this.demoReset = 0;
    this.hideLabels();
  }

  refreshTitle() {
    const s = this.save;
    $('chCoins').lastChild.textContent = s.coins;
    $('chArts').lastChild.textContent = `${Object.keys(s.arts).length}/15`;
    const today = dayStr();
    const y = new Date();
    y.setDate(y.getDate() - 1);
    const nextN = s.streak.last === dayStr(y) ? s.streak.n + 1 : s.streak.last === today ? s.streak.n : 1;
    $('chStreak').lastChild.textContent = `연속 ${s.streak.last === today ? s.streak.n : Math.max(0, nextN - 1)}일`;
    if (s.streak.last !== today) {
      const reward = 10 + 5 * Math.min(nextN, 7);
      $('dailyTitle').textContent = `출석 ${nextN}일째!`;
      $('dailyText').textContent = `오늘의 발굴 보수 +${reward}코인`;
      $('daily').classList.remove('hidden');
      this.pendingDaily = { n: nextN, reward };
    } else $('daily').classList.add('hidden');
  }

  claimDaily() {
    if (!this.pendingDaily) return;
    this.save.streak = { last: dayStr(), n: this.pendingDaily.n };
    this.save.coins += this.pendingDaily.reward;
    this.pendingDaily = null;
    this.persist();
    this.sfx.coin();
    this.refreshTitle();
  }

  beginPlayScreen() {
    this.clearTimers();
    this.closeModal();
    this.showScreen('hud');
    this.sfx.setBgm(true);
    if (this.artObj) {
      this.world.scene.remove(this.artObj);
      this.artObj = null;
    }
  }

  startExpedition(site = this.save.site || 0) {
    this.beginPlayScreen();
    this.mode = 'expedition';
    this.setSite(site);
    this.save.site = site;
    this.persist();
    const recent = new Set(this.save.recent || []);
    this.items = Packs.pickExpedition(this.pack, PLOTS, recent);
    this.save.recent = [...this.items.map((i) => i.id), ...(this.save.recent || [])].slice(0, 60);
    this.plotIdx = 0;
    this.score = 0;
    this.strikes = 0;
    this.solved = 0;
    this.earned = 0;
    this.newArts = [];
    $('lesson').classList.add('hidden');
    $('rule').classList.remove('hidden');
    this.startPlot();
  }

  startPlot() {
    this.clearTimers();
    const it = (this.item = this.items[this.plotIdx]);
    this.correctIdx = it.choices.map(String).indexOf(String(it.answer));
    this.field.reset({ labels: it.choices.map(String) });
    this.resetPlayer();
    const L = this.lv();
    this.timeLimit = TIME_BY_DIFF[it.difficulty || 1] + L.bonus;
    this.timeLeft = this.timeLimit;
    this.timerDelay = 0.6;
    this.lastTick = 99;
    $('plotChip').textContent = `구획 ${this.plotIdx + 1}/${PLOTS}`;
    $('timer').classList.remove('hidden');
    $('prompt').innerHTML = mathHTML(it.prompt);
    $('feedback').classList.add('hidden');
    $('rule').classList.toggle('hidden', this.plotIdx > 0);
    this.updatePermits();
    this.updateCoins();
    this.phase = 'playing';
    this.sfx.whoosh();
    this.fitPlayCamera();
  }

  resetPlayer() {
    this.player.pos.set(0, 0, 2.6);
    this.player.vel.set(0, 0, 0);
    this.player.yaw = Math.PI;
    this.aim.held = false;
    this.aim.id = null;
    this.joy.id = null;
    this.joy.x = this.joy.z = 0;
    this.setKnob(0, 0);
  }

  // ── 튜토리얼
  startTutorial() {
    this.beginPlayScreen();
    this.mode = 'tutorial';
    this.setSite(0);
    this.strikes = 0;
    this.score = 0;
    this.solved = 0;
    this.plotIdx = 0;
    $('rule').classList.add('hidden');
    this.startLesson(0);
  }

  startLesson(n) {
    this.clearTimers();
    this.lesson = n;
    this.lessonDone = LESSONS[n].items.map(() => false);
    this.lessonFlags = { moved: 0, brushed: 0 };
    const ls = $('lesson');
    ls.classList.remove('hidden');
    $('lsTag').textContent = `레슨 ${n + 1}/3`;
    $('lsTitle').textContent = LESSONS[n].title;
    this.renderLesson();
    $('timer').classList.add('hidden');
    $('feedback').classList.add('hidden');
    $('plotChip').textContent = '연습';
    if (n === 0) {
      this.field.reset({ labels: ['모래', '속에', '석판이', '있어요'], crust: 'none', props: 'none', numbered: false, armed: false });
      $('prompt').innerHTML = '걸어가서 <b>붓</b>으로 모래를 쓸어 보세요. 석판 <b>두 개</b>의 글자가 보이면 통과!';
    } else if (n === 1) {
      this.field.reset({ labels: ['끌로', '흙덩이를', '깨요', '조심!'], crust: 'all', props: ['pot', 'bones'], numbered: false, armed: false });
      $('prompt').innerHTML = '회색 <b>흙덩이</b>는 붓으로 안 지워져요. <b>끌</b>로 깨세요. 끌이 항아리·뼈에 닿으면 깨져요!';
    } else {
      const it = Packs.easiest(this.pack);
      this.item = it;
      this.correctIdx = it.choices.map(String).indexOf(String(it.answer));
      this.field.reset({ labels: it.choices.map(String), crust: 'random', props: ['pot'] });
      $('prompt').innerHTML = mathHTML(it.prompt);
      $('rule').classList.remove('hidden');
    }
    this.setTool(n === 1 ? 'brush' : 'brush');
    this.resetPlayer();
    this.phase = 'playing';
    this.updatePermits();
    this.updateCoins();
    this.sfx.whoosh();
    requestAnimationFrame(() => this.fitPlayCamera());
    this.fitPlayCamera();
  }

  renderLesson() {
    const ul = $('lsList');
    const cur = this.lessonDone.indexOf(false);
    ul.innerHTML = LESSONS[this.lesson].items
      .map((s, i) => `<li class="${this.lessonDone[i] ? 'done' : ''} ${i === cur ? 'cur' : ''}"><i></i><span>${s}</span></li>`)
      .join('');
  }

  tickLesson(i) {
    if (this.mode !== 'tutorial' || this.lessonDone[i]) return;
    this.lessonDone[i] = true;
    this.sfx.coin();
    this.renderLesson();
    this.fitPlayCamera();
    if (this.lesson < 2 && this.lessonDone.every(Boolean)) {
      this.toast('잘했어요! 다음 레슨으로 가요', 1500);
      this.player.cheer = 1;
      this.phase = 'resolve';
      this.later(1500, () => this.startLesson(this.lesson + 1));
    }
  }

  finishTutorial() {
    this.save.tut = true;
    this.persist();
    this.openModal(`<div class="card lessoncard"><span class="tag green">연습 끝!</span>
      <h2>발굴 허가증 발급</h2>
      <p>이제 진짜 원정이에요. 구획 10개를 발굴하고 별을 모아요.</p>
      <ul><li><i style="background:var(--red)"></i>오답·시간 초과·소품 파손 = 허가 도장 −1</li><li><i style="background:var(--red)"></i>도장 3개를 잃으면 허가 취소</li></ul>
      <div class="acts"><button class="btn big yellow" data-act="go">원정 출발</button></div></div>`);
  }

  // ───────────────────────── 도구 사용(입력·자동 공통 경로) ─────────────────────────
  setTool(t) {
    this.tool = t;
    this.player.setTool(t);
    $('tBrush').classList.toggle('on', t === 'brush');
    $('tChisel').classList.toggle('on', t === 'chisel');
    if (this.mode === 'tutorial' && this.lesson === 1 && t === 'chisel') this.tickLesson(0);
  }

  /** 도구 한 번 적용: 모래/흙덩이 깎기 + 소품 판정 + 효과 */
  useTool(tool, x, z, dt, fx = true) {
    const L = this.lv();
    const res = this.field.applyTool(tool, x, z, dt, L);
    if (!res) return null;
    const y = this.field.heightAt(x, z) + 0.05;
    if (fx) {
      if (res.sand > 0) {
        const n = Math.min(4, 1 + ((res.sand / 1500) | 0));
        for (let k = 0; k < n; k++) {
          const a = Math.random() * Math.PI * 2;
          const r = Math.random() * (tool === 'brush' ? L.brushR : L.chiselR);
          const dx = Math.cos(a), dz = Math.sin(a);
          this.particles.emit(x + dx * r, y, z + dz * r, dx * 1.6 + (Math.random() - 0.5), 1.2 + Math.random() * 1.6, dz * 1.6 + (Math.random() - 0.5), 0.5 + Math.random() * 0.4, 0.07 + Math.random() * 0.07, this.site.sand, 0.6);
        }
      }
      if (res.crust > 0 && this.chiselCD <= 0) {
        this.chiselCD = 0.12;
        this.sfx.chisel();
        for (let k = 0; k < 5; k++) {
          const a = Math.random() * Math.PI * 2;
          this.particles.emit(x, y + 0.05, z, Math.cos(a) * 2.2, 2 + Math.random() * 2, Math.sin(a) * 2.2, 0.55, 0.06 + Math.random() * 0.05, this.site.crustDark, 1.3);
        }
        this.particles.emit(x, y + 0.1, z, 0, 1.5, 0, 0.18, 0.12, '#fff6c8', 0);
      }
    }
    if (tool === 'brush' && res.sand < 1 && res.crustUnderBrush > 12 && this.hintCD <= 0) {
      this.hintCD = 3.5;
      this.toast('굳은 흙덩이는 붓으로 안 지워져요 — <b>끌</b>로 바꾸세요!', 1800);
      this.sfx.thud();
      $('tChisel').animate([{ transform: 'scale(1)' }, { transform: 'scale(1.18)' }, { transform: 'scale(1)' }], { duration: 420, iterations: 2 });
    }
    // 끌이 소품에 닿았나
    if (tool === 'chisel') {
      for (const p of this.field.props) {
        if (p.broken) continue;
        const d = Math.hypot(p.x - x, p.z - z);
        if (d < p.r + L.chiselR) this.breakProp(p);
        else if (d < p.r + L.chiselR + 0.5) p.warn = 1;
      }
    }
    if (this.mode === 'tutorial') {
      if (res.sand > 0) this.lessonFlags.brushed += res.sand;
      if (this.lesson === 0 && this.lessonFlags.brushed > 4000) this.tickLesson(1);
    }
    return res;
  }

  breakProp(p) {
    p.broken = true;
    const K = p.K;
    const say = K.say[(Math.random() * K.say.length) | 0];
    this.bubble(p.x, 0.7, p.z, say);
    if (p.kind === 'lizard') {
      this.sfx.squeak();
      for (let k = 0; k < 10; k++) this.particles.emit(p.x, 0.15, p.z, (Math.random() - 0.5) * 3, 2 + Math.random() * 2, (Math.random() - 0.5) * 3, 0.6, 0.07, '#9be27a', 1);
    } else {
      this.sfx.shatter();
      this.spawnShards(p.x, 0.25, p.z, K.color, 12);
    }
    p.obj.visible = false;
    this.world.shake = 0.7;
    if (this.mode === 'tutorial') {
      this.toast('연습이라 괜찮아요! 실전에서는 허가 도장 하나와 10코인이 사라져요.', 2600);
      return;
    }
    const lost = Math.min(10, this.save.coins);
    this.save.coins -= lost;
    this.persist();
    this.popNum(p.x, 0.5, p.z, `−10`, 'bad');
    this.updateCoins();
    this.addStrike('소품을 깨뜨렸어요');
  }

  addStrike(reason) {
    this.strikes++;
    this.updatePermits(true);
    this.sfx.stamp();
    if (this.strikes >= 3 && this.mode === 'expedition') {
      this.phase = 'failing';
      this.field.locked = true;
      this.clearTimers();
      this.later(1300, () => this.failed(reason));
    }
  }

  spawnShards(x, y, z, color, n) {
    const c = new THREE.Color(color);
    for (let k = 0; k < n; k++) {
      let i = this.shardData.findIndex((s) => s.life <= 0);
      if (i < 0) {
        if (this.shardData.length >= 30) i = (Math.random() * 30) | 0;
        else i = this.shardData.push({}) - 1;
      }
      const a = Math.random() * Math.PI * 2;
      this.shardData[i] = {
        p: new THREE.Vector3(x, y, z),
        v: new THREE.Vector3(Math.cos(a) * (1 + Math.random() * 2), 2 + Math.random() * 2.5, Math.sin(a) * (1 + Math.random() * 2)),
        r: new THREE.Vector3(Math.random() * 6, Math.random() * 6, 0),
        w: new THREE.Vector3((Math.random() - 0.5) * 20, (Math.random() - 0.5) * 20, 0),
        life: 2.2,
        s: 0.8 + Math.random() * 0.8,
      };
      this.shardMesh.setColorAt(i, c);
    }
    if (this.shardMesh.instanceColor) this.shardMesh.instanceColor.needsUpdate = true;
  }

  updateShards(dt) {
    const m = this.shardMesh;
    let n = 0;
    for (let i = 0; i < this.shardData.length; i++) {
      const s = this.shardData[i];
      if (!s.p) continue;
      if (s.life > 0) {
        s.life -= dt;
        s.v.y -= 9.8 * dt;
        s.p.addScaledVector(s.v, dt);
        if (s.p.y < 0.03) {
          s.p.y = 0.03;
          s.v.multiplyScalar(0.4);
          s.v.y = Math.abs(s.v.y) * 0.3;
          s.w.multiplyScalar(0.5);
        }
        s.r.addScaledVector(s.w, dt);
      }
      const sc = s.life > 0 ? s.s * Math.min(1, s.life / 0.4) : 0;
      this._e.set(s.r.x, s.r.y, s.r.z);
      this._q.setFromEuler(this._e);
      this._s.setScalar(sc);
      this._m4.compose(s.p, this._q, this._s);
      m.setMatrixAt(i, this._m4);
      n = i + 1;
    }
    m.count = n;
    m.instanceMatrix.needsUpdate = true;
  }

  // ───────────────────────── 판정 ─────────────────────────
  checkTablets() {
    const f = this.field;
    if (f.locked) return;
    for (const t of f.tablets) {
      if (t.state === 'buried' && t.clean >= SNAP_AT) {
        this.snap(t);
        return;
      }
    }
    if (this.mode === 'tutorial') {
      const read = f.tablets.filter((t) => t.clean >= READ_AT).length;
      if (this.lesson === 0 && read >= 2) this.tickLesson(2);
      if (this.lesson === 1) {
        const broken = f.tablets.filter((t) => t.crust0 > 0 && f.crustLeft(t) < 0.12).length;
        if (broken >= 2) {
          this.tickLesson(1);
          if (!f.props.some((p) => p.broken)) this.tickLesson(2);
          else if (this.lessonDone[1]) this.tickLesson(2);
        }
      }
      if (this.lesson === 2 && read >= 4) this.tickLesson(0);
    }
  }

  snap(t) {
    const f = this.field;
    f.locked = true;
    const pts = f.clearTablet(t);
    for (const [x, z] of pts) {
      const a = Math.atan2(z - t.z, x - t.x);
      this.particles.emit(x, 0.12, z, Math.cos(a) * 3.2, 1.6 + Math.random() * 1.4, Math.sin(a) * 3.2, 0.7, 0.11 + Math.random() * 0.06, this.site.sand, 0.5);
    }
    this.sfx.snap();
    this.world.shake = 0.35;
    if (this.mode === 'title') return;
    if (this.mode === 'tutorial' && this.lesson < 2) {
      t.state = 'lift';
      t.anim = 0;
      t.glowColor.set('#ffd23f');
      this.later(900, () => {
        t.state = 'shown';
        t.g.position.y = -0.118;
        t.g.rotation.x = 0;
        f.locked = false;
      });
      return;
    }
    this.judge(t.i);
  }

  judge(i) {
    if (this.phase !== 'playing') return;
    const f = this.field;
    const t = f.tablets[i];
    const ok = i === this.correctIdx;
    this.phase = 'resolve';
    const fb = $('feedback');
    fb.classList.remove('hidden');
    const it = this.item;
    if (ok) {
      this.solved++;
      const tl = Math.max(0, Math.ceil(this.timeLeft));
      this.score += 100 + tl * 5;
      const gain = 10 + Math.ceil(tl / 3);
      this.save.coins += gain;
      this.earned = (this.earned || 0) + gain;
      t.state = 'lift';
      t.anim = 0;
      t.glowColor.set('#ffd23f');
      const art = this.rollArtifact();
      this.spawnArtifact(art, t.x, t.z);
      this.sfx.correct();
      this.later(500, () => this.sfx.coin());
      this.player.cheer = 1;
      this.player.hop = 1;
      for (let k = 0; k < 40; k++) {
        const a = Math.random() * Math.PI * 2;
        this.particles.emit(t.x + Math.cos(a) * 0.6, 0.4, t.z + Math.sin(a) * 0.4, Math.cos(a) * 1.5, 2.5 + Math.random() * 3, Math.sin(a) * 1.5, 1.1, 0.08 + Math.random() * 0.06, Math.random() < 0.5 ? '#ffd23f' : '#fff4c0', 0.5);
      }
      this.popNum(t.x, 1.0, t.z, `+${gain}`, 'good');
      fb.className = 'feedback ok';
      fb.innerHTML = `정답! ${josa(`「${ARTS[art]}」`.slice(0, -1), '」을', '」를')} 발견했어요. <b>+${gain}코인</b>`;
      this.updateCoins();
      this.persist();
      if (this.mode === 'tutorial') {
        this.tickLesson(1);
        this.later(1800, () => this.finishTutorial());
      } else this.later(2300, () => this.nextPlot());
    } else {
      t.state = 'crack';
      t.anim = 0;
      f.crack(t);
      this.sfx.wrong();
      this.world.shake = 0.9;
      this.player.sad = 1;
      for (let k = 0; k < 24; k++) {
        const a = Math.random() * Math.PI * 2;
        this.particles.emit(t.x + Math.cos(a) * 0.5, 0.2, t.z + Math.sin(a) * 0.3, Math.cos(a) * 1.4, 1 + Math.random() * 1.5, Math.sin(a) * 1.4, 0.9, 0.12, '#9a8466', 0.6);
      }
      this.revealCorrect();
      fb.className = 'feedback bad';
      fb.innerHTML = this.wrongText('이 석판이 아니에요!');
      if (this.mode === 'tutorial') {
        this.tickLesson(1);
        this.later(3200, () => this.finishTutorial());
      } else {
        this.popNum(t.x, 0.9, t.z, '도장 −1', 'bad');
        this.addStrike('틀린 석판을 발굴했어요');
        if (this.strikes < 3) this.later(3600, () => this.nextPlot());
      }
    }
  }

  wrongText(head) {
    const it = this.item;
    return `<button class="btn sm orange nx" data-act="next">다음 ▶</button>${head} 정답은 <b>${this.correctIdx + 1}번 ${mathHTML(String(it.answer))}</b>. ${it.explain ? mathHTML(it.explain) : ''}`;
  }

  revealCorrect() {
    const c = this.field.tablets[this.correctIdx];
    this.field.clearTablet(c);
    c.state = 'reveal';
    c.anim = 0;
    c.glowColor.set('#5fe07a');
  }

  timeout() {
    if (this.phase !== 'playing') return;
    this.phase = 'resolve';
    this.field.locked = true;
    this.revealCorrect();
    this.sfx.wrong();
    this.player.sad = 1;
    const fb = $('feedback');
    fb.classList.remove('hidden');
    fb.className = 'feedback bad';
    fb.innerHTML = this.wrongText('시간 초과!');
    this.addStrike('시간이 다 됐어요');
    if (this.strikes < 3) this.later(3600, () => this.nextPlot());
  }

  nextPlot() {
    if (this.mode !== 'expedition') return;
    if (this.phase === 'failing' || this.phase === 'failed') return;
    if (this.artObj) {
      this.world.scene.remove(this.artObj);
      this.artObj = null;
    }
    this.plotIdx++;
    if (this.plotIdx >= PLOTS) this.results();
    else this.startPlot();
  }

  rollArtifact() {
    const owned = this.save.arts;
    const missing = ARTS.map((_, i) => i).filter((i) => !owned[i]);
    const pool = missing.length && Math.random() < 0.75 ? missing : ARTS.map((_, i) => i);
    const a = pool[(Math.random() * pool.length) | 0];
    if (!owned[a]) this.newArts?.push(a);
    owned[a] = (owned[a] || 0) + 1;
    return a;
  }

  spawnArtifact(a, x, z) {
    if (this.artObj) this.world.scene.remove(this.artObj);
    const o = part('Art_' + String(a).padStart(2, '0'));
    o.traverse((m) => {
      if (m.isMesh) m.castShadow = true;
    });
    o.position.set(x + TAB.w * 0.36, 0.1, z - TAB.d * 0.62);
    o.scale.setScalar(0.01);
    o.userData.t = 0;
    this.world.scene.add(o);
    this.artObj = o;
  }

  updateArtifact(dt) {
    const o = this.artObj;
    if (!o) return;
    o.userData.t += dt;
    const t = o.userData.t;
    const k = Math.min(1, t / 0.7);
    const e = 1 - Math.pow(1 - k, 3);
    o.position.y = 0.3 + e * 1.15 + Math.sin(t * 3) * 0.05;
    o.rotation.y = t * 3;
    const s = t < 1.6 ? 2.6 * (0.2 + 0.8 * e) : Math.max(0.01, 2.6 * (1 - (t - 1.6) / 0.5));
    o.scale.setScalar(s);
    if (Math.random() < 0.5) this.particles.emit(o.position.x + (Math.random() - 0.5) * 0.5, o.position.y, o.position.z + (Math.random() - 0.5) * 0.5, 0, 0.6, 0, 0.6, 0.06, '#fff2a0', -0.1);
    if (t > 2.1) {
      this.world.scene.remove(o);
      this.artObj = null;
    }
  }

  results() {
    this.phase = 'results';
    this.field.locked = true;
    const stars = this.solved >= 9 ? 3 : this.solved >= 7 ? 2 : 1;
    const bonus = stars * 15;
    this.save.coins += bonus;
    this.save.stars[this.siteIdx] = Math.max(this.save.stars[this.siteIdx] || 0, stars);
    this.persist();
    this.sfx.correct();
    const starHtml = [0, 1, 2].map((i) => `<i class="ic ${i < stars ? 'ic-star' : 'ic-star0'}"></i>`).join('');
    const arts = (this.newArts || []).map((a) => `<img alt="${ARTS[a]}" src="${this.iconFor(a)}">`).join('');
    this.openModal(`<div class="card"><span class="tag green">원정 완료!</span>
      <h2>${this.site.name} 발굴 끝</h2>
      <div class="stars">${starHtml}</div>
      <div class="statrow"><span class="chip">정답 ${this.solved}/${PLOTS}</span><span class="chip"><i class="ic ic-coin"></i>+${(this.earned || 0) + bonus}</span><span class="chip">점수 ${this.score}</span></div>
      ${arts ? `<p>새로 찾은 유물</p><div class="newart">${arts}</div>` : ''}
      <p>${stars < 3 ? '정답 9개 이상이면 별 3개!' : '완벽한 발굴이에요!'}</p>
      <div class="acts"><button class="btn yellow" data-act="again">다시 원정</button><button class="btn green" data-act="board">게시판</button><button class="btn cream" data-act="title">처음으로</button></div></div>`);
  }

  failed(reason) {
    this.phase = 'failed';
    this.field.locked = true;
    this.sfx.stamp();
    this.openModal(`<div class="card"><span class="tag red">허가 도장 0개</span>
      <div class="stamp">발굴 허가 취소</div>
      <p>${reason}. 오답·시간 초과·소품 파손으로 도장 3개를 모두 잃었어요.</p>
      <div class="statrow"><span class="chip">정답 ${this.solved}/${this.plotIdx + 1}</span><span class="chip">점수 ${this.score}</span></div>
      <div class="acts"><button class="btn yellow" data-act="again">다시 도전</button><button class="btn cream" data-act="title">처음으로</button></div></div>`);
  }

  // ───────────────────────── HUD ─────────────────────────
  updatePermits(flash) {
    const is = $('permits').children;
    for (let i = 0; i < 3; i++) is[i].classList.toggle('off', i >= 3 - this.strikes);
    if (flash) $('permits').animate([{ transform: 'scale(1.4)' }, { transform: 'scale(1)' }], { duration: 350 });
  }
  updateCoins() {
    $('hudCoins').lastChild.textContent = this.save.coins;
  }

  toast(html, ms = 1600) {
    const el = $('toast');
    el.innerHTML = html;
    el.classList.remove('hidden');
    el.style.animation = 'none';
    void el.offsetWidth;
    el.style.animation = '';
    clearTimeout(this._toastT);
    this._toastT = setTimeout(() => el.classList.add('hidden'), ms);
  }

  // 월드 고정 라벨(풀링)
  ensureLabels() {
    if (this.lbl) return;
    const w = $('world');
    const mk = (cls) => {
      const e = document.createElement('div');
      e.className = 'wl ' + cls;
      e.style.display = 'none';
      w.appendChild(e);
      return e;
    };
    this.lbl = {
      pct: [0, 1, 2, 3].map(() => mk('pct')),
      warn: mk('bubble warn'),
      bub: [0, 1].map(() => ({ el: mk('bubble'), t: 0, p: new THREE.Vector3() })),
      pop: [0, 1, 2, 3].map(() => ({ el: mk('popnum'), t: 0, p: new THREE.Vector3() })),
    };
    this.lbl.warn.textContent = '끌 조심!';
    this.pctText = ['', '', '', ''];
    this.pctCls = ['', '', '', ''];
  }
  hideLabels() {
    this.ensureLabels();
    for (const e of this.lbl.pct) e.style.display = 'none';
    this.lbl.warn.style.display = 'none';
  }
  toScreen(x, y, z) {
    this.v.set(x, y, z).project(this.world.camera);
    return [(this.v.x * 0.5 + 0.5) * this.W, (-this.v.y * 0.5 + 0.5) * this.H, this.v.z < 1];
  }
  bubble(x, y, z, text) {
    this.ensureLabels();
    const b = this.lbl.bub.find((b) => b.t <= 0) || this.lbl.bub[0];
    b.el.textContent = text;
    b.t = 1.8;
    b.p.set(x, y, z);
  }
  popNum(x, y, z, text, cls) {
    this.ensureLabels();
    const b = this.lbl.pop.find((b) => b.t <= 0) || this.lbl.pop[0];
    b.el.textContent = text;
    b.el.className = 'wl popnum ' + cls;
    b.t = 1.2;
    b.p.set(x, y, z);
  }
  updateLabels(dt) {
    this.ensureLabels();
    const L = this.lbl;
    const showPct = this.mode !== 'title' && (this.phase === 'playing');
    this.field.tablets.forEach((t, i) => {
      const e = L.pct[i];
      if (!showPct || t.state !== 'buried' || t.clean < 0.04) {
        if (e.style.display !== 'none') e.style.display = 'none';
        return;
      }
      const [sx, sy] = this.toScreen(t.x, 0.2, t.z - TAB.d / 2 - 0.05);
      const pc = Math.min(99, Math.floor(t.clean * 100));
      const answering = this.mode === 'expedition' || this.lesson === 2;
      const hot = answering && t.clean >= 0.6;
      const txt = hot ? `${pc}% 곧 확정!` : t.clean >= READ_AT ? `${pc}% 읽힘` : `${pc}%`;
      const cls = hot ? 'wl pct hot' : t.clean >= READ_AT ? 'wl pct read' : 'wl pct';
      if (this.pctText[i] !== txt) {
        e.textContent = txt;
        this.pctText[i] = txt;
      }
      if (this.pctCls[i] !== cls) {
        e.className = cls;
        this.pctCls[i] = cls;
      }
      e.style.display = '';
      e.style.transform = `translate(${sx | 0}px,${sy | 0}px) translate(-50%,-50%)`;
    });
    // 끌 경고
    let warnP = null;
    if (this.mode !== 'title' && this.tool === 'chisel') for (const p of this.field.props) if (!p.broken && p.warn > 0.5) warnP = p;
    if (warnP) {
      const [sx, sy] = this.toScreen(warnP.x, 0.75, warnP.z);
      L.warn.style.display = '';
      L.warn.style.transform = `translate(${sx | 0}px,${sy | 0}px) translate(-50%,-100%)`;
    } else if (L.warn.style.display !== 'none') L.warn.style.display = 'none';
    for (const b of L.bub) {
      if (b.t > 0) {
        b.t -= dt;
        const [sx, sy] = this.toScreen(b.p.x, b.p.y, b.p.z);
        b.el.style.display = b.t > 0 ? '' : 'none';
        const s = Math.min(1, (1.8 - b.t) * 8);
        b.el.style.transform = `translate(${sx | 0}px,${sy | 0}px) translate(-50%,-100%) scale(${s.toFixed(2)})`;
      }
    }
    for (const b of L.pop) {
      if (b.t > 0) {
        b.t -= dt;
        const [sx, sy] = this.toScreen(b.p.x, b.p.y, b.p.z);
        b.el.style.display = b.t > 0 ? '' : 'none';
        b.el.style.opacity = Math.min(1, b.t * 3).toFixed(2);
        b.el.style.transform = `translate(${sx | 0}px,${(sy - (1.2 - b.t) * 60) | 0}px) translate(-50%,-50%)`;
      }
    }
  }

  // ───────────────────────── 입력 ─────────────────────────
  bindInput() {
    const cv = $('gl');
    const unlock = () => this.sfx.unlock();
    window.addEventListener('pointerdown', unlock, { passive: true });
    window.addEventListener('keydown', unlock);
    const inJoy = (e) => {
      if (e.pointerType === 'mouse' || document.documentElement.classList.contains('fine')) return false;
      const r = $('joy').getBoundingClientRect();
      return e.clientX > r.left - 30 && e.clientX < r.right + 30 && e.clientY > r.top - 30 && e.clientY < r.bottom + 30;
    };
    cv.addEventListener('pointerdown', (e) => {
      if (this.mode === 'title' || !this.playable()) return;
      e.preventDefault();
      if (inJoy(e) && this.joy.id === null) {
        this.joy.id = e.pointerId;
        const r = $('joy').getBoundingClientRect();
        this.joy.cx = r.left + r.width / 2;
        this.joy.cy = r.top + r.height / 2;
        this.moveJoy(e);
      } else if (this.aim.id === null) {
        this.aim.id = e.pointerId;
        this.aim.held = true;
        this.aim.sx = e.clientX;
        this.aim.sy = e.clientY;
      }
      try {
        cv.setPointerCapture(e.pointerId);
      } catch {}
    });
    cv.addEventListener('pointermove', (e) => {
      if (e.pointerId === this.joy.id) this.moveJoy(e);
      else if (e.pointerId === this.aim.id || (e.pointerType === 'mouse' && this.aim.id === null)) {
        this.aim.sx = e.clientX;
        this.aim.sy = e.clientY;
        this.aim.hover = e.pointerType === 'mouse';
      }
    });
    const up = (e) => {
      if (e.pointerId === this.joy.id) {
        this.joy.id = null;
        this.joy.x = this.joy.z = 0;
        this.setKnob(0, 0);
      }
      if (e.pointerId === this.aim.id) {
        this.aim.id = null;
        this.aim.held = false;
      }
    };
    cv.addEventListener('pointerup', up);
    cv.addEventListener('pointercancel', up);
    cv.addEventListener('pointerleave', (e) => {
      if (e.pointerType === 'mouse') this.aim.hover = false;
    });
    cv.addEventListener('contextmenu', (e) => e.preventDefault());
    window.addEventListener('keydown', (e) => {
      const k = e.key.toLowerCase();
      if (k === 'escape') {
        if (this.mode !== 'title' && (this.phase === 'playing' || this.phase === 'paused')) this.togglePause();
        return;
      }
      if (['q', 'e', 'tab', '1', '2'].includes(k) && this.mode !== 'title') {
        e.preventDefault();
        this.setTool(k === '1' ? 'brush' : k === '2' ? 'chisel' : this.tool === 'brush' ? 'chisel' : 'brush');
        this.sfx.click();
        return;
      }
      if (k === 'enter' && this.phase === 'resolve' && !$('feedback').classList.contains('hidden')) this.skipResolve();
      this.keys.add(k);
      if ([' ', 'arrowup', 'arrowdown', 'arrowleft', 'arrowright'].includes(k)) e.preventDefault();
    });
    window.addEventListener('keyup', (e) => this.keys.delete(e.key.toLowerCase()));
    window.addEventListener('blur', () => {
      this.keys.clear();
      this.aim.held = false;
      this.aim.id = null;
    });
    // 도구 버튼: 탭 = 고르기, 꾹 = 바라보는 쪽에 사용
    for (const id of ['tBrush', 'tChisel']) {
      const b = $(id);
      b.addEventListener('pointerdown', (e) => {
        e.preventDefault();
        const t = b.dataset.tool;
        if (this.tool !== t) this.sfx.click();
        this.setTool(t);
        this.holdUse = true;
        try {
          b.setPointerCapture(e.pointerId);
        } catch {}
      });
      const rel = () => (this.holdUse = false);
      b.addEventListener('pointerup', rel);
      b.addEventListener('pointercancel', rel);
    }
  }

  playable() {
    return this.phase === 'playing' || this.phase === 'resolve';
  }

  moveJoy(e) {
    let dx = e.clientX - this.joy.cx;
    let dy = e.clientY - this.joy.cy;
    const R = 50;
    const d = Math.hypot(dx, dy);
    if (d > R) {
      dx = (dx / d) * R;
      dy = (dy / d) * R;
    }
    this.joy.x = dx / R;
    this.joy.z = dy / R;
    this.setKnob(dx, dy);
  }
  setKnob(dx, dy) {
    const k = document.querySelector('.joy-knob');
    if (k) k.style.transform = `translate(${dx}px,${dy}px)`;
  }

  screenToGround(sx, sy, out) {
    this.v2.set((sx / this.W) * 2 - 1, -(sy / this.H) * 2 + 1);
    this.ray.setFromCamera(this.v2, this.world.camera);
    return this.ray.ray.intersectPlane(this.plane, out);
  }

  // ───────────────────────── 프레임 ─────────────────────────
  frame(dt) {
    this.t += dt;
    this.hintCD -= dt;
    this.chiselCD -= dt;
    const P = this.player;
    const L = this.lv();
    let using = false;
    let move = { x: 0, z: 0 };
    let faceAt = null;
    if (this.mode === 'title') {
      this.fitTitleCamera(this.t);
      this.demoStep(dt, L);
      return this.renderFrame(dt);
    }
    const canAct = this.phase === 'playing' && !this.modalOpen;
    if (canAct || this.phase === 'resolve') {
      const k = this.keys;
      move.x = (k.has('d') || k.has('arrowright') ? 1 : 0) - (k.has('a') || k.has('arrowleft') ? 1 : 0) + this.joy.x;
      move.z = (k.has('s') || k.has('arrowdown') ? 1 : 0) - (k.has('w') || k.has('arrowup') ? 1 : 0) + this.joy.z;
      const keyMove = Math.abs(move.x) + Math.abs(move.z) > 0.05;
      if (keyMove && this.mode === 'tutorial' && this.lesson === 0) {
        this.lessonFlags.moved += dt;
        if (this.lessonFlags.moved > 0.5) this.tickLesson(0);
      }
      const useKey = k.has(' ') || k.has('j');
      if (canAct && this.aim.held && this.screenToGround(this.aim.sx, this.aim.sy, this.aim.world)) {
        const dx = this.aim.world.x - P.pos.x, dz = this.aim.world.z - P.pos.z;
        const dist = Math.hypot(dx, dz);
        if (dist > REACH && !keyMove) {
          move.x = dx / dist;
          move.z = dz / dist;
          if (this.mode === 'tutorial' && this.lesson === 0) {
            this.lessonFlags.moved += dt;
            if (this.lessonFlags.moved > 0.5) this.tickLesson(0);
          }
        }
        const r = Math.min(dist, REACH);
        this.toolPoint.set(P.pos.x + (dx / (dist || 1)) * r, 0, P.pos.z + (dz / (dist || 1)) * r);
        using = true;
        faceAt = this.toolPoint;
      } else if (canAct && (this.holdUse || useKey)) {
        this.toolPoint.set(P.pos.x + Math.sin(P.yaw) * 0.75, 0, P.pos.z + Math.cos(P.yaw) * 0.75);
        using = true;
      } else if (canAct && this.aim.hover && this.screenToGround(this.aim.sx, this.aim.sy, this.aim.world)) {
        const dx = this.aim.world.x - P.pos.x, dz = this.aim.world.z - P.pos.z;
        const dist = Math.hypot(dx, dz);
        const r = Math.min(dist, REACH);
        this.toolPoint.set(P.pos.x + (dx / (dist || 1)) * r, 0, P.pos.z + (dz / (dist || 1)) * r);
        faceAt = dist > 0.3 ? this.aim.world : null;
      }
    }
    P.using = using;
    const colliders = this.field.props.filter((p) => !p.broken);
    P.step(dt, move, L.speed, faceAt, colliders);
    if (using) {
      const res = this.useTool(this.tool, this.toolPoint.x, this.toolPoint.z, dt);
      this.sfx.brush(this.tool === 'brush' && res ? Math.min(1, res.sand / 600) : 0, 1);
    } else this.sfx.brush(0);
    const showRing = this.mode !== 'title' && (using || this.aim.hover || this.holdUse) && this.phase === 'playing';
    if (showRing) {
      const danger = this.tool === 'chisel' && this.field.props.some((p) => !p.broken && Math.hypot(p.x - this.toolPoint.x, p.z - this.toolPoint.z) < p.r + L.chiselR + 0.25);
      P.showRing(this.toolPoint.x, this.toolPoint.z, this.tool === 'brush' ? L.brushR : L.chiselR, this.field.heightAt(this.toolPoint.x, this.toolPoint.z), danger);
    } else P.ring.visible = false;

    // 타이머
    if (this.phase === 'playing' && this.mode === 'expedition' && !this.modalOpen) {
      if (this.timerDelay > 0) this.timerDelay -= dt;
      else {
        this.timeLeft -= dt;
        const s = Math.ceil(this.timeLeft);
        if (s <= 5 && s !== this.lastTick && s > 0) {
          this.lastTick = s;
          this.sfx.tick();
        }
        if (this.timeLeft <= 0) {
          this.timeLeft = 0;
          this.timeout();
        }
      }
    }
    if (this.mode === 'expedition') {
      const k = Math.max(0, this.timeLeft / this.timeLimit);
      $('timerFill').style.transform = `scaleX(${k.toFixed(3)})`;
      const s = String(Math.ceil(this.timeLeft));
      if ($('timerTxt').textContent !== s) $('timerTxt').textContent = s;
      $('timer').classList.toggle('low', this.timeLeft <= 5 && this.phase === 'playing');
    }
    this.renderFrame(dt);
  }

  demoStep(dt, L) {
    // 타이틀: 대원이 「유·적·발·굴」 석판을 차례로 쓴다
    const f = this.field;
    const P = this.player;
    const order = [0, 1, 2, 3];
    const ti = order[Math.floor(this.demoPath) % 4];
    const t = f.tablets[ti];
    const ph = (this.demoPath % 1) * Math.PI * 2;
    const tx = t.x + Math.sin(ph * 2) * 0.7;
    const tz = t.z + Math.sin(ph) * 0.35;
    const dx = tx - P.pos.x, dz = tz - P.pos.z;
    const dist = Math.hypot(dx, dz);
    const move = dist > 1.0 ? { x: dx / dist, z: dz / dist } : { x: 0, z: 0 };
    P.using = dist < 1.4;
    const r = Math.min(dist, 0.9);
    this.toolPoint.set(P.pos.x + (dx / (dist || 1)) * r, 0, P.pos.z + (dz / (dist || 1)) * r);
    P.step(dt, move, 2.4, this.toolPoint, f.props);
    if (P.using) {
      f.locked = false;
      const res = f.applyTool('brush', this.toolPoint.x, this.toolPoint.z, dt * 0.7, L);
      if (res && res.sand > 0 && Math.random() < 0.7) {
        const a = Math.random() * 6.28;
        this.particles.emit(this.toolPoint.x, 0.15, this.toolPoint.z, Math.cos(a) * 1.5, 1.5 + Math.random(), Math.sin(a) * 1.5, 0.6, 0.09, this.site.sand, 0.6);
      }
      f.measure();
      if (t.clean > 0.82) this.demoPath = Math.floor(this.demoPath) + 1;
      else this.demoPath += dt * 0.22;
    }
    if (f.tablets.every((x) => x.clean > 0.8)) {
      this.demoReset += dt;
      if (this.demoReset > 2.5) {
        f.reset({ labels: ['유', '적', '발', '굴'], crust: 'none', props: ['pot', 'lizard'], numbered: false, armed: false, demo: true, seed: (Math.random() * 1e6) | 0 });
        this.demoReset = 0;
      }
    }
  }

  renderFrame(dt) {
    const f = this.field;
    if (f.anyDirty) {
      if (this.mode !== 'title') {
        f.measure();
        this.checkTablets();
      }
      f.flush();
    }
    f.update(dt, this.t);
    this.player.animate(f.heightAt(this.player.pos.x, this.player.pos.z) * 0.6);
    this.particles.update(dt);
    this.updateShards(dt);
    this.updateArtifact(dt);
    this.world.update(dt, this.t);
    if (this.mode !== 'title') this.updateLabels(dt);
    this.world.render();
  }

  skipResolve() {
    if (this.phase !== 'resolve' || this.mode !== 'expedition') return;
    this.clearTimers();
    this.nextPlot();
  }

  // ───────────────────────── 모달·메뉴 ─────────────────────────
  openModal(html) {
    const m = $('modal');
    m.innerHTML = html;
    m.classList.remove('hidden');
    this.modalOpen = true;
  }
  closeModal() {
    $('modal').classList.add('hidden');
    $('modal').innerHTML = '';
    this.modalOpen = false;
  }

  togglePause() {
    if (this.phase === 'paused') {
      this.phase = this.pausedFrom || 'playing';
      this.closeModal();
      return;
    }
    if (this.phase !== 'playing') return;
    this.pausedFrom = this.phase;
    this.phase = 'paused';
    this.openModal(`<div class="card"><span class="tag teal">잠깐 쉬기</span><h2>일시정지</h2>
      <ul class="help">
        <li><b>걷기</b>: 왼쪽 스틱 · WASD · 방향키</li>
        <li><b>도구 쓰기</b>: 땅을 꾹 누르고 문지르기(멀면 걸어가요) · 도구 버튼 꾹 · 스페이스</li>
        <li><b>붓</b>은 모래, <b>끌</b>은 굳은 흙덩이. 끌이 항아리·뼈·두루마리·도마뱀에 닿으면 도장 −1</li>
        <li>글자는 40%쯤 털면 읽혀요. <b>끝까지(90%) 털어 낸 첫 석판이 답</b>으로 확정돼요.</li>
      </ul>
      <div class="acts"><button class="btn yellow" data-act="resume">계속하기</button><button class="btn cream" data-act="title">원정 그만두기</button></div></div>`);
  }

  openBoard() {
    const svg = (i) => {
      const s = SITES[i];
      const shape = i === 0 ? '<path d="M14 58 L36 22 L58 58Z" fill="#e8b871" stroke="#2b1a10" stroke-width="3"/>' : i === 1 ? '<path d="M12 58h48v-8H52v-8H44v-8H28v8h-8v8h-8z" fill="#8c9b74" stroke="#2b1a10" stroke-width="3"/>' : '<path d="M10 58 Q36 6 62 58 H50 Q36 30 22 58Z" fill="#bfe6ff" stroke="#2b1a10" stroke-width="3"/>';
      return `<svg class="pic" viewBox="0 0 72 72" style="background:linear-gradient(${s.sky[0]},${s.sky[2]} 70%,${s.ground} 70%)">${shape}</svg>`;
    };
    const cards = SITES.map((s, i) => {
      const locked = i > 0 && !(this.save.stars[i - 1] > 0);
      const st = [0, 1, 2].map((k) => `<i class="ic ${k < (this.save.stars[i] || 0) ? 'ic-star' : 'ic-star0'}"></i>`).join('');
      return `<div class="site ${locked ? 'locked' : ''}">${svg(i)}<div><h3>${s.name}</h3><span class="who">${s.who}의 의뢰</span><q>${s.quote}</q><div class="st">${st}</div></div>
        ${locked ? `<button class="btn sm cream" disabled><i class="ic ic-lock"></i>잠김</button>` : `<button class="btn sm yellow" data-act="site" data-site="${i}">출발</button>`}</div>`;
    }).join('');
    this.openModal(`<div class="card wide"><span class="tag">원정 게시판</span><button class="xbtn" data-act="close" aria-label="닫기">✕</button>
      <p>앞 유적지에서 별을 1개 이상 받으면 다음 유적지가 열려요.</p>
      <div class="scroll"><div class="sites">${cards}</div></div></div>`);
  }

  openShop() {
    const rows = Object.entries(UP).map(([k, u]) => {
      const lv = this.save.up[k];
      const max = u.costs.length;
      const cost = u.costs[lv];
      const bars = Array.from({ length: max }, (_, i) => `<i class="${i < lv ? 'on' : ''}"></i>`).join('');
      const btn = lv >= max ? `<button class="btn sm cream" disabled>최고</button>` : `<button class="btn sm yellow" data-act="buy" data-k="${k}" ${this.save.coins < cost ? 'disabled' : ''}><i class="ic ic-coin"></i>${cost}</button>`;
      return `<div class="up"><div class="ico"><i class="ic ${u.icon}"></i></div><div><h4>${u.name}</h4><small>${u.desc}</small><div class="lv">${bars}</div></div>${btn}</div>`;
    }).join('');
    this.openModal(`<div class="card"><span class="tag">발굴 상점</span><button class="xbtn" data-act="close" aria-label="닫기">✕</button>
      <div class="statrow"><span class="chip"><i class="ic ic-coin"></i>${this.save.coins}코인</span></div>
      <div class="scroll"><div class="shop">${rows}</div></div></div>`);
  }

  openMuseum() {
    const cards = ARTS.map((n, i) => {
      const c = this.save.arts[i] || 0;
      return c
        ? `<div class="art"><img alt="${n}" src="${this.iconFor(i)}"><b>${n}</b><small>×${c}</small></div>`
        : `<div class="art locked"><div class="q">?</div><b>미발견</b><small>&nbsp;</small></div>`;
    }).join('');
    const n = Object.keys(this.save.arts).length;
    this.openModal(`<div class="card wide"><span class="tag teal">박물관 진열장</span><button class="xbtn" data-act="close" aria-label="닫기">✕</button>
      <p>정답 석판 밑에서 나온 유물이 모여요. <b>${n}/15</b></p>
      <div class="scroll"><div class="museum">${cards}</div></div></div>`);
  }

  openUnits(school, grade) {
    Packs.loadIndex().then((I) => {
      const cur = this.pack.entry;
      school = school || cur.school;
      const grades = [...new Set(I.packs.filter((p) => p.school === school).map((p) => p.grade))].sort();
      grade = grade && grades.includes(grade) ? grade : school === cur.school && grades.includes(cur.grade) ? cur.grade : grades[0];
      const list = I.packs.filter((p) => p.school === school && p.grade === grade).sort((a, b) => a.semester - b.semester || a.unit_order - b.unit_order);
      const t1 = ['elementary', 'middle'].map((s) => `<button class="${s === school ? 'on' : ''}" data-act="school" data-s="${s}">${s === 'elementary' ? '초등학교' : '중학교'}</button>`).join('');
      const t2 = grades.map((g) => `<button class="${g === grade ? 'on' : ''}" data-act="grade" data-s="${school}" data-g="${g}">${g}학년</button>`).join('');
      const rows = list.map((p) => `<button class="unit ${p.pack_id === cur.pack_id ? 'on' : ''}" data-act="pack" data-id="${p.pack_id}"><span class="k">${p.semester}학기 ${p.unit_order}단원</span><span>${p.title}</span><small>${p.items}문항</small></button>`).join('');
      this.openModal(`<div class="card"><span class="tag teal">단원 고르기</span><button class="xbtn" data-act="close" aria-label="닫기">✕</button>
        <div class="tabs">${t1}</div><div class="tabs">${t2}</div><div class="scroll"><div class="units">${rows}</div></div></div>`);
    });
  }

  async choosePack(id) {
    this.pack = await Packs.loadPack(id);
    this.save.pack = id;
    this.persist();
    try {
      const u = new URL(location.href);
      u.searchParams.set('pack', id);
      history.replaceState(null, '', u);
    } catch {}
    this.updatePackChip();
    this.closeModal();
    this.sfx.coin();
  }

  iconFor(i) {
    if (this.icons.has(i)) return this.icons.get(i);
    const r = this.world.renderer;
    const S = 160;
    const scene = new THREE.Scene();
    scene.add(new THREE.HemisphereLight('#ffffff', '#c89a5a', 2.2));
    const dl = new THREE.DirectionalLight('#ffffff', 2.2);
    dl.position.set(2, 4, 3);
    scene.add(dl);
    const o = part('Art_' + String(i).padStart(2, '0'));
    scene.add(o);
    const box = new THREE.Box3().setFromObject(o);
    const c = box.getCenter(new THREE.Vector3());
    const size = box.getSize(new THREE.Vector3()).length() || 0.3;
    const cam = new THREE.PerspectiveCamera(30, 1, 0.01, 50);
    cam.position.copy(c).add(new THREE.Vector3(0.5, 0.45, 1).normalize().multiplyScalar(size * 2.05));
    cam.lookAt(c);
    const rt = new THREE.WebGLRenderTarget(S, S, { samples: 4 });
    rt.texture.colorSpace = THREE.SRGBColorSpace;
    const prevClear = new THREE.Color();
    r.getClearColor(prevClear);
    const prevAlpha = r.getClearAlpha();
    r.setRenderTarget(rt);
    r.setClearColor(0x000000, 0);
    r.clear();
    r.render(scene, cam);
    const buf = new Uint8Array(S * S * 4);
    r.readRenderTargetPixels(rt, 0, 0, S, S, buf);
    r.setRenderTarget(null);
    r.setClearColor(prevClear, prevAlpha);
    rt.dispose();
    const cv = document.createElement('canvas');
    cv.width = cv.height = S;
    const g = cv.getContext('2d');
    const img = g.createImageData(S, S);
    for (let y = 0; y < S; y++) img.data.set(buf.subarray((S - 1 - y) * S * 4, (S - y) * S * 4), y * S * 4);
    g.putImageData(img, 0, 0);
    const url = cv.toDataURL('image/png');
    this.icons.set(i, url);
    return url;
  }

  bindUI() {
    const click = (id, fn) =>
      $(id).addEventListener('click', () => {
        this.sfx.unlock();
        this.sfx.click();
        fn();
      });
    click('btnPlay', () => (this.save.tut ? this.startExpedition(this.save.site || 0) : this.startTutorial()));
    click('btnBoard', () => this.openBoard());
    click('btnShop', () => this.openShop());
    click('btnMuseum', () => this.openMuseum());
    click('btnTut', () => this.startTutorial());
    click('btnPack', () => this.openUnits());
    click('dailyBtn', () => this.claimDaily());
    click('btnPause', () => this.togglePause());
    click('lsSkip', () => {
      this.save.tut = true;
      this.persist();
      this.startExpedition(0);
    });
    const mute = $('btnMute');
    const applyMute = () => {
      mute.classList.toggle('muted', this.sfx.muted);
      mute.setAttribute('aria-pressed', String(this.sfx.muted));
    };
    applyMute();
    mute.addEventListener('click', () => {
      this.sfx.unlock();
      this.sfx.setMuted(!this.sfx.muted);
      this.save.muted = this.sfx.muted;
      this.persist();
      applyMute();
    });
    $('feedback').addEventListener('click', (e) => {
      if (e.target.closest('[data-act="next"]')) this.skipResolve();
    });
    $('modal').addEventListener('click', (e) => {
      const b = e.target.closest('[data-act]');
      if (!b) return;
      this.sfx.unlock();
      this.sfx.click();
      const a = b.dataset.act;
      if (a === 'close') {
        this.closeModal();
        if (this.mode === 'title') this.refreshTitle();
      } else if (a === 'resume') this.togglePause();
      else if (a === 'title') this.enterTitle();
      else if (a === 'again') this.startExpedition(this.siteIdx);
      else if (a === 'board') {
        this.enterTitle();
        this.openBoard();
      } else if (a === 'site') this.startExpedition(+b.dataset.site);
      else if (a === 'go') this.startExpedition(0);
      else if (a === 'buy') {
        const k = b.dataset.k;
        const u = UP[k];
        const lv = this.save.up[k];
        const cost = u.costs[lv];
        if (cost !== undefined && this.save.coins >= cost) {
          this.save.coins -= cost;
          this.save.up[k]++;
          this.persist();
          this.sfx.coin();
          this.openShop();
          this.refreshTitle();
        }
      } else if (a === 'school') this.openUnits(b.dataset.s);
      else if (a === 'grade') this.openUnits(b.dataset.s, +b.dataset.g);
      else if (a === 'pack') this.choosePack(b.dataset.id);
    });
    // 문제판 높이가 바뀌면 카메라를 다시 맞춘다
    if (window.ResizeObserver) {
      const ro = new ResizeObserver(() => this.fitPlayCamera());
      ro.observe($('board'));
      ro.observe($('lesson'));
    }
  }

  // ───────────────────────── 자동 시험 훅 ─────────────────────────
  /** 실제 붓질 경로(useTool)로 석판 하나를 완전 발굴한다 */
  autoDig(i) {
    const f = this.field;
    const t = f.tablets[i];
    const L = this.lv();
    const P = this.player;
    P.pos.set(t.x, 0, t.z + TAB.d / 2 + 0.45);
    if (f.crustLeft(t) > 0) {
      this.setTool('chisel');
      const st = L.chiselR * 0.7;
      for (let z = t.z - TAB.d / 2 + 0.1; z <= t.z + TAB.d / 2 - 0.1; z += st)
        for (let x = t.x - TAB.w / 2 + 0.1; x <= t.x + TAB.w / 2 - 0.1; x += st) {
          const gx = Math.round((x + 3.2) * 30), gz = Math.round((z + 2.4) * 30);
          if (f.data[(gz * 192 + gx) * 4 + 1] > 0) this.useTool('chisel', x, z, 0.5, false);
        }
    }
    this.setTool('brush');
    const st = L.brushR * 0.75;
    const ins = L.brushR * 0.35;
    for (let z = t.z - TAB.d / 2 + ins; z <= t.z + TAB.d / 2 - ins + 1e-6; z += st)
      for (let x = t.x - TAB.w / 2 + ins; x <= t.x + TAB.w / 2 - ins + 1e-6; x += st) this.useTool('brush', x, z, 0.6, false);
    // 가장자리 마무리
    for (const z of [t.z - TAB.d / 2 + ins, t.z + TAB.d / 2 - ins])
      for (let x = t.x - TAB.w / 2 + ins; x <= t.x + TAB.w / 2 - ins + 1e-6; x += st) this.useTool('brush', x, z, 0.6, false);
    f.measure();
    this.checkTablets();
  }

  ensurePlaying() {
    if (this.mode !== 'expedition' || this.phase === 'results' || this.phase === 'failed' || this.phase === 'failing') this.startExpedition(0);
    else if (this.phase === 'resolve') this.skipResolve();
    else if (this.phase === 'paused') this.togglePause();
  }

  installHook() {
    const g = this;
    if (/[?&]debug\b/.test(location.search)) window.__G = g;
    window.__GAME_TEST__ = {
      ready: false,
      start() {
        g.closeModal();
        g.startExpedition(0);
      },
      getState() {
        return {
          score: g.score,
          lives: 3 - g.strikes,
          level: g.plotIdx + 1,
          phase: g.mode === 'title' ? 'title' : g.phase,
          mode: g.mode,
          solved: g.solved,
          coins: g.save.coins,
          tool: g.tool,
          timeLeft: Math.ceil(g.timeLeft),
          player: { x: +g.player.pos.x.toFixed(2), z: +g.player.pos.z.toFixed(2) },
          clean: g.field.tablets.map((t) => Math.round(t.clean * 100)),
          pack: g.pack?.entry?.pack_id,
        };
      },
      answerCorrect() {
        g.ensurePlaying();
        g.autoDig(g.correctIdx);
      },
      answerWrong() {
        g.ensurePlaying();
        g.autoDig((g.correctIdx + 1) % 4);
      },
      getLayout() {
        return { land: g.isLand, playW: g.W };
      },
      sampleProblems(n = 20) {
        return Packs.sampleItems(g.pack, n).map((it) => ({
          id: it.id,
          prompt: it.prompt,
          choices: it.choices.map(String),
          answer: String(it.answer),
          answerNumeric: Number.isFinite(Number(it.answerNumeric)) ? Number(it.answerNumeric) : undefined,
          unitConcept: it.unitConcept,
          explain: it.explain,
          format: it.format,
          difficulty: it.difficulty,
        }));
      },
    };
  }
}
