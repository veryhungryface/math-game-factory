// 도토리 낙하 — 중앙값 제약 만족 문제 은행과 정수(answer2) 판정.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;

namespace Mgf.DotoriNakha
{
    [Serializable]
    public sealed class DotoriProblem
    {
        public string id;
        public int[] raw;
        public int answer2;
        public int band;
        public string prompt;

        public MgfProblem ToMgf()
        {
            return new MgfProblem
            {
                id = id,
                prompt = prompt,
                choices = Array.Empty<string>(),
                answer = DotoriRules.FormatHalf(answer2) + " g",
                answerNumeric = answer2 / 2.0,
                unitConcept = raw.Length == 5
                    ? "크기순으로 늘어놓은 자료의 중앙값"
                    : raw.Length == 6
                        ? "짝수 개 자료의 가운데 두 값의 평균"
                        : "중복되고 치우친 자료의 중앙값"
            };
        }
    }

    public static class DotoriRules
    {
        public const int StartLives = 3;
        public const int TargetSolved = 8;
        public const int MasteryNeeded = 6;
        public const float SessionSeconds = 120f;

        // curriculum/2022-middle-math.json expression_traps 이관:
        // 중학교 값 문항은 「~을/를 구하시오」로 끝낸다. 수와 단위는 띄어 쓴다(예: 5.5 g).
        // 이 단원은 어림·근호·확률을 사용하지 않으며, 모든 원자료는 정수 g, 정답은 answer2 정수로 판정한다.
        public const string RawMiddle = "raw_spatial_middle";
        public const string EvenLow = "even_one_middle_low";
        public const string EvenHigh = "even_one_middle_high";
        public const string RangeMidpoint = "range_midpoint";
        public const string MeanInstead = "mean_instead_of_median";
        public const string ModeInstead = "mode_instead_of_median";
        public const string OrderError = "median_order_error";

        public static List<DotoriProblem> BuildProblems()
        {
            var result = new List<DotoriProblem>(696);
            var signatures = new HashSet<string>();

            // 답을 먼저 균등하게 정한 뒤 그 답을 중앙값으로 갖는 원자료를 역으로 만든다.
            for (int target = 1; target <= 12; target++)
                for (int variant = 0; variant < 12; variant++)
                    result.Add(BuildUniqueOdd(target, variant, signatures));

            for (int answer2 = 2; answer2 <= 24; answer2++)
            {
                for (int variant = 0; variant < 12; variant++)
                    result.Add(BuildUniqueEven(answer2, 6, variant, signatures));
                for (int variant = 0; variant < 12; variant++)
                    result.Add(BuildUniqueEven(answer2, 8, variant, signatures));
            }

            Validate(result);
            return result;
        }

        public static DotoriProblem PracticeOdd() => Make("practice-odd", new[] { 9, 2, 5, 1, 7 }, 10, 1);
        public static DotoriProblem PracticeEven() => Make("practice-even", new[] { 2, 3, 4, 7, 10, 11 }, 11, 2);

        public static List<DotoriProblem> BuildDeck(List<DotoriProblem> pool, int serial)
        {
            var deck = new List<DotoriProblem>(TargetSolved);
            int[] sizes = { 5, 5, 6, 6, 6, 8, 8, 8 };
            for (int round = 0; round < sizes.Length; round++)
            {
                int n = sizes[round];
                int bucketCount = n == 5 ? 12 : 23;
                int bucket = (int)(Hash((uint)(serial * 1009 + round * 313 + 71)) % (uint)bucketCount);
                int answer2 = n == 5 ? 2 * (bucket + 1) : bucket + 2;
                var matches = new List<DotoriProblem>(12);
                for (int i = 0; i < pool.Count; i++)
                    if (pool[i].raw.Length == n && pool[i].answer2 == answer2) matches.Add(pool[i]);
                int pick = (int)(Hash((uint)(serial * 7919 + round * 421 + 19)) % (uint)matches.Count);
                deck.Add(matches[pick]);
            }
            return deck;
        }

