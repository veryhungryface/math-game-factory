// 꿀 도둑 — 코드로 만든 밀랍 벌통 디오라마와 화면 UI.
// 배경 원화를 깔지 않고 같은 세계를 타이틀·플레이·결과에 계속 유지한다.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mgf;

namespace Mgf.KkulDoduk
{
    public partial class KkulDodukGame
    {
        static readonly Color Honey = new Color32(232, 177, 63, 255);
        static readonly Color HoneyLight = new Color32(250, 207, 91, 255);
        static readonly Color Amber = new Color32(154, 91, 33, 255);
        static readonly Color Raspberry = new Color32(215, 82, 120, 255);
        static readonly Color Cream = new Color32(255, 244, 201, 255);
        static readonly Color Ink = new Color32(65, 76, 53, 255);
        static readonly Color Leaf = new Color32(111, 128, 69, 255);
        static readonly Color Error = new Color32(153, 54, 64, 255);

        Camera cam;
        Vector3 cameraBase;
        Transform worldRoot, archRoot;
        readonly Transform[] bees = new Transform[6];
        readonly Transform[] beeWingsL = new Transform[6];
        readonly Transform[] beeWingsR = new Transform[6];
        readonly Vector3[] beeHomes = new Vector3[6];
        readonly Transform[] honeyDrops = new Transform[18];
        readonly Vector3[] dropHomes = new Vector3[18];
        Material waxMat, waxLightMat, amberMat, creamMat, raspberryMat, inkMat, leafMat, glowMat;
        float worldClock, correctFxClock = 9f, wrongFxClock = 9f, hitStopClock;
        float beeReactClock = 9f;

