// Read-only dispatcher: the legacy probability checker cannot validate the 13-pack
// index. Use the existing semester-specific independent oracles without modifying
// them, redirecting only their generated reports into this validation directory.
import fs from 'node:fs';import path from 'node:path';import {pathToFileURL} from 'node:url';
const here=path.dirname(new URL(import.meta.url).pathname),src=path.resolve(here,'../../packs');
const name=process.argv[2]||'m2s1';const names={m2s1:'check-m2s1.mjs',geometry:'check-m2s2-geo.mjs',counting:'verify-packs.mjs'};if(!names[name])throw Error(name);
const file=path.join(src,names[name]),realWrite=fs.writeFileSync,realRead=fs.readFileSync;
fs.writeFileSync=(p,...args)=>{if(String(p).startsWith(src))return realWrite(path.join(here,'pack-check-'+path.basename(p)),...args);throw Error('Unexpected verification output: '+p);};
if(name==='counting')fs.readFileSync=(p,...args)=>{const result=realRead(p,...args);if(String(p).endsWith('/packs/index.json')){const index=JSON.parse(result);index.packs=index.packs.filter(x=>x.pack_id==='m2s2-u6');return JSON.stringify(index);}return result;};
let code=realRead(file,'utf8').replace(/^#!.*\n/,'').replaceAll('import.meta.url',JSON.stringify(pathToFileURL(file).href)).replaceAll("'./validate-pack.mjs'",JSON.stringify(pathToFileURL(path.join(src,'validate-pack.mjs')).href));
process.argv=[process.argv[0],file];await import('data:text/javascript;base64,'+Buffer.from(code).toString('base64'));
