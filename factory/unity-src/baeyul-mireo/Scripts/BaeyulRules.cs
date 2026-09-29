// 배율 밀어 — 수학 모델·문제 생성기·판정·문장. 판정은 정수 mm·정수 도만 쓴다(float 비교 없음).
//
// ■ 모델
//   원본 고무판(△ABC 또는 □ABCD)의 변 길이 os[] 는 정수 mm. 찍을 도형(△DEF / □EFGH)은
//   꼭짓점 대응 배열 corr[] 로 이름이 붙는다: 원본 꼭짓점 i ↔ 찍을 도형 글자 IL[corr[i]].
//   기호 문자열(△ABC∽△EFD)과 대응변 계산이 이 한 배열에서 나온다(expression_traps: 대응 순서).
//   놋쇠 레일 손잡이 = 찍을 도형의 「레일 변」 길이(정수 mm, 5 mm 칸). 레일 변은 원본 변 gauge 의 대응변이다.
//
// ■ 판정 (Judge)
//   구성 장: handle × m == os[gauge] × n  (닮음비 원본:찍을 도형 = m:n, 교차곱 정수 등식). 치우기는 항상 오답.
//   판별 장: 레일 고정. Similar() 가 정수로 닮음을 판정(SSS: 정렬한 세 변 교차곱 / SAS: 끼인각 같고 두 변 교차곱 /
//            AA: 세 각(셋째 각 = 180 − 두 각) 정렬 비교 / 직사각형: 가로·세로 교차곱). 닮음 → 찍기, 아니면 → 치우기.
//   정답 배율로 스냅하지 않는다 — 5 mm 칸은 모든 위치에 똑같이 적용되는 레일 눈금이다.
//
// ■ expression_traps (2022-middle-math.json) 을 생성기 규칙으로 옮김
//   · 닮음·합동 기호는 꼭짓점 대응 순서 — corr[] 하나로 기호와 대응변을 같이 만든다.
//   · 「닮음비」「넓이의 비」를 항상 구분해 쓴다. 넓이의 비 주문의 오답 유도(손잡이 시작 위치)는 닮음비를 그대로 쓴 값.
//   · 중2 에는 √ 가 없다 — 길이는 모두 정수 mm 로 주고, 넓이 주문은 밑변·높이(또는 가로·세로)로 넓이가 정수 cm² 인 것만.
//   · 종결형은 「~하시오」, 단위는 수와 띄어 쓴다(6 cm, 20 cm²). 「가정」「결론」 금지.
//   · 정삼각형·이등변삼각형·정사각형은 만들지 않는다(「모든 각이 같아 무조건 닮음」 우회와 대응 모호성 차단).
//
// ■ 오개념 → 코드 (misconceptionId)
//   M1_area_linear   넓이의 비를 닮음비로 씀 — 넓이 주문의 손잡이 시작 = os×n²/m² (거기서 찍으면 오답)
//   M2_position      대응을 그림 위치/글자 순서(AB↔DE)로 잡음 — 대응 장의 레일 변은 DE 인데 실제 대응변은 AB 가 아니다.
//                     손잡이 시작 = AB×n/m (거기서 찍으면 오답)
//   M3_looks_alike   비슷해 보이면 닮음(모든 직사각형·각 하나 다른 삼각형) — 판별 장 「닮음 아님」 후보
//   M4_additive      변에 같은 길이를 더하면 닮음 — 판별 장 「닮음 아님」 후보(각 변 +k)
using System;
using System.Collections.Generic;
using System.Text;

namespace Mgf.BaeyulMireo
{
    public enum Kind { Build, Judge }
    public enum Order { Ratio, Corr, Area, AreaValue, Judge }
    public enum Cond { None, SSS, SAS, AA, Rect }
    public enum Shape { Tri, Rect, TriBH }
    public enum Act { Stamp, Discard }

    public static class Rules
    {
        public const float RunSec = 90f;
        public const int Spares = 3;
        public const int Sheets = 12;
        public const int RailMin = 10, RailMax = 160, Step = 5;      // mm (레일 1 cm ~ 16 cm, 5 mm 칸 31개)
        public const float HandleSpeed = 300f;                        // mm/초 — 레일 전장 150 mm 를 0.5초에 횡단(훑기 상한)
        public static int Positions => (RailMax - RailMin) / Step + 1;
        public static int Limit(int stage) => stage == 1 ? 16 : stage == 2 ? 14 : 13;
        public static int Mult(int combo) => combo >= 6 ? 3 : combo >= 3 ? 2 : 1;

        /// <summary>판단 없이 찍기/치우기를 반반, 손잡이를 무작위 칸에 두었을 때 첫 시도에 맞을 확률.</summary>
        public static double Chance(Sheet s) => s.kind == Kind.Judge ? 0.5 : 0.5 / ((s.railHi - s.railLo) / Step + 1);
    }

    public class Sheet
    {
        public int no, stage = 1, limitSec = 16;
        public Kind kind = Kind.Build;
        public Order order = Order.Ratio;
        public Cond cond = Cond.None;
        public Shape shape = Shape.Tri;
        public int m = 1, n = 1;                     // 닮음비 원본 : 찍을 도형
        public int[] os = new int[4];                // 원본 변(mm). Tri: AB,BC,CA · Rect: AB(세로),BC(가로),CD,DA · TriBH: [1]=BC(밑변)
        public int oh, ofoot;                        // TriBH: 높이, 수선의 발(B 에서 BC 방향) mm
        public int[] corr = { 0, 1, 2, 3 };          // 원본 꼭짓점 i → 찍을 도형 글자 번호
        public int rot; public bool flip;            // 찍을 도형 그림의 회전(도)·뒤집기(그림만 — 판정과 무관)
        public int gauge;                            // 레일이 재는 변 = 원본 변 gauge 의 대응변
        public int handleStart, handle;
        public int railLo = Rules.RailMin, railHi = Rules.RailMax;   // 이 장에서 손잡이가 갈 수 있는 구간(배율 50%~250%, 그림이 작업 면을 넘지 않게)
        public string misconceptionId = "";
        public int areaA, areaB;                     // Area: 넓이의 비 A:B · AreaValue: 원본 넓이 A, 목표 넓이 B (cm²)
        // 판별 장
        public bool similar;
        public int[] cs = new int[3];                // 후보 변(mm). SSS: DE,EF,FD · SAS: DE,EF · Rect: EF(세로),FG(가로) · AA: [0]=그림용 DE
        public int oAng, cAng;                       // SAS: ∠B, ∠E
        public int[] oA = new int[3], cA = new int[3]; // AA: 원본 ∠A,∠B,∠C / 후보 ∠D,∠E,∠F
        public int cMask;                            // AA: 후보에서 보여 주는 두 각(비트 0=D,1=E,2=F)

