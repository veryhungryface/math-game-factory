// 원 찍어 — 기하·문제 생성·판정 (UnityEngine 의존 없음: 에디터 봇 자가 테스트도 이 코드를 그대로 쓴다)
//
// 표현 함정(2022-middle-math.json expression_traps / style_guide 에서 옮김):
//  - 종결형은 「~을 나타내시오」. 초등 「~해 보세요」·탐구형 「~해 보자」를 발문으로 쓰지 않는다.
//  - 용어는 교과서 그대로: 외심·내심·꼭지각·밑각·밑변·빗변. △ABC, ∠A, 70° 표기.
//  - 중2 에는 √ 가 없다 → 화면·발문에 제곱근 기호를 만들지 않는다. 판정은 거리² 정수 비교.
//  - 직각삼각형 합동(RHA·RHS)은 이 메커닉에서 다루지 않는다(기획서 lessons_covered 1·3·4차시).
//
// 좌표계: 외접원의 중심 O 를 원점, 외접원의 반지름 R=1 로 둔 "단위 좌표"에서 모양을 정하고,
// 게임이 R(월드 단위)과 O 의 월드 위치를 정해 배치한다. 단위 좌표 y+ = 화면 위쪽(월드 z+).
//
// 오개념 역산(검산관용 misconceptionId) — 이 게임의 "오답"은 선택지가 아니라 핀을 박은 자리다.
// 생성기는 아래 오답 자리가 정답 허용원(tol) 밖에 있도록 풀 전체를 거른다(PassesTraps):
//   M1 swap      : 외심 미션의 내심 자리 / 내심 미션의 외심 자리           (외심·내심 혼동)
//   M2 inside    : 둔각삼각형 외심 미션에서 삼각형 내부 전체 — 외심은 바깥에 있다  (외심은 항상 내부)
//   M3 bottom    : 화면에서 가장 아래 변의 중점 — 이등변삼각형을 28° 이상 돌려 밑변이 아래가 아니게  (밑각=아래 두 각)
//   M4 middle    : 무게중심 G·세 꼭짓점을 감싸는 상자의 중심 — 「세 중심은 한 점」이라며 한가운데를 누름
using System;
using System.Collections.Generic;

namespace Mgf.WonJjigeo
{
    public struct V2
    {
        public double x, y;
        public V2(double x, double y) { this.x = x; this.y = y; }
        public static V2 operator +(V2 a, V2 b) => new V2(a.x + b.x, a.y + b.y);
        public static V2 operator -(V2 a, V2 b) => new V2(a.x - b.x, a.y - b.y);
        public static V2 operator *(V2 a, double k) => new V2(a.x * k, a.y * k);
        public double Len => Math.Sqrt(x * x + y * y);
    }

    public enum Kind { Iso, Scalene, Right, Obtuse }
    /// <summary>Build=구성(핀 박기) · Reverse=역추적(원이 먼저, 그 중심을 박기) · Fix=오류 찾기(잘못 박힌 핀을 뽑고 다시)</summary>
    public enum Task { Build, Reverse, Fix }

    public class PlateSpec
    {
        public int A, B, C;          // 세 내각(도, 정수, 합 180)
        public Kind kind;
        public bool incenter;         // false=외심 미션, true=내심 미션
        public Task task;
        public int rot;               // 회전(도)
        public int band;              // 난이도 밴드 1~3
        public bool fixAtCentroid;    // Fix: 잘못 박힌 핀이 무게중심(true) 또는 반대 중심(false)

        public string Mission => incenter ? "내심" : "외심";
        public char CenterLetter => incenter ? 'I' : 'O';

        public int ObtuseVertex => A > 90 ? 0 : B > 90 ? 1 : C > 90 ? 2 : -1;
        public int RightVertex => A == 90 ? 0 : B == 90 ? 1 : C == 90 ? 2 : -1;
    }

