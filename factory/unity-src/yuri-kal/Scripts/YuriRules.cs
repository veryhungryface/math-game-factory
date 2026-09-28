// 유리칼 — 순수 규칙(생성기·판정·문구). UnityEngine 에 기대지 않는다 → 봇 자가 테스트(Editor/YuriBotSelfTest.cs)가
// 게임과 똑같은 코드를 그대로 돌린다.
//
// ── 기하 모델 (전부 정수 mm · 정수 도)
//   두 대각선 막대 AC(길이 L1) · BD(길이 L2) 가 점 O 에서 만난다. u1 = A→C 방향, u2 = B→D 방향, θ = ∠AOB (u1·u2 사이각).
//   학생이 옮기는 것은 점 O 이고, O 의 자리는 두 막대 위의 거리 s1 = AO, s2 = BO (mm, 5 mm 격자) 로만 기록된다.
//   CO = L1 − s1, DO = L2 − s2. 막대는 서로의 방향으로만 미끄러지므로 O 는 손가락을 그대로 따라가고(사선 격자),
//   AO·CO·BO·DO 는 항상 정수 mm 다. 코드는 O 를 중점으로 스냅하지 않는다 — 판정만 한다.
//
// ── 판정 (Judge)
//   이등분  : |AO − CO| ≤ 2 mm 이고 |BO − DO| ≤ 2 mm  (격자가 5 mm 라 사실상 정확히 같을 때만)
//   수직    : θ == 90  (θ 는 5° 격자 정수)
//   길이 같음: L1 == L2 (생성 단계의 정수 mm)
//   평행사변형 ⇐ 이등분 / 직사각형 ⇐ 이등분 ∧ 길이 같음 / 마름모 ⇐ 이등분 ∧ 수직 / 정사각형 ⇐ 셋 다
//   → **포함 관계(위계 분류)**: 정사각형은 직사각형 주문·마름모 주문·평행사변형 주문을 모두 만족한다.
//   쪼개기 정답 ⇔ 지금 모양이 주문을 만족 / 치우기 정답 ⇔ 이 장으로는 주문을 만들 수 없음.
//
// ── expression_traps (curriculum/2022-middle-math.json · 이 단원 오개념에서 옮김)
//   · 발문 종결은 「~하시오」. 초등 「~해 보세요」 금지. 기호 □ABCD, △ABO, ∠AOB, AB∥DC, AC⊥BD. 수와 단위는 띄운다(12 cm, 48 cm²).
//   · 중2 에는 √ 가 없다 → 길이는 전부 막대 위 정수 mm 이고, 넓이는 sin θ 가 유리수인 θ(30°·90°·150°)에서만 숫자로 보인다.
//   · 「한 쌍의 대변이 평행하고 그 길이가 같다」의 「그」 — 등변사다리꼴(AB∥DC, AD = BC)이 반례(M3).
//   · 정사각형은 직사각형이다 · 마름모는 평행사변형이다 → 참으로 채점(M1).
//   · 평행사변형의 두 대각선은 길이가 같지 않아도 된다(M2). 대각선이 수직이기만 하면 마름모가 아니다(M4).
//   · 「가정」「결론」 금지어. 폰트 서브셋에 없는 「—」「▼」는 쓰지 않는다.
using System;
using System.Collections.Generic;
using System.Text;

namespace Mgf.YuriKal
{
    public enum Order { Para, Rect, Rhom, Square }
    public enum Act { Split, Scrap }

    /// <summary>잠긴 장(판정 라운드)의 종류. None = 학생이 O 를 옮기는 구성 장.</summary>
    public enum Lock
    {
        None,
        CondG,        // ㄱ: AB∥DC, AB = DC           → 평행사변형 (쪼개기)
        Trapezoid,    // ㄴ형: AB∥DC, AD = BC (등변사다리꼴) → 평행사변형 아님 (치우기, M3)
        CondD,        // ㄷ: ∠A = ∠C, ∠B = ∠D         → 평행사변형 (쪼개기)
        CondR,        // ㄹ: AO = CO, BO = DO          → 평행사변형 (쪼개기)
        Kite,         // AC⊥BD, AO = CO, BO ≠ DO (연 모양) → 마름모 아님 (치우기, M4)
        Square,       // 정사각형 잠금 — 마름모/직사각형 주문에서 쪼개기 (M1)
        Rhombus,      // 마름모 잠금 — 평행사변형 주문에서 쪼개기 (M1)
        ParaUneq      // 평행사변형 잠금(AC ≠ BD) — 직사각형 주문에서 치우기 (M2)
    }

