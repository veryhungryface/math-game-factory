// Shared writer for the m2s2 geometry packs (u1~u5) — schema v3 (2026-09-27, DESIGN-V3 §4).
// Every item is a four-option choice. Numeric items carry exactly three misconception-derived
// wrong values; the correct option is placed so that answer positions are balanced per
// difficulty band. common.mjs is intentionally untouched and nothing here writes index.json.
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

// ── drop ledger: items rejected while building pools (reported with POOLS=1) ──
export const drops={};
const drop=(kind,why)=>{const k=`${kind}:${why}`;drops[k]=(drops[k]??0)+1;return null;};

// Numbers the student can read in the prompt ({frac:a/b} tokens count as a/b).
export function promptNumbers(prompt){const ints=new Set(),fracs=new Set();
  const rest=prompt.replace(/\{frac:(\d+)\/(\d+)\}/g,(_,a,b)=>{const g=gcd(+a,+b);fracs.add(`${a/g}/${b/g}`);return ' ';});
  for(const m of rest.matchAll(/\d+/g))ints.add(Number(m[0]));return {ints,fracs,ratios:new Set([...rest.matchAll(/(\d+):(\d+)/g)].map(m=>{const g=gcd(+m[1],+m[2]);return `${m[1]/g}:${m[2]/g}`;}))};}

// 「…의 크기는 몇 도인지 구하시오.」 → 「…의 크기를 구하시오.」 + unit on every option (「70°」).
// The numeric pad is gone, so the textbook ending is restored and the unit moves to the options.
const UNIT={'도':'°','cm²':' cm²','cm³':' cm³','cm':' cm','m':' m'};
const ASK_RE=/(\S+)(은|는) 몇 (도|cm²|cm³|cm|m)인지 구하시오\./;
export function unitize(prompt){const m=ASK_RE.exec(prompt);if(!m)return {prompt,unit:''};
  return {prompt:prompt.replace(ASK_RE,`${josa(m[1],'을','를')} 구하시오.`),unit:UNIT[m[3]]};}

