// WebAudio 합성 효과음 + 가벼운 배경 음악. 첫 사용자 제스처 뒤에만 AudioContext 를 연다.
export class Sfx {
  constructor() {
    this.ctx = null;
    this.muted = false;
    this.brushLevel = 0;
    this.bgmOn = false;
    this._step = 0;
  }
  unlock() {
    if (this.ctx) {
      if (this.ctx.state === 'suspended') this.ctx.resume().catch(() => {});
      return;
    }
    const AC = window.AudioContext || window.webkitAudioContext;
    if (!AC) return;
    const c = (this.ctx = new AC());
    this.master = c.createGain();
    this.master.gain.value = this.muted ? 0 : 0.8;
    this.master.connect(c.destination);
    // 공용 잡음 버퍼
    const len = c.sampleRate * 1.5;
    this.noise = c.createBuffer(1, len, c.sampleRate);
    const d = this.noise.getChannelData(0);
    for (let i = 0; i < len; i++) d[i] = Math.random() * 2 - 1;
    // 붓질 루프: 띠 대역 잡음, 게인을 계속 조절한다
    const src = c.createBufferSource();
    src.buffer = this.noise;
    src.loop = true;
    const bp = c.createBiquadFilter();
    bp.type = 'bandpass';
    bp.frequency.value = 2300;
    bp.Q.value = 0.6;
    this.brushFilter = bp;
    this.brushGain = c.createGain();
    this.brushGain.gain.value = 0;
    src.connect(bp).connect(this.brushGain).connect(this.master);
    src.start();
    // 배경 음악 버스
    this.music = c.createGain();
    this.music.gain.value = 0.16;
    this.music.connect(this.master);
    this._timer = setInterval(() => this._schedule(), 120);
    this._next = c.currentTime + 0.1;
  }
  setMuted(m) {
    this.muted = m;
    if (this.master) this.master.gain.setTargetAtTime(m ? 0 : 0.8, this.ctx.currentTime, 0.03);
  }
  setBgm(on) {
    this.bgmOn = on;
  }
  /** 매 프레임: 붓질 세기(0..1) */
  brush(level, pitch = 1) {
    if (!this.ctx) return;
    const t = this.ctx.currentTime;
    this.brushGain.gain.setTargetAtTime(Math.min(0.5, level * 0.5), t, 0.05);
    this.brushFilter.frequency.setTargetAtTime(1700 + 900 * pitch, t, 0.08);
  }
  _env(node, t, a, peak, dec) {
    node.gain.setValueAtTime(0.0001, t);
    node.gain.exponentialRampToValueAtTime(peak, t + a);
    node.gain.exponentialRampToValueAtTime(0.0001, t + a + dec);
  }
  tone(freq, dur = 0.15, type = 'triangle', vol = 0.3, when = 0, slideTo = 0, bus = null) {
    if (!this.ctx) return;
    const c = this.ctx;
    const t = c.currentTime + when;
    const o = c.createOscillator();
    const g = c.createGain();
    o.type = type;
    o.frequency.setValueAtTime(freq, t);
    if (slideTo) o.frequency.exponentialRampToValueAtTime(slideTo, t + dur);
    this._env(g, t, 0.008, vol, dur);
    o.connect(g).connect(bus || this.master);
    o.start(t);
    o.stop(t + dur + 0.05);
  }
  burst(dur = 0.2, freq = 1200, q = 0.8, vol = 0.4, when = 0, type = 'bandpass') {
    if (!this.ctx) return;
    const c = this.ctx;
    const t = c.currentTime + when;
    const s = c.createBufferSource();
    s.buffer = this.noise;
    const f = c.createBiquadFilter();
    f.type = type;
    f.frequency.value = freq;
    f.Q.value = q;
    const g = c.createGain();
    this._env(g, t, 0.004, vol, dur);
    s.connect(f).connect(g).connect(this.master);
    s.start(t, Math.random() * 1.0);
    s.stop(t + dur + 0.05);
  }
  chisel() {
    this.tone(2400 + Math.random() * 500, 0.06, 'square', 0.08);
    this.burst(0.05, 4200, 2, 0.25);
  }
  chunk() {
    this.burst(0.18, 380, 0.8, 0.5, 0, 'lowpass');
    this.tone(150, 0.12, 'sine', 0.3, 0, 80);
  }
  thud() {
    this.burst(0.08, 900, 1, 0.12);
  }
  snap() {
    this.burst(0.35, 900, 0.5, 0.35);
    this.tone(660, 0.12, 'triangle', 0.18, 0.05, 1320);
  }
  correct() {
    [523, 659, 784, 1047].forEach((f, i) => this.tone(f, 0.22, 'triangle', 0.26, i * 0.08));
    [1568, 2093, 2637].forEach((f, i) => this.tone(f, 0.18, 'sine', 0.08, 0.34 + i * 0.05));
  }
  wrong() {
    this.burst(0.3, 500, 0.7, 0.6, 0, 'lowpass');
    this.burst(0.12, 2600, 1.5, 0.3, 0.02);
    this.tone(220, 0.35, 'sawtooth', 0.12, 0.05, 110);
  }
  shatter() {
    for (let i = 0; i < 5; i++) this.burst(0.09, 3000 + Math.random() * 3000, 3, 0.3, i * 0.03);
    for (let i = 0; i < 4; i++) this.tone(1800 + Math.random() * 2400, 0.12, 'sine', 0.07, i * 0.04);
  }
  squeak() {
    this.tone(1300, 0.12, 'sine', 0.2, 0, 2400);
    this.tone(1900, 0.1, 'sine', 0.14, 0.12, 1200);
  }
  tick() {
    this.tone(1500, 0.04, 'square', 0.05);
  }
  coin() {
    this.tone(1320, 0.07, 'square', 0.07);
    this.tone(1980, 0.16, 'square', 0.07, 0.06);
  }
  click() {
    this.tone(880, 0.05, 'triangle', 0.14, 0, 1200);
  }
  whoosh() {
    this.burst(0.45, 700, 0.4, 0.22);
  }
  stamp() {
    this.burst(0.2, 200, 0.6, 0.7, 0, 'lowpass');
    this.tone(90, 0.25, 'sine', 0.4);
  }
  _schedule() {
    if (!this.ctx || !this.bgmOn || document.hidden) {
      if (this.ctx) this._next = this.ctx.currentTime + 0.1;
      return;
    }
    const c = this.ctx;
    // D 장조 5음계 가벼운 퉁김 + 셰이커
    const scale = [293.7, 329.6, 370, 440, 493.9, 587.3, 659.3];
    const bass = [146.8, 146.8, 196, 220];
    const beat = 0.23;
    while (this._next < c.currentTime + 0.4) {
      const s = this._step++;
      const when = this._next - c.currentTime;
      const bar = Math.floor(s / 8) % 4;
      if (s % 8 === 0) this.tone(bass[bar], 0.4, 'sine', 0.5, when, 0, this.music);
      if (s % 2 === 0 || Math.random() < 0.35) {
        const k = (s * 3 + bar * 2 + ((s * 7) % 5)) % scale.length;
        this.tone(scale[k], 0.18, 'triangle', 0.28, when, 0, this.music);
      }
      if (s % 2 === 1) {
        const t = c.currentTime + when;
        const src = c.createBufferSource();
        src.buffer = this.noise;
        const f = c.createBiquadFilter();
        f.type = 'highpass';
        f.frequency.value = 6000;
        const g = c.createGain();
        this._env(g, t, 0.003, 0.12, 0.05);
        src.connect(f).connect(g).connect(this.music);
        src.start(t, Math.random());
        src.stop(t + 0.08);
      }
      this._next += beat;
    }
  }
}
