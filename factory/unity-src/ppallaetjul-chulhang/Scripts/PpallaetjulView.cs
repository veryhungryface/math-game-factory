using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.PpallaetjulChulhang
{
    public partial class PpallaetjulGame
    {
        static readonly Color Brick = MgfLook.Hex("#F5B6A7");
        static readonly Color Cloth = MgfLook.Hex("#FFF5DF");
        static readonly Color Petrol = MgfLook.Hex("#3D6571");
        static readonly Color Coral = MgfLook.Hex("#D95940");
        static readonly Color Brass = MgfLook.Hex("#DCA748");
        static readonly Color Ink = MgfLook.Hex("#344B50");
        static readonly Color Mint = MgfLook.Hex("#91B9AC");

        Camera cam;
        Transform worldRoot, alleyRoot, sailRoot, snailRoot, gondolaRoot, scissorsRoot;
        Transform[] laundry = new Transform[14];
        Vector3[] laundryBase = new Vector3[14];
        Transform[] eyeStalks = new Transform[2];
        Transform[] leftTicks = new Transform[7];
        Transform[] rightTicks = new Transform[7];
        Renderer[] leftTickRenderers = new Renderer[7];
        Renderer[] rightTickRenderers = new Renderer[7];
        Renderer[] successWindowLights = new Renderer[8];
        Transform[] successAwnings = new Transform[8];
        Vector3[] successAwningScales = new Vector3[8];
        GameObject[] patchMarkers = new GameObject[3];
        Mesh sailMesh, smallSailMesh, remnantMesh;
        MeshRenderer sailRenderer, smallSailRenderer, remnantRenderer;
        LineRenderer cutLine, ghostLine, threadLine;
        Material brickMat, brickDarkMat, clothMat, clothGlowMat, projectionMat, projectionGlowMat, windowLightMat;
        Material petrolMat, coralMat, brassMat, inkMat, glassMat, lineMat, ghostMat, threadMat;
        Vector3 sailA, sailB, sailC;
        int startSide = -1, startTick = -1, successWindowCount;
        int layoutW, layoutH;
        float worldTime, sailShakeT, snailReactT, gondolaTripT, clothFlashT, rippleT, toastT, transitionT;
        bool reactCorrect;
        Vector3 gondolaHome;
        Vector3 snailBaseScale = Vector3.one;

        RectTransform uiRoot, titleRt, hudRt, endRt, titleCtaRt, restartRt, promptRt, goalRt, rippleRt, toastRt, revealRt;
        RectTransform guideHandRt, guidePathRt, guideTargetRt, titleGuideRingRt, titleGuideArrowRt;
        RectTransform[] guideChevrons = new RectTransform[3];
        RectTransform[] patchUi = new RectTransform[3];
        TextMeshProUGUI titleLogo, titleTag, titleMeta, promptText, goalText, statusText, scoreText, timerText, ratioText;
        TextMeshProUGUI leftDimText, rightDimText, pointText, toastText, revealText, endTitle, endScore, endStats;
        TextMeshProUGUI[] leftTickLabels = new TextMeshProUGUI[7];
        TextMeshProUGUI[] rightTickLabels = new TextMeshProUGUI[7];
        CanvasGroup titleG, hudG, endG, toastG, revealG;
        Image rippleImage, revealImage, guideHandImage, guidePathImage, guideTargetImage;
        Sprite roundedSprite, ringSprite, clothSprite, chevronSprite;
        float shownScore, shownEndScore;
        int shownTimer = -1;

        void BuildWorld()
        {
            // CreatePrimitive(Cylinder/Sphere)가 내부에서 문자열로 붙이는 콜라이더를 IL2CPP 스트리핑에서 보존한다.
            var colliderTypes = new GameObject("ColliderTypeReferences");
            colliderTypes.AddComponent<CapsuleCollider>(); colliderTypes.AddComponent<SphereCollider>();
            colliderTypes.AddComponent<BoxCollider>(); colliderTypes.AddComponent<MeshCollider>();
            Object.Destroy(colliderTypes);

            MgfLook.Sky(MgfLook.Hex("#A9D3D6"), MgfLook.Hex("#FFE8CE"), Brick, .92f);
            MgfLook.Sun(new Vector3(38, -28, -18), MgfLook.Hex("#FFF1D6"), 1.18f, .58f);
            cam = MgfLook.Camera(new Vector3(0, 4.8f, -22f), new Vector3(0, 1.6f, 0), 34f);
            cam.orthographic = true; cam.orthographicSize = 8.6f;
            cam.nearClipPlane = .1f; cam.farClipPlane = 100f;

            worldRoot = new GameObject("LaundryAlleyWorld").transform;
            alleyRoot = new GameObject("RainWashedFacades").transform; alleyRoot.SetParent(worldRoot, false);
            sailRoot = new GameObject("FrontMeasurementSail").transform; sailRoot.SetParent(worldRoot, false);

            brickMat = MgfLook.Lit(Brick, .24f);
            brickDarkMat = MgfLook.Lit(MgfLook.Hex("#C87C6D"), .18f);
            Texture2D weave = MakeWeaveTexture();
            Texture2D rainGlass = MakeRainGlassTexture();
            clothMat = MgfLook.Lit(Cloth, .16f, 0f, null, weave);
            clothGlowMat = MgfLook.Lit(MgfLook.Hex("#FFFBEF"), .35f, 0f, MgfLook.Hex("#3B2812"), weave);
            projectionMat = MgfLook.Lit(MgfLook.Hex("#8FD6D2"), .86f, .08f, MgfLook.Hex("#10393E"), rainGlass);
            projectionGlowMat = MgfLook.Lit(MgfLook.Hex("#C9FFF2"), .92f, .12f, MgfLook.Hex("#2F8B78"), rainGlass);
            windowLightMat = MgfLook.Lit(MgfLook.Hex("#FFE49C"), .72f, .08f, MgfLook.Hex("#A75516"));
            petrolMat = MgfLook.Lit(Petrol, .28f, .08f);
            coralMat = MgfLook.Lit(Coral, .18f);
            brassMat = MgfLook.Lit(Brass, .62f, .55f);
            inkMat = MgfLook.Lit(Ink, .26f);
            glassMat = MgfLook.Lit(MgfLook.Hex("#BDE4DE"), .78f, .18f, MgfLook.Hex("#173438"));
            lineMat = MgfLook.Unlit(Brass);
            ghostMat = MgfLook.Alpha(new Color(Coral.r, Coral.g, Coral.b, .72f));
            threadMat = MgfLook.Unlit(Cloth);

            // 화면을 양쪽에서 감싸는 수직 벽돌 파사드. 빈 거터가 아니라 창·난간·빨래가 깊이를 채운다.
            MgfLook.Block("RearBrickGlow", new Vector3(0, 3.5f, 7.0f), new Vector3(27f, 19f, 1f), .18f, brickMat, alleyRoot);
            BuildFacade(-1);
            BuildFacade(1);
            // 세로폰에서도 양쪽 벽돌 면이 화면 가장자리를 잡도록 가까운 전경 벽을 한 겹 더 둔다.
            MgfLook.Block("NearLeftBrick", new Vector3(-4.45f, 1.8f, 3.9f), new Vector3(2.2f, 15f, 2.2f), .20f, brickDarkMat, alleyRoot);
            MgfLook.Block("NearRightBrick", new Vector3(4.45f, 1.8f, 3.9f), new Vector3(2.2f, 15f, 2.2f), .20f, brickDarkMat, alleyRoot);
            BuildClotheslines();
            BuildSnail();
            BuildGondola();
            BuildScissors();
            BuildSail();
        }

        Texture2D MakeWeaveTexture()
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "WovenIvory", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                byte tone = (byte)(238 + ((x + y) % 4) * 3);
                if (x % 8 == 0 || y % 8 == 0) tone = 224;
                if ((x + y * 3) % 29 == 0) tone = 214;
                px[y * s + x] = new Color32(tone, (byte)Mathf.Min(255, tone + 5), (byte)Mathf.Min(255, tone + 2), 255);
            }
            tex.SetPixels32(px); tex.Apply(false, true); return tex;
        }

        Texture2D MakeRainGlassTexture()
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "RainLightProjection", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int streak = (x * 13 + y * 5) % 37;
                int drop = ((x - 11) * (x - 11) + (y - 17) * (y - 17)) % 53;
                byte r = (byte)(112 + (y * 28 / s) + (streak < 2 ? 44 : 0));
                byte g = (byte)(190 + (streak < 2 ? 38 : 0));
                byte b = (byte)(194 + (drop < 4 ? 45 : 0));
                px[y * s + x] = new Color32(r, g, b, 255);
            }
            tex.SetPixels32(px); tex.Apply(false, true); return tex;
        }

        void BuildFacade(int side)
        {
            float x = side * 7.7f;
            MgfLook.Block(side < 0 ? "LeftFacade" : "RightFacade", new Vector3(x, 3.0f, 2.8f), new Vector3(5.0f, 18f, 3.2f), .24f, brickDarkMat, alleyRoot);
            for (int floor = 0; floor < 4; floor++)
            {
                float y = -2.9f + floor * 4.0f;
                var win = MgfLook.Block("WetWindow", new Vector3(x - side * 2.56f, y + .7f, 1.05f), new Vector3(.20f, 2.3f, 2.4f), .12f, glassMat, alleyRoot);
                win.transform.rotation = Quaternion.Euler(0, side * 5f, 0);
                var glow = MgfLook.Block("RatioWindowLight", new Vector3(x - side * 2.70f, y + .7f, .88f), new Vector3(.10f, 1.82f, 1.88f), .08f, windowLightMat, alleyRoot);
                glow.transform.rotation = win.transform.rotation;
                var glowRenderer = glow.GetComponent<Renderer>(); glowRenderer.enabled = false;
                var awning = MgfLook.Block("RatioAwning", new Vector3(x - side * 2.90f, y + 2.02f, .63f), new Vector3(.34f, .18f, 2.70f), .07f,
                    floor % 2 == 0 ? petrolMat : coralMat, alleyRoot);
                awning.transform.rotation = Quaternion.Euler(0, side * 5f, floor % 2 == 0 ? 3f : -3f);
                if (successWindowCount < successWindowLights.Length)
                {
                    successWindowLights[successWindowCount] = glowRenderer;
                    successAwnings[successWindowCount] = awning.transform;
                    successAwningScales[successWindowCount] = awning.transform.localScale;
                    awning.transform.localScale = new Vector3(awning.transform.localScale.x, awning.transform.localScale.y, awning.transform.localScale.z * .08f);
                    successWindowCount++;
                }
                MgfLook.Block("WindowFrameTop", new Vector3(x - side * 2.72f, y + 1.88f, .85f), new Vector3(.24f, .18f, 2.75f), .06f, inkMat, alleyRoot);
                MgfLook.Block("Balcony", new Vector3(x - side * 2.40f, y - .78f, .1f), new Vector3(1.55f, .22f, 3.35f), .08f, inkMat, alleyRoot);
                for (int r = -1; r <= 1; r++)
                    MgfLook.Block("Rail", new Vector3(x - side * 3.05f, y + .02f, r * 1.2f), new Vector3(.10f, 1.55f, .10f), .035f, inkMat, alleyRoot);
                MgfLook.Block("FlowerBox", new Vector3(x - side * 3.0f, y - .25f, 1.35f), new Vector3(.55f, .48f, 1.45f), .16f, floor % 2 == 0 ? petrolMat : coralMat, alleyRoot);
            }
            MgfLook.Prim(PrimitiveType.Cylinder, "RainPipe", new Vector3(x - side * 3.0f, 3.0f, 3.45f), new Vector3(.28f, 9.2f, .28f), inkMat, alleyRoot, false);
            // 벽돌 이음선은 적은 오브젝트로 큰 리듬만 만든다.
            for (int row = 0; row < 8; row++)
                MgfLook.Block("BrickCourse", new Vector3(x - side * 2.55f, -5.5f + row * 2.2f, 3.5f), new Vector3(.12f, .055f, 3.0f), .02f, inkMat, alleyRoot);
        }

        void BuildClotheslines()
        {
            int li = 0;
            for (int row = 0; row < 3; row++)
            {
                float y = 5.8f - row * 3.6f;
                var lr = NewLine("Clothesline", inkMat, .055f);
                lr.positionCount = 2; lr.SetPosition(0, new Vector3(-7.8f, y, 1.6f + row)); lr.SetPosition(1, new Vector3(7.8f, y - .25f, 1.1f + row));
                for (int j = 0; j < 4 + row && li < laundry.Length; j++, li++)
                {
                    float x = -4.8f + j * (9.6f / (3 + row)) + (row % 2) * .4f;
                    var cloth = MgfLook.Block("Laundry" + li, new Vector3(x, y - .72f, 1.7f + row), new Vector3(.75f + (j % 2) * .25f, 1.25f, .09f), .08f,
                        (j + row) % 3 == 0 ? clothMat : (j + row) % 3 == 1 ? petrolMat : coralMat, alleyRoot);
                    Object.Destroy(cloth.GetComponent<Collider>());
                    laundry[li] = cloth.transform; laundryBase[li] = cloth.transform.localPosition;
                    MgfLook.Prim(PrimitiveType.Cylinder, "EnamelPin", cloth.transform.position + new Vector3(0, .68f, -.05f), new Vector3(.08f, .13f, .08f), brassMat, alleyRoot, false)
                        .transform.rotation = Quaternion.Euler(90, 0, 0);
                }
            }
        }

        void BuildSnail()
        {
            snailRoot = new GameObject("TailorSnail").transform; snailRoot.SetParent(worldRoot, false);
            snailRoot.localPosition = new Vector3(3.2f, -4.9f, -1.4f);
            var body = MgfLook.Prim(PrimitiveType.Sphere, "IvoryBody", Vector3.zero, new Vector3(1.28f, .62f, .75f), clothMat, snailRoot, false);
            body.transform.localRotation = Quaternion.Euler(0, 0, -7f);
            MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(.80f, .44f, -.08f), new Vector3(.62f, .62f, .60f), clothMat, snailRoot, false);
            for (int i = 0; i < 2; i++)
            {
                float z = (i == 0 ? -.24f : .24f);
                var stalk = new GameObject("EyeStalk" + i).transform; stalk.SetParent(snailRoot, false); stalk.localPosition = new Vector3(1.02f, .65f, z);
                var rod = MgfLook.Prim(PrimitiveType.Cylinder, "Stalk", new Vector3(0, .28f, 0), new Vector3(.055f, .30f, .055f), clothMat, stalk, false);
                var eye = MgfLook.Prim(PrimitiveType.Sphere, "Eye", new Vector3(0, .59f, 0), new Vector3(.16f, .16f, .16f), inkMat, stalk, false);
                eyeStalks[i] = stalk;
            }
            var shell = MgfLook.Prim(PrimitiveType.Cylinder, "ButtonTinShell", new Vector3(-.35f, .38f, .22f), new Vector3(.72f, .22f, .72f), coralMat, snailRoot, false);
            shell.transform.localRotation = Quaternion.Euler(90, 0, 0);
            for (int h = 0; h < 4; h++)
            {
                float a = h * Mathf.PI * .5f;
                MgfLook.Prim(PrimitiveType.Cylinder, "ButtonHole", new Vector3(-.35f + Mathf.Cos(a) * .22f, .38f + Mathf.Sin(a) * .22f, -.02f), new Vector3(.05f, .035f, .05f), inkMat, snailRoot, false)
                    .transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            var tape = MgfLook.Prim(PrimitiveType.Cylinder, "MeasureTape", new Vector3(.10f, .05f, -.48f), new Vector3(.42f, .07f, .42f), brassMat, snailRoot, false);
            tape.transform.localRotation = Quaternion.Euler(90, 0, 0);
            snailRoot.localScale = Vector3.one * 1.48f;
            snailBaseScale = snailRoot.localScale;
            snailRoot.localPosition = new Vector3(2.60f, -4.50f, -1.4f);
        }

        void BuildGondola()
        {
            gondolaRoot = new GameObject("ClotheslineGondola").transform; gondolaRoot.SetParent(worldRoot, false);
            gondolaHome = new Vector3(-4.3f, 4.5f, -.8f); gondolaRoot.localPosition = gondolaHome;
            MgfLook.Block("PetrolBasket", Vector3.zero, new Vector3(2.0f, .78f, .95f), .25f, petrolMat, gondolaRoot);
            for (int s = -1; s <= 1; s += 2)
            {
                var wheel = MgfLook.Prim(PrimitiveType.Cylinder, "BrassWheel", new Vector3(s * .73f, .56f, 0), new Vector3(.27f, .12f, .27f), brassMat, gondolaRoot, false);
                wheel.transform.localRotation = Quaternion.Euler(90, 0, 0);
                MgfLook.Block("Hanger", new Vector3(s * .73f, .30f, 0), new Vector3(.09f, .58f, .09f), .03f, inkMat, gondolaRoot);
            }
            var rope = NewLine("GondolaRope", inkMat, .065f); rope.transform.SetParent(gondolaRoot, false); rope.positionCount = 2;
            rope.useWorldSpace = false; rope.SetPosition(0, new Vector3(-1.2f, .78f, 0)); rope.SetPosition(1, new Vector3(1.2f, .78f, 0));
        }

        void BuildScissors()
        {
            scissorsRoot = new GameObject("BrassScissors").transform; scissorsRoot.SetParent(worldRoot, false);
            scissorsRoot.localPosition = new Vector3(0, -3.95f, -4.0f);
            for (int i = 0; i < 2; i++)
            {
                float z = i == 0 ? -.11f : .11f;
                var blade = MgfLook.Block("Blade", new Vector3(.68f, i == 0 ? .18f : -.18f, z), new Vector3(1.55f, .16f, .10f), .055f, brassMat, scissorsRoot);
                blade.transform.localRotation = Quaternion.Euler(0, 0, i == 0 ? 16f : -16f);
                var ring = MgfLook.Prim(PrimitiveType.Cylinder, "FingerRing", new Vector3(-.42f, i == 0 ? .34f : -.34f, z), new Vector3(.36f, .08f, .36f), coralMat, scissorsRoot, false);
                ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            MgfLook.Prim(PrimitiveType.Sphere, "ScissorPin", Vector3.zero, new Vector3(.19f, .19f, .19f), inkMat, scissorsRoot, false);
            scissorsRoot.localScale = Vector3.one * 2.15f;
        }

        void BuildSail()
        {
            sailMesh = new Mesh { name = "WholeRainLightProjection" }; sailMesh.MarkDynamic();
            smallSailMesh = new Mesh { name = "CutSmallSail" }; smallSailMesh.MarkDynamic();
            remnantMesh = new Mesh { name = "CutRemnant" }; remnantMesh.MarkDynamic();
            var whole = new GameObject("WholeSail"); whole.transform.SetParent(sailRoot, false);
            whole.AddComponent<MeshFilter>().sharedMesh = sailMesh; sailRenderer = whole.AddComponent<MeshRenderer>(); sailRenderer.sharedMaterial = projectionMat;
            sailRenderer.shadowCastingMode = ShadowCastingMode.On; sailRenderer.receiveShadows = true;
            var small = new GameObject("SmallSail"); small.transform.SetParent(sailRoot, false);
            small.AddComponent<MeshFilter>().sharedMesh = smallSailMesh; smallSailRenderer = small.AddComponent<MeshRenderer>(); smallSailRenderer.sharedMaterial = clothGlowMat; small.SetActive(false);
            var remnant = new GameObject("SailRemnant"); remnant.transform.SetParent(sailRoot, false);
            remnant.AddComponent<MeshFilter>().sharedMesh = remnantMesh; remnantRenderer = remnant.AddComponent<MeshRenderer>(); remnantRenderer.sharedMaterial = clothMat; remnant.SetActive(false);

            cutLine = NewLine("PlayerCut", lineMat, .13f); cutLine.positionCount = 2; cutLine.enabled = false;
            ghostLine = NewLine("CorrectGhost", ghostMat, .11f); ghostLine.positionCount = 2; ghostLine.enabled = false;
            threadLine = NewLine("UnravellingThread", threadMat, .075f); threadLine.positionCount = 2; threadLine.enabled = false;

            for (int i = 0; i < 7; i++)
            {
                var l = MgfLook.Prim(PrimitiveType.Sphere, "ABTick" + (i + 1), Vector3.zero, Vector3.one * .20f, brassMat, sailRoot, true);
                var r = MgfLook.Prim(PrimitiveType.Sphere, "ACTick" + (i + 1), Vector3.zero, Vector3.one * .20f, brassMat, sailRoot, true);
                l.GetComponent<SphereCollider>().radius = 1.7f; r.GetComponent<SphereCollider>().radius = 1.7f;
                leftTicks[i] = l.transform; rightTicks[i] = r.transform;
                leftTickRenderers[i] = l.GetComponent<Renderer>(); rightTickRenderers[i] = r.GetComponent<Renderer>();
            }
            for (int i = 0; i < patchMarkers.Length; i++)
            {
                patchMarkers[i] = MgfLook.Block("SewnPatch" + i, Vector3.zero, new Vector3(.62f, .32f, .08f), .07f, i % 2 == 0 ? coralMat : petrolMat, sailRoot);
                Object.Destroy(patchMarkers[i].GetComponent<Collider>()); patchMarkers[i].SetActive(false);
            }
        }

        LineRenderer NewLine(string name, Material material, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(worldRoot, false);
            var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = material; lr.widthMultiplier = width;
            lr.numCapVertices = 4; lr.numCornerVertices = 4; lr.textureMode = LineTextureMode.Stretch;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
            return lr;
        }

        void BuildUi()
        {
            roundedSprite = MakeSprite(96, 0); ringSprite = MakeSprite(96, 1); clothSprite = MakeSprite(96, 2); chevronSprite = MakeSprite(64, 3);
            // 390×844을 기준으로 하되 태블릿/와이드에서 글자가 비정상적으로 커지지 않게 세로에 맞춘다.
            MgfText.Canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = 1f;
            uiRoot = R("PpallaetjulUi", MgfText.Canvas.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            uiRoot.anchorMin = Vector2.zero; uiRoot.anchorMax = Vector2.one; uiRoot.sizeDelta = Vector2.zero;
            BuildTitleUi(); BuildHudUi(); BuildEndUi(); BuildOverlayUi();
            LayoutAll(true);
        }

        void BuildTitleUi()
        {
            titleRt = Full("TitleScreen", uiRoot); titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            var banner = Img(titleRt, "EmbroideredTitleBanner", new Vector2(.5f, .80f), Vector2.zero, new Vector2(350, 170), new Color(Cloth.r, Cloth.g, Cloth.b, .96f), clothSprite);
            Img(banner.transform, "TopStitch", new Vector2(.5f, .86f), Vector2.zero, new Vector2(302, 7), Coral, roundedSprite);
            Img(banner.transform, "BottomStitch", new Vector2(.5f, .15f), Vector2.zero, new Vector2(302, 7), Petrol, roundedSprite);
            var logoShadow = Txt(banner.transform, "빨랫줄 출항", new Vector2(.5f, .57f), new Vector2(4, -6), 48, Petrol, 330); logoShadow.fontStyle = FontStyles.Bold;
            titleLogo = Txt(banner.transform, "빨랫줄 출항", new Vector2(.5f, .57f), Vector2.zero, 48, Coral, 330); titleLogo.fontStyle = FontStyles.Bold;
            titleLogo.outlineWidth = .18f; titleLogo.outlineColor = Ink;
            titleTag = Txt(banner.transform, "돛을 닮게 잘라라", new Vector2(.5f, .24f), Vector2.zero, 18, Ink, 310); titleTag.fontStyle = FontStyles.Bold;
            titleMeta = Txt(titleRt, "중2 · 도형의 닮음  |  최고 첫 시도 기록", new Vector2(.5f, .032f), Vector2.zero, 13, Cloth, 365); titleMeta.outlineWidth = .16f; titleMeta.outlineColor = Ink;
            titleCtaRt = R("WorldScissorsHit", titleRt, new Vector2(.5f, .25f), Vector2.zero, new Vector2(280, 150));
            var cta = Txt(titleCtaRt, "화면을 눌러 실제 절단 연습 시작", new Vector2(.5f, .5f), new Vector2(0, 92), 15, Cloth, 300); cta.outlineWidth = .15f; cta.outlineColor = Ink;
        }

        void BuildHudUi()
        {
            hudRt = Full("PlayHud", uiRoot); hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            var status = Img(hudRt, "ClotheslineStatusFlag", new Vector2(.5f, .965f), Vector2.zero, new Vector2(374, 49), new Color(Petrol.r, Petrol.g, Petrol.b, .96f), clothSprite);
            scoreText = Txt(status.transform, "0000", new Vector2(.14f, .5f), Vector2.zero, 18, Brass, 95); scoreText.fontStyle = FontStyles.Bold;
            statusText = Txt(status.transform, "출항 0/9 · 첫 시도 0/0", new Vector2(.49f, .5f), Vector2.zero, 13, Cloth, 180); statusText.fontStyle = FontStyles.Bold;
            timerText = Txt(status.transform, "90초", new Vector2(.89f, .5f), Vector2.zero, 17, Cloth, 52); timerText.fontStyle = FontStyles.Bold;

            var goal = Img(hudRt, "AlwaysOnGoal", new Vector2(.5f, .905f), Vector2.zero, new Vector2(354, 43), new Color(Brass.r, Brass.g, Brass.b, .96f), roundedSprite);
            goalRt = goal.rectTransform;
            goalText = Txt(goal.transform, "돛을 닮게 자르면 곤돌라가 출항한다", new Vector2(.5f, .5f), Vector2.zero, 14, Ink, 338); goalText.fontStyle = FontStyles.Bold;

            var prompt = Img(hudRt, "BalconyOrder", new Vector2(.5f, .812f), Vector2.zero, new Vector2(356, 118), new Color(Cloth.r, Cloth.g, Cloth.b, .97f), clothSprite);
            promptRt = prompt.rectTransform;
            promptText = Txt(prompt.transform, "", new Vector2(.5f, .59f), Vector2.zero, 14, Ink, 330); promptText.fontStyle = FontStyles.Bold;
            promptText.enableAutoSizing = true; promptText.fontSizeMin = 11; promptText.fontSizeMax = 15;
            ratioText = Txt(prompt.transform, "", new Vector2(.5f, .16f), Vector2.zero, 12, Petrol, 330); ratioText.fontStyle = FontStyles.Bold;

            // 월드 눈금에 붙는 라벨은 모두 중앙 앵커를 써야 ScreenPointToLocal 결과를
            // anchoredPosition에 그대로 넣을 수 있다. 서로 다른 앵커는 DPR 2에서 오프셋을 이중 적용했다.
            leftDimText = Txt(hudRt, "AB", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 150); leftDimText.fontStyle = FontStyles.Bold;
            rightDimText = Txt(hudRt, "AC", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 150); rightDimText.fontStyle = FontStyles.Bold;
            pointText = Txt(hudRt, "A 쪽이 남길 돛", new Vector2(.5f, .5f), Vector2.zero, 14, Petrol, 280); pointText.fontStyle = FontStyles.Bold;
            for (int i = 0; i < 7; i++)
            {
                leftTickLabels[i] = Txt(hudRt, (i + 1).ToString(), new Vector2(.5f, .5f), Vector2.zero, 11, Ink, 28);
                rightTickLabels[i] = Txt(hudRt, (i + 1).ToString(), new Vector2(.5f, .5f), Vector2.zero, 11, Ink, 28);
            }
            for (int i = 0; i < 3; i++)
            {
                var p = Img(hudRt, "Patch" + i, new Vector2(.87f, .13f), new Vector2((i - 1) * 40, 0), new Vector2(34, 26), i % 2 == 0 ? Coral : Petrol, clothSprite);
                patchUi[i] = p.rectTransform;
            }
            Txt(hudRt, "수선 패치", new Vector2(.87f, .09f), Vector2.zero, 12, Ink, 130).outlineWidth = .08f;
        }

        void BuildEndUi()
        {
            endRt = Full("EndScreen", uiRoot); endG = endRt.gameObject.AddComponent<CanvasGroup>();
            var flag = Img(endRt, "ResultLaundryFlag", new Vector2(.5f, .54f), Vector2.zero, new Vector2(356, 430), new Color(Cloth.r, Cloth.g, Cloth.b, .97f), clothSprite);
            Img(flag.transform, "ResultTopStitch", new Vector2(.5f, .93f), Vector2.zero, new Vector2(314, 8), Coral, roundedSprite);
            endTitle = Txt(flag.transform, "완벽한 항로", new Vector2(.5f, .79f), Vector2.zero, 34, Coral, 320); endTitle.fontStyle = FontStyles.Bold; endTitle.outlineWidth = .12f; endTitle.outlineColor = Ink;
            endScore = Txt(flag.transform, "0", new Vector2(.5f, .58f), Vector2.zero, 54, Petrol, 310); endScore.fontStyle = FontStyles.Bold;
            endStats = Txt(flag.transform, "", new Vector2(.5f, .39f), Vector2.zero, 17, Ink, 320); endStats.fontStyle = FontStyles.Bold;
            restartRt = R("RestartScissors", flag.transform, new Vector2(.5f, .15f), Vector2.zero, new Vector2(260, 92));
            Img(restartRt, "ScissorBar", new Vector2(.5f, .58f), Vector2.zero, new Vector2(210, 52), Brass, roundedSprite);
            var t = Txt(restartRt, "가위로 다시 자르기", new Vector2(.5f, .58f), Vector2.zero, 20, Ink, 220); t.fontStyle = FontStyles.Bold;
        }

        void BuildOverlayUi()
        {
            revealRt = Img(uiRoot, "MathReveal", new Vector2(.5f, .45f), Vector2.zero, new Vector2(360, 150), new Color(Cloth.r, Cloth.g, Cloth.b, .98f), clothSprite).rectTransform;
            revealG = revealRt.gameObject.AddComponent<CanvasGroup>(); revealImage = revealRt.GetComponent<Image>();
            revealText = Txt(revealRt, "", new Vector2(.5f, .5f), Vector2.zero, 17, Ink, 330); revealText.fontStyle = FontStyles.Bold;
            revealText.enableAutoSizing = true; revealText.fontSizeMin = 13; revealText.fontSizeMax = 18;

            toastRt = Img(uiRoot, "RefusalRibbon", new Vector2(.5f, .17f), Vector2.zero, new Vector2(346, 74), new Color(Petrol.r, Petrol.g, Petrol.b, .96f), clothSprite).rectTransform;
            toastG = toastRt.gameObject.AddComponent<CanvasGroup>();
            toastText = Txt(toastRt, "", new Vector2(.5f, .5f), Vector2.zero, 14, Cloth, 320); toastText.fontStyle = FontStyles.Bold;

            rippleRt = Img(uiRoot, "TouchClothRipple", new Vector2(.5f, .5f), Vector2.zero, new Vector2(76, 76), new Color(Brass.r, Brass.g, Brass.b, .8f), ringSprite).rectTransform;
            rippleImage = rippleRt.GetComponent<Image>(); rippleRt.gameObject.SetActive(false);

            guidePathRt = Img(uiRoot, "PracticePath", new Vector2(.5f, .5f), Vector2.zero, new Vector2(9, 140), new Color(Brass.r, Brass.g, Brass.b, .66f), roundedSprite).rectTransform;
            guidePathImage = guidePathRt.GetComponent<Image>();
            guideHandRt = Img(uiRoot, "PracticeHand", new Vector2(.5f, .5f), Vector2.zero, new Vector2(62, 62), Cloth, roundedSprite).rectTransform;
            guideHandImage = guideHandRt.GetComponent<Image>(); Txt(guideHandRt, "끌기", new Vector2(.5f, .5f), Vector2.zero, 12, Ink, 56).fontStyle = FontStyles.Bold;
            guideTargetRt = Img(uiRoot, "PracticeTarget", new Vector2(.5f, .5f), Vector2.zero, new Vector2(74, 74), Coral, ringSprite).rectTransform;
            guideTargetImage = guideTargetRt.GetComponent<Image>();
            for (int i = 0; i < guideChevrons.Length; i++)
                guideChevrons[i] = Img(uiRoot, "FlowChevron" + i, new Vector2(.5f, .5f), Vector2.zero, new Vector2(25, 25), Brass, chevronSprite).rectTransform;

            titleGuideRingRt = Img(uiRoot, "TitleScissorsPulse", new Vector2(.5f, .5f), Vector2.zero, new Vector2(142, 142), Coral, ringSprite).rectTransform;
            titleGuideArrowRt = Img(uiRoot, "TitleScissorsArrow", new Vector2(.5f, .5f), Vector2.zero, new Vector2(13, 150), new Color(Brass.r, Brass.g, Brass.b, .92f), roundedSprite).rectTransform;

            revealG.alpha = toastG.alpha = 0f;
            guidePathRt.gameObject.SetActive(false); guideHandRt.gameObject.SetActive(false); guideTargetRt.gameObject.SetActive(false);
            for (int i = 0; i < guideChevrons.Length; i++) guideChevrons[i].gameObject.SetActive(false);
            titleGuideRingRt.gameObject.SetActive(false); titleGuideArrowRt.gameObject.SetActive(false);
        }

        void LayoutAll(bool force = false)
        {
            if (!force && layoutW == Screen.width && layoutH == Screen.height) return;
            layoutW = Screen.width; layoutH = Screen.height;
            Canvas.ForceUpdateCanvases();
            // WebGL의 Screen.width는 DPR이 반영될 수 있으므로 픽셀 수가 아니라 실제 화면 비율로 분기한다.
            // 그렇지 않으면 820×1180 태블릿(DPR 2)이 1640px 가로 화면으로 오인되어 주문표가 잘린다.
            bool wide = Screen.width > Screen.height * 1.20f;
            if (wide)
            {
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(.18f, .68f); promptRt.sizeDelta = new Vector2(390, 170);
                goalRt.anchorMin = goalRt.anchorMax = new Vector2(.82f, .68f); goalRt.sizeDelta = new Vector2(330, 52);
                timerText.rectTransform.anchorMin = timerText.rectTransform.anchorMax = new Vector2(.89f, .5f);
                titleLogo.fontSize = 58; titleCtaRt.anchorMin = titleCtaRt.anchorMax = new Vector2(.5f, .25f);
            }
            else
            {
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(.5f, .812f); promptRt.sizeDelta = new Vector2(356, 118);
                goalRt.anchorMin = goalRt.anchorMax = new Vector2(.5f, .905f); goalRt.sizeDelta = new Vector2(354, 43);
                // 브라우저의 48px 음소거 버튼 오른쪽 예약 영역을 피해 타이머를 안쪽으로 둔다.
                timerText.rectTransform.anchorMin = timerText.rectTransform.anchorMax = new Vector2(.79f, .5f);
                titleLogo.fontSize = 48; titleCtaRt.anchorMin = titleCtaRt.anchorMax = new Vector2(.5f, .25f);
            }
            LayoutSail(wide);
        }

        void LayoutSail(bool wide)
        {
            float ax = .5f, ay = wide ? .78f : .665f;
            float bx = wide ? .27f : .11f, cx = wide ? .73f : .89f;
            float by = wide ? .20f : .275f;
            float depth = 20.5f;
            sailA = cam.ScreenToWorldPoint(new Vector3(Screen.width * ax, Screen.height * ay, depth));
            sailB = cam.ScreenToWorldPoint(new Vector3(Screen.width * bx, Screen.height * by, depth));
            sailC = cam.ScreenToWorldPoint(new Vector3(Screen.width * cx, Screen.height * by, depth));
            SetTriMesh(sailMesh, sailA, sailB, sailC);
            for (int i = 0; i < 7; i++)
            {
                float t = (i + 1) / 8f;
                leftTicks[i].position = Vector3.Lerp(sailA, sailB, t) - cam.transform.right * .05f;
                rightTicks[i].position = Vector3.Lerp(sailA, sailC, t) + cam.transform.right * .05f;
                Vector2 ls = WorldToUiLocal(leftTicks[i].position), rs = WorldToUiLocal(rightTicks[i].position);
                leftTickLabels[i].rectTransform.anchoredPosition = ls + new Vector2(-18, 0);
                rightTickLabels[i].rectTransform.anchoredPosition = rs + new Vector2(18, 0);
            }
            Vector2 leftDim = Vector2.Lerp(WorldToUiLocal(sailA), WorldToUiLocal(sailB), .58f) + new Vector2(-62, 0);
            Vector2 rightDim = Vector2.Lerp(WorldToUiLocal(sailA), WorldToUiLocal(sailC), .58f) + new Vector2(62, 0);
            // 390px 폰에서도 B/C와 cm까지 모두 읽히도록 라벨 전체를 UI 안전 폭 안에 둔다.
            float leftHalf = leftDimText.rectTransform.sizeDelta.x * .5f;
            float rightHalf = rightDimText.rectTransform.sizeDelta.x * .5f;
            leftDim.x = Mathf.Max(leftDim.x, uiRoot.rect.xMin + leftHalf + 6f);
            rightDim.x = Mathf.Min(rightDim.x, uiRoot.rect.xMax - rightHalf - 6f);
            leftDimText.rectTransform.anchoredPosition = leftDim;
            rightDimText.rectTransform.anchoredPosition = rightDim;
            // 꼭짓점 설명은 A 바로 위 빈 공간에 둬 주문 카드와 1번 눈금 모두와 교차하지 않는다.
            pointText.rectTransform.anchoredPosition = WorldToUiLocal(sailA) + new Vector2(0, 28f);
            if (cutLine.enabled && st.selectedI > 0 && st.selectedJ > 0) SetCutLine(st.selectedI, st.selectedJ, cutLine);
            if (ghostLine.enabled && current != null) SetCutLine(current.k, current.k, ghostLine);
        }

        // WorldToScreenPoint의 결과는 WebGL DPR이 반영된 장치 픽셀이지만,
        // 중앙 앵커를 쓰는 CanvasScaler 자식의 anchoredPosition은 390×844 기준 UI 좌표다.
        // ScreenPointToLocalPointInRectangle은 WebGL의 카메라 장치 픽셀을 Canvas 스케일로 나누지
        // 않는 경우가 있어, 실제 scaleFactor와 uiRoot rect로 명시적으로 변환한다.
        Vector2 ScreenToUiLocal(Vector2 screen)
        {
            float scale = Mathf.Max(.0001f, MgfText.Canvas.scaleFactor);
            Rect rect = uiRoot.rect;
            return new Vector2(rect.xMin + screen.x / scale, rect.yMin + screen.y / scale);
        }

        Vector2 WorldToUiLocal(Vector3 world)
        {
            return ScreenToUiLocal(cam.WorldToScreenPoint(world));
        }

        void SetTriMesh(Mesh mesh, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(c - a, b - a).normalized;
            if (Vector3.Dot(n, cam.transform.forward) > 0f) n = -n;
            mesh.Clear();
            mesh.vertices = new[] { a, b, c, a, c, b };
            mesh.normals = new[] { n, n, n, -n, -n, -n };
            mesh.uv = new[] { new Vector2(.5f, 1), Vector2.zero, Vector2.right, new Vector2(.5f, 1), Vector2.right, Vector2.zero };
            mesh.triangles = new[] { 0, 1, 2, 3, 4, 5 };
            mesh.RecalculateBounds();
        }

        void SetCutMeshes(int i, int j)
        {
            Vector3 p = Vector3.Lerp(sailA, sailB, i / 8f), q = Vector3.Lerp(sailA, sailC, j / 8f);
            SetTriMesh(smallSailMesh, sailA, p, q);
            remnantMesh.Clear();
            Vector3 n = Vector3.Cross(sailC - sailB, p - sailB).normalized;
            remnantMesh.vertices = new[] { p, sailB, sailC, q, p, sailC };
            remnantMesh.normals = new[] { n, n, n, n, n, n };
            remnantMesh.uv = new[] { Vector2.up, Vector2.zero, Vector2.right, Vector2.one, Vector2.up, Vector2.right };
            remnantMesh.triangles = new[] { 0, 1, 2, 3, 4, 5 }; remnantMesh.RecalculateBounds();
        }

        void ShowTitleVisual()
        {
            worldTime = 0f; gondolaTripT = 0f; snailReactT = 0f;
            current = PpallaetjulRules.Practice();
            ResetSailVisual();
            gondolaRoot.localPosition = new Vector3(-3.2f, 2.45f, -.8f);
            titleMeta.text = PlayerPrefs.GetInt("ppallaetjul.bestFirst", 0) > 0
                ? "중2 · 도형의 닮음  |  최고 첫 시도 " + PlayerPrefs.GetInt("ppallaetjul.bestFirst", 0) + "/9"
                : "중2 · 도형의 닮음  |  90초 · 곤돌라 9대";
        }

        void ResetRunVisual()
        {
            for (int i = 0; i < patchMarkers.Length; i++) patchMarkers[i].SetActive(false);
            for (int i = 0; i < successWindowLights.Length; i++)
            {
                if (successWindowLights[i]) successWindowLights[i].enabled = false;
                if (successAwnings[i]) successAwnings[i].localScale = new Vector3(successAwningScales[i].x, successAwningScales[i].y, successAwningScales[i].z * .08f);
            }
            gondolaRoot.localPosition = gondolaHome; snailRoot.localScale = snailBaseScale;
            shownScore = 0f; shownEndScore = 0f; shownTimer = -1;
            ResetSailVisual();
        }

        void ResetSailVisual()
        {
            sailRenderer.gameObject.SetActive(true); sailRenderer.sharedMaterial = projectionMat;
            smallSailRenderer.gameObject.SetActive(false); remnantRenderer.gameObject.SetActive(false);
            smallSailRenderer.transform.position = Vector3.zero; smallSailRenderer.transform.localScale = Vector3.one;
            remnantRenderer.transform.position = Vector3.zero; remnantRenderer.transform.localScale = Vector3.one;
            cutLine.enabled = false; ghostLine.enabled = false; threadLine.enabled = false;
            st.selectedI = st.selectedJ = -1; startSide = startTick = -1;
        }

        void LoadProblemVisual(bool practice)
        {
            ResetSailVisual();
            promptText.text = current.prompt;
            leftDimText.text = "B · AB " + current.ab + " cm";
            rightDimText.text = "C · AC " + current.ac + " cm";
            ratioText.text = practice ? "연습 · 시간이 흐르지 않고 패치를 쓰지 않는다" : StageCaption();
            pointText.text = "A 쪽이 남길 △APQ";
            if (current.kind == SailOrderKind.RepairCut && !practice)
            {
                st.selectedI = current.wrong1I; st.selectedJ = current.wrong1J;
                SetCutLine(current.wrong1I, current.wrong1J, cutLine); cutLine.enabled = true;
                AddPatchAt(current.wrong1I, current.wrong1J, Mathf.Clamp(PpallaetjulRules.StartPatches - st.lives, 0, 2));
            }
            RefreshHud();
        }

        string StageCaption()
        {
            if (current.band == 1) return "1묶음 · 두 대응변의 비와 공통인 ∠A를 확인하시오";
            if (current.band == 2) return "2묶음 · 길이로 대응변의 비를 역추적하시오";
            return "3묶음 · 넓이의 비를 길이의 닮음비로 바꾸시오";
        }

        bool TryStartCut(Vector2 screen, out int side, out int tick)
        {
            side = -1; tick = -1; float best = 52f * 52f;
            for (int s = 0; s < 2; s++)
            for (int i = 0; i < 7; i++)
            {
                Vector2 p = cam.WorldToScreenPoint(s == 0 ? leftTicks[i].position : rightTicks[i].position);
                float d = (p - screen).sqrMagnitude;
                if (d < best) { best = d; side = s; tick = i + 1; }
            }
            if (side < 0) return false;
            startSide = side; startTick = tick;
            cutLine.enabled = true; cutLine.positionCount = 2;
            Vector3 start = side == 0 ? leftTicks[tick - 1].position : rightTicks[tick - 1].position;
            cutLine.SetPosition(0, start); cutLine.SetPosition(1, start);
            threadLine.enabled = true; threadLine.positionCount = 2; threadLine.SetPosition(0, start); threadLine.SetPosition(1, start);
            leftTickRenderers[tick - 1].sharedMaterial = side == 0 ? brassMat : leftTickRenderers[tick - 1].sharedMaterial;
            rightTickRenderers[tick - 1].sharedMaterial = side == 1 ? brassMat : rightTickRenderers[tick - 1].sharedMaterial;
            return true;
        }

        void DragCut(Vector2 screen)
        {
            Vector3 end = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 20.5f));
            cutLine.SetPosition(1, end); threadLine.SetPosition(1, end);
        }

        bool TryFinishCut(Vector2 screen, out int i, out int j)
        {
            i = j = -1;
            int other = 1 - startSide, found = -1; float best = 58f * 58f;
            for (int n = 0; n < 7; n++)
            {
                Vector2 p = cam.WorldToScreenPoint(other == 0 ? leftTicks[n].position : rightTicks[n].position);
                float d = (p - screen).sqrMagnitude;
                if (d < best) { best = d; found = n + 1; }
            }
            threadLine.enabled = false;
            if (found < 0) return false;
            if (startSide == 0) { i = startTick; j = found; } else { i = found; j = startTick; }
            SetCutLine(i, j, cutLine); cutLine.enabled = true;
            return true;
        }

        void SetCutLine(int i, int j, LineRenderer line)
        {
            line.positionCount = 2;
            line.SetPosition(0, Vector3.Lerp(sailA, sailB, i / 8f));
            line.SetPosition(1, Vector3.Lerp(sailA, sailC, j / 8f));
        }

        void UpdateKeyboardCut(int i, int j, int side)
        {
            cutLine.enabled = true; SetCutLine(i, j, cutLine);
            leftTicks[i - 1].localScale = Vector3.one * (side == 0 ? .28f : .20f);
            rightTicks[j - 1].localScale = Vector3.one * (side == 1 ? .28f : .20f);
        }

        void RejectCut() { cutLine.enabled = false; threadLine.enabled = false; sailShakeT = .34f; }

        void OnCorrect(SailProblem p, int i, int j, bool practice, int combo)
        {
            // 정답 사건은 천 조각을 떼어 운반하지 않는다. 비례 투영 전체가 잠기고
            // 골목의 대응 창 차양과 불빛이 순서대로 펼쳐져 기존 재단 게임과 다른 결과를 만든다.
            sailRenderer.gameObject.SetActive(true); sailRenderer.sharedMaterial = projectionGlowMat;
            smallSailRenderer.gameObject.SetActive(false); remnantRenderer.gameObject.SetActive(false);
            cutLine.enabled = false; threadLine.enabled = true; SetCutLine(i, j, threadLine);
            ghostLine.enabled = false; reactCorrect = true; snailReactT = 0f; gondolaTripT = 0f; clothFlashT = .65f;
            revealImage.color = new Color(Cloth.r, Cloth.g, Cloth.b, .98f);
            revealText.text = (practice ? "첫 절단 성공\n" : "닮은 돛 완성\n") + PpallaetjulRules.Evidence(p);
            revealG.alpha = 1f;
            MgfFx.Punch(scoreText.transform, .13f + Mathf.Min(4, combo) * .018f, .30f);
            RefreshHud();
        }

        void OnWrong(SailProblem p, int i, int j, bool practice, string misconception)
        {
            reactCorrect = false; snailReactT = 0f; sailShakeT = .36f;
            cutLine.enabled = true; SetCutLine(i, j, cutLine);
            ghostLine.enabled = true; SetCutLine(p.k, p.k, ghostLine);
            revealImage.color = new Color(Brass.r, Brass.g, Brass.b, .97f);
            string ratios = "AP:AB=" + PpallaetjulRules.RatioText(i, 8) + "  ·  AQ:AC=" + PpallaetjulRules.RatioText(j, 8);
            revealText.text = "두 비를 다시 확인\n" + ratios + "\n" + PpallaetjulRules.WrongExplanation(p, i, j);
            revealG.alpha = 1f;
            if (!practice) AddPatchAt(i, j, Mathf.Clamp(PpallaetjulRules.StartPatches - st.lives - 1, 0, 2));
            RefreshHud();
        }

        void AddPatchAt(int i, int j, int index)
        {
            if (index < 0 || index >= patchMarkers.Length) return;
            Vector3 p = Vector3.Lerp(sailA, sailB, i / 8f), q = Vector3.Lerp(sailA, sailC, j / 8f);
            patchMarkers[index].transform.position = Vector3.Lerp(p, q, .5f) - cam.transform.forward * .05f;
            patchMarkers[index].transform.rotation = cam.transform.rotation * Quaternion.Euler(0, 0, 12f + index * 17f);
            patchMarkers[index].SetActive(true);
        }

        void HideFeedback()
        {
            revealG.alpha = 0f; ghostLine.enabled = false; threadLine.enabled = false; smallSailRenderer.gameObject.SetActive(false); remnantRenderer.gameObject.SetActive(false);
        }

        void ResumeRepairVisual()
        {
            sailRenderer.gameObject.SetActive(true); sailRenderer.sharedMaterial = projectionMat;
            smallSailRenderer.gameObject.SetActive(false); remnantRenderer.gameObject.SetActive(false);
            cutLine.enabled = true; SetCutLine(st.selectedI, st.selectedJ, cutLine);
            ghostLine.enabled = false; threadLine.enabled = false;
            ratioText.text = "수리 · 첫 시도 기록은 바뀌지 않는다. 두 비를 맞춰 다시 자르시오";
        }

        void ShowBandTransition(int band)
        {
            transitionT = 2f;
            revealG.alpha = 1f; revealImage.color = new Color(Petrol.r, Petrol.g, Petrol.b, .97f);
            revealText.color = Cloth;
            revealText.text = band == 2 ? "두 번째 빨랫줄\n이제 비 읽기 없이 대응변 길이로 자른다"
                : "넓이 주문 개방\n길이가 1:2이면 넓이는 1:4 · 작은 돛은 네 조각 중 하나";
            gondolaTripT = 0f;
        }

        void SwapGoalAfterPractice()
        {
            goalText.text = "두 변에서 같은 비율을 남기면 곤돌라가 출항한다";
            MgfFx.Punch(goalText.transform, .10f, .36f);
        }

        void PrepareEnd(string reason)
        {
            bool clear = reason == "clear";
            if (clear)
            {
                endTitle.text = "빨랫줄 항로 개방";
                PlayerPrefs.SetInt("ppallaetjul.bestFirst", Mathf.Max(PlayerPrefs.GetInt("ppallaetjul.bestFirst", 0), st.firstAttemptCorrect));
            }
            else if (reason == "mastery") endTitle.text = "출항했지만 판단 연습이 더 필요하다";
            else if (reason == "time") endTitle.text = "해가 지기 전에 다시 도전";
            else endTitle.text = "수선 패치를 모두 사용했다";
            endStats.text = "출항 " + st.solved + "/9  ·  첫 시도 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal
                + "\n마지막 묶음 첫 시도 " + st.finalBandFirst + "/3  ·  패치 " + Mathf.Max(0, st.lives);
            shownEndScore = 0f; endScore.text = "0";
        }

        void RefreshHud()
        {
            statusText.text = "출항 " + st.solved + "/9 · 첫 시도 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal;
            for (int i = 0; i < patchUi.Length; i++) patchUi[i].localScale = i < st.lives ? Vector3.one : Vector3.one * .34f;
        }

        void UpdateView(float dt)
        {
            LayoutAll();
            worldTime += dt;
            bool title = runPhase == RunPhase.Title;

            // ambient-idle-motion: 위치를 누적하지 않고 기준 상태에서 계산한다.
            for (int i = 0; i < laundry.Length; i++) if (laundry[i])
            {
                float gust = 2.5f + (gondolaTripT > .08f ? 8f * (1f - gondolaTripT) : 0f);
                laundry[i].localPosition = laundryBase[i] + Vector3.up * Mathf.Sin(worldTime * 1.4f + i) * .045f;
                laundry[i].localRotation = Quaternion.Euler(Mathf.Sin(worldTime * 1.7f + i * .7f) * gust, 0, Mathf.Sin(worldTime * 1.2f + i) * 2f);
            }
            for (int i = 0; i < eyeStalks.Length; i++) if (eyeStalks[i])
                eyeStalks[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(worldTime * 1.8f + i) * 6f + (reactCorrect ? -12f : snailReactT > 0 ? 8f : 0));
            if (title) scissorsRoot.localRotation = Quaternion.Euler(0, Mathf.Sin(worldTime * .9f) * 7f, Mathf.Sin(worldTime * 1.35f) * 5f);

            if (snailReactT >= 0f && (runPhase == RunPhase.Feedback || transitionT > 0f))
            {
                snailReactT += dt;
                float pulse = Mathf.Sin(Mathf.Clamp01(snailReactT / .55f) * Mathf.PI);
                snailRoot.localScale = reactCorrect ? new Vector3(1f - .08f * pulse, 1f + .24f * pulse, 1f - .05f * pulse)
                    : new Vector3(1f + .14f * pulse, 1f - .28f * pulse, 1f + .10f * pulse);
            }
            else snailRoot.localScale = Vector3.Lerp(snailRoot.localScale, snailBaseScale, dt * 6f);

            if (reactCorrect && runPhase == RunPhase.Feedback)
            {
                gondolaTripT = Mathf.Clamp01(gondolaTripT + dt / .72f);
                float ease = 1f - Mathf.Pow(1f - gondolaTripT, 3f);
                gondolaRoot.localPosition = gondolaHome + Vector3.right * (8.6f * ease) + Vector3.up * Mathf.Sin(ease * Mathf.PI) * .45f;
                int litCount = Mathf.Clamp(st.solved, 0, successWindowLights.Length);
                if (feedbackPractice) litCount = Mathf.Max(1, litCount);
                for (int i = 0; i < successWindowLights.Length; i++)
                {
                    bool on = i < litCount;
                    if (successWindowLights[i]) successWindowLights[i].enabled = on;
                    if (successAwnings[i])
                    {
                        float delay = Mathf.Max(0f, feedbackT - .08f - i * .055f);
                        float open = on ? Mathf.SmoothStep(.08f, 1f, Mathf.Clamp01(delay / .42f)) : .08f;
                        Vector3 b = successAwningScales[i]; successAwnings[i].localScale = new Vector3(b.x, b.y, b.z * open);
                    }
                }
            }

            if (clothFlashT > 0f)
            {
                clothFlashT -= dt; sailRenderer.sharedMaterial = clothFlashT > .30f ? projectionGlowMat : projectionMat;
            }
            if (sailShakeT > 0f)
            {
                sailShakeT -= dt;
                float x = Mathf.Sin(sailShakeT * 70f) * sailShakeT * .20f;
                sailRoot.localPosition = cam.transform.right * x;
            }
            else sailRoot.localPosition = Vector3.zero;

            if (rippleRt.gameObject.activeSelf)
            {
                rippleT += dt; float q = Mathf.Clamp01(rippleT / .42f);
                rippleRt.localScale = Vector3.one * Mathf.Lerp(.35f, 1.65f, q);
                rippleImage.color = new Color(Brass.r, Brass.g, Brass.b, (1f - q) * .82f);
                if (q >= 1f) rippleRt.gameObject.SetActive(false);
            }
            if (toastG.alpha > 0f)
            {
                toastT += dt; toastG.alpha = toastT < 1.7f ? 1f : Mathf.Clamp01(2.2f - toastT);
            }
            if (transitionT > 0f)
            {
                transitionT -= dt;
                if (transitionT <= 0f && runPhase != RunPhase.Feedback) { revealG.alpha = 0f; revealText.color = Ink; }
            }

            UpdateGuide(dt);
            UpdateTitleGuide();
            shownScore = Mathf.MoveTowards(shownScore, st.score, Mathf.Max(90f, (st.score - shownScore) * 7f) * dt);
            scoreText.text = Mathf.RoundToInt(shownScore).ToString("0000");
            int sec = Mathf.Max(0, Mathf.CeilToInt(sessionLeft));
            if (shownTimer != sec) { shownTimer = sec; timerText.text = sec + "초"; if (sec <= 15) timerText.color = Coral; }
            if (runPhase == RunPhase.End)
            {
                shownEndScore = Mathf.MoveTowards(shownEndScore, st.score, Mathf.Max(160f, st.score * 1.4f) * dt);
                endScore.text = Mathf.RoundToInt(shownEndScore).ToString();
            }
        }

        void UpdateGuide(float dt)
        {
            if (!guideHandRt.gameObject.activeSelf) return;
            Vector2 start = WorldToUiLocal(leftTicks[3].position), end = WorldToUiLocal(rightTicks[3].position);
            float q = Mathf.PingPong(worldTime / 1.2f, 1f);
            guideHandRt.anchoredPosition = Vector2.Lerp(start, end, q);
            guideHandRt.localScale = Vector3.one * (1f + .12f * Mathf.Sin(worldTime * 7f));
            guideTargetRt.anchoredPosition = end; guideTargetRt.localScale = Vector3.one * (1f + .10f * Mathf.Sin(worldTime * 5f));
            Vector2 d = end - start;
            guidePathRt.anchoredPosition = (start + end) * .5f; guidePathRt.sizeDelta = new Vector2(8, d.magnitude);
            guidePathRt.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            for (int i = 0; i < guideChevrons.Length; i++)
            {
                float u = Mathf.Repeat(worldTime * .55f + i / 3f, 1f);
                guideChevrons[i].anchoredPosition = Vector2.Lerp(start, end, u); guideChevrons[i].rotation = guidePathRt.rotation;
            }
        }

        void SetScreen()
        {
            bool title = runPhase == RunPhase.Title, end = runPhase == RunPhase.End;
            titleG.alpha = title ? 1 : 0; titleG.interactable = title; titleG.blocksRaycasts = title;
            hudG.alpha = !title && !end ? 1 : 0; hudG.interactable = !title && !end; hudG.blocksRaycasts = !title && !end;
            endG.alpha = end ? 1 : 0; endG.interactable = end; endG.blocksRaycasts = end;
            st.phase = title ? "title" : end ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            bool measurements = !title && !end;
            for (int i = 0; i < 7; i++)
            {
                leftTicks[i].gameObject.SetActive(measurements); rightTicks[i].gameObject.SetActive(measurements);
                leftTickLabels[i].gameObject.SetActive(measurements); rightTickLabels[i].gameObject.SetActive(measurements);
            }
            scissorsRoot.gameObject.SetActive(title || end);
            titleGuideRingRt.gameObject.SetActive(title);
            titleGuideArrowRt.gameObject.SetActive(title);
            if (measurements) RefreshHud();
        }

        void UpdateTitleGuide()
        {
            if (runPhase != RunPhase.Title || !titleGuideRingRt.gameObject.activeSelf) return;
            Vector2 target = WorldToUiLocal(scissorsRoot.position);
            titleGuideRingRt.anchoredPosition = target;
            titleGuideRingRt.localScale = Vector3.one * (1f + .16f * Mathf.Sin(worldTime * 5.2f));
            Vector2 from = ScreenToUiLocal(new Vector2(Screen.width * .5f, Screen.height * .46f));
            Vector2 d = target - from;
            titleGuideArrowRt.anchoredPosition = (from + target) * .5f;
            titleGuideArrowRt.sizeDelta = new Vector2(13, Mathf.Max(80, d.magnitude));
            titleGuideArrowRt.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
        }

        bool HitTitleCta(Vector2 p)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(titleCtaRt, p)) return true;
            Vector2 s = cam.WorldToScreenPoint(scissorsRoot.position); return (p - s).sqrMagnitude < 96f * 96f;
        }
        bool HitRestart(Vector2 p) { return RectTransformUtility.RectangleContainsScreenPoint(restartRt, p); }

        void SpawnRipple(Vector2 screen)
        {
            rippleRt.anchoredPosition = ScreenToUiLocal(screen);
            rippleRt.localScale = Vector3.one * .3f; rippleRt.gameObject.SetActive(true); rippleT = 0f;
        }

        void Refuse(string message)
        {
            toastText.text = message; toastT = 0f; toastG.alpha = 1f;
            toastRt.gameObject.SetActive(false); toastRt.gameObject.SetActive(true);
        }

        void NudgeSail() { sailShakeT = .28f; }
        void StartGuide()
        {
            guideHandRt.gameObject.SetActive(true); guidePathRt.gameObject.SetActive(true); guideTargetRt.gameObject.SetActive(true);
            for (int i = 0; i < guideChevrons.Length; i++) guideChevrons[i].gameObject.SetActive(true);
        }
        void HideGuide()
        {
            guideHandRt.gameObject.SetActive(false); guidePathRt.gameObject.SetActive(false); guideTargetRt.gameObject.SetActive(false);
            for (int i = 0; i < guideChevrons.Length; i++) guideChevrons[i].gameObject.SetActive(false);
        }
        void BoostGuide() { StartGuide(); guideHandRt.localScale = Vector3.one * 1.35f; MgfSfx.Play("pop", .18f); }
        void PointToSail() { if (runPhase == RunPhase.Practice) StartGuide(); else { leftTicks[2].localScale = rightTicks[4].localScale = Vector3.one * .30f; } }
        void PointToTitleCta() { MgfFx.Punch(titleCtaRt, .18f, .30f); MgfFx.Punch(scissorsRoot, .12f, .30f); }
        void PointToRestart() { MgfFx.Punch(restartRt, .18f, .30f); }

        static RectTransform Full(string name, Transform parent)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero; return rt;
        }

        static RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = offset; rt.sizeDelta = size; return rt;
        }

        static Image Img(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color color, Sprite sprite)
        {
            var rt = R(name, parent, anchor, offset, size); var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = false; return img;
        }

        static TextMeshProUGUI Txt(Transform parent, string value, Vector2 anchor, Vector2 offset, float size, Color color, float width)
        {
            var rt = R("Text", parent, anchor, offset, new Vector2(width, size * 2.4f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color;
            t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false; t.text = value;
            return t;
        }

        static Sprite MakeSprite(int size, int kind)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "PpallaetjulUi" + kind, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size * 2f - 1f, v = (y + .5f) / size * 2f - 1f;
                float ax = Mathf.Abs(u), ay = Mathf.Abs(v); float alpha;
                if (kind == 0) alpha = Mathf.Clamp01((1f - Mathf.Max(ax - .82f, ay - .82f) * 7f));
                else if (kind == 1) { float r = Mathf.Sqrt(u * u + v * v); alpha = r > .68f && r < .96f ? 1f : 0f; }
                else if (kind == 2)
                {
                    float edge = Mathf.Max(ax, ay); alpha = edge < .94f ? 1f : 0f;
                    if (alpha > 0 && ((x + y * 3) % 19 == 0 || (x * 2 + y) % 23 == 0)) alpha = .72f;
                }
                else alpha = (u > -.55f && u < .55f && Mathf.Abs(v) < (.62f - u * .34f)) ? 1f : 0f;
                px[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(14, 14, 14, 14));
        }
    }
}