    public static class Geo
    {
        public const double Deg = Math.PI / 180.0;
        /// <summary>허용 반지름 = 외접원 반지름 × 0.08. 함정 자리까지의 최소 거리 = 2.5 × tol.</summary>
        public const double TolK = 0.08;
        public const double TrapK = 2.5 * TolK;
        public static readonly string[] Names = { "A", "B", "C" };

        /// <summary>단위 좌표(O=원점, R=1)의 세 꼭짓점. 호 AB=2C, 호 BC=2A, 호 CA=2B (원주각의 두 배).</summary>
        public static void Unit(PlateSpec s, V2[] p)
        {
            double a0 = 90 + s.rot, b0 = a0 + 2 * s.C, c0 = b0 + 2 * s.A;
            p[0] = new V2(Math.Cos(a0 * Deg), Math.Sin(a0 * Deg));
            p[1] = new V2(Math.Cos(b0 * Deg), Math.Sin(b0 * Deg));
            p[2] = new V2(Math.Cos(c0 * Deg), Math.Sin(c0 * Deg));
        }

        /// <summary>내심 = (a·A + b·B + c·C)/(a+b+c). 변의 길이 a=2R·sinA 이므로 사인값을 가중치로 쓴다(R 소거).</summary>
        public static V2 Incenter(PlateSpec s, V2[] p)
        {
            double a = Math.Sin(s.A * Deg), b = Math.Sin(s.B * Deg), c = Math.Sin(s.C * Deg), t = a + b + c;
            return new V2((a * p[0].x + b * p[1].x + c * p[2].x) / t, (a * p[0].y + b * p[1].y + c * p[2].y) / t);
        }

        /// <summary>외심 = 세 변의 수직이등분선의 교점. 배치된 좌표에서 직접 구한다(단위 좌표에선 원점이지만 검산용으로 일반식).</summary>
        public static V2 Circumcenter(V2[] p)
        {
            double ax = p[0].x, ay = p[0].y, bx = p[1].x, by = p[1].y, cx = p[2].x, cy = p[2].y;
            double d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
            double a2 = ax * ax + ay * ay, b2 = bx * bx + by * by, c2 = cx * cx + cy * cy;
            return new V2((a2 * (by - cy) + b2 * (cy - ay) + c2 * (ay - by)) / d, (a2 * (cx - bx) + b2 * (ax - cx) + c2 * (bx - ax)) / d);
        }

        public static V2 Centroid(V2[] p) => new V2((p[0].x + p[1].x + p[2].x) / 3, (p[0].y + p[1].y + p[2].y) / 3);

        public static V2 BoxCenter(V2[] p)
        {
            double x0 = Math.Min(p[0].x, Math.Min(p[1].x, p[2].x)), x1 = Math.Max(p[0].x, Math.Max(p[1].x, p[2].x));
            double y0 = Math.Min(p[0].y, Math.Min(p[1].y, p[2].y)), y1 = Math.Max(p[0].y, Math.Max(p[1].y, p[2].y));
            return new V2((x0 + x1) / 2, (y0 + y1) / 2);
        }

        /// <summary>화면에서 가장 아래(y 최소)에 놓인 변의 중점 — 「밑변=아래 변」 오개념의 자리.</summary>
        public static V2 BottomEdgeMid(V2[] p)
        {
            int best = 0; double by = double.MaxValue;
            for (int i = 0; i < 3; i++) { int j = (i + 1) % 3; double y = (p[i].y + p[j].y) / 2; if (y < by) { by = y; best = i; } }
            int k = (best + 1) % 3;
            return new V2((p[best].x + p[k].x) / 2, (p[best].y + p[k].y) / 2);
        }

        /// <summary>점에서 직선 PQ 까지의 거리.</summary>
        public static double LineDist(V2 x, V2 p, V2 q)
        {
            var d = q - p; double l = d.Len;
            return Math.Abs(d.x * (x.y - p.y) - d.y * (x.x - p.x)) / l;
        }

