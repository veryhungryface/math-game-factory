using System;
using System.Collections.Generic;
using System.Linq;
using Mgf;

namespace Mgf.GoseumdochiUcheguk
{
    public enum AddressMode { OrderedDigits, OrderedRoles, UnorderedRepresentatives }

    [Serializable]
    public struct AddressMark : IEquatable<AddressMark>
    {
        public int a;
        public int b;

        public AddressMark(int first, int second, bool unordered)
        {
            if (unordered && first > second) { a = second; b = first; }
            else { a = first; b = second; }
        }

        public bool Equals(AddressMark other) => a == other.a && b == other.b;
        public override bool Equals(object obj) => obj is AddressMark other && Equals(other);
        public override int GetHashCode() => a * 37 + b;
    }

    [Serializable]
    public sealed class AddressProblem
    {
        public string id;
        public string prompt;
        public string concept;
        public AddressMode mode;
        public int[] cards;
        public HashSet<AddressMark> answerSet;
        public string reveal;
        public int band;
    }

    public static class AddressRules
    {
        public const string MisconceptionLeadingZero = "leading-zero-address";
        public const string MisconceptionReuseCard = "reuse-single-card";
        public const string MisconceptionOrderIgnored = "ordered-pair-treated-unordered";
        public const string MisconceptionOrderDouble = "unordered-pair-counted-twice";
        public const string MisconceptionDuplicate = "duplicate-address";
        public const string MisconceptionMissing = "missing-address";

        // curriculum/2022-middle-math.json expression_traps copied into generator contract:
        // 1) 두 개 이상을 뽑는 발문에는 반드시 「서로 다른」을 쓴다.
        // 2) 역할이 있는 순서쌍과 역할이 없는 대표 2명을 구분한다.
        // 3) 0이 든 카드로 만드는 값은 「두 자리 자연수」 조건을 십의 자리에 반영한다.
        // 4) 중2 범위이므로 순열·조합 공식과 제곱근 기호를 사용하지 않는다.
        // 모든 답 집합은 카드 자릿값부터 전수 생성하며 무작위 생성 후 필터링하지 않는다.

        public static AddressProblem Practice()
        {
            return MakeDigits("practice-12", new[] { 1, 2, 3 }, 0,
                "1, 2, 3이 각각 하나씩 적힌 카드 중 서로 다른 2장을 놓아 주소 12를 압인하시오.");
        }

        public static AddressProblem ZoneOne()
        {
            return MakeDigits("zone-1", new[] { 1, 2, 3 }, 1,
                "1, 2, 3이 각각 하나씩 적힌 카드 중 서로 다른 2장으로 만들 수 있는 두 자리 자연수를 모두 나타내시오.");
        }

        public static AddressProblem ZoneTwo()
        {
            return MakeDigits("zone-2", new[] { 0, 1, 2, 3 }, 2,
                "0, 1, 2, 3이 각각 하나씩 적힌 카드 중 서로 다른 2장으로 만들 수 있는 두 자리 자연수를 모두 나타내시오.");
        }

        public static AddressProblem ZoneThreeOrdered()
        {
            int[] cards = { 0, 1, 2, 3, 4 };
            var set = BuildExpected(AddressMode.OrderedRoles, cards);
            return new AddressProblem
            {
                id = "zone-3-roles", prompt = "5명의 후보 중에서 회장 1명과 부회장 1명을 뽑는 모든 경우를 나타내시오.",
                concept = "역할이 있는 대표 뽑기", mode = AddressMode.OrderedRoles, cards = cards,
                answerSet = set, reveal = "회장 5가지 × 남은 부회장 4가지 = 20가지", band = 3
            };
        }

        public static AddressProblem ZoneThreeUnordered()
        {
            int[] cards = { 0, 1, 2, 3, 4 };
            var set = BuildExpected(AddressMode.UnorderedRepresentatives, cards);
            return new AddressProblem
            {
                id = "zone-3-pairs", prompt = "5명의 후보 중에서 역할이 같은 대표 2명을 뽑는 모든 경우를 나타내시오.",
                concept = "순서가 없는 대표 뽑기", mode = AddressMode.UnorderedRepresentatives, cards = cards,
                answerSet = set, reveal = "가-나와 나-가는 같은 대표 쌍이므로 모두 10가지", band = 3
            };
        }

