import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../../lib/static-server.mjs';

export const HERE = path.dirname(fileURLToPath(import.meta.url));
export const ROOT = path.resolve(HERE, '../../../../../..');
export const PUBLIC = path.join(ROOT, 'public');
export const GAME = path.join(PUBLIC, 'g/hyeopgok-sasu');
export const PACK_DIR = path.join(GAME, 'packs');
export const VIEWPORTS = Object.freeze([
  { id: '390', width: 390, height: 844, mobile: true, deviceScaleFactor: 2, minimumGlyphHeight: 14 },
  { id: '1280', width: 1280, height: 800, mobile: false, deviceScaleFactor: 1, minimumGlyphHeight: 20 },
]);

export const sleep = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
export const sha256 = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');

export function resolveChrome() {
  const explicit = process.env.PUPPETEER_EXECUTABLE_PATH;
  if (explicit && fs.existsSync(explicit)) return explicit;
  const cache = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (fs.existsSync(cache)) {
    const builds = fs.readdirSync(cache).sort((a, b) => b.localeCompare(a, undefined, { numeric: true }));
    for (const build of builds) {
      const app = path.join(cache, build, 'chrome-mac-arm64', 'Google Chrome for Testing.app', 'Contents', 'MacOS', 'Google Chrome for Testing');
      if (fs.existsSync(app) && fs.statSync(app).isFile()) return app;
    }
  }
  const bundled = puppeteer.executablePath();
  if (bundled && fs.existsSync(bundled)) return bundled;
  throw new Error('Chrome executable not found; set PUPPETEER_EXECUTABLE_PATH');
}

export function readPackIndex() {
  const file = path.join(PACK_DIR, 'index.json');
  const value = JSON.parse(fs.readFileSync(file, 'utf8'));
  if (!Array.isArray(value.packs)) throw new Error('packs/index.json has no packs array');
  return value;
}

export function packFiles(index = readPackIndex()) {
  return index.packs.map(entry => ({
    id: String(entry.pack_id),
    file: path.join(PACK_DIR, entry.file || `${entry.pack_id}.json`),
    entry,
  }));
}

export function packHashes(index = readPackIndex()) {
  const files = [{ id: 'index', file: path.join(PACK_DIR, 'index.json') }, ...packFiles(index)];
  return Object.fromEntries(files.map(({ id, file }) => [id, sha256(file)]));
}

export function readPacks(index = readPackIndex()) {
  return packFiles(index).map(record => ({ ...record, pack: JSON.parse(fs.readFileSync(record.file, 'utf8')) }));
}

const INT64_MIN = -(1n << 63n);
const INT64_MAX = (1n << 63n) - 1n;

function nonEmptyRuntimeString(value) {
  // Mirrors string.IsNullOrEmpty: whitespace-only strings are intentionally valid.
  return typeof value === 'string' && value.length > 0;
}

function parseInt64(value, allowOuterWhitespaceAndPlus = false) {
  if (typeof value !== 'string') return null;
  const token = allowOuterWhitespaceAndPlus ? value.trim() : value;
  const pattern = allowOuterWhitespaceAndPlus ? /^[+-]?\d+$/u : /^-?\d+$/u;
  if (!pattern.test(token)) return null;
  try {
    const parsed = BigInt(token);
    return parsed >= INT64_MIN && parsed <= INT64_MAX ? parsed : null;
  } catch {
    return null;
  }
}

function gcdBigInt(a, b) {
  a = a < 0n ? -a : a;
  b = b < 0n ? -b : b;
  while (b !== 0n) [a, b] = [b, a % b];
  return a > 0n ? a : 1n;
}

/** BigInt equivalent of PackItem.TryChoiceValue (without float conversion). */
export function parseChoiceValue(text) {
  if (!nonEmptyRuntimeString(text)) return null;
  const source = text.trim();
  let numerator;
  let denominator = 1n;
  let kind = '';
  let unit = '';

  if (source.startsWith('{frac:') && source.endsWith('}')) {
    const parts = source.slice(6, -1).split('/');
    if (parts.length !== 2) return null;
    numerator = parseInt64(parts[0], true);
    denominator = parseInt64(parts[1], true);
    if (numerator === null || denominator === null || denominator === 0n) return null;
    kind = 'num';
  } else {
    const ratio = /^(-?\d+):(-?\d+)$/u.exec(source);
    if (ratio) {
      numerator = parseInt64(ratio[1]);
      denominator = parseInt64(ratio[2]);
      if (numerator === null || denominator === null || denominator === 0n) return null;
      kind = 'ratio';
    } else {
      const number = /^(-?\d+)(?:\s*(°|cm²|cm³|cm|m))?$/u.exec(source);
      if (!number) return null;
      numerator = parseInt64(number[1]);
      if (numerator === null) return null;
      kind = 'num';
      unit = number[2] ?? '';
    }
  }

  if (denominator < 0n) {
    numerator = -numerator;
    denominator = -denominator;
  }
  const divisor = gcdBigInt(numerator, denominator);
  return { numerator: numerator / divisor, denominator: denominator / divisor, kind, unit };
}

