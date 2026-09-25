// Environment-only adapter: original QA and firstplay checks are not changed.
const replacement = new URL('./file-static.mjs', import.meta.url).href;
const puppeteerReplacement = new URL('./puppeteer-pipe.mjs', import.meta.url).href;
export async function resolve(specifier, context, nextResolve) {
  const entry = context.parentURL?.endsWith('/factory/lib/qa.mjs') || context.parentURL?.endsWith('/factory/lib/firstplay/harness.mjs');
  if (entry && (specifier === './static-server.mjs' || specifier === '../static-server.mjs')) return { url: replacement, shortCircuit: true };
  if (entry && specifier === 'puppeteer') return { url: puppeteerReplacement, shortCircuit: true };
  return nextResolve(specifier, context);
}
