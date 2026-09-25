// Only local-file navigation and browser launch transport change, never assertions.
const replacement = new URL('./browser-static.mjs', import.meta.url).href;
const puppeteerReplacement = new URL('./browser-pipe.mjs', import.meta.url).href;
export async function resolve(specifier, context, nextResolve) {
  const entry = context.parentURL?.endsWith('/factory/lib/qa.mjs') || context.parentURL?.endsWith('/factory/lib/firstplay/harness.mjs');
  if (entry && (specifier === './static-server.mjs' || specifier === '../static-server.mjs')) return { url: replacement, shortCircuit: true };
  if (entry && specifier === 'puppeteer') return { url: puppeteerReplacement, shortCircuit: true };
  return nextResolve(specifier, context);
}
