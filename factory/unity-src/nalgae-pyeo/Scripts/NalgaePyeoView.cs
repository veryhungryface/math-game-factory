using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.NalgaePyeo
{
    public partial class NalgaePyeoGame
    {
        static readonly Color Gold = MgfLook.Hex("#D5A63C");
        static readonly Color Black = MgfLook.Hex("#171513");
        static readonly Color Ivory = MgfLook.Hex("#FFF2CD");
        static readonly Color Rust = MgfLook.Hex("#A64F2D");
        static readonly Color Umber = MgfLook.Hex("#6B5131");
        static readonly Color LightGold = MgfLook.Hex("#FFD970");
        static readonly Color Cool = MgfLook.Hex("#4F8A88");

        Camera cam;
        Transform worldRoot, curtainRoot, puppetRoot, leftWing, rightWing, ratchet, footLatch;
        readonly Transform[] curtainFolds = new Transform[8];
        readonly Vector3[] curtainHomes = new Vector3[8];
        readonly Transform[] tassels = new Transform[18];
        readonly Vector3[] tasselHomes = new Vector3[18];
        readonly Transform[] littlePuppets = new Transform[5];
        readonly Quaternion[] littleHomes = new Quaternion[5];
        Material goldMat, goldDarkMat, blackMat, ivoryMat, rustMat, cordMat, lampMat, silkMat;
        LineRenderer leftCord, rightCord;

        Canvas canvas;
        CanvasScaler scaler;
        CanvasGroup titleG, hudG, feedbackG, endG, toastG, bridgeG;
        RectTransform titleRt, hudRt, diagramRt, railRt, handleRt, guideRingRt, guideHandRt, rippleRt, feedbackRt, endRt, toastRt, bridgeRt;
        RawImage titleArt;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleBest, titleTap;
        TextMeshProUGUI scoreTxt, roundTxt, firstTxt, timeTxt, goalTxt, promptTxt, angleTxt, handleTxt;
        TextMeshProUGUI centerTxt, pointATxt, pointBTxt, pointPTxt, pointQTxt, diagramInfoTxt;
        TextMeshProUGUI feedbackTitleTxt, feedbackMathTxt, toastTxt, bridgeTitleTxt, bridgeBodyTxt;
        TextMeshProUGUI endTitleTxt, endScoreTxt, endStatsTxt, endTapTxt;
        Image statusBg, goalBg, promptBg, diagramPlate, circleRing, handleImg, guideRing, guideHand, guidePath, feedbackBg, rippleImg;
        readonly Image[] lifeKnots = new Image[3];
        readonly Image[] ledgerMarks = new Image[8];
        readonly Image[] ticks = new Image[18];
        readonly Image[] arcDots = new Image[25];
        readonly Image[] dragTrail = new Image[6];
        Image lineOA, lineOB, linePA, linePB;
        Sprite roundedSprite, ringSprite, dotSprite;
        Texture2D titleTexture;

        bool land;
        int lastW = -1, lastH = -1;
        float clock, titleMotion, feedbackAnim = 9f, guideBoost, rippleClock, toastClock, shownScore, shownEndScore;
        int feedbackFold = -1;
        bool feedbackGood;
        float railHalf = 142f;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size); var im = rt.gameObject.AddComponent<Image>();
            im.color = color; im.sprite = sprite; im.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color, float width,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 3f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color;
            t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text; return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "PorcelainRing" : "SilkRound", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                if (ring)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - size * .39f) / (size * .055f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * .5f) - (size * .5f - radius), 0);
                    float qy = Mathf.Max(Mathf.Abs(fy - size * .5f) - (size * .5f - radius), 0);
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true); float b = ring ? 0 : radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#FFE7A0"), Gold, MgfLook.Hex("#7B351F"), .78f);
            MgfLook.Sun(new Vector3(38, -34, 12), MgfLook.Hex("#FFF0C1"), 1.30f, .70f);
            cam = MgfLook.Camera(new Vector3(0f, 8.6f, -18.4f), new Vector3(0f, 2.0f, 3.2f), 34f);
            goldMat = MgfLook.Lit(Gold, .34f, .02f, Gold * .08f);
            goldDarkMat = MgfLook.Lit(MgfLook.Hex("#9B6520"), .24f, .02f);
            blackMat = MgfLook.Lit(Black, .72f, .32f, Black * .02f);
            ivoryMat = MgfLook.Lit(Ivory, .82f, .04f, Ivory * .08f);
            rustMat = MgfLook.Lit(Rust, .44f, .10f);
            cordMat = MgfLook.Lit(MgfLook.Hex("#241B16"), .36f, .12f);
            lampMat = MgfLook.Lit(LightGold, .60f, .02f, LightGold * .85f);
            silkMat = MgfLook.Alpha(new Color(1f, .71f, .20f, .72f));

            worldRoot = new GameObject("GoldenPulleyTheater").transform;
            curtainRoot = new GameObject("EightSilkPleats").transform; curtainRoot.SetParent(worldRoot, false);
            MgfLook.Block("CeilingBeam", new Vector3(0, 6.35f, 4.7f), new Vector3(18f, .32f, .42f), .14f, blackMat, worldRoot);
            MgfLook.Block("LowerStage", new Vector3(0, -.55f, 4.2f), new Vector3(18f, .45f, 7.8f), .18f, goldDarkMat, worldRoot);

            for (int i = 0; i < 12; i++)
            {
                float x = -8.25f + i * 1.5f;
                var pleat = MgfLook.Block("SilkBackgroundPleat" + i, new Vector3(x, 2.75f + (i % 2) * .08f, 7.0f + (i % 3) * .22f),
                    new Vector3(1.72f, 7.2f, .54f), .24f, i % 2 == 0 ? goldMat : goldDarkMat, curtainRoot).transform;
                pleat.localRotation = Quaternion.Euler(0, (i % 2 == 0 ? 1 : -1) * 8f, 0);
            }
            for (int i = 0; i < 8; i++)
            {
                float x = -6.5f + i * 1.86f;
                curtainFolds[i] = MgfLook.Block("PlayableFold" + (i + 1), new Vector3(x, 2.25f, 5.85f), new Vector3(1.52f, 6.1f, .72f), .28f,
                    i % 2 == 0 ? goldMat : goldDarkMat, curtainRoot).transform;
                curtainHomes[i] = curtainFolds[i].localPosition;
                CreateSmallPuppet(i, x, 5.25f);
            }
            for (int i = 0; i < tassels.Length; i++)
            {
                float x = -8.2f + i * .96f;
                Transform t = MgfLook.Prim(PrimitiveType.Cylinder, "Tassel" + i, new Vector3(x, 5.72f + (i % 3) * .10f, 4.92f),
                    new Vector3(.09f, .33f, .09f), i % 2 == 0 ? rustMat : ivoryMat, worldRoot, false).transform;
                t.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? -1 : 1) * 7f); tassels[i] = t; tasselHomes[i] = t.localPosition;
            }
            for (int i = 0; i < 7; i++)
            {
                float x = -7.2f + i * 2.4f;
                MgfLook.Prim(PrimitiveType.Sphere, "WarmLamp" + i, new Vector3(x, 5.15f + (i % 2) * .2f, 4.55f), new Vector3(.34f, .34f, .22f), lampMat, worldRoot, false);
            }
            BuildMainPuppet();
        }

        void CreateSmallPuppet(int index, float x, float z)
        {
            var root = new GameObject("LatchedPuppet" + (index + 1)).transform; root.SetParent(worldRoot, false); root.localPosition = new Vector3(x, 4.85f, z);
            MgfLook.Prim(PrimitiveType.Sphere, "Body", Vector3.zero, new Vector3(.44f, .60f, .18f), blackMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Hinge", new Vector3(0, -.28f, -.18f), new Vector3(.18f, .18f, .12f), ivoryMat, root, false);
            var wing = MgfLook.Block("Wing", new Vector3(.42f, .02f, 0), new Vector3(.82f, .10f, .18f), .04f, goldMat, root).transform;
            wing.localRotation = Quaternion.Euler(0, 0, 16 + index * 4f);
            littlePuppets[index] = root; littleHomes[index] = root.localRotation;
            var cord = Line("PulleyCord" + index, cordMat, .045f, worldRoot, 2);
            cord.SetPosition(0, new Vector3(x, 6.2f, z)); cord.SetPosition(1, new Vector3(x, 5.25f, z));
        }

        void BuildMainPuppet()
        {
            puppetRoot = new GameObject("MainBlackLacquerPuppet").transform; puppetRoot.SetParent(worldRoot, false); puppetRoot.localPosition = new Vector3(0, .90f, 1.65f);
            MgfLook.Prim(PrimitiveType.Sphere, "OvalLacquerBody", new Vector3(0, .66f, 0), new Vector3(1.10f, 1.46f, .46f), blackMat, puppetRoot, false);
            MgfLook.Prim(PrimitiveType.Sphere, "IvoryChestHinge", new Vector3(0, .76f, -.34f), new Vector3(.32f, .32f, .17f), ivoryMat, puppetRoot, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "NeckPin", new Vector3(0, 1.53f, 0), new Vector3(.20f, .26f, .20f), ivoryMat, puppetRoot, false);
            footLatch = MgfLook.Block("FootLatch", new Vector3(0, -.10f, -.10f), new Vector3(.72f, .18f, .34f), .07f, ivoryMat, puppetRoot).transform;
            ratchet = MgfLook.Prim(PrimitiveType.Cylinder, "PorcelainRatchet", new Vector3(0, .14f, -.36f), new Vector3(.44f, .16f, .44f), ivoryMat, puppetRoot, false).transform;
            ratchet.localRotation = Quaternion.Euler(90, 0, 0);
            leftWing = new GameObject("LeftWingHinge").transform; leftWing.SetParent(puppetRoot, false); leftWing.localPosition = new Vector3(-.52f, .96f, -.08f);
            rightWing = new GameObject("RightWingHinge").transform; rightWing.SetParent(puppetRoot, false); rightWing.localPosition = new Vector3(.52f, .96f, -.08f);
            BuildWing(leftWing, -1); BuildWing(rightWing, 1);
            leftCord = Line("LeftTensionCord", cordMat, .055f, worldRoot, 2);
            rightCord = Line("RightTensionCord", cordMat, .055f, worldRoot, 2);
        }

        void BuildWing(Transform pivot, int side)
        {
            for (int i = 0; i < 5; i++)
            {
                float len = 2.05f + i * .18f;
                Transform rib = MgfLook.Block("BlackWingBone" + i, new Vector3(side * len * .5f, i * .035f, 0), new Vector3(len, .075f, .10f), .025f, blackMat, pivot).transform;
                rib.localRotation = Quaternion.Euler(0, 0, side * i * 6.0f);
            }
            Transform silk = MgfLook.Block("SaffronSilkFan", new Vector3(side * 1.18f, .15f, .08f), new Vector3(2.18f, 1.15f, .045f), .12f, goldMat, pivot).transform;
            silk.localRotation = Quaternion.Euler(0, 0, side * 12f);
            MgfLook.Prim(PrimitiveType.Sphere, "IvoryWingHandle", new Vector3(side * 2.34f, .12f, -.04f), new Vector3(.32f, .32f, .22f), ivoryMat, pivot, false);
        }

        LineRenderer Line(string name, Material mat, float width, Transform parent, int count)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat; lr.positionCount = count; lr.startWidth = lr.endWidth = width; lr.useWorldSpace = false;
            lr.numCapVertices = 4; lr.numCornerVertices = 2; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.receiveShadows = false; return lr;
        }

        void BuildUi()
        {
            roundedSprite = MakeSprite(64, 14, false); ringSprite = MakeSprite(64, 14, true);
            dotSprite = Sprite.Create(MgfLook.SoftDot, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 100);
            var go = new GameObject("NalgaeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(390, 844); scaler.matchWidthOrHeight = .5f;

            titleRt = R("TitleWorld", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            titleArt = R("GeneratedTitleArt", titleRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(430, 890)).gameObject.AddComponent<RawImage>();
            titleTexture = Resources.Load<Texture2D>("NalgaePyeo/title"); titleArt.texture = titleTexture; titleArt.color = titleTexture ? Color.white : new Color(1, 1, 1, 0); titleArt.raycastTarget = false;
            var sewn = Img(titleRt, "SewnLogoLabel", new Vector2(.5f, .86f), Vector2.zero, new Vector2(292, 126), new Color(.10f, .08f, .07f, .92f), roundedSprite);
            titleLogo = Txt(sewn.transform, "날개 펴", new Vector2(.5f, .63f), Vector2.zero, 48, Ivory, 270); titleLogo.fontStyle = FontStyles.Bold; titleLogo.outlineColor = Rust; titleLogo.outlineWidth = .20f;
            titleTag = Txt(sewn.transform, "날개를 끌어 원주각을 만들어라", new Vector2(.5f, .20f), Vector2.zero, 14, LightGold, 270); titleTag.fontStyle = FontStyles.Bold;
            titleMeta = Txt(titleRt, "중3 · 원주각", new Vector2(.16f, .065f), Vector2.zero, 12, Black, 104); titleMeta.fontStyle = FontStyles.Bold;
            titleBest = Txt(titleRt, "첫 시도 6/8이 목표", new Vector2(.76f, .065f), Vector2.zero, 12, Black, 140); titleBest.fontStyle = FontStyles.Bold;
            var latch = Img(titleRt, "StartFootLatch", new Vector2(.5f, .145f), Vector2.zero, new Vector2(230, 66), Ivory, roundedSprite);
            titleTap = Txt(latch.transform, "중앙 인형을 눌러 시작", new Vector2(.5f, .5f), Vector2.zero, 18, Black, 214); titleTap.fontStyle = FontStyles.Bold;

            hudRt = R("TheaterHud", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            statusBg = Img(hudRt, "IvoryStatusLedger", new Vector2(.5f, .955f), Vector2.zero, new Vector2(360, 58), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), roundedSprite);
            scoreTxt = Txt(statusBg.transform, "0", new Vector2(.09f, .58f), Vector2.zero, 20, Rust, 64); scoreTxt.fontStyle = FontStyles.Bold;
            roundTxt = Txt(statusBg.transform, "주름 0/8", new Vector2(.31f, .58f), Vector2.zero, 13, Black, 96); roundTxt.fontStyle = FontStyles.Bold;
            firstTxt = Txt(statusBg.transform, "첫 0/0", new Vector2(.55f, .58f), Vector2.zero, 13, Black, 82); firstTxt.fontStyle = FontStyles.Bold;
            timeTxt = Txt(statusBg.transform, "105초", new Vector2(.76f, .58f), Vector2.zero, 14, Rust, 72); timeTxt.fontStyle = FontStyles.Bold;
            for (int i = 0; i < 3; i++) lifeKnots[i] = Img(statusBg.transform, "VisibleTensionKnot" + i, new Vector2(.76f + i * .075f, .20f), Vector2.zero, new Vector2(18, 12), Black, roundedSprite);
            for (int i = 0; i < 8; i++) ledgerMarks[i] = Img(statusBg.transform, "FirstAttemptMark" + i, new Vector2(.17f + i * .055f, .17f), Vector2.zero, new Vector2(13, 7), new Color(Umber.r, Umber.g, Umber.b, .32f), roundedSprite);

            goalBg = Img(hudRt, "PersistentGoal", new Vector2(.5f, .888f), Vector2.zero, new Vector2(364, 42), new Color(Rust.r, Rust.g, Rust.b, .96f), roundedSprite);
            goalTxt = Txt(goalBg.transform, "날개를 원주각만큼 펴면 주름이 열린다", new Vector2(.5f, .52f), Vector2.zero, 14, Ivory, 346); goalTxt.fontStyle = FontStyles.Bold;

            diagramRt = R("IvoryCircleLamp", hudRt, new Vector2(.5f, .695f), Vector2.zero, new Vector2(286, 286));
            diagramPlate = Img(diagramRt, "LampPlate", new Vector2(.5f, .5f), Vector2.zero, new Vector2(286, 286), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), roundedSprite);
            circleRing = Img(diagramRt, "CircleRim", new Vector2(.5f, .54f), Vector2.zero, new Vector2(208, 208), Black, ringSprite);
            for (int i = 0; i < arcDots.Length; i++) arcDots[i] = Img(diagramRt, "TargetArcDoubleStitch" + i, new Vector2(.5f, .54f), Vector2.zero, new Vector2(12, 12), Rust, dotSprite);
            lineOA = UiLine(diagramRt, "OA", Black, 3); lineOB = UiLine(diagramRt, "OB", Black, 3); linePA = UiLine(diagramRt, "PA", Umber, 3); linePB = UiLine(diagramRt, "PB", Umber, 3);
            centerTxt = Txt(diagramRt, "O", new Vector2(.5f, .54f), Vector2.zero, 15, Black, 28); centerTxt.fontStyle = FontStyles.Bold;
            pointATxt = Txt(diagramRt, "A", new Vector2(.5f, .5f), Vector2.zero, 15, Black, 28); pointATxt.fontStyle = FontStyles.Bold;
            pointBTxt = Txt(diagramRt, "B", new Vector2(.5f, .5f), Vector2.zero, 15, Black, 28); pointBTxt.fontStyle = FontStyles.Bold;
            pointPTxt = Txt(diagramRt, "P", new Vector2(.5f, .5f), Vector2.zero, 15, Rust, 28); pointPTxt.fontStyle = FontStyles.Bold;
            pointQTxt = Txt(diagramRt, "Q", new Vector2(.5f, .5f), Vector2.zero, 15, Cool, 28); pointQTxt.fontStyle = FontStyles.Bold;
            diagramInfoTxt = Txt(diagramRt, "", new Vector2(.5f, .10f), Vector2.zero, 15, Rust, 260); diagramInfoTxt.fontStyle = FontStyles.Bold;

            promptBg = Img(hudRt, "ShortMathPrompt", new Vector2(.5f, .493f), Vector2.zero, new Vector2(364, 76), new Color(Ivory.r, Ivory.g, Ivory.b, .97f), roundedSprite);
            promptTxt = Txt(promptBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 13, Black, 344); promptTxt.fontStyle = FontStyles.Bold; promptTxt.enableAutoSizing = true; promptTxt.fontSizeMin = 9; promptTxt.fontSizeMax = 14; promptTxt.margin = new Vector4(8, 4, 8, 4);

            railRt = R("WingAngleRail", hudRt, new Vector2(.5f, .205f), Vector2.zero, new Vector2(330, 112));
            Img(railRt, "BlackEmbroideredRail", new Vector2(.5f, .42f), Vector2.zero, new Vector2(302, 7), Black, roundedSprite);
            for (int i = 0; i < ticks.Length; i++)
            {
                float u = i / 17f;
                ticks[i] = Img(railRt, "EqualFiveDegreeTick" + (i + 1), new Vector2(.07f + .86f * u, .42f), Vector2.zero, new Vector2(i % 3 == 2 ? 4 : 2, i % 3 == 2 ? 25 : 15), Umber, roundedSprite);
            }
            angleTxt = Txt(railRt, "30°", new Vector2(.5f, .88f), Vector2.zero, 28, Ivory, 126); angleTxt.fontStyle = FontStyles.Bold; angleTxt.outlineColor = Black; angleTxt.outlineWidth = .18f;
            for (int i = 0; i < dragTrail.Length; i++) dragTrail[i] = Img(railRt, "SilkWingTrail" + i, new Vector2(.5f, .42f), Vector2.zero, new Vector2(31 - i * 3, 31 - i * 3), new Color(LightGold.r, LightGold.g, LightGold.b, .22f - i * .025f), dotSprite);
            handleImg = Img(railRt, "ActualRightWingHandle", new Vector2(.5f, .42f), Vector2.zero, new Vector2(72, 62), Ivory, roundedSprite); handleRt = handleImg.rectTransform;
            handleTxt = Txt(handleImg.transform, "날개 끝", new Vector2(.5f, .5f), Vector2.zero, 13, Black, 66); handleTxt.fontStyle = FontStyles.Bold;
            guidePath = Img(railRt, "GhostDragPath", new Vector2(.5f, .42f), Vector2.zero, new Vector2(188, 7), new Color(LightGold.r, LightGold.g, LightGold.b, .86f), roundedSprite);
            guideRing = Img(railRt, "DoubleIconRipple", new Vector2(.5f, .42f), Vector2.zero, new Vector2(96, 84), Rust, ringSprite); guideRingRt = guideRing.rectTransform;
            guideHand = Img(railRt, "MovingGhostFinger", new Vector2(.5f, .42f), Vector2.zero, new Vector2(42, 34), new Color(Rust.r, Rust.g, Rust.b, .78f), roundedSprite); guideHandRt = guideHand.rectTransform;

            feedbackRt = R("MathReveal", hudRt, new Vector2(.5f, .405f), Vector2.zero, new Vector2(360, 148)); feedbackG = feedbackRt.gameObject.AddComponent<CanvasGroup>();
            feedbackBg = Img(feedbackRt, "PorcelainEvidenceRail", new Vector2(.5f, .5f), Vector2.zero, new Vector2(360, 148), new Color(Black.r, Black.g, Black.b, .97f), roundedSprite);
            feedbackTitleTxt = Txt(feedbackBg.transform, "", new Vector2(.5f, .75f), Vector2.zero, 21, LightGold, 336); feedbackTitleTxt.fontStyle = FontStyles.Bold;
            feedbackMathTxt = Txt(feedbackBg.transform, "", new Vector2(.5f, .35f), Vector2.zero, 14, Ivory, 336); feedbackMathTxt.fontStyle = FontStyles.Bold; feedbackMathTxt.enableAutoSizing = true; feedbackMathTxt.fontSizeMin = 10; feedbackMathTxt.fontSizeMax = 15;

            bridgeRt = R("BandBridge", hudRt, new Vector2(.5f, .55f), Vector2.zero, new Vector2(360, 192)); bridgeG = bridgeRt.gameObject.AddComponent<CanvasGroup>();
            var bridgeBg = Img(bridgeRt, "BridgeSilkLabel", new Vector2(.5f, .5f), Vector2.zero, new Vector2(360, 192), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), roundedSprite);
            bridgeTitleTxt = Txt(bridgeBg.transform, "", new Vector2(.5f, .68f), Vector2.zero, 24, Rust, 332); bridgeTitleTxt.fontStyle = FontStyles.Bold;
            bridgeBodyTxt = Txt(bridgeBg.transform, "", new Vector2(.5f, .35f), Vector2.zero, 15, Black, 332); bridgeBodyTxt.fontStyle = FontStyles.Bold;

            toastRt = R("RefusalLabel", hudRt, new Vector2(.5f, .40f), Vector2.zero, new Vector2(350, 54)); toastG = toastRt.gameObject.AddComponent<CanvasGroup>();
            var toastBg = Img(toastRt, "RefusalIvory", new Vector2(.5f, .5f), Vector2.zero, new Vector2(350, 54), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), roundedSprite);
            toastTxt = Txt(toastBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 13, Black, 332); toastTxt.fontStyle = FontStyles.Bold;
            rippleImg = Img(hudRt, "InkTapRipple", new Vector2(.5f, .5f), Vector2.zero, new Vector2(74, 74), Rust, ringSprite); rippleRt = rippleImg.rectTransform; rippleRt.gameObject.SetActive(false);

            endRt = R("GrandResult", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); endG = endRt.gameObject.AddComponent<CanvasGroup>();
            var endBg = Img(endRt, "OpenedPanoramaLedger", new Vector2(.5f, .52f), Vector2.zero, new Vector2(354, 428), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), roundedSprite);
            endTitleTxt = Txt(endBg.transform, "", new Vector2(.5f, .80f), Vector2.zero, 32, Rust, 328); endTitleTxt.fontStyle = FontStyles.Bold;
            endScoreTxt = Txt(endBg.transform, "0", new Vector2(.5f, .60f), Vector2.zero, 58, Black, 328); endScoreTxt.fontStyle = FontStyles.Bold;
            endStatsTxt = Txt(endBg.transform, "", new Vector2(.5f, .38f), Vector2.zero, 17, Umber, 320); endStatsTxt.fontStyle = FontStyles.Bold;
            var restart = Img(endBg.transform, "RestartFootLatch", new Vector2(.5f, .12f), Vector2.zero, new Vector2(292, 68), Black, roundedSprite);
            endTapTxt = Txt(restart.transform, "인형을 눌러 다시 열기", new Vector2(.5f, .5f), Vector2.zero, 19, Ivory, 276); endTapTxt.fontStyle = FontStyles.Bold;
            Relayout(true);
        }

        Image UiLine(Transform parent, string name, Color color, float width)
        {
            return Img(parent, name, new Vector2(.5f, .5f), Vector2.zero, new Vector2(10, width), color, roundedSprite);
        }

        static void SetUiLine(Image line, Vector2 a, Vector2 b, float width)
        {
            Vector2 d = b - a; var rt = line.rectTransform; rt.anchoredPosition = (a + b) * .5f;
            rt.sizeDelta = new Vector2(d.magnitude, width); rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        void Relayout(bool force)
        {
            int w = Screen.width, h = Screen.height; bool nextLand = MgfBridge.IsLandLayout;
            if (!force && w == lastW && h == lastH && nextLand == land) return;
            lastW = w; lastH = h; land = nextLand;
            if (land)
            {
                scaler.referenceResolution = new Vector2(1280, 800); scaler.matchWidthOrHeight = .45f;
                titleRt.sizeDelta = endRt.sizeDelta = hudRt.sizeDelta = new Vector2(1280, 800);
                titleArt.rectTransform.sizeDelta = new Vector2(1340, 890);
                ((RectTransform)titleLogo.transform.parent).anchorMin = ((RectTransform)titleLogo.transform.parent).anchorMax = new Vector2(.50f, .82f);
                statusBg.rectTransform.anchorMin = statusBg.rectTransform.anchorMax = new Vector2(.84f, .91f); statusBg.rectTransform.sizeDelta = new Vector2(370, 62);
                goalBg.rectTransform.anchorMin = goalBg.rectTransform.anchorMax = new Vector2(.84f, .80f); goalBg.rectTransform.sizeDelta = new Vector2(360, 48);
                diagramRt.anchorMin = diagramRt.anchorMax = new Vector2(.17f, .56f); diagramRt.sizeDelta = new Vector2(330, 330);
                promptBg.rectTransform.anchorMin = promptBg.rectTransform.anchorMax = new Vector2(.17f, .25f); promptBg.rectTransform.sizeDelta = new Vector2(390, 112);
                railRt.anchorMin = railRt.anchorMax = new Vector2(.53f, .16f); railRt.sizeDelta = new Vector2(500, 126); railHalf = 205f;
                feedbackRt.anchorMin = feedbackRt.anchorMax = new Vector2(.83f, .53f); bridgeRt.anchorMin = bridgeRt.anchorMax = new Vector2(.83f, .52f);
                toastRt.anchorMin = toastRt.anchorMax = new Vector2(.53f, .34f);
                endRt.sizeDelta = new Vector2(1280, 800);
                cam.transform.position = new Vector3(0f, 7.5f, -19.6f); cam.transform.LookAt(new Vector3(0f, 2f, 3.4f));
            }
            else
            {
                scaler.referenceResolution = new Vector2(390, 844); scaler.matchWidthOrHeight = .5f;
                titleRt.sizeDelta = endRt.sizeDelta = hudRt.sizeDelta = new Vector2(390, 844); titleArt.rectTransform.sizeDelta = new Vector2(430, 890);
                ((RectTransform)titleLogo.transform.parent).anchorMin = ((RectTransform)titleLogo.transform.parent).anchorMax = new Vector2(.5f, .86f);
                statusBg.rectTransform.anchorMin = statusBg.rectTransform.anchorMax = new Vector2(.5f, .955f); statusBg.rectTransform.sizeDelta = new Vector2(360, 58);
                goalBg.rectTransform.anchorMin = goalBg.rectTransform.anchorMax = new Vector2(.5f, .888f); goalBg.rectTransform.sizeDelta = new Vector2(364, 42);
                diagramRt.anchorMin = diagramRt.anchorMax = new Vector2(.5f, .695f); diagramRt.sizeDelta = new Vector2(286, 286);
                promptBg.rectTransform.anchorMin = promptBg.rectTransform.anchorMax = new Vector2(.5f, .493f); promptBg.rectTransform.sizeDelta = new Vector2(364, 76);
                railRt.anchorMin = railRt.anchorMax = new Vector2(.5f, .205f); railRt.sizeDelta = new Vector2(330, 112); railHalf = 142f;
                feedbackRt.anchorMin = feedbackRt.anchorMax = new Vector2(.5f, .405f); bridgeRt.anchorMin = bridgeRt.anchorMax = new Vector2(.5f, .55f);
                toastRt.anchorMin = toastRt.anchorMax = new Vector2(.5f, .40f);
                cam.transform.position = new Vector3(0f, 8.6f, -18.4f); cam.transform.LookAt(new Vector3(0f, 2f, 3.2f));
            }
            UpdateAngleVisual(st.selectedDeg, false);
        }

        void ResetVisuals()
        {
            for (int i = 0; i < curtainFolds.Length; i++) if (curtainFolds[i]) curtainFolds[i].localPosition = curtainHomes[i];
            puppetRoot.localPosition = new Vector3(0, .90f, 1.65f); ratchet.localRotation = Quaternion.Euler(90, 0, 0); footLatch.localRotation = Quaternion.identity;
            feedbackAnim = 9f; feedbackFold = -1; shownScore = shownEndScore = 0; guideBoost = 0;
        }

        void ResetCurtainsForRun()
        {
            for (int i = 0; i < curtainFolds.Length; i++) curtainFolds[i].localPosition = curtainHomes[i];
        }

        void ShowTitleVisual()
        {
            titleG.alpha = 1; titleG.gameObject.SetActive(true); hudG.gameObject.SetActive(false); endG.gameObject.SetActive(false);
            feedbackG.gameObject.SetActive(false); bridgeG.gameObject.SetActive(false); toastG.gameObject.SetActive(false); titleMotion = 0;
        }

        void BeginOpeningVisual()
        {
            titleG.gameObject.SetActive(true); hudG.gameObject.SetActive(false); endG.gameObject.SetActive(false); titleMotion = 0;
            footLatch.localRotation = Quaternion.Euler(0, 0, -24f); MgfFx.Punch(footLatch, .24f, .30f);
        }

        void LoadProblemVisual(WingProblem p, bool practice)
        {
            titleG.gameObject.SetActive(false); endG.gameObject.SetActive(false); hudG.gameObject.SetActive(true); hudG.alpha = 1;
            feedbackG.gameObject.SetActive(false); bridgeG.gameObject.SetActive(false); toastG.gameObject.SetActive(false);
            promptTxt.text = practice ? "연습 · 중심각 120°일 때 ∠APB의 크기를 구하시오" : p.prompt;
            BuildDiagram(p);
            goalTxt.text = practice ? "날개 끝을 끌어 60°를 만들고 놓으시오" : "날개를 원주각만큼 펴면 주름이 열린다";
            guideRing.gameObject.SetActive(practice); guideHand.gameObject.SetActive(practice); guidePath.gameObject.SetActive(practice);
            feedbackAnim = 9f; feedbackFold = -1;
        }

        void BuildDiagram(WingProblem p)
        {
            float r = land ? 112f : 91f;
            Vector2 c = new Vector2(0, 10f);
            float aDeg = p.rotationDeg;
            float bDeg = p.rotationDeg + p.targetDeg * 2f;
            float pDeg = p.rotationDeg + p.targetDeg + 180f + (p.pointVariant % 3 - 1) * 15f;
            float qDeg = pDeg + 34f + (p.pointVariant % 4) * 7f;
            Vector2 a = c + Polar(aDeg, r), b = c + Polar(bDeg, r), pp = c + Polar(pDeg, r), q = c + Polar(qDeg, r);
            SetPoint(pointATxt.rectTransform, a, "A"); SetPoint(pointBTxt.rectTransform, b, "B"); SetPoint(pointPTxt.rectTransform, pp, "P"); SetPoint(pointQTxt.rectTransform, q, "Q");
            centerTxt.rectTransform.anchoredPosition = c; SetUiLine(lineOA, c, a, 3); SetUiLine(lineOB, c, b, 3); SetUiLine(linePA, pp, a, 3); SetUiLine(linePB, pp, b, 3);
            pointQTxt.gameObject.SetActive(p.band == 2);
            lineOA.gameObject.SetActive(p.band == 1); lineOB.gameObject.SetActive(p.band == 1);
            for (int i = 0; i < arcDots.Length; i++)
            {
                float u = i / (arcDots.Length - 1f);
                arcDots[i].rectTransform.anchoredPosition = c + Polar(Mathf.Lerp(aDeg, bDeg, u), r);
                arcDots[i].gameObject.SetActive(i % 2 == 0 || i == arcDots.Length - 1);
            }
            if (p.band == 0 || p.band == 1) diagramInfoTxt.text = "∠AOB=" + p.centerDeg + "° · 같은 호 AB";
            else if (p.band == 2) diagramInfoTxt.text = "∠AQB=" + p.targetDeg + "° · 같은 호 AB";
            else diagramInfoTxt.text = "호 AB:호 CD=" + p.ratioN + ":" + p.ratioD + " · CD의 원주각 " + NalgaeRules.AngleText2(p.givenAngle2);
        }

        static Vector2 Polar(float deg, float r)
        {
            float a = deg * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }

        static void SetPoint(RectTransform rt, Vector2 p, string unused) { rt.anchoredPosition = p; }

        void UpdateAngleVisual(int deg, bool fromInput)
        {
            float u = (deg - 5f) / 85f;
            float x = Mathf.Lerp(-railHalf, railHalf, u);
            if (handleRt) handleRt.anchoredPosition = new Vector2(x, 0);
            if (angleTxt) angleTxt.text = deg + "°";
            if (leftWing) leftWing.localRotation = Quaternion.Euler(0, 0, 90f + deg * .5f);
            if (rightWing) rightWing.localRotation = Quaternion.Euler(0, 0, 90f - deg * .5f);
            if (guideRingRt) guideRingRt.anchoredPosition = new Vector2(x, 0);
            for (int i = 0; i < dragTrail.Length; i++) if (dragTrail[i]) dragTrail[i].rectTransform.anchoredPosition = new Vector2(x - (i + 1) * 12f, 0);
            if (fromInput && handleRt) handleRt.localScale = new Vector3(1.12f, .88f, 1f);
            UpdateCords();
        }

        void UpdateCords()
        {
            if (!leftCord || !rightCord) return;
            Vector3 lp = puppetRoot.localPosition + leftWing.localPosition + new Vector3(-2.1f, 1.1f, 0);
            Vector3 rp = puppetRoot.localPosition + rightWing.localPosition + new Vector3(2.1f, 1.1f, 0);
            leftCord.SetPosition(0, new Vector3(-2.7f, 6.2f, 1.65f)); leftCord.SetPosition(1, lp);
            rightCord.SetPosition(0, new Vector3(2.7f, 6.2f, 1.65f)); rightCord.SetPosition(1, rp);
        }

        void BeginDragVisual()
        {
            guideRing.gameObject.SetActive(false); guideHand.gameObject.SetActive(false); guidePath.gameObject.SetActive(false);
            handleRt.localScale = new Vector3(1.15f, .84f, 1f); guideBoost = .25f; MgfFx.Punch(ratchet, .08f, .12f);
        }

        void EndDragVisual() { handleRt.localScale = Vector3.one; }

        void BeginFeedbackVisual(bool correct, bool practice, int selected, string mis)
        {
            feedbackG.gameObject.SetActive(true); feedbackG.alpha = 1; feedbackAnim = 0f; feedbackGood = correct;
            feedbackFold = correct && !practice ? Mathf.Clamp(st.solved - 1, 0, 7) : -1;
            if (correct)
            {
                feedbackTitleTxt.text = practice ? "찰칵 · 첫 주름이 열렸다" : "찰칵 · " + selected + "°에 잠겼다";
                if (current.band <= 1) feedbackMathTxt.text = "원주각=중심각÷2\n" + current.centerDeg + "°÷2=" + current.targetDeg + "°";
                else if (current.band == 2) feedbackMathTxt.text = "같은 호 AB에 대한 원주각은 같다\n∠AQB=∠APB=" + current.targetDeg + "°";
                else feedbackMathTxt.text = "원주각은 호의 길이에 정비례한다\n" + NalgaeRules.AngleText2(current.givenAngle2) + " × " + current.ratioN + "/" + current.ratioD + " = " + current.targetDeg + "°";
                MgfSfx.Play("correct", .56f); MgfFx.Punch(ratchet, .23f, .34f);
            }
            else
            {
                feedbackTitleTxt.text = selected + "° 자수 띠를 남겼다";
                feedbackMathTxt.text = WrongEvidence(mis, selected);
                puppetRoot.localPosition += Vector3.down * .46f; MgfFx.Shake(cam, .075f, .22f); MgfSfx.Play("wrong", .36f);
            }
            UpdateHudText();
        }

        string WrongEvidence(string mis, int selected)
        {
            if (mis == "center_equal" || mis == "center_doubled") return "중심 O의 각과 원 위 P의 각을 구분하시오\n정확한 관계: " + current.centerDeg + "°÷2=" + current.targetDeg + "°";
            if (mis == "diameter_overgeneralized") return "90°는 AB가 지름일 때만이다\n이 호의 원주각은 " + current.targetDeg + "°";
            if (mis == "same_arc_position_changed") return "P의 위치가 달라도 보는 호 AB가 같으면 각은 같다\n∠APB=" + current.targetDeg + "°";
            if (mis == "ratio_ignored" || mis == "ratio_reversed" || mis == "arc_ratio_missed") return "현이 아니라 호의 길이의 비를 적용하시오\n수정 각은 " + current.targetDeg + "°";
            return "검은 띠 " + selected + "°와 수정 각 " + current.targetDeg + "°를 비교하시오";
        }

        void FinishFeedbackVisual(bool correct)
        {
            feedbackG.gameObject.SetActive(false); feedbackAnim = 9f;
            if (!correct) puppetRoot.localPosition = new Vector3(0, .90f, 1.65f);
        }

        void ShowBridgeVisual(int band)
        {
            feedbackG.gameObject.SetActive(false); bridgeG.gameObject.SetActive(true); bridgeG.alpha = 1;
            if (band == 2)
            {
                bridgeTitleTxt.text = "P가 움직여도 같은 호";
                bridgeBodyTxt.text = "같은 호 AB를 보면 원주각의 크기는 같다";
            }
            else
            {
                bridgeTitleTxt.text = "호가 길어지면 각도 커진다";
                bridgeBodyTxt.text = "한 원에서 원주각은 호의 길이에 정비례한다";
            }
            MgfSfx.Play("whoosh", .22f);
        }

        void UpdateHudText()
        {
            if (!scoreTxt) return;
            scoreTxt.text = Mathf.RoundToInt(shownScore).ToString();
            roundTxt.text = "주름 " + st.solved + "/8";
            firstTxt.text = "첫 " + st.firstCorrect + "/" + st.firstAttempts;
            timeTxt.text = Mathf.Max(0, Mathf.CeilToInt(timeLeft)) + "초";
            for (int i = 0; i < lifeKnots.Length; i++) lifeKnots[i].color = i < st.lives ? Black : new Color(Umber.r, Umber.g, Umber.b, .18f);
            for (int i = 0; i < ledgerMarks.Length; i++)
            {
                if (i < st.firstAttempts) ledgerMarks[i].color = i < st.firstCorrect ? Cool : Rust;
                else ledgerMarks[i].color = new Color(Umber.r, Umber.g, Umber.b, .25f);
            }
        }

        void ShowEndVisual(bool win, string reason)
        {
            titleG.gameObject.SetActive(false); hudG.gameObject.SetActive(false); endG.gameObject.SetActive(true); endG.alpha = 1;
            shownEndScore = 0f;
            if (win)
            {
                endTitleTxt.text = "황금 무대가 열렸다";
                endStatsTxt.text = "주름 8/8 · 첫 시도 " + st.firstCorrect + "/8\n장력끈 " + st.lives + "개 남음";
                for (int i = 0; i < curtainFolds.Length; i++) curtainFolds[i].localPosition = curtainHomes[i] + Vector3.up * 5.5f;
            }
            else
            {
                endTitleTxt.text = reason == "time" ? "막이 닫혔다" : reason == "mastery" ? "첫 시도 기록을 다시" : "장력끈이 모두 풀렸다";
                endStatsTxt.text = "연 주름 " + st.solved + "/8 · 첫 시도 " + st.firstCorrect + "/" + st.firstAttempts + "\n다음에는 O와 P를 먼저 구분하시오";
            }
            endScoreTxt.text = "0"; MgfFx.Punch(endRt, .05f, .35f);
        }

        void BoostGuide(bool practice)
        {
            guideBoost = 1.2f; guideRing.gameObject.SetActive(true); guideHand.gameObject.SetActive(true); guidePath.gameObject.SetActive(true);
            handleTxt.text = practice ? "여기를 끌어" : "날개 끝";
        }

        void Refuse(Vector2 screen, string msg)
        {
            toastTxt.text = msg; toastG.gameObject.SetActive(true); toastG.alpha = 1; toastClock = 0f;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRt, screen, null, out local);
            rippleRt.anchoredPosition = local; rippleRt.localScale = Vector3.one * .6f; rippleRt.gameObject.SetActive(true); rippleClock = 0f;
            handleRt.localRotation = Quaternion.Euler(0, 0, -7f); MgfSfx.Play("wrong", .11f);
        }

        void TickView(float dt)
        {
            clock += dt;
            MgfLook.FitWidth(cam, land ? 34f : 34f, land ? .72f : .55f);
            for (int i = 0; i < tassels.Length; i++) if (tassels[i])
            {
                Vector3 p = tasselHomes[i]; p.y += Mathf.Sin(clock * 1.25f + i * .71f) * .08f; tassels[i].localPosition = p;
                tassels[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(clock * 1.7f + i) * 7f);
            }
            for (int i = 0; i < littlePuppets.Length; i++) if (littlePuppets[i]) littlePuppets[i].localRotation = littleHomes[i] * Quaternion.Euler(0, 0, Mathf.Sin(clock * 1.12f + i * 1.4f) * 3.2f);
            if (flow == Flow.Title || flow == Flow.Opening)
            {
                titleMotion += dt; float s = 1f + Mathf.Sin(titleMotion * .72f) * .012f; titleArt.rectTransform.localScale = new Vector3(s, s, 1);
                titleLogo.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(titleMotion * 1.1f) * .8f);
                if (flow == Flow.Opening)
                {
                    float k = Mathf.Clamp01(flowClock / .42f); titleG.alpha = 1f - k; curtainRoot.localScale = new Vector3(1f + k * .12f, 1f, 1f);
                }
            }
            else { titleArt.rectTransform.localScale = Vector3.one; curtainRoot.localScale = Vector3.one; }
            if (flow == Flow.Practice || flow == Flow.Playing)
            {
                float pulse = 1f + (.09f + guideBoost * .04f) * (0.5f + .5f * Mathf.Sin(clock * 5.2f));
                if (guideRing.gameObject.activeSelf) guideRingRt.localScale = Vector3.one * pulse;
                if (guideHand.gameObject.activeSelf)
                {
                    float u = .5f + .5f * Mathf.Sin(clock * 2.7f); guideHandRt.anchoredPosition = new Vector2(Mathf.Lerp(handleRt.anchoredPosition.x, Mathf.Min(railHalf, handleRt.anchoredPosition.x + 86f), u), 0);
                }
                guideBoost = Mathf.Max(0, guideBoost - dt);
            }
            if (feedbackAnim < 8f)
            {
                feedbackAnim += dt;
                if (feedbackGood)
                {
                    // 80ms hit-stop 뒤 래칫 오버슈트와 비단 주름 상승을 같은 박자로 묶는다.
                    float k = Mathf.Clamp01((feedbackAnim - .08f) / .55f);
                    float ease = 1f - Mathf.Pow(1f - k, 3f);
                    ratchet.localRotation = Quaternion.Euler(90, 0, ease * 88f + Mathf.Sin(k * Mathf.PI) * 14f);
                    footLatch.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(k * Mathf.PI) * 28f);
                    if (feedbackFold >= 0) curtainFolds[feedbackFold].localPosition = Vector3.Lerp(curtainHomes[feedbackFold], curtainHomes[feedbackFold] + Vector3.up * 5.5f, ease);
                }
            }
            if (shownScore < st.score)
            {
                shownScore = Mathf.Min(st.score, shownScore + dt * 520f); scoreTxt.text = Mathf.RoundToInt(shownScore).ToString();
                scoreTxt.transform.localScale = Vector3.one * (1f + .16f * Mathf.Sin(Mathf.Clamp01((st.score - shownScore) / 100f) * Mathf.PI));
            }
            else if (scoreTxt) scoreTxt.transform.localScale = Vector3.one;
            if (flow == Flow.End && shownEndScore < st.score)
            {
                shownEndScore = Mathf.Min(st.score, shownEndScore + dt * 620f); endScoreTxt.text = Mathf.RoundToInt(shownEndScore).ToString();
            }
            if (rippleImg && rippleImg.gameObject.activeSelf)
            {
                rippleClock += dt; float k = Mathf.Clamp01(rippleClock / .24f); rippleImg.color = new Color(Rust.r, Rust.g, Rust.b, 1f - k); rippleRt.localScale = Vector3.one * Mathf.Lerp(.6f, 1.45f, k);
                if (k >= 1f) rippleImg.gameObject.SetActive(false);
            }
            if (toastG && toastG.gameObject.activeSelf)
            {
                toastClock += dt; toastG.alpha = toastClock < .72f ? 1f : Mathf.Clamp01(1.1f - toastClock);
                if (toastClock > 1.15f) toastG.gameObject.SetActive(false);
            }
            if (handleRt && !dragging) handleRt.localRotation = Quaternion.Slerp(handleRt.localRotation, Quaternion.identity, dt * 12f);
            UpdateCords();
        }

        void CaptureHandleScreen(out int x, out int y)
        {
            if (!handleRt) { x = y = -1; return; }
            Vector2 p = RectTransformUtility.WorldToScreenPoint(null, handleRt.position); x = Mathf.RoundToInt(p.x); y = Mathf.RoundToInt(p.y);
        }
    }
}
