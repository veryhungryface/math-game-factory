import * as THREE from 'three';
import { mat, part, addOutline } from './world.js';
import { layoutMath, drawMathLayout } from './mathtext.js';

// 발굴 격자: 구획 전체를 덮는 RGBA8 텍스처 (R=모래, G=굳은 흙덩이). 30 텍셀/미터.
export const F = { W: 6.4, D: 4.8, GW: 192, GH: 144 };
const TPU = F.GW / F.W; // texels per unit
export const PLOT = { x0: -2.7, x1: 2.7, z0: -2.0, z1: 2.0 };
export const TAB = { w: 2.0, d: 1.35 };
export const TAB_POS = [
  [-1.3, -0.95],
  [1.3, -0.95],
  [-1.3, 0.95],
  [1.3, 0.95],
];
const CLEAN_EPS = 24; // 이 미만은 깨끗한 것으로 본다
export const SNAP_AT = 0.9;
export const READ_AT = 0.4;

const PROP_SPOTS = [
  [0, 0],
  [0, -2.12],
  [-2.98, 0.0],
  [2.98, 0.0],
  [-2.95, -2.15],
  [2.95, 2.18],
  [2.95, -2.15],
  [-2.95, 2.2],
];
export const PROP_KINDS = {
  pot: { model: 'Pot', r: 0.24, say: ['쨍그랑! 3천 년 된 항아리!', '내 항아리!'], color: '#d9824a' },
  bones: { model: 'Bones', r: 0.26, say: ['뼈는 살살 다뤄요!', '화석 뼈가 부러졌어!'], color: '#f3ead6' },
  scroll: { model: 'Scroll', r: 0.24, say: ['두루마리가 찢어졌어!', '옛날 지도가!'], color: '#f1e2b9' },
  lizard: { model: 'Lizard', r: 0.22, say: ['깩! 내 꼬리!', '앗 따가워!'], color: '#6cc04a' },
};

// 값 잡음
function hash(x, y) {
  const s = Math.sin(x * 127.1 + y * 311.7) * 43758.5453;
  return s - Math.floor(s);
}
function vnoise(x, y) {
  const xi = Math.floor(x), yi = Math.floor(y);
  const xf = x - xi, yf = y - yi;
  const u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf);
  const a = hash(xi, yi), b = hash(xi + 1, yi), c = hash(xi, yi + 1), d = hash(xi + 1, yi + 1);
  return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
}
function fbm(x, y) {
  return vnoise(x, y) * 0.55 + vnoise(x * 2.1 + 5.2, y * 2.1 + 1.3) * 0.3 + vnoise(x * 4.3 + 9.1, y * 4.3 + 7.7) * 0.15;
}

// ───────────── 석판 얼굴 텍스처(돌 바탕 + 음각 글자) ─────────────
const FACE_W = 512, FACE_H = 346;
let stoneBase = null;
function makeStoneBase() {
  const c = document.createElement('canvas');
  c.width = FACE_W;
  c.height = FACE_H;
  const g = c.getContext('2d');
  const gr = g.createLinearGradient(0, 0, 0, FACE_H);
  gr.addColorStop(0, '#e9dbbb');
  gr.addColorStop(1, '#d7c39b');
  g.fillStyle = gr;
  g.fillRect(0, 0, FACE_W, FACE_H);
  for (let i = 0; i < 2600; i++) {
    const x = Math.random() * FACE_W, y = Math.random() * FACE_H;
    g.fillStyle = Math.random() < 0.5 ? 'rgba(120,90,50,0.10)' : 'rgba(255,250,235,0.18)';
    g.fillRect(x, y, 2 + Math.random() * 3, 2 + Math.random() * 3);
  }
  for (let i = 0; i < 6; i++) {
    g.strokeStyle = 'rgba(110,80,45,0.16)';
    g.lineWidth = 1.5;
    g.beginPath();
    let x = Math.random() * FACE_W, y = Math.random() * FACE_H;
    g.moveTo(x, y);
    for (let k = 0; k < 5; k++) g.lineTo((x += (Math.random() - 0.5) * 70), (y += (Math.random() - 0.5) * 40));
    g.stroke();
  }
  // 새긴 테두리
  g.strokeStyle = 'rgba(95,70,40,0.45)';
  g.lineWidth = 5;
  g.strokeRect(18, 18, FACE_W - 36, FACE_H - 36);
  g.strokeStyle = 'rgba(255,248,228,0.55)';
  g.lineWidth = 2;
  g.strokeRect(22, 22, FACE_W - 36, FACE_H - 36);
  stoneBase = c;
}

