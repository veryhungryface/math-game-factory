import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';

// ───────────────── 유적지 3곳: 팔레트·소품만 교체 ─────────────────
export const SITES = [
  {
    id: 'desert', name: '사막 피라미드', who: '모래바다 박물관 관장님',
    quote: '모래에 묻힌 계산 석판을 찾아 주세요. 항아리는 3천 년 된 거예요!',
    ground: '#efbd72', far: '#f5cf8a', sand: '#f7d283', sandDark: '#c98e47', crust: '#e4d8bf', crustDark: '#a5937a',
    floor: '#c98f58', floorLine: '#b07a46', sky: ['#5fb8ef', '#bfe5f7', '#fde7b4'], fog: '#f8ddb0',
    hemi: ['#d6efff', '#d79e5c'], sun: '#fff0d2', backdrop: 'Pyramid', tree: 'Palm', extra: 'Cactus',
    mats: { rock: '#c9a57a', stone_far: '#e8b871' },
  },
  {
    id: 'jungle', name: '정글 신전', who: '밀림 연구소 박사님',
    quote: '덩굴 아래 석판이 숨어 있어요. 두루마리는 살살 다뤄 주세요.',
    ground: '#86ae55', far: '#6f9a48', sand: '#bf8c55', sandDark: '#7f5832', crust: '#9aa680', crustDark: '#66704f',
    floor: '#6e4b2f', floorLine: '#5a3c25', sky: ['#4fbfb2', '#a9e3c8', '#eaf5c6'], fog: '#bfe0b0',
    hemi: ['#e2ffe9', '#5c7a34'], sun: '#fff7dc', backdrop: 'Temple', tree: 'JungleTree', extra: 'JungleTree',
    mats: { rock: '#8f9a7a', stone: '#b8b49a', stone_top: '#cfc9ab' },
  },
  {
    id: 'ice', name: '얼음 동굴', who: '극지 탐사대장님',
    quote: '눈 속에 석판이 얼어붙었어요. 얼음덩이는 끌로만 깨져요!',
    ground: '#dcedf8', far: '#c6def0', sand: '#f7fbff', sandDark: '#a8c9e2', crust: '#a6dcf8', crustDark: '#5fa2cf',
    floor: '#8eaec6', floorLine: '#7798b1', sky: ['#24467e', '#6d9fd6', '#cfe6f6'], fog: '#cfe2f2',
    hemi: ['#eaf6ff', '#7d9bb8'], sun: '#f4fbff', backdrop: 'IceArch', tree: 'IceCrystal', extra: 'Rock',
    mats: { rock: '#9fb6ca', stone: '#b9c3cc', stone_top: '#d2dbe2', wood: '#9a7350' },
  },
];

const PALETTE = {
  stone: '#c9b48f', stone_top: '#d8c7a3', stone_far: '#e3b778', stone_moss: '#8c9b74', wood: '#a8703f', wood_dark: '#7a4e2b',
  metal: '#8d97a3', cloth: '#f4e6c8', cloth_stripe: '#e0723c', rope: '#c9a46b', bark: '#8a5a34', leaf: '#5fae4a', leaf_dark: '#3f8a3a',
  clay: '#d9824a', clay_dark: '#9c4f2a', bone: '#f3ead6', paper: '#f1e2b9', lizard: '#6cc04a', lizard_belly: '#e6e08a', flame: '#ffb437',
  rock: '#b99a76', ice: '#bfe6ff', ice_dark: '#7fbfe8', gold: '#ffc93c', gem: '#e2465a', gem_blue: '#3fa7e0', bronze: '#c47f3a',
  fossil: '#cdb79a', jade: '#4fb38a', skin: '#f2c29b', hair: '#4a2f1f', hat: '#d9b26a', hat_band: '#7a4e2b', shirt: '#2fa39b',
  shorts: '#c9a46b', boot: '#6b4127', satchel: '#b5652f', scarf: '#e2563b', black: '#222222', white: '#ffffff',
};
const EMISSIVE = { flame: 0.9, gold: 0.12, gem: 0.1, ice: 0.08 };

export const INK = new THREE.Color('#2b1a10');

let gradientMap = null;
function toonGradient() {
  if (gradientMap) return gradientMap;
  const data = new Uint8Array([120, 120, 120, 255, 185, 185, 185, 255, 235, 235, 235, 255, 255, 255, 255, 255]);
  gradientMap = new THREE.DataTexture(data, 4, 1, THREE.RGBAFormat);
  gradientMap.minFilter = gradientMap.magFilter = THREE.NearestFilter;
  gradientMap.generateMipmaps = false;
  gradientMap.needsUpdate = true;
  return gradientMap;
}

const MAT = new Map();
export function mat(name) {
  if (MAT.has(name)) return MAT.get(name);
  const m = new THREE.MeshToonMaterial({ color: PALETTE[name] || '#cccccc', gradientMap: toonGradient() });
  m.name = name;
  if (EMISSIVE[name]) {
    m.emissive = new THREE.Color(PALETTE[name]);
    m.emissiveIntensity = EMISSIVE[name];
  }
  if (name === 'leaf' || name === 'leaf_dark') m.side = THREE.DoubleSide;
  MAT.set(name, m);
  return m;
}

