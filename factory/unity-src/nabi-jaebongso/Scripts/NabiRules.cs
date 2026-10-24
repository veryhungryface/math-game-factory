// 나비 재봉소 — SSS 작도 문제 정본과 정수 판정 규칙.
//
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 중학교 문제 종결형은 「~을/를 작도하시오」「~을/를 고치시오」로 통일한다.
// - △ABC≡△DEF는 A↔D, B↔E, C↔F 순서가 대응한다. 화면의 좌우가 아니라 기호 순서가 정본이다.
// - 수와 단위는 띄어 쓴다(6 cm). 중1 범위 밖의 근호·삼각비는 사용하지 않는다.
// - 「가정」「결론」을 쓰지 않고, 세 대응변이 같다는 실제 작도 기록으로 SSS 합동을 설명한다.
//
// 오개념 역산(misconceptionId):
// - correspondence_order: 기호 순서 대신 화면 좌우를 따라 두 반지름을 서로 바꾼다.
// - arbitrary_edge: 필요한 대응변이 아닌 나머지 원본 변을 반지름으로 옮긴다.
// - one_radius_only: 교점이 한 원호에만 놓여 두 대응변 중 하나만 같은 상태다.
using System;
using System.Collections.Generic;
using Mgf;
using UnityEngine;

namespace Mgf.NabiJaebongso
{
    [Serializable]
    public sealed class NabiProblem
    {
        public string id;
        public int band;
        public string kind;
        public int ab;
        public int ac;
        public int bc;
        public int baseEdge;
        public int leftSourceVertex;
        public int rightSourceVertex;
        public int apexSourceVertex;
        public int expectedLeftEdge;
        public int expectedRightEdge;
        public int initialWrongCenter;
        public int initialWrongEdge;
        public int rotationDeg;
        public bool reflected;
        public string targetOrder;
        public string prompt;
        public string answer;
        public string unitConcept;

        public int EdgeLength(int edge)
        {
            return edge == 0 ? ab : edge == 1 ? ac : bc;
        }

        public string EdgeName(int edge)
        {
            return edge == 0 ? "AB" : edge == 1 ? "AC" : "BC";
        }

        public char TargetLabelForSource(int sourceVertex)
        {
            return targetOrder[Mathf.Clamp(sourceVertex, 0, 2)];
        }

        public char LeftLabel => TargetLabelForSource(leftSourceVertex);
        public char RightLabel => TargetLabelForSource(rightSourceVertex);
        public char ApexLabel => TargetLabelForSource(apexSourceVertex);
    }

    public static class NabiRules
    {
        public const int TargetWings = 6;
        public const int StartLives = 3;
        public const int NeededFirstCorrect = 5;
        public const float RunSeconds = 150f;
        public const int BankSize = 360;

        // 무작위 생성 뒤 필터링하지 않는다. 모든 원소가 처음부터 서로 다른 세 변과
        // 삼각형 부등식을 만족하는 제약 풀이다. 정수 길이만 써 중1 화면에 근호가 나오지 않는다.
        static readonly int[,] SidePool = {
            {5,6,7}, {4,6,7}, {5,7,9}, {6,7,8}, {6,8,9},
            {7,8,10}, {7,9,11}, {8,9,10}, {8,10,11}, {9,10,12},
            {5,8,10}, {6,9,10}, {7,10,12}, {8,11,13}, {9,11,13},
            {10,12,15}, {7,11,13}, {8,12,13}, {9,12,14}, {10,13,15}
        };

        static readonly string[] TargetOrders = { "DEF", "DFE", "EDF", "EFD", "FDE", "FED" };

        public static NabiProblem Practice()
        {
            return Make("practice-fixed", 0, "practice", 6, 5, 7, 0, "DEF", false, false, 0, 0);
        }

