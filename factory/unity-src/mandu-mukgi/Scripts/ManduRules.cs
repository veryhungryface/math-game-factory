// 만두 묶기 — 문제 모델·생성기·판정. 수학 판정은 전부 정수(대상 비트마스크)로 한다.
//
// [9수03-09] 이등변삼각형의 성질과 그 역만 다룬다. 외심·내심·RHA/RHS 는 섞지 않는다.
// 2022-middle-math.json style_guide / expression_traps 에서 옮겨 적은 규칙:
//  - 발문 종결은 「~하시오」(중학교 지시형). 「~해 봅시다/~해 보세요」를 섞지 않는다.
//  - 도형 기호: △ABC, ∠B, 합동은 △ABD≡△ACD (SAS 합동)처럼 대응 순서대로, 조건 이름은 괄호 안에.
//  - 길이는 「7 cm」처럼 띄우고 각은 「50°」로 붙인다.
//  - 중2 에는 √ 가 없다 — 이 게임은 길이를 계산시키지 않고(높이·근호 없음) 주어진 정수 길이·각만 읽힌다.
//  - 정삼각형은 제외한다(b≠s, β≠60°) — 세 쌍이 모두 정답이 되면 판정이 무너진다.
//  - 「가정」「결론」은 발문 금지어다(증명 용어는 이 게임 범위 밖).
//
// 대상 번호 규칙: 꼭짓점 이름 k(0=A,1=B,2=C).
//  - 각 모드: 대상 k = ∠(꼭짓점 k).
//  - 변 모드: 대상 k = 꼭짓점 k 의 맞은편 변(0=BC, 1=AC, 2=AB).
//  두 모드 모두 정답 = 꼭지각(apex)이 아닌 두 대상 → correctMask = 7 ^ (1 << apex).
//  (각 모드: 두 밑각 / 변 모드: 두 밑각의 맞은편 변 = 꼭지각을 끼는 두 변)
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;

namespace Mgf.ManduMukgi
{
    public enum DoughKind { SideToAngle = 1, AngleToSide = 2, FixAngle = 3, FixSide = 4 }

    public enum Verdict { Correct, WrongPair, KeepExisting, OverStitch }

    public sealed class DoughProblem
    {
        public string id;
        public DoughKind kind;
        public int band;           // 1·2·3
        public int apex;           // 꼭지각 꼭짓점의 이름 번호 0..2
        public int s, b;           // 각 모드: 같은 두 변 s cm, 밑변 b cm
        public int beta, alpha;    // 변 모드: 밑각 β°, 꼭지각 α°=180°-2β
        public int preMask;        // 단계3: 미리 그려진 잘못된 연결(정답이 아닌 쌍)
        public bool practice;

        public bool AngleMode => kind == DoughKind.SideToAngle || kind == DoughKind.FixAngle;
        public bool Fix => kind == DoughKind.FixAngle || kind == DoughKind.FixSide;
        public int CorrectMask => 7 ^ (1 << apex);

        public static readonly string[] V = { "A", "B", "C" };

        /// <summary>꼭짓점 k 의 맞은편 변 이름(알파벳 순).</summary>
        public static string SideName(int k) => k == 0 ? "BC" : k == 1 ? "AC" : "AB";
        public static string AngleName(int k) => "∠" + V[k];
        public string TargetName(int k) => AngleMode ? AngleName(k) : SideName(k);

        public int Base1 => apex == 0 ? 1 : 0;
        public int Base2 => apex == 2 ? 1 : 2;

        /// <summary>같은 두 변(꼭지각을 끼는 두 변) — 밑각의 맞은편 변 2개.</summary>
        public string LegsText => SideName(Base2) + "=" + SideName(Base1);
        string LegsSorted { get { var a = SideName(Base1); var c = SideName(Base2); return string.CompareOrdinal(a, c) < 0 ? a + "=" + c : c + "=" + a; } }

        /// <summary>그림에 적힌 수치를 문장으로(문제 은행 prompt 와 결과 화면이 같은 모델에서 나온다).</summary>
        public string GivenText
        {
            get
            {
                if (AngleMode) return LegsSorted + "=" + s + " cm, " + SideName(apex) + "=" + b + " cm";
                string b1 = AngleName(Base1), b2 = AngleName(Base2);
                return b1 + "=" + b2 + "=" + beta + "°, " + AngleName(apex) + "=" + alpha + "°";
            }
        }

        /// <summary>반죽 위 짧은 발문(화면 태그).</summary>
        public string Instruction
        {
            get
            {
                if (Fix) return AngleMode ? "잘못 연결된 두 각을 고치시오" : "잘못 연결된 두 변을 고치시오";
                return AngleMode ? "같은 크기의 두 각을 이으시오" : "같은 길이의 두 변을 이으시오";
            }
        }

