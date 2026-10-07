import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname);
const PUBLIC=path.join(ROOT,'public');
const GAME=path.join(PUBLIC,'g/semo-sonagi');
const OUT=new URL('./area-feedback/',import.meta.url);
fs.mkdirSync(OUT,{recursive:true});
const CENTERS=[[105,346],[285,346],[105,557],[285,557]];
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));

function chrome(){
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  for(const bundle of fs.readdirSync(base).sort().reverse())for(const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
  ]){const file=path.join(base,bundle,rel);if(fs.existsSync(file))return file;}
}

function artifactHash(){
  const files=[];(function walk(dir){for(const name of fs.readdirSync(dir).sort()){const file=path.join(dir,name);fs.statSync(file).isDirectory()?walk(file):files.push(file);}})(GAME);
  const hash=crypto.createHash('sha256');for(const file of files){hash.update(path.relative(GAME,file));hash.update(fs.readFileSync(file));}return hash.digest('hex');
}

async function swipe(page,a,b=a){
  await page.mouse.move(a[0]-36,a[1]);await page.mouse.down();
  await page.mouse.move(b[0]+36,b[1],{steps:10});await page.mouse.up();
}

const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chrome(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const page=await browser.newPage(),errors=[];
await page.setViewport({width:390,height:844,deviceScaleFactor:1});
page.on('console',msg=>{if(msg.type()==='error')errors.push(msg.text());});
page.on('pageerror',error=>errors.push(String(error)));
await page.goto(`${server.url}/g/semo-sonagi/?area-feedback=1`,{waitUntil:'load',timeout:45000});
await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});await sleep(500);
await page.mouse.click(195,610);await sleep(350);
await swipe(page,[90,456],[108,456]);await sleep(2400);

const start=await page.evaluate(()=>window.__GAME_TEST__.getState());
const wrongIndices=[];for(let i=0;i<4;i++)if((start.correctMask&(1<<i))===0)wrongIndices.push(i);
const cases=[];
for(let n=0;n<wrongIndices.length;n++){
  const index=wrongIndices[n];await swipe(page,CENTERS[index]);await sleep(420);
  const state=await page.evaluate(()=>window.__GAME_TEST__.getState());
  const file=`0${n+1}-${state.misconceptionId}.png`;await page.screenshot({path:new URL(file,OUT).pathname});
  cases.push({candidate:index,state,screen:file,expected:state.misconceptionId==='no_square'
    ?'세 수는 이미 정사각형의 넓이다. 작은 두 넓이를 그대로 더해 가장 큰 넓이와 비교하라.'
    :'가까운 값이 아니라 작은 두 넓이의 합과 가장 큰 넓이가 정확히 같아야 한다.'});
  await sleep(1600);
}
const ids=cases.map(item=>item.state.misconceptionId).sort();
const result={artifactHashSha256:artifactHash(),method:'390×844 실제 canvas mouse down/move/up으로 넓이형 첫 문제의 두 거짓 후보를 각각 제출하고 reveal 화면을 캡처했다. 테스트 훅은 상태 관찰에만 사용했다.',cases,errors,passed:ids.join(',')==='near_square,no_square'&&cases.every(item=>item.state.phase==='playing'&&item.state.problemId==='ss-r1')&&errors.length===0};
fs.writeFileSync(new URL('./area-feedback-results.json',import.meta.url),JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
await browser.close();await server.close();if(!result.passed)process.exitCode=1;
