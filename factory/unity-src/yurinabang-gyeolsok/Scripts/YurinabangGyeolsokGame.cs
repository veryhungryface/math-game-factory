// 유리나방 결속 — 수직 도자 계전탑의 대응점 결속 게임.
// 씬·UI·입력·피드백은 코드 부트스트랩 하나에서 만들며 HTML 산출물은 건드리지 않는다.
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.YurinabangGyeolsok
{
    public sealed class YurinabangGyeolsokGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int floor;
            public int cracks;
            public int combo;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int attemptIndex;
            public int pointerVersion;
            public int selectedCount;
            public int selectedCandidate = -1;
            public bool onboarding;
            public bool frozen;
            public string problemId = "";
            public string condition = "";
            public string misconceptionId = "";
            public string endReason = "";
            public string lastFeedback = "";
            public int[] laces = { -1, -1, -1 };
            // 실제 pointer 회귀 검증용 화면 좌표(브라우저 좌상단 원점). 정답 정보는 담지 않는다.
            public float[] sourcePx = new float[6];
            public float[] candidatePx = new float[18];
            public float[] ropePx = new float[2];
        }

        enum Phase { Title, Practice, Playing, Reveal, End }

        sealed class PaneView
        {
            public RectTransform root;
            public Image frame;
            public Image glass;
            public Image reflection;
            public Image[] edges = new Image[3];
            public Image[] hinges = new Image[3];
            public RectTransform[] markers = new RectTransform[3];
            public Image[] markerImages = new Image[3];
            public TextMeshProUGUI[] markerLabels = new TextMeshProUGUI[3];
            public TextMeshProUGUI[] sideLabels = new TextMeshProUGUI[3];
            public TextMeshProUGUI[] angleLabels = new TextMeshProUGUI[3];
            public TextMeshProUGUI caption;
            public Image halo;
            public Vector2[] labelPositions = new Vector2[3];
            public float width;
            public float height;
        }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>(384);
        readonly int[] connection = { -1, -1, -1 };
        readonly Image[] laceLines = new Image[3];
        readonly GameObject[] relayShuttles = new GameObject[24];
        readonly Vector3[] shuttleBases = new Vector3[24];
        readonly Renderer[] towerCells = new Renderer[8];

        List<LaceProblem> deck;
        LaceProblem current;
        Phase phase = Phase.Title;
        Camera cam;
        int runSerial;
        int deckIndex;
        int selectedCandidate = -1;
        int dragSource = -1;
        bool dragging;
        bool pullingRope;
        bool firstAttempt;
        bool revealCorrect;
        bool revealPractice;
        bool endAfterReveal;
        float timeLeft;
        float revealClock;
        float rippleClock;
        float toastClock;
        float titlePull;
        float ropePull;
        float scorePunch;
        float endClock;
        float worldClock;
        float layoutAspect = -1f;
        int displayedSecond = -1;
        int displayedScore;
        int displayedEndScore;
        Vector2 pointerDown;
        Vector2 rippleScreen;

        Canvas canvas;
        RectTransform canvasRt;
        RectTransform uiRoot;
        CanvasGroup titleGroup;
        CanvasGroup playGroup;
        CanvasGroup revealGroup;
        CanvasGroup endGroup;
        RawImage titleArt;
        RectTransform titleSign;
        RectTransform titleHandle;
        RectTransform titleRopeLine;
        TextMeshProUGUI titleTagline;
        TextMeshProUGUI titleInstruction;
        TextMeshProUGUI titleBest;
        TextMeshProUGUI goalText;
        TextMeshProUGUI scoreText;
        TextMeshProUGUI floorText;
        TextMeshProUGUI timeText;
        TextMeshProUGUI statusText;
        TextMeshProUGUI toastText;
        TextMeshProUGUI revealTitle;
        TextMeshProUGUI revealFormula;
        TextMeshProUGUI endTitle;
        TextMeshProUGUI endScore;
        TextMeshProUGUI endStats;
        TextMeshProUGUI endTap;
        Image[] bulbImages = new Image[3];
        Image[] floorMarks = new Image[8];
        Image[] crackLines = new Image[3];
        RectTransform goalPlateRt;
        RectTransform statusPlateRt;
        RectTransform statsPlateRt;
        RectTransform progressRt;
        RectTransform laceRoot;
        Image dragLine;
        RectTransform guideDot;
        Image guideLine;
        RectTransform ropeHandle;
        RectTransform ropeLine;
        Image ropeGlow;
        RectTransform rippleRt;
        Image rippleImage;
        Image frostRise;
        RectTransform roofLatchEvent;
        RectTransform evidencePanel;
        Image roofGlow;
        Image[] roofPanels = new Image[3];
        Image[] ceramicLatches = new Image[3];
        RectTransform routeKey;
        Image routeKeyCore;
        Image routeKeyFinL;
        Image routeKeyFinR;
        PaneView sourcePane;
        PaneView[] candidatePanes = new PaneView[3];

        Sprite roundedSprite;
        Sprite ringSprite;
        Sprite triangleSprite;
        Sprite wingSprite;
        Texture2D titleTexture;
        Material bedClosed;
        Material bedOpenLeaf;
        Material bedOpenFlower;
        Material shuttleBody;

        static readonly Color Night = MgfLook.Hex("#152237");
        static readonly Color Iron = MgfLook.Hex("#202A36");
        static readonly Color Cyan = MgfLook.Hex("#43B9B0");
        static readonly Color Vermilion = MgfLook.Hex("#D85C4A");
        static readonly Color Brass = MgfLook.Hex("#D79A51");
        static readonly Color Ivory = MgfLook.Hex("#FFF4DD");
        static readonly Color Frost = MgfLook.Hex("#B9D8E8");
        static readonly Color Cobalt = MgfLook.Hex("#315A8D");
        static readonly Color Copper = MgfLook.Hex("#9B5A3C");
        static readonly Color Porcelain = MgfLook.Hex("#E6D8BE");

        void Awake()
        {
            MgfLook.Quality(32f);
            MgfText.Prewarm("유리나방결속대응점을자석레일로연결하시오왼쪽위쪽오른쪽계전판층균열전구초SSSAA닮음끼인각대응변의비화면위치가아닌변과각으로찾다시시작최고기록연습실전완료도자계전탑결속핀상감문양시동1234567890ABCDEFGHcm°↔∽△:=·,() ");
            Application.targetFrameRate = -1;
            titleTexture = Resources.Load<Texture2D>("YurinabangGyeolsok/title");
            BuildMaterialsAndWorld();
            BuildUi();
            var generated = YurinabangRules.BuildBank();
            bank.AddRange(generated);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void BuildMaterialsAndWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#101A2B"), MgfLook.Hex("#38506B"), MgfLook.Hex("#0C1422"), .48f);
            // 식물·직물·생물 무대가 아닌 수직 계전탑 내부. 불투명 도자 셀과 구리 버스바,
            // 에나멜 자석 셔틀이 쿼터뷰 깊이를 만들고 수학 도형은 평면 UI로 보존한다.
            cam = MgfLook.Camera(new Vector3(9.8f, 6.8f, -18.5f), new Vector3(.7f, -.3f, 2.8f), 34f);
            cam.clearFlags = CameraClearFlags.Skybox;
            MgfLook.Sun(new Vector3(44f, -28f, 12f), MgfLook.Hex("#FFE3B4"), 1.18f, .66f);
            bedClosed = MgfLook.Lit(MgfLook.Hex("#26374D"), .34f, .12f, MgfLook.Hex("#101827"));
            bedOpenLeaf = MgfLook.Lit(Cobalt, .46f, .18f, MgfLook.Hex("#102C55"));
            bedOpenFlower = MgfLook.Lit(Vermilion, .42f, .12f, MgfLook.Hex("#4B1514"));
            shuttleBody = MgfLook.Lit(MgfLook.Hex("#293341"), .5f, .22f);
            var gunmetal = MgfLook.Lit(Iron, .44f, .28f);
            var copper = MgfLook.Lit(Copper, .4f, .26f);
            var ceramic = MgfLook.Lit(Porcelain, .3f, .03f);
            var deck = MgfLook.Lit(MgfLook.Hex("#182438"), .24f, .08f);

            var enamelCoral = MgfLook.Lit(Vermilion, .5f, .08f);
            var enamelBlue = MgfLook.Lit(Cobalt, .5f, .08f, MgfLook.Hex("#10213B"));
            var floor = MgfLook.Block("RelayTowerDeck", new Vector3(.8f, -5.15f, 2.8f), new Vector3(19f, .42f, 9.2f), .12f, deck);
            floor.transform.rotation = Quaternion.Euler(0f, -5f, 0f);
            // 수직 기둥, 적층 도자 셀, 구리 버스바가 화면 전체를 하나의 계전 장치로 묶는다.
            for (int i = 0; i < 7; i++)
            {
                float x = -5.7f + i * 1.9f;
                var rib = MgfLook.Block("TowerPylon" + i, new Vector3(x, .55f, 3.2f + i * .16f), new Vector3(.28f, 12.5f, .42f), .05f, gunmetal);
                rib.transform.rotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? -2.5f : 2.5f);
            }
            for (int i = 0; i < 8; i++)
            {
                var rail = MgfLook.Block("CopperBusbar" + i, new Vector3(3.8f, -4.65f + i * 1.35f, 2.3f + i * .12f), new Vector3(14.5f, .16f, .30f), .04f, copper);
                rail.transform.rotation = Quaternion.Euler(0f, -5f, 0f);
            }
            for (int i = 0; i < 5; i++)
            {
                var beam = MgfLook.Block("CalibrationBridge" + i, new Vector3(-5.8f + i * 1.32f, -3.95f, 1.2f + i * .55f), new Vector3(1.02f, .18f, 5.8f), .04f, ceramic);
                beam.transform.rotation = Quaternion.Euler(0f, -18f, 0f);
                var insulator = MgfLook.Block("EnamelInsulator" + i, new Vector3(-7.1f + i * 1.2f, -3.55f, .4f + i * .38f), new Vector3(.62f, .55f, .62f), .08f, i % 2 == 0 ? enamelCoral : enamelBlue);
                insulator.transform.rotation = Quaternion.Euler(0f, i * 19f, 0f);
                for (int j = 0; j < 3; j++)
                {
                    var fin = MgfLook.Block("SignalFin" + i + "_" + j, new Vector3(-7.25f + i * 1.2f + j * .16f, -3.05f + j * .14f, .35f + i * .38f), new Vector3(.16f, .70f, .36f), .03f, j == 1 ? copper : gunmetal);
                    fin.transform.rotation = Quaternion.Euler(8f, i * 27f + j * 35f, -24f + j * 20f);
                }
            }
            for (int i = 0; i < 8; i++)
            {
                var pane = MgfLook.Block("PorcelainRelayCell" + (i + 1), new Vector3(4.1f, -4.08f + i * 1.35f, 2.56f + i * .12f), new Vector3(13.6f, .92f, .18f), .05f, bedClosed);
                pane.transform.rotation = Quaternion.Euler(0f, -5f, 0f);
                towerCells[i] = pane.GetComponent<Renderer>();
            }
            MgfLook.Block("TowerSpine", new Vector3(10.4f, 0, 3.6f), new Vector3(.9f, 13.8f, 1.25f), .12f, gunmetal);
            var crane = MgfLook.Block("InterlockPost", new Vector3(-3.4f, 3.5f, 2.2f), new Vector3(.24f, 7.6f, .24f), .04f, copper);
            crane.transform.rotation = Quaternion.Euler(0f, 0f, 18f);
            MgfLook.Block("InterlockBeam", new Vector3(-1.2f, 6.2f, 2.4f), new Vector3(5.2f, .24f, .24f), .04f, copper).transform.rotation = Quaternion.Euler(0f, 0f, -8f);

            var cyanLight = new GameObject("CyanGlassLight").AddComponent<Light>();
            cyanLight.type = LightType.Point; cyanLight.range = 10f; cyanLight.intensity = 1.5f;
            cyanLight.color = MgfLook.Hex("#6CD6D0"); cyanLight.transform.position = new Vector3(-5f, 2.8f, -1f); cyanLight.shadows = LightShadows.None;
            var redLight = new GameObject("VermilionGlassLight").AddComponent<Light>();
            redLight.type = LightType.Point; redLight.range = 9f; redLight.intensity = 1.25f;
            redLight.color = MgfLook.Hex("#D98B62"); redLight.transform.position = new Vector3(5.3f, -1.4f, -.8f); redLight.shadows = LightShadows.None;

            var finBlue = MgfLook.Lit(Cobalt, .52f, .12f);
            var finCoral = MgfLook.Lit(Vermilion, .52f, .12f);

            for (int i = 0; i < relayShuttles.Length; i++)
            {
                var root = new GameObject("MagneticShuttle" + i);
                float x = -7.2f + (i % 4) * .72f;
                float y = -2.8f + (i % 6) * .62f;
                float z = .8f + (i % 3) * .18f;
                shuttleBases[i] = new Vector3(x, y, z);
                root.transform.position = shuttleBases[i];
                var body = MgfLook.Block("ShuttleCore", Vector3.zero, new Vector3(.22f, .34f, .18f), .035f, shuttleBody, root.transform);
                Destroy(body.GetComponent<BoxCollider>());
                body.transform.localRotation = Quaternion.Euler(18f, 0, 0);
                var finL = MgfLook.Prim(PrimitiveType.Cube, "EnamelFinL", new Vector3(-.24f, .08f, .03f), new Vector3(.40f, .05f, .36f), finBlue, root.transform, false);
                finL.transform.localRotation = Quaternion.Euler(0, 18f, 18f);
                var finR = MgfLook.Prim(PrimitiveType.Cube, "EnamelFinR", new Vector3(.24f, .08f, .03f), new Vector3(.40f, .05f, .36f), finCoral, root.transform, false);
                finR.transform.localRotation = Quaternion.Euler(0, -18f, -18f);
                relayShuttles[i] = root;
                root.SetActive(i < 10);
            }
        }

        void BuildUi()
        {
            roundedSprite = MakeRoundedSprite(64, 14, false);
            ringSprite = MakeRoundedSprite(64, 18, true);
            triangleSprite = MakeTriangleSprite(128);
            wingSprite = MakeWingSprite(64);
            canvas = MgfText.Canvas;
            canvasRt = (RectTransform)canvas.transform;
            uiRoot = Stretch("YurinabangUi", canvas.transform);
            titleGroup = Group("Title", uiRoot);
            playGroup = Group("Play", uiRoot);
            revealGroup = Group("Reveal", uiRoot);
            endGroup = Group("End", uiRoot);

            BuildTitleUi();
            BuildPlayUi();
            BuildRevealUi();
            BuildEndUi();
        }

        void BuildTitleUi()
        {
            var bgRt = Stretch("TitleArt", titleGroup.transform);
            titleArt = bgRt.gameObject.AddComponent<RawImage>();
            // 플레이와 같은 실시간 2.5D 계전탑을 그대로 보여 준다.
            titleArt.texture = null;
            titleArt.color = new Color(.05f, .10f, .18f, .16f);
            titleArt.raycastTarget = false;
            var vignette = AddImage(titleGroup.transform, "RelayPressureDial", new Vector2(.72f, .69f), Vector2.zero, new Vector2(510, 510), new Color(Cyan.r, Cyan.g, Cyan.b, .10f), ringSprite);
            vignette.rectTransform.localScale = new Vector3(1.2f, 1.2f, 1);

            var sign = AddImage(titleGroup.transform, "PorcelainRelaySign", new Vector2(.69f, .76f), Vector2.zero, new Vector2(318, 184), new Color(Porcelain.r, Porcelain.g, Porcelain.b, .98f), triangleSprite);
            titleSign = sign.rectTransform;
            titleSign.localRotation = Quaternion.Euler(0, 0, -1.5f);
            AddImage(sign.transform, "CobaltInlay", new Vector2(.5f, .48f), Vector2.zero, new Vector2(304, 174), new Color(Cobalt.r, Cobalt.g, Cobalt.b, .30f), triangleSprite);
            var shadow = AddText(sign.transform, "유리나방\n결속", new Vector2(.5f, .5f), new Vector2(3, -3), 47, Iron, 330);
            shadow.rectTransform.sizeDelta = new Vector2(330, 150);
            var logo = AddText(sign.transform, "유리나방\n<color=#315A8D>결속</color>", new Vector2(.5f, .5f), Vector2.zero, 47, Iron, 330);
            logo.rectTransform.sizeDelta = new Vector2(330, 150); logo.outlineWidth = .16f; logo.outlineColor = new Color32(10, 16, 32, 255);
            // 세로 화면에서 첫 프레임부터 안전영역 안에 있도록 기본값은 중앙에 둔다.
            titleTagline = AddText(titleGroup.transform, "대응점을 자석 레일로 잇고 계전탑을 깨워라", new Vector2(.5f, .61f), Vector2.zero, 18, Ivory, 350);

            titleRopeLine = AddImage(titleGroup.transform, "TitleCopperActuator", new Vector2(.28f, .62f), Vector2.zero, new Vector2(12, 250), new Color(Brass.r, Brass.g, Brass.b, .96f), roundedSprite).rectTransform;
            titleRopeLine.pivot = new Vector2(.5f, 1f);
            titleHandle = AddImage(titleGroup.transform, "TitleRelayLever", new Vector2(.28f, .36f), Vector2.zero, new Vector2(124, 72), Vermilion, roundedSprite).rectTransform;
            AddImage(titleHandle, "HandleInset", new Vector2(.5f, .5f), Vector2.zero, new Vector2(92, 50), Iron, roundedSprite);
            AddText(titleHandle, "결속 시동", new Vector2(.5f, .5f), Vector2.zero, 16, Ivory, 108);
            titleInstruction = AddText(titleGroup.transform, "에나멜 레버를 아래로 당기시오", new Vector2(.28f, .22f), Vector2.zero, 17, Ivory, 300);
            titleBest = AddText(titleGroup.transform, "중2 · 도형의 닮음  |  최고 기록 0", new Vector2(.70f, .12f), Vector2.zero, 15, Brass, 350);
        }

        void BuildPlayUi()
        {
            var tint = AddImage(playGroup.transform, "RelayTowerTint", new Vector2(.5f, .5f), Vector2.zero, new Vector2(1600, 1400), new Color(.03f, .08f, .14f, .10f), roundedSprite);
            tint.rectTransform.anchorMin = Vector2.zero; tint.rectTransform.anchorMax = Vector2.one; tint.rectTransform.sizeDelta = Vector2.zero;
            for (int i = 0; i < 5; i++)
            {
                var rib = AddImage(playGroup.transform, "CopperUiBusbar" + i, new Vector2(.12f + i * .19f, .5f), Vector2.zero, new Vector2(4, 1100), new Color(Copper.r, Copper.g, Copper.b, .46f), roundedSprite);
                rib.rectTransform.localRotation = Quaternion.Euler(0, 0, (i - 2) * 1.5f);
            }

            var goalPlate = AddImage(playGroup.transform, "GoalEnamelPlate", new Vector2(.5f, .962f), Vector2.zero, new Vector2(372, 46), new Color(.05f, .14f, .24f, .98f), roundedSprite);
            goalPlateRt = goalPlate.rectTransform;
            goalText = AddText(goalPlate.transform, "닮은 판의 세 대응점을 직접 결속하시오", new Vector2(.5f, .5f), Vector2.zero, 16, Color.white, 356);
            goalText.enableAutoSizing = true; goalText.fontSizeMin = 13f; goalText.fontSizeMax = 16f;

            var statsPlate = AddImage(playGroup.transform, "RelayGauge", new Vector2(.5f, .895f), Vector2.zero, new Vector2(370, 48), new Color(.06f, .09f, .14f, .96f), roundedSprite);
            statsPlateRt = statsPlate.rectTransform;
            scoreText = AddText(statsPlate.transform, "0", new Vector2(.10f, .5f), Vector2.zero, 22, Cyan, 72);
            floorText = AddText(statsPlate.transform, "1층", new Vector2(.28f, .5f), Vector2.zero, 16, Ivory, 62);
            timeText = AddText(statsPlate.transform, "105초", new Vector2(.48f, .5f), Vector2.zero, 16, Brass, 70);
            for (int i = 0; i < 3; i++)
                bulbImages[i] = AddImage(statsPlate.transform, "MoonBulb" + i, new Vector2(.68f + i * .09f, .5f), Vector2.zero, new Vector2(24, 24), Brass, ringSprite);

            var progress = AddImage(playGroup.transform, "TowerCellRail", new Vector2(.5f, .835f), Vector2.zero, new Vector2(330, 18), new Color(.04f, .07f, .12f, .94f), roundedSprite);
            progressRt = progress.rectTransform;
            for (int i = 0; i < 8; i++) floorMarks[i] = AddImage(progress.transform, "FloorMark" + i, new Vector2(.08f + i * .12f, .5f), Vector2.zero, new Vector2(26, 8), new Color(.25f, .3f, .4f, 1f), roundedSprite);

            sourcePane = CreatePane("SourcePane", playGroup.transform, true);
            for (int i = 0; i < 3; i++) candidatePanes[i] = CreatePane("CandidatePane" + i, playGroup.transform, false);

            // 정답 사건은 세 결속핀이 차례로 맞물리고 불투명 도자판에 상감 문양이 각인되는 기계 사건이다.
            roofLatchEvent = R("TripleInterlockEvent", playGroup.transform, new Vector2(.5f, .64f), Vector2.zero, new Vector2(340, 188));
            roofGlow = AddImage(roofLatchEvent, "InlaySeal", new Vector2(.5f, .56f), Vector2.zero, new Vector2(310, 126), new Color(Cyan.r, Cyan.g, Cyan.b, 0f), triangleSprite);
            AddImage(roofLatchEvent, "InterlockHeader", new Vector2(.5f, .88f), Vector2.zero, new Vector2(330, 18), new Color(Copper.r, Copper.g, Copper.b, .98f), roundedSprite);
            for (int i = 0; i < roofPanels.Length; i++)
            {
                roofPanels[i] = AddImage(roofLatchEvent, "PorcelainLockPlate" + i, new Vector2(.22f + i * .28f, .56f), Vector2.zero, new Vector2(92, 104), i % 2 == 0 ? new Color(Cobalt.r, Cobalt.g, Cobalt.b, .98f) : new Color(Porcelain.r, Porcelain.g, Porcelain.b, .98f), roundedSprite);
                AddImage(roofPanels[i].transform, "HardEnamelInlay", new Vector2(.5f, .5f), Vector2.zero, new Vector2(68, 78), new Color(Copper.r, Copper.g, Copper.b, .34f), triangleSprite);
                ceramicLatches[i] = AddImage(roofLatchEvent, "MagneticLockPin" + i, new Vector2(.22f + i * .28f, .18f), Vector2.zero, new Vector2(38, 38), i == 1 ? Vermilion : Brass, roundedSprite);
            }
            routeKey = R("InterlockKey", playGroup.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(48, 38));
            routeKeyCore = AddImage(routeKey, "KeyCore", new Vector2(.5f, .5f), Vector2.zero, new Vector2(18, 28), Iron, roundedSprite);
            routeKeyFinL = AddImage(routeKey, "KeyFinL", new Vector2(.24f, .5f), Vector2.zero, new Vector2(25, 12), Cobalt, roundedSprite);
            routeKeyFinR = AddImage(routeKey, "KeyFinR", new Vector2(.76f, .5f), Vector2.zero, new Vector2(25, 12), Vermilion, roundedSprite);
            roofLatchEvent.gameObject.SetActive(false);
            routeKey.gameObject.SetActive(false);

            laceRoot = Stretch("LeadThreadLayer", playGroup.transform);
            for (int i = 0; i < 3; i++) laceLines[i] = AddImage(laceRoot, "MagneticRail" + i, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1, 8), Brass, roundedSprite);
            dragLine = AddImage(laceRoot, "LiveMagneticRail", new Vector2(.5f, .5f), Vector2.zero, new Vector2(1, 7), new Color(Brass.r, Brass.g, Brass.b, .78f), roundedSprite);
            guideLine = AddImage(laceRoot, "GuidePath", new Vector2(.5f, .5f), Vector2.zero, new Vector2(1, 3), new Color(Frost.r, Frost.g, Frost.b, .65f), roundedSprite);
            guideDot = AddImage(laceRoot, "GuideHand", new Vector2(.5f, .5f), Vector2.zero, new Vector2(30, 30), Frost, ringSprite).rectTransform;

            ropeLine = AddImage(playGroup.transform, "SubmitActuator", new Vector2(.91f, .44f), Vector2.zero, new Vector2(11, 180), new Color(Brass.r, Brass.g, Brass.b, .96f), roundedSprite).rectTransform;
            ropeLine.pivot = new Vector2(.5f, 1f);
            ropeHandle = AddImage(playGroup.transform, "SubmitHandle", new Vector2(.91f, .325f), Vector2.zero, new Vector2(62, 58), Brass, roundedSprite).rectTransform;
            ropeGlow = AddImage(ropeHandle, "RopeReadyGlow", new Vector2(.5f, .5f), Vector2.zero, new Vector2(76, 72), new Color(Cyan.r, Cyan.g, Cyan.b, .0f), ringSprite);
            AddText(ropeHandle, "결", new Vector2(.5f, .5f), Vector2.zero, 20, Iron, 50);
            AddText(ropeHandle, "↓ 제출", new Vector2(.5f, -.30f), Vector2.zero, 13, Brass, 80);

            var statusPlate = AddImage(playGroup.transform, "StatusEnamel", new Vector2(.5f, .205f), Vector2.zero, new Vector2(360, 45), new Color(.015f, .025f, .05f, .97f), roundedSprite);
            statusPlateRt = statusPlate.rectTransform;
            statusText = AddText(statusPlate.transform, "A에서 후보 꼭짓점까지 드래그하시오", new Vector2(.5f, .5f), Vector2.zero, 17, Color.white, 344);
            statusText.enableAutoSizing = true; statusText.fontSizeMin = 14f; statusText.fontSizeMax = 17f;

            frostRise = AddImage(playGroup.transform, "RisingFaultBand", new Vector2(.5f, 0f), Vector2.zero, new Vector2(1200, 20), new Color(Vermilion.r, Vermilion.g, Vermilion.b, .10f), roundedSprite);
            frostRise.rectTransform.pivot = new Vector2(.5f, 0f);
            for (int i = 0; i < 3; i++)
            {
                crackLines[i] = AddImage(playGroup.transform, "PersistentCrack" + i, new Vector2(.23f + i * .27f, .43f), Vector2.zero, new Vector2(5, 170), new Color(Frost.r, Frost.g, Frost.b, .74f), roundedSprite);
                crackLines[i].rectTransform.localRotation = Quaternion.Euler(0, 0, -28 + i * 31);
                crackLines[i].gameObject.SetActive(false);
            }

            var toastBg = AddImage(playGroup.transform, "Toast", new Vector2(.5f, .255f), Vector2.zero, new Vector2(354, 64), new Color(.03f, .05f, .09f, .96f), roundedSprite);
            toastText = AddText(toastBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 15, Ivory, 334);
            toastBg.gameObject.SetActive(false);
            rippleRt = AddImage(playGroup.transform, "PointerRipple", new Vector2(0, 0), Vector2.zero, new Vector2(58, 58), Color.clear, ringSprite).rectTransform;
            rippleImage = rippleRt.GetComponent<Image>();
        }

        void BuildRevealUi()
        {
            roofLatchEvent.SetParent(revealGroup.transform, false);
            routeKey.SetParent(revealGroup.transform, false);
            SetAnchor(roofLatchEvent, new Vector2(.5f, .70f));
            evidencePanel = AddImage(revealGroup.transform, "EvidenceEnamel", new Vector2(.5f, .21f), Vector2.zero, new Vector2(350, 224), new Color(.035f, .08f, .14f, .98f), roundedSprite).rectTransform;
            AddImage(evidencePanel, "EvidenceCopperInlay", new Vector2(.5f, .5f), Vector2.zero, new Vector2(318, 194), new Color(Copper.r, Copper.g, Copper.b, .18f), ringSprite);
            revealTitle = AddText(evidencePanel, "세 결속핀이 맞물렸습니다", new Vector2(.5f, .82f), Vector2.zero, 22, Brass, 314);
            revealFormula = AddText(evidencePanel, "", new Vector2(.5f, .39f), Vector2.zero, 18, Ivory, 314);
            revealFormula.rectTransform.sizeDelta = new Vector2(314, 146);
            revealFormula.textWrappingMode = TextWrappingModes.Normal;
            routeKey.SetAsLastSibling();
        }

        void BuildEndUi()
        {
            var panel = AddImage(endGroup.transform, "TowerSealPanel", new Vector2(.5f, .50f), Vector2.zero, new Vector2(378, 510), new Color(.02f, .04f, .08f, .97f), roundedSprite);
            AddImage(panel.transform, "InlayTriangle", new Vector2(.5f, .73f), Vector2.zero, new Vector2(280, 240), new Color(Cobalt.r, Cobalt.g, Cobalt.b, .28f), triangleSprite);
            endTitle = AddText(panel.transform, "계전탑 결속 완료", new Vector2(.5f, .82f), Vector2.zero, 34, Ivory, 346);
            endScore = AddText(panel.transform, "0", new Vector2(.5f, .59f), Vector2.zero, 62, Cyan, 340);
            endStats = AddText(panel.transform, "", new Vector2(.5f, .36f), Vector2.zero, 18, Ivory, 340);
            endStats.rectTransform.sizeDelta = new Vector2(340, 150); endStats.textWrappingMode = TextWrappingModes.Normal;
            endTap = AddText(panel.transform, "레버를 당겨 다시 시작", new Vector2(.5f, .13f), Vector2.zero, 18, Brass, 330);
            var handle = AddImage(panel.transform, "RestartHandle", new Vector2(.5f, .04f), Vector2.zero, new Vector2(92, 50), Brass, roundedSprite);
            AddText(handle.transform, "다시", new Vector2(.5f, .5f), Vector2.zero, 17, Iron, 80);
        }

        PaneView CreatePane(string name, Transform parent, bool source)
        {
            var p = new PaneView();
            p.root = R(name, parent, new Vector2(.5f, .5f), Vector2.zero, new Vector2(160, 150));
            AddImage(p.root, "PaneShadow", new Vector2(.5f, .5f), new Vector2(10, -10), new Vector2(166, 152), new Color(.01f, .02f, .04f, .54f), triangleSprite);
            p.frame = AddImage(p.root, "GunmetalTriangleFrame", new Vector2(.5f, .48f), new Vector2(3, -3), new Vector2(160, 146), new Color(Iron.r, Iron.g, Iron.b, .99f), triangleSprite);
            p.glass = AddImage(p.root, "OpaquePorcelainPlate", new Vector2(.5f, .48f), Vector2.zero, new Vector2(148, 134), source ? new Color(Porcelain.r, Porcelain.g, Porcelain.b, .99f) : new Color(.20f, .35f, .55f, .99f), triangleSprite);
            p.reflection = AddImage(p.root, "HardEnamelInset", new Vector2(.5f, .53f), new Vector2(-10, 2), new Vector2(108, 92), new Color(Copper.r, Copper.g, Copper.b, source ? .22f : .16f), triangleSprite);
            p.halo = AddImage(p.root, "SelectionHalo", new Vector2(.5f, .49f), Vector2.zero, new Vector2(168, 154), Color.clear, ringSprite);
            for (int i = 0; i < 3; i++) p.edges[i] = AddImage(p.root, "CopperEdge" + i, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1, 6), Copper, roundedSprite);
            string[] labels = source ? new[] { "A", "B", "C" } : new[] { "D", "E", "F" };
            for (int i = 0; i < 3; i++)
            {
                p.markers[i] = AddImage(p.root, "Vertex" + labels[i], new Vector2(.5f, .5f), Vector2.zero, new Vector2(50, 50), source ? Cyan : Brass, ringSprite).rectTransform;
                p.markerImages[i] = p.markers[i].GetComponent<Image>();
                p.markerLabels[i] = AddText(p.markers[i], labels[i], new Vector2(.5f, .5f), Vector2.zero, 21, Ivory, 42);
                p.sideLabels[i] = AddText(p.root, "", new Vector2(.5f, .5f), Vector2.zero, 17, Ivory, 94);
                p.angleLabels[i] = AddText(p.root, "", new Vector2(.5f, .5f), Vector2.zero, 16, Brass, 68);
                p.hinges[i] = AddImage(p.root, "EnamelContact" + labels[i], new Vector2(.5f, .5f), Vector2.zero, new Vector2(13, 8), Vermilion, roundedSprite);
            }
            p.caption = AddText(p.root, source ? "기준 △ABC" : "후보판", new Vector2(.5f, 1f), new Vector2(0, 8), 17, source ? Cyan : Brass, 160);
            return p;
        }

        void ShowTitle()
        {
            phase = Phase.Title;
            st.phase = "title";
            st.score = 0; st.solved = 0; st.level = 1; st.floor = 0; st.lives = 3; st.cracks = 0;
            st.combo = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.selectedCount = 0;
            st.selectedCandidate = -1; st.onboarding = false; st.frozen = true; st.problemId = ""; st.endReason = "";
            titlePull = 0; ropePull = 0; displayedScore = 0; displayedEndScore = 0;
            titleBest.text = "중2 · 도형의 닮음  |  최고 기록 " + PlayerPrefs.GetInt("yurinabang.best", 0).ToString("N0");
            SetGroups();
            SetFloorLights(0);
            MgfBridge.NotifyChanged();
        }

        void StartPractice()
        {
            runSerial++;
            phase = Phase.Practice;
            st.phase = "playing";
            st.score = 0; st.solved = 0; st.level = 1; st.floor = 0; st.lives = 3; st.cracks = 0; st.combo = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.attemptIndex = 0; st.misconceptionId = "";
            st.endReason = ""; st.onboarding = true; st.frozen = true;
            current = YurinabangRules.Practice();
            firstAttempt = true;
            ResetConnections();
            RefreshProblem();
            SetGroups();
            MgfSfx.Play("whoosh", .35f);
            MgfBridge.NotifyChanged();
        }

        void StartRun()
        {
            deck = YurinabangRules.RunDeck(runSerial);
            deckIndex = 0;
            timeLeft = YurinabangRules.RunSeconds;
            phase = Phase.Playing;
            st.phase = "playing";
            st.onboarding = false; st.frozen = false; st.floor = 1; st.level = 1;
            current = deck[0];
            firstAttempt = true;
            ResetConnections();
            RefreshProblem();
            SetGroups();
            MgfBridge.NotifyChanged();
        }

        void BeginNextProblem()
        {
            if (deckIndex >= deck.Count)
            {
                Finish(st.firstAttemptCorrect >= 6 ? "clear" : "not_mastered");
                return;
            }
            phase = Phase.Playing;
            st.phase = "playing";
            st.frozen = false;
            current = deck[deckIndex];
            st.floor = deckIndex + 1;
            st.level = deckIndex < 3 ? 1 : deckIndex < 6 ? 2 : 3;
            firstAttempt = true;
            ResetConnections();
            RefreshProblem();
            SetGroups();
            MgfBridge.NotifyChanged();
        }

        void ResetConnections()
        {
            for (int i = 0; i < 3; i++) connection[i] = -1;
            for (int i = 0; i < 3; i++) st.laces[i] = -1;
            selectedCandidate = -1;
            dragSource = -1;
            dragging = false;
            ropePull = 0;
            st.selectedCount = 0;
            st.selectedCandidate = -1;
            for (int i = 0; i < laceLines.Length; i++) laceLines[i].gameObject.SetActive(false);
            dragLine.gameObject.SetActive(false);
            if (roofLatchEvent) roofLatchEvent.gameObject.SetActive(false);
            if (routeKey) routeKey.gameObject.SetActive(false);
            for (int i = 0; i < candidatePanes.Length; i++)
            {
                candidatePanes[i].root.localScale = Vector3.one;
                candidatePanes[i].root.localRotation = Quaternion.identity;
            }
        }

        void RefreshProblem()
        {
            if (current == null) return;
            st.problemId = current.id;
            st.condition = current.condition == LaceCondition.Sss ? "SSS" : current.condition == LaceCondition.Sas ? "SAS" : "AA";
            floorText.text = phase == Phase.Practice ? "연습" : (st.floor + "층");
            goalText.text = phase == Phase.Practice ? "A↔D 확인 · 세 대응점 직접 결속" : "닮은 판 · 세 대응점 직접 결속";
            SetPane(sourcePane, true, -1);
            for (int i = 0; i < 3; i++) SetPane(candidatePanes[i], false, i);
            statusText.text = phase == Phase.Practice ? "A→D를 따라 드래그하고 세 레일을 완성하시오" : "기준 꼭짓점에서 후보 꼭짓점으로 드래그하시오";
            if (Screen.width >= 1700 && Screen.width / Mathf.Max(1f, (float)Screen.height) >= 1.75f)
            {
                Canvas.ForceUpdateCanvases();
                ResolveUltraPaneGap();
            }
            RefreshHud();
        }

        void SetPane(PaneView p, bool source, int candidateIndex)
        {
            int[] layout = source ? current.sourceLabelToVisual : current.candidates[candidateIndex].labelToVisual;
            int[] sides = source ? current.sourceSides : current.candidates[candidateIndex].sides;
            int[] angles = source ? current.sourceAngles : current.candidates[candidateIndex].angles;
            for (int label = 0; label < 3; label++)
            {
                int visual = layout[label];
                p.labelPositions[label] = VisualPoint(visual, p.width, p.height);
                p.markers[label].anchoredPosition = p.labelPositions[label];
                p.hinges[label].rectTransform.anchoredPosition = p.labelPositions[label] + Inward(p.labelPositions[label]) * 10f;
                p.hinges[label].rectTransform.localRotation = Quaternion.Euler(0, 0, visual == 2 ? 0f : visual == 0 ? -32f : 32f);
                p.angleLabels[label].rectTransform.anchoredPosition = p.labelPositions[label] + Inward(p.labelPositions[label]) * 28f;
                p.angleLabels[label].text = angles[label] > 0 && (current.condition == LaceCondition.Aa || (current.condition == LaceCondition.Sas && label == (source ? 0 : FindAngleLabel(candidateIndex)))) ? angles[label] + "°" : "";
            }
            for (int e = 0; e < 3; e++)
            {
                int a = e, b = (e + 1) % 3;
                Vector2 pa = p.labelPositions[a], pb = p.labelPositions[b];
                PlaceLocalLine(p.edges[e], pa, pb, 5f);
                p.sideLabels[e].rectTransform.anchoredPosition = (pa + pb) * .5f + Outward((pa + pb) * .5f) * (p.width < 150f ? 11f : 15f);
                p.sideLabels[e].text = sides[e] > 0 && current.condition != LaceCondition.Aa ? sides[e] + " cm" : "";
            }
            if (source) p.caption.text = "기준 △ABC · " + (current.condition == LaceCondition.Sss ? "SSS" : current.condition == LaceCondition.Sas ? "SAS" : "AA");
            else p.caption.text = new[] { "왼쪽 후보판", "위쪽 후보판", "오른쪽 후보판" }[candidateIndex];
        }

        int FindAngleLabel(int candidateIndex)
        {
            var a = current.candidates[candidateIndex].angles;
            for (int i = 0; i < 3; i++) if (a[i] > 0) return i;
            return 0;
        }

        static Vector2 VisualPoint(int visual, float w, float h)
        {
            if (visual == 0) return new Vector2(-w * .32f, -h * .25f);
            if (visual == 1) return new Vector2(w * .32f, -h * .25f);
            return new Vector2(0, h * .31f);
        }

        static Vector2 Inward(Vector2 p) => (-p).sqrMagnitude < .01f ? Vector2.down : (-p).normalized;
        static Vector2 Outward(Vector2 p) => p.sqrMagnitude < .01f ? Vector2.down : p.normalized;

        void Update()
        {
            worldClock += Time.unscaledDeltaTime;
            MgfLook.FitWidth(cam, 34f, .78f);
            UpdateLayout();
            AnimateWorld();
            HandlePointer();
            AnimateUi();
            if (phase == Phase.Playing)
            {
                timeLeft -= Time.deltaTime;
                if (timeLeft <= 0f) { timeLeft = 0f; Finish("time"); }
            }
            else if (phase == Phase.Reveal)
            {
                revealClock -= Time.deltaTime;
                if (revealClock <= 0f)
                {
                    if (endAfterReveal) Finish(st.endReason);
                    else if (revealPractice) StartRun();
                    else if (revealCorrect) { deckIndex++; BeginNextProblem(); }
                    else { phase = Phase.Playing; st.phase = "playing"; st.frozen = false; ResetConnections(); RefreshProblem(); SetGroups(); MgfBridge.NotifyChanged(); }
                }
            }
            else if (phase == Phase.End) endClock += Time.deltaTime;
            RefreshDynamicHud();
        }

        void LateUpdate()
        {
            if (sourcePane == null || ropeHandle == null) return;
            for (int i = 0; i < 3; i++) StoreBrowserPoint(st.sourcePx, i * 2, sourcePane.markers[i]);
            for (int c = 0; c < 3; c++) for (int i = 0; i < 3; i++)
                StoreBrowserPoint(st.candidatePx, (c * 3 + i) * 2, candidatePanes[c].markers[i]);
            StoreBrowserPoint(st.ropePx, 0, ropeHandle);
        }

        static void StoreBrowserPoint(float[] target, int offset, RectTransform rt)
        {
            Vector2 p = ScreenPoint(rt);
            target[offset] = p.x;
            target[offset + 1] = Screen.height - p.y;
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                pointerDown = MgfPointer.Position;
                rippleScreen = pointerDown;
                rippleClock = .42f;
                st.pointerVersion++;
                MgfBridge.NotifyChanged();

                if (phase == Phase.Title)
                {
                    // 첫플레이 하네스와 접근성 탭도 세계 안의 계전 레버 동작으로 이어진다.
                    MgfSfx.Play("tap", .25f);
                    if (!Near(titleHandle, pointerDown, 92f))
                    {
                        titlePull = 1f;
                        pullingRope = false;
                        StartPractice();
                        return;
                    }
                    pullingRope = true;
                    return;
                }
                if (phase == Phase.End)
                {
                    pullingRope = true;
                    MgfSfx.Play("tap", .25f);
                    return;
                }
                if (phase == Phase.Reveal) return;

                if (Near(ropeHandle, pointerDown, 72f))
                {
                    pullingRope = true;
                    MgfSfx.Play("tap", .24f);
                    return;
                }
                dragSource = FindSource(pointerDown);
                if (dragSource >= 0)
                {
                    if (connection[dragSource] >= 0)
                    {
                        connection[dragSource] = -1;
                        st.laces[dragSource] = -1;
                        if (ConnectionCount() == 0) selectedCandidate = -1;
                    }
                    dragging = true;
                    dragLine.gameObject.SetActive(true);
                    MgfSfx.Play("tap", .22f);
                    return;
                }
                Refuse("기준판의 A·B·C 꼭짓점에서 드래그를 시작하시오.");
            }

            if (MgfPointer.Held)
            {
                float scale = CanvasScale();
                if (phase == Phase.Title && pullingRope)
                {
                    titlePull = Mathf.Clamp((pointerDown.y - MgfPointer.Position.y) / Mathf.Max(1f, 92f * scale), 0f, 1.15f);
                }
                else if (phase == Phase.End && pullingRope)
                {
                    titlePull = Mathf.Clamp((pointerDown.y - MgfPointer.Position.y) / Mathf.Max(1f, 70f * scale), 0f, 1.15f);
                }
                else if (pullingRope)
                {
                    ropePull = Mathf.Clamp((pointerDown.y - MgfPointer.Position.y) / Mathf.Max(1f, 72f * scale), 0f, 1.15f);
                }
            }

            // WebGL/모바일에서 아주 짧은 Up 프레임이 렌더 프레임 사이로 빠질 수 있다.
            // Held가 끝난 다음 프레임에도 마지막 화면 좌표로 같은 드롭 경로를 확정한다.
            if (MgfPointer.Up || (!MgfPointer.Held && !MgfPointer.Down && (dragging || pullingRope)))
            {
                if (phase == Phase.Title && pullingRope)
                {
                    if (titlePull >= .72f || Vector2.Distance(pointerDown, MgfPointer.Position) < 18f * CanvasScale()) StartPractice();
                    else Refuse("조금 더 아래로 당기시오.");
                    pullingRope = false; titlePull = 0;
                    return;
                }
                if (phase == Phase.End && pullingRope)
                {
                    if ((pointerDown.y - MgfPointer.Position.y) > 38f * CanvasScale() || Vector2.Distance(pointerDown, MgfPointer.Position) < 18f * CanvasScale()) StartPractice();
                    pullingRope = false; titlePull = 0;
                    return;
                }
                if (pullingRope)
                {
                    if (ropePull >= .72f) Submit();
                    else Refuse(ConnectionCount() < 3 ? "세 대응점을 모두 결속한 뒤 레버를 당기시오." : "레버를 조금 더 아래로 당기시오.");
                    pullingRope = false; ropePull = 0;
                    return;
                }
                if (dragging)
                {
                    int c, label;
                    if (FindTarget(MgfPointer.Position, out c, out label)) AddConnection(dragSource, c, label);
                    else Refuse("후보판의 D·E·F 접점 위에서 손을 놓으시오.");
                    dragging = false; dragSource = -1; dragLine.gameObject.SetActive(false);
                }
            }
        }

        void AddConnection(int source, int candidate, int targetLabel)
        {
            if (selectedCandidate >= 0 && candidate != selectedCandidate)
            {
                Refuse("세 자석 레일은 같은 후보판 한 장에 이어야 합니다.");
                return;
            }
            for (int i = 0; i < 3; i++)
                if (connection[i] == targetLabel)
                {
                    Refuse("한 꼭짓점에는 자석 레일을 하나만 이을 수 있습니다.");
                    MgfFx.Punch(candidatePanes[candidate].markers[targetLabel], .1f, .22f);
                    return;
                }
            selectedCandidate = candidate;
            connection[source] = targetLabel;
            st.laces[source] = targetLabel;
            st.lastFeedback = "";
            st.selectedCandidate = candidate;
            st.selectedCount = ConnectionCount();
            statusText.text = st.selectedCount < 3 ? st.selectedCount + "/3 결속 · 남은 꼭짓점을 이으시오" : "세 레일 완성 · 오른쪽 레버를 아래로 당기시오";
            MgfFx.Punch(sourcePane.markers[source], .12f, .24f);
            MgfFx.Punch(candidatePanes[candidate].markers[targetLabel], .12f, .24f);
            MgfSfx.Play("pop", .26f);
            MgfBridge.NotifyChanged();
        }

        void Submit()
        {
            if (phase != Phase.Practice && phase != Phase.Playing) return;
            if (ConnectionCount() < 3 || selectedCandidate < 0)
            {
                Refuse("세 대응점을 모두 결속해야 계전기가 작동합니다.");
                return;
            }
            int[] map = { connection[0], connection[1], connection[2] };
            bool correct = YurinabangRules.IsCorrect(current, selectedCandidate, map);
            if (firstAttempt && phase != Phase.Practice)
            {
                st.firstAttemptTotal++;
                if (correct) st.firstAttemptCorrect++;
            }
            st.attemptIndex++;
            firstAttempt = false;
            if (correct) ResolveCorrect();
            else ResolveWrong(map);
        }

        void ResolveCorrect()
        {
            revealCorrect = true;
            revealPractice = phase == Phase.Practice;
            endAfterReveal = false;
            st.misconceptionId = "";
            st.score += phase == Phase.Practice ? 50 : 140 + st.combo * 25 + st.level * 20;
            scorePunch = .42f;
            if (phase != Phase.Practice)
            {
                st.solved++;
                st.combo++;
                if (st.combo >= 3 && st.combo % 3 == 0 && st.lives < 3) st.lives++;
                SetFloorLights(st.solved);
            }
            phase = Phase.Reveal;
            revealClock = revealPractice ? 2.35f : 2.15f;
            st.phase = "playing";
            st.frozen = true;
            revealTitle.text = revealPractice ? "첫 결속핀이 맞물렸습니다" : "세 결속핀이 맞물렸습니다";
            revealTitle.color = Brass;
            revealFormula.text = current.reveal;
            for (int i = 0; i < 3; i++) laceLines[i].color = i == 0 ? Cyan : i == 1 ? Brass : Vermilion;
            MgfSfx.Play("correct", .5f);
            BeginInterlockEvent();
            SetGroups();
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void ResolveWrong(int[] map)
        {
            revealCorrect = false;
            revealPractice = phase == Phase.Practice;
            endAfterReveal = false;
            string id = YurinabangRules.MisconceptionId(current, selectedCandidate, map);
            st.misconceptionId = id;
            st.combo = 0;
            if (!revealPractice)
            {
                st.cracks++;
                st.lives = Mathf.Max(0, st.lives - 1);
            }
            phase = Phase.Reveal;
            revealClock = 2.15f;
            st.frozen = true;
            revealTitle.text = revealPractice ? "비를 다시 맞추시오" : "도자 균열이 남았습니다";
            revealTitle.color = Frost;
            revealFormula.text = YurinabangRules.MisconceptionMessage(id) + "\n" + current.reveal;
            for (int i = 0; i < 3; i++) laceLines[i].color = Frost;
            MgfSfx.Play("wrong", .42f);
            MgfFx.Shake(cam, revealPractice ? .035f : .08f, .26f);
            if (!revealPractice && (st.lives <= 0 || st.cracks >= 3))
            {
                st.phase = "gameover";
                st.endReason = "cracks";
                endAfterReveal = true;
            }
            SetGroups();
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void BeginInterlockEvent()
        {
            roofLatchEvent.gameObject.SetActive(true);
            routeKey.gameObject.SetActive(true);
            roofLatchEvent.localScale = Vector3.one * .94f;
            roofLatchEvent.localRotation = Quaternion.identity;
            roofGlow.color = new Color(1f, .90f, .57f, 0f);
            for (int i = 0; i < roofPanels.Length; i++)
            {
                roofPanels[i].rectTransform.anchoredPosition = Vector2.zero;
                roofPanels[i].rectTransform.localRotation = Quaternion.identity;
                ceramicLatches[i].rectTransform.anchoredPosition = Vector2.zero;
                ceramicLatches[i].rectTransform.localRotation = Quaternion.identity;
            }
            candidatePanes[selectedCandidate].root.localScale = Vector3.one;
        }

        void Finish(string reason)
        {
            phase = Phase.End;
            bool clear = reason == "clear" && st.solved >= 8 && st.firstAttemptCorrect >= 6 && st.cracks < 3;
            st.phase = clear ? "clear" : "gameover";
            st.frozen = true;
            st.endReason = clear ? "clear" : reason;
            endClock = 0;
            displayedEndScore = 0;
            if (st.score > PlayerPrefs.GetInt("yurinabang.best", 0)) { PlayerPrefs.SetInt("yurinabang.best", st.score); PlayerPrefs.Save(); }
            endTitle.text = clear ? "계전탑 결속 완료" : reason == "time" ? "과부하가 꼭대기에 닿았습니다" : reason == "not_mastered" ? "첫 결속을 더 확인하시오" : "계전탑이 정지했습니다";
            endTitle.color = clear ? Cyan : Frost;
            endStats.text = "작동 " + st.solved + "/8층\n첫 결속 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal + " · 균열 " + st.cracks + "/3\n" + (clear ? "△ABC∽△DEF의 대응 순서를 완성했습니다." : "대응변의 비와 끼인각을 먼저 확인하시오.");
            endTap.text = "화면을 누르거나 레버를 당겨 다시 시작";
            MgfSfx.Play(clear ? "win" : "lose", .55f);
            if (clear)
            {
                for (int i = 0; i < relayShuttles.Length; i++)
                {
                    float row = i / 7f;
                    float col = (i % 7) - 3f;
                    shuttleBases[i] = new Vector3(col * (.65f + row * .06f), -2.7f + row * .95f, -.4f + (i % 3) * .12f);
                    relayShuttles[i].transform.localScale = Vector3.one * 1.2f;
                }
            }
            SetGroups();
            MgfBridge.NotifyChanged();
        }

        void Refuse(string message)
        {
            st.lastFeedback = message;
            toastText.text = message;
            toastText.transform.parent.gameObject.SetActive(true);
            toastClock = 1.45f;
            MgfSfx.Play("wrong", .16f);
            if (phase == Phase.Title) titleInstruction.text = message;
            else if (phase == Phase.Practice || phase == Phase.Playing) statusText.text = message;
        }

        void SetGroups()
        {
            SetGroup(titleGroup, phase == Phase.Title);
            SetGroup(playGroup, phase != Phase.Title && phase != Phase.End);
            SetGroup(revealGroup, phase == Phase.Reveal);
            SetGroup(endGroup, phase == Phase.End);
        }

        static void SetGroup(CanvasGroup g, bool on)
        {
            g.alpha = on ? 1f : 0f;
            g.gameObject.SetActive(on);
        }

        void RefreshHud()
        {
            scoreText.text = displayedScore.ToString("N0");
            for (int i = 0; i < bulbImages.Length; i++) bulbImages[i].color = i < st.lives ? Brass : new Color(.2f, .25f, .32f, .8f);
            for (int i = 0; i < floorMarks.Length; i++) floorMarks[i].color = i < st.solved ? (i % 2 == 0 ? Cyan : Vermilion) : new Color(.24f, .29f, .38f, 1f);
            for (int i = 0; i < crackLines.Length; i++) crackLines[i].gameObject.SetActive(i < st.cracks);
        }

        void RefreshDynamicHud()
        {
            int target = st.score;
            if (displayedScore != target)
            {
                displayedScore = Mathf.RoundToInt(Mathf.MoveTowards(displayedScore, target, Mathf.Max(1, Mathf.CeilToInt(Time.deltaTime * 720f))));
                scoreText.text = displayedScore.ToString("N0");
            }
            int sec = Mathf.CeilToInt(timeLeft);
            if (phase == Phase.Playing && sec != displayedSecond)
            {
                displayedSecond = sec;
                timeText.text = sec + "초";
            }
            if (phase == Phase.End && displayedEndScore != st.score)
            {
                displayedEndScore = Mathf.RoundToInt(Mathf.MoveTowards(displayedEndScore, st.score, Mathf.Max(1, Mathf.CeilToInt(Time.deltaTime * 900f))));
                endScore.text = displayedEndScore.ToString("N0");
            }
        }

        void SetFloorLights(int count)
        {
            for (int i = 0; i < towerCells.Length; i++) towerCells[i].sharedMaterial = i < count ? (i % 2 == 0 ? bedOpenLeaf : bedOpenFlower) : bedClosed;
        }

        int ConnectionCount()
        {
            int n = 0;
            for (int i = 0; i < 3; i++) if (connection[i] >= 0) n++;
            return n;
        }

        int FindSource(Vector2 screen)
        {
            float radius = 38f * CanvasScale();
            int best = -1; float bestD = radius * radius;
            for (int i = 0; i < 3; i++)
            {
                float d = (ScreenPoint(sourcePane.markers[i]) - screen).sqrMagnitude;
                if (d < bestD) { best = i; bestD = d; }
            }
            return best;
        }

        bool FindTarget(Vector2 screen, out int candidate, out int label)
        {
            float radius = 41f * CanvasScale();
            float bestD = radius * radius;
            candidate = -1; label = -1;
            for (int c = 0; c < 3; c++) for (int v = 0; v < 3; v++)
            {
                float d = (ScreenPoint(candidatePanes[c].markers[v]) - screen).sqrMagnitude;
                if (d < bestD) { bestD = d; candidate = c; label = v; }
            }
            return candidate >= 0;
        }

        bool Near(RectTransform rt, Vector2 screen, float logicalRadius) => (ScreenPoint(rt) - screen).sqrMagnitude <= Mathf.Pow(logicalRadius * CanvasScale(), 2f);
        float CanvasScale() => canvas ? Mathf.Max(.5f, canvas.scaleFactor) : 1f;
        static Vector2 ScreenPoint(RectTransform rt) => RectTransformUtility.WorldToScreenPoint(null, rt.position);

        void AnimateUi()
        {
            float dt = Time.unscaledDeltaTime;
            if (toastClock > 0f)
            {
                toastClock -= dt;
                if (toastClock <= 0f && toastText) toastText.transform.parent.gameObject.SetActive(false);
            }
            if (rippleClock > 0f)
            {
                rippleClock -= dt;
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, rippleScreen, null, out local);
                rippleRt.anchoredPosition = local;
                float k = 1f - rippleClock / .42f;
                rippleRt.localScale = Vector3.one * Mathf.Lerp(.5f, 1.6f, k);
                rippleImage.color = new Color(Frost.r, Frost.g, Frost.b, (1f - k) * .72f);
            }
            else if (rippleImage) rippleImage.color = Color.clear;

            if (phase == Phase.Title)
            {
                float bob = Mathf.Sin(worldClock * 2.2f) * 4f;
                titleHandle.anchoredPosition = new Vector2(0, -titlePull * 82f + bob);
                titleHandle.localScale = new Vector3(1f + titlePull * .12f, 1f - titlePull * .1f, 1f);
                titleRopeLine.sizeDelta = new Vector2(12, 250 + titlePull * 82f);
                titleSign.localRotation = Quaternion.Euler(0, 0, -1.5f + Mathf.Sin(worldClock * .65f) * .6f);
            }
            if (phase == Phase.Practice || phase == Phase.Playing)
            {
                ropeHandle.anchoredPosition = new Vector2(0, -ropePull * 66f + Mathf.Sin(worldClock * 2.8f) * (ConnectionCount() == 3 ? 3f : 1f));
                ropeHandle.localScale = new Vector3(1f + ropePull * .11f, 1f - ropePull * .09f, 1f);
                ropeLine.sizeDelta = new Vector2(11, 180 + ropePull * 66f);
                ropeGlow.color = new Color(Cyan.r, Cyan.g, Cyan.b, ConnectionCount() == 3 ? .34f + .25f * Mathf.Sin(worldClock * 4f) : 0f);
                float frost = phase == Phase.Playing ? 1f - Mathf.Clamp01(timeLeft / YurinabangRules.RunSeconds) : 0f;
                frostRise.rectTransform.sizeDelta = new Vector2(1200, 28 + frost * 250f);
                UpdateThreads();
                UpdatePaneHighlights();
            }
            if (phase == Phase.Reveal && revealCorrect && roofLatchEvent.gameObject.activeSelf)
            {
                float duration = revealPractice ? 2.35f : 2.15f;
                float k = 1f - Mathf.Clamp01(revealClock / duration);
                float eased = 1f - Mathf.Pow(1f - k, 3f);
                roofLatchEvent.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * .025f);
                roofGlow.color = new Color(Cyan.r, Cyan.g, Cyan.b, .12f + .66f * eased);
                for (int i = 0; i < roofPanels.Length; i++)
                {
                    float gate = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k * 1.8f - i * .16f));
                    float direction = i == 0 ? 1f : i == 2 ? -1f : 0f;
                    roofPanels[i].rectTransform.anchoredPosition = new Vector2(direction * 26f * gate, 5f * gate);
                    roofPanels[i].rectTransform.localRotation = Quaternion.Euler(0, 0, direction * 4f * gate);
                    ceramicLatches[i].rectTransform.anchoredPosition = new Vector2(0f, 34f * gate);
                    ceramicLatches[i].rectTransform.localRotation = Quaternion.Euler(0, 0, (i - 1) * 45f * gate);
                }
                AnimateInterlockKey(k);
                var chosen = candidatePanes[selectedCandidate].root;
                chosen.localRotation = Quaternion.identity;
                chosen.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * .045f);
            }
            if (scorePunch > 0f)
            {
                scorePunch -= dt;
                float k = Mathf.Sin(Mathf.Clamp01(scorePunch / .42f) * Mathf.PI);
                scoreText.rectTransform.localScale = Vector3.one * (1f + .28f * k);
            }
            else if (scoreText) scoreText.rectTransform.localScale = Vector3.one;
        }

        void AnimateInterlockKey(float progress)
        {
            if (!routeKey) return;
            float trip = Mathf.Clamp01(progress) * 3f;
            int leg = Mathf.Min(2, Mathf.FloorToInt(trip));
            float t = Mathf.SmoothStep(0f, 1f, trip - leg);
            Vector2 b = ScreenPoint(ceramicLatches[leg].rectTransform);
            Vector2 a = leg == 0 ? ScreenPoint(roofLatchEvent) + Vector2.up * 76f * CanvasScale() : ScreenPoint(ceramicLatches[leg - 1].rectTransform);
            Vector2 p = Vector2.Lerp(a, b, t);
            p.y += Mathf.Sin(t * Mathf.PI) * 14f * CanvasScale();
            SetScreenPosition(routeKey, (RectTransform)revealGroup.transform, p);
            Vector2 d = b - a;
            routeKey.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            float pulse = .84f + .16f * Mathf.Abs(Mathf.Sin(worldClock * 10f));
            routeKeyFinL.rectTransform.localScale = new Vector3(pulse, 1f, 1f);
            routeKeyFinR.rectTransform.localScale = new Vector3(pulse, 1f, 1f);
            routeKeyCore.color = Color.Lerp(Iron, Brass, .25f + .5f * Mathf.Sin(progress * Mathf.PI));
        }

        void UpdateThreads()
        {
            for (int i = 0; i < 3; i++)
            {
                bool on = connection[i] >= 0 && selectedCandidate >= 0;
                laceLines[i].gameObject.SetActive(on);
                if (on)
                {
                    laceLines[i].color = phase == Phase.Reveal ? laceLines[i].color : Brass;
                    PlaceScreenLine(laceLines[i], ScreenPoint(sourcePane.markers[i]), ScreenPoint(candidatePanes[selectedCandidate].markers[connection[i]]), 7f);
                }
            }
            if (dragging && dragSource >= 0)
            {
                dragLine.gameObject.SetActive(true);
                PlaceScreenLine(dragLine, ScreenPoint(sourcePane.markers[dragSource]), MgfPointer.Position, 6f);
            }
            if (phase == Phase.Practice && connection[0] < 0)
            {
                guideLine.gameObject.SetActive(true);
                guideDot.gameObject.SetActive(true);
                Vector2 a = ScreenPoint(sourcePane.markers[0]);
                Vector2 b = ScreenPoint(candidatePanes[current.correctCandidate].markers[0]);
                PlaceScreenLine(guideLine, a, b, 3f);
                float t = .5f - .5f * Mathf.Cos((worldClock % 1.8f) / 1.8f * Mathf.PI * 2f);
                SetScreenPosition(guideDot, Vector2.Lerp(a, b, t));
            }
            else
            {
                guideLine.gameObject.SetActive(false);
                guideDot.gameObject.SetActive(false);
            }
        }

        void UpdatePaneHighlights()
        {
            float pulse = .5f + .5f * Mathf.Sin(worldClock * 4.5f);
            for (int c = 0; c < 3; c++) candidatePanes[c].halo.color = c == selectedCandidate ? new Color(Cyan.r, Cyan.g, Cyan.b, .24f + pulse * .18f) : Color.clear;
            for (int i = 0; i < 3; i++)
            {
                bool guide = phase == Phase.Practice && connection[0] < 0 && i == 0;
                sourcePane.markerImages[i].color = guide ? Color.Lerp(Cyan, Ivory, pulse * .45f) : Cyan;
            }
            if (phase == Phase.Practice && connection[0] < 0)
                candidatePanes[current.correctCandidate].markerImages[0].color = Color.Lerp(Brass, Ivory, pulse * .45f);
        }

        void AnimateWorld()
        {
            for (int i = 0; i < relayShuttles.Length; i++)
            {
                float t = worldClock * (.42f + (i % 5) * .035f) + i * .71f;
                Vector3 p = shuttleBases[i];
                p.x += Mathf.Sin(t) * (.12f + (i % 3) * .025f);
                p.y += (Mathf.PingPong(t * .12f, 1f) - .5f) * .06f;
                relayShuttles[i].transform.position = p;
                relayShuttles[i].transform.rotation = Quaternion.Euler(0f, Mathf.Sin(t) * 8f, 0f);
            }
        }

        void UpdateLayout()
        {
            float aspect = Screen.width / Mathf.Max(1f, (float)Screen.height);
            if (Mathf.Abs(aspect - layoutAspect) < .015f) return;
            layoutAspect = aspect;
            bool land = aspect >= 1.28f;
            bool ultra = aspect >= 1.75f && Screen.width >= 1700;
            if (ultra)
            {
                // 2000px 초와이드는 목표와 HUD를 한 덩어리로 확대하지 않고 좌우 계기로 분리한다.
                SetAnchor(goalPlateRt, new Vector2(.29f, .945f));
                goalPlateRt.sizeDelta = new Vector2(300, 42);
                goalText.rectTransform.sizeDelta = new Vector2(284, 38);
                goalText.fontSize = 14f;
                SetAnchor(statsPlateRt, new Vector2(.71f, .945f));
                statsPlateRt.sizeDelta = new Vector2(300, 42);
                scoreText.fontSize = 18f; floorText.fontSize = 14f; timeText.fontSize = 14f;
                SetAnchor(progressRt, new Vector2(.5f, .865f));
                progressRt.sizeDelta = new Vector2(286, 16);
                LayoutPane(sourcePane, new Vector2(.50f, .25f), 150, 136);
                LayoutPane(candidatePanes[0], new Vector2(.20f, .63f), 126, 116);
                LayoutPane(candidatePanes[1], new Vector2(.50f, .68f), 126, 116);
                LayoutPane(candidatePanes[2], new Vector2(.80f, .63f), 126, 116);
                ropeLine.anchorMin = ropeLine.anchorMax = new Vector2(.93f, .52f);
                ropeHandle.anchorMin = ropeHandle.anchorMax = new Vector2(.93f, .36f);
                SetAnchor(statusPlateRt, new Vector2(.5f, .095f));
                statusPlateRt.sizeDelta = new Vector2(430, 46);
                statusText.rectTransform.sizeDelta = new Vector2(414, 42);
                SetAnchor(roofLatchEvent, new Vector2(.5f, .70f));
                roofLatchEvent.sizeDelta = new Vector2(430, 188);
                SetAnchor(evidencePanel, new Vector2(.5f, .20f));
                evidencePanel.sizeDelta = new Vector2(520, 172);
                revealTitle.rectTransform.sizeDelta = new Vector2(478, 44);
                revealTitle.fontSize = 22f;
                revealFormula.rectTransform.sizeDelta = new Vector2(478, 112);
                revealFormula.fontSize = 18f;
            }
            else if (land)
            {
                SetAnchor(goalPlateRt, new Vector2(.5f, .965f));
                goalPlateRt.sizeDelta = new Vector2(360, 42);
                goalText.rectTransform.sizeDelta = new Vector2(342, 38);
                goalText.fontSize = 15f;
                SetAnchor(statsPlateRt, new Vector2(.5f, .895f));
                statsPlateRt.sizeDelta = new Vector2(350, 44);
                scoreText.fontSize = 20f; floorText.fontSize = 15f; timeText.fontSize = 15f;
                SetAnchor(progressRt, new Vector2(.5f, .825f));
                progressRt.sizeDelta = new Vector2(310, 16);
                LayoutPane(sourcePane, new Vector2(.50f, .34f), 174, 158);
                LayoutPane(candidatePanes[0], new Vector2(.18f, .60f), 148, 136);
                LayoutPane(candidatePanes[1], new Vector2(.50f, .64f), 148, 136);
                LayoutPane(candidatePanes[2], new Vector2(.82f, .60f), 148, 136);
                ropeLine.anchorMin = ropeLine.anchorMax = new Vector2(.94f, .53f);
                ropeHandle.anchorMin = ropeHandle.anchorMax = new Vector2(.94f, .38f);
                SetAnchor(statusPlateRt, new Vector2(.5f, .09f));
                statusPlateRt.sizeDelta = new Vector2(400, 46);
                statusText.rectTransform.sizeDelta = new Vector2(384, 42);
                SetAnchor(roofLatchEvent, new Vector2(.5f, .70f));
                roofLatchEvent.sizeDelta = new Vector2(410, 184);
                SetAnchor(evidencePanel, new Vector2(.5f, .20f));
                evidencePanel.sizeDelta = new Vector2(490, 174);
                revealTitle.rectTransform.sizeDelta = new Vector2(450, 44);
                revealTitle.fontSize = 22f;
                revealFormula.rectTransform.sizeDelta = new Vector2(450, 114);
                revealFormula.fontSize = 18f;
            }
            else
            {
                // 우상단 48px 음소거 버튼을 침범하지 않는 세로 안전영역.
                SetAnchor(goalPlateRt, new Vector2(.44f, .962f));
                goalPlateRt.sizeDelta = new Vector2(310, 46);
                goalText.rectTransform.sizeDelta = new Vector2(294, 42);
                goalText.fontSize = 14f;
                SetAnchor(statsPlateRt, new Vector2(.5f, .895f));
                statsPlateRt.sizeDelta = new Vector2(370, 48);
                scoreText.fontSize = 22f; floorText.fontSize = 16f; timeText.fontSize = 16f;
                SetAnchor(progressRt, new Vector2(.5f, .835f));
                progressRt.sizeDelta = new Vector2(330, 18);
                LayoutPane(sourcePane, new Vector2(.48f, .34f), 184, 166);
                LayoutPane(candidatePanes[0], new Vector2(.17f, .66f), 118, 112);
                LayoutPane(candidatePanes[1], new Vector2(.50f, .72f), 118, 112);
                LayoutPane(candidatePanes[2], new Vector2(.83f, .66f), 118, 112);
                ropeLine.anchorMin = ropeLine.anchorMax = new Vector2(.92f, .44f);
                ropeHandle.anchorMin = ropeHandle.anchorMax = new Vector2(.92f, .315f);
                SetAnchor(statusPlateRt, new Vector2(.5f, .205f));
                statusPlateRt.sizeDelta = new Vector2(360, 46);
                statusText.rectTransform.sizeDelta = new Vector2(344, 42);
                SetAnchor(roofLatchEvent, new Vector2(.5f, .70f));
                roofLatchEvent.sizeDelta = new Vector2(340, 188);
                SetAnchor(evidencePanel, new Vector2(.5f, .205f));
                evidencePanel.sizeDelta = new Vector2(350, 224);
                revealTitle.rectTransform.sizeDelta = new Vector2(314, 52);
                revealTitle.fontSize = 22f;
                revealFormula.rectTransform.sizeDelta = new Vector2(314, 146);
                revealFormula.fontSize = 18f;
            }
            if (land)
            {
                SetAnchor(titleSign, new Vector2(.70f, .74f));
                SetAnchor(titleTagline.rectTransform, new Vector2(.70f, .58f));
                titleTagline.rectTransform.sizeDelta = new Vector2(380, 44);
                SetAnchor(titleRopeLine, new Vector2(.27f, .63f));
                SetAnchor(titleHandle, new Vector2(.27f, .34f));
                titleInstruction.rectTransform.anchorMin = titleInstruction.rectTransform.anchorMax = new Vector2(.27f, .18f);
                titleBest.rectTransform.anchorMin = titleBest.rectTransform.anchorMax = new Vector2(.70f, .11f);
            }
            else
            {
                SetAnchor(titleSign, new Vector2(.5f, .78f));
                SetAnchor(titleTagline.rectTransform, new Vector2(.5f, .61f));
                titleTagline.rectTransform.sizeDelta = new Vector2(350, 54);
                SetAnchor(titleRopeLine, new Vector2(.5f, .58f));
                SetAnchor(titleHandle, new Vector2(.5f, .31f));
                titleInstruction.rectTransform.anchorMin = titleInstruction.rectTransform.anchorMax = new Vector2(.5f, .17f);
                titleBest.rectTransform.anchorMin = titleBest.rectTransform.anchorMax = new Vector2(.5f, .075f);
            }
            if (current != null)
            {
                RefreshProblem();
                Canvas.ForceUpdateCanvases();
                if (ultra) ResolveUltraPaneGap();
            }
        }

        void ResolveUltraPaneGap()
        {
            // 중앙 후보와 기준창의 모든 수학 텍스트/고리를 실제 화면 Rect로 합쳐
            // 최소 24px 안전영역을 자동 보장한다. 폰트 메트릭이 바뀌어도 겹침이 재발하지 않는다.
            Rect candidate = PaneContentScreenRect(candidatePanes[1]);
            Rect source = PaneContentScreenRect(sourcePane);
            float requiredBottom = source.yMax + 24f;
            if (candidate.yMin < requiredBottom)
            {
                float shift = (requiredBottom - candidate.yMin) / Mathf.Max(.5f, CanvasScale());
                candidatePanes[1].root.anchoredPosition += Vector2.up * shift;
            }
        }

        static Rect PaneContentScreenRect(PaneView p)
        {
            Rect r = ScreenRect(p.caption.rectTransform);
            for (int i = 0; i < 3; i++)
            {
                r = Union(r, ScreenRect(p.markers[i]));
                if (!string.IsNullOrEmpty(p.sideLabels[i].text)) r = Union(r, ScreenRect(p.sideLabels[i].rectTransform));
                if (!string.IsNullOrEmpty(p.angleLabels[i].text)) r = Union(r, ScreenRect(p.angleLabels[i].rectTransform));
            }
            return r;
        }

        static Rect ScreenRect(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static Rect Union(Rect a, Rect b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }

        static void SetAnchor(RectTransform rt, Vector2 anchor)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
        }

        void LayoutPane(PaneView p, Vector2 anchor, float width, float height)
        {
            p.root.anchorMin = p.root.anchorMax = anchor;
            p.root.anchoredPosition = Vector2.zero;
            p.root.sizeDelta = new Vector2(width, height);
            p.width = width; p.height = height;
            p.frame.rectTransform.sizeDelta = new Vector2(width, height);
            p.glass.rectTransform.sizeDelta = new Vector2(width * .91f, height * .89f);
            p.reflection.rectTransform.sizeDelta = new Vector2(width * .67f, height * .62f);
            p.halo.rectTransform.sizeDelta = new Vector2(width * 1.02f, height * 1.02f);
            float marker = width < 140f ? 38f : width < 160f ? 44f : 50f;
            float labelSize = width < 140f ? 14f : width < 160f ? 15f : 17f;
            p.caption.fontSize = width < 140f ? 13f : width < 160f ? 14f : 17f;
            p.caption.rectTransform.sizeDelta = new Vector2(width + 12f, 34f);
            for (int i = 0; i < p.markers.Length; i++)
            {
                p.markers[i].sizeDelta = new Vector2(marker, marker);
                p.markerLabels[i].fontSize = width < 140f ? 17f : 21f;
                p.sideLabels[i].fontSize = labelSize;
                p.sideLabels[i].rectTransform.sizeDelta = new Vector2(width < 140f ? 72f : 90f, 32f);
                p.angleLabels[i].fontSize = width < 140f ? 13f : 16f;
            }
        }

        void PlaceScreenLine(Image image, Vector2 screenA, Vector2 screenB, float logicalWidth)
        {
            Vector2 a, b;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(laceRoot, screenA, null, out a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(laceRoot, screenB, null, out b);
            PlaceLocalLine(image, a, b, logicalWidth);
        }

        static void PlaceLocalLine(Image image, Vector2 a, Vector2 b, float width)
        {
            Vector2 d = b - a;
            image.rectTransform.anchoredPosition = (a + b) * .5f;
            image.rectTransform.sizeDelta = new Vector2(d.magnitude, width);
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        void SetScreenPosition(RectTransform rt, Vector2 screen)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(laceRoot, screen, null, out local);
            rt.anchoredPosition = local;
        }

        static void SetScreenPosition(RectTransform rt, RectTransform parent, Vector2 screen)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out local);
            rt.anchoredPosition = local;
        }

        CanvasGroup Group(string name, Transform parent)
        {
            var rt = Stretch(name, parent);
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            g.interactable = false; g.blocksRaycasts = false;
            return g;
        }

        RectTransform Stretch(string name, Transform parent)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            return rt;
        }

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = offset; rt.sizeDelta = size;
            return rt;
        }

        Image AddImage(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color color, Sprite sprite)
        {
            var rt = R(name, parent, anchor, offset, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.type = sprite == roundedSprite ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color; image.raycastTarget = false;
            return image;
        }

        TextMeshProUGUI AddText(Transform parent, string text, Vector2 anchor, Vector2 offset, float size, Color color, float width)
        {
            var t = MgfText.Ui(text, anchor, offset, size, color, width);
            t.transform.SetParent(parent, false);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = offset;
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            return t;
        }

        Sprite MakeRoundedSprite(int size, int radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "LeadRing" : "IronRounded" };
            var px = new Color32[size * size];
            float half = (size - 1) * .5f;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Abs(x - half) - (half - radius));
                float dy = Mathf.Max(0, Mathf.Abs(y - half) - (half - radius));
                bool inside = dx * dx + dy * dy <= radius * radius;
                if (ring)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(half, half));
                    inside = d > size * .31f && d < size * .47f;
                }
                px[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(px); tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        Sprite MakeTriangleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "StainedTriangle" };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fy = y / (float)(size - 1);
                float halfWidth = fy * .47f;
                float fx = x / (float)(size - 1) - .5f;
                bool inside = fy > .04f && fy < .96f && Mathf.Abs(fx) <= halfWidth;
                byte alpha = inside ? (byte)(210 + 35 * Mathf.Sin((x * 13 + y * 7) * .08f)) : (byte)0;
                px[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            tex.SetPixels32(px); tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
        }

        Sprite MakeWingSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "MothWing" };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float nx = (x + .5f) / size * 2f - 1f;
                float ny = (y + .5f) / size * 2f - 1f;
                bool inside = nx * nx * .7f + ny * ny < 1f && nx + ny < .9f;
                px[y * size + x] = inside ? new Color32(255, 255, 255, 225) : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(px); tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
        }

        public void TestStart()
        {
            StartPractice();
        }

        public void TestAnswerCorrect()
        {
            if (phase == Phase.Title || phase == Phase.End) StartPractice();
            if (phase == Phase.Reveal)
            {
                revealClock = 0;
                return;
            }
            selectedCandidate = current.correctCandidate;
            for (int i = 0; i < 3; i++) connection[i] = current.vertexMap[i];
            st.selectedCandidate = selectedCandidate; st.selectedCount = 3;
            Submit();
        }

        public void TestAnswerWrong()
        {
            if (phase == Phase.Title || phase == Phase.End) StartPractice();
            if (phase == Phase.Practice) StartRun();
            if (phase == Phase.Reveal) { revealClock = 0; return; }
            selectedCandidate = (current.correctCandidate + 1) % 3;
            int[] wrong = YurinabangRules.Permutations[(YurinabangRules.PermutationIndex(current.vertexMap) + 1) % 6];
            for (int i = 0; i < 3; i++) connection[i] = wrong[i];
            st.selectedCandidate = selectedCandidate; st.selectedCount = 3;
            Submit();
        }

        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
