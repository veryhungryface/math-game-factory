// 딱 맞는 방울 — 규칙(판정·생성기·문제 은행). UnityEngine 에 기대지 않는다(무뇌 봇 자가 테스트가 같은 코드를 쓴다).
//
// 좌표: 판(욕조 등받이 위 작업판) 위의 정수 격자. 1 월드 단위 = Sub(40) 칸. 판 범위 x∈[-HX,HX], y∈[-HY,HY].
// 판정은 전부 정수(decimal 로 128비트 곱을 안전하게)로 한다 — float/double 비교 없음.
//
//   안쪽 방울(내심): 놓은 점 P 에서 세 변 직선까지의 거리 d_i = |cross_i| / |변_i|.
//                    방울은 가장 가까운 벽(d_min)에서 멈춘다. 세 벽에 모두 닿음 ⇔ d_max ≤ (1+tol)·d_min.
//                    제곱해 비교: cross_M²·len_m² · 100² ≤ (100+tol)² · cross_m²·len_M²   (√ 없이 정수)
//   바깥 방울(외심): d_i = |P-V_i| (꼭짓점 못까지). 같은 식, d_i² 가 이미 정수.
//
// 교과서 표현 함정(2022 중학교 style_guide·expression_traps 를 생성기 주석으로 옮김):
//  - 발문은 지시형 「~놓으시오」「~구하시오」로 통일. 「가정」「결론」 낱말을 쓰지 않는다.
//  - 중2 에는 √(제곱근)가 없다. 직각삼각형은 피타고라스 수(3·4·5, 5·12·13, 8·15·17, 20·21·29 …의 배수)만 쓴다.
//  - 외심 = 세 변의 수직이등분선의 교점(세 꼭짓점까지 거리가 같다), 내심 = 세 내각의 이등분선의 교점(세 변까지 거리가 같다).
//  - 이등변삼각형의 꼭지각은 「길이가 같은 두 변 사이의 각」 — 그림에서 위쪽 각이 아니다(회전 배치로 시험).
//  - 각의 크기는 정수 도(°). ∠BOC 문항은 ∠A 가 예각일 때만 낸다(둔각이면 중심각 해석이 갈린다).
//  - 오답 값은 모든 파라미터에서 정답과 달라야 한다: ∠A=60·90·120 은 공식끼리 우연히 같아지므로 제외(SelfCheckBank 가 전수 검사).
using System;
using System.Collections.Generic;
using System.Text;

namespace Mgf.TtakMatneunBangul
{
    public struct IP
    {
        public int x, y;
        public IP(int x, int y) { this.x = x; this.y = y; }
    }

    public enum Kind { In = 0, Out = 1 }
    public enum Shape { Iso = 0, Acute = 1, Right = 2, Obtuse = 3 }

    public struct Verdict
    {
        public bool refused;   // 안쪽 라운드인데 삼각형 내부가 아니다 → 시도로 세지 않는다
        public bool ok;
        public int nearest;    // 먼저 닿은 벽(변 번호 0=BC,1=CA,2=AB) 또는 못(꼭짓점 번호)
        public int farthest;
    }

    public sealed class Board
    {
        public int round;              // 0..5, 연습 = -1
        public int band;               // 난이도 밴드 1..4
        public Kind kind;
        public Shape shape;
        public readonly IP[] V = new IP[3];          // A, B, C
        public readonly int[] ang = new int[3];      // 표시용 정수 각(°). 직각삼각형은 C=90 만 정확, 나머지 0(표시 안 함)
        public int[] lens;                           // 직각삼각형 표시 길이 BC, CA, AB (cm)
        public int tolPct;
        public bool bars, strips, angleNumbers;
        public double Ix, Iy, Ox, Oy, Gx, Gy, r, R;  // 연출용 실수(판정에 쓰지 않는다)
        public IP hit;                               // 판정을 통과하는 격자점(I·O 에 가장 가까운 점) — 테스트 훅
        public IP miss;                              // 판정에 떨어지는 격자점(무게중심 G) — 테스트 훅
        public string describe = "";                 // 화면·문제 은행 공통 도형 설명
        public string Instruction => kind == Kind.In ? "세 변에 닿도록 방울을 놓으시오" : "세 꼭짓점에 동시에 닿도록 방울을 놓으시오";
        public string CenterName => kind == Kind.In ? "내심 I" : "외심 O";
    }

    public enum LineKind { PerpBisector = 0, AngleBisector = 1 }

    public static class BangulRules
    {
        public const int Sub = 40;
        public const int HX = 168, HY = 140;      // 판 반폭·반높이(격자 칸) = 4.2 × 3.5 월드
        public const int TolIn = 28, TolOut = 16; // 허용 비율(%) — 연습·실전 공통. 봇 자가 테스트로 우연 수준을 잰다
        public const double MaxChance = 0.035;    // 판마다 정답 영역 넓이 비(우연 수준)가 이보다 크면 쓰지 않는다
        public const int Rounds = 6;
        public const int StartHearts = 3;
        public const int StartStrips = 3;
        public const float RunSeconds = 120f;
        public const int MasteryFirst = 5;
        public const double MarkRatio = 0.32;     // 각의 같은 거리 표식: 꼭짓점에서 짧은 이웃 변의 32%

        public static readonly string[] Names = { "A", "B", "C" };
        public static readonly string[] SideNames = { "BC", "CA", "AB" };

