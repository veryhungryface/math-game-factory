#!/usr/bin/env node
// 장면 점검 캡처: 타이틀→연습→드래그→정답 사건→오답 사건→끝 화면을 실제 pointer 로 찍는다. 인자: 폭 높이 [출력폴더]
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const W=+process.argv[2]||390,H=+process.argv[3]||844;
const OUT=process.argv[4]||`/tmp/poksin-look/${W}`;
fs.mkdirSync(OUT,{recursive:true});
const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
function chromePath(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const b of fs.readdirSync(base).sort().reverse()){const p=path.join(base,b,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');if(fs.existsSync(p))return p;}}
const server=await serveStatic(path.join(ROOT,'public'));
const browser=await puppeteer.launch({headless:'new',executablePath:chromePath(),args:['--no-sandbox','--mute-audio','--enable-unsafe-swiftshader']});
const page=await browser.newPage();await page.setViewport({width:W,height:H,deviceScaleFactor:1});
const errs=[];page.on('console',m=>{if(m.type()==='error')errs.push(m.text());});page.on('pageerror',e=>errs.push(String(e)));
await page.goto(`${server.url}/g/poksinhan-kkokdaegi/`,{waitUntil:'load',timeout:45000});
await page.waitForFunction(()=>window.__GAME_TEST__?.ready===true,{timeout:60000});await sleep(1500);
const shot=(n)=>page.screenshot({path:path.join(OUT,n+'.png')});
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
async function corner(k){const s=await state();return [s.cornerXPermille[k]/1000*W,s.cornerYPermille[k]/1000*H];}
async function drag(k,hold){const a=await corner(0),b=await corner(k);await page.mouse.move(a[0],a[1]);await page.mouse.down();await page.mouse.move(b[0],b[1],{steps:10});if(hold){await sleep(120);await shot(hold);}await page.mouse.up();}
await shot('00-title');
await page.mouse.click(W/2,H*.6);await sleep(1300);await shot('01-practice');
await drag(3,'02-drag-practice');await sleep(500);await shot('03-zip');await sleep(800);await shot('04-puff');await sleep(1600);
await shot('05-round1');
let s=await state();console.log(s.phase,s.round,s.onboarding);
await drag(6,'06-drag-round');await sleep(500);await shot('07-after-release');await sleep(2200);await shot('08-next');
// 층 쌓인 모습은 훅으로 빠르게.
for(let i=0;i<4;i++){await page.evaluate(()=>window.__GAME_TEST__.answerCorrect());await sleep(250);}
await sleep(400);await shot('09-tower');
await page.evaluate(()=>window.__GAME_TEST__.answerWrong());await sleep(450);await shot('10-wrong');await sleep(2500);await shot('11-after-wrong');
for(let i=0;i<3;i++){s=await state();if(s.phase!=='playing')break;await page.evaluate(()=>window.__GAME_TEST__.answerCorrect());await sleep(300);}
await sleep(2500);s=await state();console.log(s.phase,s.solved,s.firstCorrect,s.lives);await shot('12-end');
console.log('errors',errs.slice(0,5));
await browser.close();await server.close();
