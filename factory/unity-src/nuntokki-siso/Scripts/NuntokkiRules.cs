// 눈토끼 시소 — 중2 「도형의 닮음」 정수 연산 정본.
//
// curriculum/2022-middle-math.json style_guide / expression_traps 적용:
// - 문제 발문은 중학교 지시형 「구하시오 / 조절하시오」로 통일한다.
// - 「닮음비」「넓이의 비」「부피의 비」를 서로 바꾸어 쓰지 않는다.
// - 같은 밀도인 닮은 입체의 질량비는 부피의 비와 같고, 닮음비 1:k이면 1:k³이다.
// - 수와 단위 사이를 띄어 쓴다(2 cm). 중2 범위 밖인 √·세제곱근 표기는 쓰지 않는다.
// - 판정은 targetMini == selectedK * selectedK * selectedK 정수 비교 하나만 쓴다.
//
// 오개념 역산 id(ProblemBankJson 선택지와 실제 오답 설명이 공유):
// - linear_volume_as_length: 부피의 비 N을 길이 배율로 그대로 사용한다.
// - square_for_volume: N을 k²로 보고 배율을 정한다.
// - additive_growth: 직전 높이에 2 cm만 더하면 된다고 생각한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.NuntokkiSiso
{
    [Serializable]
    public sealed class SnowOrder
    {
        public string id;
        public int baseHeight;
        public int targetK;
        public int targetMini;
        public int answerHeight;
        public int band;
        public string prompt;
        public string unitConcept;
    }

    public static class NuntokkiRules
    {
        public const int StartLives = 3;
        public const int TargetOrders = 7;
        public const int MasteryFirstCorrect = 5;
        public const float SessionSeconds = 140f;
        public const int MinK = 2;
        public const int MaxK = 8;

        public static int Cube(int k) => k * k * k;

        public static SnowOrder Create(string id, int baseHeight, int targetK, int band)
        {
            if (baseHeight < 1) throw new ArgumentOutOfRangeException(nameof(baseHeight));
            if (targetK < MinK || targetK > MaxK) throw new ArgumentOutOfRangeException(nameof(targetK));
            int mini = Cube(targetK);
            return new SnowOrder {
                id = id,
                baseHeight = baseHeight,
                targetK = targetK,
                targetMini = mini,
                answerHeight = baseHeight * targetK,
                band = band,
                prompt = "양팔 길이가 같은 시소에서 기준 눈사람의 높이가 " + baseHeight + " cm이다. 같은 재료를 같은 밀도로 다진 닮은 눈사람 " + mini + "개분과 균형을 이루는 큰 눈사람의 높이를 구하시오.",
                unitConcept = "닮은 입체도형의 부피의 비"
            };
        }

        public static SnowOrder Practice()
        {
            var p = Create("nts-practice", 2, 2, 0);
            p.prompt = "양팔 길이가 같은 시소에서 미니 눈사람 8개분과 균형을 이루도록 큰 눈사람의 높이를 조절하시오. (단, 모든 눈사람은 같은 재료를 같은 밀도로 다진다.)";
            return p;
        }

        // 세션마다 k=2..8을 정확히 한 번씩 쓴다. Fisher-Yates의 결정적 seed만 바뀌므로
        // 어느 눈금 고정·일곱 눈금 순환 정책의 장기 첫 시도 기준은 정확히 1/7이다.
        public static List<SnowOrder> RunDeck(int serial)
        {
            var values = new List<int> { 2, 3, 4, 5, 6, 7, 8 };
            uint x = (uint)(0x9E3779B9u ^ (uint)(serial * 747796405));
            for (int i = values.Count - 1; i > 0; i--)
            {
                x ^= x << 13; x ^= x >> 17; x ^= x << 5;
                int j = (int)(x % (uint)(i + 1));
                int t = values[i]; values[i] = values[j]; values[j] = t;
            }
            var deck = new List<SnowOrder>(TargetOrders);
            for (int i = 0; i < values.Count; i++)
            {
                int band = i < 2 ? 1 : (i < 5 ? 2 : 3);
                // 실제 런타임 id도 은행의 같은 생성 행을 가리킨다.
                deck.Add(Create("nts-bank-1-h2-k" + values[i], 2, values[i], band));
            }
            return deck;
        }

        public static bool IsCorrect(SnowOrder order, int selectedK)
            => order != null && Cube(selectedK) == order.targetMini;

        public static string MisconceptionId(SnowOrder order, int selectedK, int previousCorrectK)
        {
            if (order == null) return "unknown";
            if (selectedK * selectedK == order.targetMini) return "square_for_volume";
            if (selectedK == order.targetMini) return "linear_volume_as_length";
            if (previousCorrectK >= MinK && selectedK == previousCorrectK + 1) return "additive_growth";
            return selectedK < order.targetK ? "volume_underestimate" : "volume_overestimate";
        }

        static int RoundedSquareGuess(int n)
        {
            // sqrt를 학생에게 제시하지 않는다. 오개념 선택지를 정수 전수 탐색으로 역산한다.
            int best = 1, bestError = Math.Abs(n - 1);
            for (int k = 2; k <= 64; k++)
            {
                int error = Math.Abs(n - k * k);
                if (error < bestError) { best = k; bestError = error; }
            }
            return best;
        }

        static MgfProblem ToProblem(SnowOrder order, int serial)
        {
            // 실제 화면에는 선택지 버튼이 없다. QA용 choices는 같은 주문 모델에서 역산한 대표 높이이다.
            // 후처리 순서: 정답 제거 → 범위 검사 → 중복 제거 → 부족분 보충 → 결정적 셔플.
            int correct = order.answerHeight;
            var wrong = new List<int>(6) {
                order.baseHeight * order.targetMini,                         // misconceptionId: linear_volume_as_length
                order.baseHeight * RoundedSquareGuess(order.targetMini),    // misconceptionId: square_for_volume
                order.baseHeight * (order.targetK + 1)                      // misconceptionId: additive_growth
            };
            var values = new List<int>(4) { correct };
            for (int i = 0; i < wrong.Count && values.Count < 4; i++)
            {
                int v = wrong[i];
                if (v <= 0 || v == correct || values.Contains(v)) continue;
                values.Add(v);
            }
            for (int delta = 1; values.Count < 4; delta++)
            {
                int v = correct + order.baseHeight * delta;
                if (v > 0 && v != correct && !values.Contains(v)) values.Add(v);
            }
            int shift = Math.Abs(serial * 17 + order.targetK * 5 + order.baseHeight) % values.Count;
            var choices = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                int v = values[(i + shift) % values.Count];
                choices[i] = v + " cm";
            }
            return new MgfProblem {
                id = order.id,
                prompt = order.prompt,
                choices = choices,
                answer = correct + " cm",
                answerNumeric = correct,
                unitConcept = order.unitConcept
            };
        }

        // 350개 = 기준 높이 1..10 × k 2..8 × 축제 의뢰 5종.
        // 런타임도 이 Create 경로에서 baseHeight=2를 선택하므로 별도 시험지가 아니다.
        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(350);
            int serial = 0;
            for (int batch = 1; batch <= 5; batch++)
            for (int baseHeight = 1; baseHeight <= 10; baseHeight++)
            for (int k = MinK; k <= MaxK; k++)
            {
                serial++;
                var order = Create("nts-bank-" + batch + "-h" + baseHeight + "-k" + k, baseHeight, k, 1 + (batch - 1) / 2);
                if (batch > 1) order.prompt = "눈축제 " + batch + "번 광장: " + order.prompt;
                bank.Add(ToProblem(order, serial));
            }
            return bank;
        }

        public static void AuditBank(IList<MgfProblem> bank)
        {
            if (bank == null || bank.Count < 300) throw new InvalidOperationException("problem bank below 300");
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < bank.Count; i++)
            {
                var p = bank[i];
                if (!ids.Add(p.id) || !prompts.Add(p.prompt)) throw new InvalidOperationException("duplicate bank row: " + p.id);
                if (p.choices == null || p.choices.Length < 4) throw new InvalidOperationException("choices missing: " + p.id);
                bool answerFound = false;
                var seen = new HashSet<string>();
                for (int c = 0; c < p.choices.Length; c++)
                {
                    if (!seen.Add(p.choices[c])) throw new InvalidOperationException("duplicate choice: " + p.id);
                    if (p.choices[c] == p.answer) answerFound = true;
                }
                if (!answerFound) throw new InvalidOperationException("answer absent: " + p.id);
            }
        }
    }
}
