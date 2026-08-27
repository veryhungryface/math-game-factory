#!/usr/bin/env bash
#
# 초등 수학 게임 1개 생산 사이클.  cron 이 2시간마다 이걸 호출한다.
#
#   bash factory/run.sh              정상 생산
#   DEPLOY=0 REPORT=0 bash factory/run.sh   드라이런 (배포·보고 생략)
#   FOCUS=6-2 bash factory/run.sh    특정 학년-학기만
#
#   RESUME_FROM=<stage> bash factory/run.sh   중간 단계부터 재개
#     stage: design(기본) | art | build | qa | review
#     factory/work/ 의 산출물을 그대로 재사용한다. 사이클이 중간에 죽었을 때
#     50분짜리 구현을 다시 돌리지 않기 위한 것.
#     예) 구현까지 끝났는데 검수에서 죽었다 → RESUME_FROM=qa
#
# 표준출력은 에르메스가 그대로 전달하는 보고서다. 로그는 stderr + logs/ 로 간다.

set -uo pipefail

# 스냅샷으로 재실행될 때는 스크립트가 /tmp 에 있으므로 위치로 ROOT 를 유추하면 안 된다.
# 그래서 최초 실행이 알아낸 ROOT 를 MGF_ROOT 로 물려준다.
ROOT="${MGF_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
cd "$ROOT" || exit 1

# bash 는 스크립트를 바이트 오프셋으로 읽어가며 실행한다. 사이클이 1시간 넘게 도는 동안
# 누군가 run.sh 를 편집하면 실행 중인 셸이 엉뚱한 위치로 점프해 조용히 망가진다.
# 그래서 항상 스냅샷 사본으로 갈아탄 뒤 진행한다.
if [ -z "${MGF_SNAPSHOT:-}" ]; then
  SNAP="${TMPDIR:-/tmp}/mgf-run-$$.sh"
  cp "$ROOT/factory/run.sh" "$SNAP" || exit 1
  MGF_SNAPSHOT="$SNAP" MGF_ROOT="$ROOT" bash "$SNAP" "$@"
  RC=$?
  rm -f "$SNAP"
  exit $RC
fi
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

# claude 헤드리스 실행. $1=제한시간 $2=로그파일 $3=프롬프트 [$4=모델(기본 $CLAUDE_MODEL)]
claude_run() {
  local secs="$1" logfile="$2" prompt="$3" model="${4:-$CLAUDE_MODEL}"
  run_timeout "$secs" env -u ANTHROPIC_API_KEY claude -p "$prompt" \
    --model "$model" \
    --dangerously-skip-permissions \
    --add-dir "$ROOT" \
    >"$logfile" 2>&1
}

# codex 헤드리스 실행. $1=제한시간 $2=로그파일 $3=프롬프트 [$4=모델(기본 $CODEX_MODEL)]
codex_run() {
  local secs="$1" logfile="$2" prompt="$3" model="${4:-$CODEX_MODEL}"
  run_timeout "$secs" env -u ANTHROPIC_API_KEY codex exec "$prompt" \
    --model "$model" \
    --sandbox workspace-write \
    --skip-git-repo-check \
    --cd "$ROOT" \
    >"$logfile" 2>&1
}

# grok 헤드리스 실행. $1=제한시간 $2=로그파일 $3=프롬프트
grok_run() {
  local secs="$1" logfile="$2" prompt="$3"
  run_timeout "$secs" grok -p "$prompt" \
    --model "$GROK_MODEL" \
    --always-approve \
    --output-format plain \
    --cwd "$ROOT" \
    >"$logfile" 2>&1
}

# codex 를 상위 티어(sol)로 돌리는 단축 러너 — 기획 2번·검수처럼 판단이 어려운 단계용.
codex_smart_run() {
  codex_run "$1" "$2" "$3" "${4:-$CODEX_MODEL_SMART}"
}

# 원하는 러너가 실제로 쓸 수 있는지 확인하고, 없으면 codex → claude 순으로 폴백한다.
# (grok 잔액 소진 402, claude 세션 한도처럼 러너 하나가 죽어도 사이클이 멈추면 안 된다)
resolve_runner() {
  local want="$1"
  case "$want" in
    grok_run)                  command -v grok   >/dev/null 2>&1 && { echo grok_run;   return; } ;;
    codex_run|codex_smart_run) command -v codex  >/dev/null 2>&1 && { echo "$want";    return; } ;;
    claude_run)                command -v claude >/dev/null 2>&1 && { echo claude_run; return; } ;;
  esac
  command -v codex  >/dev/null 2>&1 && { echo codex_smart_run; return; }
  command -v grok   >/dev/null 2>&1 && { echo grok_run;        return; }
  echo claude_run
}

