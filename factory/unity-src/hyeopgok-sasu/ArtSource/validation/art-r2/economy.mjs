// Real current-pack economy regression. Read-only packs and build. No answer,
// start, clock, simulation, or currency injection hooks: trusted CDP touch only.
// node .../economy.mjs [--all-amounts] [--case=amount-59]
// Run exclusively after the final build; a private browser/server are closed.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../../lib/static-server.mjs';
const root=process.cwd(),dir=path.dirname(new URL(import.meta.url).pathname);
const game=path.join(root,'public/g/hyeopgok-sasu'),packDir=path.join(game,'packs');
const source=path.join(root,'factory/unity-src/hyeopgok-sasu/Scripts');
const index=JSON.parse(fs.readFileSync(path.join(packDir,'index.json'),'utf8'));
const banks=index.packs.map(p=>({meta:p,data:JSON.parse(fs.readFileSync(path.join(packDir,p.file),'utf8'))}));
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const buildHashes=()=>Object.fromEntries(fs.readdirSync(path.join(game,'Build')).sort().map(f=>[f,hash(path.join(game,'Build',f))]));
const packHashes=()=>Object.fromEntries(['index.json',...index.packs.map(p=>p.file)].map(f=>[f,hash(path.join(packDir,f))]));
const all=banks.flatMap(b=>b.data.items.map(q=>({bank:b.data,question:q})));
const cost=q=>q.answer_mode==='fraction_parts'?q.answer.num+q.answer.den:q.answer;
const args=new Set(process.argv.slice(2));
const chosenAmounts=args.has('--all-amounts')?Array.from({length:20},(_,i)=>i+40):[40,50,59];
let cases=chosenAmounts.map(value=>({name:'amount-'+value,...all.find(x=>x.question.answer_mode==='amount'&&x.question.answer===value)}));
const fractions=all.filter(x=>x.question.answer_mode==='fraction_parts').sort((a,b)=>cost(b.question)-cost(a.question));
cases.push({name:'fraction-global-max',...fractions[0]});
const s1=fractions.find(x=>x.bank.pack_id.startsWith('m2s1-'));
if(s1&&s1.question.id!==fractions[0].question.id)cases.push({name:'fraction-semester1-max',...s1});
const only=[...args].find(x=>x.startsWith('--case='))?.slice(7);if(only)cases=cases.filter(x=>x.name===only);
if(!cases.length||cases.some(x=>!x.question))throw Error('No eligible source question');
const intro=all.find(x=>x.question.answer_mode==='amount'&&x.question.answer===2&&x.bank.pack_id==='m2s2-u7').question;
const tag=only?'economy-'+only:'economy',out=path.join(dir,tag);fs.mkdirSync(out,{recursive:true});
const report={time:new Date().toISOString(),pass:false,method:'Current 13 packs scanned, HTTP-only fixtures, genuine two-coin opening then target in wave 2. All target fixture difficulties become 1: conservative 66s amount / 86s fraction limits. No production JSON or rules changes.',input:{method:'trusted CDP touch',pressMs:60,releaseMs:40,hookCalls:['getState only'],retries:0},buildHashes:buildHashes(),packHashes:packHashes(),rulesHash:hash(path.join(source,'HyeopgokRules.cs')),coverage:{packCount:banks.length,amount40to59Count:all.filter(x=>x.question.answer_mode==='amount'&&x.question.answer>=40&&x.question.answer<=59).length,amountValues:[...new Set(all.filter(x=>x.question.answer_mode==='amount'&&x.question.answer>=40&&x.question.answer<=59).map(x=>x.question.answer))].sort((a,b)=>a-b),maxFraction:{id:fractions[0].question.id,answer:fractions[0].question.answer,cost:cost(fractions[0].question)}},cases:[],checks:[],frames:[],errors:[],failedRequests:[]};
let overlay=null,startMs=0,currentCase=null;
const server=await serveStatic(path.join(root,'public'));
const chrome=process.env.PUPPETEER_EXECUTABLE_PATH||path.join(process.env.HOME,'.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const browser=await puppeteer.launch({headless:true,executablePath:chrome,args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage(),cdp=await page.createCDPSession();
await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
page.on('pageerror',e=>report.errors.push(String(e)));page.on('console',m=>{if(m.type()==='error')report.errors.push(m.text());});
page.on('requestfailed',r=>report.failedRequests.push({url:r.url(),error:r.failure()?.errorText}));
await page.setRequestInterception(true);
page.on('request',r=>{const u=new URL(r.url());if(overlay&&u.pathname.endsWith('/packs/index.json'))return r.respond({status:200,contentType:'application/json',body:JSON.stringify({default_pack:overlay.pack_id,packs:[{pack_id:overlay.pack_id,title:overlay.title,file:overlay.pack_id+'.json'}]})});if(overlay&&u.pathname.endsWith('/packs/'+overlay.pack_id+'.json'))return r.respond({status:200,contentType:'application/json',body:JSON.stringify(overlay)});r.continue();});
await page.evaluateOnNewDocument(()=>{window.__ECONOMY_INPUT__={trusted:0,untrusted:0,down:0,up:0};for(const [event,key] of [['pointerdown','down'],['pointerup','up']])addEventListener(event,e=>{window.__ECONOMY_INPUT__[e.isTrusted?'trusted':'untrusted']++;window.__ECONOMY_INPUT__[key]++;});});
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
const check=(ok,message)=>{report.checks.push({case:currentCase?.name,message,pass:!!ok});if(!ok)throw Error(message);};
function point(pair,i=0){return{x:pair[i*2]*390,y:pair[i*2+1]*844};}
async function touch(p,type='touchStart'){await cdp.send('Input.dispatchTouchEvent',{type,touchPoints:type==='touchEnd'?[]:[{x:p.x,y:p.y,id:1,radiusX:2,radiusY:2,force:1}]});}
async function tap(p){await touch(p);await sleep(60);await touch(p,'touchEnd');await sleep(40);}
async function until(predicate,ms=15000){const stop=Date.now()+ms;let s;do{s=await state();if(predicate(s))return s;await sleep(25);}while(Date.now()<stop);throw Error('Timeout '+predicate+' '+JSON.stringify(s));}
async function frame(name){const file=path.join(out,name+'.png');await page.screenshot({path:file});report.frames.push({file:path.relative(dir,file),sha256:hash(file),elapsedMs:Date.now()-startMs,state:await state()});}
function skim(s){return{wallMs:Date.now()-startMs,phase:s.phase,level:s.level,hp:s.hp,coins:s.coins,earned:s.earned,spent:s.spent,investment:s.investment,poured:s.poured,pending:s.pending,solved:s.solved,redCount:s.redCount,blueCount:s.blueCount};}
async function waitFunds(amount,ms){const begin=Date.now(),trace=[];let s;do{s=await state();trace.push(skim(s));check(s.earned===s.spent+s.coins,'conserved spendable currency while collecting');if(s.coins>=amount)return{state:s,elapsedMs:Date.now()-begin,trace};if(s.pending||s.phase!=='playing')throw Error('Economy deadline/defeat before '+amount+' coins: '+JSON.stringify(s));await sleep(250);}while(Date.now()-begin<ms);throw Error('Insufficient coins after '+ms+'ms: '+JSON.stringify(s));}
async function oneCoin(pad){const before=await state(),n=before.poured[pad];await tap(point(before.padScreen,pad));const after=await until(s=>s.poured[pad]>n||s.pending,4500);check(!after.pending&&after.poured[pad]===n+1&&after.spent===before.spent+1,'one trusted tap spends exactly one coin despite eight visual pour discs');check(after.earned===after.spent+after.coins,'decorative discs do not change wallet conservation');return after;}
async function pour(pad,n){while((await state()).poured[pad]<n)await oneCoin(pad);}
async function confirm(previous){const s=await state();await tap(point(s.exitScreen));return until(x=>x.attempts>previous,6500);}
function fixture(c){const p=structuredClone(c.bank);p.pack_id='art-r2-economy-'+c.name;p.title='경제 검증 '+c.name;p.items=[{...structuredClone(intro),id:p.pack_id+'-intro',difficulty:1},...Array.from({length:9},(_,i)=>({...structuredClone(c.question),id:p.pack_id+'-target-'+i,difficulty:1}))];return p;}
try{
 for(const c of cases){
  currentCase=c;overlay=fixture(c);const record={name:c.name,sourcePack:c.bank.pack_id,sourceId:c.question.id,sourceDifficulty:c.question.difficulty,fixtureDifficulty:1,sourceAnswer:c.question.answer,cost:cost(c.question),timeLimitSeconds:c.question.answer_mode==='fraction_parts'?86:66};report.cases.push(record);
  await page.goto(server.url+'/g/hyeopgok-sasu/?pack='+overlay.pack_id,{waitUntil:'networkidle0',timeout:60000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});await sleep(250);
  check((await state()).pack_id===overlay.pack_id,'HTTP fixture loaded without mutating pack JSON');
  await tap({x:254,y:717.4});await until(s=>s.phase==='playing');startMs=Date.now();record.start=await state();
  check(record.start.coins===0&&record.start.earned===0&&record.start.spent===0,'real opening starts at zero coins');
  check(record.start.questionId.endsWith('-intro'),'first wave is the genuine original two-coin intro');
  await waitFunds(2,10000);await pour(0,2);record.intro=await confirm(0);record.introMs=Date.now()-startMs;
  check(record.intro.solved===1&&record.intro.investment===2,'trusted two-coin intro completes and builds first tower');await frame(c.name+'-intro-complete');
  const before=await until(s=>s.level===2&&!s.pending,6500);const waveStart=Date.now();record.wave2Start=before;
  check(before.questionId.includes('-target-'),'selected expensive source item is in second wave');await frame(c.name+'-wave2-start');
  const funds=await waitFunds(record.cost,record.timeLimitSeconds*1000);record.collectionMs=funds.elapsedMs;record.collectionTrace=funds.trace;record.funded=funds.state;await frame(c.name+'-funded');
  if(c.question.answer_mode==='fraction_parts'){await pour(0,c.question.answer.den);await pour(1,c.question.answer.num);}else await pour(0,c.question.answer);
  await frame(c.name+'-deposited');record.result=await confirm(before.attempts);record.wave2ElapsedMs=Date.now()-waveStart;record.timeRemainingSeconds=record.timeLimitSeconds-record.wave2ElapsedMs/1000;
  record.input=await page.evaluate(()=>window.__ECONOMY_INPUT__);
  check(record.result.solved===2&&record.result.attempts===2,'expensive item accepted on its first real attempt');
  check(record.result.spent-before.spent===record.cost&&record.result.investment-before.investment===record.cost,'all expensive coins funded and invested exactly once');
  check(record.result.hp>0&&record.wave2ElapsedMs<record.timeLimitSeconds*1000,'wave two solves before deadline with gate alive');
  check(record.input.trusted>0&&record.input.untrusted===0,'all play input trusted; no synthetic DOM pointer calls');
  check(record.result.earned===record.result.spent+record.result.coins,'final wallet identity excludes visual-only spill and pour copies');
  await frame(c.name+'-complete');record.pass=true;console.log(JSON.stringify({case:c.name,cost:record.cost,collectionMs:record.collectionMs,elapsedMs:record.wave2ElapsedMs,hp:record.result.hp,earned:record.result.earned,remainingSeconds:record.timeRemainingSeconds}));
 }
 check(JSON.stringify(report.buildHashes)===JSON.stringify(buildHashes()),'WebGL build unchanged during evidence run');
 check(JSON.stringify(report.packHashes)===JSON.stringify(packHashes()),'all thirteen production packs and index unchanged');
 check(report.rulesHash===hash(path.join(source,'HyeopgokRules.cs')),'rules unchanged during evidence run');
 check(report.errors.length===0&&report.failedRequests.length===0,'no browser, runtime, or request errors');
 report.pass=true;
}catch(error){report.failure=String(error.stack??error);await frame('failure').catch(()=>{});console.error(report.failure);process.exitCode=1;}
finally{fs.writeFileSync(path.join(dir,tag+'.json'),JSON.stringify(report,null,2)+'\n');await browser.close();await server.close();console.log(JSON.stringify({pass:report.pass,cases:report.cases.length,checks:report.checks.length,report:path.join(dir,tag+'.json')}));}
