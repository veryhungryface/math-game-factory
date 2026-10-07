import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const PUBLIC=path.join(ROOT,'public');
const GAME=path.join(PUBLIC,'g/jegop-hwaljuro');
const OUT=path.join(HERE,'pointer-playthrough');
fs.mkdirSync(OUT,{recursive:true});

function chromePath(){
  if(process.env.PUPPETEER_EXECUTABLE_PATH)return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  for(const build of fs.readdirSync(base).sort().reverse())for(const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing']){
      const p=path.join(base,build,rel);if(fs.existsSync(p))return p;
    }
  throw new Error('Chrome for Testing not found');
}
function buildHash(){
  const h=crypto.createHash('sha256');
  for(const name of ['index.html',...fs.readdirSync(path.join(GAME,'Build')).sort().map(x=>'Build/'+x)])h.update(fs.readFileSync(path.join(GAME,name)));
  return h.digest('hex');
}
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
async function drag(page,a,b){
  await page.mouse.move(a[0],a[1]);await page.mouse.down();
  await page.mouse.move((a[0]+b[0])/2,(a[1]+b[1])/2,{steps:10});
  await page.mouse.move(b[0],b[1],{steps:10});await sleep(100);await page.mouse.up();
}
async function state(page){return page.evaluate(()=>window.__GAME_TEST__.getState());}
async function waitFor(page,predicate,timeout=3500){
  const end=Date.now()+timeout;let s;
  while(Date.now()<end){s=await state(page);if(predicate(s))return s;await sleep(100);}
  throw new Error('state timeout: '+JSON.stringify(s));
}