        public int N => shape == Shape.Rect ? 4 : 3;
        public bool Locked => kind == Kind.Judge;
        public void Reset() { handle = handleStart; }
        public Sheet Clone()
        {
            var c = (Sheet)MemberwiseClone();
            c.os = (int[])os.Clone(); c.corr = (int[])corr.Clone(); c.cs = (int[])cs.Clone(); c.oA = (int[])oA.Clone(); c.cA = (int[])cA.Clone();
            return c;
        }
    }

    public static class Judge
    {
        public static int Gcd(int a, int b) { a = Math.Abs(a); b = Math.Abs(b); while (b != 0) { int t = a % b; a = b; b = t; } return a; }

        /// <summary>레일 변의 대응변(원본) 길이 mm.</summary>
        public static int GaugeOrig(Sheet s) => s.os[s.gauge];

        /// <summary>정답 레일 길이(mm) — 생성기가 5 mm 칸의 배수가 되게 고른다.</summary>
        public static int Target(Sheet s) => GaugeOrig(s) * s.n / s.m;

        /// <summary>구성 장: 교차곱 handle·m = os[gauge]·n.</summary>
        public static bool InWindow(Sheet s) => (long)s.handle * s.m == (long)GaugeOrig(s) * s.n;

        public static bool Correct(Sheet s, Act a)
        {
            if (s.kind == Kind.Judge) return (a == Act.Stamp) == Similar(s);
            return a == Act.Stamp && InWindow(s);
        }

        public static Act RightAct(Sheet s) => s.kind == Kind.Build ? Act.Stamp : (Similar(s) ? Act.Stamp : Act.Discard);

        static bool Prop(int a, int b, int c, int d) => (long)a * d == (long)b * c;   // a:b = c:d

        /// <summary>판별 장 닮음 여부(정수).</summary>
        public static bool Similar(Sheet s)
        {
            switch (s.cond)
            {
                case Cond.SSS:
                    {
                        var o = new[] { s.os[0], s.os[1], s.os[2] }; var c = new[] { s.cs[0], s.cs[1], s.cs[2] };
                        Array.Sort(o); Array.Sort(c);
                        return Prop(o[0], c[0], o[1], c[1]) && Prop(o[0], c[0], o[2], c[2]);
                    }
                case Cond.SAS:
                    // 끼인각 ∠B = ∠E 이고 끼인 두 변의 비가 같다(순서가 바뀐 짝도 닮음이다)
                    return s.oAng == s.cAng && (Prop(s.os[0], s.cs[0], s.os[1], s.cs[1]) || Prop(s.os[0], s.cs[1], s.os[1], s.cs[0]));
                case Cond.AA:
                    {
                        var o = (int[])s.oA.Clone(); var c = (int[])s.cA.Clone();
                        Array.Sort(o); Array.Sort(c);
                        return o[0] == c[0] && o[1] == c[1] && o[2] == c[2];
                    }
                case Cond.Rect:
                    return Prop(s.os[0], s.cs[0], s.os[1], s.cs[1]) || Prop(s.os[0], s.cs[1], s.os[1], s.cs[0]);
            }
            return false;
        }

        /// <summary>그림용 삼각형이 성립하는지(세 변) — 가장 작은 각(도, 실수 — 판정에는 쓰지 않는다).</summary>
        public static double MinAngle(int a, int b, int c)
        {
            if (a + b <= c || b + c <= a || a + c <= b) return 0;
            double A = Math.Acos((b * (double)b + c * (double)c - a * (double)a) / (2.0 * b * c));
            double B = Math.Acos((a * (double)a + c * (double)c - b * (double)b) / (2.0 * a * c));
            double C = Math.PI - A - B;
            return Math.Min(A, Math.Min(B, C)) * 180 / Math.PI;
        }

        /// <summary>원본 넓이(cm², 정수 — 넓이 주문 장만).</summary>
        public static int AreaCm2(Sheet s) => s.shape == Shape.Rect ? s.os[0] * s.os[1] / 100 : s.os[1] * s.oh / 200;
    }

    // ─────────────────────────────────────────────── 생성기 (제약 만족 풀에서 샘플링)
    public class SheetGen
    {
        readonly Random r;
        public SheetGen(Random r) { this.r = r; }

        static readonly int[][] Perms = { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 }, new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };

        /// <summary>원본 변의 단위(mm): 원본은 5 mm 배수, 찍을 도형의 변(×n/m)도 5 mm 배수가 되게.</summary>
        public static int Unit(int m, int n)
        {
            int need = 5 * m / Judge.Gcd(n, 5);
            return need * 5 / Judge.Gcd(need, 5);
        }

