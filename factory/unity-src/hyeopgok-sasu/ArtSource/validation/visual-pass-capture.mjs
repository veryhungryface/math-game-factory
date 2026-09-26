import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

// Read-only game capture. All evidence stays outside factory/work.
const root=process.cwd();
const phase=process.argv[2] || 'final';
if(!/^[a-z0-9-]+$/.test(phase)) throw new Error('Invalid evidence folder');
const base=path.join(root,'factory/unity-src/hyeopgok-sasu/ArtSource/validation/visual-pass');
const out=path.join(base,phase);
fs.mkdirSync(out,{recursive:true});
const server=await serveStatic(path.join(root,'public'));
const executablePath=process.env.PUPPETEER_EXECUTABLE_PATH || path.join(process.env.HOME,'.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const browser=await puppeteer.launch({headless:true,executablePath,args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage();
const errors=[],failedRequests=[],states=[];
page.on('pageerror',e=>errors.push(String(e)));
page.on('console',m=>{if(m.type()==='error') errors.push(m.text());});
page.on('requestfailed',r=>failedRequests.push({url:r.url(),error:r.failure()?.errorText}));
const wait=ms=>new Promise(r=>setTimeout(r,ms));
const capture=async name=>{
  const file=name+'.png';
  const state=await page.evaluate(()=>window.__GAME_TEST__.getState());
  await page.screenshot({path:path.join(out,file)});
  states.push({file,state});
};
const sizes=[{width:390,height:844,isMobile:true,hasTouch:true,label:'390'},
 {width:1280,height:800,isMobile:false,hasTouch:false,label:'1280'}];
if(phase!=='baseline') sizes.push(
 {width:1200,height:630,isMobile:false,hasTouch:false,label:'cover-wide'},
 {width:1080,height:1080,isMobile:false,hasTouch:false,label:'cover-square'});
try {
 for(const size of sizes) {
  const {label,...viewport}=size;
  await page.setViewport({...viewport,deviceScaleFactor:1});
  await page.goto(server.url+'/g/hyeopgok-sasu/',{waitUntil:'networkidle0',timeout:45000});
  await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:45000});
  await wait(1300);
  await capture('title-'+label);
  await page.evaluate(()=>window.__GAME_TEST__.start());
  await wait(1200); await capture('playing-'+label);
  await wait(5800); await capture('battle-'+label);
  await page.evaluate(()=>window.__GAME_TEST__.answerCorrect());
  await wait(85); await capture('impact-early-'+label);
  await wait(165); await capture('impact-mid-'+label);
  await wait(270); await capture('impact-late-'+label);
  await wait(1400); await capture('reward-'+label);
 }
 const files=fs.readdirSync(path.join(root,'public/g/hyeopgok-sasu/Build')).sort();
 const buildHashes=Object.fromEntries(files.map(file=>[file,crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'public/g/hyeopgok-sasu/Build',file))).digest('hex')]));
 const renderer=await page.evaluate(()=>{
  const c=document.querySelector('canvas'),g=c.getContext('webgl2')||c.getContext('webgl');
  const x=g?.getExtension('WEBGL_debug_renderer_info');
  return x?g.getParameter(x.UNMASKED_RENDERER_WEBGL):'unknown';
 });
 const report={phase,capturedAt:new Date().toISOString(),renderer,buildHashes,errors,failedRequests,states};
 fs.writeFileSync(path.join(out,'capture-report.json'),JSON.stringify(report,null,2)+'\n');
 console.log(JSON.stringify({out,renderer,errors,failedRequests,frames:states.length}));
} finally {await browser.close();await server.close();}
