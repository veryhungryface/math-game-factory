/** Independent review oracle. Does not import the production math module. */
export function oracle(problem, chosenHeight) {
  const { width, targetBoxes } = problem;
  if (!Number.isSafeInteger(width) || width < 3 || width > 12) throw new Error(`Invalid width ${width}`);
  if (!Number.isSafeInteger(targetBoxes) || targetBoxes < 12 || targetBoxes > 144) throw new Error(`Invalid target ${targetBoxes}`);
  const w = BigInt(width), target = BigInt(targetBoxes);
  if (target % w !== 0n) throw new Error(`Nonintegral height ${targetBoxes}/${width}`);
  const expectedHeight = target / w;
  if (expectedHeight < 3n || expectedHeight > 12n) throw new Error(`Out-of-range answer ${expectedHeight}`);
  const validInput = Number.isSafeInteger(chosenHeight) && chosenHeight >= 1 && chosenHeight <= 12;
  const chosenArea = validInput ? w * BigInt(chosenHeight) : null;
  return {
    width, targetBoxes, expectedHeight: Number(expectedHeight),
    validInput, correct: validInput && chosenArea === target,
    chosenArea: chosenArea === null ? null : Number(chosenArea),
    difference: chosenArea === null ? null : Number(chosenArea - target),
    explanation: `${targetBoxes}상자 ÷ (1상자/m²) = ${targetBoxes}m²; ${targetBoxes} ÷ ${width} = ${expectedHeight}m`,
  };
}
export function expectedNumericDomain() {
  const result = [];
  for (let width = 3; width <= 12; width++) for (let height = 3; height <= 12; height++) {
    const targetBoxes = Number(BigInt(width) * BigInt(height));
    if (targetBoxes >= 12 && targetBoxes <= 144) result.push({ width, height, targetBoxes });
  }
  return result;
}
export const unusualInputs = [NaN, Infinity, -Infinity, undefined, null, true, false, '3', '', [], {}, -1, 0, 1.5, 2.5, 12.5, 13, Number.MAX_SAFE_INTEGER];
/** Input order must contain ordinary questions only. Policy rates are not completion rates. */
export function policyBaselines(ordinaryProblems) {
  if (!ordinaryProblems.length) throw new Error('No ordinary problems');
  const fixed = (height) => ordinaryProblems.filter(p => oracle(p, height).correct).length;
  return {
    ordinaryCount: ordinaryProblems.length,
    initialTwo: { height: 2, correct: fixed(2), rate: fixed(2) / ordinaryProblems.length },
    fixedSix: { height: 6, correct: fixed(6), rate: fixed(6) / ordinaryProblems.length },
    alwaysMaximum: { height: 12, correct: fixed(12), rate: fixed(12) / ordinaryProblems.length },
    uniformInteger1To12: { probability: '1/12', rate: 1 / 12 },
    note: 'Rates are policy-specific chance baselines over the supplied actual ordinary-question distribution. Tutorial excluded. Small UI samples validate input/result agreement, not population accuracy.',
  };
}