        public static List<NabiProblem> BuildBank()
        {
            var list = new List<NabiProblem>(BankSize);
            int poolCount = SidePool.GetLength(0);
            for (int i = 0; i < BankSize; i++)
            {
                int band = i / 120 + 1;
                int p = (i * 7 + band * 3) % poolCount;
                int ab = SidePool[p, 0];
                int ac = SidePool[p, 1];
                int bc = SidePool[p, 2];
                int baseEdge = band < 3 ? 0 : (i * 5 + i / 9) % 3;
                string order = TargetOrders[(i * 5 + band) % TargetOrders.Length];
                bool swapBase = ((i / 3) + band) % 2 == 1;
                bool reflected = ((i / 5) + band) % 2 == 1;
                string kind = band == 1 ? "open_construct" : band == 2 ? "error_find" : "reverse";
                list.Add(Make("nabi-" + (i + 1).ToString("D3"), band, kind, ab, ac, bc, baseEdge,
                    order, swapBase, reflected, (i * 37 + band * 19) % 360, i + 1));
            }
            string error;
            if (!Audit(list, out error)) throw new InvalidOperationException("나비 문제 은행 감사 실패: " + error);
            return list;
        }

        static NabiProblem Make(string id, int band, string kind, int ab, int ac, int bc, int baseEdge,
            string order, bool swapBase, bool reflected, int rotationDeg, int serial)
        {
            int u, v, w;
            if (baseEdge == 0) { u = 0; v = 1; w = 2; }
            else if (baseEdge == 1) { u = 0; v = 2; w = 1; }
            else { u = 1; v = 2; w = 0; }
            int left = swapBase ? v : u;
            int right = swapBase ? u : v;
            int leftEdge = EdgeBetween(left, w);
            int rightEdge = EdgeBetween(right, w);
            var p = new NabiProblem {
                id = id,
                band = band,
                kind = kind,
                ab = ab,
                ac = ac,
                bc = bc,
                baseEdge = baseEdge,
                leftSourceVertex = left,
                rightSourceVertex = right,
                apexSourceVertex = w,
                expectedLeftEdge = leftEdge,
                expectedRightEdge = rightEdge,
                initialWrongCenter = band == 2 ? (serial % 2) : -1,
                rotationDeg = rotationDeg,
                reflected = reflected,
                targetOrder = order
            };

            int expected = p.initialWrongCenter == 0 ? p.expectedLeftEdge : p.expectedRightEdge;
            p.initialWrongEdge = p.initialWrongCenter < 0 ? -1 : DifferentEdge(expected, p.baseEdge, serial);

            string congruence = "△ABC≡△" + order;
            string baseCondition = "선분 " + p.LeftLabel + p.RightLabel + "=" + p.EdgeName(baseEdge) + "이다.";
            string lengths = " AB=" + ab + " cm, AC=" + ac + " cm, BC=" + bc + " cm.";
            string prefix = band == 0 ? "고정 연습" : "재봉 주문 " + serial.ToString("D3");
            if (band == 2)
            {
                p.prompt = prefix + ": " + congruence + "이 되도록 잘못 놓인 원호를 고치고, 두 원호의 교점까지 두 선분을 이어 삼각형을 완성하시오. " + baseCondition;
                p.unitConcept = "error_find · 대응변 원호 수선";
            }
            else if (band == 3)
            {
                p.prompt = prefix + ": " + congruence + "인 삼각형을 작도하시오. " + baseCondition;
                p.unitConcept = "reverse · 꼭짓점 대응을 바꾼 SSS 작도";
            }
            else
            {
                p.prompt = prefix + ": " + congruence + "인 삼각형을 작도하시오. " + baseCondition + lengths;
                p.unitConcept = band == 0 ? "고정 연습 · 컴퍼스로 같은 길이 옮기기" : "open_construct · 세 변이 주어진 삼각형의 작도";
            }
            p.answer = p.LeftLabel + " 중심에 " + p.EdgeName(leftEdge) + ", " + p.RightLabel + " 중심에 " +
                p.EdgeName(rightEdge) + "를 옮겨 교점 " + p.ApexLabel + "를 잇는다.";
            return p;
        }

