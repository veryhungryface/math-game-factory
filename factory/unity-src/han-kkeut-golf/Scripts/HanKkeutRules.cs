// 한 끗 골프 — 문제 생성·정수 판정·오개념 분류. UnityEngine 에 의존하지 않는다(헤드리스 검산·봇이 같은 파일을 쓴다).
//
// 표현 함정(curriculum/2022-middle-math.json style_guide·expression_traps 에서 옮김):
//  - 발문은 중학교 지시형(「~하시오」)으로 통일한다. 상시 목표 배너만 짧은 서술형.
//  - 각은 기준선(O를 지나는 수평선)의 오른쪽 반직선에서 시계 반대 방향으로 잰다. OA 는 위쪽 반원(0°~179°),
//    레일(직선)의 방향은 0°~179° 로 나타낸다(직선이므로 180° 차이는 같은 방향).
//  - 정확한 접선은 「접점을 지나는 반지름에 수직」 하나뿐이다. ±2° 는 손가락 조작 오차일 뿐 접선의 정의가 아니다 —
//    화면과 발문에서 「각도 오차 2° 이내 허용」으로만 말하고 정답 표기는 정확한 k° 로 한다.
//  - 이 학습 범위에는 현의 길이·두 접선의 길이·내접원·√ 가 나오지 않는다.
//  - 오답 대표값이 정답 허용범위와 겹치는 파라미터에서는 그 오개념으로 진단하지 않는다(우연 참 0건 — 헤드리스 검산).
using System;
using System.Collections.Generic;
using System.Text;

namespace Mgf.HanKkeutGolf
{
    public sealed class GolfProblem
    {
        public string id;
        public int band;      // 1 표시된 OA · 2 OA 직접 긋기 · 3 잘못된 보조선 OB 고치기
        public int theta;     // OA 와 기준 반직선이 이루는 각 0..179
        public int radius;    // 반지름 r cm 4..10 (표시용 길이)
        public int phi = -1;  // 3단계 보조선 OB 의 각 0..359 (없으면 -1)
        public int answer;    // 정확한 레일 방향 k = (θ+90) mod 180
        public string prompt;

        public Mgf.MgfProblem ToMgf()
        {
            return new Mgf.MgfProblem
            {
                id = id,
                prompt = prompt,
                choices = null,
                answer = answer + "°",
                answerNumeric = answer,
                unitConcept = "원의 접선은 접점을 지나는 반지름에 수직이다"
            };
        }
    }

    public static class HanKkeutRules
    {
        public const int Tolerance = 2;          // 조작 오차 허용(정수 각도 차)
        public const int Courses = 8;
        public const int MasteryNeeded = 7;      // 첫 제출 7/8
        public const int StartBalls = 3;
        public const int MaxFirstMisses = 1;     // 서로 다른 코스의 두 번째 첫 제출 실패에서 종료
        public const float SessionSeconds = 90f;
        public const int DiagnoseWindow = 4;     // 오개념 대표값 ±4° 안이면 그 오개념으로 진단

        public const string MisParallel = "tangent-parallel-to-radius";
        public const string MisMirror = "mirror-angle-across-baseline";
        public const string MisOtherRadius = "perpendicular-to-other-radius";
        public const string MisOff = "not-perpendicular";

        public static int Tangent(int theta) => (theta + 90) % 180;

        /// <summary>두 직선 방향(0..179)의 각도 차 0..90.</summary>
        public static int LineDiff(int a, int b)
        {
            int d = Math.Abs(((a % 180) + 180) % 180 - ((b % 180) + 180) % 180);
            return Math.Min(d, 180 - d);
        }

        /// <summary>두 반직선 방향(0..359)의 각도 차 0..180.</summary>
        public static int RayDiff(int a, int b)
        {
            int d = Math.Abs(((a % 360) + 360) % 360 - ((b % 360) + 360) % 360);
            return Math.Min(d, 360 - d);
        }

        public static int Norm180(int a) => ((a % 180) + 180) % 180;

        public static bool IsCorrect(GolfProblem p, int railAngle) => LineDiff(railAngle, p.answer) <= Tolerance;

        /// <summary>오개념 ① 접선이 반지름과 평행 — wrong = θ.</summary>
        public static int ParallelValue(GolfProblem p) => p.theta;
        /// <summary>오개념 ② 기준선 반대쪽으로 같은 각 — wrong = (180−θ) mod 180. 정답과 겹치면 -1.</summary>
        public static int MirrorValue(GolfProblem p)
        {
            int m = (180 - p.theta) % 180;
            return LineDiff(m, p.answer) <= Tolerance + DiagnoseWindow ? -1 : m;
        }
        /// <summary>오개념 ③ 아무 반지름(OB)에 수직 — wrong = (φ+90) mod 180. 3단계만.</summary>
        public static int OtherRadiusValue(GolfProblem p) => p.phi < 0 ? -1 : (p.phi + 90) % 180;

        public static string ClassifyMisconception(GolfProblem p, int railAngle)
        {
            if (IsCorrect(p, railAngle)) return "";
            if (LineDiff(railAngle, ParallelValue(p)) <= DiagnoseWindow) return MisParallel;
            int other = OtherRadiusValue(p);
            if (other >= 0 && LineDiff(railAngle, other) <= DiagnoseWindow) return MisOtherRadius;
            int mirror = MirrorValue(p);
            if (mirror >= 0 && LineDiff(railAngle, mirror) <= DiagnoseWindow) return MisMirror;
            return MisOff;
        }

        /// <summary>정답 이유 식: 35°+90°=125° / 127°+90°−180°=37°.</summary>
        public static string ReasonLine(GolfProblem p)
        {
            int s = p.theta + 90;
            return s < 180 ? p.theta + "°+90°=" + s + "°" : p.theta + "°+90°−180°=" + (s - 180) + "°";
        }