        RectTransform uiRoot, titleRoot, gameRoot, endRoot, revealRoot;
        CanvasGroup titleGroup, gameGroup, endGroup, revealGroup;
        RectTransform titleSealL, titleSealR, titleSealRing, restartSeal;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleSealText;
        TextMeshProUGUI hudText, goalText, promptText, leftShelfText, rightShelfText;
        TextMeshProUGUI selectedText, instructionText, recordText, toastText;
        TextMeshProUGUI revealTitle, revealText, endTitle, endStats, endReasonText, restartText;
        Image handleImage, bladeImage, guideRing, rippleImage, revealPlate;
        RectTransform handleRt, bladeRt, guideRt, rippleRt, cutTrailRt, recordPlateRt;
        readonly RectTransform[] cellRt = new RectTransform[24];
        readonly Image[] cellImage = new Image[24];
        readonly TextMeshProUGUI[] tickText = new TextMeshProUGUI[24];
        readonly RectTransform[] sampleCards = new RectTransform[8];
        readonly Image[] sampleImages = new Image[8];
        Sprite softDisc, ringSprite, waxPanel;
        bool layoutLand;
        int layoutW = -1, layoutH = -1;
        float rippleClock = 9f, toastClock = 9f, guideBoost, handlePunch, cutTrailClock = 9f;
        Vector2 rippleScreen;
        string toastCache = "";
        float shownScore;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        RectTransform Full(string name, Transform parent)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        Image Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            image.sprite = sprite; if (sprite != null && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        TextMeshProUGUI Txt(string name, Transform parent, string text, Vector2 anchor, Vector2 pos,
            float size, Color color, float width, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R(name, parent, anchor, pos, new Vector2(width, size * 3.1f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.fontStyle = FontStyles.Bold; t.color = color;
            t.alignment = align; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false; t.text = text; return t;
        }

        Sprite MakeDisc(bool ring)
        {
            const int S = 96;
            var texture = new Texture2D(S, S, TextureFormat.RGBA32, false) {
                name = ring ? "Wax ripple ring" : "Soft wax disc", wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = (x + .5f) / S * 2f - 1f, dy = (y + .5f) / S * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = ring
                    ? Mathf.Clamp01((.96f - d) * 18f) * Mathf.Clamp01((d - .66f) * 18f)
                    : Mathf.Clamp01((.96f - d) * 14f);
                pixels[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        Sprite MakeWaxPanel()
        {
            const int S = 96;
            var texture = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Carved wax tag", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[S * S];
            const float radius = 17f;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - S * .5f) - (S * .5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + .5f - S * .5f) - (S * .5f - radius), 0f);
                float a = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                float grain = ((x * 13 + y * 7) % 19 == 0) ? .93f : 1f;
                pixels[y * S + x] = new Color(1f * grain, .98f * grain, .88f * grain, a);
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(20, 20, 20, 20));
        }

        void BuildWorld()
        {
            // CreatePrimitive가 내부에서 이름으로 콜라이더를 붙인다. WebGL 스트리핑이 타입을
            // 지우지 않도록 명시 참조를 남긴다(실제 입력 판정은 화면 좌표 기반이다).
            var colliderTypes = new GameObject("Collider type keeper");
            colliderTypes.AddComponent<CapsuleCollider>(); colliderTypes.AddComponent<SphereCollider>();
            colliderTypes.AddComponent<BoxCollider>(); colliderTypes.AddComponent<MeshCollider>();
            Object.Destroy(colliderTypes);
            MgfLook.Sky(MgfLook.Hex("#9A5B21"), MgfLook.Hex("#E8B13F"), MgfLook.Hex("#FFF4C9"), .80f);
            MgfLook.Sun(new Vector3(-34, -36, 18), Cream, 1.18f, .63f);
            RenderSettings.ambientLight = new Color(.54f, .36f, .13f);
            cam = MgfLook.Camera(new Vector3(9.2f, 7.8f, -15.8f), new Vector3(0, 2.1f, 1.5f), 38f);
            cam.backgroundColor = Honey; cameraBase = cam.transform.position;

            waxMat = MgfLook.Lit(Honey, .26f, .01f, Honey * .035f);
            waxLightMat = MgfLook.Lit(HoneyLight, .38f, .02f, HoneyLight * .055f);
            amberMat = MgfLook.Lit(Amber, .20f, .01f);
            creamMat = MgfLook.Lit(Cream, .35f, .01f);
            raspberryMat = MgfLook.Lit(Raspberry, .45f, .04f, Raspberry * .05f);
            inkMat = MgfLook.Lit(Ink, .14f, .01f);
            leafMat = MgfLook.Lit(Leaf, .18f, .01f);
            glowMat = MgfLook.Lit(new Color32(255, 216, 88, 255), .55f, .05f, new Color(.16f, .08f, .005f));

            worldRoot = new GameObject("Densely inhabited wax hive").transform;
            MgfLook.Block("Wax back wall", new Vector3(0, 3.2f, 4.8f), new Vector3(24, 10.8f, 1.15f), 1.6f, waxMat, worldRoot);
            MgfLook.Block("Wax floor", new Vector3(0, -.45f, 1.5f), new Vector3(24, .9f, 13f), 1.4f, waxLightMat, worldRoot);

            // 깊이가 다른 둥근 밀랍 방. 화면의 황금 밀랍 면적을 가장 크게 유지한다.
            for (int i = 0; i < 42; i++)
            {
                int col = i % 7, row = i / 7;
                float x = -8.0f + col * 2.65f + (row % 2) * 1.28f;
                float y = .8f + row * 1.58f;
                float z = 4.05f - (i % 3) * .16f;
                var chamber = MgfLook.Prim(PrimitiveType.Sphere, "Rounded honey chamber " + i,
                    new Vector3(x, y, z), new Vector3(1.17f, 1.02f, .42f),
                    i % 5 == 0 ? amberMat : (i % 3 == 0 ? waxLightMat : waxMat), worldRoot, false);
                chamber.transform.localRotation = Quaternion.Euler(0, 0, (i % 2) * 12f);
                if (i % 4 == 0)
                    MgfLook.Prim(PrimitiveType.Sphere, "Honey pool " + i, new Vector3(x, y - .14f, z - .40f),
                        new Vector3(.56f, .40f, .10f), glowMat, worldRoot, false);
            }

            archRoot = new GameObject("Carved entrance arch").transform; archRoot.SetParent(worldRoot, false);
            MgfLook.Block("Arch left", new Vector3(-5.9f, 2.8f, .3f), new Vector3(2.0f, 7.4f, 2.2f), .95f, waxLightMat, archRoot);
            MgfLook.Block("Arch right", new Vector3(5.9f, 2.8f, .3f), new Vector3(2.0f, 7.4f, 2.2f), .95f, waxLightMat, archRoot);
            MgfLook.Block("Arch crown", new Vector3(0, 6.2f, .3f), new Vector3(10.6f, 1.8f, 2.2f), .9f, waxLightMat, archRoot);
            for (int i = 0; i < 9; i++)
            {
                float x = -4.6f + i * 1.15f;
                MgfLook.Prim(PrimitiveType.Sphere, "Crown bead " + i, new Vector3(x, 6.35f + Mathf.Cos(i * .7f) * .20f, -1.0f),
                    Vector3.one * .28f, i % 3 == 0 ? raspberryMat : amberMat, archRoot, false);
            }

            // 매달린 꽃 선반과 꿀방울 배관.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 6.7f;
                MgfLook.Block("Hanging sample shelf", new Vector3(x, 4.4f, -1.2f), new Vector3(3.2f, .32f, 1.8f), .22f, amberMat, worldRoot);
                MgfLook.Prim(PrimitiveType.Cylinder, "Honey pipe", new Vector3(x, 6.2f, -1.0f), new Vector3(.12f, 2.0f, .12f), amberMat, worldRoot, false);
                for (int j = 0; j < 4; j++)
                {
                    float sx = x + (j - 1.5f) * .62f;
                    MgfLook.Prim(PrimitiveType.Sphere, "Flower sample", new Vector3(sx, 4.86f, -1.28f),
                        new Vector3(.32f, .22f, .32f), j % 2 == 0 ? raspberryMat : creamMat, worldRoot, false);
                    MgfLook.Prim(PrimitiveType.Cylinder, "Sample stem", new Vector3(sx, 4.62f, -1.22f),
                        new Vector3(.035f, .26f, .035f), leafMat, worldRoot, false);
                }
            }

            for (int i = 0; i < honeyDrops.Length; i++)
            {
                float x = -7.2f + (i % 9) * 1.8f;
                float y = 7.2f - (i / 9) * 2.4f - (i % 3) * .42f;
                dropHomes[i] = new Vector3(x, y, 3.15f);
                honeyDrops[i] = MgfLook.Prim(PrimitiveType.Sphere, "Slow honey drop " + i, dropHomes[i],
                    new Vector3(.13f, .30f, .13f), glowMat, worldRoot, false).transform;
            }

            for (int i = 0; i < bees.Length; i++)
            {
                beeHomes[i] = new Vector3(-5.2f + (i % 3) * 5.2f, .65f + (i / 3) * 2.45f, -1.2f + (i % 2) * .45f);
                bees[i] = BuildBee(i, beeHomes[i]);
            }
        }

        Transform BuildBee(int index, Vector3 position)
        {
            var root = new GameObject("Plump tasting bee " + index).transform;
            root.SetParent(worldRoot, false); root.localPosition = position;
            var body = MgfLook.Prim(PrimitiveType.Sphere, "Amber body", Vector3.zero,
                new Vector3(.72f, .55f, .58f), waxLightMat, root, false);
            body.transform.localPosition = Vector3.zero;
            MgfLook.Block("Dark stripe 1", new Vector3(-.16f, 0, -.52f), new Vector3(.12f, .55f, .09f), .05f, inkMat, root);
            MgfLook.Block("Dark stripe 2", new Vector3(.18f, 0, -.52f), new Vector3(.12f, .53f, .09f), .05f, inkMat, root);
            MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0, .05f, -.55f), new Vector3(.43f, .42f, .39f), amberMat, root, false);
            for (int side = -1; side <= 1; side += 2)
            {
                MgfLook.Prim(PrimitiveType.Sphere, "Eye", new Vector3(side * .14f, .13f, -.88f),
                    new Vector3(.055f, .07f, .045f), inkMat, root, false);
                MgfLook.Prim(PrimitiveType.Sphere, "Cheek", new Vector3(side * .23f, -.04f, -.86f),
                    new Vector3(.09f, .055f, .035f), raspberryMat, root, false);
            }
            beeWingsL[index] = MgfLook.Prim(PrimitiveType.Sphere, "Cream wing L", new Vector3(-.55f, .28f, .04f),
                new Vector3(.48f, .12f, .30f), creamMat, root, false).transform;
            beeWingsR[index] = MgfLook.Prim(PrimitiveType.Sphere, "Cream wing R", new Vector3(.55f, .28f, .04f),
                new Vector3(.48f, .12f, .30f), creamMat, root, false).transform;
            return root;
        }

