#!/usr/bin/env node
import {makeItem,probabilityWrong,wrong,token,diceCount,diceText,writePack} from './common.mjs';
// Corpus traps: explicit 임의로 + equal balls, 서로 다른 dice, only replacement;
// every OR below joins disjoint colors; all fractions are reduced integer pairs.
const balls=[],cards=[],complement=[],colors=[],dice=[],replacement=[],atLeast=[],experiment=[],reverse=[];
const equalBalls='(단, 공의 모양과 크기는 모두 같다.)';
for(let red=1;red<=9;red++)for(let blue=1;blue<=9;blue++){
  const total=red+blue;
  balls.push(makeItem({prompt:`빨간 공 ${red}개와 파란 공 ${blue}개 중 한 개를 임의로 꺼낼 때, 빨간 공일 확률을 기약분수로 구하시오. ${equalBalls}`,n:red,d:total,format:'frac',explain:`전체 ${total}개 중 빨간 공은 ${red}개이므로 ${token(red,total)}.`,unitConcept:'경우의 수의 비율로서의 확률',difficulty:1,kind:'ball',args:{red,blue,event:'red'},distractors:probabilityWrong(red,total)}));
  complement.push(makeItem({prompt:`빨간 공 ${red}개와 파란 공 ${blue}개 중 한 개를 임의로 꺼낼 때, 빨간 공이 아닐 확률을 기약분수로 구하시오. ${equalBalls}`,n:blue,d:total,format:'frac',explain:`빨간 공이 아닌 ${blue}개의 비율은 ${token(blue,total)}.`,unitConcept:'어떤 사건이 일어나지 않을 확률',difficulty:2,kind:'ball',args:{red,blue,event:'not-red'},distractors:probabilityWrong(blue,total)}));
}
for(let total=6;total<=20;total++)for(let divisor=2;divisor<=6;divisor++){
  const count=Math.floor(total/divisor);
  cards.push(makeItem({prompt:`1부터 ${total}까지의 자연수가 각각 하나씩 적힌 카드 ${total}장 중 한 장을 임의로 뽑을 때, ${divisor}의 배수일 확률을 기약분수로 구하시오.`,n:count,d:total,format:'frac',explain:`${divisor}의 배수 ${count}장을 전체 ${total}장으로 나누면 ${token(count,total)}.`,unitConcept:'경우의 수의 비율로서의 확률',difficulty:1,kind:'multiples',args:{total,divisor},distractors:probabilityWrong(count,total)}));
}
for(let red=1;red<=5;red++)for(let blue=1;blue<=5;blue++)for(let white=1;white<=4;white++){
  const total=red+blue+white,count=red+blue;
  colors.push(makeItem({prompt:`빨간 공 ${red}개, 파란 공 ${blue}개, 흰 공 ${white}개 중 한 개를 임의로 꺼낼 때, 빨간색 또는 파란색일 확률을 기약분수로 구하시오. ${equalBalls}`,n:count,d:total,format:'frac',explain:`두 색은 겹치지 않으므로 ${count}개, 확률은 ${token(count,total)}.`,unitConcept:'서로 겹치지 않는 두 사건의 확률',difficulty:2,kind:'colors',args:{red,blue,white},distractors:probabilityWrong(count,total,[wrong(red*blue,total*total,'m2s2-u6.sum-product-confusion','multiply-disjoint-probabilities',[red,blue,total]),wrong(red,total,'m2s2-u7.event-count-omitted','first-part',[red,total])])}));
}
for(const kind of ['sum-eq','sum-le','sum-ge','product-eq','difference-eq','product-multiple'])for(let k=kind==='difference-eq'?0:2;k<=(kind==='product-eq'?36:kind==='difference-eq'?5:kind==='product-multiple'?10:12);k++){
  const [count,unordered]=diceCount(kind,k);if(count===0||count===36)continue;
  dice.push(makeItem({prompt:`서로 다른 두 개의 주사위를 동시에 던질 때, ${diceText(kind,k)} 확률을 기약분수로 구하시오.`,n:count,d:36,format:'frac',explain:`서로 구별한 36가지 중 ${count}가지이므로 ${token(count,36)}.`,unitConcept:'서로 다른 두 주사위의 확률',difficulty:3,kind:'dice',args:{test:kind,k},distractors:probabilityWrong(count,36,[wrong(unordered,21,'m2s2-u7.equal-likelihood-bias','unordered-dice',[unordered]),wrong(count,6,'m2s2-u7.wrong-sample-space','one-die-denominator',[count])])}));
}
for(let red=1;red<=8;red++)for(let blue=1;blue<=8;blue++){
  const total=red+blue,count=red*red,all=total*total;
  replacement.push(makeItem({prompt:`빨간 공 ${red}개와 파란 공 ${blue}개에서 임의로 한 개를 꺼내 확인한 후 다시 넣고 한 개를 임의로 꺼낸다. 두 번 모두 빨간 공일 확률을 기약분수로 구하시오. ${equalBalls}`,n:count,d:all,format:'frac',explain:`다시 넣으므로 ${token(red,total)} × ${token(red,total)} = ${token(count,all)}.`,unitConcept:'복원 추출에서 두 사건의 확률',difficulty:4,kind:'replacement',args:{red,blue,event:'both'},distractors:probabilityWrong(count,all,[wrong(red,total,'m2s2-u7.event-count-omitted','single-stage',[red,total]),wrong(red*2,total,'m2s2-u6.sum-product-confusion','add-instead-multiply-probabilities',[red,total]),wrong(count,total,'m2s2-u7.wrong-sample-space','count-only-denominator',[count,total])])}));
  const some=all-blue*blue;
  atLeast.push(makeItem({prompt:`빨간 공 ${red}개와 파란 공 ${blue}개에서 임의로 한 개를 꺼내 확인한 후 다시 넣고 한 개를 임의로 꺼낸다. 적어도 한 번 빨간 공일 확률을 기약분수로 구하시오. ${equalBalls}`,n:some,d:all,format:'frac',explain:`두 번 모두 파란 공일 확률을 1에서 빼면 ${token(some,all)}.`,unitConcept:'적어도 하나의 확률',difficulty:4,kind:'replacement',args:{red,blue,event:'at-least-one'},distractors:probabilityWrong(some,all,[wrong(red,total,'m2s2-u7.event-count-omitted','single-stage',[red,total]),wrong(red*2,total,'m2s2-u6.sum-product-confusion','add-instead-multiply-probabilities',[red,total]),wrong(red*red,all,'m2s2-u7.event-complement-confusion','both-red',[red,total])])}));
}
for(let total=10;total<=40;total+=5)for(let heads=2;heads<total;heads+=3){
  experiment.push(makeItem({prompt:`동전을 ${total}번 던졌더니 앞면이 ${heads}번 나왔다. 이 실험에서 앞면의 상대도수를 기약분수로 구하시오.`,n:heads,d:total,format:'frac',explain:`실험의 상대도수는 앞면 횟수 ${heads} ÷ 전체 횟수 ${total} = ${token(heads,total)}.`,unitConcept:'실험 결과의 상대도수',difficulty:2,kind:'experiment',args:{total,heads},distractors:probabilityWrong(heads,total,[wrong(1,2,'m2s2-u7.frequency-equals-theory','half',[])])}));
}
for(let total=5;total<=12;total++)for(let red=1;red<total;red++){
  reverse.push(makeItem({prompt:`빨간 공과 파란 공이 합해 ${total}개이다. 한 개를 임의로 꺼낼 때 빨간 공일 확률이 ${token(red,total)}이면, 빨간 공의 개수를 구하시오. ${equalBalls}`,n:red,explain:`전체 ${total}개에 빨간 공의 비율 ${token(red,total)}을 곱하면 ${red}개.`,unitConcept:'확률에서 경우의 수 역으로 구하기',difficulty:3,kind:'reverse',args:{total,red},distractors:[wrong(total-red,1,'m2s2-u7.event-complement-confusion','complement-count',[red,total]),wrong(total,1,'m2s2-u7.denominator-omitted','total-only',[total]),wrong(red*total,1,'m2s2-u7.denominator-omitted','multiply-count-again',[red,total]),wrong(1,1,'m2s2-u7.event-count-omitted','constant-one',[])]}));
}
writePack('m2s2-u7','확률',['[9수04-06]'],[[balls,80],[cards,60],[complement,56],[colors,48],[dice,48],[replacement,32],[atLeast,32],[experiment,24],[reverse,20]]);
