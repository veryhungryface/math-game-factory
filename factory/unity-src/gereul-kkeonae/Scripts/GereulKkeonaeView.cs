using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.GereulKkeonae
{
    // 코드 디오라마 + 원근 없는 기하/UI 레이어. 런타임에 Instantiate/Destroy하지 않고 전부 부팅 때 만든다.
    public sealed class GereulKkeonaeView
    {
        readonly GereulKkeonaeGame game;
        // 글로우 산업 작업장 계열을 벗어난 야간 도면 천막. 금속·나무보다 무광 직조·실·분필이 화면의 물성을 만든다.
        readonly Color cobalt = MgfLook.Hex("#365C9D");
        readonly Color cobaltDeep = MgfLook.Hex("#17264C");
        readonly Color tangerine = MgfLook.Hex("#F04F78");
        readonly Color ivory = MgfLook.Hex("#FFF4D6");
        readonly Color ink = MgfLook.Hex("#14264A");
        readonly Color foam = MgfLook.Hex("#7FE2D6");
        readonly Color coral = MgfLook.Hex("#FF6B6B");
        readonly Color iron = MgfLook.Hex("#29365B");
        readonly Color rust = MgfLook.Hex("#B34273");
        readonly Color timber = MgfLook.Hex("#344A73");
        readonly Color sand = MgfLook.Hex("#D9C9A5");

        Camera cam;
        Transform world;
        Transform sail;
        readonly List<Transform> dust = new List<Transform>();
        readonly List<float> dustPhase = new List<float>();
        readonly List<Transform> crabs = new List<Transform>();
        readonly List<Transform> pennants = new List<Transform>();
        readonly List<Transform> bridgePlanks = new List<Transform>();
        readonly List<Vector3> bridgeHomes = new List<Vector3>();
        Transform reel;
        Transform titleBuoy;
        float worldClock;

        RectTransform canvasRt, hudRt, goalRt, promptBackRt;
        CanvasGroup titleGroup, playGroup, revealGroup, endGroup, guideGroup;
        RectTransform titleStartRt, restartRt, rulerRt, handleRt, cutZoneRt, cutAssistRt, pieceRt, gapHitRt, netRt;
        RectTransform rippleRt, guideHandRt, guideRingRt;
        RectTransform titleGuideHandRt, titleGuideRingRt;
        Image titleBuoyImg, handleImg, cutBladeImg, cutAssistLaneImg, pieceImg, rippleImg, guideRingImg;
        readonly Image[] guideDots = new Image[7];
        readonly Image[] titleGuideDots = new Image[6];
        readonly Image[] buoyIcons = new Image[3];
        readonly Image[] netEdges = new Image[3];
        readonly Image[] netTicks = new Image[4];
        Image bisectorLine, tensionWave, netPlate;
        readonly TMP_Text[] vertexLabels = new TMP_Text[4];
        readonly List<TMP_Text> rulerLabels = new List<TMP_Text>();
        TMP_Text titleBest, hudRound, hudScore, hudTime, goalText, promptText, readoutText, stepText;
        TMP_Text givenText, proofText, toastText, revealTitle, revealBody, endTitle, endStats, endScore;
        TMP_Text pieceLabel, handleLabel;
        float toastClock, rippleClock, guideClock, revealVisualClock, endClock, shownScore;
        bool guideStrong, isWide, cutAssistExpanded, connectAssistExpanded;
        Vector2 pieceHome, pieceDragOffset;
        int lastW, lastH, lastScore = -1, lastSecond = -1, lastLives = -1;
        NetProblem shownProblem;
        bool revealCorrect;
        int revealCut;
        float buoyPress;
        float titleGuideStrongClock;
        Vector2 titleGuideFrom;

        static Sprite softSprite;
        static Sprite SoftSprite
        {
            get
            {
                if (!softSprite) softSprite = Sprite.Create(MgfLook.SoftDot, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64f);
                return softSprite;
            }
        }

        public GereulKkeonaeView(GereulKkeonaeGame game)
        {
            this.game = game;
            BuildWorld();
            BuildUi();
            Relayout(true);
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#1A2148"), MgfLook.Hex("#514A82"), MgfLook.Hex("#11172F"), .72f);
            MgfLook.Sun(new Vector3(38, -28, -18), new Color(.72f, .8f, 1f), 1.12f, .48f);
            cam = MgfLook.Camera(new Vector3(0, 8.8f, -19f), new Vector3(0, 2.5f, 1.4f), 35f);
            cam.orthographic = true; cam.orthographicSize = 8.5f; cam.backgroundColor = MgfLook.Hex("#151C3B");
            world = new GameObject("MoonlitTriangleChartLoft").transform;

            var back = MgfLook.Prim(PrimitiveType.Quad, "IndigoCanvasBackdrop", new Vector3(0, 8f, 15f), new Vector3(42, 25, 1), MgfLook.Unlit(MgfLook.Hex("#252E58")), world, false);
            back.transform.rotation = Quaternion.Euler(0, 180, 0);
            var clothDark = MgfLook.Lit(MgfLook.Hex("#24345D"), .04f, 0f);
            var clothLight = MgfLook.Lit(MgfLook.Hex("#344A73"), .03f, 0f);
            for (int i = -6; i <= 6; i++)
            {
                var panel = MgfLook.Block("WovenChartPanel", new Vector3(i * 1.48f, -.42f + (i & 1) * .018f, 4.35f), new Vector3(1.4f, .2f, 12.4f), .06f, (i & 1) == 0 ? clothDark : clothLight, world);
                panel.transform.rotation = Quaternion.Euler(0, (i % 3 - 1) * .35f, 0);
            }

            // 큰 반원 후프와 분필 격자가 '작업 랙'이 아닌 펼쳐진 항로 도면을 즉시 읽히게 한다.
            var chalk = MgfLook.Lit(MgfLook.Hex("#84D9D1"), .02f, 0f, MgfLook.Hex("#315D72"));
            for (int row = 0; row < 4; row++)
            {
                float z = .4f + row * 2.1f;
                BeamBetween("ChalkLatitude", new Vector3(-7.2f, -.24f, z), new Vector3(7.2f, -.24f, z), .024f, chalk, world);
            }
            for (int col = -3; col <= 3; col++)
                BeamBetween("ChalkLongitude", new Vector3(col * 2.1f, -.23f, -.25f), new Vector3(col * 2.1f, -.23f, 8.4f), .02f, chalk, world);

            var hoop = MgfLook.Lit(MgfLook.Hex("#D6B96D"), .12f, 0f);
            for (int ring = 0; ring < 2; ring++)
            {
                float radius = ring == 0 ? 5.6f : 3.8f;
                for (int i = 0; i < 24; i++)
                {
                    float a = i * Mathf.PI * 2f / 24f, b = (i + 1) * Mathf.PI * 2f / 24f;
                    BeamBetween("ChartHoop", new Vector3(Mathf.Cos(a) * radius, -.08f, 4.2f + Mathf.Sin(a) * radius * .62f), new Vector3(Mathf.Cos(b) * radius, -.08f, 4.2f + Mathf.Sin(b) * radius * .62f), .045f, hoop, world);
                }
            }
            BuildRopeBridge(clothLight, chalk);
            BuildPennants(); BuildDust(); BuildReel(); BuildSail();

            titleBuoy = new GameObject("KnottedChartSeal").transform; titleBuoy.SetParent(world, false); titleBuoy.position = new Vector3(0, 1.15f, -.8f);
            var hookMat = MgfLook.Lit(tangerine, .08f, 0f, MgfLook.Hex("#5D244E"));
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                MgfLook.Prim(PrimitiveType.Cylinder, "ThreadKnot", new Vector3(Mathf.Cos(a) * .72f, Mathf.Sin(a) * .72f, 0), new Vector3(.11f, .32f, .11f), hookMat, titleBuoy, false).transform.rotation = Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg);
            }
            crabs.Add(BuildDockCrab(new Vector3(-4.65f, .12f, 2.1f), .7f));
        }

        GameObject BeamBetween(string name, Vector3 a, Vector3 b, float radius, Material mat, Transform parent)
        {
            Vector3 d = b - a;
            var beam = MgfLook.Prim(PrimitiveType.Cylinder, name, (a + b) * .5f, new Vector3(radius, d.magnitude * .5f, radius), mat, parent, false);
            beam.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            return beam;
        }

        void BuildPulleys(Material ironMat, Material rustMat)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var pulley = new GameObject("WorkingPulley").transform; pulley.SetParent(world, false); pulley.position = new Vector3(side * 4.65f, 5.65f, 4.9f);
                MgfLook.Prim(PrimitiveType.Cylinder, "IronWheel", Vector3.zero, new Vector3(.72f, .18f, .72f), ironMat, pulley, false).transform.rotation = Quaternion.Euler(90, 0, 0);
                MgfLook.Prim(PrimitiveType.Cylinder, "CopperHub", new Vector3(0, 0, -.22f), new Vector3(.24f, .24f, .24f), rustMat, pulley, false).transform.rotation = Quaternion.Euler(90, 0, 0);
                BeamBetween("SuspensionCord", pulley.position + Vector3.down * .55f, new Vector3(side * 3.9f, .35f, 3.3f), .055f, MgfLook.Lit(sand, .1f), world);
            }
        }

        void BuildRopeBridge(Material plankMat, Material ropeMat)
        {
            var cord = MgfLook.Lit(MgfLook.Hex("#F04F78"), .04f, 0f, MgfLook.Hex("#5D244E"));
            BeamBetween("EvidenceThreadTop", new Vector3(-5.1f, .02f, 4.05f), new Vector3(5.1f, .02f, 4.05f), .045f, cord, world);
            BeamBetween("EvidenceThreadBottom", new Vector3(-5.1f, .02f, 5.45f), new Vector3(5.1f, .02f, 5.45f), .045f, cord, world);
            for (int i = 0; i < 11; i++)
            {
                float x = -4.55f + i * .91f;
                float stagger = (i % 3 - 1) * .08f;
                var p = MgfLook.Block("ProofClothTab", new Vector3(x, .05f + stagger, 4.75f), new Vector3(.72f, .1f, 1.12f), .04f, i % 2 == 0 ? plankMat : MgfLook.Lit(MgfLook.Hex("#465B89"), .03f), world).transform;
                p.localRotation = Quaternion.Euler(0, (i - 5) * .45f, (i % 2 == 0 ? -1.5f : 1.5f));
                bridgePlanks.Add(p); bridgeHomes.Add(p.localPosition);
                BeamBetween("TabStitch", new Vector3(x, .12f, 4.15f), new Vector3(x, .12f, 5.35f), .022f, cord, world);
            }
        }

        void BuildPennants()
        {
            var flagMat = MgfLook.Lit(MgfLook.Hex("#7FE2D6"), .04f);
            for (int i = 0; i < 7; i++)
            {
                var f = MgfLook.Block("PinnedPatternSwatch", new Vector3(-4.5f + i * 1.5f, .16f, 7.8f), new Vector3(.68f, .08f, .82f), .035f, flagMat, world).transform;
                f.localRotation = Quaternion.Euler(0, i % 2 == 0 ? -8 : 8, 0); pennants.Add(f);
            }
        }

        void BuildDust()
        {
            var mat = MgfLook.Alpha(new Color(1f, .82f, .52f, .26f));
            var rng = new System.Random(17);
            for (int i = 0; i < 18; i++)
            {
                float s = .045f + (float)rng.NextDouble() * .08f;
                var p = MgfLook.Prim(PrimitiveType.Sphere, "SunDust", new Vector3(-8f + (float)rng.NextDouble() * 16f, .2f + (float)rng.NextDouble() * 8f, 1f + (float)rng.NextDouble() * 8f), Vector3.one * s, mat, world, false).transform;
                dust.Add(p); dustPhase.Add((float)rng.NextDouble() * 8f);
            }
        }

        void BuildReel()
        {
            var ceramic = MgfLook.Lit(MgfLook.Hex("#C9A963"), .14f, .04f);
            var cordTex = Resources.Load<Texture2D>("GereulKkeonae/cord-fiber");
            if (cordTex) { cordTex.wrapMode = TextureWrapMode.Repeat; cordTex.filterMode = FilterMode.Bilinear; }
            var cordMat = MgfLook.Lit(tangerine, .04f, 0f, MgfLook.Hex("#5D244E"), cordTex);
            reel = new GameObject("ChartThreadBobbin").transform; reel.SetParent(world, false); reel.position = new Vector3(4.9f, .42f, .6f);
            MgfLook.Prim(PrimitiveType.Cylinder, "ReelBody", Vector3.zero, new Vector3(1.15f, .38f, 1.15f), ceramic, reel, false).transform.rotation = Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < 10; i++)
            {
                var coil = MgfLook.Prim(PrimitiveType.Cylinder, "CordCoil", new Vector3(0, 0, -.36f + i * .08f), new Vector3(.82f - i * .018f, .045f, .82f - i * .018f), cordMat, reel, false);
                coil.transform.rotation = Quaternion.Euler(90, 0, 0);
            }
        }

        Transform BuildDockCrab(Vector3 pos, float scale)
        {
            var root = new GameObject("RiggerCrab").transform; root.SetParent(world, false); root.position = pos; root.localScale = Vector3.one * scale;
            var orange = MgfLook.Lit(rust, .32f, .08f);
            var cream = MgfLook.Lit(sand, .28f);
            MgfLook.Block("AngularBody", new Vector3(0, .48f, 0), new Vector3(1.4f, .62f, .82f), .15f, orange, root);
            MgfLook.Block("CanvasApron", new Vector3(0, .34f, -.43f), new Vector3(.72f, .32f, .12f), .08f, cream, root);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int j = 0; j < 2; j++) BeamBetween("JointedLeg", new Vector3(side * .52f, .3f, j * .24f), new Vector3(side * (1.02f + j * .12f), .08f, .05f + j * .35f), .07f, orange, root);
                MgfLook.Block("ClampClaw", new Vector3(side * 1.02f, .68f, -.05f), new Vector3(.58f + (side > 0 ? .12f : 0), .42f, .32f), .12f, orange, root);
                MgfLook.Prim(PrimitiveType.Sphere, "Eye", new Vector3(side * .3f, .95f, -.18f), Vector3.one * .16f, MgfLook.Lit(ink, .6f), root, false);
            }
            return root;
        }

        void BuildSail()
        {
            sail = new GameObject("StitchedChartBanner").transform; sail.SetParent(world, false); sail.position = new Vector3(0, 6.2f, 4.55f);
            var cloth = MgfLook.Block("IndigoTitleCloth", Vector3.zero, new Vector3(6.2f, 2.15f, .1f), .12f, MgfLook.Lit(MgfLook.Hex("#405584"), .04f), sail);
            UnityEngine.Object.Destroy(cloth.GetComponent<Collider>());
            sail.rotation = Quaternion.Euler(2, 0, -1);
            var shadow = MgfText.World("게를 꺼내", new Vector3(.08f, .05f, -.12f), 7.8f, new Color(.04f, .08f, .18f, .58f), sail);
            var logo = MgfText.World("게를 꺼내", new Vector3(0, .12f, -.14f), 7.8f, ivory, sail);
            shadow.alignment = logo.alignment = TextAlignmentOptions.Center;
            shadow.rectTransform.sizeDelta = logo.rectTransform.sizeDelta = new Vector2(6, 2);
            // TMP 전면이 카메라(-Z)를 향한다. 180°를 주면 WebGL에서 뒷면이 좌우 반전되어 보인다.
            shadow.transform.rotation = logo.transform.rotation = Quaternion.identity;
        }

        void BuildUi()
        {
            var canvas = MgfText.Canvas; canvasRt = (RectTransform)canvas.transform;
            titleGroup = Group("TitleUI"); playGroup = Group("PlayUI"); revealGroup = Group("RevealUI"); endGroup = Group("EndUI"); guideGroup = Group("GuideUI");
            BuildTitleUi(); BuildPlayUi(); BuildRevealUi(); BuildEndUi(); BuildFeedbackUi();
        }

        CanvasGroup Group(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup)); go.transform.SetParent(canvasRt, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go.GetComponent<CanvasGroup>();
        }

        Image Img(string name, Transform parent, Color color, Vector2 size, Vector2 anchor, Vector2 pos, bool soft = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.sizeDelta = size; rt.anchoredPosition = pos;
            var im = go.GetComponent<Image>(); im.color = color; im.raycastTarget = false; if (soft) im.sprite = SoftSprite;
            return im;
        }

        TMP_Text Txt(string name, Transform parent, string text, float size, Color color, Vector2 box, Vector2 anchor, Vector2 pos, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.sizeDelta = box; rt.anchoredPosition = pos;
            var t = go.GetComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.fontStyle = FontStyles.Bold; t.color = color; t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text;
            return t;
        }

        void BuildTitleUi()
        {
            // 세계와 분리된 사각 버튼 대신 도면 천에 꿰맨 큰 매듭 인장을 시작 사물로 쓴다.
            var tagFrame = Img("ChartSealOuter", titleGroup.transform, new Color(.07f, .12f, .27f, .98f), new Vector2(164, 164), new Vector2(.5f, .5f), Vector2.zero, true);
            titleBuoyImg = Img("ChartSealThread", tagFrame.transform, new Color(.94f, .31f, .47f, .98f), new Vector2(142, 142), new Vector2(.5f, .5f), Vector2.zero, true);
            titleStartRt = tagFrame.rectTransform;
            Img("ChartSealCloth", titleBuoyImg.transform, cobaltDeep, new Vector2(92, 92), new Vector2(.5f, .5f), Vector2.zero, true);
            Img("ChartSealEyelet", titleBuoyImg.transform, sand, new Vector2(24, 24), new Vector2(.5f, .78f), Vector2.zero, true);
            Txt("BuoyVerb", titleBuoyImg.transform, "매듭을\n눌러 시작", 16, ivory, new Vector2(110, 62), new Vector2(.5f, .46f), Vector2.zero);
            Txt("TitleTagline", titleGroup.transform, "줄을 재서 삼각 항로를 꿰매라", 18, ivory, new Vector2(350, 42), new Vector2(.5f, .32f), Vector2.zero);
            Txt("TitleGrade", titleGroup.transform, "중학교 2학년 · 삼각형의 성질", 12, new Color(1, 1, 1, .88f), new Vector2(300, 28), new Vector2(.5f, .06f), Vector2.zero);
            titleBest = Txt("TitleBest", titleGroup.transform, "", 12, ivory, new Vector2(260, 26), new Vector2(.5f, .11f), Vector2.zero);
            titleGuideRingRt = Img("TitleTargetPulse", titleGroup.transform, new Color(.5f, .94f, .88f, .55f), new Vector2(176, 176), new Vector2(.5f, .5f), Vector2.zero, true).rectTransform;
            titleGuideHandRt = Txt("TitlePointer", titleGroup.transform, "●", 30, ivory, new Vector2(50, 50), new Vector2(.5f, .5f), new Vector2(0, 105)).rectTransform;
            for (int i = 0; i < titleGuideDots.Length; i++) titleGuideDots[i] = Img("TitlePath" + i, titleGroup.transform, new Color(1f, .9f, .55f, .75f), new Vector2(10, 10), new Vector2(.5f, .5f), Vector2.zero, true);
        }

        void BuildPlayUi()
        {
            var hud = Img("HudRibbon", playGroup.transform, new Color(.04f, .16f, .28f, .88f), new Vector2(374, 52), new Vector2(.5f, 1), new Vector2(0, -29)); hudRt = hud.rectTransform;
            hudRound = Txt("Round", hud.transform, "그물 1/8", 15, ivory, new Vector2(110, 36), new Vector2(.16f, .5f), Vector2.zero);
            hudScore = Txt("Score", hud.transform, "0점", 15, ivory, new Vector2(90, 36), new Vector2(.5f, .5f), Vector2.zero);
            hudTime = Txt("Time", hud.transform, "120초", 15, ivory, new Vector2(90, 36), new Vector2(.82f, .5f), Vector2.zero);
            for (int i = 0; i < 3; i++) buoyIcons[i] = Img("SafetyBuoy" + i, hud.transform, tangerine, new Vector2(22, 22), new Vector2(.68f + i * .065f, .5f), Vector2.zero, true);
            var goal = Img("GoalLine", playGroup.transform, new Color(.95f, .98f, .9f, .96f), new Vector2(374, 38), new Vector2(.5f, 1), new Vector2(0, -76)); goalRt = goal.rectTransform;
            goalText = Txt("Goal", goal.transform, "줄을 맞게 잘라 게를 꺼내시오", 14, ink, new Vector2(360, 34), new Vector2(.5f, .5f), Vector2.zero);

            netRt = (RectTransform)new GameObject("GeometryNet", typeof(RectTransform)).transform; netRt.SetParent(playGroup.transform, false);
            netPlate = Img("NetWaterPlate", netRt, new Color(.08f, .36f, .68f, .72f), new Vector2(360, 310), new Vector2(.5f, .5f), Vector2.zero);
            for (int i = 0; i < 3; i++) netEdges[i] = Img("NetEdge" + i, netRt, ivory, new Vector2(190, 6), new Vector2(.5f, .5f), Vector2.zero);
            bisectorLine = Img("Bisector", netRt, new Color(.72f, .91f, .91f, .95f), new Vector2(100, 4), new Vector2(.5f, .5f), Vector2.zero);
            tensionWave = Img("TensionWave", netRt, new Color(1f, .78f, .25f, 0), new Vector2(20, 20), new Vector2(.5f, .5f), Vector2.zero, true);
            for (int i = 0; i < 4; i++) vertexLabels[i] = Txt("Vertex" + i, netRt, "ABCD"[i].ToString(), 20, ivory, new Vector2(36, 30), new Vector2(.5f, .5f), Vector2.zero);
            for (int i = 0; i < 4; i++) netTicks[i] = Img("EqualMark" + i, netRt, tangerine, new Vector2(4, 20), new Vector2(.5f, .5f), Vector2.zero);
            givenText = Txt("Given", netRt, "", 17, ivory, new Vector2(340, 58), new Vector2(.5f, .1f), Vector2.zero);
            promptBackRt = Img("PromptBacking", playGroup.transform, new Color(1f, .95f, .84f, 1f), new Vector2(374, 88), new Vector2(.5f, .5f), Vector2.zero).rectTransform;
            promptText = Txt("Prompt", playGroup.transform, "", 16, ink, new Vector2(360, 74), new Vector2(.5f, .5f), Vector2.zero);
            proofText = Txt("Proof", playGroup.transform, "", 15, ink, new Vector2(360, 56), new Vector2(.5f, .5f), Vector2.zero);

            rulerRt = (RectTransform)new GameObject("MeasuringTool", typeof(RectTransform)).transform; rulerRt.SetParent(playGroup.transform, false);
            Img("CeramicRuler", rulerRt, new Color(.98f, .94f, .82f, .98f), new Vector2(360, 142), new Vector2(.5f, .5f), Vector2.zero);
            for (int i = 0; i < 24; i++)
            {
                float x = -158f + i * (316f / 23f);
                Img("Tick" + (i + 1), rulerRt, ink, new Vector2(i % 3 == 2 ? 3 : 2, i % 3 == 2 ? 24 : 14), new Vector2(.5f, .5f), new Vector2(x, -14));
                if (i % 3 == 2) rulerLabels.Add(Txt("TickLabel" + (i + 1), rulerRt, (i + 1).ToString(), 10, ink, new Vector2(28, 18), new Vector2(.5f, .5f), new Vector2(x, 18)));
            }
            handleImg = Img("RoundKnotHandle", rulerRt, tangerine, new Vector2(66, 66), new Vector2(.5f, .5f), Vector2.zero, true); handleRt = handleImg.rectTransform;
            handleLabel = Txt("HandleLength", handleRt, "3", 18, ink, new Vector2(50, 30), new Vector2(.5f, .5f), Vector2.zero);
            cutBladeImg = Img("CutLine", rulerRt, coral, new Vector2(8, 86), new Vector2(.5f, .5f), new Vector2(0, -2)); cutZoneRt = cutBladeImg.rectTransform;
            cutAssistRt = (RectTransform)new GameObject("CutSwipeAssist", typeof(RectTransform)).transform; cutAssistRt.SetParent(rulerRt, false);
            cutAssistRt.anchorMin = cutAssistRt.anchorMax = new Vector2(.5f, .5f); cutAssistRt.sizeDelta = new Vector2(176, 76); cutAssistRt.anchoredPosition = new Vector2(0, -2);
            cutAssistLaneImg = Img("CutSwipeLane", cutAssistRt, new Color(.94f, .31f, .47f, .3f), new Vector2(152, 18), new Vector2(.5f, .5f), Vector2.zero, true);
            Img("CutSwipeStart", cutAssistRt, foam, new Vector2(42, 42), new Vector2(.08f, .5f), Vector2.zero, true);
            Img("CutSwipeFinish", cutAssistRt, ivory, new Vector2(42, 42), new Vector2(.92f, .5f), Vector2.zero, true);
            Txt("CutSwipeArrow", cutAssistRt, "▶", 22, ivory, new Vector2(64, 38), new Vector2(.5f, .5f), Vector2.zero);
            Txt("CutSwipeStartLabel", cutAssistRt, "시작", 9, ink, new Vector2(46, 20), new Vector2(.08f, .12f), Vector2.zero);
            Txt("CutSwipeFinishLabel", cutAssistRt, "도착", 9, ink, new Vector2(46, 20), new Vector2(.92f, .12f), Vector2.zero);
            pieceImg = Img("CutPiece", playGroup.transform, ivory, new Vector2(150, 18), new Vector2(.5f, .5f), Vector2.zero);
            pieceRt = pieceImg.rectTransform; pieceLabel = Txt("PieceLabel", pieceRt, "", 16, ink, new Vector2(90, 32), new Vector2(.5f, .5f), new Vector2(0, -26));
            readoutText = Txt("InstantReadout", rulerRt, "3 cm", 20, ink, new Vector2(150, 38), new Vector2(.5f, .5f), new Vector2(0, 51));
            stepText = Txt("StepInstruction", playGroup.transform, "매듭을 끌어 길이를 정하시오", 14, ivory, new Vector2(360, 34), new Vector2(.5f, .5f), Vector2.zero);
            gapHitRt = (RectTransform)new GameObject("RepairGapHit", typeof(RectTransform)).transform; gapHitRt.SetParent(netRt, false); gapHitRt.sizeDelta = new Vector2(110, 80);
            pieceRt.gameObject.SetActive(false); cutAssistRt.gameObject.SetActive(false);
        }

        void BuildRevealUi()
        {
            var card = Img("EvidenceCard", revealGroup.transform, new Color(.98f, .95f, .84f, .98f), new Vector2(350, 160), new Vector2(.5f, .5f), Vector2.zero);
            revealTitle = Txt("RevealTitle", card.transform, "", 25, ink, new Vector2(320, 42), new Vector2(.5f, .72f), Vector2.zero);
            revealBody = Txt("RevealBody", card.transform, "", 16, ink, new Vector2(316, 92), new Vector2(.5f, .35f), Vector2.zero);
        }

        void BuildEndUi()
        {
            var card = Img("EndShell", endGroup.transform, new Color(.98f, .95f, .84f, .98f), new Vector2(356, 290), new Vector2(.5f, .5f), Vector2.zero);
            endTitle = Txt("EndTitle", card.transform, "", 30, ink, new Vector2(330, 62), new Vector2(.5f, .78f), Vector2.zero);
            endStats = Txt("EndStats", card.transform, "", 17, ink, new Vector2(320, 100), new Vector2(.5f, .48f), Vector2.zero);
            endScore = Txt("EndScore", card.transform, "0점", 28, tangerine, new Vector2(220, 50), new Vector2(.5f, .25f), Vector2.zero);
            var b = Img("RestartBuoy", card.transform, tangerine, new Vector2(86, 86), new Vector2(.5f, .05f), new Vector2(0, 8), true); restartRt = b.rectTransform;
            Txt("RestartText", b.transform, "다시 구조", 14, ivory, new Vector2(120, 32), new Vector2(.5f, .5f), new Vector2(0, -54));
        }

        void BuildFeedbackUi()
        {
            var toast = Img("Toast", canvasRt, new Color(.04f, .15f, .27f, .92f), new Vector2(350, 52), new Vector2(.5f, .18f), Vector2.zero);
            toastText = Txt("ToastText", toast.transform, "", 14, ivory, new Vector2(330, 46), new Vector2(.5f, .5f), Vector2.zero);
            toastText.transform.parent.gameObject.SetActive(false);
            rippleImg = Img("InputRipple", canvasRt, new Color(1f, .75f, .28f, 0), new Vector2(54, 54), new Vector2(.5f, .5f), Vector2.zero, true); rippleRt = rippleImg.rectTransform;
            guideHandRt = Txt("GhostHand", guideGroup.transform, "●", 34, ivory, new Vector2(50, 50), new Vector2(.5f, .5f), Vector2.zero).rectTransform;
            guideRingImg = Img("GuideRing", guideGroup.transform, new Color(1f, .78f, .3f, .7f), new Vector2(72, 72), new Vector2(.5f, .5f), Vector2.zero, true); guideRingRt = guideRingImg.rectTransform;
            for (int i = 0; i < guideDots.Length; i++) guideDots[i] = Img("PathChevron" + i, guideGroup.transform, new Color(1f, .9f, .55f, .7f), new Vector2(10, 10), new Vector2(.5f, .5f), Vector2.zero, true);
        }

        public void ShowTitle()
        {
            SetGroup(titleGroup, true); SetGroup(playGroup, false); SetGroup(revealGroup, false); SetGroup(endGroup, false); SetGroup(guideGroup, false);
            sail.gameObject.SetActive(true); titleBuoy.gameObject.SetActive(true);
            int best = PlayerPrefs.GetInt("gereul-kkeonae-best", 0); titleBest.text = best > 0 ? "최고 기록 " + best + "점" : "";
            revealVisualClock = endClock = 0f;
        }

        public void ShowPractice(int which)
        {
            sail.gameObject.SetActive(false); SetGroup(titleGroup, false); SetGroup(playGroup, true); SetGroup(revealGroup, false); SetGroup(endGroup, false); SetGroup(guideGroup, true);
            goalText.text = which == 1 ? "연습 · 같은 두 각의 맞은편 변을 찾으시오" : "연습 · 이등분된 밑변을 찾으시오";
            guideClock = 0f; guideStrong = true; Relayout(true);
        }

        public void ShowRun(bool fromTest)
        {
            sail.gameObject.SetActive(false); SetGroup(titleGroup, false); SetGroup(playGroup, true); SetGroup(revealGroup, false); SetGroup(endGroup, false); SetGroup(guideGroup, fromTest ? false : true);
            goalText.text = "8그물 구조 · 첫 연결 6/8 · 안전 부표 3개"; shownScore = 0; lastScore = -1; Relayout(true);
        }

        public void LoadProblem(NetProblem p, bool practice)
        {
            shownProblem = p; promptText.text = p.prompt; proofText.text = ""; proofText.gameObject.SetActive(false);
            givenText.text = p.band == 1 ? "같은 밑각  ∠B = ∠C" : "선분 AB = 선분 AC · D는 이등분선과 선분 BC의 교점";
            SetPulledLength(game.State.drawnLength, false); pieceRt.gameObject.SetActive(false); SetCutAssist(false, false); connectAssistExpanded = false;
            stepText.text = "① 둥근 매듭을 끌어 줄 길이를 정하시오";
            DrawNet(p); guideClock = 0f; guideStrong = practice; RefreshHud(true); Relayout(true);
        }

        public void RetryProblem(NetProblem p, int length)
        {
            SetGroup(revealGroup, false); SetGroup(playGroup, true); SetPulledLength(length, false); pieceRt.gameObject.SetActive(false);
            SetCutAssist(false, false); connectAssistExpanded = false; stepText.text = "① 길이를 다시 정하시오"; guideClock = 0f; guideStrong = true; proofText.gameObject.SetActive(false); RefreshHud(true);
        }

        public void OnLengthSet(int length)
        {
            stepText.text = "② 하늘색 시작점에서 흰 도착점까지 가로지르시오"; guideClock = 0f; guideStrong = true;
            SetCutAssist(true, false);
            MgfFx.Punch(readoutText.transform, .16f, .24f);
        }

        public void OnCut(int length)
        {
            SetCutAssist(false, false); connectAssistExpanded = false;
            pieceRt.gameObject.SetActive(true); pieceHome = pieceRt.anchoredPosition;
            pieceLabel.text = length + " cm"; stepText.text = "③ 잘린 줄을 점선 구간에 거시오";
            pieceRt.sizeDelta = new Vector2(82 + length * 4.2f, 20); guideClock = 0f; guideStrong = true;
            SyncTestGeometry();
            MgfFx.Burst(cam.ScreenToWorldPoint(new Vector3(Screen.width * .5f, Screen.height * .22f, 18f)), ivory, 10, 1.4f, .08f);
        }

        public void BeginReveal(bool correct, bool first, NetProblem p, int cut)
        {
            revealCorrect = correct; revealCut = cut; revealVisualClock = 0f;
            SetGroup(revealGroup, true); SetGroup(guideGroup, false);
            proofText.gameObject.SetActive(true); proofText.text = p.evidence;
            if (correct)
            {
                revealTitle.text = "증거선이 잠겼습니다";
                revealBody.text = p.evidence + "\n대응하는 변이 포개져 삼각 구조망이 완성됩니다.";
                revealTitle.color = cobaltDeep; MgfSfx.Play("correct", .5f); MgfFx.Glow(new Vector3(0, 3f, 1f), foam, 18, .45f);
            }
            else
            {
                revealTitle.text = cut < p.answerCm ? "줄이 닿지 않습니다" : "줄이 길어 처졌습니다";
                revealBody.text = MisconceptionText(game.State.misconceptionId) + "\n정답 상태: " + p.evidence;
                revealTitle.color = coral; MgfSfx.Play("wrong", .42f); MgfFx.Shake(cam, .07f, .22f);
            }
            RefreshHud(true); MgfFx.Punch(revealTitle.transform, .18f, .28f);
        }

        string MisconceptionText(string id)
        {
            switch (id)
            {
                case "adjacent_side": return "같은 각의 맞은편 변을 찾아야 합니다.";
                case "half_is_whole": return "전체 밑변과 한쪽 밑변을 구분하시오.";
                case "halve_everything": return "수리할 구간이 전체인지 먼저 확인하시오.";
                case "screen_bottom_base": return "화면 아래쪽이 아니라 같은 두 변의 맞은편을 보시오.";
                case "cord_too_short": return "짧은 줄은 늘일 수 없어 새 줄이 필요합니다.";
                default: return "긴 줄은 눈금 띠에 다시 걸어 다듬을 수 있습니다.";
            }
        }

        public void ShowEnd(bool clear, string reason)
        {
            SetGroup(playGroup, false); SetGroup(revealGroup, false); SetGroup(guideGroup, false); SetGroup(endGroup, true);
            endClock = 0f; shownScore = 0;
            endTitle.text = clear ? "공동 그물침대 완성" : reason == "time" ? "구조 시간 종료" : reason == "buoys" ? "안전 부표 소진" : "구조 완료 · 기준 미달";
            endStats.text = "구조 " + game.State.solved + "/8\n첫 연결 " + game.State.firstCorrect + "/" + game.State.firstAttempts + "\n재단 " + game.State.retrims + "회 · 폐기 " + game.State.discardedCm + " cm";
            if (clear)
            {
                int best = PlayerPrefs.GetInt("gereul-kkeonae-best", 0); if (game.State.score > best) { PlayerPrefs.SetInt("gereul-kkeonae-best", game.State.score); PlayerPrefs.Save(); }
                for (int i = 0; i < crabs.Count; i++) MgfFx.Glow(crabs[i].position + Vector3.up, tangerine, 4, .28f);
            }
        }

        public void Refuse(string message, Vector2 screen, bool guide)
        {
            if (game.CurrentStep == GereulKkeonaeGame.ToolStep.Cut) SetCutAssist(true, true);
            else if (game.CurrentStep == GereulKkeonaeGame.ToolStep.Connect && game.State.onboarding) connectAssistExpanded = true;
            toastText.text = message; toastText.transform.parent.gameObject.SetActive(true); toastClock = 2.2f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var local); rippleRt.anchoredPosition = local;
            rippleClock = .42f; rippleImg.color = new Color(1f, .76f, .28f, .9f); rippleRt.localScale = Vector3.one * .5f;
            if (game.CurrentFlow == GereulKkeonaeGame.Flow.Title)
            {
                titleGuideFrom = local; titleGuideStrongClock = 2.8f;
            }
            else if (guide) { guideClock = 0f; guideStrong = true; SetGroup(guideGroup, true); }
            MgfFx.Punch(toastText.transform.parent, .08f, .2f);
        }

        public bool HitStart(Vector2 p) => ContainsExpanded(titleStartRt, p, 22);
        public bool HitRestart(Vector2 p) => Contains(restartRt, p);
        public bool HitHandleOrRuler(Vector2 p) => ContainsExpanded(handleRt, p, 18) || Contains(rulerRt, p);
        public bool HitCutZone(Vector2 p) => cutAssistExpanded && game.State.onboarding ? true : cutAssistExpanded ? Contains(rulerRt, p) : ContainsExpanded(cutZoneRt, p, 88);
        public bool HitPiece(Vector2 p) => connectAssistExpanded && game.State.onboarding || ContainsExpanded(pieceRt, p, 32);
        public bool ReleasePiece(Vector2 p, bool assistGesture)
        {
            bool hit = assistGesture && connectAssistExpanded || ContainsExpanded(gapHitRt, p, 42);
            if (hit && assistGesture) pieceRt.anchoredPosition = CenterLocal(gapHitRt);
            if (!hit) pieceRt.anchoredPosition = pieceHome;
            return hit;
        }
        public bool ValidCutSwipe(Vector2 a, Vector2 b)
        {
            Rect r = ScreenRect(cutZoneRt); bool horizontalCross = (a.x < r.xMin && b.x > r.xMax) || (b.x < r.xMin && a.x > r.xMax);
            bool verticalCross = (a.y < r.yMin && b.y > r.yMax) || (b.y < r.yMin && a.y > r.yMax);
            if (cutAssistExpanded)
            {
                if (game.State.onboarding) return Vector2.Distance(a, b) >= 45f;
                Rect ruler = ScreenRect(rulerRt);
                if (ruler.Contains(a) && ruler.Contains(b)) return isWide ? Mathf.Abs(a.y - b.y) >= 45f : Mathf.Abs(a.x - b.x) >= 45f;
            }
            return isWide ? horizontalCross || verticalCross : horizontalCross;
        }

        void SetCutAssist(bool visible, bool expanded)
        {
            cutAssistExpanded = visible && expanded;
            cutAssistRt.gameObject.SetActive(visible);
            if (!visible) return;
            cutAssistRt.sizeDelta = new Vector2(expanded ? 326 : 176, 82);
            cutAssistLaneImg.rectTransform.sizeDelta = new Vector2(expanded ? 302 : 152, expanded ? 28 : 18);
            cutAssistLaneImg.color = new Color(.94f, .31f, .47f, expanded ? .52f : .3f);
        }

        public int LengthFromScreen(Vector2 screen)
        {
            Rect r = ScreenRect(rulerRt);
            float t = isWide ? Mathf.InverseLerp(r.yMin + 22, r.yMax - 22, screen.y) : Mathf.InverseLerp(r.xMin + 22, r.xMax - 22, screen.x);
            return Mathf.Clamp(Mathf.RoundToInt(1 + t * 23), 1, 24);
        }

        public void SetPulledLength(int cm, bool animated)
        {
            cm = Mathf.Clamp(cm, 1, 24); float t = (cm - 1) / 23f;
            // 자는 가로 로컬을 그대로 돌린다. 가로 화면에서도 눈금은 로컬 x축이므로
            // 손잡이를 y축으로 옮기면 눈금과 읽기값이 어긋난다.
            handleRt.anchoredPosition = new Vector2(Mathf.Lerp(-158, 158, t), -14);
            handleLabel.text = cm.ToString(); readoutText.text = cm + " cm";
            SyncTestGeometry();
            if (animated) { handleRt.localScale = new Vector3(1.08f, .92f, 1); MgfSfx.Play("tap", .08f); }
        }

        public void PressStart() { buoyPress = .18f; titleStartRt.localScale = new Vector3(.88f, 1.12f, 1); }
        public void PressHandle() { handleRt.localScale = new Vector3(1.14f, .86f, 1); MgfFx.Punch(handleRt, .08f, .18f); }
        public void ReleaseHandle() { handleRt.localScale = Vector3.one; }
        public void PressCutter() { cutBladeImg.color = tangerine; cutBladeImg.rectTransform.localScale = new Vector3(1.5f, .85f, 1); }
        public void DragCutter(Vector2 p) { RectTransformUtility.ScreenPointToLocalPointInRectangle(rulerRt, p, null, out var local); cutBladeImg.rectTransform.anchoredPosition = new Vector2(local.x, -2); }
        public void ReleaseCutter() { cutBladeImg.color = coral; cutBladeImg.rectTransform.localScale = Vector3.one; cutBladeImg.rectTransform.anchoredPosition = new Vector2(0, -2); }
        public void PressPiece(Vector2 p)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, p, null, out var local); pieceDragOffset = pieceRt.anchoredPosition - local; pieceRt.localScale = new Vector3(1.05f, .88f, 1);
        }
        public void DragPiece(Vector2 p)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, p, null, out var local); pieceRt.anchoredPosition = local + pieceDragOffset;
        }

        public void Tick(float dt)
        {
            worldClock += dt; Relayout(false); AnimateWorld(dt); UpdateTitleGuide(dt); RefreshHud(false); UpdateGuide(dt); UpdateFeedback(dt);
        }

        void AnimateWorld(float dt)
        {
            for (int i = 0; i < dust.Count; i++)
            {
                var p = dust[i].position; p.y += dt * (.12f + (i % 5) * .025f); p.x += Mathf.Sin(worldClock * .55f + dustPhase[i]) * dt * .06f;
                if (p.y > 9.5f) p.y = .15f; dust[i].position = p;
            }
            for (int i = 0; i < pennants.Count; i++) pennants[i].localRotation = Quaternion.Euler(0, Mathf.Sin(worldClock * 1.15f + i) * 10f, Mathf.Sin(worldClock * .9f + i) * 8f);
            for (int i = 0; i < crabs.Count; i++)
            {
                float bob = Mathf.Sin(worldClock * 2f + i * .7f) * .035f;
                var p = crabs[i].position; p.x = -4.65f; p.y = .12f + bob;
                if (revealCorrect && game.CurrentFlow == GereulKkeonaeGame.Flow.Reveal)
                {
                    float k = Mathf.Clamp01(revealVisualClock / 1.25f); p.y += Mathf.Sin(k * Mathf.PI * 2f) * .12f;
                    crabs[i].localScale = Vector3.one * (.7f + Mathf.Sin(k * Mathf.PI) * .08f);
                }
                else crabs[i].localScale = Vector3.one * .7f;
                crabs[i].position = p; crabs[i].rotation = Quaternion.Euler(0, Mathf.Sin(worldClock * 1.3f + i) * 7f, Mathf.Sin(worldClock * 2.4f + i) * 2f);
            }
            reel.rotation = Quaternion.Euler(0, 0, worldClock * 5f);
            if (sail.gameObject.activeSelf) sail.localRotation = Quaternion.Euler(2 + Mathf.Sin(worldClock * .7f) * .65f, 0, -1 + Mathf.Sin(worldClock * .5f) * .6f);
            float pulse = 1f + Mathf.Sin(worldClock * 2.4f) * .055f; titleStartRt.localScale = Vector3.Lerp(titleStartRt.localScale, Vector3.one * pulse, dt * 6f);
            if (buoyPress > 0) buoyPress -= dt;
            if (game.CurrentFlow == GereulKkeonaeGame.Flow.Reveal)
            {
                revealVisualClock += dt;
                if (revealCorrect)
                {
                    float k = Mathf.Clamp01(revealVisualClock / 1.2f);
                    netRt.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * .035f);
                    tensionWave.color = new Color(.5f, .95f, .88f, (1f - k) * .9f);
                    tensionWave.rectTransform.anchoredPosition = Vector2.Lerp(vertexLabels[1].rectTransform.anchoredPosition, vertexLabels[(Mathf.FloorToInt(k * 3) + 1) % 3].rectTransform.anchoredPosition, Mathf.Repeat(k * 3, 1));
                    tensionWave.rectTransform.localScale = Vector3.one * (1f + k * 1.4f);
                    for (int i = 0; i < netEdges.Length; i++)
                    {
                        Color edge = Color.Lerp(ivory, foam, Mathf.Clamp01(k * 1.8f - i * .22f));
                        edge.a = netEdges[i].color.a; netEdges[i].color = edge;
                        netEdges[i].rectTransform.localScale = new Vector3(1f, 1f + Mathf.Sin(Mathf.Clamp01(k * 1.6f - i * .18f) * Mathf.PI) * 1.8f, 1f);
                    }
                    for (int i = 0; i < bridgePlanks.Count; i++)
                    {
                        float delay = Mathf.Clamp01(k * 1.45f - i * .045f);
                        Vector3 home = bridgeHomes[i];
                        bridgePlanks[i].localPosition = home + Vector3.up * (Mathf.Sin(delay * Mathf.PI) * .12f);
                        bridgePlanks[i].localRotation = Quaternion.Euler(0, Mathf.Lerp((i - 5) * .45f, 0f, delay), Mathf.Lerp(i % 2 == 0 ? -1.5f : 1.5f, 0f, delay));
                    }
                }
                else
                {
                    float sag = Mathf.Sin(Mathf.Clamp01(revealVisualClock / .5f) * Mathf.PI) * (revealCut > (shownProblem == null ? 0 : shownProblem.answerCm) ? 16f : -12f);
                    pieceRt.localRotation = Quaternion.Euler(0, 0, sag * .2f); pieceRt.anchoredPosition = pieceHome + new Vector2(0, -Mathf.Abs(sag));
                }
            }
            else
            {
                netRt.localScale = Vector3.one; tensionWave.color = new Color(1, 1, 1, 0); pieceRt.localRotation = Quaternion.identity;
                for (int i = 0; i < netEdges.Length; i++)
                {
                    Color edge = ivory; edge.a = netEdges[i].color.a; netEdges[i].color = edge;
                    netEdges[i].rectTransform.localScale = Vector3.one;
                }
                for (int i = 0; i < bridgePlanks.Count; i++)
                {
                    bridgePlanks[i].localPosition = bridgeHomes[i];
                    bridgePlanks[i].localRotation = Quaternion.Euler(0, (i - 5) * .45f, i % 2 == 0 ? -1.5f : 1.5f);
                }
            }
        }

        void UpdateTitleGuide(float dt)
        {
            if (titleGroup.alpha <= 0) return;
            if (titleGuideStrongClock > 0) titleGuideStrongClock -= dt;
            Vector2 target = CenterLocal(titleStartRt);
            Vector2 start = titleGuideStrongClock > 0 ? titleGuideFrom : target + new Vector2(0, 112);
            float ph = Mathf.Repeat(worldClock / 1.15f, 1f);
            float move = Mathf.SmoothStep(0, 1, Mathf.Clamp01((ph - .08f) / .72f));
            titleGuideHandRt.anchoredPosition = Vector2.Lerp(start, target, move);
            titleGuideHandRt.localScale = Vector3.one * (ph < .12f ? .8f : 1f);
            titleGuideRingRt.anchoredPosition = target;
            titleGuideRingRt.localScale = Vector3.one * (.9f + ph * .16f);
            var ring = titleGuideRingRt.GetComponent<Image>(); ring.color = new Color(1f, .85f, .48f, (1f - ph) * .32f);
            for (int i = 0; i < titleGuideDots.Length; i++)
            {
                float f = (i + 1f) / (titleGuideDots.Length + 1f);
                titleGuideDots[i].rectTransform.anchoredPosition = Vector2.Lerp(start, target, f);
                titleGuideDots[i].color = new Color(1f, .92f, .6f, .28f + .68f * Mathf.Repeat(f - worldClock * .75f, 1f));
            }
        }

        void RefreshHud(bool force)
        {
            int sec = Mathf.CeilToInt(game.TimeLeft);
            if (force || sec != lastSecond) { lastSecond = sec; hudTime.text = sec + "초"; }
            if (force || game.State.score != lastScore)
            {
                lastScore = game.State.score; if (shownScore > lastScore) shownScore = lastScore;
                MgfFx.Punch(hudScore.transform, .14f, .25f);
            }
            shownScore = Mathf.MoveTowards(shownScore, game.State.score, Time.unscaledDeltaTime * 900f); hudScore.text = Mathf.RoundToInt(shownScore) + "점";
            hudRound.text = game.State.onboarding ? "연습" : "그물 " + Mathf.Max(1, game.State.round) + "/8";
            if (force || game.State.lives != lastLives)
            {
                lastLives = game.State.lives;
                for (int i = 0; i < 3; i++) { buoyIcons[i].color = i < game.State.lives ? tangerine : new Color(.3f, .5f, .65f, .35f); if (i == game.State.lives) MgfFx.Punch(buoyIcons[i].transform, .35f, .28f); }
            }
        }

        void UpdateFeedback(float dt)
        {
            if (toastClock > 0) { toastClock -= dt; toastText.transform.parent.gameObject.SetActive(true); }
            else toastText.transform.parent.gameObject.SetActive(false);
            if (rippleClock > 0)
            {
                rippleClock -= dt; float k = 1f - rippleClock / .42f; rippleRt.localScale = Vector3.one * (.5f + k * 1.5f); rippleImg.color = new Color(1f, .76f, .28f, (1 - k) * .8f);
            }
            else rippleImg.color = new Color(1, 1, 1, 0);
            if (endGroup.alpha > 0)
            {
                endClock += dt; float shown = Mathf.MoveTowards(float.Parse(endScore.text.Replace("점", "")), game.State.score, dt * 1000f); endScore.text = Mathf.RoundToInt(shown) + "점";
                restartRt.localScale = Vector3.one * (1f + Mathf.Sin(endClock * 3f) * .04f);
            }
        }

        void UpdateGuide(float dt)
        {
            if (playGroup.alpha <= 0 || game.CurrentFlow == GereulKkeonaeGame.Flow.Reveal || game.CurrentFlow == GereulKkeonaeGame.Flow.End) { SetGroup(guideGroup, false); return; }
            guideClock += dt;
            bool show = game.State.onboarding || guideStrong || guideClock < 4f;
            if (!show) { SetGroup(guideGroup, false); return; }
            SetGroup(guideGroup, true);
            Vector2 a, b;
            if (game.CurrentStep == GereulKkeonaeGame.ToolStep.Pull)
            {
                a = CenterLocal(handleRt); int target = game.State.onboarding && shownProblem != null ? shownProblem.answerCm : Mathf.Clamp(game.State.drawnLength + 5, 1, 24);
                b = RulerPointLocal(target);
            }
            else if (game.CurrentStep == GereulKkeonaeGame.ToolStep.Cut)
            {
                Vector2 c = CenterLocal(cutZoneRt); float reach = cutAssistExpanded ? 146f : 74f;
                if (isWide) { a = c + new Vector2(0, -reach); b = c + new Vector2(0, reach); }
                else { a = c + new Vector2(-reach, 0); b = c + new Vector2(reach, 0); }
            }
            else { a = CenterLocal(pieceRt); b = CenterLocal(gapHitRt); }
            float ph = Mathf.Repeat(guideClock / 1.2f, 1f); float move = Mathf.SmoothStep(0, 1, Mathf.Clamp01((ph - .12f) / .7f));
            guideHandRt.anchoredPosition = Vector2.Lerp(a, b, move); guideHandRt.localScale = Vector3.one * (guideStrong ? 1.25f : 1f) * (ph < .15f ? .82f : 1f);
            guideRingRt.anchoredPosition = b; guideRingRt.localScale = Vector3.one * (.8f + ph * .65f); guideRingImg.color = new Color(1f, .78f, .3f, 1f - ph);
            for (int i = 0; i < guideDots.Length; i++) { float f = (i + 1f) / (guideDots.Length + 1); guideDots[i].rectTransform.anchoredPosition = Vector2.Lerp(a, b, f); guideDots[i].color = new Color(1f, .9f, .55f, .25f + .7f * Mathf.Repeat(f - guideClock * .7f, 1)); }
            if (guideClock > 8f) guideStrong = true;
        }

        void DrawNet(NetProblem p)
        {
            Vector2[] basePts = { new Vector2(0, 105), new Vector2(-125, -75), new Vector2(125, -75), new Vector2(0, -75) };
            float ang = p.rotation * 90f * Mathf.Deg2Rad;
            Vector2 Rot(Vector2 v) => new Vector2(v.x * Mathf.Cos(ang) - v.y * Mathf.Sin(ang), v.x * Mathf.Sin(ang) + v.y * Mathf.Cos(ang));
            Vector2 A = Rot(basePts[0]), B = Rot(basePts[1]), C = Rot(basePts[2]), D = Rot(basePts[3]);
            PlaceLine(netEdges[0].rectTransform, A, B, p.repairKind == "AC" ? .18f : 1f);
            PlaceLine(netEdges[1].rectTransform, A, C, p.repairKind == "AC" ? .18f : 1f);
            PlaceLine(netEdges[2].rectTransform, B, C, p.repairKind == "BC" ? .18f : 1f);
            PlaceLine(bisectorLine.rectTransform, A, D, p.band >= 2 ? 1f : 0f);
            vertexLabels[0].text = "A"; vertexLabels[1].text = "B"; vertexLabels[2].text = "C"; vertexLabels[3].text = "D";
            vertexLabels[0].rectTransform.anchoredPosition = A + Rot(new Vector2(0, 22));
            vertexLabels[1].rectTransform.anchoredPosition = B + Rot(new Vector2(-18, -15));
            vertexLabels[2].rectTransform.anchoredPosition = C + Rot(new Vector2(18, -15));
            vertexLabels[3].rectTransform.anchoredPosition = D + Rot(new Vector2(0, -20)); vertexLabels[3].gameObject.SetActive(p.band >= 2);
            Vector2 gapA, gapB;
            if (p.repairKind == "AC") { gapA = A; gapB = C; }
            else if (p.repairKind == "BD") { gapA = B; gapB = D; }
            else { gapA = B; gapB = C; }
            gapHitRt.anchoredPosition = (gapA + gapB) * .5f; gapHitRt.sizeDelta = new Vector2(Mathf.Max(100, Vector2.Distance(gapA, gapB) * .7f), 84);
            // 점선은 짧은 상아 조각으로 표현한다.
            for (int i = 0; i < netTicks.Length; i++)
            {
                float f = (i + 1f) / 5f; netTicks[i].rectTransform.anchoredPosition = Vector2.Lerp(gapA, gapB, f);
                netTicks[i].rectTransform.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2((gapB-gapA).y, (gapB-gapA).x) * Mathf.Rad2Deg);
            }
        }

        void PlaceLine(RectTransform rt, Vector2 a, Vector2 b, float alpha)
        {
            Vector2 d = b - a; rt.anchoredPosition = (a + b) * .5f; rt.sizeDelta = new Vector2(d.magnitude, rt.sizeDelta.y <= 0 ? 6 : rt.sizeDelta.y); rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var im = rt.GetComponent<Image>(); Color c = im.color; c.a = alpha; im.color = c;
        }

        void Relayout(bool force)
        {
            if (!force && lastW == Screen.width && lastH == Screen.height) return;
            lastW = Screen.width; lastH = Screen.height;
            isWide = MgfBridge.IsLandLayout;
            game.State.layout = isWide ? "land" : "portrait";
            float w = canvasRt.rect.width, h = canvasRt.rect.height;
            if (isWide)
            {
                // 가로는 좌측 60% 문제·삼각형, 우측 30% 자·조작으로 분리한다.
                // 각 요소는 자기 영역의 정규화 anchor에만 묶여 1280~2000px에서 서로 넘지 않는다.
                cam.orthographicSize = 7.15f;
                hudRt.anchorMin = hudRt.anchorMax = new Vector2(.5f, 1f); hudRt.anchoredPosition = new Vector2(0, -20); hudRt.sizeDelta = new Vector2(Mathf.Min(600, w - 24), 40);
                goalRt.anchorMin = goalRt.anchorMax = new Vector2(.5f, 1f); goalRt.anchoredPosition = new Vector2(0, -55); goalRt.sizeDelta = new Vector2(Mathf.Min(600, w - 24), 28);

                promptBackRt.anchorMin = promptBackRt.anchorMax = new Vector2(.30f, .76f); promptBackRt.anchoredPosition = Vector2.zero; promptBackRt.sizeDelta = new Vector2(Mathf.Min(370, w * .59f), 84);
                promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = new Vector2(.30f, .76f); promptText.rectTransform.anchoredPosition = Vector2.zero; promptText.rectTransform.sizeDelta = new Vector2(Mathf.Min(352, w * .56f), 76); promptText.fontSize = 13;
                netRt.anchorMin = netRt.anchorMax = new Vector2(.30f, .38f); netRt.anchoredPosition = Vector2.zero; netRt.localScale = Vector3.one;
                netPlate.rectTransform.sizeDelta = new Vector2(Mathf.Min(350, w * .58f), 220);
                givenText.rectTransform.anchorMin = givenText.rectTransform.anchorMax = new Vector2(.5f, .5f); givenText.rectTransform.anchoredPosition = new Vector2(0, -118); givenText.rectTransform.sizeDelta = new Vector2(Mathf.Min(338, w * .55f), 38); givenText.fontSize = 12;
                proofText.rectTransform.anchorMin = proofText.rectTransform.anchorMax = new Vector2(.30f, .075f); proofText.rectTransform.anchoredPosition = Vector2.zero; proofText.rectTransform.sizeDelta = new Vector2(Mathf.Min(355, w * .58f), 42); proofText.fontSize = 12;

                rulerRt.anchorMin = rulerRt.anchorMax = new Vector2(.82f, .48f); rulerRt.anchoredPosition = Vector2.zero; rulerRt.localRotation = Quaternion.Euler(0, 0, 90); rulerRt.localScale = Vector3.one * .78f; rulerRt.sizeDelta = new Vector2(360, 142);
                handleLabel.rectTransform.localRotation = Quaternion.Euler(0, 0, -90); readoutText.rectTransform.localRotation = Quaternion.Euler(0, 0, -90); readoutText.rectTransform.anchoredPosition = new Vector2(0, -56);
                for (int i = 0; i < rulerLabels.Count; i++) rulerLabels[i].rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
                stepText.rectTransform.anchorMin = stepText.rectTransform.anchorMax = new Vector2(.82f, .09f); stepText.rectTransform.anchoredPosition = Vector2.zero; stepText.rectTransform.sizeDelta = new Vector2(Mathf.Min(220, w * .32f), 45); stepText.fontSize = 12;
                pieceRt.anchorMin = pieceRt.anchorMax = new Vector2(.82f, .13f); pieceHome = Vector2.zero; pieceRt.anchoredPosition = pieceHome;
            }
            else
            {
                cam.orthographicSize = 9.5f;
                hudRt.anchorMin = hudRt.anchorMax = new Vector2(.5f, 1f); hudRt.anchoredPosition = new Vector2(0, -29); hudRt.sizeDelta = new Vector2(Mathf.Min(374, w - 16), 52);
                goalRt.anchorMin = goalRt.anchorMax = new Vector2(.5f, 1f); goalRt.anchoredPosition = new Vector2(0, -76); goalRt.sizeDelta = new Vector2(Mathf.Min(374, w - 16), 38);
                netRt.anchorMin = netRt.anchorMax = new Vector2(.5f, .54f); netRt.anchoredPosition = new Vector2(0, -8); netRt.localScale = Vector3.one; netPlate.rectTransform.sizeDelta = new Vector2(Mathf.Min(374, w - 12), 318);
                givenText.rectTransform.anchorMin = givenText.rectTransform.anchorMax = new Vector2(.5f, .5f); givenText.rectTransform.anchoredPosition = new Vector2(0, -132); givenText.rectTransform.sizeDelta = new Vector2(Mathf.Min(356, w - 24), 40); givenText.fontSize = 13;
                rulerRt.anchorMin = rulerRt.anchorMax = new Vector2(.5f, 0); rulerRt.anchoredPosition = new Vector2(0, 82); rulerRt.sizeDelta = new Vector2(360, 142); rulerRt.localRotation = Quaternion.identity; rulerRt.localScale = Vector3.one; handleLabel.rectTransform.localRotation = Quaternion.identity; readoutText.rectTransform.localRotation = Quaternion.identity; readoutText.rectTransform.anchoredPosition = new Vector2(0, 51);
                for (int i = 0; i < rulerLabels.Count; i++) rulerLabels[i].rectTransform.localRotation = Quaternion.identity;
                promptBackRt.anchorMin = promptBackRt.anchorMax = new Vector2(.5f, 1); promptBackRt.anchoredPosition = new Vector2(0, -143); promptBackRt.sizeDelta = new Vector2(Mathf.Min(378, w - 8), 116);
                promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = new Vector2(.5f, 1); promptText.rectTransform.anchoredPosition = new Vector2(0, -143); promptText.rectTransform.sizeDelta = new Vector2(Mathf.Min(362, w - 24), 108); promptText.fontSize = 14;
                proofText.rectTransform.anchorMin = proofText.rectTransform.anchorMax = new Vector2(.5f, .34f); proofText.rectTransform.anchoredPosition = Vector2.zero; proofText.rectTransform.sizeDelta = new Vector2(Mathf.Min(370, w - 16), 58);
                proofText.fontSize = 13;
                stepText.rectTransform.anchorMin = stepText.rectTransform.anchorMax = new Vector2(.5f, 0); stepText.rectTransform.anchoredPosition = new Vector2(0, 168); stepText.rectTransform.sizeDelta = new Vector2(Mathf.Min(370, w - 16), 38); stepText.fontSize = 14;
                pieceHome = new Vector2(0, -h * .27f); pieceRt.anchorMin = pieceRt.anchorMax = new Vector2(.5f, .5f); pieceRt.anchoredPosition = pieceHome;
            }
            if (shownProblem != null) { DrawNet(shownProblem); SetPulledLength(game.State.drawnLength, false); }
            SyncTestGeometry();
        }

        void SyncTestGeometry()
        {
            if (!handleRt || !rulerRt || !cutZoneRt || !pieceRt || !gapHitRt) return;
            Vector2 handle = ScreenCenter(handleRt), cut = ScreenCenter(cutZoneRt);
            Vector2 piece = ScreenCenter(pieceRt), gap = ScreenCenter(gapHitRt);
            Rect ruler = ScreenRect(rulerRt);
            game.State.handleX = Mathf.RoundToInt(handle.x); game.State.handleY = Mathf.RoundToInt(handle.y);
            game.State.rulerMin = Mathf.RoundToInt(isWide ? ruler.yMin + 22 : ruler.xMin + 22);
            game.State.rulerMax = Mathf.RoundToInt(isWide ? ruler.yMax - 22 : ruler.xMax - 22);
            game.State.cutX = Mathf.RoundToInt(cut.x); game.State.cutY = Mathf.RoundToInt(cut.y);
            game.State.pieceX = Mathf.RoundToInt(piece.x); game.State.pieceY = Mathf.RoundToInt(piece.y);
            game.State.gapX = Mathf.RoundToInt(gap.x); game.State.gapY = Mathf.RoundToInt(gap.y);
        }

        static void SetGroup(CanvasGroup g, bool on) { g.alpha = on ? 1f : 0f; g.interactable = on; g.blocksRaycasts = false; }
        static bool Contains(RectTransform rt, Vector2 screen) => rt && RectTransformUtility.RectangleContainsScreenPoint(rt, screen, null);
        static bool ContainsExpanded(RectTransform rt, Vector2 screen, float expand)
        {
            Rect r = ScreenRect(rt); r.xMin -= expand; r.xMax += expand; r.yMin -= expand; r.yMax += expand; return r.Contains(screen);
        }
        static Rect ScreenRect(RectTransform rt)
        {
            var c = new Vector3[4]; rt.GetWorldCorners(c); Vector2 a = RectTransformUtility.WorldToScreenPoint(null, c[0]), b = RectTransformUtility.WorldToScreenPoint(null, c[2]); return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }
        static Vector2 ScreenCenter(RectTransform rt) => RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
        Vector2 CenterLocal(RectTransform rt)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center)); RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var local); return local;
        }

        Vector2 RulerPointLocal(int cm)
        {
            float t = (Mathf.Clamp(cm, 1, 24) - 1) / 23f;
            Vector3 worldPoint = rulerRt.TransformPoint(new Vector3(Mathf.Lerp(-158, 158, t), -14, 0));
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var local);
            return local;
        }
    }
}
