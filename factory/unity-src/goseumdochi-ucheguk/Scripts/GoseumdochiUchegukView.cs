using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Mgf;

namespace Mgf.GoseumdochiUcheguk
{
    public partial class GoseumdochiUchegukGame
    {
        static readonly Color Calcite = new Color32(231, 225, 213, 255);
        static readonly Color Lavender = new Color32(201, 189, 235, 255);
        static readonly Color Indigo = new Color32(49, 80, 111, 255);
        static readonly Color Coral = new Color32(227, 111, 82, 255);
        static readonly Color Mint = new Color32(169, 215, 197, 255);
        static readonly Color Ink = new Color32(31, 47, 65, 255);
        static readonly Color Wet = new Color32(105, 125, 150, 255);

        Camera cam;
        Transform worldRoot, gondola, courier, courierBody, satchel, cableLightsRoot;
        Transform[] fogBands = new Transform[7];
        Vector3[] fogBase = new Vector3[7];
        Transform[] moss = new Transform[18];
        Transform[] houseRoots = new Transform[36];
        Transform[,] houseSegments = new Transform[36, 3];
        float[] houseBirth = new float[36];
        bool[] houseVisible = new bool[36];
        Renderer[] cableLights = new Renderer[36];
        Vector3 courierHome;
        float courierRide;
        bool courierFinalRide;
        float worldClock;
        float stampImpactClock;
        int stampImpactCount;

        Material calciteMat, porcelainMat, indigoMat, coralMat, mintMat, cableMat, mossMat, wetBagMat, dryBagMat, fogMat, darkMat;

        RectTransform uiRoot, titleRt, titleSignRt, gameRt, endRt, promptRt, workbenchRt, cardShelfRt, houseBoardRt;
        RectTransform titleBagRt, titleTagRt, titleMetaRt, helpRt, restartRt, tensSlotRt, stampHandleRt, bellCordRt, revealRt, toastRt;
        RectTransform guideFingerRt, guidePathRt, rippleRt, floatingCardRt;
        readonly RectTransform[] windowRts = new RectTransform[5];
        readonly RectTransform[] shutterRts = new RectTransform[5];
        readonly RectTransform[] cardRts = new RectTransform[5];
        readonly RectTransform[] houseChipRts = new RectTransform[36];
        readonly RectTransform[] guideDots = new RectTransform[8];
        readonly RectTransform[] bagRts = new RectTransform[3];
        readonly Image[] cardImages = new Image[5];
        readonly Image[] windowImages = new Image[5];
        readonly Image[] shutterImages = new Image[5];
        readonly Image[] houseChipImages = new Image[36];
        readonly Image[] bagImages = new Image[3];
        readonly TextMeshProUGUI[] cardTexts = new TextMeshProUGUI[5];
        readonly TextMeshProUGUI[] windowTexts = new TextMeshProUGUI[5];
        readonly TextMeshProUGUI[] houseChipTexts = new TextMeshProUGUI[36];
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleBagText, helpText;
        TextMeshProUGUI promptText, goalText, scoreText, timerText, progressText, tensText, stampText, bellText;
        TextMeshProUGUI revealText, toastText, endTitle, endStats, restartText, floatingCardText, guideText;
        Image titleBagImage, tensImage, stampHandleImage, bellCordImage, revealImage, toastImage, rippleImage, floatingCardImage, guideFingerImage;
        CanvasGroup titleGroup, gameGroup, endGroup, revealGroup, toastGroup, guideGroup;
        Sprite roundedSprite, ovalSprite, diamondSprite, softSprite, ringSprite;
        CanvasScaler scaler;
        int lastScreenW, lastScreenH;
        bool layoutWide;
        float displayedScore;
        int lastScoreTarget;
        float toastClock = 99f, revealVisualClock = 99f, rippleClock = 99f, guideClock;
        float titlePress, stampVisualPull, bellVisualPull, shutterDragVisual;
        int activeCardCount, activeHouseCount;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        Image Img(RectTransform rt, Color color, Sprite sprite = null)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite ?? roundedSprite; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = false;
            return image;
        }

