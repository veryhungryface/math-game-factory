import * as THREE from 'three';
import { buildDigger, addOutline } from './world.js';

export const BOUNDS = { x0: -3.45, x1: 3.45, z0: -2.75, z1: 3.05 };

export class Player {
  constructor(scene) {
    this.root = new THREE.Group();
    this.rig = buildDigger();
    this.rig.scale.setScalar(1.08);
    addOutline(this.rig);
    this.root.add(this.rig);
    scene.add(this.root);
    const q = (n) => this.rig.getObjectByName(n);
    this.p = { torso: q('Torso'), head: q('Head'), armL: q('ArmL'), armR: q('ArmR'), legL: q('LegL'), legR: q('LegR'), brush: q('Brush'), chisel: q('Chisel') };
    this.base = {};
    for (const [k, o] of Object.entries(this.p)) if (o) this.base[k] = { pos: o.position.clone(), rot: o.rotation.clone() };
    this.pos = new THREE.Vector3(0, 0, 2.6);
    this.vel = new THREE.Vector3();
    this.yaw = Math.PI;
    this.walk = 0;
    this.walkAmt = 0;
    this.act = 0; // 도구 동작 세기
    this.tool = 'brush';
    this.using = false;
    this.t = 0;
    this.hop = 0;
    this.sad = 0;
    this.cheer = 0;
    this.setTool('brush');
    // 도구 범위 고리
    const ringGeo = new THREE.RingGeometry(0.86, 1, 40);
    ringGeo.rotateX(-Math.PI / 2);
    this.ringMat = new THREE.MeshBasicMaterial({ color: '#fff3c0', transparent: true, opacity: 0.85, depthWrite: false });
    this.ring = new THREE.Mesh(ringGeo, this.ringMat);
    this.ring.renderOrder = 4;
    this.ring.visible = false;
    scene.add(this.ring);
    const dotGeo = new THREE.CircleGeometry(1, 32);
    dotGeo.rotateX(-Math.PI / 2);
    this.dot = new THREE.Mesh(dotGeo, new THREE.MeshBasicMaterial({ color: '#fff3c0', transparent: true, opacity: 0.18, depthWrite: false }));
    this.dot.renderOrder = 4;
    this.ring.add(this.dot);
    this.dot.scale.setScalar(0.86);
  }

  setTool(tool) {
    this.tool = tool;
    if (this.p.brush) this.p.brush.visible = tool === 'brush';
    if (this.p.chisel) this.p.chisel.visible = tool === 'chisel';
  }

  /** move: {x,z} 입력 방향(길이≤1), face: 바라볼 지점(없으면 이동 방향) */
  step(dt, move, speed, faceAt, colliders) {
    this.t += dt;
    const want = new THREE.Vector3(move.x, 0, move.z);
    const len = Math.min(1, want.length());
    if (len > 0.01) want.normalize().multiplyScalar(len * speed);
    this.vel.lerp(want, 1 - Math.exp(-dt * 14));
    this.pos.addScaledVector(this.vel, dt);
    // 소품 충돌(밀어내기)
    for (const c of colliders) {
      const dx = this.pos.x - c.x, dz = this.pos.z - c.z;
      const d = Math.hypot(dx, dz);
      const r = c.r + 0.22;
      if (d < r && d > 1e-4) {
        this.pos.x = c.x + (dx / d) * r;
        this.pos.z = c.z + (dz / d) * r;
      }
    }
    this.pos.x = THREE.MathUtils.clamp(this.pos.x, BOUNDS.x0, BOUNDS.x1);
    this.pos.z = THREE.MathUtils.clamp(this.pos.z, BOUNDS.z0, BOUNDS.z1);
    const sp = Math.hypot(this.vel.x, this.vel.z);
    let targetYaw = this.yaw;
    if (faceAt) {
      const dx = faceAt.x - this.pos.x, dz = faceAt.z - this.pos.z;
      if (dx * dx + dz * dz > 0.01) targetYaw = Math.atan2(dx, dz);
    } else if (sp > 0.2) targetYaw = Math.atan2(this.vel.x, this.vel.z);
    let dy = targetYaw - this.yaw;
    dy = Math.atan2(Math.sin(dy), Math.cos(dy));
    this.yaw += dy * (1 - Math.exp(-dt * 16));
    this.walkAmt += ((sp > 0.15 ? Math.min(1, sp / 2.5) : 0) - this.walkAmt) * (1 - Math.exp(-dt * 10));
    this.walk += dt * (6 + sp * 3.2);
    this.act += ((this.using ? 1 : 0) - this.act) * (1 - Math.exp(-dt * 14));
  }

  animate(groundH) {
    const P = this.p, B = this.base, t = this.t;
    this.root.position.set(this.pos.x, groundH, this.pos.z);
    this.hop = Math.max(0, this.hop - 0.04);
    this.root.position.y += Math.sin(this.hop * Math.PI) * 0.25 * (this.hop > 0 ? 1 : 0);
    this.root.rotation.y = this.yaw;
    const w = this.walkAmt, a = this.act;
    const s = Math.sin(this.walk);
    if (P.legL) P.legL.rotation.x = B.legL.rot.x + s * 0.75 * w;
    if (P.legR) P.legR.rotation.x = B.legR.rot.x - s * 0.75 * w;
    if (P.torso) {
      P.torso.position.y = B.torso.pos.y + Math.abs(Math.cos(this.walk)) * 0.035 * w + Math.sin(t * 2.2) * 0.008;
      P.torso.rotation.x = B.torso.rot.x + 0.28 * a + 0.06 * w - 0.25 * this.sad;
      P.torso.rotation.y = B.torso.rot.y + (this.tool === 'brush' ? Math.sin(t * 15) * 0.12 * a : 0);
    }
    if (P.head) {
      P.head.rotation.x = B.head.rot.x - 0.18 * a + 0.3 * this.sad - 0.25 * this.cheer;
      P.head.rotation.z = B.head.rot.z + Math.sin(t * 1.7) * 0.04;
    }
    if (P.armL) {
      P.armL.rotation.x = B.armL.rot.x - s * 0.55 * w - 0.4 * a - 2.6 * this.cheer;
      P.armL.rotation.z = B.armL.rot.z + 0.1 * a;
    }
    if (P.armR) {
      let rx = B.armR.rot.x + s * 0.55 * w;
      let rz = B.armR.rot.z;
      if (this.tool === 'brush') {
        rx = rx * (1 - a) + (-1.15 + Math.sin(t * 15) * 0.18) * a;
        rz = rz + Math.sin(t * 15 + 1.2) * 0.42 * a;
      } else {
        const hit = Math.max(0, Math.sin(t * 22));
        rx = rx * (1 - a) + (-0.55 - hit * 0.95) * a;
      }
      P.armR.rotation.x = rx - 2.6 * this.cheer;
      P.armR.rotation.z = rz;
    }
    this.cheer = Math.max(0, this.cheer - 0.012);
    this.sad = Math.max(0, this.sad - 0.01);
  }

  showRing(x, z, r, y, danger) {
    this.ring.visible = true;
    this.ring.position.set(x, y + 0.02, z);
    this.ring.scale.setScalar(r);
    this.ringMat.color.set(danger ? '#ff5a4a' : this.tool === 'brush' ? '#fff3c0' : '#9fe6ff');
    this.dot.material.color.copy(this.ringMat.color);
  }
}
