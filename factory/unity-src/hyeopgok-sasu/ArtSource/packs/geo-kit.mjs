// Shared writer for the m2s2 geometry packs (u1~u5) and the u7 rewrite.
// common.mjs is intentionally untouched: its writePack() rewrites index.json,
// which the pack orchestrator owns. Nothing here writes index.json.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
export const here=path.dirname(fileURLToPath(import.meta.url));
export const output=path.resolve(here,'../../../../../public/g/hyeopgok-sasu/packs');
export const LABELS=['ㄱ','ㄴ','ㄷ','ㄹ'];
export function gcd(a,b){a=Math.abs(a);b=Math.abs(b);while(b)[a,b]=[b,a%b];return a||1;}
export const norm=p=>p.replace(/\([^)]*\)/g,'').replace(/\s+/g,' ').trim();
export const W=(value,tag,rule)=>({value,tag,rule});
// 받침 유무로 조사 선택 (은/는, 이/가, 을/를, 과/와). Digits/Latin read in Korean: 0(영)1(일)3(삼)6(육)7(칠)8(팔) end in 받침.
export function josa(word,withFinal,without){const ch=word.trim().slice(-1);const code=ch.charCodeAt(0);
  if(code>=0xac00&&code<=0xd7a3)return word+((code-0xac00)%28?withFinal:without);
  if(/[013678]/.test(ch))return word+withFinal;if(/[LMNR]/.test(ch))return word+withFinal;return word+without;}
function uniqWrongs(ws,answerKey){const seen=new Set([answerKey]),out=[];for(const w of ws){const k=typeof w.value==='object'?`${w.value.num}/${w.value.den}`:String(w.value);if(seen.has(k))continue;seen.add(k);out.push(w);}return out;}

export function amount({kind,params,prompt,answer,explain,concept,d,wrongs=[]}) {
  if(!Number.isSafeInteger(answer)||answer<2||answer>59)throw Error(`amount answer ${answer} outside 2..59: ${prompt}`);
  const ws=uniqWrongs(wrongs.filter(w=>Number.isSafeInteger(w.value)&&w.value>0),String(answer));
  return {mode:'amount',kind,params,prompt,answer,explain,concept,d,wrongs:ws};
}
// Reduced fraction answer ("몇 배인지 기약분수로"): the only fraction mode geometry uses.
export function fraction({kind,params,prompt,num,den,explain,concept,d,wrongs=[]}) {
  if(!Number.isSafeInteger(num)||!Number.isSafeInteger(den)||num<1||den<2||num>60||den>60)throw Error(`fraction parts ${num}/${den}: ${prompt}`);
  if(gcd(num,den)!==1)throw Error(`fraction not reduced ${num}/${den}: ${prompt}`);
  if(!prompt.includes('기약분수'))throw Error(`reduced prompt must say 기약분수: ${prompt}`);
  const ws=uniqWrongs(wrongs.filter(w=>w.value&&w.value.den>0),`${num}/${den}`);
  return {mode:'fraction',kind,params,prompt,num,den,explain,concept,d,wrongs:ws};
}
// style 'direct': the four option texts are the pads. style 'labels': options are listed in the
// prompt as ㄱ~ㄹ and the pads are ㄱ,ㄴ,ㄷ,ㄹ. The correct option is placed at write time so that
// answer positions stay balanced within every difficulty band.
export function choice({kind,params,stem,tail='',correct,wrongs,style='direct',explain,concept,d}) {
  if(wrongs.length!==3)throw Error(`choice needs 3 wrongs: ${stem}`);
  const texts=[correct,...wrongs.map(w=>w.text)];
  if(new Set(texts).size!==4)throw Error(`duplicate option: ${stem} ${texts}`);
  if(new Set(wrongs.map(w=>w.tag)).size<2)throw Error(`need 2+ misconception tags: ${stem}`);
  return {mode:'choice',kind,params,stem,tail,correct,wrongs,style,explain,concept,d};
}
function render(it,slot) {
  if(it.mode!=='choice')return {prompt:it.prompt};
  const opts=it.wrongs.map(w=>w.text);opts.splice(slot,0,it.correct);
  const tags=it.wrongs.map(w=>w.tag);
  if(it.style==='labels'){
    const listed=opts.map((t,i)=>`${LABELS[i]}. ${t}`).join('  ');
    const prompt=`${it.stem} ${listed}${it.tail?' '+it.tail:''}`;
    const explain=typeof it.explain==='function'?it.explain(LABELS[slot]):it.explain;
    return {prompt,choices:[...LABELS],answer:LABELS[slot],explain,options:opts,tags};
  }
  const explain=typeof it.explain==='function'?it.explain(it.correct):it.explain;
  return {prompt:it.stem+(it.tail?' '+it.tail:''),choices:opts,answer:it.correct,explain,options:opts,tags};
}
function poolKey(it){
  if(it.mode==='choice'&&it.style==='labels')return norm(it.stem)+'|'+[it.correct,...it.wrongs.map(w=>w.text)].sort().join('|');
  return norm(it.mode==='choice'?it.stem:it.prompt);
}
export function spread(pool,n){if(pool.length<n)throw Error(`Pool only ${pool.length}, need ${n} (${pool[0]?.kind})`);return Array.from({length:n},(_,i)=>pool[Math.floor(i*pool.length/n)]);}