    /// <summary>장 한 장(주문 + 막대 + 현재 O). 학생 조작은 s1·s2·theta 만 바꾼다.</summary>
    public class Sheet
    {
        public int no;              // 0 = 연습, 1..12
        public Order order;
        public Lock lk;
        public int L1, L2;          // AC, BD (mm)
        public int s1, s2;          // AO, BO (mm) — 현재
        public int theta;           // ∠AOB (도)
        public int phi;             // 화면에서 AC 막대의 방향(도, 15° 단위) — 고정 방향 연타 방지
        public bool canRotate;      // 마름모·정사각형 주문: 막대 끝 고리로 교각을 돌릴 수 있다
        public bool areaOrder;      // 「넓이가 ○ cm²인 평행사변형이 되게 하시오」
        public int stage;           // 1 = 길이 숫자 보임 · 2 = 숫자 걷힘(중점 턱·눈금·캘리퍼만)
        public int limitSec;        // 장당 제한 시간
        public string misconceptionId = "";   // 이 장이 겨누는 오개념(M1~M4). 오답 행동이 곧 그 오개념이다
        public int startS1, startS2, startTheta;

        public bool Locked => lk != Lock.None;
        public int CO => L1 - s1;
        public int DO => L2 - s2;
        public Sheet Clone() => (Sheet)MemberwiseClone();
        public void Reset() { s1 = startS1; s2 = startS2; theta = startTheta; }
    }

    public static class Judge
    {
        public const int Grid = 5;          // O 격자(mm)
        public const int Margin = 10;       // O 는 막대 끝에서 1 cm 안쪽까지만
        public const int Tol = 2;           // 이등분 허용(mm)
        public const int RotStep = 5, RotMin = 20, RotMax = 160;

        public static bool Bisect(Sheet s) => Math.Abs(s.s1 - s.CO) <= Tol && Math.Abs(s.s2 - s.DO) <= Tol;
        public static bool Perp(Sheet s) => s.theta == 90;
        public static bool EqDiag(Sheet s) => s.L1 == s.L2;

        static bool NeedEq(Order o) => o == Order.Rect || o == Order.Square;
        static bool NeedPerp(Order o) => o == Order.Rhom || o == Order.Square;

        /// <summary>지금 모양이 주문을 만족하는가(포함 관계로 분류).</summary>
        public static bool Satisfies(Sheet s)
        {
            if (!Bisect(s)) return false;
            if (NeedEq(s.order) && !EqDiag(s)) return false;
            if (NeedPerp(s.order) && !Perp(s)) return false;
            return true;
        }

        /// <summary>이 장으로 주문을 만들 수 있는가. 잠긴 장은 지금 모양 그대로.</summary>
        public static bool Possible(Sheet s)
        {
            if (s.Locked) return Satisfies(s);
            if (NeedEq(s.order) && !EqDiag(s)) return false;
            if (NeedPerp(s.order) && !(s.canRotate || Perp(s))) return false;
            return true;
        }

        public static bool Correct(Sheet s, Act a) => a == Act.Split ? Satisfies(s) : !Possible(s);

        /// <summary>정답 행동(봇·훅·문제 은행 공용).</summary>
        public static Act RightAct(Sheet s) => Possible(s) ? Act.Split : Act.Scrap;

        /// <summary>정답 상태로 O·교각을 옮긴다(TestAnswerCorrect·오답 뒤 「정답 상태 보여 주기」용 — 학생 입력 경로가 아니다).</summary>
        public static void SolveInto(Sheet s)
        {
            if (s.Locked || !Possible(s)) return;
            s.s1 = s.L1 / 2; s.s2 = s.L2 / 2;
            if (NeedPerp(s.order)) s.theta = 90;
        }

        /// <summary>O 를 사선 격자에 맞춰 넣는다(스냅은 격자에만 — 중점으로 끌어당기지 않는다).</summary>
        public static int SnapS(double mm, int L)
        {
            int v = (int)Math.Round(mm / Grid) * Grid;
            return Math.Max(Margin, Math.Min(L - Margin, v));
        }

