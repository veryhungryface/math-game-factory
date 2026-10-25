// 한 입만 — 코코아 진열장 2.5D 월드, 탑뷰 각도 재단 무대, 온보딩·결과 UI.
// 정답은 크림 봉합선 개방→층 조각 인양→맥 접시 전달, 오답은 케이크만 국소 흔들림→크림 둑과 냅킨 얼룩이다.
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.HanIpman
{
    public partial class HanIpmanGame
    {
        static readonly Color Cocoa = MgfLook.Hex("#684331");
        static readonly Color DarkCocoa = MgfLook.Hex("#35251D");
        static readonly Color Caramel = MgfLook.Hex("#B67844");
        static readonly Color Ivory = MgfLook.Hex("#F6E5C9");
        static readonly Color Plum = MgfLook.Hex("#763F68");
        static readonly Color Steel = MgfLook.Hex("#C3CDD2");
        static readonly Color Jam = MgfLook.Hex("#8E274F");

        Camera cam;
        Transform titleRoot, gameRoot, boardRoot, guestRail, cakeRoot, serviceRoot, seamGlow;
        readonly Transform[] titleDoors = new Transform[2];
        readonly Transform[] guests = new Transform[3];
        readonly Transform[] plates = new Transform[3];
        readonly Transform[] steam = new Transform[8];
        readonly LineRenderer[] failedCuts = new LineRenderer[3];
        LineRenderer oaLine, obLine, ghostPath;
        Mesh previewMesh, serviceTopMesh, serviceCreamMesh;
        MeshFilter previewFilter, serviceTopFilter, serviceCreamFilter;
        Material cocoaMat, darkMat, caramelMat, ivoryMat, plumMat, steelMat, jamMat, creamMat, glassMat, guideMat;
        Vector3 boardHome, serviceStart, serviceEnd;
        float worldClock, guideBoost, rippleClock, toastClock, titlePress, openingAnim;
        float revealAnim, hitStop, wrongKick, seamFlash, scoreShown, endScoreShown;
        int lastW = -1, lastH = -1, shownSecond = -1, shownScore = -1, failedCutCount;
        bool land;

        CanvasGroup titleG, hudG, revealG, endG, toastG;
        RectTransform titleRt, hudRt, revealRt, endRt, toastRt, rippleRt, angleRt, promptRt, formulaRt;
        Image titleSign, titleCta, goalBg, promptBg, formulaBg, revealBg, rippleImg, napkin1, napkin2, napkin3;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleBest, titleCtaText;
        TextMeshProUGUI scoreTxt, progressTxt, firstTxt, timeTxt, goalTxt, promptTxt, formulaTxt, angleTxt, directionTxt;
        TextMeshProUGUI revealTitleTxt, revealFormulaTxt, toastTxt, endTitleTxt, endScoreTxt, endStatsTxt, endTapTxt;
        Sprite roundSprite, ringSprite;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size); var im = rt.gameObject.AddComponent<Image>();
            im.color = color; im.sprite = sprite; im.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color, float width = 360,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 2.45f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color;
            t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text; return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "HipRing" : "HipRound", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                if (ring)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - size * .40f) / (size * .055f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * .5f) - (size * .5f - radius), 0);
                    float qy = Mathf.Max(Mathf.Abs(fy - size * .5f) - (size * .5f - radius), 0);
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true); float b = ring ? 0 : radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#2D1712"), Cocoa, MgfLook.Hex("#170E0B"), .72f);
            MgfLook.Sun(new Vector3(42, -32, 18), MgfLook.Hex("#FFD29A"), 1.26f, .66f);
            cam = MgfLook.Camera(new Vector3(8.8f, 8.3f, -12.9f), new Vector3(0, 1.55f, 1.35f), 36f);
            cocoaMat = MgfLook.Lit(Cocoa, .36f, .04f);
            darkMat = MgfLook.Lit(DarkCocoa, .22f, .02f);
            caramelMat = MgfLook.Lit(Caramel, .48f, .10f, Caramel * .06f);
            ivoryMat = MgfLook.Lit(Ivory, .62f, .02f);
            plumMat = MgfLook.Lit(Plum, .48f, .02f, Plum * .09f);
            steelMat = MgfLook.Lit(Steel, .78f, .62f);
            jamMat = MgfLook.Lit(Jam, .58f, .02f, Jam * .11f);
            creamMat = MgfLook.Lit(MgfLook.Hex("#FFF2D6"), .42f, .01f);
            glassMat = MgfLook.Alpha(new Color(.72f, .87f, .89f, .20f));
            guideMat = MgfLook.Additive(new Color(1f, .78f, .38f, .82f), MgfLook.SoftDot);
            titleRoot = new GameObject("CocoaVitrineTitle").transform;
            gameRoot = new GameObject("CakeCuttingStage").transform;
            BuildTitleWorld(); BuildGameWorld();
        }

        void BuildTitleWorld()
        {
            MgfLook.Block("VitrineFloor", new Vector3(0, -.65f, 2.0f), new Vector3(21, 1.2f, 16), .45f, darkMat, titleRoot);
            MgfLook.Block("BackWall", new Vector3(0, 4.5f, 9.0f), new Vector3(21, 11, .6f), .22f, cocoaMat, titleRoot);
            for (int tier = 0; tier < 3; tier++)
            {
                float y = .9f + tier * 2.15f, z = 7.7f + tier * .32f;
                MgfLook.Block("Shelf" + tier, new Vector3(0, y, z), new Vector3(18.5f, .24f, 1.65f), .10f, caramelMat, titleRoot);
                for (int i = 0; i < 6; i++)
                {
                    float x = -7.3f + i * 2.9f;
                    MgfLook.Prim(PrimitiveType.Cylinder, "Cup" + tier + "_" + i, new Vector3(x, y + .45f, z), new Vector3(.38f, .38f, .38f), ivoryMat, titleRoot, false);
                    var mini = MgfLook.Prim(PrimitiveType.Cylinder, "MiniCake" + tier + "_" + i, new Vector3(x + .65f, y + .42f, z), new Vector3(.55f, .34f, .55f), i % 2 == 0 ? plumMat : jamMat, titleRoot, false);
                    mini.transform.localRotation = Quaternion.Euler(0, i * 21, 0);
                }
            }
            MgfLook.Prim(PrimitiveType.Cylinder, "CakePedestal", new Vector3(0, .05f, 1.2f), new Vector3(6.2f, .45f, 6.2f), ivoryMat, titleRoot, false);
            BuildLayerCake(titleRoot, new Vector3(0, .70f, 1.2f), 3.05f, true);
            // 둥근 구 두 개가 아니라 가운데에서 실제로 벌어지는 진열장 유리 양문이다.
            titleDoors[0] = MgfLook.Block("GlassDoorLeft", new Vector3(-1.58f, 2.72f, -.02f), new Vector3(3.02f, 4.50f, .12f), .08f, glassMat, titleRoot).transform;
            titleDoors[1] = MgfLook.Block("GlassDoorRight", new Vector3(1.58f, 2.72f, -.02f), new Vector3(3.02f, 4.50f, .12f), .08f, glassMat, titleRoot).transform;
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -3.06f : 3.06f;
                MgfLook.Block("DoorFrame" + i, new Vector3(x, 2.72f, -.08f), new Vector3(.16f, 4.72f, .22f), .05f, steelMat, titleRoot);
            }
            BuildTapir(titleRoot, new Vector3(-5.0f, -.05f, .15f), 1.12f, 16f, 0);
            BuildTapir(titleRoot, new Vector3(4.9f, -.05f, .45f), 1.18f, -18f, 1);
            BuildTapir(titleRoot, new Vector3(3.6f, -.15f, 5.25f), .86f, -150f, 2);
        }

        void BuildGameWorld()
        {
            MgfLook.Block("CocoaCounter", new Vector3(0, -.74f, .2f), new Vector3(22, 1.25f, 17), .58f, cocoaMat, gameRoot);
            for (int i = 0; i < 7; i++)
            {
                steam[i] = MgfLook.Prim(PrimitiveType.Sphere, "WarmSteam" + i,
                    new Vector3(-8f + i * 2.5f, .15f, 7.0f), new Vector3(.18f, .05f, .26f),
                    MgfLook.Additive(new Color(1f, .69f, .30f, .24f), MgfLook.SoftDot), gameRoot, false).transform;
            }
            boardRoot = new GameObject("ExactTopViewCakeBoard").transform; boardRoot.SetParent(gameRoot, false);
            BuildLayerCake(boardRoot, Vector3.zero, 3.05f, false);
            cakeRoot = boardRoot.Find("LayerCake");
            for (int i = 0; i < 24; i++)
            {
                float a = i * 15f * Mathf.Deg2Rad;
                var tick = MgfLook.Block("Tick" + i, new Vector3(Mathf.Cos(a) * 3.72f, .78f, Mathf.Sin(a) * 3.72f),
                    new Vector3(i % 3 == 0 ? .12f : .075f, .05f, i % 3 == 0 ? .48f : .34f), .025f,
                    i % 3 == 0 ? ivoryMat : caramelMat, boardRoot).transform;
                tick.localRotation = Quaternion.Euler(0, -i * 15f, 0);
            }
            var center = MgfLook.Prim(PrimitiveType.Cylinder, "CenterO", new Vector3(0, .83f, 0), new Vector3(.72f, .06f, .72f), ivoryMat, boardRoot, true);
            center.AddComponent<SphereCollider>().radius = .8f;
            oaLine = MakeLine("RadiusOA", boardRoot, Ivory, .095f);
            obLine = MakeLine("StudentCutOB", boardRoot, Plum, .13f);
            ghostPath = MakeLine("OnboardingGhostPath", boardRoot, MgfLook.Hex("#FFD27C"), .18f);
            ghostPath.positionCount = 18; ghostPath.enabled = false;

            previewMesh = new Mesh { name = "SectorPreviewMesh" };
            var preview = new GameObject("SectorPreview"); preview.transform.SetParent(boardRoot, false); preview.transform.localPosition = new Vector3(0, .80f, 0);
            previewFilter = preview.AddComponent<MeshFilter>(); previewFilter.sharedMesh = previewMesh;
            var previewRenderer = preview.AddComponent<MeshRenderer>(); previewRenderer.sharedMaterial = MgfLook.Alpha(new Color(Plum.r, Plum.g, Plum.b, .50f)); previewRenderer.shadowCastingMode = ShadowCastingMode.Off;

            serviceRoot = new GameObject("ServedLayeredSlice").transform; serviceRoot.SetParent(boardRoot, false);
            serviceTopMesh = new Mesh { name = "ServiceTopMesh" }; serviceCreamMesh = new Mesh { name = "ServiceCreamMesh" };
            serviceTopFilter = MeshObject("ChocolateTop", serviceRoot, serviceTopMesh, jamMat, new Vector3(0, .93f, 0));
            serviceCreamFilter = MeshObject("CreamLayer", serviceRoot, serviceCreamMesh, creamMat, new Vector3(0, .84f, 0));
            serviceRoot.gameObject.SetActive(false);
            seamGlow = MgfLook.Prim(PrimitiveType.Cylinder, "CreamSeamGlow", new Vector3(0, .87f, 0), new Vector3(1, .02f, 1),
                MgfLook.Additive(new Color(1f, .88f, .58f, .88f), MgfLook.SoftDot), boardRoot, false).transform;
            seamGlow.gameObject.SetActive(false);
            for (int i = 0; i < failedCuts.Length; i++)
            {
                failedCuts[i] = MakeLine("FailedCreamRidge" + i, boardRoot, MgfLook.Hex("#E7C8A0"), .075f);
                failedCuts[i].enabled = false;
            }
            guestRail = new GameObject("TapirServingRail").transform; guestRail.SetParent(gameRoot, false);
            for (int i = 0; i < 3; i++)
            {
                guests[i] = BuildTapir(guestRail, new Vector3((i - 1) * 2.7f, -.2f, 0), .56f + i * .04f, 180f, i);
                plates[i] = MgfLook.Prim(PrimitiveType.Cylinder, "GuestPlate" + i, new Vector3((i - 1) * 2.7f, .12f, -1.15f), new Vector3(1.4f, .12f, 1.4f), ivoryMat, guestRail, false).transform;
            }
            RelayoutWorld(true);
        }

        void BuildLayerCake(Transform parent, Vector3 pos, float radius, bool title)
        {
            var root = new GameObject("LayerCake").transform; root.SetParent(parent, false); root.localPosition = pos;
            float diameter = radius * 2f;
            MgfLook.Prim(PrimitiveType.Cylinder, "BottomSponge", new Vector3(0, .12f, 0), new Vector3(diameter, .28f, diameter), caramelMat, root, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "PlumJam", new Vector3(0, .31f, 0), new Vector3(diameter * .99f, .12f, diameter * .99f), jamMat, root, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "Cream", new Vector3(0, .45f, 0), new Vector3(diameter, .17f, diameter), creamMat, root, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "TopSponge", new Vector3(0, .61f, 0), new Vector3(diameter, .20f, diameter), caramelMat, root, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "ChocolateGlaze", new Vector3(0, .76f, 0), new Vector3(diameter * 1.02f, .12f, diameter * 1.02f), darkMat, root, !title);
            int berries = title ? 9 : 8;
            for (int i = 0; i < berries; i++)
            {
                float a = i * Mathf.PI * 2f / berries, rr = radius * .70f;
                MgfLook.Prim(PrimitiveType.Sphere, "PlumBerry" + i, new Vector3(Mathf.Cos(a) * rr, .96f, Mathf.Sin(a) * rr), new Vector3(.34f, .24f, .34f), plumMat, root, false);
            }
        }

        Transform BuildTapir(Transform parent, Vector3 pos, float scale, float yaw, int variant)
        {
            var root = new GameObject("OriginalTapirDiner" + variant).transform; root.SetParent(parent, false); root.localPosition = pos; root.localScale = Vector3.one * scale; root.localRotation = Quaternion.Euler(0, yaw, 0);
            var fur = MgfLook.Lit(Color.Lerp(Cocoa, DarkCocoa, .28f + variant * .06f), .22f, .01f);
            MgfLook.Prim(PrimitiveType.Sphere, "Body", new Vector3(0, 1.25f, 0), new Vector3(1.45f, 1.45f, 1.05f), fur, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "IvoryPatch", new Vector3(0, 1.28f, .76f), new Vector3(1.20f, 1.12f, .18f), ivoryMat, root, false);
            var head = MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0, 2.45f, -.18f), new Vector3(1.15f, .92f, 1.0f), fur, root, false).transform;
            // WebGL 저사양 빌드에서 CapsuleCollider 타입 생성 로그가 나는 것을 피하려고
            // 콜라이더 없는 늘인 구로 같은 짧은 주둥이 실루엣을 만든다.
            var snout = MgfLook.Prim(PrimitiveType.Sphere, "ShortSnout", new Vector3(0, 2.20f, -1.03f), new Vector3(.50f, .38f, .78f), fur, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EarL", new Vector3(-.62f, 3.12f, -.12f), new Vector3(.36f, .42f, .30f), fur, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EarR", new Vector3(.62f, 3.12f, -.12f), new Vector3(.36f, .42f, .30f), fur, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeL", new Vector3(-.33f, 2.60f, -.94f), new Vector3(.12f, .14f, .09f), darkMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeR", new Vector3(.33f, 2.60f, -.94f), new Vector3(.12f, .14f, .09f), darkMat, root, false);
            var apron = MgfLook.Block("Apron", new Vector3(0, 1.35f, -.83f), new Vector3(1.35f, 1.25f, .14f), .24f, variant == 1 ? plumMat : ivoryMat, root);
            apron.transform.localRotation = Quaternion.Euler(-4, 0, 0);
            return root;
        }

        LineRenderer MakeLine(string name, Transform parent, Color color, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(parent, false);
            line.useWorldSpace = false; line.positionCount = 2; line.widthMultiplier = width;
            line.sharedMaterial = MgfLook.Unlit(color); line.numCapVertices = 3; line.shadowCastingMode = ShadowCastingMode.Off; return line;
        }

        MeshFilter MeshObject(string name, Transform parent, Mesh mesh, Material mat, Vector3 pos)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            var f = go.AddComponent<MeshFilter>(); f.sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; return f;
        }

        void BuildUi()
        {
            roundSprite = MakeSprite(96, 20, false); ringSprite = MakeSprite(96, 0, true);
            var canvas = MgfText.Canvas;
            titleRt = R("TitleScreen", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            titleSign = Img(titleRt, "ChocolateSign", new Vector2(.5f, .71f), Vector2.zero, new Vector2(338, 150), new Color(DarkCocoa.r, DarkCocoa.g, DarkCocoa.b, .96f), roundSprite);
            Img(titleSign.transform, "SignEdge", new Vector2(.5f, .5f), Vector2.zero, new Vector2(320, 132), new Color(Caramel.r, Caramel.g, Caramel.b, .18f), ringSprite);
            var logoShadow = Txt(titleSign.transform, "한 입만", new Vector2(.5f, .64f), new Vector2(3, -4), 58, Color.black, 320); logoShadow.fontStyle = FontStyles.Bold;
            titleLogo = Txt(titleSign.transform, "한 입만", new Vector2(.5f, .64f), Vector2.zero, 58, Ivory, 320); titleLogo.fontStyle = FontStyles.Bold; titleLogo.outlineColor = Plum; titleLogo.outlineWidth = .18f;
            titleTag = Txt(titleSign.transform, "케이크를 잘라라", new Vector2(.5f, .23f), Vector2.zero, 19, Caramel, 310); titleTag.fontStyle = FontStyles.Bold;
            titleMeta = Txt(titleRt, "중학교 1학년 · 평면도형의 성질", new Vector2(.5f, .605f), Vector2.zero, 13, Ivory, 340); titleMeta.outlineColor = DarkCocoa; titleMeta.outlineWidth = .16f;
            titleBest = Txt(titleRt, "", new Vector2(.5f, .225f), Vector2.zero, 13, Ivory, 330); titleBest.outlineColor = DarkCocoa; titleBest.outlineWidth = .14f;
            titleCta = Img(titleRt, "GlassCoverTag", new Vector2(.5f, .135f), Vector2.zero, new Vector2(330, 74), Ivory, roundSprite);
            titleCtaText = Txt(titleCta.transform, "덮개 열고 자르기", new Vector2(.5f, .5f), Vector2.zero, 24, DarkCocoa, 316); titleCtaText.fontStyle = FontStyles.Bold;

            hudRt = R("GameHud", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            Img(hudRt, "TopShelf", new Vector2(.5f, .955f), Vector2.zero, new Vector2(378, 64), new Color(DarkCocoa.r, DarkCocoa.g, DarkCocoa.b, .96f), roundSprite);
            // The host mute control owns the top-right 58 px on phones. Keep all run
            // state to its left so the timer and last napkin never sit underneath it.
            scoreTxt = Txt(hudRt, "0", new Vector2(.09f, .955f), Vector2.zero, 20, Ivory, 54); scoreTxt.fontStyle = FontStyles.Bold;
            progressTxt = Txt(hudRt, "접시 0/8", new Vector2(.28f, .955f), Vector2.zero, 15, Ivory, 104); progressTxt.fontStyle = FontStyles.Bold;
            firstTxt = Txt(hudRt, "첫 제출 0/0", new Vector2(.53f, .955f), Vector2.zero, 14, Caramel, 120); firstTxt.fontStyle = FontStyles.Bold;
            timeTxt = Txt(hudRt, "120초", new Vector2(.75f, .955f), Vector2.zero, 15, Ivory, 62); timeTxt.fontStyle = FontStyles.Bold;
            napkin1 = Img(hudRt, "Napkin1", new Vector2(.63f, .905f), Vector2.zero, new Vector2(24, 18), Ivory, roundSprite);
            napkin2 = Img(hudRt, "Napkin2", new Vector2(.71f, .905f), Vector2.zero, new Vector2(24, 18), Ivory, roundSprite);
            napkin3 = Img(hudRt, "Napkin3", new Vector2(.79f, .905f), Vector2.zero, new Vector2(24, 18), Ivory, roundSprite);
            goalBg = Img(hudRt, "GoalBanner", new Vector2(.5f, .875f), Vector2.zero, new Vector2(360, 44), new Color(Plum.r, Plum.g, Plum.b, .94f), roundSprite);
            goalTxt = Txt(goalBg.transform, "주문만큼 잘라 손님에게 내세요", new Vector2(.5f, .5f), Vector2.zero, 15, Color.white, 345); goalTxt.fontStyle = FontStyles.Bold;
            promptBg = Img(hudRt, "OrderPlate", new Vector2(.5f, .795f), Vector2.zero, new Vector2(362, 78), new Color(Ivory.r, Ivory.g, Ivory.b, .97f), roundSprite); promptRt = (RectTransform)promptBg.transform;
            promptTxt = Txt(promptBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 14, DarkCocoa, 344); promptTxt.fontStyle = FontStyles.Bold;
            formulaBg = Img(hudRt, "FormulaCard", new Vector2(.5f, .115f), Vector2.zero, new Vector2(350, 72), new Color(DarkCocoa.r, DarkCocoa.g, DarkCocoa.b, .94f), roundSprite); formulaRt = (RectTransform)formulaBg.transform;
            formulaTxt = Txt(formulaBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 14, Ivory, 335); formulaTxt.fontStyle = FontStyles.Bold;
            angleRt = R("AnglePlaque", hudRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(118, 50));
            var angleBg = angleRt.gameObject.AddComponent<Image>(); angleBg.sprite = roundSprite; angleBg.color = new Color(Ivory.r, Ivory.g, Ivory.b, .96f); angleBg.raycastTarget = false;
            angleTxt = Txt(angleRt, "0°", new Vector2(.5f, .58f), Vector2.zero, 22, Plum, 108); angleTxt.fontStyle = FontStyles.Bold;
            directionTxt = Txt(angleRt, "반시계방향", new Vector2(.5f, .16f), Vector2.zero, 10, Cocoa, 105);

            revealRt = R("MathReveal", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); revealG = revealRt.gameObject.AddComponent<CanvasGroup>();
            revealBg = Img(revealRt, "RevealPlate", new Vector2(.5f, .24f), Vector2.zero, new Vector2(354, 148), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), roundSprite);
            revealTitleTxt = Txt(revealBg.transform, "", new Vector2(.5f, .70f), Vector2.zero, 23, Plum, 332); revealTitleTxt.fontStyle = FontStyles.Bold;
            revealFormulaTxt = Txt(revealBg.transform, "", new Vector2(.5f, .36f), Vector2.zero, 15, DarkCocoa, 332); revealFormulaTxt.fontStyle = FontStyles.Bold;

            toastRt = R("Toast", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); toastG = toastRt.gameObject.AddComponent<CanvasGroup>();
            Img(toastRt, "ToastBg", new Vector2(.5f, .19f), Vector2.zero, new Vector2(342, 58), new Color(DarkCocoa.r, DarkCocoa.g, DarkCocoa.b, .96f), roundSprite);
            toastTxt = Txt(toastRt, "", new Vector2(.5f, .19f), Vector2.zero, 14, Ivory, 328); toastTxt.fontStyle = FontStyles.Bold;
            rippleRt = R("InputRipple", hudRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(76, 76)); rippleImg = rippleRt.gameObject.AddComponent<Image>(); rippleImg.sprite = ringSprite; rippleImg.color = Caramel; rippleImg.raycastTarget = false; rippleRt.gameObject.SetActive(false);

            endRt = R("EndScreen", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); endG = endRt.gameObject.AddComponent<CanvasGroup>();
            Img(endRt, "EndPlate", new Vector2(.5f, .51f), Vector2.zero, new Vector2(354, 486), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), roundSprite);
            endTitleTxt = Txt(endRt, "", new Vector2(.5f, .66f), Vector2.zero, 34, Plum, 332); endTitleTxt.fontStyle = FontStyles.Bold;
            endScoreTxt = Txt(endRt, "0", new Vector2(.5f, .55f), Vector2.zero, 56, DarkCocoa, 330); endScoreTxt.fontStyle = FontStyles.Bold;
            endStatsTxt = Txt(endRt, "", new Vector2(.5f, .43f), Vector2.zero, 18, Cocoa, 320); endStatsTxt.fontStyle = FontStyles.Bold;
            var again = Img(endRt, "AgainTag", new Vector2(.5f, .30f), Vector2.zero, new Vector2(302, 72), Plum, roundSprite);
            endTapTxt = Txt(again.transform, "다시 덮개 열기", new Vector2(.5f, .5f), Vector2.zero, 22, Color.white, 288); endTapTxt.fontStyle = FontStyles.Bold;
        }

        void ResetRunVisuals()
        {
            openingAnim = revealAnim = hitStop = wrongKick = seamFlash = 0f; failedCutCount = 0;
            for (int i = 0; i < failedCuts.Length; i++) failedCuts[i].enabled = false;
            obLine.enabled = false; ghostPath.enabled = false; previewFilter.gameObject.SetActive(false); serviceRoot.gameObject.SetActive(false); seamGlow.gameObject.SetActive(false);
            for (int i = 0; i < titleDoors.Length; i++) if (titleDoors[i]) titleDoors[i].localRotation = Quaternion.identity;
        }

        void PrepareProblemVisual(CakeProblem p, bool practice)
        {
            serviceRoot.gameObject.SetActive(false); seamGlow.gameObject.SetActive(false); previewFilter.gameObject.SetActive(false); obLine.enabled = false;
            float oa = st.oaTick * 15f;
            SetRadiusLine(oaLine, oa, 3.30f); oaLine.enabled = true;
            UpdateCutPreview(0); ghostPath.enabled = practice || guideBoost > 0;
            UpdateGhostPath(practice ? 6 : 0);
        }

        void BeginCoverOpening() { titlePress = 1f; openingAnim = 0f; }

        void RefreshProblemUi()
        {
            if (current == null) return;
            promptTxt.text = current.prompt;
            if (flow == Flow.Practice)
            {
                goalTxt.text = "중심 O에서 90° 눈금까지 한 획으로 자르세요";
                formulaTxt.text = "전체 둘레 24π cm  ·  l/(2πr) = x/360";
            }
            else if (current.kind == CakeOrderKind.Arc)
            {
                goalTxt.text = "호의 길이를 전체 둘레와 비교하세요";
                formulaTxt.text = "전체 둘레 " + (2 * current.radius) + "π cm  ·  l/(2πr) = x/360";
            }
            else if (current.kind == CakeOrderKind.Area)
            {
                goalTxt.text = "넓이를 원 전체 넓이와 비교하세요";
                formulaTxt.text = "전체 넓이 " + (current.radius * current.radius) + "π cm²  ·  S/(πr²) = x/360";
            }
            else
            {
                goalTxt.text = "남길 양을 먼저 구하고 나머지를 자르세요";
                formulaTxt.text = "남길 양 + 자를 양 = 전체  ·  부분/전체 = x/360";
            }
        }

        void SetScreen()
        {
            bool title = flow == Flow.Title || flow == Flow.Opening;
            bool game = flow == Flow.Practice || flow == Flow.Playing || flow == Flow.Reveal;
            bool end = flow == Flow.End;
            titleG.alpha = title ? 1 : 0; hudG.alpha = game ? 1 : 0; revealG.alpha = flow == Flow.Reveal ? 1 : 0; endG.alpha = end ? 1 : 0;
            toastG.alpha = toastClock > 0 && game ? 1 : 0;
            titleRoot.gameObject.SetActive(title); gameRoot.gameObject.SetActive(game || end);
            if (title)
            {
                cam.orthographic = false; cam.transform.position = new Vector3(8.8f, 8.3f, -12.9f); cam.transform.LookAt(new Vector3(0, 1.55f, 1.35f)); cam.fieldOfView = 36f;
            }
            else SetTopCamera();
            RelayoutWorld(true); RelayoutUi();
        }

        void SetTopCamera()
        {
            land = MgfBridge.IsLandLayout;
            cam.orthographic = true; cam.orthographicSize = land ? 7.25f : 8.65f;
            cam.transform.position = new Vector3(0, 18f, .2f); cam.transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        void RelayoutWorld(bool force)
        {
            bool nowLand = MgfBridge.IsLandLayout;
            if (!force && Screen.width == lastW && Screen.height == lastH && nowLand == land) return;
            land = nowLand; lastW = Screen.width; lastH = Screen.height;
            boardHome = land ? new Vector3(-3.25f, 0, .05f) : new Vector3(0, 0, .60f);
            boardRoot.localPosition = boardHome;
            guestRail.localPosition = land ? new Vector3(4.45f, 0, 1.0f) : new Vector3(0, 0, -5.25f);
            if (!titleRoot.gameObject.activeSelf) SetTopCamera();
        }

        void RelayoutUi()
        {
            bool wide = MgfBridge.IsLandLayout;
            if (wide)
            {
                // 1200×630 카드처럼 낮은 가로 화면에서도 초콜릿 간판 전체와 세계 안
                // 시작 태그가 함께 보이도록 타이틀 요소를 세로 중앙대로 압축한다.
                titleSign.rectTransform.anchorMin = titleSign.rectTransform.anchorMax = new Vector2(.5f, .62f);
                titleMeta.rectTransform.anchorMin = titleMeta.rectTransform.anchorMax = new Vector2(.5f, .50f);
                titleBest.rectTransform.anchorMin = titleBest.rectTransform.anchorMax = new Vector2(.5f, .39f);
                titleCta.rectTransform.anchorMin = titleCta.rectTransform.anchorMax = new Vector2(.5f, .31f);
                goalBg.rectTransform.anchorMin = goalBg.rectTransform.anchorMax = new Vector2(.78f, .86f);
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(.78f, .69f); promptRt.sizeDelta = new Vector2(360, 112);
                formulaRt.anchorMin = formulaRt.anchorMax = new Vector2(.78f, .48f); formulaRt.sizeDelta = new Vector2(350, 92);
                revealBg.rectTransform.anchorMin = revealBg.rectTransform.anchorMax = new Vector2(.78f, .27f);
            }
            else
            {
                titleSign.rectTransform.anchorMin = titleSign.rectTransform.anchorMax = new Vector2(.5f, .71f);
                titleMeta.rectTransform.anchorMin = titleMeta.rectTransform.anchorMax = new Vector2(.5f, .605f);
                titleBest.rectTransform.anchorMin = titleBest.rectTransform.anchorMax = new Vector2(.5f, .225f);
                titleCta.rectTransform.anchorMin = titleCta.rectTransform.anchorMax = new Vector2(.5f, .135f);
                goalBg.rectTransform.anchorMin = goalBg.rectTransform.anchorMax = new Vector2(.5f, .875f);
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(.5f, .795f); promptRt.sizeDelta = new Vector2(362, 78);
                formulaRt.anchorMin = formulaRt.anchorMax = new Vector2(.5f, .115f); formulaRt.sizeDelta = new Vector2(350, 72);
                revealBg.rectTransform.anchorMin = revealBg.rectTransform.anchorMax = new Vector2(.5f, .24f);
            }
        }

        bool PointerNearCakeCenter(Vector2 p)
        {
            Vector3 c = cam.WorldToScreenPoint(boardRoot.position + new Vector3(0, .83f, 0));
            float hit = Mathf.Max(42f * Screen.width / 390f, Mathf.Min(Screen.width, Screen.height) * (flow == Flow.Practice && idleGuide > 7f ? .10f : .075f));
            return Vector2.Distance(p, new Vector2(c.x, c.y)) <= hit;
        }

        bool PointerBeyondCake(Vector2 p)
        {
            Vector3 c = cam.WorldToScreenPoint(boardRoot.position + new Vector3(0, .83f, 0));
            Vector3 edge = cam.WorldToScreenPoint(boardRoot.position + new Vector3(3.38f, .83f, 0));
            float r = Mathf.Abs(edge.x - c.x);
            return Vector2.Distance(p, new Vector2(c.x, c.y)) >= r * .94f;
        }

        int QuantizedPointerQ(Vector2 p)
        {
            Vector3 c3 = cam.WorldToScreenPoint(boardRoot.position + new Vector3(0, .83f, 0));
            Vector2 d = p - new Vector2(c3.x, c3.y);
            float deg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float oa = st.oaTick * 15f;
            float delta = Mathf.Repeat(deg - oa + 360f, 360f);
            int q = Mathf.RoundToInt(delta / 15f);
            if (q >= 24) q = 0;
            return Mathf.Clamp(q, 0, 23);
        }

        void UpdateLayoutState(State state)
        {
            if (!cam || !boardRoot) return;
            Vector3 c = cam.WorldToScreenPoint(boardRoot.position + new Vector3(0, .83f, 0));
            Vector3 e = cam.WorldToScreenPoint(boardRoot.position + new Vector3(3.38f, .83f, 0));
            state.cakeXPermille = Mathf.RoundToInt(c.x / Mathf.Max(1, Screen.width) * 1000f);
            state.cakeYPermille = Mathf.RoundToInt(c.y / Mathf.Max(1, Screen.height) * 1000f);
            state.outerRadiusPermille = Mathf.RoundToInt(Mathf.Abs(e.x - c.x) / Mathf.Max(1, Mathf.Min(Screen.width, Screen.height)) * 1000f);
        }

        void BeginDragVisual()
        {
            obLine.enabled = true; previewFilter.gameObject.SetActive(true); ghostPath.enabled = false; seamFlash = .28f;
            cakeRoot.localScale = new Vector3(1.025f, .96f, 1.025f);
        }

        void CancelDragVisual()
        {
            obLine.enabled = false; previewFilter.gameObject.SetActive(false); cakeRoot.localScale = Vector3.one;
        }

        void UpdateCutPreview(int q)
        {
            float oa = st.oaTick * 15f;
            SetRadiusLine(obLine, oa + q * 15f, 3.30f);
            UpdateSectorMesh(previewMesh, oa, q, 3.03f);
            previewFilter.gameObject.SetActive(q > 0 && st.dragging);
            angleTxt.text = (q * 15) + "°";
        }

        void SetRadiusLine(LineRenderer line, float angle, float radius)
        {
            float a = angle * Mathf.Deg2Rad; line.SetPosition(0, new Vector3(0, .84f, 0)); line.SetPosition(1, new Vector3(Mathf.Cos(a) * radius, .84f, Mathf.Sin(a) * radius));
        }

        void UpdateSectorMesh(Mesh mesh, float startDeg, int q, float radius)
        {
            if (q <= 0) { mesh.Clear(); return; }
            int steps = q + 1; var v = new Vector3[steps + 1]; var tris = new int[q * 3];
            v[0] = Vector3.zero;
            for (int i = 0; i <= q; i++)
            {
                float a = (startDeg + i * 15f) * Mathf.Deg2Rad;
                v[i + 1] = new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
                if (i < q) { tris[i * 3] = 0; tris[i * 3 + 1] = i + 1; tris[i * 3 + 2] = i + 2; }
            }
            mesh.Clear(); mesh.vertices = v; mesh.triangles = tris; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }

        void UpdateGhostPath(int q)
        {
            int target = q > 0 ? q : 6; float oa = st.oaTick * 15f, end = oa + target * 15f;
            for (int i = 0; i < ghostPath.positionCount; i++)
            {
                float t = (float)i / (ghostPath.positionCount - 1); float r = Mathf.Lerp(.1f, 3.75f, t); float a = Mathf.Lerp(oa, end, t) * Mathf.Deg2Rad;
                ghostPath.SetPosition(i, new Vector3(Mathf.Cos(a) * r, .91f, Mathf.Sin(a) * r));
            }
        }

        void BoostGuide()
        {
            guideBoost = 1f; ghostPath.enabled = true; UpdateGhostPath(flow == Flow.Practice ? 6 : 0);
        }

        void BeginRevealVisual(bool correct, bool practice, int qInput, string misconception)
        {
            revealAnim = 0f; hitStop = correct ? .07f : 0f; wrongKick = correct ? 0f : 1f; seamFlash = 1f;
            UpdateCutPreview(qInput); previewFilter.gameObject.SetActive(true); obLine.enabled = true;
            if (correct)
            {
                UpdateSectorMesh(serviceTopMesh, st.oaTick * 15f, current.qAnswer, 3.02f);
                UpdateSectorMesh(serviceCreamMesh, st.oaTick * 15f, current.qAnswer, 2.98f);
                serviceRoot.localPosition = Vector3.zero; serviceRoot.localScale = Vector3.one; serviceRoot.gameObject.SetActive(true);
                serviceStart = Vector3.zero;
                serviceEnd = land ? new Vector3(7.2f, 2.1f, -.2f) : new Vector3(0, 2.1f, -6.3f);
                seamGlow.localPosition = new Vector3(0, .88f, 0); seamGlow.localScale = new Vector3(3.2f, .02f, 3.2f); seamGlow.gameObject.SetActive(true);
                revealTitleTxt.text = practice ? "90°, 정확한 한 조각" : "주문대로 잘랐습니다";
                revealTitleTxt.color = Plum; revealFormulaTxt.text = Explain(current, practice);
                MgfSfx.Play("correct", .46f);
            }
            else
            {
                int slot = Mathf.Min(failedCuts.Length - 1, failedCutCount++);
                SetRadiusLine(failedCuts[slot], st.oaTick * 15f + qInput * 15f, 3.15f); failedCuts[slot].enabled = true;
                revealTitleTxt.text = "크림 자국을 보고 수선하세요"; revealTitleTxt.color = Cocoa;
                revealFormulaTxt.text = WrongExplanation(misconception); MgfSfx.Play("wrong", .30f);
            }
            MgfFx.Punch(cakeRoot, correct ? .08f : .04f, .24f);
        }

        string Explain(CakeProblem p, bool practice)
        {
            if (practice) return "6π/24π = 90°/360° = 1/4\n직선 두 변은 반지름, 곡선 테두리는 호";
            if (p.kind == CakeOrderKind.Arc)
                return HanIpmanRules.PiText(p.targetPi, false) + "/" + (2 * p.radius) + "π cm = " + p.qAnswer + "/24  →  " + (p.qAnswer * 15) + "°";
            if (p.kind == CakeOrderKind.Area)
                return HanIpmanRules.PiText(p.targetPi, true) + "/" + (p.radius * p.radius) + "π cm² = " + p.qAnswer + "/24  →  " + (p.qAnswer * 15) + "°";
            return "남길 " + (24 - p.qAnswer) + "/24  →  자를 " + p.qAnswer + "/24  →  " + (p.qAnswer * 15) + "°";
        }

        string WrongExplanation(string id)
        {
            switch (id)
            {
                case "radius_as_diameter": return "반지름 r이면 전체 둘레는 2πr입니다. 전체를 절반으로 잡지 마세요.";
                case "area_as_arc_total": return "넓이 주문은 전체 넓이 πr²와 비교하세요. 둘레 2πr이 아닙니다.";
                case "missing_360_normalization": return "전체 원은 360°입니다. 부분/전체 = 중심각/360°로 놓으세요.";
                case "cut_the_amount_to_leave": return "남길 양이 아니라 그 여집합을 잘라야 합니다. 360°에서 빼세요.";
                default: return "현재 각도를 전체 360°에 대한 비로 다시 비교하세요.";
            }
        }

        void CompleteRevealVisual(bool correct)
        {
            previewFilter.gameObject.SetActive(false); obLine.enabled = false; serviceRoot.gameObject.SetActive(false); seamGlow.gameObject.SetActive(false);
            cakeRoot.localPosition = Vector3.zero; cakeRoot.localScale = Vector3.one; boardRoot.localPosition = boardHome;
            SetTopCamera();
        }

        void ShowWrongReason(string id) { toastTxt.text = WrongExplanation(id); toastClock = 2.4f; toastG.alpha = 1; }

        void ShowEndVisual(string reason)
        {
            endScoreShown = 0; endScoreTxt.text = "0";
            if (reason == "clear")
            {
                endTitleTxt.text = "오늘 영업 성공"; endTitleTxt.color = Plum;
                endStatsTxt.text = "접시 8개 완성\n첫 제출 " + st.firstCorrect + "/" + st.firstAttempted + "  ·  냅킨 " + st.lives + "장";
            }
            else
            {
                endTitleTxt.text = reason == "timer" ? "영업 시간이 끝났습니다" : (reason == "mastery" ? "첫 제출 기준 미달" : "깨끗한 냅킨이 없습니다");
                endTitleTxt.color = Cocoa;
                endStatsTxt.text = "완성 접시 " + st.solved + "/8\n첫 제출 " + st.firstCorrect + "/" + Math.Max(1, st.firstAttempted) + "\n호·넓이·남길 양을 구분해 다시 도전하세요";
            }
        }

        void RippleAtScreen(Vector2 screen, string msg)
        {
            float sx = screen.x / Mathf.Max(1, Screen.width) * 390f - 195f;
            float sy = screen.y / Mathf.Max(1, Screen.height) * 844f - 422f;
            rippleRt.anchoredPosition = new Vector2(sx, sy); rippleRt.localScale = Vector3.one * .28f;
            rippleImg.color = Caramel; rippleClock = .46f; rippleRt.gameObject.SetActive(true);
            toastTxt.text = msg; toastClock = 1.2f; toastG.alpha = 1;
        }

        void UpdateWorld(float dt)
        {
            worldClock += dt; RelayoutWorld(false);
            for (int i = 0; i < steam.Length; i++) if (steam[i])
            {
                float u = (worldClock * .16f + i * .13f) % 1f;
                steam[i].localPosition = new Vector3(-8f + i * 2.5f + Mathf.Sin(worldClock + i) * .16f, .12f + u * .42f, 7f);
                steam[i].localScale = Vector3.one * (.12f + u * .20f);
            }
            for (int i = 0; i < guests.Length; i++) if (guests[i])
            {
                float bob = Mathf.Sin(worldClock * (1.2f + i * .08f) + i) * .055f;
                var p = guests[i].localPosition; p.y = -.2f + bob; guests[i].localPosition = p;
            }
            if (flow == Flow.Title)
            {
                float p = 1f + .018f * Mathf.Sin(worldClock * 1.35f) + titlePress * .07f;
                titleCta.rectTransform.localScale = Vector3.one * p; titleLogo.transform.localScale = Vector3.one * (1f + .012f * Mathf.Sin(worldClock));
                titlePress = Mathf.Max(0, titlePress - dt * 5f);
            }
            if (flow == Flow.Opening)
            {
                openingAnim = Mathf.Clamp01(openingAnim + dt * 3.2f); float e = 1f - Mathf.Pow(1f - openingAnim, 3f);
                titleDoors[0].localRotation = Quaternion.Euler(0, -82f * e, 8f * e); titleDoors[1].localRotation = Quaternion.Euler(0, 82f * e, -8f * e);
                titleSign.transform.localScale = Vector3.one * (1f - e * .06f); titleG.alpha = 1f - e * .30f;
            }
            if (guideBoost > 0) guideBoost = Mathf.Max(0, guideBoost - dt * .55f);
            if (ghostPath.enabled)
            {
                float pulse = 1f + .12f * Mathf.Sin(worldClock * 5f) + guideBoost * .25f; ghostPath.widthMultiplier = .15f * pulse;
                if (flow != Flow.Practice && guideBoost <= 0) ghostPath.enabled = false;
            }
            if (hitStop > 0) { hitStop = Mathf.Max(0, hitStop - dt); return; }
            if (flow == Flow.Reveal && revealCorrect)
            {
                revealAnim = Mathf.Min(1, revealAnim + dt * 1.55f); float t = 1f - Mathf.Pow(1f - revealAnim, 3f);
                serviceRoot.localPosition = Vector3.Lerp(serviceStart, serviceEnd, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 2.1f;
                float squash = 1f + .20f * Mathf.Sin(t * Mathf.PI); serviceRoot.localScale = new Vector3(1f / squash, squash, 1f / squash);
                float tilt = Mathf.Sin(t * Mathf.PI); cam.transform.position = new Vector3(0, 18f - tilt * 3.4f, -tilt * 5.2f); cam.transform.LookAt(new Vector3(0, 0, 0));
                if (st.combo >= 3)
                {
                    for (int i = 0; i < plates.Length; i++) { var p = plates[i].localPosition; p.y = .12f + Mathf.Sin(Mathf.Clamp01(t * 1.6f - i * .12f) * Mathf.PI) * .75f; plates[i].localPosition = p; }
                }
            }
            else if (flow == Flow.Reveal && !revealCorrect)
            {
                revealAnim = Mathf.Min(1, revealAnim + dt * 2.2f); float k = 1f - revealAnim;
                boardRoot.localPosition = boardHome + new Vector3(Mathf.Sin(revealAnim * Mathf.PI * 7f) * .16f * k, 0, 0);
            }
            seamFlash = Mathf.Max(0, seamFlash - dt * 2.8f);
            if (seamGlow.gameObject.activeSelf) seamGlow.localScale = Vector3.one * (2.5f + seamFlash * 1.2f);
        }

        void UpdateUi(float dt)
        {
            if (Screen.width != lastW || Screen.height != lastH) { RelayoutWorld(true); RelayoutUi(); }
            if (current != null && (flow == Flow.Practice || flow == Flow.Playing || flow == Flow.Reveal))
            {
                Vector3 s = cam.WorldToScreenPoint(boardRoot.position + new Vector3(0, .83f, 0));
                angleRt.anchoredPosition = new Vector2(s.x / Mathf.Max(1, Screen.width) * 390f - 195f, s.y / Mathf.Max(1, Screen.height) * 844f - 422f + 72f);
            }
            scoreShown = Mathf.MoveTowards(scoreShown, st.score, Mathf.Max(100, Mathf.Abs(st.score - scoreShown) * 6f) * dt);
            int sc = Mathf.RoundToInt(scoreShown); if (sc != shownScore) { shownScore = sc; scoreTxt.text = sc.ToString(); scoreTxt.transform.localScale = Vector3.one * (1f + .08f * Mathf.Sin(Mathf.Clamp01(Mathf.Abs(st.score - scoreShown) / 80f) * Mathf.PI)); }
            progressTxt.text = "접시 " + st.solved + "/8"; firstTxt.text = "첫 제출 " + st.firstCorrect + "/" + st.firstAttempted;
            int sec = Mathf.Max(0, Mathf.CeilToInt(timeLeft)); if (sec != shownSecond) { shownSecond = sec; timeTxt.text = flow == Flow.Practice ? "연습" : sec + "초"; }
            napkin1.color = st.lives >= 1 ? Ivory : new Color(.35f, .22f, .18f, .45f);
            napkin2.color = st.lives >= 2 ? Ivory : new Color(.35f, .22f, .18f, .45f);
            napkin3.color = st.lives >= 3 ? Ivory : new Color(.35f, .22f, .18f, .45f);
            if (toastClock > 0) { toastClock = Mathf.Max(0, toastClock - dt); toastG.alpha = Mathf.Min(1, toastClock * 5f); }
            if (rippleClock > 0)
            {
                rippleClock = Mathf.Max(0, rippleClock - dt); float u = 1f - rippleClock / .46f;
                rippleRt.localScale = Vector3.one * Mathf.Lerp(.28f, 1.7f, u); rippleImg.color = new Color(Caramel.r, Caramel.g, Caramel.b, 1f - u);
                if (rippleClock <= 0) rippleRt.gameObject.SetActive(false);
            }
            if (flow == Flow.Reveal)
            {
                float p = 1f + .07f * Mathf.Sin(Mathf.Clamp01(revealClock / .32f) * Mathf.PI); revealBg.transform.localScale = Vector3.one * p;
            }
            if (flow == Flow.End)
            {
                endScoreShown = Mathf.MoveTowards(endScoreShown, st.score, Mathf.Max(160, st.score * 1.8f) * dt); endScoreTxt.text = Mathf.RoundToInt(endScoreShown).ToString();
                endScoreTxt.transform.localScale = Vector3.one * (1f + .045f * Mathf.Sin(worldClock * 2.5f));
            }
            int best = PlayerPrefs.GetInt("han-ipman-best", 0); titleBest.text = best > 0 ? "최고 " + best + "점  ·  8접시 영업" : "8접시 · 120초 영업";
        }
    }
}