        public string PairText(int mask)
        {
            var sb = new StringBuilder();
            for (int k = 0; k < 3; k++) if ((mask & (1 << k)) != 0) { if (sb.Length > 0) sb.Append(", "); sb.Append(TargetName(k)); }
            return sb.ToString();
        }

        string PairWith(int mask)
        {
            // 「∠A와 ∠B」 — A·B·C 이름은 모두 모음으로 끝나 「와」를 쓴다.
            int a = -1, c = -1;
            for (int k = 0; k < 3; k++) if ((mask & (1 << k)) != 0) { if (a < 0) a = k; else c = k; }
            return TargetName(a) + "와 " + TargetName(c);
        }

        public string Prompt
        {
            get
            {
                string head = "△ABC에서 " + GivenText + "이다. ";
                if (!Fix) return head + Instruction + ".";
                return head + PairWith(preMask) + "가 잘못 연결되어 있다. " + Instruction + ".";
            }
        }

        /// <summary>정답의 근거(정답 직후·오답 뒤 정답 상태 공개에 쓴다).</summary>
        public string Reason
        {
            get
            {
                string legs = LegsText;
                string angles = AngleName(Base1) + "=" + AngleName(Base2);
                return AngleMode ? legs + " 이므로 " + angles : angles + " 이므로 " + legs;
            }
        }

        public Verdict Judge(int mask)
        {
            if (Bits(mask) >= 3) return Verdict.OverStitch;
            if (mask == CorrectMask) return Verdict.Correct;
            if (Fix && mask == preMask) return Verdict.KeepExisting;
            return Verdict.WrongPair;
        }

        /// <summary>오답 쌍이 어떤 오개념에서 나오는지(검산관이 확인한다).</summary>
        public string MisconceptionId(int mask)
        {
            var v = Judge(mask);
            if (v == Verdict.Correct) return "";
            if (v == Verdict.OverStitch) return "over-stitch-all-three";
            if (v == Verdict.KeepExisting) return "keep-existing-link";
            // 정답이 아닌 두 쌍은 반드시 꼭지각(각 모드) 또는 밑변(변 모드)을 포함한다.
            return AngleMode ? "apex-angle-as-base-angle" : "base-side-included";
        }

        public string WrongExplain(int mask)
        {
            switch (Judge(mask))
            {
                case Verdict.OverStitch: return "세 곳을 모두 이었다. 두 곳만 이으시오";
                case Verdict.KeepExisting: return "이미 있던 연결은 틀린 쌍이다. " + WhyWrong;
                default: return WhyWrong;
            }
        }

        /// <summary>정답이 아닌 쌍에 반드시 들어 있는 대상(꼭지각 / 밑변)이 왜 짝이 아닌지.</summary>
        public string WhyWrong
        {
            get
            {
                if (AngleMode) return AngleName(apex) + "는 같은 두 변 사이의 꼭지각이다";
                return SideName(apex) + "는 밑변이다. " + AngleName(Base1) + "의 맞은편은 " + SideName(Base1);
            }
        }

        public MgfProblem ToMgf()
        {
            return new MgfProblem
            {
                id = id,
                prompt = Prompt,
                choices = null,
                answer = PairText(CorrectMask),
                answerNumeric = CorrectMask,
                unitConcept = AngleMode ? "이등변삼각형의 두 밑각의 크기는 같다" : "두 내각의 크기가 같은 삼각형은 이등변삼각형이다"
            };
        }

        public static int Bits(int m) { int c = 0; for (int k = 0; k < 3; k++) if ((m & (1 << k)) != 0) c++; return c; }
    }

    public static class ManduRules
    {
        public const int StartSeals = 3;
        public const int PerBand = 4;
        public const int Total = 12;
        public const float RunSeconds = 90f;
        public const int MasteryTotal = 10;
        public const int MasteryBand = 3;
        public static readonly float[] Life = { 15f, 13f, 12f };
        public static readonly int[] Cap = { 1, 2, 2 };

        /// <summary>단계1 (같은 두 변 s, 밑변 b): s∈5..10, b∈4..12, b&lt;2s, b≠s, 0.6≤b/s≤1.6 (정수: 5b≥3s, 5b≤8s).</summary>
        public static List<int[]> SidePairs()
        {
            var list = new List<int[]>();
            for (int s = 5; s <= 10; s++)
                for (int b = 4; b <= 12; b++)
                    if (b < 2 * s && b != s && 5 * b >= 3 * s && 5 * b <= 8 * s) list.Add(new[] { s, b });
            return list;
        }

        /// <summary>단계2 밑각 β (60° 제외 — 정삼각형).</summary>
        public static readonly int[] Betas = { 30, 35, 40, 45, 50, 55, 65, 70, 75 };

