// 볼트 풀어 — 중2 삼각형의 외심·내심 정수 기하 정본.
// 화면의 삼각형, 후보 좌표, 측정값, 판정, QA 문제은행은 모두 이 모델에서 나온다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.BoltPureo
{
    public enum CenterKind { Circumcenter, Incenter, Decoy }

    [Serializable]
    public struct GeoPoint
    {
        public int x, y;
        public GeoPoint(int x, int y) { this.x = x; this.y = y; }
        public override string ToString() => "(" + x + "," + y + ")";
    }

    [Serializable]
    public sealed class TriangleGeometry
    {
        public string id = "";
        public string displayName = "";
        public string[] labels = new string[3];
        public GeoPoint[] vertices = new GeoPoint[3];
        public GeoPoint circumcenter;
        public GeoPoint incenter;
        public int rightVertex = -1;
        // 렌더링 전용 배율. 판정에는 쓰지 않는다.
        public int renderMilli;
    }

    [Serializable]
    public sealed class BoltProblem
    {
        public string id = "";
        public string prompt = "";
        public string concept = "";
        public string triangle = "";
        public string geometryId = "";
        public CenterKind target;
        public int band;
        public int rotation;
    }

    [Serializable]
    public sealed class BoltCandidate
    {
        public CenterKind kind;
        public CenterKind evidenceMode;
        public int x, y;
        public int evidenceA, evidenceB, evidenceC;
        public string misconceptionId = "";
        public string evidenceTitle = "";
        public string evidenceValues = "";
        public bool matches;
        public GeoPoint Point => new GeoPoint(x, y);
    }

    public static class BoltPureoRules
    {
        public const int Goal = 6;
        public const int RequiredFirstCorrect = 5;
        public const int StartLives = 4; // 네 번째 오답에서 3칸 트레이가 막혀 종료된다.
        public const float SessionSeconds = 90f;

        // 모든 변의 길이가 정수이고 외심·내심 좌표도 정수인 삼각형만 사용한다.
        // 따라서 실제 판정은 거리제곱과 정수 외적만으로 끝난다.
        static readonly TriangleGeometry[] Geometries = {
            G("right-abc", "직각삼각형 ABC", "ABC",
                P(-4,-3), P(4,-3), P(-4,3), P(0,0), P(-2,-1), 0, 550),
            G("acute-def", "예각삼각형 DEF", "DEF",
                P(-56,-48), P(56,-48), P(16,48), P(0,-15), P(8,-16), -1, 38),
            G("isosceles-ghi", "이등변삼각형 GHI", "GHI",
                P(-24,-16), P(24,-16), P(0,16), P(0,-9), P(0,-4), -1, 90),
            G("obtuse-jkl", "둔각삼각형 JKL", "JKL",
                P(-24,-18), P(24,-18), P(0,0), P(0,-25), P(0,-10), -1, 90),
            G("rotated-mno", "회전된 이등변삼각형 MNO", "MNO",
                P(16,-24), P(16,24), P(-16,0), P(9,0), P(4,0), -1, 90),
            G("right-pqr", "직각삼각형 PQR", "PQR",
                P(3,-4), P(3,4), P(-3,-4), P(0,0), P(1,-2), 0, 550)
        };

        static GeoPoint P(int x, int y) => new GeoPoint(x, y);

        static TriangleGeometry G(string id, string name, string letters,
            GeoPoint a, GeoPoint b, GeoPoint c, GeoPoint circum, GeoPoint inner,
            int rightVertex, int renderMilli)
        {
            return new TriangleGeometry {
                id = id, displayName = name,
                labels = new[] { letters.Substring(0,1), letters.Substring(1,1), letters.Substring(2,1) },
                vertices = new[] { a, b, c }, circumcenter = circum, incenter = inner,
                rightVertex = rightVertex, renderMilli = renderMilli
            };
        }

        public static TriangleGeometry GeometryFor(BoltProblem p)
        {
            for (int i = 0; i < Geometries.Length; i++) if (Geometries[i].id == p.geometryId) return Geometries[i];
            return Geometries[0];
        }

        public static BoltProblem Practice()
        {
            return Problem("bp-practice", 0, CenterKind.Circumcenter, 1, 0,
                "빗변의 중점에 있는 외심 볼트를 판별하시오.");
        }

        public static BoltProblem RunProblem(int serial, int index)
        {
            int i = Math.Max(0, Math.Min(Goal - 1, index));
            int[] geometryOrder = { 5, 1, 2, 0, 4, 3 };
            CenterKind[] targets = {
                CenterKind.Circumcenter, CenterKind.Circumcenter, CenterKind.Incenter,
                CenterKind.Incenter, CenterKind.Incenter, CenterKind.Circumcenter
            };
            int band = i < 2 ? 1 : (i < 4 ? 2 : 3);
            int geometryIndex = geometryOrder[(i + Positive(serial * 5)) % geometryOrder.Length];
            if (i == 4) geometryIndex = 4;
            if (i == 5) geometryIndex = 3;
            CenterKind target = targets[i];
            string ask = target == CenterKind.Circumcenter
                ? "세 꼭짓점까지의 거리가 같은 외심 볼트를 판별하시오."
                : "세 변까지의 수선 거리가 같은 내심 볼트를 판별하시오.";
            return Problem("bp-run-" + serial + "-" + (i + 1), geometryIndex, target, band,
                Positive(serial * 37 + i * 61) % 360, ask);
        }

        static BoltProblem Problem(string id, int geometryIndex, CenterKind target, int band, int rotation, string ask)
        {
            var g = Geometries[geometryIndex];
            return new BoltProblem {
                id = id, prompt = g.displayName + "에서 " + ask,
                concept = target == CenterKind.Circumcenter ? "삼각형의 외심" : "삼각형의 내심",
                triangle = g.displayName, geometryId = g.id, target = target, band = band, rotation = rotation
            };
        }

        public static BoltCandidate[] Candidates(BoltProblem p, int serial, int round)
        {
            var g = GeometryFor(p);
            var points = CandidatePoints(g, p.target);
            var raw = new BoltCandidate[12];
            for (int i = 0; i < raw.Length; i++)
            {
                CenterKind evidenceMode;
                if (Same(points[i], g.circumcenter)) evidenceMode = CenterKind.Circumcenter;
                else if (Same(points[i], g.incenter)) evidenceMode = CenterKind.Incenter;
                else evidenceMode = (i % 2 == 0) ? p.target : Opposite(p.target);
                raw[i] = MakeCandidate(p, g, points[i], evidenceMode);
            }

            int seed = Positive(serial * 7919 + round * 104729 + p.rotation * 17 + 23);
            for (int i = raw.Length - 1; i > 0; i--)
            {
                seed = Xor(seed);
                int j = seed % (i + 1);
                var t = raw[i]; raw[i] = raw[j]; raw[j] = t;
            }
            // 정답 위치는 판마다 12칸 전체에 결정적으로 분산한다. 단순 주기식은
            // 1→12 순환 입력과 위상이 맞아 200판에서 우연선(1/12)을 한 번 넘겼다.
            int desired = serial == 0 ? 5 : Xor(serial * 11 + round * 104729 + 55) % 12;
            ArrangeForInput(raw, desired);
            return raw;
        }

        static void ArrangeForInput(BoltCandidate[] values, int desiredCorrect)
        {
            int correct = -1;
            for (int i = 0; i < values.Length; i++) if (values[i].matches) { correct = i; break; }
            if (correct < 0) return;
            Swap(values, correct, desiredCorrect);
            int first = (desiredCorrect / 4) * 4;
            var fixedSlots = new HashSet<int> { desiredCorrect };
            for (int slot = first; slot < first + 4; slot++)
            {
                if (slot == desiredCorrect) continue;
                int best = -1; long bestDistance = -1;
                for (int j = 0; j < values.Length; j++)
                {
                    if (fixedSlots.Contains(j) || j == desiredCorrect || values[j].matches) continue;
                    long dx = values[j].x - values[desiredCorrect].x;
                    long dy = values[j].y - values[desiredCorrect].y;
                    long d = dx * dx + dy * dy;
                    if (d > bestDistance) { bestDistance = d; best = j; }
                }
                if (best >= 0) Swap(values, slot, best);
                fixedSlots.Add(slot);
            }
        }

        static void Swap(BoltCandidate[] values, int a, int b)
        {
            if (a == b) return;
            var t = values[a]; values[a] = values[b]; values[b] = t;
        }

        static List<GeoPoint> CandidatePoints(TriangleGeometry g, CenterKind target)
        {
            var result = new List<GeoPoint>(12);
            var seen = new HashSet<string>();
            GeoPoint correct = target == CenterKind.Circumcenter ? g.circumcenter : g.incenter;
            GeoPoint opposite = target == CenterKind.Circumcenter ? g.incenter : g.circumcenter;
            Add(result, seen, correct); Add(result, seen, opposite);
            Add(result, seen, g.vertices[0]); Add(result, seen, g.vertices[1]); Add(result, seen, g.vertices[2]);
            Add(result, seen, Mid(g.vertices[0], g.vertices[1]));
            Add(result, seen, Mid(g.vertices[1], g.vertices[2]));
            Add(result, seen, Mid(g.vertices[2], g.vertices[0]));
            Add(result, seen, P((g.vertices[0].x + g.vertices[1].x + g.vertices[2].x) / 3,
                (g.vertices[0].y + g.vertices[1].y + g.vertices[2].y) / 3));
            int minX = Math.Min(g.vertices[0].x, Math.Min(g.vertices[1].x, g.vertices[2].x));
            int maxX = Math.Max(g.vertices[0].x, Math.Max(g.vertices[1].x, g.vertices[2].x));
            int minY = Math.Min(g.vertices[0].y, Math.Min(g.vertices[1].y, g.vertices[2].y));
            int maxY = Math.Max(g.vertices[0].y, Math.Max(g.vertices[1].y, g.vertices[2].y));
            Add(result, seen, P((minX + maxX) / 2, (minY + maxY) / 2));
            int nudge = Math.Max(1, 650 / g.renderMilli);
            Add(result, seen, P(correct.x + nudge, correct.y));
            Add(result, seen, P(opposite.x, opposite.y + nudge));
            for (int ring = 1; result.Count < 12 && ring < 8; ring++)
            {
                Add(result, seen, P(correct.x + nudge * ring, correct.y + nudge));
                Add(result, seen, P(correct.x - nudge * ring, correct.y - nudge));
            }
            return result;
        }

        static void Add(List<GeoPoint> list, HashSet<string> seen, GeoPoint p)
        {
            string key = p.x + "," + p.y;
            if (seen.Add(key) && list.Count < 12) list.Add(p);
        }

        static GeoPoint Mid(GeoPoint a, GeoPoint b) => P((a.x + b.x) / 2, (a.y + b.y) / 2);
        static CenterKind Opposite(CenterKind k) => k == CenterKind.Circumcenter ? CenterKind.Incenter : CenterKind.Circumcenter;

        static BoltCandidate MakeCandidate(BoltProblem p, TriangleGeometry g, GeoPoint point, CenterKind evidenceMode)
        {
            bool circum = IsCircumcenter(g, point);
            bool inner = IsIncenter(g, point);
            CenterKind kind = circum ? CenterKind.Circumcenter : (inner ? CenterKind.Incenter : CenterKind.Decoy);
            int a, b, c;
            string title;
            if (evidenceMode == CenterKind.Circumcenter)
            {
                a = DistanceSquared(point, g.vertices[0]);
                b = DistanceSquared(point, g.vertices[1]);
                c = DistanceSquared(point, g.vertices[2]);
                title = "세 꼭짓점까지의 거리²";
            }
            else
            {
                PerpendicularTicks(g, point, out a, out b, out c);
                title = "세 변까지의 수선 길이의 비";
            }
            bool property = evidenceMode == CenterKind.Circumcenter ? circum : inner;
            bool matches = evidenceMode == p.target && property;
            string misconception;
            if (kind == Opposite(p.target)) misconception = "swap_circum_incenter";
            else if (p.geometryId == "obtuse-jkl" && p.target == CenterKind.Circumcenter && PointInside(g, point))
                misconception = "circumcenter_always_inside";
            else if (g.rightVertex >= 0 && Same(point, g.vertices[g.rightVertex]) && p.target == CenterKind.Circumcenter)
                misconception = "right_vertex_as_circumcenter";
            else if (p.geometryId == "rotated-mno") misconception = "rotated_base_midpoint";
            else misconception = "unequal_distances_ignored";
            return new BoltCandidate {
                kind = kind, evidenceMode = evidenceMode, x = point.x, y = point.y,
                evidenceA = a, evidenceB = b, evidenceC = c,
                evidenceTitle = title,
                evidenceValues = evidenceMode == CenterKind.Circumcenter ? a + " · " + b + " · " + c : a + " : " + b + " : " + c,
                misconceptionId = matches ? "" : misconception, matches = matches
            };
        }

        public static bool IsCircumcenter(TriangleGeometry g, GeoPoint p)
        {
            int a = DistanceSquared(p, g.vertices[0]);
            return a == DistanceSquared(p, g.vertices[1]) && a == DistanceSquared(p, g.vertices[2]);
        }

        public static bool IsIncenter(TriangleGeometry g, GeoPoint p)
        {
            if (!PointInside(g, p)) return false;
            long c0 = Math.Abs(Cross(g.vertices[0], g.vertices[1], p));
            long c1 = Math.Abs(Cross(g.vertices[1], g.vertices[2], p));
            long c2 = Math.Abs(Cross(g.vertices[2], g.vertices[0], p));
            long l0 = DistanceSquared(g.vertices[0], g.vertices[1]);
            long l1 = DistanceSquared(g.vertices[1], g.vertices[2]);
            long l2 = DistanceSquared(g.vertices[2], g.vertices[0]);
            return c0 * c0 * l1 == c1 * c1 * l0 && c1 * c1 * l2 == c2 * c2 * l1;
        }

        static bool PointInside(TriangleGeometry g, GeoPoint p)
        {
            long a = Cross(g.vertices[0], g.vertices[1], p);
            long b = Cross(g.vertices[1], g.vertices[2], p);
            long c = Cross(g.vertices[2], g.vertices[0], p);
            bool pos = a > 0 && b > 0 && c > 0;
            bool neg = a < 0 && b < 0 && c < 0;
            return pos || neg;
        }

        static long Cross(GeoPoint a, GeoPoint b, GeoPoint p)
            => (long)(b.x - a.x) * (p.y - a.y) - (long)(b.y - a.y) * (p.x - a.x);

        static int DistanceSquared(GeoPoint a, GeoPoint b)
        {
            int dx = a.x - b.x, dy = a.y - b.y;
            return dx * dx + dy * dy;
        }

        static void PerpendicularTicks(TriangleGeometry g, GeoPoint p, out int a, out int b, out int c)
        {
            int l0 = IntegerSqrt(DistanceSquared(g.vertices[0], g.vertices[1]));
            int l1 = IntegerSqrt(DistanceSquared(g.vertices[1], g.vertices[2]));
            int l2 = IntegerSqrt(DistanceSquared(g.vertices[2], g.vertices[0]));
            int common = Lcm(l0, Lcm(l1, l2));
            a = (int)Math.Abs(Cross(g.vertices[0], g.vertices[1], p)) * (common / l0);
            b = (int)Math.Abs(Cross(g.vertices[1], g.vertices[2], p)) * (common / l1);
            c = (int)Math.Abs(Cross(g.vertices[2], g.vertices[0], p)) * (common / l2);
            int reduce = Gcd(a, Gcd(b, c));
            if (reduce > 1) { a /= reduce; b /= reduce; c /= reduce; }
        }

        static int IntegerSqrt(int n)
        {
            int r = (int)Math.Sqrt(n);
            while ((r + 1) * (r + 1) <= n) r++;
            while (r * r > n) r--;
            return r;
        }
        static int Gcd(int a, int b) { a = Math.Abs(a); b = Math.Abs(b); while (b != 0) { int t = a % b; a = b; b = t; } return a; }
        static int Lcm(int a, int b) => a / Gcd(a, b) * b;
        static bool Same(GeoPoint a, GeoPoint b) => a.x == b.x && a.y == b.y;

        public static List<MgfProblem> BuildBank()
        {
            // 실제 플레이와 같은 RunProblem→Candidates 경로에서 30라운드×12후보를 뽑는다.
            var list = new List<MgfProblem>(360);
            int bankId = 1;
            for (int serial = 1; serial <= 5; serial++)
            for (int round = 0; round < Goal; round++)
            {
                var p = RunProblem(serial, round);
                var g = GeometryFor(p);
                var candidates = Candidates(p, serial, round);
                for (int i = 0; i < candidates.Length; i++, bankId++)
                {
                    var c = candidates[i];
                    string label = p.target == CenterKind.Circumcenter ? "외심" : "내심";
                    // 후보 기호는 모든 삼각형 꼭짓점 기호와 겹치지 않는 X로 고정한다.
                    // 특히 △PQR에서 후보를 P라 부르면 서로 다른 두 좌표의 P가 생겨
                    // 판정 대상이 모호해진다.
                    string coordinates = "△" + string.Join("", g.labels) + " " +
                        g.labels[0] + g.vertices[0] + ", " + g.labels[1] + g.vertices[1] + ", " +
                        g.labels[2] + g.vertices[2] + ", 후보 X" + c.Point;
                    string prompt = "검사 " + bankId.ToString("000") + ". " + coordinates + "에서 " +
                        c.evidenceTitle + "가 " + c.evidenceValues + "이다. X가 " + label + " 볼트인지 판별하시오.";
                    list.Add(new MgfProblem {
                        id = "bp-bank-" + bankId, prompt = prompt, choices = null,
                        answer = c.matches ? "맞음" : "아님", answerNumeric = c.matches ? 1 : 0,
                        unitConcept = p.concept
                    });
                }
            }
            return list;
        }

        public static bool ValidateAll(out string error)
        {
            for (int i = 0; i < Geometries.Length; i++)
            {
                var g = Geometries[i];
                if (!IsCircumcenter(g, g.circumcenter)) { error = g.id + " 외심 좌표 오류"; return false; }
                if (!IsIncenter(g, g.incenter)) { error = g.id + " 내심 좌표 오류"; return false; }
                for (int s = 0; s < 3; s++)
                {
                    int d2 = DistanceSquared(g.vertices[s], g.vertices[(s + 1) % 3]);
                    int d = IntegerSqrt(d2);
                    if (d * d != d2) { error = g.id + " 변 길이 비정수"; return false; }
                }
                if (g.id == "obtuse-jkl" && PointInside(g, g.circumcenter))
                { error = "둔각삼각형 외심이 내부"; return false; }
                if (g.rightVertex >= 0)
                {
                    int r = g.rightVertex, u = (r + 1) % 3, v = (r + 2) % 3;
                    GeoPoint midpoint = Mid(g.vertices[u], g.vertices[v]);
                    if (!Same(midpoint, g.circumcenter)) { error = g.id + " 빗변 중점 불일치"; return false; }
                }
            }

            for (int serial = 0; serial <= 240; serial++)
            for (int round = 0; round < Goal; round++)
            {
                var p = serial == 0 ? Practice() : RunProblem(serial, round);
                var g = GeometryFor(p);
                var c = Candidates(p, serial, round);
                int correct = 0;
                var positions = new HashSet<string>();
                for (int i = 0; i < c.Length; i++)
                {
                    bool exact = c[i].evidenceMode == p.target &&
                        (p.target == CenterKind.Circumcenter ? IsCircumcenter(g, c[i].Point) : IsIncenter(g, c[i].Point));
                    if (c[i].matches != exact) { error = "판정 경로 불일치 " + serial + "/" + round + "/" + i; return false; }
                    if (c[i].matches) correct++;
                    if (!positions.Add(c[i].x + "," + c[i].y)) { error = "후보 좌표 중복"; return false; }
                    if (!c[i].matches && string.IsNullOrEmpty(c[i].misconceptionId)) { error = "오개념 id 누락"; return false; }
                }
                if (correct != 1) { error = "정답 후보 수 " + correct + " @" + serial + "/" + round; return false; }
                if (serial == 0) break;
            }

            var bank = BuildBank();
            if (bank.Count < 300) { error = "문항 은행 300개 미만"; return false; }
            var ids = new HashSet<string>(); var prompts = new HashSet<string>();
            for (int i = 0; i < bank.Count; i++)
            {
                var p = bank[i];
                if (!ids.Add(p.id) || !prompts.Add(p.prompt)) { error = "중복 문항 " + p.id; return false; }
                if (p.prompt.Contains("√") || p.prompt.Contains("가정") || p.prompt.Contains("결론"))
                { error = "중2 금지 표현 " + p.id; return false; }
                if (!p.prompt.Contains("후보 X") || !p.prompt.Contains("X가 "))
                { error = "후보 기호 계약 위반 " + p.id; return false; }
            }
            error = ""; return true;
        }

        static int Positive(int x) { return x == int.MinValue ? int.MaxValue : Math.Abs(x); }
        static int Xor(int x)
        {
            uint u = (uint)x;
            u ^= u << 13; u ^= u >> 17; u ^= u << 5;
            return (int)(u & 0x7fffffff);
        }
    }
}