        static AddressProblem MakeDigits(string id, int[] cards, int band, string prompt)
        {
            var set = BuildExpected(AddressMode.OrderedDigits, cards);
            int firstChoices = cards.Count(x => x != 0);
            return new AddressProblem
            {
                id = id, prompt = prompt, concept = "카드로 두 자리 자연수 만들기", mode = AddressMode.OrderedDigits,
                cards = cards, answerSet = set,
                reveal = "십의 자리 " + firstChoices + "가지 × 남은 일의 자리 " + (cards.Length - 1) + "가지 = " + set.Count + "가지",
                band = band
            };
        }

        public static HashSet<AddressMark> BuildExpected(AddressMode mode, int[] cards)
        {
            var result = new HashSet<AddressMark>();
            for (int i = 0; i < cards.Length; i++)
            for (int j = 0; j < cards.Length; j++)
            {
                int a = cards[i], b = cards[j];
                if (a == b) continue;
                if (mode == AddressMode.OrderedDigits && a == 0) continue;
                if (mode == AddressMode.UnorderedRepresentatives && a > b) continue;
                result.Add(new AddressMark(a, b, mode == AddressMode.UnorderedRepresentatives));
            }
            return result;
        }

        public static string CardLabel(AddressProblem p, int value)
        {
            if (p.mode == AddressMode.OrderedDigits) return value.ToString();
            string[] names = { "가", "나", "다", "라", "마", "바", "사", "아", "자", "차" };
            return names[Math.Max(0, Math.Min(names.Length - 1, value))];
        }

        public static string MarkLabel(AddressProblem p, AddressMark mark)
        {
            if (p.mode == AddressMode.OrderedDigits) return mark.a.ToString() + mark.b.ToString();
            string separator = p.mode == AddressMode.OrderedRoles ? "→" : "·";
            return CardLabel(p, mark.a) + separator + CardLabel(p, mark.b);
        }

        public static string Diagnose(AddressProblem p, IList<AddressMark> built)
        {
            var seen = new HashSet<AddressMark>();
            for (int i = 0; i < built.Count; i++)
            {
                var raw = built[i];
                if (p.mode == AddressMode.OrderedDigits && raw.a == 0) return MisconceptionLeadingZero;
                if (raw.a == raw.b) return MisconceptionReuseCard;
                var normalized = new AddressMark(raw.a, raw.b, p.mode == AddressMode.UnorderedRepresentatives);
                if (!seen.Add(normalized))
                    return p.mode == AddressMode.UnorderedRepresentatives ? MisconceptionOrderDouble : MisconceptionDuplicate;
            }
            if (p.mode == AddressMode.OrderedRoles)
            {
                for (int i = 0; i < built.Count; i++)
                {
                    var reverse = new AddressMark(built[i].b, built[i].a, false);
                    if (!seen.Contains(reverse)) return MisconceptionOrderIgnored;
                }
            }
            return MisconceptionMissing;
        }

        public static bool Exact(AddressProblem p, IList<AddressMark> built)
        {
            if (built.Count != p.answerSet.Count) return false;
            var seen = new HashSet<AddressMark>();
            for (int i = 0; i < built.Count; i++)
            {
                var m = new AddressMark(built[i].a, built[i].b, p.mode == AddressMode.UnorderedRepresentatives);
                if (!seen.Add(m) || !p.answerSet.Contains(m)) return false;
            }
            return seen.SetEquals(p.answerSet);
        }

        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(420);
            int id = 0;
            // 제약 만족 풀: 0~9에서 크기 3, 4의 카드 집합을 조합으로 직접 만든다.
            // 120 + 210 = 330개의 서로 다른 문항이며 사후 난수 필터가 없다.
            for (int size = 3; size <= 4; size++)
            {
                int[] buf = new int[size];
                BuildCombinations(bank, ref id, buf, 0, 0);
            }
            // 역할 있는/없는 대표 문항도 같은 튜플 생성기를 사용한다.
            for (int n = 4; n <= 9; n++)
            for (int variant = 0; variant < 14; variant++)
            {
                bool ordered = variant % 2 == 0;
                int[] cards = Enumerable.Range(0, n).ToArray();
                AddressMode mode = ordered ? AddressMode.OrderedRoles : AddressMode.UnorderedRepresentatives;
                var expected = BuildExpected(mode, cards);
                string context = new[] { "방송부", "과학부", "도서부", "환경부", "학생회", "체육부", "미술부" }[variant % 7];
                string prompt = context + " 후보 " + n + "명 중에서 " + (ordered ? "회장 1명과 부회장 1명" : "역할이 같은 대표 2명") + "을 뽑는 모든 경우를 나타내시오.";
                bank.Add(ToMgf("roles-" + (++id), prompt, mode, cards, expected));
            }
            return bank;
        }

