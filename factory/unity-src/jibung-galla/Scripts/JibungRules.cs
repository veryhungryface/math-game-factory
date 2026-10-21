// 지붕 갈라 — 중2 이등변삼각형의 성질 정수 문제 은행과 판정 정본.
//
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 발문은 중학교 지시형 「~하시오」로 통일한다.
// - AB=AC, ∠A, ∠B, 각의 이등분선, 수직이등분을 교과서 용어 그대로 쓴다.
// - 각은 정수와 °로 쓰며 중2 범위 밖인 근호·삼각비를 생성하지 않는다.
// - 증명 문맥에서도 「가정」「결론」이라는 낱말을 쓰지 않는다.
//
// 오개념 역산(misconceptionId):
// - whole_apex: 꼭지각 전체 2h를 반각으로 그대로 사용한다.
// - base_as_half: 밑각 90-h를 꼭지각의 반각으로 사용한다(h=45 우연 일치 시 분류하지 않음).
// - one_base_then_half: (180-base)/2를 사용한다(정수 관계일 때만 분류).
// - trace_error_line: 잘못 그은 AE의 e를 그대로 덧긋는다.
// 정답 h와 우연히 같은 값은 Diagnose에서 오개념으로 주장하지 않으며 AuditBank가 전수 검사한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.JibungGalla
{
    [Serializable]
    public sealed class RoofProblem
    {
        public string id;
        public int band;
        public int h;
        public int apex;
        public int baseAngle;
        public int errorAngle;
        public string prompt;
        public string answer;
        public string evidence;
        public string unitConcept;
    }

    public static class JibungRules
    {
        public const int StartLives = 3;
        public const int RoundCount = 10;
        public const int MasteryNeeded = 8;
        public const float RunSeconds = 110f;
        public const int MinHalf = 25;
        public const int MaxHalf = 64;
        static readonly int[] ErrorAngles = { 4, 8, 12, 16, 20, 24 };
        static List<RoofProblem> cached;

        public static RoofProblem Practice()
        {
            return Make(1, 30, 0, "practice-h30");
        }

        public static RoofProblem Make(int band, int h, int errorAngle, string id)
        {
            if (h < MinHalf || h > MaxHalf) throw new ArgumentOutOfRangeException(nameof(h));
            if (band < 1 || band > 3) throw new ArgumentOutOfRangeException(nameof(band));
            if (band == 3 && Array.IndexOf(ErrorAngles, errorAngle) < 0) throw new ArgumentOutOfRangeException(nameof(errorAngle));
            int apex = h * 2;
            int baseAngle = 90 - h;
            var p = new RoofProblem
            {
                id = id,
                band = band,
                h = h,
                apex = apex,
                baseAngle = baseAngle,
                errorAngle = band == 3 ? errorAngle : 0,
                answer = "∠BAD=" + h + "°인 내부의 선 AD",
            };
            if (band == 1)
            {
                p.prompt = "AB=AC인 이등변삼각형 ABC에서 ∠A=" + apex + "°이다. ∠A의 이등분선을 그으시오.";
                p.evidence = "∠BAD=∠CAD=" + h + "°";
                p.unitConcept = "이등변삼각형의 꼭지각 이등분";
            }
            else if (band == 2)
            {
                p.prompt = "AB=AC인 이등변삼각형 ABC에서 ∠B=" + baseAngle + "°이다. ∠A의 이등분선을 그으시오.";
                p.evidence = "∠A=180°-" + baseAngle + "°×2=" + apex + "°, ∠BAD=" + h + "°";
                p.unitConcept = "두 밑각과 꼭지각의 이등분";
            }
            else
            {
                p.prompt = "AB=AC, ∠B=" + baseAngle + "°이고 잘못 그은 선 AE에서 ∠BAE=" + errorAngle + "°이다. AE를 대신할 ∠A의 이등분선을 그으시오.";
                p.evidence = "90°-" + baseAngle + "°=" + h + "°, ∠BAD=∠CAD=" + h + "°";
                p.unitConcept = "잘못 그은 이등분선의 교정";
            }
            return p;
        }

        public static List<RoofProblem> BuildBank()
        {
            if (cached != null) return cached;
            cached = new List<RoofProblem>(320);
            for (int h = MinHalf; h <= MaxHalf; h++)
            {
                cached.Add(Make(1, h, 0, "jg-direct-h" + h));
                cached.Add(Make(2, h, 0, "jg-reverse-h" + h));
                for (int i = 0; i < ErrorAngles.Length; i++)
                    cached.Add(Make(3, h, ErrorAngles[i], "jg-repair-h" + h + "-e" + ErrorAngles[i]));
            }
            AuditBank(cached);
            return cached;
        }

        public static List<RoofProblem> BuildDeck(Random rng)
        {
            var halves = new List<int>(40);
            for (int h = MinHalf; h <= MaxHalf; h++) halves.Add(h);
            for (int i = halves.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = halves[i]; halves[i] = halves[j]; halves[j] = t;
            }
            var deck = new List<RoofProblem>(RoundCount);
            for (int round = 0; round < RoundCount; round++)
            {
                int band = round < 3 ? 1 : round < 7 ? 2 : 3;
                int h = halves[round];
                int e = band == 3 ? ErrorAngles[rng.Next(ErrorAngles.Length)] : 0;
                deck.Add(Make(band, h, e, "run-r" + (round + 1) + "-h" + h + (band == 3 ? "-e" + e : "")));
            }
            return deck;
        }

        public static string Diagnose(RoofProblem p, int selectedAngle)
        {
            if (selectedAngle == p.h) return "";
            if (selectedAngle == p.apex) return "whole_apex";
            if (p.baseAngle != p.h && selectedAngle == p.baseAngle) return "base_as_half";
            int twiceCandidate = 180 - p.baseAngle;
            if ((twiceCandidate & 1) == 0 && selectedAngle == twiceCandidate / 2 && selectedAngle != p.h) return "one_base_then_half";
            if (p.band == 3 && selectedAngle == p.errorAngle) return "trace_error_line";
            return selectedAngle < p.h ? "cut_too_close_to_ab" : "cut_too_close_to_ac";
        }

        public static List<MgfProblem> ToMgfBank(List<RoofProblem> bank)
        {
            var result = new List<MgfProblem>(bank.Count);
            foreach (var p in bank)
            {
                result.Add(new MgfProblem
                {
                    id = p.id,
                    prompt = p.prompt,
                    choices = null,
                    answer = p.answer,
                    answerNumeric = p.h,
                    unitConcept = p.unitConcept
                });
            }
            return result;
        }

        static void AuditBank(List<RoofProblem> bank)
        {
            if (bank.Count != 320) throw new Exception("문제 은행은 정확히 320개여야 합니다: " + bank.Count);
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            int[] direct = new int[65], reverse = new int[65], repair = new int[65];
            foreach (var p in bank)
            {
                if (!ids.Add(p.id)) throw new Exception("중복 id: " + p.id);
                if (!prompts.Add(p.prompt + "|" + p.answer)) throw new Exception("중복 문항: " + p.id);
                if (p.apex != 2 * p.h || p.baseAngle != 90 - p.h || p.apex + 2 * p.baseAngle != 180)
                    throw new Exception("각 관계 오류: " + p.id);
                if (p.h < MinHalf || p.h > MaxHalf) throw new Exception("반각 범위 오류: " + p.id);
                if (p.band == 3 && (p.errorAngle <= 0 || p.errorAngle >= p.apex || p.errorAngle == p.h))
                    throw new Exception("오류선 조건 오류: " + p.id);
                if (p.answer.IndexOf(p.h + "°", StringComparison.Ordinal) < 0) throw new Exception("답 표기 오류: " + p.id);
                if (p.prompt.Contains("√") || p.prompt.Contains("가정") || p.prompt.Contains("결론"))
                    throw new Exception("표현 함정 위반: " + p.id);
                if (p.band == 1) direct[p.h]++;
                else if (p.band == 2) reverse[p.h]++;
                else repair[p.h]++;
            }
            for (int h = MinHalf; h <= MaxHalf; h++)
                if (direct[h] != 1 || reverse[h] != 1 || repair[h] != 6)
                    throw new Exception("은행 분포 오류: h=" + h);
        }
    }
}
