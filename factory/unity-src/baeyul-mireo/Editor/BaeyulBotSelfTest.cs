// 배율 밀어 — 전수 검사 + 무뇌 봇 자가 테스트. 에디터 배치 전용(빌드에 들어가지 않는다).
// 게임과 똑같은 SheetGen(문제 풀)·Judge(정수 판정)·Rules(시간·예비·손잡이 속도 상한)를 쓴다.
//   bash factory/unity/build.sh baeyul-mireo     # 워크스페이스에 소스 동기화(Editor 포함)
//   Unity -batchmode -nographics -projectPath ~/UnityProjects/MGF-Workspace \
//         -executeMethod Mgf.BaeyulMireo.BaeyulBotSelfTest.Run -bmOut /tmp/bm-bots.md -quit
using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mgf.BaeyulMireo
{
    public static class BaeyulBotSelfTest
    {
#if UNITY_EDITOR
        public static void Run()
        {
            string outPath = "bm-bots.md";
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-bmOut") outPath = a[i + 1];
            var md = RunAll();
            System.IO.File.WriteAllText(outPath, md);
            UnityEngine.Debug.Log("BM_BOTS\n" + md);
            EditorApplication.Exit(0);
        }
#endif

        public static string RunAll()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 배율 밀어 — 전수 검사 + 무뇌 봇 자가 테스트");
            sb.AppendLine();
            Exhaustive(sb);
            Bots(sb);
            return sb.ToString();
        }

        // ─────────────────────────── 1) 전수 검사
        static void Exhaustive(StringBuilder sb)
        {
            var g = new SheetGen(new Random(1));
            int sheets = 0, badTarget = 0, badUnique = 0, badStart = 0, badRange = 0, badJudge = 0, badCorr = 0, badArea = 0, badSize = 0, badRoot = 0, badDistr = 0, badIso = 0;
            var kinds = new Dictionary<string, int>();
            var prompts = new HashSet<string>();
            for (int d = 0; d < 3000; d++)
            {
                var deck = g.Deck();
                if (deck.Count != 12) badRange++;
                foreach (var s in deck)
                {
                    sheets++;
                    string key = s.order + (s.kind == Kind.Judge ? "/" + s.cond + (s.similar ? "/닮음" : "/아님") : "/" + s.shape + " " + s.m + ":" + s.n);
                    kinds[key] = kinds.TryGetValue(key, out var c) ? c + 1 : 1;
                    string p = Words.Prompt(s) + Words.Answer(s) + Words.RevealRight(s, Judge.RightAct(s)) + Words.RevealWrong(s, Act.Stamp) + Words.RevealWrong(s, Act.Discard);
                    if (p.Contains("√")) badRoot++;
                    prompts.Add(Words.Prompt(s));
                    if (s.kind == Kind.Build)
                    {
                        int G = Judge.GaugeOrig(s), T = Judge.Target(s);
                        if ((long)G * s.n % s.m != 0 || T % Rules.Step != 0 || T < s.railLo || T > s.railHi) badTarget++;
                        // 레일 칸 전수: 교차곱을 만족하는 칸은 정답 하나뿐
                        int sat = 0;
                        for (int h = s.railLo; h <= s.railHi; h += Rules.Step) { s.handle = h; if (Judge.InWindow(s)) sat++; }
                        if (sat != 1) badUnique++;
                        s.Reset();
                        if (Judge.InWindow(s) || s.handleStart < s.railLo || s.handleStart > s.railHi || s.handleStart % Rules.Step != 0) badStart++;
                        if (s.railLo < Rules.RailMin || s.railHi > Rules.RailMax || s.railLo >= s.railHi) badRange++;
                        // 찍을 도형(정답)의 모든 변이 5 mm 배수·13 cm 이하
                        for (int i = 0; i < (s.shape == Shape.Rect ? 2 : s.shape == Shape.TriBH ? 0 : 3); i++)
                        {
                            long num = (long)s.os[i] * s.n;
                            if (num % s.m != 0 || (num / s.m) % 5 != 0 || num / s.m > 130) badSize++;
                        }
                        if (s.shape == Shape.Tri && (s.os[0] == s.os[1] || s.os[1] == s.os[2] || s.os[0] == s.os[2])) badIso++;
                        if (s.order == Order.Corr)
                        {
                            // 레일 변 DE 의 대응변이 AB 가 아니고 길이도 다르다 · 오개념 값(AB 기준)은 정답이 아니다
                            if (s.gauge == 0 || s.os[s.gauge] == s.os[0] || Words.GaugeName(s) != "DE") badCorr++;
                            if (s.os[0] * s.n / s.m == T) badDistr++;
                        }
                        if (s.order == Order.Area || s.order == Order.AreaValue)
                        {
                            if (s.areaA * s.n * s.n != s.areaB * s.m * s.m) badArea++;
                            if (s.order == Order.AreaValue && s.areaA != Judge.AreaCm2(s)) badArea++;
                            long lin = (long)G * s.n * s.n;
                            if (lin % (s.m * s.m) == 0 && lin / (s.m * s.m) == T) badDistr++;
                        }
                    }
                    else
                    {
                        if (Judge.Similar(s) != s.similar || FloatSimilar(s) != s.similar) badJudge++;
                        if (!s.similar && s.misconceptionId == "") badDistr++;
                    }
                }
            }
            sb.AppendLine("## 1. 전수 검사 (생성기 3000판 = " + sheets + "장)");
            var keys = new List<string>(kinds.Keys); keys.Sort(StringComparer.Ordinal);
            var ks = new StringBuilder();
            foreach (var k in keys) ks.Append(k).Append(' ').Append(kinds[k]).Append(" · ");
            sb.AppendLine("- 종류 분포: " + ks.ToString().TrimEnd(' ', '·'));
            sb.AppendLine("- 정답 레일 길이가 5 mm 칸이 아니거나 손잡이 구간 밖 **" + badTarget + "건**");
            sb.AppendLine("- 손잡이 구간 전수: 교차곱을 만족하는 칸이 정확히 하나가 아닌 장 **" + badUnique + "건**");
            sb.AppendLine("- 시작 손잡이가 이미 정답이거나 구간 밖 **" + badStart + "건** (무입력 봇 진도 0 보장)");
            sb.AppendLine("- 손잡이 구간 이상 **" + badRange + "건** · 정답 도형 변이 5 mm 배수가 아니거나 13 cm 초과 **" + badSize + "건** · 이등변 원본 **" + badIso + "건**");
            sb.AppendLine("- 판별 장: 정수 판정(Judge.Similar) ↔ 의도 ↔ 실수 재검산 불일치 **" + badJudge + "건**");
            sb.AppendLine("- 대응 장: 레일 변 DE 의 대응변이 AB 이거나 같은 길이 **" + badCorr + "건**");
            sb.AppendLine("- 넓이 장: 넓이의 비·넓이 값이 닮음비의 제곱과 어긋남 **" + badArea + "건**");
            sb.AppendLine("- 오개념 값이 정답과 우연히 같음(대응 AB 기준·넓이 선형) 또는 「닮음 아님」 후보에 오개념 표지 없음 **" + badDistr + "건**");
            sb.AppendLine("- 발문·정답·해설에 √ **" + badRoot + "건** · 고유 발문 " + prompts.Count + "개");
            sb.AppendLine();
        }

        /// <summary>실수로 다시 잰 닮음(독립 검산 — 판정에는 쓰지 않는다).</summary>
        static bool FloatSimilar(Sheet s)
        {
            const double eps = 1e-9;
            switch (s.cond)
            {
                case Cond.SSS:
                    {
                        var o = new double[] { s.os[0], s.os[1], s.os[2] }; var c = new double[] { s.cs[0], s.cs[1], s.cs[2] };
                        Array.Sort(o); Array.Sort(c);
                        double k = c[0] / o[0];
                        return Math.Abs(c[1] / o[1] - k) < eps && Math.Abs(c[2] / o[2] - k) < eps;
                    }
                case Cond.SAS:
                    return s.oAng == s.cAng && (Math.Abs((double)s.cs[0] / s.os[0] - (double)s.cs[1] / s.os[1]) < eps || Math.Abs((double)s.cs[1] / s.os[0] - (double)s.cs[0] / s.os[1]) < eps);
                case Cond.AA:
                    {
                        var o = new List<int> { s.oA[0], s.oA[1], 180 - s.oA[0] - s.oA[1] }; var c = new List<int>();
                        // 후보는 보이는 두 각에서 셋째 각을 계산
                        int sum = 0; for (int i = 0; i < 3; i++) if ((s.cMask & (1 << i)) != 0) { c.Add(s.cA[i]); sum += s.cA[i]; }
                        c.Add(180 - sum);
                        o.Sort(); c.Sort();
                        return o[0] == c[0] && o[1] == c[1] && o[2] == c[2];
                    }
                default:
                    return Math.Abs((double)s.cs[0] / s.os[0] - (double)s.cs[1] / s.os[1]) < eps || Math.Abs((double)s.cs[1] / s.os[0] - (double)s.cs[0] / s.os[1]) < eps;
            }
        }

        // ─────────────────────────── 2) 무뇌 봇
        enum BotKind { MashStamp, MashDiscard, Cycle, Random, Idle, Positional, Ideal }
        static readonly string[] BotName = {
            "연타(항상 아래로 찍기)", "연타(항상 왼쪽으로 치우기)", "순환(손잡이 ↑3칸 · 찍기 · ↓3칸 · 치우기)",
            "무작위(손잡이 끌기·찍기·치우기)", "무입력", "우회: 글자 순서 대응·넓이 비를 닮음비로(오개념 전략)", "정답 봇(참고)" };

        class Result { public int received, firstOk, completed, runs, processedSum; public double chanceSum; }

        /// <summary>한 장에서 봇이 하는 일: 행동 목록(시간 소비 포함). 반환 = 첫 확정 행동과 그때의 손잡이.</summary>
        static void Bots(StringBuilder sb)
        {
            sb.AppendLine("## 2. 무뇌 봇 각 200판 (첫 시도 정답률 = 첫 시도 정답 장 ÷ 받은 장)");
            sb.AppendLine();
            sb.AppendLine("| 봇 | 받은 장 | 첫 시도 정답률 | 우연 수준 | 12장 완주율 | 평균 처리 장 |");
            sb.AppendLine("|---|---|---|---|---|---|");
            var rows = new List<string>();
            foreach (BotKind b in Enum.GetValues(typeof(BotKind)))
            {
                var r = RunBot(b, true);
                rows.Add(Row(b, r, true));
            }
            foreach (var row in rows) sb.AppendLine(row);
            sb.AppendLine();
            sb.AppendLine("### 2-b. 판 규칙(예비 3장·90초)을 끄고 12장을 전부 받게 했을 때 — 판별·넓이 장까지 반드시 만나게 한 측정");
            sb.AppendLine();
            sb.AppendLine("| 봇 | 받은 장 | 첫 시도 정답률 | 우연 수준 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (BotKind b in Enum.GetValues(typeof(BotKind)))
            {
                var r = RunBot(b, false);
                sb.AppendLine(Row(b, r, false));
            }
            sb.AppendLine();
            sb.AppendLine("우연 수준 = 받은 장마다 「판단 없이 찍기/치우기를 반반, 손잡이는 구간 안 무작위 칸」일 때의 첫 시도 정답 확률 평균(Rules.Chance).");
        }

        static string Row(BotKind b, Result r, bool full)
        {
            double rate = r.received > 0 ? 100.0 * r.firstOk / r.received : 0, ch = r.received > 0 ? 100.0 * r.chanceSum / r.received : 0;
            if (!full) return "| " + BotName[(int)b] + " | " + r.received + " | **" + rate.ToString("0.0") + "%** | " + ch.ToString("0.0") + "% |";
            return "| " + BotName[(int)b] + " | " + r.received + " | **" + rate.ToString("0.0") + "%** | " + ch.ToString("0.0") + "% | " + (100.0 * r.completed / r.runs).ToString("0.0") + "% | " + ((double)r.processedSum / r.runs).ToString("0.0") + " |";
        }

        static Result RunBot(BotKind b, bool rules)
        {
            var res = new Result();
            var rng = new Random(4242 + (int)b);
            var gen = new SheetGen(new Random(99 + (int)b));
            int cyc = 0;
            for (int run = 0; run < 200; run++)
            {
                res.runs++;
                var deck = gen.Deck();
                double time = Rules.RunSec;
                int spares = Rules.Spares, processed = 0;
                bool ended = false;
                foreach (var s0 in deck)
                {
                    if (ended) break;
                    var s = s0.Clone(); s.Reset();
                    res.received++; res.chanceSum += Rules.Chance(s);
                    time -= 0.45;                               // 입장 애니메이션(시계가 흐른다)
                    bool first = true, done = false;
                    double sheetLeft = s.limitSec;
                    int attempts = 0;
                    while (!done)
                    {
                        // 봇 한 번의 시도: (걸린 시간, 행동). 행동 없음 = 시간 초과까지 방치
                        Act? act = Decide(b, s, rng, ref cyc, out double spent);
                        if (act == null || spent >= sheetLeft)
                        {
                            // 시간 초과: 첫 시도 실패, 예비 −1, 다음 장
                            time -= Math.Min(sheetLeft, spent);
                            if (rules) { spares--; }
                            processed++; done = true;
                            if (rules && (spares <= 0 || time <= 0)) ended = true;
                            break;
                        }
                        time -= spent; sheetLeft -= spent;
                        if (rules && time <= 0) { ended = true; break; }
                        bool ok = Judge.Correct(s, act.Value);
                        attempts++;
                        if (first && ok) res.firstOk++;
                        if (ok) { processed++; done = true; break; }
                        if (!rules) { processed++; done = true; break; }   // 강제 모드: 첫 시도만 잰다
                        spares--;
                        if (spares <= 0) { ended = true; break; }
                        if (first) { first = false; sheetLeft = Math.Max(sheetLeft, 6); continue; }   // 같은 장 한 번 더
                        processed++; done = true;
                    }
                }
                res.processedSum += processed;
                if (processed >= 12 && !ended) res.completed++;
            }
            return res;
        }

        /// <summary>봇의 한 번의 확정 행동. spent = 그 행동까지 걸린 시간(초, 손잡이 속도 상한 포함).</summary>
        static Act? Decide(BotKind b, Sheet s, Random rng, ref int cyc, out double spent)
        {
            spent = 0.35;
            switch (b)
            {
                case BotKind.MashStamp: return Act.Stamp;
                case BotKind.MashDiscard: return Act.Discard;
                case BotKind.Idle: spent = 999; return null;
                case BotKind.Cycle:
                    for (int k = 0; k < 8; k++)
                    {
                        int step = cyc++ % 4;
                        spent += 0.3;
                        if (step == 0) { if (!s.Locked) s.handle = Math.Min(s.railHi, s.handle + 3 * Rules.Step); spent += 3 * Rules.Step / Rules.HandleSpeed; }
                        else if (step == 2) { if (!s.Locked) s.handle = Math.Max(s.railLo, s.handle - 3 * Rules.Step); spent += 3 * Rules.Step / Rules.HandleSpeed; }
                        else return step == 1 ? Act.Stamp : Act.Discard;
                    }
                    return Act.Stamp;
                case BotKind.Random:
                    for (int k = 0; k < 20; k++)
                    {
                        int r = rng.Next(3);
                        spent += 0.3;
                        if (r == 0)
                        {
                            if (s.Locked) continue;
                            int pos = s.railLo + rng.Next((s.railHi - s.railLo) / Rules.Step + 1) * Rules.Step;
                            spent += Math.Abs(pos - s.handle) / Rules.HandleSpeed;
                            s.handle = pos;
                        }
                        else return r == 1 ? Act.Stamp : Act.Discard;
                    }
                    return Act.Stamp;
                case BotKind.Positional:
                    {
                        // 오개념 전략: 대응을 글자 순서(AB↔DE)로, 넓이의 비를 닮음비로. 판별 장은 「비슷해 보이면 닮음」 → 항상 찍기
                        if (s.Locked) return Act.Stamp;
                        long w;
                        if (s.order == Order.Area || s.order == Order.AreaValue) w = (long)Judge.GaugeOrig(s) * s.n * s.n / ((long)s.m * s.m);
                        else if (s.order == Order.Corr) w = (long)s.os[0] * s.n / s.m;
                        else w = Judge.Target(s);
                        int q = (int)Math.Max(s.railLo, Math.Min(s.railHi, (w + 2) / Rules.Step * Rules.Step));
                        spent += 1.5 + Math.Abs(q - s.handle) / Rules.HandleSpeed;
                        s.handle = q;
                        return Act.Stamp;
                    }
                default:
                    {
                        // 정답 봇: 판단 3초 + 손잡이 이동
                        var a = Judge.RightAct(s);
                        spent = 3.0;
                        if (s.kind == Kind.Build) { int t = Judge.Target(s); spent += Math.Abs(t - s.handle) / Rules.HandleSpeed; s.handle = t; }
                        return a;
                    }
            }
        }
    }
}
