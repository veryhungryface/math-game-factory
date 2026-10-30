// 달팽이 퍼레이드 — 중3 산포도 정수 생성기와 순열 판정 정본.
//
// curriculum/2022-middle-math.json의 표현 계약을 코드에 옮긴다.
// - 발문은 중학교 지시형 「~가 작은 순으로 무리를 놓으시오」로 통일한다.
// - 편차는 변량−평균, 분산은 네 편차의 제곱의 합÷4로 int만 사용한다.
// - 표준편차 표기는 a√b 꼴(b는 가장 작은 자연수)이며 비교는 원래 분산 q끼리 한다.
// - choices는 없다. 학생이 모든 무리를 직접 옮겨 만든 전체 순열이 유일한 답이다.
// - 오개념은 mean_minus_value, variance_without_squares, range_instead_of_variance,
//   mean_rank, variance_std_printed_number로 코드에 남기고 제출 순서에서 역산한다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;

namespace Mgf.DalpaengiParade
{
    public enum ParadeBand { Deviation = 1, Variance = 2, StandardDeviation = 3 }

    [Serializable]
    public sealed class ParadeGroup
    {
        public string label = "";
        public int record;
        public int mean;
        public int[] values;
        // band1에서는 부호 있는 편차 d, band2/3에서는 분산 q.
        public int key;
        public bool showStandardDeviation;

        public string Compact(ParadeBand band)
        {
            if (band == ParadeBand.Deviation)
                return label + " 기록 " + record + " 초 · 평균 " + mean + " 초";
            if (band == ParadeBand.Variance)
                return label + " [" + Join(values) + "] · 평균 " + mean;
            return label + " " + (showStandardDeviation
                ? "표준편차 " + ParadeRules.SqrtText(key) + " 초"
                : "분산 " + key + " 초²");
        }

        static string Join(int[] a)
        {
            var s = new StringBuilder();
            for (int i = 0; i < a.Length; i++) { if (i > 0) s.Append(", "); s.Append(a[i]); }
            return s.ToString();
        }
    }

    [Serializable]
    public sealed class ParadeProblem
    {
        public string id = "";
        public string prompt = "";
        public string concept = "";
        public ParadeBand band;
        public ParadeGroup[] groups;
        public int[] answerOrder;
        public int[] distractorA;
        public int[] distractorB;
        public string misconceptionIdA = "";
        public string misconceptionIdB = "";

        public string AnswerText { get { return OrderText(answerOrder); } }

        public string OrderText(int[] order)
        {
            if (order == null) return "";
            var s = new StringBuilder();
            for (int i = 0; i < order.Length; i++)
            {
                if (i > 0) s.Append("→");
                s.Append(groups[order[i]].label);
            }
            return s.ToString();
        }

        public bool IsCorrect(int[] order)
        {
            return ParadeRules.SameOrder(order, answerOrder);
        }

        public string Diagnose(int[] order)
        {
            if (ParadeRules.SameOrder(order, distractorA)) return misconceptionIdA;
            if (ParadeRules.SameOrder(order, distractorB)) return misconceptionIdB;
            return "adjacent_inversion";
        }

        public MgfProblem ToMgf()
        {
            return new MgfProblem {
                id = id,
                prompt = prompt,
                choices = null,
                answer = AnswerText,
                answerNumeric = double.NaN,
                unitConcept = concept
            };
        }
    }

    public static class ParadeRules
    {
        public const int StartLives = 3;
        public const int GoalRounds = 6;
        public const int RequiredFirstCorrect = 5;
        public const float SessionSeconds = 180f;
        static readonly int[] QPool = { 1, 2, 3, 4, 5, 6, 8, 9, 10, 11, 12, 16, 18, 20, 25 };
        static Dictionary<int, List<int[]>> varianceVectors;

        public static ParadeProblem Practice()
        {
            var groups = new[] {
                Dev("A", 8, 10), Dev("B", 12, 12), Dev("C", 9, 7)
            };
            return Finish("dp-practice", ParadeBand.Deviation, groups,
                "표시한 기록의 편차가 작은 순으로 무리를 놓으시오.", "편차=변량−평균");
        }

