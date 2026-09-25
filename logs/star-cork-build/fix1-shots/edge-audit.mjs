import path from 'node:path';import {pathToFileURL} from 'node:url';import puppeteer from '/Users/sitpo/math-game-factory/logs/star-cork-build/puppeteer-pipe.mjs';
const url=pathToFileURL(path.resolve('public/g/star-cork/index.html')).href;
const b=await puppeteer.launch();const page=await b.newPage();
const out=[];
for(const [w,h] of [[390,844],[1280,800],[820,1180]]){
  await page.setViewport({width:w,height:h,deviceScaleFactor:1,isMobile:w<900,hasTouch:true});
  await page.goto(url,{waitUntil:'load'});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready);
  await page.evaluate(()=>{window.__GAME_TEST__.start();window.__GAME_TEST__.answerCorrect();});
  await new Promise(r=>setTimeout(r,600));
  out.push(await page.evaluate(()=>{
    const L=window.__STAR_UI__?.getLayout?.()||{};
    const stage=document.getElementById('stage').getBoundingClientRect();
    const rect=id=>{const e=document.getElementById(id);if(!e||e.hidden)return null;const r=e.getBoundingClientRect();return{l:Math.round(r.left-stage.left),r:Math.round(r.right-stage.left),t:Math.round(r.top-stage.top)};};
    const land=document.documentElement.classList.contains('land');
    const cw=land?36:24;
    return {vw:innerWidth,vh:innerHeight,land,stageW:Math.round(stage.width),cw,post:cw+6,
      hud:rect('hud'),goal:rect('goal'),
      hudChildren:[...document.querySelectorAll('#hud .stat')].map(e=>{const r=e.getBoundingClientRect();return[Math.round(r.left-stage.left),Math.round(r.right-stage.left)];}),
      layout:L};
  }));
}
await b.close();
console.log(JSON.stringify(out,null,1).slice(0,4000));