function drawFace(canvas, label, idx, opts = {}) {
  if (!stoneBase) makeStoneBase();
  const g = canvas.getContext('2d');
  g.drawImage(stoneBase, 0, 0);
  // 번호 동그라미
  if (idx >= 0) {
    const cx = 52, cy = 52;
    g.fillStyle = 'rgba(95,70,40,0.75)';
    g.beginPath();
    g.arc(cx, cy, 24, 0, Math.PI * 2);
    g.fill();
    g.fillStyle = '#f6ead0';
    g.font = '900 30px "Apple SD Gothic Neo","Malgun Gothic",sans-serif';
    g.textAlign = 'center';
    g.textBaseline = 'middle';
    g.fillText(String(idx + 1), cx, cy + 2);
  }
  if (label) {
    const L = layoutMath(g, label, FACE_W - 96, FACE_H - 96, 132, 50);
    drawMathLayout(g, L, FACE_W / 2 + 8, FACE_H / 2 + 6, [
      { dx: 3, dy: 4, color: 'rgba(255,250,232,0.9)' },
      { dx: -2, dy: -2, color: 'rgba(30,16,4,0.6)' },
      { dx: 0, dy: 0, color: '#3a2512' },
    ]);
  }
  if (opts.crack) {
    const r = opts.crack;
    g.strokeStyle = 'rgba(40,22,8,0.9)';
    g.lineCap = 'round';
    for (let k = 0; k < 7; k++) {
      let x = FACE_W * (0.4 + r() * 0.2), y = FACE_H * (0.4 + r() * 0.2);
      const a0 = (k / 7) * Math.PI * 2 + r() * 0.6;
      g.lineWidth = 7 - k * 0.5;
      g.beginPath();
      g.moveTo(x, y);
      for (let s = 0; s < 6; s++) {
        const a = a0 + (r() - 0.5) * 0.9;
        x += Math.cos(a) * (30 + r() * 30);
        y += Math.sin(a) * (20 + r() * 26);
        g.lineTo(x, y);
      }
      g.stroke();
    }
    g.fillStyle = 'rgba(60,30,10,0.12)';
    g.fillRect(0, 0, FACE_W, FACE_H);
  }
}

function seeded(seed) {
  let s = seed >>> 0 || 1;
  return () => ((s = (s * 1664525 + 1013904223) >>> 0) / 4294967296);
}

