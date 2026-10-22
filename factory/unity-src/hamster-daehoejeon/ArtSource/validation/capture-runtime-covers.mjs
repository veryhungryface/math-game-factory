#!/usr/bin/env node
// 최종 Unity WebGL의 첫 정답 사건을 카탈로그 표지로 직접 캡처한다.
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import puppeteer from "puppeteer";
import { serveStatic } from "../../../../lib/static-server.mjs";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, "../../../../..");
const slug = "hamster-daehoejeon";
const gameDir = path.join(root, "public/g", slug);
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || "", ".cache/puppeteer/chrome");
  if (!fs.existsSync(base)) return undefined;
  const builds = fs.readdirSync(base).sort().reverse();
  for (const build of builds) {
    const executable = path.join(base, build, "chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing");
    if (fs.existsSync(executable)) return executable;
  }
  return undefined;
}

const server = await serveStatic(path.join(root, "public"));
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ["--no-sandbox", "--disable-dev-shm-usage", "--mute-audio", "--hide-scrollbars"],
});
const captures = [];

async function state(page) {
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

async function waitForState(page, predicate, label, timeout = 5000) {
  const started = Date.now();
  while (Date.now() - started < timeout) {
    const current = await state(page);
    if (predicate(current)) return current;
    await sleep(16);
  }
  throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);
}

function answer(current) {
  const match = String(current.currentProblemId).match(/-k(\d+)/);
  if (!match) throw new Error(`answer key missing: ${current.currentProblemId}`);
  const k = Number(match[1]);
  return current.band === 1 ? 20 + k : current.band === 2 ? 20 + 2 * k : 10 + k;
}

async function submitByPointer(page, height, current, value) {
  const browserPoint = (x, unityY) => [x, height - unityY];
  const targetPoint = (selected, extraRadius = 0) => {
    const index = (selected - current.allowedMin) / current.allowedStep;
    const radians = (160 - 140 * index / 59) * Math.PI / 180;
    return browserPoint(
      current.dialCenterX + Math.cos(radians) * (current.dialRadius + extraRadius),
      current.dialCenterY + Math.sin(radians) * (current.dialRadius + extraRadius),
    );
  };
  const from = browserPoint(current.handleX, current.handleY);
  const nextIndex = (Math.round((value - current.allowedMin) / current.allowedStep) + 1) % 60;
  const nudge = targetPoint(current.allowedMin + current.allowedStep * nextIndex, 8);
  const to = targetPoint(value);
  await page.mouse.move(...from);
  await page.mouse.down();
  await page.mouse.move(...nudge, { steps: 2 });
  await page.mouse.move(...to, { steps: 6 });
  await page.mouse.up();
}

async function capture(width, height, filename, scene) {
  const page = await browser.newPage();
  const consoleErrors = [];
  const pageErrors = [];
  page.on("console", (message) => { if (message.type() === "error") consoleErrors.push(message.text()); });
  page.on("pageerror", (error) => pageErrors.push(String(error)));
  await page.setViewport({ width, height, deviceScaleFactor: 1 });
  await page.goto(`${server.url}/g/${slug}/`, { waitUntil: "networkidle2", timeout: 45000 });
  await page.waitForFunction("window.__GAME_TEST__ && window.__GAME_TEST__.ready === true", { timeout: 20000 });
  if (scene === "first-correct") {
    await page.evaluate(() => window.__GAME_TEST__.start());
    const playing = await waitForState(page, (current) => current.phase === "playing" && current.handleX > 0 && current.dialRadius > 0, "play layout");
    await submitByPointer(page, height, playing, answer(playing));
    await waitForState(page, (current) => current.phase === "reveal" && current.lastResult === "correct", "correct reveal");
    await sleep(250);
  } else {
    await sleep(400);
  }
  const canvas = await page.$("#unity-canvas");
  if (!canvas) throw new Error("Unity canvas not found");
  const output = path.join(gameDir, filename);
  await canvas.screenshot({ path: output });
  const current = await state(page);
  captures.push({ filename, width, height, scene, state: current, consoleErrors, pageErrors });
  await page.close();
}

try {
  await capture(1200, 630, "thumb.png", "first-correct");
  await capture(1080, 1080, "square.png", "title");
} finally {
  await browser.close();
  await server.close();
}

const result = {
  generatedAt: new Date().toISOString(),
  method: "final WebGL canvas captures: real-pointer first correct reveal for thumb, live title for square; no generated cover art",
  captures,
};
fs.writeFileSync(path.join(here, "runtime-cover-capture-results.json"), `${JSON.stringify(result, null, 2)}\n`);
console.log(JSON.stringify(result, null, 2));
