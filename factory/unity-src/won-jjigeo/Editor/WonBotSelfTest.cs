// 원 찍어 v2 — 전수 증명 + 무뇌 봇 자가 테스트. 에디터 배치 전용(빌드에 들어가지 않는다).
// 게임과 똑같은 Gen(풀)·Prob(정수 판정)·Rules(속도·제한 시간)를 쓴다. UnityEngine 에 기대지 않아 .NET 콘솔로도 돌릴 수 있다:
//   Unity -batchmode -nographics -projectPath <ws> -executeMethod Mgf.WonJjigeo.WonBotSelfTest.Run -wonOut <md> -quit
//
// 1) 표시 전수 증명: 격자 전체 × 풀 전체(모양 × 가능한 모든 평행이동)에서
//    「화면 길이 표시(소수 둘째 자리)가 같다 ⇔ 정수 판정으로 같다」 불일치 0건, 세 표시가 모두 같은 점 = 정답 하나뿐.
// 2) 2단계 주름: 올바른 종류 두 주름의 교점 = 정답(정수), 반대 종류 주름은 정답을 지나지 않음.
// 3) 봇: 1단계(거리선) 연타·순환·무작위·무입력 + 격자 훑기·추론, 2단계(접기) 무작위 접기·고정 종류·반대 종류·정답 접기.
//    시간 모델 = 핀 이동 속도 상한 Rules.StepRate(걸음/초) + 명판 제한 시간 Rules.T1/T2 + 판단·반응 지연.
using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mgf.WonJjigeo
{
    public static class WonBotSelfTest
    {
#if UNITY_EDITOR
        public static void Run()
        {
            string outPath = "won-bots.md";
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-wonOut") outPath = a[i + 1];
            var md = RunAll();
            System.IO.File.WriteAllText(outPath, md);
            UnityEngine.Debug.Log("WON_BOTS\n" + md);
            EditorApplication.Exit(0);
        }
#endif

        const double Think = 0.8, React = 0.3, FoldCost = 1.6;

        static int KingDist(IP a, IP b) => Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));

        // ─────────────────────────── 1) 표시 전수 증명
        public static string ProveDisplay(Gen gen, out long mismatches)
        {
            var sb = new StringBuilder();
            mismatches = 0;
            // (a) 꼭짓점 거리: 정수 거리² n ∈ [0, 452] 의 표시 문자열이 서로 다르다
            int maxN = (2 * Rules.GX) * (2 * Rules.GX) + (2 * Rules.GY) * (2 * Rules.GY);
            var seen = new Dictionary<string, long>(); long dupN = 0;
            for (long n = 0; n <= maxN; n++) { var s = Geo.Fmt(Geo.HundredthsSqrt(n, 1)); if (seen.ContainsKey(s)) dupN++; else seen[s] = n; }
            // 반올림 정확성: 정수 반올림 = 실수 반올림(경계 0.5 에 걸린 값이 없는지 포함)
            long roundBad = 0;
            for (long n = 0; n <= maxN; n++) { double v = Math.Sqrt(n) * 100; if (Math.Abs(v - Math.Floor(v) - 0.5) > 1e-6 && (long)Math.Round(v, MidpointRounding.AwayFromZero) != Geo.HundredthsSqrt(n, 1)) roundBad++; }
            sb.AppendLine($"- 꼭짓점 거리² 0~{maxN} 의 둘째 자리 표시 {maxN + 1}종: 중복 {dupN}건 · 정수 반올림≠실수 반올림 {roundBad}건");
            mismatches += dupN + roundBad;

            // (b) 풀 전체 × 모든 평행이동 × 격자 전체
            long probs = 0, pts = 0, pairBad = 0, redBad = 0, iRejected = 0;
            void Check(Prob p)
            {
                probs++;
                int reds = 0;
                for (int x = -Rules.GX; x <= Rules.GX; x++)
                    for (int y = -Rules.GY; y <= Rules.GY; y++)
                    {
                        var q = RP.Of(new IP(x, y)); pts++;
                        var h = new long[3];
                        for (int i = 0; i < 3; i++) h[i] = p.DistH(q, i, p.m);
                        for (int i = 0; i < 3; i++)
                        {
                            int j = (i + 1) % 3;
                            bool shown = Geo.Fmt(h[i]) == Geo.Fmt(h[j]);
                            if (shown != p.EqualPair(q, i, j, p.m)) pairBad++;
                        }
                        bool allShown = h[0] == h[1] && h[1] == h[2];
                        if (allShown) { reds++; if (!q.Is(p.ans)) redBad++; }
                    }
                if (reds != 1) redBad++;
            }
            foreach (var list in new[] { gen.oAcute, gen.oRight, gen.oObtuse })
                foreach (var s in list)
                    ForEachPlacement(s.a, s.b, s.c, Mission.Circum, p => { p.R2 = s.R2; Check(p); });
            foreach (var s in gen.iShapes)
                ForEachPlacement(s.a, s.b, s.c, Mission.In, p => { p.inR = s.r; if (!p.DisplayConsistent()) { iRejected++; return; } Check(p); });
            sb.AppendLine($"- 풀 전체: 외심 모양 {gen.oAcute.Count + gen.oRight.Count + gen.oObtuse.Count}개(예각 {gen.oAcute.Count}·직각 {gen.oRight.Count}·둔각 {gen.oObtuse.Count}) · 내심 모양 {gen.iShapes.Count}개");
            sb.AppendLine($"- 모양 × 격자 안 모든 평행이동 = 문항 {probs:N0}개 × 격자점 255개 = {pts:N0}점 × 거리 쌍 3개");
            sb.AppendLine($"  - 「표시가 같다 ⇔ 정수로 같다」 불일치: **{pairBad}건**");
            sb.AppendLine($"  - 세 표시가 모두 같은 격자점이 정답 하나가 아닌 문항: **{redBad}건** (내심 배치 중 방심이 격자점이거나 표시가 겹쳐 게임이 쓰지 않는 배치 {iRejected:N0}개는 제외 — 게임도 같은 DisplayConsistent() 로 거른다)");
            mismatches += pairBad + redBad;
            return sb.ToString();
        }

        static void ForEachPlacement(IP a, IP b, IP c, Mission m, Action<Prob> f)
        {
            var off = new[] { a, b, c };
            for (int ox = -Rules.GX; ox <= Rules.GX; ox++)
                for (int oy = -Rules.GY; oy <= Rules.GY; oy++)
                {
                    var O = new IP(ox, oy); bool ok = true;
                    var p = new Prob { m = m, stage = 1, ans = O };
                    for (int i = 0; i < 3; i++) { p.V[i] = O + off[i]; if (!Rules.InGrid(p.V[i]) || p.V[i].Same(Rules.Start)) ok = false; }
                    if (!ok || (O - Rules.Start).Len2 < 16) continue;
                    p.Prepare();
                    f(p);
                }
        }

        // ─────────────────────────── 2) 주름 검산
        public static string ProveFolds(Gen gen, Random rng, out long bad)
        {
            bad = 0; int n = 0, conc = 0;
            for (int k = 0; k < 3000; k++)
            {
                var p = gen.Next(2, k % 6, rng); n++;
                var right = new List<Line>(); var wrong = new List<Line>();
                for (int i = 0; i < 3; i++)
                {
                    var pb = p.PerpBisector(i, (i + 1) % 3); var ab = p.AngleBisector(i);
                    (p.m == Mission.Circum ? right : wrong).Add(pb);
                    (p.m == Mission.Circum ? wrong : right).Add(ab);
                }
                var A = RP.Of(p.ans);
                foreach (var l in right) if (!l.Through(A)) bad++;
                foreach (var l in wrong) if (l.Through(A)) bad++;
                for (int i = 0; i < 3; i++) { if (!Prob.Meet(right[i], right[(i + 1) % 3], out var q) || !q.Is(p.ans)) bad++; }
                if (Prob.Meet(right[0], right[1], out var q2) && right[2].Through(q2)) conc++;
                // 직각삼각형 외심 = 빗변의 수직이등분선이 빗변과 만나는 점(빗변의 중점)
                if (p.m == Mission.Circum && p.rightV >= 0) { int i = (p.rightV + 1) % 3, j = (p.rightV + 2) % 3; if (!p.SideMid(i, j).Is(p.ans)) bad++; }
            }
            return $"- 2단계 문항 {n}개: 올바른 종류 주름이 정답을 안 지남 + 반대 종류 주름이 정답을 지남 + 두 주름 교점≠정답 + 빗변 중점≠외심 = **{bad}건** · 세 주름이 한 점에서 만남 {conc}/{n}\n";
        }

        // ─────────────────────────── 3) 봇
        class Tally { public int plates, ok; public double timeSum; public void Add(bool s, double t) { plates++; if (s) { ok++; timeSum += t; } } public double Rate => plates > 0 ? (double)ok / plates : 0; public double MeanT => ok > 0 ? timeSum / ok : double.NaN; }

        /// <summary>1단계 한 장: 봇이 받는 정보는 게임 화면과 같다(세 길이 표시와 빨간색 여부). 반환: 성공 여부·걸린 시간.</summary>
        delegate bool Stage1Bot(Prob p, Random rng, int plateNo, double limit, out double t);

        static double Spread(Prob p, IP q)
        {
            var r = RP.Of(q); double mx = double.MinValue, mn = double.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                double v;
                if (p.m == Mission.Circum) v = p.DistD(r, i, p.m);
                else v = -p.SideL(r, i) / Math.Sqrt(p.sN[i]);            // 부호 있는 거리(반시계 삼각형 안쪽이 +)
                mx = Math.Max(mx, v); mn = Math.Min(mn, v);
            }
            return mx - mn;
        }

        static bool Red(Prob p, IP q) => p.AllEqual(RP.Of(q), p.m);

        static bool Mash(Prob p, Random rng, int k, double lim, out double t) { t = React; return Red(p, Rules.Start); }
        static readonly IP[] cyc = { new IP(-4, 3), new IP(4, 3), new IP(4, -3), new IP(-4, -3) };
        static bool Cycle(Prob p, Random rng, int k, double lim, out double t)
        { var g = cyc[k % 4]; t = Think + KingDist(Rules.Start, g) / Rules.StepRate + React; return t <= lim && Red(p, g); }
        static bool RandomPlant(Prob p, Random rng, int k, double lim, out double t)
        {
            var g = new IP(rng.Next(-Rules.GX, Rules.GX + 1), rng.Next(-Rules.GY, Rules.GY + 1));
            t = Think + KingDist(Rules.Start, g) / Rules.StepRate + React; return t <= lim && Red(p, g);
        }
        static bool Idle(Prob p, Random rng, int k, double lim, out double t) { t = lim; return false; }

        /// <summary>격자 훑기: 출발점에서 아래 줄을 왼쪽 끝까지 → 한 줄씩 올라가며 지그재그로 격자 전체를 쓸다가 빨간색이 뜨면 박는다.</summary>
        static bool Sweep(Prob p, Random rng, int k, double lim, out double t)
        {
            var q = Rules.Start; double steps = 0;
            bool Visit(IP a) { steps += KingDist(q, a); q = a; return Red(p, a); }
            if (Red(p, q)) { t = React; return true; }
            for (int x = q.x - 1; x >= -Rules.GX; x--) if (Visit(new IP(x, -Rules.GY))) goto hit;
            bool right = true;
            for (int y = -Rules.GY + 1; y <= Rules.GY; y++, right = !right)
            {
                if (right) { for (int x = -Rules.GX; x <= Rules.GX; x++) if (Visit(new IP(x, y))) goto hit; }
                else { for (int x = Rules.GX; x >= -Rules.GX; x--) if (Visit(new IP(x, y))) goto hit; }
            }
            for (int x = 1; x <= Rules.GX; x++) if (Visit(new IP(x, -Rules.GY))) goto hit;
            t = lim; return false;
        hit:
            t = steps / Rules.StepRate + React;
            return t <= lim;
        }

        /// <summary>(참고) 삼각형 안 훑기: 내심은 삼각형 안에 있다는 것만 알고 삼각형 안 격자점을 줄마다 훑는다.</summary>
        static bool SweepInside(Prob p, Random rng, int k, double lim, out double t)
        {
            var q = Rules.Start; double steps = 0; bool right = true;
            int y0 = Math.Min(p.V[0].y, Math.Min(p.V[1].y, p.V[2].y)), y1 = Math.Max(p.V[0].y, Math.Max(p.V[1].y, p.V[2].y));
            for (int y = y0; y <= y1; y++, right = !right)
                for (int i = 0; i <= 2 * Rules.GX; i++)
                {
                    var a = new IP(right ? -Rules.GX + i : Rules.GX - i, y);
                    if (!p.InsideStrict(a)) continue;
                    steps += KingDist(q, a); q = a;
                    if (Red(p, a)) { t = Think + steps / Rules.StepRate + React; return t <= lim; }
                }
            t = lim; return false;
        }

        /// <summary>추론: 가장 가까운 꼭짓점(변)에서 멀어지는 쪽 — 세 길이의 차(최대−최소)가 줄어드는 이웃 칸으로 한 걸음씩.
        /// 한 걸음 이웃에 나아지는 칸이 없으면 두 칸 거리까지 본다. 걸음마다 0.15초 판단 지연.</summary>
        static bool Infer(Prob p, Random rng, int k, double lim, out double t)
        {
            var q = Rules.Start; double time = Think;
            for (int guard = 0; guard < 200; guard++)
            {
                if (Red(p, q)) { t = time + React; return t <= lim; }
                double cur = Spread(p, q); IP best = q; double bv = cur;
                for (int r = 1; r <= 2 && best.Same(q); r++)
                    for (int dx = -r; dx <= r; dx++)
                        for (int dy = -r; dy <= r; dy++)
                        {
                            var nq = new IP(q.x + dx, q.y + dy);
                            if ((dx == 0 && dy == 0) || !Rules.InGrid(nq)) continue;
                            double v = Spread(p, nq);
                            if (v < bv - 1e-12) { bv = v; best = nq; }
                        }
                if (best.Same(q)) break;
                time += KingDist(q, best) / Rules.StepRate + 0.15;
                q = best;
                if (time > lim) break;
            }
            t = lim; return false;
        }

        // 2단계 — 접기 6종: 0..2 = 꼭짓점 i 를 i+1 에 포개기(수직이등분선), 3..5 = ∠(k) 의 두 변 포개기(각의 이등분선)
        static Line Fold(Prob p, int f) => f < 3 ? p.PerpBisector(f, (f + 1) % 3) : p.AngleBisector(f - 3);

        static bool PlantAtMeet(Prob p, Random rng, int f1, int f2, out double t)
        {
            t = Think + 2 * FoldCost;
            if (Prob.Meet(Fold(p, f1), Fold(p, f2), out var q) && q.exact)
            {
                t += (Math.Abs(q.fx) + Math.Abs(q.fy)) / Rules.StepRate * 0.5 + React;
                return q.Is(p.ans);
            }
            return false;   // 무리수 교점(반대 종류 각의 이등분선이 낀 교점)은 격자점 정답과 같을 수 없다
        }

        delegate bool Stage2Bot(Prob p, Random rng, int k, out double t);
        static bool RandFold(Prob p, Random rng, int k, out double t) { int a = rng.Next(6), b; do b = rng.Next(6); while (b == a); return PlantAtMeet(p, rng, a, b, out t); }
        static bool VertexOnly(Prob p, Random rng, int k, out double t) { int a = rng.Next(3), b; do b = rng.Next(3); while (b == a); return PlantAtMeet(p, rng, a, b, out t); }
        static bool Opposite(Prob p, Random rng, int k, out double t)
        { int a = rng.Next(3), b; do b = rng.Next(3); while (b == a); int o = p.m == Mission.Circum ? 3 : 0; return PlantAtMeet(p, rng, a + o, b + o, out t); }
        static bool RightFold(Prob p, Random rng, int k, out double t)
        { int a = rng.Next(3), b; do b = rng.Next(3); while (b == a); int o = p.m == Mission.Circum ? 0 : 3; return PlantAtMeet(p, rng, a + o, b + o, out t); }
        static bool Mash2(Prob p, Random rng, int k, out double t) { t = React; return RP.Of(Rules.Start).Is(p.ans); }
        static bool Cycle2(Prob p, Random rng, int k, out double t) { t = 2; return RP.Of(cyc[k % 4]).Is(p.ans); }
        static bool Rand2(Prob p, Random rng, int k, out double t) { t = 2; return RP.Of(new IP(rng.Next(-Rules.GX, Rules.GX + 1), rng.Next(-Rules.GY, Rules.GY + 1))).Is(p.ans); }
        static bool Idle2(Prob p, Random rng, int k, out double t) { t = Rules.T2; return false; }

        public static string RunAll()
        {
            var gen = new Gen();
            var sb = new StringBuilder();
            sb.AppendLine("## 1. 「화면 길이 표시가 같다 ⇔ 실제로 같다」 전수 증명");
            sb.Append(ProveDisplay(gen, out long mm));
            sb.AppendLine();
            sb.AppendLine("## 2. 2단계 주름 검산");
            sb.Append(ProveFolds(gen, new Random(5), out long fb));
            sb.AppendLine();

            // 1단계 대표본: 온보딩 첫 명판을 뺀 1단계 명판 3000장(내심·외심 예각·직각·둔각 순환)
            var rng = new Random(20260926);
            var s1 = new List<Prob>(); for (int k = 0; k < 3000; k++) s1.Add(gen.Next(1, 1 + k % 6, rng));
            var bots1 = new List<(string, string, Stage1Bot)>
            {
                ("연타", "출발점에서 바로 박기", Mash), ("순환", "네 점을 돌아가며", Cycle), ("무작위", "아무 격자점", RandomPlant), ("무입력", "방치", Idle),
                ("격자 훑기", "격자 전체를 지그재그로 쓸다가 빨간색이 뜨면 박기", Sweep),
                ("추론", "가장 가까운 꼭짓점(변)에서 멀어지는 쪽으로", Infer),
                ("(참고) 삼각형 안 훑기", "삼각형 안 격자점만 쓸기", SweepInside),
            };
            double chance1 = 1.0 / ((2 * Rules.GX + 1) * (2 * Rules.GY + 1));
            sb.AppendLine($"## 3. 1단계(거리선) 봇 — 명판 3000장 · 제한 {Rules.T1:0}초 · 핀 속도 상한 {Rules.StepRate:0}걸음/초 · 판단 {Think}초 + 반응 {React}초");
            sb.AppendLine($"우연 수준(격자점 하나를 찍어 맞힐 확률) = 1/255 = {chance1 * 100:0.00}%. 설계 기준: 훑기 봇 성공률이 추론 봇보다 확실히 낮아야 한다.");
            sb.AppendLine();
            sb.AppendLine("| 봇 | 전략 | 첫 시도 정답률 | 외심 | 내심 | 성공까지 평균 |");
            sb.AppendLine("|---|---|---|---|---|---|");
            var r1 = new Dictionary<string, double>();
            foreach (var (name, desc, bot) in bots1)
            {
                var all = new Tally(); var o = new Tally(); var i = new Tally(); var brng = new Random(name.GetHashCode());
                for (int k = 0; k < s1.Count; k++)
                {
                    var p = s1[k];
                    bool ok = bot(p, brng, k, Rules.T1, out double t);
                    all.Add(ok, t); (p.m == Mission.Circum ? o : i).Add(ok, t);
                }
                r1[name] = all.Rate;
                sb.AppendLine($"| {name} | {desc} | {all.Rate * 100:0.0}% | {o.Rate * 100:0.0}% | {i.Rate * 100:0.0}% | {(all.ok > 0 ? all.MeanT.ToString("0.0") + "초" : "—")} |");
            }
            // 훑기 봇이 시간 제한 없이 걸리는 시간(참고)
            var inf = new Tally(); var sw = new Tally(); var brng2 = new Random(3);
            foreach (var p in s1) { Sweep(p, brng2, 0, 1e9, out double ts); sw.Add(true, ts); bool okI = Infer(p, brng2, 0, 1e9, out double ti); inf.Add(okI, ti); }
            sb.AppendLine();
            sb.AppendLine($"- 제한 시간이 없을 때 걸리는 시간: 격자 훑기 평균 {sw.MeanT:0.0}초 · 추론 평균 {inf.MeanT:0.0}초(추론이 막히지 않고 정답에 닿는 비율 {inf.Rate * 100:0.0}%)");
            sb.AppendLine();

            // 2단계
            var s2 = new List<Prob>(); for (int k = 0; k < 3000; k++) s2.Add(gen.Next(2, 2 + k % 4, rng));
            var bots2 = new List<(string, string, Stage2Bot)>
            {
                ("연타", "출발점에 바로 박기", Mash2), ("순환", "네 점을 돌아가며", Cycle2), ("무작위", "아무 격자점", Rand2), ("무입력", "방치", Idle2),
                ("무작위 접기", "접기 6종(꼭짓점 쌍 3·각 3) 중 둘을 무작위로 → 교점에 박기", RandFold),
                ("꼭짓점만 접기", "미션과 상관없이 늘 꼭짓점끼리", VertexOnly),
                ("반대로 접기", "외심↔내심 혼동(M1)", Opposite),
                ("정답 접기(상한)", "외심=꼭짓점끼리, 내심=변끼리", RightFold),
            };
            sb.AppendLine($"## 4. 2단계(접기) 봇 — 명판 3000장(안내 명판 제외) · 제한 {Rules.T2:0}초 · 거리선 없음");
            sb.AppendLine("우연 수준: 아무 격자점 = 1/255 = 0.39% · 두 번의 접기 종류를 무작위로 고를 때 둘 다 맞을 확률 = 1/2 × 1/2 = 25%");
            sb.AppendLine();
            sb.AppendLine("| 봇 | 전략 | 첫 시도 정답률 | 외심 | 내심 | 비교 기준 |");
            sb.AppendLine("|---|---|---|---|---|---|");
            var r2 = new Dictionary<string, double>();
            foreach (var (name, desc, bot) in bots2)
            {
                var all = new Tally(); var o = new Tally(); var i = new Tally(); var brng = new Random(name.GetHashCode());
                for (int k = 0; k < s2.Count; k++)
                {
                    var p = s2[k];
                    bool ok = bot(p, brng, k, out double t) && t <= Rules.T2;
                    all.Add(ok, t); (p.m == Mission.Circum ? o : i).Add(ok, t);
                }
                r2[name] = all.Rate;
                string cmp = name.Contains("접기") && !name.Contains("정답") ? "25%" : name.Contains("정답") ? "—" : "0.39%";
                sb.AppendLine($"| {name} | {desc} | {all.Rate * 100:0.0}% | {o.Rate * 100:0.0}% | {i.Rate * 100:0.0}% | {cmp} |");
            }
            sb.AppendLine();

            // 5) 판 전체(200판): 1단계(해금까지) → 2단계 6장. 안내 명판은 제한 시간 없음·오답 무감점 재시도 2회.
            sb.AppendLine("## 5. 판 전체 200판 — 무뇌 봇 4종 + 기준 봇");
            sb.AppendLine("| 봇 | 판당 명판 | 첫 시도 정답률 | 2단계 도달 | 완주(승리 연출) |");
            sb.AppendLine("|---|---|---|---|---|");
            var runBots = new List<(string, Stage1Bot, Stage2Bot)>
            {
                ("연타", Mash, Mash2), ("순환", Cycle, Cycle2), ("무작위", RandomPlant, Rand2), ("무입력", Idle, Idle2),
                ("격자 훑기 + 무작위 접기", Sweep, RandFold), ("추론 + 정답 접기", Infer, RightFold),
            };
            foreach (var (name, b1, b2) in runBots)
            {
                int plates = 0, first = 0, reach2 = 0, clear = 0; var brng = new Random(name.GetHashCode()); var g = new Gen();
                for (int game = 0; game < 200; game++)
                {
                    int lives = Rules.Lives, stage = 1, idx = 0, sO = 0, sI = 0, gp = 0, gok = 0; bool stalled = false;
                    while (lives > 0)
                    {
                        var p = g.Next(stage, idx, brng);
                        bool tut = p.tutorial;
                        double lim = tut && stage == 1 ? 1e9 : stage == 1 ? Rules.T1 : Rules.T2;
                        bool ok; double t;
                        if (stage == 1) { if (b1 == Idle && tut && idx == 0) { stalled = true; break; } ok = b1(p, brng, idx, lim, out t); }
                        else ok = b2(p, brng, idx, out t) && t <= lim;
                        gp++; if (ok) gok++;
                        if (!ok && tut) { for (int r = 0; r < 2 && !ok; r++) ok = stage == 1 ? b1(p, brng, idx + 7, lim, out t) : b2(p, brng, idx + 7, out t); }
                        if (!ok && !tut) lives--;
                        if (stage == 1)
                        {
                            if (p.m == Mission.Circum) sO = ok ? sO + 1 : 0; else sI = ok ? sI + 1 : 0;
                            idx++;
                            if (sO >= Rules.UnlockStreak && sI >= Rules.UnlockStreak) { stage = 2; idx = 0; reach2++; }
                        }
                        else { idx++; if (idx >= Rules.Stage2Plates) break; }
                    }
                    plates += gp; first += gok;
                    if (!stalled && lives > 0 && stage == 2 && idx >= Rules.Stage2Plates && gp > 0 && (double)gok / gp >= Rules.ClearAccuracy) clear++;
                }
                sb.AppendLine($"| {name} | {(double)plates / 200:0.0} | {(plates > 0 ? (double)first / plates * 100 : 0):0.0}% | {reach2 / 2.0:0}% | {clear / 2.0:0}% |");
            }
            sb.AppendLine();
            sb.AppendLine($"판정: 표시 불일치 {mm}건 · 주름 불일치 {fb}건 · 1단계 훑기 {r1["격자 훑기"] * 100:0.0}% vs 추론 {r1["추론"] * 100:0.0}% · 2단계 무작위 접기 {r2["무작위 접기"] * 100:0.0}% (우연 25%)");
            return sb.ToString();
        }
    }
}
