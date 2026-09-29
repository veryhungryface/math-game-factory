// 배율 밀어 — 월드: 미색 면 인화지 교정대 · 원본 고무판 · 놋쇠 레일(손잡이) · 흑철 플래튼 · 스파이크 파일.
// 1 월드 단위 = 1 cm. 카메라는 위에서 수직으로 내려다보는 정사영(길이 왜곡 없음 — 닮음비를 눈으로 견줄 수 있다).
// 정적 레이어(책상 종이결·인화지 모눈)는 부팅 때 1회 굽고, 도형 메시는 손잡이가 한 칸 움직일 때만 다시 쓴다.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.BaeyulMireo
{
    /// <summary>선·면을 한 메시에 모아 그리는 먹 붓(정점색). 목록은 재사용한다.</summary>
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
        /// <summary>볼록 다각형 채우기(부채꼴 분할).</summary>
        public void Poly(IList<Vector2> p, int n, Color32 col)
        {
            for (int k = 1; k < n - 1; k++) Tri(p[0], p[k], p[k + 1], col);
        }
        public void Outline(IList<Vector2> p, int n, float w, Color32 col)
        {
            for (int k = 0; k < n; k++) Seg(p[k], p[(k + 1) % n], w, col);
            for (int k = 0; k < n; k++) Disc(p[k], w * 0.5f, col, 8);
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
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(300, 10, 300));
        }
    }

    public partial class BaeyulGame
    {
        // ── 팔레트 (chosen.json art_direction.palette)
        static readonly Color Paper = Hex("EDE4D4"), Verm = Hex("B91C1C"), InkC = Hex("3F3A36"), Brass = Hex("C6A15B"), Wood = Hex("5C4A3A");
        static readonly Color Desk = Hex("DED1BA"), Card = Hex("F6F0E4"), Iron = Hex("2A2624");
        static Color Hex(string h) => MgfLook.Hex(h);
        // 선형 색공간: 정점색은 변환되지 않으므로 sRGB → 선형으로 바꿔 넣는다(안 그러면 먹색이 회색으로 뜬다)
        static Color32 C32(Color c, float a = 1f) { var l = c.linear; return new Color32((byte)(l.r * 255), (byte)(l.g * 255), (byte)(l.b * 255), (byte)(Mathf.Clamp01(a) * 255)); }
        static readonly System.Type[] keepTypes = { typeof(SphereCollider), typeof(CapsuleCollider), typeof(MeshCollider), typeof(BoxCollider) };

        const float SheetY = 0.06f;

        Camera cam;
        Transform world, sheetRoot, paperT, plateT, platenT, railT, knobT, spikeT, lampT, fingerT, pulseT;
        Ink plateInk, ghostInk, impInk, fxInk, railInk;
        Material inkMat, impMat, fxMat;
        MeshRenderer impR;
        readonly TextMeshPro[] pLetter = new TextMeshPro[4], pLen = new TextMeshPro[4], iLetter = new TextMeshPro[4], iLen = new TextMeshPro[4];
        readonly TextMeshPro[] pAng = new TextMeshPro[3], iAng = new TextMeshPro[3];
        TextMeshPro pExtra, knobTag, platenLbl, spikeLbl, plateLbl, lockLbl, gaugeLbl;
        readonly TextMeshPro[] railNum = new TextMeshPro[16];
        readonly Transform[] spikeSheets = new Transform[7];
        Transform stopLo, stopHi, lockPlate;
        Light lamp;
        int spiked;

        // 판형별 월드 배치
        Vector2 plateC, zoneC, spikeP;
        float railX, railZ0, railZ1, platenRestZ, sheetW, sheetH;
        Vector2 sheetOff;     // 인화지 사각형 중심(sheetRoot 기준)
        float wx0, wx1, wz0, wz1;

        // ─────────────────────────────── 월드 만들기
        void BuildWorld()
        {
            if (keepTypes.Length == 0) Debug.Log("keep");
            MgfLook.Quality(60f);
            if (!MgfBridge.LowGfx) QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            MgfLook.Sky(Hex("F3EBDD"), Hex("E6D9C3"), Hex("8A7560"), 1.0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.63f, 0.58f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.49f, 0.44f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.28f, 0.25f);
            // 딱딱한 옆빛(hard side light) — 낮고 긴 그림자로 판·레일의 두께가 보이게
            var sun = MgfLook.Sun(new Vector3(52, -58, 0), new Color(1f, 0.95f, 0.86f), 1.0f, 0.55f);
            sun.shadowBias = 0.02f; sun.shadowNormalBias = 0.2f;

            cam = MgfLook.Camera(new Vector3(0, 60, 0), Vector3.zero, 30f);
            cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.orthographic = true; cam.orthographicSize = 16;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Desk;
            cam.nearClipPlane = 1f; cam.farClipPlane = 120f;

            world = new GameObject("World").transform;
            inkMat = MgfLook.Alpha(Color.white);
            impMat = new Material(MgfLook.Alpha(Color.white)) { name = "BmImp" };
            fxMat = new Material(MgfLook.Alpha(Color.white)) { name = "BmFx" };

            // 책상: 거친 미색 면지(종이결 · 누름 자국) 1회 베이크
            var deskTex = PaperTexture(false);
            Plane("Desk", world, new Vector2(0, 0), new Vector2(400, 400), 0f, MgfLook.Lit(Color.white, 0.1f, 0f, null, deskTex), 400f / 6f, false);

            // 원본 고무판(나무 받침 + 붉은 고무 면 — 도형은 먹 메시로)
            plateT = new GameObject("Plate").transform; plateT.SetParent(world, false);
            var wood = MgfLook.Lit(Wood, 0.35f, 0f);
            var pb = MgfLook.Block("PlateBase", new Vector3(0, 0.45f, 0), new Vector3(1, 0.9f, 1), 0.08f, wood, plateT);
            pb.transform.localScale = new Vector3(11f, 1f, 8.6f);
            Object.Destroy(pb.GetComponent<Collider>());
            plateInk = MakeInk("PlateInk", 0.93f, inkMat, plateT);
            for (int i = 0; i < 4; i++)
            {
                pLetter[i] = Flat("", 13f, Paper, plateT); pLetter[i].fontStyle = FontStyles.Bold;
                pLen[i] = Flat("", 10.5f, Paper, plateT);
            }
            for (int i = 0; i < 3; i++) pAng[i] = Flat("", 10f, Paper, plateT);
            pExtra = Flat("", 10f, Paper, plateT);
            plateLbl = Flat("원본 고무판", 8f, C(Paper, 0.7f), plateT);

            // 인화지(작업 면) — 5 mm 모눈 · 누름 자국. 이 판이 들어오고, 찍히고, 스파이크에 꽂힌다
            sheetRoot = new GameObject("Sheet").transform; sheetRoot.SetParent(world, false);
            var paperTex = PaperTexture(true);
            paperT = Plane("Paper", sheetRoot, Vector2.zero, new Vector2(1, 1), 0.03f, MgfLook.Lit(Color.white, 0.12f, 0f, null, paperTex), 1f, true).transform;
            var shadow = Plane("PaperShadow", sheetRoot, Vector2.zero, new Vector2(1, 1), 0.015f, MgfLook.Alpha(new Color(0.25f, 0.18f, 0.12f, 0.22f)), 1f, false);
            shadow.transform.SetParent(paperT, false); shadow.transform.localPosition = new Vector3(0.012f, -0.012f, -0.012f); shadow.transform.localScale = new Vector3(1.02f, 1, 1.02f);
            impInk = MakeInk("Impression", SheetY + 0.01f, impMat, sheetRoot, out impR);
            ghostInk = MakeInk("Ghost", SheetY + 0.03f, inkMat, sheetRoot);
            fxInk = MakeInk("Fx", SheetY + 0.05f, fxMat, sheetRoot);
            for (int i = 0; i < 4; i++)
            {
                iLetter[i] = Flat("", 14f, InkC, sheetRoot); iLetter[i].fontStyle = FontStyles.Bold;
                iLen[i] = Flat("", 11.5f, InkC, sheetRoot);
            }
            for (int i = 0; i < 3; i++) iAng[i] = Flat("", 11f, InkC, sheetRoot);
            gaugeLbl = Flat("", 12f, Hex("7A5A1E"), sheetRoot); gaugeLbl.fontStyle = FontStyles.Bold;

            // 흑철 플래튼(인화지 위의 가로 막대 — 끌어 내리면 교정 롤러처럼 쓸고 지나가며 찍는다)
            platenT = new GameObject("Platen").transform; platenT.SetParent(world, false);
            var iron = MgfLook.Lit(Iron, 0.55f, 0.7f);
            var ironEdge = MgfLook.Lit(Hex("15110F"), 0.4f, 0.6f);
            var brassM = MgfLook.Lit(Brass, 0.7f, 0.9f);
            var pl = MgfLook.Block("PlatenBody", new Vector3(0, 1.1f, 0), new Vector3(1, 1.2f, 1.9f), 0.25f, iron, platenT);
            pl.name = "PlatenBody";
            var lip = MgfLook.Block("PlatenLip", new Vector3(0, 0.35f, -0.95f), new Vector3(1, 0.35f, 0.45f), 0.12f, MgfLook.Lit(Verm, 0.5f, 0.05f), platenT);
            Object.Destroy(lip.GetComponent<Collider>());
            var edge = MgfLook.Block("PlatenEdge", new Vector3(0, 0.55f, 0.95f), new Vector3(1, 0.5f, 0.3f), 0.1f, ironEdge, platenT);
            Object.Destroy(edge.GetComponent<Collider>());
            platenBody = pl.transform; platenLip = lip.transform; platenEdge = edge.transform;
            for (int i = 0; i < 4; i++)
            {
                var sc = MgfLook.Prim(PrimitiveType.Cylinder, "Screw", new Vector3(0, 1.72f, 0), new Vector3(0.45f, 0.05f, 0.45f), brassM, platenT, false);
                platenScrew[i] = sc.transform;
            }
            platenLbl = Flat("↓  끌어 내려 찍기", 8f, Brass, platenT); platenLbl.transform.localPosition = new Vector3(0, 1.75f, 0.05f);
            platenLbl.gameObject.SetActive(true);

            // 놋쇠 레일(자) + 손잡이
            railT = new GameObject("Rail").transform; railT.SetParent(world, false);
            var bar = MgfLook.Block("RailBar", new Vector3(0, 0.35f, 0), new Vector3(1.7f, 0.7f, 1), 0.2f, brassM, railT);
            railBar = bar.transform; Object.Destroy(bar.GetComponent<Collider>());
            var slot = MgfLook.Block("RailSlot", new Vector3(0.45f, 0.72f, 0), new Vector3(0.22f, 0.06f, 1), 0.03f, MgfLook.Lit(Hex("3A2E1C"), 0.3f, 0.2f), railT);
            railSlot = slot.transform; Object.Destroy(slot.GetComponent<Collider>());
            railInk = MakeInk("RailTicks", 0.73f, inkMat, railT);
            for (int i = 0; i < railNum.Length; i++) { railNum[i] = Flat((i + 1).ToString(), 6.5f, Hex("3A2E1C"), railT); railNum[i].gameObject.SetActive(true); }
            stopLo = MgfLook.Block("StopLo", Vector3.zero, new Vector3(1.9f, 0.5f, 0.35f), 0.1f, ironEdge, railT).transform;
            stopHi = MgfLook.Block("StopHi", Vector3.zero, new Vector3(1.9f, 0.5f, 0.35f), 0.1f, ironEdge, railT).transform;
            Object.Destroy(stopLo.GetComponent<Collider>()); Object.Destroy(stopHi.GetComponent<Collider>());
            knobT = new GameObject("Knob").transform; knobT.SetParent(railT, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "KnobBase", new Vector3(0, 0.9f, 0), new Vector3(2.5f, 0.25f, 2.5f), brassM, knobT, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "KnobGrip", new Vector3(0, 1.25f, 0), new Vector3(1.75f, 0.2f, 1.75f), MgfLook.Lit(Hex("2A2320"), 0.45f, 0.3f), knobT, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "KnobCap", new Vector3(0, 1.48f, 0), new Vector3(0.9f, 0.06f, 0.9f), brassM, knobT, false);
            var ptr = MgfLook.Block("KnobPointer", new Vector3(-1.5f, 0.95f, 0), new Vector3(1.4f, 0.12f, 0.16f), 0.05f, MgfLook.Lit(Verm, 0.5f, 0.1f), knobT);
            Object.Destroy(ptr.GetComponent<Collider>());
            knobTag = Flat("", 10.5f, InkC, knobT); knobTag.fontStyle = FontStyles.Bold;
            knobTag.alignment = TextAlignmentOptions.Right;
            knobTag.outlineWidth = 0.25f; knobTag.outlineColor = C32(Paper);
            lockPlate = MgfLook.Block("Lock", new Vector3(0, 1.0f, 0), new Vector3(2.6f, 0.3f, 3.4f), 0.2f, ironEdge, railT).transform;
            Object.Destroy(lockPlate.GetComponent<Collider>());
            lockLbl = Flat("고정", 8f, Brass, lockPlate); lockLbl.transform.localPosition = new Vector3(0, 0.2f, 0);
            lockLbl.gameObject.SetActive(true);

            // 스파이크 파일(치운 장이 꽂힌다)
            spikeT = new GameObject("Spike").transform; spikeT.SetParent(world, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "SpikeRim", new Vector3(0, 0.1f, 0), new Vector3(3.4f, 0.1f, 3.4f), brassM, spikeT, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "SpikeBase", new Vector3(0, 0.16f, 0), new Vector3(2.9f, 0.14f, 2.9f), MgfLook.Lit(Hex("57504A"), 0.5f, 0.6f), spikeT, false);
            var paperM = MgfLook.Lit(Hex("E7DCC8"), 0.1f, 0f);
            for (int i = 0; i < spikeSheets.Length; i++)
            {
                var q = MgfLook.Block("Spiked", new Vector3(0, 0.34f + i * 0.07f, 0), new Vector3(3.2f, 0.04f, 2.3f), 0.02f, paperM, spikeT);
                Object.Destroy(q.GetComponent<Collider>());
                q.transform.localRotation = Quaternion.Euler(0, (i * 37) % 50 - 25, 0);
                spikeSheets[i] = q.transform; q.SetActive(false);
            }
            MgfLook.Prim(PrimitiveType.Cylinder, "SpikeNail", new Vector3(0, 1.1f, 0), new Vector3(0.22f, 0.95f, 0.22f), MgfLook.Lit(Hex("8C8580"), 0.8f, 0.9f), spikeT, false);
            spikeLbl = Flat("← 치우기", 7.5f, InkC, spikeT); spikeLbl.transform.localPosition = new Vector3(0, 0.4f, -2.3f);
            spikeLbl.gameObject.SetActive(true);

            // 작업등(느리게 흐르는 따뜻한 반사광 — 가만히 둬도 살아 있는 화면)
            lamp = new GameObject("ProofLamp").AddComponent<Light>();
            lamp.type = LightType.Point; lamp.range = 40f; lamp.intensity = 1.1f; lamp.color = new Color(1f, 0.9f, 0.75f);
            lamp.shadows = LightShadows.None; lamp.renderMode = LightRenderMode.ForcePixel;
            lampT = lamp.transform;
            if (MgfBridge.LowGfx) { lamp.enabled = false; sun.shadows = LightShadows.Hard; }

            // 유령 손가락(부드러운 흰 점 + 먹 테두리) · 맥동 링
            fingerT = new GameObject("Finger", typeof(MeshFilter), typeof(MeshRenderer)).transform;
            fingerT.SetParent(world, false);
            var fq = new Ink(0f);
            fq.Disc(Vector2.zero, 0.85f, new Color32(255, 255, 255, 210), 24);
            fq.Ring(Vector2.zero, 0.88f, 0.12f, C32(InkC, 0.85f));
            fq.Disc(new Vector2(0.3f, -1.0f), 0.55f, new Color32(255, 255, 255, 110), 16);
            fq.Apply();
            fingerT.GetComponent<MeshFilter>().sharedMesh = fq.mesh;
            fingerT.GetComponent<MeshRenderer>().sharedMaterial = inkMat;
            fingerT.gameObject.SetActive(false);
            pulseT = new GameObject("Pulse", typeof(MeshFilter), typeof(MeshRenderer)).transform;
            pulseT.SetParent(world, false);
            var pr = new Ink(0f); pr.Ring(Vector2.zero, 1.0f, 0.14f, C32(Verm, 0.9f), 36); pr.Apply();
            pulseT.GetComponent<MeshFilter>().sharedMesh = pr.mesh;
            pulseT.GetComponent<MeshRenderer>().sharedMaterial = inkMat;
            pulseT.gameObject.SetActive(false);

            WorldLayout(false);
        }

        Transform platenBody, platenLip, platenEdge, railBar, railSlot;
        readonly Transform[] platenScrew = new Transform[4];

        GameObject Plane(string name, Transform parent, Vector2 c, Vector2 size, float y, Material m, float uvRepeat, bool shadowRecv)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name };
            float hx = size.x * 0.5f, hz = size.y * 0.5f;
            mesh.vertices = new[] { new Vector3(c.x - hx, y, c.y - hz), new Vector3(c.x - hx, y, c.y + hz), new Vector3(c.x + hx, y, c.y + hz), new Vector3(c.x + hx, y, c.y - hz) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, uvRepeat), new Vector2(uvRepeat, uvRepeat), new Vector2(uvRepeat, 0) };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.colors32 = new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = shadowRecv || true;
            return go;
        }

        TextMeshPro Flat(string s, float size, Color c, Transform parent)
        {
            var t = MgfText.World(s, Vector3.zero, size, c, parent);
            t.transform.localRotation = Quaternion.Euler(90, 0, 0);
            t.gameObject.SetActive(false);
            return t;
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

        /// <summary>미색 면지 텍스처(512 px = 5 cm). grid=true 면 인화지: 5 mm 모눈 + 1 cm 조금 진한 선.</summary>
        static Texture2D PaperTexture(bool grid)
        {
            const int S = 512;
            var px = new Color32[S * S];
            var rnd = new System.Random(grid ? 3 : 7);
            var baseC = grid ? Paper : Desk;
            var noise = new float[33 * 33];
            for (int i = 0; i < noise.Length; i++) noise[i] = (float)rnd.NextDouble();
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float fx = x / 16f, fy = y / 16f; int ix = (int)fx % 32, iy = (int)fy % 32;
                    float tx = fx - (int)fx, ty = fy - (int)fy;
                    float n = Mathf.Lerp(Mathf.Lerp(noise[iy * 33 + ix], noise[iy * 33 + ix + 1], tx), Mathf.Lerp(noise[(iy + 1) * 33 + ix], noise[(iy + 1) * 33 + ix + 1], tx), ty);
                    float grain = (float)rnd.NextDouble();
                    var c = baseC * (0.965f + n * 0.05f + grain * 0.018f);
                    if (grid)
                    {
                        // 5 mm = 51.2 px
                        float gx = x % 51.2f, gy = y % 51.2f;
                        float dx = Mathf.Min(gx, 51.2f - gx), dy = Mathf.Min(gy, 51.2f - gy);
                        bool majX = ((int)(x / 51.2f + 0.5f)) % 2 == 0, majY = ((int)(y / 51.2f + 0.5f)) % 2 == 0;
                        float lx = Mathf.Clamp01((majX ? 1.6f : 1.0f) - dx), ly = Mathf.Clamp01((majY ? 1.6f : 1.0f) - dy);
                        float line = Mathf.Max(lx * (majX ? 0.16f : 0.09f), ly * (majY ? 0.16f : 0.09f));
                        c = Color.Lerp(c, new Color(0.62f, 0.48f, 0.38f), line);
                    }
                    c.a = 1;
                    px[y * S + x] = c;
                }
            // 섬유 · 누름 자국
            for (int k = 0; k < (grid ? 70 : 140); k++)
            {
                float x0 = (float)rnd.NextDouble() * S, y0 = (float)rnd.NextDouble() * S, a = (float)rnd.NextDouble() * Mathf.PI, L = 6 + (float)rnd.NextDouble() * 26;
                float dark = rnd.NextDouble() < 0.6 ? 0.955f : 1.03f;
                for (float s = 0; s < L; s += 0.7f)
                {
                    int x = ((int)(x0 + Mathf.Cos(a) * s) % S + S) % S, y = ((int)(y0 + Mathf.Sin(a) * s) % S + S) % S;
                    var c = (Color)px[y * S + x]; c *= dark; c.a = 1; px[y * S + x] = c;
                }
            }
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = grid ? "BmPaper" : "BmDesk", wrapMode = TextureWrapMode.Repeat, anisoLevel = 4, filterMode = FilterMode.Trilinear };
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        /// <summary>판형에 따라 고무판·작업 면·레일·스파이크를 다시 둔다(화면 크기가 바뀔 때만).</summary>
        void WorldLayout(bool landscape)
        {
            if (!landscape)
            {
                plateC = new Vector2(-2.6f, 12.0f); spikeP = new Vector2(-11.4f, 12.2f);
                zoneC = new Vector2(-2.3f, -5.7f); sheetOff = new Vector2(0, 0.5f); sheetW = 18.4f; sheetH = 19.6f;
                railX = 10.3f; railZ0 = -14.2f; railZ1 = 13.4f; platenRestZ = 5.3f;
                wx0 = -13.4f; wx1 = 12.6f; wz0 = -16.2f; wz1 = 17.8f;
            }
            else
            {
                plateC = new Vector2(-17.8f, 2.8f); spikeP = new Vector2(-17.8f, -8.6f);
                zoneC = new Vector2(-1.6f, -2.2f); sheetOff = new Vector2(0, 0.4f); sheetW = 18.4f; sheetH = 19.8f;
                railX = 10.5f; railZ0 = -11.8f; railZ1 = 10.6f; platenRestZ = 9.2f;
                wx0 = -24.4f; wx1 = 12.6f; wz0 = -13.2f; wz1 = 11.2f;
            }
            layLand = landscape;
            FitPlate();
            spikeT.localPosition = new Vector3(spikeP.x, 0, spikeP.y);
            paperT.localPosition = new Vector3(sheetOff.x, 0, sheetOff.y);
            paperT.localScale = new Vector3(sheetW, 1, sheetH);
            paperT.GetComponent<MeshRenderer>().sharedMaterial.mainTextureScale = new Vector2(sheetW / 5f, sheetH / 5f);
            // 플래튼: 인화지 폭
            float pw = sheetW + 1.2f;
            platenBody.localScale = new Vector3(pw, 1, 1); platenLip.localScale = new Vector3(pw - 0.4f, 1, 1); platenEdge.localScale = new Vector3(pw - 0.2f, 1, 1);
            for (int i = 0; i < 4; i++) platenScrew[i].localPosition = new Vector3((i < 2 ? -1 : 1) * (pw * 0.5f - 0.9f - (i % 2) * 1.3f), 1.72f, 0);
            // 레일
            float L = railZ1 - railZ0;
            railT.localPosition = new Vector3(railX, 0, (railZ0 + railZ1) * 0.5f);
            railBar.localScale = new Vector3(1, 1, L + 2.2f);
            railSlot.localScale = new Vector3(1, 1, L);
            railInk.Clear();
            var tc = C32(Hex("3A2E1C"));
            for (int mm = Rules.RailMin; mm <= Rules.RailMax; mm += Rules.Step)
            {
                float z = RailZ(mm) - railT.localPosition.z;
                bool major = mm % 10 == 0;
                railInk.Seg(new Vector2(-0.8f, z), new Vector2(major ? 0.15f : -0.25f, z), major ? 0.09f : 0.06f, tc);
            }
            railInk.Apply();
            for (int i = 0; i < railNum.Length; i++)
            {
                int mm = (i + 1) * 10;
                railNum[i].transform.localPosition = new Vector3(0.62f, 0.75f, RailZ(mm) - railT.localPosition.z);
                railNum[i].gameObject.SetActive(i % 1 == 0);
            }
            SetSheetBase();
        }

        float RailZ(int mm) => railZ0 + (mm - Rules.RailMin) / (float)(Rules.RailMax - Rules.RailMin) * (railZ1 - railZ0);
        float RailZf(float mm) => railZ0 + (mm - Rules.RailMin) / (Rules.RailMax - Rules.RailMin) * (railZ1 - railZ0);
        float RailMm(float z) => Rules.RailMin + (z - railZ0) / (railZ1 - railZ0) * (Rules.RailMax - Rules.RailMin);

        void SetSheetBase()
        {
            if (cur != null) PlaceStops(cur);
        }

        void PlaceStops(Sheet s)
        {
            float cz = railT.localPosition.z;
            stopLo.localPosition = new Vector3(0, 0.8f, RailZ(s.railLo) - cz - 0.45f);
            stopHi.localPosition = new Vector3(0, 0.8f, RailZ(s.railHi) - cz + 0.45f);
            bool locked = s.Locked;
            stopLo.gameObject.SetActive(!locked); stopHi.gameObject.SetActive(!locked);
            knobT.gameObject.SetActive(!locked);
            lockPlate.gameObject.SetActive(locked);
            if (locked) lockPlate.localPosition = new Vector3(0, 0.3f, 0);
        }

        // ─────────────────────────────── 도형 기하(그림 전용 — 판정은 BaeyulRules 의 정수)
        static Vector2[] TriSides(float c, float a, float b)
        {
            // AB = c, BC = a, CA = b · B 원점, C 오른쪽, A 위
            float x = (a * a + c * c - b * b) / (2 * a); float y = Mathf.Sqrt(Mathf.Max(1e-4f, c * c - x * x));
            return new[] { new Vector2(x, y), Vector2.zero, new Vector2(a, 0) };
        }
        static Vector2[] TriSAS(float c, float a, int deg)
        {
            float r = deg * Mathf.Deg2Rad;
            return new[] { new Vector2(Mathf.Cos(r), Mathf.Sin(r)) * c, Vector2.zero, new Vector2(a, 0) };
        }
        static Vector2[] TriAA(float ab, int A, int B)
        {
            float C = 180 - A - B;
            float ac = ab * Mathf.Sin(B * Mathf.Deg2Rad) / Mathf.Sin(C * Mathf.Deg2Rad);
            var a = Vector2.zero; var b = new Vector2(ab, 0);
            var c = new Vector2(Mathf.Cos(A * Mathf.Deg2Rad), Mathf.Sin(A * Mathf.Deg2Rad)) * ac;
            return new[] { a, b, c };
        }
        static Vector2[] RectV(float h, float w) => new[] { new Vector2(0, h), Vector2.zero, new Vector2(w, 0), new Vector2(w, h) };

        /// <summary>원본 도형 꼭짓점(cm, 도형 좌표).</summary>
        static Vector2[] OrigShape(Sheet s)
        {
            if (s.kind == Kind.Judge)
            {
                switch (s.cond)
                {
                    case Cond.SAS: return TriSAS(s.os[0] / 10f, s.os[1] / 10f, s.oAng);
                    case Cond.AA: return TriAA(s.os[0] / 10f, s.oA[0], s.oA[1]);
                    case Cond.Rect: return RectV(s.os[0] / 10f, s.os[1] / 10f);
                }
            }
            if (s.shape == Shape.Rect) return RectV(s.os[0] / 10f, s.os[1] / 10f);
            if (s.shape == Shape.TriBH) return new[] { new Vector2(s.ofoot / 10f, s.oh / 10f), Vector2.zero, new Vector2(s.os[1] / 10f, 0) };
            return TriSides(s.os[0] / 10f, s.os[1] / 10f, s.os[2] / 10f);
        }

        /// <summary>판별 장의 후보 도형(D,E,F 가 꼭짓점 0,1,2).</summary>
        static Vector2[] CandShape(Sheet s)
        {
            switch (s.cond)
            {
                case Cond.SSS: return TriSides(s.cs[0] / 10f, s.cs[1] / 10f, s.cs[2] / 10f);
                case Cond.SAS: return TriSAS(s.cs[0] / 10f, s.cs[1] / 10f, s.cAng);
                case Cond.AA: return TriAA(s.cs[0] / 10f, s.cA[0], s.cA[1]);
                default: return RectV(s.cs[0] / 10f, s.cs[1] / 10f);
            }
        }

        /// <summary>뒤집기·회전 후 무게 중심을 c 에 둔다(상자 중심 기준).</summary>
        static void Place(Vector2[] v, int n, float scale, int rot, bool flip, Vector2 c, Vector2[] outV)
        {
            float r = rot * Mathf.Deg2Rad, cs = Mathf.Cos(r), sn = Mathf.Sin(r);
            float x0 = 1e9f, x1 = -1e9f, z0 = 1e9f, z1 = -1e9f;
            for (int i = 0; i < n; i++)
            {
                var p = v[i] * scale; if (flip) p.x = -p.x;
                p = new Vector2(p.x * cs - p.y * sn, p.x * sn + p.y * cs);
                outV[i] = p;
                x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.y); z1 = Mathf.Max(z1, p.y);
            }
            var off = c - new Vector2((x0 + x1) * 0.5f, (z0 + z1) * 0.5f);
            for (int i = 0; i < n; i++) outV[i] += off;
        }

        readonly Vector2[] PV = new Vector2[4], IV = new Vector2[4], TV = new Vector2[4];
        int PN = 3;

        /// <summary>원본 고무판을 다시 그린다(장이 바뀔 때 · 정답 빗금이 잠길 때).</summary>
        void DrawPlate(Sheet s, bool ticks)
        {
            PN = s.N;
            var raw = OrigShape(s);
            Place(raw, PN, 1f, 0, false, new Vector2(0, -0.4f), PV);
            // 받침 크기
            float x0 = 1e9f, x1 = -1e9f, z0 = 1e9f, z1 = -1e9f;
            for (int i = 0; i < PN; i++) { x0 = Mathf.Min(x0, PV[i].x); x1 = Mathf.Max(x1, PV[i].x); z0 = Mathf.Min(z0, PV[i].y); z1 = Mathf.Max(z1, PV[i].y); }
            plateInk.Clear();
            var rub = C32(Hex("8E2A1E")); var edge = C32(Hex("F2D9C2"), 0.85f);
            // 고무 면(살짝 두꺼운 테두리로 부조감)
            plateInk.Poly(PV, PN, C32(Hex("5E1A12")));
            var inner = new Vector2[4]; Shrink(PV, PN, 0.12f, inner);
            plateInk.Poly(inner, PN, rub);
            plateInk.Outline(PV, PN, 0.1f, edge);
            if (s.shape == Shape.TriBH)
            {
                var foot = new Vector2(PV[0].x, PV[1].y);
                plateInk.Dashed(PV[0], foot, 0.3f, 0.2f, 0.07f, edge, 0);
                plateInk.Seg(foot + new Vector2(0.45f, 0), foot + new Vector2(0.45f, 0.45f), 0.06f, edge);
                plateInk.Seg(foot + new Vector2(0, 0.45f), foot + new Vector2(0.45f, 0.45f), 0.06f, edge);
            }
            if (ticks) for (int i = 0; i < PN; i++) Ticks(plateInk, PV[i], PV[(i + 1) % PN], TickCount(s, i), C32(Hex("FFD9B8")));
            AngleMarks(plateInk, s, PV, true, edge);
            plateInk.Apply();

            // 글자
            var ol = Words.OL(s);
            for (int i = 0; i < 4; i++)
            {
                bool on = i < PN;
                pLetter[i].gameObject.SetActive(on); pLen[i].gameObject.SetActive(false);
                if (!on) continue;
                pLetter[i].text = ol[i].ToString();
                pLetter[i].transform.localPosition = V3(Outward(PV, PN, i, 0.95f), 0.95f);
            }
            pExtra.gameObject.SetActive(s.shape == Shape.TriBH);
            if (s.shape == Shape.TriBH)
            {
                pExtra.text = "높이 " + Words.Cm(s.oh) + " cm";
                var mid = (PV[0] + new Vector2(PV[0].x, PV[1].y)) * 0.5f;
                pExtra.transform.localPosition = V3(mid + new Vector2(1.5f, 0), 0.95f);
            }
            // 길이·각: 꼭짓점 글자·높이 상자를 먼저 막아 두고, 겹치면 자리를 옮긴다(고무판은 받침이 따라 커진다)
            OccReset();
            for (int i = 0; i < PN; i++) OccAdd(pLetter[i]);
            OccAdd(pExtra);
            for (int i = 0; i < 3; i++) pAng[i].gameObject.SetActive(false);
            if (s.kind == Kind.Judge && s.cond == Cond.SAS) SetAng(pAng[0], PV, PN, 1, s.oAng, 0.95f);
            if (s.kind == Kind.Judge && s.cond == Cond.AA) { SetAng(pAng[0], PV, PN, 0, s.oA[0], 0.95f); SetAng(pAng[1], PV, PN, 1, s.oA[1], 0.95f); }
            bool showLen = !(s.kind == Kind.Judge && s.cond == Cond.AA);
            for (int i = 0; i < PN && showLen; i++)
            {
                if (s.shape == Shape.Rect && i >= 2) continue;
                if (s.shape == Shape.TriBH && i != 1) continue;
                if (s.kind == Kind.Judge && s.cond == Cond.SAS && i == 2) continue;
                pLen[i].text = Words.Cm(s.os[i]) + " cm";
                PlaceSideLabel(pLen[i], PV, PN, i, 0.62f, 0.95f, 12);
            }
            // 받침: 도형 + 켜진 글자·길이를 모두 덮는 여백(위 여백에 「원본 고무판」)
            float bx0 = x0, bx1 = x1, bz0 = z0, bz1 = z1;
            void Ext(TextMeshPro t, float hw)
            {
                if (!t.gameObject.activeSelf) return;
                var p = t.transform.localPosition;
                bx0 = Mathf.Min(bx0, p.x - hw); bx1 = Mathf.Max(bx1, p.x + hw); bz0 = Mathf.Min(bz0, p.z - 0.6f); bz1 = Mathf.Max(bz1, p.z + 0.6f);
            }
            for (int i = 0; i < 4; i++) { Ext(pLetter[i], 0.6f); Ext(pLen[i], 1.9f); }
            for (int i = 0; i < 3; i++) Ext(pAng[i], 1.1f);
            Ext(pExtra, 2.6f);
            bx0 -= 0.5f; bx1 += 0.5f; bz0 -= 0.5f; bz1 += 1.6f;
            var pbT = plateT.Find("PlateBase");
            pbT.localScale = new Vector3(bx1 - bx0, 1, bz1 - bz0);
            pbT.localPosition = new Vector3((bx0 + bx1) * 0.5f, 0.45f, (bz0 + bz1) * 0.5f);
            plateLbl.transform.localPosition = new Vector3((bx0 + bx1) * 0.5f, 0.95f, bz1 - 0.75f);
            plateLbl.gameObject.SetActive(true);
            pbx0 = bx0; pbx1 = bx1; pbz0 = bz0; pbz1 = bz1;
            FitPlate();
        }

        bool layLand;
        float pbx0 = -3f, pbx1 = 3f, pbz0 = -3f, pbz1 = 3f;   // 고무판 받침 상자(plateT 로컬)

        /// <summary>원본 고무판을 허용 칸 안에 가둔다 — 세로 화면에서 긴 직사각형 판이 플래튼·레일·티켓을 덮던 결함(3차 검수 high).
        /// 칸보다 크면 판 전체(도형·글자)를 같은 비율로 줄이고, 칸 안에서 가운데에 둔다. 길이 글자가 수치를 말하므로 판정과 무관.</summary>
        void FitPlate()
        {
            float ax0, ax1, az0, az1;
            if (!layLand) { ax0 = spikeP.x + 2.0f; ax1 = railX - 2.0f; az0 = platenRestZ + 1.6f; az1 = plateC.y + 6.6f; }
            else { ax0 = wx0 + 0.8f; ax1 = zoneC.x + sheetOff.x - sheetW * 0.5f - 0.8f; az0 = spikeP.y + 2.8f; az1 = plateC.y + 7.0f; }
            float bw = pbx1 - pbx0, bh = pbz1 - pbz0;
            float f = Mathf.Min(1f, Mathf.Min((ax1 - ax0) / bw, (az1 - az0) / bh));
            float cx = Mathf.Clamp(plateC.x, ax0 + bw * f * 0.5f, ax1 - bw * f * 0.5f);
            float cz = Mathf.Clamp(plateC.y + 0.4f, az0 + bh * f * 0.5f, az1 - bh * f * 0.5f);
            plateT.localScale = new Vector3(f, f, f);
            plateT.localPosition = new Vector3(cx - (pbx0 + pbx1) * 0.5f * f, 0, cz - (pbz0 + pbz1) * 0.5f * f);
        }

        static Vector3 V3(Vector2 p, float y) => new Vector3(p.x, y, p.y);

        static Vector2 Centroid(Vector2[] v, int n) { var c = Vector2.zero; for (int i = 0; i < n; i++) c += v[i]; return c / n; }
        static void Shrink(Vector2[] v, int n, float d, Vector2[] o)
        {
            var c = Centroid(v, n);
            for (int i = 0; i < n; i++) { var k = v[i] - c; float L = k.magnitude; o[i] = c + k * Mathf.Max(0, (L - d * 1.6f) / Mathf.Max(1e-3f, L)); }
        }
        static void Grow(Vector2[] v, int n, float d, Vector2[] o)
        {
            var c = Centroid(v, n);
            for (int i = 0; i < n; i++) { var k = v[i] - c; float L = k.magnitude; o[i] = c + k * ((L + d) / Mathf.Max(1e-3f, L)); }
        }
        static Vector2 Outward(Vector2[] v, int n, int i, float d)
        {
            var c = Centroid(v, n); var k = (v[i] - c).normalized;
            return v[i] + k * d;
        }
        // ── 글자 겹침 방지. 짧은 변·작은 배율에서 길이 숫자가 꼭짓점 글자·다른 길이·손잡이 이름표와 붙어
        //    「4.1 cmDE」「B.5 cnC」처럼 읽히던 결함. 글자 상자(AABB, 도형 좌표)를 모아 두고 겹치지 않는 자리를 찾는다.
        readonly Rect[] occ = new Rect[20];
        int occN;
        void OccReset() => occN = 0;
        void OccAdd(Rect r) { if (occN < occ.Length) occ[occN++] = r; }
        void OccAdd(TextMeshPro t) { if (t.gameObject.activeSelf) OccAdd(TBox(t, new Vector2(t.transform.localPosition.x, t.transform.localPosition.z), 0.12f)); }
        static Rect TBox(TextMeshPro t, Vector2 c, float pad)
        {
            var sz = t.GetPreferredValues(t.text);
            float w = sz.x + pad * 2, h = sz.y * 0.78f + pad * 2;
            return new Rect(c.x - w * 0.5f, c.y - h * 0.5f, w, h);
        }
        bool FreeSpot(Rect r, Vector2[] v, int n)
        {
            for (int i = 0; i < occN; i++) if (occ[i].Overlaps(r)) return false;
            for (int i = 0; i < n; i++)
            {
                var a = v[i]; var b = v[(i + 1) % n];
                for (int k = 0; k <= 16; k++) if (r.Contains(Vector2.Lerp(a, b, k / 16f))) return false;
            }
            return true;
        }
        /// <summary>변 i 의 바깥 법선으로 d 부터 조금씩 밀며 빈 자리를 찾는다. 끝내 못 찾으면 숨긴다(값은 전표·현재 비·손잡이 이름표에 남는다).</summary>
        bool PlaceSideLabel(TextMeshPro t, Vector2[] v, int n, int i, float d, float y, int tries)
        {
            for (int k = 0; k < tries; k++)
            {
                var p = SideLabelPos(v, n, i, d + k * 0.4f);
                var r = TBox(t, p, 0.1f);
                if (!FreeSpot(r, v, n)) continue;
                t.transform.localPosition = V3(p, y);
                t.gameObject.SetActive(true);
                OccAdd(r);
                return true;
            }
            t.gameObject.SetActive(false);
            return false;
        }

        static Vector2 SideLabelPos(Vector2[] v, int n, int i, float d)
        {
            var a = v[i]; var b = v[(i + 1) % n]; var m = (a + b) * 0.5f;
            var dir = (b - a).normalized; var nr = new Vector2(-dir.y, dir.x);
            var c = Centroid(v, n);
            if (Vector2.Dot(nr, m - c) < 0) nr = -nr;
            float horiz = Mathf.Abs(dir.x);   // 가로 변은 위아래로, 세로 변은 옆으로 글 폭만큼 더 비킨다
            return m + nr * (d + 0.25f + (1f - horiz) * 1.2f);
        }

        /// <summary>대응변 빗금 수: 원본 변 i 와 그 대응변이 같은 수를 갖는다(1·2·3).</summary>
        static int TickCount(Sheet s, int i) => s.shape == Shape.Rect ? (i % 2) + 1 : i + 1;

        static void Ticks(Ink ink, Vector2 a, Vector2 b, int count, Color32 col)
        {
            var m = (a + b) * 0.5f; var d = (b - a).normalized; var n = new Vector2(-d.y, d.x);
            for (int k = 0; k < count; k++)
            {
                var p = m + d * ((k - (count - 1) * 0.5f) * 0.24f);
                ink.Seg(p - n * 0.34f, p + n * 0.34f, 0.08f, col);
            }
        }

        void AngleMarks(Ink ink, Sheet s, Vector2[] v, bool orig, Color32 col)
        {
            if (s.kind != Kind.Judge) return;
            if (s.cond == Cond.SAS) Arc(ink, v, 3, 1, 0.9f, col);
            if (s.cond == Cond.AA)
            {
                if (orig) { Arc(ink, v, 3, 0, 0.9f, col); Arc(ink, v, 3, 1, 1.0f, col); }
                else for (int i = 0; i < 3; i++) if ((s.cMask & (1 << i)) != 0) Arc(ink, v, 3, i, 0.9f + i * 0.1f, col);
            }
        }

        static void Arc(Ink ink, Vector2[] v, int n, int i, float r, Color32 col)
        {
            var p = v[i]; var a = v[(i + n - 1) % n] - p; var b = v[(i + 1) % n] - p;
            float a0 = Mathf.Atan2(a.y, a.x) * Mathf.Rad2Deg, d = Mathf.DeltaAngle(a0, Mathf.Atan2(b.y, b.x) * Mathf.Rad2Deg);
            ink.Arc(p, r, a0, a0 + d, 0.07f, col, 14);
        }

        static readonly float[] AngD = { 1.9f, 2.5f, 1.4f, 3.1f, 1.0f, 3.8f };
        void SetAng(TextMeshPro t, Vector2[] v, int n, int i, int deg, float y)
        {
            var p = v[i]; var a = (v[(i + n - 1) % n] - p).normalized; var b = (v[(i + 1) % n] - p).normalized;
            var bis = (a + b); if (bis.sqrMagnitude < 1e-4f) bis = new Vector2(-a.y, a.x); bis.Normalize();
            t.text = deg + "°";
            // 각 안쪽 이등분선 위에서 다른 글자와 안 겹치는 거리를 고른다(작은 삼각형에서 「50°45°」로 붙던 결함)
            var best = p + bis * 1.9f;
            for (int k = 0; k < AngD.Length; k++)
            {
                var q = p + bis * AngD[k]; var r = TBox(t, q, 0.08f);
                bool free = true;
                for (int j = 0; j < occN && free; j++) if (occ[j].Overlaps(r)) free = false;
                if (free) { best = q; break; }
            }
            t.transform.localPosition = V3(best, y);
            t.gameObject.SetActive(true);
            OccAdd(TBox(t, best, 0.08f));
        }

        // ─────────────────────────────── 인화지 위 도형(구성 장: 손잡이 배율의 미리보기 윤곽 / 판별 장: 후보 교정본)
        float gaugeScale = 1f;   // 현재 배율(그림용)
        const float ImageFitCm = 12.5f;
        const float ImageShiftX = -1.1f;   // 레일 왼쪽 손잡이 이름표 자리를 비운다   // 인화지(18.4) 안에 그림 + 꼭짓점·길이 글자 여백

        void ComputeImage(Sheet s, float handleMm)
        {
            if (s.kind == Kind.Judge)
            {
                var raw = CandShape(s);
                Place(raw, s.N, 1f, s.rot, s.flip, zoneC - LocalSheetOrigin(), IV);
                return;
            }
            // 그림 배율 × 장별 표시 축척: 손잡이 끝(railHi)에서도 그림+글자가 인화지 안(레일 왼쪽)에 들어오게 한 장 내내 같은 비율로 줄인다.
            // 비례는 그대로라 손잡이를 움직이면 그림도 똑같이 커지고 작아진다(판정은 정수 규칙 — 그림과 무관).
            var orw = OrigShape(s);
            float ex = 0f;
            for (int i = 0; i < s.N; i++) for (int j = i + 1; j < s.N; j++) ex = Mathf.Max(ex, (orw[i] - orw[j]).magnitude);
            float hiK = s.railHi / (float)Judge.GaugeOrig(s);
            float disp = ex * hiK > ImageFitCm ? ImageFitCm / (ex * hiK) : 1f;
            gaugeScale = handleMm / Judge.GaugeOrig(s) * disp;
            Place(orw, s.N, gaugeScale, s.rot, s.flip, zoneC - LocalSheetOrigin() + new Vector2(ImageShiftX, 0), IV);
        }

        Vector2 LocalSheetOrigin() => zoneC;   // sheetRoot 는 zoneC 에 놓인다 → 도형 좌표는 원점 기준

        /// <summary>미리보기 윤곽·레일 변·글자·(1단계) 길이·빗금.</summary>
        void DrawGhost(Sheet s, bool stamped)
        {
            ComputeImage(s, s.kind == Kind.Judge ? 0 : s.handle);
            int n = s.N;
            ghostInk.Clear();
            var ic = C32(InkC, 0.78f);
            if (s.kind == Kind.Judge)
            {
                // 교정본: 먹 실선 + 조건 표시(각 호)
                ghostInk.Outline(IV, n, 0.1f, C32(InkC, 0.9f));
                AngleMarks(ghostInk, s, IV, false, C32(InkC, 0.9f));
            }
            else if (!stamped)
            {
                for (int i = 0; i < n; i++)
                {
                    bool g = i == s.gauge;
                    if (g) ghostInk.Seg(IV[i], IV[(i + 1) % n], 0.24f, C32(Brass));
                    else ghostInk.Dashed(IV[i], IV[(i + 1) % n], 0.42f, 0.24f, 0.09f, ic, 0);
                }
                for (int i = 0; i < n; i++) ghostInk.Disc(IV[i], 0.12f, ic, 10);
                if (s.stage == 1 || s.no == 0) for (int i = 0; i < n; i++) Ticks(ghostInk, IV[i], IV[(i + 1) % n], TickCount(s, i), C32(InkC, 0.7f));
            }
            ghostInk.Apply();
            // 글자
            var il = Words.IL(s);
            for (int i = 0; i < 4; i++)
            {
                bool on = i < n;
                iLetter[i].gameObject.SetActive(on);
                if (!on) continue;
                string L = (s.kind == Kind.Judge ? il[i] : il[s.corr[i]]).ToString();
                if (iLetter[i].text != L) iLetter[i].text = L;
                iLetter[i].transform.localPosition = V3(Outward(IV, n, i, 1.0f), SheetY + 0.06f);
            }
            // 길이: 1단계는 모든 변(현재 배율) · 판별 장은 주어진 변 · 그 밖은 레일 변 이름표만
            for (int i = 0; i < 4; i++) iLen[i].gameObject.SetActive(false);
            for (int i = 0; i < 3; i++) iAng[i].gameObject.SetActive(false);
            gaugeLbl.gameObject.SetActive(false);
            // 먼저 막아 둘 자리: 꼭짓점 글자 · 레일 · 손잡이 이름표(「DE 5.5 cm」 — 레일 왼쪽으로 뻗는다)
            OccReset();
            for (int i = 0; i < n; i++) OccAdd(iLetter[i]);
            OccAdd(new Rect(railX - 1.4f - zoneC.x, -40f, 2.8f, 80f));
            if (!s.Locked && knobTag.gameObject.activeSelf)
            {
                var ksz = knobTag.GetPreferredValues(knobTag.text);
                float kz = RailZf(s.handle) - zoneC.y, kx1 = railX - 2.2f - zoneC.x;
                OccAdd(new Rect(kx1 - ksz.x - 0.3f, kz - ksz.y * 0.5f - 0.2f, ksz.x + 0.5f, ksz.y + 0.4f));
            }
            if (s.kind == Kind.Judge)
            {
                if (s.cond == Cond.SAS) SetAng(iAng[0], IV, n, 1, s.cAng, SheetY + 0.06f);
                if (s.cond == Cond.AA) { int k = 0; for (int i = 0; i < 3; i++) if ((s.cMask & (1 << i)) != 0) SetAng(iAng[k++], IV, n, i, s.cA[i], SheetY + 0.06f); }
                if (s.cond == Cond.SSS) for (int i = 0; i < 3; i++) SetLen(iLen[i], IV, n, i, Words.Cm(s.cs[i]) + " cm");
                if (s.cond == Cond.SAS || s.cond == Cond.Rect) { SetLen(iLen[0], IV, n, 0, Words.Cm(s.cs[0]) + " cm"); SetLen(iLen[1], IV, n, 1, Words.Cm(s.cs[1]) + " cm"); }
                return;
            }
            if (stamped) return;
            if (s.stage == 1 || s.no == 0)
            {
                // 레일 변부터 자리를 잡는다(가장 중요한 숫자) · 나머지는 빈 자리가 없으면 숨긴다
                for (int j = 0; j < n; j++)
                {
                    int i = (s.gauge + j) % n;
                    if (s.shape == Shape.Rect && i >= 2) continue;
                    SetLen(iLen[i], IV, n, i, Words.CmR((long)s.os[i] * s.handle, Judge.GaugeOrig(s)) + " cm");
                }
            }
            else
            {
                gaugeLbl.text = Words.GaugeName(s);
                PlaceSideLabel(gaugeLbl, IV, n, s.gauge, 0.7f, SheetY + 0.06f, 10);
            }
        }

        void SetLen(TextMeshPro t, Vector2[] v, int n, int i, string s)
        {
            if (t.text != s) t.text = s;
            PlaceSideLabel(t, v, n, i, 0.62f, SheetY + 0.06f, 10);
        }

        /// <summary>주홍 잉크 인상(교정 롤러가 지나간 곳까지만 — clipZ 위쪽). bleed = 번짐 두께(cm).</summary>
        void DrawImpression(Sheet s, float clipZ, float bleed, bool misreg, float alpha)
        {
            impInk.Clear();
            int n = s.N;
            var tmp = new List<Vector2>(8);
            var g = new Vector2[4];
            if (misreg)
            {
                // 겹인화: 어긋난 두 장(주홍 + 먹 회색)
                for (int i = 0; i < n; i++) g[i] = IV[i] + new Vector2(0.45f, -0.3f);
                ClipFill(g, n, clipZ, C32(Verm, 0.42f * alpha), tmp);
                for (int i = 0; i < n; i++) g[i] = IV[i] + new Vector2(-0.25f, 0.2f);
                ClipFill(g, n, clipZ, C32(InkC, 0.28f * alpha), tmp);
                impInk.Outline(g, n, 0.06f, C32(InkC, 0.4f * alpha));
            }
            else
            {
                if (bleed > 0.01f) { Grow(IV, n, bleed, g); ClipFill(g, n, clipZ, C32(Verm, 0.22f * alpha), tmp); }
                ClipFill(IV, n, clipZ, C32(Verm, 0.9f * alpha), tmp);
                Shrink(IV, n, 0.1f, g); ClipFill(g, n, clipZ, C32(Hex("C8281E"), 0.95f * alpha), tmp);
            }
            impInk.Apply();
        }

        /// <summary>볼록 다각형을 z ≥ clipZ 반평면으로 자른 뒤 채운다(롤러가 지나간 부분).</summary>
        void ClipFill(Vector2[] p, int n, float clipZ, Color32 col, List<Vector2> o)
        {
            o.Clear();
            for (int i = 0; i < n; i++)
            {
                var a = p[i]; var b = p[(i + 1) % n];
                bool ai = a.y >= clipZ, bi = b.y >= clipZ;
                if (ai) o.Add(a);
                if (ai != bi) { float t = (clipZ - a.y) / (b.y - a.y); o.Add(Vector2.Lerp(a, b, t)); }
            }
            if (o.Count >= 3) impInk.Poly(o, o.Count, col);
        }
    }
}
