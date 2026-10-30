// 팝콘 핀볼 — 중3 원주각 정수 눈금 생성기와 전수 검산.
// curriculum/2022-middle-math.json 표현 함정:
// - 각은 70°처럼 붙여 쓰고, 길이는 6 cm처럼 수와 단위를 띄어 쓴다.
// - 중심각·원주각·호·현을 정확히 구분한다.
// - 문제 발문은 중학교 기본 종결형인 "구하시오/나타내시오"를 쓴다.
// - 중3 원주각 단원에서는 제곱근 계산을 섞지 않는다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.PopcornPinball
{
    [Serializable]
    public sealed class PinballProblem
    {
        public string id;
        public int band;
        public int answerTick;     // 1..17, 실제 채점의 유일한 정답
        public int answerAngle;    // 5 * answerTick
        public int centralAngle;   // 10 * answerTick
        public int splitU;
        public int splitV;
        public int referenceTick;
        public string prompt;
        public string concept;
    }

    [Serializable]
    public struct DiagnosticAim
    {
        public string misconceptionId;
        public int tick;
        public DiagnosticAim(string id, int value) { misconceptionId = id; tick = value; }
    }

    public static class PopcornRules
    {
        public const int TickCount = 17;
        public const int Goal = 8;
        public const int RequiredFirstCorrect = 6;
        public const int StartLives = 3;

        public static PinballProblem Practice()
        {
            return MakeBand1(8, "practice-80-40");
        }

        public static PinballProblem MakeBand1(int a, string id)
        {
            int beta = 10 * a;
            return new PinballProblem
            {
                id = id,
                band = 1,
                answerTick = a,
                answerAngle = 5 * a,
                centralAngle = beta,
                prompt = "원 O에서 호 AB에 대한 중심각 ∠AOB의 크기가 " + beta
                    + "°일 때, 원주각 ∠APB의 크기를 구하고 포신으로 나타내시오.",
                concept = "중심각과 원주각의 관계"
            };
        }

        public static PinballProblem MakeBand2(int a, int j, string id)
        {
            int u = 5 * j;
            int v = 10 * a - u;
            if (j < 1 || j > 2 * a - 1) throw new ArgumentOutOfRangeException(nameof(j));
            return new PinballProblem
            {
                id = id,
                band = 2,
                answerTick = a,
                answerAngle = 5 * a,
                centralAngle = 10 * a,
                splitU = u,
                splitV = v,
                prompt = "원 O에서 점 T는 작은 호 AB 위에 있고 ∠AOT=" + u + "°, ∠TOB=" + v
                    + "°이다. 작은 호 AB에 대한 원주각 ∠APB의 크기를 구하고 포신으로 나타내시오.",
                concept = "나뉜 중심각과 원주각"
            };
        }

        public static PinballProblem MakeBand3(int a, string id)
        {
            if (a < 1 || a > TickCount) throw new ArgumentOutOfRangeException(nameof(a));
            int angle = 5 * a;
            return new PinballProblem
            {
                id = id,
                band = 3,
                answerTick = a,
                answerAngle = angle,
                centralAngle = 10 * a,
                referenceTick = a,
                prompt = "같은 원 O에서 ∠AQB와 ∠APB는 모두 작은 호 AB에 대한 원주각이다. ∠AQB="
                    + angle + "°일 때, ∠APB의 크기를 구하고 포신으로 나타내시오.",
                concept = "같은 호에 대한 원주각"
            };
        }

        // 답 눈금 a를 먼저 독립 균등 추출한 뒤 유형 파라미터를 만든다.
        public static PinballProblem MakeForRound(int round, Random rng, int serial)
        {
            int a = rng.Next(1, TickCount + 1);
            if (round <= 3) return MakeBand1(a, "run-" + serial + "-b1-a" + a);
            if (round <= 6)
            {
                int j = rng.Next(1, 2 * a);
                return MakeBand2(a, j, "run-" + serial + "-b2-a" + a + "-j" + j);
            }
            return MakeBand3(a, "run-" + serial + "-b3-a" + a);
        }

        public static bool IsCorrect(PinballProblem p, int selectedTick)
        {
            return selectedTick == p.answerTick;
        }

        // 오답 버튼은 없지만 오개념에서 역산한 제출값을 피드백과 전수 검산에 쓴다.
        // 후처리: 정답 제거 → 범위 검사 → 중복 제거 → 부족분 보충.
        public static List<DiagnosticAim> DiagnosticCandidates(PinballProblem p)
        {
            var raw = new List<DiagnosticAim>();
            raw.Add(new DiagnosticAim("center_equals_inscribed", p.centralAngle / 5));
            raw.Add(new DiagnosticAim("center_doubled_instead", (2 * p.centralAngle) / 5));
            if (p.band == 2)
            {
                if (p.splitU % 10 == 0) raw.Add(new DiagnosticAim("one_split_half_u", p.splitU / 10));
                if (p.splitV % 10 == 0) raw.Add(new DiagnosticAim("one_split_half_v", p.splitV / 10));
            }
            if (p.band == 3)
                raw.Add(new DiagnosticAim("same_arc_changed", p.answerTick == TickCount ? p.answerTick - 1 : p.answerTick + 1));

            var result = new List<DiagnosticAim>();
            var used = new HashSet<int>();
            for (int i = 0; i < raw.Count; i++)
            {
                int tick = raw[i].tick;
                if (tick == p.answerTick || tick < 1 || tick > TickCount || !used.Add(tick)) continue;
                result.Add(raw[i]);
            }
            for (int offset = 1; result.Count < 2 && offset <= TickCount; offset++)
            {
                int tick = ((p.answerTick + offset - 1) % TickCount) + 1;
                if (tick == p.answerTick || !used.Add(tick)) continue;
                result.Add(new DiagnosticAim("adjacent_tick_check", tick));
            }
            return result;
        }

        public static string Diagnose(PinballProblem p, int selectedTick)
        {
            var list = DiagnosticCandidates(p);
            for (int i = 0; i < list.Count; i++)
                if (list[i].tick == selectedTick) return list[i].misconceptionId;
            return selectedTick > p.answerTick ? "aimed_too_wide" : "aimed_too_narrow";
        }

        // 실제 게임과 같은 Make/IsCorrect 경로에서 323개 고유 문항을 만든다.
        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(323);
            int serial = 1;
            for (int a = 1; a <= TickCount; a++) Add(bank, MakeBand1(a, "bank-" + serial++));
            for (int a = 1; a <= TickCount; a++)
                for (int j = 1; j <= 2 * a - 1; j++) Add(bank, MakeBand2(a, j, "bank-" + serial++));
            for (int a = 1; a <= TickCount; a++) Add(bank, MakeBand3(a, "bank-" + serial++));
            return bank;
        }

        static void Add(List<MgfProblem> bank, PinballProblem p)
        {
            bank.Add(new MgfProblem
            {
                id = p.id,
                prompt = p.prompt,
                choices = null,
                answer = p.answerAngle + "°",
                answerNumeric = p.answerAngle,
                unitConcept = p.concept
            });
        }

        public static void ValidateAll()
        {
            int accidentalTrue = 0;
            int checkedProblems = 0;
            var prompts = new HashSet<string>();
            List<MgfProblem> bank = BuildBank();
            if (bank.Count < 300) throw new InvalidOperationException("문제 은행 300개 미만");
            for (int i = 0; i < bank.Count; i++)
            {
                if (!prompts.Add(bank[i].prompt)) throw new InvalidOperationException("중복 발문: " + bank[i].prompt);
                if (bank[i].answerNumeric < 5 || bank[i].answerNumeric > 85 || ((int)bank[i].answerNumeric) % 5 != 0)
                    throw new InvalidOperationException("정답 눈금 범위 오류");
            }
            for (int a = 1; a <= TickCount; a++)
            {
                var p1 = MakeBand1(a, "v1");
                ValidateOne(p1, ref accidentalTrue); checkedProblems++;
                for (int j = 1; j <= 2 * a - 1; j++)
                {
                    var p2 = MakeBand2(a, j, "v2");
                    if (p2.splitU + p2.splitV != p2.centralAngle) throw new InvalidOperationException("분할 중심각 합 오류");
                    ValidateOne(p2, ref accidentalTrue); checkedProblems++;
                }
                var p3 = MakeBand3(a, "v3");
                if (p3.referenceTick != p3.answerTick)
                    throw new InvalidOperationException("같은 호 원주각 불변 오류");
                ValidateOne(p3, ref accidentalTrue); checkedProblems++;
            }
            if (accidentalTrue != 0) throw new InvalidOperationException("오개념 우연 정답 " + accidentalTrue + "건");
            if (checkedProblems != bank.Count) throw new InvalidOperationException("은행/검산 개수 불일치");
        }

        static void ValidateOne(PinballProblem p, ref int accidentalTrue)
        {
            int solutions = 0;
            for (int k = 1; k <= TickCount; k++) if (IsCorrect(p, k)) solutions++;
            if (solutions != 1) throw new InvalidOperationException("유일해 오류: " + p.id);
            var ds = DiagnosticCandidates(p);
            if (ds.Count < 2) throw new InvalidOperationException("오개념 후보 부족: " + p.id);
            for (int i = 0; i < ds.Count; i++) if (IsCorrect(p, ds[i].tick)) accidentalTrue++;
        }
    }
}
