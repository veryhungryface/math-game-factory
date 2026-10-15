using System;
using System.Collections.Generic;
using System.Text;

namespace Mgf.RouteRecord
{
    // 음반 구조. 「링」 = 바깥 홈에서 안쪽 홈으로 건너가는 브리지 자리 한 줄(한 단계의 선택).
    //   Serial2/Serial3 : 한 면에 링이 직렬로 2·3개 → 각 링에서 하나씩 → 곱
    //   Sum2            : 서로 만나지 않는 A면·B면에 링이 1개씩 → 둘 중 한 면 → 합
    //   MixA2           : A면 직렬 2링 + B면 1링 → a×b + c
    //   MixB2           : A면 1링 + B면 직렬 2링 → a + b×c
    enum GrooveKind { Serial2, Serial3, Sum2, MixA2, MixB2 }

    sealed class RouteProblem
    {
        public string id = "";
        public string prompt = "";
        public string structureLine = "";
        public string concept = "";
        public string reveal = "";
        public GrooveKind kind;
        public int target;
        public int budget;
        public int groupCount;
        public int sideCount;
        public int minUse;
        public int legalStateCount;
        public int validStateCount;
        public readonly int[] maxima = new int[3];
        public readonly int[] side = new int[3];
        public readonly List<string> validLayouts = new List<string>();

        // 판정: 각 면 안의 링은 곱하고(각 링에서 하나씩), 서로 만나지 않는 두 면은 더한다.
        // 학생이 놓은 링별 브리지 개수만으로 정수 계산한다(부동소수 없음).
        public int SidePaths(int[] counts, int s)
        {
            int p = 1; bool any = false;
            for (int g = 0; g < groupCount; g++)
            {
                if (side[g] != s) continue;
                any = true; p *= counts[g];
            }
            return any ? p : 0;
        }

        public int PathCount(int[] counts)
        {
            if (counts == null || counts.Length < groupCount) return 0;
            int total = 0;
            for (int s = 0; s < sideCount; s++) total += SidePaths(counts, s);
            return total;
        }

        public int Used(int[] counts)
        {
            int n = 0;
            for (int i = 0; i < groupCount; i++) n += counts[i];
            return n;
        }

        public string LayoutId(int[] counts)
        {
            var sb = new StringBuilder(8);
            for (int g = 0; g < groupCount; g++) { if (g > 0) sb.Append('-'); sb.Append(counts[g]); }
            return sb.ToString();
        }

        public string Formula(int[] counts)
        {
            var sb = new StringBuilder(24);
            for (int s = 0; s < sideCount; s++)
            {
                if (s > 0) sb.Append(" + ");
                bool first = true;
                for (int g = 0; g < groupCount; g++)
                {
                    if (side[g] != s) continue;
                    if (!first) sb.Append(" × ");
                    sb.Append(counts[g]); first = false;
                }
            }
            return sb.ToString();
        }

        public string GroupLabel(int g)
        {
            switch (kind)
            {
                case GrooveKind.Sum2: return g == 0 ? "A면" : "B면";
                case GrooveKind.MixA2: return g == 0 ? "A면 링1" : g == 1 ? "A면 링2" : "B면";
                case GrooveKind.MixB2: return g == 0 ? "A면" : g == 1 ? "B면 링1" : "B면 링2";
                default: return "링" + (g + 1);
            }
        }

        public string RuleLine
        {
            get
            {
                switch (kind)
                {
                    case GrooveKind.Serial2:
                    case GrooveKind.Serial3: return "각 링에서 하나씩 지나간다";
                    case GrooveKind.Sum2: return "A면과 B면은 만나지 않는다 · 둘 중 한 면";
                    default: return "한 면 안의 링은 하나씩 모두 지나고, 두 면은 만나지 않는다";
                }
            }
        }
    }

    static class RouteRecordRules
    {
        public const int Goal = 6;
        public const int StartNeedles = 3;
        public const int MaxSlots = 6;
        public const int SessionSeconds = 110;

        // expression_traps (중2 경우의 수, 2022-middle-math.json):
        //  - 두 사건이 「동시에 일어나지 않을 때」만 더한다 → A면·B면은 서로 만나지 않는 홈으로만 만든다(합류·겹침 생성 금지).
        //  - 각 단계에서 하나씩 「함께」 고르는 경우는 곱한다 → 한 면 안의 직렬 링.
        //  - 포함배제·확률·순열·조합 기호·제곱근은 범위 밖 → 생성하지 않는다.
        //  - 발문은 지시형 「~끼우시오」 하나로 통일한다.
        public static RouteProblem Practice()
        {
            var p = Make("practice-2x3", GrooveKind.Serial2, 2, 3, 0, 6, 6);
            p.prompt = "연습 음반 · 직렬 링 2개\n서로 다른 소리 경로가 6가지가 되도록 브리지를 끼우시오.";
            return p;
        }

