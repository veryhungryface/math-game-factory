// 종이새 출격 — 측면식 종이 기계 풍동, 실제 삼각 날개, 온보딩과 피드백 연출.
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.JongisaeChulgyeok
{
    public partial class JongisaeChulgyeokGame
    {
        sealed class LeafMotion
        {
            public Transform t;
            public Quaternion baseRotation;
            public float phase;
            public float amount;
        }

        static readonly Vector3 PivotLocal = new Vector3(-1.45f, -1.05f, 0f);
        const float WingRadius = 3.35f;

        Camera cam;
        GameObject worldRoot;
        GameObject backgroundRoot;
        GameObject titleRoot;
        GameObject titleBird;
        GameObject playRoot;
        Transform mathRoot;
        GameObject resultRoot;
        GameObject restartBird;
        GameObject handle;
        GameObject flightBird;
        Transform flightLeftWing;
        Transform flightRightWing;
        readonly List<LeafMotion> leaves = new List<LeafMotion>();
        readonly List<Transform> ambientBirds = new List<Transform>();
        readonly Vector3[] ambientBirdBase = new Vector3[8];
        readonly Transform[] bells = new Transform[3];
        readonly Vector3[] bellBaseScale = new Vector3[3];
        readonly Transform[] sentBirds = new Transform[JongisaeRules.RunBirds];
        readonly Transform[] clipLeft = new Transform[JongisaeRules.StartLives];
        readonly Transform[] clipRight = new Transform[JongisaeRules.StartLives];
        readonly bool[] clipBroken = new bool[JongisaeRules.StartLives];
        readonly float[] clipBreakClock = new float[JongisaeRules.StartLives];

        Material leafBlue;
        Material leafBlueDark;
        Material shootGreen;
        Material paperIvory;
        Material paperWarm;
        Material inkNavy;
        Material coral;
        Material hollowMat;
        Material glowMat;
        Material ghostMat;

        Mesh panelMeshA;
        Mesh panelMeshB;
        MeshFilter panelAFilter;
        MeshFilter panelBFilter;
        MeshRenderer panelARenderer;
        MeshRenderer panelBRenderer;
        LineRenderer fixedBone;
        LineRenderer movingBone;
        LineRenderer baseBone;
        LineRenderer angleArc;
        LineRenderer outerArc;
        LineRenderer ticks;
        LineRenderer equalMarkAB;
        LineRenderer equalMarkAC;
        LineRenderer exteriorLine;
        LineRenderer correctGhost;
        LineRenderer guidePath;
        LineRenderer flightTrail;
        LineRenderer handleRing;
        LineRenderer proofMedian;
        readonly LineRenderer[] rippleRings = new LineRenderer[3];
        Transform rippleRoot;
        readonly Vector3[] trailHistory = new Vector3[14];

        TextMeshPro labelA;
        TextMeshPro labelB;
        TextMeshPro labelC;
        TextMeshPro labelD;
        TextMeshPro angleWorld;
        readonly TextMeshPro[] tickNumbers = new TextMeshPro[7];
        Transform guideHand;
        TextMeshPro grabHint;
        TextMeshPro proofM;
        Transform birdBody;
        Transform birdHead;
        Transform birdTail;

        TextMeshProUGUI timeUi;
        TextMeshProUGUI progressUi;
        TextMeshProUGUI clipsUi;
        TextMeshProUGUI scoreUi;
        TextMeshProUGUI goalUi;
        TextMeshProUGUI conditionUi;
        TextMeshProUGUI equationUi;
        TextMeshProUGUI toastUi;
        TextMeshProUGUI resultTitleUi;
        TextMeshProUGUI resultStatsUi;
        TextMeshProUGUI restartUi;
        Image leftHudPanel;
        Image rightHudPanel;

        float toastClock;
        float rippleClock;
        float handlePulseClock;
        float guideClock;
        bool guideActive;
        bool guideLarge;
        float guideStartAngle = 40f;
        float flightClock;
        bool flightActive;
        Vector3 flightStart;
        Vector3 flightEnd;
        float wrongClock;
        float bellFlashClock;
        int activeBell = -1;
        float displayedScore;
        float scoreVelocity;
        float titleClock;
        float resultClock;
        float targetOrtho = 7.35f;
        int lastScreenW = -1;
        int lastScreenH = -1;

        void BuildWorld()
        {
            // GameObject.CreatePrimitive(Cylinder)가 내부적으로 CapsuleCollider를 붙인다.
            // IL2CPP 스트리핑이 리플렉션 경로만 보고 이 타입을 지우지 않도록 명시적으로 보존한다.
            var colliderTypes = new GameObject("ColliderTypeKeepAlive");
            colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<SphereCollider>();
            UnityEngine.Object.Destroy(colliderTypes);

            // 빛 사수의 밝은 청록 빈 무대와 갈라지도록, 먹빛 기계 풍동 안에
            // 호박색 종이 조명이 스미는 측면식 작업장으로 팔레트와 공간을 바꾼다.
            MgfLook.Sky(MgfLook.Hex("F1D8A6"), MgfLook.Hex("8E6E64"), MgfLook.Hex("172A38"), 0.92f);
            MgfLook.Sun(new Vector3(48f, -34f, -18f), MgfLook.Hex("FFD58A"), 1.18f, 0.62f);
            cam = MgfLook.Camera(new Vector3(0f, 9.4f, -15.5f), new Vector3(0f, 1.6f, 0f), 35f);
            cam.orthographic = true;
            cam.orthographicSize = targetOrtho;

            Texture2D paper = Resources.Load<Texture2D>("JongisaeChulgyeok/paper-fiber");
            // 킷의 Lit 텍스처는 base color와 곱하지 않고 종이 원색을 보여 준다.
            // 캐노피는 팔레트가 실제 최빈색이 되도록 무늬 없는 코드 재질, 새/날개만 섬유 텍스처를 쓴다.
            leafBlue = MgfLook.Lit(MgfLook.Hex("476A72"), 0.28f, 0.08f);
            leafBlueDark = MgfLook.Lit(MgfLook.Hex("172A38"), 0.34f, 0.2f);
            shootGreen = MgfLook.Lit(MgfLook.Hex("E7A83E"), 0.22f, 0.04f);
            paperIvory = MgfLook.Lit(MgfLook.Hex("F8F1DA"), 0.22f, 0f, null, paper);
            paperWarm = MgfLook.Lit(MgfLook.Hex("E7D8B7"), 0.18f, 0f, null, paper);
            inkNavy = MgfLook.Lit(MgfLook.Hex("203548"), 0.42f, 0.28f);
            coral = MgfLook.Lit(MgfLook.Hex("DD705A"), 0.22f, 0f, MgfLook.Hex("3A0F08"));
            hollowMat = MgfLook.Lit(MgfLook.Hex("263D48"), 0.46f, 0.32f);
            glowMat = MgfLook.Unlit(MgfLook.Hex("FFE36E"));
            ghostMat = MgfLook.Alpha(new Color(0.65f, 0.96f, 0.96f, 0.42f));

            worldRoot = new GameObject("OpenWindTrialRange");
            BuildForest();
            BuildTitle();
            BuildPlayfield();
            BuildResult();
            BuildClips();
        }

        void BuildForest()
        {
            backgroundRoot = new GameObject("MechanicalPaperTunnel");
            backgroundRoot.transform.SetParent(worldRoot.transform, false);
            Transform bg = backgroundRoot.transform;

            // 둥근 관문과 빈 청록 배경 대신 측면식 풍동의 리브·봉제선·장력막을 겹쳐 화면을 채운다.
            MgfLook.Block("TunnelBack", new Vector3(0f, 0.15f, 4.4f), new Vector3(18.8f, 10.8f, 0.52f), 0.12f, leafBlueDark, bg);
            MgfLook.Block("TrialDeck", new Vector3(0f, -4.65f, 2.8f), new Vector3(19f, 0.72f, 7f), 0.12f, hollowMat, bg);
            CylinderBetween("LeftMast", new Vector3(-7.4f, -4.1f, 1.5f), new Vector3(-6.3f, 6.1f, 2.4f), 0.22f, inkNavy, bg);
            CylinderBetween("RightMast", new Vector3(7.4f, -4.1f, 1.5f), new Vector3(6.3f, 6.1f, 2.4f), 0.22f, inkNavy, bg);
            CylinderBetween("UpperTensionCable", new Vector3(-6.4f, 5.4f, 2.2f), new Vector3(6.4f, 5.4f, 2.2f), 0.065f, paperWarm, bg);
            CylinderBetween("LowerLaunchRail", new Vector3(-8.8f, -2.9f, 1.4f), new Vector3(8.8f, -2.9f, 1.4f), 0.15f, inkNavy, bg);
            for (int i = 0; i < 9; i++)
            {
                float x = -7.2f + i * 1.8f;
                CylinderBetween("TunnelRib" + i, new Vector3(x, -4.25f, 3.9f), new Vector3(x + 0.22f, 5.1f, 3.9f), 0.045f, leafBlue, bg);
                CylinderBetween("StitchRail" + i, new Vector3(x - 0.62f, -2.55f, 3.55f), new Vector3(x + 0.62f, -2.55f, 3.55f), 0.025f, shootGreen, bg);
            }
            CreateTriangleObject("LeftTensionMembrane", new[] { new Vector3(-8.8f, -1.8f, 3.3f), new Vector3(-6.3f, 5.2f, 3.1f), new Vector3(-4.25f, -1.1f, 3.15f) }, ghostMat, bg);
            CreateTriangleObject("RightTensionMembrane", new[] { new Vector3(8.8f, -1.8f, 3.3f), new Vector3(6.3f, 5.2f, 3.1f), new Vector3(4.25f, -1.1f, 3.15f) }, ghostMat, bg);

            for (int i = 0; i < 16; i++)
            {
                float side = (i & 1) == 0 ? -1f : 1f;
                float x = side * (4.3f + (i % 4) * 0.75f);
                float y = -3.4f + (i % 8) * 1.15f;
                var streamer = MgfLook.Block("WindVane" + i, new Vector3(x, y, 2.1f + (i % 3) * 0.22f), new Vector3(0.18f, 0.86f, 0.055f), 0.015f, i % 3 == 0 ? shootGreen : paperWarm, bg);
                streamer.transform.rotation = Quaternion.Euler(8f, side * 18f, side * (20f + i * 11f));
                leaves.Add(new LeafMotion { t = streamer.transform, baseRotation = streamer.transform.rotation, phase = i * 0.73f, amount = 4f + i % 3 });
            }
            for (int i = 0; i < 8; i++)
            {
                float x = -5.1f + i * 1.45f;
                var vane = CreateFoldLeaf("AirfoilMarker" + i, new Vector3(x, -3.55f + (i % 2) * 0.22f, 0.9f), new Vector2(0.28f, 0.72f), shootGreen, bg);
                vane.transform.rotation = Quaternion.Euler(8f, 0f, -38f + i * 9f);
                leaves.Add(new LeafMotion { t = vane.transform, baseRotation = vane.transform.rotation, phase = i * 1.21f, amount = 5f });
            }

            for (int i = 0; i < bells.Length; i++)
            {
                float x = -2.5f + i * 2.5f;
                bells[i] = CreateTensionRig("TensionFold" + i, new Vector3(x, 3.45f + (i % 2) * 0.28f, 0.9f), bg).transform;
                bellBaseScale[i] = bells[i].localScale;
            }

            for (int i = 0; i < 4; i++)
            {
                var bird = CreateBird("FlockBird" + i, 0.34f + (i % 3) * 0.04f, bg, false);
                bird.transform.position = new Vector3(-4.8f + i * 3.15f, 1.6f + (i % 2) * 0.85f, 1.4f + (i % 3) * 0.22f);
                bird.transform.rotation = Quaternion.Euler(0f, (i & 1) == 0 ? -15f : 18f, (i - 2) * 4f);
                ambientBirdBase[i] = bird.transform.position;
                ambientBirds.Add(bird.transform);
            }

            for (int i = 0; i < sentBirds.Length; i++)
            {
                var bird = CreateBird("SentBird" + i, 0.26f, bg, false);
                bird.transform.position = new Vector3(-7f + i * 1.75f, 5.0f + Mathf.Sin(i) * 0.35f, 1.1f);
                bird.SetActive(false);
                sentBirds[i] = bird.transform;
            }

            flightBird = CreateBird("FlightBird", 0.62f, bg, false);
            flightBird.SetActive(false);
            flightLeftWing = flightBird.transform.Find("WingLeft");
            flightRightWing = flightBird.transform.Find("WingRight");
            flightTrail = MakeLine("TensionStitchTrail", bg, MgfLook.Hex("FFE36E"), 0.075f, 14, false);
            flightTrail.enabled = false;
        }

        void BuildTitle()
        {
            titleRoot = new GameObject("WorldTitle");
            titleRoot.transform.SetParent(worldRoot.transform, false);
            var ribbon = MgfLook.Block("TitleRibbon", new Vector3(0f, 3.85f, 0.1f), new Vector3(6.1f, 1.45f, 0.14f), 0.06f, paperIvory, titleRoot.transform);
            ribbon.transform.rotation = Quaternion.Euler(0f, 0f, -2.5f);
            var shadow = MgfText.World("종이새 출격", new Vector3(0.08f, 3.75f, -0.11f), 7.8f, MgfLook.Hex("DD705A"), titleRoot.transform);
            shadow.fontStyle = FontStyles.Bold;
            var logo = MgfText.World("종이새 출격", new Vector3(0f, 3.84f, -0.18f), 7.8f, MgfLook.Hex("314162"), titleRoot.transform);
            logo.fontStyle = FontStyles.Bold;
            logo.outlineWidth = 0.13f;
            logo.outlineColor = MgfLook.Hex("F8F1DA");
            var tagline = MgfText.World("날개를 펼쳐 날려라", new Vector3(0f, 3.25f, -0.2f), 2.45f, MgfLook.Hex("314162"), titleRoot.transform);
            tagline.fontStyle = FontStyles.Bold;

            titleBird = CreateBird("StartBird", 1.18f, titleRoot.transform, true);
            titleBird.transform.localPosition = new Vector3(0.2f, -0.45f, -0.45f);
            titleBird.transform.localRotation = Quaternion.Euler(0f, -8f, 3f);
            var call = MgfText.World("날개를 잡아 시작", new Vector3(0.15f, -2.28f, -0.42f), 3.15f, MgfLook.Hex("F8F1DA"), titleRoot.transform);
            call.outlineWidth = 0.18f;
            call.outlineColor = MgfLook.Hex("314162");
            var unit = MgfText.World("중학교 2학년 · 삼각형의 성질", new Vector3(0f, -3.05f, -0.4f), 2.55f, MgfLook.Hex("F8F1DA"), titleRoot.transform);
            unit.outlineWidth = 0.1f;
            unit.outlineColor = MgfLook.Hex("314162");
        }

        void BuildPlayfield()
        {
            playRoot = new GameObject("WingWorkshop");
            playRoot.transform.SetParent(worldRoot.transform, false);
            mathRoot = new GameObject("MathPlane").transform;
            mathRoot.SetParent(playRoot.transform, false);

            panelMeshA = NewDynamicMesh("WingPanelA");
            panelMeshB = NewDynamicMesh("WingPanelB");
            panelAFilter = NewMeshObject("WingFoldLight", panelMeshA, paperIvory, mathRoot).GetComponent<MeshFilter>();
            panelBFilter = NewMeshObject("WingFoldShade", panelMeshB, paperWarm, mathRoot).GetComponent<MeshFilter>();
            panelARenderer = panelAFilter.GetComponent<MeshRenderer>();
            panelBRenderer = panelBFilter.GetComponent<MeshRenderer>();

            fixedBone = MakeLine("BoneAB", mathRoot, MgfLook.Hex("314162"), 0.12f, 2, false);
            movingBone = MakeLine("BoneAC", mathRoot, MgfLook.Hex("314162"), 0.12f, 2, false);
            baseBone = MakeLine("BaseBC", mathRoot, MgfLook.Hex("5878B0"), 0.07f, 2, false);
            angleArc = MakeLine("AngleArc", mathRoot, MgfLook.Hex("DD705A"), 0.08f, 20, false);
            outerArc = MakeLine("TickArc", mathRoot, MgfLook.Hex("314162"), 0.035f, 61, false);
            ticks = MakeLine("Ticks", mathRoot, MgfLook.Hex("314162"), 0.026f, JongisaeRules.TickCount * 2, false);
            equalMarkAB = MakeLine("EqualMarkAB", mathRoot, MgfLook.Hex("DD705A"), 0.075f, 2, false);
            equalMarkAC = MakeLine("EqualMarkAC", mathRoot, MgfLook.Hex("DD705A"), 0.075f, 2, false);
            exteriorLine = MakeLine("ExteriorRay", mathRoot, MgfLook.Hex("DD705A"), 0.055f, 2, false);
            correctGhost = MakeLine("CorrectGhost", mathRoot, MgfLook.Hex("D5F07A"), 0.1f, 3, false);
            correctGhost.enabled = false;
            guidePath = MakeLine("PracticePath", mathRoot, MgfLook.Hex("D5F07A"), 0.075f, 18, false);
            guidePath.enabled = false;
            handleRing = MakeCircle("HandleTrail", mathRoot, 0.58f, MgfLook.Hex("FFE36E"), 0.095f, 40);
            proofMedian = MakeLine("ProofMedianAM", mathRoot, MgfLook.Hex("FFE36E"), 0.085f, 2, false);
            proofMedian.enabled = false;

            for (int i = 0; i < 61; i++)
            {
                float a = (JongisaeRules.MinAngle + i * JongisaeRules.Step) * Mathf.Deg2Rad;
                outerArc.SetPosition(i, PivotLocal + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.02f) * (WingRadius + 0.36f));
                float len = i % 5 == 0 ? 0.23f : 0.11f;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                ticks.SetPosition(i * 2, PivotLocal + d * (WingRadius + 0.24f));
                ticks.SetPosition(i * 2 + 1, PivotLocal + d * (WingRadius + 0.24f + len));
            }
            for (int i = 0; i < tickNumbers.Length; i++)
            {
                int a = 20 + i * 20;
                float r = a * Mathf.Deg2Rad;
                tickNumbers[i] = MgfText.World(a.ToString(), PivotLocal + new Vector3(Mathf.Cos(r), Mathf.Sin(r), -0.03f) * (WingRadius + 0.83f), 2.7f, MgfLook.Hex("314162"), mathRoot);
            }

            handle = MgfLook.Prim(PrimitiveType.Sphere, "WingHandleC", Vector3.zero, Vector3.one * 0.72f, coral, mathRoot, true);
            handle.GetComponent<SphereCollider>().radius = 1.28f;
            labelA = MgfText.World("A", Vector3.zero, 4.0f, MgfLook.Hex("314162"), mathRoot);
            labelB = MgfText.World("B", Vector3.zero, 4.0f, MgfLook.Hex("314162"), mathRoot);
            labelC = MgfText.World("C", Vector3.zero, 4.0f, MgfLook.Hex("314162"), mathRoot);
            labelD = MgfText.World("D", Vector3.zero, 3.8f, MgfLook.Hex("DD705A"), mathRoot);
            angleWorld = MgfText.World("40°", Vector3.zero, 5.2f, MgfLook.Hex("314162"), mathRoot);
            angleWorld.outlineWidth = 0.14f;
            angleWorld.outlineColor = MgfLook.Hex("F8F1DA");
            grabHint = MgfText.World("C를 잡아 화살표대로 끌기", Vector3.zero, 2.55f, MgfLook.Hex("FFE36E"), mathRoot);
            grabHint.outlineWidth = 0.16f;
            grabHint.outlineColor = MgfLook.Hex("203548");
            grabHint.gameObject.SetActive(false);
            proofM = MgfText.World("M", Vector3.zero, 3.4f, MgfLook.Hex("FFE36E"), mathRoot);
            proofM.outlineWidth = 0.14f;
            proofM.outlineColor = MgfLook.Hex("203548");
            proofM.gameObject.SetActive(false);

            guideHand = MgfLook.Prim(PrimitiveType.Sphere, "GuideFinger", Vector3.zero, new Vector3(0.34f, 0.55f, 0.18f), glowMat, mathRoot, false).transform;
            guideHand.gameObject.SetActive(false);

            birdBody = MgfLook.Prim(PrimitiveType.Sphere, "CurrentBirdBody", PivotLocal + new Vector3(-0.52f, -0.2f, 0.22f), new Vector3(1.08f, 0.72f, 0.44f), paperIvory, mathRoot, false).transform;
            birdHead = MgfLook.Prim(PrimitiveType.Sphere, "CurrentBirdHead", PivotLocal + new Vector3(-0.91f, 0.12f, 0.12f), Vector3.one * 0.55f, paperIvory, mathRoot, false).transform;
            birdTail = CreateFoldLeaf("CurrentTail", PivotLocal + new Vector3(-1.28f, -0.25f, 0.2f), new Vector2(0.48f, 0.75f), paperWarm, mathRoot).transform;
            var eye = MgfLook.Prim(PrimitiveType.Sphere, "InkEye", PivotLocal + new Vector3(-1.14f, 0.25f, -0.23f), Vector3.one * 0.12f, inkNavy, mathRoot, false);
            eye.transform.localScale = Vector3.one * 0.12f;
            var beak = CreateTriangleObject("Beak", new Vector3[] { new Vector3(-1.25f, 0.12f, -0.02f), new Vector3(-1.63f, 0.02f, -0.02f), new Vector3(-1.25f, -0.08f, -0.02f) }, coral, mathRoot);
            beak.transform.localPosition += PivotLocal;

            rippleRoot = new GameObject("PaperRipple").transform;
            rippleRoot.SetParent(worldRoot.transform, false);
            for (int i = 0; i < rippleRings.Length; i++) rippleRings[i] = MakeCircle("Ripple" + i, rippleRoot, 0.35f + i * 0.18f, MgfLook.Hex("F8F1DA"), 0.045f, 34);
            rippleRoot.gameObject.SetActive(false);
            UpdateWingGeometry(40);
        }

        void BuildResult()
        {
            resultRoot = new GameObject("ResultWorld");
            resultRoot.transform.SetParent(worldRoot.transform, false);
            MgfLook.Block("ResultBoard", new Vector3(0f, 0.35f, 0.65f), new Vector3(7.25f, 6.15f, 0.22f), 0.18f, leafBlueDark, resultRoot.transform);
            CylinderBetween("ResultTopSeam", new Vector3(-3.1f, 2.75f, 0.25f), new Vector3(3.1f, 2.75f, 0.25f), 0.045f, shootGreen, resultRoot.transform, false);
            for (int i = 0; i < 3; i++)
            {
                var flag = CreateFoldLeaf("ResultFold" + i, new Vector3(-2.15f + i * 2.15f, -1.25f, 0.15f), new Vector2(0.72f, 1.1f), i == 1 ? shootGreen : paperIvory, resultRoot.transform);
                flag.transform.localRotation = Quaternion.Euler(0f, 0f, i == 1 ? 0f : (i == 0 ? -24f : 24f));
            }
            restartBird = CreateBird("RestartBird", 0.78f, resultRoot.transform, true);
            restartBird.transform.localPosition = new Vector3(0f, -2.35f, -0.3f);
            resultRoot.SetActive(false);
        }

        void BuildClips()
        {
            for (int i = 0; i < JongisaeRules.StartLives; i++)
            {
                var root = new GameObject("PaperClip" + i).transform;
                root.SetParent(worldRoot.transform, false);
                root.position = new Vector3(4.5f + i * 0.58f, 4.8f, 0.2f);
                clipLeft[i] = MakeLine("ClipLeft", root, MgfLook.Hex("F8F1DA"), 0.07f, 8, false).transform;
                clipRight[i] = MakeLine("ClipRight", root, MgfLook.Hex("F8F1DA"), 0.07f, 8, false).transform;
                var l = clipLeft[i].GetComponent<LineRenderer>();
                var r = clipRight[i].GetComponent<LineRenderer>();
                for (int k = 0; k < 8; k++)
                {
                    float a = Mathf.Lerp(100f, 260f, k / 7f) * Mathf.Deg2Rad;
                    l.SetPosition(k, new Vector3(Mathf.Cos(a) * 0.18f, Mathf.Sin(a) * 0.34f, 0f));
                    a = Mathf.Lerp(-80f, 80f, k / 7f) * Mathf.Deg2Rad;
                    r.SetPosition(k, new Vector3(Mathf.Cos(a) * 0.18f, Mathf.Sin(a) * 0.34f, 0f));
                }
            }
        }

        void BuildUi()
        {
            Color navy = MgfLook.Hex("314162");
            Color ivory = MgfLook.Hex("F8F1DA");
            timeUi = MgfText.Ui("100초", new Vector2(0f, 1f), new Vector2(52f, -52f), 22f, navy, 92f);
            progressUi = MgfText.Ui("출격 0/8", new Vector2(0.5f, 1f), new Vector2(0f, -52f), 22f, navy, 130f);
            clipsUi = MgfText.Ui("클립 3/3", new Vector2(1f, 1f), new Vector2(-88f, -52f), 21f, navy, 104f);
            scoreUi = MgfText.Ui("점수 0", new Vector2(0f, 1f), new Vector2(55f, -88f), 18f, navy, 105f);
            goalUi = MgfText.Ui("날개 각을 맞춰 놓으면 새가 날아간다", new Vector2(0.5f, 1f), new Vector2(0f, -103f), 20f, navy, 380f);
            goalUi.outlineWidth = 0.13f;
            goalUi.outlineColor = navy;
            conditionUi = MgfText.Ui("", new Vector2(0.5f, 0f), new Vector2(0f, 82f), 20f, navy, 375f);
            conditionUi.color = ivory;
            conditionUi.textWrappingMode = TextWrappingModes.Normal;
            conditionUi.rectTransform.sizeDelta = new Vector2(375f, 78f);
            conditionUi.outlineWidth = 0.18f;
            conditionUi.outlineColor = navy;
            equationUi = MgfText.Ui("", new Vector2(0.5f, 0.5f), new Vector2(0f, 225f), 27f, ivory, 390f);
            equationUi.outlineWidth = 0.18f;
            equationUi.outlineColor = navy;
            equationUi.textWrappingMode = TextWrappingModes.Normal;
            equationUi.rectTransform.sizeDelta = new Vector2(390f, 94f);
            toastUi = MgfText.Ui("", new Vector2(0.5f, 0.5f), new Vector2(0f, -230f), 19f, ivory, 350f);
            toastUi.outlineWidth = 0.18f;
            toastUi.outlineColor = navy;
            resultTitleUi = MgfText.Ui("", new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), 42f, navy, 380f);
            resultTitleUi.color = ivory;
            resultTitleUi.outlineWidth = 0.16f;
            resultTitleUi.outlineColor = navy;
            resultStatsUi = MgfText.Ui("", new Vector2(0.5f, 0.5f), new Vector2(0f, 118f), 24f, navy, 380f);
            resultStatsUi.color = ivory;
            resultStatsUi.textWrappingMode = TextWrappingModes.Normal;
            resultStatsUi.rectTransform.sizeDelta = new Vector2(380f, 210f);
            resultStatsUi.lineSpacing = 18f;
            resultStatsUi.outlineWidth = 0.13f;
            resultStatsUi.outlineColor = navy;
            restartUi = MgfText.Ui("접힌 새를 눌러 다시 출격", new Vector2(0.5f, 0f), new Vector2(0f, 70f), 22f, navy, 350f);
            restartUi.color = ivory;
            restartUi.outlineWidth = 0.12f;
            restartUi.outlineColor = navy;
            leftHudPanel = MakeHudPanel("LeftMissionZone");
            rightHudPanel = MakeHudPanel("RightStatusZone");
        }

        void ResetWorldForRun()
        {
            backgroundRoot.SetActive(true);
            flightActive = false;
            flightBird.SetActive(false);
            flightTrail.enabled = false;
            guideActive = false;
            guidePath.enabled = false;
            guideHand.gameObject.SetActive(false);
            grabHint.gameObject.SetActive(false);
            proofMedian.enabled = false;
            proofM.gameObject.SetActive(false);
            correctGhost.enabled = false;
            exteriorLine.enabled = false;
            resultRoot.SetActive(false);
            rippleRoot.gameObject.SetActive(false);
            rippleClock = 0f;
            wrongClock = 0f;
            resultClock = 0f;
            for (int i = 0; i < sentBirds.Length; i++) sentBirds[i].gameObject.SetActive(false);
            for (int i = 0; i < clipBroken.Length; i++)
            {
                clipBroken[i] = false;
                clipBreakClock[i] = 0f;
                clipLeft[i].localPosition = clipRight[i].localPosition = Vector3.zero;
                clipLeft[i].localRotation = clipRight[i].localRotation = Quaternion.identity;
                clipLeft[i].gameObject.SetActive(true);
                clipRight[i].gameObject.SetActive(true);
            }
            displayedScore = 0f;
            scoreVelocity = 0f;
        }

        void ShowTitleVisual()
        {
            titleClock = 0f;
            backgroundRoot.SetActive(true);
            titleRoot.SetActive(true);
            playRoot.SetActive(false);
            resultRoot.SetActive(false);
            for (int i = 0; i < clipLeft.Length; i++) clipLeft[i].parent.gameObject.SetActive(false);
            ApplyUiLayout((float)Screen.width / Mathf.Max(1, Screen.height) >= 1.25f);
        }

        void ShowPlayVisual()
        {
            backgroundRoot.SetActive(true);
            titleRoot.SetActive(false);
            playRoot.SetActive(true);
            resultRoot.SetActive(false);
            for (int i = 0; i < clipLeft.Length; i++) clipLeft[i].parent.gameObject.SetActive(true);
            ApplyUiLayout((float)Screen.width / Mathf.Max(1, Screen.height) >= 1.25f);
        }

        void ApplyProblemVisual(WingProblem p, bool practice)
        {
            mathRoot.localRotation = Quaternion.identity;
            exteriorLine.enabled = p.band == 3;
            labelD.gameObject.SetActive(p.band == 3);
            correctGhost.enabled = false;
            guideActive = practice;
            guideLarge = false;
            guidePath.enabled = practice;
            guideHand.gameObject.SetActive(practice);
            grabHint.gameObject.SetActive(practice);
            guideClock = 0f;
            panelARenderer.sharedMaterial = paperIvory;
            panelBRenderer.sharedMaterial = paperWarm;
            for (int i = 0; i < bells.Length; i++)
            {
                bells[i].localScale = bellBaseScale[i];
                bells[i].localRotation = Quaternion.identity;
            }
            RefreshProblemUi();
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            string action = current.band == 2 ? "C를 끌어 잘못 펼친 ∠A를 고치시오" : "C를 원호 눈금으로 끌어 ∠A를 만드시오";
            bool land = (float)Screen.width / Mathf.Max(1, Screen.height) >= 1.25f;
            conditionUi.text = land ? current.condition.Replace(" · ", "\n") + "\n\n" + action : current.condition + "\n" + action;
            bool practicePresentation = gamePhase == GamePhase.Practice || (gamePhase == GamePhase.Reveal && revealWasPractice);
            goalUi.text = practicePresentation
                ? "날개 각을 맞춰 놓으면 새가 날아간다"
                : (land ? "첫 출격 6/8\n외각 2/3" : "8마리 출격 · 첫 출격 6회 · 외각 첫 출격 2회");
        }

        void RefreshAllUi()
        {
            bool play = gamePhase == GamePhase.Practice || gamePhase == GamePhase.Playing || gamePhase == GamePhase.Reveal || gamePhase == GamePhase.Bridge;
            bool practicePresentation = gamePhase == GamePhase.Practice || (gamePhase == GamePhase.Reveal && revealWasPractice);
            timeUi.gameObject.SetActive(play && !practicePresentation);
            progressUi.gameObject.SetActive(play && !practicePresentation);
            clipsUi.gameObject.SetActive(play && !practicePresentation);
            scoreUi.gameObject.SetActive(play && !practicePresentation);
            goalUi.gameObject.SetActive(play);
            conditionUi.gameObject.SetActive(play && gamePhase != GamePhase.Bridge);
            equationUi.gameObject.SetActive(play && (gamePhase == GamePhase.Reveal || gamePhase == GamePhase.Bridge));
            bool end = gamePhase == GamePhase.End;
            resultTitleUi.gameObject.SetActive(end);
            resultStatsUi.gameObject.SetActive(end);
            restartUi.gameObject.SetActive(end);
            if (play) { RefreshHud(); RefreshProblemUi(); }
        }

        void RefreshHud()
        {
            timeUi.text = Mathf.CeilToInt(timeLeft) + "초";
            progressUi.text = "출격 " + st.solved + "/8";
            clipsUi.text = "클립 " + Mathf.Max(0, st.lives) + "/3";
            scoreUi.text = "점수 " + Mathf.RoundToInt(displayedScore);
        }

        void RefreshAngleUi()
        {
            if (!angleWorld) return;
            angleWorld.text = st.inputAngle + "°";
        }

        void UpdateWingGeometry(int angle)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 a = PivotLocal;
            Vector3 b = PivotLocal + Vector3.right * WingRadius;
            Vector3 c = PivotLocal + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * WingRadius;
            Vector3 m = (b + c) * 0.5f;
            SetTriangle(panelMeshA, a + Vector3.forward * 0.05f, m + Vector3.forward * 0.03f, b + Vector3.forward * 0.05f);
            SetTriangle(panelMeshB, a, c, m);
            fixedBone.SetPosition(0, a + Vector3.back * 0.03f); fixedBone.SetPosition(1, b + Vector3.back * 0.03f);
            movingBone.SetPosition(0, a + Vector3.back * 0.04f); movingBone.SetPosition(1, c + Vector3.back * 0.04f);
            baseBone.SetPosition(0, b); baseBone.SetPosition(1, c);
            handle.transform.localPosition = c + Vector3.back * 0.16f;
            handleRing.transform.localPosition = c + Vector3.back * 0.18f;
            grabHint.transform.localPosition = c + new Vector3(0f, 0.92f, -0.2f);
            labelA.transform.localPosition = a + new Vector3(-0.28f, -0.32f, -0.1f);
            labelB.transform.localPosition = b + new Vector3(0.25f, -0.25f, -0.1f);
            labelC.transform.localPosition = c + new Vector3(Mathf.Cos(rad) * 0.32f, Mathf.Sin(rad) * 0.32f, -0.1f);
            float half = angle * 0.5f * Mathf.Deg2Rad;
            angleWorld.transform.localPosition = a + new Vector3(Mathf.Cos(half), Mathf.Sin(half), -0.16f) * 1.08f;
            Vector3 mid = (b + c) * 0.5f;
            proofMedian.SetPosition(0, a + Vector3.back * 0.1f);
            proofMedian.SetPosition(1, mid + Vector3.back * 0.1f);
            proofM.transform.localPosition = mid + new Vector3(0.2f, 0.18f, -0.14f);

            for (int i = 0; i < angleArc.positionCount; i++)
            {
                float t = i / (float)(angleArc.positionCount - 1);
                float q = angle * t * Mathf.Deg2Rad;
                angleArc.SetPosition(i, a + new Vector3(Mathf.Cos(q), Mathf.Sin(q), -0.05f) * 0.78f);
            }
            SetEqualMark(equalMarkAB, a, b);
            SetEqualMark(equalMarkAC, a, c);

            Vector3 d = b + (b - c).normalized * 1.25f;
            exteriorLine.SetPosition(0, c);
            exteriorLine.SetPosition(1, d);
            labelD.transform.localPosition = d + new Vector3(0.15f, -0.12f, -0.08f);
        }

        void SetEqualMark(LineRenderer line, Vector3 a, Vector3 b)
        {
            Vector3 d = (b - a).normalized;
            Vector3 p = Vector3.Cross(Vector3.forward, d).normalized * 0.16f;
            Vector3 c = Vector3.Lerp(a, b, 0.54f);
            line.SetPosition(0, c - p);
            line.SetPosition(1, c + p);
        }

        void SetGrabbed(bool grabbed)
        {
            handle.transform.localScale = Vector3.one * (grabbed ? 0.88f : 0.72f);
            handle.GetComponent<Renderer>().sharedMaterial = grabbed ? glowMat : coral;
            handlePulseClock = grabbed ? 0.6f : 0f;
            if (grabbed) birdHead.localRotation = Quaternion.Euler(0f, 0f, -8f);
            else birdHead.localRotation = Quaternion.identity;
        }

        void ReplayPracticeGuide(bool large)
        {
            if (gamePhase != GamePhase.Practice) return;
            guideActive = true;
            guideLarge = large;
            guideStartAngle = st.inputAngle;
            guideClock = 0f;
            guidePath.enabled = true;
            guideHand.gameObject.SetActive(true);
            for (int i = 0; i < guidePath.positionCount; i++)
            {
                float t = i / (float)(guidePath.positionCount - 1);
                float a = Mathf.Lerp(guideStartAngle, 70f, t) * Mathf.Deg2Rad;
                guidePath.SetPosition(i, PivotLocal + new Vector3(Mathf.Cos(a), Mathf.Sin(a), -0.19f) * WingRadius);
            }
            PulseHandle(true);
        }

        void PulseHandle(bool strong)
        {
            handlePulseClock = strong ? 1.4f : 0.8f;
        }

        void HidePracticeGuideWhileDragging()
        {
            if (gamePhase != GamePhase.Practice) return;
            guideActive = false;
            guidePath.enabled = false;
            guideHand.gameObject.SetActive(false);
            grabHint.gameObject.SetActive(false);
        }

        void BeginRevealVisual(bool ok, bool wasFirst, WingProblem p)
        {
            guideActive = false;
            guidePath.enabled = false;
            guideHand.gameObject.SetActive(false);
            equationUi.text = ok
                ? JongisaeRules.Equation(p)
                : JongisaeRules.WrongReason(p, st.misconceptionId, st.inputIndex) + "\n" + JongisaeRules.Equation(p);
            equationUi.color = ok ? MgfLook.Hex("F8F1DA") : MgfLook.Hex("FFE6C7");
            if (ok)
            {
                panelARenderer.sharedMaterial = glowMat;
                panelBRenderer.sharedMaterial = paperIvory;
                flightActive = true;
                flightClock = 0f;
                flightStart = mathRoot.TransformPoint(PivotLocal + new Vector3(-0.45f, -0.1f, -0.25f));
                // 새가 원형 관문을 통과하는 대신 중앙 장력막을 가로질러 봉제선을 완성한다.
                flightEnd = new Vector3(3.35f, 3.55f, 0.2f);
                flightBird.SetActive(true);
                flightBird.transform.position = flightStart;
                flightBird.transform.localScale = Vector3.one * 0.62f;
                SetFlightWingAngle(st.inputAngle);
                for (int i = 0; i < trailHistory.Length; i++) trailHistory[i] = flightStart;
                flightTrail.enabled = true;
                MgfSfx.Play("correct", 0.48f);
                if (revealWasPractice)
                {
                    proofMedian.enabled = true;
                    proofM.gameObject.SetActive(true);
                    practiceProofStage = -1;
                }
            }
            else
            {
                wrongClock = 0.55f;
                ShowCorrectGhost(p.q);
                BreakClip(Mathf.Clamp(st.lives, 0, JongisaeRules.StartLives - 1));
                MgfSfx.Play("wrong", 0.42f);
            }
        }

        void FinishRevealVisual(bool ok)
        {
            panelARenderer.sharedMaterial = paperIvory;
            panelBRenderer.sharedMaterial = paperWarm;
            correctGhost.enabled = false;
            proofMedian.enabled = false;
            proofM.gameObject.SetActive(false);
            equationUi.text = "";
            if (ok)
            {
                flightActive = false;
                flightBird.SetActive(false);
                flightTrail.enabled = false;
                int index = Mathf.Clamp(st.launches - 1, 0, sentBirds.Length - 1);
                sentBirds[index].gameObject.SetActive(true);
                sentBirds[index].localScale = Vector3.zero;
            }
        }

        void UpdatePracticeProof(float clock, WingProblem p)
        {
            int stage = clock < 0.72f ? 0 : (clock < 1.46f ? 1 : 2);
            if (stage == practiceProofStage) return;
            practiceProofStage = stage;
            if (stage == 0) equationUi.text = "M은 ∠A의 이등분선과\nBC가 만난 점";
            else if (stage == 1) equationUi.text = "AB=AC · AM은 공통\n∠BAM=∠MAC";
            else equationUi.text = "△ABM≡△ACM (SAS) → ∠B=∠C\n" + p.b + "°+" + p.b + "°+" + p.q + "°=180°";
        }

        void ShowCorrectGhost(int correctAngle)
        {
            float r = correctAngle * Mathf.Deg2Rad;
            Vector3 c = PivotLocal + new Vector3(Mathf.Cos(r), Mathf.Sin(r), -0.12f) * WingRadius;
            Vector3 b = PivotLocal + Vector3.right * WingRadius + Vector3.back * 0.12f;
            correctGhost.SetPosition(0, b);
            correctGhost.SetPosition(1, PivotLocal + Vector3.back * 0.12f);
            correctGhost.SetPosition(2, c);
            correctGhost.enabled = true;
        }

        void ShowWrongReason(WingProblem p, string id)
        {
            conditionUi.text = JongisaeRules.WrongReason(p, id, st.inputIndex) + "\n같은 새의 날개 끝 C를 다시 펼치시오";
            ShowToast("현재 날개는 그대로 남아 있다", 1.8f);
        }

        void ShowBridge(int nextBand)
        {
            equationUi.gameObject.SetActive(true);
            if (nextBand == 2)
                equationUi.text = "같은 두 변 AB·AC 사이가 꼭지각이다\n도형이 돌아가도 표식부터 찾으시오";
            else
                equationUi.text = "외각과 밑각의 합은 180°\n밑각을 찾은 뒤 꼭지각으로 거슬러 가시오";
            equationUi.color = MgfLook.Hex("F8F1DA");
            MgfSfx.Play("whoosh", 0.32f);
        }

        void ShowResultVisual(bool clear, string reason)
        {
            titleRoot.SetActive(false);
            playRoot.SetActive(false);
            backgroundRoot.SetActive(false);
            resultRoot.SetActive(true);
            resultClock = 0f;
            for (int i = 0; i < clipLeft.Length; i++) clipLeft[i].parent.gameObject.SetActive(false);
            resultTitleUi.text = clear ? "대열 완성" : (reason == "mastery" ? "출격 완료 · 각 관계 재연습" : "가지에서 멈춤");
            string why = reason == "time" ? "100초가 끝났다" : reason == "clips" ? "종이 클립을 모두 썼다" : reason == "mastery" ? "완주는 했지만 첫 출격 숙련 기준이 남았다" : "여덟 마리가 같은 길로 선회했다";
            resultStatsUi.text = "총 출격      " + st.solved + "/8\n첫 출격      " + st.firstAttemptCorrect + "/8\n외각 첫 출격  " + st.exteriorFirstCorrect + "/3";
            restartUi.text = why + "\n접힌 새를 눌러 다시 출격";
            leftHudPanel.gameObject.SetActive(false);
            rightHudPanel.gameObject.SetActive(false);
            ApplyUiLayout((float)Screen.width / Mathf.Max(1, Screen.height) >= 1.25f);
        }

        void RingBell(int index, int combo)
        {
            if (index < 0 || index >= bells.Length) return;
            activeBell = index;
            bellFlashClock = 0.34f + Mathf.Min(0.16f, combo * 0.02f);
            bells[index].localRotation = Quaternion.Euler(0f, 0f, index == 1 ? 0f : (index == 0 ? -9f : 9f));
            MgfFx.Glow(bells[index].position, MgfLook.Hex("D5F07A"), 5 + Mathf.Min(8, combo * 2), 0.26f);
            MgfSfx.Play(index == 0 ? "tap" : "pop", 0.18f + index * 0.07f);
        }

        void BreakClip(int index)
        {
            if (index < 0 || index >= clipBroken.Length || clipBroken[index]) return;
            clipBroken[index] = true;
            clipBreakClock[index] = 0.01f;
        }

        void RefuseInput(string message, Vector2 screen)
        {
            ShowToast(message, 1.45f);
            var ray = cam.ScreenPointToRay(screen);
            var plane = new Plane(-cam.transform.forward, new Vector3(0f, 1.5f, 0f));
            if (plane.Raycast(ray, out float d)) rippleRoot.position = ray.GetPoint(d) - cam.transform.forward * 0.1f;
            rippleRoot.rotation = cam.transform.rotation;
            rippleRoot.localScale = Vector3.one * 0.25f;
            rippleRoot.gameObject.SetActive(true);
            rippleClock = 0.5f;
            MgfSfx.Play("tap", 0.11f);
            MgfBridge.NotifyChanged();
        }

        void ShowToast(string message, float seconds)
        {
            toastUi.text = message;
            toastUi.gameObject.SetActive(true);
            toastClock = seconds;
        }

        void AnimateWorld(float dt)
        {
            float t = Time.time;
            for (int i = 0; i < leaves.Count; i++)
            {
                var l = leaves[i];
                l.t.rotation = l.baseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.72f + l.phase) * l.amount);
            }
            for (int i = 0; i < ambientBirds.Count; i++)
            {
                var b = ambientBirds[i];
                b.position = ambientBirdBase[i] + Vector3.up * (Mathf.Sin(t * 1.25f + i) * 0.11f);
                b.localRotation = Quaternion.Euler(0f, (i & 1) == 0 ? -15f : 18f, Mathf.Sin(t * 1.8f + i) * 5f);
            }
            if (gamePhase == GamePhase.Title)
            {
                titleClock += dt;
                titleBird.transform.localPosition = new Vector3(0.2f, -0.45f + Mathf.Sin(titleClock * 1.7f) * 0.09f, -0.45f);
                titleBird.transform.localScale = Vector3.one * (1f + Mathf.Sin(titleClock * 1.1f) * 0.025f);
                cam.orthographicSize = targetOrtho - Mathf.Min(0.24f, titleClock * 0.025f);
            }
            else cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrtho, 1f - Mathf.Exp(-dt * 6f));

            if (gamePhase == GamePhase.Practice && guideActive && guideHand.gameObject.activeSelf)
            {
                guideClock += dt;
                float p = Mathf.PingPong(guideClock / 1.2f, 1f);
                float a = Mathf.Lerp(guideStartAngle, 70f, Smooth01(p)) * Mathf.Deg2Rad;
                guideHand.localPosition = PivotLocal + new Vector3(Mathf.Cos(a), Mathf.Sin(a), -0.28f) * WingRadius;
                float s = (guideLarge ? 1.32f : 1f) * (0.9f + Mathf.Sin(guideClock * 8f) * 0.08f);
                guideHand.localScale = new Vector3(0.34f, 0.55f, 0.18f) * s;
            }

            float pulse = 1f + Mathf.Sin(t * 5.2f) * 0.08f;
            if (handlePulseClock > 0f) { handlePulseClock -= dt; pulse += 0.16f * Mathf.Clamp01(handlePulseClock * 2f); }
            handleRing.transform.localScale = Vector3.one * pulse;
            handleRing.enabled = gamePhase == GamePhase.Practice || st.dragging || handlePulseClock > 0f;

            birdTail.localRotation = Quaternion.Euler(0f, 0f, -18f + Mathf.Sin(t * 2.3f) * 8f);
            birdBody.localScale = new Vector3(1.08f, 0.72f + Mathf.Sin(t * 2.1f) * 0.025f, 0.44f);
            if (Mathf.Repeat(t, 3.4f) < 0.12f) birdHead.localScale = new Vector3(1f, 0.88f, 1f); else birdHead.localScale = Vector3.one;

            if (flightActive) AnimateFlight(dt);
            for (int i = 0; i < sentBirds.Length; i++)
                if (sentBirds[i].gameObject.activeSelf)
                {
                    sentBirds[i].localScale = Vector3.Lerp(sentBirds[i].localScale, Vector3.one, 1f - Mathf.Exp(-dt * 8f));
                    sentBirds[i].position += Vector3.up * (Mathf.Sin(t * 2f + i) * 0.0008f);
                }

            if (wrongClock > 0f) wrongClock = Mathf.Max(0f, wrongClock - dt);
            if (bellFlashClock > 0f && activeBell >= 0)
            {
                bellFlashClock -= dt;
                float p = 1f + Mathf.Sin((0.4f - bellFlashClock) * 28f) * bellFlashClock * 0.8f;
                bells[activeBell].localScale = bellBaseScale[activeBell] * Mathf.Max(0.8f, p);
                if (bellFlashClock <= 0f) bells[activeBell].localScale = bellBaseScale[activeBell];
            }
            for (int i = 0; i < clipBroken.Length; i++)
            {
                if (!clipBroken[i]) continue;
                clipBreakClock[i] += dt;
                float p = Mathf.Clamp01(clipBreakClock[i] / 0.42f);
                clipLeft[i].localPosition = new Vector3(-0.14f * p, -0.22f * p, 0f);
                clipRight[i].localPosition = new Vector3(0.16f * p, -0.28f * p, 0f);
                clipLeft[i].localRotation = Quaternion.Euler(0f, 0f, 26f * p);
                clipRight[i].localRotation = Quaternion.Euler(0f, 0f, -31f * p);
            }
            if (rippleClock > 0f)
            {
                rippleClock -= dt;
                rippleRoot.localScale = Vector3.one * Mathf.Lerp(0.25f, 1.6f, 1f - rippleClock / 0.5f);
                if (rippleClock <= 0f) rippleRoot.gameObject.SetActive(false);
            }
            if (resultRoot.activeSelf)
            {
                resultClock += dt;
                float s = 0.82f + Smooth01(Mathf.Clamp01(resultClock / 0.55f)) * 0.18f;
                resultRoot.transform.localScale = Vector3.one * s;
                restartBird.transform.localPosition = new Vector3(0f, -2.5f + Mathf.Sin(t * 2f) * 0.08f, -0.3f);
            }
        }

        void AnimateFlight(float dt)
        {
            flightClock += dt;
            if (flightClock < 0.12f)
            {
                float p = flightClock / 0.12f;
                flightBird.transform.position = flightStart + cam.transform.up * (-0.18f * Mathf.Sin(p * Mathf.PI));
                flightBird.transform.localScale = new Vector3(0.72f, 0.48f, 0.68f);
            }
            else
            {
                float p = Mathf.Clamp01((flightClock - 0.12f) / 0.82f);
                Vector3 control = (flightStart + flightEnd) * 0.5f + cam.transform.up * 3.1f;
                float u = 1f - p;
                Vector3 pos = u * u * flightStart + 2f * u * p * control + p * p * flightEnd;
                flightBird.transform.position = pos;
                float stretch = 1f + Mathf.Sin(Mathf.Min(1f, p * 2f) * Mathf.PI) * 0.16f;
                flightBird.transform.localScale = new Vector3(0.62f * stretch, 0.62f / stretch, 0.62f);
                for (int i = trailHistory.Length - 1; i > 0; i--) trailHistory[i] = trailHistory[i - 1];
                trailHistory[0] = pos;
                for (int i = 0; i < trailHistory.Length; i++) flightTrail.SetPosition(i, trailHistory[i]);
            }
        }

        void SetFlightWingAngle(int angle)
        {
            float open = Mathf.InverseLerp(JongisaeRules.MinAngle, JongisaeRules.MaxAngle, angle);
            if (flightLeftWing) flightLeftWing.localRotation = Quaternion.Euler(0f, 0f, 24f + open * 34f);
            if (flightRightWing) flightRightWing.localRotation = Quaternion.Euler(0f, 180f, -24f - open * 34f);
        }

        void UpdateAnimatedUi(float dt)
        {
            displayedScore = Mathf.SmoothDamp(displayedScore, st.score, ref scoreVelocity, 0.11f, 2000f, dt);
            if (scoreUi.gameObject.activeSelf) scoreUi.text = "점수 " + Mathf.RoundToInt(displayedScore);
            if (toastClock > 0f)
            {
                toastClock -= dt;
                float s = 1f + Mathf.Sin(Mathf.Clamp01(toastClock * 3f) * Mathf.PI) * 0.05f;
                toastUi.rectTransform.localScale = Vector3.one * s;
                if (toastClock <= 0f) toastUi.gameObject.SetActive(false);
            }
        }

        void ApplyResponsiveLayout()
        {
            if (!cam) return;
            bool sizeChanged = lastScreenW != Screen.width || lastScreenH != Screen.height;
            lastScreenW = Screen.width;
            lastScreenH = Screen.height;
            float aspect = Mathf.Max(0.1f, (float)Screen.width / Mathf.Max(1, Screen.height));
            bool land = aspect >= 1.25f;
            targetOrtho = land ? 6.2f : 7.35f;
            Vector3 center = new Vector3(0f, 1.6f, 0f);
            titleRoot.transform.position = center;
            titleRoot.transform.rotation = cam.transform.rotation;
            titleRoot.transform.localScale = Vector3.one * (land ? 1.08f : 0.92f);
            resultRoot.transform.position = center;
            resultRoot.transform.rotation = cam.transform.rotation;
            Vector3 offset = cam.transform.right * (land ? 0.45f : 0.68f) + cam.transform.up * (land ? -0.25f : -0.32f);
            if (wrongClock > 0f) offset += cam.transform.right * Mathf.Sin(wrongClock * 72f) * wrongClock * 0.22f;
            int rotation = current == null ? 0 : current.rotation;
            float playScale = land ? 0.92f : 0.72f;
            Vector3 basePosition = center + offset;
            // 회전은 화면 중앙이 아니라 꼭지점 A를 축으로 한다. 원호 전체가 세로 390px에서도
            // 같은 안전 영역에 남아 회전된 밴드의 손잡이가 화면 밖으로 나가지 않는다.
            Vector3 anchoredPivot = basePosition + cam.transform.rotation * (PivotLocal * playScale);
            mathRoot.position = basePosition;
            mathRoot.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, rotation * 90f);
            mathRoot.localScale = Vector3.one * playScale;
            mathRoot.position += anchoredPivot - mathRoot.TransformPoint(PivotLocal);
            Quaternion counter = Quaternion.Euler(0f, 0f, -rotation * 90f);
            labelA.transform.localRotation = counter;
            labelB.transform.localRotation = counter;
            labelC.transform.localRotation = counter;
            labelD.transform.localRotation = counter;
            angleWorld.transform.localRotation = counter;
            grabHint.transform.localRotation = counter;
            proofM.transform.localRotation = counter;
            for (int i = 0; i < tickNumbers.Length; i++) tickNumbers[i].transform.localRotation = counter;
            if (sizeChanged) ApplyUiLayout(land);
        }

        void ApplyUiLayout(bool land)
        {
            bool sideHud = land && (gamePhase == GamePhase.Practice || gamePhase == GamePhase.Playing || gamePhase == GamePhase.Reveal || gamePhase == GamePhase.Bridge);
            leftHudPanel.gameObject.SetActive(sideHud);
            rightHudPanel.gameObject.SetActive(sideHud);
            if (land)
            {
                SetUi(goalUi, new Vector2(0.16f, 0.82f), Vector2.zero, new Vector2(185f, 86f), 15f, MgfLook.Hex("203548"));
                SetUi(conditionUi, new Vector2(0.16f, 0.48f), Vector2.zero, new Vector2(190f, 285f), 15.5f, MgfLook.Hex("203548"));
                SetUi(timeUi, new Vector2(0.84f, 0.77f), Vector2.zero, new Vector2(180f, 52f), 24f, MgfLook.Hex("203548"));
                SetUi(progressUi, new Vector2(0.84f, 0.64f), Vector2.zero, new Vector2(180f, 52f), 22f, MgfLook.Hex("203548"));
                SetUi(clipsUi, new Vector2(0.84f, 0.51f), Vector2.zero, new Vector2(180f, 52f), 21f, MgfLook.Hex("203548"));
                SetUi(scoreUi, new Vector2(0.84f, 0.38f), Vector2.zero, new Vector2(180f, 48f), 19f, MgfLook.Hex("203548"));
                equationUi.rectTransform.anchorMin = equationUi.rectTransform.anchorMax = new Vector2(0.5f, 0.82f);
                equationUi.rectTransform.anchoredPosition = Vector2.zero;
                equationUi.rectTransform.sizeDelta = new Vector2(510f, 120f);
                equationUi.fontSize = 23f;
            }
            else
            {
                SetUi(timeUi, new Vector2(0f, 1f), new Vector2(50f, -44f), new Vector2(88f, 38f), 20f, MgfLook.Hex("314162"));
                SetUi(progressUi, new Vector2(0.5f, 1f), new Vector2(-5f, -44f), new Vector2(120f, 38f), 20f, MgfLook.Hex("314162"));
                SetUi(clipsUi, new Vector2(1f, 1f), new Vector2(-123f, -44f), new Vector2(100f, 38f), 19f, MgfLook.Hex("314162"));
                SetUi(scoreUi, new Vector2(0f, 1f), new Vector2(52f, -82f), new Vector2(100f, 30f), 16f, MgfLook.Hex("314162"));
                SetUi(goalUi, new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(372f, 34f), 17f, MgfLook.Hex("F8F1DA"));
                SetUi(conditionUi, new Vector2(0.5f, 0f), new Vector2(0f, 82f), new Vector2(375f, 92f), 20f, MgfLook.Hex("F8F1DA"));
                equationUi.rectTransform.anchorMin = equationUi.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                equationUi.rectTransform.anchoredPosition = new Vector2(0f, 225f);
                equationUi.rectTransform.sizeDelta = new Vector2(356f, 112f);
                equationUi.fontSize = 21f;
            }

            if (land)
            {
                SetUi(resultTitleUi, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(620f, 72f), 40f, MgfLook.Hex("F8F1DA"));
                SetUi(resultStatsUi, new Vector2(0.5f, 0.49f), Vector2.zero, new Vector2(540f, 205f), 23f, MgfLook.Hex("F8F1DA"));
                SetUi(restartUi, new Vector2(0.5f, 0.17f), Vector2.zero, new Vector2(520f, 76f), 18f, MgfLook.Hex("F8F1DA"));
            }
            else
            {
                SetUi(resultTitleUi, new Vector2(0.5f, 0.73f), Vector2.zero, new Vector2(360f, 70f), 36f, MgfLook.Hex("F8F1DA"));
                SetUi(resultStatsUi, new Vector2(0.5f, 0.49f), Vector2.zero, new Vector2(350f, 230f), 21f, MgfLook.Hex("F8F1DA"));
                SetUi(restartUi, new Vector2(0.5f, 0.15f), Vector2.zero, new Vector2(350f, 82f), 17f, MgfLook.Hex("F8F1DA"));
            }
        }

        static void SetUi(TextMeshProUGUI ui, Vector2 anchor, Vector2 offset, Vector2 size, float fontSize, Color color)
        {
            ui.rectTransform.anchorMin = ui.rectTransform.anchorMax = anchor;
            ui.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ui.rectTransform.anchoredPosition = offset;
            ui.rectTransform.sizeDelta = size;
            ui.fontSize = fontSize;
            ui.color = color;
            ui.alignment = TextAlignmentOptions.Center;
        }

        Image MakeHudPanel(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(MgfText.Canvas.transform, false);
            go.transform.SetAsFirstSibling();
            var image = go.GetComponent<Image>();
            image.color = new Color(0.97f, 0.94f, 0.82f, 0.78f);
            image.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            bool left = name.StartsWith("Left", StringComparison.Ordinal);
            rt.anchorMin = new Vector2(left ? 0.025f : 0.745f, 0.2f);
            rt.anchorMax = new Vector2(left ? 0.295f : 0.975f, 0.88f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return image;
        }

        static float Smooth01(float t) => t * t * (3f - 2f * t);

        GameObject CreateBird(string name, float scale, Transform parent, bool collider)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var body = MgfLook.Prim(PrimitiveType.Sphere, "Body", Vector3.zero, new Vector3(1.15f, 0.72f, 0.62f) * scale, paperIvory, root.transform, collider);
            var head = MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(-0.55f, 0.28f, -0.02f) * scale, Vector3.one * 0.55f * scale, paperIvory, root.transform, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Eye", new Vector3(-0.76f, 0.39f, -0.27f) * scale, Vector3.one * 0.11f * scale, inkNavy, root.transform, false);
            var beak = CreateTriangleObject("Beak", new Vector3[] { new Vector3(-0.78f, 0.31f, -0.03f), new Vector3(-1.18f, 0.22f, -0.03f), new Vector3(-0.78f, 0.12f, -0.03f) }, coral, root.transform);
            beak.transform.localScale = Vector3.one * scale;
            var left = CreateFoldLeaf("WingLeft", new Vector3(0.18f, 0.35f, -0.21f) * scale, new Vector2(0.75f, 1.35f) * scale, paperWarm, root.transform);
            left.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
            var right = CreateFoldLeaf("WingRight", new Vector3(0.2f, 0.25f, 0.24f) * scale, new Vector2(0.68f, 1.2f) * scale, paperIvory, root.transform);
            right.transform.localRotation = Quaternion.Euler(0f, 180f, -32f);
            var tail = CreateFoldLeaf("Tail", new Vector3(0.72f, -0.08f, 0.08f) * scale, new Vector2(0.42f, 0.8f) * scale, paperWarm, root.transform);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, -78f);
            if (collider)
            {
                var c = body.GetComponent<SphereCollider>();
                c.radius = 1.45f;
            }
            return root;
        }

        GameObject CreateTensionRig(string name, Vector3 pos, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            CylinderBetween("Hanger", Vector3.zero, Vector3.up * 1.25f, 0.024f, inkNavy, root.transform, false);
            CreateTriangleObject("LeftPleat", new[] { new Vector3(0f, 0.55f, 0f), new Vector3(-0.82f, -0.2f, 0f), new Vector3(0f, -0.92f, -0.08f) }, paperIvory, root.transform);
            CreateTriangleObject("RightPleat", new[] { new Vector3(0f, 0.55f, 0f), new Vector3(0f, -0.92f, -0.08f), new Vector3(0.82f, -0.2f, 0f) }, paperWarm, root.transform);
            CylinderBetween("CenterSpar", new Vector3(0f, 0.62f, -0.12f), new Vector3(0f, -0.96f, -0.12f), 0.035f, inkNavy, root.transform, false);
            CylinderBetween("CrossSpar", new Vector3(-0.82f, -0.2f, -0.12f), new Vector3(0.82f, -0.2f, -0.12f), 0.028f, coral, root.transform, false);
            var stitch = CreateTriangleObject("TensionTab", new[] { new Vector3(-0.13f, -0.98f, 0f), new Vector3(0.13f, -0.98f, 0f), new Vector3(0f, -1.32f, 0f) }, shootGreen, root.transform);
            stitch.transform.localPosition = Vector3.zero;
            return root;
        }

        GameObject CreateFoldLeaf(string name, Vector3 pos, Vector2 size, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var mesh = new Mesh { name = name + "Mesh" };
            float w = size.x * 0.5f, h = size.y * 0.5f, ridge = Mathf.Min(size.x, size.y) * 0.12f;
            var baseVertices = new[] { new Vector3(0f, h, 0f), new Vector3(w, 0f, 0f), new Vector3(0f, -h, 0f), new Vector3(-w, 0f, 0f), new Vector3(0f, 0f, -ridge) };
            int[] faces = { 4, 1, 0, 4, 2, 1, 4, 3, 2, 4, 0, 3, 0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0, 4 };
            var vertices = new Vector3[faces.Length];
            var normals = new Vector3[faces.Length];
            var uvs = new Vector2[faces.Length];
            var triangles = new int[faces.Length];
            for (int i = 0; i < faces.Length; i += 3)
            {
                Vector3 a = baseVertices[faces[i]], b = baseVertices[faces[i + 1]], c = baseVertices[faces[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                for (int k = 0; k < 3; k++)
                {
                    Vector3 v = baseVertices[faces[i + k]];
                    vertices[i + k] = v;
                    normals[i + k] = normal;
                    uvs[i + k] = new Vector2(v.x / Mathf.Max(0.001f, size.x) + 0.5f, v.y / Mathf.Max(0.001f, size.y) + 0.5f);
                    triangles[i + k] = i + k;
                }
            }
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uvs; mesh.triangles = triangles; mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = mat; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; renderer.receiveShadows = true;
            return go;
        }

        GameObject CreateTriangleObject(string name, Vector3[] vertices, Material mat, Transform parent)
        {
            var mesh = new Mesh { name = name + "Mesh" };
            Vector3 a = vertices[0], b = vertices[1], c = vertices[2];
            mesh.vertices = new[] { a, b, c, a, c, b };
            Vector3 front = Vector3.Cross(b - a, c - a).normalized;
            mesh.normals = new[] { front, front, front, -front, -front, -front };
            mesh.triangles = new[] { 0, 1, 2, 3, 4, 5 };
            mesh.RecalculateBounds();
            return NewMeshObject(name, mesh, mat, parent);
        }

        Mesh NewDynamicMesh(string name)
        {
            var m = new Mesh { name = name };
            m.MarkDynamic();
            m.vertices = new Vector3[6];
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.forward, Vector3.forward, Vector3.forward };
            m.triangles = new[] { 0, 1, 2, 3, 4, 5 };
            return m;
        }

        GameObject NewMeshObject(string name, Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true;
            return go;
        }

        void SetTriangle(Mesh mesh, Vector3 a, Vector3 b, Vector3 c)
        {
            mesh.vertices = new[] { a, b, c, a, c, b };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.forward, Vector3.forward, Vector3.forward };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f)
            };
            mesh.RecalculateBounds();
        }

        LineRenderer MakeLine(string name, Transform parent, Color color, float width, int count, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = loop;
            lr.positionCount = count;
            lr.startWidth = lr.endWidth = width;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
            lr.sharedMaterial = MgfLook.Unlit(color);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        LineRenderer MakeCircle(string name, Transform parent, float radius, Color color, float width, int count)
        {
            var lr = MakeLine(name, parent, color, width, count, true);
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
            return lr;
        }

        GameObject CylinderBetween(string name, Vector3 a, Vector3 b, float radius, Material mat, Transform parent, bool world = true)
        {
            Vector3 mid = (a + b) * 0.5f;
            var go = MgfLook.Prim(PrimitiveType.Cylinder, name, mid, new Vector3(radius, Vector3.Distance(a, b) * 0.5f, radius), mat, parent, false);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
            return go;
        }
    }
}
