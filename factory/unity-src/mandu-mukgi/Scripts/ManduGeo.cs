// 만두 묶기 — 평면 수학 레이어용 메시 빌더. 선·호·고리·점선을 정점색 메시 하나에 모은다(드로콜 절약).
// 모든 좌표는 반죽 판의 로컬 평면(x 오른쪽, y 위, z 는 카메라 쪽이 음수)이다. 정사영 카메라가 판을 정면으로
// 보므로 화면에서 길이·각이 왜곡되지 않는다(2.5D 원근을 수학 측정에 섞지 않는다).
using System.Collections.Generic;
using UnityEngine;

namespace Mgf.ManduMukgi
{
    public sealed class MeshBuf
    {
        readonly List<Vector3> v = new List<Vector3>(512);
        readonly List<Color> c = new List<Color>(512);
        readonly List<int> t = new List<int>(1024);
        readonly List<Vector2> uv = new List<Vector2>(512);
        public float z;

        public void Clear() { v.Clear(); c.Clear(); t.Clear(); uv.Clear(); }

        public void Apply(Mesh m)
        {
            m.Clear();
            m.SetVertices(v);
            m.SetColors(c);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
        }

        // 카메라(-z) 쪽에서 보이도록 감기 방향을 맞춘다.
        void Tri(int a, int b, int d)
        {
            var n = Vector3.Cross(v[b] - v[a], v[d] - v[a]);
            if (n.z > 0) { t.Add(a); t.Add(d); t.Add(b); }
            else { t.Add(a); t.Add(b); t.Add(d); }
        }

        int Add(Vector2 p, Color col)
        {
            v.Add(new Vector3(p.x, p.y, z)); c.Add(col); uv.Add(new Vector2(0.5f, 0.5f));
            return v.Count - 1;
        }

        public void Quad(Vector2 a, Vector2 b, Vector2 d, Vector2 e, Color col)
        {
            int i0 = Add(a, col), i1 = Add(b, col), i2 = Add(d, col), i3 = Add(e, col);
            Tri(i0, i1, i2); Tri(i0, i2, i3);
        }

