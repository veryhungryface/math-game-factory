// 빗변 철길 — 실제 플레이와 QA 문제 은행이 함께 쓰는 정수 피타고라스 생성기.
// 중2 표현 함정: 길이 답은 자연수 피타고라스 수만 사용하고, 풀이에는 근호(√)를 쓰지 않는다.
// 판별할 때에는 가장 긴 변을 빗변으로 두며, 역산은 제곱의 합이 아니라 제곱의 차를 쓴다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.BitbyeonCheolgil
{
    public enum RailMode { Hypotenuse, MissingLeg }

    public sealed class RailProblem
    {
        public string id;
        public int a, b, c;
        public int knownLeg;
        public int answer;
        public int band;
        public RailMode mode;
        public string prompt;
        public string reveal;
        public string unitConcept;
    }

    public static class BitbyeonRules
    {
        public const int MinTick = 4;
        public const int MaxTick = 25;
        public const int SessionCount = 9;

        // 답 눈금은 4~25에 머물되, 알려 주는 다른 변은 더 길 수 있다. 따라서
        // 실제 래칫 판정 경로를 그대로 쓰면서도 30개의 서로 다른 정수 튜플을 다룬다.
        static readonly int[,] Triples =
        {
            { 3, 4, 5 }, { 5, 12, 13 }, { 6, 8, 10 }, { 7, 24, 25 }, { 8, 15, 17 },
            { 9, 12, 15 }, { 9, 40, 41 }, { 10, 24, 26 }, { 11, 60, 61 }, { 12, 16, 20 },
            { 12, 35, 37 }, { 13, 84, 85 }, { 14, 48, 50 }, { 15, 20, 25 }, { 15, 36, 39 },
            { 15, 112, 113 }, { 16, 30, 34 }, { 16, 63, 65 }, { 18, 24, 30 }, { 18, 80, 82 },
            { 20, 21, 29 }, { 20, 48, 52 }, { 20, 99, 101 }, { 21, 28, 35 }, { 21, 72, 75 },
            { 22, 120, 122 }, { 24, 32, 40 }, { 24, 45, 51 }, { 24, 70, 74 }, { 25, 60, 65 }
        };

        public static List<RailProblem> BuildSession()
        {
            var p = new List<RailProblem>(SessionCount)
            {
                Make("r01", 3, 4, 5, RailMode.Hypotenuse, 0, 1, 0),
                Make("r02", 6, 8, 10, RailMode.Hypotenuse, 0, 1, 1),
                Make("r03", 9, 12, 15, RailMode.Hypotenuse, 0, 1, 3),
                Make("r04", 5, 12, 13, RailMode.Hypotenuse, 0, 2, 4),
                Make("r05", 8, 15, 17, RailMode.Hypotenuse, 0, 2, 6),
                Make("r06", 7, 24, 25, RailMode.Hypotenuse, 0, 2, 7),
                Make("r07", 5, 12, 13, RailMode.MissingLeg, 5, 3, 2),
                Make("r08", 8, 15, 17, RailMode.MissingLeg, 15, 3, 5),
                Make("r09", 7, 24, 25, RailMode.MissingLeg, 7, 3, 8)
            };
            ValidateCatalog(p);
            return p;
        }

        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(360);
            var generated = new List<RailProblem>(360);
            // 30개 수치 튜플 × 12개 실제 과업 문맥. 번호나 지명 접두부를 지워도
            // 360개 핵심 문장이 모두 다르며, 모든 답은 실제 4~25 래칫으로 판정한다.
            for (int i = 0; i < 360; i++)
            {
                int row = i % Triples.GetLength(0);
                int a = Triples[row, 0], b = Triples[row, 1], c = Triples[row, 2];
                int style = i / Triples.GetLength(0);
                RailMode mode = c <= MaxTick && style % 3 != 2 ? RailMode.Hypotenuse : RailMode.MissingLeg;
                int known = mode == RailMode.MissingLeg ? (style % 2 == 0 ? a : b) : 0;
                int band = i < 120 ? 1 : i < 240 ? 2 : 3;
                RailProblem p = Make("bank-" + (i + 1).ToString("000"), a, b, c, mode, known, band, style);
                generated.Add(p);
                bank.Add(new MgfProblem
                {
                    id = p.id,
                    prompt = p.prompt,
                    choices = TickChoices(),
                    answer = p.answer.ToString(),
                    answerNumeric = p.answer,
                    unitConcept = p.unitConcept
                });
            }
            ValidateCatalog(generated);
            return bank;
        }

        static RailProblem Make(string id, int a, int b, int c, RailMode mode, int knownLeg, int band, int style)
        {
            if (mode == RailMode.MissingLeg)
            {
                int missing = knownLeg == a ? b : a;
                if (missing < MinTick || missing > MaxTick) knownLeg = knownLeg == a ? b : a;
            }
            var p = new RailProblem { id = id, a = a, b = b, c = c, mode = mode, knownLeg = knownLeg, band = band };
            if (mode == RailMode.Hypotenuse)
            {
                p.answer = c;
                p.prompt = HypotenusePrompt(style, a, b, c);
                p.reveal = a + "<sup>2</sup> + " + b + "<sup>2</sup> = " + (a * a) + " + " + (b * b) + " = " + (c * c) + " = " + c + "<sup>2</sup>";
                p.unitConcept = "피타고라스 정리로 빗변의 길이 구하기";
            }
            else
            {
                int missing = knownLeg == a ? b : a;
                p.answer = missing;
                p.prompt = MissingLegPrompt(style, knownLeg, missing, c);
                p.reveal = c + "<sup>2</sup> − " + knownLeg + "<sup>2</sup> = " + (c * c) + " − " + (knownLeg * knownLeg) + " = " + (missing * missing) + " = " + missing + "<sup>2</sup>";
                p.unitConcept = "피타고라스 정리로 다른 한 변의 길이 구하기";
            }
            return p;
        }

        static string HypotenusePrompt(int style, int a, int b, int c)
        {
            int s = style % 12;
            if (s == 0) return "직각을 낀 두 변이 " + a + " m, " + b + " m인 삼각형의 빗변 길이를 구하시오.";
            if (s == 1) return "가로 " + b + " m, 세로 " + a + " m인 신호판을 가로지르는 대각 지지대의 길이를 구하시오.";
            if (s == 2) return a + "<sup>2</sup> + " + b + "<sup>2</sup> = □<sup>2</sup>일 때 양수 □를 구하시오.";
            if (s == 3) return "직각으로 만나는 두 통로의 길이가 " + a + " m와 " + b + " m이다. 두 끝을 잇는 최단 케이블 길이를 구하시오.";
            if (s == 4) return "두 직각변 위 정사각형의 넓이가 " + (a * a) + " m², " + (b * b) + " m²일 때 빗변의 길이를 구하시오.";
            if (s == 5) return "높이 " + a + " m인 릴레이 탑에서 바닥의 " + b + " m 떨어진 점까지 잇는 와이어 길이를 구하시오.";
            if (s == 6) return "직각삼각형의 두 짧은 변 " + a + " m, " + b + " m에 맞는 가장 긴 변을 구성하시오.";
            if (s == 7) return "∠C=90°이고 AC=" + a + " m, BC=" + b + " m인 △ABC에서 AB의 길이를 구하시오.";
            if (s == 8) return "점검식 " + a + "²+" + b + "²=" + (c * c) + "을 만족하는 대각선 케이블 길이를 구하시오.";
            if (s == 9) return "서로 수직인 레일을 " + a + " m와 " + b + " m 이동했다. 출발점과 도착점 사이의 거리를 구하시오.";
            if (s == 10) return "밑변 " + b + " m, 높이 " + a + " m인 직각삼각형 노선의 빗변 신호선을 구하시오.";
            return "직각 모서리 양쪽의 거리 " + a + " m, " + b + " m를 이용해 대각 릴레이 봉의 길이를 구하시오.";
        }

        static string MissingLegPrompt(int style, int known, int missing, int c)
        {
            int s = style % 12;
            if (s == 0) return "빗변이 " + c + " m이고 한 직각변이 " + known + " m일 때 나머지 한 변의 길이를 구하시오.";
            if (s == 1) return "대각 지지대 " + c + " m와 세로 간격 " + known + " m가 주어졌다. 가로 간격을 구하시오.";
            if (s == 2) return c + "<sup>2</sup> − " + known + "<sup>2</sup> = □<sup>2</sup>일 때 양수 □를 구하시오.";
            if (s == 3) return "길이 " + c + " m인 케이블의 한쪽 직각 이동이 " + known + " m이다. 다른 쪽 이동 거리를 구하시오.";
            if (s == 4) return "빗변 위 정사각형 넓이가 " + (c * c) + " m², 한 변 위 넓이가 " + (known * known) + " m²일 때 남은 변의 길이를 구하시오.";
            if (s == 5) return "길이 " + c + " m인 사다리의 아래쪽 끝이 벽에서 " + known + " m 떨어져 있다. 벽을 따라 닿은 높이를 구하시오. (단, 벽은 바닥과 수직이다.)";
            if (s == 6) return "직각삼각형의 빗변이 " + c + " m이고 한 직각변이 " + known + " m일 때 나머지 직각변의 길이를 구하시오.";
            if (s == 7) return "∠C=90°이고 AB=" + c + " m, AC=" + known + " m인 △ABC에서 BC의 길이를 구하시오.";
            if (s == 8) return "점검식 □²+" + known + "²=" + (c * c) + "을 만족하는 양수 □를 구하시오.";
            if (s == 9) return "출발점과 도착점의 직선거리가 " + c + " m이고 수직 이동이 " + known + " m이다. 수평 이동을 구하시오.";
            if (s == 10) return "빗변 신호선 " + c + " m와 높이 " + known + " m가 주어진 직각삼각형 노선의 밑변을 구하시오.";
            return "대각 릴레이 봉 " + c + " m와 한쪽 거리 " + known + " m를 이용해 다른 직각변의 길이를 구하시오.";
        }

        static string[] TickChoices()
        {
            var result = new string[MaxTick - MinTick + 1];
            for (int i = 0; i < result.Length; i++) result[i] = (MinTick + i).ToString();
            return result;
        }

        public static bool IsCorrect(RailProblem p, int tick)
        {
            if (tick != p.answer) return false;
            if (p.mode == RailMode.Hypotenuse) return p.a * p.a + p.b * p.b == tick * tick;
            int known = p.knownLeg;
            return tick * tick + known * known == p.c * p.c;
        }

        // 오답은 수치 우연이 아니라 대표 오개념에서 역산한다.
        // misconceptionId 1: sum-without-squares — a+b=c로 착각.
        // misconceptionId 2: approximate-square-gap — 제곱 차 1~4를 같다고 어림.
        // misconceptionId 3: hypotenuse-not-longest — 가장 긴 변을 빗변으로 확인하지 않음.
        public static string Diagnose(RailProblem p, int tick)
        {
            if (p.mode == RailMode.Hypotenuse && tick == p.a + p.b) return "sum-without-squares";
            if (p.mode == RailMode.MissingLeg && tick == p.c + p.knownLeg) return "sum-instead-of-difference";
            int lhs = p.mode == RailMode.Hypotenuse ? p.a * p.a + p.b * p.b : tick * tick + p.knownLeg * p.knownLeg;
            int rhs = p.mode == RailMode.Hypotenuse ? tick * tick : p.c * p.c;
            int gap = Math.Abs(lhs - rhs);
            if (gap >= 1 && gap <= 4) return "approximate-square-gap";
            if (p.mode == RailMode.Hypotenuse && tick <= Math.Max(p.a, p.b)) return "hypotenuse-not-longest";
            return tick < p.answer ? "cable-too-short" : "cable-too-long";
        }

        public static int SquareGap(RailProblem p, int tick)
        {
            int lhs = p.mode == RailMode.Hypotenuse ? p.a * p.a + p.b * p.b : tick * tick + p.knownLeg * p.knownLeg;
            int rhs = p.mode == RailMode.Hypotenuse ? tick * tick : p.c * p.c;
            return Math.Abs(lhs - rhs);
        }

        public static int GuaranteedWrong(RailProblem p)
        {
            int sum = p.mode == RailMode.Hypotenuse ? p.a + p.b : p.c + p.knownLeg;
            if (sum >= MinTick && sum <= MaxTick && sum != p.answer) return sum;
            return p.answer == MinTick ? MinTick + 1 : MinTick;
        }

        public static void ValidateCatalog(IList<RailProblem> list)
        {
            foreach (RailProblem p in list)
            {
                if (p.c <= p.a || p.c <= p.b || p.a * p.a + p.b * p.b != p.c * p.c)
                    throw new InvalidOperationException("피타고라스 튜플 오류: " + p.id);
                if (p.answer < MinTick || p.answer > MaxTick || !IsCorrect(p, p.answer))
                    throw new InvalidOperationException("정답 범위/판정 오류: " + p.id);
                int wrong = GuaranteedWrong(p);
                if (wrong == p.answer || IsCorrect(p, wrong))
                    throw new InvalidOperationException("오개념 오답이 우연히 정답: " + p.id);
            }
        }
    }
}
