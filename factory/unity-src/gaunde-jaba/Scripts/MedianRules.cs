// 가운데 잡아 — 중앙값 제약 만족 은행과 정수(value2) 판정.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;

namespace Mgf.GaundeJaba
{
    [Serializable]
    public sealed class MedianProblem
    {
        public string id;
        public int[] raw;
        public int answer2;
        public int band;
        public bool errorFind;
        public int wrongMark2;
        public string wrongMarkMisconception;
        public string prompt;

        public MgfProblem ToMgf()
        {
            return new MgfProblem
            {
                id = id,
                prompt = prompt,
                choices = Array.Empty<string>(),
                answer = MedianRules.FormatHalf(answer2) + "초",
                answerNumeric = answer2 / 2.0,
                unitConcept = errorFind ? "잘못 구한 중앙값 고치기" : raw.Length % 2 == 0 ? "짝수 개 자료의 중앙값" : "중앙값의 뜻과 크기순 정리"
            };
        }
    }

    public static class MedianRules
    {
        public const int StartLives = 3;
        public const int TargetSolved = 6;
        public const int MasteryNeeded = 5;
        public const float SessionSeconds = 110f;

        // expression_traps: 중학교 문제 발문은 「~을/를 구하시오」로 통일한다.
        // 어림 표현·근호·확률 답 형식은 이 단원에서 사용하지 않는다. 단위는 모든 기록과 답에 "초"로 붙인다.
        public static readonly string[] MisconceptionIds =
        {
            "raw_spatial_middle", "even_one_middle_low", "even_one_middle_high", "mean_instead_of_median", "largest_as_median", "mode_as_median"
        };

        public static List<MedianProblem> BuildProblems()
        {
            var result = new List<MedianProblem>(516);
            var signatures = new HashSet<string>();
            for (int m = 1; m <= 12; m++)
            {
                for (int v = 0; v < 10; v++) result.Add(BuildUniqueOdd(m, 5, v, false, signatures));
                for (int v = 0; v < 10; v++) result.Add(BuildUniqueOdd(m, 7, v, true, signatures));
            }
            for (int answer2 = 2; answer2 <= 24; answer2++)
            {
                for (int v = 0; v < 6; v++) result.Add(BuildUniqueEven(answer2, 6, v, false, signatures));
                for (int v = 0; v < 6; v++) result.Add(BuildUniqueEven(answer2, 8, v, true, signatures));
            }
            Validate(result);
            return result;
        }

        public static MedianProblem PracticeOdd() => Make("practice-odd", new[] { 9, 2, 5 }, 10, 0, false, 0);
        public static MedianProblem PracticeEven() => Make("practice-even", new[] { 2, 4, 6, 8 }, 10, 0, false, 0);

        public static List<MedianProblem> BuildDeck(List<MedianProblem> pool, int serial, bool deterministic)
        {
            var deck = new List<MedianProblem>(TargetSolved);
            int[] counts = { 5, 5, 6, 6, 7, 8 };
            bool[] errors = { false, false, false, false, true, true };
            for (int round = 0; round < TargetSolved; round++)
            {
                int answers = counts[round] % 2 == 0 ? 23 : 12;
                int bucket = deterministic ? (round * 5 + 3) % answers : (int)(Hash((uint)(serial * 97 + round * 31 + 11)) % (uint)answers);
                int answer2 = counts[round] % 2 == 0 ? 2 + bucket : 2 * (1 + bucket);
                var matches = new List<MedianProblem>();
                for (int i = 0; i < pool.Count; i++)
                    if (pool[i].raw.Length == counts[round] && pool[i].errorFind == errors[round] && pool[i].answer2 == answer2) matches.Add(pool[i]);
                int pick = deterministic ? round % matches.Count : (int)(Hash((uint)(serial * 313 + round * 53 + 7)) % (uint)matches.Count);
                deck.Add(matches[pick]);
            }
            return deck;
        }

        static MedianProblem BuildUniqueOdd(int m, int n, int variant, bool error, HashSet<string> signatures)
        {
            for (int salt = 0; salt < 1000; salt++)
            {
                uint seed = Hash((uint)(m * 100003 + n * 1009 + variant * 37 + salt * 7919));
                int k = (n - 1) / 2;
                var raw = new int[n];
                for (int i = 0; i < k; i++) { seed = Next(seed); raw[i] = (int)(seed % (uint)m); }
                raw[k] = m;
                for (int i = k + 1; i < n; i++) { seed = Next(seed); raw[i] = m + 1 + (int)(seed % (uint)(13 - m)); }
                Shuffle(raw, ref seed);
                string sig = n + ":" + error + ":" + string.Join(",", raw);
                if (!signatures.Add(sig)) continue;
                int wrong = error ? WrongMark(raw, 2 * m, seed) : 0;
                return Make("odd-" + n + "-m" + m + "-v" + variant, raw, 2 * m, error ? 3 : 1, error, wrong);
            }
            throw new InvalidOperationException("홀수 중앙값 버킷 생성 실패");
        }

