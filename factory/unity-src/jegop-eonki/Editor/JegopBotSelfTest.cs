// 제곱 얹기 — 전수 검사 + 무뇌 봇 자가 테스트.
using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mgf.JegopEonki
{
    public static class JegopBotSelfTest
    {
#if UNITY_EDITOR
        public static void Run()
        {
            string outPath = "jegop-bots.md";
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-jeOut") outPath = a[i + 1];
            var md = RunAll();
            System.IO.File.WriteAllText(outPath, md);
            UnityEngine.Debug.Log("JE_BOTS\n" + md);
            EditorApplication.Exit(0);
        }
#endif
        public static string RunAll()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 제곱 얹기 — 전수 검사 + 무뇌 봇");
            Exhaustive(sb);
            Bots(sb);
            return sb.ToString();
        }

        static void Exhaustive(StringBuilder sb)
        {
            int sheets = 0, badLock = 0, badRoot = 0, badTri = 0, badDock = 0, badChoice = 0, collide = 0;
            var prompts = new HashSet<string>();
            var g = new SheetGen(new Random(1));
            foreach (var s in g.Exhaust())
            {
                sheets++;
                Check(s, ref badLock, ref badRoot, ref badTri, ref badDock, ref badChoice, ref collide, prompts);
            }
            for (int d = 0; d < 80; d++)
            {
                foreach (var s in g.Deck())
                {
                    sheets++;
                    Check(s, ref badLock, ref badRoot, ref badTri, ref badDock, ref badChoice, ref collide, prompts);
                }
            }
            int anyN = 0, anyBad = 0, guard = 0;
            var g2 = new SheetGen(new Random(9));
            while (anyN < 400 && guard++ < 4000)
            {
                var s = g2.Any(); anyN++;
                if (s == null) { anyBad++; continue; }
                if (s.kind != Kind.Reverse && (s.targetArea != s.Side(s.targetSide) * s.Side(s.targetSide))) anyBad++;
                if (s.kind != Kind.Reverse && !Judge.IsRight(s.a, s.b, s.c)) anyBad++;
                if (s.kind == Kind.Reverse && s.reverseTrue && !Judge.IsRight(s.a, s.b, s.c)) anyBad++;
                if (s.kind == Kind.Reverse && !s.reverseTrue && Judge.IsRight(s.a, s.b, s.c)) anyBad++;
            }
            var p0 = SheetGen.Practice();
            sb.AppendLine("- 연습 a,b,c=" + p0.a + "," + p0.b + "," + p0.c + " targetArea=" + p0.targetArea
                          + " dock " + p0.dock[0] + "/" + p0.dock[1] + "/" + p0.dock[2]);
            sb.AppendLine("- 검사 장 **" + sheets + "**, 잠금 불변 실패 **" + badLock + "**, √/근호 **" + badRoot + "**");
            sb.AppendLine("- 삼각형 실패 **" + badTri + "** · 독 충돌 **" + badDock + "** · 선택지 **" + badChoice + "** · 오답=정답 **" + collide + "**");
            sb.AppendLine("- Any() " + anyN + " 중 불변 실패 **" + anyBad + "** · 고유 발문 " + prompts.Count);
            sb.AppendLine();
        }

        static void Check(Sheet s, ref int badLock, ref int badRoot, ref int badTri, ref int badDock, ref int badChoice, ref int collide, HashSet<string> prompts)
        {
            string all = Words.Prompt(s) + Words.Answer(s) + Words.RevealRight(s) + Words.RevealWrong(s);
            prompts.Add(Words.Prompt(s) + "|" + Words.Answer(s));
            if (all.IndexOf('√') >= 0 || all.Contains("근호") || all.Contains("제곱근")) badRoot++;
            if (s.kind != Kind.Reverse)
            {
                if (!Judge.IsRight(s.a, s.b, s.c)) badTri++;
                if (!Judge.LockOk(s, s.targetSide, s.targetArea)) badLock++;
                if (!Judge.Completes(s, s.targetSide, s.targetArea)) badLock++;
                int n = s.Side(s.targetSide);
                if ((long)n * n != s.targetArea) badLock++;
                for (int i = 0; i < 3; i++)
                {
                    if (s.dock[i] == s.targetArea && s.kind == Kind.AreaHyp) collide++;
                    for (int j = i + 1; j < 3; j++) if (s.dock[i] == s.dock[j]) badDock++;
                }
            }
            else
            {
                if (!Judge.IsTriangle(s.a, s.b, s.c)) badTri++;
                if (s.reverseTrue != Judge.IsRight(s.a, s.b, s.c)) badLock++;
                if (s.reverseTrue && !Judge.StampOk(s, s.rightVertex)) badLock++;
                if (!s.reverseTrue && !Judge.DiscardOk(s)) badLock++;
                if (!s.reverseTrue && Judge.StampOk(s, 0)) badLock++;
            }
            var ch = Words.Choices(s);
            var seen = new HashSet<string>();
            bool has = false;
            for (int i = 0; i < ch.Length; i++)
            {
                if (!seen.Add(ch[i])) badChoice++;
                if (ch[i] == Words.Answer(s)) has = true;
            }
            if (!has || ch.Length < 2) badChoice++;
            if (s.kind == Kind.AreaHyp)
            {
                int sum = s.a + s.b;
                if (sum == s.c * s.c) collide++;
                if (s.a * s.a == s.targetArea) collide++;
            }
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
                    att++;
                    chance += Chance(s);
                    if (FirstOk(s, "mash", rng, i)) { firstM++; okM++; } else livesM--;
                    if (FirstOk(s, "cyc", rng, i)) { firstC++; okC++; } else livesC--;
                    if (FirstOk(s, "rnd", rng, i)) { firstR++; okR++; } else livesR--;
                }
                nM += att; nC += att; nR += att;
                mash += firstM; cyc += firstC; rnd += firstR;
                if (okM >= Cuts && livesM > 0) mashFin++;
                if (okC >= Cuts && livesC > 0) cycFin++;
                if (okR >= Cuts && livesR > 0) rndFin++;
            }
            double ch = chance / Math.Max(1, nM);
            sb.AppendLine("## 무뇌 봇 200판 × 10장 (첫 시도 정답률)");
            sb.AppendLine("- 우연 수준 ≈ **" + Pct(ch) + "** (장 유형 혼합: 넓이 쌓기 ~0%, 길이 잠금 1/9, 역 1/4)");
            sb.AppendLine("- 연타 봇 첫시도 **" + Pct(mash / nM) + "** · 완주 " + mashFin + "/" + Games);
            sb.AppendLine("- 순환 봇 첫시도 **" + Pct(cyc / nC) + "** · 완주 " + cycFin + "/" + Games);
            sb.AppendLine("- 무작위 봇 첫시도 **" + Pct(rnd / nR) + "** · 완주 " + rndFin + "/" + Games);
            sb.AppendLine("- 무입력 봇 첫시도 **0%** · 완주 0/" + Games + " (진도 0)");
            sb.AppendLine();
            sb.AppendLine("한 줄: mash=" + Pct(mash / nM) + " cyc=" + Pct(cyc / nC) + " rnd=" + Pct(rnd / nR) + " none=0% chance≈" + Pct(ch));
        }

        static double Chance(Sheet s)
        {
            if (s.kind == Kind.Reverse) return 0.25;
            if (s.kind == Kind.AreaHyp) return 0.0; // 한 장으로 c² 를 만들 수 없음
            return 1.0 / 9.0; // 기왓장 3 × 쟁반 3
        }

        static bool FirstOk(Sheet s, string bot, Random rng, int i)
        {
            if (s.kind == Kind.Reverse)
            {
                int pick = bot == "mash" ? 0 : bot == "cyc" ? i % 4 : bot == "rnd" ? rng.Next(4) : -1;
                if (pick < 0) return false;
                int correct = s.reverseTrue ? s.rightVertex : 3;
                return pick == correct;
            }
            if (bot == "mash") return false; // 빈 쟁반 0 연타
            if (bot == "cyc") return false; // 쟁반만 순환, 넓이 0
            if (bot != "rnd") return false;
            int tile = rng.Next(3);
            int tray = rng.Next(3);
            int area = s.dock[tile];
            return Judge.Completes(s, tray, area);
        }

        static string Pct(double x) => (x * 100).ToString("0.0") + "%";
    }
}
