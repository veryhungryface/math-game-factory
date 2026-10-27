// 가운데 잡아 — 올리브 수관 2.5D 디오라마, 기록 정리 줄, 숫자 자 UI와 피드백 사건.
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
        static readonly Color Olive = MgfLook.Hex("#82963B");
        static readonly Color Chartreuse = MgfLook.Hex("#C8DE68");
        static readonly Color Cocoa = MgfLook.Hex("#3B3024");
        static readonly Color Fire = MgfLook.Hex("#F6D967");
        static readonly Color Ivory = MgfLook.Hex("#FFF4C7");
        static readonly Color Shade = MgfLook.Hex("#53656E");
        static readonly Color Moss = MgfLook.Hex("#45571F");

        Camera cam;
        Transform worldRoot, beetleRoot, netRoot, canopyRoot, titleNet, titleBeetle;
        readonly List<Transform> leaves = new List<Transform>();
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
        TextMeshProUGUI rawLabelTxt, sortLabelTxt, definitionTxt, revealTitleTxt, revealEvidenceTxt, toastTxt;
        TextMeshProUGUI endTitleTxt, endScoreTxt, endStatsTxt, endTapTxt;
        Image statusBg, goalBg, promptBg, definitionBg, handleImg, shadeImg, errorMarkImg, guideRing, guidePath, guideHand;
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
            MgfLook.Sky(MgfLook.Hex("#AFCB57"), Olive, MgfLook.Hex("#36431D"), .90f);
            MgfLook.Sun(new Vector3(48, -38, 16), MgfLook.Hex("#FFF1B5"), 1.32f, .60f);
            cam = MgfLook.Camera(new Vector3(11.8f, 11.4f, -18.6f), new Vector3(0, 1.4f, 2.4f), 35f);
            oliveMat = MgfLook.Lit(Olive, .28f, 0f);
            chartreuseMat = MgfLook.Lit(Chartreuse, .34f, 0f, Chartreuse * .035f);
            cocoaMat = MgfLook.Lit(Cocoa, .38f, .02f);
            fireMat = MgfLook.Lit(Fire, .58f, .02f, Fire * .42f);
            ivoryMat = MgfLook.Lit(Ivory, .20f, 0f);
            mossMat = MgfLook.Lit(Moss, .16f, 0f);
            glowMat = MgfLook.Additive(new Color(Fire.r, Fire.g, Fire.b, .90f), MgfLook.SoftDot);
            ropeMat = MgfLook.Lit(MgfLook.Hex("#7B5C31"), .14f, 0f);

            worldRoot = new GameObject("LivingOliveCanopy").transform;
            canopyRoot = new GameObject("LayeredLeaves").transform; canopyRoot.SetParent(worldRoot, false);
            for (int i = 0; i < 19; i++)
            {
                float x = -13.5f + (i * 4.17f % 27f), z = -1.2f + (i * 2.83f % 9.6f), y = -.9f + (i % 4) * .65f;
                float s = 1.5f + (i % 5) * .34f;
                var leaf = MgfLook.Prim(PrimitiveType.Sphere, "OliveLeaf" + i, new Vector3(x, y, z), new Vector3(s * 2.35f, .25f + s * .08f, s), i % 4 == 0 ? chartreuseMat : oliveMat, canopyRoot, false).transform;
                leaf.localRotation = Quaternion.Euler(-4f + i % 3 * 5f, i * 37f % 180f, -18f + i * 19f % 36f); leaves.Add(leaf);
                // 잎의 비균등 스케일을 상속하므로 로컬 길이는 1 안쪽으로 둔다. 중복 스케일은 화면을 가로지르는 선이 된다.
                var vein = MgfLook.Prim(PrimitiveType.Cube, "Vein" + i, new Vector3(0, .48f, 0), new Vector3(.86f, .035f, .045f), chartreuseMat, leaf, false).transform;
                vein.localRotation = Quaternion.Euler(0, 0, 0);
            }
            var branch = MgfLook.Prim(PrimitiveType.Cylinder, "NestBranch", new Vector3(0, .38f, 3.3f), new Vector3(.38f, 11.5f, .38f), ropeMat, worldRoot, false).transform;
            branch.localRotation = Quaternion.Euler(0, 0, 90f);
            for (int i = 0; i < nests.Length; i++)
            {
                float x = -7.2f + i * 2.85f; nests[i] = new GameObject("LeafNest" + i).transform; nests[i].SetParent(worldRoot, false); nests[i].localPosition = new Vector3(x, 1.12f + (i % 2) * .18f, 3.0f);
                for (int k = 0; k < 9; k++)
                {
                    float a = k / 9f * Mathf.PI * 2f;
                    MgfLook.Prim(PrimitiveType.Capsule, "NestFiber" + k, new Vector3(Mathf.Cos(a) * .58f, Mathf.Sin(a) * .30f, 0), new Vector3(.14f, .42f, .14f), ropeMat, nests[i], false).transform.localRotation = Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg);
                }
                nestGlows[i] = MgfLook.Prim(PrimitiveType.Sphere, "NestGlow", new Vector3(0, .08f, -.20f), new Vector3(.52f, .32f, .16f), glowMat, nests[i], false).transform;
                nestGlows[i].gameObject.SetActive(false);
            }
            beetleRoot = BuildBeetle(worldRoot, new Vector3(-.6f, .52f, .25f), 1.35f, false);
            titleBeetle = BuildBeetle(worldRoot, new Vector3(-5.6f, .25f, 1.15f), 1.0f, true);
            netRoot = BuildNet(worldRoot, new Vector3(1.8f, .45f, .45f), false);
            titleNet = BuildNet(worldRoot, new Vector3(4.1f, .25f, .92f), true);
            for (int i = 0; i < fireflies.Length; i++)
            {
                fireflies[i] = MgfLook.Prim(PrimitiveType.Sphere, "Firefly" + i, new Vector3(0, 0, 0), new Vector3(.17f, .17f, .17f), glowMat, worldRoot, false).transform;
                fireflies[i].localPosition = new Vector3(-7f + (i * 1.93f % 14f), 2.1f + (i % 5) * .35f, 1.2f + (i % 4) * .65f);
            }
        }

        Transform BuildBeetle(Transform parent, Vector3 pos, float scale, bool title)
        {
            var root = new GameObject(title ? "TitleLeafBeetle" : "LeafBeetle").transform; root.SetParent(parent, false); root.localPosition = pos; root.localScale = Vector3.one * scale;
            MgfLook.Prim(PrimitiveType.Sphere, "Shell", new Vector3(0, .85f, 0), new Vector3(2.0f, .95f, 1.55f), oliveMat, root, false);
            var seam = MgfLook.Prim(PrimitiveType.Cube, "ShellSeam", new Vector3(0, 1.12f, -.79f), new Vector3(.07f, .76f, .05f), chartreuseMat, root, false).transform; seam.localRotation = Quaternion.Euler(0, 0, 90);
            MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0, .74f, -1.06f), new Vector3(1.18f, .78f, .82f), cocoaMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeL", new Vector3(-.30f, .90f, -1.47f), new Vector3(.16f, .18f, .11f), ivoryMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeR", new Vector3(.30f, .90f, -1.47f), new Vector3(.16f, .18f, .11f), ivoryMat, root, false);
            for (int i = 0; i < 6; i++)
            {
                float side = i < 3 ? -1f : 1f, z = -.64f + (i % 3) * .62f;
                var leg = MgfLook.Prim(PrimitiveType.Capsule, "Leg" + i, new Vector3(side * .92f, .28f, z), new Vector3(.14f, .68f, .14f), cocoaMat, root, false).transform;
                leg.localRotation = Quaternion.Euler(0, 0, side * 58f);
            }
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var ant = MgfLook.Prim(PrimitiveType.Capsule, "Antenna" + i, new Vector3(side * .28f, 1.45f, -1.25f), new Vector3(.10f, .58f, .10f), cocoaMat, root, false).transform;
                ant.localRotation = Quaternion.Euler(side * 18f, 0, side * 22f); if (!title) antennae[i] = ant;
            }
            return root;
        }

        Transform BuildNet(Transform parent, Vector3 pos, bool title)
        {
            var root = new GameObject(title ? "TitleGatheringNet" : "GatheringNet").transform; root.SetParent(parent, false); root.localPosition = pos;
            for (int i = 0; i < 7; i++)
            {
                float x = -2.2f + i * .72f;
                var rope = MgfLook.Prim(PrimitiveType.Capsule, "NetFold" + i, new Vector3(x, .35f + Mathf.Abs(i - 3) * .10f, 0), new Vector3(.10f, 1.35f, .10f), ropeMat, root, false).transform;
                rope.localRotation = Quaternion.Euler(0, 0, -7f + i * 2.4f);
            }
            MgfLook.Block("NetHandle", new Vector3(0, -.54f, 0), new Vector3(5.2f, .34f, .36f), .12f, cocoaMat, root);
            for (int i = 0; i < 6; i++) MgfLook.Prim(PrimitiveType.Sphere, "Knot" + i, new Vector3(-1.8f + i * .72f, -.50f, -.24f), new Vector3(.26f, .26f, .26f), fireMat, root, false);
            return root;
        }

        void BuildUi()
        {
            roundedSprite = MakeSprite(96, 20, false); ringSprite = MakeSprite(96, 0, true);
            dotSprite = Sprite.Create(MgfLook.SoftDot, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 100);
            var canvas = MgfText.Canvas;
            titlePortrait = Resources.Load<Texture2D>("GaundeJaba/title");
            // 가로에서도 같은 로컬 키 아트를 cover-crop한다. 별도 사본을 넣지 않아 빌드 예산을 지킨다.
            titleLandscape = titlePortrait;

            titleRt = R("TitleScreen", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            var artRt = R("LocalTitleKeyArt", titleRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844));
            titleArt = artRt.gameObject.AddComponent<RawImage>(); titleArt.raycastTarget = false; titleArt.color = Color.white;
            titleLogo = Txt(titleRt, "가운데\n잡아", new Vector2(.73f, .77f), Vector2.zero, 55, Ivory, 250); titleLogo.fontStyle = FontStyles.Bold; titleLogo.outlineColor = Cocoa; titleLogo.outlineWidth = .25f; titleLogo.lineSpacing = -22;
            titleTag = Txt(titleRt, "그물로 가운데를 잡아라", new Vector2(.67f, .63f), Vector2.zero, 17, Fire, 300); titleTag.fontStyle = FontStyles.Bold; titleTag.outlineColor = Cocoa; titleTag.outlineWidth = .15f;
            titleMeta = Txt(titleRt, "중학교 1학년 · 자료의 정리와 해석", new Vector2(.67f, .575f), Vector2.zero, 12, Ivory, 320); titleMeta.outlineColor = Cocoa; titleMeta.outlineWidth = .13f;
            titleBest = Txt(titleRt, "빈 보금자리 6곳 · 첫 시도 5번", new Vector2(.5f, .20f), Vector2.zero, 14, Ivory, 350); titleBest.outlineColor = Cocoa; titleBest.outlineWidth = .14f;
            titleCtaTxt = Txt(titleRt, "빈 그물을 당겨 시작", new Vector2(.5f, .115f), Vector2.zero, 21, Fire, 340); titleCtaTxt.fontStyle = FontStyles.Bold; titleCtaTxt.outlineColor = Cocoa; titleCtaTxt.outlineWidth = .18f;

            hudRt = R("GameHud", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            statusBg = Img(hudRt, "CanopyStatus", new Vector2(.42f, .955f), Vector2.zero, new Vector2(320, 58), new Color(Ivory.r, Ivory.g, Ivory.b, .94f), roundedSprite);
            scoreTxt = Txt(statusBg.transform, "0", new Vector2(.10f, .54f), Vector2.zero, 20, Cocoa, 60); scoreTxt.fontStyle = FontStyles.Bold;
            roundTxt = Txt(statusBg.transform, "빛 0/6", new Vector2(.31f, .54f), Vector2.zero, 14, Cocoa, 90);
            firstTxt = Txt(statusBg.transform, "첫 0/0", new Vector2(.54f, .54f), Vector2.zero, 14, Cocoa, 86);
            timeTxt = Txt(statusBg.transform, "110초", new Vector2(.78f, .54f), Vector2.zero, 15, Shade, 64); timeTxt.fontStyle = FontStyles.Bold;
            for (int i = 0; i < 3; i++) lifeImgs[i] = Img(statusBg.transform, "NetKnot" + i, new Vector2(.72f + i * .08f, .18f), Vector2.zero, new Vector2(18, 9), Fire, roundedSprite);

            goalBg = Img(hudRt, "GoalLeaf", new Vector2(.5f, .88f), Vector2.zero, new Vector2(360, 42), Cocoa, roundedSprite);
            goalTxt = Txt(goalBg.transform, "중앙값에 그물을 놓으면 빛이 모인다", new Vector2(.5f, .52f), Vector2.zero, 15, Ivory, 344); goalTxt.fontStyle = FontStyles.Bold;
            promptBg = Img(hudRt, "ObservationPrompt", new Vector2(.5f, .805f), Vector2.zero, new Vector2(360, 82), new Color(Ivory.r, Ivory.g, Ivory.b, .95f), roundedSprite);
            promptTxt = Txt(promptBg.transform, "", new Vector2(.5f, .52f), Vector2.zero, 14, Cocoa, 338); promptTxt.fontStyle = FontStyles.Bold; promptTxt.enableAutoSizing = true; promptTxt.fontSizeMin = 10; promptTxt.fontSizeMax = 15; promptTxt.margin = new Vector4(8, 5, 8, 5);
            rawLabelTxt = Txt(hudRt, "원자료 · 발광 시간(초)", new Vector2(.5f, .72f), Vector2.zero, 12, Ivory, 350); rawLabelTxt.outlineColor = Cocoa; rawLabelTxt.outlineWidth = .12f;
            sortLabelTxt = Txt(hudRt, "정리 줄 · 끌어서 순서를 바꿀 수 있다", new Vector2(.5f, .55f), Vector2.zero, 12, Ivory, 360); sortLabelTxt.outlineColor = Cocoa; sortLabelTxt.outlineWidth = .12f;
            for (int i = 0; i < 8; i++)
            {
                rawCards[i] = Img(hudRt, "RawTag" + i, new Vector2(.5f, .66f), Vector2.zero, new Vector2(42, 48), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), roundedSprite);
                rawTexts[i] = Txt(rawCards[i].transform, "", new Vector2(.5f, .5f), Vector2.zero, 19, Cocoa, 42); rawTexts[i].fontStyle = FontStyles.Bold;
                sortCards[i] = Img(hudRt, "SortTag" + i, new Vector2(.5f, .48f), Vector2.zero, new Vector2(42, 48), new Color(Chartreuse.r, Chartreuse.g, Chartreuse.b, .96f), roundedSprite);
                sortTexts[i] = Txt(sortCards[i].transform, "", new Vector2(.5f, .5f), Vector2.zero, 19, Cocoa, 42); sortTexts[i].fontStyle = FontStyles.Bold;
            }

            definitionBg = Img(hudRt, "Definition", new Vector2(.5f, .355f), Vector2.zero, new Vector2(360, 48), new Color(Cocoa.r, Cocoa.g, Cocoa.b, .90f), roundedSprite);
            definitionTxt = Txt(definitionBg.transform, "중앙값: 크기순 가운데 값 · 짝수 개는 가운데 두 값의 평균", new Vector2(.5f, .5f), Vector2.zero, 12, Ivory, 344); definitionTxt.fontStyle = FontStyles.Bold;
            shadeImg = Img(hudRt, "RisingCanopyShade", new Vector2(.5f, .02f), Vector2.zero, new Vector2(390, 5), new Color(Shade.r, Shade.g, Shade.b, .80f), roundedSprite); shadeRt = shadeImg.rectTransform;
            for (int i = 0; i < ticks.Length; i++)
            {
                float u = i / 22f;
                ticks[i] = Img(hudRt, "RailTick" + i, new Vector2(.075f + .85f * u, .18f), Vector2.zero, new Vector2(i % 2 == 0 ? 4 : 2, i % 2 == 0 ? 25 : 14), Cocoa, roundedSprite);
                if (i % 2 == 0)
                {
                    int idx = i / 2; tickLabels[idx] = Txt(hudRt, (idx + 1).ToString(), new Vector2(.075f + .85f * u, .125f), Vector2.zero, 12, Ivory, 30); tickLabels[idx].outlineColor = Cocoa; tickLabels[idx].outlineWidth = .12f;
                }
            }
            Img(hudRt, "NumberRail", new Vector2(.5f, .18f), Vector2.zero, new Vector2(334, 6), Cocoa, roundedSprite).transform.SetAsFirstSibling();
            for (int i = 0; i < trail.Length; i++) trail[i] = Img(hudRt, "NetTrail" + i, new Vector2(.075f, .18f), Vector2.zero, new Vector2(26 - i * 2, 26 - i * 2), new Color(Fire.r, Fire.g, Fire.b, .14f - i * .012f), dotSprite);
            handleImg = Img(hudRt, "GatheringNetHandle", new Vector2(.075f, .18f), Vector2.zero, new Vector2(58, 58), Fire, dotSprite); handleRt = handleImg.rectTransform;
            railValueTxt = Txt(handleImg.transform, "1", new Vector2(.5f, .5f), new Vector2(0, 58), 24, Ivory, 96); railValueTxt.fontStyle = FontStyles.Bold; railValueTxt.outlineColor = Cocoa; railValueTxt.outlineWidth = .18f;
            errorMarkImg = Img(hudRt, "WrongExistingMark", new Vector2(.5f, .18f), Vector2.zero, new Vector2(28, 74), new Color(Shade.r, Shade.g, Shade.b, .85f), ringSprite); errorMarkRt = errorMarkImg.rectTransform;
            guidePath = Img(hudRt, "GhostDragPath", new Vector2(.5f, .255f), Vector2.zero, new Vector2(250, 8), new Color(Fire.r, Fire.g, Fire.b, .72f), roundedSprite);
            guideRing = Img(hudRt, "HandlePulse", new Vector2(.075f, .18f), Vector2.zero, new Vector2(94, 94), Fire, ringSprite);
            guideHand = Img(hudRt, "GhostHand", new Vector2(.075f, .255f), Vector2.zero, new Vector2(32, 54), new Color(Ivory.r, Ivory.g, Ivory.b, .92f), roundedSprite);

            revealRt = R("EvidenceReveal", canvas.transform, new Vector2(.5f, .31f), Vector2.zero, new Vector2(360, 142)); revealG = revealRt.gameObject.AddComponent<CanvasGroup>();
            revealBg = Img(revealRt, "EvidenceLeaf", new Vector2(.5f, .5f), Vector2.zero, new Vector2(360, 142), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), roundedSprite);
            revealTitleTxt = Txt(revealBg.transform, "", new Vector2(.5f, .74f), Vector2.zero, 20, Moss, 338); revealTitleTxt.fontStyle = FontStyles.Bold;
            revealEvidenceTxt = Txt(revealBg.transform, "", new Vector2(.5f, .38f), Vector2.zero, 14, Cocoa, 338); revealEvidenceTxt.fontStyle = FontStyles.Bold; revealEvidenceTxt.enableAutoSizing = true; revealEvidenceTxt.fontSizeMin = 10; revealEvidenceTxt.fontSizeMax = 15;

            toastG = R("LeafToast", canvas.transform, new Vector2(.5f, .40f), Vector2.zero, new Vector2(350, 48)).gameObject.AddComponent<CanvasGroup>();
            var toastBg = Img(toastG.transform, "ToastBg", new Vector2(.5f, .5f), Vector2.zero, new Vector2(350, 48), Cocoa, roundedSprite);
            toastTxt = Txt(toastBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 13, Ivory, 330);
            rippleImg = Img(canvas.transform, "LeafVeinRipple", new Vector2(.5f, .5f), Vector2.zero, new Vector2(68, 68), Chartreuse, ringSprite); rippleRt = rippleImg.rectTransform; rippleRt.gameObject.SetActive(false);

            endRt = R("EndScreen", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); endG = endRt.gameObject.AddComponent<CanvasGroup>();
            endBg = Img(endRt, "ResultLeaf", new Vector2(.5f, .54f), Vector2.zero, new Vector2(350, 402), new Color(Ivory.r, Ivory.g, Ivory.b, .97f), roundedSprite);
            endTitleTxt = Txt(endBg.transform, "", new Vector2(.5f, .80f), Vector2.zero, 30, Moss, 326); endTitleTxt.fontStyle = FontStyles.Bold;
            endScoreTxt = Txt(endBg.transform, "0", new Vector2(.5f, .61f), Vector2.zero, 58, Cocoa, 320); endScoreTxt.fontStyle = FontStyles.Bold;
            endStatsTxt = Txt(endBg.transform, "", new Vector2(.5f, .39f), Vector2.zero, 17, Cocoa, 318); endStatsTxt.fontStyle = FontStyles.Bold;
            var restart = Img(endBg.transform, "RestartNet", new Vector2(.5f, .12f), Vector2.zero, new Vector2(300, 68), Fire, roundedSprite);
            endTapTxt = Txt(restart.transform, "빈 그물 다시 당기기", new Vector2(.5f, .5f), Vector2.zero, 21, Cocoa, 282); endTapTxt.fontStyle = FontStyles.Bold;
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
                cam.transform.position = new Vector3(12.8f, 10.1f, -19.5f); cam.transform.LookAt(new Vector3(0, 1.2f, 2.5f));
                titleLogo.rectTransform.anchorMin = titleLogo.rectTransform.anchorMax = new Vector2(.78f, .74f);
                titleTag.rectTransform.anchorMin = titleTag.rectTransform.anchorMax = new Vector2(.78f, .55f);
                titleMeta.rectTransform.anchorMin = titleMeta.rectTransform.anchorMax = new Vector2(.78f, .48f);
                statusBg.rectTransform.anchorMin = statusBg.rectTransform.anchorMax = new Vector2(.50f, .945f);
                statusBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(520, logical.x * .48f), 58);
                goalBg.rectTransform.anchorMin = goalBg.rectTransform.anchorMax = new Vector2(.50f, .86f);
                promptBg.rectTransform.anchorMin = promptBg.rectTransform.anchorMax = new Vector2(.50f, .765f);
                promptBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(680, logical.x * .64f), 78);
                rawLabelTxt.rectTransform.anchorMin = rawLabelTxt.rectTransform.anchorMax = new Vector2(.50f, .69f);
                sortLabelTxt.rectTransform.anchorMin = sortLabelTxt.rectTransform.anchorMax = new Vector2(.50f, .48f);
                definitionBg.rectTransform.anchorMin = definitionBg.rectTransform.anchorMax = new Vector2(.50f, .34f);
                revealRt.anchorMin = revealRt.anchorMax = new Vector2(.50f, .39f);
            }
            else
            {
                cam.transform.position = new Vector3(11.8f, 11.4f, -18.6f); cam.transform.LookAt(new Vector3(0, 1.4f, 2.4f));
                titleLogo.rectTransform.anchorMin = titleLogo.rectTransform.anchorMax = new Vector2(.62f, .80f);
                titleTag.rectTransform.anchorMin = titleTag.rectTransform.anchorMax = new Vector2(.60f, .67f);
                titleMeta.rectTransform.anchorMin = titleMeta.rectTransform.anchorMax = new Vector2(.59f, .61f);
                statusBg.rectTransform.anchorMin = statusBg.rectTransform.anchorMax = new Vector2(.42f, .955f);
                statusBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(320, logical.x - 68), 58);
                goalBg.rectTransform.anchorMin = goalBg.rectTransform.anchorMax = new Vector2(.50f, .88f);
                promptBg.rectTransform.anchorMin = promptBg.rectTransform.anchorMax = new Vector2(.50f, .805f);
                promptBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(366, logical.x * .95f), 82);
                rawLabelTxt.rectTransform.anchorMin = rawLabelTxt.rectTransform.anchorMax = new Vector2(.50f, .72f);
                sortLabelTxt.rectTransform.anchorMin = sortLabelTxt.rectTransform.anchorMax = new Vector2(.50f, .55f);
                definitionBg.rectTransform.anchorMin = definitionBg.rectTransform.anchorMax = new Vector2(.50f, .355f);
                revealRt.anchorMin = revealRt.anchorMax = new Vector2(.50f, .31f);
            }
            Texture2D tex = land ? titleLandscape : titlePortrait;
            titleArt.texture = tex; titleArt.enabled = tex != null;
            titleArt.rectTransform.sizeDelta = logical; titleArt.uvRect = CoverUv(tex, logical.x / Mathf.Max(1, logical.y));
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
                    SetAnchor(rawCards[i].rectTransform, new Vector2(.15f + .70f * u, .62f));
                    SetAnchor(sortCards[i].rectTransform, new Vector2(.15f + .70f * u, .42f));
                    rawCards[i].rectTransform.sizeDelta = sortCards[i].rectTransform.sizeDelta = new Vector2(54, 54);
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
            titleBest.text = PlayerPrefs.GetInt("gaunde-jaba-best-first", 0) > 0 ? "최고 첫 시도 " + PlayerPrefs.GetInt("gaunde-jaba-best-first", 0) + "/6" : "빈 보금자리 6곳 · 첫 시도 5번";
            if (!title && !end) UpdateHudText();
        }

        void BeginTitleFold() { titleFold = .0001f; MgfFx.Punch(titleNet, .20f, .30f); }

        void LoadProblemVisual(MedianProblem p, bool practice)
        {
            promptTxt.text = p.prompt;
            goalTxt.text = practice ? (p.id == "practice-odd" ? "9, 2, 5를 크기순으로 보고 5초에 그물을 놓으시오" : "2, 4, 6, 8의 가운데 두 값을 평균 내어 놓으시오") : "중앙값에 그물을 놓으면 빛이 모인다";
            rawLabelTxt.text = "원자료 · 발광 시간(초) · " + p.raw.Length + "개";
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
            roundTxt.text = IsPractice ? "연습" : "빛 " + st.solved + "/6";
            firstTxt.text = "첫 " + st.firstCorrect + "/" + st.attemptedRounds;
            for (int i = 0; i < lifeImgs.Length; i++) lifeImgs[i].color = i < st.lives ? Fire : new Color(Shade.r, Shade.g, Shade.b, .28f);
            definitionTxt.text = st.level >= 2 ? "중앙값: 크기순 가운데 값 · 짝수 개는 가운데 두 값의 평균" : "중앙값: 자료를 크기순으로 놓았을 때 가운데 값";
        }

        void BoostGuide(bool strong) { guideBoost = strong ? 1.8f : 1f; toastClock = 1.5f; toastTxt.text = strong ? "빛나는 손잡이를 끌어 목표 눈금에서 놓으시오" : "아래 그물 손잡이를 좌우로 끌어 놓으시오"; }

        void BeginRevealVisual(bool correct, bool practice, int value2, string misconception)
        {
            feedbackClock = 0f; feedbackGood = correct; hitStop = correct ? .075f : 0f; wrongKick = correct ? 0f : 1f; guideBoost = 0f;
            revealTitleTxt.text = correct ? (practice ? "가운데를 잡았다" : "매듭이 잠겼다") : "매듭 하나가 풀렸다";
            revealTitleTxt.color = correct ? Moss : Shade;
            revealEvidenceTxt.text = correct ? MedianRules.Evidence(current) : MedianRules.MisconceptionKorean(misconception) + "\n" + MedianRules.Evidence(current);
            if (correct)
            {
                MgfSfx.Play("correct", .46f); MgfFx.Punch(netRoot, .16f, .30f); MgfFx.Glow(new Vector3(1.8f, 1.5f, .2f), Fire, 9, .32f);
                if (!practice && st.solved > 0 && st.solved <= nests.Length) { nestGlows[st.solved - 1].gameObject.SetActive(true); MgfFx.Glow(nests[st.solved - 1].position, Fire, 8, .28f); }
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
            endTitleTxt.text = clear ? "수관이 환해졌다" : reason == "knots" ? "매듭이 모두 풀렸다" : reason == "time" ? "그늘이 보금자리에 닿았다" : "첫 시도 5번이 필요하다";
            endScoreTxt.text = "0"; shownEndScore = 0f;
            endStatsTxt.text = "빛 " + st.solved + "/6\n첫 시도 " + st.firstCorrect + "/" + st.attemptedRounds + "\n남은 매듭 " + st.lives;
            endTapTxt.text = "빈 그물 다시 당기기";
            if (clear) for (int i = 0; i < nests.Length; i++) { nestGlows[i].gameObject.SetActive(true); MgfFx.Glow(nests[i].position, Fire, 6, .26f); }
        }

        void TickView(float dt)
        {
            clock += dt;
            if (titleFold > 0f)
            {
                titleFold += dt; float k = Mathf.Clamp01(titleFold / .40f); titleG.alpha = 1f - k; if (titleNet) titleNet.localScale = Vector3.one * (1f + .28f * Mathf.Sin(k * Mathf.PI));
            }
            for (int i = 0; i < leaves.Count; i++) leaves[i].localRotation *= Quaternion.Euler(0, Mathf.Sin(clock * .33f + i) * .012f, Mathf.Sin(clock * .52f + i * .7f) * .018f);
            if (beetleRoot) beetleRoot.localPosition = new Vector3(-.6f, .52f + Mathf.Sin(clock * 2.1f) * .055f, .25f);
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
                SetAnchor(guideRing.rectTransform, new Vector2(RailXFor(st.netValue2), RailYFraction())); guideRing.rectTransform.localScale = Vector3.one * pulse;
                float from = RailXFor(st.netValue2), to = current == null ? .5f : RailXFor(current.answer2);
                float travel = (clock * .55f) % 1f; SetAnchor(guideHand.rectTransform, new Vector2(Mathf.Lerp(from, to, travel), RailYFraction() + .075f));
                SetAnchor(guidePath.rectTransform, new Vector2((from + to) * .5f, RailYFraction() + .075f)); guidePath.rectTransform.sizeDelta = new Vector2(Mathf.Abs(to - from) * logicalSize.x, 7);
            }
            else { guideRing.gameObject.SetActive(false); guidePath.gameObject.SetActive(false); guideHand.gameObject.SetActive(false); }
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
