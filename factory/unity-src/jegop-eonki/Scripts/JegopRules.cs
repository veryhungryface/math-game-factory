// 제곱 얹기 — 수학 모델·생성기·판정·발문. 판정은 정수 곱만(float 비교 없음).
//
// ■ 모델
//   직각삼각형 변 a=BC, b=AC, c=AB(빗변). 쟁반 넓이 S 에 대해 잠금은 S == n*n (long 곱).
//   역 판별은 세 변을 정렬해 c=max, a²+b²==c². 코드는 기왓장을 정답 쟁반으로 밀어 넣지 않는다.
//
// ■ expression_traps (2022-middle-math.json) 을 생성기 규칙으로 옮김
//   · 중2 에 √ 없음 — 피타고라스 수와 그 배수만. 길이 답은 자연수. 근호·제곱근 금지.
//   · 풀이 흐름은 x²=25, x>0이므로 x=5. 발문 종결은 「~을/를 구하시오」.
//   · 수와 단위 사이 공백(6 cm, 25 cm²).
//   · 역은 가장 긴 변을 c 로. 삼각형이 안 되는 세 수(c>=a+b)는 출제하지 않는다.
//   · 「○의 자리에서」 어림 표현 안 씀.
//
// ■ 오개념 → 코드 (misconceptionId)
//   M1_leg_as_hyp     빗변이 아닌 변을 c 자리에. 화면 하단·우측 변에 a²+b² 를 잠금
//   M2_sum_not_sq      a+b=c 로 제곱을 뺀다. 독에 a+b 칸 판
//   M3_apply_not_right 직각이 아닌 삼각형에 정리를 적용. 역에서 폐기해야 하는데 도장
//   M4_not_max_c       가장 긴 변을 c 로 두지 않고 짧은 변 맞은편에 도장
using System;
using System.Collections.Generic;

namespace Mgf.JegopEonki
{
    public enum Kind { AreaHyp, LenHyp, LenLeg, Reverse }

    public static class Rules
    {
        public const float RunSec = 90f;
        public const int Lives = 3;
        public const int Locks = 10;
        public const float SheetSec = 12f;
        public const float SheetSecFast = 10f;
        public const int UntimedSheets = 2;
        public static int Mult(int combo) => combo >= 3 ? 3 : combo < 1 ? 1 : combo;
        public static float SheetLimit(int stage) => stage >= 3 ? SheetSecFast : SheetSec;
    }

    public class Sheet
    {
        public int no, stage = 1, rot;
        public Kind kind = Kind.AreaHyp;
        public int a, b, c;                 // BC, AC, AB
        public int hide;                    // 0 none, 1 a, 2 b, 3 c
        public bool showRight = true, ghostOn;
        public int[] dock = new int[3];
        public string[] dockMis = new string[3];
        public int targetSide;              // 0 a, 1 b, 2 c
        public int targetArea;
        public int asked;
        public string askedUnit = "cm²";
        public bool reverseTrue = true;
        public int rightVertex = 2;         // 0 A, 1 B, 2 C
        public string misconceptionId = "M2_sum_not_sq";
        public int givenLeg;                // LenLeg: 0=a given, 1=b given
        public int[] sides;                 // reverse 제시 순서
        public int flavor;                  // 발문 변형(은행 다양성)

        public int Side(int i) => i == 0 ? a : i == 1 ? b : c;
        public int Sq(int i) { int n = Side(i); return n * n; }
        public Sheet Clone()
        {
            var s = (Sheet)MemberwiseClone();
            s.dock = (int[])dock.Clone();
            s.dockMis = (string[])dockMis.Clone();
            if (sides != null) s.sides = (int[])sides.Clone();
            return s;
        }
    }

    public static class Judge
    {
        public static bool IsRight(int a, int b, int c)
        {
            int x = a, y = b, z = c;
            if (x > y) { int t = x; x = y; y = t; }
            if (y > z) { int t = y; y = z; z = t; }
            if (x > y) { int t = x; x = y; y = t; }
            return (long)x * x + (long)y * y == (long)z * z;
        }

