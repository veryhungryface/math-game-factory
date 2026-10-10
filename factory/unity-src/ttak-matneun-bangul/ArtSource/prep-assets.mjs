// 아트 단계 PNG → Unity Resources 텍스처 (흰 배경 플러드필 크로마키 + 크롭). 헤드리스 Chrome 캔버스 사용.
//   node factory/unity-src/ttak-matneun-bangul/ArtSource/prep-assets.mjs
import fs from 'fs';
import path from 'path';
import os from 'os';
import puppeteer from 'puppeteer';
const ROOT = path.resolve(path.dirname(new URL(import.meta.url).pathname), '../../../..');
const SRC = path.join(ROOT, 'public/g/ttak-matneun-bangul/assets');
const OUT = path.join(ROOT, 'factory/unity-src/ttak-matneun-bangul/Resources/TtakMatneunBangul');
const exe = process.env.PUPPETEER_EXECUTABLE_PATH || path.join(os.homedir(), '.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const browser = await puppeteer.launch({ executablePath: exe, headless: true });
const page = await browser.newPage();
async function key(file, opts) {
  const b64 = fs.readFileSync(path.join(SRC, file)).toString('base64');
  return page.evaluate(async (b64, opts) => {
    const img = new Image(); img.src = 'data:image/png;base64,' + b64; await img.decode();
    const W = img.width, H = img.height;
    const c = document.createElement('canvas'); c.width = W; c.height = H;
    const g = c.getContext('2d'); g.drawImage(img, 0, 0);
    const d = g.getImageData(0, 0, W, H); const p = d.data;
    // 가장자리에서 연결된 흰 배경만 지운다(내부 흰 거품은 보존).
    const bg = new Uint8Array(W * H); const q = [];
    const near = (i) => { const r = p[i * 4], gg = p[i * 4 + 1], b = p[i * 4 + 2]; return Math.min(r, gg, b) >= opts.th && (Math.max(r, gg, b) - Math.min(r, gg, b)) <= opts.sat; };
    for (let x = 0; x < W; x++) { q.push(x, (H - 1) * W + x); }
    for (let y = 0; y < H; y++) { q.push(y * W, y * W + W - 1); }
    while (q.length) { const i = q.pop(); if (bg[i] || !near(i)) continue; bg[i] = 1; const x = i % W, y = (i / W) | 0;
      if (x > 0) q.push(i - 1); if (x < W - 1) q.push(i + 1); if (y > 0) q.push(i - W); if (y < H - 1) q.push(i + W); }
    // 경계 1~2px 부드럽게: 배경 이웃 수로 알파 감쇠 + 흰 번짐 제거(밝기 기반 언믹스)
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) { const i = y * W + x;
      if (bg[i]) { p[i * 4 + 3] = 0; continue; }
      let n = 0, t = 0; for (let dy = -2; dy <= 2; dy++) for (let dx = -2; dx <= 2; dx++) { const xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= W || yy >= H) continue; t++; if (bg[yy * W + xx]) n++; }
      if (n > 0) { const lum = Math.min(p[i * 4], p[i * 4 + 1], p[i * 4 + 2]) / 255; const a = Math.max(0, Math.min(1, (1 - lum) * 1.6 + (1 - n / t) * 0.6));
        p[i * 4 + 3] = Math.round(255 * Math.min(1, a));
        if (a > 0.01) for (let k = 0; k < 3; k++) p[i * 4 + k] = Math.max(0, Math.min(255, Math.round((p[i * 4 + k] - 255 * (1 - a)) / a))); } }
    g.putImageData(d, 0, 0);
    // 크롭
    let x0 = W, y0 = H, x1 = 0, y1 = 0; for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) if (p[(y * W + x) * 4 + 3] > 8) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
    const out = [];
    if (opts.cells) {
      const [cx, cy] = opts.cells; const cw = W / cx, ch = H / cy;
      const o = document.createElement('canvas'); o.width = opts.cell * cx; o.height = opts.cell * cy; const og = o.getContext('2d');
      for (let j = 0; j < cy; j++) for (let i = 0; i < cx; i++) {
        // 셀 안 알파 bbox 를 정사각 셀에 맞춘다(원본 배치가 고르지 않아 셀 경계는 opts.xs 로 준다)
        const xs = opts.xs[j]; const xa = Math.floor(xs[i] * W), xb = Math.floor(xs[i + 1] * W);
        let a0 = 1e9, b0 = 1e9, a1 = -1, b1 = -1; for (let y = Math.floor(j * ch); y < Math.floor((j + 1) * ch); y++) for (let x = xa; x < xb; x++) if (p[(y * W + x) * 4 + 3] > 20) { if (x < a0) a0 = x; if (x > a1) a1 = x; if (y < b0) b0 = y; if (y > b1) b1 = y; }
        const bw = a1 - a0 + 1, bh = b1 - b0 + 1; const s = (opts.cell - 8) / Math.max(bw, bh);
        const dw = bw * s, dh = bh * s; og.drawImage(c, a0, b0, bw, bh, i * opts.cell + (opts.cell - dw) / 2, j * opts.cell + (opts.cell - dh), dw, dh);
      }
      out.push(o.toDataURL('image/png'));
    } else {
      const pad = 6; x0 = Math.max(0, x0 - pad); y0 = Math.max(0, y0 - pad); x1 = Math.min(W - 1, x1 + pad); y1 = Math.min(H - 1, y1 + pad);
      const w = x1 - x0 + 1, h = y1 - y0 + 1; const s = Math.min(1, opts.maxW / w);
      const o = document.createElement('canvas'); o.width = Math.round(w * s); o.height = Math.round(h * s);
      o.getContext('2d').drawImage(c, x0, y0, w, h, 0, 0, o.width, o.height); out.push(o.toDataURL('image/png'));
    }
    return out;
  }, b64, opts);
}
const save = (name, url) => { fs.writeFileSync(path.join(OUT, name), Buffer.from(url.split(',')[1], 'base64')); console.log('wrote', name); };
save('logo.png', (await key('title_ink.png', { th: 236, sat: 22, maxW: 1024 }))[0]);
save('soap.png', (await key('soap-life.png', { th: 238, sat: 20, cells: [3, 2], cell: 256, xs: [[0, 0.385, 0.68, 1], [0, 0.345, 0.69, 1]] }))[0]);
await browser.close();
