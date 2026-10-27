// 가운데 잡아 — 공중 종이 시간 회랑 2.5D 디오라마, 기록 정리 줄, 숫자 자 UI와 피드백 사건.
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.GaundeJaba
{
    public partial class GaundeJabaGame
    {
        static readonly Color Olive = MgfLook.Hex("#69405C");       // 오디빛 직조 무대
        static readonly Color Chartreuse = MgfLook.Hex("#EF7462");  // 산호색 기록 잉크
        static readonly Color Cocoa = MgfLook.Hex("#283442");       // 남색 먹선
        static readonly Color Fire = MgfLook.Hex("#4868A9");        // 코발트 도자 추
        static readonly Color Ivory = MgfLook.Hex("#F5E9CF");       // 무광 종이
        static readonly Color Shade = MgfLook.Hex("#C64958");       // 오답 잉크
        static readonly Color Moss = MgfLook.Hex("#A97B9D");        // 자주빛 한지

        Camera cam;
        Transform worldRoot, beetleRoot, netRoot, canopyRoot, titleNet, titleBeetle;
        readonly List<Transform> leaves = new List<Transform>();
        readonly List<Quaternion> panelBaseRotations = new List<Quaternion>();
        readonly Transform[] antennae = new Transform[2];
        readonly Transform[] nests = new Transform[6];
        readonly Transform[] nestGlows = new Transform[6];
        readonly Transform[] fireflies = new Transform[18];
        Material oliveMat, chartreuseMat, cocoaMat, fireMat, ivoryMat, mossMat, glowMat, ropeMat;
        float clock, titleFold, feedbackClock = 9f, hitStop, wrongKick, guideBoost, rippleClock, toastClock;
        bool feedbackGood;
        int lastW = -1, lastH = -1, shownSecond = -1;
        float lastScale = -1f, shownScore, shownEndScore;
        bool land;

        CanvasGroup titleG, hudG, revealG, endG, toastG;
        RectTransform titleRt, hudRt, revealRt, endRt, handleRt, shadeRt, errorMarkRt, rippleRt;
        RawImage titleArt;
        Texture2D titlePortrait, titleLandscape;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleBest, titleCtaTxt;
        TextMeshProUGUI scoreTxt, roundTxt, firstTxt, timeTxt, goalTxt, promptTxt, railValueTxt;
        TextMeshProUGUI rawLabelTxt, sortLabelTxt, definitionTxt, revealTitleTxt, revealEvidenceTxt, toastTxt, grabHintTxt;
        TextMeshProUGUI endTitleTxt, endScoreTxt, endStatsTxt, endTapTxt;
        Image statusBg, goalBg, promptBg, definitionBg, handleImg, shadeImg, errorMarkImg, guideRing, guidePath, guideHand, numberRailImg;
        Image revealBg, endBg, rippleImg;
        readonly Image[] lifeImgs = new Image[3];
        readonly Image[] rawCards = new Image[8];
        readonly TextMeshProUGUI[] rawTexts = new TextMeshProUGUI[8];
        readonly Image[] sortCards = new Image[8];
        readonly TextMeshProUGUI[] sortTexts = new TextMeshProUGUI[8];
        readonly Image[] ticks = new Image[23];
        readonly TextMeshProUGUI[] tickLabels = new TextMeshProUGUI[12];
        readonly Image[] trail = new Image[7];
        readonly float[] trailU = new float[7];
        Sprite roundedSprite, ringSprite, dotSprite;
        Vector2 logicalSize = new Vector2(390, 844);

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

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color, float width = 360f,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 2.8f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color;
            t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text; return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "LeafRing" : "LeafRound", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                if (ring)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - size * .40f) / (size * .055f));
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
            MgfLook.Sky(MgfLook.Hex("#E9D4AC"), MgfLook.Hex("#C78673"), MgfLook.Hex("#4C3048"), .60f);
            MgfLook.Sun(new Vector3(39, -34, 12), MgfLook.Hex("#FFF0CF"), 1.24f, .68f);
            cam = MgfLook.Camera(new Vector3(11.8f, 10.7f, -19.8f), new Vector3(0, 1.0f, 2.7f), 34f);
            oliveMat = MgfLook.Lit(Olive, .24f, .02f);
            chartreuseMat = MgfLook.Lit(Chartreuse, .20f, .01f, Chartreuse * .05f);
            cocoaMat = MgfLook.Lit(Cocoa, .30f, .03f);
            fireMat = MgfLook.Lit(Fire, .55f, .02f, Fire * .06f);
            ivoryMat = MgfLook.Lit(Ivory, .12f, .00f);
            mossMat = MgfLook.Lit(Moss, .18f, .01f);
            glowMat = MgfLook.Additive(new Color(Chartreuse.r, Chartreuse.g, Chartreuse.b, .90f), MgfLook.SoftDot);
            ropeMat = MgfLook.Lit(MgfLook.Hex("#C86F87"), .16f, .00f);

            worldRoot = new GameObject("HangingPaperChronograph").transform;
            canopyRoot = new GameObject("AirbornePaperGallery").transform; canopyRoot.SetParent(worldRoot, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "PorcelainCloudStage", new Vector3(0, -1.08f, 3.2f), new Vector3(13.6f, .24f, 6.3f), ivoryMat, canopyRoot, false);
            MgfLook.Block("MulberryWovenRunner", new Vector3(0, -.66f, 2.65f), new Vector3(19.4f, .20f, 3.5f), .32f, oliveMat, canopyRoot);
            for (int thread = 0; thread < 9; thread++)
                MgfLook.Block("WovenThread" + thread, new Vector3(-8.6f + thread * 2.15f, -.53f, 2.65f), new Vector3(.055f, .05f, 3.15f), .018f,
                    thread % 2 == 0 ? chartreuseMat : ropeMat, canopyRoot);
            for (int i = 0; i < 13; i++)
            {
                float x = -11.7f + i * 1.95f;
                float y = .16f + (i % 4) * .30f;
                float z = 5.8f - (i % 5) * .58f;
                Material paper = i % 4 == 0 ? chartreuseMat : i % 3 == 0 ? mossMat : ivoryMat;
                MgfLook.Block("SuspensionCord" + i, new Vector3(x, 5.35f, z), new Vector3(.045f, 3.0f + (i % 3) * .45f, .045f), .015f, ropeMat, canopyRoot);
                var pane = MgfLook.Block("HangingTimeScroll" + i, new Vector3(x, y + 2.15f, z), new Vector3(1.34f, 4.15f, .10f), .14f,
                    paper, canopyRoot).transform;
                pane.localRotation = Quaternion.Euler(0, -10f + (i % 5) * 4f, -6f + (i % 4) * 4f); leaves.Add(pane); panelBaseRotations.Add(pane.localRotation);
                for (int k = 0; k < 3; k++)
                    MgfLook.Block("InkStroke" + i + "_" + k, new Vector3(x, y + .88f + k * 1.08f, z - .13f), new Vector3(1.02f, .045f, .035f), .015f, cocoaMat, canopyRoot);
                MgfLook.Prim(PrimitiveType.Sphere, "PorcelainWeight" + i, new Vector3(x, y - .05f, z - .08f), new Vector3(.34f, .34f, .22f), fireMat, canopyRoot, false);
            }
            MgfLook.Block("ChronographRibbon", new Vector3(0, .42f, 3.32f), new Vector3(18.2f, .14f, .20f), .07f, ropeMat, worldRoot);
            for (int i = 0; i < nests.Length; i++)
            {
                float x = -7.25f + i * 2.90f; nests[i] = new GameObject("PaperTimeRosette" + i).transform; nests[i].SetParent(worldRoot, false); nests[i].localPosition = new Vector3(x, 1.00f + (i % 2) * .12f, 3.0f);
                for (int k = 0; k < 6; k++)
                {
                    float a = k * 60f;
                    var petal = MgfLook.Block("FoldedPetal" + k, new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * .35f, Mathf.Cos(a * Mathf.Deg2Rad) * .35f, 0), new Vector3(.20f, .58f, .07f), .08f,
                        k % 2 == 0 ? ivoryMat : mossMat, nests[i]).transform;
                    petal.localRotation = Quaternion.Euler(0, 0, -a);
                }
                nestGlows[i] = MgfLook.Prim(PrimitiveType.Sphere, "ReleasedTimePollen", new Vector3(0, 0, -.10f), new Vector3(.48f, .48f, .12f), glowMat, nests[i], false).transform;
                nestGlows[i].gameObject.SetActive(false);
            }
            beetleRoot = BuildBeetle(worldRoot, new Vector3(-.8f, .30f, .32f), 1.22f, false);
            titleBeetle = BuildBeetle(worldRoot, new Vector3(-4.8f, .42f, .86f), 1.18f, true);
            netRoot = BuildNet(worldRoot, new Vector3(2.0f, .36f, .50f), false);
            titleNet = BuildNet(worldRoot, new Vector3(3.8f, .38f, .94f), true);
            for (int i = 0; i < fireflies.Length; i++)
            {
                fireflies[i] = MgfLook.Prim(PrimitiveType.Cube, "TimePhoton" + i, new Vector3(0, 0, 0), new Vector3(.13f, .13f, .40f), glowMat, worldRoot, false).transform;
                fireflies[i].localRotation = Quaternion.Euler(25f + i * 7f, i * 23f, 45f);
                fireflies[i].localPosition = new Vector3(-7f + (i * 1.93f % 14f), 1.9f + (i % 5) * .32f, 1.2f + (i % 4) * .65f);
            }
        }

        Transform BuildBeetle(Transform parent, Vector3 pos, float scale, bool title)
        {
            var root = new GameObject(title ? "TitlePaperMoth" : "PaperMothArchivist").transform; root.SetParent(parent, false); root.localPosition = pos; root.localScale = Vector3.one * scale;
            MgfLook.Prim(PrimitiveType.Capsule, "FeltBody", new Vector3(0, .78f, 0), new Vector3(.48f, .62f, .52f), cocoaMat, root, false).transform.localRotation = Quaternion.Euler(12f, 0, 0);
            MgfLook.Prim(PrimitiveType.Sphere, "PorcelainThorax", new Vector3(0, .80f, -.54f), new Vector3(.72f, .62f, .50f), fireMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "PaperHead", new Vector3(0, .82f, -1.02f), new Vector3(.82f, .66f, .58f), mossMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeL", new Vector3(-.25f, .90f, -1.34f), new Vector3(.13f, .15f, .10f), ivoryMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeR", new Vector3(.25f, .90f, -1.34f), new Vector3(.13f, .15f, .10f), ivoryMat, root, false);
            for (int i = 0; i < 4; i++)
            {
                float side = i < 2 ? -1f : 1f, z = -.34f + (i % 2) * .72f;
                var wing = MgfLook.Prim(PrimitiveType.Sphere, "PaperWing" + i, new Vector3(side * 1.02f, 1.02f, z), new Vector3(1.46f, .13f, .72f), i % 2 == 0 ? ivoryMat : chartreuseMat, root, false).transform;
                wing.localRotation = Quaternion.Euler(8f, side * (20f + (i % 2) * 9f), side * (24f + (i % 2) * 12f));
                MgfLook.Block("StitchedWingVein" + i, new Vector3(side * .94f, 1.07f, z - .04f), new Vector3(1.32f, .04f, .07f), .015f, ropeMat, root).transform.localRotation = wing.localRotation;
            }
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var ant = MgfLook.Prim(PrimitiveType.Capsule, "Antenna" + i, new Vector3(side * .25f, 1.40f, -1.22f), new Vector3(.07f, .54f, .07f), fireMat, root, false).transform;
                ant.localRotation = Quaternion.Euler(side * 18f, 0, side * 22f); if (!title) antennae[i] = ant;
            }
            return root;
        }

        Transform BuildNet(Transform parent, Vector3 pos, bool title)
        {
            var root = new GameObject(title ? "TitleMedianLoom" : "MedianRibbonLoom").transform; root.SetParent(parent, false); root.localPosition = pos;
            for (int i = 0; i < 7; i++)
            {
                float x = -2.2f + i * .72f;
                var rope = MgfLook.Prim(PrimitiveType.Cube, "TensionFiber" + i, new Vector3(x, .38f + Mathf.Abs(i - 3) * .09f, 0), new Vector3(.055f, 1.38f, .055f), i == 3 ? chartreuseMat : ropeMat, root, false).transform;
                rope.localRotation = Quaternion.Euler(0, 0, -8f + i * 2.7f);
            }
            var roll = MgfLook.Prim(PrimitiveType.Capsule, "WovenRibbonRoll", new Vector3(0, -.46f, 0), new Vector3(.22f, 2.58f, .22f), ropeMat, root, false).transform;
            roll.localRotation = Quaternion.Euler(0, 0, 90f);
            MgfLook.Prim(PrimitiveType.Sphere, "PorcelainGrip", new Vector3(0, -.78f, -.04f), new Vector3(1.56f, .48f, .56f), fireMat, root, false);
            for (int i = 0; i < 6; i++)
            {
                MgfLook.Prim(PrimitiveType.Sphere, "ThreadKnot" + i, new Vector3(-1.8f + i * .72f, -.47f, -.28f), new Vector3(.19f, .19f, .19f), chartreuseMat, root, false);
            }
            return root;
        }

        void BuildUi()
        {
            roundedSprite = MakeSprite(96, 8, false); ringSprite = MakeSprite(96, 0, true);
            dotSprite = Sprite.Create(MgfLook.SoftDot, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 100);
            var canvas = MgfText.Canvas;
            // 타이틀과 플레이 모두 같은 실시간 종이 회랑을 보여 준다. 생성 키 아트는 사용하지 않는다.
            titlePortrait = null; titleLandscape = null;

            titleRt = R("TitleScreen", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            var artRt = R("LocalTitleKeyArt", titleRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844));
            titleArt = artRt.gameObject.AddComponent<RawImage>(); titleArt.raycastTarget = false; titleArt.color = Color.white;
            titleLogo = Txt(titleRt, "가운데\n잡아", new Vector2(.66f, .79f), Vector2.zero, 55, Ivory, 250); titleLogo.fontStyle = FontStyles.Bold; titleLogo.outlineColor = Cocoa; titleLogo.outlineWidth = .25f; titleLogo.lineSpacing = -22;
            titleTag = Txt(titleRt, "시간 띠의 가운데를 접어라", new Vector2(.62f, .65f), Vector2.zero, 17, Chartreuse, 330); titleTag.fontStyle = FontStyles.Bold; titleTag.outlineColor = Ivory; titleTag.outlineWidth = .15f;
            titleMeta = Txt(titleRt, "중학교 1학년 · 중앙값 관측 기록", new Vector2(.62f, .595f), Vector2.zero, 12, Ivory, 330); titleMeta.outlineColor = Cocoa; titleMeta.outlineWidth = .13f;
            titleBest = Txt(titleRt, "종이꽃 6송이 · 첫 시도 5번", new Vector2(.5f, .20f), Vector2.zero, 14, Ivory, 370); titleBest.outlineColor = Cocoa; titleBest.outlineWidth = .14f;
            titleCtaTxt = Txt(titleRt, "도자 추를 끌어 시작", new Vector2(.5f, .115f), Vector2.zero, 21, Fire, 360); titleCtaTxt.fontStyle = FontStyles.Bold; titleCtaTxt.outlineColor = Ivory; titleCtaTxt.outlineWidth = .18f;

            hudRt = R("GameHud", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            statusBg = Img(hudRt, "ObservatoryStatus", new Vector2(.42f, .955f), Vector2.zero, new Vector2(320, 58), new Color(Olive.r, Olive.g, Olive.b, .94f), roundedSprite);
            scoreTxt = Txt(statusBg.transform, "0", new Vector2(.10f, .54f), Vector2.zero, 20, Ivory, 60); scoreTxt.fontStyle = FontStyles.Bold;
            roundTxt = Txt(statusBg.transform, "종이꽃 0/6", new Vector2(.31f, .54f), Vector2.zero, 14, Ivory, 100);
            firstTxt = Txt(statusBg.transform, "첫 0/0", new Vector2(.54f, .54f), Vector2.zero, 14, Ivory, 86);
            timeTxt = Txt(statusBg.transform, "110초", new Vector2(.78f, .54f), Vector2.zero, 15, Chartreuse, 64); timeTxt.fontStyle = FontStyles.Bold;
            for (int i = 0; i < 3; i++) lifeImgs[i] = Img(statusBg.transform, "ClampLock" + i, new Vector2(.72f + i * .08f, .18f), Vector2.zero, new Vector2(18, 9), Fire, roundedSprite);

            goalBg = Img(hudRt, "MissionLedger", new Vector2(.5f, .88f), Vector2.zero, new Vector2(360, 42), new Color(Moss.r, Moss.g, Moss.b, .96f), roundedSprite);
            goalTxt = Txt(goalBg.transform, "중앙값 위치에서 시간 띠를 접으시오", new Vector2(.5f, .52f), Vector2.zero, 15, Ivory, 344); goalTxt.fontStyle = FontStyles.Bold; goalTxt.enableAutoSizing = true; goalTxt.fontSizeMin = 11; goalTxt.fontSizeMax = 15;
            promptBg = Img(hudRt, "ObservationPrompt", new Vector2(.5f, .805f), Vector2.zero, new Vector2(360, 82), new Color(Olive.r, Olive.g, Olive.b, .95f), roundedSprite);
            promptTxt = Txt(promptBg.transform, "", new Vector2(.5f, .52f), Vector2.zero, 14, Ivory, 338); promptTxt.fontStyle = FontStyles.Bold; promptTxt.enableAutoSizing = true; promptTxt.fontSizeMin = 10; promptTxt.fontSizeMax = 15; promptTxt.margin = new Vector4(8, 5, 8, 5);
            rawLabelTxt = Txt(hudRt, "원자료 · 관측 시간(초)", new Vector2(.5f, .72f), Vector2.zero, 12, Chartreuse, 350); rawLabelTxt.outlineColor = Cocoa; rawLabelTxt.outlineWidth = .12f;
            sortLabelTxt = Txt(hudRt, "정리 줄 · 끌어서 순서를 바꿀 수 있다", new Vector2(.5f, .55f), Vector2.zero, 12, Chartreuse, 360); sortLabelTxt.outlineColor = Cocoa; sortLabelTxt.outlineWidth = .12f;
            for (int i = 0; i < 8; i++)
            {
                rawCards[i] = Img(hudRt, "RawGlassTag" + i, new Vector2(.5f, .66f), Vector2.zero, new Vector2(42, 48), new Color(Ivory.r, Ivory.g, Ivory.b, .94f), roundedSprite);
                rawTexts[i] = Txt(rawCards[i].transform, "", new Vector2(.5f, .5f), Vector2.zero, 19, Cocoa, 42); rawTexts[i].fontStyle = FontStyles.Bold;
                sortCards[i] = Img(hudRt, "SortedGlassTag" + i, new Vector2(.5f, .48f), Vector2.zero, new Vector2(42, 48), new Color(Moss.r, Moss.g, Moss.b, .96f), roundedSprite);
                sortTexts[i] = Txt(sortCards[i].transform, "", new Vector2(.5f, .5f), Vector2.zero, 19, Ivory, 42); sortTexts[i].fontStyle = FontStyles.Bold;
            }

            definitionBg = Img(hudRt, "Definition", new Vector2(.5f, .355f), Vector2.zero, new Vector2(360, 48), new Color(Cocoa.r, Cocoa.g, Cocoa.b, .94f), roundedSprite);
            definitionTxt = Txt(definitionBg.transform, "중앙값: 크기순 가운데 값 · 짝수 개는 가운데 두 값의 평균", new Vector2(.5f, .5f), Vector2.zero, 12, Ivory, 344); definitionTxt.fontStyle = FontStyles.Bold; definitionTxt.enableAutoSizing = true; definitionTxt.fontSizeMin = 9; definitionTxt.fontSizeMax = 12;
            shadeImg = Img(hudRt, "RisingArchiveAlarm", new Vector2(.5f, .02f), Vector2.zero, new Vector2(390, 5), new Color(Shade.r, Shade.g, Shade.b, .80f), roundedSprite); shadeRt = shadeImg.rectTransform;
            for (int i = 0; i < ticks.Length; i++)
            {
                float u = i / 22f;
                ticks[i] = Img(hudRt, "RailTick" + i, new Vector2(.075f + .85f * u, .18f), Vector2.zero, new Vector2(i % 2 == 0 ? 4 : 2, i % 2 == 0 ? 25 : 14), Cocoa, roundedSprite);
                if (i % 2 == 0)
                {
                    int idx = i / 2; tickLabels[idx] = Txt(hudRt, (idx + 1).ToString(), new Vector2(.075f + .85f * u, .125f), Vector2.zero, 12, Ivory, 30); tickLabels[idx].outlineColor = Cocoa; tickLabels[idx].outlineWidth = .12f;
                }
            }
            numberRailImg = Img(hudRt, "NumberRail", new Vector2(.5f, .18f), Vector2.zero, new Vector2(334, 6), Fire, roundedSprite); numberRailImg.transform.SetAsFirstSibling();
            for (int i = 0; i < trail.Length; i++) trail[i] = Img(hudRt, "NetTrail" + i, new Vector2(.075f, .18f), Vector2.zero, new Vector2(26 - i * 2, 26 - i * 2), new Color(Fire.r, Fire.g, Fire.b, .14f - i * .012f), dotSprite);
            handleImg = Img(hudRt, "ActualPorcelainWeight", new Vector2(.075f, .18f), Vector2.zero, new Vector2(92, 68), Fire, roundedSprite); handleRt = handleImg.rectTransform;
            railValueTxt = Txt(handleImg.transform, "1", new Vector2(.5f, .5f), new Vector2(0, 62), 24, Ivory, 106); railValueTxt.fontStyle = FontStyles.Bold; railValueTxt.outlineColor = Cocoa; railValueTxt.outlineWidth = .18f;
            grabHintTxt = Txt(handleImg.transform, "여기를 잡아", new Vector2(.5f, .5f), Vector2.zero, 12, Cocoa, 86); grabHintTxt.fontStyle = FontStyles.Bold;
            errorMarkImg = Img(hudRt, "WrongExistingMark", new Vector2(.5f, .18f), Vector2.zero, new Vector2(28, 74), new Color(Shade.r, Shade.g, Shade.b, .85f), ringSprite); errorMarkRt = errorMarkImg.rectTransform;
            guidePath = Img(hudRt, "ClampDragPath", new Vector2(.5f, .18f), Vector2.zero, new Vector2(250, 8), new Color(Chartreuse.r, Chartreuse.g, Chartreuse.b, .78f), roundedSprite);
            guideRing = Img(hudRt, "ClampPulse", new Vector2(.075f, .18f), Vector2.zero, new Vector2(112, 88), Chartreuse, ringSprite);
            guideHand = Img(hudRt, "ClampGhost", new Vector2(.075f, .18f), Vector2.zero, new Vector2(46, 34), new Color(Fire.r, Fire.g, Fire.b, .72f), roundedSprite);

            revealRt = R("EvidenceReveal", canvas.transform, new Vector2(.5f, .31f), Vector2.zero, new Vector2(360, 142)); revealG = revealRt.gameObject.AddComponent<CanvasGroup>();
            revealBg = Img(revealRt, "EvidenceGlass", new Vector2(.5f, .5f), Vector2.zero, new Vector2(360, 142), new Color(Olive.r, Olive.g, Olive.b, .98f), roundedSprite);
            revealTitleTxt = Txt(revealBg.transform, "", new Vector2(.5f, .74f), Vector2.zero, 20, Moss, 338); revealTitleTxt.fontStyle = FontStyles.Bold;
            revealEvidenceTxt = Txt(revealBg.transform, "", new Vector2(.5f, .38f), Vector2.zero, 14, Ivory, 338); revealEvidenceTxt.fontStyle = FontStyles.Bold; revealEvidenceTxt.enableAutoSizing = true; revealEvidenceTxt.fontSizeMin = 10; revealEvidenceTxt.fontSizeMax = 15;

            toastG = R("ArchiveToast", canvas.transform, new Vector2(.5f, .40f), Vector2.zero, new Vector2(350, 48)).gameObject.AddComponent<CanvasGroup>();
            var toastBg = Img(toastG.transform, "ToastBg", new Vector2(.5f, .5f), Vector2.zero, new Vector2(350, 48), new Color(Cocoa.r, Cocoa.g, Cocoa.b, .96f), roundedSprite);
            toastTxt = Txt(toastBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 13, Ivory, 330);
            rippleImg = Img(canvas.transform, "GlassPulseRipple", new Vector2(.5f, .5f), Vector2.zero, new Vector2(68, 68), Chartreuse, ringSprite); rippleRt = rippleImg.rectTransform; rippleRt.gameObject.SetActive(false);

            endRt = R("EndScreen", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); endG = endRt.gameObject.AddComponent<CanvasGroup>();
            endBg = Img(endRt, "ResultLedger", new Vector2(.5f, .54f), Vector2.zero, new Vector2(350, 402), new Color(Olive.r, Olive.g, Olive.b, .98f), roundedSprite);
            endTitleTxt = Txt(endBg.transform, "", new Vector2(.5f, .80f), Vector2.zero, 30, Moss, 326); endTitleTxt.fontStyle = FontStyles.Bold;
            endScoreTxt = Txt(endBg.transform, "0", new Vector2(.5f, .61f), Vector2.zero, 58, Ivory, 320); endScoreTxt.fontStyle = FontStyles.Bold;
            endStatsTxt = Txt(endBg.transform, "", new Vector2(.5f, .39f), Vector2.zero, 17, Ivory, 318); endStatsTxt.fontStyle = FontStyles.Bold;
            var restart = Img(endBg.transform, "RestartClamp", new Vector2(.5f, .12f), Vector2.zero, new Vector2(300, 68), Fire, roundedSprite);
            endTapTxt = Txt(restart.transform, "새 표본 기록 열기", new Vector2(.5f, .5f), Vector2.zero, 21, Cocoa, 282); endTapTxt.fontStyle = FontStyles.Bold;
        }

        void Relayout(bool force)
        {
            float scale = Mathf.Max(.01f, MgfText.Canvas.scaleFactor);
            if (!force && lastW == Screen.width && lastH == Screen.height && Mathf.Abs(lastScale - scale) < .001f) return;
            lastW = Screen.width; lastH = Screen.height; lastScale = scale; land = MgfBridge.IsLandLayout;
            Vector2 logical = new Vector2(Screen.width / scale, Screen.height / scale); if (logical.x < 100 || logical.y < 100) logical = new Vector2(390, 844);
            logicalSize = logical; titleRt.sizeDelta = logical; hudRt.sizeDelta = logical; endRt.sizeDelta = logical;
            MgfLook.FitWidth(cam, land ? 34f : 38f, .72f);
            if (land)
            {
                cam.transform.position = new Vector3(12.8f, 9.8f, -20.8f); cam.transform.LookAt(new Vector3(0, 1.05f, 2.8f));
                titleLogo.rectTransform.anchorMin = titleLogo.rectTransform.anchorMax = new Vector2(.73f, .72f);
                titleTag.rectTransform.anchorMin = titleTag.rectTransform.anchorMax = new Vector2(.73f, .51f);
                titleMeta.rectTransform.anchorMin = titleMeta.rectTransform.anchorMax = new Vector2(.73f, .43f);
                statusBg.rectTransform.anchorMin = statusBg.rectTransform.anchorMax = new Vector2(.50f, .945f);
                statusBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(560, logical.x * .46f), 58);

                // 가로는 좌측 기록실 / 중앙 3D 관측소 / 우측 정의·근거 / 하단 전폭 눈금의 3존이다.
                float leftPanelW = Mathf.Min(450f, logical.x * .36f);
                float rightPanelW = Mathf.Min(420f, logical.x * .34f);
                SetAnchor(goalBg.rectTransform, new Vector2(.19f, .855f)); goalBg.rectTransform.sizeDelta = new Vector2(leftPanelW, 36);
                goalTxt.rectTransform.sizeDelta = new Vector2(leftPanelW - 20, 32);
                SetAnchor(promptBg.rectTransform, new Vector2(.19f, .715f)); promptBg.rectTransform.sizeDelta = new Vector2(leftPanelW, 64);
                promptTxt.rectTransform.sizeDelta = new Vector2(leftPanelW - 24, 58);
                SetAnchor(rawLabelTxt.rectTransform, new Vector2(.19f, .605f)); rawLabelTxt.rectTransform.sizeDelta = new Vector2(leftPanelW, 34);
                SetAnchor(sortLabelTxt.rectTransform, new Vector2(.19f, .415f)); sortLabelTxt.rectTransform.sizeDelta = new Vector2(leftPanelW, 34);
                SetAnchor(definitionBg.rectTransform, new Vector2(.80f, .80f)); definitionBg.rectTransform.sizeDelta = new Vector2(rightPanelW, 62);
                definitionTxt.rectTransform.sizeDelta = new Vector2(rightPanelW - 22, 54);
                SetAnchor(revealRt, new Vector2(.80f, .60f)); revealRt.sizeDelta = new Vector2(rightPanelW, 154);
                revealBg.rectTransform.sizeDelta = new Vector2(rightPanelW, 154);
                revealTitleTxt.rectTransform.sizeDelta = new Vector2(rightPanelW - 20, 52);
                revealEvidenceTxt.rectTransform.sizeDelta = new Vector2(rightPanelW - 22, 88);
            }
            else
            {
                cam.transform.position = new Vector3(11.8f, 11.4f, -18.6f); cam.transform.LookAt(new Vector3(0, 1.4f, 2.4f));
                titleLogo.rectTransform.anchorMin = titleLogo.rectTransform.anchorMax = new Vector2(.62f, .80f);
                titleTag.rectTransform.anchorMin = titleTag.rectTransform.anchorMax = new Vector2(.60f, .67f);
                titleMeta.rectTransform.anchorMin = titleMeta.rectTransform.anchorMax = new Vector2(.59f, .61f);
                statusBg.rectTransform.anchorMin = statusBg.rectTransform.anchorMax = new Vector2(.42f, .955f);
                statusBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(320, logical.x - 68), 58);
                SetAnchor(goalBg.rectTransform, new Vector2(.50f, .88f)); goalBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(366, logical.x * .95f), 42);
                goalTxt.rectTransform.sizeDelta = new Vector2(Mathf.Min(344, logical.x * .88f), 42);
                SetAnchor(promptBg.rectTransform, new Vector2(.50f, .805f));
                promptBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(366, logical.x * .95f), 82);
                promptTxt.rectTransform.sizeDelta = new Vector2(Mathf.Min(338, logical.x * .88f), 76);
                SetAnchor(rawLabelTxt.rectTransform, new Vector2(.50f, .72f)); rawLabelTxt.rectTransform.sizeDelta = new Vector2(Mathf.Min(350, logical.x * .92f), 34);
                SetAnchor(sortLabelTxt.rectTransform, new Vector2(.50f, .55f)); sortLabelTxt.rectTransform.sizeDelta = new Vector2(Mathf.Min(360, logical.x * .94f), 34);
                SetAnchor(definitionBg.rectTransform, new Vector2(.50f, .355f)); definitionBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(360, logical.x * .94f), 48);
                definitionTxt.rectTransform.sizeDelta = new Vector2(Mathf.Min(344, logical.x * .88f), 44);
                SetAnchor(revealRt, new Vector2(.50f, .31f)); revealRt.sizeDelta = new Vector2(Mathf.Min(360, logical.x * .94f), 142);
                revealBg.rectTransform.sizeDelta = revealRt.sizeDelta;
                revealTitleTxt.rectTransform.sizeDelta = new Vector2(Mathf.Min(338, logical.x * .88f), 52);
                revealEvidenceTxt.rectTransform.sizeDelta = new Vector2(Mathf.Min(338, logical.x * .88f), 84);
            }
            Texture2D tex = land ? titleLandscape : titlePortrait;
            titleArt.texture = tex; titleArt.enabled = tex != null;
            titleArt.rectTransform.sizeDelta = logical; titleArt.uvRect = CoverUv(tex, logical.x / Mathf.Max(1, logical.y));
            SetAnchor(numberRailImg.rectTransform, new Vector2(.5f, RailYFraction()));
            numberRailImg.rectTransform.sizeDelta = new Vector2(logical.x * (RailRightFraction() - RailLeftFraction()), 6);
            shadeRt.sizeDelta = new Vector2(logical.x, 5);
            for (int i = 0; i < ticks.Length; i++)
            {
                float u = i / 22f;
                SetAnchor(ticks[i].rectTransform, new Vector2(Mathf.Lerp(RailLeftFraction(), RailRightFraction(), u), RailYFraction()));
                if (i % 2 == 0) SetAnchor(tickLabels[i / 2].rectTransform, new Vector2(Mathf.Lerp(RailLeftFraction(), RailRightFraction(), u), land ? .095f : .125f));
            }
            PositionTags(); UpdateNetUi();
        }

        static Rect CoverUv(Texture2D tex, float screenAspect)
        {
            if (!tex) return new Rect(0, 0, 1, 1);
            float texAspect = (float)tex.width / tex.height;
            if (screenAspect < texAspect) { float w = screenAspect / texAspect; return new Rect((1f - w) * .5f, 0, w, 1); }
            float h = texAspect / screenAspect; return new Rect(0, (1f - h) * .5f, 1, h);
        }

        void PositionTags()
        {
            int count = current == null ? 0 : current.raw.Length;
            for (int i = 0; i < 8; i++)
            {
                bool active = i < count; rawCards[i].gameObject.SetActive(active); sortCards[i].gameObject.SetActive(active); if (!active) continue;
                if (land)
                {
                    float u = count == 1 ? .5f : i / (float)(count - 1);
                    SetAnchor(rawCards[i].rectTransform, new Vector2(.055f + .27f * u, .535f));
                    SetAnchor(sortCards[i].rectTransform, new Vector2(.055f + .27f * u, .345f));
                    rawCards[i].rectTransform.sizeDelta = sortCards[i].rectTransform.sizeDelta = new Vector2(42, 48);
                }
                else
                {
                    int cols = Math.Min(4, count), row = i / 4, col = i % 4;
                    float rawY = count > 4 ? .675f - row * .062f : .66f;
                    float rawX = .17f + col * .22f + (row == 1 && count - 4 < 4 ? (4 - (count - 4)) * .11f : 0f);
                    SetAnchor(rawCards[i].rectTransform, new Vector2(rawX, rawY));
                    float u = count == 1 ? .5f : i / (float)(count - 1);
                    SetAnchor(sortCards[i].rectTransform, new Vector2(.09f + .82f * u, .49f));
                    rawCards[i].rectTransform.sizeDelta = new Vector2(48, 46); sortCards[i].rectTransform.sizeDelta = new Vector2(40, 48);
                }
            }
        }

        static void SetAnchor(RectTransform rt, Vector2 a) { rt.anchorMin = rt.anchorMax = a; rt.anchoredPosition = Vector2.zero; }

        void ResetVisuals()
        {
            titleFold = 0f; feedbackClock = 9f; wrongKick = 0f; hitStop = 0f; guideBoost = 0f; rippleClock = toastClock = 0f; shownScore = shownEndScore = 0f; shownSecond = -1;
            for (int i = 0; i < nests.Length; i++) if (nestGlows[i]) nestGlows[i].gameObject.SetActive(false);
            if (titleNet) titleNet.gameObject.SetActive(true); if (titleBeetle) titleBeetle.gameObject.SetActive(true);
            if (netRoot) netRoot.gameObject.SetActive(false); if (beetleRoot) beetleRoot.gameObject.SetActive(false);
        }

        void ShowScreen()
        {
            bool title = flow == Flow.Title || flow == Flow.Opening;
            bool end = flow == Flow.End;
            titleG.alpha = title ? 1f : 0f; titleG.blocksRaycasts = false;
            hudG.alpha = !title && !end ? 1f : 0f;
            revealG.alpha = flow == Flow.Reveal ? 1f : 0f;
            endG.alpha = end ? 1f : 0f;
            if (titleNet) titleNet.gameObject.SetActive(title); if (titleBeetle) titleBeetle.gameObject.SetActive(title);
            if (netRoot) netRoot.gameObject.SetActive(!title); if (beetleRoot) beetleRoot.gameObject.SetActive(!title);
            titleBest.text = PlayerPrefs.GetInt("gaunde-jaba-best-first", 0) > 0 ? "최고 첫 시도 " + PlayerPrefs.GetInt("gaunde-jaba-best-first", 0) + "/6" : "종이꽃 6송이 · 첫 시도 5번";
            if (!title && !end) UpdateHudText();
        }

        void BeginTitleFold() { titleFold = .0001f; MgfFx.Punch(titleNet, .20f, .30f); }

        void LoadProblemVisual(MedianProblem p, bool practice)
        {
            promptTxt.text = p.prompt;
            goalTxt.text = practice ? (p.id == "practice-odd" ? "9, 2, 5를 정렬해 5초에 놓으시오" : "2, 4, 6, 8의 가운데 둘을 평균 내시오") : "중앙값 위치에서 시간 띠를 접으시오";
            rawLabelTxt.text = "원자료 · 관측 시간(초) · " + p.raw.Length + "개";
            for (int i = 0; i < 8; i++)
            {
                if (i < p.raw.Length) { rawTexts[i].text = p.raw[i].ToString(); sortTexts[i].text = st.studentOrder[i].ToString(); }
            }
            errorMarkImg.gameObject.SetActive(p.errorFind);
            if (p.errorFind) { SetAnchor(errorMarkRt, new Vector2(RailXFor(p.wrongMark2), RailYFraction())); }
            revealG.alpha = 0f; guideBoost = practice ? 1f : .45f; PositionTags(); UpdateNetUi(); UpdateHudText();
        }

        void UpdateHudText()
        {
            roundTxt.text = IsPractice ? "연습" : "종이꽃 " + st.solved + "/6";
            firstTxt.text = "첫 " + st.firstCorrect + "/" + st.attemptedRounds;
            for (int i = 0; i < lifeImgs.Length; i++) lifeImgs[i].color = i < st.lives ? Fire : new Color(Shade.r, Shade.g, Shade.b, .28f);
            definitionTxt.text = st.level >= 2 ? "중앙값: 크기순 가운데 값 · 짝수 개는 가운데 두 값의 평균" : "중앙값: 자료를 크기순으로 놓았을 때 가운데 값";
        }

        void BoostGuide(bool strong) { guideBoost = strong ? 1.8f : 1f; toastClock = 1.5f; toastTxt.text = strong ? "‘여기를 잡아’ 도자 추를 끌어 목표 눈금에서 놓으시오" : "아래 도자 추를 좌우로 끌어 놓으시오"; }

        void BeginRevealVisual(bool correct, bool practice, int value2, string misconception)
        {
            feedbackClock = 0f; feedbackGood = correct; hitStop = correct ? .075f : 0f; wrongKick = correct ? 0f : 1f; guideBoost = 0f;
            revealTitleTxt.text = correct ? (practice ? "가운데를 잡았다" : "시간 띠가 종이꽃으로 접혔다") : "직조 매듭이 풀렸다";
            revealTitleTxt.color = correct ? Chartreuse : Shade;
            revealEvidenceTxt.text = correct ? MedianRules.Evidence(current) : MedianRules.MisconceptionKorean(misconception) + "\n" + MedianRules.Evidence(current);
            if (correct)
            {
                MgfSfx.Play("correct", .46f); MgfFx.Punch(netRoot, .16f, .30f); MgfFx.Glow(new Vector3(1.8f, 1.5f, .2f), Fire, 9, .32f);
                if (!practice && st.solved > 0 && st.solved <= nests.Length) { nestGlows[st.solved - 1].gameObject.SetActive(true); MgfFx.Punch(nests[st.solved - 1], .22f, .34f); MgfFx.Glow(nests[st.solved - 1].position, Chartreuse, 11, .32f); }
            }
            else
            {
                MgfSfx.Play("wrong", .28f); MgfFx.Punch(beetleRoot, .10f, .26f);
            }
            UpdateHudText();
        }

        void FinishRevealVisual(bool correct) { feedbackClock = 9f; if (netRoot) netRoot.localScale = Vector3.one; }
        void RestoreAfterWrong() { revealG.alpha = 0f; UpdateNetUi(); guideBoost = .55f; }
        void BeginNetDrag() { for (int i = 0; i < trailU.Length; i++) trailU[i] = ValueU(st.netValue2); handleRt.localScale = new Vector3(1.14f, .82f, 1f); MgfSfx.Play("tap", .18f); UpdateNetUi(); }
        void UpdateNetDrag() { for (int i = trailU.Length - 1; i > 0; i--) trailU[i] = trailU[i - 1]; trailU[0] = ValueU(st.netValue2); UpdateNetUi(); }
        void EndNetDrag() { handleRt.localScale = Vector3.one; }
        void BeginTagDrag(int index) { if (index >= 0) sortCards[index].rectTransform.localScale = Vector3.one * 1.16f; }
        void UpdateStudentOrder() { for (int i = 0; i < st.studentOrder.Length; i++) sortTexts[i].text = st.studentOrder[i].ToString(); PositionTags(); }
        void EndTagDrag(int index) { if (index >= 0 && index < sortCards.Length) sortCards[index].rectTransform.localScale = Vector3.one; }

        void Ripple(Vector2 screen, string message)
        {
            float scale = Mathf.Max(.01f, MgfText.Canvas.scaleFactor);
            rippleRt.anchorMin = rippleRt.anchorMax = new Vector2(0, 0); rippleRt.anchoredPosition = screen / scale; rippleRt.localScale = Vector3.one * .35f;
            rippleRt.gameObject.SetActive(true); rippleClock = .48f; toastClock = 1.35f; toastTxt.text = message;
        }

        void ShowEndVisual(bool clear, string reason)
        {
            endTitleTxt.text = clear ? "시간 종이꽃 6송이 완성" : reason == "knots" ? "직조 매듭이 모두 풀렸다" : reason == "time" ? "관측 시간이 끝났다" : "첫 시도 5번이 필요하다";
            endScoreTxt.text = "0"; shownEndScore = 0f;
            endStatsTxt.text = "종이꽃 " + st.solved + "/6\n첫 시도 " + st.firstCorrect + "/" + st.attemptedRounds + "\n남은 매듭 " + st.lives;
            endTapTxt.text = "새 표본 기록 열기";
            if (clear) for (int i = 0; i < nests.Length; i++) { nestGlows[i].gameObject.SetActive(true); MgfFx.Glow(nests[i].position, Fire, 6, .26f); }
        }

        void TickView(float dt)
        {
            clock += dt;
            if (titleFold > 0f)
            {
                titleFold += dt; float k = Mathf.Clamp01(titleFold / .40f); titleG.alpha = 1f - k; if (titleNet) titleNet.localScale = Vector3.one * (1f + .28f * Mathf.Sin(k * Mathf.PI));
            }
            for (int i = 0; i < leaves.Count; i++)
                leaves[i].localRotation = panelBaseRotations[i] * Quaternion.Euler(0, Mathf.Sin(clock * .33f + i) * .55f, Mathf.Sin(clock * .52f + i * .7f) * .38f);
            if (beetleRoot) beetleRoot.localPosition = new Vector3(-.8f, .30f + Mathf.Sin(clock * 2.1f) * .055f, .32f);
            for (int i = 0; i < antennae.Length; i++) if (antennae[i]) antennae[i].localRotation = Quaternion.Euler((i == 0 ? -18f : 18f) + Mathf.Sin(clock * 3f + i) * 5f, 0, i == 0 ? -22f : 22f);
            for (int i = 0; i < fireflies.Length; i++)
            {
                float a = clock * (.45f + (i % 3) * .08f) + i * 1.77f;
                Vector3 basePos = new Vector3(-7f + (i * 1.93f % 14f), 2.2f + (i % 5) * .34f, 1.3f + (i % 4) * .63f);
                if (feedbackGood && feedbackClock < .9f)
                {
                    float k = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.22f, .88f, feedbackClock));
                    int nest = Mathf.Clamp(st.solved - 1, 0, 5); basePos = Vector3.Lerp(new Vector3(1.8f + Mathf.Sin(a) * 1.2f, 1.7f, .5f), nests[nest].position, k);
                }
                fireflies[i].localPosition = basePos + new Vector3(Mathf.Sin(a) * .22f, Mathf.Cos(a * 1.37f) * .20f, Mathf.Sin(a * .8f) * .12f);
                fireflies[i].localScale = Vector3.one * (.85f + .18f * Mathf.Sin(clock * 4.2f + i));
            }
            if (feedbackClock < 2f)
            {
                feedbackClock += dt;
                if (feedbackGood)
                {
                    if (feedbackClock < hitStop) { if (netRoot) netRoot.localScale = new Vector3(1.05f, .72f, 1.08f); }
                    else if (netRoot) { float k = Mathf.InverseLerp(hitStop, .55f, feedbackClock); netRoot.localScale = new Vector3(1f + .18f * Mathf.Sin(k * Mathf.PI * 2f) * (1f - k), 1f - .10f * Mathf.Sin(k * Mathf.PI), 1f); }
                }
                else if (beetleRoot && wrongKick > 0f)
                {
                    wrongKick = Mathf.Max(0, wrongKick - dt * 3.8f); beetleRoot.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(clock * 34f) * 7f * wrongKick);
                }
            }
            else if (beetleRoot) beetleRoot.localRotation = Quaternion.identity;

            float targetScore = st.score; shownScore = Mathf.MoveTowards(shownScore, targetScore, dt * Mathf.Max(120f, Mathf.Abs(targetScore - shownScore) * 5f)); scoreTxt.text = Mathf.RoundToInt(shownScore).ToString();
            if (flow == Flow.Playing)
            {
                int sec = Mathf.CeilToInt(timeLeft); if (sec != shownSecond) { shownSecond = sec; timeTxt.text = sec + "초"; }
                float danger = Mathf.Clamp01(1f - timeLeft / MedianRules.SessionSeconds + (3 - st.lives) * .15f);
                SetAnchor(shadeRt, new Vector2(.5f, .025f + danger * .30f)); shadeImg.color = new Color(Shade.r, Shade.g, Shade.b, .35f + danger * .50f);
            }
            else if (IsPractice) timeTxt.text = "연습";
            if (flow == Flow.End)
            {
                shownEndScore = Mathf.MoveTowards(shownEndScore, st.score, dt * Mathf.Max(100f, st.score * 1.7f)); endScoreTxt.text = Mathf.RoundToInt(shownEndScore).ToString();
            }
            if (guideBoost > 0f)
            {
                guideBoost = Mathf.Max(.35f, guideBoost - dt * .18f); float pulse = .90f + .14f * Mathf.Sin(clock * 5f);
                guideRing.gameObject.SetActive(true); guidePath.gameObject.SetActive(true); guideHand.gameObject.SetActive(true);
                grabHintTxt.gameObject.SetActive(IsPractice && !draggingNet);
                SetAnchor(guideRing.rectTransform, new Vector2(RailXFor(st.netValue2), RailYFraction())); guideRing.rectTransform.localScale = Vector3.one * pulse;
                float from = RailXFor(st.netValue2), to = current == null ? .5f : RailXFor(current.answer2);
                float travel = (clock * .55f) % 1f; SetAnchor(guideHand.rectTransform, new Vector2(Mathf.Lerp(from, to, travel), RailYFraction()));
                SetAnchor(guidePath.rectTransform, new Vector2((from + to) * .5f, RailYFraction())); guidePath.rectTransform.sizeDelta = new Vector2(Mathf.Abs(to - from) * logicalSize.x, 7);
            }
            else { guideRing.gameObject.SetActive(false); guidePath.gameObject.SetActive(false); guideHand.gameObject.SetActive(false); grabHintTxt.gameObject.SetActive(false); }
            if (rippleClock > 0f)
            {
                rippleClock -= dt; float k = 1f - rippleClock / .48f; rippleRt.localScale = Vector3.one * Mathf.Lerp(.35f, 1.6f, k); rippleImg.color = new Color(Chartreuse.r, Chartreuse.g, Chartreuse.b, 1f - k); if (rippleClock <= 0f) rippleRt.gameObject.SetActive(false);
            }
            toastG.alpha = toastClock > 0f ? Mathf.Clamp01(toastClock * 3f) : 0f; if (toastClock > 0f) toastClock -= dt;
            UpdateNetUi();
        }

        void UpdateNetUi()
        {
            float x = RailXFor(st.netValue2); SetAnchor(handleRt, new Vector2(x, RailYFraction())); railValueTxt.text = MedianRules.FormatHalf(st.netValue2) + "초";
            if (!draggingNet) for (int i = 0; i < trailU.Length; i++) trailU[i] = ValueU(st.netValue2);
            for (int i = 0; i < trail.Length; i++) { SetAnchor(trail[i].rectTransform, new Vector2(Mathf.Lerp(RailLeftFraction(), RailRightFraction(), trailU[i]), RailYFraction())); trail[i].gameObject.SetActive(draggingNet); }
            if (current != null && current.errorFind) SetAnchor(errorMarkRt, new Vector2(RailXFor(current.wrongMark2), RailYFraction()));
        }

        float ValueU(int value2) => Mathf.Clamp01((value2 - 2) / 22f);
        float RailXFor(int value2) => Mathf.Lerp(RailLeftFraction(), RailRightFraction(), ValueU(value2));
    }
}
