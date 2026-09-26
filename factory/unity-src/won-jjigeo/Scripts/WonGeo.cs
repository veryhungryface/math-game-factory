// 원 찍어 v2 — 모눈(격자) 기하 · 문제 생성 · 정수 판정 · 길이 표시 (UnityEngine 의존 없음: 봇 자가 테스트도 이 코드를 그대로 쓴다)
//
// 핵심 원칙(DESIGN-v2.md): 학생이 쓰는 정보 = 정의·성질 그 자체. 판정은 그 정보가 가리키는 점과 정확히 같은지(정수 연산)로만.
//  - 모든 꼭짓점과 정답(외심 O·내심 I)은 **격자점**(정수 좌표)이다. 핀은 격자점(2단계에서는 주름 교점)에만 선다.
//  - 거리 비교는 거리²(정수). 변까지의 거리 비교는 (a·x+b·y−c)²·(a'²+b'²) 교차곱(정수). float 판정 없음.
//  - 화면 길이 표시는 모눈 칸 단위 소수 둘째 자리(정수 연산으로 반올림). 「표시가 같다 ⇔ 실제로 같다」는
//    WonBotSelfTest 가 격자 전체 × 문제 풀 전체로 전수 확인한다(불일치 0건).
//      · 꼭짓점까지: 거리² 은 정수 n ≤ 16²+14² = 452. √(n+1)−√n > 0.01 (n < 2500) 이므로 둘째 자리 표시는 단사.
//      · 변까지(내심 문항): 변의 법선이 피타고라스 방향(1,0)·(3,4)·(5,12)·(8,15)·(7,24) 이라 거리 = 정수/h (유리수, 정수 반올림).
//        배치마다 격자 전체에서 표시·정수 판정 일치를 확인하고(DisplayConsistent) 통과한 배치만 쓴다.
//
// 표현 함정(2022-middle-math.json expression_traps / style_guide):
//  - 종결형은 「~을 찾으시오 / 구하시오 / 말하시오」. 초등 「~해 보세요」·탐구형 「~해 보자」를 발문으로 쓰지 않는다.
//  - 용어는 교과서 그대로: 외심·내심·수직이등분선·각의 이등분선·빗변. △ABC, ∠A 표기.
//  - 중2 에는 √ 가 없다 → 화면·발문·정답에 제곱근 기호를 만들지 않는다. 길이가 무리수이면 반올림한 소수로만 보인다.
//  - 좌표 괄호 표기 A(−3, 2) 대신 「점 A에서 오른쪽으로 7칸, 아래로 1칸」(모눈 칸) — 게임 화면의 모눈과 같은 말.
//  - 무게중심은 이 단원(m2s2-u1) 범위가 아니라 다루지 않는다.
//  - v1 의 ∠BOC(둔각이면 360°−2∠A) 수치는 은행에서 뺐다.
//
// 오개념 역산(검산관용 misconceptionId) — 이 게임의 오답은 선택지가 아니라 핀을 박은 자리·접은 주름의 종류다.
//   M1 swap   : 외심 미션에서 변끼리 포개어 접기(각의 이등분선) / 내심 미션에서 꼭짓점끼리 포개어 접기(수직이등분선)
//               → 2단계 생성기는 「반대 종류 주름이 정답을 지나는」 삼각형(이등변)을 풀에서 뺀다(PassesStage2).
//   M2 inside : 둔각삼각형의 외심을 삼각형 안에서 찾음 — 둔각 명판의 외심은 모눈 위 삼각형 바깥 격자점.
//   M3 right  : 직각삼각형의 외심을 삼각형 안쪽에서 찾음 — 정답은 빗변의 중점(격자점).
using System;
using System.Collections.Generic;

namespace Mgf.WonJjigeo
{
    /// <summary>격자점(모눈 교점). 단위 = 모눈 한 칸.</summary>
    public struct IP
    {
        public int x, y;
        public IP(int x, int y) { this.x = x; this.y = y; }
        public static IP operator +(IP a, IP b) => new IP(a.x + b.x, a.y + b.y);
        public static IP operator -(IP a, IP b) => new IP(a.x - b.x, a.y - b.y);
        public long Len2 => (long)x * x + (long)y * y;
        public bool Same(IP o) => x == o.x && y == o.y;
        public override string ToString() => $"({x},{y})";
    }

