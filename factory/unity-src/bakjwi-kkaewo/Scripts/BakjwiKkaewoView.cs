// 박쥐 깨워 — 자줏빛 절개 동굴 2.5D 월드. 거꾸로 매달린 돌잠 박쥐의 접힌 날개가 곧 수학 삼각형이다.
// 날개 삼각형은 실제 자료(변·각)로 그린 메시이고 카메라를 정면으로 향해 왜곡되지 않는다. 수치·문자는 화면 평행 UI.
// 배경 일러스트 없음: 바위층·선반·석순·주황 광물은 전부 코드 메시.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.BakjwiKkaewo
{
    public sealed partial class BakjwiKkaewoGame
    {
        static readonly Color Plum = MgfLook.Hex("#675083");
        static readonly Color Lilac = MgfLook.Hex("#9B82B1");
        static readonly Color Apricot = MgfLook.Hex("#FFC59A");
        static readonly Color Ivory = MgfLook.Hex("#F8F1DA");
        static readonly Color Tangerine = MgfLook.Hex("#DA7248");
        static readonly Color Deep = MgfLook.Hex("#34283F");
        static readonly Color BodyCol = MgfLook.Hex("#45325C");
        static readonly Color WingCol = MgfLook.Hex("#7E62A0");
        static readonly Color RockMid = MgfLook.Hex("#5A4775");
        static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, a); }

        // ───────────── 판형 ─────────────
        static readonly Vector2 PortraitDesign = new Vector2(390, 844), LandDesign = new Vector2(880, 480);
        bool land; float G = 1f, S = 1f; Vector2 design = PortraitDesign, origin;
        readonly Vector2[] grips = new Vector2[4];
        Vector2 tabletPos, tabletSize, toastPos;
        float batMul = 1f;
        const float WingCy = -56f, WingBoxW = 112f, WingBoxH = 80f;

        Camera cam;
        Transform worldRoot;
        Material rockMat, rockLightMat, rockMidMat, darkMat, bodyMat, apricotMat, wingMat, ivoryMat, deepMat, orangeMat, lilacMat;
        Material wingFoldMat, edgeMat, arcMat, haloMat, tabletMat, tabletTriMat, strokeMat, guideMat, dustMat;
        Mesh quadMesh;

        sealed class Tri
        {
            public Transform root;
            public Mesh fill, edge, arc;
            public RectTransform ui;
            public readonly TextMeshProUGUI[] letters = new TextMeshProUGUI[3];
            public readonly TextMeshProUGUI[] sides = new TextMeshProUGUI[3];
            public readonly Image[] sidePills = new Image[3];
            public readonly TextMeshProUGUI[] angles = new TextMeshProUGUI[3];
            public readonly Image[] anglePills = new Image[3];
            public readonly Vector2[] vtx = new Vector2[3];
            public Vector2 center;
        }

        sealed class Bat
        {
            public Transform root, wingPivot, eyeL, eyeR, hiL, hiR, earL, earR, halo;
            public Tri tri;
            public RectTransform ui;
            public TextMeshProUGUI idTxt, evidence, verdict;
            public Image idDisc, evidencePill, verdictPill;
            public Vector2 grip;
            public float mul = 1f, enterDelay, enterT = 1f;
            public Vector3 home, flyFrom;
            public bool awake;
            public float peek;
        }

        readonly Bat[] bats = new Bat[4];
        Bat titleBat;
        Tri refTri;
        Transform tabletBlock;
        readonly Transform[] ledges = new Transform[4];
        Transform crownL, crownR, bottomLip;
        readonly List<Transform> backRocks = new List<Transform>();
        readonly List<Vector4> backRockSpec = new List<Vector4>();
        readonly List<Transform> strata = new List<Transform>();
        readonly List<Vector4> strataSpec = new List<Vector4>();
        readonly List<Transform> stalactites = new List<Transform>();
        readonly List<Transform> crystals = new List<Transform>();
        readonly Transform[] dust = new Transform[22];
        readonly Vector3[] dustSeed = new Vector3[22];

        LineRenderer strokeLine, guideLine;
        Transform guideBrush;
        readonly Vector3[] strokePoints = new Vector3[160];
        int strokePointCount;
        readonly Vector3[] guidePts = new Vector3[28];

        // UI
        RectTransform uiRoot, playRoot, titleRoot;
        CanvasGroup titleGroup, playGroup, endGroup, toastGroup, interludeGroup;
        TextMeshProUGUI logoTxt, logoShadow, tagTxt, ctaTxt, badgeTxt, bestTxt;
        TextMeshProUGUI goalTxt, stageTxt, hintTxt, timeTxt, scoreTxt, firstTxt, toastTxt;
        readonly TextMeshProUGUI[] refLines = new TextMeshProUGUI[3];
        readonly Image[] feathers = new Image[3];
        readonly Image[] dots = new Image[10];
        Image toastBg, rippleImg, fallingFeather, flashImg, guideRingA, guideRingB, ctaRing;
        TextMeshProUGUI interludeTitle, interludeBody, endTitle, endShadow, endStats, endCta;
        RectTransform tabletUi;
        Sprite roundSprite, ringSprite, featherSprite, dotSprite;

        float guideBoost, worldClock, toastClock, rippleClock, flashClock, featherClock, guideClock, nudgeClock;
        bool guideActive, guideIsPractice;
        Vector2 guideFrom, guideTo;
        int lastW = -1, lastH = -1, lastSecond = -1, lastSolved = -1, lastScore = -1;
        float displayScore, displayEndScore;
        int flapSoundRank = -1;

        float UiPx() { return MgfText.Canvas ? MgfText.Canvas.scaleFactor * G : 1f; }

        // ───────────── 월드 ─────────────
        void BuildWorld()
        {
            rockMat = MgfLook.Lit(Plum, .22f, .02f);
            rockLightMat = MgfLook.Lit(MgfLook.Hex("#86709F"), .26f, .02f);
            rockMidMat = MgfLook.Lit(RockMid, .2f, .02f);
            darkMat = MgfLook.Lit(MgfLook.Hex("#3D2F4D"), .16f, .02f);
            bodyMat = MgfLook.Lit(BodyCol, .62f, .02f);
            apricotMat = MgfLook.Lit(Apricot, .40f, .01f);
            wingMat = MgfLook.Lit(WingCol, .30f, .01f);
            wingFoldMat = MgfLook.Lit(MgfLook.Hex("#58447A"), .4f, .01f);
            ivoryMat = MgfLook.Unlit(Ivory);
            deepMat = MgfLook.Lit(Deep, .7f, .02f);
            orangeMat = MgfLook.Lit(Tangerine, .5f, .03f, Tangerine * .35f);
            lilacMat = MgfLook.Lit(Lilac, .3f, .01f);
            edgeMat = MgfLook.Unlit(Ivory);
            arcMat = MgfLook.Unlit(Apricot);
            haloMat = MgfLook.Alpha(A(Ivory, .55f), MgfLook.SoftDot);
            tabletMat = MgfLook.Lit(MgfLook.Hex("#A58EBC"), .24f, .01f);
            tabletTriMat = MgfLook.Lit(MgfLook.Hex("#5B4876"), .3f, .01f);
            strokeMat = MgfLook.Alpha(A(Ivory, .85f), MgfLook.SoftDot);
            guideMat = MgfLook.Alpha(A(Apricot, .9f), DashTexture());
            dustMat = MgfLook.Additive(A(Apricot, .8f), MgfLook.SoftDot);
            quadMesh = MakeQuad();

            MgfLook.Sky(MgfLook.Hex("#3A2C4C"), Plum, MgfLook.Hex("#2B2136"), .95f);
            var sun = MgfLook.Sun(new Vector3(42f, -32f, 0f), MgfLook.Hex("#FFD3B0"), 1.05f, .55f);
            sun.shadowBias = .02f;
            var rim = new GameObject("TangerineRim").AddComponent<Light>();
            rim.type = LightType.Directional; rim.color = MgfLook.Hex("#FF9A62"); rim.intensity = .45f; rim.shadows = LightShadows.None;
            rim.transform.rotation = Quaternion.Euler(20f, 150f, 0f);

            cam = MgfLook.Camera(Vector3.zero, Vector3.forward, 30f);
            cam.orthographic = true; cam.orthographicSize = 10f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Plum;
            cam.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
            cam.transform.position = -cam.transform.forward * 60f;
            cam.farClipPlane = 140f;

            worldRoot = new GameObject("PlumBasaltRoost").transform;

            // 둥근 다공질 바위 벽(깊이 40~50). 화면 비율 좌표로 배치하고 판형이 바뀌면 다시 놓는다.
            var rnd = new System.Random(7);
            for (int i = 0; i < 22; i++)
            {
                float fx = (float)rnd.NextDouble() * 1.2f - .1f, fy = (float)rnd.NextDouble() * 1.2f - .1f;
                float sz = .045f + (float)rnd.NextDouble() * .09f;
                Material m = i % 3 == 0 ? rockLightMat : i % 3 == 1 ? rockMat : rockMidMat;
                var go = MgfLook.Prim(PrimitiveType.Sphere, "PorousStone" + i, Vector3.zero, Vector3.one, m, worldRoot, false);
                Quiet(go, false, true);
                backRocks.Add(go.transform); backRockSpec.Add(new Vector4(fx, fy, sz, 41f + (i % 5) * 1.2f));
            }
            for (int i = 0; i < 8; i++)
            {
                float fx = (float)rnd.NextDouble(), fy = (float)rnd.NextDouble();
                var go = MgfLook.Prim(PrimitiveType.Sphere, "RoostHole" + i, Vector3.zero, Vector3.one, darkMat, worldRoot, false);
                Quiet(go, false, false);
                backRocks.Add(go.transform); backRockSpec.Add(new Vector4(fx, fy, .028f + (float)rnd.NextDouble() * .03f, 40f));
            }

            // 깊이가 다른 둥근 바위층(지층) — 화면 전체를 자줏빛 단면으로 채운다
            float[] fy0 = { .06f, .24f, .40f, .55f, .70f, .86f, 1.0f };
            for (int i = 0; i < fy0.Length; i++)
            {
                Material m = i % 3 == 0 ? rockMidMat : i % 3 == 1 ? rockMat : rockLightMat;
                var st0 = MgfLook.Block("Stratum" + i, Vector3.zero, Vector3.one, .5f, m, worldRoot);
                Quiet(st0, false, true);
                strata.Add(st0.transform); strataSpec.Add(new Vector4(fy0[i], .13f + (i % 2) * .05f, 48f + (i % 3) * 1.5f, (i % 2 == 0 ? 1 : -1) * (2f + i % 3)));
            }
            for (int i = 0; i < 4; i++)
            {
                ledges[i] = MgfLook.Block("RoostShelf" + i, Vector3.zero, Vector3.one, .3f, i % 2 == 0 ? rockLightMat : rockMidMat, worldRoot).transform;
            }
            crownL = MgfLook.Block("ArchCrownL", Vector3.zero, Vector3.one, .45f, rockLightMat, worldRoot).transform;
            crownR = MgfLook.Block("ArchCrownR", Vector3.zero, Vector3.one, .45f, rockLightMat, worldRoot).transform;
            bottomLip = MgfLook.Block("FloorLip", Vector3.zero, Vector3.one, .6f, darkMat, worldRoot).transform;
            tabletBlock = MgfLook.Block("ReferenceStone", Vector3.zero, Vector3.one, .35f, tabletMat, worldRoot).transform;

            for (int i = 0; i < 10; i++)
            {
                var s = MgfLook.Prim(PrimitiveType.Capsule, "Stalactite" + i, Vector3.zero, Vector3.one, rockLightMat, worldRoot, false);
                Quiet(s, true, true); stalactites.Add(s.transform);
            }
            for (int i = 0; i < 8; i++)
            {
                var c = MgfLook.Prim(PrimitiveType.Capsule, "TangerineMineral" + i, Vector3.zero, Vector3.one, orangeMat, worldRoot, false);
                Quiet(c, true, true); crystals.Add(c.transform);
            }
            for (int i = 0; i < dust.Length; i++)
            {
                var d = new GameObject("CaveMote" + i); d.transform.SetParent(worldRoot, false);
                d.AddComponent<MeshFilter>().sharedMesh = quadMesh;
                var r = d.AddComponent<MeshRenderer>(); r.sharedMaterial = dustMat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                dust[i] = d.transform; dustSeed[i] = new Vector3((float)rnd.NextDouble(), (float)rnd.NextDouble(), .4f + (float)rnd.NextDouble() * .8f);
            }

            refTri = new Tri { root = new GameObject("ReferenceTriangle").transform };
            refTri.root.SetParent(tabletBlock, false);
            InitTriMeshes(refTri, tabletTriMat);

            for (int i = 0; i < 4; i++) bats[i] = CreateBat("Sleeper" + (i + 1));
            titleBat = CreateBat("TitleBat");

            strokeLine = MakeLine("NeutralBrushStroke", strokeMat, 160, LineTextureMode.Stretch);
            guideLine = MakeLine("PracticeGhostPath", guideMat, guidePts.Length, LineTextureMode.Tile);
            guideLine.enabled = false;
            var gb = new GameObject("GhostBrush"); gb.transform.SetParent(worldRoot, false);
            gb.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            var gbr = gb.AddComponent<MeshRenderer>(); gbr.sharedMaterial = MgfLook.Alpha(A(Apricot, 1f), MgfLook.SoftDot); gbr.shadowCastingMode = ShadowCastingMode.Off;
            guideBrush = gb.transform; guideBrush.gameObject.SetActive(false);
        }

        static void Quiet(GameObject go, bool cast, bool receive)
        {
            var r = go.GetComponent<Renderer>(); r.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = receive;
        }

        Mesh MakeQuad()
        {
            var m = new Mesh { name = "BillQuad" };
            m.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 };
            m.RecalculateBounds(); return m;
        }

        static Texture2D DashTexture()
        {
            var t = new Texture2D(32, 8, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, name = "Dash" };
            var px = new Color32[32 * 8];
            for (int y = 0; y < 8; y++) for (int x = 0; x < 32; x++)
            {
                float dy = Mathf.Abs(y + .5f - 4f) / 4f;
                byte a = (byte)(x < 18 ? Mathf.Clamp01(1.2f - dy) * 255 : 0);
                px[y * 32 + x] = new Color32(255, 255, 255, a);
            }
            t.SetPixels32(px); t.Apply(false, true); return t;
        }

        LineRenderer MakeLine(string name, Material mat, int cap, LineTextureMode mode)
        {
            var go = new GameObject(name); go.transform.SetParent(worldRoot, false);
            var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = mat; lr.useWorldSpace = true;
            lr.positionCount = 0; lr.numCapVertices = 6; lr.numCornerVertices = 4; lr.textureMode = mode;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false; lr.alignment = LineAlignment.View;
            return lr;
        }

        GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, bool cast)
        {
            var go = MgfLook.Prim(type, name, pos, scale, m, parent, false);
            Quiet(go, cast, true); return go;
        }

        // 돌잠 박쥐: 한 종류 프리미티브 모델. 로컬 단위 = 설계 px, 원점 = 선반을 쥔 발.
        Bat CreateBat(string name)
        {
            var b = new Bat();
            b.root = new GameObject(name).transform; b.root.SetParent(worldRoot, false);
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -10f : 10f;
                var foot = Part(PrimitiveType.Capsule, "ShortFoot" + i, b.root, new Vector3(x, -5f, 4f), new Vector3(7f, 8f, 7f), apricotMat, false);
                foot.transform.localRotation = Quaternion.Euler(0, 0, i == 0 ? -10 : 10);
            }
            Part(PrimitiveType.Sphere, "SmoothPlumBody", b.root, new Vector3(0, -56f, 8f), new Vector3(82f, 100f, 56f), bodyMat, true);
            for (int s = 0; s < 2; s++)
            {
                float sx = s == 0 ? -1f : 1f;
                // 몸을 감싼 날개 막의 양쪽 주름(접힌 날개 가장자리)
                var lobe = Part(PrimitiveType.Sphere, "WingFold" + s, b.root, new Vector3(sx * 33f, -54f, 14f), new Vector3(26f, 84f, 22f), wingFoldMat, true);
                lobe.transform.localRotation = Quaternion.Euler(0, 0, sx * -12f);
            }
            Part(PrimitiveType.Sphere, "ApricotFace", b.root, new Vector3(0, -137f, -4f), new Vector3(62f, 50f, 46f), apricotMat, true);
            Part(PrimitiveType.Sphere, "RoundNose", b.root, new Vector3(0, -125f, -27f), new Vector3(14f, 10f, 10f), orangeMat, false);
            b.eyeL = Part(PrimitiveType.Sphere, "EyeL", b.root, new Vector3(-12.5f, -142f, -25f), Vector3.one, deepMat, false).transform;
            b.eyeR = Part(PrimitiveType.Sphere, "EyeR", b.root, new Vector3(12.5f, -142f, -25f), Vector3.one, deepMat, false).transform;
            b.hiL = Part(PrimitiveType.Sphere, "EyeShineL", b.root, new Vector3(-10.8f, -139.6f, -30f), Vector3.one * 3.4f, ivoryMat, false).transform;
            b.hiR = Part(PrimitiveType.Sphere, "EyeShineR", b.root, new Vector3(14.2f, -139.6f, -30f), Vector3.one * 3.4f, ivoryMat, false).transform;
            var cheekL = Part(PrimitiveType.Sphere, "CheekL", b.root, new Vector3(-21f, -131f, -18f), new Vector3(10f, 6f, 4f), orangeMat, false);
            var cheekR = Part(PrimitiveType.Sphere, "CheekR", b.root, new Vector3(21f, -131f, -18f), new Vector3(10f, 6f, 4f), orangeMat, false);
            cheekL.transform.localScale *= .8f; cheekR.transform.localScale *= .8f;
            b.earL = new GameObject("EarL").transform; b.earL.SetParent(b.root, false); b.earL.localPosition = new Vector3(-19f, -150f, -2f);
            b.earR = new GameObject("EarR").transform; b.earR.SetParent(b.root, false); b.earR.localPosition = new Vector3(19f, -150f, -2f);
            for (int s = 0; s < 2; s++)
            {
                Transform ear = s == 0 ? b.earL : b.earR;
                Part(PrimitiveType.Sphere, "Ear", ear, new Vector3(0, -12f, 0), new Vector3(19f, 34f, 9f), apricotMat, true);
                Part(PrimitiveType.Sphere, "EarInner", ear, new Vector3(0, -13f, -4.5f), new Vector3(9f, 21f, 5f), lilacMat, false);
            }
            // 접힌 날개(=수학 삼각형). 몸 앞에 두고 카메라를 정면으로 본다.
            b.wingPivot = new GameObject("FoldedWing").transform; b.wingPivot.SetParent(b.root, false); b.wingPivot.localPosition = new Vector3(0, 0, -34f);
            b.tri = new Tri { root = new GameObject("WingTriangle").transform };
            b.tri.root.SetParent(b.wingPivot, false);
            InitTriMeshes(b.tri, wingMat);
            var halo = new GameObject("BrushHalo"); halo.transform.SetParent(b.root, false);
            halo.AddComponent<MeshFilter>().sharedMesh = quadMesh;
            var hr = halo.AddComponent<MeshRenderer>(); hr.sharedMaterial = haloMat; hr.shadowCastingMode = ShadowCastingMode.Off; hr.receiveShadows = false;
            b.halo = halo.transform; b.halo.localPosition = new Vector3(0, WingCy, -31f); b.halo.localScale = new Vector3(190f, 160f, 1f);
            halo.SetActive(false);
            SetEyes(b, 0f);
            return b;
        }

        void InitTriMeshes(Tri t, Material fillMat)
        {
            t.fill = NewMeshChild(t.root, "Membrane", fillMat, 0f, true);
            t.edge = NewMeshChild(t.root, "IvoryEdges", edgeMat, -.6f, false);
            t.arc = NewMeshChild(t.root, "AngleArcs", arcMat, -1.2f, false);
        }

        Mesh NewMeshChild(Transform parent, string name, Material m, float z, bool cast)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(0, 0, z);
            var mesh = new Mesh { name = name }; mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = cast;
            return mesh;
        }

        void SetEyes(Bat b, float open)
        {
            float o = Mathf.Clamp01(open);
            Vector3 s = new Vector3(Mathf.Lerp(11f, 9f, o), Mathf.Lerp(2.4f, 10f, o), 5f);
            b.eyeL.localScale = s; b.eyeR.localScale = s;
            bool shine = o > .55f; b.hiL.gameObject.SetActive(shine); b.hiR.gameObject.SetActive(shine);
        }

        // ───────────── 삼각형(실제 자료 → 메시 + 라벨) ─────────────
        readonly List<Vector3> mv = new List<Vector3>(64);
        readonly List<int> mt = new List<int>(128);
        readonly List<Vector3> mn = new List<Vector3>(64);
        readonly float[] sx = new float[3], sy = new float[3];

        void LayoutTri(Tri t, SimilarityBand band, int[] v, int rotDeg, bool flip, float boxW, float boxH, Vector2 center, float sizePct)
        {
            BakjwiRules.ShapeOf(band, v, sx, sy);
            float rad = rotDeg * Mathf.Deg2Rad, c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            float minX = 1e9f, maxX = -1e9f, minY = 1e9f, maxY = -1e9f;
            for (int i = 0; i < 3; i++)
            {
                float x = flip ? -sx[i] : sx[i], y = sy[i];
                float rx = x * c - y * s, ry = x * s + y * c;
                t.vtx[i] = new Vector2(rx, ry);
                minX = Mathf.Min(minX, rx); maxX = Mathf.Max(maxX, rx); minY = Mathf.Min(minY, ry); maxY = Mathf.Max(maxY, ry);
            }
            float k = Mathf.Min(boxW / Mathf.Max(.001f, maxX - minX), boxH / Mathf.Max(.001f, maxY - minY)) * sizePct;
            Vector2 mid = new Vector2((minX + maxX) * .5f, (minY + maxY) * .5f);
            for (int i = 0; i < 3; i++) t.vtx[i] = (t.vtx[i] - mid) * k + center;
            t.center = (t.vtx[0] + t.vtx[1] + t.vtx[2]) / 3f;

            // 막
            mv.Clear(); mt.Clear(); mn.Clear();
            for (int i = 0; i < 3; i++) { mv.Add(t.vtx[i]); mn.Add(Vector3.back); }
            mt.Add(0); mt.Add(1); mt.Add(2); mt.Add(0); mt.Add(2); mt.Add(1);
            t.fill.Clear(); t.fill.SetVertices(mv); t.fill.SetNormals(mn); t.fill.SetTriangles(mt, 0); t.fill.RecalculateBounds();
            // 상아색 변(두께 3.4) + 꼭짓점 마디
            mv.Clear(); mt.Clear(); mn.Clear();
            for (int i = 0; i < 3; i++)
            {
                Vector2 p = t.vtx[i], q = t.vtx[(i + 1) % 3];
                Vector2 d = (q - p).normalized, n = new Vector2(-d.y, d.x) * 1.7f;
                AddQuad(p - n, p + n, q + n, q - n);
                Vector2 e = new Vector2(2.6f, 0), f = new Vector2(0, 2.6f);
                AddQuad(p - e - f, p + e - f, p + e + f, p - e + f);
            }
            t.edge.Clear(); t.edge.SetVertices(mv); t.edge.SetNormals(mn); t.edge.SetTriangles(mt, 0); t.edge.RecalculateBounds();
        }

        void AddQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int o = mv.Count;
            mv.Add(a); mv.Add(b); mv.Add(c); mv.Add(d);
            for (int i = 0; i < 4; i++) mn.Add(Vector3.back);
            mt.Add(o); mt.Add(o + 1); mt.Add(o + 2); mt.Add(o); mt.Add(o + 2); mt.Add(o + 3);
            mt.Add(o); mt.Add(o + 2); mt.Add(o + 1); mt.Add(o); mt.Add(o + 3); mt.Add(o + 2);
        }

        void BuildArcs(Tri t, bool a0, bool a1, bool a2)
        {
            mv.Clear(); mt.Clear(); mn.Clear();
            bool[] on = { a0, a1, a2 };
            for (int i = 0; i < 3; i++)
            {
                if (!on[i]) continue;
                Vector2 p = t.vtx[i], u = (t.vtx[(i + 1) % 3] - p).normalized, w = (t.vtx[(i + 2) % 3] - p).normalized;
                float au = Mathf.Atan2(u.y, u.x), aw = Mathf.Atan2(w.y, w.x);
                float delta = Mathf.DeltaAngle(au * Mathf.Rad2Deg, aw * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                const int seg = 10; float r0 = 11f, r1 = 15f;
                int o = mv.Count;
                for (int k = 0; k <= seg; k++)
                {
                    float a = au + delta * k / seg;
                    Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    mv.Add(p + dir * r0); mv.Add(p + dir * r1); mn.Add(Vector3.back); mn.Add(Vector3.back);
                }
                for (int k = 0; k < seg; k++)
                {
                    int q = o + k * 2;
                    mt.Add(q); mt.Add(q + 1); mt.Add(q + 3); mt.Add(q); mt.Add(q + 3); mt.Add(q + 2);
                    mt.Add(q); mt.Add(q + 3); mt.Add(q + 1); mt.Add(q); mt.Add(q + 2); mt.Add(q + 3);
                }
            }
            t.arc.Clear(); t.arc.SetVertices(mv); t.arc.SetNormals(mn); t.arc.SetTriangles(mt, 0); t.arc.RecalculateBounds();
        }

        void BuildTriLabels(Tri t, Transform parent, Color letterColor)
        {
            t.ui = AddRect("TriLabels", parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            for (int i = 0; i < 3; i++)
            {
                t.letters[i] = AddText(t.ui, "Vertex" + i, "D", Vector2.zero, Vector2.zero, 13, letterColor, 24, 20);
                t.sidePills[i] = AddImage(t.ui, "SidePill" + i, Vector2.zero, Vector2.zero, new Vector2(44, 20), A(Deep, .9f), roundSprite);
                t.sides[i] = AddText(t.sidePills[i].rectTransform, "Side" + i, "6 cm", new Vector2(.5f, .5f), Vector2.zero, 13, Ivory, 60, 20);
                t.anglePills[i] = AddImage(t.ui, "AnglePill" + i, Vector2.zero, Vector2.zero, new Vector2(36, 20), A(Apricot, .96f), roundSprite);
                t.angles[i] = AddText(t.anglePills[i].rectTransform, "Angle" + i, "60°", new Vector2(.5f, .5f), Vector2.zero, 13, Deep, 50, 20);
            }
        }

        static readonly string[] RefLetters = { "A", "B", "C" };

        // 화면 라벨: 변 길이는 변 위의 알약, 각은 안쪽 이등분선 위, 꼭짓점 문자는 바깥.
        void LabelTri(Tri t, SimilarityBand band, int[] v, string[] L, bool showSides, bool showAngles = true)
        {
            Vector2 c = t.center;
            for (int i = 0; i < 3; i++)
            {
                Vector2 p = t.vtx[i];
                Vector2 outDir = (p - c).normalized;
                t.letters[i].text = L[i]; t.letters[i].gameObject.SetActive(true);
                t.letters[i].rectTransform.anchoredPosition = p + outDir * 13f;
                t.sidePills[i].gameObject.SetActive(false); t.anglePills[i].gameObject.SetActive(false);
            }
            if (showSides && band == SimilarityBand.SSS) for (int i = 0; i < 3; i++) SidePill(t, i, i, v[i] + " cm");
            if (band == SimilarityBand.SAS)
            {
                if (showSides) { SidePill(t, 0, 0, v[0] + " cm"); SidePill(t, 1, 2, v[1] + " cm"); }
                if (showAngles) AnglePill(t, 0, v[2] + "°");
            }
            if (band == SimilarityBand.AA && showAngles) { AnglePill(t, 0, v[0] + "°"); AnglePill(t, 1, v[1] + "°"); }
        }

        // slot: 라벨 칸, edge: 변 번호(i → i+1)
        void SidePill(Tri t, int slot, int edge, string text)
        {
            Vector2 p = t.vtx[edge], q = t.vtx[(edge + 1) % 3];
            Vector2 mid = (p + q) * .5f, outN = (mid - t.center).normalized;
            Image pill = t.sidePills[slot]; pill.gameObject.SetActive(true);
            t.sides[slot].text = text;
            float w = t.sides[slot].GetPreferredValues(text).x + 10f;
            pill.rectTransform.sizeDelta = new Vector2(w, 19f);
            pill.rectTransform.anchoredPosition = mid + outN * 10f;
            ClampLabelX(pill.rectTransform, 97f);
        }

        // 각 라벨은 꼭짓점 바깥(이등분선 반대쪽)에 「∠K 45°」로 — 좁은 삼각형에서도 겹치지 않게. 각호는 안쪽.
        void AnglePill(Tri t, int vertex, string text)
        {
            Vector2 p = t.vtx[vertex];
            Vector2 u = (t.vtx[(vertex + 1) % 3] - p).normalized, w = (t.vtx[(vertex + 2) % 3] - p).normalized;
            Vector2 bis = (u + w).normalized;
            Image pill = t.anglePills[vertex]; pill.gameObject.SetActive(true);
            string label = "∠" + t.letters[vertex].text + " " + text;
            t.angles[vertex].text = label;
            float wpx = t.angles[vertex].GetPreferredValues(label).x + 10f;
            pill.rectTransform.sizeDelta = new Vector2(wpx, 19f);
            Vector2 pos = p - bis * (14f + wpx * .32f);
            pos.x = Mathf.Clamp(pos.x, -97f + wpx * .5f, 97f - wpx * .5f);
            pill.rectTransform.anchoredPosition = pos;
            t.letters[vertex].gameObject.SetActive(false);
        }

        void ClampLabelX(RectTransform rt, float half)
        {
            Vector2 a = rt.anchoredPosition; float w = rt.sizeDelta.x * .5f;
            a.x = Mathf.Clamp(a.x, -half + w, half - w); rt.anchoredPosition = a;
        }

        // ───────────── UI ─────────────
        void BuildUi()
        {
            roundSprite = MakeRoundedSprite(64, 14f, false);
            ringSprite = MakeRoundedSprite(64, 31f, true);
            dotSprite = MakeRoundedSprite(32, 15.5f, false);
            featherSprite = MakeFeatherSprite();
            Canvas canvas = MgfText.Canvas;
            uiRoot = FullRect("BakjwiUi", canvas.transform);
            playRoot = AddRect("PlayRoot", uiRoot, Vector2.zero, Vector2.zero, PortraitDesign); playRoot.pivot = Vector2.zero;
            titleRoot = AddRect("TitleRoot", uiRoot, Vector2.zero, Vector2.zero, PortraitDesign); titleRoot.pivot = Vector2.zero;
            playGroup = Group("PlayLayer", playRoot);
            titleGroup = Group("TitleLayer", titleRoot);
            endGroup = Group("EndLayer", titleRoot);
            interludeGroup = Group("InterludeLayer", playRoot);
            toastGroup = Group("ToastLayer", playRoot);

            // 타이틀: 바위 아치 위의 음각 로고 + 살아 있는 중앙 박쥐가 곧 CTA
            logoShadow = AddText(titleGroup.transform, "LogoShadow", "박쥐 깨워", Vector2.zero, Vector2.zero, 62, A(Deep, .9f), 380, 80);
            logoTxt = AddText(titleGroup.transform, "LogoEngraved", "박쥐 깨워", Vector2.zero, Vector2.zero, 62, Ivory, 380, 80);
            logoTxt.enableVertexGradient = true; logoTxt.colorGradient = new VertexGradient(Ivory, Ivory, Apricot, Tangerine);
            logoTxt.outlineColor = Deep; logoTxt.outlineWidth = .22f; logoTxt.characterSpacing = 4; logoShadow.characterSpacing = 4;
            tagTxt = AddText(titleGroup.transform, "Tagline", "닮은 날개를 한 획으로 쓸어라", Vector2.zero, Vector2.zero, 18, Apricot, 360, 30);
            ctaRing = AddImage(titleGroup.transform, "CtaRing", Vector2.zero, Vector2.zero, new Vector2(120, 120), A(Apricot, .8f), ringSprite);
            ctaTxt = AddText(titleGroup.transform, "LivingCta", "가운데 박쥐를 눌러 깨우기", Vector2.zero, Vector2.zero, 20, Ivory, 360, 34);
            var badge = AddImage(titleGroup.transform, "GradeBadge", Vector2.zero, Vector2.zero, new Vector2(232, 30), A(Deep, .78f), roundSprite);
            badgeTxt = AddText(badge.rectTransform, "BadgeText", "중2 · 2학기 · 도형의 닮음", new Vector2(.5f, .5f), Vector2.zero, 14, Ivory, 232, 30);
            bestTxt = AddText(titleGroup.transform, "Best", "", Vector2.zero, Vector2.zero, 14, Apricot, 300, 24);

            endShadow = AddText(endGroup.transform, "EndShadow", "", Vector2.zero, Vector2.zero, 40, A(Deep, .9f), 380, 60);
            endTitle = AddText(endGroup.transform, "EndTitle", "", Vector2.zero, Vector2.zero, 40, Ivory, 380, 60);
            endTitle.enableVertexGradient = true; endTitle.colorGradient = new VertexGradient(Ivory, Ivory, Apricot, Apricot);
            endTitle.outlineColor = Deep; endTitle.outlineWidth = .2f;
            var statsBg = AddImage(endGroup.transform, "StatsStone", Vector2.zero, Vector2.zero, new Vector2(320, 118), A(Deep, .84f), roundSprite);
            endStats = AddText(statsBg.rectTransform, "EndStats", "", new Vector2(.5f, .5f), Vector2.zero, 18, Ivory, 300, 110);
            endStats.textWrappingMode = TextWrappingModes.Normal; endStats.lineSpacing = 6;
            endCta = AddText(endGroup.transform, "Restart", "아무 곳이나 눌러 다시 깨우기", Vector2.zero, Vector2.zero, 19, Apricot, 360, 32);

            // 플레이 HUD
            goalTxt = AddText(playGroup.transform, "Goal", "닮은 날개만 쓸면 박쥐가 깨어난다", Vector2.zero, Vector2.zero, 16, Ivory, 300, 26);
            goalTxt.alignment = TextAlignmentOptions.Left;
            tabletUi = AddRect("TabletUi", playGroup.transform, Vector2.zero, Vector2.zero, new Vector2(350, 132));
            stageTxt = AddText(tabletUi, "Stage", "1단계 · SSS 닮음  1/3", new Vector2(.5f, 1f), new Vector2(0, 13), 14, Apricot, 330, 20);
            for (int i = 0; i < 3; i++) refLines[i] = AddText(tabletUi, "RefLine" + i, "", new Vector2(.5f, .5f), Vector2.zero, 17, Deep, 170, 24);
            hintTxt = AddText(tabletUi, "Hint", "", new Vector2(.5f, 0f), new Vector2(62, 13), 11.5f, A(Deep, .9f), 220, 18);
            BuildTriLabels(refTri, tabletUi, Deep);

            for (int i = 0; i < 4; i++) BuildBatUi(bats[i], i);

            timeTxt = AddText(playGroup.transform, "Timer", "2:00", Vector2.zero, Vector2.zero, 21, Ivory, 100, 30);
            scoreTxt = AddText(playGroup.transform, "Score", "0", Vector2.zero, Vector2.zero, 21, Apricot, 110, 30);
            for (int i = 0; i < 3; i++) feathers[i] = AddImage(playGroup.transform, "NoiseFeather" + i, Vector2.zero, Vector2.zero, new Vector2(16, 28), Ivory, featherSprite);
            for (int i = 0; i < 10; i++) dots[i] = AddImage(playGroup.transform, "SceneDot" + i, Vector2.zero, Vector2.zero, new Vector2(13, 13), A(Ivory, .25f), dotSprite);
            firstTxt = AddText(playGroup.transform, "FirstAttempt", "첫 시도 0/10 · AA 0/4", Vector2.zero, Vector2.zero, 14, A(Ivory, .9f), 300, 22);

            toastBg = AddImage(toastGroup.transform, "ToastStone", Vector2.zero, Vector2.zero, new Vector2(360, 52), A(Ivory, .96f), roundSprite);
            toastTxt = AddText(toastBg.rectTransform, "Toast", "", new Vector2(.5f, .5f), Vector2.zero, 14, Deep, 344, 50);
            toastTxt.textWrappingMode = TextWrappingModes.Normal;

            var ib = AddImage(interludeGroup.transform, "InterludeStone", Vector2.zero, Vector2.zero, new Vector2(352, 112), A(Deep, .93f), roundSprite);
            interludeTitle = AddText(ib.rectTransform, "InterludeTitle", "", new Vector2(.5f, .5f), new Vector2(0, 24), 24, Apricot, 340, 34);
            interludeBody = AddText(ib.rectTransform, "InterludeBody", "", new Vector2(.5f, .5f), new Vector2(0, -18), 15, Ivory, 330, 48);
            interludeBody.textWrappingMode = TextWrappingModes.Normal;

            guideRingA = AddImage(playGroup.transform, "GuideStart", Vector2.zero, Vector2.zero, new Vector2(70, 70), Color.clear, ringSprite);
            guideRingB = AddImage(playGroup.transform, "GuideEnd", Vector2.zero, Vector2.zero, new Vector2(70, 70), Color.clear, ringSprite);

            flashImg = AddImage(uiRoot, "ColorFlash", Vector2.zero, Vector2.zero, Vector2.zero, Color.clear, null);
            flashImg.rectTransform.anchorMin = Vector2.zero; flashImg.rectTransform.anchorMax = Vector2.one; flashImg.rectTransform.offsetMin = flashImg.rectTransform.offsetMax = Vector2.zero;
            rippleImg = AddImage(uiRoot, "PointerRipple", Vector2.zero, Vector2.zero, new Vector2(56, 56), Color.clear, ringSprite);
            fallingFeather = AddImage(uiRoot, "FallingFeather", Vector2.zero, Vector2.zero, new Vector2(22, 38), Color.clear, featherSprite);
            SetGroup(endGroup, false); SetGroup(playGroup, false); SetGroup(toastGroup, false); SetGroup(interludeGroup, false);
        }

        void BuildBatUi(Bat b, int i)
        {
            b.ui = AddRect("Bat" + (i + 1) + "Ui", playGroup.transform, Vector2.zero, Vector2.zero, Vector2.zero);
            b.idDisc = AddImage(b.ui, "IdDisc", Vector2.zero, new Vector2(-74, -16), new Vector2(26, 26), A(Deep, .9f), dotSprite);
            b.idTxt = AddText(b.idDisc.rectTransform, "Id", BakjwiRules.WingIds[i], new Vector2(.5f, .5f), Vector2.zero, 15, Apricot, 26, 26);
            BuildTriLabels(b.tri, b.ui, Ivory);
            b.verdictPill = AddImage(b.ui, "VerdictPill", Vector2.zero, new Vector2(0, 2), new Vector2(96, 22), Apricot, roundSprite);
            b.verdict = AddText(b.verdictPill.rectTransform, "Verdict", "", new Vector2(.5f, .5f), Vector2.zero, 13, Deep, 96, 22);
            b.evidencePill = AddImage(b.ui, "EvidencePill", Vector2.zero, new Vector2(0, WingCy), new Vector2(150, 44), A(Deep, .93f), roundSprite);
            b.evidence = AddText(b.evidencePill.rectTransform, "Evidence", "", new Vector2(.5f, .5f), Vector2.zero, 13, Ivory, 170, 44);
            b.evidence.lineSpacing = -8;
            b.verdictPill.gameObject.SetActive(false); b.evidencePill.gameObject.SetActive(false);
        }

        RectTransform FullRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; return rt;
        }

        CanvasGroup Group(string name, Transform parent)
        {
            RectTransform rt = FullRect(name, parent); var g = rt.gameObject.AddComponent<CanvasGroup>(); g.interactable = false; g.blocksRaycasts = false; return g;
        }

        RectTransform AddRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        Image AddImage(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite)
        {
            RectTransform rt = AddRect(name, parent, anchor, pos, size); var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.type = sprite == roundSprite ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color; img.raycastTarget = false; return img;
        }

        TextMeshProUGUI AddText(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, float size, Color color, float w, float h)
        {
            RectTransform rt = AddRect(name, parent, anchor, pos, new Vector2(w, h));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.fontStyle = FontStyles.Bold;
            t.color = color; t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.NoWrap; t.raycastTarget = false;
            t.overflowMode = TextOverflowModes.Overflow; t.text = text; return t;
        }

        Sprite MakeRoundedSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - size * .5f) - (size * .5f - radius), 0);
                float dy = Mathf.Max(Mathf.Abs(y + .5f - size * .5f) - (size * .5f - radius), 0);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float outer = Mathf.Clamp01(radius + .5f - dist);
                float a = ring ? outer * Mathf.Clamp01(dist - (radius - 4.5f)) : outer;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            float border = ring ? 0 : radius;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        Sprite MakeFeatherSprite()
        {
            const int w = 24, h = 40; var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float ny = (y + .5f) / h, cx = w * (.44f + ny * .1f);
                float half = Mathf.Sin(Mathf.Clamp01(ny * 1.1f) * Mathf.PI) * (w * .36f);
                float a = Mathf.Clamp01(half + .6f - Mathf.Abs(x + .5f - cx));
                if (y < 6 && Mathf.Abs(x + .5f - cx) < 1.2f) a = 1f;
                if (a > 0 && ((y + (x > cx ? 2 : 0)) % 7 == 0)) a *= .55f;   // 깃 결
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f));
        }

        void SetGroup(CanvasGroup g, bool on) { if (!g) return; g.alpha = on ? 1f : 0f; g.gameObject.SetActive(on); }

        // ───────────── 판형 배치 ─────────────
        void ApplyResponsiveLayout()
        {
            float sf = MgfText.Canvas.scaleFactor; if (sf <= 0f) sf = 1f;
            if (Screen.width == lastW && Screen.height == lastH && Mathf.Abs(sf - S) < .0001f) return;
            lastW = Screen.width; lastH = Screen.height; S = sf;
            float cw = Screen.width / S, ch = Screen.height / S;
            land = Screen.width > Screen.height * 1.05f;
            design = land ? LandDesign : PortraitDesign;
            G = Mathf.Min(cw / design.x, ch / design.y);
            origin = new Vector2((cw - design.x * G) * .5f, (ch - design.y * G) * .5f);
            foreach (var root in new[] { playRoot, titleRoot })
            {
                root.anchoredPosition = origin; root.sizeDelta = design; root.localScale = new Vector3(G, G, 1f);
            }
            batMul = land ? 1.06f : 1f;
            if (land)
            {
                for (int i = 0; i < 4; i++) grips[i] = new Vector2(110f + i * 220f, 318f);
                tabletPos = new Vector2(440, 394); tabletSize = new Vector2(380, 108); toastPos = new Vector2(440, 88);
                Place(goalTxt.rectTransform, new Vector2(170, 462)); goalTxt.rectTransform.sizeDelta = new Vector2(320, 26);
                Place(timeTxt.rectTransform, new Vector2(90, 40)); Place(scoreTxt.rectTransform, new Vector2(790, 40));
                for (int i = 0; i < 3; i++) Place(feathers[i].rectTransform, new Vector2(196 + i * 24, 40));
                for (int i = 0; i < 10; i++) Place(dots[i].rectTransform, new Vector2(341 + i * 22, 44));
                Place(firstTxt.rectTransform, new Vector2(440, 16));
            }
            else
            {
                grips[0] = new Vector2(98, 598); grips[1] = new Vector2(292, 598); grips[2] = new Vector2(98, 396); grips[3] = new Vector2(292, 396);
                tabletPos = new Vector2(195, 694); tabletSize = new Vector2(352, 128); toastPos = new Vector2(195, 180);
                Place(goalTxt.rectTransform, new Vector2(166, 814)); goalTxt.rectTransform.sizeDelta = new Vector2(300, 26);
                Place(timeTxt.rectTransform, new Vector2(62, 128)); Place(scoreTxt.rectTransform, new Vector2(326, 128));
                for (int i = 0; i < 3; i++) Place(feathers[i].rectTransform, new Vector2(171 + i * 24, 128));
                for (int i = 0; i < 10; i++) Place(dots[i].rectTransform, new Vector2(96 + i * 22, 86));
                Place(firstTxt.rectTransform, new Vector2(195, 52));
            }
            Place(tabletUi, tabletPos); tabletUi.sizeDelta = tabletSize;
            Place(toastBg.rectTransform, toastPos);
            Place(((RectTransform)interludeTitle.transform.parent), land ? new Vector2(440, 240) : new Vector2(195, 500));
            for (int i = 0; i < 4; i++) { bats[i].grip = grips[i]; bats[i].mul = batMul; Place(bats[i].ui, grips[i]); bats[i].ui.localScale = Vector3.one * batMul; }
            LayoutTitleUi();
            LayoutWorld();
            if (current != null) { LayoutTablet(); for (int i = 0; i < 4; i++) LayoutBatTri(i); }
            PlaceBatsHome();
            if (guideActive && guideIsPractice) { guideFrom = CandidateScreen(0); guideTo = CandidateScreen(1); }
        }

        static void Place(RectTransform rt, Vector2 p) { rt.anchorMin = rt.anchorMax = Vector2.zero; rt.anchoredPosition = p; }

        Vector2 DesignToScreen(Vector2 d) { return new Vector2((origin.x + d.x * G) * S, (origin.y + d.y * G) * S); }
        Vector3 DesignToWorld(Vector2 d, float depth) { Vector2 s = DesignToScreen(d); return cam.ScreenToWorldPoint(new Vector3(s.x, s.y, depth)); }
        float WorldPerDesign() { return G * S * 2f * cam.orthographicSize / Mathf.Max(1, Screen.height); }

        void LayoutTitleUi()
        {
            if (land)
            {
                Vector2 crown = new Vector2(440, 398);
                Place(logoTxt.rectTransform, crown + new Vector2(0, 2)); Place(logoShadow.rectTransform, crown + new Vector2(3, -3));
                Place(tagTxt.rectTransform, new Vector2(440, 330));
                Place(ctaTxt.rectTransform, new Vector2(440, 58));
                Place(badgeTxt.transform.parent as RectTransform, new Vector2(140, 26)); Place(bestTxt.rectTransform, new Vector2(744, 26));
                Place(endTitle.rectTransform, crown + new Vector2(0, 2)); Place(endShadow.rectTransform, crown + new Vector2(3, -3));
                Place(endStats.transform.parent as RectTransform, new Vector2(700, 200)); Place(endCta.rectTransform, new Vector2(440, 40));
            }
            else
            {
                Vector2 crown = new Vector2(195, 712);
                Place(logoTxt.rectTransform, crown + new Vector2(0, 2)); Place(logoShadow.rectTransform, crown + new Vector2(3, -3));
                Place(tagTxt.rectTransform, new Vector2(195, 630));
                Place(ctaTxt.rectTransform, new Vector2(195, 206));
                Place(badgeTxt.transform.parent as RectTransform, new Vector2(195, 148)); Place(bestTxt.rectTransform, new Vector2(195, 112));
                Place(endTitle.rectTransform, crown + new Vector2(0, 2)); Place(endShadow.rectTransform, crown + new Vector2(3, -3));
                Place(endStats.transform.parent as RectTransform, new Vector2(195, 236)); Place(endCta.rectTransform, new Vector2(195, 146));
            }
        }

        Vector2 TitleBatGrip() { return land ? new Vector2(440, 352) : new Vector2(195, 606); }
        float TitleBatMul() { bool end = gamePhase == GamePhase.End; return land ? (end ? 1.15f : 1.3f) : (end ? 1.4f : 1.8f); }
        Vector2 DecoGrip(int i)
        {
            if (land) return i == 0 ? new Vector2(96, 340) : i == 1 ? new Vector2(226, 290) : i == 2 ? new Vector2(654, 290) : new Vector2(784, 340);
            return i == 0 ? new Vector2(40, 600) : i == 1 ? new Vector2(350, 600) : i == 2 ? new Vector2(46, 384) : new Vector2(344, 384);
        }

        void LayoutWorld()
        {
            float wpd = WorldPerDesign();
            float wpp = 2f * cam.orthographicSize / Mathf.Max(1, Screen.height);
            float screenWorldH = 2f * cam.orthographicSize, screenWorldW = screenWorldH * Screen.width / Mathf.Max(1f, Screen.height);
            for (int i = 0; i < backRocks.Count; i++)
            {
                Vector4 sp = backRockSpec[i];
                Vector3 p = cam.ScreenToWorldPoint(new Vector3(sp.x * Screen.width, sp.y * Screen.height, sp.w));
                float size = sp.z * Mathf.Max(screenWorldH, screenWorldW * 1.1f);
                backRocks[i].position = p; backRocks[i].localScale = new Vector3(size * 1.25f, size, size * .6f);
            }
            for (int i = 0; i < strata.Count; i++)
            {
                Vector4 sp = strataSpec[i];
                strata[i].position = cam.ScreenToWorldPoint(new Vector3(Screen.width * (.5f + (i % 2 == 0 ? .04f : -.04f)), sp.x * Screen.height, sp.z));
                strata[i].rotation = Quaternion.Euler(0, 0, sp.w);
                strata[i].localScale = new Vector3(screenWorldW * 1.35f, sp.y * screenWorldH, 2.4f);
            }
            // 바닥 바위 턱(HUD 받침) — 반투명 패널이 아니라 월드의 어두운 바위
            float lipH = (land ? 66f : 162f) * wpd;
            Vector3 lip = cam.ScreenToWorldPoint(new Vector3(Screen.width * .5f, 0f, 30f)) + cam.transform.up * (lipH * .5f - .3f);
            bottomLip.position = lip; bottomLip.rotation = Quaternion.identity; bottomLip.localScale = new Vector3(screenWorldW * 1.2f, lipH, 6f);
            LayoutLedgesForPhase();
            for (int i = 0; i < dust.Length; i++) dust[i].localScale = Vector3.one * (6f + dustSeed[i].z * 5f) * wpd;
            _ = wpp;
        }

        bool titleLayoutActive;

        void LayoutLedgesForPhase()
        {
            if (!cam) return;
            float wpd = WorldPerDesign();
            float screenWorldW = 2f * cam.orthographicSize * Screen.width / Mathf.Max(1f, Screen.height);
            bool title = gamePhase == GamePhase.Title || gamePhase == GamePhase.Transition || gamePhase == GamePhase.End;
            titleLayoutActive = title;
            for (int i = 0; i < 4; i++) ledges[i].gameObject.SetActive(false);
            if (title)
            {
                Vector2 crown = land ? new Vector2(440, 398) : new Vector2(195, 712);
                float cwid = land ? 440f : 356f, chgt = land ? 104f : 118f;
                crownL.gameObject.SetActive(true); crownR.gameObject.SetActive(true);
                crownL.position = DesignToWorld(crown - new Vector2(cwid * .25f, 0), 30f); crownR.position = DesignToWorld(crown + new Vector2(cwid * .25f, 0), 30f);
                crownL.localScale = crownR.localScale = new Vector3(cwid * .5f * wpd + .02f, chgt * wpd, 3.4f);
                crownL.rotation = crownR.rotation = Quaternion.identity;
                // 장식 박쥐용 짧은 선반
                for (int i = 0; i < 4; i++)
                {
                    Vector2 g = DecoGrip(i);
                    ledges[i].gameObject.SetActive(true);
                    ledges[i].position = DesignToWorld(g + new Vector2(0, 9f), 30f); ledges[i].localScale = new Vector3(110f * wpd, 20f * wpd, 3f); ledges[i].rotation = Quaternion.identity;
                }
                PlaceDeco(true);
            }
            else
            {
                crownL.gameObject.SetActive(false); crownR.gameObject.SetActive(false);
                int rows = land ? 1 : 2;
                for (int r = 0; r < rows; r++)
                {
                    float y = grips[r * 2].y + 8f;
                    ledges[r].gameObject.SetActive(true);
                    Vector3 c = DesignToWorld(new Vector2(design.x * .5f, y), 31f);
                    ledges[r].position = c; ledges[r].localScale = new Vector3(screenWorldW * 1.2f, 18f * wpd, 1.5f); ledges[r].rotation = Quaternion.identity;
                }
                tabletBlock.position = DesignToWorld(tabletPos, 26f); tabletBlock.rotation = cam.transform.rotation;
                tabletBlock.localScale = new Vector3(tabletSize.x * wpd, tabletSize.y * wpd, 1.2f);
                PlaceDeco(false);
            }
            tabletBlock.gameObject.SetActive(!title);
        }

        void PlaceDeco(bool title)
        {
            float wpd = WorldPerDesign();
            for (int i = 0; i < stalactites.Count; i++)
            {
                Vector2 d;
                if (title) d = new Vector2(design.x * (.08f + .084f * i), (land ? 398f - 56f : 712f - 62f) - (i % 3) * 4f);
                else
                {
                    int row = land ? 0 : i % 2;
                    float y = grips[row * 2].y - 4f;
                    float[] xsP = { 195f, 18f, 372f, 150f, 240f }, xsL = { 220f, 440f, 660f, 20f, 860f };
                    d = new Vector2(land ? xsL[i % 5] : xsP[(i / 2) % 5], y);
                }
                stalactites[i].position = DesignToWorld(d, 29f) - cam.transform.up * (10f + (i % 3) * 5f) * wpd;
                stalactites[i].localScale = new Vector3(7f + i % 3 * 2f, 12f + (i % 4) * 5f, 7f) * wpd;
            }
            for (int i = 0; i < crystals.Count; i++)
            {
                Vector2 d;
                if (title) d = new Vector2(design.x * (.12f + .11f * i), land ? 456f : 776f);
                else
                {
                    int row = land ? 0 : i % 2;
                    float y = grips[row * 2].y + 22f;
                    d = new Vector2(design.x * (.06f + .13f * i) + (row * 20f), y);
                }
                crystals[i].position = DesignToWorld(d, 29f);
                crystals[i].localRotation = Quaternion.Euler(0, 0, (i % 5 - 2) * 14f);
                crystals[i].localScale = new Vector3(6f, 11f + (i % 3) * 4f, 6f) * wpd;
            }
        }

        void PlaceBatsHome()
        {
            float wpd = WorldPerDesign();
            for (int i = 0; i < 4; i++)
            {
                Bat b = bats[i];
                Vector2 g = titleLayoutActive ? DecoGrip(i) : grips[i];
                float mul = titleLayoutActive ? .6f : batMul;
                b.home = DesignToWorld(g, 24f);
                if (!(gamePhase == GamePhase.Reveal && b.awake))
                {
                    b.root.position = b.home; b.root.rotation = cam.transform.rotation; b.root.localScale = Vector3.one * wpd * mul;
                }
                b.mul = mul;
            }
            titleBat.home = DesignToWorld(TitleBatGrip(), 22f);
            if (gamePhase != GamePhase.Transition)
            {
                titleBat.root.position = titleBat.home; titleBat.root.rotation = cam.transform.rotation; titleBat.root.localScale = Vector3.one * wpd * TitleBatMul();
            }
            Vector2 face = TitleBatGrip() + new Vector2(0, -139f * TitleBatMul());
            Place(ctaRing.rectTransform, face);
        }

        // ───────────── 장면 표시 ─────────────
        void LayoutTablet()
        {
            if (current == null) return;
            float triX = -tabletSize.x * .5f + 82f;
            LayoutTri(refTri, current.band, current.target, current.band == SimilarityBand.AA ? 12 : 0, false, 112f, tabletSize.y - 48f, new Vector2(triX, -6f), 1f);
            BuildArcs(refTri, current.band != SimilarityBand.SSS, current.band == SimilarityBand.AA, false);
            LabelTri(refTri, current.band, current.target, RefLetters, false, false);
            refTri.root.localPosition = new Vector3(0, 0, -.55f);
            // 기준 블록은 월드 단위 크기를 가지므로 삼각형 루트를 설계 px 단위로 되돌린다
            float wpd = WorldPerDesign();
            refTri.root.localScale = new Vector3(wpd / Mathf.Max(.0001f, tabletBlock.localScale.x), wpd / Mathf.Max(.0001f, tabletBlock.localScale.y), wpd / Mathf.Max(.0001f, tabletBlock.localScale.z));
            int[] t = current.target;
            float lx = triX + 160f;
            if (current.band == SimilarityBand.SSS)
            {
                SetRefLine(0, "AB = " + t[0] + " cm", lx, 24f); SetRefLine(1, "BC = " + t[1] + " cm", lx, 0f); SetRefLine(2, "CA = " + t[2] + " cm", lx, -24f);
            }
            else if (current.band == SimilarityBand.SAS)
            {
                SetRefLine(0, "AB = " + t[0] + " cm", lx, 24f); SetRefLine(1, "AC = " + t[1] + " cm", lx, 0f); SetRefLine(2, "∠A = " + t[2] + "°", lx, -24f);
            }
            else
            {
                SetRefLine(0, "∠A = " + t[0] + "°", lx, 14f); SetRefLine(1, "∠B = " + t[1] + "°", lx, -12f); SetRefLine(2, "", lx, 0f);
            }
        }

        void SetRefLine(int i, string text, float x, float y) { refLines[i].text = text; refLines[i].rectTransform.anchoredPosition = new Vector2(x, y); }

        void LayoutBatTri(int i)
        {
            if (current == null) return;
            WingData w = current.wings[i];
            Bat b = bats[i];
            LayoutTri(b.tri, current.band, w.v, w.rotDeg, w.flip, WingBoxW, WingBoxH, new Vector2(0, WingCy), w.sizePct / 100f);
            BuildArcs(b.tri, current.band != SimilarityBand.SSS, current.band == SimilarityBand.AA, false);
            LabelTri(b.tri, current.band, w.v, w.letters, true);
            b.tri.ui.gameObject.SetActive(true);
        }

        static readonly int[] TitleWingDemo = { 5, 6, 7 };

        void ShowScene(BatScene p, bool enter)
        {
            LayoutLedgesForPhase();
            LayoutTablet();
            int band = (int)p.band;
            bool practice = p.id == "practice-fixed";
            int inBand = 1, before = 0;
            for (int k = 0; k < band - 1; k++) before += BakjwiRules.BandCounts[k];
            if (!practice) inBand = Mathf.Clamp(st.solved - before + 1, 1, BakjwiRules.BandCounts[band - 1]);
            stageTxt.text = practice ? "연습 · SSS 닮음" : band + "단계 · " + p.band + " 닮음   " + inBand + "/" + BakjwiRules.BandCounts[band - 1];
            // 단계 첫 장면에는 중립 안내, 이후로는 근거 문장을 줄인다(수치·각호는 그대로)
            bool showHint = practice || inBand == 1;
            hintTxt.text = !showHint ? "" : p.band == SimilarityBand.SSS ? "세 대응변의 길이의 비를 비교" : p.band == SimilarityBand.SAS ? "두 대응변의 길이의 비와 그 끼인각" : "두 각의 크기 · 남은 각은 180°에서 뺀다";
            goalTxt.text = practice ? "닮은 날개만 쓸면 박쥐가 깨어난다" : "닮은 날개를 모두 한 획으로 쓸어 손을 떼시오";
            for (int i = 0; i < 4; i++)
            {
                Bat b = bats[i];
                b.awake = false; LayoutBatTri(i);
                b.root.gameObject.SetActive(true);
                b.halo.gameObject.SetActive(false);
                b.verdictPill.gameObject.SetActive(false); b.evidencePill.gameObject.SetActive(false);
                b.ui.gameObject.SetActive(true);
                SetEyes(b, 0f);
                b.wingPivot.localScale = Vector3.one;
                if (enter) { b.enterT = 0f; b.enterDelay = i * .07f; }
            }
            PlaceBatsHome();
            if (enter) MgfSfx.Play("pop", .22f);
        }

        void ClearTouchMarks()
        {
            for (int i = 0; i < 4; i++) { bats[i].halo.gameObject.SetActive(false); SetEyes(bats[i], 0f); }
        }

        Vector2 CandidateScreen(int i)
        {
            Vector2 g = grips[i] + new Vector2(0, WingCy * batMul);
            return DesignToScreen(g);
        }

        float CandidateHitRadius() { return 52f * batMul * G * S; }

        // ───────────── 획 ─────────────
        void BeginStroke(Vector2 screen)
        {
            strokePointCount = 0; strokeLine.enabled = true;
            float w = 9f * WorldPerDesign(); strokeLine.startWidth = w; strokeLine.endWidth = w * .55f;
            AddStrokePoint(screen);
            if (gamePhase != GamePhase.Practice) StopGuide();
        }

        void AddStrokePoint(Vector2 screen)
        {
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 12f));
            if (strokePointCount > 0 && Vector3.Distance(strokePoints[strokePointCount - 1], w) < .05f) return;
            if (strokePointCount < strokePoints.Length) strokePoints[strokePointCount++] = w;
            else
            {
                System.Array.Copy(strokePoints, 1, strokePoints, 0, strokePoints.Length - 1);
                strokePoints[strokePoints.Length - 1] = w;
            }
            strokeLine.positionCount = strokePointCount; strokeLine.SetPositions(strokePoints);
        }

        void EndStroke() { strokeLine.enabled = false; strokeLine.positionCount = 0; strokePointCount = 0; }
        void ResetStrokeVisual() { if (strokeLine) { strokeLine.enabled = false; strokeLine.positionCount = 0; } strokePointCount = 0; }

        void StrokeSound(bool start) { if (start) MgfSfx.Play("tap", .12f); }

        // 접촉 표시는 정답 여부와 무관하게 모두 같은 중립 붓 자국이다.
        void MarkCandidateTouched(int index, int rank)
        {
            Bat b = bats[index];
            b.halo.gameObject.SetActive(true);
            SetEyes(b, .35f);
            MgfFx.Punch(b.wingPivot, .10f, .2f);
            MgfSfx.Play("tap", .16f + rank * .02f);
        }

        // ───────────── 판정 연출 ─────────────
        void BeginRevealVisual(bool correct, bool practice, int mask, BatScene p, bool firstTry)
        {
            StopGuide(); EndStroke();
            flapSoundRank = -1;
            for (int i = 0; i < 4; i++)
            {
                Bat b = bats[i];
                b.flyFrom = b.root.position;
                bool sim = (p.correctMask & (1 << i)) != 0, picked = (mask & (1 << i)) != 0;
                b.awake = correct && picked;
                b.halo.gameObject.SetActive(false);
                // 근거: 각 날개 자리에 실제 비·각 계산이 남는다(색 + 기호 + 문장)
                b.tri.ui.gameObject.SetActive(false);
                b.evidencePill.gameObject.SetActive(true);
                b.evidence.text = BakjwiRules.Evidence(p, i);
                b.verdictPill.gameObject.SetActive(true);
                if (sim && picked) { b.verdict.text = "○ 닮음"; b.verdictPill.color = Apricot; }
                else if (sim) { b.verdict.text = "○ 닮음 · 놓침"; b.verdictPill.color = Ivory; }
                else if (picked) { b.verdict.text = "× 닮지 않음"; b.verdictPill.color = Tangerine; }
                else { b.verdict.text = "닮지 않음"; b.verdictPill.color = A(Lilac, .9f); }
                b.verdictPill.rectTransform.sizeDelta = new Vector2(b.verdict.GetPreferredValues(b.verdict.text).x + 16f, 22f);
                MgfFx.Punch(b.evidencePill.rectTransform, .1f, .22f);
            }
            if (correct)
            {
                string note = BakjwiRules.MaskAnswer(mask) + "번 " + p.reveal;
                ShowToast(practice ? "3:6=4:8=5:10=1:2 · 3:9=4:12=5:15=1:3\n세 대응변의 길이의 비가 같다 → SSS 닮음" : note + (firstTry ? "  +150" : "  +100"), RevealCorrectSec, true);
                flashClock = .32f; MgfSfx.Play("correct", .42f);
                int n = 0; for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0) n++;
                for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0) MgfFx.Burst(bats[i].root.position + cam.transform.up * (WingCy * WorldPerDesign()), Apricot, 8 + n * 4, 2.6f, .16f);
            }
            else
            {
                ShowToast(BakjwiRules.WrongReason(p, mask), RevealWrongSec, true);
                flashClock = -.24f; MgfSfx.Play("wrong", .34f);
                if (!practice) { featherClock = .9f; MgfSfx.Play("pop", .2f); }
                MgfFx.Shake(cam, .10f, .22f);
            }
        }

        void UpdateRevealVisual(float t, bool correct)
        {
            float wpd = WorldPerDesign();
            if (correct)
            {
                // 붓이 지난 순서대로 눈을 뜨고 80ms 간격으로 뒤집혀 나선 무리 비행
                for (int i = 0; i < 4; i++)
                {
                    Bat b = bats[i];
                    if (!b.awake) continue;
                    int rank = SelectionRank(i);
                    float wake = Mathf.Clamp01((t - rank * .08f) / .18f);
                    SetEyes(b, wake);
                    float k = Mathf.Clamp01((t - .22f - rank * .08f) / 1.05f);
                    if (k <= 0f) { b.root.localScale = Vector3.one * wpd * b.mul * (1f + Mathf.Sin(wake * Mathf.PI) * .08f); continue; }
                    float flip = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / .3f));
                    float rise = k * k;
                    float spin = k * 4.2f + rank * 2.1f;
                    Vector3 up = cam.transform.up, right = cam.transform.right;
                    Vector3 center = Vector3.Lerp(b.flyFrom, DesignToWorld(new Vector2(design.x * .5f, design.y + 120f), 20f), rise);
                    float radius = (60f + rank * 16f) * wpd * Mathf.Sin(k * Mathf.PI);
                    b.root.position = center + right * Mathf.Cos(spin) * radius + up * Mathf.Sin(spin) * radius * .4f;
                    b.root.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, 180f * flip + Mathf.Sin(spin) * 12f * flip);
                    float squash = 1f + Mathf.Sin(Mathf.Clamp01(k / .3f) * Mathf.PI) * .18f;
                    b.root.localScale = new Vector3(wpd * b.mul / squash, wpd * b.mul * squash, wpd * b.mul) * (1f - k * .35f);
                    b.wingPivot.localScale = new Vector3(1f + Mathf.Sin(t * 28f + rank) * .35f * flip, 1f, 1f);
                    if (rank > flapSoundRank && flip > .2f) { flapSoundRank = rank; MgfSfx.Play("whoosh", .2f + rank * .05f); }
                }
            }
            else
            {
                float squeeze = Mathf.Sin(Mathf.Clamp01(t / .45f) * Mathf.PI);
                for (int i = 0; i < 4; i++)
                {
                    Bat b = bats[i];
                    b.root.localScale = new Vector3(wpd * b.mul * (1f - squeeze * .2f), wpd * b.mul * (1f + squeeze * .07f), wpd * b.mul);
                    b.wingPivot.localScale = new Vector3(1f - squeeze * .12f, 1f, 1f);
                    SetEyes(b, squeeze > .3f ? .9f : 0f);
                }
            }
        }

        void CompleteRevealVisual()
        {
            flashImg.color = Color.clear; fallingFeather.color = Color.clear;
            for (int i = 0; i < 4; i++)
            {
                Bat b = bats[i]; b.awake = false; b.wingPivot.localScale = Vector3.one;
                b.verdictPill.gameObject.SetActive(false); b.evidencePill.gameObject.SetActive(false); b.tri.ui.gameObject.SetActive(true);
            }
            PlaceBatsHome();
        }

        void BeginInterludeVisual(SimilarityBand band)
        {
            SetGroup(interludeGroup, true);
            interludeTitle.text = (int)band + "단계 · " + band + " 닮음";
            interludeBody.text = band == SimilarityBand.SAS ? "두 대응변의 길이의 비가 같고\n그 끼인각의 크기가 같으면 닮음"
                : "두 대응각의 크기가 각각 같으면 닮음\n남은 한 각은 180°에서 두 각을 빼서 구한다";
            MgfFx.Punch(interludeTitle.transform.parent, .08f, .3f); MgfSfx.Play("whoosh", .3f);
            for (int i = 0; i < 4; i++) bats[i].tri.ui.gameObject.SetActive(false);
        }

        void EndInterludeVisual()
        {
            SetGroup(interludeGroup, false);
            for (int i = 0; i < 4; i++) { bats[i].tri.ui.gameObject.SetActive(true); bats[i].enterT = 0f; bats[i].enterDelay = i * .07f; }
        }

        // ───────────── 안내 ─────────────
        const string PracticeLine = "빛나는 1번에서 2번까지 한 번에 쓸고 손을 떼시오";

        void ReplayPracticeGuide(bool stronger, bool setToast = true)
        {
            if (current == null || gamePhase != GamePhase.Practice) return;
            guideFrom = CandidateScreen(0); guideTo = CandidateScreen(1); guideActive = true; guideIsPractice = true; guideClock = 0f;
            guideLine.enabled = true; guideBrush.gameObject.SetActive(true);
            guideBoost = stronger ? 1.6f : 0f;
            if (setToast) ShowToast(stronger ? PracticeLine : "유령 붓을 따라 두 날개를 이어 쓸어 보시오", 999f, false);
            if (stronger) { MgfFx.Punch(guideRingA.rectTransform, .35f, .45f); MgfFx.Punch(guideRingB.rectTransform, .25f, .45f); }
        }

        // 탭 반응: 가장 가까운 박쥐가 실눈을 뜨고 날개를 움찔한다(정답 여부와 무관)
        void PeekNearestBat(Vector2 screen)
        {
            int nearest = -1; float best = float.MaxValue;
            for (int i = 0; i < 4; i++) { float d = (CandidateScreen(i) - screen).sqrMagnitude; if (d < best) { best = d; nearest = i; } }
            if (nearest < 0) return;
            bats[nearest].peek = 1.1f; MgfFx.Punch(bats[nearest].wingPivot, .12f, .3f);
        }

        void PointAtNearestCandidate(Vector2 screen)
        {
            int nearest = 0; float best = float.MaxValue;
            for (int i = 0; i < 4; i++) { float d = (CandidateScreen(i) - screen).sqrMagnitude; if (d < best) { best = d; nearest = i; } }
            guideFrom = screen; guideTo = CandidateScreen(nearest); guideActive = true; guideIsPractice = false; guideClock = 0f;
            guideLine.enabled = true; guideBrush.gameObject.SetActive(true);
        }

        void StopGuide()
        {
            guideActive = false; guideLine.enabled = false; guideBrush.gameObject.SetActive(false);
            guideRingA.color = Color.clear; guideRingB.color = Color.clear;
        }

        void RefuseDuringReveal()
        {
            for (int i = 0; i < 4; i++) if (bats[i].root.gameObject.activeSelf && !bats[i].awake) MgfFx.Punch(bats[i].wingPivot, -.04f, .16f);
            if (toastClock < .4f) ShowToast("날개가 펴진 뒤 쓸어 주시오", .9f, false);
            MgfSfx.Play("tap", .1f);
        }

        void NudgeEnd() { nudgeClock = .3f; }

        void ShowToast(string message, float seconds, bool important)
        {
            toastTxt.text = message; toastClock = seconds; SetGroup(toastGroup, true);
            toastBg.color = important ? A(Ivory, .97f) : A(Ivory, .9f);
            float h = message.IndexOf('\n') >= 0 || message.Length > 26 ? 58f : 44f;
            toastBg.rectTransform.sizeDelta = new Vector2(land ? 520f : 362f, h);
            toastTxt.rectTransform.sizeDelta = new Vector2(land ? 500f : 346f, h);
            MgfFx.Punch(toastBg.rectTransform, .05f, .2f);
        }

        void SpawnPointerRipple(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, screen, null, out Vector2 local);
            rippleImg.rectTransform.anchorMin = rippleImg.rectTransform.anchorMax = new Vector2(.5f, .5f);
            rippleImg.rectTransform.anchoredPosition = local; rippleClock = 1f;
        }

        // ───────────── 타이틀·결과 ─────────────
        void ShowTitleVisual()
        {
            int best = 0;
            try { best = PlayerPrefs.GetInt("bakjwi-kkaewo.best", 0); } catch (System.Exception) { }
            bestTxt.text = best > 0 ? "최고 기록 " + best.ToString("N0") + "점" : "";
            titleGroup.alpha = 1f; displayScore = 0; displayEndScore = 0;
            titleLayoutActive = true;
            SetupDecoBats();
        }

        void SetupDecoBats()
        {
            // 타이틀·결과 화면: 네 박쥐가 아치 양쪽에서 거꾸로 매달려 존다(날개는 수치 없는 기본 삼각형)
            LayoutLedgesForPhase();
            for (int i = 0; i < 4; i++)
            {
                Bat b = bats[i];
                LayoutTri(b.tri, SimilarityBand.SSS, TitleWingDemo, i * 70 + 10, false, WingBoxW, WingBoxH, new Vector2(0, WingCy), .95f);
                BuildArcs(b.tri, false, false, false);
                b.ui.gameObject.SetActive(false); b.halo.gameObject.SetActive(false); SetEyes(b, 0f); b.awake = false;
                b.root.gameObject.SetActive(true); b.enterT = 1f;
            }
            LayoutTri(titleBat.tri, SimilarityBand.SSS, new[] { 4, 5, 6 }, 180, false, 84f, 64f, new Vector2(0, WingCy + 4f), 1f);
            BuildArcs(titleBat.tri, false, false, false);
            SetEyes(titleBat, 0f);
            titleBat.root.gameObject.SetActive(true);
            PlaceBatsHome();
        }

        void StartSplitReveal() { transitionClock = 0f; titleBat.flyFrom = titleBat.root.position; }

        void PressTitleBat()
        {
            SetEyes(titleBat, 1f); MgfFx.Punch(titleBat.wingPivot, .14f, .2f); MgfSfx.Play("pop", .3f);
            ctaTxt.text = "깨어났다!";
        }

        void ReleaseTitleBat() { MgfFx.Burst(titleBat.root.position + cam.transform.up * (-135f * WorldPerDesign() * TitleBatMul()), Apricot, 18, 3f, .2f); }

        void ShowEnd(string reason)
        {
            string title = reason == "clear" ? "동굴 비행 완성" : reason == "time" ? "밤이 끝났다" : reason == "feathers" ? "소란 깃을 모두 잃었다"
                : reason == "mastery_impossible" ? "숙련 목표를 다시 노려라" : "완주 · 닮음 판단 재도전";
            endTitle.text = title; endShadow.text = title;
            float size = title.Length > 9 ? 31f : 40f; endTitle.fontSize = size; endShadow.fontSize = size;
            endCta.text = "아무 곳이나 눌러 다시 깨우기";
            displayEndScore = 0f;
            for (int i = 0; i < 4; i++) { bats[i].tri.ui.gameObject.SetActive(false); bats[i].verdictPill.gameObject.SetActive(false); bats[i].evidencePill.gameObject.SetActive(false); }
            StopGuide(); SetGroup(toastGroup, false); SetGroup(interludeGroup, false);
            titleLayoutActive = true;
            SetupDecoBats();
            SetEyes(titleBat, reason == "clear" ? 1f : 0f);
            if (reason == "clear") for (int i = 0; i < 4; i++) SetEyes(bats[i], 1f);
        }

        string EndStatsText(float score)
        {
            return "깨운 장면 " + st.solved + "/10\n첫 시도 " + st.firstAttemptCorrect + "/10 · AA " + st.aaFirstCorrect + "/4\n점수 " + Mathf.RoundToInt(score).ToString("N0")
                + (st.endReason == "clear" ? "" : "\n<size=13>숙련: 첫 시도 8/10 이상 · AA 3/4 이상</size>");
        }

        void ResetVisualRun()
        {
            ResetStrokeVisual(); StopGuide();
            flashImg.color = Color.clear; rippleImg.color = Color.clear; fallingFeather.color = Color.clear;
            SetGroup(toastGroup, false); SetGroup(interludeGroup, false);
            lastSolved = -1; lastScore = -1; lastSecond = -1; displayScore = 0; scoreTxt.text = "0";
            for (int i = 0; i < 4; i++) { bats[i].verdictPill.gameObject.SetActive(false); bats[i].evidencePill.gameObject.SetActive(false); }
        }

        void SetScreen()
        {
            bool title = gamePhase == GamePhase.Title || gamePhase == GamePhase.Transition;
            bool end = gamePhase == GamePhase.End;
            bool play = !title && !end;
            SetGroup(titleGroup, title); SetGroup(playGroup, play); SetGroup(endGroup, end);
            if (!play) { SetGroup(toastGroup, false); SetGroup(interludeGroup, false); }
            titleBat.root.gameObject.SetActive(title || end);
            bool tl = title || end;
            if (tl != titleLayoutActive || !play) { LayoutLedgesForPhase(); PlaceBatsHome(); }
            for (int i = 0; i < 4; i++) bats[i].ui.gameObject.SetActive(play);
        }

        // ───────────── 매 프레임 ─────────────
        void UpdateUi(float dt)
        {
            if (gamePhase == GamePhase.Transition)
            {
                float k = Mathf.Clamp01(transitionClock / TransitionSec), e = k * k * (3f - 2f * k);
                // Split Reveal: 아치가 가운데서 갈라지고 깨어난 박쥐가 뒤집혀 날아오른다
                float wpd = WorldPerDesign();
                float cw = land ? 440f : 356f;
                Vector2 crown = land ? new Vector2(440, 398) : new Vector2(195, 712);
                crownL.position = DesignToWorld(crown - new Vector2(cw * .25f + e * cw * .6f, 0), 30f);
                crownR.position = DesignToWorld(crown + new Vector2(cw * .25f + e * cw * .6f, 0), 30f);
                titleGroup.alpha = 1f - e;
                titleBat.root.position = Vector3.Lerp(titleBat.flyFrom, DesignToWorld(new Vector2(design.x * .5f, design.y + 160f), 22f), e * e);
                titleBat.root.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, 180f * Mathf.Clamp01(k * 2f));
                titleBat.wingPivot.localScale = new Vector3(1f + Mathf.Sin(k * 40f) * .3f, 1f, 1f);
                _ = wpd;
            }
            if (gamePhase == GamePhase.Title)
            {
                float pulse = .5f + .5f * Mathf.Sin(worldClock * 3.2f);
                ctaRing.color = A(Apricot, .25f + pulse * .55f); ctaRing.rectTransform.localScale = Vector3.one * (1f + pulse * .18f);
                ctaTxt.alpha = .75f + pulse * .25f;
                logoTxt.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 1.3f) * .012f);
            }
            bool playing = gamePhase == GamePhase.Practice || gamePhase == GamePhase.Playing || gamePhase == GamePhase.Reveal || gamePhase == GamePhase.Interlude;
            if (playing)
            {
                int seconds = Mathf.CeilToInt(timeLeft);
                if (seconds != lastSecond)
                {
                    lastSecond = seconds; timeTxt.text = (seconds / 60) + ":" + (seconds % 60).ToString("00");
                    timeTxt.color = seconds <= 15 && gamePhase != GamePhase.Practice ? Tangerine : Ivory;
                    if (seconds <= 10 && gamePhase == GamePhase.Playing) MgfFx.Punch(timeTxt.rectTransform, .08f, .18f);
                }
                if (st.score != lastScore) { lastScore = st.score; MgfFx.Punch(scoreTxt.rectTransform, .16f, .26f); }
                if (Mathf.Abs(displayScore - st.score) > .5f)
                {
                    displayScore = Mathf.MoveTowards(displayScore, st.score, dt * Mathf.Max(260f, Mathf.Abs(st.score - displayScore) * 6f));
                    scoreTxt.text = Mathf.RoundToInt(displayScore).ToString("N0");
                }
                if (st.solved != lastSolved)
                {
                    lastSolved = st.solved;
                    firstTxt.text = "첫 시도 " + st.firstAttemptCorrect + "/10 · AA " + st.aaFirstCorrect + "/4";
                    for (int i = 0; i < 10; i++)
                    {
                        bool done = i < st.solved;
                        dots[i].color = done ? Apricot : (i == st.solved ? A(Ivory, .7f) : A(Ivory, .22f));
                        dots[i].rectTransform.sizeDelta = i == st.solved ? new Vector2(16, 16) : new Vector2(12, 12);
                    }
                    if (st.solved > 0) MgfFx.Punch(dots[st.solved - 1].rectTransform, .6f, .3f);
                }
                for (int i = 0; i < 3; i++) feathers[i].color = i < st.lives ? Ivory : A(Deep, .45f);
            }
            if (gamePhase == GamePhase.End)
            {
                displayEndScore = Mathf.MoveTowards(displayEndScore, st.score, dt * Mathf.Max(300f, st.score * 1.4f));
                endStats.text = EndStatsText(displayEndScore);
                float ready = Mathf.Clamp01((endClock - .9f) / .3f);
                endCta.alpha = ready * (.7f + .3f * Mathf.Sin(worldClock * 3f));
                if (nudgeClock > 0f) nudgeClock -= dt;
            }
            if (toastClock > 0f && toastClock < 900f)
            {
                toastClock -= dt;
                if (toastClock <= 0f)
                {
                    if (gamePhase == GamePhase.Practice) ShowToast(PracticeLine, 999f, false);   // 첫 성공 전까지 목표 문장 유지
                    else SetGroup(toastGroup, false);
                }
            }
            if (rippleClock > 0f)
            {
                rippleClock -= dt; float k = 1f - Mathf.Clamp01(rippleClock / 1f);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);
                rippleImg.rectTransform.localScale = Vector3.one * Mathf.Lerp(.4f, 1.9f, e);
                rippleImg.color = A(Apricot, (1f - k) * .85f);
            }
            if (flashClock != 0f)
            {
                float sign = Mathf.Sign(flashClock); flashClock = Mathf.MoveTowards(flashClock, 0f, dt);
                Color c = sign > 0 ? Ivory : Tangerine; flashImg.color = A(c, Mathf.Abs(flashClock) * .5f);
            }
            if (featherClock > 0f)
            {
                featherClock -= dt; float k = 1f - Mathf.Clamp01(featherClock / .9f);
                RectTransform lost = feathers[Mathf.Clamp(st.lives, 0, 2)].rectTransform;
                Vector2 from = RectTransformUtility.WorldToScreenPoint(null, lost.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, from, null, out Vector2 local);
                fallingFeather.rectTransform.anchorMin = fallingFeather.rectTransform.anchorMax = new Vector2(.5f, .5f);
                fallingFeather.rectTransform.anchoredPosition = local + new Vector2(Mathf.Sin(k * 9f) * 22f, -k * 110f);
                fallingFeather.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * 7f) * 40f);
                fallingFeather.color = A(Tangerine, 1f - k * k);
            }
            if (guideActive)
            {
                guideClock += dt;
                float pulse = .5f + .5f * Mathf.Sin(guideClock * 6f);
                SetRingAt(guideRingA, guideFrom, A(Apricot, .5f + pulse * .5f), 1f + pulse * .2f);
                SetRingAt(guideRingB, guideTo, A(Ivory, .35f + (1f - pulse) * .5f), 1.15f - pulse * .15f);
                if (!guideIsPractice && guideClock > 1.4f) StopGuide();
            }
        }

        void SetRingAt(Image ring, Vector2 screen, Color c, float scale)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(playRoot, screen, null, out Vector2 local);
            ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = Vector2.zero;
            ring.rectTransform.anchoredPosition = local + playRoot.rect.size * playRoot.pivot;
            ring.color = c; ring.rectTransform.localScale = Vector3.one * scale;
        }

        void AnimateWorld(float dt, float raw)
        {
            worldClock += raw;
            float wpd = WorldPerDesign();
            for (int i = 0; i < crystals.Count; i++)
            {
                float s = 1f + Mathf.Sin(worldClock * 1.6f + i * 1.3f) * .12f;
                crystals[i].localScale = new Vector3(6f, (11f + (i % 3) * 4f) * s, 6f) * wpd;
            }
            float screenW = Screen.width, screenH = Screen.height;
            for (int i = 0; i < dust.Length; i++)
            {
                Vector3 sd = dustSeed[i];
                float y = Mathf.Repeat(sd.y + worldClock * .018f * sd.z, 1.1f) - .05f;
                float x = sd.x + Mathf.Sin(worldClock * .5f + i) * .015f;
                dust[i].position = cam.ScreenToWorldPoint(new Vector3(x * screenW, y * screenH, 18f + (i % 4) * 4f));
                dust[i].rotation = cam.transform.rotation;
            }
            // 대기: 귀와 발가락만 움직인다(수학 면은 정지)
            for (int i = 0; i < 4; i++)
            {
                Bat b = bats[i];
                if (!b.root.gameObject.activeSelf) continue;
                float tw = Mathf.Sin(worldClock * 2.1f + i * 1.7f);
                float twitch = Mathf.Max(0f, Mathf.Sin(worldClock * .9f + i * 2.3f) - .85f) * 60f;
                if (b.peek > 0f && gamePhase != GamePhase.Reveal) { b.peek -= raw; SetEyes(b, b.peek > 0f ? .45f : 0f); twitch += b.peek * 30f; }
                b.earL.localRotation = Quaternion.Euler(0, 0, -38f + tw * 4f + twitch);
                b.earR.localRotation = Quaternion.Euler(0, 0, 38f - tw * 4f - twitch);
                if (b.enterT < 1f && gamePhase != GamePhase.Reveal)
                {
                    if (b.enterDelay > 0f) { b.enterDelay -= raw; b.root.localScale = new Vector3(wpd * b.mul, .001f, wpd * b.mul); continue; }
                    b.enterT = Mathf.Min(1f, b.enterT + raw / .42f);
                    float e = b.enterT, over = 1f + Mathf.Sin(e * Mathf.PI) * .16f * (1f - e);
                    float sy = Mathf.Lerp(.05f, 1f, 1f - (1f - e) * (1f - e)) * over;
                    b.root.localScale = new Vector3(wpd * b.mul * (1f + (1f - sy) * .15f), wpd * b.mul * sy, wpd * b.mul);
                }
            }
            if (titleBat.root.gameObject.activeSelf && gamePhase != GamePhase.Transition)
            {
                float tw = Mathf.Sin(worldClock * 1.8f);
                titleBat.earL.localRotation = Quaternion.Euler(0, 0, -38f + tw * 5f);
                titleBat.earR.localRotation = Quaternion.Euler(0, 0, 38f - tw * 5f);
                titleBat.root.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, Mathf.Sin(worldClock * .9f) * 2.2f);
                if (gamePhase == GamePhase.End && st.endReason == "clear") titleBat.wingPivot.localScale = new Vector3(1f + Mathf.Sin(worldClock * 9f) * .12f, 1f, 1f);
            }
            if (guideActive)
            {
                Vector2 a = guideFrom, b = guideTo;
                float lift = 26f * G * S;
                for (int i = 0; i < guidePts.Length; i++)
                {
                    float t = i / (float)(guidePts.Length - 1);
                    Vector2 q = Vector2.Lerp(a, b, t); q.y += Mathf.Sin(t * Mathf.PI) * lift;
                    guidePts[i] = cam.ScreenToWorldPoint(new Vector3(q.x, q.y, 11.5f));
                }
                guideLine.positionCount = guidePts.Length; guideLine.SetPositions(guidePts);
                float w = 8f * wpd; guideLine.startWidth = guideLine.endWidth = w;
                guideMat.mainTextureScale = new Vector2(Vector2.Distance(a, b) / (22f * G * S), 1f);
                guideMat.mainTextureOffset = new Vector2(-worldClock * 1.6f, 0f);
                float u = Mathf.Repeat(guideClock / 1.2f, 1f);
                Vector2 brush = Vector2.Lerp(a, b, u); brush.y += Mathf.Sin(u * Mathf.PI) * lift;
                guideBrush.position = cam.ScreenToWorldPoint(new Vector3(brush.x, brush.y, 11f));
                guideBrush.rotation = cam.transform.rotation;
                guideBrush.localScale = Vector3.one * (46f + guideBoost * 14f + Mathf.Sin(worldClock * 8f) * 6f) * wpd;
                if (guideBoost > 0f) guideBoost = Mathf.Max(0f, guideBoost - raw);
            }
        }
    }
}
