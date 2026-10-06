using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Mgf.PrismMobil
{
    // Unity 공용 템플릿은 공유 미리보기 메타를 만들지 않으므로, 게임별 빌드 산출물에 멱등 삽입한다.
    public sealed class PrismMobilPostbuild : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            string index = Path.Combine(report.summary.outputPath, "index.html");
            if (!File.Exists(index)) return;
            string html = File.ReadAllText(index);
            if (html.Contains("property=\"og:title\"")) return;
            const string marker = "<title>프리즘 모빌</title>";
            const string meta = marker + "\n" +
                "<meta property=\"og:title\" content=\"프리즘 모빌\">\n" +
                "<meta property=\"og:description\" content=\"한 점에 걸어 빛을 맞춰라\">\n" +
                "<meta property=\"og:image\" content=\"./square.png\">\n" +
                "<meta name=\"twitter:card\" content=\"summary_large_image\">";
            if (html.Contains(marker)) File.WriteAllText(index, html.Replace(marker, meta));
        }
    }
}