        static readonly Dictionary<string, List<int[]>> triCache = new Dictionary<string, List<int[]>>();
        /// <summary>구성 장 삼각형 풀: 부등변, 가장 작은 각 26° 이상, 원본 15~80 mm, 찍을 도형 15~130 mm.</summary>
        public static List<int[]> TriPool(int m, int n)
        {
            string k = m + ":" + n;
            if (triCache.TryGetValue(k, out var l)) return l;
            l = new List<int[]>();
            int u = Unit(m, n);
            for (int a = u; a <= 80; a += u)
                for (int b = u; b <= 80; b += u)
                    for (int c = u; c <= 80; c += u)
                    {
                        if (a < 15 || b < 15 || c < 15) continue;
                        if (a == b || b == c || a == c) continue;
                        if (Math.Max(a, Math.Max(b, c)) * n / m > 130 || Math.Min(a, Math.Min(b, c)) * n / m < 15) continue;
                        if (Judge.MinAngle(a, b, c) < 26) continue;
                        l.Add(new[] { a, b, c });   // AB, BC, CA
                    }
            triCache[k] = l;
            return l;
        }

        static readonly Dictionary<string, List<int[]>> rectCache = new Dictionary<string, List<int[]>>();
        public static List<int[]> RectPool(int m, int n, bool areaInt)
        {
            string k = m + ":" + n + areaInt;
            if (rectCache.TryGetValue(k, out var l)) return l;
            l = new List<int[]>();
            int u = Unit(m, n);
            for (int h = u; h <= 70; h += u)
                for (int w = u; w <= 80; w += u)
                {
                    if (h < 15 || w < 15 || h == w) continue;
                    int mx = Math.Max(h, w), mn = Math.Min(h, w);
                    if (mx * 4 < mn * 5 || mx * 10 > mn * 26) continue;              // 가로:세로 1.25 ~ 2.6
                    if (mx * n / m > 125 || mn * n / m < 15) continue;
                    if (areaInt && ((h * w) % 100 != 0 || ((long)h * w * n * n) % (100L * m * m) != 0)) continue;
                    l.Add(new[] { h, w });   // AB(세로), BC(가로)
                }
            rectCache[k] = l;
            return l;
        }

        static readonly Dictionary<string, List<int[]>> bhCache = new Dictionary<string, List<int[]>>();
        /// <summary>넓이 주문 삼각형(밑변 BC·높이): 넓이가 정수 cm², 찍을 도형 넓이도 정수 cm².</summary>
        public static List<int[]> BHPool(int m, int n)
        {
            string k = m + ":" + n;
            if (bhCache.TryGetValue(k, out var l)) return l;
            l = new List<int[]>();
            int u = Unit(m, n);
            for (int b = u; b <= 80; b += u)
                for (int h = u; h <= 70; h += u)
                {
                    if (b < 20 || h < 20 || b == h) continue;
                    if (Math.Max(b, h) * n / m > 125 || Math.Min(b, h) * n / m < 15) continue;
                    if ((b * h) % 200 != 0) continue;                                   // b·h/2 가 100 mm² 의 배수
                    if (((long)b * h * n * n) % (200L * m * m) != 0) continue;            // 찍을 도형 넓이도 정수 cm²
                    l.Add(new[] { b, h });
                }
            bhCache[k] = l;
            return l;
        }

        T Pick<T>(IList<T> l) => l[r.Next(l.Count)];
        int[] PickRatio(int[][] l) => l[r.Next(l.Length)];

        static readonly int[][] RatiosS1 = { new[] { 1, 2 }, new[] { 2, 3 }, new[] { 3, 2 }, new[] { 3, 4 } };
        static readonly int[][] RatiosS2 = { new[] { 3, 4 }, new[] { 2, 5 }, new[] { 4, 3 }, new[] { 3, 2 }, new[] { 2, 3 }, new[] { 3, 5 }, new[] { 4, 5 } };
        static readonly int[][] RatiosArea = { new[] { 1, 2 }, new[] { 2, 3 }, new[] { 3, 2 }, new[] { 3, 4 }, new[] { 2, 1 } };

        /// <summary>한 판 12장. 구성 5 · 대응 2 · 판별 3 · 넓이 2 (textbook_alignment.type_mix_plan).</summary>
        public List<Sheet> Deck()
        {
            var d = new List<Sheet>();
            d.Add(Ratio(1, 1, new[] { 1, 2 }, Shape.Tri));
            d.Add(Ratio(2, 1, new[] { 2, 3 }, Shape.Tri));
            d.Add(Ratio(3, 1, PickRatio(new[] { new[] { 1, 2 }, new[] { 2, 3 }, new[] { 3, 2 } }), Shape.Rect));
            d.Add(Ratio(4, 1, PickRatio(new[] { new[] { 3, 2 }, new[] { 3, 4 }, new[] { 2, 3 } }), Shape.Tri));
            d.Add(Ratio(5, 2, PickRatio(RatiosS2), r.Next(3) == 0 ? Shape.Rect : Shape.Tri));      // 다리: 비 숫자가 걷힌다
            d.Add(Corr(6, false));
            d.Add(JudgeSheet(7, r.Next(2) == 0 ? Cond.SSS : Cond.SAS));
            d.Add(Corr(8, true));
            d.Add(JudgeSheet(9, r.Next(2) == 0 ? Cond.AA : Cond.SAS));
            d.Add(Area(10));
            d.Add(JudgeSheet(11, r.Next(2) == 0 ? Cond.Rect : Cond.SSS));
            d.Add(AreaValue(12));
            return d;
        }

        /// <summary>연습(온보딩) 장 — 고정값. AB=4 cm·BC=6 cm·CA=5 cm, 닮음비 2:3, 손잡이는 1:1(4 cm)에서 시작.</summary>
        public static Sheet Practice()
        {
            var s = new Sheet { no = 0, stage = 1, order = Order.Ratio, shape = Shape.Tri, m = 2, n = 3, gauge = 0, limitSec = 0 };
            s.os[0] = 40; s.os[1] = 60; s.os[2] = 50;
            SetRange(s);
            s.handleStart = 40; s.Reset();
            return s;
        }

