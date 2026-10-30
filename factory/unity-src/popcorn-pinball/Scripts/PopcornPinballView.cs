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
        static readonly Color GlassBlue = new Color32(39, 70, 74, 255);
        static readonly Color GlassShadow = new Color32(21, 39, 43, 255);
        static readonly Color Paper = new Color32(249, 226, 179, 255);

        Camera cam;
        Transform worldRoot, creatureRoot, titleCartonWorld, lowerTrayWorld;
        readonly Transform[] worldPopcorn = new Transform[18];
        readonly Transform[] worldBumpers = new Transform[3];
        readonly Transform[] worldSlots = new Transform[8];
        Material butterMat, butterLightMat, ivoryMat, redMat, inkMat, tinMat, glassMat, glassBackMat, paperMat;
        float worldClock;

        RectTransform uiRoot, titleRoot, gameRoot, feedbackRoot, bridgeRoot, resultRoot;
        CanvasGroup titleGroup, gameGroup, resultGroup;
        Image titleArt;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleStart;
        RectTransform titleCta;

        RectTransform hudPlaque, promptPlaque, circleRoot, cartonRoot, cartonLid;
        TextMeshProUGUI hudText, promptText, goalText, angleText, relationText, toastText;
        TextMeshProUGUI pLabel, aLabel, bLabel, centerLabel;
        Image circleFace, circleRing, fixedRay, aimRay, targetRay, barrel, handle, pDot, aDot, bDot, centerDot;
        readonly Image[] tickMarks = new Image[17];
        readonly Image[] lives = new Image[3];
        readonly Image[] cartonSlots = new Image[8];
        readonly Image[] bumpers = new Image[3];
        readonly Image[] trail = new Image[6];
        Image projectile, guideRing, guideFinger, guidePath, guideArrow, guideTargetRing, ripple;
        TextMeshProUGUI guideTargetLabel;
        RectTransform handleRt, projectileRt, guideRingRt, guideFingerRt, guidePathRt, guideArrowRt, guideTargetRingRt, rippleRt;

        TextMeshProUGUI feedbackTitle, feedbackMath;
        Image feedbackPlate;
        TextMeshProUGUI bridgeTitle, bridgeBody, bridgeAngleP, bridgeAngleQ;
        Image bridgeCreature, bridgeTarget, bridgePath;
        RectTransform bridgeCreatureRt, bridgeTargetRt;
        bool bridgeDragging;

        TextMeshProUGUI resultTitle, resultStats, resultReason, resultRestart;
        RectTransform restartCta;

        Sprite plateSprite, ringSprite, thinRingSprite, discSprite, faceSprite, arrowSprite, stripeSprite;
        Vector2 circleCenter;
        float circleRadius;
        Vector2 pPoint, aPoint, bPoint, refDir, aimDir;
        Vector2 dragStart;
        bool aimDragging;
        bool aimObservedHeld;
        float draggedDistance;
        int draggedTick;
        float guideClock, guideBoost;
        float rippleClock = 20f, refuseClock = 20f;
        int fxBeat = -1;
        int lastShownSecond = -1;
        float shownScore, shownResult;
        int layoutW = -1, layoutH = -1, screenPxW = -1, screenPxH = -1;
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
            tinMat = MgfLook.Lit(Tin, .82f, .34f);
            paperMat = MgfLook.Lit(Paper, .28f, 0f);
            glassBackMat = MgfLook.Lit(GlassBlue, .58f, .18f);
            glassMat = MgfLook.Alpha(new Color(.72f, .92f, .90f, .17f), MgfLook.SoftDot);

            MgfLook.Sky(new Color32(28, 51, 55, 255), new Color32(52, 86, 88, 255), new Color32(17, 31, 35, 255), .72f);
            MgfLook.Sun(new Vector3(38f, -28f, 20f), new Color(1f, .88f, .62f), 1.26f, .54f);
            cam = MgfLook.Camera(new Vector3(0f, 10.4f, -17.2f), new Vector3(0f, 1.4f, 2.7f), 36f);
            cam.orthographic = true;
            cam.orthographicSize = 9.4f;
            cam.backgroundColor = GlassShadow;

            worldRoot = new GameObject("Vertical glass cutaway popcorn machine").transform;
            MgfLook.Block("Deep glass service cavity", new Vector3(0, 1.5f, 6.0f), new Vector3(24f, 18f, 1.0f), .30f, glassBackMat, worldRoot);
            MgfLook.Block("Left folded butter shell", new Vector3(-9.2f, 1.5f, 3.7f), new Vector3(2.1f, 18f, 3.8f), .24f, butterMat, worldRoot);
            MgfLook.Block("Right folded butter shell", new Vector3(9.2f, 1.5f, 3.7f), new Vector3(2.1f, 18f, 3.8f), .24f, butterMat, worldRoot);
            MgfLook.Block("Top folded butter shell", new Vector3(0, 8.0f, 3.7f), new Vector3(18.4f, 1.1f, 3.8f), .22f, butterLightMat, worldRoot);
            lowerTrayWorld = MgfLook.Block("Lower folded paper tray", new Vector3(3.4f, -5.0f, 2.0f), new Vector3(11.0f, 1.0f, 5.8f), .12f, paperMat, worldRoot).transform;

            // 얇은 틴 프레임과 투명 유리 칸막이가 화면을 세로 컷어웨이로 읽히게 한다.
            for (int i = 0; i < 5; i++)
            {
                float x = -7.2f + i * 3.6f;
                MgfLook.Block("Brushed tin mullion " + i, new Vector3(x, 1.5f, 4.9f),
                    new Vector3(.18f, 14.0f, .22f), .035f, tinMat, worldRoot);
                MgfLook.Block("Glass channel " + i, new Vector3(x + 1.65f, 1.5f, 4.7f),
                    new Vector3(3.1f, 13.5f, .08f), .02f, glassMat, worldRoot);
                for (int b = 0; b < 3; b++)
                    MgfLook.Prim(PrimitiveType.Sphere, "Tin rivet", new Vector3(x, -3.4f + b * 4.9f, 4.55f),
                        new Vector3(.22f, .22f, .13f), tinMat, worldRoot, false);
            }
            // 세로 화면에서도 버터 노랑 기계 케이스가 사라지지 않도록 유리 안쪽에 얇은 접힌 채널을 둔다.
            // 어두운 패널을 더하지 않고, 조준실과 자판기 외장을 같은 물성으로 잇는 역할이다.
            MgfLook.Block("Left inner butter guide", new Vector3(-4.25f, 1.5f, 4.72f),
                new Vector3(.44f, 14.2f, .32f), .055f, butterLightMat, worldRoot);
            MgfLook.Block("Right inner butter guide", new Vector3(4.25f, 1.5f, 4.72f),
                new Vector3(.44f, 14.2f, .32f), .055f, butterLightMat, worldRoot);

            // 팝콘은 상단 호퍼에서 한 줄로 대기한다. 평평한 작업대 위 산개를 피한다.
            for (int i = 0; i < worldPopcorn.Length; i++)
            {
                var root = new GameObject("Cached popcorn cluster " + i).transform;
                root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(-6.8f + (i % 3) * .66f, 6.0f - (i / 3) * .72f, 3.5f + (i % 2) * .12f);
                for (int k = 0; k < 3; k++)
                    MgfLook.Prim(PrimitiveType.Sphere, "Ivory lobe", new Vector3((k - 1) * .24f, Mathf.Abs(k - 1) * .10f, 0),
                        new Vector3(.54f, .46f, .40f), ivoryMat, root, false);
                MgfLook.Prim(PrimitiveType.Sphere, "Kernel foot", new Vector3(0, -.30f, 0), new Vector3(.22f, .17f, .18f), inkMat, root, false);
                worldPopcorn[i] = root;
            }
            for (int i = 0; i < worldBumpers.Length; i++)
            {
                var fold = MgfLook.Block("Paper fold hinge " + i,
                    new Vector3(3.2f + i * 1.55f, -2.8f + i * .16f, 2.5f), new Vector3(1.25f, .14f, 1.7f),
                    .035f, i == 1 ? redMat : tinMat, worldRoot);
                fold.transform.localRotation = Quaternion.Euler(-8f, i * 4f, -20f + i * 20f);
                worldBumpers[i] = fold.transform;
            }
            for (int i = 0; i < worldSlots.Length; i++)
            {
                int col = i % 4, row = i / 4;
                var slot = MgfLook.Block("Folded paper chamber " + (i + 1), new Vector3(.8f + col * 1.45f, -4.35f + row * .72f, 1.2f),
                    new Vector3(1.18f, .58f, .82f), .055f, i % 2 == 0 ? redMat : paperMat, worldRoot);
                worldSlots[i] = slot.transform;
            }
            titleCartonWorld = MgfLook.Block("Opened folded title carton", new Vector3(2.8f, -2.0f, 1.6f), new Vector3(7.8f, 2.4f, .30f), .08f, paperMat, worldRoot).transform;
            titleCartonWorld.localRotation = Quaternion.Euler(-12f, 0, 3f);
            creatureRoot = MakeCreature("Waiting popcorn creature", new Vector3(-4.7f, .3f, 1.0f), worldRoot);
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
            thinRingSprite = MakeRingSprite(.88f);
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
            // 생성 키아트 대신 최종 런타임 컷어웨이 세계를 그대로 타이틀로 쓴다.
            titleLogo = Txt("Carton lid logo", titleRoot, "팝콘\n핀볼", new Vector2(0, 185), 58, Paper, 330);
            titleLogo.outlineColor = GlassShadow;
            titleLogo.outlineWidth = .24f;
            titleTag = Txt("Printed carton tagline", titleRoot, "포신을 돌려 쏴라", new Vector2(0, 80), 24, Paper, 310);
            titleTag.outlineColor = GlassShadow;
            titleTag.outlineWidth = .16f;
            titleCta = R("Start carton", titleRoot, new Vector2(.5f, .5f), new Vector2(0, -176), new Vector2(286, 94));
            var ctaImg = titleCta.gameObject.AddComponent<Image>(); ctaImg.sprite = stripeSprite; ctaImg.type = Image.Type.Sliced; ctaImg.color = Red; ctaImg.raycastTarget = false;
            titleStart = Txt("Start carton print", titleCta, "상자를 눌러 시작", Vector2.zero, 24, Ivory, 250);
            titleMeta = Txt("Machine product label", titleRoot, "중3 · 원주각   유리 안의 호를 접어 상자를 완성", new Vector2(0, -273), 14, Paper, 350);
            titleMeta.outlineColor = GlassShadow;
            titleMeta.outlineWidth = .12f;

            gameRoot = Full("Game overlay", uiRoot);
            gameGroup = gameRoot.gameObject.AddComponent<CanvasGroup>();
            hudPlaque = R("Top rolled HUD", gameRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.one);
            var hudBg = hudPlaque.gameObject.AddComponent<Image>(); hudBg.sprite = plateSprite; hudBg.type = Image.Type.Sliced; hudBg.color = new Color(GlassShadow.r, GlassShadow.g, GlassShadow.b, .90f); hudBg.raycastTarget = false;
            hudText = Txt("HUD engraved text", hudPlaque, "", Vector2.zero, 16, Paper, 355);
            goalText = Txt("Persistent goal line", gameRoot, "포신 각을 맞춰 쏘면 상자가 찬다", Vector2.zero, 17, Red, 360);

            promptPlaque = R("Bright target nameplate", gameRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.one);
            var promptBg = promptPlaque.gameObject.AddComponent<Image>(); promptBg.sprite = plateSprite; promptBg.type = Image.Type.Sliced; promptBg.color = new Color(GlassShadow.r, GlassShadow.g, GlassShadow.b, .90f); promptBg.raycastTarget = false;
            promptText = Txt("Target condition", promptPlaque, "", Vector2.zero, 18, Paper, 340);

            circleRoot = R("Front orthographic aiming chamber", gameRoot, new Vector2(.5f, .5f), Vector2.zero, new Vector2(320, 320));
            circleFace = Img("Printed teal aiming face", circleRoot, Vector2.zero, new Vector2(300, 300), new Color(.16f, .34f, .35f, .98f), faceSprite);
            circleRing = Img("Thin brushed tin circular rim", circleRoot, Vector2.zero, new Vector2(310, 310), new Color32(232, 220, 185, 255), thinRingSprite);
            centerDot = Img("Center O", circleRoot, Vector2.zero, new Vector2(18, 18), Paper, discSprite);
            centerLabel = Txt("Center O label", circleRoot, "O", new Vector2(18, -12), 14, Paper, 30);
            fixedRay = Img("Fixed line PA", circleRoot, Vector2.zero, new Vector2(10, 5), Tin, discSprite);
            aimRay = Img("Current ray PB", circleRoot, Vector2.zero, new Vector2(10, 7), Red, discSprite);
            targetRay = Img("Revealed target ray", circleRoot, Vector2.zero, new Vector2(10, 4), Mint, discSprite);
            barrel = Img("Butter enamel movable barrel", circleRoot, Vector2.zero, new Vector2(72, 24), ButterLight, plateSprite);
            handle = Img("Cherry red barrel handle", circleRoot, Vector2.zero, new Vector2(58, 58), Red, ringSprite);
            handleRt = handle.rectTransform;
            pDot = Img("Point P", circleRoot, Vector2.zero, new Vector2(24, 24), Red, discSprite);
            aDot = Img("Point A", circleRoot, Vector2.zero, new Vector2(20, 20), Ink, discSprite);
            bDot = Img("Current point B question", circleRoot, Vector2.zero, new Vector2(20, 20), Tin, discSprite);
            pLabel = Txt("P label", circleRoot, "P", Vector2.zero, 15, Paper, 28);
            aLabel = Txt("A label", circleRoot, "A", Vector2.zero, 15, Paper, 28);
            bLabel = Txt("B question label", circleRoot, "B?", Vector2.zero, 15, Paper, 38);
            for (int i = 0; i < tickMarks.Length; i++)
                tickMarks[i] = Img("Printed 5 degree tick " + (i + 1), circleRoot, Vector2.zero,
                    new Vector2(i % 5 == 4 ? 20 : 14, 4), i % 5 == 4 ? Red : ButterLight, discSprite);
            angleText = Txt("Magnified selected angle", circleRoot, "주차 0°", new Vector2(0, -116), 24, Paper, 175);
            relationText = Txt("Visible relation", circleRoot, "중심각 = 원주각 × 2", new Vector2(0, 118), 15, Paper, 230);

            for (int i = 0; i < bumpers.Length; i++)
                bumpers[i] = Img("Metal bumper UI " + i, gameRoot, Vector2.zero, new Vector2(42, 42), i == 1 ? Red : Tin, ringSprite);
            projectile = Img("Flying popcorn kernel", gameRoot, Vector2.zero, new Vector2(30, 30), Ivory, discSprite);
            projectileRt = projectile.rectTransform;
            for (int i = 0; i < trail.Length; i++)
                trail[i] = Img("Butter flight trail " + i, gameRoot, Vector2.zero, new Vector2(18 - i * 2, 18 - i * 2), new Color(1f, .86f, .35f, .25f), discSprite);

            cartonRoot = R("Eight chamber folded paper carton", gameRoot, new Vector2(.5f, .5f), Vector2.zero, new Vector2(350, 110));
            var cartonBg = cartonRoot.gameObject.AddComponent<Image>(); cartonBg.sprite = plateSprite; cartonBg.type = Image.Type.Sliced; cartonBg.color = Paper; cartonBg.raycastTarget = false;
            cartonLid = R("Pop-up carton lid", cartonRoot, new Vector2(.5f, .5f), new Vector2(0, 54), new Vector2(260, 38));
            var lidBg = cartonLid.gameObject.AddComponent<Image>(); lidBg.sprite = stripeSprite; lidBg.type = Image.Type.Sliced; lidBg.color = Red; lidBg.raycastTarget = false;
            for (int i = 0; i < cartonSlots.Length; i++)
                cartonSlots[i] = Img("Folded carton chamber " + (i + 1), cartonRoot, Vector2.zero, new Vector2(38, 30), new Color(Red.r, Red.g, Red.b, .22f), plateSprite);
            for (int i = 0; i < lives.Length; i++)
                lives[i] = Img("Spare popcorn " + (i + 1), gameRoot, Vector2.zero, new Vector2(26, 26), Ivory, discSprite);

            toastText = Txt("Invalid input reason", gameRoot, "", Vector2.zero, 16, Ivory, 310);
            ripple = Img("Input butter ripple", gameRoot, Vector2.zero, new Vector2(36, 36), new Color(1f, .86f, .32f, 0f), ringSprite);
            rippleRt = ripple.rectTransform;
            guidePath = Img("Ghost drag path", gameRoot, Vector2.zero, new Vector2(120, 5), new Color(1f, .96f, .77f, .72f), discSprite);
            guidePathRt = guidePath.rectTransform;
            guideArrow = Img("Ghost destination arrow", gameRoot, Vector2.zero, new Vector2(44, 44), Red, arrowSprite);
            guideArrowRt = guideArrow.rectTransform;
            guideTargetRing = Img("Practice forty degree target ring", gameRoot, Vector2.zero, new Vector2(94, 94), Mint, ringSprite);
            guideTargetRingRt = guideTargetRing.rectTransform;
            guideTargetLabel = Txt("Practice forty degree target label", gameRoot, "목표 40°", Vector2.zero, 19, Paper, 118);
            guideTargetLabel.outlineColor = GlassShadow;
            guideTargetLabel.outlineWidth = .18f;
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
                // 모바일 터치 에뮬레이션에서 release가 캔버스 밖으로 빠졌다면 다음 탭을 이전 drag의 놓기로 회복한다.
                // 이때도 학생이 마지막으로 만든 눈금을 그대로 제출한다. 정답 눈금으로 바꾸면 학습 판정이 거짓이 된다.
                if (phase == Phase.Practice && aimDragging && st.selectedTick > 0)
                {
                    draggedTick = st.selectedTick;
                    FinishAimDrag();
                    return;
                }
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
                float grabRadius = phase == Phase.Practice ? 138f : 105f;
                bool practiceChamber = phase == Phase.Practice && chamberLocal.magnitude <= circleRadius * 1.12f;
                if (practiceChamber || Vector2.Distance(chamberLocal, handleRt.anchoredPosition) < grabRadius || Vector2.Distance(chamberLocal, pPoint) < grabRadius)
                {
                    aimDragging = true;
                    st.dragging = true;
                    dragStart = chamberLocal;
                    draggedDistance = 0f;
                    draggedTick = 0;
                    aimObservedHeld = false;
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
                    aimObservedHeld = true;
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

            bool recoveredRelease = aimDragging && aimObservedHeld && !MgfPointer.Held && !MgfPointer.Up;
            if (MgfPointer.Up || recoveredRelease)
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
                    FinishAimDrag();
                }
            }
        }

        void FinishAimDrag()
        {
            aimDragging = false;
            aimObservedHeld = false;
            st.dragging = false;
            handle.rectTransform.localScale = Vector3.one;
            int releaseTick = draggedTick > 0 ? draggedTick : st.selectedTick;
            if (draggedDistance < 18f) RefuseAim("포신을 한 눈금 이상 돌린 뒤 놓으시오");
            else if (releaseTick < 1 || releaseTick > PopcornRules.TickCount) RefuseAim("5°~85° 눈금 안에서 놓으시오");
            else
            {
                SetAimTick(releaseTick, true);
                SubmitTick(releaseTick);
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
            guideTargetRing.gameObject.SetActive(phase == Phase.Practice);
            guideTargetLabel.gameObject.SetActive(phase == Phase.Practice);
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
                : "같은 작은 호 AB에 대한 원주각은 같다";
        }

        void ResetAimForProblem(bool practice)
        {
            st.selectedTick = 0;
            draggedTick = 0;
            wrongTick = 0;
            aimDragging = false;
            aimObservedHeld = false;
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
            Vector2 labelOut = bPoint.sqrMagnitude > 1f ? bPoint.normalized * 22f : new Vector2(22f, 12f);
            bool nearA = Vector2.Distance(bPoint, aPoint) < circleRadius * .40f;
            bLabel.rectTransform.anchoredPosition = bPoint + labelOut + (nearA ? new Vector2(30f, -22f) : new Vector2(8f, 4f));
            aLabel.rectTransform.anchoredPosition = aPoint + (nearA ? new Vector2(-25f, 18f) : new Vector2(0f, 18f));
            for (int i = 0; i < tickMarks.Length; i++)
            {
                bool selected = tick == i + 1;
                tickMarks[i].color = selected ? Red : (i % 5 == 4 ? Paper : ButterLight);
                tickMarks[i].rectTransform.localScale = selected ? new Vector3(1.25f, 1.45f, 1f) : Vector3.one;
            }
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
                else feedbackMath.text = "같은 작은 호 AB → ∠AQB = ∠APB = " + chosen + "°";
            }
            else if (practice)
                feedbackMath.text = "선택 " + chosen + "° → 중심각 " + (chosen * 2) + "°\n목표 "
                    + current.answerAngle + "° → 중심각 " + current.centralAngle + "°";
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
                cartonSlots[i].color = new Color(Red.r, Red.g, Red.b, .22f);
                cartonSlots[i].rectTransform.localScale = Vector3.one;
                if (i < worldSlots.Length && worldSlots[i]) worldSlots[i].localScale = Vector3.one;
            }
            if (cartonLid) cartonLid.localScale = new Vector3(1f, .18f, 1f);
        }

        void ReplayGuide(bool boosted)
        {
            guideClock = 0f;
            guideBoost = boosted ? 1f : 0f;
            toastText.text = boosted ? "현재 손잡이에서 초록 40° 링까지 끌어 놓으시오" : "초록 40° 링까지 직접 끌어 놓으시오";
            refuseClock = 0f;
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
            if (st.misconceptionId == "same_arc_changed") return "같은 작은 호인지";
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
            goalText.text = phase == Phase.Practice ? "초록 40° 링까지 손잡이를 끌어 놓으시오" : "첫 발 6회 이상으로 상자 8칸을 채우시오";
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
                Vector2 from = circleRoot.anchoredPosition + handleRt.anchoredPosition;
                Vector2 to = circleRoot.anchoredPosition + pPoint + Rotate(refDir, -40f) * Mathf.Min(74f, circleRadius * .51f);
                float loop = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(guideClock / 1.2f, 1f));
                guideFingerRt.anchoredPosition = Vector2.Lerp(from, to, loop);
                PlaceLine(guidePathRt, from, to, 5f);
                guideArrowRt.anchoredPosition = to + new Vector2(18, -10);
                guideArrowRt.localRotation = Quaternion.Euler(0, 0, -35f);
                guideTargetRingRt.anchoredPosition = to;
                guideTargetRingRt.localScale = Vector3.one * (1f + pulse * .10f);
                guideTargetLabel.rectTransform.anchoredPosition = to + new Vector2(0, 62f);
            }

            if (phase == Phase.Feedback) AnimateShotFeedback(dt);

            for (int i = 0; i < cartonSlots.Length; i++)
            {
                bool filled = i < st.solved;
                cartonSlots[i].color = filled ? Red : new Color(Red.r, Red.g, Red.b, .22f);
                float target = filled ? 1f : .86f;
                if (filled && i == st.solved - 1 && phase == Phase.Feedback && feedbackCorrect)
                {
                    float t = Mathf.Clamp01((feedbackClock - 1.15f) / .55f);
                    target = 1f + Mathf.Sin(t * Mathf.PI) * .22f;
                }
                cartonSlots[i].rectTransform.localScale = Vector3.Lerp(cartonSlots[i].rectTransform.localScale, Vector3.one * target, 1f - Mathf.Exp(-dt * 14f));
            }
            if (cartonLid)
            {
                float lid = phase == Phase.Feedback && feedbackCorrect
                    ? Mathf.SmoothStep(.18f, 1f, Mathf.Clamp01((feedbackClock - .55f) / .72f))
                    : st.solved > 0 ? .72f : .18f;
                cartonLid.localScale = Vector3.Lerp(cartonLid.localScale, new Vector3(1f, lid, 1f), 1f - Mathf.Exp(-dt * 12f));
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
                if (t < .34f) pos = Vector2.Lerp(start, hit, EaseOut(t / .34f));
                else
                {
                    float u = EaseOut((t - .34f) / .66f);
                    Vector2 control = new Vector2((hit.x + end.x) * .5f, Mathf.Max(hit.y, end.y) + 78f);
                    pos = (1f - u) * (1f - u) * hit + 2f * (1f - u) * u * control + u * u * end;
                }
                int beat = t < .58f ? -1 : t < .76f ? 0 : 1;
                if (beat != fxBeat && beat >= 0) { fxBeat = beat; MgfSfx.Play(beat == 0 ? "whoosh" : "pop", .30f); }
                for (int i = 0; i < bumpers.Length; i++)
                {
                    float bt = Mathf.Clamp01((t - .58f - i * .035f) * 5.5f);
                    float fold = Mathf.Sin(bt * Mathf.PI);
                    bumpers[i].rectTransform.localScale = new Vector3(1f + fold * .12f, .25f + bt * .75f, 1f);
                    bumpers[i].color = Color.Lerp(Tin, Red, bt * .72f);
                    if (worldBumpers[i]) worldBumpers[i].localRotation = Quaternion.Euler(-8f + bt * 24f, i * 4f, -20f + i * 20f);
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

        void LayoutCartonSlots(bool land)
        {
            if (land)
            {
                float x = cartonRoot.sizeDelta.x * .22f;
                float y0 = cartonRoot.sizeDelta.y * .27f;
                for (int i = 0; i < cartonSlots.Length; i++)
                {
                    int col = i % 2, row = i / 2;
                    cartonSlots[i].rectTransform.anchoredPosition = new Vector2(col == 0 ? -x : x, y0 - row * 42f);
                    cartonSlots[i].rectTransform.sizeDelta = new Vector2(Mathf.Min(58f, cartonRoot.sizeDelta.x * .28f), 31f);
                }
                cartonLid.anchoredPosition = new Vector2(0, cartonRoot.sizeDelta.y * .5f + 16f);
                cartonLid.sizeDelta = new Vector2(cartonRoot.sizeDelta.x * .82f, 38f);
            }
            else
            {
                float dx = Mathf.Min(70f, cartonRoot.sizeDelta.x * .205f);
                for (int i = 0; i < cartonSlots.Length; i++)
                {
                    int col = i % 4, row = i / 4;
                    cartonSlots[i].rectTransform.anchoredPosition = new Vector2((col - 1.5f) * dx, 17f - row * 37f);
                    cartonSlots[i].rectTransform.sizeDelta = new Vector2(dx - 9f, 30f);
                }
                cartonLid.anchoredPosition = new Vector2(0, cartonRoot.sizeDelta.y * .5f + 13f);
                cartonLid.sizeDelta = new Vector2(cartonRoot.sizeDelta.x * .72f, 34f);
            }
        }

        void ApplyResponsiveLayout()
        {
            Canvas.ForceUpdateCanvases();
            float w = uiRoot.rect.width, h = uiRoot.rect.height;
            int logicalW = Mathf.RoundToInt(w), logicalH = Mathf.RoundToInt(h);
            int sw = Screen.width, sh = Screen.height;
            if (logicalW == layoutW && logicalH == layoutH && sw == screenPxW && sh == screenPxH) return;
            layoutW = logicalW; layoutH = logicalH; screenPxW = sw; screenPxH = sh;

            // Unity WebGL의 Screen.width는 DPR이 곱해진 물리 픽셀일 수 있다. 판형은 Canvas 논리 사각형으로만 고른다.
            layoutLand = w / Mathf.Max(1f, h) >= 1.12f && w >= 700f;

            // 세로에서는 화면 밖으로 잘리던 오른쪽 3D 복제 장치를 숨기고, 실제 조작 상자를 안전영역 안 전경으로 통합한다.
            if (lowerTrayWorld) lowerTrayWorld.gameObject.SetActive(layoutLand);
            for (int i = 0; i < worldBumpers.Length; i++) if (worldBumpers[i]) worldBumpers[i].gameObject.SetActive(layoutLand);
            for (int i = 0; i < worldSlots.Length; i++) if (worldSlots[i]) worldSlots[i].gameObject.SetActive(layoutLand);

            titleLogo.rectTransform.anchoredPosition = layoutLand ? new Vector2(0, 120) : new Vector2(0, 178);
            titleLogo.fontSize = layoutLand ? 48 : 56;
            titleTag.rectTransform.anchoredPosition = layoutLand ? new Vector2(0, 30) : new Vector2(0, 72);
            titleCta.anchoredPosition = layoutLand ? new Vector2(0, -112) : new Vector2(0, -170);
            titleMeta.rectTransform.anchoredPosition = layoutLand ? new Vector2(0, -205) : new Vector2(0, -267);

            hudPlaque.sizeDelta = new Vector2(Mathf.Min(w - 24f, layoutLand ? 720f : 374f), 54f);
            hudPlaque.anchoredPosition = new Vector2(0, h * .5f - 35f);
            goalText.rectTransform.anchoredPosition = new Vector2(0, h * .5f - 76f);

            if (!layoutLand)
            {
                promptPlaque.anchoredPosition = new Vector2(0, h * .5f - 139f);
                promptPlaque.sizeDelta = new Vector2(w - 26f, 100f);
                promptText.rectTransform.sizeDelta = new Vector2(w - 54f, 91f);
                promptText.fontSize = Mathf.Clamp(w * .043f, 15.5f, 18f);

                circleRadius = Mathf.Min(154f, w * .34f, h * .205f);
                circleCenter = new Vector2(0, -8f);
                circleRoot.anchoredPosition = circleCenter;

                cartonRoot.anchoredPosition = new Vector2(0, -h * .5f + 79f);
                cartonRoot.sizeDelta = new Vector2(Mathf.Min(360f, w - 28f), 104f);
                LayoutCartonSlots(false);
                for (int i = 0; i < bumpers.Length; i++)
                {
                    bumpers[i].rectTransform.anchoredPosition = cartonRoot.anchoredPosition + new Vector2(-38f + i * 38f, 72f);
                    bumpers[i].rectTransform.sizeDelta = new Vector2(29f, 22f);
                }
                for (int i = 0; i < lives.Length; i++) lives[i].rectTransform.anchoredPosition = new Vector2(-w * .5f + 34f + i * 29f, -h * .5f + 145f);
                feedbackPlate.rectTransform.anchoredPosition = new Vector2(0, circleCenter.y - circleRadius * .72f);
                feedbackPlate.rectTransform.sizeDelta = new Vector2(Mathf.Min(330f, w - 38f), 100f);
                feedbackTitle.rectTransform.anchoredPosition = feedbackPlate.rectTransform.anchoredPosition + new Vector2(0, 21f);
                feedbackMath.rectTransform.anchoredPosition = feedbackPlate.rectTransform.anchoredPosition + new Vector2(0, -20f);
            }
            else
            {
                const float margin = 22f, gap = 20f;
                float leftW = Mathf.Clamp(w * .25f, 230f, 315f);
                float rightW = Mathf.Clamp(w * .20f, 190f, 270f);
                float centerW = Mathf.Max(236f, w - margin * 2f - leftW - rightW - gap * 2f);
                float leftX = -w * .5f + margin + leftW * .5f;
                float centerX = -w * .5f + margin + leftW + gap + centerW * .5f;
                float rightX = w * .5f - margin - rightW * .5f;
                float zoneY = -12f;

                promptPlaque.anchoredPosition = new Vector2(leftX, zoneY);
                promptPlaque.sizeDelta = new Vector2(leftW, Mathf.Min(252f, h - 178f));
                promptText.rectTransform.sizeDelta = promptPlaque.sizeDelta - new Vector2(28f, 28f);
                promptText.fontSize = Mathf.Clamp(leftW * .060f, 15.5f, 18.5f);

                circleRadius = Mathf.Min(centerW / 2.18f * .93f, (h - 162f) / 2.18f * .93f, 188f);
                circleCenter = new Vector2(centerX, zoneY);
                circleRoot.anchoredPosition = circleCenter;

                cartonRoot.anchoredPosition = new Vector2(rightX, zoneY - 3f);
                cartonRoot.sizeDelta = new Vector2(rightW, Mathf.Min(238f, h - 186f));
                LayoutCartonSlots(true);
                for (int i = 0; i < bumpers.Length; i++)
                {
                    bumpers[i].rectTransform.anchoredPosition = new Vector2(rightX + (i - 1) * rightW * .24f, zoneY + cartonRoot.sizeDelta.y * .5f + 24f);
                    bumpers[i].rectTransform.sizeDelta = new Vector2(31f, 25f);
                }
                for (int i = 0; i < lives.Length; i++) lives[i].rectTransform.anchoredPosition = new Vector2(rightX - 31f + i * 31f, -h * .5f + 40f);
                feedbackPlate.rectTransform.anchoredPosition = new Vector2(rightX, zoneY - cartonRoot.sizeDelta.y * .30f);
                feedbackPlate.rectTransform.sizeDelta = new Vector2(rightW - 12f, 94f);
                feedbackTitle.rectTransform.anchoredPosition = feedbackPlate.rectTransform.anchoredPosition + new Vector2(0, 20f);
                feedbackMath.rectTransform.anchoredPosition = feedbackPlate.rectTransform.anchoredPosition + new Vector2(0, -19f);
                feedbackTitle.rectTransform.sizeDelta = new Vector2(rightW - 24f, 56f);
                feedbackMath.rectTransform.sizeDelta = new Vector2(rightW - 24f, 62f);
                feedbackTitle.fontSize = Mathf.Min(18f, rightW * .074f);
                feedbackMath.fontSize = Mathf.Min(16f, rightW * .066f);
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
                tickMarks[i].rectTransform.anchoredPosition = pPoint + d * (circleRadius * .39f + (i % 5 == 4 ? 6f : 0f));
                tickMarks[i].rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                tickMarks[i].rectTransform.sizeDelta = new Vector2(i % 5 == 4 ? 20f : 14f, 4f);
            }
            angleText.rectTransform.anchoredPosition = new Vector2(0, -circleRadius * .63f);
            relationText.rectTransform.anchoredPosition = new Vector2(0, circleRadius * .57f);
            UpdateAimVisual(st.selectedTick);
            UpdateProblemView();

            toastText.rectTransform.anchoredPosition = layoutLand ? new Vector2(circleCenter.x, -h * .5f + 43f) : new Vector2(0, -h * .5f + 148f);
            toastText.rectTransform.sizeDelta = new Vector2(Mathf.Min(layoutLand ? 360f : w - 42f, 420f), 58f);
            Vector2 screenP = RectTransformUtility.WorldToScreenPoint(null, pDot.rectTransform.position);
            Vector2 screenRef = RectTransformUtility.WorldToScreenPoint(null, handleRt.position);
            st.pX = Mathf.RoundToInt(screenP.x);
            st.pY = Mathf.RoundToInt(screenP.y);
            st.refX = Mathf.RoundToInt(screenRef.x);
            st.refY = Mathf.RoundToInt(screenRef.y);
            st.screenW = sw;
            st.screenH = sh;
            st.layoutVersion++;
            if (cam) cam.orthographicSize = layoutLand ? 8.3f : 10.1f;
            MgfBridge.NotifyChanged();
        }
    }
}