        void BuildUi()
        {
            softDisc = MakeDisc(false); ringSprite = MakeDisc(true); waxPanel = MakeWaxPanel();
            uiRoot = Full("Honey UI", MgfText.Canvas.transform);

            titleRoot = Full("Title", uiRoot); titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
            titleLogo = Txt("Carved logo", titleRoot, "꿀  도 둑", new Vector2(.5f, .76f), Vector2.zero, 55, Cream, 360);
            titleLogo.outlineColor = Amber; titleLogo.outlineWidth = .24f;
            titleTag = Txt("Tagline", titleRoot, "시식 벌이 사는 밀랍 절단 통로", new Vector2(.5f, .67f), Vector2.zero, 18, Ink, 340);
            var metaPlate = Img("Specimen tag", titleRoot, new Vector2(.5f, .57f), Vector2.zero, new Vector2(292, 72), Cream, waxPanel);
            titleMeta = Txt("Meta", metaPlate.transform, "중학교 2학년 · 경우의 수\n8회 시식 · 첫 절단 6회", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 270);
            titleSealL = Img("Seal half L", titleRoot, new Vector2(.5f, .24f), new Vector2(-39, 0), new Vector2(78, 128), Raspberry, waxPanel).rectTransform;
            titleSealR = Img("Seal half R", titleRoot, new Vector2(.5f, .24f), new Vector2(39, 0), new Vector2(78, 128), Raspberry, waxPanel).rectTransform;
            titleSealRing = Img("Seal ring", titleRoot, new Vector2(.5f, .24f), Vector2.zero, new Vector2(176, 176), Cream, ringSprite).rectTransform;
            titleSealText = Txt("Seal CTA", titleRoot, "봉인 열기", new Vector2(.5f, .24f), Vector2.zero, 21, Cream, 155);

            gameRoot = Full("Game", uiRoot); gameGroup = gameRoot.gameObject.AddComponent<CanvasGroup>();
            var hudPlate = Img("Wax HUD", gameRoot, new Vector2(.5f, .955f), Vector2.zero, new Vector2(326, 52), Cream, waxPanel);
            hudText = Txt("HUD text", hudPlate.transform, "", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 306);
            goalText = Txt("Persistent goal", gameRoot, "8회 시식 · 첫 절단 6회 · 봉인 3개", new Vector2(.5f, .895f), Vector2.zero, 14, Cream, 350);
            var promptPlate = Img("Prompt tag", gameRoot, new Vector2(.5f, .80f), Vector2.zero, new Vector2(354, 108), Cream, waxPanel);
            promptText = Txt("Prompt", promptPlate.transform, "", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 330);
            promptText.rectTransform.sizeDelta = new Vector2(330, 94);
            var leftPlate = Img("Left shelf tag", gameRoot, new Vector2(.24f, .69f), Vector2.zero, new Vector2(150, 62), HoneyLight, waxPanel);
            var rightPlate = Img("Right shelf tag", gameRoot, new Vector2(.76f, .69f), Vector2.zero, new Vector2(150, 62), HoneyLight, waxPanel);
            leftShelfText = Txt("Left shelf", leftPlate.transform, "", new Vector2(.5f, .5f), Vector2.zero, 14, Ink, 138);
            rightShelfText = Txt("Right shelf", rightPlate.transform, "", new Vector2(.5f, .5f), Vector2.zero, 14, Ink, 138);

            for (int i = 0; i < 24; i++)
            {
                cellImage[i] = Img("Honeycomb ruler cell " + (i + 1), gameRoot, new Vector2(.5f, .3f), Vector2.zero,
                    new Vector2(44, 15), i % 2 == 0 ? HoneyLight : Honey, waxPanel);
                cellRt[i] = cellImage[i].rectTransform;
                tickText[i] = Txt("Tick " + (i + 1), gameRoot, (i + 1).ToString(), new Vector2(.5f, .3f), Vector2.zero, 10, Cream, 34);
                tickText[i].gameObject.SetActive((i + 1) == 1 || (i + 1) % 4 == 0);
            }
            bladeImage = Img("Wax knife blade", gameRoot, new Vector2(.5f, .3f), Vector2.zero, new Vector2(174, 10), Cream, waxPanel);
            bladeRt = bladeImage.rectTransform;
            handleImage = Img("Raspberry knife handle", gameRoot, new Vector2(.5f, .3f), Vector2.zero, new Vector2(72, 72), Raspberry, softDisc);
            handleRt = handleImage.rectTransform;
            guideRing = Img("Guide on actual handle", gameRoot, new Vector2(.5f, .3f), Vector2.zero, new Vector2(102, 102), Cream, ringSprite);
            guideRt = guideRing.rectTransform;
            selectedText = Txt("Immediate integer lens", gameRoot, "1칸", new Vector2(.5f, .3f), Vector2.zero, 21, Ink, 80);
            selectedText.outlineColor = Cream; selectedText.outlineWidth = .18f;
            cutTrailRt = Img("Knife honey trail", gameRoot, new Vector2(.5f, .3f), Vector2.zero, new Vector2(10, 10),
                new Color(Raspberry.r, Raspberry.g, Raspberry.b, .65f), waxPanel).rectTransform;
            cutTrailRt.gameObject.SetActive(false);

            var recordPlate = Img("Cut record tray", gameRoot, new Vector2(.5f, .12f), Vector2.zero, new Vector2(350, 100), Cream, waxPanel);
            recordPlateRt = recordPlate.rectTransform;
            recordText = Txt("First cut record", recordPlate.transform, "", new Vector2(.5f, .64f), Vector2.zero, 14, Ink, 328);
            instructionText = Txt("One action instruction", recordPlate.transform, "손잡이를 눈금으로 옮기고, 띠를 가로질러 베시오.",
                new Vector2(.5f, .26f), Vector2.zero, 13, Amber, 330);

            revealRoot = Full("Reveal", uiRoot); revealGroup = revealRoot.gameObject.AddComponent<CanvasGroup>();
            revealPlate = Img("Evidence tag", revealRoot, new Vector2(.5f, .18f), Vector2.zero, new Vector2(356, 136), Cream, waxPanel);
            revealTitle = Txt("Reveal heading", revealPlate.transform, "", new Vector2(.5f, .72f), Vector2.zero, 20, Ink, 330);
            revealText = Txt("Reveal formula", revealPlate.transform, "", new Vector2(.5f, .37f), Vector2.zero, 15, Amber, 330);
            for (int i = 0; i < sampleCards.Length; i++)
            {
                sampleImages[i] = Img("Spread specimen " + i, revealRoot, new Vector2(.5f, .31f), Vector2.zero,
                    new Vector2(34, 42), i % 2 == 0 ? Raspberry : HoneyLight, waxPanel);
                sampleCards[i] = sampleImages[i].rectTransform;
            }

            rippleImage = Img("Tap wax ripple", uiRoot, new Vector2(.5f, .5f), Vector2.zero, new Vector2(70, 70), Cream, ringSprite);
            rippleRt = rippleImage.rectTransform; rippleImage.color = new Color(Cream.r, Cream.g, Cream.b, 0);
            toastText = Txt("Reason toast", uiRoot, "", new Vector2(.5f, .08f), Vector2.zero, 14, Cream, 350);

            endRoot = Full("End", uiRoot); endGroup = endRoot.gameObject.AddComponent<CanvasGroup>();
            endTitle = Txt("Result title", endRoot, "", new Vector2(.5f, .75f), Vector2.zero, 48, Cream, 370);
            endTitle.outlineColor = Amber; endTitle.outlineWidth = .20f;
            var resultPlate = Img("Result medallion", endRoot, new Vector2(.5f, .52f), Vector2.zero, new Vector2(328, 176), Cream, waxPanel);
            endStats = Txt("Animated result stats", resultPlate.transform, "", new Vector2(.5f, .64f), Vector2.zero, 22, Ink, 300);
            endReasonText = Txt("End reason", resultPlate.transform, "", new Vector2(.5f, .27f), Vector2.zero, 15, Amber, 300);
            restartSeal = Img("Restart wax seal", endRoot, new Vector2(.5f, .23f), Vector2.zero, new Vector2(168, 128), Raspberry, waxPanel).rectTransform;
            restartText = Txt("Restart CTA", endRoot, "다시 봉인 열기", new Vector2(.5f, .23f), Vector2.zero, 19, Cream, 160);

            LayoutUi();
        }

