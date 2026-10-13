#!/usr/bin/env node
// 2차 수정 검증: 실제 pointer 연습 완료 시간, 3개 가로 폭 UI, 모바일 타이머 안전영역 캡처.
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
const OUT=path.join(WORK,'validation/golem-hapche-fix2');
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

async function openPage(browser,server,width,height,errors){
  const page=await browser.newPage();await page.setViewport({width,height,deviceScaleFactor:1});
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(String(e)));
  await page.goto(`${server.url}/g/golem-hapche/`,{waitUntil:'load',timeout:45000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});return page;
}

async function drag(page,from,to){
  await page.mouse.move(from.x,from.y);await page.mouse.down();await sleep(80);
  await page.mouse.move((from.x+to.x)/2,(from.y+to.y)/2,{steps:5});await page.mouse.move(to.x,to.y,{steps:5});await sleep(80);await page.mouse.up();
}

fs.mkdirSync(OUT,{recursive:true});
const server=await serveStatic(PUBLIC);const errors=[];
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
try{
  const practice=await openPage(browser,server,390,844,errors);const t0=Date.now();
  await practice.mouse.click(195,422);await sleep(700);
  const practiceBefore=await practice.evaluate(()=>window.__GAME_TEST__.getState());
  await practice.screenshot({path:path.join(OUT,'practice-locked.png')});
  // B 카드 바깥 가장자리 부근에 놓아 연습 자석 스냅도 실제 pointer 경로로 검증한다.
  await drag(practice,{x:105,y:502},{x:200,y:420});await sleep(320);
  const reveal=await practice.evaluate(()=>window.__GAME_TEST__.getState());
  await practice.screenshot({path:path.join(OUT,'practice-math-reveal.png')});
  await sleep(2850);
  const firstContract=await practice.evaluate(()=>window.__GAME_TEST__.getState());
  const firstContractMs=Date.now()-t0;
  await practice.screenshot({path:path.join(OUT,'first-contract.png')});await practice.close();

  const layouts=[];
  for(const [width,height] of [[1280,800],[1440,900],[2000,1045]]){
    const page=await openPage(browser,server,width,height,errors);await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(450);
    const file=`layout-${width}.png`;await page.screenshot({path:path.join(OUT,file)});layouts.push({width,height,file});await page.close();
  }

  const timer=await openPage(browser,server,390,844,errors);await timer.evaluate(()=>window.__GAME_TEST__.start());await sleep(120);
  await timer.screenshot({path:path.join(OUT,'timer-90.png')});await sleep(29200);
  await timer.screenshot({path:path.join(OUT,'timer-61.png')});await sleep(52000);
  await timer.screenshot({path:path.join(OUT,'timer-9.png')});await timer.close();

  const result={runId:`golem-hapche-fix2-${new Date().toISOString()}`,artifactHashSha256:artifactHash(),practice:{before:practiceBefore,reveal,firstContract,firstContractMs},layouts,timerCaptures:['timer-90.png','timer-61.png','timer-9.png'],consoleErrors:errors};
  result.passed=practiceBefore.onboarding===true&&reveal.selectedShield===0&&reveal.targetShield===1&&reveal.score===120&&String(firstContract.problemId).startsWith('gh-run-')&&firstContractMs<15000&&errors.length===0;
  fs.writeFileSync(path.join(OUT,'results.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result,null,2));if(!result.passed)process.exitCode=1;
}finally{await browser.close();await server.close();}