        static readonly int[][] SerialTwoMaxima = { new[]{3,4}, new[]{4,4}, new[]{4,5}, new[]{5,4}, new[]{5,5}, new[]{3,5}, new[]{5,3}, new[]{4,3} };
        static readonly int[][] SerialThreeMaxima = { new[]{3,3,3}, new[]{3,4,3}, new[]{4,3,4}, new[]{4,4,4}, new[]{3,3,4}, new[]{4,4,3} };
        static readonly int[][] SumMaxima = { new[]{4,5}, new[]{5,5}, new[]{5,6}, new[]{6,5}, new[]{6,6}, new[]{4,6} };
        static readonly int[][] MixAMaxima = { new[]{3,3,4}, new[]{4,3,4}, new[]{3,4,4}, new[]{4,4,4}, new[]{3,3,5} };
        static readonly int[][] MixBMaxima = { new[]{4,3,3}, new[]{4,3,4}, new[]{4,4,3}, new[]{4,4,4}, new[]{5,3,3} };

        // 제약 만족 풀: (구조, 링별 노치 수, 목표 T, 예산 B) 격자를 전수 열거하고
        // 해 1개 이상 · 법적 상태 24개 이상 · 균등 상태 우연 성공률 ≤ 1/8 · T ≤ 24 인 것만 남긴다.
        public static List<RouteProblem> BuildCatalog()
        {
            var result = new List<RouteProblem>(720);
            int serial = 0;
            AddKind(result, GrooveKind.Serial2, SerialTwoMaxima, ref serial);
            AddKind(result, GrooveKind.Serial3, SerialThreeMaxima, ref serial);
            AddKind(result, GrooveKind.Sum2, SumMaxima, ref serial);
            AddKind(result, GrooveKind.MixA2, MixAMaxima, ref serial);
            AddKind(result, GrooveKind.MixB2, MixBMaxima, ref serial);
            if (result.Count < 300) throw new InvalidOperationException("루트 레코드 제약 만족 문항이 300개 미만이다: " + result.Count);
            return result;
        }

        static void AddKind(List<RouteProblem> list, GrooveKind kind, int[][] maximaSet, ref int serial)
        {
            for (int mi = 0; mi < maximaSet.Length; mi++)
            {
                int[] m = maximaSet[mi];
                for (int t = 4; t <= 24; t++)
                {
                    var probe = Make("probe", kind, m[0], m[1], m.Length > 2 ? m[2] : 0, t, 99);
                    if (probe.validStateCount == 0) continue;
                    for (int b = probe.minUse; b <= probe.minUse + 2; b++)
                    {
                        var p = Make("", kind, m[0], m[1], m.Length > 2 ? m[2] : 0, t, b);
                        if (p.validStateCount == 0 || p.legalStateCount < 24 || p.validStateCount * 8 > p.legalStateCount) continue;
                        if (NaiveHits(p)) continue;
                        p.id = "rr-" + (++serial).ToString("D4");
                        list.Add(p);
                    }
                }
            }
        }

        // 무뇌 입력이 「항상 같은 모양」으로 만드는 배치들. 이 중 하나라도 정답이면 그 문항은 버린다 —
        // 첫 노치 하나·앞에서부터 채우기·뒤에서부터 채우기·한 링 몰기·모든 링 같은 개수(k=1..6).
        public static void NaiveLayouts(RouteProblem p, List<int[]> outList)
        {
            outList.Clear();
            int n = p.groupCount;
            outList.Add(new[] { 1, 0, 0 });
            var fwd = new int[3]; int left = p.budget;
            for (int g = 0; g < n; g++) { fwd[g] = Math.Min(p.maxima[g], left); left -= fwd[g]; }
            outList.Add(fwd);
            var rev = new int[3]; left = p.budget;
            for (int g = n - 1; g >= 0; g--) { rev[g] = Math.Min(p.maxima[g], left); left -= rev[g]; }
            outList.Add(rev);
            for (int g = 0; g < n; g++) { var one = new int[3]; one[g] = Math.Min(p.maxima[g], p.budget); outList.Add(one); }
            for (int k = 1; k <= MaxSlots; k++)
            {
                var eq = new int[3]; int used = 0;
                for (int g = 0; g < n; g++) { eq[g] = Math.Min(k, p.maxima[g]); used += eq[g]; }
                if (used <= p.budget) outList.Add(eq);
            }
        }