        TextMeshProUGUI Txt(RectTransform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, new Vector2(.5f, .5f), Vector2.zero, parent.sizeDelta - new Vector2(12, 8));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.fontStyle = FontStyles.Bold; t.color = color;
            t.alignment = align; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false; t.text = text; return t;
        }

        CanvasGroup Group(RectTransform rt, float alpha)
        {
            var g = rt.gameObject.AddComponent<CanvasGroup>(); g.alpha = alpha; g.interactable = false; g.blocksRaycasts = false; return g;
        }

        Sprite MakeRoundedSprite(string name, float radius, bool ring = false, bool oval = false, bool diamond = false)
        {
            const int S = 96;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float nx = (x + .5f) / S * 2f - 1f, ny = (y + .5f) / S * 2f - 1f;
                float d;
                if (oval) d = Mathf.Sqrt(nx * nx + ny * ny * 1.8f);
                else if (diamond) d = Mathf.Abs(nx) + Mathf.Abs(ny) * .82f;
                else
                {
                    float rx = Mathf.Max(Mathf.Abs(nx) - (1f - radius), 0f);
                    float ry = Mathf.Max(Mathf.Abs(ny) - (1f - radius), 0f);
                    d = Mathf.Sqrt(rx * rx + ry * ry) / Mathf.Max(.001f, radius);
                }
                float a = ring ? Mathf.Clamp01((1.02f - d) * 14f) * Mathf.Clamp01((d - .74f) * 17f) : Mathf.Clamp01((1.02f - d) * 14f);
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            Vector4 border = oval || diamond ? Vector4.zero : new Vector4(20, 20, 20, 20);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        Texture2D MakePorcelainTexture(Color32 baseColor, int pattern)
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Porcelain glaze", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float grain = Mathf.Sin((x * 17 + y * 7 + pattern * 23) * .19f) * .025f;
                bool vein = ((x * 11 + y * 19 + pattern * 29) % 61) < 2;
                float k = vein ? .84f : .98f + grain;
                px[y * S + x] = new Color32((byte)(baseColor.r * k), (byte)(baseColor.g * k), (byte)(baseColor.b * k), 255);
            }
            tex.SetPixels32(px); tex.Apply(false, true); return tex;
        }

        void BuildWorld()
        {
            var keeper = new GameObject("Collider type keeper");
            keeper.AddComponent<BoxCollider>(); keeper.AddComponent<SphereCollider>(); keeper.AddComponent<CapsuleCollider>(); keeper.AddComponent<MeshCollider>();
            UnityEngine.Object.Destroy(keeper);

            MgfLook.Sky(new Color32(182, 174, 218, 255), new Color32(224, 218, 230, 255), new Color32(128, 139, 164, 255), .83f);
            MgfLook.Sun(new Vector3(42, -34, 14), new Color(1f, .94f, .87f), 1.18f, .64f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.70f, .68f, .80f); RenderSettings.fogDensity = .012f;
            cam = MgfLook.Camera(new Vector3(12.8f, 11.4f, -17.5f), new Vector3(0, 3.6f, 1.7f), 34f);
            cam.orthographic = true; cam.orthographicSize = 8.2f;

            var calciteTex = MakePorcelainTexture(new Color32(231, 225, 213, 255), 2);
            var porcelainTex = MakePorcelainTexture(new Color32(247, 244, 235, 255), 5);
            calciteMat = MgfLook.Lit(Color.white, .22f, .01f, null, calciteTex);
            porcelainMat = MgfLook.Lit(Color.white, .58f, .02f, null, porcelainTex);
            indigoMat = MgfLook.Lit(Indigo, .38f, .18f);
            coralMat = MgfLook.Lit(Coral, .34f, .05f, new Color(.018f, .006f, .003f));
            mintMat = MgfLook.Lit(Mint, .28f, .02f);
            cableMat = MgfLook.Lit(new Color32(40, 52, 100, 255), .27f, .35f);
            mossMat = MgfLook.Lit(Mint, .68f, .02f, new Color(.10f, .28f, .22f));
            wetBagMat = MgfLook.Lit(new Color32(104, 126, 151, 255), .74f, .03f, new Color(.008f, .012f, .02f));
            dryBagMat = MgfLook.Lit(new Color32(232, 137, 99, 255), .25f, .01f);
            fogMat = MgfLook.Alpha(new Color(1f, .97f, 1f, .18f), MgfLook.SoftDot);
            darkMat = MgfLook.Lit(Ink, .35f, .16f);

            worldRoot = new GameObject("Vertical calcite postal canyon").transform;
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 9; i++)
            {
                float x = side * (5.6f + (i % 3) * .75f);
                float y = -1f + i * 1.38f;
                float z = 2.3f + Mathf.Sin(i * 1.4f) * 1.4f;
                var cliff = MgfLook.Block("Calcite cliff facet", new Vector3(x, y, z), new Vector3(4.2f, 2.4f, 3.5f), .22f, calciteMat, worldRoot);
                cliff.transform.rotation = Quaternion.Euler(i % 2 == 0 ? 2f : -4f, side * (10f + i * 3f), side * (2f + i % 3));
            }
            for (int i = 0; i < 8; i++)
            {
                float x = -8f + i * 2.35f;
                var ledge = MgfLook.Block("Porcelain mail ledge", new Vector3(x, 1.0f + (i % 4) * 1.65f, 1.5f + (i % 2) * .8f),
                    new Vector3(1.8f, .28f, 1.3f), .12f, porcelainMat, worldRoot);
                ledge.transform.rotation = Quaternion.Euler(0, -10f + i * 3f, 0);
            }
            BuildCable(new Vector3(-10.5f, 7.4f, -.8f), new Vector3(10.5f, 4.5f, 2.2f), .10f, cableMat, worldRoot);
            BuildCable(new Vector3(-10.2f, 3.4f, 2.0f), new Vector3(10.2f, 6.4f, -.3f), .065f, cableMat, worldRoot);

            gondola = new GameObject("Moving cable gondola").transform; gondola.SetParent(worldRoot, false);
            MgfLook.Block("Gondola porcelain cabin", Vector3.zero, new Vector3(2.2f, 1.0f, 1.25f), .26f, porcelainMat, gondola);
            MgfLook.Block("Gondola enamel stripe", new Vector3(0, -.05f, -.65f), new Vector3(1.65f, .24f, .12f), .06f, coralMat, gondola);
            MgfLook.Prim(PrimitiveType.Cylinder, "Gondola wheel", new Vector3(0, .72f, 0), new Vector3(.28f, .14f, .28f), indigoMat, gondola, false).transform.rotation = Quaternion.Euler(90, 0, 0);

            for (int i = 0; i < fogBands.Length; i++)
            {
                var f = MgfLook.Prim(PrimitiveType.Quad, "Lavender mist layer", new Vector3(-9f + i * 3.2f, 1.5f + (i % 4) * 2.1f, 5.0f + i * .22f),
                    new Vector3(5.2f + (i % 2), 2.0f, 1), fogMat, worldRoot, false);
                fogBands[i] = f.transform; fogBase[i] = f.transform.localPosition;
            }
            for (int i = 0; i < moss.Length; i++)
            {
                var m = MgfLook.Prim(PrimitiveType.Sphere, "Breathing crystal moss", new Vector3(-8f + (i * 41 % 160) / 10f, .4f + (i * 37 % 70) / 10f, 1.1f + (i % 3) * .6f),
                    Vector3.one * (.12f + (i % 4) * .045f), mossMat, worldRoot, false);
                moss[i] = m.transform;
            }
            BuildCourier(); BuildHousePool(); BuildCableLights();
        }

        void BuildCable(Vector3 a, Vector3 b, float width, Material mat, Transform parent)
        {
            Vector3 mid = (a + b) * .5f, dir = b - a;
            var o = MgfLook.Prim(PrimitiveType.Cylinder, "Braided postal cable", mid, new Vector3(width, dir.magnitude * .5f, width), mat, parent, false);
            o.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        }

        void BuildCourier()
        {
            courier = new GameObject("Mori original hedgehog courier").transform; courier.SetParent(worldRoot, false);
            courierHome = new Vector3(-2.6f, 5.15f, -.6f); courier.localPosition = courierHome;
            courierBody = MgfLook.Prim(PrimitiveType.Sphere, "Round hedgehog body", Vector3.zero, new Vector3(1.45f, 1.1f, 1.15f),
                MgfLook.Lit(new Color32(108, 83, 73, 255), .20f), courier, false).transform;
            var face = MgfLook.Prim(PrimitiveType.Sphere, "Ivory hedgehog face", new Vector3(0, -.03f, -.58f), new Vector3(1.05f, .84f, .42f),
                MgfLook.Lit(new Color32(235, 216, 189, 255), .32f), courier, false);
            for (int i = 0; i < 11; i++)
            {
                float a = (i / 10f - .5f) * 155f;
                var spine = MgfLook.Prim(PrimitiveType.Capsule, "Soft rounded spine", new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * .70f, .10f + Mathf.Cos(a * Mathf.Deg2Rad) * .48f, .18f),
                    new Vector3(.17f, .40f, .17f), darkMat, courier, false);
                spine.transform.rotation = Quaternion.Euler(0, 0, -a);
            }
            MgfLook.Prim(PrimitiveType.Sphere, "Eye L", new Vector3(-.24f, .13f, -.88f), Vector3.one * .10f, darkMat, courier, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Eye R", new Vector3(.24f, .13f, -.88f), Vector3.one * .10f, darkMat, courier, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Nose", new Vector3(0, -.06f, -1.02f), Vector3.one * .11f, darkMat, courier, false);
            satchel = MgfLook.Block("Enamel satchel", new Vector3(.58f, -.35f, -.42f), new Vector3(.64f, .54f, .28f), .13f, dryBagMat, courier).transform;
            MgfLook.Block("Indigo rain cape", new Vector3(0, .08f, .30f), new Vector3(1.45f, .12f, .92f), .06f, indigoMat, courier);
        }

        void BuildHousePool()
        {
            for (int i = 0; i < houseRoots.Length; i++)
            {
                var root = new GameObject("Pooled telescoping porcelain home " + i).transform; root.SetParent(worldRoot, false);
                root.localPosition = HouseWorldPosition(i); root.localScale = Vector3.zero; houseRoots[i] = root;
                for (int s = 0; s < 3; s++)
                {
                    var seg = MgfLook.Block("Telescoping segment", new Vector3(0, .24f + s * .35f, -.18f - s * .17f),
                        new Vector3(1.15f - s * .16f, .46f, .72f - s * .09f), .12f, s == 1 ? mintMat : porcelainMat, root).transform;
                    seg.localScale = new Vector3(1, .04f, 1); houseSegments[i, s] = seg;
                }
                MgfLook.Block("Enamel address face", new Vector3(0, .36f, -.60f), new Vector3(.72f, .31f, .08f), .07f, indigoMat, root);
                root.gameObject.SetActive(false);
            }
        }

        Vector3 HouseWorldPosition(int i)
        {
            int col = i % 9, row = i / 9;
            return new Vector3(-7.5f + col * 1.85f, 1.15f + row * 1.45f, .15f + (col % 2) * .35f);
        }

        void BuildCableLights()
        {
            cableLightsRoot = new GameObject("Student address cable lights").transform; cableLightsRoot.SetParent(worldRoot, false);
            for (int i = 0; i < cableLights.Length; i++)
            {
                float k = i / (float)(cableLights.Length - 1);
                var light = MgfLook.Prim(PrimitiveType.Sphere, "Postal route lamp", Vector3.Lerp(new Vector3(-9.5f, 6.9f, -.6f), new Vector3(9.5f, 4.7f, 1.9f), k),
                    Vector3.one * .13f, darkMat, cableLightsRoot, false);
                cableLights[i] = light.GetComponent<Renderer>();
            }
        }

        void BuildUi()
        {
            roundedSprite = MakeRoundedSprite("Porcelain rounded plate", .25f);
            ovalSprite = MakeRoundedSprite("Mail sack oval", .5f, false, true);
            diamondSprite = MakeRoundedSprite("Enamel destination sign", .3f, false, false, true);
            softSprite = MakeRoundedSprite("Soft field", .5f, false, true);
            ringSprite = MakeRoundedSprite("Pulse ring", .5f, true, true);

            var canvas = MgfText.Canvas; scaler = canvas.GetComponent<CanvasScaler>();
            uiRoot = R("Postal UI", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            uiRoot.anchorMin = Vector2.zero; uiRoot.anchorMax = Vector2.one; uiRoot.offsetMin = uiRoot.offsetMax = Vector2.zero;

            BuildTitleUi(); BuildGameUi(); BuildEndUi(); BuildOverlayUi();
            ApplyLayout(true);
        }

        void BuildTitleUi()
        {
            titleRt = R("Title screen", uiRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one; titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;
            titleGroup = Group(titleRt, 1);

            titleSignRt = R("Gondola destination sign", titleRt, new Vector2(.5f, .73f), Vector2.zero, new Vector2(354, 168));
            var signImage = Img(titleSignRt, new Color32(246, 242, 230, 245), diamondSprite); signImage.type = Image.Type.Simple;
            titleLogo = Txt(titleSignRt, "고슴도치\n우체국", 43, Indigo); titleLogo.outlineColor = new Color32(227, 111, 82, 255); titleLogo.outlineWidth = .20f;
            titleLogo.characterSpacing = -2f; titleLogo.lineSpacing = -11f; titleLogo.overflowMode = TextOverflowModes.Overflow;
            titleTagRt = R("Stamped tagline", titleRt, new Vector2(.5f, .585f), Vector2.zero, new Vector2(280, 38));
            Img(titleTagRt, new Color32(49, 80, 111, 232), roundedSprite);
            titleTag = Txt(titleTagRt, "카드로 번지를 달아라", 18, Color.white);
            titleMetaRt = R("Title meta", titleRt, new Vector2(.5f, .525f), Vector2.zero, new Vector2(340, 34));
            titleMeta = Txt(titleMetaRt,
                "중학교 2학년 · 경우의 수", 14, Ink);

            titleBagRt = R("Sealed mail sack CTA", titleRt, new Vector2(.5f, .255f), Vector2.zero, new Vector2(238, 150));
            titleBagImage = Img(titleBagRt, Coral, ovalSprite); titleBagImage.type = Image.Type.Simple;
            var knot = R("Sack knot", titleBagRt, new Vector2(.5f, 1f), new Vector2(0, 4), new Vector2(72, 48));
            Img(knot, Indigo, diamondSprite).type = Image.Type.Simple;
            titleBagText = Txt(titleBagRt, "봉인끈 당겨\n우체국 열기", 24, Color.white); titleBagText.lineSpacing = -4f;
            helpRt = R("How to link", titleRt, new Vector2(.5f, .105f), Vector2.zero, new Vector2(190, 48));
            helpText = Txt(helpRt, "어떻게 놀아요? 〉", 14, Indigo);
        }

        void BuildGameUi()
        {
            gameRt = R("Game screen", uiRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            gameRt.anchorMin = Vector2.zero; gameRt.anchorMax = Vector2.one; gameRt.offsetMin = gameRt.offsetMax = Vector2.zero;
            gameGroup = Group(gameRt, 0);

            var hud = R("Postal HUD", gameRt, new Vector2(.5f, 1f), new Vector2(0, -31), new Vector2(356, 50));
            Img(hud, new Color32(247, 244, 235, 235), roundedSprite);
            scoreText = Txt(R("Score", hud, new Vector2(.18f, .5f), Vector2.zero, new Vector2(105, 42)), "0", 19, Indigo);
            progressText = Txt(R("Progress", hud, new Vector2(.51f, .5f), Vector2.zero, new Vector2(145, 42)), "1구역", 15, Ink);
            timerText = Txt(R("Timer", hud, new Vector2(.84f, .5f), Vector2.zero, new Vector2(100, 42)), "150초", 17, Coral);
            for (int i = 0; i < 3; i++)
            {
                bagRts[i] = R("Waterproof bag " + i, hud, new Vector2(.72f + i * .055f, .84f), Vector2.zero, new Vector2(18, 24));
                bagImages[i] = Img(bagRts[i], Coral, ovalSprite); bagImages[i].type = Image.Type.Simple;
            }

            promptRt = R("Telegram prompt", gameRt, new Vector2(.5f, .89f), Vector2.zero, new Vector2(360, 105));
            Img(promptRt, new Color32(240, 235, 224, 247), roundedSprite);
            goalText = Txt(R("Goal", promptRt, new Vector2(.5f, .82f), Vector2.zero, new Vector2(336, 26)),
                "모든 유효 주소를 한 번씩 만드시오", 14, Coral);
            promptText = Txt(R("Prompt", promptRt, new Vector2(.5f, .44f), Vector2.zero, new Vector2(336, 68)), "", 15, Ink);

            houseBoardRt = R("Cliff address registry", gameRt, new Vector2(.5f, .67f), Vector2.zero, new Vector2(360, 166));
            Img(houseBoardRt, new Color32(221, 213, 232, 214), roundedSprite);
            var boardTitle = R("Registry title", houseBoardRt, new Vector2(.5f, 1f), new Vector2(0, -13), new Vector2(320, 28));
            Txt(boardTitle, "학생이 압인한 주소 · 눌러 철거", 13, Indigo);
            for (int i = 0; i < houseChipRts.Length; i++)
            {
                houseChipRts[i] = R("Address house chip " + i, houseBoardRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(48, 28));
                houseChipImages[i] = Img(houseChipRts[i], new Color32(248, 245, 236, 252), roundedSprite);
                houseChipTexts[i] = Txt(houseChipRts[i], "", 13, Indigo); houseChipRts[i].gameObject.SetActive(false);
            }

            workbenchRt = R("Porcelain embossing bench", gameRt, new Vector2(.5f, .37f), Vector2.zero, new Vector2(366, 238));
            Img(workbenchRt, new Color32(231, 225, 213, 245), roundedSprite);
            var tensLabel = R("Tens label", workbenchRt, new Vector2(.16f, .86f), Vector2.zero, new Vector2(88, 28));
            Txt(tensLabel, "십의 자리", 13, Indigo);
            tensSlotRt = R("Tens emboss slot", workbenchRt, new Vector2(.16f, .56f), Vector2.zero, new Vector2(70, 76));
            tensImage = Img(tensSlotRt, new Color32(250, 247, 238, 255), roundedSprite);
            tensText = Txt(tensSlotRt, "?", 36, Indigo);
            var onesLabel = R("Ones label", workbenchRt, new Vector2(.62f, .86f), Vector2.zero, new Vector2(222, 28));
            Txt(onesLabel, "일의 자리 스텐실 · 아래 셔터", 13, Indigo);
            for (int i = 0; i < 5; i++)
            {
                windowRts[i] = R("Stencil window " + i, workbenchRt, new Vector2(.37f + i * .12f, .57f), Vector2.zero, new Vector2(42, 68));
                windowImages[i] = Img(windowRts[i], new Color32(250, 247, 238, 255), roundedSprite);
                windowTexts[i] = Txt(windowRts[i], "·", 26, Indigo);
                shutterRts[i] = R("Shutter grip " + i, windowRts[i], new Vector2(.5f, 0f), new Vector2(0, 4), new Vector2(42, 24));
                shutterImages[i] = Img(shutterRts[i], Mint, roundedSprite);
            }
            stampHandleRt = R("Emboss pull handle", workbenchRt, new Vector2(.24f, .18f), Vector2.zero, new Vector2(138, 55));
            stampHandleImage = Img(stampHandleRt, Indigo, ovalSprite); stampHandleImage.type = Image.Type.Simple;
            stampText = Txt(stampHandleRt, "압인 손잡이 ↓", 17, Color.white);
            bellCordRt = R("Postal bell cord", workbenchRt, new Vector2(.75f, .18f), Vector2.zero, new Vector2(138, 55));
            bellCordImage = Img(bellCordRt, Coral, ovalSprite); bellCordImage.type = Image.Type.Simple;
            bellText = Txt(bellCordRt, "완성 종줄 ↓", 17, Color.white);

            cardShelfRt = R("Enamel card shelf", gameRt, new Vector2(.5f, .105f), Vector2.zero, new Vector2(366, 112));
            Img(cardShelfRt, new Color32(49, 80, 111, 238), roundedSprite);
            for (int i = 0; i < 5; i++)
            {
                cardRts[i] = R("Enamel card " + i, cardShelfRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(58, 72));
                cardImages[i] = Img(cardRts[i], new Color32(248, 244, 232, 255), roundedSprite);
                cardTexts[i] = Txt(cardRts[i], "", 30, Indigo);
            }
        }

        void BuildEndUi()
        {
            endRt = R("End screen", uiRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            endRt.anchorMin = Vector2.zero; endRt.anchorMax = Vector2.one; endRt.offsetMin = endRt.offsetMax = Vector2.zero;
            endGroup = Group(endRt, 0);
            var banner = R("Result cable banner", endRt, new Vector2(.5f, .68f), Vector2.zero, new Vector2(352, 228));
            Img(banner, new Color32(246, 242, 230, 245), diamondSprite).type = Image.Type.Simple;
            endTitle = Txt(R("End title", banner, new Vector2(.5f, .70f), Vector2.zero, new Vector2(310, 70)), "배달 완료", 35, Indigo);
            endTitle.outlineColor = Coral; endTitle.outlineWidth = .16f;
            endStats = Txt(R("End stats", banner, new Vector2(.5f, .33f), Vector2.zero, new Vector2(310, 96)), "", 17, Ink);
            restartRt = R("Restart mail sack", endRt, new Vector2(.5f, .31f), Vector2.zero, new Vector2(235, 130));
            Img(restartRt, Coral, ovalSprite).type = Image.Type.Simple;
            restartText = Txt(restartRt, "우편낭 다시 열기", 22, Color.white);
        }

        void BuildOverlayUi()
        {
            revealRt = R("Math reveal", uiRoot, new Vector2(.5f, .55f), Vector2.zero, new Vector2(350, 108));
            revealImage = Img(revealRt, new Color32(49, 80, 111, 246), diamondSprite); revealImage.type = Image.Type.Simple;
            revealText = Txt(revealRt, "", 20, Color.white); revealGroup = Group(revealRt, 0);
            toastRt = R("Instruction toast", uiRoot, new Vector2(.5f, .47f), Vector2.zero, new Vector2(342, 74));
            toastImage = Img(toastRt, new Color32(248, 244, 233, 248), roundedSprite);
            toastText = Txt(toastRt, "", 16, Ink); toastGroup = Group(toastRt, 0);
            rippleRt = R("Tap ripple", uiRoot, new Vector2(0, 0), Vector2.zero, new Vector2(62, 62));
            rippleImage = Img(rippleRt, new Color(1, 1, 1, .8f), ringSprite); rippleImage.type = Image.Type.Simple; rippleRt.gameObject.SetActive(false);
            floatingCardRt = R("Floating dragged card", uiRoot, new Vector2(0, 0), Vector2.zero, new Vector2(66, 80));
            floatingCardImage = Img(floatingCardRt, new Color32(250, 247, 238, 255), roundedSprite);
            floatingCardText = Txt(floatingCardRt, "", 31, Indigo); floatingCardRt.gameObject.SetActive(false);

            var guideRt = R("Living onboarding guide", uiRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            guideRt.anchorMin = Vector2.zero; guideRt.anchorMax = Vector2.one; guideRt.offsetMin = guideRt.offsetMax = Vector2.zero;
            guideGroup = Group(guideRt, 0);
            guidePathRt = R("Guide path host", guideRt, new Vector2(0, 0), Vector2.zero, Vector2.zero);
            for (int i = 0; i < guideDots.Length; i++)
            {
                guideDots[i] = R("Flowing chevron " + i, guideRt, new Vector2(0, 0), Vector2.zero, new Vector2(14, 14));
                Img(guideDots[i], i == guideDots.Length - 1 ? Coral : Indigo, diamondSprite).type = Image.Type.Simple;
            }
            guideFingerRt = R("Guide hand", guideRt, new Vector2(0, 0), Vector2.zero, new Vector2(50, 50));
            guideFingerImage = Img(guideFingerRt, Coral, ringSprite); guideFingerImage.type = Image.Type.Simple;
            guideText = Txt(R("Guide text", guideRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(310, 42)), "", 15, Indigo);
        }

        void ApplyLayout(bool force = false)
        {
            if (!force && Screen.width == lastScreenW && Screen.height == lastScreenH) return;
            lastScreenW = Screen.width; lastScreenH = Screen.height; layoutWide = Screen.width >= 1024 && Screen.width > Screen.height;
            if (layoutWide)
            {
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(.5f, .88f); promptRt.sizeDelta = new Vector2(620, 94);
                promptText.rectTransform.sizeDelta = new Vector2(580, 64); promptText.fontSize = 15;
                houseBoardRt.anchorMin = houseBoardRt.anchorMax = new Vector2(.29f, .50f); houseBoardRt.sizeDelta = new Vector2(470, 330);
                workbenchRt.anchorMin = workbenchRt.anchorMax = new Vector2(.73f, .49f); workbenchRt.sizeDelta = new Vector2(430, 292);
                cardShelfRt.anchorMin = cardShelfRt.anchorMax = new Vector2(.73f, .18f); cardShelfRt.sizeDelta = new Vector2(430, 112);
                titleLogo.fontSize = 52; titleSignRt.sizeDelta = new Vector2(540, 186); titleSignRt.anchorMin = titleSignRt.anchorMax = new Vector2(.5f, .72f);
                titleTagRt.anchorMin = titleTagRt.anchorMax = new Vector2(.5f, .50f); titleTagRt.sizeDelta = new Vector2(340, 40);
                titleMetaRt.anchorMin = titleMetaRt.anchorMax = new Vector2(.18f, .08f); titleMetaRt.sizeDelta = new Vector2(300, 34);
                titleBagRt.anchorMin = titleBagRt.anchorMax = new Vector2(.5f, .24f);
                helpRt.anchorMin = helpRt.anchorMax = new Vector2(.82f, .08f);
            }
            else
            {
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(.5f, .885f); promptRt.sizeDelta = new Vector2(360, 105);
                promptText.rectTransform.sizeDelta = new Vector2(336, 76); promptText.fontSize = 13;
                houseBoardRt.anchorMin = houseBoardRt.anchorMax = new Vector2(.5f, .665f); houseBoardRt.sizeDelta = new Vector2(360, 166);
                workbenchRt.anchorMin = workbenchRt.anchorMax = new Vector2(.5f, .385f); workbenchRt.sizeDelta = new Vector2(366, 238);
                cardShelfRt.anchorMin = cardShelfRt.anchorMax = new Vector2(.5f, .105f); cardShelfRt.sizeDelta = new Vector2(366, 112);
                titleLogo.fontSize = 43; titleSignRt.sizeDelta = new Vector2(354, 168); titleSignRt.anchorMin = titleSignRt.anchorMax = new Vector2(.5f, .73f);
                titleTagRt.anchorMin = titleTagRt.anchorMax = new Vector2(.5f, .585f); titleTagRt.sizeDelta = new Vector2(280, 38);
                titleMetaRt.anchorMin = titleMetaRt.anchorMax = new Vector2(.5f, .525f); titleMetaRt.sizeDelta = new Vector2(340, 34);
                titleBagRt.anchorMin = titleBagRt.anchorMax = new Vector2(.5f, .255f);
                helpRt.anchorMin = helpRt.anchorMax = new Vector2(.5f, .105f);
            }
            LayoutWorkbench(); LayoutCards(); LayoutHouseChips();
        }

        void LayoutWorkbench()
        {
            float windowStart = layoutWide ? .35f : .37f;
            float step = layoutWide ? .125f : .12f;
            for (int i = 0; i < 5; i++) windowRts[i].anchorMin = windowRts[i].anchorMax = new Vector2(windowStart + i * step, .57f);
        }

        void LayoutCards()
        {
            float spacing = activeCardCount >= 5 ? 66f : 76f;
            for (int i = 0; i < cardRts.Length; i++)
            {
                float x = (i - (activeCardCount - 1) * .5f) * spacing;
                cardRts[i].anchorMin = cardRts[i].anchorMax = new Vector2(.5f, .52f); cardRts[i].anchoredPosition = new Vector2(x, 0);
            }
        }

        void LayoutHouseChips()
        {
            int cols = layoutWide ? 7 : (activeHouseCount > 20 ? 6 : 5);
            float cellW = layoutWide ? 62f : 56f, cellH = layoutWide ? 42f : 34f;
            for (int i = 0; i < houseChipRts.Length; i++)
            {
                int row = i / cols, col = i % cols;
                float x = (col - (cols - 1) * .5f) * cellW;
                float y = (layoutWide ? 110f : 53f) - row * cellH;
                houseChipRts[i].anchoredPosition = new Vector2(x, y);
            }
        }

        void SetPhaseVisuals()
        {
            titleGroup.alpha = runPhase == RunPhase.Title ? 1 : 0;
            gameGroup.alpha = runPhase == RunPhase.Practice || runPhase == RunPhase.Playing || runPhase == RunPhase.Feedback ? 1 : 0;
            endGroup.alpha = runPhase == RunPhase.End ? 1 : 0;
            guideGroup.alpha = runPhase == RunPhase.Practice || runPhase == RunPhase.Playing ? 1 : 0;
        }

        void ResetWorldForRun()
        {
            courierRide = 0; courierFinalRide = false;
            if (courier) { courier.localPosition = courierHome; courier.localRotation = Quaternion.identity; }
            if (satchel) satchel.GetComponent<Renderer>().sharedMaterial = dryBagMat;
            for (int i = 0; i < cableLights.Length; i++) if (cableLights[i]) cableLights[i].sharedMaterial = darkMat;
            for (int i = 0; i < houseRoots.Length; i++)
            {
                houseVisible[i] = false;
                if (houseRoots[i]) { houseRoots[i].gameObject.SetActive(false); houseRoots[i].localScale = Vector3.zero; }
            }
        }

        void ResetWorldForProblem()
        {
            for (int i = 0; i < houseRoots.Length; i++)
            {
                houseVisible[i] = false;
                houseRoots[i].gameObject.SetActive(false); houseRoots[i].localScale = Vector3.zero;
            }
            activeHouseCount = 0; RebuildHouseList();
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            promptText.text = current.prompt;
            goalText.text = st.onboarding ? "카드 1→십의 자리 · 카드 2→일의 자리 · 압인" : "모든 유효 주소를 한 번씩 만들고 종줄을 당기시오";
            activeCardCount = current.cards.Length;
            for (int i = 0; i < cardRts.Length; i++)
            {
                bool on = i < activeCardCount; cardRts[i].gameObject.SetActive(on);
                if (on) cardTexts[i].text = AddressRules.CardLabel(current, current.cards[i]);
            }
            LayoutCards();
            SetTensVisual(tens);
            for (int i = 0; i < windows.Length; i++) SetWindowVisual(i, windows[i], shutters[i]);
            RefreshHud(); RebuildHouseList();
        }

        void RefreshHud()
        {
            progressText.text = st.onboarding ? "압인 연습" : (st.zone == 3 ? "3구역 " + (st.substage == 0 ? "역할 있음" : "역할 없음") : st.zone + "구역") + " · " + st.solved + "/3";
            for (int i = 0; i < bagImages.Length; i++) bagImages[i].color = i < st.lives ? Coral : new Color32(135, 157, 177, 190);
        }

        void UpdateWorld(float dt)
        {
            worldClock += dt;
            float aspect = Mathf.Max(.1f, (float)Screen.width / Screen.height);
            cam.orthographicSize = aspect < .75f ? 11.2f : aspect > 1.25f ? 7.8f : 9.2f;
            cam.transform.position = aspect > 1.25f ? new Vector3(13.4f, 10.8f, -17.6f) : new Vector3(11.3f, 12.4f, -18.6f);
            cam.transform.LookAt(new Vector3(0, 3.8f, 1.7f));

            if (gondola)
            {
                float k = Mathf.PingPong(worldClock * .055f, 1f);
                gondola.localPosition = Vector3.Lerp(new Vector3(-10f, 7.25f, -.65f), new Vector3(10f, 4.52f, 2.0f), k);
                gondola.localRotation = Quaternion.Euler(0, Mathf.Sin(worldClock * .7f) * 3f, Mathf.Sin(worldClock * 1.1f) * 1.8f);
            }
            for (int i = 0; i < fogBands.Length; i++)
            {
                Vector3 p = fogBase[i]; p.x += Mathf.Sin(worldClock * (.08f + i * .007f) + i) * 1.25f;
                p.y += Mathf.Sin(worldClock * .14f + i * .8f) * .18f; fogBands[i].localPosition = p;
            }
            for (int i = 0; i < moss.Length; i++)
            {
                float s = .82f + Mathf.Sin(worldClock * 1.05f + i * .61f) * .18f;
                moss[i].localScale = Vector3.one * s;
            }
            if (courier && courierRide <= 0)
            {
                courier.localPosition = courierHome + Vector3.up * (Mathf.Sin(worldClock * 2.2f) * .08f);
                courier.localRotation = Quaternion.Euler(0, Mathf.Sin(worldClock * 1.4f) * 7f, Mathf.Sin(worldClock * 1.8f) * 2.5f);
            }
            if (courierRide > 0)
            {
                courierRide += dt * (courierFinalRide ? .52f : .9f);
                float k = Mathf.Clamp01(courierRide);
                Vector3 a = new Vector3(-8.5f, 6.65f, -.4f), b = new Vector3(8.5f, 4.85f, 1.6f);
                courier.localPosition = Vector3.Lerp(a, b, k) + Vector3.down * Mathf.Sin(k * Mathf.PI) * .32f;
                courier.localRotation = Quaternion.Euler(0, -12f, Mathf.Sin(k * Mathf.PI) * -8f);
                int lit = Mathf.Clamp(Mathf.FloorToInt(k * cableLights.Length), 0, cableLights.Length);
                for (int i = 0; i < cableLights.Length; i++) cableLights[i].sharedMaterial = i < lit ? mossMat : darkMat;
                if (k >= 1f && !courierFinalRide) { courierRide = 0; courier.localPosition = courierHome; }
            }
            for (int i = 0; i < houseRoots.Length; i++)
            {
                if (!houseVisible[i] || !houseRoots[i].gameObject.activeSelf) continue;
                float age = Time.unscaledTime - houseBirth[i];
                float rootK = EaseOutBack(Mathf.Clamp01(age / .34f));
                houseRoots[i].localScale = Vector3.one * rootK;
                for (int s = 0; s < 3; s++)
                {
                    float sk = EaseOutBack(Mathf.Clamp01((age - s * .08f) / .28f));
                    houseSegments[i, s].localScale = new Vector3(1f + Mathf.Sin(Mathf.Clamp01(sk) * Mathf.PI) * .08f, Mathf.Max(.04f, sk), 1f);
                }
            }
            if (stampImpactClock > 0) stampImpactClock = Mathf.Max(0, stampImpactClock - dt);
        }

        float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        void UpdateUi(float dt)
        {
            ApplyLayout();
            titleBagRt.localScale = Vector3.one * (1f + Mathf.Sin(titleClock * 2.6f) * .018f - titlePress * .06f);
            titleLogo.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(titleClock * .72f) * .7f);
            if (lastScoreTarget != st.score) { lastScoreTarget = st.score; MgfFx.Punch(scoreText.transform, .16f, .30f); }
            displayedScore = Mathf.MoveTowards(displayedScore, st.score, dt * Mathf.Max(80f, Mathf.Abs(st.score - displayedScore) * 5f));
            scoreText.text = "우표 " + Mathf.RoundToInt(displayedScore);
            timerText.text = Mathf.CeilToInt(runLeft) + "초";
            RefreshHud();

            if (toastClock < 99f)
            {
                toastClock += dt; toastGroup.alpha = Mathf.Clamp01(Mathf.Min(toastClock * 8f, (3.6f - toastClock) * 6f));
                if (toastClock >= 3.6f) toastGroup.alpha = 0;
            }
            if (revealVisualClock < 99f)
            {
                revealVisualClock += dt; revealGroup.alpha = Mathf.Clamp01(Mathf.Min(revealVisualClock * 7f, (2.0f - revealVisualClock) * 5f));
                revealRt.localScale = Vector3.one * (1f + Mathf.Sin(Mathf.Clamp01(revealVisualClock / .35f) * Mathf.PI) * .06f);
                if (revealVisualClock >= 2f) revealGroup.alpha = 0;
            }
            if (rippleClock < .48f)
            {
                rippleClock += dt; float k = rippleClock / .48f;
                rippleRt.localScale = Vector3.one * Mathf.Lerp(.35f, 1.5f, k); rippleImage.color = new Color(1, 1, 1, 1f - k);
                if (k >= 1) rippleRt.gameObject.SetActive(false);
            }
            guideClock += dt; PositionGuide();
            titlePress = Mathf.MoveTowards(titlePress, 0, dt * 4f);
            stampVisualPull = Mathf.MoveTowards(stampVisualPull, 0, dt * 7f);
            bellVisualPull = Mathf.MoveTowards(bellVisualPull, 0, dt * 7f);
            stampHandleRt.anchoredPosition = new Vector2(0, -stampVisualPull);
            bellCordRt.anchoredPosition = new Vector2(0, -bellVisualPull);
        }

        bool Hit(RectTransform rt, Vector2 screen) => rt && rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, screen, null);
        int HitCard(Vector2 p) { for (int i = 0; i < activeCardCount; i++) if (Hit(cardRts[i], p)) return i; return -1; }
        int HitWindow(Vector2 p) { for (int i = 0; i < windowRts.Length; i++) if (Hit(windowRts[i], p)) return i; return -1; }
        int HitShutter(Vector2 p) { for (int i = 0; i < shutterRts.Length; i++) if (Hit(shutterRts[i], p)) return i; return -1; }
        int HitHouse(Vector2 p) { for (int i = 0; i < activeHouseCount; i++) if (Hit(houseChipRts[i], p)) return i; return -1; }

        Vector2 ScreenCenter(RectTransform rt)
        {
            Vector3[] c = new Vector3[4]; rt.GetWorldCorners(c); return new Vector2((c[0].x + c[2].x) * .5f, (c[0].y + c[2].y) * .5f);
        }

        Vector2 CanvasPoint(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, screen, null, out var local); return local;
        }

        void SpawnTapRipple(Vector2 screen)
        {
            rippleRt.gameObject.SetActive(true); rippleRt.anchoredPosition = CanvasPoint(screen); rippleRt.localScale = Vector3.one * .35f;
            rippleClock = 0; rippleImage.color = new Color(1, 1, 1, .9f);
        }

        void BeginCardDrag(int index, Vector2 screen)
        {
            floatingCardRt.gameObject.SetActive(true); floatingCardText.text = AddressRules.CardLabel(current, dragCardValue);
            floatingCardRt.anchoredPosition = CanvasPoint(screen); floatingCardRt.localScale = Vector3.one * 1.08f;
            MgfFx.Punch(cardRts[index], .12f, .22f);
        }
        void UpdateCardDrag(Vector2 screen) { floatingCardRt.anchoredPosition = CanvasPoint(screen); }
        void EndCardDrag(bool placed, Vector2 screen)
        {
            if (placed) { floatingCardRt.localScale = Vector3.one * .25f; }
            floatingCardRt.gameObject.SetActive(false);
        }

        void PressTitleBag() { titlePress = 1f; MgfSfx.Play("tap", .18f); }
        void UpdateTitleCord(float pull) { titlePress = Mathf.Clamp01(pull / 70f); titleBagRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(pull * .09f) * 2f); }
        void ReleaseTitleBag() { titleBagRt.localRotation = Quaternion.identity; titlePress = .35f; }
        void PulseTitleBag() { MgfFx.Punch(titleBagRt, .12f, .35f); }
        void PulseRestart() { MgfFx.Punch(restartRt, .12f, .34f); }
        void PulseCards() { for (int i = 0; i < activeCardCount; i++) MgfFx.Punch(cardRts[i], .08f, .26f); }
        void PulseTens() { MgfFx.Punch(tensSlotRt, .14f, .32f); }
        void PulseWindow(int i) { if (i >= 0 && i < windowRts.Length) MgfFx.Punch(windowRts[i], .13f, .30f); }
        void PulseWindows() { for (int i = 0; i < windowRts.Length; i++) MgfFx.Punch(windowRts[i], .08f, .25f); }
        void PulseBell() { MgfFx.Punch(bellCordRt, .13f, .32f); }
        void StampPlatePress(RectTransform rt) { MgfFx.Punch(rt, .13f, .26f); }
        void AnticipateStamp() { MgfFx.Punch(stampHandleRt, .08f, .16f); }
        void UpdateStampPull(float pull) { stampVisualPull = Mathf.Clamp(pull * .35f, 0, 42f); }
        void ReleaseStamp() { stampVisualPull = 0; }
        void AnticipateBell() { MgfFx.Punch(bellCordRt, .08f, .16f); }
        void UpdateBellPull(float pull) { bellVisualPull = Mathf.Clamp(pull * .35f, 0, 44f); }
        void ReleaseBell() { bellVisualPull = 0; }
        void UpdateShutterDrag(int index, float dx) { shutterDragVisual = dx; if (index >= 0) shutterRts[index].anchoredPosition = new Vector2(Mathf.Clamp(dx * .25f, -8, 8), 4); }

        void SetTensVisual(int value)
        {
            tensText.text = value < 0 || current == null ? "?" : AddressRules.CardLabel(current, value);
            tensImage.color = value < 0 ? new Color32(250, 247, 238, 255) : new Color32(255, 253, 244, 255);
        }

        void SetWindowVisual(int index, int value, bool closed)
        {
            if (current == null) return;
            windowTexts[index].text = value < 0 ? "·" : AddressRules.CardLabel(current, value);
            windowImages[index].color = closed ? new Color32(191, 196, 203, 255) : new Color32(250, 247, 238, 255);
            shutterImages[index].color = closed ? Indigo : Mint;
            shutterRts[index].anchoredPosition = new Vector2(0, 4);
        }

        void RebuildHouseList()
        {
            activeHouseCount = Mathf.Min(built.Count, houseChipRts.Length);
            for (int i = 0; i < houseChipRts.Length; i++)
            {
                bool on = i < activeHouseCount; houseChipRts[i].gameObject.SetActive(on);
                if (on)
                {
                    houseChipTexts[i].text = AddressRules.MarkLabel(current, built[i]);
                    houseChipImages[i].color = new Color32(248, 245, 236, 252);
                }
            }
            LayoutHouseChips();
        }

        void SpawnHouse(int index, AddressMark mark, float delay)
        {
            if (index < 0 || index >= houseRoots.Length) return;
            houseRoots[index].gameObject.SetActive(true); houseRoots[index].localPosition = HouseWorldPosition(index);
            houseRoots[index].localScale = Vector3.zero; houseBirth[index] = Time.unscaledTime + delay; houseVisible[index] = true;
        }

        void RebuildWorldHouses(IList<AddressMark> marks)
        {
            for (int i = 0; i < houseRoots.Length; i++)
            {
                bool on = i < marks.Count; houseVisible[i] = on; houseRoots[i].gameObject.SetActive(on);
                if (on) { houseRoots[i].localPosition = HouseWorldPosition(i); houseRoots[i].localScale = Vector3.one; houseBirth[i] = Time.unscaledTime - 1f; }
            }
        }

        void RemoveHouseVisual(int index)
        {
            if (index >= 0 && index < houseRoots.Length) { houseRoots[index].localScale = new Vector3(1.15f, .05f, 1.15f); }
        }

        void StampImpact(int count)
        {
            stampImpactClock = .18f; stampImpactCount = count;
            MgfFx.Punch(workbenchRt, Mathf.Min(.10f, .025f * count), .22f);
            if (activeHouseCount > 0) MgfFx.Punch(houseBoardRt, .055f, .30f);
        }

        void CorrectDistrictEvent(IList<AddressMark> addresses)
        {
            for (int i = 0; i < activeHouseCount; i++) houseChipImages[i].color = new Color32(190, 229, 208, 255);
            for (int i = 0; i < Math.Min(addresses.Count, cableLights.Length); i++) cableLights[i].sharedMaterial = mossMat;
            MgfFx.Punch(houseBoardRt, .10f, .42f);
        }

        void CourierZip(bool final)
        {
            courierRide = .001f; courierFinalRide = final;
            MgfSfx.Play("whoosh", final ? .38f : .25f);
        }

        void WrongRainBag(int lives)
        {
            if (satchel) satchel.GetComponent<Renderer>().sharedMaterial = wetBagMat;
            int lost = Mathf.Clamp(2 - lives, 0, 2);
            if (lost >= 0 && lost < bagRts.Length) MgfFx.Punch(bagRts[lost], .22f, .38f);
        }

        void ShowFailure(string misconception)
        {
            string message = FailureMessage(misconception);
            ShowMathReveal(message);
            for (int i = 0; i < built.Count && i < houseChipImages.Length; i++)
            {
                bool bad = false;
                AddressMark raw = built[i];
                if (misconception == AddressRules.MisconceptionLeadingZero) bad = raw.a == 0;
                else if (misconception == AddressRules.MisconceptionReuseCard) bad = raw.a == raw.b;
                else if (misconception == AddressRules.MisconceptionDuplicate)
                {
                    for (int j = 0; j < i; j++) if (built[j].Equals(raw)) bad = true;
                }
                if (bad) houseChipImages[i].color = new Color32(197, 205, 215, 255);
            }
        }

        void ShowMathReveal(string text)
        {
            revealText.text = text; revealVisualClock = 0; revealGroup.alpha = 1; revealRt.localScale = Vector3.one * .92f;
        }
        void HideReveal() { revealGroup.alpha = 0; revealVisualClock = 99f; }
        void ShowToast(string text, float duration)
        {
            toastText.text = text; toastClock = 0; toastGroup.alpha = 1;
        }
        void Refuse(string text)
        {
            ShowToast(text, 3f); MgfSfx.Play("wrong", .13f);
            if (runPhase == RunPhase.Practice || runPhase == RunPhase.Playing) MgfFx.Punch(workbenchRt, .025f, .22f);
        }

        void ReplayGuide(bool boosted)
        {
            guideClock = boosted ? 0 : .2f; if (boosted) guideFingerRt.sizeDelta = new Vector2(64, 64);
            else guideFingerRt.sizeDelta = new Vector2(50, 50);
        }

        void PositionGuide()
        {
            if (runPhase != RunPhase.Practice && runPhase != RunPhase.Playing) { guideGroup.alpha = 0; return; }
            RectTransform from = null, to = null;
            string label;
            if (runPhase == RunPhase.Practice)
            {
                if (!practiceFirstDone)
                {
                    if (tens != 1) { from = CardRectForValue(1); to = tensSlotRt; label = "카드 1을 십의 자리에 놓으시오"; }
                    else if (!OpenSecondsContainOnly(2)) { from = CardRectForValue(2); to = windowRts[0]; label = "카드 2를 일의 자리 창에 놓으시오"; }
                    else { from = stampHandleRt; to = workbenchRt; label = "압인 손잡이를 아래로 당기시오"; }
                }
                else
                {
                    if (tens != 2) { from = CardRectForValue(2); to = tensSlotRt; label = "카드 2를 십의 자리에 놓으시오"; }
                    else if (!AnyWindowValue(1)) { from = CardRectForValue(1); to = windowRts[1]; label = "카드 1을 열린 창에 놓으시오"; }
                    else if (!AnyWindowValue(3)) { from = CardRectForValue(3); to = windowRts[2]; label = "카드 3을 열린 창에 놓으시오"; }
                    else if (!ClosedWindowValue(2)) { from = WindowRectForValue(2); to = WindowRectForValue(2); label = "카드 2 창의 셔터를 옆으로 미시오"; }
                    else { from = stampHandleRt; to = workbenchRt; label = "손잡이를 당겨 주소 21·23을 함께 압인하시오"; }
                }
            }
            else
            {
                if (built.Count == 0) { from = cardRts[0]; to = tensSlotRt; label = "십의 자리부터 계획하시오"; }
                else { from = bellCordRt; to = workbenchRt; label = "완전하다고 판단하면 종줄을 당기시오"; }
            }
            if (!from || !to) { guideGroup.alpha = 0; return; }
            guideGroup.alpha = 1;
            Vector2 a = ScreenCenter(from), b = ScreenCenter(to);
            if (from == to) b = a + new Vector2(42, 0);
            if (from == stampHandleRt || from == bellCordRt) b = a + new Vector2(0, -72);
            float flow = Mathf.Repeat(guideClock / 1.15f, 1f);
            for (int i = 0; i < guideDots.Length; i++)
            {
                float k = Mathf.Repeat(flow + i / (float)guideDots.Length, 1f);
                Vector2 p = Vector2.Lerp(a, b, k);
                guideDots[i].anchoredPosition = CanvasPoint(p);
                float s = .72f + Mathf.Sin(k * Mathf.PI) * .35f; guideDots[i].localScale = Vector3.one * s;
            }
            guideFingerRt.anchoredPosition = CanvasPoint(Vector2.Lerp(a, b, .5f + Mathf.Sin(guideClock * 4f) * .12f));
            guideText.gameObject.SetActive(false);
            if (runPhase == RunPhase.Practice) goalText.text = label;
        }

        RectTransform CardRectForValue(int value)
        {
            for (int i = 0; i < activeCardCount; i++) if (current.cards[i] == value) return cardRts[i]; return cardRts[0];
        }
        RectTransform WindowRectForValue(int value) { for (int i = 0; i < windows.Length; i++) if (windows[i] == value) return windowRts[i]; return windowRts[0]; }
        bool AnyWindowValue(int v) { for (int i = 0; i < windows.Length; i++) if (windows[i] == v) return true; return false; }
        bool ClosedWindowValue(int v) { for (int i = 0; i < windows.Length; i++) if (windows[i] == v && shutters[i]) return true; return false; }
        bool OpenSecondsContainOnly(int v)
        {
            int count = 0; for (int i = 0; i < windows.Length; i++) if (windows[i] >= 0 && !shutters[i]) { count++; if (windows[i] != v) return false; }
            return count == 1;
        }

        void SetEndScreen(bool win, string reason)
        {
            SetPhaseVisuals();
            endTitle.text = win ? "우편 노선 완성" : st.phase == "practice_complete" ? "연습 배달 완료" : "결정비 도착";
            endStats.text = reason + "\n완성 구역 " + st.solved + "/3 · 첫 시도 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal + "\n우표 " + st.score + " · 압인 " + st.stamps + "회";
            restartText.text = "우편낭 다시 열기";
            displayedScore = 0; lastScoreTarget = -1;
            if (win) { courierRide = .001f; courierFinalRide = true; }
        }
    }
}
