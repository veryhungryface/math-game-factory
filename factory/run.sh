#!/usr/bin/env bash
#
# 초등 수학 게임 1개 생산 사이클.  cron 이 2시간마다 이걸 호출한다.
#
#   bash factory/run.sh              정상 생산
#   DEPLOY=0 REPORT=0 bash factory/run.sh   드라이런 (배포·보고 생략)
#   FOCUS=6-2 bash factory/run.sh    특정 학년-학기만
#
# 표준출력은 에르메스가 그대로 전달하는 보고서다. 로그는 stderr + logs/ 로 간다.

set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT" || exit 1
# shellcheck source=/dev/null
source "$ROOT/factory/config.sh"

RUN_ID="$(date +%Y%m%d-%H%M%S)"
LOG_DIR="$ROOT/logs/$RUN_ID"
WORK="$ROOT/factory/work"
LOCK="$ROOT/factory/state/run.lock"
mkdir -p "$LOG_DIR" "$ROOT/factory/state"

# ── 유틸 ───────────────────────────────────────────────────────────
log()  { printf '[%s] %s\n' "$(date +%H:%M:%S)" "$*" >&2; }
step() { printf '\n\033[1;36m━━━ %s ━━━\033[0m\n' "$*" >&2; }
die()  { log "💀 $*"; finish "실패" "$*"; exit 1; }

# macOS 에는 timeout 이 없다. 백그라운드 + 폴링으로 대체.
run_timeout() {
  local secs="$1"; shift
  "$@" & local pid=$!
  local waited=0
  while kill -0 "$pid" 2>/dev/null; do
    if [ "$waited" -ge "$secs" ]; then
      log "⏱  제한시간 ${secs}s 초과 → 중단 (pid $pid)"
      kill -TERM "$pid" 2>/dev/null; sleep 3; kill -KILL "$pid" 2>/dev/null
      wait "$pid" 2>/dev/null
      return 124
    fi
    sleep 2; waited=$((waited + 2))
  done
  wait "$pid"; return $?
}

# claude 헤드리스 실행. $1=제한시간 $2=로그파일 $3=프롬프트
claude_run() {
  local secs="$1" logfile="$2" prompt="$3"
  run_timeout "$secs" env -u ANTHROPIC_API_KEY claude -p "$prompt" \
    --model "$CLAUDE_MODEL" \
    --dangerously-skip-permissions \
    --add-dir "$ROOT" \
    >"$logfile" 2>&1
}

# codex 헤드리스 실행. $1=제한시간 $2=로그파일 $3=프롬프트
codex_run() {
  local secs="$1" logfile="$2" prompt="$3"
  run_timeout "$secs" env -u ANTHROPIC_API_KEY codex exec "$prompt" \
    --model "$CODEX_MODEL" \
    --sandbox workspace-write \
    --skip-git-repo-check \
    --cd "$ROOT" \
    >"$logfile" 2>&1
}

jqv() { jq -r "$2 // empty" "$1" 2>/dev/null; }

# ── 락 ────────────────────────────────────────────────────────────
if [ -f "$LOCK" ]; then
  LOCK_PID="$(cat "$LOCK" 2>/dev/null)"
  if [ -n "$LOCK_PID" ] && kill -0 "$LOCK_PID" 2>/dev/null; then
    echo "⏭  이전 사이클(pid $LOCK_PID)이 아직 실행 중입니다. 이번 회차는 건너뜁니다."
    exit 0
  fi
  log "고아 락 발견 — 제거"
  rm -f "$LOCK"
fi
echo $$ > "$LOCK"

STARTED_AT="$(date +%s)"
SLUG=""; TITLE=""; SCORE=""; VERDICT=""; DEPLOY_URL=""; STATUS="진행중"

cleanup() { rm -f "$LOCK"; }
trap cleanup EXIT