        static readonly List<int[]> naiveTmp = new List<int[]>(16);
        static bool NaiveHits(RouteProblem p)
        {
            NaiveLayouts(p, naiveTmp);
            for (int i = 0; i < naiveTmp.Count; i++) if (p.PathCount(naiveTmp[i]) == p.target) return true;
            return false;
        }

        static RouteProblem Make(string id, GrooveKind kind, int max0, int max1, int max2, int target, int budget)
        {
            var p = new RouteProblem { id = id, kind = kind, target = target, budget = budget, concept = Concept(kind), reveal = Rule(kind) };
            p.groupCount = kind == GrooveKind.Serial2 || kind == GrooveKind.Sum2 ? 2 : 3;
            p.sideCount = kind == GrooveKind.Serial2 || kind == GrooveKind.Serial3 ? 1 : 2;
            p.maxima[0] = max0; p.maxima[1] = max1; p.maxima[2] = p.groupCount == 3 ? max2 : 0;
            switch (kind)
            {
                case GrooveKind.Sum2: p.side[0] = 0; p.side[1] = 1; break;
                case GrooveKind.MixA2: p.side[0] = 0; p.side[1] = 0; p.side[2] = 1; break;
                case GrooveKind.MixB2: p.side[0] = 0; p.side[1] = 1; p.side[2] = 1; break;
                default: p.side[0] = p.side[1] = p.side[2] = 0; break;
            }

            // 법적 상태 = 링별 브리지 개수 벡터(각 0..노치 수, 합 ≤ 예산). 전수 열거.
            int[] c = new int[3];
            int last = p.groupCount == 3 ? p.maxima[2] : 0;
            p.minUse = int.MaxValue;
            for (c[0] = 0; c[0] <= p.maxima[0]; c[0]++)
            for (c[1] = 0; c[1] <= p.maxima[1]; c[1]++)
            for (c[2] = 0; c[2] <= last; c[2]++)
            {
                int used = p.Used(c);
                if (p.PathCount(c) == target && used < p.minUse) p.minUse = used;
                if (used > budget) continue;
                p.legalStateCount++;
                if (p.PathCount(c) == target) { p.validStateCount++; p.validLayouts.Add(p.LayoutId(c)); }
            }
            p.validLayouts.Sort(StringComparer.Ordinal);

            p.structureLine = Structure(p) + " · 브리지 " + budget + "개";
            p.prompt = p.structureLine + "\n서로 다른 소리 경로가 " + target + "가지가 되도록 브리지를 끼우시오.";
            return p;
        }

        static string Structure(RouteProblem p)
        {
            switch (p.kind)
            {
                case GrooveKind.Serial2: return "직렬 링 2개 · 노치 " + p.maxima[0] + "·" + p.maxima[1];
                case GrooveKind.Serial3: return "직렬 링 3개 · 노치 " + p.maxima[0] + "·" + p.maxima[1] + "·" + p.maxima[2];
                case GrooveKind.Sum2: return "A면·B면 · 노치 " + p.maxima[0] + "·" + p.maxima[1];
                case GrooveKind.MixA2: return "A면 링 2개 + B면 · 노치 " + p.maxima[0] + "·" + p.maxima[1] + "·" + p.maxima[2];
                default: return "A면 + B면 링 2개 · 노치 " + p.maxima[0] + "·" + p.maxima[1] + "·" + p.maxima[2];
            }
        }

        static string Concept(GrooveKind kind)
        {
            if (kind == GrooveKind.Sum2) return "동시에 일어나지 않는 두 사건의 합";
            if (kind == GrooveKind.Serial2 || kind == GrooveKind.Serial3) return "두 선택을 함께 하는 곱";
            return "합과 곱이 섞인 간단한 경우";
        }

        static string Rule(GrooveKind kind)
        {
            if (kind == GrooveKind.Sum2) return "두 면 중 하나를 지나므로 더한다";
            if (kind == GrooveKind.Serial2 || kind == GrooveKind.Serial3) return "각 링에서 하나씩 지나므로 곱한다";
            return "한 면 안의 링은 곱하고, 두 면은 더한다";
        }