        // ───────── 문제 문장(화면 발문과 같은 데이터 모델) ─────────
        public static string Prompt(int band, int theta, int radius, int phi)
        {
            var sb = new StringBuilder(160);
            sb.Append("반지름의 길이가 ").Append(radius).Append(" cm인 원 O 위의 점 A에 대하여, OA가 O를 지나는 기준선의 오른쪽 반직선과 이루는 각은 ")
              .Append(theta).Append("°이다. ");
            if (band == 1) sb.Append("A에서 원 O에 접하도록 레일을 놓으시오.");
            else if (band == 2) sb.Append("O에서 A까지 반지름을 직접 긋고, A에서 원 O에 접하도록 레일을 놓으시오.");
            else sb.Append("보조선은 O를 지나는 기준선의 오른쪽 반직선에서 시계 반대 방향으로 잰 방향각이 ").Append(phi).Append("°인 반지름 OB로 잘못 그어져 있다. 보조선을 OA로 고치고, A에서 원 O에 접하도록 레일을 놓으시오.");
            sb.Append(" 레일의 방향은 기준 반직선에서 시계 반대 방향으로 0°~179°로 잰다.");
            return sb.ToString();
        }

        static GolfProblem Make(string id, int band, int theta, int radius, int phi)
        {
            return new GolfProblem
            {
                id = id, band = band, theta = theta, radius = radius, phi = band == 3 ? phi : -1,
                answer = Tangent(theta), prompt = Prompt(band, theta, radius, band == 3 ? phi : -1)
            };
        }

        /// <summary>3단계 보조선 각 φ 후보: B 가 A 와 최소 20° 떨어지고, OB 가 OA 와 같은 직선이 아니어서
        /// (φ+90) 이 정답 허용범위와 겹치지 않는 각(제약 만족 풀).</summary>
        public static List<int> PhiCandidates(int theta)
        {
            var list = new List<int>(300);
            for (int f = 0; f < 360; f++)
                if (RayDiff(f, theta) >= 20 && LineDiff(f, theta) >= 20) list.Add(f);
            return list;
        }

        static uint Hash(uint x)
        {
            x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16;
            return x;
        }

        /// <summary>문제 은행: 3단계 × θ 180 = 540 문항(반지름·φ 는 결정적 해시). 실제 판(덱)과 같은 Make/Prompt 를 쓴다.</summary>
        public static List<GolfProblem> BuildProblems()
        {
            var list = new List<GolfProblem>(540);
            for (int band = 1; band <= 3; band++)
                for (int theta = 0; theta < 180; theta++)
                {
                    uint h = Hash((uint)(band * 7919 + theta * 104729 + 17));
                    int radius = 4 + (int)(h % 7u);
                    int phi = -1;
                    if (band == 3) { var c = PhiCandidates(theta); phi = c[(int)((h >> 8) % (uint)c.Count)]; }
                    list.Add(Make("b" + band + "-t" + theta, band, theta, radius, phi));
                }
            return list;
        }

        /// <summary>무료 연습(고정): θ=30°, 정답 120°, 시작 65°. 실전 집계에 넣지 않는다.</summary>
        public static GolfProblem Practice() => Make("practice", 1, 30, 6, -1);
        public const int PracticeStart = 65;

        public static int BandOf(int courseIndex) => courseIndex < 2 ? 1 : courseIndex < 5 ? 2 : 3;

        /// <summary>한 판 8코스. θ 는 판 안에서 겹치지 않게 균등, 반지름·φ·시작 레일 각은 정답과 독립.</summary>
        public static List<GolfProblem> BuildDeck(int seed, out int[] startAngles)
        {
            var rng = new Random((int)(Hash((uint)seed * 2654435761u) & 0x7FFFFFFF));
            var deck = new List<GolfProblem>(Courses);
            startAngles = new int[Courses];
            var used = new HashSet<int>();
            for (int i = 0; i < Courses; i++)
            {
                int band = BandOf(i);
                int theta;
                do theta = rng.Next(0, 180); while (!used.Add(theta));
                int radius = rng.Next(4, 11);
                int phi = -1;
                if (band == 3) { var c = PhiCandidates(theta); phi = c[rng.Next(c.Count)]; }
                var p = Make("c" + (i + 1) + "-b" + band + "-t" + theta, band, theta, radius, phi);
                deck.Add(p);
                // 시작 레일은 정답과 독립인 균등값(0..179). 어떤 고정 회전 정책도 기대 정답률이 5/180 을 넘지 못한다.
                // 우연히 허용범위 안에서 시작해도 자동 제출·자동 진도는 없다(학생의 4px 이상 실제 드래그 후에만 판정).
                startAngles[i] = rng.Next(0, 180);
            }
            return deck;
        }

        // ───────── 기하(표시·애니메이션 전용, 판정은 위의 정수 비교) ─────────
        /// <summary>레일 시작 끝 S=A−L·d 에서 출발한 공이 원 안으로 처음 들어가는 거리 t(0..2L). 접선이면 -1.</summary>
        public static double FirstEntry(double ax, double az, double ox, double oz, double r, int railAngle, double halfLen)
        {
            double rad = railAngle * Math.PI / 180.0;
            double dx = Math.Cos(rad), dz = Math.Sin(rad);
            double sx = ax - halfLen * dx, sz = az - halfLen * dz;
            double wx = sx - ox, wz = sz - oz;
            double b = wx * dx + wz * dz;
            double c = wx * wx + wz * wz - r * r;
            double disc = b * b - c;
            if (disc <= 1e-9) return -1;
            double t1 = -b - Math.Sqrt(disc);
            if (t1 < 0 || t1 > 2 * halfLen) return -1;
            return t1;
        }
    }
}
