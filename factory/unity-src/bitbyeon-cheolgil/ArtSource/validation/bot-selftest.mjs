// 4종 무뇌 봇 × 200판. 오답 뒤 같은 릴레이를 재시도하는 현재 진행 규칙까지 모사한다.
// 측정값은 문항별 첫 잠금만 세며, 입력이 없으면 문항 수와 진도가 모두 0이다.
import fs from "node:fs";

const answerBands = [[5, 10, 15], [13, 17, 25], [12, 8, 24]];
const min = 4, max = 25, chance = 1 / (max - min + 1);
const games = 200;

function play(kind) {
  let attempts = 0, correct = 0, completions = 0, progress = 0;
  let seed = 0x41c6ce57;
  const nextRandom = () => {
    seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0;
    return min + (seed % (max - min + 1));
  };
  for (let g = 0; g < games; g++) {
    let lives = 3, reached = 0, firstPracticeMistake = true, cycleCursor = 0;
    for (let r = 0; r < 9; r++) {
      if (kind === "no-input") break;
      const band = Math.floor(r / 3);
      const answer = answerBands[band][(r % 3 + g + band) % 3];
      let first = true, solved = false;
      while (lives > 0 && !solved) {
        const guess = kind === "fixed" ? 4 : kind === "cycle" ? min + (cycleCursor++ % 22) : nextRandom();
        if (first) { attempts++; if (guess === answer) correct++; first = false; }
        if (guess === answer) { solved = true; progress++; reached++; break; }
        if (r === 0 && firstPracticeMistake) firstPracticeMistake = false;
        else lives--;
      }
      if (lives <= 0) break;
    }
    if (reached === 9 && lives > 0) completions++;
  }
  return {
    games,
    firstAttemptCorrect: correct,
    firstAttemptTotal: attempts,
    firstAttemptRate: attempts ? correct / attempts : 0,
    chance,
    completionRate: completions / games,
    progress
  };
}

const result = {
  generatedAt: new Date().toISOString(),
  fixed: play("fixed"),
  cycle: play("cycle"),
  random: play("random"),
  noInput: play("no-input")
};
for (const [name, row] of Object.entries(result)) {
  if (name === "generatedAt") continue;
  if (row.firstAttemptRate > chance + 1e-12) throw new Error(name + " exceeds chance");
  if (name === "noInput" && row.progress !== 0) throw new Error("no-input advanced");
}
fs.writeFileSync(new URL("./bot-results.json", import.meta.url), JSON.stringify(result, null, 2) + "\n");
console.log(JSON.stringify(result, null, 2));