# ── 최종 보고 ──────────────────────────────────────────────────────
finish() {
  local status="$1" note="${2:-}"
  local elapsed=$(( $(date +%s) - STARTED_AT ))
  local mins=$(( elapsed / 60 ))
  local report_file="$LOG_DIR/report.md"

  {
    if [ "$status" = "게시" ]; then
      echo "🎮 **새 게임이 나왔습니다** — ${TITLE:-?}"
    elif [ "$status" = "폐기" ]; then
      echo "🗑 **게임 폐기** — 품질 게이트 미달"
    elif [ "$status" = "건너뜀" ]; then
      echo "⏭ **이번 회차 건너뜀**"
    else
      echo "💀 **생산 실패**"
    fi
    echo ""
    [ -n "$TITLE" ] && echo "**제목**: $TITLE"
    [ -n "$SLUG"  ] && echo "**슬러그**: \`$SLUG\`"
    if [ -f "$WORK/chosen.json" ]; then
      local g s u tl mech
      g="$(jqv "$WORK/chosen.json" .grade)"; s="$(jqv "$WORK/chosen.json" .semester)"
      u="$(jqv "$WORK/chosen.json" .unit_title)"; tl="$(jqv "$WORK/chosen.json" .tagline)"
      mech="$(jqv "$WORK/chosen.json" .mechanic_origin)"
      [ -n "$tl" ] && echo "**한 줄**: $tl"
      [ -n "$u" ] && echo "**단원**: ${g}학년 ${s}학기 · $u"
      [ -n "$mech" ] && echo "**차용 메커닉**: $mech"
    fi
    if [ -f "$WORK/slot.json" ]; then
      local std
      std="$(jq -r '.unit.standards | join(", ")' "$WORK/slot.json" 2>/dev/null)"
      [ -n "$std" ] && echo "**성취기준**: $std"
    fi
    [ -n "$SCORE" ] && echo "**검수 점수**: ${SCORE}/100 (커트라인 ${GATE_SCORE})"
    if [ -f "$WORK/review.json" ]; then
      echo ""
      echo "**검수 세부**"
      jq -r '.breakdown | to_entries[] | "  - \(.key): \(.value)"' "$WORK/review.json" 2>/dev/null
      local strengths
      strengths="$(jq -r '.strengths[]? | "  - \(.)"' "$WORK/review.json" 2>/dev/null | head -3)"
      [ -n "$strengths" ] && { echo ""; echo "**좋은 점**"; echo "$strengths"; }
      local fixes
      fixes="$(jq -r '.must_fix[]? | "  - [\(.severity)] \(.issue)"' "$WORK/review.json" 2>/dev/null | head -4)"
      [ -n "$fixes" ] && { echo ""; echo "**지적 사항**"; echo "$fixes"; }
    fi
    echo ""
    if [ -n "$DEPLOY_URL" ]; then
      echo "▶ **플레이**: $DEPLOY_URL/g/$SLUG/"
      echo "🏠 **전체 목록**: $DEPLOY_URL"
    fi
    [ -n "$note" ] && { echo ""; echo "> $note"; }
    echo ""
    echo "_소요 ${mins}분 · 로그 \`logs/$RUN_ID\`_"
  } > "$report_file"

  cat "$report_file"

  cp "$report_file" "$ROOT/logs/latest-report.md" 2>/dev/null

  if [ "$REPORT" = "1" ]; then
    if hermes send --to "$REPORT_TARGET" --file "$report_file" --quiet 2>>"$LOG_DIR/report.log"; then
      log "📨 에르메스 보고 전송 완료 → $REPORT_TARGET"
      if [ "$status" = "게시" ] && [ -f "$ROOT/public/g/$SLUG/thumb.png" ]; then
        hermes send --to "$REPORT_TARGET" "MEDIA:$ROOT/public/g/$SLUG/thumb.png" --quiet 2>/dev/null || true
      fi
    else
      # 전송 실패해도 결과를 잃지 않는다: 로컬 파일 + macOS 알림
      log "⚠️  에르메스 보고 실패 ($(tail -1 "$LOG_DIR/report.log" 2>/dev/null)) — logs/latest-report.md 에 보관"
      osascript -e "display notification \"${TITLE:-생산 결과} — $status\" with title \"수학 게임 공장\"" 2>/dev/null || true
    fi
  fi
}

# ════════════════════════════════════════════════════════════════
step "0. 준비"
# RESUME=1 이면 이미 만들어진 기획(chosen.json)을 재사용하고 기획·심사를 건너뛴다.
# 아트/구현 단계에서 죽었을 때 8분짜리 기획을 다시 돌리지 않기 위한 것.
RESUMED=0
if [ "${RESUME:-0}" = "1" ] && [ -f "$WORK/chosen.json" ] && [ -f "$WORK/slot.json" ]; then
  RESUMED=1
  log "RESUME=1 — 기존 기획을 재사용합니다 ($(jqv "$WORK/chosen.json" .title))"
else
  rm -rf "$WORK"; mkdir -p "$WORK"
fi
[ -f "$ROOT/curriculum/2022-elementary-math.json" ] || die "교육과정 파일이 없습니다"
[ -d "$ROOT/node_modules/puppeteer" ] || { log "puppeteer 설치 중…"; npm install --silent >/dev/null 2>&1; }

