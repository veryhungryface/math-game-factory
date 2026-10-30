// 참새 착지 — 산포도 정수 생성기와 판정 규칙.
// expression_traps: 근호 답은 제곱 인수를 제거한 a√b/c 꼴로 쓰고, 분산은 기약분수로 쓴다.
// 중학교 발문은 지시형 "~하시오"로 통일한다. 판정은 평균·편차제곱합 정수식만 사용한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.ChamsaeChakji
{
    [Serializable]
    public sealed class LandingProblem
    {
        public string id;
        public int band;
        public int shift;
        public int[] fixedValues;
        public int answerA;
        public int answerB;
        public int mean;
        public int squareSum;
        public int deviation;
        public int alternateA;
        public int alternateB;
        public string prompt;

        public int MovableCount => band == 1 ? 1 : 2;

        public MgfProblem ToMgf()
        {
            string answer = band == 1
                ? (answerA + shift).ToString()
                : (answerA + shift) + ", " + (answerB + shift);
            return new MgfProblem
            {
                id = id,
                prompt = prompt,
                choices = Array.Empty<string>(),
                answer = answer,
                answerNumeric = band == 1 ? answerA + shift : double.NaN,
                unitConcept = band == 1 ? "편차=변량−평균" : band == 2 ? "분산=편차 제곱의 평균" : "표준편차=√분산"
            };
        }
    }

    public static class ChamsaeRules
    {
        public const int StartLives = 3;
        public const int RoundCount = 6;
        public const int MasteryNeeded = 5;
        public const int DispersionMasteryNeeded = 3;
        public const float SessionSeconds = 180f;

        static readonly int[] PairX =
        {
            0,0,0,0,0,0, 1,1,1,1,1,1,1, 2,2,2,2,2,2, 3,3,3,3,3, 4,4,4,4, 5,5,5, 6
        };
        static readonly int[] PairY =
        {
            3,4,5,6,7,8, 2,3,4,5,6,7,8, 3,4,5,6,7,8, 4,5,6,7,8, 5,6,7,8, 6,7,8, 7
        };

        public static LandingProblem Practice()
        {
            return new LandingProblem
            {
                id = "practice-deviation",
                band = 1,
                shift = 0,
                fixedValues = new[] { 0, 3, 5 },
                answerA = 4,
                answerB = -1,
                mean = 3,
                squareSum = 14,
                deviation = 1,
                alternateA = -1,
                alternateB = -1,
                prompt = "평균이 3일 때 편차가 +1인 위치에 횃대를 놓으시오."
            };
        }

        public static List<LandingProblem> BuildProblems()
        {
            var result = new List<LandingProblem>(1100);
            BuildBandOne(result);
            BuildDispersion(result, 2);
            BuildDispersion(result, 3);
            return result;
        }

        static void BuildBandOne(List<LandingProblem> result)
        {
            int serial = 0;
            for (int shiftIndex = 0; shiftIndex < 3; shiftIndex++)
            {
                int shift = shiftIndex * 10;
                for (int x = 0; x <= 8; x++)
                {
                    int kept = 0;
                    for (int a = 0; a <= 8 && kept < 12; a++)
                    for (int b = a; b <= 8 && kept < 12; b++)
                    for (int c = b; c <= 8 && kept < 12; c++)
                    {
                        int sum = a + b + c + x;
                        if (sum % 4 != 0) continue;
                        int mu = sum / 4;
                        int d = x - mu;
                        if (d == 0 || d < -4 || d > 4) continue;
                        int q = Sq(a - mu) + Sq(b - mu) + Sq(c - mu) + Sq(d);
                        string signed = d > 0 ? "+" + d : "−" + (-d);
                        result.Add(new LandingProblem
                        {
                            id = "dev-" + (++serial).ToString("D4"), band = 1, shift = shift,
                            fixedValues = new[] { a + shift, b + shift, c + shift },
                            answerA = x, answerB = -1, mean = mu + shift, squareSum = q, deviation = d,
                            alternateA = 2 * mu - x, alternateB = -1,
                            prompt = "고정 자료 " + (a + shift) + ", " + (b + shift) + ", " + (c + shift) + "의 평균이 " + (mu + shift) + "일 때, 편차가 " + signed + "인 위치에 횃대를 놓으시오."
                        });
                        kept++;
                    }
                }
            }
        }

        static void BuildDispersion(List<LandingProblem> result, int band)
        {
            int serial = 0;
            for (int shiftIndex = 0; shiftIndex < 3; shiftIndex++)
            {
                int shift = shiftIndex * 10;
                for (int pair = 0; pair < PairX.Length; pair++)
                {
                    int x = PairX[pair], y = PairY[pair], kept = 0;
                    for (int a = 0; a <= 8 && kept < 4; a++)
                    for (int b = a; b <= 8 && kept < 4; b++)
                    {
                        int sum = a + b + x + y;
                        if (sum % 4 != 0) continue;
                        int mu = sum / 4;
                        int q = Sq(a - mu) + Sq(b - mu) + Sq(x - mu) + Sq(y - mu);
                        if (q <= 0 || q > 64 || x + y == 2 * mu) continue;

                        int solutionCount = 0;
                        for (int u = 0; u <= 8; u++)
                        for (int v = u + 1; v <= 8; v++)
                        {
                            if (a + b + u + v != 4 * mu) continue;
                            int uq = Sq(a - mu) + Sq(b - mu) + Sq(u - mu) + Sq(v - mu);
                            if (uq == q) solutionCount++;
                        }
                        if (solutionCount != 1) continue;

                        int altA = -1, altB = -1;
                        for (int u = 0; u <= 8 && altA < 0; u++)
                        for (int v = u; v <= 8; v++)
                        {
                            if (u == x && v == y) continue;
                            if (u + v != x + y) continue;
                            int uq = Sq(a - mu) + Sq(b - mu) + Sq(u - mu) + Sq(v - mu);
                            if (uq != q) { altA = u; altB = v; break; }
                        }
                        if (altA < 0) continue;

                        string target = band == 2 ? "분산 " + FormatFraction(q, 4) : "표준편차 " + FormatStandardDeviation(q);
                        string concept = band == 2 ? "평균과 분산" : "평균과 표준편차";
                        result.Add(new LandingProblem
                        {
                            id = (band == 2 ? "var-" : "sd-") + (++serial).ToString("D4"), band = band, shift = shift,
                            fixedValues = new[] { a + shift, b + shift },
                            answerA = x, answerB = y, mean = mu + shift, squareSum = q, deviation = 0,
                            alternateA = altA, alternateB = altB,
                            prompt = "고정 자료 " + (a + shift) + ", " + (b + shift) + "와 " + concept + " 조건(평균 " + (mu + shift) + " · " + target + ")을 만족하도록 두 횃대를 배치하시오."
                        });
                        kept++;
                    }
                }
            }
        }

        public static List<LandingProblem> BuildDeck(List<LandingProblem> pool, int runSerial, bool deterministic)
        {
            var deck = new List<LandingProblem>(RoundCount);
            uint seed = deterministic
                ? Mix(unchecked((uint)runSerial) ^ 0x00C0FFEEu)
                : Mix(unchecked((uint)(runSerial * 0x45D9F3B + DateTime.Now.Millisecond * 7919)));
            for (int round = 0; round < RoundCount; round++)
            {
                int band = round < 2 ? 1 : round < 4 ? 2 : 3;
                seed = Mix(seed + (uint)(round * 0x9E3779B9));
                // 정답 위치/쌍을 먼저 고른다. 200판 검증 창에서도 초기 9칸과 후반 32쌍이
                // 거의 정확히 균등해 특정 고정 입력이 사전분포 이득을 얻지 못하는 계층 추출이다.
                int wantedAnswer = band == 1 ? PositiveMod(runSerial * 7 + round * 7 + 8, 9) : -1;
                int wantedPair = band == 1 ? -1 : PositiveMod(runSerial * 9 + round * 12 + 9, 32);
                var candidates = new List<LandingProblem>(48);
                for (int i = 0; i < pool.Count; i++)
                {
                    var p = pool[i];
                    if (p.band != band) continue;
                    if (band == 1)
                    {
                        if (p.answerA == wantedAnswer) candidates.Add(p);
                    }
                    else if (p.answerA == PairX[wantedPair] && p.answerB == PairY[wantedPair]) candidates.Add(p);
                }
                if (candidates.Count == 0) throw new InvalidOperationException("산포도 문제 풀이 비었습니다.");
                seed = Mix(seed ^ 0xA511E9B3u);
                deck.Add(candidates[(int)(seed % (uint)candidates.Count)]);
            }
            return deck;
        }

        public static bool IsCorrect(LandingProblem p, int placedAAbsolute, int placedBAbsolute)
        {
            if (p.band == 1) return placedAAbsolute - p.mean == p.deviation;
            int a = p.fixedValues[0], b = p.fixedValues[1];
            int sum = a + b + placedAAbsolute + placedBAbsolute;
            if (sum != 4 * p.mean) return false;
            int q = Sq(a - p.mean) + Sq(b - p.mean) + Sq(placedAAbsolute - p.mean) + Sq(placedBAbsolute - p.mean);
            return q == p.squareSum;
        }

        public static int ActualSquareSum(LandingProblem p, int placedAAbsolute, int placedBAbsolute)
        {
            if (p.band == 1)
            {
                int q = 0;
                for (int i = 0; i < p.fixedValues.Length; i++) q += Sq(p.fixedValues[i] - p.mean);
                return q + Sq(placedAAbsolute - p.mean);
            }
            return Sq(p.fixedValues[0] - p.mean) + Sq(p.fixedValues[1] - p.mean) + Sq(placedAAbsolute - p.mean) + Sq(placedBAbsolute - p.mean);
        }

        public static string Classify(LandingProblem p, int placedAAbsolute, int placedBAbsolute)
        {
            if (p.band == 1)
            {
                int reversed = p.mean - p.deviation;
                return placedAAbsolute == reversed ? "deviation_reversed" : "deviation_position_error";
            }
            int sum = p.fixedValues[0] + p.fixedValues[1] + placedAAbsolute + placedBAbsolute;
            int q = ActualSquareSum(p, placedAAbsolute, placedBAbsolute);
            if (sum == 4 * p.mean && q != p.squareSum) return "variance_zero_from_deviation_sum";
            if (p.band == 3 && IsPerfectSquare(p.squareSum))
            {
                int sigma = IntSqrt(p.squareSum) / 2;
                if (sigma > 0 && q == 4 * sigma) return "standard_deviation_used_as_variance";
            }
            if (placedAAbsolute + placedBAbsolute == 2 * p.mean) return "mean_center_symmetry";
            return q > p.squareSum ? "dispersion_too_wide" : "dispersion_too_narrow";
        }

        public static int[] Values(LandingProblem p, int placedAAbsolute, int placedBAbsolute)
        {
            if (p.band == 1) return new[] { p.fixedValues[0], p.fixedValues[1], p.fixedValues[2], placedAAbsolute };
            return new[] { p.fixedValues[0], p.fixedValues[1], placedAAbsolute, placedBAbsolute };
        }

        public static string Evidence(LandingProblem p, int placedAAbsolute, int placedBAbsolute)
        {
            int[] values = Values(p, placedAAbsolute, placedBAbsolute);
            int sum = 0, q = 0;
            for (int i = 0; i < values.Length; i++) { sum += values[i]; q += Sq(values[i] - p.mean); }
            if (p.band == 1)
                return placedAAbsolute + "−" + p.mean + "=" + Signed(placedAAbsolute - p.mean) + " · 네 자료의 평균 " + FormatFraction(sum, 4);
            string deviations = Signed(values[0] - p.mean) + ", " + Signed(values[1] - p.mean) + ", " + Signed(values[2] - p.mean) + ", " + Signed(values[3] - p.mean);
            return "편차 " + deviations + "\n편차 제곱의 합 " + q + " ÷ 4 = " + FormatFraction(q, 4) + (p.band == 3 ? " · 표준편차 " + FormatStandardDeviation(q) : "");
        }

        public static int[] WrongPlacement(LandingProblem p)
        {
            if (p.band == 1)
            {
                int reversed = p.mean - p.deviation;
                if (reversed >= p.shift && reversed <= p.shift + 8 && reversed != p.answerA + p.shift) return new[] { reversed, -1 };
                int wrong = p.shift + ((p.answerA + 1) % 9);
                return new[] { wrong, -1 };
            }
            return new[] { p.alternateA + p.shift, p.alternateB + p.shift };
        }

        public static string FormatVariance(int q) => FormatFraction(q, 4);

        public static string FormatStandardDeviation(int q)
        {
            int squareFactor = 1;
            for (int k = 2; k * k <= q; k++) if (q % (k * k) == 0) squareFactor = k;
            int radicand = q / (squareFactor * squareFactor);
            int numerator = squareFactor, denominator = 2;
            int g = Gcd(numerator, denominator); numerator /= g; denominator /= g;
            if (radicand == 1) return denominator == 1 ? numerator.ToString() : numerator + "/" + denominator;
            string root = (numerator == 1 ? "" : numerator.ToString()) + "√" + radicand;
            return denominator == 1 ? root : root + "/" + denominator;
        }

        public static bool ValidateRecord(LandingProblem p)
        {
            if (p.band == 1) return IsCorrect(p, p.answerA + p.shift, -1);
            int solutions = 0;
            for (int x = p.shift; x <= p.shift + 8; x++)
            for (int y = p.shift; y <= p.shift + 8; y++) if (IsCorrect(p, x, y)) solutions++;
            bool alternateIsFalse = !IsCorrect(p, p.alternateA + p.shift, p.alternateB + p.shift);
            bool sameMean = p.alternateA + p.alternateB == p.answerA + p.answerB;
            return solutions == 2 && alternateIsFalse && sameMean;
        }

        static string Signed(int value) => value > 0 ? "+" + value : value < 0 ? "−" + (-value) : "0";
        static int Sq(int value) => value * value;
        static bool IsPerfectSquare(int value) { int r = IntSqrt(value); return r * r == value; }
        static int IntSqrt(int value) { int r = 0; while ((r + 1) * (r + 1) <= value) r++; return r; }
        static int Gcd(int a, int b) { while (b != 0) { int t = a % b; a = b; b = t; } return Math.Abs(a); }

        static string FormatFraction(int numerator, int denominator)
        {
            int g = Gcd(Math.Abs(numerator), denominator); numerator /= g; denominator /= g;
            return denominator == 1 ? numerator.ToString() : numerator + "/" + denominator;
        }

        static uint Mix(uint x)
        {
            x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16; return x;
        }

        static int PositiveMod(int value, int modulus)
        {
            int r = value % modulus; return r < 0 ? r + modulus : r;
        }
    }
}