        public static bool IsTriangle(int a, int b, int c)
        {
            int x = a, y = b, z = c;
            if (x > y) { int t = x; x = y; y = t; }
            if (y > z) { int t = y; y = z; z = t; }
            if (x > y) { int t = x; x = y; y = t; }
            return x + y > z && x > 0;
        }

        public static int MaxI(int a, int b, int c)
        {
            if (a >= b && a >= c) return 0;
            if (b >= a && b >= c) return 1;
            return 2;
        }

        public static bool LockOk(Sheet s, int tray, int area)
        {
            if (s == null || tray < 0 || tray > 2 || area <= 0) return false;
            int n = s.Side(tray);
            if (n <= 0) return false;
            return (long)area == (long)n * n;
        }

        public static bool Completes(Sheet s, int tray, int area)
        {
            if (!LockOk(s, tray, area)) return false;
            return tray == s.targetSide && area == s.targetArea;
        }

        public static bool StampOk(Sheet s, int vertex)
        {
            if (s == null || s.kind != Kind.Reverse) return false;
            if (!s.reverseTrue) return false;
            return vertex == s.rightVertex;
        }

        public static bool DiscardOk(Sheet s)
        {
            if (s == null || s.kind != Kind.Reverse) return false;
            return !s.reverseTrue;
        }
    }

    public static class Words
    {
        public static string Cm(int n) => n + " cm";
        public static string Cm2(int n) => n + " cm²";

        public static string Prompt(Sheet s)
        {
            if (s == null) return "변 위의 정사각형 넓이를 구하시오.";
            switch (s.kind)
            {
                case Kind.AreaHyp:
                    if (s.flavor == 1)
                        return "∠C=90°인 직각삼각형 ABC의 세 변을 각각 한 변으로 하는 정사각형을 그렸다. BC, AC 위의 정사각형의 넓이가 각각 "
                               + Cm2(s.a * s.a) + ", " + Cm2(s.b * s.b) + "일 때, AB 위의 정사각형의 넓이를 구하시오.";
                    if (s.flavor == 2)
                        return "∠C=90°인 직각삼각형 ABC에서 직각을 낀 두 변 위의 정사각형의 넓이가 " + Cm2(s.a * s.a)
                               + ", " + Cm2(s.b * s.b) + "일 때, 빗변 위 정사각형의 넓이를 구하시오.";
                    return "∠C=90°인 직각삼각형 ABC의 세 변을 각각 한 변으로 하는 정사각형을 그렸다. BC="
                           + Cm(s.a) + ", AC=" + Cm(s.b) + "일 때, AB 위의 정사각형의 넓이를 구하시오.";
                case Kind.LenHyp:
                    if (s.flavor == 1)
                        return "∠C=90°인 직각삼각형 ABC에서 AC=" + Cm(s.b) + ", BC=" + Cm(s.a)
                               + "일 때, 빗변 AB의 길이를 구하시오.";
                    if (s.flavor == 2)
                        return "직각삼각형의 직각을 낀 두 변의 길이가 " + Cm(s.b) + ", " + Cm(s.a)
                               + "일 때, 빗변의 길이를 구하시오.";
                    return "직각을 낀 두 변의 길이가 " + Cm(s.a) + ", " + Cm(s.b)
                           + "인 직각삼각형의 빗변의 길이를 구하시오.";
                case Kind.LenLeg:
                    if (s.givenLeg == 0)
                    {
                        if (s.flavor == 1)
                            return "∠C=90°인 직각삼각형 ABC에서 빗변 AB=" + Cm(s.c) + ", BC=" + Cm(s.a)
                                   + "일 때, AC의 길이를 구하시오.";
                        if (s.flavor == 2)
                            return "빗변의 길이가 " + Cm(s.c) + "이고 한 변의 길이가 " + Cm(s.a)
                                   + "인 직각삼각형에서 다른 한 변의 길이를 구하시오.";
                        return "빗변의 길이가 " + Cm(s.c) + "이고 다른 한 변의 길이가 " + Cm(s.a)
                               + "인 직각삼각형의 나머지 한 변의 길이를 구하시오.";
                    }
                    if (s.flavor == 1)
                        return "∠C=90°인 직각삼각형 ABC에서 빗변 AB=" + Cm(s.c) + ", AC=" + Cm(s.b)
                               + "일 때, BC의 길이를 구하시오.";
                    if (s.flavor == 2)
                        return "빗변의 길이가 " + Cm(s.c) + "이고 한 변의 길이가 " + Cm(s.b)
                               + "인 직각삼각형에서 다른 한 변의 길이를 구하시오.";
                    return "빗변의 길이가 " + Cm(s.c) + "이고 다른 한 변의 길이가 " + Cm(s.b)
                           + "인 직각삼각형의 나머지 한 변의 길이를 구하시오.";
                case Kind.Reverse:
                {
                    int x = s.sides != null ? s.sides[0] : s.a;
                    int y = s.sides != null ? s.sides[1] : s.b;
                    int z = s.sides != null ? s.sides[2] : s.c;
                    return "세 변의 길이가 " + Cm(x) + ", " + Cm(y) + ", " + Cm(z)
                           + "인 삼각형은 직각삼각형인지 판별하시오.";
                }
                default:
                    return "피타고라스 정리를 이용하여 구하시오.";
            }
        }

