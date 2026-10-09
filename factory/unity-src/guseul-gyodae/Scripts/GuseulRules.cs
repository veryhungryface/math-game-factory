// 구슬 교대 — 정수 분수 모델, 제약 만족 문제 풀, 오개념 역산.
// 중학교 표현 함정(2022-middle-math.json): 공은 "임의로" 꺼내고 모양과 크기가 모두 같다고 명시한다.
// 서로 다른 A/B 추첨은 "서로 영향을 끼치지 않는다"고 명시하며 답은 항상 기약분수다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.GuseulGyodae
{
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

        public static int Gcd(int a, int b)
        {
            while (b != 0) { int t = a % b; a = b; b = t; }
            return Math.Max(1, a);
        }

        public bool Equals(IntFraction other) => n * other.d == other.n * d;
        public string Display => d == 1 ? n.ToString() : n + "/" + d;
    }

    [Serializable]
    public sealed class MarbleProblem
    {
        public string id;
        public int band;
        public int k;                 // 정답 B 파랑 구슬 수. 0..6 균등.
        public int j;                 // 3단계 고정 A의 파랑 수. 1..3.
        public int currentBlue;
        public int receiptNo;
        public int observedBlue;
        public int observedTrials;
        public int targetN;
        public int targetD;
        public string prompt;
        public string concept;
        public string answer;
    }

    public struct DiagnosticPolicy
    {
        public string misconceptionId;
        public int blueCount;
        public DiagnosticPolicy(string id, int count) { misconceptionId = id; blueCount = count; }
    }

    public static class GuseulRules
    {
        public const int SlotCount = 6;
        public const int StartLives = 3;
        public const int OrderCount = 10;
        public const int RequiredFirstCorrect = 8;
        public const int RequiredBand3FirstCorrect = 3;

        public static IntFraction Probability(MarbleProblem p, int blueCount)
        {
            blueCount = Math.Max(0, Math.Min(SlotCount, blueCount));
            if (p.band == 2) return new IntFraction(SlotCount - blueCount, SlotCount);
            if (p.band == 3) return new IntFraction(p.j * blueCount, 4 * SlotCount);
            return new IntFraction(blueCount, SlotCount);
        }

        public static IntFraction Target(MarbleProblem p) => new IntFraction(p.targetN, p.targetD);

        // 실제 채점은 정수 교차곱만 쓴다. float/double 비교 금지.
        public static bool IsCorrect(MarbleProblem p, int blueCount)
        {
            IntFraction made = Probability(p, blueCount);
            return made.n * p.targetD == p.targetN * made.d;
        }

        public static MarbleProblem Make(int band, int k, int j, int currentBlue, int receiptNo,
            int observedBlue, int observedTrials, string id)
        {
            band = Math.Max(1, Math.Min(3, band));
            k = Math.Max(0, Math.Min(SlotCount, k));
            j = band == 3 ? Math.Max(1, Math.Min(3, j)) : 0;
            currentBlue = Math.Max(0, Math.Min(SlotCount, currentBlue));
            IntFraction target = band == 1
                ? new IntFraction(k, SlotCount)
                : band == 2
                    ? new IntFraction(SlotCount - k, SlotCount)
                    : new IntFraction(j * k, 4 * SlotCount);

            var p = new MarbleProblem
            {
                id = id,
                band = band,
                k = k,
                j = j,
                currentBlue = currentBlue,
                receiptNo = receiptNo,
                observedBlue = observedBlue,
                observedTrials = observedTrials,
                targetN = target.n,
                targetD = target.d
            };

            if (band == 1)
            {
                p.concept = "경우의 수의 비율로서의 확률";
                p.prompt = "현재 B는 파랑 " + currentBlue + "개, 흰 " + (SlotCount - currentBlue)
                    + "개이다. B에서 공 한 개를 임의로 꺼낼 때, 파랑 공이 나올 확률이 " + target.Display
                    + "이 되도록 구성하시오. (단, 공의 모양과 크기는 모두 같다.)";
            }
            else if (band == 2)
            {
                p.concept = "어떤 사건이 일어나지 않을 확률";
                p.prompt = "같은 구성의 B에서 공 한 개를 꺼내 확인한 뒤 다시 넣는 시행을 " + observedTrials
                    + "회 반복했더니 파랑 공이 " + observedBlue + "회 나왔다. 현재 B는 파랑 " + currentBlue
                    + "개, 흰 " + (SlotCount - currentBlue) + "개이다. 관찰 상대도수와 비교하며, B에서 공 한 개를 임의로 꺼낼 때 "
                    + "파랑 공이 나오지 않을 확률이 " + target.Display
                    + "이 되도록 구성하시오. (단, 공의 모양과 크기는 모두 같다.)";
            }
            else
            {
                p.concept = "서로 영향을 끼치지 않는 두 사건이 동시에 일어날 확률";
                p.prompt = "서로 다른 A와 B에서 공을 각각 한 개씩 임의로 꺼낸다. A는 파랑 " + j
                    + "개, 흰 " + (4 - j) + "개이고 B는 현재 파랑 " + currentBlue + "개, 흰 "
                    + (SlotCount - currentBlue) + "개이다. 두 공이 모두 파랑일 확률이 " + target.Display
                    + "이 되도록 B를 구성하시오. (단, 각 추첨은 서로 영향을 끼치지 않고 공의 모양과 크기는 모두 같다.)";
            }

            p.answer = "파랑 " + k + "개·흰 " + (SlotCount - k) + "개, 확률 " + target.Display;
            return p;
        }

        public static MarbleProblem Practice()
        {
            return Make(1, 3, 0, 2, 0, 0, 0, "practice-fixed");
        }

        public static List<MarbleProblem> BuildRunDeck(Random rng, int currentBlue)
        {
            var list = new List<MarbleProblem>(OrderCount);
            for (int order = 1; order <= OrderCount; order++)
            {
                int band = order <= 3 ? 1 : order <= 6 ? 2 : 3;
                int k = rng.Next(0, 7); // 목표와 현재 구성의 차이를 이유로 재추출하지 않는다.
                int j = band == 3 ? rng.Next(1, 4) : 0;
                // 보완사건의 관찰 기록은 정답 구성 k에서 얻은 반복 시행이다.
                // 시행 횟수는 6의 배수, 관찰값은 이론값 k/6 부근의 정수로 만들어
                // 상대도수와 이론확률을 비교할 근거가 되게 한다.
                int trials = band == 2 ? 12 + rng.Next(0, 3) * 6 : 12;
                int expectedBlue = k * (trials / SlotCount);
                int observed = band == 2
                    ? Math.Max(0, Math.Min(trials, expectedBlue + rng.Next(-1, 2)))
                    : 0;
                list.Add(Make(band, k, j, currentBlue, order, observed, trials,
                    "run-b" + band + "-o" + order + "-k" + k + "-j" + j));
            }
            return list;
        }

        // QA 은행도 실제 판정과 같은 Make/Probability/Target 경로에서 만든다.
        // 문장 다양성을 괄호 꼬리표에 의존하지 않도록 현재 구성·관찰 기록·관찰 수치를 본문에 넣는다.
        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(441);
            int serial = 1;
            for (int band = 1; band <= 3; band++)
            {
                int jMax = band == 3 ? 3 : 1;
                for (int jRaw = 1; jRaw <= jMax; jRaw++)
                for (int k = 0; k <= 6; k++)
                for (int current = 0; current <= 6; current++)
                for (int history = 0; history < (band == 3 ? 1 : 3); history++)
                {
                    int j = band == 3 ? jRaw : 0;
                    int trials = band == 2 ? 12 + history * 6 : 12;
                    int expectedBlue = k * (trials / SlotCount);
                    int observed = band == 2
                        ? Math.Max(0, Math.Min(trials, expectedBlue + (current % 3) - 1))
                        : 0;
                    var p = Make(band, k, j, current, 20 + history * 17 + current, observed, trials,
                        "bank-" + serial);
                    bank.Add(new MgfProblem
                    {
                        id = p.id,
                        prompt = p.prompt,
                        choices = null,
                        answer = p.answer,
                        answerNumeric = p.targetN / (double)p.targetD,
                        unitConcept = p.concept
                    });
                    serial++;
                }
            }
            return bank;
        }

        // 오개념 역산 정책. 후처리 순서: 정답 제거 → 범위 검사 → 중복 제거 → 부족분 보충.
        // 이 게임은 보기 버튼을 만들지 않지만, 실제 오답 구성을 설명하고 분류하는 데 같은 정책을 쓴다.
        public static List<DiagnosticPolicy> MisconceptionPolicies(MarbleProblem p)
        {
            var raw = new List<DiagnosticPolicy>();
            IntFraction target = Target(p);
            raw.Add(new DiagnosticPolicy("equiprobable-half", 3));
            raw.Add(new DiagnosticPolicy("copy-reduced-numerator", target.n));
            if (p.band == 2) raw.Add(new DiagnosticPolicy("complement-not-inverted", 6 - p.k));
            if (p.band == 3)
            {
                // j/4 + K/6 = 목표라고 잘못 두고 가장 가까운 정수 K를 선택하는 정책.
                int best = 0, bestErr = int.MaxValue;
                for (int blue = 0; blue <= 6; blue++)
                {
                    int left = (6 * p.j + 4 * blue) * target.d;
                    int right = target.n * 24;
                    int err = Math.Abs(left - right);
                    if (err < bestErr) { bestErr = err; best = blue; }
                }
                raw.Add(new DiagnosticPolicy("add-independent-events", best));
            }

            var result = new List<DiagnosticPolicy>();
            var used = new HashSet<int>();
            for (int i = 0; i < raw.Count; i++)
            {
                int v = raw[i].blueCount;
                if (v < 0 || v > 6 || IsCorrect(p, v) || !used.Add(v)) continue;
                result.Add(raw[i]);
            }
            for (int delta = 1; result.Count < 3 && delta <= 6; delta++)
            {
                int[] values = { p.k - delta, p.k + delta };
                for (int q = 0; q < values.Length && result.Count < 3; q++)
                {
                    int v = values[q];
                    if (v < 0 || v > 6 || IsCorrect(p, v) || !used.Add(v)) continue;
                    result.Add(new DiagnosticPolicy("near-miss-count", v));
                }
            }
            return result;
        }

        public static string ClassifyWrong(MarbleProblem p, int blueCount)
        {
            List<DiagnosticPolicy> policies = MisconceptionPolicies(p);
            for (int i = 0; i < policies.Count; i++)
                if (policies[i].blueCount == blueCount) return policies[i].misconceptionId;
            return "count-not-matching-target";
        }

        // 모든 매개변수 풀에서 오개념 후보가 우연히 정답이 되는 경우가 0인지 런타임 부팅 전에 검사한다.
        public static void ValidateAll()
        {
            for (int band = 1; band <= 3; band++)
            for (int j = 1; j <= (band == 3 ? 3 : 1); j++)
            for (int k = 0; k <= 6; k++)
            for (int current = 0; current <= 6; current++)
            {
                MarbleProblem p = Make(band, k, band == 3 ? j : 0, current, 1, 0, 10, "audit");
                List<DiagnosticPolicy> d = MisconceptionPolicies(p);
                if (d.Count < 2) throw new InvalidOperationException("오개념 역산 후보 부족: band=" + band + " k=" + k);
                for (int i = 0; i < d.Count; i++)
                    if (IsCorrect(p, d[i].blueCount))
                        throw new InvalidOperationException("오개념 후보가 정답과 일치: " + d[i].misconceptionId);
            }
        }
    }
}
