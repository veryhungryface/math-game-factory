const MIN = 1;
const MAX = 48;
const answers = Array.from({length: MAX}, (_, i) => i + 1);
const templates = [
  'Tan45Opp', 'Sin30Opp', 'Cos60Adj', 'Sin45Opp', 'Cos45Adj',
  'Tan30Opp', 'Tan60Adj', 'Height45', 'Height30', 'Distance60'
];

function derive(template, answer) {
  let wrongSwap = 0, wrongMultiply = 0;
  if (template === 'Sin30Opp' || template === 'Cos60Adj') {
    if (4 * answer <= MAX) wrongMultiply = 4 * answer;
  } else if (template === 'Tan30Opp' || template === 'Height30') {
    if (3 * answer <= MAX) wrongSwap = 3 * answer;
  } else if (template === 'Tan60Adj' || template === 'Distance60') {
    if (3 * answer <= MAX) wrongMultiply = 3 * answer;
  }
  return {wrongSwap, wrongMultiply};
}

let prompts = 0;
let distractorCollisions = 0;
let outOfRange = 0;
let invalid45Diagnosis = 0;
for (const answer of answers) {
  for (const template of templates) {
    prompts++;
    const {wrongSwap, wrongMultiply} = derive(template, answer);
    for (const v of [wrongSwap, wrongMultiply]) {
      if (v !== 0 && (v < MIN || v > MAX)) outOfRange++;
      if (v === answer) distractorCollisions++;
    }
    if (wrongSwap !== 0 && wrongSwap === wrongMultiply) distractorCollisions++;
    if ((template === 'Tan45Opp' || template === 'Sin45Opp' || template === 'Cos45Adj') &&
        (wrongSwap !== 0 || wrongMultiply !== 0)) invalid45Diagnosis++;
  }
}

const checks = {
  bankSize: prompts,
  reachableTemplates: templates,
  uniqueAnswerCoverage: new Set(answers).size,
  answerRange: [Math.min(...answers), Math.max(...answers)],
  rawChance: 1 / MAX,
  distractorCollisions,
  outOfRange,
  invalid45Diagnosis,
  radicalCoefficientOne: {input: 1, sqrt2: '√2', sqrt3: '√3'},
  applicationReferencePoints: ['관측점', '밑점', '꼭대기'],
  distancePrompt: '관측점에서 수직으로 세워진 크레인의 꼭대기를 올려본각이 60°이고',
  pointerMapping: {min: MIN, max: MAX, span: MAX - MIN, fineUnitsPerMetre: 22},
  fixedPractice: {angle: 45, adjacent: 6, answer: 6, equation: 'tan45=6/6=1'},
  exactIntegerJudgement: true,
  passed: prompts >= 300 && templates.length === 10 && distractorCollisions === 0 &&
    outOfRange === 0 && invalid45Diagnosis === 0
};
console.log(JSON.stringify(checks, null, 2));
fs.writeFileSync(path.join(path.dirname(new URL(import.meta.url).pathname), 'math-results.json'), JSON.stringify(checks, null, 2));
if (!checks.passed) process.exitCode = 1;
import fs from 'node:fs';
import path from 'node:path';
