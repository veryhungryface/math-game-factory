#!/usr/bin/env bash
#
# 중학교 빈 단원 배치 생산 (2026-10-05 사용자 지시)
#   「지금부터 9시간 동안 클로드 사용량을 다 쓰고 싶어. 중학교 1, 2, 3학년에서 못 채운 단원이 있으면
#    게임을 만들어 보자. 병렬로 만들어 봐.」
#
#   bash factory/batch-middle.sh                 기본: 9시간, 레인 4개, 빈 단원 전부(학년 섞어서)
#   BATCH_HOURS=6 BATCH_LANES=3 bash factory/batch-middle.sh
#   BATCH_UNITS="m1s1-u1,m3s2-u2" bash factory/batch-middle.sh
#
# 구조: 이 스크립트가 지휘자다(run.sh 의 전역 락을 쓰지 않는다).
#  1) 빈 단원마다 기획 1안(claude opus) → 보완(claude opus, 16-refine) → factory/state/pending/batch/ 에 넣는다.
#     기획은 레인이 곧 쓸 만큼만 앞서 만든다(대기 ≤ BATCH_LOOKAHEAD) — 못 쓸 기획에 사용량을 버리지 않는다.
#  2) 레인 풀: 동시에 BATCH_LANES 개까지 run.sh 레인(MGF_LANE_ITEM)을 띄운다. 레인은 아트(codex 이미지)부터
#     빌드·수정은 claude opus, 검산·검수는 codex sol(교차 검증)로 만들고 스스로 게시·배포·보고한다.
#  3) 마감(BATCH_HOURS) 1.5시간 전부터 새 레인을 띄우지 않는다. 도는 레인은 끝까지 간다.
#  배치 중에는 factory/state/batch.active 가 크론의 일반 회차를 쉬게 한다(run.sh).
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT" || exit 1
export SCHOOL=middle
# shellcheck source=/dev/null
source "$ROOT/factory/config.sh"

BATCH_ID="B$(date +%Y%m%d-%H%M)"
LOG_DIR="$ROOT/logs/$BATCH_ID"; mkdir -p "$LOG_DIR"
QUEUE="$ROOT/factory/state/pending/batch"; mkdir -p "$QUEUE"
HOURS="${BATCH_HOURS:-9}"; LANES="${BATCH_LANES:-4}"; LOOKAHEAD="${BATCH_LOOKAHEAD:-2}"; DESIGN_PAR="${BATCH_DESIGN_PAR:-4}"
DEADLINE=$(( $(date +%s) + HOURS*3600 ))
STOP_SPAWN=$(( DEADLINE - 5400 ))

log()  { printf '[%s] %s\n' "$(date +%H:%M:%S)" "$*" | tee -a "$LOG_DIR/batch.log" >&2; }
step() { log "━━ $*"; }
die()  { log "💀 $*"; exit 1; }

# run.sh 의 러너·프롬프트 함수를 그대로 쓴다(복붙 금지 — 한 곳만 고치게).
eval "$(sed -n '/^run_timeout()/,/^}/p; /^lane_prompt()/,/^}/p; /^claude_run()/,/^}/p; /^codex_reasoning_for()/,/^}/p; /^codex_run()/,/^}/p; /^prompt_file()/,/^}/p; /^jqv()/p' "$ROOT/factory/run.sh")"

echo $$ > "$ROOT/factory/state/batch.active"
trap 'rm -f "$ROOT/factory/state/batch.active"' EXIT

# ── 단원 목록: 게시작이 없는 중1·2·3 단원, 학년을 섞어서 ─────────────────────
if [ -n "${BATCH_UNITS:-}" ]; then
  UNITS="$(echo "$BATCH_UNITS" | tr ',' '\n')"
else
  UNITS="$(node -e '
    const fs=require("fs"),path=require("path");
    const cur=JSON.parse(fs.readFileSync("curriculum/2022-middle-math.json","utf8"));
    const have=new Set();
    for (const d of fs.readdirSync("public/g")) { try { const m=JSON.parse(fs.readFileSync(path.join("public/g",d,"meta.json"),"utf8"));
      if (m.qa && m.qa.passed===false && !m.qa.manual_release) continue; if (m.unit && m.unit.id) have.add(m.unit.id); } catch {} }
    const by={1:[],2:[],3:[]};
    for (const u of cur.units) if (!have.has(u.id) && by[u.grade]) by[u.grade].push(u.id);
    // 2학기(지금 가르치는 학기)를 앞에, 학년을 번갈아
    for (const g of [1,2,3]) by[g].sort((a,b)=>(b.includes("s2")-a.includes("s2"))||a.localeCompare(b));
    const out=[]; while (by[1].length||by[2].length||by[3].length) for (const g of [2,3,1]) if (by[g].length) out.push(by[g].shift());
    console.log(out.join("\n"));')"
fi
TOTAL_UNITS=$(echo "$UNITS" | grep -c .)
log "배치 $BATCH_ID — ${HOURS}시간, 레인 ${LANES}개, 빈 단원 ${TOTAL_UNITS}개: $(echo $UNITS | tr '\n' ' ')"

