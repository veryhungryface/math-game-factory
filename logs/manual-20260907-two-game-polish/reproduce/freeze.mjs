import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const slug=process.argv[2];assert.ok(['twin-forts','sunbasket-farm'].includes(slug));
const game=`public/g/${slug}`,out=`logs/manual-20260907-two-game-polish/${slug}`;fs.mkdirSync(out,{recursive:true});
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
function list(dir,base=''){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?list(path.join(dir,e.name),base+e.name+'/'):[base+e.name]);}
const files=list(game).filter(p=>p!=='meta.json').sort().map(p=>{const b=fs.readFileSync(path.join(game,p));return{path:p,bytes:b.length,sha256:hash(b)};});
const artifact_sha256=hash(files.map(f=>`${f.path}:${f.sha256}`).join('\n'));
fs.writeFileSync(`${out}/artifact-manifest.json`,JSON.stringify({slug,version:JSON.parse(fs.readFileSync(`${game}/meta.json`)).version,definition:'SHA256 of newline-joined sorted relative_path:sha256 entries, meta excluded because publisher updates qa',artifact_sha256,files},null,2)+'\n');
console.log(JSON.stringify({slug,artifact_sha256,files:files.length,bytes:files.reduce((s,f)=>s+f.bytes,0)}));
