// 현악 네모 — 사각형 대각선 성질을 두 꼭짓점 배치로 구성하는 정수 규칙.
// 표현 함정(2022 중학교 수학 정본): 정사각형은 직사각형·마름모에 포함된다.
// 아직 B-O-D가 일직선이 아닐 때 O를 교점이라 부르지 않으며, 발문은 항상
// "O가 두 대각선의 교점이 되도록"이라고 쓴다. 길이는 정수, 방향은 30° 인덱스다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.HyeonakNemo
{
    public enum FrameKind { Practice, Parallelogram, Rectangle, Rhombus, Square }

    [Serializable]
    public sealed class FrameProblem
    {
        public string id;
        public FrameKind kind;
        public int band;
        public int a;
        public int b;
        public int baseK;
        public bool reverse;
        public string prompt;
        public string concept;
        public string answer;
        public string misconceptionId;
    }

    public static class HyeonakRules
    {
        public const int StartLives = 3;
        public const int TargetSolved = 6;

        public static FrameProblem Practice()
        {
            return new FrameProblem {
                id = "practice-fixed", kind = FrameKind.Practice, band = 0, a = 4, b = 7, baseK = 0,
                prompt = "AO=4 cm, BO=7 cm이다. O가 두 대각선의 교점이 되도록 C와 D를 옮기시오.",
                concept = "평행사변형의 두 대각선은 서로 다른 것을 이등분한다",
                answer = "OC=4 cm, OD=7 cm, C와 D는 각각 반대 방향",
                misconceptionId = "parallelogram_diagonals_equal"
            };
        }

        public static List<FrameProblem> BuildBank()
        {
            var list = new List<FrameProblem>(300);
            int id = 1;

            // Band 1: a!=b인 90쌍 × direct/reverse = 180. 정답은 정확히 (c=a,d=b) 한 쌍.
            for (int a = 1; a <= 10; a++)
            for (int b = 1; b <= 10; b++)
            {
                if (a == b) continue;
                for (int rev = 0; rev < 2; rev++)
                {
                    bool reverse = rev == 1;
                    string given = reverse
                        ? $"AC={a * 2} cm, BD={b * 2} cm이고 O는 각 대각선의 중점이다."
                        : $"AO={a} cm, BO={b} cm이다.";
                    list.Add(new FrameProblem {
                        id = "hn-" + id++, kind = FrameKind.Parallelogram, band = 1, a = a, b = b,
                        baseK = ((a * 3 + b * 5 + rev) % 2) * 1, reverse = reverse,
                        prompt = given + " O가 두 대각선의 교점인 평행사변형 ABCD를 만드시오.",
                        concept = reverse ? "전체 대각선 길이에서 반쪽 길이를 구성" : "평행사변형의 대각선 이등분",
                        answer = $"OC={a} cm, OD={b} cm",
                        misconceptionId = "parallelogram_diagonals_equal"
                    });
                }
            }

            // Band 2: a 10개 × 두 발문형 = 20. 방향 5쌍이 모두 정답이며 수직 정사각형도 포함된다.
            for (int a = 1; a <= 10; a++)
            for (int rev = 0; rev < 2; rev++)
            {
                bool reverse = rev == 1;
                string given = reverse ? $"AC={a * 2} cm이고 AO=OC이다." : $"AO=OC={a} cm이다.";
                list.Add(new FrameProblem {
                    id = "hn-" + id++, kind = FrameKind.Rectangle, band = 2, a = a, b = a,
                    baseK = (a + rev * 2) % 12, reverse = reverse,
                    prompt = given + " O가 두 대각선의 교점인 직사각형 ABCD를 만드시오.",
                    concept = reverse ? "직사각형의 같은 대각선 전체 길이" : "직사각형의 대각선은 길이가 같고 서로 다른 것을 이등분",
                    answer = $"OB=OD={a} cm, B와 D는 반대 방향(수직인 정사각형도 정답)",
                    misconceptionId = "equal_length_without_bisection"
                });
            }

            // Band 3: 마름모 90 + 정사각형 10 = 100.
            for (int a = 1; a <= 10; a++)
            for (int b = 1; b <= 10; b++)
            {
                if (a == b) continue;
                list.Add(new FrameProblem {
                    id = "hn-" + id++, kind = FrameKind.Rhombus, band = 3, a = a, b = b,
                    baseK = (a * 7 + b * 3) % 12,
                    prompt = $"AO=OC={a} cm, BD={b * 2} cm이다. O가 두 대각선의 교점인 마름모 ABCD를 만드시오.",
                    concept = "마름모의 두 대각선은 서로 다른 것을 수직이등분",
                    answer = $"OB=OD={b} cm, AC와 BD는 수직",
                    misconceptionId = "perpendicular_without_bisection"
                });
            }
            for (int a = 1; a <= 10; a++)
            {
                list.Add(new FrameProblem {
                    id = "hn-" + id++, kind = FrameKind.Square, band = 3, a = a, b = a,
                    baseK = (a * 5) % 12,
                    prompt = $"AC={a * 2} cm이고 AO=OC이다. O가 두 대각선의 교점인 정사각형 ABCD를 만드시오.",
                    concept = "정사각형의 두 대각선은 길이가 같고 서로 다른 것을 수직이등분",
                    answer = $"OB=OD={a} cm, AC와 BD는 수직",
                    misconceptionId = "square_excluded_from_rectangle_rhombus"
                });
            }
            return list;
        }

        public static bool IsAllowedDirection(bool isB, int k)
        {
            k = Mod12(k);
            return isB ? k >= 1 && k <= 5 : k >= 7 && k <= 11;
        }

        public static bool IsCorrect(FrameProblem p, int rB, int kB, int rD, int kD)
        {
            kB = Mod12(kB - p.baseK);
            kD = Mod12(kD - p.baseK);
            bool opposite = Mod12(kD - kB) == 6;
            if (p.kind == FrameKind.Practice || p.kind == FrameKind.Parallelogram)
                return rB == p.a && rD == p.b; // 여기서 B/D는 이동하는 C/D 반쪽 길이 역할이다.
            if (p.kind == FrameKind.Rectangle)
                return rB == p.a && rD == p.a && opposite;
            bool perpendicular = kB == 3 && kD == 9;
            if (p.kind == FrameKind.Rhombus)
                return rB == p.b && rD == p.b && opposite && perpendicular;
            return rB == p.a && rD == p.a && opposite && perpendicular;
        }

        public static string Diagnose(FrameProblem p, int rB, int kB, int rD, int kD)
        {
            int kb = Mod12(kB - p.baseK), kd = Mod12(kD - p.baseK);
            bool opposite = Mod12(kd - kb) == 6;
            if (p.kind == FrameKind.Practice || p.kind == FrameKind.Parallelogram)
            {
                if (rB == p.a && rD == p.a && p.a != p.b) return "parallelogram_diagonals_equal";
                if (rB != p.a) return "first_half_not_equal";
                return "second_half_not_equal";
            }
            if (!opposite) return "equal_length_without_bisection";
            if (p.kind == FrameKind.Rectangle)
                return (rB != p.a || rD != p.a) ? "diagonals_not_equal" : "";
            if (kb == 3 && kd == 9 && rB + rD == p.b * 2 && rB != rD)
                return "perpendicular_without_bisection";
            if (kb != 3 || kd != 9) return "not_perpendicular";
            return "halves_not_equal";
        }

        public static int CountSolutions(FrameProblem p)
        {
            int count = 0;
            if (p.kind == FrameKind.Practice || p.kind == FrameKind.Parallelogram)
            {
                for (int rb = 1; rb <= 10; rb++)
                for (int rd = 1; rd <= 10; rd++)
                    if (IsCorrect(p, rb, p.baseK + 3, rd, p.baseK + 9)) count++;
                return count;
            }
            for (int kb = 1; kb <= 5; kb++)
            for (int rb = 1; rb <= 10; rb++)
            for (int kd = 7; kd <= 11; kd++)
            for (int rd = 1; rd <= 10; rd++)
                if (IsCorrect(p, rb, p.baseK + kb, rd, p.baseK + kd)) count++;
            return count;
        }

        public static MgfProblem ToMgf(FrameProblem p)
        {
            return new MgfProblem {
                id = p.id,
                prompt = p.prompt,
                choices = null,
                answer = p.answer,
                answerNumeric = p.kind == FrameKind.Parallelogram ? p.a * 100 + p.b : p.b,
                unitConcept = p.concept
            };
        }

        public static int Mod12(int x) { x %= 12; return x < 0 ? x + 12 : x; }
    }
}
