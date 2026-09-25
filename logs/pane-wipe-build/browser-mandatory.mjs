import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { spawn } from 'node:child_process';
const out=path.resolve('logs/pane-wipe-build');
const sourceHash=()=>Object.fromEntries(['factory/lib/qa.mjs','factory/lib/firstplay/harness.mjs'].map(f=>[f,crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex')]));
const report={at:new Date().toISOString(),before:sourceHash(),runs:[]};
async function run(name,args,env={}) {
  console.log(`Starting ${name}`);
  const log=fs.createWriteStream(path.join(out,`browser-${name}.txt`));
  const start=Date.now();
  const child=spawn(process.execPath,args,{cwd:process.cwd(),env:{...process.env,...env},stdio:['ignore','pipe','pipe']});
  child.stdout.on('data',d=>{log.write(d);process.stdout.write(d)});
  child.stderr.on('data',d=>{log.write(d);process.stderr.write(d)});
  const code=await new Promise((resolve,reject)=>{child.on('error',reject);child.on('exit',resolve)});
  log.end();
  report.runs.push({name,args,env,code,elapsedMs:Date.now()-start});
  console.log(`Finished ${name}: ${code}`);
  fs.writeFileSync(path.join(out,'browser-command-results.json'),JSON.stringify(report,null,2)+'\n');
}
await run('qa-official',['factory/lib/qa.mjs','pane-wipe']);
await run('firstplay-official',['factory/lib/firstplay/harness.mjs','pane-wipe'],{FIRSTPLAY_PLAY_MS:'45000',FIRSTPLAY_OUT:path.join(out,'browser-firstplay-official')});
await run('qa-file',['--loader','./logs/pane-wipe-build/browser-loader.mjs','factory/lib/qa.mjs','pane-wipe','--out',path.join(out,'browser-qa-file')]);
await run('firstplay-file',['--loader','./logs/pane-wipe-build/browser-loader.mjs','factory/lib/firstplay/harness.mjs','pane-wipe'],{FIRSTPLAY_PLAY_MS:'45000',FIRSTPLAY_OUT:path.join(out,'browser-firstplay-file')});
report.after=sourceHash();
report.gatesUnchanged=JSON.stringify(report.before)===JSON.stringify(report.after);
fs.writeFileSync(path.join(out,'browser-command-results.json'),JSON.stringify(report,null,2)+'\n');
