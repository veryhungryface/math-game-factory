// 깃 나눠 — 전수 검사 + 무뇌 봇 자가 테스트.
using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mgf.GitNanwo
{
    public static class GitBotSelfTest
    {
#if UNITY_EDITOR
        public static void Run()
        {
            string outPath = "git-nanwo-bots.md";
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-gnOut") outPath = a[i + 1];
            var md = RunAll();
            System.IO.File.WriteAllText(outPath, md);
            UnityEngine.Debug.Log("GN_BOTS\n" + md);
            EditorApplication.Exit(0);
        }
#endif
        public static string RunAll()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 깃 나눠 — 전수 검사 + 무뇌 봇");
            Exhaustive(sb);
            Bots(sb);
            return sb.ToString();
        }

        static void Exhaustive(StringBuilder sb)
        {
            var g = new SheetGen(new Random(1));
            int sheets = 0, badU = 0, badStart = 0, badSat = 0, badRoot = 0, badSpan = 0, badMis = 0, badAsk = 0, leak = 0;
            var prompts = new HashSet<string>();
            for (int d = 0; d < 400; d++)
            {
                foreach (var s in g.Deck())
                {
                    sheets++;
                    prompts.Add(Words.Prompt(s));
                    string all = Words.Prompt(s) + Words.Answer(s) + Words.RevealRight(s);
                    if (all.IndexOf('√') >= 0 || all.Contains("근호") || all.Contains("가정") || all.Contains("결론") || all.IndexOf('∥') >= 0) badRoot++;
                    if (Judge.UniqueHits(s) != 1) badU++;
                    if (!Judge.Satisfies(s, s.target)) badSat++;
                    if (s.ad0 == s.target) badStart++;
                    if (s.stage > 0 && s.Span < 9) badSpan++;
                    if (s.misTick == s.target) badMis++;
                    if (s.kind != Kind.Similar && s.kind != Kind.Mid && s.asked != s.target) badAsk++;
                    if (s.kind == Kind.Similar && (s.asked != s.de || s.target != s.de)) badAsk++;
                    if (s.kind == Kind.Mid && (s.asked != s.target || s.asked * 2 != s.bc)) badAsk++;
                    if (s.kind == Kind.Area && (s.asked != s.target || s.askedUnit != "cm" || s.target * 3 != s.median * 2 || s.area * 2 != s.bc * s.median)) badAsk++;
                    if (s.kind == Kind.FindEc && s.stage > 0 && (s.ab == s.ac || s.ae == s.target)) badAsk++;
                    if (s.kind == Kind.Three && !Words.Prompt(s).Contains("그 직선에서")) badAsk++;
                    if (s.kind == Kind.Similar)
                    {
                        string rev = Words.RevealRight(s);
                        if (rev.IndexOf(s.m + ":" + s.n, StringComparison.Ordinal) < 0) badAsk++;
                        Judge.Ratio(s.target, s.ab, out int wm, out int wn);
                        if (s.ab != s.bc && (wm != s.m || wn != s.n) && rev.IndexOf(wm + ":" + wn, StringComparison.Ordinal) >= 0) badAsk++;
                    }
                    if (s.kind == Kind.Three && !Judge.Prop(s.fAd, s.givenAd, s.ae, s.asked)) badAsk++;
                    string p = Words.Prompt(s);
                    string drop = s.target + " cm";
                    if (s.kind != Kind.FindAe && p.Contains(drop) && s.kind != Kind.Part) leak++;
                }
            }
            int guard = 0, anyN = 0, anyBad = 0;
            var g2 = new SheetGen(new Random(9));
            while (anyN < 800 && guard++ < 4000)
            {
                var s = g2.Any(); anyN++;
                if (s == null || Judge.UniqueHits(s) != 1 || !Judge.Satisfies(s, s.target) || s.ad0 == s.target) anyBad++;
                if (s != null)
                {
                    prompts.Add(Words.Prompt(s));
                    if (s.kind == Kind.FindEc && (s.ab == s.ac || s.ae == s.target)) anyBad++;
                    if (s.kind == Kind.Area && (s.asked != s.target || s.askedUnit != "cm")) anyBad++;
                    if (s.kind == Kind.Three && !Words.Prompt(s).Contains("그 직선에서")) anyBad++;
                }
            }
            var p0 = SheetGen.Practice();
            int pDe = Judge.De(p0, p0.givenAd);
            sb.AppendLine("- 연습 장 UniqueHits=" + Judge.UniqueHits(p0) + " target=" + p0.target + " ad0=" + p0.ad0
                          + " de=" + p0.de + " Judge.De=" + pDe + (p0.de == pDe ? " ok" : " DE불일치"));
            sb.AppendLine("- 덱 400판 = " + sheets + "장, 정답 칸이 1개가 아닌 장 **" + badU + "**");
            sb.AppendLine("- 타깃이 Satisfies 가 아닌 장 **" + badSat + "** · 스폰=정답 **" + badStart + "** · 오개념=정답 **" + badMis + "** · span<9 **" + badSpan + "**");
            sb.AppendLine("- √/가정/결론/∥ **" + badRoot + "** · asked 불일치 **" + badAsk + "**");
            sb.AppendLine("- Any() " + anyN + "장 중 유일성 실패 **" + anyBad + "** · 고유 발문 " + prompts.Count);
            sb.AppendLine();
        }

        static void Bots(StringBuilder sb)
        {
            const int Games = 200, Cuts = 10;
            double mash = 0, cyc = 0, rnd = 0, none = 0, chance = 0;
            int mashFin = 0, cycFin = 0, rndFin = 0, nM = 0, nC = 0, nR = 0;
            var rng = new Random(42);
            for (int g = 0; g < Games; g++)
            {
                var deck = new SheetGen(new Random(1000 + g)).Deck();
                int firstM = 0, firstC = 0, firstR = 0, att = 0;
                int livesM = 3, livesC = 3, livesR = 3;
                int okM = 0, okC = 0, okR = 0;
                for (int i = 0; i < Math.Min(Cuts, deck.Count); i++)
                {
                    var s = deck[i];
                    int ticks = Rules.Ticks(s);
                    chance += 1.0 / ticks; att++;
                    // 연타: 장 시작 칸(ad0)을 즉시 탭
                    if (s.ad0 == s.target) firstM++;
                    if (s.ad0 == s.target) { okM++; } else { livesM--; }
                    // 순환: 시작 칸에서 i칸 왕복한 위치
                    int cycTick = s.ad0, dir = 1;
                    for (int k = 0; k < i; k++)
                    {
                        cycTick += dir;
                        if (cycTick >= s.adHi) { cycTick = s.adHi; dir = -1; }
                        if (cycTick <= s.adLo) { cycTick = s.adLo; dir = 1; }
                    }
                    if (cycTick == s.target) firstC++;
                    if (cycTick == s.target) okC++; else livesC--;
                    // 무작위: 내부 칸 균등
                    int rAd = s.adLo + rng.Next(Math.Max(1, ticks));
                    if (rAd > s.adHi) rAd = s.adHi;
                    if (rAd == s.target) firstR++;
                    if (rAd == s.target) okR++; else livesR--;
                }
                nM += att; nC += att; nR += att;
                mash += firstM; cyc += firstC; rnd += firstR;
                if (okM >= Cuts && livesM > 0) mashFin++;
                if (okC >= Cuts && livesC > 0) cycFin++;
                if (okR >= Cuts && livesR > 0) rndFin++;
            }
            double ch = chance / nR;
            sb.AppendLine("## 무뇌 봇 200판 × 10컷, 측정=첫 시도 정답률 (우연≈1/칸수)");
            sb.AppendLine("- 연타 봇: " + Pct(mash / nM) + "  (우연 " + Pct(ch) + ")  완주율 " + Pct(mashFin / (double)Games));
            sb.AppendLine("- 순환 봇: " + Pct(cyc / nC) + "  (우연 " + Pct(ch) + ")  완주율 " + Pct(cycFin / (double)Games));
            sb.AppendLine("- 무작위 봇: " + Pct(rnd / nR) + "  (우연 " + Pct(ch) + ")  완주율 " + Pct(rndFin / (double)Games));
            sb.AppendLine("- 무입력 봇: " + Pct(none) + "  진도 0");
        }
        static string Pct(double x) => (x * 100).ToString("0.0") + "%";
    }
}