    /// <summary>핀이 설 수 있는 점: 유리수 점 (x/d, y/d) — 격자점은 d=1, 2단계 주름 교점은 d>1.
    /// exact=false 는 무리수 점(외심 문항에서 변끼리 접은 각의 이등분선이 낀 교점)이며 fx, fy 만 유효하다 — 격자점 정답과 같을 수 없다.</summary>
    public struct RP
    {
        public long x, y, d; public bool exact; public double fx, fy;
        public static RP Of(IP p) => new RP { x = p.x, y = p.y, d = 1, exact = true, fx = p.x, fy = p.y };
        public static RP Rat(long x, long y, long d)
        {
            if (d < 0) { x = -x; y = -y; d = -d; }
            long g = Geo.Gcd(Geo.Gcd(Math.Abs(x), Math.Abs(y)), d);
            if (g > 1) { x /= g; y /= g; d /= g; }
            return new RP { x = x, y = y, d = d, exact = true, fx = (double)x / d, fy = (double)y / d };
        }
        public static RP Approx(double fx, double fy) => new RP { exact = false, fx = fx, fy = fy, d = 1 };
        public bool Is(IP p) => exact && x == (long)p.x * d && y == (long)p.y * d;
        public bool IsLattice => exact && d == 1;
        public IP Lattice => new IP((int)x, (int)y);
        public bool SameAs(RP o) => exact && o.exact ? x * o.d == o.x * d && y * o.d == o.y * d : Math.Abs(fx - o.fx) < 1e-9 && Math.Abs(fy - o.fy) < 1e-9;
    }

    /// <summary>직선 a·x + b·y = c. exact=true 면 정수 계수, 아니면 fa·x + fb·y = fc (무리수 방향).</summary>
    public struct Line
    {
        public long a, b, c; public bool exact; public double fa, fb, fc;
        public static Line Int(long a, long b, long c) => new Line { a = a, b = b, c = c, exact = true, fa = a, fb = b, fc = c };
        public static Line Dbl(double a, double b, double c) => new Line { exact = false, fa = a, fb = b, fc = c };
        public bool Through(RP p) => exact && p.exact ? a * p.x + b * p.y == c * p.d : Math.Abs(fa * p.fx + fb * p.fy - fc) < 1e-7 * (Math.Abs(fa) + Math.Abs(fb) + 1);
    }

    public enum Mission { Circum, In }
    public enum Kind { Acute, Right, Obtuse }

    /// <summary>게임 규칙 상수 — 게임과 봇 자가 테스트가 같은 값을 쓴다.</summary>
    public static class Rules
    {
        public const int GX = 8, GY = 7;                  // 격자: x ∈ [−8, 8], y ∈ [−7, 7] (17×15 = 255 점)
        public static readonly IP Start = new IP(0, -GY); // 핀 출발점(트레이 바로 위 모눈 아래 가운데)
        public const float StepRate = 6f;                 // 핀 이동 속도 상한: 초당 격자 6걸음(가로·세로·대각 한 칸이 한 걸음)
        public const float T1 = 16f;                      // 1단계 명판 제한 시간(초). 첫 명판(온보딩)은 무제한
        public const float T2 = 40f;                      // 2단계 명판 제한 시간(초)
        public const int Lives = 3;                       // 핀 3개
        public const int UnlockStreak = 2;                // 1단계 외심·내심 각각 연속 첫 시도 정답 2회 → 2단계 해금
        public const int Stage2Plates = 6;                // 2단계 명판 수 → 모두 끝나면 완주
        public const int MaxCreases = 3;                  // 명판 한 장에 낼 수 있는 주름 수
        public const float ClearAccuracy = 0.7f;          // 첫 시도 정답률이 이 미만이면 승리 연출 없음
        public static bool InGrid(IP p) => p.x >= -GX && p.x <= GX && p.y >= -GY && p.y <= GY;
    }

    public static class Geo
    {
        public static readonly string[] Names = { "A", "B", "C" };
        public static long Gcd(long a, long b) { a = Math.Abs(a); b = Math.Abs(b); while (b != 0) { long t = a % b; a = b; b = t; } return a; }

        public static long ISqrt(long n)
        {
            if (n <= 0) return 0;
            long r = (long)Math.Sqrt(n);
            while (r * r > n) r--;
            while ((r + 1) * (r + 1) <= n) r++;
            return r;
        }

        /// <summary>round(num/den), num ≥ 0, den > 0 (반올림은 0.5 에서 올림).</summary>
        public static long RoundDiv(long num, long den) => (2 * num + den) / (2 * den);

        /// <summary>round(100·√N / d) — √N/d 의 소수 둘째 자리 반올림을 100 배 한 정수. 정수 연산만.</summary>
        public static long HundredthsSqrt(long N, long d)
        {
            long q = ISqrt(10000 * N) / d;                   // floor(100√N/d) = floor(floor(√(10000N))/d)
            if ((2 * q + 1) * (2 * q + 1) * d * d <= 40000 * N) q++;   // 100√N/d ≥ q+½ 이면 올림
            return q;
        }