        public static int SnapTheta(double deg)
        {
            int v = (int)Math.Round(deg / RotStep) * RotStep;
            return Math.Max(RotMin, Math.Min(RotMax, v));
        }

        /// <summary>sin θ 가 유리수인 교각(30·90·150)에서만 넓이를 숫자로 보인다. 반환: sin θ = num/2, 아니면 0.</summary>
        public static int SinHalfNum(int theta) => theta == 90 ? 2 : (theta == 30 || theta == 150) ? 1 : 0;

        /// <summary>△ABO 넓이(= 네 조각 각각, 이등분일 때) — cm² 문자열. 유리수가 아니면 null.</summary>
        public static string TriAreaText(Sheet s)
        {
            int k = SinHalfNum(s.theta);
            if (k == 0) return null;
            // ½·AO·BO·sinθ = AO·BO·k/4 (mm²) → cm² 는 /100 → 분모 400 (항상 유한소수)
            return Words.Dec((long)s.s1 * s.s2 * k, 400);
        }

        /// <summary>전체 넓이(이등분일 때) = ½·AC·BD·sinθ → L1·L2·k/4 mm².</summary>
        public static string TotalAreaText(Sheet s)
        {
            int k = SinHalfNum(s.theta);
            if (k == 0) return null;
            return Words.Dec((long)s.L1 * s.L2 * k, 400);
        }
    }

    public static class Words
    {
        public static string Cm(int mm) => mm % 10 == 0 ? (mm / 10).ToString() : (mm / 10) + "." + (mm % 10);

        /// <summary>num/den 을 유한소수 문자열로(den 은 2·5 의 곱).</summary>
        public static string Dec(long num, long den)
        {
            long ip = num / den, rem = num % den;
            if (rem == 0) return ip.ToString();
            var sb = new StringBuilder(); sb.Append(ip).Append('.');
            for (int i = 0; i < 6 && rem != 0; i++) { rem *= 10; sb.Append(rem / den); rem %= den; }
            return sb.ToString();
        }

        public static string Name(Order o)
        {
            switch (o)
            {
                case Order.Rect: return "직사각형";
                case Order.Rhom: return "마름모";
                case Order.Square: return "정사각형";
                default: return "평행사변형";
            }
        }
        static string Ga(Order o) => o == Order.Rhom ? "가" : "이";

        /// <summary>작업 티켓 큰 줄(주문).</summary>
        public static string OrderLine(Sheet s)
        {
            if (s.areaOrder) return "넓이가 " + Judge.TotalAreaText(SolvedCopy(s)) + " cm²인 평행사변형이 되게 하시오.";
            if (s.Locked) return "□ABCD가 " + Name(s.order) + "이면 쪼개고, 아니면 치우시오.";
            return "□ABCD가 " + Name(s.order) + Ga(s.order) + " 되게 하시오.";
        }

        /// <summary>작업 티켓 둘째 줄(주어진 것).</summary>
        public static string DataLine(Sheet s)
        {
            switch (s.lk)
            {
                case Lock.CondG: return "AB∥DC, AB = DC";
                case Lock.Trapezoid: return "AB∥DC, AD = BC";
                case Lock.CondD: return "∠A = ∠C, ∠B = ∠D";
                case Lock.CondR: return "AO = CO, BO = DO";
                case Lock.Kite: return "AC⊥BD, AO = CO";
                case Lock.Square: return "AC = BD, AC⊥BD, AO = CO, BO = DO";
                case Lock.Rhombus: return "AC⊥BD, AO = CO, BO = DO";
                case Lock.ParaUneq: return "AO = CO, BO = DO, AC = " + Cm(s.L1) + " cm, BD = " + Cm(s.L2) + " cm";
            }
            return "AC = " + Cm(s.L1) + " cm, BD = " + Cm(s.L2) + " cm";
        }

        public static string FootLine(Sheet s) => s.Locked ? "↑ 쪼개기 · ↓ 치우기 · 점 O는 잠김" : "안 되면 아래로 치우시오 · ↑ 쪼개기 · ↓ 치우기";

        static Sheet SolvedCopy(Sheet s) { var c = s.Clone(); c.s1 = c.L1 / 2; c.s2 = c.L2 / 2; return c; }

