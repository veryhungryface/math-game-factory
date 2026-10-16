// 자두 집게 — 라일락 수관 2.5D 디오라마, 집게 계측 UI, 온보딩과 수확 연출.
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.JaduJipge
{
    public partial class JaduJipgeGame
    {
        // 깊은 수관 절개 무대: 파스텔 온실과 분리되는 어두운 녹청·목재·법랑 계열.
        static readonly Color Lilac = MgfLook.Hex("#17393A");
        static readonly Color Plum = MgfLook.Hex("#582A55");
        static readonly Color Ivory = MgfLook.Hex("#FFF3CF");
        static readonly Color Leaf = MgfLook.Hex("#789C62");
        static readonly Color Bark = MgfLook.Hex("#5A3428");
        static readonly Color Ink = MgfLook.Hex("#182B2B");
        static readonly Color Amber = MgfLook.Hex("#D89B45");
        static readonly Color SoftGreen = MgfLook.Hex("#DCE7B2");

        const float ArmCenterDeg = 90f;

        Camera cam;
        Transform worldRoot;
        Transform canopyRoot;
        Transform branchRoot;
        Transform beetleRoot;
        Transform beetleBody;
        Transform beetleHead;
        Transform antennaLeft;
        Transform antennaRight;
        Transform titlePlum;
        Transform targetPlum;
        Transform cartRoot;
        Transform cartBody;
        Transform bruiseBin;
        Transform activePad;
        Transform harvestNetRoot;
        Vector3 cartHome;
        readonly Transform[] canopy = new Transform[30];
        readonly Transform[] idlePlums = new Transform[12];
        readonly Transform[] legs = new Transform[6];
        readonly Transform[] harvestPlums = new Transform[10];
        readonly Transform[] bruisedPlums = new Transform[3];
        readonly Transform[] chuteLeaves = new Transform[5];
        Material lilacMat;
        Material lilacLightMat;
        Material plumMat;
        Material plumGlowMat;
        Material ivoryMat;
        Material leafMat;
        Material barkMat;
        Material darkMat;
        Material amberMat;
        float worldClock;
        float revealVisualClock;
        float titlePress;
        float cartBounce;
        float wrongWobble;
        Vector3 beetleHome;
        Vector3 targetPlumHome;
        bool worldReveal;
        bool worldCorrect;

        CanvasGroup titleG;
        CanvasGroup hudG;
        CanvasGroup revealG;
        CanvasGroup endG;
        CanvasGroup toastG;
        RectTransform rootRt;
        RectTransform titleSignRt;
        RectTransform titlePlumRingRt;
        RectTransform refCardRt;
        RectTransform clawRootRt;
        RectTransform armURt;
        RectTransform armVRt;
        RectTransform railURt;
        RectTransform railVRt;
        RectTransform handleURt;
        RectTransform handleVRt;
        RectTransform angleKnobRt;
        RectTransform jointRt;
        RectTransform guideLineRt;
        RectTransform guideHandRt;
        RectTransform rippleRt;
        RectTransform toastRt;
        readonly RectTransform[] angleTicks = new RectTransform[JaduRules.AngleCount];
        readonly Image[] railUTicks = new Image[12];
        readonly Image[] railVTicks = new Image[12];
        readonly TextMeshProUGUI[] railUNumbers = new TextMeshProUGUI[12];
        readonly TextMeshProUGUI[] railVNumbers = new TextMeshProUGUI[12];
        readonly Image[] touchKnots = new Image[3];
        readonly Image[] padIcons = new Image[JaduRules.StartLives];
        Image titleSign;
        Image titlePlumRing;
        Image refCard;
        Image armUImg;
        Image armVImg;
        Image handleUImg;
        Image handleVImg;
        Image railUImg;
        Image railVImg;
        Image angleKnobImg;
        Image targetUImg;
        Image targetVImg;
        Image jointImg;
        Image guideLine;
        Image guideHand;
        Image rippleImg;
        Image revealPanel;
        Image endPanel;
        Image toastBg;
        Image calloutUBg;
        Image calloutVBg;
        Image calloutABg;
        Image leaderU;
        Image leaderV;
        Image leaderA;
        RectTransform leaderURt;
        RectTransform leaderVRt;
        RectTransform leaderARt;
        TextMeshProUGUI titleShadow;
        TextMeshProUGUI titleLogo;
        TextMeshProUGUI titleTag;
        TextMeshProUGUI titleMeta;
        TextMeshProUGUI titleBest;
        TextMeshProUGUI titleStart;
        TextMeshProUGUI scoreTxt;
        TextMeshProUGUI progressTxt;
        TextMeshProUGUI timerTxt;
        TextMeshProUGUI goalTxt;
        TextMeshProUGUI refTitleTxt;
        TextMeshProUGUI refValuesTxt;
        TextMeshProUGUI instructionTxt;
        TextMeshProUGUI currentTxt;
        TextMeshProUGUI handleULabel;
        TextMeshProUGUI handleVLabel;
        TextMeshProUGUI angleLabel;
        TextMeshProUGUI revealTitleTxt;
        TextMeshProUGUI revealFormulaTxt;
        TextMeshProUGUI toastTxt;
        TextMeshProUGUI endTitleTxt;
        TextMeshProUGUI endScoreTxt;
        TextMeshProUGUI endStatsTxt;
        TextMeshProUGUI endTapTxt;
        Sprite roundSprite;
        Sprite ringSprite;
        Sprite thinSprite;
        float displayedScore;
        float endDisplayedScore;
        float toastClock;
        float rippleClock;
        float guideClock;
        Vector2 guideStart;
        Vector2 guideEnd;
        bool guideHasPath;
        int guideControl;
        int shownScore = -1;
        int shownSecond = -1;
        int layoutW = -1;
        int layoutH = -1;
        bool landscape;
        float armUnit = 17f;

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

        RectTransform FullGroup(string name, Transform parent, out CanvasGroup group)
        {
            var rt = R(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            group = rt.gameObject.AddComponent<CanvasGroup>();
            return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color,
            float width = 360f, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 2.4f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.text = text;
            return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = ring ? "JaduRing" : "JaduRound",
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + .5f;
                float fy = y + .5f;
                float alpha;
                if (ring)
                {
                    float dx = fx - size * .5f;
                    float dy = fy - size * .5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(1f - Mathf.Abs(d - size * .38f) / (size * .075f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * .5f) - (size * .5f - radius), 0f);
                    float qy = Mathf.Max(Mathf.Abs(fy - size * .5f) - (size * .5f - radius), 0f);
                    alpha = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            float border = ring ? 0f : radius + 2f;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        void BuildWorld()
        {
            // WebGL 엔진 스트리핑이 CreatePrimitive가 내부에서 붙이는 콜라이더 타입을 제거하지 않게
            // 명시적으로 참조한다. 실제 게임 입력은 UI 좌표이므로 이 임시 오브젝트는 즉시 없앤다.
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<SphereCollider>();
            colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<BoxCollider>();
            Object.Destroy(colliderTypes);

            MgfLook.Sky(MgfLook.Hex("#071C1F"), MgfLook.Hex("#17393A"), MgfLook.Hex("#6D5440"), .95f);
            MgfLook.Sun(new Vector3(42f, -36f, 14f), MgfLook.Hex("#FFE3A8"), 1.28f, .62f);
            cam = MgfLook.Camera(new Vector3(11.5f, 13.8f, -18.5f), new Vector3(0f, 1.3f, 2.2f), 34f);
            cam.orthographic = true;
            cam.orthographicSize = 8.2f;

            lilacMat = MgfLook.Lit(MgfLook.Hex("#264E43"), .16f, .01f);
            lilacLightMat = MgfLook.Lit(MgfLook.Hex("#3F6B54"), .20f, .01f);
            plumMat = MgfLook.Lit(Plum, .70f, .12f, Plum * .08f);
            plumGlowMat = MgfLook.Lit(MgfLook.Hex("#6C3D72"), .84f, .16f, MgfLook.Hex("#C878C8") * .24f);
            ivoryMat = MgfLook.Lit(Ivory, .58f, .08f);
            leafMat = MgfLook.Lit(Leaf, .26f, .01f);
            barkMat = MgfLook.Lit(Bark, .16f, .01f);
            darkMat = MgfLook.Lit(Ink, .26f, .04f);
            amberMat = MgfLook.Lit(Amber, .36f, .03f);

            worldRoot = new GameObject("PlumCanopyWorld").transform;
            canopyRoot = new GameObject("LilacCanopy").transform;
            canopyRoot.SetParent(worldRoot, false);
            branchRoot = new GameObject("Branches").transform;
            branchRoot.SetParent(worldRoot, false);

            BuildCanopy();
            BuildChuteAndCart();
            BuildBeetle();
            BuildFruitActors();
        }

        Transform CylinderBetween(string name, Vector3 a, Vector3 b, float radius, Material mat, Transform parent)
        {
            Vector3 d = b - a;
            var o = MgfLook.Prim(PrimitiveType.Cylinder, name, (a + b) * .5f,
                new Vector3(radius, d.magnitude * .5f, radius), mat, parent, false);
            o.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            return o.transform;
        }

        void BuildCanopy()
        {
            // 껍질 단면이 화면 깊이를 채우는 수관 절개 무대. 동심 목질 링과 어두운
            // 잎 가장자리가 주인공이고, 둥근 파스텔 매트/온실은 사용하지 않는다.
            Material heartwood = MgfLook.Lit(MgfLook.Hex("#A9673F"), .20f, .02f);
            Material sapwood = MgfLook.Lit(MgfLook.Hex("#D2A26A"), .25f, .01f);
            Material innerBark = MgfLook.Lit(MgfLook.Hex("#75452F"), .12f, .01f);
            var cutaway = new GameObject("CanopyCutawayRings").transform;
            cutaway.SetParent(worldRoot, false);
            cutaway.localPosition = new Vector3(0f, 2.7f, 9.5f);
            float[] ringSizes = { 9.8f, 8.7f, 7.4f, 6.2f, 4.9f, 3.5f };
            for (int r = 0; r < ringSizes.Length; r++)
            {
                Material mat = r == 0 ? barkMat : (r & 1) == 0 ? heartwood : sapwood;
                var disc = MgfLook.Prim(PrimitiveType.Cylinder, "GrowthRing" + r, new Vector3(0f, 0f, r * -.045f),
                    new Vector3(ringSizes[r], .16f, ringSizes[r] * .82f), mat, cutaway, false);
                disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            var core = MgfLook.Prim(PrimitiveType.Cylinder, "CutawayCore", new Vector3(0f, 0f, -.3f),
                new Vector3(1.8f, .17f, 1.45f), innerBark, cutaway, false);
            core.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            CylinderBetween("WorkBranch", new Vector3(-10f, -.62f, 2.6f), new Vector3(10f, -.12f, 3.4f), .62f,
                barkMat, branchRoot);
            CylinderBetween("DepthTrunkLeft", new Vector3(-7.2f, -3.2f, 8.5f), new Vector3(-6.1f, 8.2f, 8.9f), .72f,
                barkMat, branchRoot);
            CylinderBetween("DepthTrunkRight", new Vector3(7.5f, -2.8f, 9.4f), new Vector3(5.9f, 8.4f, 8.1f), .66f,
                barkMat, branchRoot);
            for (int i = 0; i < canopy.Length; i++)
            {
                int column = i % 6;
                int level = i / 6;
                float x = -8.8f + column * 3.45f + (level % 2) * .55f;
                float z = 7.8f + (column % 3) * 1.05f + level * .35f;
                float y = -.3f + level * 1.65f + (column % 2) * .28f;
                var mat = (i & 1) == 0 ? lilacMat : lilacLightMat;
                canopy[i] = MgfLook.Prim(PrimitiveType.Capsule, "CanopyLeaf" + i, new Vector3(x, y, z),
                    new Vector3(.38f + (i % 3) * .05f, 1.30f + (i % 2) * .16f, .28f), mat, canopyRoot, false).transform;
                canopy[i].localRotation = Quaternion.Euler(66f, (i * 37) % 180, -28f + (i % 5) * 14f);
            }

            CylinderBetween("MainBranch", new Vector3(-9f, 1.2f, 5.4f), new Vector3(9f, 3.45f, 6.3f), .38f, barkMat, branchRoot);
            CylinderBetween("BranchLeft", new Vector3(-5.6f, 1.62f, 5.62f), new Vector3(-8.4f, 6.7f, 8.5f), .22f, barkMat, branchRoot);
            CylinderBetween("BranchRight", new Vector3(3.8f, 2.78f, 5.98f), new Vector3(7.5f, 7.0f, 9.1f), .23f, barkMat, branchRoot);

            for (int i = 0; i < idlePlums.Length; i++)
            {
                float x = -7.8f + (i % 6) * 3.0f;
                float z = 7.2f + (i / 6) * 2.4f + (i % 2) * .35f;
                float y = 1.8f + (i % 4) * 1.05f;
                idlePlums[i] = MgfLook.Prim(PrimitiveType.Sphere, "IdlePlum" + i, new Vector3(x, y, z),
                    new Vector3(.66f, .78f, .62f), plumMat, worldRoot, false).transform;
                CylinderBetween("PlumStem" + i, idlePlums[i].position + Vector3.up * .54f,
                    idlePlums[i].position + new Vector3(.08f, 1.0f, .04f), .055f, barkMat, worldRoot);
            }
        }

        void BuildChuteAndCart()
        {
            for (int i = 0; i < chuteLeaves.Length; i++)
            {
                float x = -3.8f + i * 1.75f;
                float y = .75f - i * .15f;
                float z = 1.5f + i * .35f;
                var leaf = MgfLook.Prim(PrimitiveType.Sphere, "ChuteLeaf" + i, new Vector3(x, y, z),
                    new Vector3(1.45f, .14f, .7f), leafMat, worldRoot, false);
                leaf.transform.localRotation = Quaternion.Euler(0f, -18f + i * 8f, -6f);
                chuteLeaves[i] = leaf.transform;
            }

            cartRoot = new GameObject("PlumCart").transform;
            cartRoot.SetParent(worldRoot, false);
            cartRoot.localPosition = new Vector3(5.8f, -.05f, 3.0f);
            cartHome = cartRoot.localPosition;
            cartBody = MgfLook.Block("CartBody", Vector3.zero, new Vector3(3.25f, 1.1f, 2.2f), .28f, barkMat, cartRoot).transform;
            MgfLook.Block("CartLip", new Vector3(0f, .62f, -.78f), new Vector3(3.4f, .26f, .34f), .10f, ivoryMat, cartRoot);
            for (int i = 0; i < 2; i++)
            {
                var wheel = MgfLook.Prim(PrimitiveType.Cylinder, "CartWheel" + i,
                    new Vector3(i == 0 ? -1.15f : 1.15f, -.56f, -.78f), new Vector3(.52f, .20f, .52f), darkMat, cartRoot, false);
                wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            for (int i = 0; i < harvestPlums.Length; i++)
            {
                float x = -.95f + (i % 4) * .62f;
                float y = .35f + (i / 4) * .45f;
                float z = -.05f + (i % 2) * .45f;
                harvestPlums[i] = MgfLook.Prim(PrimitiveType.Sphere, "HarvestedPlum" + i,
                    new Vector3(x, y, z), new Vector3(.38f, .45f, .37f), plumMat, cartRoot, false).transform;
                harvestPlums[i].gameObject.SetActive(false);
            }

            bruiseBin = MgfLook.Block("BruisedFruitBin", new Vector3(-6.6f, -.42f, 2.0f), new Vector3(2.5f, .65f, 2f), .25f,
                amberMat, worldRoot).transform;
            for (int i = 0; i < bruisedPlums.Length; i++)
            {
                bruisedPlums[i] = MgfLook.Prim(PrimitiveType.Sphere, "BruisedPlum" + i,
                    new Vector3(-7.25f + i * .62f, .05f, 1.7f + (i % 2) * .35f), new Vector3(.42f, .37f, .42f),
                    darkMat, worldRoot, false).transform;
                bruisedPlums[i].gameObject.SetActive(false);
            }
        }

        void BuildBeetle()
        {
            beetleRoot = new GameObject("OriginalPlumClawBeetle").transform;
            beetleRoot.SetParent(worldRoot, false);
            beetleHome = new Vector3(0f, .28f, .35f);
            beetleRoot.localPosition = beetleHome;

            beetleBody = MgfLook.Prim(PrimitiveType.Sphere, "SeedBody", Vector3.zero,
                new Vector3(1.28f, .72f, 1.48f), plumMat, beetleRoot, false).transform;
            beetleHead = MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0f, .28f, -.92f),
                new Vector3(.86f, .62f, .72f), plumGlowMat, beetleRoot, false).transform;
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -.29f : .29f;
                MgfLook.Prim(PrimitiveType.Sphere, "EyeWhite" + i, new Vector3(x, .44f, -1.42f),
                    new Vector3(.25f, .28f, .18f), ivoryMat, beetleRoot, false);
                MgfLook.Prim(PrimitiveType.Sphere, "EyePupil" + i, new Vector3(x, .45f, -1.56f),
                    new Vector3(.105f, .12f, .07f), darkMat, beetleRoot, false);
            }

            for (int i = 0; i < legs.Length; i++)
            {
                bool left = i < 3;
                int row = i % 3;
                Vector3 a = new Vector3(left ? -.72f : .72f, -.1f, -.5f + row * .52f);
                Vector3 b = a + new Vector3(left ? -.78f : .78f, -.48f, -.18f + row * .08f);
                legs[i] = CylinderBetween("Leg" + i, a, b, .095f, darkMat, beetleRoot);
            }

            antennaLeft = CylinderBetween("AntennaLeft", new Vector3(-.28f, .58f, -1.18f), new Vector3(-.70f, 1.16f, -1.62f),
                .045f, darkMat, beetleRoot);
            antennaRight = CylinderBetween("AntennaRight", new Vector3(.28f, .58f, -1.18f), new Vector3(.70f, 1.16f, -1.62f),
                .045f, darkMat, beetleRoot);

            // 법랑 신축팔은 계측 UI가 정확한 정면도로 보여 주고, 월드에서는 캐릭터의 짧은 받침팔로 존재한다.
            CylinderBetween("WorldClawLeft", new Vector3(-.35f, .25f, -.7f), new Vector3(-1.55f, 1.0f, -1.65f), .14f,
                ivoryMat, beetleRoot);
            CylinderBetween("WorldClawRight", new Vector3(.35f, .25f, -.7f), new Vector3(1.55f, 1.0f, -1.65f), .14f,
                ivoryMat, beetleRoot);
        }

        void BuildFruitActors()
        {
            // 타이틀의 단일 CTA인 실제 자두. 카메라 쪽 전경에 두어 벌레와 겹쳐 가려지지 않게 한다.
            Vector3 titleFruitPos = beetleHome + new Vector3(0f, 1.82f, -2.05f);
            titlePlum = MgfLook.Prim(PrimitiveType.Sphere, "TitlePlum", titleFruitPos,
                new Vector3(1.30f, 1.52f, 1.22f), plumGlowMat, worldRoot, false).transform;
            CylinderBetween("TitlePlumStem", titleFruitPos + Vector3.up * .55f,
                titleFruitPos + new Vector3(.08f, 1.22f, .16f), .055f, barkMat, worldRoot);
            targetPlum = MgfLook.Prim(PrimitiveType.Sphere, "TargetPlum", new Vector3(0f, 2.95f, 2.9f),
                new Vector3(.88f, 1.02f, .82f), plumGlowMat, worldRoot, false).transform;
            targetPlumHome = targetPlum.localPosition;
            // 수확의 주인공은 벌레의 이동이 아니라 세 법랑 변이 닫히는 삼각 수확망이다.
            harvestNetRoot = new GameObject("TriangularHarvestNet").transform;
            harvestNetRoot.SetParent(worldRoot, false);
            harvestNetRoot.localPosition = targetPlumHome + new Vector3(0f, -.1f, -.45f);
            Vector3 netA = new Vector3(-1.55f, -1.05f, 0f);
            Vector3 netB = new Vector3(1.55f, -1.05f, 0f);
            Vector3 netC = new Vector3(0f, 1.55f, 0f);
            CylinderBetween("NetLeft", netA, netC, .075f, ivoryMat, harvestNetRoot);
            CylinderBetween("NetRight", netC, netB, .075f, ivoryMat, harvestNetRoot);
            CylinderBetween("NetLatch", netB, netA, .075f, amberMat, harvestNetRoot);
            activePad = MgfLook.Block("SafetyPad", new Vector3(0f, -.34f, 1.25f), new Vector3(3.3f, .22f, 2.1f), .20f,
                ivoryMat, worldRoot).transform;
        }

        void BuildUi()
        {
            roundSprite = MakeSprite(64, 18f, false);
            thinSprite = MakeSprite(32, 8f, false);
            ringSprite = MakeSprite(64, 18f, true);

            rootRt = (RectTransform)MgfText.Canvas.transform;
            var titleRt = FullGroup("TitleLayer", rootRt, out titleG);
            var hudRt = FullGroup("HudLayer", rootRt, out hudG);
            var revealRt = FullGroup("RevealLayer", rootRt, out revealG);
            var endRt = FullGroup("EndLayer", rootRt, out endG);
            var toastLayer = FullGroup("ToastLayer", rootRt, out toastG);

            titleSign = Img(titleRt, "HangingEnamelSign", new Vector2(.70f, .79f), Vector2.zero,
                new Vector2(232f, 122f), new Color(Ivory.r, Ivory.g, Ivory.b, .94f), roundSprite);
            titleSignRt = titleSign.rectTransform;
            Img(titleSignRt, "SignTop", new Vector2(.5f, .92f), Vector2.zero, new Vector2(272f, 18f), Bark, thinSprite);
            titleShadow = Txt(titleSignRt, "자두 집게", new Vector2(.5f, .62f), new Vector2(3f, -3f), 39f,
                new Color(Plum.r, Plum.g, Plum.b, .35f), 220f);
            titleLogo = Txt(titleSignRt, "자두 집게", new Vector2(.5f, .62f), Vector2.zero, 39f, Plum, 220f);
            titleTag = Txt(titleSignRt, "두 팔로 닮음을 잠그시오", new Vector2(.5f, .26f), Vector2.zero, 14f, Ink, 218f);
            titleMeta = Txt(titleSignRt, "중2-2 · 도형의 닮음", new Vector2(.5f, .07f), Vector2.zero, 10f,
                new Color(Ink.r, Ink.g, Ink.b, .72f), 210f);
            titleBest = Txt(titleRt, "", new Vector2(.5f, .47f), Vector2.zero, 14f, Ivory, 320f);
            titlePlumRing = Img(titleRt, "TitlePlumRing", new Vector2(.5f, .5f), Vector2.zero, new Vector2(126f, 126f),
                new Color(Ivory.r, Ivory.g, Ivory.b, .9f), ringSprite);
            titlePlumRingRt = titlePlumRing.rectTransform;
            titleStart = Txt(titleRt, "빛나는 자두를 눌러 시작", new Vector2(.5f, .205f), Vector2.zero, 20f, Ivory, 330f);

            scoreTxt = Txt(hudRt, "0", new Vector2(.09f, .965f), Vector2.zero, 21f, Amber, 90f);
            progressTxt = Txt(hudRt, "수확 0/10", new Vector2(.47f, .965f), Vector2.zero, 16f, Ivory, 230f);
            timerTxt = Txt(hudRt, "120초", new Vector2(.80f, .965f), Vector2.zero, 17f, Amber, 86f);
            goalTxt = Txt(hudRt, "집게를 닮게 맞추면 자두를 딴다", new Vector2(.5f, .905f), Vector2.zero, 15f, Ivory, 360f);

            for (int i = 0; i < padIcons.Length; i++)
            {
                padIcons[i] = Img(hudRt, "Pad" + i, new Vector2(.5f, .855f), new Vector2((i - 1) * 30f, 0f),
                    new Vector2(24f, 15f), Ivory, thinSprite);
            }

            refCard = Img(hudRt, "ReferenceTag", new Vector2(.5f, .745f), Vector2.zero,
                new Vector2(350f, 150f), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), roundSprite);
            refCardRt = refCard.rectTransform;
            refTitleTxt = Txt(refCardRt, "견본 △ABC", new Vector2(.5f, .82f), Vector2.zero, 17f, Plum, 324f);
            refValuesTxt = Txt(refCardRt, "", new Vector2(.5f, .49f), Vector2.zero, 18f, Ink, 328f);
            instructionTxt = Txt(refCardRt, "", new Vector2(.5f, .16f), Vector2.zero, 14f, Bark, 328f);

            clawRootRt = R("ClawMeasureLayer", hudRt, new Vector2(.5f, .43f), Vector2.zero, new Vector2(390f, 360f));
            jointImg = Img(clawRootRt, "JointDisc", new Vector2(.5f, .38f), Vector2.zero, new Vector2(80f, 80f),
                Amber, roundSprite);
            jointRt = jointImg.rectTransform;
            railUImg = Img(jointRt, "RailU", new Vector2(.5f, .5f), Vector2.zero, new Vector2(12f * armUnit, 7f),
                new Color(Ivory.r, Ivory.g, Ivory.b, .78f), thinSprite);
            railURt = railUImg.rectTransform;
            railURt.pivot = new Vector2(0f, .5f);
            railVImg = Img(jointRt, "RailV", new Vector2(.5f, .5f), Vector2.zero, new Vector2(12f * armUnit, 7f),
                new Color(Ivory.r, Ivory.g, Ivory.b, .78f), thinSprite);
            railVRt = railVImg.rectTransform;
            railVRt.pivot = new Vector2(0f, .5f);
            armUImg = Img(jointRt, "ArmU", new Vector2(.5f, .5f), Vector2.zero, new Vector2(100f, 24f),
                Ivory, thinSprite);
            armURt = armUImg.rectTransform;
            armURt.pivot = new Vector2(0f, .5f);
            armVImg = Img(jointRt, "ArmV", new Vector2(.5f, .5f), Vector2.zero, new Vector2(100f, 24f),
                Ivory, thinSprite);
            armVRt = armVImg.rectTransform;
            armVRt.pivot = new Vector2(0f, .5f);
            handleUImg = Img(armURt, "HandleU", new Vector2(1f, .5f), Vector2.zero, new Vector2(44f, 44f),
                Amber, roundSprite);
            handleURt = handleUImg.rectTransform;
            handleVImg = Img(armVRt, "HandleV", new Vector2(1f, .5f), Vector2.zero, new Vector2(44f, 44f),
                Amber, roundSprite);
            handleVRt = handleVImg.rectTransform;

            for (int i = 0; i < angleTicks.Length; i++)
            {
                var tick = Img(jointRt, "AngleTick" + i, new Vector2(.5f, .5f), Vector2.zero, new Vector2(10f, 10f),
                    new Color(Plum.r, Plum.g, Plum.b, .48f), roundSprite);
                angleTicks[i] = tick.rectTransform;
            }
            angleKnobImg = Img(jointRt, "AngleKnob", new Vector2(.5f, .5f), Vector2.zero, new Vector2(54f, 54f),
                Leaf, roundSprite);
            angleKnobRt = angleKnobImg.rectTransform;

            for (int i = 0; i < 12; i++)
            {
                railUTicks[i] = Img(jointRt, "RailUTick" + (i + 1), new Vector2(.5f, .5f), Vector2.zero,
                    new Vector2(3f, i == 11 ? 22f : 14f), Ivory, thinSprite);
                railVTicks[i] = Img(jointRt, "RailVTick" + (i + 1), new Vector2(.5f, .5f), Vector2.zero,
                    new Vector2(3f, i == 11 ? 22f : 14f), Ivory, thinSprite);
                railUNumbers[i] = Txt(jointRt, (i + 1).ToString(), new Vector2(.5f, .5f), Vector2.zero, 9f, Ink, 26f);
                railVNumbers[i] = Txt(jointRt, (i + 1).ToString(), new Vector2(.5f, .5f), Vector2.zero, 9f, Ink, 26f);
            }
            targetUImg = Img(jointRt, "PracticeTargetU", new Vector2(.5f, .5f), Vector2.zero, new Vector2(64f, 64f),
                new Color(Amber.r, Amber.g, Amber.b, .92f), ringSprite);
            targetVImg = Img(jointRt, "PracticeTargetV", new Vector2(.5f, .5f), Vector2.zero, new Vector2(64f, 64f),
                new Color(Amber.r, Amber.g, Amber.b, .92f), ringSprite);

            calloutUBg = Img(clawRootRt, "ArmUCallout", new Vector2(.5f, .5f), new Vector2(-142f, 122f),
                new Vector2(116f, 42f), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), roundSprite);
            calloutVBg = Img(clawRootRt, "ArmVCallout", new Vector2(.5f, .5f), new Vector2(142f, 122f),
                new Vector2(116f, 42f), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), roundSprite);
            calloutABg = Img(clawRootRt, "AngleCallout", new Vector2(.5f, .5f), new Vector2(0f, -54f),
                new Vector2(124f, 40f), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), roundSprite);
            handleULabel = Txt(calloutUBg.rectTransform, "", new Vector2(.5f, .5f), Vector2.zero, 14f, Ink, 108f);
            handleVLabel = Txt(calloutVBg.rectTransform, "", new Vector2(.5f, .5f), Vector2.zero, 14f, Ink, 108f);
            angleLabel = Txt(calloutABg.rectTransform, "", new Vector2(.5f, .5f), Vector2.zero, 13f, Ink, 116f);
            leaderU = Img(clawRootRt, "ArmULeader", new Vector2(.5f, .5f), Vector2.zero, new Vector2(80f, 3f),
                new Color(Ivory.r, Ivory.g, Ivory.b, .82f), thinSprite);
            leaderV = Img(clawRootRt, "ArmVLeader", new Vector2(.5f, .5f), Vector2.zero, new Vector2(80f, 3f),
                new Color(Ivory.r, Ivory.g, Ivory.b, .82f), thinSprite);
            leaderA = Img(clawRootRt, "AngleLeader", new Vector2(.5f, .5f), Vector2.zero, new Vector2(60f, 3f),
                new Color(Ivory.r, Ivory.g, Ivory.b, .82f), thinSprite);
            leaderURt = leaderU.rectTransform;
            leaderVRt = leaderV.rectTransform;
            leaderARt = leaderA.rectTransform;
            currentTxt = Txt(clawRootRt, "", new Vector2(.5f, .07f), Vector2.zero, 15f, Ivory, 390f);
            currentTxt.textWrappingMode = TextWrappingModes.NoWrap;

            for (int i = 0; i < touchKnots.Length; i++)
                touchKnots[i] = Img(clawRootRt, "TouchKnot" + i, new Vector2(.5f, .12f), new Vector2((i - 1) * 34f, 0f),
                    new Vector2(18f, 18f), new Color(Amber.r, Amber.g, Amber.b, .25f), roundSprite);

            guideLine = Img(hudRt, "GuidePath", new Vector2(.5f, .5f), Vector2.zero, new Vector2(120f, 5f),
                new Color(Amber.r, Amber.g, Amber.b, .68f), thinSprite);
            guideLineRt = guideLine.rectTransform;
            guideHand = Img(hudRt, "GuideHand", new Vector2(.5f, .5f), Vector2.zero, new Vector2(54f, 54f),
                new Color(Ivory.r, Ivory.g, Ivory.b, .94f), ringSprite);
            guideHandRt = guideHand.rectTransform;

            revealPanel = Img(revealRt, "RevealTag", new Vector2(.5f, .30f), Vector2.zero, new Vector2(354f, 126f),
                new Color(Ivory.r, Ivory.g, Ivory.b, .97f), roundSprite);
            revealTitleTxt = Txt(revealPanel.rectTransform, "", new Vector2(.5f, .70f), Vector2.zero, 22f, Plum, 330f);
            revealFormulaTxt = Txt(revealPanel.rectTransform, "", new Vector2(.5f, .32f), Vector2.zero, 15f, Ink, 330f);

            endPanel = Img(endRt, "EndCrate", new Vector2(.5f, .52f), Vector2.zero, new Vector2(350f, 386f),
                new Color(Ivory.r, Ivory.g, Ivory.b, .98f), roundSprite);
            endTitleTxt = Txt(endPanel.rectTransform, "", new Vector2(.5f, .78f), Vector2.zero, 34f, Plum, 320f);
            endScoreTxt = Txt(endPanel.rectTransform, "0", new Vector2(.5f, .56f), Vector2.zero, 46f, Amber, 320f);
            endStatsTxt = Txt(endPanel.rectTransform, "", new Vector2(.5f, .35f), Vector2.zero, 18f, Ink, 320f);
            endTapTxt = Txt(endPanel.rectTransform, "자두를 눌러 다시 수확", new Vector2(.5f, .12f), Vector2.zero, 19f, Plum, 320f);

            toastBg = Img(toastLayer, "Toast", new Vector2(.5f, .14f), Vector2.zero, new Vector2(350f, 66f),
                new Color(Ivory.r, Ivory.g, Ivory.b, .96f), roundSprite);
            toastRt = toastBg.rectTransform;
            toastTxt = Txt(toastRt, "", new Vector2(.5f, .5f), Vector2.zero, 14f, Ink, 326f);

            rippleImg = Img(toastLayer, "PointerRipple", new Vector2(.5f, .5f), Vector2.zero, new Vector2(58f, 58f),
                new Color(Leaf.r, Leaf.g, Leaf.b, 0f), ringSprite);
            rippleRt = rippleImg.rectTransform;

            ApplyResponsiveLayout(true);
        }

        void ShowTitleVisual()
        {
            int best = PlayerPrefs.GetInt("jadu-jipge.best", 0);
            titleBest.text = best > 0 ? "최고 기록 " + best.ToString("N0") : "";
            titlePress = 0f;
            titlePlum.gameObject.SetActive(true);
            targetPlum.gameObject.SetActive(false);
        }

        void SetScreen()
        {
            bool title = gamePhase == GamePhase.Title;
            bool end = gamePhase == GamePhase.End;
            bool reveal = gamePhase == GamePhase.Reveal;
            titleG.alpha = title ? 1f : 0f;
            titleG.blocksRaycasts = title;
            hudG.alpha = title || end ? 0f : 1f;
            revealG.alpha = reveal ? 1f : 0f;
            endG.alpha = end ? 1f : 0f;
            guideLine.gameObject.SetActive(!title && !end && !reveal && guideControl > 0);
            guideHand.gameObject.SetActive(!title && !end && !reveal && guideControl > 0);
            titlePlum.gameObject.SetActive(title);
            targetPlum.gameObject.SetActive(!title && !end);
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            refTitleTxt.text = current.sourceTriangle + "∽" + current.clawTriangle + "  ·  대응 순서";
            refValuesTxt.text = "AB=" + current.referenceU + " cm   AC=" + current.referenceV + " cm   ∠A=" + current.angleDeg + "°\n"
                + "견본과 집게의 닮음비 " + current.ratioQ + ":1";
            instructionTxt.text = current.angleRequired ? "두 변과 그 끼인각을 맞추시오." : "견본과 닮도록 두 팔의 길이를 구하시오.";
            handleULabel.text = current.armULabel + "  " + st.currentU + " cm";
            handleVLabel.text = current.armVLabel + "  " + st.currentV + " cm";
            angleLabel.text = "∠" + current.jointLabel + "  " + JaduRules.AngleFromIndex(st.currentAngleIndex) + "°";
            goalTxt.text = gamePhase == GamePhase.Practice ? "빛나는 손잡이 하나씩 · 고스트 경로를 따라 끌어 놓으시오" :
                "집게를 닮게 맞추면 자두를 딴다 · 첫 시도 8/10";
            for (int i = 0; i < angleTicks.Length; i++) angleTicks[i].gameObject.SetActive(current.angleRequired);
            angleKnobRt.gameObject.SetActive(current.angleRequired);
            angleLabel.gameObject.SetActive(true);
            UpdateClawVisual(false);
            UpdateTouchKnots();
        }

        void UpdateTouchKnots()
        {
            touchKnots[0].color = touchedU ? Amber : new Color(Amber.r, Amber.g, Amber.b, .22f);
            touchKnots[1].color = touchedV ? Amber : new Color(Amber.r, Amber.g, Amber.b, .22f);
            touchKnots[2].gameObject.SetActive(current != null && current.angleRequired);
            touchKnots[2].color = touchedAngle ? Amber : new Color(Amber.r, Amber.g, Amber.b, .22f);
        }

        void UpdateClawVisual(bool fromDrag)
        {
            if (armURt == null || current == null) return;
            float theta = JaduRules.AngleFromIndex(st.currentAngleIndex);
            // 두 팔을 위쪽 좌·우로 대칭 배치해 작은 끼인각에서도 손잡이와 눈금이
            // 중앙에 포개지지 않게 한다. 두 방향의 차이는 정확히 theta다.
            float angleU = ArmCenterDeg + theta * .5f;
            float angleV = ArmCenterDeg - theta * .5f;
            armURt.sizeDelta = new Vector2(st.currentU * armUnit, 24f);
            armVRt.sizeDelta = new Vector2(st.currentV * armUnit, 24f);
            railURt.sizeDelta = new Vector2(12f * armUnit, 7f);
            railVRt.sizeDelta = new Vector2(12f * armUnit, 7f);
            armURt.localRotation = Quaternion.Euler(0f, 0f, angleU);
            armVRt.localRotation = Quaternion.Euler(0f, 0f, angleV);
            railURt.localRotation = Quaternion.Euler(0f, 0f, angleU);
            railVRt.localRotation = Quaternion.Euler(0f, 0f, angleV);

            Vector2 dirU = new Vector2(Mathf.Cos(angleU * Mathf.Deg2Rad), Mathf.Sin(angleU * Mathf.Deg2Rad));
            Vector2 dirV = new Vector2(Mathf.Cos(angleV * Mathf.Deg2Rad), Mathf.Sin(angleV * Mathf.Deg2Rad));
            Vector2 normalU = new Vector2(-dirU.y, dirU.x);
            Vector2 normalV = new Vector2(dirV.y, -dirV.x);
            for (int i = 0; i < 12; i++)
            {
                float distance = (i + 1) * armUnit;
                railUTicks[i].rectTransform.anchoredPosition = dirU * distance;
                railVTicks[i].rectTransform.anchoredPosition = dirV * distance;
                railUTicks[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, angleU);
                railVTicks[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, angleV);
                railUNumbers[i].rectTransform.anchoredPosition = dirU * distance + normalU * 18f;
                railVNumbers[i].rectTransform.anchoredPosition = dirV * distance + normalV * 18f;
            }
            targetUImg.rectTransform.anchoredPosition = dirU * (current.targetU * armUnit);
            targetVImg.rectTransform.anchoredPosition = dirV * (current.targetV * armUnit);

            for (int i = 0; i < angleTicks.Length; i++)
            {
                // 각 조절기는 팔과 분리된 하단 직선 슬롯이다.
                float x = Mathf.Lerp(-84f, 84f, i / (float)(JaduRules.AngleCount - 1));
                angleTicks[i].anchoredPosition = new Vector2(x, -72f);
                var img = angleTicks[i].GetComponent<Image>();
                img.color = i == st.currentAngleIndex ? Amber : new Color(Plum.r, Plum.g, Plum.b, .42f);
            }
            angleKnobRt.anchoredPosition = new Vector2(Mathf.Lerp(-84f, 84f,
                st.currentAngleIndex / (float)(JaduRules.AngleCount - 1)), -72f);
            angleKnobRt.localScale = fromDrag ? Vector3.one * 1.08f : Vector3.one;
            handleULabel.text = current.armULabel + "  " + st.currentU + " cm";
            handleVLabel.text = current.armVLabel + "  " + st.currentV + " cm";
            angleLabel.text = "∠" + current.jointLabel + "  " + JaduRules.AngleFromIndex(st.currentAngleIndex) + "°"
                + (current.angleRequired ? "" : " 고정");
            currentTxt.text = current.armULabel + " " + st.currentU + " cm   ·   " + current.armVLabel + " " + st.currentV + " cm"
                + "   ·   ∠" + current.jointLabel + " " + JaduRules.AngleFromIndex(st.currentAngleIndex) + "°"
                + (current.angleRequired ? "" : " 고정");
            UpdateControlVisibility();
            PositionControlLabels();
        }

        int ActivePracticeControl()
        {
            if (gamePhase != GamePhase.Practice) return 0;
            return !touchedU ? 1 : !touchedV ? 2 : current != null && current.angleRequired && !touchedAngle ? 3 : 0;
        }

        void UpdateControlVisibility()
        {
            bool practice = gamePhase == GamePhase.Practice;
            int active = ActivePracticeControl();
            bool portrait = !landscape;

            handleUImg.gameObject.SetActive(!practice || active == 1);
            handleVImg.gameObject.SetActive(!practice || active == 2);
            armUImg.color = practice && active != 1 ? new Color(Ivory.r, Ivory.g, Ivory.b, .20f) : Ivory;
            armVImg.color = practice && active != 2 ? new Color(Ivory.r, Ivory.g, Ivory.b, .20f) : Ivory;
            railUImg.color = practice && active != 1 ? new Color(Ivory.r, Ivory.g, Ivory.b, .12f) : new Color(Ivory.r, Ivory.g, Ivory.b, .78f);
            railVImg.color = practice && active != 2 ? new Color(Ivory.r, Ivory.g, Ivory.b, .12f) : new Color(Ivory.r, Ivory.g, Ivory.b, .78f);
            targetUImg.gameObject.SetActive(practice && active == 1);
            targetVImg.gameObject.SetActive(practice && active == 2);

            // 세로 화면은 현재값 요약 한 줄만 둔다. 눈금 숫자는 활성 레일 한쪽만,
            // 가로 화면도 숫자는 두 레일 중 조작 중인 쪽에만 보여 충돌을 방지한다.
            int numberedRail = dragControl == 1 || dragControl == 2 ? dragControl
                : practice ? active : 0;
            for (int i = 0; i < 12; i++)
            {
                railUNumbers[i].gameObject.SetActive(numberedRail == 1);
                railVNumbers[i].gameObject.SetActive(numberedRail == 2);
                railUTicks[i].gameObject.SetActive(!practice || active == 1);
                railVTicks[i].gameObject.SetActive(!practice || active == 2);
            }

            bool showCallouts = landscape && !practice;
            calloutUBg.gameObject.SetActive(showCallouts);
            calloutVBg.gameObject.SetActive(showCallouts);
            calloutABg.gameObject.SetActive(showCallouts);
            leaderU.gameObject.SetActive(showCallouts);
            leaderV.gameObject.SetActive(showCallouts);
            leaderA.gameObject.SetActive(showCallouts);
            currentTxt.gameObject.SetActive(!landscape || practice);
        }

        void PositionControlLabels()
        {
            if (handleURt == null) return;
            Vector2 local;
            Vector2 screenU = RectTransformUtility.WorldToScreenPoint(null, handleURt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(clawRootRt, screenU, null, out local);
            Vector2 handleULocal = local;
            Vector2 screenV = RectTransformUtility.WorldToScreenPoint(null, handleVRt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(clawRootRt, screenV, null, out local);
            Vector2 handleVLocal = local;
            calloutUBg.rectTransform.anchoredPosition = new Vector2(-clawRootRt.rect.width * .5f + 140f, 116f);
            calloutVBg.rectTransform.anchoredPosition = new Vector2(clawRootRt.rect.width * .5f - 70f, 116f);
            calloutABg.rectTransform.anchoredPosition = new Vector2(0f, -84f);
            SetUiLine(leaderURt, calloutUBg.rectTransform.anchoredPosition + new Vector2(48f, -6f), handleULocal, 3f);
            SetUiLine(leaderVRt, calloutVBg.rectTransform.anchoredPosition + new Vector2(-48f, -6f), handleVLocal, 3f);
            SetUiLine(leaderARt, calloutABg.rectTransform.anchoredPosition + new Vector2(0f, 18f), angleKnobRt.anchoredPosition, 3f);
        }

        int PickControl(Vector2 screen, bool angleRequired)
        {
            int best = 0;
            float bestDistance = float.MaxValue;
            RectTransform[] candidates = { handleURt, handleVRt, angleKnobRt };
            int count = angleRequired ? 3 : 2;
            for (int i = 0; i < count; i++)
            {
                Vector2 center = RectTransformUtility.WorldToScreenPoint(null, candidates[i].position);
                float radius = i < 2 ? 62f : 50f;
                if (!RectTransformUtility.RectangleContainsScreenPoint(candidates[i], screen, null)
                    && Vector2.Distance(center, screen) > radius) continue;
                float d = Vector2.SqrMagnitude(center - screen);
                if (d < bestDistance) { bestDistance = d; best = i + 1; }
            }
            return best;
        }

        int DragControlValue(int control, Vector2 screen)
        {
            Vector2 jointScreen = RectTransformUtility.WorldToScreenPoint(null, jointRt.position);
            if (control == 3)
            {
                float slotPixels = Mathf.Max(1f, 168f * rootRt.lossyScale.x);
                float normalized = Mathf.Clamp01((screen.x - jointScreen.x + slotPixels * .5f) / slotPixels);
                int index = Mathf.RoundToInt(normalized * (JaduRules.AngleCount - 1));
                return Mathf.Clamp(index, 0, JaduRules.AngleCount - 1);
            }

            RectTransform arm = control == 1 ? armURt : armVRt;
            Vector2 unitPoint = RectTransformUtility.WorldToScreenPoint(null, arm.TransformPoint(new Vector3(armUnit, 0f, 0f)));
            Vector2 axis = unitPoint - jointScreen;
            float unitPixels = Mathf.Max(1f, axis.magnitude);
            axis /= unitPixels;
            float projected = Vector2.Dot(screen - jointScreen, axis);
            return Mathf.Clamp(Mathf.RoundToInt(projected / unitPixels), 1, 12);
        }

        void BeginControlDrag(int control)
        {
            guideControl = 0;
            guideLine.gameObject.SetActive(false);
            guideHand.gameObject.SetActive(false);
            RectTransform handle = control == 1 ? handleURt : control == 2 ? handleVRt : angleKnobRt;
            handle.localScale = Vector3.one * .88f;
            UpdateControlVisibility();
        }

        void UpdateDragTrail(int control, Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, dragStart, null, out Vector2 start);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, screen, null, out Vector2 end);
            SetUiLine(guideLineRt, start, end, 4f);
            guideLine.color = new Color(Amber.r, Amber.g, Amber.b, .50f);
            guideLine.gameObject.SetActive(true);
            guideHandRt.anchoredPosition = end;
            guideHand.color = new Color(Ivory.r, Ivory.g, Ivory.b, .86f);
            guideHand.gameObject.SetActive(true);
        }

        void EndDragTrail()
        {
            if (handleURt) handleURt.localScale = Vector3.one;
            if (handleVRt) handleVRt.localScale = Vector3.one;
            if (angleKnobRt) angleKnobRt.localScale = Vector3.one;
            guideLine.gameObject.SetActive(false);
            guideHand.gameObject.SetActive(false);
        }

        void MarkAdjusted(int control)
        {
            UpdateTouchKnots();
            Image knot = touchKnots[Mathf.Clamp(control - 1, 0, 2)];
            knot.rectTransform.localScale = Vector3.one * 1.5f;
            MgfFx.Punch(knot.transform, .20f, .22f);
        }

        void BounceControl(int control)
        {
            RectTransform handle = control == 1 ? handleURt : control == 2 ? handleVRt : angleKnobRt;
            if (handle) MgfFx.Punch(handle, .15f, .22f);
        }

        void ReplayGuide(bool boosted)
        {
            if (current == null || gamePhase == GamePhase.Reveal || gamePhase == GamePhase.End) return;
            int control = !touchedU ? 1 : !touchedV ? 2 : current.angleRequired && !touchedAngle ? 3 : 0;
            guideControl = control;
            if (control == 0)
            {
                guideLine.gameObject.SetActive(false);
                guideHand.gameObject.SetActive(false);
                return;
            }

            RectTransform handle = control == 1 ? handleURt : control == 2 ? handleVRt : angleKnobRt;
            Vector2 handleScreen = RectTransformUtility.WorldToScreenPoint(null, handle.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, handleScreen, null, out guideStart);
            guideEnd = guideStart;
            guideHasPath = gamePhase == GamePhase.Practice;

            if (guideHasPath)
            {
                if (control == 1 || control == 2)
                {
                    RectTransform arm = control == 1 ? armURt : armVRt;
                    int target = control == 1 ? current.targetU : current.targetV;
                    Vector2 jointScreen = RectTransformUtility.WorldToScreenPoint(null, jointRt.position);
                    Vector2 unitScreen = RectTransformUtility.WorldToScreenPoint(null, arm.TransformPoint(new Vector3(armUnit, 0f, 0f)));
                    Vector2 endScreen = jointScreen + (unitScreen - jointScreen) * target;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, endScreen, null, out guideEnd);
                }
                else
                {
                    Vector2 jointScreen = RectTransformUtility.WorldToScreenPoint(null, jointRt.position);
                    float targetX = Mathf.Lerp(-84f, 84f, current.angleIndex / (float)(JaduRules.AngleCount - 1));
                    Vector2 targetScreen = jointScreen + new Vector2(targetX * rootRt.lossyScale.x, -72f * rootRt.lossyScale.y);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, targetScreen, null, out guideEnd);
                }
            }
            else
            {
                // 본판에서는 정답 눈금을 누설하지 않고 손잡이를 짧게 바깥으로 흔들기만 한다.
                Vector2 dir = (guideStart - new Vector2(0f, -70f)).normalized;
                guideEnd = guideStart + dir * 30f;
            }

            guideClock = boosted ? .55f : 0f;
            SetUiLine(guideLineRt, guideStart, guideEnd, boosted ? 7f : 5f);
            guideLine.color = new Color(Amber.r, Amber.g, Amber.b, boosted ? .88f : .64f);
            guideHand.color = new Color(Ivory.r, Ivory.g, Ivory.b, .96f);
            guideLine.gameObject.SetActive(true);
            guideHand.gameObject.SetActive(true);
            if (boosted) MgfFx.Punch(handle, .22f, .30f);
        }

        void SetUiLine(RectTransform rt, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 d = b - a;
            rt.anchoredPosition = (a + b) * .5f;
            rt.sizeDelta = new Vector2(Mathf.Max(8f, d.magnitude), thickness);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        void SpawnPointerRipple(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, screen, null, out Vector2 local);
            rippleRt.anchoredPosition = local;
            rippleRt.sizeDelta = new Vector2(34f, 34f);
            rippleImg.color = new Color(Leaf.r, Leaf.g, Leaf.b, .92f);
            rippleClock = .36f;
        }

        void RefuseInput(Vector2 screen, string message)
        {
            toastTxt.text = message;
            toastClock = 1.25f;
            toastG.alpha = 1f;
            MgfFx.Punch(toastRt, .10f, .20f);
            MgfSfx.Play("wrong", .16f);
        }

        void PressTitlePlum()
        {
            titlePress = 1f;
            titlePlum.localScale = new Vector3(.98f, .88f, .98f);
            branchRoot.localRotation = Quaternion.Euler(0f, 0f, -2.5f);
            titlePlumRingRt.localScale = Vector3.one * .90f;
            MgfSfx.Play("pop", .34f);
        }

        void ReleaseTitlePlum()
        {
            titlePress = 0f;
            MgfFx.Punch(titlePlum, .18f, .26f);
            MgfFx.Punch(titlePlumRingRt, .15f, .22f);
        }

        void ResetWorldForRun()
        {
            worldReveal = false;
            worldCorrect = false;
            revealVisualClock = 0f;
            beetleRoot.localPosition = beetleHome;
            beetleRoot.localRotation = Quaternion.identity;
            beetleRoot.localScale = Vector3.one;
            targetPlum.localPosition = targetPlumHome;
            targetPlum.localScale = Vector3.one;
            cartRoot.localScale = Vector3.one;
            cartRoot.localPosition = cartHome;
            harvestNetRoot.localScale = Vector3.one;
            harvestNetRoot.localRotation = Quaternion.identity;
            branchRoot.localRotation = Quaternion.identity;
            activePad.localPosition = new Vector3(0f, -.34f, 1.25f);
            for (int i = 0; i < harvestPlums.Length; i++) harvestPlums[i].gameObject.SetActive(false);
            for (int i = 0; i < bruisedPlums.Length; i++) bruisedPlums[i].gameObject.SetActive(false);
        }

        void ResetWorldForProblem(JaduProblem p)
        {
            worldReveal = false;
            worldCorrect = false;
            revealVisualClock = 0f;
            beetleRoot.localPosition = beetleHome;
            beetleRoot.localRotation = Quaternion.identity;
            beetleRoot.localScale = Vector3.one;
            targetPlum.localPosition = targetPlumHome + new Vector3((st.solved % 3 - 1) * .52f, 0f, 0f);
            targetPlum.localScale = Vector3.one;
            targetPlum.gameObject.SetActive(true);
            cartRoot.localScale = Vector3.one;
            cartRoot.localPosition = cartHome;
            harvestNetRoot.localScale = Vector3.one;
            harvestNetRoot.localRotation = Quaternion.identity;
            activePad.localPosition = new Vector3(0f, -.34f, 1.25f);
            for (int i = 0; i < harvestPlums.Length; i++) harvestPlums[i].gameObject.SetActive(i < st.solved);
            for (int i = 0; i < bruisedPlums.Length; i++) bruisedPlums[i].gameObject.SetActive(i < st.firstAttemptWrong);
        }

        void BeginRevealVisual(bool correct, bool wasFirst, bool practice, string evidence)
        {
            worldReveal = true;
            worldCorrect = correct;
            revealVisualClock = 0f;
            guideControl = 0;
            guideLine.gameObject.SetActive(false);
            guideHand.gameObject.SetActive(false);
            revealTitleTxt.text = correct ? (practice ? "닮은 집게 완성" : "자두 수확")
                : practice ? "연습 확인 · 이 관계를 기억하시오" : "관계를 다시 확인하시오";
            revealFormulaTxt.text = evidence;
            revealPanel.color = correct ? new Color(SoftGreen.r, SoftGreen.g, SoftGreen.b, .98f)
                : new Color(Ivory.r, Ivory.g * .92f, Ivory.b * .82f, .98f);
            if (correct)
            {
                MgfSfx.Play("correct", .54f);
                cartBounce = 1f;
            }
            else
            {
                MgfSfx.Play("wrong", .42f);
                MgfFx.Shake(cam, .055f + st.attemptIndex * .025f, .22f);
                wrongWobble = 1f;
                if (st.firstAttemptWrong > 0 && st.firstAttemptWrong <= bruisedPlums.Length)
                    bruisedPlums[st.firstAttemptWrong - 1].gameObject.SetActive(true);
            }
        }

        void CompleteRevealVisual(bool correct)
        {
            worldReveal = false;
            if (correct)
            {
                int shown = Mathf.Clamp(st.solved - 1, 0, harvestPlums.Length - 1);
                if (!revealWasPractice && st.solved > 0) harvestPlums[shown].gameObject.SetActive(true);
            }
            targetPlum.localPosition = targetPlumHome;
            targetPlum.localScale = Vector3.one;
            beetleRoot.localPosition = beetleHome;
            beetleRoot.localRotation = Quaternion.identity;
            beetleRoot.localScale = Vector3.one;
            cartRoot.localPosition = cartHome;
            harvestNetRoot.localScale = Vector3.one;
            harvestNetRoot.localRotation = Quaternion.identity;
            branchRoot.localRotation = Quaternion.identity;
        }

        void AnimateWorld(float dt)
        {
            worldClock += dt;
            for (int i = 0; i < canopy.Length; i++)
            {
                float bob = Mathf.Sin(worldClock * (.46f + (i % 4) * .05f) + i * .71f) * .025f;
                canopy[i].localScale = new Vector3(1f + bob, 1f - bob * .5f, 1f + bob * .4f);
            }
            for (int i = 0; i < idlePlums.Length; i++)
                idlePlums[i].localRotation = Quaternion.Euler(0f, Mathf.Sin(worldClock * .55f + i) * 5f, Mathf.Sin(worldClock * .72f + i) * 2f);
            for (int i = 0; i < chuteLeaves.Length; i++)
                chuteLeaves[i].localRotation *= Quaternion.Euler(0f, 0f, Mathf.Sin(worldClock * 1.2f + i) * .018f);

            float antenna = Mathf.Sin(worldClock * 2.1f) * 7f;
            antennaLeft.localRotation = Quaternion.Euler(0f, 0f, antenna);
            antennaRight.localRotation = Quaternion.Euler(0f, 0f, -antenna);
            beetleBody.localScale = new Vector3(1f + Mathf.Sin(worldClock * 2.2f) * .025f, 1f - Mathf.Sin(worldClock * 2.2f) * .035f, 1f);

            if (gamePhase == GamePhase.Title)
            {
                float pulse = 1f + Mathf.Sin(worldClock * 2.0f) * .045f;
                if (titlePress <= 0f) titlePlum.localScale = new Vector3(pulse, pulse * 1.04f, pulse);
                titlePlum.localRotation = Quaternion.Euler(0f, Mathf.Sin(worldClock * .65f) * 8f, Mathf.Sin(worldClock * 1.1f) * 3f);
                titlePlumRingRt.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 2.0f) * .06f);
                Vector2 plumScreen = cam.WorldToScreenPoint(titlePlum.position);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, plumScreen, null, out Vector2 plumLocal))
                    titlePlumRingRt.anchoredPosition = plumLocal;
            }

            if (worldReveal)
            {
                revealVisualClock += dt;
                if (worldCorrect)
                {
                    float t = Mathf.Clamp01(revealVisualClock / 1.25f);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    Vector3 a = targetPlumHome;
                    Vector3 b = new Vector3(0f, 5.0f, 1.0f);
                    Vector3 c = new Vector3(5.75f, .65f, 3.0f);
                    float close = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / .42f));
                    harvestNetRoot.localScale = Vector3.Lerp(Vector3.one, new Vector3(.50f, .55f, .50f), close);
                    harvestNetRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(close * Mathf.PI) * -6f);
                    targetPlum.localPosition = Vector3.Lerp(Vector3.Lerp(a, b, eased), Vector3.Lerp(b, c, eased), eased);
                    float squash = Mathf.Sin(t * Mathf.PI) * .18f;
                    targetPlum.localScale = new Vector3(1f + squash, 1f - squash * .65f, 1f + squash);
                    // 벌레는 보조 반응만 하고, 삼각망과 카운터웨이트가 정답 사건을 이끈다.
                    beetleRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI) * -7f);
                    branchRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI) * 5f);
                    if (t > .72f)
                    {
                        float land = Mathf.Sin((t - .72f) / .28f * Mathf.PI);
                        cartRoot.localScale = new Vector3(1f + land * .06f, 1f - land * .14f, 1f + land * .06f);
                        cartRoot.localPosition = cartHome + Vector3.down * Mathf.SmoothStep(0f, .72f, (t - .72f) / .28f);
                        int leafIndex = Mathf.Clamp(Mathf.FloorToInt((t - .72f) / .056f), 0, 4);
                        chuteLeaves[leafIndex].localScale = Vector3.one * (1f + land * .09f);
                    }
                }
                else
                {
                    float t = Mathf.Clamp01(revealVisualClock / .86f);
                    targetPlum.localPosition = targetPlumHome + new Vector3(Mathf.Sin(t * 9f) * .28f, -t * 2.5f, 0f);
                    beetleRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 18f) * 5f * (1f - t));
                    antennaLeft.localRotation = Quaternion.Euler(0f, 0f, 32f * t);
                    antennaRight.localRotation = Quaternion.Euler(0f, 0f, -32f * t);
                    activePad.localPosition = new Vector3(0f, -.34f - t * 1.4f, 1.25f + t * .55f);
                }
            }

            if (cartBounce > 0f) cartBounce = Mathf.Max(0f, cartBounce - dt * 2.8f);
            if (wrongWobble > 0f) wrongWobble = Mathf.Max(0f, wrongWobble - dt * 3.2f);
        }

        void UpdateUi(float dt)
        {
            displayedScore = Mathf.MoveTowards(displayedScore, st.score, Mathf.Max(80f, Mathf.Abs(st.score - displayedScore) * 7f) * dt);
            int scoreNow = Mathf.RoundToInt(displayedScore);
            if (scoreNow != shownScore)
            {
                scoreTxt.text = scoreNow.ToString("N0");
                if (shownScore >= 0 && scoreNow > shownScore) scoreTxt.rectTransform.localScale = Vector3.one * 1.08f;
                shownScore = scoreNow;
            }
            scoreTxt.rectTransform.localScale = Vector3.Lerp(scoreTxt.rectTransform.localScale, Vector3.one, dt * 9f);
            progressTxt.text = "수확 " + st.solved + "/10  ·  첫 시도 " + st.firstAttemptCorrect + "/" + Mathf.Max(1, st.firstAttemptTotal);
            int seconds = Mathf.CeilToInt(timeLeft);
            if (seconds != shownSecond)
            {
                timerTxt.text = seconds + "초";
                shownSecond = seconds;
            }
            for (int i = 0; i < padIcons.Length; i++)
            {
                bool alive = i < st.lives;
                padIcons[i].color = alive ? Ivory : new Color(Bark.r, Bark.g, Bark.b, .22f);
                padIcons[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, alive ? 0f : -18f);
            }

            if (toastClock > 0f)
            {
                toastClock -= dt;
                toastG.alpha = Mathf.Clamp01(toastClock * 4f);
            }
            else toastG.alpha = 0f;

            if (rippleClock > 0f)
            {
                rippleClock -= dt;
                float p = 1f - rippleClock / .36f;
                rippleRt.sizeDelta = Vector2.one * Mathf.Lerp(34f, 96f, p);
                rippleImg.color = new Color(Leaf.r, Leaf.g, Leaf.b, (1f - p) * .88f);
            }
            else rippleImg.color = new Color(Leaf.r, Leaf.g, Leaf.b, 0f);

            if (guideControl > 0 && gamePhase != GamePhase.Reveal)
            {
                guideClock += dt;
                float p = (guideClock % 1.2f) / 1.2f;
                float eased = p * p * (3f - 2f * p);
                guideHandRt.anchoredPosition = Vector2.Lerp(guideStart, guideEnd, eased);
                guideHandRt.localScale = Vector3.one * (1f + Mathf.Sin(p * Mathf.PI) * .18f);
                guideLine.color = new Color(Amber.r, Amber.g, Amber.b, .44f + Mathf.Sin(p * Mathf.PI) * .38f);
            }

            if (gamePhase == GamePhase.End)
            {
                endDisplayedScore = Mathf.MoveTowards(endDisplayedScore, st.score, Mathf.Max(100f, st.score * 1.6f) * dt);
                endScoreTxt.text = Mathf.RoundToInt(endDisplayedScore).ToString("N0");
            }
        }

        void ShowEnd(string reason)
        {
            bool clear = reason == "clear";
            endTitleTxt.text = clear ? "수확 숙련 통과" : reason == "time" ? "수확 시간이 끝났다" :
                reason == "band3_lost" ? "사이각 기록을 다시 세우시오" :
                reason == "mastery_lost" || reason == "mastery_fail" ? "첫 시도 기록을 다시 세우시오" : "안전 패드가 모두 떨어졌다";
            endStatsTxt.text = "자두 " + st.solved + "/10\n첫 시도 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal
                + " · 후반 " + st.band3FirstCorrect + "/4\n남은 패드 " + st.lives + "/3";
            endPanel.color = clear ? new Color(SoftGreen.r, SoftGreen.g, SoftGreen.b, .98f)
                : new Color(Ivory.r, Ivory.g * .94f, Ivory.b * .86f, .98f);
            endDisplayedScore = 0f;
            endScoreTxt.text = "0";
        }

        void ApplyResponsiveLayout(bool force = false)
        {
            if (!force && layoutW == Screen.width && layoutH == Screen.height) return;
            layoutW = Screen.width;
            layoutH = Screen.height;
            landscape = Screen.width >= Screen.height * 1.12f;
            armUnit = landscape ? 18.5f : 16.2f;

            if (landscape)
            {
                refCardRt.anchorMin = refCardRt.anchorMax = new Vector2(.23f, .61f);
                refCardRt.sizeDelta = new Vector2(324f, 174f);
                clawRootRt.anchorMin = clawRootRt.anchorMax = new Vector2(.66f, .34f);
                clawRootRt.sizeDelta = new Vector2(470f, 390f);
                titleSignRt.anchorMin = titleSignRt.anchorMax = new Vector2(.72f, .76f);
                // ScreenPointToLocalPointInRectangle returns coordinates relative to the
                // canvas centre, so the tracked ring must use a centred anchor too.
                titlePlumRingRt.anchorMin = titlePlumRingRt.anchorMax = new Vector2(.5f, .5f);
                revealPanel.rectTransform.anchorMin = revealPanel.rectTransform.anchorMax = new Vector2(.5f, .22f);
            }
            else
            {
                refCardRt.anchorMin = refCardRt.anchorMax = new Vector2(.5f, .72f);
                refCardRt.sizeDelta = new Vector2(354f, 154f);
                clawRootRt.anchorMin = clawRootRt.anchorMax = new Vector2(.5f, .37f);
                clawRootRt.sizeDelta = new Vector2(390f, 350f);
                titleSignRt.anchorMin = titleSignRt.anchorMax = new Vector2(.66f, .80f);
                titlePlumRingRt.anchorMin = titlePlumRingRt.anchorMax = new Vector2(.5f, .5f);
                revealPanel.rectTransform.anchorMin = revealPanel.rectTransform.anchorMax = new Vector2(.5f, .22f);
            }
            if (cam) cam.orthographicSize = landscape ? 7.6f : 9.1f;
            if (current != null) UpdateClawVisual(false);
        }

        void ApplyResponsiveLayout()
        {
            ApplyResponsiveLayout(false);
        }

        void FillControlPixels(float[] output)
        {
            if (output == null || output.Length < 8 || handleURt == null) return;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(null, handleURt.position);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(null, handleVRt.position);
            Vector2 c = RectTransformUtility.WorldToScreenPoint(null, angleKnobRt.position);
            Vector2 j = RectTransformUtility.WorldToScreenPoint(null, jointRt.position);
            output[0] = a.x; output[1] = Screen.height - a.y;
            output[2] = b.x; output[3] = Screen.height - b.y;
            output[4] = c.x; output[5] = Screen.height - c.y;
            output[6] = j.x; output[7] = Screen.height - j.y;
        }
    }
}