let groundTex = null;
export function groundTexture() {
  if (groundTex) return groundTex;
  const c = document.createElement('canvas');
  c.width = c.height = 256;
  const g = c.getContext('2d');
  g.fillStyle = '#ffffff';
  g.fillRect(0, 0, 256, 256);
  for (let i = 0; i < 40; i++) {
    const x = Math.random() * 256, y = Math.random() * 256, r = 18 + Math.random() * 40;
    const gr = g.createRadialGradient(x, y, 0, x, y, r);
    const dark = Math.random() < 0.5;
    gr.addColorStop(0, dark ? 'rgba(150,110,60,0.10)' : 'rgba(255,255,240,0.14)');
    gr.addColorStop(1, 'rgba(0,0,0,0)');
    g.fillStyle = gr;
    for (const ox of [-256, 0, 256]) for (const oy of [-256, 0, 256]) {
      g.save(); g.translate(ox, oy); g.beginPath(); g.arc(x, y, r, 0, Math.PI * 2); g.fill(); g.restore();
    }
  }
  for (let i = 0; i < 1600; i++) {
    g.fillStyle = Math.random() < 0.55 ? 'rgba(120,85,40,0.16)' : 'rgba(255,255,245,0.25)';
    g.fillRect(Math.random() * 256, Math.random() * 256, 1.5 + Math.random() * 2, 1.5 + Math.random() * 2);
  }
  // 바람 물결
  g.strokeStyle = 'rgba(140,100,50,0.09)';
  g.lineWidth = 2;
  for (let y = 8; y < 256; y += 16) {
    g.beginPath();
    for (let x = 0; x <= 256; x += 8) g.lineTo(x, y + Math.sin(x * 0.0245 * 2 + y) * 4);
    g.stroke();
  }
  groundTex = new THREE.CanvasTexture(c);
  groundTex.wrapS = groundTex.wrapT = THREE.RepeatWrapping;
  groundTex.repeat.set(16, 16);
  groundTex.colorSpace = THREE.SRGBColorSpace;
  groundTex.anisotropy = 4;
  return groundTex;
}
export function applySitePalette(site) {
  for (const [k, v] of Object.entries(PALETTE)) if (MAT.has(k)) MAT.get(k).color.set(site.mats[k] || v);
}

// 외곽선(뒤집은 껍데기)
const outlineMat = new THREE.MeshBasicMaterial({ color: INK, side: THREE.BackSide });
outlineMat.onBeforeCompile = (sh) => {
  sh.uniforms.uOut = { value: 0.022 };
  sh.vertexShader = sh.vertexShader
    .replace('#include <common>', '#include <common>\nuniform float uOut;')
    .replace('#include <begin_vertex>', '#include <begin_vertex>\ntransformed += normalize(normal) * uOut;');
};
export function addOutline(root, thickness = 1) {
  const list = [];
  root.traverse((o) => {
    if (o.isMesh && !o.userData.isOutline && !o.userData.noOutline) list.push(o);
  });
  for (const o of list) {
    const m = new THREE.Mesh(o.geometry, outlineMat);
    m.userData.isOutline = true;
    m.raycast = () => {};
    if (thickness !== 1) m.scale.setScalar(1);
    o.add(m);
  }
}

// ───────────────── 모델 키트 ─────────────────
export const KIT = { digger: null, parts: {}, ok: false };

function convert(root) {
  root.traverse((o) => {
    if (!o.isMesh) return;
    const src = Array.isArray(o.material) ? o.material : [o.material];
    const conv = src.map((m) => mat(m && m.name ? m.name.replace(/\.\d+$/, '') : 'stone'));
    o.material = Array.isArray(o.material) ? conv : conv[0];
    o.castShadow = true;
    o.receiveShadow = true;
  });
}

export async function loadKit() {
  const loader = new GLTFLoader();
  try {
    const [dg, kit] = await Promise.all([loader.loadAsync('./assets/models/digger.glb'), loader.loadAsync('./assets/models/kit.glb')]);
    convert(dg.scene);
    convert(kit.scene);
    KIT.digger = dg.scene.getObjectByName('Digger') || dg.scene;
    for (const c of kit.scene.children) KIT.parts[c.name] = c;
    KIT.ok = true;
  } catch (e) {
    KIT.ok = false;
  }
  return KIT;
}

/** 키트에서 복제(없으면 대체 도형) */
export function part(name, baked = false) {
  const src = KIT.parts[name];
  let c;
  if (src) {
    c = src.clone(true);
    c.position.set(0, 0, 0);
    c.rotation.set(0, 0, 0);
    c.scale.set(1, 1, 1);
  } else c = fallbackPart(name);
  return baked ? bake(c, ['Tail']) : c;
}

