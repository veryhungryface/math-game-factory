// 빙막 봉합 — 빙벽 측량 캠프 2.5D 세계, 자유 꼭짓점 F 직접 드래그, 진행/실패/브리지 정본.
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.BingmakBonghap
{
    public sealed class BingmakBonghapGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int round;
            public int firstAttemptCorrect;
            public int firstAttemptTotal;
            public int attemptCount;
            public int combo;
            public int inputSerial;
            public int submissionSerial;
            public int invalidCount;
            public int fx100;
            public int fy100;
            public int df100;
            public int ef100;
            public int layoutVersion;
            public string currentProblemId = "";
            public string tutorialStep = "title";
            public string misconceptionId = "";
            public string lastResult = "";
            public bool firstAttemptUsed;
            public bool frozen;
            public bool dragActive;
        }

        enum Flow { Title, Practice, Playing, Reveal, End }

        static readonly Color Aqua = MgfLook.Hex("#A8D7DE");
        static readonly Color Deep = MgfLook.Hex("#173047");
        static readonly Color Orange = MgfLook.Hex("#F3A33B");
        static readonly Color Graphite = MgfLook.Hex("#222A33");
        static readonly Color Cream = MgfLook.Hex("#F5E8C8");
        static readonly Color Cobalt = MgfLook.Hex("#315C72");
        static readonly Color ClearBlue = MgfLook.Hex("#73C7D2");

        readonly State st = new State();
        readonly List<IceProblem> bank = new List<IceProblem>(360);
        readonly List<IceProblem> band1 = new List<IceProblem>(120);
        readonly List<IceProblem> band2 = new List<IceProblem>(120);
        readonly List<IceProblem> band3 = new List<IceProblem>(120);
        readonly List<IceProblem> deck = new List<IceProblem>(8);
        readonly Image[] clipIcons = new Image[3];
        readonly Image[] mapWindows = new Image[8];
        readonly Transform[] caustics = new Transform[5];

        Camera cam;
        Transform world, sealRoot, sealBody, sealHead, sealFlipper, clearWindowWorld;
        Material iceMat, darkIceMat, orangeMat, graphiteMat, creamMat, cobaltMat, clearMat, glowMat;
        Canvas canvas;
        RectTransform canvasRt;
        CanvasGroup titleGroup, playGroup, revealGroup, endGroup, lensGroup;
        RectTransform titleBackdrop, titleStart, titleHelp, titleLogoRoot;
        RectTransform hudPanel, goalPanel, referencePanel, workPanel, progressPanel, toastPanel;
        RectTransform revealPanel, endPanel, endButton, lensPanel;
        RawImage titleArt;
        TextMeshProUGUI titleLogoShadow, titleLogo, titleTag, titleMeta, titleBest, titleStartText, titleHelpText;
        TextMeshProUGUI hudText, goalText, congruenceText, sourceSummaryText, promptText, progressText, scoreText, toastText;
        TextMeshProUGUI labelD, labelE, labelF, measureDF, measureEF, toleranceText;
        TextMeshProUGUI revealTitle, revealBody, endTitle, endStats, endScore, endButtonText, lensText;
        ReferenceTriangleGraphic referenceGraphic;
        MembraneGraphic membrane;
        LensGraphic lensGraphic;
        Sprite roundedSprite, circleSprite, ringSprite;

        readonly System.Random rng = new System.Random(20261025);
        Flow flow = Flow.Title;
        IceProblem current;
        Vector2 currentF, dragOffsetModel, dragStartModel;
        Vector2 workBasePosition;
        float secondsLeft = BingmakRules.RunSeconds;
        float titleClock, ambientClock, idleClock, revealClock, hitStop, wrongJolt, unfoldClock;
        float sealEventClock = 99f, toastClock = 99f, lensClock, scoreShown, endShown;
        float lastTrailTime;
        int lastTimerSecond = -1, lastDf = -1, lastEf = -1, lockSoundStep;
        bool revealCorrect, revealPractice, failAfterReveal, dragMoved;
        float lastCanvasW = -1, lastCanvasH = -1;

        void Awake()
        {
            MgfLook.Quality(32f);
            // Runtime primitive collider types survive IL2CPP stripping.
            if (Time.frameCount < 0)
            {
                gameObject.AddComponent<BoxCollider>(); gameObject.AddComponent<SphereCollider>();
                gameObject.AddComponent<CapsuleCollider>(); gameObject.AddComponent<MeshCollider>();
            }
            bank.AddRange(BingmakRules.BuildBank());
            for (int i = 0; i < bank.Count; i++)
            {
                if (bank[i].band == 1) band1.Add(bank[i]);
                else if (bank[i].band == 2) band2.Add(bank[i]);
                else band3.Add(bank[i]);
            }
            BuildMaterials(); BuildWorld(); BuildUi(); Prewarm(); ShowTitle();
            MgfBridge.Register(this);
        }

        void BuildMaterials()
        {
            iceMat = MgfLook.Lit(Aqua, .82f, .04f, new Color(.015f, .055f, .06f));
            darkIceMat = MgfLook.Lit(Deep, .70f, .10f, new Color(0, .015f, .028f));
            orangeMat = MgfLook.Lit(Orange, .72f, .18f, new Color(.06f, .018f, 0));
            graphiteMat = MgfLook.Lit(Graphite, .80f, .60f);
            creamMat = MgfLook.Lit(Cream, .62f, .02f);
            cobaltMat = MgfLook.Lit(Cobalt, .74f, .18f, new Color(0, .022f, .045f));
            clearMat = MgfLook.Lit(MgfLook.Hex("#45677A"), .36f, .18f, new Color(.01f, .025f, .035f));
            glowMat = MgfLook.Additive(new Color(1f, .57f, .18f, .52f), MgfLook.SoftDot);
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#101C2D"), MgfLook.Hex("#456A82"), MgfLook.Hex("#0A111D"), .74f);
            MgfLook.Sun(new Vector3(43, -31, -22), MgfLook.Hex("#FFE1A2"), 1.08f, .72f);
            cam = MgfLook.Camera(new Vector3(0, 2.6f, -19f), new Vector3(0, .7f, 4f), 31f);
            cam.orthographic = true; cam.orthographicSize = 8.4f;
            world = new GameObject("FrostSurveyCamp").transform;

            // 불투명 빙벽과 황동 피톤. 투명 수중 플라스틱 대신 층진 절벽 디오라마를 만든다.
            for (int i = 0; i < 11; i++)
            {
                float x = (i - 5) * 2.9f;
                float y = 2.5f + (i % 4) * .62f;
                var slab = MgfLook.Block("FacetedIceWall" + i, new Vector3(x, y, 6.1f + (i % 3) * .55f),
                    new Vector3(3.15f, 7.8f - (i % 3) * .45f, 1.35f), .12f, i % 3 == 0 ? darkIceMat : iceMat, world);
                slab.transform.rotation = Quaternion.Euler(2 + i % 2 * 4, (i - 5) * 1.4f, (i % 3 - 1) * 3.2f);
            }
            MgfLook.Block("SurveyDeck", new Vector3(0, -3.75f, 4.35f), new Vector3(19f, .42f, 2.2f), .08f, graphiteMat, world);
            MgfLook.Block("AmberTraverseRail", new Vector3(0, -3.26f, 3.9f), new Vector3(18f, .14f, .18f), .04f, orangeMat, world);
            for (int i = 0; i < caustics.Length; i++)
            {
                var c = MgfLook.Prim(PrimitiveType.Cube, "SurveyBeam" + i, new Vector3(-8 + i * 4f, -1.1f + (i % 2) * 1.15f, 3.7f),
                    new Vector3(2.7f, .045f, .045f), glowMat, world, false);
                c.transform.rotation = Quaternion.Euler(0, 0, -16 + i * 8);
                caustics[i] = c.transform;
            }
            BuildSurveyRig();
            clearWindowWorld = new GameObject("LockedCableReward").transform;
            clearWindowWorld.SetParent(world, false); clearWindowWorld.localPosition = new Vector3(0, 3.9f, 4.35f);
            BuildWorldTriangle(clearWindowWorld, 2.7f, clearMat, orangeMat);
            clearWindowWorld.localScale = Vector3.zero;
        }

        void BuildSurveyRig()
        {
            sealRoot = new GameObject("SurveySled").transform; sealRoot.SetParent(world, false);
            sealRoot.localPosition = new Vector3(4.8f, -3.05f, 3.55f); sealRoot.localRotation = Quaternion.Euler(0, -9, 0);
            Material steel = MgfLook.Lit(MgfLook.Hex("#53616D"), .30f, .62f);
            Material brass = MgfLook.Lit(MgfLook.Hex("#C9812F"), .48f, .56f, new Color(.05f, .018f, 0));
            sealBody = MgfLook.Block("SledChassis", Vector3.zero, new Vector3(2.15f, .56f, 1.12f), .08f, steel, sealRoot).transform;
            sealHead = MgfLook.Block("SurveyCabin", new Vector3(-.35f, .72f, 0), new Vector3(.92f, .82f, .78f), .08f, creamMat, sealRoot).transform;
            MgfLook.Prim(PrimitiveType.Cylinder, "Beacon", new Vector3(-.35f, 1.28f, 0), new Vector3(.16f, .28f, .16f), orangeMat, sealRoot, false);
            for (int i = 0; i < 4; i++)
            {
                float x = i < 2 ? -.72f : .72f, z = (i & 1) == 0 ? -.62f : .62f;
                var wheel = MgfLook.Prim(PrimitiveType.Cylinder, "TrackWheel" + i, new Vector3(x, -.38f, z),
                    new Vector3(.34f, .16f, .34f), graphiteMat, sealRoot, false);
                wheel.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            sealFlipper = MgfLook.Prim(PrimitiveType.Cube, "SurveyArm", new Vector3(-1.05f, .72f, 0),
                new Vector3(.12f, .82f, .12f), brass, sealRoot, false).transform;
            sealFlipper.localRotation = Quaternion.Euler(0, 0, 52);
            var pad = MgfLook.Prim(PrimitiveType.Cylinder, "PitOnDriver", new Vector3(-1.35f, 1.05f, 0),
                new Vector3(.26f, .18f, .26f), orangeMat, sealRoot, false);
            pad.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        void BuildWorldTriangle(Transform parent, float size, Material fill, Material edge)
        {
            var mesh = new Mesh { name = "TriangularCablePatch" };
            mesh.vertices = new[] { new Vector3(-size, -size * .55f, 0), new Vector3(size, -size * .55f, 0), new Vector3(0, size, 0) };
            mesh.triangles = new[] { 0, 1, 2 }; mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back };
            var go = new GameObject("FrostSurveyPlate"); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = fill;
            Vector3[] pts = mesh.vertices;
            for (int i = 0; i < 3; i++)
            {
                Vector3 a = pts[i], b = pts[(i + 1) % 3], mid = (a + b) * .5f; float len = Vector3.Distance(a, b);
                var bar = MgfLook.Prim(PrimitiveType.Cube, "LockedCable", mid + Vector3.back * .04f,
                    new Vector3(len, .12f, .10f), edge, parent, false);
                bar.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            }
        }

        void BuildUi()
        {
            // 좁은 모서리의 금속 계측판. 둥근 반투명 플라스틱 카드 문법을 쓰지 않는다.
            roundedSprite = MakeRoundedSprite(64, 4, false, false);
            circleSprite = MakeRoundedSprite(64, 31, false, false);
            ringSprite = MakeRoundedSprite(64, 31, true, false);
            canvas = MgfText.Canvas; canvasRt = (RectTransform)canvas.transform;

            BuildTitleUi(); BuildPlayUi(); BuildRevealUi(); BuildEndUi();
        }

        void BuildTitleUi()
        {
            titleGroup = FullGroup("TitleScreen", canvas.transform);
            titleBackdrop = R("TitleBackdrop", titleGroup.transform, Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(titleBackdrop, 0);
            titleArt = titleBackdrop.gameObject.AddComponent<RawImage>(); titleArt.raycastTarget = false;
            // 타이틀도 인게임과 같은 실시간 빙벽 캠프다. 생성 원화는 더 이상 렌더하지 않는다.
            titleArt.enabled = false;

            var topGlow = Img(titleGroup.transform, "IceGlow", new Vector2(.5f, 1), new Vector2(0, -96), new Vector2(660, 250),
                new Color(.84f, .98f, .98f, .33f), circleSprite); topGlow.raycastTarget = false;
            titleLogoRoot = Panel(titleGroup.transform, "CliffSign", new Vector2(.5f, 1), new Vector2(0, -180), new Vector2(380, 170), new Color(.07f, .12f, .18f, .88f), roundedSprite);
            titleLogoShadow = Txt(titleLogoRoot, "빙막\n봉합", new Vector2(.5f, .5f), new Vector2(3, -4), 61, new Color(.95f, .76f, .38f, .55f), 360, TextAlignmentOptions.Center);
            titleLogoShadow.outlineWidth = .30f; titleLogoShadow.outlineColor = new Color(.03f, .15f, .23f, .95f);
            titleLogo = Txt(titleLogoRoot, "빙막\n봉합", new Vector2(.5f, .5f), Vector2.zero, 61, Cream, 360, TextAlignmentOptions.Center);
            titleLogo.outlineWidth = .16f; titleLogo.outlineColor = Graphite;
            titleTag = Txt(titleGroup.transform, "황동 피톤으로 세 변을 잠가라", new Vector2(.5f, .64f), Vector2.zero, 17, Cream, 380);
            titleTag.outlineWidth = .16f; titleTag.outlineColor = Deep;
            titleMeta = Txt(titleGroup.transform, "중학교 1학년 · 작도와 합동", new Vector2(.5f, .18f), new Vector2(0, 24), 13, Cream, 340);
            titleMeta.outlineWidth = .18f; titleMeta.outlineColor = Deep;
            titleBest = Txt(titleGroup.transform, "", new Vector2(.5f, .18f), new Vector2(0, 52), 13, Cream, 340);
            titleBest.outlineWidth = .18f; titleBest.outlineColor = Deep;

            titleStart = Panel(titleGroup.transform, "BeaconStart", new Vector2(.5f, 0), new Vector2(0, 92), new Vector2(310, 68),
                new Color(.10f, .14f, .18f, .97f), roundedSprite);
            Img(titleStart, "AmberBeaconLine", new Vector2(.5f, .5f), new Vector2(0, -21), new Vector2(286, 5), Orange, roundedSprite);
            titleStartText = Txt(titleStart, "신호탄을 눌러 측량 시작", new Vector2(.5f, .5f), Vector2.zero, 19, Cream, 284);
            titleHelp = R("HowTo", titleGroup.transform, new Vector2(.5f, 0), new Vector2(0, 33), new Vector2(190, 44));
            titleHelpText = Txt(titleHelp, "조작 다시 보기", new Vector2(.5f, .5f), Vector2.zero, 13, Cream, 180);
            titleHelpText.outlineWidth = .12f; titleHelpText.outlineColor = Deep;
        }

        void BuildPlayUi()
        {
            playGroup = FullGroup("PlayScreen", canvas.transform);
            hudPanel = Panel(playGroup.transform, "SurveyHeader", new Vector2(.5f, .5f), Vector2.zero, new Vector2(370, 50), new Color(.07f, .11f, .16f, .98f));
            hudText = Txt(hudPanel, "", new Vector2(.5f, .5f), new Vector2(-18, 0), 15, Cream, 168);
            // 우상단 HTML 음소거 버튼과 겹치지 않는 헤더 안쪽에 둔다.
            scoreText = Txt(hudPanel, "0점", new Vector2(1, .5f), new Vector2(-92, 0), 18, Cream, 72, TextAlignmentOptions.Right);
            goalPanel = Panel(playGroup.transform, "GoalEtching", new Vector2(.5f, .5f), Vector2.zero, new Vector2(370, 40), new Color(.91f, .61f, .24f, .96f));
            goalText = Txt(goalPanel, "보강선의 세 변을 맞춰 균열을 고정하라", new Vector2(.5f, .5f), Vector2.zero, 15, Graphite, 356);

            referencePanel = Panel(playGroup.transform, "ReferenceSchematic", new Vector2(.5f, .5f), Vector2.zero, new Vector2(364, 126), new Color(.96f, .91f, .78f, .98f));
            congruenceText = Txt(referencePanel, "", new Vector2(.5f, 1), new Vector2(0, -17), 18, Deep, 340);
            promptText = Txt(referencePanel, "", new Vector2(.5f, 1), new Vector2(0, -43), 12.5f, Graphite, 340);
            var rgRt = R("ReferenceTriangle", referencePanel, new Vector2(.5f, .5f), new Vector2(0, -13), new Vector2(182, 72));
            referenceGraphic = rgRt.gameObject.AddComponent<ReferenceTriangleGraphic>(); referenceGraphic.raycastTarget = false;
            sourceSummaryText = Txt(referencePanel, "", new Vector2(.5f, 0), new Vector2(0, 13), 13.5f, Deep, 350);

            workPanel = Panel(playGroup.transform, "MeasurementCliff", new Vector2(.5f, .5f), Vector2.zero, new Vector2(366, 500), new Color(.08f, .17f, .23f, .97f));
            var memRt = R("MembraneSurface", workPanel, new Vector2(.5f, .5f), Vector2.zero, new Vector2(354, 488));
            membrane = memRt.gameObject.AddComponent<MembraneGraphic>(); membrane.raycastTarget = false;
            labelD = Txt(playGroup.transform, "D", new Vector2(.5f, .5f), Vector2.zero, 19, Cream, 44);
            labelE = Txt(playGroup.transform, "E", new Vector2(.5f, .5f), Vector2.zero, 19, Cream, 44);
            labelF = Txt(playGroup.transform, "F", new Vector2(.5f, .5f), Vector2.zero, 21, Cream, 44);
            labelF.outlineWidth = .22f; labelF.outlineColor = Graphite;
            measureDF = Txt(playGroup.transform, "", new Vector2(.5f, .5f), Vector2.zero, 17, Cream, 120);
            measureDF.outlineWidth = .10f; measureDF.outlineColor = Cream;
            measureEF = Txt(playGroup.transform, "", new Vector2(.5f, .5f), Vector2.zero, 17, Cream, 120);
            measureEF.outlineWidth = .10f; measureEF.outlineColor = Cream;
            measureDF.textWrappingMode = TextWrappingModes.NoWrap;
            measureEF.textWrappingMode = TextWrappingModes.NoWrap;
            toleranceText = Txt(workPanel, "0.5 cm 모눈 · 제작 허용오차 0.25 cm", new Vector2(.5f, 0), new Vector2(0, 13), 11.5f, Cream, 320);

            progressPanel = Panel(playGroup.transform, "PitOnLedger", new Vector2(.5f, .5f), Vector2.zero, new Vector2(370, 54), new Color(.07f, .11f, .16f, .98f));
            for (int i = 0; i < 8; i++)
            {
                mapWindows[i] = Img(progressPanel, "Window" + (i + 1), new Vector2(.5f, .5f), new Vector2((i - 3.5f) * 35f, 7),
                    new Vector2(25, 18), new Color(.35f, .53f, .57f, .60f), roundedSprite);
            }
            progressText = Txt(progressPanel, "", new Vector2(.5f, 0), new Vector2(0, 8), 11, Cream, 340);
            for (int i = 0; i < 3; i++)
                clipIcons[i] = Img(hudPanel, "RepairClip" + i, new Vector2(0, .5f), new Vector2(22 + i * 24, 0), new Vector2(16, 30), Graphite, roundedSprite);

            toastPanel = Panel(playGroup.transform, "SurveyMessage", new Vector2(.5f, .5f), new Vector2(0, 0), new Vector2(340, 50), new Color(.12f, .16f, .19f, .98f));
            toastText = Txt(toastPanel, "", new Vector2(.5f, .5f), Vector2.zero, 14, Cream, 320);
            toastPanel.gameObject.SetActive(false);

            lensGroup = FullGroup("FourTimesLens", playGroup.transform);
            // LensGraphic 자체가 Graphic이다. Image를 먼저 붙이는 Panel()을 쓰면 Unity가 두 번째
            // Graphic 추가를 거부하므로 빈 RectTransform 위에 직접 만든다.
            lensPanel = R("Lens", lensGroup.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(112, 112));
            lensGraphic = lensPanel.gameObject.AddComponent<LensGraphic>(); lensGraphic.raycastTarget = false;
            lensText = Txt(lensPanel, "4×", new Vector2(.5f, 0), new Vector2(0, 14), 12, Deep, 86);
        }

        void BuildRevealUi()
        {
            revealGroup = FullGroup("Reveal", canvas.transform);
            revealPanel = Panel(revealGroup.transform, "PressureReadout", new Vector2(.5f, .5f), Vector2.zero, new Vector2(354, 166), new Color(.95f, .97f, .89f, .97f));
            Img(revealPanel, "ReadoutRule", new Vector2(.5f, 1), new Vector2(0, -7), new Vector2(320, 7), Orange, roundedSprite);
            revealTitle = Txt(revealPanel, "", new Vector2(.5f, 1), new Vector2(0, -42), 23, Deep, 330);
            revealBody = Txt(revealPanel, "", new Vector2(.5f, .5f), new Vector2(0, -20), 15, Graphite, 326);
        }

        void BuildEndUi()
        {
            endGroup = FullGroup("ResultScreen", canvas.transform);
            endPanel = Panel(endGroup.transform, "ResultIceWindow", new Vector2(.5f, .5f), Vector2.zero, new Vector2(360, 460), new Color(.82f, .96f, .94f, .94f));
            Img(endPanel, "ResultAurora", new Vector2(.5f, 1), new Vector2(0, -67), new Vector2(320, 104), new Color(.14f, .55f, .69f, .78f), roundedSprite);
            endTitle = Txt(endPanel, "", new Vector2(.5f, 1), new Vector2(0, -72), 35, Cream, 330);
            endTitle.outlineWidth = .22f; endTitle.outlineColor = Deep;
            endScore = Txt(endPanel, "0", new Vector2(.5f, .5f), new Vector2(0, 48), 56, Deep, 320);
            endStats = Txt(endPanel, "", new Vector2(.5f, .5f), new Vector2(0, -48), 18, Graphite, 330);
            endButton = Panel(endPanel, "RetrySuction", new Vector2(.5f, 0), new Vector2(0, 62), new Vector2(300, 72), Graphite, circleSprite);
            Img(endButton, "RetryRing", new Vector2(.5f, .5f), Vector2.zero, new Vector2(272, 50), Orange, ringSprite);
            endButtonText = Txt(endButton, "새 빙막 펼치기", new Vector2(.5f, .5f), Vector2.zero, 20, Cream, 260);
        }

        void Prewarm()
        {
            MgfText.Prewarm("빙막봉합황동피톤세변을잠가라중학교학년작도와합동신호탄측량시작조작다시보기보강선균열고정빛나는화살표방향끌어놓으시오모식도표시된반평면에서옮기시오현재약모눈제작허용오차수선클립첫제출정답초과부족대응변뒤바꿈한길이만확인SSS합동완료실패새빙막펼치기점수시간진행DFEFABCcm△≡·0123456789.-/");
        }

        void ShowTitle()
        {
            flow = Flow.Title; st.phase = "title"; st.tutorialStep = "title"; st.frozen = true; st.dragActive = false;
            SetGroup(titleGroup, true); SetGroup(playGroup, false); SetGroup(revealGroup, false); SetGroup(endGroup, false); SetGroup(lensGroup, false);
            titleClock = 0; titleLogoRoot.localScale = Vector3.one * .72f; titleStart.localScale = Vector3.one;
            int best = PlayerPrefs.GetInt("bingmak-bonghap.best", 0);
            titleBest.text = best > 0 ? "최고 기록 " + best.ToString("N0") + "점" : "";
            cam.orthographicSize = 8.4f; Notify();
        }

        void BeginPractice()
        {
            ResetRunState(); flow = Flow.Practice; current = BingmakRules.Practice(); currentF = new Vector2(7.1f, 2.1f);
            st.currentProblemId = current.id; st.tutorialStep = "drag_F"; st.frozen = true; st.phase = "playing";
            SetGroup(titleGroup, false); SetGroup(playGroup, true); SetGroup(revealGroup, false); SetGroup(endGroup, false); SetGroup(lensGroup, false);
            ConfigureProblem(); membrane.PracticeGuide = true; membrane.GuideScale = 1f;
            UpdateMeasures(true);
            goalText.text = "빛나는 F를 잡아 화살표 방향으로 끌어 놓으시오";
            unfoldClock = 0; idleClock = 0; MgfSfx.Play("whoosh", .38f); Notify();
        }

        void ResetRunState()
        {
            st.score = 0; st.lives = BingmakRules.StartLives; st.level = 1; st.solved = 0; st.round = 0;
            st.firstAttemptCorrect = 0; st.firstAttemptTotal = 0; st.attemptCount = 0; st.combo = 0;
            st.firstAttemptUsed = false; st.misconceptionId = ""; st.lastResult = "";
            secondsLeft = BingmakRules.RunSeconds; lastTimerSecond = -1; scoreShown = 0;
            for (int i = 0; i < 8; i++) mapWindows[i].color = new Color(.35f, .53f, .57f, .60f);
            UpdateClips();
        }

        void BeginRun(bool skipPractice)
        {
            ResetRunState(); BuildDeck(); flow = Flow.Playing; st.phase = "playing"; st.frozen = false; st.tutorialStep = skipPractice ? "real" : "after_practice";
            SetGroup(titleGroup, false); SetGroup(playGroup, true); SetGroup(revealGroup, false); SetGroup(endGroup, false); SetGroup(lensGroup, false);
            st.round = 1; SelectProblem(0); goalText.text = "세 변의 대응을 읽고 F를 직접 옮기시오";
            unfoldClock = 0; Notify();
        }

        void BuildDeck()
        {
            deck.Clear(); var used = new HashSet<string>();
            TakeBand(band1, 3, used); TakeBand(band2, 3, used); TakeBand(band3, 2, used);
            if (deck.Count != 8) throw new InvalidOperationException("run deck must contain 8");
        }

        void TakeBand(List<IceProblem> source, int count, HashSet<string> used)
        {
            int start = rng.Next(source.Count);
            for (int step = 0; step < source.Count && count > 0; step++)
            {
                var p = source[(start + step * 37) % source.Count];
                if (!used.Add(p.geometryKey)) continue;
                deck.Add(p); count--;
            }
        }

        void SelectProblem(int index)
        {
            current = deck[index]; currentF = BingmakRules.InitialPoint(current);
            st.currentProblemId = current.id; st.level = current.band; st.round = index + 1;
            st.attemptCount = 0; st.firstAttemptUsed = false; st.misconceptionId = current.initialMisconceptionId; st.lastResult = "";
            ConfigureProblem(); membrane.PracticeGuide = false; membrane.HasWrong = false; membrane.LockProgress = 0; membrane.CrackClose = 0; membrane.Flash = 0;
            unfoldClock = 0; idleClock = 0; UpdateMeasures(true); Notify();
        }

        void ConfigureProblem()
        {
            membrane.Problem = current; membrane.CurrentF = currentF; membrane.TrailCount = 0;
            congruenceText.text = current.congruence;
            promptText.text = current.band == 3 ? "잘못 놓인 막을 고쳐 대응하는 세 변을 맞추시오." : "꼭짓점 F를 옮겨 대응하는 세 변을 맞추시오.";
            sourceSummaryText.text = "모식도 · AB=" + current.sourceEdges[0] + " cm  BC=" + current.sourceEdges[1] + " cm  CA=" + current.sourceEdges[2] + " cm";
            referenceGraphic.Refresh(); membrane.Refresh(); UpdateHud(true); UpdateGeometryLabels();
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            ambientClock += dt; titleClock += dt;
            Relayout(); AnimateWorld(dt); AnimateUi(dt); HandleInput();

            if (flow == Flow.Playing)
            {
                secondsLeft -= dt;
                if (secondsLeft <= 0) { secondsLeft = 0; EndRun(false, "시간이 끝나 빙막이 다시 갈라졌다"); }
                else UpdateHud(false);
            }
            else if (flow == Flow.Practice)
            {
                idleClock += dt;
                if (idleClock > 8f) membrane.GuideScale = 1.35f + Mathf.Sin(ambientClock * 3f) * .08f;
            }
            else if (flow == Flow.Reveal)
            {
                if (hitStop > 0) hitStop -= dt;
                else revealClock += dt;
                UpdateRevealAnimation();
                if (revealClock >= (revealCorrect ? 1.55f : 1.18f)) FinishReveal();
            }

            if (toastClock < 3f) { toastClock += dt; toastPanel.gameObject.SetActive(toastClock < 2.65f); }
            if (membrane.RippleClock < 1f) membrane.RippleClock += dt;
            if (wrongJolt > 0) wrongJolt = Mathf.Max(0, wrongJolt - dt * 4.4f);
            scoreShown = Mathf.MoveTowards(scoreShown, st.score, Mathf.Max(80f, Mathf.Abs(st.score - scoreShown) * 5f) * dt);
            scoreText.text = Mathf.RoundToInt(scoreShown).ToString("N0") + "점";
            membrane.Pulse = ambientClock; membrane.WrongPulse = wrongJolt; membrane.CurrentF = currentF; membrane.Dragging = st.dragActive; membrane.Refresh();
            referenceGraphic.Pulse = ambientClock; referenceGraphic.Refresh(); lensGraphic.Pulse = ambientClock; lensGraphic.Refresh();
            UpdateGeometryLabels();
        }

        void AnimateWorld(float dt)
        {
            for (int i = 0; i < caustics.Length; i++)
            {
                var p = caustics[i].localPosition;
                p.x = -8f + i * 4f + Mathf.Sin(ambientClock * (.28f + i * .035f) + i) * 1.1f;
                p.y = -1.5f + (i % 2) * 1.2f + Mathf.Sin(ambientClock * .46f + i * .7f) * .22f;
                caustics[i].localPosition = p;
            }
            float breath = 1f + Mathf.Sin(ambientClock * 1.35f) * .025f;
            if (sealBody) sealBody.localScale = new Vector3(2.15f, .56f * breath, 1.12f);
            if (sealHead) sealHead.localRotation = Quaternion.Euler(0, Mathf.Sin(ambientClock * .8f) * 4f, 0);
            if (sealFlipper) sealFlipper.localRotation = Quaternion.Euler(0, 0, 52 + Mathf.Sin(ambientClock * 1.1f) * 7f);
            if (sealEventClock < 2.4f)
            {
                sealEventClock += dt; float k = Mathf.Clamp01(sealEventClock / 2.1f);
                float arc = Mathf.Sin(k * Mathf.PI);
                sealRoot.localPosition = new Vector3(Mathf.Lerp(4.8f, -4.8f, k), -3.05f + arc * .32f, 3.55f);
                sealRoot.localRotation = Quaternion.Euler(0, -9, arc * 3f);
            }
            else if (sealRoot && flow != Flow.Reveal)
            {
                sealRoot.localPosition = Vector3.Lerp(sealRoot.localPosition, new Vector3(4.8f, -3.05f, 3.55f), dt * 2.2f);
            }
            float baseSize = canvasRt && canvasRt.rect.width > canvasRt.rect.height * 1.15f ? 7.1f : 8.4f;
            // 정답은 짧은 카메라 푸시만 사용한다. 화면 흔들림은 없다.
            float zoom = flow == Flow.Reveal && revealCorrect ? Mathf.Sin(Mathf.Clamp01(revealClock / 1.4f) * Mathf.PI) * .26f : 0;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, baseSize - zoom, dt * 7f);
        }

        void AnimateUi(float dt)
        {
            if (flow == Flow.Title)
            {
                float intro = Mathf.Clamp01(titleClock / .65f);
                float over = 1f + Mathf.Sin(Mathf.Clamp01(titleClock / .78f) * Mathf.PI) * .09f;
                titleLogoRoot.localScale = Vector3.one * Mathf.Lerp(.72f, over, SmoothOut(intro));
                titleLogoRoot.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-4f, 0, SmoothOut(intro)));
                titleStart.localScale = Vector3.one * (1f + Mathf.Max(0, Mathf.Sin((titleClock - 1.1f) * 2.4f)) * .018f);
                if (titleArt.enabled) titleBackdrop.localScale = Vector3.one * (1.025f + Mathf.Sin(titleClock * .18f) * .012f);
            }
            if (flow == Flow.Practice || flow == Flow.Playing)
            {
                unfoldClock += dt;
                float k = Mathf.Clamp01(unfoldClock / .48f), over = 1f + Mathf.Sin(k * Mathf.PI) * .035f;
                workPanel.localScale = Vector3.one * Mathf.Lerp(.84f, over, SmoothOut(k));
            }
            if (wrongJolt > 0)
            {
                float x = Mathf.Sin(wrongJolt * 34f) * 7f * wrongJolt;
                workPanel.anchoredPosition = workBasePosition + Vector2.right * x;
            }
            else workPanel.anchoredPosition = workBasePosition;
            if (flow == Flow.End)
            {
                endShown = Mathf.MoveTowards(endShown, st.score, Mathf.Max(120f, st.score * 1.1f) * dt);
                endScore.text = Mathf.RoundToInt(endShown).ToString("N0") + "점";
                endButton.localScale = Vector3.one * (1f + Mathf.Max(0, Mathf.Sin(ambientClock * 2.1f)) * .018f);
            }
        }

        static float SmoothOut(float k) { k = Mathf.Clamp01(k); return 1f - Mathf.Pow(1f - k, 3f); }

        void HandleInput()
        {
            if (flow == Flow.Title)
            {
                if (MgfPointer.Down)
                {
                    st.inputSerial++; MgfSfx.Play("tap", .45f);
                    if (Contains(titleHelp, MgfPointer.Position, 6)) BeginPractice();
                    else BeginPractice(); // The first tap is the suction action anywhere in the title world.
                }
                return;
            }
            if (flow == Flow.End)
            {
                if (MgfPointer.Down)
                {
                    st.inputSerial++; MgfSfx.Play("tap", .44f); BeginRun(true);
                }
                return;
            }
            if (flow == Flow.Reveal)
            {
                if (MgfPointer.Down)
                {
                    st.inputSerial++; invalidCountVisual("황동 피톤이 잠기는 중이다");
                    sealFlipper.localScale = Vector3.one * 1.08f; Notify();
                }
                return;
            }
            if (flow != Flow.Playing && flow != Flow.Practice) return;

            Vector2 canvasPoint = ToCanvas(MgfPointer.Position);
            Vector2 handle = membrane.ModelToCanvas(currentF, canvasRt);
            float grabRadius = flow == Flow.Practice ? 94f : 58f;
            if (MgfPointer.Down)
            {
                st.inputSerial++;
                // 연습에서는 조작판 어디를 눌러도 손잡이로 안내해 첫 잡기 자체가 막히지 않는다.
                // 실전은 F 고리만 잡히며 자유 좌표 판정은 그대로다.
                bool practiceFunnel = flow == Flow.Practice && Contains(workPanel, MgfPointer.Position, 0);
                if ((Vector2.Distance(canvasPoint, handle) <= grabRadius || practiceFunnel) && membrane.ScreenToModel(MgfPointer.Position, out var model))
                {
                    st.dragActive = true; dragMoved = false; dragOffsetModel = practiceFunnel ? Vector2.zero : currentF - model; dragStartModel = currentF;
                    membrane.TrailCount = 1; membrane.Trail[0] = currentF; lastTrailTime = ambientClock;
                    idleClock = 0; MgfSfx.Play("tap", .40f); SetGroup(lensGroup, true); Notify();
                }
                else
                {
                    InvalidTap();
                }
            }
            if (MgfPointer.Held && st.dragActive && membrane.ScreenToModel(MgfPointer.Position, out var dragModel))
            {
                Vector2 next = dragModel + dragOffsetModel;
                next.x = Mathf.Clamp(next.x, BingmakRules.DomainMinX, BingmakRules.DomainMaxX);
                next.y = Mathf.Clamp(next.y, BingmakRules.DomainMinY + .04f, BingmakRules.DomainMaxY);
                if (Vector2.Distance(next, currentF) > .004f)
                {
                    currentF = next; dragMoved |= Vector2.Distance(currentF, dragStartModel) >= .18f;
                    if (ambientClock - lastTrailTime > .035f)
                    {
                        if (membrane.TrailCount < membrane.Trail.Length) membrane.Trail[membrane.TrailCount++] = currentF;
                        else
                        {
                            for (int i = 1; i < membrane.Trail.Length; i++) membrane.Trail[i - 1] = membrane.Trail[i];
                            membrane.Trail[membrane.Trail.Length - 1] = currentF;
                        }
                        lastTrailTime = ambientClock;
                    }
                    UpdateMeasures(false);
                }
            }
            if (MgfPointer.Up && st.dragActive)
            {
                st.dragActive = false; SetGroup(lensGroup, false); membrane.Dragging = false;
                if (!dragMoved)
                {
                    InvalidTap("F를 누른 채 끌어서 놓으시오");
                }
                else if (flow == Flow.Practice)
                {
                    // 연습은 정답을 대신 푸는 문항이 아니라 잡기→끌기→놓기 조작 확인이다.
                    // 실제 이동이 있어야 하며, 완료 뒤 실전부터 두 길이를 학생이 직접 맞춘다.
                    currentF = new Vector2(current.targetX, current.targetY);
                    UpdateMeasures(true);
                    SubmitCurrent();
                }
                else SubmitCurrent();
                Notify();
            }
        }

        void InvalidTap(string message = "귤색 F 고리를 잡아 끌어 놓으시오")
        {
            Vector2 model;
            if (!membrane.ScreenToModel(MgfPointer.Position, out model)) model = currentF;
            membrane.RippleModel = new Vector2(Mathf.Clamp(model.x, -1, 11), Mathf.Clamp(model.y, 0, 10));
            membrane.RippleClock = 0; st.invalidCount++; idleClock = 8.1f; membrane.GuideScale = 1.22f;
            invalidCountVisual(message); MgfSfx.Play("wrong", .16f); Notify();
        }

        void invalidCountVisual(string message)
        {
            ShowToast(message); wrongJolt = Mathf.Max(wrongJolt, .18f);
        }

        void SubmitCurrent()
        {
            st.submissionSerial++; st.attemptCount++;
            bool correct = BingmakRules.IsCorrect(current, currentF);
            if (flow == Flow.Practice)
            {
                if (correct)
                {
                    st.lastResult = "practice_correct"; st.tutorialStep = "sss_reveal";
                    StartReveal(true, true); return;
                }
                st.lastResult = "practice_retry"; st.misconceptionId = BingmakRules.Diagnose(current, currentF);
                membrane.HasWrong = true; membrane.WrongF = currentF; membrane.PracticeGuide = true; membrane.GuideScale = 1.25f;
                wrongJolt = 1f; ShowToast(ExplainWrong(current, currentF)); MgfSfx.Play("wrong", .42f); Notify(); return;
            }

            bool first = !st.firstAttemptUsed;
            if (first) { st.firstAttemptUsed = true; st.firstAttemptTotal++; }
            if (correct)
            {
                if (first) st.firstAttemptCorrect++;
                st.combo = first ? st.combo + 1 : 0;
                st.score += 120 + (first ? 30 : 0) + st.combo * 12;
                st.solved++; st.lastResult = first ? "first_correct" : "repaired"; st.misconceptionId = "";
                mapWindows[Mathf.Clamp(st.solved - 1, 0, 7)].color = new Color(.39f, .90f, .94f, .96f);
                StartReveal(true, false);
            }
            else
            {
                st.combo = 0; st.lives = Mathf.Max(0, st.lives - 1); st.lastResult = "wrong";
                st.misconceptionId = BingmakRules.Diagnose(current, currentF);
                membrane.HasWrong = true; membrane.WrongF = currentF; UpdateClips(); wrongJolt = 1f;
                failAfterReveal = st.lives <= 0 || st.firstAttemptCorrect + (BingmakRules.RoundCount - st.firstAttemptTotal) < BingmakRules.MasteryNeeded;
                StartReveal(false, false);
            }
            Notify();
        }

        void StartReveal(bool correct, bool practice)
        {
            flow = Flow.Reveal; st.phase = "playing"; st.frozen = true; st.dragActive = false;
            revealCorrect = correct; revealPractice = practice; revealClock = 0; lockSoundStep = 0;
            hitStop = correct ? .075f : 0; SetGroup(revealGroup, true); SetGroup(lensGroup, false);
            if (correct)
            {
                revealTitle.text = practice ? "잡기·끌기·놓기 확인" : (st.lastResult == "repaired" ? "수선 완료" : "첫 고정 성공");
                revealBody.text = practice
                    ? "실전에서는 합동 순서를 읽고 DF·EF를 직접 맞춘다"
                    : "DE=" + current.baseLength + " cm · DF=" + current.targetDF + " cm · EF=" + current.targetEF + " cm\nSSS 합동으로 세 피톤을 고정한다";
                sealEventClock = 0; clearWindowWorld.localScale = Vector3.zero;
            }
            else
            {
                revealTitle.text = "눌린 자국을 확인하시오";
                revealBody.text = ExplainWrong(current, currentF) + "\n정확한 대응: DF=" + current.targetDF + " cm · EF=" + current.targetEF + " cm";
                membrane.PracticeGuide = true; MgfSfx.Play("wrong", .54f);
            }
        }

        void UpdateRevealAnimation()
        {
            if (revealCorrect)
            {
                membrane.LockProgress = Mathf.Clamp(revealClock / .27f, 0, 3.15f);
                int step = Mathf.Min(3, Mathf.FloorToInt(membrane.LockProgress));
                while (lockSoundStep < step)
                {
                    lockSoundStep++; MgfSfx.Play(lockSoundStep == 3 ? "correct" : "pop", .48f + lockSoundStep * .05f);
                }
                membrane.CrackClose = Mathf.Clamp01((revealClock - .78f) / .52f);
                membrane.Flash = Mathf.Sin(Mathf.Clamp01(revealClock / 1.4f) * Mathf.PI);
                float k = Mathf.Clamp01((revealClock - .72f) / .62f);
                clearWindowWorld.localScale = Vector3.one * (k <= 0 ? 0 : SmoothOut(k) * (1f + Mathf.Sin(k * Mathf.PI) * .12f));
            }
            else
            {
                membrane.WrongPulse = .55f + Mathf.Sin(revealClock * 15f) * .20f;
                membrane.GuideScale = 1f + Mathf.Sin(revealClock * 5f) * .05f;
            }
        }

        void FinishReveal()
        {
            SetGroup(revealGroup, false); membrane.LockProgress = 0; membrane.CrackClose = 0; membrane.Flash = 0;
            if (revealPractice && revealCorrect)
            {
                PlayerPrefs.SetInt("bingmak-bonghap.onboarding", 1); PlayerPrefs.Save(); BeginRun(true); return;
            }
            if (revealCorrect)
            {
                if (st.solved >= BingmakRules.RoundCount)
                {
                    EndRun(st.firstAttemptCorrect >= BingmakRules.MasteryNeeded,
                        st.firstAttemptCorrect >= BingmakRules.MasteryNeeded ? "여덟 균열이 하나의 빙막 지도가 되었다" : "첫 제출 6개가 필요하다");
                    return;
                }
                flow = Flow.Playing; st.phase = "playing"; st.frozen = false; st.round = st.solved + 1; SelectProblem(st.solved);
            }
            else
            {
                membrane.PracticeGuide = false;
                if (failAfterReveal) { EndRun(false, st.lives <= 0 ? "수선 클립을 모두 사용했다" : "첫 제출 6개를 달성할 수 없다"); return; }
                flow = Flow.Playing; st.phase = "playing"; st.frozen = false; ShowToast("눌린 자국을 따라 같은 보강막을 다시 수선하시오"); Notify();
            }
        }

        void EndRun(bool clear, string reason)
        {
            flow = Flow.End; st.phase = clear ? "clear" : "gameover"; st.frozen = true; st.dragActive = false;
            SetGroup(playGroup, false); SetGroup(revealGroup, false); SetGroup(endGroup, true); SetGroup(lensGroup, false);
            endTitle.text = clear ? "빙벽 측량 완료" : "고정 중단";
            endStats.text = reason + "\n고정 " + st.solved + "/8 · 첫 제출 " + st.firstAttemptCorrect + "/8 · 남은 클립 " + st.lives;
            endShown = 0; endScore.text = "0점";
            if (clear)
            {
                MgfSfx.Play("win", .62f);
                int best = PlayerPrefs.GetInt("bingmak-bonghap.best", 0); if (st.score > best) PlayerPrefs.SetInt("bingmak-bonghap.best", st.score);
            }
            else MgfSfx.Play("lose", .58f);
            PlayerPrefs.SetInt("bingmak-bonghap.last-first", st.firstAttemptCorrect); PlayerPrefs.Save(); Notify();
        }

        string ExplainWrong(IceProblem p, Vector2 f)
        {
            float df = f.magnitude, ef = Vector2.Distance(f, new Vector2(p.baseLength, 0));
            return "DF " + DifferenceWords(df - p.targetDF) + " · EF " + DifferenceWords(ef - p.targetEF);
        }

        string DifferenceWords(float diff)
        {
            string amount = Mathf.Abs(diff).ToString("0.0") + " cm";
            if (Mathf.Abs(diff) <= BingmakRules.Tolerance) return "허용오차 안";
            return amount + (diff > 0 ? " 초과" : " 부족");
        }

        void UpdateMeasures(bool force)
        {
            int df = Mathf.RoundToInt(currentF.magnitude * 100), ef = Mathf.RoundToInt(Vector2.Distance(currentF, new Vector2(current.baseLength, 0)) * 100);
            st.fx100 = Mathf.RoundToInt(currentF.x * 100); st.fy100 = Mathf.RoundToInt(currentF.y * 100); st.df100 = df; st.ef100 = ef;
            if (force || df != lastDf) { measureDF.text = "DF 약 " + (df / 100f).ToString("0.00") + " cm"; lastDf = df; }
            if (force || ef != lastEf) { measureEF.text = "EF 약 " + (ef / 100f).ToString("0.00") + " cm"; lastEf = ef; }
            lensText.text = "4×  DF " + (df / 100f).ToString("0.0") + " · EF " + (ef / 100f).ToString("0.0");
        }

        void UpdateHud(bool force)
        {
            int sec = Mathf.CeilToInt(secondsLeft);
            if (force || sec != lastTimerSecond)
            {
                hudText.text = flow == Flow.Practice ? "연습 · 시간 정지" : "균열 " + Mathf.Max(1, st.round) + "/8 · " + sec + "초";
                lastTimerSecond = sec;
            }
            progressText.text = "고정점 " + st.solved + "/8 · 첫 제출 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal + " (목표 6)";
        }

        void UpdateClips()
        {
            for (int i = 0; i < 3; i++)
            {
                bool full = i < st.lives;
                clipIcons[i].color = full ? Graphite : new Color(.48f, .33f, .31f, .42f);
                clipIcons[i].rectTransform.localRotation = Quaternion.Euler(0, 0, full ? 0 : (i % 2 == 0 ? 18 : -18));
            }
        }

        void ShowToast(string message)
        {
            toastText.text = message; toastClock = 0; toastPanel.gameObject.SetActive(true);
        }

        void UpdateGeometryLabels()
        {
            if (current == null || membrane == null || !playGroup.gameObject.activeSelf) return;
            Vector2 d = membrane.ModelToCanvas(Vector2.zero, canvasRt), e = membrane.ModelToCanvas(new Vector2(current.baseLength, 0), canvasRt), f = membrane.ModelToCanvas(currentF, canvasRt);
            labelD.rectTransform.anchoredPosition = d + new Vector2(-18, -19);
            labelE.rectTransform.anchoredPosition = e + new Vector2(18, -19);
            labelF.rectTransform.anchoredPosition = f + new Vector2(0, 31);
            // 두 변이 가까워져도 수치가 맞붙지 않도록 측정판 상단 좌우에 전용 칸을 둔다.
            float spread = Mathf.Min(96f, workPanel.sizeDelta.x * .24f);
            float measureY = workPanel.anchoredPosition.y + workPanel.sizeDelta.y * .5f - 38f;
            measureDF.rectTransform.anchoredPosition = new Vector2(workPanel.anchoredPosition.x - spread, measureY);
            measureEF.rectTransform.anchoredPosition = new Vector2(workPanel.anchoredPosition.x + spread, measureY);
            if (st.dragActive)
            {
                Vector2 lp = f + new Vector2(f.x > canvasRt.rect.center.x ? -82 : 82, 92);
                float hw = canvasRt.rect.width * .5f, hh = canvasRt.rect.height * .5f;
                lp.x = Mathf.Clamp(lp.x, -hw + 66, hw - 66); lp.y = Mathf.Clamp(lp.y, -hh + 66, hh - 66);
                lensPanel.anchoredPosition = lp;
            }
        }

        void Relayout()
        {
            float w = canvasRt.rect.width, h = canvasRt.rect.height; if (w < 100 || h < 100) return;
            if (Mathf.Abs(w - lastCanvasW) < .5f && Mathf.Abs(h - lastCanvasH) < .5f) return;
            lastCanvasW = w; lastCanvasH = h; st.layoutVersion++;
            bool land = w >= h * 1.15f;
            titleArt.enabled = false;

            if (land)
            {
                const float edge = 24f, zoneGap = 50f;
                Center(hudPanel, new Vector2(0, h * .5f - 31), new Vector2(Mathf.Min(760, w - 220), 50));
                Center(goalPanel, new Vector2(0, h * .5f - 82), new Vector2(Mathf.Min(720, w - 260), 42));
                float top = h * .5f - 114, bottom = -h * .5f + 24;
                float sourceW = Mathf.Clamp(w * .20f, 210, 250), recordW = Mathf.Clamp(w * .15f, 160, 190);
                float workW = Mathf.Clamp(w - edge * 2 - sourceW - recordW - zoneGap * 2, 300, 440);
                float sourceX = -w * .5f + edge + sourceW * .5f;
                float workX = sourceX + sourceW * .5f + zoneGap + workW * .5f;
                float recordX = w * .5f - edge - recordW * .5f;
                Center(referencePanel, new Vector2(sourceX, (top + bottom) * .5f), new Vector2(sourceW, top - bottom));
                Center(workPanel, new Vector2(workX, (top + bottom) * .5f), new Vector2(workW, top - bottom)); workBasePosition = workPanel.anchoredPosition;
                Center(progressPanel, new Vector2(recordX, (top + bottom) * .5f - 30), new Vector2(recordW, Mathf.Min(226, top - bottom - 72)));
                for (int i = 0; i < 8; i++) mapWindows[i].rectTransform.anchoredPosition = new Vector2(((i % 2) - .5f) * 44, 62 - (i / 2) * 32);
                progressText.rectTransform.anchoredPosition = new Vector2(0, -64);
                Center(toastPanel, new Vector2(recordX, top - 28), new Vector2(recordW, 48));
                Center(revealPanel, new Vector2(workX, 0), new Vector2(Mathf.Min(430, workW - 18), 166));
                Center(endPanel, Vector2.zero, new Vector2(480, Mathf.Min(480, h - 44)));
                titleLogoRoot.anchoredPosition = new Vector2(0, -86); titleLogoRoot.sizeDelta = new Vector2(500, 96);
                titleLogo.text = titleLogoShadow.text = "빙막 봉합"; titleLogo.fontSize = titleLogoShadow.fontSize = 56;
                titleTag.rectTransform.anchorMin = titleTag.rectTransform.anchorMax = new Vector2(.5f, 1);
                titleTag.rectTransform.anchoredPosition = new Vector2(0, -153);
                titleStart.anchoredPosition = new Vector2(0, 82); titleHelp.anchoredPosition = new Vector2(0, 25);
            }
            else
            {
                Center(hudPanel, new Vector2(0, h * .5f - 27), new Vector2(Mathf.Min(374, w - 14), 48));
                Center(goalPanel, new Vector2(0, h * .5f - 72), new Vector2(Mathf.Min(372, w - 18), 38));
                float refH = Mathf.Clamp(h * .145f, 112, 140);
                float refY = h * .5f - 92 - refH * .5f;
                Center(referencePanel, new Vector2(0, refY), new Vector2(Mathf.Min(370, w - 18), refH));
                float referenceBottom = refY - refH * .5f;
                Center(toastPanel, new Vector2(0, referenceBottom - 28), new Vector2(Mathf.Min(344, w - 30), 46));
                float workTop = referenceBottom - 57, workBottom = -h * .5f + 69;
                Center(workPanel, new Vector2(0, (workTop + workBottom) * .5f), new Vector2(Mathf.Min(370, w - 16), workTop - workBottom)); workBasePosition = workPanel.anchoredPosition;
                Center(progressPanel, new Vector2(0, -h * .5f + 30), new Vector2(Mathf.Min(372, w - 16), 50));
                for (int i = 0; i < 8; i++) mapWindows[i].rectTransform.anchoredPosition = new Vector2((i - 3.5f) * 35f, 7);
                progressText.rectTransform.anchoredPosition = new Vector2(0, 7);
                Center(revealPanel, new Vector2(0, 0), new Vector2(Mathf.Min(354, w - 24), 166));
                Center(endPanel, Vector2.zero, new Vector2(Mathf.Min(360, w - 24), Mathf.Min(460, h - 64)));
                titleLogoRoot.anchoredPosition = new Vector2(0, -Mathf.Min(180, h * .21f));
                titleLogoRoot.sizeDelta = new Vector2(Mathf.Min(380, w - 28), 170);
                titleLogo.text = titleLogoShadow.text = "빙막\n봉합"; titleLogo.fontSize = titleLogoShadow.fontSize = 61;
                titleTag.rectTransform.anchorMin = titleTag.rectTransform.anchorMax = new Vector2(.5f, .64f);
                titleTag.rectTransform.anchoredPosition = Vector2.zero;
                titleStart.anchoredPosition = new Vector2(0, 92); titleHelp.anchoredPosition = new Vector2(0, 33);
            }
            ((RectTransform)membrane.transform).sizeDelta = workPanel.sizeDelta - new Vector2(12, 12);
            float refInnerW = Mathf.Max(100, referencePanel.sizeDelta.x - 28);
            ((RectTransform)referenceGraphic.transform).sizeDelta = new Vector2(refInnerW, Mathf.Clamp(referencePanel.sizeDelta.y - 112, 70, 210));
            congruenceText.rectTransform.sizeDelta = new Vector2(referencePanel.sizeDelta.x - 20, 28);
            promptText.rectTransform.sizeDelta = new Vector2(referencePanel.sizeDelta.x - 24, land ? 58 : 30);
            sourceSummaryText.rectTransform.sizeDelta = new Vector2(referencePanel.sizeDelta.x - 20, land ? 76 : 28);
            sourceSummaryText.rectTransform.anchoredPosition = land ? new Vector2(0, 44) : new Vector2(0, 13);
            toleranceText.rectTransform.sizeDelta = new Vector2(workPanel.sizeDelta.x - 24, 28);
            progressText.rectTransform.sizeDelta = new Vector2(progressPanel.sizeDelta.x - 16, 34);
            titleMeta.rectTransform.anchorMin = titleMeta.rectTransform.anchorMax = new Vector2(.5f, 0);
            titleBest.rectTransform.anchorMin = titleBest.rectTransform.anchorMax = new Vector2(.5f, 0);
            titleMeta.rectTransform.anchoredPosition = land ? new Vector2(-w * .34f, 88) : new Vector2(0, 165);
            titleBest.rectTransform.anchoredPosition = land ? new Vector2(-w * .34f, 116) : new Vector2(0, 193);
            if (current != null) { ConfigureProblem(); UpdateMeasures(true); }
            Notify();
        }

        void SetCoverUv(RawImage image, float rw, float rh)
        {
            if (!image.texture || rw <= 0 || rh <= 0) return;
            float ta = (float)image.texture.width / image.texture.height, ra = rw / rh;
            if (ta > ra) { float width = ra / ta; image.uvRect = new Rect((1 - width) * .5f, 0, width, 1); }
            else { float height = ta / ra; image.uvRect = new Rect(0, (1 - height) * .5f, 1, height); }
        }

        public void TestStart() => BeginRun(true);

        void EnsureTestPlaying()
        {
            if (flow == Flow.Reveal) { revealClock = 99; FinishReveal(); }
            if (flow != Flow.Playing) BeginRun(true);
        }

        public void TestAnswerCorrect()
        {
            EnsureTestPlaying(); currentF = new Vector2(current.targetX, current.targetY); UpdateMeasures(true); SubmitCurrent();
        }

        public void TestAnswerWrong()
        {
            EnsureTestPlaying(); currentF = BingmakRules.InitialPoint(current);
            if (BingmakRules.IsCorrect(current, currentF)) currentF = new Vector2(10.5f, .5f);
            UpdateMeasures(true); SubmitCurrent();
        }

        public string StateJson() => JsonUtility.ToJson(st);

        public string ProblemBankJson()
        {
            var list = new List<MgfProblem>(bank.Count);
            for (int i = 0; i < bank.Count; i++) list.Add(BingmakRules.ToMgf(bank[i]));
            return MgfJson.Bank(list);
        }

        void Notify() => MgfBridge.NotifyChanged();
        Vector2 ToCanvas(Vector2 screen) { RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var p); return p; }
        bool Contains(RectTransform rt, Vector2 screen, float pad)
        {
            Rect r = rt.rect; r.xMin -= pad; r.xMax += pad; r.yMin -= pad; r.yMax += pad;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, null, out var p); return r.Contains(p);
        }
        void SetGroup(CanvasGroup g, bool on) { g.alpha = on ? 1 : 0; g.interactable = on; g.blocksRaycasts = on; g.gameObject.SetActive(on); }

        CanvasGroup FullGroup(string name, Transform parent)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero); Stretch(rt, 0);
            return rt.gameObject.AddComponent<CanvasGroup>();
        }

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.one * inset; rt.offsetMax = Vector2.one * -inset;
        }

        RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size); var im = rt.gameObject.AddComponent<Image>();
            im.sprite = sprite ?? roundedSprite; im.type = Image.Type.Sliced; im.color = color; im.raycastTarget = false; return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size); var im = rt.gameObject.AddComponent<Image>(); im.sprite = sprite ?? roundedSprite;
            im.type = im.sprite == roundedSprite ? Image.Type.Sliced : Image.Type.Simple; im.color = color; im.raycastTarget = false; return im;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color, float width, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 3f)); var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.fontStyle = FontStyles.Bold; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false; t.text = text; return t;
        }

        static void Center(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        static Sprite MakeRoundedSprite(int size, float radius, bool ring, bool diamond)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "BingmakUiSprite" };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                float dx = Mathf.Abs(fx - size * .5f), dy = Mathf.Abs(fy - size * .5f);
                if (diamond)
                {
                    float dist = dx + dy; a = Mathf.Clamp01(size * .46f - dist + 1f);
                }
                else if (ring)
                {
                    float d = Mathf.Sqrt((fx - size * .5f) * (fx - size * .5f) + (fy - size * .5f) * (fy - size * .5f));
                    a = Mathf.Clamp01(1 - Mathf.Abs(d - size * .40f) / (size * .055f));
                }
                else
                {
                    float qx = Mathf.Max(dx - (size * .5f - radius), 0), qy = Mathf.Max(dy - (size * .5f - radius), 0);
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true); float b = ring || diamond ? 0 : radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }
    }
}
