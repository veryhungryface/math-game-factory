// 머신 공용 자원 잠금 (2026-10-03 3안 병렬 생산).
//
// 기획안 3개를 동시에 만들면서 **측정이 흔들리면 안 되는 구간**만 한 번에 하나씩 돌린다:
// QA(fps·15초 저하율을 실 GPU 로 잰다)와 첫 플레이 캡처. 옆 레인이 Unity 빌드나 QA 를 돌리는 중에
// 재면 멀쩡한 게임이 fps 미달로 떨어진다. run.sh 의 bash 잠금(acquire_lock)과 같은 디렉터리 규약이다:
//   factory/state/<name>.lock.d/pid   — 잡은 프로세스 pid. 그 pid 가 죽었으면 회수한다.
import fs from 'node:fs';
import path from 'node:path';
import { P } from './paths.mjs';

const alive = (pid) => { try { process.kill(pid, 0); return true; } catch { return false; } };
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

/** 잠금을 잡을 때까지 기다린다. 프로세스가 끝나면(정상·예외 모두) 자동으로 푼다. */
export async function acquireMachineLock(name, { maxWaitMs = 40 * 60e3, label = name } = {}) {
  if (process.env.MGF_NO_MACHINE_LOCK === '1') return () => {};
  const dir = path.join(P.root, 'factory/state', `${name}.lock.d`);
  const t0 = Date.now();
  let told = false;
  for (;;) {
    try {
      fs.mkdirSync(dir);
      fs.writeFileSync(path.join(dir, 'pid'), String(process.pid));
      break;
    } catch {
      let pid = 0;
      try { pid = Number(fs.readFileSync(path.join(dir, 'pid'), 'utf8')) || 0; } catch {}
      if (pid && !alive(pid)) { fs.rmSync(dir, { recursive: true, force: true }); continue; }
      if (Date.now() - t0 > maxWaitMs) { console.error(`[lock] ${label} 대기 ${Math.round(maxWaitMs / 60e3)}분 초과 — 잠금 없이 진행`); return () => {}; }
      if (!told) { console.error(`[lock] ${label}: 다른 레인(pid ${pid || '?'})이 쓰는 중 — 대기`); told = true; }
      await sleep(3000);
    }
  }
  const release = () => {
    try { if (fs.readFileSync(path.join(dir, 'pid'), 'utf8') === String(process.pid)) fs.rmSync(dir, { recursive: true, force: true }); } catch {}
  };
  process.on('exit', release);
  for (const sig of ['SIGINT', 'SIGTERM']) process.on(sig, () => { release(); process.exit(130); });
  return release;
}
