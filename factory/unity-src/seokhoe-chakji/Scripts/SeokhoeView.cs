// 석회 착지 — 월드. 한낮 콘크리트 운동장 삼각 측량 + 1 cm 석회 칸 + 나무 측량 쐐기.
// 우레탄 트랙·노란 육상 기구 없음(곧게 걸쳐와 계열 분리). 최빈 픽셀은 #C4B8A8 콘크리트.
// 정답: 석회 스탬프 + cm 스프링. 오답: 줄이 번지고 노란 카드. 화면 흔들림·파티클 버스트 없음.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.SeokhoeChakji
{
    class Ink
    {
        public readonly Mesh mesh = new Mesh();
        readonly List<Vector3> v = new List<Vector3>(1024);
        readonly List<Color32> c = new List<Color32>(1024);
        readonly List<int> t = new List<int>(2048);
        public float y;
        public Ink(float y) { this.y = y; mesh.MarkDynamic(); }
        public void Clear() { v.Clear(); c.Clear(); t.Clear(); }
        Vector3 P(Vector2 p) => new Vector3(p.x, y, p.y);
        public void Seg(Vector2 a, Vector2 b, float w, Color32 col)
        {
            var d = b - a; float L = d.magnitude; if (L < 1e-4f) return;
            var n = new Vector2(-d.y, d.x) / L * (w * 0.5f);
            int i = v.Count;
            v.Add(P(a - n)); v.Add(P(a + n)); v.Add(P(b + n)); v.Add(P(b - n));
            c.Add(col); c.Add(col); c.Add(col); c.Add(col);
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }
        public void Tri(Vector2 a, Vector2 b, Vector2 d, Color32 col)
        {
            int i = v.Count;
            v.Add(P(a)); v.Add(P(b)); v.Add(P(d)); c.Add(col); c.Add(col); c.Add(col);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }
        public void Disc(Vector2 o, float r, Color32 col, int n = 16)
        {
            int i0 = v.Count; v.Add(P(o)); c.Add(col);
            for (int k = 0; k <= n; k++) { float a = k * Mathf.PI * 2 / n; v.Add(P(o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r)); c.Add(col); }
            for (int k = 0; k < n; k++) { t.Add(i0); t.Add(i0 + 1 + k); t.Add(i0 + 2 + k); }
        }
        public void Dashed(Vector2 a, Vector2 b, float dash, float gap, float w, Color32 col, float phase)
        {
            var d = b - a; float L = d.magnitude; if (L < 1e-3f) return; d /= L;
            float per = dash + gap, s = -((phase % per) + per) % per;
            for (; s < L; s += per) { float s0 = Mathf.Max(0, s), s1 = Mathf.Min(L, s + dash); if (s1 > s0) Seg(a + d * s0, a + d * s1, w, col); }
        }
        public void Chevron(Vector2 at, Vector2 dir, float size, float w, Color32 col)
        {
            dir.Normalize(); var n = new Vector2(-dir.y, dir.x);
            Seg(at - dir * size * 0.5f + n * size * 0.55f, at + dir * size * 0.5f, w, col);
            Seg(at - dir * size * 0.5f - n * size * 0.55f, at + dir * size * 0.5f, w, col);
        }
        public void Apply()
        {
            mesh.Clear();
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(80, 20, 80));
        }
    }

    public partial class SeokhoeChakjiGame
    {
        static readonly Color Conc = Hex("C4B8A8"), Cream = Hex("F6F1E8"), Yel = Hex("F0C93A"), Wood = Hex("A56B32"), DirtC = Hex("6E5340"), InkC = Hex("1C1C1C"), Steel = Hex("5C6570");
        static Color Hex(string h) => MgfLook.Hex(h);
        static Color32 C32(Color c, float a = 1f) { var l = c.linear; return new Color32((byte)(l.r * 255), (byte)(l.g * 255), (byte)(l.b * 255), (byte)(Mathf.Clamp01(a) * 255)); }

        Camera cam;
        Transform world, blockT, ghostTform, pulseGo, playCol, fingerT, triPad;
        readonly Transform[] dust = new Transform[6];
        readonly Transform[] splitPiece = new Transform[6];
        MeshFilter tickF, stampF, guideF, smearF;
        Ink ticks, stampInk, guideInk, smearInk;
        Material matUre, matGrass, matBlock, matInk, matAdd;
        Texture2D ureTex;
        TextMeshPro[] vLbl = new TextMeshPro[8];
        TextMeshPro[] cmLbl = new TextMeshPro[12];
        int inkBudget;
        bool stampOn, smearOn;
        TextMeshPro ratioLbl, liveLbl, logoW, stampCm, hopHint, acSide;
        Vector3 A, B, C, D, E, Gpt, midD;
        Vector3 blockBaseScale;
        Vector3 camHome, lookHome;
        float ghostAlpha;
        bool demo;

        Vector2 A2, B2, C2;

        void BuildWorld()
        {
            MgfLook.Quality(18f);
            QualitySettings.antiAliasing = MgfBridge.LowGfx ? 0 : 2;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.pixelLightCount = 1;
            MgfLook.Sky(Hex("B9C6CE"), Hex("E7DCC8"), Hex("C4B8A8"), 0.92f);
            var sun = MgfLook.Sun(new Vector3(52f, -34f, 8), Hex("FFF1D6"), 1.05f, 0.40f);
            sun.shadows = LightShadows.Hard;
            cam = MgfLook.Camera(new Vector3(0, 13.2f, -9.4f), new Vector3(0, 0.2f, 0.8f), 34f);
            cam.allowMSAA = !MgfBridge.LowGfx;
            camHome = cam.transform.position; lookHome = new Vector3(0, 0.2f, 0.8f);

            ureTex = BakeConcrete();
            matUre = MgfLook.Lit(Conc, 0.18f, 0.02f, Hex("6A5E52"), ureTex);
            matGrass = MgfLook.Lit(DirtC, 0.20f, 0.0f, Hex("2A1C12"));
            var woodTex = BakeWood();
            matBlock = MgfLook.Lit(Wood, 0.38f, 0.06f, Hex("4A2A10"), woodTex);
            matInk = MgfLook.Alpha(Color.white);
            matAdd = MgfLook.Additive(Cream);

            world = new GameObject("World").transform;
            NoShadow(MgfLook.Block("Track", new Vector3(0, -0.22f, 0.4f), new Vector3(22, 0.4f, 16), 0.04f, matUre, world));
            var jointMat = MgfLook.Lit(Hex("8A7C6E"), 0.12f, 0.0f, Hex("3A322C"));
            for (int i = -1; i <= 1; i++)
            {
                NoShadow(MgfLook.Block("JointZ" + i, new Vector3(i * 5.4f, -0.01f, 0.2f), new Vector3(0.08f, 0.06f, 16), 0.01f, jointMat, world));
                NoShadow(MgfLook.Block("JointX" + i, new Vector3(0, -0.01f, i * 4.8f), new Vector3(18, 0.06f, 0.08f), 0.01f, jointMat, world));
            }
            var steelMat = MgfLook.Lit(Steel, 0.45f, 0.25f, Hex("1A2026"));
            for (int i = 0; i < 4; i++)
                NoShadow(MgfLook.Block("Grate" + i, new Vector3(-9.2f, 0.02f, -1.6f + i * 1.2f), new Vector3(1.4f, 0.05f, 0.12f), 0.01f, steelMat, world));
            NoShadow(MgfLook.Block("GrateRail", new Vector3(-9.2f, 0.04f, 0.4f), new Vector3(0.1f, 0.08f, 5.2f), 0.01f, steelMat, world));
            var bleachMat = MgfLook.Lit(Hex("B0A494"), 0.16f, 0.02f, Hex("5A5048"));
            for (int i = 0; i < 2; i++)
                NoShadow(MgfLook.Block("Bleach" + i, new Vector3(0, 0.15f + i * 0.42f, 8.2f + i * 0.55f), new Vector3(12 - i * 1.2f, 0.28f, 0.7f), 0.04f, bleachMat, world));

            triPad = MakeTriPad();
            var triMr = triPad.GetComponent<MeshRenderer>();
            if (triMr) triMr.sharedMaterial = MgfLook.Lit(Hex("B9A78C"), 0.2f, 0.03f, Hex("5A4C40"), ureTex);
            MgfLook.Block("Infield", new Vector3(0, -0.18f, -6.2f), new Vector3(16, 0.22f, 5.2f), 0.05f, matGrass, world);

            blockT = MakeWedge(world);
            blockBaseScale = blockT.localScale;
            ghostTform = MgfLook.Block("BlockGhost", Vector3.zero, new Vector3(1.1f, 0.22f, 0.55f), 0.04f, MgfLook.Alpha(new Color(1, 1, 1, 0.32f)), world).transform;
            ghostTform.gameObject.SetActive(false);

            ticks = new Ink(0.06f); stampInk = new Ink(0.08f); guideInk = new Ink(0.07f); smearInk = new Ink(0.07f);
            tickF = MakeMr("Ticks", ticks.mesh, matInk, world);
            stampF = MakeMr("Stamp", stampInk.mesh, matInk, world);
            guideF = MakeMr("Guide", guideInk.mesh, matInk, world);
            smearF = MakeMr("Smear", smearInk.mesh, matInk, world);

            for (int i = 0; i < dust.Length; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Dust" + i;
                Object.Destroy(q.GetComponent<Collider>());
                q.GetComponent<Renderer>().sharedMaterial = matAdd;
                q.transform.SetParent(world, false);
                q.transform.localScale = Vector3.one * (0.28f + (i % 3) * 0.1f);
                q.SetActive(false);
                dust[i] = q.transform;
            }

            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject("Split" + i);
                go.transform.SetParent(world, false);
                var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = new Mesh();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = MgfLook.Alpha(new Color(Cream.r, Cream.g, Cream.b, 0.85f));
                mr.shadowCastingMode = ShadowCastingMode.Off;
                go.SetActive(false);
                splitPiece[i] = go.transform;
            }

            fingerT = MakeFinger();
            pulseGo = MgfLook.Prim(PrimitiveType.Cylinder, "Pulse", Vector3.zero, new Vector3(1.1f, 0.02f, 1.1f), MgfLook.Alpha(new Color(1, 1, 1, 0.45f)), world).transform;
            pulseGo.gameObject.SetActive(false);

            playCol = MgfLook.Block("Play", new Vector3(0, 0.06f, 0.4f), new Vector3(14, 0.1f, 14), 0.02f, MgfLook.Alpha(new Color(0, 0, 0, 0)), world).transform;
            var pc = playCol.GetComponent<Renderer>(); if (pc) pc.enabled = false;

            string[] names = { "A", "B", "C", "D", "E", "G", "M", "N" };
            for (int i = 0; i < 8; i++)
            {
                vLbl[i] = MgfText.World(names[i], Vector3.zero, 3.6f, Cream, world);
                vLbl[i].fontStyle = FontStyles.Bold;
                vLbl[i].outlineWidth = 0.22f; vLbl[i].outlineColor = new Color(0.08f, 0.05f, 0.04f, 0.95f);
            }
            for (int i = 0; i < cmLbl.Length; i++)
            {
                cmLbl[i] = MgfText.World("", Vector3.zero, 2.15f, InkC, world);
                cmLbl[i].fontStyle = FontStyles.Bold;
                cmLbl[i].outlineWidth = 0.08f; cmLbl[i].outlineColor = new Color(0.96f, 0.94f, 0.9f, 0.7f);
            }
            ratioLbl = MgfText.World("", Vector3.zero, 3.1f, Cream, world);
            ratioLbl.fontStyle = FontStyles.Bold;
            ratioLbl.outlineWidth = 0.22f; ratioLbl.outlineColor = new Color(0.1f, 0.06f, 0.04f, 0.95f);
            liveLbl = MgfText.World("", Vector3.zero, 2.4f, InkC, world);
            liveLbl.fontStyle = FontStyles.Bold;
            liveLbl.outlineWidth = 0.18f; liveLbl.outlineColor = new Color(0.96f, 0.94f, 0.9f, 0.9f);
            stampCm = MgfText.World("", Vector3.zero, 4.2f, Cream, world);
            stampCm.fontStyle = FontStyles.Bold;
            stampCm.outlineWidth = 0.24f; stampCm.outlineColor = new Color(0.1f, 0.06f, 0.04f, 1f);
            logoW = MgfText.World("석회 착지", Vector3.zero, 8.4f, Cream, world);
            logoW.fontStyle = FontStyles.Bold;
            logoW.outlineWidth = 0.32f; logoW.outlineColor = new Color(0.18f, 0.12f, 0.08f, 1f);
            hopHint = MgfText.World("", Vector3.zero, 2.4f, Cream, world);
            hopHint.fontStyle = FontStyles.Bold;
            hopHint.outlineWidth = 0.2f; hopHint.outlineColor = new Color(0.12f, 0.08f, 0.04f, 0.95f);
            acSide = MgfText.World("", Vector3.zero, 2.5f, Cream, world);
            acSide.fontStyle = FontStyles.Bold;
            acSide.outlineWidth = 0.18f; acSide.outlineColor = new Color(0.12f, 0.08f, 0.04f, 0.95f);
        }

        static void NoShadow(GameObject go)
        {
            if (!go) return;
            var r = go.GetComponent<Renderer>();
            if (r) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        Transform MakeWedge(Transform parent)
        {
            var go = MgfLook.Block("Block", Vector3.zero, new Vector3(1.65f, 0.52f, 0.78f), 0.06f, matBlock, parent);
            MgfLook.Block("BlockSlope", new Vector3(0, -0.1f, 0.38f), new Vector3(1.25f, 0.28f, 0.55f), 0.05f, matBlock, go.transform);
            MgfLook.Block("BlockLip", new Vector3(0, 0.2f, -0.22f), new Vector3(1.05f, 0.08f, 0.12f), 0.02f, MgfLook.Lit(Hex("D8C4A0"), 0.32f, 0.08f), go.transform);
            return go.transform;
        }

        Transform MakeTriPad()
        {
            var go = new GameObject("TriPad");
            go.transform.SetParent(world, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = matUre;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
            go.AddComponent<MeshCollider>();
            go.name = "TriPad";
            return go.transform;
        }

        static MeshFilter MakeMr(string n, Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject(n);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            return mf;
        }

        Transform MakeFinger()
        {
            var root = new GameObject("Finger").transform;
            root.SetParent(world, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Palm", Vector3.zero, Vector3.one * 0.42f, MgfLook.Lit(Hex("E8D5C4"), 0.45f), root);
            MgfLook.Prim(PrimitiveType.Capsule, "Idx", new Vector3(0.05f, 0.38f, 0), new Vector3(0.16f, 0.28f, 0.16f), MgfLook.Lit(Hex("E8D5C4"), 0.45f), root);
            root.gameObject.SetActive(false);
            return root;
        }

        static Texture2D BakeConcrete()
        {
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGB24, false) { name = "Conc", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[S * S];
            var rng = new System.Random(17);
            Color a = Hex("C4B8A8"), b = Hex("B3A696"), c = Hex("D0C6B6"), d = Hex("8E8274");
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.6f + Mathf.PerlinNoise(x * 0.22f, y * 0.19f) * 0.4f;
                Color g = Color.Lerp(a, n > 0.55f ? c : b, n);
                int cell = (x / 64) + (y / 64) * 4;
                if ((x % 64) < 2 || (y % 64) < 2) g = Color.Lerp(g, d, 0.45f);
                if (rng.NextDouble() < 0.04) g = Color.Lerp(g, d, 0.35f);
                if (cell % 3 == 0) g = Color.Lerp(g, Hex("CBC1B2"), 0.12f);
                px[y * S + x] = g;
            }
            tex.SetPixels(px); tex.Apply(false, true);
            return tex;
        }

        static Texture2D BakeWood()
        {
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGB24, false) { name = "Wood", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[S * S];
            Color a = Hex("A56B32"), b = Hex("8A5424"), c = Hex("C4894A");
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float grain = Mathf.PerlinNoise(x * 0.18f, y * 0.04f);
                float ring = Mathf.Abs(Mathf.Sin((x + grain * 8f) * 0.22f));
                Color g = Color.Lerp(a, ring > 0.7f ? b : c, grain);
                px[y * S + x] = g;
            }
            tex.SetPixels(px); tex.Apply(false, true);
            return tex;
        }

        void LayoutTri(Sheet s)
        {
            if (s == null) return;
            float h = land ? 11.2f : 10.4f;
            float half = land ? 6.6f : 5.6f;
            float ox = (land && ph != Ph.Title) ? 3.0f : 0f;
            A2 = new Vector2(ox, h * 0.50f);
            B2 = new Vector2(-half + ox, -h * 0.50f);
            C2 = new Vector2(half + ox, -h * 0.50f);
            A = new Vector3(A2.x, 0.05f, A2.y);
            B = new Vector3(B2.x, 0.05f, B2.y);
            C = new Vector3(C2.x, 0.05f, C2.y);
            RebuildTriPad();
        }

        void RebuildTriPad()
        {
            if (!triPad) return;
            var mf = triPad.GetComponent<MeshFilter>();
            var mc = triPad.GetComponent<MeshCollider>();
            var mesh = new Mesh { name = "TriPad" };
            float y0 = 0.02f, y1 = 0.11f;
            var v = new Vector3[] {
                new Vector3(A2.x, y1, A2.y), new Vector3(B2.x, y1, B2.y), new Vector3(C2.x, y1, C2.y),
                new Vector3(A2.x, y0, A2.y), new Vector3(B2.x, y0, B2.y), new Vector3(C2.x, y0, C2.y)
            };
            Vector2 Uv(Vector2 p) => new Vector2(p.x / 12f + 0.5f, p.y / 12f + 0.5f);
            var uv = new Vector2[] { Uv(A2), Uv(B2), Uv(C2), Uv(A2), Uv(B2), Uv(C2) };
            mesh.vertices = v;
            mesh.uv = uv;
            mesh.triangles = new[] { 0, 1, 2,  3, 5, 4,  0, 3, 1, 1, 3, 4,  1, 4, 2, 2, 4, 5,  2, 5, 0, 0, 5, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            if (mc) mc.sharedMesh = mesh;
        }

        Vector2 OnAB(float t) => Vector2.Lerp(A2, B2, t);
        Vector2 OnAC(float t) => Vector2.Lerp(A2, C2, t);
        Vector2 OnBC(float t) => Vector2.Lerp(B2, C2, t);
        Vector3 W(Vector2 p, float y = 0.05f) => new Vector3(p.x, y, p.y);

        Vector2 RungL(Sheet s, int cm)
        {
            float t = cm / (float)Mathf.Max(1, s.Span);
            if (s.isPlumb) return Vector2.Lerp(A2, (B2 + C2) * 0.5f, t);
            return OnAB(t);
        }
        Vector2 RungR(Sheet s, int cm)
        {
            float t = cm / (float)Mathf.Max(1, s.Span);
            if (s.isPlumb)
            {
                var mid = (B2 + C2) * 0.5f;
                var dir = (C2 - B2).normalized;
                float w = Vector2.Distance(B2, C2) * t * 0.5f;
                return mid + dir * w;
            }
            return OnAC(t);
        }
        Vector2 RungMid(Sheet s, int cm) => (RungL(s, cm) + RungR(s, cm)) * 0.5f;

        void SetupSheet(Sheet s, bool isDemo)
        {
            demo = isDemo;
            LayoutTri(s);
            DrawStatic(s);
            RefreshBlock(true);
            FrameCam();
            stampInk.Clear(); stampInk.Apply();
            smearInk.Clear(); smearInk.Apply();
            stampCm.text = "";
            for (int i = 0; i < 6; i++) if (splitPiece[i]) splitPiece[i].gameObject.SetActive(false);
        }

        void DrawStatic(Sheet s)
        {
            if (s == null) return;
            ticks.Clear();
            int span = Mathf.Max(1, s.Span);
            var lime = C32(Cream, 0.92f);
            var faint = C32(Cream, 0.55f);
            ticks.Seg(A2, B2, 0.07f, C32(InkC, 0.85f));
            ticks.Seg(A2, C2, 0.07f, C32(InkC, 0.85f));
            ticks.Seg(B2, C2, 0.09f, C32(InkC, 0.9f));

            if (s.isPlumb)
            {
                var mid = (B2 + C2) * 0.5f;
                ticks.Seg(A2, mid, 0.08f, lime);
                for (int i = 1; i < span; i++)
                {
                    var p = Vector2.Lerp(A2, mid, i / (float)span);
                    var n = (C2 - B2).normalized;
                    float w = 0.55f + 1.6f * (i / (float)span);
                    ticks.Seg(p - n * w, p + n * w, i == s.misTick ? 0.055f : 0.045f, faint);
                }
            }
            else
            {
                for (int i = 1; i < span; i++)
                {
                    var l = OnAB(i / (float)span);
                    var r = OnAC(i / (float)span);
                    bool ghost = s.hideRatio && (i == s.misTick || i == s.misTick2) && i != s.target;
                    ticks.Seg(l, r, ghost ? 0.07f : 0.048f, faint);
                }
                if (s.kind == Kind.Three && s.fAd > 0)
                {
                    var l = OnAB(s.fAd / (float)span);
                    var r = OnAC(s.fAd / (float)span);
                    ticks.Seg(l, r, 0.07f, lime);
                }
            }
            ticks.Apply();

            PlaceLabels(s);
        }

        void PlaceLabels(Sheet s)
        {
            int span = Mathf.Max(1, s.Span);
            Face(vLbl[0], "A", W(A2, 0.35f) + new Vector3(0, 0, 0.55f));
            Face(vLbl[1], "B", W(B2, 0.28f) + new Vector3(-0.55f, 0, -0.35f));
            Face(vLbl[2], "C", W(C2, 0.28f) + new Vector3(0.55f, 0, -0.35f));
            bool showDE = !s.isPlumb && (s.kind != Kind.Mid);
            if (showDE && s.target > 0)
            {
                var dOff = land ? new Vector3(-0.85f, 0, 0.15f) : new Vector3(-0.45f, 0, 0.1f);
                var eOff = land ? new Vector3(0.85f, 0, 0.15f) : new Vector3(0.45f, 0, 0.1f);
                Face(vLbl[3], "D", W(OnAB(s.target / (float)span), 0.28f) + dOff);
                Face(vLbl[4], "E", W(OnAC(s.target / (float)span), 0.28f) + eOff);
            }
            else { vLbl[3].text = ""; vLbl[4].text = ""; }
            if (s.isPlumb)
            {
                var mid = (B2 + C2) * 0.5f;
                Face(vLbl[5], "G", W(Vector2.Lerp(A2, mid, 2f / 3f), 0.32f));
                Face(vLbl[6], "D", W(mid, 0.28f) + new Vector3(0, 0, -0.3f));
                vLbl[7].text = "";
            }
            else if (s.kind == Kind.Mid)
            {
                Face(vLbl[6], "M", W(OnAB(0.5f), 0.28f) + new Vector3(-0.25f, 0, 0));
                Face(vLbl[7], "N", W(OnAC(0.5f), 0.28f) + new Vector3(0.25f, 0, 0));
                vLbl[5].text = "";
            }
            else { vLbl[5].text = ""; vLbl[6].text = ""; vLbl[7].text = ""; }

            int showN = Mathf.Min(cmLbl.Length, span - 1);
            bool showAllCm = s.stage <= 1 || ph == Ph.Practice || ph == Ph.Title;
            for (int i = 0; i < cmLbl.Length; i++)
            {
                int cm = i + 1;
                if (i >= showN) { cmLbl[i].text = ""; continue; }
                bool edge = cm == 1 || cm == span / 2 || cm == s.target || (showAllCm && (cm % 2 == 0 || span <= 10));
                if (!edge && !showAllCm) { cmLbl[i].text = ""; continue; }
                var p = RungMid(s, cm);
                var l = RungL(s, cm); var r = RungR(s, cm);
                var along = (r - l);
                if (along.sqrMagnitude > 1e-6f) along.Normalize();
                else along = new Vector2(1, 0);
                Vector2 off = cm == s.ad ? along * 0.85f : Vector2.zero;
                Face(cmLbl[i], cm + "", W(p + off, 0.22f));
                cmLbl[i].fontSize = 2.15f;
                cmLbl[i].color = InkC;
            }
            if (acSide != null)
            {
                if (s.kind == Kind.FindAe)
                {
                    var mid = (A2 + C2) * 0.5f;
                    var n = new Vector2(C2.y - A2.y, A2.x - C2.x);
                    if (n.sqrMagnitude > 1e-6f) n.Normalize();
                    Face(acSide, "AC " + s.ac + " cm", W(mid + n * 0.7f, 0.28f));
                }
                else acSide.text = "";
            }
        }

        void Face(TextMeshPro t, string s, Vector3 p)
        {
            t.text = s;
            t.transform.position = p;
            if (cam) t.transform.rotation = Quaternion.LookRotation(t.transform.position - cam.transform.position, Vector3.up);
        }

        void RefreshBlock(bool snap)
        {
            if (!blockT || cur == null) return;
            var p = RungMid(cur, cur.ad);
            var l = RungL(cur, cur.ad);
            var r = RungR(cur, cur.ad);
            var mid = W(p, 0.38f);
            if (hopAnim > 0)
            {
                float k = 1f - hopAnim / 0.18f;
                float arc = Mathf.Sin(k * Mathf.PI) * 0.45f;
                mid.y += arc;
            }
            blockT.position = mid;
            var dir = r - l;
            float ang = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            blockT.rotation = Quaternion.Euler(0, ang, 0);
            float sq = landSquash > 0 ? Mathf.Lerp(1f, landSquash, landSquash) : 1f;
            if (jiggleT > 0) blockT.position += new Vector3(Mathf.Sin(jiggleT * 48f) * 0.08f, 0, 0);
            blockT.localScale = Vector3.Scale(blockBaseScale, new Vector3(2f - sq, sq, 2f - sq));

            D = W(l, 0.08f); E = W(r, 0.08f);
            midD = (B + C) * 0.5f;
            Gpt = Vector3.Lerp(A, midD, 2f / 3f);

            if (ph != Ph.Title) UpdateLive(cur);
        }

        void UpdateLive(Sheet s)
        {
            if (s == null || liveLbl == null) return;
            int span = Mathf.Max(1, s.Span);
            int ad = s.ad;
            int db = span - ad;
            if (s.kind == Kind.Mid && ph != Ph.Title)
            {
                // 현재 평행 줄의 MN 길이. 밑변의 ½이 되는 칸 = 중점.
                if (span > 0 && (long)s.bc * ad % span == 0)
                    liveLbl.text = "MN " + (s.bc * ad / span) + " cm";
                else liveLbl.text = "MN";
                var rm = RungMid(s, ad);
                var rr = RungR(s, ad) - RungL(s, ad);
                if (rr.sqrMagnitude > 1e-6f) rr.Normalize();
                Face(liveLbl, liveLbl.text, W(rm + rr * 1.15f, 0.7f));
            }
            else if (s.hopAc && s.stage <= 1 && ph != Ph.Title)
            {
                int ec = Mathf.Max(1, span - ad);
                Judge.Ratio(Mathf.Max(1, ad), ec, out int m, out int n);
                liveLbl.text = "AE:EC " + m + ":" + n;
                var rm = RungMid(s, ad);
                var rr = RungR(s, ad) - RungL(s, ad);
                if (rr.sqrMagnitude > 1e-6f) rr.Normalize();
                Face(liveLbl, liveLbl.text, W(rm + rr * 1.15f, 0.7f));
            }
            else if (s.stage <= 1 && !s.hideRatio && ph != Ph.Title)
            {
                Judge.Ratio(Mathf.Max(1, ad), Mathf.Max(1, db), out int m, out int n);
                liveLbl.text = (s.isPlumb ? "AG:GD " : "AD:DB ") + m + ":" + n;
                var rm = RungMid(s, ad);
                var rr = RungR(s, ad) - RungL(s, ad);
                if (rr.sqrMagnitude > 1e-6f) rr.Normalize();
                Face(liveLbl, liveLbl.text, W(rm + rr * 1.15f, 0.7f));
            }
            else liveLbl.text = "";

            ratioLbl.text = "";
        }

        void DrawGuide(Sheet s)
        {
            guideInk.Clear();
            if (ph == Ph.Practice)
            {
                var a = RungMid(s, 1);
                var b = RungMid(s, 2);
                var c = RungMid(s, 4);
                float phs = ghostT * 1.6f;
                var col = C32(Cream, 0.7f);
                guideInk.Dashed(a, b, 0.22f, 0.12f, 0.05f, col, phs);
                guideInk.Dashed(b, c, 0.22f, 0.12f, 0.05f, col, phs + 0.3f);
                guideInk.Chevron(c, c - b, 0.28f * ghostScale, 0.06f, col);
                var ring = RungMid(s, 4);
                float rr = 0.42f + 0.12f * Mathf.Sin(ghostT * 5f);
                guideInk.Disc(ring, rr * ghostScale, C32(Cream, 0.28f + 0.12f * Mathf.Sin(ghostT * 6f)));
            }
            if (refuseT > 0 && cur != null)
            {
                var to = RungMid(cur, cur.ad);
                guideInk.Chevron(to, new Vector2(0, 1), 0.4f, 0.07f, C32(Wood, 0.85f));
            }
            guideInk.Apply();
        }

        void DrawStamp(Sheet s)
        {
            stampInk.Clear();
            if (s == null || stampT <= 0) { stampInk.Apply(); return; }
            float k = Mathf.Clamp01(stampT / 0.25f);
            var col = C32(Cream, 0.55f + 0.4f * k);
            int span = Mathf.Max(1, s.Span);
            float t = s.target / (float)span;
            var l = s.isPlumb ? Vector2.Lerp(A2, (B2 + C2) * 0.5f, t) : OnAB(t);
            var r = s.isPlumb ? l : OnAC(t);
            if (s.isPlumb)
            {
                var mid = (B2 + C2) * 0.5f;
                var n = (C2 - B2).normalized;
                float w = Vector2.Distance(B2, C2) * t * 0.5f;
                l = Vector2.Lerp(A2, mid, t) - n * w;
                r = Vector2.Lerp(A2, mid, t) + n * w;
            }
            stampInk.Tri(A2, l, r, C32(Cream, 0.35f * k));
            stampInk.Seg(l, r, 0.1f, col);
            stampInk.Seg(A2, l, 0.07f, col);
            stampInk.Seg(A2, r, 0.07f, col);
            stampInk.Apply();
            var midP = (l + r) * 0.5f;
            Face(stampCm, Words.Answer(s), W(midP, 0.55f));
            stampCm.color = Cream;
        }

        void DrawSmear(Sheet s)
        {
            smearInk.Clear();
            if (s == null || smearT <= 0 || s.misTick < 1) { smearInk.Apply(); return; }
            float t = s.misTick / (float)Mathf.Max(1, s.Span);
            Vector2 l, r;
            if (s.isPlumb)
            {
                var mid = (B2 + C2) * 0.5f;
                var p = Vector2.Lerp(A2, mid, t);
                var n = (C2 - B2).normalized;
                l = p - n * 0.9f; r = p + n * 0.9f;
            }
            else { l = OnAB(t); r = OnAC(t); }
            var col = C32(Cream, 0.35f * Mathf.Clamp01(smearT / 0.3f));
            smearInk.Seg(l + new Vector2(0.05f, 0.04f), r + new Vector2(-0.04f, 0.06f), 0.11f, col);
            smearInk.Apply();
        }

        void ApplyWideQuality()
        {
            if (MgfBridge.LowGfx || !cam) return;
            if (land)
            {
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadowDistance = 14f;
                cam.allowMSAA = false;
            }
            else
            {
                QualitySettings.antiAliasing = 2;
                QualitySettings.shadowDistance = 18f;
                cam.allowMSAA = true;
            }
        }

        void FrameCam()
        {
            if (!cam) return;
            var p = camHome;
            var look = lookHome;
            if (land && ph != Ph.Title) { p = new Vector3(2.4f, 10.6f, -8.2f); look = new Vector3(2.6f, 0.05f, 0.15f); }
            else if (land) { p = new Vector3(0, 11.6f, -8.6f); look = new Vector3(0, 0.1f, 0.45f); }
            cam.transform.position = p;
            cam.transform.LookAt(look);
            camHome = p; lookHome = look;
            MgfLook.FitWidth(cam, land ? 28f : 34f, 0.72f);
        }

        void ShowGhost(int ad)
        {
            if (!ghostTform || cur == null) return;
            var p = RungMid(cur, ad);
            ghostTform.position = W(p, 0.32f);
            ghostTform.gameObject.SetActive(true);
            ghostAlpha = 1f;
        }
        void HideGhost() { if (ghostTform) ghostTform.gameObject.SetActive(false); ghostAlpha = 0; }

        void PunchBlock() { landSquash = 1.22f; }
        void JiggleBlock() { jiggleT = 0.28f; }
        void PointAtBlock() { if (pulseGo) pulseGo.gameObject.SetActive(true); pulseTm = 0.7f; }

        void Animate(float dt)
        {
            titleT += dt; ghostT += dt;
            if (jiggleT > 0) jiggleT -= dt;
            if (pulseTm > 0) pulseTm -= dt;
            if (refuseT > 0) refuseT -= dt;
            if (revealT > 0) revealT -= dt;
            if (stampT > 0) stampT -= dt;
            if (smearT > 0) smearT -= dt;
            if (splitT > 0) splitT -= dt;
            if (cardSlide > 0) cardSlide -= dt;
            if (hopAnim > 0) hopAnim -= dt;
            if (dustT > 0) dustT -= dt;
            if (landSquash > 0)
            {
                landSquash = Mathf.MoveTowards(landSquash, 1f, dt * 3.2f);
                if (Mathf.Abs(landSquash - 1f) < 0.02f) landSquash = 0;
            }

            float goal = cur != null ? (stampT > 0 ? cmGoal : cur.ad) : 0;
            if (cmGoal <= 0) cmGoal = goal;
            cmVel += (goal - shownCm) * 160f * dt;
            cmVel *= Mathf.Exp(-8f * dt);
            shownCm += cmVel * dt;

            if (ph == Ph.Title && cur != null && dt > 0)
            {
                demoHop += dt;
                if (demoHop > 0.52f)
                {
                    demoHop = 0;
                    demoAd++;
                    if (demoAd > cur.adHi - 1) demoAd = 1;
                    cur.ad = demoAd;
                    hopAnim = 0.16f; landSquash = 0.84f; dustT = 0.25f;
                    SeokhoeSound.Play("hop", 0.35f);
                    RefreshBlock(true);
                }
            }

            inkBudget = land ? 1 : 2;
            bool moving = hopAnim > 0 || landSquash > 0 || jiggleT > 0;
            if (moving) RefreshBlock(false);
            else if (cur != null && ph != Ph.Title) UpdateLive(cur);

            if (stampT > 0 || stampOn)
            {
                if (inkBudget > 0) { DrawStamp(cur); inkBudget--; }
                stampOn = stampT > 0;
            }
            if (smearT > 0 || smearOn)
            {
                if (inkBudget > 0) { DrawSmear(cur); inkBudget--; }
                smearOn = smearT > 0;
            }
            if ((ph == Ph.Practice || refuseT > 0) && inkBudget > 0) DrawGuide(cur);
            AnimateDust(dt);
            AnimateSplit(dt);
            AnimateUi(dt);
            FaceLogo();
        }

        void FaceLogo()
        {
            if (!logoW) return;
            logoW.gameObject.SetActive(false);
            if (ph != Ph.Title) return;
            float z = 0.06f * Mathf.Sin(titleT * 0.7f);
            logoW.transform.position = new Vector3(A2.x, 1.28f, A2.y + 1.05f + z);
            if (cam) logoW.transform.rotation = Quaternion.LookRotation(logoW.transform.position - cam.transform.position, Vector3.up);
            swingAmp = 4f * Mathf.Sin(titleT * 1.6f);
        }

        void AnimateDust(float dt)
        {
            bool on = dustT > 0 && blockT;
            for (int i = 0; i < dust.Length; i++)
            {
                if (!dust[i]) continue;
                dust[i].gameObject.SetActive(on);
                if (!on) continue;
                float u = (i + 0.3f) / dust.Length;
                var p = blockT.position + new Vector3(Mathf.Sin(i * 1.7f) * 0.35f, 0.15f + (1f - dustT / 0.28f) * 0.4f * u, Mathf.Cos(i * 2.1f) * 0.25f);
                dust[i].position = p;
                float s = 0.2f + 0.25f * (1f - dustT / 0.28f);
                dust[i].localScale = Vector3.one * s;
                dust[i].LookAt(cam ? cam.transform.position : p + Vector3.up);
            }
        }

        void AnimateSplit(float dt)
        {
            bool on = splitT > 0 && cur != null && (cur.kind == Kind.Cent || cur.kind == Kind.Area);
            var mid = (B2 + C2) * 0.5f;
            var g = Vector2.Lerp(A2, mid, 2f / 3f);
            Vector2[] pts = { A2, B2, C2, mid, Vector2.Lerp(A2, B2, 0.5f), Vector2.Lerp(A2, C2, 0.5f) };
            for (int i = 0; i < 6; i++)
            {
                if (!splitPiece[i]) continue;
                splitPiece[i].gameObject.SetActive(on);
                if (!on) continue;
                var mf = splitPiece[i].GetComponent<MeshFilter>();
                var mesh = mf.sharedMesh;
                Vector2 a = g, b = pts[i], c = pts[(i + 1) % 6];
                if (i >= 3) { b = pts[i - 3]; c = mid; }
                float k = 1f - Mathf.Clamp01(splitT / 1.15f);
                var off = ((a + b + c) / 3f - g) * k * 0.35f;
                mesh.Clear();
                mesh.vertices = new[] { W(a + off, 0.14f), W(b + off, 0.14f), W(c + off, 0.14f) };
                mesh.triangles = new[] { 0, 1, 2 };
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
            }
        }
    }
}
