// 팝콘 핀볼 — 노란 자판기 2.5D 디오라마와 정면 원판 UI.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.PopcornPinball
{
    public partial class PopcornPinballGame
    {
        static readonly Color Butter = new Color32(232, 188, 48, 255);
        static readonly Color ButterLight = new Color32(255, 221, 104, 255);
        static readonly Color Ivory = new Color32(255, 241, 207, 255);
        static readonly Color Red = new Color32(201, 74, 50, 255);
        static readonly Color Ink = new Color32(72, 64, 56, 255);
        static readonly Color Tin = new Color32(185, 181, 169, 255);
        static readonly Color Mint = new Color32(75, 142, 119, 255);

        Camera cam;
        Transform worldRoot, creatureRoot, titleCartonWorld;
        readonly Transform[] worldPopcorn = new Transform[18];
        readonly Transform[] worldBumpers = new Transform[3];
        readonly Transform[] worldSlots = new Transform[8];
        Material butterMat, butterLightMat, ivoryMat, redMat, inkMat, tinMat, glassMat;
        float worldClock;

        RectTransform uiRoot, titleRoot, gameRoot, feedbackRoot, bridgeRoot, resultRoot;
        CanvasGroup titleGroup, gameGroup, resultGroup;
        Image titleArt;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleStart;
        RectTransform titleCta;

        RectTransform hudPlaque, promptPlaque, circleRoot, cartonRoot;
        TextMeshProUGUI hudText, promptText, goalText, angleText, relationText, toastText;
        TextMeshProUGUI pLabel, aLabel, bLabel, centerLabel;
        Image circleFace, circleRing, fixedRay, aimRay, targetRay, barrel, handle, pDot, aDot, bDot, centerDot;
        readonly Image[] tickMarks = new Image[17];
        readonly Image[] lives = new Image[3];
        readonly Image[] cartonSlots = new Image[8];
        readonly Image[] bumpers = new Image[3];
        readonly Image[] trail = new Image[6];
        Image projectile, guideRing, guideFinger, guidePath, guideArrow, ripple;
        RectTransform handleRt, projectileRt, guideRingRt, guideFingerRt, guidePathRt, guideArrowRt, rippleRt;

        TextMeshProUGUI feedbackTitle, feedbackMath;
        Image feedbackPlate;
        TextMeshProUGUI bridgeTitle, bridgeBody, bridgeAngleP, bridgeAngleQ;
        Image bridgeCreature, bridgeTarget, bridgePath;
        RectTransform bridgeCreatureRt, bridgeTargetRt;
        bool bridgeDragging;

        TextMeshProUGUI resultTitle, resultStats, resultReason, resultRestart;
        RectTransform restartCta;

        Sprite plateSprite, ringSprite, discSprite, faceSprite, arrowSprite, stripeSprite;
        Vector2 circleCenter;
        float circleRadius;
        Vector2 pPoint, aPoint, bPoint, refDir, aimDir;
        Vector2 dragStart;
        bool aimDragging;
        float draggedDistance;
        int draggedTick;
        float guideClock, guideBoost;
        float rippleClock = 20f, refuseClock = 20f;
        int fxBeat = -1;
        int lastShownSecond = -1;
        float shownScore, shownResult;
        int layoutW = -1, layoutH = -1;
        bool layoutLand;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        RectTransform Full(string name, Transform parent)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        Image Img(string name, Transform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        TextMeshProUGUI Txt(string name, Transform parent, string text, Vector2 pos, float size, Color color,
            float width, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), pos, new Vector2(width, size * 3.5f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        Sprite MakePlateSprite(bool striped = false)
        {
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                name = striped ? "Red paper carton stripe" : "Rolled tin plaque",
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float nx = Mathf.Abs((x + .5f) / S * 2f - 1f);
                float ny = Mathf.Abs((y + .5f) / S * 2f - 1f);
                float d = Mathf.Max(nx - .88f, ny - .84f);
                float a = Mathf.Clamp01(.045f - d) * 24f;
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d + .055f) * 34f);
                Color c = Color.Lerp(Color.white * .88f, Color.white, rim * .7f);
                if (striped && ((x + y) / 17) % 2 == 0) c *= .90f;
                px[y * S + x] = new Color(c.r, c.g, c.b, a);
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(18, 18, 18, 18));
        }

        Sprite MakeRingSprite(float inner = .72f)
        {
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Engraved circular rail", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = (x + .5f) / S * 2f - 1f, dy = (y + .5f) / S * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((.98f - d) * 24f) * Mathf.Clamp01((d - inner) * 24f);
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        Sprite MakeArrowSprite()
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Mechanical guide arrow", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float nx = (x + .5f) / S, ny = Mathf.Abs((y + .5f) / S - .5f) * 2f;
                bool shaft = nx < .60f && ny < .17f;
                bool head = nx > .33f && ny < (nx - .28f) * .82f;
                px[y * S + x] = new Color32(255, 255, 255, (byte)((shaft || head) ? 255 : 0));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        Sprite MakeFilledDiscSprite()
        {
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Opaque powder coated aiming face", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = (x + .5f) / S * 2f - 1f, dy = (y + .5f) / S * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((.985f - d) * 32f);
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        void BuildWorld()
        {
            butterMat = MgfLook.Lit(Butter, .54f, .03f);
            butterLightMat = MgfLook.Lit(ButterLight, .62f, .04f);
            ivoryMat = MgfLook.Lit(Ivory, .72f, .01f);
            redMat = MgfLook.Lit(Red, .48f, .01f);
            inkMat = MgfLook.Lit(Ink, .26f, .02f);
            tinMat = MgfLook.Lit(Tin, .72f, .18f);
            glassMat = MgfLook.Alpha(new Color(1f, .96f, .78f, .22f), MgfLook.SoftDot);

            MgfLook.Sky(new Color32(255, 221, 108, 255), new Color32(242, 190, 48, 255), new Color32(174, 109, 35, 255), .88f);
            MgfLook.Sun(new Vector3(42f, -31f, 18f), new Color(1f, .91f, .72f), 1.18f, .58f);
            cam = MgfLook.Camera(new Vector3(0f, 10.6f, -16.5f), new Vector3(0f, 1.5f, 2.5f), 38f);
            cam.orthographic = true;
            cam.orthographicSize = 9.4f;
            cam.backgroundColor = Butter;

            worldRoot = new GameObject("Butter yellow popcorn vending machine").transform;
            MgfLook.Block("Powder coated cabinet back", new Vector3(0, 1.8f, 5.8f), new Vector3(25f, 18f, 1.0f), .52f, butterMat, worldRoot);
            MgfLook.Block("Left rolled cabinet wall", new Vector3(-9.1f, 1.6f, 3.8f), new Vector3(2.0f, 18f, 3.6f), .62f, butterLightMat, worldRoot);
            MgfLook.Block("Right rolled cabinet wall", new Vector3(9.1f, 1.6f, 3.8f), new Vector3(2.0f, 18f, 3.6f), .62f, butterLightMat, worldRoot);
            MgfLook.Block("Lower machine shelf", new Vector3(0, -5.4f, 2.2f), new Vector3(18f, 1.0f, 6f), .38f, redMat, worldRoot);
            for (int i = 0; i < 5; i++)
            {
                var rail = MgfLook.Block("Thin tin chute " + i, new Vector3(-7.2f + i * 3.6f, -.5f + (i % 2) * .7f, 4.9f),
                    new Vector3(2.5f, .18f, .24f), .08f, tinMat, worldRoot);
                rail.transform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -12f : 12f);
            }
            for (int i = 0; i < worldPopcorn.Length; i++)
            {
                var root = new GameObject("Cached popcorn cluster " + i).transform;
                root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(-7.4f + (i % 6) * 2.9f, 5.0f + (i / 6) * .86f, 3.6f + (i % 3) * .16f);
                for (int k = 0; k < 3; k++)
                    MgfLook.Prim(PrimitiveType.Sphere, "Ivory lobe", new Vector3((k - 1) * .24f, Mathf.Abs(k - 1) * .10f, 0),
                        new Vector3(.54f, .46f, .40f), ivoryMat, root, false);
                MgfLook.Prim(PrimitiveType.Sphere, "Kernel foot", new Vector3(0, -.30f, 0), new Vector3(.22f, .17f, .18f), inkMat, root, false);
                worldPopcorn[i] = root;
            }
            for (int i = 0; i < worldBumpers.Length; i++)
            {
                var b = MgfLook.Prim(PrimitiveType.Cylinder, "Spring bumper " + i,
                    new Vector3(3.2f + i * 1.8f, 1.3f - i * 1.4f, 2.6f), new Vector3(.85f, .30f, .85f),
                    i == 1 ? redMat : tinMat, worldRoot, false);
                b.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                worldBumpers[i] = b.transform;
            }
            for (int i = 0; i < worldSlots.Length; i++)
            {
                var slot = MgfLook.Block("Carton chamber " + (i + 1), new Vector3(-5.25f + i * 1.5f, -4.45f, 1.4f),
                    new Vector3(1.24f, 1.05f, 1.0f), .18f, i % 2 == 0 ? redMat : ivoryMat, worldRoot);
                worldSlots[i] = slot.transform;
            }
            titleCartonWorld = MgfLook.Block("Opened title carton lid", new Vector3(0, -2.9f, 1.6f), new Vector3(8.8f, 2.3f, .42f), .28f, redMat, worldRoot).transform;
            titleCartonWorld.localRotation = Quaternion.Euler(-18f, 0, -4f);
            creatureRoot = MakeCreature("Waiting popcorn creature", new Vector3(-2.6f, -.9f, 1.0f), worldRoot);
        }

        Transform MakeCreature(string name, Vector3 pos, Transform parent)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            MgfLook.Prim(PrimitiveType.Sphere, "Left lobe", new Vector3(-.32f, .08f, 0), new Vector3(.82f, .72f, .62f), ivoryMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Top lobe", new Vector3(0, .35f, .02f), new Vector3(.88f, .78f, .66f), ivoryMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Right lobe", new Vector3(.34f, .05f, 0), new Vector3(.78f, .70f, .60f), ivoryMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Left eye", new Vector3(-.18f, .24f, -.55f), new Vector3(.10f, .12f, .08f), inkMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Right eye", new Vector3(.18f, .24f, -.55f), new Vector3(.10f, .12f, .08f), inkMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Left kernel foot", new Vector3(-.24f, -.48f, 0), new Vector3(.22f, .14f, .28f), inkMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Right kernel foot", new Vector3(.24f, -.48f, 0), new Vector3(.22f, .14f, .28f), inkMat, root, false);
            return root;
        }

        void BuildUi()
        {
            plateSprite = MakePlateSprite();
            stripeSprite = MakePlateSprite(true);
            ringSprite = MakeRingSprite();
            faceSprite = MakeFilledDiscSprite();
            arrowSprite = MakeArrowSprite();
            discSprite = Sprite.Create(MgfLook.SoftDot, new Rect(0, 0, MgfLook.SoftDot.width, MgfLook.SoftDot.height), new Vector2(.5f, .5f), 100f);

            var canvasGo = new GameObject("Popcorn Pinball Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            uiRoot = Full("UI root", canvasGo.transform);

            titleRoot = Full("Title world overlay", uiRoot);
            titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
            Texture2D titleTexture = Resources.Load<Texture2D>("PopcornPinball/title");
            if (titleTexture)
            {
                titleArt = Img("Generated vending machine key art", titleRoot, Vector2.zero, Vector2.one, Color.white);
                titleArt.sprite = Sprite.Create(titleTexture, new Rect(0, 0, titleTexture.width, titleTexture.height), new Vector2(.5f, .5f), 100f);
                titleArt.preserveAspect = true;
                titleArt.rectTransform.anchorMin = Vector2.zero;
                titleArt.rectTransform.anchorMax = Vector2.one;
                titleArt.rectTransform.offsetMin = titleArt.rectTransform.offsetMax = Vector2.zero;
            }
            titleLogo = Txt("Carton lid logo", titleRoot, "팝콘\n핀볼", new Vector2(0, 185), 58, Ink, 330);
            titleLogo.outlineColor = Ivory;
            titleLogo.outlineWidth = .20f;
            titleTag = Txt("Printed carton tagline", titleRoot, "포신을 돌려 쏴라", new Vector2(0, 80), 24, Ink, 310);
            titleCta = R("Start carton", titleRoot, new Vector2(.5f, .5f), new Vector2(0, -176), new Vector2(286, 94));
            var ctaImg = titleCta.gameObject.AddComponent<Image>(); ctaImg.sprite = stripeSprite; ctaImg.type = Image.Type.Sliced; ctaImg.color = Red; ctaImg.raycastTarget = false;
            titleStart = Txt("Start carton print", titleCta, "상자를 눌러 시작", Vector2.zero, 24, Ivory, 250);
            titleMeta = Txt("Machine product label", titleRoot, "중3 · 원주각   최고 기록은 이 기기에 저장", new Vector2(0, -273), 14, Ink, 350);

            gameRoot = Full("Game overlay", uiRoot);
            gameGroup = gameRoot.gameObject.AddComponent<CanvasGroup>();
            hudPlaque = R("Top rolled HUD", gameRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.one);
            var hudBg = hudPlaque.gameObject.AddComponent<Image>(); hudBg.sprite = plateSprite; hudBg.type = Image.Type.Sliced; hudBg.color = Ivory; hudBg.raycastTarget = false;
            hudText = Txt("HUD engraved text", hudPlaque, "", Vector2.zero, 16, Ink, 355);
            goalText = Txt("Persistent goal line", gameRoot, "포신 각을 맞춰 쏘면 상자가 찬다", Vector2.zero, 17, Red, 360);

            promptPlaque = R("Bright target nameplate", gameRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.one);
            var promptBg = promptPlaque.gameObject.AddComponent<Image>(); promptBg.sprite = plateSprite; promptBg.type = Image.Type.Sliced; promptBg.color = Ivory; promptBg.raycastTarget = false;
            promptText = Txt("Target condition", promptPlaque, "", Vector2.zero, 18, Ink, 340);

            circleRoot = R("Front orthographic aiming chamber", gameRoot, new Vector2(.5f, .5f), Vector2.zero, new Vector2(320, 320));
            circleFace = Img("Opaque butter aiming face", circleRoot, Vector2.zero, new Vector2(300, 300), ButterLight, faceSprite);
            circleRing = Img("Rolled circular rim", circleRoot, Vector2.zero, new Vector2(310, 310), Ink, ringSprite);
            centerDot = Img("Center O", circleRoot, Vector2.zero, new Vector2(18, 18), Ink, discSprite);
            centerLabel = Txt("Center O label", circleRoot, "O", new Vector2(18, -12), 14, Ink, 30);
            fixedRay = Img("Fixed line PA", circleRoot, Vector2.zero, new Vector2(10, 5), Tin, discSprite);
            aimRay = Img("Current ray PB", circleRoot, Vector2.zero, new Vector2(10, 7), Red, discSprite);
            targetRay = Img("Revealed target ray", circleRoot, Vector2.zero, new Vector2(10, 4), Mint, discSprite);
            barrel = Img("Short movable barrel", circleRoot, Vector2.zero, new Vector2(72, 24), Ink, plateSprite);
            handle = Img("Cherry red barrel handle", circleRoot, Vector2.zero, new Vector2(58, 58), Red, ringSprite);
            handleRt = handle.rectTransform;
            pDot = Img("Point P", circleRoot, Vector2.zero, new Vector2(24, 24), Red, discSprite);
            aDot = Img("Point A", circleRoot, Vector2.zero, new Vector2(20, 20), Ink, discSprite);
            bDot = Img("Current point B question", circleRoot, Vector2.zero, new Vector2(20, 20), Tin, discSprite);
            pLabel = Txt("P label", circleRoot, "P", Vector2.zero, 15, Ink, 28);
            aLabel = Txt("A label", circleRoot, "A", Vector2.zero, 15, Ink, 28);
            bLabel = Txt("B question label", circleRoot, "B?", Vector2.zero, 15, Ink, 38);
            for (int i = 0; i < tickMarks.Length; i++)
                tickMarks[i] = Img("Equal 5 degree tick " + (i + 1), circleRoot, Vector2.zero, new Vector2(11, 3), Ink, discSprite);
            angleText = Txt("Magnified selected angle", circleRoot, "주차 0°", new Vector2(0, -116), 24, Ink, 175);
            relationText = Txt("Visible relation", circleRoot, "중심각 = 원주각 × 2", new Vector2(0, 118), 15, Ink, 230);

            for (int i = 0; i < bumpers.Length; i++)
                bumpers[i] = Img("Metal bumper UI " + i, gameRoot, Vector2.zero, new Vector2(42, 42), i == 1 ? Red : Tin, ringSprite);
            projectile = Img("Flying popcorn kernel", gameRoot, Vector2.zero, new Vector2(30, 30), Ivory, discSprite);
            projectileRt = projectile.rectTransform;
            for (int i = 0; i < trail.Length; i++)
                trail[i] = Img("Butter flight trail " + i, gameRoot, Vector2.zero, new Vector2(18 - i * 2, 18 - i * 2), new Color(1f, .86f, .35f, .25f), discSprite);

            cartonRoot = R("Eight chamber paper carton", gameRoot, new Vector2(.5f, .5f), Vector2.zero, new Vector2(350, 76));
            var cartonBg = cartonRoot.gameObject.AddComponent<Image>(); cartonBg.sprite = stripeSprite; cartonBg.type = Image.Type.Sliced; cartonBg.color = Red; cartonBg.raycastTarget = false;
            for (int i = 0; i < cartonSlots.Length; i++)
                cartonSlots[i] = Img("Carton fill chamber " + (i + 1), cartonRoot, new Vector2(-139 + i * 40, 0), new Vector2(32, 42), new Color(Ivory.r, Ivory.g, Ivory.b, .28f), discSprite);
            for (int i = 0; i < lives.Length; i++)
                lives[i] = Img("Spare popcorn " + (i + 1), gameRoot, Vector2.zero, new Vector2(26, 26), Ivory, discSprite);

            toastText = Txt("Invalid input reason", gameRoot, "", Vector2.zero, 16, Ivory, 310);
            ripple = Img("Input butter ripple", gameRoot, Vector2.zero, new Vector2(36, 36), new Color(1f, .86f, .32f, 0f), ringSprite);
            rippleRt = ripple.rectTransform;
            guidePath = Img("Ghost drag path", gameRoot, Vector2.zero, new Vector2(120, 5), new Color(1f, .96f, .77f, .72f), discSprite);
            guidePathRt = guidePath.rectTransform;
            guideArrow = Img("Ghost destination arrow", gameRoot, Vector2.zero, new Vector2(44, 44), Red, arrowSprite);
            guideArrowRt = guideArrow.rectTransform;
            guideRing = Img("Animated handle border trail", gameRoot, Vector2.zero, new Vector2(84, 84), new Color(1f, .96f, .72f, .9f), ringSprite);
            guideRingRt = guideRing.rectTransform;
            guideFinger = Img("Ghost finger", gameRoot, Vector2.zero, new Vector2(38, 52), new Color(Ivory.r, Ivory.g, Ivory.b, .9f), discSprite);
            guideFingerRt = guideFinger.rectTransform;

            feedbackRoot = Full("Shot evidence", gameRoot);
            feedbackPlate = Img("Evidence plate", feedbackRoot, Vector2.zero, new Vector2(330, 104), Ivory, plateSprite);
            feedbackPlate.type = Image.Type.Sliced;
            feedbackTitle = Txt("Evidence title", feedbackRoot, "", new Vector2(0, 23), 19, Red, 300);
            feedbackMath = Txt("Evidence math", feedbackRoot, "", new Vector2(0, -19), 17, Ink, 300);

            bridgeRoot = Full("Concept bridge", gameRoot);
            bridgeTitle = Txt("Bridge heading", bridgeRoot, "같은 호, 같은 원주각", new Vector2(0, 158), 25, Red, 350);
            bridgeBody = Txt("Bridge instruction", bridgeRoot, "팝콘을 P에서 Q로 옮겨 보자", new Vector2(0, 120), 17, Ink, 350);
            bridgePath = Img("Same arc path", bridgeRoot, Vector2.zero, new Vector2(220, 7), new Color(Red.r, Red.g, Red.b, .55f), discSprite);
            bridgeCreature = Img("Movable bridge popcorn", bridgeRoot, new Vector2(-110, 20), new Vector2(58, 58), Ivory, discSprite);
            bridgeCreatureRt = bridgeCreature.rectTransform;
            bridgeTarget = Img("Bridge Q target", bridgeRoot, new Vector2(110, 20), new Vector2(78, 78), new Color(Mint.r, Mint.g, Mint.b, .62f), ringSprite);
            bridgeTargetRt = bridgeTarget.rectTransform;
            bridgeAngleP = Txt("Angle at P", bridgeRoot, "P   40°", new Vector2(-110, -36), 19, Ink, 120);
            bridgeAngleQ = Txt("Angle at Q", bridgeRoot, "Q   40°", new Vector2(110, -36), 19, Ink, 120);

            resultRoot = Full("Result carton", uiRoot);
            resultGroup = resultRoot.gameObject.AddComponent<CanvasGroup>();
            var resultPlate = Img("Large result paper lid", resultRoot, Vector2.zero, new Vector2(350, 430), Ivory, plateSprite);
            resultPlate.type = Image.Type.Sliced;
            resultTitle = Txt("Result heading", resultRoot, "", new Vector2(0, 135), 42, Red, 320);
            resultStats = Txt("Animated result count", resultRoot, "", new Vector2(0, 45), 25, Ink, 320);
            resultReason = Txt("Result learning note", resultRoot, "", new Vector2(0, -52), 16, Ink, 305);
            resultReason.rectTransform.sizeDelta = new Vector2(310, 94);
            restartCta = R("Restart carton", resultRoot, new Vector2(.5f, .5f), new Vector2(0, -148), new Vector2(260, 70));
            var restartBg = restartCta.gameObject.AddComponent<Image>(); restartBg.sprite = stripeSprite; restartBg.type = Image.Type.Sliced; restartBg.color = Red; restartBg.raycastTarget = false;
            resultRestart = Txt("Restart print", restartCta, "새 상자 시작", Vector2.zero, 23, Ivory, 230);
        }

        void HandlePointer()
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, MgfPointer.Position, null, out local);
            if (MgfPointer.Down)
            {
                ShowRipple(local);
                NotePointer("pointer-down");
                if (phase == Phase.Title)
                {
                    if (Inside(titleCta, local) || Vector2.Distance(local, titleCta.anchoredPosition) < 180f) StartPractice();
                    else RefuseAim("가운데 종이 상자를 누르시오");
                    return;
                }
                if (phase == Phase.Clear || phase == Phase.GameOver)
                {
                    if (Inside(restartCta, local) || Vector2.Distance(local, restartCta.anchoredPosition) < 150f) StartPractice();
                    else RefuseAim("새 상자를 눌러 다시 시작하시오");
                    return;
                }
                if (phase == Phase.SameArcBridge)
                {
                    if (Vector2.Distance(local, bridgeCreatureRt.anchoredPosition) < 90f)
                    {
                        bridgeDragging = true;
                        st.dragging = true;
                        st.lastAction = "bridge-drag";
                        MgfSfx.Play("tap", .24f);
                    }
                    else RefuseAim("왼쪽 팝콘을 잡아 Q로 옮기시오");
                    return;
                }
                if (phase != Phase.Practice && phase != Phase.Playing && phase != Phase.Feedback)
                {
                    RefuseAim("연출이 끝나면 포신을 잡으시오");
                    return;
                }
                if (phase == Phase.Feedback)
                {
                    RefuseAim("탄도를 확인하고 잠시 기다리시오");
                    return;
                }

                Vector2 chamberLocal = local - circleRoot.anchoredPosition;
                if (Vector2.Distance(chamberLocal, handleRt.anchoredPosition) < 105f || Vector2.Distance(chamberLocal, pPoint) < 120f)
                {
                    aimDragging = true;
                    st.dragging = true;
                    dragStart = chamberLocal;
                    draggedDistance = 0f;
                    draggedTick = 0;
                    guideBoost = 0f;
                    handle.rectTransform.localScale = new Vector3(1.12f, .86f, 1f);
                    st.lastAction = "aim-drag-start";
                    MgfSfx.Play("tap", .23f);
                }
                else RefuseAim("포신의 빨간 손잡이를 잡으시오");
            }

            if (MgfPointer.Held)
            {
                if (bridgeDragging)
                {
                    bridgeCreatureRt.anchoredPosition = local;
                    st.lastAction = "bridge-dragging";
                }
                else if (aimDragging)
                {
                    Vector2 chamberLocal = local - circleRoot.anchoredPosition;
                    draggedDistance = Mathf.Max(draggedDistance, Vector2.Distance(chamberLocal, dragStart));
                    Vector2 v = chamberLocal - pPoint;
                    if (v.sqrMagnitude > 100f)
                    {
                        float theta = -Vector2.SignedAngle(refDir, v.normalized);
                        if (theta < 0f) theta += 360f;
                        if (theta >= 2.5f && theta < 87.5f)
                        {
                            int tick = Mathf.FloorToInt((theta - 2.5f) / 5f) + 1;
                            if (tick != draggedTick)
                            {
                                draggedTick = tick;
                                SetAimTick(tick, true);
                            }
                        }
                        else draggedTick = 0;
                    }
                }
            }

            if (MgfPointer.Up)
            {
                if (bridgeDragging)
                {
                    bridgeDragging = false;
                    st.dragging = false;
                    if (Vector2.Distance(bridgeCreatureRt.anchoredPosition, bridgeTargetRt.anchoredPosition) < 100f)
                    {
                        bridgeCreatureRt.anchoredPosition = bridgeTargetRt.anchoredPosition;
                        MgfSfx.Play("correct", .5f);
                        CompleteBridge();
                    }
                    else
                    {
                        bridgeCreatureRt.anchoredPosition = new Vector2(-110, 20);
                        RefuseAim("Q의 링 안까지 옮기시오");
                    }
                }
                else if (aimDragging)
                {
                    aimDragging = false;
                    st.dragging = false;
                    handle.rectTransform.localScale = Vector3.one;
                    if (draggedDistance < 18f) RefuseAim("포신을 한 눈금 이상 돌린 뒤 놓으시오");
                    else if (draggedTick < 1 || draggedTick > PopcornRules.TickCount) RefuseAim("5°~85° 눈금 안에서 놓으시오");
                    else SubmitTick(draggedTick);
                }
            }
        }

        bool Inside(RectTransform rt, Vector2 rootLocal)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 p0, p2;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, RectTransformUtility.WorldToScreenPoint(null, corners[0]), null, out p0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, RectTransformUtility.WorldToScreenPoint(null, corners[2]), null, out p2);
            return rootLocal.x >= p0.x && rootLocal.x <= p2.x && rootLocal.y >= p0.y && rootLocal.y <= p2.y;
        }

        void ShowRipple(Vector2 local)
        {
            rippleRt.anchoredPosition = local;
            rippleClock = 0f;
            ripple.color = new Color(1f, .86f, .32f, .9f);
        }

        internal void RefuseAim(string message)
        {
            refuseClock = 0f;
            toastText.text = message;
            toastText.color = Ivory;
            barrel.rectTransform.localScale = new Vector3(.90f, 1.08f, 1f);
            MgfSfx.Play("wrong", .18f);
        }

        void SetPhaseView()
        {
            bool title = phase == Phase.Title;
            bool game = phase == Phase.Practice || phase == Phase.Playing || phase == Phase.Feedback || phase == Phase.SameArcBridge || phase == Phase.DiameterBridge;
            bool result = phase == Phase.Clear || phase == Phase.GameOver;
            titleRoot.gameObject.SetActive(title);
            gameRoot.gameObject.SetActive(game);
            resultRoot.gameObject.SetActive(result);
            feedbackRoot.gameObject.SetActive(phase == Phase.Feedback);
            bridgeRoot.gameObject.SetActive(phase == Phase.SameArcBridge || phase == Phase.DiameterBridge);
            if (phase == Phase.Title) st.phase = "title";
            else if (phase == Phase.Practice) st.phase = "practice";
            else if (phase == Phase.Playing) st.phase = "playing";
            guideRing.gameObject.SetActive(phase == Phase.Practice);
            guideFinger.gameObject.SetActive(phase == Phase.Practice);
            guidePath.gameObject.SetActive(phase == Phase.Practice);
            guideArrow.gameObject.SetActive(phase == Phase.Practice);
            targetRay.gameObject.SetActive(false);
            projectile.gameObject.SetActive(false);
            for (int i = 0; i < trail.Length; i++) trail[i].gameObject.SetActive(false);
            if (titleArt) titleArt.gameObject.SetActive(title);
            if (titleCartonWorld) titleCartonWorld.gameObject.SetActive(title);
            for (int i = 0; i < worldPopcorn.Length; i++)
                if (worldPopcorn[i]) worldPopcorn[i].gameObject.SetActive(title || i % 3 == 0);
        }

        internal void UpdateProblemView()
        {
            if (current == null) return;
            promptText.text = current.prompt;
            relationText.text = current.band == 1 ? "중심각 = 원주각 × 2"
                : current.band == 2 ? "나뉜 중심각을 더한 뒤 ÷ 2"
                : "같은 원: 원주각은 호의 길이에 정비례";
        }

        void ResetAimForProblem(bool practice)
        {
            st.selectedTick = 0;
            draggedTick = 0;
            wrongTick = 0;
            aimDragging = false;
            st.dragging = false;
            angleText.text = "주차 0°";
            targetRay.gameObject.SetActive(false);
            UpdateAimVisual(0);
            if (practice) UpdateProblemView();
        }

        internal void UpdateAimVisual(int tick)
        {
            float angle = tick * 5f;
            aimDir = Rotate(refDir, -angle).normalized;
            bPoint = SecondCircleIntersection(pPoint, aimDir, circleCenter, circleRadius);
            PlaceLine(aimRay.rectTransform, pPoint, bPoint, 7f);
            PlaceLine(barrel.rectTransform, pPoint, pPoint + aimDir * Mathf.Min(76f, circleRadius * .52f), 24f);
            handleRt.anchoredPosition = pPoint + aimDir * Mathf.Min(74f, circleRadius * .51f);
            bDot.rectTransform.anchoredPosition = bPoint;
            bLabel.rectTransform.anchoredPosition = bPoint + new Vector2(18, 12);
            angleText.text = tick <= 0 ? "주차 0°" : "현재 " + angle.ToString("0") + "°";
        }

        Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
        }

        Vector2 SecondCircleIntersection(Vector2 p, Vector2 d, Vector2 center, float radius)
        {
            Vector2 q = p - center;
            float t = -2f * Vector2.Dot(q, d);
            if (t < 8f) t = radius * 1.2f;
            return p + d * t;
        }

        void PlaceLine(RectTransform rt, Vector2 start, Vector2 end, float thickness)
        {
            Vector2 d = end - start;
            rt.pivot = new Vector2(0f, .5f);
            rt.anchoredPosition = start;
            rt.sizeDelta = new Vector2(d.magnitude, thickness);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        void BeginShotFeedback(int tick, bool correct, bool practice, bool first)
        {
            feedbackRoot.gameObject.SetActive(true);
            feedbackTitle.text = correct ? (practice ? "관계 확인" : first ? "첫 발 적중" : "수리 완료") : "탄도를 비교하시오";
            int chosen = tick * 5;
            if (correct)
            {
                if (current.band == 1) feedbackMath.text = "중심각 " + current.centralAngle + "° = 2 × 원주각 " + chosen + "°";
                else if (current.band == 2) feedbackMath.text = "(" + current.splitU + "° + " + current.splitV + "°) ÷ 2 = " + chosen + "°";
                else feedbackMath.text = current.referenceTick * 5 + "° × " + current.targetArcLength + "/" + current.referenceArcLength + " = " + chosen + "°";
            }
            else feedbackMath.text = "실제 중심각 " + (chosen * 2) + "° / 목표 중심각 " + current.centralAngle + "°";
            feedbackTitle.color = correct ? Mint : Red;
            targetRay.gameObject.SetActive(!correct);
            if (!correct)
            {
                Vector2 targetDir = Rotate(refDir, -current.answerTick * 5f).normalized;
                Vector2 targetB = SecondCircleIntersection(pPoint, targetDir, circleCenter, circleRadius);
                PlaceLine(targetRay.rectTransform, pPoint, targetB, 4f);
            }
            projectile.gameObject.SetActive(true);
            for (int i = 0; i < trail.Length; i++) trail[i].gameObject.SetActive(correct);
            fxBeat = -1;
        }

        void ResetAllCartons()
        {
            for (int i = 0; i < cartonSlots.Length; i++)
            {
                cartonSlots[i].color = new Color(Ivory.r, Ivory.g, Ivory.b, .28f);
                cartonSlots[i].rectTransform.localScale = Vector3.one;
                if (i < worldSlots.Length && worldSlots[i]) worldSlots[i].localScale = Vector3.one;
            }
        }

        void ReplayGuide(bool boosted)
        {
            guideClock = 0f;
            guideBoost = boosted ? 1f : 0f;
            if (boosted) toastText.text = "빨간 손잡이를 40° 눈금까지 끌어 놓으시오";
        }

        void ShowSameArcBridge()
        {
            bridgeTitle.text = "같은 호, 같은 원주각";
            bridgeBody.text = "팝콘을 P에서 Q로 옮겨 보자";
            bridgeCreatureRt.anchoredPosition = new Vector2(-110, 20);
            bridgeTargetRt.anchoredPosition = new Vector2(110, 20);
            bridgeAngleP.text = "P   " + current.answerAngle + "°";
            bridgeAngleQ.text = "Q   " + current.answerAngle + "°";
            PlaceLine(bridgePath.rectTransform, new Vector2(-105, 20), new Vector2(105, 20), 7f);
            bridgeCreature.gameObject.SetActive(true);
            bridgeTarget.gameObject.SetActive(true);
            bridgePath.gameObject.SetActive(true);
            bridgeAngleP.gameObject.SetActive(true);
            bridgeAngleQ.gameObject.SetActive(true);
        }

        void ShowDiameterBridge()
        {
            bridgeTitle.text = "지름축 잠금";
            bridgeBody.text = "반원에 대한 원주각은 90°";
            bridgeCreature.gameObject.SetActive(false);
            bridgeTarget.gameObject.SetActive(false);
            bridgePath.gameObject.SetActive(true);
            bridgePath.color = Tin;
            PlaceLine(bridgePath.rectTransform, new Vector2(-135, 10), new Vector2(135, 10), 12f);
            bridgeAngleP.gameObject.SetActive(true);
            bridgeAngleQ.gameObject.SetActive(false);
            bridgeAngleP.text = "∠APB = 90°";
            bridgeAngleP.rectTransform.anchoredPosition = new Vector2(0, -45);
            bridgeAngleP.rectTransform.sizeDelta = new Vector2(250, 70);
        }

        void HideBridge()
        {
            bridgeRoot.gameObject.SetActive(false);
            bridgeDragging = false;
            st.dragging = false;
        }

        void ShowResult(bool clear, string reason)
        {
            resultTitle.text = clear ? "상자 완성" : "기계 정지";
            string reasonText = reason == "lives" ? "여분 팝콘이 모두 떨어졌다."
                : reason == "time" ? "90초가 끝났다."
                : reason == "mastery" ? "첫 발 6회를 더 이상 채울 수 없다."
                : "첫 발 기준까지 통과했다.";
            resultReason.text = reasonText + "\n다음 판에는 " + SuggestedRetry() + " 확인하시오.";
            shownResult = 0f;
            resultStats.text = "첫 발 0/8   완성 0/8\n점수 0";
        }

        string SuggestedRetry()
        {
            if (st.misconceptionId == "center_equals_inscribed") return "중심각을 2로 나누었는지";
            if (st.misconceptionId == "one_split_half_u" || st.misconceptionId == "one_split_half_v") return "두 중심각을 먼저 더했는지";
            if (st.misconceptionId == "arc_ratio_reversed") return "호의 길이 비의 순서를";
            return "손을 떼기 전 한 눈금을";
        }

        void RefreshUi(float dt)
        {
            int sec = Mathf.CeilToInt(remainingTime);
            if (sec != lastShownSecond || phase == Phase.Practice)
            {
                lastShownSecond = sec;
                hudText.text = phase == Phase.Practice ? "연습 · 여분 3 · 첫 발 0/8"
                    : "상자 " + st.solved + "/8   첫 발 " + st.firstAttemptCorrect + "/6   여분 " + st.lives + "   " + sec + "초";
            }
            shownScore = Mathf.Lerp(shownScore, st.score, 1f - Mathf.Exp(-dt * 10f));
            goalText.text = phase == Phase.Practice ? "포신 각을 맞춰 쏘면 상자가 찬다" : "첫 발 6회 이상으로 상자 8칸을 채우시오";
            for (int i = 0; i < lives.Length; i++) lives[i].color = i < st.lives ? Ivory : new Color(Ink.r, Ink.g, Ink.b, .18f);

            if (rippleClock < .55f)
            {
                rippleClock += dt;
                float t = rippleClock / .55f;
                rippleRt.localScale = Vector3.one * Mathf.Lerp(.55f, 2.2f, t);
                ripple.color = new Color(1f, .86f, .32f, (1f - t) * .78f);
            }
            if (refuseClock < .55f)
            {
                refuseClock += dt;
                float t = refuseClock / .55f;
                float shake = Mathf.Sin(t * Mathf.PI * 6f) * (1f - t) * 8f;
                barrel.rectTransform.localPosition = new Vector3(shake, 0, 0);
                toastText.color = new Color(Ivory.r, Ivory.g, Ivory.b, 1f - Mathf.Max(0f, t - .72f) / .28f);
            }
            else barrel.rectTransform.localPosition = Vector3.zero;

            if (phase == Phase.Clear || phase == Phase.GameOver)
            {
                shownResult = Mathf.MoveTowards(shownResult, st.firstAttemptCorrect, dt * 12f);
                int shownFirst = Mathf.Min(st.firstAttemptCorrect, Mathf.RoundToInt(shownResult));
                int shownSolved = st.firstAttemptCorrect == 0 ? Mathf.RoundToInt(Mathf.Lerp(0, st.solved, Mathf.Clamp01(phaseClock * 2f))) : st.solved;
                resultStats.text = "첫 발 " + shownFirst + "/8   완성 " + shownSolved + "/8\n점수 " + Mathf.RoundToInt(shownScore);
            }
        }

        void AnimateView(float dt)
        {
            worldClock += dt;
            for (int i = 0; i < worldPopcorn.Length; i++)
            {
                if (!worldPopcorn[i]) continue;
                Vector3 p = worldPopcorn[i].localPosition;
                p.y += Mathf.Sin(worldClock * 1.4f + i * .71f) * .0019f;
                worldPopcorn[i].localPosition = p;
                worldPopcorn[i].localRotation = Quaternion.Euler(0, Mathf.Sin(worldClock + i) * 5f, Mathf.Sin(worldClock * 1.3f + i) * 2f);
            }
            if (creatureRoot)
            {
                float breath = 1f + Mathf.Sin(worldClock * 2.2f) * .035f;
                creatureRoot.localScale = new Vector3(1f / breath, breath, 1f);
            }
            if (titleCartonWorld) titleCartonWorld.localRotation = Quaternion.Euler(-18f + Mathf.Sin(worldClock * .8f) * 1.5f, 0, -4f);

            if (phase == Phase.Title)
            {
                float pulse = 1f + Mathf.Sin(worldClock * 2.1f) * .025f;
                titleCta.localScale = Vector3.one * pulse;
                titleLogo.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(worldClock * .65f) * 1.2f);
            }

            if (phase == Phase.Practice)
            {
                guideClock += dt;
                float pulse = .5f + .5f * Mathf.Sin(guideClock * 5.5f);
                guideRingRt.anchoredPosition = circleRoot.anchoredPosition + handleRt.anchoredPosition;
                guideRingRt.localScale = Vector3.one * (1f + pulse * (.16f + guideBoost * .12f));
                guideRingRt.localRotation = Quaternion.Euler(0, 0, -guideClock * 90f);
                Vector2 from = circleRoot.anchoredPosition + pPoint + refDir * Mathf.Min(74f, circleRadius * .51f);
                Vector2 to = circleRoot.anchoredPosition + pPoint + Rotate(refDir, -40f) * Mathf.Min(74f, circleRadius * .51f);
                float loop = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(guideClock / 1.2f, 1f));
                guideFingerRt.anchoredPosition = Vector2.Lerp(from, to, loop);
                PlaceLine(guidePathRt, from, to, 5f);
                guideArrowRt.anchoredPosition = to + new Vector2(18, -10);
                guideArrowRt.localRotation = Quaternion.Euler(0, 0, -35f);
            }

            if (phase == Phase.Feedback) AnimateShotFeedback(dt);

            for (int i = 0; i < cartonSlots.Length; i++)
            {
                bool filled = i < st.solved;
                cartonSlots[i].color = filled ? Ivory : new Color(Ivory.r, Ivory.g, Ivory.b, .28f);
                float target = filled ? 1f : .86f;
                if (filled && i == st.solved - 1 && phase == Phase.Feedback && feedbackCorrect)
                {
                    float t = Mathf.Clamp01((feedbackClock - 1.15f) / .55f);
                    target = 1f + Mathf.Sin(t * Mathf.PI) * .22f;
                }
                cartonSlots[i].rectTransform.localScale = Vector3.Lerp(cartonSlots[i].rectTransform.localScale, Vector3.one * target, 1f - Mathf.Exp(-dt * 14f));
            }
        }

        void AnimateShotFeedback(float dt)
        {
            float t = feedbackCorrect ? Mathf.Clamp01(feedbackClock / 1.75f) : Mathf.Clamp01(feedbackClock / .8f);
            Vector2 start = circleRoot.anchoredPosition + pPoint;
            Vector2 hit = circleRoot.anchoredPosition + bPoint;
            Vector2 end = cartonRoot.anchoredPosition;
            Vector2 pos;
            if (feedbackCorrect)
            {
                Vector2 b0 = bumpers[0].rectTransform.anchoredPosition;
                Vector2 b1 = bumpers[1].rectTransform.anchoredPosition;
                Vector2 b2 = bumpers[2].rectTransform.anchoredPosition;
                if (t < .25f) pos = Vector2.Lerp(start, hit, EaseOut(t / .25f));
                else if (t < .46f) pos = Vector2.Lerp(hit, b0, EaseOut((t - .25f) / .21f));
                else if (t < .63f) pos = Vector2.Lerp(b0, b1, EaseOut((t - .46f) / .17f));
                else if (t < .79f) pos = Vector2.Lerp(b1, b2, EaseOut((t - .63f) / .16f));
                else pos = Vector2.Lerp(b2, end, EaseOut((t - .79f) / .21f));
                int beat = t < .46f ? -1 : t < .63f ? 0 : t < .79f ? 1 : 2;
                if (beat != fxBeat && beat >= 0)
                {
                    fxBeat = beat;
                    MgfSfx.Play("tap", .23f + beat * .04f);
                }
                for (int i = 0; i < bumpers.Length; i++)
                {
                    float bt = Mathf.Clamp01(1f - Mathf.Abs(t - (.51f + i * .17f)) * 9f);
                    bumpers[i].rectTransform.localScale = new Vector3(1f + bt * .22f, 1f - bt * .28f, 1f);
                    bumpers[i].color = Color.Lerp(i == 1 ? Red : Tin, ButterLight, bt);
                    if (worldBumpers[i]) worldBumpers[i].localScale = new Vector3(.85f, .30f * (1f - bt * .35f), .85f);
                }
            }
            else
            {
                Vector2 crumb = cartonRoot.anchoredPosition + new Vector2(-150, -35);
                pos = t < .62f ? Vector2.Lerp(start, hit, EaseOut(t / .62f)) : Vector2.Lerp(hit, crumb, (t - .62f) / .38f);
                float shake = Mathf.Sin(t * Mathf.PI * 8f) * (1f - t) * 7f;
                handleRt.localPosition = new Vector3(shake, 0, 0);
            }
            projectileRt.anchoredPosition = pos;
            float squash = Mathf.Sin(t * Mathf.PI * 5f) * .16f;
            projectileRt.localScale = new Vector3(1f + squash, 1f - squash, 1f);
            for (int i = 0; i < trail.Length; i++)
            {
                float lag = Mathf.Max(0f, t - (i + 1) * .018f);
                trail[i].rectTransform.anchoredPosition = Vector2.Lerp(trail[i].rectTransform.anchoredPosition, pos, 1f - Mathf.Exp(-dt * (16f - i)));
                trail[i].color = new Color(1f, .84f, .28f, Mathf.Clamp01(lag * 3f) * (.32f - i * .035f));
            }
        }

        float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return 1f - u * u * u;
        }

        void ApplyResponsiveLayout()
        {
            int sw = Screen.width, sh = Screen.height;
            if (sw == layoutW && sh == layoutH) return;
            layoutW = sw; layoutH = sh;
            layoutLand = (sw >= sh && sw >= 640) || sw >= 1024;
            Canvas.ForceUpdateCanvases();
            float w = uiRoot.rect.width, h = uiRoot.rect.height;

            if (titleArt)
            {
                titleArt.rectTransform.sizeDelta = Vector2.zero;
                titleArt.rectTransform.anchoredPosition = Vector2.zero;
            }
            titleLogo.rectTransform.anchoredPosition = layoutLand ? new Vector2(0, 145) : new Vector2(0, 190);
            titleLogo.fontSize = layoutLand ? 50 : 58;
            titleTag.rectTransform.anchoredPosition = layoutLand ? new Vector2(0, 45) : new Vector2(0, 80);
            titleCta.anchoredPosition = layoutLand ? new Vector2(0, -135) : new Vector2(0, -176);
            titleMeta.rectTransform.anchoredPosition = layoutLand ? new Vector2(0, -232) : new Vector2(0, -273);

            hudPlaque.sizeDelta = new Vector2(Mathf.Min(w - 18f, layoutLand ? 700f : 374f), 56f);
            hudPlaque.anchoredPosition = new Vector2(0, h * .5f - 36f);
            goalText.rectTransform.anchoredPosition = new Vector2(0, h * .5f - 75f);
            if (!layoutLand)
            {
                promptPlaque.anchoredPosition = new Vector2(0, h * .5f - 135f);
                promptPlaque.sizeDelta = new Vector2(w - 24f, 96f);
                promptText.rectTransform.sizeDelta = new Vector2(w - 50f, 88f);
                promptText.fontSize = 16.5f;
                circleCenter = new Vector2(0, 8f);
                circleRadius = Mathf.Min(150f, w * .39f);
                circleRoot.anchoredPosition = circleCenter;
                cartonRoot.anchoredPosition = new Vector2(0, -h * .5f + 74f);
                cartonRoot.sizeDelta = new Vector2(Mathf.Min(350f, w - 22f), 72f);
                for (int i = 0; i < bumpers.Length; i++) bumpers[i].rectTransform.anchoredPosition = new Vector2(78f + i * 42f, -115f - i * 19f);
                for (int i = 0; i < lives.Length; i++) lives[i].rectTransform.anchoredPosition = new Vector2(-w * .5f + 36f + i * 30f, -h * .5f + 125f);
                feedbackPlate.rectTransform.anchoredPosition = new Vector2(0, -110f);
                feedbackTitle.rectTransform.anchoredPosition = new Vector2(0, -85f);
                feedbackMath.rectTransform.anchoredPosition = new Vector2(0, -126f);
            }
            else
            {
                promptPlaque.anchoredPosition = new Vector2(-w * .5f + 165f, 0f);
                promptPlaque.sizeDelta = new Vector2(285f, 240f);
                promptText.rectTransform.sizeDelta = new Vector2(245f, 215f);
                promptText.fontSize = 17.5f;
                circleCenter = new Vector2(25f, -12f);
                circleRadius = Mathf.Min(196f, h * .245f);
                circleRoot.anchoredPosition = circleCenter;
                cartonRoot.anchoredPosition = new Vector2(w * .5f - 175f, -12f);
                cartonRoot.sizeDelta = new Vector2(300f, 96f);
                for (int i = 0; i < bumpers.Length; i++) bumpers[i].rectTransform.anchoredPosition = new Vector2(w * .5f - 300f + i * 56f, 105f - i * 58f);
                for (int i = 0; i < lives.Length; i++) lives[i].rectTransform.anchoredPosition = new Vector2(w * .5f - 260f + i * 34f, -120f);
                feedbackPlate.rectTransform.anchoredPosition = new Vector2(w * .5f - 175f, -125f);
                feedbackTitle.rectTransform.anchoredPosition = feedbackPlate.rectTransform.anchoredPosition + new Vector2(0, 22);
                feedbackMath.rectTransform.anchoredPosition = feedbackPlate.rectTransform.anchoredPosition + new Vector2(0, -19);
            }

            circleRoot.sizeDelta = Vector2.one * circleRadius * 2.18f;
            circleFace.rectTransform.sizeDelta = Vector2.one * circleRadius * 1.88f;
            circleRing.rectTransform.sizeDelta = Vector2.one * circleRadius * 2.04f;
            pPoint = circleRadius * new Vector2(Mathf.Cos(210f * Mathf.Deg2Rad), Mathf.Sin(210f * Mathf.Deg2Rad));
            aPoint = circleRadius * Vector2.up;
            refDir = (aPoint - pPoint).normalized;
            pDot.rectTransform.anchoredPosition = pPoint;
            aDot.rectTransform.anchoredPosition = aPoint;
            pLabel.rectTransform.anchoredPosition = pPoint + new Vector2(-18, -15);
            aLabel.rectTransform.anchoredPosition = aPoint + new Vector2(0, 18);
            PlaceLine(fixedRay.rectTransform, pPoint, aPoint, 5f);
            centerDot.rectTransform.anchoredPosition = Vector2.zero;
            centerLabel.rectTransform.anchoredPosition = new Vector2(20, -12);
            for (int i = 0; i < tickMarks.Length; i++)
            {
                Vector2 d = Rotate(refDir, -(i + 1) * 5f);
                tickMarks[i].rectTransform.anchoredPosition = pPoint + d * (circleRadius * .33f + (i % 5 == 4 ? 5f : 0f));
                tickMarks[i].rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                tickMarks[i].rectTransform.sizeDelta = new Vector2(i % 5 == 4 ? 16f : 10f, 3f);
            }
            angleText.rectTransform.anchoredPosition = new Vector2(0, -circleRadius * .63f);
            relationText.rectTransform.anchoredPosition = new Vector2(0, circleRadius * .57f);
            UpdateAimVisual(st.selectedTick);
            UpdateProblemView();

            toastText.rectTransform.anchoredPosition = new Vector2(0, -h * .5f + 122f);
            toastText.rectTransform.sizeDelta = new Vector2(Mathf.Min(w - 40f, 420f), 58f);
            Vector2 screenP = RectTransformUtility.WorldToScreenPoint(null, pDot.rectTransform.position);
            Vector2 screenRef = RectTransformUtility.WorldToScreenPoint(null, handleRt.position);
            st.pX = Mathf.RoundToInt(screenP.x);
            st.pY = Mathf.RoundToInt(screenP.y);
            st.refX = Mathf.RoundToInt(screenRef.x);
            st.refY = Mathf.RoundToInt(screenRef.y);
            st.screenW = sw;
            st.screenH = sh;
            st.layoutVersion++;
            layoutW = sw; layoutH = sh;
            if (cam) cam.orthographicSize = layoutLand ? 8.2f : 10.2f;
            MgfBridge.NotifyChanged();
        }
    }
}
