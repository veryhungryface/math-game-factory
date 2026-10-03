// 케이블 록 — 삼각비 길이 문제 모델과 정수 판정.
//
// curriculum/2022-middle-math.json expression_traps:
// - 삼각비는 기준각이 있어야 한다. 모든 발문에 ∠C=90°와 기준각 A를 밝힌다.
// - 수와 단위는 띄어 쓴다(6 m). 근호는 a√b 꼴로 단순화하고 분모에 근호를 두지 않는다.
// - 삼각비 사이의 관계식은 쓰지 않는다. 답 판정은 1~48의 정수 동등 비교만 쓴다.
//
// 오개념 역산은 화면의 오답 매듭에 misconceptionId로 남긴다.
// swap_sin_cos: 기준각에서 대변/이웃변을 바꾸거나 sin 30°와 sin 60°를 바꾼 길이.
// multiply_instead_of_divide: 미지수가 분모 쪽인데 나누지 않고 곱한 길이.
// 두 대표 오답은 정답 제거 → 범위 검사 → 중복 제거 순서로 만든다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.CableLock
{
    public enum CableTemplate
    {
        Tan45Opp, Sin30Opp, Cos60Adj, Sin45Opp, Cos45Adj,
        Tan30Opp, Tan60Adj, Height45, Height30, Distance60
    }

    [Serializable]
    public sealed class CableProblem
    {
        public string id;
        public int answer;
        public int band;
        public CableTemplate template;
        public string prompt;
        public string formula;
        public string roles;
        public string unitConcept;
        public int angleDegrees;
        public int wrongSwap;
        public int wrongMultiply;
    }

    public static class CableLockRules
    {
        public const int MinLength = 1;
        // 48칸으로 답 공간을 넓혀 수학을 읽지 않는 단일 길이 입력의 우연 성공률을 1/48로 제한한다.
        public const int MaxLength = 48;
        public const int TargetCount = 6;
        public const int StartLives = 3;
        public const int PrecisionBonusFirst = 5;

        static readonly string[] LengthChoices = BuildChoices();

        static string[] BuildChoices()
        {
            var c = new string[MaxLength];
            for (int i = 0; i < c.Length; i++) c[i] = (i + 1) + " m";
            return c;
        }

        public static CableProblem Practice()
        {
            return new CableProblem
            {
                id = "cl-practice-45-6", answer = 6, band = 0, template = CableTemplate.Tan45Opp, angleDegrees = 45,
                prompt = "∠C=90°인 직각삼각형 ABC에서 ∠A=45°, 이웃변 AC=6 m이다. 대변 케이블 BC의 길이를 구하시오.",
                formula = "tan 45° = BC / AC = 6 / 6 = 1",
                roles = "대변 BC = 케이블  ·  이웃변 AC = 6 m",
                unitConcept = "tan 45°로 미지의 대변 구하기",
                // 45°에서는 sin/cos 교환이나 tan의 역수를 써도 같은 값이므로 특정 오개념으로 진단하지 않는다.
                wrongSwap = 0, wrongMultiply = 0
            };
        }

        // 실제 판은 답 1~48의 균형 덱을 먼저 만들고 독립 셔플한다.
        // 길이에서 역으로 템플릿을 붙이므로 생성 후 필터링이나 무한 재시도가 없다.
        // 실플레이(MakeForAnswer)와 QA 표본은행(BuildBank)이 공유하는 단계별 도달 가능 템플릿 공급자.
        static readonly CableTemplate[][] BandTemplates = {
            new[] { CableTemplate.Tan45Opp, CableTemplate.Sin30Opp, CableTemplate.Cos60Adj },
            new[] { CableTemplate.Tan30Opp, CableTemplate.Tan60Adj, CableTemplate.Sin45Opp, CableTemplate.Cos45Adj },
            new[] { CableTemplate.Height45, CableTemplate.Height30, CableTemplate.Distance60 }
        };

        public static CableTemplate[] TemplatesForBand(int band)
        {
            return BandTemplates[Math.Max(1, Math.Min(3, band)) - 1];
        }

        // 실제 판은 답 1~48의 균형 덱을 독립 셔플한다. 길이에서 역으로 템플릿을 붙이므로
        // 생성 후 필터링이나 무한 재시도가 없다.
        public static CableProblem MakeForAnswer(int answer, int band, int variant)
        {
            answer = Math.Max(MinLength, Math.Min(MaxLength, answer));
            var list = TemplatesForBand(band);
            var t = list[(variant & int.MaxValue) % list.Length];
            return Make(answer, Math.Max(1, Math.Min(3, band)), t, "run-" + variant);
        }

        static CableProblem Make(int a, int band, CableTemplate t, string suffix)
        {
            var p = new CableProblem { answer = a, band = band, template = t };
            // 실플레이 id에는 답을 넣지 않는다(상태 훅으로 답을 읽는 우회 방지). 표본은행 id는 suffix에 길이를 넣어 고유하게 한다.
            p.id = "cl-" + band + "-" + (int)t + "-" + suffix;
            switch (t)
            {
                case CableTemplate.Tan45Opp:
                    p.prompt = "∠C=90°인 직각삼각형 ABC에서 ∠A=45°, 이웃변 AC=" + a + " m이다. 대변 케이블 BC의 길이를 구하시오.";
                    p.formula = "tan 45° = BC / " + a + " = 1  →  BC = " + a + " m";
                    p.roles = "미지: 대변 BC  ·  주어진 변: 이웃변 AC";
                    p.unitConcept = "tan 45°로 대변 구하기";
                    break;
                case CableTemplate.Sin30Opp:
                    p.prompt = "∠C=90°인 직각삼각형 ABC에서 ∠A=30°, 빗변 AB=" + (2 * a) + " m이다. 대변 케이블 BC의 길이를 구하시오.";
                    p.formula = "sin 30° = BC / " + (2 * a) + " = 1 / 2  →  BC = " + a + " m";
                    p.roles = "미지: 대변 BC  ·  주어진 변: 빗변 AB";
                    p.unitConcept = "sin 30°로 대변 구하기";
                    break;
                case CableTemplate.Cos60Adj:
                    p.prompt = "∠C=90°인 직각삼각형 ABC에서 ∠A=60°, 빗변 AB=" + (2 * a) + " m이다. 이웃변 케이블 AC의 길이를 구하시오.";
                    p.formula = "cos 60° = AC / " + (2 * a) + " = 1 / 2  →  AC = " + a + " m";
                    p.roles = "미지: 이웃변 AC  ·  주어진 변: 빗변 AB";
                    p.unitConcept = "cos 60°로 이웃변 구하기";
                    break;
                case CableTemplate.Sin45Opp:
                    p.prompt = "∠C=90°인 직각삼각형 ABC에서 ∠A=45°, 빗변 AB=" + Radical(a, 2) + " m이다. 대변 케이블 BC의 길이를 구하시오.";
                    p.formula = "sin 45° = BC / (" + Radical(a, 2) + ") = √2 / 2  →  BC = " + a + " m";
                    p.roles = "미지: 대변 BC  ·  주어진 변: 빗변 AB";
                    p.unitConcept = "sin 45°의 정확한 값";
                    break;
                case CableTemplate.Cos45Adj:
                    p.prompt = "∠C=90°인 직각삼각형 ABC에서 ∠A=45°, 빗변 AB=" + Radical(a, 2) + " m이다. 이웃변 케이블 AC의 길이를 구하시오.";
                    p.formula = "cos 45° = AC / (" + Radical(a, 2) + ") = √2 / 2  →  AC = " + a + " m";
                    p.roles = "미지: 이웃변 AC  ·  주어진 변: 빗변 AB";
                    p.unitConcept = "cos 45°의 정확한 값";
                    break;
                case CableTemplate.Tan30Opp:
                    p.prompt = "∠C=90°인 직각삼각형 ABC에서 ∠A=30°, 이웃변 AC=" + Radical(a, 3) + " m이다. 대변 케이블 BC의 길이를 구하시오.";
                    p.formula = "tan 30° = BC / (" + Radical(a, 3) + ") = √3 / 3  →  BC = " + a + " m";
                    p.roles = "미지: 대변 BC  ·  주어진 변: 이웃변 AC";
                    p.unitConcept = "tan 30°의 정확한 값";
                    break;
                case CableTemplate.Tan60Adj:
                    p.prompt = "∠C=90°인 직각삼각형 ABC에서 ∠A=60°, 대변 BC=" + Radical(a, 3) + " m이다. 이웃변 케이블 AC의 길이를 구하시오.";
                    p.formula = "tan 60° = (" + Radical(a, 3) + ") / AC = √3  →  AC = " + a + " m";
                    p.roles = "미지: 이웃변 AC  ·  주어진 변: 대변 BC";
                    p.unitConcept = "tan 60°의 정확한 값";
                    break;
                case CableTemplate.Height45:
                    p.prompt = "컨테이너 밑점에서 수평으로 " + a + " m 떨어진 관측점에서 꼭대기를 올려본각이 45°이다. 눈의 높이를 생각하지 않을 때 컨테이너의 높이를 구하시오.";
                    p.formula = "tan 45° = 높이 / " + a + " = 1  →  높이 = " + a + " m";
                    p.roles = "미지: 높이  ·  주어진 값: 수평 거리";
                    p.unitConcept = "삼각비를 활용한 높이 구하기";
                    break;
                case CableTemplate.Height30:
                    p.prompt = "크레인 밑점에서 수평으로 " + Radical(a, 3) + " m 떨어진 관측점에서 꼭대기를 올려본각이 30°이다. 눈의 높이를 생각하지 않을 때 크레인의 높이를 구하시오.";
                    p.formula = "tan 30° = 높이 / (" + Radical(a, 3) + ") = √3 / 3  →  높이 = " + a + " m";
                    p.roles = "미지: 높이  ·  주어진 값: 수평 거리";
                    p.unitConcept = "tan 30°를 활용한 높이 구하기";
                    break;
                case CableTemplate.Distance60:
                    p.prompt = "관측점에서 수직으로 세워진 크레인의 꼭대기를 올려본각이 60°이고, 밑점부터 꼭대기까지의 높이가 " + Radical(a, 3) + " m이다. 눈의 높이를 생각하지 않을 때 관측점에서 크레인 밑점까지의 수평 거리를 구하시오.";
                    p.formula = "tan 60° = (" + Radical(a, 3) + ") / 거리 = √3  →  거리 = " + a + " m";
                    p.roles = "미지: 수평 거리  ·  주어진 값: 높이";
                    p.unitConcept = "tan 60°를 활용한 거리 구하기";
                    break;
            }
            p.angleDegrees = AngleFor(t);
            DeriveMisconceptions(p);
            return p;
        }

        static string Radical(int coefficient, int radicand)
        {
            return (coefficient == 1 ? "" : coefficient.ToString()) + "√" + radicand;
        }

        static int AngleFor(CableTemplate t)
        {
            if (t == CableTemplate.Sin30Opp || t == CableTemplate.Tan30Opp || t == CableTemplate.Height30) return 30;
            if (t == CableTemplate.Cos60Adj || t == CableTemplate.Tan60Adj || t == CableTemplate.Distance60) return 60;
            return 45;
        }

        // 화면에 제시된 주어진 변과 잘못 적용한 삼각비에서 정수로 정확히 나오는 길이만
        // 오개념 후보로 둔다. a√3 같은 무리수를 정수 눈금으로 반올림해 특정 오개념이라
        // 단정하지 않는다. 45°처럼 뒤바꿔도 같은 값인 경우에도 진단을 만들지 않는다.
        static void DeriveMisconceptions(CableProblem p)
        {
            p.wrongSwap = 0;
            p.wrongMultiply = 0;
            int a = p.answer;
            if (p.template == CableTemplate.Sin30Opp || p.template == CableTemplate.Cos60Adj)
            {
                int divided = 4 * a; // (2a) ÷ (1/2): 곱해야 할 때 나눈 정확한 정수 결과
                if (divided <= MaxLength) p.wrongMultiply = divided;
            }
            else if (p.template == CableTemplate.Tan30Opp || p.template == CableTemplate.Height30)
            {
                int reciprocal = 3 * a; // (a√3)×tan60° = 3a
                if (reciprocal <= MaxLength) p.wrongSwap = reciprocal;
            }
            else if (p.template == CableTemplate.Tan60Adj || p.template == CableTemplate.Distance60)
            {
                int multiplied = 3 * a; // (a√3)×tan60° = 3a, 나눗셈 대신 곱한 결과
                if (multiplied <= MaxLength) p.wrongMultiply = multiplied;
            }
        }

        public static string IdentifyMisconception(CableProblem p, int selected)
        {
            if (p == null) return "length_mismatch";
            if (p.wrongSwap > 0 && selected == p.wrongSwap)
                return p.template == CableTemplate.Sin30Opp || p.template == CableTemplate.Cos60Adj
                    ? "swap_sin_cos" : "reciprocal_ratio";
            if (p.wrongMultiply > 0 && selected == p.wrongMultiply) return "multiply_instead_of_divide";
            return selected < p.answer ? "cable_too_short" : "cable_too_long";
        }

        public static List<MgfProblem> BuildBank()
        {
            var result = new List<MgfProblem>(MaxLength * 10);
            for (int band = 1; band <= 3; band++)
            {
                var templates = TemplatesForBand(band);
                for (int n = MinLength; n <= MaxLength; n++)
                for (int k = 0; k < templates.Length; k++)
                {
                    var p = Make(n, band, templates[k], "bank-" + k + "-" + n);
                    result.Add(new MgfProblem
                    {
                        id = p.id,
                        prompt = p.prompt,
                        choices = (string[])LengthChoices.Clone(),
                        answer = p.answer + " m",
                        answerNumeric = p.answer,
                        unitConcept = p.unitConcept
                    });
                }
            }
            return result;
        }

        public static int[] ShuffledDeck(Random rng)
        {
            var d = new int[MaxLength];
            for (int i = 0; i < d.Length; i++) d[i] = i + 1;
            for (int i = d.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = d[i]; d[i] = d[j]; d[j] = t;
            }
            return d;
        }
    }
}
