// 현악 네모 — 햇빛 기하 음악원의 황동 현·원목 공명판 무대.
// 두 꼭짓점을 직접 끌어 대각선의 이등분·같은 길이·수직 조건을 구성한다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.HyeonakNemo
{
    public sealed class HyeonakNemoGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int round;
            public int firstAttempts;
            public int firstCorrect;
            public int attemptsOnCurrent;
            public int ringBRadius;
            public int ringBDirection;
            public int ringDRadius;
            public int ringDDirection;
            public int ringBScreenX;
            public int ringBScreenY;
            public int ringDScreenX;
            public int ringDScreenY;
            public int boardCenterScreenX;
            public int boardCenterScreenY;
            public bool ringBChanged;
            public bool ringDChanged;
            public string problemId = "";
            public int inputSerial;
            public string misconceptionId = "";
        }

        enum RunPhase { Title, Practice, Playing, Reveal, End }

        static readonly Color Basalt = MgfLook.Hex("#C98B52");
        static readonly Color Basalt2 = MgfLook.Hex("#F1DFC1");
        static readonly Color Copper = MgfLook.Hex("#C94B3D");
        static readonly Color CopperHot = MgfLook.Hex("#E2A83B");
        static readonly Color Teal = MgfLook.Hex("#197B78");
        static readonly Color TealGlow = MgfLook.Hex("#67C7BF");
        static readonly Color Ivory = MgfLook.Hex("#FFF8E8");
        static readonly Color Violet = MgfLook.Hex("#345DA8");
        static readonly Color Amber = MgfLook.Hex("#F2C14E");
        static readonly Color Ink = MgfLook.Hex("#26333A");

        readonly State st = new State();
        readonly List<FrameProblem> bank = new List<FrameProblem>(300);
        readonly List<FrameProblem> band1 = new List<FrameProblem>();
        readonly List<FrameProblem> band2 = new List<FrameProblem>();
        readonly List<FrameProblem> band3 = new List<FrameProblem>();
        readonly Transform[] pitchNodes = new Transform[6];
        readonly Renderer[] pitchNodeRenderers = new Renderer[6];
        readonly Transform[] spectrumCells = new Transform[6];
        readonly Vector3[] spectrumCellHomes = new Vector3[6];
        readonly Transform[] fuses = new Transform[3];
        readonly Transform[] savedFrames = new Transform[6];
        readonly Transform[] waveLocks = new Transform[6];
        readonly LineRenderer[] radialLines = new LineRenderer[10];
        readonly LineRenderer[] measureRings = new LineRenderer[10];
        readonly LineRenderer[] rippleLines = new LineRenderer[5];
        readonly float[] rippleTimes = { 9, 9, 9, 9, 9 };

        Camera cam;
        Transform boardRoot, handleB, handleD, fixedA, fixedC, pinO;
        Transform beetle, beetleShell, baton;
        LineRenderer frameLine, diagAC, diagBD, resonanceTrail, ghostLine, guideLine;
        Renderer handleBR, handleDR, shellRenderer;
        Material basaltMat, basaltLightMat, copperMat, copperHotMat, tealMat, tealGlowMat, ivoryMat, violetMat, amberMat, dimCopperMat;
        FrameProblem current;
        RunPhase phase = RunPhase.Title;
        System.Random rng;
        int runSerial;
        int b1Cursor, b2Cursor, b3Cursor;
        bool draggingB, draggingD, dragMoved, firstAttempt;
        float secondsLeft = 110f;
        float revealT, idleGuide, titleT, hitStop, wrongJolt, trailT;
        bool revealCorrect, revealWasFirst;
        Vector3 correctGhostB, correctGhostD;
        int rippleCursor;

        CanvasGroup titleGroup, hudGroup, endGroup, toastGroup;
        RectTransform titlePanel, ctaPanel, goalPanel, promptPanel, statusPanel, progressPanel, fusePanel, magnifierPanel, endPanel, toastPanel;
        TextMeshProUGUI titleText, tagText, badgeText, bestText, ctaText;
        TextMeshProUGUI goalText, promptText, hudText, progressText, fuseText, measureText, feedbackText;
        TextMeshProUGUI endTitle, endStats, endScore, endCta;
        TextMeshPro labelA, labelC, labelB, labelD;
        Image[] fuseImages = new Image[3];
        Image ringBGuide, ringDGuide, ringBTarget, ringDTarget, guideHandB, guideHandD, guideArrowB, guideArrowD;
        TextMeshProUGUI guideTextB, guideTextD;
        float displayedScore, toastT;
        float lastLayoutW = -1, lastLayoutH = -1;
        Sprite roundedSprite, squareSprite, ringSprite, arrowSprite, handSprite;
        int practiceMisses;
        bool tutorialAssistActive, tutorialAssistB;
        float tutorialAssistT, tutorialAssistDelay;
        Vector3 beetleHome;

        const float Unit = 0.285f;

        void Awake()
        {
            MgfLook.Quality(34f);
            bank.AddRange(HyeonakRules.BuildBank());
            for (int i = 0; i < bank.Count; i++)
            {
                if (bank[i].band == 1) band1.Add(bank[i]);
                else if (bank[i].band == 2) band2.Add(bank[i]);
                else if (bank[i].band == 3) band3.Add(bank[i]);
            }
            BuildMaterials();
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void BuildMaterials()
        {
            basaltMat = MgfLook.Lit(Basalt, .20f, .01f);
            basaltLightMat = MgfLook.Lit(Basalt2, .12f, .01f);
            copperMat = MgfLook.Lit(Copper, .24f, .06f);
            copperHotMat = MgfLook.Lit(CopperHot, .42f, .16f);
            dimCopperMat = MgfLook.Lit(MgfLook.Hex("#8A7562"), .10f, .02f);
            tealMat = MgfLook.Lit(Teal, .24f, .03f);
            tealGlowMat = MgfLook.Lit(TealGlow, .32f, .03f);
            ivoryMat = MgfLook.Lit(Ivory, .12f, .01f);
            violetMat = MgfLook.Lit(Violet, .20f, .02f);
            amberMat = MgfLook.Lit(Amber, .28f, .03f);
        }

        void BuildWorld()
        {
            // 코드로 만드는 Capsule/Sphere 프리미티브의 Collider 타입을 WebGL 스트리핑에서 보존한다.
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<SphereCollider>();
            Destroy(colliderTypes);
            MgfLook.Sky(MgfLook.Hex("#76BDD1"), MgfLook.Hex("#F1D9A9"), MgfLook.Hex("#F7E9CE"), .84f);
            MgfLook.Sun(new Vector3(42, -28, 14), MgfLook.Hex("#FFF1C4"), 1.05f, .70f);
            cam = MgfLook.Camera(new Vector3(7.7f, 6.4f, -16.5f), new Vector3(0, 1.15f, 0), 34f);
            cam.orthographic = true;
            cam.orthographicSize = 7.35f;

            // 햇빛이 드는 기하 음악원. 검은 계기판 대신 층진 원목 무대와 실제 마림바 건반이 세계를 채운다.
            MgfLook.Block("MapleConservatoryFloor", new Vector3(0, -1.05f, 3.8f), new Vector3(24, .9f, 20), .12f, basaltMat);
            for (int side = -1; side <= 1; side += 2)
            for (int row = 0; row < 5; row++)
            {
                float x = side * (4.7f + row * 1.55f);
                float y = -.45f + row * .64f;
                float z = 2.3f + row * 1.15f;
                var rack = MgfLook.Block("MarimbaTerrace" + side + "_" + row, new Vector3(x, y, z),
                    new Vector3(3.15f + row * .28f, .48f, 3.2f), .07f, row % 2 == 0 ? basaltLightMat : ivoryMat);
                rack.transform.localRotation = Quaternion.Euler(0, side * (4 + row * 2), 0);
                MakeMarimbaBank(new Vector3(x - side * .7f, y + .58f, z - .8f), side, row);
            }
            for (int i = 0; i < 18; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * (5.2f + (i % 5) * 1.14f);
                float y = .1f + (i % 4) * .58f;
                float z = -1.8f + (i % 7) * 1.2f;
                var mobile = MgfLook.Block("AcousticMobile" + i, new Vector3(x, y, z),
                    new Vector3(.13f, .38f + (i % 3) * .18f, .09f), .018f,
                    i % 3 == 0 ? amberMat : (i % 3 == 1 ? violetMat : copperMat));
                mobile.transform.localRotation = Quaternion.Euler(0, side * 12, side * (8 + i % 4 * 4));
            }
            // 중앙 악기 아래에 박힌 황동 박자 눈금.
            for (int i = 0; i < 9; i++)
            {
                float x = -5.6f + i * 1.4f;
                MgfLook.Block("BrassBeatInlay" + i, new Vector3(x, -.46f, -2.15f), new Vector3(.62f, .06f, .12f), .018f,
                    i % 2 == 0 ? amberMat : copperHotMat);
            }
            MgfLook.Block("StartPianoKey", new Vector3(0, -.44f, -3.0f), new Vector3(4.4f, .22f, 1.0f), .045f, copperHotMat);

            BuildResonanceGallery();
            BuildBeetle();
            BuildBoard();
            BuildRipples();
        }

        void MakeMarimbaBank(Vector3 center, float side, int seed)
        {
            var root = new GameObject("MarimbaBank" + side + "_" + seed).transform;
            root.position = center;
            MgfLook.Block("BrassAxle", new Vector3(0, .05f, 0), new Vector3(1.55f, .10f, .62f), .025f, copperHotMat, root);
            for (int i = 0; i < 5; i++)
            {
                float h = .32f + ((i + seed) % 4) * .22f;
                MgfLook.Block("WoodenToneKey", new Vector3((i - 2) * .27f, h * .5f + .12f, 0),
                    new Vector3(.16f, h, .18f), .018f, i % 3 == 0 ? tealMat : (i % 3 == 1 ? violetMat : copperMat), root);
            }
        }

        void BuildResonanceGallery()
        {
            for (int i = 0; i < 6; i++)
            {
                float side = i < 3 ? -1f : 1f;
                float lane = i % 3;
                Vector3 center = new Vector3(side * (4.55f + lane * 1.25f), .4f + lane * 1.1f, 1.8f + lane * .65f);
                var root = new GameObject("ScoreTileRack" + i).transform;
                root.position = center;
                spectrumCells[i] = root;
                spectrumCellHomes[i] = center;
                MgfLook.Block("BirchScoreTile", Vector3.zero, new Vector3(1.75f, 2.18f, .16f), .035f, basaltLightMat, root);
                MgfLook.Block("BrassRailTop", new Vector3(0, .82f, -.14f), new Vector3(1.35f, .08f, .08f), .018f, copperHotMat, root);
                MgfLook.Block("BrassRailBottom", new Vector3(0, -.82f, -.14f), new Vector3(1.35f, .08f, .08f), .018f, copperHotMat, root);
                var node = MgfLook.Block("MarimbaKey" + i, new Vector3(0, 0, -.22f),
                    new Vector3(.58f, .22f, .10f), .025f, i % 2 == 0 ? violetMat : copperMat, root);
                pitchNodes[i] = node.transform;
                pitchNodeRenderers[i] = node.GetComponent<Renderer>();
                savedFrames[i] = MakeMiniFrame(root, new Vector3(0, -1.0f, 0));
                savedFrames[i].gameObject.SetActive(false);
                waveLocks[i] = MakePunchedScore(root, new Vector3(0, .05f, -.28f));
                waveLocks[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < 3; i++)
            {
                var fuse = MgfLook.Prim(PrimitiveType.Cylinder, "SpareMallet" + i,
                    new Vector3(-.65f + i * .65f, -.28f, -2.2f), new Vector3(.055f, .44f, .055f), copperHotMat);
                fuse.transform.rotation = Quaternion.Euler(90, 0, 0);
                fuses[i] = fuse.transform;
            }
        }

        Transform MakePunchedScore(Transform parent, Vector3 local)
        {
            var root = new GameObject("PunchedQuadrilateralScore").transform;
            root.SetParent(parent, false); root.localPosition = local;
            MgfLook.Block("StampedCard", Vector3.zero, new Vector3(1.20f, .86f, .08f), .025f, ivoryMat, root);
            for (int i = 0; i < 8; i++)
            {
                float a = (45 + i * 45) * Mathf.Deg2Rad;
                var hole = MgfLook.Prim(PrimitiveType.Cylinder, "ScorePunch", new Vector3(Mathf.Cos(a) * .38f, Mathf.Sin(a) * .25f, -.08f),
                    new Vector3(.09f, .04f, .09f), tealMat, root, false);
                hole.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            return root;
        }

        Transform MakeMiniFrame(Transform parent, Vector3 local)
        {
            var root = new GameObject("SavedFrame").transform;
            root.SetParent(parent, false); root.localPosition = local;
            for (int i = 0; i < 4; i++)
            {
                float a = (45 + i * 90) * Mathf.Deg2Rad;
                var peg = MgfLook.Prim(PrimitiveType.Sphere, "MiniPeg", new Vector3(Mathf.Cos(a) * .42f, Mathf.Sin(a) * .42f, 0),
                    Vector3.one * .09f, copperHotMat, root, false);
                peg.transform.localScale = Vector3.one * .09f;
            }
            var lr = MakeLine("MiniPhaseFrame", tealMat, .035f, root);
            lr.useWorldSpace = false; lr.positionCount = 5;
            for (int i = 0; i < 5; i++)
            {
                float a = (45 + (i % 4) * 90) * Mathf.Deg2Rad;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * .42f, Mathf.Sin(a) * .42f, 0));
            }
            return root;
        }

        void BuildBeetle()
        {
            beetle = new GameObject("TealStoneBeetleConductor").transform;
            beetle.position = new Vector3(0, -.1f, -2.15f);
            beetleHome = beetle.position;
            beetleShell = MgfLook.Prim(PrimitiveType.Sphere, "RoundedTealShell", new Vector3(0, .52f, 0),
                new Vector3(1.05f, .56f, .82f), tealMat, beetle).transform;
            shellRenderer = beetleShell.GetComponent<Renderer>();
            MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0, .42f, -.70f), new Vector3(.53f, .38f, .48f), basaltLightMat, beetle);
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 3; i++)
            {
                var leg = MgfLook.Prim(PrimitiveType.Capsule, "Leg" + side + "_" + i,
                    new Vector3(side * (.62f + i * .05f), .22f, -.35f + i * .36f), new Vector3(.13f, .35f, .13f), basaltMat, beetle, false);
                leg.transform.localRotation = Quaternion.Euler(0, 0, side * (42 + i * 7));
            }
            baton = MgfLook.Prim(PrimitiveType.Cylinder, "CopperBaton", new Vector3(.66f, 1.04f, -.10f),
                new Vector3(.055f, .70f, .055f), copperHotMat, beetle, false).transform;
            baton.localRotation = Quaternion.Euler(0, 0, -28);
            var hit = beetle.gameObject.AddComponent<BoxCollider>();
            hit.center = new Vector3(0, .55f, 0); hit.size = new Vector3(2.2f, 1.8f, 1.8f);
        }

        void BuildBoard()
        {
            boardRoot = new GameObject("MapleStringSoundboard").transform;
            boardRoot.position = new Vector3(0, 1.35f, .25f);
            // TMP/board front(-Z)가 카메라를 향하도록 +Z를 카메라 반대쪽으로 둔다.
            boardRoot.rotation = Quaternion.LookRotation(boardRoot.position - cam.transform.position, Vector3.up);
            MgfLook.Block("MapleSoundboardFrame", Vector3.zero, new Vector3(7.0f, 6.15f, .34f), .20f, basaltMat, boardRoot);
            MgfLook.Block("BrassSoundboardBezel", new Vector3(0, 0, -.20f), new Vector3(6.55f, 5.70f, .10f), .12f, copperHotMat, boardRoot);
            MgfLook.Block("IvoryStringChart", new Vector3(0, 0, -.27f), new Vector3(6.30f, 5.45f, .08f), .08f, ivoryMat, boardRoot);

            for (int i = 0; i < radialLines.Length; i++)
                radialLines[i] = MakeLine("RadialScale" + i, MgfLook.Alpha(new Color(Ink.r, Ink.g, Ink.b, .28f)), .018f, boardRoot);
            for (int i = 0; i < measureRings.Length; i++)
            {
                measureRings[i] = MakeLine("MeasureRing" + (i + 1), MgfLook.Alpha(new Color(Ink.r, Ink.g, Ink.b, i % 5 == 4 ? .62f : .25f)),
                    i % 5 == 4 ? .025f : .014f, boardRoot);
                measureRings[i].useWorldSpace = false; measureRings[i].positionCount = 49;
                float r = (i + 1) * Unit;
                for (int j = 0; j <= 48; j++)
                {
                    float a = j * Mathf.PI * 2 / 48;
                    measureRings[i].SetPosition(j, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, -.34f));
                }
            }

            frameLine = MakeLine("QuadrilateralString", copperMat, .075f, boardRoot);
            frameLine.positionCount = 5;
            diagAC = MakeLine("ActualDiagonalAC", MgfLook.Alpha(new Color(Teal.r, Teal.g, Teal.b, .92f)), .038f, boardRoot);
            diagAC.positionCount = 2;
            diagBD = MakeLine("ActualDiagonalBD", MgfLook.Alpha(new Color(Violet.r, Violet.g, Violet.b, .92f)), .042f, boardRoot);
            diagBD.positionCount = 2;
            resonanceTrail = MakeLine("TealResonanceTrail", MgfLook.Additive(new Color(TealGlow.r, TealGlow.g, TealGlow.b, .85f)), .12f, boardRoot);
            resonanceTrail.positionCount = 2; resonanceTrail.enabled = false;
            ghostLine = MakeLine("CorrectionGhost", MgfLook.Alpha(new Color(TealGlow.r, TealGlow.g, TealGlow.b, .58f)), .055f, boardRoot);
            ghostLine.positionCount = 2; ghostLine.enabled = false;
            guideLine = MakeLine("PracticeDragPath", MgfLook.Additive(new Color(TealGlow.r, TealGlow.g, TealGlow.b, .64f)), .045f, boardRoot);
            guideLine.positionCount = 2; guideLine.enabled = false;

            fixedA = MakePeg("A", ivoryMat, false);
            fixedC = MakePeg("C", ivoryMat, false);
            handleB = MakePeg("B", copperHotMat, true);
            handleD = MakePeg("D", copperHotMat, true);
            handleBR = handleB.GetComponent<Renderer>(); handleDR = handleD.GetComponent<Renderer>();
            pinO = MakePeg("O", tealGlowMat, false);
            pinO.localScale = Vector3.one * .23f;

            labelA = MakeLabel("A", fixedA, new Vector3(0, .46f, -.15f));
            labelC = MakeLabel("C", fixedC, new Vector3(0, .46f, -.15f));
            labelB = MakeLabel("B", handleB, new Vector3(0, .54f, -.15f));
            labelD = MakeLabel("D", handleD, new Vector3(0, .54f, -.15f));
            MakeLabel("중앙 핀 O", pinO, new Vector3(0, -.46f, -.15f), 2.4f);
        }

        Transform MakePeg(string name, Material mat, bool movable)
        {
            var go = MgfLook.Prim(PrimitiveType.Sphere, name + "VertexRing", Vector3.zero,
                movable ? new Vector3(.38f, .38f, .22f) : new Vector3(.28f, .28f, .20f), mat, boardRoot);
            if (movable)
            {
                var col = go.GetComponent<SphereCollider>(); col.radius = .75f;
                MgfLook.Block(name + "CaliperJawLeft", new Vector3(-.34f, 0, -.11f), new Vector3(.12f, .52f, .08f), .025f, copperHotMat, go.transform);
                MgfLook.Block(name + "CaliperJawRight", new Vector3(.34f, 0, -.11f), new Vector3(.12f, .52f, .08f), .025f, copperHotMat, go.transform);
                MgfLook.Block(name + "CaliperBridge", new Vector3(0, .23f, -.11f), new Vector3(.58f, .10f, .08f), .025f, copperHotMat, go.transform);
            }
            return go.transform;
        }

        TextMeshPro MakeLabel(string text, Transform parent, Vector3 pos, float size = 3.2f)
        {
            var t = MgfText.World(text, pos, size, Ink, parent);
            t.fontStyle = FontStyles.Bold;
            t.outlineWidth = .18f; t.outlineColor = new Color32(255, 248, 232, 255);
            return t;
        }

        void BuildRipples()
        {
            for (int i = 0; i < rippleLines.Length; i++)
            {
                rippleLines[i] = MakeLine("InvalidTapRipple" + i, MgfLook.Alpha(new Color(Ivory.r, Ivory.g, Ivory.b, .70f)), .045f, boardRoot);
                rippleLines[i].useWorldSpace = false; rippleLines[i].positionCount = 33; rippleLines[i].enabled = false;
            }
        }

        LineRenderer MakeLine(string name, Material mat, float width, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat; lr.startWidth = lr.endWidth = width;
            lr.useWorldSpace = true; lr.numCapVertices = 5; lr.numCornerVertices = 3;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
            return lr;
        }

        void MakeEllipse(string name, Transform parent, float rx, float ry, float width, Material mat, int count, float z = 0)
        {
            var lr = MakeLine(name, mat, width, parent);
            lr.useWorldSpace = false; lr.positionCount = count + 1;
            for (int i = 0; i <= count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, z));
            }
        }

        void BuildUi()
        {
            roundedSprite = MakeSprite(64, 15, false);
            squareSprite = MakeSprite(64, 3, false);
            ringSprite = MakeSprite(96, 0, true);
            arrowSprite = MakeArrowSprite(128, 48);
            handSprite = MakeHandSprite(64);
            Transform root = MgfText.Canvas.transform;

            titlePanel = Full("TitleScreen", root); titleGroup = titlePanel.gameObject.AddComponent<CanvasGroup>();
            var plaque = Panel(titlePanel, "HangingScoreBanner", new Vector2(.5f, .78f), Vector2.zero, new Vector2(354, 168), new Color(Ivory.r, Ivory.g, Ivory.b, .97f), squareSprite);
            plaque.localRotation = Quaternion.Euler(0, 0, -2.2f);
            titleText = Txt(plaque, "현악 네모", new Vector2(.5f, .61f), Vector2.zero, 54, Violet, 330);
            titleText.outlineWidth = .14f; titleText.outlineColor = new Color32(255, 248, 232, 255);
            titleText.fontStyle = FontStyles.Bold;
            tagText = Txt(plaque, "꼭짓점을 끌어 대각선을 조율하라", new Vector2(.5f, .24f), Vector2.zero, 16, Teal, 330);
            badgeText = Txt(titlePanel, "중2 · 사각형의 성질", new Vector2(.5f, .62f), Vector2.zero, 14, Ink, 280);
            bestText = Txt(titlePanel, "", new Vector2(.5f, .26f), Vector2.zero, 14, Ink, 300);
            ctaPanel = Panel(titlePanel, "BrassPianoKey", new Vector2(.5f, .14f), Vector2.zero, new Vector2(330, 72), CopperHot, squareSprite);
            ctaText = Txt(ctaPanel, "황동 건반을 눌러 시작", new Vector2(.5f, .5f), Vector2.zero, 22, Ink, 310);

            var hud = Full("Hud", root); hudGroup = hud.gameObject.AddComponent<CanvasGroup>();
            goalPanel = Panel(hud, "ScoreStrip", new Vector2(.5f, 1f), new Vector2(0, -49), new Vector2(368, 54), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), squareSprite);
            goalText = Txt(goalPanel, "두 꼭짓점을 맞춰 공명판을 완성하시오", new Vector2(.5f, .5f), Vector2.zero, 16, Ink, 344);
            promptPanel = Panel(hud, "PromptScore", new Vector2(.5f, 1f), new Vector2(0, -116), new Vector2(372, 72), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), squareSprite);
            promptText = Txt(promptPanel, "", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 350);
            statusPanel = Panel(hud, "StatusStrip", new Vector2(.5f, 1f), new Vector2(0, -166), new Vector2(372, 42), new Color(Basalt2.r, Basalt2.g, Basalt2.b, .94f), squareSprite);
            hudText = Txt(hud, "", new Vector2(.5f, 1f), new Vector2(0, -166), 15, Ink, 350);
            progressPanel = Panel(hud, "PerforatedProgress", new Vector2(.5f, 0f), new Vector2(0, 80), new Vector2(372, 56), new Color(Ivory.r, Ivory.g, Ivory.b, .94f), squareSprite);
            progressText = Txt(hud, "", new Vector2(.5f, 0f), new Vector2(0, 80), 18, Ink, 360);
            fusePanel = Panel(hud, "MalletTray", new Vector2(.5f, 0f), new Vector2(0, 34), new Vector2(330, 46), new Color(Basalt2.r, Basalt2.g, Basalt2.b, .94f), squareSprite);
            fuseText = Txt(hud, "남은 말렛", new Vector2(.18f, 0f), new Vector2(0, 34), 13, Ink, 110);
            for (int i = 0; i < 3; i++)
                fuseImages[i] = Img(hud, "Fuse" + i, new Vector2(.36f + i * .11f, 0), new Vector2(0, 36), new Vector2(30, 8), CopperHot);
            magnifierPanel = Panel(hud, "MeasureCard", new Vector2(.5f, 0f), new Vector2(0, 140), new Vector2(300, 54), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), squareSprite);
            measureText = Txt(magnifierPanel, "", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 284);
            ringBGuide = Img(hud, "RingBGuide", new Vector2(.5f, .5f), Vector2.zero, new Vector2(72, 72), TealGlow, ringSprite);
            ringDGuide = Img(hud, "RingDGuide", new Vector2(.5f, .5f), Vector2.zero, new Vector2(72, 72), CopperHot, ringSprite);
            ringBTarget = Img(hud, "RingBTarget", new Vector2(.5f, .5f), Vector2.zero, new Vector2(88, 88), Teal, ringSprite);
            ringDTarget = Img(hud, "RingDTarget", new Vector2(.5f, .5f), Vector2.zero, new Vector2(88, 88), Violet, ringSprite);
            guideArrowB = Img(hud, "ArrowC", new Vector2(.5f, .5f), Vector2.zero, new Vector2(126, 48), new Color(Teal.r, Teal.g, Teal.b, .90f), arrowSprite);
            guideArrowD = Img(hud, "ArrowD", new Vector2(.5f, .5f), Vector2.zero, new Vector2(126, 48), new Color(Violet.r, Violet.g, Violet.b, .90f), arrowSprite);
            guideHandB = Img(hud, "HandC", new Vector2(.5f, .5f), Vector2.zero, new Vector2(50, 50), Ivory, handSprite);
            guideHandD = Img(hud, "HandD", new Vector2(.5f, .5f), Vector2.zero, new Vector2(50, 50), Ivory, handSprite);
            guideTextB = Txt(hud, "C → 4 cm", new Vector2(.5f, .5f), Vector2.zero, 13, Ink, 100);
            guideTextD = Txt(hud, "D → 7 cm", new Vector2(.5f, .5f), Vector2.zero, 13, Ink, 100);
            guideTextB.outlineWidth = guideTextD.outlineWidth = .24f;
            guideTextB.outlineColor = guideTextD.outlineColor = new Color32(255, 248, 232, 255);
            SetPracticeGuidesVisible(false);

            toastPanel = Panel(hud, "FeedbackScore", new Vector2(.5f, .5f), new Vector2(0, 205), new Vector2(352, 76), new Color(Ink.r, Ink.g, Ink.b, .97f), squareSprite);
            toastGroup = toastPanel.gameObject.AddComponent<CanvasGroup>();
            feedbackText = Txt(toastPanel, "", new Vector2(.5f, .5f), Vector2.zero, 18, Ivory, 330);

            endPanel = Full("EndScreen", root); endGroup = endPanel.gameObject.AddComponent<CanvasGroup>();
            var resultPlaque = Panel(endPanel, "ResultScore", new Vector2(.5f, .58f), Vector2.zero, new Vector2(360, 300), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), squareSprite);
            endTitle = Txt(resultPlaque, "", new Vector2(.5f, .78f), Vector2.zero, 38, TealGlow, 330);
            endScore = Txt(resultPlaque, "", new Vector2(.5f, .52f), Vector2.zero, 54, Ink, 330);
            endStats = Txt(resultPlaque, "", new Vector2(.5f, .25f), Vector2.zero, 18, Ink, 330);
            var retry = Panel(endPanel, "RetryKey", new Vector2(.5f, .28f), Vector2.zero, new Vector2(330, 72), CopperHot, squareSprite);
            endCta = Txt(retry, "다시 조율하기", new Vector2(.5f, .5f), Vector2.zero, 23, Ink, 310);
        }

        void Prewarm()
        {
            var s = new StringBuilder("현악네모꼭짓점을끌어대각선을조율하라중학교2학년사각형의성질황동건반을눌러시작손잡이공명판악보타일평행사변형직사각형마름모정사각형중앙핀교점서로다른것을이등분수직선분의길이는반대방향첫시도남은말렛남은시간완성다시조율하기연습C와D옮기시오cm");
            for (int i = 0; i < bank.Count; i++) { s.Append(bank[i].prompt); s.Append(bank[i].answer); }
            MgfText.Prewarm(s.ToString());
        }

        void ShowTitle()
        {
            phase = RunPhase.Title;
            st.phase = "title"; st.score = 0; st.lives = 3; st.level = 1; st.solved = 0;
            st.round = 0; st.firstAttempts = 0; st.firstCorrect = 0; st.attemptsOnCurrent = 0;
            st.inputSerial = 0; st.misconceptionId = ""; displayedScore = 0;
            current = HyeonakRules.Practice(); SetProblem(current, true);
            titleGroup.alpha = 1; titleGroup.blocksRaycasts = false;
            hudGroup.alpha = 0; endGroup.alpha = 0; toastGroup.alpha = 0;
            bestText.text = PlayerPrefs.GetInt("hyeonak-nemo.bestFirst", 0) > 0
                ? "최고 첫 시도 " + PlayerPrefs.GetInt("hyeonak-nemo.bestFirst", 0) + "/6" : "악보 타일 6장을 완성하라";
            for (int i = 0; i < 6; i++)
            {
                savedFrames[i].gameObject.SetActive(false); waveLocks[i].gameObject.SetActive(false);
                pitchNodeRenderers[i].sharedMaterial = ivoryMat;
            }
            for (int i = 0; i < 3; i++) { fuses[i].gameObject.SetActive(true); fuses[i].localPosition = new Vector3(-.65f + i * .65f, -.28f, -2.2f); }
            SetPracticeGuidesVisible(false); tutorialAssistActive = false; beetle.position = beetleHome;
            MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount + runSerial * 7919));
            Shuffle(band1); Shuffle(band2); Shuffle(band3);
            b1Cursor = b2Cursor = b3Cursor = 0;
            st.score = 0; st.lives = HyeonakRules.StartLives; st.level = 1; st.solved = 0;
            st.round = 0; st.firstAttempts = 0; st.firstCorrect = 0; st.attemptsOnCurrent = 0;
            st.inputSerial = 0; st.misconceptionId = ""; displayedScore = 0;
            secondsLeft = 110f; firstAttempt = true; revealT = 0; idleGuide = 0;
            practiceMisses = 0; tutorialAssistActive = false; tutorialAssistT = 0; tutorialAssistDelay = 0;
            beetle.position = beetleHome;
            for (int i = 0; i < 6; i++)
            {
                savedFrames[i].gameObject.SetActive(false); waveLocks[i].gameObject.SetActive(false);
                pitchNodeRenderers[i].sharedMaterial = ivoryMat;
            }
            for (int i = 0; i < 3; i++) { fuses[i].gameObject.SetActive(true); fuses[i].localPosition = new Vector3(-.65f + i * .65f, -.28f, -2.2f); fuses[i].localRotation = Quaternion.Euler(90, 0, 0); }
        }

        void StartPractice()
        {
            ResetRun(); phase = RunPhase.Practice; st.phase = "playing";
            current = HyeonakRules.Practice(); SetProblem(current, true);
            titleGroup.alpha = 0; hudGroup.alpha = 1; endGroup.alpha = 0;
            goalText.text = "C와 D 손잡이를 끌어 두 대각선을 이등분하시오";
            SetPracticeGuidesVisible(true);
            MgfSfx.Play("whoosh", .30f); MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun(); phase = RunPhase.Playing; st.phase = "playing";
            titleGroup.alpha = 0; hudGroup.alpha = 1; endGroup.alpha = 0;
            SetPracticeGuidesVisible(false);
            NextProblem(); MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            st.score = 0; st.solved = 0; st.round = 0; st.firstAttempts = 0; st.firstCorrect = 0;
            st.lives = HyeonakRules.StartLives; st.attemptsOnCurrent = 0; secondsLeft = 110f;
            phase = RunPhase.Playing; st.phase = "playing";
            SetPracticeGuidesVisible(false); tutorialAssistActive = false; beetle.position = beetleHome;
            goalText.text = "두 손잡이를 놓아 주문한 사각형을 만드시오";
            NextProblem();
        }

        void NextProblem()
        {
            if (st.solved >= HyeonakRules.TargetSolved) { EndRun(st.firstCorrect >= 4); return; }
            st.round = st.solved + 1;
            st.level = st.round <= 2 ? 1 : st.round <= 4 ? 2 : 3;
            current = st.level == 1 ? band1[b1Cursor++ % band1.Count]
                : st.level == 2 ? band2[b2Cursor++ % band2.Count]
                : band3[b3Cursor++ % band3.Count];
            if (st.round == 5)
            {
                // 다섯 번째는 반드시 마름모, 여섯 번째는 정사각형.
                int guard = 0;
                while (current.kind != FrameKind.Rhombus && guard++ < band3.Count) current = band3[b3Cursor++ % band3.Count];
            }
            else if (st.round == 6)
            {
                current = band3.Find(p => p.kind == FrameKind.Square && p.a == 6) ?? band3[band3.Count - 1];
            }
            firstAttempt = true; st.attemptsOnCurrent = 0; st.misconceptionId = "";
            SetProblem(current, false);
        }

        void SetProblem(FrameProblem p, bool practice)
        {
            st.problemId = p.id; st.ringBChanged = false; st.ringDChanged = false;
            bool firstBand = p.kind == FrameKind.Practice || p.kind == FrameKind.Parallelogram;
            labelA.text = "A"; labelC.text = firstBand ? "B" : "C";
            labelB.text = firstBand ? "C" : "B"; labelD.text = "D";
            if (p.kind == FrameKind.Practice)
            {
                st.ringBRadius = 2; st.ringDRadius = 5;
                st.ringBDirection = p.baseK + 6; st.ringDDirection = p.baseK + 8;
            }
            else if (p.kind == FrameKind.Parallelogram)
            {
                st.ringBRadius = p.a == 10 ? 8 : p.a + 1;
                st.ringDRadius = p.a; // 오개념: 두 대각선 길이를 같게 둔다.
                st.ringBDirection = p.baseK + 6; st.ringDDirection = p.baseK + 8;
            }
            else if (p.kind == FrameKind.Rectangle)
            {
                st.ringBRadius = p.a; st.ringDRadius = p.a;
                st.ringBDirection = p.baseK + 2; st.ringDDirection = p.baseK + 9; // 길이는 같지만 반대가 아님.
            }
            else if (p.kind == FrameKind.Rhombus)
            {
                st.ringBRadius = Mathf.Max(1, p.b - 1); st.ringDRadius = Mathf.Min(10, p.b + 1);
                st.ringBDirection = p.baseK + 3; st.ringDDirection = p.baseK + 9; // 수직·전체 길이만 맞고 이등분 아님.
            }
            else
            {
                st.ringBRadius = Mathf.Max(1, p.a - 1); st.ringDRadius = p.a;
                st.ringBDirection = p.baseK + 2; st.ringDDirection = p.baseK + 8;
            }
            UpdateGeometry(); RefreshText();
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            titleT += dt;
            if (hitStop > 0) hitStop -= dt;
            HandleInput();
            if (phase == RunPhase.Playing)
            {
                secondsLeft -= dt;
                if (secondsLeft <= 0) { secondsLeft = 0; EndRun(false); }
            }
            if (tutorialAssistActive) UpdateTutorialAssist(dt);
            if (phase == RunPhase.Practice && !tutorialAssistActive)
            {
                idleGuide += dt; tutorialAssistDelay += dt;
                bool firstIdle = !st.ringBChanged && idleGuide >= 4.5f;
                bool secondReady = st.ringBChanged && !st.ringDChanged && tutorialAssistDelay >= .8f;
                if (firstIdle || secondReady) BeginTutorialAssist();
            }
            if (phase == RunPhase.Reveal)
            {
                revealT += dt;
                trailT += dt;
                bool firstMainExplanation = current.kind == FrameKind.Parallelogram && st.round == 1 && st.firstAttempts <= 1;
                float revealDuration = current.kind == FrameKind.Practice && revealCorrect ? 2.55f
                    : firstMainExplanation ? 3.2f
                    : (revealCorrect ? 1.18f : .92f);
                if (revealT >= revealDuration) FinishReveal();
            }
            else if (toastT > 0)
            {
                toastT -= dt;
                if (toastT <= 0) toastGroup.alpha = 0;
            }
            UpdatePresentation(dt);
            UpdateUi();
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                if (phase == RunPhase.Title) { StartPractice(); return; }
                if (phase == RunPhase.End) { StartRunDirect(); return; }
                if (phase == RunPhase.Reveal) { InvalidTap(); return; }
                if (phase != RunPhase.Practice && phase != RunPhase.Playing) return;

                // 안내가 떠도 빈 공간을 고리로 오인하지 않도록 화면 히트박스는 손잡이 주변에만 둔다.
                Vector2 pointer = MgfPointer.Position;
                Vector2 bScreen = new Vector2(st.ringBScreenX, Screen.height - st.ringBScreenY);
                Vector2 dScreen = new Vector2(st.ringDScreenX, Screen.height - st.ringDScreenY);
                float db = Vector2.Distance(pointer, bScreen), dd = Vector2.Distance(pointer, dScreen);
                float hitRadius = phase == RunPhase.Practice ? 64f : 54f;
                if (Mathf.Min(db, dd) <= hitRadius)
                {
                    draggingB = db <= dd; draggingD = !draggingB; dragMoved = false; idleGuide = 0;
                    MgfSfx.Play("tap", .16f); return;
                }
                if (MgfPointer.DownHit(cam, out var hit))
                {
                    if (hit.collider.transform == handleB) { draggingB = true; draggingD = false; dragMoved = false; idleGuide = 0; MgfSfx.Play("tap", .16f); return; }
                    if (hit.collider.transform == handleD) { draggingD = true; draggingB = false; dragMoved = false; idleGuide = 0; MgfSfx.Play("tap", .16f); return; }
                }
                InvalidTap();
            }
            if ((draggingB || draggingD) && MgfPointer.Held && ScreenToBoard(out var local))
            {
                ApplyDrag(draggingB, local); dragMoved = true;
            }
            if ((draggingB || draggingD) && MgfPointer.Up)
            {
                bool wasB = draggingB;
                draggingB = draggingD = false;
                if (dragMoved)
                {
                    if (wasB) st.ringBChanged = true; else st.ringDChanged = true;
                    st.inputSerial++; MgfSfx.Play("pop", .20f);
                    if (phase == RunPhase.Practice)
                        SetPracticeGuidesVisible(true);
                    if (st.ringBChanged && st.ringDChanged) Submit();
                    MgfBridge.NotifyChanged();
                }
                else InvalidTap();
            }
        }

        void BeginTutorialAssist()
        {
            bool firstMain = phase == RunPhase.Playing && st.round == 1 && st.firstAttempts == 0 && st.attemptsOnCurrent == 0;
            if ((phase != RunPhase.Practice && !firstMain) || tutorialAssistActive || (st.ringBChanged && st.ringDChanged)) return;
            tutorialAssistB = !st.ringBChanged;
            tutorialAssistT = 0; tutorialAssistActive = true; idleGuide = 0;
            string point = tutorialAssistB ? (current.kind == FrameKind.Practice || current.kind == FrameKind.Parallelogram ? "C" : "B") : "D";
            string half = tutorialAssistB ? "AO" : "BO";
            int targetRadius = tutorialAssistB ? current.a : current.b;
            feedbackText.text = $"{point} 손잡이를 직접 끌어 선분 {half}와 같은 {targetRadius} cm 눈금에 놓으시오";
            feedbackText.color = Ivory; toastGroup.alpha = 1; toastT = 1.2f;
            MgfSfx.Play("whoosh", .20f);
        }

        void UpdateTutorialAssist(float dt)
        {
            tutorialAssistT += dt;
            SetPracticeGuidesVisible(true);
            if (tutorialAssistT < 1.4f) return;
            tutorialAssistActive = false;
            tutorialAssistDelay = 0;
        }

        bool ScreenToBoard(out Vector3 local)
        {
            var ray = cam.ScreenPointToRay(MgfPointer.Position);
            var plane = new Plane(boardRoot.forward, boardRoot.position);
            if (plane.Raycast(ray, out float d)) { local = boardRoot.InverseTransformPoint(ray.GetPoint(d)); return true; }
            local = default; return false;
        }

        void ApplyDrag(bool isB, Vector3 local)
        {
            int r, k;
            if (current.kind == FrameKind.Practice || current.kind == FrameKind.Parallelogram)
            {
                k = current.baseK + (isB ? 6 : 8);
                Vector2 dir = Direction(k);
                r = Mathf.Clamp(Mathf.RoundToInt((local.x * dir.x + local.y * dir.y) / Unit), 1, 10);
            }
            else
            {
                float deg = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
                k = Mathf.RoundToInt(deg / 30f);
                k = HyeonakRules.Mod12(k);
                int rel = HyeonakRules.Mod12(k - current.baseK);
                if (isB) rel = Mathf.Clamp(rel, 1, 5); else rel = Mathf.Clamp(rel, 7, 11);
                k = current.baseK + rel;
                r = Mathf.Clamp(Mathf.RoundToInt(new Vector2(local.x, local.y).magnitude / Unit), 1, 10);
            }
            if (isB) { st.ringBRadius = r; st.ringBDirection = HyeonakRules.Mod12(k); }
            else { st.ringDRadius = r; st.ringDDirection = HyeonakRules.Mod12(k); }
            UpdateGeometry(); RefreshMeasure();
        }

        void Submit()
        {
            if (phase != RunPhase.Practice && phase != RunPhase.Playing) return;
            bool ok = HyeonakRules.IsCorrect(current, st.ringBRadius, st.ringBDirection, st.ringDRadius, st.ringDDirection);
            revealCorrect = ok; revealWasFirst = firstAttempt;
            if (phase == RunPhase.Playing && firstAttempt)
            {
                st.firstAttempts++;
                if (ok) st.firstCorrect++;
            }
            if (ok)
            {
                st.score += phase == RunPhase.Practice ? 0 : (firstAttempt ? 180 : 70) + st.level * 20;
                if (phase == RunPhase.Playing) st.solved++;
                st.misconceptionId = "";
            }
            else
            {
                st.misconceptionId = HyeonakRules.Diagnose(current, st.ringBRadius, st.ringBDirection, st.ringDRadius, st.ringDDirection);
                if (phase == RunPhase.Playing)
                {
                    st.lives--;
                    int broken = HyeonakRules.StartLives - Mathf.Max(0, st.lives);
                    if (broken > 0 && broken <= 3) BeginFuseFall(broken - 1);
                }
                firstAttempt = false; st.attemptsOnCurrent++;
            }
            ComputeCorrectGhost();
            phase = RunPhase.Reveal; st.phase = "playing"; revealT = 0; trailT = 0;
            st.ringBChanged = st.ringDChanged = false;
            BeginReveal(); RefreshText(); MgfBridge.NotifyChanged();
        }

        void BeginReveal()
        {
            if (revealCorrect)
            {
                hitStop = .075f; frameLine.sharedMaterial = copperHotMat; resonanceTrail.enabled = true;
                feedbackText.text = MathRevealText(); feedbackText.color = TealGlow; toastGroup.alpha = 1;
                MgfSfx.Play("correct", .42f);
                if (phase == RunPhase.Reveal && current.kind != FrameKind.Practice)
                {
                    int slot = Mathf.Clamp(st.solved - 1, 0, 5);
                    savedFrames[slot].gameObject.SetActive(true);
                    waveLocks[slot].gameObject.SetActive(true);
                    waveLocks[slot].localScale = Vector3.zero;
                    pitchNodeRenderers[slot].sharedMaterial = copperHotMat;
                }
            }
            else
            {
                frameLine.sharedMaterial = dimCopperMat; ghostLine.enabled = true;
                ghostLine.SetPosition(0, correctGhostB); ghostLine.SetPosition(1, correctGhostD);
                feedbackText.text = WrongReason(st.misconceptionId); feedbackText.color = Ivory; toastGroup.alpha = 1;
                wrongJolt = 1f; MgfSfx.Play("wrong", .34f);
            }
        }

        void FinishReveal()
        {
            if (phase != RunPhase.Reveal) return;
            toastGroup.alpha = 0; resonanceTrail.enabled = false; ghostLine.enabled = false;
            beetle.position = beetleHome;
            frameLine.sharedMaterial = copperMat; wrongJolt = 0;
            if (current.kind == FrameKind.Practice)
            {
                if (revealCorrect) BeginMainAfterPractice();
                else { phase = RunPhase.Practice; st.phase = "playing"; SetPracticeGuidesVisible(true); }
            }
            else if (!revealCorrect)
            {
                if (st.lives <= 0) EndRun(false);
                else { phase = RunPhase.Playing; st.phase = "playing"; RefreshText(); }
            }
            else
            {
                phase = RunPhase.Playing; st.phase = "playing"; NextProblem();
            }
            MgfBridge.NotifyChanged();
        }

        void EndRun(bool clear)
        {
            if (phase == RunPhase.End) return;
            phase = RunPhase.End; st.phase = clear ? "clear" : "gameover";
            titleGroup.alpha = 0; hudGroup.alpha = 0; endGroup.alpha = 1;
            int best = Mathf.Max(PlayerPrefs.GetInt("hyeonak-nemo.bestFirst", 0), st.firstCorrect);
            PlayerPrefs.SetInt("hyeonak-nemo.bestFirst", best); PlayerPrefs.Save();
            endTitle.text = clear ? "육현 악보 완성" : st.lives <= 0 ? "말렛을 모두 썼습니다" : secondsLeft <= 0 ? "조율 시간 종료" : "첫 시도 4회에 도전";
            endTitle.color = clear ? Teal : Ink;
            endScore.text = st.score.ToString("N0");
            endStats.text = $"완성 {st.solved}/6   ·   첫 시도 {st.firstCorrect}/6\n최고 첫 시도 {best}/6";
            if (clear) MgfSfx.Play("win", .46f); else MgfSfx.Play("lose", .38f);
            MgfBridge.NotifyChanged();
        }

        void ComputeCorrectGhost()
        {
            int rb, rd, kb, kd;
            if (current.kind == FrameKind.Practice || current.kind == FrameKind.Parallelogram)
            { rb = current.a; rd = current.b; kb = current.baseK + 6; kd = current.baseK + 8; }
            else if (current.kind == FrameKind.Rectangle)
            { rb = rd = current.a; kb = current.baseK + 2; kd = current.baseK + 8; }
            else
            { rb = rd = current.kind == FrameKind.Square ? current.a : current.b; kb = current.baseK + 3; kd = current.baseK + 9; }
            correctGhostB = BoardWorld(rb, kb); correctGhostD = BoardWorld(rd, kd);
        }

        string MathRevealText()
        {
            if (current.kind == FrameKind.Practice || current.kind == FrameKind.Parallelogram)
                return $"선분 AO와 선분 OC의 길이는 각각 {current.a} cm\n선분 BO와 선분 OD의 길이는 각각 {current.b} cm";
            if (current.kind == FrameKind.Rectangle)
                return $"선분 AC와 선분 BD의 길이는 각각 {current.a * 2} cm\nO에서 서로 이등분 · 정사각형도 정답";
            if (current.kind == FrameKind.Rhombus)
                return $"선분 OB와 선분 OD의 길이는 각각 {current.b} cm\n선분 AC와 선분 BD는 서로 수직";
            return $"네 반쪽 선분의 길이는 각각 {current.a} cm\n선분 AC와 선분 BD는 서로 수직";
        }

        string WrongReason(string id)
        {
            switch (id)
            {
                case "parallelogram_diagonals_equal": return "평행사변형의 두 대각선은 길이가 같을 필요가 없습니다.\n각 대각선의 두 반쪽을 맞추시오.";
                case "equal_length_without_bisection": return "길이는 맞지만 B-O-D가 한 직선이 아닙니다.\nO가 교점이 되도록 반대 방향에 놓으시오.";
                case "perpendicular_without_bisection": return "수직이고 전체 길이는 맞지만 O에서 이등분되지 않았습니다.\n선분 OB와 선분 OD의 길이를 같게 놓으시오.";
                case "not_perpendicular": return "O에서 이등분되지만 두 대각선이 수직이 아닙니다.\n90°가 되도록 방향을 바꾸시오.";
                case "first_half_not_equal": return "선분 AO와 선분 OC의 길이가 다릅니다. 같은 눈금에 놓으시오.";
                case "second_half_not_equal": return "선분 BO와 선분 OD의 길이가 다릅니다. 같은 눈금에 놓으시오.";
                case "not_opposite_rays": return "C와 D가 정해진 반대쪽 반직선에 놓이지 않았습니다.\nO를 지나도록 빛 절점을 옮기시오.";
                default: return "두 반쪽 길이와 반대 방향을 함께 확인하시오.";
            }
        }

        void InvalidTap()
        {
            st.inputSerial++; idleGuide = 0;
            if (ScreenToBoard(out var local)) StartRipple(local);
            if (phase == RunPhase.Reveal)
            {
                MgfSfx.Play("tap", .08f);
                MgfBridge.NotifyChanged();
                return;
            }
            wrongJolt = .35f; MgfSfx.Play("tap", .12f);
            string pending = !st.ringBChanged ? (current.kind == FrameKind.Practice || current.kind == FrameKind.Parallelogram ? "C" : "B") : "D";
            feedbackText.text = $"맥동하는 {pending} 손잡이를 잡고 화살표 끝까지 직접 끌어 놓으시오";
            feedbackText.color = Ivory; toastGroup.alpha = 1;
            toastT = .85f;
            bool firstMain = phase == RunPhase.Playing && st.round == 1 && st.firstAttempts == 0 && st.attemptsOnCurrent == 0;
            if ((phase == RunPhase.Practice || firstMain) && !tutorialAssistActive)
            {
                practiceMisses++;
                if (practiceMisses >= 1) BeginTutorialAssist();
            }
            MgfBridge.NotifyChanged();
        }

        void StartRipple(Vector3 local)
        {
            int i = rippleCursor++ % rippleLines.Length;
            rippleTimes[i] = 0; rippleLines[i].enabled = true;
            rippleLines[i].transform.localPosition = new Vector3(local.x, local.y, -.43f);
        }

        void BeginFuseFall(int index)
        {
            if (index < 0 || index >= fuses.Length) return;
            // 실제 낙하는 UpdatePresentation에서 현재 lives로 계산한다.
        }

        void UpdatePresentation(float dt)
        {
            // 늘 움직이는 음악원: 마림바 건반과 악보 타일이 서로 다른 박자로 호흡한다.
            for (int i = 0; i < 6; i++)
            {
                float pulse = 1f + Mathf.Sin(titleT * (1.6f + i * .08f) + i * .72f) * (i < st.solved ? .10f : .035f);
                pitchNodes[i].localScale = new Vector3(.34f, .34f, .18f) * pulse;
                float lift = i < st.solved ? Mathf.PingPong(titleT * .38f + i * .11f, 1) : 0;
                spectrumCells[i].position = spectrumCellHomes[i] + Vector3.up * (lift * .025f);
            }
            if (beetle)
            {
                float lean = (draggingB ? -7f : draggingD ? 7f : Mathf.Sin(titleT * 1.7f) * 1.5f);
                beetle.localRotation = Quaternion.Euler(0, 0, lean);
                baton.localRotation = Quaternion.Euler(0, 0, -28 + Mathf.Sin(titleT * 3.6f) * (phase == RunPhase.Title ? 10f : 4f));
                beetleShell.localScale = new Vector3(1.05f, .56f + Mathf.Sin(titleT * 2.1f) * .018f, .82f);
            }
            if (wrongJolt > 0)
            {
                wrongJolt = Mathf.Max(0, wrongJolt - dt * 2.8f);
                float x = Mathf.Sin(titleT * 64f) * .12f * wrongJolt;
                boardRoot.localPosition = new Vector3(x, 1.35f, .25f);
            }
            else boardRoot.localPosition = new Vector3(0, 1.35f, .25f);

            for (int i = 0; i < 3; i++)
            {
                int broken = HyeonakRules.StartLives - Mathf.Max(0, st.lives);
                if (i < broken)
                {
                    Vector3 p = fuses[i].position; p.y = Mathf.MoveTowards(p.y, -1.0f, dt * 3.1f); fuses[i].position = p;
                    fuses[i].Rotate(0, 0, dt * 190f, Space.Self);
                }
            }
            for (int i = 0; i < rippleLines.Length; i++)
            {
                if (!rippleLines[i].enabled) continue;
                rippleTimes[i] += dt;
                float q = rippleTimes[i] / .48f;
                if (q >= 1) { rippleLines[i].enabled = false; continue; }
                float r = .12f + q * .58f;
                for (int j = 0; j <= 32; j++)
                {
                    float a = j * Mathf.PI * 2 / 32;
                    rippleLines[i].SetPosition(j, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0));
                }
                var c = rippleLines[i].startColor; c.a = 1 - q; rippleLines[i].startColor = rippleLines[i].endColor = c;
            }
            if (phase == RunPhase.Reveal && revealCorrect)
            {
                float q = Mathf.Clamp01(trailT / .65f);
                Vector3[] pts = FramePointsWorld();
                float f = q * 4f; int seg = Mathf.Min(3, Mathf.FloorToInt(f)); float u = f - seg;
                Vector3 scan = Vector3.Lerp(pts[seg], pts[seg + 1], u);
                resonanceTrail.SetPosition(0, pts[seg]); resonanceTrail.SetPosition(1, scan);
                // 정답 사건은 현을 훑는 진동 뒤 천공 악보가 물리적으로 솟는 연주 기록이다.
                beetle.position = beetleHome;
                beetle.localScale = Vector3.one * (1f + Mathf.Sin(q * Mathf.PI) * .05f);
                if (current.kind != FrameKind.Practice)
                {
                    int slot = Mathf.Clamp(st.solved - 1, 0, 5);
                    float lockPhase = Mathf.Clamp01(trailT / .58f);
                    float overshoot = lockPhase + Mathf.Sin(lockPhase * Mathf.PI) * .16f;
                    waveLocks[slot].localScale = Vector3.one * overshoot;
                }
                float pulse = 1 + Mathf.Sin(q * Mathf.PI) * .20f;
                handleB.localScale = new Vector3(.38f, .38f, .22f) * pulse;
                handleD.localScale = new Vector3(.38f, .38f, .22f) * pulse;
            }
            else
            {
                beetle.position = beetleHome; beetle.localScale = Vector3.one;
                float bPulse = draggingB ? 1.16f : 1f, dPulse = draggingD ? 1.16f : 1f;
                handleB.localScale = new Vector3(.38f, .38f, .22f) * bPulse;
                handleD.localScale = new Vector3(.38f, .38f, .22f) * dPulse;
            }
            PositionGuides();
        }

        void UpdateGeometry()
        {
            Vector3 a, b, c, d;
            if (current.kind == FrameKind.Practice || current.kind == FrameKind.Parallelogram)
            {
                a = BoardWorld(current.a, current.baseK);
                b = BoardWorld(current.b, current.baseK + 2);
                c = BoardWorld(st.ringBRadius, st.ringBDirection);
                d = BoardWorld(st.ringDRadius, st.ringDDirection);
                fixedA.position = a; fixedC.position = b; handleB.position = c; handleD.position = d;
            }
            else
            {
                a = BoardWorld(current.a, current.baseK);
                c = BoardWorld(current.a, current.baseK + 6);
                b = BoardWorld(st.ringBRadius, st.ringBDirection);
                d = BoardWorld(st.ringDRadius, st.ringDDirection);
                fixedA.position = a; fixedC.position = c; handleB.position = b; handleD.position = d;
            }
            pinO.position = boardRoot.TransformPoint(new Vector3(0, 0, -.40f));
            Vector3 z = boardRoot.forward * -.05f;
            frameLine.SetPosition(0, a + z); frameLine.SetPosition(1, b + z); frameLine.SetPosition(2, c + z); frameLine.SetPosition(3, d + z); frameLine.SetPosition(4, a + z);
            diagAC.SetPosition(0, a + z * 1.2f); diagAC.SetPosition(1, c + z * 1.2f);
            diagBD.SetPosition(0, b + z * 1.25f); diagBD.SetPosition(1, d + z * 1.25f);
            Vector3 sb = cam.WorldToScreenPoint(handleB.position), sd = cam.WorldToScreenPoint(handleD.position);
            Vector3 so = cam.WorldToScreenPoint(boardRoot.position);
            st.ringBScreenX = Mathf.RoundToInt(sb.x); st.ringBScreenY = Screen.height - Mathf.RoundToInt(sb.y);
            st.ringDScreenX = Mathf.RoundToInt(sd.x); st.ringDScreenY = Screen.height - Mathf.RoundToInt(sd.y);
            st.boardCenterScreenX = Mathf.RoundToInt(so.x); st.boardCenterScreenY = Screen.height - Mathf.RoundToInt(so.y);
            UpdateRadials(); RefreshMeasure();
        }

        Vector3[] FramePointsWorld()
        {
            var p = new Vector3[5]; for (int i = 0; i < 5; i++) p[i] = frameLine.GetPosition(i); return p;
        }

        void UpdateRadials()
        {
            int count = current.kind == FrameKind.Practice || current.kind == FrameKind.Parallelogram ? 2 : 10;
            for (int i = 0; i < radialLines.Length; i++)
            {
                radialLines[i].enabled = i < count; if (!radialLines[i].enabled) continue;
                int rel = count == 2 ? (i == 0 ? 6 : 8) : (i < 5 ? i + 1 : i + 2);
                int k = current.baseK + rel;
                radialLines[i].positionCount = 2;
                radialLines[i].SetPosition(0, boardRoot.TransformPoint(new Vector3(0, 0, -.33f)));
                radialLines[i].SetPosition(1, BoardWorld(10, k));
            }
        }

        Vector3 BoardWorld(int radius, int k)
        {
            Vector2 d = Direction(k);
            return boardRoot.TransformPoint(new Vector3(d.x * radius * Unit, d.y * radius * Unit, -.37f));
        }

        static Vector2 Direction(int k)
        {
            float a = HyeonakRules.Mod12(k) * 30f * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        void RefreshText()
        {
            promptText.text = current.prompt;
            RefreshMeasure();
        }

        void RefreshMeasure()
        {
            string n1 = current.kind == FrameKind.Practice || current.kind == FrameKind.Parallelogram ? "OC" : "OB";
            measureText.text = $"선분 {n1}의 길이 {st.ringBRadius} cm\n선분 OD의 길이 {st.ringDRadius} cm";
        }

        void UpdateUi()
        {
            displayedScore = Mathf.MoveTowards(displayedScore, st.score, Mathf.Max(80f, Mathf.Abs(st.score - displayedScore) * 7f) * Time.deltaTime);
            int shownScore = Mathf.RoundToInt(displayedScore);
            hudText.text = phase == RunPhase.Practice ? "연습 · 시계 정지"
                : $"점수 {shownScore:N0}   ·   {Mathf.CeilToInt(secondsLeft)}초   ·   첫 시도 {st.firstCorrect}/{st.firstAttempts}";
            bool firstMainGuide = phase == RunPhase.Playing && st.round == 1 && st.firstAttempts == 0 && st.attemptsOnCurrent == 0;
            int placed = (st.ringBChanged ? 1 : 0) + (st.ringDChanged ? 1 : 0);
            progressText.text = phase == RunPhase.Practice ? $"연습 절점 {placed}/2   ·   둘 다 놓으면 자동 제출"
                : firstMainGuide ? $"첫 조율 {placed}/2   ·   두 손잡이를 차례로 놓으시오"
                : $"악보 타일 {st.solved}/6   ·   {BandName(st.level)}";
            fuseText.text = "남은 말렛 " + Mathf.Max(0, st.lives);
            for (int i = 0; i < 3; i++) fuseImages[i].color = i < st.lives ? CopperHot : new Color(.28f, .27f, .27f, .65f);
            RectTransform canvasRt = (RectTransform)MgfText.Canvas.transform;
            float logicalW = canvasRt.rect.width, logicalH = canvasRt.rect.height;
            bool land = logicalW >= logicalH * 1.05f;
            if (Mathf.Abs(logicalW - lastLayoutW) > .5f || Mathf.Abs(logicalH - lastLayoutH) > .5f)
            {
                lastLayoutW = logicalW; lastLayoutH = logicalH;
                if (land)
                {
                    float margin = 24f;
                    float leftW = Mathf.Clamp(logicalW * .285f, 218f, 278f);
                    float rightW = Mathf.Clamp(logicalW * .245f, 205f, 276f);
                    float leftX = -logicalW * .5f + margin + leftW * .5f;
                    float rightX = logicalW * .5f - margin - rightW * .5f;
                    float topY = logicalH * .5f - margin;

                    CenterRect(goalPanel, new Vector2(leftX, topY - 30), new Vector2(leftW, 60));
                    CenterRect(promptPanel, new Vector2(leftX, topY - 132), new Vector2(leftW, 132));
                    CenterRect(magnifierPanel, new Vector2(rightX, -logicalH * .5f + 62), new Vector2(rightW, 72));
                    CenterRect(statusPanel, new Vector2(leftX, topY - 218), new Vector2(leftW, 44));
                    CenterRect(progressPanel, new Vector2(leftX, -logicalH * .5f + 112), new Vector2(leftW, 52));
                    CenterRect(fusePanel, new Vector2(leftX, -logicalH * .5f + 39), new Vector2(leftW, 48));
                    CenterText(hudText, new Vector2(leftX, topY - 218), leftW, 13);
                    CenterText(progressText, new Vector2(leftX, -logicalH * .5f + 112), leftW, 15);
                    CenterText(fuseText, new Vector2(leftX - leftW * .22f, -logicalH * .5f + 38), leftW * .46f, 12);
                    for (int i = 0; i < 3; i++)
                    {
                        var rt = fuseImages[i].rectTransform;
                        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
                        rt.anchoredPosition = new Vector2(leftX + leftW * (.05f + i * .18f), -logicalH * .5f + 39);
                    }
                    promptText.fontSize = 13; promptText.rectTransform.sizeDelta = new Vector2(leftW - 20, 118);
                    goalText.fontSize = 14; goalText.rectTransform.sizeDelta = new Vector2(leftW - 18, 48);
                    measureText.fontSize = 13; measureText.rectTransform.sizeDelta = new Vector2(rightW - 16, 60);
                    toastPanel.sizeDelta = new Vector2(Mathf.Min(390, logicalW * .42f), 82);
                    toastPanel.anchoredPosition = new Vector2(0, logicalH * .5f - 76);
                }
                else
                {
                    float safeW = Mathf.Max(280, logicalW - 48f);
                    float panelW = Mathf.Min(372, safeW);
                    float topY = logicalH * .5f - 24f;
                    CenterRect(goalPanel, new Vector2(0, topY - 28), new Vector2(panelW, 56));
                    CenterRect(promptPanel, new Vector2(0, topY - 112), new Vector2(panelW, 96));
                    CenterRect(magnifierPanel, new Vector2(0, -logicalH * .5f + 154), new Vector2(Mathf.Min(326, panelW), 62));
                    CenterRect(statusPanel, new Vector2(0, topY - 183), new Vector2(panelW, 44));
                    CenterRect(progressPanel, new Vector2(0, -logicalH * .5f + 92), new Vector2(panelW, 54));
                    CenterRect(fusePanel, new Vector2(0, -logicalH * .5f + 37), new Vector2(Mathf.Min(330, panelW), 46));
                    CenterText(hudText, new Vector2(0, topY - 183), panelW, 14);
                    CenterText(progressText, new Vector2(0, -logicalH * .5f + 92), panelW, 17);
                    CenterText(fuseText, new Vector2(-panelW * .28f, -logicalH * .5f + 36), panelW * .42f, 12);
                    for (int i = 0; i < 3; i++)
                    {
                        var rt = fuseImages[i].rectTransform;
                        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
                        rt.anchoredPosition = new Vector2(-panelW * .03f + i * 42, -logicalH * .5f + 37);
                    }
                    promptText.fontSize = 14; promptText.rectTransform.sizeDelta = new Vector2(panelW - 20, 86);
                    goalText.fontSize = 14; goalText.rectTransform.sizeDelta = new Vector2(panelW - 18, 46);
                    measureText.fontSize = 14; measureText.rectTransform.sizeDelta = new Vector2(Mathf.Min(306, panelW - 18), 54);
                    toastPanel.sizeDelta = new Vector2(Mathf.Min(352, panelW), 76);
                    toastPanel.anchoredPosition = new Vector2(0, 205);
                }
            }
        }

        static void CenterRect(RectTransform rt, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.anchoredPosition = position; rt.sizeDelta = size;
        }

        static void CenterText(TextMeshProUGUI text, Vector2 position, float width, float fontSize)
        {
            RectTransform rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.anchoredPosition = position; rt.sizeDelta = new Vector2(width, fontSize * 2.5f);
            text.fontSize = fontSize;
        }

        static string BandName(int level) => level == 1 ? "반쪽 길이" : level == 2 ? "같은 대각선" : "수직이등분";

        void PositionGuides()
        {
            bool mainFirst = phase == RunPhase.Playing && st.round == 1 && st.firstAttempts == 0 && st.attemptsOnCurrent == 0;
            if (phase != RunPhase.Practice && !mainFirst) { SetPracticeGuidesVisible(false); guideLine.enabled = false; return; }
            SetPracticeGuidesVisible(true);
            RectTransform canvasRt = (RectTransform)MgfText.Canvas.transform;
            Vector2 bStart = ScreenToCanvas(handleB.position, canvasRt);
            Vector2 dStart = ScreenToCanvas(handleD.position, canvasRt);
            Vector2 bTarget = ScreenToCanvas(BoardWorld(current.a, current.baseK + 6), canvasRt);
            Vector2 dTarget = ScreenToCanvas(BoardWorld(current.b, current.baseK + 8), canvasRt);
            float pulse = 1 + Mathf.Sin(titleT * (idleGuide > 8 ? 8f : 5f)) * .12f;
            ringBGuide.rectTransform.localScale = ringDGuide.rectTransform.localScale = Vector3.one * pulse;
            ringBTarget.rectTransform.localScale = ringDTarget.rectTransform.localScale = Vector3.one * (1.02f + Mathf.Sin(titleT * 4.2f) * .06f);
            string bLabel = "C · " + current.a + " cm";
            string dLabel = "D · " + current.b + " cm";
            PositionGuideSet(ringBGuide, ringBTarget, guideArrowB, guideHandB, guideTextB, bStart, bTarget, bLabel, 0f);
            PositionGuideSet(ringDGuide, ringDTarget, guideArrowD, guideHandD, guideTextD, dStart, dTarget, dLabel, .55f);

            guideLine.enabled = !st.ringBChanged || !st.ringDChanged;
            if (guideLine.enabled)
            {
                bool first = !st.ringBChanged;
                guideLine.SetPosition(0, first ? handleB.position : handleD.position);
                guideLine.SetPosition(1, first ? BoardWorld(current.a, current.baseK + 6) : BoardWorld(current.b, current.baseK + 8));
                guideLine.startWidth = guideLine.endWidth = .06f + .035f * (.5f + .5f * Mathf.Sin(titleT * 7f));
            }
        }

        void PositionGuideSet(Image startRing, Image targetRing, Image arrow, Image hand, TextMeshProUGUI label,
            Vector2 start, Vector2 target, string text, float phaseOffset)
        {
            if (!startRing.enabled) return;
            startRing.rectTransform.anchoredPosition = start;
            targetRing.rectTransform.anchoredPosition = target;
            Vector2 delta = target - start;
            float length = delta.magnitude;
            Vector2 perpendicular = length > 1f ? new Vector2(-delta.y, delta.x) / length : Vector2.up;
            arrow.rectTransform.anchoredPosition = (start + target) * .5f;
            arrow.rectTransform.sizeDelta = new Vector2(Mathf.Clamp(length, 78, 190), 48);
            arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            float travel = Mathf.PingPong(titleT * .72f + phaseOffset, 1f);
            hand.rectTransform.anchoredPosition = Vector2.Lerp(start, target, Mathf.SmoothStep(0, 1, travel)) + perpendicular * 36f;
            label.rectTransform.anchoredPosition = target + perpendicular * 42f;
            label.text = text;
        }

        Vector2 ScreenToCanvas(Vector3 world, RectTransform canvasRt)
        {
            Vector2 sp = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out var lp);
            return lp;
        }

        void Shuffle(List<FrameProblem> list)
        {
            for (int i = list.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = list[i]; list[i] = list[j]; list[j] = t; }
        }

        // ── UI construction helpers ──────────────────────────────────────────
        RectTransform Full(string name, Transform parent)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero; return rt;
        }

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size); var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ?? roundedSprite; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false; return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size); var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ?? roundedSprite; img.type = sprite != null && sprite.border == Vector4.zero ? Image.Type.Simple : Image.Type.Sliced;
            img.color = color; img.raycastTarget = false; return img;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color, float width)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 2.3f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color;
            t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false; t.text = text; return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                if (ring)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(1 - Mathf.Abs(d - size * .41f) / (size * .055f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * .5f) - (size * .5f - radius), 0);
                    float qy = Mathf.Max(Mathf.Abs(fy - size * .5f) - (size * .5f - radius), 0);
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            float b = ring ? 0 : radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        static Sprite MakeArrowSprite(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[width * height];
            float cy = height * .5f;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                float dy = Mathf.Abs(y + .5f - cy);
                bool shaft = x < width * .76f && dy <= height * .105f;
                float headStart = width * .60f;
                bool head = x >= headStart && dy <= (x - headStart) * .30f + 1;
                px[y * width + x] = new Color32(255, 255, 255, (byte)((shaft || head) ? 255 : 0));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 100);
        }

        static Sprite MakeHandSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            Vector2 palm = new Vector2(size * .45f, size * .62f);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + .5f, y + .5f);
                bool palmShape = Vector2.Distance(p, palm) <= size * .19f;
                bool finger = x >= size * .43f && x <= size * .57f && y >= size * .12f && y <= size * .62f;
                bool fingertip = Vector2.Distance(p, new Vector2(size * .5f, size * .14f)) <= size * .07f;
                bool thumb = Vector2.Distance(p, new Vector2(size * .64f, size * .62f)) <= size * .10f;
                px[y * size + x] = new Color32(255, 255, 255, (byte)((palmShape || finger || fingertip || thumb) ? 235 : 0));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100);
        }

        void SetPracticeGuidesVisible(bool visible)
        {
            if (ringBGuide == null) return;
            // 한 번에 한 절점만 가리켜 첫 본판에서도 "다음에 무엇을 잡는지"가 끊기지 않게 한다.
            bool showB = visible && !st.ringBChanged;
            bool showD = visible && st.ringBChanged && !st.ringDChanged;
            ringBGuide.enabled = showB;
            ringDGuide.enabled = showD;
            ringBTarget.enabled = showB;
            ringDTarget.enabled = showD;
            guideArrowB.enabled = showB;
            guideArrowD.enabled = showD;
            guideHandB.enabled = showB;
            guideHandD.enabled = showD;
            guideTextB.gameObject.SetActive(showB);
            guideTextD.gameObject.SetActive(showD);
        }

        // ── QA bridge ────────────────────────────────────────────────────────
        public void TestStart() => StartRunDirect();

        public void TestAnswerCorrect()
        {
            if (phase == RunPhase.Title || phase == RunPhase.End || phase == RunPhase.Practice) StartRunDirect();
            if (phase == RunPhase.Reveal) { revealT = 99; FinishReveal(); if (phase == RunPhase.End) StartRunDirect(); }
            if (current.kind == FrameKind.Parallelogram)
            { st.ringBRadius = current.a; st.ringDRadius = current.b; st.ringBDirection = HyeonakRules.Mod12(current.baseK + 6); st.ringDDirection = HyeonakRules.Mod12(current.baseK + 8); }
            else if (current.kind == FrameKind.Rectangle)
            { st.ringBRadius = st.ringDRadius = current.a; st.ringBDirection = HyeonakRules.Mod12(current.baseK + 2); st.ringDDirection = HyeonakRules.Mod12(current.baseK + 8); }
            else
            { st.ringBRadius = st.ringDRadius = current.kind == FrameKind.Square ? current.a : current.b; st.ringBDirection = HyeonakRules.Mod12(current.baseK + 3); st.ringDDirection = HyeonakRules.Mod12(current.baseK + 9); }
            st.ringBChanged = st.ringDChanged = true; UpdateGeometry(); Submit();
        }

        public void TestAnswerWrong()
        {
            if (phase == RunPhase.Title || phase == RunPhase.End || phase == RunPhase.Practice) StartRunDirect();
            if (phase == RunPhase.Reveal) { revealT = 99; FinishReveal(); if (phase == RunPhase.End) StartRunDirect(); }
            st.ringBRadius = 1; st.ringDRadius = 10;
            st.ringBDirection = HyeonakRules.Mod12(current.baseK + 1); st.ringDDirection = HyeonakRules.Mod12(current.baseK + 7);
            st.ringBChanged = st.ringDChanged = true; UpdateGeometry(); Submit();
        }

        public string StateJson() => JsonUtility.ToJson(st);

        public string ProblemBankJson()
        {
            var list = new List<MgfProblem>(bank.Count);
            for (int i = 0; i < bank.Count; i++) list.Add(HyeonakRules.ToMgf(bank[i]));
            return MgfJson.Bank(list);
        }
    }
}
