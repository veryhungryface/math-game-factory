// 박쥐 깨워 헤드리스 검사 — BakjwiRules.cs 를 그대로 컴파일해 (1) 은행 검산 (2) 독립 실수 검산 (3) 덱 마스크 분포
// (4) B3 무뇌 봇 200판×정책 을 돈다. 봇의 획 판정은 게임과 같은 세로 판형 기하(날개 중심·반경 52 설계 px·선분 거리)를 쓴다.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mgf.BakjwiKkaewo;

static class HeadlessCheck
{
    // 세로 판형(390×844 설계 px) 날개 중심 = 발 위치 + (0, −60)
    static readonly double[][] Centers = { new[]{98.0,540}, new[]{292.0,540}, new[]{98.0,342}, new[]{292.0,342} };
    const double HitR = 52, MinStroke = 20;

    static double SegDist(double px, double py, double ax, double ay, double bx, double by)
    {
        double dx = bx - ax, dy = by - ay, n = dx * dx + dy * dy;
        double t = n < 1e-9 ? 0 : Math.Max(0, Math.Min(1, ((px - ax) * dx + (py - ay) * dy) / n));
        double qx = ax + dx * t - px, qy = ay + dy * t - py; return Math.Sqrt(qx * qx + qy * qy);
    }

    // 획(점 열) → (마스크, 길이). 게임 HandleInput 과 같은 규칙: 지나간 선분에서 반경 안이면 접촉.
    static (int mask, double len) Stroke(List<double[]> pts)
    {
        int m = 0; double len = 0;
        for (int i = 0; i < pts.Count; i++)
        {
            var a = pts[Math.Max(0, i - 1)]; var b = pts[i];
            if (i > 0) len += Math.Sqrt((a[0]-b[0])*(a[0]-b[0]) + (a[1]-b[1])*(a[1]-b[1]));
            for (int k = 0; k < 4; k++) if (SegDist(Centers[k][0], Centers[k][1], a[0], a[1], b[0], b[1]) <= HitR) m |= 1 << k;
        }
        return (m, len);
    }

    // 날개 중심을 차례로 잇는 획. 한 마리면 날개 위를 짧게 가로지른다(탭은 제출되지 않는다).
    static List<double[]> Through(params int[] wings)
    {
        var l = new List<double[]>();
        if (wings.Length == 1) { l.Add(new[] { Centers[wings[0]][0] - 28, Centers[wings[0]][1] }); l.Add(new[] { Centers[wings[0]][0] + 28, Centers[wings[0]][1] }); return l; }
        foreach (int w in wings) l.Add(new[] { Centers[w][0], Centers[w][1] });
        return l;
    }

    // 독립 검산: 실수 삼각형(코사인 법칙)으로 세 변/세 각을 만들어 비교 — 정수 판정기와 다른 경로
    static double[] AnglesOf(SimilarityBand band, int[] v)
    {
        double a, b, c;
        if (band == SimilarityBand.AA) return new double[] { v[0], v[1], 180 - v[0] - v[1] }.OrderBy(x => x).ToArray();
        if (band == SimilarityBand.SSS) { a = v[0]; b = v[1]; c = v[2]; }
        else { a = v[0]; b = v[1]; c = Math.Sqrt(a * a + b * b - 2 * a * b * Math.Cos(v[2] * Math.PI / 180)); }
        double A = Math.Acos((b*b + c*c - a*a) / (2*b*c)) * 180 / Math.PI, B = Math.Acos((a*a + c*c - b*b) / (2*a*c)) * 180 / Math.PI;
        return new[] { A, B, 180 - A - B }.OrderBy(x => x).ToArray();
    }

    static bool IndepSimilar(BatScene s, WingData w)
    {
        var t = AnglesOf(s.band, s.target); var c = AnglesOf(s.band, w.v);
        for (int i = 0; i < 3; i++) if (Math.Abs(t[i] - c[i]) > 1e-7) return false;
        return true;
    }

