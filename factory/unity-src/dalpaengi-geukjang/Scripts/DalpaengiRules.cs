// 달팽이 극장 — 중학교 2학년 경우의 수 정수 생성기와 판정 정본.
//
// curriculum/2022-middle-math.json의 표현 함정을 코드 계약으로 옮긴다.
// - 합의 법칙: 서로 겹치지 않는 두 목록 중 하나만 고른다. 답은 m+n.
// - 곱의 법칙: 두 목록에서 각각 하나씩 고르며 모든 조합이 가능하다. 답은 m*n.
// - 역문제: 전체 T와 알려진 부분 m에서 나머지 T-m을 구한다.
// - 확률, 포함배제, 순열·조합 기호, 근호는 만들지 않는다.
// - 발문은 중학교 교과서 관습인 「경우의 수를 구하시오」로 통일한다.
// - 생성·판정·오개념 진단은 모두 int만 사용한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.DalpaengiGeukjang
{
    public enum ShowKind { Sum, Product, Reverse }

    [Serializable]
    public sealed class ShowProblem
    {
        public string id = "";
        public string prompt = "";
        public string concept = "";
        public string formula = "";
        public string leftLabel = "";
        public string rightLabel = "";
        public ShowKind kind;
        public int band;
        public int m;
        public int n;
        public int total;
        public int answer;

        // 화면 선택지가 아니다. 학생이 실제로 펼친 좌석 수가 이 값일 때만
        // 해당 오개념을 역산해 설명한다.
        public int misconceptionA;
        public int misconceptionB;
        public string misconceptionAId = "";
        public string misconceptionBId = "";

        public MgfProblem ToMgf()
        {
            return new MgfProblem {
                id = id,
                prompt = prompt,
                choices = null,
                answer = answer.ToString(),
                answerNumeric = answer,
                unitConcept = concept
            };
        }
    }

    public static class DalpaengiRules
    {
        public const int MinSeats = 2;
        public const int MaxSeats = 17;
        public const int Goal = 8;
        public const int StartLives = 3;
        public const int RequiredFirstCorrect = 6;
        public const float SessionSeconds = 120f;

        static readonly string[] Solo = {
            "현악 독주곡", "피아노 독주곡", "기타 독주곡", "플루트 독주곡",
            "첼로 독주곡", "클라리넷 독주곡", "마림바 독주곡", "바이올린 독주곡",
            "오보에 독주곡", "하프 독주곡", "트럼펫 독주곡", "바순 독주곡"
        };
        static readonly string[] Ensemble = {
            "현악 합주곡", "관악 합주곡", "타악 합주곡", "재즈 합주곡",
            "실내 합주곡", "금관 합주곡", "목관 합주곡", "리듬 합주곡",
            "교향 합주곡", "현대 합주곡", "소규모 합주곡", "축제 합주곡"
        };
        static readonly string[] Song = {
            "서곡", "왈츠", "소나타", "랩소디", "행진곡", "녹턴", "판타지아", "세레나데",
            "협주곡", "카프리치오", "모음곡", "즉흥곡", "전주곡", "변주곡", "발라드", "간주곡"
        };
        static readonly string[] Backdrop = {
            "달빛 숲", "별빛 호수", "장미 정원", "노을 언덕", "유리 온실", "종이 도시",
            "겨울 역", "구름 계단", "푸른 동굴", "비 오는 거리", "해변 극장", "석양 광장",
            "오래된 서재", "은빛 항구", "바람 초원", "눈꽃 마을", "새벽 골목"
        };

        public static ShowProblem SumPractice()
        {
            return new ShowProblem {
                id = "dg-practice-sum", kind = ShowKind.Sum, band = 0,
                m = 2, n = 3, total = 5, answer = 5,
                leftLabel = "독주곡", rightLabel = "합주곡",
                concept = "두 사건이 동시에 일어나지 않을 때의 합의 법칙",
                prompt = "연습. 독주곡 2가지 또는 합주곡 3가지 중 한 곡만 고른다. 두 목록에 같은 곡은 없다. 경우의 수를 구하시오.",
                formula = "2 + 3 = 5",
                misconceptionA = 6, misconceptionAId = "sum_used_product",
                misconceptionB = 3, misconceptionBId = "one_group_only"
            };
        }

        public static ShowProblem ProductPractice()
        {
            return new ShowProblem {
                id = "dg-practice-product", kind = ShowKind.Product, band = 0,
                m = 2, n = 2, total = 4, answer = 4,
                leftLabel = "곡", rightLabel = "배경",
                concept = "곱의 법칙",
                prompt = "연습. 곡 2가지와 무대 배경 2가지 중 각각 하나씩 고른다. 모든 조합이 가능하다. 경우의 수를 구하시오.",
                formula = "2 × 2 = 4",
                // 2+2는 우연히 정답이므로 오개념으로 단정하지 않는다.
                misconceptionA = 2, misconceptionAId = "one_group_only",
                misconceptionB = 3, misconceptionBId = "one_combination_missing"
            };
        }

        // 본판은 유형 순서를 고정하되, 각 문항의 정답 y를 2..17에서 먼저 독립 균등 추출한다.
        // 그 뒤 y를 만족하는 정수쌍 풀에서 직접 고르므로 사후 필터·재시도 루프가 없다.
        public static List<ShowProblem> RunDeck(int serial)
        {
            var deck = new List<ShowProblem>(Goal);
            var rng = new Random(unchecked(0x4D534E47 + serial * 7919));
            for (int i = 0; i < Goal; i++)
            {
                int y = rng.Next(MinSeats, MaxSeats + 1);
                int variant = rng.Next(0, 1000000);
                if (i < 3) deck.Add(MakeSum("dg-run-" + serial + "-" + (i + 1), y, variant, 1));
                else if (i < 6) deck.Add(MakeProduct("dg-run-" + serial + "-" + (i + 1), y, variant, 2));
                else deck.Add(MakeReverse("dg-run-" + serial + "-" + (i + 1), y, 1 + rng.Next(12), variant, 3));
            }
            return deck;
        }

        public static ShowProblem MakeSum(string id, int y, int variant, int band)
        {
            var pairs = SumPairs(y);
            var pair = pairs[Positive(variant) % pairs.Count];
            int m = pair[0], n = pair[1];
            string left = Solo[Positive(variant * 3 + y) % Solo.Length];
            string right = Ensemble[Positive(variant * 7 + y + 1) % Ensemble.Length];
            int multiply = m * n;
            int a = multiply >= MinSeats && multiply <= MaxSeats && multiply != y ? multiply : Math.Max(m, n);
            int b = Math.Min(m, n);
            if (b == a || b == y || b < MinSeats) b = Math.Abs(m - n);
            if (b == a || b == y || b < MinSeats) b = y == MinSeats ? MinSeats + 1 : y - 1;
            return new ShowProblem {
                id = id, kind = ShowKind.Sum, band = band, m = m, n = n, total = y, answer = y,
                leftLabel = left, rightLabel = right,
                concept = "두 사건이 동시에 일어나지 않을 때의 합의 법칙",
                prompt = left + " " + m + "가지 또는 " + right + " " + n +
                    "가지 중 한 곡만 고른다. 두 목록에 같은 곡은 없다. 경우의 수를 구하시오.",
                formula = m + " + " + n + " = " + y,
                misconceptionA = a, misconceptionAId = multiply == a ? "sum_used_product" : "one_group_only",
                misconceptionB = b, misconceptionBId = "one_group_only"
            };
        }

        public static ShowProblem MakeProduct(string id, int y, int variant, int band)
        {
            var pairs = ProductPairs(y);
            var pair = pairs[Positive(variant) % pairs.Count];
            int m = pair[0], n = pair[1];
            string left = Song[Positive(variant * 5 + y) % Song.Length];
            string right = Backdrop[Positive(variant * 11 + y + 2) % Backdrop.Length];
            int sum = m + n;
            int a = sum != y && sum >= MinSeats && sum <= MaxSeats ? sum : Math.Min(m, n);
            if (a == y || a < MinSeats) a = y == MinSeats ? MinSeats + 1 : y - 1;
            int b = Math.Max(m, n);
            if (b == y || b == a || b < MinSeats) b = y > MinSeats ? y - 1 : MinSeats + 1;
            return new ShowProblem {
                id = id, kind = ShowKind.Product, band = band, m = m, n = n, total = y, answer = y,
                leftLabel = left, rightLabel = right,
                concept = "곱의 법칙",
                prompt = left + " " + m + "가지와 " + right + " " + n +
                    "가지 중 각각 하나씩 고른다. 모든 조합이 가능하다. 경우의 수를 구하시오.",
                formula = m + " × " + n + " = " + y,
                misconceptionA = a, misconceptionAId = sum == a ? "product_used_sum" : "one_group_only",
                misconceptionB = b, misconceptionBId = "one_group_only"
            };
        }

        public static ShowProblem MakeReverse(string id, int y, int known, int variant, int band)
        {
            int total = y + known;
            string knownName = Solo[Positive(variant + known) % Solo.Length];
            string targetName = Ensemble[Positive(variant * 5 + y) % Ensemble.Length];
            int whole = total >= MinSeats && total <= MaxSeats ? total : 0;
            int add = total + known;
            if (add < MinSeats || add > MaxSeats) add = 0;
            return new ShowProblem {
                id = id, kind = ShowKind.Reverse, band = band, m = known, n = y, total = total, answer = y,
                leftLabel = knownName, rightLabel = targetName,
                concept = "전체와 부분으로 경우의 수 거꾸로 구하기",
                prompt = knownName + " 또는 " + targetName + " 중 한 곡을 고르는 경우는 모두 " + total +
                    "가지이다. " + knownName + "은 " + known + "가지이고 두 목록에 같은 곡은 없다. " +
                    targetName + "을 고르는 경우의 수를 구하시오.",
                formula = total + " − " + known + " = " + y,
                misconceptionA = whole, misconceptionAId = "reverse_used_total",
                misconceptionB = add, misconceptionBId = "reverse_added_part"
            };
        }

        public static string Diagnose(ShowProblem p, int submitted)
        {
            if (p == null) return "integer_miss";
            if (p.misconceptionA > 0 && submitted == p.misconceptionA) return p.misconceptionAId;
            if (p.misconceptionB > 0 && submitted == p.misconceptionB) return p.misconceptionBId;
            return submitted < p.answer ? "cases_missing" : "cases_overcounted";
        }

        public static List<MgfProblem> BuildBank()
        {
            var result = new List<MgfProblem>(400);
            int serial = 0;
            for (int y = MinSeats; y <= MaxSeats; y++)
            {
                var sums = SumPairs(y);
                for (int i = 0; i < sums.Count; i++)
                    result.Add(MakeSum("dg-bank-s-" + (++serial), y, i * 101 + y * 17, 1).ToMgf());
            }
            for (int y = MinSeats; y <= MaxSeats; y++)
            {
                var products = ProductPairs(y);
                for (int i = 0; i < products.Count; i++)
                    result.Add(MakeProduct("dg-bank-p-" + (++serial), y, i * 137 + y * 19, 2).ToMgf());
            }
            for (int y = MinSeats; y <= MaxSeats; y++)
            for (int known = 1; known <= 12; known++)
                result.Add(MakeReverse("dg-bank-r-" + (++serial), y, known, y * 211 + known * 29, 3).ToMgf());
            return result;
        }

        public static bool ValidateAll(out string error)
        {
            var practiceA = SumPractice();
            var practiceB = ProductPractice();
            if (!ValidateProblem(practiceA, out error)) return false;
            if (!ValidateProblem(practiceB, out error)) return false;

            // 전체 파라미터 풀 × 오개념 조합 전수 검사. 우연히 정답인 오답은 0건이어야 한다.
            for (int y = MinSeats; y <= MaxSeats; y++)
            {
                var sums = SumPairs(y);
                if (sums.Count == 0) { error = "sum pool empty " + y; return false; }
                for (int i = 0; i < sums.Count; i++)
                    if (!ValidateProblem(MakeSum("vs" + y + "-" + i, y, i, 1), out error)) return false;

                var products = ProductPairs(y);
                if (products.Count == 0) { error = "product pool empty " + y; return false; }
                for (int i = 0; i < products.Count; i++)
                    if (!ValidateProblem(MakeProduct("vp" + y + "-" + i, y, i, 2), out error)) return false;

                for (int known = 1; known <= 12; known++)
                    if (!ValidateProblem(MakeReverse("vr" + y + "-" + known, y, known, known, 3), out error)) return false;
            }

            for (int run = 1; run <= 512; run++)
            {
                var deck = RunDeck(run);
                if (deck.Count != Goal) { error = "deck count"; return false; }
                for (int i = 0; i < deck.Count; i++)
                {
                    if ((i < 3 && deck[i].kind != ShowKind.Sum) ||
                        (i >= 3 && i < 6 && deck[i].kind != ShowKind.Product) ||
                        (i >= 6 && deck[i].kind != ShowKind.Reverse))
                    { error = "band order"; return false; }
                    if (!ValidateProblem(deck[i], out error)) return false;
                }
            }

            var bank = BuildBank();
            if (bank.Count < 300) { error = "bank too small " + bank.Count; return false; }
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < bank.Count; i++)
            {
                var p = bank[i];
                if (!ids.Add(p.id)) { error = "duplicate id " + p.id; return false; }
                if (!prompts.Add(p.prompt)) { error = "duplicate prompt " + p.prompt; return false; }
                if (p.choices != null && p.choices.Length > 0) { error = "choices must be null"; return false; }
                if (p.answer != ((int)p.answerNumeric).ToString()) { error = "export mismatch"; return false; }
            }
            error = "";
            return true;
        }

        static bool ValidateProblem(ShowProblem p, out string error)
        {
            if (p == null) { error = "null problem"; return false; }
            int expected = p.kind == ShowKind.Sum ? p.m + p.n :
                p.kind == ShowKind.Product ? p.m * p.n : p.total - p.m;
            if (p.answer != expected || p.answer < MinSeats || p.answer > MaxSeats)
            { error = "answer invariant " + p.id; return false; }
            if (p.kind == ShowKind.Sum && p.prompt.IndexOf("같은 곡은 없다", StringComparison.Ordinal) < 0)
            { error = "sum disjoint phrase " + p.id; return false; }
            if (p.kind == ShowKind.Product &&
                (p.prompt.IndexOf("각각 하나씩", StringComparison.Ordinal) < 0 ||
                 p.prompt.IndexOf("모든 조합이 가능하다", StringComparison.Ordinal) < 0))
            { error = "product condition phrase " + p.id; return false; }
            if (p.kind == ShowKind.Reverse && p.total != p.answer + p.m)
            { error = "reverse total " + p.id; return false; }
            if (p.misconceptionA == p.answer || p.misconceptionB == p.answer)
            { error = "misconception accidentally true " + p.id; return false; }
            error = "";
            return true;
        }

        static List<int[]> SumPairs(int y)
        {
            var pool = new List<int[]>();
            for (int m = 1; m < y; m++) pool.Add(new[] { m, y - m });
            return pool;
        }

        static List<int[]> ProductPairs(int y)
        {
            var nonTrivial = new List<int[]>();
            var fallback = new List<int[]>();
            for (int m = 1; m <= y; m++)
            {
                if (y % m != 0) continue;
                int n = y / m;
                fallback.Add(new[] { m, n });
                if (m > 1 && n > 1) nonTrivial.Add(new[] { m, n });
            }
            return nonTrivial.Count > 0 ? nonTrivial : fallback;
        }

        static int Positive(int n) { return n == int.MinValue ? 0 : Math.Abs(n); }
    }
}
