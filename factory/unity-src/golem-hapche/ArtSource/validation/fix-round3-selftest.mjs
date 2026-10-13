#!/usr/bin/env node
// 3차 수정 검증: 연습 탭→탭, 820×1180 태블릿, 마지막 두 계약 숙련 게이트, AA 순서 판정.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const PUBLIC=path.join(ROOT,'public');
const GAME=path.join(PUBLIC,'g/golem-hapche');
const WORK=path.resolve(ROOT,process.env.MGF_WORK||'factory/work-lanes/20261013-055311-L2');
const OUT=path.join(WORK,'validation/golem-hapche-fix3');
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));

function chromePath(){
  if(process.env.PUPPETEER_EXECUTABLE_PATH)return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  for(const build of fs.readdirSync(base).sort().reverse())for(const rel of[
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-linux64/chrome']){
    const candidate=path.join(base,build,rel);if(fs.existsSync(candidate))return candidate;
  }
  throw new Error('Chrome for Testing not found');
}

function artifactHash(){
  const files=[];(function walk(dir){for(const name of fs.readdirSync(dir).sort()){const file=path.join(dir,name);fs.statSync(file).isDirectory()?walk(file):files.push(file);}})(GAME);
  const hash=crypto.createHash('sha256');for(const file of files){hash.update(path.relative(GAME,file));hash.update(fs.readFileSync(file));}return hash.digest('hex');
}

function pairByIndex(index){let n=0;for(let a=0;a<6;a++)for(let b=a+1;b<6;b++,n++)if(n===index)return[a,b];throw new Error(`bad pair ${index}`);}
function shieldPoint(index){const col=index%2,row=Math.floor(index/2);return{x:390*(col===0?.27:.73),y:844*(1-(.485-row*.18))};}
async function tap(page,p){await page.mouse.move(p.x,p.y);await page.mouse.down();await sleep(70);await page.mouse.up();}
async function dragPair(page,pairIndex){const[a,b]=pairByIndex(pairIndex),from=shieldPoint(a),to=shieldPoint(b);await page.mouse.move(from.x,from.y);await page.mouse.down();await sleep(45);await page.mouse.move(to.x,to.y,{steps:6});await sleep(45);await page.mouse.up();}
function correctPair(runSerial,contract){return((runSerial*7+contract*5+contract*contract)%15+15)%15;}

async function openPage(browser,server,width,height,errors,deviceScaleFactor=1){
  const page=await browser.newPage();await page.setViewport({width,height,deviceScaleFactor,isMobile:height>width,hasTouch:height>width});
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(String(e)));
  await page.goto(`${server.url}/g/golem-hapche/`,{waitUntil:'load',timeout:45000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});return page;
}

function auditBank(bank){
  const errors=[];let aaCount=0;
  for(const problem of bank){
    if(problem.prompt.includes('비끼인각'))errors.push(`${problem.id}: 비표준 조어`);
    if(!problem.id.endsWith('-aa'))continue;
    aaCount++;const cards=[];
    for(const segment of problem.prompt.split(' / ').slice(-6)){
      const match=segment.match(/([A-F])=△[A-Z]{3} ∠[A-Z]=(\d+)° · ∠[A-Z]=(\d+)°/);
      if(match)cards.push({letter:match[1],a1:Number(match[2]),a2:Number(match[3])});
    }
    const matches=[];for(let i=0;i<cards.length;i++)for(let j=i+1;j<cards.length;j++)if(cards[i].a1===cards[j].a1&&cards[i].a2===cards[j].a2)matches.push(`${cards[i].letter}와 ${cards[j].letter}`);
    if(cards.length!==6||matches.length!==1||matches[0]!==problem.answer)errors.push(`${problem.id}: 순서 대응 정답 ${JSON.stringify(matches)} / ${problem.answer}`);
  }
  return{bankCount:bank.length,aaCount,errors,passed:bank.length===360&&aaCount===72&&errors.length===0};
}

async function completeRun(page,runSerial,wrongContract=-1){
  await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(150);const inputs=[];
  for(let contract=0;contract<6;contract++){
    const correct=correctPair(runSerial,contract);
    if(contract===wrongContract){const wrong=(correct+1)%15;await dragPair(page,wrong);await sleep(1550);inputs.push({contract:contract+1,pair:wrong,result:'wrong'});await page.screenshot({path:path.join(OUT,`goal-after-wrong-run${runSerial}.png`)});}
    if(contract===4&&wrongContract<0)await page.screenshot({path:path.join(OUT,`goal-last-two-run${runSerial}.png`)});
    await dragPair(page,correct);await sleep(contract===5?1900:1750);inputs.push({contract:contract+1,pair:correct,result:'correct'});
  }
  return{runSerial,wrongContract,inputs,state:await page.evaluate(()=>window.__GAME_TEST__.getState())};
}

fs.mkdirSync(OUT,{recursive:true});
const server=await serveStatic(PUBLIC);const errors=[];
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
try{
  const practice=await openPage(browser,server,390,844,errors);const t0=Date.now();
  await tap(practice,{x:195,y:422});await sleep(650);
  await tap(practice,{x:105,y:502});await sleep(160);
  const afterFirstTap=await practice.evaluate(()=>window.__GAME_TEST__.getState());
  await tap(practice,{x:285,y:502});await sleep(2850);
  const firstContract=await practice.evaluate(()=>window.__GAME_TEST__.getState());const firstContractMs=Date.now()-t0;
  await practice.screenshot({path:path.join(OUT,'tap-tap-first-contract.png')});await practice.close();

  const tablet=await openPage(browser,server,820,1180,errors,2);await tablet.evaluate(()=>window.__GAME_TEST__.start());await sleep(500);
  await tablet.screenshot({path:path.join(OUT,'tablet-820x1180.png')});await tablet.close();

  const mastery=await openPage(browser,server,390,844,errors);
  const lateWrong=await completeRun(mastery,1,4);await mastery.screenshot({path:path.join(OUT,'late-wrong-mastery-fail.png')});
  const perfect=await completeRun(mastery,2,-1);await mastery.screenshot({path:path.join(OUT,'perfect-clear.png')});
  const bank=await mastery.evaluate(()=>window.__GAME_TEST__.sampleProblems(1000));await mastery.close();
  const mathAudit=auditBank(bank);

  const result={runId:`golem-hapche-fix3-${new Date().toISOString()}`,artifactHashSha256:artifactHash(),practice:{afterFirstTap,firstContract,firstContractMs},tablet:{viewport:'820x1180@2',file:'tablet-820x1180.png'},mastery:{lateWrong,perfect},mathAudit,consoleErrors:errors};
  result.passed=afterFirstTap.onboarding===true&&afterFirstTap.selectedShield>=0&&String(firstContract.problemId).startsWith('gh-run-')&&firstContractMs<15000&&lateWrong.state.phase==='gameover'&&lateWrong.state.endReason==='mastery'&&lateWrong.state.firstAttemptCorrect===5&&lateWrong.state.lastTwoFirstTry===1&&perfect.state.phase==='clear'&&perfect.state.firstAttemptCorrect===6&&perfect.state.lastTwoFirstTry===2&&mathAudit.passed&&errors.length===0;
  fs.writeFileSync(path.join(OUT,'results.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result,null,2));if(!result.passed)process.exitCode=1;
}finally{await browser.close();await server.close();}
