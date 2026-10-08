// 뭉실 패션쇼 문제 규칙. 실제 플레이와 QA 문제 은행이 같은 FashionOrder 객체를 사용한다.
// 중학교 style_guide / expression_traps:
// - 값 문항은 교과서 지시형 「~을/를 구하시오」로 통일한다.
// - 두 동전·주사위는 반드시 「서로 다른」을 명시하고 순서쌍으로 센다.
// - 합의 법칙은 두 사건이 동시에 일어나지 않는 문제만 만들며, 겹침 문항은 직접 나열로 검산한다.
// - 0 카드 문항은 십의 자리 후보에서 0을 먼저 제외해 구조적으로 생성한다.
// - 회장·부회장은 n(n-1), 자격이 같은 대표 2명은 n(n-1)/2로 정수 연산한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.MungsilFashionShow
{
    public enum OrderKind
    {
        Product, Sum, DiceSum, CoinPair, ZeroCards, OrderedRoles,
        RepresentativePair, OverlapMultiples, SingleEvent, ErrorFind
    }

    [Serializable]
    public sealed class FashionOrder
    {
        public string id;
        public string prompt;
        public string concept;
        public string reveal;
        public OrderKind kind;
        public int band;
        public int answer;
        public int a;
        public int b;
        public int c;
        public int distractorA;
        public int distractorB;
        public string misconceptionA;
        public string misconceptionB;
        public string[] choices;

        public MgfProblem ToMgf()
        {
            return new MgfProblem
            {
                id = id,
                prompt = prompt,
                choices = choices,
                answer = answer.ToString(),
                answerNumeric = answer,
                unitConcept = concept
            };
        }

        public string MisconceptionFor(int guessed)
        {
            if (guessed == distractorA) return misconceptionA;
            if (guessed == distractorB) return misconceptionB;
            return guessed < answer ? "undercount" : "overcount";
        }
    }

    public static class MungsilRules
    {
        public const int StartLives = 3;
        public const int Goal = 8;
        public const int MaxRope = 30;

        static readonly string[] ProductFirst =
        {
            "모자가", "상의가", "가방이", "재킷이", "신발이", "무대 조명이"
        };

        static readonly string[] ProductSecond =
        {
            "리본이", "하의가", "장식 핀이", "스카프가", "양말이", "포즈가"
        };

        static readonly string[] ProductPair =
        {
            "모자와 리본", "상의와 하의", "가방과 장식 핀",
            "재킷과 스카프", "신발과 양말", "무대 조명과 포즈"
        };

        static readonly string[] SumFirst =
        {
            "낮 쇼의 모자가", "민트 리본이", "A관 좌석이",
            "신인 부문 의상이", "단색 천이", "첫째 무대의 조명이"
        };

        static readonly string[] SumSecond =
        {
            "밤 쇼의 가방이", "자두색 브로치가", "B관 좌석이",
            "자유 부문 의상이", "무늬 천이", "둘째 무대의 조명이"
        };

        static readonly string[] CandidateGroups =
        {
            "패션 동아리 후보", "무대 감독 후보", "조명부 후보", "의상부 후보",
            "학생회 후보", "심사단 후보"
        };

        public static FashionOrder Practice()
        {
            return Finish(new FashionOrder
            {
                id = "practice-2x3", kind = OrderKind.Product, band = 0,
                a = 2, b = 3, answer = 6,
                prompt = "모자 2종류와 리본 3종류 중에서 각각 하나씩 고르는 경우의 수를 구하시오.",
                concept = "두 사건이 잇달아 일어날 때의 곱의 법칙",
                reveal = "모자 2줄 × 리본 3칸 = 6가지",
                distractorA = 5, misconceptionA = "product-as-sum",
                distractorB = 3, misconceptionB = "count-only-one-stage"
            });
        }

        public static List<FashionOrder> BuildCatalog()
        {
            var result = new List<FashionOrder>(600);
            var seen = new HashSet<string>();
            int serial = 1;

            for (int w = 0; w < ProductPair.Length; w++)
            for (int a = 2; a <= 6; a++)
            for (int b = 2; b <= 6; b++)
            {
                int answer = a * b;
                if (answer > MaxRope || answer == a + b) continue; // 2×2처럼 오답 공식이 우연히 참인 값 제거.
                Add(result, seen, Finish(new FashionOrder
                {
                    id = "product-" + serial++, kind = OrderKind.Product, band = answer <= 10 ? 1 : 2,
                    a = a, b = b, answer = answer,
                    prompt = $"{ProductFirst[w]} {a}가지이고 {ProductSecond[w]} {b}가지일 때, {ProductPair[w]} 중에서 각각 하나씩 고르는 경우의 수를 구하시오.",
                    concept = "두 사건이 잇달아 일어날 때의 곱의 법칙",
                    reveal = $"첫 번째 선택 한 가지마다 두 번째 선택 {b}가지 → {a}×{b}={answer}",
                    distractorA = a + b, misconceptionA = "product-as-sum",
                    distractorB = Math.Max(a, b), misconceptionB = "count-only-one-stage"
                }));
            }

            for (int w = 0; w < SumFirst.Length; w++)
            for (int a = 2; a <= 8; a++)
            for (int b = 2; b <= 6; b++)
            {
                int answer = a + b;
                if (answer == a * b) continue; // 2+2=2×2 우연 충돌 제거.
                Add(result, seen, Finish(new FashionOrder
                {
                    id = "sum-" + serial++, kind = OrderKind.Sum, band = answer <= 8 ? 1 : 2,
                    a = a, b = b, answer = answer,
                    prompt = $"두 선택은 동시에 고를 수 없다. {SumFirst[w]} {a}가지이고 {SumSecond[w]} {b}가지일 때, 둘 중 하나를 고르는 경우의 수를 구하시오.",
                    concept = "두 사건이 동시에 일어나지 않을 때의 합의 법칙",
                    reveal = $"동시에 일어나지 않으므로 {a}+{b}={answer}",
                    distractorA = a * b, misconceptionA = "sum-as-product",
                    distractorB = Math.Max(a, b), misconceptionB = "count-only-one-event"
                }));
            }

            string[] dicePairs = { "빨간색과 파란색", "민트색과 자두색", "노란색과 흰색", "주황색과 남색", "분홍색과 초록색" };
            for (int p = 0; p < dicePairs.Length; p++)
            for (int target = 3; target <= 11; target++)
            {
                int answer = DiceSumCount(target);
                int unordered = (answer + 1) / 2;
                Add(result, seen, Finish(new FashionOrder
                {
                    id = "dice-sum-" + serial++, kind = OrderKind.DiceSum, band = 2,
                    a = target, b = p, answer = answer,
                    prompt = $"서로 다른 {dicePairs[p]} 두 개의 주사위를 동시에 던질 때, 두 눈의 수의 합이 {target}인 경우의 수를 구하시오.",
                    concept = "서로 다른 두 주사위의 순서쌍",
                    reveal = $"(첫째, 둘째)를 구별해 세면 {answer}가지",
                    distractorA = unordered, misconceptionA = "distinct-dice-unordered",
                    distractorB = target, misconceptionB = "target-as-count"
                }));
            }

            string[] coinPairs = { "금색과 은색", "민트색과 자두색", "줄무늬와 점무늬", "큰 글자와 작은 글자", "별과 달", "A와 B" };
            for (int p = 0; p < coinPairs.Length; p++)
            {
                Add(result, seen, Finish(new FashionOrder
                {
                    id = "coins-" + serial++, kind = OrderKind.CoinPair, band = 2,
                    a = p, b = 2, answer = 4,
                    prompt = $"서로 다른 {coinPairs[p]} 두 개의 동전을 동시에 던질 때, 나오는 모든 경우의 수를 구하시오.",
                    concept = "서로 다른 두 동전의 순서쌍", reveal = "(앞, 뒤)와 (뒤, 앞)을 구별하면 4가지",
                    distractorA = 3, misconceptionA = "distinct-coins-unordered",
                    distractorB = 2, misconceptionB = "count-only-one-coin"
                }));
            }

            // 자릿값부터 역으로 생성: 십의 자리 후보는 0을 뺀 n-1개, 일의 자리는 남은 n-1개다.
            for (int count = 4; count <= 6; count++)
            for (int shift = 0; shift < 9; shift++)
            {
                var digits = new List<int> { 0 };
                for (int k = 0; k < count - 1; k++) digits.Add(1 + (shift + k) % 9);
                int answer = (count - 1) * (count - 1);
                Add(result, seen, Finish(new FashionOrder
                {
                    id = "zero-cards-" + serial++, kind = OrderKind.ZeroCards, band = 3,
                    a = count, b = shift, answer = answer,
                    prompt = $"숫자 {string.Join(", ", digits)}을 각각 하나씩 적은 카드 {count}장 중 2장을 뽑아 만들 수 있는 두 자리 자연수의 개수를 구하시오.",
                    concept = "0이 포함된 카드로 두 자리 자연수 만들기",
                    reveal = $"십의 자리 {count - 1}가지 × 일의 자리 {count - 1}가지 = {answer}",
                    distractorA = count * (count - 1), misconceptionA = "leading-zero-included",
                    distractorB = (count - 1) * (count - 2), misconceptionB = "zero-excluded-from-ones"
                }));
            }

            for (int g = 0; g < CandidateGroups.Length; g++)
            for (int n = 4; n <= 6; n++)
            {
                int ordered = n * (n - 1);
                Add(result, seen, Finish(new FashionOrder
                {
                    id = "ordered-roles-" + serial++, kind = OrderKind.OrderedRoles, band = 3,
                    a = n, b = g, answer = ordered,
                    prompt = $"{CandidateGroups[g]} {n}명 중에서 회장 1명과 부회장 1명을 뽑는 경우의 수를 구하시오.",
                    concept = "역할이 다른 두 사람 뽑기", reveal = $"회장 {n}가지 × 부회장 {n - 1}가지 = {ordered}",
                    distractorA = ordered / 2, misconceptionA = "ordered-roles-as-pair",
                    distractorB = n + (n - 1), misconceptionB = "product-as-sum"
                }));

                int pair = ordered / 2;
                Add(result, seen, Finish(new FashionOrder
                {
                    id = "representatives-" + serial++, kind = OrderKind.RepresentativePair, band = 3,
                    a = n, b = g, answer = pair,
                    prompt = $"{CandidateGroups[g]} {n}명 중에서 자격이 같은 대표 2명을 뽑는 경우의 수를 구하시오.",
                    concept = "자격이 같은 대표 2명 뽑기", reveal = $"순서를 바꾼 같은 쌍을 합치면 {n}×{n - 1}÷2={pair}",
                    distractorA = ordered, misconceptionA = "representatives-ordered",
                    distractorB = n + (n - 1), misconceptionB = "pair-as-sum"
                }));
            }

            int[,] overlapSets =
            {
                { 10, 2, 3 }, { 14, 3, 4 }, { 18, 4, 6 }, { 20, 3, 5 },
                { 24, 4, 6 }, { 25, 4, 6 }, { 28, 5, 7 }, { 30, 4, 6 }
            };
            for (int i = 0; i < overlapSets.GetLength(0); i++)
            {
                int limit = overlapSets[i, 0], d1 = overlapSets[i, 1], d2 = overlapSets[i, 2];
                int answer = CountUnion(limit, d1, d2);
                int naive = limit / d1 + limit / d2;
                if (answer > MaxRope || naive == answer) continue;
                Add(result, seen, Finish(new FashionOrder
                {
                    id = "overlap-" + serial++, kind = OrderKind.OverlapMultiples, band = 3,
                    a = limit, b = d1, c = d2, answer = answer,
                    prompt = $"1부터 {limit}까지의 자연수 중에서 {d1}의 배수 또는 {d2}의 배수인 수의 개수를 구하시오. 직접 나열하여 세시오.",
                    concept = "겹치는 사건을 직접 나열해 세기", reveal = $"겹치는 수는 한 번만 세어 {answer}가지",
                    distractorA = naive, misconceptionA = "overlap-double-count",
                    distractorB = Math.Max(limit / d1, limit / d2), misconceptionB = "count-only-one-event"
                }));
            }

            // 오류 찾기형: 잘못 놓인 줄의 근거를 확인하되, 답은 바른 경우의 수를 줄 위치로 제출한다.
            for (int a = 3; a <= 6; a++)
            for (int b = 2; b <= 5; b++)
            {
                int answer = a * b;
                if (answer > MaxRope || answer == a + b) continue;
                Add(result, seen, Finish(new FashionOrder
                {
                    id = "error-find-" + serial++, kind = OrderKind.ErrorFind, band = 3,
                    a = a, b = b, answer = answer,
                    prompt = $"모자 {a}종류와 리본 {b}종류에서 각각 하나씩 고르는데 줄을 {a + b}칸에 두었다. 바른 경우의 수를 구하시오.",
                    concept = "합과 곱을 바꾼 오류 찾기", reveal = $"각 모자마다 리본 {b}종류가 있으므로 {a}×{b}={answer}",
                    distractorA = a + b, misconceptionA = "product-as-sum",
                    distractorB = Math.Max(a, b), misconceptionB = "count-only-one-stage"
                }));
            }

            if (result.Count < 300) throw new InvalidOperationException("문제 은행이 300개보다 작습니다: " + result.Count);
            Validate(result);
            return result;
        }

        // 12개의 서로 다른 정답 버킷에서 한 세션마다 8개를 순환 추출한다.
        // 12회 주기 전체에서 각 정답의 빈도는 정확히 1/12이며, 한 세션 안에서는 답이 반복되지 않는다.
        public static FashionOrder PickSession(List<FashionOrder> catalog, int solved, int runSerial, int variant)
        {
            int[] buckets = { 4, 5, 6, 7, 8, 9, 10, 12, 15, 18, 20, 24 };
            var selected = new List<int>(8);
            int start = PositiveMod(runSerial * 5, buckets.Length);
            for (int i = 0; i < 8; i++) selected.Add(buckets[(start + i) % buckets.Length]);
            selected.Sort((x, y) =>
            {
                int d = DifficultyKey(x).CompareTo(DifficultyKey(y));
                return d != 0 ? d : x.CompareTo(y);
            });
            int index = Math.Min(solved, selected.Count - 1);
            int desired = selected[index];
            OrderKind kind = KindForAnswer(desired);
            var pool = new List<FashionOrder>();
            for (int i = 0; i < catalog.Count; i++)
                if (catalog[i].kind == kind && catalog[i].answer == desired) pool.Add(catalog[i]);
            if (pool.Count == 0) throw new InvalidOperationException("세션 문제 풀이 비었습니다: " + kind + " / " + desired);
            return pool[PositiveMod(runSerial * 31 + variant * 17 + solved * 47, pool.Count)];
        }

        static int DifficultyKey(int answer)
        {
            if (answer == 5 || answer == 6 || answer == 8) return 1;
            if (answer == 4 || answer == 12 || answer == 18) return 2;
            return 3;
        }

        static OrderKind KindForAnswer(int answer)
        {
            switch (answer)
            {
                case 4: return OrderKind.DiceSum;
                case 5: return OrderKind.Sum;
                case 6: return OrderKind.Product;
                case 7: return OrderKind.OverlapMultiples;
                case 8: return OrderKind.Sum;
                case 9: return OrderKind.ZeroCards;
                case 10: return OrderKind.RepresentativePair;
                case 12: return OrderKind.Product;
                case 15: return OrderKind.RepresentativePair;
                case 18: return OrderKind.Product;
                case 20: return OrderKind.OrderedRoles;
                case 24: return OrderKind.ErrorFind;
                default: throw new InvalidOperationException("정의되지 않은 세션 정답: " + answer);
            }
        }

        static FashionOrder Finish(FashionOrder p)
        {
            if (p.answer < 1 || p.answer > MaxRope) throw new InvalidOperationException("줄 범위 밖 정답: " + p.id);
            if (p.distractorA == p.answer || p.distractorB == p.answer || p.distractorA == p.distractorB)
                throw new InvalidOperationException("오답 공식 우연 충돌: " + p.id);

            var values = new List<int> { p.answer, p.distractorA, p.distractorB };
            int fill = p.answer + 1;
            while (values.Contains(fill) || fill > MaxRope) fill = fill > MaxRope ? Math.Max(1, p.answer - 1) : fill + 1;
            while (values.Contains(fill)) fill = fill > 1 ? fill - 1 : fill + 2;
            values.Add(fill);
            int shift = PositiveMod(StableCode(p.id), values.Count);
            p.choices = new string[values.Count];
            for (int i = 0; i < values.Count; i++) p.choices[(i + shift) % values.Count] = values[i].ToString();
            return p;
        }

        static void Add(List<FashionOrder> result, HashSet<string> seen, FashionOrder p)
        {
            string key = p.prompt + "|" + p.answer;
            if (seen.Add(key)) result.Add(p);
        }

        static void Validate(List<FashionOrder> all)
        {
            var ids = new HashSet<string>();
            for (int i = 0; i < all.Count; i++)
            {
                FashionOrder p = all[i];
                if (!ids.Add(p.id)) throw new InvalidOperationException("중복 id: " + p.id);
                int expected = EnumerateAnswer(p);
                if (expected != p.answer) throw new InvalidOperationException($"정답 불일치 {p.id}: {p.answer}/{expected}");
                var values = new HashSet<string>();
                bool hasAnswer = false;
                for (int k = 0; k < p.choices.Length; k++)
                {
                    if (!values.Add(p.choices[k])) throw new InvalidOperationException("선택지 중복: " + p.id);
                    if (p.choices[k] == p.answer.ToString()) hasAnswer = true;
                }
                if (!hasAnswer) throw new InvalidOperationException("정답 선택지 없음: " + p.id);
                if (p.distractorA == expected || p.distractorB == expected)
                    throw new InvalidOperationException("오답이 참이 됨: " + p.id);
            }
        }

        static int EnumerateAnswer(FashionOrder p)
        {
            switch (p.kind)
            {
                case OrderKind.Product:
                case OrderKind.ErrorFind: return p.a * p.b;
                case OrderKind.Sum: return p.a + p.b;
                case OrderKind.DiceSum: return DiceSumCount(p.a);
                case OrderKind.CoinPair: return 4;
                case OrderKind.ZeroCards: return (p.a - 1) * (p.a - 1);
                case OrderKind.OrderedRoles: return p.a * (p.a - 1);
                case OrderKind.RepresentativePair: return p.a * (p.a - 1) / 2;
                case OrderKind.OverlapMultiples: return CountUnion(p.a, p.b, p.c);
                case OrderKind.SingleEvent: return p.answer;
                default: return p.answer;
            }
        }

        static int DiceSumCount(int target)
        {
            int count = 0;
            for (int first = 1; first <= 6; first++)
                for (int second = 1; second <= 6; second++)
                    if (first + second == target) count++;
            return count;
        }

        static int CountUnion(int limit, int d1, int d2)
        {
            int count = 0;
            for (int n = 1; n <= limit; n++) if (n % d1 == 0 || n % d2 == 0) count++;
            return count;
        }

        static int StableCode(string s)
        {
            int value = 17;
            for (int i = 0; i < s.Length; i++) value = unchecked(value * 31 + s[i]);
            return value;
        }

        static int PositiveMod(int x, int n) { int r = x % n; return r < 0 ? r + n : r; }
    }
}
