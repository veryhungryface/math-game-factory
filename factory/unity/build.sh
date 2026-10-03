#!/usr/bin/env bash
# =============================================================================
# build.sh — Unity 게임 1개를 WebGL 로 빌드해 public/g/<slug>/ 에 배치한다
# =============================================================================
#
#   bash factory/unity/build.sh <slug> [game_src_dir]
#
#   game_src_dir 기본값: factory/unity-src/<slug>/   (게임별 Unity 소스 정본 — git 에 남는다)
#     Scripts/*.cs      namespace Mgf.<Slug>. IMgfGame 을 구현한 MonoBehaviour 가 정확히 1개(부트스트랩)
#     Resources/…       텍스처·FBX·오디오 (Resources.Load 경로 = Resources/ 아래 상대경로, 확장자 없이)
#     Editor/*.cs       (선택) 임포트 설정 등 에디터 스크립트
#     mgf.json          (선택) {"title":"…","bg":"#RRGGBB","scene":"Assets/Game/Scenes/Main.unity"}
#     ArtSource/        (선택) 원화·프롬프트 기록 — Unity 로 복사하지 않는다
#
# 하는 일:
#   1) 워크스페이스(~/UnityProjects/MGF-Workspace) 점검 — 없으면 setup-workspace.sh 로 만든다
#   2) <game_src_dir> → 워크스페이스 Assets/Game/ 교체 동기화(.meta 는 보존)
#   3) unity build … --execute-method MgfBuild.Perform (배치). 에디터가 열려 있으면 알리고 실패
#   4) 컴파일 에러는 `error CS…` 줄로 요약해 stdout 에 찍고 exit 2
#   5) 성공하면 public/g/<slug>/ 의 index.html·Build/·TemplateData/ 교체
#      (meta.json·thumb.png·square.png·assets/ 는 보존) + 파일 크기 요약
#
# 종료 코드: 0 성공 · 1 인자/환경 · 2 컴파일 에러 · 3 빌드 실패 · 4 에디터가 열려 있음
# 정본 문서: docs/unity-track.md
set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
WS="${MGF_UNITY_WORKSPACE:-$HOME/UnityProjects/MGF-Workspace}"
UNITY_VERSION="${MGF_UNITY_VERSION:-6000.3.24f1}"
TIMEOUT="${MGF_UNITY_BUILD_TIMEOUT:-1800}"

SLUG="${1:-}"
[ -n "$SLUG" ] || { echo "사용법: bash factory/unity/build.sh <slug> [game_src_dir]" >&2; exit 1; }
[[ "$SLUG" =~ ^[a-z0-9][a-z0-9-]*$ ]] || { echo "[build] slug 는 소문자·숫자·하이픈만: $SLUG" >&2; exit 1; }
SRC="${2:-$ROOT/factory/unity-src/$SLUG}"
SRC="$(cd "$SRC" 2>/dev/null && pwd)" || { echo "[build] 게임 소스 폴더 없음: ${2:-factory/unity-src/$SLUG}" >&2; exit 1; }
DEST="$ROOT/public/g/$SLUG"

log() { printf '[build] %s\n' "$*"; }
die() { local c="$1"; shift; printf '[build] 실패: %s\n' "$*" >&2; exit "$c"; }
human() { awk -v b="$1" 'BEGIN{ if (b>=1048576) printf "%.1fMB", b/1048576; else printf "%.0fKB", b/1024 }'; }

# ── 0. 소스 점검 ────────────────────────────────────────────────────────────
CS_COUNT=$(find "$SRC" -name '*.cs' | wc -l | tr -d ' ')
[ "$CS_COUNT" -gt 0 ] || die 1 "$SRC 에 .cs 파일이 없다"
BAD_NS=$(grep -L -E '^\s*namespace\s+Mgf\.' $(find "$SRC" -name '*.cs' -not -path '*/Editor/*') 2>/dev/null || true)
[ -z "$BAD_NS" ] || log "경고: namespace Mgf.<Slug> 가 없는 파일 — $(echo $BAD_NS | tr '\n' ' ')"

