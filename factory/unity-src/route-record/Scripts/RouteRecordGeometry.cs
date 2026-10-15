using System.Collections.Generic;
using UnityEngine;

namespace Mgf.RouteRecord
{
    // 음반 위 홈·노치·경로의 기하. 모든 좌표는 음반 로컬(XZ 평면, 각도 0° = +X, 반시계 증가).
    // 펄스는 바깥 홈(R0)의 진입점에서 출발해 홈을 따라 진행하다가 노치의 브리지를 건너 안쪽 홈으로 넘어가고,
    // 마지막 홈 끝의 리드아웃으로 라벨(출구)에 닿는다. 한 면의 링은 각도 구간을 순서대로 나눠 쓰므로
    // 모든 경로는 되돌아가지 않고(사이클 없음) 두 면은 출구 전까지 만나지 않는다(합류 없음).
    sealed class RecordLayout
    {
        public const float RecordRadius = 4.3f;
        public const float LabelRadius = 1.25f;
        public const float OuterGroove = 3.95f;
        public const float EntryDeg = 60f;   // 톤암(뒤쪽 오른편 축)의 바늘이 닿는 바깥 홈 진입점
        const float ArcStep = 3f;

        public RouteProblem problem;
        public readonly int[] sideGaps = new int[2];
        public readonly int[] sideDir = new int[2];
        public readonly float[] exitDeg = new float[2];
        public readonly float[][] sideRadii = { new float[4], new float[4] };
        public readonly int[] gapInSide = new int[3];
        public readonly float[] gapOuter = new float[3];
        public readonly float[] gapInner = new float[3];
        public readonly float[] gapStartDeg = new float[3];
        public readonly float[] gapEndDeg = new float[3];
        public readonly float[,] slotDeg = new float[3, RouteRecordRules.MaxSlots];

        public void Build(RouteProblem p)
        {
            problem = p;
            sideGaps[0] = sideGaps[1] = 0;
            for (int g = 0; g < p.groupCount; g++) gapInSide[g] = sideGaps[p.side[g]]++;
            bool sided = p.sideCount == 2;
            for (int s = 0; s < p.sideCount; s++)
            {
                int n = sideGaps[s];
                float d = n >= 3 ? .72f : n == 2 ? .8f : .85f;
                for (int k = 0; k <= 3; k++) sideRadii[s][k] = OuterGroove - Mathf.Min(k, n) * d;
                sideDir[s] = sided ? (s == 0 ? 1 : -1) : -1;
                float off = sided ? 8f : 10f, usable = sided ? 166f : 330f;
                float seg = usable / n;
                exitDeg[s] = EntryDeg + sideDir[s] * (off + usable + 2f);
                for (int g = 0; g < p.groupCount; g++)
                {
                    if (p.side[g] != s) continue;
                    int k = gapInSide[g];
                    gapOuter[g] = sideRadii[s][k];
                    gapInner[g] = sideRadii[s][k + 1];
                    gapStartDeg[g] = EntryDeg + sideDir[s] * (off + k * seg);
                    gapEndDeg[g] = EntryDeg + sideDir[s] * (off + (k + 1) * seg);
                    int m = p.maxima[g];
                    for (int j = 0; j < RouteRecordRules.MaxSlots; j++)
                        slotDeg[g, j] = j < m ? EntryDeg + sideDir[s] * (off + k * seg + (j + .5f) * seg / m) : 0f;
                }
            }
        }

        public static Vector3 Polar(float r, float deg, float y)
        {
            float a = deg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
        }

        public Vector3 SlotCenter(int g, int j, float y) { return Polar((gapOuter[g] + gapInner[g]) * .5f, slotDeg[g, j], y); }
        public float GapLength(int g) { return gapOuter[g] - gapInner[g]; }
        public float LastRadius(int s) { return sideRadii[s][sideGaps[s]]; }

        // 한 경로(면 s, 그 면의 링마다 고른 노치 번호)를 따라가는 점열. pts 는 호출자가 재사용한다.
        public void PathPoints(int s, int[] slotPerGap, float y, List<Vector3> pts)
        {
            pts.Clear();
            int dir = sideDir[s];
            float cur = EntryDeg;
            pts.Add(Polar(OuterGroove, cur, y));
            int k = 0;
            for (int g = 0; g < problem.groupCount; g++)
            {
                if (problem.side[g] != s) continue;
                float target = slotDeg[g, slotPerGap[k]];
                Arc(sideRadii[s][k], cur, target, dir, y, pts);
                pts.Add(Polar(sideRadii[s][k + 1], target, y));
                cur = target; k++;
            }
            Arc(sideRadii[s][k], cur, exitDeg[s], dir, y, pts);
            pts.Add(Polar(LabelRadius + .08f, exitDeg[s], y));
        }

        static void Arc(float r, float from, float to, int dir, float y, List<Vector3> pts)
        {
            float span = (to - from) * dir;
            if (span <= 0f) return;
            int steps = Mathf.Max(1, Mathf.CeilToInt(span / ArcStep));
            for (int i = 1; i <= steps; i++) pts.Add(Polar(r, from + dir * span * i / steps, y));
        }

        // 화면에서 누른 점(음반 로컬)과 가장 가까운 노치. maxDist 밖이면 -1.
        public bool NearestSlot(Vector3 local, float maxDist, out int group, out int slot)
        {
            group = -1; slot = -1;
            if (problem == null) return false;
            float best = maxDist * maxDist;
            for (int g = 0; g < problem.groupCount; g++)
            for (int j = 0; j < problem.maxima[g]; j++)
            {
                Vector3 c = SlotCenter(g, j, local.y);
                float d = (c - local).sqrMagnitude;
                if (d < best) { best = d; group = g; slot = j; }
            }
            return group >= 0;
        }
    }
}