export function writeGeoPack({id,title,standards,intro,groups,log=true}) {
  const seen=new Set([poolKey(intro)]);
  const picked=groups.map(([pool,n])=>{const u=[];for(const it of pool){if(!it)continue;const k=poolKey(it);if(seen.has(k))continue;seen.add(k);u.push(it);}return spread(u,n);});
  const ordered=[intro];
  for(let i=0;picked.some(g=>i<g.length);i++)for(const g of picked)if(g[i])ordered.push(g[i]);
  const counters=[0,0,0,0,0],items=[],proofs=[],prompts=new Set();
  ordered.forEach((it,i)=>{
    const qid=`${id}-${String(i+1).padStart(3,'0')}`;
    const slot=it.mode==='choice'?counters[it.d]++%4:0;const r=render(it,slot);
    const key=norm(r.prompt);if(prompts.has(key))throw Error(`duplicate prompt ${r.prompt}`);prompts.add(key);
    let q;
    if(it.mode==='amount')q={id:qid,prompt:r.prompt,answer_mode:'amount',answer:it.answer,max:60,coin_budget:60,answerNumeric:it.answer,choices:null,format:'int'};
    else if(it.mode==='fraction')q={id:qid,prompt:r.prompt,answer_mode:'fraction_parts',answer:{num:it.num,den:it.den},accept:'reduced',num_label:'분자',den_label:'분모',max:60,coin_budget:120,answerNumeric:it.num/it.den,choices:null,format:'frac'};
    else q={id:qid,prompt:r.prompt,answer_mode:'choice',answer:r.answer,choices:r.choices,format:'text'};
    q.explain=it.mode==='choice'?r.explain:it.explain;q.unitConcept=it.concept;q.difficulty=it.d;
    const tags=it.mode==='choice'?r.tags:it.wrongs.map(w=>w.tag);q.distractor_tags=[...new Set(tags)];
    items.push(q);
    proofs.push({id:qid,kind:it.kind,mode:it.mode,params:it.params,
      ...(it.mode==='choice'?{style:it.style,options:r.options,correct_index:slot,wrong_tags:r.tags}:{wrongs:it.wrongs})});
  });
  const pack={schema_version:2,pack_id:id,title,school:'middle',grade:2,semester:2,unit_id:id,standards,economy:{carry_capacity:120,coin_per_kill:1,min_spawn_coins:140},items};
  fs.mkdirSync(output,{recursive:true});
  fs.writeFileSync(path.join(output,`${id}.json`),JSON.stringify(pack));
  fs.writeFileSync(path.join(here,`${id}-proofs.json`),JSON.stringify({pack_id:id,proofs},null,1)+'\n');
  if(log){const m={},b={};for(const q of items){m[q.answer_mode]=(m[q.answer_mode]??0)+1;b[q.difficulty]=(b[q.difficulty]??0)+1;}console.log(`${id}: ${items.length} items`,JSON.stringify(m),JSON.stringify(b),`${Buffer.byteLength(JSON.stringify(pack))} bytes`);}
  return pack;
}