# ════════════════════════════════════════════════════════════════
step "1. 슬롯 선택"
if [ "$RESUMED" = "0" ]; then
  node factory/lib/pick-slot.mjs --focus "$FOCUS" --write > "$LOG_DIR/slot.log" 2>&1 \
    || die "슬롯 선택 실패 — $(tail -3 "$LOG_DIR/slot.log")"
fi
cp "$WORK/slot.json" "$LOG_DIR/slot.json"
UNIT_TITLE="$(jqv "$WORK/slot.json" .unit.title)"
UNIT_GRADE="$(jqv "$WORK/slot.json" .unit.grade)"
UNIT_SEM="$(jqv "$WORK/slot.json" .unit.semester)"
log "슬롯: ${UNIT_GRADE}학년 ${UNIT_SEM}학기 · $UNIT_TITLE"

SLOT_CTX="$(cat "$WORK/slot.json")"

# ════════════════════════════════════════════════════════════════
if [ "$RESUMED" = "1" ]; then
  log "기획·심사 단계 건너뜀 (RESUME)"
else
step "2. 기획 (병렬 ${DESIGN_VARIANTS}개)"
DESIGN_PIDS=()
for i in $(seq 1 "$DESIGN_VARIANTS"); do
  PROMPT="$(cat factory/prompts/10-design.md)

---
## 이번 슬롯