        static DoughProblem SideProblem(int s, int b, int apex, DoughKind kind, int pre)
        {
            var p = new DoughProblem { kind = kind, band = kind == DoughKind.SideToAngle ? 1 : 3, apex = apex, s = s, b = b, preMask = pre };
            p.id = (kind == DoughKind.SideToAngle ? "sa" : "fa") + "-s" + s + "b" + b + "-" + DoughProblem.V[apex] + (pre != 0 ? "-p" + pre : "");
            return p;
        }

        static DoughProblem AngleProblem(int beta, int apex, DoughKind kind, int pre)
        {
            var p = new DoughProblem { kind = kind, band = kind == DoughKind.AngleToSide ? 2 : 3, apex = apex, beta = beta, alpha = 180 - 2 * beta, preMask = pre };
            p.id = (kind == DoughKind.AngleToSide ? "as" : "fs") + "-b" + beta + "-" + DoughProblem.V[apex] + (pre != 0 ? "-p" + pre : "");
            return p;
        }

        /// <summary>정답이 아닌 두 쌍(꼭지각/밑변을 포함하는 쌍).</summary>
        public static int[] WrongPairs(int apex)
        {
            int b1 = apex == 0 ? 1 : 0, b2 = apex == 2 ? 1 : 2;
            return new[] { (1 << apex) | (1 << b1), (1 << apex) | (1 << b2) };
        }

        /// <summary>실전에 나오는 모든 문항(문제 은행 = 런타임 출제 풀). 같은 생성기에서 나온다.</summary>
        public static List<DoughProblem> Catalog()
        {
            var list = new List<DoughProblem>();
            var pairs = SidePairs();
            foreach (var sb in pairs) for (int a = 0; a < 3; a++) list.Add(SideProblem(sb[0], sb[1], a, DoughKind.SideToAngle, 0));
            foreach (var beta in Betas) for (int a = 0; a < 3; a++) list.Add(AngleProblem(beta, a, DoughKind.AngleToSide, 0));
            foreach (var sb in pairs) for (int a = 0; a < 3; a++) foreach (var w in WrongPairs(a)) list.Add(SideProblem(sb[0], sb[1], a, DoughKind.FixAngle, w));
            foreach (var beta in Betas) for (int a = 0; a < 3; a++) foreach (var w in WrongPairs(a)) list.Add(AngleProblem(beta, a, DoughKind.FixSide, w));
            return list;
        }

        /// <summary>한 판의 12문항 덱: 단계1 4 → 단계2 4 → 단계3 (각·변·각·변). 난이도 밴드를 순서대로 소진한다.</summary>
        public static List<DoughProblem> Deck(List<DoughProblem> catalog, Random rng)
        {
            var deck = new List<DoughProblem>();
            deck.AddRange(PickDistinct(catalog, DoughKind.SideToAngle, 4, rng));
            deck.AddRange(PickDistinct(catalog, DoughKind.AngleToSide, 4, rng));
            var fa = PickDistinct(catalog, DoughKind.FixAngle, 2, rng);
            var fs = PickDistinct(catalog, DoughKind.FixSide, 2, rng);
            deck.Add(fa[0]); deck.Add(fs[0]); deck.Add(fa[1]); deck.Add(fs[1]);
            return deck;
        }

        /// <summary>같은 종류에서 수치 조합이 겹치지 않게 n개(제약 만족 풀에서 비복원 추출).</summary>
        static List<DoughProblem> PickDistinct(List<DoughProblem> catalog, DoughKind kind, int n, Random rng)
        {
            var pool = new List<DoughProblem>();
            foreach (var p in catalog) if (p.kind == kind) pool.Add(p);
            var picked = new List<DoughProblem>();
            var usedValues = new HashSet<int>();
            for (int guard = 0; picked.Count < n && guard < 400; guard++)
            {
                var p = pool[rng.Next(pool.Count)];
                int key = p.AngleMode ? p.s * 100 + p.b : 10000 + p.beta;
                if (!usedValues.Add(key)) continue;
                picked.Add(p);
            }
            return picked;
        }

        // ── 고정 연습 문항(통계·숙련에서 제외) ──
        public static DoughProblem Practice1() => new DoughProblem { id = "practice-1", kind = DoughKind.SideToAngle, band = 1, apex = 0, s = 7, b = 6, practice = true };
        public static DoughProblem Practice2() => new DoughProblem { id = "practice-2", kind = DoughKind.AngleToSide, band = 2, apex = 0, beta = 50, alpha = 80, practice = true };
        public static DoughProblem Practice3() => new DoughProblem { id = "practice-3", kind = DoughKind.FixAngle, band = 3, apex = 1, s = 8, b = 5, preMask = (1 << 0) | (1 << 1), practice = true };
    }
}
