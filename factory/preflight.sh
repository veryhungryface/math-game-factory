#!/usr/bin/env bash
#
# 공장 사전점검. run.sh 를 고쳤으면 반드시 이걸 돌려라.
#
#   bash factory/preflight.sh
#
# 08:33 크론이 "교육과정 파일이 없습니다"로 죽은 적이 있다. 원인은 스냅샷 재실행에서
# ROOT 가 /tmp 로 잡힌 것이었는데, 문법 검사(bash -n)로는 절대 잡히지 않는 종류였다.
# 그래서 "실제로 실행해 봐야만 드러나는 것들"을 여기서 미리 확인한다.

set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT" || exit 1

FAIL=0
ok()   { printf '  ✅ %s\n' "$*"; }
bad()  { printf '  ❌ %s\n' "$*"; FAIL=$((FAIL+1)); }
warn() { printf '  ⚠️  %s\n' "$*"; }

echo "━━━ 수학 게임 공장 사전점검 ━━━"
echo "ROOT=$ROOT"
echo

echo "[1] 스크립트"
bash -n factory/run.sh 2>/dev/null && ok "run.sh 문법" || bad "run.sh 문법 오류"

# ROOT 가 스냅샷 재실행을 거쳐도 저장소를 가리키는지 — 실제로 재실행해 본다.
SNAP_ROOT="$(MGF_SNAPSHOT=1 MGF_ROOT="$ROOT" bash -c '
  ROOT="${MGF_ROOT:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"; echo "$ROOT"')"
[ "$SNAP_ROOT" = "$ROOT" ] && ok "스냅샷 재실행 후 ROOT 유지" || bad "스냅샷에서 ROOT 가 $SNAP_ROOT 로 바뀜"

echo
echo "[2] 데이터 파일"
for f in curriculum/2022-elementary-math.json references/game-references.json; do
  if [ -f "$f" ]; then
    jq -e . "$f" >/dev/null 2>&1 && ok "$f ($(jq -r 'if .units then "단원 \(.units|length), 성취기준 \(.standards|length)" else "메커닉 \(.mechanics|length), 사이트 \(.sites|length)" end' "$f"))" \
      || bad "$f — JSON 파싱 실패"
  else
    bad "$f 없음"
  fi
done
[ -f references/game-references.md ] && ok "references/game-references.md" || warn "references/game-references.md 없음 (기획 프롬프트가 참조함) — node factory/lib/build-references-doc.mjs"
[ -f public/vendor/three.module.js ] && [ -f public/vendor/three.core.js ] && ok "three.js vendor" || bad "public/vendor/three.{module,core}.js 없음"

echo
echo "[3] 실행 도구"
for c in node jq git claude codex vercel gh hermes; do
  command -v "$c" >/dev/null 2>&1 && ok "$c ($(command -v "$c"))" || bad "$c 없음 — cron 환경 PATH 확인"
done

CHROME="$(ls -d "$HOME"/.cache/puppeteer/chrome/*/chrome-mac-arm64/"Google Chrome for Testing.app"/Contents/MacOS/"Google Chrome for Testing" 2>/dev/null | tail -1)"
[ -n "$CHROME" ] && ok "헤드리스 Chrome (QA용)" || bad "puppeteer Chrome 없음 — npx puppeteer browsers install chrome"
[ -d node_modules/puppeteer ] && ok "puppeteer 설치됨" || bad "npm install 필요"

echo
echo "[4] 파이프라인 동작"
SLOT="$(node factory/lib/pick-slot.mjs --focus "${FOCUS:-5-2,6-2}" 2>&1)"
if echo "$SLOT" | jq -e .unit.id >/dev/null 2>&1; then
  ok "슬롯 선택 → $(echo "$SLOT" | jq -r '"\(.unit.grade)-\(.unit.semester) \(.unit.order)단원 \(.unit.title)"')"
else
  bad "슬롯 선택 실패: $(echo "$SLOT" | tail -2)"
fi
node factory/lib/build-index.mjs >/dev/null 2>&1 && ok "허브 빌드" || bad "허브 빌드 실패"

echo
echo "[5] 외부 연결"
vercel whoami >/dev/null 2>&1 && ok "Vercel 로그인 ($(vercel whoami 2>/dev/null | tail -1))" || bad "Vercel 미인증"
gh auth status >/dev/null 2>&1 && ok "GitHub 인증" || bad "GitHub 미인증"
TARGET="$(grep -m1 '^export REPORT_TARGET=' factory/config.sh | sed 's/.*:-\([^}]*\)}.*/\1/')"
[ -n "$TARGET" ] && ok "보고 대상: $TARGET" || bad "REPORT_TARGET 파싱 실패"

echo
echo "[6] 상태"
if [ -f factory/state/run.lock ]; then
  LP="$(cat factory/state/run.lock)"
  if kill -0 "$LP" 2>/dev/null; then warn "사이클 실행 중 (pid $LP) — 지금 크론이 돌면 건너뛴다"
  else bad "고아 락 발견 (pid $LP 없음) — rm factory/state/run.lock"; fi
else
  ok "락 없음"
fi
echo "  ℹ️  게시 $(jq -r '.produced|length' factory/state/queue.json 2>/dev/null || echo 0)개 / 폐기 $(jq -r '.failed|length' factory/state/queue.json 2>/dev/null || echo 0)개"

echo
if [ "$FAIL" -eq 0 ]; then echo "━━━ 이상 없음 ━━━"; exit 0
else echo "━━━ 문제 ${FAIL}건 — 고치기 전에는 크론을 믿지 마라 ━━━"; exit 1; fi
