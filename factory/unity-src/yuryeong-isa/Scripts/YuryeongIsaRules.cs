// 유령 이사 — 네 변량 중 한 곳에서 2를 빼고 다른 곳에 2를 더하는 산포도 구성 규칙.
// 모든 판정은 정수의 편차 제곱합으로 한다. float/double 비교는 사용하지 않는다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;

namespace Mgf.YuryeongIsa
{
    [Serializable]
    public sealed class TransferProblem
    {
        public string id;
        public int[] values;
        public int mean;
        public int startVariance;
        public int targetVariance;
        public int from;
        public int to;
        public int band;
        public string prompt;

        public int[] ResultValues()
        {
            var result = (int[])values.Clone();
            result[from] -= 2; result[to] += 2;
            return result;
        }

        public MgfProblem ToMgf()
        {
            string move = YuryeongIsaRules.HouseName(from) + "→" + YuryeongIsaRules.HouseName(to);
            return new MgfProblem
            {
                id = id,
                prompt = prompt,
                choices = null,
                answer = move + " / 결과 " + YuryeongIsaRules.Join(ResultValues()) + " / 분산 " + targetVariance,
                answerNumeric = targetVariance,
                unitConcept = band < 3 ? "분산을 만드는 자료 구성" : "표준편차에서 분산을 역산하는 자료 구성"
            };
        }
    }

    public static class YuryeongIsaRules
    {
        public const int StartLives = 3;
        public const int RoundCount = 8;
        public const int MasteryNeeded = 6;
        public const int SessionSeconds = 180;

        static readonly string[] Houses = { "첫째 집", "둘째 집", "셋째 집", "넷째 집" };
        static readonly string[] LedgerA =
        {
            "구리", "살구", "벽돌", "상아", "노을", "커튼", "화분", "굴뚝",
            "발코니", "골목", "등불", "덧문", "기와", "빨랫줄", "우편함", "종소리",
            "달빛", "연기", "석양", "다락", "처마", "창틀", "마당", "계단",
            "바람", "저녁", "가로등", "옥상", "벽난로", "난간", "창문", "테라코타"
        };
        static readonly string[] LedgerB =
        {
            "연립", "안뜰", "회랑", "간판", "창고", "다리", "거리", "마을",
            "주택", "기록", "이삿짐", "봉인", "수첩", "장부", "열쇠", "정류장",
            "벽화", "분수대", "작업실", "우체국", "시장", "정원", "공방", "광장",
            "전망대", "문패", "골목길", "다락방", "연립동", "저택", "탑", "현관"
        };

        // 중학교 expression_traps 반영:
        // 1) 발문은 문제용 지시형 「~하시오」로 통일한다.
        // 2) 표준편차는 √분산이며 근호는 a√b 꼴(b는 가장 작은 자연수)로 정규화한다.
        // 3) 분산은 편차 제곱합÷4이다. 표본분산의 n−1은 사용하지 않는다.
        // 4) 어림 발문과 부동소수점 근삿값은 이 단원 게임에서 사용하지 않는다.
        // 오답은 버튼 선택지가 아니라 실제 이동이다. 아래 misconceptionId는 대표 오개념에서 역산한다.
        public const string MisAlwaysEven = "always_max_to_min";
        public const string MisVarianceSigma = "variance_standard_deviation_confusion";
        public const string MisDeviationAverage = "deviation_average_is_variance";
        public const string MisUnplanned = "unplanned_transfer";

        public static TransferProblem Practice()
        {
            int[] values = { 2, 6, 12, 20 };
            return Make("practice-fixed", values, 3, 0, 0);
        }

        public static List<int[]> BuildBasePool()
        {
            var pool = new List<int[]>(40);
            for (int a = 2; a <= 30; a += 2)
            for (int b = a + 2; b <= 30; b += 2)
            for (int c = b + 2; c <= 30; c += 2)
            for (int d = c + 2; d <= 30; d += 2)
            {
                int[] v = { a, b, c, d };
                int sum = a + b + c + d;
                if (sum % 4 != 0) continue;
                int mean = sum / 4;
                int maxDistance = Math.Max(Math.Max(Math.Abs(a - mean), Math.Abs(b - mean)), Math.Max(Math.Abs(c - mean), Math.Abs(d - mean)));
                if (maxDistance > 13) continue;

                var directedDifferences = new HashSet<int>();
                bool valid = true;
                for (int i = 0; i < 4 && valid; i++)
                for (int j = 0; j < 4; j++) if (i != j)
                {
                    int difference = v[j] - v[i];
                    if (Math.Abs(difference) == 2 || !directedDifferences.Add(difference)) { valid = false; break; }
                }
                if (!valid || directedDifferences.Count != 12) continue;

                int start = Variance(v);
                var results = new HashSet<int>();
                for (int i = 0; i < 4 && valid; i++)
                for (int j = 0; j < 4; j++) if (i != j)
                {
                    int result = VarianceAfter(v, i, j);
                    if (result < 0 || result > 144 || result == start || !results.Add(result)) { valid = false; break; }
                }
                if (valid && results.Count == 12) pool.Add(v);
            }
            if (pool.Count != 40) throw new InvalidOperationException("산포도 기초 자료는 정확히 40개여야 합니다: " + pool.Count);
            return pool;
        }