        // ───────────────────────────── 정수 기하
        public static long Cross(IP o, IP a, IP p) => (long)(a.x - o.x) * (p.y - o.y) - (long)(a.y - o.y) * (p.x - o.x);
        public static long Len2(IP a, IP b) { long dx = a.x - b.x, dy = a.y - b.y; return dx * dx + dy * dy; }
        public static IP SideFrom(Board b, int side) => b.V[(side + 1) % 3];
        public static IP SideTo(Board b, int side) => b.V[(side + 2) % 3];

        /// <summary>P 가 삼각형 내부(경계 제외)인가. 세 cross 의 부호가 모두 넓이 부호와 같다.</summary>
        public static bool Inside(Board b, IP p)
        {
            long area = Cross(b.V[0], b.V[1], b.V[2]);
            int s = Math.Sign(area);
            for (int i = 0; i < 3; i++)
            {
                long c = Cross(SideFrom(b, i), SideTo(b, i), p);
                if (Math.Sign(c) != s || c == 0) return false;
            }
            return true;
        }

        public static bool OnBoard(IP p) => p.x >= -HX && p.x <= HX && p.y >= -HY && p.y <= HY;

        /// <summary>d_i² 를 분수 num/den 으로. 안쪽 = 변까지, 바깥 = 꼭짓점까지.</summary>
        static void Dist2(Board b, IP p, int i, out decimal num, out decimal den)
        {
            if (b.kind == Kind.In)
            {
                long c = Cross(SideFrom(b, i), SideTo(b, i), p);
                num = (decimal)c * c;
                den = Len2(SideFrom(b, i), SideTo(b, i));
            }
            else
            {
                num = Len2(p, b.V[i]);
                den = 1m;
            }
        }

        public static Verdict Judge(Board b, IP p) => Judge(b, p, b.tolPct);

        public static Verdict Judge(Board b, IP p, int tolPct)
        {
            var v = new Verdict();
            if (b.kind == Kind.In && !Inside(b, p)) { v.refused = true; return v; }
            if (!OnBoard(p)) { v.refused = true; return v; }
            decimal n0, d0, n1, d1, n2, d2;
            Dist2(b, p, 0, out n0, out d0); Dist2(b, p, 1, out n1, out d1); Dist2(b, p, 2, out n2, out d2);
            decimal[] n = { n0, n1, n2 }, d = { d0, d1, d2 };
            int m = 0, M = 0;
            for (int i = 1; i < 3; i++)
            {
                if (n[i] * d[m] < n[m] * d[i]) m = i;    // d_i² < d_m²
                if (n[i] * d[M] > n[M] * d[i]) M = i;
            }
            v.nearest = m; v.farthest = M;
            decimal k = 100 + tolPct;
            // d_M² ≤ (k/100)² d_m²  ⇔  n_M · d_m · 10000 ≤ k² · n_m · d_M
            v.ok = n[M] * d[m] * 10000m <= k * k * n[m] * d[M];
            return v;
        }

        // ───────────────────────────── 실수 연출 값(판정과 무관)
        public static void Centers(Board b)
        {
            double ax = b.V[0].x, ay = b.V[0].y, bx = b.V[1].x, by = b.V[1].y, cx = b.V[2].x, cy = b.V[2].y;
            double a = Math.Sqrt(Len2(b.V[1], b.V[2])), bb = Math.Sqrt(Len2(b.V[2], b.V[0])), c = Math.Sqrt(Len2(b.V[0], b.V[1]));
            double per = a + bb + c;
            b.Ix = (a * ax + bb * bx + c * cx) / per; b.Iy = (a * ay + bb * by + c * cy) / per;
            double area2 = Math.Abs((bx - ax) * (cy - ay) - (by - ay) * (cx - ax));
            b.r = area2 / per;
            double D = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
            double a2 = ax * ax + ay * ay, b2 = bx * bx + by * by, c2 = cx * cx + cy * cy;
            b.Ox = (a2 * (by - cy) + b2 * (cy - ay) + c2 * (ay - by)) / D;
            b.Oy = (a2 * (cx - bx) + b2 * (ax - cx) + c2 * (bx - ax)) / D;
            b.R = Math.Sqrt((ax - b.Ox) * (ax - b.Ox) + (ay - b.Oy) * (ay - b.Oy));
            b.Gx = (ax + bx + cx) / 3.0; b.Gy = (ay + by + cy) / 3.0;
        }

        static IP Round(double x, double y) => new IP((int)Math.Round(x), (int)Math.Round(y));

        /// <summary>각의 같은 거리 표식 6개(꼭짓점 v 마다 이웃 변 두 개 위). 접기 띠 끝을 포개는 대상.</summary>
        public static void Mark(Board b, int target, out double x, out double y)
        {
            if (target < 3) { x = b.V[target].x; y = b.V[target].y; return; }
            int v = (target - 3) / 2, k = (target - 3) % 2;
            IP P = b.V[v], Q = b.V[(v + 1 + k) % 3];
            double la = Math.Sqrt(Len2(P, b.V[(v + 1) % 3])), lb = Math.Sqrt(Len2(P, b.V[(v + 2) % 3]));
            double t = MarkRatio * Math.Min(la, lb);
            double lq = Math.Sqrt(Len2(P, Q));
            x = P.x + (Q.x - P.x) * t / lq; y = P.y + (Q.y - P.y) * t / lq;
        }