        bool UpdateLayoutIfNeeded()
        {
            bool land = IsLand();
            if (layoutW == Screen.width && layoutH == Screen.height && layoutLand == land) return false;
            layoutW = Screen.width; layoutH = Screen.height; layoutLand = land; LayoutUi();
            return true;
        }

        void LayoutUi()
        {
            if (uiRoot == null) return;
            layoutLand = IsLand();
            if (!layoutLand)
            {
                titleLogo.rectTransform.anchorMin = titleLogo.rectTransform.anchorMax = new Vector2(.5f, .76f);
                titleTag.rectTransform.anchorMin = titleTag.rectTransform.anchorMax = new Vector2(.5f, .67f);
                titleSealL.anchorMin = titleSealL.anchorMax = new Vector2(.5f, .24f);
                titleSealR.anchorMin = titleSealR.anchorMax = new Vector2(.5f, .24f);
                titleSealRing.anchorMin = titleSealRing.anchorMax = new Vector2(.5f, .24f);
                titleSealText.rectTransform.anchorMin = titleSealText.rectTransform.anchorMax = new Vector2(.5f, .24f);
                promptText.transform.parent.GetComponent<RectTransform>().anchorMin = promptText.transform.parent.GetComponent<RectTransform>().anchorMax = new Vector2(.5f, .80f);
                leftShelfText.transform.parent.GetComponent<RectTransform>().anchorMin = leftShelfText.transform.parent.GetComponent<RectTransform>().anchorMax = new Vector2(.24f, .69f);
                rightShelfText.transform.parent.GetComponent<RectTransform>().anchorMin = rightShelfText.transform.parent.GetComponent<RectTransform>().anchorMax = new Vector2(.76f, .69f);
                revealPlate.rectTransform.anchorMin = revealPlate.rectTransform.anchorMax = new Vector2(.5f, .18f);
                restartSeal.anchorMin = restartSeal.anchorMax = new Vector2(.5f, .23f);
                restartText.rectTransform.anchorMin = restartText.rectTransform.anchorMax = new Vector2(.5f, .23f);
            }
            else
            {
                titleLogo.rectTransform.anchorMin = titleLogo.rectTransform.anchorMax = new Vector2(.5f, .79f);
                titleTag.rectTransform.anchorMin = titleTag.rectTransform.anchorMax = new Vector2(.5f, .68f);
                titleSealL.anchorMin = titleSealL.anchorMax = new Vector2(.5f, .34f);
                titleSealR.anchorMin = titleSealR.anchorMax = new Vector2(.5f, .34f);
                titleSealRing.anchorMin = titleSealRing.anchorMax = new Vector2(.5f, .34f);
                titleSealText.rectTransform.anchorMin = titleSealText.rectTransform.anchorMax = new Vector2(.5f, .34f);
                promptText.transform.parent.GetComponent<RectTransform>().anchorMin = promptText.transform.parent.GetComponent<RectTransform>().anchorMax = new Vector2(.5f, .84f);
                leftShelfText.transform.parent.GetComponent<RectTransform>().anchorMin = leftShelfText.transform.parent.GetComponent<RectTransform>().anchorMax = new Vector2(.22f, .69f);
                rightShelfText.transform.parent.GetComponent<RectTransform>().anchorMin = rightShelfText.transform.parent.GetComponent<RectTransform>().anchorMax = new Vector2(.78f, .69f);
                revealPlate.rectTransform.anchorMin = revealPlate.rectTransform.anchorMax = new Vector2(.5f, .15f);
                restartSeal.anchorMin = restartSeal.anchorMax = new Vector2(.5f, .30f);
                restartText.rectTransform.anchorMin = restartText.rectTransform.anchorMax = new Vector2(.5f, .30f);
            }
            PositionTrack();
        }

