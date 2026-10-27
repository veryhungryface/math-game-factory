using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.SaeuIsa
{
    public sealed class SaeuIsaGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int firstAttempted;
            public int firstCorrect;
            public int repairFirstCorrect;
            public int[] firstCorrectByBand = new int[3];
            public int waitingCount;
            public int[] histogramCounts = new int[4];
            public int samplePlacements;
            public int pointerVersion;
            public string problemId = "";
            public string misconceptionId = "";
            public string lastResult = "";
            public string endReason = "";
            public bool onboarding;
            public int currentValueMm;
            public int currentLower;
            public int currentWidth;
            public int currentCorrectClass = -1;
            public int currentOriginClass = -1;
            public int sourceX;
            public int sourceY;
            public int targetY;
            public int[] classX = new int[4];
        }

        enum Mode { Title, Practice, Playing, Feedback, Interlude, RepairPractice, End }

        sealed class SampleView
        {
            public Transform root;
            public Transform creature;
            public Transform tail;
            public Transform handle;
            public Transform ageBar;
            public GameObject hitbox;
            public Renderer pocketRenderer;
            public Renderer handleRenderer;
            public TextMeshPro label;
            public ShrimpProblem problem;
            public Vector3 home;
            public Vector3 dragStart;
            public int homeClass = -1;
            public bool visible;
            public bool dragging;
            public float pulse;
            public float refuse;
        }

        static readonly Color Lacquer = MgfLook.Hex("#173B78");
        static readonly Color LacquerDark = MgfLook.Hex("#0B2351");
        static readonly Color Coral = MgfLook.Hex("#E96B55");
        static readonly Color Ivory = MgfLook.Hex("#F2E8D5");
        static readonly Color Nacre = MgfLook.Hex("#76A79B");
        static readonly Color Ink = MgfLook.Hex("#191B28");
        static readonly Color Gold = MgfLook.Hex("#D9BE72");
        static readonly Color Dawn = MgfLook.Hex("#E8D9B8");
        static readonly Color Clay = MgfLook.Hex("#B96745");
        static readonly Color WovenTeal = MgfLook.Hex("#79A89A");
        static readonly Color WovenSand = MgfLook.Hex("#E5C98C");

        readonly State st = new State();
        readonly List<ShrimpProblem> catalog = new List<ShrimpProblem>(740);
        readonly List<MgfProblem> bank = new List<MgfProblem>(740);
        readonly List<ShrimpProblem> run = new List<ShrimpProblem>(8);
        readonly SampleView[] samples = new SampleView[8];
        readonly Transform[] binRelief = new Transform[4];
        readonly Renderer[] binReliefRenderer = new Renderer[4];
        readonly Transform[] histogramRibbons = new Transform[4];
        readonly Transform[] histogramCaps = new Transform[4];
        readonly TextMeshPro[] classLabels = new TextMeshPro[4];
        readonly TextMeshPro[] frequencyLabels = new TextMeshPro[4];
        readonly Transform[] safetyNails = new Transform[3];
        readonly Renderer[] safetyNailRenderer = new Renderer[3];
        readonly Transform[] guideDots = new Transform[10];
        readonly Transform[] trail = new Transform[10];
        readonly float[] trailAge = new float[10];
        readonly Transform[] nacreChips = new Transform[14];
        readonly Vector3[] chipVelocity = new Vector3[14];
        readonly float[] chipAge = new float[14];
        readonly Transform[] titleShrimp = new Transform[6];
        readonly Vector3[] titleShrimpHome = new Vector3[6];

        Camera cam;
        Transform worldRoot, graphRoot, drawerDecor, guideFinger, guideHand, guideGhostPocket, guideArrow, invalidRipple, titleTop, titleBottom, titlePlaque;
        Material lacquerMat, lacquerDarkMat, coralMat, ivoryMat, clothMat, timberMat, nacreMat, nacreGlowMat, inkMat, goldMat, blackMat;
        Material dawnMat, clayMat, wovenTealMat, wovenSandMat;
        Mode mode = Mode.Title;
        int runSerial = 1;
        int band;
        int spawned;
        int bandSolved;
        int dragged = -1;
        int trailCursor;
        int previousLower = 10;
        int previousWidth = 10;
        float timeLeft = ShrimpRules.RunSeconds;
        float nextSpawn;
        float feedbackLeft;
        float interludeLeft;
        float actionLock;
        float idleGuide;
        float uiTick;
        float scoreShown;
        float endScoreShown;
        float rippleAge = 10f;
        Vector2 dragPointerStart;
        float dragPointerTravel;
        float titleSplit;
        float scorePunch;
        float binPulse;
        int pulseBin = -1;
        bool wide;
        int lastW = -1, lastH = -1;
        string feedbackText = "";
        string endTitleText = "";

        TextMeshProUGUI titleLogo, titleTag, titlePrompt, titleMeta;
        TextMeshProUGUI hud, goal, timer, formula, feedback, terms;
        TextMeshProUGUI endTitle, endStats, endPrompt;
        Image titleVeil, endVeil, goalPlate, formulaPlate;

        void Awake()
        {
            MgfLook.Quality(32f);
            catalog.AddRange(ShrimpRules.BuildBank());
            for (int i = 0; i < catalog.Count; i++) bank.Add(ShrimpRules.ToMgf(catalog[i]));
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var s = new StringBuilder("새우이사새우를나눠담아라끌어서시작중학교1학년자료의정리와해석");
            s.Append("길이계급도수마리이상미만표본그림은실제크기를나타내지않음안전등첫시도정답남은시간");
            s.Append("알맞은계급으로옮기시오표본의분류를고치시오현재위치를믿지말고꼬리표를확인도수새우를길이별로옮기면그래프완성");
            s.Append("그래프면에놓으시오이미정리한자료기존도수새도수분포도감다시정리게임종료분류완료");
            for (int i = 0; i < catalog.Count; i++) { s.Append(bank[i].prompt); s.Append(bank[i].answer); }
            MgfText.Prewarm(s.ToString());
        }

        void BuildWorld()
        {
            // WebGL IL2CPP collider 스트리핑 방지.
            var keep = new GameObject("ColliderTypes");
            keep.AddComponent<BoxCollider>(); keep.AddComponent<SphereCollider>(); keep.AddComponent<CapsuleCollider>(); Destroy(keep);

            MgfLook.Sky(MgfLook.Hex("#7DB6B0"), MgfLook.Hex("#D7D1AE"), MgfLook.Hex("#D98A58"), .86f);
            MgfLook.Sun(new Vector3(48f, -32f, -18f), MgfLook.Hex("#FFE2A8"), 1.38f, .78f);
            cam = MgfLook.Camera(new Vector3(.8f, 1.2f, -20f), new Vector3(0f, .7f, 0f), 31f);
            cam.orthographic = true;
            cam.orthographicSize = 9.4f;

            lacquerMat = MgfLook.Lit(Lacquer, .38f, .08f);
            lacquerDarkMat = MgfLook.Lit(LacquerDark, .25f, .05f);
            coralMat = MgfLook.Lit(Coral, .12f, .02f, Coral * .025f);
            ivoryMat = MgfLook.Lit(Ivory, .06f, 0f);
            clothMat = MgfLook.Lit(MgfLook.Hex("#E7D4B4"), .025f, 0f);
            timberMat = MgfLook.Lit(MgfLook.Hex("#7A4D36"), .18f, .02f);
            nacreMat = MgfLook.Lit(Nacre, .7f, .3f, Nacre * .12f);
            nacreGlowMat = MgfLook.Lit(MgfLook.Hex("#B9E8D8"), .82f, .36f, Nacre * .9f);
            inkMat = MgfLook.Lit(Ink, .16f, .02f);
            goldMat = MgfLook.Lit(Gold, .74f, .42f, Gold * .12f);
            blackMat = MgfLook.Lit(MgfLook.Hex("#080B12"), .08f, .04f);
            dawnMat = MgfLook.Lit(Dawn, .045f, 0f);
            clayMat = MgfLook.Lit(Clay, .06f, 0f);
            wovenTealMat = MgfLook.Lit(WovenTeal, .035f, 0f);
            wovenSandMat = MgfLook.Lit(WovenSand, .025f, 0f);

            worldRoot = new GameObject("UltramarineLacquerDrawerWall").transform;
            // 전면의 어두운 패널벽이 아니라 햇빛이 드는 열린 직조 골목이다. 울트라마린은
            // 서랍 내부와 수학 잉크에만 남겨 bitbyeon-cheolgil의 검은 기계실 계열을 끊는다.
            Hard("OpenCorridorSky", new Vector3(0, 2.4f, 4.4f), new Vector3(32f, 14f, .7f), dawnMat, worldRoot);
            Hard("TerracottaSortingWalk", new Vector3(0, -6.9f, 2.7f), new Vector3(26f, 7.2f, .65f), clayMat, worldRoot).transform.rotation = Quaternion.Euler(0, 0, -2f);
            BuildDrawerDecor();
            BuildGraph();
            BuildSamplePool();
            BuildFxPool();
            BuildTitleWorld();
        }

        void BuildDrawerDecor()
        {
            drawerDecor = new GameObject("OpenNacreSortingAlley").transform;
            drawerDecor.SetParent(worldRoot, false);

            // 중앙을 비운 사선 골목. 양쪽 선반이 위쪽으로 좁아지며 깊이를 만든다.
            for (int depth = 0; depth < 6; depth++)
            {
                float y = -6.0f + depth * 2.55f;
                float scale = 1f - depth * .07f;
                float inset = depth * .72f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * (11.4f - inset);
                    float yaw = side * -11f;
                    var shelf = new GameObject("AlleyShelf_" + depth + "_" + side).transform;
                    shelf.SetParent(drawerDecor, false);
                    shelf.localPosition = new Vector3(x, y, 2.25f + depth * .10f);
                    shelf.localRotation = Quaternion.Euler(0, 0, yaw);

                    Hard("PostL", new Vector3(-1.55f * scale, 0, 0), new Vector3(.18f, 2.28f * scale, .42f), timberMat, shelf);
                    Hard("PostR", new Vector3(1.55f * scale, 0, 0), new Vector3(.18f, 2.28f * scale, .42f), timberMat, shelf);
                    Hard("Lintel", new Vector3(0, 1.04f * scale, 0), new Vector3(3.28f * scale, .18f, .42f), timberMat, shelf);
                    Hard("Sill", new Vector3(0, -1.04f * scale, 0), new Vector3(3.28f * scale, .18f, .42f), timberMat, shelf);
                    Hard("DeepOpenCavity", new Vector3(0, 0, .24f), new Vector3(2.92f * scale, 1.82f * scale, .48f), lacquerDarkMat, shelf);

                    // 꽃잎 대신 직물 묶음과 자개 모서리: 표지와 런타임의 재질 밀도를 맞춘다.
                    for (int thread = 0; thread < 5; thread++)
                    {
                        var strip = Hard("WovenBundle_" + thread, new Vector3(-.9f * scale + thread * .43f * scale, -.45f * scale + (thread % 2) * .24f, -.34f),
                            new Vector3(.32f * scale, .76f * scale, .12f), thread % 2 == 0 ? clothMat : ivoryMat, shelf);
                        strip.transform.rotation = Quaternion.Euler(0, 0, (thread - 2) * 4f);
                    }
                    for (int corner = 0; corner < 2; corner++)
                    {
                        var inlay = Hard("NacreLeaf_" + corner, new Vector3((corner == 0 ? -1.25f : 1.25f) * scale, .78f * scale, -.40f),
                            new Vector3(.42f * scale, .10f, .06f), (depth + corner) % 2 == 0 ? nacreMat : goldMat, shelf);
                        inlay.transform.rotation = Quaternion.Euler(0, 0, corner == 0 ? 31f : -31f);
                    }
                }
            }

            // 골목 바닥의 원근선과 흔들리는 직물 현수막.
            for (int i = 0; i < 6; i++)
            {
                float y = -7.1f + i * 2.25f;
                Hard("FloorPerspectiveL" + i, new Vector3(-7.7f + i * .65f, y, 2.25f), new Vector3(6.5f - i * .35f, .055f, .05f), goldMat, drawerDecor).transform.rotation = Quaternion.Euler(0, 0, -18f);
                Hard("FloorPerspectiveR" + i, new Vector3(7.7f - i * .65f, y, 2.25f), new Vector3(6.5f - i * .35f, .055f, .05f), nacreMat, drawerDecor).transform.rotation = Quaternion.Euler(0, 0, 18f);
            }
            Hard("HangingBannerL", new Vector3(-7.9f, 5.6f, 1.9f), new Vector3(1.55f, 4.2f, .12f), wovenSandMat, drawerDecor).transform.rotation = Quaternion.Euler(0, 0, -8f);
            Hard("HangingBannerR", new Vector3(8.0f, 4.7f, 1.9f), new Vector3(1.35f, 3.7f, .12f), coralMat, drawerDecor).transform.rotation = Quaternion.Euler(0, 0, 7f);
        }

        void BuildGraph()
        {
            graphRoot = new GameObject("ScreenFacingWovenHistogramLoom").transform;
            graphRoot.SetParent(worldRoot, false);
            Hard("LoomBackShadow", new Vector3(.18f, -.16f, .18f), new Vector3(10.35f, 8.25f, .44f), lacquerDarkMat, graphRoot);
            Hard("LoomCloth", new Vector3(0, -.15f, -.30f), new Vector3(9.65f, 7.25f, .16f), wovenSandMat, graphRoot);
            Hard("LoomTop", new Vector3(0, 3.76f, -.48f), new Vector3(10.45f, .34f, .40f), timberMat, graphRoot);
            Hard("LoomBottom", new Vector3(0, -3.80f, -.48f), new Vector3(10.45f, .34f, .40f), timberMat, graphRoot);
            Hard("LoomLeft", new Vector3(-5.06f, -.02f, -.48f), new Vector3(.34f, 7.9f, .40f), timberMat, graphRoot);
            Hard("LoomRight", new Vector3(5.06f, -.02f, -.48f), new Vector3(.34f, 7.9f, .40f), timberMat, graphRoot);

            for (int c = 0; c < 4; c++)
            {
                float x = -3.45f + c * 2.3f;
                binRelief[c] = Hard("OpenClassBay_" + c, new Vector3(x, .15f, -.51f), new Vector3(2.25f, 5.42f, .09f),
                    c % 2 == 0 ? dawnMat : wovenTealMat, graphRoot).transform;
                binReliefRenderer[c] = binRelief[c].GetComponent<Renderer>();
                histogramRibbons[c] = Hard("HistogramRibbon_" + c, new Vector3(x, -2.32f, -.74f), new Vector3(1.82f, .04f, .08f),
                    c % 2 == 0 ? coralMat : nacreMat, graphRoot).transform;
                histogramCaps[c] = Hard("HistogramCap_" + c, new Vector3(x, -2.28f, -.78f), new Vector3(1.96f, .08f, .08f), goldMat, graphRoot).transform;
                classLabels[c] = MgfText.World("", new Vector3(x, -3.16f, -.68f), 2.45f, Ink, graphRoot);
                classLabels[c].rectTransform.sizeDelta = new Vector2(2.15f, .8f);
                classLabels[c].textWrappingMode = TextWrappingModes.Normal;
                frequencyLabels[c] = MgfText.World("0", new Vector3(x, 3.15f, -.68f), 3.2f, Ink, graphRoot);
            }
            // 천의 씨실: 막힌 사각 셀 대신 얇은 직조 결이 그래프 바탕을 잇는다.
            for (int i = 0; i < 13; i++)
            {
                float yy = -3.12f + i * .52f;
                Hard("ClothWeft" + i, new Vector3(0, yy, -.59f), new Vector3(9.45f, .018f, .022f),
                    i % 3 == 0 ? goldMat : nacreMat, graphRoot);
            }
            // 도수 격자: 0~8. 화면과 평행하며 계급 사이 틈을 만들지 않는다.
            for (int y = 0; y <= 8; y++)
            {
                float yy = -2.38f + y * .62f;
                Hard("FrequencyGrid" + y, new Vector3(0, yy, -.66f), new Vector3(9.2f, .025f, .03f),
                    y == 0 ? goldMat : nacreMat, graphRoot);
                var tick = MgfText.World(y.ToString(), new Vector3(-4.86f, yy, -.7f), 2.2f, Ink, graphRoot);
                tick.alignment = TextAlignmentOptions.MidlineRight;
            }
            var yAxis = MgfText.World("도수(마리)", new Vector3(-5.0f, .25f, -.7f), 2.2f, Ivory, graphRoot);
            yAxis.transform.rotation = Quaternion.Euler(0, 0, 90);
            MgfText.World("길이(mm)", new Vector3(0, -3.78f, -.7f), 2.35f, Ivory, graphRoot);

            for (int i = 0; i < 3; i++)
            {
                safetyNails[i] = MgfLook.Prim(PrimitiveType.Cylinder, "SafetyNail" + i,
                    new Vector3(3.5f - i * .62f, 3.62f, -.72f), new Vector3(.21f, .075f, .21f), goldMat, graphRoot, false).transform;
                safetyNails[i].rotation = Quaternion.Euler(90, 0, 0);
                safetyNailRenderer[i] = safetyNails[i].GetComponent<Renderer>();
            }
        }

        void BuildSamplePool()
        {
            for (int i = 0; i < samples.Length; i++) samples[i] = MakeSample(i);
        }

        SampleView MakeSample(int index)
        {
            var v = new SampleView();
            v.root = new GameObject("LinenPocket_" + index).transform;
            v.root.SetParent(worldRoot, false);
            v.hitbox = Round("WovenPocketBody", Vector3.zero, new Vector3(1.68f, 1.10f, .28f), .045f, clothMat, v.root);
            v.hitbox.GetComponent<BoxCollider>().size = new Vector3(2.20f, 1.64f, .48f);
            v.pocketRenderer = v.hitbox.GetComponent<Renderer>();
            Hard("PocketFoldL", new Vector3(-.76f, .06f, -.18f), new Vector3(.18f, .88f, .16f), ivoryMat, v.root).transform.rotation = Quaternion.Euler(0, 0, -8f);
            Hard("PocketFoldR", new Vector3(.76f, .06f, -.18f), new Vector3(.18f, .88f, .16f), ivoryMat, v.root).transform.rotation = Quaternion.Euler(0, 0, 8f);
            for (int w = 0; w < 5; w++)
            {
                var thread = Hard("LinenWeave" + w, new Vector3(0, -.38f + w * .19f, -.17f), new Vector3(1.48f, .025f, .035f),
                    w % 2 == 0 ? goldMat : nacreMat, v.root);
                thread.transform.rotation = Quaternion.Euler(0, 0, w % 2 == 0 ? 3f : -3f);
            }
            v.handle = Hard("NacreBarHandle", new Vector3(0, .68f, -.08f), new Vector3(1.02f, .22f, .18f), nacreMat, v.root).transform;
            v.handleRenderer = v.handle.GetComponent<Renderer>();
            Hard("HandleStemL", new Vector3(-.37f, .52f, -.08f), new Vector3(.09f, .30f, .12f), timberMat, v.root);
            Hard("HandleStemR", new Vector3(.37f, .52f, -.08f), new Vector3(.09f, .30f, .12f), timberMat, v.root);
            v.ageBar = Hard("WaitingTicks", new Vector3(0, -.57f, -.22f), new Vector3(1.36f, .075f, .05f), goldMat, v.root).transform;
            v.creature = new GameObject("CoralBisqueShrimp").transform;
            v.creature.SetParent(v.root, false);
            v.creature.localPosition = new Vector3(.02f, .20f, -.30f);
            // 각진 도자 갑각 5마디. 구형 토큰이 아니라 실제 새우 실루엣으로 읽히게 한다.
            for (int b = 0; b < 5; b++)
            {
                var body = Hard("AngularShell" + b,
                    new Vector3(-.42f + b * .19f, -.01f + Mathf.Sin(b * 1.1f) * .055f, 0),
                    new Vector3(.25f - b * .018f, .23f, .18f), coralMat, v.creature);
                body.transform.rotation = Quaternion.Euler(0, 0, -24f + b * 9f);
            }
            Hard("AngularHead", new Vector3(.56f, .09f, 0), new Vector3(.36f, .28f, .20f), coralMat, v.creature).transform.rotation = Quaternion.Euler(0, 0, -7f);
            Hard("Rostrum", new Vector3(.82f, .18f, 0), new Vector3(.30f, .065f, .07f), coralMat, v.creature).transform.rotation = Quaternion.Euler(0, 0, 13f);
            v.tail = new GameObject("TailFan").transform; v.tail.SetParent(v.creature, false); v.tail.localPosition = new Vector3(-.58f, -.08f, 0);
            Hard("TailFanA", new Vector3(-.13f, .08f, 0), new Vector3(.34f, .11f, .14f), coralMat, v.tail).transform.rotation = Quaternion.Euler(0, 0, 34f);
            Hard("TailFanB", new Vector3(-.16f, -.06f, 0), new Vector3(.31f, .10f, .14f), coralMat, v.tail).transform.rotation = Quaternion.Euler(0, 0, -28f);
            Hard("ClawA", new Vector3(.42f, -.20f, 0), new Vector3(.25f, .055f, .07f), coralMat, v.creature).transform.rotation = Quaternion.Euler(0, 0, -42f);
            Hard("ClawB", new Vector3(.61f, -.22f, 0), new Vector3(.16f, .08f, .08f), coralMat, v.creature).transform.rotation = Quaternion.Euler(0, 0, 18f);
            for (int a = 0; a < 2; a++)
            {
                var ant = MgfLook.Prim(PrimitiveType.Cylinder, "Antenna" + a,
                    new Vector3(.78f, .20f + a * .06f, 0), new Vector3(.012f, .42f + a * .08f, .012f), coralMat, v.creature, false);
                ant.transform.rotation = Quaternion.Euler(0, 0, 71f + a * 7f);
            }
            MgfLook.Prim(PrimitiveType.Sphere, "Eye", new Vector3(.66f, .15f, -.13f), new Vector3(.052f, .052f, .052f), inkMat, v.creature, false);
            v.label = MgfText.World("23 mm", new Vector3(0, -.18f, -.25f), 3.05f, Ink, v.root);
            v.label.fontStyle = FontStyles.Bold;
            v.root.gameObject.SetActive(false);
            return v;
        }

        void BuildFxPool()
        {
            guideHand = new GameObject("GuideHand").transform; guideHand.SetParent(worldRoot, false);
            guideFinger = Round("GuideFinger", new Vector3(0, .18f, 0), new Vector3(.34f, .80f, .12f), .16f, ivoryMat, guideHand).transform;
            guideFinger.localRotation = Quaternion.Euler(0, 0, -18f);
            Round("GuidePalm", new Vector3(.24f, -.18f, .02f), new Vector3(.68f, .52f, .14f), .22f, ivoryMat, guideHand);
            guideHand.gameObject.SetActive(false);

            guideGhostPocket = new GameObject("DestinationGhostPocket").transform; guideGhostPocket.SetParent(worldRoot, false);
            Hard("GhostTop", new Vector3(0, .47f, 0), new Vector3(1.70f, .08f, .05f), nacreGlowMat, guideGhostPocket);
            Hard("GhostBottom", new Vector3(0, -.47f, 0), new Vector3(1.70f, .08f, .05f), nacreGlowMat, guideGhostPocket);
            Hard("GhostLeft", new Vector3(-.81f, 0, 0), new Vector3(.08f, .94f, .05f), nacreGlowMat, guideGhostPocket);
            Hard("GhostRight", new Vector3(.81f, 0, 0), new Vector3(.08f, .94f, .05f), nacreGlowMat, guideGhostPocket);
            guideGhostPocket.gameObject.SetActive(false);

            guideArrow = new GameObject("GuideArrowHead").transform; guideArrow.SetParent(worldRoot, false);
            Hard("ArrowWingL", new Vector3(-.11f, 0, 0), new Vector3(.42f, .09f, .05f), nacreGlowMat, guideArrow).transform.rotation = Quaternion.Euler(0, 0, 36f);
            Hard("ArrowWingR", new Vector3(.11f, 0, 0), new Vector3(.42f, .09f, .05f), nacreGlowMat, guideArrow).transform.rotation = Quaternion.Euler(0, 0, -36f);
            guideArrow.gameObject.SetActive(false);
            for (int i = 0; i < guideDots.Length; i++)
            {
                guideDots[i] = MgfLook.Prim(PrimitiveType.Sphere, "GuideChevron" + i, Vector3.zero, new Vector3(.09f, .09f, .05f), nacreGlowMat, worldRoot, false).transform;
                guideDots[i].gameObject.SetActive(false);
            }
            invalidRipple = MgfLook.Prim(PrimitiveType.Cylinder, "NacreInputRipple", Vector3.zero, new Vector3(.22f, .025f, .22f), nacreGlowMat, worldRoot, false).transform;
            invalidRipple.rotation = Quaternion.Euler(90, 0, 0); invalidRipple.gameObject.SetActive(false);
            for (int i = 0; i < trail.Length; i++)
            {
                trail[i] = MgfLook.Prim(PrimitiveType.Sphere, "LinenTrail" + i, Vector3.zero, new Vector3(.1f, .1f, .04f), nacreGlowMat, worldRoot, false).transform;
                trail[i].gameObject.SetActive(false); trailAge[i] = 99f;
            }
            for (int i = 0; i < nacreChips.Length; i++)
            {
                nacreChips[i] = Hard("NacreReliefChip" + i, Vector3.zero, new Vector3(.11f, .055f, .04f), i % 3 == 0 ? goldMat : nacreGlowMat, worldRoot).transform;
                nacreChips[i].gameObject.SetActive(false); chipAge[i] = 99f;
            }
        }

        void BuildTitleWorld()
        {
            titlePlaque = new GameObject("HangingNacreNameplate").transform;
            titlePlaque.SetParent(worldRoot, false); titlePlaque.localPosition = new Vector3(0, 5.9f, -1.1f);
            Hard("NameplateFace", Vector3.zero, new Vector3(7.0f, 1.55f, .26f), nacreMat, titlePlaque);
            Hard("PlaqueCordL", new Vector3(-2.7f, 1.3f, .16f), new Vector3(.07f, 1.7f, .07f), goldMat, titlePlaque).transform.rotation = Quaternion.Euler(0, 0, -8f);
            Hard("PlaqueCordR", new Vector3(2.7f, 1.3f, .16f), new Vector3(.07f, 1.7f, .07f), goldMat, titlePlaque).transform.rotation = Quaternion.Euler(0, 0, 8f);
            titleTop = new GameObject("SplitWovenAwningTop").transform;
            titleTop.SetParent(worldRoot, false); titleTop.localPosition = new Vector3(0, 2.7f, -1.0f);
            Hard("AwningBeam", Vector3.zero, new Vector3(9.8f, .62f, .30f), clothMat, titleTop);
            titleBottom = new GameObject("SplitWovenSortingTray").transform;
            titleBottom.SetParent(worldRoot, false); titleBottom.localPosition = new Vector3(0, -2.0f, -1.0f);
            Hard("SortingTrayFace", Vector3.zero, new Vector3(8.7f, 2.35f, .30f), lacquerDarkMat, titleBottom);
            Hard("TitleTrayRimTop", new Vector3(0, 1.05f, -.20f), new Vector3(8.4f, .12f, .08f), nacreMat, titleBottom);
            Hard("TitleTrayRimBottom", new Vector3(0, -1.05f, -.20f), new Vector3(8.4f, .12f, .08f), goldMat, titleBottom);
            Hard("TitleHandle", new Vector3(0, 1.42f, -.22f), new Vector3(1.65f, .24f, .16f), nacreMat, titleBottom);
            Hard("TitleHandleStemL", new Vector3(-.58f, 1.24f, -.18f), new Vector3(.10f, .40f, .12f), timberMat, titleBottom);
            Hard("TitleHandleStemR", new Vector3(.58f, 1.24f, -.18f), new Vector3(.10f, .40f, .12f), timberMat, titleBottom);
            for (int s = 0; s < 9; s++)
            {
                var flag = Hard("TitleWovenPennant" + s, new Vector3(-4.3f + s * 1.08f, -.48f - (s % 2) * .12f, -.22f), new Vector3(.62f, .78f, .05f),
                    s % 3 == 0 ? coralMat : (s % 3 == 1 ? nacreMat : goldMat), titleTop);
                flag.transform.rotation = Quaternion.Euler(0, 0, (s % 2 == 0 ? -1f : 1f) * (4f + s % 3 * 2f));
            }
            for (int i = 0; i < titleShrimp.Length; i++)
            {
                var shrimp = new GameObject("TitleAngularShrimp" + i).transform;
                shrimp.SetParent(worldRoot, false);
                shrimp.localPosition = new Vector3(-3.5f + i * 1.42f, -1.95f + (i % 2) * .36f, -1.42f);
                for (int b = 0; b < 4; b++)
                {
                    var shell = Hard("Shell" + b, new Vector3(-.24f + b * .18f, Mathf.Sin(b) * .04f, 0),
                        new Vector3(.24f - b * .02f, .20f, .16f), coralMat, shrimp);
                    shell.transform.rotation = Quaternion.Euler(0, 0, -20f + b * 10f);
                }
                Hard("Head", new Vector3(.48f, .08f, 0), new Vector3(.32f, .25f, .18f), coralMat, shrimp);
                Hard("Tail", new Vector3(-.53f, -.02f, 0), new Vector3(.30f, .10f, .14f), coralMat, shrimp).transform.rotation = Quaternion.Euler(0, 0, 31f);
                titleShrimp[i] = shrimp; titleShrimpHome[i] = shrimp.localPosition;
            }
        }

        void BuildUi()
        {
            titleVeil = Panel("TitleVeil", new Color(0.025f, .07f, .16f, .18f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            titleLogo = Text("새우 이사", new Vector2(.5f, .82f), Vector2.zero, 55, Ivory, 360);
            titleLogo.outlineColor = new Color32(15, 29, 65, 255); titleLogo.outlineWidth = .28f;
            titleTag = Text("새우를 나눠 담아라", new Vector2(.5f, .70f), Vector2.zero, 22, LacquerDark, 340);
            titlePrompt = Text("가운데 직물 손잡이를 눌러 시작", new Vector2(.5f, .13f), Vector2.zero, 24, Ivory, 380);
            titleMeta = Text("중학교 1학년 · 자료의 정리와 해석   최고 기록 0", new Vector2(.5f, .06f), Vector2.zero, 15, Dawn, 380);

            goalPlate = Panel("GoalInlay", new Color(.03f, .09f, .22f, .88f), new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0, -66), new Vector2(374, 56));
            goal = Text("새우를 길이별로 옮기면 그래프 완성", new Vector2(.5f, 1f), new Vector2(0, -66), 21, Ivory, 360);
            hud = Text("", new Vector2(.5f, 1f), new Vector2(0, -22), 18, LacquerDark, 380);
            timer = Text("", new Vector2(.5f, 1f), new Vector2(0, -104), 17, LacquerDark, 360);
            terms = Text("계급: 나눈 구간\n도수: 자료 수\n하한 포함 · 상한 제외", new Vector2(.5f, 0f), new Vector2(0, 66), 15, LacquerDark, 380);
            terms.fontStyle = FontStyles.Bold;
            terms.outlineColor = new Color32(242, 232, 213, 220); terms.outlineWidth = .12f;
            formulaPlate = Panel("FormulaInlay", new Color(.95f, .91f, .83f, .92f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(360, 62));
            formula = Text("", new Vector2(.5f, .5f), Vector2.zero, 23, Ink, 350);
            feedback = Text("", new Vector2(.5f, .5f), new Vector2(0, 72), 22, LacquerDark, 370);

            endVeil = Panel("EndVeil", new Color(.02f, .06f, .15f, .90f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            endTitle = Text("분류 완료", new Vector2(.5f, .70f), Vector2.zero, 46, Ivory, 370);
            endTitle.outlineColor = new Color32(8, 20, 50, 255); endTitle.outlineWidth = .24f;
            endStats = Text("", new Vector2(.5f, .48f), Vector2.zero, 24, Gold, 370);
            endPrompt = Text("서랍 손잡이를 눌러 새 분포를 정리", new Vector2(.5f, .19f), Vector2.zero, 21, Ivory, 370);
            SetGameplayUi(false); SetEndUi(false);
        }

        Image Panel(string name, Color color, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(MgfText.Canvas.transform, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = min; rt.anchorMax = max; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var image = go.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }

        TextMeshProUGUI Text(string value, Vector2 anchor, Vector2 offset, float size, Color color, float width)
        {
            var t = MgfText.Ui(value, anchor, offset, size, color, width); t.raycastTarget = false; return t;
        }

        GameObject Hard(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent = null)
        {
            return MgfLook.Prim(PrimitiveType.Cube, name, pos, scale, mat, parent, false);
        }

        GameObject Round(string name, Vector3 pos, Vector3 size, float radius, Material mat, Transform parent = null)
        {
            return MgfLook.Block(name, pos, size, radius, mat, parent);
        }

        void ShowTitle()
        {
            mode = Mode.Title; st.phase = "title"; st.onboarding = true; st.endReason = ""; st.lastResult = "";
            titleSplit = 0; actionLock = 0;
            titlePlaque.gameObject.SetActive(true); titleTop.gameObject.SetActive(true); titleBottom.gameObject.SetActive(true);
            titleTop.localPosition = new Vector3(0, 2.7f, -1f); titleBottom.localPosition = new Vector3(0, -2.0f, -1f);
            graphRoot.gameObject.SetActive(false);
            for (int i = 0; i < titleShrimp.Length; i++) titleShrimp[i].gameObject.SetActive(true);
            for (int i = 0; i < samples.Length; i++) HideSample(i);
            SetTitleUi(true); SetGameplayUi(false); SetEndUi(false);
            int best = 0; try { best = PlayerPrefs.GetInt("saeu-isa-best-first", 0); } catch { }
            titleMeta.text = "중학교 1학년 · 자료의 정리와 해석   최고 첫 시도 " + best + "/" + ShrimpRules.Goal;
            MgfBridge.NotifyChanged();
        }

        void BeginPractice()
        {
            ResetState();
            mode = Mode.Practice; st.phase = "playing"; st.level = 0; st.onboarding = true;
            SetTitleUi(false); SetEndUi(false); SetGameplayUi(true);
            titleSplit = .001f;
            graphRoot.gameObject.SetActive(true);
            SetupAxis(10, 10);
            for (int i = 0; i < samples.Length; i++) HideSample(i);
            ActivateSample(0, ShrimpRules.FixedPractice(), true);
            // 고정 연습은 대기열의 작은 첫 칸이 아니라 화면 중앙의 큰 실물 주머니로 시작한다.
            // 콜라이더는 보이는 주머니와 안내 손 전체를 감싸 첫 몇 초의 탐색 탭도 잡는다.
            samples[0].home = PracticeSourcePosition();
            samples[0].root.position = samples[0].home;
            samples[0].root.localScale = Vector3.one * 1.38f;
            samples[0].hitbox.GetComponent<BoxCollider>().size = new Vector3(3.9f, 2.10f, .62f);
            goal.text = "23 mm 표본을 잡아 알맞은 계급으로 옮기시오";
            feedbackText = "큰 직물 주머니를 누른 채 점선 끝까지 끌어 놓으시오";
            formula.text = "20 ≤ 23 < 30"; formulaPlate.gameObject.SetActive(false); formula.gameObject.SetActive(false);
            idleGuide = 0; RefreshUi(true); MgfBridge.NotifyChanged();
        }

        void ResetState()
        {
            st.score = 0; st.lives = ShrimpRules.StartLives; st.level = 1; st.solved = 0;
            st.firstAttempted = 0; st.firstCorrect = 0; st.repairFirstCorrect = 0;
            st.firstCorrectByBand = new int[3]; st.waitingCount = 0; st.histogramCounts = new int[4];
            st.samplePlacements = 0; st.problemId = ""; st.misconceptionId = ""; st.lastResult = ""; st.endReason = "";
            timeLeft = ShrimpRules.RunSeconds; scoreShown = 0; endScoreShown = 0; spawned = 0; bandSolved = 0;
            for (int i = 0; i < 3; i++) { safetyNails[i].localRotation = Quaternion.Euler(90, 0, 0); safetyNailRenderer[i].sharedMaterial = goldMat; }
        }

        void BeginBand(int nextBand)
        {
            band = nextBand; st.level = band; st.phase = "playing"; st.onboarding = false; mode = Mode.Playing;
            if (band == 1) { ResetState(); st.level = 1; }
            previousLower = run.Count > 0 ? run[0].lower : 10;
            previousWidth = run.Count > 0 ? run[0].width : 10;
            run.Clear();
            var made = ShrimpRules.RunBand(band, runSerial);
            for (int i = 0; i < made.Count; i++) run.Add(ShrimpRules.CloneForRun(made[i]));
            SetupAxis(run[0].lower, run[0].width);
            ClearStageViews(); spawned = 0; bandSolved = 0; nextSpawn = .35f;
            if (band < 3)
            {
                SpawnOne(); SpawnOne(); SpawnOne();
                goal.text = band == 1 ? "8개를 계급에 옮기시오 · 내부값" : "8개를 계급에 옮기시오 · 경계값";
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    ActivateSample(i, run[i], false); samples[i].homeClass = run[i].originClass; samples[i].problem.placedClass = run[i].originClass;
                }
                spawned = 8; LayoutPlaced();
                goal.text = "잘못 든 표본 8개의 분류를 고치시오";
            }
            feedbackText = band == 1 ? "이상은 포함, 미만은 제외" : band == 2 ? "경계값은 오른쪽 구간부터 확인" : "현재 위치를 믿지 말고 꼬리표를 확인";
            RefreshUi(true); MgfBridge.NotifyChanged();
        }

        void BeginRepairPractice()
        {
            mode = Mode.RepairPractice; st.phase = "playing"; st.onboarding = true; st.level = 3;
            run.Clear(); run.Add(ShrimpRules.FixedRepairPractice()); SetupAxis(10, 10); ClearStageViews();
            ActivateSample(0, run[0], false); samples[0].homeClass = 2; samples[0].problem.placedClass = 2; LayoutPlaced();
            goal.text = "41 mm 표본의 분류를 고치시오";
            feedbackText = "30 이상 40 미만에 잘못 든 표본을 직접 옮기시오";
            idleGuide = 0; RefreshUi(true); MgfBridge.NotifyChanged();
        }

        void SpawnOne()
        {
            if (spawned >= run.Count) return;
            int waiting = CountWaiting();
            if (waiting >= 5) { End(false, "대기 선반이 가득 찼습니다"); return; }
            int idx = spawned++;
            ActivateSample(idx, run[idx], true);
            ReflowQueue();
        }

        void ActivateSample(int index, ShrimpProblem p, bool waiting)
        {
            var v = samples[index]; v.problem = p; v.visible = true; v.dragging = false; v.homeClass = waiting ? -1 : p.originClass;
            v.refuse = 0; v.pulse = 0; v.creature.localScale = Vector3.one;
            v.root.gameObject.SetActive(true); v.root.localScale = Vector3.one; v.pocketRenderer.sharedMaterial = clothMat; v.handleRenderer.sharedMaterial = nacreMat;
            v.label.text = p.valueMm + " mm"; v.ageBar.gameObject.SetActive(waiting); p.waitingAge = 0; p.firstSubmitted = false; p.sealedSample = false;
            v.hitbox.GetComponent<Collider>().enabled = true;
            v.hitbox.GetComponent<BoxCollider>().size = new Vector3(2.20f, 1.64f, .48f);
            st.problemId = p.id;
        }

        void HideSample(int i)
        {
            samples[i].visible = false; samples[i].dragging = false; samples[i].problem = null; samples[i].root.gameObject.SetActive(false);
        }

        void ClearStageViews()
        {
            for (int i = 0; i < samples.Length; i++) HideSample(i);
            st.histogramCounts = new int[4];
        }

        int CountWaiting()
        {
            int n = 0;
            for (int i = 0; i < samples.Length; i++)
                if (samples[i].visible && samples[i].problem != null && !samples[i].problem.sealedSample && samples[i].problem.placedClass < 0) n++;
            return n;
        }

        void ReflowQueue()
        {
            int q = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                var v = samples[i];
                if (!v.visible || v.problem == null || v.problem.sealedSample || v.problem.placedClass >= 0 || v.dragging) continue;
                v.home = QueuePosition(q++); v.root.position = v.home; v.root.localScale = Vector3.one;
            }
            st.waitingCount = q;
        }

        Vector3 QueuePosition(int slot)
        {
            if (wide) return new Vector3(2.75f, 3.15f - slot * 1.48f, -1.05f);
            return new Vector3(-3.15f + slot * 1.58f, 5.35f, -1.05f);
        }

        Vector3 PracticeSourcePosition()
        {
            return wide ? new Vector3(2.25f, 2.75f, -1.05f) : new Vector3(0f, 4.85f, -1.05f);
        }

        void SetupAxis(int lower, int width)
        {
            for (int c = 0; c < 4; c++)
            {
                int lo = lower + c * width;
                classLabels[c].text = lo + " 이상\n" + (lo + width) + " 미만";
                frequencyLabels[c].text = "0";
                binReliefRenderer[c].sharedMaterial = c % 2 == 0 ? dawnMat : wovenTealMat;
                binRelief[c].localPosition = new Vector3(-3.45f + c * 2.3f, .15f, -.53f);
                histogramRibbons[c].localScale = new Vector3(1f, .02f, 1f);
                histogramRibbons[c].localPosition = new Vector3(-3.45f + c * 2.3f, -2.36f, -.74f);
                histogramCaps[c].localPosition = new Vector3(-3.45f + c * 2.3f, -2.30f, -.78f);
                }
            st.histogramCounts = new int[4];
        }

        void LayoutPlaced()
        {
            int[] counts = new int[4];
            for (int i = 0; i < samples.Length; i++)
            {
                var v = samples[i];
                if (!v.visible || v.problem == null || v.problem.placedClass < 0 || v.dragging) continue;
                int c = v.problem.placedClass;
                int row = counts[c]++;
                Vector3 local = new Vector3(-3.45f + c * 2.3f, -2.05f + row * .62f, -1.0f - row * .012f);
                v.root.position = graphRoot.TransformPoint(local);
                v.root.rotation = graphRoot.rotation;
                v.root.localScale = Vector3.one * .60f;
                v.home = v.root.position; v.homeClass = c;
                v.ageBar.gameObject.SetActive(false);
            }
            st.histogramCounts = counts;
            for (int c = 0; c < 4; c++)
            {
                frequencyLabels[c].text = counts[c].ToString();
                float h = Mathf.Max(.035f, counts[c] * .62f);
                histogramRibbons[c].localScale = new Vector3(1f, h / .04f, 1f);
                histogramRibbons[c].localPosition = new Vector3(-3.45f + c * 2.3f, -2.36f + h * .5f, -.74f);
                histogramCaps[c].localPosition = new Vector3(-3.45f + c * 2.3f, -2.32f + h, -.78f);
            }
        }

        void Submit(int index, int submittedClass, bool forced = false)
        {
            if (index < 0 || index >= samples.Length) return;
            var v = samples[index]; var p = v.problem;
            if (p == null || p.sealedSample || actionLock > 0) return;
            bool practice = mode == Mode.Practice;
            bool repairPractice = mode == Mode.RepairPractice;
            bool validMode = practice || repairPractice || mode == Mode.Playing;
            if (!validMode) return;

            bool first = !p.firstSubmitted;
            bool correct = ShrimpRules.Belongs(p, submittedClass);
            if (!practice && !repairPractice && first)
            {
                p.firstSubmitted = true; st.firstAttempted++;
                if (correct)
                {
                    st.firstCorrect++; st.firstCorrectByBand[Mathf.Clamp(band - 1, 0, 2)]++;
                    if (band == 3) st.repairFirstCorrect++;
                }
            }
            else if (practice || repairPractice) p.firstSubmitted = true;

            if (correct)
            {
                int from = p.placedClass;
                p.placedClass = submittedClass; p.sealedSample = true; v.homeClass = submittedClass;
                if (!practice && !repairPractice)
                {
                    st.solved++; bandSolved++; st.samplePlacements++;
                    st.score += 100 + (first ? 40 : 0) + (band == 3 ? 25 : 0);
                    scorePunch = .38f;
                }
                v.pocketRenderer.sharedMaterial = clothMat; v.handleRenderer.sharedMaterial = nacreGlowMat;
                v.pulse = .72f; actionLock = .075f; pulseBin = submittedClass; binPulse = .65f;
                EmitNacre(graphRoot.TransformPoint(new Vector3(-3.45f + submittedClass * 2.3f, .2f, -1.0f)));
                MgfSfx.Play("correct", .50f);
                LayoutPlaced();

                if (practice)
                {
                    feedbackText = "20 ≤ 23 < 30   ·   표본 1마리 → 도수 +1";
                    formula.text = "20 ≤ 23 < 30   ·   도수 +1"; formulaPlate.gameObject.SetActive(true); formula.gameObject.SetActive(true);
                    feedbackLeft = 1.15f; mode = Mode.Feedback;
                }
                else if (repairPractice)
                {
                    feedbackText = "기존 도수 −1   ·   새 도수 +1";
                    formula.text = "30 이상 40 미만 −1   →   40 이상 50 미만 +1"; formulaPlate.gameObject.SetActive(true); formula.gameObject.SetActive(true);
                    feedbackLeft = 1.2f; mode = Mode.Feedback;
                }
                else
                {
                    feedbackText = from >= 0 ? "기존 도수 −1 · 새 도수 +1" : p.valueMm + " mm → 도수 +1";
                    feedbackLeft = .55f; mode = Mode.Feedback;
                    if (band < 3) ReflowQueue();
                }
            }
            else
            {
                if (!practice && !repairPractice)
                {
                    st.lives--;
                    if (first) st.misconceptionId = ShrimpRules.WrongMisconception(p, submittedClass, previousLower, previousWidth);
                    TurnSafetyNail(st.lives);
                }
                v.pocketRenderer.sharedMaterial = ivoryMat;
                v.refuse = .48f; actionLock = .07f;
                feedbackText = WrongReason(p, submittedClass);
                MgfSfx.Play("wrong", .46f); MgfFx.Shake(cam, .065f, .18f);
                p.placedClass = v.homeClass;
                if (p.placedClass >= 0) LayoutPlaced(); else { v.root.position = v.home; v.root.localScale = Vector3.one; }
                feedbackLeft = .72f; mode = Mode.Feedback;
                if (!practice && !repairPractice)
                {
                    if (st.lives <= 0 || st.firstAttempted - st.firstCorrect >= 3) { End(false, "첫 시도 목표에 더는 도달할 수 없습니다"); return; }
                    int repairAttempted = band == 3 ? Mathf.Max(0, st.firstAttempted - 16) : 0;
                    if (band == 3 && repairAttempted - st.repairFirstCorrect >= 2) { End(false, "수정 구역 첫 시도 목표가 닫혔습니다"); return; }
                }
            }
            st.lastResult = correct ? "correct" : "wrong";
            RefreshUi(true); MgfBridge.NotifyChanged();
        }

        string WrongReason(ShrimpProblem p, int submittedClass)
        {
            return ShrimpRules.WrongExplanation(p, submittedClass, previousLower, previousWidth);
        }

        void TurnSafetyNail(int livesAfter)
        {
            int lost = 2 - Mathf.Clamp(livesAfter, 0, 2);
            if (lost < 0 || lost >= 3) return;
            safetyNails[lost].localRotation = Quaternion.Euler(90, 180, 0);
            safetyNailRenderer[lost].sharedMaterial = blackMat;
        }

        void End(bool clear, string reason)
        {
            mode = Mode.End; st.phase = clear ? "clear" : "gameover"; st.endReason = reason; st.onboarding = false; actionLock = .5f;
            dragged = -1;
            for (int i = 0; i < samples.Length; i++) samples[i].dragging = false;
            SetGameplayUi(false); SetTitleUi(false); SetEndUi(true);
            endTitleText = clear ? "분포 도감 완성" : "서랍 잠금";
            endTitle.text = endTitleText;
            endScoreShown = 0;
            if (clear)
            {
                try
                {
                    int best = PlayerPrefs.GetInt("saeu-isa-best-first", 0);
                    if (st.firstCorrect > best) { PlayerPrefs.SetInt("saeu-isa-best-first", st.firstCorrect); PlayerPrefs.Save(); }
                }
                catch { }
                MgfSfx.Play("win", .58f);
                for (int c = 0; c < 4; c++) EmitNacre(graphRoot.TransformPoint(new Vector3(-3.45f + c * 2.3f, 2.0f, -1.1f)));
            }
            else MgfSfx.Play("lose", .52f);
            endStats.text = reason; MgfBridge.NotifyChanged();
        }

        void CompleteBand()
        {
            mode = Mode.Interlude; interludeLeft = 1.35f; actionLock = .5f;
            if (band == 1) feedbackText = "첫 기록패 완성 · 다음은 계급의 크기 5 mm";
            else if (band == 2) feedbackText = "두 기록패 완성 · 이제 잘못 든 자료를 수정";
            else
            {
                bool mastery = st.firstCorrect >= ShrimpRules.MasteryTarget && st.repairFirstCorrect >= ShrimpRules.RepairTarget;
                End(mastery, mastery ? ShrimpRules.Goal + "개 정리 · 첫 시도 기준 달성" : "정리는 끝났지만 첫 시도 기준이 부족합니다");
                return;
            }
            formula.text = band == 1 ? "계급의 크기 10 mm → 5 mm" : "자료 이동: 출발 도수 −1 · 도착 도수 +1";
            formulaPlate.gameObject.SetActive(true); formula.gameObject.SetActive(true);
            RefreshUi(true);
        }

        void HandleFeedbackFinished()
        {
            formulaPlate.gameObject.SetActive(false); formula.gameObject.SetActive(false);
            if (st.onboarding && st.level == 0)
            {
                if (samples[0].problem != null && samples[0].problem.sealedSample) { runSerial++; BeginBand(1); return; }
                mode = Mode.Practice; feedbackText = "손잡이에서 20 이상 30 미만 칸까지 다시 끌어 보시오"; idleGuide = 0; RefreshUi(true); return;
            }
            if (st.onboarding && st.level == 3)
            {
                if (samples[0].problem != null && samples[0].problem.sealedSample) { st.onboarding = false; BeginBand(3); return; }
                mode = Mode.RepairPractice; feedbackText = "41 mm 표본을 40 이상 50 미만 칸으로 옮기시오"; idleGuide = 0; RefreshUi(true); return;
            }
            mode = Mode.Playing;
            if (bandSolved >= 8) CompleteBand();
            else if (band < 3 && CountWaiting() == 0 && spawned < 8) { nextSpawn = 0; SpawnOne(); }
            RefreshUi(true);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            UpdateLayout();
            UpdateAmbient(dt);
            UpdateFx(dt);
            if (actionLock > 0) actionLock -= dt;

            if (mode == Mode.Feedback)
            {
                feedbackLeft -= dt;
                if (feedbackLeft <= 0) HandleFeedbackFinished();
            }
            else if (mode == Mode.Interlude)
            {
                interludeLeft -= dt;
                if (interludeLeft <= 0)
                {
                    formulaPlate.gameObject.SetActive(false); formula.gameObject.SetActive(false);
                    if (band == 1) BeginBand(2); else BeginRepairPractice();
                }
            }

            bool timersRun = mode == Mode.Playing && band >= 1;
            if (timersRun)
            {
                timeLeft -= dt;
                if (timeLeft <= 0) { End(false, "95초가 지났습니다"); return; }
                if (band < 3)
                {
                    nextSpawn -= dt;
                    for (int i = 0; i < samples.Length; i++)
                    {
                        var v = samples[i];
                        if (!v.visible || v.problem == null || v.problem.placedClass >= 0 || v.problem.sealedSample) continue;
                        v.problem.waitingAge += dt;
                        float remain = 1f - v.problem.waitingAge / ShrimpRules.WaitingLimit;
                        v.ageBar.localScale = new Vector3(1.16f * Mathf.Max(.02f, remain), .07f, .05f);
                        if (v.problem.waitingAge >= ShrimpRules.WaitingLimit) { End(false, v.problem.valueMm + " mm 표본의 대기 시간이 끝났습니다"); return; }
                    }
                    if (nextSpawn <= 0 && spawned < 8)
                    {
                        SpawnOne(); nextSpawn = band == 1 ? 5f : 4f;
                    }
                }
            }

            idleGuide += dt;
            UpdateGuide();
            HandlePointer();
            uiTick -= dt;
            if (uiTick <= 0) { uiTick = .12f; RefreshUi(false); }
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                ShowRipple(MgfPointer.Position);
                if (mode == Mode.Title && actionLock <= 0) { MgfSfx.Play("tap", .45f); BeginPractice(); return; }
                if (mode == Mode.End && actionLock <= 0) { runSerial++; ShowTitle(); return; }
                if (mode == Mode.Interlude || actionLock > 0) { feedbackText = "서랍이 움직이는 중입니다"; RefreshUi(true); MgfBridge.NotifyChanged(); return; }

                int hit = HitSample();
                // 8초 뒤에는 화면 전체에 펼친 손·점선 안내가 고정 연습의 잡기 면이 된다.
                // 실전에는 적용하지 않으며, 실제 pointerdown→move→pointerup은 그대로 요구한다.
                if (hit < 0 && mode == Mode.Practice && idleGuide >= 8f) hit = 0;
                if (hit >= 0)
                {
                    var v = samples[hit];
                    if (v.problem != null && v.problem.sealedSample)
                    {
                        feedbackText = "이미 정리한 자료"; v.refuse = .32f; MgfSfx.Play("tap", .35f); RefreshUi(true); MgfBridge.NotifyChanged(); return;
                    }
                    dragged = hit; v.dragging = true; v.dragStart = v.root.position; v.home = v.root.position;
                    if (v.problem != null) v.homeClass = v.problem.placedClass;
                    v.hitbox.GetComponent<Collider>().enabled = false;
                    v.root.localScale *= 1.08f; MgfSfx.Play("tap", .38f);
                    if (mode != Mode.Practice) idleGuide = 0;
                    dragPointerStart = MgfPointer.Position; dragPointerTravel = 0f;
                    st.problemId = v.problem != null ? v.problem.id : ""; MgfBridge.NotifyChanged();
                }
                else
                {
                    feedbackText = "빛나는 자개 손잡이가 달린 주머니를 잡으시오"; MgfSfx.Play("tap", .28f); RefreshUi(true); MgfBridge.NotifyChanged();
                }
            }

            if (dragged >= 0 && MgfPointer.Held)
            {
                var v = samples[dragged];
                dragPointerTravel = Mathf.Max(dragPointerTravel, Vector2.Distance(dragPointerStart, MgfPointer.Position));
                if (PointerWorld(out var p))
                {
                    v.root.position = p; v.root.localScale = Vector3.one * 1.08f;
                    AddTrail(p + Vector3.forward * .04f);
                }
            }

            if (dragged >= 0 && MgfPointer.Up)
            {
                int idx = dragged; dragged = -1; var v = samples[idx]; v.dragging = false;
                v.hitbox.GetComponent<Collider>().enabled = true;
                int c = ClassAt(v.root.position);
                if (mode == Mode.Practice && dragPointerTravel < 28f) c = -1;
                // 고정 연습은 정답 칸을 유령 주머니와 식으로 이미 공개한다. 주머니를 실제로
                // 28px 이상 끈 제스처에는 강한 자석을 적용해 공개된 칸으로 마무리한다.
                // 탭만으로는 통과하지 않고, 실전에는 이 흡착을 적용하지 않는다.
                if (mode == Mode.Practice && dragPointerTravel >= 28f) c = v.problem.correctClass;
                if (c < 0)
                {
                    v.root.position = v.home; v.root.localScale = v.homeClass >= 0 ? Vector3.one * .60f : Vector3.one;
                    v.refuse = .30f; feedbackText = "그래프 면에 놓으시오"; MgfSfx.Play("tap", .28f); RefreshUi(true); MgfBridge.NotifyChanged();
                }
                else Submit(idx, c);
            }
        }

        int HitSample()
        {
            var hits = Physics.RaycastAll(cam.ScreenPointToRay(MgfPointer.Position), 100f);
            for (int h = 0; h < hits.Length; h++)
                for (int i = 0; i < samples.Length; i++)
                    if (samples[i].visible && hits[h].collider.gameObject == samples[i].hitbox) return i;
            return -1;
        }

        bool PointerWorld(out Vector3 p)
        {
            var ray = cam.ScreenPointToRay(MgfPointer.Position);
            var plane = new Plane(Vector3.forward, new Vector3(0, 0, -1.35f));
            if (plane.Raycast(ray, out float d)) { p = ray.GetPoint(d); return true; }
            p = Vector3.zero; return false;
        }

        int ClassAt(Vector3 world)
        {
            Vector3 p = graphRoot.InverseTransformPoint(world);
            if (p.x < -4.6f || p.x >= 4.6f || p.y < -2.65f || p.y > 2.9f) return -1;
            return Mathf.Clamp(Mathf.FloorToInt((p.x + 4.6f) / 2.3f), 0, 3);
        }

        void ShowRipple(Vector2 screen)
        {
            var ray = cam.ScreenPointToRay(screen); var plane = new Plane(Vector3.forward, new Vector3(0, 0, -1.6f));
            if (plane.Raycast(ray, out float d)) invalidRipple.position = ray.GetPoint(d);
            invalidRipple.localScale = new Vector3(.18f, .025f, .18f); invalidRipple.gameObject.SetActive(true); rippleAge = 0;
        }

        void AddTrail(Vector3 p)
        {
            int i = trailCursor++ % trail.Length; trail[i].position = p; trail[i].localScale = new Vector3(.11f, .11f, .04f);
            trail[i].gameObject.SetActive(true); trailAge[i] = 0;
        }

        void EmitNacre(Vector3 p)
        {
            for (int i = 0; i < nacreChips.Length; i++)
            {
                float a = i * Mathf.PI * 2f / nacreChips.Length;
                nacreChips[i].position = p; nacreChips[i].localScale = Vector3.one;
                chipVelocity[i] = new Vector3(Mathf.Cos(a) * (1.1f + i % 3 * .2f), Mathf.Sin(a) * .7f + 1.0f, -.15f);
                chipAge[i] = 0; nacreChips[i].gameObject.SetActive(true);
            }
        }

        void UpdateFx(float dt)
        {
            rippleAge += dt;
            if (invalidRipple.gameObject.activeSelf)
            {
                float k = rippleAge / .38f; invalidRipple.localScale = new Vector3(.18f + k * .75f, .025f, .18f + k * .75f);
                if (k >= 1) invalidRipple.gameObject.SetActive(false);
            }
            for (int i = 0; i < trail.Length; i++)
            {
                if (!trail[i].gameObject.activeSelf) continue;
                trailAge[i] += dt; float k = trailAge[i] / .34f;
                trail[i].localScale = Vector3.one * Mathf.Max(.01f, .13f * (1f - k));
                if (k >= 1) trail[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < nacreChips.Length; i++)
            {
                if (!nacreChips[i].gameObject.activeSelf) continue;
                chipAge[i] += dt; chipVelocity[i] += Vector3.down * 3.8f * dt; nacreChips[i].position += chipVelocity[i] * dt;
                nacreChips[i].rotation *= Quaternion.Euler(0, 0, 260f * dt);
                float k = chipAge[i] / .62f; nacreChips[i].localScale = Vector3.one * Mathf.Max(.01f, 1f - k);
                if (k >= 1) nacreChips[i].gameObject.SetActive(false);
            }
            if (binPulse > 0 && pulseBin >= 0)
            {
                binPulse -= dt; float k = 1f - binPulse / .65f;
                float lift = 1f + Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * .18f;
                histogramCaps[pulseBin].localScale = new Vector3(lift, lift, 1f);
                // 정답 사건은 셀이 앞으로 잠기는 대신 직조 기둥의 위쪽 매듭이 한 번 들리고
                // 실제 새우 주머니가 세로로 쌓이는 사건이다.
                binReliefRenderer[pulseBin].sharedMaterial = pulseBin % 2 == 0 ? dawnMat : wovenTealMat;
                if (binPulse <= 0) histogramCaps[pulseBin].localScale = Vector3.one;
            }
        }

        void UpdateAmbient(float dt)
        {
            float t = Time.unscaledTime;
            for (int i = 0; i < titleShrimp.Length; i++)
                if (titleShrimp[i].gameObject.activeSelf)
                {
                    titleShrimp[i].localPosition = titleShrimpHome[i] + Vector3.up * (Mathf.Sin(t * 1.6f + i * .8f) * .12f);
                    titleShrimp[i].localRotation = Quaternion.Euler(0, Mathf.Sin(t * .7f + i) * 7f, Mathf.Sin(t * 1.1f + i) * 5f);
                }
            for (int i = 0; i < samples.Length; i++)
            {
                var v = samples[i]; if (!v.visible) continue;
                if (!v.dragging)
                {
                    v.creature.localPosition = new Vector3(.02f, .20f + Mathf.Sin(t * 2.1f + i) * .025f, -.30f);
                    v.tail.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2.7f + i) * 6f);
                    v.handle.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2.0f + i) * 4f);
                }
                if (v.pulse > 0)
                {
                    v.pulse -= dt; float k = 1f - v.pulse / .72f;
                    v.creature.localScale = new Vector3(1f + Mathf.Sin(k * Mathf.PI) * .30f, 1f - Mathf.Sin(k * Mathf.PI) * .12f, 1f);
                    if (v.pulse <= 0) v.creature.localScale = Vector3.one;
                }
                if (v.refuse > 0)
                {
                    v.refuse -= dt; float x = Mathf.Sin((.48f - v.refuse) * 48f) * .10f * Mathf.Clamp01(v.refuse * 4f);
                    v.root.position = v.home + Vector3.right * x;
                    if (v.refuse <= 0) v.root.position = v.home;
                }
            }
            if (titleSplit > 0 && titleSplit < .36f)
            {
                titleSplit += dt; float k = Mathf.SmoothStep(0, 1, titleSplit / .36f);
                titleTop.localPosition = new Vector3(0, 2.7f + k * 4.3f, -1f);
                titleBottom.localPosition = new Vector3(0, -2.0f - k * 5.4f, -1f);
                if (titleSplit >= .36f)
                {
                    titleTop.gameObject.SetActive(false); titleBottom.gameObject.SetActive(false); titlePlaque.gameObject.SetActive(false);
                    for (int i = 0; i < titleShrimp.Length; i++) titleShrimp[i].gameObject.SetActive(false);
                }
            }
            if (scorePunch > 0) scorePunch -= dt;
            scoreShown = Mathf.MoveTowards(scoreShown, st.score, Mathf.Max(60f, Mathf.Abs(st.score - scoreShown) * 8f) * dt);
            if (mode == Mode.End) endScoreShown = Mathf.MoveTowards(endScoreShown, st.score, Mathf.Max(100f, st.score * 2.5f) * dt);
        }

        void UpdateGuide()
        {
            bool guide = (mode == Mode.Practice || mode == Mode.RepairPractice) && samples[0].visible && !samples[0].problem.sealedSample;
            if (!guide)
            {
                guideHand.gameObject.SetActive(false);
                guideGhostPocket.gameObject.SetActive(false);
                guideArrow.gameObject.SetActive(false);
                for (int i = 0; i < guideDots.Length; i++) guideDots[i].gameObject.SetActive(false);
                return;
            }
            var sample = samples[0];
            Vector3 from = sample.home;
            int c = samples[0].problem.correctClass;
            Vector3 to = graphRoot.TransformPoint(new Vector3(-3.45f + c * 2.3f, -.4f, -1.25f));

            // 8초 뒤 실제 주머니가 경로 절반까지 움직이는 시범. 자동 제출하지 않으며 학생의
            // 실제 pointerdown→move→pointerup이 있어야만 연습이 닫힌다.
            if (!sample.dragging && idleGuide >= 8f)
            {
                float demo = Mathf.SmoothStep(0f, .52f, Mathf.Clamp01((idleGuide - 8f) / 3.2f));
                sample.root.position = Vector3.Lerp(from, to, demo) + Vector3.up * Mathf.Sin(demo * Mathf.PI) * .20f;
                feedbackText = idleGuide < 11.2f ? "주머니가 가는 길을 보시오 · 손잡이를 따라 끌어 보시오" : "이제 주머니를 잡아 점선 끝까지 옮기시오";
            }

            float speed = idleGuide > 8f ? 1.45f : 1.05f;
            float k = Mathf.Repeat(Time.unscaledTime * speed, 1f);
            Vector3 handFrom = sample.root.position + Vector3.up * .66f;
            guideHand.position = idleGuide < 8f ? handFrom : Vector3.Lerp(handFrom, to + Vector3.up * .30f, k * .35f);
            guideHand.localScale = Vector3.one * (idleGuide > 8f ? .66f : .54f);
            guideHand.gameObject.SetActive(true);
            guideGhostPocket.position = to; guideGhostPocket.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * .06f); guideGhostPocket.gameObject.SetActive(true);
            guideArrow.position = Vector3.Lerp(from, to, .78f) + Vector3.up * .24f;
            guideArrow.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg - 90f);
            guideArrow.localScale = Vector3.one * 1.35f; guideArrow.gameObject.SetActive(true);
            for (int i = 0; i < guideDots.Length; i++)
            {
                float d = (i + 1f) / (guideDots.Length + 1f);
                guideDots[i].position = Vector3.Lerp(from, to, d) + Vector3.up * Mathf.Sin(d * Mathf.PI) * .18f;
                float pulse = .11f + .07f * Mathf.Max(0, Mathf.Sin((d - k) * 12f));
                guideDots[i].localScale = Vector3.one * pulse; guideDots[i].gameObject.SetActive(true);
            }
        }

        void UpdateLayout()
        {
            if (lastW == Screen.width && lastH == Screen.height) return;
            lastW = Screen.width; lastH = Screen.height; wide = Screen.width >= 1024 || (Screen.width >= 640 && Screen.width >= Screen.height);
            if (wide)
            {
                cam.orthographicSize = 7.5f;
                graphRoot.localPosition = new Vector3(-4.75f, -1.72f, 0); graphRoot.localScale = Vector3.one * .82f;

                // CanvasScaler는 가로에서 390 논리 폭을 유지한다. 700px 논리 폭을 쓰면 실제로
                // 화면 전체를 덮으므로, 좌측 정보 헤더와 우측 설명 존을 논리 폭 205/96으로 분리한다.
                hud.fontSize = 11f; goal.fontSize = 13f; timer.fontSize = 10.5f; feedback.fontSize = 11.5f; terms.fontSize = 10f; formula.fontSize = 11.5f;
                hud.rectTransform.anchorMin = hud.rectTransform.anchorMax = new Vector2(.28f, .965f);
                hud.rectTransform.anchoredPosition = Vector2.zero; hud.rectTransform.sizeDelta = new Vector2(205, 16);
                goalPlate.rectTransform.anchorMin = goalPlate.rectTransform.anchorMax = new Vector2(.28f, .875f);
                goalPlate.rectTransform.anchoredPosition = Vector2.zero; goalPlate.rectTransform.sizeDelta = new Vector2(210, 27);
                goal.rectTransform.anchorMin = goal.rectTransform.anchorMax = new Vector2(.28f, .875f);
                goal.rectTransform.anchoredPosition = Vector2.zero; goal.rectTransform.sizeDelta = new Vector2(202, 25);
                timer.rectTransform.anchorMin = timer.rectTransform.anchorMax = new Vector2(.28f, .795f);
                timer.rectTransform.anchoredPosition = Vector2.zero; timer.rectTransform.sizeDelta = new Vector2(202, 17);
                feedback.rectTransform.anchorMin = feedback.rectTransform.anchorMax = new Vector2(.28f, .715f);
                feedback.rectTransform.anchoredPosition = Vector2.zero; feedback.rectTransform.sizeDelta = new Vector2(205, 23);

                terms.rectTransform.anchorMin = terms.rectTransform.anchorMax = new Vector2(.845f, .54f);
                terms.rectTransform.anchoredPosition = Vector2.zero; terms.rectTransform.sizeDelta = new Vector2(112, 72);
                terms.textWrappingMode = TextWrappingModes.Normal; terms.alignment = TextAlignmentOptions.Center;

                formulaPlate.rectTransform.anchorMin = formulaPlate.rectTransform.anchorMax = new Vector2(.855f, .19f);
                formulaPlate.rectTransform.anchoredPosition = Vector2.zero; formulaPlate.rectTransform.sizeDelta = new Vector2(100, 38);
                formula.rectTransform.anchorMin = formula.rectTransform.anchorMax = new Vector2(.855f, .19f);
                formula.rectTransform.anchoredPosition = Vector2.zero; formula.rectTransform.sizeDelta = new Vector2(94, 34);
            }
            else
            {
                cam.orthographicSize = 9.4f;
                graphRoot.localPosition = new Vector3(0, -1.45f, 0); graphRoot.localScale = Vector3.one * .82f;
                hud.fontSize = 18f; goal.fontSize = 21f; timer.fontSize = 17f; feedback.fontSize = 22f; terms.fontSize = 15f; formula.fontSize = 23f;
                hud.rectTransform.anchorMin = hud.rectTransform.anchorMax = new Vector2(.5f, 1f);
                hud.rectTransform.anchoredPosition = new Vector2(0, -22f); hud.rectTransform.sizeDelta = new Vector2(380, 40);
                goalPlate.rectTransform.anchorMin = goalPlate.rectTransform.anchorMax = new Vector2(.5f, 1f);
                goalPlate.rectTransform.anchoredPosition = new Vector2(0, -66f); goalPlate.rectTransform.sizeDelta = new Vector2(374, 60);
                goal.rectTransform.anchorMin = goal.rectTransform.anchorMax = new Vector2(.5f, 1f);
                goal.rectTransform.anchoredPosition = new Vector2(0, -66f); goal.rectTransform.sizeDelta = new Vector2(360, 58);
                timer.rectTransform.anchorMin = timer.rectTransform.anchorMax = new Vector2(.5f, 1f);
                timer.rectTransform.anchoredPosition = new Vector2(0, -104f); timer.rectTransform.sizeDelta = new Vector2(360, 40);
                feedback.rectTransform.anchorMin = feedback.rectTransform.anchorMax = new Vector2(.5f, 1f);
                feedback.rectTransform.anchoredPosition = new Vector2(0, -168f); feedback.rectTransform.sizeDelta = new Vector2(370, 88);
                terms.rectTransform.anchorMin = terms.rectTransform.anchorMax = new Vector2(.5f, 0f);
                terms.rectTransform.anchoredPosition = new Vector2(0, 66); terms.rectTransform.sizeDelta = new Vector2(380, 84);
                terms.textWrappingMode = TextWrappingModes.Normal; terms.alignment = TextAlignmentOptions.Center;
                formulaPlate.rectTransform.anchorMin = formulaPlate.rectTransform.anchorMax = new Vector2(.5f, .5f);
                formulaPlate.rectTransform.anchoredPosition = Vector2.zero; formulaPlate.rectTransform.sizeDelta = new Vector2(360, 62);
                formula.rectTransform.anchorMin = formula.rectTransform.anchorMax = new Vector2(.5f, .5f);
                formula.rectTransform.anchoredPosition = Vector2.zero; formula.rectTransform.sizeDelta = new Vector2(350, 58);
            }
            if (mode == Mode.Practice && samples[0].visible && !samples[0].dragging)
            {
                samples[0].home = PracticeSourcePosition();
                samples[0].root.position = samples[0].home;
                samples[0].root.localScale = Vector3.one * 1.38f;
            }
            else if (band < 3) ReflowQueue();
            else LayoutPlaced();
        }

        void RefreshUi(bool force)
        {
            if (!force && !hud.gameObject.activeSelf) return;
            st.waitingCount = CountWaiting();
            int shown = Mathf.RoundToInt(scoreShown);
            hud.text = "점수 " + shown + "   안전등 " + Mathf.Max(0, st.lives) + "/3   첫 시도 " + st.firstCorrect + "/" + ShrimpRules.Goal + " · 통과 " + ShrimpRules.MasteryTarget;
            timer.text = "남은 시간 " + Mathf.Max(0, Mathf.CeilToInt(timeLeft)) + "초   ·   정리 " + st.solved + "/" + ShrimpRules.Goal;
            feedback.text = feedbackText;
            RefreshTargetState();
            if (mode == Mode.End)
                endStats.text = endTitleText + "\n점수 " + Mathf.RoundToInt(endScoreShown) + "\n첫 시도 " + st.firstCorrect + "/" + ShrimpRules.Goal + " · 통과 " + ShrimpRules.MasteryTarget + " · 수정 " + st.repairFirstCorrect + "/" + ShrimpRules.SamplesPerBand + "\n" + st.endReason;
        }

        void RefreshTargetState()
        {
            int i = FirstAnswerable();
            if (i < 0 || samples[i].problem == null)
            {
                st.currentCorrectClass = -1; st.currentOriginClass = -1; st.sourceX = 0; st.sourceY = 0; return;
            }
            var p = samples[i].problem;
            st.problemId = p.id;
            st.currentValueMm = p.valueMm; st.currentLower = p.lower; st.currentWidth = p.width;
            st.currentCorrectClass = p.correctClass; st.currentOriginClass = p.placedClass;
            Vector3 source = cam.WorldToScreenPoint(samples[i].root.position);
            st.sourceX = Mathf.RoundToInt(source.x); st.sourceY = Screen.height - Mathf.RoundToInt(source.y);
            for (int c = 0; c < 4; c++)
            {
                Vector3 target = cam.WorldToScreenPoint(graphRoot.TransformPoint(new Vector3(-3.45f + c * 2.3f, -.25f, -1.25f)));
                st.classX[c] = Mathf.RoundToInt(target.x);
                if (c == 0) st.targetY = Screen.height - Mathf.RoundToInt(target.y);
            }
        }

        void SetTitleUi(bool on)
        {
            titleVeil.gameObject.SetActive(on); titleLogo.gameObject.SetActive(on); titleTag.gameObject.SetActive(on);
            titlePrompt.gameObject.SetActive(on); titleMeta.gameObject.SetActive(on);
        }

        void SetGameplayUi(bool on)
        {
            goalPlate.gameObject.SetActive(on); goal.gameObject.SetActive(on); hud.gameObject.SetActive(on); timer.gameObject.SetActive(on);
            terms.gameObject.SetActive(on); feedback.gameObject.SetActive(on);
            if (!on) { formulaPlate.gameObject.SetActive(false); formula.gameObject.SetActive(false); }
        }

        void SetEndUi(bool on)
        {
            endVeil.gameObject.SetActive(on); endTitle.gameObject.SetActive(on); endStats.gameObject.SetActive(on); endPrompt.gameObject.SetActive(on);
        }

        // ── IMgfGame: 훅도 실제 Submit 경로를 사용한다.
        public void TestStart()
        {
            SetTitleUi(false); SetEndUi(false); SetGameplayUi(true); graphRoot.gameObject.SetActive(true);
            titleTop.gameObject.SetActive(false); titleBottom.gameObject.SetActive(false); titlePlaque.gameObject.SetActive(false);
            for (int i = 0; i < titleShrimp.Length; i++) titleShrimp[i].gameObject.SetActive(false);
            runSerial++;
            BeginBand(1);
        }

        public void TestAnswerCorrect()
        {
            if (mode == Mode.Title || mode == Mode.End) TestStart();
            if (mode == Mode.Feedback) { feedbackLeft = 0; HandleFeedbackFinished(); }
            if (mode != Mode.Playing) return;
            if (band < 3 && CountWaiting() == 0 && spawned < 8) SpawnOne();
            int i = FirstAnswerable(); if (i >= 0) { actionLock = 0; Submit(i, samples[i].problem.correctClass, true); }
        }

        public void TestAnswerWrong()
        {
            if (mode == Mode.Title || mode == Mode.End) TestStart();
            if (mode == Mode.Feedback) { feedbackLeft = 0; HandleFeedbackFinished(); }
            if (mode != Mode.Playing) return;
            if (band < 3 && CountWaiting() == 0 && spawned < 8) SpawnOne();
            int i = FirstAnswerable(); if (i >= 0) { actionLock = 0; Submit(i, (samples[i].problem.correctClass + 1) % 4, true); }
        }

        int FirstAnswerable()
        {
            for (int i = 0; i < samples.Length; i++)
                if (samples[i].visible && samples[i].problem != null && !samples[i].problem.sealedSample) return i;
            return -1;
        }

        public string StateJson() { return JsonUtility.ToJson(st); }
        public string ProblemBankJson() { return MgfJson.Bank(bank); }
    }
}