        public static List<ParadeProblem> RunDeck(int runSerial)
        {
            var list = new List<ParadeProblem>(GoalRounds);
            list.Add(MakeDeviation("dp-run-" + runSerial + "-1", runSerial * 17 + 1));
            list.Add(MakeDeviation("dp-run-" + runSerial + "-2", runSerial * 17 + 7));

            // 필수 반례: 범위는 모두 4지만 분산은 2,3,4.
            list.Add(Finish("dp-run-" + runSerial + "-3", ParadeBand.Variance, Shuffle(new[] {
                Var("", new[]{6,8,8,10}, 8), Var("", new[]{2,6,6,6}, 5), Var("", new[]{10,10,14,14}, 12)
            }, runSerial * 31 + 3), "분산이 작은 순으로 무리를 놓으시오.", "편차의 제곱의 평균인 분산"));

            // 필수 반례: 편차 절댓값 합은 모두 12지만 분산은 9,10,12.
            list.Add(Finish("dp-run-" + runSerial + "-4", ParadeBand.Variance, Shuffle(new[] {
                Var("", new[]{7,7,13,13}, 10), Var("", new[]{8,10,14,16}, 12), Var("", new[]{2,10,10,10}, 8)
            }, runSerial * 37 + 5), "분산이 작은 순으로 무리를 놓으시오.", "편차의 제곱의 평균인 분산"));

            list.Add(MakeStandard("dp-run-" + runSerial + "-5", runSerial * 41 + 11, false));
            list.Add(MakeStandard("dp-run-" + runSerial + "-6", runSerial * 43 + 13, true));
            return list;
        }

        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(360);
            var prompts = new HashSet<string>();
            for (int serial = 1; serial <= 360; serial++)
            {
                ParadeProblem p;
                int band = (serial - 1) % 3;
                if (band == 0) p = MakeDeviation("dp-bank-" + serial.ToString("D3"), serial * 101 + 7);
                else if (band == 1) p = MakeVariance("dp-bank-" + serial.ToString("D3"), serial * 103 + 9);
                else p = MakeStandard("dp-bank-" + serial.ToString("D3"), serial * 107 + 11, serial % 2 == 0);
                p.prompt = "행진 기록 " + serial + ". " + p.prompt;
                if (!prompts.Add(p.prompt)) throw new InvalidOperationException("중복 발문 " + p.prompt);
                bank.Add(p.ToMgf());
            }
            return bank;
        }

        public static ParadeProblem MakeDeviation(string id, int seed)
        {
            int[][] patterns = {
                new[]{-5,-1,3}, new[]{-4,1,5}, new[]{-3,0,4}, new[]{-2,2,5},
                new[]{-5,2,4}, new[]{-4,-1,2}, new[]{-3,1,5}, new[]{-2,0,3}
            };
            int[] ds = patterns[Positive(seed) % patterns.Length];
            var g = new ParadeGroup[3];
            for (int i = 0; i < 3; i++)
            {
                // 평균 순위와 편차 순위가 같지 않도록 지그재그 평균을 쓴다.
                int mean = 8 + ((Positive(seed / 3 + i * 5) + (2 - i) * 7) % 13);
                int record = mean + ds[i];
                if (record <= 0) { mean += 6; record = mean + ds[i]; }
                g[i] = Dev("", record, mean);
            }
            g = Shuffle(g, seed * 13 + 5);
            return Finish(id, ParadeBand.Deviation, g,
                "표시한 기록의 편차가 작은 순으로 무리를 놓으시오.", "편차=변량−평균");
        }

        public static ParadeProblem MakeVariance(string id, int seed)
        {
            EnsureVarianceVectors();
            int[][] qSets = {
                new[]{1,5,9}, new[]{2,6,11}, new[]{3,8,12}, new[]{4,10,16},
                new[]{2,5,10}, new[]{3,6,9}, new[]{4,8,12}, new[]{5,11,16}
            };
            int[] qs = qSets[Positive(seed) % qSets.Length];
            var groups = new ParadeGroup[3];
            for (int i = 0; i < 3; i++)
            {
                var pool = varianceVectors[qs[i]];
                int[] d = pool[Positive(seed + i * 97) % pool.Count];
                int mean = 8 + Positive(seed / 7 + i * 11) % 11;
                int min = d[0];
                for (int k = 1; k < 4; k++) if (d[k] < min) min = d[k];
                if (mean + min <= 0) mean = 1 - min;
                int[] values = new int[4];
                for (int k = 0; k < 4; k++) values[k] = mean + d[(k + Positive(seed + i) % 4) % 4];
                groups[i] = Var("", values, mean);
            }
            groups = Shuffle(groups, seed * 17 + 3);
            return Finish(id, ParadeBand.Variance, groups,
                "분산이 작은 순으로 무리를 놓으시오.", "편차의 제곱의 평균인 분산");
        }