        /// <summary>문제 은행 문장 = 화면 티켓 두 줄 + 시작 상태(같은 Sheet 에서 만든다).</summary>
        public static string Prompt(Sheet s)
        {
            var sb = new StringBuilder();
            sb.Append("두 대각선 AC, BD가 점 O에서 만난다. ").Append(DataLine(s)).Append(". ");
            if (!s.Locked)
                sb.Append("지금 AO = ").Append(Cm(s.startS1)).Append(" cm, BO = ").Append(Cm(s.startS2))
                  .Append(" cm, ∠AOB = ").Append(s.startTheta).Append("°이다. ");
            sb.Append(OrderLine(s));
            if (!s.Locked) sb.Append(" 안 되면 치우시오.");
            return sb.ToString();
        }

        public static string Answer(Sheet s)
        {
            if (Judge.RightAct(s) == Act.Scrap) return "치운다";
            if (s.Locked) return "쪼갠다";
            string a = "AO = " + Cm(s.L1 / 2) + " cm, BO = " + Cm(s.L2 / 2) + " cm";
            if (s.canRotate) a += ", ∠AOB = 90°";
            return a + "로 옮겨 쪼갠다";
        }

        public static string Concept(Sheet s)
        {
            switch (s.lk)
            {
                case Lock.CondG: case Lock.CondD: case Lock.CondR: case Lock.Trapezoid: return "평행사변형이 되는 조건";
                case Lock.Square: case Lock.Rhombus: return "여러 가지 사각형 사이의 관계";
            }
            if (s.areaOrder) return "평행사변형과 넓이";
            return s.order == Order.Para ? "평행사변형의 대각선" : "여러 가지 사각형의 대각선";
        }

        /// <summary>맞았을 때 드러내는 수학(1초 이상 화면에 남는다).</summary>
        public static string RevealRight(Sheet s, Act a)
        {
            if (a == Act.Scrap)
            {
                switch (s.lk)
                {
                    case Lock.Kite: return "AC⊥BD이지만 BO = " + Cm(s.s2) + " cm, DO = " + Cm(s.DO) + " cm\n수직이등분이 아니면 마름모가 아니다";
                    case Lock.Trapezoid: return "평행한 쌍은 AB, DC · 길이가 같은 쌍은 AD, BC\n「그」 쌍이 아니다 → 등변사다리꼴";
                }
                return "AC = " + Cm(s.L1) + " cm, BD = " + Cm(s.L2) + " cm\n" + Name(s.order) + "의 두 대각선은 길이가 같다";
            }
            switch (s.lk)
            {
                case Lock.CondG: return "한 쌍의 대변이 평행하고 그 길이가 같다\n→ 평행사변형";
                case Lock.CondD: return "두 쌍의 대각의 크기가 각각 같다\n→ 평행사변형";
                case Lock.CondR: return "두 대각선이 서로 다른 것을 이등분한다\n→ 평행사변형";
                case Lock.Square: return s.order == Order.Rect ? "정사각형은 직사각형이다" : "정사각형은 마름모이다";
                case Lock.Rhombus: return "마름모는 평행사변형이다";
            }
            string l = "AO = CO = " + Cm(s.s1) + " cm · BO = DO = " + Cm(s.s2) + " cm";
            if (s.areaOrder) return l + "\n△ABO = " + Judge.TriAreaText(s) + " cm² = 전체의 1/4";
            switch (s.order)
            {
                case Order.Rect: return l + "\nAC = BD · " + (s.theta == 90 ? "정사각형도 직사각형이다" : "이등분 + 길이 같음 → 직사각형");
                case Order.Rhom: return l + "\nAC⊥BD · 수직이등분 → 마름모";
                case Order.Square: return l + "\nAC = BD · AC⊥BD → 정사각형";
            }
            return l + "\n두 대각선이 서로 다른 것을 이등분한다";
        }