function fallbackPart(name) {
  const g = new THREE.Group();
  const add = (geo, m, y = 0, x = 0, z = 0) => {
    const me = new THREE.Mesh(geo, mat(m));
    me.position.set(x, y, z);
    me.castShadow = me.receiveShadow = true;
    g.add(me);
    return me;
  };
  switch (name) {
    case 'Tablet': {
      add(new THREE.BoxGeometry(1, 0.12, 0.7), 'stone', 0.06);
      add(new THREE.BoxGeometry(0.94, 0.02, 0.64), 'stone_top', 0.13);
      break;
    }
    case 'Pot': add(new THREE.SphereGeometry(0.17, 10, 8), 'clay', 0.2); add(new THREE.CylinderGeometry(0.07, 0.09, 0.16, 10), 'clay_dark', 0.4); break;
    case 'Bones': add(new THREE.CapsuleGeometry(0.04, 0.3, 4, 8), 'bone', 0.05).rotation.z = Math.PI / 2; add(new THREE.SphereGeometry(0.08, 8, 6), 'bone', 0.08, 0.15); break;
    case 'Scroll': add(new THREE.CylinderGeometry(0.06, 0.06, 0.42, 10), 'paper', 0.07).rotation.z = Math.PI / 2; break;
    case 'Lizard': { add(new THREE.CapsuleGeometry(0.05, 0.22, 4, 8), 'lizard', 0.05).rotation.x = Math.PI / 2; const t = new THREE.Group(); t.name = 'Tail'; t.position.z = -0.14; g.add(t); const tm = new THREE.Mesh(new THREE.ConeGeometry(0.035, 0.22, 6), mat('lizard')); tm.rotation.x = -Math.PI / 2; tm.position.set(0, 0.05, -0.1); t.add(tm); break; }
    case 'Tent': add(new THREE.ConeGeometry(1.1, 1.6, 4), 'cloth', 0.8).rotation.y = Math.PI / 4; break;
    case 'Crate': add(new THREE.BoxGeometry(0.5, 0.5, 0.5), 'wood', 0.25); break;
    case 'Cart': add(new THREE.BoxGeometry(0.5, 0.25, 0.8), 'wood', 0.35); add(new THREE.TorusGeometry(0.14, 0.04, 6, 12), 'metal', 0.15, 0, 0.4).rotation.y = Math.PI / 2; break;
    case 'Torch': { add(new THREE.CylinderGeometry(0.04, 0.05, 1.1, 6), 'wood_dark', 0.55); add(new THREE.CylinderGeometry(0.12, 0.07, 0.1, 8), 'metal', 1.1); const f = add(new THREE.ConeGeometry(0.1, 0.25, 6), 'flame', 1.27); f.name = 'Flame'; break; }
    case 'Palm': case 'JungleTree': add(new THREE.CylinderGeometry(0.09, 0.14, 2.4, 6), 'bark', 1.2); add(new THREE.SphereGeometry(0.8, 7, 5), 'leaf', 2.5).scale.y = 0.45; break;
    case 'Cactus': add(new THREE.CapsuleGeometry(0.16, 0.8, 4, 8), 'leaf_dark', 0.55); break;
    case 'IceCrystal': add(new THREE.ConeGeometry(0.25, 1.0, 5), 'ice', 0.5); break;
    case 'Rock': add(new THREE.DodecahedronGeometry(0.32, 0), 'rock', 0.22); break;
    case 'RockFlat': add(new THREE.DodecahedronGeometry(0.4, 0), 'rock', 0.1).scale.y = 0.4; break;
    case 'RopePost': add(new THREE.CylinderGeometry(0.05, 0.06, 0.55, 6), 'wood', 0.27); break;
    case 'Pyramid': add(new THREE.ConeGeometry(6.5, 6, 4), 'stone_far', 3).rotation.y = Math.PI / 4; break;
    case 'Temple': add(new THREE.CylinderGeometry(2.5, 4.5, 5, 4), 'stone_moss', 2.5).rotation.y = Math.PI / 4; break;
    case 'IceArch': add(new THREE.TorusGeometry(3.2, 0.9, 6, 12, Math.PI), 'ice', 0); break;
    case 'Shard': add(new THREE.TetrahedronGeometry(0.06, 0), 'clay', 0); break;
    case 'Bucket': add(new THREE.CylinderGeometry(0.14, 0.11, 0.26, 10), 'metal', 0.13); break;
    case 'Sign': add(new THREE.CylinderGeometry(0.04, 0.04, 0.9, 6), 'wood_dark', 0.45); add(new THREE.BoxGeometry(0.6, 0.3, 0.05), 'wood', 0.75); break;
    default: {
      // Art_xx
      const i = parseInt(name.slice(4), 10) || 0;
      const ms = ['clay', 'gold', 'fossil', 'bronze', 'gold', 'gold', 'gem_blue', 'bone', 'bronze', 'gold', 'clay', 'gem', 'clay', 'fossil', 'jade'];
      add(i % 3 === 0 ? new THREE.SphereGeometry(0.13, 10, 8) : i % 3 === 1 ? new THREE.OctahedronGeometry(0.14) : new THREE.TorusGeometry(0.1, 0.04, 8, 14), ms[i] || 'gold', 0.15);
    }
  }
  return g;
}

