using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.MuneoSajinsa
{
    public partial class MuneoSajinsaGame
    {
        static readonly Color Water = MgfLook.Hex("#26384B");
        static readonly Color Copper = MgfLook.Hex("#B97732");
        static readonly Color Cream = MgfLook.Hex("#E9D7AD");
        static readonly Color Deep = MgfLook.Hex("#0C1622");
        static readonly Color Yellow = MgfLook.Hex("#F0A62B");
        static readonly Color SeaDark = MgfLook.Hex("#060A10");
        static readonly Color Coral = MgfLook.Hex("#8E2F2C");
        static readonly Color Ink = MgfLook.Hex("#111319");

        Camera cam;
        Transform worldRoot, octopus, octoHead, puffer, pufferBody, pufferTail, cameraBody, titleLensWorld;
        readonly Transform[] tentacles = new Transform[8];
        readonly Transform[] kelp = new Transform[18];
        readonly Transform[] bubbles = new Transform[16];
        readonly Transform[] photoPegs = new Transform[12];
        readonly Transform[] wallPhotos = new Transform[12];
        Transform wornHat, wornFlag;
        Vector3 cameraHome;
        Material waterMat, copperMat, creamMat, deepMat, yellowMat, coralMat, glassMat, inkMat;

        Canvas canvas;
        RectTransform rootRt, titleRt, hudRt, endRt, feedbackRt, dragGhostRt, rippleRt, guideRingRt, guideHandRt, flashRt, toastRt;
        CanvasGroup titleG, hudG, endG, feedbackG, toastG;
        Image titleLens, shutterImage, tearImage, trashImage, dragGhost, ripple, guideRing, guideHand, flashImage;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleStart;
        TextMeshProUGUI promptText, goalText, scoreText, livesText, timerText, roundText, instructionText;
        TextMeshProUGUI wornLeftText, wornRightText, shotsText, tearText, feedbackTitle, feedbackBody, endTitle, endStats, restartText, toastText;
        readonly RectTransform[,] propRt = new RectTransform[2, 4];
        readonly Image[,] propImage = new Image[2, 4];
        readonly TextMeshProUGUI[,] propText = new TextMeshProUGUI[2, 4];
        readonly RectTransform[] slotRt = new RectTransform[2];
        readonly Image[] slotImage = new Image[2];
        readonly RectTransform[] filmRt = new RectTransform[MuneoRules.ShotLimit];
        readonly Image[] filmImage = new Image[MuneoRules.ShotLimit];
        readonly TextMeshProUGUI[] filmText = new TextMeshProUGUI[MuneoRules.ShotLimit];
        RectTransform shutterRt, tearRt, trashRt, restartRt, titleLensRt, promptPlateRt;
        Sprite roundSprite, discSprite, ringSprite, dashedSprite;
        float worldClock, shootClock, flashClock, rippleClock, toastClock, guideBoost, refuseClock;
        float displayedScore, displayedEndScore;
        int lastW = -1, lastH = -1, lastScore = -1, shuffledOffset;
        Vector2 tearBase;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size); var im = rt.gameObject.AddComponent<Image>();
            im.color = color; im.sprite = sprite; im.raycastTarget = false; if (sprite && sprite.border != Vector4.zero) im.type = Image.Type.Sliced; return im;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color, float width = 360f, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 3.2f)); var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = color; t.alignment = align; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; t.text = text; return t;
        }

        static Sprite MakeSprite(int size, float radius, int mode)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "MuneoUi", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = (x + .5f) / size * 2 - 1, fy = (y + .5f) / size * 2 - 1; float a;
                if (mode == 1) { float d = Mathf.Sqrt(fx * fx + fy * fy); a = Mathf.Clamp01(1 - Mathf.Abs(d - .72f) * 13f); }
                else if (mode == 2) { float e = Mathf.Max(Mathf.Abs(fx), Mathf.Abs(fy)); float dash = Mathf.Sin((fx + fy) * 24f) > -.15f ? 1 : .18f; a = Mathf.Clamp01(1 - Mathf.Abs(e - .82f) * 22f) * dash; }
                else { float rr = Mathf.Clamp01(radius * 2 / size); float qx = Mathf.Max(Mathf.Abs(fx) - (1 - rr), 0); float qy = Mathf.Max(Mathf.Abs(fy) - (1 - rr), 0); a = Mathf.Clamp01((rr - Mathf.Sqrt(qx * qx + qy * qy)) * size * .5f + .5f); }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true); float b = mode == 0 ? radius + 2 : 0;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent, bool collider = false)
        {
            return MgfLook.Prim(type, name, pos, scale, mat, parent, collider);
        }

        GameObject Block(string name, Vector3 pos, Vector3 size, float radius, Material mat, Transform parent)
        {
            var o = MgfLook.Block(name, pos, size, radius, mat, parent); var c = o.GetComponent<Collider>(); if (c) Object.Destroy(c); return o;
        }

        void BuildWorld()
        {
            var types = new GameObject("PrimitiveColliderTypes"); types.AddComponent<SphereCollider>(); types.AddComponent<CapsuleCollider>(); Object.Destroy(types);
            MgfLook.Sky(SeaDark, Deep, MgfLook.Hex("#26303A"), .76f);
            MgfLook.Sun(new Vector3(42, -34, 16), MgfLook.Hex("#FFC76B"), 1.05f, .28f);
            cam = MgfLook.Camera(new Vector3(10.7f, 10.8f, -18.4f), new Vector3(0, 1.2f, 1.8f), 34f); cameraHome = cam.transform.position;
            waterMat = MgfLook.Lit(Water, .23f, .12f); copperMat = MgfLook.Lit(Copper, .22f, .62f, Copper * .025f);
            creamMat = MgfLook.Lit(Cream, .16f); deepMat = MgfLook.Lit(Deep, .14f, .38f); yellowMat = MgfLook.Lit(Yellow, .24f, .35f, Yellow * .18f);
            coralMat = MgfLook.Lit(Coral, .20f, .22f, Coral * .09f); glassMat = MgfLook.Lit(MgfLook.Hex("#45697A"), .56f, .38f, MgfLook.Hex("#142632") * .06f); inkMat = MgfLook.Lit(Ink, .12f, .18f);
            worldRoot = new GameObject("PressureHullDarkroom").transform;
            Block("RibbedSteelDeck", new Vector3(0, -1.15f, 2.2f), new Vector3(24, 1.4f, 17), .05f, inkMat, worldRoot);
            for (int i = 0; i < 13; i++) Block("DeckRib" + i, new Vector3(-9f + i * 1.5f, -.38f, .6f), new Vector3(.10f, .10f, 11f), .01f, copperMat, worldRoot);
            BuildCoralWall(); BuildKelp(); BuildOctopus(); BuildPuffer(); BuildCameraProp(); BuildPhotoWall(); BuildBubbles();
        }

        void BuildCoralWall()
        {
            Block("PressureBulkhead", new Vector3(0, 2.7f, 7.2f), new Vector3(18, 6.5f, .55f), .04f, deepMat, worldRoot);
            for (int i = 0; i < 24; i++)
            {
                int row = i / 8, col = i % 8; float x = -7.7f + col * 2.2f, y = .3f + row * 2.05f;
                Block("RivetedHullPanel" + i, new Vector3(x, y, 6.82f), new Vector3(2.02f, 1.86f, .12f), .025f, (i + row) % 3 == 0 ? waterMat : inkMat, worldRoot);
                for (int b = 0; b < 4; b++) Prim(PrimitiveType.Sphere, "PanelBolt", new Vector3(x + (b % 2 == 0 ? -.84f : .84f), y + (b < 2 ? -.72f : .72f), 6.63f), new Vector3(.10f, .10f, .06f), copperMat, worldRoot);
            }
            for (int i = 0; i < 3; i++)
            {
                float x = -6.2f + i * 6.2f;
                var rim = Prim(PrimitiveType.Cylinder, "PortholeRim", new Vector3(x, 4.45f, 6.45f), new Vector3(1.28f, .15f, 1.28f), copperMat, worldRoot).transform; rim.localRotation = Quaternion.Euler(90, 0, 0);
                var glass = Prim(PrimitiveType.Cylinder, "PortholeGlass", new Vector3(x, 4.45f, 6.27f), new Vector3(.91f, .08f, .91f), glassMat, worldRoot).transform; glass.localRotation = Quaternion.Euler(90, 0, 0);
            }
        }

        void BuildKelp()
        {
            for (int i = 0; i < kelp.Length; i++)
            {
                float side = i < kelp.Length / 2 ? -1 : 1; int k = i % (kelp.Length / 2);
                var root = new GameObject("GaugeNeedle" + i).transform; root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(side * (6.25f + (k % 3) * .68f), .28f + (k / 3) * 1.55f, 5.95f);
                var gauge = Prim(PrimitiveType.Cylinder, "GaugeFace", Vector3.zero, new Vector3(.42f, .08f, .42f), creamMat, root).transform; gauge.localRotation = Quaternion.Euler(90, 0, 0);
                Block("Needle", new Vector3(0, .12f, -.12f), new Vector3(.06f, .34f, .05f), .01f, coralMat, root);
                kelp[i] = root;
            }
        }

        void BuildOctopus()
        {
            octopus = new GameObject("BrassDarkroomOperator").transform; octopus.SetParent(worldRoot, false); octopus.localPosition = new Vector3(-4.15f, .42f, 1.9f);
            octoHead = Block("AngularHead", new Vector3(0, 1.35f, 0), new Vector3(1.55f, 1.32f, 1.18f), .08f, copperMat, octopus).transform;
            Block("ApronBody", new Vector3(0, .25f, .18f), new Vector3(1.65f, 1.45f, .9f), .05f, inkMat, octopus);
            for (int e = -1; e <= 1; e += 2)
            {
                Prim(PrimitiveType.Cylinder, "ApertureEye", new Vector3(e * .39f, 1.45f, -.65f), new Vector3(.25f, .09f, .25f), yellowMat, octopus).transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            for (int i = 0; i < tentacles.Length; i++)
            {
                float a = Mathf.Lerp(-112f, 112f, i / 7f) * Mathf.Deg2Rad;
                var t = new GameObject("ArticulatedArm" + i).transform; t.SetParent(octopus, false); t.localPosition = new Vector3(Mathf.Sin(a) * .68f, .40f, Mathf.Cos(a) * .32f);
                for (int s = 0; s < 3; s++)
                {
                    var seg = Block("JointedArmSegment", new Vector3(0, -.30f - s * .39f, s * .08f), new Vector3(.18f, .40f, .18f), .025f, s % 2 == 0 ? copperMat : inkMat, t).transform;
                    seg.localRotation = Quaternion.Euler(0, 0, (i - 3.5f) * 3f + s * (i % 2 == 0 ? 6 : -6));
                }
                tentacles[i] = t;
            }
        }

        void BuildPuffer()
        {
            puffer = new GameObject("PressureSuitSubject").transform; puffer.SetParent(worldRoot, false); puffer.localPosition = new Vector3(.45f, .72f, 1.5f);
            pufferBody = Block("RivetedSuitBody", new Vector3(0, .92f, 0), new Vector3(2.05f, 1.75f, 1.35f), .10f, waterMat, puffer).transform;
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2 / 10f; var rivet = Prim(PrimitiveType.Cube, "SuitRivet", new Vector3(Mathf.Cos(a) * 1.01f, .92f + Mathf.Sin(a) * .82f, -.57f), new Vector3(.13f, .13f, .10f), copperMat, puffer).transform;
                rivet.localRotation = Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg + 45f);
            }
            for (int e = -1; e <= 1; e += 2)
            {
                Prim(PrimitiveType.Cylinder, "SuitLens", new Vector3(e * .43f, 1.18f, -.77f), new Vector3(.25f, .08f, .25f), yellowMat, puffer).transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            pufferTail = Block("PressureLever", new Vector3(1.35f, .94f, .12f), new Vector3(.22f, 1.0f, .18f), .02f, coralMat, puffer).transform;
            pufferTail.localRotation = Quaternion.Euler(0, 0, -22f);
            wornHat = new GameObject("WornHat").transform; wornHat.SetParent(puffer, false); wornHat.localPosition = new Vector3(0, 2.02f, 0);
            Block("HatBrim", Vector3.zero, new Vector3(1.30f, .12f, .72f), .02f, yellowMat, wornHat);
            Block("HatCrown", new Vector3(0, .34f, 0), new Vector3(.72f, .62f, .62f), .04f, coralMat, wornHat);
            wornFlag = new GameObject("WornFlag").transform; wornFlag.SetParent(puffer, false); wornFlag.localPosition = new Vector3(0, 1.28f, .62f);
            Prim(PrimitiveType.Cylinder, "FlagPole", new Vector3(.68f, .25f, 0), new Vector3(.07f, .84f, .07f), copperMat, wornFlag);
            var flag = Block("SignalPlate", new Vector3(.18f, .68f, 0), new Vector3(1.0f, .52f, .08f), .02f, coralMat, wornFlag); flag.transform.localRotation = Quaternion.Euler(0, 0, 7f);
            wornHat.gameObject.SetActive(false); wornFlag.gameObject.SetActive(false);
        }

        void BuildCameraProp()
        {
            var root = new GameObject("BrassPlateCamera").transform; root.SetParent(worldRoot, false); root.localPosition = new Vector3(4.5f, .70f, .50f);
            cameraBody = Block("BellowsCameraBody", new Vector3(0, .82f, 0), new Vector3(2.25f, 1.60f, 1.08f), .05f, copperMat, root).transform;
            for (int i = 0; i < 5; i++) Block("BellowsRib", new Vector3(-.76f + i * .38f, .82f, -.58f), new Vector3(.09f, 1.18f, .32f), .01f, inkMat, root);
            titleLensWorld = Prim(PrimitiveType.Cylinder, "ApertureLens", new Vector3(0, .84f, -.76f), new Vector3(1.30f, .18f, 1.30f), deepMat, root).transform;
            titleLensWorld.localRotation = Quaternion.Euler(90, 0, 0);
            Prim(PrimitiveType.Cylinder, "SmokedLens", new Vector3(0, .84f, -.91f), new Vector3(.90f, .10f, .90f), glassMat, root).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Block("FilmMouth", new Vector3(0, .05f, -.20f), new Vector3(1.38f, .20f, .62f), .025f, inkMat, root);
        }

        void BuildPhotoWall()
        {
            for (int i = 0; i < photoPegs.Length; i++)
            {
                int row = i / 6, col = i % 6; float x = -2.2f + col * 1.23f, y = 2.0f + row * 1.28f;
                var peg = Prim(PrimitiveType.Cylinder, "DevelopmentLamp" + i, new Vector3(x, y + .52f, 6.28f), new Vector3(.11f, .05f, .11f), yellowMat, worldRoot).transform; peg.localRotation = Quaternion.Euler(90, 0, 0);
                var card = Block("ChemicalDevelopmentCell", new Vector3(x, y, 6.35f), new Vector3(.94f, .92f, .08f), .015f, creamMat, worldRoot).transform;
                card.gameObject.SetActive(false); peg.gameObject.SetActive(false); photoPegs[i] = peg; wallPhotos[i] = card;
            }
        }

        void BuildBubbles()
        {
            for (int i = 0; i < bubbles.Length; i++)
            {
                float side = i < 8 ? -1 : 1; int k = i % 8;
                Block("SafetyLampCable", new Vector3(side * (3.0f + k * .68f), 5.52f, 4.7f), new Vector3(.035f, 1.05f + (k % 3) * .22f, .035f), .005f, inkMat, worldRoot);
                var b = Prim(PrimitiveType.Sphere, "RedSafetyLamp", new Vector3(side * (3.0f + k * .68f), 4.95f - (k % 3) * .22f, 4.65f), new Vector3(.16f, .16f, .16f), i % 3 == 0 ? yellowMat : coralMat, worldRoot, false).transform;
                bubbles[i] = b;
            }
        }

        void BuildUi()
        {
            roundSprite = MakeSprite(96, 22, 0); discSprite = MakeSprite(96, 46, 0); ringSprite = MakeSprite(96, 0, 1); dashedSprite = MakeSprite(96, 0, 2);
            var go = new GameObject("MuneoUi", typeof(RectTransform)); canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
            var scaler = go.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(390, 844); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            rootRt = go.GetComponent<RectTransform>(); BuildTitleUi(); BuildHudUi(); BuildEndUi(); BuildOverlays();
        }

        void BuildTitleUi()
        {
            titleRt = R("Title", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero); titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one; titleRt.sizeDelta = Vector2.zero; titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            Img(titleRt, "HangingBrassSign", new Vector2(.5f, .79f), Vector2.zero, new Vector2(356, 82), new Color(Deep.r, Deep.g, Deep.b, .94f), roundSprite);
            titleLogo = Txt(titleRt, "문어 사진사", new Vector2(.50f, .80f), Vector2.zero, 43, Cream, 350); titleLogo.fontStyle = FontStyles.Bold; titleLogo.outlineWidth = .20f; titleLogo.outlineColor = Ink;
            titleTag = Txt(titleRt, "압력선체 암실 · 가능한 장면을 전부 현상하라", new Vector2(.5f, .715f), Vector2.zero, 14, Yellow, 360); titleTag.outlineWidth = .10f; titleTag.outlineColor = Ink;
            titleLensRt = R("StartLens", titleRt, new Vector2(.5f, .39f), Vector2.zero, new Vector2(156, 156));
            Img(titleLensRt, "OuterCopper", new Vector2(.5f, .5f), Vector2.zero, new Vector2(156, 156), Copper, discSprite);
            Img(titleLensRt, "DarkRing", new Vector2(.5f, .5f), Vector2.zero, new Vector2(128, 128), Deep, discSprite);
            titleLens = Img(titleLensRt, "SeaGlass", new Vector2(.5f, .5f), Vector2.zero, new Vector2(104, 104), new Color(Water.r, Water.g, Water.b, .98f), discSprite);
            titleStart = Txt(titleLensRt, "렌즈를 눌러\n사진관 열기", new Vector2(.5f, .5f), Vector2.zero, 17, Cream, 126); titleStart.fontStyle = FontStyles.Bold;
            titleMeta = Txt(titleRt, "중2 · 경우의 수    목표: 첫 제출 5/6", new Vector2(.5f, .16f), Vector2.zero, 12, Cream, 340);
        }

        void BuildHudUi()
        {
            hudRt = R("Hud", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero); hudRt.anchorMin = Vector2.zero; hudRt.anchorMax = Vector2.one; hudRt.sizeDelta = Vector2.zero; hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            promptPlateRt = Img(hudRt, "PromptPlate", new Vector2(.5f, .855f), Vector2.zero, new Vector2(360, 104), new Color(Cream.r, Cream.g, Cream.b, .96f), roundSprite).rectTransform;
            promptText = Txt(hudRt, "", new Vector2(.5f, .865f), Vector2.zero, 14, Ink, 340); promptText.fontStyle = FontStyles.Bold;
            goalText = Txt(hudRt, "첫 제출 0/6 · 목표 5 · 실수 여유 1", new Vector2(.5f, .775f), Vector2.zero, 12, Yellow, 350); goalText.fontStyle = FontStyles.Bold;
            scoreText = Txt(hudRt, "0점", new Vector2(.09f, .962f), Vector2.zero, 13, Cream, 78); livesText = Txt(hudRt, "고리 3", new Vector2(.28f, .962f), Vector2.zero, 13, Cream, 78);
            timerText = Txt(hudRt, "150초", new Vector2(.52f, .962f), Vector2.zero, 13, Cream, 78); roundText = Txt(hudRt, "1/6", new Vector2(.73f, .962f), Vector2.zero, 13, Cream, 70);
            instructionText = Txt(hudRt, "카드를 누르거나 착용 자리로 끌어 놓으시오", new Vector2(.5f, .715f), Vector2.zero, 13, Cream, 360); instructionText.outlineWidth = .18f; instructionText.outlineColor = Deep;
            for (int g = 0; g < 2; g++)
            {
                slotRt[g] = R("WearSlot" + g, hudRt, new Vector2(.38f + g * .24f, .58f), Vector2.zero, new Vector2(104, 66));
                slotImage[g] = Img(slotRt[g], "Slot", new Vector2(.5f, .5f), Vector2.zero, new Vector2(104, 66), new Color(Cream.r, Cream.g, Cream.b, .30f), dashedSprite);
            }
            wornLeftText = Txt(slotRt[0], "모자", new Vector2(.5f, .5f), Vector2.zero, 14, Cream, 98); wornRightText = Txt(slotRt[1], "깃발", new Vector2(.5f, .5f), Vector2.zero, 14, Cream, 98);
            shutterRt = R("CameraLens", hudRt, new Vector2(.84f, .54f), Vector2.zero, new Vector2(78, 78));
            Img(shutterRt, "Copper", new Vector2(.5f, .5f), Vector2.zero, new Vector2(78, 78), Copper, discSprite); shutterImage = Img(shutterRt, "Glass", new Vector2(.5f, .5f), Vector2.zero, new Vector2(58, 58), Deep, discSprite);
            Txt(shutterRt, "촬영", new Vector2(.5f, .5f), Vector2.zero, 14, Cream, 66);
            for (int g = 0; g < 2; g++) for (int i = 0; i < 4; i++)
            {
                propRt[g, i] = R("Prop" + g + "_" + i, hudRt, new Vector2(.14f + i * .24f, .36f - g * .09f), Vector2.zero, new Vector2(82, 58));
                propImage[g, i] = Img(propRt[g, i], "Card", new Vector2(.5f, .5f), Vector2.zero, new Vector2(82, 58), g == 0 ? new Color(Copper.r, Copper.g, Copper.b, .98f) : new Color(Water.r, Water.g, Water.b, .98f), roundSprite);
                propText[g, i] = Txt(propRt[g, i], "", new Vector2(.5f, .5f), Vector2.zero, 13, Cream, 76); propText[g, i].fontStyle = FontStyles.Bold;
            }
            trashRt = R("Trash", hudRt, new Vector2(.08f, .08f), Vector2.zero, new Vector2(58, 58)); trashImage = Img(trashRt, "TrashBody", new Vector2(.5f, .5f), Vector2.zero, new Vector2(58, 58), Deep, roundSprite); Txt(trashRt, "버리기", new Vector2(.5f, .5f), Vector2.zero, 11, Cream, 54);
            tearRt = R("TearHandle", hudRt, new Vector2(.92f, .09f), Vector2.zero, new Vector2(62, 72)); tearBase = tearRt.anchoredPosition;
            tearImage = Img(tearRt, "Handle", new Vector2(.5f, .5f), Vector2.zero, new Vector2(62, 72), Copper, roundSprite); tearText = Txt(tearRt, "필름\n당기기", new Vector2(.5f, .5f), Vector2.zero, 11, Cream, 58);
            shotsText = Txt(hudRt, "촬영 0/14", new Vector2(.5f, .205f), Vector2.zero, 12, Cream, 180);
            for (int i = 0; i < filmRt.Length; i++)
            {
                int row = i / 7, col = i % 7; filmRt[i] = R("Film" + i, hudRt, new Vector2(.18f + col * .105f, .13f - row * .07f), Vector2.zero, new Vector2(38, 46));
                filmImage[i] = Img(filmRt[i], "Photo", new Vector2(.5f, .5f), Vector2.zero, new Vector2(38, 46), new Color(Cream.r, Cream.g, Cream.b, .28f), roundSprite);
                filmText[i] = Txt(filmRt[i], "", new Vector2(.5f, .5f), Vector2.zero, 9, Ink, 36);
            }
        }

        void BuildEndUi()
        {
            endRt = R("End", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero); endRt.anchorMin = Vector2.zero; endRt.anchorMax = Vector2.one; endRt.sizeDelta = Vector2.zero; endG = endRt.gameObject.AddComponent<CanvasGroup>();
            Img(endRt, "DarkroomResult", new Vector2(.5f, .55f), Vector2.zero, new Vector2(350, 365), new Color(Deep.r, Deep.g, Deep.b, .97f), roundSprite);
            endTitle = Txt(endRt, "현상 격자 완성", new Vector2(.5f, .68f), Vector2.zero, 35, Yellow, 330); endTitle.fontStyle = FontStyles.Bold; endTitle.outlineWidth = .16f; endTitle.outlineColor = Ink;
            endStats = Txt(endRt, "", new Vector2(.5f, .52f), Vector2.zero, 19, Ink, 320);
            restartRt = R("RestartPeg", endRt, new Vector2(.5f, .30f), Vector2.zero, new Vector2(156, 72)); Img(restartRt, "Peg", new Vector2(.5f, .5f), Vector2.zero, new Vector2(156, 72), Yellow, roundSprite);
            restartText = Txt(restartRt, "새 필름 걸기", new Vector2(.5f, .5f), Vector2.zero, 18, Ink, 150); restartText.fontStyle = FontStyles.Bold;
        }

        void BuildOverlays()
        {
            feedbackRt = R("Feedback", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero); feedbackRt.anchorMin = Vector2.zero; feedbackRt.anchorMax = Vector2.one; feedbackRt.sizeDelta = Vector2.zero; feedbackG = feedbackRt.gameObject.AddComponent<CanvasGroup>();
            Img(feedbackRt, "ChemicalTray", new Vector2(.5f, .52f), Vector2.zero, new Vector2(350, 210), new Color(Deep.r, Deep.g, Deep.b, .98f), roundSprite);
            feedbackTitle = Txt(feedbackRt, "", new Vector2(.5f, .58f), Vector2.zero, 28, Yellow, 330); feedbackTitle.fontStyle = FontStyles.Bold;
            feedbackBody = Txt(feedbackRt, "", new Vector2(.5f, .48f), Vector2.zero, 17, Cream, 320);
            dragGhostRt = R("DragGhost", rootRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(88, 62)); dragGhost = dragGhostRt.gameObject.AddComponent<Image>(); dragGhost.sprite = roundSprite; dragGhost.color = Yellow; dragGhost.raycastTarget = false; dragGhostRt.gameObject.SetActive(false);
            rippleRt = R("Ripple", rootRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(28, 28)); ripple = rippleRt.gameObject.AddComponent<Image>(); ripple.sprite = ringSprite; ripple.color = Cream; ripple.raycastTarget = false; rippleRt.gameObject.SetActive(false);
            guideRingRt = R("GuideRing", rootRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(98, 74)); guideRing = guideRingRt.gameObject.AddComponent<Image>(); guideRing.sprite = ringSprite; guideRing.color = Yellow; guideRing.raycastTarget = false;
            guideHandRt = R("GuideHand", rootRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(34, 34)); guideHand = guideHandRt.gameObject.AddComponent<Image>(); guideHand.sprite = discSprite; guideHand.color = Cream; guideHand.raycastTarget = false;
            flashRt = R("Flash", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero); flashRt.anchorMin = Vector2.zero; flashRt.anchorMax = Vector2.one; flashRt.sizeDelta = Vector2.zero; flashImage = flashRt.gameObject.AddComponent<Image>(); flashImage.color = new Color(Cream.r, Cream.g, Cream.b, 0); flashImage.raycastTarget = false;
            toastRt = R("Toast", rootRt, new Vector2(.5f, .685f), Vector2.zero, new Vector2(354, 62)); toastG = toastRt.gameObject.AddComponent<CanvasGroup>(); Img(toastRt, "ToastPlate", new Vector2(.5f, .5f), Vector2.zero, new Vector2(354, 62), Ink, roundSprite); toastText = Txt(toastRt, "", new Vector2(.5f, .5f), Vector2.zero, 13, Cream, 338);
        }

        void ResetWorldForRun()
        {
            octopus.localPosition = new Vector3(-4.15f, .42f, 1.9f); puffer.localPosition = new Vector3(.45f, .72f, 1.5f); cameraHome = cam.transform.position;
            for (int i = 0; i < photoPegs.Length; i++) { photoPegs[i].gameObject.SetActive(false); wallPhotos[i].gameObject.SetActive(false); } ApplyWornVisual();
        }

        void ResetWorldForProblem()
        {
            shootClock = 0; flashClock = 0; shuffledOffset = (shuffledOffset + 1) % 4; ApplyWornVisual();
        }

        void ApplyWornVisual()
        {
            if (!wornHat || !wornFlag) return; wornHat.gameObject.SetActive(wornLeft.Length > 0); wornFlag.gameObject.SetActive(wornRight.Length > 0);
            if (wornHat.gameObject.activeSelf) wornHat.localRotation = Quaternion.Euler(0, 0, (wornLeft.GetHashCode() % 11) - 5);
            if (wornFlag.gameObject.activeSelf) wornFlag.localRotation = Quaternion.Euler(0, 0, (wornRight.GetHashCode() % 9) - 4);
        }

        void SetScreen()
        {
            titleG.alpha = phase == GamePhase.Title ? 1 : 0; titleG.blocksRaycasts = false; titleRt.gameObject.SetActive(phase == GamePhase.Title);
            bool play = phase == GamePhase.Practice || phase == GamePhase.Playing || phase == GamePhase.Feedback;
            hudG.alpha = play ? 1 : 0; hudRt.gameObject.SetActive(play); endG.alpha = phase == GamePhase.End ? 1 : 0; endRt.gameObject.SetActive(phase == GamePhase.End);
            feedbackG.alpha = phase == GamePhase.Feedback ? 1 : 0; feedbackRt.gameObject.SetActive(phase == GamePhase.Feedback);
            guideRingRt.gameObject.SetActive(phase == GamePhase.Practice); guideHandRt.gameObject.SetActive(phase == GamePhase.Practice);
        }

        void RebuildPropCards()
        {
            for (int g = 0; g < 2; g++) for (int i = 0; i < 4; i++)
            {
                string[] arr = g == 0 ? current.leftLabels : current.rightLabels; bool on = i < arr.Length;
                propRt[g, i].gameObject.SetActive(on); if (on) propText[g, i].text = arr[i];
            }
            wornLeftText.text = current.kind == TaskKind.NumberCards ? "십의 자리" : "모자";
            wornRightText.text = current.kind == TaskKind.NumberCards ? "일의 자리" : "깃발";
            if (current.kind == TaskKind.Sum) instructionText.text = "카드를 누르거나 끌어 모자 또는 깃발 하나만 착용하시오";
            else if (current.kind == TaskKind.Product) instructionText.text = "카드를 누르거나 끌어 모자와 깃발을 하나씩 착용하시오";
            else instructionText.text = "카드를 누르거나 끌어 십의 자리와 일의 자리를 채우시오";
        }

        void ShufflePropCards()
        {
            shuffledOffset = (shuffledOffset + 1 + st.shotsUsed) % 4;
            LayoutUi(true); ShowToast("소품 상자 위치가 바뀌었습니다. 사진을 비교해 다음 모습을 정하시오.");
        }

        void RefreshProblemUi()
        {
            promptText.text = current.prompt; RebuildPropCards(); RefreshHud();
        }

        void RefreshFilm()
        {
            for (int i = 0; i < filmRt.Length; i++)
            {
                bool has = i < photos.Count; filmImage[i].color = has ? Cream : new Color(Cream.r, Cream.g, Cream.b, .20f); filmText[i].text = has ? PhotoLabel(photos[i]) : "";
                filmRt[i].localRotation = Quaternion.Euler(0, 0, has ? (i % 3 - 1) * 2f : 0);
            }
        }

        string PhotoLabel(string id)
        {
            if (current.kind == TaskKind.NumberCards) return id;
            if (id.Contains("|"))
            {
                string[] p = id.Split('|'); int h = int.Parse(p[0].Substring(1)), f = int.Parse(p[1].Substring(1)); return Short(current.leftLabels[h]) + "\n" + Short(current.rightLabels[f]);
            }
            if (id.StartsWith("H")) return Short(current.leftLabels[int.Parse(id.Substring(1))]);
            return Short(current.rightLabels[int.Parse(id.Substring(1))]);
        }

        static string Short(string s) { return s.Replace(" 모자", "").Replace(" 깃발", ""); }

        void RefreshHud()
        {
            shotsText.text = "촬영 " + st.shotsUsed + "/" + MuneoRules.ShotLimit + " · 필름 " + photos.Count + "장";
            livesText.text = "고리 " + st.lives; timerText.text = st.remainingTime + "초"; roundText.text = (phase == GamePhase.Practice ? "연습" : Mathf.Max(1, st.round) + "/6");
            int mistakes = Mathf.Max(0, st.firstAttemptTotal - st.firstAttemptCorrect);
            goalText.text = phase == GamePhase.Practice ? "연습 · 카드 탭도 착용으로 인정" : "첫 제출 " + st.firstAttemptCorrect + "/6 · 목표 5 · 실수 여유 " + Mathf.Max(0, 1 - mistakes);
            if (lastScore < 0) displayedScore = st.score; lastScore = st.score;
        }

        void UpdateWorld(float dt)
        {
            worldClock += dt; float s = Mathf.Sin(worldClock * 1.4f);
            for (int i = 0; i < kelp.Length; i++) kelp[i].localRotation = Quaternion.Euler(0, 0, -38f + (i % 7) * 12f + Mathf.Sin(worldClock * .45f + i * .61f) * 2.5f);
            for (int i = 0; i < tentacles.Length; i++) tentacles[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(worldClock * .75f + i * .72f) * 3.5f);
            octoHead.localScale = new Vector3(1.55f, 1.32f + s * .012f, 1.18f); puffer.localPosition = new Vector3(.45f, .72f + Mathf.Sin(worldClock * .8f) * .018f, 1.5f);
            pufferTail.localRotation = Quaternion.Euler(0, 0, -22f + Mathf.Sin(worldClock * 2.2f) * 4f);
            for (int i = 0; i < bubbles.Length; i++)
            {
                float glow = .88f + Mathf.Max(0, Mathf.Sin(worldClock * 1.7f + i * .73f)) * .22f; bubbles[i].localScale = Vector3.one * glow;
            }
            if (shootClock > 0)
            {
                shootClock -= dt; float k = 1f - shootClock / .55f; float pulse = 1f + Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * .08f; pufferBody.localScale = new Vector3(2.05f, 1.75f, 1.35f) * pulse;
            }
            else pufferBody.localScale = new Vector3(2.05f, 1.75f, 1.35f);
            if (phase == GamePhase.Title) titleLensWorld.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 2.1f) * .035f);
            MgfLook.FitWidth(cam, 34f, .72f);
        }

        void UpdateUi(float dt)
        {
            if (Screen.width != lastW || Screen.height != lastH) { lastW = Screen.width; lastH = Screen.height; LayoutUi(false); }
            displayedScore = Mathf.MoveTowards(displayedScore, st.score, Mathf.Max(80, Mathf.Abs(st.score - displayedScore) * 4) * dt); scoreText.text = Mathf.RoundToInt(displayedScore) + "점";
            if (phase == GamePhase.End) { displayedEndScore = Mathf.MoveTowards(displayedEndScore, st.score, Mathf.Max(120, st.score * 2) * dt); endStats.text = "점수 " + Mathf.RoundToInt(displayedEndScore) + "\n첫 제출 " + st.firstAttemptCorrect + "/6 · 완성 " + st.solved + "/6"; }
            if (phase == GamePhase.Title)
            {
                float pulse = 1f + Mathf.Max(0, Mathf.Sin(worldClock * 2.4f)) * .045f; titleLensRt.localScale = Vector3.one * pulse;
            }
            if (rippleClock > 0)
            {
                rippleClock -= dt; float k = 1 - rippleClock / .32f; rippleRt.localScale = Vector3.one * Mathf.Lerp(.35f, 2.4f, k); ripple.color = new Color(Cream.r, Cream.g, Cream.b, 1 - k); if (rippleClock <= 0) rippleRt.gameObject.SetActive(false);
            }
            if (flashClock > 0) { flashClock -= dt; flashImage.color = new Color(Cream.r, Cream.g, Cream.b, Mathf.Clamp01(flashClock / .20f) * .78f); } else flashImage.color = new Color(1, 1, 1, 0);
            if (toastClock > 0) { toastClock -= dt; toastG.alpha = Mathf.Clamp01(toastClock * 5f) * Mathf.Clamp01((2.4f - toastClock) * 5f); } else toastG.alpha = 0;
            if (phase == GamePhase.Practice) UpdateGuide();
            if (refuseClock > 0)
            {
                refuseClock -= dt; shutterRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(refuseClock * 80f) * refuseClock * 10f);
            }
            else shutterRt.localRotation = Quaternion.identity;
        }

        void LayoutUi(bool shuffleOnly)
        {
            if (!hudRt || current == null) return; bool wide = Screen.width >= 640 && Screen.width >= Screen.height;
            if (wide)
            {
                promptPlateRt.anchorMin = promptPlateRt.anchorMax = new Vector2(.5f, .865f); promptPlateRt.sizeDelta = new Vector2(620, 78);
                promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = new Vector2(.5f, .875f); promptText.rectTransform.sizeDelta = new Vector2(590, 58); promptText.fontSize = 14;
                goalText.rectTransform.anchorMin = goalText.rectTransform.anchorMax = new Vector2(.5f, .805f);
                instructionText.rectTransform.anchorMin = instructionText.rectTransform.anchorMax = new Vector2(.5f, .735f);
                slotRt[0].anchorMin = slotRt[0].anchorMax = new Vector2(.43f, .57f); slotRt[1].anchorMin = slotRt[1].anchorMax = new Vector2(.56f, .57f);
                shutterRt.anchorMin = shutterRt.anchorMax = new Vector2(.61f, .43f);
                for (int g = 0; g < 2; g++) for (int i = 0; i < 4; i++)
                {
                    int j = (i + shuffledOffset + g) % 4; propRt[g, i].anchorMin = propRt[g, i].anchorMax = new Vector2(.09f + (j % 2) * .12f, .62f - g * .20f - (j / 2) * .10f);
                }
                for (int i = 0; i < filmRt.Length; i++) { int row = i / 4, col = i % 4; filmRt[i].anchorMin = filmRt[i].anchorMax = new Vector2(.73f + col * .065f, .58f - row * .095f); filmRt[i].sizeDelta = new Vector2(43, 52); }
                trashRt.anchorMin = trashRt.anchorMax = new Vector2(.73f, .15f); tearRt.anchorMin = tearRt.anchorMax = new Vector2(.94f, .15f); shotsText.rectTransform.anchorMin = shotsText.rectTransform.anchorMax = new Vector2(.83f, .73f);
            }
            else
            {
                promptPlateRt.anchorMin = promptPlateRt.anchorMax = new Vector2(.5f, .855f); promptPlateRt.sizeDelta = new Vector2(360, 104);
                promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = new Vector2(.5f, .865f); promptText.rectTransform.sizeDelta = new Vector2(340, 76); promptText.fontSize = 14;
                goalText.rectTransform.anchorMin = goalText.rectTransform.anchorMax = new Vector2(.5f, .775f);
                instructionText.rectTransform.anchorMin = instructionText.rectTransform.anchorMax = new Vector2(.5f, .715f);
                slotRt[0].anchorMin = slotRt[0].anchorMax = new Vector2(.38f, .58f); slotRt[1].anchorMin = slotRt[1].anchorMax = new Vector2(.62f, .58f); shutterRt.anchorMin = shutterRt.anchorMax = new Vector2(.84f, .54f);
                for (int g = 0; g < 2; g++) for (int i = 0; i < 4; i++)
                {
                    int j = (i + shuffledOffset + g) % 4; propRt[g, i].anchorMin = propRt[g, i].anchorMax = new Vector2(.14f + j * .24f, .36f - g * .09f);
                }
                for (int i = 0; i < filmRt.Length; i++) { int row = i / 7, col = i % 7; filmRt[i].anchorMin = filmRt[i].anchorMax = new Vector2(.18f + col * .105f, .13f - row * .07f); filmRt[i].sizeDelta = new Vector2(38, 46); }
                trashRt.anchorMin = trashRt.anchorMax = new Vector2(.08f, .08f); tearRt.anchorMin = tearRt.anchorMax = new Vector2(.92f, .09f); shotsText.rectTransform.anchorMin = shotsText.rectTransform.anchorMax = new Vector2(.5f, .205f);
            }
            if (!shuffleOnly) { titleLensRt.anchorMin = titleLensRt.anchorMax = wide ? new Vector2(.5f, .41f) : new Vector2(.5f, .39f); }
        }

        bool Contains(RectTransform rt, Vector2 screen) { return rt && rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, screen, null); }
        bool HitTitleLens(Vector2 p) => Contains(titleLensRt, p) || Vector2.Distance(p, new Vector2(Screen.width * .5f, Screen.height * .5f)) <= Mathf.Min(Screen.width, Screen.height) * .28f;
        bool HitRestart(Vector2 p) => Contains(restartRt, p);
        bool HitCamera(Vector2 p) => Contains(shutterRt, p);
        bool HitTear(Vector2 p) => Contains(tearRt, p);
        bool HitTrash(Vector2 p) => Contains(trashRt, p);
        int HitWearSlot(Vector2 p)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector2 c = RectTransformUtility.WorldToScreenPoint(null, slotRt[i].position);
                Vector2 radius = Vector2.Scale(slotRt[i].rect.size, slotRt[i].lossyScale) * .72f;
                if (Mathf.Abs(p.x - c.x) <= radius.x && Mathf.Abs(p.y - c.y) <= radius.y) return i;
            }
            return -1;
        }
        int HitProp(Vector2 p, out int group)
        {
            for (int g = 0; g < 2; g++) for (int i = 0; i < 4; i++)
            {
                if (!propRt[g, i].gameObject.activeInHierarchy) continue;
                Vector2 c = RectTransformUtility.WorldToScreenPoint(null, propRt[g, i].position);
                Vector2 scale = new Vector2(propRt[g, i].lossyScale.x, propRt[g, i].lossyScale.y);
                Vector2 radius = Vector2.Scale(propRt[g, i].rect.size, scale) * new Vector2(.72f, .88f);
                if (Mathf.Abs(p.x - c.x) <= radius.x && Mathf.Abs(p.y - c.y) <= radius.y) { group = g; return i; }
            }
            group = -1; return -1;
        }
        int HitPhoto(Vector2 p) { for (int i = 0; i < photos.Count && i < filmRt.Length; i++) if (Contains(filmRt[i], p)) return i; return -1; }

        Vector2 Local(Vector2 screen) { RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, screen, null, out Vector2 p); return p; }
        Vector2 Center(RectTransform rt) { return Local(RectTransformUtility.WorldToScreenPoint(null, rt.position)); }

        void BeginDragGhost(int group, int index, Vector2 screen)
        {
            dragGhostRt.gameObject.SetActive(true); dragGhost.color = group == 0 ? Yellow : Coral; dragGhostRt.anchoredPosition = Local(screen); dragGhostRt.localScale = Vector3.one * .86f;
        }
        void BeginPhotoDrag(int photo, Vector2 screen) { dragGhostRt.gameObject.SetActive(true); dragGhost.color = Cream; dragGhostRt.anchoredPosition = Local(screen); dragGhostRt.localScale = Vector3.one * .75f; }
        void MoveDragGhost(Vector2 screen) { dragGhostRt.anchoredPosition = Local(screen); dragGhostRt.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 10) * .04f); }
        void EndDragGhost() { dragGhostRt.gameObject.SetActive(false); }
        void MoveTear(float dy) { tearRt.anchoredPosition = tearBase + Vector2.up * Mathf.Clamp(dy, 0, 95); }
        void EndTear() { tearRt.anchoredPosition = tearBase; }

        void SpawnRipple(Vector2 screen) { rippleRt.gameObject.SetActive(true); rippleRt.anchoredPosition = Local(screen); rippleRt.localScale = Vector3.one * .35f; ripple.color = Cream; rippleClock = .32f; }
        void ShowToast(string message) { toastText.text = message; toastClock = 2.4f; toastG.alpha = 1; }
        void Refuse(string message) { ShowToast(message); refuseClock = .24f; MgfSfx.Play("wrong", .10f); PointToNextAction(); }
        void PunchPuffer() { MgfFx.Punch(pufferBody, .11f, .24f); }
        void PulseTitleLens() { MgfFx.Punch(titleLensRt, .12f, .26f); }
        void PulseRestart() { MgfFx.Punch(restartRt, .10f, .24f); }
        void AnticipateProp(int group, int index) { MgfFx.Punch(propRt[group, index], .09f, .18f); }
        void AnticipateTear() { MgfFx.Punch(tearRt, .09f, .18f); }
        void BlinkLensClosed() { shutterImage.transform.localScale = new Vector3(1f, .18f, 1f); MgfFx.Punch(shutterRt, .08f, .20f); }

        void ShootAnimation(string id, int index)
        {
            shootClock = .55f; flashClock = .20f; shutterImage.transform.localScale = Vector3.one; MgfFx.Punch(cameraBody, .08f, .24f); MgfFx.Punch(filmRt[Mathf.Min(index, filmRt.Length - 1)], .20f, .34f);
            MgfFx.Glow(puffer.position + Vector3.up * 1.2f, Cream, 8, .28f);
        }

        void OpenShutter() { MgfFx.Punch(titleLensWorld, .12f, .30f); }
        void StartGuide() { guideBoost = 1f; PointDragPath(0, 0); ShowToast("고깔을 복어 머리 자리까지 끌어 놓으시오."); }
        void BoostGuide() { guideBoost = 1f; PointToNextAction(); ShowToast(CurrentInstruction()); }
        void GuideCamera() { guideBoost = 1f; PointToCamera(); }
        void PointToNextProp() { PointDragPath(1, 0); }
        void PointToCamera() { SetGuide(shutterRt, shutterRt); }
        void PointToTear() { SetGuide(tearRt, tearRt); }
        void PointToTrash() { SetGuide(trashRt, trashRt); }
        void PointToNextAction()
        {
            if (phase != GamePhase.Practice) return;
            if (photos.Count >= current.answerCount) PointToTear();
            else if (wornLeft.Length > 0 || wornRight.Length > 0) PointToCamera();
            else PointDragPath(photos.Count == 0 ? 0 : 1, 0);
        }
        void PointDragPath(int group, int index) { if (group < 0 || index < 0) return; SetGuide(propRt[group, index], slotRt[group]); }
        void SetGuide(RectTransform from, RectTransform to) { guideRingRt.anchoredPosition = Center(to); guideRingRt.sizeDelta = to.sizeDelta + new Vector2(18, 18); guideHandRt.anchoredPosition = Center(from); }
        void UpdateGuide()
        {
            guideBoost = Mathf.Max(0, guideBoost - Time.deltaTime * .35f); float pulse = 1f + (.08f + guideBoost * .12f) * Mathf.Sin(worldClock * 5f);
            guideRingRt.localScale = Vector3.one * pulse; guideRing.color = new Color(Yellow.r, Yellow.g, Yellow.b, .68f + .25f * Mathf.Sin(worldClock * 4f));
            RectTransform from, to;
            if (photos.Count >= current.answerCount) { from = tearRt; to = tearRt; }
            else if (wornLeft.Length > 0 || wornRight.Length > 0) { from = shutterRt; to = shutterRt; }
            else { int g = photos.Count == 0 ? 0 : 1; from = propRt[g, 0]; to = slotRt[g]; }
            Vector2 a = Center(from), b = Center(to); float t = .5f - .5f * Mathf.Cos((worldClock % 1.2f) / 1.2f * Mathf.PI * 2f); guideHandRt.anchoredPosition = Vector2.Lerp(a, b, t);
        }

        void PlayCorrectFilm(string reveal, IList<string> submitted)
        {
            SetScreen(); feedbackRt.gameObject.SetActive(true); feedbackG.alpha = 1; feedbackTitle.text = "현상 성공"; feedbackBody.text = reveal + "\n학생이 찍은 " + submitted.Count + "장이 현상 격자에 나타납니다.";
            for (int i = 0; i < Mathf.Min(submitted.Count, photoPegs.Length); i++)
            {
                photoPegs[i].gameObject.SetActive(true); wallPhotos[i].gameObject.SetActive(true);
                photoPegs[i].localScale = Vector3.one; wallPhotos[i].localScale = Vector3.one;
                MgfFx.Punch(photoPegs[i], .35f, .55f + i * .025f); MgfFx.Punch(wallPhotos[i], .18f, .48f + i * .025f);
            }
            MgfFx.Glow(new Vector3(0, 3.0f, 6.0f), Yellow, 18, .38f); displayedScore = Mathf.Max(0, st.score - 100);
        }

        void PlayWrongFilm(string reason, string misconception)
        {
            SetScreen(); feedbackRt.gameObject.SetActive(true); feedbackG.alpha = 1; feedbackTitle.text = "필름을 다시 확인"; feedbackBody.text = reason + "\n사진을 비교하고 같은 의뢰를 다시 촬영하시오.";
            MgfFx.Punch(octopus, .08f, .28f); MgfFx.Shake(cam, .07f, .22f);
        }

        void StopFeedbackVisuals()
        {
            feedbackG.alpha = 0; feedbackRt.gameObject.SetActive(false); cam.transform.position = cameraHome;
            for (int i = 0; i < photoPegs.Length; i++) { photoPegs[i].gameObject.SetActive(false); wallPhotos[i].gameObject.SetActive(false); }
        }

        void ShowEnd(bool mastered, string reason)
        {
            SetScreen(); endRt.gameObject.SetActive(true); endG.alpha = 1; displayedEndScore = 0;
            endTitle.text = mastered ? "현상 격자 완성" : (st.solved >= MuneoRules.Goal ? "연습 필름 완성" : "보호고리 소진");
            restartText.text = "새 필름 걸기"; ShowToast(reason);
            if (mastered) { for (int i = 0; i < photoPegs.Length; i++) { photoPegs[i].gameObject.SetActive(true); wallPhotos[i].gameObject.SetActive(true); } MgfFx.Glow(puffer.position + Vector3.up * 1.5f, Yellow, 22, .45f); }
        }
    }
}