const panels=[[103,536],[195,536],[287,536],[147,477],[242,477]];
const pairSlots=[[0,1],[0,2],[0,3],[0,4],[1,2],[1,3],[1,4],[2,3],[2,4],[3,4]];
const log=[];
const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-setuid-sandbox']});
try{
  // 표지는 사진풍 별도 아트가 아니라 검증한 같은 WebGL 타이틀 장면을 그대로 쓴다.
  const cover=await browser.newPage();
  await cover.setViewport({width:1200,height:630,deviceScaleFactor:1});
  await cover.goto(server.url+'/g/jegop-hwaljuro/',{waitUntil:'networkidle2',timeout:45000});
  await cover.waitForFunction('window.__GAME_TEST__&&window.__GAME_TEST__.ready===true',{timeout:20000});
  await sleep(450);await cover.screenshot({path:path.join(GAME,'thumb.png')});
  await cover.setViewport({width:1080,height:1080,deviceScaleFactor:1});await sleep(450);
  await cover.screenshot({path:path.join(GAME,'square.png')});await cover.close();

  const page=await browser.newPage();
  await page.setViewport({width:390,height:844,deviceScaleFactor:1,isMobile:true,hasTouch:true});
  const errors=[];page.on('console',m=>{if(m.type()==='error')errors.push(m.text())});page.on('pageerror',e=>errors.push(String(e)));
  await page.goto(server.url+'/g/jegop-hwaljuro/',{waitUntil:'networkidle2',timeout:45000});
  await page.waitForFunction('window.__GAME_TEST__&&window.__GAME_TEST__.ready===true',{timeout:20000});
  await page.screenshot({path:path.join(OUT,'00-title.png')});

  await page.mouse.click(195,420);await sleep(500);
  log.push({action:'title-center-tap',state:await state(page)});
  await page.screenshot({path:path.join(OUT,'01-practice-guide.png')});

  // 1차 빌드에서는 이 빈 탭/오드래그가 정답 쌍으로 치환됐다. 같은 입력이 이제
  // 연습을 유지하는지 먼저 증명한다.
  await page.mouse.click(350,700);await sleep(350);
  let invalid=await state(page);
  log.push({action:'practice-blank-tap-must-not-solve',at:[350,700],state:invalid});
  if(!invalid.onboarding||invalid.batteryReady||invalid.practiceStep!=='merge-panels')throw new Error('blank tap auto-solved practice: '+JSON.stringify(invalid));
  await drag(page,panels[0],[195,650]);await sleep(350);
  invalid=await state(page);
  log.push({action:'practice-incomplete-drag-must-not-solve',from:panels[0],to:[195,650],state:invalid});
  if(!invalid.onboarding||invalid.batteryReady||invalid.practiceStep!=='merge-panels')throw new Error('incomplete drag auto-solved practice: '+JSON.stringify(invalid));

  await drag(page,panels[0],panels[1]);
  await waitFor(page,s=>s.batteryReady===true&&s.practiceStep==='dock-plate');
  log.push({action:'practice-drag-9-to-16',from:panels[0],to:panels[1],state:await state(page)});
  await page.screenshot({path:path.join(OUT,'02-practice-merged.png')});

  async function dockBattery(label,solvedBefore,onboardingBefore){
    const starts=[[195,430],[195,450],[195,470]];
    const ends=[[195,315],[195,335],[195,355],[250,330]];
    for(const a of starts)for(const b of ends){
      await drag(page,a,b);await sleep(250);const s=await state(page);
      if((onboardingBefore&&s.practiceStep==='reveal')||(!onboardingBefore&&s.solved>solvedBefore)){
        log.push({action:label,from:a,to:b,state:s});return s;
      }
    }
    throw new Error('battery dock failed: '+JSON.stringify(await state(page)));
  }
  // 홈 밖에 놓으면 자동 도킹하지 않고 두 번째 실제 입력을 계속 기다린다.
  await drag(page,[195,430],[80,700]);await sleep(350);
  invalid=await state(page);log.push({action:'practice-misdock-must-not-finish',state:invalid});
  if(!invalid.onboarding||invalid.practiceStep!=='dock-plate')throw new Error('misdock auto-finished practice: '+JSON.stringify(invalid));
  const practiceReveal=await dockBattery('practice-battery-dock',0,true);
  await page.screenshot({path:path.join(OUT,'02b-practice-equation-reveal.png')});
  await sleep(1400);const firstRunStart=await state(page);
  if(practiceReveal.practiceStep!=='reveal'||firstRunStart.onboarding)throw new Error('practice reveal timing failed: '+JSON.stringify({practiceReveal,firstRunStart}));
  await page.screenshot({path:path.join(OUT,'03-first-problem.png')});

  // 첫 실전 문제의 정답 위치는 slot 1([0,2]). 먼저 slot 0([0,1])을 실제 드래그해 회복성을 확인한다.
  await drag(page,panels[0],panels[1]);
  let s=await waitFor(page,x=>x.lives===2&&x.wasteBlocks===1);
  log.push({action:'first-problem-wrong-pointer',from:panels[0],to:panels[1],state:s});
  await sleep(1400);await page.screenshot({path:path.join(OUT,'04-waste-persists.png')});

  // 같은 문항 정답 뒤 나머지 여섯 문항까지 모두 실제 pointer pair merge + battery dock으로 완주한다.
  const correctSlots=[1,3,5,7,9,1,3];
  for(let solved=0;solved<7;solved++){
    const [i,j]=pairSlots[correctSlots[solved]];
    await drag(page,panels[i],panels[j]);
    await waitFor(page,x=>x.batteryReady===true);
    log.push({action:'correct-pair-'+(solved+1),pair:[i,j],from:panels[i],to:panels[j],state:await state(page)});
    await dockBattery('battery-dock-'+(solved+1),solved,false);
    await sleep(solved===6?500:1300);
  }
  s=await state(page);await page.screenshot({path:path.join(OUT,'05-clear-after-recovery.png')});
  if(s.phase!=='clear'||s.solved!==7||s.lives!==2||s.wasteBlocks!==1)throw new Error('recovery clear failed: '+JSON.stringify(s));

  // 두 번째 판도 실제 탭→연습 드래그→도킹으로 들어가며, runSerial 2의 첫 정답 위치는 slot 2([0,3])로 달라진다.
  await page.mouse.click(195,420);await sleep(500);
  await drag(page,panels[0],panels[1]);await waitFor(page,x=>x.batteryReady===true);
  await dockBattery('second-practice-battery-dock',0,true);await sleep(1400);const retryStart=await state(page);
  if(retryStart.problemId===firstRunStart.problemId)throw new Error('second problem did not change: '+JSON.stringify(retryStart));
  await drag(page,panels[0],panels[3]);await waitFor(page,x=>x.batteryReady===true);
  const retry=await dockBattery('second-run-first-correct',0,false);await sleep(400);
  await page.screenshot({path:path.join(OUT,'06-second-run.png')});
  if(retry.phase!=='playing'||retry.solved!==1)throw new Error('second run pointer solve failed: '+JSON.stringify(retry));
  log.push({action:'second-run-different-pair',pair:[0,3],state:retry});

  const out={generated_at:new Date().toISOString(),build_sha256:buildHash(),viewport:'390x844',input:'Puppeteer mouse down/move/up on Unity canvas',covers:'same runtime title at 1200x630 and 1080x1080',console_errors:errors,final_state:s,second_run_state:retry,actions:log,passed:errors.length===0};
  fs.writeFileSync(path.join(HERE,'pointer-playthrough-results.json'),JSON.stringify(out,null,2)+'\n');
  console.log(JSON.stringify(out,null,2));if(!out.passed)process.exitCode=1;
}finally{await browser.close();await server.close();}
