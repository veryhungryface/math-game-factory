#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import {makeItem,probabilityWrong,wrong,token,diceCount,diceText,spread,introItem,gcd,here,output} from './common.mjs';
// Corpus traps: explicit 임의로 + equal balls, 서로 다른 dice, only replacement;
// every OR below joins disjoint colors. The two input pads always mean the raw
// 사건/전체 counts; equivalent pairs are accepted and feedback performs the
// textbook reduction after the learner commits the assembled probability.
const balls=[],cards=[],complement=[],colors=[],dice=[],replacement=[],atLeast=[],experiment=[],reverse=[],overlap=[],basic=[];
const equalBalls='(단, 공의 모양과 크기는 모두 같다.)';
const ASK='확률을 구하시오.';
const bag=(parts)=>`${parts}가 들어 있는 주머니에서`;
for(let red=1;red<=9;red++)for(let blue=1;blue<=9;blue++){
  const total=red+blue;
  balls.push(makeItem({prompt:`${bag(`빨간 공 ${red}개와 파란 공 ${blue}개`)} 공 한 개를 임의로 꺼낼 때, 빨간 공이 나올 ${ASK} ${equalBalls}`,n:red,d:total,format:'frac',explain:`모든 경우는 공 ${total}개 중 한 개를 꺼내는 ${total}가지, 빨간 공이 나오는 경우는 ${red}가지이므로 @P입니다.`,unitConcept:'경우의 수의 비율로서의 확률',difficulty:1,kind:'ball',args:{red,blue,event:'red'},distractors:probabilityWrong(red,total)}));
  complement.push(makeItem({prompt:`${bag(`빨간 공 ${red}개와 파란 공 ${blue}개`)} 공 한 개를 임의로 꺼낼 때, 빨간 공이 나오지 않을 ${ASK} ${equalBalls}`,n:blue,d:total,format:'frac',explain:`모든 경우 ${total}가지 중 빨간 공이 나오지 않는 경우는 파란 공 ${blue}가지이므로 확률은 ${gcd(blue,total)>1?`{frac:${blue}/${total}}=`:''}${token(blue,total)}입니다.`,unitConcept:'어떤 사건이 일어나지 않을 확률',difficulty:2,kind:'ball',args:{red,blue,event:'not-red'},distractors:probabilityWrong(blue,total)}));
}
for(let total=6;total<=20;total++)for(let divisor=2;divisor<=6;divisor++){
  const count=Math.floor(total/divisor);
  cards.push(makeItem({prompt:`1부터 ${total}까지의 자연수가 각각 하나씩 적힌 카드 ${total}장 중 한 장을 임의로 뽑을 때, ${divisor}의 배수일 ${ASK}`,n:count,d:total,format:'frac',explain:`모든 경우 ${total}가지 중 ${divisor}의 배수는 ${count}가지이므로 @P입니다.`,unitConcept:'경우의 수의 비율로서의 확률',difficulty:1,kind:'multiples',args:{total,divisor},distractors:probabilityWrong(count,total)}));
}
for(let red=1;red<=5;red++)for(let blue=1;blue<=5;blue++)for(let white=1;white<=4;white++){
  const total=red+blue+white,count=red+blue;
  colors.push(makeItem({prompt:`${bag(`빨간 공 ${red}개, 파란 공 ${blue}개, 흰 공 ${white}개`)} 공 한 개를 임의로 꺼낼 때, 빨간 공 또는 파란 공이 나올 ${ASK} ${equalBalls}`,n:count,d:total,format:'frac',explain:`두 사건은 겹치지 않으므로 사건의 경우는 ${red}+${blue}=${count}가지, 모든 경우는 ${total}가지이고 확률은 @P입니다.`,unitConcept:'서로 겹치지 않는 두 사건의 확률',difficulty:2,kind:'colors',args:{red,blue,white},distractors:probabilityWrong(count,total,[wrong(red*blue,total*total,'m2s2-u6.sum-product-confusion','multiply-disjoint-probabilities',[red,blue,total]),wrong(red,total,'m2s2-u7.event-count-omitted','first-part',[red,total])])}));
}
for(const kind of ['sum-eq','sum-le','sum-ge','product-eq','difference-eq','product-multiple'])for(let k=kind==='difference-eq'?0:2;k<=(kind==='product-eq'?36:kind==='difference-eq'?5:kind==='product-multiple'?10:12);k++){
  const [count,unordered]=diceCount(kind,k);if(count===0||count===36)continue;
  dice.push(makeItem({prompt:`서로 다른 두 개의 주사위를 동시에 던질 때, ${diceText(kind,k)} ${ASK}`,n:count,d:36,format:'frac',explain:`두 주사위를 구별한 모든 경우 36가지 중 사건이 일어나는 경우는 ${count}가지이므로 @P입니다.`,unitConcept:'서로 다른 두 주사위의 확률',difficulty:3,kind:'dice',args:{test:kind,k},distractors:probabilityWrong(count,36,[wrong(unordered,21,'m2s2-u7.equal-likelihood-bias','unordered-dice',[unordered]),wrong(count,6,'m2s2-u7.wrong-sample-space','one-die-denominator',[count])])}));
}
const again='공 한 개를 임의로 꺼내 확인한 후 다시 넣고 또 한 개를 꺼낼 때,';
for(let red=1;red<=8;red++)for(let blue=1;blue<=8;blue++){
  const total=red+blue,count=red*red,all=total*total;if(all>60)continue;
  replacement.push(makeItem({prompt:`${bag(`빨간 공 ${red}개와 파란 공 ${blue}개`)} ${again} 두 번 모두 빨간 공이 나올 ${ASK} ${equalBalls}`,n:count,d:all,format:'frac',explain:`모든 경우는 ${total}×${total}=${all}가지, 두 번 모두 빨간 공인 경우는 ${red}×${red}=${count}가지이므로 @P입니다. 이는 {frac:${red}/${total}}×{frac:${red}/${total}}의 값과 같습니다.`,unitConcept:'복원 추출에서 두 사건의 확률',difficulty:4,kind:'replacement',args:{red,blue,event:'both'},distractors:probabilityWrong(count,all,[wrong(red,total,'m2s2-u7.event-count-omitted','single-stage',[red,total]),wrong(red*2,total,'m2s2-u6.sum-product-confusion','add-instead-multiply-probabilities',[red,total]),wrong(count,total,'m2s2-u7.wrong-sample-space','count-only-denominator',[count,total])])}));
  const some=all-blue*blue;
  atLeast.push(makeItem({prompt:`${bag(`빨간 공 ${red}개와 파란 공 ${blue}개`)} ${again} 적어도 한 번 빨간 공이 나올 ${ASK} ${equalBalls}`,n:some,d:all,format:'frac',explain:`모든 경우 ${total}×${total}=${all}가지에서 두 번 모두 파란 공인 ${blue}×${blue}=${blue*blue}가지를 빼면 ${some}가지이므로 @P입니다.`,unitConcept:'적어도 하나의 확률',difficulty:4,kind:'replacement',args:{red,blue,event:'at-least-one'},distractors:probabilityWrong(some,all,[wrong(red,total,'m2s2-u7.event-count-omitted','single-stage',[red,total]),wrong(red*2,total,'m2s2-u6.sum-product-confusion','add-instead-multiply-probabilities',[red,total]),wrong(red*red,all,'m2s2-u7.event-complement-confusion','both-red',[red,total])])}));
}
for(let total=10;total<=40;total+=5)for(let heads=2;heads<total;heads+=3){
  experiment.push(makeItem({prompt:`동전을 ${total}번 던졌더니 앞면이 ${heads}번 나왔다. 이 실험에서 앞면의 상대도수를 구하시오.`,n:heads,d:total,format:'frac',explain:`상대도수는 (앞면이 나온 횟수)÷(전체 시행 횟수)이므로 @P입니다.`,unitConcept:'실험 결과의 상대도수',difficulty:2,kind:'experiment',args:{total,heads},distractors:probabilityWrong(heads,total,[wrong(1,2,'m2s2-u7.frequency-equals-theory','half',[])])}));
}
for(let total=5;total<=12;total++)for(let red=2;red<total;red++){
  reverse.push(makeItem({prompt:`빨간 공과 파란 공이 합하여 ${total}개 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때 빨간 공이 나올 확률이 ${token(red,total)}이면, 빨간 공은 몇 개인지 구하시오. ${equalBalls}`,n:red,explain:`(빨간 공의 개수)=${total}×${token(red,total)}=${red}이므로 빨간 공은 ${red}개입니다.`,unitConcept:'확률에서 경우의 수 역으로 구하기',difficulty:3,kind:'reverse',args:{total,red},distractors:[wrong(total-red,1,'m2s2-u7.event-complement-confusion','complement-count',[red,total]),wrong(total,1,'m2s2-u7.denominator-omitted','total-only',[total]),wrong(red*total,1,'m2s2-u7.denominator-omitted','multiply-count-again',[red,total]),wrong(1,1,'m2s2-u7.event-count-omitted','constant-one',[])]}));
}
// Overlapping OR (curriculum misconception 4): direct-listing problems, oracle enumerates.
for(const [a,b] of [[2,3],[2,5],[3,4],[2,7],[3,5],[4,6]])for(let total=6;total<=16;total++){
  const hits=[];for(let i=1;i<=total;i++)if(i%a===0||i%b===0)hits.push(i);
  const both=hits.filter(i=>i%a===0&&i%b===0);if(!both.length)continue;
  const ca=Math.floor(total/a),cb=Math.floor(total/b);
  overlap.push(makeItem({prompt:`1부터 ${total}까지의 자연수가 각각 하나씩 적힌 카드 ${total}장 중 한 장을 임의로 뽑을 때, ${a}의 배수 또는 ${b}의 배수일 ${ASK}`,n:hits.length,d:total,format:'frac',explain:`${hits.join(', ')}의 ${hits.length}장(겹치는 ${both.join(', ')}도 한 번만 센다)이므로 모든 경우 ${total}가지 중 @P입니다.`,unitConcept:'동시에 일어날 수 있는 두 사건 — 직접 세기',difficulty:3,kind:'multiples-or',args:{total,a,b},distractors:[wrong(ca+cb,total,'m2s2-u7.overlap-double-counted','add-overlapping-counts',[a,b,total]),...probabilityWrong(hits.length,total)]}));
}
for(const [color,other] of [['흰','검은'],['빨간','파란'],['노란','초록']])for(let n=2;n<=9;n++)for(const certain of [false,true]){
  const target=certain?color:other,p=certain?1:0;
  basic.push(makeItem({prompt:`${color} 공만 ${n}개 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, ${target} 공이 나올 확률을 구하시오. ${equalBalls}`,n:p,d:1,format:'frac',explain:certain?`꺼낸 공은 반드시 ${color} 공이므로 확률은 1.`:`${other} 공은 절대로 나오지 않으므로 확률은 0.`,unitConcept:'확률의 기본 성질',difficulty:1,kind:'single-color',args:{n,certain},distractors:[wrong(1-p,1,'m2s2-u7.basic-property','certain-impossible-swap',[p]),wrong(1,n,'m2s2-u7.event-count-omitted','one-outcome',[n]),wrong(1,2,'m2s2-u7.equal-likelihood-bias','half',[]),wrong(n,1,'m2s2-u7.denominator-omitted','count-only',[n])]}));
}