        // 난이도 밴드를 순서대로 소진한다: 재킷 1·2 = 직렬 2링(예산 여유 1), 3 = 직렬 3링(더 적은 브리지 분해),
        // 4 = A면/B면 합, 5·6 = 합+곱 혼합. 눈에 보이는 구조(링 수·면 수)가 단계마다 바뀐다.
        public static RouteProblem Pick(List<RouteProblem> catalog, int solved, System.Random rng, string avoidId)
        {
            var pool = new List<RouteProblem>(160);
            for (int i = 0; i < catalog.Count; i++)
            {
                var p = catalog[i];
                if (p.id == avoidId) continue;
                if (InBand(p, solved)) pool.Add(p);
            }
            if (pool.Count == 0) return catalog[rng.Next(catalog.Count)];
            return pool[rng.Next(pool.Count)];
        }

        public static bool InBand(RouteProblem p, int solved)
        {
            if (solved < 2) return p.kind == GrooveKind.Serial2 && p.budget == p.minUse + 1 && p.target >= 6 && p.target <= 12;
            if (solved == 2) return p.kind == GrooveKind.Serial3 && p.target >= 8;
            if (solved == 3) return p.kind == GrooveKind.Sum2;
            return (p.kind == GrooveKind.MixA2 || p.kind == GrooveKind.MixB2) && p.target >= 7 && p.target <= 18;
        }

        // 오답 진단(오개념 역산). 학생의 실제 링별 개수로 「다른 규칙을 썼다면 목표와 맞았는가」를 본다.
        // 정답 배치에는 호출되지 않는다(Submit 이 pathCount==target 이면 진단을 건너뛴다).
        public static string Diagnose(RouteProblem p, int[] counts)
        {
            int sum = 0, prod = 1;
            for (int i = 0; i < p.groupCount; i++) { sum += counts[i]; prod *= counts[i]; }
            bool serial = p.kind == GrooveKind.Serial2 || p.kind == GrooveKind.Serial3;
            if (serial && sum == p.target) return "serial-rings-added";          // 직렬 링을 더했다
            if (!serial && p.sideCount == 2 && CrossSideProduct(p, counts) == p.target) return "or-sides-multiplied"; // 또는 면을 곱했다
            if (sum == p.target) return "bridge-count-equals-path-count";      // 브리지 수 = 경로 수로 봤다
            return "path-count-mismatch";
        }

        static int CrossSideProduct(RouteProblem p, int[] counts)
        {
            int a = p.SidePaths(counts, 0), b = p.SidePaths(counts, 1);
            return a * b;
        }

        public static string MisconceptionLine(string id)
        {
            switch (id)
            {
                case "or-sides-multiplied": return "두 면은 만나지 않는다 · 둘 중 한 면이므로 더한다";
                case "serial-rings-added": return "각 링에서 하나씩 지나므로 링끼리는 곱한다";
                case "bridge-count-equals-path-count": return "브리지 수와 경로 수는 같지 않다";
                default: return "한 면 안의 링은 곱하고, 두 면은 더한다";
            }
        }

        public static string BankJson(List<RouteProblem> catalog)
        {
            var sb = new StringBuilder(200000);
            sb.Append("{\"items\":[");
            for (int i = 0; i < catalog.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var p = catalog[i];
                sb.Append("{\"id\":").Append(Esc(p.id));
                sb.Append(",\"prompt\":").Append(Esc(p.prompt.Replace('\n', ' ')));
                sb.Append(",\"choices\":null");
                sb.Append(",\"answer\":").Append(Esc(p.validLayouts[0]));
                sb.Append(",\"answerNumeric\":").Append(p.target);
                sb.Append(",\"unitConcept\":").Append(Esc(p.concept));
                sb.Append(",\"structure\":").Append(Esc(p.kind.ToString()));
                sb.Append(",\"budget\":").Append(p.budget);
                sb.Append(",\"legalStateCount\":").Append(p.legalStateCount);
                sb.Append(",\"validStateCount\":").Append(p.validStateCount);
                sb.Append(",\"allValidLayouts\":[");
                for (int v = 0; v < p.validLayouts.Count; v++)
                {
                    if (v > 0) sb.Append(',');
                    sb.Append(Esc(p.validLayouts[v]));
                }
                sb.Append("]}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        static string Esc(string s)
        {
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char ch in s)
            {
                if (ch == '"' || ch == '\\') sb.Append('\\').Append(ch);
                else if (ch == '\n') sb.Append("\\n");
                else if (ch < ' ') sb.Append(' ');
                else sb.Append(ch);
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