        public static string Answer(Sheet s)
        {
            if (s.kind == Kind.Reverse)
                return s.reverseTrue ? "직각삼각형이다" : "직각삼각형이 아니다";
            if (s.askedUnit == "cm²") return Cm2(s.asked);
            return Cm(s.asked);
        }

        public static string[] Choices(Sheet s)
        {
            if (s.kind == Kind.Reverse)
                return new[]
                {
                    "직각삼각형이다",
                    "직각삼각형이 아니다"
                };
            var set = new List<string>();
            string ans = Answer(s);
            set.Add(ans);
            if (s.kind == Kind.AreaHyp || s.kind == Kind.LenHyp || s.kind == Kind.LenLeg)
            {
                foreach (int d in s.dock)
                {
                    if (d <= 0) continue;
                    string w = s.askedUnit == "cm²" ? Cm2(d) : Cm(d);
                    if (!set.Contains(w)) set.Add(w);
                }
                int[] extra = ExtraDistractors(s);
                for (int i = 0; i < extra.Length && set.Count < 4; i++)
                {
                    if (extra[i] <= 0) continue;
                    string w = s.askedUnit == "cm²" ? Cm2(extra[i]) : Cm(extra[i]);
                    if (!set.Contains(w)) set.Add(w);
                }
            }
            while (set.Count < 4)
            {
                int pad = s.asked + set.Count + 3;
                if (pad == s.asked) pad += 5;
                string w = s.askedUnit == "cm²" ? Cm2(pad) : Cm(pad);
                if (!set.Contains(w)) set.Add(w);
            }
            if (set.Count > 4) set.RemoveRange(4, set.Count - 4);
            return set.ToArray();
        }

        static int[] ExtraDistractors(Sheet s)
        {
            if (s.kind == Kind.AreaHyp)
                return new[] { s.a + s.b, s.a * s.b, s.a * s.a };
            if (s.kind == Kind.LenHyp)
                return new[] { s.a + s.b, Math.Abs(s.b - s.a), s.a * s.a };
            int given = s.givenLeg == 0 ? s.a : s.b;
            return new[] { s.c - given, s.c + given, given };
        }