// ───────────────── 정점색 굽기(드로콜 줄이기) ─────────────────
const VC = {};
export function vcMat(double = false) {
  const k = double ? 'd' : 's';
  if (!VC[k]) VC[k] = new THREE.MeshToonMaterial({ vertexColors: true, gradientMap: toonGradient(), side: double ? THREE.DoubleSide : THREE.FrontSide });
  return VC[k];
}
function newAcc() {
  return { s: { p: [], n: [], c: [] }, d: { p: [], n: [], c: [] } };
}
const _v3 = new THREE.Vector3();
const _n3 = new THREE.Matrix3();
function collectMesh(mesh, matrix, acc) {
  const geo = mesh.geometry;
  const mats = Array.isArray(mesh.material) ? mesh.material : [mesh.material];
  const g = geo.index ? geo.toNonIndexed() : geo;
  if (!g.attributes.normal) g.computeVertexNormals();
  const P = g.attributes.position, N = g.attributes.normal;
  const groups = Array.isArray(mesh.material) && geo.groups.length ? geo.groups : [{ start: 0, count: P.count, materialIndex: 0 }];
  _n3.getNormalMatrix(matrix);
  const VCA = mats[0] && mats[0].vertexColors ? g.attributes.color : null;
  for (const gr of groups) {
    const m = mats[gr.materialIndex] || mats[0];
    const col = (m && m.color) || new THREE.Color(1, 1, 1);
    const em = m && m.emissive ? m.emissiveIntensity || 0 : 0;
    const r = Math.min(1, col.r * (1 + em)), gg = Math.min(1, col.g * (1 + em)), b = Math.min(1, col.b * (1 + em));
    const A = m && m.side === THREE.DoubleSide ? acc.d : acc.s;
    const end = Math.min(P.count, gr.start + gr.count);
    for (let i = gr.start; i < end; i++) {
      _v3.fromBufferAttribute(P, i).applyMatrix4(matrix);
      A.p.push(_v3.x, _v3.y, _v3.z);
      _v3.fromBufferAttribute(N, i).applyMatrix3(_n3).normalize();
      A.n.push(_v3.x, _v3.y, _v3.z);
      if (VCA) A.c.push(VCA.getX(i), VCA.getY(i), VCA.getZ(i));
      else A.c.push(r, gg, b);
    }
  }
}
function accMeshes(acc) {
  const out = [];
  for (const k of ['s', 'd']) {
    const A = acc[k];
    if (!A.p.length) continue;
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.Float32BufferAttribute(A.p, 3));
    g.setAttribute('normal', new THREE.Float32BufferAttribute(A.n, 3));
    g.setAttribute('color', new THREE.Float32BufferAttribute(A.c, 3));
    g.computeBoundingSphere();
    const me = new THREE.Mesh(g, vcMat(k === 'd'));
    me.castShadow = true;
    me.receiveShadow = true;
    out.push(me);
  }
  return out;
}

/** 오브젝트 전체를 (최대 2개) 정점색 메시로 구운 그룹을 돌려준다. keep: 따로 남길 자식 이름 */
export function bake(root, keep = []) {
  root.updateMatrixWorld(true);
  const inv = new THREE.Matrix4().copy(root.matrixWorld).invert();
  const acc = newAcc();
  const kept = [];
  for (const name of keep) {
    const k = root.getObjectByName(name);
    if (k) kept.push(k);
  }
  const under = (o) => kept.some((k) => { for (let x = o; x; x = x.parent) if (x === k) return true; return false; });
  const m = new THREE.Matrix4();
  root.traverse((o) => {
    if (o.isMesh && !o.userData.isOutline && !under(o)) collectMesh(o, m.multiplyMatrices(inv, o.matrixWorld), acc);
  });
  const g = new THREE.Group();
  g.name = root.name;
  for (const me of accMeshes(acc)) g.add(me);
  for (const k of kept) {
    // 남길 자식은 세계 변환을 유지한 채 새 그룹으로 옮긴다
    const wm = new THREE.Matrix4().multiplyMatrices(inv, k.matrixWorld);
    const bk = bake(k);
    bk.name = k.name;
    wm.decompose(bk.position, bk.quaternion, bk.scale);
    g.add(bk);
  }
  return g;
}