        public void Segment(Vector2 a, Vector2 b, float w, Color col)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1e-8f) return;
            var n = new Vector2(-d.y, d.x).normalized * (w * 0.5f);
            Quad(a + n, b + n, b - n, a - n, col);
        }

        public void Disc(Vector2 center, float r, Color col, int seg = 20)
        {
            int ci = Add(center, col);
            int first = -1, prev = -1;
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                int idx = i == seg ? first : Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, col);
                if (i == 0) first = idx;
                else Tri(ci, prev, idx);
                prev = idx;
            }
        }

        /// <summary>원호 띠(a0→a1 라디안).</summary>
        public void Arc(Vector2 center, float r, float w, float a0, float a1, Color col, int seg = 16)
        {
            float rin = r - w * 0.5f, rout = r + w * 0.5f;
            for (int i = 0; i < seg; i++)
            {
                float u0 = Mathf.Lerp(a0, a1, (float)i / seg), u1 = Mathf.Lerp(a0, a1, (float)(i + 1) / seg);
                var d0 = new Vector2(Mathf.Cos(u0), Mathf.Sin(u0)); var d1 = new Vector2(Mathf.Cos(u1), Mathf.Sin(u1));
                Quad(center + d0 * rin, center + d0 * rout, center + d1 * rout, center + d1 * rin, col);
            }
        }

        public void Ring(Vector2 center, float r, float w, Color col, int seg = 28, bool dashed = false, float phase = 0f)
        {
            if (!dashed) { Arc(center, r, w, 0f, Mathf.PI * 2f, col, seg); return; }
            int dashes = 10;
            for (int i = 0; i < dashes; i++)
            {
                float a0 = (i + phase) * Mathf.PI * 2f / dashes;
                Arc(center, r, w, a0, a0 + Mathf.PI * 2f / dashes * 0.55f, col, 3);
            }
        }

        /// <summary>점선(dash 길이·gap 길이, phase 로 흐르게).</summary>
        public void Dashed(Vector2 a, Vector2 b, float w, float dash, float gap, Color col, float phase = 0f)
        {
            float len = Vector2.Distance(a, b);
            if (len < 1e-4f) return;
            var dir = (b - a) / len;
            float period = dash + gap;
            float s = -((phase % 1f + 1f) % 1f) * period;
            for (; s < len; s += period)
            {
                float s0 = Mathf.Max(0f, s), s1 = Mathf.Min(len, s + dash);
                if (s1 > s0) Segment(a + dir * s0, a + dir * s1, w, col);
            }
        }

        public void Polyline(List<Vector2> pts, float w, Color col, bool joints = true)
        {
            for (int i = 1; i < pts.Count; i++)
            {
                Segment(pts[i - 1], pts[i], w, col);
                if (joints) Disc(pts[i], w * 0.5f, col, 8);
            }
            if (joints && pts.Count > 0) Disc(pts[0], w * 0.5f, col, 8);
        }

        /// <summary>화살촉(끝 b 를 가리킴).</summary>
        public void Arrow(Vector2 a, Vector2 b, float size, Color col)
        {
            var d = (b - a).normalized; var n = new Vector2(-d.y, d.x);
            int i0 = Add(b, col), i1 = Add(b - d * size + n * size * 0.6f, col), i2 = Add(b - d * size - n * size * 0.6f, col);
            Tri(i0, i1, i2);
        }
    }

    /// <summary>부풀어 오른 반죽 조각(다각형 돌출 + 경사 테두리). 앞면 z=-depth.</summary>
    public static class DoughMesh
    {
        static readonly List<Vector3> V = new List<Vector3>(64);
        static readonly List<Vector2> UV = new List<Vector2>(64);
        static readonly List<int> T = new List<int>(192);

        public static void Build(Mesh m, Vector2[] poly, float depth, float inset, float uvScale = 0.35f)
        {
            V.Clear(); UV.Clear(); T.Clear();
            int n = poly.Length;
            var cen = Vector2.zero; for (int i = 0; i < n; i++) cen += poly[i]; cen /= n;
            // 안쪽 고리(앞면) — 꼭짓점 쪽으로 inset 만큼 들인다.
            var inner = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                var dir = cen - poly[i];
                float l = dir.magnitude;
                inner[i] = poly[i] + (l > 1e-4f ? dir / l * Mathf.Min(inset, l * 0.45f) : Vector2.zero);
            }
            int front = 0;
            V.Add(new Vector3(cen.x, cen.y, -depth)); UV.Add(cen * uvScale);
            for (int i = 0; i < n; i++) { V.Add(new Vector3(inner[i].x, inner[i].y, -depth)); UV.Add(inner[i] * uvScale); }
            for (int i = 0; i < n; i++) AddTri(front, 1 + i, 1 + (i + 1) % n);
            int ring = V.Count;
            for (int i = 0; i < n; i++) { V.Add(new Vector3(inner[i].x, inner[i].y, -depth)); UV.Add(inner[i] * uvScale); }
            for (int i = 0; i < n; i++) { V.Add(new Vector3(poly[i].x, poly[i].y, -depth * 0.25f)); UV.Add(poly[i] * uvScale); }
            for (int i = 0; i < n; i++)
            {
                int a = ring + i, b = ring + (i + 1) % n, c = ring + n + (i + 1) % n, d = ring + n + i;
                AddTri(a, b, c); AddTri(a, c, d);
            }
            m.Clear();
            m.SetVertices(V); m.SetUVs(0, UV); m.SetTriangles(T, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
        }

        static void AddTri(int a, int b, int c)
        {
            var nrm = Vector3.Cross(V[b] - V[a], V[c] - V[a]);
            // 앞면·경사면 모두 카메라(-z) 쪽을 보게 한다.
            if (nrm.z > 0) { T.Add(a); T.Add(c); T.Add(b); } else { T.Add(a); T.Add(b); T.Add(c); }
        }
    }
}
