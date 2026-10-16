#!/usr/bin/env node
import fs from 'node:fs';
import crypto from 'node:crypto';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';
import {acquireMachineLock} from '../../../../lib/machine-lock.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const PUBLIC=path.join(ROOT,'public');
const GAME=path.join(PUBLIC,'g','gopbaegi-bbangjip');
const OUT=path.join(HERE,'math-boundary-results.json');
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));

function chromePath(){
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  if(!fs.existsSync(base))return undefined;
  for(const build of fs.readdirSync(base).sort().reverse())for(const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing']){
      const p=path.join(base,build,rel);if(fs.existsSync(p))return p;
    }
}

function artifactHash(){
  const rels=['index.html',...fs.readdirSync(path.join(GAME,'Build')).sort().map(name=>`Build/${name}`)];
  const h=crypto.createHash('sha256');
  for(const rel of rels){h.update(rel);h.update(fs.readFileSync(path.join(GAME,rel)));}
  return h.digest('hex');
}

async function openPractice(browser,url,errors){
  const page=await browser.newPage();
  page.on('console',message=>{if(message.type()==='error')errors.push(message.text());});
  page.on('pageerror',error=>errors.push(String(error)));
  await page.setViewport({width:390,height:844,deviceScaleFactor:1});
  await page.goto(url,{waitUntil:'load',timeout:45000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
  await page.mouse.move(281,713);await page.mouse.down();await sleep(110);await page.mouse.up();
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().onboarding,{timeout:5000});
  return page;
}

async function releaseAt(page,value){
  await page.mouse.move(281,713);await page.mouse.down();
  await page.waitForFunction(expected=>window.__GAME_TEST__.getState().poured>=expected,{timeout:30000},value);
  await page.mouse.up();
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().actionPhase==='reveal',{timeout:5000});
  return page.evaluate(()=>window.__GAME_TEST__.getState());
}

const release=await acquireMachineLock('gpu',{label:'gopbaegi-bbangjip 정확 판정 경계값'});
const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio','--use-gl=angle','--use-angle=metal']});
let exitCode=0;
try{
  const hashBefore=artifactHash(),errors=[],cases=[];
  for(const requested of [195,199,200,201,205]){
    const page=await openPractice(browser,`${server.url}/g/gopbaegi-bbangjip/`,errors);
    const state=await releaseAt(page,requested);
    cases.push({requested,actual_release:state.poured,accepted:state.solved===1,misconceptionId:state.misconceptionId,actionPhase:state.actionPhase});
    await page.close();
  }
  const hashAfter=artifactHash();
  const passed=hashBefore===hashAfter&&errors.length===0&&cases.every(item=>item.actual_release===item.requested&&item.accepted===(item.requested===200));
  const result={run_id:`${path.basename(process.env.MGF_WORK||'20261017-000511-L3')}-fix1-boundaries-${new Date().toISOString()}`,
    artifact_hash:hashBefore,generated_at:new Date().toISOString(),
    method:'고정 연습 target=200 cm³를 매번 새로 열고 실제 Chrome mouse down/up으로 195, 199, 200, 201, 205에서 놓았다. 훅은 상태 관찰에만 썼다.',
    cases,consoleErrors:errors,passed};
  fs.writeFileSync(OUT,JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result,null,2));
  if(!passed)exitCode=1;
}catch(error){console.error(error.stack||error);exitCode=1;}
finally{await browser.close();await server.close();await release();}
process.exit(exitCode);
