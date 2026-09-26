import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';
const root=process.cwd(),dir=path.join(root,'public/g/hyeopgok-sasu'),packDir=path.join(dir,'packs');
const out=path.join(root,'factory/unity-src/hyeopgok-sasu/ArtSource/validation');
const chrome=process.env.PUPPETEER_EXECUTABLE_PATH||path.join(process.env.HOME,'.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const indexPath=path.join(packDir,'index.json'),original=fs.readFileSync(indexPath),temporary=[];
const originalPack=JSON.parse(fs.readFileSync(path.join(packDir,'m2s2-u7.json')));
const server=await serveStatic(path.join(root,'public'));
const browser=await puppeteer.launch({headless:true,executablePath:chrome,args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage(),cdp=await page.createCDPSession(),errors=[],report={};
page.on('pageerror',e=>errors.push(String(e)));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
page.on('requestfailed',r=>errors.push(r.url()));
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const assert=(yes,msg)=>{if(!yes)throw Error(msg);};
function hashes(){const h={};for(const f of fs.readdirSync(path.join(dir,'Build')))h[f]=crypto.createHash('sha256').update(fs.readFileSync(path.join(dir,'Build',f))).digest('hex');return h;}
function add(pack){const file=pack.pack_id+'.json';temporary.push(path.join(packDir,file));fs.writeFileSync(path.join(packDir,file),JSON.stringify(pack));const index=JSON.parse(fs.readFileSync(indexPath));index.packs.push({pack_id:pack.pack_id,title:pack.title,file});fs.writeFileSync(indexPath,JSON.stringify(index));}
async function load(id){await page.goto(server.url+'/g/hyeopgok-sasu/?pack='+id,{waitUntil:'networkidle0'});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});await sleep(500);return page.evaluate(()=>window.__GAME_TEST__.getState());}
async function tap(x,y){await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x,y,radiusX:2,radiusY:2,force:1,id:1}]});await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});}
try{
 await page.setViewport({width:390,height:844,deviceScaleFactor:2,isMobile:true,hasTouch:true});
 const before=hashes(),renamed=structuredClone(originalPack);renamed.pack_id='temporary-swap-check';renamed.title='재빌드 없는 교체 확인';add(renamed);
 let s=await load(renamed.pack_id);assert(s.pack_id===renamed.pack_id&&s.packTitle===renamed.title,'renamed pack loaded');
 const sample=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(63));assert(sample.length===63&&sample.every(q=>originalPack.items.some(p=>p.id===q.id&&p.answer===q.answer)),'actual bank from external pack');
 report.renamedOnly={pass:true,packId:s.pack_id,title:s.packTitle,sampleCount:sample.length};await page.screenshot({path:path.join(out,'pack-swap.png')});
 // Transient UI fixture only; not a new curriculum pack or published content.
 const elementary=structuredClone(originalPack);elementary.pack_id='temporary-display-check';elementary.title='초등 문자 보기';elementary.school='elementary';elementary.grade=3;
 Object.assign(elementary.items[0],{prompt:'직사각형의 넓이를 구할 때 곱하는 두 길이는 무엇인가?',choices:['가로와 세로','가로와 둘레','세로와 넓이','넓이와 둘레'],answer:'가로와 세로',format:'text',explain:'직사각형의 넓이는 가로와 세로를 곱하여 구한다.'});delete elementary.items[0].answerNumeric;add(elementary);
 await load(elementary.pack_id);await page.screenshot({path:path.join(out,'elementary-title.png')});
 await tap(195,844*.865);await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='playing');await sleep(1000);
 s=await page.evaluate(()=>window.__GAME_TEST__.getState());assert(s.choices.includes('가로와 세로'),'text choices loaded');await page.screenshot({path:path.join(out,'text-choices.png')});
 const i=s.choices.indexOf('가로와 세로');await tap(s.padScreen[i*2]*390,s.padScreen[i*2+1]*844);
 await page.waitForFunction(()=>window.__GAME_TEST__.getState().solved===1,{timeout:5000});report.elementaryText={pass:true,final:await page.evaluate(()=>window.__GAME_TEST__.getState())};
 await page.screenshot({path:path.join(out,'text-correct.png')});
 const after=hashes();assert(JSON.stringify(before)===JSON.stringify(after),'Unity build unchanged');report.buildHashes=after;report.buildUnchanged=true;
 assert(errors.length===0,'browser errors');report.errors=errors;
}finally{
 fs.writeFileSync(indexPath,original);for(const f of temporary)if(fs.existsSync(f))fs.unlinkSync(f);
 await browser.close();await server.close();
}
report.temporaryRemoved=temporary.every(f=>!fs.existsSync(f));assert(report.temporaryRemoved,'temporary files removed');
fs.writeFileSync(path.join(out,'pack-swap.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
