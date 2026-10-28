#!/bin/bash
# Unity 라이선스 없이 게임 C# 전체를 Unity/TMP/uGUI 어셈블리에 대해 형식 검사한다(빌드 아님).
set -e
cd "$(dirname "$0")/../../../../.."
U=/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents
DN=$U/Resources/Scripting/NetCoreRuntime/dotnet; CSC=$U/Resources/Scripting/DotNetSdkRoslyn/csc.dll
WS=$HOME/UnityProjects/MGF-Workspace
OUT=${TMPDIR:-/tmp}/hkg-typecheck; mkdir -p "$OUT"
{ echo "-nologo -nostdlib -target:library -langversion:9 -define:UNITY_WEBGL -nowarn:0414,0169,0649,0219 -out:$OUT/game.dll"


  for f in $U/Resources/Scripting/Managed/UnityEngine/*.dll; do echo "-r:\"$f\""; done
  echo "-r:\"$U/Resources/Scripting/NetStandard/ref/2.1.0/netstandard.dll\""
  echo "-r:\"$WS/Library/ScriptAssemblies/UnityEngine.UI.dll\""
  echo "-r:\"$WS/Library/ScriptAssemblies/Unity.TextMeshPro.dll\""
  for f in factory/unity/kit/MgfKit/Runtime/*.cs factory/unity-src/han-kkeut-golf/Scripts/*.cs; do echo "\"$f\""; done
} > "$OUT/game.rsp"
"$DN" "$CSC" @"$OUT/game.rsp" 2>&1 | grep -v "^$" | grep -E "error|warning CS0(162|168|219|414)" || echo "typecheck OK"