        /// <summary>100 배 정수 → "5.00".</summary>
        public static string Fmt(long h) => (h / 100).ToString() + "." + (h % 100).ToString("00");

        public static long Cross(IP a, IP b) => (long)a.x * b.y - (long)a.y * b.x;
        public static long Dot(IP a, IP b) => (long)a.x * b.x + (long)a.y * b.y;
    }

    /// <summary>문항 1개(명판 1장). 게임 화면·판정·문제 은행이 모두 이 모델에서 나온다.</summary>
    public class Prob
    {
        public readonly IP[] V = new IP[3];
        public Mission m; public Kind kind; public bool iso; public int stage;
        public IP ans;
        public long R2;                   // 외심 문항: 외접원 반지름² (정수)
        public int inR;                   // 내심 문항: 내접원의 반지름(칸, 정수)
        public bool tutorial;             // 첫 명판·2단계 첫 외심/내심 명판: 안내 + 무감점 재시도
        public int rightV = -1, obtuseV = -1;
        // 변 i = V[i]→V[i+1] 을 품은 직선 a·x+b·y=c, N=a²+b², sq=√N (정수이면 >0, 아니면 0)
        public readonly long[] sa = new long[3], sb = new long[3], sc = new long[3], sN = new long[3], sq = new long[3];

        public char Letter => m == Mission.Circum ? 'O' : 'I';
        public string Center => m == Mission.Circum ? "외심" : "내심";

        public void Prepare()
        {
            for (int i = 0; i < 3; i++)
            {
                var p = V[i]; var q = V[(i + 1) % 3];
                sa[i] = q.y - p.y; sb[i] = p.x - q.x; sc[i] = sa[i] * p.x + sb[i] * p.y;
                sN[i] = sa[i] * sa[i] + sb[i] * sb[i];
                long r = Geo.ISqrt(sN[i]); sq[i] = r * r == sN[i] ? r : 0;
            }
            for (int i = 0; i < 3; i++)
            {
                long dot = Geo.Dot(V[(i + 1) % 3] - V[i], V[(i + 2) % 3] - V[i]);
                if (dot == 0) rightV = i; else if (dot < 0) obtuseV = i;
            }
            kind = rightV >= 0 ? Kind.Right : obtuseV >= 0 ? Kind.Obtuse : Kind.Acute;
            long s0 = (V[1] - V[0]).Len2, s1 = (V[2] - V[1]).Len2, s2 = (V[0] - V[2]).Len2;
            iso = s0 == s1 || s1 == s2 || s0 == s2;
        }

        // ── 측정(정수): 핀 → 꼭짓점 거리² 분자(같은 d 공유), 핀 → 변 직선의 부호 있는 값 L=a·x+b·y−c·d
        public long VertN(RP p, int i) { long dx = p.x - (long)V[i].x * p.d, dy = p.y - (long)V[i].y * p.d; return dx * dx + dy * dy; }
        public long SideL(RP p, int i) => sa[i] * p.x + sb[i] * p.y - sc[i] * p.d;

        /// <summary>두 거리가 정확히 같은가(정수). 외심 미션=꼭짓점까지, 내심 미션=변까지.</summary>
        public bool EqualPair(RP p, int i, int j, Mission mode)
        {
            if (!p.exact) return Math.Abs(DistD(p, i, mode) - DistD(p, j, mode)) < 1e-12;
            if (mode == Mission.Circum) return VertN(p, i) == VertN(p, j);
            long li = SideL(p, i), lj = SideL(p, j);
            return li * li * sN[j] == lj * lj * sN[i];          // (a·x+b·y−c)²·(a'²+b'²) 교차곱
        }

        public bool AllEqual(RP p, Mission mode) => EqualPair(p, 0, 1, mode) && EqualPair(p, 1, 2, mode);

        /// <summary>화면 표시용 길이(100 배 정수). 판정에는 쓰지 않는다.</summary>
        public long DistH(RP p, int i, Mission mode)
        {
            if (!p.exact) return (long)Math.Round(DistD(p, i, mode) * 100);
            if (mode == Mission.Circum) return Geo.HundredthsSqrt(VertN(p, i), p.d);
            long l = Math.Abs(SideL(p, i));
            if (sq[i] > 0) return Geo.RoundDiv(100 * l, p.d * sq[i]);
            return (long)Math.Round(100.0 * l / (p.d * Math.Sqrt(sN[i])));   // 변 길이가 무리수인 외심 문항의 변 거리(오답 연출에만)
        }