        public static bool InsideTri(V2 x, V2[] p)
        {
            double s0 = Cross(p[1] - p[0], x - p[0]), s1 = Cross(p[2] - p[1], x - p[1]), s2 = Cross(p[0] - p[2], x - p[2]);
            return (s0 >= 0 && s1 >= 0 && s2 >= 0) || (s0 <= 0 && s1 <= 0 && s2 <= 0);
        }
        static double Cross(V2 a, V2 b) => a.x * b.y - a.y * b.x;

        public static V2 Answer(PlateSpec s, V2[] p) => s.incenter ? Incenter(s, p) : Circumcenter(p);

        // ── 판정: 좌표를 1/1000 월드 단위 정수로 바꿔 거리²를 정수로 비교한다(부동소수 동등 비교 없음).
        public static long Milli(double v) => (long)Math.Round(v * 1000.0);

        public static bool Hit(V2 pin, V2 answer, double tol)
        {
            long dx = Milli(pin.x) - Milli(answer.x), dy = Milli(pin.y) - Milli(answer.y), t = Milli(tol);
            return dx * dx + dy * dy <= t * t;
        }

        /// <summary>함정 자리 전부가 정답 허용원의 2.5배 밖에 있는가(단위 좌표, R=1).</summary>
        public static bool PassesTraps(PlateSpec s)
        {
            var p = new V2[3];
            Unit(s, p);
            var ans = Answer(s, p);
            var other = s.incenter ? Circumcenter(p) : Incenter(s, p);
            if ((Centroid(p) - ans).Len < TrapK) return false;      // M4
            if ((BoxCenter(p) - ans).Len < TrapK) return false;     // M4
            if ((other - ans).Len < TrapK) return false;            // M1
            if (!s.incenter && s.kind != Kind.Right && (BottomEdgeMid(p) - ans).Len < TrapK) return false; // M3
            if (s.kind == Kind.Iso)
            {
                // 밑변 BC 가 수평에서 28° 이상 기울어야 한다(M3)
                double ang = Math.Atan2(p[2].y - p[1].y, p[2].x - p[1].x) / Deg;
                double h = ((ang % 180) + 180) % 180, tilt = Math.Min(h, 180 - h);
                if (tilt < 28) return false;
            }
            return true;
        }

        public static int Gcd(int a, int b) { while (b != 0) { int t = a % b; a = b; b = t; } return a; }
    }

    /// <summary>풀 항목: 각의 세 쌍 + 미션 + 함정을 통과하는 회전 목록(5° 간격).</summary>
    public class PoolEntry
    {
        public int A, B, C; public Kind kind; public bool incenter;
        public List<int> rots = new List<int>();
    }

    /// <summary>제약 만족 풀에서 샘플링하는 생성기. 무작위 생성 후 사후 거르기가 아니라, 부팅 때 조건을 만족하는
    /// (각, 미션, 회전) 풀을 만든 뒤 거기서만 뽑는다. 게임 판과 문제 은행이 같은 풀·같은 Make 를 쓴다.</summary>
    public class Gen
    {
        public readonly List<PoolEntry> isoO = new List<PoolEntry>();   // 이등변 예각(꼭지각 A, AB=AC) · 외심
        public readonly List<PoolEntry> scO = new List<PoolEntry>();    // 부등변 예각 · 외심
        public readonly List<PoolEntry> scI = new List<PoolEntry>();    // 부등변 예각 · 내심
        public readonly List<PoolEntry> rightO = new List<PoolEntry>(); // 직각 · 외심(빗변의 중점)
        public readonly List<PoolEntry> obtO = new List<PoolEntry>();   // 둔각 · 외심(바깥)
        public readonly List<PoolEntry> wideI = new List<PoolEntry>();  // 직각·둔각 · 내심(항상 내부)
        Random rng;
        /// <summary>판마다 다른 명판 순서(풀은 그대로 두고 난수만 바꾼다).</summary>
        public void Reseed(int seed) { rng = new Random(seed); }