        static int DifferentEdge(int expected, int baseEdge, int salt)
        {
            int first = (expected + 1 + salt % 2) % 3;
            if (first != expected && first != baseEdge) return first;
            for (int e = 0; e < 3; e++) if (e != expected && e != baseEdge) return e;
            return (expected + 1) % 3;
        }

        public static int EdgeBetween(int a, int b)
        {
            int lo = Math.Min(a, b), hi = Math.Max(a, b);
            if (lo == 0 && hi == 1) return 0;
            if (lo == 0 && hi == 2) return 1;
            return 2;
        }

        public static string MisconceptionId(NabiProblem p, int leftEdge, int rightEdge, bool bothSegments, bool geometryOkay)
        {
            if (p == null) return "incomplete_construction";
            if (leftEdge == p.expectedRightEdge && rightEdge == p.expectedLeftEdge) return "correspondence_order";
            if (leftEdge != p.expectedLeftEdge || rightEdge != p.expectedRightEdge) return "arbitrary_edge";
            if (!bothSegments || !geometryOkay) return "one_radius_only";
            return "";
        }

        public static bool CorrectSources(NabiProblem p, int leftEdge, int rightEdge)
        {
            return p != null && leftEdge == p.expectedLeftEdge && rightEdge == p.expectedRightEdge;
        }

        public static List<NabiProblem> RunDeck(List<NabiProblem> bank, System.Random rng)
        {
            var bands = new[] { new List<NabiProblem>(), new List<NabiProblem>(), new List<NabiProblem>() };
            for (int i = 0; i < bank.Count; i++) bands[Mathf.Clamp(bank[i].band - 1, 0, 2)].Add(bank[i]);
            for (int b = 0; b < bands.Length; b++) Shuffle(bands[b], rng);
            var deck = new List<NabiProblem>(TargetWings);
            for (int b = 0; b < 3; b++) { deck.Add(bands[b][0]); deck.Add(bands[b][1]); }
            return deck;
        }

        static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }

        public static List<MgfProblem> ToMgfBank(List<NabiProblem> bank)
        {
            var result = new List<MgfProblem>(bank.Count);
            for (int i = 0; i < bank.Count; i++)
            {
                NabiProblem p = bank[i];
                result.Add(new MgfProblem {
                    id = p.id,
                    prompt = p.prompt,
                    choices = null,
                    answer = p.answer,
                    answerNumeric = double.NaN,
                    unitConcept = p.unitConcept
                });
            }
            return result;
        }

        public static bool Audit(IList<NabiProblem> bank, out string error)
        {
            error = "";
            if (bank == null || bank.Count < 300) { error = "문항 수 300 미만"; return false; }
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < bank.Count; i++)
            {
                NabiProblem p = bank[i];
                if (!ids.Add(p.id)) { error = "중복 id " + p.id; return false; }
                if (!prompts.Add(p.prompt)) { error = "중복 발문 " + p.id; return false; }
                if (p.ab == p.ac || p.ab == p.bc || p.ac == p.bc) { error = "부등변 위반 " + p.id; return false; }
                int max = Math.Max(p.ab, Math.Max(p.ac, p.bc));
                if (p.ab + p.ac + p.bc - max <= max) { error = "삼각형 부등식 위반 " + p.id; return false; }
                if (p.expectedLeftEdge == p.expectedRightEdge) { error = "두 반지름 중복 " + p.id; return false; }
                if (p.initialWrongCenter >= 0)
                {
                    int expected = p.initialWrongCenter == 0 ? p.expectedLeftEdge : p.expectedRightEdge;
                    if (p.initialWrongEdge == expected) { error = "오개념 우연 정답 " + p.id; return false; }
                }
                // 화면 좌우를 따른 교환 오개념은 부등변 풀에서 언제나 정답과 다르다.
                if (CorrectSources(p, p.expectedRightEdge, p.expectedLeftEdge)) { error = "대응 교환 충돌 " + p.id; return false; }
            }
            return true;
        }
    }
}