        public double DistD(RP p, int i, Mission mode)
        {
            if (mode == Mission.Circum) { double dx = p.fx - V[i].x, dy = p.fy - V[i].y; return Math.Sqrt(dx * dx + dy * dy); }
            return Math.Abs(sa[i] * p.fx + sb[i] * p.fy - sc[i]) / Math.Sqrt(sN[i]);
        }

        /// <summary>수선의 발(실수 — 그리기용).</summary>
        public void Foot(double px, double py, int i, out double fx, out double fy, out double t)
        {
            var a = V[i]; var b = V[(i + 1) % 3];
            double dx = b.x - a.x, dy = b.y - a.y;
            t = ((px - a.x) * dx + (py - a.y) * dy) / (dx * dx + dy * dy);
            fx = a.x + dx * t; fy = a.y + dy * t;
        }

        public bool InsideStrict(IP p)
        {
            long s0 = Geo.Cross(V[1] - V[0], p - V[0]), s1 = Geo.Cross(V[2] - V[1], p - V[1]), s2 = Geo.Cross(V[0] - V[2], p - V[2]);
            return (s0 > 0 && s1 > 0 && s2 > 0) || (s0 < 0 && s1 < 0 && s2 < 0);
        }

        /// <summary>격자 전체 확인: (1) 두 거리의 표시(둘째 자리)가 같다 ⇔ 정수로 같다, (2) 세 거리가 같은 점은 정답 하나뿐
        /// (내심 문항에서 방심이 격자 안 격자점이면 탈락). 내심 문항은 배치마다 이걸 통과해야 쓴다.</summary>
        public bool DisplayConsistent()
        {
            int n = 0;
            for (int x = -Rules.GX; x <= Rules.GX; x++)
                for (int y = -Rules.GY; y <= Rules.GY; y++)
                {
                    var p = RP.Of(new IP(x, y));
                    long h0 = DistH(p, 0, m), h1 = DistH(p, 1, m), h2 = DistH(p, 2, m);
                    if ((h0 == h1) != EqualPair(p, 0, 1, m) || (h1 == h2) != EqualPair(p, 1, 2, m) || (h2 == h0) != EqualPair(p, 2, 0, m)) return false;
                    if (AllEqual(p, m)) { n++; if (!p.Is(ans)) return false; }
                }
            return n == 1;
        }

        // ── 2단계 주름(접는 선)
        /// <summary>꼭짓점 i 를 꼭짓점 j 에 포개어 접은 주름 = 변 ViVj 의 수직이등분선(정수 계수).</summary>
        public Line PerpBisector(int i, int j)
        {
            var p = V[i]; var q = V[j];
            return Line.Int(2L * (q.x - p.x), 2L * (q.y - p.y), q.Len2 - p.Len2);
        }

        /// <summary>꼭짓점 k 에 모인 두 변을 포개어 접은 주름 = ∠k 의 이등분선. 내심 문항은 V_k 와 I(격자점)를 지나는 정수 직선,
        /// 외심 문항은 두 단위벡터의 합 방향(무리수) — 외심 문항에서 이 주름은 오답 종류라 정답과 만날 수 없다(PassesStage2).</summary>
        public Line AngleBisector(int k)
        {
            var v = V[k];
            if (m == Mission.In)
            {
                long dx = ans.x - v.x, dy = ans.y - v.y;
                return Line.Int(dy, -dx, dy * v.x - dx * v.y);
            }
            var u = V[(k + 1) % 3] - v; var w = V[(k + 2) % 3] - v;
            double lu = Math.Sqrt(u.Len2), lw = Math.Sqrt(w.Len2);
            double ex = u.x / lu + w.x / lw, ey = u.y / lu + w.y / lw;
            return Line.Dbl(ey, -ex, ey * v.x - ex * v.y);
        }

        public RP SideMid(int i, int j) => RP.Rat(V[i].x + V[j].x, V[i].y + V[j].y, 2);

        public static bool Meet(Line l1, Line l2, out RP p)
        {
            if (l1.exact && l2.exact)
            {
                long det = l1.a * l2.b - l2.a * l1.b;
                if (det == 0) { p = default; return false; }
                p = RP.Rat(l1.c * l2.b - l2.c * l1.b, l1.a * l2.c - l2.a * l1.c, det);
                return true;
            }
            double dd = l1.fa * l2.fb - l2.fa * l1.fb;
            if (Math.Abs(dd) < 1e-12) { p = default; return false; }
            p = RP.Approx((l1.fc * l2.fb - l2.fc * l1.fb) / dd, (l1.fa * l2.fc - l2.fa * l1.fc) / dd);
            return true;
        }

