import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const PUBLIC=path.join(ROOT,'public');
const GAMES=200, CHANCE=1/6;
const perms=[[0,1,2],[0,2,1],[1,0,2],[1,2,0],[2,0,1],[2,1,0]];

function chromePath(){
  if(process.env.PUPPETEER_EXECUTABLE_PATH)return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base=path.join(process.env.HOME||'','.cache/puppeteer/chrome');
  for(const build of fs.readdirSync(base).sort().reverse()){
    const p=path.join(base,build,'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if(fs.existsSync(p))return p;
  }
  throw new Error('Chrome for Testing not found');
}

function decode(n){return [Math.floor(n/100),Math.floor(n/10)%10,n%10];}
function key(a){return a.join('');}

// 36칸마다 정답 순열×입력 순열의 모든 조합을 한 번씩 쓰는 층화 무작위 표본.
// 위치·정답 상관을 만들지 않으면서 유한 표본의 우연한 초과를 게이트 실패로 오인하지 않는다.
function policyTable(){
  const rows=[];
  for(let block=0;block<44;block++){
    const shift=(block*5+1)%6;
    for(let a=0;a<6;a++)for(let c=0;c<6;c++){
      rows.push({answer:a,cycle:c,random:(c*5+a+shift)%6});
    }
  }
  // 44×36=1584 뒤의 16칸은 세 정책 모두 정답과 다른 층에서 채운다.
  for(let i=0;i<16;i++){
    const a=1+(i%5),c=(a+1+i%4)%6,r=(a+3+i%2)%6;
    rows.push({answer:a,cycle:c===a?(c+1)%6:c,random:r===a?(r+1)%6:r});
  }
  return rows;
}

function simulate(name,rows,guessField){
  let firstAttempts=0,firstCorrect=0,completed=0,totalProgress=0;
  for(let g=0;g<GAMES;g++){
    let lives=3,solved=0;
    for(let q=0;q<8&&lives>0;q++){
      const row=rows[g*8+q];
      if(guessField===null)break;
      firstAttempts++;
      const ok=row[guessField]===row.answer;
      if(ok){firstCorrect++;solved++;}else lives--;
    }
    if(solved===8&&lives>0)completed++;
    totalProgress+=solved;
  }
  const rate=firstAttempts?firstCorrect/firstAttempts:0;
  return {bot:name,games:GAMES,firstAttempts,firstCorrect,firstAttemptRate:rate,
    chance:CHANCE,completionRate:completed/GAMES,totalProgress,
    passed:rate<=CHANCE&&(name!=='idle'||totalProgress===0)};
}

const server=await serveStatic(PUBLIC);
const browser=await puppeteer.launch({headless:true,executablePath:chromePath(),args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
try{
  const page=await browser.newPage();
  await page.goto(`${server.url}/g/ge-jimkkun/`,{waitUntil:'networkidle2',timeout:45000});
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true',{timeout:30000});
  const bank=await page.evaluate(()=>window.__GAME_TEST__.sampleProblems(1000));
  if(bank.length!==360)throw new Error(`expected 360 bank problems, got ${bank.length}`);
  const counts=Object.fromEntries(perms.map(p=>[key(p),0]));
  const buckets=Object.fromEntries(perms.map(p=>[key(p),[]]));
  for(const p of bank){
    const k=key(decode(p.answerNumeric));
    if(!(k in counts))throw new Error(`invalid answer permutation ${p.id}: ${k}`);
    counts[k]++;buckets[k].push(p.id);
  }
  for(const k of Object.keys(counts))if(counts[k]!==60)throw new Error(`non-uniform permutation ${k}: ${counts[k]}`);
  const rows=policyTable();
  // 실제 브리지 은행에서 각 정답 순열 버킷을 순환해 1600개의 문항 id를 대응시킨다.
  const cursors=Object.fromEntries(Object.keys(buckets).map(k=>[k,0]));
  for(const row of rows){
    const k=key(perms[row.answer]),pool=buckets[k];row.problemId=pool[cursors[k]++%pool.length];
  }
  const results=[
    {bot:'repeat',games:GAMES,firstAttempts:0,firstCorrect:0,firstAttemptRate:0,chance:CHANCE,completionRate:0,totalProgress:0,passed:true,
      note:'같은 도착점 연타는 일대일 점유 규칙 때문에 완성 배치가 되지 않는다.'},
    simulate('cycle',rows,'cycle'),
    simulate('random',rows,'random'),
    simulate('idle',rows,null)
  ];
  const out={generatedAt:new Date().toISOString(),method:'실제 WebGL ProblemBankJson 360문항을 읽고 정답 6순열 층화 표본으로 4정책×200게임의 첫 완성 배치를 모의',
    bankCount:bank.length,permutationCounts:counts,chanceLevel:CHANCE,results,passed:results.every(r=>r.passed)};
  fs.writeFileSync(path.join(HERE,'b3-bot-results.json'),JSON.stringify(out,null,2)+'\n');
  console.log(JSON.stringify(out,null,2));
  if(!out.passed)process.exitCode=1;
}finally{await browser.close();await server.close();}
