// 제비 특급 — 문제 모델·은행·판정(정수 연산만).
//
// 교과서 표현 함정(curriculum/2022-middle-math.json style_guide·expression_traps + 기획서 phrasing_rule):
//  - 발문은 중학교 지시형 「~의 길이를 구하시오」로 통일한다(「~해 봅시다」 금지).
//  - 접선의 길이는 「원 밖의 점에서 접점까지」다. 원 밖의 점과 중심 사이의 거리(OP)나 반지름이 아니다.
//  - 접점과 공통 바깥점을 발문에서 정의하고, 내접원은 변과 접점의 대응 순서(AB↔D, BC↔E, CA↔F)를 명시한다.
//  - 수와 단위는 「9 cm」처럼 띄운다. 이번 범위에는 근호(√)·피타고라스 접선 길이·현의 성질을 넣지 않는다.
//  - 도면은 개략도다. 화면의 픽셀 길이는 정답 t 와 독립이며, 판정은 지급 눈금 정수와 정답 정수의 비교뿐이다.
//
// 은행: 세 종류 × 정답 t=1..24 × 변형 5 = 360문항. 정답 t 를 먼저 정하고 조건을 역으로 만든다
// (Pair: PA=t / Sum: PA+PB=2t / Incircle: AD=u, AB=u+t). 각 종류에서 t 마다 같은 개수라 정답 분포가 균등하다.
using System.Collections.Generic;
using System.Text;

namespace Mgf.JebiTeukgeup
{
    public enum StationKind { Pair = 1, Sum = 2, Incircle = 3 }

    public sealed class JebiProblem
    {
        public string id;
        public StationKind kind;
        public int t;          // 정답(접선의 길이, cm). 1..24
        public int r;          // Pair·Sum: 실제 조건인 반지름(2..6). PB 와 무관
        public int u;          // Incircle: AD(=AF) 의 길이(2..6)
        public int variant;
        public string prompt;
        public bool practice;

        public const int W = 8; // Incircle: 꼭짓점 C 의 접선 길이 CE=CF (도형 생성용, 학생에게 보이지 않음)

        public string Target => kind == StationKind.Incircle ? "BE" : "PB";
        public int SideAB => u + t;
        public int SumPAPB => 2 * t;

        public bool IsCorrect(int value) => value == t;

        public string ConceptName
        {
            get
            {
                switch (kind)
                {
                    case StationKind.Pair: return "원 밖의 한 점에서 그은 두 접선의 길이는 같다";
                    case StationKind.Sum: return "두 접선의 길이의 합에서 한 접선의 길이 구하기";
                    default: return "삼각형의 내접원과 접선의 길이";
                }
            }
        }
    }

