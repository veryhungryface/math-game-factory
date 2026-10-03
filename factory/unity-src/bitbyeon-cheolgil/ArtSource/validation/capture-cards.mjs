// 실제 Unity 타이틀 세계를 배포 카드 규격으로 캡처해 타이틀/인게임 재질 불일치를 막는다.
import path from 'node:path';
import fs from 'node:fs';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';
const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname),PUBLIC=path.join(ROOT,'public'),OUT=path.join(PUBLIC,'g/bitbyeon-cheolgil');
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
function chrome(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const b of fs.readdirSync(base).sort().reverse()){const p=path.join(base,b,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');if(fs.existsSync(p))return p;}}
const server=await serveStatic(PUBLIC),browser=await puppeteer.launch({headless:true,executablePath:chrome(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
for(const [name,width,height] of [['thumb.png',1200,630],['square.png',1080,1080]]){const p=await browser.newPage();await p.setViewport({width,height,deviceScaleFactor:1});await p.goto(`${server.url}/g/bitbyeon-cheolgil/`,{waitUntil:'load',timeout:45000});await p.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});await sleep(800);await p.screenshot({path:path.join(OUT,name)});await p.close();}
await browser.close();await server.close();console.log('captured thumb.png 1200x630 and square.png 1080x1080 from live title');
