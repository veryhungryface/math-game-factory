// Visual growth timing. These scales do not enter area, box counts, score or harvest decisions.
const ease=t=>1-(1-Math.max(0,Math.min(1,t)))**3;
export function carrotGrowth(age){
 if(age<.28)return{stage:'carrot-sprout',scale:.35+.65*ease(age/.18)};
 if(age<.66)return{stage:'carrot-young',scale:.70+.30*ease((age-.28)/.38)};
 return{stage:'carrot',scale:.68+.32*ease((age-.66)/.41)};
}
export const CARROT_STAGE_HEIGHTS=Object.freeze({'carrot-sprout':.25,'carrot-young':.60});