        public static string Concept(Sheet s)
        {
            switch (s.kind)
            {
                case Kind.AreaHyp: return "정사각형의 넓이로 보는 피타고라스 정리";
                case Kind.LenHyp: return "빗변의 길이 구하기";
                case Kind.LenLeg: return "다른 한 변의 길이 구하기";
                default: return "피타고라스 정리의 역(직각삼각형 판별)";
            }
        }

        public static string Goal(Sheet s)
        {
            if (s == null) return "넓이판을 변에 얹고 잠가라";
            if (s.kind == Kind.Reverse)
                return s.reverseTrue ? "가장 긴 변 맞은편 꼭짓점에 직각 도장을 찍어라" : "직각이 아니면 지그를 폐기하라";
            if (s.kind == Kind.LenLeg) return "빈 변 쟁반에 넓이판을 얹고 잠가라";
            if (s.kind == Kind.LenHyp) return "빗변 쟁반에 넓이판을 얹고 잠가라";
            return "두 넓이판을 빗변 쟁반에 얹고 잠가라";
        }

        public static string RevealRight(Sheet s)
        {
            if (s.kind == Kind.AreaHyp)
                return (s.a * s.a) + "+" + (s.b * s.b) + "=" + (s.c * s.c) + "  ·  빗변 위 정사각형의 넓이";
            if (s.kind == Kind.LenHyp)
                return "x²=" + (s.c * s.c) + ", x>0이므로 x=" + s.c;
            if (s.kind == Kind.LenLeg)
            {
                int miss = s.givenLeg == 0 ? s.b : s.a;
                return "x²=" + (s.c * s.c) + "−" + (s.givenLeg == 0 ? s.a * s.a : s.b * s.b) + "=" + (miss * miss)
                       + ", x>0이므로 x=" + miss;
            }
            if (s.reverseTrue)
                return "가장 긴 변 " + Cm(s.c) + "의 제곱이 다른 두 변 제곱의 합과 같다";
            return "가장 긴 변의 제곱이 다른 두 변 제곱의 합과 같지 않다";
        }

        public static string RevealWrong(Sheet s)
        {
            if (s.kind == Kind.Reverse)
                return s.reverseTrue ? "가장 긴 변 맞은편에 직각이 있다" : "직각삼각형이 아니므로 폐기하라";
            if (s.misconceptionId == "M2_sum_not_sq")
                return "길이의 합 " + (s.a + s.b) + " ≠ 넓이 " + (s.a * s.a) + "+" + (s.b * s.b);
            if (s.kind == Kind.LenLeg)
                return "빗변이 아닌 변의 제곱은 차 " + (s.c * s.c - (s.givenLeg == 0 ? s.a * s.a : s.b * s.b)) + " 이다";
            return "직각의 대변이 빗변이다. 넓이 = 변 길이의 제곱";
        }
    }

    public class SheetGen
    {
        static readonly int[,] Tri =
        {
            { 3, 4, 5 }, { 5, 12, 13 }, { 6, 8, 10 }, { 8, 15, 17 },
            { 7, 24, 25 }, { 9, 12, 15 }, { 20, 21, 29 }
        };
        static readonly int[,] FalseTri =
        {
            { 6, 8, 11 }, { 5, 12, 14 }, { 7, 24, 26 }, { 8, 15, 16 },
            { 9, 12, 16 }, { 20, 21, 30 }, { 3, 4, 6 }, { 5, 12, 12 },
            { 6, 8, 9 }, { 8, 15, 18 }, { 7, 24, 23 }, { 9, 12, 14 },
            { 4, 5, 6 }, { 6, 8, 12 }, { 10, 24, 25 }, { 12, 16, 21 },
            { 8, 15, 20 }, { 9, 12, 17 }, { 11, 13, 17 }, { 9, 40, 42 }
        };

        readonly Random rng;
        public SheetGen(Random rng) { this.rng = rng ?? new Random(1); }

