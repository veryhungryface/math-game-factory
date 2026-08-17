#!/usr/bin/env node
/** 로컬 미리보기 서버.  node factory/lib/serve.mjs [port] */
import { P } from './paths.mjs';
import { serveStatic } from './static-server.mjs';

const port = Number(process.argv[2] || 4321);
const { url } = await serveStatic(P.publicDir, port);
console.log(`수학 놀이터 로컬 서버 → ${url}`);
console.log('종료: Ctrl+C');
