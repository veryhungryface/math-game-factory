// 깃 나눠 — 월드. 한낮 베란다 옥색 캔버스 삼각기 + 철 밀대.
// 최빈 픽셀은 #B8D4C8 옥색 삼각기(부팅 때 1회 베이크한 면직 결·헴 스티치). 회화 배경 없음.
// 정답: 밀대 안착 + 천이 실밥을 따라 갈라지고 작은 닮은 삼각기가 더미로 미끄러지며 cm 스프링.
// 오답: 밀대 덜컥 + 잘못된 작은 깃이 쇠꽂이에. 파티클 버스트·점수 팝업·화면 흔들림 없음.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.GitNanwo
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
        public void Disc(Vector2 o, float r, Color32 col, int n = 14)
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
        public void Apply()
        {
            mesh.Clear();
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(80, 20, 80));
        }
    }

    public partial class GitNanwoGame
    {
        static readonly Color Celadon = Hex("B8D4C8"), Beech = Hex("E7D7B8"), Iron = Hex("3A3F45"), Brass = Hex("C45A2A"), InkC = Hex("1E2A24");
        static readonly Color Hem = Hex("F4F0E6"), Cream = Hex("F7F1E4");
        static Color Hex(string h) => MgfLook.Hex(h);
        static Color32 C32(Color c, float a = 1f)
        {
            var l = c.linear;
            return new Color32((byte)(l.r * 255), (byte)(l.g * 255), (byte)(l.b * 255), (byte)(Mathf.Clamp01(a) * 255));
        }

        Camera cam;
        Transform world, milldae, millHead, millBlade, brassScrew, ringT, ghostMill, fingerT, cutSmall, cutTrap;
        Transform spikeT;
        readonly Transform[] spareFlag = new Transform[3];
        readonly Transform[] pileFlag = new Transform[10];
        int pileN, impaled;
        MeshFilter tickF, guideF, hemF, ghostF;
        Ink ticks, guideInk, hemInk, ghostInk;
        Material matCanvas, matWood, matIron, matBrass, matInk, matAlpha;
        Texture2D canvasTex, woodTex;
        TextMeshPro[] vLbl = new TextMeshPro[6];
        TextMeshPro[] cmLbl = new TextMeshPro[16];
        TextMeshPro liveAd, liveDb, stampCm;
        Vector3 A, B, C, Dpt, Ept, Gpt, midBc;
        Vector2 A2, B2, C2;
        Vector3 camHome, lookHome;
        Vector3 millBaseScale, ringBaseScale;
        float ghostAlpha;
        int flagsLeftVis = 3;

        static void NoShadow(GameObject o)
        {
            var r = o.GetComponent<Renderer>();
            if (r) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true; }
        }

        MeshFilter MakeMr(string name, Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            return mf;
        }

        Texture2D BakeCanvas()
        {
            const int S = 256;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Canvas", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[S * S];
            var rng = new System.Random(11);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float wx = 0.5f + 0.5f * Mathf.Sin(x * 0.85f) * Mathf.Sin(y * 0.17f);
                float wy = 0.5f + 0.5f * Mathf.Sin(y * 0.9f) * Mathf.Sin(x * 0.14f);
                float weave = 0.88f + 0.12f * wx * wy;
                int n = rng.Next(8) - 4;
                float k = weave + n / 255f;
                byte r = (byte)Mathf.Clamp(184 * k, 0, 255);
                byte g = (byte)Mathf.Clamp(212 * k, 0, 255);
                byte b = (byte)Mathf.Clamp(200 * k, 0, 255);
                if (y % 32 < 1 || x % 40 < 1) { r = (byte)Mathf.Min(255, r + 10); g = (byte)Mathf.Min(255, g + 8); }
                px[y * S + x] = new Color32(r, g, b, 255);
            }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        Texture2D BakeWood()
        {
            const int S = 256;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Beech", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[S * S];
            var rng = new System.Random(4);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float g = 0.55f + 0.08f * Mathf.Sin((x + y * 0.08f) * 0.21f) + 0.04f * Mathf.Sin(y * 0.4f);
                g += (rng.Next(7) - 3) / 255f;
                byte r = (byte)Mathf.Clamp(231 * g, 0, 255);
                byte gg = (byte)Mathf.Clamp(215 * g, 0, 255);
                byte b = (byte)Mathf.Clamp(184 * g, 0, 255);
                px[y * S + x] = new Color32(r, gg, b, 255);
            }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        GameObject MakeTriMesh(string name, Vector3 a, Vector3 b, Vector3 c, float y, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            var nrm = Vector3.up;
            mesh.SetVertices(new List<Vector3> { new Vector3(a.x, y, a.z), new Vector3(b.x, y, b.z), new Vector3(c.x, y, c.z) });
            mesh.SetNormals(new List<Vector3> { nrm, nrm, nrm });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) });
            // A,C,B 가 +Y 에서 반시계 → 앞면. 양면을 넣어 빗각에서도 면이 보이게.
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 1, 2 }, 0);
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
            return go;
        }

        Transform MakeSmallFlag(string name, Transform parent, float scale)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var a = new Vector3(0, 0.04f, 0.55f * scale);
            var b = new Vector3(-0.48f * scale, 0.04f, -0.35f * scale);
            var c = new Vector3(0.48f * scale, 0.04f, -0.35f * scale);
            MakeTriMesh(name + "Cloth", a, b, c, 0.04f, matCanvas, root);
            MgfLook.Block(name + "Hem", new Vector3(0, 0.05f, -0.35f * scale), new Vector3(0.96f * scale, 0.03f, 0.06f), 0.01f, MgfLook.Lit(Hem, 0.35f, 0f), root);
            return root;
        }

        void BuildWorld()
        {
            MgfLook.Quality(16f);
            QualitySettings.antiAliasing = MgfBridge.LowGfx ? 0 : 2;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.pixelLightCount = 1;
            MgfLook.Sky(Hex("C5D8D0"), Hex("D5E0D8"), Hex("C8D4C6"), 0.94f);
            var sun = MgfLook.Sun(new Vector3(48f, -42f, 6), Hex("FFF4DC"), 1.18f, 0.42f);
            sun.shadows = LightShadows.Hard;
            cam = MgfLook.Camera(new Vector3(0, 9.2f, -6.5f), new Vector3(0, 0.08f, 0.25f), 30f);
            cam.allowMSAA = !MgfBridge.LowGfx;
            camHome = cam.transform.position; lookHome = new Vector3(0, 0.08f, 0.25f);

            canvasTex = BakeCanvas();
            woodTex = BakeWood();
            matCanvas = MgfLook.Lit(Color.white, 0.18f, 0.0f, Celadon * 0.28f, canvasTex);
            matWood = MgfLook.Lit(Beech, 0.28f, 0.02f, Hex("8A7050"), woodTex);
            matIron = MgfLook.Lit(Iron, 0.42f, 0.55f, Hex("1A1E22"));
            matBrass = MgfLook.Lit(Brass, 0.55f, 0.65f, Hex("6A2A10"));
            matInk = MgfLook.Alpha(Color.white);
            matAlpha = MgfLook.Alpha(new Color(1, 1, 1, 0.35f));

            world = new GameObject("World").transform;
            NoShadow(MgfLook.Block("Table", new Vector3(0, -0.28f, 0.15f), new Vector3(10.4f, 0.52f, 10.0f), 0.08f, matWood, world));
            NoShadow(MgfLook.Block("Apron", new Vector3(0, -0.72f, 0.15f), new Vector3(9.6f, 0.4f, 9.2f), 0.04f, MgfLook.Lit(Hex("C8B490"), 0.2f, 0.02f), world));
            var leg = MgfLook.Lit(Hex("C4B086"), 0.18f, 0.02f);
            foreach (var p in new[] { new Vector3(-4.4f, -1.4f, 4.2f), new Vector3(4.4f, -1.4f, 4.2f), new Vector3(-4.4f, -1.4f, -3.8f), new Vector3(4.4f, -1.4f, -3.8f) })
                NoShadow(MgfLook.Block("Leg", p, new Vector3(0.38f, 1.7f, 0.38f), 0.04f, leg, world));

            // 플레이 히트 평면 (BoxCollider 만 — 스트리핑된 CapsuleCollider 를 피한다)
            var play = MgfLook.Block("Play", new Vector3(0, 0.02f, 0.2f), new Vector3(10.2f, 0.04f, 9.6f), 0.01f, matWood, world);
            Object.Destroy(play.GetComponent<MeshRenderer>());

            milldae = new GameObject("Milldae").transform;
            milldae.SetParent(world, false);
            millBlade = MgfLook.Block("Blade", new Vector3(0, 0.12f, 0), new Vector3(5.6f, 0.07f, 0.28f), 0.02f, matIron, milldae).transform;
            millHead = MgfLook.Block("Head", new Vector3(-2.5f, 0.16f, 0), new Vector3(0.32f, 0.14f, 1.35f), 0.03f, matIron, milldae).transform;
            brassScrew = MgfLook.Block("Screw", new Vector3(-2.5f, 0.32f, 0), new Vector3(0.18f, 0.16f, 0.18f), 0.04f, matBrass, milldae).transform;
            millBaseScale = milldae.localScale;

            ringT = MgfLook.Block("Ring", Vector3.zero, new Vector3(0.55f, 0.10f, 0.55f), 0.12f, matBrass, world).transform;
            ringBaseScale = ringT.localScale;
            ringT.gameObject.SetActive(false);

            ghostMill = MgfLook.Block("GhostMill", Vector3.zero, new Vector3(5.2f, 0.05f, 0.22f), 0.02f, matAlpha, world).transform;
            ghostMill.gameObject.SetActive(false);

            fingerT = new GameObject("Finger").transform;
            fingerT.SetParent(world, false);
            var tip = MgfLook.Block("Tip", Vector3.zero, new Vector3(0.28f, 0.28f, 0.28f), 0.12f, MgfLook.Unlit(new Color(0.12f, 0.14f, 0.16f, 0.85f)), fingerT);
            Object.Destroy(tip.GetComponent<Collider>());
            fingerT.gameObject.SetActive(false);

            ticks = new Ink(0.07f); guideInk = new Ink(0.08f); hemInk = new Ink(0.055f); ghostInk = new Ink(0.075f);
            tickF = MakeMr("Ticks", ticks.mesh, matInk, world);
            guideF = MakeMr("Guide", guideInk.mesh, matInk, world);
            hemF = MakeMr("Hem", hemInk.mesh, matInk, world);
            ghostF = MakeMr("GhostLine", ghostInk.mesh, matInk, world);

            for (int i = 0; i < vLbl.Length; i++)
            {
                vLbl[i] = MgfText.World("", Vector3.zero, 3.4f, InkC, world);
                vLbl[i].alignment = TextAlignmentOptions.Center;
            }
            for (int i = 0; i < cmLbl.Length; i++)
            {
                cmLbl[i] = MgfText.World("", Vector3.zero, 2.2f, InkC, world);
                cmLbl[i].fontStyle = FontStyles.Bold;
            }
            liveAd = MgfText.World("", Vector3.zero, 2.6f, InkC, world);
            liveAd.fontStyle = FontStyles.Bold;
            liveDb = MgfText.World("", Vector3.zero, 2.6f, InkC, world);
            stampCm = MgfText.World("", Vector3.zero, 4.2f, Brass, world);
            stampCm.fontStyle = FontStyles.Bold;
            stampCm.gameObject.SetActive(false);

            cutSmall = MakeSmallFlag("CutSmall", world, 0.85f);
            cutSmall.gameObject.SetActive(false);
            cutTrap = new GameObject("CutTrap").transform;
            cutTrap.SetParent(world, false);

            spikeT = MgfLook.Block("Spike", new Vector3(-4.55f, 0.55f, 2.1f), new Vector3(0.14f, 1.1f, 0.14f), 0.02f, matIron, world).transform;
            MgfLook.Block("SpikeBase", new Vector3(-4.55f, 0.08f, 2.1f), new Vector3(0.62f, 0.1f, 0.62f), 0.04f, matIron, world);
            for (int i = 0; i < 3; i++)
            {
                spareFlag[i] = MakeSmallFlag("Flag" + i, world, 0.55f);
                spareFlag[i].position = new Vector3(-4.5f - i * 0.05f, 0.12f + i * 0.02f, 3.15f + i * 0.12f);
                spareFlag[i].rotation = Quaternion.Euler(0, -18 + i * 10f, 8f);
            }
            for (int i = 0; i < pileFlag.Length; i++)
            {
                pileFlag[i] = MakeSmallFlag("Pile" + i, world, 0.5f);
                pileFlag[i].gameObject.SetActive(false);
            }

            // 창가 빛 띠 — 코드로만 그린 짧은 그림자 가장자리
            NoShadow(MgfLook.Block("Sill", new Vector3(5.2f, 1.6f, 0.8f), new Vector3(0.16f, 3.2f, 7.2f), 0.02f, MgfLook.Lit(Hex("E8F0EA"), 0.4f, 0f), world));
        }

        void LayoutTri(Sheet s)
        {
            float h = 8.0f;
            float half = Mathf.Clamp(3.35f * s.bc / Mathf.Max(8, s.ab), 2.6f, 4.4f);
            A = new Vector3(0f, 0.07f, 3.85f);
            B = new Vector3(-half, 0.07f, 3.85f - h);
            C = new Vector3(half, 0.07f, 3.85f - h);
            A2 = new Vector2(A.x, A.z); B2 = new Vector2(B.x, B.z); C2 = new Vector2(C.x, C.z);
            midBc = (B + C) * 0.5f;
        }

        Vector3 Along(float cm, Sheet s)
        {
            int span = Mathf.Max(1, s.Span);
            float t = Mathf.Clamp01(cm / span);
            if (s.isPlumb)
            {
                var p = Vector3.Lerp(A, midBc, t);
                p.y = 0.16f;
                return p;
            }
            var d = Vector3.Lerp(A, B, t);
            var e = Vector3.Lerp(A, C, t);
            var m = (d + e) * 0.5f;
            m.y = 0.14f;
            return m;
        }

        Quaternion MillRot(Sheet s)
        {
            var across = DirAcross(s);
            across.y = 0f;
            if (across.sqrMagnitude < 1e-6f) across = Vector3.right;
            across.Normalize();
            var fwd = Vector3.Cross(across, Vector3.up);
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
            return Quaternion.LookRotation(fwd.normalized, Vector3.up);
        }

        float DeWidth(float cm, Sheet s)
        {
            int span = Mathf.Max(1, s.Span);
            float t = Mathf.Clamp01(cm / span);
            return Vector3.Distance(Vector3.Lerp(A, B, t), Vector3.Lerp(A, C, t));
        }

        Vector3 DirAcross(Sheet s)
        {
            if (s != null && s.isPlumb) return (C - B).normalized;
            var d = B - A; var e = C - A;
            // DE ∥ BC
            return (C - B).normalized;
        }

        void SetupSheet(Sheet s, bool demo)
        {
            LayoutTri(s);
            DrawStatic(s);
            PoseMilldae(true);
            cutSmall.gameObject.SetActive(false);
            stampCm.gameObject.SetActive(false);
            bool ring = s.kind == Kind.Cent || s.kind == Kind.Area;
            if (milldae) milldae.gameObject.SetActive(!ring);
            if (ringT) ringT.gameObject.SetActive(ring);
            RefreshLiveLabels(SnapTick());
            FrameCam();
        }

        void DrawStatic(Sheet s)
        {
            // 삼각기 메시를 매 장 교체하지 않고 점만 갱신
            RebuildPennant(s);
            ticks.Clear(); hemInk.Clear(); ghostInk.Clear();
            var ink = C32(InkC, 0.82f);
            var hemC = C32(Hem, 1f);
            var tickC = C32(InkC, 0.55f);
            hemInk.Seg(B2, C2, 0.14f, hemC);
            // 스티치
            var bc = C2 - B2; float L = bc.magnitude; var u = bc / Mathf.Max(1e-4f, L);
            for (float d = 0.12f; d < L; d += 0.22f)
                hemInk.Disc(B2 + u * d, 0.035f, C32(InkC, 0.35f), 8);

            int span = Mathf.Max(1, s.Span);
            for (int i = 1; i < span; i++)
            {
                float t = i / (float)span;
                var p = Vector2.Lerp(A2, B2, t);
                var q = Vector2.Lerp(A2, C2, t);
                var across = q - p;
                var n = across.normalized;
                ticks.Seg(p, p + n * 0.16f, i % 5 == 0 ? 0.045f : 0.028f, tickC);
                ticks.Seg(q, q - n * 0.16f, i % 5 == 0 ? 0.045f : 0.028f, tickC);
                if (i % 5 == 0 || span <= 10)
                {
                    int li = i % cmLbl.Length;
                    // labels posed in RefreshLiveLabels
                }
            }
            ticks.Seg(A2, B2, 0.035f, ink);
            ticks.Seg(A2, C2, 0.035f, ink);
            ticks.Seg(B2, C2, 0.04f, ink);

            if (s.ghostOn && s.misTick > 0 && s.misTick != s.target)
            {
                float t = s.misTick / (float)span;
                var p = Vector2.Lerp(A2, B2, t);
                var q = Vector2.Lerp(A2, C2, t);
                ghostInk.Seg(p, q, 0.045f, C32(InkC, 0.28f));
            }
            if (s.kind == Kind.Three && s.fAd > 0)
            {
                float t = s.fAd / (float)span;
                ghostInk.Seg(Vector2.Lerp(A2, B2, t), Vector2.Lerp(A2, C2, t), 0.04f, C32(InkC, 0.22f));
            }

            ticks.Apply(); hemInk.Apply(); ghostInk.Apply();

            vLbl[0].text = "A"; vLbl[0].transform.position = A + new Vector3(0, 0.12f, 0.42f);
            vLbl[1].text = "B"; vLbl[1].transform.position = B + new Vector3(-0.35f, 0.12f, -0.25f);
            vLbl[2].text = "C"; vLbl[2].transform.position = C + new Vector3(0.35f, 0.12f, -0.25f);
            vLbl[3].text = s.kind == Kind.Cent || s.kind == Kind.Area ? "D" : "";
            vLbl[3].transform.position = midBc + new Vector3(0, 0.12f, -0.4f);
            vLbl[4].text = ""; vLbl[5].text = "";
        }

        GameObject pennantGo;
        void RebuildPennant(Sheet s)
        {
            if (pennantGo) Object.Destroy(pennantGo);
            pennantGo = MakeTriMesh("Pennant", A, B, C, 0.07f, matCanvas, world);
            pennantGo.name = "Pennant";
            float hemLen = Vector3.Distance(B, C);
            var hemMid = (B + C) * 0.5f;
            MgfLook.Block("Hem3d", new Vector3(hemMid.x, 0.10f, hemMid.z), new Vector3(hemLen, 0.05f, 0.14f), 0.02f, MgfLook.Lit(Hem, 0.32f, 0f), pennantGo.transform);
        }

        void PoseMilldae(bool instant)
        {
            if (cur == null) return;
            var p = Along(tickVis, cur);
            var rot = MillRot(cur);
            float deW = DeWidth(tickVis, cur);
            float sx = Mathf.Max(0.22f, deW / 5.6f);
            if (cur.kind == Kind.Cent || cur.kind == Kind.Area)
            {
                if (ringT)
                {
                    ringT.position = p;
                    ringT.rotation = rot;
                    float sq = dropSquash > 0 ? 1f - 0.18f * Mathf.Sin(dropSquash * Mathf.PI) : 1f;
                    ringT.localScale = ringBaseScale * sq;
                }
            }
            else if (milldae)
            {
                milldae.position = p;
                milldae.rotation = rot;
                float sq = dropSquash > 0 ? 1f + 0.22f * Mathf.Sin(Mathf.Min(1f, dropSquash) * Mathf.PI) * (1f - Mathf.Clamp01(dropSquash - 0.4f)) : 1f;
                milldae.localScale = Vector3.Scale(millBaseScale, new Vector3(sx, sq, 1f));
                if (jiggleT > 0) milldae.position += milldae.right * Mathf.Sin(jiggleT * 52f) * 0.07f;
            }
            float t = tickVis / Mathf.Max(1, cur.Span);
            Dpt = Vector3.Lerp(A, B, t);
            Ept = Vector3.Lerp(A, C, t);
            PlaceMillLabel(SnapTick());
        }

        void PlaceMillLabel(int snap)
        {
            if (liveAd == null || cur == null) return;
            bool play = ph == Ph.Practice || ph == Ph.Play;
            liveAd.gameObject.SetActive(play);
            if (!play) return;
            int span = Mathf.Max(1, cur.Span);
            string txt;
            if (cur.kind == Kind.Similar) txt = "DE=" + snap + " cm";
            else if (cur.kind == Kind.Mid)
                txt = (snap * 2 == cur.bc) ? ("중점 " + snap + " cm") : (snap + " cm");
            else if (cur.kind == Kind.Cent || cur.kind == Kind.Area)
                txt = (cur.median > 0 && snap * 3 == cur.median * 2)
                    ? ("2:1  AG=" + snap + " cm")
                    : ("AG=" + snap + " cm");
            else if (cur.hopAc) txt = "AE=" + snap + " cm";
            else txt = "AD=" + snap + " cm";
            if (liveAd.text != txt) liveAd.text = txt;
            liveAd.color = Brass;
            liveAd.fontSize = 3.0f;
            var millP = Along(tickVis, cur);
            float t = tickVis / span;
            var towardA = A - millP; towardA.y = 0f;
            var across = DirAcross(cur);
            float deW = DeWidth(tickVis, cur);
            // 와이드 2단에서는 오른쪽 주문서가 삼각기 우측을 가리므로 라벨을 B쪽(왼쪽)에 둔다.
            Vector3 side = across * (land ? -1f : 1f) * Mathf.Clamp(deW * 0.18f, 0.25f, 0.70f);
            Vector3 lift = Vector3.up * 0.40f;
            // 하단 칸에서는 꼭짓점 쪽으로 끌어 올려 HUD·화면 밖과 겹치지 않게.
            if (t > 0.48f && towardA.sqrMagnitude > 1e-4f)
                liveAd.transform.position = millP + side + towardA.normalized * (0.55f + (t - 0.48f) * 1.9f) + lift;
            else
                liveAd.transform.position = millP + side + lift + new Vector3(0f, 0f, 0.18f);
        }

        void RefreshLiveLabels(int snap)
        {
            if (cur == null) return;
            int span = Mathf.Max(1, cur.Span);
            bool side = !cur.hideRatio && (ph == Ph.Practice || ph == Ph.Play);
            bool deTick = cur.kind == Kind.Similar || cur.kind == Kind.Mid || cur.isPlumb;
            if (liveDb)
            {
                liveDb.gameObject.SetActive(side && !deTick && span - snap > 0);
                if (side && !deTick)
                {
                    liveDb.text = cur.hopAc ? ("EC=" + (span - snap) + " cm") : ("DB=" + (span - snap) + " cm");
                    var p = Vector3.Lerp(A, B, (snap + span) * 0.5f / span);
                    liveDb.transform.position = p + new Vector3(-0.62f, 0.22f, -0.05f);
                }
            }
            PlaceMillLabel(snap);
            for (int i = 0; i < cmLbl.Length; i++)
            {
                int cm = i + 1;
                bool on = cm < span && (span <= 12 || cm % 2 == 0 || cm == snap);
                cmLbl[i].gameObject.SetActive(on);
                if (!on) continue;
                cmLbl[i].text = cm.ToString();
                var p = Vector3.Lerp(A, B, cm / (float)span);
                cmLbl[i].transform.position = p + new Vector3(-0.30f, 0.22f, 0.06f);
                cmLbl[i].color = cm == snap ? Brass : InkC;
                cmLbl[i].fontSize = cm == snap ? 2.8f : 2.2f;
            }
        }

        void FrameCam()
        {
            if (!cam) return;
            bool split = land && ph != Ph.Title && ph != Ph.End;
            float pane = split ? 1f - 380f / Mathf.Max(1, Screen.width) : 1f;
            cam.rect = new Rect(0f, 0f, Mathf.Clamp(pane, 0.55f, 1f), 1f);
            cam.transform.position = camHome;
            cam.transform.LookAt(lookHome);
            MgfLook.FitWidth(cam, land ? 26f : 30f, land ? 1.15f : 0.46f);
        }

        void ShowGhost(int cm)
        {
            if (cur == null || !ghostMill) return;
            ghostMill.gameObject.SetActive(true);
            ghostAlpha = 0.7f;
            var p = Along(cm, cur);
            ghostMill.position = p + Vector3.up * 0.03f;
            ghostMill.rotation = MillRot(cur);
            float sx = Mathf.Max(0.22f, DeWidth(cm, cur) / 5.2f);
            ghostMill.localScale = new Vector3(sx, 1f, 1f);
        }
        void HideGhost()
        {
            ghostAlpha = 0;
            if (ghostMill) ghostMill.gameObject.SetActive(false);
            if (fingerT) fingerT.gameObject.SetActive(false);
        }

        void PointAtMilldae()
        {
            if (!fingerT || milldae == null) return;
            fingerT.gameObject.SetActive(true);
            fingerT.position = milldae.position + new Vector3(0.15f, 0.55f, -0.2f);
        }

        void ImpaleFlag()
        {
            int i = Rules.Flags - Mathf.Max(0, st.lives) - 1;
            if (i < 0 || i >= spareFlag.Length) return;
            if (spareFlag[i])
            {
                spareFlag[i].position = spikeT.position + new Vector3(0.05f * i, 0.35f + i * 0.12f, 0.02f);
                spareFlag[i].rotation = Quaternion.Euler(-70f, 20f + i * 12f, 8f);
            }
            flagsLeftVis = Mathf.Max(0, st.lives);
            GitSound.Play("impale", 0.7f);
        }

        void ResetFlags()
        {
            flagsLeftVis = Rules.Flags;
            pileN = 0;
            for (int i = 0; i < spareFlag.Length; i++)
            {
                if (!spareFlag[i]) continue;
                spareFlag[i].gameObject.SetActive(true);
                spareFlag[i].position = new Vector3(-4.5f - i * 0.05f, 0.12f + i * 0.02f, 3.15f + i * 0.12f);
                spareFlag[i].rotation = Quaternion.Euler(0, -18 + i * 10f, 8f);
            }
            for (int i = 0; i < pileFlag.Length; i++)
                if (pileFlag[i]) pileFlag[i].gameObject.SetActive(false);
        }

        void PushPile()
        {
            if (pileN >= pileFlag.Length) return;
            var f = pileFlag[pileN];
            f.gameObject.SetActive(true);
            f.position = new Vector3(4.35f, 0.08f + pileN * 0.06f, -3.1f);
            f.rotation = Quaternion.Euler(0, 12f + pileN * 7f, 0);
            pileN++;
            swingAmp = 1f;
        }

        void Animate(float dt)
        {
            shimmerT += dt;
            if (brassScrew) brassScrew.localRotation = Quaternion.Euler(0, shimmerT * 25f, 0);
            if (dropSquash > 0) dropSquash = Mathf.Max(0, dropSquash - dt * 2.4f);
            if (jiggleT > 0) jiggleT = Mathf.Max(0, jiggleT - dt);
            if (splitT > 0) splitT = Mathf.Max(0, splitT - dt);
            if (slideT > 0) slideT = Mathf.Max(0, slideT - dt);
            if (revealT > 0) revealT = Mathf.Max(0, revealT - dt);
            if (pulseTm > 0) pulseTm = Mathf.Max(0, pulseTm - dt);
            if (refuseT > 0) refuseT = Mathf.Max(0, refuseT - dt);
            if (swingAmp > 0) swingAmp = Mathf.Max(0, swingAmp - dt * 1.6f);
            if (ghostAlpha > 0)
            {
                ghostAlpha = Mathf.Max(0, ghostAlpha - dt * 0.35f);
                if (ghostMill && ghostAlpha <= 0 && ph != Ph.Practice) ghostMill.gameObject.SetActive(false);
            }

            PoseMilldae(false);
            AnimateGhostFinger(dt);
            AnimateCut(dt);
            AnimateCounter(dt);
            BillboardLabels();
            FrameCam();
            TickRipples(dt);
            TickCta(dt);

            if (toastT > 0)
            {
                toastT -= dt;
                if (toastG) toastG.alpha = Mathf.Clamp01(toastT / 0.15f) * Mathf.Clamp01(toastDur > 0 ? toastT / toastDur * 8f : 1f);
                if (toastT <= 0 && toastG) toastG.alpha = 0;
            }

            if (shownScore < st.score)
            {
                shownScore = Mathf.MoveTowards(shownScore, st.score, dt * 480f);
                int v = Mathf.RoundToInt(shownScore);
                if (v != shownScoreInt) { shownScoreInt = v; if (scoreTxt) scoreTxt.text = v.ToString(); GitSound.Play("counter", 0.35f); }
            }

            int sec = ph == Ph.Play ? Mathf.CeilToInt(Mathf.Max(0, runLeft)) : -1;
            if (sec != lastTimeInt && timeTxt && ph == Ph.Play)
            {
                lastTimeInt = sec;
                timeTxt.text = sec.ToString();
                if (timeFill)
                {
                    float u = Mathf.Clamp01(runLeft / Rules.RunSec);
                    timeFill.rectTransform.anchorMax = new Vector2(u, 1f);
                }
            }
        }

        void AnimateGhostFinger(float dt)
        {
            bool show = ph == Ph.Practice && !lockedThis;
            if (!fingerT) return;
            fingerT.gameObject.SetActive(show);
            if (!show || cur == null) return;
            ghostT += dt;
            float loop = 1.2f;
            float u = (ghostT % loop) / loop;
            // 밀대를 따라가다가 목표 칸에서 탭 모션
            float aim = cur.target;
            float pos = tickVis;
            var p = Along(pos, cur);
            float pulse = (Mathf.Abs(pos - aim) < 0.55f) ? 1f + 0.35f * Mathf.Sin(ghostT * 10f) : 1f;
            fingerT.position = p + new Vector3(0.05f, 0.55f * ghostScale * pulse, -0.12f);
            fingerT.localScale = Vector3.one * (0.9f * ghostScale * pulse);
            if (ghostMill)
            {
                ghostMill.gameObject.SetActive(true);
                ghostMill.position = Along(aim, cur) + Vector3.up * 0.04f;
                ghostMill.rotation = MillRot(cur);
                float sx = Mathf.Max(0.22f, DeWidth(aim, cur) / 5.2f);
                ghostMill.localScale = new Vector3(sx, 1f, 1f);
            }
        }

        void AnimateCut(float dt)
        {
            if (splitT > 0 && cutSmall)
            {
                cutSmall.gameObject.SetActive(true);
                float k = 1f - Mathf.Clamp01(splitT / 1.15f);
                var from = Along(cur != null ? Mathf.Max(1, cur.ad) : 4, cur);
                var to = lastOk
                    ? new Vector3(4.35f, 0.12f + pileN * 0.06f, -3.1f)
                    : (spikeT.position + new Vector3(0.04f, 0.4f, 0));
                cutSmall.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, k));
                cutSmall.rotation = Quaternion.Euler(0, 18f * k, lastOk ? 0 : -60f * k);
                if (k > 0.92f && lastOk && slideT < 0.15f) { PushPile(); cutSmall.gameObject.SetActive(false); }
            }
            else if (cutSmall && splitT <= 0 && slideT <= 0) cutSmall.gameObject.SetActive(false);

            if (pileN > 0 && swingAmp > 0 && pileFlag[pileN - 1])
            {
                var f = pileFlag[pileN - 1];
                f.localRotation = Quaternion.Euler(0, 12f + (pileN - 1) * 7f, Mathf.Sin(Time.time * 9f) * 10f * swingAmp);
            }
        }

        void AnimateCounter(float dt)
        {
            if (cmGoal <= 0 || !stampCm) { if (stampCm) stampCm.gameObject.SetActive(false); return; }
            stampCm.gameObject.SetActive(revealT > 0);
            if (revealT <= 0) return;
            shownCm = Mathf.SmoothDamp(shownCm, cmGoal, ref cmVel, 0.18f);
            int v = Mathf.RoundToInt(shownCm);
            string unit = Words.CounterUnit(cur);
            bool area = cur != null && cur.kind == Kind.Area;
            stampCm.text = area ? ("△GBC " + v + " " + unit) : (v + " " + unit);
            stampCm.transform.position = area
                ? Vector3.Lerp(A, midBc, 0.78f) + new Vector3(0, 0.32f + 0.12f * (1f - revealT), 0.05f)
                : midBc + new Vector3(0, 0.35f + 0.15f * (1f - revealT), 0.2f);
            float punch = 1f + 0.18f * Mathf.Sin(Mathf.Clamp01(1.15f - revealT) * Mathf.PI);
            stampCm.transform.localScale = Vector3.one * punch;
        }

        void BillboardLabels()
        {
            if (!cam) return;
            var f = cam.transform.forward; f.y = 0; if (f.sqrMagnitude < 1e-4f) return;
            var rot = Quaternion.LookRotation(f);
            foreach (var t in vLbl) if (t) t.transform.rotation = rot;
            foreach (var t in cmLbl) if (t) t.transform.rotation = rot;
            if (liveAd) liveAd.transform.rotation = rot;
            if (liveDb) liveDb.transform.rotation = rot;
            if (stampCm) stampCm.transform.rotation = rot;
        }
    }
}