/** 리그: 관절마다 그 관절에 직접 딸린 메시만 하나로 굽는다 */
export function bakeRig(root, joints) {
  root.updateMatrixWorld(true);
  const J = joints.map((n) => root.getObjectByName(n)).filter(Boolean);
  const owner = (o) => {
    for (let x = o.parent; x; x = x.parent) if (J.includes(x)) return x;
    return root;
  };
  const buckets = new Map();
  const meshes = [];
  root.traverse((o) => {
    if (o.isMesh && !o.userData.isOutline) meshes.push(o);
  });
  for (const o of meshes) {
    const j = J.includes(o) ? o : owner(o);
    if (!buckets.has(j)) buckets.set(j, []);
    buckets.get(j).push(o);
  }
  const m = new THREE.Matrix4();
  for (const [j, list] of buckets) {
    const inv = new THREE.Matrix4().copy(j.matrixWorld).invert();
    const acc = newAcc();
    for (const o of list) collectMesh(o, m.multiplyMatrices(inv, o.matrixWorld), acc);
    for (const o of list) {
      if (o === j) continue;
      if (o.children.length) {
        o.geometry = new THREE.BufferGeometry();
        o.geometry.setAttribute('position', new THREE.Float32BufferAttribute([], 3));
      } else o.parent.remove(o);
    }
    if (j.isMesh) {
      // 관절이 메시 자신이면 빈 그룹으로 바꿔 끼운다
      j.geometry = new THREE.BufferGeometry();
      j.visible = true;
      j.material = vcMat();
      j.geometry.setAttribute('position', new THREE.Float32BufferAttribute([], 3));
    }
    for (const me of accMeshes(acc)) j.add(me);
  }
  return root;
}

/** 대원 리그(키트 또는 대체) */
export function buildDigger() {
  let root;
  if (KIT.digger) {
    root = KIT.digger.clone(true);
    root.position.set(0, 0, 0);
    root.rotation.set(0, 0, 0);
  } else {
    root = new THREE.Group();
    root.name = 'Digger';
    const m = (geo, name, parent, pos) => {
      const me = new THREE.Mesh(geo, mat(name));
      me.castShadow = true;
      me.position.copy(pos);
      parent.add(me);
      return me;
    };
    const V = (x, y, z) => new THREE.Vector3(x, y, z);
    const torso = new THREE.Group(); torso.name = 'Torso'; torso.position.set(0, 0.36, 0); root.add(torso);
    m(new THREE.CapsuleGeometry(0.17, 0.16, 4, 10), 'shirt', torso, V(0, 0.16, 0));
    const head = new THREE.Group(); head.name = 'Head'; head.position.set(0, 0.36, 0); torso.add(head);
    m(new THREE.SphereGeometry(0.2, 14, 10), 'skin', head, V(0, 0.18, 0));
    m(new THREE.CylinderGeometry(0.3, 0.3, 0.04, 16), 'hat', head, V(0, 0.3, 0));
    m(new THREE.SphereGeometry(0.17, 12, 8, 0, Math.PI * 2, 0, Math.PI / 2), 'hat', head, V(0, 0.3, 0));
    m(new THREE.SphereGeometry(0.025, 6, 4), 'black', head, V(-0.07, 0.2, 0.18));
    m(new THREE.SphereGeometry(0.025, 6, 4), 'black', head, V(0.07, 0.2, 0.18));
    for (const [n, x] of [['ArmL', 0.22], ['ArmR', -0.22]]) {
      const a = new THREE.Group(); a.name = n; a.position.set(x, 0.3, 0); torso.add(a);
      m(new THREE.CapsuleGeometry(0.05, 0.16, 4, 8), 'skin', a, V(0, -0.12, 0));
      if (n === 'ArmR') {
        const b = new THREE.Group(); b.name = 'Brush'; b.position.set(0, -0.24, 0.02); a.add(b);
        m(new THREE.CylinderGeometry(0.02, 0.02, 0.2, 6), 'wood', b, V(0, -0.06, 0.04));
        m(new THREE.BoxGeometry(0.12, 0.06, 0.05), 'hat', b, V(0, -0.16, 0.06));
        const c = new THREE.Group(); c.name = 'Chisel'; c.position.set(0, -0.24, 0.02); a.add(c);
        m(new THREE.BoxGeometry(0.03, 0.22, 0.03), 'metal', c, V(0, -0.08, 0.05));
      }
    }
    for (const [n, x] of [['LegL', 0.09], ['LegR', -0.09]]) {
      const l = new THREE.Group(); l.name = n; l.position.set(x, 0.36, 0); root.add(l);
      m(new THREE.CapsuleGeometry(0.06, 0.18, 4, 8), 'shorts', l, V(0, -0.15, 0));
      m(new THREE.BoxGeometry(0.11, 0.08, 0.16), 'boot', l, V(0, -0.32, 0.02));
    }
  }
  bakeRig(root, ['Torso', 'Head', 'ArmL', 'ArmR', 'LegL', 'LegR', 'Brush', 'Chisel']);
  root.traverse((o) => {
    if (o.isMesh) {
      o.castShadow = true;
      o.receiveShadow = false;
    }
  });
  return root;
}

