// 빛 사수 — 삼각비 문제 모델과 정수 검산 규칙.
//
// expression_traps (curriculum/2022-middle-math.json):
// - 삼각비는 기준각이 정해져야 값이 정해진다. 모든 발문에 ∠C=90°와 기준각 A를 쓴다.
// - 근삿값에는 ≈를 쓰고, 각은 1° 단위로 구하게 한다.
// - 수와 단위는 띄어 쓴다. 삼각비 사이의 관계식은 쓰지 않는다.
//
// 답 입력과 판정은 정수 각도 하나의 동등 비교다. 표의 근삿값 비교는
// |1000*numerator - tableValue*denominator| 정수식만 사용한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.BitSasu
{
    public enum TrigKind { Sin = 0, Cos = 1, Tan = 2 }

    [Serializable]
    public sealed class LightProblem
    {
        public string id;
        public int band;
        public int angle;
        public TrigKind trig;
        public int p; // ∠A의 대변 BC
        public int q; // ∠A의 이웃변 AC
        public int r; // 빗변 AB
        public int scale;
        public int eye;
        public int totalHeight;
        public int distance;
        public int tableValue;
        public string prompt;
        public string unitConcept;
    }

    public static class BitSasuRules
    {
        public const int MinAngle = 15;
        public const int MaxAngle = 74;
        public const int BucketCount = 60;
        public const int TargetCount = 9;
        public const int StartLives = 3;
        public const float RunSeconds = 120f;

        // 15°~74°, 실제 삼각비×1000을 가장 가까운 정수로 반올림한 정적 표.
        public static readonly int[] Sin1000 = {
            259,276,292,309,326,342,358,375,391,407,423,438,454,469,485,
            500,515,530,545,559,574,588,602,616,629,643,656,669,682,695,
            707,719,731,743,755,766,777,788,799,809,819,829,839,848,857,
            866,875,883,891,899,906,914,921,927,934,940,946,951,956,961
        };
        public static readonly int[] Cos1000 = {
            966,961,956,951,946,940,934,927,921,914,906,899,891,883,875,
            866,857,848,839,829,819,809,799,788,777,766,755,743,731,719,
            707,695,682,669,656,643,629,616,602,588,574,559,545,530,515,
            500,485,469,454,438,423,407,391,375,358,342,326,309,292,276
        };
        public static readonly int[] Tan1000 = {
            268,287,306,325,344,364,384,404,424,445,466,488,510,532,554,
            577,601,625,649,675,700,727,754,781,810,839,869,900,933,966,
            1000,1036,1072,1111,1150,1192,1235,1280,1327,1376,1428,1483,
            1540,1600,1664,1732,1804,1881,1963,2050,2145,2246,2356,2475,
            2605,2747,2904,3078,3271,3487
        };

        static readonly LightProblem[] bucketBase = BuildBuckets();

        public static int Table(TrigKind kind, int angle)
        {
            int i = angle - MinAngle;
            if (i < 0 || i >= BucketCount) return 0;
            return kind == TrigKind.Sin ? Sin1000[i] : kind == TrigKind.Cos ? Cos1000[i] : Tan1000[i];
        }

        public static string TrigName(TrigKind kind) => kind == TrigKind.Sin ? "sin" : kind == TrigKind.Cos ? "cos" : "tan";

        public static int Gcd(int a, int b)
        {
            a = Math.Abs(a); b = Math.Abs(b);
            while (b != 0) { int t = a % b; a = b; b = t; }
            return Math.Max(1, a);
        }

        public static void RatioFor(TrigKind kind, int p, int q, int r, out int n, out int d)
        {
            if (kind == TrigKind.Sin) { n = p; d = r; }
            else if (kind == TrigKind.Cos) { n = q; d = r; }
            else { n = p; d = q; }
        }

        public static int NearestAngle(TrigKind kind, int numerator, int denominator)
        {
            long best = long.MaxValue;
            int answer = MinAngle;
            bool tied = false;
            for (int a = MinAngle; a <= MaxAngle; a++)
            {
                long diff = Math.Abs(1000L * numerator - (long)Table(kind, a) * denominator);
                if (diff < best) { best = diff; answer = a; tied = false; }
                else if (diff == best) tied = true;
            }
            return tied ? -1 : answer;
        }

        static LightProblem[] BuildBuckets()
        {
            var result = new LightProblem[BucketCount];
            // 제약 만족 풀: m,n에서 피타고라스 삼각형을 먼저 만들고 세 함수 모두 같은
            // 정수각 버킷을 가리키는 후보만 보존한다. 무작위 생성 후 필터링하지 않는다.
            for (int m = 2; m <= 40; m++)
            for (int n = 1; n < m; n++)
            {
                int x = m * m - n * n;
                int y = 2 * m * n;
                int r = m * m + n * n;
                int g = Gcd(Gcd(x, y), r); x /= g; y /= g; r /= g;
                TryCandidate(result, y, x, r);
                TryCandidate(result, x, y, r);
            }
            for (int i = 0; i < result.Length; i++)
                if (result[i] == null)
                    throw new InvalidOperationException("삼각비 정수 후보 버킷 누락: " + (i + MinAngle));
            return result;
        }

        static void TryCandidate(LightProblem[] result, int p, int q, int r)
        {
            int a = NearestAngle(TrigKind.Tan, p, q);
            if (a < MinAngle || a > MaxAngle) return;
            if (NearestAngle(TrigKind.Sin, p, r) != a || NearestAngle(TrigKind.Cos, q, r) != a) return;
            // ±0.3° 조건은 후보 풀 생성 때만 쓴다. 실제 문제 정답·판정에는 실수를 쓰지 않는다.
            double exact = Math.Atan2(p, q) * 180.0 / Math.PI;
            if (Math.Abs(exact - a) > 0.3000001) return;
            int idx = a - MinAngle;
            if (result[idx] == null || r < result[idx].r)
                result[idx] = new LightProblem { angle = a, p = p, q = q, r = r };
        }

        public static LightProblem Make(int angle, int band, TrigKind trig, int variant)
        {
            int idx = Math.Max(0, Math.Min(BucketCount - 1, angle - MinAngle));
            var b = bucketBase[idx];
            var p = new LightProblem
            {
                id = "bs-" + band + "-" + angle + "-" + (int)trig + "-" + variant,
                band = band,
                angle = angle,
                trig = trig,
                p = b.p,
                q = b.q,
                r = b.r,
                scale = 1 + Math.Abs(variant % 3),
                tableValue = Table(trig, angle)
            };
            int ps = p.p * p.scale, qs = p.q * p.scale, rs = p.r * p.scale;
            string f = TrigName(trig);
            if (band == 1)
            {
                p.prompt = "∠C=90°인 직각삼각형 ABC에서 " + f + " A≈" + Thousand(p.tableValue)
                    + "이다. 삼각비표를 이용하여 ∠A의 크기를 1° 단위로 구하시오.";
                p.unitConcept = "삼각비표의 근삿값에서 각 구하기";
            }
            else if (band == 2)
            {
                p.prompt = "∠C=90°인 직각삼각형 ABC에서 AB=" + rs + " cm, BC=" + ps
                    + " cm, AC=" + qs + " cm이다. " + f
                    + " A와 삼각비표를 이용하여 ∠A의 크기를 1° 단위로 구하시오.";
                p.unitConcept = "기준각에 따른 대변·이웃변·빗변의 비";
            }
            else
            {
                p.eye = 80 + (variant % 5) * 20;
                p.distance = qs;
                p.totalHeight = p.eye + ps;
                p.trig = TrigKind.Tan;
                p.tableValue = Table(TrigKind.Tan, angle);
                // 각의 두 변(렌즈 수평선·표적 꼭대기 방향)과 지면·표적 조건을 모두 남기고 군더더기만 줄인다.
                p.prompt = "렌즈를 지나는 수평선과 렌즈에서 표적 꼭대기로 향하는 선이 이루는 각을 A라 하자. "
                    + "지면은 수평, 표적은 지면에 수직이고 표적 높이 " + p.totalHeight + " cm, 렌즈 높이 " + p.eye
                    + " cm, 렌즈에서 표적까지의 수평 거리 " + p.distance
                    + " cm이다. tan A와 삼각비표를 이용하여 A를 1° 단위로 구하시오.";
                p.unitConcept = "높이 차와 수평 거리를 이용한 삼각비 활용";
            }
            return p;
        }

        public static LightProblem Practice()
        {
            return new LightProblem
            {
                id = "practice-45", band = 0, angle = 45, trig = TrigKind.Tan,
                p = 1, q = 1, r = 1, scale = 1, tableValue = 1000,
                prompt = "연습: tan A≈1.000인 행을 찾아 렌즈를 45°에 놓고 빛을 보내시오.",
                unitConcept = "삼각비표 읽기 연습"
            };
        }

        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(480);
            for (int a = MinAngle; a <= MaxAngle; a++)
            {
                for (int f = 0; f < 3; f++) AddBank(bank, Make(a, 1, (TrigKind)f, a + f));
                for (int f = 0; f < 3; f++) AddBank(bank, Make(a, 2, (TrigKind)f, a + f + 1));
                AddBank(bank, Make(a, 3, TrigKind.Tan, a));
                AddBank(bank, Make(a, 3, TrigKind.Tan, a + 7));
            }
            return bank;
        }

        static void AddBank(List<MgfProblem> bank, LightProblem p)
        {
            bank.Add(new MgfProblem
            {
                id = p.id,
                prompt = p.prompt,
                choices = null,
                answer = "약 " + p.angle + "°",
                answerNumeric = p.angle,
                unitConcept = p.unitConcept
            });
        }

        public static string Thousand(int value)
        {
            int whole = value / 1000;
            int frac = value % 1000;
            return whole + "." + frac.ToString("000");
        }

        // 오개념 역산 경로. 수치 선택지를 띄우지는 않지만 학생이 실제로 그 각에 조준하면
        // 해당 misconceptionId를 상태와 해설에 남긴다.
        public static int MisconceptionAngle(LightProblem p, string id)
        {
            if (p == null) return -1;
            if (id == "swap_opposite_adjacent")
            {
                if (p.trig == TrigKind.Sin) return NearestAngle(TrigKind.Sin, p.q, p.r);
                if (p.trig == TrigKind.Cos) return NearestAngle(TrigKind.Cos, p.p, p.r);
                return NearestAngle(TrigKind.Tan, p.q, p.p);
            }
            if (id == "tan_uses_hypotenuse")
                return NearestAngle(TrigKind.Tan, p.p, p.r);
            if (id == "uses_total_height" && p.band == 3)
                return NearestAngle(TrigKind.Tan, p.totalHeight, p.distance);
            return -1;
        }

        public static string IdentifyMisconception(LightProblem p, int selected)
        {
            if (p == null) return "aimed_too_low";
            // 1단계 화면에는 삼각비의 근삿값과 표만 보인다. 생성에 사용한 내부
            // 삼각형의 변은 학생에게 제시되지 않으므로 변 선택 오개념으로 진단하지 않는다.
            if (p.band == 1) return selected > p.angle ? "aimed_too_high" : "aimed_too_low";
            if (selected == MisconceptionAngle(p, "swap_opposite_adjacent")) return "swap_opposite_adjacent";
            if (selected == MisconceptionAngle(p, "tan_uses_hypotenuse")) return "tan_uses_hypotenuse";
            if (selected == MisconceptionAngle(p, "uses_total_height")) return "uses_total_height";
            return selected > p.angle ? "aimed_too_high" : "aimed_too_low";
        }
    }
}
