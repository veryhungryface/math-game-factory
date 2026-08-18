#!/usr/bin/env bash
#
# 초등 수학 게임 공장 — 에르메스 크론 진입점.
# 2시간마다 실행되어 게임 1개를 기획·제작·검수·배포하고, 이 스크립트의 stdout 이
# 그대로 사용자에게 전달된다 (hermes cron --no-agent).
#
# 게임 코드/파이프라인 본체: /Users/sitpo/math-game-factory
#
set -uo pipefail

export PATH="/opt/homebrew/bin:/usr/local/bin:$HOME/.local/nodejs/bin:$HOME/.local/bin:$HOME/.nvm/versions/node/current/bin:/usr/bin:/bin:/usr/sbin:/sbin:$PATH"
ROOT="$HOME/math-game-factory"

[ -d "$ROOT" ] || { echo "💀 공장 디렉터리가 없습니다: $ROOT"; exit 1; }
cd "$ROOT" || exit 1

# node 를 못 찾는 환경(launchd 등) 대비
if ! command -v node >/dev/null 2>&1; then
  for c in "$HOME"/.local/nodejs/bin/node /opt/homebrew/bin/node /usr/local/bin/node "$HOME/.local/bin/node"; do
    [ -x "$c" ] && export PATH="$(dirname "$c"):$PATH" && break
  done
fi
for c in node claude codex jq git; do
  command -v "$c" >/dev/null 2>&1 || { echo "💀 \`$c\` 를 찾을 수 없습니다 (PATH=$PATH)"; exit 1; }
done

# 보고는 크론이 stdout 으로 전달하므로 run.sh 자체 전송은 끈다.
export REPORT=0
export DEPLOY=1

REPORT_FILE=""
bash factory/run.sh >/tmp/mgf-stdout.$$ 2>/tmp/mgf-stderr.$$
RC=$?

# run.sh 는 마지막에 보고서를 stdout 으로 뱉고 logs/latest-report.md 에도 남긴다.
if [ -s /tmp/mgf-stdout.$$ ]; then
  cat /tmp/mgf-stdout.$$
elif [ -f "$ROOT/logs/latest-report.md" ]; then
  cat "$ROOT/logs/latest-report.md"
else
  echo "💀 생산 실패 (종료코드 $RC)"
  echo '```'
  tail -20 /tmp/mgf-stderr.$$
  echo '```'
fi

# 게시된 경우 썸네일을 이어서 보낸다.
SLUG="$(grep -m1 '^\*\*슬러그\*\*' "$ROOT/logs/latest-report.md" 2>/dev/null | sed 's/.*`\(.*\)`.*/\1/')"
if [ -n "$SLUG" ] && [ -f "$ROOT/public/g/$SLUG/thumb.png" ] && grep -q '새 게임이 나왔습니다' "$ROOT/logs/latest-report.md" 2>/dev/null; then
  TARGET="$(grep -m1 '^export REPORT_TARGET=' "$ROOT/factory/config.sh" | sed 's/.*:-\([^}]*\)}.*/\1/')"
  IMG="$ROOT/public/g/$SLUG/square.png"
  [ -f "$IMG" ] || IMG="$ROOT/public/g/$SLUG/thumb.png"
  [ -n "$TARGET" ] && hermes send --to "$TARGET" "MEDIA:$IMG" --quiet 2>/dev/null || true
fi

rm -f /tmp/mgf-stdout.$$ /tmp/mgf-stderr.$$
exit 0
