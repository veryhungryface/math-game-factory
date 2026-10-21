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
        readonly Color cobalt = MgfLook.Hex("#2868C7");
        readonly Color cobaltDeep = MgfLook.Hex("#123B79");
        readonly Color tangerine = MgfLook.Hex("#F59B3D");
        readonly Color ivory = MgfLook.Hex("#FFF7E6");
        readonly Color ink = MgfLook.Hex("#173457");
        readonly Color foam = MgfLook.Hex("#B7E1E1");
        readonly Color coral = MgfLook.Hex("#FF7B68");

        Camera cam;
        Transform world;
        Transform sail;
        readonly List<Transform> bubbles = new List<Transform>();
        readonly List<float> bubblePhase = new List<float>();
        readonly List<Transform> crabs = new List<Transform>();
        readonly List<Transform> kelp = new List<Transform>();
        Transform reel;
        Transform titleBuoy;
        float worldClock;

        RectTransform canvasRt;
        CanvasGroup titleGroup, playGroup, revealGroup, endGroup, guideGroup;
        RectTransform titleStartRt, restartRt, rulerRt, handleRt, cutZoneRt, pieceRt, gapHitRt, netRt;
        RectTransform rippleRt, guideHandRt, guideRingRt;
        Image titleBuoyImg, handleImg, cutBladeImg, pieceImg, rippleImg, guideRingImg;
        readonly Image[] guideDots = new Image[7];
        readonly Image[] buoyIcons = new Image[3];
        readonly Image[] netEdges = new Image[3];
        readonly Image[] netTicks = new Image[4];
        Image bisectorLine, tensionWave, netPlate;
        readonly TMP_Text[] vertexLabels = new TMP_Text[4];
        TMP_Text titleBest, hudRound, hudScore, hudTime, goalText, promptText, readoutText, stepText;
        TMP_Text givenText, proofText, toastText, revealTitle, revealBody, endTitle, endStats, endScore;
        TMP_Text pieceLabel, handleLabel;
        float toastClock, rippleClock, guideClock, revealVisualClock, endClock, shownScore;
        bool guideStrong, isWide;
        Vector2 pieceHome, pieceDragOffset;
        int lastW, lastH, lastScore = -1, lastSecond = -1, lastLives = -1;
        NetProblem shownProblem;
        bool revealCorrect;
        int revealCut;
        float buoyPress;

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
            MgfLook.Sky(MgfLook.Hex("#66CFF2"), cobalt, MgfLook.Hex("#082A5C"), .82f);
            MgfLook.Sun(new Vector3(38, -28, -18), new Color(.8f, .95f, 1f), 1.15f, .55f);
            cam = MgfLook.Camera(new Vector3(0, 8.5f, -19f), new Vector3(0, 2.4f, 1.2f), 38f);
            cam.orthographic = true; cam.orthographicSize = 8.5f; cam.backgroundColor = cobalt;
            world = new GameObject("CobaltCoralRescueWorld").transform;

            var floor = MgfLook.Prim(PrimitiveType.Plane, "Seafloor", new Vector3(0, -.7f, 4f), new Vector3(5.2f, 1, 5.2f), MgfLook.Lit(MgfLook.Hex("#1760A0"), .24f), world, false);
            floor.GetComponent<Renderer>().receiveShadows = true;
            var back = MgfLook.Prim(PrimitiveType.Quad, "DeepWater", new Vector3(0, 8f, 14f), new Vector3(42, 25, 1), MgfLook.Unlit(cobalt), world, false);
            back.transform.rotation = Quaternion.Euler(0, 180, 0);

            var rock = MgfLook.Lit(MgfLook.Hex("#466C8E"), .2f, 0f);
            var rockLight = MgfLook.Lit(MgfLook.Hex("#7098AA"), .23f, 0f);
            var coralMat = MgfLook.Lit(coral, .3f, 0f);
            var blueCoral = MgfLook.Lit(MgfLook.Hex("#225A89"), .2f, 0f);
            var shell = MgfLook.Lit(ivory, .45f, .05f);
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 9; i++)
            {
                float x = side * (6.0f + (i % 3) * .75f), z = -1f + i * 1.55f;
                var r = MgfLook.Prim(PrimitiveType.Sphere, "ScallopedCliff", new Vector3(x, .25f + (i % 2) * .45f, z), new Vector3(2.2f, 1.2f + (i % 3) * .3f, 1.7f), i % 2 == 0 ? rock : rockLight, world, false);
                r.transform.rotation = Quaternion.Euler(0, i * 27, side * 5);
                for (int j = 0; j < 3; j++)
                {
                    var c = MgfLook.Prim(PrimitiveType.Sphere, "CoralKnob", new Vector3(x + side * (.25f + j * .25f), 1.05f + j * .28f, z - .45f + j * .32f), Vector3.one * (.28f + j * .05f), j == 1 ? coralMat : blueCoral, world, false);
                    c.transform.localScale = new Vector3(.5f, 1.5f, .5f) * (.55f + j * .08f);
                }
            }
            for (int i = 0; i < 7; i++)
            {
                float x = -5.4f + i * 1.8f;
                var s = MgfLook.Prim(PrimitiveType.Sphere, "Shell", new Vector3(x, -.25f, 1f + (i % 3) * 1.9f), new Vector3(.7f, .18f, .55f), shell, world, false);
                s.transform.rotation = Quaternion.Euler(0, i * 41, 0);
            }
            BuildKelp(); BuildBubbles(); BuildReel(); BuildSail();
            titleBuoy = new GameObject("WorldBuoy").transform; titleBuoy.SetParent(world, false); titleBuoy.position = new Vector3(0, .3f, -1.2f);
            var buoyMat = MgfLook.Lit(tangerine, .42f, .02f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * .25f;
                MgfLook.Prim(PrimitiveType.Sphere, "BuoyArc", new Vector3(Mathf.Cos(a) * .72f, .18f, Mathf.Sin(a) * .72f), new Vector3(.48f, .18f, .48f), buoyMat, titleBuoy, false);
            }
            for (int i = 0; i < 8; i++) crabs.Add(BuildCrab(new Vector3(-4.8f + (i % 4) * 3.2f, .15f, 3.6f + (i / 4) * 3.8f), .62f + (i % 3) * .06f));
        }

        void BuildKelp()
        {
            var mat = MgfLook.Lit(MgfLook.Hex("#2F8A77"), .25f);
            for (int i = 0; i < 12; i++)
            {
                float x = -8f + i * 1.45f;
                var k = MgfLook.Prim(PrimitiveType.Capsule, "Kelp", new Vector3(x, .65f, 4.5f + (i % 4) * 2f), new Vector3(.18f, .95f + (i % 3) * .2f, .18f), mat, world, false).transform;
                k.rotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? -1 : 1) * 10f); kelp.Add(k);
            }
        }

        void BuildBubbles()
        {
            var mat = MgfLook.Alpha(new Color(.82f, .98f, 1f, .38f));
            var rng = new System.Random(17);
            for (int i = 0; i < 22; i++)
            {
                float s = .08f + (float)rng.NextDouble() * .16f;
                var b = MgfLook.Prim(PrimitiveType.Sphere, "Bubble", new Vector3(-8f + (float)rng.NextDouble() * 16f, (float)rng.NextDouble() * 9f, 2f + (float)rng.NextDouble() * 8f), Vector3.one * s, mat, world, false).transform;
                bubbles.Add(b); bubblePhase.Add((float)rng.NextDouble() * 8f);
            }
        }

        void BuildReel()
        {
            var ceramic = MgfLook.Lit(ivory, .5f, .08f);
            var cordTex = Resources.Load<Texture2D>("GereulKkeonae/cord-fiber");
            if (cordTex) { cordTex.wrapMode = TextureWrapMode.Repeat; cordTex.filterMode = FilterMode.Bilinear; }
            var cordMat = MgfLook.Lit(ivory, .15f, 0f, null, cordTex);
            reel = new GameObject("CeramicMeasuringReel").transform; reel.SetParent(world, false); reel.position = new Vector3(4.9f, .35f, .6f);
            MgfLook.Prim(PrimitiveType.Cylinder, "ReelBody", Vector3.zero, new Vector3(1.15f, .38f, 1.15f), ceramic, reel, false).transform.rotation = Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < 10; i++)
            {
                var coil = MgfLook.Prim(PrimitiveType.Cylinder, "CordCoil", new Vector3(0, 0, -.36f + i * .08f), new Vector3(.82f - i * .018f, .045f, .82f - i * .018f), cordMat, reel, false);
                coil.transform.rotation = Quaternion.Euler(90, 0, 0);
            }
        }

        Transform BuildCrab(Vector3 pos, float scale)
        {
            var root = new GameObject("CoralCrab").transform; root.SetParent(world, false); root.position = pos; root.localScale = Vector3.one * scale;
            var orange = MgfLook.Lit(tangerine, .38f, .02f);
            var cream = MgfLook.Lit(MgfLook.Hex("#FFE0A8"), .35f);
            MgfLook.Prim(PrimitiveType.Sphere, "PebbleBody", new Vector3(0, .48f, 0), new Vector3(1.15f, .62f, .8f), orange, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Belly", new Vector3(0, .35f, -.38f), new Vector3(.65f, .35f, .16f), cream, root, false);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int j = 0; j < 2; j++) MgfLook.Prim(PrimitiveType.Capsule, "Leg", new Vector3(side * (.65f + j * .18f), .2f, .05f + j * .24f), new Vector3(.12f, .38f, .12f), orange, root, false).transform.rotation = Quaternion.Euler(0, 0, side * 58);
                MgfLook.Prim(PrimitiveType.Sphere, "Claw", new Vector3(side * 1.02f, .65f, -.05f), new Vector3(.55f + (side > 0 ? .15f : 0), .45f, .3f), orange, root, false);
                MgfLook.Prim(PrimitiveType.Sphere, "Eye", new Vector3(side * .3f, .95f, -.18f), Vector3.one * .16f, MgfLook.Lit(ink, .6f), root, false);
            }
            return root;
        }

        void BuildSail()
        {
            sail = new GameObject("ShipwreckSail").transform; sail.SetParent(world, false); sail.position = new Vector3(0, 6.7f, 5.8f);
            var cloth = MgfLook.Block("BentSail", Vector3.zero, new Vector3(5.9f, 2.35f, .12f), .38f, MgfLook.Lit(ivory, .15f), sail);
            UnityEngine.Object.Destroy(cloth.GetComponent<Collider>());
            sail.rotation = Quaternion.Euler(4, 0, -2);
            var shadow = MgfText.World("게를 꺼내", new Vector3(.08f, .05f, -.12f), 7.8f, new Color(.05f, .15f, .27f, .48f), sail);
            var logo = MgfText.World("게를 꺼내", new Vector3(0, .12f, -.14f), 7.8f, ink, sail);
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
            titleBuoyImg = Img("FloatingStartBuoy", titleGroup.transform, new Color(1f, .62f, .23f, .98f), new Vector2(116, 116), new Vector2(.5f, .18f), Vector2.zero, true);
            titleStartRt = titleBuoyImg.rectTransform;
            Img("BuoyHole", titleBuoyImg.transform, new Color(.1f, .28f, .48f, .96f), new Vector2(54, 54), new Vector2(.5f, .5f), Vector2.zero, true);
            Txt("BuoyVerb", titleBuoyImg.transform, "구조 시작", 17, ivory, new Vector2(130, 34), new Vector2(.5f, .5f), new Vector2(0, -78));
            Txt("TitleTagline", titleGroup.transform, "줄을 재서 잘라라", 19, ivory, new Vector2(300, 38), new Vector2(.5f, .5f), new Vector2(0, 8));
            Txt("TitleGrade", titleGroup.transform, "중학교 2학년 · 삼각형의 성질", 12, new Color(1, 1, 1, .88f), new Vector2(300, 28), new Vector2(.5f, .06f), Vector2.zero);
            titleBest = Txt("TitleBest", titleGroup.transform, "", 12, ivory, new Vector2(260, 26), new Vector2(.5f, .11f), Vector2.zero);
        }

        void BuildPlayUi()
        {
            var hud = Img("HudRibbon", playGroup.transform, new Color(.04f, .16f, .28f, .88f), new Vector2(374, 52), new Vector2(.5f, 1), new Vector2(0, -29));
            hudRound = Txt("Round", hud.transform, "그물 1/8", 15, ivory, new Vector2(110, 36), new Vector2(.16f, .5f), Vector2.zero);
            hudScore = Txt("Score", hud.transform, "0점", 15, ivory, new Vector2(90, 36), new Vector2(.5f, .5f), Vector2.zero);
            hudTime = Txt("Time", hud.transform, "120초", 15, ivory, new Vector2(90, 36), new Vector2(.82f, .5f), Vector2.zero);
            for (int i = 0; i < 3; i++) buoyIcons[i] = Img("SafetyBuoy" + i, hud.transform, tangerine, new Vector2(22, 22), new Vector2(.68f + i * .065f, .5f), Vector2.zero, true);
            var goal = Img("GoalLine", playGroup.transform, new Color(.95f, .98f, .9f, .96f), new Vector2(374, 38), new Vector2(.5f, 1), new Vector2(0, -76));
            goalText = Txt("Goal", goal.transform, "줄을 맞게 잘라 게를 꺼내시오", 14, ink, new Vector2(360, 34), new Vector2(.5f, .5f), Vector2.zero);

            netRt = (RectTransform)new GameObject("GeometryNet", typeof(RectTransform)).transform; netRt.SetParent(playGroup.transform, false);
            netPlate = Img("NetWaterPlate", netRt, new Color(.08f, .36f, .68f, .72f), new Vector2(360, 310), new Vector2(.5f, .5f), Vector2.zero);
            for (int i = 0; i < 3; i++) netEdges[i] = Img("NetEdge" + i, netRt, ivory, new Vector2(190, 6), new Vector2(.5f, .5f), Vector2.zero);
            bisectorLine = Img("Bisector", netRt, new Color(.72f, .91f, .91f, .95f), new Vector2(100, 4), new Vector2(.5f, .5f), Vector2.zero);
            tensionWave = Img("TensionWave", netRt, new Color(1f, .78f, .25f, 0), new Vector2(20, 20), new Vector2(.5f, .5f), Vector2.zero, true);
            for (int i = 0; i < 4; i++) vertexLabels[i] = Txt("Vertex" + i, netRt, "ABCD"[i].ToString(), 20, ivory, new Vector2(36, 30), new Vector2(.5f, .5f), Vector2.zero);
            for (int i = 0; i < 4; i++) netTicks[i] = Img("EqualMark" + i, netRt, tangerine, new Vector2(4, 20), new Vector2(.5f, .5f), Vector2.zero);
            givenText = Txt("Given", netRt, "", 17, ivory, new Vector2(340, 58), new Vector2(.5f, .1f), Vector2.zero);
            promptText = Txt("Prompt", playGroup.transform, "", 16, ink, new Vector2(360, 74), new Vector2(.5f, .5f), Vector2.zero);
            proofText = Txt("Proof", playGroup.transform, "", 15, ink, new Vector2(360, 56), new Vector2(.5f, .5f), Vector2.zero);

            rulerRt = (RectTransform)new GameObject("MeasuringTool", typeof(RectTransform)).transform; rulerRt.SetParent(playGroup.transform, false);
            Img("CeramicRuler", rulerRt, new Color(.98f, .94f, .82f, .98f), new Vector2(360, 142), new Vector2(.5f, .5f), Vector2.zero);
            for (int i = 0; i < 24; i++)
            {
                float x = -158f + i * (316f / 23f);
                Img("Tick" + (i + 1), rulerRt, ink, new Vector2(i % 3 == 2 ? 3 : 2, i % 3 == 2 ? 24 : 14), new Vector2(.5f, .5f), new Vector2(x, -14));
                if (i % 3 == 2) Txt("TickLabel" + (i + 1), rulerRt, (i + 1).ToString(), 10, ink, new Vector2(28, 18), new Vector2(.5f, .5f), new Vector2(x, 18));
            }
            handleImg = Img("RoundKnotHandle", rulerRt, tangerine, new Vector2(66, 66), new Vector2(.5f, .5f), Vector2.zero, true); handleRt = handleImg.rectTransform;
            handleLabel = Txt("HandleLength", handleRt, "3", 18, ink, new Vector2(50, 30), new Vector2(.5f, .5f), Vector2.zero);
            cutBladeImg = Img("CutLine", rulerRt, coral, new Vector2(8, 86), new Vector2(.5f, .5f), new Vector2(0, -2)); cutZoneRt = cutBladeImg.rectTransform;
            pieceImg = Img("CutPiece", playGroup.transform, ivory, new Vector2(150, 18), new Vector2(.5f, .5f), Vector2.zero);
            pieceRt = pieceImg.rectTransform; pieceLabel = Txt("PieceLabel", pieceRt, "", 16, ink, new Vector2(90, 32), new Vector2(.5f, .5f), new Vector2(0, -26));
            readoutText = Txt("InstantReadout", rulerRt, "3 cm", 20, ink, new Vector2(150, 38), new Vector2(.5f, .5f), new Vector2(0, 51));
            stepText = Txt("StepInstruction", playGroup.transform, "매듭을 끌어 길이를 정하시오", 14, ivory, new Vector2(360, 34), new Vector2(.5f, .5f), Vector2.zero);
            gapHitRt = (RectTransform)new GameObject("RepairGapHit", typeof(RectTransform)).transform; gapHitRt.SetParent(netRt, false); gapHitRt.sizeDelta = new Vector2(110, 80);
            pieceRt.gameObject.SetActive(false);
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
            givenText.text = p.band == 1 ? "같은 밑각 ∠B = ∠C" : "AB = AC · AD는 꼭지각의 이등분선";
            SetPulledLength(game.State.drawnLength, false); pieceRt.gameObject.SetActive(false);
            stepText.text = "① 둥근 매듭을 끌어 줄 길이를 정하시오";
            DrawNet(p); guideClock = 0f; guideStrong = practice; RefreshHud(true); Relayout(true);
        }

        public void RetryProblem(NetProblem p, int length)
        {
            SetGroup(revealGroup, false); SetGroup(playGroup, true); SetPulledLength(length, false); pieceRt.gameObject.SetActive(false);
            stepText.text = "① 길이를 다시 정하시오"; guideClock = 0f; guideStrong = true; proofText.gameObject.SetActive(false); RefreshHud(true);
        }

        public void OnLengthSet(int length)
        {
            stepText.text = "② 붉은 절단선을 가로질러 스와이프하시오"; guideClock = 0f; guideStrong = true;
            MgfFx.Punch(readoutText.transform, .16f, .24f);
        }

        public void OnCut(int length)
        {
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
                revealTitle.text = "매듭이 맞았습니다";
                revealBody.text = p.evidence + "\n게가 줄을 타고 탈출합니다.";
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
            toastText.text = message; toastText.transform.parent.gameObject.SetActive(true); toastClock = 2.2f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var local); rippleRt.anchoredPosition = local;
            rippleClock = .42f; rippleImg.color = new Color(1f, .76f, .28f, .9f); rippleRt.localScale = Vector3.one * .5f;
            if (guide) { guideClock = 0f; guideStrong = true; SetGroup(guideGroup, true); }
            MgfFx.Punch(toastText.transform.parent, .08f, .2f);
        }

        public bool HitStart(Vector2 p) => Contains(titleStartRt, p);
        public bool HitRestart(Vector2 p) => Contains(restartRt, p);
        public bool HitHandleOrRuler(Vector2 p) => ContainsExpanded(handleRt, p, 18) || Contains(rulerRt, p);
        public bool HitCutZone(Vector2 p) => ContainsExpanded(cutZoneRt, p, 46);
        public bool HitPiece(Vector2 p) => ContainsExpanded(pieceRt, p, 32);
        public bool ReleasePiece(Vector2 p)
        {
            bool hit = ContainsExpanded(gapHitRt, p, 42);
            if (!hit) pieceRt.anchoredPosition = pieceHome;
            return hit;
        }
        public bool ValidCutSwipe(Vector2 a, Vector2 b)
        {
            Rect r = ScreenRect(cutZoneRt); bool horizontalCross = (a.x < r.xMin && b.x > r.xMax) || (b.x < r.xMin && a.x > r.xMax);
            bool verticalCross = (a.y < r.yMin && b.y > r.yMax) || (b.y < r.yMin && a.y > r.yMax);
            return isWide ? horizontalCross || verticalCross : horizontalCross;
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
            if (isWide) handleRt.anchoredPosition = new Vector2(0, Mathf.Lerp(-118, 118, t));
            else handleRt.anchoredPosition = new Vector2(Mathf.Lerp(-158, 158, t), -14);
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
            worldClock += dt; Relayout(false); AnimateWorld(dt); RefreshHud(false); UpdateGuide(dt); UpdateFeedback(dt);
        }

        void AnimateWorld(float dt)
        {
            for (int i = 0; i < bubbles.Count; i++)
            {
                var p = bubbles[i].position; p.y += dt * (.32f + (i % 5) * .055f); p.x += Mathf.Sin(worldClock * .7f + bubblePhase[i]) * dt * .08f;
                if (p.y > 10f) p.y = -.25f; bubbles[i].position = p;
            }
            for (int i = 0; i < kelp.Count; i++) kelp[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(worldClock * .75f + i) * 13f);
            for (int i = 0; i < crabs.Count; i++)
            {
                float bob = Mathf.Sin(worldClock * 2f + i * .7f) * .055f;
                var p = crabs[i].position; p.y = .15f + bob;
                if (revealCorrect && game.CurrentFlow == GereulKkeonaeGame.Flow.Reveal)
                {
                    float k = Mathf.Clamp01(revealVisualClock / 1.25f); p.y += Mathf.SmoothStep(0, 2.2f, k); p.x += Mathf.Sin(k * Mathf.PI) * .35f;
                }
                crabs[i].position = p; crabs[i].rotation = Quaternion.Euler(0, Mathf.Sin(worldClock * 1.3f + i) * 6f, Mathf.Sin(worldClock * 2.4f + i) * 3f);
            }
            reel.rotation = Quaternion.Euler(0, 0, worldClock * 8f);
            if (sail.gameObject.activeSelf) sail.localRotation = Quaternion.Euler(3 + Mathf.Sin(worldClock * .7f) * 1.3f, 0, -2 + Mathf.Sin(worldClock * .5f) * 1.2f);
            float pulse = 1f + Mathf.Sin(worldClock * 2.4f) * .055f; titleStartRt.localScale = Vector3.Lerp(titleStartRt.localScale, Vector3.one * pulse, dt * 6f);
            if (buoyPress > 0) buoyPress -= dt;
            if (game.CurrentFlow == GereulKkeonaeGame.Flow.Reveal)
            {
                revealVisualClock += dt;
                if (revealCorrect)
                {
                    float k = Mathf.Clamp01(revealVisualClock / 1.2f);
                    netRt.localScale = new Vector3(1f, Mathf.Cos(k * Mathf.PI * 2f) * .1f + .9f, 1f);
                    tensionWave.color = new Color(1f, .8f, .3f, (1f - k) * .9f);
                    tensionWave.rectTransform.anchoredPosition = Vector2.Lerp(vertexLabels[1].rectTransform.anchoredPosition, vertexLabels[(Mathf.FloorToInt(k * 3) + 1) % 3].rectTransform.anchoredPosition, Mathf.Repeat(k * 3, 1));
                    tensionWave.rectTransform.localScale = Vector3.one * (1f + k * 1.4f);
                }
                else
                {
                    float sag = Mathf.Sin(Mathf.Clamp01(revealVisualClock / .5f) * Mathf.PI) * (revealCut > (shownProblem == null ? 0 : shownProblem.answerCm) ? 16f : -12f);
                    pieceRt.localRotation = Quaternion.Euler(0, 0, sag * .2f); pieceRt.anchoredPosition = pieceHome + new Vector2(0, -Mathf.Abs(sag));
                }
            }
            else { netRt.localScale = Vector3.one; tensionWave.color = new Color(1, 1, 1, 0); pieceRt.localRotation = Quaternion.identity; }
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
                float t = (target - 1) / 23f; b = CenterLocal(rulerRt) + (isWide ? new Vector2(0, Mathf.Lerp(-118, 118, t)) : new Vector2(Mathf.Lerp(-158, 158, t), -14));
            }
            else if (game.CurrentStep == GereulKkeonaeGame.ToolStep.Cut)
            {
                Vector2 c = CenterLocal(cutZoneRt); a = c + new Vector2(-58, 0); b = c + new Vector2(58, 0);
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
            lastW = Screen.width; lastH = Screen.height; isWide = Screen.width >= 640 && (float)Screen.width / Mathf.Max(1, Screen.height) >= 1.05f;
            game.State.layout = isWide ? "land" : "portrait";
            float w = canvasRt.rect.width, h = canvasRt.rect.height;
            if (isWide)
            {
                cam.orthographicSize = 7.4f; netRt.anchorMin = netRt.anchorMax = new Vector2(.34f, .55f); netRt.anchoredPosition = new Vector2(0, -5); netPlate.rectTransform.sizeDelta = new Vector2(Mathf.Min(690, w * .58f), 500);
                rulerRt.anchorMin = rulerRt.anchorMax = new Vector2(.84f, .5f); rulerRt.anchoredPosition = Vector2.zero; rulerRt.localRotation = Quaternion.Euler(0, 0, 90); rulerRt.sizeDelta = new Vector2(360, 142);
                handleLabel.rectTransform.localRotation = Quaternion.Euler(0, 0, -90); readoutText.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
                promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = new Vector2(.34f, .89f); promptText.rectTransform.anchoredPosition = Vector2.zero; promptText.rectTransform.sizeDelta = new Vector2(Mathf.Min(690, w * .58f), 70);
                proofText.rectTransform.anchorMin = proofText.rectTransform.anchorMax = new Vector2(.34f, .17f); proofText.rectTransform.anchoredPosition = Vector2.zero; proofText.rectTransform.sizeDelta = new Vector2(Mathf.Min(690, w * .58f), 56);
                stepText.rectTransform.anchorMin = stepText.rectTransform.anchorMax = new Vector2(.84f, .17f); stepText.rectTransform.anchoredPosition = Vector2.zero; stepText.rectTransform.sizeDelta = new Vector2(Mathf.Min(350, w * .27f), 56);
                pieceHome = new Vector2(w * .34f, -h * .28f); pieceRt.anchorMin = pieceRt.anchorMax = new Vector2(.5f, .5f); pieceRt.anchoredPosition = pieceHome;
            }
            else
            {
                cam.orthographicSize = 9.5f; netRt.anchorMin = netRt.anchorMax = new Vector2(.5f, .55f); netRt.anchoredPosition = new Vector2(0, -18); netPlate.rectTransform.sizeDelta = new Vector2(Mathf.Min(374, w - 12), 318);
                rulerRt.anchorMin = rulerRt.anchorMax = new Vector2(.5f, 0); rulerRt.anchoredPosition = new Vector2(0, 82); rulerRt.localRotation = Quaternion.identity; handleLabel.rectTransform.localRotation = Quaternion.identity; readoutText.rectTransform.localRotation = Quaternion.identity;
                promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = new Vector2(.5f, 1); promptText.rectTransform.anchoredPosition = new Vector2(0, -130); promptText.rectTransform.sizeDelta = new Vector2(Mathf.Min(370, w - 16), 78);
                proofText.rectTransform.anchorMin = proofText.rectTransform.anchorMax = new Vector2(.5f, .34f); proofText.rectTransform.anchoredPosition = Vector2.zero; proofText.rectTransform.sizeDelta = new Vector2(Mathf.Min(370, w - 16), 58);
                stepText.rectTransform.anchorMin = stepText.rectTransform.anchorMax = new Vector2(.5f, 0); stepText.rectTransform.anchoredPosition = new Vector2(0, 168); stepText.rectTransform.sizeDelta = new Vector2(Mathf.Min(370, w - 16), 38);
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
    }
}