# mgf.json (선택)
read_cfg() { python3 -c "import json,sys;d=json.load(open(sys.argv[1]));print(d.get(sys.argv[2],''))" "$SRC/mgf.json" "$1" 2>/dev/null || true; }
TITLE="" BG="" SCENE=""
if [ -f "$SRC/mgf.json" ]; then TITLE="$(read_cfg title)"; BG="$(read_cfg bg)"; SCENE="$(read_cfg scene)"; fi
if [ -z "$TITLE" ] && [ -f "$DEST/meta.json" ]; then
  TITLE="$(python3 -c "import json;print(json.load(open('$DEST/meta.json')).get('title',''))" 2>/dev/null || true)"
fi
[ -n "$TITLE" ] || TITLE="$SLUG"
[ -n "$BG" ] || BG="#1B2440"

# ── 1. 워크스페이스 ─────────────────────────────────────────────────────────
# 3안 병렬 생산(2026-10-03): 워크스페이스가 하나라 Unity 배치 빌드는 한 번에 하나만 돈다.
# 다른 레인이 빌드 중이면 기다린다(죽은 pid 의 잠금은 회수). run.sh·machine-lock.mjs 와 같은 규약.
LOCKD="$ROOT/factory/state/unity.lock.d"
_waited=0
while ! mkdir "$LOCKD" 2>/dev/null; do
  _p="$(cat "$LOCKD/pid" 2>/dev/null)"
  if [ -n "$_p" ] && ! kill -0 "$_p" 2>/dev/null; then rm -rf "$LOCKD"; continue; fi
  [ "$_waited" -eq 0 ] && echo "[build] 다른 레인이 Unity 빌드 중(pid ${_p:-?}) — 대기" >&2
  sleep 5; _waited=$((_waited+5))
  [ "$_waited" -ge 2400 ] && { echo "[build] Unity 잠금 40분 대기 초과 — 실패" >&2; exit 4; }
done
echo $$ > "$LOCKD/pid"
trap 'rm -rf "$LOCKD"' EXIT

editor_open() { ps -axo pid=,command= | grep -i "Unity.app/Contents/MacOS/Unity" | grep -v grep | grep -iF -- "$WS" || true; }
OPEN="$(editor_open)"
if [ -n "$OPEN" ]; then
  echo "[build] Unity 가 워크스페이스를 이미 열고 있다 — 에디터(또는 이전 빌드)를 닫고 다시 실행해라:" >&2
  echo "$OPEN" >&2
  exit 4
fi
if [ ! -f "$WS/Assets/TextMesh Pro/Resources/TMP Settings.asset" ]; then
  log "워크스페이스가 없거나 불완전하다 → setup-workspace.sh"
  bash "$HERE/setup-workspace.sh" || die 1 "워크스페이스 설정 실패"
else
  bash "$HERE/setup-workspace.sh" --sync || die 1 "킷 동기화 실패"
fi

# ── 2. 게임 소스 동기화(.meta 보존, 나머지는 소스와 똑같이) ────────────────
mkdir -p "$WS/Assets/Game"
# ArtSource/(생성 원화·프롬프트 기록)와 mgf.json 은 Unity 에 넣지 않는다 — 텍스처로 쓸 것은 Resources/ 에 복사해 둬라.
rsync -a --delete --delete-excluded --filter='P *.meta' --exclude='.DS_Store' --exclude='ArtSource/' --exclude='mgf.json' --exclude='*.md' "$SRC/" "$WS/Assets/Game/"
# 소스에서 사라진 파일의 고아 .meta 정리(Unity 경고 방지)
find "$WS/Assets/Game" -name '*.meta' | while read -r m; do [ -e "${m%.meta}" ] || rm -f "$m"; done
log "소스 동기화: $SRC → Assets/Game (${CS_COUNT}개 .cs)"

# ── 3. 배치 빌드 ────────────────────────────────────────────────────────────
mkdir -p "$WS/Logs" "$WS/Builds"
STAMP=$(date +%Y%m%d-%H%M%S)
LOG="$WS/Logs/build-$SLUG-$STAMP.log"
OUT="$WS/Builds/$SLUG"
ARGS="-mgfOut $OUT -mgfName $SLUG -mgfBg $BG -mgfCompression ${MGF_UNITY_COMPRESSION:-gzip} -mgfStrip ${MGF_UNITY_STRIP:-Medium}"
[ -n "${MGF_UNITY_OPT:-}" ] && ARGS="$ARGS -mgfOpt $MGF_UNITY_OPT"
[ -n "$SCENE" ] && ARGS="$ARGS -mgfScene $SCENE"
source "$HOME/.unity/env" 2>/dev/null || true
command -v unity >/dev/null || die 1 "unity CLI 가 없다 (~/.unity/env)"

