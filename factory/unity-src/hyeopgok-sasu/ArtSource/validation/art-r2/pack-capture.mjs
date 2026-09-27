// Actual WebGL capture; source packs are read-only. Item selection uses a scoped
// HTTP fixture that moves one unedited source question to slot 0 for repeatability.
import fs from 'node:fs';import path from 'node:path';import crypto from 'node:crypto';import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../../lib/static-server.mjs';
const dir=path.dirname(new URL(import.meta.url).pathname),out=path.join(dir,process.argv[2]||'packs');fs.mkdirSync(out,{recursive:true});
const game='public/g/hyeopgok-sasu',packDir=game+'/packs';
const hash=f=>crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const hashes=()=>Object.fromEntries(fs.readdirSync(packDir).filter(f=>f.endsWith('.json')).sort().map(f=>[f,hash(packDir+'/'+f)]));
const before=hashes(),index=JSON.parse(fs.readFileSync(packDir+'/index.json'));
const server=await serveStatic(path.join(process.cwd(),'public'));
const browser=await puppeteer.launch({headless:true,executablePath:process.env.PUPPETEER_EXECUTABLE_PATH||path.join(process.env.HOME,'.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'),args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage(),cdp=await page.createCDPSession(),errors=[],warnings=[],frames=[];let overlay=null,currentId='';
page.on('pageerror',e=>errors.push(String(e)));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());if(m.type()==='warning'&&/glyph|unicode|font/i.test(m.text()))warnings.push(m.text());});
await page.setViewport({width:390,height:844,deviceScaleFactor:2,isMobile:true,hasTouch:true});await page.setRequestInterception(true);
page.on('request',r=>{if(overlay&&r.url().split('?')[0].endsWith('/packs/'+currentId+'.json'))r.respond({status:200,contentType:'application/json',body:JSON.stringify(overlay)});else r.continue();});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
const state=()=>page.evaluate(()=>window.__GAME_TEST__.getState());
async function tap(x,y){await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x,y,id:1}]});await sleep(80);await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await sleep(120);}
async function frame(name,source){await page.screenshot({path:path.join(out,name+'.png')});frames.push({file:name+'.png',source,state:await state()});console.log(name);}
async function load(pack,q,label){currentId=pack.pack_id;overlay=q?{...pack,items:[q,...pack.items.filter(x=>x.id!==q.id)]}:null;
 await page.goto(server.url+'/g/hyeopgok-sasu/?pack='+pack.pack_id,{waitUntil:'networkidle0',timeout:90000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
 const samples=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(40));if(samples.length!==40)throw Error('sample count '+pack.pack_id);
 await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(600);
 if(q&&(await state()).questionId!==q.id)throw Error('wrong fixture '+q.id+' '+JSON.stringify(await state()));
 const info={pack:pack.pack_id,id:q?.id,prompt:q?.prompt,chars:q?.prompt.length,choices:q?.choices,den_label:q?.den_label,num_label:q?.num_label};
 await frame(label+'-open',info);await sleep(3100);await frame(label+'-collapsed',info);const a=await state();await tap(180,89);await frame(label+'-expanded',info);const b=await state();if(a.moves!==b.moves||a.spent!==b.spent)throw Error('banner click leaked '+label);
}
try{
 for(const entry of index.packs){const pack=JSON.parse(fs.readFileSync(packDir+'/'+entry.file));
  const rank=q=>(q.prompt.match(/[∠△°²∥⊥∽≡×÷]/g)||[]).length*30+(q.prompt.includes('<sup>')?120:0)+Math.min(200,q.prompt.length)/10;
  const q=pack.items.reduce((a,b)=>rank(a)>rank(b)?a:b);await load(pack,q,pack.pack_id);
 }
 const all=index.packs.flatMap(e=>{const p=JSON.parse(fs.readFileSync(packDir+'/'+e.file));return p.items.map(q=>({p,q}));});
 const longest=all.reduce((a,b)=>a.q.prompt.length>b.q.prompt.length?a:b);await load(longest.p,longest.q,'longest-'+longest.q.prompt.length);
 const longChoice=all.filter(x=>x.q.answer_mode==='choice').sort((a,b)=>Math.max(...b.q.choices.map(c=>c.length))-Math.max(...a.q.choices.map(c=>c.length)))[0];await load(longChoice.p,longChoice.q,'long-choice');
 const longPlainChoice=all.filter(x=>x.q.answer_mode==='choice'&&x.q.choices.some(c=>!c.includes('{frac:')&&c.replace(/<[^>]*>/g,'').length>=7&&c.replace(/<[^>]*>/g,'').length<=12)).sort((a,b)=>Math.max(...b.q.choices.map(c=>c.replace(/<[^>]*>/g,'').length))-Math.max(...a.q.choices.map(c=>c.replace(/<[^>]*>/g,'').length)))[0];if(longPlainChoice)await load(longPlainChoice.p,longPlainChoice.q,'long-choice-text-7to12');
 const frac=all.find(x=>x.p.pack_id.startsWith('m2s1')&&x.q.answer_mode==='fraction_parts'&&x.q.den_label==='분모');await load(frac.p,frac.q,'fraction-two-pads');
 const multi=all.find(x=>x.p.pack_id==='m2s2-u4'&&x.q.prompt.length>=150);if(multi)await load(multi.p,multi.q,'geometry-long-150plus');
 const base=JSON.parse(fs.readFileSync(packDir+'/m2s2-u7.json'));let q={...base.items[0],id:'font-diagnostic',prompt:'수학 기호 확인: ∠ △ ° ² ∥ ⊥ ∽ ≡ × ÷. 2<sup>12</sup>와 x<sup>2</sup>의 거듭제곱 표시. 동전 두 면의 수는?',explain:'앞면과 뒷면으로 2가지입니다.'};await load(base,q,'all-symbols-diagnostic');
}catch(e){errors.push(String(e));await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});process.exitCode=1;}
finally{const after=hashes();fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({frames,errors,warnings,before,after,packsUnchanged:JSON.stringify(before)===JSON.stringify(after),build:Object.fromEntries(fs.readdirSync(game+'/Build').map(f=>[f,hash(game+'/Build/'+f)]))},null,2));await browser.close();await server.close();}
