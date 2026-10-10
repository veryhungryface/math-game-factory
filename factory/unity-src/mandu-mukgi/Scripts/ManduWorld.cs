// 만두 묶기 — 2.5D 세계: 비 내리는 새벽 시장의 코발트 차양, 스테인리스 컨베이어, 증기 레인, 노점 까치, 찜통.
// 배경 일러스트 없이 전부 코드 메시·조명으로 만든다(background_policy). 정사영 55° 쿼터뷰.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.ManduMukgi
{
    public partial class ManduMukgiGame
    {
        Camera cam;
        int cap = 1;
        bool land;
        int lastW, lastH;
        float orthoBase = 6f, camPunch, shakeT;
        Vector3 camBasePos;
        float hitStop;
        readonly bool[] sealDemo = new bool[2];
        readonly bool[] sealPractice = new bool[2];
        readonly List<Vector2>[] sealTrail = { new List<Vector2>(), new List<Vector2>() };
        readonly float[] liftT = new float[2], wiggleT = new float[2], stubT = new float[2];
        readonly int[] stubFrom = new int[2];
        readonly float[] trayScale = { 1f, 1f };
        float pxPerUnit = 40f;
        const float CamPitch = 52f;

        Transform worldRoot, canopyRoot, stationRoot, supplyRoot, beltRoot, titleTilesRoot, tilesRoot, propsL, propsR;
        TrayMats tm;
        Material mCobalt, mCobaltLight, mSteel, mSteelDark, mInk, mFloor, mDough, mGold, mCoral, mMagenta, mGlass, mCrate, mPorcelain, mRubber, mBulb, mWhite, mBlack, mBlackSheen, mApron, mTong, mBeak, mFill, mRing;

        // 까치
        Transform magpie, mBody, mHead, mWingR, mWingL, mTail, mEyeL, mEyeR, mTongA, mTongB, mTongTip, mLegL, mLegR;
        float poseCatch, poseOops, poseClamp, poseLever, blinkT = 2f, magpieT;
        // 찜통·접시·봉인·회수컵
        Transform steamer, lid, cup, cupFill, numberTray, lever;
        readonly Transform[] slotDumplings = new Transform[12];
        readonly Transform[] wowDumplings = new Transform[3];
        float wowT = -1f, cupLevel;
        readonly Transform[] sealDisc = new Transform[3];
        readonly Transform[] sealHalfA = new Transform[3], sealHalfB = new Transform[3];
        readonly float[] sealBreakT = { -1f, -1f, -1f };
        // 주문 패
        readonly Transform[] tilePivot = new Transform[12];
        readonly float[] tileFlip = new float[12];
        readonly bool[] tileOn = new bool[12];
        readonly Transform[] titlePivot = new Transform[4];
        float titleT, titleRise;
        // 컨베이어 판자
        readonly List<Transform> slats = new List<Transform>();
        float beltOffset, beltLen = 30f;
        // 빗방울·물결·증기
        readonly Transform[] drips = new Transform[16];
        readonly float[] dripY = new float[16];
        readonly Transform[] ripples = new Transform[10];
        readonly float[] rippleT = new float[10];
        readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        int rippleNext;
        float rippleClock;
        float steamT;
        // 날아가는 만두·속 방울
        sealed class Flyer { public Transform t; public Mesh mesh; public float time = -1f; public Vector3 p0, p1, p2, p3; public int slot; public float scale0; public bool caught; }
        readonly Flyer[] flyers = new Flyer[3];
        sealed class Blob { public Transform t; public float time = -1f; public Vector3 p0, p1; }
        readonly Blob[] blobs = new Blob[3];
        Light lampL, lampR, steamLight;

        Vector3 ViewGround(float u, float v, float h)
        {
            var p = cam.ViewportToWorldPoint(new Vector3(u, v, 40f));
            var f = cam.transform.forward;
            float t = (h - p.y) / f.y;
            return p + f * t;
        }

        static Material L(string hex, float sm = 0.3f, float me = 0f, string em = null)
        {
            return MgfLook.Lit(MgfLook.Hex(hex), sm, me, em == null ? (Color?)null : MgfLook.Hex(em));
        }

        GameObject B(string n, Vector3 p, Vector3 s, float r, Material m, Transform parent, bool shadows = true)
        {
            var o = MgfLook.Block(n, p, s, r, m, parent);
            Destroy(o.GetComponent<Collider>());
            var rr = o.GetComponent<Renderer>();
            if (!shadows) { rr.shadowCastingMode = ShadowCastingMode.Off; }
            return o;
        }

        GameObject P(PrimitiveType type, string n, Vector3 p, Vector3 s, Material m, Transform parent, bool shadows = true)
        {
            var o = MgfLook.Prim(type, n, p, s, m, parent, false);
            if (!shadows) o.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return o;
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("0E2440"), MgfLook.Hex("2B5F92"), MgfLook.Hex("141A22"), 1.05f);
            var sun = MgfLook.Sun(new Vector3(58f, 24f, 0f), MgfLook.Hex("D6E6FF"), 1.0f, 0.55f);
            sun.shadowBias = 0.05f;
            cam = MgfLook.Camera(Vector3.zero, Vector3.forward, 30f);
            cam.orthographic = true;
            cam.transform.rotation = Quaternion.Euler(CamPitch, 0f, 0f);
            cam.transform.position = -cam.transform.forward * 70f;
            cam.nearClipPlane = 1f; cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = MgfLook.Hex("141A22");
            camBasePos = cam.transform.position;

            mCobalt = L("164A7A", 0.82f);
            mCobaltLight = L("1F5E99", 0.82f);
            mSteel = L("C9D6DE", 0.78f, 0.75f);
            mSteelDark = L("6E7C88", 0.6f, 0.7f);
            mInk = L("20242B", 0.4f);
            mFloor = MgfLook.Lit(Color.white, 0.74f, 0.1f, null, FloorTex());
            mDough = MgfLook.Lit(MgfLook.Hex("FFEBCB"), 0.2f, 0f, MgfLook.Hex("4A3A26"), FlourTex());
            mGold = L("F2C14E", 0.5f, 0.2f, "3A2A00");
            mCoral = L("FF5C7A", 0.5f, 0f, "2A0710");
            mMagenta = L("FF3D8E", 0.4f, 0f, "8A1448");
            mGlass = MgfLook.Alpha(new Color(0.88f, 0.94f, 0.99f, 0.38f));
            mCrate = L("2B5FA8", 0.45f);
            mPorcelain = L("F4F1EA", 0.85f, 0f, "151515");
            mRubber = L("1C2026", 0.35f);
            mBulb = L("FFE2A0", 0.2f, 0f, "FFC66A");
            mWhite = L("F2F4F6", 0.45f);
            mBlack = L("14161B", 0.55f);
            mBlackSheen = L("13203D", 0.75f, 0.2f, "081530");
            mApron = L("1E5BB8", 0.7f);
            mTong = L("FF6B6B", 0.55f, 0.2f);
            mBeak = L("2A2E35", 0.6f);
            mFill = L("E88FA0", 0.5f);
            mRing = MgfLook.Alpha(Color.white, MgfLook.SoftDot);

            tm = new TrayMats
            {
                plate = L("8C9DA9", 0.38f, 0.55f),
                plateEdge = L("9AA9B4", 0.6f, 0.8f),
                dough = mDough,
                overlay = MgfLook.Alpha(Color.white),
                ticket = L("164A7A", 0.6f, 0f, "05192E"),
                ticketOk = L("F2C14E", 0.5f, 0f, "4A3500"),
                ticketBad = L("FF5C7A", 0.5f, 0f, "3A0A14"),
                gaugeBack = L("2A3038", 0.5f),
                post = L("8F9CA6", 0.7f, 0.8f),
                bolt = L("AAB6BE", 0.8f, 0.9f)
            };

            worldRoot = new GameObject("World").transform;

            // 젖은 바닥(빛 반사) — 큰 평면 1장
            var floor = new GameObject("Floor");
            floor.transform.SetParent(worldRoot, false);
            floor.AddComponent<MeshFilter>().sharedMesh = TiledPlane(90f, 26f);
            var fr = floor.AddComponent<MeshRenderer>(); fr.sharedMaterial = mFloor; fr.receiveShadows = true; fr.shadowCastingMode = ShadowCastingMode.Off;
            // 빗물 홈(코발트 줄)
            for (int i = -3; i <= 3; i++) B("Gutter" + i, new Vector3(0, 0.01f, i * 7.5f + 2f), new Vector3(90f, 0.04f, 0.28f), 0.02f, mCobalt, worldRoot, false);

            BuildCanopy();
            BuildBelt();
            BuildStation();
            BuildSupply();
            BuildProps();
            BuildAmbient();

            foreach (var t in trays) t.Build(worldRoot, tm);
            for (int i = 0; i < flyers.Length; i++)
            {
                var f = new Flyer { mesh = new Mesh { name = "Flyer" } };
                var go = new GameObject("Flyer" + i);
                go.AddComponent<MeshFilter>().sharedMesh = f.mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mDough;
                f.t = go.transform; go.SetActive(false);
                flyers[i] = f;
            }
            for (int i = 0; i < blobs.Length; i++)
            {
                var b = new Blob { t = P(PrimitiveType.Sphere, "Blob" + i, Vector3.zero, Vector3.one * 0.32f, mFill, worldRoot).transform };
                b.t.gameObject.SetActive(false); blobs[i] = b;
            }
            ApplyLayout();
        }

        // ───────────────────────── 환경 구성 ─────────────────────────

        void BuildCanopy()
        {
            canopyRoot = new GameObject("Canopy").transform;
            canopyRoot.SetParent(worldRoot, false);
            // 비스듬한 방수 차양(코발트 두 톤 줄무늬). 로컬 원점 = 앞 가장자리(수술 위).
            var slope = new GameObject("Slope").transform; slope.SetParent(canopyRoot, false);
            slope.localRotation = Quaternion.Euler(-14f, 0, 0);
            for (int i = -14; i <= 14; i++)
                B("Stripe" + i, new Vector3(i * 1.05f, 0.0f, 3.2f), new Vector3(1.06f, 0.12f, 6.6f), 0.04f, (i & 1) == 0 ? mCobalt : mCobaltLight, slope);
            // 앞 수술(부채꼴 반원)
            for (int i = -15; i <= 15; i++)
            {
                var sc = P(PrimitiveType.Cylinder, "Scallop" + i, new Vector3(i * 1.0f, -0.12f, -0.02f), new Vector3(0.98f, 0.05f, 0.98f), (i & 1) == 0 ? mCobaltLight : mCobalt, canopyRoot);
                sc.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            }
            B("Valance", new Vector3(0, 0.12f, 0.05f), new Vector3(32f, 0.42f, 0.16f), 0.06f, mCobalt, canopyRoot);
            B("Rail", new Vector3(0, -0.5f, -0.25f), new Vector3(32f, 0.1f, 0.1f), 0.04f, mSteel, canopyRoot);
            // 차양 아래 걸린 전구 2개(따뜻한 점광원)
            for (int s = -1; s <= 1; s += 2)
            {
                var bulb = P(PrimitiveType.Sphere, "Bulb", new Vector3(s * 4.75f, -0.62f, -0.5f), new Vector3(0.3f, 0.36f, 0.3f), mBulb, canopyRoot, false);
                P(PrimitiveType.Cylinder, "Cord", new Vector3(s * 4.75f, -0.25f, -0.5f), new Vector3(0.04f, 0.25f, 0.04f), mInk, canopyRoot, false);
                P(PrimitiveType.Cylinder, "Shade", new Vector3(s * 4.75f, -0.45f, -0.5f), new Vector3(0.52f, 0.07f, 0.52f), mSteelDark, canopyRoot, false);
                var l = new GameObject("Lamp").AddComponent<Light>();
                l.type = LightType.Point; l.color = MgfLook.Hex("FFC77A"); l.intensity = 1.1f; l.range = 8f; l.shadows = LightShadows.None;
                l.transform.SetParent(bulb.transform, false); l.transform.localPosition = new Vector3(0, -0.6f, 0);
                if (s < 0) lampL = l; else lampR = l;
            }

            // 진행 주문 패 12장(뒤집히면 노랑)
            tilesRoot = new GameObject("OrderTiles").transform; tilesRoot.SetParent(canopyRoot, false);
            for (int i = 0; i < 12; i++)
            {
                var pv = new GameObject("Tile" + i).transform; pv.SetParent(tilesRoot, false);
                var front = B("Front", new Vector3(0, -0.36f, 0), new Vector3(0.6f, 0.66f, 0.07f), 0.07f, tm.plate, pv, false);
                var t1 = MgfText.World((i + 1).ToString(), new Vector3(0, 0, -0.05f), 3.2f, MgfLook.Hex("20242B"), front.transform);
                t1.fontStyle = FontStyles.Bold;
                var back = B("Back", new Vector3(0, -0.36f, 0.075f), new Vector3(0.6f, 0.66f, 0.07f), 0.07f, mGold, pv, false);
                var t2 = MgfText.World((i + 1).ToString(), new Vector3(0, 0, 0.05f), 3.4f, MgfLook.Hex("20242B"), back.transform);
                t2.fontStyle = FontStyles.Bold;
                t2.transform.localRotation = Quaternion.Euler(180f, 0, 0); // X 축 180° 뒤집기 뒤에 바로 읽힌다
                // 줄
                P(PrimitiveType.Cylinder, "String", new Vector3(0, 0.02f, 0.03f), new Vector3(0.02f, 0.06f, 0.02f), mInk, pv, false);
                tilePivot[i] = pv;
            }

            // 타이틀 주문 패 4장(만·두·묶·기)
            titleTilesRoot = new GameObject("TitleTiles").transform; titleTilesRoot.SetParent(canopyRoot, false);
            string[] letters = { "만", "두", "묶", "기" };
            for (int i = 0; i < 4; i++)
            {
                var pv = new GameObject("TT" + i).transform; pv.SetParent(titleTilesRoot, false);
                var front = B("Front", new Vector3(0, -1.0f, 0), new Vector3(2.0f, 2.05f, 0.18f), 0.22f, MgfLook.Lit(MgfLook.Hex("FFF6E4"), 0.4f, 0f, MgfLook.Hex("2A2010")), pv);
                var rim = B("Rim", new Vector3(0, -1.0f, 0.05f), new Vector3(2.16f, 2.21f, 0.12f), 0.24f, i == 2 ? mCoral : mGold, pv);
                var t = MgfText.World(letters[i], new Vector3(0, -0.04f, -0.12f), 15f, MgfLook.Hex("164A7A"), front.transform);
                t.fontStyle = FontStyles.Bold;
                t.outlineWidth = 0.18f; t.outlineColor = new Color32(0x0B, 0x22, 0x3C, 255);
                var back = B("Back", new Vector3(0, -1.0f, 0.16f), new Vector3(2.0f, 2.05f, 0.1f), 0.2f, mCobaltLight, pv);
                P(PrimitiveType.Cylinder, "StringL", new Vector3(-0.6f, 0.15f, 0.02f), new Vector3(0.035f, 0.18f, 0.035f), mInk, pv, false);
                P(PrimitiveType.Cylinder, "StringR", new Vector3(0.6f, 0.15f, 0.02f), new Vector3(0.035f, 0.18f, 0.035f), mInk, pv, false);
                titlePivot[i] = pv;
            }
        }

        void BuildBelt()
        {
            beltRoot = new GameObject("Belt").transform; beltRoot.SetParent(worldRoot, false);
            B("RailF", new Vector3(0, 0.42f, -0.78f), new Vector3(beltLen, 0.22f, 0.14f), 0.06f, mSteel, beltRoot);
            B("RailB", new Vector3(0, 0.42f, 0.78f), new Vector3(beltLen, 0.22f, 0.14f), 0.06f, mSteel, beltRoot);
            B("BeltBed", new Vector3(0, 0.3f, 0), new Vector3(beltLen, 0.16f, 1.44f), 0.05f, mRubber, beltRoot);
            for (int i = -14; i <= 14; i += 2)
            {
                B("Leg", new Vector3(i, 0.15f, -0.7f), new Vector3(0.12f, 0.3f, 0.12f), 0.03f, mSteelDark, beltRoot);
                B("Leg", new Vector3(i, 0.15f, 0.7f), new Vector3(0.12f, 0.3f, 0.12f), 0.03f, mSteelDark, beltRoot);
            }
            for (int i = 0; i < 30; i++)
            {
                var s = B("Slat", Vector3.zero, new Vector3(0.36f, 0.05f, 1.36f), 0.02f, mSteelDark, beltRoot, false);
                slats.Add(s.transform);
            }
            // 증기 레인: 젖빛 유리 아래 자홍 열판
            var lane = new GameObject("SteamLane").transform; lane.SetParent(beltRoot, false); lane.localPosition = new Vector3(-2.2f, 0, 0);
            B("Heat", new Vector3(0, 0.42f, 0), new Vector3(3.4f, 0.03f, 0.9f), 0.02f, mMagenta, lane, false);
            // 젖빛 유리관(가로 캡슐) + 양끝 금속 고리
            var glass = P(PrimitiveType.Capsule, "GlassTube", new Vector3(0, 0.95f, 0), new Vector3(1.1f, 2.1f, 1.1f), mGlass, lane, false);
            glass.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            glass.GetComponent<MeshFilter>().sharedMesh = WhiteColored(glass.GetComponent<MeshFilter>().sharedMesh);
            for (int e = -1; e <= 1; e += 2)
            {
                var ring = P(PrimitiveType.Cylinder, "TubeRing", new Vector3(e * 1.55f, 0.95f, 0), new Vector3(1.18f, 0.06f, 1.18f), mSteel, lane, false);
                ring.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            }
            steamLight = new GameObject("SteamLight").AddComponent<Light>();
            steamLight.type = LightType.Point; steamLight.color = MgfLook.Hex("FF4FA0"); steamLight.intensity = 1.4f; steamLight.range = 5f;
            steamLight.transform.SetParent(lane, false); steamLight.transform.localPosition = new Vector3(0, 1.2f, -0.5f);
        }

        void BuildStation()
        {
            stationRoot = new GameObject("Station").transform; stationRoot.SetParent(worldRoot, false);
            // 찜통(스테인리스 2단 + 뚜껑)
            steamer = new GameObject("Steamer").transform; steamer.SetParent(stationRoot, false); steamer.localPosition = new Vector3(-1.55f, 0, 0.1f);
            P(PrimitiveType.Cylinder, "Tier1", new Vector3(0, 0.32f, 0), new Vector3(1.5f, 0.32f, 1.5f), mSteel, steamer);
            P(PrimitiveType.Cylinder, "Band1", new Vector3(0, 0.62f, 0), new Vector3(1.56f, 0.04f, 1.56f), mSteelDark, steamer);
            P(PrimitiveType.Cylinder, "Tier2", new Vector3(0, 0.9f, 0), new Vector3(1.46f, 0.26f, 1.46f), mSteel, steamer);
            lid = new GameObject("Lid").transform; lid.SetParent(steamer, false); lid.localPosition = new Vector3(0, 1.18f, 0);
            P(PrimitiveType.Sphere, "Dome", new Vector3(0, 0.02f, 0), new Vector3(1.5f, 0.36f, 1.5f), mSteel, lid);
            P(PrimitiveType.Sphere, "Knob", new Vector3(0, 0.2f, 0), new Vector3(0.22f, 0.14f, 0.22f), mInk, lid);
            for (int i = 0; i < 3; i++)
            {
                var d = BuildDumplingMini("Wow" + i, steamer);
                d.localPosition = new Vector3(-0.42f + i * 0.42f, 1.05f, 0f);
                d.gameObject.SetActive(false);
                wowDumplings[i] = d;
            }
            // 봉인 3개(찜통 앞의 도자기 원판)
            for (int i = 0; i < 3; i++)
            {
                var root = new GameObject("Seal" + i).transform; root.SetParent(steamer, false);
                root.localPosition = new Vector3(-0.42f + i * 0.42f, 0.42f, -0.78f);
                root.localRotation = Quaternion.Euler(-10f, 0, 0);
                var a = P(PrimitiveType.Cylinder, "HalfA", new Vector3(-0.08f, 0, 0), new Vector3(0.17f, 0.03f, 0.34f), mPorcelain, root, false);
                a.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                var b = P(PrimitiveType.Cylinder, "HalfB", new Vector3(0.08f, 0, 0), new Vector3(0.17f, 0.03f, 0.34f), mPorcelain, root, false);
                b.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                var stamp = P(PrimitiveType.Cylinder, "Stamp", new Vector3(0, 0, -0.035f), new Vector3(0.2f, 0.01f, 0.2f), mCoral, root, false);
                stamp.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                stamp.transform.SetParent(a.transform, true);
                sealDisc[i] = root; sealHalfA[i] = a.transform; sealHalfB[i] = b.transform;
            }
            // 번호 트레이(완성 만두 12칸)
            numberTray = new GameObject("NumberTray").transform; numberTray.SetParent(stationRoot, false); numberTray.localPosition = new Vector3(0.1f, 0, -1.3f);
            B("TrayBody", new Vector3(0, 0.12f, 0), new Vector3(2.5f, 0.2f, 0.95f), 0.08f, mSteel, numberTray);
            for (int i = 0; i < 12; i++)
            {
                var d = BuildDumplingMini("Slot" + i, numberTray);
                d.localPosition = new Vector3(-1.05f + (i % 6) * 0.42f, 0.28f, (i < 6 ? 0.2f : -0.2f));
                d.localScale = Vector3.one * 0.62f;
                d.gameObject.SetActive(false);
                slotDumplings[i] = d;
            }
            // 투명 회수컵
            cup = new GameObject("Cup").transform; cup.SetParent(stationRoot, false); cup.localPosition = new Vector3(1.75f, 0, -0.9f);
            var cg = P(PrimitiveType.Cylinder, "CupGlass", new Vector3(0, 0.38f, 0), new Vector3(0.62f, 0.38f, 0.62f), mGlass, cup, false);
            cg.GetComponent<MeshFilter>().sharedMesh = WhiteColored(cg.GetComponent<MeshFilter>().sharedMesh);
            cupFill = P(PrimitiveType.Cylinder, "CupFill", new Vector3(0, 0.05f, 0), new Vector3(0.52f, 0.02f, 0.52f), mFill, cup, false).transform;
            BuildMagpie();
        }

        Transform BuildDumplingMini(string n, Transform parent)
        {
            var root = new GameObject(n).transform; root.SetParent(parent, false);
            var body = P(PrimitiveType.Sphere, "Body", new Vector3(0, 0.08f, 0), new Vector3(0.38f, 0.22f, 0.3f), mDough, root);
            var crest = P(PrimitiveType.Capsule, "Crimp", new Vector3(0, 0.19f, 0), new Vector3(0.07f, 0.17f, 0.07f), mGold, root, false);
            crest.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            return root;
        }

        void BuildMagpie()
        {
            magpie = new GameObject("Magpie").transform; magpie.SetParent(stationRoot, false);
            magpie.localPosition = new Vector3(0.85f, 0, 0.1f);
            magpie.localRotation = Quaternion.Euler(0, -18f, 0);
            magpie.localScale = Vector3.one * 1.25f;
            mLegL = P(PrimitiveType.Cylinder, "LegL", new Vector3(-0.17f, 0.22f, 0), new Vector3(0.07f, 0.22f, 0.07f), mInk, magpie).transform;
            mLegR = P(PrimitiveType.Cylinder, "LegR", new Vector3(0.17f, 0.22f, 0), new Vector3(0.07f, 0.22f, 0.07f), mInk, magpie).transform;
            B("FootL", new Vector3(-0.17f, 0.03f, -0.08f), new Vector3(0.2f, 0.05f, 0.26f), 0.02f, mInk, magpie);
            B("FootR", new Vector3(0.17f, 0.03f, -0.08f), new Vector3(0.2f, 0.05f, 0.26f), 0.02f, mInk, magpie);
            mBody = new GameObject("Body").transform; mBody.SetParent(magpie, false); mBody.localPosition = new Vector3(0, 0.42f, 0);
            P(PrimitiveType.Sphere, "Torso", new Vector3(0, 0.55f, 0.02f), new Vector3(0.95f, 1.15f, 0.82f), mBlack, mBody);
            P(PrimitiveType.Sphere, "Belly", new Vector3(0, 0.52f, -0.2f), new Vector3(0.72f, 0.92f, 0.5f), mWhite, mBody);
            var apron = B("Apron", new Vector3(0, 0.3f, -0.42f), new Vector3(0.74f, 0.56f, 0.08f), 0.06f, mApron, mBody);
            apron.transform.localRotation = Quaternion.Euler(8f, 0, 0);
            B("Strap", new Vector3(0, 0.62f, -0.41f), new Vector3(0.78f, 0.06f, 0.06f), 0.02f, mApron, mBody);
            B("Pocket", new Vector3(0.12f, 0.26f, -0.47f), new Vector3(0.22f, 0.16f, 0.03f), 0.02f, mCobaltLight, mBody);
            var towel = B("Towel", new Vector3(-0.18f, 0.18f, -0.48f), new Vector3(0.12f, 0.3f, 0.03f), 0.02f, mWhite, mBody);
            mTail = new GameObject("Tail").transform; mTail.SetParent(mBody, false); mTail.localPosition = new Vector3(0, 0.25f, 0.38f);
            var tail = B("TailFeather", new Vector3(0, 0.05f, 0.55f), new Vector3(0.26f, 0.06f, 1.15f), 0.03f, mBlackSheen, mTail);
            mTail.localRotation = Quaternion.Euler(-28f, 0, 0);
            mHead = new GameObject("Head").transform; mHead.SetParent(mBody, false); mHead.localPosition = new Vector3(0, 1.18f, -0.02f);
            P(PrimitiveType.Sphere, "Skull", Vector3.zero, new Vector3(0.66f, 0.62f, 0.64f), mBlack, mHead);
            P(PrimitiveType.Sphere, "Cheek", new Vector3(0, -0.12f, -0.12f), new Vector3(0.5f, 0.32f, 0.42f), mBlackSheen, mHead);
            var beak = P(PrimitiveType.Sphere, "Beak", new Vector3(0.0f, -0.04f, -0.42f), new Vector3(0.14f, 0.12f, 0.42f), mBeak, mHead);
            beak.transform.localRotation = Quaternion.Euler(8f, 0, 0);
            for (int s = -1; s <= 1; s += 2)
            {
                var eye = new GameObject(s < 0 ? "EyeL" : "EyeR").transform; eye.SetParent(mHead, false);
                eye.localPosition = new Vector3(s * 0.2f, 0.06f, -0.24f);
                P(PrimitiveType.Sphere, "White", Vector3.zero, new Vector3(0.2f, 0.22f, 0.12f), mWhite, eye, false);
                P(PrimitiveType.Sphere, "Pupil", new Vector3(0, 0, -0.05f), new Vector3(0.12f, 0.14f, 0.06f), mInk, eye, false);
                P(PrimitiveType.Sphere, "Glint", new Vector3(0.03f, 0.04f, -0.08f), new Vector3(0.04f, 0.04f, 0.02f), mWhite, eye, false);
                if (s < 0) mEyeL = eye; else mEyeR = eye;
            }
            // 날개(오른쪽은 집게를 든다)
            mWingL = new GameObject("WingL").transform; mWingL.SetParent(mBody, false); mWingL.localPosition = new Vector3(-0.44f, 0.8f, 0.02f);
            P(PrimitiveType.Sphere, "Wing", new Vector3(-0.05f, -0.3f, 0), new Vector3(0.24f, 0.72f, 0.5f), mBlackSheen, mWingL);
            mWingR = new GameObject("WingR").transform; mWingR.SetParent(mBody, false); mWingR.localPosition = new Vector3(0.44f, 0.8f, 0.02f);
            P(PrimitiveType.Sphere, "Wing", new Vector3(0.05f, -0.3f, 0), new Vector3(0.24f, 0.72f, 0.5f), mBlackSheen, mWingR);
            var tong = new GameObject("Tongs").transform; tong.SetParent(mWingR, false); tong.localPosition = new Vector3(0.1f, -0.58f, -0.14f);
            tong.localRotation = Quaternion.Euler(-70f, 0, 0);
            mTongA = B("TongA", new Vector3(-0.05f, 0.42f, 0), new Vector3(0.06f, 0.84f, 0.06f), 0.025f, mTong, tong).transform;
            mTongB = B("TongB", new Vector3(0.05f, 0.42f, 0), new Vector3(0.06f, 0.84f, 0.06f), 0.025f, mTong, tong).transform;
            B("TongGrip", new Vector3(0, 0.02f, 0), new Vector3(0.16f, 0.14f, 0.1f), 0.03f, mSteelDark, tong);
            mTongTip = new GameObject("TongTip").transform; mTongTip.SetParent(tong, false); mTongTip.localPosition = new Vector3(0, 0.88f, 0);
        }

        void BuildSupply()
        {
            supplyRoot = new GameObject("Supply").transform; supplyRoot.SetParent(worldRoot, false);
            // 반죽 공급기: 밀가루 통 + 쌓인 반죽 + 레버
            P(PrimitiveType.Cylinder, "FlourBin", new Vector3(-0.6f, 0.5f, 0.3f), new Vector3(1.0f, 0.5f, 1.0f), mSteel, supplyRoot);
            P(PrimitiveType.Sphere, "Flour", new Vector3(-0.6f, 1.0f, 0.3f), new Vector3(0.9f, 0.25f, 0.9f), mDough, supplyRoot);
            for (int i = 0; i < 4; i++)
            {
                var slab = B("DoughStack" + i, new Vector3(0.7f, 0.14f + i * 0.13f, -0.2f), new Vector3(0.9f, 0.1f, 0.8f), 0.05f, mDough, supplyRoot);
                slab.transform.localRotation = Quaternion.Euler(0, i * 17f, 0);
            }
            lever = new GameObject("Lever").transform; lever.SetParent(supplyRoot, false); lever.localPosition = new Vector3(1.4f, 0.3f, 0.4f);
            B("LeverBase", new Vector3(0, 0, 0), new Vector3(0.4f, 0.3f, 0.4f), 0.06f, mSteelDark, lever);
            P(PrimitiveType.Cylinder, "Shaft", new Vector3(0, 0.6f, 0), new Vector3(0.08f, 0.6f, 0.08f), mSteel, lever);
            P(PrimitiveType.Sphere, "Grip", new Vector3(0, 1.2f, 0), Vector3.one * 0.2f, mGold, lever);
            var crate = B("Crate", new Vector3(-0.4f, 0.3f, -1.1f), new Vector3(1.1f, 0.6f, 0.8f), 0.08f, mCrate, supplyRoot);
            var bowl = P(PrimitiveType.Sphere, "Filling", new Vector3(-0.4f, 0.62f, -1.1f), new Vector3(0.7f, 0.16f, 0.5f), mFill, supplyRoot);
        }

        void BuildProps()
        {
            propsL = new GameObject("PropsL").transform; propsL.SetParent(worldRoot, false);
            propsR = new GameObject("PropsR").transform; propsR.SetParent(worldRoot, false);
            foreach (var root in new[] { propsL, propsR })
            {
                float s = root == propsL ? -1f : 1f;
                B("Crate1", new Vector3(0, 0.35f, 0), new Vector3(1.2f, 0.7f, 0.9f), 0.08f, mCrate, root);
                B("Crate2", new Vector3(0.1f * s, 1.05f, 0.05f), new Vector3(1.2f, 0.7f, 0.9f), 0.08f, mCrate, root);
                P(PrimitiveType.Cylinder, "Pot", new Vector3(-1.2f * s, 0.55f, 1.2f), new Vector3(1.1f, 0.55f, 1.1f), mSteel, root);
                P(PrimitiveType.Cylinder, "PotLid", new Vector3(-1.2f * s, 1.12f, 1.2f), new Vector3(1.14f, 0.04f, 1.14f), mSteelDark, root);
                P(PrimitiveType.Cylinder, "Pot2", new Vector3(0.2f * s, 0.4f, 2.4f), new Vector3(0.9f, 0.4f, 0.9f), mSteel, root);
            }
        }

        void BuildAmbient()
        {
            var dripMat = MgfLook.Lit(MgfLook.Hex("BFE3FF"), 0.9f, 0f, MgfLook.Hex("2A4A6A"));
            for (int i = 0; i < drips.Length; i++)
            {
                drips[i] = P(PrimitiveType.Capsule, "Drip" + i, Vector3.zero, new Vector3(0.05f, 0.14f, 0.05f), dripMat, worldRoot, false).transform;
                dripY[i] = Random.Range(0f, 4.6f);
            }
            var ringMesh = RingMesh();
            for (int i = 0; i < ripples.Length; i++)
            {
                var go = new GameObject("Ripple" + i);
                go.transform.SetParent(worldRoot, false);
                go.AddComponent<MeshFilter>().sharedMesh = ringMesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = MgfLook.Alpha(Color.white); r.shadowCastingMode = ShadowCastingMode.Off;
                go.SetActive(false);
                ripples[i] = go.transform; rippleT[i] = -1f;
            }
        }

        // ───────────────────────── 절차 텍스처·메시 ─────────────────────────

        static Texture2D FloorTex()
        {
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "WetSlate", wrapMode = TextureWrapMode.Repeat };
            var rnd = new System.Random(5);
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    bool grout = (x % 64) < 2 || (y % 64) < 2;
                    int tile = (x / 64) + (y / 64) * 2;
                    float v = 0.11f + 0.015f * ((tile * 37) % 5) + (float)rnd.NextDouble() * 0.012f;
                    var c = grout ? new Color(0.05f, 0.08f, 0.13f) : new Color(v * 0.72f, v * 1.18f, v * 1.95f);
                    px[y * S + x] = c;
                }
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }

        static Texture2D FlourTex()
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Flour", wrapMode = TextureWrapMode.Repeat };
            var rnd = new System.Random(9);
            var px = new Color32[S * S];
            for (int i = 0; i < px.Length; i++)
            {
                float n = (float)rnd.NextDouble();
                var c = new Color(0.965f, 0.945f, 0.905f);
                if (n > 0.97f) c = new Color(0.86f, 0.82f, 0.75f);
                else if (n > 0.9f) c = new Color(1f, 0.99f, 0.97f);
                px[i] = c;
            }
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }

        static Mesh TiledPlane(float size, float tiles)
        {
            var m = new Mesh { name = "Floor" };
            float h = size * 0.5f;
            m.SetVertices(new List<Vector3> { new Vector3(-h, 0, -h), new Vector3(h, 0, -h), new Vector3(h, 0, h), new Vector3(-h, 0, h) });
            m.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            m.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(tiles, 0), new Vector2(tiles, tiles), new Vector2(0, tiles) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.RecalculateBounds();
            return m;
        }

        static Mesh RingMesh()
        {
            var m = new Mesh { name = "FloorRing" };
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            const int seg = 28;
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(d * 0.85f); v.Add(d * 1f); c.Add(Color.white); c.Add(Color.white);
                if (i < seg) { int k = i * 2; t.Add(k); t.Add(k + 2); t.Add(k + 1); t.Add(k + 1); t.Add(k + 2); t.Add(k + 3); }
            }
            m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        }

        static Mesh WhiteColored(Mesh src)
        {
            var m = Instantiate(src);
            var cols = new Color[m.vertexCount];
            for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
            m.colors = cols;
            return m;
        }

        // ───────────────────────── 배치(세로 390×844 · 가로 1280×800) ─────────────────────────

        void CheckLayout()
        {
            if (Screen.width != lastW || Screen.height != lastH) ApplyLayout();
        }

        void SetCap(int n)
        {
            cap = n;
            foreach (var t in trays) if (t.phase != Tray.Phase.Hidden && t.phase != Tray.Phase.Sealing) PlaceTray(t, false);
        }

        void ApplyLayout()
        {
            lastW = Screen.width; lastH = Screen.height;
            float aspect = lastW / (float)Mathf.Max(1, lastH);
            land = aspect >= 1.1f;
            orthoBase = land ? 6f : Mathf.Max(6f, 5f / aspect);
            cam.orthographicSize = orthoBase;
            pxPerUnit = lastH / (2f * orthoBase);

            float canopyV = land ? 0.865f : 0.862f;
            canopyRoot.position = ViewGround(0.5f, canopyV, 4.9f);
            float viewW = 2f * orthoBase * aspect;
            float span = Mathf.Min(viewW * 0.92f, 12f);
            for (int i = 0; i < 12; i++)
            {
                float x = -span * 0.5f + span * (i + 0.5f) / 12f;
                tilePivot[i].localPosition = new Vector3(x, -0.52f, -0.35f);
                float sc = land ? 1.15f : Mathf.Min(1f, span / 12f / 0.66f);
                tilePivot[i].localScale = Vector3.one * sc;
            }
            float tspan = land ? 9.2f : Mathf.Min(viewW * 0.93f, 9.4f);
            for (int i = 0; i < 4; i++)
            {
                titlePivot[i].localPosition = new Vector3(-tspan * 0.5f + tspan * (i + 0.5f) / 4f, -0.55f - (i % 2) * 0.22f, -0.6f);
                titlePivot[i].localScale = Vector3.one * Mathf.Min(1.1f, tspan / 4f / 2.2f);
            }

            if (land)
            {
                stationRoot.position = ViewGround(0.865f, 0.26f, 0f);
                stationRoot.localScale = Vector3.one * 1.25f;
                supplyRoot.position = ViewGround(0.09f, 0.28f, 0f);
                supplyRoot.localScale = Vector3.one * 1.2f;
                beltRoot.position = ViewGround(0.5f, 0.1f, 0f);
                propsL.position = ViewGround(0.04f, 0.5f, 0f);
                propsR.position = ViewGround(0.97f, 0.56f, 0f);
            }
            else
            {
                stationRoot.position = ViewGround(0.7f, 0.075f, 0f);
                stationRoot.localScale = Vector3.one * 0.95f;
                supplyRoot.position = ViewGround(0.1f, 0.06f, 0f);
                supplyRoot.localScale = Vector3.one * 0.8f;
                beltRoot.position = ViewGround(0.5f, 0.03f, 0f);
                propsL.position = ViewGround(-0.07f, 0.2f, 0f);
                propsR.position = ViewGround(1.07f, 0.24f, 0f);
            }
            foreach (var t in trays) if (t.phase != Tray.Phase.Hidden) PlaceTray(t, false);
            SnapshotDoughs();
            LayoutUi();
        }

        /// <summary>레인 칸의 판 중심 화면 위치와 판 1로컬 단위당 픽셀.</summary>
        void LaneSlot(int lane, out float u, out float v, out float pxLocal)
        {
            float W = lastW, H = lastH;
            const float blockH = 5.85f, centerOff = 0.235f;
            bool title = stage == Stage.Title || (stage == Stage.Practice && practiceStep == 1);
            if (!land)
            {
                float top = 0.165f * H, bottom = H - 0.168f * H;
                float avail = bottom - top;
                if (cap == 1)
                {
                    pxLocal = Mathf.Min(0.8f * W / Tray.PlateW, avail * 0.86f / blockH);
                    float cy = top + avail * (title ? 0.6f : 0.5f) + centerOff * pxLocal;
                    u = 0.5f; v = 1f - cy / H;
                }
                else
                {
                    float per = avail * 0.5f;
                    pxLocal = Mathf.Min(0.68f * W / Tray.PlateW, (per - 8f) / blockH);
                    float cy = top + per * (lane == 0 ? 0.5f : 1.5f) + centerOff * pxLocal;
                    u = 0.5f; v = 1f - cy / H;
                }
            }
            else
            {
                float top = 0.17f * H, bottom = H - 0.04f * H;
                float avail = bottom - top;
                float cyc = top + avail * (title ? 0.56f : 0.5f);
                if (cap == 1)
                {
                    pxLocal = Mathf.Min(0.33f * W / Tray.PlateW, avail * 0.8f / blockH);
                    u = 0.47f;
                }
                else
                {
                    pxLocal = Mathf.Min(0.255f * W / Tray.PlateW, avail * 0.75f / blockH);
                    u = lane == 0 ? 0.335f : 0.61f;
                }
                v = 1f - (cyc + centerOff * pxLocal) / H;
            }
        }

        void PlaceTray(Tray t, bool fromOffscreen)
        {
            LaneSlot(cap == 1 ? 0 : t.lane, out float u, out float v, out float pxLocal);
            float s = pxLocal / pxPerUnit;
            trayScale[t.lane] = s;
            t.trayScale = s;
            float h = 2.4f;
            var facePos = ViewGround(u, v, h);
            t.station = facePos - Vector3.up * h;
            t.face.localScale = Vector3.one * s;
            t.face.localRotation = cam.transform.rotation;
            t.SetHeight(h);
            if (fromOffscreen)
            {
                t.fromPos = t.station + Vector3.left * (2f * orthoBase * cam.aspect * 0.75f + 4f);
                t.stand.position = t.fromPos;
            }
            else t.stand.position = t.station;
        }

        void FinishArrival(Tray t)
        {
            if (t.phase != Tray.Phase.Arriving) return;
            t.phase = Tray.Phase.Ready; t.arriveT = 1f;
            t.stand.position = t.station;
            SnapshotDoughs();
            MgfBridge.NotifyChanged();
        }

        // ───────────────────────── 트레이 애니메이션 ─────────────────────────

        static float EaseOutBack(float x) { const float c1 = 1.70158f, c3 = c1 + 1f; x = Mathf.Clamp01(x); return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2); }
        static float EaseInOut(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        void UpdateTrays(float dt)
        {
            float adt = hitStop > 0f ? 0f : dt;
            if (hitStop > 0f) hitStop -= Time.unscaledDeltaTime;
            strokePhase += dt * 1.6f;
            foreach (var t in trays)
            {
                if (t.phase == Tray.Phase.Hidden) continue;
                int i = t.lane;
                var right = Vector3.right;
                switch (t.phase)
                {
                    case Tray.Phase.Arriving:
                        t.arriveT += adt / 0.55f;
                        if (t.arriveT >= 1f) { FinishArrival(t); }
                        else t.stand.position = Vector3.LerpUnclamped(t.fromPos, t.station, EaseOutBack(Mathf.Max(0f, t.arriveT)));
                        break;
                    case Tray.Phase.Ready:
                        {
                            float frac = t.maxLife > 100f ? 0f : Mathf.Clamp01(t.drift / Mathf.Max(1f, t.maxLife));
                            t.stand.position = t.station + right * (0.32f * frac * t.trayScale);
                            if (strokeTray == i && strokeOrder.Count > 0) RedrawStrokeIfNeeded(t);
                        }
                        break;
                    case Tray.Phase.Feedback:
                        t.phaseT += adt;
                        AnimateWrong(t, adt);
                        break;
                    case Tray.Phase.Sealing:
                        t.phaseT += adt;
                        AnimateSeal(t, adt);
                        break;
                }
                if (t.phase == Tray.Phase.Hidden) continue;
                // 살짝 들림(첫 접촉) + 흔들림(거절) + 숨쉬기
                liftT[i] = Mathf.Max(0f, liftT[i] - dt * 4f);
                wiggleT[i] = Mathf.Max(0f, wiggleT[i] - dt);
                float lift = Mathf.Sin(Mathf.Clamp01(liftT[i]) * Mathf.PI) * 0.12f;
                bool held = strokeTray == i;
                float bob = Mathf.Sin(Time.time * 1.7f + i) * 0.025f;
                t.face.localPosition = new Vector3(0, 2.4f + bob + (held ? 0.06f : 0f) + lift, 0);
                float wig = wiggleT[i] > 0f ? Mathf.Sin(wiggleT[i] * 55f) * 4f * wiggleT[i] / 0.35f : 0f;
                if (t.phase != Tray.Phase.Sealing) t.face.localRotation = cam.transform.rotation * Quaternion.Euler(0, 0, wig);
                bool timed = stage == Stage.Play && !t.prob.practice;
                t.SetGauge(timed ? t.life / Mathf.Max(0.01f, t.maxLife) : 0f, timed && t.phase != Tray.Phase.Sealing);
                if (stubT[i] > 0f)
                {
                    stubT[i] -= dt;
                    float k = 1f - stubT[i] / 0.9f;
                    t.DrawStub(stubFrom[i], k);
                    if (stubT[i] <= 0f) t.ClearHint();
                }
            }
        }

        void RedrawStrokeIfNeeded(Tray t)
        {
            // 점선 고리가 천천히 돌게(선택 중 피드백) — 정오는 드러내지 않는다.
            if (Time.frameCount % 3 == 0) RedrawStroke(t);
        }

        void AnimateWrong(Tray t, float adt)
        {
            float p = t.phaseT;
            int i = t.lane;
            float shake = p < 0.45f ? Mathf.Sin(p * 70f) * 0.12f * (1f - p / 0.45f) : 0f;
            t.stand.position = t.station + Vector3.right * shake * t.trayScale;
            var tr = sealTrail[i];
            if (tr.Count > 1)
            {
                // 느슨해진 산호색 봉합선(아래로 처진다)
                slack.Clear();
                for (int k = 0; k < tr.Count; k++)
                {
                    float f = tr.Count == 1 ? 0 : (float)k / (tr.Count - 1);
                    slack.Add(tr[k] + new Vector2(0, -Mathf.Sin(f * Mathf.PI) * 0.35f * Mathf.Clamp01(p * 3f)));
                }
                t.DrawStroke(slack, t.lastMask, new Color(1f, 0.36f, 0.48f, Mathf.Clamp01(1.4f - p)), 0.1f, p);
            }
            if (p > 0.35f) t.DrawReveal(true, t.lastMask, false, p * 0.9f);
        }

        readonly List<Vector2> slack = new List<Vector2>(128);
        readonly List<Vector2> zip = new List<Vector2>(128);

        void AnimateSeal(Tray t, float adt)
        {
            int i = t.lane;
            bool demo = sealDemo[i];
            float p = t.phaseT;
            float zipEnd = 0.28f, foldDur = demo ? 0.95f : 0.42f, foldEnd = zipEnd + foldDur, hold = demo ? 0.65f : 0.08f, puffEnd = foldEnd + 0.16f, launchAt = puffEnd + hold, end = launchAt + 0.45f;
            var tr = sealTrail[i];
            // 형광 봉합선이 손 궤적을 따라 지퍼처럼 닫힌다.
            int n = Mathf.Clamp(Mathf.CeilToInt(tr.Count * Mathf.Clamp01(p / zipEnd)), 0, tr.Count);
            zip.Clear(); for (int k = 0; k < n; k++) zip.Add(tr[k]);
            float glow = p < foldEnd ? 1f : Mathf.Clamp01(1f - (p - foldEnd) * 4f);
            t.DrawStroke(zip, 0, new Color(0.95f, 0.76f, 0.31f, glow), 0.17f, p);
            t.DrawReveal(false, 0, demo && p > zipEnd, p);
            if (p > zipEnd) t.Fold(EaseInOut((p - zipEnd) / foldDur));
            float puff = p > foldEnd ? Mathf.Sin(Mathf.Clamp01((p - foldEnd) / 0.16f) * Mathf.PI) : 0f;
            if (p < launchAt) t.content.localScale = new Vector3(1f + puff * 0.06f, 1f + puff * 0.06f, 1f + puff * 1.4f);
            if (p >= launchAt && !launched[i])
            {
                launched[i] = true;
                LaunchFlyer(t);
                ManduSfx.Play("shutter", 0.5f);
            }
            if (p >= launchAt)
            {
                float f = Mathf.Clamp01((p - launchAt) / 0.4f);
                t.content.localScale = Vector3.one * (1f - EaseInOut(f));
                // 철판 셔터가 뒤집힌다
                t.face.localRotation = cam.transform.rotation * Quaternion.Euler(-360f * EaseInOut(f), 0, 0);
            }
            if (p >= end)
            {
                launched[i] = false;
                t.Fold(0f);
                t.face.localRotation = cam.transform.rotation;
                HideTray(t);
                SnapshotDoughs();
                MgfBridge.NotifyChanged();
            }
        }

        readonly bool[] launched = new bool[2];

        // ───────────────────────── 연출 ─────────────────────────

        void CorrectFx(Tray t, bool demo)
        {
            int i = t.lane;
            launched[i] = false;
            CaptureTrail(t, true);
            ManduSfx.Play("pleat", 0.55f, 1f);
            ManduSfx.Play("pleat", 0.45f, 1.25f);
            camPunch = 1f;
            poseClamp = 0f;
        }

        void WrongFx(Tray t, int mask, bool real)
        {
            CaptureTrail(t, false);
            ManduSfx.Play("thud", 0.6f);
            liftT[t.lane] = 0f;
            poseOops = 1f;
            if (real)
            {
                int idx = Mathf.Clamp(st.lives, 0, 2); // 방금 잃은 봉인
                sealBreakT[idx] = 0f;
                ManduSfx.Play("crack", 0.5f);
                shakeT = 0.25f;
                LaunchBlob(t);
            }
        }

        void CaptureTrail(Tray t, bool correct)
        {
            var dst = sealTrail[t.lane];
            dst.Clear();
            if (trail.Count > 1) dst.AddRange(trail);
            else
            {
                // 훅·만료처럼 손 궤적이 없으면: 정답이면 정답 두 대상, 오답이면 낸 쌍 사이 직선
                int m = correct ? t.prob.CorrectMask : t.lastMask;
                int a = -1, b = -1;
                for (int k = 0; k < 3; k++) if ((m & (1 << k)) != 0) { if (a < 0) a = k; else if (b < 0) b = k; }
                if (a >= 0 && b >= 0) { dst.Add(t.tpos[a]); dst.Add(t.tpos[b]); }
            }
            trail.Clear();
        }

        void LaunchFlyer(Tray t)
        {
            Flyer f = null;
            foreach (var x in flyers) if (x.time < 0f) { f = x; break; }
            if (f == null) f = flyers[0];
            var poly = t.FoldedPolygon();
            DoughMesh.Build(f.mesh, poly, 0.42f, 0.18f);
            f.t.gameObject.SetActive(true);
            f.scale0 = t.trayScale;
            f.t.localScale = Vector3.one * f.scale0;
            f.t.rotation = cam.transform.rotation;
            f.p0 = t.DoughCenterWorld();
            f.p3 = mTongTip.position;
            f.p1 = f.p0 + Vector3.up * 2.5f;
            f.p2 = f.p3 + Vector3.up * 2.2f;
            f.slot = Mathf.Clamp(st.solved - 1, 0, 11);
            f.time = 0f;
            poseCatch = 0.01f;
        }

        void LaunchBlob(Tray t)
        {
            Blob b = null;
            foreach (var x in blobs) if (x.time < 0f) { b = x; break; }
            if (b == null) b = blobs[0];
            b.p0 = t.DoughCenterWorld();
            b.p1 = cup.position + Vector3.up * 0.5f * stationRoot.localScale.y;
            b.time = 0f;
            b.t.gameObject.SetActive(true);
        }

        void QueueWow() { wowT = 0f; }

        void LiftTray(Tray t) { liftT[t.lane] = 1f; }
        void WiggleTray(Tray t) { wiggleT[t.lane] = 0.35f; ManduSfx.Play("refuse", 0.35f); }
        void SlackTrail(Tray t) { t.ClearDyn(); WiggleTray(t); }
        void MagpieClamp() { poseClamp = 1f; }
        void StubHint(Tray t, int from) { stubFrom[t.lane] = from; stubT[t.lane] = 0.9f; t.ClearDyn(); WiggleTray(t); }

        void ResetWorldRun()
        {
            for (int i = 0; i < 12; i++) { tileOn[i] = false; slotDumplings[i].gameObject.SetActive(false); }
            for (int i = 0; i < 3; i++)
            {
                sealBreakT[i] = -1f;
                sealHalfA[i].localPosition = new Vector3(-0.08f, 0, 0); sealHalfA[i].localRotation = Quaternion.Euler(90f, 0, 0);
                sealHalfB[i].localPosition = new Vector3(0.08f, 0, 0); sealHalfB[i].localRotation = Quaternion.Euler(90f, 0, 0);
                sealDisc[i].gameObject.SetActive(true);
                wowDumplings[i].gameObject.SetActive(false);
            }
            cupLevel = 0f; wowT = -1f;
            foreach (var f in flyers) { f.time = -1f; f.t.gameObject.SetActive(false); }
            foreach (var b in blobs) { b.time = -1f; b.t.gameObject.SetActive(false); }
        }

        // ───────────────────────── 세계 갱신 ─────────────────────────

        void UpdateWorld(float dt)
        {
            float t = Time.time;
            magpieT += dt;
            // 카메라: 정답 순간 짧은 줌 인(정답은 확산·빛), 오답은 작은 흔들림
            camPunch = Mathf.Max(0f, camPunch - dt * 3.2f);
            cam.orthographicSize = orthoBase * (1f - 0.025f * Mathf.Sin(Mathf.Clamp01(camPunch) * Mathf.PI));
            if (shakeT > 0f)
            {
                shakeT -= dt;
                cam.transform.position = camBasePos + cam.transform.right * Mathf.Sin(shakeT * 90f) * 0.06f * (shakeT / 0.25f);
            }
            else cam.transform.position = camBasePos;

            // 컨베이어 판자 흐름
            beltOffset = (beltOffset + dt * 0.9f) % 1f;
            for (int i = 0; i < slats.Count; i++)
            {
                float x = -beltLen * 0.5f + ((i + beltOffset) / slats.Count) * beltLen;
                slats[i].localPosition = new Vector3(x, 0.4f, 0);
            }

            // 주문 패: 타이틀 패 뒤집기 → 플레이 중 올라감
            bool titleOn = stage == Stage.Title;
            titleT += dt;
            titleRise = Mathf.MoveTowards(titleRise, titleOn ? 0f : 1f, dt * 2.2f);
            titleTilesRoot.localPosition = new Vector3(0, titleRise * 4f, 0);
            titleTilesRoot.gameObject.SetActive(titleRise < 0.99f);
            for (int i = 0; i < 4; i++)
            {
                float k = Mathf.Clamp01((titleT - 0.08f - i * 0.11f) / 0.3f);
                float ang = 180f * (1f - EaseOutBack(k));
                float sway = Mathf.Sin(t * 1.3f + i * 1.7f) * 3f;
                titlePivot[i].localRotation = Quaternion.Euler(ang, 0, sway);
                if (k > 0f && k < 0.12f && titleOn && !flipped[i]) { flipped[i] = true; ManduSfx.Play("flip", 0.35f, 0.9f + i * 0.08f); }
            }
            tilesRoot.gameObject.SetActive(!titleOn);
            for (int i = 0; i < 12; i++)
            {
                float target = tileOn[i] ? 180f : 0f;
                tileFlip[i] = Mathf.MoveTowards(tileFlip[i], target, dt * 600f);
                float sway = Mathf.Sin(t * 1.1f + i * 0.9f) * 2.5f;
                tilePivot[i].localRotation = Quaternion.Euler(tileFlip[i], 0, sway);
            }
            for (int i = 0; i < 12; i++)
            {
                bool want = i < st.solved;
                if (want && !tileOn[i]) { tileOn[i] = true; ManduSfx.Play("flip", 0.3f, 1.1f); }
                if (!want && tileOn[i]) tileOn[i] = false;
            }

            UpdateMagpie(dt);
            UpdateFlyers(dt);
            UpdateSeals(dt);

            // 찜통 뚜껑 + 첫 3연속 와우
            if (wowT >= 0f)
            {
                wowT += dt;
                float up = wowT < 1.6f ? EaseOutBack(Mathf.Clamp01(wowT / 0.4f)) : 1f - EaseInOut((wowT - 1.6f) / 0.4f);
                lid.localPosition = new Vector3(0, 1.18f + up * 0.9f, -up * 0.2f);
                lid.localRotation = Quaternion.Euler(-up * 28f, 0, 0);
                for (int i = 0; i < 3; i++)
                {
                    float k = Mathf.Clamp01((wowT - 0.25f - i * 0.18f) / 0.3f);
                    wowDumplings[i].gameObject.SetActive(k > 0f && wowT < 2.0f);
                    wowDumplings[i].localScale = Vector3.one * 1.7f * EaseOutBack(k) * (1f + 0.12f * Mathf.Sin(wowT * 9f + i));
                    wowDumplings[i].localPosition = new Vector3(-0.48f + i * 0.48f, 1.05f + 0.55f * EaseOutBack(k), -0.1f);
                    if (k > 0f && k < 0.1f && !wowPop[i]) { wowPop[i] = true; ManduSfx.Play("pleat", 0.5f, 1.3f + i * 0.1f); MgfFx.Glow(wowDumplings[i].position + Vector3.up * 0.3f, new Color(1f, 1f, 1f, 0.75f), 6, 0.5f); }
                }
                if (wowT > 2.1f) { wowT = -1f; for (int i = 0; i < 3; i++) { wowDumplings[i].gameObject.SetActive(false); wowPop[i] = false; } lid.localPosition = new Vector3(0, 1.18f, 0); lid.localRotation = Quaternion.identity; }
            }

            // 증기(찜통·증기 레인) — 공용 파티클 시스템 1개 재사용
            steamT += dt;
            if (steamT > 0.45f)
            {
                steamT = 0f;
                MgfFx.Glow(steamer.position + Vector3.up * 1.6f * stationRoot.localScale.y, new Color(1f, 1f, 1f, 0.35f), 2, 0.55f);
            }
            if (steamLight) steamLight.intensity = 1.25f + Mathf.Sin(t * 2.3f) * 0.2f;
            if (lampL) lampL.intensity = 1.05f + Mathf.Sin(t * 7.1f) * 0.05f;

            // 빗방울: 차양 끝에서 떨어져 바닥에 물결
            float viewW = 2f * orthoBase * cam.aspect;
            for (int i = 0; i < drips.Length; i++)
            {
                dripY[i] -= dt * 7.5f;
                if (dripY[i] < 0f)
                {
                    dripY[i] = 4.6f + Random.Range(0f, 2.5f);
                    float side = (i & 1) == 0 ? -1f : 1f;
                    float x = side * Random.Range(viewW * (land ? 0.36f : 0.38f), viewW * 0.5f);
                    drips[i].position = canopyRoot.position + new Vector3(x, 0, -0.3f);
                }
                var p = drips[i].position; p.y = Mathf.Min(dripY[i], canopyRoot.position.y - 0.3f); drips[i].position = p;
            }
            // 젖은 바닥의 빗물 파문: 판·까치를 피해 화면 가장자리 바닥에 무작위로
            rippleClock -= dt;
            if (rippleClock <= 0f)
            {
                rippleClock = Random.Range(0.25f, 0.6f);
                float u = Random.value < 0.5f ? Random.Range(0.02f, land ? 0.2f : 0.12f) : Random.Range(land ? 0.8f : 0.88f, 0.98f);
                SpawnRipple(ViewGround(u, Random.Range(0.18f, 0.8f), 0.02f), Random.Range(0.25f, 0.5f));
            }
            for (int i = 0; i < ripples.Length; i++)
            {
                if (rippleT[i] < 0f) continue;
                rippleT[i] += dt;
                float k = rippleT[i] / 0.9f;
                if (k >= 1f) { rippleT[i] = -1f; ripples[i].gameObject.SetActive(false); continue; }
                ripples[i].localScale = Vector3.one * (0.15f + k * rippleSize[i]);
                mpb.SetColor("_Color", new Color(0.75f, 0.88f, 1f, 0.22f * (1f - k)));
                ripples[i].GetComponent<Renderer>().SetPropertyBlock(mpb);
            }
        }

        readonly bool[] flipped = new bool[4];
        readonly bool[] wowPop = new bool[3];
        readonly float[] rippleSize = new float[10];

        void SpawnRipple(Vector3 pos, float size)
        {
            int i = rippleNext; rippleNext = (rippleNext + 1) % ripples.Length;
            ripples[i].position = pos; rippleT[i] = 0f; rippleSize[i] = size;
            ripples[i].gameObject.SetActive(true);
        }

        void UpdateMagpie(float dt)
        {
            poseCatch = poseCatch > 0f ? Mathf.Max(0f, poseCatch - dt * 1.1f) : 0f;
            poseOops = Mathf.Max(0f, poseOops - dt * 0.9f);
            poseClamp = Mathf.Max(0f, poseClamp - dt * 1.6f);
            float idle = Mathf.Sin(magpieT * 2.1f);
            float c = Mathf.Sin(Mathf.Clamp01(poseCatch) * Mathf.PI);
            float o = Mathf.Sin(Mathf.Clamp01(poseOops) * Mathf.PI);
            float cl = Mathf.Sin(Mathf.Clamp01(poseClamp) * Mathf.PI);
            // 숨쉬기 + 정답 포착 시 늘어남(스쿼시&스트레치) + 오답 시 고개 숙임
            float stretch = 1f + 0.06f * c - 0.05f * o;
            mBody.localScale = new Vector3(1f / Mathf.Sqrt(stretch), stretch + idle * 0.012f, 1f / Mathf.Sqrt(stretch));
            mBody.localRotation = Quaternion.Euler(-8f * c + 14f * o + 6f * cl, 0, idle * 1.5f);
            mHead.localRotation = Quaternion.Euler(-22f * c + 26f * o + Mathf.Sin(magpieT * 0.7f) * 6f, Mathf.Sin(magpieT * 0.45f) * 18f - 20f * cl, 0);
            mWingR.localRotation = Quaternion.Euler(-110f * c - 40f * cl, 0, 18f + 10f * c);
            mWingL.localRotation = Quaternion.Euler(-30f * o - 55f * leverPose, 0, -16f - 20f * o);
            mTail.localRotation = Quaternion.Euler(-28f + idle * 4f + 18f * c, Mathf.Sin(magpieT * 1.3f) * 6f, 0);
            float clamp = 0.05f - 0.035f * (c + cl);
            mTongA.localPosition = new Vector3(-clamp, 0.42f, 0); mTongB.localPosition = new Vector3(clamp, 0.42f, 0);
            blinkT -= dt;
            float blink = blinkT < 0.12f ? 0.15f : 1f;
            if (blinkT < 0f) blinkT = Random.Range(2.2f, 4.5f);
            mEyeL.localScale = new Vector3(1, blink, 1); mEyeR.localScale = new Vector3(1, blink, 1);
            // 타이틀: 레버를 당겨 첫 반죽을 내보낸다
            leverPose = stage == Stage.Title ? Mathf.Clamp01(Mathf.Sin(Mathf.Clamp01((titleT - 0.6f) / 0.7f) * Mathf.PI)) : 0f;
            lever.localRotation = Quaternion.Euler(0, 0, -35f * leverPose);
        }

        float leverPose;

        void UpdateFlyers(float dt)
        {
            foreach (var f in flyers)
            {
                if (f.time < 0f) continue;
                f.time += dt;
                float k1 = Mathf.Clamp01(f.time / 0.5f);
                if (f.time < 0.5f)
                {
                    float u = k1, iu = 1f - u;
                    f.t.position = iu * iu * iu * f.p0 + 3f * iu * iu * u * f.p1 + 3f * iu * u * u * f.p2 + u * u * u * f.p3;
                    f.t.localScale = Vector3.one * Mathf.Lerp(f.scale0 * 0.85f, 0.42f, u);
                    f.t.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, u * 240f);
                    if (f.time > 0.38f && poseCatch < 0.5f) { poseCatch = 1f; }
                }
                else if (f.time < 0.85f)
                {
                    // 까치가 집게로 공중 포착 → 번호 트레이에 꽂는다
                    if (!f.caught) { f.caught = true; ManduSfx.Play("tong", 0.5f); MgfFx.Glow(f.t.position, new Color(1f, 0.85f, 0.45f, 0.8f), 5 + st.combo * 3, 0.45f); }
                    float u = (f.time - 0.5f) / 0.35f;
                    var slotPos = slotDumplings[f.slot].position;
                    f.t.position = Vector3.Lerp(mTongTip.position, slotPos, EaseInOut(u)) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.5f;
                    f.t.localScale = Vector3.one * Mathf.Lerp(0.42f, 0.3f, u);
                }
                else
                {
                    f.time = -1f; f.caught = false;
                    f.t.gameObject.SetActive(false);
                    var d = slotDumplings[f.slot];
                    d.gameObject.SetActive(true);
                    MgfFx.Punch(d, 0.5f, 0.3f);
                    ManduSfx.Play("steam", 0.35f);
                    MgfFx.Glow(d.position + Vector3.up * 0.2f, new Color(1f, 1f, 1f, 0.5f), 3 + st.combo * 2, 0.4f);
                }
            }
            foreach (var b in blobs)
            {
                if (b.time < 0f) continue;
                b.time += dt;
                float u = Mathf.Clamp01(b.time / 0.6f);
                b.t.position = Vector3.Lerp(b.p0, b.p1, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 1.2f;
                b.t.localScale = new Vector3(0.3f, 0.3f + u * 0.15f, 0.3f);
                if (u >= 1f) { b.time = -1f; b.t.gameObject.SetActive(false); cupLevel = Mathf.Min(1f, cupLevel + 0.34f); ManduSfx.Play("drip", 0.5f, 0.7f); }
            }
            float lvl = Mathf.Lerp(cupFill.localScale.y, 0.02f + cupLevel * 0.3f, dt * 6f);
            cupFill.localScale = new Vector3(0.52f, lvl, 0.52f);
            cupFill.localPosition = new Vector3(0, 0.04f + lvl, 0);
        }

        void UpdateSeals(float dt)
        {
            for (int i = 0; i < 3; i++)
            {
                if (sealBreakT[i] < 0f) continue;
                sealBreakT[i] += dt;
                float k = EaseOutBack(Mathf.Clamp01(sealBreakT[i] / 0.4f));
                sealHalfA[i].localPosition = new Vector3(-0.08f - 0.12f * k, -0.08f * k, 0);
                sealHalfA[i].localRotation = Quaternion.Euler(90f, 0, 25f * k);
                sealHalfB[i].localPosition = new Vector3(0.08f + 0.12f * k, -0.12f * k, 0);
                sealHalfB[i].localRotation = Quaternion.Euler(90f, 0, -35f * k);
            }
        }
    }
}
