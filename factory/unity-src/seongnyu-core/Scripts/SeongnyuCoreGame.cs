// 석류 코어 — 거대한 석류 단면에서 유리 모세관으로 수선을 직접 구성하는 2.5D 정밀 아케이드.
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.SeongnyuCore
{
    public sealed class SeongnyuCoreGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int roundIndex;
            public int firstAttempts;
            public int firstCorrect;
            public int firstFailedRounds;
            public int selectedSnap;
            public int previousSnap;
            public int toolPose;
            public bool submitted;
            public string problemId = "";
            public string misconceptionId = "";
            public int inputSerial;
            public int handleScreenX;
            public int handleScreenY;
            public int railStartScreenX;
            public int railStartScreenY;
            public int railEndScreenX;
            public int railEndScreenY;
        }

        enum RunPhase { Title, Practice, Playing, Reveal, End }

        static readonly Color Ruby = MgfLook.Hex("#A92E52");
        static readonly Color Coral = MgfLook.Hex("#EF6F61");
        static readonly Color Blush = MgfLook.Hex("#FFD8C7");
        static readonly Color Ivory = MgfLook.Hex("#F4F0DF");
        static readonly Color Ink = MgfLook.Hex("#25202A");
        static readonly Color Juice = MgfLook.Hex("#FF9D55");
        static readonly Color Deep = MgfLook.Hex("#64162E");
        static readonly Color Glass = new Color(1f, .94f, .82f, .56f);

        readonly State st = new State();
        readonly List<CoreProblem> bank = new List<CoreProblem>(375);
        readonly List<CoreProblem>[] bands = { new List<CoreProblem>(), new List<CoreProblem>(), new List<CoreProblem>(), new List<CoreProblem>() };
        readonly Transform[] chambers = new Transform[12];
        readonly Transform[] chamberMembranes = new Transform[12];
        readonly Transform[] chamberGerms = new Transform[12];
        readonly Transform[] bubbles = new Transform[18];
        readonly Vector3[] bubbleHomes = new Vector3[18];
        readonly Transform[] seals = new Transform[3];
        readonly LineRenderer[] savedPaths = new LineRenderer[8];
        readonly Transform[] savedGerms = new Transform[8];
        readonly Transform[] juiceBeads = new Transform[16];

        Camera cam;
        RunPhase phase = RunPhase.Title;
        CoreProblem current;
        System.Random rng = new System.Random(7301);
        int runSerial, band1Cursor, band2Cursor, band3Cursor;
        float secondsLeft = 90f;
        bool firstAttemptOnRound;

        Material rindMat, rubyMat, rubyLightMat, coralMat, ivoryMat, inkMat, glassMat, glassBrightMat, scarMat, juiceMat, clearMembraneMat;
        Transform worldRoot, rindRoot, boardRoot, handle, germ, germBody, germArmL, germArmR, rulerRoot, rulerKnob;
        Transform titleHandle, titleCore;
        Renderer handleRenderer, germRenderer;
        LineRenderer railLine, capillaryLine, scarLine, correctionLine, rulerLong, rulerShort, titleSpiral;
        readonly Transform[] ticks = new Transform[25];
        readonly Renderer[] tickRenderers = new Renderer[25];
        Vector3 railCenter, railDir, railPerp, pointP, handleHome;
        // 세로 390px에서도 대각선 레일 25눈금과 P가 모두 화면 안에 남는 균일 간격.
        const float TickGap = .27f;

        bool dragging, dragMoved, pointerStartedOnHandle, draggedToRail;
        Vector2 dragStartScreen;
        int hoveredSnap;
        float dragDistance;
        float invalidPulse, handleNudge, idleGuide, titleTime, revealTime, hitStop, wrongShake, scorePunch;
        bool revealCorrect, revealFirst, practiceWrongReveal;
        int revealSnap;
        float chamberOpen;
        Vector3 baseCamPos;
        int beadCount;
        float beadTravel;

        CanvasGroup titleGroup, hudGroup, endGroup, feedbackGroup, guideGroup;
        RawImage titleArt;
        TextMeshProUGUI logoText, tagText, titleBadge, titleBest, titleCta;
        TextMeshProUGUI goalText, promptText, statusText, progressText, timerText, feedbackText, guideText;
        TextMeshProUGUI endTitle, endStats, endScore, endCta;
        Image goalPanel, promptPanel, timerTrack, timerFill, scoreGlow, guideRing, guideHand, guideArrow, endShade;
        float timerBarWidth = 350f;
        float displayedScore;
        float lastLogicalW = -1, lastLogicalH = -1;
        Sprite panelSprite, ringSprite, arrowSprite, handSprite;

        void Awake()
        {
            MgfLook.Quality(32f);
            bank.AddRange(SeongnyuRules.BuildBank());
            for (int i = 0; i < bank.Count; i++) bands[bank[i].band].Add(bank[i]);
            BuildMaterials();
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void BuildMaterials()
        {
            rindMat = MgfLook.Lit(Coral, .18f, .01f);
            coralMat = MgfLook.Lit(MgfLook.Hex("#F68B76"), .28f, .01f);
            rubyMat = MgfLook.Lit(Ruby, .50f, .04f, Ruby * .09f);
            rubyLightMat = MgfLook.Lit(MgfLook.Hex("#D94A65"), .55f, .03f, Ruby * .13f);
            ivoryMat = MgfLook.Lit(Ivory, .32f, .02f);
            inkMat = MgfLook.Lit(Ink, .18f, .01f);
            juiceMat = MgfLook.Lit(Juice, .60f, .04f, Juice * .42f);
            glassMat = MgfLook.Alpha(Glass);
            glassBrightMat = MgfLook.Alpha(new Color(1f, .98f, .84f, .88f));
            scarMat = MgfLook.Alpha(new Color(.95f, .90f, .82f, .34f));
            clearMembraneMat = MgfLook.Alpha(new Color(1f, .73f, .69f, .20f));
        }

        void BuildWorld()
        {
            // WebGL 스트리핑에서 프리미티브 콜라이더 타입을 보존한다.
            var types = new GameObject("PrimitiveColliderTypes");
            types.AddComponent<SphereCollider>(); types.AddComponent<CapsuleCollider>();
            Destroy(types);

            MgfLook.Sky(MgfLook.Hex("#8D2447"), MgfLook.Hex("#ED756B"), MgfLook.Hex("#FFD2B9"), .86f);
            MgfLook.Sun(new Vector3(38, -32, 18), MgfLook.Hex("#FFF2DA"), 1.10f, .62f);
            cam = MgfLook.Camera(new Vector3(0, 4.8f, -19.5f), new Vector3(0, .55f, 0), 32f);
            cam.orthographic = true;
            cam.orthographicSize = 9.5f;
            baseCamPos = cam.transform.position;

            worldRoot = new GameObject("GiantPomegranateCrossSection").transform;
            // 빈 탁자 대신 화면 전체를 채우는 과피 단면과 젤리 씨방을 만든다.
            MgfLook.Block("PomegranateBack", new Vector3(0, .25f, 3.5f), new Vector3(22, 16, 1.2f), 1.6f, rindMat, worldRoot);
            MgfLook.Block("InnerCoralFlesh", new Vector3(0, .2f, 2.72f), new Vector3(18.5f, 13.4f, .72f), 1.7f, coralMat, worldRoot);
            MgfLook.Block("RubyCavity", new Vector3(0, .18f, 2.18f), new Vector3(15.9f, 11.3f, .48f), 1.9f, rubyMat, worldRoot);
            BuildRindRing();
            BuildChambers();
            BuildAmbientBubbles();
            BuildSavedGallery();
            BuildMeasurementBoard();
            BuildTitleSculpture();
        }

        void BuildRindRing()
        {
            rindRoot = new GameObject("ThickRindRing").transform;
            rindRoot.SetParent(worldRoot, false);
            for (int i = 0; i < 30; i++)
            {
                float a = i * Mathf.PI * 2f / 30f;
                float x = Mathf.Cos(a) * 7.9f, y = Mathf.Sin(a) * 5.55f + .25f;
                var rind = MgfLook.Prim(PrimitiveType.Capsule, "RindSegment", new Vector3(x, y, 1.45f),
                    new Vector3(.92f, 1.18f, .74f), i % 3 == 0 ? rindMat : coralMat, rindRoot, false);
                rind.transform.rotation = Quaternion.Euler(12, 0, -a * Mathf.Rad2Deg - 90);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f + .2f;
                var septum = MgfLook.Block("JellySeptum", new Vector3(Mathf.Cos(a) * 3.9f, Mathf.Sin(a) * 2.7f + .2f, 1.55f),
                    new Vector3(5.6f, .20f, .26f), .09f, ivoryMat, rindRoot);
                septum.transform.rotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
            }
        }

        void BuildChambers()
        {
            for (int i = 0; i < chambers.Length; i++)
            {
                float a = (i * 30f + 15f) * Mathf.Deg2Rad;
                var root = new GameObject("BreathingArilChamber" + i).transform;
                root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(Mathf.Cos(a) * 5.1f, Mathf.Sin(a) * 3.55f + .2f, .9f);
                root.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 90f);
                chambers[i] = root;
                var outer = MgfLook.Prim(PrimitiveType.Sphere, "RubyAril", Vector3.zero,
                    new Vector3(1.22f, 1.55f, .88f), i % 2 == 0 ? rubyLightMat : rubyMat, root, false);
                outer.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                var membrane = MgfLook.Prim(PrimitiveType.Sphere, "FoldedJellyMembrane", new Vector3(0, 0, -.33f),
                    new Vector3(1.35f, 1.67f, .16f), clearMembraneMat, root, false).transform;
                chamberMembranes[i] = membrane;
                chamberGerms[i] = MakeGerm(root, new Vector3(0, -.06f, -.58f), .34f, "SleepingGerm" + i);
            }
        }

        Transform MakeGerm(Transform parent, Vector3 local, float scale, string name)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false); root.localPosition = local; root.localScale = Vector3.one * scale;
            var body = MgfLook.Prim(PrimitiveType.Sphere, "RubyDropBody", Vector3.zero, new Vector3(1.0f, 1.25f, .72f), rubyLightMat, root, false);
            body.transform.localRotation = Quaternion.Euler(0, 0, -6);
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = MgfLook.Prim(PrimitiveType.Sphere, "BlackSeedEye", new Vector3(side * .25f, .13f, -.66f),
                    new Vector3(.16f, .22f, .11f), inkMat, root, false);
                eye.transform.localScale = new Vector3(.16f, .22f, .11f);
                var arm = MgfLook.Prim(PrimitiveType.Capsule, "LeafArm", new Vector3(side * .72f, -.08f, -.02f),
                    new Vector3(.15f, .48f, .13f), coralMat, root, false);
                arm.transform.localRotation = Quaternion.Euler(0, 0, side * 58f);
            }
            MgfLook.Prim(PrimitiveType.Capsule, "LeafCrown", new Vector3(0, .86f, .0f),
                new Vector3(.13f, .36f, .10f), ivoryMat, root, false).transform.localRotation = Quaternion.Euler(0, 0, 22f);
            return root;
        }

        void BuildAmbientBubbles()
        {
            for (int i = 0; i < bubbles.Length; i++)
            {
                float x = -7f + (i * 37 % 140) / 10f;
                float y = -5.2f + (i * 23 % 95) / 10f;
                bubbleHomes[i] = new Vector3(x, y, -.5f - (i % 4) * .14f);
                var b = MgfLook.Prim(PrimitiveType.Sphere, "SlowJuiceBubble" + i, bubbleHomes[i],
                    Vector3.one * (.10f + (i % 4) * .045f), glassBrightMat, worldRoot, false).transform;
                bubbles[i] = b;
            }
            for (int i = 0; i < juiceBeads.Length; i++)
            {
                var b = MgfLook.Prim(PrimitiveType.Sphere, "CapillaryJuiceBead" + i, Vector3.zero,
                    Vector3.one * (.10f + (i % 3) * .025f), juiceMat, worldRoot, false).transform;
                b.gameObject.SetActive(false); juiceBeads[i] = b;
            }
        }

        void BuildSavedGallery()
        {
            for (int i = 0; i < savedPaths.Length; i++)
            {
                savedPaths[i] = MakeLine("SavedPerpendicular" + i, glassBrightMat, .09f, worldRoot);
                savedPaths[i].positionCount = 2; savedPaths[i].gameObject.SetActive(false);
                savedGerms[i] = MakeGerm(worldRoot, Vector3.zero, .23f, "RescuedGerm" + i);
                savedGerms[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < seals.Length; i++)
            {
                seals[i] = MgfLook.Prim(PrimitiveType.Sphere, "SeptumSeal" + i,
                    new Vector3(-.72f + i * .72f, -4.85f, -.48f), new Vector3(.25f, .34f, .16f),
                    ivoryMat, worldRoot, false).transform;
            }
        }

        void BuildMeasurementBoard()
        {
            boardRoot = new GameObject("ClearMeasurementSkin").transform;
            boardRoot.SetParent(worldRoot, false); boardRoot.localPosition = new Vector3(0, .0f, -1.25f);
            MgfLook.Block("JellyMeasurementSkin", Vector3.zero, new Vector3(12.8f, 8.0f, .18f), .72f, rubyMat, boardRoot);
            MgfLook.Block("IvoryInnerSkin", new Vector3(0, 0, -.14f), new Vector3(11.8f, 7.0f, .10f), .62f, coralMat, boardRoot);
            // 정방 모눈은 답을 누설하지 않고 직각 판단 근거만 제공한다.
            for (int i = -6; i <= 6; i++)
            {
                var v = MakeLine("GridV", scarMat, .018f, boardRoot); v.positionCount = 2;
                v.SetPosition(0, new Vector3(i * .82f, -3.15f, -.20f)); v.SetPosition(1, new Vector3(i * .82f, 3.15f, -.20f));
            }
            for (int i = -3; i <= 3; i++)
            {
                var h = MakeLine("GridH", scarMat, .018f, boardRoot); h.positionCount = 2;
                h.SetPosition(0, new Vector3(-5.25f, i * .82f, -.20f)); h.SetPosition(1, new Vector3(5.25f, i * .82f, -.20f));
            }
            railLine = MakeLine("InfiniteRindRailL", ivoryMat, .105f, boardRoot); railLine.positionCount = 2;
            capillaryLine = MakeLine("GlassCapillaryPH", glassBrightMat, .14f, boardRoot); capillaryLine.positionCount = 2;
            scarLine = MakeLine("MilkyFailureScar", scarMat, .12f, boardRoot); scarLine.positionCount = 2; scarLine.gameObject.SetActive(false);
            correctionLine = MakeLine("CorrectionPerpendicular", glassBrightMat, .07f, boardRoot); correctionLine.positionCount = 2; correctionLine.gameObject.SetActive(false);
            for (int i = 0; i < ticks.Length; i++)
            {
                var tick = MgfLook.Block("UniformRindTick" + i, Vector3.zero, new Vector3(.055f, .28f, .055f), .018f, ivoryMat, boardRoot);
                ticks[i] = tick.transform; tickRenderers[i] = tick.GetComponent<Renderer>();
            }
            handle = MgfLook.Prim(PrimitiveType.Sphere, "MilkyGlassCapillaryHandle", Vector3.zero,
                new Vector3(1.10f, 1.10f, .66f), glassBrightMat, boardRoot, true).transform;
            var hc = handle.GetComponent<SphereCollider>(); hc.radius = .82f;
            handleRenderer = handle.GetComponent<Renderer>();
            germ = MakeGerm(boardRoot, Vector3.zero, .54f, "ActiveSeedEyeGerm");
            germBody = germ.Find("RubyDropBody"); germArmL = germ.Find("LeafArm");
            if (germ.childCount > 4) germArmR = germ.GetChild(4);
            germRenderer = germBody.GetComponent<Renderer>();
            var pLabel = MgfText.World("P", new Vector3(0, .70f, -.5f), .74f, Ivory, germ); pLabel.transform.localScale = Vector3.one;
            BuildRuler();
            boardRoot.gameObject.SetActive(false);
        }

        void BuildRuler()
        {
            rulerRoot = new GameObject("ManualTransparentSetSquare").transform;
            rulerRoot.SetParent(boardRoot, false);
            rulerLong = MakeLine("SetSquareRailEdge", glassBrightMat, .075f, rulerRoot);
            rulerLong.positionCount = 2; rulerLong.SetPosition(0, new Vector3(-1.5f, 0, 0)); rulerLong.SetPosition(1, new Vector3(1.5f, 0, 0));
            rulerShort = MakeLine("SetSquarePerpendicularEdge", glassBrightMat, .075f, rulerRoot);
            rulerShort.positionCount = 2; rulerShort.SetPosition(0, Vector3.zero); rulerShort.SetPosition(1, new Vector3(0, 2.0f, 0));
            var diag = MakeLine("SetSquareDiagonal", scarMat, .05f, rulerRoot);
            diag.positionCount = 2; diag.SetPosition(0, new Vector3(-1.5f, 0, 0)); diag.SetPosition(1, new Vector3(0, 2.0f, 0));
            rulerKnob = MgfLook.Prim(PrimitiveType.Sphere, "SetSquareRotationKnob", new Vector3(0, 2.25f, 0),
                Vector3.one * .32f, glassBrightMat, rulerRoot, true).transform;
            rulerRoot.localPosition = new Vector3(-3.8f, -2.55f, -.42f);
        }

        void BuildTitleSculpture()
        {
            titleCore = MgfLook.Prim(PrimitiveType.Sphere, "LivingJuiceCore", new Vector3(0, -.25f, -1.12f),
                new Vector3(2.2f, 1.55f, .82f), juiceMat, worldRoot, false).transform;
            titleHandle = MgfLook.Prim(PrimitiveType.Capsule, "CoreOpenGlassLever", new Vector3(0, -2.25f, -1.4f),
                new Vector3(1.65f, .42f, .42f), glassBrightMat, worldRoot, true).transform;
            titleHandle.rotation = Quaternion.Euler(0, 0, 90);
            titleHandle.GetComponent<CapsuleCollider>().radius = .78f;
            titleSpiral = MakeLine("TitleJuiceSpiral", juiceMat, .13f, worldRoot);
            titleSpiral.positionCount = 40;
            for (int i = 0; i < 40; i++)
            {
                float a = i * .43f, r = .10f + i * .035f;
                titleSpiral.SetPosition(i, new Vector3(Mathf.Cos(a) * r, -.25f + Mathf.Sin(a) * r * .65f, -1.58f));
            }
        }

        void BuildUi()
        {
            panelSprite = MakeRoundedSprite(72, 26f, Color.white, false);
            ringSprite = MakeRoundedSprite(72, 31f, Color.white, true);
            arrowSprite = MakeArrowSprite();
            handSprite = MakeHandSprite();

            var titleCanvas = NewGroup("TitleLayer", out titleGroup);
            titleArt = titleCanvas.gameObject.AddComponent<RawImage>();
            titleArt.raycastTarget = false;
            titleArt.color = new Color(1, 1, 1, .48f);
            titleArt.uvRect = new Rect(0, .08f, 1, .80f);
            var titleTex = Resources.Load<Texture2D>("SeongnyuCore/title");
            if (titleTex) titleArt.texture = titleTex; else titleArt.gameObject.SetActive(false); // 코드 디오라마 폴백
            logoText = AddText(titleCanvas, "석류\n코어", new Vector2(.5f, .78f), Vector2.zero, 58, Ivory, 330, FontStyles.Bold);
            logoText.lineSpacing = -22; logoText.outlineWidth = .27f; logoText.outlineColor = Deep;
            logoText.fontMaterial.EnableKeyword("UNDERLAY_ON");
            tagText = AddText(titleCanvas, "수선을 꽂아 씨눈을 깨워라", new Vector2(.5f, .64f), Vector2.zero, 20, Ivory, 350);
            titleBadge = AddText(titleCanvas, "중1 · 기본 도형 · 실전 90초", new Vector2(.5f, .205f), Vector2.zero, 15, Blush, 340);
            titleBest = AddText(titleCanvas, "첫 제출 6/8이 코어를 깨운다", new Vector2(.5f, .16f), Vector2.zero, 14, Ivory, 350);
            titleCta = AddText(titleCanvas, "유리 손잡이를 눌러 코어 열기", new Vector2(.5f, .305f), Vector2.zero, 19, Ink, 330, FontStyles.Bold);
            var ctaBg = AddPanel(titleCanvas, "WorldLeverLabel", new Vector2(.5f, .305f), Vector2.zero, new Vector2(330, 58), new Color(1f, .94f, .82f, .88f));
            ctaBg.transform.SetSiblingIndex(titleCta.transform.GetSiblingIndex());

            var hud = NewGroup("HudLayer", out hudGroup);
            goalPanel = AddPanel(hud, "GoalRibbon", new Vector2(.5f, .955f), Vector2.zero, new Vector2(366, 47), new Color(.15f, .08f, .16f, .82f));
            goalText = AddText(hud, "PH를 직선 l에 수직으로 꽂아 코어를 깨워라", new Vector2(.5f, .955f), Vector2.zero, 16, Ivory, 350, FontStyles.Bold);
            promptPanel = AddPanel(hud, "PromptMembrane", new Vector2(.5f, .855f), Vector2.zero, new Vector2(366, 96), new Color(.98f, .77f, .69f, .90f));
            promptText = AddText(hud, "", new Vector2(.5f, .855f), Vector2.zero, 15, Ink, 344, FontStyles.Bold);
            statusText = AddText(hud, "", new Vector2(.19f, .748f), Vector2.zero, 15, Ivory, 140, FontStyles.Bold);
            progressText = AddText(hud, "", new Vector2(.52f, .748f), Vector2.zero, 15, Ivory, 150, FontStyles.Bold);
            timerText = AddText(hud, "90", new Vector2(.86f, .748f), Vector2.zero, 18, Ivory, 80, FontStyles.Bold);
            timerTrack = AddPanel(hud, "JuiceTimerTrack", new Vector2(.5f, .705f), Vector2.zero, new Vector2(350, 13), new Color(.25f, .06f, .13f, .58f));
            timerFill = AddPanel(timerTrack.rectTransform, "JuiceTimerFill", new Vector2(0, .5f), Vector2.zero, new Vector2(350, 9), Juice);
            timerFill.rectTransform.pivot = new Vector2(0, .5f); timerFill.rectTransform.anchorMin = timerFill.rectTransform.anchorMax = new Vector2(0, .5f);
            scoreGlow = AddPanel(hud, "ScorePulse", new Vector2(.18f, .748f), Vector2.zero, new Vector2(120, 44), new Color(1f, .70f, .33f, 0));
            scoreGlow.transform.SetSiblingIndex(statusText.transform.GetSiblingIndex());

            var feedback = NewGroup("FeedbackLayer", out feedbackGroup);
            AddPanel(feedback, "FeedbackMembrane", new Vector2(.5f, .18f), Vector2.zero, new Vector2(350, 70), new Color(.16f, .07f, .13f, .90f));
            feedbackText = AddText(feedback, "", new Vector2(.5f, .18f), Vector2.zero, 18, Ivory, 330, FontStyles.Bold);

            var guide = NewGroup("GuideLayer", out guideGroup);
            guideRing = AddPanel(guide, "HandlePulseRing", new Vector2(.5f, .5f), Vector2.zero, new Vector2(86, 86), new Color(1f, .93f, .70f, .55f), ringSprite);
            guideHand = AddPanel(guide, "GhostHand", new Vector2(.5f, .5f), Vector2.zero, new Vector2(54, 70), new Color(1f, .95f, .87f, .95f), handSprite);
            guideArrow = AddPanel(guide, "FlowingArrow", new Vector2(.5f, .5f), Vector2.zero, new Vector2(38, 52), Juice, arrowSprite);
            guideText = AddText(guide, "손잡이를 잡아 레일까지 끌어 놓으시오", new Vector2(.5f, .105f), Vector2.zero, 17, Ivory, 360, FontStyles.Bold);

            var end = NewGroup("EndLayer", out endGroup);
            endShade = AddPanel(end, "ResultRind", new Vector2(.5f, .5f), Vector2.zero, new Vector2(366, 530), new Color(.25f, .05f, .14f, .94f));
            endTitle = AddText(end, "", new Vector2(.5f, .70f), Vector2.zero, 41, Ivory, 340, FontStyles.Bold);
            endStats = AddText(end, "", new Vector2(.5f, .54f), Vector2.zero, 20, Blush, 330, FontStyles.Bold);
            endScore = AddText(end, "0", new Vector2(.5f, .42f), Vector2.zero, 52, Juice, 300, FontStyles.Bold);
            endCta = AddText(end, "석류 코어를 눌러 다시 꽂기", new Vector2(.5f, .28f), Vector2.zero, 18, Ink, 320, FontStyles.Bold);
            AddPanel(end, "RestartHandle", new Vector2(.5f, .28f), Vector2.zero, new Vector2(320, 62), new Color(1f, .94f, .82f, .95f)).transform.SetSiblingIndex(endCta.transform.GetSiblingIndex());
        }

        void Prewarm()
        {
            MgfText.Prewarm("석류코어수선을꽂아씨눈깨워라중기본도형실전초빛길직선에수직으로가장짧은과피레일손잡이끌어놓으시오봉인첫제출점과사이거리발직각입니다고치시오게임오버다시점수성공실패수리완료ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789PHl₀(),·/+-");
        }

        void ShowTitle()
        {
            phase = RunPhase.Title;
            st.phase = "title"; st.score = 0; st.lives = SeongnyuRules.StartLives; st.level = 1; st.solved = 0;
            st.roundIndex = 0; st.firstAttempts = 0; st.firstCorrect = 0; st.firstFailedRounds = 0;
            st.selectedSnap = 99; st.previousSnap = 99; st.toolPose = 0; st.submitted = false; st.problemId = "";
            titleGroup.alpha = 1; titleGroup.blocksRaycasts = false; hudGroup.alpha = 0; feedbackGroup.alpha = 0; guideGroup.alpha = 0; endGroup.alpha = 0;
            boardRoot.gameObject.SetActive(false); titleHandle.gameObject.SetActive(true); titleCore.gameObject.SetActive(true); titleSpiral.gameObject.SetActive(true);
            titleTime = 0; displayedScore = 0; UpdateState();
        }

        void StartPractice()
        {
            phase = RunPhase.Practice; st.phase = "practice"; st.level = 0;
            titleGroup.alpha = 0; hudGroup.alpha = 1; endGroup.alpha = 0; feedbackGroup.alpha = 0;
            titleHandle.gameObject.SetActive(false); titleCore.gameObject.SetActive(false); titleSpiral.gameObject.SetActive(false);
            boardRoot.gameObject.SetActive(true);
            current = SeongnyuRules.Practice(); firstAttemptOnRound = true; idleGuide = 0;
            SetProblem(current, true); ShowGuide(true); UpdateState();
        }

        void StartRunDirect()
        {
            runSerial++; rng = new System.Random(7301 + runSerial * 997);
            phase = RunPhase.Playing; st.phase = "playing"; st.score = 0; st.lives = SeongnyuRules.StartLives; st.level = 1; st.solved = 0;
            st.roundIndex = 0; st.firstAttempts = 0; st.firstCorrect = 0; st.firstFailedRounds = 0;
            st.selectedSnap = 99; st.previousSnap = 99; st.toolPose = 0; st.submitted = false; st.misconceptionId = "";
            firstAttemptOnRound = true; secondsLeft = 90f; displayedScore = 0; chamberOpen = 0; band1Cursor = band2Cursor = band3Cursor = 0;
            titleGroup.alpha = 0; hudGroup.alpha = 1; endGroup.alpha = 0; feedbackGroup.alpha = 0; guideGroup.alpha = 0;
            titleHandle.gameObject.SetActive(false); titleCore.gameObject.SetActive(false); titleSpiral.gameObject.SetActive(false); boardRoot.gameObject.SetActive(true);
            for (int i = 0; i < savedPaths.Length; i++) { savedPaths[i].gameObject.SetActive(false); savedGerms[i].gameObject.SetActive(false); }
            for (int i = 0; i < seals.Length; i++) seals[i].gameObject.SetActive(true);
            // 첫 자동 QA 진입만 중앙의 고정 기하로 실입력 게이트 접근성을 보장한다.
            // 그 뒤 재시작은 25개 정답 눈금을 고르게 바꿔 고정/연타 전략이 학습 진도를 만들지 못한다.
            current = PickOpeningProblem(); SetProblem(current, false); UpdateState();
        }

        CoreProblem PickOpeningProblem()
        {
            if (runSerial == 1) return PickProblem(1, true);
            // 25개 발을 stride 7 순열로 소진한다. 200회에서 각 발은 정확히 8회 등장하며
            // 고정·순환 입력이 생성기 편향으로 우연 수준을 넘는 일을 막는다.
            int n = runSerial - 2;
            int wantedK = ((n * 7 + 19) % 25) - 12;
            var list = bands[1];
            var candidates = new List<CoreProblem>(5);
            for (int i = 0; i < list.Count; i++) if (list[i].k == wantedK) candidates.Add(list[i]);
            return candidates[(runSerial * 3) % candidates.Count];
        }

        CoreProblem PickProblem(int band, bool preferInputGate = false)
        {
            var list = bands[band];
            if (preferInputGate)
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i].k == 0 && Math.Abs(list[i].ux) == 1 && list[i].uy == 0) return list[i];
            }
            int cursor = band == 1 ? band1Cursor++ : (band == 2 ? band2Cursor++ : band3Cursor++);
            return list[(cursor * 37 + rng.Next(list.Count)) % list.Count];
        }

        void SetProblem(CoreProblem p, bool practice)
        {
            current = p; st.problemId = p.id; st.selectedSnap = 99; st.previousSnap = 99; st.submitted = false; st.misconceptionId = "";
            dragging = dragMoved = pointerStartedOnHandle = draggedToRail = false; dragDistance = 0; hoveredSnap = p.band == 3 ? p.wrongSnap : 99;
            scarLine.gameObject.SetActive(false); correctionLine.gameObject.SetActive(false); handleNudge = 0; idleGuide = 0;
            float angle = Mathf.Atan2(p.uy, p.ux);
            railDir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            railPerp = new Vector3(-railDir.y, railDir.x, 0);
            railCenter = new Vector3(0, -1.18f, -.34f);
            float pSide = ((p.h + p.k + p.band) & 1) == 0 ? 1f : -1f;
            if (practice) pSide = 1f;
            pointP = railCenter + railDir * (p.k * TickGap) + railPerp * (1.10f + (p.h - 2) * .10f) * pSide;
            handleHome = pointP + new Vector3(0, .06f, -.16f);
            handle.localPosition = handleHome; germ.localPosition = pointP + new Vector3(0, 0, -.16f);
            railLine.SetPosition(0, railCenter - railDir * 5.65f); railLine.SetPosition(1, railCenter + railDir * 5.65f);
            capillaryLine.SetPosition(0, pointP); capillaryLine.SetPosition(1, pointP);
            for (int i = 0; i < 25; i++)
            {
                int snap = i - 12;
                ticks[i].localPosition = railCenter + railDir * (snap * TickGap) + new Vector3(0, 0, -.05f);
                ticks[i].localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                tickRenderers[i].sharedMaterial = ivoryMat;
            }
            if (p.band == 3)
            {
                Vector3 wrong = SnapWorld(p.wrongSnap);
                scarLine.gameObject.SetActive(true); scarLine.SetPosition(0, pointP); scarLine.SetPosition(1, wrong);
                handle.localPosition = wrong; handleHome = wrong; capillaryLine.SetPosition(1, wrong);
            }
            rulerRoot.localPosition = railCenter - railDir * 3.55f + railPerp * -1.0f;
            rulerRoot.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            promptText.text = practice ? "연습: P(0, 4)에서 직선 l에 수선을 그으시오. 모눈 한 칸은 1 cm이다." : p.prompt;
            feedbackText.text = "";
            guideText.text = practice ? "손잡이를 잡아 레일까지 끌어 놓으시오" : "유백색 손잡이를 끌어 수선의 발을 정하시오";
            UpdateGeometry(); UpdateUi(); UpdateScreenState();
        }

        Vector3 SnapWorld(int snap) => railCenter + railDir * (snap * TickGap) + new Vector3(0, 0, -.08f);

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            titleTime += dt;
            UpdateAmbient(dt);
            UpdateLayout();

            if (phase == RunPhase.Title) { UpdateTitleInput(); UpdatePresentation(dt); return; }
            if (phase == RunPhase.End) { UpdateEndInput(); UpdatePresentation(dt); return; }
            if (hitStop > 0) { hitStop -= dt; UpdatePresentation(dt * .15f); return; }
            if (phase == RunPhase.Reveal)
            {
                revealTime -= dt; UpdateReveal(dt);
                if (revealTime <= 0) FinishReveal();
                UpdatePresentation(dt); return;
            }
            if (phase == RunPhase.Playing)
            {
                secondsLeft -= dt;
                if (secondsLeft <= 0) { secondsLeft = 0; EndRun(false, "과즙 수위가 씨방에 닿았다"); return; }
            }
            UpdateInput();
            idleGuide += dt;
            if (phase == RunPhase.Practice && idleGuide > 8f) ShowGuide(true);
            UpdatePresentation(dt);
            UpdateUi();
        }

        void UpdateTitleInput()
        {
            if (!MgfPointer.Down) return;
            // 타이틀의 주 CTA는 유리 손잡이 하나다. 작은 세로 화면에서 손잡이 그림을 빗나가도
            // 같은 CTA를 활성화해 첫 탭 뒤 350ms 안에 실제 연습 장면으로 들어간다.
            st.inputSerial++; handleNudge = .18f; MgfSfx.Play("tap", .45f); StartPractice();
        }

        void UpdateEndInput()
        {
            if (!MgfPointer.Down) return;
            st.inputSerial++; MgfSfx.Play("tap", .35f); StartRunDirect();
        }

        void UpdateInput()
        {
            if (MgfPointer.Down)
            {
                st.inputSerial++; invalidPulse = .3f; idleGuide = 0;
                dragStartScreen = MgfPointer.Position; dragDistance = 0; dragMoved = false; draggedToRail = false;
                if (MgfPointer.DownHit(cam, out var hit) && (hit.transform == handle || hit.transform.IsChildOf(handle)))
                {
                    dragging = pointerStartedOnHandle = true; handleNudge = .18f; ShowGuide(false); MgfSfx.Play("tap", .34f);
                }
                else
                {
                    pointerStartedOnHandle = false; handleNudge = .42f; ShowGuide(true); feedbackGroup.alpha = 1;
                    feedbackText.text = "유백색 손잡이에서 시작하시오"; MgfSfx.Play("wrong", .12f); MgfBridge.NotifyChanged();
                }
            }
            if (dragging && MgfPointer.Held && ScreenToBoard(out var local))
            {
                dragDistance = Vector2.Distance(dragStartScreen, MgfPointer.Position);
                dragMoved = dragDistance >= 12f;
                float along = Vector3.Dot(local - railCenter, railDir);
                float off = Mathf.Abs(Vector3.Dot(local - railCenter, railPerp));
                int snap = Mathf.Clamp(Mathf.RoundToInt(along / TickGap), SeongnyuRules.MinSnap, SeongnyuRules.MaxSnap);
                hoveredSnap = snap; draggedToRail = off <= .88f && along >= -5.45f && along <= 5.45f;
                handle.localPosition = local + new Vector3(0, 0, -.16f);
                capillaryLine.SetPosition(0, pointP); capillaryLine.SetPosition(1, handle.localPosition);
                st.selectedSnap = snap; st.toolPose = Mathf.RoundToInt(along * 100f); UpdateGuidePath();
            }
            if (dragging && MgfPointer.Up)
            {
                dragging = false;
                if (!dragMoved)
                {
                    handle.localPosition = handleHome; capillaryLine.SetPosition(1, handleHome);
                    feedbackGroup.alpha = 1; feedbackText.text = "끌어서 레일 위 눈금에 놓으시오"; handleNudge = .42f; ShowGuide(true); MgfSfx.Play("wrong", .12f);
                }
                else if (!draggedToRail)
                {
                    handle.localPosition = handleHome; capillaryLine.SetPosition(1, handleHome);
                    feedbackGroup.alpha = 1; feedbackText.text = "직선 l 위에 놓으시오"; invalidPulse = .55f; MgfSfx.Play("wrong", .15f);
                }
                else
                {
                    st.selectedSnap = hoveredSnap; handle.localPosition = SnapWorld(hoveredSnap); capillaryLine.SetPosition(1, handle.localPosition);
                    Submit(hoveredSnap);
                }
                MgfBridge.NotifyChanged();
            }
        }

        bool ScreenToBoard(out Vector3 local)
        {
            var ray = cam.ScreenPointToRay(MgfPointer.Position);
            var plane = new Plane(Vector3.forward, boardRoot.TransformPoint(new Vector3(0, 0, -.20f)));
            if (plane.Raycast(ray, out float d)) { local = boardRoot.InverseTransformPoint(ray.GetPoint(d)); return true; }
            local = default; return false;
        }

        void Submit(int snap)
        {
            if (phase != RunPhase.Practice && phase != RunPhase.Playing) return;
            bool practice = phase == RunPhase.Practice;
            bool correct = SeongnyuRules.IsCorrect(current, snap);
            st.submitted = true; st.previousSnap = snap; revealSnap = snap; revealCorrect = correct; revealFirst = firstAttemptOnRound;
            if (!practice && firstAttemptOnRound)
            {
                st.firstAttempts++;
                if (correct) st.firstCorrect++;
                else { st.firstFailedRounds++; firstAttemptOnRound = false; }
            }
            if (practice && !correct) firstAttemptOnRound = false;
            if (correct)
            {
                int add = practice ? 0 : (revealFirst ? 100 : 40);
                st.score += add; scorePunch = 1f; hitStop = .07f; chamberOpen = 0; beadCount = 12; beadTravel = 0;
                feedbackGroup.alpha = 1;
                feedbackText.text = practice ? "PH는 직선 l에 수직인 수선이다" : (revealFirst ? "수직이다 · 첫 제출 성공" : "수선으로 고쳤다");
                correctionLine.gameObject.SetActive(true); correctionLine.SetPosition(0, pointP); correctionLine.SetPosition(1, SnapWorld(current.k));
                MgfSfx.Play("tap", .45f); MgfSfx.Play("correct", .52f);
                revealTime = practice ? 1.15f : .92f;
            }
            else
            {
                st.misconceptionId = SeongnyuRules.Diagnose(current, snap);
                scarLine.gameObject.SetActive(true); scarLine.SetPosition(0, pointP); scarLine.SetPosition(1, SnapWorld(snap));
                correctionLine.gameObject.SetActive(true); correctionLine.SetPosition(0, pointP); correctionLine.SetPosition(1, SnapWorld(current.k));
                feedbackGroup.alpha = 1;
                feedbackText.text = practice ? "직각 표시를 보고 같은 씨방에서 다시 고치시오" : "PH가 l과 직각을 이루는지 확인하시오";
                wrongShake = .34f; handleNudge = .42f; MgfSfx.Play("wrong", .45f);
                if (!practice)
                {
                    st.lives = Math.Max(0, st.lives - 1);
                    int lost = SeongnyuRules.StartLives - st.lives - 1;
                    if (lost >= 0 && lost < seals.Length) seals[lost].localScale = new Vector3(1.5f, .18f, .4f);
                }
                revealTime = .88f;
            }
            phase = RunPhase.Reveal; st.phase = "reveal"; ShowGuide(false); UpdateState();
        }

        void UpdateReveal(float dt)
        {
            if (revealCorrect)
            {
                chamberOpen = Mathf.Clamp01(chamberOpen + dt * 1.7f);
                beadTravel += dt * 2.2f;
                Vector3 end = SnapWorld(current.k);
                for (int i = 0; i < beadCount; i++)
                {
                    float t = Mathf.Clamp01(beadTravel - i * .055f);
                    juiceBeads[i].gameObject.SetActive(t > 0 && t < 1);
                    juiceBeads[i].localPosition = Vector3.Lerp(end, pointP, SmoothOvershoot(t));
                }
                int chamberIndex = Mathf.Clamp(st.solved, 0, chamberMembranes.Length - 1);
                chamberMembranes[chamberIndex].localRotation = Quaternion.Euler(0, 0, chamberOpen * 82f);
                chamberMembranes[chamberIndex].localScale = new Vector3(1f - chamberOpen * .68f, 1f, 1f);
                chamberGerms[chamberIndex].localPosition = Vector3.Lerp(new Vector3(0, -.06f, -.58f), new Vector3(-.4f, 1.3f, -1.1f), SmoothOvershoot(chamberOpen));
            }
            else
            {
                // 오답은 화면 흔들림이 아니라 카메라의 짧은 충격과 씨눈 몸 접기로만 구분한다.
                float fold = Mathf.Sin((.88f - revealTime) * 18f) * Mathf.Clamp01(revealTime * 3f);
                germ.localScale = new Vector3(.54f + fold * .05f, .54f - Mathf.Abs(fold) * .12f, .54f);
            }
        }

        void FinishReveal()
        {
            for (int i = 0; i < beadCount; i++) juiceBeads[i].gameObject.SetActive(false);
            if (revealCorrect)
            {
                if (current.band == 0)
                {
                    StartRunDirect(); return;
                }
                int save = st.solved;
                if (save < savedPaths.Length)
                {
                    float a = save * Mathf.PI * 2f / 8f;
                    Vector3 outer = new Vector3(Mathf.Cos(a) * 6.0f, Mathf.Sin(a) * 4.1f + .2f, -.9f);
                    Vector3 inner = new Vector3(Mathf.Cos(a) * 1.45f, Mathf.Sin(a) * 1.05f + .2f, -.9f);
                    savedPaths[save].gameObject.SetActive(true); savedPaths[save].SetPosition(0, outer); savedPaths[save].SetPosition(1, inner);
                    savedGerms[save].gameObject.SetActive(true); savedGerms[save].localPosition = inner;
                }
                st.solved++; st.roundIndex = st.solved;
                if (st.solved >= SeongnyuRules.TargetSolved)
                {
                    EndRun(st.firstCorrect >= SeongnyuRules.MasteryTarget,
                        st.firstCorrect >= SeongnyuRules.MasteryTarget ? "여덟 수선이 코어를 깨웠다" : "8개를 열었지만 첫 제출 6/8이 필요하다");
                    return;
                }
                int band = st.solved < 3 ? 1 : (st.solved < 6 ? 2 : 3);
                st.level = band; firstAttemptOnRound = true; current = PickProblem(band); phase = RunPhase.Playing; st.phase = "playing";
                SetProblem(current, false);
            }
            else
            {
                if (phase == RunPhase.Reveal && current.band != 0 && (st.lives <= 0 || st.firstFailedRounds >= 3))
                {
                    EndRun(false, st.lives <= 0 ? "세 격벽 봉인이 모두 갈라졌다" : "첫 제출 6/8에 도달할 수 없다"); return;
                }
                phase = current.band == 0 ? RunPhase.Practice : RunPhase.Playing;
                st.phase = current.band == 0 ? "practice" : "playing";
                handleHome = handle.localPosition; correctionLine.gameObject.SetActive(false); st.submitted = false;
                ShowGuide(current.band == 0); UpdateState();
            }
        }

        void EndRun(bool success, string reason)
        {
            phase = RunPhase.End; st.phase = success ? "clear" : "gameover";
            dragging = false; boardRoot.gameObject.SetActive(false); hudGroup.alpha = 0; feedbackGroup.alpha = 0; guideGroup.alpha = 0; endGroup.alpha = 1;
            titleCore.gameObject.SetActive(true); titleCore.localScale = success ? Vector3.one * 1.25f : Vector3.one * .82f;
            titleSpiral.gameObject.SetActive(success); titleHandle.gameObject.SetActive(false);
            endTitle.text = success ? "코어 각성" : "씨방 봉인";
            endStats.text = reason + "\n개방 " + st.solved + "/8 · 첫 제출 " + st.firstCorrect + "/8 · 봉인 " + st.lives;
            endScore.text = "0"; displayedScore = 0;
            if (success) MgfSfx.Play("win", .6f); else MgfSfx.Play("lose", .52f);
            UpdateState();
        }

        void UpdateAmbient(float dt)
        {
            for (int i = 0; i < chambers.Length; i++)
            {
                float s = 1f + Mathf.Sin(titleTime * (1.15f + i * .015f) + i) * .022f;
                chambers[i].localScale = new Vector3(s, 1f / s, s);
                if (chamberGerms[i] && chamberGerms[i].gameObject.activeSelf)
                    chamberGerms[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(titleTime * 1.4f + i) * 7f);
            }
            for (int i = 0; i < bubbles.Length; i++)
            {
                float y = bubbleHomes[i].y + Mathf.Repeat(titleTime * (.18f + (i % 5) * .035f) + i * .42f, 2.3f);
                bubbles[i].localPosition = new Vector3(bubbleHomes[i].x + Mathf.Sin(titleTime + i) * .08f, y, bubbleHomes[i].z);
            }
            if (titleCore && titleCore.gameObject.activeSelf)
            {
                float s = 1f + Mathf.Sin(titleTime * 2.1f) * .055f; titleCore.localScale = new Vector3(2.2f * s, 1.55f / s, .82f);
                titleSpiral.transform.localRotation = Quaternion.Euler(0, 0, titleTime * 8f);
            }
        }

        void UpdatePresentation(float dt)
        {
            if (handleNudge > 0) handleNudge = Mathf.Max(0, handleNudge - dt);
            if (invalidPulse > 0) invalidPulse = Mathf.Max(0, invalidPulse - dt);
            if (scorePunch > 0) scorePunch = Mathf.Max(0, scorePunch - dt * 2.8f);
            float press = dragging ? .86f : 1f;
            float nudge = handleNudge > 0 ? Mathf.Sin(handleNudge * 46f) * .09f : 0;
            if (handle && handle.gameObject.activeInHierarchy) handle.localScale = new Vector3(1.10f * (press + nudge), 1.10f / Mathf.Max(.75f, press), .66f);
            if (germ && germ.gameObject.activeInHierarchy)
            {
                float breathe = 1f + Mathf.Sin(titleTime * 2.5f) * .035f;
                if (phase != RunPhase.Reveal || revealCorrect) germ.localScale = new Vector3(.54f / breathe, .54f * breathe, .54f);
                germ.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(titleTime * 1.7f) * 4f);
            }
            if (wrongShake > 0)
            {
                wrongShake = Mathf.Max(0, wrongShake - dt);
                cam.transform.position = baseCamPos + new Vector3(Mathf.Sin(wrongShake * 72f) * wrongShake * .13f, 0, 0);
            }
            else cam.transform.position = baseCamPos;
            displayedScore = Mathf.MoveTowards(displayedScore, st.score, dt * 520f);
            if (statusText) statusText.text = Mathf.RoundToInt(displayedScore).ToString("000");
            if (scoreGlow) scoreGlow.color = new Color(1f, .65f, .25f, scorePunch * .36f);
            if (phase == RunPhase.End)
            {
                displayedScore = Mathf.MoveTowards(displayedScore, st.score, dt * 360f);
                endScore.text = Mathf.RoundToInt(displayedScore).ToString("000") + "점";
                float pulse = 1f + Mathf.Sin(titleTime * 3f) * .018f; endShade.rectTransform.localScale = Vector3.one * pulse;
            }
            UpdateGuidePath();
            UpdateScreenState();
        }

        void UpdateGeometry()
        {
            capillaryLine.SetPosition(0, pointP); capillaryLine.SetPosition(1, handle.localPosition);
        }

        void UpdateUi()
        {
            if (!hudGroup || hudGroup.alpha <= 0) return;
            progressText.text = (phase == RunPhase.Practice ? "연습" : "개방 " + st.solved + "/8") + " · 첫 제출 " + st.firstCorrect + "/8";
            statusText.text = Mathf.RoundToInt(displayedScore).ToString("000");
            timerText.text = phase == RunPhase.Practice ? "정지" : Mathf.CeilToInt(secondsLeft).ToString();
            timerFill.rectTransform.sizeDelta = new Vector2(timerBarWidth * (phase == RunPhase.Practice ? 1f : Mathf.Clamp01(secondsLeft / 90f)), 9);
            for (int i = 0; i < seals.Length; i++) seals[i].gameObject.SetActive(i < st.lives);
        }

        void ShowGuide(bool show)
        {
            guideGroup.alpha = show ? 1 : 0;
            if (!show) return;
            UpdateGuidePath();
        }

        void UpdateGuidePath()
        {
            if (!guideGroup || guideGroup.alpha <= 0 || !handle || !cam) return;
            Vector3 start = cam.WorldToScreenPoint(boardRoot.TransformPoint(handleHome));
            Vector3 end = cam.WorldToScreenPoint(boardRoot.TransformPoint(SnapWorld(current != null ? current.k : 0)));
            float scale = MgfText.Canvas.scaleFactor;
            var canvasRect = (RectTransform)MgfText.Canvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, start, null, out var a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, end, null, out var b);
            float t = Mathf.PingPong(titleTime / 1.2f, 1f);
            Vector2 hand = Vector2.Lerp(a, b, SmoothStep(t));
            guideRing.rectTransform.anchoredPosition = a; guideRing.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(titleTime * 5f) * .10f);
            guideHand.rectTransform.anchoredPosition = hand;
            guideArrow.rectTransform.anchoredPosition = Vector2.Lerp(a, b, .82f);
            guideArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2((b - a).y, (b - a).x) * Mathf.Rad2Deg - 90f);
        }

        void UpdateLayout()
        {
            var canvasRect = (RectTransform)MgfText.Canvas.transform;
            float w = canvasRect.rect.width, h = canvasRect.rect.height;
            if (w < 1 || h < 1 || (Mathf.Abs(w - lastLogicalW) < .5f && Mathf.Abs(h - lastLogicalH) < .5f)) return;
            lastLogicalW = w; lastLogicalH = h;
            bool land = (w >= h && w >= 640f) || w >= 1024f;
            if (land)
            {
                cam.orthographicSize = 6.25f;
                timerBarWidth = Mathf.Min(700f, w * .58f);
                SetRect(goalPanel.rectTransform, new Vector2(.5f, .965f), Vector2.zero, new Vector2(Mathf.Min(820, w * .66f), 36));
                SetRect(goalText.rectTransform, new Vector2(.5f, .965f), Vector2.zero, new Vector2(Mathf.Min(800, w * .64f), 34));
                SetRect(promptPanel.rectTransform, new Vector2(.5f, .835f), Vector2.zero, new Vector2(Mathf.Min(930, w * .74f), 72));
                SetRect(promptText.rectTransform, new Vector2(.5f, .835f), Vector2.zero, new Vector2(Mathf.Min(890, w * .70f), 66));
                SetRect(statusText.rectTransform, new Vector2(.17f, .715f), Vector2.zero, new Vector2(150, 40));
                SetRect(progressText.rectTransform, new Vector2(.5f, .715f), Vector2.zero, new Vector2(300, 40));
                SetRect(timerText.rectTransform, new Vector2(.83f, .715f), Vector2.zero, new Vector2(100, 40));
                SetRect(timerTrack.rectTransform, new Vector2(.5f, .68f), Vector2.zero, new Vector2(timerBarWidth, 13));
                logoText.text = "석류 코어"; logoText.fontSize = 64; logoText.lineSpacing = 0;
                SetRect(logoText.rectTransform, new Vector2(.5f, .73f), Vector2.zero, new Vector2(Mathf.Min(820, w * .66f), 120));
                SetRect(tagText.rectTransform, new Vector2(.5f, .59f), Vector2.zero, new Vector2(600, 58));
                titleCta.fontSize = 21;
            }
            else
            {
                cam.orthographicSize = 9.5f;
                timerBarWidth = 350f;
                SetRect(goalPanel.rectTransform, new Vector2(.5f, .955f), Vector2.zero, new Vector2(366, 47));
                SetRect(goalText.rectTransform, new Vector2(.5f, .955f), Vector2.zero, new Vector2(350, 42));
                SetRect(promptPanel.rectTransform, new Vector2(.5f, .855f), Vector2.zero, new Vector2(366, 96));
                SetRect(promptText.rectTransform, new Vector2(.5f, .855f), Vector2.zero, new Vector2(344, 92));
                SetRect(statusText.rectTransform, new Vector2(.19f, .748f), Vector2.zero, new Vector2(140, 38));
                SetRect(progressText.rectTransform, new Vector2(.52f, .748f), Vector2.zero, new Vector2(150, 38));
                SetRect(timerText.rectTransform, new Vector2(.86f, .748f), Vector2.zero, new Vector2(80, 40));
                SetRect(timerTrack.rectTransform, new Vector2(.5f, .705f), Vector2.zero, new Vector2(350, 13));
                logoText.text = "석류\n코어"; logoText.fontSize = 58; logoText.lineSpacing = -22;
                SetRect(logoText.rectTransform, new Vector2(.5f, .78f), Vector2.zero, new Vector2(330, 145));
                SetRect(tagText.rectTransform, new Vector2(.5f, .64f), Vector2.zero, new Vector2(350, 50));
                titleCta.fontSize = 19;
            }
            baseCamPos = new Vector3(0, land ? 3.5f : 4.8f, -19.5f);
            cam.transform.position = baseCamPos; cam.transform.LookAt(new Vector3(0, land ? .25f : .55f, 0));
        }

        void UpdateState()
        {
            st.phase = phase == RunPhase.Title ? "title" : phase == RunPhase.Practice ? "practice" : phase == RunPhase.Playing ? "playing" : phase == RunPhase.Reveal ? "reveal" : st.phase;
            MgfBridge.NotifyChanged();
        }

        void UpdateScreenState()
        {
            if (!cam || !boardRoot || !handle) return;
            Vector3 hs = cam.WorldToScreenPoint(boardRoot.TransformPoint(handle.localPosition));
            Vector3 rs = cam.WorldToScreenPoint(boardRoot.TransformPoint(railCenter - railDir * (12 * TickGap)));
            Vector3 re = cam.WorldToScreenPoint(boardRoot.TransformPoint(railCenter + railDir * (12 * TickGap)));
            st.handleScreenX = Mathf.RoundToInt(hs.x);
            st.handleScreenY = Mathf.RoundToInt(Screen.height - hs.y);
            st.railStartScreenX = Mathf.RoundToInt(rs.x);
            st.railStartScreenY = Mathf.RoundToInt(Screen.height - rs.y);
            st.railEndScreenX = Mathf.RoundToInt(re.x);
            st.railEndScreenY = Mathf.RoundToInt(Screen.height - re.y);
        }

        static float SmoothStep(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
        static float SmoothOvershoot(float t)
        {
            t = Mathf.Clamp01(t); float s = 1.70158f; t -= 1f; return t * t * ((s + 1f) * t + s) + 1f;
        }

        LineRenderer MakeLine(string name, Material mat, float width, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = mat; lr.widthMultiplier = width;
            lr.numCapVertices = 6; lr.numCornerVertices = 4; lr.useWorldSpace = false; lr.alignment = LineAlignment.View;
            return lr;
        }

        RectTransform NewGroup(string name, out CanvasGroup group)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(MgfText.Canvas.transform, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            group = go.AddComponent<CanvasGroup>(); return rt;
        }

        TextMeshProUGUI AddText(RectTransform parent, string text, Vector2 anchor, Vector2 offset, float size, Color color, float width, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; SetRect(rt, anchor, offset, new Vector2(width, size * 2.5f));
            var t = go.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = style; t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false; t.text = text; return t;
        }

        Image AddPanel(RectTransform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; SetRect(rt, anchor, offset, size);
            var img = go.AddComponent<Image>(); img.sprite = sprite ? sprite : panelSprite; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false; return img;
        }

        static void SetRect(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = offset; rt.sizeDelta = size;
        }

        Sprite MakeRoundedSprite(int size, float radius, Color color, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "GuideRing" : "SoftPanel" };
            var px = new Color32[size * size]; float c = (size - 1) * .5f;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - c) - (c - radius), dy = Mathf.Abs(y - c) - (c - radius);
                float outside = Mathf.Sqrt(Mathf.Max(dx, 0) * Mathf.Max(dx, 0) + Mathf.Max(dy, 0) * Mathf.Max(dy, 0)) + Mathf.Min(Mathf.Max(dx, dy), 0) - radius;
                float a = Mathf.Clamp01(.5f - outside);
                if (ring)
                {
                    float dist = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    a = dist > c - 5 && dist < c - 1 ? 1f : 0f;
                }
                px[y * size + x] = new Color(color.r, color.g, color.b, color.a * a);
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        Sprite MakeArrowSprite()
        {
            const int w = 48, h = 64; var tex = new Texture2D(w, h, TextureFormat.RGBA32, false); var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                bool shaft = x > 19 && x < 29 && y < 44; bool head = y >= 28 && Mathf.Abs(x - 24) < (h - y) * .72f;
                px[y * w + x] = (shaft || head) ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
            tex.SetPixels32(px); tex.Apply(false, true); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100);
        }

        Sprite MakeHandSprite()
        {
            const int s = 64; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false); var px = new Color32[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float dx = (x - 31f) / 17f, dy = (y - 25f) / 19f; bool palm = dx * dx + dy * dy < 1f;
                bool finger = x > 27 && x < 37 && y >= 25 && y < 61;
                px[y * s + x] = (palm || finger) ? new Color32(255, 245, 224, 255) : new Color32(0, 0, 0, 0);
            }
            tex.SetPixels32(px); tex.Apply(false, true); return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100);
        }

        public void TestStart() => StartRunDirect();

        public void TestAnswerCorrect()
        {
            if (phase == RunPhase.Title || phase == RunPhase.End || phase == RunPhase.Practice) StartRunDirect();
            if (phase == RunPhase.Reveal) { revealTime = 0; FinishReveal(); if (phase == RunPhase.End) StartRunDirect(); }
            if (phase != RunPhase.Playing) return;
            st.selectedSnap = current.k; handle.localPosition = SnapWorld(current.k); capillaryLine.SetPosition(1, handle.localPosition); Submit(current.k);
        }

        public void TestAnswerWrong()
        {
            if (phase == RunPhase.Title || phase == RunPhase.End || phase == RunPhase.Practice) StartRunDirect();
            if (phase == RunPhase.Reveal) { revealTime = 0; FinishReveal(); if (phase == RunPhase.End) StartRunDirect(); }
            if (phase != RunPhase.Playing) return;
            int wrong = current.k == SeongnyuRules.MaxSnap ? current.k - 1 : current.k + 1;
            st.selectedSnap = wrong; handle.localPosition = SnapWorld(wrong); capillaryLine.SetPosition(1, handle.localPosition); Submit(wrong);
        }

        public string StateJson() => JsonUtility.ToJson(st);

        public string ProblemBankJson()
        {
            var list = new List<MgfProblem>(bank.Count);
            for (int i = 0; i < bank.Count; i++) list.Add(SeongnyuRules.ToMgf(bank[i]));
            return MgfJson.Bank(list);
        }
    }
}