        /// <summary>2단계 조건: 반대 종류의 주름이 정답을 지나지 않는다(정수 판정).
        /// 외심 문항: O 가 어떤 각의 이등분선 위에도 없다 ⇔ 변 AB, AC 까지의 거리가 다르다 (L²·N 교차곱).
        /// 내심 문항: I 가 어떤 수직이등분선 위에도 없다 ⇔ |I−Vi|² ≠ |I−Vj|².</summary>
        public bool PassesStage2()
        {
            if (iso) return false;
            var a = RP.Of(ans);
            for (int k = 0; k < 3; k++)
            {
                if (m == Mission.Circum)
                {
                    int s1 = k, s2 = (k + 2) % 3;          // 꼭짓점 k 에 모이는 두 변: k→k+1, k+2→k
                    long l1 = SideL(a, s1), l2 = SideL(a, s2);
                    if (l1 * l1 * sN[s2] == l2 * l2 * sN[s1]) return false;
                }
                else if (VertN(a, k) == VertN(a, (k + 1) % 3)) return false;
            }
            return true;
        }
    }

    /// <summary>외심 문항 모양: 외접원 중심을 원점으로 둔 세 꼭짓점 오프셋(반지름² 이 같은 격자점 셋).</summary>
    public class OShape { public IP a, b, c; public long R2; public Kind kind; public bool iso; }
    /// <summary>내심 문항 모양: 내심을 원점으로 둔 세 꼭짓점 오프셋. 세 변의 법선이 피타고라스 방향이라 변까지의 거리가 유한소수.</summary>
    public class IShape { public IP a, b, c; public int r; public Kind kind; public bool iso; }

    /// <summary>제약 만족 풀에서 샘플링하는 생성기. 부팅 때 조건을 만족하는 모양 풀을 만들고 거기서 뽑아 격자 안에 둔다.
    /// 게임 명판과 문제 은행이 같은 풀·같은 Make 를 쓴다.</summary>
    public class Gen
    {
        public readonly List<OShape> oAcute = new List<OShape>(), oRight = new List<OShape>(), oObtuse = new List<OShape>();
        public readonly List<IShape> iShapes = new List<IShape>();
        const double MinAngle = 25, MaxAngle = 125;

        public Gen()
        {
            BuildO();
            BuildI();
        }

        /// <summary>세 각이 [MinAngle, MaxAngle] 안인가(생성 필터 — 판정이 아니다). 반시계 방향으로 정렬해 돌려준다.</summary>
        static bool ShapeOk(ref IP a, ref IP b, ref IP c, double minA)
        {
            long cr = Geo.Cross(b - a, c - a);
            if (cr == 0) return false;
            if (cr < 0) { var t = b; b = c; c = t; }
            var P = new[] { a, b, c };
            for (int i = 0; i < 3; i++)
            {
                var u = P[(i + 1) % 3] - P[i]; var v = P[(i + 2) % 3] - P[i];
                double cos = Geo.Dot(u, v) / Math.Sqrt((double)u.Len2 * v.Len2);
                double ang = Math.Acos(Math.Max(-1, Math.Min(1, cos))) * 180 / Math.PI;
                if (ang < minA - 1e-9 || ang > MaxAngle + 1e-9) return false;
                if (u.Len2 < 10) return false;                          // 가장 짧은 변도 3칸 이상
            }
            // 모눈 안에 들어갈 크기 + 너무 작지 않게
            int x0 = Math.Min(a.x, Math.Min(b.x, c.x)), x1 = Math.Max(a.x, Math.Max(b.x, c.x));
            int y0 = Math.Min(a.y, Math.Min(b.y, c.y)), y1 = Math.Max(a.y, Math.Max(b.y, c.y));
            if (x1 - x0 > 2 * Rules.GX - 2 || y1 - y0 > 2 * Rules.GY - 2) return false;
            if (Math.Abs(cr) < 30) return false;                        // 넓이의 두 배 ≥ 30
            return true;
        }

        static Kind KindOf(IP a, IP b, IP c, out bool iso)
        {
            var P = new[] { a, b, c }; Kind k = Kind.Acute;
            for (int i = 0; i < 3; i++)
            {
                long d = Geo.Dot(P[(i + 1) % 3] - P[i], P[(i + 2) % 3] - P[i]);
                if (d == 0) k = Kind.Right; else if (d < 0) k = Kind.Obtuse;
            }
            long s0 = (b - a).Len2, s1 = (c - b).Len2, s2 = (a - c).Len2;
            iso = s0 == s1 || s1 == s2 || s0 == s2;
            return k;
        }