        public static Sheet Practice()
        {
            var s = new Sheet
            {
                no = 0, stage = 1, rot = 0, kind = Kind.AreaHyp,
                a = 3, b = 4, c = 5, hide = 3, showRight = true, ghostOn = true,
                targetSide = 2, targetArea = 25, asked = 25, askedUnit = "cm²",
                misconceptionId = "M2_sum_not_sq"
            };
            s.dock = new[] { 9, 16, 7 };
            s.dockMis = new[] { "", "", "M2_sum_not_sq" };
            return s;
        }

        public List<Sheet> Deck()
        {
            var list = new List<Sheet>(10);
            list.Add(MakeArea(1, PickTrue(true), 90));
            list.Add(MakeArea(1, PickTrue(true), 0));
            list.Add(MakeArea(1, PickTrue(true), 180));
            list.Add(rng.Next(2) == 0 ? MakeLenHyp(2, PickTrue(false), 270) : MakeLenLeg(2, PickTrue(false), 90));
            list.Add(MakeLenLeg(2, PickTrue(false), 0));
            list.Add(MakeLenHyp(2, PickTrue(false), 180));
            list.Add(MakeLenLeg(2, PickTrue(false), 270));
            list.Add(MakeRev(3, true, PickTrue(false), 90));
            list.Add(MakeRev(3, false, PickFalse(), 0));
            list.Add(MakeRev(3, rng.Next(2) == 0, rng.Next(2) == 0 ? PickTrue(false) : PickFalse(), 180));
            for (int i = 0; i < list.Count; i++) list[i].no = i + 1;
            return list;
        }

        public Sheet NextFor(int no, int stage)
        {
            if (stage <= 1) return MakeArea(1, PickTrue(true), Rot());
            if (stage == 2) return rng.Next(2) == 0 ? MakeLenHyp(2, PickTrue(false), Rot()) : MakeLenLeg(2, PickTrue(false), Rot());
            bool ok = rng.Next(2) == 0;
            return MakeRev(3, ok, ok ? PickTrue(false) : PickFalse(), Rot());
        }

        public Sheet Any()
        {
            int k = rng.Next(5);
            if (k == 0) return MakeArea(1, PickTrue(false), Rot());
            if (k == 1) return MakeLenHyp(2, PickTrue(false), Rot());
            if (k == 2) return MakeLenLeg(2, PickTrue(false), Rot());
            if (k == 3) return MakeRev(3, true, PickTrue(false), Rot());
            return MakeRev(3, false, PickFalse(), Rot());
        }

        public List<Sheet> Exhaust()
        {
            var list = new List<Sheet>(400);
            int nR = Tri.GetLength(0);
            for (int r = 0; r < nR; r++)
            for (int k = 1; k <= 2; k++)
            {
                if (r >= 4 && k == 2) continue;
                var t = new[] { Tri[r, 0] * k, Tri[r, 1] * k, Tri[r, 2] * k };
                for (int fl = 0; fl < 3; fl++)
                {
                    var a = MakeArea(1, t, 0); a.flavor = fl; list.Add(a);
                    var h = MakeLenHyp(2, t, 90); h.flavor = fl; list.Add(h);
                    list.Add(MakeLenLegGiven(2, t, 180, 0, fl));
                    list.Add(MakeLenLegGiven(2, t, 270, 1, fl));
                }
                int[] perm = { t[0], t[1], t[2] };
                for (int p = 0; p < 6; p++)
                {
                    var rv = MakeRev(3, true, t, 0);
                    rv.sides = new[] { perm[0], perm[1], perm[2] };
                    list.Add(rv);
                    NextPerm(perm);
                }
            }
            int nF = FalseTri.GetLength(0);
            for (int r = 0; r < nF; r++)
            {
                var t = new[] { FalseTri[r, 0], FalseTri[r, 1], FalseTri[r, 2] };
                if (!Judge.IsTriangle(t[0], t[1], t[2]) || Judge.IsRight(t[0], t[1], t[2])) continue;
                int[] perm = { t[0], t[1], t[2] };
                for (int p = 0; p < 6; p++)
                {
                    var rv = MakeRev(3, false, t, 0);
                    rv.sides = new[] { perm[0], perm[1], perm[2] };
                    list.Add(rv);
                    NextPerm(perm);
                }
            }
            return list;
        }