# ── 기획 1안 + 보완 (단원 1개) ─────────────────────────────────────────────
# 짝수 번째는 아기자기 계열을 강제해 배치 전체가 한 화풍으로 수렴하지 않게 한다.
design_unit() {
  local unit="$1" idx="$2" wdir="factory/work-lanes/$BATCH_ID-design-$1"
  rm -rf "$ROOT/$wdir"; mkdir -p "$ROOT/$wdir"
  export MGF_WORK="$wdir"
  node factory/lib/pick-slot.mjs --unit "$unit" --write >"$LOG_DIR/slot-$unit.log" 2>&1 || { log "⚠️  $unit 슬롯 실패"; return 1; }
  local slot; slot="$(cat "$ROOT/$wdir/slot.json")"
  local style
  if [ $((idx % 2)) -eq 0 ]; then
    style="**이 안은 아기자기 계열로 가라** — 장난감 미니어처 디오라마, 둥글고 통통한 형태, 이 게임 전용 오리지널 귀여운 캐릭터/생물이 정답·오답·대기에 반응(_school-middle.md §7)."
  else
    style="화풍은 자유지만 _school-middle.md §7 의 「왼쪽 큰 제목 + 단일 버튼 + 빈 탁자 위 도구」 틀은 금지다. 살아 있는 세계(생물·움직임)를 넣어라."
  fi
  local siblings; siblings="$(cat "$LOG_DIR/titles.txt" 2>/dev/null | tail -12)"
  claude_run "$T_DESIGN" "$LOG_DIR/design-$unit.log" "$(prompt_file factory/prompts/10-design.md)

---
## 이번 슬롯

\`\`\`json
$slot
\`\`\`

## 이번 배치 지시 (2026-10-05 중학교 빈 단원 배치)
- 너는 이 단원의 **유일한 기획자**다. 기획안 1개만 만든다.
- $style
- 이번 배치에서 이미 기획된 게임(제목 · 정체성) — 이것들과 화면·동사·세계가 겹치지 마라:
$siblings
- 레퍼런스 광산: \`references/game-references.json\`. \`mechanic_pool\` 은 추천일 뿐이다.

## 출력 파일
\`factory/work/concept-1.json\` 로 저장해라. \`n\` 필드는 1 이다." "$CLAUDE_MODEL_SMART"
  [ -f "$ROOT/$wdir/concept-1.json" ] || { log "⚠️  $unit 기획 실패 — $(tail -2 "$LOG_DIR/design-$unit.log" | tr '\n' ' ')"; return 1; }
  claude_run "$T_JUDGE" "$LOG_DIR/refine-$unit.log" "$(prompt_file factory/prompts/16-refine.md)

---
## 이번 배치 예외
- 이번에는 기획안이 \`concept-1.json\` **하나뿐**이다. 형제 비교 대신 카탈로그(중학교 게시작 썸네일)와 아래 배치 목록을 형제로 보고 보완해라.
- 이번 배치에서 이미 기획된 게임:
$siblings

## 이번 슬롯
\`\`\`json
$slot
\`\`\`" "$CLAUDE_MODEL_SMART"
  local slug title; slug="$(jqv "$ROOT/$wdir/concept-1.json" .slug)"; title="$(jqv "$ROOT/$wdir/concept-1.json" .title)"
  [ -n "$slug" ] || { log "⚠️  $unit 기획에 slug 없음"; return 1; }
  if [ -e "$ROOT/public/g/$slug" ]; then slug="$slug-$(echo "$unit" | tr -d 's-')"; jq --arg s "$slug" '.slug=$s' "$ROOT/$wdir/concept-1.json" > "$ROOT/$wdir/c.tmp" && mv "$ROOT/$wdir/c.tmp" "$ROOT/$wdir/concept-1.json"; fi
  local dst="$QUEUE/$(printf '%02d' "$idx")-$unit"
  mkdir -p "$dst.tmp" && cp "$ROOT/$wdir/concept-1.json" "$dst.tmp/chosen.json" && cp "$ROOT/$wdir/slot.json" "$dst.tmp/slot.json" && mv "$dst.tmp" "$dst"
  echo "- $title ($slug, $unit) · $(jqv "$ROOT/$wdir/concept-1.json" .art_direction.identity_name)" >> "$LOG_DIR/titles.txt"
  log "📝 기획 완료 [$idx] $unit → 「$title」 ($slug) fun예측 $(jqv "$ROOT/$wdir/concept-1.json" ._refine.predicted_fun)"
}

# ── 레인 ───────────────────────────────────────────────────────────────────
spawn_lane() {  # $1=대기열 항목 폴더 $2=레인 번호
  local item="$1" k="$2" dst="$LOG_DIR/lane-$2-$(basename "$1")"
  mv "$item" "$dst" || return 1
  log "🛤  레인 $k 시작: 「$(jqv "$dst/chosen.json" .title)」 ($(jqv "$dst/chosen.json" .slug), $(jqv "$dst/slot.json" .unit.id))"
  env -u MGF_SNAPSHOT -u MGF_WORK \
    MGF_LANE_ITEM="$dst" MGF_RUN_ID="$BATCH_ID-L$k" MGF_LANE_NO="$k" \
    SCHOOL=middle REPORT="${REPORT:-1}" DEPLOY="${DEPLOY:-1}" \
    BUILD_RUNNER=claude_run BUILD_MODEL="$CLAUDE_MODEL_SMART" \
    BUILD_FALLBACK_RUNNER=codex_run BUILD_FALLBACK_MODEL="$CODEX_MODEL_SMART" \
    FIX_RUNNER=claude_run FIX_MODEL="$CLAUDE_MODEL_SMART" \
    FIX_FALLBACK_RUNNER=codex_smart_run FIX_FALLBACK_MODEL="$CODEX_MODEL_SMART" \
    JUDGE_FALLBACK_RUNNER=agy_run \
    bash "$ROOT/factory/run.sh" >"$LOG_DIR/lane-$k.out" 2>"$LOG_DIR/lane-$k.log" &
  LANE_PIDS+=("$!:$k")
}

LANE_PIDS=(); DESIGN_PIDS=(); NEXT=0; LANE_NO=0; DONE_OK=0; DONE_FAIL=0
UNIT_ARR=(); while IFS= read -r u; do [ -n "$u" ] && UNIT_ARR+=("$u"); done <<< "$UNITS"

step "시작"
while :; do
  now=$(date +%s)
  # 끝난 레인 거두기
  alive=()
  for e in ${LANE_PIDS[@]+"${LANE_PIDS[@]}"}; do
    p="${e%%:*}"; k="${e##*:}"
    if kill -0 "$p" 2>/dev/null; then alive+=("$e"); else
      wait "$p"; rc=$?
      res="$(grep -m1 -E '새 게임|폐기|실패' "$LOG_DIR/lane-$k.out" 2>/dev/null | head -c 140)"
      if echo "$res" | grep -q '새 게임'; then DONE_OK=$((DONE_OK+1)); else DONE_FAIL=$((DONE_FAIL+1)); fi
      log "레인 $k 종료 rc=$rc — ${res:-결과 없음}"
    fi
  done
  LANE_PIDS=(${alive[@]+"${alive[@]}"})
  dalive=()
  for p in ${DESIGN_PIDS[@]+"${DESIGN_PIDS[@]}"}; do kill -0 "$p" 2>/dev/null && dalive+=("$p") || wait "$p"; done
  DESIGN_PIDS=(${dalive[@]+"${dalive[@]}"})

  queued=$(ls -d "$QUEUE"/*/ 2>/dev/null | grep -v '\.tmp/$' | wc -l | tr -d ' ')
  # 레인 띄우기
  while [ "${#LANE_PIDS[@]}" -lt "$LANES" ] && [ "$now" -lt "$STOP_SPAWN" ]; do
    item="$(ls -d "$QUEUE"/*/ 2>/dev/null | grep -v '\.tmp/$' | sort | head -1)"; item="${item%/}"
    [ -n "$item" ] || break
    LANE_NO=$((LANE_NO+1)); spawn_lane "$item" "$LANE_NO"; sleep 20
    queued=$((queued-1))
  done
  # 기획 미리 만들기 — 레인이 곧 쓸 만큼만
  need=$(( LANES - ${#LANE_PIDS[@]} + LOOKAHEAD - queued - ${#DESIGN_PIDS[@]} ))
  while [ "$need" -gt 0 ] && [ "${#DESIGN_PIDS[@]}" -lt "$DESIGN_PAR" ] && [ "$NEXT" -lt "${#UNIT_ARR[@]}" ] && [ "$now" -lt $((STOP_SPAWN - 1200)) ]; do
    u="${UNIT_ARR[$NEXT]}"; NEXT=$((NEXT+1))
    ( design_unit "$u" "$NEXT" ) &
    DESIGN_PIDS+=($!); need=$((need-1))
    log "기획 시작 [$NEXT/${#UNIT_ARR[@]}] $u"
  done
  # 끝 조건: 띄울 것도, 돌고 있는 것도 없다
  if [ "${#LANE_PIDS[@]}" -eq 0 ] && [ "${#DESIGN_PIDS[@]}" -eq 0 ]; then
    if [ "$queued" -eq 0 ] && { [ "$NEXT" -ge "${#UNIT_ARR[@]}" ] || [ "$now" -ge $((STOP_SPAWN - 1200)) ]; }; then break; fi
    [ "$now" -ge "$STOP_SPAWN" ] && break
  fi
  sleep 15
done
LEFT=$(ls -d "$QUEUE"/*/ 2>/dev/null | wc -l | tr -d ' ')
log "배치 종료 — 게시 ${DONE_OK} · 미게시 ${DONE_FAIL} · 대기열 남음 ${LEFT} · 기획 안 한 단원 $(( ${#UNIT_ARR[@]} - NEXT ))"
echo "🏁 [중학교 배치] 게시 ${DONE_OK} · 미게시 ${DONE_FAIL} (로그 logs/$BATCH_ID)"