        static MedianProblem BuildUniqueEven(int answer2, int n, int variant, bool error, HashSet<string> signatures)
        {
            var pairs = new List<int[]>();
            for (int a = 0; a <= 13; a++)
            {
                int b = answer2 - a;
                if (a < b && b <= 13) pairs.Add(new[] { a, b });
            }
            if (pairs.Count == 0) throw new InvalidOperationException("짝수 중앙값 쌍 없음: " + answer2);
            for (int salt = 0; salt < 2000; salt++)
            {
                uint seed = Hash((uint)(answer2 * 100003 + n * 1013 + variant * 41 + salt * 6151));
                int[] pair = pairs[(int)(seed % (uint)pairs.Count)];
                int a = pair[0], b = pair[1], k = n / 2 - 1;
                var raw = new int[n];
                for (int i = 0; i < k; i++) { seed = Next(seed); raw[i] = (int)(seed % (uint)(a + 1)); }
                raw[k] = a; raw[k + 1] = b;
                for (int i = k + 2; i < n; i++) { seed = Next(seed); raw[i] = b + (int)(seed % (uint)(14 - b)); }
                Shuffle(raw, ref seed);
                string sig = n + ":" + error + ":" + string.Join(",", raw);
                if (!signatures.Add(sig)) continue;
                int wrong = error ? WrongMark(raw, answer2, seed) : 0;
                return Make("even-" + n + "-a2" + answer2 + "-v" + variant, raw, answer2, error ? 3 : 2, error, wrong);
            }
            throw new InvalidOperationException("짝수 중앙값 버킷 생성 실패");
        }

        static MedianProblem Make(string id, int[] raw, int answer2, int band, bool error, int wrong)
        {
            string values = JoinValues(raw);
            string prompt = error
                ? "관측 시간 " + values + "초에 놓인 " + FormatHalf(wrong) + "초 표식은 잘못되었다. 중앙값을 구하시오."
                : "관측 시간 " + values + "초의 중앙값을 구하시오.";
            var problem = new MedianProblem { id = id, raw = raw, answer2 = answer2, band = band, errorFind = error, wrongMark2 = wrong, prompt = prompt };
            problem.wrongMarkMisconception = error ? ClassifyMisconception(problem, wrong) : "";
            return problem;
        }

        public static bool IsCorrect(MedianProblem p, int value2) => p != null && value2 == p.answer2;

        public static int RecalculateAnswer2(int[] raw)
        {
            var sorted = (int[])raw.Clone(); Array.Sort(sorted);
            int n = sorted.Length;
            return n % 2 == 1 ? 2 * sorted[n / 2] : sorted[n / 2 - 1] + sorted[n / 2];
        }

        public static string ClassifyMisconception(MedianProblem p, int selected2)
        {
            if (p == null || selected2 == p.answer2) return "";
            int n = p.raw.Length;
            int rawMiddle2 = n % 2 == 1 ? 2 * p.raw[n / 2] : p.raw[n / 2 - 1] + p.raw[n / 2];
            if (selected2 == rawMiddle2) return "raw_spatial_middle";
            var sorted = (int[])p.raw.Clone(); Array.Sort(sorted);
            if (n % 2 == 0 && selected2 == 2 * sorted[n / 2 - 1]) return "even_one_middle_low";
            if (n % 2 == 0 && selected2 == 2 * sorted[n / 2]) return "even_one_middle_high";
            int sum = 0, max = sorted[n - 1];
            for (int i = 0; i < n; i++) sum += sorted[i];
            if (selected2 * n == 2 * sum) return "mean_instead_of_median";
            if (selected2 == 2 * max) return "largest_as_median";
            int mode = UniqueMode(sorted);
            if (mode >= 0 && selected2 == 2 * mode) return "mode_as_median";
            return "median_order_error";
        }

        public static string MisconceptionKorean(string id)
        {
            switch (id)
            {
                case "raw_spatial_middle": return "보이는 순서의 가운데가 아니라 크기순 가운데를 찾으시오";
                case "even_one_middle_low":
                case "even_one_middle_high": return "짝수 개는 가운데 두 값의 평균을 구하시오";
                case "mean_instead_of_median": return "전체 평균이 아니라 크기순 가운데 값이다";
                case "largest_as_median": return "가장 큰 값이 아니라 크기순 가운데 값이다";
                case "mode_as_median": return "가장 자주 나온 값이 아니라 크기순 가운데 값이다";
                default: return "자료를 크기순으로 다시 놓고 가운데를 확인하시오";
            }
        }

        public static string Evidence(MedianProblem p)
        {
            var s = (int[])p.raw.Clone(); Array.Sort(s);
            string row = JoinValues(s);
            if (s.Length % 2 == 1) return row + "초  →  가운데 값 " + s[s.Length / 2] + "초";
            int a = s[s.Length / 2 - 1], b = s[s.Length / 2];
            return row + "초  →  (" + a + "+" + b + ")÷2=" + FormatHalf(p.answer2) + "초";
        }