// ───────────── 모래 덮개 재질(정점 변위 + 잡음 색) ─────────────
function makeSandMaterial(tex) {
  const m = new THREE.MeshLambertMaterial({ color: '#ffffff', side: THREE.FrontSide });
  m.userData.uniforms = {
    uDig: { value: tex },
    uField: { value: new THREE.Vector2(F.W, F.D) },
    uSand: { value: new THREE.Color('#f7d283') },
    uSandDark: { value: new THREE.Color('#c98e47') },
    uCrust: { value: new THREE.Color('#e4d8bf') },
    uCrustDark: { value: new THREE.Color('#a5937a') },
    uRise: { value: 1 },
    uTexel: { value: new THREE.Vector2(1 / F.GW, 1 / F.GH) },
  };
  m.onBeforeCompile = (sh) => {
    Object.assign(sh.uniforms, m.userData.uniforms);
    sh.vertexShader = sh.vertexShader
      .replace(
        '#include <common>',
        `#include <common>
        uniform sampler2D uDig; uniform vec2 uField; uniform float uRise; uniform vec2 uTexel;
        varying vec2 vDig; varying vec2 vXZ;
        float hgt(vec2 uv){ vec4 d=texture2D(uDig,uv); float s=d.r, c=d.g;
          return uRise*( (s>0.07? 0.045+s*0.15 : 0.0) + (c>0.07? 0.05+c*0.06 : 0.0) ); }`
      )
      .replace(
        '#include <beginnormal_vertex>',
        `vec2 dUV = vec2(position.x/uField.x+0.5, position.z/uField.y+0.5);
        float hL=hgt(dUV-vec2(uTexel.x,0.0)), hR=hgt(dUV+vec2(uTexel.x,0.0));
        float hD=hgt(dUV-vec2(0.0,uTexel.y)), hU=hgt(dUV+vec2(0.0,uTexel.y));
        vec3 objectNormal = normalize(vec3((hL-hR)/(2.0*uTexel.x*uField.x), 1.0, (hD-hU)/(2.0*uTexel.y*uField.y)));
        #ifdef USE_TANGENT
        vec3 objectTangent = vec3( tangent.xyz );
        #endif`
      )
      .replace(
        '#include <begin_vertex>',
        `vec3 transformed = vec3(position);
        vec4 dd = texture2D(uDig, dUV);
        vDig = dd.rg; vXZ = position.xz;
        transformed.y += hgt(dUV);`
      );
    sh.fragmentShader = sh.fragmentShader
      .replace(
        '#include <common>',
        `#include <common>
        uniform vec3 uSand, uSandDark, uCrust, uCrustDark; varying vec2 vDig; varying vec2 vXZ;
        float h21(vec2 p){ return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
        float vn(vec2 p){ vec2 i=floor(p), f=fract(p); f=f*f*(3.0-2.0*f);
          return mix(mix(h21(i),h21(i+vec2(1,0)),f.x), mix(h21(i+vec2(0,1)),h21(i+vec2(1,1)),f.x), f.y); }`
      )
      .replace(
        '#include <color_fragment>',
        `#include <color_fragment>
        float s=vDig.x, c=vDig.y;
        float grain = vn(vXZ*38.0);
        float edge = 0.07 + grain*0.07;
        float amt = max(s,c);
        if (amt < edge) discard;
        vec3 sandC = mix(uSandDark, uSand, smoothstep(0.08, 0.42, s));
        sandC *= 0.9 + 0.2*vn(vXZ*9.0) + 0.08*grain;
        float rip = sin(vXZ.x*15.0 + vXZ.y*6.0 + vn(vXZ*2.5)*7.0);
        sandC *= 1.0 + 0.07*smoothstep(0.55,1.0,rip) - 0.05*smoothstep(0.55,1.0,-rip);
        // 젖은 듯 진한 가장자리 고리(쓸어 낸 자국)
        sandC = mix(sandC, uSandDark*0.92, (1.0-smoothstep(edge, edge+0.12, s))*0.7);
        vec3 col = sandC;
        if (c > 0.08 && s < 0.38) {
          float cr = vn(vXZ*16.0);
          vec3 crustC = mix(uCrustDark, uCrust, 0.55 + 0.45*cr);
          crustC *= 0.85 + 0.25*smoothstep(0.2,0.9,c);
          float lines = smoothstep(0.47,0.5,abs(fract(vn(vXZ*7.0)*6.0)-0.5));
          crustC = mix(crustC, uCrustDark*0.8, lines*0.5);
          col = mix(col, crustC, smoothstep(0.08, 0.22, c) * (1.0 - smoothstep(0.22, 0.38, s)));
        }
        diffuseColor.rgb = col;`
      );
  };
  return m;
}

