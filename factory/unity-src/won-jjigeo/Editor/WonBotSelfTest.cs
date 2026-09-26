// 원 찍어 v3 — 전수 증명 + 무뇌 봇 자가 테스트. 에디터 배치 전용(빌드에 들어가지 않는다).
// v3: 다리 단계 전수 증명(발자국 판정 ⇔ 이등분선 위) + 2단계 작도 봇(무작위 탭·변만·꼭짓점만·무입력·박기 연타·반대·정답).
// 게임과 똑같은 Gen(풀)·Prob(정수 판정)·Rules(속도·제한 시간)를 쓴다. UnityEngine 에 기대지 않아 .NET 콘솔로도 돌릴 수 있다:
//   Unity -batchmode -nographics -projectPath <ws> -executeMethod Mgf.WonJjigeo.WonBotSelfTest.Run -wonOut <md> -quit
//
// 1) 표시 전수 증명: 격자 전체 × 풀 전체(모양 × 가능한 모든 평행이동)에서
//    「화면 길이 표시(소수 둘째 자리)가 같다 ⇔ 정수 판정으로 같다」 불일치 0건, 세 표시가 모두 같은 점 = 정답 하나뿐.
// 2) 다리 단계: 모든 배치 × 격자 전체에서 「발자국 판정(정수) ⇔ 그 이등분선 위(∠A 안쪽)」, 생성 삼각형 격자점 ≥ 4.
// 3) 2단계 작도: 올바른 종류 두 선의 교점 = 정답(정수), 반대 종류 선은 정답을 지나지 않음.
// 4) 봇: 1단계(거리선) 연타·순환·무작위·무입력 + 격자 훑기·추론, 다리 단계 무작위 찍기, 2단계(작도) 탭 봇.
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
                    for (int i = 0; i < 3; i++) { p.V[i] = O + off[i]; if (!Rules.InBoard(p.V[i]) || p.V[i].Same(Rules.Start)) ok = false; }
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
            return $"- 2단계 문항 {n}개: 올바른 종류 선(외심=수직이등분선, 내심=각의 이등분선)이 정답을 안 지남 + 반대 종류 선이 정답을 지남 + 올바른 두 선의 교점≠정답 + 빗변 중점≠외심 = **{bad}건** · 세 선이 한 점에서 만남 {conc}/{n}\n";
        }

        // ─────────────────────────── 2') 다리 단계 전수 증명
        /// <summary>외심 다리(부등변 외심 모양 전부) · 내심 다리(내심 모양 전부) × 격자 안 모든 배치 × 격자점 255개:
        /// 「BridgeOk(정수 판정) ⇔ 이등분선(정수 직선) 위 [+ 내심은 ∠A 안쪽]」 불일치 0건이면, 발자국 3개는 언제나 그 이등분선 위에 있다.
        /// 게임이 실제로 쓰는 Gen.MakeBridge 명판 2000장은 격자점 ≥ Rules.BridgeMinPts 와 세 발자국 공선(교차곱 0)을 다시 확인한다.</summary>
        public static string ProveBridge(Gen gen, Random rng, out long bad)
        {
            long mis = 0, probs = 0, pts = 0, fewer = 0; int minPts = int.MaxValue, maxPts = 0;
            void Check(Prob p)
            {
                probs++;
                var L = p.BridgeLine();
                for (int x = -Rules.GX; x <= Rules.GX; x++)
                    for (int y = -Rules.GY; y <= Rules.GY; y++)
                    {
                        var q = RP.Of(new IP(x, y)); pts++;
                        bool on = L.Through(q) && (p.m == Mission.Circum || p.InsideAngleA(q));
                        if (p.BridgeOk(q) != on) mis++;
                        // 화면: 두 길이 표시가 같다 ⇔ 정수로 같다(초록 표시가 거짓말하지 않는다)
                        int j = p.m == Mission.Circum ? 1 : 2;
                        if ((p.DistH(q, 0, p.m) == p.DistH(q, j, p.m)) != p.EqualPair(q, 0, j, p.m)) mis++;
                    }
            }
            foreach (var list in new[] { gen.oAcute, gen.oRight, gen.oObtuse })
                foreach (var s in list)
                {
                    if (s.iso) continue;
                    for (int r = 0; r < 3; r++)
                        ForEachPlacement(Rot(s.a, s.b, s.c, r, 0), Rot(s.a, s.b, s.c, r, 1), Rot(s.a, s.b, s.c, r, 2), Mission.Circum, p => { p.R2 = s.R2; Check(p); });
                }
            foreach (var s in gen.iShapes)
            {
                if (s.iso) continue;
                for (int r = 0; r < 3; r++)
                    ForEachPlacement(Rot(s.a, s.b, s.c, r, 0), Rot(s.a, s.b, s.c, r, 1), Rot(s.a, s.b, s.c, r, 2), Mission.In, p => { p.inR = s.r; if (p.DisplayConsistent()) Check(p); });
            }
            long gameBad = 0;
            for (int k = 0; k < 2000; k++)
            {
                var p = gen.MakeBridge(k % 2 == 0 ? Mission.Circum : Mission.In, rng);
                var list = p.BridgePoints();
                minPts = Math.Min(minPts, list.Count); maxPts = Math.Max(maxPts, list.Count);
                if (list.Count < Rules.BridgeMinPts) fewer++;
                // 발자국 후보 중 아무 셋이나(처음·가운데·끝) — 한 직선 위이고 이등분선 위
                var a = list[0]; var b = list[list.Count / 2]; var c = list[list.Count - 1];
                if (Geo.Cross(b - a, c - a) != 0) gameBad++;
                foreach (var q in new[] { a, b, c }) if (!p.BridgeLine().Through(RP.Of(q))) gameBad++;
            }
            bad = mis + fewer + gameBad;
            return $"- 다리 단계 배치 {probs:N0}개(외심 다리 = 부등변 외심 모양 × 이름 회전 3 × 평행이동, 내심 다리 = 내심 모양 × 이름 회전 3 × 평행이동) × 격자점 255개 = {pts:N0}점\n" +
                   $"  - 「발자국 판정(정수) ⇔ 이등분선 위(내심 다리는 ∠A 안쪽)」 불일치 + 「두 길이 표시가 같다 ⇔ 정수로 같다」 불일치: **{mis}건**\n" +
                   $"- 게임 생성기 Gen.MakeBridge 명판 2000장: 발자국 자리(판 안 격자점, 출발점 제외) 최소 {minPts}개 · 최대 {maxPts}개 · {Rules.BridgeMinPts}개 미만 **{fewer}건** · 세 발자국 공선/이등분선 위 아님 **{gameBad}건**\n";
        }

        static IP Rot(IP a, IP b, IP c, int r, int i) { var o = new[] { a, b, c }; return o[(i + r) % 3]; }

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

        // 2단계 — 작도: 게임과 같은 규칙. 탭 대상 6곳(0..2 = 꼭짓점 → ∠k 의 이등분선, 3..5 = 변 k → 수직이등분선)과 빈 곳.
        // 같은 선 두 번은 거절, 선은 3개까지, 판 안 교점이 생기면 핀이 그 교점으로 옮겨 서고 봇은 곧바로 박는다(교점이 없으면 박기가 거절된다).
        static Line Cons(Prob p, int h) => h < 3 ? p.AngleBisector(h) : p.PerpBisector(h - 3, (h - 3 + 1) % 3);

        /// <summary>탭 순서(-1 = 빈 곳)대로 선을 긋다가 판 안 교점이 처음 생기면 박는다. 반환: 정답 여부. planted=false 면 박지 못했다(시간 초과).</summary>
        static bool PlayTaps(Prob p, IEnumerable<int> taps, out bool planted, out double t)
        {
            var drawn = new List<int>(); planted = false; t = Think;
            foreach (int h in taps)
            {
                t += TapCost;
                if (h < 0 || drawn.Contains(h)) continue;
                if (drawn.Count >= Rules.MaxLines) break;
                drawn.Add(h);
                var L = Cons(p, h);
                for (int i = 0; i < drawn.Count - 1; i++)
                    if (Prob.Meet(Cons(p, drawn[i]), L, out var q) && Math.Abs(q.fx) <= Rules.GX + 0.5 && Math.Abs(q.fy) <= Rules.GY + 0.5)
                    { planted = true; t += React; return q.Is(p.ans); }
                if (t > Rules.T2) break;
            }
            return false;
        }

        const double TapCost = 0.6;
        delegate bool Stage2Bot(Prob p, Random rng, int k, out double t);
        static IEnumerable<int> RandTaps(Random rng, bool withEmpty, int from, int count)
        { for (int n = 0; n < 60; n++) yield return withEmpty && rng.Next(4) == 0 ? -1 : from + rng.Next(count); }
        static bool RandTap(Prob p, Random rng, int k, out double t) => PlayTaps(p, RandTaps(rng, true, 0, 6), out _, out t) && t <= Rules.T2;
        static bool SidesOnly(Prob p, Random rng, int k, out double t) => PlayTaps(p, RandTaps(rng, false, 3, 3), out _, out t) && t <= Rules.T2;
        static bool VertsOnly(Prob p, Random rng, int k, out double t) => PlayTaps(p, RandTaps(rng, false, 0, 3), out _, out t) && t <= Rules.T2;
        static bool Opposite(Prob p, Random rng, int k, out double t) => PlayTaps(p, RandTaps(rng, false, p.m == Mission.Circum ? 0 : 3, 3), out _, out t) && t <= Rules.T2;
        static bool RightCons(Prob p, Random rng, int k, out double t) => PlayTaps(p, RandTaps(rng, false, p.m == Mission.Circum ? 3 : 0, 3), out _, out t) && t <= Rules.T2;
        static IEnumerable<int> CycleSeq(int k) { for (int n = 0; n < 12; n++) yield return (k + n) % 6; }
        static bool Cycle2(Prob p, Random rng, int k, out double t) => PlayTaps(p, CycleSeq(k), out _, out t) && t <= Rules.T2;
        static bool Mash2(Prob p, Random rng, int k, out double t) { t = Rules.T2; return false; }   // 선 없이 박기만 연타 → 무감점 거절만 반복 → 시간 초과
        static bool Idle2(Prob p, Random rng, int k, out double t) { t = Rules.T2; return false; }

        // 다리 단계: 한 번 찍을 때마다 아무 격자점(무작위 찍기) — 틀려도 목숨은 잃지 않는다. 발자국 3개까지 찍은 횟수를 센다.
        static int BridgeRandomStamps(Prob p, Random rng)
        {
            var ok = new HashSet<long>(); int stamps = 0;
            foreach (var q in p.BridgePoints()) ok.Add(q.x * 100L + q.y);
            var got = new HashSet<long>();
            while (got.Count < Rules.BridgeDots && stamps < 100000)
            {
                stamps++;
                var q = new IP(rng.Next(-Rules.GX, Rules.GX + 1), rng.Next(-Rules.GY, Rules.GY + 1));
                long key = q.x * 100L + q.y;
                if (ok.Contains(key) && p.BridgeOk(RP.Of(q))) got.Add(key);
            }
            return stamps;
        }

        public static string RunAll()
        {
            var gen = new Gen();
            var sb = new StringBuilder();
            sb.AppendLine("## 1. 「화면 길이 표시가 같다 ⇔ 실제로 같다」 전수 증명(1단계)");
            sb.Append(ProveDisplay(gen, out long mm));
            sb.AppendLine();
            sb.AppendLine("## 2. 다리 단계 전수 증명 — 발자국 3개는 언제나 그 이등분선 위");
            sb.Append(ProveBridge(gen, new Random(11), out long bb));
            sb.AppendLine();
            sb.AppendLine("## 3. 2단계 작도 선 검산");
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
            sb.AppendLine($"## 4. 1단계(거리선) 봇 — 명판 3000장 · 제한 {Rules.T1:0}초 · 핀 속도 상한 {Rules.StepRate:0}걸음/초 · 판단 {Think}초 + 반응 {React}초");
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
            var inf = new Tally(); var sw = new Tally(); var brng2 = new Random(3);
            foreach (var p in s1) { Sweep(p, brng2, 0, 1e9, out double ts); sw.Add(true, ts); bool okI = Infer(p, brng2, 0, 1e9, out double ti); inf.Add(okI, ti); }
            sb.AppendLine();
            sb.AppendLine($"- 제한 시간이 없을 때 걸리는 시간: 격자 훑기 평균 {sw.MeanT:0.0}초 · 추론 평균 {inf.MeanT:0.0}초(추론이 막히지 않고 정답에 닿는 비율 {inf.Rate * 100:0.0}%)");
            sb.AppendLine();

            // 다리 단계 봇
            sb.AppendLine("## 5. 다리 단계 봇 — 명판 2000장(외심 다리·내심 다리 반씩) · 목숨·제한 시간 없음(학습 단계)");
            {
                var br = new Random(77); long stampsSum = 0; long hitPts = 0; int n = 0; int worst = 0;
                var bpl = new List<Prob>(); for (int k = 0; k < 2000; k++) bpl.Add(gen.MakeBridge(k % 2 == 0 ? Mission.Circum : Mission.In, br));
                foreach (var p in bpl) { int st = BridgeRandomStamps(p, br); stampsSum += st; worst = Math.Max(worst, st); hitPts += p.BridgePoints().Count; n++; }
                double perStamp = (double)hitPts / n / 255.0;
                sb.AppendLine($"- 무작위 찍기 봇(아무 격자점에 「점 찍기」): 한 번 찍어 발자국이 남을 확률 평균 {perStamp * 100:0.0}% (= 판마다 발자국 자리 수/255 — 우연 수준 그 자체) · 발자국 3개까지 평균 {(double)stampsSum / n:0.0}번 · 최악 {worst}번");
                sb.AppendLine("- 다리 단계는 첫 시도 정답률·목숨에 들어가지 않는다(점수 +20/발자국만). 무작위로 찍어 넘기면 2단계에 도착해도 거리선이 없어 아래 5·6의 우연 수준으로 떨어진다.");
                sb.AppendLine("- 무입력: 진도 0(핀이 출발점에 서 있고 발자국 자리가 아니면 아무 일도 없다 — 출발점은 발자국 자리에서 뺐다).");
            }
            sb.AppendLine();

            // 2단계
            var s2 = new List<Prob>(); for (int k = 0; k < 3000; k++) s2.Add(gen.Next(2, 2 + k % 4, rng));
            var bots2 = new List<(string, string, Stage2Bot, string)>
            {
                ("박기 연타", "선 없이 「핀 박기」만(무감점 거절 → 시간 초과)", Mash2, "0%"), ("무입력", "방치", Idle2, "0%"),
                ("무작위 탭", "꼭짓점 3·변 3·빈 곳을 무작위로 → 첫 교점에 박기", RandTap, "20%"),
                ("순환 탭", "탭 대상을 차례로 돌며", Cycle2, "20%"),
                ("변만", "미션과 상관없이 늘 변(수직이등분선)", SidesOnly, "50%"),
                ("꼭짓점만", "미션과 상관없이 늘 꼭짓점(각의 이등분선)", VertsOnly, "50%"),
                ("반대로", "외심↔내심 혼동(M1)", Opposite, "—"),
                ("정답 작도(상한)", "외심=변, 내심=꼭짓점", RightCons, "—"),
            };
            sb.AppendLine($"## 6. 2단계(작도) 봇 — 명판 3000장(안내 명판 제외: 외심 직각·둔각, 내심 교대) · 제한 {Rules.T2:0}초 · 거리선 없음 · 탭 {TapCost}초");
            sb.AppendLine("우연 수준: 서로 다른 선 두 개를 6가지 작도에서 무작위로 고를 때 둘 다 맞는 종류 = 3/6 × 2/5 = 20% · 「종류」 이진 선택 = 50%");
            sb.AppendLine();
            sb.AppendLine("| 봇 | 전략 | 첫 시도 정답률 | 외심 | 내심 | 비교 기준 |");
            sb.AppendLine("|---|---|---|---|---|---|");
            var r2 = new Dictionary<string, double>();
            foreach (var (name, desc, bot, cmp) in bots2)
            {
                var all = new Tally(); var o = new Tally(); var i = new Tally(); var brng = new Random(name.GetHashCode());
                for (int k = 0; k < s2.Count; k++)
                {
                    var p = s2[k];
                    bool ok = bot(p, brng, k, out double t) && t <= Rules.T2;
                    all.Add(ok, t); (p.m == Mission.Circum ? o : i).Add(ok, t);
                }
                r2[name] = all.Rate;
                sb.AppendLine($"| {name} | {desc} | {all.Rate * 100:0.0}% | {o.Rate * 100:0.0}% | {i.Rate * 100:0.0}% | {cmp} |");
            }
            sb.AppendLine();

            // 7) 판 전체(200판): 1단계(해금까지) → 다리 단계(2판, 무작위 찍기로도 통과) → 2단계 6장.
            sb.AppendLine("## 7. 판 전체 200판 — 무뇌 봇 + 기준 봇 (다리 단계는 발자국 3개씩 찍어 통과, 목숨·정답률 무관)");
            sb.AppendLine($"승리 연출 = 완주 + 첫 시도 정답률(전체) ≥ {Rules.ClearAccuracy * 100:0}% + 2단계만의 첫 시도 정답률 ≥ {Rules.ClearAccuracy * 100:0}%");
            sb.AppendLine("| 봇 | 판당 명판 | 첫 시도 정답률 | 다리 단계 도달 | 2단계 도달 | 완주(승리 연출) |");
            sb.AppendLine("|---|---|---|---|---|---|");
            var runBots = new List<(string, Stage1Bot, Stage2Bot)>
            {
                ("연타", Mash, Mash2), ("순환", Cycle, Cycle2), ("무작위", RandomPlant, RandTap), ("무입력", Idle, Idle2),
                ("격자 훑기 + 무작위 탭", Sweep, RandTap), ("추론 + 변만", Infer, SidesOnly), ("추론 + 정답 작도", Infer, RightCons),
            };
            foreach (var (name, b1, b2) in runBots)
            {
                int plates = 0, first = 0, reachB = 0, reach2 = 0, clear = 0; var brng = new Random(name.GetHashCode()); var g = new Gen();
                for (int game = 0; game < 200; game++)
                {
                    int lives = Rules.Lives, stage = 1, idx = 0, sO = 0, sI = 0, gp = 0, gok = 0, gp2 = 0, gok2 = 0; bool stalled = false;
                    while (lives > 0)
                    {
                        var p = g.Next(stage, idx, brng);
                        bool tut = p.tutorial;
                        double lim = tut ? 1e9 : stage == 1 ? Rules.T1 : Rules.T2;
                        bool ok; double t;
                        if (stage == 1) { if (b1 == Idle && tut && idx == 0) { stalled = true; break; } ok = b1(p, brng, idx, lim, out t); }
                        else { ok = b2(p, brng, idx, out t) && (tut || t <= lim); if (b2 == Idle2 && tut) { stalled = true; break; } }
                        gp++; if (ok) gok++;
                        if (stage == 2) { gp2++; if (ok) gok2++; }
                        if (!ok && tut) { for (int r = 0; r < 2 && !ok; r++) ok = stage == 1 ? b1(p, brng, idx + 7, lim, out t) : b2(p, brng, idx + 7, out t); }
                        if (!ok && !tut) lives--;
                        if (stage == 1)
                        {
                            if (p.m == Mission.Circum) sO = ok ? sO + 1 : 0; else sI = ok ? sI + 1 : 0;
                            idx++;
                            if (sO >= Rules.UnlockStreak && sI >= Rules.UnlockStreak)
                            {
                                reachB++;
                                // 다리 단계 2판: 무입력이 아니면 찍다 보면 통과(목숨 무관)
                                g.MakeBridge(Mission.Circum, brng); g.MakeBridge(Mission.In, brng);
                                stage = 2; idx = 0; reach2++;
                            }
                        }
                        else { idx++; if (idx >= Rules.Stage2Plates) break; }
                    }
                    plates += gp; first += gok;
                    if (!stalled && lives > 0 && stage == 2 && idx >= Rules.Stage2Plates && gp > 0 && (double)gok / gp >= Rules.ClearAccuracy && gp2 > 0 && (double)gok2 / gp2 >= Rules.ClearAccuracy) clear++;
                }
                sb.AppendLine($"| {name} | {(double)plates / 200:0.0} | {(plates > 0 ? (double)first / plates * 100 : 0):0.0}% | {reachB / 2.0:0}% | {reach2 / 2.0:0}% | {clear / 2.0:0}% |");
            }
            sb.AppendLine();
            sb.AppendLine($"판정: 표시 불일치 {mm}건 · 다리 단계 불일치 {bb}건 · 작도 선 불일치 {fb}건 · 1단계 훑기 {r1["격자 훑기"] * 100:0.0}% vs 추론 {r1["추론"] * 100:0.0}% · 2단계 무작위 탭 {r2["무작위 탭"] * 100:0.0}% (우연 20%) · 변만 {r2["변만"] * 100:0.0}% · 꼭짓점만 {r2["꼭짓점만"] * 100:0.0}% (우연 50%)");
            return sb.ToString();
        }
    }
}
