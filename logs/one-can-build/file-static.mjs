import { pathToFileURL } from 'node:url';
import path from 'node:path';

export async function serveStatic(rootDir) {
  return {
    // qa.mjs appends /g/<slug>/; keep that suffix inside a harmless fragment.
    url: pathToFileURL(path.join(rootDir, 'g/one-can/index.html')).href + '#',
    close: async () => {},
  };
}