        /// <summary>두 대상을 포갰을 때 생기는 접는 선. 꼭짓점 둘 = 그 변의 수직이등분선, 한 각의 표식 둘 = 그 각의 이등분선.
        /// 접기는 언제나 「포갠 두 점의 수직이등분선」이다 — 같은 거리 표식 둘의 수직이등분선은 꼭짓점을 지나므로 각의 이등분선이 된다.</summary>
        public static bool FoldPair(int t1, int t2, out LineKind kind, out int which)
        {
            kind = LineKind.PerpBisector; which = -1;
            if (t1 == t2 || t1 < 0 || t2 < 0) return false;
            if (t1 < 3 && t2 < 3) { kind = LineKind.PerpBisector; which = 3 - t1 - t2; return true; } // which = 대변의 번호(= 남는 꼭짓점)
            if (t1 >= 3 && t2 >= 3)
            {
                int v1 = (t1 - 3) / 2, v2 = (t2 - 3) / 2;
                if (v1 != v2) return false;
                kind = LineKind.AngleBisector; which = v1; return true;
            }
            return false;
        }

        /// <summary>접는 선의 한 점과 방향(연출용 실수).</summary>
        public static void FoldLine(Board b, LineKind kind, int which, out double px, out double py, out double dx, out double dy)
        {
            if (kind == LineKind.PerpBisector)
            {
                IP P = SideFrom(b, which), Q = SideTo(b, which);
                px = (P.x + Q.x) * .5; py = (P.y + Q.y) * .5;
                dx = -(Q.y - P.y); dy = Q.x - P.x;
            }
            else
            {
                double m0x, m0y, m1x, m1y;
                Mark(b, 3 + which * 2, out m0x, out m0y); Mark(b, 4 + which * 2, out m1x, out m1y);
                px = b.V[which].x; py = b.V[which].y;
                dx = (m0x + m1x) * .5 - px; dy = (m0y + m1y) * .5 - py;
            }
            double l = Math.Sqrt(dx * dx + dy * dy); dx /= l; dy /= l;
        }

        // ───────────────────────────── 생성기
        static readonly int[] IsoApexBand1 = { 30, 32, 34, 36, 38, 40, 42, 44, 46, 76, 78, 80, 82, 84, 86, 88 };
        static readonly int[] IsoApexBand4 = { 36, 40, 44, 48, 70, 74, 78, 80, 84, 96, 100, 104, 108, 112, 116 };
        static readonly int[][] RightTriples = { new[] { 3, 4, 5 }, new[] { 5, 12, 13 }, new[] { 8, 15, 17 }, new[] { 20, 21, 29 } };
        // 피타고라스 방향 (p,q,h): p²+q²=h² — 직각을 정수 좌표로 정확히 유지하며 회전한다
        static readonly int[][] PyDirs = { new[] { 1, 0, 1 }, new[] { 0, 1, 1 }, new[] { 3, 4, 5 }, new[] { 4, 3, 5 }, new[] { 5, 12, 13 }, new[] { 12, 5, 13 }, new[] { 8, 15, 17 }, new[] { 15, 8, 17 } };

        /// <summary>연습 판(고정값): 꼭지각 40°·밑각 70° 이등변삼각형, 밑변 가로, 안쪽 방울, 거리 막대.</summary>
        public static Board Practice()
        {
            var b = new Board { round = -1, band = 1, kind = Kind.In, shape = Shape.Iso, tolPct = TolIn, bars = true };
            b.ang[0] = 40; b.ang[1] = 70; b.ang[2] = 70;
            // 밑변 BC 가로 = 156칸(3.9), 높이 = 78·tan70° ≈ 214.3칸 → 판 높이(280)에 맞춰 0.92배
            b.V[0] = new IP(0, 112); b.V[1] = new IP(-72, -86); b.V[2] = new IP(72, -86);
            Finish(b);
            b.describe = "AB=AC, ∠A=40°인 이등변삼각형 ABC";
            return b;
        }

        public static List<Board> RunDeck(Random rng)
        {
            var deck = new List<Board>(Rounds);
            deck.Add(MakeIso(rng, 0, 1, Kind.In, IsoApexBand1, true));
            deck.Add(MakeGeneral(rng, 1, 2, Shape.Acute, Kind.Out));
            deck.Add(MakeRight(rng, 2));
            deck.Add(MakeGeneral(rng, 3, 3, Shape.Obtuse, Kind.Out));
            var b5 = MakeIso(rng, 4, 4, Kind.In, IsoApexBand4, false);
            deck.Add(b5);
            deck.Add(Twin(b5, 5, Kind.Out));
            return deck;
        }

        /// <summary>R6 = R5 와 같은 삼각형, 바깥 방울. 같은 대칭축 위에 I·O 가 함께 놓인다.</summary>
        public static Board Twin(Board src, int round, Kind kind)
        {
            var b = new Board { round = round, band = src.band, kind = kind, shape = src.shape, tolPct = kind == Kind.In ? TolIn : TolOut, strips = true, angleNumbers = true };
            for (int i = 0; i < 3; i++) { b.V[i] = src.V[i]; b.ang[i] = src.ang[i]; }
            b.describe = src.describe;
            Finish(b);
            // 쌍둥이도 바깥 판정에서 순진한 점이 떨어지는지 확인(생성 시 Validate 가 두 종류를 모두 본다)
            return b;
        }

