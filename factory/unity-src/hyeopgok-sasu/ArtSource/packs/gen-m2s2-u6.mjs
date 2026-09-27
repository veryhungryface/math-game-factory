#!/usr/bin/env node
import {makeItem,wrong,diceCount,diceText,writePack,introItem} from './common.mjs';
// Only two-step products/sums and directly enumerated pairs; no permutation or
// combination notation, nonreplacement probability, or overlapping OR events.
const product=[],sum=[],cards=[],roles=[],dice=[];
for(let shirts=2;shirts<=12;shirts++)for(let pants=2;pants<=12;pants++){
  if(shirts===pants)continue; // Guarantees three distinct misconception answers.
  product.push(makeItem({prompt:`서로 다른 윗옷 ${shirts}벌과 바지 ${pants}벌 중 각각 한 벌씩 골라 입는 경우의 수를 구하시오.`,n:shirts*pants,explain:`윗옷마다 바지가 ${pants}벌이므로 ${shirts} × ${pants} = ${shirts*pants}가지.`,unitConcept:'곱의 법칙',difficulty:1,kind:'product',args:{a:shirts,b:pants},distractors:[wrong(shirts+pants,1,'m2s2-u6.sum-product-confusion','sum',[shirts,pants]),wrong(shirts,1,'m2s2-u6.second-stage-omitted','first-count',[shirts]),wrong(pants,1,'m2s2-u6.first-stage-omitted','first-count',[pants])]}));
  sum.push(makeItem({prompt:`서로 다른 주스 ${shirts}종류와 차 ${pants}종류 중 주스 한 잔 또는 차 한 잔만 고르는 경우의 수를 구하시오.`,n:shirts+pants,explain:`주스와 차를 함께 고르지 않으므로 ${shirts} + ${pants} = ${shirts+pants}가지.`,unitConcept:'합의 법칙',difficulty:1,kind:'sum',args:{a:shirts,b:pants},distractors:[wrong(shirts*pants,1,'m2s2-u6.sum-product-confusion','product',[shirts,pants]),wrong(shirts,1,'m2s2-u6.second-event-omitted','first-count',[shirts]),wrong(pants,1,'m2s2-u6.first-event-omitted','first-count',[pants])]}));
}
function cardCount(digits,modulus,zeroAllowed=false,reuse=false){let count=0;for(const a of digits)for(const b of digits)if((zeroAllowed||a!==0)&&(reuse||a!==b)&&(a*10+b)%modulus===0)count++;return count;}
for(let mask=1;mask<512;mask++){
  const digits=[0];for(let n=1;n<=9;n++)if(mask&(1<<(n-1)))digits.push(n);if(digits.length<3||digits.length>5)continue;
  for(const modulus of [1,2,3]){
    const count=cardCount(digits,modulus);if(count<2)continue;
    let unordered=0;for(let i=0;i<digits.length;i++)for(let j=i+1;j<digits.length;j++)if((digits[i]!==0&&(digits[i]*10+digits[j])%modulus===0)||(digits[j]!==0&&(digits[j]*10+digits[i])%modulus===0))unordered++;
    const target=modulus===1?'두 자리 자연수':modulus===2?'두 자리 짝수':'두 자리 자연수 중 3의 배수';
    cards.push(makeItem({prompt:`숫자 카드 ${digits.join(', ')}(각 한 장) 중 서로 다른 두 장을 임의로 뽑아 나란히 놓는다. 만들 수 있는 ${target}의 개수를 구하시오.`,n:count,explain:`십의 자리 0과 카드 재사용을 빼고 조건에 맞게 세면 ${count}개.`,unitConcept:'카드로 두 자리 자연수 만들기',difficulty:modulus===1?2:3,kind:'cards',args:{digits,modulus},distractors:[wrong(cardCount(digits,modulus,true),1,'m2s2-u6.leading-zero','cards-leading-zero',[digits,modulus]),wrong(cardCount(digits,modulus,false,true),1,'m2s2-u6.same-card-reused','cards-reuse',[digits,modulus]),wrong(unordered,1,'m2s2-u6.order-ignored','cards-unordered',[digits,modulus]),wrong((digits.length-1)*2,1,'m2s2-u6.sum-product-confusion','add-card-stages',[digits.length]),wrong(digits.length*(digits.length-1),1,'m2s2-u6.leading-zero','all-ordered-cards',[digits.length]),wrong(digits.length-1,1,'m2s2-u6.second-stage-omitted','card-first-stage',[digits.length])]}));
  }
}
for(let n=4;n<=18;n++)for(const ordered of [true,false]){
  const count=ordered?n*(n-1):n*(n-1)/2;if(count>=60)continue;
  roles.push(makeItem({prompt:`후보 ${n}명 중 ${ordered?'회장 한 명과 부회장 한 명':'서로 역할이 같은 대표 두 명'}을 뽑는 경우의 수를 구하시오. ${ordered?'(단, 한 사람이 두 역할을 맡지 않는다.)':''}`.trim(),n:count,explain:ordered?`회장 ${n}가지마다 부회장 ${n-1}가지, ${n} × ${n-1} = ${count}가지.`:`두 명의 순서를 바꿔도 같으므로 ${n} × ${n-1} ÷ 2 = ${count}가지.`,unitConcept:ordered?'역할이 다른 대표 뽑기':'역할이 같은 대표 뽑기',difficulty:4,kind:'roles',args:{n,ordered},distractors:[wrong(ordered?n*(n-1)/2:n*(n-1),1,'m2s2-u6.roles-order-confusion','opposite-role-order',[n,ordered]),wrong(n*n,1,'m2s2-u6.same-person-twice','square',[n]),wrong(2*n-1,1,'m2s2-u6.sum-product-confusion','add-role-stages',[n])]}));
}
for(const kind of ['sum-eq','sum-le','sum-ge','product-eq','difference-eq','product-multiple'])for(let k=kind==='difference-eq'?0:2;k<=(kind==='product-eq'?36:kind==='difference-eq'?5:kind==='product-multiple'?10:12);k++){
  const [count,unordered]=diceCount(kind,k);if(count<=1||count===36)continue;
  dice.push(makeItem({prompt:`서로 다른 두 개의 주사위를 동시에 던질 때, ${diceText(kind,k)} 경우의 수를 구하시오.`,n:count,explain:`첫째 주사위와 둘째 주사위를 구별해 조건에 맞는 순서쌍을 세면 ${count}가지.`,unitConcept:'서로 다른 두 주사위의 경우의 수',difficulty:2,kind:'dice',args:{test:kind,k},distractors:[wrong(unordered,1,'m2s2-u6.order-ignored','unordered-count',[unordered]),wrong(12,1,'m2s2-u6.sum-product-confusion','sum',[6,6]),wrong(6,1,'m2s2-u6.second-stage-omitted','first-count',[6]),wrong(21,1,'m2s2-u6.order-and-event-ignored','unordered-all-dice',[]),wrong(36,1,'m2s2-u6.event-condition-ignored','product',[6,6])]}));
}
writePack('m2s2-u6','경우의 수',['[9수04-05]'],[[[introItem()],1],[product.filter(q=>q&&q.answerNumeric<60),61],[sum,90],[cards,182],[roles,12],[dice,54]]);
