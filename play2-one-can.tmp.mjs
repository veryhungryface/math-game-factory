import puppeteer from 'puppeteer';import http from 'node:http';import fs from 'node:fs';import path from 'node:path';
const ROOT='/Users/sitpo/math-game-factory/public';
const types={'.html':'text/html','.js':'text/javascript','.json':'application/json','.png':'image/png'};
const srv=http.createServer((q,r)=>{let f=path.join(ROOT,decodeURIComponent(q.url.split('?')[0]));if(f.endsWith('/'))f+='index.html';
  fs.readFile(f,(e,d)=>{if(e){r.statusCode=404;r.end('x')}else{r.setHeader('content-type',types[path.extname(f)]||'application/octet-stream');r.end(d)}})});
await new Promise(res=>srv.listen(0,'127.0.0.1',res));const port=srv.address().port;
const b=await puppeteer.launch({headless:'new',executablePath:process.env.CHROME_BIN,args:['--no-sandbox']});
const p=await b.newPage();await p.setViewport({width:390,height:844,deviceScaleFactor:2,isMobile:true,hasTouch:true});
const errs=[];p.on('console',m=>{if(m.type()==='error')errs.push(m.text())});p.on('pageerror',e=>errs.push('PE '+e.message));
await p.goto(`http://127.0.0.1:${port}/g/one-can/`,{waitUntil:'networkidle0'});
await p.waitForFunction('window.__GAME_TEST__&&window.__GAME_TEST__.ready');
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const rail=()=>p.evaluate(()=>{const s=window.__GAME_TEST__.getState();return{need:Math.min(1000,s.problem.targetMl-s.orderShipped*1000)-s.canMl,canMl:s.canMl,drops:s.dropsLeft,lives:s.lives,cups:s.rail.map(c=>({ml:c.ml,role:c.role})),targets:window.__GAME_TEST__.getInputTargets()}});
const idle=async()=>{for(let k=0;k<60;k++){if(!(await p.evaluate(()=>window.__GAME_TEST__.getState().animating)))return;await sleep(120)}};
async function tapMl(ml,wait=0){await idle();const st=await rail();const i=st.cups.findIndex(c=>c.ml===ml);if(i<0)throw new Error('no cup '+ml+' on rail '+JSON.stringify(st.cups));await p.mouse.click(st.targets[i].x,st.targets[i].y);await sleep(wait||420)}
const fb=()=>p.$eval('#feedback',e=>e.innerText);
await p.click('#startButton');await sleep(500);await tapMl(500);await tapMl(500);await idle();await sleep(300);
// order 1 -> ship it so we land on a fresh order
let st=await rail();let sol=[];for(let i=0;i<3;i++)for(let j=i+1;j<3;j++)if(st.cups[i].ml+st.cups[j].ml===st.need)sol=[st.cups[i].ml,st.cups[j].ml];
// A: overflow — tap a cup bigger than the space left
let over=st.cups.find(c=>c.ml>st.need);
if(!over){await tapMl(sol[0]);st=await rail();over=st.cups.find(c=>c.ml>st.need)}
if(over){await tapMl(over.ml,700);await p.screenshot({path:'/tmp/oc2-A-overflow.png'});console.log('A. 넘침 피드백:',await fb(),'| lives',(await rail()).lives)}
else console.log('A. no spill cup on this rack');
// B: stranded can — pour a legal cup that cannot be finished, then the last drop
st=await rail();sol=[];for(let i=0;i<3;i++)for(let j=i+1;j<3;j++)if(st.cups[i].ml+st.cups[j].ml===st.need)sol=[st.cups[i].ml,st.cups[j].ml];
const odd=st.cups.find(c=>!sol.includes(c.ml)&&c.ml<st.need);
if(odd){await tapMl(odd.ml,900);const s2=await rail();
  console.log('B. after stranding pour: need',s2.need,'drops',s2.drops,'cups',JSON.stringify(s2.cups.map(c=>c.ml)),'exact match on rail:',s2.cups.filter(c=>c.ml===s2.need).length);
  const under=s2.cups.find(c=>c.ml<s2.need)||s2.cups[0];
  await tapMl(under.ml,700);await p.screenshot({path:'/tmp/oc2-B-stranded.png'});
  console.log('B. 부족 피드백:',await fb(),'| lives',(await rail()).lives)}
else console.log('B. odd cup was a spill cup on this rack');
await sleep(1200);await p.screenshot({path:'/tmp/oc2-C2-play.png'});
// landscape check
await p.setViewport({width:1280,height:720,deviceScaleFactor:1});await sleep(900);
await p.evaluate(()=>window.__GAME_TEST__.start());await sleep(1200);await p.screenshot({path:'/tmp/oc2-D-wide.png'});
console.log('\nconsole errors:',errs.length,errs.slice(0,3));
await b.close();srv.close();