        void BuildO()
        {
            var seen = new HashSet<string>();
            for (long R2 = 5; R2 <= 72; R2++)
            {
                var pts = new List<IP>();
                int lim = (int)Math.Sqrt(R2) + 1;
                for (int x = -lim; x <= lim; x++) for (int y = -lim; y <= lim; y++) if ((long)x * x + (long)y * y == R2) pts.Add(new IP(x, y));
                if (pts.Count < 8) continue;
                for (int i = 0; i < pts.Count; i++)
                    for (int j = i + 1; j < pts.Count; j++)
                        for (int k = j + 1; k < pts.Count; k++)
                        {
                            IP a = pts[i], b = pts[j], c = pts[k];
                            if (!ShapeOk(ref a, ref b, ref c, MinAngle)) continue;
                            var kind = KindOf(a, b, c, out bool iso);
                            string key = a + "" + b + c;
                            if (!seen.Add(key)) continue;
                            var s = new OShape { a = a, b = b, c = c, R2 = R2, kind = kind, iso = iso };
                            (kind == Kind.Acute ? oAcute : kind == Kind.Right ? oRight : oObtuse).Add(s);
                        }
            }
        }

        void BuildI()
        {
            // 변의 바깥 법선 (p, q) 과 √(p²+q²)=h (피타고라스 수). 변까지의 거리 = 정수/h (유리수) — 정수 연산으로 반올림해 보인다.
            // h 가 서로 다른 두 변의 거리가 둘째 자리에서 같게 보일 수 있으므로, 배치마다 DisplayConsistent() 로 격자 전체를 확인한다.
            var dirs = new List<int[]>();
            int[][] bases = { new[] { 1, 0, 1 }, new[] { 3, 4, 5 }, new[] { 5, 12, 13 }, new[] { 8, 15, 17 }, new[] { 7, 24, 25 } };
            foreach (var bs in bases)
                for (int sw = 0; sw < 2; sw++)
                    for (int sx = -1; sx <= 1; sx += 2)
                        for (int sy = -1; sy <= 1; sy += 2)
                        {
                            int p = (sw == 0 ? bs[0] : bs[1]) * sx, q = (sw == 0 ? bs[1] : bs[0]) * sy;
                            bool dup = false; foreach (var d in dirs) if (d[0] == p && d[1] == q) dup = true;
                            if (!dup) dirs.Add(new[] { p, q, bs[2] });
                        }
            var seen = new HashSet<string>();
            for (int r = 1; r <= 6; r++)
                for (int i = 0; i < dirs.Count; i++)
                    for (int j = i + 1; j < dirs.Count; j++)
                        for (int k = j + 1; k < dirs.Count; k++)
                        {
                            var n = new[] { dirs[i], dirs[j], dirs[k] };
                            // 원점(내심)이 세 법선의 볼록 껍질 안에 있어야 삼각형이 원을 품는다(교차곱 부호가 모두 같다)
                            long c0 = (long)n[0][0] * n[1][1] - (long)n[0][1] * n[1][0];
                            long c1 = (long)n[1][0] * n[2][1] - (long)n[1][1] * n[2][0];
                            long c2 = (long)n[2][0] * n[0][1] - (long)n[2][1] * n[0][0];
                            if (!((c0 > 0 && c1 > 0 && c2 > 0) || (c0 < 0 && c1 < 0 && c2 < 0))) continue;
                            var P = new IP[3]; bool ok = true;
                            for (int e = 0; e < 3 && ok; e++)
                            {
                                var L1 = n[e]; var L2 = n[(e + 1) % 3];
                                long det = (long)L1[0] * L2[1] - (long)L2[0] * L1[1];
                                long xn = (long)r * ((long)L1[2] * L2[1] - (long)L2[2] * L1[1]);
                                long yn = (long)r * ((long)L1[0] * L2[2] - (long)L2[0] * L1[2]);
                                if (det == 0 || xn % det != 0 || yn % det != 0) { ok = false; break; }
                                P[e] = new IP((int)(xn / det), (int)(yn / det));
                            }
                            if (!ok) continue;
                            IP a = P[0], b = P[1], c = P[2];
                            if (!ShapeOk(ref a, ref b, ref c, 20)) continue;
                            var kind = KindOf(a, b, c, out bool iso);
                            string key = a + "" + b + c;
                            if (!seen.Add(key)) continue;
                            iShapes.Add(new IShape { a = a, b = b, c = c, r = r, kind = kind, iso = iso });
                        }
        }