        public static List<TransferProblem> BuildBank()
        {
            var bases = BuildBasePool();
            var permutations = BuildPermutations();
            var bank = new List<TransferProblem>(960);
            var signatures = new HashSet<string>();
            int serial = 0;
            for (int b = 0; b < bases.Count && bank.Count < 960; b++)
            for (int p = 0; p < permutations.Count && bank.Count < 960; p++)
            {
                int[] arranged = Permute(bases[b], permutations[p]);
                for (int move = 0; move < 12 && bank.Count < 960; move++)
                {
                    MoveAt(move, out int from, out int to);
                    int band = 1 + (serial % 8 >= 5 ? 2 : serial % 8 >= 2 ? 1 : 0);
                    var problem = Make("dispersion-" + serial, arranged, from, to, band);
                    string signature = problem.prompt + "|" + problem.targetVariance;
                    if (!signatures.Add(signature)) throw new InvalidOperationException("문제 은행 중복: " + signature);
                    bank.Add(problem); serial++;
                }
            }
            if (bank.Count < 300) throw new InvalidOperationException("문제 은행 300개 미만");
            ValidateBank(bank);
            return bank;
        }

        public static List<TransferProblem> BuildDeck(List<int[]> pool, int runSerial)
        {
            var deck = new List<TransferProblem>(RoundCount);
            var permutations = BuildPermutations();
            var usedBases = new HashSet<int>();
            for (int round = 0; round < RoundCount; round++)
            {
                int baseIndex = PositiveMod(runSerial * 7 + round * 11, pool.Count);
                while (!usedBases.Add(baseIndex)) baseIndex = (baseIndex + 1) % pool.Count;
                int permIndex = PositiveMod((int)Hash((uint)(runSerial * 101 + round * 37 + 17)), permutations.Count);
                int move = round == 0
                    ? PositiveMod(runSerial - 1, 12) // 독립 시작 문제 200판에서 각 방향이 16~17회로 정확히 균형.
                    : PositiveMod((int)Hash((uint)(runSerial * 313 + round * 79 + 29)), 12);
                MoveAt(move, out int from, out int to);
                int band = round < 2 ? 1 : round < 5 ? 2 : 3;
                deck.Add(Make("run-" + runSerial + "-r" + (round + 1), Permute(pool[baseIndex], permutations[permIndex]), from, to, band));
            }
            return deck;
        }

        public static TransferProblem Make(string id, int[] values, int from, int to, int band)
        {
            if (from == to || from < 0 || to < 0 || from > 3 || to > 3) throw new ArgumentOutOfRangeException("이사 방향");
            int[] copy = (int[])values.Clone();
            int sum = 0; for (int i = 0; i < 4; i++) sum += copy[i];
            if (sum % 4 != 0) throw new InvalidOperationException("평균이 정수가 아닌 자료");
            int mean = sum / 4;
            int start = Variance(copy);
            int target = VarianceAfter(copy, from, to);
            string targetText = band < 3 ? "분산이 " + target : "표준편차가 " + SqrtForm(target);
            string prompt = LedgerName(id) + " 이사 장부의 네 집 주민 수 " + Join(copy) + "에서 두 유령을 다른 한 집으로 옮겨 " + targetText + "가 되도록 하시오.";
            return new TransferProblem
            {
                id = id, values = copy, mean = mean, startVariance = start, targetVariance = target,
                from = from, to = to, band = band, prompt = prompt
            };
        }

