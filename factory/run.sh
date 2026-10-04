#!/usr/bin/env bash
#
# 초등 수학 게임 1개 생산 사이클.
#
# 리듬: **하루 1작이 목표**다. cron 은 2시간마다 이걸 호출하지만, 오늘(KST) 이미 게시한
# 게임이 있으면 최상단 「하루 1작 가드」가 신규 생산을 건너뛴다 — 하루 최대 12번 시도하되
# 첫 성공에서 멈춘다. 가드 해제는 DAILY_TARGET=0 또는 FORCE_PRODUCE=1.
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

# ⚠️ 이 스크립트는 **bash 전용**이다. zsh 로 돌리지 마라.
# 이유: zsh 는 따옴표 없는 파라미터 확장을 단어 분리하지 않는다. 즉
#   ASSET_IDS=$'thumb\nsquare\ntitle'; for aid in $ASSET_IDS   # bash: 3회, zsh: 1회
# 처럼 여러 줄 문자열을 도는 루프(4단계 아트 생성)가 조용히 1회만 돈다 — 에러 없이
# 에셋 2개가 사라진다. 아래 가드로 즉시 죽인다.
if [ -z "${BASH_VERSION:-}" ]; then
  echo "run.sh 는 bash 전용입니다 (zsh 워드분리 차이). 'bash factory/run.sh' 로 실행하세요." >&2
  exit 1
fi

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

# ── 3안 병렬 레인 (2026-10-03 사용자 지시 「병렬로 바꿔」) ─────────────────────
# 부모 회차가 기획안 3개를 보완한 뒤 레인 3개를 동시에 띄운다(launch_lanes). 레인은 이 스크립트를
# MGF_LANE_ITEM=<기획안 폴더> 로 다시 부른 것이다 — 작업 폴더(MGF_WORK)·RUN_ID 를 레인별로 나누고,
# 전역 락·하루 1작 가드는 부모가 잡고 있으므로 건너뛴다. 머신 공용 자원은 잠금으로 직렬화한다:
#   Unity 빌드(unity.lock.d — build.sh) · QA/첫 플레이 fps 측정(gpu.lock.d — machine-lock.mjs) ·
#   게시·장부·git·배포(publish.lock.d — 아래 acquire_lock).
MGF_LANE_ITEM="${MGF_LANE_ITEM:-}"
RUN_ID="${MGF_RUN_ID:-$(date +%Y%m%d-%H%M%S)}"
LOG_DIR="$ROOT/logs/$RUN_ID"
if [ -n "$MGF_LANE_ITEM" ]; then
  # factory/work 밖에 둔다 — 일반 회차가 시작하며 factory/work 를 통째로 지워도 레인 작업이 살아남게.
  export MGF_WORK="factory/work-lanes/$RUN_ID"
fi
WORK="$ROOT/${MGF_WORK:-factory/work}"
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

# 레인의 에이전트는 자기 작업 폴더를 봐야 한다. 프롬프트·문서가 「factory/work/」 를 글자로 박아 두었으므로
# 러너에 넘기기 직전에 레인 경로로 바꿔 준다(레인이 아니면 그대로).
lane_prompt() {
  local p="$1"
  if [ -n "${MGF_WORK:-}" ] && [ "$MGF_WORK" != "factory/work" ]; then
    p="${p//factory\/work\//$MGF_WORK/}"
    p="이 회차의 작업 폴더는 \`$MGF_WORK/\` 다(병렬 레인). 다른 레인의 \`factory/work-lanes/*\` 는 건드리지 마라. 환경 변수 MGF_WORK 가 이미 설정돼 있어 \`node factory/lib/qa.mjs\` 도 이 폴더에 쓴다.

$p"
  fi
  printf '%s' "$p"
}

# claude 헤드리스 실행. $1=제한시간 $2=로그파일 $3=프롬프트 [$4=모델(기본 $CLAUDE_MODEL)]
claude_run() {
  local secs="$1" logfile="$2" prompt; prompt="$(lane_prompt "$3")"; local model="${4:-$CLAUDE_MODEL}"
  run_timeout "$secs" env -u ANTHROPIC_API_KEY claude -p "$prompt" \
    --model "$model" \
    --dangerously-skip-permissions \
    --add-dir "$ROOT" \
    >"$logfile" 2>&1
}

# 모델 이름으로 추론 등급을 고른다 (config.sh 「codex 추론 등급」 참조).
# 상위 티어(astra/sol) = 판단이 어려운 단계 → ultra, 그 외(terra/luna) → medium.
codex_reasoning_for() {
  case "$1" in
    *astra*|*sol*) echo "${CODEX_REASONING_SMART:-ultra}" ;;
    *)             echo "${CODEX_REASONING:-medium}" ;;
  esac
}

# codex 헤드리스 실행. $1=제한시간 $2=로그파일 $3=프롬프트 [$4=모델(기본 $CODEX_MODEL)] [$5=추론등급]
codex_run() {
  local secs="$1" logfile="$2" prompt; prompt="$(lane_prompt "$3")"; local model="${4:-$CODEX_MODEL}"
  local effort="${5:-$(codex_reasoning_for "$model")}"
  run_timeout "$secs" env -u ANTHROPIC_API_KEY codex exec "$prompt" \
    --model "$model" \
    -c model_reasoning_effort="$effort" \
    -c features.multi_agent=false \
    ${CODEX_SANDBOX_ARGS:---sandbox workspace-write} \
    --skip-git-repo-check \
    --cd "$ROOT" \
    >"$logfile" 2>&1
}

# grok 헤드리스 실행. $1=제한시간 $2=로그파일 $3=프롬프트
grok_run() {
  local secs="$1" logfile="$2" prompt; prompt="$(lane_prompt "$3")"
  run_timeout "$secs" grok -p "$prompt" \
    --model "$GROK_MODEL" \
    --always-approve \
    --output-format plain \
    --cwd "$ROOT" \
    >"$logfile" 2>&1
}

# Antigravity CLI(agy) 헤드리스 실행 — Gemini. $1=제한시간 $2=로그파일 $3=프롬프트 [$4=모델(기본 $AGY_MODEL)]
# (2026-10-03 사용자 지시: 2번 기획자 gpt-5.6-sol → Antigravity Gemini 3.8)
# agy 는 작업 디렉터리 기준으로 파일을 쓰므로 서브셸에서 $ROOT 로 cd 한 뒤 실행한다. stdin 은 닫는다.
_agy_exec() {
  cd "$ROOT" || exit 1
  exec agy -p "$2" --model "$1" --dangerously-skip-permissions --add-dir "$ROOT" </dev/null
}
agy_run() {
  local secs="$1" logfile="$2" prompt; prompt="$(lane_prompt "$3")"; local model="${4:-$AGY_MODEL}"
  run_timeout "$secs" env -u ANTHROPIC_API_KEY bash -c "$(declare -f _agy_exec); ROOT='$ROOT'; _agy_exec \"\$1\" \"\$2\"" _ "$model" "$prompt" \
    >"$logfile" 2>&1
}

# codex 를 상위 티어(sol)로 돌리는 단축 러너 — 기획 2번·검수처럼 판단이 어려운 단계용.
# 추론 등급은 codex_run 이 모델 이름을 보고 ultra 로 올린다.
codex_smart_run() {
  codex_run "$1" "$2" "$3" "${4:-$CODEX_MODEL_SMART}"
}

# 3번 기획자 전용 (2026-10-03 사용자 지시: claude sonnet 5.5)
claude_design3_run() {
  claude_run "$1" "$2" "$3" "${4:-$DESIGN3_MODEL}"
}

