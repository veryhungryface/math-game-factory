#!/usr/bin/env node
// 실제 Chrome pointer 무뇌 봇. 문제를 읽지 않는 4정책(+무입력)의 첫 제출 정답률을 우연 1/8 과 비교한다.
// 인자: 판수(기본 40). 연습은 두 번 탭 경로로 지나가며 통계에 들어가지 않는다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';
const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const RUNS=+process.argv[2]||40,W=390,H=844;
const sleep=(ms)=>new Promise(r=>setTimeout(r,ms));
function chromePath(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const b of fs.readdirSync(base).sort().reverse()){const p=path.join(base,b,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');if(fs.existsSync(p))return p;}}
const server=await serveStatic(path.join(ROOT,'public'));
const browsers=[];
const launch=async()=>{const b=await puppeteer.launch({headless:'new',executablePath:chromePath(),protocolTimeout:600000,args:['--no-sandbox','--mute-audio','--disable-background-timer-throttling','--disable-renderer-backgrounding','--disable-backgrounding-occluded-windows']});browsers.push(b);return b;};
async function open(){
  const browser=await launch();const page=await browser.newPage();await page.setViewport({width:W,height:H});
  await page.goto(`${server.url}/g/poksinhan-kkokdaegi/`,{waitUntil:'load',timeout:60000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready===true,{timeout:90000});await sleep(800);return page;
}
const state=(p)=>p.evaluate(()=>{const s=window.__GAME_TEST__.getState();return {phase:s.phase,round:s.round,solved:s.solved,lives:s.lives,fc:s.firstCorrect,fa:s.firstAttempts,onboarding:s.onboarding,cx:s.cornerXPermille,cy:s.cornerYPermille};});
const at=(s,k)=>[s.cx[k]/1000*W,s.cy[k]/1000*H];
async function pointerDrag(p,s,path){const a=at(s,0);await p.mouse.move(a[0],a[1]);await p.mouse.down();for(const k of path){const b=at(s,k);await p.mouse.move(b[0],b[1],{steps:4});}await p.mouse.up();}
async function playPolicy(name,pick){
  const page=await open();let attempts=0,correct=0,clears=0,runs=0,subs=0;
  await page.mouse.click(W/2,H*.7);await sleep(1000);                 // 타이틀→연습
  await page.mouse.click(W/2,H*.5);await sleep(200);await page.mouse.click(W/2,H*.5);await sleep(900); // 두 번 탭 연습 경로
  await page.mouse.click(W/2,H*.95);await sleep(300);
  let n=0;
  while(runs<RUNS){
    const s=await state(page);
    if(s.phase==='clear'||s.phase==='gameover'){
      attempts+=s.fa;correct+=s.fc;if(s.phase==='clear')clears++;runs++;
      await sleep(550);await page.mouse.click(W/2,H*.95);await sleep(350);n=0;continue;
    }
    if(s.onboarding){await sleep(200);continue;}
    await pointerDrag(page,s,pick(n++,runs));subs++;
    await sleep(800);await page.mouse.click(W/2,H*.95);await sleep(150); // 근거 패널 건너뛰기
  }
  await page.close();
  const rate=attempts?correct/attempts:0,z=1.96,den=1+z*z/attempts,centre=(rate+z*z/(2*attempts))/den,half=z*Math.sqrt(rate*(1-rate)/attempts+z*z/(4*attempts*attempts))/den;
  return {name,runs,submissions:subs,firstAttempts:attempts,firstCorrect:correct,firstAttemptRate:+rate.toFixed(4),wilson95:[+(centre-half).toFixed(3),+(centre+half).toFixed(3)],clears};
}
const rnd=()=>1+Math.floor(Math.random()*8);
const policies=[
  ['고정 4칸',()=>[4]],
  ['순환 1→8',(n)=>[1+(n%8)]],
  ['무작위 1~8',()=>[rnd()]],
  ['왕복 훑기 후 무작위 놓기',()=>[1,2,3,4,5,6,7,8,7,6,5,4,3,2,1,rnd()]]
];
const rows=await Promise.all(policies.map(([n,f])=>playPolicy(n,f)));
// 무입력: 시간이 끝나도 진도 0 이어야 한다.
const idle=await open();await idle.mouse.click(W/2,H*.7);await sleep(1000);await idle.mouse.click(W/2,H*.5);await sleep(200);await idle.mouse.click(W/2,H*.5);await sleep(3000);
await sleep(123000);const end=await state(idle);await idle.close();
const result={chance:1/8,method:'real Chrome pointer; 문제·정답을 읽지 않음',rows,noInput:{phase:end.phase,solved:end.solved,firstAttempts:end.fa},checked_at:new Date().toISOString()};
fs.writeFileSync(path.join(HERE,'pointer-bot-results.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify(result,null,1));
for(const b of browsers)await b.close();await server.close();