    static (double lo, double hi) Wilson(int k, int n)
    {
        if (n == 0) return (0, 0);
        double z = 1.96, p = (double)k / n, d = 1 + z * z / n, c = p + z * z / (2 * n), r = z * Math.Sqrt(p * (1 - p) / n + z * z / (4.0 * n * n));
        return ((c - r) / d, (c + r) / d);
    }

    delegate List<double[]> Policy(BatScene s, int submission, Random r);

    static Dictionary<string, object> RunBot(string name, Policy pol, List<BatScene> bank, int runs, int seed)
    {
        var rng = new Random(seed);
        int firstAtt = 0, firstOk = 0, completed = 0, cleared = 0, submissions = 0, solvedSum = 0;
        for (int run = 0; run < runs; run++)
        {
            var deck = BakjwiRules.RunDeck(bank, rng);
            int lives = 3, solved = 0, fa = 0, fo = 0, aaAtt = 0, aaOk = 0, idx = 0, sub = 0; double time = BakjwiRules.RunSeconds;
            bool first = true; string end = "";
            while (true)
            {
                if (solved >= 10) { end = fo >= 8 && aaOk >= 3 ? "clear" : "mastery_fail"; break; }
                var s = deck[idx];
                var pts = pol(s, sub, rng);
                time -= 4;
                if (time <= 0) { end = "time"; break; }
                if (pts == null) continue;                    // 무입력: 시간만 흐른다
                var (mask, len) = Stroke(pts);
                if (mask == 0 || len < MinStroke) continue;    // 탭·빈 획: 제출 안 됨
                sub++; submissions++;
                bool ok = mask == s.correctMask;
                if (first) { fa++; if (s.band == SimilarityBand.AA) aaAtt++; if (ok) { fo++; if (s.band == SimilarityBand.AA) aaOk++; } }
                if (ok) { solved++; idx++; first = true; continue; }
                first = false; lives--;
                bool impossible = fo + (10 - fa) < 8 || aaOk + (4 - aaAtt) < 3;
                if (lives <= 0) { end = "feathers"; break; }
                if (impossible) { end = "mastery_impossible"; break; }
            }
            firstAtt += fa; firstOk += fo; solvedSum += solved;
            if (solved >= 10) completed++;
            if (end == "clear") cleared++;
        }
        var ci = Wilson(firstOk, firstAtt);
        return new Dictionary<string, object> {
            ["policy"] = name, ["runs"] = runs, ["firstAttempts"] = firstAtt, ["firstCorrect"] = firstOk,
            ["firstRate"] = firstAtt == 0 ? 0 : Math.Round((double)firstOk / firstAtt, 4),
            ["ci95"] = new[] { Math.Round(ci.lo, 4), Math.Round(ci.hi, 4) },
            ["chance"] = Math.Round(1.0 / 14, 4), ["completionRate"] = (double)completed / runs, ["clearRate"] = (double)cleared / runs,
            ["meanSolved"] = (double)solvedSum / runs, ["submissions"] = submissions
        };
    }

    static string J(object o)
    {
        if (o is string s) return "\"" + s.Replace("\"", "\\\"") + "\"";
        if (o is double d) return d.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
        if (o is int i) return i.ToString();
        if (o is bool b) return b ? "true" : "false";
        if (o is double[] da) return "[" + string.Join(",", da.Select(x => J(x))) + "]";
        if (o is Dictionary<string, object> m) return "{" + string.Join(",", m.Select(kv => J(kv.Key) + ":" + J(kv.Value))) + "}";
        if (o is IEnumerable<object> l) return "[" + string.Join(",", l.Select(J)) + "]";
        return o == null ? "null" : "\"" + o + "\"";
    }

