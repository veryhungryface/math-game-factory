#!/bin/bash
# 번들 Roslyn + .NET 6 으로 게임의 RouteRecordRules.cs + HeadlessCheck.cs 를 컴파일·실행한다(Unity 라이선스 불필요).
set -e
cd "$(dirname "$0")/../../../../.."
U=/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents
DN=$U/Resources/Scripting/NetCoreRuntime/dotnet; CSC=$U/Resources/Scripting/DotNetSdkRoslyn/csc.dll
FW=$U/Resources/Scripting/NetCoreRuntime/shared/Microsoft.NETCore.App/6.0.21
OUT=${TMPDIR:-/tmp}/route-record-headless; mkdir -p "$OUT"
{ echo "-nologo -nostdlib -target:exe -langversion:9 -out:$OUT/check.dll"
  for f in $FW/System.*.dll $FW/netstandard.dll $FW/mscorlib.dll; do echo "-r:$f"; done
  echo "\"factory/unity-src/route-record/Scripts/RouteRecordRules.cs\""
  echo "\"factory/unity-src/route-record/ArtSource/validation/HeadlessCheck.cs\""
} > "$OUT/check.rsp"
"$DN" "$CSC" @"$OUT/check.rsp" | grep -E "error" && exit 2 || true
echo '{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"6.0.21"}}}' > "$OUT/check.runtimeconfig.json"
DOTNET_ROOT=$U/Resources/Scripting/NetCoreRuntime "$DN" "$OUT/check.dll" factory/unity-src/route-record/ArtSource/validation/headless-results.json
