// 다리 놓는 수달 — 피타고라스 정리 문제 모델, 제약 만족 풀, 정수 판정.
//
// curriculum/2022-middle-math.json expression_traps:
// - 중2에는 제곱근(√)이 없다. 모든 길이는 피타고라스 수 또는 그 자연수 배에서 뽑는다.
// - 발문은 「~을/를 구하시오」 지시형, 수와 단위는 띄어 쓴다(6 cm).
// - 피타고라스 정리의 역은 세 변을 정렬하여 가장 긴 변을 c로 두고 정수식 a²+b²=c²로 판정한다.
// - 정당화에 「가정」「결론」을 쓰지 않는다. 어림 표현은 만들지 않는다.
//
// 오개념 역산(misconceptionId):
// - sum_without_squares: a+b=c로 제곱을 빼먹은 길이.
// - add_instead_of_subtract: 빗변과 한 변이 주어졌는데 c²-a²가 아니라 c²+a²에 가까운 제곱수를 쓴 길이.
// - apply_to_non_right: 비직각 시작 프레임을 그대로 잠근 경우.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.SudalDari
{
    public enum BridgeKind { Hypotenuse, MissingLeg, CorrectFrame }

    [Serializable]
    public sealed class BridgeProblem
    {
        public string id;
        public BridgeKind kind;
        public int a, b, c;
        public int shownLeg;
        public int startLongest;
        public int answer;
        public int band;
        public string prompt;
        public string formula;
        public string unitConcept;
        public int wrongSum;
        public int wrongAddSquares;
    }

    public static class SudalRules
    {
        public const int MinLength = 1;
        public const int MaxLength = 25;
        public const int TargetBridges = 8;
        public const int StartLives = 3;
        public const float TideSeconds = 25f;

        // 무작위 생성 후 필터링하지 않는다. 이 정본 풀의 모든 원소는 자연수 피타고라스 수이고 c<=25다.
        static readonly int[,] Triples = {
            {3,4,5}, {6,8,10}, {5,12,13}, {9,12,15},
            {8,15,17}, {12,16,20}, {15,20,25}, {7,24,25}
        };

        static readonly string[] Places = {
            "버드나무 둑", "물레방아 둑", "도토리 둑", "갈대 둑", "돌담 둑", "등불 둑",
            "새벽 둑", "복숭아 둑", "바람개비 둑", "수련 둑", "구름 둑", "밤나무 둑",
            "노을 둑", "솔방울 둑", "징검돌 둑", "종이배 둑"
        };

        static readonly string[] LengthChoices = BuildChoices();

        static string[] BuildChoices()
        {
            var a = new string[MaxLength];
            for (int i = 0; i < a.Length; i++) a[i] = (i + 1) + " cm";
            return a;
        }

        public static bool IsRight(int a, int b, int c)
        {
            int x = a, y = b, z = c;
            if (x > y) { int t = x; x = y; y = t; }
            if (y > z) { int t = y; y = z; z = t; }
            if (x > y) { int t = x; x = y; y = t; }
            return x > 0 && (long)x * x + (long)y * y == (long)z * z;
        }

        public static bool IsTriangle(int a, int b, int c)
        {
            int x = a, y = b, z = c;
            if (x > y) { int t = x; x = y; y = t; }
            if (y > z) { int t = y; y = z; z = t; }
            if (x > y) { int t = x; x = y; y = t; }
            return x > 0 && x + y > z;
        }

        static int ClosestRoot(int n)
        {
            int best = 1, bestGap = Math.Abs(n - 1);
            for (int x = 2; x <= MaxLength; x++)
            {
                int gap = Math.Abs(n - x * x);
                if (gap < bestGap) { best = x; bestGap = gap; }
            }
            return best;
        }

        public static BridgeProblem Practice()
        {
            return MakeHyp(3, 4, 5, 0, "연습 둑", "practice");
        }

        static BridgeProblem MakeHyp(int a, int b, int c, int band, string place, string suffix)
        {
            return new BridgeProblem {
                id = "sd-h-" + suffix, kind = BridgeKind.Hypotenuse, a = a, b = b, c = c,
                answer = c, band = band,
                prompt = place + "에서 직각을 낀 두 변의 길이가 " + a + " cm, " + b + " cm인 직각삼각형의 빗변 길이를 구하시오.",
                formula = a + "² + " + b + "² = " + (a*a) + " + " + (b*b) + " = " + (c*c) + " = " + c + "²",
                unitConcept = band <= 1 ? "정사각형의 넓이로 보는 피타고라스 정리" : "빗변의 길이 구하기",
                wrongSum = a + b <= MaxLength && a + b != c ? a + b : 0
            };
        }

        static BridgeProblem MakeLeg(int a, int b, int c, bool showA, string place, string suffix)
        {
            int shown = showA ? a : b;
            int answer = showA ? b : a;
            int wrong = ClosestRoot(c*c + shown*shown);
            if (wrong == answer) wrong = Math.Min(MaxLength, wrong + 1);
            return new BridgeProblem {
                id = "sd-l-" + suffix, kind = BridgeKind.MissingLeg, a = a, b = b, c = c,
                shownLeg = shown, answer = answer, band = 3,
                prompt = place + "에서 빗변의 길이가 " + c + " cm이고 다른 한 변의 길이가 " + shown + " cm인 직각삼각형의 나머지 한 변 길이를 구하시오.",
                formula = c + "² − " + shown + "² = " + (c*c) + " − " + (shown*shown) + " = " + (answer*answer) + " = " + answer + "²",
                unitConcept = "빗변과 다른 한 변으로 나머지 한 변 구하기",
                wrongAddSquares = wrong
            };
        }

        static BridgeProblem MakeCorrection(int a, int b, int c, int start, string place, string suffix)
        {
            return new BridgeProblem {
                id = "sd-r-" + suffix, kind = BridgeKind.CorrectFrame, a = a, b = b, c = c,
                startLongest = start, answer = c, band = 3,
                prompt = place + "의 세 변 중 두 변이 " + a + " cm, " + b + " cm이다. 가장 긴 변을 조정하여 직각삼각형을 만드시오.",
                formula = a + "² + " + b + "² = " + (a*a + b*b) + " = " + c + "²  →  가장 긴 변 " + c + " cm",
                unitConcept = "피타고라스 정리의 역으로 직각삼각형 만들기"
            };
        }

        // 난도 밴드를 순서대로 소진한다. 첫 3개는 넓이 격자, 다음 3개는 밀물, 마지막 2개는 역산/역이다.
        public static List<BridgeProblem> RunDeck(Random rng)
        {
            var result = new List<BridgeProblem>(TargetBridges) {
                MakeHyp(3,4,5,1,"버드나무 둑","run-0"),
                MakeHyp(6,8,10,1,"물레방아 둑","run-1"),
                MakeHyp(5,12,13,1,"도토리 둑","run-2")
            };
            int[][] mid = {
                new[]{8,15,17}, new[]{9,12,15}, new[]{12,16,20}, new[]{7,24,25}
            };
            for (int i = mid.Length - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = mid[i]; mid[i] = mid[j]; mid[j] = t; }
            for (int i = 0; i < 3; i++) result.Add(MakeHyp(mid[i][0], mid[i][1], mid[i][2], 2, Places[3+i], "run-" + (3+i)));
            if (rng.Next(2) == 0) result.Add(MakeLeg(5,12,13,true,"수련 둑","run-6"));
            else result.Add(MakeLeg(8,15,17,true,"수련 둑","run-6"));
            int pick = rng.Next(3);
            if (pick == 0) result.Add(MakeCorrection(6,8,10,11,"등불 둑","run-7"));
            else if (pick == 1) result.Add(MakeCorrection(5,12,13,12,"등불 둑","run-7"));
            else result.Add(MakeCorrection(8,15,17,16,"등불 둑","run-7"));
            return result;
        }

        public static string MisconceptionId(BridgeProblem p, int selected)
        {
            if (p == null || selected == p.answer) return "";
            if (p.kind == BridgeKind.Hypotenuse && p.wrongSum > 0 && selected == p.wrongSum) return "sum_without_squares";
            if (p.kind == BridgeKind.MissingLeg && p.wrongAddSquares > 0 && selected == p.wrongAddSquares) return "add_instead_of_subtract";
            if (p.kind == BridgeKind.CorrectFrame && selected == p.startLongest) return "apply_to_non_right";
            return selected < p.answer ? "bridge_too_short" : "bridge_too_long";
        }

        public static List<MgfProblem> BuildBank()
        {
            var result = new List<MgfProblem>(384);
            for (int t = 0; t < Triples.GetLength(0); t++)
            {
                int a = Triples[t,0], b = Triples[t,1], c = Triples[t,2];
                for (int v = 0; v < Places.Length; v++)
                {
                    string s = "bank-" + t + "-" + v;
                    var h = MakeHyp(a,b,c, t < 3 ? 1 : 2, Places[v], s);
                    AddBank(result, h);
                    AddBank(result, MakeLeg(a,b,c, (v & 1) == 0, Places[v], s));
                    int start = c == 25 ? 23 + (v & 1) : Math.Min(25, c + ((v & 1) == 0 ? 1 : -1));
                    if (!IsTriangle(a,b,start)) start = c - 1;
                    AddBank(result, MakeCorrection(a,b,c,start,Places[v],s));
                }
            }
            return result;
        }

        static void AddBank(List<MgfProblem> bank, BridgeProblem p)
        {
            bank.Add(new MgfProblem {
                id = p.id,
                prompt = p.prompt,
                choices = (string[])LengthChoices.Clone(),
                answer = p.answer + " cm",
                answerNumeric = p.answer,
                unitConcept = p.unitConcept
            });
        }
    }
}