/** Mirrors PackItem.EquivalentChoice, including exact fallback for non-numeric text. */
export function equivalentChoice(a, b) {
  const left = parseChoiceValue(a);
  const right = parseChoiceValue(b);
  if (!left || !right) return a === b;
  return left.kind === right.kind && left.unit === right.unit &&
    left.numerator === right.numerator && left.denominator === right.denominator;
}

function runtimeAnswerType(item) {
  // HyeopgokPackJson.Parse injects answer_type from the JSON answer value before
  // JsonUtility creates PackItem; raw validation JSON therefore has no such field.
  if (typeof item?.answer === 'string') return 'choice';
  if (typeof item?.answer === 'number') return 'amount';
  if (item?.answer && typeof item.answer === 'object' && !Array.isArray(item.answer)) return 'fraction_parts';
  return item?.answer_type;
}

/** Stateful mirror of HyeopgokGame.EligibleChoice. */
export function eligibleChoice(item, schemaVersion = 0, ids = new Set()) {
  if (!item || typeof item !== 'object') return false;
  const mode = item.answer_mode == null || item.answer_mode === '' ? 'choice' : item.answer_mode;
  if (mode !== 'choice' || !nonEmptyRuntimeString(item.id) || ids.has(item.id)) return false;

  // EligibleChoice calls ids.Add before checking the remaining fields. Preserve
  // that side effect so a malformed first occurrence still rejects later IDs.
  ids.add(item.id);
  if (!nonEmptyRuntimeString(item.prompt) || !nonEmptyRuntimeString(item.explain)) return false;
  if (runtimeAnswerType(item) !== 'choice' || !nonEmptyRuntimeString(item.answer) ||
      !Array.isArray(item.choices) || item.choices.length !== 4) return false;
  if (schemaVersion >= 3 && (!Array.isArray(item.distractor_tags) || item.distractor_tags.length !== 3)) return false;

  let exact = 0;
  let equivalent = 0;
  for (let index = 0; index < item.choices.length; index += 1) {
    const choice = item.choices[index];
    if (!nonEmptyRuntimeString(choice)) return false;
    if (choice === item.answer) exact += 1;
    if (equivalentChoice(choice, item.answer)) equivalent += 1;
    for (let prior = 0; prior < index; prior += 1) {
      if (equivalentChoice(choice, item.choices[prior])) return false;
    }
  }
  return exact === 1 && equivalent === 1;
}

export function eligibleChoiceItems(pack) {
  if (!pack || typeof pack !== 'object' || !Array.isArray(pack.items) || !nonEmptyRuntimeString(pack.title)) return [];
  // JsonUtility's missing int field defaults to zero, while an explicitly
  // malformed/null schema_version is rejected by HyeopgokPackJson.Parse.
  const schemaVersion = Object.hasOwn(pack, 'schema_version') ? pack.schema_version : 0;
  if (!Number.isInteger(schemaVersion) || schemaVersion < 0 || schemaVersion > 3) return [];
  const ids = new Set();
  return pack.items.filter(item => eligibleChoice(item, schemaVersion, ids));
}

// Kept as a public single-item predicate for existing harness consumers. Pack
// selection must use eligibleChoiceItems so duplicate-ID ordering is preserved.
export function isChoice(item, schemaVersion = 0) {
  return eligibleChoice(item, schemaVersion, new Set());
}