    /// <summary>SplitMix64 난수(정답 t·배치 추출용). System.Random 의 시드 상관·편향을 피한다. Next(n) 은 기각 표본으로 정확히 균등.</summary>
    public sealed class JebiRng
    {
        ulong state;
        public JebiRng(ulong seed) { state = seed; }
        public ulong NextU64()
        {
            ulong z = (state += 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
        public int Next(int maxExclusive)
        {
            ulong n = (ulong)maxExclusive, limit = ulong.MaxValue - ulong.MaxValue % n;
            ulong r;
            do { r = NextU64(); } while (r >= limit);
            return (int)(r % n);
        }
        public int Next(int minInclusive, int maxExclusive) => minInclusive + Next(maxExclusive - minInclusive);
    }

    public static class JebiRules
    {
        public const int MaxLen = 24;
        public const int Stations = 8;
        public const int PassFirst = 6;
        public const int StartRings = 3;
        public const float ActiveSeconds = 90f;
        public const float WrongPenalty = 2f;

        public static string Prompt(StationKind kind, int t, int r, int u)
        {
            switch (kind)
            {
                case StationKind.Pair:
                    return "반지름의 길이가 " + r + " cm인 원 O 밖의 점 P에서 원 O에 그은 두 접선의 접점을 각각 A, B라 하자. PA=" + t + " cm일 때, PB의 길이를 구하시오.";
                case StationKind.Sum:
                    return "반지름의 길이가 " + r + " cm인 원 O 밖의 점 P에서 원 O에 그은 두 접선의 접점을 각각 A, B라 하자. PA+PB=" + (2 * t) + " cm일 때, PB의 길이를 구하시오.";
                default:
                    return "△ABC의 내접원이 변 AB, BC, CA와 접하는 점을 각각 D, E, F라 하자. AB=" + (u + t) + " cm, AD=" + u + " cm일 때, BE의 길이를 구하시오.";
            }
        }

        static JebiProblem Make(StationKind kind, int t, int v)
        {
            var p = new JebiProblem { kind = kind, t = t, variant = v };
            if (kind == StationKind.Incircle) { p.u = 2 + v; p.r = 0; }
            else { p.r = 2 + v; p.u = 0; }
            p.id = "k" + (int)kind + "-t" + t.ToString("00") + "-v" + v;
            p.prompt = Prompt(kind, t, p.r, p.u);
            return p;
        }

        public static List<JebiProblem> BuildCatalog()
        {
            var list = new List<JebiProblem>(360);
            for (int k = 1; k <= 3; k++)
                for (int t = 1; t <= MaxLen; t++)
                    for (int v = 0; v < 5; v++)
                        list.Add(Make((StationKind)k, t, v));
            return list;
        }

        /// <summary>고정 연습: PA=7 cm, 반지름 4 cm, PB 를 뽑는다.</summary>
        public static JebiProblem Practice()
        {
            var p = Make(StationKind.Pair, 7, 2);
            p.id = "practice";
            p.practice = true;
            return p;
        }

        public static StationKind KindForRound(int round) // round 1..8
        {
            if (round <= 3) return StationKind.Pair;
            if (round <= 5) return StationKind.Sum;
            return StationKind.Incircle;
        }

        public static bool IsInspectRound(int round) => round == 5;

        /// <summary>오개념 진단. 우연히 정답과 같아지는 값(u=t, 2t>24 등)은 진단하지 않는다 — 정답을 오류로 기록하지 않기 위해.</summary>
        public static string MisconceptionFor(JebiProblem p, int value)
        {
            if (value == p.t) return "";
            switch (p.kind)
            {
                case StationKind.Pair:
                    if (p.r != p.t && value == p.r) return "radius-as-tangent";
                    return "other";
                case StationKind.Sum:
                    if (value == 2 * p.t) return "sum-as-one-tangent";
                    if (p.r != p.t && value == p.r) return "radius-as-tangent";
                    return "other";
                default:
                    if (p.u != p.t && value == p.u) return "all-tangents-equal";
                    if (value == p.u + p.t) return "whole-side-as-part";
                    if ((p.u + p.t) % 2 == 0 && (p.u + p.t) / 2 != p.t && value == (p.u + p.t) / 2) return "half-of-side";
                    return "other";
            }
        }

        /// <summary>오개념 역산 값(범위 1..24 안이고 정답과 다른 것만). misconceptionId 와 짝.</summary>
        public static List<KeyValuePair<string, int>> MisconceptionValues(JebiProblem p)
        {
            var res = new List<KeyValuePair<string, int>>();
            void Add(string id, int v) { if (v >= 1 && v <= MaxLen && v != p.t) res.Add(new KeyValuePair<string, int>(id, v)); }
            switch (p.kind)
            {
                case StationKind.Pair: Add("radius-as-tangent", p.r); break;
                case StationKind.Sum: Add("sum-as-one-tangent", 2 * p.t); Add("radius-as-tangent", p.r); break;
                default:
                    Add("all-tangents-equal", p.u);
                    Add("whole-side-as-part", p.u + p.t);
                    if ((p.u + p.t) % 2 == 0) Add("half-of-side", (p.u + p.t) / 2);
                    break;
            }
            return res;
        }

        public static int WrongValueFor(JebiProblem p)
        {
            var m = MisconceptionValues(p);
            if (m.Count > 0) return m[0].Value;
            return p.t % MaxLen + 1;
        }

        /// <summary>부팅 전수 검사: 각 문항의 후보 1..24 중 정답이 정확히 1개, 종류별 t 빈도 균등, 오개념 값이 정답과 겹치지 않음.</summary>
        public static string Verify(List<JebiProblem> catalog)
        {
            var sb = new StringBuilder();
            var freq = new int[4, MaxLen + 1];
            var prompts = new HashSet<string>();
            int bad = 0;
            foreach (var p in catalog)
            {
                int hits = 0;
                for (int v = 1; v <= MaxLen; v++) if (p.IsCorrect(v)) hits++;
                if (hits != 1) bad++;
                freq[(int)p.kind, p.t]++;
                if (!prompts.Add(p.prompt)) bad++;
                foreach (var kv in MisconceptionValues(p)) if (kv.Value == p.t) bad++;
                if (p.kind == StationKind.Incircle && (p.u <= 0 || p.t <= 0)) bad++;
            }
            for (int k = 1; k <= 3; k++)
                for (int t = 1; t <= MaxLen; t++)
                    if (freq[k, t] != 5) bad++;
            sb.Append("catalog=").Append(catalog.Count).Append(" bad=").Append(bad);
            return sb.ToString();
        }
    }
}
