import puppeteer from '/Users/sitpo/math-game-factory/node_modules/puppeteer/lib/esm/puppeteer/puppeteer.js';
import { serveStatic } from '/Users/sitpo/math-game-factory/factory/lib/static-server.mjs';
const server = await serveStatic('/Users/sitpo/math-game-factory/public');
const browser = await puppeteer.launch({ executablePath: process.env.PUPPETEER_EXECUTABLE_PATH, headless: true, protocolTimeout: 180000, args:['--use-angle=metal'] });
const strat = process.argv[2]; const games = +process.argv[3]||1; const W=390,H=844;
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const page = await browser.newPage(); await page.setViewport({width:W,height:H,isMobile:false});
const errs=[]; page.on('pageerror',e=>errs.push(String(e)));
await page.goto(`${server.url}/g/hyeopgok-sasu/`,{waitUntil:'load'});
await page.waitForFunction(()=>window.__GAME_TEST__&&window.__GAME_TEST__.ready,{timeout:90000});
const bank = await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(9999));
const ans = Object.fromEntries(bank.map(p=>[p.id,p.answer]));
const st=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
let total={first:0,att:0,res:[]};
for(let g=0;g<games;g++){
  // tap 출격 via real click: title start button approx
  let s=await st();
  if(s.phase!=='playing'){ await page.mouse.click(W*0.65,H*0.845); await sleep(1500); s=await st(); if(s.phase!=='playing'){await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(800);} }
  let lastQ='',qn=0,cyc=0;
  for(let t=0;t<400;t++){
    s=await st();
    if(['clear','gameover','survived'].includes(s.phase)) break;
    if(s.pending||s.feedbackVisible||!s.choices||!s.choices.length||s.questionId===lastQ){await sleep(300);continue;}
    lastQ=s.questionId;qn++;
    let pad;
    if(strat==='fixed') pad=0;
    else if(strat==='cycle') pad=(cyc++)%4;
    else if(strat==='random') pad=Math.floor(Math.random()*4);
    else if(strat==='smart'){ pad=s.choices.indexOf(ans[s.questionId]); if(qn===1||qn===2) pad=(pad+1)%4; }
    await sleep(strat==='smart'?1500:300);
    s=await st(); const r=s.choicePadRects; const x=(r[pad*4]+r[pad*4+2]/2)*W, y=(r[pad*4+1]+r[pad*4+3]/2)*H;
    await page.mouse.click(x,y);
    if(strat==='smart'&&qn===1) { await sleep(700); await page.screenshot({path:`/tmp/hsrev/${strat}-q1-moving.png`}); }
    for(let k=0;k<40;k++){await sleep(250); const z=await st(); if(z.pending||z.feedbackVisible||z.questionId!==lastQ||['clear','gameover','survived'].includes(z.phase))break;}
    if(strat==='smart'&&(qn===1||qn===3)){ await sleep(300); await page.screenshot({path:`/tmp/hsrev/${strat}-q${qn}-feedback.png`}); }
  }
  await sleep(2500); s=await st();
  await page.screenshot({path:`/tmp/hsrev/${strat}-end${g}.png`});
  total.res.push({phase:s.phase,hp:s.hp,firstTry:s.firstTry,attempts:s.attempts,victory:s.victory,coins:s.coins,built:s.builtTowers,lvl:s.upgradeLevel});
  total.first+=s.firstTry; total.att+=s.attempts;
  // retry
  if(g<games-1){ await page.evaluate(()=>window.__GAME_TEST__.start()); await sleep(1000);} 
}
console.log(strat, JSON.stringify(total), 'errs',errs.length);
await browser.close(); await server.close();