# 단계 실행. $1=원하는러너 $2=모델(비워도 됨) $3=제한시간 $4=로그 $5=프롬프트 [$6=& 로 띄울지]
# 폴백이 일어나면 **모델 인자를 버린다** — 다른 회사 모델 이름을 그대로 넘겨서 400 을 맞은
# 사고가 실제로 있었다(2026-08-25, run.sh 러너 모델 인자 400).
stage_run() {
  local want="$1" model="$2" secs="$3" logfile="$4" prompt="$5"
  local got; got="$(resolve_runner "$want")"
  if [ "$got" != "$want" ]; then
    log "⚠️  러너 폴백: $want → $got (모델 인자는 러너 기본값으로 되돌린다)"
    model=""
  fi
  "$got" "$secs" "$logfile" "$prompt" "$model"
}

jqv() { jq -r "$2 // empty" "$1" 2>/dev/null; }

# queue.json failed 장부 기입. $1=사유(비우면 생략).
# 원래 폐기 단계에만 인라인으로 있어서 **빌드 단계에서 죽은 회차는 장부에 안 남았다**
# (2026-08-27~28 grok 402 4연속 사망이 전부 미기록). 빌드 실패 경로도 이걸 거친다.
record_failed() {
  local reason="${1:-}"
  node -e '
    const fs=require("fs"),p=process.argv[1];
    const q=fs.existsSync(p)?JSON.parse(fs.readFileSync(p,"utf8")):{produced:[],failed:[],mechanic_history:[]};
    q.failed=q.failed||[];
    const e={run:process.argv[2],slug:process.argv[3],title:process.argv[4],score:Number(process.argv[5])||0,unit:process.argv[6],at:new Date().toISOString()};
    if(process.argv[7]) e.reason=process.argv[7];
    q.failed.push(e);
    fs.writeFileSync(p,JSON.stringify(q,null,2)+"\n");
  ' "$ROOT/factory/state/queue.json" "$RUN_ID" "${SLUG:-}" "${TITLE:-}" "${SCORE:-0}" "$(jqv "$WORK/slot.json" .unit.id)" "$reason"
}

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

# ── 러너 사전 점검 ────────────────────────────────────────────────
# 어떤 러너로 이번 회차를 돌리는지 로그 첫머리에 남긴다. grok 잔액 소진(402)이나
# claude 세션 한도처럼 러너 하나가 조용히 죽으면 원인을 찾느라 로그를 뒤져야 했다.
for _c in grok codex claude; do
  command -v "$_c" >/dev/null 2>&1 && _AVAIL="${_AVAIL:-}$_c " || true
done
log "러너 가용: ${_AVAIL:-없음}| 빌드=$(resolve_runner "${BUILD_RUNNER:-grok_run}") 검수=$(resolve_runner "${REVIEW_RUNNER:-codex_run}") 수정=$(resolve_runner "${FIX_RUNNER:-grok_run}")"

STARTED_AT="$(date +%s)"
SLUG=""; TITLE=""; SCORE=""; VERDICT=""; DEPLOY_URL=""; STATUS="진행중"
BUILD_FALLBACK_NOTE=""   # 빌드 러너 폴백이 발동하면 채워진다 — 리포트에 그대로 남긴다

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
    [ -n "${BUILD_FALLBACK_NOTE:-}" ] && { echo ""; echo "⚙️ **빌드 폴백**: $BUILD_FALLBACK_NOTE"; }
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
        IMG="$ROOT/public/g/$SLUG/square.png"
        [ -f "$IMG" ] || IMG="$ROOT/public/g/$SLUG/thumb.png"
        hermes send --to "$REPORT_TARGET" "MEDIA:$IMG" --quiet 2>/dev/null || true
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
# 단계 재개. 각 단계에 번호를 주고, 시작 번호보다 앞선 단계는 건너뛴다.
stage_no() {
  case "$1" in
    design) echo 2 ;; art) echo 4 ;; build) echo 5 ;;
    qa)     echo 6 ;; review) echo 7 ;;
    *)      echo 2 ;;
  esac
}
RESUME_FROM="${RESUME_FROM:-}"
# 예전 RESUME=1 은 art 부터 재개하는 것과 같다 (하위 호환)
[ -z "$RESUME_FROM" ] && [ "${RESUME:-0}" = "1" ] && RESUME_FROM="art"
START_AT=$(stage_no "${RESUME_FROM:-design}")