export class Field {
  constructor(scene) {
    this.scene = scene;
    this.data = new Uint8Array(F.GW * F.GH * 4);
    for (let i = 3; i < this.data.length; i += 4) this.data[i] = 255;
    const tex = (this.tex = new THREE.DataTexture(this.data, F.GW, F.GH, THREE.RGBAFormat));
    tex.minFilter = tex.magFilter = THREE.LinearFilter;
    tex.generateMipmaps = false;
    tex.needsUpdate = true;
    this.dirtyRows = new Int32Array(F.GH * 2).fill(-1); // [minX,maxX] per row
    this.anyDirty = false;

    // 발굴 바닥(다진 흙 + 측량 격자)
    const fc = document.createElement('canvas');
    fc.width = 512;
    fc.height = 384;
    this.floorCanvas = fc;
    this.floorTex = new THREE.CanvasTexture(fc);
    this.floorTex.colorSpace = THREE.SRGBColorSpace;
    this.floorTex.anisotropy = 4;
    const floor = new THREE.Mesh(new THREE.PlaneGeometry(PLOT.x1 - PLOT.x0 + 0.3, PLOT.z1 - PLOT.z0 + 0.3), new THREE.MeshLambertMaterial({ map: this.floorTex }));
    floor.rotation.x = -Math.PI / 2;
    floor.position.y = 0.004;
    floor.receiveShadow = true;
    scene.add(floor);
    this.floor = floor;

    // 모래 덮개
    const geo = new THREE.PlaneGeometry(F.W, F.D, 150, 112);
    geo.rotateX(-Math.PI / 2);
    this.sandMat = makeSandMaterial(tex);
    const sand = (this.sand = new THREE.Mesh(geo, this.sandMat));
    sand.position.y = 0.0;
    sand.receiveShadow = true;
    sand.castShadow = false;
    sand.renderOrder = 1;
    scene.add(sand);

    // 석판 4개
    this.tablets = TAB_POS.map(([x, z], i) => {
      const g = new THREE.Group();
      g.position.set(x, -0.118, z);
      const slab = part('Tablet', true);
      slab.scale.set(TAB.w / 1.0, 1, TAB.d / 0.7);
      g.add(slab);
      addOutline(slab);
      const can = document.createElement('canvas');
      can.width = FACE_W;
      can.height = FACE_H;
      const t = new THREE.CanvasTexture(can);
      t.colorSpace = THREE.SRGBColorSpace;
      t.anisotropy = 8;
      const fm = new THREE.MeshLambertMaterial({ map: t, emissive: new THREE.Color('#000000') });
      const face = new THREE.Mesh(new THREE.PlaneGeometry(TAB.w * 0.92, TAB.d * 0.9), fm);
      face.rotation.x = -Math.PI / 2;
      face.position.y = 0.1415;
      face.receiveShadow = true;
      g.add(face);
      scene.add(g);
      const r = this.rectOf(x, z);
      return { i, g, slab, face, can, tex: t, fm, x, z, rect: r, clean: 0, label: '', state: 'buried', anim: 0, glow: 0, glowColor: new THREE.Color('#ffd23f'), crust0: 0 };
    });
    this.props = [];
    this.locked = false;
    this.rise = 1;
    this.riseT = 1;
    this._c = new THREE.Color();
  }

  rectOf(x, z) {
    const gx0 = Math.round((x - TAB.w / 2 + F.W / 2) * TPU);
    const gx1 = Math.round((x + TAB.w / 2 + F.W / 2) * TPU);
    const gz0 = Math.round((z - TAB.d / 2 + F.D / 2) * TPU);
    const gz1 = Math.round((z + TAB.d / 2 + F.D / 2) * TPU);
    return { gx0, gx1, gz0, gz1, n: (gx1 - gx0) * (gz1 - gz0) };
  }