# 1번 기획자 전용 (2026-10-03 사용자 지시: gpt-6.1-sol)
codex_design1_run() {
  codex_run "$1" "$2" "$3" "${4:-$DESIGN1_MODEL}"
}

# 프롬프트 파일을 읽는다. 학교급이 중학교면 초등 교육과정 경로를 중학교 파일로 바꾸고
# factory/prompts/_school-middle.md(학교급 보충 지시 — 본문과 충돌하면 이쪽이 이긴다)를 덧붙인다.
# 빌드 프롬프트는 BUILD_TECH=unity 일 때 30-build-unity.md(Unity 트랙 계약)를 추가로 붙인다.
# 초등 회차에서는 파일 내용을 그대로 돌려준다 — 초등 프롬프트는 한 글자도 바뀌지 않는다.
prompt_file() {
  local f="$1"
  if [ "${SCHOOL:-elementary}" != "middle" ]; then cat "$f"; return; fi
  sed 's#curriculum/2022-elementary-math\.json#curriculum/2022-middle-math.json#g' "$f"
  printf '\n\n---\n\n'
  cat factory/prompts/_school-middle.md
  if { [ "$(basename "$f")" = "30-build.md" ] || [ "$(basename "$f")" = "45-fix.md" ]; } && [ "${BUILD_TECH:-html}" = "unity" ]; then
    printf '\n\n---\n\n'
    cat factory/prompts/30-build-unity.md
  fi
}

# 원하는 러너가 실제로 쓸 수 있는지 확인하고, 없으면 codex → claude 순으로 폴백한다.
# (grok 잔액 소진 402, claude 세션 한도처럼 러너 하나가 죽어도 사이클이 멈추면 안 된다)
resolve_runner() {
  local want="$1"
  case "$want" in
    grok_run)                  command -v grok   >/dev/null 2>&1 && { echo grok_run;   return; } ;;
    codex_run|codex_smart_run|codex_design1_run) command -v codex  >/dev/null 2>&1 && { echo "$want";    return; } ;;
    claude_run|claude_design3_run) command -v claude >/dev/null 2>&1 && { echo "$want"; return; } ;;
    agy_run)                   command -v agy    >/dev/null 2>&1 && { echo agy_run;    return; } ;;
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

# 판정 단계(수학 검산·검수) 러너 + 인프라 실패 폴백 (2026-09-26).
# codex 쿼터가 소진되면(402) 검산이 「unknown」, 검수가 결과 없음으로 끝나 수정 루프가 헛돈다
# (9/9~10, 9/26 실제 발생). 로그에 인프라 오류가 보이면 JUDGE_FALLBACK_RUNNER(기본 grok —
# 빌더(codex/claude)와 다른 회사라 교차 검증 원칙이 유지된다. 이미지 판독 가능 확인)로 1회 재시도한다.
judge_run() {
  local want="$1" model="$2" secs="$3" logfile="$4" prompt="$5" err
  stage_run "$want" "$model" "$secs" "$logfile" "$prompt"
  err="$(runner_infra_err "$logfile")"
  [ -z "$err" ] && return 0
  local fb="${JUDGE_FALLBACK_RUNNER:-grok_run}"
  log "⚠️  판정 러너($want) 인프라 실패($err) → $fb 로 1회 재시도 — ${logfile%.log}-fallback.log"
  JUDGE_FALLBACK_NOTE="${JUDGE_FALLBACK_NOTE:+$JUDGE_FALLBACK_NOTE; }$(basename "$logfile" .log): ${want}→${fb} (${err})"
  stage_run "$fb" "" "$secs" "${logfile%.log}-fallback.log" "$prompt"
}

# ── 러너 장애 분류 (P0-1, 2026-09-06) ──────────────────────────────
# 러너 바이너리는 살아 있는데 API 가 죽는 경우(grok 402 잔액 소진, 인증 만료, 쿼터 초과)는
# resolve_runner 가 못 잡는다. 로그에서 이 패턴이 보이면 "모델이 못 고친 것"이 아니라
# **인프라 실패**다 — 같은 러너로 재시도하거나 재검사 루프를 도는 것은 순수한 낭비다.
# 2026-08-27~28 에 4회 연속으로, 2026-09-01~03 에 20회 연속으로 여기서 시간을 태웠다.
RUNNER_ERR_RE='RESOURCE_EXHAUSTED|hit your usage limit|usage limit reached|status 402|402 Payment Required|Payment Required|usage balance exhausted|insufficient_quota|quota exceeded|exceeded your current quota|rate limit exceeded|401 Unauthorized|invalid api key|invalid_api_key|authentication_error|Not authenticated|Please run .?login'
# 러너 CLI 가 직접 찍는 오류 줄에서만 찾는다 — codex `ERROR: …`, grok JSON `"message": "API error (status 402 …)"`,
# claude `API Error …`. 로그 전체를 grep 하면 러너가 읽은 문서(OPERATIONS.md 의 「402 Payment Required」)나
# 러너의 최종 보고 인용까지 걸려, 멀쩡히 끝난 수정 라운드를 인프라 실패로 중단시켰다(2026-09-27 협곡 사수).
RUNNER_ERR_LINE_RE='^[[:space:]]*(ERROR:|Error:|API Error|"message": *"API error|"error": *\{)'
runner_infra_err() {
  [ -f "$1" ] || return 0
  grep -E "$RUNNER_ERR_LINE_RE" "$1" 2>/dev/null | grep -m1 -Eio "$RUNNER_ERR_RE" | head -1
}

# 디렉터리 내용 해시. 수정 러너가 **실제로 파일을 바꿨는지** 확인하는 데 쓴다.
# (rc=0 으로 끝났는데 아무것도 안 고친 회차를 재검수에 태우면 같은 코드를 다시 채점한다)
# 최신 검수의 **미해결 must_fix high** 개수. 게시 게이트(P0-3)가 이걸 0으로 요구한다.
# review.json 이 없거나 깨져 있으면 「검사하지 못했다」이므로 1로 친다 (통과시키지 않는다).
open_high() {
  local f="$WORK/review.json"
  [ -f "$f" ] || { echo 1; return; }
  jq -r '[.must_fix[]? | select((.severity // "") == "high")] | length' "$f" 2>/dev/null || echo 1
}

tree_hash() {
  [ -d "$1" ] || { echo "none"; return; }
  find "$1" -type f ! -name '.DS_Store' -print0 2>/dev/null \
    | LC_ALL=C sort -z | xargs -0 shasum 2>/dev/null | shasum | awk '{print $1}'
}

# queue.json failed 장부 기입. $1=사유(비우면 생략).
# 원래 폐기 단계에만 인라인으로 있어서 **빌드 단계에서 죽은 회차는 장부에 안 남았다**
# (2026-08-27~28 grok 402 4연속 사망이 전부 미기록). 빌드 실패 경로도 이걸 거친다.
record_failed() {
  local reason="${1:-}" held=0
  [ "$(cat "$ROOT/factory/state/publish.lock.d/pid" 2>/dev/null)" = "$$" ] && held=1
  acquire_lock publish
  node -e '
    const fs=require("fs"),p=process.argv[1];
    const q=fs.existsSync(p)?JSON.parse(fs.readFileSync(p,"utf8")):{produced:[],failed:[],mechanic_history:[]};
    q.failed=q.failed||[];
    const e={run:process.argv[2],slug:process.argv[3],title:process.argv[4],score:Number(process.argv[5])||0,unit:process.argv[6],school:process.argv[8]||"elementary",at:new Date().toISOString()};
    if(process.argv[7]) e.reason=process.argv[7];
    q.failed.push(e);
    fs.writeFileSync(p,JSON.stringify(q,null,2)+"\n");
  ' "$ROOT/factory/state/queue.json" "$RUN_ID" "${SLUG:-}" "${TITLE:-}" "${SCORE:-0}" "$(jqv "$WORK/slot.json" .unit.id)" "$reason" "$SCHOOL"
  [ "$held" = 1 ] || release_lock publish
}