if [ "$START_AT" -gt 2 ]; then
  if [ ! -f "$WORK/chosen.json" ] || [ ! -f "$WORK/slot.json" ]; then
    log "⚠️  재개할 산출물이 없습니다 (factory/work/chosen.json) — 처음부터 시작합니다"
    START_AT=2; RESUME_FROM=""
    rm -rf "$WORK"; mkdir -p "$WORK"
  else
    log "RESUME_FROM=$RESUME_FROM — 「$(jqv "$WORK/chosen.json" .title)」 기존 산출물을 재사용합니다"
  fi
else
  rm -rf "$WORK"; mkdir -p "$WORK"
fi
RESUMED=$([ "$START_AT" -gt 2 ] && echo 1 || echo 0)
[ -f "$ROOT/curriculum/2022-elementary-math.json" ] \
  || die "교육과정 파일이 없습니다: $ROOT/curriculum/2022-elementary-math.json (ROOT=$ROOT 가 저장소를 가리키는지 확인해라)"
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

# ── 사용자 피드백 인박스 ────────────────────────────────────────
# factory/state/feedback.md 에 뭔가 적혀 있으면 이번 사이클의 기획·검수에 끼워 넣고
# 아카이브로 옮긴다. 매 사이클 반복 주입되지 않게 읽는 즉시 비운다.
FEEDBACK_FILE="$ROOT/factory/state/feedback.md"
USER_FEEDBACK=""
if [ "$RESUMED" = "0" ] && [ -f "$FEEDBACK_FILE" ]; then
  FB_BODY="$(grep -v '^<!--\|^사용자 피드백\|^여기에 적은\|^프롬프트에 그대로\|^쓰는 법\|^factory/state/feedback-archive\|^되게 하기\|^쓰면 된다\|^("젤리\|^-->' "$FEEDBACK_FILE" 2>/dev/null | sed '/^[[:space:]]*$/d')"
  if [ -n "$FB_BODY" ]; then
    USER_FEEDBACK="## 사용자 피드백 (직접 반영해라 — 무시하지 마라)

$FB_BODY"
    mkdir -p "$ROOT/factory/state/feedback-archive"
    cp "$FEEDBACK_FILE" "$ROOT/factory/state/feedback-archive/$RUN_ID.md"
    # 안내 주석만 남기고 본문은 비운다
    head -8 "$FEEDBACK_FILE" > "$FEEDBACK_FILE.new" && mv "$FEEDBACK_FILE.new" "$FEEDBACK_FILE"
    log "사용자 피드백 반영 — factory/state/feedback-archive/$RUN_ID.md 로 보관"
  fi
fi

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

