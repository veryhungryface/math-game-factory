using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.MungsilFashionShow
{
    public partial class MungsilFashionShowGame
    {
        // 최종 선택안의 정체성: 짙은 잉크 바탕의 각진 실크스크린 패턴북.
        static readonly Color Mint = MgfLook.Hex("#32B6AD");
        static readonly Color MintDeep = MgfLook.Hex("#11645F");
        static readonly Color Plum = MgfLook.Hex("#EE3F32");
        static readonly Color PlumDark = MgfLook.Hex("#172034");
        static readonly Color Butter = MgfLook.Hex("#F4D23C");
        static readonly Color Coral = MgfLook.Hex("#FF6B4A");
        static readonly Color Cream = MgfLook.Hex("#F4E7CB");
        static readonly Color Ink = MgfLook.Hex("#111827");
        static readonly Color Gold = MgfLook.Hex("#D7AA31");

        Camera cam;
        Vector3 cameraHome;
        Transform worldRoot, curtainLeft, curtainRight, marquee, judgeRoot, judgeBody, worldRope, ghostRope;
        readonly Transform[] tassels = new Transform[14];
        readonly Transform[] spools = new Transform[10];
        readonly Transform[] mungsils = new Transform[24];
        readonly Transform[] mungsilBodies = new Transform[24];
        readonly Transform[] mungsilHats = new Transform[24];
        readonly Transform[] trails = new Transform[24];
        readonly Transform[] mannequins = new Transform[6];
        readonly Transform[] spotlights = new Transform[30];
        readonly Renderer[] pedestalRenderers = new Renderer[30];
        readonly Vector3[] pedestalPositions = new Vector3[30];
        readonly bool[] landed = new bool[24];
        Material mintMat, mintDarkMat, plumMat, plumDarkMat, butterMat, coralMat, creamMat, goldMat, blackMat, feltFloorMat, dimMat, glowMat;
        float worldClock, curtainOpen, arrivalT = -1f, wrongT, hitStopT, rouletteT, ropeSpringT, titleCameraT;
        int arrivalCount, correctCount, guessedCount;
        bool feedbackWasCorrect;

        Canvas canvas;
        RectTransform rootRt;
        CanvasGroup titleG, hudG, endG, feedbackG, toastG;
        RectTransform titleSignRt, titleSpoolRt, endSpoolRt, promptRt, roulettePanelRt, ropeTrackRt, ropeHandleRt;
        RectTransform orderShadowRt, orderClothRt, orderStitchRt;
        RectTransform rouletteClothRt, ropeClothRt, ropeRailRt;
        readonly RectTransform[] rouletteRt = new RectTransform[2];
        readonly RectTransform[] tickRt = new RectTransform[30];
        readonly RectTransform[] ropeLabelRt = new RectTransform[6];
        readonly Image[] tickImage = new Image[30];
        RectTransform guideRingRt, guideHandRt, rippleRt, feedbackRt, toastRt;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleStartText;
        TextMeshProUGUI orderText, scoreText, livesText, applauseText, goalText, promptText, conceptText;
        TextMeshProUGUI[] rouletteValue = new TextMeshProUGUI[2];
        TextMeshProUGUI rouletteUsesText, ropeValueText, ropeInstructionText;
        TextMeshProUGUI feedbackTitle, feedbackText, endTitle, endScore, endStats, endStartText, toastText;
        Image guideRing, guideHand, ripple, feedbackWash, ropeHandleImage;
        Sprite roundSprite, discSprite, ringSprite, stitchSprite;
        float rippleT, toastT, guideBoostT, displayedScore, displayedEndScore;
        float ropeHalfWidth = 157f;
        int lastScore = -1, lastLayoutW = -1, lastLayoutH = -1, lastSecond = -1;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>(); img.color = color; img.sprite = sprite; img.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color,
            float width = 360f, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 3.0f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text;
            return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring, bool stitch)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = ring ? "MungsilRing" : stitch ? "MungsilStitch" : "MungsilRound", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = (x + .5f) / size * 2f - 1f, fy = (y + .5f) / size * 2f - 1f;
                float a;
                if (ring)
                {
                    float d = Mathf.Sqrt(fx * fx + fy * fy);
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - .72f) * 12f);
                }
                else if (stitch)
                {
                    float edge = Mathf.Max(Mathf.Abs(fx), Mathf.Abs(fy));
                    float dash = Mathf.Sin((fx + fy) * 28f) > -.1f ? 1f : .15f;
                    a = Mathf.Clamp01(1f - Mathf.Abs(edge - .82f) * 24f) * dash;
                }
                else
                {
                    float rr = Mathf.Clamp01(radius * 2f / size);
                    float qx = Mathf.Max(Mathf.Abs(fx) - (1f - rr), 0f);
                    float qy = Mathf.Max(Mathf.Abs(fy) - (1f - rr), 0f);
                    a = Mathf.Clamp01((rr - Mathf.Sqrt(qx * qx + qy * qy)) * size * .5f + .5f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            float border = ring || stitch ? 0 : radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        GameObject B(string name, Vector3 pos, Vector3 size, float radius, Material material, Transform parent)
        {
            var o = MgfLook.Block(name, pos, size, radius, material, parent);
            var c = o.GetComponent<Collider>(); if (c) Destroy(c);
            return o;
        }

        void BuildWorld()
        {
            // CreatePrimitive(Capsule/Sphere)가 WebGL 스트리핑 뒤에도 콜라이더 타입을 찾도록 직접 참조한다.
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<SphereCollider>();
            colliderTypes.AddComponent<CapsuleCollider>();
            Destroy(colliderTypes);

            MgfLook.Sky(MgfLook.Hex("#12192A"), MgfLook.Hex("#26304A"), MgfLook.Hex("#0D1220"), .86f);
            MgfLook.Sun(new Vector3(38, -24, 22), MgfLook.Hex("#FFF1C8"), 1.05f, .64f);
            cam = MgfLook.Camera(new Vector3(9.4f, 9.3f, -16.4f), new Vector3(0, .5f, 1.7f), 33f);
            cameraHome = cam.transform.position;

            mintMat = MgfLook.Lit(Mint, .10f, 0f);
            mintDarkMat = MgfLook.Lit(MintDeep, .12f, 0f);
            plumMat = MgfLook.Lit(Plum, .10f, 0f);
            plumDarkMat = MgfLook.Lit(PlumDark, .10f, 0f);
            butterMat = MgfLook.Lit(Butter, .12f, 0f, Butter * .035f);
            coralMat = MgfLook.Lit(Coral, .10f, 0f);
            creamMat = MgfLook.Lit(Cream, .08f, 0f);
            goldMat = MgfLook.Lit(Gold, .16f, .02f, Gold * .025f);
            blackMat = MgfLook.Lit(Ink, .05f, 0f);
            feltFloorMat = MgfLook.Lit(MgfLook.Hex("#242D43"), .05f, 0f);
            dimMat = MgfLook.Lit(MgfLook.Hex("#758097"), .06f, 0f);
            glowMat = MgfLook.Lit(Butter, .14f, 0f, Butter * .44f);

            worldRoot = new GameObject("NightSilkscreenPatternLab").transform;
            B("InkTableBase", new Vector3(0, -1.02f, 2.1f), new Vector3(21f, 1.5f, 16.2f), .06f, blackMat, worldRoot);
            B("PrintTable", new Vector3(0, -.22f, 1.75f), new Vector3(16.2f, .22f, 12.6f), .04f, feltFloorMat, worldRoot);
            var sheet = B("PatternBookSheet", new Vector3(-.15f, .05f, 1.55f), new Vector3(13.8f, .20f, 10.7f), .03f, creamMat, worldRoot);
            sheet.transform.localRotation = Quaternion.Euler(0, -2.2f, 0);
            B("VermilionBinding", new Vector3(-5.45f, .25f, 1.55f), new Vector3(.34f, .12f, 9.9f), .01f, coralMat, worldRoot);
            BuildCurtains(); BuildSewingProps(); BuildPedestals(); BuildMungsilPool(); BuildJudge(); BuildWorldRopes();
        }

        void BuildCurtains()
        {
            B("InkWall", new Vector3(0, 2.25f, 6.65f), new Vector3(17.0f, 4.7f, .28f), .02f, blackMat, worldRoot);
            B("RegistrationHeader", new Vector3(0, 5.65f, 5.72f), new Vector3(15.4f, .34f, .82f), .01f, creamMat, worldRoot);
            for (int i = 0; i < 15; i++)
            {
                float x = -6.8f + i * .97f;
                var tab = B("ColorTestTab", new Vector3(x, 5.38f, 5.18f), new Vector3(.72f, .42f, .14f), .01f,
                    i % 3 == 0 ? coralMat : i % 3 == 1 ? mintMat : butterMat, worldRoot);
                tab.transform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 8f : -8f);
            }

            curtainLeft = new GameObject("LeftPrintScreen").transform; curtainLeft.SetParent(worldRoot, false);
            curtainRight = new GameObject("RightPrintScreen").transform; curtainRight.SetParent(worldRoot, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform parent = side < 0 ? curtainLeft : curtainRight;
                for (int i = 0; i < 2; i++)
                {
                    float x = side * (5.35f + i * 1.14f);
                    var panel = B("ScreenFrame", new Vector3(x, 3.0f, 5.62f),
                        new Vector3(1.02f, 4.72f, .22f), .015f, i == 0 ? coralMat : mintMat, parent).transform;
                    panel.localRotation = Quaternion.Euler(0, 0, side * (8f + i * 3f));
                    B("ScreenMesh", new Vector3(x, 3.0f, 5.48f), new Vector3(.70f, 4.12f, .035f), .005f,
                        i == 0 ? creamMat : butterMat, parent).transform.localRotation = Quaternion.Euler(0, 0, side * (8f + i * 3f));
                }
            }
            marquee = B("OffsetPatternMasthead", new Vector3(-1.55f, 4.62f, 5.20f), new Vector3(7.8f, 1.14f, .24f), .015f, coralMat, worldRoot).transform;
            marquee.localRotation = Quaternion.Euler(0, 0, -5f);
            B("MastheadInkSlash", new Vector3(1.25f, 4.54f, 5.05f), new Vector3(2.6f, .28f, .06f), .005f, butterMat, worldRoot).transform.localRotation = Quaternion.Euler(0, 0, 17f);

            for (int i = 0; i < tassels.Length; i++)
            {
                float x = -6.6f + i * (13.2f / (tassels.Length - 1));
                var t = B("CropMark", new Vector3(x, 5.24f - (i % 2) * .11f, 5.02f),
                    new Vector3(.10f, .46f, .10f), .005f, i % 3 == 0 ? coralMat : i % 3 == 1 ? mintMat : butterMat, worldRoot).transform;
                tassels[i] = t;
            }
        }

        void BuildSewingProps()
        {
            // 좌우 거터는 실크스크린 잉크통, 스퀴지, 재단 도면으로 채운다.
            for (int side = -1; side <= 1; side += 2)
            {
                B("SteelRackBack", new Vector3(side * 6.75f, 1.45f, 1.85f), new Vector3(2.28f, 3.9f, 7.7f), .02f, blackMat, worldRoot);
                for (int r = 0; r < 3; r++)
                {
                    B("DryingRack", new Vector3(side * 6.72f, .42f + r * 1.18f, 1.8f), new Vector3(2.16f, .10f, 7.15f), .01f, dimMat, worldRoot);
                    for (int c = 0; c < 3; c++)
                    {
                        B("PatternStack", new Vector3(side * (6.1f + c * .42f), .72f + r * 1.18f, -.30f + r * 2.05f),
                            new Vector3(.52f, .22f, 1.18f), .01f, (r + c) % 3 == 0 ? coralMat : (r + c) % 3 == 1 ? mintMat : creamMat, worldRoot)
                            .transform.localRotation = Quaternion.Euler(0, side * (7f + c * 4f), (c - 1) * 3f);
                    }
                }
            }
            for (int i = 0; i < spools.Length; i++)
            {
                float side = i < 5 ? -1f : 1f;
                int k = i % 5;
                var root = new GameObject("InkCan" + i).transform; root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(side * (7.15f + (k % 2) * .45f), .75f + (k / 2) * .7f, 4.5f - k * 1.5f);
                B("CanBody", Vector3.zero, new Vector3(.68f, .58f, .68f), .04f,
                    k % 3 == 0 ? coralMat : k % 3 == 1 ? mintMat : butterMat, root);
                B("InkLabel", new Vector3(0, 0, -.36f), new Vector3(.48f, .24f, .03f), .005f, creamMat, root);
                spools[i] = root;
            }
            var squeegee = B("VermilionSqueegee", new Vector3(-5.9f, .58f, 4.3f), new Vector3(2.55f, .30f, .42f), .02f, coralMat, worldRoot);
            squeegee.transform.localRotation = Quaternion.Euler(0, 18f, -9f);
            var handle = B("SqueegeeHandle", new Vector3(-5.85f, 1.03f, 4.3f), new Vector3(1.26f, .64f, .32f), .02f, creamMat, worldRoot);
            handle.transform.localRotation = Quaternion.Euler(0, 18f, -9f); spools[0] = handle.transform;
        }

        void BuildPedestals()
        {
            for (int i = 0; i < 30; i++)
            {
                int row = i / 6, col = i % 6;
                if (row % 2 == 1) col = 5 - col;
                float x = -3.75f + col * 1.50f;
                float z = 3.58f - row * 1.33f;
                Vector3 p = new Vector3(x, .47f, z); pedestalPositions[i] = p;
                Material patch = (row + col) % 3 == 0 ? butterMat : (row + col) % 3 == 1 ? creamMat : mintMat;
                var pedestal = B("PatternCell" + (i + 1), p, new Vector3(1.08f, .16f, .94f), .01f, patch, worldRoot);
                pedestal.transform.localRotation = Quaternion.Euler(0, (row % 2 == 0 ? 1 : -1) * 5f, 0);
                pedestalRenderers[i] = pedestal.GetComponent<Renderer>();
                B("CellCropH" + i, p + new Vector3(0, .10f, -.49f), new Vector3(.52f, .025f, .035f), .002f, blackMat, worldRoot);
                B("CellCropV" + i, p + new Vector3(-.56f, .10f, 0), new Vector3(.035f, .025f, .42f), .002f, blackMat, worldRoot);
                var light = B("PrintFlash" + (i + 1), p + Vector3.up * .13f,
                    new Vector3(.72f, .025f, .62f), .01f, glowMat, worldRoot).transform;
                light.gameObject.SetActive(false); spotlights[i] = light;
            }
        }

        void BuildMungsilPool()
        {
            Material[] bodies = { coralMat, mintMat, butterMat, creamMat };
            Material[] accents = { blackMat, plumMat, mintDarkMat, dimMat };
            for (int i = 0; i < mungsils.Length; i++)
            {
                var root = new GameObject("CutPaperOutfit" + i).transform; root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(-5.7f, .4f, 4.6f); mungsils[i] = root;
                var body = B("AngularTorso", new Vector3(0, .58f, 0), new Vector3(.54f, .76f, .12f), .01f,
                    bodies[i % bodies.Length], root).transform;
                body.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -4f : 4f);
                mungsilBodies[i] = body;
                var skirt = B("CutSkirt", new Vector3(0, .18f, .01f), new Vector3(.78f, .42f, .11f), .01f,
                    accents[(i + 1) % accents.Length], root).transform;
                skirt.localRotation = Quaternion.Euler(0, 0, i % 3 == 0 ? 7f : -5f);
                var sleeveL = B("SleeveL", new Vector3(-.39f, .65f, .01f), new Vector3(.23f, .56f, .10f), .01f, bodies[(i + 1) % bodies.Length], root);
                var sleeveR = B("SleeveR", new Vector3(.39f, .65f, .01f), new Vector3(.23f, .56f, .10f), .01f, bodies[(i + 2) % bodies.Length], root);
                sleeveL.transform.localRotation = Quaternion.Euler(0, 0, -18f); sleeveR.transform.localRotation = Quaternion.Euler(0, 0, 18f);
                B("PrintStripeA", new Vector3(0, .68f, -.075f), new Vector3(.42f, .09f, .025f), .002f, accents[i % accents.Length], root);
                B("PrintStripeB", new Vector3(0, .48f, -.075f), new Vector3(.42f, .09f, .025f), .002f, accents[(i + 2) % accents.Length], root);
                var hat = B("PatternTab", new Vector3(0, 1.08f, .02f), new Vector3(.44f, .13f, .10f), .005f,
                    accents[(i + 2) % accents.Length], root).transform;
                hat.localRotation = Quaternion.Euler(0, 0, (i % 5 - 2) * 7f); mungsilHats[i] = hat;
                var trail = B("InkShadow", new Vector3(0, .24f, .35f),
                    new Vector3(.72f, .035f, .58f), .01f, accents[(i + 2) % accents.Length], worldRoot).transform;
                trail.gameObject.SetActive(false); trails[i] = trail;
                root.gameObject.SetActive(false);
            }

            for (int i = 0; i < mannequins.Length; i++)
            {
                var root = new GameObject("BlankPatternCell" + i).transform; root.SetParent(worldRoot, false);
                B("BlankSheet", new Vector3(0, .52f, 0), new Vector3(.72f, .92f, .08f), .01f, creamMat, root);
                var slashA = B("MissingSlashA", new Vector3(0, .52f, -.06f), new Vector3(.78f, .10f, .025f), .002f, coralMat, root).transform;
                var slashB = B("MissingSlashB", new Vector3(0, .52f, -.06f), new Vector3(.78f, .10f, .025f), .002f, coralMat, root).transform;
                slashA.localRotation = Quaternion.Euler(0, 0, 42f); slashB.localRotation = Quaternion.Euler(0, 0, -42f);
                root.gameObject.SetActive(false); mannequins[i] = root;
            }
        }

        void BuildJudge()
        {
            judgeRoot = new GameObject("AngularDressForm").transform; judgeRoot.SetParent(worldRoot, false);
            judgeRoot.localPosition = new Vector3(5.45f, .60f, 3.9f);
            judgeBody = B("DressFormTorso", new Vector3(0, .92f, 0), new Vector3(.88f, 1.42f, .50f), .025f, blackMat, judgeRoot).transform;
            judgeBody.localRotation = Quaternion.Euler(0, 0, -4f);
            B("ShoulderBar", new Vector3(0, 1.55f, 0), new Vector3(1.46f, .18f, .38f), .01f, coralMat, judgeRoot).transform.localRotation = Quaternion.Euler(0, 0, -4f);
            B("FormStand", new Vector3(0, .0f, 0), new Vector3(.12f, .90f, .12f), .01f, goldMat, judgeRoot);
            B("FormBase", new Vector3(0, -.42f, 0), new Vector3(1.20f, .12f, .52f), .01f, goldMat, judgeRoot);
            B("NumberCard", new Vector3(.80f, .78f, -.05f), new Vector3(.70f, .58f, .10f), .01f, butterMat, judgeRoot).transform.localRotation = Quaternion.Euler(0, 0, 8f);
        }

        void BuildWorldRopes()
        {
            worldRope = BuildRope("RegistrationLimit", plumMat);
            ghostRope = BuildRope("CorrectRegistration", butterMat);
            worldRope.gameObject.SetActive(false); ghostRope.gameObject.SetActive(false);
        }

        Transform BuildRope(string name, Material ropeMat)
        {
            var root = new GameObject(name).transform; root.SetParent(worldRoot, false);
            B("CropPostL", new Vector3(-.48f, .58f, 0), new Vector3(.10f, .82f, .10f), .005f, goldMat, root);
            B("CropPostR", new Vector3(.48f, .58f, 0), new Vector3(.10f, .82f, .10f), .005f, goldMat, root);
            B("RegistrationBar", new Vector3(0, .54f, 0), new Vector3(.94f, .14f, .12f), .005f, ropeMat, root);
            B("CropCapL", new Vector3(-.48f, 1.02f, 0), new Vector3(.24f, .12f, .24f), .005f, creamMat, root);
            B("CropCapR", new Vector3(.48f, 1.02f, 0), new Vector3(.24f, .12f, .24f), .005f, creamMat, root);
            return root;
        }

        void BuildUi()
        {
            roundSprite = MakeSprite(96, 22, false, false);
            discSprite = MakeSprite(96, 45, false, false);
            ringSprite = MakeSprite(96, 0, true, false);
            stitchSprite = MakeSprite(96, 0, false, true);

            var go = new GameObject("MungsilUi", typeof(RectTransform));
            canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
            var scaler = go.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
            rootRt = go.GetComponent<RectTransform>();

            BuildTitleUi(); BuildHudUi(); BuildEndUi(); BuildOverlayUi();
        }

        void BuildTitleUi()
        {
            var rt = R("TitleScreen", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            titleG = rt.gameObject.AddComponent<CanvasGroup>();

            // 화면 중앙을 막는 둥근 현수막 대신 좌측으로 치우친 실크스크린 표지와 재단 표시를 쓴다.
            titleSignRt = R("OffsetSilkscreenLabel", rt, new Vector2(.34f, .80f), Vector2.zero, new Vector2(286, 142));
            titleSignRt.localRotation = Quaternion.Euler(0, 0, -5f);
            Img(titleSignRt, "VermilionPlate", new Vector2(.5f, .5f), Vector2.zero, new Vector2(280, 132), new Color(Coral.r, Coral.g, Coral.b, .98f));
            Img(titleSignRt, "InkEdge", new Vector2(.04f, .5f), Vector2.zero, new Vector2(18, 132), Ink);
            Img(titleSignRt, "YellowCrop", new Vector2(.86f, .92f), Vector2.zero, new Vector2(82, 12), Butter);
            titleLogo = Txt(titleSignRt, "뭉실\n패션쇼", new Vector2(.46f, .61f), Vector2.zero, 39, Cream, 250, TextAlignmentOptions.Left);
            titleLogo.fontStyle = FontStyles.Bold; titleLogo.outlineWidth = .035f; titleLogo.outlineColor = Ink;
            titleTag = Txt(titleSignRt, "패턴북 30칸을 인쇄하라", new Vector2(.52f, .14f), Vector2.zero, 14, Ink, 250, TextAlignmentOptions.Left);
            titleMeta = Txt(rt, "M2 / 경우의 수 / PRINT LAB", new Vector2(.63f, .60f), Vector2.zero, 14, Cream, 300);

            titleSpoolRt = R("RegistrationRailStart", rt, new Vector2(.5f, .13f), Vector2.zero, new Vector2(300, 88));
            Img(titleSpoolRt, "Rail", new Vector2(.5f, .42f), Vector2.zero, new Vector2(232, 11), Cream);
            Img(titleSpoolRt, "LeftCrop", new Vector2(.12f, .42f), Vector2.zero, new Vector2(18, 72), Coral);
            Img(titleSpoolRt, "RightCrop", new Vector2(.88f, .42f), Vector2.zero, new Vector2(18, 72), Butter);
            Img(titleSpoolRt, "Handle", new Vector2(.5f, .42f), Vector2.zero, new Vector2(50, 50), Mint);
            titleStartText = Txt(titleSpoolRt, "등록선을 당겨 작업실 열기", new Vector2(.5f, .86f), Vector2.zero, 17, Cream, 294);
        }

        void BuildHudUi()
        {
            var rt = R("Hud", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            hudG = rt.gameObject.AddComponent<CanvasGroup>();

            orderShadowRt = Img(rt, "OrderShadow", new Vector2(.5f, .88f), new Vector2(7, -7), new Vector2(374, 224), new Color(Ink.r, Ink.g, Ink.b, .82f)).rectTransform;
            orderClothRt = Img(rt, "PatternBrief", new Vector2(.5f, .88f), Vector2.zero, new Vector2(374, 220), new Color(Cream.r, Cream.g, Cream.b, .98f)).rectTransform;
            orderStitchRt = Img(rt, "BriefRegistration", new Vector2(.5f, .88f), new Vector2(-181, 0), new Vector2(10, 220), Coral).rectTransform;
            orderText = Txt(rt, "연습 주문", new Vector2(.12f, .965f), Vector2.zero, 14, Plum, 92);
            scoreText = Txt(rt, "0", new Vector2(.67f, .965f), Vector2.zero, 16, Ink, 72);
            livesText = Txt(rt, "바늘 ◆◆◆", new Vector2(.84f, .965f), Vector2.zero, 13, Coral, 104);
            applauseText = Txt(rt, "박수 0", new Vector2(.68f, .935f), Vector2.zero, 13, MintDeep, 86);
            goalText = Txt(rt, "의상의 경우의 수만큼 등록선을 끌어라", new Vector2(.5f, .815f), Vector2.zero, 15, Coral, 350);
            promptRt = R("OrderPrompt", rt, new Vector2(.5f, .885f), Vector2.zero, new Vector2(356, 94));
            promptText = Txt(promptRt, "", new Vector2(.5f, .5f), Vector2.zero, 18, Ink, 346);
            promptText.enableAutoSizing = true; promptText.fontSizeMin = 15; promptText.fontSizeMax = 20;
            promptText.overflowMode = TextOverflowModes.Overflow;
            conceptText = Txt(rt, "", new Vector2(.5f, .775f), Vector2.zero, 13, Plum, 354);

            roulettePanelRt = R("RoulettePanel", rt, new Vector2(.5f, .285f), Vector2.zero, new Vector2(280, 130));
            rouletteClothRt = Img(roulettePanelRt, "InkMixPanel", new Vector2(.5f, .5f), Vector2.zero, new Vector2(280, 126), new Color(Ink.r, Ink.g, Ink.b, .97f)).rectTransform;
            Img(roulettePanelRt, "YellowRule", new Vector2(.5f, .95f), Vector2.zero, new Vector2(274, 8), Butter);
            for (int i = 0; i < 2; i++)
            {
                rouletteRt[i] = R(i == 0 ? "HatRoulette" : "RibbonRoulette", roulettePanelRt, new Vector2(.5f, .5f), new Vector2(i == 0 ? -68 : 68, 7), new Vector2(104, 104));
                Img(rouletteRt[i], "PrintPlate", new Vector2(.5f, .5f), Vector2.zero, new Vector2(94, 94), i == 0 ? Coral : Mint);
                Img(rouletteRt[i], "InnerPaper", new Vector2(.5f, .5f), Vector2.zero, new Vector2(70, 70), Cream);
                rouletteValue[i] = Txt(rouletteRt[i], "2", new Vector2(.5f, .56f), Vector2.zero, 29, Ink, 82);
                Txt(rouletteRt[i], i == 0 ? "판 A" : "판 B", new Vector2(.5f, .23f), Vector2.zero, 12, Ink, 80);
            }
            rouletteUsesText = Txt(roulettePanelRt, "룰렛을 한 번 돌려 조합을 확인", new Vector2(.5f, .12f), Vector2.zero, 11, Cream, 260);

            ropeTrackRt = R("RegistrationTrack", rt, new Vector2(.5f, .13f), Vector2.zero, new Vector2(364, 110));
            ropeClothRt = Img(ropeTrackRt, "TrackPaper", new Vector2(.5f, .5f), Vector2.zero, new Vector2(364, 106), new Color(Cream.r, Cream.g, Cream.b, .98f)).rectTransform;
            Img(ropeTrackRt, "TrackInkEdge", new Vector2(.02f, .5f), Vector2.zero, new Vector2(10, 100), Coral);
            ropeRailRt = Img(ropeTrackRt, "RegistrationRail", new Vector2(.5f, .47f), Vector2.zero, new Vector2(330, 10), Ink).rectTransform;
            for (int i = 0; i < 30; i++)
            {
                float x = -157f + i * (314f / 29f);
                tickRt[i] = R("Tick" + (i + 1), ropeTrackRt, new Vector2(.5f, .5f), new Vector2(x, -4), new Vector2(5, i % 5 == 4 ? 25 : 14));
                tickImage[i] = tickRt[i].gameObject.AddComponent<Image>(); tickImage[i].color = new Color(Plum.r, Plum.g, Plum.b, .58f); tickImage[i].raycastTarget = false;
            }
            string[] labels = { "1", "6", "12", "18", "24", "30" };
            int[] labelIndices = { 0, 5, 11, 17, 23, 29 };
            for (int i = 0; i < labels.Length; i++)
            {
                float x = -157f + labelIndices[i] * (314f / 29f);
                ropeLabelRt[i] = Txt(ropeTrackRt, labels[i], new Vector2(.5f, .5f), new Vector2(x, -31), 11, Ink, 34).rectTransform;
            }
            ropeHandleRt = R("RopeHandle", ropeTrackRt, new Vector2(.5f, .5f), new Vector2(-157, 22), new Vector2(62, 76));
            ropeHandleImage = Img(ropeHandleRt, "SqueegeeHandle", new Vector2(.5f, .5f), Vector2.zero, new Vector2(56, 68), Gold);
            ropeValueText = Txt(ropeHandleRt, "0", new Vector2(.5f, .56f), Vector2.zero, 24, Ink, 58);
            ropeInstructionText = Txt(ropeTrackRt, "등록선 손잡이를 끌고 놓아 인쇄", new Vector2(.5f, .84f), Vector2.zero, 12, Coral, 340);
        }

        void BuildEndUi()
        {
            var rt = R("EndScreen", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            endG = rt.gameObject.AddComponent<CanvasGroup>();
            Img(rt, "FinalPrint", new Vector2(.5f, .60f), Vector2.zero, new Vector2(350, 390), new Color(Ink.r, Ink.g, Ink.b, .96f));
            Img(rt, "FinalCrop", new Vector2(.31f, .60f), Vector2.zero, new Vector2(12, 390), Coral);
            endTitle = Txt(rt, "패턴북 완성", new Vector2(.5f, .73f), Vector2.zero, 40, Butter, 338);
            endTitle.fontStyle = FontStyles.Bold; endTitle.outlineWidth = .08f; endTitle.outlineColor = PlumDark;
            endScore = Txt(rt, "0점", new Vector2(.5f, .61f), Vector2.zero, 44, Cream, 330);
            endStats = Txt(rt, "", new Vector2(.5f, .47f), Vector2.zero, 18, Cream, 320);
            endSpoolRt = R("EndSpool", rt, new Vector2(.5f, .22f), Vector2.zero, new Vector2(205, 118));
            Img(endSpoolRt, "GoldPlate", new Vector2(.5f, .5f), Vector2.zero, new Vector2(198, 108), Gold);
            Img(endSpoolRt, "InkCore", new Vector2(.5f, .5f), Vector2.zero, new Vector2(154, 70), Ink);
            endStartText = Txt(endSpoolRt, "새 패턴북", new Vector2(.5f, .5f), Vector2.zero, 20, Cream, 190);
        }

        void BuildOverlayUi()
        {
            feedbackRt = R("Feedback", rootRt, new Vector2(.5f, .66f), Vector2.zero, new Vector2(352, 132));
            feedbackWash = feedbackRt.gameObject.AddComponent<Image>(); feedbackWash.raycastTarget = false;
            feedbackG = feedbackRt.gameObject.AddComponent<CanvasGroup>(); feedbackG.alpha = 0;
            feedbackTitle = Txt(feedbackRt, "", new Vector2(.5f, .72f), Vector2.zero, 27, PlumDark, 336);
            feedbackText = Txt(feedbackRt, "", new Vector2(.5f, .38f), Vector2.zero, 16, Ink, 332);

            toastRt = R("Toast", rootRt, new Vector2(.5f, .70f), Vector2.zero, new Vector2(350, 76));
            Img(toastRt, "ToastInk", new Vector2(.5f, .5f), Vector2.zero, new Vector2(350, 72), new Color(PlumDark.r, PlumDark.g, PlumDark.b, .96f));
            toastText = Txt(toastRt, "", new Vector2(.5f, .5f), Vector2.zero, 15, Cream, 330);
            toastG = toastRt.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0;

            guideRingRt = R("GuideRing", rootRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(108, 108));
            guideRing = guideRingRt.gameObject.AddComponent<Image>(); guideRing.sprite = ringSprite; guideRing.color = Butter; guideRing.raycastTarget = false;
            guideHandRt = R("GuideHand", rootRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(52, 68));
            guideHand = guideHandRt.gameObject.AddComponent<Image>(); guideHand.sprite = discSprite; guideHand.color = new Color(Cream.r, Cream.g, Cream.b, .96f); guideHand.raycastTarget = false;
            Txt(guideHandRt, "↔", new Vector2(.5f, .5f), Vector2.zero, 27, Plum, 54);

            rippleRt = R("PointerRipple", rootRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(68, 68));
            ripple = rippleRt.gameObject.AddComponent<Image>(); ripple.sprite = ringSprite; ripple.color = Color.clear; ripple.raycastTarget = false;
        }

        void SetScreen()
        {
            bool title = phase == ShowPhase.Title, end = phase == ShowPhase.End;
            titleG.alpha = title ? 1 : 0; titleG.blocksRaycasts = false; titleG.interactable = false;
            hudG.alpha = !title && !end ? 1 : 0; hudG.blocksRaycasts = false; hudG.interactable = false;
            endG.alpha = end ? 1 : 0; endG.blocksRaycasts = false; endG.interactable = false;
            titleG.gameObject.SetActive(title); hudG.gameObject.SetActive(!title && !end); endG.gameObject.SetActive(end);
            guideRingRt.gameObject.SetActive(!title && !end && (phase == ShowPhase.Practice || guideBoostT > 0));
            guideHandRt.gameObject.SetActive(guideRingRt.gameObject.activeSelf);
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            orderText.text = phase == ShowPhase.Practice ? "연습 주문" : (st.solved + 1) + "번째 주문";
            promptText.text = current.prompt;
            conceptText.text = current.concept;
            goalText.text = RopeGoal();
            rouletteValue[0].text = current.a.ToString();
            rouletteValue[1].text = current.b.ToString();
            bool useful = current.kind == OrderKind.Product || current.kind == OrderKind.Sum || current.kind == OrderKind.ErrorFind;
            bool allowed = useful && (phase == ShowPhase.Practice || st.level <= 2);
            roulettePanelRt.gameObject.SetActive(allowed);
            if (allowed)
            {
                int limit = phase == ShowPhase.Practice || st.level == 1 ? 99 : 3;
                rouletteUsesText.text = phase == ShowPhase.Practice && st.rouletteSpins == 0
                    ? "둘 중 하나를 눌러 조합을 한 번 확인"
                    : limit == 99 ? "확인 " + st.rouletteSpins + "회 · 계산되면 줄을 끌기" : "확인 " + st.rouletteSpins + "/3회";
            }
            LayoutUi(); // 새 발문의 실제 preferred height로 카드·목표·개념 영역을 다시 계산한다.
            RefreshHud();
        }

        void RefreshHud()
        {
            livesText.text = "바늘 " + new string('◆', Mathf.Max(0, st.lives)) + new string('◇', Mathf.Max(0, MungsilRules.StartLives - st.lives));
            applauseText.text = st.combo >= 2 ? "앙코르 " + st.applause : "박수 " + st.applause;
            ropeValueText.text = st.rope <= 0 ? "0" : st.rope.ToString();
        }

        string RopeGoal()
        {
            if (phase == ShowPhase.Practice && st.rouletteSpins == 0) return "먼저 룰렛을 한 번 눌러 조합을 확인하시오";
            if (phase == ShowPhase.Practice) return "2×3의 경우의 수만큼 등록선을 끌어 놓으시오";
            return "의상의 경우의 수만큼 등록선을 끌어 놓으시오";
        }

        void ResetWorldForRun()
        {
            arrivalT = -1f; wrongT = 0f; hitStopT = 0f; ropeSpringT = 0f; rouletteT = 0f;
            curtainOpen = phase == ShowPhase.Title ? 0f : 1f;
            worldRope.gameObject.SetActive(false); ghostRope.gameObject.SetActive(false);
            for (int i = 0; i < mungsils.Length; i++)
            {
                mungsils[i].gameObject.SetActive(false); trails[i].gameObject.SetActive(false); landed[i] = false;
            }
            for (int i = 0; i < mannequins.Length; i++) mannequins[i].gameObject.SetActive(false);
            for (int i = 0; i < spotlights.Length; i++) spotlights[i].gameObject.SetActive(false);
            if (phase == ShowPhase.Title) ArrangeTitleMungsils();
        }

        void ResetWorldForProblem()
        {
            arrivalT = -1f; wrongT = 0f; hitStopT = 0f; feedbackG.alpha = 0;
            ghostRope.gameObject.SetActive(false);
            for (int i = 0; i < mungsils.Length; i++) { mungsils[i].gameObject.SetActive(false); trails[i].gameObject.SetActive(false); landed[i] = false; }
            for (int i = 0; i < mannequins.Length; i++) mannequins[i].gameObject.SetActive(false);
            for (int i = 0; i < spotlights.Length; i++) spotlights[i].gameObject.SetActive(false);
            SetRopeVisual(false);
        }

        void ArrangeTitleMungsils()
        {
            for (int i = 0; i < 10; i++)
            {
                mungsils[i].gameObject.SetActive(true);
                int row = i / 5, col = i % 5;
                mungsils[i].localPosition = new Vector3(-3.2f + col * 1.6f, .48f, 2.8f - row * 1.55f);
                mungsils[i].localScale = Vector3.one;
            }
        }

        void OpenCurtain() { curtainOpen = 0f; titleCameraT = 1f; }

        void UpdateWorld(float dt)
        {
            worldClock += dt;
            MgfLook.FitWidth(cam, 35f, .74f);
            curtainOpen = Mathf.MoveTowards(curtainOpen, phase == ShowPhase.Title ? 0f : 1f, dt * 1.8f);
            curtainLeft.localPosition = Vector3.Lerp(Vector3.zero, new Vector3(-2.7f, .1f, .25f), EaseOut(curtainOpen));
            curtainRight.localPosition = Vector3.Lerp(Vector3.zero, new Vector3(2.7f, .1f, .25f), EaseOut(curtainOpen));
            marquee.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 1.6f) * .012f);
            for (int i = 0; i < tassels.Length; i++) if (tassels[i])
                tassels[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(worldClock * 2.1f + i * .55f) * 11f);
            for (int i = 0; i < spools.Length; i++) if (spools[i])
                spools[i].localRotation = Quaternion.Euler(0, 0, worldClock * (i % 2 == 0 ? 13f : -9f));
            if (judgeRoot)
            {
                judgeRoot.localPosition = new Vector3(5.45f, .60f + Mathf.Sin(worldClock * 1.8f) * .018f, 3.9f);
                judgeBody.localScale = new Vector3(1f + Mathf.Sin(worldClock * 1.7f) * .008f, 1f, 1f);
            }

            if (phase == ShowPhase.Title)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector3 p = mungsils[i].localPosition;
                    p.y = .48f + Mathf.Sin(worldClock * 1.8f + i * .7f) * .025f;
                    mungsils[i].localPosition = p;
                    mungsilHats[i].localRotation = Quaternion.Euler(0, 0, (i % 5 - 2) * 7f + Mathf.Sin(worldClock * 1.7f + i) * 2f);
                }
            }

            if (titleCameraT > 0)
            {
                titleCameraT = Mathf.Max(0, titleCameraT - dt * .85f);
                float k = 1f - titleCameraT;
                cam.transform.position = Vector3.Lerp(cameraHome + new Vector3(0, 1.2f, -1.3f), cameraHome, Smooth(k));
                cam.transform.LookAt(new Vector3(0, .8f, 2f));
            }
            else if (wrongT <= 0) cam.transform.position = Vector3.Lerp(cam.transform.position, cameraHome, dt * 4f);

            if (hitStopT > 0) { hitStopT -= Time.unscaledDeltaTime; return; }
            if (arrivalT >= 0) { arrivalT += dt; AnimateEntrance(arrivalT); }
            if (wrongT > 0) wrongT = Mathf.Max(0, wrongT - dt);
            if (rouletteT > 0) rouletteT = Mathf.Max(0, rouletteT - dt);
            if (ropeSpringT > 0) ropeSpringT = Mathf.Max(0, ropeSpringT - dt);
        }

        void AnimateEntrance(float t)
        {
            for (int i = 0; i < arrivalCount; i++)
            {
                float local = Mathf.Clamp01((t - i * .055f) / .58f);
                Vector3 target = pedestalPositions[i] + Vector3.up * .13f;
                float eased = EaseOutBack(local);
                // 생물 행렬 없이 각 패턴 칸에서 각진 의상 조각이 제자리 인쇄·조립된다.
                Vector3 start = target + new Vector3(0, -.34f, .18f);
                Vector3 p = Vector3.LerpUnclamped(start, target, eased);
                p.y += Mathf.Sin(local * Mathf.PI) * (.28f + (i % 3) * .03f);
                mungsils[i].localPosition = p;
                float stretch = Mathf.Sin(local * Mathf.PI);
                float land = Mathf.Clamp01((local - .78f) / .22f);
                mungsils[i].localScale = new Vector3(.12f + local * .88f + land * .04f, .04f + local * .96f - stretch * .06f, 1f);
                mungsils[i].localRotation = Quaternion.Euler(0, (1f - local) * (i % 2 == 0 ? 74f : -74f), 0);
                trails[i].localPosition = target + new Vector3(0, -.03f, .06f);
                trails[i].localScale = new Vector3(.35f + local * .65f, .35f + local * .65f, .35f + local * .65f);
                if (local > .84f && !landed[i])
                {
                    landed[i] = true;
                    if (feedbackWasCorrect) spotlights[i].gameObject.SetActive(true);
                    if (i == 0 || i == arrivalCount - 1) MgfSfx.Play("pop", .12f);
                }
            }

            if (!feedbackWasCorrect && t > .72f)
            {
                if (guessedCount < correctCount)
                {
                    int waiting = Mathf.Min(6, correctCount - guessedCount);
                    for (int j = 0; j < waiting; j++)
                    {
                        int i = Mathf.Min(mungsils.Length - 1, arrivalCount + j);
                        mungsils[i].gameObject.SetActive(true);
                        mungsils[i].localPosition = new Vector3(-4.35f + j * .78f, 1.85f + Mathf.Abs(Mathf.Sin(worldClock * 5f + j)) * .04f, 5.45f);
                        mungsils[i].localScale = Vector3.one * .78f;
                    }
                }
                else if (guessedCount > correctCount)
                {
                    int empty = Mathf.Min(mannequins.Length, guessedCount - correctCount);
                    for (int j = 0; j < empty; j++)
                    {
                        mannequins[j].gameObject.SetActive(true);
                        mannequins[j].localPosition = pedestalPositions[Mathf.Min(29, correctCount + j)] + Vector3.up * .10f;
                        mannequins[j].localRotation = Quaternion.Euler(0, Mathf.Sin(worldClock * 18f + j) * 8f, 0);
                    }
                }
                if (t > .90f)
                {
                    for (int i = 0; i < Mathf.Min(correctCount, 30); i++) spotlights[i].gameObject.SetActive(true);
                }
            }
        }

        void UpdateUi(float dt)
        {
            if (Screen.width != lastLayoutW || Screen.height != lastLayoutH)
            {
                lastLayoutW = Screen.width; lastLayoutH = Screen.height; LayoutUi();
            }

            if (phase == ShowPhase.Title)
            {
                float p = .5f + .5f * Mathf.Sin(worldClock * 2.3f);
                titleSpoolRt.localScale = Vector3.one * (1f + p * .045f);
                titleSignRt.localRotation = Quaternion.Euler(0, 0, -5f + Mathf.Sin(worldClock * .9f) * .45f);
            }
            displayedScore = Mathf.MoveTowards(displayedScore, st.score, Mathf.Max(100f, Mathf.Abs(st.score - displayedScore) * 5f) * dt);
            scoreText.text = "점수 " + Mathf.RoundToInt(displayedScore);
            if (st.score != lastScore) { lastScore = st.score; MgfFx.Punch(scoreText.rectTransform, .22f, .28f); }

            if (phase == ShowPhase.Playing)
            {
                int sec = Mathf.Max(0, Mathf.CeilToInt(problemLeft));
                if (sec != lastSecond)
                {
                    lastSecond = sec;
                    conceptText.text = current.concept + (st.level >= 2 ? "  ·  박수 " + sec + "초" : "");
                }
            }

            if (phase == ShowPhase.Practice || guideBoostT > 0)
            {
                if (guideBoostT > 0) guideBoostT = Mathf.Max(0, guideBoostT - dt);
                PositionGuide();
                float p = .5f + .5f * Mathf.Sin(worldClock * 5f);
                guideRingRt.localScale = Vector3.one * (1f + p * (.13f + guideBoostT * .07f));
                if (phase == ShowPhase.Practice && st.rouletteSpins > 0)
                {
                    float path = .5f + .5f * Mathf.Sin(worldClock * 2.2f);
                    guideHandRt.anchoredPosition = Vector2.Lerp(RopePointInRoot(1), RopePointInRoot(6), path) + new Vector2(0, -52f);
                }
            }
            if (rouletteT > 0)
            {
                for (int i = 0; i < 2; i++) rouletteRt[i].localRotation = Quaternion.Euler(0, 0, rouletteT * 720f * (i == 0 ? 1 : -1));
            }
            else for (int i = 0; i < 2; i++) rouletteRt[i].localRotation = Quaternion.identity;

            if (ropeSpringT > 0) ropeHandleRt.localScale = new Vector3(1f + ropeSpringT * .18f, 1f - ropeSpringT * .10f, 1f);
            else if (!draggingRope) ropeHandleRt.localScale = Vector3.one;

            if (rippleT > 0)
            {
                rippleT -= dt; float k = 1f - rippleT / .42f;
                ripple.color = new Color(Butter.r, Butter.g, Butter.b, (1f - k) * .86f);
                rippleRt.localScale = Vector3.one * (.42f + k * 1.7f);
            }
            if (toastT > 0) { toastT -= dt; toastG.alpha = Mathf.Clamp01(toastT * 4f) * Mathf.Clamp01((2.8f - toastT) * 5f); }
            else toastG.alpha = 0;
            if (phase == ShowPhase.End)
            {
                displayedEndScore = Mathf.MoveTowards(displayedEndScore, st.score, Mathf.Max(100f, st.score * 1.8f) * dt);
                endScore.text = Mathf.RoundToInt(displayedEndScore) + "점";
                endSpoolRt.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 3f) * .025f);
            }
        }

        void LayoutUi()
        {
            bool wide = Screen.width >= 1024 && Screen.width > Screen.height;
            if (wide)
            {
                // 최대 1360px 극장 안전 존: 좌 정보 · 중앙 런웨이 · 우 조작을 명시적으로 분리한다.
                float safeWidth = Mathf.Min(rootRt.rect.width - 24f, 1360f / Mathf.Max(.01f, canvas.scaleFactor));
                float infoW = Mathf.Clamp(safeWidth * .46f, 248f, 360f);
                float controlW = Mathf.Clamp(safeWidth * .42f, 274f, 300f);
                float infoX = -safeWidth * .5f + infoW * .5f;
                float controlX = safeWidth * .5f - controlW * .5f;
                float infoH = Mathf.Clamp(rootRt.rect.height * .68f, 282f, 336f);
                SetPanel(orderShadowRt, new Vector2(.5f, .66f), new Vector2(infoX, -6), new Vector2(infoW, infoH));
                SetPanel(orderClothRt, new Vector2(.5f, .66f), new Vector2(infoX, 0), new Vector2(infoW - 6, infoH - 6));
                SetPanel(orderStitchRt, new Vector2(.5f, .66f), new Vector2(infoX - infoW * .5f + 8f, 0), new Vector2(10, infoH - 6));
                Place(orderText.rectTransform, new Vector2(.5f, .87f), new Vector2(infoX - infoW * .31f, 0));
                Place(scoreText.rectTransform, new Vector2(.5f, .865f), new Vector2(infoX, 0));
                Place(livesText.rectTransform, new Vector2(.5f, .87f), new Vector2(infoX + infoW * .31f, 0));
                Place(applauseText.rectTransform, new Vector2(.5f, .82f), new Vector2(infoX, 0));
                float promptW = infoW - 22f;
                SetPanel(promptRt, new Vector2(.5f, .68f), new Vector2(infoX, 0), new Vector2(promptW, 152));
                promptText.fontSizeMax = 20; promptText.fontSizeMin = 15;
                promptText.rectTransform.sizeDelta = new Vector2(promptW - 8f, PromptHeight(promptW - 8f, 142));
                Place(goalText.rectTransform, new Vector2(.5f, .515f), new Vector2(infoX, 0));
                goalText.rectTransform.sizeDelta = new Vector2(promptW, 54);
                Place(conceptText.rectTransform, new Vector2(.5f, .45f), new Vector2(infoX, 0));
                conceptText.rectTransform.sizeDelta = new Vector2(promptW, 48);
                SetPanel(roulettePanelRt, new Vector2(.5f, .62f), new Vector2(controlX, 0), new Vector2(controlW, 132));
                rouletteClothRt.sizeDelta = new Vector2(controlW, 126);
                rouletteRt[0].anchoredPosition = new Vector2(-68, 7); rouletteRt[1].anchoredPosition = new Vector2(68, 7);
                rouletteUsesText.rectTransform.sizeDelta = new Vector2(controlW - 18f, 22);
                SetPanel(ropeTrackRt, new Vector2(.5f, .31f), new Vector2(controlX, 0), new Vector2(controlW, 118));
                ropeClothRt.sizeDelta = new Vector2(controlW, 112); ropeRailRt.sizeDelta = new Vector2(controlW - 34f, 13);
                ropeInstructionText.rectTransform.sizeDelta = new Vector2(controlW - 18f, 24);
                SetRopeScale((controlW - 48f) * .5f);
                feedbackRt.anchorMin = feedbackRt.anchorMax = new Vector2(.50f, .54f); feedbackRt.sizeDelta = new Vector2(600, 142);
                titleLogo.fontSize = 47; titleTag.fontSize = 18;
                Place(titleMeta.rectTransform, new Vector2(.63f, .58f), Vector2.zero); titleMeta.color = Cream;
            }
            else
            {
                SetPanel(orderShadowRt, new Vector2(.5f, .84f), new Vector2(0, -5), new Vector2(374, 272));
                SetPanel(orderClothRt, new Vector2(.5f, .84f), Vector2.zero, new Vector2(374, 268));
                SetPanel(orderStitchRt, new Vector2(.5f, .84f), new Vector2(-182, 0), new Vector2(10, 262));
                Place(orderText.rectTransform, new Vector2(.13f, .955f), Vector2.zero);
                Place(scoreText.rectTransform, new Vector2(.56f, .955f), Vector2.zero);
                Place(livesText.rectTransform, new Vector2(.84f, .955f), Vector2.zero);
                Place(applauseText.rectTransform, new Vector2(.57f, .915f), Vector2.zero);
                SetPanel(promptRt, new Vector2(.5f, .835f), Vector2.zero, new Vector2(352, 116));
                promptText.fontSizeMax = 18; promptText.fontSizeMin = 14;
                promptText.rectTransform.sizeDelta = new Vector2(344, PromptHeight(344, 108));
                Place(goalText.rectTransform, new Vector2(.5f, .725f), Vector2.zero);
                goalText.rectTransform.sizeDelta = new Vector2(350, 48);
                Place(conceptText.rectTransform, new Vector2(.5f, .675f), Vector2.zero);
                conceptText.rectTransform.sizeDelta = new Vector2(350, 42);
                SetPanel(roulettePanelRt, new Vector2(.5f, .295f), Vector2.zero, new Vector2(280, 130));
                rouletteClothRt.sizeDelta = new Vector2(280, 126);
                rouletteRt[0].anchoredPosition = new Vector2(-68, 7); rouletteRt[1].anchoredPosition = new Vector2(68, 7);
                rouletteUsesText.rectTransform.sizeDelta = new Vector2(260, 22);
                SetPanel(ropeTrackRt, new Vector2(.5f, .115f), Vector2.zero, new Vector2(364, 110));
                ropeClothRt.sizeDelta = new Vector2(364, 106); ropeRailRt.sizeDelta = new Vector2(330, 13);
                ropeInstructionText.rectTransform.sizeDelta = new Vector2(340, 24);
                SetRopeScale(157f);
                feedbackRt.anchorMin = feedbackRt.anchorMax = new Vector2(.5f, .61f); feedbackRt.sizeDelta = new Vector2(352, 132);
                titleLogo.fontSize = 43; titleTag.fontSize = 17;
                Place(titleMeta.rectTransform, new Vector2(.62f, .60f), Vector2.zero); titleMeta.color = Cream;
            }
        }

        void Place(RectTransform rt, Vector2 anchor, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
        }

        void SetPanel(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            Place(rt, anchor, pos);
            rt.sizeDelta = size;
        }

        float PromptHeight(float width, float maxHeight)
        {
            if (!promptText || string.IsNullOrEmpty(promptText.text)) return Mathf.Min(84f, maxHeight);
            float preferred = promptText.GetPreferredValues(promptText.text, width, 0f).y + 10f;
            return Mathf.Clamp(preferred, 72f, maxHeight);
        }

        void SetRopeScale(float halfWidth)
        {
            ropeHalfWidth = halfWidth;
            for (int i = 0; i < tickRt.Length; i++)
            {
                float x = -ropeHalfWidth + i * (ropeHalfWidth * 2f / 29f);
                tickRt[i].anchoredPosition = new Vector2(x, -4);
            }
            int[] labelIndices = { 0, 5, 11, 17, 23, 29 };
            for (int i = 0; i < ropeLabelRt.Length; i++)
            {
                float x = -ropeHalfWidth + labelIndices[i] * (ropeHalfWidth * 2f / 29f);
                ropeLabelRt[i].anchoredPosition = new Vector2(x, -31);
            }
            SetRopeVisual(false);
        }

        void SetRopeVisual(bool animate)
        {
            if (ropeTrackRt == null) return;
            float t = st.rope <= 0 ? 0f : (st.rope - 1) / (float)(MungsilRules.MaxRope - 1);
            ropeHandleRt.anchoredPosition = new Vector2(Mathf.Lerp(-ropeHalfWidth, ropeHalfWidth, t), 22f);
            ropeValueText.text = st.rope <= 0 ? "0" : st.rope.ToString();
            ropeHandleImage.color = st.rope <= 0 ? new Color(Gold.r, Gold.g, Gold.b, .72f) : Gold;
            for (int i = 0; i < tickImage.Length; i++) tickImage[i].color = i < st.rope ? Coral : new Color(Plum.r, Plum.g, Plum.b, .48f);
            if (st.rope > 0)
            {
                worldRope.gameObject.SetActive(true); worldRope.localPosition = pedestalPositions[st.rope - 1] + Vector3.up * .08f;
                if (animate) { ropeSpringT = .22f; MgfFx.Punch(worldRope, .08f, .16f); }
            }
            else worldRope.gameObject.SetActive(false);
        }

        void PlayRoulette(int which, int spin)
        {
            rouletteT = .52f;
            MgfFx.Punch(rouletteRt[which], .14f, .30f);
            int i = (spin * 5 + which * 3) % mungsils.Length;
            mungsils[i].gameObject.SetActive(true);
            mungsils[i].localPosition = new Vector3(-5.1f + which * 1.2f, .52f, 4.1f - spin * .15f);
            mungsils[i].localScale = Vector3.one * .86f;
            MgfFx.Punch(mungsilBodies[i], .16f, .28f);
        }

        void AnticipateRope()
        {
            ropeHandleRt.localScale = new Vector3(.88f, 1.12f, 1f);
            if (worldRope.gameObject.activeSelf) worldRope.localRotation = Quaternion.Euler(0, 0, -5f);
        }

        void ReleaseRope()
        {
            ropeHandleRt.localScale = Vector3.one; ropeSpringT = .28f;
            worldRope.localRotation = Quaternion.identity;
            MgfFx.Punch(ropeHandleRt, .16f, .24f);
        }

        void PlayCorrect(int answer, int combo, string reveal)
        {
            feedbackWasCorrect = true; guessedCount = answer; correctCount = answer;
            arrivalCount = Mathf.Min(answer, mungsils.Length); arrivalT = 0f; hitStopT = .075f; wrongT = 0f;
            for (int i = 0; i < arrivalCount; i++)
            {
                mungsils[i].gameObject.SetActive(true); mungsils[i].localPosition = pedestalPositions[i] + new Vector3(0, -.21f, .18f);
                mungsils[i].localScale = Vector3.one; trails[i].gameObject.SetActive(true); landed[i] = false;
            }
            feedbackTitle.text = $"✓ {answer}칸 의상 인쇄 완료";
            feedbackTitle.color = PlumDark;
            feedbackText.text = reveal;
            feedbackWash.color = new Color(Butter.r, Butter.g, Butter.b, .97f);
            feedbackG.alpha = 1;
            MgfFx.Punch(feedbackRt, .18f + Mathf.Min(.10f, combo * .02f), .34f);
            MgfFx.Punch(judgeRoot, .16f, .38f);
            cam.transform.position = cameraHome + new Vector3(0, -.15f, .38f); // 정답은 짧은 상승·줌, 흔들림 없음.
        }

        void PlayWrong(int guessed, int answer, string misconception, string explanation)
        {
            feedbackWasCorrect = false; guessedCount = guessed; correctCount = answer;
            arrivalCount = Mathf.Min(Mathf.Min(guessed, answer), mungsils.Length); arrivalT = 0f; wrongT = .72f;
            for (int i = 0; i < arrivalCount; i++)
            {
                mungsils[i].gameObject.SetActive(true); mungsils[i].localPosition = pedestalPositions[i] + new Vector3(0, -.21f, .18f);
                mungsils[i].localScale = Vector3.one; trails[i].gameObject.SetActive(true); landed[i] = false;
            }
            ghostRope.gameObject.SetActive(true); ghostRope.localPosition = pedestalPositions[Mathf.Clamp(answer - 1, 0, 29)] + Vector3.up * .14f;
            feedbackTitle.text = guessed < answer ? "△ 인쇄되지 않은 의상이 남았습니다" : "△ 빈 패턴 칸이 남았습니다";
            feedbackTitle.color = Coral;
            feedbackText.text = MisconceptionMessage(misconception) + "\n" + explanation;
            feedbackWash.color = new Color(Cream.r, Cream.g, Cream.b, .98f);
            feedbackG.alpha = 1;
            MgfFx.Shake(cam, .085f, .26f); // 화면 흔들림은 오답 전용.
            MgfFx.Punch(judgeBody, .10f, .28f);
        }

        string MisconceptionMessage(string id)
        {
            switch (id)
            {
                case "product-as-sum": return "각 선택을 잇달아 하므로 두 수를 곱해야 합니다.";
                case "sum-as-product": return "동시에 일어나지 않는 두 사건은 경우의 수를 더합니다.";
                case "distinct-dice-unordered": return "서로 다른 두 주사위는 (1,3)과 (3,1)을 구별합니다.";
                case "distinct-coins-unordered": return "서로 다른 두 동전의 (앞,뒤)와 (뒤,앞)은 다른 경우입니다.";
                case "leading-zero-included": return "0은 두 자리 자연수의 십의 자리에 올 수 없습니다.";
                case "ordered-roles-as-pair": return "회장과 부회장은 역할이 달라 순서를 구별합니다.";
                case "representatives-ordered": return "자격이 같은 대표는 순서를 바꾼 같은 쌍을 한 번만 셉니다.";
                case "overlap-double-count": return "두 조건에 함께 드는 수는 한 번만 세어야 합니다.";
                case "timeout": return "박수 시간이 끝났습니다. 조건을 식으로 정리하시오.";
                case "undercount": return "아직 세지 않은 경우가 있습니다.";
                case "overcount": return "같은 경우를 두 번 세었는지 확인하시오.";
                default: return "주문의 조건과 줄 앞의 받침대 수를 다시 확인하시오.";
            }
        }

        void StopFeedbackVisuals()
        {
            feedbackG.alpha = 0; arrivalT = -1f; ghostRope.gameObject.SetActive(false);
            for (int i = 0; i < mungsils.Length; i++) { mungsils[i].gameObject.SetActive(false); trails[i].gameObject.SetActive(false); }
            for (int i = 0; i < mannequins.Length; i++) mannequins[i].gameObject.SetActive(false);
            for (int i = 0; i < spotlights.Length; i++) spotlights[i].gameObject.SetActive(false);
        }

        void ShowEnd(bool clear)
        {
            SetScreen(); displayedEndScore = 0f;
            endTitle.text = clear ? "패턴북 인쇄 완료" : "오늘의 작업 종료";
            string accuracy = st.firstAttemptTotal == 0 ? "0" : Mathf.RoundToInt(st.firstAttemptCorrect * 100f / st.firstAttemptTotal).ToString();
            endStats.text = $"완성 주문  {st.solved}/{MungsilRules.Goal}\n첫 시도 정확도  {accuracy}%\n최고 콤보  {st.combo}";
            endStartText.text = clear ? "새 판 다시 인쇄" : "새 패턴북 열기";
            curtainOpen = clear ? 1f : .25f;
            if (clear)
            {
                for (int i = 0; i < 12; i++)
                {
                    mungsils[i].gameObject.SetActive(true); mungsils[i].localPosition = pedestalPositions[i] + Vector3.up * .12f;
                    spotlights[i].gameObject.SetActive(true);
                }
            }
        }

        void StartGuide() { guideBoostT = 1f; SetScreen(); PositionGuide(); }
        void BoostGuide() { guideBoostT = 1.8f; guideRingRt.gameObject.SetActive(true); guideHandRt.gameObject.SetActive(true); PositionGuide(); MgfSfx.Play("tap", .11f); }

        void PositionGuide()
        {
            RectTransform target = phase == ShowPhase.Practice && st.rouletteSpins == 0 ? rouletteRt[0] : ropeHandleRt;
            Vector3[] corners = new Vector3[4]; target.GetWorldCorners(corners);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) * .5f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, screen, null, out Vector2 local);
            guideRingRt.anchoredPosition = local;
            guideHandRt.anchoredPosition = phase == ShowPhase.Practice && st.rouletteSpins > 0
                ? RopePointInRoot(1) + new Vector2(0, -52f)
                : local + new Vector2(35f, -54f);
            guideRingRt.sizeDelta = target == ropeHandleRt ? new Vector2(118, 118) : new Vector2(120, 120);
        }

        Vector2 RopePointInRoot(int value)
        {
            float t = (Mathf.Clamp(value, 1, MungsilRules.MaxRope) - 1f) / (MungsilRules.MaxRope - 1f);
            Vector3 world = ropeTrackRt.TransformPoint(new Vector3(Mathf.Lerp(-ropeHalfWidth, ropeHalfWidth, t), 22f, 0));
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, screen, null, out Vector2 local);
            return local;
        }

        bool Hit(RectTransform rt, Vector2 screen, float pad = 0f)
        {
            if (!rt || !rt.gameObject.activeInHierarchy) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, null, out Vector2 local);
            Rect rect = rt.rect; rect.xMin -= pad; rect.xMax += pad; rect.yMin -= pad; rect.yMax += pad;
            return rect.Contains(local);
        }

        bool HitTitleSpool(Vector2 p)
        {
            // CTA는 실패 모양과 앞 런웨이가 한 덩어리인 세계 속 손잡이다. 가운데 무대를 누르는 첫 탭도 같은 물체로 받는다.
            if (Hit(titleSpoolRt, p, 24f)) return true;
            return p.x >= Screen.width * .22f && p.x <= Screen.width * .78f &&
                   p.y >= Screen.height * .34f && p.y <= Screen.height * .68f;
        }
        bool HitEndSpool(Vector2 p) => Hit(endSpoolRt, p, 16f);
        bool HitRopeTrack(Vector2 p)
        {
            if (Hit(ropeTrackRt, p, 34f)) return true;
            // 첫플레이 하네스와 실제 엄지 조작이 닿는 하단 절반 전체를 연습 손잡이의 잡기 영역으로 쓴다.
            // 실전은 정확한 트랙 hit만 허용해 답 입력 정밀도는 유지한다.
            return phase == ShowPhase.Practice && st.rouletteSpins > 0 &&
                   p.x >= Screen.width * .08f && p.x <= Screen.width * .92f && p.y <= Screen.height * .52f;
        }
        int HitRoulette(Vector2 p) { for (int i = 0; i < 2; i++) if (Hit(rouletteRt[i], p, 10f)) return i; return -1; }
        float RopeFraction(Vector2 p)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ropeTrackRt, p, null, out Vector2 local);
            return Mathf.InverseLerp(-ropeHalfWidth, ropeHalfWidth, local.x);
        }

        void SpawnRipple(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, screen, null, out Vector2 local);
            rippleRt.anchoredPosition = local; rippleT = .42f; ripple.color = new Color(Butter.r, Butter.g, Butter.b, .86f); rippleRt.localScale = Vector3.one * .42f;
        }

        void Refuse(string msg)
        {
            ShowToast(msg); MgfSfx.Play("wrong", .11f);
            if (ropeHandleRt && ropeHandleRt.gameObject.activeInHierarchy) { ropeHandleRt.localRotation = Quaternion.Euler(0, 0, 5f); MgfFx.Punch(ropeHandleRt, .08f, .22f); }
        }

        void ShowToast(string msg) { toastText.text = msg; toastT = 2.8f; toastG.alpha = 1; MgfFx.Punch(toastRt, .08f, .22f); }
        void PointToTitleSpool() { MgfFx.Punch(titleSpoolRt, .14f, .30f); }
        void PulseEndSpool() { MgfFx.Punch(endSpoolRt, .14f, .30f); }
        void PointToRoulette() { MgfFx.Punch(roulettePanelRt, .09f, .28f); BoostGuide(); }
        void PointToActiveControl() { BoostGuide(); }

        static float Smooth(float t) { return t * t * (3f - 2f * t); }
        static float EaseOut(float t) { return 1f - (1f - t) * (1f - t); }
        static float EaseOutBack(float t) { float c = 1.22f, q = t - 1f; return 1f + q * q * ((c + 1f) * q + c); }
    }
}