        public static string FormatHalf(int value2) => value2 % 2 == 0 ? (value2 / 2).ToString() : (value2 / 2) + ".5";
        public static string JoinValues(int[] values)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++) { if (i > 0) sb.Append(", "); sb.Append(values[i]); }
            return sb.ToString();
        }

        static int UniqueMode(int[] sorted)
        {
            int bestValue = -1, bestCount = 1, ties = 0;
            for (int i = 0; i < sorted.Length;)
            {
                int j = i + 1; while (j < sorted.Length && sorted[j] == sorted[i]) j++;
                int count = j - i;
                if (count > bestCount) { bestCount = count; bestValue = sorted[i]; ties = 1; }
                else if (count == bestCount && count > 1) ties++;
                i = j;
            }
            return ties == 1 ? bestValue : -1;
        }

        // 오류 찾기 표식은 임의의 다른 눈금이 아니라 실제 중앙값 오개념에서만 고른다.
        // 생성된 문제는 항상 최댓값이 중앙값보다 크므로 후보가 하나도 없는 경우는 없다.
        static int WrongMark(int[] raw, int answer2, uint seed)
        {
            var candidates = new List<int>(6);
            int n = raw.Length;
            int rawMiddle2 = n % 2 == 1 ? 2 * raw[n / 2] : raw[n / 2 - 1] + raw[n / 2];
            AddWrongCandidate(candidates, rawMiddle2, answer2);

            var sorted = (int[])raw.Clone(); Array.Sort(sorted);
            if (n % 2 == 0)
            {
                AddWrongCandidate(candidates, 2 * sorted[n / 2 - 1], answer2);
                AddWrongCandidate(candidates, 2 * sorted[n / 2], answer2);
            }
            int sum = 0;
            for (int i = 0; i < n; i++) sum += sorted[i];
            if ((2 * sum) % n == 0) AddWrongCandidate(candidates, 2 * sum / n, answer2);
            AddWrongCandidate(candidates, 2 * sorted[n - 1], answer2);
            int mode = UniqueMode(sorted);
            if (mode >= 0) AddWrongCandidate(candidates, 2 * mode, answer2);

            // m=12 극단 자료처럼 모든 대표 오개념이 정답과 같거나 1~12 레일 밖이면
            // 정답 바로 옆의 온전한 1초 눈금을 일반적인 크기순 위치 오류로 사용한다.
            if (candidates.Count == 0) return answer2 == 24 ? 22 : answer2 + 2;
            return candidates[(int)(seed % (uint)candidates.Count)];
        }

        static void AddWrongCandidate(List<int> candidates, int value2, int answer2)
        {
            if (value2 < 2 || value2 > 24 || value2 == answer2 || candidates.Contains(value2)) return;
            candidates.Add(value2);
        }

        static void Shuffle(int[] a, ref uint state)
        {
            for (int i = a.Length - 1; i > 0; i--) { state = Next(state); int j = (int)(state % (uint)(i + 1)); (a[i], a[j]) = (a[j], a[i]); }
        }

        static uint Next(uint x) { x ^= x << 13; x ^= x >> 17; x ^= x << 5; return x; }
        static uint Hash(uint x) { x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16; return x; }

        static void Validate(List<MedianProblem> pool)
        {
            if (pool.Count != 516) throw new InvalidOperationException("문제 은행은 516개여야 합니다.");
            var ids = new HashSet<string>(); var prompts = new HashSet<string>();
            foreach (var p in pool)
            {
                if (!ids.Add(p.id) || !prompts.Add(p.prompt)) throw new InvalidOperationException("문항 중복: " + p.id);
                if (p.answer2 < 2 || p.answer2 > 24 || RecalculateAnswer2(p.raw) != p.answer2) throw new InvalidOperationException("중앙값 오류: " + p.id);
                int solutions = 0; for (int v = 2; v <= 24; v++) if (IsCorrect(p, v)) solutions++;
                if (solutions != 1) throw new InvalidOperationException("유일해 오류: " + p.id);
                if (p.errorFind && (p.wrongMark2 < 2 || p.wrongMark2 > 24 || p.wrongMark2 == p.answer2)) throw new InvalidOperationException("오류 표식 오류: " + p.id);
                if (p.errorFind && string.IsNullOrEmpty(p.wrongMarkMisconception))
                    throw new InvalidOperationException("설명할 수 없는 오류 표식: " + p.id);
                if (p.prompt.Contains("자리에서") || p.prompt.Contains("√") || !p.prompt.EndsWith("구하시오.")) throw new InvalidOperationException("발문 오류: " + p.id);
                foreach (int v in new[] { p.answer2, 2, 3, 8, 13, 24 })
                    if (v == p.answer2 && ClassifyMisconception(p, v).Length != 0) throw new InvalidOperationException("오개념 우연 참: " + p.id);
            }
        }
    }
}
