// 한 입만 — 부채꼴 문제 은행과 정수 판정 정본.
// 중학교 style_guide: 문제 발문은 지시형 「~을 자르시오 / ~이 되도록 자르시오」로 통일한다.
// expression_traps: 수와 단위를 띄고(6 cm, 6π cm²), π를 기호로 유지한다. 근호·삼각비·현 길이 공식은 만들지 않는다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.HanIpman
{
    public enum CakeOrderKind { Arc, Area, LeaveArc, LeaveArea }

    [Serializable]
    public struct IntFraction
    {
        public int n;
        public int d;

        public IntFraction(int numerator, int denominator)
        {
            if (denominator == 0) throw new ArgumentException("분모는 0일 수 없습니다.");
            if (denominator < 0) { numerator = -numerator; denominator = -denominator; }
            int g = Gcd(Math.Abs(numerator), denominator);
            n = numerator / g;
            d = denominator / g;
        }

        static int Gcd(int a, int b)
        {
            while (b != 0) { int t = a % b; a = b; b = t; }
            return Math.Max(1, a);
        }

        public static bool Equal(IntFraction a, IntFraction b) => a.n * b.d == b.n * a.d;
    }

    [Serializable]
    public sealed class CakeProblem
    {
        public string id;
        public CakeOrderKind kind;
        public int radius;
        public int qAnswer;
        public IntFraction targetPi;
        public string prompt;
        public string answer;
        public string unitConcept;
        public int band;
    }

    public static class HanIpmanRules
    {
        public const int StartLives = 3;
        public const int TargetSolved = 8;
        public const int MasteryFirstCorrect = 6;
        public const float SessionSeconds = 120f;
        public static readonly string[] AngleChoices = BuildAngleChoices();

        static string[] BuildAngleChoices()
        {
            var a = new string[23];
            for (int q = 1; q <= 23; q++) a[q - 1] = (q * 15) + "°";
            return a;
        }

        public static IntFraction CutArc(int r, int q) => new IntFraction(r * q, 12);
        public static IntFraction CutArea(int r, int q) => new IntFraction(r * r * q, 24);
        public static IntFraction LeaveArc(int r, int q) => new IntFraction(r * (24 - q), 12);
        public static IntFraction LeaveArea(int r, int q) => new IntFraction(r * r * (24 - q), 24);

        static IntFraction Target(CakeOrderKind kind, int r, int q)
        {
            switch (kind)
            {
                case CakeOrderKind.Arc: return CutArc(r, q);
                case CakeOrderKind.Area: return CutArea(r, q);
                case CakeOrderKind.LeaveArc: return LeaveArc(r, q);
                default: return LeaveArea(r, q);
            }
        }

        public static string PiText(IntFraction f, bool square)
        {
            string unit = square ? " cm²" : " cm";
            if (f.d == 1)
            {
                if (f.n == 1) return "π" + unit;
                return f.n + "π" + unit;
            }
            return f.n + "π/" + f.d + unit;
        }

        public static string FractionRich(IntFraction f, bool square)
        {
            string unit = square ? " cm²" : " cm";
            if (f.d == 1) return (f.n == 1 ? "π" : f.n + "π") + unit;
            return "<size=80%><sup>" + f.n + "π</sup><line-height=45%>\n<voffset=0.08em>━━</voffset>\n<sup>" + f.d + "</sup></size>" + unit;
        }

        public static CakeProblem Make(CakeOrderKind kind, int r, int q, string prefix = "hip")
        {
            if (q < 1 || q > 23) throw new ArgumentOutOfRangeException(nameof(q));
            bool square = kind == CakeOrderKind.Area || kind == CakeOrderKind.LeaveArea;
            var target = Target(kind, r, q);
            string quantity = PiText(target, square);
            string prompt;
            string concept;
            switch (kind)
            {
                case CakeOrderKind.Arc:
                    prompt = "반지름이 " + r + " cm인 원에서 호의 길이가 " + quantity + "인 부채꼴을 자르시오.";
                    concept = "호의 길이와 중심각의 관계";
                    break;
                case CakeOrderKind.Area:
                    prompt = "반지름이 " + r + " cm인 원에서 넓이가 " + quantity + "인 부채꼴을 자르시오.";
                    concept = "부채꼴의 넓이와 중심각의 관계";
                    break;
                case CakeOrderKind.LeaveArc:
                    prompt = "반지름이 " + r + " cm인 원에서 남은 호의 길이가 " + quantity + "가 되도록 자르시오.";
                    concept = "전체 호와 남은 호에서 중심각 역추적";
                    break;
                default:
                    prompt = "반지름이 " + r + " cm인 원에서 남은 부분의 넓이가 " + quantity + "가 되도록 자르시오.";
                    concept = "전체 넓이와 남은 넓이에서 중심각 역추적";
                    break;
            }
            int band = kind == CakeOrderKind.Arc ? 1 : (kind == CakeOrderKind.Area ? 2 : 3);
            return new CakeProblem {
                id = prefix + "-" + KindId(kind) + "-r" + r + "-q" + q,
                kind = kind, radius = r, qAnswer = q, targetPi = target,
                prompt = prompt, answer = (q * 15) + "°", unitConcept = concept, band = band
            };
        }

        static string KindId(CakeOrderKind kind)
        {
            switch (kind)
            {
                case CakeOrderKind.Arc: return "arc";
                case CakeOrderKind.Area: return "area";
                case CakeOrderKind.LeaveArc: return "leave-arc";
                default: return "leave-area";
            }
        }

        public static CakeProblem Practice() => Make(CakeOrderKind.Arc, 12, 6, "practice");

        public static List<CakeProblem> BuildProblemPool()
        {
            var pool = new List<CakeProblem>(322);
            int[] arcR = { 12, 24 };
            int[] allR = { 6, 12, 18, 24 };
            for (int i = 0; i < arcR.Length; i++)
                for (int q = 1; q <= 23; q++) pool.Add(Make(CakeOrderKind.Arc, arcR[i], q));
            for (int i = 0; i < allR.Length; i++)
                for (int q = 1; q <= 23; q++) pool.Add(Make(CakeOrderKind.Area, allR[i], q));
            for (int i = 0; i < allR.Length; i++)
                for (int q = 1; q <= 23; q++) pool.Add(Make(CakeOrderKind.LeaveArc, allR[i], q));
            for (int i = 0; i < allR.Length; i++)
                for (int q = 1; q <= 23; q++) pool.Add(Make(CakeOrderKind.LeaveArea, allR[i], q));
            ValidatePool(pool);
            return pool;
        }

        public static List<MgfProblem> BuildBank()
        {
            var pool = BuildProblemPool();
            var bank = new List<MgfProblem>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                var p = pool[i];
                bank.Add(new MgfProblem {
                    id = p.id, prompt = p.prompt, choices = (string[])AngleChoices.Clone(),
                    answer = p.answer, answerNumeric = p.qAnswer * 15, unitConcept = p.unitConcept
                });
            }
            return bank;
        }

        public static List<CakeProblem> RunDeck(int serial)
        {
            // 각 난도 밴드를 순서대로 소진한다. 후보 풀에서 직접 뽑아 사후 필터링을 하지 않는다.
            int seed = unchecked(serial * 7919 + Environment.TickCount * 31 + 0x51A7);
            var rng = new Random(seed);
            int[] arcR = { 12, 24 };
            int[] allR = { 6, 12, 18, 24 };
            var kinds = new[] {
                CakeOrderKind.Arc, CakeOrderKind.Arc,
                CakeOrderKind.Area, CakeOrderKind.Area, CakeOrderKind.Area,
                CakeOrderKind.LeaveArc, CakeOrderKind.LeaveArea, CakeOrderKind.LeaveArea
            };
            var deck = new List<CakeProblem>(8);
            var used = new HashSet<string>();
            for (int i = 0; i < kinds.Length; i++)
            {
                CakeProblem p;
                do
                {
                    int r = kinds[i] == CakeOrderKind.Arc ? arcR[rng.Next(arcR.Length)] : allR[rng.Next(allR.Length)];
                    int q = 1 + rng.Next(23);
                    p = Make(kinds[i], r, q, "run" + serial);
                } while (!used.Add(p.id));
                deck.Add(p);
            }
            return deck;
        }

        public static string MisconceptionId(CakeProblem p, int qInput)
        {
            if (p.kind == CakeOrderKind.Arc && p.qAnswer <= 11 && qInput == p.qAnswer * 2)
                return "radius_as_diameter";
            if ((p.kind == CakeOrderKind.LeaveArc || p.kind == CakeOrderKind.LeaveArea) &&
                p.qAnswer != 12 && qInput == 24 - p.qAnswer)
                return "cut_the_amount_to_leave";
            if (p.kind == CakeOrderKind.Area)
            {
                // xWrong = 360S/(2rπ). 정수 눈금과 정확히 겹칠 때만 이 오개념으로 분류한다.
                int num = 180 * p.targetPi.n;
                int den = p.radius * p.targetPi.d * 15;
                if (den != 0 && num % den == 0 && qInput == num / den && qInput != p.qAnswer)
                    return "area_as_arc_total";
            }
            if (qInput == 0) return "missing_360_normalization";
            return "angle_ratio_mismatch";
        }

        static void ValidatePool(List<CakeProblem> pool)
        {
            if (pool.Count != 322) throw new InvalidOperationException("문제 은행은 322개여야 합니다.");
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < pool.Count; i++)
            {
                var p = pool[i];
                if (!ids.Add(p.id) || !prompts.Add(p.prompt)) throw new InvalidOperationException("중복 문항: " + p.id);
                if (p.qAnswer < 1 || p.qAnswer > 23) throw new InvalidOperationException("각도 범위 오류");
                var target = Target(p.kind, p.radius, p.qAnswer);
                if (!IntFraction.Equal(target, p.targetPi)) throw new InvalidOperationException("목표 계수 오류: " + p.id);
                int solutions = 0;
                for (int q = 1; q <= 23; q++)
                    if (IntFraction.Equal(Target(p.kind, p.radius, q), p.targetPi)) solutions++;
                if (solutions != 1) throw new InvalidOperationException("유일해 오류: " + p.id);
                if (p.prompt.Contains("자리에서") || p.prompt.Contains("√")) throw new InvalidOperationException("금지 표현: " + p.id);
                if (!p.prompt.Contains(" cm")) throw new InvalidOperationException("수와 단위 띄어쓰기 오류: " + p.id);
                for (int q = 1; q <= 23; q++)
                {
                    string m = MisconceptionId(p, q);
                    if (q == p.qAnswer && m != "angle_ratio_mismatch")
                        throw new InvalidOperationException("오개념 우연 참: " + p.id + " / " + m);
                }
            }
        }
    }
}