        void PositionTrack()
        {
            if (cellRt[0] == null) return;
            for (int i = 0; i < 24; i++)
            {
                float t = i / 23f;
                if (layoutLand)
                {
                    Vector2 a = new Vector2(Mathf.Lerp(.22f, .78f, t), .36f);
                    cellRt[i].anchorMin = cellRt[i].anchorMax = a; cellRt[i].sizeDelta = new Vector2(42, 38);
                    tickText[i].rectTransform.anchorMin = tickText[i].rectTransform.anchorMax = new Vector2(a.x, .29f);
                }
                else
                {
                    Vector2 a = new Vector2(.5f, Mathf.Lerp(.22f, .67f, t));
                    cellRt[i].anchorMin = cellRt[i].anchorMax = a; cellRt[i].sizeDelta = new Vector2(78, 18);
                    tickText[i].rectTransform.anchorMin = tickText[i].rectTransform.anchorMax = new Vector2(.37f, a.y);
                }
                cellRt[i].anchoredPosition = Vector2.zero; tickText[i].rectTransform.anchoredPosition = Vector2.zero;
            }
            MoveHandleVisual(st.selectedCellCount <= 0 ? 1 : st.selectedCellCount);
        }

        void MoveHandleVisual(int value)
        {
            if (handleRt == null) return;
            float t = (Mathf.Clamp(value, 1, 24) - 1) / 23f;
            Vector2 a = layoutLand ? new Vector2(Mathf.Lerp(.22f, .78f, t), .36f) : new Vector2(.5f, Mathf.Lerp(.22f, .67f, t));
            handleRt.anchorMin = handleRt.anchorMax = a; handleRt.anchoredPosition = Vector2.zero;
            bladeRt.anchorMin = bladeRt.anchorMax = a; bladeRt.anchoredPosition = Vector2.zero;
            guideRt.anchorMin = guideRt.anchorMax = a; guideRt.anchoredPosition = Vector2.zero;
            selectedText.rectTransform.anchorMin = selectedText.rectTransform.anchorMax = a;
            selectedText.rectTransform.anchoredPosition = layoutLand ? new Vector2(0, 62) : new Vector2(77, 0);
            if (layoutLand) { bladeRt.sizeDelta = new Vector2(10, 142); bladeRt.localRotation = Quaternion.identity; }
            else { bladeRt.sizeDelta = new Vector2(174, 10); bladeRt.localRotation = Quaternion.identity; }
            selectedText.text = value + "칸"; handlePunch = .16f;
        }

