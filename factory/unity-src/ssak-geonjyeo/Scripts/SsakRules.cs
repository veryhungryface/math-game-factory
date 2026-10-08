// 싹 건져 — 중2 「경우의 수」 정수 집합 생성기/판정기.
//
// curriculum/2022-middle-math.json style_guide / expression_traps 반영:
// - 발문은 지시형 「~에 해당하는 경우를 모두 건지시오」로 통일한다.
// - 등번호와 집게번호는 역할이 다른 순서쌍이다. (1,2)와 (2,1)을 합치지 않는다.
// - 「또는」은 동시에 일어나지 않는 두 사건만 생성하고, 답은 두 집합의 합집합이다.
// - 「각각 하나씩」은 데카르트 곱이며 정수 비트마스크를 전수 구성한다.
// - 확률, 순열·조합 기호, 포함배제 공식, 근호는 생성하지 않는다.
//
// 오개념 역산 id:
// - sum_as_product: 또는인 두 사건의 경우의 수를 곱함.
// - product_as_sum: 두 표식을 각각 고르는 경우의 수를 더함.
// - unordered_pair_roles: 등번호/집게번호 역할을 지워 (a,b)와 (b,a)를 같은 경우로 셈.
// - one_axis_only: 한 등번호 또는 한 집게번호의 경우만 건짐.
// - right_count_wrong_set: 수는 맞지만 조건에 없는 경우를 넣고 필요한 경우를 빠뜨림.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;

namespace Mgf.SsakGeonjyeo
{
    public enum TideKind { Single, Sum, Product }

    [Serializable]
    public sealed class TideProblem
    {
        public string id;
        public TideKind kind;
        public int band;
        public int answerMask;
        public int answerCount;
        public int firstMask;
        public int secondMask;
        public int xMask;
        public int yMask;
        public int firstCount;
        public int secondCount;
        public string prompt;
        public string reveal;
        public string unitConcept;
    }

    public static class SsakRules
    {
        public const int UniverseCount = 12;
        public const int TargetTides = 7;
        public const int StartLives = 3;
        public const int RequiredFirstCorrect = 5;
        public const float RunSeconds = 90f;

        public static int OutcomeId(int back, int claw) => back * 10 + claw;
        public static int OutcomeIndex(int back, int claw) => (back - 1) * 3 + (claw - 1);
        public static int Bit(int back, int claw) => 1 << OutcomeIndex(back, claw);
        public static int BackOfIndex(int index) => index / 3 + 1;
        public static int ClawOfIndex(int index) => index % 3 + 1;

        public static int CountBits(int mask)
        {
            int n = 0;
            while (mask != 0) { n += mask & 1; mask >>= 1; }
            return n;
        }

        static int Rectangle(int xMask, int yMask)
        {
            int result = 0;
            for (int a = 1; a <= 4; a++)
            for (int b = 1; b <= 3; b++)
                if ((xMask & (1 << (a - 1))) != 0 && (yMask & (1 << (b - 1))) != 0)
                    result |= Bit(a, b);
            return result;
        }

