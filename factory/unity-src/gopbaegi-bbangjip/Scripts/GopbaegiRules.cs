// 곱빼기 빵집 — 닮은 입체도형의 부피를 정수로만 만드는 문제 모델.
// 중2 표현 함정(2022-middle-math.json):
// 1) 「닮음비」「넓이의 비」「부피의 비」를 반드시 구분한다.
// 2) 수와 단위를 띄어 쓰고(cm³), 중2에서 배우지 않는 √·삼각비를 쓰지 않는다.
// 3) m:n → m³:n³를 같은 데이터 모델에서 만들며, 오답은 선형/제곱 오개념을 역산한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.GopbaegiBbangjip
{
    [Serializable]
    public sealed class DoughOrder
    {
        public string id;
        public int band, m, n, v0, target, capacity;
        public int linear, square, reverse, additive;
        public string prompt;
    }

    public static class GopbaegiRules
    {
        public const int StartLives = 3;
        public const int StartStock = 3650;
        public const int TargetOrders = 9;
        public const float SessionSeconds = 120f;
        // 정답은 정확한 정수 한 값뿐이다. 이 범위는 오개념 진단 문구를 고르는 데만 쓴다.
        public const int MisconceptionWindow = 5;
        public const float TickSeconds = .020f;
        public const float MinHoldSeconds = .080f;

        static int Cube(int x) => x * x * x;

        public static DoughOrder Make(int m, int n, int target, int band, int serial)
        {
            int n3 = Cube(n), m3 = Cube(m);
            if (target < 200 || target > 600 || target % n3 != 0)
                throw new ArgumentException("target must be 200..600 and divisible by n³");
            int k = target / n3;
            int v0 = k * m3;
            int numerator = v0 * n3;
            if (numerator % m3 != 0 || numerator / m3 != target)
                throw new InvalidOperationException("volume invariant failed");
            return new DoughOrder
            {
                id = "uv-" + serial.ToString("D4") + "-" + m + n + "-" + v0,
                band = band,
                m = m,
                n = n,
                v0 = v0,
                target = target,
                capacity = (target * 3 + 1) / 2,
                linear = v0 * n / m,
                square = v0 * n * n / (m * m),
                reverse = (v0 * m3) % n3 == 0 ? (v0 * m3) / n3 : -1,
                additive = (v0 * n) % m == 0 ? (v0 * n) / m : -1,
                prompt = "작은 틀의 부피 " + v0 + " cm³, 닮음비 " + m + ":" + n + "입니다. 큰 틀의 부피를 구하시오."
            };
        }

        public static List<DoughOrder> RunDeck(int serial)
        {
            // 밴드를 순서대로 소진한다. 첫 문제는 온보딩과 같은 고정값이다.
            var d = new List<DoughOrder>
            {
                Make(1,2,200,1,1), Make(1,3,216,1,2), Make(1,2,320,1,3),
                Make(2,3,216,2,4), Make(2,3,270,2,5), Make(3,4,320,2,6),
                Make(1,2,400,3,7), Make(1,3,432,3,8), Make(3,4,576,3,9)
            };
            // 같은 밴드 안에서만 두 번째 판 이후 순서를 바꾼다. 난도 순서는 유지된다.
            if ((serial & 1) == 1)
            {
                Swap(d, 1, 2); Swap(d, 3, 4); Swap(d, 7, 8);
            }
            return d;
        }

        static void Swap(List<DoughOrder> d, int a, int b)
        {
            DoughOrder t = d[a]; d[a] = d[b]; d[b] = t;
        }

        public static DoughOrder Practice() => Make(1, 2, 200, 1, 0);

        public static List<MgfProblem> BuildBank()
        {
            var result = new List<MgfProblem>(348);
            int serial = 0;
            int[,] ratios = { { 1, 2 }, { 1, 3 }, { 2, 3 }, { 3, 4 } };
            string[] lead = { "햇빛 공방", "반죽 리프트", "빵틀 의뢰", "계량 기록" };
            for (int r = 0; r < ratios.GetLength(0); r++)
            {
                int m = ratios[r, 0], n = ratios[r, 1], n3 = Cube(n);
                int k0 = (200 + n3 - 1) / n3, k1 = 600 / n3;
                for (int k = k0; k <= k1; k++)
                {
                    int target = k * n3;
                    int band = r < 2 ? 1 : r == 2 ? 2 : 3;
                    var o = Make(m, n, target, band, ++serial);
                    for (int v = 0; v < 4; v++)
                    {
                        string code = (serial * 4 + v).ToString("D3");
                        string prompt = lead[v] + " " + code + ": 작은 틀의 부피가 " + o.v0 +
                            " cm³이고 작은 틀과 큰 틀의 닮음비가 " + m + ":" + n +
                            "일 때, 큰 틀의 부피를 구하시오.";
                        result.Add(new MgfProblem
                        {
                            id = o.id + "-v" + v,
                            prompt = prompt,
                            choices = Array.Empty<string>(),
                            answer = target + " cm³",
                            answerNumeric = target,
                            unitConcept = "닮은 입체도형의 부피의 비 m³:n³"
                        });
                    }
                }
            }
            Validate(result);
            return result;
        }

        static void Validate(List<MgfProblem> bank)
        {
            if (bank.Count < 300) throw new InvalidOperationException("problem bank below 300");
            // 전 파라미터 풀에서 선형·제곱 오개념 값이 정답과 우연히 같거나 서로 겹치는지 전수 검사한다.
            int[,] ratios = { { 1, 2 }, { 1, 3 }, { 2, 3 }, { 3, 4 } };
            for (int r = 0; r < ratios.GetLength(0); r++)
            {
                int m = ratios[r, 0], n = ratios[r, 1], n3 = Cube(n);
                for (int k = (200 + n3 - 1) / n3; k <= 600 / n3; k++)
                {
                    var o = Make(m, n, k * n3, 1, k);
                    if (o.linear == o.target || o.square == o.target || o.linear == o.square)
                        throw new InvalidOperationException("accidental distractor truth: " + o.id);
                }
            }
        }

        public static string Diagnose(DoughOrder o, int poured)
        {
            if (Math.Abs(poured - o.linear) <= MisconceptionWindow) return "linear_ratio";
            if (Math.Abs(poured - o.square) <= MisconceptionWindow) return "square_ratio";
            if (o.reverse >= 0 && Math.Abs(poured - o.reverse) <= MisconceptionWindow) return "reverse_ratio";
            if (o.additive >= 0 && Math.Abs(poured - o.additive) <= MisconceptionWindow) return "additive_ratio";
            if (Math.Abs(poured - o.target) <= MisconceptionWindow) return "near_miss";
            return poured < o.target ? "underfill" : "overflow";
        }
    }
}
