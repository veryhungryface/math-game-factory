// 유리칼 — 전수 검사 + 무뇌 봇 자가 테스트. 에디터 배치 전용(빌드에 들어가지 않는다).
// 게임과 똑같은 SheetGen(문제 풀)·Judge(정수 판정)·Rules(시간·O 속도 상한)를 쓴다.
//   bash factory/unity/build.sh yuri-kal     # 워크스페이스에 소스 동기화(Editor 포함)
//   Unity -batchmode -nographics -projectPath ~/UnityProjects/MGF-Workspace \
//         -executeMethod Mgf.YuriKal.YuriBotSelfTest.Run -ykOut /tmp/yk-bots.md -quit
using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mgf.YuriKal
{
    public static class YuriBotSelfTest
    {
#if UNITY_EDITOR
        public static void Run()
        {
            string outPath = "yk-bots.md";
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-ykOut") outPath = a[i + 1];
            var md = RunAll();
            System.IO.File.WriteAllText(outPath, md);
            UnityEngine.Debug.Log("YK_BOTS\n" + md);
            EditorApplication.Exit(0);
        }
#endif
        const double Enter = 0.45;

        public static string RunAll()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 유리칼 — 전수 검사 + 무뇌 봇 자가 테스트");
            sb.AppendLine();
            Exhaustive(sb);
            Bots(sb);
            return sb.ToString();
        }

        // ─────────────────────────── 1) 전수 검사
        static void Exhaustive(StringBuilder sb)
        {
            var g = new SheetGen(new Random(1));
            int sheets = 0, badUnique = 0, badPossible = 0, badStart = 0, badGeom = 0, badArea = 0, badConvex = 0, points = 0;
            var kinds = new Dictionary<string, int>();
            for (int d = 0; d < 3000; d++)
                foreach (var s0 in g.Deck())
                {
                    sheets++;
                    string key = (s0.Locked ? s0.lk.ToString() : "Build") + "/" + s0.order + (s0.areaOrder ? "/넓이" : "");
                    kinds[key] = kinds.TryGetValue(key, out var c) ? c + 1 : 1;
                    var s = s0.Clone();
                    // 시작 상태는 주문을 만족하지 않는다(무입력 0%)
                    s.Reset();
                    if (!s.Locked && Judge.Satisfies(s)) badStart++;
                    // 만족하는 격자점 × 교각 전수
                    int sat = 0;
                    if (!s.Locked)
                    {
                        var ths = new List<int>();
                        if (s.canRotate) for (int t = Judge.RotMin; t <= Judge.RotMax; t += Judge.RotStep) ths.Add(t); else ths.Add(s.startTheta);
                        foreach (int th in ths)
                            for (int a = Judge.Margin; a <= s.L1 - Judge.Margin; a += Judge.Grid)
                                for (int b = Judge.Margin; b <= s.L2 - Judge.Margin; b += Judge.Grid)
                                {
                                    s.s1 = a; s.s2 = b; s.theta = th; points++;
                                    if (Judge.Satisfies(s))
                                    {
                                        sat++;
                                        if (2 * a != s.L1 || 2 * b != s.L2) badUnique++;
                                    }
                                }
                        s.Reset();
                        if ((sat > 0) != Judge.Possible(s)) badPossible++;
                        if (sat > 1) badUnique++;
                    }
                    // 기하(실수 검산 — 판정에는 쓰지 않는다): 볼록 · 잠긴 장의 조건 표시가 참인지
                    Geo(s, out var A, out var B, out var C, out var D);
                    if (!Convex(A, B, C, D)) badConvex++;
                    if (!CheckLock(s, A, B, C, D)) badGeom++;
                    if (s.areaOrder)
                    {
                        var t = s.Clone(); t.s1 = t.L1 / 2; t.s2 = t.L2 / 2;
                        string tot = Judge.TotalAreaText(t);
                        if (tot == null || tot.Contains(".")) badArea++;
                    }
                }
            sb.AppendLine("## 1. 전수 검사 (생성기 3000판 = " + sheets + "장)");
            sb.AppendLine("- 종류 분포: " + string.Join(" · ", Sorted(kinds)));
            sb.AppendLine("- 구성 장 격자점 × 교각 전수 " + points.ToString("N0") + "개: 주문을 만족하는 점이 이등분점 하나뿐이 아닌 경우 **" + badUnique + "건**");
            sb.AppendLine("- `Judge.Possible`(치우기 정답 판정) ↔ 전수 탐색 불일치 **" + badPossible + "건**");
            sb.AppendLine("- 시작 상태가 이미 정답인 장 **" + badStart + "건** (무입력 봇 진도 0 보장)");
            sb.AppendLine("- 볼록이 아닌 사각형 **" + badConvex + "건** · 잠긴 장 조건 표시(AB∥DC, AD = BC, 대각, 수직, 이등분)가 실제 기하와 어긋남 **" + badGeom + "건**");
            sb.AppendLine("- 넓이 주문의 전체 넓이가 정수 cm²가 아닌 장 **" + badArea + "건**");
            sb.AppendLine();
        }

        static IEnumerable<string> Sorted(Dictionary<string, int> d)
        {
            var l = new List<string>(); foreach (var kv in d) l.Add(kv.Key + " " + kv.Value); l.Sort(); return l;
        }

        struct V { public double x, y; public V(double a, double b) { x = a; y = b; } public static V operator -(V a, V b) => new V(a.x - b.x, a.y - b.y); public double Len => Math.Sqrt(x * x + y * y); }
        static double Cross(V a, V b) => a.x * b.y - a.y * b.x;
        static double Dot(V a, V b) => a.x * b.x + a.y * b.y;

        static void Geo(Sheet s, out V A, out V B, out V C, out V D)
        {
            double p = s.phi * Math.PI / 180, q = (s.phi + s.theta) * Math.PI / 180;
            var u1 = new V(Math.Cos(p), Math.Sin(p)); var u2 = new V(Math.Cos(q), Math.Sin(q));
            A = new V(-s.s1 * u1.x, -s.s1 * u1.y); C = new V(s.CO * u1.x, s.CO * u1.y);
            B = new V(-s.s2 * u2.x, -s.s2 * u2.y); D = new V(s.DO * u2.x, s.DO * u2.y);
        }

        static bool Convex(V A, V B, V C, V D)
        {
            V[] q = { A, B, C, D }; int sign = 0;
            for (int i = 0; i < 4; i++)
            {
                double c = Cross(q[(i + 1) % 4] - q[i], q[(i + 2) % 4] - q[(i + 1) % 4]);
                int sg = Math.Sign(c); if (sg == 0) return false;
                if (sign == 0) sign = sg; else if (sg != sign) return false;
            }
            return true;
        }

        static bool Par(V a, V b) => Math.Abs(Cross(a, b)) < 1e-6 * a.Len * b.Len;
        static bool Eq(double a, double b) => Math.Abs(a - b) < 1e-6;
        static double Ang(V v, V p, V q) => Math.Acos(Dot(p - v, q - v) / ((p - v).Len * (q - v).Len));

        static bool CheckLock(Sheet s, V A, V B, V C, V D)
        {
            switch (s.lk)
            {
                case Lock.CondG: return Par(B - A, C - D) && Eq((B - A).Len, (C - D).Len);
                case Lock.Trapezoid: return Par(B - A, C - D) && Eq((D - A).Len, (C - B).Len) && !Eq((B - A).Len, (C - D).Len) && !Par(D - A, C - B) && !Judge.Bisect(s);
                case Lock.CondD: return Eq(Ang(A, B, D), Ang(C, B, D)) && Eq(Ang(B, A, C), Ang(D, A, C));
                case Lock.CondR: case Lock.ParaUneq: return Judge.Bisect(s) && (s.lk != Lock.ParaUneq || s.L1 != s.L2);
                case Lock.Kite: return s.theta == 90 && s.s1 == s.CO && s.s2 != s.DO;
                case Lock.Square: return s.theta == 90 && Judge.Bisect(s) && s.L1 == s.L2;
                case Lock.Rhombus: return s.theta == 90 && Judge.Bisect(s) && s.L1 != s.L2;
            }
            return true;
        }

        // ─────────────────────────── 2) 무뇌 봇
        interface IBot { void Sheet(Sheet s, Random r); double NextDt(); Act? Step(Sheet s, Random r, out double cost); }

        class Result { public int runs, attempts, first, processed, complete, decisions; public double chanceSum; }

        /// <summary>한 판 시뮬레이션: 90초 · 장당 제한 · 예비 3장 · 오답 뒤 같은 장 1회 재시도. 반환 첫 시도 정답 수 등.</summary>
        static void Play(Func<IBot> make, Random r, Result res)
        {
            var bot = make();
            var deck = new SheetGen(r).Deck();
            double run = Rules.RunSec; int spares = Rules.Spares, processed = 0;
            foreach (var s in deck)
            {
                if (run <= 0 || spares <= 0) break;
                s.Reset();
                run -= Enter; if (run <= 0) break;
                res.attempts++;
                res.chanceSum += Chance(s);
                bool first = true, done = false; double left = s.limitSec;
                bot.Sheet(s, r);
                while (!done)
                {
                    double dt = bot.NextDt();
                    Act? act = bot.Step(s, r, out double cost);
                    double used = dt + cost;
                    left -= used; run -= used;
                    if (run <= 0) { done = true; break; }
                    if (left <= 0) { spares--; processed++; done = true; break; }   // 시간 초과(첫 시도 오답)
                    if (act == null) continue;
                    bool ok = Judge.Correct(s, act.Value);
                    if (ok) { if (first) res.first++; processed++; done = true; }
                    else
                    {
                        spares--;
                        if (!first || spares <= 0) { processed++; done = true; }
                        first = false;
                        left = Math.Max(left, 5);
                    }
                }
            }
            res.runs++; res.processed += processed;
            if (processed >= 12) res.complete++;
        }

        /// <summary>우연 수준: 판단 없이 무작위 이진 확정(위/아래 50%)을 했을 때의 기대 정답률. 구성 장(만들 수 있는 장)은
        /// 무작위 O 가 이등분점(격자 한 점)에 떨어질 확률 ≈ 0 이므로 0 으로 둔다.</summary>
        static double Chance(Sheet s)
        {
            if (s.Locked) return 0.5;
            if (!Judge.Possible(s)) return 0.5;
            int n = ((s.L1 - 2 * Judge.Margin) / Judge.Grid + 1) * ((s.L2 - 2 * Judge.Margin) / Judge.Grid + 1);
            return 0.5 / n;
        }

        class Spam : IBot { public void Sheet(Sheet s, Random r) { } public double NextDt() => 0.3; public Act? Step(Sheet s, Random r, out double c) { c = 0; return Act.Split; } }
        class SpamDown : IBot { public void Sheet(Sheet s, Random r) { } public double NextDt() => 0.3; public Act? Step(Sheet s, Random r, out double c) { c = 0; return Act.Scrap; } }
        class Idle : IBot { public void Sheet(Sheet s, Random r) { } public double NextDt() => 1.0; public Act? Step(Sheet s, Random r, out double c) { c = 0; return null; } }
        class Cycle : IBot
        {
            int k;
            public void Sheet(Sheet s, Random r) { }
            public double NextDt() => 0.4;
            public Act? Step(Sheet s, Random r, out double c)
            {
                c = 0; int m = k++ % 4;
                if (m == 0) { if (!s.Locked) s.s1 = Judge.SnapS(s.s1 + 5, s.L1); return null; }
                if (m == 1) return Act.Split;
                if (m == 2) { if (!s.Locked) s.s2 = Judge.SnapS(s.s2 + 5, s.L2); return null; }
                return Act.Scrap;
            }
        }
        class RandomBot : IBot
        {
            public void Sheet(Sheet s, Random r) { }
            public double NextDt() => 0.4;
            public Act? Step(Sheet s, Random r, out double c)
            {
                c = 0; int m = r.Next(4);
                if (m == 0 && !s.Locked)
                {
                    // 매트(16 cm × 10 cm) 위 무작위 점으로 끈다 — 속도 상한만큼 시간이 든다
                    double px = (r.NextDouble() - 0.5) * 160, py = (r.NextDouble() - 0.5) * 100;
                    double p = s.phi * Math.PI / 180, q = (s.phi + s.theta) * Math.PI / 180;
                    double ux = Math.Cos(p), uy = Math.Sin(p), vx = Math.Cos(q), vy = Math.Sin(q), det = ux * vy - uy * vx;
                    double a = (px * vy - py * vx) / det + s.L1 / 2.0, b = (ux * py - uy * px) / det + s.L2 / 2.0;
                    int na = Judge.SnapS(a, s.L1), nb = Judge.SnapS(b, s.L2);
                    double dist = Math.Sqrt(Math.Pow(na - s.s1, 2) + Math.Pow(nb - s.s2, 2)) / 10.0;
                    c = dist / Rules.OSpeed;
                    s.s1 = na; s.s2 = nb;
                    return null;
                }
                if (m == 1 && s.canRotate) { s.theta = Judge.SnapTheta(Judge.RotMin + r.NextDouble() * (Judge.RotMax - Judge.RotMin)); c = 0.3; return null; }
                if (m == 0 || m == 1) return null;
                return m == 2 ? Act.Split : Act.Scrap;
            }
        }
        /// <summary>화면 공개 정보만 쓰는 우회 전략: 「잠긴 장 → 쪼개기, 구성 장 → 치우기」(수학 없이 잠금 표시만 본다).</summary>
        class LockHeuristic : IBot
        {
            public void Sheet(Sheet s, Random r) { }
            public double NextDt() => 0.5;
            public Act? Step(Sheet s, Random r, out double c) { c = 0; return s.Locked ? Act.Split : Act.Scrap; }
        }
        /// <summary>정답 봇(판단 1.2초 + O 를 이등분점까지 끄는 시간 + 필요하면 회전 0.8초).</summary>
        class Oracle : IBot
        {
            bool moved;
            public void Sheet(Sheet s, Random r) { moved = false; }
            public double NextDt() => 1.2;
            public Act? Step(Sheet s, Random r, out double c)
            {
                c = 0;
                var a = Judge.RightAct(s);
                if (a == Act.Split && !s.Locked && !moved)
                {
                    double dist = Math.Sqrt(Math.Pow(s.L1 / 2 - s.s1, 2) + Math.Pow(s.L2 / 2 - s.s2, 2)) / 10.0;
                    c = dist / Rules.OSpeed + 1.0 + (s.canRotate ? 0.8 : 0);
                    Judge.SolveInto(s); moved = true;
                }
                return a;
            }
        }

        static void Bots(StringBuilder sb)
        {
            var bots = new (string name, Func<IBot> make)[]
            {
                ("연타(항상 위로 쪼개기)", () => new Spam()),
                ("연타(항상 아래로 치우기)", () => new SpamDown()),
                ("순환(O→ · 위 · O↑ · 아래)", () => new Cycle()),
                ("무작위(끌기·돌리기·위·아래)", () => new RandomBot()),
                ("무입력", () => new Idle()),
                ("우회: 잠김→위, 아니면 아래", () => new LockHeuristic()),
                ("정답 봇(참고)", () => new Oracle()),
            };
            sb.AppendLine("## 2. 무뇌 봇 각 200판 (첫 시도 정답률 = 첫 시도 정답 장 ÷ 받은 장)");
            sb.AppendLine();
            sb.AppendLine("| 봇 | 받은 장 | 첫 시도 정답률 | 우연 수준 | 12장 완주율 | 평균 처리 장 |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var b in bots)
            {
                var res = new Result();
                var r = new Random(4242);
                for (int i = 0; i < 200; i++) Play(b.make, r, res);
                double rate = res.attempts > 0 ? 100.0 * res.first / res.attempts : 0;
                double chance = res.attempts > 0 ? 100.0 * res.chanceSum / res.attempts : 0;
                sb.AppendLine("| " + b.name + " | " + res.attempts + " | **" + rate.ToString("0.0") + "%** | " + chance.ToString("0.0") + "% | " +
                              (100.0 * res.complete / res.runs).ToString("0.0") + "% | " + ((double)res.processed / res.runs).ToString("0.0") + " |");
            }
            sb.AppendLine();
            sb.AppendLine("### 2-b. 판 규칙(예비 3장·90초)을 끄고 12장을 전부 받게 했을 때 — 판정 장까지 반드시 만나게 한 측정");
            sb.AppendLine();
            sb.AppendLine("| 봇 | 받은 장 | 첫 시도 정답률 | 우연 수준 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var b in bots)
            {
                var r = new Random(99); int n = 0, ok = 0; double ch = 0;
                for (int i = 0; i < 200; i++)
                {
                    var bot = b.make();
                    foreach (var s in new SheetGen(r).Deck())
                    {
                        s.Reset(); bot.Sheet(s, r); n++; ch += Chance(s);
                        double left = s.limitSec; Act? act = null;
                        while (left > 0 && act == null) { left -= bot.NextDt(); act = bot.Step(s, r, out double c); left -= c; if (left <= 0) act = null; }
                        if (act != null && Judge.Correct(s, act.Value)) ok++;
                    }
                }
                sb.AppendLine("| " + b.name + " | " + n + " | **" + (100.0 * ok / n).ToString("0.0") + "%** | " + (100.0 * ch / n).ToString("0.0") + "% |");
            }
            sb.AppendLine();
            sb.AppendLine("우연 수준 = 받은 장마다 「판단 없이 위/아래를 반반으로 확정」했을 때의 기대 정답률 평균(구성 장은 무작위 O 가 이등분점 한 칸에 떨어질 확률).");
        }
    }
}
