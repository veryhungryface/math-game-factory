// Independent validation only. No production module imports or browser access.
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';

const integer = (n, label) => {
  if (!Number.isSafeInteger(n)) throw new TypeError(`${label} must be a safe integer: ${n}`);
  return BigInt(n);
};

export function oracle(total, a, b) {
  const N = integer(total, 'total'), A = integer(a, 'a'), B = integer(b, 'b');
  if (N <= 0n || A <= 0n || B <= 0n) throw new RangeError('Positive total and ratio required');
  const denominator = A + B;
  if (N * A % denominator || N * B % denominator) throw new RangeError('Nonintegral troop shares');
  const left = N * A / denominator, right = N * B / denominator;
  assert.equal(left + right, N);
  assert.equal(left * B, right * A);
  return {left: Number(left), right: Number(right), total, a, b,
    expression: `${total} × ${a} ÷ (${a} + ${b}) = ${left}; ${total} − ${left} = ${right}`};
}

export function accepts(total, a, b, left, right = total - left) {
  if (![total,a,b,left,right].every(Number.isSafeInteger)) return false;
  if (total <= 0 || a <= 0 || b <= 0 || left < 0 || right < 0) return false;
  return BigInt(left) + BigInt(right) === BigInt(total)
    && BigInt(left) * BigInt(b) === BigInt(right) * BigInt(a);
}

// Only observable problem quantities enter this routine; never claimed answers.
export function validateRows(rows) {
  const issues = [], verified = [];
  rows.forEach((p, index) => {
    try {
      const calculated = oracle(p.total, p.a, p.b);
      if (p.left !== undefined) assert.equal(p.left, calculated.left, 'left claim');
      if (p.right !== undefined) assert.equal(p.right, calculated.right, 'right claim');
      verified.push({index, id:p.id ?? null, ...calculated});
    } catch (error) { issues.push({index,id:p.id??null,problem:p,message:error.message}); }
  });
  return {verdict:issues.length?'fail':'pass',count:rows.length,issues,verified};
}

// Exact baseline for each deterministic policy on the actual observed campaign
// distribution. Tutorial questions MUST be excluded by the caller.
export function policyBaselines(rows) {
  if (!rows.length) throw new RangeError('No ordinary campaign questions');
  const policies = {
    'always-half-floor': p=>Math.floor(p.total/2),
    'always-half-round': p=>Math.round(p.total/2),
    'always-left-12': p=>Math.min(12,p.total),
    'always-left-20': p=>Math.min(20,p.total),
    'always-left-40-percent': p=>Math.round(p.total*0.4),
    'always-left-end': ()=>0,
    'always-right-end': p=>p.total,
    'repeat-sweep-end-left': ()=>0,
    'repeat-sweep-end-right': p=>p.total,
  };
  const result = Object.fromEntries(Object.entries(policies).map(([name,fn])=> {
    let correct = 0;
    for (const p of rows) if (accepts(p.total,p.a,p.b,fn(p))) correct++;
    return [name,{trials:rows.length,expectedCorrect:correct,expectedAccuracy:correct/rows.length,
      basis:'Exact solution frequency at this policy allocation in ordinary campaign distribution'}];
  }));
  const expected = rows.reduce((sum,p)=>sum+1/(p.total+1),0);
  result['uniform-divider-integer-0-N']={trials:rows.length,expectedCorrect:expected,
    expectedAccuracy:expected/rows.length,
    basis:'Exactly one integer solution among N+1 permitted allocations for each problem'};
  result['no-input']={trials:0,expectedProgress:0,basis:'No submitted first attempt; accuracy undefined'};
  return result;
}

export function selfTest() {
  const cases = [
    [30,2,3,12,18], [30,3,2,18,12], [60,4,6,24,36], [60,6,4,36,24],
    [48,4,4,24,24], [18,1,5,3,15], [18,5,1,15,3], [90,4,5,40,50],
    [90,5,4,50,40], [15,2,4,5,10], [14,9,9,7,7], [35,3,4,15,20]
  ];
  let predicates = 0;
  for (const [N,a,b,L,R] of cases) {
    const result = oracle(N,a,b);
    assert.equal(result.left,L); assert.equal(result.right,R);
    for (let x=0;x<=N;x++) {assert.equal(accepts(N,a,b,x),x===L);predicates++;}
    for (const bad of [-1,N+1,0.5,NaN,Infinity,'12',null]) assert.equal(accepts(N,a,b,bad),false);
  }
  for (const input of [[5,1,1],[0,1,1],[12,0,1],[12,1,-1],[12,1.5,2]]) assert.throws(()=>oracle(...input));
  const pool=cases.map(([total,a,b,left,right])=>({total,a,b,left,right}));
  assert.equal(validateRows(pool).verdict,'pass');
  assert.equal(validateRows([{total:30,a:2,b:3,left:18,right:12}]).verdict,'fail');
  const baseline=policyBaselines(pool);
  assert.equal(baseline['always-half-round'].expectedCorrect,2);
  assert.equal(baseline['always-left-end'].expectedCorrect,0);
  return {cases:cases.length,allocationPredicates:predicates,status:'pass'};
}

if (process.argv[1] && import.meta.url===pathToFileURL(process.argv[1]).href) {
  console.log(JSON.stringify(selfTest(),null,2));
}
