import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const base='https://math-game-factory.vercel.app',out='logs/manual-20260907-two-game-polish';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const catalog=await(await fetch(`${base}/catalog.json`,{cache:'no-store'})).json();
for(const slug of ['twin-forts','sunbasket-farm']){
 const manifest=JSON.parse(fs.readFileSync(`${out}/${slug}/artifact-manifest.json`));
 const version=JSON.parse(fs.readFileSync(`public/g/${slug}/meta.json`)).version;
 const report={checked_at:new Date().toISOString(),slug,version,base,catalog_count:catalog.count,catalog_entry:catalog.games.find(g=>g.slug===slug),files:[],artifact_sha256:manifest.artifact_sha256};
 assert.equal(report.catalog_entry?.version,version,'Catalog version not deployed');
 for(const file of [...manifest.files,{path:'meta.json'}]){
  const res=await fetch(`${base}/g/${slug}/${file.path}?v=${version}`);
  const bytes=Buffer.from(await res.arrayBuffer()),expected=hash(fs.readFileSync(`public/g/${slug}/${file.path}`)),actual=hash(bytes);
  report.files.push({path:file.path,status:res.status,bytes:bytes.length,sha256:actual,matches_local:expected===actual});
  assert.equal(res.status,200,file.path);assert.equal(actual,expected,file.path);
 }
 fs.writeFileSync(`${out}/${slug}/deployment-files.json`,JSON.stringify(report,null,2)+'\n');
 console.log(JSON.stringify({slug,version,catalog_count:catalog.count,files:report.files.length,matches:true}));
}