  setPalette(site) {
    // 석판 색은 유적지 팔레트로 다시 굽는다
    for (const t of this.tablets) {
      t.g.remove(t.slab);
      const slab = part('Tablet', true);
      slab.scale.set(TAB.w / 1.0, 1, TAB.d / 0.7);
      addOutline(slab);
      t.g.add(slab);
      t.slab = slab;
    }
    const u = this.sandMat.userData.uniforms;
    u.uSand.value.set(site.sand);
    u.uSandDark.value.set(site.sandDark);
    u.uCrust.value.set(site.crust);
    u.uCrustDark.value.set(site.crustDark);
    // 바닥 텍스처
    const g = this.floorCanvas.getContext('2d');
    g.fillStyle = site.floor;
    g.fillRect(0, 0, 512, 384);
    for (let i = 0; i < 1400; i++) {
      g.fillStyle = Math.random() < 0.5 ? 'rgba(0,0,0,0.07)' : 'rgba(255,255,255,0.07)';
      g.fillRect(Math.random() * 512, Math.random() * 384, 3, 3);
    }
    g.strokeStyle = site.floorLine;
    g.lineWidth = 2;
    for (let x = 0; x <= 512; x += 512 / 8) {
      g.beginPath(); g.moveTo(x, 0); g.lineTo(x, 384); g.stroke();
    }
    for (let y = 0; y <= 384; y += 384 / 6) {
      g.beginPath(); g.moveTo(0, y); g.lineTo(512, y); g.stroke();
    }
    g.strokeStyle = 'rgba(255,255,255,0.6)';
    g.lineWidth = 6;
    const c = 26;
    for (const [x, y, sx, sy] of [[8, 8, 1, 1], [504, 8, -1, 1], [8, 376, 1, -1], [504, 376, -1, -1]]) {
      g.beginPath(); g.moveTo(x, y + sy * c); g.lineTo(x, y); g.lineTo(x + sx * c, y); g.stroke();
    }
    this.floorTex.needsUpdate = true;
  }

  /** 새 구획: 모래를 다시 채우고 석판 글자·흙덩이·소품을 배치 */
  reset({ labels, crust = 'random', props = 'random', seed = (Math.random() * 1e9) | 0, numbered = true, demo = false, armed = true }) {
    this.armed = armed;
    const rnd = seeded(seed);
    const d = this.data;
    const ox = rnd() * 50, oz = rnd() * 50;
    for (let gz = 0; gz < F.GH; gz++) {
      for (let gx = 0; gx < F.GW; gx++) {
        const x = gx / TPU - F.W / 2;
        const z = gz / TPU - F.D / 2;
        const i = (gz * F.GW + gx) * 4;
        // 구획 경계 밖은 모래 없음 (경계는 잡음으로 들쭉날쭉)
        const ex = Math.max(PLOT.x0 - x, x - PLOT.x1);
        const ez = Math.max(PLOT.z0 - z, z - PLOT.z1);
        const out = Math.max(ex, ez) - (fbm(x * 2 + ox, z * 2 + oz) - 0.5) * 0.35;
        let s = 0;
        if (out < 0.12) {
          const n = fbm(x * 1.6 + ox, z * 1.6 + oz);
          s = 150 + n * 105;
          if (out > -0.05) s *= Math.max(0, (0.12 - out) / 0.17);
        }
        d[i] = Math.max(0, Math.min(255, s | 0));
        d[i + 1] = 0;
        d[i + 2] = 0;
      }
    }
    // 흙덩이
    const crustTabs = crust === 'all' ? [0, 1, 2, 3] : crust === 'none' ? [] : pickN([0, 1, 2, 3], 2 + (rnd() < 0.45 ? 1 : 0), rnd);
    for (const ti of crustTabs) {
      const t = this.tablets[ti];
      const cx = t.x + (rnd() - 0.5) * 0.7;
      const cz = t.z + (rnd() - 0.5) * 0.3;
      const rx = 0.34 + rnd() * 0.16;
      const rz = 0.24 + rnd() * 0.1;
      this.stampCrust(cx, cz, rx, rz, t, ox + ti * 3.3, oz);
    }
    for (const t of this.tablets) {
      t.crust0 = this.sumRect(t.rect, 1);
      t.cov0 = 0;
    }
    // 석판
    this.tablets.forEach((t, i) => {
      t.label = labels[i] ?? '';
      t.state = 'buried';
      t.anim = 0;
      t.glow = 0;
      t.g.position.set(t.x, -0.118, t.z);
      t.g.rotation.set(0, 0, 0);
      t.g.scale.setScalar(1);
      t.g.visible = true;
      t.fm.emissive.setRGB(0, 0, 0);
      drawFace(t.can, t.label, numbered ? i : -1);
      t.tex.needsUpdate = true;
      t.clean = 0;
    });
    // 소품
    for (const p of this.props) this.scene.remove(p.obj);
    this.props = [];
    if (props !== 'none') {
      const kinds = Array.isArray(props) ? props : pickN(['pot', 'bones', 'scroll', 'lizard', 'pot'], 3, rnd);
      const spots = Array.isArray(props) ? PROP_SPOTS.slice(0, kinds.length) : pickN(PROP_SPOTS.slice(), kinds.length, rnd);
      if (!Array.isArray(props) && rnd() < 0.6) spots[0] = PROP_SPOTS[0];
      kinds.forEach((k, i) => this.addProp(k, spots[i][0], spots[i][1], rnd));
    }
    this.locked = false;
    this.fullUpload();
    this.measure();
    this.rise = 0;
    this.riseT = 0;
    this.demo = demo;
  }

