// 유리칼 — 월드(형광 재단 매트 · 아크릴 네 조각 · 먹선 막대 · 유리칼 머리 · 브레이커 · 고철 통).
// 1 월드 단위 = 1 cm. 카메라는 위에서 수직으로 내려다보는 정사영(길이 왜곡 없음 — 이등분을 눈으로 견줄 수 있게).
// 정적 레이어(매트 텍스처)는 부팅 때 1회 굽고, 장 메시는 O·교각이 바뀔 때만 다시 쓴다(매 프레임 할당 없음).
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.YuriKal
{
    /// <summary>선·면을 한 메시에 모아 그리는 먹선 붓(정점색). 목록은 재사용한다.</summary>
    public class Ink
    {
        public readonly Mesh mesh = new Mesh();
        readonly List<Vector3> v = new List<Vector3>(2048);
        readonly List<Color32> c = new List<Color32>(2048);
        readonly List<int> t = new List<int>(4096);
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
        public void Disc(Vector2 o, float r, Color32 col, int n = 20)
        {
            int i0 = v.Count; v.Add(P(o)); c.Add(col);
            for (int k = 0; k <= n; k++) { float a = k * Mathf.PI * 2 / n; v.Add(P(o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r)); c.Add(col); }
            for (int k = 0; k < n; k++) { t.Add(i0); t.Add(i0 + 1 + k); t.Add(i0 + 2 + k); }
        }
        public void Arc(Vector2 o, float r, float a0, float a1, float w, Color32 col, int n = 18)
        {
            int i0 = v.Count;
            for (int k = 0; k <= n; k++)
            {
                float a = Mathf.Lerp(a0, a1, (float)k / n) * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                v.Add(P(o + d * (r - w * 0.5f))); v.Add(P(o + d * (r + w * 0.5f))); c.Add(col); c.Add(col);
            }
            for (int k = 0; k < n; k++) { int a = i0 + k * 2; t.Add(a); t.Add(a + 1); t.Add(a + 3); t.Add(a); t.Add(a + 3); t.Add(a + 2); }
        }
        public void Ring(Vector2 o, float r, float w, Color32 col, int n = 28) => Arc(o, r, 0, 360, w, col, n);
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
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(200, 10, 200));
        }
    }

    public partial class YuriKalGame
    {
        // ── 팔레트 (chosen.json art_direction.palette)
        static readonly Color MatC = Hex("B7D14A"), Milk = Hex("F4F7EA"), InkC = Hex("2B2F36"), Mag = Hex("C2316B"), Orange = Hex("E85D04"), Moss = Hex("3A4A1C");
        static Color Hex(string h) => MgfLook.Hex(h);
        // 선형 색공간 프로젝트: 정점색은 변환되지 않으므로 여기서 sRGB → 선형으로 바꿔 넣는다(안 그러면 먹색이 회색으로 뜬다)
        static Color32 C32(Color c, float a = 1f) { var l = c.linear; return new Color32((byte)(l.r * 255), (byte)(l.g * 255), (byte)(l.b * 255), (byte)(Mathf.Clamp01(a) * 255)); }

        // CreatePrimitive 가 붙이는 콜라이더 형식이 엔진 코드 스트리핑으로 빠지지 않게 붙잡아 둔다(원 찍어와 같은 방법)
        static readonly System.Type[] keepTypes = { typeof(SphereCollider), typeof(CapsuleCollider), typeof(MeshCollider), typeof(BoxCollider) };
        const float TopY = 0.32f;           // 아크릴 윗면 높이(cm)
        const float BreakerZ = 11.2f, BinZ = -11.4f;

        Camera cam;
        Transform world, sheetRoot, headRoot, rotHandle, rivet, frostWeb, lampT;
        readonly Transform[] pieceT = new Transform[4];
        readonly Mesh[] pieceMesh = new Mesh[4];
        readonly Vector3[][] pieceV = new Vector3[4][];
        readonly Vector3[][] pieceN = new Vector3[4][];
        Ink ink, guide, glow;
        MeshRenderer glowR, frostR, matFrostR;
        Material glowMat, frostMat, matFrostMat, inkMat;
        TextMeshPro[] vLbl = new TextMeshPro[4], lenLbl = new TextMeshPro[4], areaLbl = new TextMeshPro[4];
        TextMeshPro angLbl, caliperLbl, breakerLbl, binLbl;
        Transform wheelT;
        Transform binT, breakerT, fingerT, pulseT, trailT;
        Light lamp;

        // ─────────────────────────────── 월드 만들기
        void BuildWorld()
        {
            if (keepTypes.Length == 0) Debug.Log("keep");
            MgfLook.Quality(80f);
            QualitySettings.shadowDistance = 80f;
            if (!MgfBridge.LowGfx) QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            MgfLook.Sky(Hex("DDE6C0"), Hex("B7D14A"), Hex("3A4A1C"), 1.0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.64f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.52f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.32f, 0.28f);
            var sun = MgfLook.Sun(new Vector3(58, -32, 0), new Color(1f, 0.97f, 0.9f), 0.95f, 0.45f);
            sun.shadowBias = 0.02f; sun.shadowNormalBias = 0.2f;

            cam = MgfLook.Camera(new Vector3(0, 40, 0), Vector3.zero, 30f);
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.orthographic = true;
            cam.orthographicSize = 14;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = MatC;
            cam.nearClipPlane = 1f; cam.farClipPlane = 100f;

            world = new GameObject("World").transform;

            // 매트: 5 cm 타일 텍스처(1 cm 흰 인쇄 격자, 5 cm 굵은 선, 칼집 스크래치) — 1회 베이크
            var matTex = MatTexture();
            var matMesh = new Mesh { name = "Mat" };
            const float M = 200f;
            matMesh.vertices = new[] { new Vector3(-M, 0, -M), new Vector3(-M, 0, M), new Vector3(M, 0, M), new Vector3(M, 0, -M) };
            matMesh.uv = new[] { new Vector2(-M / 5, -M / 5), new Vector2(-M / 5, M / 5), new Vector2(M / 5, M / 5), new Vector2(M / 5, -M / 5) };
            matMesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            matMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            var matGo = new GameObject("Mat", typeof(MeshFilter), typeof(MeshRenderer));
            matGo.transform.SetParent(world, false);
            matGo.GetComponent<MeshFilter>().sharedMesh = matMesh;
            var mr = matGo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = MgfLook.Lit(Color.white, 0.18f, 0f, null, matTex);
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = true;

            inkMat = MgfLook.Alpha(Color.white);
            glowMat = new Material(MgfLook.Additive(Color.white)) { name = "YkGlow" };
            frostMat = new Material(MgfLook.Additive(Color.white)) { name = "YkFrost" };
            matFrostMat = new Material(MgfLook.Alpha(Color.white)) { name = "YkMatFrost" };

            // 브레이커(위, 철) · 고철 통(아래, 자투리 마젠타)
            var steel = MgfLook.Lit(Hex("8E979F"), 0.62f, 0.75f);
            var steelDark = MgfLook.Lit(Hex("3B4047"), 0.5f, 0.6f);
            breakerT = new GameObject("Breaker").transform; breakerT.SetParent(world, false); breakerT.localPosition = new Vector3(0, 0, BreakerZ);
            MgfLook.Block("BreakerBar", new Vector3(0, 0.55f, 0.35f), new Vector3(26f, 1.1f, 1.5f), 0.25f, steel, breakerT);
            MgfLook.Block("BreakerLip", new Vector3(0, 0.2f, -0.75f), new Vector3(26f, 0.4f, 0.5f), 0.12f, steelDark, breakerT);
            foreach (float bx in new[] { -11.5f, -8.5f, -5.5f, 5.5f, 8.5f, 11.5f })
                MgfLook.Block("Bolt", new Vector3(bx, 1.12f, 0.35f), new Vector3(0.5f, 0.12f, 0.5f), 0.2f, steelDark, breakerT);
            breakerLbl = Flat("브레이커  ↑", 10f, InkC); breakerLbl.transform.SetParent(breakerT, false); breakerLbl.transform.localPosition = new Vector3(0, 1.16f, 0.4f);
            breakerLbl.gameObject.SetActive(true);

            binT = new GameObject("Bin").transform; binT.SetParent(world, false); binT.localPosition = new Vector3(0, 0, BinZ);
            var magM = MgfLook.Lit(Mag, 0.45f, 0.05f);
            var magDark = MgfLook.Lit(Hex("7A1C42"), 0.3f, 0f);
            MgfLook.Block("BinBack", new Vector3(0, 0.45f, -0.9f), new Vector3(24f, 0.9f, 0.5f), 0.18f, magM, binT);
            MgfLook.Block("BinFloor", new Vector3(0, 0.08f, 0f), new Vector3(24f, 0.16f, 2.2f), 0.06f, magDark, binT);
            MgfLook.Block("BinL", new Vector3(-12f, 0.45f, 0f), new Vector3(0.5f, 0.9f, 2.3f), 0.18f, magM, binT);
            MgfLook.Block("BinR", new Vector3(12f, 0.45f, 0f), new Vector3(0.5f, 0.9f, 2.3f), 0.18f, magM, binT);
            // 자투리(이미 버려진 아크릴 조각)
            var off = MgfLook.Lit(Hex("E6C9D4"), 0.8f, 0f);
            float[] ox = { -8.2f, -4.5f, 5.3f, 9.1f };
            for (int i = 0; i < ox.Length; i++)
            {
                var o = MgfLook.Block("Offcut", new Vector3(ox[i], 0.26f, 0.1f), new Vector3(1.6f + i * 0.3f, 0.2f, 0.9f), 0.05f, off, binT);
                o.transform.localRotation = Quaternion.Euler(0, 12 + i * 37, 0);
            }
            binLbl = Flat("↓  고철 통", 10f, Milk); binLbl.transform.SetParent(binT, false); binLbl.transform.localPosition = new Vector3(0, 0.95f, -0.9f);
            binLbl.outlineWidth = 0.15f; binLbl.outlineColor = new Color32(90, 20, 50, 255);
            binLbl.gameObject.SetActive(true);

            // 작업등(느리게 흐르는 반사광 — 가만히 둬도 살아 있는 화면)
            lamp = new GameObject("ShopLamp").AddComponent<Light>();
            lamp.type = LightType.Point; lamp.range = 34f; lamp.intensity = 1.4f; lamp.color = new Color(1f, 0.93f, 0.8f);
            lamp.shadows = LightShadows.None; lamp.renderMode = LightRenderMode.ForcePixel;
            lampT = lamp.transform;
            if (MgfBridge.LowGfx) { lamp.enabled = false; sun.shadows = LightShadows.Hard; }   // 소프트웨어 렌더러: 추가 픽셀 광원·부드러운 그림자 끔

            // 매트 서리(오답 뒤 1.5초 — 매트 왼쪽 1/4 을 점유)
            var mf = new GameObject("MatFrost", typeof(MeshFilter), typeof(MeshRenderer));
            mf.transform.SetParent(world, false);
            var mfm = new Mesh();
            mfm.vertices = new[] { new Vector3(-30, 0.05f, -13), new Vector3(-30, 0.05f, 13), new Vector3(-6.5f, 0.05f, 13), new Vector3(-6.5f, 0.05f, -13) };
            mfm.colors32 = new[] { new Color32(235, 245, 255, 150), new Color32(235, 245, 255, 150), new Color32(235, 245, 255, 0), new Color32(235, 245, 255, 0) };
            mfm.uv = new Vector2[4]; mfm.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mf.GetComponent<MeshFilter>().sharedMesh = mfm;
            matFrostR = mf.GetComponent<MeshRenderer>(); matFrostR.sharedMaterial = matFrostMat; matFrostR.shadowCastingMode = ShadowCastingMode.Off;
            matFrostR.enabled = false;

            BuildSheetView();
        }

        TextMeshPro Flat(string s, float size, Color c)
        {
            var t = MgfText.World(s, Vector3.zero, size, c, world);
            t.transform.rotation = Quaternion.Euler(90, 0, 0);
            t.gameObject.SetActive(false);
            return t;
        }

        void BuildSheetView()
        {
            sheetRoot = new GameObject("Sheet").transform;
            sheetRoot.SetParent(world, false);
            var acr = MgfLook.Lit(Milk, 0.9f, 0f);
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("Piece" + i, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(sheetRoot, false);
                pieceMesh[i] = new Mesh { name = "Piece" }; pieceMesh[i].MarkDynamic();
                pieceV[i] = new Vector3[15]; pieceN[i] = new Vector3[15];
                pieceMesh[i].vertices = pieceV[i];
                go.GetComponent<MeshFilter>().sharedMesh = pieceMesh[i];
                var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = acr; r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true;
                pieceT[i] = go.transform;
                areaLbl[i] = Flat("", 12f, InkC); areaLbl[i].transform.SetParent(go.transform, false);
                areaLbl[i].transform.localPosition = new Vector3(0, TopY + 0.05f, 0);
            }
            ink = MakeInk("Ink", TopY + 0.012f, inkMat, sheetRoot);
            guide = MakeInk("Guide", TopY + 0.03f, inkMat, sheetRoot);
            glow = MakeInk("Glow", TopY + 0.04f, glowMat, sheetRoot, out glowR);
            for (int i = 0; i < 4; i++)
            {
                vLbl[i] = Flat("ABCD"[i].ToString(), 15f, InkC); vLbl[i].transform.SetParent(sheetRoot, false);
                vLbl[i].fontStyle = FontStyles.Bold;
                lenLbl[i] = Flat("", 11f, InkC); lenLbl[i].transform.SetParent(sheetRoot, false);
            }
            angLbl = Flat("", 10f, InkC); angLbl.transform.SetParent(sheetRoot, false);
            caliperLbl = Flat("", 12f, Milk); caliperLbl.transform.SetParent(sheetRoot, false);
            caliperLbl.outlineWidth = 0.3f; caliperLbl.outlineColor = C32(InkC);

            // 서리 거미줄(1회 생성 — 오답 때 O 에서 퍼진다)
            var fw = new Ink(0f);
            var rr = new System.Random(5);
            for (int k = 0; k < 11; k++)
            {
                float a = k * 32.7f + (float)rr.NextDouble() * 14f, rad = 3.2f + (float)rr.NextDouble() * 2.2f;
                Vector2 p = Vector2.zero;
                for (int s = 0; s < 4; s++)
                {
                    float aa = (a + (float)(rr.NextDouble() - 0.5) * 18f) * Mathf.Deg2Rad;
                    var q = p + new Vector2(Mathf.Cos(aa), Mathf.Sin(aa)) * rad / 4f;
                    fw.Seg(p, q, 0.12f - s * 0.02f, new Color32(225, 240, 255, 255));
                    if (s == 1) { float ba = aa + 0.7f; fw.Seg(q, q + new Vector2(Mathf.Cos(ba), Mathf.Sin(ba)) * 0.9f, 0.06f, new Color32(225, 240, 255, 220)); }
                    p = q;
                }
            }
            for (int ring = 1; ring <= 3; ring++)
                for (int k = 0; k < 9; k++)
                {
                    float a0 = k * 40f + ring * 13f;
                    fw.Arc(Vector2.zero, ring * 1.1f, a0, a0 + 22f + ring * 4f, 0.06f, new Color32(225, 240, 255, (byte)(235 - ring * 30)), 5);
                }
            fw.Apply();
            var fgo = new GameObject("FrostWeb", typeof(MeshFilter), typeof(MeshRenderer));
            fgo.transform.SetParent(sheetRoot, false);
            fgo.GetComponent<MeshFilter>().sharedMesh = fw.mesh;
            fgo.transform.localScale = Vector3.one;
            frostR = fgo.GetComponent<MeshRenderer>(); frostR.sharedMaterial = frostMat; frostR.shadowCastingMode = ShadowCastingMode.Off;
            frostWeb = fgo.transform; frostR.enabled = false;

            // 유리칼 머리(탄화 휠 하우징 + 황동 나사 + 주황 손잡이 링) — 점 O
            headRoot = new GameObject("CutterHead").transform; headRoot.SetParent(sheetRoot, false);
            var body = MgfLook.Lit(Hex("3B4047"), 0.72f, 0.85f);
            var brass = MgfLook.Lit(Hex("C9A34A"), 0.75f, 0.9f);
            var wheel = MgfLook.Lit(Hex("E3E7EA"), 0.92f, 1f);
            var orange = MgfLook.Lit(Orange, 0.5f, 0.1f);
            MgfLook.Prim(PrimitiveType.Cylinder, "Housing", new Vector3(0, 0.38f, 0), new Vector3(1.15f, 0.2f, 1.15f), body, headRoot, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "Ring", new Vector3(0, 0.3f, 0), new Vector3(1.55f, 0.06f, 1.55f), orange, headRoot, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "Screw", new Vector3(0, 0.6f, 0), new Vector3(0.36f, 0.04f, 0.36f), brass, headRoot, false);
            var wh = MgfLook.Prim(PrimitiveType.Cylinder, "Wheel", new Vector3(0, 0.62f, 0), new Vector3(0.95f, 0.025f, 0.95f), wheel, headRoot, false);
            wheelT = wh.transform;
            wh.transform.localRotation = Quaternion.Euler(0, 0, 90);
            wh.name = "Wheel";
            pulseT = MakeRingObj("Pulse", 1.0f, new Color(1f, 0.6f, 0.2f, 0.9f), sheetRoot);
            trailT = MakeRingObj("Trail", 0.9f, new Color(1f, 0.85f, 0.6f, 1f), headRoot);
            trailT.gameObject.SetActive(false);

            rivet = MgfLook.Prim(PrimitiveType.Cylinder, "Rivet", Vector3.zero, new Vector3(0.7f, 0.05f, 0.7f), MgfLook.Lit(Hex("8E979F"), 0.7f, 0.8f), sheetRoot, false).transform;

            rotHandle = new GameObject("RotHandle").transform; rotHandle.SetParent(sheetRoot, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "Knob", new Vector3(0, 0.25f, 0), new Vector3(1.5f, 0.12f, 1.5f), orange, rotHandle, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "KnobCap", new Vector3(0, 0.4f, 0), new Vector3(0.7f, 0.05f, 0.7f), Lit(Milk), rotHandle, false);

            // 유령 손가락(부드러운 흰 점 + 먹 테두리)
            fingerT = new GameObject("Finger", typeof(MeshFilter), typeof(MeshRenderer)).transform;
            fingerT.SetParent(sheetRoot, false);
            var fq = new Ink(0f);
            fq.Disc(Vector2.zero, 0.75f, new Color32(255, 255, 255, 200), 24);
            fq.Ring(Vector2.zero, 0.78f, 0.1f, C32(InkC, 0.8f));
            fq.Disc(new Vector2(0.28f, -0.9f), 0.5f, new Color32(255, 255, 255, 110), 16);
            fq.Apply();
            fingerT.GetComponent<MeshFilter>().sharedMesh = fq.mesh;
            fingerT.GetComponent<MeshRenderer>().sharedMaterial = inkMat;
            fingerT.gameObject.SetActive(false);
        }

        static Material Lit(Color c) => MgfLook.Lit(c, 0.6f, 0f);

        Transform MakeRingObj(string name, float r, Color col, Transform parent)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            var i = new Ink(0f); i.Ring(Vector2.zero, r, 0.12f, C32(col, col.a), 32); i.Apply();
            go.GetComponent<MeshFilter>().sharedMesh = i.mesh;
            var m = go.GetComponent<MeshRenderer>(); m.sharedMaterial = glowMat; m.shadowCastingMode = ShadowCastingMode.Off;
            return go.transform;
        }

        Ink MakeInk(string name, float y, Material m, Transform parent) => MakeInk(name, y, m, parent, out _);
        Ink MakeInk(string name, float y, Material m, Transform parent, out MeshRenderer r)
        {
            var i = new Ink(y);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = i.mesh;
            r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return i;
        }

        static Texture2D MatTexture()
        {
            const int S = 500;   // 5 cm → 100 px/cm
            var px = new Color32[S * S];
            var rnd = new System.Random(3);
            var baseC = MatC;
            // 값 잡음(미세 얼룩)
            var noise = new float[26 * 26];
            for (int i = 0; i < noise.Length; i++) noise[i] = (float)rnd.NextDouble();
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float fx = x / 20f, fy = y / 20f; int ix = (int)fx % 25, iy = (int)fy % 25;
                    float tx = fx - (int)fx, ty = fy - (int)fy;
                    float n = Mathf.Lerp(Mathf.Lerp(noise[iy * 26 + ix], noise[iy * 26 + ix + 1], tx), Mathf.Lerp(noise[(iy + 1) * 26 + ix], noise[(iy + 1) * 26 + ix + 1], tx), ty);
                    var c = baseC * (0.95f + n * 0.07f);
                    // 1 cm 인쇄 격자(흰색) · 5 cm 굵은 선
                    int mx = x % 100, my = y % 100;
                    float dx = Mathf.Min(mx, 100 - mx), dy = Mathf.Min(my, 100 - my);
                    bool major = x < 4 || x > S - 4 || y < 4 || y > S - 4;
                    float w = major ? 3.4f : 1.6f;
                    float line = Mathf.Max(Mathf.Clamp01(w - dx), Mathf.Clamp01(w - dy));
                    // 0.5 cm 짧은 눈금 점
                    if ((x % 50 == 0 || x % 50 == 49) && my < 8) line = Mathf.Max(line, 0.5f);
                    c = Color.Lerp(c, new Color(0.97f, 1f, 0.9f), line * (major ? 0.85f : 0.6f));
                    c.a = 1;
                    px[y * S + x] = c;
                }
            // 칼집 스크래치(자가 치유 매트의 흔적)
            for (int k = 0; k < 60; k++)
            {
                float x0 = (float)rnd.NextDouble() * S, y0 = (float)rnd.NextDouble() * S, a = (float)rnd.NextDouble() * Mathf.PI, L = 20 + (float)rnd.NextDouble() * 90;
                float dark = rnd.NextDouble() < 0.5 ? 0.9f : 1.06f;
                for (float s = 0; s < L; s += 0.7f)
                {
                    int x = ((int)(x0 + Mathf.Cos(a) * s) % S + S) % S, y = ((int)(y0 + Mathf.Sin(a) * s) % S + S) % S;
                    var c = (Color)px[y * S + x]; c *= dark; c.a = 1; px[y * S + x] = c;
                }
            }
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "YkMat", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4, filterMode = FilterMode.Trilinear };
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        // ─────────────────────────────── 장 기하(시트 로컬 cm, x-z 평면)
        Vector2 u1, u2, P0;          // 막대 방향 · 사선 격자 원점
        Vector2 Ol, Al, Bl, Cl, Dl;  // 현재 꼭짓점(로컬)
        Vector2 oVis;                // 화면의 O(속도 상한으로 손가락을 쫓는다)

        static Vector2 Dir(float deg) => new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));

        /// <summary>새 장: 목표(이등분점)가 로컬 원점에 오도록 격자 원점 P0 를 둔다.</summary>
        void InitFrame(Sheet s)
        {
            u1 = Dir(s.phi); u2 = Dir(s.phi + s.theta);
            P0 = -(s.L1 / 20f) * u1 - (s.L2 / 20f) * u2;
            Recompute(s);
            oVis = Ol;
        }

        void Recompute(Sheet s)
        {
            u1 = Dir(s.phi); u2 = Dir(s.phi + s.theta);
            Ol = P0 + (s.s1 / 10f) * u1 + (s.s2 / 10f) * u2;
            Al = Ol - (s.s1 / 10f) * u1; Cl = Ol + (s.CO / 10f) * u1;
            Bl = Ol - (s.s2 / 10f) * u2; Dl = Ol + (s.DO / 10f) * u2;
        }

        /// <summary>교각을 돌릴 때: O 는 제자리, 막대 BD 가 O 둘레로 돈다 → 격자 원점을 다시 잡는다.</summary>
        void Rotated(Sheet s)
        {
            u2 = Dir(s.phi + s.theta);
            P0 = Ol - (s.s1 / 10f) * u1 - (s.s2 / 10f) * u2;
            Recompute(s);
        }

        /// <summary>로컬 점 → 사선 좌표(mm).</summary>
        void Oblique(Vector2 p, out double a, out double b)
        {
            var d = p - P0;
            double det = u1.x * u2.y - u1.y * u2.x;
            a = (d.x * u2.y - d.y * u2.x) / det * 10.0;
            b = (u1.x * d.y - u1.y * d.x) / det * 10.0;
        }

        // ─────────────────────────────── 장 그리기
        void RebuildPieces()
        {
            Vector2[] q = { Al, Bl, Cl, Dl };
            for (int i = 0; i < 4; i++)
            {
                Vector2 a = q[i], b = q[(i + 1) % 4], o = Ol;
                var cen = (a + b + o) / 3f;
                pieceT[i].localPosition = new Vector3(cen.x, 0, cen.y);
                var V = pieceV[i]; var N = pieceN[i];
                a -= cen; b -= cen; o -= cen;
                Vector3 A = new Vector3(a.x, TopY, a.y), B = new Vector3(b.x, TopY, b.y), O = new Vector3(o.x, TopY, o.y);
                V[0] = A; V[1] = B; V[2] = O; N[0] = N[1] = N[2] = Vector3.up;
                Vector3[] top = { A, B, O };
                for (int e = 0; e < 3; e++)
                {
                    var p = top[e]; var r = top[(e + 1) % 3];
                    int k = 3 + e * 4;
                    V[k] = p; V[k + 1] = r; V[k + 2] = new Vector3(r.x, 0.01f, r.z); V[k + 3] = new Vector3(p.x, 0.01f, p.z);
                    var mid = (p + r) * 0.5f; var outn = new Vector3(mid.x, 0, mid.z);
                    var ed = r - p; var nrm = new Vector3(ed.z, 0, -ed.x).normalized;
                    if (Vector3.Dot(nrm, outn) < 0) nrm = -nrm;
                    N[k] = N[k + 1] = N[k + 2] = N[k + 3] = nrm;
                }
                var m = pieceMesh[i];
                m.vertices = V; m.normals = N;
                var tris = triBuf;
                int ti = 0;
                AddTri(tris, ref ti, V, 0, 1, 2, Vector3.up);
                for (int e = 0; e < 3; e++)
                {
                    int k = 3 + e * 4;
                    AddTri(tris, ref ti, V, k, k + 1, k + 2, N[k]);
                    AddTri(tris, ref ti, V, k, k + 2, k + 3, N[k]);
                }
                m.triangles = tris;
                m.RecalculateBounds();
                pieceT[i].localRotation = Quaternion.identity;
            }
        }
        readonly int[] triBuf = new int[21];

        static void AddTri(int[] t, ref int i, Vector3[] v, int a, int b, int c, Vector3 want)
        {
            var n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (Vector3.Dot(n, want) < 0) { int x = b; b = c; c = x; }
            t[i++] = a; t[i++] = b; t[i++] = c;
        }

        /// <summary>먹선: 막대(1 cm 눈금 · 중점 턱) · 사각형 변 · 교각 표시 · 잠긴 장의 조건 표시.</summary>
        void RebuildInk(Sheet s, bool numbers)
        {
            ink.Clear();
            var ic = C32(InkC); var soft = C32(InkC, 0.5f);
            bool pair1 = numbers && s.s1 == s.CO, pair2 = numbers && s.s2 == s.DO;
            // 사각형 변(가는 칼금)
            ink.Seg(Al, Bl, 0.07f, soft); ink.Seg(Bl, Cl, 0.07f, soft); ink.Seg(Cl, Dl, 0.07f, soft); ink.Seg(Dl, Al, 0.07f, soft);
            Bar(Al, Cl, s.L1, pair1 ? 0.28f : 0.16f, ic);
            Bar(Bl, Dl, s.L2, pair2 ? 0.28f : 0.16f, ic);
            // 교각 ∠AOB
            if (!s.Locked || s.lk == Lock.Kite || s.lk == Lock.Square || s.lk == Lock.Rhombus)
            {
                var oa = -u1; var ob = -u2;
                if (s.theta == 90)
                {
                    // 직각 표시(유리칼 머리 반지름 ≈ 1.3 cm 바깥까지 보이게)
                    float r = 1.55f;
                    ink.Seg(Ol + oa * r, Ol + oa * r + ob * r, 0.08f, ic); ink.Seg(Ol + ob * r, Ol + oa * r + ob * r, 0.08f, ic);
                }
                else if (!s.Locked)
                {
                    float a0 = Mathf.Atan2(oa.y, oa.x) * Mathf.Rad2Deg, a1 = Mathf.Atan2(ob.y, ob.x) * Mathf.Rad2Deg;
                    float d = Mathf.DeltaAngle(a0, a1);
                    ink.Arc(Ol, 2.0f, a0, a0 + d, 0.07f, ic, 20);
                }
            }
            CondMarks(s);
            ink.Apply();
        }

        void Bar(Vector2 a, Vector2 b, int Lmm, float w, Color32 col)
        {
            ink.Seg(a, b, w, col);
            var d = (b - a).normalized; var n = new Vector2(-d.y, d.x);
            for (int mm = 10; mm < Lmm; mm += 10)
            {
                var p = a + d * (mm / 10f);
                if (mm * 2 == Lmm) continue;
                ink.Seg(p - n * 0.2f, p + n * 0.2f, 0.06f, col);
            }
            // 중점 턱(항상 같은 모양 — O 가 가까워져도 색·크기가 바뀌지 않는다)
            var m = a + d * (Lmm / 20f);
            ink.Tri(m + n * 0.12f - d * 0.3f, m + n * 0.12f + d * 0.3f, m + n * 0.75f, col);
            ink.Tri(m - n * 0.12f - d * 0.3f, m - n * 0.12f + d * 0.3f, m - n * 0.75f, col);
            ink.Disc(a, 0.2f, col, 10); ink.Disc(b, 0.2f, col, 10);
        }

        void Ticks(Vector2 a, Vector2 b, int count, Color32 col)
        {
            var m = (a + b) * 0.5f; var d = (b - a).normalized; var n = new Vector2(-d.y, d.x);
            for (int k = 0; k < count; k++)
            {
                var p = m + d * ((k - (count - 1) * 0.5f) * 0.2f);
                ink.Seg(p - n * 0.3f, p + n * 0.3f, 0.06f, col);
            }
        }

        void ParallelMark(Vector2 a, Vector2 b, Vector2 dir, Color32 col)
        {
            var m = (a + b) * 0.5f;
            ink.Chevron(m + dir.normalized * 0.35f, dir, 0.45f, 0.07f, col);
        }

        void AngleMark(Vector2 v, Vector2 p, Vector2 q, int count, Color32 col)
        {
            var dp = (p - v).normalized; var dq = (q - v).normalized;
            float a0 = Mathf.Atan2(dp.y, dp.x) * Mathf.Rad2Deg, d = Mathf.DeltaAngle(a0, Mathf.Atan2(dq.y, dq.x) * Mathf.Rad2Deg);
            for (int k = 0; k < count; k++) ink.Arc(v, 0.9f + k * 0.22f, a0, a0 + d, 0.06f, col, 12);
        }

        void CondMarks(Sheet s)
        {
            var mc = C32(Mag);
            switch (s.lk)
            {
                case Lock.CondG:
                    ParallelMark(Al, Bl, Bl - Al, mc); ParallelMark(Dl, Cl, Cl - Dl, mc);
                    Ticks(Al, Bl, 1, mc); Ticks(Dl, Cl, 1, mc); break;
                case Lock.Trapezoid:
                    ParallelMark(Al, Bl, Bl - Al, mc); ParallelMark(Dl, Cl, Cl - Dl, mc);
                    Ticks(Al, Dl, 2, mc); Ticks(Bl, Cl, 2, mc); break;
                case Lock.CondD:
                    AngleMark(Al, Bl, Dl, 1, mc); AngleMark(Cl, Bl, Dl, 1, mc);
                    AngleMark(Bl, Al, Cl, 2, mc); AngleMark(Dl, Al, Cl, 2, mc); break;
                case Lock.CondR: case Lock.ParaUneq:
                    Ticks(Al, Ol, 1, mc); Ticks(Ol, Cl, 1, mc); Ticks(Bl, Ol, 2, mc); Ticks(Ol, Dl, 2, mc); break;
                case Lock.Kite:
                    Ticks(Al, Ol, 1, mc); Ticks(Ol, Cl, 1, mc); break;
                case Lock.Square:
                    Ticks(Al, Ol, 1, mc); Ticks(Ol, Cl, 1, mc); Ticks(Bl, Ol, 1, mc); Ticks(Ol, Dl, 1, mc); break;
                case Lock.Rhombus:
                    Ticks(Al, Ol, 1, mc); Ticks(Ol, Cl, 1, mc); Ticks(Bl, Ol, 2, mc); Ticks(Ol, Dl, 2, mc); break;
            }
        }

        readonly string[] lenCache = new string[4];
        string angCache;

        /// <summary>라벨 위치·글자(바뀔 때만 text 대입).</summary>
        void PlaceLabels(Sheet s, bool numbers, bool showAngle)
        {
            Vector2[] v = { Al, Bl, Cl, Dl };
            for (int i = 0; i < 4; i++)
            {
                var d = (v[i] - Ol).normalized;
                var p = v[i] + d * 1.05f;
                vLbl[i].transform.localPosition = new Vector3(p.x, TopY + 0.05f, p.y);
                vLbl[i].gameObject.SetActive(true);
            }
            int[] mm = { s.s1, s.s2, s.CO, s.DO };   // AO, BO, CO, DO
            for (int i = 0; i < 4; i++)
            {
                // 반쪽 막대 위: 길면 가운데, 짧으면 유리칼 머리(반지름 ≈ 1.3 cm)에 가리지 않게 바깥쪽으로
                var a = v[i]; var d = (a - Ol); float len = d.magnitude; var dir = len > 1e-3f ? d / len : Vector2.right;
                var n = new Vector2(-dir.y, dir.x);
                // 라벨은 이웃 막대와 이루는 각이 더 큰 쪽에 적는다(교각이 30°여도 두 라벨이 겹치지 않게)
                var other = (i % 2 == 0) ? u2 : u1;
                var wide = Vector2.Dot(dir, other) < Vector2.Dot(dir, -other) ? other : -other;
                if (Vector2.Dot(n, wide) < 0) n = -n;
                // 짧은 반쪽(유리칼 머리 반지름 ≈ 1.3 cm 안쪽)은 막대 옆으로 더 비켜 적는다(꼭짓점 글자와도 안 겹치게)
                var p = len < 3.2f ? Ol + dir * (len + 2.7f) : Ol + dir * (len * 0.5f) + n * 0.8f;
                lenLbl[i].transform.localPosition = new Vector3(p.x, TopY + 0.05f, p.y);
                bool on = numbers && !s.Locked;
                lenLbl[i].gameObject.SetActive(on);
                if (on)
                {
                    string t = Words.Cm(shownMm[i]) + " cm";
                    if (t != lenCache[i]) { lenCache[i] = t; lenLbl[i].text = t; }
                }
            }
            angLbl.gameObject.SetActive(showAngle);
            if (showAngle)
            {
                var bis = (-u1 - u2); if (bis.sqrMagnitude < 1e-3f) bis = new Vector2(-u1.y, u1.x); bis.Normalize();
                var p = Ol + bis * 2.6f;
                angLbl.transform.localPosition = new Vector3(p.x, TopY + 0.05f, p.y);
                string t = "∠AOB = " + s.theta + "°";
                if (t != angCache) { angCache = t; angLbl.text = t; }
            }
        }

        // 길이 숫자 카운트 애니메이션(표시값이 실제값을 5 mm 씩 따라간다)
        readonly int[] shownMm = new int[4];
        float countAcc;
        bool StepCounters(Sheet s, float dt)
        {
            int[] mm = { s.s1, s.s2, s.CO, s.DO };
            countAcc += dt;
            if (countAcc < 0.025f) return false;
            countAcc = 0;
            bool ch = false;
            for (int i = 0; i < 4; i++)
                if (shownMm[i] != mm[i]) { shownMm[i] += shownMm[i] < mm[i] ? 5 : -5; ch = true; }
            return ch;
        }
        void SnapCounters(Sheet s) { shownMm[0] = s.s1; shownMm[1] = s.s2; shownMm[2] = s.CO; shownMm[3] = s.DO; }
    }
}