export function longestChoiceCases(records = readPacks()) {
  return records.map(({ id, file, pack }) => {
    const choices = eligibleChoiceItems(pack);
    const item = choices.reduce((best, candidate) => {
      if (!best) return candidate;
      const candidateLength = [...String(candidate.prompt || '')].length;
      const bestLength = [...String(best.prompt || '')].length;
      return candidateLength > bestLength ? candidate : best;
    }, null);
    return {
      packId: id,
      file,
      schemaVersion: pack.schema_version ?? null,
      totalItems: Array.isArray(pack.items) ? pack.items.length : 0,
      choiceItems: choices.length,
      item,
      pack,
    };
  });
}

export function finite(value) {
  return typeof value === 'number' && Number.isFinite(value);
}

function looksNormalized(values, source) {
  return source?.normalized === true || (source?.normalized !== false && values.every(value => value >= 0 && value <= 1));
}

export function readPoint(value, viewport) {
  if (!value) return null;
  let x;
  let y;
  if (Array.isArray(value)) [x, y] = value;
  else ({ x, y } = value);
  x = Number(x);
  y = Number(y);
  if (!finite(x) || !finite(y)) return null;
  const normalized = looksNormalized([x, y], value);
  return {
    x: normalized ? x * viewport.width : x,
    y: normalized ? y * viewport.height : y,
    source: normalized ? 'normalized' : 'css-px',
  };
}

export function readRect(value, viewport) {
  if (!value) return null;
  let x = Number(value.x ?? value.left);
  let y = Number(value.y ?? value.top);
  let width = Number(value.width);
  let height = Number(value.height);
  if (!finite(width) && finite(Number(value.right)) && finite(x)) width = Number(value.right) - x;
  if (!finite(height) && finite(Number(value.bottom)) && finite(y)) height = Number(value.bottom) - y;
  if (![x, y, width, height].every(finite) || width < 0 || height < 0) return null;
  const normalized = looksNormalized([x, y, width, height], value);
  if (normalized) {
    x *= viewport.width;
    width *= viewport.width;
    y *= viewport.height;
    height *= viewport.height;
  }
  return { x, y, width, height, source: normalized ? 'normalized' : 'css-px' };
}

export function pointInFrame(point, viewport) {
  return !!point && point.x >= 0 && point.x < viewport.width && point.y >= 0 && point.y < viewport.height;
}

export function pointInSafeFrame(point, viewport, marginRatio = 0.06) {
  const marginX = viewport.width * marginRatio;
  const marginY = viewport.height * marginRatio;
  return !!point && point.x >= marginX && point.x <= viewport.width - marginX && point.y >= marginY && point.y <= viewport.height - marginY;
}

export function rectInFrame(rect, viewport, tolerance = 0) {
  return !!rect && rect.width > 0 && rect.height > 0 && rect.x >= -tolerance && rect.y >= -tolerance &&
    rect.x + rect.width <= viewport.width + tolerance && rect.y + rect.height <= viewport.height + tolerance;
}

export function rectContains(outer, inner, tolerance = 0.75) {
  return !!outer && !!inner && inner.x >= outer.x - tolerance && inner.y >= outer.y - tolerance &&
    inner.x + inner.width <= outer.x + outer.width + tolerance &&
    inner.y + inner.height <= outer.y + outer.height + tolerance;
}

export function intersection(a, b) {
  if (!a || !b) return { x: 0, y: 0, width: 0, height: 0, area: null };
  const x = Math.max(a.x, b.x);
  const y = Math.max(a.y, b.y);
  const right = Math.min(a.x + a.width, b.x + b.width);
  const bottom = Math.min(a.y + a.height, b.y + b.height);
  const width = Math.max(0, right - x);
  const height = Math.max(0, bottom - y);
  return { x, y, width, height, area: width * height };
}

function canonicalType(type) {
  const value = String(type || '').toLowerCase();
  if (value === 'king' || value === 'hero') return 'king';
  if (['soldier', 'unit', 'troop', 'red-soldier', 'blue-soldier'].includes(value)) return 'soldier';
  if (['building', 'tower', 'barracks', 'structure', 'prop'].includes(value)) return 'building';
  if (value === 'tree' || value === 'foliage') return 'tree';
  return value;
}

function normalizePad(source, viewport, fallbackId) {
  const raw = source?.rect ?? source;
  return {
    id: String(source?.id ?? fallbackId),
    visible: source?.visible !== false,
    active: source?.active !== false,
    rect: readRect(raw, viewport),
  };
}