// ───────────────── 렌더러·장면 ─────────────────
export class World {
  constructor(canvas) {
    this.canvas = canvas;
    const r = (this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true, preserveDrawingBuffer: true, powerPreference: 'high-performance' }));
    r.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    r.shadowMap.enabled = true;
    r.shadowMap.type = THREE.PCFSoftShadowMap;
    r.outputColorSpace = THREE.SRGBColorSpace;
    this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(34, 1, 0.1, 120);
    this.hemi = new THREE.HemisphereLight('#d6efff', '#d79e5c', 1.05);
    this.scene.add(this.hemi);
    const sun = (this.sun = new THREE.DirectionalLight('#fff0d2', 2.7));
    sun.position.set(4.5, 10, 5.5);
    sun.castShadow = true;
    sun.shadow.mapSize.set(1536, 1536);
    const sc = sun.shadow.camera;
    sc.left = -8; sc.right = 8; sc.top = 8; sc.bottom = -8; sc.near = 1; sc.far = 30;
    sun.shadow.bias = -0.0006;
    sun.shadow.normalBias = 0.02;
    this.scene.add(sun);
    this.scene.add(sun.target);
    this.envGroup = new THREE.Group();
    this.scene.add(this.envGroup);
    this.dyn = new THREE.Group(); // 횃불 불꽃 등 움직이는 장식
    this.scene.add(this.dyn);
    this.flames = [];
    this.skyCanvas = document.createElement('canvas');
    this.skyCanvas.width = 4;
    this.skyCanvas.height = 256;
    this.skyTex = new THREE.CanvasTexture(this.skyCanvas);
    this.skyTex.colorSpace = THREE.SRGBColorSpace;
    this.scene.background = this.skyTex;
    this.scene.fog = new THREE.Fog('#f8ddb0', 22, 60);
    this.viewRect = { x: 0, y: 0, w: 1, h: 1 };
    this.camMode = 'play';
    this.camT = 0;
    this._v = new THREE.Vector3();
    this.shake = 0;
  }

  resize(w, h) {
    this.W = w;
    this.H = h;
    this.renderer.setSize(w, h, false);
    this.camera.aspect = w / h;
    this.camera.updateProjectionMatrix();
  }

  setSky(site) {
    const g = this.skyCanvas.getContext('2d');
    const gr = g.createLinearGradient(0, 0, 0, 256);
    gr.addColorStop(0, site.sky[0]);
    gr.addColorStop(0.55, site.sky[1]);
    gr.addColorStop(1, site.sky[2]);
    g.fillStyle = gr;
    g.fillRect(0, 0, 4, 256);
    this.skyTex.needsUpdate = true;
    this.scene.fog.color.set(site.fog);
    this.hemi.color.set(site.hemi[0]);
    this.hemi.groundColor.set(site.hemi[1]);
    this.sun.color.set(site.sun);
  }

  /** 정적 배경을 재질별로 합쳐 드로콜을 줄인다. */
  buildEnvironment(site, groundMat) {
    for (const c of [...this.envGroup.children]) {
      this.envGroup.remove(c);
      c.geometry?.dispose?.();
    }
    for (const c of [...this.dyn.children]) this.dyn.remove(c);
    this.flames.length = 0;
    applySitePalette(site);
    this.setSky(site);

    // 넓은 바닥 + 멀리 둔덕
    const gGeo = new THREE.PlaneGeometry(90, 90, 60, 60);
    gGeo.rotateX(-Math.PI / 2);
    const pos = gGeo.attributes.position;
    for (let i = 0; i < pos.count; i++) {
      const x = pos.getX(i);
      const z = pos.getZ(i);
      const d = Math.hypot(x, z * 1.3);
      const k = THREE.MathUtils.smoothstep(d, 9, 22);
      pos.setY(i, k * (Math.sin(x * 0.35) * 1.1 + Math.cos(z * 0.28 + x * 0.1) * 1.3 + 1.2) - 0.02);
    }
    gGeo.computeVertexNormals();
    const ground = new THREE.Mesh(gGeo, groundMat);
    ground.receiveShadow = true;
    this.envGroup.add(ground);

    const pieces = [];
    const place = (name, x, z, ry = 0, s = 1) => {
      const o = part(name);
      o.position.set(x, 0, z);
      o.rotation.y = ry;
      o.scale.setScalar(s);
      pieces.push(o);
      return o;
    };
    // 구획 로프 말뚝
    const px = 2.95;
    const pz = 2.3;
    const posts = [[-px, -pz], [0, -pz], [px, -pz], [px, 0], [px, pz], [0, pz], [-px, pz], [-px, 0]];
    for (const [x, z] of posts) place('RopePost', x, z, Math.random() * 3);
    // 배경 소품
    place(site.backdrop, -4, -19, 0.3, 1.5);
    place(site.backdrop, 10, -26, -0.2, 1.1);
    place('Tent', -5.2, -4.2, 0.5, 1.15);
    place('Crate', 4.3, -3.4, 0.2);
    place('Crate', 4.85, -3.0, -0.3, 0.9);
    place('Crate', 4.5, -3.2, 0.6, 0.8).position.y = 0.48;
    place('Cart', 4.7, 1.9, -0.9);
    place('Bucket', -4.0, 2.9, 0.4);
    place('Sign', -4.2, -2.1, 0.35);
    place(site.tree, -6.4, -1.2, 0.2, 1.1);
    place(site.tree, 6.8, -4.8, 1.2, 1.25);
    place(site.tree, -7.2, 3.6, 2.2, 1.0);
    place(site.extra, 6.1, 4.2, 0.4, 1.0);
    place(site.extra, -5.6, 5.8, 1.4, 0.9);
    place('Rock', 5.6, -0.6, 0.6, 1.0);
    place('RockFlat', -4.6, 1.0, 1.2, 1.0);
    place('Rock', -3.6, -5.6, 2.1, 1.3);
    place('RockFlat', 3.0, 5.6, 0.2, 1.2);
    place('Rock', 8.5, 1.5, 0.2, 1.6);
    place('Rock', -9, -2, 1.1, 1.8);
    for (const [x, z] of [[-3.7, -3.1], [3.75, 3.2]]) {
      const t = place('Torch', x, z, 0);
      // 불꽃은 움직이므로 따로 둔다
      const f = t.getObjectByName('Flame');
      if (f) {
        t.updateMatrixWorld(true);
        const wp = new THREE.Vector3();
        f.getWorldPosition(wp);
        f.parent.remove(f);
        f.position.copy(wp);
        this.dyn.add(f);
        this.flames.push(f);
      }
    }
    // 로프(얇은 원기둥)
    const ropeGeos = [];
    for (let i = 0; i < posts.length; i++) {
      const [x1, z1] = posts[i];
      const [x2, z2] = posts[(i + 1) % posts.length];
      const len = Math.hypot(x2 - x1, z2 - z1);
      const cg = new THREE.CylinderGeometry(0.018, 0.018, len, 5);
      cg.rotateZ(Math.PI / 2);
      cg.rotateY(-Math.atan2(z2 - z1, x2 - x1));
      cg.translate((x1 + x2) / 2, 0.44, (z1 + z2) / 2);
      ropeGeos.push(cg);
    }
    const rope = new THREE.Mesh(mergeGeometries(ropeGeos), mat('rope'));
    rope.castShadow = true;
    this.envGroup.add(rope);

    // 정점색으로 구워 두 덩어리(단면/양면)로 합친다 → 드로콜 2
    const acc = newAcc();
    for (const p of pieces) {
      p.updateMatrixWorld(true);
      p.traverse((o) => {
        if (o.isMesh && !o.userData.isOutline) collectMesh(o, o.matrixWorld, acc);
      });
    }
    for (const me of accMeshes(acc)) {
      me.castShadow = true;
      me.receiveShadow = true;
      this.envGroup.add(me);
    }
  }

  /** 플레이 구획이 viewRect(px) 안에 들어오게 카메라 거리를 맞춘다. */
  fitCamera(rect, bounds, pitchDeg, yawDeg = 0) {
    const cam = this.camera;
    cam.clearViewOffset();
    const W = this.W;
    const H = this.H;
    const pitch = THREE.MathUtils.degToRad(pitchDeg);
    const yaw = THREE.MathUtils.degToRad(yawDeg);
    const dir = new THREE.Vector3(Math.sin(yaw) * Math.cos(pitch), Math.sin(pitch), Math.cos(yaw) * Math.cos(pitch));
    const tgt = new THREE.Vector3((bounds.x0 + bounds.x1) / 2, 0, (bounds.z0 + bounds.z1) / 2);
    const corners = [];
    for (const x of [bounds.x0, bounds.x1]) for (const z of [bounds.z0, bounds.z1]) for (const y of [0, bounds.y || 0.4]) corners.push(new THREE.Vector3(x, y, z));
    const v = new THREE.Vector3();
    const measure = (d) => {
      cam.position.copy(tgt).addScaledVector(dir, d);
      cam.lookAt(tgt);
      cam.updateMatrixWorld(true);
      let x0 = 1e9, x1 = -1e9, y0 = 1e9, y1 = -1e9;
      for (const c of corners) {
        v.copy(c).project(cam);
        const sx = (v.x * 0.5 + 0.5) * W;
        const sy = (-v.y * 0.5 + 0.5) * H;
        x0 = Math.min(x0, sx); x1 = Math.max(x1, sx); y0 = Math.min(y0, sy); y1 = Math.max(y1, sy);
      }
      return { x0, x1, y0, y1 };
    };
    let lo = 2;
    let hi = 80;
    for (let i = 0; i < 28; i++) {
      const mid = (lo + hi) / 2;
      const b = measure(mid);
      if (b.x1 - b.x0 <= rect.w && b.y1 - b.y0 <= rect.h) hi = mid;
      else lo = mid;
    }
    const b = measure(hi);
    const offX = (b.x0 + b.x1) / 2 - (rect.x + rect.w / 2);
    const offY = (b.y0 + b.y1) / 2 - (rect.y + rect.h / 2);
    cam.setViewOffset(W, H, offX, offY, W, H);
    this.camBase = { pos: cam.position.clone(), tgt, dir, dist: hi };
    this.scene.fog.near = hi + 4;
    this.scene.fog.far = hi + 46;
  }

  update(dt, t) {
    for (let i = 0; i < this.flames.length; i++) {
      const f = this.flames[i];
      const s = 1 + Math.sin(t * 17 + i * 2) * 0.12 + Math.sin(t * 29 + i) * 0.06;
      f.scale.set(1 / s, s, 1 / s);
    }
    if (this.shake > 0) {
      this.shake = Math.max(0, this.shake - dt * 3);
    }
  }

  render() {
    const cam = this.camera;
    if (this.shake > 0 && this.camBase) {
      const k = this.shake * 0.06;
      cam.position.set(this.camBase.pos.x + (Math.random() - 0.5) * k, this.camBase.pos.y + (Math.random() - 0.5) * k, this.camBase.pos.z);
    } else if (this.camBase && this.camMode === 'play') cam.position.copy(this.camBase.pos);
    this.renderer.render(this.scene, cam);
  }
}