        public static int Variance(int[] values)
        {
            if (values == null || values.Length != 4) throw new ArgumentException("변량 네 개가 필요합니다");
            int sum = values[0] + values[1] + values[2] + values[3];
            if (sum % 4 != 0) return -1;
            int mean = sum / 4, squareSum = 0;
            for (int i = 0; i < 4; i++) { int d = values[i] - mean; squareSum += d * d; }
            return squareSum % 4 == 0 ? squareSum / 4 : -1;
        }

        public static int VarianceAfter(int[] values, int from, int to)
        {
            int[] changed = (int[])values.Clone(); changed[from] -= 2; changed[to] += 2;
            return Variance(changed);
        }

        public static bool IsCorrect(TransferProblem problem, int from, int to)
            => problem != null && from != to && VarianceAfter(problem.values, from, to) == problem.targetVariance;

        public static string ClassifyWrong(TransferProblem problem, int from, int to)
        {
            int min = 0, max = 0;
            for (int i = 1; i < 4; i++) { if (problem.values[i] < problem.values[min]) min = i; if (problem.values[i] > problem.values[max]) max = i; }
            if (from == max && to == min) return MisAlwaysEven;
            if (problem.band == 3) return MisVarianceSigma;
            int result = VarianceAfter(problem.values, from, to);
            if (result == 0) return MisDeviationAverage;
            return MisUnplanned;
        }

        public static string MisconceptionFeedback(string id)
        {
            switch (id)
            {
                case MisAlwaysEven: return "항상 큰 집에서 작은 집으로 옮기면 분산이 줄어듭니다. 이번 목표는 정확한 값입니다.";
                case MisVarianceSigma: return "표준편차를 제곱해 목표 분산을 먼저 확인하시오.";
                case MisDeviationAverage: return "편차의 합은 0이지만 분산은 편차 제곱의 평균입니다.";
                default: return "바뀌는 두 편차의 제곱만 다시 비교하시오.";
            }
        }

        public static string SqrtForm(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == 0) return "0";
            int factor = 1;
            for (int s = 1; s * s <= value; s++) if (value % (s * s) == 0) factor = s;
            int radicand = value / (factor * factor);
            if (radicand == 1) return factor.ToString();
            return (factor == 1 ? "" : factor.ToString()) + "√" + radicand;
        }

        public static string Join(int[] values)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < values.Length; i++) { if (i > 0) sb.Append(','); sb.Append(values[i]); }
            return sb.Append(']').ToString();
        }

        public static string HouseName(int index) => Houses[Math.Max(0, Math.Min(3, index))];

        static string LedgerName(string id)
        {
            uint h = 2166136261u;
            for (int i = 0; i < id.Length; i++) { h ^= id[i]; h *= 16777619u; }
            int a = (int)(h % (uint)LedgerA.Length);
            int b = (int)((h / (uint)LedgerA.Length) % (uint)LedgerB.Length);
            return LedgerA[a] + " " + LedgerB[b];
        }

        public static void MoveAt(int move, out int from, out int to)
        {
            move = PositiveMod(move, 12);
            from = move / 3;
            int r = move % 3;
            to = r >= from ? r + 1 : r;
        }

        static void ValidateBank(List<TransferProblem> bank)
        {
            for (int n = 0; n < bank.Count; n++)
            {
                var p = bank[n];
                if (p.values[p.from] < 2) throw new InvalidOperationException("출발 집 주민 부족: " + p.id);
                var seen = new HashSet<int>();
                for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) if (i != j)
                {
                    int v = VarianceAfter(p.values, i, j);
                    if (!seen.Add(v)) throw new InvalidOperationException("12방향 결과 분산 중복: " + p.id);
                }
                if (seen.Count != 12 || !seen.Contains(p.targetVariance) || !IsCorrect(p, p.from, p.to))
                    throw new InvalidOperationException("문제 검증 실패: " + p.id);
            }
        }

        static List<int[]> BuildPermutations()
        {
            var list = new List<int[]>(24);
            for (int a = 0; a < 4; a++) for (int b = 0; b < 4; b++) if (b != a)
            for (int c = 0; c < 4; c++) if (c != a && c != b)
            for (int d = 0; d < 4; d++) if (d != a && d != b && d != c) list.Add(new[] { a, b, c, d });
            return list;
        }

        static int[] Permute(int[] source, int[] order)
            => new[] { source[order[0]], source[order[1]], source[order[2]], source[order[3]] };

        static int PositiveMod(int value, int mod) { int r = value % mod; return r < 0 ? r + mod : r; }
        static uint Hash(uint x) { x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16; return x; }
    }
}
