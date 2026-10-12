// 그림자 극장 — 중2 「도형의 닮음」 정수 문제 생성기.
//
// curriculum/2022-middle-math.json style_guide / expression_traps 반영:
// - 문제 발문은 「~도록 인형을 놓으시오」 명령형으로 통일한다.
// - 수와 단위는 띄어 쓴다(6 cm). 닮음비와 넓이의 비를 정확히 구분한다.
// - 그림자:인형 대응 순서를 데이터(p:q)에 보존하고 △OAB∽△OCD의 순서를 바꾸지 않는다.
// - 중2에는 √를 쓰지 않는다. 길이비 reduce(12,d), 넓이비 reduce(144,d²)를 int로만 계산한다.
// - 「가정」「결론」과 어림 표현은 생성하지 않는다.
//
// 오개념 역산 id:
// - area_ratio_used_as_length_ratio: 넓이의 비 P:Q를 길이의 비에 그대로 적용한다.
// - distance_equals_ratio_front: 기약 길이비의 앞항 p를 거리 d와 같은 수로 본다.
// - inverse_scale_direction: 거리가 멀수록 그림자가 작아지는 방향을 거꾸로 읽는다.
// - one_tick_estimate: 비례식을 계산하지 않고 정답 이웃 눈금으로 추측한다.
// 선택지는 없고 2~10 cm의 연속 레일이 답 공간이다. 아래 오개념 좌표는 오답 설명용이며,
// 정답 제거 → 범위 검사 → 중복 제거 순서로 보존한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.GeurimjaGeukjang
{
    public enum ShadowKind { LengthRatio, ReverseLength, AreaRatio }

    [Serializable]
    public sealed class ShadowProblem
    {
        public string id;
        public ShadowKind kind;
        public int band;
        public int answerD;
        public int p;
        public int q;
        public int puppetLength;
        public int shadowLength;
        public int wrongAreaLinear;
        public int wrongDistanceScale;
        public int wrongInverseDirection;
        public int wrongOneTick;
        public string scene;
        public string prompt;
        public string targetRatio;
        public string revealFormula;
        public string unitConcept;
        public bool rotated;
    }

    public static class ShadowRules
    {
        public const int L = 12;
        public const int MinD = 2;
        public const int MaxD = 10;
        public const int TargetActs = 8;
        public const int StartLives = 3;
        public const float RunSeconds = 110f;

        static readonly string[] Scenes = {
            "검은 새의 첫 날개", "톱니 달의 행진", "산맥 사이의 신호", "여덟 갈래 바람",
            "수평선의 종이배", "두 겹 나선 장치", "긴 목의 기계새", "주홍 표식의 밤",
            "구멍 난 별자리", "접힌 파도의 박자", "세모 연의 비행", "검은 꽃의 회전",
            "황동 축의 진자", "절지 숲의 통로", "네모 계단의 합주", "마지막 광선의 새"
        };

        // 첫 막 답을 9회마다 정확히 한 번씩 쓴다. 2→3→…→10 순환 입력과는
        // 한 주기에서 한 번만 만나므로 고정·순환 정책의 우연율이 정확히 1/9이다.
        static readonly int[] BalancedFirstD = { 6, 2, 9, 4, 10, 3, 8, 5, 7 };

        public static int Gcd(int a, int b)
        {
            a = Math.Abs(a); b = Math.Abs(b);
            while (b != 0) { int t = a % b; a = b; b = t; }
            return Math.Max(1, a);
        }

        static void Reduce(int a, int b, out int p, out int q)
        {
            int g = Gcd(a, b); p = a / g; q = b / g;
        }

        static int ValidWrong(int value, int answer, params int[] prior)
        {
            if (value < MinD || value > MaxD || value == answer) return 0;
            for (int i = 0; i < prior.Length; i++) if (value == prior[i]) return 0;
            return value;
        }

        static int RoundRatio(int numerator, int denominator)
        {
            // 양의 정수 반올림. 발문에서 어림을 묻는 것이 아니라 오개념 위치를 재현하는 내부 계산이다.
            return (numerator * 2 + denominator) / (2 * denominator);
        }

        public static ShadowProblem Practice()
        {
            return Make(ShadowKind.LengthRatio, 6, 0, "고정 연습");
        }

        public static ShadowProblem Make(ShadowKind kind, int d, int variant, string sceneOverride = null)
        {
            if (d < MinD || d > MaxD) throw new ArgumentOutOfRangeException(nameof(d));
            int lp, lq; Reduce(L, d, out lp, out lq);
            int ap, aq; Reduce(L * L, d * d, out ap, out aq);
            int k = 1 + Math.Abs(variant) % 4;
            int h = d * k;
            int s = L * k;
            string scene = sceneOverride ?? Scenes[Math.Abs(variant) % Scenes.Length];
            string prompt;
            string concept;
            string target;
            string formula;
            int band;
            if (kind == ShadowKind.LengthRatio)
            {
                band = 1;
                target = lp + ":" + lq;
                prompt = "장면 ‘" + scene + "’에서 그림자와 인형의 길이의 비를 " + target + "로 맞추도록 인형을 놓으시오.";
                formula = "그림자:인형 = OC:OA = 12:" + d + " = " + target;
                concept = "닮음비 직접 구성";
            }
            else if (kind == ShadowKind.ReverseLength)
            {
                band = 2;
                target = lp + ":" + lq;
                prompt = "장면 ‘" + scene + "’에서 인형의 길이는 " + h + " cm이고 그림자의 길이는 " + s + " cm이다. 이 그림자가 생기도록 인형을 놓으시오.";
                formula = "그림자:인형 = " + s + ":" + h + " = " + target + " = 12:" + d;
                concept = "대응변의 길이로 닮음비 역추적";
            }
            else
            {
                band = 3;
                target = ap + ":" + aq;
                prompt = "장면 ‘" + scene + "’에서 그림자와 인형의 넓이의 비를 " + target + "로 맞추도록 인형을 놓으시오.";
                formula = "넓이의 비 = 144:" + (d * d) + " = " + target + "  ·  길이의 비 = " + lp + ":" + lq;
                concept = "닮은 평면도형의 넓이의 비";
            }

            // 오개념 1: 넓이비 P:Q를 길이비로 그대로 쓰면 d≈12Q/P가 된다.
            // 정확한 정수가 아니어도 학생이 레일의 가장 가까운 눈금에 놓는 행동을 재현한다.
            int areaLinear = kind == ShadowKind.AreaRatio ? RoundRatio(L * aq, ap) : 0;
            areaLinear = ValidWrong(areaLinear, d);
            // 오개념 2: 비의 앞항 p를 거리와 같은 수로 읽는다.
            int distanceScale = ValidWrong(kind == ShadowKind.AreaRatio ? ap : lp, d, areaLinear);
            // 오개념 3: 거리와 그림자 크기의 증감 방향을 거꾸로 읽는다.
            // 12-d는 2~10 레일 안에서 대칭 위치이며, 중심 6은 끝 눈금으로 방향 오류를 드러낸다.
            int inverseDirection = ValidWrong(d == 6 ? MaxD : L - d, d, areaLinear, distanceScale);
            // 계산 없이 가장 가까운 눈금을 고르는 행동. 앞 좌표와 겹치면 반대 이웃,
            // 그래도 겹치면 남은 첫 눈금을 택해 문항마다 실제 진단 좌표 2개 이상을 보장한다.
            int oneTick = ValidWrong(d < MaxD ? d + 1 : d - 1, d, areaLinear, distanceScale, inverseDirection);
            if (oneTick == 0) oneTick = ValidWrong(d > MinD ? d - 1 : d + 1, d, areaLinear, distanceScale, inverseDirection);
            if (oneTick == 0)
                for (int x = MinD; x <= MaxD && oneTick == 0; x++)
                    oneTick = ValidWrong(x, d, areaLinear, distanceScale, inverseDirection);

            return new ShadowProblem {
                id = "gg-" + (kind == ShadowKind.LengthRatio ? "len" : kind == ShadowKind.ReverseLength ? "rev" : "area") + "-d" + d + "-v" + variant,
                kind = kind, band = band, answerD = d, p = kind == ShadowKind.AreaRatio ? ap : lp,
                q = kind == ShadowKind.AreaRatio ? aq : lq, puppetLength = h, shadowLength = s,
                wrongAreaLinear = areaLinear, wrongDistanceScale = distanceScale,
                wrongInverseDirection = inverseDirection, wrongOneTick = oneTick,
                scene = scene, prompt = prompt, targetRatio = target, revealFormula = formula,
                unitConcept = concept, rotated = (variant & 1) == 1
            };
        }

        public static bool IsCorrect(ShadowProblem p, int d)
        {
            if (p == null || d < MinD || d > MaxD) return false;
            if (p.kind == ShadowKind.AreaRatio)
                return (long)L * L * p.q == (long)d * d * p.p;
            return (long)L * p.q == (long)d * p.p;
        }

        public static string MisconceptionId(ShadowProblem p, int d)
        {
            if (p == null || d == p.answerD) return "";
            if (d == p.wrongAreaLinear && p.wrongAreaLinear > 0) return "area_ratio_used_as_length_ratio";
            if (d == p.wrongDistanceScale && p.wrongDistanceScale > 0) return "distance_equals_ratio_front";
            if (d == p.wrongInverseDirection && p.wrongInverseDirection > 0) return "inverse_scale_direction";
            if (d == p.wrongOneTick && p.wrongOneTick > 0) return "one_tick_estimate";
            return d < p.answerD ? "puppet_too_close" : "puppet_too_far";
        }

        public static string ActualRatio(ShadowProblem p, int d)
        {
            int a = p != null && p.kind == ShadowKind.AreaRatio ? L * L : L;
            int b = p != null && p.kind == ShadowKind.AreaRatio ? d * d : d;
            int x, y; Reduce(a, b, out x, out y);
            return x + ":" + y;
        }

        // 현재 비 a:b와 목표 p:q를 같은 종류(길이/넓이)끼리만 외적으로 비교한다.
        // 오답 설명도 IsCorrect와 동일한 정수 경로를 쓰므로 넓이비 문항에서 12:d를
        // 목표 넓이비와 직접 비교하는 잘못된 안내가 생기지 않는다.
        public static int CompareActualToTarget(ShadowProblem p, int d)
        {
            if (p == null || d < MinD || d > MaxD) return 0;
            long a = p.kind == ShadowKind.AreaRatio ? (long)L * L : L;
            long b = p.kind == ShadowKind.AreaRatio ? (long)d * d : d;
            long delta = a * p.q - b * p.p;
            return delta == 0 ? 0 : delta > 0 ? 1 : -1;
        }

        public static int FirstD(int runSerial, int offset)
        {
            int i = ((runSerial - 1 + offset) % BalancedFirstD.Length + BalancedFirstD.Length) % BalancedFirstD.Length;
            return BalancedFirstD[i];
        }

        public static List<ShadowProblem> RunDeck(Random rng, int firstD)
        {
            var deck = new List<ShadowProblem>(TargetActs);
            for (int i = 0; i < TargetActs; i++)
            {
                int d = i == 0 ? firstD : MinD + rng.Next(MaxD - MinD + 1);
                ShadowKind kind = i <= 2 ? ShadowKind.LengthRatio : i <= 4 ? ShadowKind.ReverseLength : ShadowKind.AreaRatio;
                deck.Add(Make(kind, d, rng.Next(100000) + i * 31));
            }
            return deck;
        }

        public static List<MgfProblem> BuildBank()
        {
            // 실제 RunDeck과 같은 Make/IsCorrect 경로다. 9개 정답 위치 × 16개 장면 × 3유형 = 432문항.
            var result = new List<MgfProblem>(432);
            for (int d = MinD; d <= MaxD; d++)
            for (int v = 0; v < Scenes.Length; v++)
            {
                Add(result, Make(ShadowKind.LengthRatio, d, v));
                Add(result, Make(ShadowKind.ReverseLength, d, v + 32));
                Add(result, Make(ShadowKind.AreaRatio, d, v + 64));
            }
            return result;
        }

        static void Add(List<MgfProblem> result, ShadowProblem p)
        {
            if (!IsCorrect(p, p.answerD)) throw new InvalidOperationException("문제 생성기 정수 판정 불일치: " + p.id);
            // 답은 위치 구성값이므로 choices=null이다. 하단 선택지 버튼은 존재하지 않는다.
            result.Add(new MgfProblem {
                id = p.id, prompt = p.prompt, choices = null,
                answer = p.answerD + " cm", answerNumeric = p.answerD, unitConcept = p.unitConcept
            });
        }

        public static void ExhaustiveInvariantCheck()
        {
            // 전체 매개변수 풀 × 9개 입력을 검사한다. 정답은 정확히 하나이고 모든 오개념 좌표는 거짓이어야 한다.
            for (int d = MinD; d <= MaxD; d++)
            for (int v = 0; v < Scenes.Length; v++)
            foreach (ShadowKind kind in (ShadowKind[])Enum.GetValues(typeof(ShadowKind)))
            {
                var p = Make(kind, d, v + (int)kind * 32);
                int truths = 0;
                for (int x = MinD; x <= MaxD; x++) if (IsCorrect(p, x)) truths++;
                if (truths != 1 || !IsCorrect(p, d)) throw new InvalidOperationException("정답 비유일: " + p.id);
                int[] wrongs = { p.wrongAreaLinear, p.wrongDistanceScale, p.wrongInverseDirection, p.wrongOneTick };
                int validWrongs = 0;
                for (int i = 0; i < wrongs.Length; i++)
                {
                    if (wrongs[i] <= 0) continue;
                    validWrongs++;
                    if (IsCorrect(p, wrongs[i])) throw new InvalidOperationException("오개념 우연 정답: " + p.id);
                    for (int j = 0; j < i; j++)
                        if (wrongs[j] == wrongs[i]) throw new InvalidOperationException("오개념 좌표 중복: " + p.id);
                }
                if (validWrongs < 2) throw new InvalidOperationException("오개념 좌표 2개 미만: " + p.id);
            }
        }
    }
}