  addProp(kind, x, z, rnd = Math.random) {
    const K = PROP_KINDS[kind];
    const obj = part(K.model, true);
    obj.position.set(x, 0, z);
    obj.rotation.y = rnd() * Math.PI * 2;
    obj.traverse((o) => {
      if (o.isMesh) {
        o.castShadow = true;
      }
    });
    addOutline(obj);
    this.scene.add(obj);
    const p = { kind, K, obj, x, z, home: [x, z], r: K.r, broken: false, warn: 0, flee: 0, t: rnd() * 10, tail: obj.getObjectByName('Tail') };
    this.props.push(p);
    return p;
  }

  stampCrust(cx, cz, rx, rz, t, ox, oz) {
    const d = this.data;
    const r = t.rect;
    for (let gz = r.gz0 + 4; gz < r.gz1 - 4; gz++) {
      for (let gx = r.gx0 + 4; gx < r.gx1 - 4; gx++) {
        const x = gx / TPU - F.W / 2;
        const z = gz / TPU - F.D / 2;
        const e = ((x - cx) / rx) ** 2 + ((z - cz) / rz) ** 2 + (fbm(x * 5 + ox, z * 5 + oz) - 0.5) * 0.9;
        if (e < 1) {
          const i = (gz * F.GW + gx) * 4;
          d[i + 1] = Math.max(d[i + 1], Math.min(255, 200 + (1 - e) * 120) | 0);
        }
      }
    }
  }

  fullUpload() {
    this.tex.clearUpdateRanges();
    this.tex.needsUpdate = true;
    this.fullPending = true;
    this.dirtyRows.fill(-1);
    this.anyDirty = false;
  }

  markDirty(gz, gx0, gx1) {
    const k = gz * 2;
    if (this.dirtyRows[k] < 0) {
      this.dirtyRows[k] = gx0;
      this.dirtyRows[k + 1] = gx1;
    } else {
      if (gx0 < this.dirtyRows[k]) this.dirtyRows[k] = gx0;
      if (gx1 > this.dirtyRows[k + 1]) this.dirtyRows[k + 1] = gx1;
    }
    this.anyDirty = true;
  }

  /** 바뀐 행만 GPU 로 올린다 */
  flush() {
    if (this.fullPending) {
      // 전체 업로드가 아직 안 끝났다 — 부분 범위를 섞으면 이전 구획 데이터가 남는다
      this.fullPending = false;
      this.dirtyRows.fill(-1);
      this.anyDirty = false;
      this.tex.clearUpdateRanges();
      this.tex.needsUpdate = true;
      return;
    }
    if (!this.anyDirty) return;
    const rows = this.dirtyRows;
    for (let gz = 0; gz < F.GH; gz++) {
      const a = rows[gz * 2];
      if (a < 0) continue;
      const b = rows[gz * 2 + 1];
      this.tex.addUpdateRange((gz * F.GW + a) * 4, (b - a + 1) * 4);
      rows[gz * 2] = -1;
    }
    this.tex.needsUpdate = true;
    this.anyDirty = false;
  }

