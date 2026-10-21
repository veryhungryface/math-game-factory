#!/usr/bin/env node
// 최종 카탈로그 표지는 완성된 WebGL 런타임 타이틀을 정확한 납품 치수로 캡처한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const PUBLIC=path.join(ROOT,'public');
const OUT=path.join(PUBLIC,'g/jibung-galla');
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
function chromePath(){
  if(process.env.PUPPETEER_EXECUTABLE_PATH)return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  for(const build of fs.readdirSync(base).sort().reverse())for(const rel of ['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-linux64/chrome']){const p=path.join(base,build,rel);if(fs.existsSync(p))return p;}
  throw new Error('Chrome for Testing not found');
}
const server=await serveStatic(PUBLIC);const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
try{
  for(const spec of [{w:1200,h:630,name:'thumb.png'},{w:1080,h:1080,name:'square.png'}]){
    const page=await browser.newPage();await page.setViewport({width:spec.w,height:spec.h,deviceScaleFactor:1});
    await page.goto(`${server.url}/g/jibung-galla/`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});await sleep(2600);
    await page.screenshot({path:path.join(OUT,spec.name),type:'png'});await page.close();
  }
  console.log('captured runtime covers: thumb.png 1200x630, square.png 1080x1080');
}finally{await browser.close();await server.close();}