        public Gen(int seed)
        {
            rng = new Random(seed);
            for (int A = 22; A <= 128; A++)
            for (int B = 22; B <= 128; B++)
            {
                int C = 180 - A - B;
                if (C < 22 || C > 128) continue;
                // 정삼각형에 가까운 것 금지(가장 긴 변/가장 짧은 변 < 1.05 이면 세 중심이 거의 겹친다)
                double sa = Math.Sin(A * Geo.Deg), sb = Math.Sin(B * Geo.Deg), sc = Math.Sin(C * Geo.Deg);
                if (Math.Max(sa, Math.Max(sb, sc)) / Math.Min(sa, Math.Min(sb, sc)) < 1.05) continue;
                Kind k;
                if (A == 90 || B == 90 || C == 90) k = Kind.Right;
                else if (A > 90 || B > 90 || C > 90) k = Kind.Obtuse;
                else if (A == B || B == C || A == C) k = Kind.Iso;
                else k = Kind.Scalene;
                if (k == Kind.Iso && B != C) continue;            // 이등변은 꼭지각을 A 로(AB=AC) 통일
                if (k == Kind.Scalene && (Math.Abs(A - B) < 5 || Math.Abs(B - C) < 5 || Math.Abs(A - C) < 5)) continue; // 눈으로 이등변처럼 보이는 것 제외
                for (int m = 0; m < 2; m++)
                {
                    bool inc = m == 1;
                    if (k == Kind.Iso && inc) continue;
                    if (inc && (A % 2 != 0)) continue;             // ∠BIC=90°+½∠A 가 정수가 되게
                    var e = new PoolEntry { A = A, B = B, C = C, kind = k, incenter = inc };
                    var s = new PlateSpec { A = A, B = B, C = C, kind = k, incenter = inc };
                    for (int r = 0; r < 360; r += 5) { s.rot = r; if (Geo.PassesTraps(s)) e.rots.Add(r); }
                    if (e.rots.Count == 0) continue;
                    switch (k)
                    {
                        case Kind.Iso: isoO.Add(e); break;
                        case Kind.Scalene: (inc ? scI : scO).Add(e); break;
                        case Kind.Right: (inc ? wideI : rightO).Add(e); break;
                        case Kind.Obtuse: (inc ? wideI : obtO).Add(e); break;
                    }
                }
            }
        }

        public int Next(int n) => rng.Next(n);
        public double NextD() => rng.NextDouble();

        PlateSpec From(List<PoolEntry> pool, int band)
        {
            var e = pool[rng.Next(pool.Count)];
            return new PlateSpec { A = e.A, B = e.B, C = e.C, kind = e.kind, incenter = e.incenter, rot = e.rots[rng.Next(e.rots.Count)], band = band };
        }

        /// <summary>밴드별 명판 한 장. plateIndex 는 밴드 안 몇 번째인지(교대 규칙용).</summary>
        public PlateSpec Make(int band, int plateIndex)
        {
            PlateSpec s;
            if (band <= 1)
            {
                s = From(isoO, 1);
                s.task = Task.Build;
                return s;
            }
            if (band == 2)
            {
                s = From(plateIndex % 2 == 0 ? scO : scI, 2);   // 외심/내심 교대
            }
            else
            {
                int r = rng.Next(4);
                s = From(r == 0 ? rightO : r == 1 ? obtO : r == 2 ? (rng.Next(2) == 0 ? obtO : rightO) : (rng.Next(2) == 0 ? wideI : (rng.Next(2) == 0 ? scI : scO)), 3);
            }
            int t = rng.Next(12);                  // 역추적 1/4 · 오류 찾기 1/6 · 나머지 구성
            s.task = t < 3 ? Task.Reverse : t < 5 ? Task.Fix : Task.Build;
            s.fixAtCentroid = rng.Next(2) == 0;
            return s;
        }

