import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname),PUBLIC=path.join(ROOT,'public');
const OUT=path.join(path.dirname(new URL(import.meta.url).pathname),'pointer-playthrough');fs.mkdirSync(OUT,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
function chromePath(){const b=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const v of fs.readdirSync(b).sort().reverse()){const p=path.join(b,v,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');if(fs.existsSync(p))return p;}}
const bank=new Map();let id=1;
for(let a=1;a<=10;a++)for(let b=1;b<=10;b++)if(a!==b)for(let rev=0;rev<2;rev++)bank.set(`hn-${id++}`,{kind:'parallelogram',a,b,baseK:(a*3+b*5+rev)%2});
for(let a=1;a<=10;a++)for(let rev=0;rev<2;rev++)bank.set(`hn-${id++}`,{kind:'rectangle',a,b:a,baseK:(a+rev*2)%12});
for(let a=1;a<=10;a++)for(let b=1;b<=10;b++)if(a!==b)bank.set(`hn-${id++}`,{kind:'rhombus',a,b,baseK:(a*7+b*3)%12});
for(let a=1;a<=10;a++)bank.set(`hn-${id++}`,{kind:'square',a,b:a,baseK:(a*5)%12});
const mod=x=>((x%12)+12)%12;
function current(s,w){return w==='B'?{x:s.ringBScreenX,y:s.ringBScreenY}:{x:s.ringDScreenX,y:s.ringDScreenY};}
function target(s,w,r,k){const c={x:s.boardCenterScreenX,y:s.boardCenterScreenY},p=current(s,w),cr=w==='B'?s.ringBRadius:s.ringDRadius,scale=Math.hypot(p.x-c.x,p.y-c.y)/cr,a=mod(k)*Math.PI/6;return{x:c.x+scale*r*Math.cos(a),y:c.y-scale*r*Math.sin(a)};}
async function drag(page,a,b){await page.mouse.move(a.x,a.y);await page.mouse.down();await sleep(20);await page.mouse.move(b.x,b.y,{steps:6});await sleep(20);await page.mouse.up();await sleep(35);}
async function place(page,rb,kb,rd,kd){let s=await page.evaluate(()=>window.__GAME_TEST__.getState());await drag(page,current(s,'B'),target(s,'B',rb,kb));s=await page.evaluate(()=>window.__GAME_TEST__.getState());await drag(page,current(s,'D'),target(s,'D',rd,kd));}
function answerFor(s){const p=bank.get(s.problemId);if(!p)throw new Error(`unknown ${s.problemId}`);if(p.kind==='parallelogram')return[p.a,p.baseK+6,p.b,p.baseK+8];if(p.kind==='rectangle')return[p.a,p.baseK+2,p.a,p.baseK+8];const r=p.kind==='square'?p.a:p.b;return[r,p.baseK+3,r,p.baseK+9];}

const server=await serveStatic(PUBLIC),browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--mute-audio']});
const page=await browser.newPage(),errors=[];await page.setViewport({width:390,height:844,deviceScaleFactor:1});
page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(String(e)));
await page.goto(`${server.url}/g/hyeonak-nemo/`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
await page.screenshot({path:path.join(OUT,'00-title.png')});
await page.mouse.click(195,610);await sleep(180);let s=await page.evaluate(()=>window.__GAME_TEST__.getState());
// 연습 정답 설명은 학생이 읽을 수 있도록 2.55초 유지한다. 본문 전환까지 기다린다.
await place(page,4,s.ringBDirection,7,s.ringDDirection);await sleep(2850);
await page.screenshot({path:path.join(OUT,'01-main-start.png')});
// 공개된 두 고리를 실제로 잘못 놓고 같은 문제를 고쳐 회복한다.
s=await page.evaluate(()=>window.__GAME_TEST__.getState());await place(page,1,s.ringBDirection,1,s.ringDDirection);await sleep(1050);
const wrong=await page.evaluate(()=>window.__GAME_TEST__.getState());await page.screenshot({path:path.join(OUT,'02-after-wrong.png')});
if(wrong.lives!==2||wrong.round!==1||wrong.firstAttempts!==1)throw new Error(`wrong path ${JSON.stringify(wrong)}`);
for(let solved=0;solved<6;solved++){
  s=await page.evaluate(()=>window.__GAME_TEST__.getState());const a=answerFor(s);await place(page,...a);await sleep(1350);
  if(solved===0)await page.screenshot({path:path.join(OUT,'03-recovered.png')});
}
const recovered=await page.evaluate(()=>window.__GAME_TEST__.getState());await page.screenshot({path:path.join(OUT,'04-recovery-clear.png')});
if(recovered.phase!=='clear'||recovered.solved!==6||recovered.lives!==2||recovered.firstCorrect<4)throw new Error(`recovery end ${JSON.stringify(recovered)}`);

// 결과 화면 한 탭으로 새 판. 여섯 문항 모두 실제 포인터로 첫 시도 정답.
await page.mouse.click(195,620);await sleep(160);
for(let solved=0;solved<6;solved++){s=await page.evaluate(()=>window.__GAME_TEST__.getState());const a=answerFor(s);await place(page,...a);await sleep(1350);}
const clean=await page.evaluate(()=>window.__GAME_TEST__.getState());await page.screenshot({path:path.join(OUT,'05-clean-clear.png')});
if(clean.phase!=='clear'||clean.solved!==6||clean.lives!==3||clean.firstCorrect!==6)throw new Error(`clean end ${JSON.stringify(clean)}`);
const result={run_id:`hyeonak-nemo-playthrough-${new Date().toISOString()}`,method:'all vertex placements use Chrome real mouse down/move/up',practicePassed:true,recovery:{wrong,recovered},clean,consoleErrors:errors,passed:errors.length===0};
fs.writeFileSync(path.join(path.dirname(new URL(import.meta.url).pathname),'pointer-playthrough-results.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
await page.close();await browser.close();await server.close();if(!result.passed)process.exitCode=1;