        static Board MakeIso(Random rng, int round, int band, Kind kind, int[] apexPool, bool axisAligned)
        {
            for (int tries = 0; tries < 400; tries++)
            {
                int A = apexPool[rng.Next(apexPool.Length)];
                int base_ = (180 - A) / 2;
                double rot = axisAligned ? 90 * rng.Next(4) : RotAvoidAxes(rng);
                var b = new Board { round = round, band = band, kind = kind, shape = Shape.Iso, tolPct = kind == Kind.In ? TolIn : TolOut };
                b.ang[0] = A; b.ang[1] = base_; b.ang[2] = base_;
                b.bars = band <= 2; b.strips = band >= 3; b.angleNumbers = band >= 4;
                if (!Place(b, rng, A, base_, base_, rot, rng.Next(2) == 1, band == 4)) continue;
                b.describe = "AB=AC, ∠A=" + A + "°인 이등변삼각형 ABC";
                if (band == 4)
                {
                    // R5·R6 는 같은 삼각형을 쓰므로 바깥 판정도 통과해야 한다
                    var twin = new Board { kind = Kind.Out, tolPct = TolOut };
                    for (int i = 0; i < 3; i++) twin.V[i] = b.V[i];
                    Finish(twin);
                    if (!Validate(twin)) continue;
                }
                if (Validate(b)) return b;
            }
            var fb = Practice(); fb.round = round; fb.band = band; fb.kind = kind; fb.tolPct = kind == Kind.In ? TolIn : TolOut; Finish(fb);
            fb.bars = band <= 2; fb.strips = band >= 3; fb.angleNumbers = band >= 4;
            return fb;
        }

        static double RotAvoidAxes(Random rng)
        {
            // 꼭지가 위·아래·좌·우 정방향이 아닌 회전(밑각을 「아래쪽 두 각」으로 읽는 오개념을 시험)
            double r = 18 + rng.NextDouble() * 54;   // 18°..72°
            return r + 90 * rng.Next(4);
        }

        static Board MakeGeneral(Random rng, int round, int band, Shape shape, Kind kind)
        {
            for (int tries = 0; tries < 600; tries++)
            {
                int A, B, C;
                if (shape == Shape.Acute)
                {
                    A = 40 + rng.Next(46); B = 40 + rng.Next(46); C = 180 - A - B;
                    if (C < 40 || C > 85) continue;
                    int mx = Math.Max(A, Math.Max(B, C)), mn = Math.Min(A, Math.Min(B, C));
                    if (mx - mn < 15 || A == B || B == C || A == C) continue;
                }
                else
                {
                    int obtuse = 100 + rng.Next(23);           // 100..122 (더 크면 외심 근처 정답 영역이 길쭉하게 넓어진다)
                    int rest = 180 - obtuse;
                    int x = 18 + rng.Next(Math.Max(1, rest - 35));
                    int y = rest - x;
                    if (y < 18 || x == y) continue;
                    int at = rng.Next(3);                       // 둔각 꼭짓점을 A·B·C 중 아무 데나
                    int[] arr = new int[3]; arr[at] = obtuse; arr[(at + 1) % 3] = x; arr[(at + 2) % 3] = y;
                    A = arr[0]; B = arr[1]; C = arr[2];
                }
                var b = new Board { round = round, band = band, kind = kind, shape = shape, tolPct = kind == Kind.In ? TolIn : TolOut };
                b.ang[0] = A; b.ang[1] = B; b.ang[2] = C;
                b.bars = band <= 2; b.strips = band >= 3;
                if (!Place(b, rng, A, B, C, rng.NextDouble() * 360, rng.Next(2) == 1, false)) continue;
                b.describe = "∠A=" + A + "°, ∠B=" + B + "°, ∠C=" + C + "°인 △ABC";
                if (Validate(b)) return b;
            }
            return MakeIso(rng, round, band, kind, IsoApexBand4, false);
        }

        static Board MakeRight(Random rng, int round)
        {
            for (int tries = 0; tries < 400; tries++)
            {
                var t = RightTriples[rng.Next(RightTriples.Length)];
                var dir = PyDirs[rng.Next(PyDirs.Length)];
                int p = dir[0], q = dir[1], h = dir[2];
                // 다리 길이 legA = t0·k, legB = t1·k (k 는 h 의 배수 → 회전 후 좌표가 정수)
                int hyp = t[2];
                int kMax = (int)(230.0 / hyp);                // 빗변 ≤ 230칸(5.75)
                int kMin = (int)Math.Ceiling(160.0 / hyp);
                var ks = new List<int>();
                for (int k = kMin; k <= kMax; k++) if (k % h == 0) ks.Add(k);
                if (ks.Count == 0) continue;
                int kk = ks[rng.Next(ks.Count)];
                int la = t[0] * kk, lb = t[1] * kk;
                if (rng.Next(2) == 1) { int tmp = la; la = lb; lb = tmp; }
                int sx = rng.Next(2) == 1 ? -1 : 1, sy = rng.Next(2) == 1 ? -1 : 1;
                // C = 원점, CA = la·(p,q)/h, CB = lb·(-q,p)/h
                int ax = sx * la * p / h, ay = sy * la * q / h;
                int bx = sx * (-lb * q / h), by = sy * (lb * p / h);
                var b = new Board { round = round, band = 3, kind = Kind.Out, shape = Shape.Right, tolPct = TolOut, strips = true };
                b.V[2] = new IP(0, 0); b.V[0] = new IP(ax, ay); b.V[1] = new IP(bx, by);
                // 빗변 중점(=외심)을 판 중앙 근처로, 정수 이동
                int mx = (ax + bx) / 2, my = (ay + by) / 2;
                int jx = rng.Next(-40, 41), jy = rng.Next(-24, 25);
                for (int i = 0; i < 3; i++) b.V[i] = new IP(b.V[i].x - mx + jx, b.V[i].y - my + jy);
                b.ang[2] = 90;
                int mult = t[2] <= 5 ? 2 * (1 + rng.Next(3)) : (t[2] <= 17 ? 1 + rng.Next(2) : 1);
                b.lens = new[] { (la == t[0] * kk ? t[1] : t[0]) * mult, (la == t[0] * kk ? t[0] : t[1]) * mult, t[2] * mult };
                // lens = BC, CA, AB. CA 는 la 방향, CB 는 lb 방향
                b.lens[0] = lb / kk * mult; b.lens[1] = la / kk * mult; b.lens[2] = t[2] * mult;
                Finish(b);
                if (!FitsBoard(b, true)) continue;
                b.describe = "∠C=90°인 직각삼각형 ABC";
                if (Validate(b)) return b;
            }
            return MakeGeneral(rng, round, 3, Shape.Obtuse, Kind.Out);
        }