  /**
   * 도구 적용 — 입력·튜토리얼·자동 시험 경로가 모두 이 함수 하나를 쓴다.
   * brush: 모래(R)만 쓸어 냄. chisel: 흙덩이(G)를 깨고 모래도 조금.
   * 반환: { sand, crust, crustUnderBrush }
   */
  applyTool(tool, x, z, dt, lv) {
    if (this.locked) return null;
    const res = { sand: 0, crust: 0, crustUnderBrush: 0 };
    const radius = tool === 'brush' ? lv.brushR : lv.chiselR;
    const rr = radius * TPU;
    const cgx = (x + F.W / 2) * TPU;
    const cgz = (z + F.D / 2) * TPU;
    const gx0 = Math.max(0, Math.floor(cgx - rr)), gx1 = Math.min(F.GW - 1, Math.ceil(cgx + rr));
    const gz0 = Math.max(0, Math.floor(cgz - rr)), gz1 = Math.min(F.GH - 1, Math.ceil(cgz + rr));
    const d = this.data;
    const inv = 1 / (rr * rr);
    for (let gz = gz0; gz <= gz1; gz++) {
      let touched = false;
      for (let gx = gx0; gx <= gx1; gx++) {
        const dx = gx - cgx, dz = gz - cgz;
        const q = (dx * dx + dz * dz) * inv;
        if (q >= 1) continue;
        const f = 1 - q * q;
        const i = (gz * F.GW + gx) * 4;
        if (tool === 'brush') {
          const s = d[i];
          if (s > 0) {
            let ns = s - lv.brushRate * dt * f;
            if (ns < CLEAN_EPS) ns = 0;
            d[i] = ns;
            res.sand += s - d[i];
            touched = true;
          } else if (d[i + 1] > CLEAN_EPS) res.crustUnderBrush++;
        } else {
          const c = d[i + 1];
          if (c > 0) {
            let nc = c - lv.chiselPow * dt * (0.6 + 0.4 * f);
            if (nc < CLEAN_EPS) nc = 0;
            d[i + 1] = nc;
            res.crust += c - d[i + 1];
            touched = true;
          }
          const s = d[i];
          if (s > 0) {
            let ns = s - 60 * dt * f;
            if (ns < CLEAN_EPS) ns = 0;
            d[i] = ns;
            res.sand += s - d[i];
            touched = true;
          }
        }
      }
      if (touched) this.markDirty(gz, gx0, gx1);
    }
    return res;
  }

  sumRect(r, ch) {
    const d = this.data;
    let s = 0;
    for (let gz = r.gz0; gz < r.gz1; gz++) {
      let i = (gz * F.GW + r.gx0) * 4 + ch;
      for (let gx = r.gx0; gx < r.gx1; gx++, i += 4) s += d[i];
    }
    return s;
  }

  /** 석판별 노출 비율(모래·흙덩이 중 큰 값 기준) */
  measure() {
    const d = this.data;
    for (const t of this.tablets) {
      const r = t.rect;
      let cov = 0;
      for (let gz = r.gz0; gz < r.gz1; gz++) {
        let i = (gz * F.GW + r.gx0) * 4;
        for (let gx = r.gx0; gx < r.gx1; gx++, i += 4) {
          const a = d[i], b = d[i + 1];
          cov += a > b ? a : b;
        }
      }
      t.cov = cov;
      if (!t.cov0) t.cov0 = Math.max(cov, 1);
      t.clean = Math.max(0, 1 - cov / t.cov0);
    }
  }

  crustLeft(t) {
    return t.crust0 > 0 ? this.sumRect(t.rect, 1) / t.crust0 : 0;
  }

  /** 남은 모래를 한 번에 쓸어 낸다(스냅). 쓸려 나간 위치 몇 곳을 돌려준다(먼지 효과용). */
  clearTablet(t, margin = 3) {
    const d = this.data;
    const r = t.rect;
    const pts = [];
    for (let gz = Math.max(0, r.gz0 - margin); gz < Math.min(F.GH, r.gz1 + margin); gz++) {
      for (let gx = Math.max(0, r.gx0 - margin); gx < Math.min(F.GW, r.gx1 + margin); gx++) {
        const i = (gz * F.GW + gx) * 4;
        if ((d[i] > 40 || d[i + 1] > 40) && pts.length < 60 && Math.random() < 0.08) pts.push([gx / TPU - F.W / 2, gz / TPU - F.D / 2]);
        d[i] = 0;
        d[i + 1] = 0;
      }
      this.markDirty(gz, Math.max(0, r.gx0 - margin), Math.min(F.GW - 1, r.gx1 + margin - 1));
    }
    t.clean = 1;
    return pts;
  }