        void ResetWorldForRun()
        {
            correctFxClock = wrongFxClock = beeReactClock = 9f; hitStopClock = 0f; shownScore = 0;
            if (cam) { cam.transform.position = cameraBase; cam.transform.rotation = Quaternion.LookRotation(new Vector3(0, 2.1f, 1.5f) - cameraBase); }
            for (int i = 0; i < bees.Length; i++) if (bees[i]) bees[i].localPosition = beeHomes[i];
        }

        void ShowTitleVisual()
        {
            titleSealL.anchoredPosition = new Vector2(-39, 0); titleSealR.anchoredPosition = new Vector2(39, 0);
            titleSealL.localRotation = titleSealR.localRotation = Quaternion.identity;
            titleSealText.text = "봉인 열기"; guideBoost = 1f;
        }

        void BeginSealSplit()
        {
            titleSealText.text = "벌통 여는 중"; guideBoost = 1.4f; ReactBees(true);
        }

        void StartProblemVisual(HoneyProblem p, bool practice, int selected)
        {
            promptText.text = practice
                ? "연습. 꽃꿀 2종 또는 과일꿀 3종 중 한 종류만 고른다. 경우의 수를 구하시오."
                : p.prompt;
            leftShelfText.text = p.leftName + "\n" + p.m + "종";
            rightShelfText.text = p.rightName + "\n" + p.n + "종";
            goalText.text = practice ? "압박 없음 · 정답을 낼 때까지 연습" : "8회 시식 · 첫 절단 6회 · 봉인 " + st.lives + "개";
            instructionText.text = layoutLand
                ? "분홍 손잡이를 눈금으로 옮기고 아래로 그어 베시오."
                : "분홍 손잡이를 눈금으로 옮기고 옆으로 그어 베시오.";
            for (int i = 0; i < 24; i++) cellImage[i].color = i % 2 == 0 ? HoneyLight : Honey;
            MoveHandleVisual(selected); guideBoost = practice ? 1.3f : .45f; RefreshHud();
        }

        void RefreshHud()
        {
            if (hudText == null) return;
            int sec = Mathf.Max(0, Mathf.CeilToInt(sessionLeft));
            hudText.text = "의뢰 " + Mathf.Min(KkulDodukRules.Goal, st.solved + 1) + "/8   봉인 " + st.lives + "   " + sec + "초";
            recordText.text = "첫 절단 " + st.firstCorrect + "/" + st.firstAttempted + "  ·  정확도 " + st.firstAccuracy + "%  ·  점수 " + Mathf.RoundToInt(shownScore);
        }

        void SetScreen()
        {
            if (titleGroup == null) return;
            titleGroup.alpha = phase == GamePhase.Title ? 1f : 0f;
            gameGroup.alpha = (phase == GamePhase.Practice || phase == GamePhase.Playing || phase == GamePhase.Reveal) ? 1f : 0f;
            revealGroup.alpha = phase == GamePhase.Reveal ? 1f : 0f;
            endGroup.alpha = phase == GamePhase.End ? 1f : 0f;
            titleRoot.gameObject.SetActive(titleGroup.alpha > 0);
            gameRoot.gameObject.SetActive(gameGroup.alpha > 0);
            revealRoot.gameObject.SetActive(revealGroup.alpha > 0);
            endRoot.gameObject.SetActive(endGroup.alpha > 0);
            if (recordPlateRt != null) recordPlateRt.gameObject.SetActive(phase != GamePhase.Reveal);
        }

        void BeginHandleDrag()
        {
            handlePunch = .24f; guideBoost = .2f;
        }

        void CancelHandleDrag()
        {
            handlePunch = .20f; cutTrailClock = .12f;
        }

        void UpdateCutTrail(Vector2 start, Vector2 now, bool ended)
        {
            if (cutTrailRt == null) return;
            // Screen 좌표를 Overlay Rect 좌표로 변환한다. CanvasScaler와 같은 기하 평균 스케일을 쓴다.
            float scale = Mathf.Max(.01f, ReferenceScale());
            Vector2 delta = (now - start) / scale;
            float len = delta.magnitude;
            cutTrailRt.gameObject.SetActive(len > 5f);
            cutTrailRt.anchorMin = cutTrailRt.anchorMax = new Vector2(.5f, .5f);
            Vector2 midScreen = (start + now) * .5f;
            cutTrailRt.anchoredPosition = new Vector2((midScreen.x - Screen.width * .5f) / scale, (midScreen.y - Screen.height * .5f) / scale);
            cutTrailRt.sizeDelta = new Vector2(Mathf.Max(8, len), 9);
            cutTrailRt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            cutTrailClock = ended ? .18f : 0f;
        }