// ── acceptance policy ────────────────────────────────────────
const LAB={frac:['사건','전체']};
function coinInput(item,proof,packId){
  const isAmount=['coin-intro','reverse'].includes(proof.kind);
  const [n,d]=proof.parts,[rn,rd]=proof.expected;const reducedTok=`{frac:${rn}/${rd}}`,partsTok=`{frac:${n}/${d}}`;
  if(isAmount){
    item.answer_mode='amount';item.answer=proof.parts[0];item.max=60;item.coin_budget=60;item.choices=null;
    if(item.answer<2||item.answer>=60)throw Error(`Unbalanced amount answer ${item.answer}: ${item.prompt}`);
  } else if(proof.kind==='single-color') item.answer_mode='choice';
  else {
    item.answer_mode='fraction_parts';item.max=60;item.coin_budget=120;item.choices=null;
    item.accept='equivalent';
    const parts=proof.parts;
    item.answer={num:parts[0],den:parts[1]};
    if(parts.some(v=>v>60))throw Error(`Answer exceeds coin pad: ${item.prompt}`);
    const [nl,dl]=LAB.frac;item.num_label=nl;item.den_label=dl;
    item.explain=item.explain.replace('@P',n*rd===rn*d&&(n!==rn||d!==rd)?`${partsTok}=${reducedTok}`:partsTok);
  }
  if(item.explain.includes('@P'))throw Error(`unfilled explain ${item.explain}`);
  return item;
}
function writeU7Pack(id,title,standards,groups){
  const picked=groups.map(([pool,n])=>spread(pool.filter(Boolean),n));const ordered=[];
  for(let i=0;picked.some(g=>i<g.length);i++)for(const group of picked)if(group[i])ordered.push(group[i]);
  const counters=[0,0,0,0,0],proofs=[];const items=ordered.map((raw,i)=>{
    const {_proof,...item}=raw;item.id=`${id}-${String(i+1).padStart(3,'0')}`;
    const slot=_proof.kind==='single-color'?counters[item.difficulty]++%4:0;
    item.choices=_proof.distractors.map(w=>w.value);item.choices.splice(slot,0,item.answer);
    item.distractor_tags=_proof.distractors.map(w=>w.misconceptionId);
    proofs.push({id:item.id,..._proof});return coinInput(item,_proof,id);
  });
  const pack={schema_version:2,pack_id:id,title,school:'middle',grade:2,semester:2,unit_id:id,standards,economy:{carry_capacity:120,coin_per_kill:1,min_spawn_coins:140},items};
  fs.mkdirSync(output,{recursive:true});fs.writeFileSync(path.join(output,`${id}.json`),JSON.stringify(pack));
  fs.writeFileSync(path.join(here,`${id}-proofs.json`),JSON.stringify({pack_id:id,proofs},null,2)+'\n');
  const acc={};for(const q of items)if(q.accept)acc[q.accept]=(acc[q.accept]??0)+1;
  console.log(`${id}: ${items.length} items / ${Buffer.byteLength(JSON.stringify(pack))} bytes`,JSON.stringify(acc));
  return pack;
}
writeU7Pack('m2s2-u7','확률',['[9수04-06]'],[[[introItem()],1],[balls,80],[cards,75],[complement,56],[colors,58],[dice,48],[replacement,19],[atLeast,19],[experiment,24],[reverse,20],[overlap,28],[basic,16]]);
