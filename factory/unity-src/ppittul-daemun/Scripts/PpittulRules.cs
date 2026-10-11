// 삐뚤 대문 — 평행사변형의 각과 포함 관계를 11개 정수 걸쇠로 다루는 규칙.
//
// curriculum/2022-middle-math.json style_guide / expression_traps 반영:
// - 중학교 기본 종결형인 「~하시오」를 쓴다.
// - 각은 ∠A, ∠B와 70°처럼 표기한다.
// - 정사각형은 직사각형·마름모·평행사변형에 모두 포함된다.
// - 평행사변형의 두 대각선은 서로 다른 것을 이등분하지만, 길이가 같은 것은 직사각형일 때다.
// - 중2 범위이므로 근호·삼각비·「가정」「결론」은 쓰지 않는다.
//
// 오개념 역산:
// - adjacent_angles_equal: ∠B=x를 ∠A=x로 그대로 복사한다.
// - bisected_means_equal_diagonals: O에서 이등분됨을 두 대각선의 길이가 같다고 오해한다.
// - square_not_rectangle: 정사각형을 직사각형·마름모에서 제외한다.
// 정답 제거 → 40°..140° 범위 검사 → 중복 제거 → 부족분 보충 순서로 wrongDetents를 만든다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.PpittulDaemun
{
    public enum GatePromptType { DirectA, DirectC, AdjacentB, AdjacentD, EqualDiagonals, Rectangle, RectangleAndRhombus }

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

        public static GateProblem Practice(int step)
        {
            if (step == 0)
                return Make(4, GatePromptType.DirectA, 0, 1001, "첫 연습: ∠A=80°가 되도록 대문을 잠그시오");
            return Make(7, GatePromptType.AdjacentB, 0, 1002, "연결 연습: ∠B=70°가 되도록 대문을 잠그시오");
        }

        public static int[] BuildCourse(Random rng)
        {
            var v = new List<int>();
            for (int i = 0; i < DetentCount; i++) if (i != 5) v.Add(i);
            for (int i = v.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = v[i]; v[i] = v[j]; v[j] = t;
            }
            // 첫 8문은 여러 각 관계, 마지막 3문은 같은 90°를 서로 다른
            // 사각형 성질(대각선→직사각형→포함 관계)로 해석하는 학습 경로다.
            var course = new int[TargetCount];
            for (int i = 0; i < 8; i++) course[i] = v[i];
            course[8] = course[9] = course[10] = 5;
            return course;
        }

        public static GateProblem MakeCourseProblem(int target, int index, Random rng, int serial)
        {
            if (index == 8)
                return Make(5, GatePromptType.EqualDiagonals, 3, serial, null);
            if (index == 9)
                return Make(5, GatePromptType.Rectangle, 3, serial, null);
            if (index == 10)
                return Make(5, GatePromptType.RectangleAndRhombus, 3, serial, null);

            GatePromptType type;
            if (index < 4) type = index % 2 == 0 ? GatePromptType.DirectA : GatePromptType.AdjacentB;
            else type = index % 2 == 0 ? GatePromptType.DirectC : GatePromptType.AdjacentD;
            return Make(target, type, index < 4 ? 1 : 2, serial, null);
        }

        public static GateProblem Make(int target, GatePromptType type, int band, int serial, string fixedPrompt)
        {
            target = ClampDetent(target);
            int a = AngleFromDetent(target);
            int adjacent = 180 - a;
            string prompt;
            if (!string.IsNullOrEmpty(fixedPrompt)) prompt = fixedPrompt;
            else if (type == GatePromptType.EqualDiagonals)
                prompt = DirectOpeners[Math.Abs(serial / 100) % DirectOpeners.Length] +
                    " 평행사변형의 두 대각선의 길이가 같도록 대문을 잠그시오";
            else if (type == GatePromptType.Rectangle)
                prompt = DirectOpeners[Math.Abs(serial / 100) % DirectOpeners.Length] +
                    " 평행사변형이 직사각형이 되도록 대문을 잠그시오";
            else if (type == GatePromptType.RectangleAndRhombus)
                prompt = DirectOpeners[Math.Abs(serial / 100) % DirectOpeners.Length] +
                    " 네 변의 길이가 같은 평행사변형이 직사각형이면서 마름모가 되도록 잠그시오";
            else
            {
                string vertex = type == GatePromptType.DirectA ? "A" : type == GatePromptType.DirectC ? "C" : type == GatePromptType.AdjacentB ? "B" : "D";
                int shown = (type == GatePromptType.DirectA || type == GatePromptType.DirectC) ? a : adjacent;
                int variant = Math.Abs(serial) % DirectOpeners.Length;
                prompt = DirectOpeners[variant] + " ∠" + vertex + "=" + shown + "°가 되도록 " + DirectClosers[(variant * 3 + target) % DirectClosers.Length];
            }

            BuildWrongDetents(target, type, a, adjacent, out int[] wrong, out string[] misconception);
            return new GateProblem {
                id = "gate-" + serial + "-" + target + "-" + (int)type,
                prompt = prompt,
                answer = "∠A=" + a + "°",
                unitConcept = type == GatePromptType.RectangleAndRhombus ? "사각형의 포함 관계" :
                    type == GatePromptType.Rectangle ? "직사각형의 정의" :
                    type == GatePromptType.EqualDiagonals ? "직사각형의 대각선" :
                    (type == GatePromptType.AdjacentB || type == GatePromptType.AdjacentD) ? "평행사변형의 이웃각" : "평행사변형의 대각",
                type = type, targetDetent = target,
                angleA = a, angleB = adjacent, angleC = a, angleD = adjacent,
                allSidesEqual = type == GatePromptType.RectangleAndRhombus,
                band = band, wrongDetents = wrong, wrongMisconceptions = misconception
            };
        }

        static void BuildWrongDetents(int target, GatePromptType type, int a, int adjacent,
            out int[] wrong, out string[] misconception)
        {
            var ds = new List<int>();
            var ms = new List<string>();
            Action<int, string> add = (d, m) => {
                if (d < 0 || d >= DetentCount || d == target || ds.Contains(d)) return;
                ds.Add(d); ms.Add(m);
            };

            // 오개념 1: 이웃각도 같다고 착각해 문표의 B/D 숫자를 그대로 A에 둔다.
            if (type == GatePromptType.AdjacentB || type == GatePromptType.AdjacentD)
                add(DetentFromAngle(adjacent), "adjacent_angles_equal");
            else
                add(DetentFromAngle(180 - a), "adjacent_instead_of_opposite");

            // 오개념 2: 두 대각선이 서로를 이등분하면 길이도 같다고 착각해 비직각을 유지한다.
            int nonRight = target < 5 ? Math.Max(0, target - 1) : Math.Min(10, target + 1);
            add(nonRight, "bisected_means_equal_diagonals");

            // 오개념 3: 정사각형을 직사각형에서 제외해 90°를 피한다.
            if (type == GatePromptType.RectangleAndRhombus || type == GatePromptType.Rectangle || type == GatePromptType.EqualDiagonals)
                add(3, "square_not_rectangle");

            for (int d = 0; ds.Count < 4 && d < DetentCount; d++)
                add((target + 3 + d * 2) % DetentCount, "unrelated_detent");
            wrong = ds.ToArray(); misconception = ms.ToArray();
        }

        public static string IdentifyMisconception(GateProblem p, int selected)
        {
            if (p == null) return "unknown";
            for (int i = 0; i < p.wrongDetents.Length; i++)
                if (p.wrongDetents[i] == selected) return p.wrongMisconceptions[i];
            return "angle_relation_missed";
        }

        public static bool IsRectangle(int selectedDetent) => ClampDetent(selectedDetent) == 5;
        public static bool IsRhombus(GateProblem p) => p != null && p.allSidesEqual;
        public static bool IsCorrect(GateProblem p, int selectedDetent)
        {
            if (p == null) return false;
            if (p.type == GatePromptType.EqualDiagonals || p.type == GatePromptType.Rectangle)
                return IsRectangle(selectedDetent);
            if (p.type == GatePromptType.RectangleAndRhombus)
                return IsRectangle(selectedDetent) && IsRhombus(p);
            return ClampDetent(selectedDetent) == p.targetDetent;
        }

        public static List<MgfProblem> BuildBank()
        {
            var list = new List<MgfProblem>(352);
            // QA 표본도 실제 11문 코스와 동일한 BuildCourse→MakeCourseProblem 경로를 쓴다.
            // 32개 결정적 코스 × 11문 = 352개이며 매 코스에 대각선·직사각형·포함 관계가 모두 들어간다.
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
                        answer = gp.answer, answerNumeric = gp.targetDetent,
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
                int d = (int)p.answerNumeric;
                int a = AngleFromDetent(d);
                if (p.answer != "∠A=" + a + "°") { error = "answer mismatch at " + i; return false; }
                if (!seen.Add(p.id)) { error = "duplicate id at " + i; return false; }
                concepts.Add(p.unitConcept);
                if (p.prompt.Contains("√") || p.prompt.Contains("가정") || p.prompt.Contains("결론")) { error = "scope trap at " + i; return false; }
            }
            string[] required = { "평행사변형의 대각", "평행사변형의 이웃각", "직사각형의 대각선", "직사각형의 정의", "사각형의 포함 관계" };
            for (int i = 0; i < required.Length; i++)
                if (!concepts.Contains(required[i])) { error = "missing live concept " + required[i]; return false; }
            // 전체 파라미터 풀 × 오개념 오답 전수 검사: 정답 우연 일치·범위 밖·중복 0건.
            for (int target = 0; target < DetentCount; target++)
            for (int kind = 0; kind <= (int)GatePromptType.RectangleAndRhombus; kind++)
            {
                if (kind >= (int)GatePromptType.EqualDiagonals && target != 5) continue;
                var gp = Make(target, (GatePromptType)kind, 2, 900000 + target * 10 + kind, null);
                var wrongSeen = new HashSet<int>();
                if (gp.wrongDetents == null || gp.wrongDetents.Length < 2) { error = "distractor shortage " + target + "/" + kind; return false; }
                for (int w = 0; w < gp.wrongDetents.Length; w++)
                {
                    int d = gp.wrongDetents[w];
                    if (d == target || d < 0 || d >= DetentCount || !wrongSeen.Add(d))
                    { error = "distractor accidental truth " + target + "/" + kind + "/" + d; return false; }
                    if (string.IsNullOrEmpty(gp.wrongMisconceptions[w]))
                    { error = "missing misconceptionId " + target + "/" + kind; return false; }
                }
                if (gp.angleA != gp.angleC || gp.angleA + gp.angleB != 180 || gp.angleB != gp.angleD)
                { error = "parallelogram invariant " + target + "/" + kind; return false; }
                if (!IsCorrect(gp, target) || (target != 5 && IsCorrect(gp, 5)))
                { error = "judgment mismatch " + target + "/" + kind; return false; }
            }
            error = "";
            return bank.Count >= 300;
        }
    }
}
