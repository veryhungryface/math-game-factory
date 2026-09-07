import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const game='public/g/sunbasket-farm',out='logs/manual-20260907-sunbasket-farm';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function list(dir,base=''){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?list(path.join(dir,e.name),base+e.name+'/'):[base+e.name]);}
const files=list(game).filter(p=>p!=='meta.json').sort().map(p=>{const b=fs.readFileSync(path.join(game,p));return {path:p,bytes:b.length,sha256:hash(b)};});
const artifact_sha256=hash(files.map(f=>`${f.path}:${f.sha256}`).join('\n'));
fs.writeFileSync(path.join(out,'artifact-manifest.json'),JSON.stringify({run:'manual-20260907-sunbasket-farm',definition:'SHA256 of newline-joined sorted relative_path:sha256 entries; meta.json excluded because publisher updates qa',artifact_sha256,files},null,2)+'\n');
console.log(JSON.stringify({artifact_sha256,files:files.length,bytes:files.reduce((s,f)=>s+f.bytes,0)}));
