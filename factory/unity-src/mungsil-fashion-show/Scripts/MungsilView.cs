using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.MungsilFashionShow
{
    public partial class MungsilFashionShowGame
    {
        // 선택안의 정체성: 햇빛 든 펠트 부티크, 플러시 생물, 자수와 나무 실패.
        static readonly Color Mint = MgfLook.Hex("#9ADBCB");
        static readonly Color MintDeep = MgfLook.Hex("#2B665E");
        static readonly Color Plum = MgfLook.Hex("#A44878");
        static readonly Color PlumDark = MgfLook.Hex("#50334F");
        static readonly Color Butter = MgfLook.Hex("#FFD66E");
        static readonly Color Coral = MgfLook.Hex("#F2776E");
        static readonly Color Cream = MgfLook.Hex("#FFF7E8");
        static readonly Color Ink = MgfLook.Hex("#35293A");
        static readonly Color Gold = MgfLook.Hex("#D99745");

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

            MgfLook.Sky(MgfLook.Hex("#CDEFE8"), MgfLook.Hex("#FFF2D7"), MgfLook.Hex("#F7B7AE"), .72f);
            MgfLook.Sun(new Vector3(48, -34, 18), MgfLook.Hex("#FFF0C4"), 1.18f, .72f);
            cam = MgfLook.Camera(new Vector3(10.2f, 8.3f, -15.5f), new Vector3(0, .65f, 1.8f), 34f);
            cameraHome = cam.transform.position;

            mintMat = MgfLook.Lit(Mint, .10f, 0f);
            mintDarkMat = MgfLook.Lit(MintDeep, .12f, 0f);
            plumMat = MgfLook.Lit(Plum, .10f, 0f);
            plumDarkMat = MgfLook.Lit(PlumDark, .10f, 0f);
            butterMat = MgfLook.Lit(Butter, .12f, 0f, Butter * .035f);
            coralMat = MgfLook.Lit(Coral, .10f, 0f);
            creamMat = MgfLook.Lit(Cream, .08f, 0f);
            goldMat = MgfLook.Lit(Gold, .16f, .02f, Gold * .025f);
            blackMat = MgfLook.Lit(MgfLook.Hex("#6E5149"), .08f, 0f);
            feltFloorMat = MgfLook.Lit(MgfLook.Hex("#EFA8A0"), .08f, 0f);
            dimMat = MgfLook.Lit(MgfLook.Hex("#CBB7DA"), .08f, 0f);
            glowMat = MgfLook.Lit(MgfLook.Hex("#FFF1A8"), .20f, 0f, Butter * .38f);

            worldRoot = new GameObject("SunlitPlushBoutique").transform;
            B("BoutiqueGarden", new Vector3(0, -1.02f, 2.1f), new Vector3(21f, 1.5f, 16.2f), .55f, creamMat, worldRoot);
            B("CoralFeltRug", new Vector3(0, -.22f, 1.75f), new Vector3(16.2f, .22f, 12.6f), .65f, feltFloorMat, worldRoot);
            B("MintRunway", new Vector3(0, .02f, 1.55f), new Vector3(13.9f, .30f, 10.8f), .45f, mintMat, worldRoot);
            B("CreamPatchwork", new Vector3(0, .24f, .42f), new Vector3(10.2f, .18f, 8.15f), .26f, creamMat, worldRoot);
            BuildCurtains(); BuildSewingProps(); BuildPedestals(); BuildMungsilPool(); BuildJudge(); BuildWorldRopes();
        }

        void BuildCurtains()
        {
            B("OpenSkyBackdrop", new Vector3(0, 2.25f, 6.65f), new Vector3(17.0f, 4.7f, .42f), .32f, MgfLook.Lit(MgfLook.Hex("#F8D5BF"), .08f, 0f), worldRoot);
            B("CanopyTop", new Vector3(0, 5.82f, 5.76f), new Vector3(15.3f, .48f, 1.25f), .22f, creamMat, worldRoot);
            for (int i = 0; i < 15; i++)
            {
                float x = -6.8f + i * .97f;
                B("AwningScallop", new Vector3(x, 5.52f, 5.12f), new Vector3(.83f, .58f, .26f), .25f,
                    i % 2 == 0 ? coralMat : butterMat, worldRoot);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                B("CanopyPost", new Vector3(side * 7.2f, 2.55f, 5.35f), new Vector3(.32f, 6.35f, .32f), .15f, goldMat, worldRoot);
                B("RibbonTie", new Vector3(side * 7.2f, 3.35f, 5.0f), new Vector3(.72f, .34f, .18f), .16f, plumMat, worldRoot);
            }

            curtainLeft = new GameObject("LeftFeltDrape").transform; curtainLeft.SetParent(worldRoot, false);
            curtainRight = new GameObject("RightFeltDrape").transform; curtainRight.SetParent(worldRoot, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Transform parent = side < 0 ? curtainLeft : curtainRight;
                for (int i = 0; i < 3; i++)
                {
                    float x = side * (4.95f + i * 1.02f);
                    var panel = B("PleatedFelt", new Vector3(x, 3.08f, 5.68f),
                        new Vector3(.92f, 4.65f, .34f), .28f, i % 2 == 0 ? plumMat : dimMat, parent).transform;
                    panel.localRotation = Quaternion.Euler(0, 0, side * (5f - i));
                    B("EmbroideredSeam", new Vector3(x, 3.08f, 5.47f), new Vector3(.08f, 3.76f, .045f), .04f,
                        i % 2 == 0 ? butterMat : creamMat, parent).transform.localRotation = Quaternion.Euler(0, 0, side * 5f);
                }
            }
            marquee = B("EmbroideredCanopySign", new Vector3(0, 4.75f, 5.21f), new Vector3(7.5f, 1.35f, .38f), .34f, plumMat, worldRoot).transform;
            B("CreamEmbroideryField", new Vector3(0, 4.75f, 4.98f), new Vector3(6.85f, .86f, .08f), .26f, creamMat, worldRoot);

            for (int i = 0; i < tassels.Length; i++)
            {
                float x = -6.6f + i * (13.2f / (tassels.Length - 1));
                var t = B("CanopyTassel", new Vector3(x, 5.27f - (i % 2) * .12f, 5.02f),
                    new Vector3(.28f, .52f, .16f), .13f, i % 3 == 0 ? coralMat : i % 3 == 1 ? mintMat : butterMat, worldRoot).transform;
                tassels[i] = t;
            }
        }

        void BuildSewingProps()
        {
            // 좌우 거터는 검은 기계함 대신 열린 실패 선반·쿠션·천 화분으로 채운다.
            for (int side = -1; side <= 1; side += 2)
            {
                B("BirchShelfBack", new Vector3(side * 6.75f, 1.45f, 1.85f), new Vector3(2.45f, 3.9f, 7.7f), .38f, creamMat, worldRoot);
                for (int r = 0; r < 3; r++)
                {
                    B("WoodShelf", new Vector3(side * 6.72f, .42f + r * 1.18f, 1.8f), new Vector3(2.30f, .16f, 7.15f), .08f, goldMat, worldRoot);
                    for (int c = 0; c < 3; c++)
                    {
                        var roll = MgfLook.Prim(PrimitiveType.Cylinder, "FeltRoll", new Vector3(side * (6.1f + c * .42f), .78f + r * 1.18f, -.30f + r * 2.05f),
                            new Vector3(.42f, .66f, .42f), (r + c) % 3 == 0 ? coralMat : (r + c) % 3 == 1 ? mintMat : dimMat, worldRoot, false);
                        roll.transform.localRotation = Quaternion.Euler(90, 0, side * (8f + c * 3f));
                    }
                }
            }
            for (int i = 0; i < spools.Length; i++)
            {
                float side = i < 5 ? -1f : 1f;
                int k = i % 5;
                var root = new GameObject("ThreadSpool" + i).transform; root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(side * (7.15f + (k % 2) * .45f), .75f + (k / 2) * .7f, 4.5f - k * 1.5f);
                B("SoftThread", Vector3.zero, new Vector3(.68f, .50f, .72f), .24f,
                    k % 3 == 0 ? coralMat : k % 3 == 1 ? mintMat : butterMat, root);
                B("WoodCapA", new Vector3(0, 0, -.41f), new Vector3(.82f, .60f, .10f), .05f, goldMat, root);
                B("WoodCapB", new Vector3(0, 0, .41f), new Vector3(.82f, .60f, .10f), .05f, goldMat, root);
                spools[i] = root;
            }

            var machine = new GameObject("PlushSewingMachine").transform; machine.SetParent(worldRoot, false);
            machine.localPosition = new Vector3(-6.2f, .55f, 4.4f);
            B("MachineCushion", Vector3.zero, new Vector3(2.05f, .42f, 1.30f), .20f, butterMat, machine);
            B("MachineBody", new Vector3(-.45f, .72f, .1f), new Vector3(.70f, 1.38f, .86f), .28f, mintMat, machine);
            B("MachineArm", new Vector3(.25f, 1.15f, .1f), new Vector3(1.28f, .46f, .76f), .20f, creamMat, machine);
            var wheel = MgfLook.Prim(PrimitiveType.Cylinder, "WoodWheel", new Vector3(-.82f, 1.06f, -.48f), new Vector3(.58f, .13f, .58f), plumMat, machine, false);
            wheel.transform.localRotation = Quaternion.Euler(90, 0, 0); spools[0] = wheel.transform;
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
                Material patch = (row + col) % 3 == 0 ? butterMat : (row + col) % 3 == 1 ? creamMat : dimMat;
                var pedestal = B("QuiltPatch" + (i + 1), p, new Vector3(1.08f, .20f, .94f), .24f, patch, worldRoot);
                pedestal.transform.localRotation = Quaternion.Euler(0, (row % 2 == 0 ? 1 : -1) * 3f, 0);
                pedestalRenderers[i] = pedestal.GetComponent<Renderer>();
                var light = MgfLook.Prim(PrimitiveType.Cylinder, "Spot" + (i + 1), p + Vector3.up * .16f,
                    new Vector3(.36f, .025f, .36f), glowMat, worldRoot, false).transform;
                light.gameObject.SetActive(false); spotlights[i] = light;
            }
        }

        void BuildMungsilPool()
        {
            Material[] bodies = { creamMat, butterMat, mintMat, coralMat, dimMat };
            Material[] accents = { plumMat, coralMat, butterMat, mintDarkMat };
            for (int i = 0; i < mungsils.Length; i++)
            {
                var root = new GameObject("PlushMungsil" + i).transform; root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(-5.7f, .4f, 4.6f); mungsils[i] = root;
                var body = MgfLook.Prim(PrimitiveType.Sphere, "RoundBody", new Vector3(0, .53f, 0),
                    new Vector3(.72f, .76f, .60f), bodies[i % bodies.Length], root, false).transform;
                mungsilBodies[i] = body;
                MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0, 1.04f, -.02f),
                    new Vector3(.62f, .58f, .56f), bodies[i % bodies.Length], root, false);
                MgfLook.Prim(PrimitiveType.Sphere, "EarL", new Vector3(-.25f, 1.31f, .0f), new Vector3(.20f, .25f, .18f), accents[i % accents.Length], root, false);
                MgfLook.Prim(PrimitiveType.Sphere, "EarR", new Vector3(.25f, 1.31f, .0f), new Vector3(.20f, .25f, .18f), accents[i % accents.Length], root, false);
                MgfLook.Prim(PrimitiveType.Sphere, "EyeL", new Vector3(-.12f, 1.09f, -.29f), new Vector3(.075f, .09f, .055f), blackMat, root, false);
                MgfLook.Prim(PrimitiveType.Sphere, "EyeR", new Vector3(.12f, 1.09f, -.29f), new Vector3(.075f, .09f, .055f), blackMat, root, false);
                B("StitchedScarf", new Vector3(0, .78f, -.31f), new Vector3(.52f, .14f, .07f), .065f, accents[(i + 1) % accents.Length], root);
                B("FootL", new Vector3(-.22f, .17f, -.02f), new Vector3(.26f, .18f, .35f), .12f, accents[i % accents.Length], root);
                B("FootR", new Vector3(.22f, .17f, -.02f), new Vector3(.26f, .18f, .35f), .12f, accents[i % accents.Length], root);
                var hat = B("SoftBow", new Vector3(0, 1.48f, .02f), new Vector3(.48f, .16f, .18f), .08f,
                    accents[(i + 2) % accents.Length], root).transform;
                hat.localRotation = Quaternion.Euler(0, 0, (i % 5 - 2) * 4f); mungsilHats[i] = hat;
                var trail = B("FeltShadow", new Vector3(0, .24f, .35f),
                    new Vector3(.72f, .06f, .58f), .24f, accents[(i + 2) % accents.Length], worldRoot).transform;
                trail.gameObject.SetActive(false); trails[i] = trail;
                root.gameObject.SetActive(false);
            }

            for (int i = 0; i < mannequins.Length; i++)
            {
                var root = new GameObject("EmptyHanger" + i).transform; root.SetParent(worldRoot, false);
                B("Hook", new Vector3(0, .86f, 0), new Vector3(.12f, .46f, .12f), .01f, goldMat, root);
                var bar = B("Hanger", new Vector3(0, .55f, 0), new Vector3(.74f, .10f, .10f), .01f, goldMat, root).transform;
                bar.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 13f : -13f);
                root.gameObject.SetActive(false); mannequins[i] = root;
            }
        }

        void BuildJudge()
        {
            judgeRoot = new GameObject("PlushTailorJudge").transform; judgeRoot.SetParent(worldRoot, false);
            judgeRoot.localPosition = new Vector3(5.45f, .65f, 3.9f);
            judgeBody = MgfLook.Prim(PrimitiveType.Sphere, "JudgeBody", new Vector3(0, .72f, 0),
                new Vector3(1.18f, 1.08f, .88f), creamMat, judgeRoot, false).transform;
            MgfLook.Prim(PrimitiveType.Sphere, "JudgeEyeL", new Vector3(-.23f, .90f, -.46f), new Vector3(.10f, .13f, .07f), blackMat, judgeRoot, false);
            MgfLook.Prim(PrimitiveType.Sphere, "JudgeEyeR", new Vector3(.23f, .90f, -.46f), new Vector3(.10f, .13f, .07f), blackMat, judgeRoot, false);
            B("PinCrown", new Vector3(0, 1.52f, 0), new Vector3(.82f, .20f, .64f), .10f, plumMat, judgeRoot).transform.localRotation = Quaternion.Euler(0, 0, -4f);
            B("ScoreCard", new Vector3(.78f, .72f, -.05f), new Vector3(.76f, .62f, .14f), .16f, butterMat, judgeRoot);
        }

        void BuildWorldRopes()
        {
            worldRope = BuildRope("VelvetLimitRope", plumMat);
            ghostRope = BuildRope("CorrectGhostRope", butterMat);
            worldRope.gameObject.SetActive(false); ghostRope.gameObject.SetActive(false);
        }

        Transform BuildRope(string name, Material ropeMat)
        {
            var root = new GameObject(name).transform; root.SetParent(worldRoot, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "PostL", new Vector3(-.48f, .58f, 0), new Vector3(.12f, .80f, .12f), goldMat, root, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "PostR", new Vector3(.48f, .58f, 0), new Vector3(.12f, .80f, .12f), goldMat, root, false);
            B("Velvet", new Vector3(0, .54f, 0), new Vector3(.92f, .18f, .16f), .08f, ropeMat, root);
            MgfLook.Prim(PrimitiveType.Sphere, "FinialL", new Vector3(-.48f, 1.04f, 0), new Vector3(.23f, .23f, .23f), goldMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "FinialR", new Vector3(.48f, 1.04f, 0), new Vector3(.23f, .23f, .23f), goldMat, root, false);
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

            // 떠 있는 CTA 카드가 아니라 3D 천막의 자수 현수막과 같은 폭·색으로 맞춘다.
            titleSignRt = R("CanopyEmbroidery", rt, new Vector2(.5f, .80f), Vector2.zero, new Vector2(360, 126));
            Img(titleSignRt, "PlumBanner", new Vector2(.5f, .5f), Vector2.zero, new Vector2(354, 112), new Color(Plum.r, Plum.g, Plum.b, .96f), roundSprite);
            Img(titleSignRt, "CreamStitches", new Vector2(.5f, .5f), Vector2.zero, new Vector2(346, 106), new Color(Cream.r, Cream.g, Cream.b, .88f), stitchSprite);
            titleLogo = Txt(titleSignRt, "뭉실 패션쇼", new Vector2(.5f, .62f), Vector2.zero, 43, Cream, 350);
            titleLogo.fontStyle = FontStyles.Bold; titleLogo.outlineWidth = .06f; titleLogo.outlineColor = PlumDark;
            titleTag = Txt(titleSignRt, "솜털 코디를 줄 끝까지 채워라", new Vector2(.5f, .23f), Vector2.zero, 17, Cream, 342);
            titleMeta = Txt(rt, "중학교 2학년 · 경우의 수  |  최고 쇼 0", new Vector2(.5f, .685f), Vector2.zero, 15, Ink, 360);

            // 시작 사물은 실제 벨벳 줄: 얇은 줄과 두 실패를 당기는 형태이며 알약 버튼이 아니다.
            titleSpoolRt = R("WorldVelvetRopeStart", rt, new Vector2(.5f, .13f), Vector2.zero, new Vector2(300, 88));
            Img(titleSpoolRt, "Rope", new Vector2(.5f, .42f), Vector2.zero, new Vector2(218, 14), Plum, roundSprite);
            Img(titleSpoolRt, "LeftSpool", new Vector2(.15f, .42f), Vector2.zero, new Vector2(46, 70), Gold, discSprite);
            Img(titleSpoolRt, "RightSpool", new Vector2(.85f, .42f), Vector2.zero, new Vector2(46, 70), Gold, discSprite);
            titleStartText = Txt(titleSpoolRt, "금색 줄을 당겨 입장", new Vector2(.5f, .86f), Vector2.zero, 17, Ink, 294);
        }

        void BuildHudUi()
        {
            var rt = R("Hud", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            hudG = rt.gameObject.AddComponent<CanvasGroup>();

            orderShadowRt = Img(rt, "OrderShadow", new Vector2(.5f, .88f), new Vector2(0, -5), new Vector2(374, 224), new Color(PlumDark.r, PlumDark.g, PlumDark.b, .32f), roundSprite).rectTransform;
            orderClothRt = Img(rt, "OrderCloth", new Vector2(.5f, .88f), Vector2.zero, new Vector2(374, 220), new Color(Cream.r, Cream.g, Cream.b, .98f), roundSprite).rectTransform;
            orderStitchRt = Img(rt, "OrderStitch", new Vector2(.5f, .88f), Vector2.zero, new Vector2(374, 220), new Color(Plum.r, Plum.g, Plum.b, .64f), stitchSprite).rectTransform;
            orderText = Txt(rt, "연습 주문", new Vector2(.12f, .965f), Vector2.zero, 14, Plum, 92);
            scoreText = Txt(rt, "0", new Vector2(.67f, .965f), Vector2.zero, 16, Ink, 72);
            livesText = Txt(rt, "바늘 ◆◆◆", new Vector2(.84f, .965f), Vector2.zero, 13, Coral, 104);
            applauseText = Txt(rt, "박수 0", new Vector2(.68f, .935f), Vector2.zero, 13, MintDeep, 86);
            goalText = Txt(rt, "코디의 경우의 수만큼 줄을 끌어라", new Vector2(.5f, .815f), Vector2.zero, 15, Plum, 350);
            promptRt = R("OrderPrompt", rt, new Vector2(.5f, .885f), Vector2.zero, new Vector2(356, 94));
            promptText = Txt(promptRt, "", new Vector2(.5f, .5f), Vector2.zero, 18, Ink, 346);
            promptText.enableAutoSizing = true; promptText.fontSizeMin = 15; promptText.fontSizeMax = 20;
            promptText.overflowMode = TextOverflowModes.Overflow;
            conceptText = Txt(rt, "", new Vector2(.5f, .775f), Vector2.zero, 13, Plum, 354);

            roulettePanelRt = R("RoulettePanel", rt, new Vector2(.5f, .285f), Vector2.zero, new Vector2(280, 130));
            rouletteClothRt = Img(roulettePanelRt, "Cloth", new Vector2(.5f, .5f), Vector2.zero, new Vector2(280, 126), new Color(Mint.r, Mint.g, Mint.b, .96f), roundSprite).rectTransform;
            Img(roulettePanelRt, "Stitches", new Vector2(.5f, .5f), Vector2.zero, new Vector2(274, 120), new Color(Cream.r, Cream.g, Cream.b, .64f), stitchSprite);
            for (int i = 0; i < 2; i++)
            {
                rouletteRt[i] = R(i == 0 ? "HatRoulette" : "RibbonRoulette", roulettePanelRt, new Vector2(.5f, .5f), new Vector2(i == 0 ? -68 : 68, 7), new Vector2(104, 104));
                Img(rouletteRt[i], "Dial", new Vector2(.5f, .5f), Vector2.zero, new Vector2(98, 98), i == 0 ? Plum : Coral, discSprite);
                Img(rouletteRt[i], "Inner", new Vector2(.5f, .5f), Vector2.zero, new Vector2(75, 75), Cream, discSprite);
                rouletteValue[i] = Txt(rouletteRt[i], "2", new Vector2(.5f, .56f), Vector2.zero, 29, i == 0 ? Plum : Coral, 82);
                Txt(rouletteRt[i], i == 0 ? "선택 A" : "선택 B", new Vector2(.5f, .23f), Vector2.zero, 12, Ink, 80);
            }
            rouletteUsesText = Txt(roulettePanelRt, "룰렛을 한 번 돌려 조합을 확인", new Vector2(.5f, .05f), Vector2.zero, 12, Ink, 260);

            ropeTrackRt = R("VelvetRopeTrack", rt, new Vector2(.5f, .13f), Vector2.zero, new Vector2(364, 110));
            ropeClothRt = Img(ropeTrackRt, "TrackCloth", new Vector2(.5f, .5f), Vector2.zero, new Vector2(364, 106), new Color(Cream.r, Cream.g, Cream.b, .98f), roundSprite).rectTransform;
            Img(ropeTrackRt, "TrackStitches", new Vector2(.5f, .5f), Vector2.zero, new Vector2(358, 100), new Color(Coral.r, Coral.g, Coral.b, .55f), stitchSprite);
            ropeRailRt = Img(ropeTrackRt, "VelvetRail", new Vector2(.5f, .47f), Vector2.zero, new Vector2(330, 13), Plum, roundSprite).rectTransform;
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
            ropeHandleImage = Img(ropeHandleRt, "GoldPost", new Vector2(.5f, .5f), Vector2.zero, new Vector2(58, 72), Gold, discSprite);
            ropeValueText = Txt(ropeHandleRt, "0", new Vector2(.5f, .56f), Vector2.zero, 24, PlumDark, 58);
            ropeInstructionText = Txt(ropeTrackRt, "벨벳 줄 손잡이를 끌고 놓아 제출", new Vector2(.5f, .95f), Vector2.zero, 13, Plum, 340);
        }

        void BuildEndUi()
        {
            var rt = R("EndScreen", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            endG = rt.gameObject.AddComponent<CanvasGroup>();
            Img(rt, "FinalDrape", new Vector2(.5f, .60f), Vector2.zero, new Vector2(350, 390), new Color(Plum.r, Plum.g, Plum.b, .94f), roundSprite);
            Img(rt, "FinalStitches", new Vector2(.5f, .60f), Vector2.zero, new Vector2(350, 390), new Color(Butter.r, Butter.g, Butter.b, .80f), stitchSprite);
            endTitle = Txt(rt, "앙코르 쇼", new Vector2(.5f, .73f), Vector2.zero, 40, Butter, 338);
            endTitle.fontStyle = FontStyles.Bold; endTitle.outlineWidth = .08f; endTitle.outlineColor = PlumDark;
            endScore = Txt(rt, "0점", new Vector2(.5f, .61f), Vector2.zero, 44, Cream, 330);
            endStats = Txt(rt, "", new Vector2(.5f, .47f), Vector2.zero, 18, Cream, 320);
            endSpoolRt = R("EndSpool", rt, new Vector2(.5f, .22f), Vector2.zero, new Vector2(205, 118));
            Img(endSpoolRt, "Gold", new Vector2(.5f, .5f), Vector2.zero, new Vector2(198, 108), Gold, discSprite);
            Img(endSpoolRt, "Core", new Vector2(.5f, .5f), Vector2.zero, new Vector2(154, 70), Mint, discSprite);
            endStartText = Txt(endSpoolRt, "새 쇼 열기", new Vector2(.5f, .5f), Vector2.zero, 20, PlumDark, 190);
        }

        void BuildOverlayUi()
        {
            feedbackRt = R("Feedback", rootRt, new Vector2(.5f, .66f), Vector2.zero, new Vector2(352, 132));
            feedbackWash = feedbackRt.gameObject.AddComponent<Image>(); feedbackWash.sprite = roundSprite; feedbackWash.raycastTarget = false;
            feedbackG = feedbackRt.gameObject.AddComponent<CanvasGroup>(); feedbackG.alpha = 0;
            feedbackTitle = Txt(feedbackRt, "", new Vector2(.5f, .72f), Vector2.zero, 27, PlumDark, 336);
            feedbackText = Txt(feedbackRt, "", new Vector2(.5f, .38f), Vector2.zero, 16, Ink, 332);

            toastRt = R("Toast", rootRt, new Vector2(.5f, .70f), Vector2.zero, new Vector2(350, 76));
            Img(toastRt, "ToastCloth", new Vector2(.5f, .5f), Vector2.zero, new Vector2(350, 72), new Color(PlumDark.r, PlumDark.g, PlumDark.b, .94f), roundSprite);
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
            if (phase == ShowPhase.Practice) return "2×3의 경우의 수만큼 줄을 끌어 놓으시오";
            return "코디의 경우의 수만큼 줄을 끌어 놓으시오";
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
                mungsils[i].localPosition = new Vector3(-3.2f + col * 1.6f, .45f, 2.8f - row * 1.55f);
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
                judgeRoot.localPosition = new Vector3(5.45f, .65f + Mathf.Sin(worldClock * 2.3f) * .04f, 3.9f);
                judgeBody.localScale = new Vector3(1f + Mathf.Sin(worldClock * 2f) * .018f, 1f - Mathf.Sin(worldClock * 2f) * .012f, 1f);
            }

            if (phase == ShowPhase.Title)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector3 p = mungsils[i].localPosition;
                    p.y = .45f + Mathf.Sin(worldClock * 2.5f + i * .7f) * .055f;
                    mungsils[i].localPosition = p;
                    mungsilHats[i].localRotation = Quaternion.Euler(0, 0, (i % 5 - 2) * 5f + Mathf.Sin(worldClock * 2f + i) * 3f);
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
                // 행렬 입장 대신 각 패턴 칸에서 의상이 접혔다가 제자리 조립된다.
                Vector3 start = target + new Vector3(0, -.34f, .18f);
                Vector3 p = Vector3.LerpUnclamped(start, target, eased);
                p.y += Mathf.Sin(local * Mathf.PI) * (.28f + (i % 3) * .03f);
                mungsils[i].localPosition = p;
                float stretch = Mathf.Sin(local * Mathf.PI);
                float land = Mathf.Clamp01((local - .78f) / .22f);
                mungsils[i].localScale = new Vector3(.16f + local * .84f + land * .05f, .08f + local * .92f - stretch * .08f, 1f);
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
                titleSignRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(worldClock * .9f) * .7f);
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
                SetPanel(orderStitchRt, new Vector2(.5f, .66f), new Vector2(infoX, 0), new Vector2(infoW - 12, infoH - 12));
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
                rouletteUsesText.rectTransform.sizeDelta = new Vector2(controlW - 18f, 36);
                SetPanel(ropeTrackRt, new Vector2(.5f, .31f), new Vector2(controlX, 0), new Vector2(controlW, 118));
                ropeClothRt.sizeDelta = new Vector2(controlW, 112); ropeRailRt.sizeDelta = new Vector2(controlW - 34f, 13);
                ropeInstructionText.rectTransform.sizeDelta = new Vector2(controlW - 18f, 39);
                SetRopeScale((controlW - 48f) * .5f);
                feedbackRt.anchorMin = feedbackRt.anchorMax = new Vector2(.50f, .54f); feedbackRt.sizeDelta = new Vector2(600, 142);
                titleLogo.fontSize = 47; titleTag.fontSize = 18;
                Place(titleMeta.rectTransform, new Vector2(.5f, .66f), Vector2.zero); titleMeta.color = Ink;
            }
            else
            {
                SetPanel(orderShadowRt, new Vector2(.5f, .84f), new Vector2(0, -5), new Vector2(374, 272));
                SetPanel(orderClothRt, new Vector2(.5f, .84f), Vector2.zero, new Vector2(374, 268));
                SetPanel(orderStitchRt, new Vector2(.5f, .84f), Vector2.zero, new Vector2(368, 262));
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
                rouletteUsesText.rectTransform.sizeDelta = new Vector2(260, 36);
                SetPanel(ropeTrackRt, new Vector2(.5f, .115f), Vector2.zero, new Vector2(364, 110));
                ropeClothRt.sizeDelta = new Vector2(364, 106); ropeRailRt.sizeDelta = new Vector2(330, 13);
                ropeInstructionText.rectTransform.sizeDelta = new Vector2(340, 39);
                SetRopeScale(157f);
                feedbackRt.anchorMin = feedbackRt.anchorMax = new Vector2(.5f, .61f); feedbackRt.sizeDelta = new Vector2(352, 132);
                titleLogo.fontSize = 43; titleTag.fontSize = 17;
                Place(titleMeta.rectTransform, new Vector2(.5f, .685f), Vector2.zero); titleMeta.color = Ink;
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
            feedbackTitle.text = $"✓ {answer}칸 코디 완성";
            feedbackTitle.color = PlumDark;
            feedbackText.text = reveal;
            feedbackWash.color = new Color(Mint.r, Mint.g, Mint.b, .96f);
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
            feedbackTitle.text = guessed < answer ? "△ 조립되지 않은 패턴이 남았습니다" : "△ 빈 옷걸이가 남았습니다";
            feedbackTitle.color = Coral;
            feedbackText.text = MisconceptionMessage(misconception) + "\n" + explanation;
            feedbackWash.color = new Color(Cream.r, Cream.g, Cream.b, .97f);
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
            endTitle.text = clear ? "앙코르 쇼 확정" : "오늘의 무대 폐막";
            string accuracy = st.firstAttemptTotal == 0 ? "0" : Mathf.RoundToInt(st.firstAttemptCorrect * 100f / st.firstAttemptTotal).ToString();
            endStats.text = $"완성 주문  {st.solved}/{MungsilRules.Goal}\n첫 시도 정확도  {accuracy}%\n최고 콤보  {st.combo}";
            endStartText.text = clear ? "앙코르 다시 열기" : "새 쇼 다시 열기";
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
