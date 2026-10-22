// 햄스터 대회전 — 중2 이등변삼각형 각도 문제의 정수 생성·판정 정본.
//
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 발문은 중학교 기본 종결형 「~의 크기를 구하시오」로 통일한다.
// - 같은 두 변, 꼭지각·밑각, 각의 이등분선과 교점을 문장과 기호에 함께 적는다.
// - 각은 정수와 °로 쓰며 중2 범위 밖인 √·삼각비를 만들지 않는다.
// - 정당화 장면에서도 교육과정 제한어인 「가정」「결론」은 쓰지 않는다.
// - 꼭짓점 대응 배열에서 등변 표식·발문·목표 각을 함께 생성한다.
//
// 오개념 역산(misconceptionId):
// - base_sum_as_one: 두 밑각의 합 180-A를 한 밑각으로 사용한다.
// - one_base_subtraction: 180-밑각 하나만 계산해 꼭지각으로 사용한다.
// - whole_apex_as_half: 꼭지각 전체를 이등분된 한 각으로 사용한다.
// - half_base_angle: 주어진 밑각을 반으로 나눈다.
// - screen_bottom_vertex: 그림의 아래쪽을 밑각으로 읽은 수치 대용값이다.
// 값 생성 뒤 정답 제거 → 범위 검사 → 중복 제거 순으로 후보를 만든다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.HamsterDaehoejeon
{
    [Serializable]
    public sealed class AngleProblem
    {
        public string id;
        public int band;
        public int answer;
        public int apexAngle;
        public int baseAngle;
        public int allowedMin;
        public int allowedMax;
        public int allowedStep;
        public int diagramRotation;
        public string apex;
        public string baseLeft;
        public string baseRight;
        public string divider;
        public string equalSides;
        public string targetAngleName;
        public string vertexMap;
        public string prompt;
        public string answerText;
        public string evidence;
        public string unitConcept;
        public int[] misconceptionValues;
        public string[] misconceptionIds;
    }

    public static class HamsterRules
    {
        public const int RoundCount = 8;
        public const int MasteryNeeded = 6;
        public const int StartLives = 3;
        public const float RunSeconds = 100f;

        static readonly string[,] Maps =
        {
            { "A", "B", "C", "D" },
            { "P", "Q", "R", "S" },
            { "X", "Y", "Z", "M" },
            { "E", "F", "G", "H" },
            { "J", "K", "L", "N" },
            { "U", "V", "W", "T" }
        };

        static List<AngleProblem> cached;

        public static AngleProblem FirstPractice() => Make(1, 50, 0, "practice-40-to-70"); // answer 70, apex 40
        public static AngleProblem BridgePractice() => Make(3, 22, 0, "bridge-58-to-32"); // answer 32, base 58

        public static AngleProblem Make(int band, int k, int mapIndex, string id)
        {
            if (band < 1 || band > 3) throw new ArgumentOutOfRangeException(nameof(band));
            if (k < 0 || k >= 60) throw new ArgumentOutOfRangeException(nameof(k));
            if (mapIndex < 0 || mapIndex >= Maps.GetLength(0)) throw new ArgumentOutOfRangeException(nameof(mapIndex));

            var p = new AngleProblem
            {
                id = id,
                band = band,
                apex = Maps[mapIndex, 0],
                baseLeft = Maps[mapIndex, 1],
                baseRight = Maps[mapIndex, 2],
                divider = Maps[mapIndex, 3],
                diagramRotation = ((mapIndex * 37 + k * 11 + band * 19) % 151) - 75,
            };
            p.equalSides = p.apex + p.baseLeft + "=" + p.apex + p.baseRight;
            p.vertexMap = p.apex + "→꼭짓점," + p.baseLeft + "·" + p.baseRight + "→밑변," + p.divider + "→이등분선 교점";

            if (band == 1)
            {
                p.answer = 20 + k;
                p.baseAngle = p.answer;
                p.apexAngle = 180 - 2 * p.baseAngle;
                p.allowedMin = 20; p.allowedMax = 79; p.allowedStep = 1;
                p.targetAngleName = "∠" + p.baseLeft;
                p.prompt = p.equalSides + "인 이등변삼각형 " + p.apex + p.baseLeft + p.baseRight + "에서 ∠" + p.apex + "=" + p.apexAngle + "°이다. ∠" + p.baseLeft + "의 크기를 구하시오.";
                p.evidence = "두 밑각은 같으므로 (180°−" + p.apexAngle + "°)÷2=" + p.answer + "°";
                p.unitConcept = "꼭지각에서 두 밑각 구하기";
            }
            else if (band == 2)
            {
                p.answer = 20 + 2 * k;
                p.apexAngle = p.answer;
                p.baseAngle = (180 - p.apexAngle) / 2;
                p.allowedMin = 20; p.allowedMax = 138; p.allowedStep = 2;
                p.targetAngleName = "∠" + p.apex;
                p.prompt = p.equalSides + "인 이등변삼각형 " + p.apex + p.baseLeft + p.baseRight + "에서 ∠" + p.baseRight + "=" + p.baseAngle + "°이다. ∠" + p.apex + "의 크기를 구하시오.";
                p.evidence = "두 밑각은 " + p.baseAngle + "°이므로 180°−" + p.baseAngle + "°×2=" + p.answer + "°";
                p.unitConcept = "밑각에서 꼭지각 거꾸로 구하기";
            }
            else
            {
                p.answer = 10 + k;
                p.apexAngle = 2 * p.answer;
                p.baseAngle = 90 - p.answer;
                p.allowedMin = 10; p.allowedMax = 69; p.allowedStep = 1;
                string leftTarget = mapIndex % 2 == 0 ? p.baseLeft : p.baseRight;
                p.targetAngleName = "∠" + leftTarget + p.apex + p.divider;
                p.prompt = p.equalSides + "인 이등변삼각형 " + p.apex + p.baseLeft + p.baseRight + "에서 ∠" + p.baseLeft + "=" + p.baseAngle + "°이다. ∠" + p.apex + "의 이등분선과 선분 " + p.baseLeft + p.baseRight + "의 교점을 " + p.divider + "라고 할 때, " + p.targetAngleName + "의 크기를 구하시오.";
                p.evidence = "∠" + p.apex + "=180°−" + p.baseAngle + "°×2=" + p.apexAngle + "°, 한 반각은 " + p.answer + "°";
                p.unitConcept = "꼭지각의 이등분선과 반각";
            }
            p.answerText = p.answer + "°";
            BuildMisconceptions(p);
            return p;
        }

        static void BuildMisconceptions(AngleProblem p)
        {
            var values = new List<int>(5);
            var ids = new List<string>(5);
            if (p.band == 1)
            {
                AddCandidate(p, values, ids, 180 - p.apexAngle, "base_sum_as_one");
                AddCandidate(p, values, ids, p.apexAngle, "screen_bottom_vertex");
                AddCandidate(p, values, ids, p.apexAngle / 2, "halve_given_apex");
                AddCandidate(p, values, ids, p.answer + (p.answer < p.allowedMax ? 1 : -1), "off_by_one_detent");
            }
            else if (p.band == 2)
            {
                AddCandidate(p, values, ids, 180 - p.baseAngle, "one_base_subtraction");
                AddCandidate(p, values, ids, p.baseAngle, "base_as_apex");
                AddCandidate(p, values, ids, 2 * p.baseAngle, "base_sum_as_apex");
                AddCandidate(p, values, ids, p.answer + (p.answer < p.allowedMax ? 2 : -2), "off_by_one_detent");
            }
            else
            {
                AddCandidate(p, values, ids, p.apexAngle, "whole_apex_as_half");
                AddCandidate(p, values, ids, p.baseAngle / 2, "half_base_angle");
                AddCandidate(p, values, ids, p.baseAngle, "base_as_half");
                AddCandidate(p, values, ids, 180 - p.baseAngle, "one_base_subtraction");
                AddCandidate(p, values, ids, p.answer + (p.answer < p.allowedMax ? 1 : -1), "off_by_one_detent");
            }
            // 공식 오개념 값이 입력 범위 밖이거나 정답과 우연히 겹치는 경계 문항의 부족분을
            // 실제 허용 눈금에서 보충한다. 화면 위치로 밑각을 판단하는 오개념의 수치 표본이며
            // 정답 제거·범위 검사·중복 제거를 같은 AddCandidate가 다시 강제한다.
            for (int offset = 1; values.Count < 2 && offset < 60; offset++)
            {
                AddCandidate(p, values, ids, p.answer - p.allowedStep * offset, "screen_bottom_vertex");
                if (values.Count < 2) AddCandidate(p, values, ids, p.answer + p.allowedStep * offset, "screen_bottom_vertex");
            }
            if (values.Count < 2) throw new Exception("오개념 후보 2개 미만: " + p.id);
            p.misconceptionValues = values.ToArray();
            p.misconceptionIds = ids.ToArray();
        }

        // 오답 후처리: 정답 제거 → 범위 검사 → 중복 제거 → 부족분 보충은 위의 off_by_one 후보 → 순서 보존.
        static void AddCandidate(AngleProblem p, List<int> values, List<string> ids, int value, string id)
        {
            if (value == p.answer) return;
            if (value < p.allowedMin || value > p.allowedMax) return;
            if ((value - p.allowedMin) % p.allowedStep != 0) return;
            if (values.Contains(value)) return;
            values.Add(value); ids.Add(id);
        }

        public static string Diagnose(AngleProblem p, int selected)
        {
            if (selected == p.answer) return "";
            for (int i = 0; i < p.misconceptionValues.Length; i++)
                if (p.misconceptionValues[i] == selected) return p.misconceptionIds[i];
            return selected < p.answer ? "angle_too_small" : "angle_too_large";
        }

        public static List<AngleProblem> BuildBank()
        {
            if (cached != null) return cached;
            cached = new List<AngleProblem>(1080);
            for (int band = 1; band <= 3; band++)
                for (int k = 0; k < 60; k++)
                    for (int map = 0; map < Maps.GetLength(0); map++)
                        cached.Add(Make(band, k, map, "hd-b" + band + "-k" + k + "-m" + map));
            AuditBank(cached);
            return cached;
        }

        public static List<AngleProblem> BuildDeck(Random rng)
        {
            var answers = new List<int>(60);
            for (int i = 0; i < 60; i++) answers.Add(i);
            for (int i = answers.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = answers[i]; answers[i] = answers[j]; answers[j] = t;
            }
            var deck = new List<AngleProblem>(RoundCount);
            for (int round = 0; round < RoundCount; round++)
            {
                int band = round < 3 ? 1 : round < 5 ? 2 : 3;
                int map = rng.Next(Maps.GetLength(0));
                int k = answers[round];
                deck.Add(Make(band, k, map, "run-r" + (round + 1) + "-b" + band + "-k" + k + "-m" + map));
            }
            return deck;
        }

        public static List<MgfProblem> ToMgfBank(List<AngleProblem> bank)
        {
            var result = new List<MgfProblem>(bank.Count);
            foreach (var p in bank)
            {
                result.Add(new MgfProblem
                {
                    id = p.id,
                    prompt = p.prompt,
                    choices = null,
                    answer = p.answerText,
                    answerNumeric = p.answer,
                    unitConcept = p.unitConcept
                });
            }
            return result;
        }

        static void AuditBank(List<AngleProblem> bank)
        {
            if (bank.Count != 1080) throw new Exception("문제 은행 수 오류: " + bank.Count);
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            int[,] counts = new int[4, 60];
            foreach (var p in bank)
            {
                if (!ids.Add(p.id)) throw new Exception("중복 id: " + p.id);
                if (!prompts.Add(p.prompt + "|" + p.answerText)) throw new Exception("중복 문항: " + p.id);
                if (p.apexAngle <= 0 || p.baseAngle <= 0 || p.apexAngle + 2 * p.baseAngle != 180)
                    throw new Exception("삼각형 내각 오류: " + p.id);
                if (p.band == 1 && p.answer != p.baseAngle) throw new Exception("1단계 답 오류: " + p.id);
                if (p.band == 2 && p.answer != p.apexAngle) throw new Exception("2단계 답 오류: " + p.id);
                if (p.band == 3 && p.answer * 2 != p.apexAngle) throw new Exception("3단계 반각 오류: " + p.id);
                if (p.answer < p.allowedMin || p.answer > p.allowedMax || (p.answer - p.allowedMin) % p.allowedStep != 0)
                    throw new Exception("답 눈금 오류: " + p.id);
                if ((p.allowedMax - p.allowedMin) / p.allowedStep + 1 != 60) throw new Exception("눈금 수 오류: " + p.id);
                if (p.prompt.Contains("√") || p.prompt.Contains("가정") || p.prompt.Contains("결론") || !p.prompt.EndsWith("크기를 구하시오.", StringComparison.Ordinal))
                    throw new Exception("표현 규칙 위반: " + p.id);
                if (p.misconceptionValues == null || p.misconceptionValues.Length < 2 || p.misconceptionValues.Length != p.misconceptionIds.Length)
                    throw new Exception("오개념 후보 오류: " + p.id);
                var seen = new HashSet<int>();
                for (int i = 0; i < p.misconceptionValues.Length; i++)
                {
                    int v = p.misconceptionValues[i];
                    if (v == p.answer || v < p.allowedMin || v > p.allowedMax || (v - p.allowedMin) % p.allowedStep != 0 || !seen.Add(v))
                        throw new Exception("오개념 우연 정답/범위/중복 오류: " + p.id + " / " + v);
                }
                int k = p.band == 1 ? p.answer - 20 : p.band == 2 ? (p.answer - 20) / 2 : p.answer - 10;
                counts[p.band, k]++;
            }
            for (int band = 1; band <= 3; band++)
                for (int k = 0; k < 60; k++)
                    if (counts[band, k] != 6) throw new Exception("은행 균등 분포 오류: band=" + band + " k=" + k);
        }
    }
}