        static void NextPerm(int[] a)
        {
            // 3원소 다음 순열
            int i = 1;
            while (i >= 0 && a[i] >= a[i + 1]) i--;
            if (i < 0) { System.Array.Reverse(a); return; }
            int j = 2;
            while (a[j] <= a[i]) j--;
            int tmp = a[i]; a[i] = a[j]; a[j] = tmp;
            System.Array.Reverse(a, i + 1, a.Length - i - 1);
        }

        int Rot() => rng.Next(4) * 90;

        int[] PickTrue(bool small)
        {
            int row;
            if (small)
            {
                int[] rows = { 0, 2, 5 }; // 3-4-5, 6-8-10, 9-12-15
                row = rows[rng.Next(rows.Length)];
            }
            else row = rng.Next(Tri.GetLength(0));
            int k = small ? 1 : (rng.Next(3) == 0 ? 2 : 1);
            if (row == 4 && k == 2) k = 1; // 14-48-50 은 화면 숫자만
            if (row == 6 && k == 2) k = 1;
            return new[] { Tri[row, 0] * k, Tri[row, 1] * k, Tri[row, 2] * k };
        }

        int[] PickFalse()
        {
            int row = rng.Next(FalseTri.GetLength(0));
            int a = FalseTri[row, 0], b = FalseTri[row, 1], c = FalseTri[row, 2];
            if (!Judge.IsTriangle(a, b, c) || Judge.IsRight(a, b, c))
            {
                a = 6; b = 8; c = 11;
            }
            return new[] { a, b, c };
        }

        Sheet Base(int stage, int[] t, int rot, Kind kind)
        {
            int a = t[0], b = t[1], c = t[2];
            if (a > b) { int tmp = a; a = b; b = tmp; }
            return new Sheet
            {
                stage = stage, rot = rot, kind = kind,
                a = a, b = b, c = c, showRight = kind != Kind.Reverse,
                ghostOn = kind == Kind.AreaHyp && stage <= 1,
                flavor = rng.Next(3)
            };
        }

        Sheet MakeArea(int stage, int[] t, int rot)
        {
            var s = Base(stage, t, rot, Kind.AreaHyp);
            s.hide = 3;
            s.targetSide = 2;
            s.targetArea = s.c * s.c;
            s.asked = s.c * s.c;
            s.askedUnit = "cm²";
            s.misconceptionId = "M2_sum_not_sq";
            FillDock(s, new[] { s.a * s.a, s.b * s.b, s.a + s.b },
                new[] { "", "", "M2_sum_not_sq" });
            return s;
        }

        Sheet MakeLenHyp(int stage, int[] t, int rot)
        {
            var s = Base(stage, t, rot, Kind.LenHyp);
            s.hide = 3;
            s.targetSide = 2;
            s.targetArea = s.c * s.c;
            s.asked = s.c;
            s.askedUnit = "cm";
            s.misconceptionId = "M2_sum_not_sq";
            FillDock(s, new[] { s.c * s.c, s.a + s.b, s.a * s.a },
                new[] { "", "M2_sum_not_sq", "M1_leg_as_hyp" });
            return s;
        }

        Sheet MakeLenLegGiven(int stage, int[] t, int rot, int given, int flavor)
        {
            var s = MakeLenLeg(stage, t, rot);
            s.flavor = flavor;
            s.givenLeg = given;
            if (given == 0) { s.hide = 2; s.targetSide = 1; s.targetArea = s.b * s.b; s.asked = s.b; }
            else { s.hide = 1; s.targetSide = 0; s.targetArea = s.a * s.a; s.asked = s.a; }
            int g = given == 0 ? s.a : s.b;
            FillDock(s, new[] { s.targetArea, s.c - g, s.c * s.c },
                new[] { "", "M2_sum_not_sq", "M1_leg_as_hyp" });
            return s;
        }

