import puppeteer from '/Users/sitpo/math-game-factory/node_modules/puppeteer/lib/esm/puppeteer/puppeteer.js';
import { serveStatic } from '/Users/sitpo/math-game-factory/factory/lib/static-server.mjs';
const server = await serveStatic('/Users/sitpo/math-game-factory/public');
const browser = await puppeteer.launch({ executablePath: process.env.PUPPETEER_EXECUTABLE_PATH, headless: true, protocolTimeout: 180000 });
const W=390,H=844; const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const page = await browser.newPage(); await page.setViewport({width:W,height:H});
await page.goto(`${server.url}/g/hyeopgok-sasu/`,{waitUntil:'load'});
await page.waitForFunction(()=>window.__GAME_TEST__&&window.__GAME_TEST__.ready,{timeout:90000});
const bank = await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(9999));
const ans = Object.fromEntries(bank.map(p=>[p.id,p.answer]));
const st=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
await page.mouse.click(W*0.65,H*0.845); await sleep(2500);
for(let q=0;q<3;q++){
 let s=await st(); while(s.pending||s.feedbackVisible){await sleep(300);s=await st();}
 const pad=s.choices.indexOf(ans[s.questionId]); const r=s.choicePadRects;
 const x=(r[pad*4]+r[pad*4+2]/2)*W, y=(r[pad*4+1]+r[pad*4+3]/2)*H;
 console.log('Q',s.questionId,s.choices,'ans',ans[s.questionId],'pad',pad,'click',x.toFixed(0),y.toFixed(0),'rects',r.map(v=>v.toFixed(2)).join(','));
 await page.mouse.click(x,y);
 for(let k=0;k<16;k++){await sleep(250);const z=await st();console.log(k,'hover',z.hover,'dwell',z.dwell.toFixed(2),'pend',z.pending,'king',z.kingX.toFixed(2),z.kingZ.toFixed(2),'ks',z.kingScreen.map(v=>v.toFixed(2)),'fb',z.feedbackVisible,'ft',z.firstTry,'att',z.attempts); if(z.pending)break;}
 await sleep(2500);
}
await browser.close(); await server.close();