        static void BuildCombinations(List<MgfProblem> bank, ref int id, int[] buffer, int depth, int start)
        {
            if (depth == buffer.Length)
            {
                int[] cards = (int[])buffer.Clone();
                var expected = BuildExpected(AddressMode.OrderedDigits, cards);
                string list = string.Join(", ", cards.Select(x => x.ToString()).ToArray());
                string prompt = list + "가 각각 하나씩 적힌 카드 중 서로 다른 2장으로 만들 수 있는 두 자리 자연수를 모두 나타내시오.";
                bank.Add(ToMgf("digits-" + (++id), prompt, AddressMode.OrderedDigits, cards, expected));
                return;
            }
            for (int value = start; value <= 10 - (buffer.Length - depth); value++)
            {
                buffer[depth] = value;
                BuildCombinations(bank, ref id, buffer, depth + 1, value + 1);
            }
        }

        static MgfProblem ToMgf(string id, string prompt, AddressMode mode, int[] cards, HashSet<AddressMark> expected)
        {
            var answer = FormatSet(mode, cards, expected);
            var distractors = new List<string>();

            // 오개념 역산 1: 한 장뿐인 카드를 같은 두 자리에 재사용한다.
            var reuse = new HashSet<AddressMark>(expected);
            for (int i = 0; i < cards.Length; i++) reuse.Add(new AddressMark(cards[i], cards[i], false));
            distractors.Add(FormatSet(mode, cards, reuse)); // misconceptionId: reuse-single-card

            // 오개념 역산 2: 순서 조건을 반대로 해석한다.
            AddressMode swappedMode = mode == AddressMode.UnorderedRepresentatives ? AddressMode.OrderedRoles : AddressMode.UnorderedRepresentatives;
            distractors.Add(FormatSet(swappedMode, cards, BuildExpected(swappedMode, cards))); // misconceptionId: ordered/unordered

            // 오개념 역산 3: 0을 십의 자리에도 둔다. 0이 없으면 한 경우를 누락한다.
            var third = new HashSet<AddressMark>(expected);
            if (mode == AddressMode.OrderedDigits && cards.Contains(0))
            {
                for (int i = 0; i < cards.Length; i++) if (cards[i] != 0) third.Add(new AddressMark(0, cards[i], false));
            }
            else if (third.Count > 0) third.Remove(third.OrderBy(x => x.a).ThenBy(x => x.b).First());
            distractors.Add(FormatSet(mode, cards, third)); // misconceptionId: leading-zero-address | missing-address

            var choices = new List<string> { answer };
            for (int i = 0; i < distractors.Count; i++) if (!choices.Contains(distractors[i])) choices.Add(distractors[i]);
            while (choices.Count < 4) choices.Add("경우 " + (expected.Count + choices.Count) + "가지");

            return new MgfProblem
            {
                id = id, prompt = prompt, choices = choices.ToArray(), answer = answer,
                answerNumeric = expected.Count,
                unitConcept = mode == AddressMode.OrderedDigits ? "카드로 두 자리 자연수 만들기" :
                    mode == AddressMode.OrderedRoles ? "역할이 있는 대표 뽑기" : "순서가 없는 대표 뽑기"
            };
        }

        static string FormatSet(AddressMode mode, int[] cards, IEnumerable<AddressMark> values)
        {
            var p = new AddressProblem { mode = mode, cards = cards };
            var labels = values.Select(x => MarkLabel(p, x)).Distinct().OrderBy(x => x).ToArray();
            return "{" + string.Join(", ", labels) + "}";
        }

        public static void ExhaustiveInvariantCheck()
        {
            var bank = BuildBank();
            if (bank.Count < 300) throw new InvalidOperationException("문제 은행 300개 미만");
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < bank.Count; i++)
            {
                var p = bank[i];
                if (!ids.Add(p.id) || !prompts.Add(p.prompt)) throw new InvalidOperationException("문항 중복: " + p.id);
                if (p.choices == null || p.choices.Length < 4) throw new InvalidOperationException("선택지 부족: " + p.id);
                var unique = new HashSet<string>(p.choices);
                if (unique.Count != p.choices.Length || !unique.Contains(p.answer)) throw new InvalidOperationException("선택지 불변식: " + p.id);
                if (p.prompt.Contains("√") || p.prompt.Contains("가정") || p.prompt.Contains("결론")) throw new InvalidOperationException("학년 표현 위반: " + p.id);
            }
        }
    }
}
