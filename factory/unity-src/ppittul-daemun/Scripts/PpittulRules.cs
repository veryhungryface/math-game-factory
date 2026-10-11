// 삐뚤 대문 — 평행사변형의 각·대각선·포함 관계를 실제 문틀 조건 걸쇠로 판별한다.
//
// 중2 정본 반영:
// - 사각형의 성질은 대각선에 관한 성질을 중심으로 다룬다.
// - 평행사변형의 대각선은 서로를 이등분한다(AO=CO, BO=DO).
// - 직사각형의 대각선은 길이가 같고, 마름모의 대각선은 서로 수직이다.
// - 정사각형은 직사각형·마름모·평행사변형에 모두 포함된다.
// - 정답 수치와 실제 판정은 같은 GateProblem에서 나오며 answerNumeric은 내부 걸쇠 번호가 아니라 화면 각도다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.PpittulDaemun
{
    public enum GatePromptType
    {
        DirectA, DirectC, AdjacentB, AdjacentD,
        BisectAC, BisectBD, EqualDiagonals, PerpendicularDiagonals, RectangleAndRhombus
    }

    [Serializable]
    public sealed class GateProblem
    {
        public string id;
        public string prompt;
        public string answer;
        public string unitConcept;
        public GatePromptType type;
        public int targetDetent;
        public int angleA;
        public int angleB;
        public int angleC;
        public int angleD;
        public bool allSidesEqual;
        public int band;
        public int[] wrongDetents;
        public string[] wrongMisconceptions;
    }

    public static class PpittulRules
    {
        public const int MinAngle = 40;
        public const int MaxAngle = 140;
        public const int DetentCount = 11;
        public const int StartLives = 3;
        public const int TargetCount = 11;
        public const int MasteryFirst = 9;

        // 조건 판별 모드에서도 같은 물리 레일을 쓴다. 네 위치는 문틀의 실제 상태를 바꾼다.
        public const int AlwaysDetent = 1;
        public const int RightAngleDetent = 5;
        public const int EqualSidesDetent = 8;
        public const int BothDetent = 10;

        static readonly int[] PropertyDetents = { AlwaysDetent, RightAngleDetent, EqualSidesDetent, BothDetent };
        static readonly string[] DirectOpeners = {
            "산호 리본 문표를 읽고", "청록 문표를 확인하고", "별실 문표에 맞춰", "검은 문표의 지시에 따라",
            "황동 표찰을 읽고", "자수 번호표에 맞춰", "무대 문표를 확인하고", "퍼레이드 표찰을 읽고"
        };
        static readonly string[] DirectClosers = {
            "대문을 잠그시오", "알맞은 걸쇠에 잠그시오", "평행사변형 대문을 맞추시오", "A 눈금에 잠그시오",
            "문틀을 정확히 고정하시오", "대문을 한 번에 잠그시오", "알맞은 각으로 문을 맞추시오", "걸쇠를 선택하여 잠그시오"
        };

        public static int AngleFromDetent(int detent) => MinAngle + 10 * ClampDetent(detent);
        public static int DetentFromAngle(int angle) => ClampDetent((angle - MinAngle) / 10);
        public static int ClampDetent(int detent) => Math.Max(0, Math.Min(DetentCount - 1, detent));

        public static bool IsPropertyType(GatePromptType type) => type >= GatePromptType.BisectAC;
        public static bool IsPropertyProblem(GateProblem p) => p != null && IsPropertyType(p.type);
        public static bool IsPropertyDetent(int detent)
        {
            for (int i = 0; i < PropertyDetents.Length; i++) if (PropertyDetents[i] == detent) return true;
            return false;
        }
        public static int SnapPropertyDetent(int detent)
        {
            int best = PropertyDetents[0], distance = Math.Abs(detent - best);
            for (int i = 1; i < PropertyDetents.Length; i++)
            {
                int d = Math.Abs(detent - PropertyDetents[i]);
                if (d < distance) { best = PropertyDetents[i]; distance = d; }
            }
            return best;
        }
        static int PropertyRotation(int serial) => Math.Abs(serial) % PropertyDetents.Length;
        static int DetentForCondition(int serial, int conditionIndex)
            => PropertyDetents[(conditionIndex + PropertyRotation(serial)) % PropertyDetents.Length];
        static int ConditionIndexForDetent(GateProblem p, int detent)
        {
            detent = SnapPropertyDetent(detent);
            int physical = 0;
            for (int i = 0; i < PropertyDetents.Length; i++) if (PropertyDetents[i] == detent) { physical = i; break; }
            int serial = 0;
            if (p != null && !string.IsNullOrEmpty(p.id))
            {
                string[] parts = p.id.Split('-');
                if (parts.Length >= 4) int.TryParse(parts[parts.Length - 3], out serial);
            }
            return (physical - PropertyRotation(serial) + PropertyDetents.Length) % PropertyDetents.Length;
        }
        public static string ConditionLabel(GateProblem p, int detent)
        {
            int condition = ConditionIndexForDetent(p, detent);
            if (condition == 0) return "항상";
            if (condition == 1) return "∠A=90°";
            if (condition == 2) return "네 변 같음";
            return "둘 다";
        }
        public static int AngleForSelection(GateProblem p, int detent)
        {
            if (!IsPropertyProblem(p)) return AngleFromDetent(detent);
            int condition = ConditionIndexForDetent(p, detent);
            return condition == 1 || condition == 3 ? 90 : 70;
        }
        public static bool SelectionHasEqualSides(GateProblem p, int detent)
        {
            if (!IsPropertyProblem(p)) return p != null && p.allSidesEqual;
            int condition = ConditionIndexForDetent(p, detent);
            return condition == 2 || condition == 3;
        }

        public static GateProblem Practice(int step)
        {
            if (step == 0)
                return MakeAngle(4, GatePromptType.DirectA, 0, 1001, "첫 연습: ∠A=80°가 되도록 대문을 잠그시오");
            return MakeAngle(7, GatePromptType.AdjacentB, 0, 1002, "연결 연습: ∠B=70°가 되도록 대문을 잠그시오");
        }

        public static int[] BuildCourse(Random rng)
        {
            var values = new List<int>();
            for (int i = 0; i < DetentCount; i++) if (i != RightAngleDetent) values.Add(i);
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = values[i]; values[i] = values[j]; values[j] = t;
            }
            // 값은 각 관계 5문에만 쓰인다. 나머지 6문은 대각선·포함 관계 조건 걸쇠이고,
            // 90° 결론은 서로 떨어뜨려 같은 위치 반복으로 개념을 건너뛰지 못하게 한다.
            return new[] { values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7], values[8], values[9], RightAngleDetent };
        }

        public static GateProblem MakeCourseProblem(int target, int index, Random rng, int serial)
        {
            switch (index)
            {
                case 0: return MakeAngle(target, GatePromptType.DirectA, 1, serial, null);
                case 1: return MakeProperty(GatePromptType.BisectAC, 2, serial);
                case 2: return MakeAngle(target, GatePromptType.AdjacentB, 1, serial, null);
                case 3: return MakeProperty(GatePromptType.EqualDiagonals, 2, serial);
                case 4: return MakeAngle(target, GatePromptType.DirectC, 2, serial, null);
                case 5: return MakeProperty(GatePromptType.BisectBD, 2, serial);
                case 6: return MakeProperty(GatePromptType.PerpendicularDiagonals, 3, serial);
                case 7: return MakeAngle(target, GatePromptType.AdjacentD, 2, serial, null);
                case 8: return MakeProperty(GatePromptType.EqualDiagonals, 3, serial);
                case 9: return MakeAngle(target, GatePromptType.DirectA, 3, serial, null);
                default: return MakeProperty(GatePromptType.RectangleAndRhombus, 3, serial);
            }
        }

        static GateProblem MakeAngle(int target, GatePromptType type, int band, int serial, string fixedPrompt)
        {
            target = ClampDetent(target);
            int a = AngleFromDetent(target);
            int adjacent = 180 - a;
            string prompt = fixedPrompt;
            if (string.IsNullOrEmpty(prompt))
            {
                string vertex = type == GatePromptType.DirectA ? "A" : type == GatePromptType.DirectC ? "C" : type == GatePromptType.AdjacentB ? "B" : "D";
                int shown = type == GatePromptType.DirectA || type == GatePromptType.DirectC ? a : adjacent;
                int variant = Math.Abs(serial) % DirectOpeners.Length;
                prompt = DirectOpeners[variant] + " ∠" + vertex + "=" + shown + "°가 되도록 " + DirectClosers[(variant * 3 + target) % DirectClosers.Length];
            }
            BuildWrongDetents(target, type, a, adjacent, serial, out int[] wrong, out string[] misconceptions);
            return NewProblem(type, target, a, false, band, serial, prompt, "∠A=" + a + "°",
                type == GatePromptType.AdjacentB || type == GatePromptType.AdjacentD ? "평행사변형의 이웃각" : "평행사변형의 대각",
                wrong, misconceptions);
        }

        static GateProblem MakeProperty(GatePromptType type, int band, int serial)
        {
            int target, condition;
            bool equalSides;
            int targetAngle;
            string prompt, answer, concept;
            string opener = DirectOpeners[Math.Abs(serial / 100) % DirectOpeners.Length];
            if (type == GatePromptType.BisectAC)
            {
                condition = 0; target = DetentForCondition(serial, condition); equalSides = false; targetAngle = 70;
                prompt = opener + " 평행사변형에서 AO=CO가 성립하는 가장 알맞은 최소 조건 걸쇠에 잠그시오";
                answer = "항상 성립"; concept = "평행사변형의 대각선 이등분";
            }
            else if (type == GatePromptType.BisectBD)
            {
                condition = 0; target = DetentForCondition(serial, condition); equalSides = false; targetAngle = 70;
                prompt = opener + " 평행사변형에서 BO=DO가 성립하는 가장 알맞은 최소 조건 걸쇠에 잠그시오";
                answer = "항상 성립"; concept = "평행사변형의 대각선 이등분";
            }
            else if (type == GatePromptType.EqualDiagonals)
            {
                condition = 1; target = DetentForCondition(serial, condition); equalSides = false; targetAngle = 90;
                prompt = opener + " 평행사변형에서 AC=BD가 되는 가장 알맞은 최소 조건 걸쇠에 잠그시오";
                answer = "∠A=90°"; concept = "직사각형의 대각선";
            }
            else if (type == GatePromptType.PerpendicularDiagonals)
            {
                condition = 2; target = DetentForCondition(serial, condition); equalSides = true; targetAngle = 70;
                prompt = opener + " 평행사변형에서 AC⊥BD가 되는 가장 알맞은 최소 조건 걸쇠에 잠그시오";
                answer = "네 변 같음"; concept = "마름모의 대각선";
            }
            else
            {
                condition = 3; target = DetentForCondition(serial, condition); equalSides = true; targetAngle = 90;
                prompt = opener + " 직사각형이면서 마름모가 되는 가장 알맞은 조건 걸쇠에 잠그시오";
                answer = "∠A=90° + 네 변 같음"; concept = "사각형의 포함 관계";
            }
            BuildWrongDetents(target, type, targetAngle, 180 - targetAngle, serial, out int[] wrong, out string[] misconceptions);
            return NewProblem(type, target, targetAngle, equalSides, band, serial, prompt, answer, concept, wrong, misconceptions);
        }

        static GateProblem NewProblem(GatePromptType type, int target, int a, bool equalSides, int band, int serial,
            string prompt, string answer, string concept, int[] wrong, string[] misconceptions)
        {
            return new GateProblem {
                id = "gate-" + serial + "-" + target + "-" + (int)type,
                prompt = prompt, answer = answer, unitConcept = concept, type = type, targetDetent = target,
                angleA = a, angleB = 180 - a, angleC = a, angleD = 180 - a,
                allSidesEqual = equalSides, band = band, wrongDetents = wrong, wrongMisconceptions = misconceptions
            };
        }

        static void BuildWrongDetents(int target, GatePromptType type, int a, int adjacent, int serial,
            out int[] wrong, out string[] misconceptions)
        {
            var ds = new List<int>();
            var ms = new List<string>();
            Action<int, string> add = (d, m) => {
                if (d < 0 || d >= DetentCount || d == target || ds.Contains(d)) return;
                ds.Add(d); ms.Add(m);
            };

            if (type == GatePromptType.BisectAC || type == GatePromptType.BisectBD)
            {
                add(DetentForCondition(serial, 1), "only_rectangles_bisect");
                add(DetentForCondition(serial, 2), "only_rhombi_bisect");
                add(DetentForCondition(serial, 3), "only_squares_bisect");
            }
            else if (type == GatePromptType.EqualDiagonals)
            {
                add(DetentForCondition(serial, 0), "bisected_means_equal_diagonals");
                add(DetentForCondition(serial, 2), "perpendicular_means_equal_diagonals");
                add(DetentForCondition(serial, 3), "square_only_equal_diagonals");
            }
            else if (type == GatePromptType.PerpendicularDiagonals)
            {
                add(DetentForCondition(serial, 0), "all_parallelograms_perpendicular");
                add(DetentForCondition(serial, 1), "equal_diagonals_means_perpendicular");
                add(DetentForCondition(serial, 3), "square_only_perpendicular");
            }
            else if (type == GatePromptType.RectangleAndRhombus)
            {
                add(DetentForCondition(serial, 1), "rectangle_not_rhombus");
                add(DetentForCondition(serial, 2), "rhombus_not_rectangle");
                add(DetentForCondition(serial, 0), "parallelogram_is_square");
            }
            else
            {
                // 실제 각 관계 오개념 두 종류 이상을 각 문항에 보장한다.
                if (type == GatePromptType.AdjacentB || type == GatePromptType.AdjacentD)
                {
                    int copied = DetentFromAngle(adjacent);
                    add(copied, "adjacent_angles_equal");
                    int usedNinety = target < RightAngleDetent ? Math.Min(10, target + 2) : Math.Max(0, target - 2);
                    if (usedNinety == copied)
                        usedNinety = target < RightAngleDetent ? Math.Min(10, target + 3) : Math.Max(0, target - 3);
                    add(usedNinety, "used_90_sum_instead_of_180");
                }
                else
                {
                    int supplement = DetentFromAngle(180 - a);
                    add(supplement, "adjacent_instead_of_opposite");
                    int half = DetentFromAngle(Math.Max(MinAngle, a / 2));
                    if (half == target || half == supplement)
                        half = target < RightAngleDetent ? Math.Min(10, target + 3) : Math.Max(0, target - 3);
                    add(half, "halved_opposite_angle");
                }
                add(target == 0 ? 1 : target - 1, "read_neighbor_tick");
                add(target == 10 ? 9 : target + 1, "read_neighbor_tick");
            }

            wrong = ds.ToArray(); misconceptions = ms.ToArray();
        }

        public static string IdentifyMisconception(GateProblem p, int selected)
        {
            if (p == null) return "unknown";
            for (int i = 0; i < p.wrongDetents.Length; i++)
                if (p.wrongDetents[i] == selected) return p.wrongMisconceptions[i];
            return IsPropertyProblem(p) ? "property_condition_missed" : "angle_relation_missed";
        }

        public static bool IsCorrect(GateProblem p, int selectedDetent)
        {
            return p != null && ClampDetent(selectedDetent) == p.targetDetent;
        }

        public static List<MgfProblem> BuildBank()
        {
            var list = new List<MgfProblem>(352);
            // 실제 11문 코스와 같은 경로: 32개 결정적 코스 × 11문 = 352문항.
            for (int seed = 0; seed < 32; seed++)
            {
                var seeded = new Random(0x51A7 + seed * 104729);
                int[] course = BuildCourse(seeded);
                for (int index = 0; index < TargetCount; index++)
                {
                    int serial = 100000 + seed * 100 + index;
                    GateProblem gp = MakeCourseProblem(course[index], index, seeded, serial);
                    list.Add(new MgfProblem {
                        id = gp.id, prompt = gp.prompt, choices = null,
                        answer = gp.answer, answerNumeric = gp.angleA,
                        unitConcept = gp.unitConcept
                    });
                }
            }
            return list;
        }

        public static bool AuditBank(IList<MgfProblem> bank, out string error)
        {
            var seen = new HashSet<string>();
            var concepts = new HashSet<string>();
            for (int i = 0; i < bank.Count; i++)
            {
                var p = bank[i];
                int a = (int)p.answerNumeric;
                if (a < MinAngle || a > MaxAngle || a % 10 != 0) { error = "answerNumeric angle mismatch at " + i; return false; }
                if (p.answer.StartsWith("∠A=") && !p.answer.Contains("+") && p.answer != "∠A=" + a + "°")
                { error = "answer mismatch at " + i; return false; }
                if (!seen.Add(p.id)) { error = "duplicate id at " + i; return false; }
                concepts.Add(p.unitConcept);
                if (p.prompt.Contains("√") || p.prompt.Contains("가정") || p.prompt.Contains("결론")) { error = "scope trap at " + i; return false; }
            }
            string[] required = {
                "평행사변형의 대각", "평행사변형의 이웃각", "평행사변형의 대각선 이등분",
                "직사각형의 대각선", "마름모의 대각선", "사각형의 포함 관계"
            };
            for (int i = 0; i < required.Length; i++)
                if (!concepts.Contains(required[i])) { error = "missing live concept " + required[i]; return false; }

            // 실제 생성·판정 경로 전수 감사: 정답 우연 일치, 범위 밖, 중복, 유형별 오개념 부족 0건.
            for (int seed = 0; seed < 48; seed++)
            {
                var rng = new Random(7001 + seed * 7919);
                int[] course = BuildCourse(rng);
                for (int index = 0; index < TargetCount; index++)
                {
                    GateProblem gp = MakeCourseProblem(course[index], index, rng, 900000 + seed * 20 + index);
                    var wrongSeen = new HashSet<int>();
                    var misconceptionSeen = new HashSet<string>();
                    if (gp.wrongDetents == null || gp.wrongDetents.Length < 2) { error = "distractor shortage " + seed + "/" + index; return false; }
                    for (int w = 0; w < gp.wrongDetents.Length; w++)
                    {
                        int d = gp.wrongDetents[w];
                        if (d < 0 || d >= DetentCount || IsCorrect(gp, d) || !wrongSeen.Add(d))
                        { error = "distractor accidental truth " + seed + "/" + index + "/" + d; return false; }
                        if (string.IsNullOrEmpty(gp.wrongMisconceptions[w]))
                        { error = "missing misconceptionId " + seed + "/" + index; return false; }
                        if (gp.wrongMisconceptions[w] != "read_neighbor_tick") misconceptionSeen.Add(gp.wrongMisconceptions[w]);
                    }
                    if (misconceptionSeen.Count < 2) { error = "misconception variety " + seed + "/" + index; return false; }
                    if (gp.angleA != gp.angleC || gp.angleA + gp.angleB != 180 || gp.angleB != gp.angleD)
                    { error = "parallelogram invariant " + seed + "/" + index; return false; }
                    if (!IsCorrect(gp, gp.targetDetent)) { error = "judgment mismatch " + seed + "/" + index; return false; }
                }
            }
            error = "";
            return bank.Count >= 300;
        }
    }
}
