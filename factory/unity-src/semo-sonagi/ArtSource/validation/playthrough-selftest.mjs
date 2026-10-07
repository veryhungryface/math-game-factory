import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname),PUBLIC=path.join(ROOT,'public');
const GAME=path.join(PUBLIC,'g/semo-sonagi');
const OUT=new URL('./playthrough/',import.meta.url);fs.mkdirSync(OUT,{recursive:true});
const PAIR_MASKS=[3,5,9,6,10,12],CENTERS=[[105,346],[285,346],[105,557],[285,557]];
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
function chrome(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const b of fs.readdirSync(base).sort().reverse())for(const r of ['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing']){const f=path.join(base,b,r);if(fs.existsSync(f))return f;}}
function artifactHash(){const files=[];(function walk(dir){for(const n of fs.readdirSync(dir).sort()){const f=path.join(dir,n);fs.statSync(f).isDirectory()?walk(f):files.push(f);}})(GAME);const h=crypto.createHash('sha256');for(const f of files){h.update(path.relative(GAME,f));h.update(fs.readFileSync(f));}return h.digest('hex');}
async function state(page){return page.evaluate(()=>window.__GAME_TEST__.getState());}
async function swipeMask(page,mask,log,label){const ids=[];for(let i=0;i<4;i++)if(mask&(1<<i))ids.push(i);const a=CENTERS[ids[0]],b=CENTERS[ids[1]??ids[0]];await page.mouse.move(a[0],a[1]);await page.mouse.down();await sleep(30);await page.mouse.move(b[0],b[1],{steps:12});await sleep(30);await page.mouse.up();log.push({label,type:'pointer-swipe',from:a,to:b,mask});}
async function practice(page,log,label){await page.mouse.move(90,456);await page.mouse.down();await page.mouse.move(144,456,{steps:8});await page.mouse.up();log.push({label,type:'pointer-swipe',from:[90,456],to:[144,456],mask:1});await sleep(2350);}
async function shot(page,name){await page.screenshot({path:new URL(name,OUT).pathname});}
async function clearRun(page,log,{wrongFirst=false,prefix}){
  let s=await state(page);
  if(wrongFirst){const wrong=PAIR_MASKS.find(m=>m!==s.correctMask);await swipeMask(page,wrong,log,`${prefix}-wave1-wrong`);await sleep(1950);s=await state(page);await swipeMask(page,s.correctMask,log,`${prefix}-wave1-recovery`);await sleep(2350);}
  while((s=await state(page)).phase==='playing'&&s.solved<6){const finalAudit=s.solved===5;await swipeMask(page,s.correctMask,log,`${prefix}-wave${s.solved+1}`);if(finalAudit){await sleep(420);await shot(page,prefix==='run1'?'03-run1-audit-reveal.png':'06-run2-audit-reveal.png');await sleep(1930);}else await sleep(2350);}
  return state(page);
}

const server=await serveStatic(PUBLIC),browser=await puppeteer.launch({headless:true,executablePath:chrome(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const page=await browser.newPage(),errors=[],inputLog=[];await page.setViewport({width:390,height:844,deviceScaleFactor:1});page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(String(e)));
await page.goto(`${server.url}/g/semo-sonagi/?playthrough=2`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});await sleep(700);await shot(page,'00-title.png');
await page.mouse.click(195,610);inputLog.push({label:'title-start',type:'pointer-tap',at:[195,610]});await sleep(450);await shot(page,'01-practice.png');
await practice(page,inputLog,'run1-practice');await shot(page,'02-first-wave.png');
const run1=await clearRun(page,inputLog,{wrongFirst:true,prefix:'run1'});await shot(page,'04-run1-recovery-clear.png');

// 결과 화면부터 두 번째 판 끝까지 start()/answerCorrect() 없이 실제 pointer만 사용한다.
await page.mouse.click(195,610);inputLog.push({label:'run2-restart',type:'pointer-tap',at:[195,610]});await sleep(450);await shot(page,'04-run2-practice.png');
await practice(page,inputLog,'run2-practice');const run2Start=await state(page);await shot(page,'05-run2-first-wave.png');
const run2=await clearRun(page,inputLog,{wrongFirst:false,prefix:'run2'});await shot(page,'07-run2-clear.png');

const hash=artifactHash();
const result={
  artifactHashSha256:hash,
  method:'새 페이지의 타이틀→연습→첫 판→결과→연습→두 번째 판을 실제 canvas mouse down/move/up만으로 연속 실행. __GAME_TEST__는 상태 관찰에만 사용하고 start/correct/wrong 명령은 호출하지 않음.',
  run1,run2Start,run2,inputLog,errors,
  passed:run1.phase==='clear'&&run1.solved===6&&run1.firstAttemptCorrect===5&&run1.firstAttemptTotal===6&&run1.lives===2&&run2.phase==='clear'&&run2.solved===6&&run2.firstAttemptCorrect===6&&run2.firstAttemptTotal===6&&run2.lives===3&&errors.length===0
};
console.log(JSON.stringify(result,null,2));fs.writeFileSync(new URL('./playthrough-results.json',import.meta.url),JSON.stringify(result,null,2));
await browser.close();await server.close();if(!result.passed)process.exitCode=1;
