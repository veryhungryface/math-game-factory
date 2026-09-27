import puppeteer from '/Users/sitpo/math-game-factory/node_modules/puppeteer/lib/esm/puppeteer/puppeteer.js';
import { serveStatic } from '/Users/sitpo/math-game-factory/factory/lib/static-server.mjs';
const server = await serveStatic('/Users/sitpo/math-game-factory/public');
const browser = await puppeteer.launch({ executablePath: process.env.PUPPETEER_EXECUTABLE_PATH, headless: true, protocolTimeout: 180000 });
const W=390,H=844; const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const page = await browser.newPage(); await page.setViewport({width:W,height:H});
await page.goto(`${server.url}/g/hyeopgok-sasu/`,{waitUntil:'load'});
await page.waitForFunction(()=>window.__GAME_TEST__&&window.__GAME_TEST__.ready,{timeout:90000});
const st=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
await page.mouse.click(W*0.65,H*0.845); await sleep(3000);
for(const [fx,fy,label] of [[0.12,0.5,'left-edge'],[0.5,0.15,'top-edge'],[0.85,0.85,'corner']]){
 let s=await st(); while(s.pending||s.feedbackVisible){await sleep(300);s=await st();}
 const r=s.choicePadRects; const pad=0; const x=(r[0]+r[2]*fx)*W,y=(r[1]+r[3]*fy)*H;
 await page.mouse.click(x,y); await sleep(3000); const z=await st();
 console.log(label,'click',x.toFixed(0),y.toFixed(0),'hover',z.hover,'pending',z.pending,'att',z.attempts,'fb',z.feedbackVisible);
 await page.screenshot({path:`/tmp/hsrev/corner-${label}.png`});
 if(!z.pending&&!z.feedbackVisible){ // tap center to continue
   const rr=z.choicePadRects; await page.mouse.click((rr[0]+rr[2]/2)*W,(rr[1]+rr[3]/2)*H); await sleep(3500);}
 else await sleep(2000);
}
await browser.close(); await server.close();