    static int Main(string[] args)
    {
        var bank = BakjwiRules.BuildBank();
        var log = new StringBuilder();
        int audit = BakjwiRules.AuditBank(bank, log);
        Console.WriteLine($"bank={bank.Count} auditErrors={audit}"); if (audit > 0) Console.WriteLine(log);

        // 독립 검산 + 오답 우연 참 전수
        int indepMismatch = 0, accidental = 0, wings = 0; var misCount = new Dictionary<string, int>();
        foreach (var s in bank)
            for (int i = 0; i < 4; i++)
            {
                wings++;
                bool sim = (s.correctMask & (1 << i)) != 0;
                if (IndepSimilar(s, s.wings[i]) != sim) indepMismatch++;
                if (!sim && BakjwiRules.IsSimilar(s, s.wings[i])) accidental++;
                string id = s.wings[i].misconceptionId; if (id != "") misCount[id] = misCount.TryGetValue(id, out int c) ? c + 1 : 1;
            }
        var practice = BakjwiRules.Practice();
        int pm = BakjwiRules.ComputeMask(practice);
        Console.WriteLine($"independent mismatches={indepMismatch} accidentalTrue={accidental} wings={wings} practiceMask={pm} (expect 3)");
        Console.WriteLine("misconceptions: " + string.Join(", ", misCount.Select(kv => kv.Key + "=" + kv.Value)));
        // 근거 문장 = 판정과 일치(닮음이면 '같지 않다/다르다' 류가 나오면 안 된다)
        int evidenceBad = 0;
        foreach (var s in bank) for (int i = 0; i < 4; i++)
        {
            string e = BakjwiRules.Evidence(s, i); bool sim = (s.correctMask & (1 << i)) != 0;
            bool saysNo = e.Contains("≠") || e.Contains("다르다") || e.Contains("같지 않다");
            if (sim == saysNo && s.band != SimilarityBand.AA) evidenceBad++;
            if (sim && BakjwiRules.SimilarNotation(s, s.wings[i]) == "") evidenceBad++;
        }
        Console.WriteLine($"evidenceMismatch={evidenceBad}");

        // 덱 분포(1만 판): 단계별 마스크 빈도
        var drng = new Random(1); var freq = new int[3, 15]; var sizeFreq = new int[5];
        for (int r = 0; r < 10000; r++) foreach (var s in BakjwiRules.RunDeck(bank, drng)) { freq[(int)s.band - 1, s.correctMask]++; int n = 0; for (int k = 0; k < 4; k++) if ((s.correctMask >> k & 1) == 1) n++; sizeFreq[n]++; }
        for (int b = 0; b < 3; b++) { var row = new List<string>(); for (int m = 1; m <= 14; m++) row.Add(freq[b, m].ToString()); Console.WriteLine($"band{b + 1} mask freq: " + string.Join(" ", row)); }
        Console.WriteLine($"correct-set size freq 1/2/3: {sizeFreq[1]}/{sizeFreq[2]}/{sizeFreq[3]} (기대 4:6:4)");

        // 표본 20문항 반복 체감: 같은 기준·같은 날개 수치 조합 3회 이상 여부
        var rr = new Random(5); int repeatRuns = 0;
        for (int r = 0; r < 200; r++) { var d = BakjwiRules.RunDeck(bank, rr).Concat(BakjwiRules.RunDeck(bank, rr)).ToList(); if (d.GroupBy(x => string.Join(",", x.target)).Any(g => g.Count() >= 3 && g.Select(x => x.id).Distinct().Count() < 3)) repeatRuns++; }

        // B3 봇
        Policy mash = (s, k, r) => new List<double[]> { new[] { 98.0, 540 }, new[] { 99.0, 541 } };               // 연타: 같은 자리 탭
        Policy fixedStroke = (s, k, r) => Through(0, 1);                                                          // 연타형 고정 획(1→2)
        int[][] cyc = { new[] { 0, 1 }, new[] { 1, 3 }, new[] { 3, 2 }, new[] { 2, 0 } };
        Policy cycle = (s, k, r) => Through(cyc[k % 4]);                                                          // 순환
        Policy random = (s, k, r) => { var l = new List<double[]>(); int n = 2 + r.Next(3); for (int i = 0; i < n; i++) l.Add(new[] { r.NextDouble() * 390, 150 + r.NextDouble() * 650 }); return l; };
        Policy all4 = (s, k, r) => Through(0, 1, 3, 2);                                                           // 네 마리 전부
        Policy none = (s, k, r) => null;                                                                         // 무입력
        // 실루엣 정책: 수치를 읽지 않고 그림의 세 각만 눈대중(±3°)으로 비교해 닮아 보이는 날개를 쓴다
        Func<double, Policy> silhouetteAt = tol => (s, k, r) =>
        {
            var t = AnglesOf(s.band, s.target); var pick = new List<int>();
            for (int i = 0; i < 4; i++) { var c = AnglesOf(s.band, s.wings[i].v); if (Enumerable.Range(0, 3).All(j => Math.Abs(t[j] - c[j]) <= tol)) pick.Add(i); }
            if (pick.Count == 0 || pick.Count == 4) { pick.Clear(); pick.Add(r.Next(4)); }
            return Through(pick.ToArray());
        };
        // 정답 정책(상한 확인): 판정기와 같은 집합
        Policy oracle = (s, k, r) => { var p = new List<int>(); for (int i = 0; i < 4; i++) if ((s.correctMask >> i & 1) == 1) p.Add(i); return Through(p.ToArray()); };

        var results = new List<object>();
        results.Add(RunBot("mash-tap", mash, bank, 200, 11));
        results.Add(RunBot("fixed-stroke-1-2", fixedStroke, bank, 200, 12));
        results.Add(RunBot("cycle", cycle, bank, 200, 13));
        results.Add(RunBot("random", random, bank, 200, 14));
        results.Add(RunBot("all-four", all4, bank, 200, 15));
        results.Add(RunBot("no-input", none, bank, 200, 16));
        results.Add(RunBot("silhouette-1deg", silhouetteAt(1.0), bank, 200, 17));
        results.Add(RunBot("silhouette-2deg", silhouetteAt(2.0), bank, 200, 19));
        results.Add(RunBot("silhouette-4deg", silhouetteAt(4.0), bank, 200, 20));
        results.Add(RunBot("oracle", oracle, bank, 200, 18));
        // 정밀도 보강: 같은 정책 2000판
        results.Add(RunBot("fixed-stroke-1-2 x2000", fixedStroke, bank, 2000, 21));
        results.Add(RunBot("cycle x2000", cycle, bank, 2000, 22));
        results.Add(RunBot("random x2000", random, bank, 2000, 23));
        foreach (Dictionary<string, object> r in results) Console.WriteLine(J(r));

        // 경로 존재: 정답 집합 14개 모두 다른 날개를 스치지 않는 획이 있는가(직선 연결 순서 중 하나라도)
        int noPath = 0;
        for (int m = 1; m <= 14; m++)
        {
            var w = Enumerable.Range(0, 4).Where(i => (m >> i & 1) == 1).ToArray(); bool found = false;
            foreach (var perm in Perms(w)) { var (mk, _) = Stroke(Through(perm)); if (mk == m) { found = true; break; } }
            if (!found) { noPath++; Console.WriteLine("직선 경로로 못 만드는 마스크 " + m + " (우회 곡선 필요)"); }
        }

        var outp = new Dictionary<string, object> {
            ["bank"] = bank.Count, ["auditErrors"] = audit, ["independentMismatch"] = indepMismatch, ["accidentalTrue"] = accidental,
            ["evidenceMismatch"] = evidenceBad, ["practiceMask"] = pm, ["masksWithoutStraightPath"] = noPath,
            ["repeatTargets3x_in_20"] = repeatRuns, ["bots"] = results
        };
        if (args.Length > 0) File.WriteAllText(args[0], J(outp));
        return audit == 0 && indepMismatch == 0 && accidental == 0 && evidenceBad == 0 ? 0 : 1;
    }

    static IEnumerable<int[]> Perms(int[] a)
    {
        if (a.Length <= 1) { yield return a; yield break; }
        for (int i = 0; i < a.Length; i++)
            foreach (var rest in Perms(a.Where((_, j) => j != i).ToArray()))
                yield return new[] { a[i] }.Concat(rest).ToArray();
    }
}
