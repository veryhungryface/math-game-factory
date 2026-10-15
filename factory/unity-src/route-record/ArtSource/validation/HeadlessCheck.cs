// 루트 레코드 헤드리스 검산 + B3 무뇌 봇 (Unity 없이 번들 Roslyn/.NET 6 으로 실행 — run-headless.sh).
// 게임과 똑같은 RouteRecordRules.cs(생성기·판정·밴드 선택)를 그대로 컴파일해 쓴다.
//  1) 은행: 문항 수·핵심 문장 다양성·법적 상태 ≥24·균등 우연율 ≤1/8·무뇌 배치 정답 0건
//  2) 독립 검산: 실제 노치 부분집합 전수(2^노치 수) × 그래프 DFS 로 센 경로 수 == PathCount
//  3) 오답 진단 우연 참: 정답 배치에 붙는 오개념 문구 0건, 진단 문구가 실제 규칙과 모순 0건
//  4) B3: 정책 8종 × 200판 — 첫 시도 정답률 vs 우연 수준(그 판이 만난 문항들의 균등 상태 우연율 평균)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Mgf.RouteRecord
{
    static class HeadlessCheck
    {
        const int Runs = 1000;   // 정책마다 1000판(B3 하한 200판 이상)
        static int Main(string[] args)
        {
            var cat = RouteRecordRules.BuildCatalog();
            var sb = new StringBuilder();
            sb.Append("{\n");
            bool ok = true;

            // ── 1) 은행
            var cores = new HashSet<string>(cat.Select(p => p.prompt));
            int minLegal = cat.Min(p => p.legalStateCount);
            double maxChance = cat.Max(p => (double)p.validStateCount / p.legalStateCount);
            double meanChance = cat.Average(p => (double)p.validStateCount / p.legalStateCount);
            var naive = new List<int[]>();
            int naiveHits = 0;
            foreach (var p in cat) { RouteRecordRules.NaiveLayouts(p, naive); foreach (var c in naive) if (p.PathCount(c) == p.target) naiveHits++; }
            var bandSizes = new int[6];
            for (int b = 0; b < 6; b++) bandSizes[b] = cat.Count(p => RouteRecordRules.InBand(p, b));
            var kinds = cat.GroupBy(p => p.kind.ToString()).ToDictionary(g => g.Key, g => g.Count());
            bool bankOk = cat.Count >= 300 && cores.Count == cat.Count && minLegal >= 24 && maxChance <= .125 + 1e-9 && naiveHits == 0 && bandSizes.All(x => x >= 10) && cat.All(p => p.target <= 24);
            ok &= bankOk;
            sb.AppendFormat(CultureInfo.InvariantCulture,
                "  \"bank\": {{ \"size\": {0}, \"uniquePrompts\": {1}, \"minLegalStateCount\": {2}, \"maxUniformChance\": {3:0.0000}, \"meanUniformChance\": {4:0.0000}, \"naiveLayoutHits\": {5}, \"bandPoolSizes\": [{6}], \"kinds\": {{{7}}}, \"pass\": {8} }},\n",
                cat.Count, cores.Count, minLegal, maxChance, meanChance, naiveHits, string.Join(",", bandSizes),
                string.Join(",", kinds.Select(k => "\"" + k.Key + "\":" + k.Value)), bankOk ? "true" : "false");

            // ── 2) 독립 검산: 노치 부분집합 전수 + DFS
            long subsetsChecked = 0; int mismatch = 0, layoutMismatch = 0;
            foreach (var p in cat.Concat(new[] { RouteRecordRules.Practice() }))
            {
                int total = 0; for (int g = 0; g < p.groupCount; g++) total += p.maxima[g];
                var validFromSubsets = new HashSet<string>();
                for (int mask = 0; mask < (1 << total); mask++)
                {
                    var placed = new bool[3, RouteRecordRules.MaxSlots]; int bit = 0, used = 0;
                    var counts = new int[3];
                    for (int g = 0; g < p.groupCount; g++) for (int j = 0; j < p.maxima[g]; j++, bit++)
                        if ((mask >> bit & 1) != 0) { placed[g, j] = true; counts[g]++; used++; }
                    if (used > p.budget) continue;
                    subsetsChecked++;
                    int dfs = DfsPaths(p, placed);
                    if (dfs != p.PathCount(counts)) mismatch++;
                    if (dfs == p.target) validFromSubsets.Add(p.LayoutId(counts));
                }
                if (!validFromSubsets.SetEquals(p.validLayouts)) layoutMismatch++;
            }
            bool dfsOk = mismatch == 0 && layoutMismatch == 0;
            ok &= dfsOk;
            sb.AppendFormat("  \"independentDfs\": {{ \"placementsChecked\": {0}, \"pathCountMismatch\": {1}, \"validLayoutSetMismatch\": {2}, \"pass\": {3} }},\n",
                subsetsChecked, mismatch, layoutMismatch, dfsOk ? "true" : "false");

            // ── 3) 오답 진단 우연 참
            int diagOnCorrect = 0, diagContradiction = 0, diagCounts = 0;
            foreach (var p in cat)
            {
                foreach (var c in AllCountVectors(p))
                {
                    if (p.Used(c) > p.budget || p.Used(c) == 0) continue;
                    int paths = p.PathCount(c);
                    if (paths == p.target) { continue; }
                    string d = RouteRecordRules.Diagnose(p, c);
                    diagCounts++;
                    int sum = c[0] + c[1] + c[2];
                    // 진단 문구는 「다른 규칙이라면 목표와 맞았다」인 경우에만 붙는다 — 실제 규칙으로는 반드시 틀린 배치여야 한다.
                    if (d == "serial-rings-added" && !(sum == p.target && paths != p.target)) diagContradiction++;
                    if (d == "or-sides-multiplied" && !(p.SidePaths(c, 0) * p.SidePaths(c, 1) == p.target && paths != p.target)) diagContradiction++;
                    if (d == "bridge-count-equals-path-count" && !(sum == p.target && paths != p.target)) diagContradiction++;
                }
                foreach (var c in AllCountVectors(p))
                    if (p.Used(c) <= p.budget && p.PathCount(c) == p.target && p.validLayouts.IndexOf(p.LayoutId(c)) < 0) diagOnCorrect++;
            }
            bool diagOk = diagContradiction == 0 && diagOnCorrect == 0;
            ok &= diagOk;
            sb.AppendFormat("  \"distractorAccidentalTruth\": {{ \"wrongLayoutsDiagnosed\": {0}, \"contradictions\": {1}, \"validLayoutsMissing\": {2}, \"pass\": {3} }},\n",
                diagCounts, diagContradiction, diagOnCorrect, diagOk ? "true" : "false");

            // ── 4) B3 무뇌 봇
            string[] policies = { "spam-first-notch", "fill-forward", "one-ring", "equal-per-ring", "cycle", "random-state", "random-taps", "no-input" };
            sb.Append("  \"b3\": {\n");
            bool b3Ok = true;
            for (int pi = 0; pi < policies.Length; pi++)
            {
                var r = RunPolicy(cat, policies[pi], Runs, 9000 + pi * 101);
                double se = Math.Sqrt(Math.Max(1e-9, r.chance * (1 - r.chance) / Math.Max(1, r.firstAttempted)));
                bool pass = policies[pi] == "no-input" ? r.solvedTotal == 0 : r.firstRate <= r.chance + 1.96 * se && r.firstRate <= .125;
                b3Ok &= pass;
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "    \"{0}\": {{ \"runs\": {10}, \"firstAttempted\": {1}, \"firstCorrect\": {2}, \"firstAttemptRate\": {3:0.0000}, \"chanceLevel\": {4:0.0000}, \"upper95\": {5:0.0000}, \"completionRate\": {6:0.0000}, \"meanSolved\": {7:0.00}, \"pass\": {8} }}{9}\n",
                    policies[pi], r.firstAttempted, r.firstCorrect, r.firstRate, r.chance, r.chance + 1.96 * se, r.completion, r.meanSolved, pass ? "true" : "false", pi < policies.Length - 1 ? "," : "", Runs);
            }
            sb.Append("  },\n");
            ok &= b3Ok;
            sb.AppendFormat("  \"pass\": {0}\n}}\n", ok ? "true" : "false");
            string outPath = args.Length > 0 ? args[0] : "headless-results.json";
            File.WriteAllText(outPath, sb.ToString());
            Console.Write(sb.ToString());
            return ok ? 0 : 1;
        }

        static IEnumerable<int[]> AllCountVectors(RouteProblem p)
        {
            int last = p.groupCount == 3 ? p.maxima[2] : 0;
            for (int a = 0; a <= p.maxima[0]; a++) for (int b = 0; b <= p.maxima[1]; b++) for (int c = 0; c <= last; c++) yield return new[] { a, b, c };
        }

        // 그래프 DFS: 노드 = (면, 홈 k). 링 g 의 놓인 브리지 하나 = 홈 k → 홈 k+1 간선. 마지막 홈 → 출구.
        static int DfsPaths(RouteProblem p, bool[,] placed)
        {
            int total = 0;
            for (int s = 0; s < p.sideCount; s++)
            {
                var gaps = new List<int>();
                for (int g = 0; g < p.groupCount; g++) if (p.side[g] == s) gaps.Add(g);
                total += Dfs(p, placed, gaps, 0);
            }
            return total;
        }

        static int Dfs(RouteProblem p, bool[,] placed, List<int> gaps, int k)
        {
            if (k == gaps.Count) return 1;
            int sum = 0, g = gaps[k];
            for (int j = 0; j < p.maxima[g]; j++) if (placed[g, j]) sum += Dfs(p, placed, gaps, k + 1);
            return sum;
        }

        sealed class Result { public int firstAttempted, firstCorrect, solvedTotal; public double firstRate, chance, completion, meanSolved; }

        static Result RunPolicy(List<RouteProblem> cat, string policy, int runs, int seed)
        {
            var rng = new Random(seed);
            var res = new Result(); double chanceSum = 0; int completed = 0; int globalAttempt = 0;
            for (int run = 0; run < runs; run++)
            {
                int needles = RouteRecordRules.StartNeedles, solved = 0, carry = 0; double time = 0;
                RouteProblem p = RouteRecordRules.Pick(cat, 0, rng, "");
                int budget = p.budget, attemptOnThis = 0;
                while (needles > 0 && solved < RouteRecordRules.Goal)
                {
                    if (policy == "no-input") { time = RouteRecordRules.SessionSeconds; break; }
                    var c = Layout(policy, p, budget, attemptOnThis, globalAttempt++, rng);
                    int used = p.Used(c);
                    time += 1.2 + .45 * used;          // 노치 탭·톤암 드래그에 드는 활성 시간(시험 연출 중엔 시계가 멈춘다)
                    if (time >= RouteRecordRules.SessionSeconds) break;
                    if (used == 0) { attemptOnThis++; continue; } // 빈 음반은 판정 없이 거절
                    bool correct = p.PathCount(c) == p.target;
                    if (attemptOnThis == 0)
                    {
                        res.firstAttempted++; if (correct) res.firstCorrect++;
                        chanceSum += PolicyChance(policy, p, budget);
                    }
                    attemptOnThis++;
                    if (correct)
                    {
                        carry = Math.Min(2, Math.Max(0, budget - used));
                        solved++;
                        if (solved >= RouteRecordRules.Goal) break;
                        p = RouteRecordRules.Pick(cat, solved, rng, p.id); budget = p.budget + carry; attemptOnThis = 0;
                    }
                    else needles--;
                }
                res.solvedTotal += solved;
                if (solved >= RouteRecordRules.Goal && needles > 0) completed++;
            }
            res.firstRate = res.firstAttempted > 0 ? (double)res.firstCorrect / res.firstAttempted : 0;
            res.chance = res.firstAttempted > 0 ? chanceSum / res.firstAttempted : 0;
            res.completion = (double)completed / runs;
            res.meanSolved = (double)res.solvedTotal / runs;
            return res;
        }

        // 우연 수준 = 판단 없이 맞을 확률. 결정적 정책·균등 상태 정책은 실제 예산(이월 포함)에서의 균등 법적 상태 우연율,
        // 무작위 탭 정책은 그 탭 분포 자체의 성공 확률(몬테카를로 600회)로 잰다.
        static readonly Random mc = new Random(424242);
        static double PolicyChance(string policy, RouteProblem p, int budget)
        {
            if (policy == "random-taps")
            {
                int hit = 0;
                for (int i = 0; i < 600; i++) if (p.PathCount(Layout(policy, p, budget, 0, 0, mc)) == p.target) hit++;
                return hit / 600.0;
            }
            int legal = 0, valid = 0;
            foreach (var c in AllCountVectors(p)) { if (p.Used(c) > budget) continue; legal++; if (p.PathCount(c) == p.target) valid++; }
            return (double)valid / legal;
        }

        static int[] Layout(string policy, RouteProblem p, int budget, int attemptOnThis, int globalAttempt, Random rng)
        {
            int n = p.groupCount; var c = new int[3];
            switch (policy)
            {
                case "spam-first-notch": c[0] = 1; break;
                case "fill-forward": { int left = budget; for (int g = 0; g < n; g++) { c[g] = Math.Min(p.maxima[g], left); left -= c[g]; } break; }
                case "one-ring": { int g = attemptOnThis % n; c[g] = Math.Min(p.maxima[g], budget); break; }
                case "equal-per-ring":
                case "cycle":
                {
                    int k = policy == "cycle" ? globalAttempt % 4 + 1 : attemptOnThis % 4 + 2;
                    int used = 0; for (int g = 0; g < n; g++) { c[g] = Math.Min(k, p.maxima[g]); used += c[g]; }
                    for (int g = n - 1; g >= 0 && used > budget; g--) { int cut = Math.Min(c[g], used - budget); c[g] -= cut; used -= cut; }
                    break;
                }
                case "random-state":
                {
                    // 법적 상태(합 ≤ 예산) 중 균등 추첨 — 거절 표본추출
                    while (true)
                    {
                        int used = 0; for (int g = 0; g < n; g++) { c[g] = rng.Next(p.maxima[g] + 1); used += c[g]; }
                        if (used <= budget) break;
                    }
                    break;
                }
                case "random-taps":
                {
                    var order = new List<int>();
                    for (int g = 0; g < n; g++) for (int j = 0; j < p.maxima[g]; j++) if (rng.NextDouble() < .5) order.Add(g);
                    order = order.OrderBy(_ => rng.Next()).ToList();
                    for (int i = 0; i < order.Count && i < budget; i++) c[order[i]]++;
                    break;
                }
            }
            return c;
        }
    }
}
