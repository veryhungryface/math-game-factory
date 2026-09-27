// Shared primitives for the independent m2s2 geometry oracle (check-m2s2-geo.mjs).
// Imports NO generator module. Integer / rational arithmetic only.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
export const here=path.dirname(fileURLToPath(import.meta.url));export const root=path.resolve(here,'../../../../..');
export const out=path.join(root,'public/g/hyeopgok-sasu/packs');
export const curriculum=JSON.parse(fs.readFileSync(path.join(root,'curriculum/2022-middle-math.json'),'utf8'));
export const failures=[];export const stats={items:0,bruteForce:0,latticeQuads:0,latticeClaims:0,coordinateChecks:0,enumeratedOutcomes:0,choiceOptions:0,numericOptions:0};
export const fail=(id,msg)=>failures.push(`${id}: ${msg}`);
export const gcd=(a,b)=>{a=Math.abs(a);b=Math.abs(b);while(b)[a,b]=[b,a%b];return a||1;};
export const M=(re,s,id)=>{const m=re.exec(s);if(!m)throw Error(`prompt does not match ${re}`);return m;};
export const N=Number;
// brute force: the unique integer x in [lo,hi] with pred(x); throws if none or several.
export function solve(pred,lo=0,hi=400){const hits=[];for(let x=lo;x<=hi;x++){stats.bruteForce++;if(pred(x))hits.push(x);}if(hits.length!==1)throw Error(`brute force found ${hits.length} solutions`);return hits[0];}
export const isqrt=n=>{if(n<0)return NaN;let r=Math.floor(Math.sqrt(n));while(r*r>n)r--;while((r+1)*(r+1)<=n)r++;return r*r===n?r:NaN;};