  /** 높이(모래) 조회 — 캐릭터 발 높이·입자 높이 */
  heightAt(x, z) {
    const gx = Math.round((x + F.W / 2) * TPU), gz = Math.round((z + F.D / 2) * TPU);
    if (gx < 0 || gz < 0 || gx >= F.GW || gz >= F.GH) return 0;
    const i = (gz * F.GW + gx) * 4;
    const s = this.data[i] / 255, c = this.data[i + 1] / 255;
    return this.rise * ((s > 0.07 ? 0.045 + s * 0.15 : 0) + (c > 0.07 ? 0.05 + c * 0.06 : 0));
  }

  crack(t) {
    drawFace(t.can, t.label, t.i, { crack: seeded(t.i * 977 + 13) });
    t.tex.needsUpdate = true;
  }

  update(dt, time) {
    if (this.rise < 1) {
      this.riseT += dt;
      this.rise = Math.min(1, this.riseT / 0.55);
      const e = 1 - Math.pow(1 - this.rise, 3);
      this.sandMat.userData.uniforms.uRise.value = e;
    }
    for (const t of this.tablets) {
      if (t.state === 'lift') {
        t.anim += dt;
        const k = Math.min(1, t.anim / 0.7);
        const e = 1 - Math.pow(1 - k, 3);
        t.g.position.y = -0.118 + e * 0.75 + Math.sin(time * 3) * 0.03 * k;
        t.g.rotation.x = e * 0.55;
        t.glow = 0.32 + Math.sin(time * 8) * 0.1;
      } else if (t.state === 'crack') {
        t.anim += dt;
        const k = Math.min(1, t.anim / 0.5);
        t.g.position.y = -0.118 - k * 0.012;
        t.g.rotation.z = k < 1 ? Math.sin(t.anim * 50) * 0.02 * (1 - k) : 0;
        t.g.position.x = t.x + (k < 1 ? Math.sin(t.anim * 70) * 0.04 * (1 - k) : 0);
        t.glow = 0;
      } else if (t.state === 'reveal') {
        t.anim += dt;
        t.glow = 0.22 + Math.sin(time * 6) * 0.12;
      } else if (t.state === 'buried') {
        t.glow = this.armed && t.clean >= 0.6 ? (t.clean - 0.6) * 0.7 * (0.6 + 0.4 * Math.sin(time * 9)) : 0;
      } else if (t.state === 'shown') {
        t.glow = 0;
      }
      this._c.copy(t.glowColor).multiplyScalar(t.glow);
      t.fm.emissive.copy(this._c);
    }
    // 소품: 도마뱀 배회·경고 흔들림
    for (const p of this.props) {
      if (p.broken) continue;
      p.t += dt;
      if (p.kind === 'lizard') {
        const sp = p.flee > 0 ? 2.2 : 0.35;
        p.flee = Math.max(0, p.flee - dt);
        const a = p.t * (p.flee > 0 ? 2.5 : 0.6);
        const nx = p.home[0] + Math.cos(a) * 0.32;
        const nz = p.home[1] + Math.sin(a * 1.3) * 0.22;
        const dx = nx - p.x, dz = nz - p.z;
        p.x += dx * Math.min(1, dt * sp * 3);
        p.z += dz * Math.min(1, dt * sp * 3);
        if (Math.abs(dx) + Math.abs(dz) > 0.002) p.obj.rotation.y = Math.atan2(dx, dz);
        p.obj.position.set(p.x, 0, p.z);
        if (p.tail) p.tail.rotation.y = Math.sin(p.t * 9) * 0.5;
      }
      const w = p.warn;
      p.obj.rotation.z = w > 0 ? Math.sin(p.t * 40) * 0.08 * w : 0;
      p.warn = Math.max(0, p.warn - dt * 3);
    }
  }
}

function pickN(arr, n, rnd) {
  const a = arr.slice();
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(rnd() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a.slice(0, n);
}
