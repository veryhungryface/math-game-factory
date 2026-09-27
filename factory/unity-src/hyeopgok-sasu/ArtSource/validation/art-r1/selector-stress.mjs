// Run after the art build is ready:
// node factory/unity-src/hyeopgok-sasu/ArtSource/validation/art-r1/selector-stress.mjs
// 60 catalogue entries exist only in intercepted HTTP responses. Published packs
// are read, hashed, and never written. All selection input uses trusted CDP touch.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../../lib/static-server.mjs';

const dir=path.dirname(fileURLToPath(import.meta.url));
const root=process.cwd(),game=path.join(root,'public/g/hyeopgok-sasu');
const tag=process.argv[2]||'selector';
if(!/^[a-z0-9-]+$/.test(tag))throw Error('Invalid output tag');
const out=path.join(dir,tag);
const chrome=process.env.PUPPETEER_EXECUTABLE_PATH||path.join(process.env.HOME,'.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const hashes=folder=>Object.fromEntries(fs.readdirSync(folder).filter(f=>fs.statSync(path.join(folder,f)).isFile()).sort().map(f=>[f,hash(path.join(folder,f))]));
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
const clone=value=>JSON.parse(JSON.stringify(value));
const actualIndex=JSON.parse(fs.readFileSync(path.join(game,'packs/index.json'),'utf8'));
const originals=actualIndex.packs.map(entry=>JSON.parse(fs.readFileSync(path.join(game,'packs',entry.file||entry.pack_id+'.json'),'utf8')));
if(originals.length===0)throw Error('At least one real pack is required for read-only fixtures');

const descriptors=[];
for(const [school,maxGrade] of [['elementary',6],['middle',3],['high',3]]){
  for(let grade=1;grade<=maxGrade;grade++)for(let semester=1;semester<=2;semester++){
    const count=school==='middle'&&grade===2?8:2;
    for(let unit_order=1;unit_order<=count;unit_order++){
      const serial=descriptors.length,source=originals[serial%originals.length];
      const pack_id=`art-stress-${school}-g${grade}s${semester}-u${unit_order}`;
      const pack=clone(source);
      Object.assign(pack,{pack_id,school,grade,semester,unit_order,unit_id:`fixture-g${grade}s${semester}-u${unit_order}`});
      pack.title=school==='middle'&&grade===2&&semester===2&&unit_order===8?'생활 속 분수와 소수의 계산':`${source.title} 원정 ${semester}-${unit_order}`;
      // Preserve every mathematical field; only identity and catalogue metadata differ.
      pack.items=pack.items.map(item=>({...item,id:pack_id+'::'+item.id}));
      const entry={pack_id,title:pack.title,file:pack_id+'.json'};
      if(serial%3===0)Object.assign(entry,{school,grade,semester,unit_order});
      else if(serial%3===1)Object.assign(entry,{school,grade});
      // Exercise JSON header fallback to unit_id, independently of missing index fields.
      if(serial%4===2)delete pack.unit_order;
      descriptors.push({pack_id,school,grade,semester,unit_order,title:pack.title,source:source.pack_id,indexMetadata:serial%3===0?'full':serial%3===1?'partial':'missing',headerOrder:pack.unit_order?'explicit':'unit_id fallback',entry,pack});
    }
  }
}
// Deliberately reverse order: a list that merely preserves index order must fail.
const catalogue={default_pack:descriptors.find(x=>x.school==='middle'&&x.grade===2&&x.semester===2&&x.unit_order===8).pack_id,packs:descriptors.map(x=>x.entry).reverse()};
const byId=new Map(descriptors.map(d=>[d.pack_id,d]));
const responses=new Map(descriptors.map(d=>[d.entry.file,JSON.stringify(d.pack)]));
const requiredMetadata=new Set(descriptors.filter(d=>d.indexMetadata!=='full').map(d=>d.pack_id));
const ordered=(school,grade)=>descriptors.filter(d=>d.school===school&&d.grade===grade).sort((a,b)=>a.semester-b.semester||a.unit_order-b.unit_order);
const report={time:new Date().toISOString(),fixture:{count:descriptors.length,existsOnlyInMemory:true,indexOrder:'reversed',metadataFallbackCount:requiredMetadata.size,descriptors:descriptors.map(({pack,entry,...d})=>d)},buildHashes:hashes(path.join(game,'Build')),packHashes:hashes(path.join(game,'packs')),checks:[],frames:[],runs:[],requests:[],errors:[],requestFailures:[],visualReview:'Screenshots require direct visual inspection; the script does not claim OCR or rendered-text bounds verification.'};
fs.mkdirSync(out,{recursive:true});
let server,browser,page,cdp,viewport,currentRun;
const metadataSeen=new Set();
const assert=(pass,message,details)=>{report.checks.push({pass:!!pass,message,...(details?{details}:{} )});if(!pass)throw Error(message);};
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
async function until(predicate,timeout=30000){
  const deadline=Date.now()+timeout;let value;
  do{value=await state();if(predicate(value))return value;await sleep(35);}while(Date.now()<deadline);
  throw Error('Timed out waiting for '+predicate+' / '+JSON.stringify(value));
}
function coords(){
  const {width:w,height:h}=viewport,s=h/844,wide=w/h>1.15;
  const left=w*.5-185*s,top=h*.5-297*s;
  return {
    scale:s,open:{x:(wide?w*.23:w*.5)-108*s,y:h*(wide?.83:.85)},
    school:i=>({x:w*.5+(i-1)*112*s,y:top+95*s}),
    grade:(school,grade)=>({x:w*.5+(grade-1-(school==='elementary'?2.5:1))*(school==='elementary'?54:108)*s,y:top+148*s}),
    card:(row=0,scroll=0)=>({x:w*.5-3*s,y:top+(254+row*94-scroll)*s}),
    dragFrom:{x:w*.5+81*s,y:top+515*s},dragTo:{x:w*.5+81*s,y:top+248*s},
    close:{x:left+342*s,y:top+29*s},
  };
}
async function touch(point,type='touchStart'){
  await cdp.send('Input.dispatchTouchEvent',{type,touchPoints:type==='touchEnd'?[]:[{x:point.x,y:point.y,id:1,radiusX:3,radiusY:3,force:1}]});
}
async function tap(point){await touch(point);await sleep(90);await touch(point,'touchEnd');await sleep(100);}
async function drag(from,to){
  await touch(from);await sleep(90);
  for(let step=1;step<=12;step++){await touch({x:from.x+(to.x-from.x)*step/12,y:from.y+(to.y-from.y)*step/12},'touchMove');await sleep(45);}
  await touch(to,'touchEnd');await sleep(130);
}
async function frame(name){
  const file=name+'.png';await page.screenshot({path:path.join(out,file)});
  report.frames.push({file,sha256:hash(path.join(out,file)),viewport,state:await state()});
}
async function auditBank(descriptor){
  const samples=[];
  for(const n of [17,40,63]){
    const got=await page.evaluate(count=>window.__GAME_TEST__.sampleProblems(count),n);
    const actualIds=new Set(descriptor.pack.items.map(q=>q.id));
    assert(got.length===Math.min(n,descriptor.pack.items.length),'sample count refreshes after selection',{pack_id:descriptor.pack_id,requested:n,actual:got.length});
    assert(got.every(q=>actualIds.has(q.id)&&q.id.startsWith(descriptor.pack_id+'::')),'sample bank contains only selected pack',{pack_id:descriptor.pack_id,requested:n});
    assert(got.every(q=>descriptor.pack.items.find(item=>item.id===q.id)?.prompt===q.prompt),'sample prompts preserve source pack questions',{pack_id:descriptor.pack_id,requested:n});
    samples.push({requested:n,returned:got.length,firstIds:got.slice(0,3).map(q=>q.id)});
  }
  const s=await state();assert(s.pack_id===descriptor.pack_id&&s.packTitle===descriptor.title,'selected pack title and identity update together',{expected:descriptor.title,actual:s.packTitle});
  assert(await page.evaluate(()=>window.__SELECTOR_SAMPLE_BEFORE__===window.__GAME_TEST__.sampleProblems),'same-page sample function remains stable');
  currentRun.selections.push({pack_id:descriptor.pack_id,title:s.packTitle,samples});
}
async function open(school,grade){
  const c=coords();await tap(c.open);await sleep(150);
  await tap(c.school(['elementary','middle','high'].indexOf(school)));await tap(c.grade(school,grade));await sleep(120);
}
async function choose(school,grade,row,atBottom=false){
  const group=ordered(school,grade),expected=group[row],c=coords();
  await open(school,grade);
  let scroll=0;
  if(atBottom){
    const before=await state();scroll=Math.max(0,group.length*94-336);
    for(let i=0;i<Math.ceil(scroll/267)+1;i++)await drag(c.dragFrom,c.dragTo);
    assert((await state()).pack_id===before.pack_id,'drag scroll does not accidentally choose a card',{school,grade});
  }
  await frame(`${viewport.width}-${school}-g${grade}-${atBottom?'bottom':'top'}-${row}`);
  await tap(c.card(row,scroll));await until(s=>s.pack_id===expected.pack_id);
  await auditBank(expected);await frame(`${viewport.width}-title-${school}-g${grade}-s${expected.semester}-u${expected.unit_order}`);
}

try{
  server=await serveStatic(path.join(root,'public'));
  browser=await puppeteer.launch({headless:true,executablePath:chrome,args:['--no-sandbox','--mute-audio']});
  page=await browser.newPage();cdp=await page.createCDPSession();
  page.on('pageerror',error=>report.errors.push(String(error)));
  page.on('console',message=>{if(message.type()==='error')report.errors.push(message.text());});
  page.on('requestfailed',request=>report.requestFailures.push({url:request.url(),error:request.failure()?.errorText}));
  await page.setRequestInterception(true);
  page.on('request',request=>{
    const url=new URL(request.url());
    if(url.pathname.endsWith('/packs/index.json'))return request.respond({status:200,contentType:'application/json',body:JSON.stringify(catalogue)});
    const filename=url.pathname.split('/').at(-1);
    if(url.pathname.includes('/packs/')&&responses.has(filename)){
      const id=filename.slice(0,-5);metadataSeen.add(id);report.requests.push({viewport:viewport?.width,pack_id:id,time:Date.now()});
      return request.respond({status:200,contentType:'application/json',body:responses.get(filename)});
    }
    return request.continue();
  });
  await page.evaluateOnNewDocument(()=>{
    window.__SELECTOR_INPUT__={trusted:0,untrusted:0,down:0,move:0,up:0};
    for(const [event,key] of [['pointerdown','down'],['pointermove','move'],['pointerup','up']])addEventListener(event,e=>{window.__SELECTOR_INPUT__[e.isTrusted?'trusted':'untrusted']++;window.__SELECTOR_INPUT__[key]++;});
  });
  for(const [width,height] of [[390,844],[1280,800]]){
    viewport={width,height};metadataSeen.clear();currentRun={viewport,selections:[]};report.runs.push(currentRun);
    await page.setViewport({width,height,deviceScaleFactor:1,isMobile:width<900,hasTouch:true});
    await page.goto(server.url+'/g/hyeopgok-sasu/',{waitUntil:'networkidle0',timeout:60000});
    await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
    await page.evaluate(()=>{window.__SELECTOR_SAMPLE_BEFORE__=window.__GAME_TEST__.sampleProblems;});
    const deadline=Date.now()+60000;
    while([...requiredMetadata].some(id=>!metadataSeen.has(id))&&Date.now()<deadline)await sleep(80);
    assert([...requiredMetadata].every(id=>metadataSeen.has(id)),'missing catalogue metadata fetched from pack JSON',{expected:requiredMetadata.size,fetched:[...requiredMetadata].filter(id=>metadataSeen.has(id)).length});
    await sleep(500);await auditBank(byId.get(catalogue.default_pack));await frame(`${width}-initial-title-long-name`);
    // Default group has 16 cards. Reversed input order proves actual sorting.
    await choose('middle',2,0);await choose('middle',2,1);await choose('middle',2,15,true);
    await choose('elementary',6,0);await choose('elementary',1,3,true);
    await choose('middle',3,0);await choose('high',1,0);await choose('high',3,3,true);
    await open('middle',2);await frame(`${width}-catalogue-16-cards`);await tap(coords().close);
    currentRun.input=await page.evaluate(()=>window.__SELECTOR_INPUT__);
    assert(currentRun.input.trusted>0&&currentRun.input.untrusted===0,'all catalogue interactions use trusted browser pointer input',currentRun.input);
    assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'no horizontal overflow');
  }
  assert(report.errors.length===0&&report.requestFailures.length===0,'no browser/runtime/request failures');
  assert(JSON.stringify(report.packHashes)===JSON.stringify(hashes(path.join(game,'packs'))),'published pack files unchanged during test');
  assert(JSON.stringify(report.buildHashes)===JSON.stringify(hashes(path.join(game,'Build'))),'WebGL build unchanged during test');
  report.pass=true;
}catch(error){
  report.pass=false;report.failure=String(error.stack??error);process.exitCode=1;
  if(page)await frame('failure').catch(()=>{});
  console.error(report.failure);
}finally{
  fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2)+'\n');
  if(browser)await browser.close();if(server)await server.close();
  console.log(JSON.stringify({pass:report.pass,catalogueCount:descriptors.length,checks:report.checks.length,frames:report.frames.length,out}));
}
