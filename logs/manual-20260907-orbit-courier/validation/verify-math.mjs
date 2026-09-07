import fs from 'node:fs';
import assert from 'node:assert/strict';

// The oracle uses only integer operands, never the game's supplied answer.
export function oracle(a, b) {
  assert.ok(Number.isSafeInteger(a) && Number.isSafeInteger(b));
  return BigInt(a) * BigInt(b);
}
export function checkSamples(samples) {
  const errors = [], unique = new Set(), products = new Set();
  for (const [i,p] of samples.entries()) {
    try {
      const match = p.prompt.match(/(\d+)\s*[×x*]\s*(\d+)/u);
      const korean = p.prompt.match(/대원\s*(\d+)명[\s\S]*한 명당\s*(\d+)씩/u);
      const fromPrompt = match ? {a:Number(match[1]),b:Number(match[2])} : korean ? {a:Number(korean[2]),b:Number(korean[1])} : null;
      assert.ok(fromPrompt,'Cannot independently parse operands from displayed prompt');
      const operands = p.operands ?? ((p.a!==undefined&&p.b!==undefined)?{a:p.a,b:p.b}:fromPrompt);
      assert.ok(operands, 'No independent operands in prompt or sample');
      const {a,b}=operands, expected=oracle(a,b);
      assert.deepEqual(operands,fromPrompt,'Problem data disagrees with displayed Korean quantities');
      assert.ok(a>=100 && a<=329 && b>=2 && b<=6, 'Operands outside declared generation pool');
      assert.ok(expected<=999n, 'Product outside declared cargo range');
      assert.equal(BigInt(String(p.answer).replace(/\s*개$/,'')),expected, 'Answer differs from independent integer product');
      if(p.answerNumeric!==undefined) assert.equal(p.answerNumeric,Number(expected));
      assert.ok(!/(일|십|백|천|만|억|소수 ?(첫|둘|셋)째)( ?의)? ?자리에서 ?(올림|버림|반올림)/u.test(p.prompt));
      if(match) assert.equal(oracle(Number(match[1]),Number(match[2])),expected,'Displayed equation mismatch');
      const v=Number(expected), hundreds=Math.floor(v/100), tens=Math.floor(v%100/10), ones=v%10;
      assert.equal(100n*BigInt(hundreds)+10n*BigInt(tens)+BigInt(ones),expected);
      unique.add(`${a}×${b}`);products.add(String(expected));
    } catch(e) { errors.push({index:i,problem:p,issue:e.message}); }
  }
  return {checked:samples.length,unique_equations:unique.size,unique_products:products.size,errors};
}
if(process.argv[1]?.endsWith('verify-math.mjs')) {
  if(!process.argv[2]) throw new Error('usage: node verify-math.mjs <samples-or-qa-report.json>');
  const input=JSON.parse(fs.readFileSync(process.argv[2],'utf8'));
  const result=checkSamples(Array.isArray(input)?input:input.problems_sample ?? input.samples);
  console.log(JSON.stringify(result,null,2));
  if(result.errors.length)process.exitCode=1;
}