function normalizeGlyph(source, viewport, fallbackId, fallbackKind = 'body') {
  return {
    id: String(source?.id ?? fallbackId),
    char: String(source?.char ?? source?.character ?? ''),
    kind: String(source?.kind ?? fallbackKind).toLowerCase(),
    visible: source?.visible !== false,
    rect: readRect(source?.rect ?? source, viewport),
    backgroundPoints: (Array.isArray(source?.backgroundPoints) ? source.backgroundPoints : []).map(point => readPoint(point, viewport)).filter(Boolean),
  };
}

function normalizeChoiceLabel(source, viewport, index) {
  const glyphSource = source?.glyphs ?? [];
  return {
    id: String(source?.id ?? `choice-${index}`),
    text: String(source?.text ?? ''),
    bodyRect: readRect(source?.bodyRect, viewport),
    renderedTextRect: readRect(source?.renderedTextRect ?? source?.textRect, viewport),
    glyphs: (Array.isArray(glyphSource) ? glyphSource : []).map((glyph, glyphIndex) => normalizeGlyph(glyph, viewport, `choice-${index}-char-${glyphIndex}`, 'choice')),
    isTruncated: Boolean(source?.isTruncated),
    isOverflowing: Boolean(source?.isOverflowing),
    hasMissingGlyph: Boolean(source?.hasMissingGlyph),
    visibleCharacters: Number(source?.visibleCharacters),
    totalCharacters: Number(source?.totalCharacters),
  };
}

function normalizeWorldLabel(source, viewport, index) {
  return {
    id: String(source?.id ?? `world-label-${index}`),
    kind: String(source?.kind ?? '').toLowerCase(),
    visible: source?.visible !== false,
    active: source?.active !== false,
    rect: readRect(source?.rect ?? source, viewport),
  };
}

