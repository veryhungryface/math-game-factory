// Browser integration: trusted CDP touch only. No correct/incorrect hooks or coin injection.
// Fixture packs are HTTP responses scoped to this page; public/packs is never edited.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../../lib/static-server.mjs';
const testTag=process.argv[3]||'regression-final';if(!/^[a-z0-9-]+$/.test(testTag))throw Error('Invalid tag');
const root=process.cwd(),dir=path.dirname(new URL(import.meta.url).pathname),out=path.join(dir,testTag);
const game=path.join(root,'public/g/hyeopgok-sasu'),source=path.join(root,'factory/unity-src/hyeopgok-sasu/ArtSource/packs');
fs.mkdirSync(out,{recursive:true});
const modes=new Set((process.argv[2]||'hook,layouts,play,edges').split(','));
const chrome=process.env.PUPPETEER_EXECUTABLE_PATH||path.join(process.env.HOME,'.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const bank=Object.fromEntries(['m2s2-u6','m2s2-u7'].map(id=>[id,JSON.parse(fs.readFileSync(path.join(game,'packs',id+'.json'),'utf8'))]));
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const hashes=()=>Object.fromEntries(fs.readdirSync(path.join(game,'Build')).sort().map(f=>[f,hash(path.join(game,'Build',f))]));
const report={time:new Date().toISOString(),inputCadence:{pressMs:60,releaseMs:40},retryCount:0,priorInputFailure:null,inputNote:'Trusted CDP input, 60ms down/40ms release. Ported existing phase2 regression with new title CTA position; no hooks change math or coins.',buildHashes:hashes(),packHashes:Object.fromEntries(Object.keys(bank).map(id=>[id,hash(path.join(game,'packs',id+'.json'))])),frames:[],runs:[],fixtures:[],covers:[],bankChecks:[],errors:[],failedRequests:[],checks:[]};
let viewport={width:390,height:844},overlay=null,activeBank=bank['m2s2-u7'],startMs=0;
const server=await serveStatic(path.join(root,'public'));
const browser=await puppeteer.launch({headless:true,executablePath:chrome,args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage(),cdp=await page.createCDPSession();
page.on('pageerror',e=>report.errors.push(String(e)));page.on('console',m=>{if(m.type()==='error')report.errors.push(m.text());});
page.on('requestfailed',r=>report.failedRequests.push({url:r.url(),error:r.failure()?.errorText}));
await page.setRequestInterception(true);
page.on('request',req=>{const url=new URL(req.url());if(overlay&&url.pathname.endsWith('/packs/index.json'))return req.respond({status:200,contentType:'application/json',body:JSON.stringify({default_pack:overlay.pack_id,packs:[{pack_id:overlay.pack_id,title:overlay.title,file:overlay.pack_id+'.json'}]})});if(overlay&&url.pathname.endsWith('/packs/'+overlay.pack_id+'.json'))return req.respond({status:200,contentType:'application/json',body:JSON.stringify(overlay)});req.continue();});
await page.evaluateOnNewDocument(()=>{window.__INPUT_AUDIT__={trusted:0,untrusted:0,down:0,move:0,up:0};for(const [event,key] of [['pointerdown','down'],['pointermove','move'],['pointerup','up']])addEventListener(event,e=>{window.__INPUT_AUDIT__[e.isTrusted?'trusted':'untrusted']++;window.__INPUT_AUDIT__[key]++;});});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
const assert=(yes,msg)=>{report.checks.push({message:msg,pass:!!yes});if(!yes)throw Error(msg);};
async function until(predicate,timeout=15000){const end=Date.now()+timeout;let s;do{s=await state();if(predicate(s))return s;await sleep(25);}while(Date.now()<end);throw Error('Timed out waiting: '+predicate+' / state '+JSON.stringify(s));}
function point(pair,index=0){return{x:pair[index*2]*viewport.width,y:pair[index*2+1]*viewport.height};}
async function touch(p,type='touchStart'){await cdp.send('Input.dispatchTouchEvent',{type,touchPoints:type==='touchEnd'?[]:[{x:p.x,y:p.y,id:1,radiusX:2,radiusY:2,force:1}]});}
async function tap(p){await touch(p);await sleep(60);await touch(p,'touchEnd');await sleep(40);}
async function drag(from,to,duration=360,steps=8){await touch(from);for(let i=1;i<=steps;i++){await sleep(duration/steps);await touch({x:from.x+(to.x-from.x)*i/steps,y:from.y+(to.y-from.y)*i/steps},'touchMove');}await touch(to,'touchEnd');await sleep(40);}
async function frame(name){const file=name+'.png';await page.screenshot({path:path.join(out,file)});report.frames.push({file,sha256:hash(path.join(out,file)),elapsedMs:startMs?Date.now()-startMs:null,state:await state()});}
async function load(packId='m2s2-u7',w=390,h=844,fixture=null){overlay=fixture;activeBank=fixture??bank[packId];viewport={width:w,height:h};await page.setViewport({width:w,height:h,deviceScaleFactor:1,isMobile:w<900,hasTouch:true});await page.goto(server.url+'/g/hyeopgok-sasu/?pack='+activeBank.pack_id,{waitUntil:'networkidle0',timeout:60000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});await sleep(200);startMs=0;assert((await state()).pack_id===activeBank.pack_id,'loaded '+activeBank.pack_id);await auditBank();}
async function auditBank(){
 const gcd=(a,b)=>b?gcd(b,a%b):Math.abs(a)||1,checks=[];
 for(const requested of [40,17,63]){
  const sample=await page.evaluate(n=>window.__GAME_TEST__.sampleProblems(n),requested),errors=[];
  if(sample.length!==Math.min(requested,activeBank.items.length))errors.push('count mismatch');
  if(new Set(sample.map(q=>q.id)).size!==sample.length)errors.push('duplicate sample id');
  for(const got of sample){
   const q=activeBank.items.find(q=>q.id===got.id);if(!q){errors.push(got.id+': stale pack id');continue;}
   const mode=q.answer_mode??'choice',g=mode==='fraction_parts'?gcd(q.answer.num,q.answer.den):1;
   const answer=mode==='amount'?String(q.answer):mode==='fraction_parts'?'{frac:'+q.answer.num/g+'/'+q.answer.den/g+'}':q.answer;
   if(got.answer!==answer||typeof got.answer!=='string')errors.push(q.id+': v1 string answer');
   if(q.format==='text'?got.answerNumeric!==undefined:got.answerNumeric!==q.answerNumeric)errors.push(q.id+': v1 numeric answer');
   if(JSON.stringify(got.choices)!==JSON.stringify(mode==='choice'?q.choices:null))errors.push(q.id+': v1 choices');
   if(got.prompt!==q.prompt||got.unitConcept!==q.unitConcept||got.explain!==q.explain)errors.push(q.id+': source text');
   if(got.answer_mode!==mode||got.max!==(q.max??0))errors.push(q.id+': mode/max metadata');
   if(mode==='amount'&&got.answerValue!==q.answer)errors.push(q.id+': amount metadata');
   if(mode==='fraction_parts'&&(JSON.stringify(got.answerParts)!==JSON.stringify(q.answer)||got.accept!==q.accept||got.num_label!==q.num_label||got.den_label!==q.den_label))errors.push(q.id+': fraction metadata');
  }
  assert(errors.length===0,'live compiled sample bank '+activeBank.pack_id+' n='+requested+' '+errors.join('; '));checks.push({requested,returned:sample.length,errors});
 }
 const wrapped=await page.evaluate(()=>window.__GAME_TEST__.__hyeopgokBankWrapped===true);assert(wrapped,'compiled metadata wrapper installed');report.bankChecks.push({pack_id:activeBank.pack_id,checks,wrapped});
}
async function start(){await tap({x:viewport.width*(viewport.width/viewport.height>1.2?.23:.5)+59*viewport.height/844,y:viewport.height*(viewport.width/viewport.height>1.2?.83:.85)});await until(s=>s.phase==='playing');startMs=Date.now();}
function current(s){const q=activeBank.items.find(q=>q.id===s.questionId);if(!q)throw Error('Unknown bank item '+s.questionId);return q;}
async function waitCoins(amount,timeout=40000){if(amount<=0)return state();return until(s=>s.coins>=amount||s.pending||s.phase!=='playing',timeout).then(s=>{assert(!s.pending&&s.phase==='playing','coins earned before deadline');return s;});}
async function oneCoin(pad){let s=await state();const previous=s.poured[pad];await tap(point(s.padScreen,pad));return until(x=>x.poured[pad]>previous||x.pending,4000).then(x=>{assert(!x.pending&&x.poured[pad]===previous+1,'one real tap pours exactly one coin');return x;});}
async function zeroVisit(pad){const s=await state();await drag(point(s.exitScreen),point(s.padScreen,pad));return until(x=>x.visited?.[pad]&&x.poured[pad]===0&&!x.pending,6000);}
async function pour(pad,amount){if(amount===0)return zeroVisit(pad);const s=await state();await waitCoins(Math.max(0,amount-s.poured[pad]));while((await state()).poured[pad]<amount)await oneCoin(pad);return state();}
async function exitAndConfirm(before){let s=await state();if(!s.pending)await tap(point(s.exitScreen));s=await until(x=>x.attempts>before,6000);return s;}
async function correct({capture=null,wrong=false}={}){
 const before=await state(),q=current(before),mode=q.answer_mode??'choice';
 if(mode==='choice'){const idx=before.choices.indexOf(q.answer),pad=wrong?(idx+1)%4:idx;await tap(point(before.padScreen,pad));return until(s=>s.attempts>before.attempts,6000);}
 const den=mode==='amount'?q.answer:q.answer.den,num=mode==='fraction_parts'?q.answer.num:0;
 await waitCoins(den+num+(wrong?1:0));await pour(0,den+(wrong&&mode==='amount'?1:0));if(mode==='fraction_parts')await pour(1,num+(wrong?1:0));
 if(capture)await frame(capture);return exitAndConfirm(before.attempts);
}
async function nextWave(previous){return until(s=>s.level>previous||s.phase!=='playing',6500);}
async function trust(){return page.evaluate(()=>window.__INPUT_AUDIT__);}
function fixture(name,question){const p=JSON.parse(JSON.stringify(bank['m2s2-u7']));p.pack_id='fixture-'+name;p.title='입력 경계 검증 '+name;p.items=Array.from({length:10},(_,i)=>({...JSON.parse(JSON.stringify(question)),id:'fixture-'+name+'-'+i,difficulty:i<3?1:i<6?2:i<8?3:4}));return p;}
try{
 if(modes.has('hook')||modes.has('smoke')){
  await load();await frame('title-390');await start();await sleep(400);await frame('first-kill-ground');await until(s=>s.coins>=2,8000);await frame('first-coins-backpack-ghost');
  const before=await state();assert(current(before).answer_mode==='amount'&&current(before).answer===2,'first question amount 2');await oneCoin(0);await frame('first-one-coin-pouring');
  await tap(point((await state()).exitScreen));await until(s=>s.confirming,2500);await frame('confirm-ring');await tap(point((await state()).padScreen,0));await until(s=>s.poured[0]===2&&!s.confirming,2500);await frame('first-two-coins-pouring');
  const result=await exitAndConfirm(before.attempts);await frame('first-two-coin-tower');const elapsedMs=Date.now()-startMs;
  assert(result.solved===1&&result.investment===2&&result.builtTowers===before.builtTowers+1,'two real coins become first tower investment');assert(elapsedMs<=10000,'first real coin-to-tower hook within 10 seconds');report.firstTenSeconds={elapsedMs,result,trust:await trust()};
 }
 if(modes.has('smoke')||modes.has('layouts')){
  await load('m2s2-u7',1280,800);await start();await until(s=>s.coins>=2,8000);await frame('layout-1280-first-coins');
  const longest=bank['m2s2-u7'].items.filter(q=>q.answer_mode==='fraction_parts').sort((a,b)=>b.prompt.length-a.prompt.length)[0];
  await load('m2s2-u7',390,844,fixture('long-fraction',longest));await start();await sleep(1800);await frame('layout-390-long-fraction');
  const beforeExpand=await state();await tap({x:320,y:105});await frame('layout-390-long-expanded');const afterExpand=await state();assert(afterExpand.moves===beforeExpand.moves&&afterExpand.spent===beforeExpand.spent,'reading full prompt leaves world input unchanged');
  await load('m2s2-u7',1280,800,fixture('long-fraction',longest));await start();await sleep(1800);await frame('layout-1280-long-fraction');await tap({x:930,y:100});await frame('layout-1280-long-expanded');
 }
 if(modes.has('play')){
  for(const [name,wrongFirst] of [['real-perfect',false],['real-recovery',true]]){
   await load();await start();const states=[];let fractionShot=false;
   for(let i=0;i<10;i++){
    const s=await state(),q=current(s),take=!fractionShot&&q.answer_mode==='fraction_parts';
    const resolved=await correct({wrong:wrongFirst&&i===0,capture:take?'fraction-pouring-'+name:null});states.push(resolved);console.log(JSON.stringify({run:name,wave:i+1,solved:resolved.solved,spent:resolved.spent}));
    if(take){await frame('fraction-assembled-'+name);fractionShot=true;}
    if(wrongFirst&&i===0){await frame('wrong-coin-loss');assert(resolved.solved===0&&resolved.investment===0&&resolved.spent===3&&resolved.poured[0]===3,'wrong three coins disappear without investment');}
    if(i<9)await nextWave(s.level);else await until(x=>x.phase!=='playing',6500);
   }
   const final=await state();assert(final.phase==='clear'&&final.solved===(wrongFirst?9:10),name+' wins with actual movement/taps');await frame('victory-'+name);report.runs.push({name,elapsedMs:Date.now()-startMs,final,states,trust:await trust()});
  }
 }
	 if(modes.has('fixtures')||modes.has('edges')){
	  // Review regressions use the same real drag shape that used to submit an
	  // accidental zero in firstplay. State and screenshots preserve before/after evidence.
	  const introQuestion={...bank['m2s2-u6'].items[0]};
	  const introPack=fixture('review-fly-through',introQuestion);
	  await load('m2s2-u7',390,844,introPack);await start();await waitCoins(2);
	  let beforeFly=await state(),flyFrom=point(beforeFly.kingScreen),flyPad=point(beforeFly.padScreen,0);
	  const flyPast={x:flyPad.x+(flyPad.x-flyFrom.x)*.65,y:flyPad.y+(flyPad.y-flyFrom.y)*.65};
	  await drag(flyFrom,flyPast,260);await sleep(1500);const afterFly=await state();
	  assert(afterFly.attempts===0&&afterFly.hp===100&&!afterFly.pending&&!afterFly.confirming&&afterFly.poured[0]===0&&afterFly.spent===0&&!afterFly.visited[0],'same fly-through leaves attempts, hp and poured amount unchanged');
	  await frame('fix-fly-through-safe');report.fixtures.push({name:'review-fly-through',beforeReference:'firstplay frame-03 -> frame-04 previously attempts 1 / hp 82',before:beforeFly,after:afterFly,trust:await trust()});
	  const blockedPack=fixture('review-intro-empty-blocked',introQuestion);
	  await load('m2s2-u7',390,844,blockedPack);await start();await waitCoins(2);let blocked=await state();
	  await drag(point(blocked.kingScreen),point(blocked.padScreen,0));await until(s=>s.visited?.[0]&&s.poured[0]===0,6000);blocked=await state();await tap(point(blocked.exitScreen));await sleep(1400);const afterBlocked=await state();
	  assert(afterBlocked.attempts===0&&afterBlocked.hp===100&&!afterBlocked.pending&&!afterBlocked.confirming&&afterBlocked.tutorialBlocked,'opening tutorial refuses an empty stop-and-leave');
	  await frame('fix-intro-empty-blocked');report.fixtures.push({name:'review-intro-empty-blocked',result:afterBlocked,trust:await trust()});
	  const fractionTutorial=fixture('review-fraction-tutorial',bank['m2s2-u7'].items.find(q=>q.answer_mode==='fraction_parts'));
	  await load('m2s2-u7',390,844,fractionTutorial);await start();await sleep(1250);await frame('fix-fraction-tutorial-all-cases');await sleep(1100);await frame('fix-fraction-tutorial-event-cases');
	  const fractionIdle=await state();assert(fractionIdle.attempts===0&&fractionIdle.poured[0]===0&&fractionIdle.poured[1]===0,'fraction tutorial demonstrates without changing the mathematical input');report.fixtures.push({name:'review-fraction-tutorial',result:fractionIdle,trust:await trust()});
	  const fractionDisplay=fixture('review-fraction-zero-denominator-display',bank['m2s2-u7'].items.find(q=>q.answer_mode==='fraction_parts'));
	  await load('m2s2-u7',390,844,fractionDisplay);await start();await waitCoins(3);await pour(1,1);await zeroVisit(0);const incompleteFraction=await state();
	  assert(incompleteFraction.poured[0]===0&&incompleteFraction.poured[1]===1&&incompleteFraction.visited[0]&&incompleteFraction.visited[1],'numerator-first fixture reaches a deliberate n/0 intermediate input');
	  assert(!incompleteFraction.assembledFractionVisible&&incompleteFraction.assembledFractionText==='','n/0 intermediate input never renders as an assembled fraction');await frame('fix-fraction-zero-denominator-hidden');
	  await oneCoin(0);const completeFraction=await state();assert(completeFraction.assembledFractionVisible&&completeFraction.assembledFractionText.length>0,'assembled fraction appears after denominator becomes positive');
	  report.fixtures.push({name:'review-fraction-zero-denominator-display',incomplete:incompleteFraction,complete:completeFraction,trust:await trust()});
	  if(modes.has('fixtures')){
  const worst=bank['m2s2-u7'].items.filter(q=>q.answer_mode==='fraction_parts').sort((a,b)=>(b.answer.num+b.answer.den)-(a.answer.num+a.answer.den))[0];
  const worstPack=fixture('worst-cost-empty-wallet',worst);worstPack.items[0].difficulty=worst.difficulty;
  await load('m2s2-u7',390,844,worstPack);await start();const empty=await state();assert(empty.coins===0&&empty.earned===0&&empty.spent===0,'worst-cost fixture starts with an empty wallet');
  const cost=worst.answer.num+worst.answer.den;await waitCoins(cost,85000);await frame('worst-cost-earned-coins');await pour(0,worst.answer.den);await pour(1,worst.answer.num);await frame('worst-cost-pouring');
  const worstResult=await exitAndConfirm(0),worstMs=Date.now()-startMs,budgetMs=(80+6*worst.difficulty)*1000;
  assert(worstResult.solved===1&&worstResult.investment===cost&&worstResult.spent===cost&&worstResult.earned>=cost&&worstMs<budgetMs,'largest actual answer is funded by combat and solved before deadline');await frame('worst-cost-investment');
  report.fixtures.push({name:'worst-cost-empty-wallet',sourceQuestionId:worst.id,cost,elapsedMs:worstMs,budgetMs,start:empty,result:worstResult,trust:await trust()});
  const old=JSON.parse(fs.readFileSync(path.join(source,'fixtures/v1-legacy.json'),'utf8'));old.items=Array.from({length:10},(_,i)=>({...old.items[i%old.items.length],id:'legacy-'+i}));
  await load('m2s2-u7',390,844,old);await start();const v1=await correct();assert(v1.answerMode==='choice'&&v1.solved===1,'v1 missing answer_mode preserves choice gameplay');await frame('v1-choice-compatible');report.fixtures.push({name:'v1',result:v1,trust:await trust()});
  const holdPack=fixture('hold-continuous',{...bank['m2s2-u6'].items[0],answer:8,answerNumeric:8,prompt:'3 + 5를 계산하시오.',explain:'3 + 5 = 8입니다.'});
  await load('m2s2-u7',390,844,holdPack);await start();await waitCoins(12);
  let held=await state();await touch(point(held.padScreen,0));await until(s=>s.poured[0]>=4,8000);await frame('hold-pouring');
  held=await state();await touch(point(held.exitScreen),'touchMove');await touch(point(held.exitScreen),'touchEnd');
  const heldResult=await until(s=>s.attempts===1,5000);assert(heldResult.pourCount>=4,'holding pours multiple coins through real pointer movement');report.fixtures.push({name:'hold-continuous',result:heldResult,trust:await trust()});
  }
  const amount={...bank['m2s2-u6'].items[0],prompt:'0 + 0을 계산하시오.',answer:0,answerNumeric:0,explain:'0입니다.'};
  const base={...bank['m2s2-u7'].items.find(q=>q.answer_mode==='fraction_parts'),answer:{num:1,den:2},answerNumeric:.5,num_label:'분자',den_label:'분모'};
  const tests=[
   {name:'amount-zero',q:amount,input:[0,0],ok:true},
   {name:'fraction-zero-numerator',q:{...base,answer:{num:0,den:6},answerNumeric:0,accept:'exact_parts',prompt:'흰 공만 6개 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, 검은 공이 나올 확률을 구하시오. 모든 경우와 사건의 경우를 세어 약분하지 말고 나타내시오. (단, 공의 모양과 크기는 모두 같다.)',explain:'모든 경우는 6가지, 검은 공이 나오는 경우는 0가지이므로 {frac:0/6}입니다.'},input:[6,0],ok:true},
   {name:'exact-reject-equivalent',q:{...base,answer:{num:2,den:4},accept:'exact_parts',prompt:'서로 다른 두 개의 동전을 동시에 던질 때, 앞면이 정확히 한 개 나올 확률을 구하시오. 모든 경우와 사건의 경우를 세어 약분하지 말고 나타내시오.',explain:'앞앞, 앞뒤, 뒤앞, 뒤뒤의 4가지 중 앞뒤와 뒤앞의 2가지이므로 {frac:2/4}입니다.'},input:[2,1],ok:false},
   {name:'equivalent-accept',q:{...base,accept:'equivalent',prompt:'동전 한 개를 던질 때, 앞면이 나올 확률을 분수로 구하시오. 같은 값을 나타내는 분수도 정답입니다.',explain:'앞면과 뒷면의 2가지 중 앞면은 1가지이므로 {frac:1/2}입니다.'},input:[4,2],ok:true},
   {name:'reduced-reject',q:{...base,accept:'reduced',prompt:'동전 한 개를 던질 때, 앞면이 나올 확률을 기약분수로 구하시오.',explain:'앞면과 뒷면의 2가지 중 앞면은 1가지이므로 기약분수 {frac:1/2}입니다.'},input:[4,2],ok:false},
   {name:'reduced-accept',q:{...base,accept:'reduced',prompt:'동전 한 개를 던질 때, 앞면이 나올 확률을 기약분수로 구하시오.',explain:'앞면과 뒷면의 2가지 중 앞면은 1가지이므로 기약분수 {frac:1/2}입니다.'},input:[2,1],ok:true},
  ];
  for(const t of tests){const p=fixture(t.name,t.q);await load('m2s2-u7',390,844,p);await start();await waitCoins(t.input[0]+t.input[1]);await pour(0,t.input[0]);if(t.q.answer_mode==='fraction_parts')await pour(1,t.input[1]);const result=await exitAndConfirm(0);assert((result.solved===1)===t.ok,'runtime '+t.name);await frame(t.name);report.fixtures.push({name:t.name,input:t.input,expectedCorrect:t.ok,result,trust:await trust()});}
 }
 if(modes.has('covers')){
  for(const [name,w,h,file] of [['wide',1200,630,'thumb.png'],['square',1080,1080,'square.png']]){
   // Cover the published default unit (m2s2-u7 확률), not the optional 경우의 수 pack.
   await load('m2s2-u7',w,h);await start();await correct();await nextWave(1);await waitCoins(18);const s=await state(),q=current(s),amount=Math.min(typeof q.answer==='object'?q.answer.den:q.answer,4);for(let i=0;i<amount;i++)await oneCoin(0);
   // Capture during the final coin's flight; the screenshot is the complete original frame.
   const target=path.join(game,file);await page.screenshot({path:target});fs.copyFileSync(target,path.join(out,file));
   report.covers.push({file,width:w,height:h,sha256:hash(target),kind:'original WebGL screenshot, no image editing',state:await state(),trust:await trust()});
  }
 }
 assert(JSON.stringify(report.buildHashes)===JSON.stringify(hashes()),'WebGL binaries unchanged during validation');
 assert(report.errors.length===0&&report.failedRequests.length===0,'no browser/runtime/request errors');
 report.pass=true;
}catch(e){report.pass=false;report.failure=String(e.stack??e);await frame('failure').catch(()=>{});console.error(report.failure);process.exitCode=1;}
finally{fs.writeFileSync(path.join(dir,testTag+'.json'),JSON.stringify(report,null,2)+'\n');await browser.close();await server.close();console.log(JSON.stringify({pass:report.pass,checks:report.checks.length,frames:report.frames.length,runs:report.runs.length,fixtures:report.fixtures.length,out:dir}));}
