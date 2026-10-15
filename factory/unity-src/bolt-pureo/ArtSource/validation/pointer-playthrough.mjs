import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname),PUBLIC=path.join(ROOT,'public'),GAME=path.join(PUBLIC,'g/bolt-pureo'),OUT=path.dirname(new URL(import.meta.url).pathname);
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
function chrome(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const b of fs.readdirSync(base).sort().reverse())for(const rel of['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing']){const p=path.join(base,b,rel);if(fs.existsSync(p))return p;}}
function hash(){const h=crypto.createHash('sha256'),files=[];(function walk(d){for(const n of fs.readdirSync(d).sort()){const f=path.join(d,n);fs.statSync(f).isDirectory()?walk(f):files.push(f);}})(GAME);for(const f of files){h.update(path.relative(GAME,f));h.update(fs.readFileSync(f));}return h.digest('hex');}
async function state(page){return page.evaluate(()=>window.__GAME_TEST__.getState());}
const points=s=>(s.hitMap||'').split(';').filter(Boolean).map(x=>x.split(',').map(Number));
async function evidence(page,i){const s=await state(page),p=points(s)[i];await page.mouse.move(p[0],p[1]);await page.mouse.down();await sleep(90);await page.mouse.up();await sleep(55);return state(page);}
async function submit(page,i){const s=await state(page),p=points(s)[i];await page.mouse.move(p[0],p[1]);await page.mouse.down();await sleep(310);await page.mouse.up();await sleep(90);}
function isVisibleMatch(s){const nums=(s.evidence||'').match(/\d+/g)?.slice(-3).map(Number)||[];const prefix=s.requirement==='외심'?'세 꼭짓점까지의 거리²':'세 변까지의 수선 길이의 비';return (s.evidence||'').startsWith(prefix)&&nums.length===3&&nums[0]===nums[1]&&nums[1]===nums[2];}
async function find(page,want){const before=await state(page),ps=points(before),seen=[];for(let i=0;i<ps.length;i++){const s=await evidence(page,i);seen.push({i,p:ps[i],e:s.evidence});if(isVisibleMatch(s)===want)return i;}throw Error(`화면 증거로 ${want?'정답':'오답'} 후보를 찾지 못함 ${JSON.stringify({phase:before.phase,requirement:before.requirement,triangle:before.triangleGeometry,hitMap:before.hitMap,seen})}`);}
async function solveRound(page,wantCorrect=true){const i=await find(page,wantCorrect);await submit(page,i);await sleep(1650);return i;}
async function finishRun(page,{wrongFirst=false}={}){const order=[];if(wrongFirst){order.push(await solveRound(page,false));await sleep(100);order.push(await solveRound(page,true));}else order.push(await solveRound(page,true));for(let r=1;r<6;r++)order.push(await solveRound(page,true));return{order,end:await state(page)};}

const server=await serveStatic(PUBLIC),browser=await puppeteer.launch({headless:true,executablePath:chrome(),args:['--no-sandbox','--mute-audio']}),page=await browser.newPage(),errors=[];
await page.setViewport({width:390,height:844,deviceScaleFactor:1});page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(String(e)));
await page.goto(`${server.url}/g/bolt-pureo/?playthrough=fix1`,{waitUntil:'load',timeout:60000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:90000});
await page.evaluate(()=>window.__GAME_TEST__.start());await sleep(120);await page.screenshot({path:path.join(OUT,'fix1-00-run1-start.png')});
const wrong=await find(page,false);await submit(page,wrong);await sleep(1250);const afterWrong=await state(page);await page.screenshot({path:path.join(OUT,'fix1-01-run1-wrong.png')});
const run1=await finishRun(page);await page.screenshot({path:path.join(OUT,'fix1-02-run1-clear.png')});
// 결과 화면의 실제 재시작 입력. 훅으로 새 판을 열지 않는다.
await page.mouse.click(195,680);await sleep(160);const practiceStart=await state(page);
// 연습의 흰 링은 계약상 6번째 볼트에 고정된다. 후보 탐색용 짧은 탭을 세 번 하면
// 새 접근성 시범이 시작되므로, 링이 가리키는 볼트를 실제 포인터로 바로 길게 누른다.
const practiceCorrect=5;await submit(page,practiceCorrect);await sleep(1650);const afterPractice=await state(page);await page.screenshot({path:path.join(OUT,'fix1-03-run2-first.png')});
// 연습 직후 첫 본문항도 화면 증거를 직접 열어 실제 포인터로 제출한 프레임을 남긴다.
const run2Order=[await solveRound(page,true)];const run2FirstSubmit=await state(page);await page.screenshot({path:path.join(OUT,'fix2-04-run2-first-submit.png')});
for(let r=1;r<6;r++)run2Order.push(await solveRound(page,true));const run2={order:run2Order,end:await state(page)};await page.screenshot({path:path.join(OUT,'fix1-04-run2-clear.png')});
const artifactHashSha256=hash(),result={runId:`bolt-pureo-fix1-two-runs-${new Date().toISOString()}`,artifactHashSha256,method:'390×844 최종 WebGL. 첫 판은 화면의 실제 거리/수선 증거를 짧게 열어 비교하고 실제 310ms down/up으로 첫 오답 뒤 회복했다. 결과 화면을 실제 클릭해 재시작하고 연습도 실제 pointer로 통과한 뒤 별도 두 번째 판을 끝까지 완주했다. answerCorrect/answerWrong은 사용하지 않았다.',afterWrong,run1,practiceStart,practiceCorrect,afterPractice,run2FirstSubmit,run2,errors,passed:afterWrong.solved===0&&afterWrong.firstCorrect===0&&afterWrong.firstAttempted===1&&run1.end.phase==='clear'&&run1.end.solved===6&&run1.end.firstCorrect===5&&practiceStart.onboarding===true&&afterPractice.onboarding===false&&run2FirstSubmit.solved===1&&run2FirstSubmit.firstAttempted===1&&run2FirstSubmit.firstCorrect===1&&run2.end.phase==='clear'&&run2.end.solved===6&&run2.end.firstCorrect===6&&run2.end.firstAttempted===6&&JSON.stringify(run1.order)!==JSON.stringify(run2.order)&&errors.length===0};
fs.writeFileSync(new URL('./pointer-playthrough-results.json',import.meta.url),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result,null,2));await page.close();await browser.close();await server.close();if(!result.passed)process.exitCode=1;