// Pick 3 of the valid candidate wrongs. The first listed (signature) misconception is always kept;
// the other two are chosen among the next five so that, per kind, the rank of the correct value
// among the four options (0 = smallest … 3 = largest) is spread evenly — otherwise "never tap the
// largest/smallest pad" beats chance. Ties are broken by listing priority.
const rankUse={};
function pickWrongs(kind,cands,key,val,ans){
  const seen=new Set([key.answer]),u=[];for(const w of cands){const k=key.of(w.value);if(seen.has(k))continue;seen.add(k);u.push(w);}
  if(u.length<3)return drop(kind,'fewer-than-3-wrongs');
  const pool=u.slice(0,6),use=rankUse[kind]??=[0,0,0,0];let best=null;
  for(let i=1;i<pool.length;i++)for(let j=i+1;j<pool.length;j++){const pick=[pool[0],pool[i],pool[j]];
    if(new Set(pick.map(w=>w.tag)).size<2)continue;const rank=pick.filter(w=>val(w.value)<ans).length;
    const score=use[rank]*100+i+j;if(!best||score<best.score)best={pick,rank,score};}
  if(!best)return drop(kind,'one-misconception-only');
  use[best.rank]++;return best.pick;
}
// Integer answer. Wrongs: W(value, tag, rule) in priority order (first three valid ones are used).
// lo/hi bound plausible wrong values (e.g. a triangle angle is below 180). Rejects items whose
// answer is printed in the prompt (answer-by-copying).
export function amount({kind,params,prompt,answer,explain,concept,d,wrongs=[],lo=1,hi=Infinity}) {
  if(!Number.isSafeInteger(answer)||answer<1)throw Error(`integer answer ${answer}: ${prompt}`);
  if(promptNumbers(prompt).ints.has(answer))return drop(kind,'answer-in-prompt');
  const ok=wrongs.filter(w=>Number.isSafeInteger(w.value)&&w.value>=lo&&w.value<=hi);
  const ws=pickWrongs(kind,ok,{answer:String(answer),of:v=>String(v)},v=>v,answer);if(!ws)return null;
  return {mode:'int',kind,params,prompt,answer,explain,concept,d,wrongs:ws};
}
export const num=amount;
// Rational answer num/den. as:'frac' renders {frac:a/b} options (prompt should say 기약분수 or
// 「몇 배」); as:'ratio' renders 「a:b」 options in lowest terms (prompt: 가장 간단한 자연수의 비).
export function fraction({kind,params,prompt,num,den,explain,concept,d,wrongs=[],as='frac'}) {
  if(!Number.isSafeInteger(num)||!Number.isSafeInteger(den)||num<1||den<1)throw Error(`fraction parts ${num}/${den}: ${prompt}`);
  if(gcd(num,den)!==1)throw Error(`fraction not reduced ${num}/${den}: ${prompt}`);
  if(as==='frac'&&den<2)throw Error(`integer disguised as fraction: ${prompt}`);
  const pn=promptNumbers(prompt);
  if(as==='frac'&&pn.fracs.has(`${num}/${den}`))return drop(kind,'answer-in-prompt');
  if(as==='ratio'&&pn.ratios.has(`${num}:${den}`))return drop(kind,'answer-in-prompt');
  const red=v=>{const g=gcd(v.num,v.den);return {num:v.num/g,den:v.den/g};};
  const ok=wrongs.filter(w=>w.value&&Number.isSafeInteger(w.value.num)&&Number.isSafeInteger(w.value.den)&&w.value.num>0&&w.value.den>0).map(w=>({...w,value:red(w.value)}));
  const ws=pickWrongs(kind,ok,{answer:`${num}/${den}`,of:v=>`${v.num}/${v.den}`},v=>v.num/v.den,num/den);if(!ws)return null;
  return {mode:as,kind,params,prompt,num,den,explain,concept,d,wrongs:ws};
}
// style 'direct': the four option texts are the pads. style 'labels': options are listed in the
// prompt as ㄱ~ㄹ and the pads are ㄱ,ㄴ,ㄷ,ㄹ.
export function choice({kind,params,stem,tail='',correct,wrongs,style='direct',explain,concept,d}) {
  if(wrongs.length!==3)throw Error(`choice needs 3 wrongs: ${stem}`);
  const texts=[correct,...wrongs.map(w=>w.text)];
  if(new Set(texts).size!==4)throw Error(`duplicate option: ${stem} ${texts}`);
  if(new Set(wrongs.map(w=>w.tag)).size<2)throw Error(`need 2+ misconception tags: ${stem}`);
  return {mode:'text',kind,params,stem,tail,correct,wrongs,style,explain,concept,d};
}
const fracTok=(n,d)=>d===1?String(n):`{frac:${n}/${d}}`;
function render(it,slot) {
  if(it.mode==='text'){
    const opts=it.wrongs.map(w=>w.text);opts.splice(slot,0,it.correct);const tags=it.wrongs.map(w=>w.tag);
    if(it.style==='labels'){
      const listed=opts.map((t,i)=>`${LABELS[i]}. ${t}`).join('  ');
      const prompt=`${it.stem} ${listed}${it.tail?' '+it.tail:''}`;
      const explain=typeof it.explain==='function'?it.explain(LABELS[slot]):it.explain;
      return {prompt,choices:[...LABELS],answer:LABELS[slot],explain,options:opts,tags,format:'text'};
    }
    const explain=typeof it.explain==='function'?it.explain(it.correct):it.explain;
    return {prompt:it.stem+(it.tail?' '+it.tail:''),choices:opts,answer:it.correct,explain,options:opts,tags,format:'text'};
  }
  let prompt=it.prompt,show,answerNumeric,format;
  if(it.mode==='int'){const u=unitize(prompt);prompt=u.prompt;show=v=>`${v}${u.unit}`;answerNumeric=it.answer;format=u.unit?'text':'int';}
  else if(it.mode==='frac'){show=v=>fracTok(v.num,v.den);answerNumeric=it.num/it.den;format='frac';}
  else {show=v=>`${v.num}:${v.den}`;format='text';}
  const correct=it.mode==='int'?show(it.answer):show({num:it.num,den:it.den});
  const opts=it.wrongs.map(w=>show(w.value));opts.splice(slot,0,correct);
  return {prompt,choices:opts,answer:correct,explain:it.explain,options:opts,tags:it.wrongs.map(w=>w.tag),format,answerNumeric};
}
function poolKey(it){
  if(it.mode==='text'&&it.style==='labels')return norm(it.stem)+'|'+[it.correct,...it.wrongs.map(w=>w.text)].sort().join('|');
  return norm(it.mode==='text'?it.stem:it.prompt);
}
export function spread(pool,n){if(pool.length<n)throw Error(`Pool only ${pool.length}, need ${n} (${pool[0]?.kind})`);return Array.from({length:n},(_,i)=>pool[Math.floor(i*pool.length/n)]);}
// Balanced answer slots: each difficulty band walks through blocks of the 24 permutations of
// 0..3 (a fixed, non-cyclic order), so every band ends with max−min ≤ 1 per position.
const PERMS=(()=>{const r=[];const go=(a,rest)=>{if(!rest.length)return r.push(a);rest.forEach((x,i)=>go([...a,x],rest.filter((_,j)=>j!==i)));};go([],[0,1,2,3]);return r;})();
export function slotter(){const c={};return d=>{const i=c[d]=(c[d]??-1)+1;return PERMS[(Math.floor(i/4)*7+d*5)%24][i%4];};}
const FORBID_EXPLAIN=/붓|닢|약분하지 말고|약분하지 않아도/;

