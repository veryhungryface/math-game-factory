#!/usr/bin/env node
// 동일 WebGL 산출물을 trusted Chrome mouse down/move/up만으로 두 판 완주한다.
// answerCorrect/answerWrong 훅은 호출하지 않는다. 문제 은행은 정답 상태를 독립 대조하는 데만 쓰고,
// 모든 봉합과 종줄 제출은 실제 캔버스 좌표의 브라우저 입력으로 수행한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const PUBLIC=path.join(ROOT,'public');
const FRAMES=path.join(HERE,'pointer-playthrough');
fs.mkdirSync(FRAMES,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));

function chromePath(){
  if(process.env.PUPPETEER_EXECUTABLE_PATH)return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  for(const build of fs.readdirSync(base).sort().reverse()){
    const p=path.join(base,build,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if(fs.existsSync(p))return p;
  }
  throw new Error('Chrome for Testing not found');
}
async function state(page){return page.evaluate(()=>window.__GAME_TEST__.getState());}
async function waitFor(page,predicate,timeout=7000){
  const end=Date.now()+timeout;let s;
  while(Date.now()<end){s=await state(page);if(predicate(s))return s;await sleep(60);}
  throw new Error(`state timeout: ${JSON.stringify(s)}`);
}
async function drag(page,a,b){
  await page.mouse.move(a[0],a[1]);await sleep(120);await page.mouse.down();await sleep(100);
  await page.mouse.move((a[0]+b[0])/2,(a[1]+b[1])/2,{steps:5});
  await page.mouse.move(b[0],b[1],{steps:5});await sleep(120);await page.mouse.up();await sleep(220);
}
function parseAnswer(answer){
  const m=answer.match(/^(왼쪽|위쪽|오른쪽) 창 · A↔([DEF]), B↔([DEF]), C↔([DEF])$/);
  if(!m)throw new Error(`answer parse failed: ${answer}`);
  return {candidate:{왼쪽:0,위쪽:1,오른쪽:2}[m[1]],map:[m[2],m[3],m[4]].map(x=>x.charCodeAt(0)-68)};
}
function xy(array,index){return [array[index*2],array[index*2+1]];}
async function laceAndSubmit(page,s,answer){
  for(let source=0;source<3;source++){
    let accepted=false;
    for(let retry=0;retry<3&&!accepted;retry++){
      s=await state(page);
      await drag(page,xy(s.sourcePx,source),xy(s.candidatePx,answer.candidate*3+answer.map[source]));
      const after=await state(page);
      accepted=after.selectedCount>=source+1;
    }
    if(!accepted)throw new Error(`lace ${source} not accepted: ${JSON.stringify(await state(page))}`);
  }
  s=await state(page);
  if(s.selectedCount!==3)throw new Error(`three laces not accepted: ${JSON.stringify(s)}`);
  const rope=xy(s.ropePx,0);await drag(page,rope,[rope[0],rope[1]+96]);
}

const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const errors=[],events=[];
try{
  const page=await browser.newPage();
  await page.setViewport({width:390,height:844,deviceScaleFactor:1});
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  page.on('pageerror',e=>errors.push(String(e)));
  await page.goto(`${server.url}/g/yurinabang-gyeolsok/`,{waitUntil:'networkidle2',timeout:45000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});
  const problems=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(400));
  const answers=new Map(problems.map(p=>[p.id,parseAnswer(p.answer)]));
  if(problems.length!==384)throw new Error(`bank size ${problems.length}`);
  await page.screenshot({path:path.join(FRAMES,'00-title.png')});

  async function practice(tag){
    await page.mouse.click(195,422); // 접근성 탭 → 나방이 종줄을 당긴다.
    let s=await waitFor(page,x=>x.onboarding&&x.problemId==='lace-practice');
    await sleep(420);s=await state(page);
    await page.screenshot({path:path.join(FRAMES,`${tag}-practice.png`)});
    await laceAndSubmit(page,s,{candidate:1,map:[0,1,2]});
    s=await waitFor(page,x=>x.onboarding&&x.frozen&&x.score>0);
    events.push({name:`${tag}-practice-real-pointer`,score:s.score,selectedCount:s.selectedCount});
    await waitFor(page,x=>!x.onboarding&&!x.frozen&&x.floor===1,6000);
    await sleep(420); // 문제 교체 뒤 RectTransform 좌표가 다음 브리지 푸시에 반영될 때까지 기다린다.
    return state(page);
  }

  async function play(tag,injectWrong){
    let s=await practice(tag),wrongDone=false;
    await page.screenshot({path:path.join(FRAMES,`${tag}-floor1.png`)});
    while(s.phase==='playing'){
      const before={id:s.problemId,solved:s.solved,lives:s.lives,first:s.firstAttemptCorrect};
      const right=answers.get(s.problemId);if(!right)throw new Error(`missing bank answer ${s.problemId}`);
      if(injectWrong&&!wrongDone){
        const wrong={candidate:(right.candidate+1)%3,map:right.map};
        await laceAndSubmit(page,s,wrong);
        s=await waitFor(page,x=>x.frozen&&x.lives===before.lives-1,3000);
        events.push({name:`${tag}-first-wrong-real-pointer`,before,after:{lives:s.lives,cracks:s.cracks,firstAttemptCorrect:s.firstAttemptCorrect},misconceptionId:s.misconceptionId});
        await page.screenshot({path:path.join(FRAMES,`${tag}-wrong-evidence.png`)});
        s=await waitFor(page,x=>!x.frozen&&x.problemId===before.id,5000);
        await sleep(320);s=await state(page);
        wrongDone=true;
        continue;
      }
      await laceAndSubmit(page,s,right);
      s=await waitFor(page,x=>x.frozen&&(x.solved===before.solved+1||x.phase==='clear'),3000);
      events.push({name:`${tag}-correct-real-pointer`,problemId:before.id,solved:s.solved,lives:s.lives});
      if(s.solved===1)await page.screenshot({path:path.join(FRAMES,`${tag}-correct-assembly.png`)});
      if(s.phase==='clear')break;
      s=await waitFor(page,x=>x.phase==='clear'||(!x.frozen&&x.problemId!==before.id),5000);
      if(s.phase!=='clear'){await sleep(320);s=await state(page);}
    }
    await page.screenshot({path:path.join(FRAMES,`${tag}-end.png`)});
    return s;
  }

  const normal=await play('run1',false);
  if(normal.phase!=='clear'||normal.solved!==8||normal.lives!==3||normal.firstAttemptCorrect!==8)throw new Error(`normal clear failed: ${JSON.stringify(normal)}`);
  const recovery=await play('run2',true);
  if(recovery.phase!=='clear'||recovery.solved!==8||recovery.lives<2||recovery.cracks!==1||recovery.firstAttemptCorrect!==7)throw new Error(`recovery clear failed: ${JSON.stringify(recovery)}`);

  const result={
    generatedAt:new Date().toISOString(),viewport:'390x844',
    method:'trusted Chrome mouse down/move/up on source vertices, candidate vertices, and bell rope; no answerCorrect/answerWrong calls',
    normal:{phase:normal.phase,solved:normal.solved,lives:normal.lives,firstAttemptCorrect:normal.firstAttemptCorrect,firstAttemptTotal:normal.firstAttemptTotal},
    recovery:{phase:recovery.phase,solved:recovery.solved,lives:recovery.lives,firstAttemptCorrect:recovery.firstAttemptCorrect,firstAttemptTotal:recovery.firstAttemptTotal},
    events,consoleErrors:errors,passed:errors.length===0
  };
  fs.writeFileSync(path.join(HERE,'pointer-playthrough-results.json'),JSON.stringify(result,null,2)+'\n');
  console.log(JSON.stringify({...result,events:`${events.length} events`},null,2));
  if(!result.passed)process.exitCode=1;
}finally{
  await browser.close();await server.close();
}