        /// <summary>첫 문항 고정(온보딩): AB=AC, ∠A=30°, ∠B=∠C=75°, 70° 회전(밑변 BC 가 70° 기울어 아래 변이 아니다), 외심.
        /// 기획서 초안의 ∠A=50° 는 무게중심이 외심에서 0.1R 밖에 안 떨어져(허용원 안) 무뇌 한가운데 탭이 통과하므로 바꿨다.</summary>
        public static PlateSpec First() => new PlateSpec { A = 30, B = 75, C = 75, kind = Kind.Iso, incenter = false, rot = 70, band = 1, task = Task.Build };
    }

    /// <summary>문항 문장·정답 — 게임 화면 문장과 같은 데이터 모델에서 만든다.</summary>
    public static class Words
    {
        public static string Tri(PlateSpec s)
        {
            if (s.kind == Kind.Iso) return $"AB=AC, ∠A={s.A}°인 이등변삼각형 ABC";
            if (s.kind == Kind.Right)
            {
                int rv = s.RightVertex, ov = rv == 0 ? 1 : 0;   // 직각이 아닌 첫 꼭짓점의 각을 함께 준다
                int[] ang = { s.A, s.B, s.C };
                return $"∠{Geo.Names[rv]}=90°, ∠{Geo.Names[ov]}={ang[ov]}°인 직각삼각형 ABC";
            }
            return $"∠A={s.A}°, ∠B={s.B}°, ∠C={s.C}°인 △ABC";
        }

        public static string Prompt(PlateSpec s)
        {
            string t = Tri(s);
            switch (s.task)
            {
                case Task.Reverse:
                    return s.incenter
                        ? $"{t}에 세 변에 접하는 원이 그려져 있다. 이 원의 중심인 내심의 위치를 나타내시오."
                        : $"{t}에 세 꼭짓점을 지나는 원이 그려져 있다. 이 원의 중심인 외심의 위치를 나타내시오.";
                case Task.Fix:
                    // 잘못 박힌 핀: 무게중심(M4) 또는 반대 중심(M1). 무게중심은 아직 배우지 않은 말이라 이름을 쓰지 않는다.
                    string wrong = s.fixAtCentroid ? "엉뚱한 점" : (s.incenter ? "외심" : "내심");
                    return $"{t}의 {s.Mission}이라며 {wrong}에 잘못 박힌 핀을 뽑고, {s.Mission}의 위치를 나타내시오.";
                default:
                    return $"{t}의 {s.Mission}의 위치를 나타내시오.";
            }
        }

        /// <summary>∠BOC(외심) 또는 ∠BIC(내심)의 크기 — 정답 자리의 수치 검산값(도).</summary>
        public static int AngleAtCenter(PlateSpec s)
        {
            if (s.incenter) return 90 + s.A / 2;            // 내심: ∠BIC = 90° + ½∠A (A 는 짝수로 생성)
            return 2 * s.A;                                   // 외심: ∠BOC = 2∠A (중심각 — ∠A 가 둔각이면 180° 초과, 외심은 외부)
        }

        public static string Where(PlateSpec s)
        {
            if (s.incenter) return "삼각형의 내부";
            int rv = s.RightVertex, ov = s.ObtuseVertex;
            if (rv >= 0) return $"빗변 {Geo.Names[Math.Min((rv + 1) % 3, (rv + 2) % 3)]}{Geo.Names[Math.Max((rv + 1) % 3, (rv + 2) % 3)]}의 중점";
            if (ov >= 0) return $"삼각형의 외부(둔각 ∠{Geo.Names[ov]}의 맞은편 변 {Geo.Names[(ov + 1) % 3]}{Geo.Names[(ov + 2) % 3]} 바깥쪽)";
            return "삼각형의 내부";
        }

        public static string Answer(PlateSpec s)
        {
            int ang = AngleAtCenter(s);
            return s.incenter
                ? $"내심 I: {Where(s)}, 세 변까지의 거리가 같은 점, ∠BIC={ang}°"
                : $"외심 O: {Where(s)}, OA=OB=OC, ∠BOC={ang}°";
        }