        static DotoriProblem BuildUniqueOdd(int target, int variant, HashSet<string> signatures)
        {
            for (int salt = 0; salt < 5000; salt++)
            {
                uint seed = Hash((uint)(target * 100003 + variant * 1009 + salt * 7919));
                var sorted = new int[5];
                sorted[0] = 1 + NextInt(ref seed, target);
                sorted[1] = 1 + NextInt(ref seed, target);
                sorted[2] = target;
                sorted[3] = target + NextInt(ref seed, 13 - target);
                sorted[4] = target + NextInt(ref seed, 13 - target);
                Array.Sort(sorted);
                string signature = "5:" + string.Join(",", sorted);
                if (!signatures.Add(signature)) continue;
                var raw = (int[])sorted.Clone();
                Shuffle(raw, ref seed);
                return Make("odd5-m" + target + "-v" + variant, raw, target * 2, 1);
            }
            throw new InvalidOperationException("홀수 중앙값 버킷 생성 실패: " + target);
        }

        static DotoriProblem BuildUniqueEven(int answer2, int n, int variant, HashSet<string> signatures)
        {
            int aMin = Math.Max(1, answer2 - 12);
            int aMax = Math.Min(12, answer2 / 2);
            for (int salt = 0; salt < 10000; salt++)
            {
                uint seed = Hash((uint)(answer2 * 130003 + n * 2017 + variant * 97 + salt * 6151));
                int a = aMin + NextInt(ref seed, aMax - aMin + 1);
                int b = answer2 - a;
                int side = n / 2 - 1;
                var sorted = new int[n];
                for (int i = 0; i < side; i++) sorted[i] = 1 + NextInt(ref seed, a);
                sorted[side] = a;
                sorted[side + 1] = b;
                for (int i = side + 2; i < n; i++) sorted[i] = b + NextInt(ref seed, 13 - b);
                // 3단계는 중복을 구조적으로 포함한다. 중복 자료도 각각 한 자료로 세어야 한다.
                if (n == 8) sorted[1] = sorted[0];
                Array.Sort(sorted);
                string signature = n + ":" + string.Join(",", sorted);
                if (!signatures.Add(signature)) continue;
                var raw = (int[])sorted.Clone();
                Shuffle(raw, ref seed);
                return Make((n == 6 ? "even6" : "even8") + "-a2" + answer2 + "-v" + variant, raw, answer2, n == 6 ? 2 : 3);
            }
            throw new InvalidOperationException("짝수 중앙값 버킷 생성 실패: " + n + "/" + answer2);
        }

        static DotoriProblem Make(string id, int[] raw, int answer2, int band)
        {
            return new DotoriProblem
            {
                id = id,
                raw = raw,
                answer2 = answer2,
                band = band,
                prompt = "무게 자료 " + JoinValues(raw) + " g의 중앙값을 구하시오."
            };
        }

        public static bool IsCorrect(DotoriProblem problem, int submitted2)
            => problem != null && submitted2 == problem.answer2;

        public static int RecalculateAnswer2(int[] raw)
        {
            var sorted = (int[])raw.Clone();
            Array.Sort(sorted);
            int n = sorted.Length;
            return n % 2 == 1 ? 2 * sorted[n / 2] : sorted[n / 2 - 1] + sorted[n / 2];
        }

        public static string ClassifyMisconception(DotoriProblem problem, int submitted2)
        {
            if (problem == null || submitted2 == problem.answer2) return "";
            int n = problem.raw.Length;
            int rawMiddle2 = n % 2 == 1
                ? 2 * problem.raw[n / 2]
                : problem.raw[n / 2 - 1] + problem.raw[n / 2];
            if (submitted2 == rawMiddle2) return RawMiddle;

            var sorted = (int[])problem.raw.Clone();
            Array.Sort(sorted);
            if (n % 2 == 0 && submitted2 == 2 * sorted[n / 2 - 1]) return EvenLow;
            if (n % 2 == 0 && submitted2 == 2 * sorted[n / 2]) return EvenHigh;
            if (submitted2 == sorted[0] + sorted[n - 1]) return RangeMidpoint;

            int sum = 0;
            for (int i = 0; i < n; i++) sum += sorted[i];
            if (submitted2 * n == 2 * sum) return MeanInstead;
            int mode = UniqueMode(sorted);
            if (mode >= 0 && submitted2 == 2 * mode) return ModeInstead;
            return OrderError;
        }

        public static string MisconceptionKorean(string id)
        {
            switch (id)
            {
                case RawMiddle: return "보이는 순서가 아니라 크기순으로 늘어놓으시오";
                case EvenLow:
                case EvenHigh: return "짝수 개이면 가운데 두 값의 평균을 구하시오";
                case RangeMidpoint: return "최솟값과 최댓값의 중간이 아니라 자료의 가운데를 찾으시오";
                case MeanInstead: return "전체 평균이 아니라 크기순 가운데 값이다";
                case ModeInstead: return "가장 자주 나온 값이 아니라 크기순 가운데 값이다";
                default: return "자료를 크기순으로 놓고 가운데 순번을 다시 확인하시오";
            }
        }

