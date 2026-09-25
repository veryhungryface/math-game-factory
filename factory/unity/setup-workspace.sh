#!/usr/bin/env bash
# =============================================================================
# setup-workspace.sh — 공용 Unity 워크스페이스를 만들거나 점검한다 (멱등)
# =============================================================================
#
#   bash factory/unity/setup-workspace.sh            # 없으면 생성 + 킷 동기화 + 설정
#   bash factory/unity/setup-workspace.sh --sync     # 킷 파일만 동기화(Unity 실행 없음, build.sh 가 매번 부름)
#
# 워크스페이스: $MGF_UNITY_WORKSPACE (기본 ~/UnityProjects/MGF-Workspace)
#   - 게임 하나만 Assets/Game/ 에 들어간다(build.sh 가 교체). Library 를 따뜻하게 유지해 빌드를 빠르게 한다.
#   - Assets/MgfKit, Assets/WebGLTemplates 는 factory/unity/kit 의 사본 — 워크스페이스에서 고치지 마라.
#   - 내장 렌더 파이프라인 · 선형 색공간 · WebGL2 · 구 Input Manager · uGUI+TextMeshPro.
# 정본 문서: docs/unity-track.md
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
KIT="$HERE/kit"
WS="${MGF_UNITY_WORKSPACE:-$HOME/UnityProjects/MGF-Workspace}"
UNITY_VERSION="${MGF_UNITY_VERSION:-6000.3.24f1}"
UNITY_APP="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app"
MODE="${1:-full}"

log() { printf '[setup] %s\n' "$*"; }
die() { printf '[setup] 오류: %s\n' "$*" >&2; exit 1; }

sync_kit() {
  mkdir -p "$WS/Assets/MgfKit" "$WS/Assets/WebGLTemplates" "$WS/Assets/Game"
  # .meta 는 워크스페이스가 만든 것을 보존(P = protect) — GUID 가 흔들리면 불필요한 재임포트가 생긴다.
  rsync -a --delete --filter='P *.meta' "$KIT/MgfKit/" "$WS/Assets/MgfKit/"
  rsync -a --delete --filter='P *.meta' "$KIT/WebGLTemplates/" "$WS/Assets/WebGLTemplates/"
}

editor_open() {
  # GUI 에디터(허브가 띄운 것)는 -projectpath 인자를 달고 있다. 배치 프로세스도 같이 잡힌다.
  ps -axo pid=,command= | grep -i "Unity.app/Contents/MacOS/Unity" | grep -v grep | grep -iF -- "$WS" || true
}

if [ "$MODE" = "--sync" ]; then
  [ -d "$WS/ProjectSettings" ] || die "워크스페이스가 없다: $WS — 먼저 인자 없이 실행해라"
  sync_kit
  exit 0
fi

[ -d "$UNITY_APP" ] || die "Unity $UNITY_VERSION 이 없다: $UNITY_APP"
[ -d "$UNITY_APP/../PlaybackEngines/WebGLSupport" ] || die "Unity $UNITY_VERSION 에 Web 모듈이 없다"
if [ -n "$(editor_open)" ]; then
  die "Unity 에디터가 워크스페이스를 열고 있다 — 닫고 다시 실행해라:
$(editor_open)"
fi

# ── 1. 뼈대 (없을 때만) ─────────────────────────────────────────────────────
if [ ! -f "$WS/ProjectSettings/ProjectVersion.txt" ]; then
  log "새 워크스페이스 생성: $WS"
  mkdir -p "$WS/Assets" "$WS/Packages" "$WS/ProjectSettings"
  printf 'm_EditorVersion: %s\n' "$UNITY_VERSION" > "$WS/ProjectSettings/ProjectVersion.txt"
fi
# 패키지는 최소 구성(빌드 시간·산출 크기). 필요한 모듈이 생기면 여기에 추가하고 문서에 적어라.
cat > "$WS/Packages/manifest.json" <<'JSON'
{
  "dependencies": {
    "com.unity.ugui": "2.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0"
  }
}
JSON
cat > "$WS/.gitignore" <<'TXT'
Library/
Temp/
Logs/
Builds/
UserSettings/
TXT

# ── 2. TMP Essential Resources (패키지 안 .unitypackage 를 직접 풀어 넣는다 — ImportPackage 는 배치에서 비동기라 불안정)
if [ ! -f "$WS/Assets/TextMesh Pro/Resources/TMP Settings.asset" ]; then
  PKG="$UNITY_APP/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage"
  [ -f "$PKG" ] || die "TMP Essential Resources 패키지 없음: $PKG"
  log "TMP Essential Resources 풀기"
  python3 - "$PKG" "$WS" <<'PY'
import sys, tarfile, os
pkg, ws = sys.argv[1], sys.argv[2]
entries = {}
with tarfile.open(pkg, 'r:gz') as t:
    for m in t.getmembers():
        if not m.isfile(): continue
        guid, _, name = m.name.lstrip('./').partition('/')
        entries.setdefault(guid, {})[name] = t.extractfile(m).read()
n = 0
for guid, e in entries.items():
    if 'pathname' not in e: continue
    rel = e['pathname'].decode('utf-8').splitlines()[0].strip()
    dst = os.path.join(ws, rel)
    if 'asset' in e:
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        open(dst, 'wb').write(e['asset']); n += 1
    else:
        os.makedirs(dst, exist_ok=True)
    if 'asset.meta' in e:
        open(dst + '.meta', 'wb').write(e['asset.meta'])
print(f'[setup] TMP 파일 {n}개')
PY
fi

# ── 3. 킷 동기화 ────────────────────────────────────────────────────────────
sync_kit
log "킷 동기화 완료"

# ── 4. Unity 배치 설정(첫 실행은 Library 생성으로 수 분 걸린다) ────────────────
mkdir -p "$WS/Logs"
LOG="$WS/Logs/mgf-setup.log"
log "Unity 배치 설정 실행 (로그: $LOG)"
source "$HOME/.unity/env" 2>/dev/null || true
T0=$(date +%s)
set +e
"$UNITY_APP/Contents/MacOS/Unity" -batchmode -nographics -projectPath "$WS" -buildTarget WebGL \
  -executeMethod MgfSetup.Configure -logFile "$LOG" >/dev/null 2>&1
RC=$?
set -e
T1=$(date +%s)
if [ $RC -ne 0 ] || ! grep -q "MGF_SETUP_OK" "$LOG"; then
  grep -E "error CS[0-9]+|\[MGF\]|Exception" "$LOG" | sort -u | head -30 >&2 || true
  die "Unity 설정 실패(rc=$RC). 로그: $LOG"
fi
log "완료 ($((T1 - T0))초) — $WS"
