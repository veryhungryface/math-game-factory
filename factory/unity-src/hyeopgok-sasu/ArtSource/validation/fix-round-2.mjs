import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const root=process.cwd();
const out=path.join(root,'factory/unity-src/hyeopgok-sasu/ArtSource/validation/fix-round-2');
const pack=JSON.parse(fs.readFileSync(path.join(root,'public/g/hyeopgok-sasu/packs/m2s2-u7.json'))).items;
fs.mkdirSync(out,{recursive:true});

function chromePath(){
  if(process.env.PUPPETEER_EXECUTABLE_PATH)return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base=path.join(process.env.HOME,'.cache/puppeteer/chrome');
  for(const build of fs.readdirSync(base).sort().reverse())for(const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
  ]){const candidate=path.join(base,build,rel);if(fs.existsSync(candidate))return candidate;}
  throw Error('Chrome for Testing not found');
}

const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
const server=await serveStatic(path.join(root,'public'));
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage();
const cdp=await page.createCDPSession();
const errors=[],failed=[];
page.on('pageerror',error=>errors.push(String(error)));
page.on('console',message=>{if(message.type()==='error')errors.push(message.text());});
page.on('requestfailed',request=>failed.push(request.url()));
await page.setViewport({width:390,height:844,deviceScaleFactor:2,isMobile:true,hasTouch:true});
await page.evaluateOnNewDocument(()=>{
  window.__FIX_TRUST__={yes:0,no:0};
  for(const type of ['pointerdown','pointermove','pointerup'])addEventListener(type,event=>window.__FIX_TRUST__[event.isTrusted?'yes':'no']++);
});

const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
async function touch(x,y,type='touchStart'){
  await cdp.send('Input.dispatchTouchEvent',{type,touchPoints:type==='touchEnd'?[]:[{x,y,radiusX:2,radiusY:2,force:1,id:1}]});
}
async function tap(x,y){await touch(x,y);await sleep(80);await touch(x,y,'touchEnd');}
async function loadAndStart(){
  await page.goto(server.url+'/g/hyeopgok-sasu/',{waitUntil:'networkidle0'});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:30000});
  const before=await state(),startedAt=Date.now();
  await tap(195,422);
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='playing',{timeout:5000});
  return {before,after:await state(),transitionMs:Date.now()-startedAt};
}
async function moveToPad(index){
  const before=await state();
  const x=before.padScreen[index*2]*390,y=before.padScreen[index*2+1]*844;
  const startX=(before.padScreen[4]+before.padScreen[6])*195;
  const startY=before.padScreen[5]*844+62;
  await touch(startX,startY);
  for(let step=1;step<=14;step++){
    await touch(startX+(x-startX)*step/14,startY+(y-startY)*step/14,'touchMove');
    await sleep(22);
  }
  await touch(x,y,'touchEnd');
  await page.waitForFunction(attempts=>window.__GAME_TEST__.getState().attempts>attempts,{timeout:6000},before.attempts);
}
function answerIndex(current,correct){
  const item=pack.find(candidate=>candidate.id===current.questionId);
  if(!item)throw Error('Question missing from production pack: '+current.questionId);
  const answer=current.choices.indexOf(item.answer);
  if(answer<0)throw Error('Answer missing from rendered choices: '+current.questionId);
  return correct?answer:(answer+1)%4;
}

const report={scenario:'real touch; first answer wrong (capture correct-pad mark); wait 21.5 seconds on every later question (past the 18 s pressure line); remaining nine correct',waves:[]};
try{
  report.titleTransition=await loadAndStart();
  await page.screenshot({path:path.join(out,'01-first-question-guide.png')});
  for(let wave=0;wave<10;wave++){
    const before=await state(),questionStart=Date.now();
    await sleep(wave===0?1500:21500);
    await moveToPad(answerIndex(before,wave!==0));
    const after=await state();
    report.waves.push({wave:wave+1,waitAndInputMs:Date.now()-questionStart,before:{hp:before.hp,attempts:before.attempts,solved:before.solved,questionId:before.questionId},after:{hp:after.hp,attempts:after.attempts,solved:after.solved,phase:after.phase,moves:after.moves}});
    if(wave===0){await sleep(350);report.firstWrongState=await state();await page.screenshot({path:path.join(out,'02-wrong-marks-correct-pad.png')});}
    if(wave<9)await page.waitForFunction(level=>window.__GAME_TEST__.getState().level>level,{timeout:6000},before.level);
  }
  await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='clear',{timeout:6000});
  report.final=await state();
  await page.screenshot({path:path.join(out,'03-21s-recovery-clear.png')});
  if(report.final.solved!==9||report.final.attempts!==10||report.final.hp<=0)throw Error('Slow recovery did not finish with 9/10 and HP>0');

  report.trustedPointers=await page.evaluate(()=>window.__FIX_TRUST__);
  report.errors=errors;report.failedRequests=failed;
  if(errors.length||failed.length)throw Error('Browser errors or failed requests');
  fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2)+'\n');
  console.log(JSON.stringify({transitionMs:report.titleTransition.transitionMs,final:{phase:report.final.phase,hp:report.final.hp,solved:report.final.solved,attempts:report.final.attempts},trustedPointers:report.trustedPointers}));
}finally{
  await browser.close();
  await server.close();
}
