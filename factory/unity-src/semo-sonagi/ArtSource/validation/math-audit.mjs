import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname);
const PUBLIC=path.join(ROOT,'public');
const GAME=path.join(PUBLIC,'g/semo-sonagi');
const PAIRS=['ㄱ·ㄴ','ㄱ·ㄷ','ㄱ·ㄹ','ㄴ·ㄷ','ㄴ·ㄹ','ㄷ·ㄹ'];

function chrome(){
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  for(const bundle of fs.readdirSync(base).sort().reverse())for(const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
  ]){const file=path.join(base,bundle,rel);if(fs.existsSync(file))return file;}
}

function artifactHash(){
  const files=[];(function walk(dir){for(const name of fs.readdirSync(dir).sort()){const file=path.join(dir,name);fs.statSync(file).isDirectory()?walk(file):files.push(file);}})(GAME);
  const hash=crypto.createHash('sha256');for(const file of files){hash.update(path.relative(GAME,file));hash.update(fs.readFileSync(file));}return hash.digest('hex');
}

function audit(problem){
  const area=problem.prompt.includes('정사각형의 넓이');
  const rows=[];const re=/([ㄱㄴㄷㄹ])\s+(\d+)\s*·\s*(\d+)\s*·\s*(\d+)\s*cm(²)?/g;let match;
  while((match=re.exec(problem.prompt))){const values=[+match[2],+match[3],+match[4]].sort((a,b)=>a-b);rows.push({label:match[1],values,right:area?values[0]+values[1]===values[2]:values[0]**2+values[1]**2===values[2]**2});}
  const answer=rows.filter(row=>row.right).map(row=>row.label).join('·');
  const errors=[];
  if(rows.length!==4)errors.push(`candidate-count=${rows.length}`);
  if(rows.filter(row=>row.right).length!==2)errors.push(`truth-count=${rows.filter(row=>row.right).length}`);
  if(answer!==problem.answer)errors.push(`answer ${problem.answer} != ${answer}`);
  if(problem.answerNumeric!==PAIRS.indexOf(answer))errors.push(`answerNumeric ${problem.answerNumeric} != ${PAIRS.indexOf(answer)}`);
  if(!Array.isArray(problem.choices)||problem.choices.join('|')!==PAIRS.join('|'))errors.push('choices');
  if(problem.prompt.includes('√'))errors.push('middle2-root');
  return {id:problem.id,area,answer,errors};
}

const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chrome(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const page=await browser.newPage(),consoleErrors=[];page.on('console',msg=>{if(msg.type()==='error')consoleErrors.push(msg.text());});page.on('pageerror',error=>consoleErrors.push(String(error)));
await page.goto(`${server.url}/g/semo-sonagi/?math-audit=1`,{waitUntil:'load',timeout:45000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:60000});
const problems=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(360));
const rows=problems.map(audit),errors=rows.filter(row=>row.errors.length);
const result={artifactHashSha256:artifactHash(),method:'최종 WebGL이 부팅 때 푸시한 실제 문제은행 360개를 sampleProblems(360)으로 읽고, 표시된 네 자료를 독립 파싱해 Area는 A+B=C, Length는 정수 제곱 a²+b²=c²로 재계산했다.',checked:rows.length,unique:new Set(problems.map(problem=>problem.prompt)).size,area:rows.filter(row=>row.area).length,length:rows.filter(row=>!row.area).length,errors,consoleErrors,forbiddenRoots:problems.filter(problem=>problem.prompt.includes('√')).length,passed:rows.length===360&&errors.length===0&&consoleErrors.length===0};
fs.writeFileSync(new URL('./math-audit-results.json',import.meta.url),JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
await browser.close();await server.close();if(!result.passed)process.exitCode=1;