        /// <summary>각(정수 도)으로 실수 삼각형을 만들고 판에 맞춰 크기·위치를 정한 뒤 정수 격자로 고정한다.</summary>
        static bool Place(Board b, Random rng, int A, int B, int C, double rotDeg, bool mirror, bool fitBothCenters)
        {
            double d2r = Math.PI / 180;
            // BC = 1: B=(0,0), C=(1,0), AB = sin C / sin A
            double ab = Math.Sin(C * d2r) / Math.Sin(A * d2r);
            double[] x = { ab * Math.Cos(B * d2r), 0, 1 }, y = { ab * Math.Sin(B * d2r), 0, 0 };
            if (mirror) for (int i = 0; i < 3; i++) x[i] = -x[i];
            double gx = (x[0] + x[1] + x[2]) / 3, gy = (y[0] + y[1] + y[2]) / 3;
            double cr = Math.Cos(rotDeg * d2r), sr = Math.Sin(rotDeg * d2r);
            for (int i = 0; i < 3; i++)
            {
                double px = x[i] - gx, py = y[i] - gy;
                x[i] = px * cr - py * sr; y[i] = px * sr + py * cr;
            }
            // 맞출 상자: 안쪽 = 삼각형, 바깥 = 삼각형 ∪ 외접원
            double minX = Math.Min(x[0], Math.Min(x[1], x[2])), maxX = Math.Max(x[0], Math.Max(x[1], x[2]));
            double minY = Math.Min(y[0], Math.Min(y[1], y[2])), maxY = Math.Max(y[0], Math.Max(y[1], y[2]));
            if (b.kind == Kind.Out || fitBothCenters)
            {
                double ox, oy, rr; Circum(x, y, out ox, out oy, out rr);
                minX = Math.Min(minX, ox - rr); maxX = Math.Max(maxX, ox + rr);
                minY = Math.Min(minY, oy - rr); maxY = Math.Max(maxY, oy + rr);
            }
            const double margin = 24;
            double availX = 2 * (HX - margin), availY = 2 * (HY - margin);
            double s = Math.Min(availX / (maxX - minX), availY / (maxY - minY)) * (0.86 + rng.NextDouble() * 0.14);
            double slackX = availX - (maxX - minX) * s, slackY = availY - (maxY - minY) * s;
            double cx = -(minX + maxX) * .5 * s + (rng.NextDouble() - .5) * slackX;
            double cy = -(minY + maxY) * .5 * s + (rng.NextDouble() - .5) * slackY;
            for (int i = 0; i < 3; i++) b.V[i] = Round(x[i] * s + cx, y[i] * s + cy);
            Finish(b);
            return FitsBoard(b, b.kind == Kind.Out || fitBothCenters);
        }

        static void Circum(double[] x, double[] y, out double ox, out double oy, out double rr)
        {
            double D = 2 * (x[0] * (y[1] - y[2]) + x[1] * (y[2] - y[0]) + x[2] * (y[0] - y[1]));
            double a2 = x[0] * x[0] + y[0] * y[0], b2 = x[1] * x[1] + y[1] * y[1], c2 = x[2] * x[2] + y[2] * y[2];
            ox = (a2 * (y[1] - y[2]) + b2 * (y[2] - y[0]) + c2 * (y[0] - y[1])) / D;
            oy = (a2 * (x[2] - x[1]) + b2 * (x[0] - x[2]) + c2 * (x[1] - x[0])) / D;
            rr = Math.Sqrt((x[0] - ox) * (x[0] - ox) + (y[0] - oy) * (y[0] - oy));
        }

        static bool FitsBoard(Board b, bool circle)
        {
            for (int i = 0; i < 3; i++) if (Math.Abs(b.V[i].x) > HX - 16 || Math.Abs(b.V[i].y) > HY - 16) return false;
            if (circle && (Math.Abs(b.Ox) + b.R > HX - 4 || Math.Abs(b.Oy) + b.R > HY - 4)) return false;
            return true;
        }

        static void Finish(Board b)
        {
            Centers(b);
            b.hit = FindHit(b);
            b.miss = Round(b.Gx, b.Gy);
        }

