import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname),PUBLIC=path.join(ROOT,'public');
const OUT=path.join(path.dirname(new URL(import.meta.url).pathname),'pointer-playthrough');fs.mkdirSync(OUT,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
function chromePath(){const b=path.join(process.env.HOME||'','.cache/puppeteer/chrome');if(!fs.existsSync(b))return undefined;for(const v of fs.readdirSync(b).sort().reverse()){const p=path.join(b,v,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');if(fs.existsSync(p))return p;}}
const snapFromId=id=>{if(id==='practice-fixed')return 0;const n=Number(id.split('-')[1])-1;return -12+Math.floor((n%125)/5);};
function pointAt(s,snap){const t=(snap+12)/24;return{x:s.railStartScreenX+(s.railEndScreenX-s.railStartScreenX)*t,y:s.railStartScreenY+(s.railEndScreenY-s.railStartScreenY)*t};}
async function drag(page,s,snap){const a={x:s.handleScreenX,y:s.handleScreenY},b=pointAt(s,snap);await page.mouse.move(a.x,a.y);await page.mouse.down();await sleep(18);await page.mouse.move(b.x,b.y,{steps:7});await sleep(18);await page.mouse.up();await sleep(90);}
async function state(page){return page.evaluate(()=>window.__GAME_TEST__.getState());}
async function waitForReveal(page){await page.waitForFunction(()=>window.__GAME_TEST__.getState().phase!=='reveal',{timeout:3000});}
async function solveCurrent(page,wrong=false){let s=await state(page);const k=snapFromId(s.problemId);await drag(page,s,wrong?(k===12?11:k+1):k);await waitForReveal(page);return state(page);}

const server=await serveStatic(PUBLIC),browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const page=await browser.newPage(),errors=[];await page.setViewport({width:390,height:844,deviceScaleFactor:1});
page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(String(e)));
await page.goto(`${server.url}/g/seongnyu-core/`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
await page.screenshot({path:path.join(OUT,'00-title.png')});
// 세계 속 유리 손잡이를 실제로 누른다. 실패 시 주변 탭의 거절 피드백도 확인한다.
await page.mouse.click(30,420);await sleep(120);await page.mouse.click(195,585);await sleep(220);
let s=await state(page);if(s.phase!=='practice')throw new Error(`title did not enter practice: ${JSON.stringify(s)}`);
await page.screenshot({path:path.join(OUT,'01-practice-guide.png')});
await drag(page,s,0);await sleep(1200);s=await state(page);if(s.phase!=='playing')throw new Error(`practice did not finish: ${JSON.stringify(s)}`);
await page.screenshot({path:path.join(OUT,'02-first-problem.png')});

// 첫 일반 실수 뒤 같은 씨방을 고치고도 목표에 도달하는 경로.
let afterWrong=await solveCurrent(page,true);await page.screenshot({path:path.join(OUT,'03-wrong-scar.png')});
if(afterWrong.lives!==2||afterWrong.firstAttempts!==1||afterWrong.solved!==0)throw new Error(`wrong path ${JSON.stringify(afterWrong)}`);
for(let i=0;i<8;i++){s=await state(page);if(s.phase==='clear')break;await solveCurrent(page,false);}
const recovered=await state(page);await page.screenshot({path:path.join(OUT,'04-recovery-clear.png')});
if(recovered.phase!=='clear'||recovered.solved!==8||recovered.lives!==2||recovered.firstCorrect!==7)throw new Error(`recovery end ${JSON.stringify(recovered)}`);

// 한 탭 재시작 뒤 무실수 실제 포인터 완주.
await page.mouse.click(195,610);await sleep(180);
for(let i=0;i<8;i++)await solveCurrent(page,false);
const clean=await state(page);await page.screenshot({path:path.join(OUT,'05-clean-clear.png')});
if(clean.phase!=='clear'||clean.solved!==8||clean.lives!==3||clean.firstCorrect!==8)throw new Error(`clean end ${JSON.stringify(clean)}`);

// 결과 화면 재시작 뒤 오답 3회로 즉시 판 종료.
await page.mouse.click(195,610);await sleep(180);
for(let i=0;i<3;i++){s=await state(page);const k=snapFromId(s.problemId);let wrong=k+i+1;while(wrong>12)wrong-=25;await drag(page,s,wrong);await waitForReveal(page);if((await state(page)).phase==='gameover')break;}
const gameover=await state(page);await page.screenshot({path:path.join(OUT,'06-gameover.png')});
if(gameover.phase!=='gameover'||gameover.lives!==0)throw new Error(`gameover path ${JSON.stringify(gameover)}`);

const result={run_id:`seongnyu-core-playthrough-${new Date().toISOString()}`,method:'all title/practice/answer/restart actions use Chrome real mouse down/move/up; answer snap is reconstructed from the same public integer generator, not test answer hooks',practicePassed:true,recovered,clean,gameover,consoleErrors:errors,passed:errors.length===0};
fs.writeFileSync(path.join(path.dirname(new URL(import.meta.url).pathname),'pointer-playthrough-results.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
await page.close();await browser.close();await server.close();if(!result.passed)process.exitCode=1;
