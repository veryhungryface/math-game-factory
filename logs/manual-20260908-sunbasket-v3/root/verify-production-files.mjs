import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const base='https://math-game-factory.vercel.app',out='logs/manual-20260908-sunbasket-v3/root';
const manifest=JSON.parse(fs.readFileSync(`${out}/artifact-manifest.json`));
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const report={checked_at:new Date().toISOString(),base,files:[],artifact_sha256:manifest.artifact_sha256};
const catalog=await(await fetch(`${base}/catalog.json`,{cache:'no-store'})).json();
report.catalog_count=catalog.count;report.catalog_entry=catalog.games.find(g=>g.slug==='sunbasket-farm');
assert.ok(report.catalog_entry,'New game missing from production catalog');
assert.equal(report.catalog_entry.version,JSON.parse(fs.readFileSync('public/g/sunbasket-farm/meta.json')).version);
for(const file of [...manifest.files,{path:'meta.json'}]){
 const res=await fetch(`${base}/g/sunbasket-farm/${file.path}?v=3`);
 const bytes=Buffer.from(await res.arrayBuffer()),expected=hash(fs.readFileSync(`public/g/sunbasket-farm/${file.path}`)),actual=hash(bytes);
 report.files.push({path:file.path,status:res.status,bytes:bytes.length,sha256:actual,matches_local:expected===actual});
 assert.equal(res.status,200,file.path);assert.equal(actual,expected,file.path);
}
fs.writeFileSync(`${out}/deployment-files.json`,JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({catalog_count:catalog.count,files:report.files.length,matches:true}));