        public static ParadeProblem MakeStandard(string id, int seed, bool fullPool)
        {
            int[] source = fullPool ? QPool : new[]{1,2,3,4,5,6,8,9,10,11,12};
            var picked = new List<int>();
            int cursor = Positive(seed) % source.Length;
            while (picked.Count < 4)
            {
                int q = source[cursor % source.Length];
                if (!picked.Contains(q)) picked.Add(q);
                cursor += 3 + picked.Count;
            }
            var groups = new ParadeGroup[4];
            for (int i = 0; i < 4; i++)
            {
                groups[i] = new ParadeGroup {
                    key = picked[i], values = null, mean = 0, record = 0,
                    showStandardDeviation = (i % 2) == 1
                };
            }
            groups = Shuffle(groups, seed * 19 + 7);
            // 셔플 뒤에도 반드시 분산 2개·표준편차 2개가 남는다.
            return Finish(id, ParadeBand.StandardDeviation, groups,
                "표준편차가 작은 순으로 무리를 놓으시오.", "표준편차=√분산");
        }

        static ParadeProblem Finish(string id, ParadeBand band, ParadeGroup[] groups, string instruction, string concept)
        {
            for (int i = 0; i < groups.Length; i++) groups[i].label = ((char)('A' + i)).ToString();
            int[] answer = SortIndices(groups, delegate(ParadeGroup g) { return g.key; });
            var p = new ParadeProblem { id = id, band = band, groups = groups, answerOrder = answer, concept = concept };
            var sb = new StringBuilder(instruction).Append("  ");
            for (int i = 0; i < groups.Length; i++) { if (i > 0) sb.Append(" / "); sb.Append(groups[i].Compact(band)); }
            p.prompt = sb.ToString();

            if (band == ParadeBand.Deviation)
            {
                p.misconceptionIdA = "mean_minus_value";
                p.distractorA = Reverse(answer);
                p.misconceptionIdB = "mean_rank";
                p.distractorB = SortIndices(groups, delegate(ParadeGroup g) { return g.mean; });
            }
            else if (band == ParadeBand.Variance)
            {
                p.misconceptionIdA = "range_instead_of_variance";
                p.distractorA = SortIndices(groups, delegate(ParadeGroup g) { return Range(g.values); });
                p.misconceptionIdB = "mean_rank";
                p.distractorB = SortIndices(groups, delegate(ParadeGroup g) { return g.mean; });
            }
            else
            {
                p.misconceptionIdA = "variance_std_printed_number";
                p.distractorA = SortIndices(groups, PrintedNumberKey);
                p.misconceptionIdB = "variance_std_relation_reversed";
                p.distractorB = Reverse(answer);
            }
            // 동률 전략은 답을 결정하지 못한다. 진단용 배열이 우연히 정답이면 인접 두 칸을 바꾸어
            // 정답 선택지로 오인되는 일을 막고 generic adjacent_inversion으로만 다룬다.
            if (SameOrder(p.distractorA, answer)) p.distractorA = SwapFirst(answer);
            if (SameOrder(p.distractorB, answer)) p.distractorB = SwapLast(answer);
            ValidateProblem(p);
            return p;
        }

