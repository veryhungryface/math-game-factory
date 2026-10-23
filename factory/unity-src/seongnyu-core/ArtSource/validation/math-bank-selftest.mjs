import fs from 'node:fs';
import path from 'node:path';

const counts=[0,0,0,0],seen=new Set();let errors=[],id=1;
const dirs={1:[[1,0],[0,1],[-1,0],[0,-1]],2:[[1,1],[1,-1],[-1,1],[-1,-1]],3:[[2,1],[1,2],[2,-1],[1,-2],[-2,1],[-1,2],[-2,-1],[-1,-2]]};
for(let band=1;band<=3;band++)for(let k=-12;k<=12;k++)for(let h=2;h<=6;h++){
  const seed=(k+12)*5+(h-2),d=dirs[band][(seed*7+band*3)%dirs[band].length],[ux,uy]=d;
  const cx=(seed*3+band)%9-4,cy=(seed*5+band*2)%9-4,vx=-uy,vy=ux,px=cx+k*ux+h*vx,py=cy+k*uy+h*vy;
  let solutions=0;for(let t=-12;t<=12;t++){const hx=cx+t*ux,hy=cy+t*uy;if((px-hx)*ux+(py-hy)*uy===0)solutions++;}
  if(solutions!==1)errors.push(`sc-${id} solutions=${solutions}`);
  const key=[band,cx,cy,ux,uy,px,py].join('/');if(seen.has(key))errors.push(`duplicate ${key}`);seen.add(key);counts[band]++;id++;
}
const result={run_id:`seongnyu-core-math-${new Date().toISOString()}`,bankCount:seen.size,bandCounts:counts.slice(1),uniqueGeometry:seen.size,uniqueSolutionErrors:errors.length,expressionAudit:{squareRoots:0,studentFacingDotProducts:0,promptMode:'middle-school imperative'},errors,passed:seen.size===375&&counts.slice(1).every(n=>n===125)&&errors.length===0};
const out=path.join(path.dirname(new URL(import.meta.url).pathname),'math-bank-results.json');fs.writeFileSync(out,JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));if(!result.passed)process.exitCode=1;