\`\`\`json
$SLOT_CTX
\`\`\`

## 참고
- 레퍼런스 광산: \`references/game-references.json\` 과 \`references/game-references.md\` 를 읽어서 아이디어를 가져와라.
- 위 \`mechanic_pool\` 은 추천일 뿐이다. 더 좋은 게 있으면 references 에서 직접 골라도 된다. \`avoid_mechanics\` 는 피해라.
- 너는 **${i}번 기획자**다. 다른 기획자 2명이 동시에 다른 안을 만들고 있다. 남들이 안 할 법한 각도로 가라.
  - 1번: 가장 정통적이고 안전한 아케이드
  - 2번: 요즘 초등학생이 실제로 하는 모바일 게임 문법 (머지·로그라이크·오토배틀·리듬·.io)
  - 3번: 비주얼로 승부하는 안 (3D·셰이더·물리). 단, 단원 내용과 억지로 붙이지 마라

## 출력 파일
\`factory/work/concept-${i}.json\` 로 저장해라. \`n\` 필드는 ${i} 다."

  claude_run "$T_DESIGN" "$LOG_DIR/design-$i.log" "$PROMPT" &
  DESIGN_PIDS+=($!)
done
for pid in ${DESIGN_PIDS[@]+"${DESIGN_PIDS[@]}"}; do wait "$pid"; done

CONCEPTS=$(ls "$WORK"/concept-*.json 2>/dev/null | wc -l | tr -d ' ')
log "기획안 ${CONCEPTS}개 생성"
[ "$CONCEPTS" -ge 1 ] || die "기획안이 하나도 나오지 않았습니다 — $(tail -5 "$LOG_DIR/design-1.log")"

# ════════════════════════════════════════════════════════════════
step "3. 컨셉 심사"
if [ "$CONCEPTS" -eq 1 ]; then
  cp "$(ls "$WORK"/concept-*.json | head -1)" "$WORK/chosen.json"
  log "기획안이 1개뿐 — 심사 생략"
else
  claude_run "$T_JUDGE" "$LOG_DIR/judge.log" "$(cat factory/prompts/15-judge.md)

---
## 이번 슬롯
\`\`\`json
$SLOT_CTX
\`\`\`"
  [ -f "$WORK/chosen.json" ] || {
    log "⚠️  심사 실패 — 1번 기획안으로 진행"
    cp "$(ls "$WORK"/concept-*.json | head -1)" "$WORK/chosen.json"
  }
fi
fi

SLUG="$(jqv "$WORK/chosen.json" .slug)"
TITLE="$(jqv "$WORK/chosen.json" .title)"
[ -n "$SLUG" ] || die "선택된 컨셉에 slug 가 없습니다"

# 슬롯 정보를 chosen.json 에 병합 (빌드·보고 단계가 쓴다)
jq --argjson slot "$SLOT_CTX" \
   '. + {grade: $slot.unit.grade, semester: $slot.unit.semester,
         unit_id: $slot.unit.id, unit_title: $slot.unit.title,
         standards: (.standards // $slot.unit.standards),
         unit_context: $slot.unit}' \
   "$WORK/chosen.json" > "$WORK/chosen.tmp" && mv "$WORK/chosen.tmp" "$WORK/chosen.json"
cp "$WORK/chosen.json" "$LOG_DIR/chosen.json"
log "선택: 「${TITLE}」 ($SLUG)"

mkdir -p "$ROOT/public/g/$SLUG/assets"

# ════════════════════════════════════════════════════════════════
step "4. 아트 생성 (병렬)"
ASSET_IDS="$(jq -r '.art_direction.assets_needed[]?.id' "$WORK/chosen.json" 2>/dev/null)"
if [ -z "$ASSET_IDS" ]; then
  log "생성할 에셋이 지정되지 않음 — 썸네일만 만든다"
  ASSET_IDS="thumb"
fi

ART_PIDS=()
for aid in $ASSET_IDS; do
  ASSET_JSON="$(jq -c --arg id "$aid" '.art_direction.assets_needed[]? | select(.id==$id)' "$WORK/chosen.json")"
  [ -n "$ASSET_JSON" ] || ASSET_JSON="{\"id\":\"$aid\",\"prompt\":\"$(jqv "$WORK/chosen.json" .one_liner) key art\",\"size\":\"1536x1024\"}"

  ART_PROMPT="$(cat factory/prompts/20-art.md)

---
## 네가 만들 에셋은 **딱 1개**다 (다른 에이전트가 나머지를 동시에 만들고 있다)

\`\`\`json
$ASSET_JSON
\`\`\`

- slug: \`$SLUG\`
- 게임: 「${TITLE}」 — $(jqv "$WORK/chosen.json" .one_liner)
- 아트 방향: $(jq -c '.art_direction | {mood, palette}' "$WORK/chosen.json")

저장 경로: $([ "$aid" = "thumb" ] && echo "\`public/g/$SLUG/thumb.png\` (정확히 1200×630)" || echo "\`public/g/$SLUG/assets/${aid}.png\`")

작업이 끝나면 \`factory/work/art-${aid}.json\` 에 \`{\"id\":\"$aid\",\"path\":\"...\",\"w\":0,\"h\":0,\"kb\":0,\"ok\":true,\"note\":\"\"}\` 를 써라.
다른 에이전트와 충돌하니 \`factory/work/art.json\` 은 건드리지 마라."

  codex_run "$T_ART" "$LOG_DIR/art-$aid.log" "$ART_PROMPT" &
  ART_PIDS+=($!)
done
for pid in ${ART_PIDS[@]+"${ART_PIDS[@]}"}; do wait "$pid"; done

# 개별 결과를 art.json 으로 합친다
jq -s --arg slug "$SLUG" \
  '{slug:$slug, generated:[.[]|select(.ok!=false)], failed:[.[]|select(.ok==false)|.id]}' \
  "$WORK"/art-*.json 2>/dev/null > "$WORK/art.json" || echo "{\"slug\":\"$SLUG\",\"generated\":[],\"failed\":[]}" > "$WORK/art.json"

ART_OK=$(find "$ROOT/public/g/$SLUG" -name '*.png' 2>/dev/null | wc -l | tr -d ' ')
log "이미지 ${ART_OK}장 생성됨"

# ════════════════════════════════════════════════════════════════
step "5. 게임 구현"
BUILD_PROMPT="$(cat factory/prompts/30-build.md)

---
## 기획서
\`\`\`json
$(cat "$WORK/chosen.json")
\`\`\`

## 실제로 존재하는 에셋
\`\`\`
$(find "$ROOT/public/g/$SLUG" -type f \( -name '*.png' -o -name '*.jpg' -o -name '*.webp' \) 2>/dev/null | sed "s|$ROOT/public/g/$SLUG/|./|")
\`\`\`
이 목록에 **없는 파일은 절대 참조하지 마라.** 404 하나면 QA 자동 탈락이다.
이미지 로딩은 \`img.onerror\` 로 폴백(코드 드로잉)을 반드시 붙여라.

## 단원 정보
\`\`\`json
$(jq -c '.unit' "$WORK/slot.json")
\`\`\`

## 만들 위치
- \`public/g/$SLUG/index.html\`
- \`public/g/$SLUG/meta.json\`  (slug=\"$SLUG\", grade=$UNIT_GRADE, semester=$UNIT_SEM, unit.id=\"$(jqv "$WORK/slot.json" .unit.id)\")

## 끝내기 전에 반드시
\`node factory/lib/qa.mjs $SLUG\` 를 돌려서 **치명적 결함 0건**을 확인해라. 실패하면 고치고 다시 돌려라."

claude_run "$T_BUILD" "$LOG_DIR/build.log" "$BUILD_PROMPT"
[ -f "$ROOT/public/g/$SLUG/index.html" ] || die "게임 파일이 생성되지 않았습니다 — $(tail -5 "$LOG_DIR/build.log")"

# ════════════════════════════════════════════════════════════════
step "6. 자동 QA"
node factory/lib/qa.mjs "$SLUG" > "$LOG_DIR/qa-1.log" 2>&1
QA1=$?
cp -r "$WORK/qa/$SLUG" "$LOG_DIR/qa-1" 2>/dev/null
log "자동 QA 종료코드 $QA1"
tail -25 "$LOG_DIR/qa-1.log" >&2

# ════════════════════════════════════════════════════════════════
step "7. 검수"
REVIEW_PROMPT="$(cat factory/prompts/40-review.md)

---
- slug: \`$SLUG\`
- 커트라인: ${GATE_SCORE}점
- 자동 QA 리포트: \`factory/work/qa/$SLUG/report.json\`
- 스크린샷: \`factory/work/qa/$SLUG/mobile.png\`, \`tablet.png\`, \`desktop.png\` — **Read 툴로 실제로 봐라**"

claude_run "$T_REVIEW" "$LOG_DIR/review-1.log" "$REVIEW_PROMPT"
cp "$WORK/review.json" "$LOG_DIR/review-1.json" 2>/dev/null

SCORE="$(jqv "$WORK/review.json" .score)"
PASSED="$(jqv "$WORK/review.json" .passed)"
REJECT="$(jqv "$WORK/review.json" .reject_immediately)"
log "1차 검수: ${SCORE:-?}점 / passed=$PASSED / reject=$REJECT"

# ════════════════════════════════════════════════════════════════
if [ "$PASSED" != "true" ] || [ "$QA1" -ne 0 ]; then
  step "8. 수정 (마지막 기회)"
  claude_run "$T_FIX" "$LOG_DIR/fix.log" "$(cat factory/prompts/45-fix.md)

---
- slug: \`$SLUG\`
- 검수 결과: \`factory/work/review.json\`
- 자동 QA: \`factory/work/qa/$SLUG/report.json\`"

  node factory/lib/qa.mjs "$SLUG" > "$LOG_DIR/qa-2.log" 2>&1
  QA1=$?
  cp -r "$WORK/qa/$SLUG" "$LOG_DIR/qa-2" 2>/dev/null
  log "재 QA 종료코드 $QA1"

  step "9. 재검수"
  rm -f "$WORK/review.json"
  claude_run "$T_REVIEW" "$LOG_DIR/review-2.log" "$REVIEW_PROMPT

이것은 **재검수**다. \`factory/work/fix.json\` 에 수정 내역이 있다. 수정이 실제로 반영됐는지 확인해라.
봐주지 마라 — 여전히 미달이면 폐기가 맞다."
  cp "$WORK/review.json" "$LOG_DIR/review-2.json" 2>/dev/null

  SCORE="$(jqv "$WORK/review.json" .score)"
  PASSED="$(jqv "$WORK/review.json" .passed)"
  REJECT="$(jqv "$WORK/review.json" .reject_immediately)"
  log "2차 검수: ${SCORE:-?}점 / passed=$PASSED"
fi

VERDICT="$(jqv "$WORK/review.json" .verdict)"

# ════════════════════════════════════════════════════════════════
if [ "$PASSED" != "true" ] || [ "$REJECT" = "true" ] || [ "$QA1" -ne 0 ]; then
  step "폐기"
  ARCHIVE="$ROOT/factory/state/rejected/$RUN_ID-$SLUG"
  mkdir -p "$(dirname "$ARCHIVE")"
  mv "$ROOT/public/g/$SLUG" "$ARCHIVE" 2>/dev/null
  node -e '
    const fs=require("fs"),p=process.argv[1];
    const q=fs.existsSync(p)?JSON.parse(fs.readFileSync(p,"utf8")):{produced:[],failed:[],mechanic_history:[]};
    q.failed=q.failed||[];
    q.failed.push({run:process.argv[2],slug:process.argv[3],title:process.argv[4],score:Number(process.argv[5])||0,unit:process.argv[6],at:new Date().toISOString()});
    fs.writeFileSync(p,JSON.stringify(q,null,2)+"\n");
  ' "$ROOT/factory/state/queue.json" "$RUN_ID" "$SLUG" "$TITLE" "${SCORE:-0}" "$(jqv "$WORK/slot.json" .unit.id)"
  finish "폐기" "${VERDICT:-품질 게이트 미달}"
  exit 0
fi

# ════════════════════════════════════════════════════════════════
step "10. 게시 준비"
node -e '
  const fs=require("fs"),f=process.argv[1];
  const m=JSON.parse(fs.readFileSync(f,"utf8"));
  const r=JSON.parse(fs.readFileSync(process.argv[2],"utf8"));
  m.qa={score:r.score,gate:Number(process.argv[3]),passed:true,reviewed_at:new Date().toISOString(),notes:(r.strengths||[]).slice(0,3)};
  fs.writeFileSync(f,JSON.stringify(m,null,2)+"\n");
' "$ROOT/public/g/$SLUG/meta.json" "$WORK/review.json" "$GATE_SCORE"

node factory/lib/build-index.mjs >&2 || die "허브 빌드 실패"

node -e '
  const fs=require("fs"),p=process.argv[1];
  const q=fs.existsSync(p)?JSON.parse(fs.readFileSync(p,"utf8")):{produced:[],failed:[],mechanic_history:[]};
  q.produced=q.produced||[]; q.mechanic_history=q.mechanic_history||[];
  q.produced.push({run:process.argv[2],slug:process.argv[3],title:process.argv[4],score:Number(process.argv[5])||0,unit:process.argv[6],mechanic:process.argv[7],at:new Date().toISOString()});
  q.mechanic_history.push(process.argv[7]);
  fs.writeFileSync(p,JSON.stringify(q,null,2)+"\n");
' "$ROOT/factory/state/queue.json" "$RUN_ID" "$SLUG" "$TITLE" "${SCORE:-0}" \
  "$(jqv "$WORK/slot.json" .unit.id)" "$(jqv "$WORK/chosen.json" .mechanic)"

# ════════════════════════════════════════════════════════════════
if [ "$DEPLOY" = "1" ]; then
  step "11. GitHub + Vercel 배포"
  git add -A >/dev/null 2>&1
  git -c user.name="math-game-factory" -c user.email="bot@localhost" \
      commit -q -m "게임 추가: $TITLE ($SLUG)

단원: ${UNIT_GRADE}학년 ${UNIT_SEM}학기 · $UNIT_TITLE
성취기준: $(jq -r '.unit.standards | join(", ")' "$WORK/slot.json" 2>/dev/null)
검수: ${SCORE}/100
메커닉: $(jqv "$WORK/chosen.json" .mechanic) ($(jqv "$WORK/chosen.json" .mechanic_origin))" >/dev/null 2>&1 \
    && log "커밋 완료" || log "⚠️  커밋할 변경 없음"

  # 다른 곳(사람/다른 사이클)에서 먼저 푸시했을 수 있으니 리베이스 후 푸시한다.
  git -c user.name="math-game-factory" -c user.email="bot@localhost" \
      pull --rebase --autostash -q origin main >>"$LOG_DIR/push.log" 2>&1 || log "⚠️  리베이스 실패 — 그대로 푸시 시도"
  git push -q origin main >>"$LOG_DIR/push.log" 2>&1 && log "GitHub 푸시 완료" || log "⚠️  푸시 실패 — $(tail -2 "$LOG_DIR/push.log")"

  vercel deploy --prod --yes >"$LOG_DIR/vercel.log" 2>&1
  ALIAS="https://${VERCEL_PROJECT}.vercel.app"
  # 배포 반영까지 최대 60초 폴링 — 실제로 게임 URL 이 열려야 성공으로 친다.
  DEPLOY_URL=""
  for _ in $(seq 1 12); do
    if curl -sf -o /dev/null "$ALIAS/g/$SLUG/"; then DEPLOY_URL="$ALIAS"; break; fi
    sleep 5
  done
  if [ -n "$DEPLOY_URL" ]; then
    log "배포 완료: $DEPLOY_URL/g/$SLUG/"
  else
    log "⚠️  배포 확인 실패 — $(grep -Eo 'https://[^ ]*vercel\.app' "$LOG_DIR/vercel.log" | tail -1) / $(tail -3 "$LOG_DIR/vercel.log")"
  fi
else
  log "DEPLOY=0 — 배포 생략"
  DEPLOY_URL="http://localhost (드라이런)"
fi

finish "게시"
