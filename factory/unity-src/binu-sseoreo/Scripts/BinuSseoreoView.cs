// 비누 썰어 — 거대 비누 내부 협곡 2.5D 월드와 화면 UI.
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.BinuSseoreo
{
    public partial class BinuSseoreoGame
    {
        static readonly Color Grape = MgfLook.Hex("#6F4A8E");
        static readonly Color GrapeDeep = MgfLook.Hex("#332044");
        static readonly Color Ivory = MgfLook.Hex("#F2E9D8");
        static readonly Color Ink = MgfLook.Hex("#171923");
        static readonly Color Saffron = MgfLook.Hex("#F2B134");
        static readonly Color Coral = MgfLook.Hex("#D95D6A");
        static readonly Color Mint = MgfLook.Hex("#86D7B3");

        Camera cam;
        Transform titleRoot, gameRoot, soapRoot, soapLower, soapUpper, bridgeRoot, bubbleRoot, creatureRoot;
        Transform titleHandle, cutHandle, cutGlow, ghostHand, guideArrow;
        readonly Transform[] ribs = new Transform[8];
        readonly Transform[] sockets = new Transform[8];
        readonly Transform[] formings = new Transform[8];
        readonly Transform[] lampGlows = new Transform[8];
        readonly List<Transform> ambientBubbles = new List<Transform>();
        LineRenderer wireLine, trailLine, ghostPath;
        Material grapeMat, grapeDeepMat, ivoryMat, inkMat, saffronMat, coralMat, mintMat, glowMat;
        float clock, guideBoost, rippleClock, toastClock, revealAnim, hitStop, wrongKick, titleSplit;
        float shownScore, shownEndScore;
        int shownSecond = -1;
        int lastW = -1, lastH = -1;
        float lastCanvasScale = -1f;
        bool land;

        CanvasGroup titleG, hudG, revealG, endG, toastG;
        RectTransform titleRt, hudRt, revealRt, endRt, rippleRt, knobRt, cutLineRt, exitRt, rulerRt;
        Image titleCta, statusBg, goalBg, promptBg, formulaBg, revealBg, endBg, rippleImg, knobImg, cutLineImg, exitImg;
        readonly Image[] lifeImgs = new Image[3];
        readonly Image[] tickImgs = new Image[26];
        TextMeshProUGUI titleShadow, titleLogo, titleTag, titleMeta, titleBest, titleCtaTxt;
        TextMeshProUGUI scoreTxt, progressTxt, firstTxt, timeTxt, goalTxt, promptTxt, formulaTxt, heightTxt, exitTxt;
        TextMeshProUGUI revealTitleTxt, revealFormulaTxt, toastTxt, endTitleTxt, endScoreTxt, endStatsTxt, endTapTxt;
        Sprite roundSprite, ringSprite;

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
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 2.7f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color;
            t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text;
            return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "BinuRing" : "BinuRound", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                if (ring)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - size * .40f) / (size * .06f));
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
            MgfLook.Sky(MgfLook.Hex("#38224C"), Grape, MgfLook.Hex("#24162F"), .82f);
            MgfLook.Sun(new Vector3(44, -32, 18), MgfLook.Hex("#FFE3A7"), 1.34f, .68f);
            cam = MgfLook.Camera(new Vector3(11.2f, 10.8f, -16.8f), new Vector3(0, 2.4f, 1.8f), 35f);
            grapeMat = MgfLook.Lit(Grape, .18f, .01f);
            grapeDeepMat = MgfLook.Lit(GrapeDeep, .12f, .01f);
            ivoryMat = MgfLook.Lit(Ivory, .30f, .01f);
            inkMat = MgfLook.Lit(Ink, .72f, .28f);
            saffronMat = MgfLook.Lit(Saffron, .54f, .08f, Saffron * .08f);
            coralMat = MgfLook.Lit(Coral, .34f, .02f);
            mintMat = MgfLook.Lit(Mint, .35f, .02f, Mint * .05f);
            glowMat = MgfLook.Additive(new Color(1f, .70f, .22f, .76f), MgfLook.SoftDot);

            titleRoot = new GameObject("SoapCanyonTitle").transform;
            gameRoot = new GameObject("SoapSliceYard").transform;
            BuildCanyon(titleRoot, true);
            BuildCanyon(gameRoot, false);
            BuildTitleWorld();
            BuildGameWorld();
        }

        void BuildCanyon(Transform root, bool title)
        {
            MgfLook.Block("BackStratum", new Vector3(0, 3.4f, 6.8f), new Vector3(30f, 9.8f, 1.7f), .55f, grapeMat, root);
            MgfLook.Block("LeftCliff", new Vector3(-11.5f, 1.0f, 1.8f), new Vector3(5.0f, 5.4f, 8.4f), .65f, grapeMat, root);
            MgfLook.Block("RightCliff", new Vector3(11.5f, .7f, 2.5f), new Vector3(5.0f, 5.0f, 7.6f), .65f, grapeMat, root);
            MgfLook.Block("LowerShelf", new Vector3(0, -1.1f, 2.2f), new Vector3(30f, 1.8f, 10.4f), .55f, grapeMat, root);
            int holes = title ? 18 : 14;
            for (int i = 0; i < holes; i++)
            {
                float x = -7.2f + (i * 3.37f % 14.4f);
                float y = -.15f + ((i * 1.73f) % 6.7f);
                float z = 5.85f - (i % 3) * .14f;
                float s = .42f + (i % 5) * .17f;
                var b = MgfLook.Prim(PrimitiveType.Sphere, "AirPocket" + i, new Vector3(x, y, z), new Vector3(s * 1.45f, s, .18f), grapeDeepMat, root, false).transform;
                ambientBubbles.Add(b);
            }
            for (int i = 0; i < 6; i++)
            {
                var lamp = MgfLook.Prim(PrimitiveType.Sphere, "CanyonLamp" + i,
                    new Vector3(-6.7f + i * 2.65f, -.05f + (i % 2) * 1.05f, 5.2f), new Vector3(.22f, .22f, .12f), glowMat, root, false).transform;
                ambientBubbles.Add(lamp);
            }
        }

        void BuildTitleWorld()
        {
            MgfLook.Block("CarvedTitleShelf", new Vector3(0, 2.5f, 4.5f), new Vector3(10.6f, 2.7f, 1.0f), .42f, grapeDeepMat, titleRoot);
            titleHandle = MgfLook.Block("SaffronPullHandle", new Vector3(0, -.05f, -.2f), new Vector3(4.5f, .82f, 1.0f), .32f, saffronMat, titleRoot).transform;
            MgfLook.Block("HandleGrip", new Vector3(0, .18f, -.78f), new Vector3(2.8f, .34f, .42f), .16f, inkMat, titleHandle);
            for (int i = 0; i < 5; i++) BuildForming(titleRoot, new Vector3(-5.5f + i * 2.75f, -.18f + (i % 2) * .22f, 1.7f + (i % 3) * .8f), .72f, i * 19f, i);
            for (int i = 0; i < 6; i++)
                MgfLook.Block("EmptySocket" + i, new Vector3(-5.0f + i * 2f, -.25f, 3.2f), new Vector3(.62f, .55f, 1.2f), .18f, inkMat, titleRoot);
        }

        void BuildGameWorld()
        {
            soapRoot = new GameObject("WorkingSoap").transform; soapRoot.SetParent(gameRoot, false); soapRoot.localPosition = new Vector3(-2.7f, -.25f, .8f);
            soapLower = new GameObject("LowerPiece").transform; soapLower.SetParent(soapRoot, false);
            soapUpper = new GameObject("UpperPiece").transform; soapUpper.SetParent(soapRoot, false);
            cutGlow = MgfLook.Prim(PrimitiveType.Cube, "DryCutGlow", new Vector3(0, 2.3f, -.72f), new Vector3(3.2f, .07f, .18f), MgfLook.Unlit(Coral), soapRoot, false).transform;
            cutGlow.gameObject.SetActive(false);

            wireLine = MakeLine("SaffronWire", gameRoot, Saffron, .105f);
            trailLine = MakeLine("WireTrail", gameRoot, new Color(Saffron.r, Saffron.g, Saffron.b, .40f), .22f);
            wireLine.enabled = trailLine.enabled = false;

            ghostPath = MakeLine("OnboardingPath", gameRoot, Saffron, .16f); ghostPath.positionCount = 18; ghostPath.enabled = false;
            ghostHand = MgfLook.Prim(PrimitiveType.Sphere, "GhostHand", new Vector3(-5.2f, 1f, -.2f), new Vector3(.48f, .48f, .20f), glowMat, gameRoot, false).transform;
            ghostHand.gameObject.SetActive(false);
            guideArrow = MgfLook.Block("GuideArrow", new Vector3(-4.8f, 1f, -.2f), new Vector3(.85f, .18f, .18f), .08f, saffronMat, gameRoot).transform;
            guideArrow.gameObject.SetActive(false);

            bridgeRoot = new GameObject("RibBridge").transform; bridgeRoot.SetParent(gameRoot, false);
            bridgeRoot.localPosition = new Vector3(3.7f, -.18f, 2.1f);
            for (int i = 0; i < ribs.Length; i++)
            {
                float x = -3.25f + i * .92f;
                sockets[i] = MgfLook.Block("BlackSocket" + i, new Vector3(x, .05f, 0), new Vector3(.72f, .48f, 1.45f), .17f, inkMat, bridgeRoot).transform;
                ribs[i] = MgfLook.Block("SoapRib" + i, new Vector3(x, .9f, 0), new Vector3(.52f, 2.15f, 1.08f), .13f, ivoryMat, bridgeRoot).transform;
                ribs[i].gameObject.SetActive(false);
                lampGlows[i] = MgfLook.Prim(PrimitiveType.Sphere, "WorkLampGlow" + i, new Vector3(x, 2.18f, .55f), new Vector3(.22f, .22f, .12f), glowMat, bridgeRoot, false).transform;
                lampGlows[i].gameObject.SetActive(false);
            }
            creatureRoot = new GameObject("FormingWorkers").transform; creatureRoot.SetParent(gameRoot, false);
            for (int i = 0; i < formings.Length; i++)
                formings[i] = BuildForming(creatureRoot, new Vector3(.7f + (i % 4) * 1.5f, -.05f, 3.7f + (i / 4) * 1.0f), .42f, 180f, i);

            // 정면 읽기용 자: 5 cm마다 굵은 눈금과 숫자. 실제 판정은 같은 0~25 투영을 쓴다.
            for (int i = 0; i <= 25; i++)
            {
                float y = -.05f + i * .205f;
                float w = i % 5 == 0 ? .72f : .42f;
                MgfLook.Block("RulerTick" + i, new Vector3(-5.0f + w * .5f, y, -.15f), new Vector3(w, i % 5 == 0 ? .055f : .034f, .10f), .015f,
                    i % 5 == 0 ? coralMat : inkMat, gameRoot);
                if (i % 5 == 0)
                {
                    var tx = MgfText.World(i.ToString(), new Vector3(-5.25f, y, -.12f), .36f, Ivory, gameRoot);
                    tx.alignment = TextAlignmentOptions.Right; tx.transform.rotation = Quaternion.Euler(24f, 0, 0);
                }
            }
            cutHandle = MgfLook.Block("CutHandle", new Vector3(-4.35f, 2.4f, -.45f), new Vector3(.85f, .62f, .52f), .22f, saffronMat, gameRoot).transform;
        }

        Transform BuildForming(Transform parent, Vector3 pos, float scale, float yaw, int variant)
        {
            var root = new GameObject("OriginalForming" + variant).transform; root.SetParent(parent, false); root.localPosition = pos; root.localScale = Vector3.one * scale; root.localRotation = Quaternion.Euler(0, yaw, 0);
            MgfLook.Prim(PrimitiveType.Sphere, "Body", new Vector3(0, .74f, 0), new Vector3(1.15f, .88f, .92f), ivoryMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeL", new Vector3(-.27f, .86f, -.45f), new Vector3(.13f, .17f, .09f), inkMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeR", new Vector3(.27f, .86f, -.45f), new Vector3(.13f, .17f, .09f), inkMat, root, false);
            for (int i = 0; i < 6; i++)
            {
                float x = (i % 3 - 1) * .42f, z = i < 3 ? -.30f : .30f;
                MgfLook.Prim(PrimitiveType.Sphere, "Foot" + i, new Vector3(x, .15f, z), new Vector3(.22f, .34f, .22f), i % 2 == 0 ? ivoryMat : coralMat, root, false);
            }
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -.28f : .28f;
                var ant = MgfLook.Prim(PrimitiveType.Sphere, "Antenna" + i, new Vector3(x, 1.48f, -.03f), new Vector3(.16f, .55f, .16f), ivoryMat, root, false).transform;
                ant.localRotation = Quaternion.Euler(0, 0, i == 0 ? -16f : 16f);
                MgfLook.Prim(PrimitiveType.Sphere, "Tip" + i, new Vector3(x * 1.35f, 1.82f, -.03f), new Vector3(.26f, .26f, .26f), variant % 2 == 0 ? grapeMat : saffronMat, root, false);
            }
            MgfLook.Block("ToolBelt", new Vector3(0, .58f, .43f), new Vector3(1.05f, .20f, .15f), .06f, inkMat, root);
            return root;
        }

        LineRenderer MakeLine(string name, Transform parent, Color color, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(parent, false);
            line.useWorldSpace = true; line.positionCount = 2; line.widthMultiplier = width; line.numCapVertices = 4;
            line.sharedMaterial = MgfLook.Unlit(color); line.shadowCastingMode = ShadowCastingMode.Off; return line;
        }

        void BuildUi()
        {
            roundSprite = MakeSprite(96, 20, false); ringSprite = MakeSprite(96, 0, true);
            var canvas = MgfText.Canvas;

            titleRt = R("TitleScreen", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            titleShadow = Txt(titleRt, "비누 썰어", new Vector2(.5f, .78f), new Vector2(4, -8), 61, Ink, 360); titleShadow.fontStyle = FontStyles.Bold;
            titleLogo = Txt(titleRt, "비누 썰어", new Vector2(.5f, .78f), Vector2.zero, 61, Ivory, 360); titleLogo.fontStyle = FontStyles.Bold; titleLogo.outlineColor = Coral; titleLogo.outlineWidth = .22f;
            titleTag = Txt(titleRt, "비누를 잘라 다리를 세워라", new Vector2(.5f, .69f), Vector2.zero, 18, Saffron, 350); titleTag.fontStyle = FontStyles.Bold;
            titleMeta = Txt(titleRt, "중학교 1학년 · 입체도형의 성질", new Vector2(.5f, .635f), Vector2.zero, 13, Ivory, 350);
            titleBest = Txt(titleRt, "리브 8개 · 첫 칼 6개", new Vector2(.5f, .23f), Vector2.zero, 14, Ivory, 330);
            titleCta = Img(titleRt, "WorldHandleCTA", new Vector2(.5f, .135f), Vector2.zero, new Vector2(328, 72), Saffron, roundSprite);
            Img(titleCta.transform, "HandleCut", new Vector2(.5f, .18f), Vector2.zero, new Vector2(250, 7), Ink, roundSprite);
            titleCtaTxt = Txt(titleCta.transform, "손잡이를 당겨 시작", new Vector2(.5f, .60f), Vector2.zero, 22, Ink, 310); titleCtaTxt.fontStyle = FontStyles.Bold;

            hudRt = R("GameHud", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            statusBg = Img(hudRt, "Status", new Vector2(.44f, .955f), Vector2.zero, new Vector2(335, 64), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), roundSprite);
            scoreTxt = Txt(statusBg.transform, "0", new Vector2(.10f, .56f), Vector2.zero, 22, Ink, 66); scoreTxt.fontStyle = FontStyles.Bold;
            progressTxt = Txt(statusBg.transform, "리브 0/8", new Vector2(.34f, .56f), Vector2.zero, 15, Ink, 108);
            firstTxt = Txt(statusBg.transform, "첫 칼 0/0", new Vector2(.62f, .56f), Vector2.zero, 14, Ink, 110);
            timeTxt = Txt(statusBg.transform, "120초", new Vector2(.88f, .56f), Vector2.zero, 16, Coral, 72); timeTxt.fontStyle = FontStyles.Bold;
            for (int i = 0; i < lifeImgs.Length; i++) lifeImgs[i] = Img(statusBg.transform, "SpareSoap" + i, new Vector2(.74f + i * .07f, .18f), Vector2.zero, new Vector2(19, 10), Ivory, roundSprite);

            goalBg = Img(hudRt, "Goal", new Vector2(.5f, .875f), Vector2.zero, new Vector2(360, 42), Ink, roundSprite);
            goalTxt = Txt(goalBg.transform, "필요한 부피로 잘라 리브를 세우시오", new Vector2(.5f, .52f), Vector2.zero, 15, Ivory, 344); goalTxt.fontStyle = FontStyles.Bold;
            promptBg = Img(hudRt, "OrderPlate", new Vector2(.5f, .79f), Vector2.zero, new Vector2(362, 94), new Color(Ivory.r, Ivory.g, Ivory.b, .97f), roundSprite);
            promptTxt = Txt(promptBg.transform, "", new Vector2(.5f, .52f), Vector2.zero, 15, Ink, 340); promptTxt.fontStyle = FontStyles.Bold;

            formulaBg = Img(hudRt, "Formula", new Vector2(.5f, .105f), Vector2.zero, new Vector2(350, 72), new Color(Ink.r, Ink.g, Ink.b, .94f), roundSprite);
            formulaTxt = Txt(formulaBg.transform, "V = 밑넓이 × 높이  ·  전체 높이 25 cm", new Vector2(.5f, .50f), Vector2.zero, 15, Ivory, 330); formulaTxt.fontStyle = FontStyles.Bold;

            rulerRt = R("RulerOverlay", hudRt, new Vector2(.18f, .49f), Vector2.zero, new Vector2(64, 430));
            Img(rulerRt, "RulerSpine", new Vector2(.34f, .5f), Vector2.zero, new Vector2(5, 418), Ink, roundSprite);
            for (int i = 0; i <= 25; i++)
            {
                float u = i / 25f;
                tickImgs[i] = Img(rulerRt, "Tick" + i, new Vector2(.45f, .03f + .94f * u), Vector2.zero,
                    new Vector2(i % 5 == 0 ? 31 : 18, i % 5 == 0 ? 3 : 2), i % 5 == 0 ? Coral : Ink, roundSprite);
                if (i % 5 == 0) Txt(rulerRt, i.ToString(), new Vector2(.86f, .03f + .94f * u), Vector2.zero, 11, Ivory, 34, TextAlignmentOptions.Left);
            }
            knobImg = Img(hudRt, "CutKnob", new Vector2(.18f, .5f), Vector2.zero, new Vector2(54, 54), Saffron, roundSprite); knobRt = knobImg.rectTransform;
            heightTxt = Txt(knobImg.transform, "13 cm", new Vector2(.5f, .5f), Vector2.zero, 14, Ink, 54); heightTxt.fontStyle = FontStyles.Bold;
            cutLineImg = Img(hudRt, "CutPreview", new Vector2(.5f, .5f), Vector2.zero, new Vector2(260, 5), Coral, roundSprite); cutLineRt = cutLineImg.rectTransform;
            exitImg = Img(hudRt, "ExitStrip", new Vector2(.87f, .49f), Vector2.zero, new Vector2(48, 430), new Color(Saffron.r, Saffron.g, Saffron.b, .16f), ringSprite); exitRt = exitImg.rectTransform;
            exitTxt = Txt(exitImg.transform, "놓기", new Vector2(.5f, .50f), Vector2.zero, 13, Saffron, 42); exitTxt.fontStyle = FontStyles.Bold;

            revealRt = R("Reveal", canvas.transform, new Vector2(.5f, .28f), Vector2.zero, new Vector2(354, 154)); revealG = revealRt.gameObject.AddComponent<CanvasGroup>();
            revealBg = Img(revealRt, "Evidence", new Vector2(.5f, .5f), Vector2.zero, new Vector2(354, 154), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), roundSprite);
            revealTitleTxt = Txt(revealBg.transform, "", new Vector2(.5f, .73f), Vector2.zero, 21, GrapeDeep, 330); revealTitleTxt.fontStyle = FontStyles.Bold;
            revealFormulaTxt = Txt(revealBg.transform, "", new Vector2(.5f, .37f), Vector2.zero, 15, Ink, 330);

            toastG = R("Toast", canvas.transform, new Vector2(.5f, .36f), Vector2.zero, new Vector2(340, 48)).gameObject.AddComponent<CanvasGroup>();
            var toastBg = Img(toastG.transform, "ToastBg", new Vector2(.5f, .5f), Vector2.zero, new Vector2(340, 48), Ink, roundSprite);
            toastTxt = Txt(toastBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 14, Ivory, 322);
            rippleImg = Img(canvas.transform, "SoapPowderRipple", new Vector2(.5f, .5f), Vector2.zero, new Vector2(64, 64), Ivory, ringSprite); rippleRt = rippleImg.rectTransform; rippleRt.gameObject.SetActive(false);

            endRt = R("EndScreen", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); endG = endRt.gameObject.AddComponent<CanvasGroup>();
            endBg = Img(endRt, "EndPlaque", new Vector2(.5f, .52f), Vector2.zero, new Vector2(350, 410), new Color(Ivory.r, Ivory.g, Ivory.b, .97f), roundSprite);
            endTitleTxt = Txt(endBg.transform, "", new Vector2(.5f, .80f), Vector2.zero, 31, GrapeDeep, 330); endTitleTxt.fontStyle = FontStyles.Bold;
            endScoreTxt = Txt(endBg.transform, "0", new Vector2(.5f, .61f), Vector2.zero, 60, Coral, 320); endScoreTxt.fontStyle = FontStyles.Bold;
            endStatsTxt = Txt(endBg.transform, "", new Vector2(.5f, .38f), Vector2.zero, 18, Ink, 314);
            var restart = Img(endBg.transform, "RestartHandle", new Vector2(.5f, .12f), Vector2.zero, new Vector2(300, 68), Saffron, roundSprite);
            endTapTxt = Txt(restart.transform, "새 비누 자르기", new Vector2(.5f, .5f), Vector2.zero, 22, Ink, 280); endTapTxt.fontStyle = FontStyles.Bold;
        }

        void Relayout(bool force)
        {
            float canvasScale = Mathf.Max(.01f, MgfText.Canvas.scaleFactor);
            if (!force && lastW == Screen.width && lastH == Screen.height && Mathf.Abs(lastCanvasScale - canvasScale) < .001f) return;
            lastW = Screen.width; lastH = Screen.height; lastCanvasScale = canvasScale; land = MgfBridge.IsLandLayout;
            Vector2 logical = new Vector2(Screen.width / canvasScale, Screen.height / canvasScale);
            if (logical.x < 100f || logical.y < 100f) logical = new Vector2(390, 844);
            titleRt.sizeDelta = logical; hudRt.sizeDelta = logical; endRt.sizeDelta = logical;
            MgfLook.FitWidth(cam, land ? 34f : 36f, .72f);
            if (land)
            {
                cam.transform.position = new Vector3(12.8f, 9.8f, -17.2f); cam.transform.LookAt(new Vector3(0, 2.0f, 2.0f));
                promptBg.rectTransform.anchorMin = promptBg.rectTransform.anchorMax = new Vector2(.35f, .58f);
                promptBg.rectTransform.sizeDelta = new Vector2(350, 94);
                goalBg.rectTransform.anchorMin = goalBg.rectTransform.anchorMax = new Vector2(.35f, .78f);
                formulaBg.rectTransform.anchorMin = formulaBg.rectTransform.anchorMax = new Vector2(.67f, .12f);
                rulerRt.anchorMin = rulerRt.anchorMax = new Vector2(.13f, .45f);
                exitRt.anchorMin = exitRt.anchorMax = new Vector2(.58f, .45f);
                statusBg.rectTransform.anchorMin = statusBg.rectTransform.anchorMax = new Vector2(.50f, .94f);
                statusBg.rectTransform.sizeDelta = new Vector2(520, 64);
            }
            else
            {
                cam.transform.position = new Vector3(11.2f, 10.8f, -16.8f); cam.transform.LookAt(new Vector3(0, 2.4f, 1.8f));
                promptBg.rectTransform.anchorMin = promptBg.rectTransform.anchorMax = new Vector2(.5f, .79f);
                promptBg.rectTransform.sizeDelta = new Vector2(362, 94);
                goalBg.rectTransform.anchorMin = goalBg.rectTransform.anchorMax = new Vector2(.5f, .875f);
                formulaBg.rectTransform.anchorMin = formulaBg.rectTransform.anchorMax = new Vector2(.5f, .105f);
                rulerRt.anchorMin = rulerRt.anchorMax = new Vector2(.18f, .49f);
                exitRt.anchorMin = exitRt.anchorMax = new Vector2(.87f, .49f);
                statusBg.rectTransform.anchorMin = statusBg.rectTransform.anchorMax = new Vector2(.44f, .955f);
                statusBg.rectTransform.sizeDelta = new Vector2(335, 64);
            }
        }

        void ResetVisuals()
        {
            titleSplit = 0f; revealAnim = 0f; hitStop = 0f; wrongKick = 0f; shownEndScore = 0f;
            titleRoot.gameObject.SetActive(true); gameRoot.gameObject.SetActive(false);
            titleRoot.localPosition = Vector3.zero; titleRoot.localScale = Vector3.one;
            if (soapRoot) { soapRoot.localPosition = new Vector3(-2.7f, -.25f, .8f); soapRoot.localRotation = Quaternion.identity; }
            for (int i = 0; i < ribs.Length; i++)
            {
                if (ribs[i]) ribs[i].gameObject.SetActive(false);
                if (lampGlows[i]) lampGlows[i].gameObject.SetActive(false);
                if (sockets[i]) sockets[i].localPosition = new Vector3(sockets[i].localPosition.x, .05f, 0);
            }
            if (ghostPath) ghostPath.enabled = false;
            if (ghostHand) ghostHand.gameObject.SetActive(false);
        }

        void ShowScreen()
        {
            titleG.alpha = flow == Flow.Title || flow == Flow.Opening ? 1f : 0f; titleG.blocksRaycasts = false;
            hudG.alpha = flow == Flow.Practice || flow == Flow.Playing || flow == Flow.Reveal ? 1f : 0f;
            revealG.alpha = flow == Flow.Reveal ? 1f : 0f;
            endG.alpha = flow == Flow.End ? 1f : 0f;
            titleRoot.gameObject.SetActive(flow == Flow.Title || flow == Flow.Opening);
            gameRoot.gameObject.SetActive(flow != Flow.Title && flow != Flow.Opening);
            knobImg.gameObject.SetActive(flow == Flow.Practice || flow == Flow.Playing);
            cutLineImg.gameObject.SetActive(flow == Flow.Practice || flow == Flow.Playing || flow == Flow.Reveal);
            exitImg.gameObject.SetActive(flow == Flow.Practice || flow == Flow.Playing);
        }

        void BeginTitleSplit() { titleSplit = .001f; }

        void LoadProblemVisual(SoapProblem p, bool practice)
        {
            ClearSoapChildren();
            float fullH = 5.15f, cutY = fullH * p.answerHeight / 25f;
            if (p.solid == SolidKind.RectPrism)
            {
                MgfLook.Block("LowerSoap", new Vector3(0, cutY * .5f, 0), new Vector3(2.7f, Mathf.Max(.18f, cutY), 2.15f), .18f, ivoryMat, soapLower);
                MgfLook.Block("UpperSoap", new Vector3(0, cutY + (fullH - cutY) * .5f, 0), new Vector3(2.7f, Mathf.Max(.18f, fullH - cutY), 2.15f), .18f, ivoryMat, soapUpper);
            }
            else
            {
                MgfLook.Prim(PrimitiveType.Cylinder, "LowerSoap", new Vector3(0, cutY * .5f, 0), new Vector3(2.55f, Mathf.Max(.09f, cutY * .5f), 2.55f), ivoryMat, soapLower, false);
                MgfLook.Prim(PrimitiveType.Cylinder, "UpperSoap", new Vector3(0, cutY + (fullH - cutY) * .5f, 0), new Vector3(2.55f, Mathf.Max(.09f, (fullH - cutY) * .5f), 2.55f), ivoryMat, soapUpper, false);
            }
            soapLower.localPosition = soapUpper.localPosition = Vector3.zero; soapLower.localRotation = soapUpper.localRotation = Quaternion.identity;
            cutGlow.gameObject.SetActive(false); wireLine.enabled = trailLine.enabled = false;
            promptTxt.text = p.prompt;
            formulaTxt.text = p.piece == PieceKind.Upper ? "V = 밑넓이 × 위 높이  ·  절단 높이 = 25 − 위 높이" : "V = 밑넓이 × 높이  ·  전체 높이 25 cm";
            if (practice)
            {
                goalTxt.text = "4 cm 눈금에서 막대 끝까지 그으시오";
                formulaTxt.text = "밑넓이 6 cm² · 아래 부피 24 cm³";
                ghostPath.enabled = true; ghostHand.gameObject.SetActive(true); guideArrow.gameObject.SetActive(true); UpdateGhostPath();
            }
            else
            {
                goalTxt.text = st.round <= 6 ? "필요한 부피로 잘라 리브를 세우시오" : "위쪽 조각의 부피로 절단 높이를 찾으시오";
                ghostPath.enabled = false; ghostHand.gameObject.SetActive(false); guideArrow.gameObject.SetActive(false);
            }
            UpdateCutUi();
        }

        void ClearSoapChildren()
        {
            for (int i = soapLower.childCount - 1; i >= 0; i--) Destroy(soapLower.GetChild(i).gameObject);
            for (int i = soapUpper.childCount - 1; i >= 0; i--) Destroy(soapUpper.GetChild(i).gameObject);
        }

        void ReloadSoapForRepair()
        {
            LoadProblemVisual(current, false); formulaTxt.text = WrongExplanation(st.misconceptionId); toastTxt.text = "같은 주문입니다. 새 비누에서 다시 자르시오"; toastClock = 2.1f; toastG.alpha = 1f;
        }

        void BeginWire(int h)
        {
            wireLine.enabled = trailLine.enabled = true; ghostPath.enabled = false; ghostHand.gameObject.SetActive(false); guideArrow.gameObject.SetActive(false);
            cutHandle.localScale = new Vector3(1.18f, .78f, 1.08f); UpdateWire(h, MgfPointer.Position);
        }

        void UpdateWire(int h, Vector2 pointer)
        {
            UpdateCutUi();
            float y = -.05f + h * .205f;
            Vector3 start = new Vector3(-4.95f, y, -.55f);
            float ux = pointer.x / Mathf.Max(1f, Screen.width);
            Vector3 end = new Vector3(Mathf.Lerp(-4.7f, 2.4f, Mathf.Clamp01((ux - .18f) / .65f)), y, -.55f);
            wireLine.SetPosition(0, start); wireLine.SetPosition(1, end);
            trailLine.SetPosition(0, start + Vector3.back * .05f); trailLine.SetPosition(1, end + Vector3.back * .05f);
            cutHandle.localPosition = new Vector3(-4.35f, y, -.45f);
        }

        void CancelWire()
        {
            wireLine.enabled = trailLine.enabled = false; cutHandle.localScale = Vector3.one; UpdateCutUi();
        }

        void UpdateCutUi()
        {
            float u = st.cutHeight / 25f;
            float y = Mathf.Lerp(-244f, 244f, u);
            knobRt.anchorMin = knobRt.anchorMax = land ? new Vector2(.15f, .49f) : new Vector2(.18f, .49f);
            knobRt.anchoredPosition = new Vector2(0, y);
            heightTxt.text = st.cutHeight + " cm";
            cutLineRt.anchorMin = cutLineRt.anchorMax = new Vector2(.49f, .49f);
            cutLineRt.anchoredPosition = new Vector2(0, y);
            cutLineRt.sizeDelta = new Vector2(land ? 520 : 250, 5);
        }

        void UpdateGhostPath()
        {
            float y = -.05f + 4 * .205f;
            for (int i = 0; i < ghostPath.positionCount; i++)
            {
                float t = i / (float)(ghostPath.positionCount - 1);
                float x = t < .28f ? -5.0f : Mathf.Lerp(-5.0f, 2.5f, (t - .28f) / .72f);
                float yy = t < .28f ? Mathf.Lerp(2.6f, y, t / .28f) : y;
                ghostPath.SetPosition(i, new Vector3(x, yy, -.65f));
            }
        }

        void BoostGuide()
        {
            guideBoost = 1f;
            if (flow == Flow.Practice) { ghostPath.enabled = true; ghostHand.gameObject.SetActive(true); guideArrow.gameObject.SetActive(true); UpdateGhostPath(); }
        }

        void BeginRevealVisual(bool correct, bool practice, int selectedHeight, string misconception)
        {
            revealAnim = 0f; hitStop = correct ? .085f : 0f; wrongKick = correct ? 0f : 1f;
            cutGlow.localPosition = new Vector3(0, selectedHeight * .205f, -.72f); cutGlow.gameObject.SetActive(true);
            cutGlow.localScale = new Vector3(3.2f, .07f, .18f);
            UpdateWire(selectedHeight, new Vector2(Screen.width * .82f, HeightToScreen(selectedHeight)));
            if (correct)
            {
                revealTitleTxt.text = practice ? "4 cm, 정확한 절단" : "리브가 잠겼습니다";
                revealTitleTxt.color = GrapeDeep; revealFormulaTxt.text = Explain(current);
                if (!practice)
                {
                    int slot = Mathf.Clamp(st.solved - 1, 0, ribs.Length - 1); ribs[slot].gameObject.SetActive(true); ribs[slot].localScale = Vector3.one * .08f;
                    lampGlows[slot].gameObject.SetActive(true);
                }
                MgfFx.Glow(soapRoot.position + Vector3.up * (selectedHeight * .205f), Saffron, 14, .24f);
                MgfSfx.Play("correct", .46f);
            }
            else
            {
                revealTitleTxt.text = "절단 부피를 비교하시오"; revealTitleTxt.color = Coral; revealFormulaTxt.text = WrongExplanation(misconception);
                MgfSfx.Play("wrong", .31f);
            }
            MgfFx.Punch(cutHandle, correct ? .16f : .07f, .28f);
        }

        string Explain(SoapProblem p)
        {
            if (p.id.StartsWith("practice")) return "6 cm² × 4 cm = 24 cm³";
            if (p.solid == SolidKind.RectPrism)
                return p.a + " × " + p.b + " = " + p.baseCoefficient + " cm²\n" + p.baseCoefficient + " × " + p.pieceHeight + " = " + p.targetCoefficient + " cm³";
            return "π × " + p.radius + "² = " + BinuRules.Pi(p.baseCoefficient) + " cm²\n" + BinuRules.Pi(p.baseCoefficient) + " × " + p.pieceHeight + " = " + BinuRules.Pi(p.targetCoefficient) + " cm³";
        }

        string WrongExplanation(string id)
        {
            switch (id)
            {
                case "rectangle_side_sum_as_area": return "밑넓이는 두 변의 합이 아니라 가로 × 세로입니다.";
                case "radius_not_squared": return "원기둥의 밑넓이는 πr²입니다. 반지름을 제곱하시오.";
                case "circumference_as_base_area": return "2πr은 둘레입니다. 부피에는 밑넓이 πr²를 쓰시오.";
                case "upper_piece_height_as_cut": return "위 높이와 절단 높이는 다릅니다. 25에서 위 높이를 빼시오.";
                default: return "목표 부피 ÷ 밑넓이로 조각 높이를 다시 구하시오.";
            }
        }

        void FinishRevealVisual(bool correct)
        {
            cutGlow.gameObject.SetActive(false); wireLine.enabled = trailLine.enabled = false; cutHandle.localScale = Vector3.one;
            soapLower.localPosition = soapUpper.localPosition = Vector3.zero; soapLower.localRotation = soapUpper.localRotation = Quaternion.identity;
            soapRoot.localPosition = new Vector3(-2.7f, -.25f, .8f); soapRoot.localRotation = Quaternion.identity;
            if (revealPractice && correct && ribs[0]) { ribs[0].gameObject.SetActive(false); lampGlows[0].gameObject.SetActive(false); }
        }

        void ShowEndVisual(bool clear, string reason)
        {
            shownEndScore = 0f;
            endTitleTxt.text = clear ? "기포 협곡 연결 완료" : (reason == "time" ? "절단 시간이 끝났습니다" : reason == "mastery" ? "첫 칼 기준에 못 미쳤습니다" : "여분 비누가 없습니다");
            endTitleTxt.color = clear ? GrapeDeep : Coral;
            endStatsTxt.text = "리브 " + st.solved + "/8\n첫 칼 " + st.firstAttemptCorrect + "/" + Math.Max(1, st.firstAttemptTotal) + "  ·  여분 " + st.lives + "개\n" +
                (clear ? "포밍들이 새 층으로 건넜습니다" : "밑넓이와 보낼 조각을 확인하고 다시 도전하시오");
        }

        void Ripple(Vector2 screen, string message)
        {
            float sx = screen.x / Mathf.Max(1f, Screen.width) * 390f - 195f;
            float sy = screen.y / Mathf.Max(1f, Screen.height) * 844f - 422f;
            rippleRt.anchoredPosition = new Vector2(sx, sy); rippleRt.localScale = Vector3.one * .25f; rippleRt.gameObject.SetActive(true);
            rippleImg.color = Ivory; rippleClock = .42f; toastTxt.text = message; toastClock = 1.25f; toastG.alpha = 1f;
        }

        void TickView(float dt)
        {
            clock += dt;
            for (int i = 0; i < ambientBubbles.Count; i++) if (ambientBubbles[i])
                ambientBubbles[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(clock * (.38f + i * .004f) + i) * 1.5f);
            for (int i = 0; i < formings.Length; i++) if (formings[i])
            {
                Vector3 p = formings[i].localPosition; p.y = -.05f + Mathf.Sin(clock * (1.3f + i * .03f) + i) * .055f; formings[i].localPosition = p;
                formings[i].localRotation = Quaternion.Euler(0, 180f + Mathf.Sin(clock * .7f + i) * 9f, Mathf.Sin(clock * 1.4f + i) * 2f);
            }
            if (flow == Flow.Title)
            {
                float p = 1f + .025f * Mathf.Sin(clock * 1.45f);
                titleCta.rectTransform.localScale = Vector3.one * p;
                titleHandle.localPosition = new Vector3(Mathf.Sin(clock * .85f) * .12f, -.05f + Mathf.Sin(clock * 1.7f) * .06f, -.2f);
                titleLogo.transform.localScale = Vector3.one * (1f + .012f * Mathf.Sin(clock));
            }
            if (flow == Flow.Opening)
            {
                titleSplit = Mathf.Min(1f, titleSplit + dt * 3.2f); float e = 1f - Mathf.Pow(1f - titleSplit, 3f);
                titleRoot.localScale = new Vector3(1f + e * .12f, 1f - e * .08f, 1f);
                titleRoot.localPosition = new Vector3(0, e * .45f, e * 1.1f); titleG.alpha = 1f - e * .72f;
            }
            if (guideBoost > 0) guideBoost = Mathf.Max(0, guideBoost - dt * .55f);
            if (ghostPath.enabled)
            {
                float u = (clock * .72f) % 1f; int idx = Mathf.Clamp(Mathf.RoundToInt(u * (ghostPath.positionCount - 1)), 0, ghostPath.positionCount - 1);
                ghostHand.position = ghostPath.GetPosition(idx); float pulse = 1f + .18f * Mathf.Sin(clock * 5f) + guideBoost * .28f; ghostHand.localScale = Vector3.one * (.48f * pulse);
                guideArrow.localPosition = new Vector3(1.9f + Mathf.Sin(clock * 3.2f) * .18f, ghostPath.GetPosition(ghostPath.positionCount - 1).y, -.55f);
            }
            if (hitStop > 0) { hitStop = Mathf.Max(0, hitStop - dt); UpdateUi(dt); return; }
            if (flow == Flow.Reveal)
            {
                revealAnim = Mathf.Min(1f, revealAnim + dt * (revealCorrect ? 1.55f : 2.25f));
                if (revealCorrect)
                {
                    float t = 1f - Mathf.Pow(1f - revealAnim, 3f);
                    float split = Mathf.Sin(Mathf.Clamp01(t * 1.6f) * Mathf.PI) * .38f;
                    soapLower.localPosition = Vector3.down * split; soapUpper.localPosition = Vector3.up * split;
                    Transform chosen = current.piece == PieceKind.Lower ? soapLower : soapUpper;
                    chosen.localRotation = Quaternion.Euler(0, 0, -90f * Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .30f) / .70f)));
                    chosen.localPosition += new Vector3(Mathf.SmoothStep(0, 5.2f, Mathf.Clamp01((t - .35f) / .65f)), Mathf.Sin(t * Mathf.PI) * 1.25f, 0);
                    int slot = revealPractice ? 0 : Mathf.Clamp(st.solved - 1, 0, ribs.Length - 1);
                    if (ribs[slot] && ribs[slot].gameObject.activeSelf)
                    {
                        float overshoot = 1f + .18f * Mathf.Sin(Mathf.Clamp01(t * 1.25f) * Mathf.PI);
                        ribs[slot].localScale = Vector3.one * Mathf.Lerp(.08f, overshoot, t);
                        formings[slot].localPosition = Vector3.Lerp(formings[slot].localPosition, new Vector3(.45f + slot * .45f, .42f + Mathf.Sin(t * Mathf.PI) * .5f, 2.4f), dt * 3.5f);
                    }
                    if (!revealPractice && st.combo >= 3)
                        for (int i = 0; i < st.solved; i++) { Vector3 p = sockets[i].localPosition; p.y = .05f + Mathf.Sin(Mathf.Clamp01(t * 1.4f - i * .06f) * Mathf.PI) * .55f; sockets[i].localPosition = p; }
                }
                else
                {
                    float k = 1f - revealAnim; soapRoot.localPosition = new Vector3(-2.7f + Mathf.Sin(revealAnim * Mathf.PI * 7f) * .18f * k, -.25f, .8f);
                    cutGlow.localScale = new Vector3(3.2f + Mathf.Sin(clock * 13f) * .25f, .07f, .18f);
                }
            }
            UpdateUi(dt);
        }

        void UpdateUi(float dt)
        {
            UpdateCutUi();
            shownScore = Mathf.MoveTowards(shownScore, st.score, Mathf.Max(120f, Mathf.Abs(st.score - shownScore) * 5f) * dt);
            scoreTxt.text = Mathf.RoundToInt(shownScore).ToString();
            scoreTxt.transform.localScale = Vector3.one * (1f + .08f * Mathf.Sin(Mathf.Clamp01(Mathf.Abs(st.score - shownScore) / 80f) * Mathf.PI));
            progressTxt.text = "리브 " + st.solved + "/8"; firstTxt.text = "첫 칼 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal;
            int sec = Mathf.Max(0, Mathf.CeilToInt(timeLeft)); if (sec != shownSecond) { shownSecond = sec; timeTxt.text = flow == Flow.Practice ? "연습" : sec + "초"; }
            for (int i = 0; i < lifeImgs.Length; i++) lifeImgs[i].color = st.lives > i ? Saffron : new Color(.28f, .22f, .30f, .35f);
            if (toastClock > 0) { toastClock = Mathf.Max(0, toastClock - dt); toastG.alpha = Mathf.Min(1f, toastClock * 5f); }
            else toastG.alpha = 0f;
            if (rippleClock > 0)
            {
                rippleClock = Mathf.Max(0, rippleClock - dt); float u = 1f - rippleClock / .42f;
                rippleRt.localScale = Vector3.one * Mathf.Lerp(.25f, 1.75f, u); rippleImg.color = new Color(Ivory.r, Ivory.g, Ivory.b, 1f - u);
                if (rippleClock <= 0) rippleRt.gameObject.SetActive(false);
            }
            if (flow == Flow.Reveal) revealBg.transform.localScale = Vector3.one * (1f + .06f * Mathf.Sin(Mathf.Clamp01(revealClock / .32f) * Mathf.PI));
            if (flow == Flow.End)
            {
                shownEndScore = Mathf.MoveTowards(shownEndScore, st.score, Mathf.Max(180f, st.score * 1.8f) * dt);
                endScoreTxt.text = Mathf.RoundToInt(shownEndScore).ToString();
                endScoreTxt.transform.localScale = Vector3.one * (1f + .035f * Mathf.Sin(clock * 2.5f));
            }
            int best = PlayerPrefs.GetInt("binu-sseoreo-best", 0);
            titleBest.text = best > 0 ? "최고 " + best + "점 · 리브 8개" : "리브 8개 · 첫 칼 6개";
        }
    }
}