        /// <summary>모양을 격자 안에 두고 꼭짓점 이름(A, B, C — 반시계)을 정한다. 실패하면 null.</summary>
        static Prob Place(IP a, IP b, IP c, IP center, Mission m, int stage, Random rng, int labelShift)
        {
            var off = new[] { a, b, c };
            int x0 = -Rules.GX - Math.Min(0, Math.Min(a.x, Math.Min(b.x, c.x))), x1 = Rules.GX - Math.Max(0, Math.Max(a.x, Math.Max(b.x, c.x)));
            int y0 = -Rules.GY - Math.Min(0, Math.Min(a.y, Math.Min(b.y, c.y))), y1 = Rules.GY - Math.Max(0, Math.Max(a.y, Math.Max(b.y, c.y)));
            if (x0 > x1 || y0 > y1) return null;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                var O = new IP(x0 + rng.Next(x1 - x0 + 1), y0 + rng.Next(y1 - y0 + 1));
                var pr = new Prob { m = m, stage = stage, ans = O };
                for (int i = 0; i < 3; i++) pr.V[i] = O + off[(i + labelShift) % 3];
                if (!Admissible(pr)) continue;
                pr.Prepare();
                return pr;
            }
            return null;
        }

        /// <summary>핀 출발점과 겹치지 않고, 정답이 출발점에서 충분히 멀다.</summary>
        static bool Admissible(Prob p)
        {
            var s = Rules.Start;
            for (int i = 0; i < 3; i++) if (p.V[i].Same(s)) return false;
            if ((p.ans - s).Len2 < 16) return false;
            return Rules.InGrid(p.ans);
        }

        /// <summary>외심 문항 한 장.</summary>
        public Prob MakeO(int stage, Kind want, Random rng)
        {
            var pool = want == Kind.Acute ? oAcute : want == Kind.Right ? oRight : oObtuse;
            for (int t = 0; t < 400; t++)
            {
                var s = pool[rng.Next(pool.Count)];
                if (stage == 2 && s.iso) continue;
                var p = Place(s.a, s.b, s.c, default, Mission.Circum, stage, rng, rng.Next(3));
                if (p == null) continue;
                p.R2 = s.R2;
                if (stage == 2 && !p.PassesStage2()) continue;
                return p;
            }
            throw new Exception("MakeO 실패");
        }

        /// <summary>내심 문항 한 장. 격자 전체에서 세 수선이 같아지는 점이 내심 하나뿐인 배치만 쓴다(방심 제외).</summary>
        public Prob MakeI(int stage, Random rng)
        {
            for (int t = 0; t < 600; t++)
            {
                var s = iShapes[rng.Next(iShapes.Count)];
                if (stage == 2 && s.iso) continue;
                var p = Place(s.a, s.b, s.c, default, Mission.In, stage, rng, rng.Next(3));
                if (p == null) continue;
                p.inR = s.r;
                if (!p.DisplayConsistent()) continue;
                if (stage == 2 && !p.PassesStage2()) continue;
                return p;
            }
            throw new Exception("MakeI 실패");
        }

        /// <summary>첫 명판(온보딩 고정): 외심 O 는 격자점 (2, 1), 반지름² 25. A(−1, 5)·B(2, −4)·C(6, 4) — 예각 부등변.
        /// 핀 출발점 (0, −7) 에서 곧장 위로 가면 B 를 지나므로, 「가장 가까운 꼭짓점에서 멀어지기」가 드러나는 배치.</summary>
        public static Prob First()
        {
            var p = new Prob { m = Mission.Circum, stage = 1, ans = new IP(2, 1), R2 = 25, tutorial = true };
            p.V[0] = new IP(-1, 5); p.V[1] = new IP(2, -4); p.V[2] = new IP(6, 4);
            p.Prepare();
            return p;
        }

