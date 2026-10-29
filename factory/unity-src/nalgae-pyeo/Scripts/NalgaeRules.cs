using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.NalgaePyeo
{
    [Serializable]
    public sealed class WingProblem
    {
        public string id;
        public int band;
        public int targetDeg;
        public int centerDeg;
        public int givenAngle2;
        public int ratioN;
        public int ratioD;
        public int rotationDeg;
        public int pointVariant;
        public string prompt;
        public string concept;

        public MgfProblem ToMgf()
        {
            return new MgfProblem
            {
                id = id,
                prompt = prompt,
                choices = Array.Empty<string>(),
                answer = targetDeg + "°",
                answerNumeric = targetDeg,
                unitConcept = concept
            };
        }
    }

    public static class NalgaeRules
    {
        public const int Goal = 8;
        public const int Lives = 3;
        public const int Mastery = 6;
        public const int SessionSeconds = 105;

        // curriculum/2022-middle-math.json의 style_guide·expression_traps를 생성기 계약으로 옮긴다.
        // - 값 요구 발문은 중학교 기본 종결형인 「~의 크기를 구하시오」로 통일한다.
        // - 각은 수와 °를 붙여 쓰고, ∠APB처럼 꼭짓점이 가운데 오게 쓴다.
        // - 「호의 길이의 비」를 명시하고 현의 길이의 비로 바꾸지 않는다.
        // - 점 P·Q는 대상 호 밖, P≠A,B 조건으로 생성한다. 반원은 중심각 180°로만 만든다.
        // - 근호·삼각비 관계·가정/결론 용어는 이 단원에서 쓰지 않는다.

        public static WingProblem Practice()
        {
            return new WingProblem
            {
                id = "practice-fixed", band = 0, targetDeg = 60, centerDeg = 120,
                givenAngle2 = 0, ratioN = 1, ratioD = 1, rotationDeg = 18, pointVariant = 0,
                concept = "원주각과 중심각",
                prompt = "중심각 ∠AOB=120°일 때, 같은 호 AB에 대한 원주각 ∠APB의 크기를 구하시오."
            };
        }

        public static List<WingProblem> BuildBank()
        {
            var list = new List<WingProblem>(432);
            for (int band = 1; band <= 3; band++)
            for (int target = 5; target <= 90; target += 5)
            for (int variant = 0; variant < 8; variant++)
                list.Add(Make(band, target, variant));
            ValidateBank(list);
            return list;
        }

        static WingProblem Make(int band, int target, int variant)
        {
            int rotation = (variant * 37 + target * 7 + band * 23) % 360;
            var p = new WingProblem
            {
                id = "arc-b" + band + "-a" + target.ToString("D2") + "-v" + variant,
                band = band,
                targetDeg = target,
                centerDeg = target * 2,
                rotationDeg = rotation,
                pointVariant = variant,
                ratioN = 1,
                ratioD = 1
            };
            string lamp = " 등불 " + (variant + 1) + "의 표시를 보고";
            if (band == 1)
            {
                p.concept = target == 90 ? "반원에 대한 원주각" : "원주각과 중심각";
                p.prompt = "원 O에서 중심각 ∠AOB=" + p.centerDeg + "°이다." + lamp + ", 같은 호 AB에 대한 원주각 ∠APB의 크기를 구하시오.";
            }
            else if (band == 2)
            {
                p.givenAngle2 = target * 2;
                p.concept = "같은 호에 대한 원주각";
                p.prompt = "∠APB와 ∠AQB는 같은 호 AB에 대한 원주각이고 ∠AQB=" + target + "°이다." + lamp + " ∠APB의 크기를 구하시오.";
            }
            else
            {
                bool threeTwo = target % 15 == 0 && variant % 2 == 1;
                p.ratioN = threeTwo ? 3 : 2;
                p.ratioD = threeTwo ? 2 : 1;
                // givenAngle2는 주어진 CD 원주각의 2배. 반도 단위도 정수로 보관한다.
                p.givenAngle2 = threeTwo ? target * 4 / 3 : target;
                p.concept = "호의 길이와 원주각";
                p.prompt = "한 원에서 호 AB의 길이와 호 CD의 길이의 비가 " + p.ratioN + ":" + p.ratioD + "이고, 호 CD에 대한 원주각은 " + AngleText2(p.givenAngle2) + "이다." + lamp + " 호 AB에 대한 원주각의 크기를 구하시오.";
            }
            return p;
        }

        public static List<WingProblem> BuildDeck(List<WingProblem> bank, int seed)
        {
            var rng = new Random(seed);
            var deck = new List<WingProblem>(Goal);
            int[] counts = { 0, 2, 3, 3 };
            for (int band = 1; band <= 3; band++)
            {
                var pool = new List<WingProblem>();
                for (int i = 0; i < bank.Count; i++) if (bank[i].band == band) pool.Add(bank[i]);
                for (int i = 0; i < pool.Count; i++)
                {
                    int j = rng.Next(i, pool.Count);
                    WingProblem t = pool[i]; pool[i] = pool[j]; pool[j] = t;
                }
                var usedTargets = new HashSet<int>();
                for (int i = 0; i < pool.Count && usedTargets.Count < counts[band]; i++)
                    if (usedTargets.Add(pool[i].targetDeg)) deck.Add(pool[i]);
            }
            return deck;
        }

        public static bool IsCorrect(WingProblem p, int selectedDeg) => p != null && selectedDeg == p.targetDeg;

        public static string MisconceptionId(WingProblem p, int selectedDeg)
        {
            if (p == null) return "unknown";
            if (p.band == 1 && selectedDeg == p.centerDeg && selectedDeg <= 90) return "center_equal";
            if (p.band == 1 && selectedDeg == p.centerDeg * 2 && selectedDeg <= 90) return "center_doubled";
            if (selectedDeg == 90 && p.targetDeg != 90) return "diameter_overgeneralized";
            if (p.band == 2 && selectedDeg != p.targetDeg) return "same_arc_position_changed";
            if (p.band == 3)
            {
                int givenRounded = (p.givenAngle2 + 1) / 2;
                if (selectedDeg == givenRounded) return "ratio_ignored";
                int reversed2 = p.givenAngle2 * p.ratioD / p.ratioN;
                if (selectedDeg * 2 == reversed2) return "ratio_reversed";
                return "arc_ratio_missed";
            }
            return selectedDeg < p.targetDeg ? "angle_too_small" : "angle_too_large";
        }

        public static string AngleText2(int angle2)
        {
            return angle2 % 2 == 0 ? (angle2 / 2) + "°" : (angle2 / 2) + ".5°";
        }

        static void ValidateBank(List<WingProblem> list)
        {
            if (list.Count < 300) throw new InvalidOperationException("문제 은행 300개 미만");
            var ids = new HashSet<string>();
            foreach (WingProblem p in list)
            {
                if (!ids.Add(p.id)) throw new InvalidOperationException("중복 문항 id: " + p.id);
                if (p.targetDeg < 5 || p.targetDeg > 90 || p.targetDeg % 5 != 0) throw new InvalidOperationException("답 눈금 범위 오류");
                if (p.band == 1 && p.centerDeg != p.targetDeg * 2) throw new InvalidOperationException("중심각 관계 오류");
                if (p.band == 2 && p.givenAngle2 != p.targetDeg * 2) throw new InvalidOperationException("같은 호 관계 오류");
                if (p.band == 3 && p.givenAngle2 * p.ratioN != p.targetDeg * 2 * p.ratioD) throw new InvalidOperationException("호의 비 관계 오류");
                // 오개념 역산값이 정답과 우연히 같아지는 경우는 분류하지 않는다.
                if (p.band == 1 && p.centerDeg == p.targetDeg) throw new InvalidOperationException("center_equal 우연 정답");
                if (p.band == 3 && p.givenAngle2 == p.targetDeg * 2) throw new InvalidOperationException("ratio_ignored 우연 정답");
            }
        }
    }
}
