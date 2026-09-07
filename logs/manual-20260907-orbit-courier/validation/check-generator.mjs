import fs from 'node:fs';
import {createHash} from 'node:crypto';
import * as game from '../../../public/g/orbit-courier/math.mjs';
import {checkSamples} from './verify-math.mjs';
const pool=[];
for(let a=100;a<=329;a++)for(let b=2;b<=6;b++)if(BigInt(a)*BigInt(b)<=999n)pool.push(game.makeProblem(a,b,`independent-${a}-${b}`));
const samples=game.sampleProblems(2000);
const reports={source_sha256:createHash('sha256').update(fs.readFileSync('public/g/orbit-courier/math.mjs')).digest('hex'),all_declared_operands:checkSamples(pool),samples:checkSamples(samples),sample_lengths:[0,17,40,63,2000].map(n=>({requested:n,received:game.sampleProblems(n).length})),campaign_verified:false};
if(game.makeCampaign){
  const campaigns=Array.from({length:1000},()=>game.makeCampaign());
  reports.campaigns=checkSamples(campaigns.flat());
  reports.campaign_count=campaigns.length;
  reports.campaign_lengths=[...new Set(campaigns.map(c=>c.length))];
  reports.random_mission_unique=Array.from({length:9},(_,i)=>new Set(campaigns.map(c=>`${c[i].a}×${c[i].b}`)).size);
  reports.campaign_verified=true;
}
fs.writeFileSync(new URL('generator-result.json',import.meta.url),JSON.stringify(reports,null,2));
console.log(JSON.stringify(reports,null,2));
if(Object.values(reports).some(r=>r?.errors?.length))process.exitCode=1;