        static IP FindHit(Board b)
        {
            double cx = b.kind == Kind.In ? b.Ix : b.Ox, cy = b.kind == Kind.In ? b.Iy : b.Oy;
            var c = Round(cx, cy);
            IP best = c; bool found = false; long bestD = long.MaxValue;
            for (int dy = -4; dy <= 4; dy++)
                for (int dx = -4; dx <= 4; dx++)
                {
                    var p = new IP(c.x + dx, c.y + dy);
                    if (!Judge(b, p, Math.Max(1, b.tolPct / 3)).ok) continue;   // 넉넉히 안쪽인 점
                    long d = (long)dx * dx + (long)dy * dy;
                    if (d < bestD) { bestD = d; best = p; found = true; }
                }
            if (!found) best = c;
            return best;
        }

        /// <summary>순진한 점(무게중심·판 중앙·삼각형 상자 중앙·반대 중심)이 허용오차 2배로도 떨어지고,
        /// 정답 격자점이 실제로 통과하는 판만 쓴다. 정삼각형 근처(O≈I≈G)는 여기서 걸러진다.</summary>
        public static bool Validate(Board b)
        {
            long area = Cross(b.V[0], b.V[1], b.V[2]);
            if (Math.Abs(area) < 4000) return false;
            if (!Judge(b, b.hit).ok) return false;
            int strict = b.tolPct * 2;
            var naive = new List<IP>(6);
            naive.Add(Round(b.Gx, b.Gy));
            naive.Add(new IP(0, 0));
            int bx0 = Math.Min(b.V[0].x, Math.Min(b.V[1].x, b.V[2].x)), bx1 = Math.Max(b.V[0].x, Math.Max(b.V[1].x, b.V[2].x));
            int by0 = Math.Min(b.V[0].y, Math.Min(b.V[1].y, b.V[2].y)), by1 = Math.Max(b.V[0].y, Math.Max(b.V[1].y, b.V[2].y));
            naive.Add(new IP((bx0 + bx1) / 2, (by0 + by1) / 2));
            naive.Add(b.kind == Kind.In ? Round(b.Ox, b.Oy) : Round(b.Ix, b.Iy));
            foreach (var p in naive)
            {
                var v = Judge(b, p, strict);
                if (!v.refused && v.ok) return false;
            }
            // 오답 훅용 점(G)은 반드시 시도로 세져야(안쪽이면 내부) 하고 떨어져야 한다
            var mv = Judge(b, b.miss);
            if (mv.refused || mv.ok) return false;
            // 우연 수준 상한(둔각이 아주 크면 외심 근처 정답 영역이 길쭉하게 넓어진다)
            if (ChanceFraction(b, 8) > MaxChance) return false;
            return true;
        }

        // ───────────────────────────── 우연 수준(조작 가능 영역 대비 정답 영역 넓이 비)
        /// <summary>안쪽 = 삼각형 내부 격자점 중 통과 비율, 바깥 = 판 전체 격자점 중 통과 비율. step 칸 간격 표본.
        /// 우연 수준 「추정」 전용(통계값)이라 실수 근사를 쓴다 — 실제 채점(Judge)은 위의 정수 판정만 쓴다.</summary>
        public static double ChanceFraction(Board b, int step)
        {
            long total = 0, pass = 0;
            double k = (100 + b.tolPct) / 100.0, k2 = k * k;
            double[] vx = { b.V[0].x, b.V[1].x, b.V[2].x }, vy = { b.V[0].y, b.V[1].y, b.V[2].y };
            double[] ex = new double[3], ey = new double[3], len2 = new double[3];
            for (int i = 0; i < 3; i++)
            {
                IP s0 = SideFrom(b, i), s1 = SideTo(b, i);
                ex[i] = s1.x - s0.x; ey[i] = s1.y - s0.y; len2[i] = ex[i] * ex[i] + ey[i] * ey[i];
            }
            var d2 = new double[3];
            for (int y = -HY; y <= HY; y += step)
                for (int x = -HX; x <= HX; x += step)
                {
                    if (b.kind == Kind.In)
                    {
                        if (!Inside(b, new IP(x, y))) continue;
                        for (int i = 0; i < 3; i++)
                        {
                            IP s0 = SideFrom(b, i);
                            double c = ex[i] * (y - s0.y) - ey[i] * (x - s0.x);
                            d2[i] = c * c / len2[i];
                        }
                    }
                    else
                        for (int i = 0; i < 3; i++) { double dx = x - vx[i], dy = y - vy[i]; d2[i] = dx * dx + dy * dy; }
                    total++;
                    double mn = Math.Min(d2[0], Math.Min(d2[1], d2[2])), mx = Math.Max(d2[0], Math.Max(d2[1], d2[2]));
                    if (mx <= k2 * mn) pass++;
                }
            return total == 0 ? 0 : (double)pass / total;
        }

        // ───────────────────────────── 오개념 판별(오답 기록 · 루루의 김서림 기록)
        public static string Misconception(Board b, IP p)
        {
            double px = p.x, py = p.y;
            double tolR = Math.Max(10, (b.kind == Kind.In ? b.r : b.R) * 0.22);
            double dO = Math.Sqrt((px - b.Ox) * (px - b.Ox) + (py - b.Oy) * (py - b.Oy));
            double dI = Math.Sqrt((px - b.Ix) * (px - b.Ix) + (py - b.Iy) * (py - b.Iy));
            double dG = Math.Sqrt((px - b.Gx) * (px - b.Gx) + (py - b.Gy) * (py - b.Gy));
            if (b.kind == Kind.In && dO < tolR) return "swap_IO";
            if (b.kind == Kind.Out && dI < tolR) return "swap_IO";
            if (dG < tolR) return "centroid_center";
            if (b.kind == Kind.Out && b.shape == Shape.Obtuse && Inside(b, p)) return "circumcenter_always_inside";
            if (b.kind == Kind.Out && b.shape == Shape.Right && Inside(b, p)) return "right_circumcenter_not_midpoint";
            return "off_center";
        }

