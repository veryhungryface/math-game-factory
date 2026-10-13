#!/usr/bin/env node
// 실제 pointer로 첫 오답 뒤 보호고리 2개 상태에서 6의뢰 clear에 도달하는 회복 경로를 검증한다.
import fs from 'node:fs'; import path from 'node:path'; import crypto from 'node:crypto'; import {fileURLToPath} from 'node:url'; import puppeteer from 'puppeteer'; import {serveStatic} from '../../../../lib/static-server.mjs';
const HERE=path.dirname(fileURLToPath(import.meta.url)), ROOT=path.resolve(HERE,'../../../../..'), PUBLIC=path.join(ROOT,'public');
const GAME=path.join(PUBLIC,'g/muneo-sajinsa');
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
function chrome(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const b of fs.readdirSync(base).sort().reverse())for(const rel of ['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-linux64/chrome']){const p=path.join(base,b,rel);if(fs.existsSync(p))return p;}throw new Error('chrome missing');}
function artifactHash(){const h=crypto.createHash('sha256');for(const n of ['index.html',...fs.readdirSync(path.join(GAME,'Build')).sort().map(x=>'Build/'+x)])h.update(fs.readFileSync(path.join(GAME,n)));return h.digest('hex');}
const server=await serveStatic(PUBLIC), browser=await puppeteer.launch({headless:true,executablePath:chrome(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const page=await browser.newPage(), errors=[]; await page.setViewport({width:390,height:844,deviceScaleFactor:1}); page.on('console',m=>{if(m.type()==='error')errors.push(m.text())}); page.on('pageerror',e=>errors.push(String(e)));
const outDir=path.join(HERE,'pointer-playthrough');fs.mkdirSync(outDir,{recursive:true});
const x=[55,148,242,336], sy=[540,616], slot=[[148,354],[242,354]], camera=[328,388], tear=[359,768];
async function drag(a,b){await page.mouse.move(...a);await page.mouse.down();await sleep(22);await page.mouse.move(...b,{steps:3});await sleep(22);await page.mouse.up();await sleep(28)}
async function tap(p){await page.mouse.move(...p);await page.mouse.down();await sleep(22);await page.mouse.up();await sleep(28)}
async function state(){return page.evaluate(()=>window.__GAME_TEST__.getState());}
async function start(){await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(90);}
async function values(group){
  const found=[];
  for(let j=0;j<4;j++){await tap([x[j],sy[group]]);const parts=(await state()).currentProps.split('|');const value=parts[group];if(value&&value!=='없음'&&!found.includes(value))found.push(value);}
  return found;
}
async function wearValue(group,value){
  for(let j=0;j<4;j++){await tap([x[j],sy[group]]);const parts=(await state()).currentProps.split('|');if(parts[group]===value)return;}
  throw new Error(`card not found group=${group} value=${value} state=${JSON.stringify(await state())}`);
}
async function photo(parts){for(const [g,value] of parts)await wearValue(g,value);await tap(camera);}
async function submit(){await drag(tear,[359,676]);await sleep(90)}
async function feedback(ms){await sleep(ms);}
async function cases(round){
  const left=await values(0),right=await values(1),out=[];
  if(round<2){for(const v of left)out.push([[0,v]]);for(const v of right)out.push([[1,v]]);return out;}
  for(const a of left)for(const b of right)if(round<4||(a!=='0'&&a!==b))out.push([[0,a],[1,b]]);
  return out;
}
async function solveRound(round){for(const parts of await cases(round))await photo(parts);await submit();await sleep(120);return state();}
try{
  await page.goto(`${server.url}/g/muneo-sajinsa/`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
  await start();await page.screenshot({path:path.join(outDir,'00-recovery-start.png')});
  for(let r=0;r<4;r++){const s=await solveRound(r);if(s.solved!==r+1)throw new Error(`recovery round${r+1} ${JSON.stringify(s)}`);await feedback(2100);}
  // 5번째 의뢰에서 첫 일반 실수를 만들고, 같은 의뢰를 다시 풀어도 clear가 가능한지 검증한다.
  const fifthLeft=await values(0),fifthRight=await values(1);await photo([[0,fifthLeft[1]],[1,fifthRight[0]]]);await submit();await sleep(120);const fifthWrong=await state();
  await page.screenshot({path:path.join(outDir,'01-fifth-round-first-error.png')});
  if(fifthWrong.solved!==4||fifthWrong.lives!==2||fifthWrong.firstAttemptCorrect!==4)throw new Error('fifth error '+JSON.stringify(fifthWrong));
  await feedback(1750);let s=await solveRound(4);if(s.solved!==5)throw new Error('fifth recovery '+JSON.stringify(s));await feedback(2100);
  s=await solveRound(5);if(s.solved!==6)throw new Error('sixth '+JSON.stringify(s));await sleep(2200);
  const recoveryFinal=await state();await page.screenshot({path:path.join(outDir,'02-recovery-clear.png')});

  // 같은 실제 포인터 전략을 익힌 두 번째 판에서 6/6 첫 제출 clear를 확인한다.
  await start();await page.screenshot({path:path.join(outDir,'03-second-run-start.png')});
  for(let r=0;r<6;r++){const n=await solveRound(r);if(n.solved!==r+1)throw new Error(`perfect round${r+1} ${JSON.stringify(n)}`);if(r<5)await feedback(2100);}
  await sleep(2200);const perfectFinal=await state();await page.screenshot({path:path.join(outDir,'04-second-run-clear.png')});
  const result={runId:`muneo-sajinsa-playthrough-${new Date().toISOString()}`,artifactHashSha256:artifactHash(),generatedAt:new Date().toISOString(),method:'start()는 타이틀 생략에만 사용. 이후 실제 trusted Puppeteer tap/drag만 사용; 카드 탭 착용, 렌즈 탭, 필름 드래그. answer hooks 미사용.',fifthWrong,recoveryFinal,perfectFinal,consoleErrors:errors,passed:fifthWrong.solved===4&&recoveryFinal.phase==='clear'&&recoveryFinal.firstAttemptCorrect===5&&recoveryFinal.lives===2&&perfectFinal.phase==='clear'&&perfectFinal.firstAttemptCorrect===6&&perfectFinal.lives===3&&errors.length===0};
  fs.writeFileSync(path.join(HERE,'pointer-playthrough-results.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result,null,2));if(!result.passed)process.exitCode=1;
}finally{await browser.close();await server.close();}
