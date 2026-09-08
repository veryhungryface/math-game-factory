// One genuine numeric pool for both play and independent QA. A cell is 1 m².
export const POOL=Object.freeze((()=>{
  const list=[];
  for(let width=3;width<=12;width++)for(let height=3;height<=12;height++){
    const targetBoxes=width*height;
    if(targetBoxes<12)continue;
    list.push(Object.freeze({id:`field-${width}-${targetBoxes}`,width,height,targetBoxes}));
  }
  return list;
})());
export const TUTORIAL=Object.freeze({id:'first-carrots',width:4,height:3,targetBoxes:12,tutorial:true,crop:'carrot'});
export const CROPS=Object.freeze({carrot:{name:'당근',color:'#e88740',icon:'🥕'},strawberry:{name:'딸기',color:'#df6371',icon:'🍓'},corn:{name:'옥수수',color:'#e3b54d',icon:'🌽'}});
export function isCorrect(problem,height){return Number.isInteger(height)&&height>=1&&height<=12&&problem.width*height===problem.targetBoxes;}
export function shuffledPool(random=Math.random){const list=[...POOL];for(let i=list.length-1;i>0;i--){const j=Math.floor(random()*(i+1));[list[i],list[j]]=[list[j],list[i]];}return list;}
export function createDay(random=Math.random){const list=shuffledPool(random),used=new Set(['field-4-12']),day=[{...TUTORIAL}];for(let i=1;i<9;i++){const available=list.filter(p=>!used.has(p.id)&&(i<3?p.width<=7&&p.height<=7:i<6?p.width<=10:true));const p=available[0];used.add(p.id);day.push({...p,tutorial:false,crop:i<3?'carrot':i<6?'strawberry':'corn'});}return day;}
export function sampleProblems(n){const result=[],count=Math.max(0,Math.floor(Number(n)||0));while(result.length<count)for(const p of shuffledPool()){result.push({...p,prompt:`이 밭은 1m²마다 작물 1상자를 수확합니다. ${p.targetBoxes}상자를 수확하려고 가로가 ${p.width}m인 직사각형 밭을 만들 때, 세로는 몇 m여야 하나요?`,choices:null,answer:`${p.height}m`,answerNumeric:p.height,unitConcept:'직사각형의 넓이 역문제'});if(result.length>=count)break;}return result;}
