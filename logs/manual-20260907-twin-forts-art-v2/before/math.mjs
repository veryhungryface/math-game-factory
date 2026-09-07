// The game and QA use this same finite pool. All quantities are integers.
export const TUTORIAL = Object.freeze({ id:'guide', total:30, a:2, b:3, left:12, right:18, unit:6, tutorial:true });
export const POOL = Object.freeze((() => {
  const rows=[];
  for(let a=1;a<=9;a++) for(let b=1;b<=9;b++) for(let k=1;k<=10;k++) {
    const total=(a+b)*k;
    if(total<12 || total>90 || a*k<3 || b*k<3) continue;
    rows.push(Object.freeze({id:`${total}-${a}-${b}`,total,a,b,left:a*k,right:b*k,unit:k,tutorial:false}));
  }
  return rows;
})());
export function isCorrect(p,left,right=p.total-left) {
  return Number.isInteger(left)&&Number.isInteger(right)&&left>=0&&right>=0&&left+right===p.total&&left*p.b===right*p.a;
}
export function shuffledPool(rng=Math.random) {
  const rows=[...POOL];
  for(let i=rows.length-1;i>0;i--){const j=Math.floor(rng()*(i+1));[rows[i],rows[j]]=[rows[j],rows[i]];}
  return rows;
}
export function campaign(rng=Math.random) {
  const rows=shuffledPool(rng), used=new Set(), result=[{...TUTORIAL}];
  for(let round=1;round<9;round++) {
    const candidates=rows.filter(p=>!used.has(p.id)&&(round<3?p.a+p.b<=8:round<6?p.a+p.b<=13:true));
    const p=candidates[0];used.add(p.id);result.push({...p});
  }
  return result;
}
export function sampleProblems(n) {
  const count=Math.max(0,Math.floor(Number(n)||0)), result=[];
  while(result.length<count) for(const p of shuffledPool()) {
    result.push({...p,prompt:`병사 ${p.total}명을 왼쪽과 오른쪽에 ${p.a} : ${p.b}로 비례배분하세요. 왼쪽에는 몇 명을 배치해야 하나요?`,choices:null,answer:`${p.left}명`,answerNumeric:p.left,unitConcept:'비례배분'});
    if(result.length>=count) break;
  }
  return result;
}