        public static string Concept(PlateSpec s)
        {
            if (s.incenter) return "삼각형의 내심(세 내각의 이등분선의 교점)";
            if (s.kind == Kind.Iso) return "이등변삼각형의 성질과 외심(꼭지각의 이등분선 위)";
            if (s.kind == Kind.Right) return "직각삼각형의 외심은 빗변의 중점";
            if (s.kind == Kind.Obtuse) return "둔각삼각형의 외심은 삼각형의 외부";
            return "삼각형의 외심(세 변의 수직이등분선의 교점)";
        }
    }

    /// <summary>배치된 명판: 월드(XZ) 좌표. 게임·봇 자가 테스트가 같은 배치 함수를 쓴다.</summary>
    public class Placed
    {
        public PlateSpec spec;
        public V2 O;          // 외접원의 중심(월드)
        public double R, tol;
        public readonly V2[] P = new V2[3];
        public V2 ans, other, G;
        public double inR;    // 내접원의 반지름
    }

    public static class Layout
    {
        /// <summary>작업대(베드) 전체: x∈[-BedHX, BedHX], z∈[-BedHZ, BedHZ]. 스크랩 한 장이 오른쪽 가장자리 ScrapW 만큼을 먹는다.</summary>
        public const double BedHX = 4.5, BedHZ = 4.0, ScrapW = 1.1, Margin = 0.32;

        public static double UsableMaxX(int scraps) => BedHX - ScrapW * scraps;

        /// <summary>외접원 전체(둔각이면 삼각형 바깥 외심까지)가 남은 베드 안에 들어오게 둔다 — 조작 불능 방지.
        /// 정답 자리가 베드 한가운데·남은 영역 한가운데에서 2.5 tol 이상 떨어지게 한다(한가운데 연타 봇 차단).</summary>
        public static Placed Place(PlateSpec s, int scraps, Random rng)
        {
            double x0 = -BedHX + Margin, x1 = UsableMaxX(scraps) - Margin, z0 = -BedHZ + Margin, z1 = BedHZ - Margin;
            double maxR = Math.Min(x1 - x0, z1 - z0) / 2;
            double R = Math.Min(maxR, 2.6 + rng.NextDouble() * 0.5);
            var unit = new V2[3];
            Geo.Unit(s, unit);
            var pl = new Placed { spec = s, R = R, tol = Geo.TolK * R };
            var uAns = Geo.Answer(s, unit);
            var bedC = new V2(0, 0);
            var useC = new V2((x0 + x1) / 2, 0);
            for (int attempt = 0; attempt < 80; attempt++)
            {
                double ox = x0 + R + rng.NextDouble() * Math.Max(0, (x1 - x0) - 2 * R);
                double oz = z0 + R + rng.NextDouble() * Math.Max(0, (z1 - z0) - 2 * R);
                var O = new V2(ox, oz);
                var a = O + uAns * R;
                if (attempt < 79 && ((a - bedC).Len < 2.5 * pl.tol || (a - useC).Len < 2.5 * pl.tol)) continue;
                pl.O = O;
                break;
            }
            for (int i = 0; i < 3; i++) pl.P[i] = pl.O + unit[i] * R;
            pl.ans = Geo.Answer(s, pl.P);
            pl.other = s.incenter ? Geo.Circumcenter(pl.P) : Geo.Incenter(s, pl.P);
            pl.G = Geo.Centroid(pl.P);
            pl.inR = 4 * R * Math.Sin(s.A * Geo.Deg / 2) * Math.Sin(s.B * Geo.Deg / 2) * Math.Sin(s.C * Geo.Deg / 2);
            return pl;
        }

        public static bool OnBed(V2 p, int scraps) => p.x >= -BedHX && p.x <= UsableMaxX(scraps) && p.y >= -BedHZ && p.y <= BedHZ;
    }
}
