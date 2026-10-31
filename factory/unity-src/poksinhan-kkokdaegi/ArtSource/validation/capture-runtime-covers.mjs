#!/usr/bin/env node
// 생성 키아트 대신 최종 Unity WebGL 타이틀을 카탈로그 표지로 촬영한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const GAME=path.join(ROOT,'public/g/poksinhan-kkokdaegi');
const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
function chromePath(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const b of fs.readdirSync(base).sort().reverse()){const p=path.join(base,b,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');if(fs.existsSync(p))return p;}}

const server=await serveStatic(path.join(ROOT,'public'));
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const errors=[];
for(const [name,width,height] of [['thumb.png',1200,630],['square.png',1080,1080]]){
  const page=await browser.newPage();
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  page.on('pageerror',e=>errors.push(String(e)));
  await page.setViewport({width,height,deviceScaleFactor:1});
  await page.goto(`${server.url}/g/poksinhan-kkokdaegi/`,{waitUntil:'load',timeout:45000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready===true,{timeout:60000});
  await sleep(1200);
  await page.$eval('#mgf-mute',el=>{el.style.display='none';});
  await page.screenshot({path:path.join(GAME,name)});
  await page.close();
}
fs.copyFileSync(path.join(GAME,'thumb.png'),path.join(GAME,'assets/title.png'));
const result={source:'final Unity WebGL title',thumb:'1200x630',square:'1080x1080',errors,passed:errors.length===0};
fs.writeFileSync(path.join(HERE,'cover-capture-results.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify(result,null,2));
await browser.close();await server.close();
if(!result.passed)process.exitCode=1;
