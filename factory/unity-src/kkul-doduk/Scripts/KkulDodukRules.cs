// 꿀 도둑 — 중학교 2학년 경우의 수 정수 문제 정본.
//
// curriculum/2022-middle-math.json / chosen.json 표현 계약:
// - 합의 법칙은 두 군에 같은 꿀이 없고, 한 시식에서 두 군 중 한 종류만 고를 때만 쓴다.
// - 곱의 법칙은 역할이 다른 두 선반에서 각각 하나씩 고르는 순서쌍을 센다.
// - 확률, 포함배제, 순열·조합 공식, 대표 선출로 범위를 넓히지 않는다.
// - 발문은 중학교 지시형 「경우의 수를 구하시오」로 통일한다.
// - 모든 계산과 판정은 1~24 정수이며 부동소수점 비교를 하지 않는다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.KkulDoduk
{
    public enum HoneyKind { Sum, Product, Repair }

    [Serializable]
    public sealed class HoneyProblem
    {
        public string id = "";
        public string prompt = "";
        public string concept = "";
        public string formula = "";
        public string leftName = "";
        public string rightName = "";
        public HoneyKind kind;
        public int band;
        public int m;
        public int n;
        public int answer;
        public int precut;
        // 오개념 역산값. 화면 선택지가 아니라 실제 절단값을 진단하는 정수들이다.
        public int distractorA;
        public int distractorB;
        public int distractorC;

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

    public static class KkulDodukRules
    {
        public const int Goal = 8;
        public const int RequiredFirstCorrect = 6;
        public const int StartLives = 3;
        public const float SessionSeconds = 120f;

        public static readonly int[] SumTargets = { 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        public static readonly int[] ProductTargets = { 6, 8, 9, 10, 12, 14, 15, 16, 18, 20, 21, 24 };

        static readonly string[] FlowerNames = {
            "아카시아꿀", "밤꿀", "유채꿀", "메밀꿀", "라벤더꿀", "감귤꽃꿀",
            "클로버꿀", "해바라기꿀", "로즈메리꿀", "사과꽃꿀", "산딸기꿀", "백리향꿀"
        };
        static readonly string[] FruitNames = {
            "살구꿀", "복숭아꿀", "자두꿀", "배꽃꿀", "체리꿀", "레몬꿀",
            "무화과꿀", "석류꿀", "블루베리꿀", "대추꿀", "멜론꿀", "포도꿀"
        };
        static readonly string[] VesselNames = {
            "밀랍잔", "도토리잔", "꽃잎잔", "유리병", "작은 항아리", "나무숟가락",
            "찻잔", "은방울", "열매접시", "향기병", "꿀주걱", "시식종"
        };

        public static HoneyProblem Practice()
        {
            var p = MakeSum("kd-practice", 5, 0, 1, 0);
            p.m = 2; p.n = 3; p.answer = 5; p.precut = 1;
            p.leftName = "꽃꿀"; p.rightName = "과일꿀";
            p.prompt = "연습. 꽃꿀 2종 또는 과일꿀 3종 중 한 종류만 고른다. 두 군에 같은 꿀은 없다. 경우의 수를 구하시오.";
            p.formula = "2 + 3 = 5";
            p.distractorA = 6; p.distractorB = 3; p.distractorC = 1;
            return p;
        }

        // 목표값을 먼저 고르고, 그 목표를 만드는 제약 만족 (m,n) 풀에서 직접 고른다.
        // 무작위 생성 뒤 필터링하거나 재시도하는 루프는 없다.
        public static HoneyProblem MakeSum(string id, int target, int variant, int band, int salt)
        {
            var pairs = SumPairs(target, band >= 3 ? 12 : 6);
            var pair = pairs[Positive(variant * 7 + salt * 3) % pairs.Count];
            int m = pair[0], n = pair[1];
            string left = FlowerNames[Positive(variant + salt) % FlowerNames.Length];
            string right = FruitNames[Positive(variant * 5 + salt + 3) % FruitNames.Length];
            string ticket = Ticket(id);
            return new HoneyProblem {
                id = id, kind = HoneyKind.Sum, band = Math.Max(1, band), m = m, n = n,
                answer = m + n, precut = InitialWrong(m + n, m * n, variant),
                leftName = left, rightName = right,
                concept = "두 사건이 동시에 일어나지 않을 때의 합의 법칙",
                prompt = ticket + " " + left + " " + m + "종과 " + right + " " + n +
                    "종 중 한 종류만 고른다. 두 군에 같은 꿀은 없다. 경우의 수를 구하시오.",
                formula = m + " + " + n + " = " + (m + n),
                distractorA = m * n,                    // 곱을 적용함
                distractorB = Math.Max(m, n),           // 한 선반만 셈
                distractorC = Math.Abs(m - n)            // 두 군의 차만 셈
            };
        }

        public static HoneyProblem MakeProduct(string id, int target, int variant, int band, bool repair, int salt)
        {
            var pairs = ProductPairs(target);
            var pair = pairs[Positive(variant * 5 + salt * 7) % pairs.Count];
            int m = pair[0], n = pair[1];
            string left = FlowerNames[Positive(variant * 3 + salt) % FlowerNames.Length];
            string right = VesselNames[Positive(variant * 7 + salt + 2) % VesselNames.Length];
            string ticket = Ticket(id);
            string condition = left + " " + m + "종과 " + right + " " + n + "종 중에서 각각 하나씩 고른다.";
            return new HoneyProblem {
                id = id, kind = repair ? HoneyKind.Repair : HoneyKind.Product, band = Math.Max(2, band),
                m = m, n = n, answer = m * n, precut = m + n,
                leftName = left, rightName = right,
                concept = repair ? "곱의 법칙 오류 찾기" : "곱의 법칙",
                prompt = repair
                    ? ticket + " " + condition + " 벌이 벌집을 " + (m + n) + "칸으로 잘못 나누었다. 필요한 경우의 수를 구하고 벌집을 고치시오."
                    : ticket + " " + condition + " 경우의 수를 구하시오.",
                formula = m + " × " + n + " = " + (m * n),
                distractorA = m + n,                    // 더함
                distractorB = m,                        // 한 꿀에 대한 용기만 셈
                distractorC = n                         // 한 용기에 대한 꿀만 셈
            };
        }

        public static List<HoneyProblem> RunDeck(int serial)
        {
            int s = Positive(serial);
            var deck = new List<HoneyProblem>(Goal);
            int sumA = SumTargets[s % SumTargets.Length];
            int sumB = SumTargets[(s + 4) % SumTargets.Length];
            deck.Add(MakeSum("kd-run-1-" + serial, sumA, s + 1, 1, 11));
            deck.Add(MakeSum("kd-run-2-" + serial, sumB, s + 2, 1, 17));
            deck.Add(MakeProduct("kd-run-3-" + serial, ProductTargets[(s + 1) % 12], s + 3, 2, false, 23));
            deck.Add(MakeProduct("kd-run-4-" + serial, ProductTargets[(s + 5) % 12], s + 4, 2, false, 29));
            deck.Add(MakeProduct("kd-run-5-" + serial, ProductTargets[(s + 9) % 12], s + 5, 2, false, 31));
            if ((s & 1) == 0)
                deck.Add(MakeSum("kd-run-6-" + serial, SumTargets[(s + 7) % 9], s + 6, 3, 37));
            else
                deck.Add(MakeProduct("kd-run-6-" + serial, ProductTargets[(s + 7) % 12], s + 6, 3, false, 37));
            deck.Add(MakeProduct("kd-run-7-" + serial, ProductTargets[(s + 3) % 12], s + 7, 3, true, 41));
            deck.Add(MakeProduct("kd-run-8-" + serial, ProductTargets[(s + 11) % 12], s + 8, 3, true, 43));
            return deck;
        }

        public static int InitialSelection(HoneyProblem p, int problemIndex)
        {
            if (p == null) return 1;
            if (p.kind == HoneyKind.Repair) return p.precut; // 반드시 m+n이며 정답과 다르다.
            int v = 1 + ((p.answer + 6 + problemIndex * 5) % 24);
            if (v == p.answer) v = v == 24 ? 1 : v + 1;
            return v;
        }

        public static string Diagnose(HoneyProblem p, int submitted)
        {
            if (p == null) return "integer_miss";
            if (p.kind == HoneyKind.Sum)
            {
                // 4+1처럼 m×n과 큰 군의 수가 우연히 같은 경우, 관찰 가능한 누락인
                // '한 군만 셈'을 우선해 모순 없는 합의 법칙 피드백을 준다.
                if (submitted == p.distractorB) return "one_group_only";
                if (submitted == p.distractorA) return "sum_used_product";
                if (submitted == p.distractorC) return "difference_only";
            }
            else
            {
                if (submitted == p.distractorA) return "product_used_sum";
                if (submitted == p.distractorB || submitted == p.distractorC) return "one_group_only";
                if (p.m == p.n && submitted == p.m * (p.m + 1) / 2) return "roles_merged";
            }
            return "integer_miss";
        }

        public static List<MgfProblem> BuildBank()
        {
            var source = BuildHoneyBank();
            var bank = new List<MgfProblem>(source.Count);
            for (int i = 0; i < source.Count; i++) bank.Add(source[i].ToMgf());
            return bank;
        }

        static List<HoneyProblem> BuildHoneyBank()
        {
            var bank = new List<HoneyProblem>(900);
            for (int i = 0; i < 300; i++)
                bank.Add(MakeSum("kd-bank-s-" + (i + 1), SumTargets[i % 9], i, i < 180 ? 1 : 3, 101 + i));
            for (int i = 0; i < 420; i++)
                bank.Add(MakeProduct("kd-bank-p-" + (i + 1), ProductTargets[i % 12], i, i < 260 ? 2 : 3, false, 503 + i));
            for (int i = 0; i < 180; i++)
                bank.Add(MakeProduct("kd-bank-r-" + (i + 1), ProductTargets[(i * 5) % 12], i, 3, true, 907 + i));
            return bank;
        }

        public static bool ValidateAll(out string error)
        {
            for (int k = 0; k < SumTargets.Length; k++)
            {
                // 실제 RunDeck 3단계와 BuildBank가 쓰는 최대 범위까지 같은 풀을 검증한다.
                var pairs = SumPairs(SumTargets[k], 12);
                if (pairs.Count == 0) { error = "sum target pool " + SumTargets[k]; return false; }
                for (int i = 0; i < pairs.Count; i++)
                {
                    int m = pairs[i][0], n = pairs[i][1], a = m + n;
                    if (m * n == a || a < 1 || a > 24) { error = "sum accidental truth"; return false; }
                    if (m * n == a || Math.Max(m, n) == a || Math.Abs(m - n) == a)
                    { error = "sum distractor equals answer"; return false; }
                }
            }
            for (int k = 0; k < ProductTargets.Length; k++)
            {
                var pairs = ProductPairs(ProductTargets[k]);
                if (pairs.Count == 0) { error = "product target pool " + ProductTargets[k]; return false; }
                for (int i = 0; i < pairs.Count; i++)
                {
                    int m = pairs[i][0], n = pairs[i][1], a = m * n;
                    if (m + n == a || m == a || n == a || a < 1 || a > 24)
                    { error = "product distractor equals answer"; return false; }
                    if (m == n && m * (m + 1) / 2 == a)
                    { error = "roles-merged accidental truth"; return false; }
                }
            }
            var practice = Practice();
            if (!ValidateProblem(practice, out error)) { error = "practice: " + error; return false; }
            // 실전 덱의 모든 선택 인덱스와 band 3 합 문제를 실제 생성 경로로 검증한다.
            for (int serial = 1; serial <= 72; serial++)
            {
                var deck = RunDeck(serial);
                if (deck.Count != Goal) { error = "run deck count " + serial; return false; }
                for (int i = 0; i < deck.Count; i++)
                    if (!ValidateProblem(deck[i], out error))
                    { error = "run deck " + serial + "/" + i + ": " + error; return false; }
            }

            var bank = BuildHoneyBank();
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < bank.Count; i++)
            {
                var p = bank[i];
                if (!ValidateProblem(p, out error)) { error = "bank " + p.id + ": " + error; return false; }
                if (!ids.Add(p.id)) { error = "duplicate id " + p.id; return false; }
                if (!prompts.Add(p.prompt)) { error = "duplicate prompt " + p.id; return false; }
                var exported = p.ToMgf();
                if (exported.choices != null && exported.choices.Length != 0) { error = "choices not null"; return false; }
                if (exported.answer != ((int)exported.answerNumeric).ToString()) { error = "answer mismatch"; return false; }
            }
            error = "";
            return true;
        }

        static bool ValidateProblem(HoneyProblem p, out string error)
        {
            if (p == null) { error = "null problem"; return false; }
            int calculated = p.kind == HoneyKind.Sum ? p.m + p.n : p.m * p.n;
            if (p.m < 1 || p.n < 1 || calculated < 1 || calculated > 24 || p.answer != calculated)
            { error = "answer calculation"; return false; }
            string expectedFormula = p.kind == HoneyKind.Sum
                ? p.m + " + " + p.n + " = " + calculated
                : p.m + " × " + p.n + " = " + calculated;
            if (p.formula != expectedFormula) { error = "formula mismatch"; return false; }
            if (p.prompt.IndexOf(p.leftName + " " + p.m + "종", StringComparison.Ordinal) < 0 ||
                p.prompt.IndexOf(p.rightName + " " + p.n + "종", StringComparison.Ordinal) < 0)
            { error = "prompt operands"; return false; }
            if (p.kind == HoneyKind.Sum &&
                p.prompt.IndexOf("두 군에 같은 꿀은 없다", StringComparison.Ordinal) < 0)
            { error = "sum disjoint condition"; return false; }
            if (p.kind != HoneyKind.Sum &&
                p.prompt.IndexOf("각각 하나씩", StringComparison.Ordinal) < 0)
            { error = "product pair condition"; return false; }
            if (p.distractorA == p.answer || p.distractorB == p.answer || p.distractorC == p.answer)
            { error = "distractor equals answer"; return false; }
            error = "";
            return true;
        }

        static List<int[]> SumPairs(int target, int max)
        {
            var pool = new List<int[]>();
            for (int m = 1; m <= max; m++)
            for (int n = 1; n <= max; n++)
                if (m + n == target && m * n != target) pool.Add(new[] { m, n });
            return pool;
        }

        static List<int[]> ProductPairs(int target)
        {
            var pool = new List<int[]>();
            for (int m = 2; m <= 8; m++)
            for (int n = 2; n <= 8; n++)
                if (m * n == target && m + n != target) pool.Add(new[] { m, n });
            return pool;
        }

        static int InitialWrong(int answer, int alternate, int salt)
        {
            int v = alternate >= 1 && alternate <= 24 && alternate != answer ? alternate : 1 + Positive(answer + salt * 5) % 24;
            if (v == answer) v = v == 24 ? 1 : v + 1;
            return v;
        }

        static string Ticket(string id)
        {
            int h = 17;
            for (int i = 0; i < id.Length; i++) h = unchecked(h * 31 + id[i]);
            return "밀랍방 " + (1 + Positive(h) % 997).ToString("000") + "번 의뢰.";
        }

        static int Positive(int n) { return n == int.MinValue ? 0 : Math.Abs(n); }
    }
}