        /// <summary>한 판의 명판 순서. stage1: 0 번은 First(), 이후 내심·외심 교대(외심은 예각→직각→둔각 순환).
        /// stage2: 외심(예각)·내심·외심(직각)·내심·외심(둔각)·내심. 2단계 첫 외심·첫 내심 명판은 안내 명판.</summary>
        public Prob Next(int stage, int idx, Random rng)
        {
            if (stage == 1)
            {
                if (idx == 0) return First();
                if (idx % 2 == 1) { var p = MakeI(1, rng); p.tutorial = idx == 1; return p; }
                int k = (idx / 2 - 1) % 3;
                return MakeO(1, k == 0 ? Kind.Acute : k == 1 ? Kind.Right : Kind.Obtuse, rng);
            }
            int j = idx % 6;
            Prob q = j % 2 == 1 ? MakeI(2, rng) : MakeO(2, j == 0 ? Kind.Acute : j == 2 ? Kind.Right : Kind.Obtuse, rng);
            q.tutorial = idx < 2;
            return q;
        }
    }

    /// <summary>문항 문장·정답 — 게임 화면 문장과 같은 데이터 모델에서 만든다.</summary>
    public static class Words
    {
        /// <summary>「오른쪽으로 7칸, 아래로 1칸」 — 모눈 칸 이동으로 위치를 말한다(좌표 괄호 표기 대신).</summary>
        public static string Rel(IP from, IP to)
        {
            int dx = to.x - from.x, dy = to.y - from.y;
            string h = dx > 0 ? $"오른쪽으로 {dx}칸" : dx < 0 ? $"왼쪽으로 {-dx}칸" : "";
            string v = dy > 0 ? $"위로 {dy}칸" : dy < 0 ? $"아래로 {-dy}칸" : "";
            return h.Length > 0 && v.Length > 0 ? h + ", " + v : h + v;
        }

        public static string TriText(Prob p) =>
            $"점 B는 점 A에서 {Rel(p.V[0], p.V[1])}, 점 C는 점 A에서 {Rel(p.V[0], p.V[2])} 떨어진 곳에 있다";

        public static string Prompt(Prob p)
        {
            string tri = TriText(p);
            if (p.stage == 1)
                return p.m == Mission.Circum
                    ? $"모눈 위의 △ABC에서 {tri}. 세 꼭짓점에 이르는 거리를 재어 △ABC의 외심 O를 찾으시오."
                    : $"모눈 위의 △ABC에서 {tri}. 점에서 세 변까지의 거리를 재어 △ABC의 내심 I를 찾으시오.";
            return p.m == Mission.Circum
                ? $"트레이싱 필름에 그린 △ABC에서 {tri}. 필름을 접어 △ABC의 외심 O를 찾고, 두 주름의 교점에 핀을 박으시오."
                : $"트레이싱 필름에 그린 △ABC에서 {tri}. 필름을 접어 △ABC의 내심 I를 찾고, 두 주름의 교점에 핀을 박으시오.";
        }

        public static string Where(Prob p)
        {
            if (p.m == Mission.In) return "삼각형의 내부";
            if (p.rightV >= 0)
            {
                int i = (p.rightV + 1) % 3, j = (p.rightV + 2) % 3;
                return $"빗변 {Geo.Names[Math.Min(i, j)]}{Geo.Names[Math.Max(i, j)]}의 중점";
            }
            return p.obtuseV >= 0 ? "삼각형의 외부" : "삼각형의 내부";
        }

        static bool IntRadius(Prob p, out long R) { R = Geo.ISqrt(p.R2); return R * R == p.R2; }

        public static string Answer(Prob p)
        {
            string at = $"점 A에서 {Rel(p.V[0], p.ans)} 떨어진 점({Where(p)})";
            if (p.m == Mission.Circum)
            {
                string eq = IntRadius(p, out long R) ? $"OA=OB=OC={R}" : "OA=OB=OC";
                return p.stage == 1
                    ? $"외심 O: {at}, {eq}"
                    : $"두 꼭짓점을 포개어 접은 주름(변의 수직이등분선) 두 개의 교점. 외심 O: {at}, {eq}";
            }
            return p.stage == 1
                ? $"내심 I: {at}, 세 변까지의 거리는 모두 {p.inR}"
                : $"두 변을 포개어 접은 주름(각의 이등분선) 두 개의 교점. 내심 I: {at}, 세 변까지의 거리는 모두 {p.inR}";
        }

        public static double Numeric(Prob p)
        {
            if (p.m == Mission.In) return p.inR;
            return IntRadius(p, out long R) ? R : double.NaN;
        }

        public static string Concept(Prob p)
        {
            if (p.stage == 2) return p.m == Mission.Circum ? "외심은 세 변의 수직이등분선의 교점" : "내심은 세 내각의 이등분선의 교점";
            if (p.m == Mission.In) return "삼각형의 내심에서 세 변에 이르는 거리는 같다";
            if (p.kind == Kind.Right) return "직각삼각형의 외심은 빗변의 중점";
            if (p.kind == Kind.Obtuse) return "둔각삼각형의 외심은 삼각형의 외부";
            return "삼각형의 외심에서 세 꼭짓점에 이르는 거리는 같다";
        }
    }
}