        static string Values(int mask, int count)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(i + 1);
            }
            return sb.ToString();
        }

        static string EventText(int xMask, int yMask)
        {
            bool allX = xMask == 15, allY = yMask == 7;
            if (allY && CountBits(xMask) == 1) return "등번호가 " + Values(xMask, 4) + "인 경우";
            if (allX && CountBits(yMask) == 1) return "집게번호가 " + Values(yMask, 3) + "인 경우";
            return "등번호가 " + Values(xMask, 4) + " 중 하나이고 집게번호가 " + Values(yMask, 3) + " 중 하나인 경우";
        }

        public static string PairList(int mask)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < UniverseCount; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append('(').Append(BackOfIndex(i)).Append(',').Append(ClawOfIndex(i)).Append(')');
            }
            return sb.ToString();
        }

        public static int[] OutcomeIds(int mask)
        {
            var result = new int[CountBits(mask)];
            int w = 0;
            for (int i = 0; i < UniverseCount; i++)
                if ((mask & (1 << i)) != 0) result[w++] = OutcomeId(BackOfIndex(i), ClawOfIndex(i));
            return result;
        }

        public static TideProblem Practice()
        {
            int answer = Bit(1, 1) | Bit(1, 2);
            return new TideProblem {
                id = "sg-practice", kind = TideKind.Single, band = 0,
                answerMask = answer, answerCount = 2, xMask = 1, yMask = 3,
                prompt = "등번호가 1인 경우를 모두 건지시오.",
                reveal = "집게번호 1, 2 → 2가지 · (1,1), (1,2)",
                unitConcept = "한 사건의 경우의 수"
            };
        }

        static TideProblem SingleBack(int a)
        {
            int mask = Rectangle(1 << (a - 1), 7);
            return new TideProblem {
                id = "single-back-" + a, kind = TideKind.Single, band = 1,
                answerMask = mask, answerCount = 3, xMask = 1 << (a - 1), yMask = 7,
                prompt = "등번호가 " + a + "인 경우를 모두 건지시오.",
                reveal = "등번호 " + a + "의 집게번호 1, 2, 3 → 3가지",
                unitConcept = "한 사건의 경우의 수"
            };
        }

        static TideProblem SingleClaw(int b)
        {
            int mask = Rectangle(15, 1 << (b - 1));
            return new TideProblem {
                id = "single-claw-" + b, kind = TideKind.Single, band = 1,
                answerMask = mask, answerCount = 4, xMask = 15, yMask = 1 << (b - 1),
                prompt = "집게번호가 " + b + "인 경우를 모두 건지시오.",
                reveal = "등번호 1, 2, 3, 4의 집게번호 " + b + " → 4가지",
                unitConcept = "한 사건의 경우의 수"
            };
        }

        static TideProblem SumProblem(int r1, int r2, int serial)
        {
            int union = r1 | r2;
            int c1 = CountBits(r1), c2 = CountBits(r2);
            return new TideProblem {
                id = "sum-" + serial + "-" + r1 + "-" + r2, kind = TideKind.Sum, band = 2,
                answerMask = union, answerCount = c1 + c2, firstMask = r1, secondMask = r2,
                firstCount = c1, secondCount = c2,
                prompt = "사건 A: " + DescribeMask(r1) + "\n사건 B: " + DescribeMask(r2) + "\nA 또는 B에 해당하는 경우를 모두 건지시오.",
                reveal = c1 + "+" + c2 + "=" + (c1 + c2) + " · 동시에 일어나지 않는 두 사건의 합",
                unitConcept = "합의 법칙"
            };
        }

        static string DescribeMask(int mask)
        {
            for (int xm = 1; xm < 16; xm++)
            for (int ym = 1; ym < 8; ym++)
                if (Rectangle(xm, ym) == mask) return EventText(xm, ym);
            return "{" + PairList(mask) + "}";
        }

        static TideProblem ProductProblem(int xMask, int yMask, int serial)
        {
            int answer = Rectangle(xMask, yMask);
            int cx = CountBits(xMask), cy = CountBits(yMask);
            return new TideProblem {
                id = "product-" + serial + "-" + xMask + "-" + yMask, kind = TideKind.Product, band = 3,
                answerMask = answer, answerCount = cx * cy, xMask = xMask, yMask = yMask,
                firstCount = cx, secondCount = cy,
                prompt = "등번호 " + Values(xMask, 4) + " 중 하나와 집게번호 " + Values(yMask, 3) + " 중 하나를 각각 고를 때의 모든 경우를 건지시오.",
                reveal = cx + "×" + cy + "=" + (cx * cy) + " · " + PairList(answer),
                unitConcept = "곱의 법칙"
            };
        }

        public static List<TideProblem> BuildProblems()
        {
            var result = new List<TideProblem>(360);
            for (int a = 1; a <= 4; a++) result.Add(SingleBack(a));
            for (int b = 1; b <= 3; b++) result.Add(SingleClaw(b));

            // 사후 난수 필터가 아니라 유한 제약 풀을 전수 열거한다.
            var rectangles = new List<int>();
            var rectSeen = new HashSet<int>();
            for (int xm = 1; xm < 16; xm++)
            for (int ym = 1; ym < 8; ym++)
            {
                int r = Rectangle(xm, ym), c = CountBits(r);
                if (c >= 1 && c <= 4 && rectSeen.Add(r)) rectangles.Add(r);
            }
            int serial = 0;
            for (int i = 0; i < rectangles.Count; i++)
            for (int j = i + 1; j < rectangles.Count; j++)
            {
                int r1 = rectangles[i], r2 = rectangles[j];
                int c1 = CountBits(r1), c2 = CountBits(r2);
                if ((r1 & r2) != 0 || c1 + c2 < 3 || c1 + c2 > 6) continue;
                result.Add(SumProblem(r1, r2, serial++));
                if (result.Count >= 307) break;
            }

            int productSerial = 0;
            for (int xm = 1; xm < 16; xm++)
            for (int ym = 1; ym < 8; ym++)
            {
                int n = CountBits(xm) * CountBits(ym);
                if (n < 3 || n > 6) continue;
                result.Add(ProductProblem(xm, ym, productSerial++));
            }
            if (result.Count < 300) throw new InvalidOperationException("경우의 수 문제은행 300개 미만");
            return result;
        }

        public static List<TideProblem> RunDeck(Random rng, List<TideProblem> bank)
        {
            var singles = new List<TideProblem>();
            var sums = new List<TideProblem>();
            var products = new List<TideProblem>();
            for (int i = 0; i < bank.Count; i++)
            {
                if (bank[i].kind == TideKind.Single) singles.Add(bank[i]);
                else if (bank[i].kind == TideKind.Sum) sums.Add(bank[i]);
                else products.Add(bank[i]);
            }
            var deck = new List<TideProblem>(TargetTides);
            PickDifferent(deck, singles, 2, rng);
            PickDifferent(deck, sums, 2, rng);
            PickDifferent(deck, products, 3, rng);
            return deck;
        }

        static void PickDifferent(List<TideProblem> deck, List<TideProblem> pool, int n, Random rng)
        {
            var used = new HashSet<int>();
            while (used.Count < n)
            {
                int index = rng.Next(pool.Count);
                if (used.Add(index)) deck.Add(pool[index]);
            }
        }

        public static bool IsCorrect(TideProblem p, int capacity, int selectedMask)
        {
            return p != null && capacity == p.answerCount && selectedMask == p.answerMask;
        }

        static int CollapsedRoleMask(int mask)
        {
            int result = 0;
            for (int i = 0; i < UniverseCount; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                int a = BackOfIndex(i), b = ClawOfIndex(i);
                int lo = Math.Min(a, b), hi = Math.Max(a, b);
                result |= 1 << ((lo - 1) * 4 + (hi - 1));
            }
            return result;
        }

        public static string MisconceptionId(TideProblem p, int capacity, int selectedMask)
        {
            if (p == null || IsCorrect(p, capacity, selectedMask)) return "";
            if (p.kind == TideKind.Sum && capacity == p.firstCount * p.secondCount) return "sum_as_product";
            if (p.kind == TideKind.Product && capacity == p.firstCount + p.secondCount) return "product_as_sum";
            if (p.kind == TideKind.Product && selectedMask != p.answerMask &&
                CollapsedRoleMask(selectedMask) == CollapsedRoleMask(p.answerMask)) return "unordered_pair_roles";
            if (p.kind == TideKind.Product && (selectedMask == Rectangle(p.xMask, 1) || selectedMask == Rectangle(1, p.yMask)))
                return "one_axis_only";
            if (capacity == p.answerCount && CountBits(selectedMask) == p.answerCount) return "right_count_wrong_set";
            if ((selectedMask & ~p.answerMask) != 0) return "extra_case";
            if ((p.answerMask & ~selectedMask) != 0) return "missing_case";
            return capacity < p.answerCount ? "net_too_small" : "net_too_large";
        }

        public static List<MgfProblem> BuildBank()
        {
            var raw = BuildProblems();
            var result = new List<MgfProblem>(raw.Count);
            var seen = new HashSet<string>();
            for (int i = 0; i < raw.Count; i++)
            {
                TideProblem p = raw[i];
                if (CountBits(p.answerMask) != p.answerCount) throw new InvalidOperationException("답 집합 크기 불일치: " + p.id);
                if (!seen.Add(p.prompt + "|" + p.answerMask)) throw new InvalidOperationException("중복 문항: " + p.id);
                result.Add(new MgfProblem {
                    id = p.id,
                    prompt = p.prompt,
                    choices = null,
                    answer = p.answerCount + "칸 · " + PairList(p.answerMask),
                    answerNumeric = p.answerCount,
                    unitConcept = p.unitConcept
                });
            }
            return result;
        }
    }
}
