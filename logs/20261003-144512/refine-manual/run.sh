set -uo pipefail
cd /Users/sitpo/math-game-factory
R=factory/state/pending/.refine-20261003-144512
source factory/config.sh; export SCHOOL=middle; ROOT=$PWD
eval "$(sed -n '/^run_timeout()/,/^}/p; /^codex_reasoning_for()/,/^}/p; /^codex_run()/,/^}/p; /^prompt_file()/,/^}/p' factory/run.sh)"
P="$(prompt_file factory/prompts/16-refine.md)

---
## 이번 실행의 예외 (수동 실행)
- 작업 폴더는 \`factory/work/\` 가 아니라 **\`$R/\`** 다. 거기 있는 \`concept-2.json\`, \`concept-3.json\` 두 개만 보완해 **같은 자리에 덮어써라.** \`factory/work/\` 는 다른 빌드가 쓰는 중이니 절대 건드리지 마라.
- 세트의 1번 안 「빛 사수」(bit-sasu)는 이미 빌드 중이다(\`$R/sibling-built-bit-sasu.json\`). 이 안은 고치지 말고 **형제로만** 비교해라 — 두 안이 이것과도 동사·디자인 시스템이 달라야 한다.
- 이 두 안은 원래 심사에서 실격됐다: concept-2(삼각 릴레이) D1·D6, concept-3(크레인 각도기) D6·D9. 심사 근거는 sibling 파일의 \`_judge.fun_gate\` 에 있다. 이 사유를 반드시 해소해라.
- \`build_order_hint\` 는 2·3 중에서 매겨라.

## 이번 슬롯
\`\`\`json
$(cat $R/slot.json)
\`\`\`"
codex_run 1500 $R/refine.log "$P" "$CODEX_MODEL_SMART"; echo rc=$?