        public static string Evidence(DotoriProblem problem)
        {
            var sorted = (int[])problem.raw.Clone();
            Array.Sort(sorted);
            string row = JoinValues(sorted) + " g";
            if (sorted.Length % 2 == 1)
                return row + "  →  세 번째 값 " + sorted[sorted.Length / 2] + " g";
            int a = sorted[sorted.Length / 2 - 1];
            int b = sorted[sorted.Length / 2];
            return row + "  →  (" + a + "+" + b + ")÷2=" + FormatHalf(problem.answer2) + " g";
        }

        public static int[] SortedCopy(int[] values)
        {
            var result = (int[])values.Clone();
            Array.Sort(result);
            return result;
        }

        public static string FormatHalf(int value2)
            => value2 % 2 == 0 ? (value2 / 2).ToString() : (value2 / 2) + ".5";

        public static string JoinValues(int[] values)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(values[i]);
            }
            return sb.ToString();
        }

        static int UniqueMode(int[] sorted)
        {
            int bestValue = -1, bestCount = 1, ties = 0;
            for (int i = 0; i < sorted.Length;)
            {
                int j = i + 1;
                while (j < sorted.Length && sorted[j] == sorted[i]) j++;
                int count = j - i;
                if (count > bestCount) { bestCount = count; bestValue = sorted[i]; ties = 1; }
                else if (count == bestCount && count > 1) ties++;
                i = j;
            }
            return ties == 1 ? bestValue : -1;
        }

        static int NextInt(ref uint state, int exclusive)
        {
            state = Next(state);
            return exclusive <= 1 ? 0 : (int)(state % (uint)exclusive);
        }

        static void Shuffle(int[] values, ref uint state)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = NextInt(ref state, i + 1);
                int temp = values[i]; values[i] = values[j]; values[j] = temp;
            }
        }

        static uint Next(uint x)
        {
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            return x;
        }

        static uint Hash(uint x)
        {
            x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16;
            return x;
        }

        static void Validate(List<DotoriProblem> pool)
        {
            if (pool.Count != 696) throw new InvalidOperationException("문제 은행은 696개여야 합니다.");
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            var signatures = new HashSet<string>();
            var buckets = new Dictionary<string, int>();
            foreach (var p in pool)
            {
                if (!ids.Add(p.id) || !prompts.Add(p.prompt)) throw new InvalidOperationException("문항 중복: " + p.id);
                var sorted = SortedCopy(p.raw);
                if (!signatures.Add(p.raw.Length + ":" + string.Join(",", sorted))) throw new InvalidOperationException("다중집합 중복: " + p.id);
                if (p.answer2 < 2 || p.answer2 > 24 || RecalculateAnswer2(p.raw) != p.answer2) throw new InvalidOperationException("중앙값 오류: " + p.id);
                if (!p.prompt.EndsWith("중앙값을 구하시오.") || p.prompt.Contains("자리에서") || p.prompt.Contains("√")) throw new InvalidOperationException("발문 오류: " + p.id);
                int solutions = 0;
                for (int v = 2; v <= 24; v++) if (IsCorrect(p, v)) solutions++;
                if (solutions != 1) throw new InvalidOperationException("유일해 오류: " + p.id);
                for (int v = 2; v <= 24; v++)
                    if (v != p.answer2 && string.IsNullOrEmpty(ClassifyMisconception(p, v))) throw new InvalidOperationException("오답 진단 누락: " + p.id + "/" + v);
                string key = p.raw.Length + ":" + p.answer2;
                buckets[key] = buckets.TryGetValue(key, out int count) ? count + 1 : 1;
            }
            for (int target = 1; target <= 12; target++)
                if (!buckets.TryGetValue("5:" + target * 2, out int count) || count != 12) throw new InvalidOperationException("홀수 답 버킷 불균등");
            for (int a2 = 2; a2 <= 24; a2++)
                foreach (int n in new[] { 6, 8 })
                    if (!buckets.TryGetValue(n + ":" + a2, out int count) || count != 12) throw new InvalidOperationException("짝수 답 버킷 불균등");
        }
    }
}
