// MGF Unity kit — 배치 WebGL 빌드 진입점. factory/unity/build.sh 가 부른다.
//
//   unity build <ws> --target WebGL --execute-method MgfBuild.Perform \
//     --args "-mgfOut <dir> -mgfName <slug> [-mgfScene Assets/Game/X.unity] [-mgfTitle <제목> | -mgfTitleFile <파일>] [-mgfBg #RRGGBB]"
//
// -mgfScene 이 없으면 씬을 코드로 생성한다: Main Camera(+AudioListener) + "Game" 오브젝트에
// IMgfGame 을 구현한 MonoBehaviour(Assets/Game 안에서 정확히 1개)를 붙인다. 씬 YAML 손편집은 하지 않는다.
// 결과 요약은 로그에 "MGF_BUILD_RESULT {json}" 한 줄로 남긴다(build.sh 가 파싱).
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MgfBuild
{
    const string GeneratedScene = "Assets/MgfGenerated/Main.unity";

    static string Arg(string name, string fallback = null)
    {
        var a = Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++)
            if (a[i] == name) return a[i + 1];
        return fallback;
    }

    [Serializable]
    class Result
    {
        public string result, slug, scene, output, bootstrap;
        public long totalSize;
        public int errors, warnings;
        public double seconds;
        public string[] errorMessages;
    }

    public static void Perform()
    {
        var t0 = DateTime.UtcNow;
        string slug = Arg("-mgfName") ?? throw new Exception("-mgfName <slug> 가 필요하다");
        string outDir = Arg("-mgfOut") ?? Arg("-buildOutput") ?? throw new Exception("-mgfOut <dir> 가 필요하다");
        string scene = Arg("-mgfScene");
        string title = Arg("-mgfTitle");
        string titleFile = Arg("-mgfTitleFile"); // 한글·공백 제목은 파일로 받는다(build.sh)
        if (!string.IsNullOrEmpty(titleFile) && File.Exists(titleFile)) title = File.ReadAllText(titleFile).Trim();
        if (string.IsNullOrEmpty(title)) title = slug;
        string bg = Arg("-mgfBg", "#1B2440");
        var res = new Result { slug = slug, output = outDir };
        int code = 1;
        try
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            MgfSetup.ApplyPlayerSettings(title, bg);
            MgfSetup.ApplyBuildVariant(Arg("-mgfCompression", "gzip"), Arg("-mgfStrip", "Medium"), Arg("-mgfOpt"));
            MgfSetup.EnsureTmpFont();
            if (string.IsNullOrEmpty(scene))
            {
                res.bootstrap = GenerateScene();
                scene = GeneratedScene;
            }
            else if (!File.Exists(scene)) throw new Exception("씬 없음: " + scene);
            res.scene = scene;

            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            var s = report.summary;
            res.result = s.result.ToString();
            res.totalSize = (long)s.totalSize;
            res.errors = s.totalErrors;
            res.warnings = s.totalWarnings;
            res.errorMessages = report.steps.SelectMany(st => st.messages)
                .Where(m => m.type == LogType.Error || m.type == LogType.Exception)
                .Select(m => m.content).Take(20).ToArray();
            if (s.result == BuildResult.Succeeded && File.Exists(Path.Combine(outDir, "index.html"))) code = 0;
        }
        catch (Exception e)
        {
            res.result = "Exception";
            res.errorMessages = new[] { e.Message };
            Debug.LogError("[MGF] 빌드 실패: " + e);
        }
        res.seconds = Math.Round((DateTime.UtcNow - t0).TotalSeconds, 1);
        Debug.Log("MGF_BUILD_RESULT " + JsonUtility.ToJson(res));
        EditorApplication.Exit(code);
    }

    /// <summary>Assets/Game 안에서 IMgfGame 을 구현한 MonoBehaviour 를 정확히 1개 찾아 씬을 만든다.</summary>
    static string GenerateScene()
    {
        var candidates = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
            .Where(t => !t.IsAbstract && typeof(Mgf.IMgfGame).IsAssignableFrom(t))
            .ToArray();
        if (candidates.Length != 1)
            throw new Exception("IMgfGame 을 구현한 MonoBehaviour 가 정확히 1개여야 한다. 찾은 것: " +
                                (candidates.Length == 0 ? "없음" : string.Join(", ", candidates.Select(t => t.FullName))));
        var type = candidates[0];
        if (type.Namespace == null || !type.Namespace.StartsWith("Mgf."))
            Debug.LogWarning("[MGF] 게임 타입은 namespace Mgf.<Slug> 안에 둬라: " + type.FullName);

        Directory.CreateDirectory(Path.GetDirectoryName(GeneratedScene));
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera") { tag = "MainCamera" };
        var c = cam.AddComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.1f, 0.12f, 0.2f);
        cam.AddComponent<AudioListener>();
        cam.transform.position = new Vector3(0, 3, -8);
        new GameObject("Game").AddComponent(type);
        EditorSceneManager.SaveScene(sc, GeneratedScene);
        AssetDatabase.SaveAssets();
        return type.FullName;
    }
}