// ───────────────── 입자(모래 먼지·돌가루·반짝이) ─────────────────
export class Particles {
  constructor(scene, max = 520) {
    this.max = max;
    this.n = 0;
    const g = new THREE.BufferGeometry();
    this.pos = new Float32Array(max * 3);
    this.col = new Float32Array(max * 3);
    this.siz = new Float32Array(max);
    this.alp = new Float32Array(max);
    this.vel = new Float32Array(max * 3);
    this.life = new Float32Array(max);
    this.age = new Float32Array(max);
    this.grav = new Float32Array(max);
    g.setAttribute('position', new THREE.BufferAttribute(this.pos, 3).setUsage(THREE.DynamicDrawUsage));
    g.setAttribute('color', new THREE.BufferAttribute(this.col, 3).setUsage(THREE.DynamicDrawUsage));
    g.setAttribute('size', new THREE.BufferAttribute(this.siz, 1).setUsage(THREE.DynamicDrawUsage));
    g.setAttribute('alpha', new THREE.BufferAttribute(this.alp, 1).setUsage(THREE.DynamicDrawUsage));
    this.geo = g;
    const m = new THREE.ShaderMaterial({
      uniforms: { uScale: { value: 400 } },
      vertexShader: `attribute float size; attribute float alpha; attribute vec3 color; varying vec3 vC; varying float vA; uniform float uScale;
        void main(){ vC=color; vA=alpha; vec4 mv=modelViewMatrix*vec4(position,1.0); gl_Position=projectionMatrix*mv; gl_PointSize=size*uScale/(-mv.z); }`,
      fragmentShader: `varying vec3 vC; varying float vA; void main(){ vec2 p=gl_PointCoord-0.5; float d=length(p); if(d>0.5) discard;
        float a=vA*smoothstep(0.5,0.32,d); gl_FragColor=vec4(vC*(1.0-0.25*d),a); }`,
      transparent: true,
      depthWrite: false,
    });
    this.mat = m;
    this.points = new THREE.Points(g, m);
    this.points.frustumCulled = false;
    this.points.renderOrder = 5;
    scene.add(this.points);
    this.c = new THREE.Color();
  }
  emit(x, y, z, vx, vy, vz, life, size, color, grav = 1) {
    let i = this.n < this.max ? this.n++ : Math.floor(Math.random() * this.max);
    this.pos[i * 3] = x; this.pos[i * 3 + 1] = y; this.pos[i * 3 + 2] = z;
    this.vel[i * 3] = vx; this.vel[i * 3 + 1] = vy; this.vel[i * 3 + 2] = vz;
    this.life[i] = life; this.age[i] = 0; this.siz[i] = size; this.grav[i] = grav;
    this.c.set(color);
    this.col[i * 3] = this.c.r; this.col[i * 3 + 1] = this.c.g; this.col[i * 3 + 2] = this.c.b;
    this.alp[i] = 1;
  }
  update(dt) {
    let n = this.n;
    for (let i = 0; i < n; i++) {
      this.age[i] += dt;
      if (this.age[i] >= this.life[i]) {
        // 마지막 것과 바꿔 끼워 배열이 늘지 않게
        n--;
        if (i !== n) {
          for (let k = 0; k < 3; k++) {
            this.pos[i * 3 + k] = this.pos[n * 3 + k];
            this.vel[i * 3 + k] = this.vel[n * 3 + k];
            this.col[i * 3 + k] = this.col[n * 3 + k];
          }
          this.life[i] = this.life[n]; this.age[i] = this.age[n]; this.siz[i] = this.siz[n]; this.grav[i] = this.grav[n]; this.alp[i] = this.alp[n];
        }
        i--;
        continue;
      }
      const drag = Math.exp(-dt * 2.2);
      this.vel[i * 3] *= drag; this.vel[i * 3 + 2] *= drag;
      this.vel[i * 3 + 1] -= 6.5 * this.grav[i] * dt;
      this.pos[i * 3] += this.vel[i * 3] * dt;
      this.pos[i * 3 + 1] = Math.max(0.03, this.pos[i * 3 + 1] + this.vel[i * 3 + 1] * dt);
      this.pos[i * 3 + 2] += this.vel[i * 3 + 2] * dt;
      const k = this.age[i] / this.life[i];
      this.alp[i] = k < 0.15 ? k / 0.15 : 1 - (k - 0.15) / 0.85;
    }
    this.n = n;
    this.geo.setDrawRange(0, n);
    for (const k of ['position', 'color', 'size', 'alpha']) this.geo.attributes[k].needsUpdate = true;
  }
}