        /// <summary>틀렸을 때: 무엇이 왜 안 맞았는지(색이 아니라 문장+값으로).</summary>
        public static string RevealWrong(Sheet s, Act a)
        {
            if (a == Act.Split)
            {
                switch (s.lk)
                {
                    case Lock.Kite: return "BO = " + Cm(s.s2) + " cm, DO = " + Cm(s.DO) + " cm · 이등분이 아니다\n수직이기만 하면 마름모가 아니다";
                    case Lock.Trapezoid: return "AD = BC는 평행한 쌍이 아니다\n등변사다리꼴 · 평행사변형이 아니다";
                    case Lock.ParaUneq: return "AC = " + Cm(s.L1) + " cm ≠ BD = " + Cm(s.L2) + " cm\n평행사변형이지만 직사각형은 아니다";
                }
                if (!Judge.Bisect(s))
                    return "AO = " + Cm(s.s1) + " cm, CO = " + Cm(s.CO) + " cm · BO = " + Cm(s.s2) + " cm, DO = " + Cm(s.DO) + " cm\n아직 이등분이 아니다";
                if ((s.order == Order.Rect || s.order == Order.Square) && s.L1 != s.L2)
                    return "AC = " + Cm(s.L1) + " cm ≠ BD = " + Cm(s.L2) + " cm\n" + Name(s.order) + "의 두 대각선은 길이가 같다 · 치웠어야 한다";
                return "∠AOB = " + s.theta + "° · 수직이 아니다\n" + Name(s.order) + "의 대각선은 서로 수직이다";
            }
            switch (s.lk)
            {
                case Lock.CondG: return "한 쌍의 대변이 평행하고 그 길이가 같다\n평행사변형이니 쪼갰어야 한다";
                case Lock.CondD: return "두 쌍의 대각의 크기가 각각 같다\n평행사변형이니 쪼갰어야 한다";
                case Lock.CondR: return "두 대각선이 서로 다른 것을 이등분한다\n평행사변형이니 쪼갰어야 한다";
                case Lock.Square: return s.order == Order.Rect ? "정사각형도 직사각형이다" : "정사각형도 마름모이다";
                case Lock.Rhombus: return "마름모도 평행사변형이다";
            }
            string t = "이 장은 될 수 있었다 · O를 두 중점 턱이 겹치는 곳으로";
            if (s.canRotate) t += "\n그리고 ∠AOB = 90°";
            else if (s.order == Order.Rect && s.theta == 90) t += "\n정사각형도 직사각형이다";
            return t;
        }
    }

    /// <summary>문제 생성 — 제약 만족 풀에서 샘플링(사후 필터 대신 가능한 값 목록을 먼저 만들고 뽑는다).</summary>
    public class SheetGen
    {
        static readonly int[][] Uneq = { new[] { 80, 120 }, new[] { 100, 140 }, new[] { 120, 160 }, new[] { 100, 160 }, new[] { 80, 140 }, new[] { 120, 140 }, new[] { 140, 100 }, new[] { 160, 120 }, new[] { 120, 80 }, new[] { 140, 80 } };
        static readonly int[] EqL = { 100, 120, 140 };
        static readonly int[] ThAny = { 30, 45, 60, 75, 105, 120, 135, 150 };
        static readonly int[] ThRot = { 60, 65, 70, 110, 115, 120 };
        // 넓이 주문: ½·AC·BD·sinθ 가 정수 cm² 가 되는 (AC, BD, θ)
        static readonly int[][] AreaSets = { new[] { 120, 160, 30 }, new[] { 100, 120, 90 }, new[] { 80, 120, 150 }, new[] { 120, 140, 30 }, new[] { 100, 160, 150 }, new[] { 80, 140, 90 }, new[] { 120, 120, 30 } };

        readonly Random r;
        public SheetGen(Random rng) { r = rng; }

        T Pick<T>(T[] a) => a[r.Next(a.Length)];
        int Phi() => r.Next(12) * 15;

        /// <summary>연습 장(고정값 — 안내 문구와 화면이 어긋나지 않게).</summary>
        public static Sheet Practice()
        {
            var s = new Sheet { no = 0, order = Order.Para, L1 = 80, L2 = 120, theta = 150, phi = 15, stage = 1, limitSec = 0 };
            s.startS1 = 20; s.startS2 = 60; s.startTheta = 150; s.Reset();
            return s;
        }

