// MGF Unity kit — 워크스페이스 설정(1회) + 빌드마다 적용하는 PlayerSettings.
// setup-workspace.sh 가 `-executeMethod MgfSetup.Configure` 로 부른다. 멱등이다.
//
// 결정 근거(실측·함정)는 docs/unity-track.md 「빌드 설정」 절에 있다. 여기 값을 바꾸면 문서도 고쳐라.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;
using TMPro;

public static class MgfSetup
{
    public static void Configure()
    {
        int code = 0;
        try
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            ApplyPlayerSettings(null, null);
            ConfigureQuality();
            ConfigureGraphics();
            ConfigureInput();
            ConfigureFontImport();
            EnsureTmpFont();
            AssetDatabase.SaveAssets();
            Debug.Log("MGF_SETUP_OK");
        }
        catch (Exception e)
        {
            Debug.LogError("[MGF] 설정 실패: " + e);
            code = 1;
        }
        EditorApplication.Exit(code);
    }

    /// <summary>빌드마다 다시 적용한다(누가 에디터로 열어 바꿔도 빌드 결과가 같도록).</summary>
    public static void ApplyPlayerSettings(string title, string bgHex)
    {
        var web = NamedBuildTarget.WebGL;
        PlayerSettings.companyName = "MGF";
        if (!string.IsNullOrEmpty(title)) PlayerSettings.productName = title;
        PlayerSettings.bundleVersion = "1.0";
        if (!string.IsNullOrEmpty(bgHex) && ColorUtility.TryParseHtmlString(bgHex, out var bg))
            PlayerSettings.SplashScreen.backgroundColor = bg;

        // 화면: 선형 색공간(WebGL2 전용) — Coral Rescue 와 같은 조명 품질
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultWebScreenWidth = 390;
        PlayerSettings.defaultWebScreenHeight = 844;

        // WebGL 산출물 압축은 ApplyBuildVariant 가 정한다(기본 gzip + decompressionFallback:
        // Content-Encoding 헤더 없이 어떤 정적 서버에서도 뜨고, 전송량은 무압축의 1/3 — 실측은 문서).
        PlayerSettings.WebGL.template = "PROJECT:MGF";
        ApplyBuildVariant("gzip", "Medium");
        PlayerSettings.WebGL.nameFilesAsHashes = true;      // 내용 해시 파일명 → vercel.json 에서 immutable 캐시
        PlayerSettings.WebGL.dataCaching = false;           // IndexedDB 캐시 대신 HTTP 캐시(해시 파일명)
        PlayerSettings.WebGL.showDiagnostics = false;
        PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.initialMemorySize = 64;
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.WebGL.maximumMemorySize = 1024;
        PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;

        // 코드: IL2CPP(WebGL 유일 백엔드). 크기·빌드 시간 균형 — 실측은 문서 참조
        PlayerSettings.SetScriptingBackend(web, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(web, Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetIl2CppCodeGeneration(web, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetManagedStrippingLevel(web, ManagedStrippingLevel.Medium);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetApiCompatibilityLevel(web, ApiCompatibilityLevel.NET_Standard);
    }

    /// <summary>빌드 변형 스위치(build.sh 의 MGF_UNITY_COMPRESSION / MGF_UNITY_STRIP / MGF_UNITY_OPT). 기본 gzip / Medium / DiskSizeLTO.</summary>
    public static void ApplyBuildVariant(string compression, string strip, string codeOpt = null)
    {
        SetCodeOptimization(string.IsNullOrEmpty(codeOpt) ? DefaultCodeOptimization : codeOpt);
        switch ((compression ?? "none").ToLowerInvariant())
        {
            case "gzip": // 헤더 없이도 뜨게 로더가 JS 로 푼다(decompressionFallback). 전송량↓ 부팅 CPU↑
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                break;
            case "brotli":
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
                PlayerSettings.WebGL.decompressionFallback = true;
                break;
            default:
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.decompressionFallback = false;
                break;
        }
        if (Enum.TryParse<ManagedStrippingLevel>(strip ?? "Medium", true, out var lvl))
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, lvl);
    }

    /// <summary>Wasm 코드 최적화(Unity 6 WebGL 「Code Optimization」). 기본값 근거는 docs/unity-track.md 실측표.</summary>
    public const string DefaultCodeOptimization = "DiskSizeLTO";

    /// <summary>UnityEditor.WebGL.UserBuildSettings.codeOptimization — WebGL 확장 어셈블리에 있어 리플렉션으로 설정.</summary>
    static void SetCodeOptimization(string value)
    {
        Type t = null;
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            if ((t = a.GetType("UnityEditor.WebGL.UserBuildSettings")) != null) break;
        var p = t?.GetProperty("codeOptimization", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (p == null) { Debug.LogWarning("[MGF] codeOptimization 설정 API 없음 — 기본값으로 빌드"); return; }
        try { p.SetValue(null, Enum.Parse(p.PropertyType, value, true)); }
        catch (Exception e) { Debug.LogWarning("[MGF] codeOptimization=" + value + " 실패: " + e.Message); }
        Debug.Log("[MGF] codeOptimization=" + p.GetValue(null));
    }

    static void ConfigureQuality()
    {
        // 모든 품질 레벨을 같은 값으로(웹 기본 레벨이 무엇이든 결과가 같게). 세부 조정은 런타임 MgfLook.Quality().
        var qs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
        var levels = qs.FindProperty("m_QualitySettings");
        for (int i = 0; i < levels.arraySize; i++)
        {
            var q = levels.GetArrayElementAtIndex(i);
            Set(q, "pixelLightCount", 4);
            Set(q, "shadows", 2);            // All (hard+soft)
            Set(q, "shadowResolution", 2);   // High
            Set(q, "shadowCascades", 1);
            Set(q, "shadowDistance", 40f);
            Set(q, "antiAliasing", 4);
            Set(q, "vSyncCount", 0);
            Set(q, "anisotropicTextures", 1);
            Set(q, "globalTextureMipmapLimit", 0);
            Set(q, "realtimeReflectionProbes", 0);
            Set(q, "softParticles", 0);
            Set(q, "skinWeights", 4);
        }
        qs.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Set(SerializedProperty parent, string name, float value)
    {
        var p = parent.FindPropertyRelative(name);
        if (p == null) return;
        if (p.propertyType == SerializedPropertyType.Float) p.floatValue = value;
        else p.intValue = (int)value;
    }

    static void ConfigureGraphics()
    {
        // 내장 렌더 파이프라인(URP 아님) — Coral Rescue 방식. 킷 셰이더는 Resources 에 있어 자동 포함.
        GraphicsSettings.defaultRenderPipeline = null;
        QualitySettings.renderPipeline = null;
    }

    static void ConfigureInput()
    {
        // 구 Input Manager 만 사용(Input System 패키지 없음). 0=Old, 1=New, 2=Both
        var ps = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var p = ps.FindProperty("activeInputHandler");
        if (p != null && p.intValue != 0) { p.intValue = 0; ps.ApplyModifiedPropertiesWithoutUndo(); }
    }

    public const string KrFontAsset = "Assets/MgfGenerated/MgfKR SDF.asset";
    const string TmpSettings = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    /// <summary>
    /// 킷 한글 폰트로 동적 SDF 폰트 에셋을 만들어 TMP 기본 폰트로 지정하고, 영문 LiberationSans·이모지
    /// 리소스를 Resources 에서 걷어낸다(빌드 .data 약 1.7MB 절감 + 기본 폰트가 한글이 된다). 멱등 — 빌드마다 부른다.
    /// </summary>
    public static void EnsureTmpFont()
    {
        if (!File.Exists(TmpSettings)) throw new Exception("TMP Essential Resources 가 없다: " + TmpSettings + " (setup-workspace.sh 가 풀어 넣는다)");
        ConfigureFontImport();
        var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KrFontAsset);
        if (!fa)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/MgfKit/Resources/MgfKit/Fonts/MgfKR-Bold.otf");
            if (!font) throw new Exception("킷 폰트 없음");
            Directory.CreateDirectory(Path.GetDirectoryName(KrFontAsset));
            fa = TMP_FontAsset.CreateFontAsset(font, 72, 7, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (!fa) throw new Exception("TMP 폰트 에셋 생성 실패");
            fa.name = "MgfKR SDF";
            AssetDatabase.CreateAsset(fa, KrFontAsset);
            fa.atlasTextures[0].name = "MgfKR SDF Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.material.name = "MgfKR SDF Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            AssetDatabase.SaveAssets();
        }
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettings);
        var so = new SerializedObject(settings);
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = fa;
        so.FindProperty("m_defaultSpriteAsset").objectReferenceValue = null;
        var fb = so.FindProperty("m_fallbackFontAssets"); if (fb != null) fb.arraySize = 0;
        var clear = so.FindProperty("m_ClearDynamicDataOnBuild"); if (clear != null) clear.boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        foreach (var dir in new[] { "Assets/TextMesh Pro/Resources/Fonts & Materials", "Assets/TextMesh Pro/Resources/Sprite Assets" })
            if (AssetDatabase.IsValidFolder(dir)) AssetDatabase.DeleteAsset(dir);
        AssetDatabase.SaveAssets();
    }

    static void ConfigureFontImport()
    {
        const string path = "Assets/MgfKit/Resources/MgfKit/Fonts/MgfKR-Bold.otf";
        if (AssetImporter.GetAtPath(path) is TrueTypeFontImporter fi && (!fi.includeFontData || fi.fontTextureCase != FontTextureCase.Dynamic))
        {
            fi.includeFontData = true;
            fi.fontTextureCase = FontTextureCase.Dynamic;
            fi.SaveAndReimport();
        }
    }
}
