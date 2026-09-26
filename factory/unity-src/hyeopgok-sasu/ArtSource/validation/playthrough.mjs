import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';
const root=process.cwd(),out=path.join(root,'factory/unity-src/hyeopgok-sasu/ArtSource/validation');
const dir=path.join(root,'public/g/hyeopgok-sasu'),packDir=path.join(dir,'packs');
const chrome=process.env.PUPPETEER_EXECUTABLE_PATH||path.join(process.env.HOME,'.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const server=await serveStatic(path.join(root,'public'));
const browser=await puppeteer.launch({headless:true,executablePath:chrome,args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage(),cdp=await page.createCDPSession(),errors=[],failed=[],report={};
page.on('pageerror',e=>errors.push(String(e)));page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});page.on('requestfailed',r=>failed.push(r.url()));
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
const assert=(yes,msg)=>{if(!yes)throw Error(msg);};
async function load(query=''){
 await page.goto(server.url+'/g/hyeopgok-sasu/'+query,{waitUntil:'networkidle0'});
 await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});await sleep(300);
}
async function touch(x,y,type='touchStart'){
 await cdp.send('Input.dispatchTouchEvent',{type,touchPoints:type==='touchEnd'?[]:[{x,y,radiusX:2,radiusY:2,force:1,id:1}]});
}
async function padMove(i){
 const s=await state(),x=s.padScreen[i*2]*390,y=s.padScreen[i*2+1]*844;
 // Real trusted touchscreen drag; no answer hook or Unity SendMessage.
 const sx=(s.padScreen[4]+s.padScreen[6])*195,sy=s.padScreen[5]*844+62;
 await touch(sx,sy);
 for(let k=1;k<=12;k++){await touch(sx+(x-sx)*k/12,sy+(y-sy)*k/12,'touchMove');await sleep(20);}
 await touch(x,y,'touchEnd');
 await page.waitForFunction(n=>window.__GAME_TEST__.getState().attempts>n,{timeout:6000},s.attempts);
}
async function play(name,wrongFirst=false){
 await load();await touch(195,844*.865);await touch(195,844*.865,'touchEnd');
 await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='playing');await sleep(950);
 const bank=JSON.parse(fs.readFileSync(path.join(packDir,'m2s2-u7.json'))).items;
 const frames=[];
 for(let q=0;q<10;q++){
   let s=await state();const p=bank.find(p=>p.id===s.questionId);assert(p,'actual item in pack');
   if(q===8||q===9)await page.screenshot({path:path.join(out,`${name}-question-${q+1}.png`)});
   let answer=s.choices.indexOf(p.answer);if(wrongFirst&&q===0)answer=(answer+1)%4;
   await padMove(answer);await sleep(140);s=await state();frames.push(s);
   if(q===0||q===9)await page.screenshot({path:path.join(out,`${name}-${q}.png`)});
   if(q<9)await page.waitForFunction(level=>window.__GAME_TEST__.getState().level>level,{timeout:5000},q+1);
   else await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='clear',{timeout:5000});
 }
 const s=await state();assert(s.solved===(wrongFirst?9:10)&&s.phase==='clear',name+' victory');
 report[name]={final:s,frames};await page.screenshot({path:path.join(out,`${name}-clear.png`)});
}
function hashes(){const result={};for(const f of fs.readdirSync(path.join(dir,'Build')))result[f]=crypto.createHash('sha256').update(fs.readFileSync(path.join(dir,'Build',f))).digest('hex');return result;}
const indexPath=path.join(packDir,'index.json'),original=fs.readFileSync(indexPath);
try{
 await page.setViewport({width:390,height:844,deviceScaleFactor:2,isMobile:true,hasTouch:true});
 await page.evaluateOnNewDocument(()=>{window.__TRUSTED__={yes:0,no:0};for(const t of ['pointerdown','pointermove','pointerup'])addEventListener(t,e=>window.__TRUSTED__[e.isTrusted?'yes':'no']++);});
 await play('real-perfect');report.perfectTrust=await page.evaluate(()=>window.__TRUSTED__);
 await play('real-recovery',true);report.recoveryTrust=await page.evaluate(()=>window.__TRUSTED__);
 await load();await touch(195,844*.78);await touch(195,844*.78,'touchEnd');
 await page.waitForFunction(()=>window.__GAME_TEST__.getState().pack_id==='m2s2-u6',{timeout:10000});
 const titleSamples=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(63));
 assert(titleSamples.length===63&&titleSamples.every(p=>p.id.startsWith('m2s2-u6-')),'title selector refreshes actual bank');
 report.titlePackSwitch={pass:true,state:await state(),sampleCount:titleSamples.length};
 // A quick retreat before the .8-second dwell must not submit an answer.
 await load();await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(950);
 let cancelState=await state(),cx=cancelState.padScreen[4]*390,cy=cancelState.padScreen[5]*844;
 await touch(cx,cy);await touch(cx,cy,'touchEnd');await sleep(650);
 const homeX=(cancelState.padScreen[4]+cancelState.padScreen[6])*195,homeY=cy+62;
 await touch(homeX,homeY);await touch(homeX,homeY,'touchEnd');await sleep(1300);
 assert((await state()).attempts===0,'leaving pad cancels dwell');report.dwellCancellation=true;
 // Use the production answer hooks to exercise the exact victory boundary.
 for(const correct of [7,6]){
   await load();await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(850);
   for(let i=0;i<10;i++){
     await page.evaluate(ok=>window.__GAME_TEST__[ok?'answerCorrect':'answerWrong'](),i<correct);
     await sleep(i<correct?1350:1800);
   }
   let boundary=await state();assert(boundary.phase===(correct===7?'clear':'survived')&&boundary.solved===correct,'70% victory boundary');
   report['boundary'+correct]=boundary;await page.screenshot({path:path.join(out,`boundary-${correct}.png`)});
 }
 await load('?pack=m2s2-u6');let s=await state();assert(s.pack_id==='m2s2-u6','counting URL loaded');report.counting=s;
 const before=hashes(),temp=JSON.parse(fs.readFileSync(path.join(packDir,'m2s2-u7.json')));
 temp.pack_id='temporary-swap-check';temp.title='재빌드 없는 교체 확인';
 fs.writeFileSync(path.join(packDir,temp.pack_id+'.json'),JSON.stringify(temp));
 const idx=JSON.parse(original);idx.packs.push({pack_id:temp.pack_id,title:temp.title,file:temp.pack_id+'.json'});fs.writeFileSync(indexPath,JSON.stringify(idx));
 await load('?pack='+temp.pack_id);s=await state();assert(s.pack_id===temp.pack_id&&s.packTitle===temp.title,'temporary pack title/id loaded');
 const sample=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(63));assert(sample.length===63&&sample.every(p=>temp.items.some(x=>x.id===p.id&&x.answer===p.answer)),'bridge bank matches temporary pack');
 await page.screenshot({path:path.join(out,'pack-swap.png')});assert(JSON.stringify(before)===JSON.stringify(hashes()),'build hashes unchanged');
 report.packSwap={pass:true,pack_id:s.pack_id,title:s.packTitle,sampleCount:sample.length,buildHashes:before};
 fs.writeFileSync(indexPath,original);fs.unlinkSync(path.join(packDir,temp.pack_id+'.json'));report.packSwap.temporaryRemoved=true;
 await load();await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(900);
 for(let i=0;i<6;i++){await page.evaluate(()=>window.__GAME_TEST__.answerWrong());await sleep(1800);}
 s=await state();assert(s.hp===0&&s.phase==='gameover','loss');await page.evaluate(()=>{window.__GAME_TEST__.answerCorrect();window.__GAME_TEST__.answerWrong();});await sleep(1200);const after=await state();assert(after.hp===0&&after.phase==='gameover','loss terminal');report.loss=after;
 await page.screenshot({path:path.join(out,'loss.png')});
 await load();await page.evaluate(()=>window.__GAME_TEST__.start());const idleStart=Date.now();
 await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='gameover',{timeout:120000,polling:1000});
 s=await state();assert(s.solved===0&&s.hp===0,'idle cannot progress or win');report.idle={elapsedMs:Date.now()-idleStart,final:s};
 report.errors=errors;report.failedRequests=failed;assert(errors.length===0&&failed.length===0,'browser errors');
 fs.writeFileSync(path.join(out,'playthrough.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
}finally{
 fs.writeFileSync(indexPath,original);const tempPath=path.join(packDir,'temporary-swap-check.json');if(fs.existsSync(tempPath))fs.unlinkSync(tempPath);
 await browser.close();await server.close();
}