        /// <summary>시작 O: 목표(이등분점)에서 20~45 mm 떨어진 격자점 전체를 먼저 나열하고 하나를 뽑는다.</summary>
        void PlaceStart(Sheet s)
        {
            var pool = new List<int>();
            double c = Math.Cos(s.theta * Math.PI / 180.0);
            for (int a = Judge.Margin; a <= s.L1 - Judge.Margin; a += Judge.Grid)
            for (int b = Judge.Margin; b <= s.L2 - Judge.Margin; b += Judge.Grid)
            {
                double d1 = a - s.L1 / 2, d2 = b - s.L2 / 2;
                double d = Math.Sqrt(d1 * d1 + d2 * d2 + 2 * d1 * d2 * c);   // 생성 제약(거리)에만 실수 — 판정에는 안 쓴다
                if (d < 20 || d > 45) continue;
                if (Math.Abs(d1) < 5 && Math.Abs(d2) < 5) continue;
                pool.Add(a * 1000 + b);
            }
            int v = pool[r.Next(pool.Count)];
            s.startS1 = v / 1000; s.startS2 = v % 1000;
        }

        Sheet Build(int no, Order o, int L1, int L2, int theta, bool rot)
        {
            var s = new Sheet { no = no, order = o, L1 = L1, L2 = L2, theta = theta, phi = Phi(), canRotate = rot };
            s.startTheta = theta;
            PlaceStart(s);
            s.Reset();
            return s;
        }

        Sheet Locked(int no, Order o, Lock lk, int L1, int L2, int a, int b, int theta)
        {
            var s = new Sheet { no = no, order = o, lk = lk, L1 = L1, L2 = L2, theta = theta, phi = Phi() };
            s.startS1 = a; s.startS2 = b; s.startTheta = theta; s.Reset();
            return s;
        }

        public Sheet Para(int no)
        {
            var p = Pick(Uneq);
            return Build(no, Order.Para, p[0], p[1], Pick(ThAny), false);
        }

        public Sheet AreaSheet(int no)
        {
            var a = Pick(AreaSets);
            var s = Build(no, Order.Para, a[0], a[1], a[2], false);
            s.areaOrder = true;
            return s;
        }

        /// <summary>직사각형 구성(길이 같음). 절반은 교각 90° = 정사각형 → 포함 관계(M1)로 쪼개기가 정답.</summary>
        public Sheet RectEq(int no)
        {
            int L = Pick(EqL);
            bool sq = r.Next(2) == 0;
            var s = Build(no, Order.Rect, L, L, sq ? 90 : Pick(new[] { 45, 60, 120, 135 }), false);
            if (sq) s.misconceptionId = "M1";
            return s;
        }

        /// <summary>치우기 정답 ①: 직사각형 주문 + 길이가 다른 대각선(구성 장 또는 잠긴 평행사변형) — M2.</summary>
        public Sheet Scrap1(int no)
        {
            var p = Pick(Uneq);
            Sheet s = r.Next(3) == 0
                ? Locked(no, Order.Rect, Lock.ParaUneq, p[0], p[1], p[0] / 2, p[1] / 2, Pick(ThAny))
                : Build(no, Order.Rect, p[0], p[1], Pick(ThAny), false);
            s.misconceptionId = "M2";
            return s;
        }

        public Sheet Rhom(int no)
        {
            var p = Pick(Uneq);
            return Build(no, Order.Rhom, p[0], p[1], Pick(ThRot), true);
        }

        public Sheet SquareBuild(int no)
        {
            int L = Pick(EqL);
            return Build(no, Order.Square, L, L, Pick(ThRot), true);
        }

        /// <summary>치우기 정답 ②: 연 모양(M4) · 등변사다리꼴(M3) · 정사각형 주문 + 길이 다름(M2).</summary>
        public Sheet Scrap2(int no)
        {
            int k = r.Next(3);
            if (k == 0)
            {
                int L1 = Pick(new[] { 100, 120, 140 }), L2 = Pick(new[] { 120, 150 });   // BO = BD/3
                var s = Locked(no, Order.Rhom, Lock.Kite, L1, L2, L1 / 2, L2 / 3, 90);
                s.misconceptionId = "M4";
                return s;
            }
            if (k == 1)
            {
                int L = Pick(new[] { 120, 150 });                                           // AO = BO = L/3 → AB∥DC, AD = BC
                var s = Locked(no, Order.Para, Lock.Trapezoid, L, L, L / 3, L / 3, Pick(new[] { 60, 75, 90, 105, 120 }));
                s.misconceptionId = "M3";
                return s;
            }
            var p = Pick(Uneq);
            var q = Build(no, Order.Square, p[0], p[1], Pick(ThRot), true);
            q.misconceptionId = "M2";
            return q;
        }

