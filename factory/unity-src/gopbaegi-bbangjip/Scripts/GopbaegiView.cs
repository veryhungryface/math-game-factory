// 곱빼기 빵집 — 햇빛 아래 무광 장난감 제빵 공방, 반응형 UI와 피드백 연출.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.GopbaegiBbangjip
{
    public partial class GopbaegiGame
    {
        static readonly Color Violet = MgfLook.Hex("#6F9F86");   // 세이지 기계 몸체
        static readonly Color Coral = MgfLook.Hex("#DF7351");    // 반죽·테라코타
        static readonly Color Mint = MgfLook.Hex("#F2C451");     // 구운 빵·버터색
        static readonly Color Cream = MgfLook.Hex("#FFF2D3");
        static readonly Color Iris = MgfLook.Hex("#6DB6B0");     // 맑은 창문색
        static readonly Color Ink = MgfLook.Hex("#3A2D29");
        static readonly Color Nickel = MgfLook.Hex("#B96E47");   // 무광 구리

        Camera cam;
        Vector3 cameraHome;
        Transform worldRoot, towerRoot, cupRoot, dough, stream, leverWorld, knobWorld, cellRoot, wasteCoil;
        readonly Transform[] cells = new Transform[64];
        readonly Renderer[] cellRender = new Renderer[64];
        readonly Transform[] yeast = new Transform[3];
        readonly Transform[] bubbles = new Transform[18];
        readonly Renderer[] stages = new Renderer[9];
        Material violetMat, coralMat, mintMat, creamMat, nickelMat, darkMat, glassMat, glowMat, emptyMat;
        float worldTime, wrongShake, towerDisplayY, towerTargetY, wasteVisual, drumPulse;
        int revealCellCount, revealFilledCount;

        CanvasGroup titleG, hudG, revealG, endG;
        RectTransform uiRoot, leverRt, leverKnobRt, guideRingRt, rippleRt, promptCardRt, altCardRt, revealRt, endRt, toastRt;
        RectTransform fineMinusRt, fineSealRt, finePlusRt;
        RectTransform holdCoachRt, holdCoachFillRt, holdCoachFingerRt;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleLeverText, holdCoachText;
        TextMeshProUGUI goalText, promptText, altText, amountText, scoreText, livesText, stockText, progressText, timerText;
        TextMeshProUGUI revealText, revealDetail, endTitle, endStats, endCta, toastText;
        Image leverBase, leverKnob, guideRing, ripple, promptCard, altCard, revealPanel, endPanel, holdCoachFill, holdCoachFinger;
        Sprite roundSprite, ringSprite, diamondSprite;
        float displayedScore, endDisplayedScore, toastClock, rippleClock, guideBoost, titleClock;
        int shownAmount = -1, shownStock = -1, shownLives = -1, shownSolved = -1, coachStep = -1;
        int lastUiWidth = -1, lastUiHeight = -1;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size); var img = rt.gameObject.AddComponent<Image>();
            img.color = color; img.sprite = sprite; img.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        TextMeshProUGUI Txt(Transform parent, string value, Vector2 anchor, Vector2 pos, float size, Color color,
            float width = 360f, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 3.0f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size;
            t.color = color; t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal;
            t.text = value; return t;
        }

        static Sprite MakeShape(int size, float radius, int kind)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "GopbaegiUi", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size, v = (y + .5f) / size, a;
                if (kind == 1)
                {
                    float d = Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f));
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - .36f) * size * .34f);
                }
                else if (kind == 2)
                {
                    float q = Mathf.Abs(u - .5f) + Mathf.Abs(v - .5f);
                    a = Mathf.Clamp01((.45f - q) * size * .16f);
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(x + .5f - size / 2f) - (size / 2f - radius), 0f);
                    float qy = Mathf.Max(Mathf.Abs(y + .5f - size / 2f) - (size / 2f - radius), 0f);
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            float b = kind == 0 ? radius + 2 : 0;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect,
                new Vector4(b, b, b, b));
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#8ED4D2"), MgfLook.Hex("#F7D7A4"), MgfLook.Hex("#D88763"), .68f);
            MgfLook.Sun(new Vector3(48, -32, 14), MgfLook.Hex("#FFF0C6"), 1.32f, .72f);
            cam = MgfLook.Camera(new Vector3(10.5f, 7.4f, -15.8f), new Vector3(0, 3.5f, .8f), 34f);
            cameraHome = cam.transform.position;

            violetMat = MgfLook.Lit(Violet, .30f, .03f);
            coralMat = MgfLook.Lit(Coral, .35f, .01f);
            mintMat = MgfLook.Lit(Mint, .28f, .01f);
            creamMat = MgfLook.Lit(Cream, .24f, .01f);
            nickelMat = MgfLook.Lit(Nickel, .48f, .18f);
            darkMat = MgfLook.Lit(Ink, .22f, .02f);
            emptyMat = MgfLook.Lit(MgfLook.Hex("#B8A98F"), .18f, .01f);
            glassMat = MgfLook.Lit(Iris, .38f, .02f);
            glowMat = MgfLook.Lit(MgfLook.Hex("#F8E7B8"), .18f, .01f);

            worldRoot = new GameObject("SunlitDoughBlockWorkshop").transform;
            MgfLook.Block("BakeryWall", new Vector3(0, 3.6f, 5.4f), new Vector3(22, 10.7f, .65f), .6f, creamMat, worldRoot);
            MgfLook.Block("TerracottaFloor", new Vector3(0, -.7f, 1f), new Vector3(26, 1.1f, 15f), 1.2f, coralMat, worldRoot);
            BuildBackgroundPipes(); BuildTower(); BuildYeast(); BuildBubbles();
        }

        void BuildBackgroundPipes()
        {
            // 열린 빵집 안뜰: 유리 관 대신 무광 진열대·차양·창문이 화면을 채운다.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 7.1f;
                MgfLook.Block("Window", new Vector3(x, 4.8f, 5.0f), new Vector3(4.3f, 6.4f, .22f), .55f, glassMat, worldRoot);
                MgfLook.Block("WindowPost", new Vector3(x, 4.8f, 4.72f), new Vector3(.22f, 6.4f, .28f), .08f, creamMat, worldRoot);
                MgfLook.Block("WindowCross", new Vector3(x, 4.8f, 4.70f), new Vector3(4.3f, .22f, .28f), .08f, creamMat, worldRoot);
                MgfLook.Block("BreadShelf", new Vector3(side * 7.4f, 1.25f, 2.85f), new Vector3(5.0f, .38f, 2.1f), .18f, violetMat, worldRoot);
                for (int i = 0; i < 5; i++)
                {
                    var bun = MgfLook.Block("ShelfLoaf", new Vector3(side * 7.4f - 1.65f + i * .82f, 1.64f, 2.72f),
                        new Vector3(.62f, .48f, .78f), .20f, i % 2 == 0 ? mintMat : coralMat, worldRoot);
                    bun.transform.localRotation = Quaternion.Euler(0, side * 8f, i % 2 == 0 ? 3f : -3f);
                }
            }
            for (int i = 0; i < 9; i++)
            {
                float x = -10.4f + i * 2.6f;
                MgfLook.Block("AwningStripe", new Vector3(x, 8.75f, 3.55f), new Vector3(2.22f, .48f, 3.0f), .16f,
                    i % 2 == 0 ? coralMat : creamMat, worldRoot).transform.localRotation = Quaternion.Euler(8f, 0, 0);
            }
            MgfLook.Block("BackConveyor", new Vector3(0, 2.25f, 4.25f), new Vector3(12.8f, .42f, 1.35f), .20f, nickelMat, worldRoot);
            for (int i = 0; i < 7; i++)
                MgfLook.Block("ConveyorCubeLoaf", new Vector3(-5.4f + i * 1.8f, 2.68f, 4.05f), new Vector3(1.05f, .72f, .92f), .25f,
                    i % 3 == 0 ? mintMat : coralMat, worldRoot).transform.localRotation = Quaternion.Euler(0, i * 7f, 0);
        }

        void BuildTower()
        {
            towerRoot = new GameObject("TowerCore").transform; towerRoot.SetParent(worldRoot, false);
            MgfLook.Block("RollingCart", new Vector3(0, .08f, .8f), new Vector3(7.2f, .62f, 5.7f), .55f, violetMat, towerRoot);
            MgfLook.Block("CartTop", new Vector3(0, .47f, .8f), new Vector3(6.65f, .18f, 5.2f), .08f, creamMat, towerRoot);
            for (int side = -1; side <= 1; side += 2)
                MgfLook.Prim(PrimitiveType.Cylinder, "CartWheel", new Vector3(side * 2.7f, -.24f, -.95f), new Vector3(.48f, .22f, .48f), darkMat, towerRoot, false)
                    .transform.localRotation = Quaternion.Euler(90, 0, 0);
            cupRoot = new GameObject("MeasuringVat").transform; cupRoot.SetParent(towerRoot, false); cupRoot.localPosition = new Vector3(0, .55f, .8f);
            MgfLook.Block("MoldBase", new Vector3(0, .05f, 0), new Vector3(5.5f, .28f, 4.35f), .22f, nickelMat, cupRoot);
            MgfLook.Block("MoldBack", new Vector3(0, 2.55f, 1.98f), new Vector3(5.5f, 5.15f, .28f), .16f, creamMat, cupRoot);
            MgfLook.Block("MoldLeft", new Vector3(-2.62f, 2.55f, 0), new Vector3(.28f, 5.15f, 4.15f), .16f, creamMat, cupRoot);
            MgfLook.Block("MoldRight", new Vector3(2.62f, 2.55f, 0), new Vector3(.28f, 5.15f, 4.15f), .16f, creamMat, cupRoot);
            MgfLook.Block("MoldFrontLip", new Vector3(0, .35f, -1.98f), new Vector3(5.5f, .72f, .28f), .16f, creamMat, cupRoot);
            dough = MgfLook.Block("DoughSlab", new Vector3(0, .12f, 0), new Vector3(4.7f, .04f, 3.55f), .20f, coralMat, cupRoot).transform;
            stream = MgfLook.Prim(PrimitiveType.Cylinder, "DoughRope", new Vector3(0, 6.35f, 0), new Vector3(.52f, 1.25f, .52f), coralMat, cupRoot, false).transform;
            stream.gameObject.SetActive(false);

            // 정면 5 cm³ 보조 눈금을 세계 오브젝트로 새겨, 타이밍 감각이 아니라 읽어서 놓게 한다.
            for (int i = 0; i <= 20; i++)
            {
                float y = .22f + i * .235f; float w = i % 5 == 0 ? .72f : .42f;
                MgfLook.Block("GaugeTick", new Vector3(-2.80f, y, -2.18f), new Vector3(w, .055f, .10f), .018f, darkMat, cupRoot);
            }

            cellRoot = new GameObject("CubicProof").transform; cellRoot.SetParent(towerRoot, false); cellRoot.localPosition = new Vector3(0, 3.55f, -.55f);
            for (int i = 0; i < cells.Length; i++)
            {
                var c = MgfLook.Block("BreadCube" + i, Vector3.zero, Vector3.one * .56f, .15f, mintMat, cellRoot);
                Destroy(c.GetComponent<Collider>()); cells[i] = c.transform; cellRender[i] = c.GetComponent<Renderer>(); c.SetActive(false);
            }

            for (int i = 0; i < stages.Length; i++)
            {
                var loaf = MgfLook.Block("OrderLoaf" + i,
                    new Vector3(4.42f, .62f + i * .57f, 1.52f), new Vector3(1.18f, .43f, 1.0f), .18f, emptyMat, towerRoot);
                stages[i] = loaf.GetComponent<Renderer>();
            }
            wasteCoil = MgfLook.Block("WasteConveyor", new Vector3(-4.55f, .35f, 1.2f), new Vector3(.35f, .25f, .85f), .16f, coralMat, towerRoot).transform;

            leverWorld = new GameObject("PressureLever").transform; leverWorld.SetParent(worldRoot, false); leverWorld.localPosition = new Vector3(4.7f, .2f, -1.4f);
            MgfLook.Block("LeverBase", Vector3.zero, new Vector3(2.0f, .65f, 1.65f), .32f, violetMat, leverWorld);
            var shaft = MgfLook.Prim(PrimitiveType.Cylinder, "LeverShaft", new Vector3(0, 1.2f, 0), new Vector3(.24f, 1.25f, .24f), nickelMat, leverWorld, false);
            shaft.transform.localRotation = Quaternion.Euler(0, 0, -20f);
            knobWorld = MgfLook.Block("LeverKnob", new Vector3(.42f, 2.28f, 0), Vector3.one * .88f, .30f, mintMat, leverWorld).transform;
        }

        Transform MakeYeast(int index)
        {
            var root = new GameObject("StarYeast" + index).transform; root.SetParent(worldRoot, false);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f - Mathf.PI / 2f;
                var lobe = MgfLook.Prim(PrimitiveType.Sphere, "Lobe", new Vector3(Mathf.Cos(a) * .34f, Mathf.Sin(a) * .34f, 0),
                    new Vector3(.38f, .55f, .30f), creamMat, root, false);
                lobe.transform.localRotation = Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg - 90f);
            }
            MgfLook.Prim(PrimitiveType.Sphere, "Core", Vector3.zero, new Vector3(.72f, .68f, .38f), creamMat, root, false);
            for (int s = -1; s <= 1; s += 2)
                MgfLook.Prim(PrimitiveType.Sphere, "Eye", new Vector3(s * .13f, .06f, -.35f), new Vector3(.055f, .08f, .045f), darkMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "BakerCap", new Vector3(0, .40f, -.02f), new Vector3(.55f, .30f, .42f), mintMat, root, false);
            return root;
        }

        void BuildYeast()
        {
            for (int i = 0; i < yeast.Length; i++) yeast[i] = MakeYeast(i);
        }

        void BuildBubbles()
        {
            for (int i = 0; i < bubbles.Length; i++)
            {
                float x = -7.2f + (i * 2.71f) % 14.4f, y = .5f + (i * 1.37f) % 7.8f, z = 3.1f + (i % 3) * .32f;
                bubbles[i] = MgfLook.Prim(PrimitiveType.Sphere, "FlourMote" + i, new Vector3(x, y, z), Vector3.one * (.07f + (i % 4) * .025f), glowMat, worldRoot, false).transform;
            }
        }

        void BuildUi()
        {
            roundSprite = MakeShape(64, 16, 0); ringSprite = MakeShape(64, 0, 1); diamondSprite = MakeShape(64, 0, 2);
            uiRoot = R("GopbaegiUi", MgfText.Canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844));
            uiRoot.anchorMin = Vector2.zero; uiRoot.anchorMax = Vector2.one; uiRoot.offsetMin = uiRoot.offsetMax = Vector2.zero;

            titleG = new GameObject("TitleScreen", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
            titleG.transform.SetParent(uiRoot, false); var titleRoot = (RectTransform)titleG.transform; titleRoot.anchorMin = Vector2.zero; titleRoot.anchorMax = Vector2.one; titleRoot.offsetMin = titleRoot.offsetMax = Vector2.zero;
            // 타이틀도 같은 실시간 3D 공방 위의 차양 간판으로 구성해 플레이 화면과 재질 언어를 잇는다.
            var sigil = Img(titleRoot, "AwningSign", new Vector2(.5f, .78f), Vector2.zero, new Vector2(320, 164), new Color(Cream.r, Cream.g, Cream.b, .94f), roundSprite);
            Img(sigil.rectTransform, "SignStripe", new Vector2(.5f, .88f), Vector2.zero, new Vector2(286, 18), Coral, roundSprite);
            titleLogo = Txt(sigil.rectTransform, "곱빼기 빵집", new Vector2(.5f, .61f), Vector2.zero, 42, Ink, 300);
            titleLogo.fontStyle = FontStyles.Bold; titleLogo.outlineWidth = .08f; titleLogo.outlineColor = new Color32(255, 244, 220, 255);
            titleTag = Txt(sigil.rectTransform, "세제곱만큼 정확히 부어라", new Vector2(.5f, .30f), Vector2.zero, 17, Coral, 290);
            titleMeta = Txt(titleRoot, "중2 · 도형의 닮음  |  움직이는 반죽 블록 공방", new Vector2(.5f, .645f), Vector2.zero, 13, Ink, 350);

            hudG = new GameObject("Hud", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
            hudG.transform.SetParent(uiRoot, false); var hudRoot = (RectTransform)hudG.transform; hudRoot.anchorMin = Vector2.zero; hudRoot.anchorMax = Vector2.one; hudRoot.offsetMin = hudRoot.offsetMax = Vector2.zero;
            goalText = Txt(hudRoot, "닮음비를 세제곱해 정확한 큰 틀의 부피만큼 부으시오", new Vector2(.5f, .965f), Vector2.zero, 15, Ink, 370);
            // 모바일의 우상단 음소거 버튼은 공용 WebGL 템플릿이 덮어쓴다. 목표 문장을
            // 그 안전 영역 밖으로 밀어내지 말고, 세로에서는 두 줄로 감싸 전체 발문을 보존한다.
            goalText.overflowMode = TextOverflowModes.Overflow;
            scoreText = Txt(hudRoot, "0", new Vector2(.11f, .905f), Vector2.zero, 18, Violet, 80);
            livesText = Txt(hudRoot, "봉인 ◆◆◆", new Vector2(.50f, .905f), Vector2.zero, 15, Ink, 150);
            stockText = Txt(hudRoot, "재고 3650 cm³", new Vector2(.84f, .905f), Vector2.zero, 15, Coral, 145);
            timerText = Txt(hudRoot, "120초", new Vector2(.10f, .855f), Vector2.zero, 15, Ink, 70);
            progressText = Txt(hudRoot, "주문 1/9", new Vector2(.83f, .855f), Vector2.zero, 15, Violet, 105);

            promptCard = Img(hudRoot, "OrderDrum", new Vector2(.5f, .77f), Vector2.zero, new Vector2(362, 105), new Color(Cream.r, Cream.g, Cream.b, .94f), roundSprite);
            promptCardRt = promptCard.rectTransform;
            promptText = Txt(promptCardRt, "", new Vector2(.5f, .5f), Vector2.zero, 17, Ink, 338);
            altCard = Img(hudRoot, "QueuedDrum", new Vector2(.76f, .655f), Vector2.zero, new Vector2(172, 63), new Color(Violet.r, Violet.g, Violet.b, .94f), roundSprite);
            altCardRt = altCard.rectTransform;
            altText = Txt(altCardRt, "", new Vector2(.5f, .5f), Vector2.zero, 12, Cream, 160);
            amountText = Txt(hudRoot, "0 cm³", new Vector2(.5f, .455f), Vector2.zero, 38, Ink, 250);
            amountText.outlineWidth = .10f; amountText.outlineColor = new Color32(255, 244, 220, 255);

            leverBase = Img(uiRoot, "PressureLeverUi", new Vector2(.72f, .155f), Vector2.zero, new Vector2(126, 126), new Color(Violet.r, Violet.g, Violet.b, .97f), roundSprite);
            leverRt = leverBase.rectTransform;
            Img(leverRt, "MetalRing", new Vector2(.5f, .5f), Vector2.zero, new Vector2(104, 104), Nickel, ringSprite);
            leverKnob = Img(leverRt, "ButterKnob", new Vector2(.5f, .5f), Vector2.zero, new Vector2(75, 75), Mint, diamondSprite);
            leverKnobRt = leverKnob.rectTransform;
            // 부모 바깥 음수 앵커는 CanvasScaler의 가로 배율에서 화면 밖으로 밀렸다.
            // 중앙 앵커의 명시적 오프셋으로 두어 1280/2000px에서도 안전 여백을 보존한다.
            titleLeverText = Txt(leverRt, "누른 채\n거칠게", new Vector2(.5f, .5f), new Vector2(0, -86), 13, Ink, 145);
            guideRing = Img(uiRoot, "GuideRing", new Vector2(.72f, .155f), Vector2.zero, new Vector2(154, 154), Mint, ringSprite);
            guideRingRt = guideRing.rectTransform;

            var coach = Img(uiRoot, "HoldCoach", new Vector2(.31f, .155f), Vector2.zero, new Vector2(208, 126), new Color(Cream.r, Cream.g, Cream.b, .96f), roundSprite);
            holdCoachRt = coach.rectTransform;
            holdCoachText = Txt(holdCoachRt, "① 레버를 누른 채 거칠게 계량\n② 놓고 ±1 cm³로 정밀 조정\n③ 정확한 값에서 봉인 제출 · 0 cm³", new Vector2(.5f, .65f), Vector2.zero, 11, Ink, 194);
            holdCoachText.rectTransform.sizeDelta = new Vector2(194, 68);
            Img(holdCoachRt, "CoachTrack", new Vector2(.5f, .17f), Vector2.zero, new Vector2(174, 13), new Color(.74f, .66f, .55f, 1f), roundSprite);
            holdCoachFill = Img(holdCoachRt, "CoachFlow", new Vector2(.5f, .17f), Vector2.zero, new Vector2(8, 13), Coral, roundSprite);
            holdCoachFillRt = holdCoachFill.rectTransform;
            holdCoachFinger = Img(uiRoot, "CoachFinger", new Vector2(.72f, .155f), new Vector2(0, 54), new Vector2(46, 58), new Color(Cream.r, Cream.g, Cream.b, .90f), roundSprite);
            holdCoachFingerRt = holdCoachFinger.rectTransform;

            var fineMinus = Img(uiRoot, "FineMinus", new Vector2(.17f, .155f), Vector2.zero, new Vector2(76, 58), new Color(Cream.r, Cream.g, Cream.b, .97f), roundSprite);
            fineMinusRt = fineMinus.rectTransform;
            Txt(fineMinusRt, "−1 cm³", new Vector2(.5f, .5f), Vector2.zero, 14, Ink, 70);
            var fineSeal = Img(uiRoot, "FineSeal", new Vector2(.39f, .155f), Vector2.zero, new Vector2(92, 58), Coral, roundSprite);
            fineSealRt = fineSeal.rectTransform;
            Txt(fineSealRt, "봉인 제출", new Vector2(.5f, .5f), Vector2.zero, 14, Cream, 88);
            var finePlus = Img(uiRoot, "FinePlus", new Vector2(.61f, .155f), Vector2.zero, new Vector2(76, 58), new Color(Cream.r, Cream.g, Cream.b, .97f), roundSprite);
            finePlusRt = finePlus.rectTransform;
            Txt(finePlusRt, "+1 cm³", new Vector2(.5f, .5f), Vector2.zero, 14, Ink, 70);

            var toastBg = Img(uiRoot, "Toast", new Vector2(.5f, .285f), Vector2.zero, new Vector2(350, 56), new Color(Ink.r, Ink.g, Ink.b, .90f), roundSprite);
            toastRt = toastBg.rectTransform;
            toastText = Txt(toastBg.rectTransform, "", new Vector2(.5f, .5f), Vector2.zero, 14, Cream, 330);
            toastBg.gameObject.SetActive(false);

            revealG = new GameObject("MathReveal", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
            revealG.transform.SetParent(uiRoot, false); var revealRoot = (RectTransform)revealG.transform; revealRoot.anchorMin = Vector2.zero; revealRoot.anchorMax = Vector2.one; revealRoot.offsetMin = revealRoot.offsetMax = Vector2.zero;
            // 입체 셀 증명을 가리지 않도록 결과 판은 계량조 아래쪽으로 내려 둔다.
            revealPanel = Img(revealRoot, "RevealPlate", new Vector2(.5f, .27f), Vector2.zero, new Vector2(360, 160), new Color(Cream.r, Cream.g, Cream.b, .96f), roundSprite);
            revealRt = revealPanel.rectTransform;
            revealText = Txt(revealRt, "", new Vector2(.5f, .66f), Vector2.zero, 24, Coral, 336);
            revealDetail = Txt(revealRt, "", new Vector2(.5f, .28f), Vector2.zero, 14, Ink, 330);

            endG = new GameObject("EndScreen", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
            endG.transform.SetParent(uiRoot, false); var endRoot = (RectTransform)endG.transform; endRoot.anchorMin = Vector2.zero; endRoot.anchorMax = Vector2.one; endRoot.offsetMin = endRoot.offsetMax = Vector2.zero;
            endPanel = Img(endRoot, "EndBoard", new Vector2(.5f, .50f), Vector2.zero, new Vector2(355, 390), new Color(Cream.r, Cream.g, Cream.b, .97f), roundSprite);
            endRt = endPanel.rectTransform;
            endTitle = Txt(endRt, "", new Vector2(.5f, .75f), Vector2.zero, 38, Coral, 330);
            endStats = Txt(endRt, "", new Vector2(.5f, .49f), Vector2.zero, 19, Ink, 325);
            endCta = Txt(endRt, "탑을 눌러 다시 영업", new Vector2(.5f, .18f), Vector2.zero, 18, Coral, 310);

            ripple = Img(uiRoot, "InputRipple", new Vector2(0, 0), Vector2.zero, new Vector2(64, 64), Coral, ringSprite);
            rippleRt = ripple.rectTransform; ripple.gameObject.SetActive(false);
        }

        void SetScreen()
        {
            bool title = mode == Mode.Title, end = mode == Mode.End, reveal = mode == Mode.Reveal;
            titleG.alpha = title ? 1 : 0; titleG.gameObject.SetActive(title);
            hudG.alpha = (!title && !end) ? 1 : 0; hudG.gameObject.SetActive(!title && !end);
            revealG.alpha = reveal ? 1 : 0; revealG.gameObject.SetActive(reveal);
            endG.alpha = end ? 1 : 0; endG.gameObject.SetActive(end);
            leverRt.gameObject.SetActive(!end && !reveal);
            bool fine = st.adjusting && !title && !end && !reveal;
            fineMinusRt.gameObject.SetActive(fine); fineSealRt.gameObject.SetActive(fine); finePlusRt.gameObject.SetActive(fine);
            guideRingRt.gameObject.SetActive((title || mode == Mode.Practice) && !reveal && !fine);
            bool coach = mode == Mode.Practice && !practiceHoldLearned && !st.holding && !fine;
            holdCoachRt.gameObject.SetActive(coach); holdCoachFingerRt.gameObject.SetActive(coach);
            if (title) titleLeverText.text = "눌러서\n연습 시작";
            else titleLeverText.text = st.holding ? "흐르는 중" : fine ? "더 부으려면\n다시 누르기" : "누른 채\n거칠게";
        }

        void ResetWorldForRun()
        {
            towerTargetY = towerDisplayY = 0; towerRoot.localPosition = Vector3.zero; wasteVisual = 0; wasteCoil.localScale = new Vector3(.35f, .25f, .55f);
            for (int i = 0; i < stages.Length; i++) stages[i].sharedMaterial = emptyMat;
            for (int i = 0; i < cells.Length; i++) cells[i].gameObject.SetActive(false);
            UpdateFillVisual(0, 300, false); wrongShake = 0; displayedScore = 0; endDisplayedScore = 0;
        }

        void ResetWorldForOrder()
        {
            for (int i = 0; i < cells.Length; i++) cells[i].gameObject.SetActive(false);
            revealCellCount = revealFilledCount = 0; stream.gameObject.SetActive(false);
            UpdateFillVisual(st.poured, current.capacity, false); drumPulse = .001f;
        }

        void RefreshOrderUi()
        {
            if (current == null) return;
            promptText.text = "작은 틀 " + current.v0 + " cm³   ·   닮음비 " + current.m + ":" + current.n +
                "\n큰 틀의 부피만큼 부으시오";
            bool pair = PairAvailable(); altCardRt.gameObject.SetActive(pair);
            if (pair)
            {
                DoughOrder alt = deck[orderIndex + 1];
                altText.text = "대기 주문 · 눌러 교대\n" + alt.v0 + " cm³   " + alt.m + ":" + alt.n;
            }
            shownAmount = -1; shownStock = -1; shownLives = -1; shownSolved = -1;
        }

        void UpdateFillVisual(int poured, int capacity, bool trail)
        {
            float f = Mathf.Clamp01(poured / (float)Mathf.Max(1, capacity));
            float h = Mathf.Max(.035f, 2.42f * f);
            dough.localScale = new Vector3(4.70f, h, 3.55f);
            dough.localPosition = new Vector3(0, .12f + h, 0);
            if (trail) stream.localScale = new Vector3(.52f + .05f * Mathf.Sin(worldTime * 18f), 1.25f + f * .24f, .52f);
        }

        void BeginLeverVisual()
        {
            stream.gameObject.SetActive(true); titleLeverText.text = "흐르는 중"; guideBoost = 0;
            knobWorld.localScale = new Vector3(.72f, .48f, .72f);
        }

        void EndLeverVisual()
        {
            stream.gameObject.SetActive(false); titleLeverText.text = "누른 채\n거칠게";
            knobWorld.localScale = Vector3.one * .62f;
        }

        void ShowDrip()
        {
            Toast("짧은 탭은 한 방울만 튑니다 · 누른 채 유지하세요"); BoostGuide();
        }

        void ShowFineTune()
        {
            Toast("거친 계량 완료 · ±1 cm³로 맞춘 뒤 봉인 제출하세요");
            guideBoost = .7f;
        }

        void FineTunePulse(int delta)
        {
            amountText.transform.localScale = Vector3.one * 1.12f;
            titleLeverText.text = delta < 0 ? "1 cm³ 되돌림" : "1 cm³ 더함";
        }

        void BoostGuide() { guideBoost = 1.35f; Toast("레버를 누른 채 유지하고, 계산한 정확한 눈금에서 놓으시오"); }
        void SetGoalAfterPractice() { goalText.text = "9개 주문 중 첫 시도 7개 이상을 정확히 채우시오"; }
        void PulseDrum() { drumPulse = .55f; Toast("주문 순서를 바꿨습니다 · 재고를 계획하세요"); }
        void Refuse(string msg) { drumPulse = .42f; Toast(msg); MgfSfx.Play("wrong", .10f); }

        void InvalidTap(Vector2 screen, string msg)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, screen, null, out local);
            rippleRt.anchoredPosition = local; rippleRt.localScale = Vector3.one * .4f; rippleClock = .42f; ripple.gameObject.SetActive(true);
            guideBoost = 1.1f; Toast(msg);
        }

        void Toast(string msg)
        {
            toastText.text = msg; toastText.transform.parent.gameObject.SetActive(true); toastClock = 2.1f;
        }

        Vector2 LeverScreenPoint()
        {
            return RectTransformUtility.WorldToScreenPoint(null, leverRt.position);
        }

        void BeginRevealVisual(bool correct, int poured, DoughOrder o, string misconception)
        {
            int total = o.n * o.n * o.n;
            revealCellCount = total;
            revealFilledCount = correct ? total : Mathf.Clamp(Mathf.RoundToInt(total * poured / (float)o.target), 0, total);
            LayoutCells(o.n);
            if (correct)
            {
                for (int i = 0; i < total; i++) cells[i].gameObject.SetActive(false);
                int m3 = o.m * o.m * o.m, n3 = o.n * o.n * o.n;
                revealText.text = o.m + ":" + o.n + " → " + o.m + "³:" + o.n + "³ = " + m3 + ":" + n3;
                revealDetail.text = o.v0 + " × " + n3 + " ÷ " + m3 + " = " + o.target + " cm³";
                int stage = Mathf.Clamp(st.solved - 1, 0, stages.Length - 1); stages[stage].sharedMaterial = mintMat;
                towerTargetY = st.solved * .18f;
            }
            else
            {
                for (int i = 0; i < total; i++)
                {
                    cells[i].gameObject.SetActive(true); cellRender[i].sharedMaterial = i < revealFilledCount ? coralMat : emptyMat;
                }
                revealText.text = poured + " cm³   →   " + o.target + " cm³";
                revealDetail.text = MisconceptionText(misconception, o);
                wrongShake = .46f;
                if (poured > o.target) { wasteVisual += poured; wasteCoil.localScale = new Vector3(.35f + Mathf.Min(3.2f, wasteVisual / 230f), .25f, .55f); }
            }
        }

        string MisconceptionText(string id, DoughOrder o)
        {
            if (id == "linear_ratio") return "길이의 비를 그대로 썼습니다 · 부피는 " + o.m + "³:" + o.n + "³";
            if (id == "square_ratio") return "제곱까지만 했습니다 · 입체의 부피는 세제곱입니다";
            if (id == "reverse_ratio") return "작은 틀과 큰 틀의 비의 순서를 확인하세요";
            if (id == "near_miss") return "거의 맞았지만 부피는 정확히 " + o.target + " cm³여야 합니다";
            if (id == "overflow") return "넘친 반죽은 폐기 코일로 이동했습니다 · 새 컵에서 다시 계량하세요";
            return "빈 셀을 확인하세요 · 같은 컵에 한 번 더 부을 수 있습니다";
        }

        void LayoutCells(int n)
        {
            int idx = 0; float gap = n == 4 ? .48f : .62f; float center = (n - 1) * gap * .5f;
            for (int z = 0; z < n; z++) for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                var t = cells[idx]; t.localPosition = new Vector3(x * gap - center, y * gap - center, z * gap - center);
                t.localScale = Vector3.one * (n == 4 ? .40f : .52f); idx++;
            }
            for (; idx < cells.Length; idx++) cells[idx].gameObject.SetActive(false);
        }

        void CompleteRevealVisual()
        {
            for (int i = 0; i < cells.Length; i++) cells[i].gameObject.SetActive(false);
        }

        void ShowEndVisual(string reason)
        {
            bool clear = reason == "clear";
            endTitle.text = clear ? "빵 블록 완성" : "계량 카트 잠금";
            string why = clear ? "세제곱 계량 숙련 완료" : reason == "mastery" ? "완주했지만 첫 시도 숙련이 부족합니다" :
                reason == "timer" ? "120초가 지났습니다" : reason == "stock" ? "반죽 재고가 비었습니다" : "허가 봉인 3개를 모두 썼습니다";
            endStats.text = why + "\n\n완성 주문  " + st.solved + "/9\n첫 시도  " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal +
                "\n마지막 구간  " + st.finalBandFirstCorrect + "/3\n점수  0";
            endDisplayedScore = 0;
            endPanel.color = clear ? new Color(.96f, .88f, .62f, .97f) : new Color(.93f, .76f, .65f, .97f);
        }

        void UpdateWorld(float dt)
        {
            worldTime += dt; MgfLook.FitWidth(cam, 34f, .72f);
            float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            Vector3 wantCam = aspect < .72f ? new Vector3(10.5f, 7.4f, -15.8f) : new Vector3(11.8f, 6.8f, -17.2f);
            cameraHome = Vector3.Lerp(cameraHome, wantCam, dt * 3f);
            if (wrongShake > 0f)
            {
                wrongShake -= dt;
                float a = wrongShake * .22f; cam.transform.position = cameraHome + new Vector3(Mathf.Sin(worldTime * 77f) * a, Mathf.Cos(worldTime * 61f) * a * .45f, 0);
            }
            else cam.transform.position = Vector3.Lerp(cam.transform.position, cameraHome, dt * 8f);
            cam.transform.LookAt(new Vector3(0, 3.5f, .8f));

            towerDisplayY = Mathf.Lerp(towerDisplayY, towerTargetY + (towerDisplayY < towerTargetY ? .16f : 0f), dt * 5f);
            towerRoot.localPosition = new Vector3(0, towerDisplayY, 0);
            for (int i = 0; i < yeast.Length; i++)
            {
                float a = worldTime * (.42f + i * .08f) + i * 2.1f;
                float r = 4.1f + i * .45f;
                float reaction = mode == Mode.Reveal ? (revealCorrect ? Mathf.Abs(Mathf.Sin(revealClock * 11f + i)) * .65f : -.35f) : 0f;
                yeast[i].localPosition = new Vector3(Mathf.Cos(a) * r, 2.4f + i * 1.45f + Mathf.Sin(a * 1.7f) * .35f + reaction, .2f + Mathf.Sin(a) * 1.4f);
                yeast[i].localRotation = Quaternion.Euler(8f * Mathf.Sin(a), -a * Mathf.Rad2Deg + 90f, 9f * Mathf.Cos(a * 1.4f));
                yeast[i].localScale = Vector3.one * (.74f + .06f * Mathf.Sin(worldTime * 3f + i) + (mode == Mode.Reveal && revealCorrect ? .10f : 0f));
            }
            for (int i = 0; i < bubbles.Length; i++)
            {
                Vector3 p = bubbles[i].localPosition; p.y += dt * (.18f + (i % 4) * .035f);
                p.x += Mathf.Sin(worldTime * .7f + i) * dt * .025f; if (p.y > 9f) p.y = .3f; bubbles[i].localPosition = p;
            }
            leverWorld.localRotation = Quaternion.Euler(0, 0, st.holding ? -9f : Mathf.Sin(worldTime * 1.6f) * 1.5f);
            if (mode == Mode.Reveal && revealCorrect && revealCellCount > 0)
            {
                int show = Mathf.Clamp(Mathf.FloorToInt(revealClock * revealCellCount / 1.05f), 0, revealCellCount);
                for (int i = 0; i < show; i++) { cells[i].gameObject.SetActive(true); cellRender[i].sharedMaterial = mintMat; }
            }
        }

        void UpdateUi(float dt)
        {
            ApplyResponsiveUi();
            titleClock += dt;
            if (holdCoachRt.gameObject.activeSelf)
            {
                float cycle = worldTime % 3.2f;
                float press = Mathf.Clamp01((cycle - .35f) / .35f);
                float hold = Mathf.Clamp01((cycle - .70f) / 1.75f);
                float release = Mathf.Clamp01((cycle - 2.45f) / .35f);
                holdCoachFingerRt.anchoredPosition = new Vector2(0, Mathf.Lerp(58f, 8f, press) + release * 50f);
                holdCoachFingerRt.localScale = new Vector3(1f + guideBoost * .10f, Mathf.Lerp(1f, .72f, press * (1f - release)), 1f);
                holdCoachFillRt.sizeDelta = new Vector2(Mathf.Lerp(8f, 174f, hold), 13f);
                int nextCoachStep = hold < .08f ? 0 : hold < .44f ? 1 : hold < .78f ? 2 : 3;
                if (coachStep != nextCoachStep)
                {
                    coachStep = nextCoachStep;
                    string sample = coachStep == 0 ? "0" : coachStep == 1 ? "10" : coachStep == 2 ? "25" : "50";
                    holdCoachText.text = "① 레버를 누른 채 거칠게 계량\n② 놓고 ±1 cm³로 정밀 조정\n③ 정확한 값에서 봉인 제출 · " + sample + " cm³";
                }
            }
            if (guideRingRt.gameObject.activeSelf)
            {
                float pulse = .5f + .5f * Mathf.Sin(worldTime * (guideBoost > 0 ? 7.4f : 3.3f));
                guideRingRt.localScale = Vector3.one * (1f + .11f * pulse + guideBoost * .06f);
                guideRing.color = new Color(Mint.r, Mint.g, Mint.b, .30f + .55f * pulse);
                guideBoost = Mathf.Max(0, guideBoost - dt);
            }
            leverKnobRt.localScale = Vector3.Lerp(leverKnobRt.localScale, st.holding ? new Vector3(1.18f, .72f, 1f) : Vector3.one, dt * 15f);
            if (drumPulse > 0)
            {
                drumPulse -= dt; float q = 1f + Mathf.Sin((.55f - drumPulse) * 22f) * drumPulse * .12f;
                promptCardRt.localScale = new Vector3(q, 1f / Mathf.Max(.85f, q), 1);
            }
            else promptCardRt.localScale = Vector3.Lerp(promptCardRt.localScale, Vector3.one, dt * 10f);

            if (shownAmount != st.poured) { shownAmount = st.poured; amountText.text = st.poured + " cm³"; amountText.transform.localScale = Vector3.one * 1.08f; }
            amountText.transform.localScale = Vector3.Lerp(amountText.transform.localScale, Vector3.one, dt * 12f);
            displayedScore = Mathf.MoveTowards(displayedScore, st.score, dt * Mathf.Max(90f, Mathf.Abs(st.score - displayedScore) * 3.2f));
            scoreText.text = "점수 " + Mathf.RoundToInt(displayedScore);
            if (shownLives != st.lives) { shownLives = st.lives; livesText.text = "봉인 " + new string('◆', Mathf.Max(0, st.lives)) + new string('◇', Mathf.Max(0, 3 - st.lives)); livesText.transform.localScale = Vector3.one * 1.12f; }
            livesText.transform.localScale = Vector3.Lerp(livesText.transform.localScale, Vector3.one, dt * 10f);
            if (shownStock != st.stock) { shownStock = st.stock; stockText.text = "재고 " + st.stock + " cm³"; }
            if (shownSolved != st.solved)
            {
                shownSolved = st.solved;
                progressText.text = (mode == Mode.Practice || (mode == Mode.Reveal && revealPractice))
                    ? "연습 계량" : "주문 " + Mathf.Min(9, st.solved + 1) + "/9";
            }
            int sec = Mathf.Max(0, Mathf.CeilToInt(sessionLeft)); if (sec != lastSecond) { lastSecond = sec; timerText.text = sec + "초"; }

            if (toastClock > 0)
            {
                toastClock -= dt; toastText.transform.parent.gameObject.SetActive(true);
                toastText.color = new Color(Cream.r, Cream.g, Cream.b, Mathf.Clamp01(toastClock * 2f));
            }
            else if (toastText.transform.parent.gameObject.activeSelf) toastText.transform.parent.gameObject.SetActive(false);
            if (rippleClock > 0)
            {
                rippleClock -= dt; rippleRt.localScale = Vector3.one * Mathf.Lerp(.4f, 1.8f, 1f - rippleClock / .42f);
                ripple.color = new Color(Coral.r, Coral.g, Coral.b, rippleClock / .42f);
            }
            else if (ripple.gameObject.activeSelf) ripple.gameObject.SetActive(false);
            if (mode == Mode.End)
            {
                endDisplayedScore = Mathf.MoveTowards(endDisplayedScore, st.score, dt * Mathf.Max(120f, st.score * 1.8f));
                string why = endReason == "clear" ? "세제곱 계량 숙련 완료" : endReason == "mastery" ? "완주했지만 첫 시도 숙련이 부족합니다" :
                    endReason == "timer" ? "120초가 지났습니다" : endReason == "stock" ? "반죽 재고가 비었습니다" : "허가 봉인 3개를 모두 썼습니다";
                endStats.text = why + "\n\n완성 주문  " + st.solved + "/9\n첫 시도  " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal +
                    "\n마지막 구간  " + st.finalBandFirstCorrect + "/3\n점수  " + Mathf.RoundToInt(endDisplayedScore);
                endRt.localScale = Vector3.one * (1f + .012f * Mathf.Sin(worldTime * 2.2f));
            }
        }

        void ApplyResponsiveUi()
        {
            if (lastUiWidth == Screen.width && lastUiHeight == Screen.height) return;
            lastUiWidth = Screen.width; lastUiHeight = Screen.height;
            float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            bool land = aspect >= 1.15f;

            goalText.rectTransform.anchorMin = goalText.rectTransform.anchorMax = land
                ? new Vector2(.5f, .965f) : new Vector2(.43f, .952f);
            goalText.rectTransform.sizeDelta = land ? new Vector2(620f, 45f) : new Vector2(300f, 58f);
            goalText.fontSize = land ? 15f : 13.5f;

            Vector2 leverAnchor = land ? new Vector2(.84f, .22f) : new Vector2(.80f, .155f);
            leverRt.anchorMin = leverRt.anchorMax = leverAnchor;
            guideRingRt.anchorMin = guideRingRt.anchorMax = leverAnchor;
            holdCoachFingerRt.anchorMin = holdCoachFingerRt.anchorMax = leverAnchor;

            Vector2 coachAnchor = land ? new Vector2(.66f, .22f) : new Vector2(.30f, .155f);
            holdCoachRt.anchorMin = holdCoachRt.anchorMax = coachAnchor;

            Vector2 minus = land ? new Vector2(.43f, .22f) : new Vector2(.15f, .155f);
            Vector2 seal = land ? new Vector2(.55f, .22f) : new Vector2(.38f, .155f);
            Vector2 plus = land ? new Vector2(.67f, .22f) : new Vector2(.61f, .155f);
            fineMinusRt.anchorMin = fineMinusRt.anchorMax = minus;
            fineSealRt.anchorMin = fineSealRt.anchorMax = seal;
            finePlusRt.anchorMin = finePlusRt.anchorMax = plus;

            // 레버 아래는 초와이드 CanvasScaler에서 잘릴 수 있으므로 레이블을 버튼 위에 고정한다.
            titleLeverText.rectTransform.anchoredPosition = new Vector2(0, land ? 88f : 86f);
            Vector2 toastAnchor = land ? new Vector2(.5f, .36f) : new Vector2(.5f, .285f);
            toastRt.anchorMin = toastRt.anchorMax = toastAnchor;
        }
    }
}
