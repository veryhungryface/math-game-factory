import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';
import {acquireMachineLock} from '../../../../lib/machine-lock.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const HERE = path.dirname(new URL(import.meta.url).pathname);
const FRAMES = path.join(HERE, 'playthrough-frames');
const sleep = ms => new Promise(r => setTimeout(r, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const b of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
  ]) { const p = path.join(base, b, rel); if (fs.existsSync(p)) return p; }
}

function artifactHash() {
  const dir = path.join(PUBLIC, 'g/prism-mobil'); const files = [];
  (function walk(d){for(const f of fs.readdirSync(d).sort()){const p=path.join(d,f);fs.statSync(p).isDirectory()?walk(p):files.push(p);}})(dir);
  const h=crypto.createHash('sha256');for(const f of files){h.update(path.relative(dir,f));h.update(fs.readFileSync(f));}return h.digest('hex');
}

const stage=(x,y)=>[.10+.80*x,.27+.45*y];
const grid=(x,y)=>stage(.05+.90*x/12,.08+.84*y/12);
const linePoint=(kind,node,max)=>kind==='parallel'
  ? [stage(.50,.90)[0]+(stage(.10,.10)[0]-stage(.50,.90)[0])*node/max,stage(.50,.90)[1]+(stage(.10,.10)[1]-stage(.50,.90)[1])*node/max]
  : [stage(.50,.90)[0],stage(.50,.90)[1]+(stage(.50,.10)[1]-stage(.50,.90)[1])*node/max];
const toPx=([x,y])=>[x*390,(1-y)*844];

async function dragNorm(page,a,b){const [x1,y1]=toPx(a),[x2,y2]=toPx(b);await page.mouse.move(x1,y1);await page.mouse.down();await page.mouse.move(x2,y2,{steps:12});await sleep(70);await page.mouse.up();await sleep(90);}
function parseProblem(prompt){
  let m=prompt.match(/AE=(\d+) cm, EC=(\d+) cm/);if(m){const ae=+m[1],ec=+m[2];return{kind:'parallel',node:24*ae/(ae+ec),max:24};}
  m=prompt.match(/중선 AD=(\d+) cm/);if(m){const ad=+m[1];return{kind:'median',node:ad*2/3,max:ad};}
  const nums=[...prompt.matchAll(/x=(\d+), y=(\d+)/g)].map(x=>[+x[1],+x[2]]);if(nums.length===3){return{kind:'centroid',a:nums[0],b:nums[1],c:nums[2],g:[(nums[0][0]+nums[1][0]+nums[2][0])/3,(nums[0][1]+nums[1][1]+nums[2][1])/3]};}
  throw new Error('unparsed prompt: '+prompt);
}

const release=await acquireMachineLock('gpu',{label:'prism-mobil 실제 포인터 완주'});
const server=await serveStatic(PUBLIC);const errors=[];fs.mkdirSync(FRAMES,{recursive:true});
const browser=await puppeteer.launch({headless:true,executablePath:resolveChrome(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const page=await browser.newPage();await page.setViewport({width:390,height:844,deviceScaleFactor:1});
page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});page.on('pageerror',e=>errors.push(String(e)));
await page.goto(`${server.url}/g/prism-mobil/`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
const bank=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(1000));const byId=new Map(bank.map(p=>[p.id,p]));const log=[];
async function state(){return page.evaluate(()=>window.__GAME_TEST__.getState())}
async function shot(name){await page.screenshot({path:path.join(FRAMES,name+'.png')})}

// 첫 판: 타이틀에서 시작해 안내 점선대로 4 cm 연습을 실제 드래그로 완료한다.
await dragNorm(page,[.5,.56],[.5,.56]);await sleep(250);await shot('00-practice-guide');
await dragNorm(page,[.5,.70],linePoint('median',4,6));await sleep(1750);let s=await state();log.push({step:'practice-correct',state:s});
if(s.onboarding||s.solved!==0||s.phase!=='playing')throw new Error('practice did not enter run '+JSON.stringify(s));
await shot('01-second-board');

let intentionallyMissed=false;let guard=0;
while((s=await state()).phase==='playing'&&guard++<12){
  const item=byId.get(s.problemId);if(!item)throw new Error('problem missing '+s.problemId);const p=parseProblem(item.prompt);
  if(!intentionallyMissed){
    const wrong=p.kind==='centroid'?grid((p.g[0]+1)%13,p.g[1]):linePoint(p.kind,(p.node+1)%p.max,p.max);
    if(p.kind==='centroid'){
      const A=grid(...p.a),B=grid(...p.b),C=grid(...p.c);await dragNorm(page,[(B[0]+C[0])/2,(B[1]+C[1])/2],A);await dragNorm(page,[(A[0]+C[0])/2,(A[1]+C[1])/2],B);
    }
    await dragNorm(page,stage(.5,.55),wrong);await sleep(1400);const afterWrong=await state();log.push({step:'general-wrong',problemId:s.problemId,state:afterWrong});
    if(afterWrong.firstMisses!==1||afterWrong.fuses!==2)throw new Error('wrong path not recorded '+JSON.stringify(afterWrong));
    await shot('02-one-wrong');intentionallyMissed=true;s=afterWrong;
  }
  if(p.kind==='centroid'){
    const now=await state();if(now.tensionLines<2){const A=grid(...p.a),B=grid(...p.b),C=grid(...p.c);if(now.tensionLines===0)await dragNorm(page,[(B[0]+C[0])/2,(B[1]+C[1])/2],A);const mid=await state();if(mid.tensionLines===1)await dragNorm(page,[(A[0]+C[0])/2,(A[1]+C[1])/2],B);}
    await dragNorm(page,stage(.5,.55),grid(...p.g));
  }else await dragNorm(page,stage(.5,.55),linePoint(p.kind,p.node,p.max));
  await sleep(1650);const after=await state();log.push({step:'correct',problemId:s.problemId,calculated:p,state:after});
  if(after.phase==='playing'&&after.solved===4)await shot('03-four-solved');
}
s=await state();await shot('04-clear');
const result={run_id:`prism-mobil-playthrough-${new Date().toISOString()}`,artifact_hash_sha256:artifactHash(),method:'practice and seven-board run by Chrome pointer drag only; answers calculated from visible prompt; no answerCorrect/answerWrong calls',errors,final:s,log,passed:s.phase==='clear'&&s.solved===7&&s.firstAttemptCorrect===6&&s.firstAttemptTotal===7&&errors.length===0};
fs.writeFileSync(path.join(HERE,'playthrough-results.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
await browser.close();await server.close();release();if(!result.passed)process.exitCode=1;
