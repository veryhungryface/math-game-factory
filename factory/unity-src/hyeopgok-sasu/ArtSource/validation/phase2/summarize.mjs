// Run after final WebGL QA and trusted-pointer capture, from the repository root.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import {fileURLToPath} from 'node:url';

const dir=path.dirname(fileURLToPath(import.meta.url));
const game=path.resolve('public/g/hyeopgok-sasu');
const read=file=>JSON.parse(fs.readFileSync(file,'utf8'));
const sha=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const qa=read(path.join(dir,'qa/report.json'));
const integration=read(path.join(dir,'integration.json'));
const bots=read(path.join(dir,'bot-results.json'));
const verification=read(path.join(dir,'../../packs/verification-report.json'));
const schema=read(path.join(dir,'../../packs/schema-report.json'));
const meta=read(path.join(game,'meta.json'));
const buildLog=process.argv[2]??null;
const buildLine=buildLog?fs.readFileSync(buildLog,'utf8').split('\n').find(line=>line.startsWith('MGF_BUILD_RESULT ')):null;
const build=buildLine?JSON.parse(buildLine.slice('MGF_BUILD_RESULT '.length)):null;
const walk=base=>fs.readdirSync(base,{withFileTypes:true}).sort((a,b)=>a.name.localeCompare(b.name)).flatMap(e=>e.isDirectory()?walk(path.join(base,e.name)):[path.join(base,e.name)]);
const files=walk(game).map(file=>{
  const bytes=fs.readFileSync(file);
  const text=/\.(?:html|js|json|md|css|svg|txt)$/.test(file);
  // Unity payloads are already gzip. PNG files travel unchanged. Text is gzip.
  return {file:path.relative(game,file),bytes:bytes.length,transferBytes:text?zlib.gzipSync(bytes).length:bytes.length,sha256:sha(file)};
});
const currentBuild=Object.fromEntries(files.filter(f=>f.file.startsWith('Build/')).map(f=>[path.basename(f.file),f.sha256]));
const sameBuild=JSON.stringify(currentBuild)===JSON.stringify(integration.buildHashes);
const samePacks=Object.entries(integration.packHashes).every(([id,hash])=>sha(path.join(game,'packs',id+'.json'))===hash);
const covers=integration.covers.map(c=>({...c,matchesPublic:sha(path.join(game,c.file))===c.sha256}));
const frames=integration.frames.map(f=>({file:f.file,matchesCapture:sha(path.join(dir,'screenshots',f.file))===f.sha256}));
const rawBytes=files.reduce((n,f)=>n+f.bytes,0),transferBytes=files.reduce((n,f)=>n+f.transferBytes,0);
const resetQa=meta.version===2&&meta.qa.score===0&&meta.qa.gate===80&&meta.qa.passed===false&&meta.qa.reviewed_at===''&&meta.qa.notes.length===0;
const checks={sameBuild,samePacks,covers: covers.length===2&&covers.every(c=>c.matchesPublic),frames:frames.every(f=>f.matchesCapture),qa:qa.failed===0&&qa.fatal===0,integration:integration.pass===true,firstHook:integration.firstTenSeconds.elapsedMs<=10000,bots:bots.nominalChanceGate&&bots.uniformChanceGate&&bots.controlsPassed&&bots.runtimePackValidationPassed&&bots.modelErrors===0,packMath:verification.verdict==='pass'&&verification.failures===0,packSchema:schema.verdict==='pass',rawUnder12MB:rawBytes<12000000,gzipUnder12MB:transferBytes<12000000,metaQaReset:resetQa};
const summary={time:new Date().toISOString(),scope:'phase2 local only; no push, publication or deployment',checks,pass:Object.values(checks).every(Boolean),buildLog,build,qa:{total:qa.total,passed:qa.passed,fatal:qa.fatal,renderer:qa.render.renderer,mobileFps:qa.perf.mobile.fps,desktop1280Fps:qa.perf.desktop1280.fps,decay:qa.perf.decay},size:{rawBytes,transferBytes,method:'Already-gzipped Unity payloads and PNG unchanged; text gzip. Decimal 12 MB limit.'},integration:{checks:integration.checks.length,firstTenSeconds:integration.firstTenSeconds,runs:integration.runs,fixtures:integration.fixtures,errors:integration.errors,failedRequests:integration.failedRequests},bots:{nominalChanceGate:bots.nominalChanceGate,uniformChanceGate:bots.uniformChanceGate,modelErrors:bots.modelErrors},schema,verification,covers,frames,files};
fs.writeFileSync(path.join(dir,'summary.json'),JSON.stringify(summary,null,2)+'\n');
console.log(JSON.stringify({pass:summary.pass,checks,qa:summary.qa,size:summary.size},null,2));
if(!summary.pass)process.exitCode=1;