        Sheet Base(int no, int stage) => new Sheet { no = no, stage = stage, limitSec = Rules.Limit(stage) };

        Sheet Ratio(int no, int stage, int[] mn, Shape shape)
        {
            var s = Base(no, stage);
            s.order = Order.Ratio; s.shape = shape; s.m = mn[0]; s.n = mn[1];
            if (shape == Shape.Tri) { var t = Pick(TriPool(s.m, s.n)); s.os[0] = t[0]; s.os[1] = t[1]; s.os[2] = t[2]; }
            else { var t = Pick(RectPool(s.m, s.n, false)); s.os[0] = t[0]; s.os[1] = t[1]; s.os[2] = t[0]; s.os[3] = t[1]; }
            s.gauge = shape == Shape.Rect ? r.Next(2) : 0;
            SetRange(s);
            s.handleStart = FarStart(s, Judge.Target(s));
            s.Reset();
            return s;
        }

        Sheet Corr(int no, bool flip)
        {
            // 레일 변은 항상 DE. 대응변이 AB 가 아니고 길이도 AB 와 다른 순열만 쓴다(위치·글자 순서 오개념이 다른 값을 내게)
            for (int guard = 0; guard < 200; guard++)
            {
                var mn = PickRatio(RatiosS2);
                var s = Base(no, 2);
                s.order = Order.Corr; s.shape = Shape.Tri; s.m = mn[0]; s.n = mn[1];
                var t = Pick(TriPool(s.m, s.n)); s.os[0] = t[0]; s.os[1] = t[1]; s.os[2] = t[2];
                var p = Perms[1 + r.Next(5)];
                for (int i = 0; i < 3; i++) s.corr[i] = p[i];
                int vd = Array.IndexOf(s.corr, 0, 0, 3), ve = Array.IndexOf(s.corr, 1, 0, 3);
                int g = SideOf(vd, ve);
                if (g == 0 || s.os[g] == s.os[0]) continue;
                s.gauge = g;
                s.rot = flip ? r.Next(4) * 90 : 90 * (1 + r.Next(3));
                s.flip = flip;
                s.misconceptionId = "M2_position";
                SetRange(s);
                int tgt = Judge.Target(s), wrong = s.os[0] * s.n / s.m;
                s.handleStart = InRail(s, wrong) && wrong != tgt ? wrong : FarStart(s, tgt);
                s.Reset();
                return s;
            }
            return Ratio(no, 2, new[] { 2, 3 }, Shape.Tri);
        }

        /// <summary>꼭짓점 i, j 를 잇는 변 번호(0=AB, 1=BC, 2=CA).</summary>
        public static int SideOf(int i, int j)
        {
            int a = Math.Min(i, j), b = Math.Max(i, j);
            return a == 0 && b == 1 ? 0 : a == 1 && b == 2 ? 1 : 2;
        }

        Sheet Area(int no)
        {
            var mn = PickRatio(RatiosArea);
            var s = Base(no, 3);
            s.order = Order.Area; s.shape = Shape.Tri; s.m = mn[0]; s.n = mn[1];
            var t = Pick(TriPool(s.m, s.n)); s.os[0] = t[0]; s.os[1] = t[1]; s.os[2] = t[2];
            s.gauge = 0;
            s.areaA = s.m * s.m; s.areaB = s.n * s.n;
            s.misconceptionId = "M1_area_linear";
            SetRange(s);
            s.handleStart = LinearStart(s);
            s.Reset();
            return s;
        }

        Sheet AreaValue(int no)
        {
            for (int guard = 0; guard < 50; guard++)
            {
                var mn = PickRatio(new[] { new[] { 1, 2 }, new[] { 2, 3 }, new[] { 3, 2 }, new[] { 2, 1 }, new[] { 3, 4 } });
                var s = Base(no, 3);
                s.order = Order.AreaValue; s.m = mn[0]; s.n = mn[1];
                if (r.Next(2) == 0)
                {
                    var pool = BHPool(s.m, s.n); if (pool.Count == 0) continue;
                    var t = Pick(pool);
                    s.shape = Shape.TriBH; s.os[1] = t[0]; s.oh = t[1];
                    // 그림용 수선의 발(밑변 안쪽, 5 mm 칸) — 넓이·판정과 무관
                    int f = (int)Math.Round(t[0] * (0.3 + r.NextDouble() * 0.4) / 5.0) * 5;
                    s.ofoot = Math.Max(5, Math.Min(t[0] - 5, f));
                    s.gauge = 1;
                }
                else
                {
                    var pool = RectPool(s.m, s.n, true); if (pool.Count == 0) continue;
                    var t = Pick(pool);
                    s.shape = Shape.Rect; s.os[0] = t[0]; s.os[1] = t[1]; s.os[2] = t[0]; s.os[3] = t[1];
                    s.gauge = r.Next(2);
                }
                s.areaA = Judge.AreaCm2(s);
                s.areaB = s.areaA * s.n * s.n / (s.m * s.m);
                s.misconceptionId = "M1_area_linear";
                SetRange(s);
                s.handleStart = LinearStart(s);
                s.Reset();
                return s;
            }
            return Area(no);
        }

        /// <summary>넓이 주문의 손잡이 시작 = 넓이의 비를 닮음비로 쓴 위치(M1). 칸·범위를 벗어나면 먼 곳.</summary>
        int LinearStart(Sheet s)
        {
            int tgt = Judge.Target(s);
            long num = (long)Judge.GaugeOrig(s) * s.n * s.n;
            long den = (long)s.m * s.m;
            if (num % den == 0)
            {
                int w = (int)(num / den);
                if (w % Rules.Step == 0 && InRail(s, w) && w != tgt) return w;
            }
            return FarStart(s, tgt);
        }

        static bool InRail(Sheet s, int mm) => mm >= s.railLo && mm <= s.railHi && mm % Rules.Step == 0;