        public static bool ValidateAll(out string error)
        {
            try
            {
                ValidateProblem(Practice());
                for (int r = 1; r <= 256; r++)
                {
                    var deck = RunDeck(r);
                    if (deck.Count != GoalRounds) throw new Exception("deck count");
                    for (int i = 0; i < deck.Count; i++) ValidateProblem(deck[i]);
                }
                var bank = BuildBank();
                if (bank.Count < 300) throw new Exception("bank too small");
                for (int i = 0; i < bank.Count; i++)
                {
                    if (bank[i].choices != null && bank[i].choices.Length > 0) throw new Exception("choices are forbidden");
                    if (string.IsNullOrEmpty(bank[i].answer) || bank[i].answer.IndexOf("→", StringComparison.Ordinal) < 0)
                        throw new Exception("permutation answer missing");
                }
                error = ""; return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        static void ValidateProblem(ParadeProblem p)
        {
            if (p.groups == null || p.groups.Length < 3 || p.groups.Length > 4) throw new Exception("group count " + p.id);
            var keys = new HashSet<int>();
            for (int i = 0; i < p.groups.Length; i++)
            {
                var g = p.groups[i];
                if (!keys.Add(g.key)) throw new Exception("tie " + p.id);
                if (p.band == ParadeBand.Deviation && g.record - g.mean != g.key) throw new Exception("deviation " + p.id);
                if (p.band == ParadeBand.Variance)
                {
                    if (g.values == null || g.values.Length != 4) throw new Exception("values " + p.id);
                    int sum = 0, sq = 0;
                    for (int k = 0; k < 4; k++) { sum += g.values[k]; int d = g.values[k] - g.mean; sq += d * d; }
                    if (sum != 4 * g.mean || sq != 4 * g.key) throw new Exception("variance " + p.id);
                }
                if (p.band == ParadeBand.StandardDeviation && g.key <= 0) throw new Exception("std key " + p.id);
            }
            for (int i = 1; i < p.answerOrder.Length; i++)
                if (p.groups[p.answerOrder[i - 1]].key >= p.groups[p.answerOrder[i]].key) throw new Exception("order " + p.id);
            if (SameOrder(p.distractorA, p.answerOrder) || SameOrder(p.distractorB, p.answerOrder))
                throw new Exception("misconception accidentally true " + p.id);
        }

        static ParadeGroup Dev(string label, int record, int mean)
        {
            return new ParadeGroup { label = label, record = record, mean = mean, key = record - mean };
        }

        static ParadeGroup Var(string label, int[] values, int mean)
        {
            int sq = 0;
            for (int i = 0; i < values.Length; i++) { int d = values[i] - mean; sq += d * d; }
            if (sq % 4 != 0) throw new InvalidOperationException("분산이 정수가 아님");
            return new ParadeGroup { label = label, values = values, mean = mean, key = sq / 4 };
        }

        public static string SqrtText(int q)
        {
            int a = 1, b = q;
            for (int f = 2; f * f <= b; f++)
            {
                int square = f * f;
                while (b % square == 0) { a *= f; b /= square; }
            }
            if (b == 1) return a.ToString();
            return (a == 1 ? "" : a.ToString()) + "√" + b;
        }

        static int PrintedNumberKey(ParadeGroup g)
        {
            if (!g.showStandardDeviation) return g.key * 100;
            int a = 1, b = g.key;
            for (int f = 2; f * f <= b; f++) while (b % (f * f) == 0) { a *= f; b /= f * f; }
            return a * 100 + b; // 학생이 인쇄 숫자만 읽는 잘못된 정책의 정수 대용 키.
        }

        static int Range(int[] a)
        {
            int min = a[0], max = a[0];
            for (int i = 1; i < a.Length; i++) { if (a[i] < min) min = a[i]; if (a[i] > max) max = a[i]; }
            return max - min;
        }

        static int[] SortIndices(ParadeGroup[] groups, Func<ParadeGroup, int> key)
        {
            var idx = new int[groups.Length];
            for (int i = 0; i < idx.Length; i++) idx[i] = i;
            Array.Sort(idx, delegate(int a, int b) { int c = key(groups[a]).CompareTo(key(groups[b])); return c != 0 ? c : a.CompareTo(b); });
            return idx;
        }

        static ParadeGroup[] Shuffle(ParadeGroup[] source, int seed)
        {
            var a = (ParadeGroup[])source.Clone();
            var rng = new Random(Positive(seed));
            for (int i = a.Length - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = a[i]; a[i] = a[j]; a[j] = t; }
            return a;
        }

        static void EnsureVarianceVectors()
        {
            if (varianceVectors != null) return;
            varianceVectors = new Dictionary<int, List<int[]>>();
            for (int i = 0; i < QPool.Length; i++) varianceVectors[QPool[i]] = new List<int[]>();
            for (int a = -6; a <= 6; a++)
            for (int b = a; b <= 6; b++)
            for (int c = b; c <= 6; c++)
            {
                int d = -a - b - c;
                if (d < c || d > 6) continue;
                int sq = a*a + b*b + c*c + d*d;
                if (sq % 4 != 0) continue;
                int q = sq / 4;
                if (varianceVectors.ContainsKey(q)) varianceVectors[q].Add(new[]{a,b,c,d});
            }
            foreach (var pair in varianceVectors) if (pair.Value.Count == 0) throw new InvalidOperationException("분산 벡터 풀 없음 q=" + pair.Key);
        }

        static int[] Reverse(int[] a) { var r = (int[])a.Clone(); Array.Reverse(r); return r; }
        static int[] SwapFirst(int[] a) { var r = (int[])a.Clone(); int t=r[0];r[0]=r[1];r[1]=t;return r; }
        static int[] SwapLast(int[] a) { var r=(int[])a.Clone();int n=r.Length;int t=r[n-1];r[n-1]=r[n-2];r[n-2]=t;return r; }
        public static bool SameOrder(int[] a, int[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
        static int Positive(int x) { return x == int.MinValue ? int.MaxValue : Math.Abs(x); }
    }
}
