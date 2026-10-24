import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url)), ROOT=path.resolve(HERE,'../../../../..');
const OUT=path.join(HERE,'pointer-playthrough');fs.mkdirSync(OUT,{recursive:true});
function chromePath(){
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  for(const b of fs.readdirSync(base).sort().reverse()){
    const p=path.join(base,b,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if(fs.existsSync(p))return p;
  }throw new Error('chrome missing');
}
const source=[[90,711],[202,596],[306,711]],target=[[90,369],[202,252],[306,369]];
async function drag(page,a,b){
  await page.mouse.move(a[0],a[1]);await page.mouse.down();await page.mouse.move((a[0]+b[0])/2,(a[1]+b[1])/2,{steps:8});await page.mouse.move(b[0],b[1],{steps:8});await page.mouse.up();await new Promise(r=>setTimeout(r,180));
}
const decode=n=>[Math.floor(n/100),Math.floor(n/10)%10,n%10];
const server=await serveStatic(path.join(ROOT,'public'));
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const result={consoleErrors:[],states:{}};
try{
  const page=await browser.newPage();await page.setViewport({width:390,height:844,deviceScaleFactor:1});
  page.on('console',m=>{if(m.type()==='error')result.consoleErrors.push(m.text());});
  await page.goto(`${server.url}/g/ge-jimkkun/`,{waitUntil:'networkidle2',timeout:45000});
  await page.waitForFunction('window.__GAME_TEST__?.ready===true',{timeout:30000});
  const bank=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(1000));
  const answers=new Map(bank.map(p=>[p.id,decode(p.answerNumeric)]));
  await page.screenshot({path:path.join(OUT,'00-title.png')});
  await page.mouse.click(195,574);await new Promise(r=>setTimeout(r,350));
  await page.screenshot({path:path.join(OUT,'01-practice-guide.png')});
  // 첫 A→F 성공 뒤 나머지 두 대응은 승무원 시범으로 완성되어 실전으로 넘어간다.
  await drag(page,source[0],target[2]);
  await page.waitForFunction('window.__GAME_TEST__.getState().currentProblemId!=="practice-fixed"',{timeout:5000});
  await page.screenshot({path:path.join(OUT,'02-first-run.png')});

  async function state(){return await page.evaluate(()=>window.__GAME_TEST__.getState());}
  async function solveCorrect(){
    const before=await state(),perm=answers.get(before.currentProblemId);if(!perm)throw new Error(`answer missing ${before.currentProblemId}`);
    for(let i=0;i<3;i++)await drag(page,source[i],target[perm[i]]);
    await page.waitForFunction(`(()=>{const s=window.__GAME_TEST__.getState();return s.solved>${before.solved}||s.phase==='clear'||s.phase==='gameover'})()`,{timeout:5000});
    await page.waitForFunction(`['playing','clear','gameover'].includes(window.__GAME_TEST__.getState().phase)`,{timeout:5000});
  }
  async function submitWrongThenRepair(){
    const before=await state(),p=answers.get(before.currentProblemId),wrong=[p[1],p[0],p[2]];
    for(let i=0;i<3;i++)await drag(page,source[i],target[wrong[i]]);
    await page.waitForFunction(`window.__GAME_TEST__.getState().lives===${before.lives-1}`,{timeout:4000});
    await page.screenshot({path:path.join(OUT,'03-wrong-evidence.png')});
    await page.waitForFunction(`window.__GAME_TEST__.getState().phase==='playing'`,{timeout:4000});
    await drag(page,target[wrong[0]],source[0]);
    await drag(page,target[wrong[1]],target[p[1]]);
    await drag(page,source[0],target[p[0]]);
    await page.waitForFunction(`window.__GAME_TEST__.getState().solved>${before.solved}`,{timeout:5000});
    await page.waitForFunction(`['playing','clear','gameover'].includes(window.__GAME_TEST__.getState().phase)`,{timeout:5000});
  }
  await submitWrongThenRepair();
  for(let i=1;i<8;i++)await solveCorrect();
  result.states.recovery=await state();await page.screenshot({path:path.join(OUT,'04-recovery-clear.png')});

  await page.evaluate(()=>window.__GAME_TEST__.start());await new Promise(r=>setTimeout(r,250));
  result.states.secondStart=await state();
  for(let i=0;i<8;i++)await solveCorrect();
  result.states.clean=await state();await page.screenshot({path:path.join(OUT,'05-clean-clear.png')});

  await page.evaluate(()=>window.__GAME_TEST__.start());await new Promise(r=>setTimeout(r,250));
  const doomed=await state(),badPerm=answers.get(doomed.currentProblemId),wrong=[badPerm[1],badPerm[0],badPerm[2]];
  for(let i=0;i<3;i++)await drag(page,source[i],target[wrong[i]]);
  await page.waitForFunction(`window.__GAME_TEST__.getState().phase==='playing'`,{timeout:4000});
  for(let loss=1;loss<3;loss++){
    await drag(page,target[wrong[0]],source[0]);await drag(page,source[0],target[wrong[0]]);
    if(loss<2)await page.waitForFunction(`window.__GAME_TEST__.getState().phase==='playing'`,{timeout:4000});
  }
  await page.waitForFunction(`window.__GAME_TEST__.getState().phase==='gameover'`,{timeout:5000});
  result.states.gameover=await state();await page.screenshot({path:path.join(OUT,'06-knots-gameover.png')});
  result.passed=result.consoleErrors.length===0&&result.states.recovery.phase==='clear'&&result.states.recovery.solved===8&&result.states.recovery.firstAttemptCorrect===7&&result.states.recovery.lives===2&&result.states.clean.phase==='clear'&&result.states.clean.firstAttemptCorrect===8&&result.states.gameover.phase==='gameover'&&result.states.gameover.lives===0;
  fs.writeFileSync(path.join(HERE,'pointer-playthrough-results.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result,null,2));if(!result.passed)process.exitCode=1;
}finally{await browser.close();await server.close();}
