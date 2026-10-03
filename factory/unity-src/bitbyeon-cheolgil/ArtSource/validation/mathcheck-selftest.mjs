// 빌드된 WebGL이 내보낸 360문항을 독립 정수 계산으로 전수 검산한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname);
const PUBLIC=path.join(ROOT,'public'), GAME=path.join(PUBLIC,'g/bitbyeon-cheolgil');
const triples=[[3,4,5],[5,12,13],[6,8,10],[7,24,25],[8,15,17],[9,12,15],[9,40,41],[10,24,26],[11,60,61],[12,16,20],[12,35,37],[13,84,85],[14,48,50],[15,20,25],[15,36,39],[15,112,113],[16,30,34],[16,63,65],[18,24,30],[18,80,82],[20,21,29],[20,48,52],[20,99,101],[21,28,35],[21,72,75],[22,120,122],[24,32,40],[24,45,51],[24,70,74],[25,60,65]];
function resolveChrome(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');if(!fs.existsSync(base))return;for(const b of fs.readdirSync(base).sort().reverse()){const p=path.join(base,b,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');if(fs.existsSync(p))return p;}}
function hash(){const files=[];(function walk(d){for(const f of fs.readdirSync(d).sort()){const p=path.join(d,f);fs.statSync(p).isDirectory()?walk(p):files.push(p);}})(GAME);const h=crypto.createHash('sha256');for(const f of files){h.update(path.relative(GAME,f));h.update(fs.readFileSync(f));}return h.digest('hex');}
const server=await serveStatic(PUBLIC),browser=await puppeteer.launch({headless:true,executablePath:resolveChrome(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const page=await browser.newPage();await page.goto(`${server.url}/g/bitbyeon-cheolgil/`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
const bank=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(360));
const errors=[],seen=new Set();
for(const p of bank){
  const n=Number(String(p.id).replace('bank-',''))-1,row=n%30,style=Math.floor(n/30),[a,b,c]=triples[row];
  const hyp=c<=25&&style%3!==2;let known=hyp?0:(style%2===0?a:b);let answer=hyp?c:(known===a?b:a);
  if(!hyp&&(answer<4||answer>25)){known=known===a?b:a;answer=known===a?b:a;}
  if(a*a+b*b!==c*c)errors.push(`${p.id}:tuple`);
  if(Number(p.answer)!==answer||Number(p.answerNumeric)!==answer)errors.push(`${p.id}:answer ${p.answer}/${p.answerNumeric} != ${answer}`);
  if(answer<4||answer>25)errors.push(`${p.id}:range ${answer}`);
  if(!Array.isArray(p.choices)||p.choices.length!==22||!p.choices.map(String).includes(String(answer)))errors.push(`${p.id}:choices`);
  if(/[√]|근호/.test(p.prompt))errors.push(`${p.id}:middle-grade-radical`);
  if(/의 자리에서\s*(올림|버림|반올림)/.test(p.prompt))errors.push(`${p.id}:AT_PLACE`);
  let required=hyp?[a,b]:[c,known];
  if(style===4)required=hyp?[a*a,b*b]:[c*c,known*known];
  if(style===8)required=hyp?[a,b,c*c]:[known,c*c];
  for(const v of required)if(!String(p.prompt).includes(String(v)))errors.push(`${p.id}:prompt-missing-${v}`);
  if(!hyp&&style===5&&!/벽은 바닥과 수직/.test(p.prompt))errors.push(`${p.id}:ladder-right-angle-omitted`);
  if(!hyp&&style===6&&!/직각삼각형의 빗변/.test(p.prompt))errors.push(`${p.id}:missing-leg-right-angle-omitted`);
  seen.add(String(p.prompt).replace(/\s+/g,' ').trim());
}
const result={run_id:`bitbyeon-mathcheck-${new Date().toISOString()}`,artifact_hash_sha256:hash(),method:'built WebGL sampleProblems(360), independent integer tuple/model verification',verified_count:bank.length,unique_core_prompts:seen.size,errors,passed:bank.length===360&&seen.size===360&&errors.length===0};
fs.writeFileSync(new URL('./mathcheck-results.json',import.meta.url),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result,null,2));
await page.close();await browser.close();await server.close();if(!result.passed)process.exitCode=1;