        void BeginCorrectReveal(HoneyProblem p, int submitted, bool practice)
        {
            revealTitle.text = practice ? "첫 절단 성공" : "빈 칸 없이 시식 완료";
            revealText.text = p.formula + "가지\n" + (p.kind == HoneyKind.Sum ? "한 종류만 골라 두 군의 수를 더한다." : "각각 하나씩 골라 묶음 수를 곱한다.");
            correctFxClock = 0f; hitStopClock = .075f; guideBoost = 0f;
            for (int i = 0; i < sampleCards.Length; i++)
            {
                sampleCards[i].gameObject.SetActive(i < Mathf.Min(8, p.answer));
                sampleCards[i].anchoredPosition = Vector2.zero; sampleCards[i].localScale = Vector3.one * .35f;
            }
            ReactBees(true); handlePunch = .34f;
        }

        void BeginWrongReveal(HoneyProblem p, int submitted, string misconception, bool practice)
        {
            revealTitle.text = practice ? "봉인은 그대로 · 다시 해 보시오" : "" + submitted + "칸에는 견본이 남는다";
            switch (misconception)
            {
                case "sum_used_product": revealText.text = "한 종류만 고르므로 곱하지 않는다. 두 군의 수를 더하시오."; break;
                case "product_used_sum": revealText.text = "각각 하나씩 고르므로 " + p.m + "묶음마다 " + p.n + "가지를 세시오."; break;
                case "one_group_only": revealText.text = "한 묶음만 세지 말고 모든 꿀과 용기의 짝을 세시오."; break;
                case "roles_merged": revealText.text = "꿀과 용기는 역할이 다르므로 바뀐 짝도 따로 센다."; break;
                case "difference_only": revealText.text = "두 군의 차가 아니라, 겹치지 않는 모든 종류를 세시오."; break;
                default: revealText.text = "선택 조건을 확인하고 1~24 눈금에서 다시 자르시오."; break;
            }
            wrongFxClock = 0f; guideBoost = .5f; ReactBees(false); handlePunch = .25f;
            for (int i = 0; i < sampleCards.Length; i++) sampleCards[i].gameObject.SetActive(false);
        }

        void CompleteRevealVisual(bool correct)
        {
            for (int i = 0; i < sampleCards.Length; i++) sampleCards[i].gameObject.SetActive(false);
            if (!correct) for (int i = 0; i < 24; i++) cellImage[i].color = i % 2 == 0 ? HoneyLight : Honey;
        }

        void ResumeProblemAfterWrong()
        {
            revealGroup.alpha = 0; revealRoot.gameObject.SetActive(false);
            goalText.text = phase == GamePhase.Practice ? "정답을 낼 때까지 봉인은 줄지 않는다" :
                "같은 의뢰 수정 중 · 봉인 " + st.lives + "개 · 첫 절단 기록은 유지";
            instructionText.text = "조건을 다시 보고 손잡이를 옮긴 뒤 가로질러 베시오.";
        }

        void ShowEndVisual(bool clear, string reason)
        {
            endTitle.text = clear ? "꿀빛 통로 완성" : "봉인 통로가 멈췄다";
            endStats.text = "시식 " + st.solved + "/8\n첫 절단 " + st.firstCorrect + "/" + st.firstAttempted + " · " + st.firstAccuracy + "%";
            endReasonText.text = reason + "\n점수 " + st.score;
            shownScore = 0f; restartText.text = "다시 봉인 열기"; ReactBees(clear);
        }

        void ShowWrongReason(string text) { RefuseAt(HandleScreenPosition(st.selectedCellCount), text); }

        void BoostGuide() { guideBoost = Mathf.Max(guideBoost, 1.25f); }
        void PointToHandle() { guideBoost = Mathf.Max(guideBoost, 1.1f); }

        void RippleAt(Vector2 screen)
        {
            rippleScreen = screen; rippleClock = 0f;
        }

        void RefuseAt(Vector2 screen, string message)
        {
            RippleAt(screen); toastCache = message; toastText.text = message; toastClock = 0f;
            handlePunch = .16f; MgfSfx.Play("tap", .09f);
        }

        void ReactBees(bool happy)
        {
            beeReactClock = happy ? 0f : -.01f;
        }

        void UpdateWorldVisual(float dt)
        {
            if (hitStopClock > 0f) { hitStopClock -= dt; return; }
            worldClock += dt;
            for (int i = 0; i < bees.Length; i++)
            {
                if (!bees[i]) continue;
                float phaseOffset = i * .83f;
                float react = beeReactClock < .7f ? Mathf.Sin(Mathf.Clamp01(beeReactClock / .7f) * Mathf.PI) : 0f;
                float sad = beeReactClock < 0f ? 1f : 0f;
                bees[i].localPosition = beeHomes[i] + new Vector3(Mathf.Sin(worldClock * .9f + phaseOffset) * .14f,
                    Mathf.Sin(worldClock * 1.7f + phaseOffset) * .16f + react * (sad > 0 ? -.22f : .35f), 0);
                float wing = Mathf.Sin(worldClock * 14f + phaseOffset) * 24f;
                beeWingsL[i].localRotation = Quaternion.Euler(0, 0, 18f + wing);
                beeWingsR[i].localRotation = Quaternion.Euler(0, 0, -18f - wing);
            }
            if (beeReactClock < 9f) beeReactClock += dt;
            for (int i = 0; i < honeyDrops.Length; i++)
            {
                float y = dropHomes[i].y - Mathf.Repeat(worldClock * (.16f + (i % 3) * .025f) + i * .21f, 1.25f);
                honeyDrops[i].localPosition = new Vector3(dropHomes[i].x, y, dropHomes[i].z);
            }
            if (wrongFxClock < .55f)
            {
                wrongFxClock += dt;
                float amp = (1f - wrongFxClock / .55f) * .14f;
                cam.transform.position = cameraBase + new Vector3(Mathf.Sin(wrongFxClock * 70f) * amp, 0, 0);
            }
            else if (cam) cam.transform.position = Vector3.Lerp(cam.transform.position, cameraBase, Mathf.Clamp01(dt * 9f));
            if (correctFxClock < 9f) correctFxClock += dt;
        }

