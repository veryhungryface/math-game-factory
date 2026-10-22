// 현악 네모 — 청록 바위벌레의 현무암 육현 공명극장.
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

        static readonly Color Basalt = MgfLook.Hex("#25272C");
        static readonly Color Basalt2 = MgfLook.Hex("#34383F");
        static readonly Color Copper = MgfLook.Hex("#B56F3A");
        static readonly Color CopperHot = MgfLook.Hex("#E9A762");
        static readonly Color Teal = MgfLook.Hex("#38B8AE");
        static readonly Color TealGlow = MgfLook.Hex("#78FFF1");
        static readonly Color Ivory = MgfLook.Hex("#EDE4CB");
        static readonly Color Violet = MgfLook.Hex("#795B92");
        static readonly Color Amber = MgfLook.Hex("#D9A441");

        readonly State st = new State();
        readonly List<FrameProblem> bank = new List<FrameProblem>(300);
        readonly List<FrameProblem> band1 = new List<FrameProblem>();
        readonly List<FrameProblem> band2 = new List<FrameProblem>();
        readonly List<FrameProblem> band3 = new List<FrameProblem>();
        readonly Transform[] chimes = new Transform[6];
        readonly Renderer[] chimeRenderers = new Renderer[6];
        readonly Transform[] resonanceRings = new Transform[6];
        readonly Vector3[] resonanceRingHomes = new Vector3[6];
        readonly Transform[] fuses = new Transform[3];
        readonly Transform[] savedFrames = new Transform[6];
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
        RectTransform titlePanel, ctaPanel, goalPanel, promptPanel, magnifierPanel, endPanel, toastPanel;
        TextMeshProUGUI titleText, tagText, badgeText, bestText, ctaText;
        TextMeshProUGUI goalText, promptText, hudText, progressText, fuseText, measureText, feedbackText;
        TextMeshProUGUI endTitle, endStats, endScore, endCta;
        TextMeshPro labelA, labelC, labelB, labelD;
        Image[] fuseImages = new Image[3];
        Image ringBGuide, ringDGuide;
        float displayedScore, toastT;
        int lastLayoutW = -1, lastLayoutH = -1;
        Sprite roundedSprite, ringSprite;

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
            basaltMat = MgfLook.Lit(Basalt, .08f, .02f);
            basaltLightMat = MgfLook.Lit(Basalt2, .10f, .03f);
            copperMat = MgfLook.Lit(Copper, .38f, .72f);
            copperHotMat = MgfLook.Lit(CopperHot, .62f, .78f, Copper * .45f);
            dimCopperMat = MgfLook.Lit(MgfLook.Hex("#66452F"), .10f, .34f);
            tealMat = MgfLook.Lit(Teal, .32f, .12f);
            tealGlowMat = MgfLook.Lit(Teal, .72f, .18f, TealGlow * .9f);
            ivoryMat = MgfLook.Lit(Ivory, .16f, .04f);
            violetMat = MgfLook.Lit(Violet, .20f, .10f);
            amberMat = MgfLook.Lit(Amber, .35f, .22f, Amber * .24f);
        }

        void BuildWorld()
        {
            // 코드로 만드는 Capsule/Sphere 프리미티브의 Collider 타입을 WebGL 스트리핑에서 보존한다.
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<SphereCollider>();
            Destroy(colliderTypes);
            MgfLook.Sky(MgfLook.Hex("#17191E"), MgfLook.Hex("#353A43"), MgfLook.Hex("#17191D"), .78f);
            MgfLook.Sun(new Vector3(48, -34, 18), MgfLook.Hex("#FFE5BD"), 1.18f, .72f);
            cam = MgfLook.Camera(new Vector3(7.7f, 6.4f, -16.5f), new Vector3(0, 1.15f, 0), 34f);
            cam.orthographic = true;
            cam.orthographicSize = 7.35f;

            // 층진 현무암 원형극장. 중앙 좁은 컬럼이 아니라 와이드에서 좌우 객석이 드러난다.
            MgfLook.Block("CavernFloor", new Vector3(0, -1.05f, 3.8f), new Vector3(24, .9f, 20), .28f, basaltMat);
            for (int side = -1; side <= 1; side += 2)
            for (int row = 0; row < 5; row++)
            {
                float x = side * (4.7f + row * 1.55f);
                float y = -.45f + row * .64f;
                float z = 2.3f + row * 1.15f;
                var seat = MgfLook.Block("BasaltTier" + side + "_" + row, new Vector3(x, y, z),
                    new Vector3(3.5f + row * .35f, .75f, 4.8f), .30f, row % 2 == 0 ? basaltLightMat : basaltMat);
                seat.transform.localRotation = Quaternion.Euler(0, side * (8 + row * 2), side * 2);
            }
            for (int i = 0; i < 18; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * (5.2f + (i % 5) * 1.14f);
                float y = .1f + (i % 4) * .58f;
                float z = -1.8f + (i % 7) * 1.2f;
                var ore = MgfLook.Prim(PrimitiveType.Cube, "TealOre" + i, new Vector3(x, y, z),
                    new Vector3(.13f, .48f + (i % 3) * .12f, .13f), i % 3 == 0 ? tealGlowMat : tealMat, null, false);
                ore.transform.rotation = Quaternion.Euler(12, i * 31, 45);
            }
            // 좁은 호박색 각광.
            for (int i = 0; i < 9; i++)
            {
                float x = -5.6f + i * 1.4f;
                MgfLook.Block("Footlight" + i, new Vector3(x, -.46f, -2.15f), new Vector3(.78f, .10f, .16f), .04f, amberMat);
            }

            BuildResonanceGallery();
            BuildBeetle();
            BuildBoard();
            BuildRipples();
        }

        void BuildResonanceGallery()
        {
            for (int i = 0; i < 6; i++)
            {
                float side = i < 3 ? -1f : 1f;
                float lane = i % 3;
                Vector3 center = new Vector3(side * (4.55f + lane * 1.25f), .4f + lane * 1.1f, 1.8f + lane * .65f);
                var root = new GameObject("ResonanceAlcove" + i).transform;
                root.position = center;
                resonanceRings[i] = root;
                resonanceRingHomes[i] = center;
                MakeEllipse("CopperResonanceRing" + i, root, 1.12f, 1.43f, .10f, copperMat, 40);
                var chime = MgfLook.Prim(PrimitiveType.Cube, "MineralChime" + i, new Vector3(0, 0, 0),
                    new Vector3(.34f, .68f, .34f), dimCopperMat, root, false);
                chime.transform.localRotation = Quaternion.Euler(0, 35, 45);
                chimes[i] = chime.transform;
                chimeRenderers[i] = chime.GetComponent<Renderer>();
                savedFrames[i] = MakeMiniFrame(root, new Vector3(0, -1.0f, 0));
                savedFrames[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < 3; i++)
            {
                var fuse = MgfLook.Prim(PrimitiveType.Cylinder, "ResonanceFuse" + i,
                    new Vector3(-.65f + i * .65f, -.28f, -2.2f), new Vector3(.055f, .44f, .055f), copperHotMat);
                fuse.transform.rotation = Quaternion.Euler(90, 0, 0);
                fuses[i] = fuse.transform;
            }
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
            var lr = MakeLine("MiniWire", copperHotMat, .035f, root);
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
            boardRoot = new GameObject("FrontalStringFrameStage").transform;
            boardRoot.position = new Vector3(0, 1.35f, .25f);
            // TMP/board front(-Z)가 카메라를 향하도록 +Z를 카메라 반대쪽으로 둔다.
            boardRoot.rotation = Quaternion.LookRotation(boardRoot.position - cam.transform.position, Vector3.up);
            MgfLook.Block("IrregularBasaltMusicStand", Vector3.zero, new Vector3(7.0f, 6.15f, .34f), .34f, basaltMat, boardRoot);
            MgfLook.Block("CopperInnerRim", new Vector3(0, 0, -.20f), new Vector3(6.55f, 5.70f, .10f), .26f, copperMat, boardRoot);
            MgfLook.Block("BlackMeasurementFace", new Vector3(0, 0, -.27f), new Vector3(6.30f, 5.45f, .08f), .22f, basaltLightMat, boardRoot);

            for (int i = 0; i < radialLines.Length; i++)
                radialLines[i] = MakeLine("RadialScale" + i, MgfLook.Alpha(new Color(Ivory.r, Ivory.g, Ivory.b, .28f)), .018f, boardRoot);
            for (int i = 0; i < measureRings.Length; i++)
            {
                measureRings[i] = MakeLine("MeasureRing" + (i + 1), MgfLook.Alpha(new Color(Ivory.r, Ivory.g, Ivory.b, i % 5 == 4 ? .30f : .16f)),
                    i % 5 == 4 ? .025f : .014f, boardRoot);
                measureRings[i].useWorldSpace = false; measureRings[i].positionCount = 49;
                float r = (i + 1) * Unit;
                for (int j = 0; j <= 48; j++)
                {
                    float a = j * Mathf.PI * 2 / 48;
                    measureRings[i].SetPosition(j, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, -.34f));
                }
            }

            frameLine = MakeLine("QuadrilateralCopperStrings", copperMat, .075f, boardRoot);
            frameLine.positionCount = 5;
            diagAC = MakeLine("ActualDiagonalAC", MgfLook.Alpha(new Color(Ivory.r, Ivory.g, Ivory.b, .80f)), .032f, boardRoot);
            diagAC.positionCount = 2;
            diagBD = MakeLine("ActualDiagonalBD", MgfLook.Alpha(new Color(Violet.r, Violet.g, Violet.b, .88f)), .042f, boardRoot);
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
                MakeEllipse(name + "CopperRing", go.transform, .31f, .31f, .075f, copperHotMat, 28, -.12f);
            }
            return go.transform;
        }

        TextMeshPro MakeLabel(string text, Transform parent, Vector3 pos, float size = 3.2f)
        {
            var t = MgfText.World(text, pos, size, Ivory, parent);
            t.fontStyle = FontStyles.Bold;
            t.outlineWidth = .15f; t.outlineColor = new Color32(20, 20, 24, 220);
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
            ringSprite = MakeSprite(96, 0, true);
            Transform root = MgfText.Canvas.transform;

            titlePanel = Full("TitleScreen", root); titleGroup = titlePanel.gameObject.AddComponent<CanvasGroup>();
            var plaque = Panel(titlePanel, "SuspendedBasaltPlaque", new Vector2(.5f, .78f), Vector2.zero, new Vector2(354, 168), new Color(.10f, .11f, .13f, .96f));
            plaque.localRotation = Quaternion.Euler(0, 0, -2.2f);
            titleText = Txt(plaque, "현악 네모", new Vector2(.5f, .61f), Vector2.zero, 54, Ivory, 330);
            titleText.outlineWidth = .24f; titleText.outlineColor = new Color32(58, 30, 20, 255);
            titleText.fontStyle = FontStyles.Bold;
            tagText = Txt(plaque, "꼭짓점을 끌어 대각선을 조율하라", new Vector2(.5f, .24f), Vector2.zero, 16, TealGlow, 330);
            badgeText = Txt(titlePanel, "중2 · 사각형의 성질", new Vector2(.5f, .62f), Vector2.zero, 14, Ivory, 280);
            bestText = Txt(titlePanel, "", new Vector2(.5f, .26f), Vector2.zero, 14, Ivory, 300);
            ctaPanel = Panel(titlePanel, "TuneCta", new Vector2(.5f, .14f), Vector2.zero, new Vector2(330, 72), Copper);
            ctaText = Txt(ctaPanel, "조율사를 눌러 공명 시작", new Vector2(.5f, .5f), Vector2.zero, 22, MgfLook.Hex("#21150E"), 310);

            var hud = Full("Hud", root); hudGroup = hud.gameObject.AddComponent<CanvasGroup>();
            goalPanel = Panel(hud, "GoalPlaque", new Vector2(.5f, 1f), new Vector2(0, -49), new Vector2(368, 54), new Color(.12f, .13f, .15f, .94f));
            goalText = Txt(goalPanel, "꼭짓점을 맞추면 광물 빛종이 울린다", new Vector2(.5f, .5f), Vector2.zero, 16, Ivory, 344);
            promptPanel = Panel(hud, "PromptPlaque", new Vector2(.5f, 1f), new Vector2(0, -116), new Vector2(372, 72), new Color(.93f, .89f, .80f, .96f));
            promptText = Txt(promptPanel, "", new Vector2(.5f, .5f), Vector2.zero, 15, MgfLook.Hex("#24262B"), 350);
            hudText = Txt(hud, "", new Vector2(.5f, 1f), new Vector2(0, -166), 15, Ivory, 350);
            progressText = Txt(hud, "", new Vector2(.5f, 0f), new Vector2(0, 80), 18, Ivory, 360);
            fuseText = Txt(hud, "공명 퓨즈", new Vector2(.18f, 0f), new Vector2(0, 34), 13, Ivory, 110);
            for (int i = 0; i < 3; i++)
                fuseImages[i] = Img(hud, "Fuse" + i, new Vector2(.36f + i * .11f, 0), new Vector2(0, 36), new Vector2(30, 8), CopperHot);
            magnifierPanel = Panel(hud, "Magnifier", new Vector2(.5f, 0f), new Vector2(0, 140), new Vector2(250, 54), new Color(.12f, .13f, .15f, .94f));
            measureText = Txt(magnifierPanel, "", new Vector2(.5f, .5f), Vector2.zero, 17, Ivory, 232);
            ringBGuide = Img(hud, "RingBGuide", new Vector2(.5f, .5f), Vector2.zero, new Vector2(72, 72), TealGlow, ringSprite);
            ringDGuide = Img(hud, "RingDGuide", new Vector2(.5f, .5f), Vector2.zero, new Vector2(72, 72), TealGlow, ringSprite);
            ringBGuide.enabled = ringDGuide.enabled = false;

            toastPanel = Panel(hud, "Feedback", new Vector2(.5f, .5f), new Vector2(0, 205), new Vector2(352, 76), new Color(.12f, .13f, .15f, .96f));
            toastGroup = toastPanel.gameObject.AddComponent<CanvasGroup>();
            feedbackText = Txt(toastPanel, "", new Vector2(.5f, .5f), Vector2.zero, 18, Ivory, 330);

            endPanel = Full("EndScreen", root); endGroup = endPanel.gameObject.AddComponent<CanvasGroup>();
            var resultPlaque = Panel(endPanel, "ResultPlaque", new Vector2(.5f, .58f), Vector2.zero, new Vector2(360, 300), new Color(.10f, .11f, .13f, .97f));
            endTitle = Txt(resultPlaque, "", new Vector2(.5f, .78f), Vector2.zero, 38, TealGlow, 330);
            endScore = Txt(resultPlaque, "", new Vector2(.5f, .52f), Vector2.zero, 54, Ivory, 330);
            endStats = Txt(resultPlaque, "", new Vector2(.5f, .25f), Vector2.zero, 18, Ivory, 330);
            var retry = Panel(endPanel, "RetryCta", new Vector2(.5f, .28f), Vector2.zero, new Vector2(330, 72), Copper);
            endCta = Txt(retry, "다시 조율하기", new Vector2(.5f, .5f), Vector2.zero, 23, MgfLook.Hex("#21150E"), 310);
        }

        void Prewarm()
        {
            var s = new StringBuilder("현악네모꼭짓점을끌어대각선을조율하라중학교2학년사각형의성질조율사를눌러공명시작빛종울린다평행사변형직사각형마름모정사각형중앙핀교점서로다른것을이등분수직길이반대방향첫시도공명퓨즈남은시간완성연주다시조율하기연습C와D옮기시오cm");
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
                ? "최고 첫 시도 " + PlayerPrefs.GetInt("hyeonak-nemo.bestFirst", 0) + "/6" : "광물 빛종 6개를 깨워라";
            for (int i = 0; i < 6; i++) { savedFrames[i].gameObject.SetActive(false); chimeRenderers[i].sharedMaterial = dimCopperMat; }
            for (int i = 0; i < 3; i++) { fuses[i].gameObject.SetActive(true); fuses[i].localPosition = new Vector3(-.65f + i * .65f, -.28f, -2.2f); }
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
            for (int i = 0; i < 6; i++) { savedFrames[i].gameObject.SetActive(false); chimeRenderers[i].sharedMaterial = dimCopperMat; }
            for (int i = 0; i < 3; i++) { fuses[i].gameObject.SetActive(true); fuses[i].localPosition = new Vector3(-.65f + i * .65f, -.28f, -2.2f); fuses[i].localRotation = Quaternion.Euler(90, 0, 0); }
        }

        void StartPractice()
        {
            ResetRun(); phase = RunPhase.Practice; st.phase = "playing";
            current = HyeonakRules.Practice(); SetProblem(current, true);
            titleGroup.alpha = 0; hudGroup.alpha = 1; endGroup.alpha = 0;
            goalText.text = "C와 D를 끌어 두 대각선을 서로 다른 것을 이등분하시오";
            ringBGuide.enabled = true; ringDGuide.enabled = false;
            MgfSfx.Play("whoosh", .30f); MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun(); phase = RunPhase.Playing; st.phase = "playing";
            titleGroup.alpha = 0; hudGroup.alpha = 1; endGroup.alpha = 0;
            NextProblem(); MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            st.score = 0; st.solved = 0; st.round = 0; st.firstAttempts = 0; st.firstCorrect = 0;
            st.lives = HyeonakRules.StartLives; st.attemptsOnCurrent = 0; secondsLeft = 110f;
            phase = RunPhase.Playing; st.phase = "playing";
            ringBGuide.enabled = ringDGuide.enabled = false;
            goalText.text = "두 꼭짓점을 놓아 주문한 사각형을 만드시오";
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
            if (phase == RunPhase.Practice)
            {
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0; st.inputSerial++; MgfSfx.Play("tap", .12f); }
            }
            if (phase == RunPhase.Reveal)
            {
                revealT += dt;
                trailT += dt;
                if (revealT >= (revealCorrect ? 1.18f : .92f)) FinishReveal();
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

                // 바위벌레와 줄이 시각적으로 겹쳐도 52px 고리의 화면 히트박스가 항상 먼저 잡힌다.
                Vector2 pointer = MgfPointer.Position;
                Vector2 bScreen = new Vector2(st.ringBScreenX, Screen.height - st.ringBScreenY);
                Vector2 dScreen = new Vector2(st.ringDScreenX, Screen.height - st.ringDScreenY);
                float db = Vector2.Distance(pointer, bScreen), dd = Vector2.Distance(pointer, dScreen);
                if (Mathf.Min(db, dd) <= 46f)
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
                    {
                        ringBGuide.enabled = !st.ringBChanged;
                        ringDGuide.enabled = st.ringBChanged && !st.ringDChanged;
                    }
                    if (st.ringBChanged && st.ringDChanged) Submit();
                    MgfBridge.NotifyChanged();
                }
                else InvalidTap();
            }
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
                    chimeRenderers[slot].sharedMaterial = tealGlowMat;
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
            frameLine.sharedMaterial = copperMat; wrongJolt = 0;
            if (current.kind == FrameKind.Practice)
            {
                if (revealCorrect) BeginMainAfterPractice();
                else { phase = RunPhase.Practice; st.phase = "playing"; ringBGuide.enabled = true; ringDGuide.enabled = false; }
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
            endTitle.text = clear ? "육현 공명 완성" : st.lives <= 0 ? "공명 퓨즈 소진" : secondsLeft <= 0 ? "연주 시간 종료" : "첫 시도 4회에 도전";
            endTitle.color = clear ? TealGlow : Ivory;
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
                return $"AO=OC={current.a} cm · BO=OD={current.b} cm\n두 대각선이 서로 다른 것을 이등분";
            if (current.kind == FrameKind.Rectangle)
                return $"AC=BD={current.a * 2} cm · O에서 서로 이등분\n정사각형도 직사각형의 성질을 만족";
            if (current.kind == FrameKind.Rhombus)
                return $"OB=OD={current.b} cm · AC⊥BD\n두 대각선이 서로 다른 것을 수직이등분";
            return $"AO=BO=CO=DO={current.a} cm · AC⊥BD\n직사각형과 마름모의 성질을 모두 만족";
        }

        string WrongReason(string id)
        {
            switch (id)
            {
                case "parallelogram_diagonals_equal": return "평행사변형의 두 대각선은 길이가 같을 필요가 없습니다.\n각 대각선의 두 반쪽을 맞추시오.";
                case "equal_length_without_bisection": return "길이는 맞지만 B-O-D가 한 직선이 아닙니다.\nO가 교점이 되도록 반대 방향에 놓으시오.";
                case "perpendicular_without_bisection": return "수직이고 전체 길이는 맞지만 O에서 이등분되지 않았습니다.\nOB와 OD를 같게 놓으시오.";
                case "not_perpendicular": return "O에서 이등분되지만 두 대각선이 수직이 아닙니다.\n90°가 되도록 방향을 바꾸시오.";
                case "first_half_not_equal": return "AO와 OC의 길이가 다릅니다. 같은 눈금에 놓으시오.";
                case "second_half_not_equal": return "BO와 OD의 길이가 다릅니다. 같은 눈금에 놓으시오.";
                default: return "두 반쪽 길이와 반대 방향을 함께 확인하시오.";
            }
        }

        void InvalidTap()
        {
            st.inputSerial++; idleGuide = 0;
            if (ScreenToBoard(out var local)) StartRipple(local);
            wrongJolt = .35f; MgfSfx.Play("tap", .12f);
            feedbackText.text = phase == RunPhase.Reveal ? "현이 울리는 동안 잠시 기다리시오" : "구리 꼭짓점 고리를 끌어 놓으시오";
            feedbackText.color = Ivory; toastGroup.alpha = 1;
            toastT = .85f;
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
            // 늘 움직이는 동굴: 빛종·공명환·조율사의 건조한 대기 동작.
            for (int i = 0; i < 6; i++)
            {
                float sway = Mathf.Sin(titleT * (1.25f + i * .04f) + i * .72f) * (phase == RunPhase.Reveal && revealCorrect ? 6f : 2.2f);
                chimes[i].localRotation = Quaternion.Euler(0, 35 + sway, 45 + sway * .35f);
                float lift = i < st.solved ? Mathf.SmoothStep(0, .32f, Mathf.PingPong(titleT * .45f + i * .11f, 1)) : 0;
                resonanceRings[i].position = resonanceRingHomes[i] + Vector3.up * (lift * .05f);
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
                resonanceTrail.SetPosition(0, pts[seg]); resonanceTrail.SetPosition(1, Vector3.Lerp(pts[seg], pts[seg + 1], u));
                float pulse = 1 + Mathf.Sin(q * Mathf.PI) * .20f;
                handleB.localScale = new Vector3(.38f, .38f, .22f) * pulse;
                handleD.localScale = new Vector3(.38f, .38f, .22f) * pulse;
            }
            else
            {
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
            measureText.text = $"{n1} {st.ringBRadius} cm · {(current.kind == FrameKind.Practice || current.kind == FrameKind.Parallelogram ? "OD" : "OD")} {st.ringDRadius} cm";
        }

        void UpdateUi()
        {
            displayedScore = Mathf.MoveTowards(displayedScore, st.score, Mathf.Max(80f, Mathf.Abs(st.score - displayedScore) * 7f) * Time.deltaTime);
            int shownScore = Mathf.RoundToInt(displayedScore);
            hudText.text = phase == RunPhase.Practice ? "연습 · 시계 정지"
                : $"점수 {shownScore:N0}   ·   {Mathf.CeilToInt(secondsLeft)}초   ·   첫 시도 {st.firstCorrect}/{st.firstAttempts}";
            progressText.text = phase == RunPhase.Practice ? "두 고리를 모두 움직이면 자동 제출"
                : $"공명 프레임 {st.solved}/6   ·   {BandName(st.level)}";
            fuseText.text = "공명 퓨즈 " + Mathf.Max(0, st.lives);
            for (int i = 0; i < 3; i++) fuseImages[i].color = i < st.lives ? CopperHot : new Color(.28f, .27f, .27f, .65f);
            bool land = Screen.width >= 1024 || (Screen.width >= 640 && Screen.width >= Screen.height);
            if (Screen.width != lastLayoutW || Screen.height != lastLayoutH)
            {
                lastLayoutW = Screen.width; lastLayoutH = Screen.height;
                if (land)
                {
                    goalPanel.anchorMin = goalPanel.anchorMax = new Vector2(.16f, .90f); goalPanel.anchoredPosition = Vector2.zero; goalPanel.sizeDelta = new Vector2(310, 64);
                    promptPanel.anchorMin = promptPanel.anchorMax = new Vector2(.16f, .76f); promptPanel.anchoredPosition = Vector2.zero; promptPanel.sizeDelta = new Vector2(320, 120);
                    magnifierPanel.anchorMin = magnifierPanel.anchorMax = new Vector2(.85f, .20f); magnifierPanel.anchoredPosition = Vector2.zero;
                }
                else
                {
                    goalPanel.anchorMin = goalPanel.anchorMax = new Vector2(.5f, 1f); goalPanel.anchoredPosition = new Vector2(0, -49); goalPanel.sizeDelta = new Vector2(368, 54);
                    promptPanel.anchorMin = promptPanel.anchorMax = new Vector2(.5f, 1f); promptPanel.anchoredPosition = new Vector2(0, -116); promptPanel.sizeDelta = new Vector2(372, 72);
                    magnifierPanel.anchorMin = magnifierPanel.anchorMax = new Vector2(.5f, 0f); magnifierPanel.anchoredPosition = new Vector2(0, 140);
                }
            }
        }

        static string BandName(int level) => level == 1 ? "반쪽 길이" : level == 2 ? "같은 대각선" : "수직이등분";

        void PositionGuides()
        {
            if (phase != RunPhase.Practice) { ringBGuide.enabled = ringDGuide.enabled = false; guideLine.enabled = false; return; }
            RectTransform canvasRt = (RectTransform)MgfText.Canvas.transform;
            if (ringBGuide.enabled) ringBGuide.rectTransform.anchoredPosition = ScreenToCanvas(handleB.position, canvasRt);
            if (ringDGuide.enabled) ringDGuide.rectTransform.anchoredPosition = ScreenToCanvas(handleD.position, canvasRt);
            float pulse = 1 + Mathf.Sin(titleT * (idleGuide > 8 ? 8f : 5f)) * .12f;
            ringBGuide.rectTransform.localScale = ringDGuide.rectTransform.localScale = Vector3.one * pulse;
            guideLine.enabled = ringBGuide.enabled || ringDGuide.enabled;
            if (guideLine.enabled)
            {
                bool first = ringBGuide.enabled;
                guideLine.SetPosition(0, first ? handleB.position : handleD.position);
                guideLine.SetPosition(1, first ? BoardWorld(4, current.baseK + 6) : BoardWorld(7, current.baseK + 8));
                guideLine.startWidth = guideLine.endWidth = .035f + .025f * (.5f + .5f * Mathf.Sin(titleT * 7f));
            }
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

        RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var rt = R(name, parent, anchor, pos, size); var img = rt.gameObject.AddComponent<Image>();
            img.sprite = roundedSprite; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false; return rt;
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