        public static string MisconceptionLabel(string id)
        {
            switch (id)
            {
                case "swap_IO": return "내심·외심 뒤바꿈";
                case "centroid_center": return "한가운데(무게중심)에 놓음";
                case "circumcenter_always_inside": return "외심을 안쪽에 놓음";
                case "right_circumcenter_not_midpoint": return "빗변 중점을 지나침";
                default: return "한쪽 벽에 먼저 막힘";
            }
        }

        // ───────────────────────────── 문제 은행(sampleProblems) — 같은 생성 규칙·같은 각 배열에서 만든다
        public sealed class Item
        {
            public string id, prompt, answer, concept;
            public string[] choices;
            public string[] misconceptionIds;   // choices 와 같은 순서(정답 칸은 "")
            public double numeric = double.NaN;
        }

        public static List<Item> BuildBank()
        {
            var list = new List<Item>(420);
            var rng = new Random(20261011);
            int serial = 0;
            // 1) 이등변삼각형: 밑각·∠BIC·방울 중심(안쪽)
            for (int A = 24; A <= 140; A += 2)
            {
                if (A == 60 || A == 90 || A == 120) continue;
                int bs = (180 - A) / 2;
                string tri = "AB=AC이고 ∠A=" + A + "°인 이등변삼각형 ABC";
                list.Add(Num(ref serial, rng, tri + "에서 ∠B의 크기를 구하시오.", bs, "°", "이등변삼각형의 밑각",
                    new[] { A, 180 - A, A / 2 }, new[] { "apex_base_swap", "halve_missing", "bisector_half" }));
                list.Add(Num(ref serial, rng, tri + "의 내심을 I라 할 때, ∠BIC의 크기를 구하시오.", 90 + A / 2, "°", "내심과 각의 크기",
                    new[] { 2 * A, 90 + A, 180 - A }, new[] { "swap_IO_formula", "half_missing", "supplement" }));
                list.Add(Kind_(ref serial, rng, tri + "에서 " + "세 변에 닿도록 방울을 놓으시오. 방울의 중심이 되는 점은?", Kind.In));
            }
            // 2) 예각삼각형: ∠BOC·∠BIC·외심 위치
            int acuteCount = 0;
            for (int A = 40; A <= 84 && acuteCount < 70; A += 2)
                for (int B = 40; B <= 84 && acuteCount < 70; B += 6)
                {
                    int C = 180 - A - B;
                    if (C < 40 || C > 85 || A == 60 || A == B || B == C || A == C) continue;
                    int mx = Math.Max(A, Math.Max(B, C)), mn = Math.Min(A, Math.Min(B, C));
                    if (mx - mn < 15) continue;
                    string tri = "∠A=" + A + "°, ∠B=" + B + "°, ∠C=" + C + "°인 △ABC";
                    list.Add(Num(ref serial, rng, tri + "의 외심을 O라 할 때, ∠BOC의 크기를 구하시오.", 2 * A, "°", "외심과 각의 크기",
                        new[] { 90 + A / 2, A, 180 - A }, new[] { "swap_IO_formula", "half_central", "supplement" }));
                    if (A != 90)
                        list.Add(Num(ref serial, rng, tri + "의 내심을 I라 할 때, ∠BIC의 크기를 구하시오.", 90 + A / 2, "°", "내심과 각의 크기",
                            new[] { 2 * A, 90 + A, 180 - A }, new[] { "swap_IO_formula", "half_missing", "supplement" }));
                    list.Add(Where(ref serial, rng, tri + "에서 세 꼭짓점에 동시에 닿도록 방울을 놓으시오. 방울의 중심은 어디에 있는가?", Shape.Acute));
                    acuteCount++;
                }
            // 3) 둔각삼각형: 외심 위치(바깥)·∠BIC
            int obtCount = 0;
            for (int A = 100; A <= 130 && obtCount < 40; A += 2)
                for (int B = 15; B <= 40 && obtCount < 40; B += 5)
                {
                    int C = 180 - A - B;
                    if (C < 15 || C == B) continue;
                    string tri = "∠A=" + A + "°, ∠B=" + B + "°, ∠C=" + C + "°인 △ABC";
                    list.Add(Where(ref serial, rng, tri + "에서 세 꼭짓점에 동시에 닿도록 방울을 놓으시오. 방울의 중심은 어디에 있는가?", Shape.Obtuse));
                    if (A != 120)
                        list.Add(Num(ref serial, rng, tri + "의 내심을 I라 할 때, ∠BIC의 크기를 구하시오.", 90 + A / 2, "°", "내심과 각의 크기",
                            new[] { 2 * A, 90 + A, 180 - A }, new[] { "swap_IO_formula", "half_missing", "supplement" }));
                    obtCount++;
                }
            // 4) 직각삼각형: 외심까지의 거리(빗변의 절반) · 외심 위치(빗변의 중점)
            foreach (var t in RightTriples)
                for (int m = 1; m <= 4; m++)
                {
                    int a = t[0] * m, bb = t[1] * m, c = t[2] * m;
                    if (c > 60) continue;
                    string tri = "∠C=90°인 직각삼각형 ABC에서 BC=" + a + " cm, CA=" + bb + " cm, AB=" + c + " cm";
                    // 정답 c/2, 오답: 내접원의 반지름 (a+b-c)/2(내심·외심 뒤바꿈), BC/2, CA/2 — 반 단위 정수(×2)로 비교
                    list.Add(Half(ref serial, rng, tri + "이다. 외심을 O라 할 때, OC의 길이를 구하시오.", c, " cm", "직각삼각형의 외심",
                        new[] { a + bb - c, a, bb }, new[] { "swap_IO_inradius", "wrong_side_half", "wrong_side_half" }));
                    list.Add(Where(ref serial, rng, tri + "이다. 세 꼭짓점에 동시에 닿도록 방울을 놓으시오. 방울의 중심은 어디에 있는가?", Shape.Right));
                }
            return list;
        }

