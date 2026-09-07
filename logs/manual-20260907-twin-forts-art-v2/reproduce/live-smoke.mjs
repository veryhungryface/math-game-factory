import fs from 'node:fs';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
const base='https://math-game-factory.vercel.app';
const out='logs/manual-20260907-twin-forts-art-v2';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const report={checked_at:new Date().toISOString(),base,files:[],errors:[],failed_requests:[]};
const catalog=await (await fetch(`${base}/catalog.json`,{cache:'no-store'})).json();
report.catalog_count=catalog.count;report.catalog_entry=catalog.games.find(g=>g.slug==='twin-forts');
if(!report.catalog_entry || report.catalog_entry.version!==2)throw Error('Production catalog not updated');
const manifest=JSON.parse(fs.readFileSync(`${out}/artifact-manifest.json`));
for(const file of [...manifest.files,{path:'meta.json'}]){
 const res=await fetch(`${base}/g/twin-forts/${file.path}?v=2`);
 const bytes=Buffer.from(await res.arrayBuffer());const actual=hash(bytes),expected=hash(fs.readFileSync(`public/g/twin-forts/${file.path}`));
 report.files.push({path:file.path,status:res.status,bytes:bytes.length,sha256:actual,matches_local:actual===expected});
 if(!res.ok||actual!==expected)throw Error(`Deployment mismatch: ${file.path}`);
}
const browser=await puppeteer.launch({headless:true,executablePath:'/Users/sitpo/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',args:['--no-sandbox']});
try{
 const p=await browser.newPage();await p.setViewport({width:390,height:844,deviceScaleFactor:2,isMobile:true,hasTouch:true});
 p.on('pageerror',e=>report.errors.push(String(e)));p.on('console',m=>{if(m.type()==='error')report.errors.push(m.text())});
 p.on('requestfailed',r=>report.failed_requests.push({url:r.url(),error:r.failure()?.errorText}));
 p.on('response',r=>{if(r.status()>=400)report.failed_requests.push({url:r.url(),status:r.status()})});
 await p.goto(`${base}/g/twin-forts/?v=2`,{waitUntil:'networkidle0'});await p.waitForFunction(()=>window.__GAME_TEST__?.ready);
 await p.screenshot({path:`${out}/production-title.png`});await p.tap('#start');await p.screenshot({path:`${out}/production-planning.png`});
 for(let i=0;i<3;i++){await p.tap('#move-right');await new Promise(r=>setTimeout(r,70));}
 report.allocation=await p.evaluate(()=>window.__GAME_TEST__.getState());await p.tap('#deploy');
 await p.waitForFunction(()=>window.__GAME_TEST__.getState().phase==='success',{timeout:10000});
 report.state=await p.evaluate(()=>window.__GAME_TEST__.getState());report.overflow=await p.evaluate(()=>document.documentElement.scrollWidth>innerWidth);
 await p.screenshot({path:`${out}/production-success.png`});
 fs.writeFileSync(`${out}/deployment.json`,JSON.stringify(report,null,2));
 if(report.errors.length||report.failed_requests.length||report.overflow||report.state.solved!==1||report.state.left!==12||report.state.right!==18)throw Error(JSON.stringify(report));
 console.log(JSON.stringify({catalog_count:report.catalog_count,files:report.files.length,phase:report.state.phase,left:report.state.left,right:report.state.right,errors:report.errors,failed_requests:report.failed_requests}));
}finally{await browser.close()}
