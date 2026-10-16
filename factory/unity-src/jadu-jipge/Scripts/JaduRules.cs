// 자두 집게 — 대응변 두 길이와 그 사이각을 직접 구성하는 SAS 닮음 규칙.
//
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 중학교 발문은 「~을/를 구하시오」「~을/를 맞추시오」로 끝낸다.
// - △ABC∽△DEF는 A↔D, B↔E, C↔F처럼 꼭짓점 배열 하나로 기호와 계산을 함께 만든다.
// - 수와 단위는 띄어 쓰고(6 cm, 60°), 「비」가 아니라 「닮음비」라고 명시한다.
// - 중2 범위이므로 제곱근·근호·삼각비를 생성하지 않는다.
// - 답은 두 대응변과 필요할 때 두 변의 사이각이며, 채점은 정수 교차곱과 각 인덱스만 쓴다.
//
// 오개념 역산(misconceptionId):
// - additive_difference: 같은 수를 빼서 축소해도 닮는다는 가산적 추론.
// - correspondence_order: 회전·반사 뒤에도 화면의 왼쪽 변끼리 대응한다는 오류.
// - sas_wrong_angle: 두 변의 비만 같으면 사이각이 달라도 닮는다는 오류.
// - reversed_ratio: 견본:집게=q:1을 1:q로 거꾸로 적용하는 오류.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.JaduJipge
{
    [Serializable]
    public sealed class JaduProblem
    {
        public string id;
        public int band;
        public int targetU;
        public int targetV;
        public int ratioQ;
        public int referenceU;
        public int referenceV;
        public int angleIndex;
        public int angleDeg;
        public int correspondence;
        public bool mirrored;
        public bool angleRequired;
        public string sourceTriangle;
        public string clawTriangle;
        public string jointLabel;
        public string armULabel;
        public string armVLabel;
        public string prompt;
        public string answer;
        public string unitConcept;
    }

    public static class JaduRules
    {
        public const int StartLives = 3;
        public const int TargetFruit = 10;
        public const float RunSeconds = 120f;
        public const int AngleCount = 9;

        // A가 대응하는 집게 꼭짓점과 B/C가 대응하는 두 끝점. 화면 배치와 무관한 유일한 정본이다.
        static readonly char[,] Correspondence =
        {
            { 'D', 'E', 'F' },
            { 'D', 'F', 'E' },
            { 'E', 'D', 'F' },
            { 'E', 'F', 'D' },
            { 'F', 'D', 'E' },
            { 'F', 'E', 'D' }
        };

        public static int AngleFromIndex(int index) => 30 + 15 * Math.Max(0, Math.Min(AngleCount - 1, index));

        public static bool IsCorrect(JaduProblem p, int inputU, int inputV, int inputAngleIndex)
        {
            if (p == null || inputU < 1 || inputU > 12 || inputV < 1 || inputV > 12) return false;
            if (p.ratioQ * inputU != p.referenceU) return false;
            if (p.ratioQ * inputV != p.referenceV) return false;
            // angleRequired는 학생이 각을 직접 조작하는가를 뜻한다. 닮음 판정에서
            // 끼인각이 필요 없는 뜻이 아니다. 전반에는 각을 견본과 같게 고정하고,
            // 후반에는 학생이 직접 맞추되 모든 단계에서 완성 도형의 각을 검증한다.
            return inputAngleIndex == p.angleIndex;
        }

        public static string MisconceptionId(JaduProblem p, int inputU, int inputV, int inputAngleIndex)
        {
            if (p == null) return "incomplete_claw";
            if (inputU == p.targetV && inputV == p.targetU && p.targetU != p.targetV)
                return "correspondence_order";
            if (inputU == p.targetU && inputV == p.targetV && inputAngleIndex != p.angleIndex)
                return "sas_wrong_angle";

            int d1 = p.referenceU - inputU;
            int d2 = p.referenceV - inputV;
            if (d1 == d2 && d1 > 0 && (p.ratioQ * inputU != p.referenceU || p.ratioQ * inputV != p.referenceV))
                return "additive_difference";

            if (inputU == p.referenceU * p.ratioQ || inputV == p.referenceV * p.ratioQ)
                return "reversed_ratio";
            return "ratio_mismatch";
        }

        public static string Feedback(JaduProblem p, int inputU, int inputV, int inputAngleIndex)
        {
            string id = MisconceptionId(p, inputU, inputV, inputAngleIndex);
            if (id == "correspondence_order") return "기호 순서대로 대응변을 다시 확인하시오.";
            if (id == "sas_wrong_angle") return "두 변의 비는 맞지만 끼인각이 다르다.";
            if (id == "additive_difference") return "길이의 차가 아니라 닮음비가 일정해야 한다.";
            if (id == "reversed_ratio") return "견본:집게=" + p.ratioQ + ":1이므로 견본 길이를 " + p.ratioQ + "로 나누시오.";
            return "AB:" + p.armULabel + "와 AC:" + p.armVLabel + "가 각각 "
                + p.ratioQ + ":1인지 확인하시오.";
        }

        public static JaduProblem Practice()
        {
            return Make("practice", 0, 4, 6, 2, 2, 0, false);
        }

        public static JaduProblem Make(string id, int band, int u, int v, int q, int angleIndex, int correspondence, bool mirrored)
        {
            correspondence = Math.Max(0, Math.Min(5, correspondence));
            char joint = Correspondence[correspondence, 0];
            char endU = Correspondence[correspondence, 1];
            char endV = Correspondence[correspondence, 2];
            string claw = "△" + joint + endU + endV;
            string source = "△ABC";
            string armU = "" + joint + endU;
            string armV = "" + joint + endV;
            int deg = AngleFromIndex(angleIndex);
            bool angleRequired = band >= 3;

            string instruction = angleRequired
                ? "두 변과 그 끼인각을 맞추시오."
                : "두 팔의 길이를 구하시오.";
            // ∽ 왼쪽과 오른쪽의 꼭짓점 순서가 곧 대응 순서다. 발문·화면·판정이
            // 모두 이 배열을 공유해야 팔을 바꾼 다른 삼각형을 임의로 오답 처리하지 않는다.
            string prompt = "견본 " + source + ": AB=" + (q * u) + " cm, AC=" + (q * v)
                + " cm, ∠A=" + deg + "°. 견본과 집게의 닮음비=" + q + ":1. "
                + source + "∽" + claw + "가 되도록 " + instruction;
            string answer = armU + "=" + u + " cm, " + armV + "=" + v + " cm"
                + (angleRequired ? ", ∠" + joint + "=" + deg + "°" : "");

            return new JaduProblem
            {
                id = id,
                band = band,
                targetU = u,
                targetV = v,
                ratioQ = q,
                referenceU = q * u,
                referenceV = q * v,
                angleIndex = angleIndex,
                angleDeg = deg,
                correspondence = correspondence,
                mirrored = mirrored,
                angleRequired = angleRequired,
                sourceTriangle = source,
                clawTriangle = claw,
                jointLabel = joint.ToString(),
                armULabel = armU,
                armVLabel = armV,
                prompt = prompt,
                answer = answer,
                unitConcept = angleRequired ? "SAS 닮음" : band == 2 ? "대응변과 기호 순서" : "닮음비와 대응변"
            };
        }

        public static List<JaduProblem> RunDeck(int seed)
        {
            var rng = new Random(seed);
            var deck = new List<JaduProblem>(TargetFruit);
            for (int i = 0; i < TargetFruit; i++)
            {
                int band = i < 3 ? 1 : i < 6 ? 2 : 3;
                // 핵심 성질을 자릿값부터 역으로 만드는 것과 같은 방식으로 정답 길이 u,v를 먼저 뽑는다.
                // 사후 필터가 없으므로 1~12 전체가 균등하고 무한 루프가 없다.
                int u = rng.Next(1, 13);
                int v = rng.Next(1, 13);
                int q = rng.Next(2, 5);
                int a = rng.Next(0, AngleCount);
                int corr = band == 1 ? 0 : rng.Next(0, 6);
                bool mirror = band >= 2 && rng.Next(0, 2) == 1;
                deck.Add(Make("run-" + seed + "-" + (i + 1), band, u, v, q, a, corr, mirror));
            }
            return deck;
        }

        public static List<MgfProblem> BuildBank()
        {
            // 3×12×12=432개. 실전과 같은 Make 경로를 공유하며 prompt+answer가 모두 고유하다.
            var bank = new List<MgfProblem>(432);
            int id = 0;
            for (int q = 2; q <= 4; q++)
            for (int u = 1; u <= 12; u++)
            for (int v = 1; v <= 12; v++)
            {
                int band = id % 10 < 3 ? 1 : id % 10 < 6 ? 2 : 3;
                int angleIndex = (u * 5 + v * 3 + q) % AngleCount;
                int corr = band == 1 ? 0 : (u + v + q) % 6;
                var p = Make("bank-" + (++id), band, u, v, q, angleIndex, corr, ((u * 13 + v) & 1) == 1);
                bank.Add(new MgfProblem
                {
                    id = p.id,
                    prompt = p.prompt,
                    choices = null,
                    answer = p.answer,
                    answerNumeric = p.targetU,
                    unitConcept = p.unitConcept
                });
            }
            return bank;
        }

        public static int AuditBank()
        {
            // 전수 검산: 정답·오답 역산값이 우연히 겹치는 모든 파라미터 조합을 센다.
            int errors = 0;
            for (int q = 2; q <= 4; q++)
            for (int u = 1; u <= 12; u++)
            for (int v = 1; v <= 12; v++)
            for (int a = 0; a < AngleCount; a++)
            {
                var p = Make("audit", 3, u, v, q, a, (u + v + a) % 6, false);
                if (!IsCorrect(p, u, v, a)) errors++;
                if (u != v && IsCorrect(p, v, u, a)) errors++;
                if (IsCorrect(p, u, v, (a + 1) % AngleCount)) errors++;

                // 전반(각 조작 없음)도 고정 각이 어긋난 완성 도형을 정답으로 받지 않는다.
                var early = Make("audit-early", 1, u, v, q, a, 0, false);
                if (!IsCorrect(early, u, v, a)) errors++;
                if (IsCorrect(early, u, v, (a + 1) % AngleCount)) errors++;

                // 같은 수를 빼는 오개념 후보는 후처리 계약대로 먼저 정답을 제거한 뒤 범위 안에서만 검사한다.
                // refU-d/refV-d가 우연히 정답과 같으면 오답 후보가 아니므로 audit 대상에서도 제외한다.
                for (int d = 1; d <= 36; d++)
                {
                    int wrongU = p.referenceU - d;
                    int wrongV = p.referenceV - d;
                    bool inRange = wrongU >= 1 && wrongU <= 12 && wrongV >= 1 && wrongV <= 12;
                    bool sameAsAnswer = wrongU == p.targetU && wrongV == p.targetV;
                    if (inRange && !sameAsAnswer && IsCorrect(p, wrongU, wrongV, a)) errors++;
                }
            }
            return errors;
        }
    }
}
