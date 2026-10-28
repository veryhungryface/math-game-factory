#!/bin/bash
# Unity 라이선스 없이 번들 Roslyn + .NET 6 으로 HanKkeutRules.cs + HeadlessCheck.cs 를 컴파일·실행한다.
set -e
cd "$(dirname "$0")/../../../../.."
U=/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents
DN=$U/Resources/Scripting/NetCoreRuntime/dotnet; CSC=$U/Resources/Scripting/DotNetSdkRoslyn/csc.dll
FW=$U/Resources/Scripting/NetCoreRuntime/shared/Microsoft.NETCore.App/6.0.21
OUT=${TMPDIR:-/tmp}/hkg-headless; mkdir -p "$OUT"
cat > "$OUT/Stub.cs" <<'CS'
namespace Mgf { public class MgfProblem { public string id, prompt, answer, unitConcept; public string[] choices; public double answerNumeric = double.NaN; } }
CS
{ echo "-nologo -nostdlib -target:exe -langversion:9 -nowarn:0414,0169,0649,0219 -out:$OUT/check.dll"
  for f in $FW/System.*.dll $FW/netstandard.dll $FW/mscorlib.dll; do echo "-r:$f"; done
  echo "\"$OUT/Stub.cs\""
  echo "\"factory/unity-src/han-kkeut-golf/Scripts/HanKkeutRules.cs\""
  echo "\"factory/unity-src/han-kkeut-golf/ArtSource/validation/HeadlessCheck.cs\""
} > "$OUT/check.rsp"
"$DN" "$CSC" @"$OUT/check.rsp" | grep -E "error" || true
echo '{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"6.0.21"}}}' > "$OUT/check.runtimeconfig.json"
DOTNET_ROOT=$U/Resources/Scripting/NetCoreRuntime "$DN" "$OUT/check.dll" factory/unity-src/han-kkeut-golf/ArtSource/validation/headless-results.json