        /// <summary>손잡이 구간: 배율 50% ~ min(250%, 그림 17 cm) — 5 mm 칸으로 안쪽 반올림.</summary>
        public static void SetRange(Sheet s)
        {
            int G = Judge.GaugeOrig(s);
            int maxSide = s.shape == Shape.Rect ? Math.Max(s.os[0], s.os[1]) : s.shape == Shape.TriBH ? Math.Max(s.os[1], s.oh) : Math.Max(s.os[0], Math.Max(s.os[1], s.os[2]));
            int lo = (G + 1) / 2; lo = (lo + Rules.Step - 1) / Rules.Step * Rules.Step;
            long hiA = (long)G * 5 / 2, hiB = (long)G * 170 / maxSide;
            int hi = (int)Math.Min(hiA, hiB) / Rules.Step * Rules.Step;
            s.railLo = Math.Max(Rules.RailMin, lo); s.railHi = Math.Min(Rules.RailMax, hi);
        }

        /// <summary>정답에서 max(15 mm, 30%) 이상 떨어진 칸 중 무작위(무입력 봇 진도 0 보장).</summary>
        int FarStart(Sheet s, int target)
        {
            int gap = Math.Max(15, target * 3 / 10);
            var c = new List<int>();
            for (int p = s.railLo; p <= s.railHi; p += Rules.Step) if (Math.Abs(p - target) >= gap) c.Add(p);
            if (c.Count == 0) for (int p = s.railLo; p <= s.railHi; p += Rules.Step) if (p != target) c.Add(p);
            return Pick(c);
        }

        // ───────────── 판별 장 (레일 고정 — 닮음이면 찍기, 아니면 치우기. 닮음/아님 반반)
        Sheet JudgeSheet(int no, Cond cond)
        {
            for (int guard = 0; guard < 500; guard++)
            {
                var s = Base(no, no >= 10 ? 3 : 2);
                s.kind = Kind.Judge; s.order = Order.Judge; s.cond = cond;
                bool want = r.Next(2) == 0;
                bool ok = cond == Cond.SSS ? MakeSSS(s, want) : cond == Cond.SAS ? MakeSAS(s, want) : cond == Cond.AA ? MakeAA(s, want) : MakeRect(s, want);
                if (!ok) continue;
                if (Judge.Similar(s) != want) continue;   // 정수 판정과 의도가 다르면 버린다
                s.similar = want;
                s.rot = cond == Cond.Rect ? 0 : r.Next(4) * 90;
                s.handleStart = 0; s.Reset();
                return s;
            }
            throw new Exception("판별 장 생성 실패 " + cond);
        }

        static readonly int[][] Factors = { new[] { 2, 1 }, new[] { 3, 2 }, new[] { 1, 2 }, new[] { 5, 2 }, new[] { 4, 3 }, new[] { 3, 1 } };

        bool Scale(int v, int[] f, out int o) { o = 0; if ((v * f[0]) % f[1] != 0) return false; o = v * f[0] / f[1]; return o % 5 == 0; }

        bool MakeSSS(Sheet s, bool similar)
        {
            s.shape = Shape.Tri;
            var t = Pick(TriPool(1, 1).FindAll(x => x[0] % 10 == 0 && x[1] % 10 == 0 && x[2] % 10 == 0 && Math.Max(x[0], Math.Max(x[1], x[2])) <= 70));
            s.os[0] = t[0]; s.os[1] = t[1]; s.os[2] = t[2];
            var c = new int[3];
            if (similar)
            {
                var f = Factors[r.Next(Factors.Length)];
                for (int i = 0; i < 3; i++) if (!Scale(t[i], f, out c[i])) return false;
            }
            else if (r.Next(2) == 0)
            {
                int k = 10 * (1 + r.Next(3));
                for (int i = 0; i < 3; i++) c[i] = t[i] + k;
                s.misconceptionId = "M4_additive";
            }
            else
            {
                var f = Factors[r.Next(Factors.Length)];
                for (int i = 0; i < 3; i++) if (!Scale(t[i], f, out c[i])) return false;
                int j = r.Next(3); c[j] += (r.Next(2) == 0 ? -1 : 1) * 10;
                s.misconceptionId = "M3_looks_alike";
            }
            if (Math.Max(c[0], Math.Max(c[1], c[2])) > 125 || Math.Min(c[0], Math.Min(c[1], c[2])) < 15) return false;
            // 후보 세 변은 대응 순서를 섞어 적는다(정렬해서 비를 봐야 한다)
            var p = Perms[r.Next(6)];
            for (int i = 0; i < 3; i++) s.cs[i] = c[p[i]];
            if (Judge.MinAngle(s.cs[0], s.cs[1], s.cs[2]) < 22) return false;
            return true;
        }

        static readonly int[] SasAngles = { 40, 50, 60, 70, 80, 100, 110 };

        bool MakeSAS(Sheet s, bool similar)
        {
            s.shape = Shape.Tri;
            int c0 = 10 * (2 + r.Next(5)), a0 = 10 * (2 + r.Next(5));
            if (c0 == a0) return false;
            s.os[0] = c0; s.os[1] = a0; s.oAng = SasAngles[r.Next(SasAngles.Length)];
            int c1, a1;
            if (similar || r.Next(2) == 0)
            {
                var f = Factors[r.Next(Factors.Length)];
                if (!Scale(c0, f, out c1) || !Scale(a0, f, out a1)) return false;
                s.cAng = s.oAng;
                if (!similar) { s.cAng = s.oAng + (r.Next(2) == 0 ? -1 : 1) * (10 + 10 * r.Next(2)); s.misconceptionId = "M3_looks_alike"; }
            }
            else
            {
                int k = 10 * (1 + r.Next(3));
                c1 = c0 + k; a1 = a0 + k; s.cAng = s.oAng;
                s.misconceptionId = "M4_additive";
            }
            if (s.cAng < 30 || s.cAng > 125) return false;
            if (Math.Max(c1, a1) > 120 || Math.Min(c1, a1) < 15) return false;
            s.cs[0] = c1; s.cs[1] = a1;
            if (SasMinAngle(c0, a0, s.oAng) < 20 || SasMinAngle(c1, a1, s.cAng) < 20) return false;
            if (ThirdSide(c0, a0, s.oAng) > 85) return false;
            return true;
        }