        /// <summary>잠긴 장 쪼개기 정답 ①: 마름모 주문 + 정사각형(M1) · ㄱ 조건 · 평행사변형 주문 + 마름모(M1).</summary>
        public Sheet LockSplitA(int no)
        {
            int k = r.Next(3);
            if (k == 0) { int L = Pick(EqL); var s = Locked(no, Order.Rhom, Lock.Square, L, L, L / 2, L / 2, 90); s.misconceptionId = "M1"; return s; }
            var p = Pick(Uneq);
            if (k == 1) { var s = Locked(no, Order.Para, Lock.CondG, p[0], p[1], p[0] / 2, p[1] / 2, Pick(ThAny)); s.misconceptionId = "M3"; return s; }
            var q = Locked(no, Order.Para, Lock.Rhombus, p[0], p[1], p[0] / 2, p[1] / 2, 90); q.misconceptionId = "M1"; return q;
        }

        /// <summary>잠긴 장 쪼개기 정답 ②: ㄷ 조건 · ㄹ 조건 · 직사각형 주문 + 정사각형(M1).</summary>
        public Sheet LockSplitB(int no)
        {
            int k = r.Next(3);
            if (k == 2) { int L = Pick(EqL); var s = Locked(no, Order.Rect, Lock.Square, L, L, L / 2, L / 2, 90); s.misconceptionId = "M1"; return s; }
            var p = Pick(Uneq);
            return Locked(no, Order.Para, k == 0 ? Lock.CondD : Lock.CondR, p[0], p[1], p[0] / 2, p[1] / 2, Pick(ThAny));
        }

        /// <summary>한 판 12장. 1~4 평행사변형(숫자 보임) · 5 다리(숫자 걷힘) · 6 직사각형 · 7~12 섞음.
        /// 치우기 정답 2장 = 잠긴 쪼개기 정답 2장 → 「항상 위」·「항상 아래」 연타가 둘 다 우연 수준(판정 장 4장의 50%)에 묶인다.</summary>
        public List<Sheet> Deck()
        {
            var d = new List<Sheet> { Para(1), Para(2), Para(3), AreaOrPara(4), Para(5), RectEq(6) };
            var tail = new List<Sheet> { Scrap1(0), Rhom(0), LockSplitA(0), Scrap2(0), LockSplitB(0), r.Next(2) == 0 ? SquareBuild(0) : AreaSheet(0) };
            for (int i = tail.Count - 1; i > 0; i--) { int j = r.Next(i + 1); var t = tail[i]; tail[i] = tail[j]; tail[j] = t; }
            d.AddRange(tail);
            for (int i = 0; i < d.Count; i++)
            {
                var s = d[i];
                s.no = i + 1;
                s.stage = s.no <= 4 ? 1 : 2;
                s.limitSec = s.no <= 4 ? 14 : s.no <= 8 ? 12 : 10;
                if (s.canRotate) s.limitSec += 3;
            }
            return d;
        }

        Sheet AreaOrPara(int no)
        {
            // 4번째 장: 교각 30°/150° 평행사변형 → 쪼갠 뒤 네 조각에 같은 넓이 숫자가 찍힌다(넓이 맛보기)
            var p = Pick(new[] { new[] { 80, 120 }, new[] { 120, 160 }, new[] { 100, 160 } });
            return Build(no, Order.Para, p[0], p[1], Pick(new[] { 30, 150 }), false);
        }
    }

    /// <summary>한 판 규칙(시간·예비 장·콤보). 게임과 봇이 함께 쓴다.</summary>
    public static class Rules
    {
        public const float RunSec = 90f;
        public const int Spares = 3;
        public const float OSpeed = 36f;        // O 이동 속도 상한 cm/s (16 cm 매트 폭 ≈ 0.45초)
        public const float RotSpeed = 240f;     // 교각 회전 속도 상한 °/s
        public static int Mult(int combo) => combo >= 5 ? 3 : combo >= 3 ? 2 : 1;
    }
}
