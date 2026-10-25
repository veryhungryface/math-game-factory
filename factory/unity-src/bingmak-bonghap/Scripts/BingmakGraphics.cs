using Mgf;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.BingmakBonghap
{
    static class IceUiDraw
    {
        public static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.up);
            vh.AddVert(c, color, Vector2.one); vh.AddVert(d, color, Vector2.right);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        public static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = b - a; if (d.sqrMagnitude < .001f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * width * .5f;
            Quad(vh, a - n, a + n, b + n, b - n, color);
        }

        public static void Dashed(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color, float dash = 10f, float gap = 7f, float offset = 0)
        {
            Vector2 d = b - a; float len = d.magnitude; if (len < .01f) return; d /= len;
            float start = -Mathf.Repeat(offset, dash + gap);
            for (float x = start; x < len; x += dash + gap)
            {
                float x0 = Mathf.Max(0, x), x1 = Mathf.Min(len, x + dash);
                if (x1 > x0) Line(vh, a + d * x0, a + d * x1, width, color);
            }
        }

        public static void Disc(VertexHelper vh, Vector2 c, float rx, float ry, Color color, int seg = 32)
        {
            int start = vh.currentVertCount; vh.AddVert(c, color, new Vector2(.5f, .5f));
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                vh.AddVert(c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry), color,
                    new Vector2(Mathf.Cos(a) * .5f + .5f, Mathf.Sin(a) * .5f + .5f));
            }
            for (int i = 0; i < seg; i++) vh.AddTriangle(start, start + i + 1, start + i + 2);
        }

        public static void Ring(VertexHelper vh, Vector2 c, float rx, float ry, float width, Color color, int seg = 32)
        {
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg, b = (i + 1) * Mathf.PI * 2f / seg;
                Line(vh, c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry),
                    c + new Vector2(Mathf.Cos(b) * rx, Mathf.Sin(b) * ry), width, color);
            }
        }

        public static void Arrow(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Line(vh, a, b, width, color);
            Vector2 d = (b - a).normalized, n = new Vector2(-d.y, d.x);
            Line(vh, b, b - d * 15f + n * 8f, width, color);
            Line(vh, b, b - d * 15f - n * 8f, width, color);
        }
    }

    public sealed class ReferenceTriangleGraphic : MaskableGraphic
    {
        public float Pulse;

        public Vector2 Vertex(int i)
        {
            Rect r = rectTransform.rect;
            if (i == 0) return new Vector2(r.xMin + r.width * .17f, r.yMin + r.height * .18f);
            if (i == 1) return new Vector2(r.xMax - r.width * .14f, r.yMin + r.height * .27f);
            return new Vector2(r.xMin + r.width * .57f, r.yMax - r.height * .16f);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Color ink = MgfLook.Hex("#153F58"), pale = new Color(.86f, .98f, .98f, .92f);
            Color orange = MgfLook.Hex("#F28B35");
            Vector2 a = Vertex(0), b = Vertex(1), c = Vertex(2);
            int s = vh.currentVertCount;
            vh.AddVert(a, new Color(pale.r, pale.g, pale.b, .25f), Vector2.zero);
            vh.AddVert(b, new Color(pale.r, pale.g, pale.b, .25f), Vector2.right);
            vh.AddVert(c, new Color(pale.r, pale.g, pale.b, .25f), Vector2.up);
            vh.AddTriangle(s, s + 1, s + 2);
            IceUiDraw.Line(vh, a, b, 5f, ink); IceUiDraw.Line(vh, b, c, 5f, ink); IceUiDraw.Line(vh, c, a, 5f, ink);
            IceUiDraw.Disc(vh, a, 7, 7, orange); IceUiDraw.Disc(vh, b, 7, 7, orange); IceUiDraw.Disc(vh, c, 7, 7, orange);
            IceUiDraw.Ring(vh, c, 13 + Mathf.Sin(Pulse * 2f) * 1.4f, 13 + Mathf.Sin(Pulse * 2f) * 1.4f, 2f,
                new Color(1f, .67f, .28f, .55f));
        }

        public void Refresh() => SetVerticesDirty();
    }

    public sealed class MembraneGraphic : MaskableGraphic
    {
        public IceProblem Problem;
        public Vector2 CurrentF;
        public Vector2 WrongF;
        public bool HasWrong;
        public bool PracticeGuide;
        public bool Dragging;
        public float Pulse;
        public float GuideScale = 1f;
        public float LockProgress;
        public float CrackClose;
        public float Flash;
        public float WrongPulse;
        public float RippleClock = 9f;
        public Vector2 RippleModel;
        public readonly Vector2[] Trail = new Vector2[14];
        public int TrailCount;

        Vector2 TransformModel(Vector2 p, bool inverse)
        {
            if (Problem == null) return p;
            float a = Problem.displayRotation * Mathf.Deg2Rad;
            if (inverse) a = -a;
            float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            if (!inverse)
            {
                if (Problem.displayMirror) p.x = -p.x;
                return new Vector2(p.x * cs - p.y * sn, p.x * sn + p.y * cs);
            }
            Vector2 q = new Vector2(p.x * cs - p.y * sn, p.x * sn + p.y * cs);
            if (Problem.displayMirror) q.x = -q.x;
            return q;
        }

        float Scale
        {
            get
            {
                Rect r = rectTransform.rect;
                return Mathf.Min((r.width - 30f) / 15.2f, (r.height - 30f) / 13.2f);
            }
        }

        public Vector2 ModelToLocal(Vector2 p)
        {
            Rect r = rectTransform.rect; Vector2 center = r.center;
            Vector2 q = p - new Vector2(5f, 4.5f);
            q = TransformModel(q, false);
            return center + q * Scale;
        }

        public Vector2 LocalToModel(Vector2 p)
        {
            Rect r = rectTransform.rect;
            Vector2 q = (p - r.center) / Mathf.Max(.001f, Scale);
            q = TransformModel(q, true);
            return q + new Vector2(5f, 4.5f);
        }

        public bool ScreenToModel(Vector2 screen, out Vector2 model)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, null, out var local))
            {
                model = LocalToModel(local); return true;
            }
            model = default; return false;
        }

        public Vector2 ModelToCanvas(Vector2 p, RectTransform canvas)
        {
            Vector3 world = rectTransform.TransformPoint(ModelToLocal(p));
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out var local);
            return local;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (Problem == null) return;
            Color ink = MgfLook.Hex("#D5E8EA"), graphite = MgfLook.Hex("#222A33"), orange = MgfLook.Hex("#F3A33B");
            Color aqua = MgfLook.Hex("#86C7CF"), cream = MgfLook.Hex("#F5E8C8"), blue = MgfLook.Hex("#6EC6D2");
            Rect r = rectTransform.rect;

            // 0.5 cm grid. It is measurement evidence, not an answer-color guide.
            for (float x = BingmakRules.DomainMinX; x <= BingmakRules.DomainMaxX + .01f; x += .5f)
            {
                Color c = new Color(ink.r, ink.g, ink.b, Mathf.Abs(x - Mathf.Round(x)) < .01f ? .17f : .075f);
                IceUiDraw.Line(vh, ModelToLocal(new Vector2(x, 0)), ModelToLocal(new Vector2(x, 10)), 1.1f, c);
            }
            for (float y = 0; y <= 10.01f; y += .5f)
            {
                Color c = new Color(ink.r, ink.g, ink.b, Mathf.Abs(y - Mathf.Round(y)) < .01f ? .17f : .075f);
                IceUiDraw.Line(vh, ModelToLocal(new Vector2(-1, y)), ModelToLocal(new Vector2(11, y)), 1.1f, c);
            }

            Vector2 d = ModelToLocal(Vector2.zero), e = ModelToLocal(new Vector2(Problem.baseLength, 0)), f = ModelToLocal(CurrentF);
            Vector2 centroid = (d + e + f) / 3f;
            int tri = vh.currentVertCount;
            Color membrane = Color.Lerp(new Color(.31f, .39f, .43f, .96f), new Color(orange.r, orange.g, orange.b, .82f), Flash * .58f);
            vh.AddVert(d, membrane, Vector2.zero); vh.AddVert(e, membrane, Vector2.right); vh.AddVert(f, membrane, Vector2.up);
            vh.AddTriangle(tri, tri + 1, tri + 2);

            // Bounded drag trail: a compressed-rubber afterimage, never an unbounded particle list.
            for (int i = 1; i < TrailCount; i++)
            {
                float k = (float)i / TrailCount;
                Color tc = new Color(orange.r, orange.g, orange.b, .04f + .20f * k);
                IceUiDraw.Line(vh, ModelToLocal(Trail[i - 1]), ModelToLocal(Trail[i]), 2f + 3f * k, tc);
            }

            Color outline = Color.Lerp(graphite, cream, Flash * .7f);
            IceUiDraw.Line(vh, d, e, 9f, outline); IceUiDraw.Line(vh, e, f, 9f, outline); IceUiDraw.Line(vh, f, d, 9f, outline);
            IceUiDraw.Line(vh, d, e, 3f, orange); IceUiDraw.Line(vh, e, f, 3f, orange); IceUiDraw.Line(vh, f, d, 3f, orange);

            Vector2[] ribA = { d, e, f }, ribB = { (d + e) * .5f, (e + f) * .5f, (f + d) * .5f };
            for (int i = 0; i < 3; i++)
            {
                float threshold = i + 1;
                bool locked = LockProgress >= threshold;
                Color rc = locked ? cream : new Color(graphite.r, graphite.g, graphite.b, .55f);
                IceUiDraw.Line(vh, ribA[i], centroid, locked ? 8f : 5f, rc);
                IceUiDraw.Ring(vh, ribA[i], locked ? 13f : 10f, locked ? 13f : 10f, locked ? 4f : 2f, rc, 22);
            }

            if (HasWrong)
            {
                Vector2 wf = ModelToLocal(WrongF);
                Color wc = new Color(graphite.r, graphite.g, graphite.b, .30f + .25f * WrongPulse);
                IceUiDraw.Dashed(vh, d, wf, 4f, wc, 9, 6, Pulse * 10);
                IceUiDraw.Dashed(vh, e, wf, 4f, wc, 9, 6, Pulse * 10);
                IceUiDraw.Ring(vh, wf, 16 + WrongPulse * 4, 11, 3f, wc, 24);
            }

            // 정답 때 빙벽의 균열선이 피톤 쪽으로 접히고, 불투명 보강선이 남는다.
            Vector2[] crackOrigins = {
                ModelToLocal(new Vector2(-.8f, 8.8f)), ModelToLocal(new Vector2(10.8f, 9.1f)), ModelToLocal(new Vector2(5.1f, 10f))
            };
            Vector2[] crackEnds = { d, e, f };
            for (int i = 0; i < 3; i++)
            {
                Vector2 end = Vector2.Lerp(crackEnds[i], crackOrigins[i], CrackClose);
                IceUiDraw.Line(vh, crackOrigins[i], end, 3.5f, new Color(.20f, .48f, .62f, .48f));
                if (CrackClose > .94f) IceUiDraw.Ring(vh, crackEnds[i], 18, 18, 2f, new Color(blue.r, blue.g, blue.b, .45f), 20);
            }

            if (PracticeGuide)
            {
                Vector2 target = ModelToLocal(new Vector2(Problem.targetX, Problem.targetY));
                float ring = (26f + 6f * Mathf.Sin(Pulse * 4f)) * GuideScale;
                IceUiDraw.Ring(vh, f, ring, ring, 4f * GuideScale, new Color(1f, .96f, .72f, .9f), 32);
                IceUiDraw.Dashed(vh, f, target, 4f, new Color(1f, .96f, .72f, .82f), 11, 8, Pulse * 23f);
                IceUiDraw.Arrow(vh, Vector2.Lerp(f, target, .76f), target, 4f, new Color(1f, .96f, .72f, .95f));
                IceUiDraw.Ring(vh, target, 18, 18, 3f, new Color(1f, .96f, .72f, .72f), 28);
            }

            if (RippleClock < .55f)
            {
                float k = RippleClock / .55f, rr = Mathf.Lerp(5, 34, k);
                Color rc = new Color(aqua.r, aqua.g, aqua.b, (1f - k) * .85f);
                Vector2 rp = ModelToLocal(RippleModel);
                IceUiDraw.Ring(vh, rp, rr, rr, 3f, rc, 28);
                IceUiDraw.Arrow(vh, rp, f, 2.4f, new Color(aqua.r, aqua.g, aqua.b, (1f - k) * .65f));
            }

            IceUiDraw.Disc(vh, d, 15, 15, graphite); IceUiDraw.Disc(vh, e, 15, 15, graphite);
            IceUiDraw.Ring(vh, d, 22, 22, 5f, new Color(.75f, .93f, .91f, .75f), 28);
            IceUiDraw.Ring(vh, e, 22, 22, 5f, new Color(.75f, .93f, .91f, .75f), 28);
            float sx = Dragging ? 1.16f : 1f, sy = Dragging ? .86f : 1f;
            IceUiDraw.Disc(vh, f, 20 * sx, 20 * sy, graphite);
            IceUiDraw.Ring(vh, f, 29 * sx, 29 * sy, 6f, orange, 32);
            IceUiDraw.Ring(vh, f, (34 + Mathf.Sin(Pulse * 3f) * 2f) * sx, (34 + Mathf.Sin(Pulse * 3f) * 2f) * sy,
                2f, new Color(cream.r, cream.g, cream.b, .48f), 32);
        }

        public void Refresh() => SetVerticesDirty();
    }

    public sealed class LensGraphic : MaskableGraphic
    {
        public float Pulse;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r = rectTransform.rect; Vector2 c = r.center;
            Color ink = MgfLook.Hex("#163F58"), orange = MgfLook.Hex("#F28B35"), cream = MgfLook.Hex("#F4F1DF");
            IceUiDraw.Disc(vh, c, r.width * .49f, r.height * .49f, new Color(.91f, .99f, .98f, .94f), 40);
            for (int i = -2; i <= 2; i++)
            {
                IceUiDraw.Line(vh, c + new Vector2(i * 14, -r.height * .38f), c + new Vector2(i * 14, r.height * .38f), 1.4f, new Color(ink.r, ink.g, ink.b, .20f));
                IceUiDraw.Line(vh, c + new Vector2(-r.width * .38f, i * 14), c + new Vector2(r.width * .38f, i * 14), 1.4f, new Color(ink.r, ink.g, ink.b, .20f));
            }
            IceUiDraw.Line(vh, c + Vector2.left * 27, c + Vector2.right * 27, 3f, ink);
            IceUiDraw.Line(vh, c + Vector2.down * 27, c + Vector2.up * 27, 3f, ink);
            IceUiDraw.Ring(vh, c, 18 + Mathf.Sin(Pulse * 5f) * 1.4f, 18 + Mathf.Sin(Pulse * 5f) * 1.4f, 5f, orange, 28);
            IceUiDraw.Ring(vh, c, r.width * .48f, r.height * .48f, 5f, cream, 40);
        }
        public void Refresh() => SetVerticesDirty();
    }
}
