#!/usr/bin/env bash
#
# 초등 수학 게임 공장 — 에르메스 크론 진입점.
#
# 중요: 이 스크립트는 파이프라인이 끝날 때까지 기다리지 않는다. 에르메스 크론은
# --no-agent 스크립트를 cron.script_timeout_seconds(기본 600초=10분) 안에 강제
# 종료시키는데, 실제 생산 파이프라인은 30~50분 걸린다. 예전 버전은 여기서
# `bash factory/run.sh` 를 동기 실행하다가 10분에 잘려서 보고가 통째로 유실됐다
# (2026-08-18 5회차에서 실제로 발생 — 게임은 백그라운드 고아 프로세스로 계속
# 만들어졌지만 보고 채널이 죽어서 알림이 안 갔다).
#
# 그래서 여기서는 파이프라인을 백그라운드로 던지고 즉시 끝난다. 실제 결과 보고는
# factory/run.sh 자신이 REPORT=1 로 끝났을 때 `hermes send` 를 직접 호출해서
# 보낸다 — 이 크론 잡 프로세스의 생존 여부와 완전히 무관하다.
#
# 게임 코드/파이프라인 본체: /Users/sitpo/math-game-factory
#
set -uo pipefail

export PATH="/opt/homebrew/bin:/usr/local/bin:$HOME/.local/nodejs/bin:$HOME/.local/bin:$HOME/.nvm/versions/node/current/bin:/usr/bin:/bin:/usr/sbin:/sbin:$PATH"
ROOT="$HOME/math-game-factory"

[ -d "$ROOT" ] || { echo "💀 공장 디렉터리가 없습니다: $ROOT"; exit 1; }
cd "$ROOT" || exit 1

if ! command -v node >/dev/null 2>&1; then
  for c in "$HOME"/.local/nodejs/bin/node /opt/homebrew/bin/node /usr/local/bin/node "$HOME/.local/bin/node"; do
    [ -x "$c" ] && export PATH="$(dirname "$c"):$PATH" && break
  done
fi
for c in node claude codex jq git hermes; do
  command -v "$c" >/dev/null 2>&1 || { echo "💀 \`$c\` 를 찾을 수 없습니다 (PATH=$PATH)"; exit 1; }
done

# 이미 돌고 있는 사이클이 있으면 새로 띄우지 않는다 (run.sh 자체 락과 별개로,
# 여기서 먼저 걸러야 중복 백그라운드 프로세스가 안 생긴다).
if [ -f "$ROOT/factory/state/run.lock" ]; then
  LOCK_PID="$(cat "$ROOT/factory/state/run.lock" 2>/dev/null)"
  if [ -n "$LOCK_PID" ] && kill -0 "$LOCK_PID" 2>/dev/null; then
    echo "⏭ 이전 사이클(pid $LOCK_PID)이 아직 실행 중입니다. 이번 회차는 건너뜁니다."
    exit 0
  fi
fi

export REPORT=1     # run.sh 가 직접 hermes send 로 보고한다
export DEPLOY=1

LAUNCH_LOG="$ROOT/logs/cron-launch-$(date +%Y%m%d-%H%M%S).log"
nohup bash factory/run.sh >"$LAUNCH_LOG" 2>&1 &
disown

# 여기서 stdout 에 뭔가 찍으면 에르메스 크론이 "Cronjob Response: ... To stop or
# manage this job..." 틀로 감싸서 매번 디스코드에 별도 메시지로 올린다. 실제
# 결과 보고는 어차피 factory/run.sh 가 끝나고 나서 직접 hermes send 로 (이 틀 없이
# 깔끔하게) 보내므로, 여기서는 stderr 로만 남기고 stdout 은 비운다 — 시작 알림
# 중복을 없애기 위해서다. 로그는 $LAUNCH_LOG 에 그대로 남는다.
echo "🏭 생산 시작 (백그라운드 로그: $LAUNCH_LOG)" >&2
exit 0
