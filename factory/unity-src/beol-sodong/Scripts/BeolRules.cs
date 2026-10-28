using System;
using System.Collections.Generic;
using System.Text;
using Mgf;

namespace Mgf.BeolSodong
{
    [Serializable]
    public sealed class SwarmProblem
    {
        public string id;
        public int band;
        public int[] values = new int[9];
        public int[] modes = Array.Empty<int>();
        public int maxFrequency;
        public int expectedMask;
        public string prompt;

        public string AnswerText()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < modes.Length; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(modes[i]);
            }
            sb.Append(" | 벌 ");
            bool first = true;
            for (int i = 0; i < 9; i++) if ((expectedMask & (1 << i)) != 0)
            {
                if (!first) sb.Append("·");
                first = false;
                sb.Append(i + 1);
            }
            return sb.ToString();
        }

        public MgfProblem ToMgf()
        {
            return new MgfProblem
            {
                id = id,
                prompt = prompt,
                choices = Array.Empty<string>(),
                answer = AnswerText(),
                answerNumeric = modes.Length == 1 ? modes[0] : double.NaN,
                unitConcept = modes.Length == 1 ? "최빈값과 도수" : "여러 개의 최빈값"
            };
        }
    }

    public static class BeolRules
    {
        public const int Goal = 9;
        public const int StartLives = 3;
        public const int RunSeconds = 105;
        public const int MasteryFirst = 7;
        public const int CandidateCount = 9;

        // 중학교 정본의 expression_traps를 이 생성기에 옮긴다.
        // - 문제 발문은 중학교 지시형(~을/를 구하시오)으로 통일한다.
        // - 이번 차시는 최빈값만 다루며 중앙값·상대도수·확률을 평가했다고 쓰지 않는다.
        // - 최빈값은 가장 큰 값이나 최대 도수 자체가 아니라 가장 자주 나타난 변량이다.
        // - 공동 최빈값은 최대 도수가 같은 모든 변량을 답으로 삼는다.

        public static SwarmProblem Practice()
        {
            return FromValues("practice-fixed", 0, new[] { 2, 7, 4, 2, 9, 2, 5, 11, 13 });
        }

        public static SwarmProblem Bridge()
        {
            return FromValues("bridge-fixed", 3, new[] { 4, 7, 4, 1, 7, 4, 7, 10, 12 });
        }

        public static List<SwarmProblem> BuildBank()
        {
            var list = new List<SwarmProblem>(360);
            for (int band = 1; band <= 3; band++)
            {
                var signatures = new HashSet<string>(StringComparer.Ordinal);
                int serial = 0;
                while (signatures.Count < 120 && serial < 20000)
                {
                    SwarmProblem p = Generate(band, serial++);
                    string sig = Signature(p);
                    if (signatures.Add(sig)) list.Add(p);
                }
                if (signatures.Count != 120) throw new InvalidOperationException("벌 소동 문제 은행 생성 실패: band " + band);
            }
            return list;
        }

        public static List<SwarmProblem> RunDeck(List<SwarmProblem> bank, int seed)
        {
            var rng = new Random(seed);
            var deck = new List<SwarmProblem>(Goal);
            for (int band = 1; band <= 3; band++)
            {
                var pool = new List<SwarmProblem>();
                for (int i = 0; i < bank.Count; i++) if (bank[i].band == band) pool.Add(bank[i]);
                for (int i = 0; i < 3; i++)
                {
                    int j = rng.Next(i, pool.Count);
                    SwarmProblem t = pool[i]; pool[i] = pool[j]; pool[j] = t;
                    deck.Add(pool[i]);
                }
            }
            return deck;
        }

        static SwarmProblem Generate(int band, int serial)
        {
            var rng = new Random(9143 + band * 100003 + serial * 7919);
            var labels = new List<int>(5);
            var counts = new List<int>(5);
            if (band == 1)
            {
                PickDistinct(rng, labels, 1, 18, 5);
                counts.AddRange(new[] { 4, 2, 1, 1, 1 });
                ShuffleTogether(rng, labels, counts);
            }
            else if (band == 2)
            {
                bool frequencyValueTrap = serial % 3 == 0;
                if (frequencyValueTrap) labels.Add(3);
                PickDistinct(rng, labels, 1, 18, 4);
                labels.Add(19 + serial % 6); // 최댓값은 구조적으로 단 한 번만 등장한다.

                int modeIndex = frequencyValueTrap ? 1 + rng.Next(3) : rng.Next(4);
                counts.AddRange(new[] { 1, 1, 1, 1, 1 });
                counts[modeIndex] = 3;
                var twos = new List<int>();
                for (int i = 0; i < 4; i++) if (i != modeIndex && (!frequencyValueTrap || i != 0)) twos.Add(i);
                while (twos.Count < 2) for (int i = 0; i < 4; i++) if (i != modeIndex && !twos.Contains(i)) twos.Add(i);
                Shuffle(rng, twos);
                counts[twos[0]] = 2; counts[twos[1]] = 2;
                // labels[4]의 큰 값과 frequencyValueTrap의 3은 언제나 도수 1이다.
            }
            else
            {
                PickDistinct(rng, labels, 1, 24, 5);
                counts.AddRange(new[] { 3, 3, 1, 1, 1 });
                ShuffleTogether(rng, labels, counts);
            }

            var raw = new List<int>(9);
            for (int i = 0; i < labels.Count; i++) for (int k = 0; k < counts[i]; k++) raw.Add(labels[i]);
            Shuffle(rng, raw);
            return FromValues("mode-b" + band + "-" + serial.ToString("D3"), band, raw.ToArray());
        }

        static void PickDistinct(Random rng, List<int> target, int min, int max, int total)
        {
            var pool = new List<int>();
            for (int v = min; v <= max; v++) if (!target.Contains(v)) pool.Add(v);
            Shuffle(rng, pool);
            for (int i = 0; target.Count < total; i++) target.Add(pool[i]);
        }

        static void ShuffleTogether(Random rng, List<int> a, List<int> b)
        {
            for (int i = a.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = a[i]; a[i] = a[j]; a[j] = t;
                t = b[i]; b[i] = b[j]; b[j] = t;
            }
        }

        static void Shuffle<T>(Random rng, List<T> a)
        {
            for (int i = a.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); T t = a[i]; a[i] = a[j]; a[j] = t; }
        }

        public static SwarmProblem FromValues(string id, int band, int[] values)
        {
            var freq = new Dictionary<int, int>();
            for (int i = 0; i < values.Length; i++) freq[values[i]] = freq.TryGetValue(values[i], out int n) ? n + 1 : 1;
            int max = 0;
            foreach (var kv in freq) if (kv.Value > max) max = kv.Value;
            var modes = new List<int>();
            foreach (var kv in freq) if (kv.Value == max) modes.Add(kv.Key);
            modes.Sort();
            int mask = 0;
            for (int i = 0; i < values.Length; i++) if (modes.BinarySearch(values[i]) >= 0) mask |= 1 << i;
            string instruction = modes.Count > 1 ? "꽃 방문 횟수의 최빈값을 모두 구하고, 그 기록의 벌을 모두 이으시오." : "꽃 방문 횟수의 최빈값을 구하고, 그 기록의 벌을 모두 이으시오.";
            return new SwarmProblem
            {
                id = id,
                band = band,
                values = (int[])values.Clone(),
                modes = modes.ToArray(),
                maxFrequency = max,
                expectedMask = mask,
                prompt = instruction + " 자료(회): " + string.Join(", ", values)
            };
        }

        public static bool IsExact(SwarmProblem p, int selectedMask) => p != null && selectedMask == p.expectedMask;

        public static string MisconceptionId(SwarmProblem p, int selectedMask)
        {
            if (p == null) return "empty-selection";
            if (selectedMask == 0) return "empty-selection";
            int maxValue = 0;
            for (int i = 0; i < p.values.Length; i++) maxValue = Math.Max(maxValue, p.values[i]);
            if (selectedMask == MaskForValue(p, maxValue)) return "mode-as-largest-value";
            if (selectedMask == MaskForValue(p, p.maxFrequency) && p.modes.Length == 1 && p.modes[0] != p.maxFrequency)
                return "mode-as-frequency";
            for (int i = 0; i < p.modes.Length; i++)
            {
                int one = MaskForValue(p, p.modes[i]);
                if (selectedMask == one && p.modes.Length > 1) return "mode-assumed-single";
            }
            if ((selectedMask & ~p.expectedMask) == 0) return "mode-incomplete-occurrences";
            return "included-nonmode";
        }

        public static string WrongReason(SwarmProblem p, int selectedMask)
        {
            string id = MisconceptionId(p, selectedMask);
            if (id == "mode-as-largest-value") return "가장 큰 값이 아니라 가장 많이 나타난 값을 찾으시오.";
            if (id == "mode-as-frequency") return p.maxFrequency + "은 나타난 횟수이다. 그 횟수로 나타난 값을 찾으시오.";
            if (id == "mode-assumed-single") return "최빈값이 둘이다. 같은 도수의 두 무리를 모두 이으시오.";
            if (id == "mode-incomplete-occurrences") return "같은 최빈값의 벌이 더 있다. 빠짐없이 모두 이으시오.";
            if (id == "empty-selection") return "숫자표에서 시작해 여러 벌을 한 획으로 이으시오.";
            return "최빈값이 아닌 벌이 선에 닿았다. 빈 통로로 우회하시오.";
        }

        public static int DeliberateWrongMask(SwarmProblem p)
        {
            int maxValue = 0;
            for (int i = 0; i < p.values.Length; i++) maxValue = Math.Max(maxValue, p.values[i]);
            int mask = MaskForValue(p, maxValue);
            if (mask == p.expectedMask || mask == 0) mask = p.expectedMask ^ 1;
            return mask & 511;
        }

        public static int MaskForValue(SwarmProblem p, int value)
        {
            int mask = 0;
            for (int i = 0; i < p.values.Length; i++) if (p.values[i] == value) mask |= 1 << i;
            return mask;
        }

        static string Signature(SwarmProblem p)
        {
            var freq = new Dictionary<int, int>();
            for (int i = 0; i < p.values.Length; i++) freq[p.values[i]] = freq.TryGetValue(p.values[i], out int n) ? n + 1 : 1;
            var keys = new List<int>(freq.Keys); keys.Sort();
            var sb = new StringBuilder(p.band + "|");
            for (int i = 0; i < keys.Count; i++) sb.Append(keys[i]).Append('x').Append(freq[keys[i]]).Append(';');
            return sb.ToString();
        }

        public static int AuditBank(IList<SwarmProblem> bank, StringBuilder log = null)
        {
            int errors = 0;
            void Err(string s) { errors++; if (log != null && errors <= 30) log.AppendLine(s); }
            if (bank == null || bank.Count < 360) Err("bank count");
            var ids = new HashSet<string>(); var prompts = new HashSet<string>();
            int[] byBand = new int[4];
            for (int k = 0; k < bank.Count; k++)
            {
                SwarmProblem p = bank[k]; byBand[p.band]++;
                if (!ids.Add(p.id)) Err("duplicate id " + p.id);
                if (!prompts.Add(p.prompt)) Err("duplicate prompt " + p.id);
                if (p.values.Length != 9) Err("length " + p.id);
                var freq = new Dictionary<int, int>();
                for (int i = 0; i < 9; i++) freq[p.values[i]] = freq.TryGetValue(p.values[i], out int n) ? n + 1 : 1;
                if (freq.Count != 5) Err("distinct " + p.id);
                int max = 0; foreach (var kv in freq) max = Math.Max(max, kv.Value);
                int computed = 0; foreach (var kv in freq) if (kv.Value == max) computed |= MaskForValue(p, kv.Key);
                if (computed != p.expectedMask || max != p.maxFrequency) Err("answer " + p.id);
                int expectedCount = p.band == 1 ? 4 : p.band == 2 ? 3 : 6;
                if (BitCount(p.expectedMask) != expectedCount) Err("target count " + p.id);
                if (p.band == 2)
                {
                    int largest = 0; foreach (var kv in freq) largest = Math.Max(largest, kv.Key);
                    if (freq[largest] != 1) Err("largest singleton " + p.id);
                }
                int accepted = 0;
                for (int mask = 0; mask < 512; mask++) if (IsExact(p, mask)) accepted++;
                if (accepted != 1) Err("subset uniqueness " + p.id);
            }
            for (int b = 1; b <= 3; b++) if (byBand[b] != 120) Err("band " + b + " count " + byBand[b]);
            return errors;
        }

        public static int BitCount(int mask)
        {
            int n = 0; while (mask != 0) { n += mask & 1; mask >>= 1; } return n;
        }
    }
}