# 제목은 공백·한글이 있어 --args 셸 분할에 안 맞는다 → 파일로 넘긴다
printf '%s' "$TITLE" > "$WS/Logs/mgf-title.txt"
log "빌드 시작: ${SLUG}「${TITLE}」 (로그: $LOG)"
T0=$(date +%s)
unity build "$WS" --target WebGL --execute-method MgfBuild.Perform \
  --editor-version "$UNITY_VERSION" --architecture arm64 \
  --log-file "$LOG" --no-tail --no-provenance --allow-dirty-build --timeout "$TIMEOUT" \
  --args "$ARGS -mgfTitleFile $WS/Logs/mgf-title.txt" --no-banner --non-interactive >"$WS/Logs/build-$SLUG-cli.txt" 2>&1
RC=$?
T1=$(date +%s)
SECS=$((T1 - T0))

# ── 4. 결과 판정 ────────────────────────────────────────────────────────────
CS_ERR=$(grep -E "error CS[0-9]+" "$LOG" 2>/dev/null | sed -E 's#^.*/Assets/#Assets/#' | sort -u)
if [ -n "$CS_ERR" ]; then
  echo "[build] 컴파일 에러 $(echo "$CS_ERR" | wc -l | tr -d ' ')건 (Assets/Game ↔ $SRC 같은 상대경로):"
  echo "$CS_ERR" | head -40
  echo "[build] 로그: $LOG"
  exit 2
fi
RESULT=$(grep -o 'MGF_BUILD_RESULT .*' "$LOG" 2>/dev/null | tail -1 | sed 's/^MGF_BUILD_RESULT //')
if [ $RC -ne 0 ] || [ -z "$RESULT" ] || [ ! -f "$OUT/index.html" ]; then
  echo "[build] 빌드 실패 (unity rc=$RC, ${SECS}초)"
  [ -n "$RESULT" ] && echo "[build] 결과: $RESULT"
  grep -E "\[MGF\]|Exception|Error building|BuildFailedException|error:|Error:|Shader error|Aborting batchmode" "$LOG" 2>/dev/null \
    | grep -v "^\[Licensing" | sort -u | head -25
  tail -5 "$WS/Logs/build-$SLUG-cli.txt" 2>/dev/null
  echo "[build] 로그: $LOG"
  exit 3
fi

# ── 5. 배치 ─────────────────────────────────────────────────────────────────
mkdir -p "$DEST"
rm -rf "$DEST/Build" "$DEST/TemplateData" "$DEST/StreamingAssets" "$DEST/index.html"
cp "$OUT/index.html" "$DEST/index.html"
cp -R "$OUT/Build" "$DEST/Build"
[ -d "$OUT/TemplateData" ] && cp -R "$OUT/TemplateData" "$DEST/TemplateData"
[ -d "$OUT/StreamingAssets" ] && cp -R "$OUT/StreamingAssets" "$DEST/StreamingAssets"

echo "[build] 성공: $SLUG · ${SECS}초 · $(printf '%s' "$RESULT" | python3 -c 'import json,sys;d=json.load(sys.stdin);print("bootstrap="+str(d.get("bootstrap")),"warnings="+str(d.get("warnings")))' 2>/dev/null)"
TOTAL=0
while IFS= read -r f; do
  b=$(stat -f %z "$f"); TOTAL=$((TOTAL + b))
  printf '  %8s  %s\n' "$(human "$b")" "${f#$DEST/}"
done < <(find "$DEST/index.html" "$DEST/Build" -type f | sort)
echo "  -------- "
echo "  $(human $TOTAL)  Unity 산출물 합계 → $DEST"
for k in meta.json thumb.png square.png; do [ -f "$DEST/$k" ] || echo "[build] 주의: $DEST/$k 없음 (게시 전 필요)"; done
exit 0
