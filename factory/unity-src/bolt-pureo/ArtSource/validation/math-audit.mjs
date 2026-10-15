import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT=path.resolve(new URL('../../../../../',import.meta.url).pathname);
const PUBLIC=path.join(ROOT,'public'), GAME=path.join(PUBLIC,'g/bolt-pureo');
function chrome(){const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');for(const b of fs.readdirSync(base).sort().reverse())for(const rel of['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing','chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing']){const p=path.join(base,b,rel);if(fs.existsSync(p))return p;}}
function hash(){const h=crypto.createHash('sha256'),files=[];(function walk(d){for(const n of fs.readdirSync(d).sort()){const f=path.join(d,n);fs.statSync(f).isDirectory()?walk(f):files.push(f);}})(GAME);for(const f of files){h.update(path.relative(GAME,f));h.update(fs.readFileSync(f));}return h.digest('hex');}
const sq=(a,b)=>(a.x-b.x)**2+(a.y-b.y)**2;
const cross=(a,b,p)=>(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
const inside=(v,p)=>{const s=[cross(v[0],v[1],p),cross(v[1],v[2],p),cross(v[2],v[0],p)];return s.every(x=>x>0)||s.every(x=>x<0);};
const gcd=(a,b)=>{a=Math.abs(a);b=Math.abs(b);while(b){[a,b]=[b,a%b];}return a;};
const lcm=(a,b)=>a/gcd(a,b)*b;
function ticks(v,p){const lens=[Math.sqrt(sq(v[0],v[1])),Math.sqrt(sq(v[1],v[2])),Math.sqrt(sq(v[2],v[0]))];if(lens.some(x=>!Number.isInteger(x)))throw Error('비정수 변 길이');const common=lcm(lens[0],lcm(lens[1],lens[2]));const t=[Math.abs(cross(v[0],v[1],p))*common/lens[0],Math.abs(cross(v[1],v[2],p))*common/lens[1],Math.abs(cross(v[2],v[0],p))*common/lens[2]];const d=gcd(t[0],gcd(t[1],t[2]));return d>1?t.map(x=>x/d):t;}
function circum(v,p){const d=v.map(q=>sq(p,q));return d[0]===d[1]&&d[1]===d[2];}
function incenter(v,p){if(!inside(v,p))return false;const c=[Math.abs(cross(v[0],v[1],p)),Math.abs(cross(v[1],v[2],p)),Math.abs(cross(v[2],v[0],p))],l=[sq(v[0],v[1]),sq(v[1],v[2]),sq(v[2],v[0])];return c[0]**2*l[1]===c[1]**2*l[0]&&c[1]**2*l[2]===c[2]**2*l[1];}

const re=/^검사 (\d{3})\. △([A-Z]{3}) ([A-Z])\((-?\d+),(-?\d+)\), ([A-Z])\((-?\d+),(-?\d+)\), ([A-Z])\((-?\d+),(-?\d+)\), 후보 X\((-?\d+),(-?\d+)\)에서 (세 꼭짓점까지의 거리²|세 변까지의 수선 길이의 비)가 (\d+) (?:·|:) (\d+) (?:·|:) (\d+)이다\. X가 (외심|내심) 볼트인지 판별하시오\.$/;
const server=await serveStatic(PUBLIC),browser=await puppeteer.launch({headless:true,executablePath:chrome(),args:['--no-sandbox','--mute-audio']}),page=await browser.newPage(),errors=[];
page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});page.on('pageerror',e=>errors.push(String(e)));
await page.goto(`${server.url}/g/bolt-pureo/?mathaudit=1`,{waitUntil:'load',timeout:60000});await page.waitForFunction(()=>window.__GAME_TEST__?.ready,{timeout:90000});
const bank=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(360));
const ids=new Set(),prompts=new Set(),failures=[];let visibleEvidenceChecked=0,judgmentsChecked=0,obtuseExternalCorrect=0,rightMidpointCorrect=0;
for(let i=0;i<bank.length;i++){
  const q=bank[i],m=q.prompt.match(re);if(!m){failures.push({id:q.id,why:'발문 파싱 실패',prompt:q.prompt});continue;}
  const v=[{x:+m[4],y:+m[5]},{x:+m[7],y:+m[8]},{x:+m[10],y:+m[11]}],p={x:+m[12],y:+m[13]},mode=m[14],shown=[+m[15],+m[16],+m[17]],target=m[18];
  const expectedEvidence=mode.startsWith('세 꼭짓점')?v.map(x=>sq(p,x)):ticks(v,p);
  if(shown.some((x,j)=>x!==expectedEvidence[j]))failures.push({id:q.id,why:'화면 증거 불일치',shown,expectedEvidence});else visibleEvidenceChecked++;
  const correct=target==='외심'?circum(v,p):incenter(v,p),answer=correct?'맞음':'아님';
  if(q.answer!==answer||q.answerNumeric!==(correct?1:0))failures.push({id:q.id,why:'판정 불일치',answer:q.answer,expected:answer});else judgmentsChecked++;
  if(m[2]==='JKL'&&target==='외심'&&correct&&!inside(v,p))obtuseExternalCorrect++;
  if((m[2]==='ABC'||m[2]==='PQR')&&target==='외심'&&correct){const rv=v.findIndex((x,j)=>{const a=v[(j+1)%3],b=v[(j+2)%3];return (a.x-x.x)*(b.x-x.x)+(a.y-x.y)*(b.y-x.y)===0;});if(rv>=0){const a=v[(rv+1)%3],b=v[(rv+2)%3];if(p.x*2===a.x+b.x&&p.y*2===a.y+b.y)rightMidpointCorrect++;}}
  if(ids.has(q.id))failures.push({id:q.id,why:'중복 id'});else ids.add(q.id);
  prompts.add(q.prompt);
}
const uniqueIds=new Set(bank.map(q=>q.id)).size,uniquePrompts=prompts.size;
const groups=new Map();for(const q of bank){const g=Math.floor((Number(q.id.split('-').at(-1))-1)/12);groups.set(g,(groups.get(g)||0)+(q.answer==='맞음'?1:0));}for(const [g,n] of groups)if(n!==1)failures.push({group:g,why:`정답 후보 ${n}개`});
const result={runId:`bolt-pureo-math-audit-${new Date().toISOString()}`,artifactHashSha256:hash(),method:'최종 WebGL ProblemBankJson 360문항의 공개 좌표·측정값을 C#과 독립된 JS 정수 외적/거리제곱으로 전수 재계산',bankProblems:bank.length,uniqueIds,uniquePrompts,visibleEvidenceChecked,judgmentsChecked,groupsChecked:bank.length/12,obtuseExternalCorrect,rightMidpointCorrect,bannedSqrtInGeneratedPrompts:bank.filter(q=>q.prompt.includes('√')).length,consoleErrors:errors,failures,passed:bank.length===360&&uniqueIds===360&&uniquePrompts===360&&visibleEvidenceChecked===360&&judgmentsChecked===360&&obtuseExternalCorrect>0&&rightMidpointCorrect>0&&errors.length===0&&failures.length===0};
fs.writeFileSync(new URL('./math-audit-results.json',import.meta.url),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result,null,2));await page.close();await browser.close();await server.close();if(!result.passed)process.exitCode=1;