# ── 하루 1작 가드 (2026-09-06 체제 전환) ───────────────────────────
# 크론은 2시간마다 부르지만 목표는 **하루 1작**이다. 오늘(KST) 이미 게시한 게임이 있으면
# 신규 생산을 시작하지 않는다 — 로그만 남기고 조용히 끝낸다(디스코드 중복 알림 없음).
#   DAILY_TARGET=0        가드 해제 (무제한 시도)
#   FORCE_PRODUCE=1       오늘 게시작이 있어도 강행
#   RESUME_FROM=<stage>   진행 중이던 회차를 이어받는 것이므로 가드를 적용하지 않는다
# 3안 포트폴리오(2026-10-03): 앞 회차가 남긴 보완 기획안(factory/state/pending/<학교급>/)이 있으면
# 그건 「새 생산」이 아니라 같은 세트의 나머지다 — 하루 1작 가드를 적용하지 않고 먼저 만든다.
PENDING_ROOT="$ROOT/factory/state/pending/$SCHOOL"
HAS_PENDING="$(ls -d "$PENDING_ROOT"/*/ 2>/dev/null | head -1)"
if [ "${DAILY_TARGET:-1}" -gt 0 ] && [ "${FORCE_PRODUCE:-0}" != "1" ] && [ -z "${RESUME_FROM:-}" ] && [ -z "$HAS_PENDING" ] && [ -z "$MGF_LANE_ITEM" ]; then
  TODAY_DONE="$(node -e '
    const fs=require("fs"), p=process.argv[1], target=Number(process.argv[2])||1;
    if(!fs.existsSync(p)) process.exit(0);
    let q; try{ q=JSON.parse(fs.readFileSync(p,"utf8")); }catch(e){ process.exit(0); }
    const kst=(v)=>{ const t=new Date(v).getTime();
      return Number.isFinite(t) ? new Date(t+9*3600e3).toISOString().slice(0,10) : null; };
    const today=kst(Date.now()), school=process.argv[3]||"elementary";
    // 학교급별로 센다 (2026-09-26). school 필드가 없던 기존 항목은 단원 id 로 가른다(m2s2-… = 중학교).
    const schoolOf=e=>e.school||(/^m\d/.test(e.unit||"")?"middle":"elementary");
    const hit=(q.produced||[]).filter(e=>e && e.at && kst(e.at)===today && schoolOf(e)===school);
    if(hit.length>=target) console.log(hit.map(e=>`${e.title||e.slug}(${e.slug})`).join(", "));
  ' "$ROOT/factory/state/queue.json" "${DAILY_TARGET:-1}" "$SCHOOL" 2>/dev/null)"
  if [ -n "$TODAY_DONE" ]; then
    log "🎯 오늘 목표 달성 — 다음 발진은 내일 (오늘 게시: $TODAY_DONE)"
    echo "🎯 오늘 목표 달성 — 다음 발진은 내일 (오늘 게시: $TODAY_DONE)"
    exit 0
  fi
fi

# ── 배치 생산 중이면 일반 회차는 쉰다 (2026-10-05 batch-middle.sh) ──────────
# 배치 지휘 스크립트가 factory/state/batch.active 에 pid 를 적어 둔다. 그 pid 가 살아 있으면
# 크론의 일반 회차(기획부터 시작)는 자원을 다투지 않게 조용히 건너뛴다. 레인은 해당 없음.
if [ -z "$MGF_LANE_ITEM" ] && [ -f "$ROOT/factory/state/batch.active" ] \
   && kill -0 "$(cat "$ROOT/factory/state/batch.active" 2>/dev/null)" 2>/dev/null; then
  echo "⏭ 배치 생산 중(pid $(cat "$ROOT/factory/state/batch.active")) — 일반 회차는 건너뜁니다."
  exit 0
fi

# ── 락 ────────────────────────────────────────────────────────────
# 레인은 부모가 잡은 전역 락 아래에서 돈다 — 락을 다시 잡지 않는다.
if [ -n "$MGF_LANE_ITEM" ]; then
  :
elif [ -f "$LOCK" ]; then
  LOCK_PID="$(cat "$LOCK" 2>/dev/null)"
  if [ -n "$LOCK_PID" ] && kill -0 "$LOCK_PID" 2>/dev/null; then
    echo "⏭  이전 사이클(pid $LOCK_PID)이 아직 실행 중입니다. 이번 회차는 건너뜁니다."
    exit 0
  fi
  log "고아 락 발견 — 제거"
  rm -f "$LOCK"
fi
[ -n "$MGF_LANE_ITEM" ] || echo $$ > "$LOCK"

# ── 러너 사전 점검 ────────────────────────────────────────────────
# 어떤 러너로 이번 회차를 돌리는지 로그 첫머리에 남긴다. grok 잔액 소진(402)이나
# claude 세션 한도처럼 러너 하나가 조용히 죽으면 원인을 찾느라 로그를 뒤져야 했다.
for _c in grok codex claude agy; do
  command -v "$_c" >/dev/null 2>&1 && _AVAIL="${_AVAIL:-}$_c " || true
done
log "러너 가용: ${_AVAIL:-없음}| 빌드=$(resolve_runner "${BUILD_RUNNER:-grok_run}") 검수=$(resolve_runner "${REVIEW_RUNNER:-codex_run}") 수정=$(resolve_runner "${FIX_RUNNER:-grok_run}")"

STARTED_AT="$(date +%s)"
SLUG=""; TITLE=""; SCORE=""; VERDICT=""; DEPLOY_URL=""; STATUS="진행중"
BUILD_FALLBACK_NOTE=""   # 빌드 러너 폴백이 발동하면 채워진다 — 리포트에 그대로 남긴다

cleanup() { release_lock publish; [ -n "$MGF_LANE_ITEM" ] || rm -f "$LOCK"; }

# 머신 공용 자원 잠금(병렬 레인용). factory/state/<이름>.lock.d/pid — 잡은 pid 가 죽었으면 회수한다.
# machine-lock.mjs·unity/build.sh 와 같은 규약. $1=이름 [$2=최대 대기초(기본 3600)]
acquire_lock() {
  local d="$ROOT/factory/state/$1.lock.d" waited=0 p
  [ "$(cat "$d/pid" 2>/dev/null)" = "$$" ] && return 0   # 이미 이 프로세스가 쥐고 있다(재진입)
  while ! mkdir "$d" 2>/dev/null; do
    p="$(cat "$d/pid" 2>/dev/null)"
    if [ -n "$p" ] && ! kill -0 "$p" 2>/dev/null; then rm -rf "$d"; continue; fi
    [ "$waited" -eq 0 ] && log "⏳ 잠금 $1 대기 (pid ${p:-?})"
    sleep 3; waited=$((waited+3))
    if [ "$waited" -ge "${2:-3600}" ]; then log "⚠️  잠금 $1 대기 초과 — 강행"; mkdir -p "$d"; break; fi
  done
  echo $$ > "$d/pid"
}
release_lock() { [ "$(cat "$ROOT/factory/state/$1.lock.d/pid" 2>/dev/null)" = "$$" ] && rm -rf "$ROOT/factory/state/$1.lock.d"; return 0; }
trap cleanup EXIT