        /// <summary>두 변과 끼인각으로 만든 삼각형의 가장 작은 각(그림 품질 검사용 실수 — 판정에는 쓰지 않는다).</summary>
        static double SasMinAngle(int c, int a, int deg)
        {
            double th = deg * Math.PI / 180, b = ThirdSide(c, a, deg);
            double A = Math.Acos(Math.Max(-1, Math.Min(1, (b * b + c * c - a * (double)a) / (2 * b * c))));
            double C = Math.PI - th - A;
            return Math.Min(th, Math.Min(A, C)) * 180 / Math.PI;
        }
        static double ThirdSide(int c, int a, int deg) => Math.Sqrt(c * (double)c + a * (double)a - 2.0 * a * c * Math.Cos(deg * Math.PI / 180));

        bool MakeAA(Sheet s, bool similar)
        {
            s.shape = Shape.Tri;
            int A = 5 * (7 + r.Next(11)), B = 5 * (7 + r.Next(11)), C = 180 - A - B;
            if (C < 35 || C > 100 || A == B || B == C || A == C) return false;
            s.oA[0] = A; s.oA[1] = B; s.oA[2] = C;
            s.os[0] = 50;   // 그림용 AB(표시하지 않는다)
            var shown = new[] { 3, 5, 6 }[r.Next(3)];   // 후보에서 보여 줄 두 각: DE / DF / EF
            s.cMask = shown;
            var c = new[] { A, B, C };
            if (!similar)
            {
                // 보여 주는 각 하나를 10° 또는 15° 비튼다(모양은 비슷해 보인다 — M3)
                var idx = new List<int>(); for (int i = 0; i < 3; i++) if ((shown & (1 << i)) != 0) idx.Add(i);
                int j = idx[r.Next(2)];
                int d = (r.Next(2) == 0 ? -1 : 1) * (10 + 5 * r.Next(2));
                c[j] += d;
                int hidden = 3 - idx[0] - idx[1];
                c[hidden] = 180 - c[idx[0]] - c[idx[1]];
                if (c[hidden] < 25 || c[j] < 25) return false;
                s.misconceptionId = "M3_looks_alike";
            }
            s.cA[0] = c[0]; s.cA[1] = c[1]; s.cA[2] = c[2];
            s.cs[0] = 5 * (13 + r.Next(10));   // 그림용 DE
            return true;
        }