export function normalizeSnapshot(raw, viewport) {
  const shadowSource = raw?.shadowSamples ?? raw?.samples ?? raw?.groundSamples ?? [];
  const shadowSamples = (Array.isArray(shadowSource) ? shadowSource : []).map((sample, index) => {
    const ringSource = sample?.ring ?? sample?.surround ?? sample?.surroundPoints ?? sample?.groundPoints ?? [];
    let ring = (Array.isArray(ringSource) ? ringSource : [ringSource]).map(point => readPoint(point, viewport)).filter(point => pointInFrame(point, viewport));
    const reference = readPoint(sample?.ground ?? sample?.reference ?? sample?.ambient, viewport);
    if (pointInFrame(reference, viewport)) ring.push(reference);
    ring = [...new Map(ring.map(point => [`${point.x.toFixed(3)},${point.y.toFixed(3)}`, point])).values()];
    return {
      id: String(sample?.id ?? `${sample?.type ?? 'sample'}-${index + 1}`),
      type: canonicalType(sample?.type ?? sample?.category),
      visible: sample?.visible !== false,
      foot: readPoint(sample?.foot ?? sample?.shadow ?? sample?.shadowPoint ?? sample?.center, viewport),
      ring,
    };
  });

  const lineSource = raw?.frontLine?.points ?? raw?.frontLine ?? raw?.frontContacts ?? [];
  let frontLinePoints = (Array.isArray(lineSource) ? lineSource : []).map(point => readPoint(point, viewport)).filter(Boolean);
  const contact = readPoint(raw?.frontContact ?? raw?.front ?? raw?.front_contact, viewport);
  if (contact && frontLinePoints.length === 0) frontLinePoints = [contact];

  const questionSource = raw?.question && !Array.isArray(raw.question) ? raw.question : {};
  const question = {
    prompt: String(questionSource.prompt ?? raw?.prompt ?? ''),
    panelRect: readRect(questionSource.panelRect ?? raw?.questionRect ?? raw?.scrollRect ?? raw?.scroll, viewport),
    bodyRect: readRect(questionSource.bodyRect ?? raw?.questionBodyRect ?? raw?.promptRect, viewport),
    renderedTextRect: readRect(questionSource.renderedTextRect ?? questionSource.textRect ?? raw?.renderedTextRect, viewport),
    isFolded: Boolean(questionSource.isFolded ?? raw?.isQuestionFolded),
    hasEllipsis: Boolean(questionSource.hasEllipsis ?? raw?.questionHasEllipsis),
    isTruncated: Boolean(questionSource.isTruncated ?? raw?.questionIsTruncated),
    isOverflowing: Boolean(questionSource.isOverflowing ?? raw?.questionIsOverflowing),
    visibleCharacters: Number(questionSource.visibleCharacters ?? raw?.visibleCharacters),
    totalCharacters: Number(questionSource.totalCharacters ?? raw?.totalCharacters),
    diagnosticsReported: {
      isFolded: Object.hasOwn(questionSource, 'isFolded') || Object.hasOwn(raw ?? {}, 'isQuestionFolded'),
      hasEllipsis: Object.hasOwn(questionSource, 'hasEllipsis') || Object.hasOwn(raw ?? {}, 'questionHasEllipsis'),
      isTruncated: Object.hasOwn(questionSource, 'isTruncated') || Object.hasOwn(raw ?? {}, 'questionIsTruncated'),
      isOverflowing: Object.hasOwn(questionSource, 'isOverflowing') || Object.hasOwn(raw ?? {}, 'questionIsOverflowing'),
      visibleCharacters: Object.hasOwn(questionSource, 'visibleCharacters') || Object.hasOwn(raw ?? {}, 'visibleCharacters'),
      totalCharacters: Object.hasOwn(questionSource, 'totalCharacters') || Object.hasOwn(raw ?? {}, 'totalCharacters'),
    },
    glyphs: [],
  };
  const glyphSource = questionSource.glyphs ?? raw?.questionGlyphs ?? [];
  question.glyphs = (Array.isArray(glyphSource) ? glyphSource : []).map((glyph, index) => normalizeGlyph(glyph, viewport, index));

  const choiceSource = raw?.choicePadRects ?? raw?.choicePads ?? [];
  const upgradeSource = raw?.upgradePadRects ?? raw?.upgrades ?? raw?.upgradePads ?? [];
  let choicePadRects = (Array.isArray(choiceSource) ? choiceSource : []).map((pad, index) => normalizePad(pad, viewport, `choice-${index + 1}`));
  let upgradePadRects = (Array.isArray(upgradeSource) ? upgradeSource : []).map((pad, index) => normalizePad(pad, viewport, `upgrade-${index + 1}`));
  if (choicePadRects.length === 0 && upgradePadRects.length === 0) {
    const legacy = raw?.activePadRects ?? raw?.pads ?? raw?.padRects ?? [];
    choicePadRects = (Array.isArray(legacy) ? legacy : []).map((pad, index) => normalizePad(pad, viewport, `choice-${index + 1}`));
  }
  const choicesSource = raw?.choices ?? raw?.choiceLabels ?? [];
  const choices = (Array.isArray(choicesSource) ? choicesSource : []).map((choice, index) => normalizeChoiceLabel(choice, viewport, index));
  const worldLabelSource = raw?.worldLabels ?? [];
  const worldLabels = (Array.isArray(worldLabelSource) ? worldLabelSource : []).map((label, index) => normalizeWorldLabel(label, viewport, index));

  return {
    version: Number(raw?.version ?? raw?.schemaVersion ?? 0),
    shadowSamples,
    frontLine: { visible: raw?.frontLine?.visible !== false && raw?.frontContact?.visible !== false, points: frontLinePoints },
    question,
    choices,
    choicePadRects,
    upgradePadRects,
    worldLabels,
    battlefieldRect: readRect(raw?.battlefieldRect, viewport),
    kingScreen: readPoint(raw?.kingScreen ?? raw?.king?.center ?? raw?.kingCenter, viewport),
  };
}

export async function readDebugSnapshot(page) {
  return page.evaluate(() => {
    const debug = window.__HYEOPGOK_V3_DEBUG__ ?? window.__HYEOPGOK_ART_DEBUG__;
    if (!debug) return { __error: 'window.__HYEOPGOK_V3_DEBUG__ is missing (legacy __HYEOPGOK_ART_DEBUG__ also absent)' };
    try {
      const value = typeof debug === 'function' ? debug()
        : typeof debug.snapshot === 'function' ? debug.snapshot()
          : typeof debug.getSnapshot === 'function' ? debug.getSnapshot()
            : debug;
      return JSON.parse(JSON.stringify(value));
    } catch (error) {
      return { __error: `debug snapshot failed: ${String(error)}` };
    }
  });
}

