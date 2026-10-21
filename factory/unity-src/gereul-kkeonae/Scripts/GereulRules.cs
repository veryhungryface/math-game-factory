// 게를 꺼내 — 중2 이등변삼각형의 성질, 줄 길이 구성형 문제 은행.
//
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 발문은 중학교 지시형 「~의 길이를 구하시오」로 통일한다.
// - 수와 단위는 띄어 쓴다(8 cm). 선분 위에 선을 그을 수 없는 UI에서는
//   중학교 style_guide의 허용 표현인 「선분 AB」를 쓰고, 각은 ∠B처럼 쓴다.
// - 중2 범위에서 근호·삼각비를 요구하지 않는다. 모든 답은 1~24 cm 정수다.
// - AB=AC와 AD가 꼭지각의 이등분선이라는 두 조건이 함께 있을 때만 BD=DC를 쓴다.
//
// 오개념 역산(misconceptionId):
// - adjacent_side: 같은 각의 맞은편이 아닌 밑변 길이를 옮긴다.
// - half_is_whole: 한쪽 밑변을 전체 밑변과 같다고 본다(2L을 연결).
// - halve_everything: 이등분선이 보이면 수리 대상과 무관하게 주어진 수를 반으로 만든다.
// - screen_bottom_base: 회전된 그림에서 화면 아래 변을 밑변으로 본다.
// 정답과 우연히 같아지는 오개념 값은 Diagnose에서 주장하지 않고 AuditBank가 전수 검사한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.GereulKkeonae
{
    [Serializable]
    public sealed class NetProblem
    {
        public string id;
        public int band;
        public int answerCm;
        public int auxiliary;
        public int equalSideCm;
        public int baseQuarter; // 0.25 cm = 1 내부 단위
        public int rotation;
        public string repairKind;
        public string prompt;
        public string evidence;
        public string unitConcept;
        public int adjacentWrong;
        public int wholeWrong;
        public int halveWrong;
    }

    public static class GereulRules
    {
        public const int MinLength = 1;
        public const int MaxLength = 24;
        public const int StartLives = 3;
        public const int RoundCount = 8;
        public const int MasteryNeeded = 6;
        public const float RunSeconds = 120f;

        static List<NetProblem> cached;

        public static NetProblem PracticeAngles()
        {
            return Make(1, 8, 4, "practice-angle", 0);
        }

        public static NetProblem PracticeBisector()
        {
            return Make(2, 6, 3, "practice-bisector", 0);
        }

        // 정답 L을 먼저 1..24에서 정하고, 보조 치수 1..5를 붙인다.
        // 생성 자체가 답 범위·이등변 조건·이등분 조건을 보장하므로 무작위 사후 필터가 없다.
        public static NetProblem Make(int band, int L, int auxiliary, string id, int rotation)
        {
            if (L < MinLength || L > MaxLength) throw new ArgumentOutOfRangeException(nameof(L));
            if (auxiliary < 1 || auxiliary > 5) throw new ArgumentOutOfRangeException(nameof(auxiliary));
            var p = new NetProblem
            {
                id = id,
                band = band,
                answerCm = L,
                auxiliary = auxiliary,
                rotation = ((rotation % 4) + 4) % 4,
            };
            if (band == 1)
            {
                int t = auxiliary + 2; // 3..7, BC=(t/4)L. 2L > BC를 항상 만족한다.
                p.equalSideCm = L;
                p.baseQuarter = t * L;
                p.repairKind = "AC";
                p.prompt = "삼각형 ABC에서 ∠B=∠C이고 선분 AB의 길이가 " + L + " cm, 선분 BC의 길이가 " + QuarterText(p.baseQuarter) + " cm일 때, 점선으로 나타낸 선분 AC의 길이를 구하시오.";
                p.evidence = "∠B=∠C이면 그 맞은편인 선분 AB와 선분 AC의 길이는 같다 · AC=" + L + " cm";
                p.unitConcept = "같은 두 내각의 맞은편 두 변";
                p.adjacentWrong = IntegralCm(p.baseQuarter) && p.baseQuarter / 4 != L ? p.baseQuarter / 4 : 0;
            }
            else if (band == 2)
            {
                p.equalSideCm = L + auxiliary;
                p.baseQuarter = 8 * L; // BC=2L
                p.repairKind = "BD";
                p.prompt = "AB=AC인 이등변삼각형 ABC에서 선분 AB와 선분 AC의 길이가 모두 " + p.equalSideCm + " cm이다. ∠A의 이등분선이 선분 BC와 만나는 점을 D라 할 때, 선분 BC의 길이가 " + (2 * L) + " cm이면 점선으로 나타낸 선분 BD의 길이를 구하시오.";
                p.evidence = "이등변삼각형의 꼭지각의 이등분선은 밑변을 이등분한다 · 선분 BD와 선분 DC의 길이는 " + L + " cm";
                p.unitConcept = "꼭지각의 이등분선과 밑변의 이등분";
                p.wholeWrong = 2 * L <= MaxLength ? 2 * L : 0;
            }
            else if (band == 3)
            {
                p.equalSideCm = L + auxiliary;
                p.baseQuarter = 2 * L; // BD=L/2 cm, quarter-unit exact because L/2 = 2L quarters
                p.repairKind = "BC";
                p.prompt = "AB=AC인 이등변삼각형 ABC에서 선분 AB와 선분 AC의 길이가 모두 " + p.equalSideCm + " cm이다. ∠A의 이등분선이 선분 BC와 만나는 점을 D라 할 때, 선분 BD의 길이가 " + HalfText(L) + " cm이면 점선으로 나타낸 선분 BC의 길이를 구하시오.";
                p.evidence = "선분 BD와 선분 DC의 길이는 같으므로 선분 BC의 길이는 선분 BD의 2배다 · BC=" + L + " cm";
                p.unitConcept = "한쪽 밑변에서 전체 밑변 복원";
                p.halveWrong = L % 2 == 0 && L / 2 != L ? L / 2 : 0;
            }
            else throw new ArgumentOutOfRangeException(nameof(band));
            return p;
        }

        static bool IntegralCm(int quarter) => quarter % 4 == 0;
        public static string QuarterText(int quarter)
        {
            int whole = quarter / 4, rem = quarter % 4;
            return rem == 0 ? whole.ToString() : rem == 1 ? whole + ".25" : rem == 2 ? whole + ".5" : whole + ".75";
        }
        public static string HalfText(int wholeAnswer) => wholeAnswer % 2 == 0 ? (wholeAnswer / 2).ToString() : (wholeAnswer / 2) + ".5";

        public static List<NetProblem> BuildBank()
        {
            if (cached != null) return cached;
            cached = new List<NetProblem>(360);
            for (int band = 1; band <= 3; band++)
            for (int L = MinLength; L <= MaxLength; L++)
            for (int a = 1; a <= 5; a++)
                cached.Add(Make(band, L, a, "gk-b" + band + "-l" + L.ToString("00") + "-a" + a, (L + a + band) & 3));
            AuditBank(cached);
            return cached;
        }

        public static List<NetProblem> BuildDeck(List<NetProblem> bank, Random rng)
        {
            var result = new List<NetProblem>(RoundCount);
            var usedAnswers = new HashSet<int>();
            for (int round = 0; round < RoundCount; round++)
            {
                int band = round < 3 ? 1 : round < 6 ? 2 : 3;
                int L;
                do L = rng.Next(MinLength, MaxLength + 1); while (usedAnswers.Contains(L));
                usedAnswers.Add(L);
                int a = rng.Next(1, 6);
                result.Add(Make(band, L, a, "run-" + (round + 1) + "-b" + band + "-l" + L + "-a" + a, rng.Next(4)));
            }
            return result;
        }

        // 시작값은 정답이 아닌 23개 눈금에서 균등하게 고른다.
        public static int WrongStart(NetProblem p, Random rng)
        {
            int k = rng.Next(MaxLength - 1) + 1;
            return k >= p.answerCm ? k + 1 : k;
        }

        public static string Diagnose(NetProblem p, int cutCm)
        {
            if (cutCm == p.answerCm) return "";
            if (p.band == 1 && p.adjacentWrong > 0 && cutCm == p.adjacentWrong) return "adjacent_side";
            if (p.band == 2 && p.wholeWrong > 0 && cutCm == p.wholeWrong) return "half_is_whole";
            if (p.band == 3 && p.halveWrong > 0 && cutCm == p.halveWrong) return "halve_everything";
            if (p.rotation != 0 && cutCm == p.equalSideCm && cutCm != p.answerCm) return "screen_bottom_base";
            return cutCm < p.answerCm ? "cord_too_short" : "cord_too_long";
        }

        public static List<MgfProblem> ToMgfBank(List<NetProblem> bank)
        {
            var result = new List<MgfProblem>(bank.Count);
            foreach (var p in bank)
            {
                result.Add(new MgfProblem
                {
                    id = p.id,
                    prompt = p.prompt,
                    choices = null,
                    answer = p.answerCm + " cm",
                    answerNumeric = p.answerCm,
                    unitConcept = p.unitConcept
                });
            }
            return result;
        }

        static void AuditBank(List<NetProblem> bank)
        {
            if (bank.Count != 360) throw new Exception("문제 은행은 정확히 360개여야 합니다: " + bank.Count);
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            var perBandAnswer = new int[4, 25];
            foreach (var p in bank)
            {
                if (!ids.Add(p.id) || !prompts.Add(p.prompt)) throw new Exception("중복 문항: " + p.id);
                if (p.answerCm < 1 || p.answerCm > 24) throw new Exception("답 범위 오류: " + p.id);
                if (p.equalSideCm <= 0 || p.baseQuarter <= 0) throw new Exception("길이 모델 오류: " + p.id);
                if (p.band == 1 && 8 * p.equalSideCm <= p.baseQuarter) throw new Exception("삼각부등식 오류: " + p.id);
                if (p.band == 2 && p.baseQuarter != p.answerCm * 8) throw new Exception("이등분 모델 오류: " + p.id);
                if (p.band == 3 && p.baseQuarter != p.answerCm * 2) throw new Exception("전체 복원 모델 오류: " + p.id);
                if (p.adjacentWrong == p.answerCm || p.wholeWrong == p.answerCm || p.halveWrong == p.answerCm)
                    throw new Exception("오개념 우연 정답: " + p.id);
                perBandAnswer[p.band, p.answerCm]++;
            }
            for (int band = 1; band <= 3; band++)
            for (int L = 1; L <= 24; L++)
                if (perBandAnswer[band, L] != 5) throw new Exception("균등 답 풀 오류: b" + band + " l" + L);
        }
    }
}