export function writeGeoPack({id,title,standards,intro,groups,log=true}) {
  if(!intro||intro.d!==1)throw Error('items[0] must be an easy (difficulty 1) item');
  const seen=new Set([poolKey(intro)]);
  const picked=groups.map(([pool,n])=>{const u=[];for(const it of pool){if(!it)continue;const k=poolKey(it);if(seen.has(k))continue;seen.add(k);u.push(it);}return spread(u,n);});
  const ordered=[intro];
  for(let i=0;picked.some(g=>i<g.length);i++)for(const g of picked)if(g[i])ordered.push(g[i]);
  const slotOf=slotter(),items=[],proofs=[],prompts=new Set();
  ordered.forEach((it,i)=>{
    const qid=`${id}-${String(i+1).padStart(3,'0')}`;
    const slot=slotOf(it.d);const r=render(it,slot);
    const key=norm(r.prompt);if(prompts.has(key))throw Error(`duplicate prompt ${r.prompt}`);prompts.add(key);
    if(FORBID_EXPLAIN.test(r.prompt+r.explain))throw Error(`pour-era wording: ${r.prompt} / ${r.explain}`);
    if(/\n/.test(r.explain))throw Error(`explain must be one line: ${r.explain}`);
    const q={id:qid,prompt:r.prompt,answer_mode:'choice',answer:r.answer,choices:r.choices,format:r.format};
    if(r.answerNumeric!==undefined)q.answerNumeric=r.answerNumeric;
    q.explain=r.explain;q.unitConcept=it.concept;q.difficulty=it.d;q.distractor_tags=[...new Set(r.tags)];
    items.push(q);
    proofs.push({id:qid,kind:it.kind,source:it.mode,params:it.params,options:r.options,correct_index:slot,wrong_tags:r.tags,
      ...(it.mode==='text'?{style:it.style}:{wrong_rules:it.wrongs.map(w=>w.rule)})});
  });
  const pack={schema_version:3,pack_id:id,title,school:'middle',grade:2,semester:2,unit_id:id,standards,economy:{carry_capacity:120,coin_per_kill:1,min_spawn_coins:140},items};
  fs.mkdirSync(output,{recursive:true});
  fs.writeFileSync(path.join(output,`${id}.json`),JSON.stringify(pack));
  fs.writeFileSync(path.join(here,`${id}-proofs.json`),JSON.stringify({pack_id:id,proofs},null,1)+'\n');
  if(log){const m={},b={};for(const [k,p] of proofs.entries()){m[p.source]=(m[p.source]??0)+1;const d=items[k].difficulty;b[d]=(b[d]??0)+1;}
    console.log(`${id}: ${items.length} items`,JSON.stringify(m),JSON.stringify(b),`${Buffer.byteLength(JSON.stringify(pack))} bytes`);
    if(process.env.POOLS)console.log('drops',JSON.stringify(drops));}
  return pack;
}