${USER_FEEDBACK:+
$USER_FEEDBACK
}
## 참고
- 레퍼런스 광산: \`references/game-references.json\` 과 \`references/game-references.md\` 를 읽어서 아이디어를 가져와라.
- 위 \`mechanic_pool\` 은 추천일 뿐이다. 더 좋은 게 있으면 references 에서 직접 골라도 된다. \`avoid_mechanics\` 는 피해라.
- 너는 **${i}번 기획자**다. 다른 기획자 2명이 동시에 다른 안을 만들고 있다. 남들이 안 할 법한 각도로 가라.
  - 1번: 가장 정통적이고 안전한 아케이드
  - 2번: 요즘 초등학생이 실제로 하는 모바일 게임 문법 (머지·로그라이크·오토배틀·리듬·.io)
  - 3번: 비주얼로 승부하는 안 (3D·셰이더·물리). 단, 단원 내용과 억지로 붙이지 마라

## 출력 파일
\`factory/work/concept-${i}.json\` 로 저장해라. \`n\` 필드는 ${i} 다."

  # 기획 3안을 서로 다른 회사·티어 모델로 나눠 돌린다 — 전부 같은 모델로만 기획하니
  # 게임들이 서로 비슷해 보인다는 지적을 받았다. 배정은 config.sh 의 DESIGN_RUNNERS
  # (쉼표 구분, i 번째가 i 번 기획자). 기본값은 클로드를 쓰지 않는다.
  IFS=',' read -ra _DR <<< "${DESIGN_RUNNERS:-grok_run,codex_smart_run,codex_run}"
  DESIGN_R="$(resolve_runner "${_DR[$((i-1))]:-codex_run}")"
  "$DESIGN_R" "$T_DESIGN" "$LOG_DIR/design-$i.log" "$PROMPT" &
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
  # 심사는 GPT 상위 티어로 — 사용자 요청(claude 편중 완화). 기획안이 claude/codex/grok
  # 3사에서 나오므로 어차피 어느 모델이 심사해도 자기 안이 하나는 섞여 있다.
  codex_run "$T_JUDGE" "$LOG_DIR/judge.log" "$(cat factory/prompts/15-judge.md)

---
## 이번 슬롯
\`\`\`json
$SLOT_CTX
\`\`\`" "$CODEX_MODEL_SMART"
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
if [ "$START_AT" -gt 4 ]; then
  log "아트 생성 건너뜀 (RESUME) — 기존 이미지 $(find "$ROOT/public/g/$SLUG" -name '*.png' 2>/dev/null | wc -l | tr -d ' ')장"
else
step "4. 아트 생성 (병렬)"
ASSET_IDS="$(jq -r '.art_direction.assets_needed[]?.id' "$WORK/chosen.json" 2>/dev/null)"
if [ -z "$ASSET_IDS" ]; then
  log "생성할 에셋이 지정되지 않음 — 썸네일만 만든다"
  ASSET_IDS="thumb"
fi
# thumb(가로 카드)·square(정사각 공유용)·title(타이틀 화면 키 아트)은 기획서가 뭘 넣었든 항상 만든다.
for req in thumb square title; do
  echo "$ASSET_IDS" | grep -qx "$req" || ASSET_IDS="$ASSET_IDS
$req"
done

ART_PIDS=()
for aid in $ASSET_IDS; do
  ASSET_JSON="$(jq -c --arg id "$aid" '.art_direction.assets_needed[]? | select(.id==$id)' "$WORK/chosen.json")"
  if [ -z "$ASSET_JSON" ] && [ "$aid" = "square" ]; then
    ASSET_JSON="{\"id\":\"square\",\"prompt\":\"$(jqv "$WORK/chosen.json" .one_liner) — 주인공을 중앙에 크게 클로즈업한 정사각 커버 아트\",\"size\":\"1024x1024\",\"transparent_bg\":false}"
  fi
  if [ -z "$ASSET_JSON" ] && [ "$aid" = "title" ]; then
    ASSET_JSON="{\"id\":\"title\",\"prompt\":\"$(jqv "$WORK/chosen.json" .one_liner) — 주인공이 역동적 포즈로 등장하는 세로형 타이틀 키 아트, 상단 1/3은 로고 여백\",\"size\":\"1024x1536\",\"transparent_bg\":false}"
  fi
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

저장 경로: $([ "$aid" = "thumb" ] && echo "\`public/g/$SLUG/thumb.png\` (정확히 1200×630, 가로형)" || ([ "$aid" = "square" ] && echo "\`public/g/$SLUG/square.png\` (정확히 1080×1080, 정사각 — thumb.png 크롭 재사용 금지, 새로 생성)" || ([ "$aid" = "title" ] && echo "\`public/g/$SLUG/assets/title.png\` (세로형 1024×1536, 타이틀 화면 키 아트 — 상단 1/3 로고 여백)" || echo "\`public/g/$SLUG/assets/${aid}.png\`")))

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
fi

# ════════════════════════════════════════════════════════════════
if [ "$START_AT" -gt 5 ]; then
  log "게임 구현 건너뜀 (RESUME) — 기존 index.html 재사용"
  [ -f "$ROOT/public/g/$SLUG/index.html" ] || die "재개하려는데 게임 파일이 없습니다: public/g/$SLUG/index.html"
else
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

stage_run "${BUILD_RUNNER:-grok_run}" "${BUILD_MODEL:-}" "$T_BUILD" "$LOG_DIR/build.log" "$BUILD_PROMPT"
BUILD_RC=$?

# 러너 바이너리는 살아 있는데 API 쪽이 죽는 경우(대표: grok 402 잔액 소진)는
# resolve_runner 가 못 잡는다 — 2026-08-27~28 에 4회 연속으로 회차가 통째로 죽었다.
# 그래서 빌드 단계는 실행 **결과**를 보고 한 번 더 폴백한다: 비정상 종료·402류 출력·
# index.html 미생성이면 codex 상위 티어(sol)로 1회 재시도. 빌드는 어려운 단계라 sol 을 쓴다.
BUILD_ERR="$(grep -m1 -Eo 'status 402|402 Payment Required|Payment Required|usage balance exhausted' "$LOG_DIR/build.log" 2>/dev/null | head -1)"
BUILD_PRIMARY="$(resolve_runner "${BUILD_RUNNER:-grok_run}")"
if { [ "$BUILD_RC" -ne 0 ] || [ -n "$BUILD_ERR" ] || [ ! -f "$ROOT/public/g/$SLUG/index.html" ]; } \
   && [ "$BUILD_PRIMARY" != "codex_run" ] && [ "$BUILD_PRIMARY" != "codex_smart_run" ] \
   && command -v codex >/dev/null 2>&1; then
  if [ -n "$BUILD_ERR" ]; then
    BUILD_FALLBACK_NOTE="grok 402 → codex 폴백 ($BUILD_ERR, 모델 $CODEX_MODEL_SMART)"
  else
    BUILD_FALLBACK_NOTE="빌드 러너($BUILD_PRIMARY) 실패(rc=$BUILD_RC) → codex 폴백 (모델 $CODEX_MODEL_SMART)"
  fi
  log "⚠️  $BUILD_FALLBACK_NOTE — 재시도 로그 build-fallback.log"
  codex_run "$T_BUILD" "$LOG_DIR/build-fallback.log" "$BUILD_PROMPT" "$CODEX_MODEL_SMART"
fi

if [ ! -f "$ROOT/public/g/$SLUG/index.html" ]; then
  BLOG="$LOG_DIR/build.log"
  [ -f "$LOG_DIR/build-fallback.log" ] && BLOG="$LOG_DIR/build-fallback.log"
  record_failed "빌드 실패${BUILD_ERR:+ ($BUILD_ERR)}${BUILD_FALLBACK_NOTE:+ — codex 폴백도 실패}"
  die "게임 파일이 생성되지 않았습니다 — $(tail -5 "$BLOG")"
fi
fi

# ════════════════════════════════════════════════════════════════
step "6. 자동 QA"
node factory/lib/qa.mjs "$SLUG" > "$LOG_DIR/qa-1.log" 2>&1
QA1=$?
cp -r "$WORK/qa/$SLUG" "$LOG_DIR/qa-1" 2>/dev/null
log "자동 QA 종료코드 $QA1"
tail -25 "$LOG_DIR/qa-1.log" >&2

# ════════════════════════════════════════════════════════════════
# 수학 오류는 이 프로젝트에서 가장 치명적인 결함이라 종합 검수와 분리해
# 독립 에이전트에게 전수 검산만 시킨다. 두 눈이 따로 보게 하는 것이 요점.
MATHCHECK_PROMPT="$(cat factory/prompts/35-mathcheck.md)

---
- slug: \`$SLUG\`
- 표본: \`factory/work/qa/$SLUG/report.json\` 의 \`problems_sample\`
- 단원: $(jqv "$WORK/slot.json" .unit.title) (${UNIT_GRADE}학년 ${UNIT_SEM}학기)"

run_mathcheck() {
  local tag="$1"
  rm -f "$WORK/mathcheck.json"
  # 게임을 만든 모델(claude)과 다른 회사 모델(GPT 상위 티어)로 검산한다 —
  # 같은 모델이 만들고 검산하면 같은 맹점을 공유한다.
  codex_run "$T_MATHCHECK" "$LOG_DIR/mathcheck-$tag.log" "$MATHCHECK_PROMPT" "$CODEX_MODEL_SMART"
  cp "$WORK/mathcheck.json" "$LOG_DIR/mathcheck-$tag.json" 2>/dev/null
  MATH_VERDICT="$(jqv "$WORK/mathcheck.json" .verdict)"
  MATH_ERRORS="$(jq -r '.errors | length' "$WORK/mathcheck.json" 2>/dev/null || echo '?')"
  if [ -z "$MATH_VERDICT" ]; then
    log "⚠️  검산 에이전트가 결과를 남기지 않음 — 종합 검수에 맡긴다"
    MATH_VERDICT="unknown"
  fi
  log "수학 검산($tag): $MATH_VERDICT / 오류 ${MATH_ERRORS}건"
}

step "6.5 수학 전수 검산"
run_mathcheck 1

# ════════════════════════════════════════════════════════════════
step "7. 검수"
REVIEW_PROMPT="$(cat factory/prompts/40-review.md)

---
- slug: \`$SLUG\`
- 커트라인: ${GATE_SCORE}점
- 자동 QA 리포트: \`factory/work/qa/$SLUG/report.json\`
- 독립 수학 검산 결과: \`factory/work/mathcheck.json\` — 여기서 오류가 나왔다면 그대로 인정하고 반영해라
- 스크린샷: \`factory/work/qa/$SLUG/mobile.png\`, \`tablet.png\`, \`desktop.png\` — **Read 툴로 실제로 봐라**
${USER_FEEDBACK:+
$USER_FEEDBACK
이 피드백을 채점에 직접 반영해라. 특히 지목된 문제가 이번 게임에도 있으면 must_fix 로 적어라.}"

stage_run "${REVIEW_RUNNER:-codex_run}" "${REVIEW_MODEL:-$CODEX_MODEL_SMART}" "$T_REVIEW" "$LOG_DIR/review-1.log" "$REVIEW_PROMPT"
cp "$WORK/review.json" "$LOG_DIR/review-1.json" 2>/dev/null

SCORE="$(jqv "$WORK/review.json" .score)"
PASSED="$(jqv "$WORK/review.json" .passed)"
REJECT="$(jqv "$WORK/review.json" .reject_immediately)"
log "1차 검수: ${SCORE:-?}점 / passed=${PASSED:-false} / reject=${REJECT:-false}"

# ════════════════════════════════════════════════════════════════
if [ "$PASSED" != "true" ] || [ "$QA1" -ne 0 ] || [ "$MATH_VERDICT" = "fail" ]; then
  step "8. 수정 (마지막 기회)"
  # 수정 라운드는 grok — 사용자 요청(claude 편중 완화). grok 은 로컬 무샌드박스라
  # qa.mjs(puppeteer + 로컬 서버)를 직접 돌릴 수 있다. codex 샌드박스는 그게 안 된다.
  stage_run "${FIX_RUNNER:-grok_run}" "${FIX_MODEL:-}" "$T_FIX" "$LOG_DIR/fix.log" "$(cat factory/prompts/45-fix.md)

---
- slug: \`$SLUG\`
- 검수 결과: \`factory/work/review.json\`
- 독립 수학 검산: \`factory/work/mathcheck.json\` — verdict 가 fail 이면 **이것부터** 고쳐라
- 자동 QA: \`factory/work/qa/$SLUG/report.json\`"

  node factory/lib/qa.mjs "$SLUG" > "$LOG_DIR/qa-2.log" 2>&1
  QA1=$?
  cp -r "$WORK/qa/$SLUG" "$LOG_DIR/qa-2" 2>/dev/null
  log "재 QA 종료코드 $QA1"

  step "8.5 수학 재검산"
  run_mathcheck 2

  step "9. 재검수"
  rm -f "$WORK/review.json"
  stage_run "${REVIEW_RUNNER:-codex_run}" "${REVIEW_MODEL:-$CODEX_MODEL_SMART}" "$T_REVIEW" "$LOG_DIR/review-2.log" "$REVIEW_PROMPT

이것은 **재검수**다. \`factory/work/fix.json\` 에 수정 내역이 있다. 수정이 실제로 반영됐는지 확인해라.
봐주지 마라 — 여전히 미달이면 폐기가 맞다."
  cp "$WORK/review.json" "$LOG_DIR/review-2.json" 2>/dev/null

  SCORE="$(jqv "$WORK/review.json" .score)"
  PASSED="$(jqv "$WORK/review.json" .passed)"
  REJECT="$(jqv "$WORK/review.json" .reject_immediately)"
  log "2차 검수: ${SCORE:-?}점 / passed=${PASSED:-false}"
fi

VERDICT="$(jqv "$WORK/review.json" .verdict)"

# ════════════════════════════════════════════════════════════════
if [ "$PASSED" != "true" ] || [ "$REJECT" = "true" ] || [ "$QA1" -ne 0 ] || [ "${MATH_VERDICT:-unknown}" = "fail" ]; then
  step "폐기"
  ARCHIVE="$ROOT/factory/state/rejected/$RUN_ID-$SLUG"
  mkdir -p "$(dirname "$ARCHIVE")"
  mv "$ROOT/public/g/$SLUG" "$ARCHIVE" 2>/dev/null
  record_failed
  finish "폐기" "${VERDICT:-품질 게이트 미달}"
  exit 0
fi

# ════════════════════════════════════════════════════════════════
step "10. 게시 준비"
# meta.qa 갱신 + queue.json(produced·mechanic_history·palette_history) + 허브 재빌드를
# 한 스크립트가 전부 한다. 예전에는 이 세 가지가 run.sh 안에 인라인으로 흩어져 있었고,
# 그래서 **부활(salvage) 경로처럼 run.sh 를 안 거치는 게시는 meta.qa.passed 가 false 로
# 남아 허브에서 사라졌다** — 실제로 5작(쩍쩍·첨벙·유리를 불어·등불을 켜·칸자물쇠)이
# 게시됐는데도 카탈로그에 안 뜨는 사고가 났다(2026-08-26 복구). 사람이 손으로 부활시킬
# 때도 똑같이 이 스크립트를 호출해야 한다 — docs/OPERATIONS.md §8 참조.
node factory/lib/publish-game.mjs "$SLUG" \
  --score "${SCORE:-0}" \
  --gate "$GATE_SCORE" \
  --run "$RUN_ID" \
  --unit "$(jqv "$WORK/slot.json" .unit.id)" \
  --mechanic "$(jqv "$WORK/chosen.json" .mechanic)" \
  --mood "$(jqv "$WORK/chosen.json" .art_direction.mood)" \
  --bg "$(jq -r '.art_direction.palette[0] // ""' "$WORK/chosen.json" 2>/dev/null)" \
  --notes-from "$WORK/review.json" >&2 || die "게시 처리 실패 (게이트 미달이거나 필수 파일 누락)"

# 게시 직후 정합성 감사 — 게시했는데 허브에 안 보이는 사고를 그 자리에서 잡는다.
node factory/lib/verify-catalog.mjs >>"$LOG_DIR/verify-catalog.log" 2>&1 \
  || log "⚠️  카탈로그 정합성 경고 — $(tail -3 "$LOG_DIR/verify-catalog.log" | tr '\n' ' ')"

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

# ════════════════════════════════════════════════════════════════
# 게임 게시가 끝난 뒤에만 시도한다 — 이 게임의 성공 여부와 완전히 무관한 부가
# 작업이라 실패해도 die() 로 전체를 죽이지 않는다. 게임 N개마다(기본 10개)
# 한 번, 새 카테고리에서 레퍼런스 후보를 찾아 references/pending/ 에 쌓아둔다.
# 바로 game-references.json 에 섞이지 않는다 — 사람이 검토 후
# merge-references.mjs 로 승인해야 실제 기획에 반영된다.
if node factory/lib/scout-references.mjs check >"$LOG_DIR/scout.log" 2>&1; then
  step "12. 레퍼런스 스카우트"
  node factory/lib/scout-references.mjs prepare >>"$LOG_DIR/scout.log" 2>&1
  SCOUT_PROMPT="$(cat factory/prompts/50-reference-scout.md)

---
## 이번 포커스
\`\`\`json
$(cat "$WORK/scout-focus.json" 2>/dev/null)
\`\`\`"
  stage_run "${SCOUT_RUNNER:-grok_run}" "" "$T_SCOUT" "$LOG_DIR/scout-agent.log" "$SCOUT_PROMPT"
  if node factory/lib/scout-references.mjs ingest >>"$LOG_DIR/scout.log" 2>&1; then
    log "레퍼런스 후보 보관 — $(tail -2 "$LOG_DIR/scout.log" | tr '\n' ' ')"
  else
    log "⚠️  레퍼런스 스카우트 실패(게임 게시와는 무관) — $(tail -3 "$LOG_DIR/scout.log" | tr '\n' ' ')"
  fi
fi

finish "게시"
