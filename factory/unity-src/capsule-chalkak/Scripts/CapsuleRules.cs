// 캡슐 찰칵 — 중2 확률 정수 표본공간 생성기와 정확 집합 판정.
//
// curriculum/2022-middle-math.json style_guide / expression_traps 반영:
// - 값 요구 발문은 중학교 지시형 「확률을 구하시오」로 통일한다.
// - 공 뽑기는 「임의로」와 「(단, 공의 모양과 크기는 모두 같다.)」를 함께 쓴다.
// - 복수 도구는 「서로 다른」으로 순서쌍을 구별한다.
// - 동시에 일어나는 사건은 「서로 영향을 끼치지 않는다」를 명시한다.
// - 답은 정수쌍 Fraction으로 약분한 0·1 또는 기약분수이며 float/double로 판정하지 않는다.
// - 중2 범위 밖인 비복원·조건부 확률·순열/조합 공식·근호를 생성하지 않는다.
//
// 오개념 역산 id (모든 후보는 정답 제거→0..1 범위 검사→중복 제거 뒤 사용):
// - equiprobability_half: 결과를 사건/여사건 두 종류로만 보고 무조건 1/2이라고 생각.
// - favorable_count_only: 유리한 경우의 수 a를 확률 자체로 답해 분모를 빠뜨림.
// - add_independent_events: 서로 영향을 끼치지 않는 동시 사건에 덧셈을 적용.
// - reverse_fraction: 사건/전체를 전체/사건으로 뒤집음.
// - complement_confusion: 사건과 여사건의 확률을 바꿈.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.CapsuleChalkak
{
    public enum CapsuleKind { Practice, Bag, Certain, DiceSum, ThreeCoins, CoinDie }

    [Serializable]
    public struct Fraction : IEquatable<Fraction>
    {
        public int n;
        public int d;

        public Fraction(int numerator, int denominator)
        {
            if (denominator == 0) throw new ArgumentException("분모는 0일 수 없다.");
            if (denominator < 0) { numerator = -numerator; denominator = -denominator; }
            int g = Gcd(Math.Abs(numerator), denominator);
            n = numerator / g;
            d = denominator / g;
        }

        static int Gcd(int a, int b)
        {
            if (a == 0) return b == 0 ? 1 : b;
            while (b != 0) { int t = a % b; a = b; b = t; }
            return Math.Max(1, a);
        }

        public bool Equals(Fraction other) => n == other.n && d == other.d;
        public override bool Equals(object obj) => obj is Fraction f && Equals(f);
        public override int GetHashCode() => n * 397 ^ d;
        public override string ToString()
        {
            if (n == 0) return "0";
            if (d == 1) return n.ToString();
            return n + "/" + d;
        }
        public double Numeric => n / (double)d;
    }

    [Serializable]
    public sealed class MisconceptionChoice
    {
        public string value;
        public string misconceptionId;
        public Fraction fraction;
    }

    [Serializable]
    public sealed class CapsuleProblem
    {
        public string id;
        public CapsuleKind kind;
        public int band;
        public string prompt;
        public string shortCondition;
        public string reveal;
        public string unitConcept;
        public string[] labels;
        public ulong answerMask;
        public int answerCount;
        public Fraction answer;
        public bool allowComplement;
        public MisconceptionChoice[] distractors;
        public string[] choices;
    }

    public static class CapsuleRules
    {
        public const int TargetOrders = 6;
        public const int StartLives = 3;
        public const int RequiredFirstCorrect = 4;
        public const float RunSeconds = 105f;
        // 짧은 표본 타일은 색 이름(빨강/파랑/하양/검정), 발문은 자연스러운
        // 관형형(빨간/파란/흰/검은)을 쓴다. 둘을 한 문자열로 재사용하면
        // 「빨강 공」처럼 교과서 예문과 동떨어진 표현이 된다.
        static readonly string[] Colors = { "빨강", "파랑", "하양", "검정" };

        public static int CountBits(ulong mask)
        {
            int count = 0;
            while (mask != 0) { count += (int)(mask & 1UL); mask >>= 1; }
            return count;
        }

        public static ulong FullMask(int count) => count >= 64 ? ulong.MaxValue : count <= 0 ? 0UL : (1UL << count) - 1UL;

        public static bool IsExact(CapsuleProblem p, ulong selectedMask, bool complementMode)
        {
            ulong required = complementMode && p.allowComplement
                ? FullMask(p.labels.Length) & ~p.answerMask
                : p.answerMask;
            return selectedMask == required;
        }

        public static CapsuleProblem Practice()
        {
            return FinalizeProblem(new CapsuleProblem {
                id = "cc-practice", kind = CapsuleKind.Practice, band = 0,
                prompt = "빨간 공 2개와 파란 공 2개 중 파란 공이 나오는 경우를 모두 엮으시오.",
                shortCondition = "파란 공을 모두 엮으시오",
                labels = new[] { "빨강 ①", "빨강 ②", "파랑 ①", "파랑 ②" },
                answerMask = Bit(2) | Bit(3), unitConcept = "경우의 수의 비율로서의 확률",
                reveal = "파란 공 2개 / 전체 4개 → 2/4 = 1/2"
            });
        }

        public static List<CapsuleProblem> RuntimeDeck(System.Random rng)
        {
            // 난이도 밴드를 처음부터 전체 풀에서 섞지 않고 눈에 보이는 순서로 소진한다.
            // 첫 실전은 반드시 빨강 3·파랑 5가 포함된 유효 은행에서 목표색만 난수화한다.
            bool firstBlue = rng.Next(2) == 0;
            var deck = new List<CapsuleProblem> {
                Bag("run-bag-1", 3, 5, "빨강", "파랑", firstBlue ? 1 : 0, 1),
                Bag("run-bag-2", 4, 4, "하양", "검정", rng.Next(2), 1),
                Certain("run-certain", 6, true, 2),
                DiceSum("run-dice-7", 7, 3),
                DiceSum("run-dice-8", 8, 3),
                CoinPrime("run-coindie", 5)
            };
            // 세 동전 여사건은 동시 시행 주문 대신 매 판 한 번 끼워 넣는다.
            int replace = 4 + rng.Next(2);
            deck[replace] = ThreeCoinsAtLeastOne("run-coins", 4);
            return deck;
        }

        public static List<CapsuleProblem> BuildBank()
        {
            // 무작위 생성 후 필터링하지 않는다. 유한한 제약 만족 풀을 자릿값처럼 역으로 전수 열거한다.
            var bank = new List<CapsuleProblem>(520);
            var ids = new HashSet<string>();
            for (int ca = 0; ca < Colors.Length; ca++)
            for (int cb = ca + 1; cb < Colors.Length; cb++)
            for (int total = 6; total <= 10; total++)
            for (int a = 1; a < total; a++)
            for (int target = 0; target < 2; target++)
            {
                int b = total - a;
                Add(bank, ids, Bag("bag-" + ca + "-" + cb + "-" + a + "-" + b + "-" + target,
                    a, b, Colors[ca], Colors[cb], target, 1));
            }
            for (int count = 4; count <= 10; count++)
            {
                Add(bank, ids, Certain("certain-white-" + count, count, true, 2));
                Add(bank, ids, Certain("impossible-black-" + count, count, false, 2));
            }
            for (int sum = 2; sum <= 12; sum++) Add(bank, ids, DiceSum("dice-sum-" + sum, sum, 3));
            Add(bank, ids, ThreeCoinsAtLeastOne("coins-atleast-one", 4));
            Add(bank, ids, ThreeCoinsAtLeastTwo("coins-atleast-two", 4));
            Add(bank, ids, CoinPrime("coin-die-prime", 5));
            Add(bank, ids, CoinEven("coin-die-even", 5));

            if (bank.Count < 300) throw new InvalidOperationException("문제 은행은 300개 이상이어야 한다: " + bank.Count);
            return bank;
        }

        static void Add(List<CapsuleProblem> list, HashSet<string> ids, CapsuleProblem p)
        {
            if (!ids.Add(p.id)) throw new InvalidOperationException("중복 문제 id: " + p.id);
            list.Add(p);
        }

        static ulong Bit(int index) => 1UL << index;

        static string BallAdjective(string color)
        {
            switch (color)
            {
                case "빨강": return "빨간";
                case "파랑": return "파란";
                case "하양": return "흰";
                case "검정": return "검은";
                default: return color;
            }
        }

        static CapsuleProblem Bag(string id, int a, int b, string colorA, string colorB, int target, int band)
        {
            int total = a + b;
            string targetColor = target == 0 ? colorA : colorB;
            int favorable = target == 0 ? a : b;
            var labels = new string[total];
            ulong mask = 0;
            for (int i = 0; i < total; i++)
            {
                bool isA = i < a;
                string c = isA ? colorA : colorB;
                int serial = isA ? i + 1 : i - a + 1;
                labels[i] = c + " " + Circled(serial);
                if ((target == 0 && isA) || (target == 1 && !isA)) mask |= Bit(i);
            }
            return FinalizeProblem(new CapsuleProblem {
                id = id, kind = CapsuleKind.Bag, band = band, labels = labels, answerMask = mask,
                prompt = BallAdjective(colorA) + " 공 " + a + "개와 " + BallAdjective(colorB) + " 공 " + b + "개가 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, " + BallAdjective(targetColor) + " 공이 나올 확률을 구하시오. (단, 공의 모양과 크기는 모두 같다.)",
                shortCondition = BallAdjective(targetColor) + " 공이 나오는 경우",
                reveal = BallAdjective(targetColor) + " 공 " + favorable + "개 / 전체 " + total + "개",
                unitConcept = "경우의 수의 비율로서의 확률"
            });
        }

        static CapsuleProblem Certain(string id, int count, bool certain, int band)
        {
            var labels = new string[count];
            for (int i = 0; i < count; i++) labels[i] = "하양 " + Circled(i + 1);
            return FinalizeProblem(new CapsuleProblem {
                id = id, kind = CapsuleKind.Certain, band = band, labels = labels,
                answerMask = certain ? FullMask(count) : 0UL,
                prompt = "하얀 공만 " + count + "개 들어 있는 주머니에서 공 한 개를 임의로 꺼낼 때, " + (certain ? "하얀" : "검은") + " 공이 나올 확률을 구하시오. (단, 공의 모양과 크기는 모두 같다.)",
                shortCondition = (certain ? "하얀" : "검은") + " 공이 나오는 경우",
                reveal = certain ? "반드시 일어나는 사건 → 1" : "절대로 일어나지 않는 사건 → 0",
                unitConcept = "확률의 기본 성질"
            });
        }

        static CapsuleProblem DiceSum(string id, int sum, int band)
        {
            var labels = new string[36];
            ulong mask = 0;
            int w = 0;
            for (int a = 1; a <= 6; a++)
            for (int b = 1; b <= 6; b++)
            {
                labels[w] = "(" + a + "," + b + ")";
                if (a + b == sum) mask |= Bit(w);
                w++;
            }
            return FinalizeProblem(new CapsuleProblem {
                id = id, kind = CapsuleKind.DiceSum, band = band, labels = labels, answerMask = mask,
                prompt = "서로 다른 두 개의 주사위 A와 B를 동시에 던질 때, 나오는 두 눈의 수의 합이 " + sum + "일 확률을 구하시오.",
                shortCondition = "A+B=" + sum + "인 순서쌍",
                reveal = "합이 " + sum + "인 순서쌍 " + CountBits(mask) + "개 / 전체 36개",
                unitConcept = "두 주사위의 확률"
            });
        }

        static CapsuleProblem ThreeCoinsAtLeastOne(string id, int band)
        {
            var labels = CoinLabels();
            ulong mask = 0;
            for (int i = 0; i < 8; i++) if (i != 0) mask |= Bit(i); // 0 = 뒤뒤뒤
            return FinalizeProblem(new CapsuleProblem {
                id = id, kind = CapsuleKind.ThreeCoins, band = band, labels = labels, answerMask = mask,
                prompt = "서로 다른 세 개의 동전 A, B, C를 동시에 던질 때, 적어도 한 개는 앞면이 나올 확률을 구하시오.",
                shortCondition = "적어도 한 개가 앞면인 경우",
                reveal = "1 − 앞면이 없는 1/8 = 7/8", allowComplement = true,
                unitConcept = "어떤 사건이 일어나지 않을 확률"
            });
        }

        static CapsuleProblem ThreeCoinsAtLeastTwo(string id, int band)
        {
            var labels = CoinLabels();
            ulong mask = 0;
            for (int i = 0; i < 8; i++) if (HeadCount(i) >= 2) mask |= Bit(i);
            return FinalizeProblem(new CapsuleProblem {
                id = id, kind = CapsuleKind.ThreeCoins, band = band, labels = labels, answerMask = mask,
                prompt = "서로 다른 세 개의 동전 A, B, C를 동시에 던질 때, 두 개 이상이 앞면일 확률을 구하시오.",
                shortCondition = "앞면이 두 개 이상인 경우", reveal = "앞면이 2개 또는 3개인 경우",
                unitConcept = "경우의 수의 비율로서의 확률"
            });
        }

        static string[] CoinLabels()
        {
            var labels = new string[8];
            for (int i = 0; i < 8; i++)
                labels[i] = ((i & 4) != 0 ? "앞" : "뒤") + ((i & 2) != 0 ? "앞" : "뒤") + ((i & 1) != 0 ? "앞" : "뒤");
            return labels;
        }

        static int HeadCount(int v)
        {
            int n = 0;
            while (v != 0) { n += v & 1; v >>= 1; }
            return n;
        }

        static CapsuleProblem CoinPrime(string id, int band) => CoinDie(id, band, true);
        static CapsuleProblem CoinEven(string id, int band) => CoinDie(id, band, false);

        static CapsuleProblem CoinDie(string id, int band, bool prime)
        {
            var labels = new string[12];
            ulong mask = 0;
            int w = 0;
            for (int coin = 0; coin < 2; coin++)
            for (int die = 1; die <= 6; die++)
            {
                labels[w] = (coin == 0 ? "뒤" : "앞") + "·" + die;
                bool dieOk = prime ? die == 2 || die == 3 || die == 5 : die % 2 == 0;
                if (coin == 1 && dieOk) mask |= Bit(w);
                w++;
            }
            return FinalizeProblem(new CapsuleProblem {
                id = id, kind = CapsuleKind.CoinDie, band = band, labels = labels, answerMask = mask,
                prompt = "동전 한 개와 주사위 한 개를 동시에 던진다. 동전의 결과와 주사위의 결과가 서로 영향을 끼치지 않을 때, 동전은 앞면이 나오고 주사위는 " + (prime ? "소수" : "짝수") + "의 눈이 나올 확률을 구하시오.",
                shortCondition = "앞면 · " + (prime ? "소수" : "짝수") + "의 눈",
                reveal = "동전 앞면 1/2 × 주사위 " + (prime ? "소수" : "짝수") + " 3/6 = 1/4",
                unitConcept = "서로 영향을 끼치지 않는 두 사건의 확률"
            });
        }

        static CapsuleProblem FinalizeProblem(CapsuleProblem p)
        {
            p.answerCount = CountBits(p.answerMask);
            p.answer = new Fraction(p.answerCount, p.labels.Length);
            var candidates = new List<MisconceptionChoice>(8);
            AddWrong(candidates, p.answer, new Fraction(1, 2), "equiprobability_half", true);
            AddWrong(candidates, p.answer, new Fraction(p.answerCount, 1), "favorable_count_only", true);
            AddWrong(candidates, p.answer, new Fraction(p.answerCount + p.labels.Length, p.labels.Length), "add_independent_events", false);
            if (p.answerCount > 0) AddWrong(candidates, p.answer, new Fraction(p.labels.Length, p.answerCount), "reverse_fraction", false);
            AddWrong(candidates, p.answer, new Fraction(p.labels.Length - p.answerCount, p.labels.Length), "complement_confusion", true);
            for (int delta = 1; candidates.Count < 3 && delta <= p.labels.Length; delta++)
            {
                int n = p.answerCount + (delta % 2 == 1 ? delta : -delta);
                if (n >= 0 && n <= p.labels.Length)
                    AddWrong(candidates, p.answer, new Fraction(n, p.labels.Length), "nearby_case_count", true);
            }
            if (candidates.Count < 3) throw new InvalidOperationException("오답 후보 부족: " + p.id);
            p.distractors = candidates.ToArray();
            p.choices = new[] { p.answer.ToString(), candidates[0].value, candidates[1].value, candidates[2].value };
            // 결정적 셔플. answer는 반드시 choices 안에 있고 choices는 중복이 없다.
            int seed = StableHash(p.id);
            for (int i = p.choices.Length - 1; i > 0; i--)
            {
                int j = Math.Abs(seed + i * 7919) % (i + 1);
                string t = p.choices[i]; p.choices[i] = p.choices[j]; p.choices[j] = t;
            }
            return p;
        }

        static void AddWrong(List<MisconceptionChoice> list, Fraction answer, Fraction value, string id, bool requireProbabilityRange)
        {
            if (value.Equals(answer)) return; // 정답 제거
            if (requireProbabilityRange && (value.n < 0 || value.n > value.d)) return; // 범위 검사
            for (int i = 0; i < list.Count; i++) if (list[i].fraction.Equals(value)) return; // 중복 제거
            list.Add(new MisconceptionChoice { value = value.ToString(), misconceptionId = id, fraction = value });
        }

        static int StableHash(string s)
        {
            unchecked { int h = 17; for (int i = 0; i < s.Length; i++) h = h * 31 + s[i]; return h == int.MinValue ? 0 : h; }
        }

        static string Circled(int n)
        {
            string[] c = { "①", "②", "③", "④", "⑤", "⑥", "⑦", "⑧", "⑨", "⑩" };
            return n >= 1 && n <= c.Length ? c[n - 1] : n.ToString();
        }

        public static List<MgfProblem> BridgeBank(List<CapsuleProblem> bank)
        {
            var result = new List<MgfProblem>(bank.Count);
            for (int i = 0; i < bank.Count; i++)
            {
                CapsuleProblem p = bank[i];
                result.Add(new MgfProblem {
                    id = p.id,
                    prompt = p.prompt,
                    choices = p.choices,
                    answer = p.answer.ToString(),
                    answerNumeric = p.answer.Numeric,
                    unitConcept = p.unitConcept
                });
            }
            return result;
        }
    }
}