export async function measureScreenshot(page, pngBase64, snapshot, viewport) {
  return page.evaluate(async ({ encoded, samples, glyphs, view }) => {
    const image = new Image();
    image.src = `data:image/png;base64,${encoded}`;
    await new Promise((resolve, reject) => { image.onload = resolve; image.onerror = reject; });
    const canvas = document.createElement('canvas');
    canvas.width = image.naturalWidth;
    canvas.height = image.naturalHeight;
    const context = canvas.getContext('2d', { willReadFrequently: true });
    context.drawImage(image, 0, 0);
    const rgba = context.getImageData(0, 0, canvas.width, canvas.height).data;
    const scaleX = canvas.width / view.width;
    const scaleY = canvas.height / view.height;
    const at = (x, y) => {
      const px = Math.max(0, Math.min(canvas.width - 1, Math.round(x * scaleX)));
      const py = Math.max(0, Math.min(canvas.height - 1, Math.round(y * scaleY)));
      const offset = (py * canvas.width + px) * 4;
      return [rgba[offset], rgba[offset + 1], rgba[offset + 2]];
    };
    const luminance = rgb => (0.2126 * rgb[0] + 0.7152 * rgb[1] + 0.0722 * rgb[2]) / 255;
    const median = values => {
      if (!values.length) return null;
      const sorted = [...values].sort((a, b) => a - b);
      return sorted[Math.floor(sorted.length / 2)];
    };
    const medianRgb = colors => colors.length ? [0, 1, 2].map(channel => median(colors.map(color => color[channel]))) : null;
    const luminanceAt = (point, radiusCss = 1.5) => {
      const values = [];
      const radiusX = Math.max(1, Math.round(radiusCss * scaleX));
      const radiusY = Math.max(1, Math.round(radiusCss * scaleY));
      const centerX = point.x * scaleX;
      const centerY = point.y * scaleY;
      for (let y = Math.max(0, Math.floor(centerY - radiusY)); y <= Math.min(canvas.height - 1, Math.ceil(centerY + radiusY)); y++) {
        for (let x = Math.max(0, Math.floor(centerX - radiusX)); x <= Math.min(canvas.width - 1, Math.ceil(centerX + radiusX)); x++) {
          const dx = (x - centerX) / radiusX;
          const dy = (y - centerY) / radiusY;
          if (dx * dx + dy * dy > 1) continue;
          const offset = (y * canvas.width + x) * 4;
          values.push(luminance([rgba[offset], rgba[offset + 1], rgba[offset + 2]]));
        }
      }
      return median(values);
    };

    const measuredShadows = samples.map(sample => {
      const footLuminance = sample.foot ? luminanceAt(sample.foot) : null;
      const references = (sample.ring || []).map(point => ({ point, luminance: luminanceAt(point) })).filter(value => value.luminance != null);
      const sorted = references.map(value => value.luminance).sort((a, b) => a - b);
      const groundLuminance = sorted.length ? sorted[Math.min(sorted.length - 1, Math.floor(sorted.length * 0.625))] : null;
      const darkness = footLuminance != null && groundLuminance != null && groundLuminance > 1e-6
        ? (groundLuminance - footLuminance) / groundLuminance : null;
      return { ...sample, footLuminance, groundLuminance, references, darkness };
    });

    const measuredGlyphs = glyphs.map(glyph => {
      const rect = glyph.rect;
      if (!rect || rect.width <= 0 || rect.height <= 0) return { ...glyph, inkHeightCss: null, reason: 'invalid rect' };
      const explicitBackground = (glyph.backgroundPoints || []).map(at);
      const border = [];
      if (!explicitBackground.length) {
        const stepX = Math.max(1, rect.width / 8);
        const stepY = Math.max(1, rect.height / 8);
        for (let x = rect.x - 2; x <= rect.x + rect.width + 2; x += stepX) {
          border.push(at(x, rect.y - 2), at(x, rect.y + rect.height + 2));
        }
        for (let y = rect.y - 2; y <= rect.y + rect.height + 2; y += stepY) {
          border.push(at(rect.x - 2, y), at(rect.x + rect.width + 2, y));
        }
      }
      const background = medianRgb(explicitBackground.length ? explicitBackground : border);
      if (!background) return { ...glyph, inkHeightCss: null, reason: 'no background samples' };
      const backgroundLuminance = luminance(background);
      const left = Math.max(0, Math.floor(rect.x * scaleX));
      const top = Math.max(0, Math.floor(rect.y * scaleY));
      const right = Math.min(canvas.width - 1, Math.ceil((rect.x + rect.width) * scaleX));
      const bottom = Math.min(canvas.height - 1, Math.ceil((rect.y + rect.height) * scaleY));
      const activeRows = [];
      for (let y = top; y <= bottom; y++) {
        let ink = 0;
        for (let x = left; x <= right; x++) {
          const offset = (y * canvas.width + x) * 4;
          const rgb = [rgba[offset], rgba[offset + 1], rgba[offset + 2]];
          const distance = Math.hypot(rgb[0] - background[0], rgb[1] - background[1], rgb[2] - background[2]) / 441.673;
          if (Math.abs(luminance(rgb) - backgroundLuminance) >= 0.075 || distance >= 0.16) ink++;
        }
        const required = Math.max(1, Math.ceil((right - left + 1) * 0.045));
        if (ink >= required) activeRows.push(y);
      }
      if (!activeRows.length) return { ...glyph, background, inkHeightCss: 0, activeRows: 0, reason: 'no ink rows' };
      // Measure the complete ink extent. Closed Korean glyphs such as 공/용 have
      // a deliberate empty band through the middle; taking only the longest
      // connected run under-reports an otherwise full-height rendered glyph.
      const bestStart = activeRows[0];
      const bestEnd = activeRows[activeRows.length - 1];
      return {
        ...glyph,
        background,
        activeRows: activeRows.length,
        inkTopDevicePx: bestStart,
        inkBottomDevicePx: bestEnd,
        inkHeightCss: (bestEnd - bestStart + 1) / scaleY,
        reason: null,
      };
    });
    return {
      screenshot: { width: canvas.width, height: canvas.height, scaleX, scaleY },
      shadows: measuredShadows,
      glyphs: measuredGlyphs,
    };
  }, { encoded: pngBase64, samples: snapshot.shadowSamples, glyphs: snapshot.question.glyphs, view: viewport });
}

