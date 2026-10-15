// 종이새 출격 — 이등변삼각형 날개 생성기와 정수 판정.
//
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 중학교 문제 발문은 「~의 크기를 구하시오」「~를 고치시오」로 끝낸다.
// - AB=AC이면 같은 두 변 사이의 ∠A가 꼭지각이고, ∠B와 ∠C가 밑각이다.
// - 외각은 어느 반직선을 연장했는지 점의 순서까지 표시한다(C-B-D가 한 직선).
// - 중2 범위이므로 √·삼각비를 생성하지 않는다. 각은 모두 정수 도(°)다.
// - 답을 고르는 보기는 없다. 학생이 실제 AC 날개뼈를 돌려 ∠A를 만든다.
//
// 오개념 역산(misconceptionId):
// - subtract_one_base: 180-b (밑각을 한 번만 뺌)
// - apex_equals_base: b (밑각을 꼭지각으로 그대로 씀)
// - split_after_one_base: (180-b)/2 (남은 각을 다시 반으로 나눔)
// - exterior_as_vertex: e-90 (외각을 밑각으로 바로 취급한 뒤 임의로 90을 뺌)
// - rotated_position_guess: 회전된 그림의 화면 위치만 보고 꼭지각을 찾음
// 각 진단값은 20°~140°의 짝수 눈금에 있고 정답과 다를 때만 사용한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.JongisaeChulgyeok
{
    [Serializable]
    public sealed class WingProblem
    {
        public string id;
        public int band;
        public int qIndex;
        public int q;
        public int b;
        public int e;
        public int startIndex;
        public int startAngle;
        public int rotation;
        public int wording;
        public string givenVertex;
        public string prompt;
        public string condition;
        public string unitConcept;
        public string misconceptionA;
        public string misconceptionB;
    }

    public static class JongisaeRules
    {
        public const int MinAngle = 20;
        public const int MaxAngle = 140;
        public const int Step = 2;
        public const int TickCount = 61;
        public const int RunBirds = 8;
        public const int StartLives = 3;
        public const int RequiredFirst = 6;
        public const int RequiredExteriorFirst = 2;
        public const float RunSeconds = 100f;

        public static int AngleFromIndex(int index) => MinAngle + Step * Math.Max(0, Math.Min(TickCount - 1, index));
        public static int IndexFromAngle(int angle) => Math.Max(0, Math.Min(TickCount - 1, (angle - MinAngle + Step / 2) / Step));

        public static WingProblem Practice()
        {
            return MakeProblem(25, 0, 0, 10, 0, 0); // q=70°, b=55°, start=40°
        }

        // qIndex를 먼저 균등하게 뽑고 startIndex는 qIndex를 제외한 60칸에서 뽑는다.
        // 사후 필터링 없이 정답≠시작이라는 제약을 구조적으로 만족한다.
        public static WingProblem MakeRandom(Random rng, int band, int serial)
        {
            int qIndex = rng.Next(TickCount);
            int rawStart = rng.Next(TickCount - 1);
            int startIndex = rawStart >= qIndex ? rawStart + 1 : rawStart;
            int rotation = band == 2 ? 1 + (serial % 3) : (band == 3 ? (serial & 1) * 2 : 0);
            int wording = rng.Next(2);
            return MakeProblem(qIndex, band, rotation, startIndex, wording, serial);
        }

        public static WingProblem MakeProblem(int qIndex, int band, int rotation, int startIndex, int wording, int serial)
        {
            qIndex = Math.Max(0, Math.Min(TickCount - 1, qIndex));
            startIndex = Math.Max(0, Math.Min(TickCount - 1, startIndex));
            if (startIndex == qIndex) startIndex = (startIndex + 1) % TickCount;
            int q = AngleFromIndex(qIndex);
            int b = (180 - q) / 2;
            int e = 180 - b;
            string prompt;
            string condition;
            string concept;
            string givenVertex = (serial & 1) == 0 ? "B" : "C";

            if (band == 3)
            {
                condition = "AB=AC · C-B-D는 한 직선 · 목표 ∠ABD=" + e + "°";
                prompt = wording == 0
                    ? "AB=AC이고 BC를 B 너머 D까지 연장하였다. 외각 ∠ABD=" + e + "°일 때, ∠A의 크기를 구하시오."
                    : "C-B-D가 한 직선이고 AB=AC이다. 목표 외각 ∠ABD=" + e + "°가 되도록 꼭지각 ∠A의 크기를 구하시오.";
                concept = "외각과 밑각으로 이등변삼각형의 꼭지각 역추적";
            }
            else if (band == 2)
            {
                condition = "AB=AC · 목표 ∠" + givenVertex + "=" + b + "° · 현재 ∠A=" + AngleFromIndex(startIndex) + "°";
                prompt = wording == 0
                    ? "AB=AC인 날개가 ∠A=" + AngleFromIndex(startIndex) + "°로 잘못 펼쳐져 있다. 목표 ∠" + givenVertex + "=" + b + "°를 만족하도록 ∠A를 고치시오."
                    : "회전된 이등변삼각형에서 AB=AC이고 목표 ∠" + givenVertex + "=" + b + "°이다. 현재 " + AngleFromIndex(startIndex) + "°인 꼭지각 ∠A를 고치시오.";
                concept = "회전된 이등변삼각형의 꼭지각과 밑각";
            }
            else
            {
                condition = "AB=AC · 목표 ∠" + givenVertex + "=" + b + "°";
                prompt = wording == 0
                    ? "AB=AC이고 ∠" + givenVertex + "=" + b + "°일 때, 꼭지각 ∠A의 크기를 구하시오."
                    : "길이가 같은 두 변이 AB와 AC이고 목표 밑각 ∠" + givenVertex + "=" + b + "°이다. ∠A의 크기를 구하시오.";
                concept = "이등변삼각형의 두 밑각";
            }

            return new WingProblem
            {
                id = (band == 0 ? "practice" : "wing-b" + band + "-q" + qIndex + "-w" + wording + "-r" + rotation),
                band = band,
                qIndex = qIndex,
                q = q,
                b = b,
                e = e,
                startIndex = startIndex,
                startAngle = AngleFromIndex(startIndex),
                rotation = rotation,
                wording = wording,
                givenVertex = givenVertex,
                prompt = prompt,
                condition = condition,
                unitConcept = concept,
                misconceptionA = "subtract_one_base",
                misconceptionB = band == 3 ? "exterior_as_vertex" : "apex_equals_base"
            };
        }

        public static List<WingProblem> RunDeck(int seed)
        {
            var rng = new Random(seed);
            var deck = new List<WingProblem>(RunBirds);
            for (int i = 0; i < 2; i++) deck.Add(MakeRandom(rng, 1, i));
            for (int i = 0; i < 3; i++) deck.Add(MakeRandom(rng, 2, i));
            for (int i = 0; i < 3; i++) deck.Add(MakeRandom(rng, 3, i));
            return deck;
        }

        // 판정은 그림용 float가 아니라 손을 놓은 정수 눈금 인덱스의 완전 일치다.
        public static bool IsCorrect(WingProblem p, int inputIndex)
        {
            return p != null && inputIndex == p.qIndex;
        }

        static bool DiagnosticValue(WingProblem p, int value, int inputAngle)
        {
            return value >= MinAngle && value <= MaxAngle && value % Step == 0 && value != p.q && inputAngle == value;
        }

        public static string MisconceptionId(WingProblem p, int inputIndex)
        {
            if (p == null) return "unknown_angle_relation";
            int a = AngleFromIndex(inputIndex);
            if (DiagnosticValue(p, 180 - p.b, a)) return "subtract_one_base";
            if (DiagnosticValue(p, p.b, a)) return "apex_equals_base";
            int split = 180 - p.b;
            if (split % 2 == 0 && DiagnosticValue(p, split / 2, a)) return "split_after_one_base";
            if (p.band == 3 && DiagnosticValue(p, p.e - 90, a)) return "exterior_as_vertex";
            if (p.band == 2) return "rotated_position_guess";
            return "unknown_angle_relation";
        }

        public static string WrongReason(WingProblem p, string id, int inputIndex)
        {
            int a = AngleFromIndex(inputIndex);
            switch (id)
            {
                case "subtract_one_base": return "밑각은 두 개다. " + p.b + "°를 두 번 빼야 한다";
                case "apex_equals_base": return "같은 것은 두 밑각이다. 꼭지각은 따로 구해야 한다";
                case "split_after_one_base": return "남은 두 각이 아니라, 같은 밑각 두 개를 먼저 더한다";
                case "exterior_as_vertex": return "외각 " + p.e + "°의 보각이 밑각 " + p.b + "°다";
                case "rotated_position_guess": return "화면 위치가 아니라 같은 두 변 AB·AC 사이가 꼭지각이다";
                default: return "현재 ∠A=" + a + "°. 두 밑각과 합해 180°인지 확인하시오";
            }
        }

        public static string Equation(WingProblem p)
        {
            if (p == null) return "";
            if (p.band == 3) return p.e + "°+" + p.b + "°=180°  →  " + p.q + "°+2×" + p.b + "°=180°";
            return p.b + "°+" + p.b + "°+" + p.q + "°=180°";
        }

        // 실제 플레이 생성기 MakeProblem을 그대로 써 61답×3밴드×2발문 = 366개를 만든다.
        public static List<MgfProblem> BuildBank()
        {
            var list = new List<MgfProblem>(366);
            for (int wording = 0; wording < 2; wording++)
            for (int band = 1; band <= 3; band++)
            for (int qIndex = 0; qIndex < TickCount; qIndex++)
            {
                int startIndex = (qIndex + 7 + band * 11 + wording * 13) % TickCount;
                if (startIndex == qIndex) startIndex = (startIndex + 1) % TickCount;
                int rotation = band == 2 ? 1 + ((qIndex + wording) % 3) : (band == 3 ? (qIndex & 1) * 2 : 0);
                var p = MakeProblem(qIndex, band, rotation, startIndex, wording, qIndex + wording);
                list.Add(new MgfProblem
                {
                    id = "bank-" + wording + "-" + band + "-" + qIndex,
                    prompt = p.prompt,
                    choices = new string[0],
                    answer = p.q + "°",
                    answerNumeric = p.q,
                    unitConcept = p.unitConcept
                });
            }
            return list;
        }

        // 183개 기본 조건 × 모든 진단 공식을 전수 검사한다. 우연히 정답이 된 오개념은 진단에서 제외되어야 한다.
        public static int AuditMath()
        {
            int errors = 0;
            var prompts = new HashSet<string>();
            var bank = BuildBank();
            if (bank.Count < 300) errors++;
            for (int i = 0; i < bank.Count; i++) if (!prompts.Add(bank[i].prompt + "|" + bank[i].answer)) errors++;
            for (int band = 1; band <= 3; band++)
            for (int qIndex = 0; qIndex < TickCount; qIndex++)
            {
                var p = MakeProblem(qIndex, band, band == 2 ? 1 : 0, (qIndex + 1) % TickCount, 0, qIndex);
                if (p.q + 2 * p.b != 180) errors++;
                if (p.e + p.b != 180) errors++;
                if (p.q < MinAngle || p.q > MaxAngle || p.q % Step != 0) errors++;
                int[] candidates = { 180 - p.b, p.b, (180 - p.b) / 2, p.e - 90 };
                for (int k = 0; k < candidates.Length; k++)
                {
                    int v = candidates[k];
                    if (v == p.q && DiagnosticValue(p, v, v)) errors++;
                }
                for (int input = 0; input < TickCount; input++)
                    if (IsCorrect(p, input) != (AngleFromIndex(input) == p.q)) errors++;
            }
            return errors;
        }
    }
}