# ── 최종 보고 ──────────────────────────────────────────────────────
finish() {
  local status="$1" note="${2:-}"
  local elapsed=$(( $(date +%s) - STARTED_AT ))
  local mins=$(( elapsed / 60 ))
  local report_file="$LOG_DIR/report.md"

  {
    if [ "$status" = "게시" ]; then
      echo "🎮 ${SCHOOL_TAG:+$SCHOOL_TAG }**새 게임이 나왔습니다** — ${TITLE:-?}"
    elif [ "$status" = "폐기" ]; then
      echo "🗑 ${SCHOOL_TAG:+$SCHOOL_TAG }**게임 폐기** — 품질 게이트 미달"
    elif [ "$status" = "건너뜀" ]; then
      echo "⏭ **이번 회차 건너뜀**"
    else
      echo "💀 ${SCHOOL_TAG:+$SCHOOL_TAG }**생산 실패**"
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
      [ -n "$u" ] && echo "**단원**: $([ "$SCHOOL" = middle ] && echo "중학교 ")${g}학년 ${s}학기 · $u"
      [ -n "$mech" ] && echo "**차용 메커닉**: $mech"
    fi
    if [ -f "$WORK/slot.json" ]; then
      local std
      std="$(jq -r '.unit.standards | join(", ")' "$WORK/slot.json" 2>/dev/null)"
      [ -n "$std" ] && echo "**성취기준**: $std"
    fi
    [ -n "$SCORE" ] && echo "**검수 점수**: ${SCORE}/100 (커트라인 ${GATE_SCORE})"
    local _left; _left="$(ls -d "${PENDING_ROOT:-/nonexistent}"/*/ 2>/dev/null | wc -l | tr -d ' ')"
    [ "${_left:-0}" -gt 0 ] && echo "**3안 세트**: 같은 단원 보완 기획안 ${_left}개가 남아 다음 회차에 이어서 만든다"
    [ "${FIX_ROUNDS:-0}" -gt 0 ] && echo "**수정 루프**: ${FIX_ROUNDS}/${MAX_FIX_ROUNDS}회"
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
    [ -n "${JUDGE_FALLBACK_NOTE:-}" ] && { echo ""; echo "⚙️ **판정 폴백**: $JUDGE_FALLBACK_NOTE"; }
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

# 대기열(factory/state/pending/<학교급>/*)의 기획안을 레인으로 동시에 띄우고 전부 끝날 때까지 기다린다.
# 레인마다 이 스크립트를 새로 부른다(자기 스냅샷·RUN_ID·작업 폴더). 각 레인이 스스로 게시·배포·보고한다.
# 꺼낸 기획안은 즉시 부모 로그 폴더로 옮긴다 — 레인이 죽어도 같은 안을 무한 재시도하지 않는다.
launch_lanes() {
  local items k=0 pids=() d dst
  items="$(ls -d "$PENDING_ROOT"/*/ 2>/dev/null | sort | head -n "${PARALLEL_LANES:-3}")"
  [ -n "$items" ] || return 0
  step "병렬 레인 $(echo "$items" | wc -l | tr -d ' ')개"
  while IFS= read -r d; do
    [ -n "$d" ] || continue
    d="${d%/}"; k=$((k+1))
    dst="$LOG_DIR/lane-$k-$(basename "$d")"
    mv "$d" "$dst" || continue
    log "레인 $k: 「$(jqv "$dst/chosen.json" .title)」 ($(jqv "$dst/chosen.json" .slug)) → logs/$RUN_ID-L$k"
    env -u MGF_SNAPSHOT MGF_LANE_ITEM="$dst" MGF_RUN_ID="$RUN_ID-L$k" MGF_LANE_NO="$k" \
      bash "$ROOT/factory/run.sh" >"$LOG_DIR/lane-$k.out" 2>"$LOG_DIR/lane-$k.log" &
    pids+=($!)
    sleep 15   # 아트 생성·codex 세션이 한꺼번에 몰리지 않게 살짝 엇갈린다
  done <<< "$items"
  local rc fails=0
  for k in "${!pids[@]}"; do
    wait "${pids[$k]}"; rc=$?
    [ "$rc" -eq 0 ] || fails=$((fails+1))
    log "레인 $((k+1)) 종료 rc=$rc — $(grep -m1 -E '새 게임|폐기|실패' "$LOG_DIR/lane-$((k+1)).out" 2>/dev/null | head -c 120)"
  done
  echo "🛤 병렬 레인 ${#pids[@]}개 종료 (비정상 종료 ${fails}개) — 각 레인이 개별 보고했다. 로그: logs/$RUN_ID-L*"
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
# 3안 병렬 생산 (2026-10-03):
#  - 레인(MGF_LANE_ITEM)이면 받은 기획안 폴더로 아트 단계부터 만든다.
#  - 레인이 아닌데 대기열(factory/state/pending/<학교급>/)이 남아 있으면 전부 레인으로 동시에 띄우고 끝낸다.
PORTFOLIO_ITEM=""
if [ -n "$MGF_LANE_ITEM" ]; then
  { [ -f "$MGF_LANE_ITEM/chosen.json" ] && [ -f "$MGF_LANE_ITEM/slot.json" ]; } || die "레인 기획안 폴더가 비었습니다: $MGF_LANE_ITEM"
  PORTFOLIO_ITEM="$(basename "$MGF_LANE_ITEM")"
  rm -rf "$WORK"; mkdir -p "$WORK"
  cp "$MGF_LANE_ITEM/chosen.json" "$MGF_LANE_ITEM/slot.json" "$WORK/"
  RESUME_FROM="art"
  log "🛤  레인 ${MGF_LANE_NO:-?}: 「$(jqv "$WORK/chosen.json" .title)」 ($PORTFOLIO_ITEM) — 작업 폴더 $MGF_WORK"
elif [ -z "$RESUME_FROM" ] && [ "${RESUME:-0}" != "1" ] && [ -n "$(ls -d "$PENDING_ROOT"/*/ 2>/dev/null)" ]; then
  launch_lanes
  exit 0
fi
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
[ -f "$ROOT/$CURRICULUM_FILE" ] \
  || die "교육과정 파일이 없습니다: $ROOT/$CURRICULUM_FILE (SCHOOL=$SCHOOL, ROOT=$ROOT 가 저장소를 가리키는지 확인해라)"
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
log "슬롯: $(jqv "$WORK/slot.json" .school_label) ${UNIT_SEM}학기 · $UNIT_TITLE"

SLOT_CTX="$(cat "$WORK/slot.json")"

# ── 사용자 피드백 인박스 ────────────────────────────────────────
# factory/state/feedback.md 에 뭔가 적혀 있으면 이번 사이클의 기획·검수에 끼워 넣고
# 아카이브로 옮긴다. 매 사이클 반복 주입되지 않게 읽는 즉시 비운다.
FEEDBACK_FILE="$ROOT/factory/state/feedback.md"
USER_FEEDBACK=""
if [ "$RESUMED" = "0" ] && [ -f "$FEEDBACK_FILE" ]; then
  # 안내 주석 블록(<!-- … -->)을 통째로 걷어낸다. 예전 줄 단위 grep 필터는 주석 한 줄
  # (「피드백이 매 사이클…」)을 놓쳐 매 회차 가짜 피드백을 주입하고 아카이브를 쌓았다.
  # 닫는 `-->` 가 없는(아래 옛 head -8 이 잘라 먹은) 파일도 처리한다 (2026-09-26).
  FB_BODY="$(perl -0pe 's/<!--.*?(-->|\z)//gs' "$FEEDBACK_FILE" 2>/dev/null | sed '/^[[:space:]]*$/d')"
  if [ -n "$FB_BODY" ]; then
    USER_FEEDBACK="## 사용자 피드백 (직접 반영해라 — 무시하지 마라)

$FB_BODY"
    mkdir -p "$ROOT/factory/state/feedback-archive"
    cp "$FEEDBACK_FILE" "$ROOT/factory/state/feedback-archive/$RUN_ID.md"
    # 안내 주석만 남기고 본문은 비운다
    printf '%s\n' '<!--' '사용자 피드백 인박스. 이 주석 아래에 적은 내용은 다음 생산 사이클의 기획·검수 프롬프트에' \
      '그대로 끼워 넣어지고, 한 번 읽히면 factory/state/feedback-archive/<RUN_ID>.md 로 옮겨진 뒤 비워진다.' \
      '초등·중학교 크론이 이 인박스를 공유한다 — 먼저 도는 회차가 가져간다.' '-->' > "$FEEDBACK_FILE"
    log "사용자 피드백 반영 — factory/state/feedback-archive/$RUN_ID.md 로 보관"
  fi
fi

# ════════════════════════════════════════════════════════════════
if [ "$RESUMED" = "1" ]; then
  log "기획·심사 단계 건너뜀 (RESUME)"
else
step "2. 기획 (병렬 ${DESIGN_VARIANTS}개)"
DESIGN_PIDS=(); declare -a DESIGN_USED DESIGN_PROMPT
for i in $(seq 1 "$DESIGN_VARIANTS"); do
  PROMPT="$(prompt_file factory/prompts/10-design.md)

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
  # (쉼표 구분, i 번째가 i 번 기획자). 기본값은 codex·Gemini(agy)·claude 3사다.
  IFS=',' read -ra _DR <<< "${DESIGN_RUNNERS:-grok_run,codex_smart_run,codex_run}"
  DESIGN_R="$(resolve_runner "${_DR[$((i-1))]:-codex_run}")"
  DESIGN_USED[$i]="$DESIGN_R"; DESIGN_PROMPT[$i]="$PROMPT"
  "$DESIGN_R" "$T_DESIGN" "$LOG_DIR/design-$i.log" "$PROMPT" &
  DESIGN_PIDS+=($!)
done
for pid in ${DESIGN_PIDS[@]+"${DESIGN_PIDS[@]}"}; do wait "$pid"; done

# 인프라 실패(402·쿼터·인증)로 안이 안 나온 기획자만 다른 회사 러너로 1회 재시도 (2026-10-03).
# 10/2~3 에 codex 설치가 깨지고(resolve_runner 가 grok 으로 폴백) grok 잔액이 소진돼 3안이 전부 402 로 죽었는데,
# 살아 있던 claude 로 넘어가지 않아 2시간마다 「기획안 0개」 실패 알림만 12번 쌓였다.
DESIGN_PIDS=()
for i in $(seq 1 "$DESIGN_VARIANTS"); do
  [ -f "$WORK/concept-$i.json" ] && continue
  _err="$(runner_infra_err "$LOG_DIR/design-$i.log")"
  [ -n "$_err" ] || continue
  case "${DESIGN_USED[$i]}" in
    codex*) _fb=claude_run ;;
    *) command -v codex >/dev/null 2>&1 && _fb=codex_run || _fb=claude_run ;;
  esac
  log "⚠️  ${i}번 기획자 인프라 실패(${DESIGN_USED[$i]}: ${_err}) → ${_fb} 로 1회 재시도"
  "$_fb" "$T_DESIGN" "$LOG_DIR/design-$i-fallback.log" "${DESIGN_PROMPT[$i]}" &
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
elif [ "${PORTFOLIO:-1}" = "1" ]; then
  # 3안 포트폴리오 (2026-10-03 사용자 지시): 택일하지 않고 3안을 전부 보완해서 전부 만든다.
  # 보완은 심사와 같은 기준(D1~D10)으로 실격 사유를 「탈락」 대신 「수정」한다(16-refine.md).
  # 보완한 안을 전부 대기열에 넣고 곧바로 레인으로 동시에 만든다(launch_lanes). build_order_hint 는 레인 번호 순서다.
  step "3. 기획 보완 (3안 전부 — 택일 없음)"
  codex_run "$T_JUDGE" "$LOG_DIR/refine.log" "$(prompt_file factory/prompts/16-refine.md)

---
## 이번 슬롯
\`\`\`json
$SLOT_CTX
\`\`\`" "$CODEX_MODEL_SMART"
  _err="$(runner_infra_err "$LOG_DIR/refine.log")"
  [ -n "$_err" ] && log "⚠️  보완 러너 인프라 실패($_err) — 원안 그대로 3안을 만든다"
  mkdir -p "$LOG_DIR/concepts"; cp "$WORK"/concept-*.json "$LOG_DIR/concepts/" 2>/dev/null || true
  # build_order_hint 순(없으면 n 순)으로 정렬
  ORDERED="$(for f in "$WORK"/concept-*.json; do
      printf '%s\t%s\n' "$(jq -r '._refine.build_order_hint // .n // 9' "$f" 2>/dev/null || echo 9)" "$f"
    done | sort -n | cut -f2)"
  _k=0
  while IFS= read -r f; do
    [ -n "$f" ] || continue
    _k=$((_k+1))
    _dst="$PENDING_ROOT/${RUN_ID}-$_k"
    mkdir -p "$_dst"; cp "$f" "$_dst/chosen.json"; cp "$WORK/slot.json" "$_dst/slot.json"
    log "보완 ${_k}순위: 「$(jqv "$f" .title)」 → 레인 대기열"
  done <<< "$ORDERED"
  # 3안을 레인으로 동시에 만든다(병렬, 2026-10-03). 이 부모 회차는 레인이 끝날 때까지 전역 락을 쥐고 기다린다.
  launch_lanes
  exit 0
else
  # 심사는 GPT 상위 티어로 — 사용자 요청(claude 편중 완화). 기획안이 claude/codex/grok
  # 3사에서 나오므로 어차피 어느 모델이 심사해도 자기 안이 하나는 섞여 있다.
  codex_run "$T_JUDGE" "$LOG_DIR/judge.log" "$(prompt_file factory/prompts/15-judge.md)

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
# ⚠️ 아래 `$ASSET_IDS` 는 **일부러 따옴표를 뺐다** — 줄바꿈으로 이어 붙인 id 목록을
# IFS 워드분리로 도는 bash 관용구다. zsh 에서는 분리가 안 돼 루프가 1회만 돌고
# thumb/square/title 이 조용히 누락된다. 최상단 bash 가드가 이걸 막는다. 건드리지 마라.
for aid in $ASSET_IDS; do
  ASSET_JSON="$(jq -c --arg id "$aid" '.art_direction.assets_needed[]? | select(.id==$id)' "$WORK/chosen.json")"
  if [ -z "$ASSET_JSON" ] && [ "$aid" = "square" ]; then
    ASSET_JSON="{\"id\":\"square\",\"prompt\":\"$(jqv "$WORK/chosen.json" .one_liner) — 주인공을 중앙에 크게 클로즈업한 정사각 커버 아트\",\"size\":\"1024x1024\",\"transparent_bg\":false}"
  fi
  if [ -z "$ASSET_JSON" ] && [ "$aid" = "title" ]; then
    ASSET_JSON="{\"id\":\"title\",\"prompt\":\"$(jqv "$WORK/chosen.json" .one_liner) — 주인공이 역동적 포즈로 등장하는 세로형 타이틀 키 아트, 상단 1/3은 로고 여백\",\"size\":\"1024x1536\",\"transparent_bg\":false}"
  fi
  [ -n "$ASSET_JSON" ] || ASSET_JSON="{\"id\":\"$aid\",\"prompt\":\"$(jqv "$WORK/chosen.json" .one_liner) key art\",\"size\":\"1536x1024\"}"

  ART_PROMPT="$(prompt_file factory/prompts/20-art.md)

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
BUILD_PROMPT="$(prompt_file factory/prompts/30-build.md)

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
- \`public/g/$SLUG/meta.json\`  (slug=\"$SLUG\", grade=$UNIT_GRADE, semester=$UNIT_SEM, unit.id=\"$(jqv "$WORK/slot.json" .unit.id)\"$([ "$SCHOOL" = middle ] && echo ', school=\"middle\"'))

## 끝내기 전에 반드시
\`node factory/lib/qa.mjs $SLUG\` 를 돌려서 **치명적 결함 0건**을 확인해라. 실패하면 고치고 다시 돌려라."

CODEX_SANDBOX_ARGS="${BUILD_SANDBOX_ARGS:-}" stage_run "${BUILD_RUNNER:-grok_run}" "${BUILD_MODEL:-}" "$T_BUILD" "$LOG_DIR/build.log" "$BUILD_PROMPT"
BUILD_RC=$?

# 러너 바이너리는 살아 있는데 API 쪽이 죽는 경우(402 잔액 소진·인증 만료·쿼터)는
# resolve_runner 가 못 잡는다 — 2026-08-27~28 에 4회 연속으로 회차가 통째로 죽었다.
# 그래서 빌드 단계는 실행 **결과**를 보고 한 번 더 폴백한다: 비정상 종료·인프라 오류 출력·
# index.html 미생성이면 BUILD_FALLBACK_RUNNER/MODEL(기본 codex gpt-5.6-sol)로 1회 재시도.
BUILD_ERR="$(runner_infra_err "$LOG_DIR/build.log")"
if [ "$BUILD_RC" -ne 0 ] || [ -n "$BUILD_ERR" ] || [ ! -f "$ROOT/public/g/$SLUG/index.html" ]; then
  BUILD_FALLBACK_NOTE="빌드 러너($(resolve_runner "${BUILD_RUNNER:-codex_run}") / ${BUILD_MODEL:-기본}) 실패(rc=$BUILD_RC${BUILD_ERR:+, $BUILD_ERR}) → ${BUILD_FALLBACK_RUNNER:-codex_run} / ${BUILD_FALLBACK_MODEL:-$CODEX_MODEL_SMART} 로 1회 재시도"
  log "⚠️  $BUILD_FALLBACK_NOTE — 재시도 로그 build-fallback.log"
  CODEX_SANDBOX_ARGS="${BUILD_SANDBOX_ARGS:-}" stage_run "${BUILD_FALLBACK_RUNNER:-codex_run}" "${BUILD_FALLBACK_MODEL:-$CODEX_MODEL_SMART}" \
            "$T_BUILD" "$LOG_DIR/build-fallback.log" "$BUILD_PROMPT"
  BUILD_ERR2="$(runner_infra_err "$LOG_DIR/build-fallback.log")"
  [ -n "$BUILD_ERR2" ] && BUILD_FALLBACK_NOTE="$BUILD_FALLBACK_NOTE — 폴백도 인프라 실패($BUILD_ERR2)"
fi

if [ ! -f "$ROOT/public/g/$SLUG/index.html" ]; then
  BLOG="$LOG_DIR/build.log"
  [ -f "$LOG_DIR/build-fallback.log" ] && BLOG="$LOG_DIR/build-fallback.log"
  # 인프라 실패와 콘텐츠 실패를 장부에서 구분한다 — 러너가 죽어서 못 만든 회차를
  # 「모델이 게임을 못 만든다」로 집계하면 슬롯 정책·모델 정책이 엉뚱하게 흔들린다.
  if [ -n "$BUILD_ERR" ] || [ -n "${BUILD_ERR2:-}" ]; then
    record_failed "인프라 실패(빌드): ${BUILD_ERR:-}${BUILD_ERR2:+ / 폴백 $BUILD_ERR2}"
    die "빌드 러너 인프라 실패 — 폴백까지 실패했습니다 ($(tail -3 "$BLOG"))"
  fi
  record_failed "빌드 실패${BUILD_FALLBACK_NOTE:+ — 폴백도 실패}"
  die "게임 파일이 생성되지 않았습니다 — $(tail -5 "$BLOG")"
fi
fi

# ════════════════════════════════════════════════════════════════
# ── 첫 플레이 증거 캡처 (P0-2, 2026-09-06) ─────────────────────────
# 검수 7번 게이트(첫 플레이 이해도)는 지금까지 **프레임 없이** 판정돼 왔다 —
# 최근 9회의 전용 firstplay 프레임이 0/9 였는데도 이해도 판정이 나왔다(감사 §1-5).
# 그래서 캡처를 검수관의 선택이 아니라 **호스트 단계**로 올린다.
# 캡처에 실패하면 통과 처리하지 않고 「미검증」 상태로 검수에 전달한다.
FIRSTPLAY_STATUS="미실행"
FIRSTPLAY_FRAMES=0
run_firstplay() {
  local tag="$1"
  local out="$WORK/qa/$SLUG/firstplay"
  rm -rf "$out"; mkdir -p "$out"
  step "6.2 첫 플레이 증거 캡처 ($tag)"
  FIRSTPLAY_OUT="$out" FIRSTPLAY_PLAY_MS="${FIRSTPLAY_MS:-45000}" \
    run_timeout "${T_FIRSTPLAY:-300}" node factory/lib/firstplay/harness.mjs "$SLUG" \
    >"$LOG_DIR/firstplay-$tag.log" 2>&1
  local rc=$?
  FIRSTPLAY_FRAMES="$(find "$out" -name 'frame-*.png' 2>/dev/null | wc -l | tr -d ' ')"
  if [ "$FIRSTPLAY_FRAMES" -gt 0 ]; then
    FIRSTPLAY_STATUS="captured"
    log "첫 플레이 프레임 ${FIRSTPLAY_FRAMES}장 → $out/$SLUG/"
  else
    FIRSTPLAY_STATUS="unverified"
    log "⚠️  첫 플레이 캡처 실패(rc=$rc) — 검수에 「미검증」으로 전달한다: $(tail -2 "$LOG_DIR/firstplay-$tag.log" 2>/dev/null | tr '\n' ' ')"
  fi
  cp -r "$out" "$LOG_DIR/firstplay-$tag" 2>/dev/null
}

step "6. 자동 QA"
node factory/lib/qa.mjs "$SLUG" > "$LOG_DIR/qa-1.log" 2>&1
QA1=$?
cp -r "$WORK/qa/$SLUG" "$LOG_DIR/qa-1" 2>/dev/null
log "자동 QA 종료코드 $QA1"
tail -25 "$LOG_DIR/qa-1.log" >&2

run_firstplay 1

# ════════════════════════════════════════════════════════════════
# 수학 오류는 이 프로젝트에서 가장 치명적인 결함이라 종합 검수와 분리해
# 독립 에이전트에게 전수 검산만 시킨다. 두 눈이 따로 보게 하는 것이 요점.
MATHCHECK_PROMPT="$(prompt_file factory/prompts/35-mathcheck.md)

---
- slug: \`$SLUG\`
- 표본: \`factory/work/qa/$SLUG/report.json\` 의 \`problems_sample\`
- 단원: $(jqv "$WORK/slot.json" .unit.title) (${UNIT_GRADE}학년 ${UNIT_SEM}학기)"

run_mathcheck() {
  local tag="$1"
  rm -f "$WORK/mathcheck.json"
  # 게임을 만든 모델(claude)과 다른 회사 모델(GPT 상위 티어)로 검산한다 —
  # 같은 모델이 만들고 검산하면 같은 맹점을 공유한다.
  judge_run codex_run "$CODEX_MODEL_SMART" "$T_MATHCHECK" "$LOG_DIR/mathcheck-$tag.log" "$MATHCHECK_PROMPT"
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
REVIEW_PROMPT="$(prompt_file factory/prompts/40-review.md)

---
- slug: \`$SLUG\`
- 커트라인: ${GATE_SCORE}점
- 자동 QA 리포트: \`factory/work/qa/$SLUG/report.json\`
- 독립 수학 검산 결과: \`factory/work/mathcheck.json\` — 여기서 오류가 나왔다면 그대로 인정하고 반영해라
- 스크린샷: \`factory/work/qa/$SLUG/mobile.png\`, \`tablet.png\`, \`desktop.png\` — **Read 툴로 실제로 봐라**
- 첫 플레이 증거: 상태=**${FIRSTPLAY_STATUS}** (프레임 ${FIRSTPLAY_FRAMES}장)
  - 경로: \`factory/work/qa/$SLUG/firstplay/$SLUG/\` (\`frame-*.png\` + \`manifest.json\`)
  - 호스트가 이미 캡처했다. 네가 하네스를 다시 돌릴 필요는 없다 — **프레임을 Read 툴로 열어라.**
  - 상태가 \`unverified\` 면 프레임이 없다는 뜻이다. 그때는 \`firstplay.comprehensible\` 을
    \`null\`, \`frames_seen\` 을 빈 값으로 두고 **미검증으로 게시 보류**해라 (통과 처리 금지).
${USER_FEEDBACK:+
$USER_FEEDBACK
이 피드백을 채점에 직접 반영해라. 특히 지목된 문제가 이번 게임에도 있으면 must_fix 로 적어라.}"

judge_run "${REVIEW_RUNNER:-codex_run}" "${REVIEW_MODEL:-$CODEX_MODEL_SMART}" "$T_REVIEW" "$LOG_DIR/review-1.log" "$REVIEW_PROMPT"
cp "$WORK/review.json" "$LOG_DIR/review-1.json" 2>/dev/null

SCORE="$(jqv "$WORK/review.json" .score)"
PASSED="$(jqv "$WORK/review.json" .passed)"
REJECT="$(jqv "$WORK/review.json" .reject_immediately)"
log "1차 검수: ${SCORE:-?}점 / passed=${PASSED:-false} / reject=${REJECT:-false}"

# ════════════════════════════════════════════════════════════════
# 수정 루프: 미달 시 최대 MAX_FIX_ROUNDS회 "수정 → 재QA → 재첫플레이 → 재검산 → 재검수".
# MAX_FIX_ROUNDS 는 **비용 상한**이지 품질 보증이 아니다 — 다음 검사에 들어가려면
# 정상 종료·실제 변경(해시)·지적별 해결 증거가 있어야 한다(P0-1, 2026-09-06).
# 각 라운드의 검수 이력(review-N.json)을 다음 수정에 누적 주입해 같은 지적의 반복을 막는다.
FIX_ROUNDS=0
if [ "$PASSED" != "true" ] || [ "$QA1" -ne 0 ] || [ "${MATH_VERDICT:-unknown}" != "pass" ] || [ "$(open_high)" -gt 0 ]; then
  # 누적 검수 이력 — 전 라운드 점수와 must_fix 를 요약해 반환
  fix_history() {
    local r out=""
    for r in $(seq 1 "$1"); do
      [ -f "$LOG_DIR/review-$r.json" ] || continue
      out+="
### ${r}차 검수: $(jqv "$LOG_DIR/review-$r.json" .score)점
$(jq -r '.must_fix[]? | "- [\(.severity)] \(.issue)"' "$LOG_DIR/review-$r.json" 2>/dev/null)"
    done
    printf '%s' "$out"
  }
  # 수정 러너 1회 실행 + 결과 검사. $1=로그파일. 반환: 0=실제 수정됨 / 1=무효(무변경·실패)
  # 전역 FIX_ERR 에 인프라 오류 문자열을 남긴다.
  fix_attempt() {
    local logfile="$1" want="$2" model="$3" before after rc
    before="$(tree_hash "$ROOT/public/g/$SLUG")"
    CODEX_SANDBOX_ARGS="${BUILD_SANDBOX_ARGS:-}" stage_run "$want" "$model" "$T_FIX" "$logfile" "$FIX_PROMPT"
    rc=$?
    after="$(tree_hash "$ROOT/public/g/$SLUG")"
    FIX_ERR="$(runner_infra_err "$logfile")"
    if [ "$rc" -ne 0 ]; then log "⚠️  수정 러너 비정상 종료 (rc=$rc)"; return 1; fi
    if [ -n "$FIX_ERR" ]; then log "⚠️  수정 러너 인프라 오류: $FIX_ERR"; return 1; fi
    if [ "$before" = "$after" ]; then
      log "⚠️  수정 러너가 산출물을 하나도 바꾸지 않았다 (해시 $before) — 실패로 친다"
      FIX_ERR="${FIX_ERR:-산출물 무변경}"
      return 1
    fi
    return 0
  }

  while [ "$FIX_ROUNDS" -lt "$MAX_FIX_ROUNDS" ]; do
    FIX_ROUNDS=$((FIX_ROUNDS+1))
    step "8. 수정 (${FIX_ROUNDS}/${MAX_FIX_ROUNDS})"
    FIX_PROMPT="$(prompt_file factory/prompts/45-fix.md)

---
- slug: \`$SLUG\`
- 이것은 **${FIX_ROUNDS}차 수정**이다 (상한 ${MAX_FIX_ROUNDS}회 — 몇 차든 매 라운드가 마지막처럼 고쳐라)
- 검수 결과: \`factory/work/review.json\` (최신)
- 독립 수학 검산: \`factory/work/mathcheck.json\` — verdict 가 fail 이면 **이것부터** 고쳐라
- 자동 QA: \`factory/work/qa/$SLUG/report.json\`
- 누적 검수 이력 (이전 라운드 지적 — 같은 것을 다시 지적받지 않게 전부 반영해라):
$(fix_history $((FIX_ROUNDS-1)))"

    # P0-1: 빌드와 같은 결과 검사를 수정에도 건다.
    # rc≠0 / 402·인증·쿼터 / **산출물 무변경** 중 하나면 러너를 1회 대체하고,
    # 그래도 실패면 **인프라 실패**로 회차를 즉시 중단한다 — 재QA·재검산·재검수로
    # 넘어가지 않는다. 안 고친 코드를 다시 채점하는 데 최근 20회가 8.77시간을 태웠다.
    if ! fix_attempt "$LOG_DIR/fix-$FIX_ROUNDS.log" "${FIX_RUNNER:-codex_smart_run}" "${FIX_MODEL:-}"; then
      FIX_ERR1="$FIX_ERR"
      log "⚠️  수정 러너 대체 1회: ${FIX_RUNNER:-codex_smart_run} → ${FIX_FALLBACK_RUNNER:-claude_run}"
      if ! fix_attempt "$LOG_DIR/fix-$FIX_ROUNDS-fallback.log" \
             "${FIX_FALLBACK_RUNNER:-claude_run}" "${FIX_FALLBACK_MODEL:-$CLAUDE_MODEL_SMART}"; then
        record_failed "인프라 실패(수정 ${FIX_ROUNDS}차): ${FIX_ERR1:-실패} / 대체 ${FIX_FALLBACK_RUNNER:-claude_run}: ${FIX_ERR:-실패}"
        finish "실패" "수정 러너 인프라 실패 — ${FIX_ERR1:-실패} / 대체도 ${FIX_ERR:-실패}. 게임은 \`public/g/$SLUG/\` 에 그대로 두었다. 러너를 복구한 뒤 \`RESUME_FROM=qa bash factory/run.sh\` 로 이어서 돌려라."
        exit 1
      fi
      BUILD_FALLBACK_NOTE="${BUILD_FALLBACK_NOTE:+$BUILD_FALLBACK_NOTE / }수정 러너 대체(${FIX_ERR1:-실패}) → ${FIX_FALLBACK_RUNNER:-claude_run}"
    fi

    node factory/lib/qa.mjs "$SLUG" > "$LOG_DIR/qa-$((FIX_ROUNDS+1)).log" 2>&1
    QA1=$?
    cp -r "$WORK/qa/$SLUG" "$LOG_DIR/qa-$((FIX_ROUNDS+1))" 2>/dev/null
    log "재 QA(${FIX_ROUNDS}차) 종료코드 $QA1"

    run_firstplay $((FIX_ROUNDS+1))

    step "8.5 수학 재검산 (${FIX_ROUNDS}/${MAX_FIX_ROUNDS})"
    run_mathcheck $((FIX_ROUNDS+1))

    step "9. 재검수 (${FIX_ROUNDS}/${MAX_FIX_ROUNDS})"
    rm -f "$WORK/review.json"
    judge_run "${REVIEW_RUNNER:-codex_run}" "${REVIEW_MODEL:-$CODEX_MODEL_SMART}" "$T_REVIEW" "$LOG_DIR/review-$((FIX_ROUNDS+1)).log" "$REVIEW_PROMPT

이것은 **${FIX_ROUNDS}차 재검수**다 (수정 상한 ${MAX_FIX_ROUNDS}회 중 ${FIX_ROUNDS}차 시도 후).
\`factory/work/fix.json\` 에 수정 내역이 있다. 수정이 실제로 반영됐는지 확인해라.
호스트가 수정 후 산출물이 **실제로 바뀐 것을 해시로 확인**했다. 그래도 지적별 해결 증거는 네가 직접 봐라.
첫 플레이 증거(재캡처): 상태=**${FIRSTPLAY_STATUS}** (프레임 ${FIRSTPLAY_FRAMES}장)
봐주지 마라 — 이전 점수와 무관하게 기준대로 채점해라. 여전히 미달이면 그렇게 판정해라."
    cp "$WORK/review.json" "$LOG_DIR/review-$((FIX_ROUNDS+1)).json" 2>/dev/null

    SCORE="$(jqv "$WORK/review.json" .score)"
    PASSED="$(jqv "$WORK/review.json" .passed)"
    REJECT="$(jqv "$WORK/review.json" .reject_immediately)"
    log "${FIX_ROUNDS}차 재검수: ${SCORE:-?}점 / passed=${PASSED:-false} / reject=${REJECT:-false}"

    if [ "$PASSED" = "true" ] && [ "$QA1" -eq 0 ] && [ "${MATH_VERDICT:-unknown}" = "pass" ] \
       && [ "$(open_high)" -eq 0 ]; then
      log "✅ ${FIX_ROUNDS}차 수정으로 게이트 통과"
      break
    fi
  done
fi

VERDICT="$(jqv "$WORK/review.json" .verdict)"

# ════════════════════════════════════════════════════════════════
# 게시 게이트 (P0-3, 2026-09-06):
#   ① 검수 passed  ② 즉시폐기 아님  ③ 자동 QA 치명 0
#   ④ **독립 검산이 명시적으로 pass** — `unknown`(검산 산출물 누락)은 통과가 아니다.
#      「검사하지 못했다」를 「문제 없다」로 읽어 온 것이 감사 §1-5 의 지적이다.
#   ⑤ **미해결 high 0** — 검수가 must_fix high 를 남긴 채 총점만으로 통과시키지 않는다.
GATE_BLOCK=""
[ "$PASSED" != "true" ]                  && GATE_BLOCK="검수 미통과(passed=${PASSED:-false})"
[ "$REJECT" = "true" ]                   && GATE_BLOCK="${GATE_BLOCK:+$GATE_BLOCK · }즉시 폐기 판정"
[ "$QA1" -ne 0 ]                         && GATE_BLOCK="${GATE_BLOCK:+$GATE_BLOCK · }자동 QA 치명적 결함"
[ "${MATH_VERDICT:-unknown}" != "pass" ] && GATE_BLOCK="${GATE_BLOCK:+$GATE_BLOCK · }독립 검산 verdict=${MATH_VERDICT:-unknown} (명시적 pass 아님)"
OPEN_HIGH="$(open_high)"
[ "$OPEN_HIGH" -gt 0 ]                   && GATE_BLOCK="${GATE_BLOCK:+$GATE_BLOCK · }미해결 must_fix high ${OPEN_HIGH}건"
if [ -n "$GATE_BLOCK" ]; then
  log "🚧 게시 차단: $GATE_BLOCK"
  step "폐기"
  ARCHIVE="$ROOT/factory/state/rejected/$RUN_ID-$SLUG"
  mkdir -p "$(dirname "$ARCHIVE")"
  acquire_lock publish
  mv "$ROOT/public/g/$SLUG" "$ARCHIVE" 2>/dev/null
  record_failed "$GATE_BLOCK"
  release_lock publish
  finish "폐기" "${VERDICT:-품질 게이트 미달} — 차단 사유: $GATE_BLOCK"
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
# 병렬 레인(2026-10-03): queue.json·catalog·허브·git·배포는 공유 자원이라 게시~배포 전체를 한 레인씩 돈다.
acquire_lock publish
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
  # 병렬 레인이 있으므로 git add -A 금지 — 다른 레인이 만들고 있는 public/g/<slug> 반쪽짜리가 섞인다.
  # 이 게임·허브·장부·폐기 보관소·레퍼런스만 담는다.
  git add -- "public/g/$SLUG" public/index.html public/catalog.json factory/state references \
    $( [ -d "factory/unity-src/$SLUG" ] && echo "factory/unity-src/$SLUG" ) >/dev/null 2>&1
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
release_lock publish

# ════════════════════════════════════════════════════════════════
# 게임 게시가 끝난 뒤에만 시도한다 — 이 게임의 성공 여부와 완전히 무관한 부가
# 작업이라 실패해도 die() 로 전체를 죽이지 않는다. 게임 N개마다(기본 10개)
# 한 번, 새 카테고리에서 레퍼런스 후보를 찾아 references/pending/ 에 쌓아둔다.
# 바로 game-references.json 에 섞이지 않는다 — 사람이 검토 후
# merge-references.mjs 로 승인해야 실제 기획에 반영된다.
if [ "${MGF_LANE_NO:-1}" = "1" ] && node factory/lib/scout-references.mjs check >"$LOG_DIR/scout.log" 2>&1; then
  step "12. 레퍼런스 스카우트"
  node factory/lib/scout-references.mjs prepare >>"$LOG_DIR/scout.log" 2>&1
  SCOUT_PROMPT="$(prompt_file factory/prompts/50-reference-scout.md)

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
