import fs from 'node:fs';
const results=[];
for(const slug of ['multiply-siege','fraction-rail','decimal-drift']){
 const samples=JSON.parse(fs.readFileSync(`logs/manual-20260907-three-games/${slug}/qa.json`)).problems_sample;
 const errors=[];let checkedChoices=0;
 const assert=(ok,p,why)=>{if(!ok)errors.push({id:p.id,why,prompt:p.prompt});};
 for(const p of samples){
  if(slug==='multiply-siege'){
   const m=p.prompt.match(/(\d+)\s*×\s*(\d+)/);
   assert(!!m,p,'prompt arithmetic missing');if(!m)continue;
   let answer=0n;for(let n=0n;n<BigInt(m[2]);n++)answer+=BigInt(m[1]);
   assert(answer===BigInt(p.answer),p,'repeated addition differs');
   assert(Number(answer)===p.answerNumeric,p,'numeric mismatch');
   if(p.charges){let reachable=new Set([0]);for(let k=0;k<p.maxShots;k++)for(const v of [...reachable])for(const c of p.charges)for(const g of p.groups){if(v+c*g<=Number(answer))reachable.add(v+c*g);}
    assert(reachable.has(Number(answer)),p,'unsolvable with actual ammunition/shot limit');}
  }else if(slug==='fraction-rail'){
   const delta=BigInt(p.targetNumerator)-BigInt(p.baseNumerator);
   const expected=delta<0n?-delta:delta;
   const [n,d]=p.answer.split('/').map(BigInt);
   assert(expected*d===n*BigInt(p.denominator),p,'cross-product mismatch');
   assert(expected>=0n,p,'negative fraction outside intended domain');
   assert((delta<0n?'subtract':'add')===p.operation,p,'wrong operation');
   let possible=false;for(let a=0;a<=2;a++)for(let b=0;b<=2;b++)for(let c=0;c<=2;c++)if(BigInt(a*p.pieces[0]+b*p.pieces[1]+c*p.pieces[2])===expected)possible=true;
   assert(possible,p,'unsolvable with actual rail stock');
  }else{
   const p2=p.model;let hits=0;
   for(let a=0;a<3;a++)for(let b=0;b<3;b++){
    const x=BigInt(p2.start)+BigInt(p2.options[0][a])+BigInt(p2.options[1][b]);checkedChoices++;
    assert(x>=0n,p,'negative fuel route');
    if(x===BigInt(p2.target))hits++;
   }
   assert(hits===1,p,'nonunique or missing solution');
   const correct=p2.start+p2.options[0][p2.solution[0]]+p2.options[1][p2.solution[1]];
   assert(correct===p2.target,p,'wrong route');
   assert(p.choices.includes(p.answer),p,'answer absent');
   assert(new Set(p.choices).size===9,p,'duplicate route choices');
   assert(p.answerNumeric===correct/100,p,'numeric mismatch');
  }
 }
 results.push({slug,verdict:errors.length?'fail':'pass',samples:samples.length,checkedChoices,errors});
}
fs.writeFileSync('logs/manual-20260907-three-games/mathcheck.json',JSON.stringify(results,null,2));
console.log(JSON.stringify(results,null,2));
process.exitCode=results.some(x=>x.errors.length)?1:0;
