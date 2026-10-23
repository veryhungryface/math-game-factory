import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname),PUBLIC=path.join(ROOT,'public');
const TRIALS=Number(process.env.BOT_TRIALS||200),CHANCE=1/25;
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
function chromePath(){const b=path.join(process.env.HOME||'','.cache/puppeteer/chrome');if(!fs.existsSync(b))return undefined;for(const v of fs.readdirSync(b).sort().reverse()){const p=path.join(b,v,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');if(fs.existsSync(p))return p;}}
function rnd(seed){let x=seed>>>0;return()=>{x^=x<<13;x^=x>>>17;x^=x<<5;return(x>>>0)/4294967296;};}
function binomialTail(k,n,p){let q=Math.pow(1-p,n),tail=k===0?q:0;for(let i=1;i<=n;i++){q*=((n-i+1)/i)*(p/(1-p));if(i>=k)tail+=q;}return Math.min(1,tail);}
function target(s,snap){const t=(snap+12)/24;return{x:s.railStartScreenX+(s.railEndScreenX-s.railStartScreenX)*t,y:s.railStartScreenY+(s.railEndScreenY-s.railStartScreenY)*t};}
async function drag(page,s,snap){const b=target(s,snap);await page.mouse.move(s.handleScreenX,s.handleScreenY);await page.mouse.down();await sleep(18);await page.mouse.move(b.x,b.y,{steps:4});await sleep(18);await page.mouse.up();await sleep(42);}
function artifactHash(){const dir=path.join(PUBLIC,'g/seongnyu-core'),files=[];(function walk(d){for(const f of fs.readdirSync(d).sort()){const p=path.join(d,f);fs.statSync(p).isDirectory()?walk(p):files.push(p);}})(dir);const h=crypto.createHash('sha256');for(const f of files){h.update(path.relative(dir,f));h.update(fs.readFileSync(f));}return h.digest('hex');}

const server=await serveStatic(PUBLIC),browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
async function open(){const page=await browser.newPage(),errors=[];await page.setViewport({width:390,height:844,deviceScaleFactor:1});page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(String(e)));await page.goto(`${server.url}/g/seongnyu-core/`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});return{page,errors};}
async function run(kind){const {page,errors}=await open();const random=rnd(1);let correct=0,submitted=0,complete=0,progressed=0,invalidSamples=[];
  for(let i=0;i<TRIALS;i++){
    const before=await page.evaluate(()=>{window.__GAME_TEST__.start();return window.__GAME_TEST__.getState();});await sleep(18);
    if(kind==='noinput'){const after=await page.evaluate(()=>window.__GAME_TEST__.getState());if(after.solved!==0||after.firstAttempts!==0||after.firstCorrect!==0)progressed++;continue;}
    const snap=kind==='repeat'?0:kind==='cycle'?-12+(i%25):-12+Math.floor(random()*25);
    const fresh=await page.evaluate(()=>window.__GAME_TEST__.getState());await drag(page,fresh,snap);
    let after=await page.evaluate(()=>window.__GAME_TEST__.getState());
    for(let retry=0;after.firstAttempts===0&&retry<2;retry++){await drag(page,after,snap);after=await page.evaluate(()=>window.__GAME_TEST__.getState());}
    if(after.firstAttempts===1){submitted++;if(after.firstCorrect===1)correct++;}else if(invalidSamples.length<8)invalidSamples.push({i,snap,before:fresh,after});if(after.phase==='clear')complete++;
  }
  await page.close();const rate=correct/TRIALS,p=binomialTail(correct,TRIALS,CHANCE);
  const passed=kind==='noinput'?progressed===0&&errors.length===0:submitted===TRIALS&&rate<=CHANCE&&p>=.05&&errors.length===0;
  return{trials:TRIALS,submitted,correct,firstTryRate:rate,chance:kind==='noinput'?0:CHANCE,pValueAboveChance:p,completionRate:complete/TRIALS,progressed,invalidSamples,consoleErrors:errors,passed};
}
const repeat=await run('repeat'),cycle=await run('cycle'),random=await run('random'),noinput=await run('noinput');
const result={run_id:`seongnyu-core-b3-${new Date().toISOString()}`,artifact_hash_sha256:artifactHash(),method:'Chrome CDP real mouse events only; repeat=snap 0, cycle=25 snaps in order, random=seeded uniform snap, noinput=start then no event. No prompt/id/answer/test-answer/sampleProblems is read.',criterion:'each 200-run first-attempt rate must be <= exact 1/25 chance and not significantly above it; noinput progress and completion are zero',repeat,cycle,random,noinput,passed:repeat.passed&&cycle.passed&&random.passed&&noinput.passed};
console.log(JSON.stringify(result,null,2));fs.writeFileSync(path.join(path.dirname(new URL(import.meta.url).pathname),'b3-pointer-results.json'),JSON.stringify(result,null,2));
await browser.close();await server.close();if(!result.passed)process.exitCode=1;
