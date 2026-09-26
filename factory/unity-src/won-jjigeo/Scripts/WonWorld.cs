// 원 찍어 v3 — 월드·재질·텍스처·소리·UI 구성·카메라 (WonJjigeoGame 의 partial)
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.WonJjigeo
{
    public partial class WonJjigeoGame
    {
        // ── 팔레트 (기획서: #D2C4A8 모래 베드 · #1F6B5A 녹청 · #9B2C2C 산화 적 · #2A2420 철 · #E8DCC8 각인 크림)
        static readonly Color Sand = Hex("D2C4A8"), Verd = Hex("1F6B5A"), VerdLit = Hex("3FB597"), Rust = Hex("9B2C2C"),
            Iron = Hex("2A2420"), Cream = Hex("E8DCC8"), Brass = Hex("B08D57"), Ink = Hex("1C2630"),
            Match = Hex("FF3B2E"), Pair = Hex("5FD3B3"), Amber = Hex("F0A83C");
        static Color Hex(string h) => MgfLook.Hex(h);
        const float CELL = 0.5f, PlateH = 0.2f, FilmY = 0.235f, LineY = 0.26f;
        const float FX = (Rules.GX + 0.7f) * CELL, FZ = (Rules.GY + 0.7f) * CELL;   // 트레이싱 필름 반폭·반높이(월드)
        const float BedHX = 4.5f, BedHZ = 4.0f;
        static readonly System.Type[] keepTypes = { typeof(SphereCollider), typeof(CapsuleCollider), typeof(MeshCollider) };

        Camera cam;
        Light keyLight;
        bool land, matchH;
        float camBlend, camPush;
        Transform world, staticRoot, tray, rackRail, console3d;
        Material sandMat, bronzeMat, ironMat, brassMat, steelMat, bentMat, scrapMat, lineMat, dashMat, glowMat, filmMat, shadowMat, clampMat;
        readonly List<Plate> platePool = new List<Plate>();
        readonly List<Plate> rack = new List<Plate>();
        readonly Pin[] pins = new Pin[3];
        readonly Transform[] clamps = new Transform[4];

        class Pin { public GameObject root; public Transform body; public Renderer[] rends; public Renderer head; public int slot; public bool bent; }
        class Plate { public GameObject go; public Mesh mesh; public MeshRenderer mr; public Vector3 pivot; }

        static Vector3 GW(double gx, double gy, float y) => new Vector3((float)gx * CELL, y, (float)gy * CELL);
        static Vector3 PW(RP p, float y) => GW(p.fx, p.fy, y);
        static Vector3 IW(IP p, float y) => GW(p.x, p.y, y);

        // ─────────────────────────────── 월드
        void BuildWorld()
        {
            world = new GameObject("World").transform;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("5A4B3C") * 0.6f;
            RenderSettings.ambientEquatorColor = Hex("3A3029") * 0.5f;
            RenderSettings.ambientGroundColor = Hex("15110E");
            RenderSettings.skybox = null;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("120E0B"); RenderSettings.fogStartDistance = 34f; RenderSettings.fogEndDistance = 70f;

            cam = MgfLook.Camera(new Vector3(0, 22, -6), Vector3.zero, 36f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("120E0B");
            cam.farClipPlane = 120f;

            keyLight = new GameObject("WorkLamp").AddComponent<Light>();
            keyLight.type = LightType.Spot;
            keyLight.transform.position = new Vector3(-1.2f, 16f, 2.5f);
            keyLight.transform.LookAt(new Vector3(0, 0, -0.8f));
            keyLight.spotAngle = 80f; keyLight.innerSpotAngle = 34f; keyLight.range = 40f;
            keyLight.intensity = 1.85f; keyLight.color = Hex("FFE2B8");
            keyLight.shadows = LightShadows.Soft; keyLight.shadowStrength = 0.75f;
            keyLight.shadowBias = 0.06f; keyLight.shadowNormalBias = 0.4f;
            // v1 의 좌하단 가로 줄무늬 = 스폿 그림자 깊이 정밀도 부족(근평면 0.2 → 16 떨어진 베드 가장자리에서 여드름·계단). 근평면을 9로 밀어 정밀도 확보
            keyLight.shadowNearPlane = 9f;
            keyLight.renderMode = LightRenderMode.ForcePixel;
            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.type = LightType.Directional; fill.transform.rotation = Quaternion.Euler(58, 140, 0);
            fill.color = Hex("8FA7B8"); fill.intensity = 0.3f; fill.shadows = LightShadows.None;
            if (MgfBridge.LowGfx) { fill.enabled = false; keyLight.shadows = LightShadows.Hard; RenderSettings.ambientSkyColor = Hex("5A4B3C") * 0.8f; }

            sandMat = MgfLook.Lit(Color.white, 0.08f, 0f, null, SandTex());
            sandMat.mainTextureScale = new Vector2(2.2f, 2.2f);
            sandMat.SetColor("_RimColor", new Color(1, 1, 1, 0f));
            var bronzeTex = BronzeTex();
            bronzeMat = MgfLook.Lit(Hex("C49A66"), 0.62f, 0.5f, Hex("120A04"), bronzeTex);
            bronzeMat.SetColor("_RimColor", new Color(1f, 0.85f, 0.6f, 0.35f));
            scrapMat = MgfLook.Lit(Hex("6A5040"), 0.25f, 0.55f, null, bronzeTex);
            var ironTex = IronTex();
            ironMat = MgfLook.Lit(Color.white, 0.22f, 0.35f, null, ironTex);
            ironMat.SetColor("_RimColor", new Color(1, 0.9f, 0.8f, 0.0f));
            brassMat = MgfLook.Lit(Brass, 0.6f, 0.85f, Hex("1A1206"));
            steelMat = MgfLook.Lit(Hex("A8ADB2"), 0.7f, 0.9f);
            bentMat = MgfLook.Lit(Hex("7A4A38"), 0.3f, 0.6f);
            clampMat = MgfLook.Lit(Hex("8C6A3A"), 0.55f, 0.85f, Hex("0A0603"));
            lineMat = new Material(MgfLook.Shader("MgfAlpha")) { name = "WonLine" };
            lineMat.SetColor("_Color", Color.white);
            dashMat = new Material(MgfLook.Shader("MgfAlpha")) { name = "WonDash", mainTexture = DashTex() };
            dashMat.SetColor("_Color", Color.white);
            glowMat = MgfLook.Additive(Color.white, MgfLook.SoftDot);
            filmMat = new Material(MgfLook.Shader("MgfAlpha")) { name = "WonFilm" };
            filmMat.SetColor("_Color", Color.white);
            shadowMat = new Material(MgfLook.Shader("MgfAlpha")) { name = "WonShadow" };
            shadowMat.SetColor("_Color", Color.white);
            // 필름은 다른 반투명(선·글자·표식)보다 먼저 그린다 — 거리순 정렬에 맡기면 우윳빛 필름이 선·라벨 위를 덮어 흐려진다(v3 캡처로 확인)
            filmMat.renderQueue = 2950; shadowMat.renderQueue = 2951;

            staticRoot = new GameObject("Static").transform;
            staticRoot.SetParent(world, false);
            if (!MgfBridge.LowGfx) NoCol(MgfLook.Block("Floor", new Vector3(0, -1.2f, -2), new Vector3(70, 1, 60), 0.2f, MgfLook.Lit(Hex("1C1612"), 0.2f, 0.3f), staticRoot));
            NoCol(MgfLook.Block("Bed", new Vector3(0, -0.3f, 0), new Vector3(BedHX * 2, 0.6f, BedHZ * 2), 0.05f, sandMat, staticRoot));
            float lx = BedHX, lz = BedHZ, lw = 0.75f;
            var rivetMat = MgfLook.Lit(Hex("5B4E44"), 0.55f, 0.8f);
            MakeLip(new Vector3(0, 0.12f, lz + lw / 2), new Vector3(lx * 2 + lw * 2, 0.5f, lw), rivetMat, 7, true);
            MakeLip(new Vector3(0, 0.12f, -lz - lw / 2), new Vector3(lx * 2 + lw * 2, 0.5f, lw), rivetMat, 7, true);
            MakeLip(new Vector3(-lx - lw / 2, 0.12f, 0), new Vector3(lw, 0.5f, lz * 2), rivetMat, 5, false);
            MakeLip(new Vector3(lx + lw / 2, 0.12f, 0), new Vector3(lw, 0.5f, lz * 2), rivetMat, 5, false);
            WorldUV(NoCol(MgfLook.Block("PressBody", new Vector3(0, -0.85f, -0.6f), new Vector3(lx * 2 + 2.6f, 0.9f, lz * 2 + 4.0f), 0.3f, ironMat, staticRoot)));
            // 어두운 철 틀·바닥은 그림자를 받지 않는다(스폿 원뿔 가장자리에서 거친 계단 그림자가 잡티로 보였다). 그림자는 베드·명판·핀·필름에서만.
            // 베드·틀은 그림자를 드리우지 않는다(자기 그림자 여드름 = v1 줄무늬의 원인). 그림자는 명판·핀만 드리운다.
            foreach (var r in staticRoot.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; if (r.gameObject.name != "Bed") r.receiveShadows = false; }
            if (MgfBridge.LowGfx)
            {
                // 소프트웨어 렌더러: 움직이지 않는 틀(립·리벳·받침)은 그림자를 드리우지 않는다(그림자 패스 정점 수 절감). 명판·핀 그림자는 유지.
                foreach (var r in staticRoot.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; if (r.gameObject.name != "Bed") r.receiveShadows = false; }
            }
            StaticBatchingUtility.Combine(staticRoot.gameObject);

            // 핀 트레이(목숨 3개) — 모눈 바로 아래
            tray = WorldUV(NoCol(MgfLook.Block("Tray", Vector3.zero, new Vector3(4.6f, 0.35f, 1.3f), 0.18f, ironMat, world))).transform;
            for (int i = 0; i < 3; i++)
            {
                var hole = MgfLook.Prim(PrimitiveType.Cylinder, "Hole" + i, new Vector3(-1.4f + 1.4f * i, 0.16f, 0), new Vector3(0.42f, 0.02f, 0.42f), MgfLook.Lit(Hex("0E0B09"), 0.1f), tray, false);
                hole.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            // 가로 화면: 왼쪽 완성 랙, 오른쪽 계기 콘솔(UI 계기판이 그 위에 얹힌다)
            rackRail = WorldUV(NoCol(MgfLook.Block("Rack", new Vector3(-8.2f, -0.2f, 0), new Vector3(1.7f, 0.25f, 8.0f), 0.12f, ironMat, world))).transform;
            var rl = MgfText.World("각인 완료", new Vector3(0, 0.2f, -3.55f), 3.4f, Hex("8C7D66"), rackRail);
            rl.transform.localRotation = Quaternion.Euler(90, 0, 0);
            console3d = WorldUV(NoCol(MgfLook.Block("Console", new Vector3(8.2f, -0.25f, 0.2f), new Vector3(3.0f, 0.3f, 8.6f), 0.2f, ironMat, world))).transform;
            foreach (var t in new[] { tray, rackRail, console3d }) foreach (var r in t.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; if (t != tray) r.receiveShadows = false; }
            if (MgfBridge.LowGfx)
                foreach (var t in new[] { tray, rackRail, console3d })
                    foreach (var r in t.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }

            for (int i = 0; i < 16; i++) platePool.Add(MakePlate(i));
            for (int i = 0; i < 3; i++) { pins[i] = MakePin("Pin" + i, brassMat); pins[i].slot = i; }
            demoPin = MakePin("DemoPin", brassMat);

            // 필름 모서리 집게(1단계에는 필름이 고정되어 있다 → 2단계 해금 때 튕겨 나간다)
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? -1 : 1, sz = i < 2 ? -1 : 1;
                var c = NoCol(MgfLook.Block("Clamp" + i, new Vector3(sx * (FX - 0.12f), FilmY + 0.06f, sz * (FZ - 0.12f)), new Vector3(0.62f, 0.14f, 0.62f), 0.06f, clampMat, world));
                foreach (var r in c.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;   // 작은 떠 있는 상자의 스폿 그림자가 거친 계단+빗금(그림자 여드름)으로 번지던 잡티 제거
                clamps[i] = c.transform;
            }
            BuildFilm();
            BuildLines();
            if (!MgfBridge.LowGfx) motes = MakeMotes();
        }

        static GameObject NoCol(GameObject g) { var c = g.GetComponent<Collider>(); if (c) Destroy(c); return g; }

        /// <summary>킷 RoundedBox 는 면마다 UV 0~1 이라 긴 블록에서 텍스처가 한 방향으로 5~14배 늘어나 가로 줄무늬로 보인다(v1 잡티).
        /// 메시를 복제해 UV 를 월드 크기(tile 월드 단위 = 텍스처 1장)로 다시 매긴다 — 면의 법선 주축으로 투영.</summary>
        static GameObject WorldUV(GameObject g, float tile = 2.2f)
        {
            var mf = g.GetComponent<MeshFilter>();
            var m = Instantiate(mf.sharedMesh);
            var v = m.vertices; var n = m.normals; var uv = new Vector2[v.Length];
            var o = g.transform.position;
            for (int i = 0; i < v.Length; i++)
            {
                var p = v[i] + o; var a = n[i];
                float ax = Mathf.Abs(a.x), ay = Mathf.Abs(a.y), az = Mathf.Abs(a.z);
                uv[i] = (ay >= ax && ay >= az ? new Vector2(p.x, p.z) : ax >= az ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y)) / tile;
            }
            m.uv = uv;
            mf.sharedMesh = m;
            return g;
        }

        void MakeLip(Vector3 pos, Vector3 size, Material rivetMat, int rivets, bool alongX)
        {
            WorldUV(NoCol(MgfLook.Block("Lip", pos, size, 0.12f, ironMat, staticRoot)));
            float len = alongX ? size.x : size.z;
            for (int i = 0; i < rivets; i++)
            {
                float t = (i + 0.5f) / rivets - 0.5f;
                var p = pos + (alongX ? new Vector3(t * len * 0.92f, size.y / 2, 0) : new Vector3(0, size.y / 2, t * len * 0.92f));
                var r = MgfLook.Prim(PrimitiveType.Sphere, "Rivet", p, new Vector3(0.24f, 0.12f, 0.24f), rivetMat, staticRoot, false);
                r.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        TextMeshPro FlatText(string s, float size, Color c)
        {
            var t = MgfText.World(s, Vector3.zero, size, c, world);
            t.transform.rotation = Quaternion.Euler(90, 0, 0);
            t.outlineWidth = 0.3f;
            t.outlineColor = new Color32(20, 16, 12, 255);
            t.gameObject.SetActive(false);
            return t;
        }

        LineRenderer Line(string name, float width, int count, bool loop, Material mat = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(world, false);
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.TransformZ;
            lr.sharedMaterial = mat ?? lineMat;
            lr.widthMultiplier = width;
            lr.positionCount = count;
            lr.loop = loop;
            lr.numCapVertices = loop ? 0 : 3;
            lr.numCornerVertices = 2;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.textureMode = mat != null && mat == dashMat ? LineTextureMode.Tile : LineTextureMode.Stretch;
            go.SetActive(false);
            return lr;
        }

        static void SetColor(LineRenderer lr, Color c) { lr.startColor = c; lr.endColor = c; }

        static void CirclePts(LineRenderer lr, Vector3 c, float r, int n, float frac = 1f)
        {
            int count = Mathf.Max(2, Mathf.CeilToInt((n - 1) * frac) + 1);
            if (lr.positionCount != count) lr.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Min(frac, (float)i / (n - 1)) * Mathf.PI * 2f;
                lr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
        }

        // ── 명판 메시(베벨 있는 삼각 판)
        Plate MakePlate(int i)
        {
            var go = new GameObject("Plate" + i);
            go.transform.SetParent(world, false);
            var mesh = new Mesh { name = "PlateMesh" };
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = bronzeMat;
            mr.shadowCastingMode = ShadowCastingMode.On; mr.receiveShadows = true;
            go.SetActive(false);
            return new Plate { go = go, mesh = mesh, mr = mr };
        }

        readonly List<Vector3> mv = new List<Vector3>(64);
        readonly List<Vector3> mn = new List<Vector3>(64);
        readonly List<Vector2> mu = new List<Vector2>(64);
        readonly List<int> mt = new List<int>(96);

        void ShapePlate(Plate pl, IP[] V)
        {
            var o = new Vector3[3];
            for (int i = 0; i < 3; i++) o[i] = IW(V[i], 0);
            var c = (o[0] + o[1] + o[2]) / 3f;
            pl.pivot = c;
            var inn = new Vector3[3];
            for (int i = 0; i < 3; i++) o[i] -= c;
            const float b = 0.11f;
            for (int i = 0; i < 3; i++)
            {
                var p = o[i]; var a = (o[(i + 1) % 3] - p).normalized; var d = (o[(i + 2) % 3] - p).normalized;
                float half = Vector3.Angle(a, d) * 0.5f * Mathf.Deg2Rad;
                inn[i] = p + (a + d).normalized * (b / Mathf.Max(0.2f, Mathf.Sin(half)));
            }
            if (Vector3.Cross(o[1] - o[0], o[2] - o[0]).y < 0) { (o[1], o[2]) = (o[2], o[1]); (inn[1], inn[2]) = (inn[2], inn[1]); }
            mv.Clear(); mn.Clear(); mu.Clear(); mt.Clear();
            float top = PlateH, bev = PlateH - 0.07f;
            int s0 = mv.Count;
            for (int i = 0; i < 3; i++) { var v = inn[i] + Vector3.up * top; mv.Add(v); mn.Add(Vector3.up); mu.Add(new Vector2(v.x + c.x, v.z + c.z) * 0.22f); }
            mt.Add(s0); mt.Add(s0 + 1); mt.Add(s0 + 2);
            for (int i = 0; i < 3; i++)
            {
                int j = (i + 1) % 3;
                var ao = o[i] + Vector3.up * bev; var bo = o[j] + Vector3.up * bev;
                var ai = inn[i] + Vector3.up * top; var bi = inn[j] + Vector3.up * top;
                Quad(ao, bo, bi, ai, c);
                Quad(o[i], o[j], bo, ao, c);
            }
            pl.mesh.Clear();
            pl.mesh.SetVertices(mv); pl.mesh.SetNormals(mn); pl.mesh.SetUVs(0, mu); pl.mesh.SetTriangles(mt, 0);
            pl.mesh.RecalculateBounds();
        }

        void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Vector3 c)
        {
            var n = Vector3.Cross(b - a, d - a).normalized;
            var mid = (a + b) * 0.5f;
            if (n.x * mid.x + n.z * mid.z < 0) n = -n;
            int s = mv.Count;
            mv.Add(a); mv.Add(b); mv.Add(cc); mv.Add(d);
            for (int k = 0; k < 4; k++) mn.Add(n);
            mu.Add(new Vector2(a.x + c.x, a.z + c.z + a.y) * 0.22f); mu.Add(new Vector2(b.x + c.x, b.z + c.z + b.y) * 0.22f);
            mu.Add(new Vector2(cc.x + c.x, cc.z + c.z + cc.y) * 0.22f); mu.Add(new Vector2(d.x + c.x, d.z + c.z + d.y) * 0.22f);
            if (Vector3.Dot(Vector3.Cross(b - a, cc - a), n) > 0) { mt.Add(s); mt.Add(s + 1); mt.Add(s + 2); mt.Add(s); mt.Add(s + 2); mt.Add(s + 3); }
            else { mt.Add(s); mt.Add(s + 2); mt.Add(s + 1); mt.Add(s); mt.Add(s + 3); mt.Add(s + 2); }
        }

        Plate TakePlate()
        {
            foreach (var p in platePool) if (!p.go.activeSelf) return p;
            var old = rack[0]; rack.RemoveAt(0); return old;
        }

        // ── 핀(피벗 = 핀 끝)
        static Mesh coneMesh;
        Pin MakePin(string name, Material headMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(world, false);
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            if (!coneMesh) coneMesh = Cone(0.07f, 0.2f, 14);
            var tip = new GameObject("Tip");
            tip.transform.SetParent(body, false);
            tip.AddComponent<MeshFilter>().sharedMesh = coneMesh;
            tip.AddComponent<MeshRenderer>().sharedMaterial = steelMat;
            MgfLook.Prim(PrimitiveType.Cylinder, "Shaft", new Vector3(0, 0.4f, 0), new Vector3(0.14f, 0.2f, 0.14f), headMat, body, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "Collar", new Vector3(0, 0.6f, 0), new Vector3(0.22f, 0.03f, 0.22f), headMat, body, false);
            var head = MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0, 0.7f, 0), new Vector3(0.42f, 0.24f, 0.42f), headMat, body, false);
            var p = new Pin { root = root, body = body, rends = root.GetComponentsInChildren<Renderer>(), head = head.GetComponent<Renderer>() };
            foreach (var r in p.rends) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
            return p;
        }

        static Mesh Cone(float r, float h, int seg)
        {
            var m = new Mesh { name = "Cone" };
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
                int s = v.Count;
                v.Add(Vector3.zero); v.Add(new Vector3(Mathf.Cos(a1) * r, h, Mathf.Sin(a1) * r)); v.Add(new Vector3(Mathf.Cos(a0) * r, h, Mathf.Sin(a0) * r));
                t.Add(s); t.Add(s + 1); t.Add(s + 2);
            }
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        void SetPinMat(Pin p, Material head) { foreach (var r in p.rends) if (r.gameObject.name != "Tip") r.sharedMaterial = head; }

        ParticleSystem motes;
        ParticleSystem MakeMotes()
        {
            var go = new GameObject("Motes");
            go.transform.SetParent(world, false);
            go.transform.position = new Vector3(0, 3.5f, 0);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false; main.maxParticles = 40;
            main.startLifetime = 5f; main.startSpeed = 0.12f; main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new Color(1f, 0.88f, 0.66f, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.004f;
            var em = ps.emission; em.rateOverTime = MgfBridge.LowGfx ? 3f : 6f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(10, 4, 8);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.15f; noise.frequency = 0.3f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = glowMat; r.shadowCastingMode = ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        // ── 절차적 텍스처(부팅 1회)
        static float Hash(int x, int y, int s) { unchecked { int h = x * 374761393 + y * 668265263 + s * 982451653; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
        static float VNoise(float x, float y, int s, int period)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int X0 = ((x0 % period) + period) % period, Y0 = ((y0 % period) + period) % period, X1 = (X0 + 1) % period, Y1 = (Y0 + 1) % period;
            float a = Hash(X0, Y0, s), b = Hash(X1, Y0, s), c = Hash(X0, Y1, s), d = Hash(X1, Y1, s);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
        static float Fbm(float u, float v, int s, int baseP)
        {
            float sum = 0, amp = 0.5f; int p = baseP;
            for (int o = 0; o < 4; o++) { sum += VNoise(u * p, v * p, s + o, p) * amp; amp *= 0.5f; p *= 2; }
            return sum;
        }

        Texture2D SandTex()
        {
            const int S = 256;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Sand", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float n = Fbm((float)x / S, (float)y / S, 11, 8), grain = Hash(x, y, 3);
                    float k = 0.9f + n * 0.16f + (grain - 0.5f) * 0.1f;
                    if (grain > 0.985f) k *= 0.72f;
                    var c = Sand * k;
                    px[y * S + x] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1);
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        Texture2D BronzeTex()
        {
            const int S = 128;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Bronze", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[S * S];
            var baseC = Hex("B88A58"); var dark = Hex("6E4A2C"); var pat = Hex("4F8C78");
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (float)x / S, v = (float)y / S;
                    float n = Fbm(u, v, 21, 4), m = Fbm(u, v, 57, 8);
                    var c = Color.Lerp(dark, baseC, 0.45f + n * 0.7f);
                    c = Color.Lerp(c, pat, Mathf.Clamp01((m - 0.62f) * 5f) * 0.55f);
                    c *= 0.94f + Hash(x, y, 9) * 0.1f;
                    px[y * S + x] = new Color(c.r, c.g, c.b, 1);
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        Texture2D IronTex()
        {
            const int S = 128;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Iron", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    var c = Color.Lerp(Hex("1E1916"), Hex("4A3E36"), Fbm((float)x / S, (float)y / S, 33, 4));
                    c *= 0.93f + Hash(x, y, 5) * 0.1f;   // (v1 의 녹 점은 큰 블록에서 늘어나 검은 얼룩처럼 보여 뺐다)
                    px[y * S + x] = new Color(c.r, c.g, c.b, 1);
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        static Texture2D DashTex()
        {
            var t = new Texture2D(16, 4, TextureFormat.RGBA32, false) { name = "Dash", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[64];
            for (int y = 0; y < 4; y++) for (int x = 0; x < 16; x++) px[y * 16 + x] = new Color32(255, 255, 255, (byte)(x < 9 ? 255 : 0));
            t.SetPixels32(px); t.Apply(false, true);
            return t;
        }

        static Sprite RoundSprite(int S, float rad, bool ring)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = ring ? "Ring" : "Round", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float a;
                    if (ring)
                    {
                        float dx = x + 0.5f - S / 2f, dy = y + 0.5f - S / 2f, d = Mathf.Sqrt(dx * dx + dy * dy);
                        float r = S / 2f - 2, w = S * 0.06f;
                        a = Mathf.Clamp01(Mathf.Clamp01(1 - Mathf.Abs(d - (r - w)) / w) * 2.2f);
                    }
                    else
                    {
                        float qx = Mathf.Max(Mathf.Abs(x + 0.5f - S / 2f) - (S / 2f - rad), 0), qy = Mathf.Max(Mathf.Abs(y + 0.5f - S / 2f) - (S / 2f - rad), 0);
                        a = Mathf.Clamp01(rad - Mathf.Sqrt(qx * qx + qy * qy) + 0.5f);
                    }
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply(false, true);
            float bd = ring ? 0 : rad + 2;
            return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(bd, bd, bd, bd));
        }

        // ─────────────────────────────── 소리(합성 — 외부 음원 없음)
        AudioSource sfx;
        AudioClip clClang, clThunk, clScrape, clSeat, clChime, clTick, clRefuse, clMatch, clFold, clCrease, clUnlock, clPing, clAlarm;

        void BuildSounds()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false; sfx.spatialBlend = 0;
            clClang = Synth("clang", 0.8f, t => Partials(t, new[] { 523f, 1307f, 2211f, 3120f }, new[] { 1f, 0.55f, 0.35f, 0.2f }, new[] { 3.2f, 5f, 7f, 9f }) * 0.5f);
            clThunk = Synth("thunk", 0.4f, t => (Mathf.Sin(2 * Mathf.PI * 92 * t) * Mathf.Exp(-t * 11) + Noise(t) * 0.35f * Mathf.Exp(-t * 26)) * 0.6f);
            clScrape = Synth("scrape", 0.7f, t => Noise(t) * 0.2f * Mathf.Sin(Mathf.PI * t / 0.7f) + Mathf.Sin(2 * Mathf.PI * (900 + t * 500) * t) * 0.03f);
            clSeat = Synth("seat", 0.12f, t => Mathf.Sin(2 * Mathf.PI * 1850 * t) * Mathf.Exp(-t * 55) * 0.5f);
            clChime = Synth("chime", 1.0f, t => Partials(t, new[] { 880f, 1320f, 1760f }, new[] { 0.8f, 0.5f, 0.25f }, new[] { 2.2f, 3f, 4f }) * 0.45f);
            clTick = Synth("tick", 0.035f, t => Mathf.Sin(2 * Mathf.PI * 2400 * t) * Mathf.Exp(-t * 160) * 0.35f + Noise(t) * 0.08f * Mathf.Exp(-t * 300));
            clRefuse = Synth("refuse", 0.22f, t => Mathf.Sign(Mathf.Sin(2 * Mathf.PI * (t < 0.1f ? 240 : 180) * t)) * 0.16f * Mathf.Exp(-t * 9));
            clMatch = Synth("match", 0.55f, t => (Mathf.Sin(2 * Mathf.PI * 1318.5f * t) * Mathf.Exp(-t * 7) + (t > 0.08f ? Mathf.Sin(2 * Mathf.PI * 1975.5f * (t - 0.08f)) * Mathf.Exp(-(t - 0.08f) * 6) : 0)) * 0.32f);
            clFold = Synth("fold", 0.32f, t => Noise(t) * 0.22f * Mathf.Sin(Mathf.PI * t / 0.32f) * (0.6f + 0.4f * Mathf.Sin(t * 90)));
            clCrease = Synth("crease", 0.16f, t => Noise(t) * 0.5f * Mathf.Exp(-t * 30) * (Mathf.Repeat(t * 70, 1f) < 0.5f ? 1 : 0.3f));
            clUnlock = Synth("unlock", 0.9f, t => Partials(t, new[] { 392f, 587f, 784f, 1175f }, new[] { 0.7f, 0.5f, 0.4f, 0.25f }, new[] { 2.5f, 3f, 3.5f, 4f }) * 0.45f);
            clPing = Synth("ping", 0.25f, t => Mathf.Sin(2 * Mathf.PI * 2637f * t) * Mathf.Exp(-t * 18) * 0.25f);
            clAlarm = Synth("alarm", 0.1f, t => Mathf.Sin(2 * Mathf.PI * 660 * t) * Mathf.Exp(-t * 25) * 0.25f);
        }

        static uint noiseSeed = 12345;
        static float Noise(float t) { noiseSeed = noiseSeed * 1664525u + 1013904223u; return ((noiseSeed >> 9) / 8388607f) * 2f - 1f; }
        static float Partials(float t, float[] f, float[] a, float[] d)
        {
            float s = 0; for (int i = 0; i < f.Length; i++) s += Mathf.Sin(2 * Mathf.PI * f[i] * t) * a[i] * Mathf.Exp(-t * d[i]);
            return s * Mathf.Min(1, t * 400);
        }
        static AudioClip Synth(string name, float dur, System.Func<float, float> f)
        {
            const int rate = 44100; int n = (int)(rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f((float)i / rate), -1, 1) * (1f - (float)i / n);
            var c = AudioClip.Create("won-" + name, n, 1, rate, false);
            c.SetData(data, 0);
            return c;
        }
        void Play(AudioClip c, float v = 0.8f) { if (c && st.phase != "title") sfx.PlayOneShot(c, v); }

        // ─────────────────────────────── UI
        Canvas canvas;
        RectTransform titleRoot, hudRoot, endRoot, badgeRt, ctaRt, ctaShine, endCtaRt, endCtaShine, timerFill, gaugeRt, plantRt, plantShine, progRt;
        TextMeshProUGUI badgeBig, badgeSub, stageTxt, progTxt, scoreTxt, comboTxt, bannerTxt, hintTxt, bestTxt, gaugeHead, plantTxt;
        TextMeshProUGUI endTitle, endPlates, endScore, endCombo, endBest;
        readonly TextMeshProUGUI[] gName = new TextMeshProUGUI[3], gVal = new TextMeshProUGUI[3], listTxt = new TextMeshProUGUI[3];
        readonly Image[] gBar = new Image[3], listBar = new Image[3];
        readonly RectTransform[] gCell = new RectTransform[3];
        RectTransform listRoot, cardRt, fingerRt;
        Image fingerImg, cardBg;
        TextMeshProUGUI cardBig, cardSub;
        CanvasGroup cardGroup, gaugeGroup;
        float cardT = 9f;
        readonly Image[] pipO = new Image[2], pipI = new Image[2];
        Image bannerBg, timerBg, plantFace, gaugeBg;
        Sprite roundSpr, ringSpr;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }
        Image Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color c, Sprite s = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = s; im.color = c; im.raycastTarget = false;
            if (s && s.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }
        TextMeshProUGUI Txt(Transform parent, string s, Vector2 anchor, Vector2 pos, float size, Color c, float width = 360, TextAlignmentOptions al = TextAlignmentOptions.Center)
        {
            var rt = R("T", parent, anchor, pos, new Vector2(width, size * 1.5f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = c; t.alignment = al; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; t.text = s;
            return t;
        }
        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero; }

        void BuildUi()
        {
            canvas = MgfText.Canvas;
            roundSpr = RoundSprite(64, 16, false);
            ringSpr = RoundSprite(128, 0, true);
            var ct = canvas.transform;

            // ── 타이틀
            titleRoot = R("Title", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Stretch(titleRoot);
            var logo = R("Logo", titleRoot, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(360, 200));
            Img("LogoRing", logo, new Vector2(0.5f, 0.5f), new Vector2(-70, 6), new Vector2(150, 150), Match, ringSpr);
            var pinTex = Resources.Load<Texture2D>("WonJjigeo/pin");
            if (pinTex)
            {
                var pinSpr = Sprite.Create(pinTex, new Rect(0, 0, pinTex.width, pinTex.height), new Vector2(0.5f, 0.5f));
                var pi = Img("LogoPin", logo, new Vector2(0.5f, 0.5f), new Vector2(-18, 58), new Vector2(92, 92), Color.white, pinSpr);
                pi.rectTransform.localRotation = Quaternion.Euler(0, 0, 8);
            }
            var logoShadow = Txt(logo, "원 찍어", new Vector2(0.5f, 0.5f), new Vector2(4, -6), 92, new Color(0.05f, 0.03f, 0.02f, 0.85f), 380);
            var logoT = Txt(logo, "원 찍어", new Vector2(0.5f, 0.5f), Vector2.zero, 92, Cream, 380);
            logoT.outlineWidth = 0.22f; logoT.outlineColor = new Color32(120, 24, 18, 255);
            logoShadow.outlineWidth = 0.22f; logoShadow.outlineColor = new Color32(10, 6, 4, 220);
            var tagBg = Img("TagBg", titleRoot, new Vector2(0.5f, 1f), new Vector2(0, -262), new Vector2(280, 36), new Color(0.1f, 0.08f, 0.06f, 0.8f), roundSpr);
            var tagT = Txt(tagBg.transform, "세 거리가 같아지면 빨개진다", new Vector2(0.5f, 0.5f), Vector2.zero, 19, Cream, 280);   // 1단계의 훅(타이틀 데모와 같은 장면)
            tagT.characterSpacing = 2;
            ctaRt = Img("Cta", titleRoot, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(236, 70), Hex("B98A56"), roundSpr).rectTransform;
            Img("CtaEdge", ctaRt, new Vector2(0.5f, 0.5f), new Vector2(0, -5), new Vector2(236, 70), Hex("5A3E24"), roundSpr).transform.SetAsFirstSibling();
            var ctaFace = Img("CtaFace", ctaRt, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(226, 60), Hex("D6AC72"), roundSpr);
            var mask = R("ShineMask", ctaRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(226, 64));
            mask.gameObject.AddComponent<RectMask2D>();
            ctaShine = Img("Shine", mask, new Vector2(0.5f, 0.5f), new Vector2(-200, 0), new Vector2(34, 110), new Color(1, 1, 1, 0.35f)).rectTransform;
            ctaShine.localRotation = Quaternion.Euler(0, 0, -22);
            var ctaT = Txt(ctaRt, "각인 시작", new Vector2(0.5f, 0.5f), new Vector2(0, 2), 30, Hex("2A1A0E"), 220);
            ctaT.fontStyle = FontStyles.Bold;
            ctaFace.transform.SetSiblingIndex(1);
            bestTxt = Txt(titleRoot, "", new Vector2(0.5f, 0f), new Vector2(0, 92), 17, Hex("BFAF94"), 360);
            var badge = Img("GradeBadge", titleRoot, new Vector2(0.5f, 0f), new Vector2(0, 52), new Vector2(300, 34), new Color(0.12f, 0.1f, 0.08f, 0.85f), roundSpr);
            Txt(badge.transform, "중2 · 삼각형의 성질 · 외심과 내심", new Vector2(0.5f, 0.5f), Vector2.zero, 16, Hex("D8C9AE"), 300);

            // ── HUD
            hudRoot = R("Hud", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Stretch(hudRoot);
            timerBg = Img("TimerBg", hudRoot, new Vector2(0.5f, 1f), new Vector2(0, -5), new Vector2(0, 8), new Color(0.1f, 0.08f, 0.06f, 0.9f));
            timerBg.rectTransform.anchorMin = new Vector2(0, 1); timerBg.rectTransform.anchorMax = new Vector2(1, 1); timerBg.rectTransform.sizeDelta = new Vector2(0, 8);
            var tf = Img("TimerFill", timerBg.transform, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, VerdLit);
            timerFill = tf.rectTransform;
            timerFill.anchorMin = new Vector2(0, 0); timerFill.anchorMax = new Vector2(1, 1); timerFill.pivot = new Vector2(0, 0.5f); timerFill.sizeDelta = Vector2.zero;
            var badgeImg = Img("Badge", hudRoot, new Vector2(0.5f, 1f), new Vector2(0, -54), new Vector2(178, 72), Hex("2A2420"), roundSpr);
            badgeRt = badgeImg.rectTransform;
            Img("BadgeFace", badgeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 64), Hex("3A302A"), roundSpr);
            badgeBig = Txt(badgeRt, "외심", new Vector2(0.5f, 0.5f), new Vector2(0, 9), 34, Cream, 170);
            badgeBig.outlineWidth = 0.2f; badgeBig.outlineColor = new Color32(31, 107, 90, 255);
            badgeSub = Txt(badgeRt, "", new Vector2(0.5f, 0.5f), new Vector2(0, -21), 13, Hex("CDBB9C"), 164);
            badgeSub.textWrappingMode = TextWrappingModes.NoWrap; badgeSub.enableAutoSizing = true; badgeSub.fontSizeMin = 8; badgeSub.fontSizeMax = 13;
            badgeSub.rectTransform.sizeDelta = new Vector2(164, 20);
            // 왼쪽: 단계 + 해금 진행(외심 ●● 내심 ●●)
            stageTxt = Txt(hudRoot, "", new Vector2(0f, 1f), new Vector2(57, -30), 15, Cream, 100, TextAlignmentOptions.Left);
            progRt = R("Prog", hudRoot, new Vector2(0f, 1f), new Vector2(57, -62), new Vector2(100, 40));
            progTxt = Txt(progRt, "", new Vector2(0.5f, 0.5f), new Vector2(0, 0), 13, Hex("CDBB9C"), 100, TextAlignmentOptions.Left);
            for (int k = 0; k < 2; k++)
            {
                Txt(progRt, k == 0 ? "외심" : "내심", new Vector2(0f, 0.5f), new Vector2(18, 9 - k * 19), 12, Hex("CDBB9C"), 40, TextAlignmentOptions.Left);
                for (int j = 0; j < 2; j++)
                {
                    var pip = Img("Pip", progRt, new Vector2(0f, 0.5f), new Vector2(50 + j * 16, 9 - k * 19), new Vector2(11, 11), new Color(1, 1, 1, 0.18f), roundSpr);
                    if (k == 0) pipO[j] = pip; else pipI[j] = pip;
                }
            }
            scoreTxt = Txt(hudRoot, "", new Vector2(1f, 1f), new Vector2(-56, -78), 20, Cream, 96, TextAlignmentOptions.Right);
            comboTxt = Txt(hudRoot, "", new Vector2(1f, 1f), new Vector2(-56, -97), 14, VerdLit, 96, TextAlignmentOptions.Right);
            bannerBg = Img("Banner", hudRoot, new Vector2(0.5f, 1f), new Vector2(0, -134), new Vector2(372, 50), new Color(0.1f, 0.08f, 0.06f, 0.88f), roundSpr);
            bannerTxt = Txt(bannerBg.transform, "", new Vector2(0.5f, 0.5f), Vector2.zero, 15.5f, Cream, 356);
            bannerTxt.rectTransform.sizeDelta = new Vector2(356, 46);
            // 긴 성질 문장은 두 줄로(한 줄에 욱여넣어 글자가 작아지지 않게)
            bannerTxt.enableAutoSizing = true; bannerTxt.fontSizeMin = 11; bannerTxt.fontSizeMax = 15.5f; bannerTxt.textWrappingMode = TextWrappingModes.Normal;
            bannerTxt.lineSpacing = -8;

            // 계기판(1단계: 세 길이 / 2단계: 주름 기록)
            gaugeBg = Img("Gauge", hudRoot, new Vector2(0.5f, 0f), new Vector2(0, 152), new Vector2(372, 100), new Color(0.09f, 0.07f, 0.055f, 0.94f), roundSpr);
            gaugeRt = gaugeBg.rectTransform;
            gaugeGroup = gaugeRt.gameObject.AddComponent<CanvasGroup>(); gaugeGroup.blocksRaycasts = false; gaugeGroup.interactable = false;
            Img("GaugeFace", gaugeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(364, 92), new Color(0.16f, 0.13f, 0.11f, 1f), roundSpr);
            gaugeHead = Txt(gaugeRt, "", new Vector2(0.5f, 1f), new Vector2(0, -15), 13, Hex("BFAF94"), 350);
            gaugeHead.textWrappingMode = TextWrappingModes.NoWrap; gaugeHead.enableAutoSizing = true; gaugeHead.fontSizeMin = 9; gaugeHead.fontSizeMax = 13;
            gaugeHead.rectTransform.sizeDelta = new Vector2(350, 20);
            for (int i = 0; i < 3; i++)
            {
                var cell = R("G" + i, gaugeRt, new Vector2(0.5f, 0.5f), new Vector2(-118 + i * 118, -8), new Vector2(112, 64));
                gCell[i] = cell;
                gName[i] = Txt(cell, "", new Vector2(0.5f, 1f), new Vector2(0, -10), 13, Hex("CDBB9C"), 110);
                gName[i].textWrappingMode = TextWrappingModes.NoWrap; gName[i].enableAutoSizing = true; gName[i].fontSizeMin = 8; gName[i].fontSizeMax = 13;
                gName[i].rectTransform.sizeDelta = new Vector2(110, 18);
                gVal[i] = Txt(cell, "", new Vector2(0.5f, 0.5f), new Vector2(0, -6), 27, Cream, 110);
                gVal[i].textWrappingMode = TextWrappingModes.NoWrap; gVal[i].enableAutoSizing = true; gVal[i].fontSizeMin = 10; gVal[i].fontSizeMax = 27;
                gVal[i].rectTransform.sizeDelta = new Vector2(110, 34);
                gBar[i] = Img("Bar", cell, new Vector2(0.5f, 0f), new Vector2(0, 3), new Vector2(84, 4), Cream, roundSpr);
            }
            // 2단계·다리 단계 계기판 목록 모드: 그은 선(또는 드러난 선) 이름 3줄
            listRoot = R("List", gaugeRt, new Vector2(0.5f, 0.5f), new Vector2(0, -9), new Vector2(350, 72));
            for (int i = 0; i < 3; i++)
            {
                listBar[i] = Img("LBar", listRoot, new Vector2(0f, 1f), new Vector2(10, -12 - i * 24), new Vector2(6, 18), Cream, roundSpr);
                listTxt[i] = Txt(listRoot, "", new Vector2(0f, 1f), new Vector2(186, -12 - i * 24), 15, Cream, 330, TextAlignmentOptions.Left);
                listTxt[i].rectTransform.sizeDelta = new Vector2(330, 22);
                listTxt[i].textWrappingMode = TextWrappingModes.NoWrap; listTxt[i].enableAutoSizing = true; listTxt[i].fontSizeMin = 10; listTxt[i].fontSizeMax = 15;
            }
            listRoot.gameObject.SetActive(false);

            // 박기 버튼(유일한 확정 입력 — 하단 n지선다가 아니다)
            plantRt = Img("Plant", hudRoot, new Vector2(0.5f, 0f), new Vector2(0, 56), new Vector2(250, 70), Hex("6E2A1E"), roundSpr).rectTransform;
            plantFace = Img("PlantFace", plantRt, new Vector2(0.5f, 0.5f), new Vector2(0, 3), new Vector2(240, 60), Hex("C8452F"), roundSpr);
            var pm = R("ShineMask", plantRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 64));
            pm.gameObject.AddComponent<RectMask2D>();
            plantShine = Img("Shine", pm, new Vector2(0.5f, 0.5f), new Vector2(-220, 0), new Vector2(30, 110), new Color(1, 1, 1, 0.3f)).rectTransform;
            plantShine.localRotation = Quaternion.Euler(0, 0, -22);
            plantTxt = Txt(plantRt, "핀 박기", new Vector2(0.5f, 0.5f), new Vector2(0, 3), 28, Hex("FFF1E0"), 230);
            plantTxt.outlineWidth = 0.18f; plantTxt.outlineColor = new Color32(70, 18, 10, 255);
            hintTxt = Txt(hudRoot, "", new Vector2(0.5f, 0f), new Vector2(0, 222), 15, Hex("E9D9BC"), 372);
            hintTxt.outlineWidth = 0.22f; hintTxt.outlineColor = new Color32(18, 14, 11, 255);
            hintTxt.rectTransform.sizeDelta = new Vector2(372, 44);
            hintTxt.enableAutoSizing = true; hintTxt.fontSizeMin = 11; hintTxt.fontSizeMax = 15; hintTxt.lineSpacing = -6;

            // 단계 전환 카드(판 위에 잠깐 — 입력을 막지 않는다. 다음 명판은 이미 떠 있다)
            // 계기판 자리에 겹쳐 뜬다(판을 가리지 않는다) — DoLayout 이 세로/가로에 맞춰 옮긴다
            cardBg = Img("Card", hudRoot, new Vector2(0.5f, 0f), new Vector2(0, 152), new Vector2(372, 100), new Color(0.12f, 0.09f, 0.07f, 0.97f), roundSpr);
            cardRt = cardBg.rectTransform;
            cardGroup = cardRt.gameObject.AddComponent<CanvasGroup>(); cardGroup.blocksRaycasts = false; cardGroup.interactable = false;
            Img("CardEdge", cardRt, new Vector2(0.5f, 0f), new Vector2(0, 6), new Vector2(330, 3), Match, roundSpr);
            cardBig = Txt(cardRt, "", new Vector2(0.5f, 0.5f), new Vector2(0, 14), 30, Cream, 350);
            cardBig.outlineWidth = 0.2f; cardBig.outlineColor = new Color32(120, 24, 18, 255);
            cardSub = Txt(cardRt, "", new Vector2(0.5f, 0.5f), new Vector2(0, -22), 17, Hex("E9D9BC"), 350);
            cardRt.gameObject.SetActive(false);
            // 유령 손가락(2단계 첫 외심·첫 내심 명판의 탭 시범)
            var fingerSpr = FingerSprite();
            fingerImg = Img("GhostFinger", hudRoot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 69), Color.white, fingerSpr);
            fingerImg.type = Image.Type.Simple;
            fingerRt = fingerImg.rectTransform;
            fingerRt.pivot = new Vector2(0.5f, 0.97f);   // 손가락 끝이 누르는 점
            fingerRt.gameObject.SetActive(false);

            // ── 결과
            endRoot = R("End", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Stretch(endRoot);
            var plaque = Img("EndPlaque", endRoot, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(320, 330), Hex("2A2420"), roundSpr);
            Img("EndFace", plaque.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(308, 318), Hex("3A302A"), roundSpr);
            Img("EndRing", plaque.transform, new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(64, 64), Match, ringSpr);
            endTitle = Txt(plaque.transform, "", new Vector2(0.5f, 1f), new Vector2(0, -68), 26, Cream, 300);
            endPlates = Txt(plaque.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0, 40), 44, Cream, 300);
            endPlates.outlineWidth = 0.2f; endPlates.outlineColor = new Color32(31, 107, 90, 255);
            endScore = Txt(plaque.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0, -12), 19, Hex("CDBB9C"), 300);
            endCombo = Txt(plaque.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0, -44), 15, Hex("CDBB9C"), 290);
            endBest = Txt(plaque.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0, -92), 16, VerdLit, 300);
            endCtaRt = Img("EndCta", endRoot, new Vector2(0.5f, 0.5f), new Vector2(0, -180), new Vector2(236, 66), Hex("B98A56"), roundSpr).rectTransform;
            Img("EndCtaFace", endCtaRt, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(226, 56), Hex("D6AC72"), roundSpr);
            var m2 = R("ShineMask", endCtaRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(226, 60));
            m2.gameObject.AddComponent<RectMask2D>();
            endCtaShine = Img("Shine", m2, new Vector2(0.5f, 0.5f), new Vector2(-200, 0), new Vector2(30, 100), new Color(1, 1, 1, 0.35f)).rectTransform;
            endCtaShine.localRotation = Quaternion.Euler(0, 0, -22);
            Txt(endCtaRt, "다시 각인", new Vector2(0.5f, 0.5f), new Vector2(0, 2), 28, Hex("2A1A0E"), 220);
        }

        /// <summary>계기판: 세 칸(거리 값) ↔ 목록(선 이름 3줄).</summary>
        void GaugeList(bool list)
        {
            if (listRoot.gameObject.activeSelf == list) return;
            listRoot.gameObject.SetActive(list);
            for (int i = 0; i < 3; i++) gCell[i].gameObject.SetActive(!list);
            lastGaugeKey = list ? "list" : "";
        }

        void ShowCard(string big, string sub)
        {
            cardBig.text = big; cardSub.text = sub;
            cardRt.gameObject.SetActive(true); cardT = 0f; cardGroup.alpha = 0f;
            gaugeGroup.alpha = 0f;   // 카드가 계기판 자리에 뜨는 동안 계기판은 숨긴다
            Play(clUnlock, 0.55f);
        }

        void HideCard() { cardT = 9f; if (cardRt) cardRt.gameObject.SetActive(false); if (gaugeGroup) gaugeGroup.alpha = 1f; }

        void UpdateCard(float dt)
        {
            if (cardT > 3f) return;
            cardT += dt;
            float a = cardT < 0.2f ? cardT / 0.2f : cardT > 2.5f ? 1f - (cardT - 2.5f) / 0.5f : 1f;
            cardGroup.alpha = Mathf.Clamp01(a);
            gaugeGroup.alpha = cardT > 2.5f ? Mathf.Clamp01((cardT - 2.5f) / 0.5f) : 0f;
            cardRt.localScale = Vector3.one * (cardT < 0.2f ? Mathf.Lerp(0.85f, 1.04f, cardT / 0.2f) : Mathf.Lerp(1.04f, 1f, Mathf.Clamp01((cardT - 0.2f) / 0.2f)));
            if (cardT > 3f) { cardRt.gameObject.SetActive(false); gaugeGroup.alpha = 1f; }
        }

        /// <summary>유령 손가락 스프라이트(코드 생성, 64×96): 뻗은 검지 + 손바닥, 흰 면 + 먹선 테두리. 끝이 위쪽 가운데.</summary>
        static Sprite FingerSprite()
        {
            const int W = 64, H = 96;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "Finger", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            float Cap(float x, float y, float ax, float ay, float bx, float by, float r)
            {
                float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
                float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
                float dx = pax - bax * h, dy = pay - bay * h; return Mathf.Sqrt(dx * dx + dy * dy) - r;
            }
            float Box(float x, float y, float cx, float cy, float hx, float hy, float r)
            {
                float qx = Mathf.Abs(x - cx) - hx + r, qy = Mathf.Abs(y - cy) - hy + r;
                return Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
            }
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float d = Cap(fx, fy, 30, 56, 30, 84, 9.5f);                        // 검지
                    d = Mathf.Min(d, Box(fx, fy, 33, 30, 21, 24, 11));                // 손바닥
                    d = Mathf.Min(d, Cap(fx, fy, 44, 44, 45, 52, 7.5f));              // 접은 가운뎃손가락
                    d = Mathf.Min(d, Cap(fx, fy, 14, 36, 8, 48, 6.5f));               // 엄지
                    float fill = Mathf.Clamp01(0.5f - d), edge = Mathf.Clamp01(0.5f - (d - 3.2f));
                    var ink = new Color(0.1f, 0.08f, 0.07f);
                    var c = Color.Lerp(ink, new Color(1f, 0.97f, 0.9f), Mathf.Clamp01(-d - 2.2f));
                    px[y * W + x] = new Color(c.r, c.g, c.b, Mathf.Max(fill, edge));
                }
            t.SetPixels32(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.97f), 100);
        }

        bool Over(RectTransform rt, Vector2 screen, float padPx = 0)
        {
            if (!rt || !rt.gameObject.activeInHierarchy) return false;
            if (RectTransformUtility.RectangleContainsScreenPoint(rt, screen, null)) return true;
            if (padPx <= 0) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, null, out var lp);
            var r = rt.rect; float s = canvas.scaleFactor;
            return lp.x >= r.xMin - padPx / s && lp.x <= r.xMax + padPx / s && lp.y >= r.yMin - padPx / s && lp.y <= r.yMax + padPx / s;
        }

        // ─────────────────────────────── 레이아웃·카메라
        void DoLayout()
        {
            bool l = cam.aspect >= 1.0f;
            // 세로라도 화면이 390×844 보다 넓적하면(태블릿 820×1180) 높이 기준으로 UI 를 맞춘다 — 폭 기준이면 HUD 가 커져 베드가 우표만 해진다
            bool mh = l || cam.aspect > 390f / 844f;
            if (l == land && mh == matchH && tray.position != Vector3.zero) return;
            land = l; matchH = mh;
            tray.position = new Vector3(0, -0.12f, -5.35f);
            rackRail.gameObject.SetActive(land);
            console3d.gameObject.SetActive(land);
            canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = matchH ? 1f : 0f;
            if (st.phase != "title") PlacePinsInTray();
            for (int i = 0; i < rack.Count; i++) SnapRack(rack[i], i);
            if (land)
            {
                // 가로: 계기판·박기 버튼은 오른쪽 거터(철 콘솔 위), 안내 한 줄은 그 위
                gaugeRt.anchorMin = gaugeRt.anchorMax = new Vector2(1f, 0.5f); gaugeRt.anchoredPosition = new Vector2(-196, 40);
                cardRt.anchorMin = cardRt.anchorMax = new Vector2(1f, 0.5f); cardRt.anchoredPosition = new Vector2(-196, 40);
                plantRt.anchorMin = plantRt.anchorMax = new Vector2(1f, 0.5f); plantRt.anchoredPosition = new Vector2(-196, -62);
                var hr = hintTxt.rectTransform; hr.anchorMin = hr.anchorMax = new Vector2(1f, 0.5f); hr.anchoredPosition = new Vector2(-196, 124); hr.sizeDelta = new Vector2(360, 60);
            }
            else
            {
                gaugeRt.anchorMin = gaugeRt.anchorMax = new Vector2(0.5f, 0f); gaugeRt.anchoredPosition = new Vector2(0, 152);
                cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0f); cardRt.anchoredPosition = new Vector2(0, 152);
                plantRt.anchorMin = plantRt.anchorMax = new Vector2(0.5f, 0f); plantRt.anchoredPosition = new Vector2(0, 58);
                var hr = hintTxt.rectTransform; hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0f); hr.anchoredPosition = new Vector2(0, 224); hr.sizeDelta = new Vector2(372, 44);
            }
        }

        Vector3 RackSlot(int k) { k %= 7; return new Vector3(-8.2f, 0.05f + k * 0.03f, 3.2f - k * 1.08f); }
        void SnapRack(Plate p, int k) { p.go.transform.position = RackSlot(k); p.go.transform.localScale = Vector3.one * 0.26f; p.go.transform.rotation = Quaternion.Euler(0, k * 23f, 0); }

        /// <summary>보여야 하는 월드 영역(모눈 베드 + 핀 트레이)을 위 HUD 와 아래 UI 띠 사이에 맞춘다(세로·가로 모두).</summary>
        void UpdateCamera(float dt)
        {
            float pitch = 76f, fov = 36f;
            cam.fieldOfView = fov;
            float th = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float a = Mathf.Max(0.3f, cam.aspect);
            // UI 기준 픽셀(세로=폭 390 기준, 가로=높이 844 기준)을 화면 비율로
            float refH = matchH ? 844f : 390f / a;
            float topPx = land ? 156f : 172f, botPx = land ? 24f : 262f;
            float hudTop = topPx / refH, hudBot = botPx / refH;
            float hw = land ? 10.2f : 4.98f;
            float zTop = 4.85f, zBot = -6.15f;
            float f = Mathf.Max(0.3f, 1f - hudTop - hudBot);
            float needH = (zTop - zBot) / f;
            float sp = Mathf.Sin(pitch * Mathf.Deg2Rad);
            float d = Mathf.Max(needH * 0.5f * sp / th, hw / (th * a));
            d *= 1f - camPush * 0.035f;
            float visH = 2f * d * th / sp;
            float yrc = hudBot + f * 0.5f;
            float cz = (zTop + zBot) * 0.5f - (yrc - 0.5f) * visH;
            var fwd = new Vector3(0, -Mathf.Sin(pitch * Mathf.Deg2Rad), Mathf.Cos(pitch * Mathf.Deg2Rad));
            var playPos = new Vector3(0, 0, cz) - fwd * d;
            var playRot = Quaternion.LookRotation(fwd, Vector3.forward);

            float t = Time.time;
            float orbit = Mathf.Sin(t * 0.18f) * 18f;
            float tp = land ? 52f : 62f, td = land ? 15f : 24f;
            var tFwd = Quaternion.Euler(0, orbit, 0) * new Vector3(0, -Mathf.Sin(tp * Mathf.Deg2Rad), Mathf.Cos(tp * Mathf.Deg2Rad));
            var titlePos = new Vector3(0, 0, land ? -0.6f : -1.0f) - tFwd * td;
            var titleRot = Quaternion.LookRotation(tFwd, Vector3.up);

            float goal = st.phase == "title" ? 0f : 1f;
            camBlend = Mathf.MoveTowards(camBlend, goal, dt * 1.6f);
            float e = camBlend * camBlend * (3 - 2 * camBlend);
            cam.transform.position = Vector3.Lerp(titlePos, playPos, e) + shakeOffset;
            cam.transform.rotation = Quaternion.Slerp(titleRot, playRot, e);
            camPush = Mathf.MoveTowards(camPush, 0, dt * 2.5f);
        }
    }
}