export async function openBrowserHarness() {
  const chrome = resolveChrome();
  const server = await serveStatic(PUBLIC);
  const browser = await puppeteer.launch({
    headless: true,
    executablePath: chrome,
    args: ['--no-sandbox', '--mute-audio', '--disable-background-timer-throttling', '--disable-renderer-backgrounding'],
  });
  return { chrome, server, browser };
}

export function attachDiagnostics(page, report) {
  page.on('pageerror', error => report.consoleErrors.push(String(error)));
  page.on('console', message => {
    if (message.type() === 'error') report.consoleErrors.push(message.text());
    if (message.type() === 'warning') report.consoleWarnings.push(message.text());
  });
  page.on('requestfailed', request => report.failedRequests.push({ url: request.url(), error: request.failure()?.errorText ?? 'unknown' }));
  page.on('response', response => {
    if (response.status() >= 400) report.failedRequests.push({ url: response.url(), status: response.status() });
  });
}

export async function setViewport(page, viewport) {
  await page.setViewport({
    width: viewport.width,
    height: viewport.height,
    deviceScaleFactor: viewport.deviceScaleFactor,
    isMobile: viewport.mobile,
    hasTouch: true,
  });
}

export async function loadGame(page, server, viewport, query) {
  await setViewport(page, viewport);
  // Unity may keep browser-side fetch bookkeeping alive while the previous
  // WebGL instance is being torn down.  The game-ready hook is the real load
  // contract; waiting for networkidle0 made long 13-pack sweeps intermittently
  // lose a valid case after 90 seconds.
  await page.goto(`${server.url}/g/hyeopgok-sasu/?${query}`, { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 90000 });
  await sleep(450);
}

export async function startGame(page) {
  const accepted = await page.evaluate(() => window.__GAME_TEST__?.start?.());
  if (!accepted) throw new Error('__GAME_TEST__.start() was not accepted');
  await page.waitForFunction(() => window.__GAME_TEST__?.getState?.().phase === 'playing', { timeout: 15000 });
}

export async function waitForState(page, predicateSource, timeout = 15000, args = []) {
  await page.waitForFunction(predicateSource, { timeout, polling: 35 }, ...args);
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

export async function currentState(page) {
  return page.evaluate(() => window.__GAME_TEST__?.getState?.() ?? null);
}

export async function touch(page, x, y, holdMs = 80) {
  const cdp = await page.createCDPSession();
  await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x, y, id: 1 }] });
  await sleep(holdMs);
  await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  await cdp.detach();
}

export function centerOf(rect) {
  return rect ? { x: rect.x + rect.width / 2, y: rect.y + rect.height / 2 } : null;
}
