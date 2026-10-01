// 수문 — 전수 검사 + 무뇌 봇 자가 테스트. 에디터 배치 전용.
using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mgf.Sumun
{
    public static class SumunBotSelfTest
    {
#if UNITY_EDITOR
        public static void Run()
        {
            string outPath = "sumun-bots.md";
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-smOut") outPath = a[i + 1];
            var md = RunAll();
            System.IO.File.WriteAllText(outPath, md);
            UnityEngine.Debug.Log("SM_BOTS\n" + md);
            EditorApplication.Exit(0);
        }
#endif
        public static string RunAll()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 수문 — 전수 검사 + 무뇌 봇");
            Exhaustive(sb);
            Bots(sb);
            return sb.ToString();
        }

        static void Exhaustive(StringBuilder sb)
        {
            var g = new SheetGen(new Random(1));
            int sheets = 0, badU = 0, badStart = 0, badFit = 0, badRoot = 0, badSat = 0, badMis = 0, badDe = 0, badRev = 0, badCon = 0;
            var prompts = new HashSet<string>();
            for (int d = 0; d < 400; d++)
            {
                foreach (var s in g.Deck())
                {
                    sheets++;
                    prompts.Add(Words.Prompt(s));
                    string all = Words.Prompt(s) + Words.Answer(s) + Words.RevealRight(s);
                    if (all.IndexOf('√') >= 0 || all.IndexOf('√') >= 0) badRoot++;
                    if (all.Contains("근호") || all.Contains("가정") || all.Contains("결론")) badRoot++;
                    int hits = Judge.UniqueHits(s);
                    if (hits != 1) badU++;
                    if (!Judge.Satisfies(s, s.target)) badSat++;
                    if (s.ad0 == s.target && s.kind != Kind.Fix) badStart++;
                    if (s.kind == Kind.Fix && s.ad0 == s.target) badStart++;
                    if (s.target < 1) badFit++;
                    if (s.misTick == s.target && s.kind == Kind.Fix) badMis++;
                    if (!s.isPlumb && s.de > 0 && Judge.De(s, s.target) != s.de) badDe++;
                    if (s.kind == Kind.Fix && !s.showWhole)
                    {
                        string rv = Words.RevealRight(s);
                        if (rv.IndexOf("DE", StringComparison.Ordinal) >= 0) badRev++;
                        if (Words.Concept(s).IndexOf("닮음", StringComparison.Ordinal) >= 0) badCon++;
                    }
                    if ((s.kind == Kind.Part || (s.kind == Kind.Fix && !s.showWhole)) && s.ab % (s.m + s.n) != 0) badFit++;
                }
            }
            int guard = 0, anyN = 0, anyBad = 0;
            var g2 = new SheetGen(new Random(9));
            while (anyN < 800 && guard++ < 4000)
            {
                var s = g2.Any(); anyN++;
                if (Judge.UniqueHits(s) != 1 || !Judge.Satisfies(s, s.target)) anyBad++;
                prompts.Add(Words.Prompt(s));
            }
            sb.AppendLine("- 덱 400판 = " + sheets + "장, 정답 칸이 1개가 아닌 장 **" + badU + "**");
            sb.AppendLine("- 타깃이 Satisfies 가 아닌 장 **" + badSat + "** · 스폰=정답 **" + badStart + "** · 오개념=정답(Fix) **" + badMis + "**");
            sb.AppendLine("- √/가정/결론 **" + badRoot + "** · DE 불일치 **" + badDe + "** · FixPart 해설 DE **" + badRev + "** · Fix 닮음 카드 **" + badCon + "**");
            sb.AppendLine("- Any() " + anyN + "장 중 유일성 실패 **" + anyBad + "** · 고유 발문 " + prompts.Count);
            sb.AppendLine();
        }

        static void Bots(StringBuilder sb)
        {
            const int Games = 200, Locks = 10;
            double mash = 0, cyc = 0, rnd = 0, none = 0, chance = 0;
            int mashFin = 0, cycFin = 0, rndFin = 0, nM = 0, nC = 0, nR = 0;
            var rng = new Random(42);
            for (int g = 0; g < Games; g++)
            {
                var deck = new SheetGen(new Random(1000 + g)).Deck();
                int firstM = 0, firstC = 0, firstR = 0, att = 0;
                for (int i = 0; i < Math.Min(Locks, deck.Count); i++)
                {
                    var s = deck[i];
                    int ticks = Math.Max(1, (s.isPlumb ? s.median : s.ab) - 1);
                    chance += 1.0 / ticks; att++;
                    // 연타: 스폰 칸에서 바로 잠금
                    if (s.ad0 == s.target) firstM++;
                    // 순환: 1,2,3,4... 첫 잠금
                    int cycAd = (i % 4) + 1; if (cycAd > ticks) cycAd = 1;
                    if (cycAd == s.target) firstC++;
                    // 무작위
                    int rAd = 1 + rng.Next(ticks);
                    if (rAd == s.target) firstR++;
                }
                nM += att; nC += att; nR += att;
                mash += firstM; cyc += firstC; rnd += firstR;
            }
            double ch = chance / nR;
            sb.AppendLine("## 무뇌 봇 200판 × 10잠금, 측정=첫 시도 정답률 (우연≈1/칸수)");
            sb.AppendLine("- 연타 봇: " + Pct(mash / nM) + "  (우연 " + Pct(ch) + ")  완주율 " + Pct(mashFin / (double)Games));
            sb.AppendLine("- 순환 봇: " + Pct(cyc / nC) + "  (우연 " + Pct(ch) + ")  완주율 " + Pct(cycFin / (double)Games));
            sb.AppendLine("- 무작위 봇: " + Pct(rnd / nR) + "  (우연 " + Pct(ch) + ")  완주율 " + Pct(rndFin / (double)Games));
            sb.AppendLine("- 무입력 봇: " + Pct(none) + "  진도 0");
        }
        static string Pct(double x) => (x * 100).ToString("0.0") + "%";
    }
}
