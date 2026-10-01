// 석회 착지 — 전수 검사 + 무뇌 봇 자가 테스트.
using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mgf.SeokhoeChakji
{
    public static class SeokhoeBotSelfTest
    {
#if UNITY_EDITOR
        public static void Run()
        {
            string outPath = "seokhoe-bots.md";
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-skOut") outPath = a[i + 1];
            var md = RunAll();
            System.IO.File.WriteAllText(outPath, md);
            UnityEngine.Debug.Log("SK_BOTS\n" + md);
            EditorApplication.Exit(0);
        }
#endif
        public static string RunAll()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 석회 착지 — 전수 검사 + 무뇌 봇");
            Exhaustive(sb);
            Bots(sb);
            return sb.ToString();
        }

        static void Exhaustive(StringBuilder sb)
        {
            var g = new SheetGen(new Random(1));
            int sheets = 0, badU = 0, badStart = 0, badSat = 0, badRoot = 0, badSpan = 0, badMis = 0, badAsk = 0, badAe = 0, badIso = 0, badAm = 0;
            var prompts = new HashSet<string>();
            for (int d = 0; d < 400; d++)
            {
                foreach (var s in g.Deck())
                {
                    sheets++;
                    prompts.Add(Words.Prompt(s));
                    string all = Words.Prompt(s) + Words.Answer(s) + Words.RevealRight(s);
                    if (all.IndexOf('√') >= 0 || all.Contains("근호") || all.Contains("가정") || all.Contains("결론")) badRoot++;
                    if (Judge.UniqueHits(s) != 1) badU++;
                    if (!Judge.Satisfies(s, s.target)) badSat++;
                    if (s.ad0 == s.target) badStart++;
                    if (s.stage > 0 && s.Span < 9) badSpan++;
                    if (s.misTick == s.target) badMis++;
                    if (s.asked != s.target) badAsk++;
                    if (s.kind == Kind.Area && s.askedUnit == "cm²") badAsk++;
                    if (s.kind == Kind.FindAe && Words.Prompt(s).IndexOf("AC=", StringComparison.Ordinal) < 0) badAe++;
                    if (s.kind == Kind.FindAe && (s.ab == s.ac || s.ae == s.givenAd || !s.hopAc)) badIso++;
                    if (s.kind == Kind.Mid && (Words.Prompt(s).IndexOf("AM의", StringComparison.Ordinal) >= 0 || Words.Prompt(s).IndexOf("MN", StringComparison.Ordinal) < 0 || s.asked * 2 != s.bc || s.target != s.asked)) badAm++;
                }
            }
            int guard = 0, anyN = 0, anyBad = 0;
            var g2 = new SheetGen(new Random(9));
            while (anyN < 800 && guard++ < 4000)
            {
                var s = g2.Any(); anyN++;
                if (Judge.UniqueHits(s) != 1 || !Judge.Satisfies(s, s.target) || s.ad0 == s.target) anyBad++;
                prompts.Add(Words.Prompt(s));
            }
            var p = SheetGen.Practice();
            int pHits = Judge.UniqueHits(p);
            sb.AppendLine("- 연습 장 UniqueHits=" + pHits + " target=" + p.target + " ad0=" + p.ad0);
            sb.AppendLine("- 덱 400판 = " + sheets + "장, 정답 칸이 1개가 아닌 장 **" + badU + "**");
            sb.AppendLine("- 타깃이 Satisfies 가 아닌 장 **" + badSat + "** · 스폰=정답 **" + badStart + "** · 오개념=정답 **" + badMis + "** · span<9 **" + badSpan + "**");
            sb.AppendLine("- √/가정/결론 **" + badRoot + "** · asked≠착지 **" + badAsk + "** · FindAe에 AC 없음 **" + badAe + "**");
            sb.AppendLine("- FindAe 이등변/AD복사 **" + badIso + "** · Mid가 AM을 묻거나 MN≠BC/2 **" + badAm + "**");
            sb.AppendLine("- Any() " + anyN + "장 중 유일성 실패 **" + anyBad + "** · 고유 발문 " + prompts.Count);
            sb.AppendLine();
        }

        static void Bots(StringBuilder sb)
        {
            const int Games = 200, Lands = 10;
            double mash = 0, cyc = 0, rnd = 0, none = 0, chance = 0;
            int mashFin = 0, cycFin = 0, rndFin = 0, nM = 0, nC = 0, nR = 0;
            var rng = new Random(42);
            for (int g = 0; g < Games; g++)
            {
                var deck = new SheetGen(new Random(1000 + g)).Deck();
                int firstM = 0, firstC = 0, firstR = 0, att = 0;
                for (int i = 0; i < Math.Min(Lands, deck.Count); i++)
                {
                    var s = deck[i];
                    int ticks = Rules.Ticks(s);
                    chance += 1.0 / ticks; att++;
                    if (s.ad0 == s.target) firstM++;
                    int cycAd = s.ad0;
                    if (cycAd == s.target) firstC++;
                    int rAd = s.adLo + rng.Next(Math.Max(1, ticks));
                    if (rAd > s.adHi) rAd = s.adHi;
                    if (rAd == s.target) firstR++;
                }
                nM += att; nC += att; nR += att;
                mash += firstM; cyc += firstC; rnd += firstR;
            }
            double ch = chance / nR;
            sb.AppendLine("## 무뇌 봇 200판 × 10착지, 측정=첫 시도 정답률 (우연≈1/칸수)");
            sb.AppendLine("- 연타 봇: " + Pct(mash / nM) + "  (우연 " + Pct(ch) + ")  완주율 " + Pct(mashFin / (double)Games));
            sb.AppendLine("- 순환 봇: " + Pct(cyc / nC) + "  (우연 " + Pct(ch) + ")  완주율 " + Pct(cycFin / (double)Games));
            sb.AppendLine("- 무작위 봇: " + Pct(rnd / nR) + "  (우연 " + Pct(ch) + ")  완주율 " + Pct(rndFin / (double)Games));
            sb.AppendLine("- 무입력 봇: " + Pct(none) + "  진도 0");
        }
        static string Pct(double x) => (x * 100).ToString("0.0") + "%";
    }
}
