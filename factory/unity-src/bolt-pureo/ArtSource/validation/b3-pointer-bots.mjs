import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname);
const PUBLIC=path.join(ROOT,'public');
const GAME=path.join(PUBLIC,'g/bolt-pureo');
const TRIALS=200, CHANCE=1/12;
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
function chrome(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const b of fs.readdirSync(base).sort().reverse())for(const rel of['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing']){const p=path.join(base,b,rel);if(fs.existsSync(p))return p;}}
function rng(seed){let x=seed>>>0;return()=>{x^=x<<13;x^=x>>>17;x^=x<<5;return(x>>>0)/4294967296;};}
function hash(){const h=crypto.createHash('sha256'),files=[];(function walk(d){for(const n of fs.readdirSync(d).sort()){const f=path.join(d,n);fs.statSync(f).isDirectory()?walk(f):files.push(f);}})(GAME);for(const f of files){h.update(path.relative(GAME,f));h.update(fs.readFileSync(f));}return h.digest('hex');}
function wilson(c,n){if(!n)return[0,0];const z=1.959963984540054,p=c/n,d=1+z*z/n,m=(p+z*z/(2*n))/d,a=z*Math.sqrt((p*(1-p)+z*z/(4*n))/n)/d;return[Math.max(0,m-a),Math.min(1,m+a)];}
const server=await serveStatic(PUBLIC);
async function open(){const browser=await puppeteer.launch({headless:true,executablePath:chrome(),args:['--no-sandbox','--mute-audio']});const page=await browser.newPage(),errors=[];await page.setViewport({width:390,height:844,deviceScaleFactor:1});page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(String(e)));await page.goto(`${server.url}/g/bolt-pureo/?b3=${Date.now()}`,{waitUntil:'load',timeout:60000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:90000});return{browser,page,errors};}
async function hold(page,index){const s=await page.evaluate(()=>window.__GAME_TEST__.getState()),pts=(s.hitMap||'').split(';').map(x=>x.split(',').map(Number)),[x,y]=pts[index];await page.mouse.move(x,y);await sleep(20);await page.mouse.down();await sleep(310);await page.mouse.up();await sleep(100);}
async function run(kind){const{browser,page,errors}=await open();const random=rng(20261016);let correct=0,total=0,completed=0;try{for(let trial=0;trial<TRIALS;trial++){await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(100);const index=kind==='fixed'?0:kind==='cycle'?trial%12:Math.floor(random()*12);await hold(page,index);const s=await page.evaluate(()=>window.__GAME_TEST__.getState());total+=s.firstAttempted;correct+=s.firstCorrect;if(s.phase==='clear')completed++;if((trial+1)%50===0)console.log(`${kind} progress ${trial+1}/${TRIALS} correct=${correct}`);}}finally{await page.close();await browser.close();}const rate=total?correct/total:0;return{games:TRIALS,firstAttemptCorrect:correct,firstAttemptTotal:total,firstAttemptRate:rate,chance:CHANCE,wilson95:wilson(correct,total),completed,completionRate:completed/TRIALS,consoleErrors:errors,passed:total>0&&rate<=CHANCE&&errors.length===0};}
const fixed=await run('fixed');console.log('fixed',JSON.stringify(fixed));
const cycle=await run('cycle');console.log('cycle',JSON.stringify(cycle));
const random=await run('random');console.log('random',JSON.stringify(random));
const idleOpen=await open();let progressed=0;try{for(let trial=0;trial<TRIALS;trial++){await idleOpen.page.evaluate(()=>window.__GAME_TEST__.start());const a=await idleOpen.page.evaluate(()=>window.__GAME_TEST__.getState());await sleep(24);const b=await idleOpen.page.evaluate(()=>window.__GAME_TEST__.getState());if(b.solved!==a.solved||b.firstAttempted!==a.firstAttempted)progressed++;}}finally{await idleOpen.page.close();await idleOpen.browser.close();}
const idle={games:TRIALS,firstAttemptCorrect:0,firstAttemptTotal:0,firstAttemptRate:0,chance:CHANCE,completionRate:0,progressed,consoleErrors:idleOpen.errors,passed:progressed===0&&idleOpen.errors.length===0};
const result={runId:`bolt-pureo-pointer-bots-${new Date().toISOString()}`,artifactHashSha256:hash(),criterion:'각 200판 첫 시도 정답률이 유일 정답 1/12 우연 수준 이하, 무입력 진도 0',method:'390×844 Chrome 실제 canvas mouse down 310ms/up. 매 판 __GAME_TEST__.start로 새 본판만 열고 상태의 정답 비포함 hitMap 좌표에 실제 포인터를 보냈다. fixed=같은 인덱스, cycle=12인덱스 순환, random=xorshift seed 20261016의 12볼트 균등 입력. prompt·answer·candidate 종류는 읽지 않았다.',fixed,cycle,random,idle,passed:fixed.passed&&cycle.passed&&random.passed&&idle.passed};
fs.writeFileSync(new URL('./b3-results.json',import.meta.url),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify(result,null,2));await server.close();if(!result.passed)process.exitCode=1;
