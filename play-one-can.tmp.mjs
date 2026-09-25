import puppeteer from 'puppeteer';
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
const ROOT='/Users/sitpo/math-game-factory/public';
const types={'.html':'text/html','.js':'text/javascript','.json':'application/json','.png':'image/png','.css':'text/css'};
const srv=http.createServer((q,r)=>{let f=path.join(ROOT,decodeURIComponent(q.url.split('?')[0]));if(f.endsWith('/'))f+='index.html';
  fs.readFile(f,(e,d)=>{if(e){r.statusCode=404;r.end('x')}else{r.setHeader('content-type',types[path.extname(f)]||'application/octet-stream');r.end(d)}})});
await new Promise(res=>srv.listen(0,'127.0.0.1',res));
const port=srv.address().port;
const b=await puppeteer.launch({headless:'new',executablePath:process.env.CHROME_BIN,args:['--no-sandbox']});
const p=await b.newPage();
await p.setViewport({width:390,height:844,deviceScaleFactor:2,isMobile:true,hasTouch:true});
const errs=[],reqs=[];
p.on('console',m=>{if(m.type()==='error')errs.push(m.text())});
p.on('pageerror',e=>errs.push('PAGEERROR '+e.message));
p.on('requestfailed',r=>reqs.push(r.url()));
await p.goto(`http://127.0.0.1:${port}/g/one-can/`,{waitUntil:'networkidle0'});
await p.waitForFunction('window.__GAME_TEST__&&window.__GAME_TEST__.ready',{timeout:20000});
const shot=n=>p.screenshot({path:`/tmp/oc2-${n}.png`});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const rail=()=>p.evaluate(()=>{const s=window.__GAME_TEST__.getState();return{need:s.problem?Math.min(1000,s.problem.targetMl-s.orderShipped*1000)-s.canMl:null,canMl:s.canMl,drops:s.dropsLeft,prompt:s.problem&&s.problem.prompt,cups:s.rail.map(c=>({ml:c.ml,uid:c.uid,slot:c.slot,role:c.role})),targets:window.__GAME_TEST__.getInputTargets()}});
async function tapMl(ml){const st=await rail();const i=st.cups.findIndex(c=>c.ml===ml);const t=st.targets[i];
  await p.mouse.click(t.x,t.y);await sleep(1500);}

// --- title + tutorial
await shot('00-title');
await p.click('#startButton'); await sleep(600);
await shot('01-tutorial'); 
let st=await rail(); console.log('TUTORIAL rail',JSON.stringify(st.cups),'budget=',await p.$eval('#budget',e=>e.innerText.replace(/\n/g,' | ')));
await tapMl(500); await tapMl(500);
await shot('02-after-tutorial');
console.log('AFTER TUTORIAL feedback:',await p.$eval('#feedback',e=>e.innerText));

// --- first real order: verify the rack is a unique pair and cups persist
st=await rail(); console.log('\nORDER1 prompt=',st.prompt,'need=',st.need,'drops=',st.drops);
console.log('  rack',JSON.stringify(st.cups));
const pair=[];for(let i=0;i<3;i++)for(let j=i+1;j<3;j++)if(st.cups[i].ml+st.cups[j].ml===st.need)pair.push([st.cups[i].ml,st.cups[j].ml]);
console.log('  pairs summing to need:',JSON.stringify(pair),'| single cups equal to need:',st.cups.filter(c=>c.ml===st.need).length);
await shot('03-order1-rack');
const beforeUids=st.cups.map(c=>c.uid);
await tapMl(pair[0][0]);
const st2=await rail();
console.log('  after 1st pour: canMl=',st2.canMl,'need=',st2.need,'drops=',st2.drops);
console.log('  rack',JSON.stringify(st2.cups));
const kept=st2.cups.filter(c=>beforeUids.includes(c.uid));
console.log('  survivors kept (uid+mL unchanged):',kept.length,JSON.stringify(kept.map(c=>c.ml)),'| fresh cups:',st2.cups.length-kept.length);
console.log('  cups equal to new need:',st2.cups.filter(c=>c.ml===st2.need).length);
await shot('04-after-first-pour');
await sleep(400); await shot('05-rail-settled');
await tapMl(pair[0][1]);
await shot('06-lid');
console.log('  lid feedback:',await p.$eval('#feedback',e=>e.innerText));
console.log('  shipped=',await p.evaluate(()=>window.__GAME_TEST__.getState().shipped));

// --- deliberate wrong path: pour the odd cup, then see the stranded-can message
let st3=await rail();
console.log('\nORDER2 prompt=',st3.prompt,'need=',st3.need,'rack',JSON.stringify(st3.cups.map(c=>c.ml)));
let sol=[];for(let i=0;i<3;i++)for(let j=i+1;j<3;j++)if(st3.cups[i].ml+st3.cups[j].ml===st3.need)sol=[st3.cups[i].ml,st3.cups[j].ml];
const odd=st3.cups.find(c=>!sol.includes(c.ml));
console.log('  odd cup',odd.ml,'role',odd.role,'(need',st3.need+')');
await tapMl(odd.ml);
await sleep(500);
console.log('  after odd pour:',await p.$eval('#feedback',e=>e.innerText).catch(()=>''),'| budget:',await p.$eval('#budget',e=>e.innerText.replace(/\n/g,' | ')));
await shot('07-after-odd-cup');
const st4=await rail();
if(st4.drops===1){ await tapMl(st4.cups[0].ml); await sleep(400); }
console.log('  stranded/overflow feedback:',await p.$eval('#feedback',e=>e.innerText));
await shot('08-failure');
console.log('  lives=',await p.evaluate(()=>window.__GAME_TEST__.getState().lives));

// help text
await p.click('#help'); await sleep(300); await shot('09-help');
console.log('\nHELP:',await p.$eval('#helpBox',e=>e.innerText.replace(/\n/g,' / ')));
console.log('\nconsole errors:',errs.length,errs.slice(0,3),'| failed requests:',reqs.length);
await b.close(); srv.close();
