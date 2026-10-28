// 한 끗 골프 — 파란 인조잔디 벙커 골프 군도(2.5D 직교 쿼터뷰 ↔ 입력 중 평면도), 두더지 캐디, 레일·공 주행, HUD.
// 배경 일러스트 없음: 기복 있는 파란 펠트 섬·흰 원형 벙커·깃발·덤불을 코드 메시로 지어 화면 끝까지 채운다.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.HanKkeutGolf
{
    public partial class HanKkeutGolfGame
    {
        // ───────── 팔레트 ─────────
        static Color H(string h) => MgfLook.Hex(h);
        static readonly Color Felt = H("#4F84C4"), FeltDeep = H("#3D6DAA"), FeltLight = H("#6B9DD8"), FeltStitch = H("#E8EEF8");
        static readonly Color Cream = H("#FFF8E9"), Sand = H("#F5E8C9"), SandDark = H("#D8C190"), Rail = H("#EAC94F"), RailDark = H("#C9A834");
        static readonly Color Ink = H("#273347"), Coral = H("#F19C8C"), Fur = H("#EADBC3"), FurDark = H("#C9B396"), Nose = H("#D9877E");
        static readonly Color Bush = H("#5C9A5B"), BushLight = H("#79B46A"), Rock = H("#8E99A8"), Pink = H("#F3B3A6");

        Camera cam;
        Transform world;
        Material lineMat;

        // ───────── 코스 ─────────
        sealed class Course
        {
            public Vector3 o;          // 원의 중심 O (섬 위 y=0)
            public float R;            // 벙커 반지름(세계 단위)
            public Transform flag;     // 깃대 루트
            public Transform flower;   // 첫 제출 성공 모래꽃
            public Transform tag;      // 수리 성공 흰 표지
            public Transform rail;     // 완성 레일(지도에 남는다)
            public LineRenderer rim;   // 수학 원(먹색 정확한 원)
            public Transform sand;
        }
        const int CourseCount = 9; // 0 = 연습
        readonly Course[] courses = new Course[CourseCount];
        Vector3 titleSpot;
        float curR, curL;
        Vector3 curO, curA, curB;

        static Vector3 CourseCenter(int i)
        {
            // 연습 벙커에서 오른쪽으로 이어지는 군도. 짝수/홀수 섬을 위아래로 엇갈린다.
            return new Vector3(i * 15.5f, 0f, (i % 2 == 0 ? 0f : 3.2f) + (i % 3 == 0 ? -.6f : 0f));
        }

        static float WorldR(int r) => 1.55f + .1f * (r - 4);

        // ───────── 활성 코스 오브젝트 ─────────
        Transform railRoot, railBar, gripNear, gripFar, cupFar, pivot, ball, crater, sandFlower, carryFlower;
        MeshFilter railBarMf;
        MeshRenderer railBarR;
        Material railMat, railLockedMat;
        LineRenderer oaLine, obLine, baseLine, aParallel, thetaArc, rightMark, ghostRail, wrongTrace, drawLine, rimHalo;
        TextMeshPro labO, labA, labB, labTheta, labBase, labRadius;
        Transform tickRing;
        readonly List<Transform> keptRails = new List<Transform>();
        readonly LineRenderer[] strings = new LineRenderer[CourseCount];
        readonly float[] stringSag = new float[CourseCount];
        readonly float[] stringSagTarget = new float[CourseCount];
        readonly bool[] stringTaut = new bool[CourseCount];

        // 두더지 캐디
        Transform mole, moleBody, moleHead, molePawL, molePawR, moleRake;
        Vector3 moleHome;
        float moleDuck, moleDuckTarget;
        readonly List<Transform> idleMoles = new List<Transform>();
        readonly List<Vector3> idleMoleHome = new List<Vector3>();

        // 타이틀
        Transform titleFlag, titleCloth, titleBall, titleRing, titleRollBall;
        Vector3 titleRailA, titleRailDir;
        Mesh clothMesh;
        Vector3[] clothBase, clothVerts;

        // 카메라
        Vector3 camPos, camPosT;
        Quaternion camRot = Quaternion.identity, camRotT = Quaternion.identity;
        float camSize = 6f, camSizeT = 6f;
        enum View { Title, Plan, Roll, Overview }
        View view;

        // 주행 시퀀스
        bool rollOk, rollCounted;
        int rollAngle;
        string rollMis = "";
        float rollT, rollDur, tEntry;
        float[] k = new float[8];
        Vector3 rollS, rollD;
        bool rollBallGone;

        // ───────── 세계 ─────────
        void BuildWorld()
        {
            lineMat = MgfLook.Alpha(Color.white);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = H("#DDE8FA") * .78f;
            RenderSettings.ambientEquatorColor = H("#A9C0E2") * .62f;
            RenderSettings.ambientGroundColor = H("#5D6F8F") * .55f;
            MgfLook.Sun(new Vector3(58f, -32f, 0f), H("#FFF3DC"), 1.05f, .5f);
            cam = MgfLook.Camera(new Vector3(0, 20, -20), Vector3.zero, 30f);
            cam.orthographic = true; cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = FeltDeep;
            cam.nearClipPlane = .1f; cam.farClipPlane = 120f;

            world = new GameObject("BlueFeltBunkerArchipelago").transform;
            var decor = new Decor();
            var meshSphere = PrimMesh(PrimitiveType.Sphere);
            var meshCyl = PrimMesh(PrimitiveType.Cylinder);
            var meshCube = PrimMesh(PrimitiveType.Cube);

            // 아래 펠트 바닥(화면 끝까지)
            decor.Add(FeltDeep, meshCube, Matrix4x4.TRS(new Vector3(60, -1.05f, 2), Quaternion.identity, new Vector3(260, 1f, 160)));
            var rng = new System.Random(42);
            // 낮은 펠트 언덕들(섬 사이 기복)
            for (int i = 0; i < 70; i++)
            {
                float x = -40 + (float)rng.NextDouble() * 210, z = -34 + (float)rng.NextDouble() * 72;
                if (Mathf.Abs(z - 1.5f) < 9f && x > -24 && x < 132) continue; // 코스 줄은 비운다
                float s = 3 + (float)rng.NextDouble() * 6;
                decor.Add(i % 3 == 0 ? FeltLight : Felt, meshSphere, Matrix4x4.TRS(new Vector3(x, -.75f, z), Quaternion.Euler(0, (float)rng.NextDouble() * 180, 0), new Vector3(s * 1.4f, s * .32f, s)));
                if (i % 2 == 0) AddBush(decor, meshSphere, new Vector3(x + s * .3f, -.75f + s * .14f, z - s * .2f), .5f + (float)rng.NextDouble() * .5f, rng);
            }
            // 타이틀 뒤쪽 언덕·덤불·깃발(세로 화면 위쪽을 채운다)
            for (int i = 0; i < 14; i++)
            {
                float x = -28 + i * 2.3f + (float)rng.NextDouble(), z = 9.5f + (float)rng.NextDouble() * 9f;
                float s = 2.4f + (float)rng.NextDouble() * 2.6f;
                decor.Add(i % 2 == 0 ? FeltLight : Felt, meshSphere, Matrix4x4.TRS(new Vector3(x, -.55f, z), Quaternion.identity, new Vector3(s * 1.3f, s * .4f, s)));
                AddBush(decor, meshSphere, new Vector3(x + .4f, -.55f + s * .19f, z - s * .25f), .55f + (float)rng.NextDouble() * .4f, rng);
            }
            // 타이틀 섬 + 연습 섬 + 8코스 섬
            titleSpot = new Vector3(-15f, 0, .8f);
            AddIsland(decor, meshCube, meshSphere, titleSpot, new Vector2(12, 10), rng);
            for (int i = 0; i < CourseCount; i++)
            {
                var c = new Course { o = CourseCenter(i), R = 1.9f };
                courses[i] = c;
                AddIsland(decor, meshCube, meshSphere, c.o, new Vector2(13.2f, 11f), rng);
            }
            decor.Bake(world);

            // 벙커(반지름은 코스마다 바뀌므로 개별 오브젝트)
            for (int i = 0; i < CourseCount; i++) BuildBunker(courses[i], i);
            BuildActiveCourse();
            BuildMole();
            BuildTitleProps();
            for (int i = 1; i < CourseCount; i++)
            {
                strings[i] = Line("CourseString" + i, Cream, .07f, world, 14);
                strings[i].gameObject.SetActive(false);
            }
            camPos = camPosT = new Vector3(0, 20, -20);
        }

        static Mesh PrimMesh(PrimitiveType t)
        {
            var g = GameObject.CreatePrimitive(t);
            var m = g.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(g);
            return m;
        }

        /// <summary>정적 장식은 재질별로 메시 하나에 굽는다(드로콜 절약).</summary>
        sealed class Decor
        {
            readonly Dictionary<Color, List<CombineInstance>> byColor = new Dictionary<Color, List<CombineInstance>>();
            public void Add(Color c, Mesh m, Matrix4x4 mtx)
            {
                if (!byColor.TryGetValue(c, out var l)) byColor[c] = l = new List<CombineInstance>();
                l.Add(new CombineInstance { mesh = m, transform = mtx });
            }
            public void Bake(Transform parent)
            {
                foreach (var kv in byColor)
                {
                    var mesh = new Mesh { name = "Decor", indexFormat = IndexFormat.UInt32 };
                    mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                    mesh.RecalculateBounds();
                    var go = new GameObject("Decor");
                    go.transform.SetParent(parent, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = go.AddComponent<MeshRenderer>();
                    bool flatShine = kv.Key == Cream || kv.Key == FeltStitch;
                    r.sharedMaterial = MgfLook.Lit(kv.Key, flatShine ? .2f : .08f);
                    r.shadowCastingMode = ShadowCastingMode.On;
                    r.receiveShadows = true;
                }
            }
        }

        void AddBush(Decor d, Mesh sphere, Vector3 p, float s, System.Random rng)
        {
            var col = rng.NextDouble() < .28 ? Coral : (rng.NextDouble() < .5 ? Bush : BushLight);
            d.Add(col, sphere, Matrix4x4.TRS(p, Quaternion.identity, Vector3.one * s));
            d.Add(col, sphere, Matrix4x4.TRS(p + new Vector3(s * .45f, -s * .12f, s * .1f), Quaternion.identity, Vector3.one * s * .72f));
            d.Add(col == Coral ? Pink : Bush, sphere, Matrix4x4.TRS(p + new Vector3(-s * .4f, -s * .15f, -s * .05f), Quaternion.identity, Vector3.one * s * .62f));
        }

        void AddIsland(Decor d, Mesh cube, Mesh sphere, Vector3 c, Vector2 size, System.Random rng)
        {
            // 펠트 섬: 둥근 상자 + 가장자리 쿠션, 위에 흰 바느질 점선
            var box = MgfLook.RoundedBox(new Vector3(size.x, 1.0f, size.y), .48f, 4);
            d.Add(Felt, box, Matrix4x4.TRS(c + new Vector3(0, -.5f, 0), Quaternion.identity, Vector3.one));
            float hx = size.x * .5f - .42f, hz = size.y * .5f - .42f;
            float per = 2 * (hx + hz) * 2;
            int n = Mathf.RoundToInt(per / .42f);
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n * per, x, z, rot;
                if (u < 2 * hx) { x = -hx + u; z = hz; rot = 0; }
                else if (u < 2 * hx + 2 * hz) { x = hx; z = hz - (u - 2 * hx); rot = 90; }
                else if (u < 4 * hx + 2 * hz) { x = hx - (u - 2 * hx - 2 * hz); z = -hz; rot = 0; }
                else { x = -hx; z = -hz + (u - 4 * hx - 2 * hz); rot = 90; }
                d.Add(FeltStitch, cube, Matrix4x4.TRS(c + new Vector3(x, .005f, z), Quaternion.Euler(0, rot, 0), new Vector3(.2f, .02f, .05f)));
            }
            // 모서리 덤불·바위·데이지
            for (int i = 0; i < 4; i++)
            {
                float sx = (i % 2 == 0 ? -1 : 1) * (size.x * .5f - 1.0f), sz = (i < 2 ? -1 : 1) * (size.y * .5f - .9f);
                var p = c + new Vector3(sx, .28f, sz);
                AddBush(d, sphere, p, .55f + (float)rng.NextDouble() * .25f, rng);
                if (i % 2 == 1) d.Add(Rock, sphere, Matrix4x4.TRS(p + new Vector3(.9f * -Mathf.Sign(sx), -.18f, 0), Quaternion.Euler(0, 30 * i, 0), new Vector3(.5f, .32f, .42f)));
                for (int j = 0; j < 3; j++)
                {
                    var q = p + new Vector3(-Mathf.Sign(sx) * (.6f + j * .32f), -.26f, -Mathf.Sign(sz) * (.5f + j * .18f));
                    d.Add(Cream, sphere, Matrix4x4.TRS(q, Quaternion.identity, new Vector3(.16f, .05f, .16f)));
                    d.Add(Rail, sphere, Matrix4x4.TRS(q + Vector3.up * .02f, Quaternion.identity, new Vector3(.06f, .04f, .06f)));
                }
            }
        }

        void BuildBunker(Course c, int i)
        {
            var root = new GameObject("Bunker" + i).transform; root.SetParent(world, false); root.localPosition = c.o;
            var sand = MgfLook.Prim(PrimitiveType.Cylinder, "Sand", new Vector3(0, .004f, 0), new Vector3(2 * c.R, .01f, 2 * c.R), MgfLook.Lit(Sand, .05f), root, false);
            sand.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            c.sand = sand.transform;
            // 가리비 모양 흰 테(원 바깥에 둘러서 수학 원을 가리지 않는다)
            var scallop = new GameObject("Scallops").transform; scallop.SetParent(root, false);
            for (int s = 0; s < 22; s++)
            {
                float a = s / 22f * Mathf.PI * 2f;
                var g = MgfLook.Prim(PrimitiveType.Sphere, "Sc", new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.04f, new Vector3(.16f, .045f, .16f), MgfLook.Lit(Cream, .2f), scallop, false);
                g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            scallop.localScale = Vector3.one * c.R;
            c.rim = Line("Rim" + i, Ink, .055f, root, 73);
            SetCircle(c.rim, c.o + Vector3.up * .03f, c.R);
            c.flag = BuildPennant(root, i);
            c.flag.localPosition = new Vector3(c.R + 1.3f, 0, c.R * .5f);
            c.flag.gameObject.SetActive(i == 0);
        }

        void ResizeBunker(Course c, int i)
        {
            c.sand.localScale = new Vector3(2 * c.R, .01f, 2 * c.R);
            c.sand.parent.Find("Scallops").localScale = Vector3.one * c.R;
            SetCircle(c.rim, c.o + Vector3.up * .03f, c.R);
        }

        Transform BuildPennant(Transform parent, int i)
        {
            var f = new GameObject("Pennant" + i).transform; f.SetParent(parent, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "Pole", new Vector3(0, .8f, 0), new Vector3(.06f, .8f, .06f), MgfLook.Lit(Cream, .5f), f, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Knob", new Vector3(0, 1.62f, 0), Vector3.one * .12f, MgfLook.Lit(Rail, .6f), f, false);
            var cloth = MgfLook.Block("Cloth", new Vector3(.36f, 1.36f, 0), new Vector3(.64f, .42f, .03f), .012f, MgfLook.Lit(Cream, .15f), f);
            Object.Destroy(cloth.GetComponent<Collider>());
            var num = MgfText.World(i == 0 ? "연습" : i.ToString(), new Vector3(.36f, 1.36f, -.03f), i == 0 ? 2.4f : 3.4f, Ink, f);
            num.fontStyle = FontStyles.Bold;
            var flower = BuildFlower(f, 1.0f); flower.localPosition = new Vector3(-.02f, 1.1f, -.08f); flower.gameObject.SetActive(false);
            var tag = MgfLook.Block("RepairTag", new Vector3(0, .95f, -.05f), new Vector3(.2f, .26f, .03f), .02f, MgfLook.Lit(Color.white, .2f), f).transform;
            Object.Destroy(tag.GetComponent<Collider>()); tag.gameObject.SetActive(false);
            var c = courses[i];
            if (c != null) { c.flower = flower; c.tag = tag; }
            return f;
        }

        Transform BuildFlower(Transform parent, float s)
        {
            var fl = new GameObject("SandFlower").transform; fl.SetParent(parent, false);
            for (int p = 0; p < 6; p++)
            {
                float a = p / 6f * Mathf.PI * 2f;
                MgfLook.Prim(PrimitiveType.Sphere, "Petal", new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .11f * s, new Vector3(.14f, .05f, .14f) * s, MgfLook.Lit(Cream, .3f), fl, false);
            }
            MgfLook.Prim(PrimitiveType.Sphere, "Heart", Vector3.up * .02f * s, new Vector3(.1f, .06f, .1f) * s, MgfLook.Lit(Rail, .4f), fl, false);
            fl.localRotation = Quaternion.Euler(-70, 0, 0);
            return fl;
        }

        void BuildActiveCourse()
        {
            railRoot = new GameObject("RailPivot").transform; railRoot.SetParent(world, false);
            var bar = new GameObject("YellowRail"); bar.transform.SetParent(railRoot, false);
            railBarMf = bar.AddComponent<MeshFilter>();
            var br = bar.AddComponent<MeshRenderer>(); railMat = MgfLook.Lit(Rail, .72f); railLockedMat = MgfLook.Lit(H("#C9CDD6"), .3f); br.sharedMaterial = railMat; br.shadowCastingMode = ShadowCastingMode.On; railBarR = br;
            railBar = bar.transform; railBar.localPosition = new Vector3(0, .17f, 0);
            gripNear = MgfLook.Prim(PrimitiveType.Cylinder, "GripNear", Vector3.zero, new Vector3(.4f, .2f, .4f), MgfLook.Lit(Coral, .25f), railRoot, false).transform;
            gripFar = MgfLook.Prim(PrimitiveType.Cylinder, "CupFar", Vector3.zero, new Vector3(.44f, .16f, .44f), MgfLook.Lit(Coral, .25f), railRoot, false).transform;
            cupFar = MgfLook.Prim(PrimitiveType.Cylinder, "Hole", Vector3.zero, new Vector3(.28f, .02f, .28f), MgfLook.Lit(Ink, .1f), railRoot, false).transform;
            pivot = MgfLook.Prim(PrimitiveType.Cylinder, "PivotPin", Vector3.zero, new Vector3(.16f, .2f, .16f), MgfLook.Lit(Ink, .5f), world, false).transform;
            ball = MgfLook.Prim(PrimitiveType.Sphere, "GolfBall", Vector3.zero, Vector3.one * .3f, MgfLook.Lit(Color.white, .62f), world, false).transform;
            crater = MgfLook.Prim(PrimitiveType.Sphere, "SandDent", Vector3.zero, new Vector3(.62f, .04f, .62f), MgfLook.Lit(H("#B4945F"), .02f), world, false).transform;
            crater.gameObject.SetActive(false);
            sandFlower = BuildFlower(world, 1.3f); sandFlower.gameObject.SetActive(false);
            sandFlower.localRotation = Quaternion.identity;
            carryFlower = sandFlower;

            oaLine = Line("RadiusOA", Ink, .075f, world, 2);
            obLine = Line("AuxOB", Ink, .06f, world, 2);
            drawLine = Line("DrawStroke", Ink, .075f, world, 2);
            baseLine = Line("Baseline", new Color(Ink.r, Ink.g, Ink.b, .55f), .035f, world, 2);
            aParallel = Line("ParallelAtA", new Color(Ink.r, Ink.g, Ink.b, .35f), .03f, world, 2);
            thetaArc = Line("ThetaArc", Ink, .04f, world, 24);
            rightMark = Line("RightAngle", H("#C2463A"), .055f, world, 3);
            ghostRail = Line("GhostRail", new Color(1f, 1f, 1f, .5f), .22f, world, 2);
            wrongTrace = Line("WrongTrace", new Color(.45f, .48f, .54f, .75f), .09f, world, 2);
            rimHalo = Line("RimHalo", new Color(1, 1, 1, .0f), .2f, world, 73);
            labO = Flat("O", 5.2f); labA = Flat("A", 5.2f); labB = Flat("B", 4.8f);
            labTheta = Flat("", 4.2f); labBase = Flat("기준 0°", 2.8f); labRadius = Flat(UiStrings.RadiusName, 3.0f);
            tickRing = BuildTickRing();
            ShowCourseMarks(false);
        }

        TextMeshPro Flat(string s, float size)
        {
            var t = MgfText.World(s, Vector3.zero, size, Ink, world);
            t.fontStyle = FontStyles.Bold;
            t.transform.rotation = Quaternion.Euler(90, 0, 0);
            t.outlineWidth = .22f; t.outlineColor = new Color32(255, 248, 233, 255);
            return t;
        }

        Transform BuildTickRing()
        {
            var verts = new List<Vector3>(); var tris = new List<int>(); var cols = new List<Color>();
            for (int a = 0; a < 360; a += 5)
            {
                bool major = a % 30 == 0, mid = a % 10 == 0;
                if (!mid && !major) continue;
                float len = major ? .13f : .07f, w = major ? .011f : .007f;
                float rad = a * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)); var side = new Vector3(-dir.z, 0, dir.x);
                var p0 = dir * (1f - len * .5f); var p1 = dir * (1f + len * .5f);
                int b = verts.Count;
                verts.Add(p0 - side * w); verts.Add(p0 + side * w); verts.Add(p1 + side * w); verts.Add(p1 - side * w);
                var col = new Color(1, 1, 1, major ? .75f : .45f);
                cols.Add(col); cols.Add(col); cols.Add(col); cols.Add(col);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1); tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
            }
            var m = new Mesh { name = "TickRing" };
            m.SetVertices(verts); m.SetColors(cols); m.SetTriangles(tris, 0); m.RecalculateBounds();
            var go = new GameObject("TickRing"); go.transform.SetParent(world, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = MgfLook.Alpha(Ink); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return go.transform;
        }

        LineRenderer Line(string name, Color c, float w, Transform parent, int n)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; lr.alignment = LineAlignment.TransformZ;
            lr.sharedMaterial = lineMat; lr.startColor = lr.endColor = c; lr.widthMultiplier = w; lr.positionCount = n;
            lr.numCapVertices = 3; lr.numCornerVertices = 2;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
            return lr;
        }

        static void SetCircle(LineRenderer lr, Vector3 c, float r)
        {
            int n = lr.positionCount;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)(n - 1) * Mathf.PI * 2f;
                lr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
        }

        static Vector3 Dir(float deg) => new Vector3(Mathf.Cos(deg * Mathf.Deg2Rad), 0, Mathf.Sin(deg * Mathf.Deg2Rad));

        // ───────── 두더지 캐디 ─────────
        Transform MakeMole(string name, float s, bool vest)
        {
            var root = new GameObject(name).transform; root.SetParent(world, false); root.localScale = Vector3.one * s;
            var body = new GameObject("Body").transform; body.SetParent(root, false);
            var fur = MgfLook.Lit(Fur, .12f);
            MgfLook.Prim(PrimitiveType.Sphere, "Torso", new Vector3(0, .38f, 0), new Vector3(.66f, .74f, .58f), fur, body, false);
            if (vest)
            {
                MgfLook.Prim(PrimitiveType.Sphere, "Vest", new Vector3(0, .34f, -.01f), new Vector3(.69f, .46f, .61f), MgfLook.Lit(Cream, .2f), body, false);
                MgfLook.Prim(PrimitiveType.Sphere, "VestTrim", new Vector3(0, .13f, 0), new Vector3(.66f, .07f, .58f), MgfLook.Lit(Rail, .4f), body, false);
                MgfLook.Prim(PrimitiveType.Cube, "Pocket", new Vector3(-.12f, .3f, -.3f), new Vector3(.14f, .1f, .02f), MgfLook.Lit(Felt, .2f), body, false);
            }
            var head = new GameObject("Head").transform; head.SetParent(body, false); head.localPosition = new Vector3(0, .8f, -.02f);
            MgfLook.Prim(PrimitiveType.Sphere, "Skull", Vector3.zero, new Vector3(.54f, .48f, .5f), fur, head, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EarL", new Vector3(-.22f, .17f, .02f), new Vector3(.14f, .12f, .08f), MgfLook.Lit(Pink, .2f), head, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EarR", new Vector3(.22f, .17f, .02f), new Vector3(.14f, .12f, .08f), MgfLook.Lit(Pink, .2f), head, false);
            var eye = MgfLook.Lit(Ink, .85f);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeL", new Vector3(-.11f, .05f, -.22f), Vector3.one * .075f, eye, head, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeR", new Vector3(.11f, .05f, -.22f), Vector3.one * .075f, eye, head, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Snout", new Vector3(0, -.05f, -.24f), new Vector3(.2f, .14f, .14f), MgfLook.Lit(H("#F2E6D3"), .1f), head, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Nose", new Vector3(0, -.02f, -.31f), new Vector3(.09f, .07f, .06f), MgfLook.Lit(Nose, .5f), head, false);
            MgfLook.Prim(PrimitiveType.Sphere, "CheekL", new Vector3(-.17f, -.06f, -.18f), new Vector3(.09f, .05f, .03f), MgfLook.Lit(Pink, .1f), head, false);
            MgfLook.Prim(PrimitiveType.Sphere, "CheekR", new Vector3(.17f, -.06f, -.18f), new Vector3(.09f, .05f, .03f), MgfLook.Lit(Pink, .1f), head, false);
            var pawL = MgfLook.Prim(PrimitiveType.Sphere, "PawL", new Vector3(-.26f, .42f, -.24f), new Vector3(.17f, .14f, .17f), MgfLook.Lit(FurDark, .1f), body, false).transform;
            var pawR = MgfLook.Prim(PrimitiveType.Sphere, "PawR", new Vector3(.26f, .42f, -.24f), new Vector3(.17f, .14f, .17f), MgfLook.Lit(FurDark, .1f), body, false).transform;
            MgfLook.Prim(PrimitiveType.Sphere, "FootL", new Vector3(-.16f, .04f, -.16f), new Vector3(.2f, .08f, .24f), MgfLook.Lit(FurDark, .1f), body, false);
            MgfLook.Prim(PrimitiveType.Sphere, "FootR", new Vector3(.16f, .04f, -.16f), new Vector3(.2f, .08f, .24f), MgfLook.Lit(FurDark, .1f), body, false);
            if (root.name == "MoleCaddy") { moleBody = body; moleHead = head; molePawL = pawL; molePawR = pawR; }
            return root;
        }

        void BuildMole()
        {
            mole = MakeMole("MoleCaddy", .95f, true);
            // 갈퀴(대기 중 모래 고르기)
            moleRake = new GameObject("Rake").transform; moleRake.SetParent(moleBody, false); moleRake.localPosition = new Vector3(.3f, .4f, -.3f);
            MgfLook.Prim(PrimitiveType.Cylinder, "Handle", new Vector3(0, -.05f, -.25f), new Vector3(.04f, .33f, .04f), MgfLook.Lit(Cream, .3f), moleRake, false).transform.localRotation = Quaternion.Euler(-62, 0, 0);
            MgfLook.Prim(PrimitiveType.Cube, "Head", new Vector3(0, -.3f, -.55f), new Vector3(.34f, .05f, .07f), MgfLook.Lit(Rail, .5f), moleRake, false);
            // 다른 섬에서 모래를 고르는 대기 두더지들
            for (int i = 0; i < 5; i++)
            {
                var m = MakeMole("IdleMole" + i, .62f, i % 2 == 0);
                var c = CourseCenter(1 + i * 2 % 8);
                var p = (i == 0 ? titleSpot + new Vector3(-3.6f, 0, -2.4f) : c + new Vector3(4.6f, 0, -3.2f + i * .5f));
                m.localPosition = p; m.localRotation = Quaternion.Euler(0, 180 + i * 37, 0);
                idleMoles.Add(m); idleMoleHome.Add(p);
            }
        }

        // ───────── 타이틀 소품 ─────────
        void BuildTitleProps()
        {
            titleFlag = new GameObject("TitlePennant").transform; titleFlag.SetParent(world, false);
            titleFlag.localPosition = titleSpot + new Vector3(-.4f, 0, 1.4f);
            MgfLook.Prim(PrimitiveType.Cylinder, "Pole", new Vector3(-2.15f, 2.05f, 0), new Vector3(.12f, 2.05f, .12f), MgfLook.Lit(Cream, .5f), titleFlag, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Knob", new Vector3(-2.15f, 4.15f, 0), Vector3.one * .26f, MgfLook.Lit(Rail, .6f), titleFlag, false);
            // 바람에 흔들리는 천(격자 메시)
            const int nx = 14, ny = 7;
            clothBase = new Vector3[(nx + 1) * (ny + 1)]; clothVerts = new Vector3[clothBase.Length];
            var uvs = new Vector2[clothBase.Length]; var tris = new int[nx * ny * 6];
            for (int y = 0; y <= ny; y++) for (int x = 0; x <= nx; x++)
            {
                int i = y * (nx + 1) + x;
                clothBase[i] = new Vector3(x / (float)nx * 4.4f, y / (float)ny * 2.5f, 0);
                uvs[i] = new Vector2(x / (float)nx, y / (float)ny);
            }
            int t = 0;
            for (int y = 0; y < ny; y++) for (int x = 0; x < nx; x++)
            {
                int i = y * (nx + 1) + x;
                tris[t++] = i; tris[t++] = i + nx + 1; tris[t++] = i + 1;
                tris[t++] = i + 1; tris[t++] = i + nx + 1; tris[t++] = i + nx + 2;
            }
            clothMesh = new Mesh { name = "TitleCloth" };
            clothMesh.vertices = clothBase; clothMesh.uv = uvs; clothMesh.triangles = tris; clothMesh.RecalculateNormals();
            var clothGo = new GameObject("Cloth"); clothGo.transform.SetParent(titleFlag, false); clothGo.transform.localPosition = new Vector3(-2.1f, 1.55f, 0);
            clothGo.AddComponent<MeshFilter>().sharedMesh = clothMesh;
            var cr = clothGo.AddComponent<MeshRenderer>(); cr.sharedMaterial = MgfLook.Lit(Cream, .12f, 0f, Cream * .32f); cr.shadowCastingMode = ShadowCastingMode.TwoSided;
            titleCloth = clothGo.transform;
            // 천에 인쇄된 제목(로고: 굵은 획 + 외곽선 + 포인트 색)
            var logo = MgfText.World("<color=#273347>한</color> <color=#D9A92E>끗</color>\n<color=#3D6DAA>골프</color>", new Vector3(2.2f, 1.32f, -.14f), 9.6f, Ink, titleCloth);
            logo.fontStyle = FontStyles.Bold; logo.lineSpacing = -22f; logo.textWrappingMode = TextWrappingModes.NoWrap;
            logo.outlineWidth = .12f; logo.outlineColor = new Color32(39, 51, 71, 255);
            var stitch = MgfText.World("· · · · · · · · · · · ·", new Vector3(2.2f, .2f, -.1f), 3.2f, H("#9DB3D4"), titleCloth);
            stitch.textWrappingMode = TextWrappingModes.NoWrap;
            // 티 위의 실제 골프공(시작 CTA)
            titleBall = new GameObject("TeeBall").transform; titleBall.SetParent(world, false);
            titleBall.localPosition = titleSpot + new Vector3(.4f, 0, -1.9f);
            MgfLook.Prim(PrimitiveType.Cylinder, "Tee", new Vector3(0, .12f, 0), new Vector3(.08f, .12f, .08f), MgfLook.Lit(Coral, .3f), titleBall, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Ball", new Vector3(0, .42f, 0), Vector3.one * .42f, MgfLook.Lit(Color.white, .65f), titleBall, false);
            titleRing = Line("TitlePulse", new Color(1, 1, 1, .9f), .07f, world, 41).transform;
            // 연습 벙커 앞 작은 벙커(타이틀 장식)
            var dc = titleSpot + new Vector3(2.9f, 0, -.8f);
            var deco = MgfLook.Prim(PrimitiveType.Cylinder, "TitleBunker", dc + Vector3.up * .004f, new Vector3(3.4f, .01f, 3.4f), MgfLook.Lit(Sand, .05f), world, false);
            deco.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            for (int s = 0; s < 18; s++)
            {
                float a = s / 18f * Mathf.PI * 2f;
                MgfLook.Prim(PrimitiveType.Sphere, "Sc", dc + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.78f, new Vector3(.36f, .09f, .36f), MgfLook.Lit(Cream, .2f), world, false);
            }
            // 벙커 가장자리 한 점에 닿은 노란 레일과 굴러가는 공(게임의 한 장면을 미리 보여 준다)
            var ta = dc + Dir(118) * 1.7f;
            var tr = MgfLook.Block("TitleRail", ta + Vector3.up * .17f, new Vector3(5.6f, .13f, .24f), .06f, MgfLook.Lit(Rail, .72f), world);
            Object.Destroy(tr.GetComponent<Collider>()); tr.transform.rotation = Quaternion.Euler(0, -28, 0);
            MgfLook.Prim(PrimitiveType.Cylinder, "TitleGripA", ta + Dir(28) * 2.8f + Vector3.up * .17f, new Vector3(.4f, .2f, .4f), MgfLook.Lit(Coral, .25f), world, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "TitleGripB", ta - Dir(28) * 2.8f + Vector3.up * .12f, new Vector3(.46f, .16f, .46f), MgfLook.Lit(Coral, .25f), world, false);
            titleRollBall = MgfLook.Prim(PrimitiveType.Sphere, "TitleRollBall", ta + Vector3.up * .38f, Vector3.one * .3f, MgfLook.Lit(Color.white, .62f), world, false).transform;
            titleRailA = ta; titleRailDir = Dir(28);
            var tf = BuildFlower(world, 1.3f); tf.position = ta + Vector3.up * .05f; tf.localRotation = Quaternion.identity;
            // 공 통·깃발 두 개
            var bucket = MgfLook.Prim(PrimitiveType.Cylinder, "BallBucket", titleSpot + new Vector3(-3.4f, .3f, -2.9f), new Vector3(.7f, .3f, .7f), MgfLook.Lit(Cream, .3f), world, false);
            for (int b = 0; b < 4; b++) MgfLook.Prim(PrimitiveType.Sphere, "BucketBall", bucket.transform.position + new Vector3(-.16f + (b % 2) * .3f, .32f + (b / 2) * .1f, -.1f + (b / 2) * .2f), Vector3.one * .26f, MgfLook.Lit(Color.white, .62f), world, false);
        }

        // ───────── UI ─────────
        RectTransform canvasRt, hudRt, guideRt, dialRt, endRt, titleRt, curtainL, curtainR;
        TextMeshProUGUI hudCourse, hudFirst, hudTime, bannerMain, bannerSub, toastTxt, readout, revealTxt, propertyTxt;
        TextMeshProUGUI titleBadge, titleBest, titleReview, titleCta, endHead, endWhy, endCourses, endFirst, endScore, endReview, endHint;
        Image timeFill, toastBg, readoutBg, revealBg, propertyBg, ctaBg;
        readonly Image[] ballIcons = new Image[3];
        readonly Image[] ripples = new Image[4];
        readonly float[] rippleT = { 9, 9, 9, 9 };
        int rippleNext;
        Image guideRing, guideHand, guideGhost;
        readonly Image[] guideDots = new Image[9];
        Image dialNeedle;
        Sprite roundSprite, ringSprite, dotSprite;
        float toastT = 9f, revealShowT = 9f, propertyT = 9f, boost, guidePhase;
        float shownScore, endT;
        int lastHudKey = -1;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite)
        {
            var rt = R(name, parent, anchor, pos, size); var im = rt.gameObject.AddComponent<Image>();
            im.color = color; im.sprite = sprite; im.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }

        TextMeshProUGUI Txt(Transform parent, string s, Vector2 anchor, Vector2 pos, float size, Color color, float width)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 2.6f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color;
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal;
            t.fontStyle = FontStyles.Bold; t.text = s;
            return t;
        }

        static Sprite MakeSprite(int size, float radius, int kind)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                float dx = fx - size * .5f, dy = fy - size * .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                if (kind == 1) a = Mathf.Clamp01(1f - Mathf.Abs(d - size * .42f) / (size * .06f));
                else if (kind == 2) a = Mathf.Clamp01(size * .5f - d);
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(dx) - (size * .5f - radius), 0), qy = Mathf.Max(Mathf.Abs(dy) - (size * .5f - radius), 0);
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            float b = kind == 0 ? radius + 2 : 0;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        Image Tag(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            // 천 표찰: 크림 천 + 먹색 얇은 바느질 테두리
            var edge = Img(parent, name, anchor, pos, size + new Vector2(4, 4), new Color(Ink.r, Ink.g, Ink.b, .18f), roundSprite);
            Img(edge.transform, "Cloth", new Vector2(.5f, .5f), Vector2.zero, size, Cream, roundSprite);
            return edge;
        }

        void BuildUi()
        {
            roundSprite = MakeSprite(64, 18, 0); ringSprite = MakeSprite(64, 0, 1); dotSprite = MakeSprite(32, 0, 2);
            var canvas = MgfText.Canvas; canvasRt = (RectTransform)canvas.transform;
            guideRt = R("Guide", canvasRt, Vector2.zero, Vector2.zero, Vector2.zero); guideRt.pivot = Vector2.zero;
            for (int i = 0; i < guideDots.Length; i++) guideDots[i] = Img(guideRt, "GuideDot" + i, Vector2.zero, Vector2.zero, new Vector2(8, 8), new Color(1, 1, 1, .85f), dotSprite);
            guideGhost = Img(guideRt, "GhostHandle", Vector2.zero, Vector2.zero, new Vector2(40, 40), new Color(1, 1, 1, .55f), ringSprite);
            guideRing = Img(guideRt, "PulseRing", Vector2.zero, Vector2.zero, new Vector2(70, 70), Color.white, ringSprite);
            guideHand = Img(guideRt, "Fingertip", Vector2.zero, Vector2.zero, new Vector2(20, 20), new Color(1, 1, 1, .8f), dotSprite);
            var handEdge = Img(guideHand.transform, "Edge", new Vector2(.5f, .5f), Vector2.zero, new Vector2(28, 28), new Color(Ink.r, Ink.g, Ink.b, .75f), ringSprite);
            handEdge.transform.SetAsFirstSibling();
            for (int i = 0; i < ripples.Length; i++) { ripples[i] = Img(guideRt, "Ripple" + i, Vector2.zero, Vector2.zero, new Vector2(30, 30), new Color(1, 1, 1, 0), ringSprite); }
            readoutBg = Tag(guideRt, "AngleReadout", Vector2.zero, Vector2.zero, new Vector2(112, 34));
            readout = Txt(readoutBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 18, Ink, 112); readout.textWrappingMode = TextWrappingModes.NoWrap;
            readoutBg.gameObject.SetActive(false);

            // HUD (오른쪽 위 음소거 버튼 자리 60px 는 비운다)
            hudRt = R("Hud", canvasRt, new Vector2(.5f, 1f), new Vector2(0, -64), new Vector2(390, 128));
            var t1 = Tag(hudRt, "CourseTag", new Vector2(0f, 1f), new Vector2(54, -26), new Vector2(92, 36));
            hudCourse = Txt(t1.transform, "", new Vector2(.5f, .5f), Vector2.zero, 17, Ink, 92);
            var t2 = Tag(hudRt, "FirstTag", new Vector2(0f, 1f), new Vector2(160, -26), new Vector2(110, 36));
            hudFirst = Txt(t2.transform, "", new Vector2(.5f, .5f), Vector2.zero, 16, Ink, 110);
            var t3 = Tag(hudRt, "BallTray", new Vector2(0f, 1f), new Vector2(267, -26), new Vector2(92, 36));
            for (int i = 0; i < 3; i++)
            {
                ballIcons[i] = Img(t3.transform, "Ball" + i, new Vector2(.5f, .5f), new Vector2(-24 + i * 24, 0), new Vector2(18, 18), Color.white, dotSprite);
                var e = Img(ballIcons[i].transform, "Edge", new Vector2(.5f, .5f), Vector2.zero, new Vector2(22, 22), new Color(Ink.r, Ink.g, Ink.b, .55f), ringSprite);
                e.transform.SetAsFirstSibling();
            }
            hudTime = Txt(hudRt, "", new Vector2(0f, 1f), new Vector2(44, -58), 15, Cream, 80);
            var tb = Img(hudRt, "TimeTrack", new Vector2(0f, 1f), new Vector2(222, -58), new Vector2(260, 8), new Color(Cream.r, Cream.g, Cream.b, .5f), roundSprite);
            timeFill = Img(tb.transform, "Fill", new Vector2(0, .5f), Vector2.zero, new Vector2(260, 8), Rail, roundSprite);
            timeFill.rectTransform.pivot = new Vector2(0, .5f);
            var bb = Tag(hudRt, "Banner", new Vector2(.5f, 1f), new Vector2(0, -100), new Vector2(372, 56));
            bannerMain = Txt(bb.transform, "", new Vector2(.5f, .5f), new Vector2(0, 8), 16, Ink, 360);
            bannerMain.enableAutoSizing = true; bannerMain.fontSizeMin = 12; bannerMain.fontSizeMax = 16.5f; bannerMain.textWrappingMode = TextWrappingModes.NoWrap;
            bannerSub = Txt(bb.transform, "", new Vector2(.5f, .5f), new Vector2(0, -14), 13, H("#4E5D78"), 360);
            bannerSub.enableAutoSizing = true; bannerSub.fontSizeMin = 10; bannerSub.fontSizeMax = 13; bannerSub.textWrappingMode = TextWrappingModes.NoWrap;
            hudRt.gameObject.SetActive(false);

            // 정답 이유·성질(실제 도형 위 문구)
            revealBg = Tag(canvasRt, "Reason", new Vector2(.5f, .5f), Vector2.zero, new Vector2(300, 42));
            revealTxt = Txt(revealBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 19, Ink, 296); revealTxt.textWrappingMode = TextWrappingModes.NoWrap; revealTxt.enableAutoSizing = true; revealTxt.fontSizeMin = 13; revealTxt.fontSizeMax = 19;
            revealBg.gameObject.SetActive(false);
            propertyBg = Tag(canvasRt, "Property", new Vector2(.5f, 0f), new Vector2(0, 268), new Vector2(340, 44));
            propertyTxt = Txt(propertyBg.transform, UiStrings.Property, new Vector2(.5f, .5f), Vector2.zero, 18, H("#C2463A"), 340);
            propertyBg.gameObject.SetActive(false);

            // 토스트
            toastBg = Tag(canvasRt, "Toast", new Vector2(.5f, 0f), new Vector2(0, 270), new Vector2(330, 40));
            toastTxt = Txt(toastBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 16, Ink, 326);
            toastBg.gameObject.SetActive(false);

            // 세로 화면 하단: 레일과 연동된 확대 반원 눈금
            dialRt = R("Dial", canvasRt, new Vector2(.5f, 0f), new Vector2(0, 34), new Vector2(10, 10));
            Img(dialRt, "DialFelt", new Vector2(.5f, .5f), new Vector2(0, 70), new Vector2(360, 196), new Color(Cream.r, Cream.g, Cream.b, .9f), roundSprite);
            Img(dialRt, "DialHub", new Vector2(.5f, .5f), Vector2.zero, new Vector2(18, 18), Ink, dotSprite);
            for (int a = 0; a <= 180; a += 5)
            {
                bool major = a % 30 == 0, mid = a % 10 == 0;
                float len = major ? 20 : mid ? 12 : 7, rr = 150 - len * .5f;
                var tick = Img(dialRt, "Tick" + a, new Vector2(.5f, .5f), new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * rr, new Vector2(major ? 3 : 2, len), new Color(Ink.r, Ink.g, Ink.b, major ? .9f : .5f), null);
                tick.rectTransform.localRotation = Quaternion.Euler(0, 0, a - 90);
                if (major) Txt(dialRt, a + "°", new Vector2(.5f, .5f), new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * 116, 14, Ink, 50);
            }
            dialNeedle = Img(dialRt, "Needle", new Vector2(.5f, .5f), Vector2.zero, new Vector2(7, 150), Rail, roundSprite);
            dialNeedle.rectTransform.pivot = new Vector2(.5f, 0f);
            var nTip = Img(dialNeedle.transform, "Grip", new Vector2(.5f, 1f), Vector2.zero, new Vector2(30, 30), Coral, dotSprite);
            Txt(dialRt, "기준 0°", new Vector2(.5f, .5f), new Vector2(150, -16), 13, H("#4E5D78"), 80);
            dialRt.gameObject.SetActive(false);

            // 타이틀 UI(세계가 화면을 채우고, 아래에 뱃지만)
            titleRt = R("TitleUi", canvasRt, new Vector2(.5f, 0f), new Vector2(0, 0), new Vector2(380, 10));
            ctaBg = Tag(canvasRt, "TapBallTag", new Vector2(0, 0), Vector2.zero, new Vector2(150, 40));
            titleCta = Txt(ctaBg.transform, UiStrings.TapBall, new Vector2(.5f, .5f), Vector2.zero, 17, Ink, 150);
            var badge = Tag(titleRt, "Badge", new Vector2(.5f, .5f), new Vector2(-92, 44), new Vector2(150, 34));
            titleBadge = Txt(badge.transform, UiStrings.Badge, new Vector2(.5f, .5f), Vector2.zero, 16, Ink, 150);
            var best = Tag(titleRt, "Best", new Vector2(.5f, .5f), new Vector2(88, 44), new Vector2(170, 34));
            titleBest = Txt(best.transform, "", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 170);
            titleReview = Txt(titleRt, "", new Vector2(.5f, .5f), new Vector2(0, 84), 15, Cream, 370);

            // 결과 화면
            endRt = R("End", canvasRt, new Vector2(.5f, .5f), new Vector2(0, -150), new Vector2(340, 330));
            var card = Tag(endRt, "Card", new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 320));
            endHead = Txt(card.transform, "", new Vector2(.5f, 1f), new Vector2(0, -38), 30, Ink, 320);
            endWhy = Txt(card.transform, "", new Vector2(.5f, 1f), new Vector2(0, -80), 14, H("#4E5D78"), 300);
            endCourses = Txt(card.transform, "", new Vector2(.5f, 1f), new Vector2(0, -124), 24, Ink, 320);
            endFirst = Txt(card.transform, "", new Vector2(.5f, 1f), new Vector2(0, -162), 20, Ink, 320);
            endScore = Txt(card.transform, "", new Vector2(.5f, 1f), new Vector2(0, -204), 30, H("#3D6DAA"), 320);
            endReview = Txt(card.transform, "", new Vector2(.5f, 1f), new Vector2(0, -246), 14, H("#C2463A"), 300);
            endHint = Txt(card.transform, UiStrings.Restart, new Vector2(.5f, 0f), new Vector2(0, 26), 16, Ink, 320);
            endRt.gameObject.SetActive(false);

            curtainL = Img(canvasRt, "CurtainL", new Vector2(0, .5f), Vector2.zero, new Vector2(10, 10), Cream, null).rectTransform;
            curtainR = Img(canvasRt, "CurtainR", new Vector2(1, .5f), Vector2.zero, new Vector2(10, 10), Cream, null).rectTransform;
            curtainL.pivot = new Vector2(0, .5f); curtainR.pivot = new Vector2(1, .5f);
            curtainL.anchorMin = new Vector2(0, 0); curtainL.anchorMax = new Vector2(0, 1);
            curtainR.anchorMin = new Vector2(1, 0); curtainR.anchorMax = new Vector2(1, 1);
            curtainL.sizeDelta = new Vector2(400, 0); curtainR.sizeDelta = new Vector2(400, 0);
        }

        float Sf => MgfText.Canvas ? Mathf.Max(.01f, MgfText.Canvas.scaleFactor) : 1f;
        Vector2 ToCanvas(Vector2 screen) => screen / Sf;
        Vector2 CanvasSize => canvasRt ? canvasRt.rect.size : new Vector2(390, 844);

        void PositionReadout(Vector2 handleScreen, float gap)
        {
            var p = ToCanvas(handleScreen) + new Vector2(0, gap);
            // 세로 판형에서는 위쪽 손잡이가 HUD·발문 아래로 들어간다. 읽기표를 고정 안전띠에 두어
            // 390×844뿐 아니라 820×1180에서도 시간 막대와 발문을 가리지 않게 한다.
            if (!Land) p = new Vector2(CanvasSize.x * .5f, CanvasSize.y - 154f);
            readoutBg.rectTransform.anchoredPosition = p;
        }

        // ───────── 카메라 ─────────
        void PoseCamera(Vector3 focus, float pitch, float yaw, float size, float fy)
        {
            var q = Quaternion.Euler(pitch, yaw, 0);
            camRotT = q; camSizeT = size;
            camPosT = focus - q * Vector3.forward * 45f + q * Vector3.up * ((.5f - fy) * 2f * size);
        }

        bool Land => MgfBridge.IsLandLayout;
        float HudScale => Land ? .74f : 1f;
        float HudPx => 132f * HudScale * Sf;
        float DialPx => Land ? 0f : 236f * Sf;

        void FramePlan(bool roll)
        {
            float W = Screen.width, Hh = Screen.height;
            float fieldH = Hh - HudPx - DialPx, fieldW = W * (Land ? .9f : .97f);
            float rv = curR * 2.42f;
            float ppw = Mathf.Min(fieldW, fieldH * .97f) / (2f * rv);
            float size = Hh / (2f * Mathf.Max(1f, ppw));
            float fy = (DialPx + fieldH * .5f) / Hh;
            if (roll) PoseCamera(curA + (curO - curA) * .25f, 52f, 0f, size * 1.2f, fy);
            else PoseCamera(curA, 90f, 0f, size, fy);
            view = roll ? View.Roll : View.Plan;
        }

        void FrameTitle()
        {
            float aspect = Mathf.Max(.3f, (float)Screen.width / Mathf.Max(1, Screen.height));
            float size = Land ? 5.6f : Mathf.Max(5.6f, 3.4f / aspect);
            PoseCamera(titleSpot + new Vector3(.2f, .6f, .2f), 38f, -16f, size, Land ? .46f : .5f);
            view = View.Title;
        }

        Vector3 overviewFocus;
        void FrameOverview()
        {
            // 결과 화면: 지나온 코스를 따라 카메라가 천천히 흐른다(카드 뒤로 깃발·끈·레일이 보인다)
            float aspect = Mathf.Max(.3f, (float)Screen.width / Mathf.Max(1, Screen.height));
            float size = Land ? 9f : Mathf.Max(8f, 4.2f / aspect);
            PoseCamera(overviewFocus, 46f, -8f, size, Land ? .5f : .7f);
            view = View.Overview;
        }

        bool CameraSettled() => (camPos - camPosT).sqrMagnitude < .0025f && Quaternion.Angle(camRot, camRotT) < .3f && Mathf.Abs(camSize - camSizeT) < .02f;

        void SnapCameraNow()
        {
            camPos = camPosT; camRot = camRotT; camSize = camSizeT; ApplyCamera();
        }

        void ApplyCamera()
        {
            cam.transform.SetPositionAndRotation(camPos, camRot);
            cam.orthographicSize = camSize;
        }

        int lastW, lastH;
        bool lastLand;

        void TickCamera(float dt)
        {
            if (Screen.width != lastW || Screen.height != lastH || Land != lastLand)
            {
                lastW = Screen.width; lastH = Screen.height; lastLand = Land;
                RefreshLayout();
            }
            float a = 1f - Mathf.Exp(-dt * 7.5f);
            camPos = Vector3.Lerp(camPos, camPosT, a);
            camRot = Quaternion.Slerp(camRot, camRotT, a);
            camSize = Mathf.Lerp(camSize, camSizeT, a);
            if (flow == Flow.Aim || flow == Flow.Draw) { camPos = camPosT; camRot = camRotT; camSize = camSizeT; } // 입력 중 카메라 고정
            ApplyCamera();
        }

        void RefreshLayout()
        {
            switch (view)
            {
                case View.Title: FrameTitle(); break;
                case View.Plan: FramePlan(false); break;
                case View.Roll: FramePlan(true); break;
                case View.Overview: FrameOverview(); break;
            }
            bool portraitAim = !Land && current != null && flow != Flow.Title && flow != Flow.End;
            dialRt.gameObject.SetActive(portraitAim);
            if (flow == Flow.Aim || flow == Flow.Draw) { SnapCameraNow(); CaptureScreenAnchors(); }
            hudRt.anchoredPosition = new Vector2(0, -64 * HudScale); hudRt.localScale = Vector3.one * HudScale;
            endRt.anchoredPosition = new Vector2(0, Land ? 0 : -150); endRt.localScale = Vector3.one * (Land ? .9f : 1f);
            toastBg.rectTransform.anchoredPosition = new Vector2(0, Land ? 40 : 262);
        }

        // ───────── 좌표 ─────────
        Vector3 PointO() => curO + Vector3.up * .05f;
        Vector3 PointA() => curA + Vector3.up * .05f;
        Vector3 PointB() => curB + Vector3.up * .05f;
        Vector2 ScreenOf(Vector3 w) { var s = cam.WorldToScreenPoint(w); return new Vector2(s.x, s.y); }
        float HitPx() => Mathf.Max(46f, 40f * Sf);
        Vector3 RailEnd(int sign, int angle) => curA + Dir(angle) * (curL * sign);

        bool RailHit(Vector2 p)
        {
            if (current == null) return false;
            var a = ScreenOf(PointA());
            var e1 = ScreenOf(RailEnd(1, st.railAngle)); var e2 = ScreenOf(RailEnd(-1, st.railAngle));
            float hit = Mathf.Max(60f, 44f * Sf);
            if (Vector2.Distance(p, e1) <= hit || Vector2.Distance(p, e2) <= hit) return true;
            // 레일 몸통(A 근처 제외)
            var seg = e1 - e2; float len = seg.magnitude; if (len < 1f) return false;
            var u = seg / len; var rel = p - e2; float along = Vector2.Dot(rel, u);
            float perp = Mathf.Abs(rel.x * u.y - rel.y * u.x);
            return along >= 0 && along <= len && perp <= Mathf.Max(26f, 18f * Sf) && Vector2.Distance(p, a) >= len * .14f;
        }

        int AngleFromRailPointer(Vector2 screen)
        {
            var ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0));
            var plane = new Plane(Vector3.up, new Vector3(0, .17f, 0));
            if (!plane.Raycast(ray, out float d)) return st.railAngle;
            var v = ray.GetPoint(d) - curA;
            if (new Vector2(v.x, v.z).magnitude < .25f) return st.railAngle;
            int a = Mathf.RoundToInt(Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg);
            return HanKkeutRules.Norm180(a);
        }

        Vector2 DialCenterScreen() => new Vector2(Screen.width * .5f, 34f * Sf);
        float DialRadiusScreen() => 150f * Sf;

        bool DialHit(Vector2 p)
        {
            if (!dialRt.gameObject.activeSelf) return false;
            var c = DialCenterScreen();
            return p.y >= c.y - 24f * Sf && Vector2.Distance(p, c) <= DialRadiusScreen() * 1.3f && p.y <= DialPx + 8f * Sf;
        }

        int AngleFromDial(Vector2 p)
        {
            var v = p - DialCenterScreen();
            float deg = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            if (deg < 0) deg = deg < -90f ? 180f : 0f;
            return HanKkeutRules.Norm180(Mathf.RoundToInt(Mathf.Clamp(deg, 0f, 180f)));
        }

        void CaptureScreenAnchors()
        {
            if (current == null || cam == null) return;
            float Hh = Screen.height;
            var a = ScreenOf(PointA()); var o = ScreenOf(PointO()); var e = ScreenOf(RailEnd(1, st.railAngle));
            st.aPx = Mathf.RoundToInt(a.x); st.aPy = Mathf.RoundToInt(Hh - a.y);
            st.oPx = Mathf.RoundToInt(o.x); st.oPy = Mathf.RoundToInt(Hh - o.y);
            st.handlePx = Mathf.RoundToInt(e.x); st.handlePy = Mathf.RoundToInt(Hh - e.y);
            st.railPx = Mathf.RoundToInt(Vector2.Distance(a, e));
            if (current.band == 3) { var b = ScreenOf(PointB()); st.bPx = Mathf.RoundToInt(b.x); st.bPy = Mathf.RoundToInt(Hh - b.y); }
            else { st.bPx = -1; st.bPy = -1; }
            bool dial = dialRt.gameObject.activeSelf;
            var dc = DialCenterScreen();
            st.dialCx = dial ? Mathf.RoundToInt(dc.x) : -1; st.dialCy = dial ? Mathf.RoundToInt(Hh - dc.y) : -1; st.dialR = dial ? Mathf.RoundToInt(DialRadiusScreen()) : 0;
        }

        // ───────── 화면 상태 전환 ─────────
        static bool CoverMode => Application.absoluteURL != null && Application.absoluteURL.Contains("cover=1");

        void ShowTitleVisual()
        {
            hudRt.gameObject.SetActive(false); endRt.gameObject.SetActive(false); dialRt.gameObject.SetActive(false);
            titleRt.gameObject.SetActive(!CoverMode); ctaBg.gameObject.SetActive(!CoverMode);
            int best = PlayerPrefs.GetInt("han-kkeut-golf-best-first", 0);
            titleBest.text = best > 0 ? "최고 첫 제출 " + best + "/8" : "통과 기준 첫 제출 7/8";
            titleReview.text = PlayerPrefs.GetString("han-kkeut-golf-review", "");
            ShowCourseMarks(false); ball.gameObject.SetActive(false);
            for (int i = 1; i < CourseCount; i++) courses[i].flag.gameObject.SetActive(false);
            mole.localPosition = titleSpot + new Vector3(2.9f, 0, -.8f); mole.localRotation = Quaternion.Euler(0, 200, 0); moleDuck = moleDuckTarget = 0;
            curtainL.sizeDelta = curtainR.sizeDelta = new Vector2(0, 0); curtain = curtainTarget = 0f;
            FrameTitle(); SnapCameraNow();
        }

        float curtain;      // 0 열림 … 1 닫힘
        float curtainTarget;

        void BeginOpeningVisual()
        {
            titleRt.gameObject.SetActive(false); ctaBg.gameObject.SetActive(false); endRt.gameObject.SetActive(false);
            curtainTarget = 1f; MgfFx.Punch(titleBall, .3f, .25f);
        }

        void ResetCoursesVisual()
        {
            foreach (var r in keptRails) r.gameObject.SetActive(false);
            for (int i = 0; i < CourseCount; i++)
            {
                courses[i].flower.gameObject.SetActive(false); courses[i].tag.gameObject.SetActive(false);
                courses[i].flag.gameObject.SetActive(i == 0);
                if (courses[i].rail) courses[i].rail = null;
                if (i > 0) { strings[i].gameObject.SetActive(false); stringTaut[i] = false; }
            }
            crater.gameObject.SetActive(false); wrongTrace.gameObject.SetActive(false);
        }

        void LoadCourseVisual(GolfProblem p, int idx, bool isPractice, int startAngle)
        {
            var c = courses[idx];
            c.R = WorldR(p.radius); ResizeBunker(c, idx);
            curR = c.R; curL = curR * 2.25f; curO = c.o;
            curA = curO + Dir(p.theta) * curR;
            curB = p.phi >= 0 ? curO + Dir(p.phi) * curR : curO;
            // 깃발: 레일이 돌아가는 원판(반지름 L) 밖, O 를 사이에 두고 A 반대쪽
            var fp = curO + Dir(p.theta + 180 + 52) * (curR + 1.15f);
            c.flag.position = new Vector3(fp.x, 0, fp.z); c.flag.rotation = Quaternion.identity;
            if (!c.flag.gameObject.activeSelf) { c.flag.gameObject.SetActive(true); MgfFx.Punch(c.flag, .25f, .35f); }
            moleHome = curO + Dir(p.theta + 230) * (curR * .6f);
            mole.position = moleHome; mole.rotation = Quaternion.Euler(0, 180, 0); moleDuck = 0;

            railBarMf.sharedMesh = MgfLook.RoundedBox(new Vector3(curL * 2f, .13f, .24f), .06f, 3);
            gripNear.localPosition = new Vector3(-curL, .17f, 0); gripFar.localPosition = new Vector3(curL, .12f, 0); cupFar.localPosition = new Vector3(curL, .29f, 0);
            railRoot.position = curA; pivot.position = curA + Vector3.up * .1f;
            SetRailVisual(startAngle, false);
            ball.gameObject.SetActive(true); PlaceBallAtStart(startAngle); rollBallGone = false;
            crater.gameObject.SetActive(false); wrongTrace.gameObject.SetActive(false); sandFlower.gameObject.SetActive(false);
            rightMark.gameObject.SetActive(false); ghostRail.gameObject.SetActive(isPractice); revealBg.gameObject.SetActive(false);
            if (isPractice) SetLine(ghostRail, RailEnd(-1, p.answer) + Vector3.up * .06f, RailEnd(1, p.answer) + Vector3.up * .06f);

            // 도형 표지
            ShowCourseMarks(true);
            SetLine(baseLine, curO + new Vector3(-curR - .7f, .045f, 0), curO + new Vector3(curR + 1.05f, .045f, 0));
            SetLine(aParallel, curA + new Vector3(-.75f, .045f, 0), curA + new Vector3(.75f, .045f, 0));
            labBase.transform.position = curO + new Vector3(curR + 1.55f, .06f, -.02f);
            for (int i = 0; i < thetaArc.positionCount; i++)
            {
                float a = p.theta * i / (float)(thetaArc.positionCount - 1);
                thetaArc.SetPosition(i, curO + Dir(a) * (curR * .36f) + Vector3.up * .05f);
            }
            labTheta.text = p.theta + "°";
            labTheta.transform.position = curO + Dir(p.theta < 26 ? p.theta + 24 : p.theta * .5f) * (curR * .36f + .42f) + Vector3.up * .06f; // 작은 각이면 OA 위로 비켜 둔다
            labO.transform.position = curO + Dir(p.theta + 135) * .46f + Vector3.up * .06f;
            labA.transform.position = curA + Dir(p.theta) * .46f + Dir(p.theta + 90) * .2f + Vector3.up * .3f;
            SetLine(oaLine, PointO(), PointA());
            oaLine.gameObject.SetActive(p.band == 1);
            labRadius.gameObject.SetActive(false);
            bool hasB = p.band == 3;
            obLine.gameObject.SetActive(hasB); labB.gameObject.SetActive(hasB);
            if (hasB)
            {
                SetLine(obLine, PointO(), PointB()); obLine.startColor = obLine.endColor = Ink;
                labB.transform.position = curB + Dir(p.phi) * .45f + Vector3.up * .06f;
            }
            drawLine.gameObject.SetActive(false);
            tickRing.position = curA + Vector3.up * .04f; tickRing.localScale = Vector3.one * curL;
            readoutBg.gameObject.SetActive(false);

            hudRt.gameObject.SetActive(true); endRt.gameObject.SetActive(false); titleRt.gameObject.SetActive(false); ctaBg.gameObject.SetActive(false);
            FramePlan(false);
            RefreshLayout();
            if (flow == Flow.Opening || curtain > .5f) { SnapCameraNow(); curtainTarget = 0f; }
            UpdateBanner();
        }

        void ShowCourseMarks(bool on)
        {
            oaLine.gameObject.SetActive(on); obLine.gameObject.SetActive(on); baseLine.gameObject.SetActive(on); aParallel.gameObject.SetActive(on);
            thetaArc.gameObject.SetActive(on); labO.gameObject.SetActive(on); labA.gameObject.SetActive(on); labB.gameObject.SetActive(on);
            labTheta.gameObject.SetActive(on); labBase.gameObject.SetActive(on); labRadius.gameObject.SetActive(false);
            railRoot.gameObject.SetActive(on); pivot.gameObject.SetActive(on); tickRing.gameObject.SetActive(on);
            if (!on) { rightMark.gameObject.SetActive(false); ghostRail.gameObject.SetActive(false); wrongTrace.gameObject.SetActive(false); drawLine.gameObject.SetActive(false); }
        }

        static void SetLine(LineRenderer lr, Vector3 a, Vector3 b) { lr.SetPosition(0, a); lr.SetPosition(1, b); }

        void SetRailVisual(int angle, bool fromInput)
        {
            railRoot.rotation = Quaternion.Euler(0, -angle, 0);
            if (!rollBallGone && flow != Flow.Rolling) PlaceBallAtStart(angle);
            if (fromInput) { readout.text = "레일 " + angle + "°"; railTick++; if (railTick % 3 == 0) MgfSfx.Play("tap", .05f); }
            dialNeedle.rectTransform.localRotation = Quaternion.Euler(0, 0, angle - 90);
        }
        int railTick;

        void PlaceBallAtStart(int angle)
        {
            var d = Dir(angle);
            ball.position = curA - d * (curL - .42f) + Vector3.up * (.23f + .15f);
        }

        void EnterInputVisual()
        {
            RefreshLayout();
            SnapCameraNow();
            readoutBg.gameObject.SetActive(flow == Flow.Aim);
            readout.text = "레일 " + st.railAngle + "°";
            UpdateBanner();
        }

        void RadiusDrawnVisual()
        {
            drawLine.gameObject.SetActive(false);
            oaLine.gameObject.SetActive(true); SetLine(oaLine, PointO(), PointA());
            MgfFx.Punch(labA.transform, .4f, .3f);
            labRadius.gameObject.SetActive(true);
            labRadius.transform.position = Vector3.Lerp(curO, curA, .5f) + Dir(st.theta + 90) * .36f + Vector3.up * .07f;
            labRadius.transform.rotation = Quaternion.Euler(90, -(st.theta > 90 ? st.theta - 180 : st.theta), 0);
            if (current != null && current.band == 3)
            {
                obLine.startColor = obLine.endColor = new Color(.5f, .54f, .6f, .45f);
                labB.text = "B"; labB.color = new Color(.45f, .5f, .58f);
            }
            readoutBg.gameObject.SetActive(true); readout.text = "레일 " + st.railAngle + "°";
            MgfFx.Glow(PointA(), new Color(1f, .95f, .7f), 10, .4f);
            UpdateBanner();
        }

        void BeginDrawVisual(bool fromO)
        {
            drawLine.gameObject.SetActive(true);
            var s = fromO ? PointO() : PointA();
            SetLine(drawLine, s, s);
        }

        void DrawDragVisual(Vector2 screen, bool fromO)
        {
            var ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0));
            var plane = new Plane(Vector3.up, new Vector3(0, .05f, 0));
            if (!plane.Raycast(ray, out float d)) return;
            SetLine(drawLine, fromO ? PointO() : PointA(), ray.GetPoint(d));
        }

        void CancelDrawVisual() { drawLine.gameObject.SetActive(false); }

        void GrabRailVisual() { MgfFx.Punch(gripFar, .25f, .2f); MgfFx.Punch(gripNear, .25f, .2f); }
        void ReleaseRailVisual() { }

        float pathDemoT = 9f;
        void PathDemoVisual() { pathDemoT = 0f; }

        void RefuseHandle(string msg)
        {
            Toast(msg);
            refuseShakeT = 0f;
            MgfSfx.Play("pop", .1f);
        }
        float refuseShakeT = 9f;

        void PointAtHandle() { boost = 1f; }
        void PointAtO() { boost = 1f; }
        void BoostGuide() { boost = 1f; guidePhase = 0f; }

        void Toast(string msg)
        {
            toastTxt.text = msg; toastT = 0f; toastBg.gameObject.SetActive(true);
            var rt = toastBg.rectTransform; rt.localScale = Vector3.one * .92f;
        }

        void Ripple(Vector2 screen, string msg)
        {
            var r = ripples[rippleNext]; rippleNext = (rippleNext + 1) % ripples.Length;
            r.rectTransform.anchoredPosition = ToCanvas(screen); rippleT[System.Array.IndexOf(ripples, r)] = 0f;
            if (!string.IsNullOrEmpty(msg)) Toast(msg);
        }

        void UpdateBanner()
        {
            if (current == null) return;
            if (practice)
            {
                bannerMain.text = UiStrings.GoalLine;
                bannerSub.text = "OA " + current.theta + "° · " + UiStrings.PracticeSub;
                return;
            }
            if (flow == Flow.Draw)
            {
                bannerMain.text = current.band == 3 ? (st.bridge ? UiStrings.FixBridge : UiStrings.FixPlain) : (st.bridge ? UiStrings.DrawBridge : UiStrings.DrawPlain);
                bannerSub.text = "OA와 기준 반직선의 각 " + current.theta + "°" + (st.bridge ? " · " + UiStrings.Paused : "");
                return;
            }
            bannerMain.text = st.repairMode ? UiStrings.Repair : UiStrings.AimLine;
            bannerSub.text = "OA와 기준 반직선의 각 " + current.theta + "° · " + UiStrings.Tolerance;
        }

        // ───────── 주행(판정 결과 재생) ─────────
        void BeginRollVisual(bool ok, int angle, bool counted, string mis)
        {
            rollOk = ok; rollAngle = angle; rollCounted = counted; rollMis = mis; rollT = 0f;
            readoutBg.gameObject.SetActive(false); ghostRail.gameObject.SetActive(false); revealBg.gameObject.SetActive(false);
            rollD = Dir(angle); rollS = curA - rollD * (curL - .42f);
            float travel = 2f * curL - .84f;
            float v = 6.5f;
            k[0] = .4f;
            if (ok)
            {
                float toA = curL - .42f - .55f;
                k[1] = k[0] + toA / v;          // A 근처 도착
                k[2] = k[1] + .8f;              // A 를 천천히 스침(0.8초)
                k[3] = k[2] + toA / v;          // 홀 도착
                k[4] = k[3] + .3f;              // 홀인
                k[5] = k[4] + .45f;             // 두더지가 A 로
                k[6] = k[5] + .55f;             // 깃발로 옮겨 꽂음
                k[7] = k[6] + (practice ? 1.6f : .5f); // 끈 당김 / 성질 문구
            }
            else
            {
                double te = HanKkeutRules.FirstEntry(curA.x, curA.z, curO.x, curO.z, curR, angle, curL);
                tEntry = te < 0 ? curL : (float)te - .42f;
                tEntry = Mathf.Clamp(tEntry, .05f, travel);
                k[1] = k[0] + tEntry / v;       // 첫 진입점
                k[2] = k[1] + .4f;              // 모래에 박힘
                k[3] = k[2] + .35f;
                k[4] = k[3] + 1.35f;            // 정답 상태 보여 주기(직각 표식 + 이유)
                k[5] = k[4] + .45f;             // 두더지가 공을 떠냄
                k[6] = k[5]; k[7] = k[5];
            }
            rollDur = k[7];
            FramePlan(true);
            stepFlags = 0;
            if (!ok)
            {
                wrongTrace.gameObject.SetActive(true);
                SetLine(wrongTrace, RailEnd(-1, angle) + Vector3.up * .05f, RailEnd(1, angle) + Vector3.up * .05f);
                if (rollCounted && courseIndex > 0) DropString(courseIndex);
            }
        }

        int stepFlags;
        bool Step(int bit) { if ((stepFlags & (1 << bit)) != 0) return false; stepFlags |= 1 << bit; return true; }

        bool RollFinished() => rollT >= rollDur;

        void FinishRollNow()
        {
            if (flow != Flow.Rolling) return;
            rollT = rollDur; TickRoll(0f);
        }

        void TickRoll(float dt)
        {
            rollT += dt;
            float t = rollT;
            if (rollOk)
            {
                float s;
                float toA = curL - .42f - .55f;
                if (t < k[0]) s = 0;
                else if (t < k[1]) s = (t - k[0]) / (k[1] - k[0]) * toA;
                else if (t < k[2]) s = toA + (t - k[1]) / .8f * 1.1f;
                else if (t < k[3]) s = toA + 1.1f + (t - k[2]) / (k[3] - k[2]) * toA;
                else s = 2f * toA + 1.1f;
                var bp = rollS + rollD * s + Vector3.up * .38f;
                if (t >= k[3]) { float dn = Mathf.Clamp01((t - k[3]) / .3f); bp = RailEnd(1, rollAngle) + Vector3.up * Mathf.Lerp(.42f, .15f, dn); if (dn >= 1f && Step(5)) { MgfSfx.Play("pop", .5f); rollBallGone = true; ball.gameObject.SetActive(false); } }
                if (!rollBallGone) { ball.position = bp; ball.Rotate(Vector3.Cross(Vector3.up, rollD), dt * 6.5f / .15f * Mathf.Rad2Deg * (t > k[1] && t < k[2] ? .2f : 1f), Space.World); }
                if (t >= k[1] && Step(0))
                {
                    // 접점 A: 모래꽃이 피고 직각 표식이 겹친다(한 점에서 만난다 = 반지름에 수직)
                    sandFlower.gameObject.SetActive(true); sandFlower.position = PointA() + Vector3.up * .02f; sandFlower.localScale = Vector3.one * .1f;
                    ShowRightAngle(rollAngle);
                    ShowReason(true);
                    MgfSfx.Play("correct", .45f);
                    MgfFx.Glow(PointA(), new Color(1f, .97f, .8f), 14, .5f);
                    if (practice) { propertyT = 0f; propertyBg.gameObject.SetActive(true); }
                }
                if (sandFlower.gameObject.activeSelf && t < k[5]) sandFlower.localScale = Vector3.one * Mathf.Min(1f, .1f + (t - k[1]) * 3.2f) * (1f + .12f * Mathf.Sin(Mathf.Clamp01((t - k[1]) * 3f) * Mathf.PI));
                // 두더지: A 로 뛰어가 꽃을 받고 깃발에 꽂는다
                var c = courses[courseIndex];
                var flagFoot = c.flag.position + new Vector3(.25f, 0, -.35f);
                var nearA = curA + (curO - curA).normalized * .45f;
                if (t >= k[4] && t < k[5]) { float u = (t - k[4]) / (k[5] - k[4]); MoleHop(moleHome, nearA, u); }
                else if (t >= k[5] && t < k[6]) { float u = (t - k[5]) / (k[6] - k[5]); MoleHop(nearA, flagFoot, u); sandFlower.position = mole.position + Vector3.up * 1.05f + mole.rotation * new Vector3(0, 0, -.3f); }
                if (t >= k[6] && Step(1))
                {
                    sandFlower.gameObject.SetActive(false);
                    if (rollCounted || practice) { c.flower.gameObject.SetActive(true); MgfFx.Punch(c.flower, .5f, .35f); }
                    else { c.tag.gameObject.SetActive(true); MgfFx.Punch(c.tag, .4f, .3f); }
                    if (courseIndex > 0) TightenString(courseIndex, rollCounted);
                    MgfSfx.Play("pop", .4f);
                }
                if (t >= k[6]) { float u = Mathf.Clamp01((t - k[6]) / .45f); mole.rotation = Quaternion.Euler(0, 180 + (st.streak >= 2 && rollCounted ? 360f * u : 0f), 0) ; }
            }
            else
            {
                if (t >= k[0] && t < k[1])
                {
                    float s = (t - k[0]) / (k[1] - k[0]) * tEntry;
                    ball.position = rollS + rollD * s + Vector3.up * .38f;
                    ball.Rotate(Vector3.Cross(Vector3.up, rollD), dt * 6.5f / .15f * Mathf.Rad2Deg, Space.World);
                }
                if (t >= k[1] && Step(0))
                {
                    var hit = rollS + rollD * (tEntry + .42f);
                    crater.gameObject.SetActive(true); crater.position = new Vector3(hit.x, .012f, hit.z); crater.localScale = new Vector3(.62f, .04f, .62f);
                    MgfFx.Burst(hit + Vector3.up * .1f, Sand, 22, 2.4f, .16f);
                    MgfFx.Shake(cam, .06f, .22f);
                    MgfSfx.Play("wrong", .4f);
                    sandHiss = 0f;
                }
                if (t >= k[1] && t < k[2])
                {
                    float u = (t - k[1]) / (k[2] - k[1]);
                    var hit = rollS + rollD * (tEntry + .42f);
                    ball.position = Vector3.Lerp(hit + Vector3.up * .38f, new Vector3(hit.x, .06f, hit.z), u * u);
                }
                if (t >= k[3] && Step(1))
                {
                    // 정답 상태를 잠깐 보여 준다: 반지름에 수직인 유령 레일 + 직각 표식 + 이유
                    ghostRail.gameObject.SetActive(true);
                    SetLine(ghostRail, RailEnd(-1, current.answer) + Vector3.up * .06f, RailEnd(1, current.answer) + Vector3.up * .06f);
                    ShowRightAngle(current.answer);
                    ShowReason(false);
                }
                if (t >= k[4] && Step(2))
                {
                    ghostRail.gameObject.SetActive(false); rightMark.gameObject.SetActive(false); revealBg.gameObject.SetActive(false);
                    ball.gameObject.SetActive(false);
                    if (!practice) MgfSfx.Play("pop", .25f);
                }
                if (t >= k[4] && t < k[5]) { float u = (t - k[4]) / (k[5] - k[4]); mole.rotation = Quaternion.Euler(0, 180 + 25f * Mathf.Sin(u * Mathf.PI * 2f), 0); }
                if (t >= k[5] && Step(3))
                {
                    // 같은 레일로 수리: 공을 시작점에 다시 올린다
                    ball.gameObject.SetActive(true); rollBallGone = false; PlaceBallAtStart(rollAngle);
                    FramePlan(false);
                }
            }
        }
        float sandHiss = 9f;

        void MoleHop(Vector3 a, Vector3 b, float u)
        {
            u = Mathf.Clamp01(u);
            var p = Vector3.Lerp(a, b, u); p.y = Mathf.Sin(u * Mathf.PI) * .55f;
            mole.position = p;
            var d = b - a; d.y = 0;
            if (d.sqrMagnitude > .001f) mole.rotation = Quaternion.LookRotation(-d.normalized, Vector3.up);
            moleDuck = 0;
        }

        void ShowRightAngle(int railAngle)
        {
            rightMark.gameObject.SetActive(true);
            var toO = (curO - curA).normalized; var d = Dir(railAngle);
            if (Vector3.Dot(d, Dir(st.theta + 90)) < 0) d = -d;
            float s = curR * .2f;
            var a0 = PointA() + Vector3.up * .02f;
            rightMark.SetPosition(0, a0 + toO * s);
            rightMark.SetPosition(1, a0 + toO * s + d * s);
            rightMark.SetPosition(2, a0 + d * s);
        }

        void ShowReason(bool ok)
        {
            revealBg.gameObject.SetActive(true);
            revealTxt.text = "OA ⊥ 레일 · " + HanKkeutRules.ReasonLine(current);
            revealShowT = 0f;
        }

        void CourseDoneVisual(bool repaired)
        {
            // 완성 레일을 지도에 남긴다(풀에서 재사용)
            Transform kept = null;
            foreach (var r in keptRails) if (!r.gameObject.activeSelf) { kept = r; break; }
            if (kept == null)
            {
                var go = new GameObject("KeptRail"); go.transform.SetParent(world, false);
                go.AddComponent<MeshFilter>(); var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = MgfLook.Lit(Rail, .72f);
                kept = go.transform; keptRails.Add(kept);
            }
            kept.gameObject.SetActive(true);
            kept.GetComponent<MeshFilter>().sharedMesh = railBarMf.sharedMesh;
            kept.position = curA + Vector3.up * .17f; kept.rotation = Quaternion.Euler(0, -rollAngle, 0);
            courses[courseIndex].rail = kept;
            crater.gameObject.SetActive(false); wrongTrace.gameObject.SetActive(false); rightMark.gameObject.SetActive(false);
            revealBg.gameObject.SetActive(false);
        }

        void RepairVisual()
        {
            FramePlan(false); SnapCameraNow();
            if (practice && current != null)
            {
                ghostRail.gameObject.SetActive(true);
                SetLine(ghostRail, RailEnd(-1, current.answer) + Vector3.up * .06f, RailEnd(1, current.answer) + Vector3.up * .06f);
            }
            readoutBg.gameObject.SetActive(true); readout.text = "레일 " + st.railAngle + "°";
            UpdateBanner();
        }

        void DropString(int i)
        {
            if (i <= 0 || i >= CourseCount) return;
            strings[i].gameObject.SetActive(true); stringTaut[i] = false; stringSag[i] = 2.2f; stringSagTarget[i] = 1.6f;
            strings[i].startColor = strings[i].endColor = new Color(.85f, .87f, .9f, .8f);
        }

        void TightenString(int i, bool firstTry)
        {
            if (i <= 0 || i >= CourseCount) return;
            strings[i].gameObject.SetActive(true);
            if (!stringTaut[i] && stringSag[i] <= 0f) stringSag[i] = 1.2f;
            stringTaut[i] = true; stringSagTarget[i] = 0f;
            strings[i].startColor = strings[i].endColor = firstTry ? new Color(1, 1, 1, .98f) : new Color(.78f, .8f, .84f, .9f);
            strings[i].widthMultiplier = firstTry ? .08f : .05f;
        }

        void TickStrings(float dt)
        {
            for (int i = 1; i < CourseCount; i++)
            {
                var lr = strings[i]; if (!lr.gameObject.activeSelf) continue;
                float target = stringSagTarget[i];
                // 「탁」: 목표를 살짝 지나쳤다 돌아오는 감쇠 진동
                stringSag[i] = Mathf.Lerp(stringSag[i], target, 1f - Mathf.Exp(-dt * 9f));
                var a = courses[i - 1].flag.position + Vector3.up * 1.62f; var b = courses[i].flag.position + Vector3.up * 1.62f;
                int n = lr.positionCount;
                for (int j = 0; j < n; j++)
                {
                    float u = j / (float)(n - 1);
                    var p = Vector3.Lerp(a, b, u); p.y -= stringSag[i] * 4f * u * (1 - u);
                    lr.SetPosition(j, p);
                }
            }
        }

        void ShowEndVisual(bool clear, string reason, bool newBest)
        {
            hudRt.gameObject.SetActive(false); dialRt.gameObject.SetActive(false); readoutBg.gameObject.SetActive(false); toastBg.gameObject.SetActive(false);
            revealBg.gameObject.SetActive(false); propertyBg.gameObject.SetActive(false);
            endRt.gameObject.SetActive(true); endT = 0f;
            endHead.text = clear ? UiStrings.ClearHead : UiStrings.OverHead;
            endWhy.text = clear ? "첫 제출 " + st.firstCorrect + "/8 · 통과 기준 7 달성" : reason == "time" ? UiStrings.WhyTime : reason == "balls" ? UiStrings.WhyBalls : UiStrings.WhyMastery;
            endReview.text = ReviewGoal();
            shownScore = 0;
            overviewFocus = courses[Mathf.Clamp(courseIndex, 0, CourseCount - 1)].o;
            FrameOverview();
            endBallT = 0f;
        }
        float endBallT;

        // ───────── 매 프레임 ─────────
        float clock;
        void TickView(float dt)
        {
            clock += dt;
            TickCamera(dt);
            // 커튼(Split Reveal: 깃발 천 두 장이 좌우로 열린다)
            curtain = Mathf.MoveTowards(curtain, curtainTarget, dt / .22f);
            if (flow == Flow.Opening && curtain >= 1f && flowClock > .3f && current == null) { }
            float halfW = CanvasSize.x * .5f + 4f;
            curtainL.sizeDelta = new Vector2(halfW * Ease(curtain), 0); curtainR.sizeDelta = new Vector2(halfW * Ease(curtain), 0);

            // 타이틀 천 펄럭임 + 공 맥동
            if (titleCloth && (view == View.Title || flow == Flow.Opening))
            {
                for (int i = 0; i < clothBase.Length; i++)
                {
                    var b = clothBase[i]; float w = b.x / 4.4f;
                    clothVerts[i] = new Vector3(b.x, b.y + Mathf.Sin(clock * 2.1f + b.x * 1.3f) * .05f * w, Mathf.Sin(clock * 3.0f + b.x * 1.7f + b.y * .6f) * .09f * w);
                }
                clothMesh.vertices = clothVerts; clothMesh.RecalculateNormals();
                float pr = .45f + .18f * ((clock * 1.3f) % 1f);
                var lr = titleRing.GetComponent<LineRenderer>();
                lr.startColor = lr.endColor = new Color(1, 1, 1, .9f * (1f - (clock * 1.3f) % 1f));
                SetCircle(lr, titleBall.position + Vector3.up * .03f, pr);
                float rb = Mathf.PingPong(clock * .55f, 1f);
                if (titleRollBall) titleRollBall.position = titleRailA + titleRailDir * Mathf.Lerp(-2.4f, 2.4f, Mathf.SmoothStep(0, 1, rb)) + Vector3.up * .38f;
                titleBall.GetChild(1).localPosition = new Vector3(0, .42f + Mathf.Abs(Mathf.Sin(clock * 3f)) * .06f, 0);
                var sp = ScreenOf(titleBall.position + new Vector3(0, -.15f, -.9f));
                ctaBg.rectTransform.anchoredPosition = ToCanvas(sp);
                float cs = 1f + .04f * Mathf.Sin(clock * 4f); ctaBg.rectTransform.localScale = new Vector3(cs, cs, 1);
            }

            // 두더지 대기 동작(갈퀴질) + 레일이 머리 위를 지나면 굴로 쏙
            if (moleBody)
            {
                bool idleMole = flow == Flow.Title || flow == Flow.Aim || flow == Flow.Draw || flow == Flow.Transition || flow == Flow.End;
                if (idleMole)
                {
                    moleBody.localRotation = Quaternion.Euler(6f * Mathf.Sin(clock * 2.4f), 10f * Mathf.Sin(clock * 1.2f), 0);
                    moleRake.localRotation = Quaternion.Euler(14f * Mathf.Sin(clock * 2.4f), 0, 0);
                    molePawL.localPosition = new Vector3(-.26f, .42f + .04f * Mathf.Sin(clock * 2.4f), -.24f);
                    molePawR.localPosition = new Vector3(.26f, .42f - .04f * Mathf.Sin(clock * 2.4f), -.24f);
                }
                if ((flow == Flow.Aim || flow == Flow.Draw) && current != null)
                {
                    // 두더지 위치와 레일 직선 사이 거리
                    var rel = mole.position - curA; var d = Dir(st.railAngle);
                    float along = rel.x * d.x + rel.z * d.z;
                    float perp = Mathf.Abs(rel.x * d.z - rel.z * d.x);
                    moleDuckTarget = (perp < .55f && Mathf.Abs(along) < curL + .3f) ? 1f : 0f;
                    mole.position = new Vector3(moleHome.x, -moleDuck * .95f, moleHome.z);
                    mole.rotation = Quaternion.Euler(0, 180, 0);
                }
                else if (flow != Flow.Rolling) moleDuckTarget = 0f;
                moleDuck = Mathf.MoveTowards(moleDuck, moleDuckTarget, dt * 5f);
            }
            for (int i = 0; i < idleMoles.Count; i++)
            {
                var m = idleMoles[i]; float ph = clock * 2f + i * 1.7f;
                m.GetChild(0).localRotation = Quaternion.Euler(7f * Mathf.Sin(ph), 12f * Mathf.Sin(ph * .5f), 0);
                m.position = idleMoleHome[i] + Vector3.up * Mathf.Max(0, Mathf.Sin(ph * .35f)) * .08f;
            }

            if (flow == Flow.Rolling) TickRoll(dt);
            TickStrings(dt);
            TickGuide(dt);
            TickHud(dt);

            // 손잡이 탭 경로 시범(판정 없음): 반투명 공이 레일 위를 한 번 지나간다
            if (pathDemoT < 1.1f && flow == Flow.Aim && !rollBallGone)
            {
                pathDemoT += dt;
                float u = Mathf.Clamp01(pathDemoT / 1.1f);
                var d = Dir(st.railAngle);
                ball.position = curA - d * (curL - .42f) * (1 - 2 * u) + Vector3.up * .38f;
                if (u >= 1f) PlaceBallAtStart(st.railAngle);
            }
            // 거절: 손잡이 좌우 흔들림(화면 흔들림 아님)
            if (refuseShakeT < .35f)
            {
                refuseShakeT += dt;
                float s = Mathf.Sin(refuseShakeT * 60f) * (1f - refuseShakeT / .35f) * .08f;
                gripFar.localPosition = new Vector3(curL, .12f, s); gripNear.localPosition = new Vector3(-curL, .17f, -s);
            }
            else if (railRoot.gameObject.activeSelf) { gripFar.localPosition = new Vector3(curL, .12f, 0); gripNear.localPosition = new Vector3(-curL, .17f, 0); }

            // 3단계 반지름 그리기 전: 레일은 잠김(반투명 느낌으로 살짝 내려앉음)
            if (railRoot.gameObject.activeSelf)
            {
                railBar.localPosition = new Vector3(0, flow == Flow.Draw ? .1f : .17f, 0);
                var want = flow == Flow.Draw ? railLockedMat : railMat;
                if (railBarR.sharedMaterial != want) railBarR.sharedMaterial = want;
            }

            // 결과 화면 연출(카운트업) + 장식 리플레이 공
            if (flow == Flow.End)
            {
                endT += dt;
                float s = endT < .26f ? Mathf.Lerp(.9f, 1.04f, endT / .26f) : Mathf.Lerp(1.04f, 1f, Mathf.Clamp01((endT - .26f) / .2f));
                endRt.localScale = Vector3.one * s * (Land ? .9f : 1f);
                float u = Mathf.Clamp01(endT / 1.1f);
                endCourses.text = "코스 완성 " + Mathf.RoundToInt(st.solved * u) + "/8";
                endFirst.text = "첫 제출 " + Mathf.RoundToInt(st.firstCorrect * u) + "/8 · 통과 7";
                shownScore = Mathf.Lerp(0, st.score, Ease(u));
                endScore.text = Mathf.RoundToInt(shownScore) + "점";
                endHint.alpha = endClock >= .8f ? .6f + .4f * Mathf.Sin(clock * 4f) : .25f;
                // 이어진 깃발 끈을 따라 달리는 장식 공(실제 도형 주장이 아니다)
                endBallT += dt * .35f;
                int maxSeg = 0; for (int i = 1; i < CourseCount; i++) if (strings[i].gameObject.activeSelf && stringTaut[i]) maxSeg = i;
                if (maxSeg > 0)
                {
                    float pos = (endBallT % 1f) * maxSeg; int seg = Mathf.Clamp(Mathf.FloorToInt(pos), 0, maxSeg - 1); float f = pos - seg;
                    ball.gameObject.SetActive(true);
                    ball.position = Vector3.Lerp(courses[seg].flag.position, courses[seg + 1].flag.position, f) + Vector3.up * 1.8f;
                }
                int last = Mathf.Clamp(Mathf.Max(1, courseIndex), 1, CourseCount - 1);
                float pan = Mathf.PingPong(clock * .05f, 1f) * last;
                int pi = Mathf.Clamp(Mathf.FloorToInt(pan), 0, last - 1);
                overviewFocus = Vector3.Lerp(courses[pi].o, courses[pi + 1].o, pan - pi);
                FrameOverview();
            }
        }

        static float Ease(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }

        void TickGuide(float dt)
        {
            guidePhase += dt; boost = Mathf.Max(0, boost - dt * .35f);
            for (int i = 0; i < ripples.Length; i++)
            {
                if (rippleT[i] > 1f) { if (ripples[i].color.a > 0) ripples[i].color = new Color(1, 1, 1, 0); continue; }
                rippleT[i] += dt / .25f;
                float u = Mathf.Clamp01(rippleT[i]);
                ripples[i].rectTransform.sizeDelta = Vector2.one * (24 + 70 * u);
                ripples[i].color = new Color(1, 1, 1, .9f * (1 - u));
            }
            bool aim = flow == Flow.Aim && current != null && grab == Grab.None;
            bool draw = flow == Flow.Draw && current != null && grab == Grab.None;
            bool showHand = (aim && (practice || boost > 0)) || (draw && (st.bridge || boost > 0));
            guideRing.gameObject.SetActive(aim || draw);
            guideHand.gameObject.SetActive(showHand);
            guideGhost.gameObject.SetActive(aim && practice);
            if (!(aim || draw)) { for (int i = 0; i < guideDots.Length; i++) guideDots[i].gameObject.SetActive(false); }
            if (aim)
            {
                var h = ScreenOf(RailEnd(1, st.railAngle) + Vector3.up * .2f);
                float pulse = (guidePhase * 1.1f) % 1f;
                guideRing.rectTransform.anchoredPosition = ToCanvas(h);
                guideRing.rectTransform.sizeDelta = Vector2.one * ((36 + 28 * pulse) * (1f + boost * .5f));
                guideRing.color = new Color(1, 1, 1, (.8f - .7f * pulse));
                // 유령 손: 시작점 → 원호 → 도착 유령 손잡이(연습) / 원호 일부(실전, 정답과 무관한 방향)
                int target = practice ? current.answer : st.railAngle + 40;
                int start = st.railAngle;
                int delta = practice ? ShortDelta(start, target) : 40;
                for (int i = 0; i < guideDots.Length; i++)
                {
                    guideDots[i].gameObject.SetActive(showHand);
                    float a = start + delta * (i + 1) / (float)(guideDots.Length + 1);
                    guideDots[i].rectTransform.anchoredPosition = ToCanvas(ScreenOf(curA + Dir(a) * curL + Vector3.up * .2f));
                }
                if (showHand)
                {
                    float u = (guidePhase / 1.2f) % 1f;
                    float a = start + delta * Ease(u);
                    guideHand.rectTransform.anchoredPosition = ToCanvas(ScreenOf(curA + Dir(a) * curL + Vector3.up * .2f)) + new Vector2(14, -16);
                    float hs = 1f + boost * .5f; guideHand.rectTransform.localScale = new Vector3(hs, hs, 1);
                }
                if (practice) guideGhost.rectTransform.anchoredPosition = ToCanvas(ScreenOf(curA + Dir(current.answer) * curL + Vector3.up * .2f));
                PositionReadout(h, 46);
            }
            else if (draw)
            {
                // 3단계 연결 장면: A 와 B 를 번갈아 가리킨 뒤 O→A 경로
                bool alt = current.band == 3 && st.bridge && ((int)(guidePhase / .9f)) % 3 == 1;
                var target = alt ? PointB() : PointO();
                float pulse = (guidePhase * 1.1f) % 1f;
                guideRing.rectTransform.anchoredPosition = ToCanvas(ScreenOf(target));
                guideRing.rectTransform.sizeDelta = Vector2.one * ((30 + 26 * pulse) * (1f + boost * .5f));
                guideRing.color = alt ? new Color(.55f, .6f, .7f, .9f - .7f * pulse) : new Color(1, 1, 1, .85f - .75f * pulse);
                var o = ScreenOf(PointO()); var a = ScreenOf(PointA());
                for (int i = 0; i < guideDots.Length; i++)
                {
                    guideDots[i].gameObject.SetActive(showHand);
                    guideDots[i].rectTransform.anchoredPosition = ToCanvas(Vector2.Lerp(o, a, (i + 1) / (float)(guideDots.Length + 1)));
                }
                if (showHand)
                {
                    float u = Ease((guidePhase / 1.2f) % 1f);
                    guideHand.rectTransform.anchoredPosition = ToCanvas(Vector2.Lerp(o, a, u)) + new Vector2(14, -16);
                }
            }
            if (aim && grab == Grab.None && !readoutBg.gameObject.activeSelf) readoutBg.gameObject.SetActive(true);
            if (grab == Grab.Rail || grab == Grab.Dial)
                PositionReadout(ScreenOf(RailEnd(1, st.railAngle) + Vector3.up * .2f), 52);
        }

        static int ShortDelta(int from, int to)
        {
            int d = HanKkeutRules.Norm180(to - from);
            return d > 90 ? d - 180 : d;
        }

        void TickHud(float dt)
        {
            if (toastT < 2.2f)
            {
                toastT += dt;
                float s = toastT < .2f ? Mathf.Lerp(.92f, 1.03f, toastT / .2f) : Mathf.Lerp(1.03f, 1f, Mathf.Clamp01((toastT - .2f) / .15f));
                toastBg.rectTransform.localScale = Vector3.one * s;
                if (toastT >= 2.2f) toastBg.gameObject.SetActive(false);
            }
            if (revealBg.gameObject.activeSelf)
            {
                revealShowT += dt;
                // 실제 도형(A) 바로 옆에 이유 식
                revealBg.rectTransform.anchoredPosition = ToCanvas(ScreenOf(PointA())) - CanvasSize * .5f + new Vector2(0, 58);
                float s = revealShowT < .26f ? Mathf.Lerp(.92f, 1.04f, revealShowT / .26f) : Mathf.Lerp(1.04f, 1f, Mathf.Clamp01((revealShowT - .26f) / .2f));
                revealBg.rectTransform.localScale = Vector3.one * s;
            }
            if (propertyBg.gameObject.activeSelf)
            {
                propertyT += dt;
                float s = propertyT < .26f ? Mathf.Lerp(.92f, 1.04f, propertyT / .26f) : Mathf.Lerp(1.04f, 1f, Mathf.Clamp01((propertyT - .26f) / .2f));
                propertyBg.rectTransform.localScale = Vector3.one * s;
                if (propertyT > 2.6f) propertyBg.gameObject.SetActive(false);
            }
            if (!hudRt.gameObject.activeSelf) return;
            shownScore = Mathf.MoveTowards(shownScore, st.score, dt * 400f);
            int secs = Mathf.CeilToInt(timeLeft);
            int key = (practice ? 1 : 0) + st.course * 3 + st.firstCorrect * 41 + st.firstAttempts * 389 + st.lives * 4001 + secs * 17011 + (st.bridge ? 7 : 0);
            if (key != lastHudKey)
            {
                lastHudKey = key;
                hudCourse.text = practice ? "연습" : "코스 " + st.course + "/8";
                hudFirst.text = practice ? "기록 없음" : "첫 제출 " + st.firstCorrect + "/" + st.firstAttempts;
                hudTime.text = practice || st.bridge ? UiStrings.Paused : secs + "초";
                for (int i = 0; i < 3; i++)
                {
                    bool has = i < st.lives;
                    ballIcons[i].color = has ? FeltDeep : new Color(FeltDeep.r, FeltDeep.g, FeltDeep.b, .12f);
                }
            }
            timeFill.rectTransform.sizeDelta = new Vector2(260f * Mathf.Clamp01(timeLeft / HanKkeutRules.SessionSeconds), 8);
            timeFill.color = practice || st.bridge ? new Color(.7f, .75f, .82f) : timeLeft < 15f ? Coral : Rail;
        }
    }
}
