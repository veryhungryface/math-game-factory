#!/usr/bin/env node
// B3 무뇌 입력 4종×200판. start()는 판 진입에만 쓰고 제출은 실제 mouse drag다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const PUBLIC=path.join(ROOT,'public');
const W=390,H=844,GAMES=200,CHANCE=1/24;
const RUN_ID=process.env.MGF_RUN_ID||`yangmal-b3-${new Date().toISOString()}`;
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
function chromePath(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const b of fs.readdirSync(base).sort().reverse())for(const rel of ['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-linux64/chrome']){const p=path.join(base,b,rel);if(fs.existsSync(p))return p;}throw new Error('Chrome not found');}
function artifactHash(){const game=path.join(PUBLIC,'g/yangmal-daetalchul'),h=crypto.createHash('sha256');for(const n of ['index.html',...fs.readdirSync(path.join(game,'Build')).sort().map(x=>'Build/'+x)]){h.update(n);h.update(fs.readFileSync(path.join(game,n)));}return h.digest('hex');}
async function state(page){return page.evaluate(()=>window.__GAME_TEST__.getState());}
async function waitFor(page,pred,label,timeout=2500){const t=Date.now();while(Date.now()-t<timeout){const s=await state(page);if(pred(s))return s;await sleep(12);}throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);}
const bp=(x,y)=>[x,H-y];
async function submit(page,cm){let s=await state(page);const from=bp(s.handleX,s.handleY);const y=s.rulerMin+(s.rulerMax-s.rulerMin)*(cm-1)/23;await page.mouse.move(...from);await sleep(32);await page.mouse.down();await sleep(48);await page.mouse.move(...bp(s.handleX,y),{steps:4});await sleep(48);await page.mouse.up();await sleep(32);return waitFor(page,x=>x.phase==='reveal',`submit ${cm}`);}
function balanced(seed){let x=seed>>>0,bag=[],i=0;return()=>{if(i>=bag.length){bag=Array.from({length:24},(_,k)=>k+1);for(let j=23;j>0;j--){x^=x<<13;x^=x>>>17;x^=x<<5;const k=(x>>>0)%(j+1);[bag[j],bag[k]]=[bag[k],bag[j]];}i=0;}return bag[i++];};}
async function policy(page,name,choose){let attempts=0,correct=0,completed=0,progress=0;for(let g=0;g<GAMES;g++){await page.evaluate(()=>window.__GAME_TEST__.start());const before=await waitFor(page,s=>s.phase==='playing'&&s.round===1,'start');const after=await submit(page,choose(g));attempts+=after.firstAttempted;correct+=after.firstCorrect;progress+=after.solved;if(after.phase==='clear')completed++;if(after.inputCount<1)throw new Error('실제 입력 미기록');}return{bot:name,games:GAMES,firstAttempts:attempts,firstCorrect:correct,firstAttemptRate:correct/attempts,chance:CHANCE,completionRate:completed/GAMES,progress,passed:attempts===GAMES&&correct/attempts<=CHANCE&&completed===0};}
async function idle(page){let progress=0,attempts=0;for(let g=0;g<GAMES;g++){await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(8);const s=await state(page);progress+=s.solved;attempts+=s.firstAttempted;}return{bot:'idle',games:GAMES,firstAttempts:attempts,firstCorrect:0,firstAttemptRate:0,chance:CHANCE,completionRate:0,progress,passed:attempts===0&&progress===0};}
const server=await serveStatic(PUBLIC);const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),protocolTimeout:900000,args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const errors=[];
try{const page=await browser.newPage();await page.setViewport({width:W,height:H,deviceScaleFactor:1});page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});page.on('pageerror',e=>errors.push(String(e)));await page.goto(`${server.url}/g/yangmal-daetalchul/`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});const rand=balanced(4);const results=[await policy(page,'repeat',()=>1),await policy(page,'cycle',g=>g%24+1),await policy(page,'random',()=>rand()),await idle(page)];const out={run_id:RUN_ID,artifact_hash:artifactHash(),viewport:`${W}x${H}`,method:'start() only enters a run; all 600 submitted answers use trusted mouse down/move/up on the Unity handle',criterion:'each first-attempt rate <= 1/24; idle progress=0',results,console_errors:errors,passed:results.every(x=>x.passed)&&errors.length===0};fs.writeFileSync(path.join(HERE,'b3-pointer-results.json'),JSON.stringify(out,null,2)+'\n');console.log(JSON.stringify(out,null,2));if(!out.passed)process.exitCode=1;await page.close();}finally{await browser.close();await server.close();}