        void UpdateUiVisual(float dt)
        {
            if (titleOpening)
            {
                float t = Mathf.Clamp01(titleClock / .38f);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                titleSealL.anchoredPosition = new Vector2(-39 - ease * 78, Mathf.Sin(t * Mathf.PI) * 9);
                titleSealR.anchoredPosition = new Vector2(39 + ease * 78, Mathf.Sin(t * Mathf.PI) * 9);
                titleSealL.localRotation = Quaternion.Euler(0, 0, -ease * 18);
                titleSealR.localRotation = Quaternion.Euler(0, 0, ease * 18);
            }
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * (guideBoost > 1 ? 8f : 4f)) * (.06f + guideBoost * .025f);
            guideRt.localScale = Vector3.one * pulse;
            guideRing.color = new Color(Cream.r, Cream.g, Cream.b, phase == GamePhase.Practice ? .82f : Mathf.Clamp01(.16f + guideBoost * .40f));
            guideBoost = Mathf.Max(0f, guideBoost - dt * .22f);

            if (handlePunch > 0f) handlePunch = Mathf.Max(0f, handlePunch - dt);
            float hp = handlePunch > 0f ? Mathf.Sin((1f - handlePunch / .34f) * Mathf.PI) : 0f;
            handleRt.localScale = dragging
                ? new Vector3(layoutLand ? 1.18f : .88f, layoutLand ? .88f : 1.18f, 1)
                : new Vector3(1f + hp * .18f, 1f - hp * .10f, 1);

            if (rippleClock < .28f)
            {
                rippleClock += dt;
                float t = Mathf.Clamp01(rippleClock / .28f), scale = ReferenceScale();
                rippleRt.anchorMin = rippleRt.anchorMax = new Vector2(.5f, .5f);
                rippleRt.anchoredPosition = new Vector2((rippleScreen.x - Screen.width * .5f) / scale, (rippleScreen.y - Screen.height * .5f) / scale);
                rippleRt.localScale = Vector3.one * Mathf.Lerp(.35f, 1.65f, t);
                rippleImage.color = new Color(Cream.r, Cream.g, Cream.b, (1f - t) * .86f);
            }
            else rippleImage.color = new Color(Cream.r, Cream.g, Cream.b, 0);

            if (toastClock < 2.3f)
            {
                toastClock += dt;
                toastText.text = toastCache;
                toastText.color = new Color(Cream.r, Cream.g, Cream.b, toastClock < 1.8f ? 1f : 1f - (toastClock - 1.8f) / .5f);
            }
            else toastText.color = new Color(Cream.r, Cream.g, Cream.b, 0);

            if (cutTrailClock < .22f)
            {
                cutTrailClock += dt;
                var img = cutTrailRt.GetComponent<Image>();
                if (img) img.color = new Color(Raspberry.r, Raspberry.g, Raspberry.b, Mathf.Clamp01(1f - cutTrailClock / .22f) * .72f);
                if (cutTrailClock >= .22f) cutTrailRt.gameObject.SetActive(false);
            }

            if (phase == GamePhase.Reveal && revealCorrect && current != null)
            {
                float t = Mathf.Clamp01(correctFxClock / 1.15f);
                int filled = Mathf.Min(current.answer, Mathf.FloorToInt(t * (current.answer + 2)));
                for (int i = 0; i < 24; i++) cellImage[i].color = i < filled ? Raspberry : (i % 2 == 0 ? HoneyLight : Honey);
                for (int i = 0; i < sampleCards.Length; i++)
                {
                    float local = Mathf.Clamp01((correctFxClock - i * .055f) / .30f);
                    float eased = 1f - Mathf.Pow(1f - local, 3f);
                    sampleCards[i].anchoredPosition = new Vector2((i - 3.5f) * 38f * eased, Mathf.Sin(i * 1.7f) * 7f);
                    sampleCards[i].localScale = Vector3.one * Mathf.Lerp(.35f, 1f + Mathf.Sin(local * Mathf.PI) * .14f, local);
                }
            }

            if (phase == GamePhase.End)
            {
                shownScore = Mathf.MoveTowards(shownScore, st.score, Mathf.Max(35f, st.score * 1.8f) * dt);
                endStats.text = "시식 " + st.solved + "/8\n첫 절단 " + st.firstCorrect + "/" + st.firstAttempted + " · " + st.firstAccuracy + "%";
            }
            else shownScore = Mathf.MoveTowards(shownScore, st.score, Mathf.Max(20f, st.score * 2.5f) * dt);

            if (phase == GamePhase.Playing || phase == GamePhase.Practice) RefreshHud();
        }
    }
}
