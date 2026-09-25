const replacement = new URL('./file-static.mjs', import.meta.url).href;
const puppeteerReplacement = new URL('./puppeteer-pipe.mjs', import.meta.url).href;

export async function resolve(specifier, context, nextResolve) {
  const validationEntry =
    context.parentURL?.endsWith('/factory/lib/qa.mjs') ||
    context.parentURL?.endsWith('/factory/lib/firstplay/harness.mjs');
  if (
    (specifier === './static-server.mjs' || specifier === '../static-server.mjs') &&
    validationEntry
  ) {
    return { url: replacement, shortCircuit: true };
  }
  if (specifier === 'puppeteer' && validationEntry) {
    return { url: puppeteerReplacement, shortCircuit: true };
  }
  return nextResolve(specifier, context);
}