        static void Shuffle<T>(Random rng, T[] a, T[] b)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var t = a[i]; a[i] = a[j]; a[j] = t;
                var u = b[i]; b[i] = b[j]; b[j] = u;
            }
        }

        /// <summary>정수 답 + 오개념 역산 오답. 순서: 정답 제거 → 범위 검사 → 중복 제거 → (부족분 없음 — 전수 검사로 보장) → 셔플.</summary>
        static Item Num(ref int serial, Random rng, string prompt, int ans, string unit, string concept, int[] wrong, string[] mis)
        {
            var ch = new List<string> { ans + unit }; var ms = new List<string> { "" };
            for (int i = 0; i < wrong.Length; i++)
            {
                if (wrong[i] == ans || wrong[i] <= 0 || wrong[i] >= 360) continue;
                string s = wrong[i] + unit;
                if (ch.Contains(s)) continue;
                ch.Add(s); ms.Add(mis[i]);
            }
            var a = ch.ToArray(); var m = ms.ToArray(); Shuffle(rng, a, m);
            return new Item { id = "q" + (++serial), prompt = prompt, answer = ans + unit, concept = concept, choices = a, misconceptionIds = m, numeric = ans };
        }

        /// <summary>반 단위 답(×2 정수로 계산). 6.5 cm 같은 값.</summary>
        static Item Half(ref int serial, Random rng, string prompt, int twiceAns, string unit, string concept, int[] twiceWrong, string[] mis)
        {
            Func<int, string> f = v => (v % 2 == 0 ? (v / 2).ToString() : (v / 2) + ".5") + unit;
            var ch = new List<string> { f(twiceAns) }; var ms = new List<string> { "" };
            for (int i = 0; i < twiceWrong.Length; i++)
            {
                if (twiceWrong[i] == twiceAns || twiceWrong[i] <= 0) continue;
                string s = f(twiceWrong[i]);
                if (ch.Contains(s)) continue;
                ch.Add(s); ms.Add(mis[i]);
            }
            var a = ch.ToArray(); var m = ms.ToArray(); Shuffle(rng, a, m);
            return new Item { id = "q" + (++serial), prompt = prompt, answer = f(twiceAns), concept = concept, choices = a, misconceptionIds = m, numeric = twiceAns / 2.0 };
        }

        static Item Kind_(ref int serial, Random rng, string prompt, Kind k)
        {
            var a = new[] { "내심 I", "외심 O", "무게중심 G" };
            var m = new[] { k == Kind.In ? "" : "swap_IO", k == Kind.In ? "swap_IO" : "", "centroid_center" };
            Shuffle(rng, a, m);
            return new Item { id = "q" + (++serial), prompt = prompt, answer = k == Kind.In ? "내심 I" : "외심 O", concept = k == Kind.In ? "삼각형의 내심" : "삼각형의 외심", choices = a, misconceptionIds = m };
        }

        static Item Where(ref int serial, Random rng, string prompt, Shape s)
        {
            string ans = s == Shape.Right ? "빗변의 중점" : s == Shape.Obtuse ? "삼각형의 외부" : "삼각형의 내부";
            var a = new[] { "삼각형의 내부", "삼각형의 외부", "빗변의 중점" };
            var m = new string[3];
            for (int i = 0; i < 3; i++)
                m[i] = a[i] == ans ? "" : a[i] == "삼각형의 내부" ? "circumcenter_always_inside" : a[i] == "빗변의 중점" ? "right_midpoint_overgeneralized" : "circumcenter_outside_confusion";
            Shuffle(rng, a, m);
            return new Item { id = "q" + (++serial), prompt = prompt, answer = ans, concept = "삼각형의 외심의 위치", choices = a, misconceptionIds = m };
        }

        /// <summary>은행 전수 검사: 정답이 선택지에 있다, 중복 없음, 오개념 오답 ≥ 2, 우연 참 0.</summary>
        public static string SelfCheckBank(List<Item> bank)
        {
            int bad = 0, few = 0;
            var prompts = new HashSet<string>();
            foreach (var it in bank)
            {
                if (Array.IndexOf(it.choices, it.answer) < 0) bad++;
                if (new HashSet<string>(it.choices).Count != it.choices.Length) bad++;
                int mis = 0; foreach (var m in it.misconceptionIds) if (!string.IsNullOrEmpty(m)) mis++;
                if (mis < 2) few++;
                prompts.Add(it.prompt);
            }
            return "{\"items\":" + bank.Count + ",\"unique\":" + prompts.Count + ",\"bad\":" + bad + ",\"fewMis\":" + few + "}";
        }
    }
}
