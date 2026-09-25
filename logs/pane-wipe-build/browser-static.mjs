import { pathToFileURL } from 'node:url';
import path from 'node:path';
export async function serveStatic(rootDir) {
  return {
    // Harnesses append /g/<slug>/; a fragment keeps the local file target intact.
    url: pathToFileURL(path.join(rootDir, 'g/pane-wipe/index.html')).href + '#',
    close: async () => {},
  };
}