        Sheet MakeLenLeg(int stage, int[] t, int rot)
        {
            var s = Base(stage, t, rot, Kind.LenLeg);
            s.givenLeg = rng.Next(2);
            if (s.givenLeg == 0) { s.hide = 2; s.targetSide = 1; s.targetArea = s.b * s.b; s.asked = s.b; }
            else { s.hide = 1; s.targetSide = 0; s.targetArea = s.a * s.a; s.asked = s.a; }
            s.askedUnit = "cm";
            s.misconceptionId = "M1_leg_as_hyp";
            int given = s.givenLeg == 0 ? s.a : s.b;
            FillDock(s, new[] { s.targetArea, s.c - given, s.c * s.c },
                new[] { "", "M2_sum_not_sq", "M1_leg_as_hyp" });
            return s;
        }

        Sheet MakeRev(int stage, bool ok, int[] t, int rot)
        {
            var s = Base(stage, t, rot, Kind.Reverse);
            s.showRight = false;
            s.ghostOn = false;
            s.hide = 0;
            s.reverseTrue = ok;
            if (ok)
            {
                s.a = t[0]; s.b = t[1]; s.c = t[2];
                if (s.a > s.b) { int tmp = s.a; s.a = s.b; s.b = tmp; }
                s.rightVertex = 2;
                s.asked = 1;
            }
            else
            {
                s.a = t[0]; s.b = t[1]; s.c = t[2];
                int max = Judge.MaxI(s.a, s.b, s.c);
                if (max == 0) { int tmp = s.a; s.a = s.c; s.c = tmp; }
                else if (max == 1) { int tmp = s.b; s.b = s.c; s.c = tmp; }
                s.rightVertex = 2;
                s.asked = 0;
            }
            s.askedUnit = "";
            s.targetSide = -1;
            s.targetArea = 0;
            s.misconceptionId = ok ? "M4_not_max_c" : "M3_apply_not_right";
            s.sides = new[] { s.a, s.b, s.c };
            Shuffle(s.sides);
            s.dock = new[] { s.a * s.a, s.b * s.b, s.c * s.c };
            s.dockMis = new[] { "M4_not_max_c", "M4_not_max_c", "" };
            return s;
        }

        void FillDock(Sheet s, int[] raw, string[] mis)
        {
            var areas = new List<int>();
            var ids = new List<string>();
            for (int i = 0; i < raw.Length; i++)
            {
                int v = raw[i];
                if (v <= 0) continue;
                if (s.kind != Kind.AreaHyp && v == s.asked && s.askedUnit == "cm") continue;
                bool dup = false;
                for (int j = 0; j < areas.Count; j++) if (areas[j] == v) dup = true;
                if (dup) continue;
                areas.Add(v);
                ids.Add(i < mis.Length ? mis[i] : "");
            }
            int extra = 2;
            while (areas.Count < 3)
            {
                int cand = s.a + s.b + extra;
                extra++;
                if (cand <= 0 || cand == s.targetArea) continue;
                bool dup = false;
                for (int j = 0; j < areas.Count; j++) if (areas[j] == cand) dup = true;
                if (dup) continue;
                areas.Add(cand);
                ids.Add("M2_sum_not_sq");
            }
            var idx = new[] { 0, 1, 2 };
            Shuffle(idx);
            s.dock = new[] { areas[idx[0]], areas[idx[1]], areas[idx[2]] };
            s.dockMis = new[] { ids[idx[0]], ids[idx[1]], ids[idx[2]] };
        }

        void Shuffle(int[] a)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = a[i]; a[i] = a[j]; a[j] = t;
            }
        }
    }
}