        bool MakeRect(Sheet s, bool similar)
        {
            s.shape = Shape.Rect;
            int h = 10 * (2 + r.Next(4)), w = 10 * (2 + r.Next(5));
            if (h == w || Math.Max(h, w) * 4 < Math.Min(h, w) * 5) return false;
            s.os[0] = h; s.os[1] = w; s.os[2] = h; s.os[3] = w;
            int h1, w1;
            if (similar)
            {
                var f = Factors[r.Next(Factors.Length)];
                if (!Scale(h, f, out h1) || !Scale(w, f, out w1)) return false;
            }
            else if (r.Next(2) == 0)
            {
                int k = 10 * (1 + r.Next(3)); h1 = h + k; w1 = w + k; s.misconceptionId = "M4_additive";
            }
            else
            {
                var f = Factors[r.Next(Factors.Length)];
                if (!Scale(h, f, out h1) || !Scale(w, f, out w1)) return false;
                if (r.Next(2) == 0) h1 += 10; else w1 += 10;
                s.misconceptionId = "M3_looks_alike";
            }
            if (Math.Max(h1, w1) > 120 || Math.Min(h1, w1) < 15) return false;
            s.cs[0] = h1; s.cs[1] = w1;
            return true;
        }
    }

    // ─────────────────────────────────────────────── 문장 (티켓·드러난 수학·문제 은행이 같은 함수를 쓴다)
    public static class Words
    {
        public static string OL(Sheet s) => s.shape == Shape.Rect ? "ABCD" : "ABC";
        public static string IL(Sheet s) => s.shape == Shape.Rect ? "EFGH" : "DEF";
        static string Fig(Sheet s) => s.shape == Shape.Rect ? "□" : "△";

        public static string Cm(int mm) => mm % 10 == 0 ? (mm / 10).ToString() : (mm / 10) + "." + (mm % 10);
        public static string CmR(long num, long den)
        {
            // num/den mm 를 0.1 cm(= 1 mm) 로 반올림해 cm 로
            long mm = (2 * num + den) / (2 * den);
            return mm % 10 == 0 ? (mm / 10).ToString() : (mm / 10) + "." + (mm % 10);
        }
        public static string Ratio(int a, int b) { int g = Judge.Gcd(a, b); if (g == 0) g = 1; return (a / g) + ":" + (b / g); }
        public static string R(int m, int n) => m + ":" + n;

        /// <summary>찍을 도형 기호(대응 순서): △ABC∽△EFD 의 뒤쪽.</summary>
        public static string ImgName(Sheet s)
        {
            var il = IL(s); var sb = new StringBuilder(Fig(s));
            for (int i = 0; i < s.N; i++) sb.Append(il[s.corr[i]]);
            return sb.ToString();
        }
        public static string Sym(Sheet s) => Fig(s) + OL(s) + "∽" + ImgName(s);

        public static string OrigSide(Sheet s, int i) { var ol = OL(s); return ol[i].ToString() + ol[(i + 1) % s.N]; }
        /// <summary>원본 변 i 의 대응변 이름(알파벳 순).</summary>
        public static string ImgSide(Sheet s, int i)
        {
            var il = IL(s); char a = il[s.corr[i]], b = il[s.corr[(i + 1) % s.N]];
            return a < b ? a.ToString() + b : b.ToString() + a;
        }
        public static string GaugeName(Sheet s) => ImgSide(s, s.gauge);

        public static string OrderLine(Sheet s)
        {
            switch (s.order)
            {
                case Order.Ratio:
                case Order.Corr:
                    return Sym(s) + "이고 닮음비가 " + R(s.m, s.n) + "일 때, 닮음비에 맞게 " + ImgName(s) + "를 찍으시오.";
                case Order.Area:
                    return Sym(s) + "이고 넓이의 비가 " + R(s.areaA, s.areaB) + "일 때, 알맞은 닮음비로 " + ImgName(s) + "를 찍으시오.";
                case Order.AreaValue:
                    return Sym(s) + "이고 " + Fig(s) + OL(s) + "의 넓이가 " + s.areaA + " cm²일 때, " + ImgName(s) + "의 넓이가 " + s.areaB + " cm²가 되게 찍으시오.";
                default:
                    return (s.shape == Shape.Rect ? "□ABCD와 □EFGH" : "△ABC와 △DEF") + "가 닮은 도형이면 찍고, 아니면 치우시오.";
            }
        }

        /// <summary>주어진 것(원본 길이 · 판별 장은 두 도형).</summary>
        public static string DataLine(Sheet s)
        {
            if (s.kind == Kind.Judge)
            {
                switch (s.cond)
                {
                    case Cond.SSS:
                        return "AB = " + Cm(s.os[0]) + " cm, BC = " + Cm(s.os[1]) + " cm, CA = " + Cm(s.os[2]) + " cm\nDE = " + Cm(s.cs[0]) + " cm, EF = " + Cm(s.cs[1]) + " cm, FD = " + Cm(s.cs[2]) + " cm";
                    case Cond.SAS:
                        return "AB = " + Cm(s.os[0]) + " cm, BC = " + Cm(s.os[1]) + " cm, ∠B = " + s.oAng + "°\nDE = " + Cm(s.cs[0]) + " cm, EF = " + Cm(s.cs[1]) + " cm, ∠E = " + s.cAng + "°";
                    case Cond.AA:
                        {
                            var sb = new StringBuilder("∠A = " + s.oA[0] + "°, ∠B = " + s.oA[1] + "°\n");
                            bool first = true;
                            for (int i = 0; i < 3; i++) if ((s.cMask & (1 << i)) != 0) { if (!first) sb.Append(", "); sb.Append("∠" + "DEF"[i] + " = " + s.cA[i] + "°"); first = false; }
                            return sb.ToString();
                        }
                    default:
                        return "AB = " + Cm(s.os[0]) + " cm, BC = " + Cm(s.os[1]) + " cm\nEF = " + Cm(s.cs[0]) + " cm, FG = " + Cm(s.cs[1]) + " cm";
                }
            }
            string d;
            if (s.shape == Shape.TriBH) d = "BC = " + Cm(s.os[1]) + " cm, 높이 " + Cm(s.oh) + " cm";
            else if (s.shape == Shape.Rect) d = "AB = " + Cm(s.os[0]) + " cm, BC = " + Cm(s.os[1]) + " cm";
            else d = "AB = " + Cm(s.os[0]) + " cm, BC = " + Cm(s.os[1]) + " cm, CA = " + Cm(s.os[2]) + " cm";
            return d + "\n레일이 재는 변: " + GaugeName(s);
        }

        public static string Prompt(Sheet s) => OrderLine(s) + " " + DataLine(s).Replace("\n", ", ") + ".";

        public static string Answer(Sheet s)
        {
            if (s.kind == Kind.Build) return GaugeName(s) + " = " + Cm(Judge.Target(s)) + " cm";
            return Judge.Similar(s) ? "찍기 (" + CondName(s) + ")" : "치우기 (닮음이 아니다)";
        }

        static string CondName(Sheet s) => s.cond == Cond.SSS ? "SSS 닮음" : s.cond == Cond.SAS ? "SAS 닮음" : s.cond == Cond.AA ? "AA 닮음" : "닮은 직사각형";

        public static string Concept(Sheet s)
        {
            switch (s.order)
            {
                case Order.Ratio: return "닮음비와 대응변의 길이";
                case Order.Corr: return "닮음 기호의 대응 순서와 대응변";
                case Order.Area: return "닮은 도형의 넓이의 비";
                case Order.AreaValue: return "닮은 도형의 넓이의 비";
                default: return s.cond == Cond.Rect ? "닮은 도형(직사각형)" : "삼각형의 닮음 조건";
            }
        }

        public static string Foot(Sheet s) => s.Locked ? "레일 고정 · 닮음이면 ↓ 찍기 · 아니면 ← 치우기" : "손잡이로 " + GaugeName(s) + "를 맞추고 ↓ 찍기";

        /// <summary>1단계 현재 비: 「AB : DE = 4 cm : 5.5 cm = 8 : 11」.</summary>
        public static string Readout(Sheet s)
        {
            int o = Judge.GaugeOrig(s);
            return OrigSide(s, s.gauge) + " : " + GaugeName(s) + " = " + Cm(o) + " cm : " + Cm(s.handle) + " cm = " + Ratio(o, s.handle);
        }

        // ── 드러난 수학(정답)
        public static string RevealRight(Sheet s, Act a)
        {
            int o = Judge.GaugeOrig(s), t = Judge.Target(s);
            string og = OrigSide(s, s.gauge), ig = GaugeName(s);
            switch (s.order)
            {
                case Order.Ratio:
                    {
                        int k = (s.gauge + 1) % s.N;
                        if (s.shape == Shape.Rect && s.os[k] == s.os[s.gauge]) k = (k + 1) % s.N;
                        int ok = s.os[k], ik = ok * s.n / s.m;
                        return og + " : " + ig + " = " + Cm(o) + " cm : " + Cm(t) + " cm = " + R(s.m, s.n) + "\n" + OrigSide(s, k) + " : " + ImgSide(s, k) + " = " + Cm(ok) + " cm : " + Cm(ik) + " cm → 닮음비 " + R(s.m, s.n);
                    }
                case Order.Corr:
                    return Sym(s) + " → " + ig + "의 대응변은 " + og + "\n" + og + " : " + ig + " = " + Cm(o) + " cm : " + Cm(t) + " cm = " + R(s.m, s.n);
                case Order.Area:
                    return "넓이의 비 " + R(s.areaA, s.areaB) + " = " + s.m + "²:" + s.n + "² → 닮음비 " + R(s.m, s.n) + "\n" + og + " : " + ig + " = " + Cm(o) + " cm : " + Cm(t) + " cm";
                case Order.AreaValue:
                    return s.areaA + " : " + s.areaB + " = " + Ratio(s.areaA, s.areaB) + " = " + s.m + "²:" + s.n + "² → 닮음비 " + R(s.m, s.n) + "\n" + og + " : " + ig + " = " + Cm(o) + " cm : " + Cm(t) + " cm";
            }
            return JudgeWhy(s) + (a == Act.Stamp ? "" : " · 치웠다");
        }

        /// <summary>판별 장의 근거 한 줄.</summary>
        public static string JudgeWhy(Sheet s)
        {
            bool sim = Judge.Similar(s);
            switch (s.cond)
            {
                case Cond.SSS:
                    {
                        var o = new[] { s.os[0], s.os[1], s.os[2] }; var c = new[] { s.cs[0], s.cs[1], s.cs[2] };
                        Array.Sort(o); Array.Sort(c);
                        if (sim) return Cm(o[0]) + " : " + Cm(c[0]) + " = " + Cm(o[1]) + " : " + Cm(c[1]) + " = " + Cm(o[2]) + " : " + Cm(c[2]) + " = " + Ratio(o[0], c[0]) + " → SSS 닮음";
                        for (int i = 1; i < 3; i++)
                            if ((long)o[0] * c[i] != (long)o[i] * c[0])
                                return Cm(o[0]) + " : " + Cm(c[0]) + " = " + Ratio(o[0], c[0]) + ", " + Cm(o[i]) + " : " + Cm(c[i]) + " = " + Ratio(o[i], c[i]) + " → 비가 일정하지 않다" + (s.misconceptionId == "M4_additive" ? "\n길이의 차가 같아도 닮음이 아니다" : "");
                        return "비가 일정하지 않다";
                    }
                case Cond.SAS:
                    if (sim) return "∠B = ∠E = " + s.oAng + "°, AB : DE = BC : EF = " + Ratio(s.os[0], s.cs[0]) + " → SAS 닮음";
                    if (s.oAng != s.cAng) return "∠B = " + s.oAng + "°, ∠E = " + s.cAng + "° → 끼인각이 다르다";
                    return "AB : DE = " + Ratio(s.os[0], s.cs[0]) + ", BC : EF = " + Ratio(s.os[1], s.cs[1]) + " → 비가 다르다" + (s.misconceptionId == "M4_additive" ? "\n길이의 차가 같아도 닮음이 아니다" : "");
                case Cond.AA:
                    {
                        int hid = 0; for (int i = 0; i < 3; i++) if ((s.cMask & (1 << i)) == 0) hid = i;
                        string third = "∠C = " + s.oA[2] + "°, ∠" + "DEF"[hid] + " = " + s.cA[hid] + "°";
                        return sim ? third + " → 세 각이 같다 → AA 닮음" : third + " → 대응각이 다르다";
                    }
                default:
                    if (sim) return "AB : EF = BC : FG = " + Ratio(s.os[0], s.cs[0]) + " → 닮음";
                    return "AB : EF = " + Ratio(s.os[0], s.cs[0]) + ", BC : FG = " + Ratio(s.os[1], s.cs[1]) + " → 비가 다르다\n모든 직사각형이 닮은 것은 아니다";
            }
        }

        // ── 드러난 수학(오답)
        public static string RevealWrong(Sheet s, Act a)
        {
            if (s.kind == Kind.Judge)
                return JudgeWhy(s) + "\n" + (Judge.Similar(s) ? "찍었어야 했다" : "치웠어야 했다");
            int o = Judge.GaugeOrig(s), t = Judge.Target(s);
            string og = OrigSide(s, s.gauge), ig = GaugeName(s);
            if (a == Act.Discard) return "이 장은 닮음비만 맞추면 찍을 수 있다\n" + ig + " = " + Cm(t) + " cm";
            string head = og + " : " + ig + " = " + Cm(o) + " : " + Cm(s.handle) + " = " + Ratio(o, s.handle) + " ≠ " + R(s.m, s.n);
            if (s.order == Order.Area || s.order == Order.AreaValue)
            {
                long lin = (long)o * s.n * s.n;
                if (lin % (s.m * s.m) == 0 && (int)(lin / (s.m * s.m)) == s.handle) head = "넓이의 비를 닮음비로 썼다";
                return head + "\n넓이의 비 " + s.m + "²:" + s.n + "² → 닮음비 " + R(s.m, s.n) + " · " + ig + " = " + Cm(t) + " cm";
            }
            if (s.order == Order.Corr)
            {
                if (s.os[0] * s.n == s.handle * s.m) head = "AB를 " + ig + "의 대응변으로 잡았다";
                return head + "\n" + Sym(s) + " → " + ig + "의 대응변은 " + og + " · " + ig + " = " + Cm(t) + " cm";
            }
            return head + "\n" + ig + " = " + Cm(t) + " cm여야 한다";
        }
    }
}
