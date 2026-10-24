// 나비 재봉소 — 빛이 스미는 고치 섬유 터널, 수학 평면, UI와 피드백.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.NabiJaebongso
{
    public sealed partial class NabiJaebongsoGame
    {
        static readonly Color SkyBlue = MgfLook.Hex("#BFE8E2");
        static readonly Color SkyLight = MgfLook.Hex("#FFF0D8");
        static readonly Color Ivory = MgfLook.Hex("#FFF8EA");
        static readonly Color IvoryShadow = MgfLook.Hex("#E9D8EF");
        static readonly Color Ultramarine = MgfLook.Hex("#31536B");
        static readonly Color Saffron = MgfLook.Hex("#EF7C68");
        static readonly Color Ink = MgfLook.Hex("#243647");
        static readonly Color Brass = MgfLook.Hex("#59AFA8");
        static readonly Color Ghost = new Color(.95f, .98f, 1f, .90f);
        static readonly Color SilkRose = MgfLook.Hex("#F3A8B5");
        static readonly Color Lavender = MgfLook.Hex("#B9A7DF");

        Camera cam;
        Transform worldRoot, boardRoot, sourcePanel, targetPanel, activeButterfly, butterflyBody, wingLeft, wingRight, titleRelief;
        Transform hinge, pencil, guideHand, activeCocoon, membrane;
        readonly Transform[] sourcePins = new Transform[3];
        readonly Transform[] targetPins = new Transform[3];
        readonly Transform[] cocoons = new Transform[12];
        readonly Transform[] flowers = new Transform[18];
        readonly Transform[] sentButterflies = new Transform[NabiRules.TargetWings];
        readonly Transform[] antennae = new Transform[2];
        readonly Transform[] brassPins = new Transform[3];
        readonly Transform[] practiceMarkers = new Transform[2];
        readonly Vector3[] cocoonHomes = new Vector3[12];
        readonly Vector3[] sentHomes = new Vector3[NabiRules.TargetWings];
        readonly Vector3[] uiCorners = new Vector3[4];
        readonly Vector2[] sourcePoints = new Vector2[3];

        LineRenderer sourceTriangle, targetBase, sourceDragLine, compassLeg, guidePath, borderTrail;
        readonly LineRenderer[] arcLines = new LineRenderer[2];
        readonly LineRenderer[] segmentLines = new LineRenderer[2];
        readonly LineRenderer[] ghostLines = new LineRenderer[2];
        TextMeshPro[] sourceLabels = new TextMeshPro[3];
        TextMeshPro[] targetLabels = new TextMeshPro[3];
        TextMeshPro[] sourceEdgeLabels = new TextMeshPro[3];

        Material ivoryMat, ivorySoftMat, blueMat, blueGlowMat, saffronMat, inkMat, brassMat, bodyMat, wingMat, seamMat, ghostMat;
        Material flowerMat, silkMat;
        Mesh membraneMesh;
        Vector2 sourceOffset, targetOffset;
        Vector2 targetLeftLocal, targetRightLocal, hingeHomeLocal, hingeLocal, pencilLocal;
        float geometryScale = .42f;
        bool landscape, portraitTablet;
        float lastLayoutW = -1, lastLayoutH = -1;
        float worldClock, revealVisualClock, refuseClock, toastClock, guideBoost;
        bool revealVisualActive, revealVisualCorrect;
        Vector2 revealCandidate;
        Vector3 cameraHome;

        CanvasGroup titleGroup, hudGroup, endGroup, helpGroup, toastGroup;
        RectTransform canvasRt, titleArtRt, titleArchRt, titleCtaRt, helpLinkRt, compassButtonRt, rulerButtonRt;
        RectTransform headerRt, goalRt, promptRt, toolDockRt, endRt, helpRt, toastRt, rippleRt;
        RawImage titleArt;
        Image titleArch, titleCta, headerBg, goalBg, promptBg, compassButton, rulerButton;
        Image rippleImg, toastBg;
        TextMeshProUGUI titleShadow, titleLogo, titleTag, titleMeta, titleBest, titleStart, helpLink;
        TextMeshProUGUI scoreText, progressText, timeText, livesText, goalText, promptText, toolHintText, feedbackText;
        TextMeshProUGUI endTitle, endScore, endStats, endTap, helpTitle, helpBody, helpTap, toastText;
        readonly TextMeshProUGUI[] screenSourceLabels = new TextMeshProUGUI[3];
        readonly TextMeshProUGUI[] screenTargetLabels = new TextMeshProUGUI[2];
        readonly TextMeshProUGUI[] screenEdgeLabels = new TextMeshProUGUI[3];
        Image titleFrame;

        // 타이틀 시연막: 실제 작도 순서(원호 둘 → 교점 → 두 선 → 실크막 성장)를 반복한다.
        LineRenderer demoBase, demoLeg, demoSegL, demoSegR;
        readonly LineRenderer[] demoArcs = new LineRenderer[2];
        readonly Transform[] demoPins = new Transform[3];
        Transform demoMembrane, demoPencil;
        static readonly Vector2 DemoLeft = new Vector2(-1.3f, -1.2f);
        static readonly Vector2 DemoRight = new Vector2(1.3f, -1.2f);
        const float DemoR0 = 2.3f, DemoR1 = 1.9f, DemoLoop = 5.2f;
        Vector2 demoApex;
        const float PortraitBaseY = -4.5f;
        Sprite roundSprite, softOvalSprite, ringSprite, diamondSprite;
        float displayedScore, displayedEndScore, titleBreath;
        int shownSecond = -1;

        static readonly Vector3 HingeScale = new Vector3(.46f, .46f, .18f);
        Vector3 ShuttleHome => landscape ? new Vector3(targetOffset.x + 3.35f, targetOffset.y + 2.25f, -.55f) :
            new Vector3(2.7f, PortraitTargetBaseY - 1.1f, -.55f);
        float ShuttleScale => landscape ? .6f : .5f;
        float PortraitTargetBaseY => portraitTablet ? -4.15f : PortraitBaseY;
        Vector2 TargetLeftLocal => targetLeftLocal;
        Vector2 TargetRightLocal => targetRightLocal;
        Vector2 HingeHomeLocal => hingeHomeLocal;
        Vector2 HingeLocal => hingeLocal;
        Vector2 PencilLocal => pencilLocal;

        void BuildMaterials()
        {
            ivoryMat = MgfLook.Alpha(new Color(1f, .965f, .88f, .94f));
            ivorySoftMat = MgfLook.Alpha(new Color(.88f, .78f, .94f, .78f));
            blueMat = MgfLook.Lit(Ultramarine, .42f, .02f);
            blueGlowMat = MgfLook.Lit(MgfLook.Hex("#73D8CC"), .54f, .08f, MgfLook.Hex("#2A9F9B") * .24f);
            saffronMat = MgfLook.Lit(Saffron, .58f, .02f);
            inkMat = MgfLook.Lit(Ink, .12f, .01f);
            brassMat = MgfLook.Lit(Brass, .72f, .04f, Brass * .08f);
            bodyMat = MgfLook.Lit(MgfLook.Hex("#F2A09B"), .64f, .03f);
            wingMat = MgfLook.Alpha(new Color(.72f, .94f, .91f, .72f));
            seamMat = MgfLook.Unlit(Ultramarine);
            ghostMat = MgfLook.Alpha(Ghost);
            flowerMat = MgfLook.Lit(Lavender, .52f, .02f);
            silkMat = MgfLook.Alpha(new Color(1f, .83f, .82f, .66f));
        }

        void BuildWorld()
        {
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<SphereCollider>();
            Destroy(colliderTypes);

            MgfLook.Sky(MgfLook.Hex("#8ACFC8"), SkyBlue, SkyLight, .96f);
            MgfLook.Sun(new Vector3(43, -28, 12), MgfLook.Hex("#FFF1CC"), 1.20f, .58f);
            cam = MgfLook.Camera(new Vector3(0, 6.8f, -18f), new Vector3(0, .5f, 1.1f), 34f);
            cam.orthographic = true;
            cam.orthographicSize = 8.4f;
            cameraHome = cam.transform.position;

            worldRoot = new GameObject("LuminousCocoonFiberTunnel").transform;
            BuildSilkSky();
            BuildCocoonsAndFlowers();
            BuildTitlePressRelief();
            BuildSentButterflies();

            boardRoot = new GameObject("CameraParallelConstructionPlane").transform;
            boardRoot.position = new Vector3(0, .45f, .3f);
            boardRoot.rotation = cam.transform.rotation;
            sourcePanel = MakeSilkPanel("SourceSilkMembrane", ivoryMat, boardRoot);
            sourcePanel.localPosition = new Vector3(0, 0, .28f);
            sourcePanel.localScale = new Vector3(6.7f, 3.65f, 1f);
            targetPanel = MakeSilkPanel("TargetSilkMembrane", ivorySoftMat, boardRoot);
            targetPanel.localPosition = new Vector3(0, -3.5f, .28f);
            targetPanel.localScale = new Vector3(6.7f, 4.6f, 1f);

            sourceTriangle = MakeLine("OriginalTriangle", seamMat, .095f, 4, boardRoot);
            targetBase = MakeLine("GivenBase", MgfLook.Unlit(Ink), .12f, 2, boardRoot);
            sourceDragLine = MakeLine("CompassWidthDrag", MgfLook.Unlit(Saffron), .11f, 2, boardRoot);
            sourceDragLine.gameObject.SetActive(false);
            compassLeg = MakeLine("PorcelainCompassLeg", MgfLook.Unlit(Brass), .085f, 3, boardRoot);
            guidePath = MakeLine("OnboardingSilkPath", MgfLook.Alpha(new Color(.82f, .45f, .12f, .55f)), .20f, 24, boardRoot);
            guidePath.gameObject.SetActive(false);
            borderTrail = MakeLine("AnimatedThreadBorder", MgfLook.Additive(new Color(.47f, .98f, .88f, .8f)), .075f, 20, boardRoot);
            borderTrail.gameObject.SetActive(false);
            for (int i = 0; i < 2; i++)
            {
                arcLines[i] = MakeLine("StudentArc" + i, MgfLook.Unlit(new Color(.10f, .17f, .25f, .92f)), .065f, 64, boardRoot);
                arcLines[i].gameObject.SetActive(false);
                segmentLines[i] = MakeLine("StudentSegment" + i, MgfLook.Unlit(Ink), .105f, 2, boardRoot);
                segmentLines[i].gameObject.SetActive(false);
                ghostLines[i] = MakeLine("CorrectGhost" + i, ghostMat, .075f, 2, boardRoot);
                ghostLines[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < 3; i++)
            {
                sourcePins[i] = MgfLook.Prim(PrimitiveType.Sphere, "SourcePin" + i, Vector3.zero,
                    new Vector3(.28f, .28f, .13f), inkMat, boardRoot, false).transform;
                targetPins[i] = MgfLook.Prim(PrimitiveType.Sphere, "TargetPin" + i, Vector3.zero,
                    new Vector3(.31f, .31f, .15f), i < 2 ? inkMat : brassMat, boardRoot, false).transform;
                sourceLabels[i] = WorldLabel("ABC"[i].ToString(), 1.2f, Ink);
                targetLabels[i] = WorldLabel("DEF"[i].ToString(), 1.2f, Ink);
                sourceLabels[i].gameObject.SetActive(false);
                targetLabels[i].gameObject.SetActive(false);
                brassPins[i] = MgfLook.Prim(PrimitiveType.Cylinder, "CeramicStitchKnot" + i, Vector3.zero,
                    new Vector3(.18f, .07f, .18f), brassMat, boardRoot, false).transform;
                brassPins[i].localRotation = Quaternion.Euler(90, 0, 0);
                brassPins[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < 3; i++)
            {
                sourceEdgeLabels[i] = WorldLabel("", .72f, Ultramarine);
                sourceEdgeLabels[i].gameObject.SetActive(false); // 화면 공간 라벨(screenEdgeLabels)로 대체 — 월드 글자는 판독 불가 크기였다.
            }

            hinge = MgfLook.Prim(PrimitiveType.Sphere, "PorcelainCompassHinge", Vector3.zero,
                HingeScale, brassMat, boardRoot, false).transform;
            pencil = MgfLook.Prim(PrimitiveType.Cylinder, "CoralThreadNib", Vector3.zero,
                new Vector3(.16f, .38f, .16f), saffronMat, boardRoot, false).transform;
            pencil.localRotation = Quaternion.Euler(90, 0, 0);
            guideHand = MgfLook.Prim(PrimitiveType.Sphere, "GuideFinger", Vector3.zero,
                new Vector3(.36f, .36f, .12f), MgfLook.Unlit(new Color(1f, .93f, .62f)), boardRoot, false).transform;
            guideHand.gameObject.SetActive(false);

            for (int i = 0; i < practiceMarkers.Length; i++)
            {
                practiceMarkers[i] = MgfLook.Block("PracticeEndpoint" + i, Vector3.zero,
                    new Vector3(.72f, .72f, .08f), .16f, saffronMat, boardRoot).transform;
                practiceMarkers[i].localRotation = Quaternion.Euler(0, 0, 45f);
                practiceMarkers[i].gameObject.SetActive(false);
            }

            BuildActiveButterfly();
            BuildMembrane();
            // 작도면 위 점·손잡이의 그림자가 어긋난 두 번째 점처럼 보여 꼭짓점을 헷갈리게 했다.
            foreach (var r in boardRoot.GetComponentsInChildren<Renderer>(true))
                if (r.transform != sourcePanel && r.transform != targetPanel) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        void BuildSilkSky()
        {
            // 빛이 통과하는 고치 안쪽을 굵은 비정형 리본과 겹친 장력 고리로 만든다.
            // 직교 격자·계기실 선을 피하고, 화면 가장자리에서 중앙으로 감싸는 터널 실루엣을 낸다.
            for (int row = 0; row < 11; row++)
            {
                Material ribbon = row % 3 == 0 ? MgfLook.Alpha(new Color(.46f, .88f, .82f, .55f)) :
                    row % 3 == 1 ? MgfLook.Alpha(new Color(.96f, .58f, .66f, .48f)) :
                    MgfLook.Alpha(new Color(.70f, .61f, .90f, .42f));
                var line = MakeLine("BreathingSilkRibbon" + row, ribbon, .12f + row % 3 * .035f, 17, worldRoot);
                for (int i = 0; i < 17; i++)
                {
                    float x = -16f + i * 2f;
                    float u = (i - 8f) / 8f;
                    float sideCurl = Mathf.Sign(u) * u * u * (1.6f + row * .08f);
                    float y = -7.2f + row * 1.58f + (1f - u * u) * (2.4f + row * .05f) + sideCurl;
                    float z = 5.2f + row * .85f + Mathf.Abs(u) * 1.8f;
                    line.SetPosition(i, new Vector3(x, y, z));
                }
            }
            for (int ring = 0; ring < 7; ring++)
            {
                var loop = MakeLine("SoftTensionLoop" + ring,
                    ring % 2 == 0 ? MgfLook.Alpha(new Color(.98f, .82f, .72f, .55f)) :
                    MgfLook.Alpha(new Color(.55f, .91f, .86f, .55f)), .07f, 49, worldRoot);
                loop.loop = true;
                float cx = (ring - 3f) * 3.8f;
                float cy = -1.8f + (ring % 3) * 3.3f;
                for (int k = 0; k < 49; k++)
                {
                    float a = k * Mathf.PI * 2f / 48f;
                    loop.SetPosition(k, new Vector3(cx + Mathf.Cos(a) * (2.2f + ring * .12f),
                        cy + Mathf.Sin(a) * (3.9f - ring * .16f), 8.5f + ring * .7f));
                }
            }
        }

        void BuildCocoonsAndFlowers()
        {
            for (int i = 0; i < cocoons.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (5.0f + (i % 4) * 1.32f);
                float y = -1.8f + (i % 6) * 1.85f;
                float z = 3.8f + (i % 3) * 1.7f;
                var root = new GameObject("CeramicTensionBobbin" + i).transform;
                root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(x, y, z);
                cocoonHomes[i] = root.localPosition;
                var core = MgfLook.Prim(PrimitiveType.Capsule, "PorcelainBobbinCore", Vector3.zero,
                    new Vector3(.42f + i % 3 * .05f, .66f, .42f), i % 3 == 0 ? brassMat : flowerMat, root, false);
                core.transform.localRotation = Quaternion.Euler(0, i * 17f, side * (22f + i % 4 * 6f));
                for (int wrap = 0; wrap < 4; wrap++)
                {
                    var thread = MakeLine("SoftThreadWrap", wrap % 2 == 0 ? silkMat : MgfLook.Alpha(new Color(.96f, .55f, .62f, .65f)), .045f, 25, root);
                    thread.loop = true;
                    float yBand = -.30f + wrap * .20f;
                    for (int k = 0; k < 25; k++)
                    {
                        float a = k * Mathf.PI * 2f / 24f;
                        thread.SetPosition(k, new Vector3(Mathf.Cos(a) * .38f, yBand, Mathf.Sin(a) * .38f));
                    }
                }
            }
            for (int i = 0; i < flowers.Length; i++)
            {
                float a = i * Mathf.PI * 2f / flowers.Length;
                float r = 6.3f + (i % 4) * .9f;
                var root = new GameObject("SilkDewLantern" + i).transform;
                root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(Mathf.Cos(a) * r * 1.2f, -3.4f + (i % 6) * 1.5f, 7.5f + Mathf.Abs(Mathf.Sin(a)) * 3f + (i % 3));
                flowers[i] = root;
                var dew = MgfLook.Prim(PrimitiveType.Sphere, "TranslucentDew", Vector3.zero,
                    new Vector3(.20f + i % 3 * .04f, .27f + i % 2 * .04f, .16f), i % 2 == 0 ? brassMat : saffronMat, root, false);
                dew.transform.localRotation = Quaternion.Euler(i * 11f, i * 23f, 0);
                var tail = MakeLine("DewTail", silkMat, .035f, 4, root);
                tail.SetPosition(0, Vector3.zero);
                tail.SetPosition(1, new Vector3(.15f, -.38f, .08f));
                tail.SetPosition(2, new Vector3(-.12f, -.72f, .04f));
                tail.SetPosition(3, new Vector3(.08f, -1.05f, 0));
            }
        }

        void BuildSentButterflies()
        {
            for (int i = 0; i < sentButterflies.Length; i++)
            {
                sentButterflies[i] = MakeMiniButterfly("PressedWingSeal" + i, worldRoot);
                float a = 25f + i * 27f;
                sentHomes[i] = new Vector3(-4.7f + i * 1.75f, 4.2f + Mathf.Sin(i * 1.6f) * .9f, 3.2f + i % 3);
                sentButterflies[i].localPosition = sentHomes[i];
                sentButterflies[i].localRotation = Quaternion.Euler(0, a, i % 2 == 0 ? -8 : 8);
                sentButterflies[i].gameObject.SetActive(false);
            }
        }

        void BuildTitlePressRelief()
        {
            // 타이틀 중앙의 살아 있는 실크 창. 게임과 같은 작도 순서가 반복된다:
            // 왼쪽 원호 → 오른쪽 원호 → 교점까지 두 선 → 막이 자라고 세 봉합매듭이 맺힌다.
            titleRelief = new GameObject("TitleConstructionDemo").transform;
            titleRelief.SetParent(worldRoot, false);
            titleRelief.position = new Vector3(0, .1f, 1.6f);
            titleRelief.rotation = cam.transform.rotation;
            var silkWindow = MakeSilkPanel("DemoSilkWindow", ivoryMat, titleRelief);
            silkWindow.localScale = new Vector3(6.35f, 4.75f, 1f);
            for (int i = 0; i < 5; i++)
            {
                float a = (-125f + i * 62.5f) * Mathf.Deg2Rad;
                var knot = MgfLook.Prim(PrimitiveType.Sphere, "DemoStitchKnot", new Vector3(Mathf.Cos(a) * 3.0f, Mathf.Sin(a) * 2.18f, -.14f),
                    new Vector3(.18f, .18f, .10f), i % 2 == 0 ? saffronMat : brassMat, titleRelief, false);
                knot.transform.localRotation = Quaternion.Euler(0, 0, i * 21f);
            }
            float d = Vector2.Distance(DemoLeft, DemoRight);
            float ax = (DemoR0 * DemoR0 - DemoR1 * DemoR1 + d * d) / (2f * d);
            demoApex = DemoLeft + new Vector2(ax, Mathf.Sqrt(Mathf.Max(0f, DemoR0 * DemoR0 - ax * ax)));

            demoBase = MakeLine("DemoBase", MgfLook.Unlit(Ink), .11f, 2, titleRelief);
            demoBase.SetPosition(0, V3(DemoLeft, -.16f));
            demoBase.SetPosition(1, V3(DemoRight, -.16f));
            for (int i = 0; i < 2; i++)
            {
                demoArcs[i] = MakeLine("DemoArc" + i, MgfLook.Unlit(new Color(.10f, .17f, .25f, .95f)), .07f, 40, titleRelief);
                demoArcs[i].gameObject.SetActive(false);
            }
            demoSegL = MakeLine("DemoSegL", MgfLook.Unlit(Ink), .10f, 2, titleRelief);
            demoSegR = MakeLine("DemoSegR", MgfLook.Unlit(Ink), .10f, 2, titleRelief);
            demoLeg = MakeLine("DemoCompassLeg", MgfLook.Unlit(Brass), .09f, 3, titleRelief);
            demoPencil = MgfLook.Prim(PrimitiveType.Cylinder, "DemoPencil", Vector3.zero,
                new Vector3(.16f, .30f, .16f), saffronMat, titleRelief, false).transform;
            demoPencil.localRotation = Quaternion.Euler(90, 0, 0);

            var go = new GameObject("DemoMembrane");
            go.transform.SetParent(titleRelief, false);
            var mesh = new Mesh { name = "DemoMembraneMesh" };
            mesh.vertices = new[] { V3(DemoLeft, -.14f), V3(DemoRight, -.14f), V3(demoApex, -.14f) };
            mesh.triangles = new[] { 0, 2, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = wingMat;
            demoMembrane = go.transform;
            for (int i = 0; i < 3; i++)
            {
                Vector2 at = i == 0 ? DemoLeft : i == 1 ? DemoRight : demoApex;
                demoPins[i] = MgfLook.Prim(PrimitiveType.Sphere, "DemoStitchKnot" + i, V3(at, -.24f),
                    new Vector3(.26f, .07f, .26f), brassMat, titleRelief, false).transform;
            }
            foreach (var r in titleRelief.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
            UpdateTitleDemo(0f);
        }

        void UpdateTitleDemo(float t)
        {
            // t: 0~DemoLoop 반복. 할당 없이 위치만 갱신한다.
            float k0 = Mathf.Clamp01(t / 1.1f);
            float k1 = Mathf.Clamp01((t - 1.2f) / 1.1f);
            float ks = Mathf.Clamp01((t - 2.4f) / .6f);
            float km = Mathf.Clamp01((t - 3.05f) / .45f);
            float fade = t > DemoLoop - .45f ? 0f : 1f;
            float a0 = Mathf.Atan2(demoApex.y - DemoLeft.y, demoApex.x - DemoLeft.x);
            float a1 = Mathf.Atan2(demoApex.y - DemoRight.y, demoApex.x - DemoRight.x);
            DrawDemoArc(0, DemoLeft, DemoR0, a0 - .55f, a0 + .45f, k0 * fade);
            DrawDemoArc(1, DemoRight, DemoR1, a1 + .55f, a1 - .45f, k1 * fade);

            bool drawing0 = k0 > 0f && k0 < 1f, drawing1 = k1 > 0f && k1 < 1f;
            Vector2 c = drawing1 || (k0 >= 1f && k1 > 0f) ? DemoRight : DemoLeft;
            float r = c == DemoRight ? DemoR1 : DemoR0;
            float ang = c == DemoRight ? Mathf.Lerp(a1 + .55f, a1 - .45f, k1) : Mathf.Lerp(a0 - .55f, a0 + .45f, k0);
            Vector2 tip = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            bool legOn = fade > 0f && t < 2.4f;
            demoLeg.gameObject.SetActive(legOn);
            demoPencil.gameObject.SetActive(legOn);
            if (legOn)
            {
                Vector2 knee = (c + tip) * .5f + new Vector2(0f, .55f);
                demoLeg.SetPosition(0, V3(c, -.30f));
                demoLeg.SetPosition(1, V3(knee, -.42f));
                demoLeg.SetPosition(2, V3(tip, -.30f));
                demoPencil.localPosition = V3(tip, -.34f);
                demoPencil.localScale = new Vector3(.16f, .30f, .16f) * (drawing0 || drawing1 ? 1.15f : 1f);
            }

            bool segOn = ks > 0f && fade > 0f;
            demoSegL.gameObject.SetActive(segOn);
            demoSegR.gameObject.SetActive(segOn);
            if (segOn)
            {
                demoSegL.SetPosition(0, V3(DemoLeft, -.20f));
                demoSegL.SetPosition(1, V3(Vector2.Lerp(DemoLeft, demoApex, ks), -.20f));
                demoSegR.SetPosition(0, V3(DemoRight, -.20f));
                demoSegR.SetPosition(1, V3(Vector2.Lerp(DemoRight, demoApex, ks), -.20f));
            }
            bool memOn = km > 0f && fade > 0f;
            demoMembrane.gameObject.SetActive(memOn);
            if (memOn)
            {
                float over = 1f + Mathf.Sin(km * Mathf.PI) * .06f;
                Vector3 pivot = V3((DemoLeft + DemoRight + demoApex) / 3f, 0f);
                demoMembrane.localScale = Vector3.one * (km * over);
                demoMembrane.localPosition = pivot * (1f - km * over);
            }
            for (int i = 0; i < 3; i++)
            {
                float pk = Mathf.Clamp01((t - 3.2f - i * .16f) / .18f);
                bool on = pk > 0f && fade > 0f;
                demoPins[i].gameObject.SetActive(on);
                if (on) demoPins[i].localScale = new Vector3(.26f, .07f, .26f) * (1f + (1f - pk) * .8f);
            }
        }

        void DrawDemoArc(int i, Vector2 c, float r, float from, float to, float k)
        {
            var line = demoArcs[i];
            line.gameObject.SetActive(k > 0f);
            if (k <= 0f) return;
            float end = Mathf.Lerp(from, to, k);
            for (int j = 0; j < 40; j++)
            {
                float a = Mathf.Lerp(from, end, j / 39f);
                line.SetPosition(j, V3(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, -.18f));
            }
        }

        Transform MakeMiniButterfly(string name, Transform parent)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var left = MakeTriangle("WovenWingLeft", new Color(.52f, .89f, .82f, .78f), root);
            left.localPosition = new Vector3(-.10f, -.18f, .02f);
            left.localScale = new Vector3(-.48f, .48f, .48f);
            var right = MakeTriangle("WovenWingRight", new Color(.96f, .61f, .67f, .72f), root);
            right.localPosition = new Vector3(.10f, -.18f, .02f);
            right.localScale = Vector3.one * .48f;
            MgfLook.Prim(PrimitiveType.Sphere, "SilkKnot", new Vector3(0, .03f, -.05f),
                new Vector3(.16f, .28f, .12f), brassMat, root, false);
            return root;
        }

        void BuildActiveButterfly()
        {
            activeButterfly = new GameObject("CeramicWeavingShuttle").transform;
            activeButterfly.SetParent(boardRoot, false);
            butterflyBody = MgfLook.Prim(PrimitiveType.Capsule, "PorcelainShuttleBody", Vector3.zero,
                new Vector3(.32f, .68f, .28f), bodyMat, activeButterfly, false).transform;
            butterflyBody.localRotation = Quaternion.Euler(0, 0, 90f);
            wingLeft = MgfLook.Prim(PrimitiveType.Cylinder, "ThreadSpoolLeft", new Vector3(-.42f, 0, 0),
                new Vector3(.24f, .12f, .24f), flowerMat, activeButterfly, false).transform;
            wingLeft.localRotation = Quaternion.Euler(90, 0, 0);
            wingRight = MgfLook.Prim(PrimitiveType.Cylinder, "ThreadSpoolRight", new Vector3(.42f, 0, 0),
                new Vector3(.24f, .12f, .24f), brassMat, activeButterfly, false).transform;
            wingRight.localRotation = Quaternion.Euler(90, 0, 0);
            MgfLook.Prim(PrimitiveType.Sphere, "CoralNeedleEye", new Vector3(0, .38f, -.04f),
                new Vector3(.16f, .16f, .10f), saffronMat, activeButterfly, false);
        }

        void BuildMembrane()
        {
            var go = new GameObject("StudentWingMembrane");
            go.transform.SetParent(boardRoot, false);
            var mf = go.AddComponent<MeshFilter>();
            membraneMesh = new Mesh { name = "StudentTriangleMembrane" };
            membraneMesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            membraneMesh.triangles = new[] { 0, 1, 2 };
            membraneMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up };
            membraneMesh.RecalculateNormals();
            mf.sharedMesh = membraneMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = wingMat;
            membrane = go.transform;
            membrane.gameObject.SetActive(false);
        }

        Transform MakeTriangle(string name, Color color, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name + "Mesh" };
            mesh.vertices = new[] { new Vector3(0, -.15f, 0), new Vector3(1.1f, .25f, 0), new Vector3(.25f, 1.35f, 0) };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up };
            mesh.RecalculateNormals();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = MgfLook.Alpha(color);
            return go.transform;
        }

        Transform MakeSilkPanel(string name, Material mat, Transform parent)
        {
            // 사각 제도판 대신 가장자리가 불규칙한 얇은 날개막. 수학 좌표는 카메라와
            // 평행한 로컬 평면을 그대로 쓰므로 시각 정체성만 바뀌고 길이 판정은 변하지 않는다.
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name + "Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(0, 0, 0),
                new Vector3(-.46f, -.33f, 0), new Vector3(-.18f, -.50f, 0),
                new Vector3(.30f, -.43f, 0), new Vector3(.50f, -.08f, 0),
                new Vector3(.42f, .38f, 0), new Vector3(.08f, .50f, 0),
                new Vector3(-.36f, .42f, 0), new Vector3(-.52f, .08f, 0)
            };
            mesh.triangles = new[]
            {
                0,1,2, 0,2,3, 0,3,4, 0,4,5,
                0,5,6, 0,6,7, 0,7,8, 0,8,1
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            var outline = MakeLine("SilkMembraneSeam", MgfLook.Alpha(new Color(.24f, .48f, .55f, .55f)), .018f, 9, go.transform);
            outline.loop = true;
            for (int i = 0; i < 8; i++) outline.SetPosition(i, mesh.vertices[i + 1] + Vector3.back * .02f);
            outline.SetPosition(8, mesh.vertices[1] + Vector3.back * .02f);
            return go.transform;
        }

        LineRenderer MakeLine(string name, Material mat, float width, int count, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = mat;
            line.useWorldSpace = false;
            line.positionCount = count;
            line.startWidth = line.endWidth = width;
            line.numCapVertices = 5;
            line.numCornerVertices = 4;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        TextMeshPro WorldLabel(string text, float size, Color color)
        {
            var label = MgfText.World(text, Vector3.zero, size, color, boardRoot);
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.transform.localRotation = Quaternion.identity;
            label.transform.localScale = Vector3.one * .50f;
            return label;
        }

        void BuildUi()
        {
            roundSprite = MakeSprite(96, 20, 0);
            softOvalSprite = MakeSprite(96, 38, 0);
            ringSprite = MakeSprite(96, 0, 1);
            diamondSprite = MakeDiamondSprite(96);
            canvasRt = (RectTransform)MgfText.Canvas.transform;
            titleGroup = Group("Title", canvasRt);
            hudGroup = Group("Hud", canvasRt);
            endGroup = Group("End", canvasRt);
            helpGroup = Group("Help", canvasRt);
            toastGroup = Group("Toast", canvasRt);

            titleArtRt = R("TitleSkyArt", titleGroup.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844));
            titleArt = titleArtRt.gameObject.AddComponent<RawImage>();
            titleArt.raycastTarget = false;
            // 타이틀도 실제 Unity 고치 단면 세계를 그대로 본다. 생성 회화는 런타임에 쓰지 않는다.
            titleArt.texture = null;
            titleArt.color = Color.clear;

            titleArchRt = R("SilkLogoArch", titleGroup.transform, new Vector2(.5f, .83f), Vector2.zero, new Vector2(340, 150));
            // 황동 테두리 + 자개 명판. Simple 이미지로 늘이면 96px 원이 흐린 타원이 됐다 → 9-slice.
            titleArch = titleArchRt.gameObject.AddComponent<Image>();
            titleArch.sprite = roundSprite;
            titleArch.type = Image.Type.Sliced;
            titleArch.color = MgfLook.Hex("#EF8F91");
            titleArch.raycastTarget = false;
            titleFrame = Img(titleArchRt, "LuminousSilkLabel", new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, new Color(.91f, .98f, .94f, .96f), roundSprite);
            titleFrame.rectTransform.anchorMin = Vector2.zero;
            titleFrame.rectTransform.anchorMax = Vector2.one;
            titleFrame.rectTransform.offsetMin = new Vector2(6, 6);
            titleFrame.rectTransform.offsetMax = new Vector2(-6, -6);
            titleShadow = Txt(titleArchRt, "나비 재봉소", new Vector2(.5f, .62f), new Vector2(3, -5), 51, new Color(.05f, .08f, .18f, .55f), 350);
            titleLogo = Txt(titleArchRt, "나비 재봉소", new Vector2(.5f, .62f), Vector2.zero, 51, Ultramarine, 350);
            titleTag = Txt(titleArchRt, "두 원호로 빛나는 날개맥을 짜라", new Vector2(.5f, .24f), Vector2.zero, 17, Ink, 340);
            titleMeta = Txt(titleGroup.transform, "중학교 1학년 · 작도와 합동", new Vector2(.5f, .235f), Vector2.zero, 15, Ivory, 340);
            titleBest = Txt(titleGroup.transform, "", new Vector2(.5f, .19f), Vector2.zero, 14, Ivory, 320);

            titleCtaRt = R("OpeningCocoon", titleGroup.transform, new Vector2(.5f, .105f), Vector2.zero, new Vector2(318, 78));
            titleCta = titleCtaRt.gameObject.AddComponent<Image>();
            titleCta.sprite = softOvalSprite;
            titleCta.sprite = diamondSprite;
            titleCta.color = new Color(.28f, .68f, .65f, .98f);
            titleCta.raycastTarget = false;
            titleStart = Txt(titleCtaRt, "빛실 재봉소 열기", new Vector2(.5f, .5f), Vector2.zero, 21, Ivory, 270);
            helpLink = Txt(titleGroup.transform, "어떻게 놀아요?", new Vector2(.5f, .035f), Vector2.zero, 13, Ivory, 180);
            helpLinkRt = helpLink.rectTransform;
            helpLinkRt.sizeDelta = new Vector2(190, 46);

            headerBg = Img(hudGroup.transform, "SilkHudHeader", new Vector2(.5f, 1f), new Vector2(0, -34), new Vector2(374, 58), new Color(.89f, .97f, .93f, .96f), roundSprite);
            headerRt = headerBg.rectTransform;
            scoreText = Txt(headerRt, "0", new Vector2(.08f, .5f), Vector2.zero, 20, Ultramarine, 48);
            progressText = Txt(headerRt, "날개 0/6", new Vector2(.36f, .5f), Vector2.zero, 13, Ink, 132);
            timeText = Txt(headerRt, "150초", new Vector2(.665f, .5f), Vector2.zero, 15, Ultramarine, 66);
            livesText = Txt(headerRt, "실 ◆◆◆", new Vector2(.895f, .5f), Vector2.zero, 12, MgfLook.Hex("#A8601E"), 62);

            goalBg = Img(hudGroup.transform, "GoalThreadRibbon", new Vector2(.5f, 1f), new Vector2(0, -90), new Vector2(360, 44), new Color(.18f, .38f, .42f, .94f), roundSprite);
            goalRt = goalBg.rectTransform;
            goalText = Txt(goalRt, "같은 길이로 날개맥을 짜라", new Vector2(.5f, .5f), Vector2.zero, 16, Ivory, 342);
            goalText.enableAutoSizing = true;
            goalText.fontSizeMin = 11;
            goalText.fontSizeMax = 16;

            promptBg = Img(hudGroup.transform, "PromptSilkMembrane", new Vector2(.5f, 1f), new Vector2(0, -145), new Vector2(364, 58), new Color(.96f, .94f, 1f, .92f), roundSprite);
            promptRt = promptBg.rectTransform;
            promptText = Txt(promptRt, "", new Vector2(.5f, .5f), Vector2.zero, 13, Ink, 344);
            promptText.textWrappingMode = TextWrappingModes.Normal;
            promptText.enableAutoSizing = true;
            promptText.fontSizeMin = 11;
            promptText.fontSizeMax = 14;

            toolDockRt = R("ToolDock", hudGroup.transform, new Vector2(.5f, 0f), new Vector2(0, 42), new Vector2(360, 72));
            compassButton = Img(toolDockRt, "CompassTool", new Vector2(.27f, .5f), Vector2.zero, new Vector2(132, 58), new Color(.93f, .70f, .22f, .98f), roundSprite);
            compassButtonRt = compassButton.rectTransform;
            Txt(compassButtonRt, "컴퍼스 C", new Vector2(.5f, .5f), Vector2.zero, 18, Ink, 120);
            rulerButton = Img(toolDockRt, "RulerTool", new Vector2(.73f, .5f), Vector2.zero, new Vector2(132, 58), new Color(.93f, .91f, .83f, .98f), roundSprite);
            rulerButtonRt = rulerButton.rectTransform;
            Txt(rulerButtonRt, "눈금 없는 자 R", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 124);
            toolHintText = Txt(hudGroup.transform, "원본 변의 두 꼭짓점을 이어 폭을 잡으시오", new Vector2(.5f, 0f), new Vector2(0, 92), 15, Ink, 360);
            for (int i = 0; i < screenSourceLabels.Length; i++)
                screenSourceLabels[i] = Txt(hudGroup.transform, "ABC"[i].ToString(), new Vector2(.5f, .5f), Vector2.zero, 18, Ink, 32);
            for (int i = 0; i < screenTargetLabels.Length; i++)
                screenTargetLabels[i] = Txt(hudGroup.transform, "DE"[i].ToString(), new Vector2(.5f, .5f), Vector2.zero, 18, Ink, 32);
            for (int i = 0; i < screenEdgeLabels.Length; i++)
                screenEdgeLabels[i] = Txt(hudGroup.transform, "", new Vector2(.5f, .5f), Vector2.zero, 13, Ultramarine, 96);

            endRt = R("ResultCocoon", endGroup.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(366, 470));
            Img(endRt, "ResultIvory", new Vector2(.5f, .5f), Vector2.zero, new Vector2(362, 464), new Color(1f, .97f, .88f, .97f), softOvalSprite);
            endTitle = Txt(endRt, "날개맥 짜기 완료", new Vector2(.5f, .80f), Vector2.zero, 28, Ultramarine, 330);
            endScore = Txt(endRt, "0", new Vector2(.5f, .58f), Vector2.zero, 62, Saffron, 310);
            endStats = Txt(endRt, "", new Vector2(.5f, .38f), Vector2.zero, 19, Ink, 326);
            endTap = Txt(endRt, "고치를 눌러 바로 다시 재봉", new Vector2(.5f, .14f), Vector2.zero, 18, Ultramarine, 330);

            helpRt = R("HowToSilk", helpGroup.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(370, 540));
            Img(helpRt, "HelpIvory", new Vector2(.5f, .5f), Vector2.zero, new Vector2(366, 536), new Color(1f, .97f, .88f, .98f), softOvalSprite);
            helpTitle = Txt(helpRt, "세 변을 직접 옮기는 SSS 작도", new Vector2(.5f, .82f), Vector2.zero, 26, Ultramarine, 330);
            helpBody = Txt(helpRt,
                "1. 원본의 두 꼭짓점을 이어 컴퍼스 폭을 잡으시오.\n\n2. 중심을 밑변 끝점에 두고 원호를 그으시오.\n\n3. 두 원호의 교점을 밑변 양 끝과 직접 이으시오.\n\n실전 6개 중 첫 제출 5개 이상이 목표다.",
                new Vector2(.5f, .50f), Vector2.zero, 18, Ink, 326, TextAlignmentOptions.MidlineLeft);
            helpBody.rectTransform.sizeDelta = new Vector2(326, 320);
            helpTap = Txt(helpRt, "화면을 눌러 고정 연습 시작", new Vector2(.5f, .13f), Vector2.zero, 18, Ultramarine, 330);

            toastBg = Img(toastGroup.transform, "ThreadToast", new Vector2(.5f, .5f), Vector2.zero, new Vector2(344, 72), new Color(.12f, .20f, .42f, .94f), roundSprite);
            toastRt = toastBg.rectTransform;
            toastText = Txt(toastRt, "", new Vector2(.5f, .5f), Vector2.zero, 16, Ivory, 320);
            feedbackText = Txt(hudGroup.transform, "", new Vector2(.5f, .50f), Vector2.zero, 28, Ultramarine, 360);
            feedbackText.fontSize = 21;
            feedbackText.textWrappingMode = TextWrappingModes.Normal;
            feedbackText.rectTransform.sizeDelta = new Vector2(360, 84);
            feedbackText.gameObject.SetActive(false);

            rippleRt = R("AirRipple", toastGroup.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(80, 80));
            rippleImg = rippleRt.gameObject.AddComponent<Image>();
            rippleImg.sprite = ringSprite;
            rippleImg.color = new Color(1f, .78f, .25f, 0f);
            rippleImg.raycastTarget = false;
        }

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

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite)
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
            float width, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 2.4f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.text = text;
            return t;
        }

        CanvasGroup Group(string name, Transform parent)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt.gameObject.AddComponent<CanvasGroup>();
        }

        static Sprite MakeSprite(int size, float radius, int ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                if (ring == 1)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - size * .38f) / (size * .06f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * .5f) - (size * .5f - radius), 0);
                    float qy = Mathf.Max(Mathf.Abs(fy - size * .5f) - (size * .5f - radius), 0);
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            float border = ring == 1 ? 0 : radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        static Sprite MakeDiamondSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var p = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float d = Mathf.Abs((x + .5f) / size - .5f) + Mathf.Abs((y + .5f) / size - .5f);
                p[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01((.51f - d) * size) * 255));
            }
            tex.SetPixels32(p); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100);
        }

        void UpdateLayoutIfNeeded()
        {
            float w = canvasRt.rect.width, h = canvasRt.rect.height;
            if (Mathf.Abs(w - lastLayoutW) < .5f && Mathf.Abs(h - lastLayoutH) < .5f) return;
            lastLayoutW = w; lastLayoutH = h;
            float aspect = w / Mathf.Max(1f, h);
            landscape = (w >= h && w >= 640f) || (w >= 1024f && aspect >= .82f);
            portraitTablet = !landscape && aspect >= .58f;
            cam.orthographicSize = landscape ? 7.1f : portraitTablet ? 7.85f : 8.55f;

            // 가로: 발문 아래(위 3.3)·도구 안내 위(아래 -2.7) 띠 안에 원본판과 작도판을 겹치지 않게 나란히 둔다.
            sourceOffset = landscape ? new Vector2(-4.6f, .3f) : new Vector2(0, 2.45f);
            targetOffset = landscape ? new Vector2(3.0f, .3f) : new Vector2(0, -1.65f);
            hingeHomeLocal = landscape ? new Vector2(-.4f, 2.7f) : new Vector2(0, PortraitTargetBaseY - 1.0f);
            hingeLocal = hingeHomeLocal;
            sourcePanel.localPosition = new Vector3(sourceOffset.x, sourceOffset.y, .28f);
            sourcePanel.localScale = landscape ? new Vector3(6.6f, 5.9f, 1f) :
                portraitTablet ? new Vector3(7.55f, 4.10f, 1f) : new Vector3(6.7f, 3.65f, 1f);
            targetPanel.localPosition = new Vector3(targetOffset.x, targetOffset.y, .28f);
            targetPanel.localScale = landscape ? new Vector3(8.0f, 5.9f, 1f) :
                portraitTablet ? new Vector3(7.55f, 5.0f, 1f) : new Vector3(6.7f, 4.6f, 1f);
            if (titleRelief) titleRelief.position = new Vector3(0, landscape ? .55f : .1f, 1.6f);

            titleArtRt.sizeDelta = new Vector2(w, h);
            if (titleArt.texture)
            {
                float imageAspect = (float)titleArt.texture.width / titleArt.texture.height;
                float viewAspect = w / Mathf.Max(1f, h);
                if (imageAspect > viewAspect)
                {
                    float uvW = viewAspect / imageAspect;
                    titleArt.uvRect = new Rect((1f - uvW) * .5f, 0, uvW, 1);
                }
                else
                {
                    float uvH = imageAspect / viewAspect;
                    titleArt.uvRect = new Rect(0, (1f - uvH) * .5f, 1, uvH);
                }
            }
            titleArchRt.sizeDelta = new Vector2(Mathf.Min(500, w * .84f), landscape ? 118 : 150);
            titleArchRt.anchorMin = titleArchRt.anchorMax = new Vector2(.5f, landscape ? .835f : .84f);
            titleCtaRt.anchorMin = titleCtaRt.anchorMax = new Vector2(.5f, landscape ? .175f : .115f);
            titleMeta.rectTransform.anchorMin = titleMeta.rectTransform.anchorMax = new Vector2(.5f, landscape ? .29f : .215f);
            titleLogo.fontSize = landscape ? 46 : 48;
            titleShadow.fontSize = titleLogo.fontSize;
            titleCtaRt.sizeDelta = new Vector2(Mathf.Min(350, w * .80f), 78);

            // 오른쪽 위 48px 음소거 버튼(템플릿)이 헤더 끝의 실 타래 표시를 가리지 않게 폭을 줄인다.
            float headerW = landscape ? 340f : Mathf.Min(portraitTablet ? 700f : 374f, w - 78f);
            headerRt.sizeDelta = new Vector2(headerW, 58);
            headerRt.anchoredPosition = landscape ? new Vector2(w * .5f - 40f - headerW * .5f, -34) : new Vector2(-w * .5f + 8f + headerW * .5f, -34);
            float goalW = landscape ? Mathf.Min(360f, w - headerW - 70f) : Mathf.Min(portraitTablet ? 660f : 360f, w - 24f);
            goalRt.sizeDelta = new Vector2(goalW, 44);
            goalRt.anchoredPosition = landscape ? new Vector2(-w * .5f + 10f + goalW * .5f, -34) : new Vector2(0, -90);
            goalText.rectTransform.sizeDelta = new Vector2(goalW - 18f, 40);
            Vector2 promptSize = landscape ? new Vector2(Mathf.Min(700f, w - 30f), 56) :
                portraitTablet ? new Vector2(Mathf.Min(720f, w - 36f), 72) : new Vector2(364, 58);
            promptRt.sizeDelta = promptSize;
            promptRt.anchoredPosition = landscape ? new Vector2(0, -92) :
                portraitTablet ? new Vector2(0, -150) : new Vector2(0, -145);
            promptText.rectTransform.sizeDelta = promptSize - new Vector2(20f, 6f);
            toolDockRt.anchoredPosition = landscape ? new Vector2(0, 52) : portraitTablet ? new Vector2(0, 54) : new Vector2(0, 42);
            toolHintText.rectTransform.anchoredPosition = landscape ? new Vector2(0, 108) :
                portraitTablet ? new Vector2(0, 98) : new Vector2(0, 92);
            toolHintText.rectTransform.sizeDelta = new Vector2(Mathf.Min(portraitTablet ? 720f : 360f, w - 24f), 42f);
            endRt.sizeDelta = landscape ? new Vector2(520, 440) : new Vector2(366, 470);
            helpRt.sizeDelta = landscape ? new Vector2(560, 500) : new Vector2(370, 540);
            if (current != null) ResetWorldForProblem(current);
        }

        void SetScreen()
        {
            SetGroup(titleGroup, phase == RunPhase.Title);
            SetGroup(helpGroup, phase == RunPhase.Help);
            SetGroup(hudGroup, phase == RunPhase.Practice || phase == RunPhase.Playing || phase == RunPhase.Reveal);
            SetGroup(endGroup, phase == RunPhase.End);
            boardRoot.gameObject.SetActive(phase == RunPhase.Practice || phase == RunPhase.Playing || phase == RunPhase.Reveal);
            if (titleRelief) titleRelief.gameObject.SetActive(phase == RunPhase.Title || phase == RunPhase.Help);
            SetGroup(toastGroup, true);
            toastGroup.alpha = 0f;
            shownSecond = -1; // 연습의 「시간 정지」가 실전 첫 초(150)까지 남던 캐시를 비운다.
        }

        static void SetGroup(CanvasGroup group, bool on)
        {
            group.alpha = on ? 1 : 0;
            group.interactable = false;
            group.blocksRaycasts = false;
            group.gameObject.SetActive(on);
        }

        void ShowTitleVisual()
        {
            string best = PlayerPrefs.GetInt("nabi-jaebongso.bestFirst", 0) > 0 ?
                "최고 첫 제출 " + PlayerPrefs.GetInt("nabi-jaebongso.bestFirst", 0) + "/6" : "";
            titleBest.text = best;
            titleArtRt.localScale = Vector3.one;
            titleCtaRt.localScale = Vector3.one;
            titleArchRt.localScale = Vector3.one;
        }

        void ResetWorldForRun()
        {
            for (int i = 0; i < sentButterflies.Length; i++) sentButterflies[i].gameObject.SetActive(false);
            activeButterfly.gameObject.SetActive(false);
            for (int i = 0; i < practiceMarkers.Length; i++) practiceMarkers[i].gameObject.SetActive(false);
            membrane.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++) brassPins[i].gameObject.SetActive(false);
            revealVisualActive = false;
        }

        void ResetWorldForProblem(NabiProblem p)
        {
            // 화면 전환 직전에도 기하를 준비해야 한다. 비활성 보드도 Transform/LineRenderer 값은
            // 안전하게 갱신할 수 있으며, 여기서 건너뛰면 첫 프레임의 모든 꼭짓점이 (0,0)이 된다.
            if (p == null) return;
            ComputeGeometry(p);
            UpdateGeometryVisuals(p);
            for (int i = 0; i < 2; i++)
            {
                SetArcVisual(i, arcPresent[i], arcEdge[i], arcStart[i], arcEnd[i], arcProvided[i]);
                SetSegmentVisual(i, i == 0 ? segmentLeft : segmentRight, i == 0 ? segmentLeftEnd : segmentRightEnd);
                ghostLines[i].gameObject.SetActive(false);
            }
            hingeLocal = hingeHomeLocal;
            SetHingeVisual(hingeHomeLocal, false);
            SetCompassWidthVisual(selectedSourceEdge);
            activeButterfly.gameObject.SetActive(true);
            activeButterfly.localPosition = ShuttleHome;
            activeButterfly.localScale = Vector3.one * ShuttleScale;
            activeButterfly.localRotation = Quaternion.identity;
            practiceMarkers[0].localPosition = V3(sourcePoints[1], -.56f);
            practiceMarkers[1].localPosition = V3(sourcePoints[2], -.56f);
            bool showPracticeEndpoints = phase == RunPhase.Practice && selectedSourceEdge < 0;
            for (int i = 0; i < practiceMarkers.Length; i++) practiceMarkers[i].gameObject.SetActive(showPracticeEndpoints);
            membrane.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++) brassPins[i].gameObject.SetActive(false);
            // 작도판 위를 도는 노란 장식 호가 학생 원호처럼 읽혀 끈다.
            borderTrail.gameObject.SetActive(false);
            revealVisualActive = false;
        }

        void ComputeGeometry(NabiProblem p)
        {
            float ab = p.ab, ac = p.ac, bc = p.bc;
            float cx = (ab * ab + ac * ac - bc * bc) / (2f * ab);
            float cy = Mathf.Sqrt(Mathf.Max(.01f, ac * ac - cx * cx));
            Vector2[] raw = { new Vector2(0, 0), new Vector2(ab, 0), new Vector2(cx, cy) };
            Vector2 center = (raw[0] + raw[1] + raw[2]) / 3f;
            float ang = p.rotationDeg * Mathf.Deg2Rad;
            float cs = Mathf.Cos(ang), sn = Mathf.Sin(ang);
            float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 q = raw[i] - center;
                if (p.reflected) q.x = -q.x;
                q = new Vector2(q.x * cs - q.y * sn, q.x * sn + q.y * cs);
                raw[i] = q;
                minX = Mathf.Min(minX, q.x); maxX = Mathf.Max(maxX, q.x);
                minY = Mathf.Min(minY, q.y); maxY = Mathf.Max(maxY, q.y);
            }
            float halfX = Mathf.Max(1f, Mathf.Max(-minX, maxX));
            float halfY = Mathf.Max(1f, Mathf.Max(-minY, maxY));
            float baseCm = p.EdgeLength(p.baseEdge);
            if (landscape)
            {
                float apexCm = ab * cy / baseCm;
                geometryScale = Mathf.Min(Mathf.Min(.52f, Mathf.Min(2.6f / halfX, 2.2f / halfY)),
                    Mathf.Min(4.5f / Mathf.Max(1f, apexCm), 7.0f / baseCm));
                for (int i = 0; i < 3; i++) sourcePoints[i] = sourceOffset + raw[i] * geometryScale;
                float baseLength = baseCm * geometryScale;
                targetLeftLocal = targetOffset + new Vector2(-baseLength * .5f, -2.05f);
                targetRightLocal = targetOffset + new Vector2(baseLength * .5f, -2.05f);
                return;
            }

            // 세로 화면: 원본 삼각형은 발문 아래 원본판 안에, 대상 밑변은 아래쪽에 고정한다.
            // 같은 축척(geometryScale)으로 원본 높이 + 대상 꼭짓점 높이(밑변 위 높이)가 함께
            // 들어가도록 축척을 고른다. 예전에는 원본이 판 밖으로 나가 꼭짓점이 발문 패널에 가려졌다.
            float sourceTop = portraitTablet ? 3.38f : 4.3f;
            float baseY = PortraitTargetBaseY;
            const float gap = 1.05f;
            float apexHeightCm = ab * cy / baseCm; // 2×넓이÷밑변
            float sY = (sourceTop - baseY - gap) / Mathf.Max(1f, (maxY - minY) + apexHeightCm);
            float halfWidthLimit = portraitTablet ? 3.45f : 2.95f;
            float baseWidthLimit = portraitTablet ? 6.45f : 5.6f;
            geometryScale = Mathf.Min(.52f, Mathf.Min(sY, Mathf.Min(halfWidthLimit / halfX, baseWidthLimit / baseCm)));
            Vector2 origin = new Vector2(0f, sourceTop - maxY * geometryScale);
            for (int i = 0; i < 3; i++) sourcePoints[i] = origin + raw[i] * geometryScale;
            float sourceBottom = sourceTop - (maxY - minY) * geometryScale;
            float half = baseCm * geometryScale * .5f;
            targetLeftLocal = new Vector2(-half, baseY);
            targetRightLocal = new Vector2(half, baseY);

            float srcTop = sourceTop + .78f, srcBottom = sourceBottom - .72f;
            sourcePanel.localPosition = new Vector3(0f, (srcTop + srcBottom) * .5f, .28f);
            float panelWidth = portraitTablet ? 7.55f : 6.7f;
            sourcePanel.localScale = new Vector3(panelWidth, srcTop - srcBottom, .18f);
            // 820×1180 태블릿에서 대상판 하단과 하단 조작 안내 사이 24px 안전 간격을 남긴다.
            float tgtTop = srcBottom - .14f, tgtBottom = baseY - (portraitTablet ? .35f : 1.5f);
            targetOffset = new Vector2(0f, (tgtTop + tgtBottom) * .5f);
            targetPanel.localPosition = new Vector3(0f, targetOffset.y, .28f);
            targetPanel.localScale = new Vector3(panelWidth, tgtTop - tgtBottom, .18f);
        }

        void UpdateGeometryVisuals(NabiProblem p)
        {
            sourceTriangle.positionCount = 4;
            sourceTriangle.SetPosition(0, V3(sourcePoints[0], -.30f));
            sourceTriangle.SetPosition(1, V3(sourcePoints[1], -.30f));
            sourceTriangle.SetPosition(2, V3(sourcePoints[2], -.30f));
            sourceTriangle.SetPosition(3, V3(sourcePoints[0], -.30f));
            targetBase.SetPosition(0, V3(targetLeftLocal, -.30f));
            targetBase.SetPosition(1, V3(targetRightLocal, -.30f));
            Vector2 sourceCentroid = (sourcePoints[0] + sourcePoints[1] + sourcePoints[2]) / 3f;
            for (int i = 0; i < 3; i++)
            {
                sourcePins[i].localPosition = V3(sourcePoints[i], -.42f);
                sourceLabels[i].text = "ABC"[i].ToString();
                Vector2 outward = (sourcePoints[i] - sourceCentroid).normalized;
                sourceLabels[i].transform.localPosition = V3(sourcePoints[i] + outward * .58f, -.46f);
            }
            int[] edgesA = { 0, 0, 1 };
            int[] edgesB = { 1, 2, 2 };
            for (int e = 0; e < 3; e++)
            {
                Vector2 mid = (sourcePoints[edgesA[e]] + sourcePoints[edgesB[e]]) * .5f;
                sourceEdgeLabels[e].text = p.EdgeName(e) + (p.band <= 1 ? "  " + p.EdgeLength(e) + " cm" : "");
                if (screenEdgeLabels[e]) screenEdgeLabels[e].text = p.band <= 1 ? p.EdgeLength(e) + " cm" : "";
                sourceEdgeLabels[e].transform.localPosition = V3(mid + new Vector2(0, .20f), -.45f);
            }
            targetPins[0].localPosition = V3(targetLeftLocal, -.43f);
            targetPins[1].localPosition = V3(targetRightLocal, -.43f);
            targetPins[2].gameObject.SetActive(false);
            targetLabels[0].text = p.LeftLabel.ToString();
            targetLabels[1].text = p.RightLabel.ToString();
            targetLabels[2].text = p.ApexLabel.ToString();
            if (screenTargetLabels[0]) screenTargetLabels[0].text = p.LeftLabel.ToString();
            if (screenTargetLabels[1]) screenTargetLabels[1].text = p.RightLabel.ToString();
            targetLabels[0].transform.localPosition = V3(targetLeftLocal + new Vector2(0, -.45f), -.45f);
            targetLabels[1].transform.localPosition = V3(targetRightLocal + new Vector2(0, -.45f), -.45f);
            targetLabels[2].gameObject.SetActive(false);
        }

        static Vector3 V3(Vector2 p, float z) => new Vector3(p.x, p.y, z);
        static readonly int[] EdgeA = { 0, 0, 1 };
        static readonly int[] EdgeB = { 1, 2, 2 };

        Vector2 SourcePointLocal(int index) => sourcePoints[Mathf.Clamp(index, 0, 2)];
        float EdgeWorldLength(int edge) => current == null || edge < 0 ? 1.5f : current.EdgeLength(edge) * geometryScale;

        bool ScreenToBoardLocal(Vector2 screen, out Vector2 local)
        {
            Ray ray = cam.ScreenPointToRay(screen);
            Plane plane = new Plane(boardRoot.forward, boardRoot.position);
            if (plane.Raycast(ray, out float d))
            {
                Vector3 lp = boardRoot.InverseTransformPoint(ray.GetPoint(d));
                local = new Vector2(lp.x, lp.y);
                return true;
            }
            local = Vector2.zero;
            return false;
        }

        Vector2 LocalToScreen(Vector2 local)
        {
            Vector3 world = boardRoot.TransformPoint(V3(local, -.42f));
            return cam.WorldToScreenPoint(world);
        }

        void UpdateStateScreenCoordinates()
        {
            Vector2 a = LocalToScreen(sourcePoints[0]);
            Vector2 b = LocalToScreen(sourcePoints[1]);
            Vector2 c = LocalToScreen(sourcePoints[2]);
            Vector2 l = LocalToScreen(targetLeftLocal);
            Vector2 r = LocalToScreen(targetRightLocal);
            Vector2 h = LocalToScreen(hingeLocal);
            Vector2 p = LocalToScreen(pencilLocal);
            st.sourceAScreenX = a.x; st.sourceAScreenY = Screen.height - a.y;
            st.sourceBScreenX = b.x; st.sourceBScreenY = Screen.height - b.y;
            st.sourceCScreenX = c.x; st.sourceCScreenY = Screen.height - c.y;
            st.targetLeftScreenX = l.x; st.targetLeftScreenY = Screen.height - l.y;
            st.targetRightScreenX = r.x; st.targetRightScreenY = Screen.height - r.y;
            st.hingeScreenX = h.x; st.hingeScreenY = Screen.height - h.y;
            st.pencilScreenX = p.x; st.pencilScreenY = Screen.height - p.y;
            Vector3 cb = compassButtonRt.TransformPoint(compassButtonRt.rect.center);
            Vector3 rb = rulerButtonRt.TransformPoint(rulerButtonRt.rect.center);
            st.compassButtonScreenX = cb.x; st.compassButtonScreenY = Screen.height - cb.y;
            st.rulerButtonScreenX = rb.x; st.rulerButtonScreenY = Screen.height - rb.y;
        }

        void SetSourceDragVisual(int startVertex, Vector2 currentPoint)
        {
            sourceDragLine.gameObject.SetActive(true);
            sourceDragLine.SetPosition(0, V3(sourcePoints[startVertex], -.52f));
            sourceDragLine.SetPosition(1, V3(currentPoint, -.52f));
        }

        void HideSourceDragVisual() => sourceDragLine.gameObject.SetActive(false);

        void SetHingeVisual(Vector2 local, bool draggingNow)
        {
            hingeLocal = local;
            hinge.localPosition = V3(local, -.52f);
            // 처음 만든 납작한 구 크기를 유지한다. Vector3.one 으로 덮으면 단위 구(지름 1)로 커져
            // 안내문과 밑변 라벨을 가렸다.
            hinge.localScale = HingeScale * (draggingNow ? 1.16f : 1f);
            UpdateCompassGeometry();
        }

        void SetCompassWidthVisual(int edge)
        {
            if (edge >= 0)
                for (int i = 0; i < practiceMarkers.Length; i++) practiceMarkers[i].gameObject.SetActive(false);
            if (edge < 0)
            {
                hingeLocal = hingeHomeLocal;
                hinge.localPosition = V3(hingeLocal, -.52f);
                pencilLocal = hingeHomeLocal + new Vector2(1.35f, 0);
            }
            else UpdateCompassGeometry();
            UpdateCompassGeometry();
        }

        void UpdateCompassGeometry()
        {
            float radius = selectedSourceEdge < 0 ? 1.35f : EdgeWorldLength(selectedSourceEdge);
            // 양쪽 중심 모두에서 연필 손잡이가 화면 안쪽을 향하게 한다. 오른쪽 중심을 늘
            // 오른쪽으로 펼치면 390px 화면 밖으로 나가 실제 손가락으로 원호를 시작할 수 없다.
            // 가장 긴 15 cm 반지름도 390px 화면 안에서 잡을 수 있게 양쪽 연필을
            // 위·안쪽으로 세운다. 수평에 가까운 각도는 오른쪽 중심에서 x<0까지 벗어났다.
            float angle = drag == DragKind.Arc ? tracedCurrentAngle : compassCenter == 1 ? 2.36f : .78f;
            if (compassCenter >= 0)
            {
                hingeLocal = compassCenter == 0 ? targetLeftLocal : targetRightLocal;
                pencilLocal = hingeLocal + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            else pencilLocal = hingeLocal + new Vector2(Mathf.Min(radius, 2.4f), .05f);
            hinge.localPosition = V3(hingeLocal, -.52f);
            pencil.localPosition = V3(pencilLocal, -.52f);
            compassLeg.positionCount = 3;
            compassLeg.SetPosition(0, V3(hingeLocal, -.48f));
            compassLeg.SetPosition(1, V3((hingeLocal + pencilLocal) * .5f + new Vector2(0, .32f), -.48f));
            compassLeg.SetPosition(2, V3(pencilLocal, -.48f));
        }

        void SetArcPreview(int center, int edge, float start, float end)
        {
            SetArcVisual(center, true, edge, start, end, false);
            UpdateCompassGeometry();
        }

        void SetArcVisual(int center, bool visible, int edge, float start, float end, bool provided)
        {
            if (center < 0 || center > 1) return;
            var line = arcLines[center];
            line.gameObject.SetActive(visible && edge >= 0);
            if (!visible || edge < 0) return;
            Vector2 c = center == 0 ? targetLeftLocal : targetRightLocal;
            float radius = EdgeWorldLength(edge);
            float delta = end - start;
            if (Mathf.Abs(delta) < .02f) delta = .02f;
            line.positionCount = 64;
            line.sharedMaterial = provided ? MgfLook.Unlit(new Color(.35f, .43f, .55f, .72f)) : MgfLook.Unlit(Ink);
            for (int i = 0; i < 64; i++)
            {
                float t = i / 63f;
                float a = start + delta * t;
                line.SetPosition(i, V3(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, -.40f));
            }
        }

        void RestoreArcVisual(int center)
        {
            SetArcVisual(center, arcPresent[center], arcEdge[center], arcStart[center], arcEnd[center], arcProvided[center]);
        }

        void SetSegmentPreview(int center, Vector2 start, Vector2 end)
        {
            var line = segmentLines[center];
            line.gameObject.SetActive(true);
            line.SetPosition(0, V3(start, -.52f));
            line.SetPosition(1, V3(end, -.52f));
        }

        void SetSegmentVisual(int center, bool visible, Vector2 end)
        {
            var line = segmentLines[center];
            line.gameObject.SetActive(visible);
            if (!visible) return;
            Vector2 start = center == 0 ? targetLeftLocal : targetRightLocal;
            line.SetPosition(0, V3(start, -.52f));
            line.SetPosition(1, V3(end, -.52f));
        }

        void RestoreSegmentVisual(int center)
        {
            bool visible = center == 0 ? segmentLeft : segmentRight;
            Vector2 end = center == 0 ? segmentLeftEnd : segmentRightEnd;
            SetSegmentVisual(center, visible, end);
        }

        bool CircleIntersections(float r0, float r1, out Vector2 a, out Vector2 b)
        {
            Vector2 p0 = targetLeftLocal, p1 = targetRightLocal;
            float d = Vector2.Distance(p0, p1);
            if (d <= .001f || d > r0 + r1 || d < Mathf.Abs(r0 - r1))
            {
                a = b = (p0 + p1) * .5f + Vector2.up * Mathf.Max(.8f, Mathf.Min(r0, r1));
                return false;
            }
            float x = (r0 * r0 - r1 * r1 + d * d) / (2f * d);
            float y = Mathf.Sqrt(Mathf.Max(0, r0 * r0 - x * x));
            Vector2 dir = (p1 - p0) / d;
            Vector2 mid = p0 + dir * x;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            a = mid + perp * y;
            b = mid - perp * y;
            return true;
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            promptText.text = current.prompt;
            promptText.fontSizeMax = landscape ? 15 : 13.5f;
            if (phase == RunPhase.Practice)
            {
                goalText.text = "같은 길이로 날개맥을 짜라";
                toolHintText.text = PracticeHint();
            }
            else
            {
                goalText.text = "실전 6개 · 첫 제출 5개 이상 · 실 타래 3개";
                toolHintText.text = tool == ToolMode.Compass ? "원본 변 → 중심 → 원호 순서로 직접 옮기시오" : "두 원호의 교점을 밑변 양 끝과 이으시오";
            }
        }

        string PracticeHint()
        {
            if (selectedSourceEdge < 0) return practiceGuideStrong ?
                "B를 누른 채 C에서 떼기" : "원본의 B에서 C까지 이어 컴퍼스 폭을 잡으시오";
            if (compassCenter < 0) return "중심 손잡이를 오른쪽 밑변 끝점으로 옮기시오";
            if (!arcPresent[1]) return "연필 끝을 원둘레로 움직여 두 번째 원호를 그으시오";
            if (tool == ToolMode.Compass) return "눈금 없는 자를 눌러 두 원호의 교점을 이으시오";
            if (!segmentLeft) return "왼쪽 밑변 끝점에서 원호의 교점까지 그으시오";
            if (!segmentRight) return "오른쪽 밑변 끝점에서도 같은 교점까지 그으시오";
            return "세 대응변이 같은지 확인 중";
        }

        void ReplayGuide(bool stronger)
        {
            if (phase != RunPhase.Practice)
            {
                guideBoost = stronger ? 1.2f : .7f;
                return;
            }
            guideBoost = stronger ? 3f : 2f;
            guideHand.gameObject.SetActive(true);
            guidePath.gameObject.SetActive(true);
            RefreshProblemUi();
        }

        void UpdateGuide(float dt)
        {
            if (!guideHand || !guidePath) return;
            if (phase != RunPhase.Practice)
            {
                guideHand.gameObject.SetActive(false);
                guidePath.gameObject.SetActive(false);
                return;
            }
            // 연습 중에는 다음 동작의 경로를 계속 보여 준다. 진전이 없을 때(guideBoost)만 더 크게 맥동한다.
            guideBoost = Mathf.Max(0f, guideBoost - dt);
            guideHand.gameObject.SetActive(true);
            guidePath.gameObject.SetActive(true);
            Vector2 from, to;
            if (selectedSourceEdge < 0) { from = sourcePoints[1]; to = sourcePoints[2]; }
            else if (compassCenter < 0) { from = hingeHomeLocal; to = targetRightLocal; }
            else if (!arcPresent[1])
            {
                float r = EdgeWorldLength(selectedSourceEdge);
                from = targetRightLocal + new Vector2(r, 0);
                to = targetRightLocal + new Vector2(0, r);
            }
            else if (tool == ToolMode.Compass)
            {
                Vector2 screen = compassButtonRt.anchoredPosition;
                guideHand.gameObject.SetActive(false);
                guidePath.gameObject.SetActive(false);
                rulerButtonRt.localScale = Vector3.one * (1f + .09f * Mathf.Sin(worldClock * 8f));
                return;
            }
            else
            {
                Vector2 a, b;
                CircleIntersections(EdgeWorldLength(current.expectedLeftEdge), EdgeWorldLength(current.expectedRightEdge), out a, out b);
                Vector2 apex = a.y >= b.y ? a : b;
                from = !segmentLeft ? targetLeftLocal : targetRightLocal;
                to = apex;
            }
            float flow = Mathf.Repeat(worldClock / 1.2f, 1f);
            float eased = flow * flow * (3f - 2f * flow);
            guideHand.localPosition = V3(Vector2.Lerp(from, to, eased), -.65f);
            float guidePulse = 1f + (guideBoost > 2f ? .28f : guideBoost > 0f ? .16f : .08f) * Mathf.Sin(worldClock * 7f);
            guideHand.localScale = new Vector3(.36f, .36f, .12f) * guidePulse;
            guidePath.positionCount = 24;
            for (int i = 0; i < 24; i++)
            {
                float t = i / 23f;
                Vector2 p = Vector2.Lerp(from, to, t);
                p.y += Mathf.Sin(t * Mathf.PI) * .16f;
                guidePath.SetPosition(i, V3(p, -.60f));
            }
        }

        void UpdateWorld(float dt)
        {
            worldClock += dt;
            float reduce = MgfBridge.LowGfx ? .25f : 1f;
            if (titleRelief && titleRelief.gameObject.activeSelf)
            {
                // 2.5D: 카메라와 평행한 판을 살짝 기울여 흔들어 두께와 그림자가 보이게 한다.
                titleRelief.rotation = cam.transform.rotation * Quaternion.Euler(10f + Mathf.Sin(worldClock * .7f) * 2f * reduce,
                    -12f + Mathf.Sin(worldClock * .5f) * 5f * reduce, 0f);
                UpdateTitleDemo(Mathf.Repeat(worldClock, DemoLoop));
            }
            for (int i = 0; i < cocoons.Length; i++)
            {
                if (!cocoons[i]) continue;
                float sway = Mathf.Sin(worldClock * (.55f + i * .017f) + i) * (2.2f + i % 3) * reduce;
                cocoons[i].localPosition = cocoonHomes[i] + Vector3.up * Mathf.Sin(worldClock * .65f + i) * .07f * reduce;
                cocoons[i].localRotation = Quaternion.Euler(0, sway * .35f, sway);
            }
            for (int i = 0; i < flowers.Length; i++)
                if (flowers[i]) flowers[i].localRotation = Quaternion.Euler(0, 0,
                    Mathf.Sin(worldClock * .45f + i * .7f) * 3f * reduce);
            for (int i = 0; i < sentButterflies.Length; i++) if (sentButterflies[i] && sentButterflies[i].gameObject.activeSelf)
            {
                sentButterflies[i].localPosition = sentHomes[i];
                sentButterflies[i].localScale = Vector3.one * (.86f + Mathf.Sin(worldClock * 2.2f + i) * .025f);
            }
            if (activeButterfly && activeButterfly.gameObject.activeSelf)
            {
                float pulse = 1f + Mathf.Sin(worldClock * 1.8f) * .018f;
                if (wingLeft) wingLeft.localScale = new Vector3(-.82f * pulse, .82f, .82f);
                if (wingRight) wingRight.localScale = new Vector3(.82f * pulse, .82f, .82f);
            }
            if (phase == RunPhase.Practice && selectedSourceEdge < 0)
                for (int i = 0; i < practiceMarkers.Length; i++) if (practiceMarkers[i])
                {
                    float markerPulse = 1f + Mathf.Sin(worldClock * 4.2f + i * Mathf.PI) * .13f;
                    float markerSize = practiceGuideStrong ? .90f : .72f;
                    practiceMarkers[i].localScale = new Vector3(markerSize, markerSize, .08f) * markerPulse;
                }
            UpdateGuide(dt);
            UpdateBorderTrail();
            UpdateRevealVisual(dt);
            if (drag == DragKind.Arc) UpdateCompassGeometry();
        }

        void UpdateBorderTrail()
        {
            if (!borderTrail || !borderTrail.gameObject.activeSelf || current == null) return;
            float t0 = Mathf.Repeat(worldClock * .16f, 1f);
            Vector2 center = targetOffset;
            float rx = targetPanel.localScale.x * .48f;
            float ry = targetPanel.localScale.y * .47f;
            borderTrail.positionCount = 20;
            for (int i = 0; i < 20; i++)
            {
                float t = t0 + (i / 19f) * .13f;
                float a = t * Mathf.PI * 2f;
                borderTrail.SetPosition(i, V3(center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry), -.58f));
            }
        }

        void UpdateRevealVisual(float dt)
        {
            if (!revealVisualActive) return;
            revealVisualClock += dt;
            if (revealVisualCorrect)
            {
                float k = Mathf.Clamp01(revealVisualClock / .7f);
                float overshoot = 1f + Mathf.Sin(k * Mathf.PI) * .12f * (1f - k);
                membrane.localScale = Vector3.one * (Mathf.SmoothStep(.05f, 1f, k) * overshoot);
                for (int i = 0; i < 3; i++)
                {
                    bool on = revealVisualClock >= .12f + i * .18f;
                    brassPins[i].gameObject.SetActive(on);
                    if (on) brassPins[i].localScale = Vector3.one * (1f + Mathf.Sin((revealVisualClock - i * .18f) * 12f) * .12f);
                }
                if (revealVisualClock > .78f)
                {
                    // 학생이 만든 두 변을 도자 셔틀이 따라가며 실크 막을 엮는다. 정답 화면 흔들림은 없다.
                    float weave = Mathf.Clamp01((revealVisualClock - .78f) / .72f);
                    Vector2 path = weave < .5f ? Vector2.Lerp(targetLeftLocal, revealCandidate, weave * 2f) :
                        Vector2.Lerp(revealCandidate, targetRightLocal, (weave - .5f) * 2f);
                    activeButterfly.localPosition = V3(path + new Vector2(0, .18f), -.72f);
                    activeButterfly.localScale = Vector3.one * (ShuttleScale * (1f + Mathf.Sin(weave * Mathf.PI) * .20f));
                }
            }
            else
            {
                activeButterfly.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(revealVisualClock * 12f) * 4f * (1f - revealVisualClock / 2f));
            }
        }

        void UpdateUi(float dt)
        {
            if (phase == RunPhase.Title)
            {
                titleBreath += dt;
                float s = 1f + Mathf.Sin(titleBreath * .65f) * (MgfBridge.LowGfx ? .004f : .012f);
                titleArtRt.localScale = Vector3.one * s;
                titleCtaRt.localScale = Vector3.one * (1f + Mathf.Sin(titleBreath * 2.2f) * .025f);
                titleArchRt.localScale = Vector3.one * (1f + Mathf.Sin(titleBreath * 1.25f) * .008f);
            }
            if (phase == RunPhase.Practice || phase == RunPhase.Playing || phase == RunPhase.Reveal)
            {
                UpdateScreenPointLabels();
                displayedScore = Mathf.MoveTowards(displayedScore, st.score, dt * Mathf.Max(160f, Mathf.Abs(st.score - displayedScore) * 4f));
                scoreText.text = Mathf.RoundToInt(displayedScore).ToString();
                progressText.text = phase == RunPhase.Practice ? "고정 연습" : "날개 " + st.solved + "/6 · 첫 " + st.firstCorrect + "/" + Mathf.Max(st.firstAttempts, 1);
                int sec = phase == RunPhase.Practice ? 150 : Mathf.Max(0, Mathf.CeilToInt(secondsLeft));
                int secKey = phase == RunPhase.Practice ? -2 : sec;
                if (secKey != shownSecond) { shownSecond = secKey; timeText.text = phase == RunPhase.Practice ? "정지" : sec + "초"; }
                livesText.text = "실 " + new string('◆', Mathf.Max(0, st.lives)) + new string('◇', Mathf.Max(0, NabiRules.StartLives - st.lives));
                compassButton.color = tool == ToolMode.Compass ? new Color(.96f, .73f, .22f, 1f) : new Color(.90f, .87f, .76f, .88f);
                rulerButton.color = tool == ToolMode.Ruler ? new Color(.96f, .73f, .22f, 1f) : new Color(.90f, .87f, .76f, .88f);
                if (phase != RunPhase.Practice) rulerButtonRt.localScale = Vector3.one;
            }
            if (phase == RunPhase.End)
            {
                displayedEndScore = Mathf.MoveTowards(displayedEndScore, st.score, dt * Mathf.Max(180f, st.score * 2f));
                endScore.text = Mathf.RoundToInt(displayedEndScore).ToString();
            }
            if (toastClock > 0f)
            {
                toastClock -= dt;
                toastGroup.alpha = Mathf.Clamp01(Mathf.Min(toastClock * 3f, 1f));
                toastRt.localScale = Vector3.one * (1f + Mathf.Sin((1.2f - toastClock) * 8f) * .03f);
            }
            else toastGroup.alpha = 0f;
            if (refuseClock > 0f)
            {
                refuseClock -= dt;
                float k = refuseClock / .42f;
                toolDockRt.anchoredPosition += new Vector2(Mathf.Sin(refuseClock * 70f) * 3.2f * k, 0);
            }
            if (rippleImg.color.a > 0f)
            {
                float a = Mathf.Max(0, rippleImg.color.a - dt * 2.8f);
                rippleImg.color = new Color(1f, .78f, .25f, a);
                rippleRt.localScale += Vector3.one * dt * 2.4f;
            }
        }

        void UpdateScreenPointLabels()
        {
            if (current == null) return;
            Vector2 centroid = (sourcePoints[0] + sourcePoints[1] + sourcePoints[2]) / 3f;
            float sourceLabelTopCenter = float.MaxValue;
            for (int i = 0; i < screenSourceLabels.Length; i++)
            {
                Vector2 outward = (sourcePoints[i] - centroid).normalized;
                Vector2 screen = LocalToScreen(sourcePoints[i] + outward * (portraitTablet ? .22f : .72f));
                sourceLabelTopCenter = Mathf.Min(sourceLabelTopCenter, Screen.height - screen.y);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out Vector2 local);
                screenSourceLabels[i].rectTransform.anchoredPosition = local;
            }
            st.sourceLabelTopCenterScreenY = sourceLabelTopCenter;
            int[] ea = EdgeA, eb = EdgeB;
            for (int e = 0; e < screenEdgeLabels.Length; e++)
            {
                Vector2 mid = (sourcePoints[ea[e]] + sourcePoints[eb[e]]) * .5f;
                Vector2 outward = mid - centroid;
                outward = outward.sqrMagnitude < .0001f ? Vector2.up : outward.normalized;
                Vector2 screen = LocalToScreen(mid + outward * .38f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out Vector2 local);
                screenEdgeLabels[e].rectTransform.anchoredPosition = local;
            }
            for (int i = 0; i < screenTargetLabels.Length; i++)
            {
                Vector2 target = i == 0 ? targetLeftLocal : targetRightLocal;
                Vector2 screen = LocalToScreen(target + Vector2.down * .58f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out Vector2 local);
                screenTargetLabels[i].rectTransform.anchoredPosition = local;
            }
            Vector2 panelBottomScreen = LocalToScreen(new Vector2(targetOffset.x,
                targetOffset.y - targetPanel.localScale.y * .5f));
            st.targetPanelBottomScreenY = Screen.height - panelBottomScreen.y;
            promptRt.GetWorldCorners(uiCorners);
            st.promptBottomScreenY = Screen.height - uiCorners[0].y;
            toolHintText.rectTransform.GetWorldCorners(uiCorners);
            st.toolHintTopScreenY = Screen.height - uiCorners[1].y;
        }

        bool HitToolButton(Vector2 screen, out ToolMode hit)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(compassButtonRt, screen)) { hit = ToolMode.Compass; return true; }
            if (RectTransformUtility.RectangleContainsScreenPoint(rulerButtonRt, screen)) { hit = ToolMode.Ruler; return true; }
            hit = ToolMode.Compass;
            return false;
        }

        bool HitHelp(Vector2 screen) => RectTransformUtility.RectangleContainsScreenPoint(helpLinkRt, screen);

        void PunchTool(ToolMode mode)
        {
            MgfFx.Punch(mode == ToolMode.Compass ? compassButtonRt : rulerButtonRt, .10f, .20f);
            RefreshProblemUi();
            if (phase == RunPhase.Practice) ReplayGuide(false);
        }

        void RefuseInput(Vector2 screen, string message)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out local);
            rippleRt.anchoredPosition = local;
            rippleRt.localScale = Vector3.one * .25f;
            rippleImg.color = new Color(1f, .78f, .25f, .9f);
            refuseClock = .42f;
            ShowToast(message, true);
            MgfSfx.Play("wrong", .22f);
            if (phase == RunPhase.Practice)
            {
                if (selectedSourceEdge < 0)
                {
                    practiceFailedInputs++;
                    if (practiceFailedInputs >= 2) practiceGuideStrong = true;
                }
                ReplayGuide(true);
            }
            else guideBoost = 1f;
            MgfBridge.NotifyChanged();
        }

        void ShowToast(string text, bool refusal)
        {
            toastText.text = text;
            toastText.color = refusal ? MgfLook.Hex("#FFE29A") : Ivory;
            toastClock = refusal ? 1.35f : 1.05f;
            toastGroup.alpha = 1f;
            toastRt.localScale = Vector3.one * .86f;
        }

        void BeginRevealVisual(bool correct, bool wasFirst, Vector2 candidate)
        {
            revealVisualActive = true;
            revealVisualCorrect = correct;
            revealVisualClock = 0f;
            revealCandidate = candidate;
            feedbackText.gameObject.SetActive(true);
            if (correct)
            {
                SetMembrane(targetLeftLocal, targetRightLocal, candidate);
                membrane.gameObject.SetActive(true);
                membrane.localScale = Vector3.one * .05f;
                targetPins[2].gameObject.SetActive(true);
                targetPins[2].localPosition = V3(candidate, -.48f);
                targetLabels[2].gameObject.SetActive(true);
                targetLabels[2].transform.localPosition = V3(candidate + new Vector2(0, .45f), -.50f);
                Vector3 world = boardRoot.TransformPoint(V3(candidate, -.7f));
                MgfFx.Glow(world, Saffron, 16 + Mathf.Min(18, st.firstCorrect * 3), .30f);
                MgfFx.Burst(world, Saffron, 10 + Mathf.Min(16, st.firstCorrect * 2), 2.7f, .12f);
                MgfSfx.Play("correct", .46f);
                feedbackText.text = "세 대응변의 길이가 같으므로 SSS 합동";
                feedbackText.color = Ultramarine;
                goalText.text = current.EdgeName(current.baseEdge) + "=" + current.LeftLabel + current.RightLabel +
                    " · " + current.EdgeName(current.expectedLeftEdge) + "=" + current.LeftLabel + current.ApexLabel +
                    " · " + current.EdgeName(current.expectedRightEdge) + "=" + current.RightLabel + current.ApexLabel;
            }
            else
            {
                Vector2 a, b;
                CircleIntersections(EdgeWorldLength(current.expectedLeftEdge), EdgeWorldLength(current.expectedRightEdge), out a, out b);
                Vector2 apex = Vector2.Distance(candidate, a) <= Vector2.Distance(candidate, b) ? a : b;
                ghostLines[0].gameObject.SetActive(true);
                ghostLines[0].SetPosition(0, V3(targetLeftLocal, -.62f));
                ghostLines[0].SetPosition(1, V3(apex, -.62f));
                ghostLines[1].gameObject.SetActive(true);
                ghostLines[1].SetPosition(0, V3(targetRightLocal, -.62f));
                ghostLines[1].SetPosition(1, V3(apex, -.62f));
                MgfFx.Shake(cam, .08f, .25f);
                MgfSfx.Play("wrong", .42f);
                feedbackText.text = st.misconceptionId == "correspondence_order" ? "기호 순서대로 대응 중심을 다시 확인하시오" :
                    st.misconceptionId == "arbitrary_edge" ? "원호에 옮긴 원본 변이 대응변과 다르다" : "두 원호가 만나는 같은 교점에 두 선을 이으시오";
                feedbackText.color = Ink;
                goalText.text = "상아 유령선은 제출 뒤에만 보이는 올바른 대응이다";
            }
            MgfFx.Punch(feedbackText.transform, .12f, .30f);
        }

        void CompleteRevealVisual(bool correct)
        {
            feedbackText.gameObject.SetActive(false);
            for (int i = 0; i < 2; i++) ghostLines[i].gameObject.SetActive(false);
            for (int i = 0; i < 3; i++) brassPins[i].gameObject.SetActive(false);
            membrane.gameObject.SetActive(false);
            activeButterfly.localRotation = Quaternion.identity;
            activeButterfly.localPosition = ShuttleHome;
            activeButterfly.localScale = Vector3.one * ShuttleScale;
            if (correct && current.band > 0 && st.solved > 0)
            {
                int idx = Mathf.Clamp(st.solved - 1, 0, sentButterflies.Length - 1);
                sentButterflies[idx].gameObject.SetActive(true);
                sentButterflies[idx].localPosition = sentHomes[idx];
            }
            revealVisualActive = false;
            if (phase == RunPhase.Reveal && current != null) RefreshProblemUi();
        }

        void SetMembrane(Vector2 a, Vector2 b, Vector2 c)
        {
            membraneMesh.vertices = new[] { V3(a, -.49f), V3(b, -.49f), V3(c, -.49f) };
            membraneMesh.RecalculateBounds();
            membraneMesh.RecalculateNormals();
            membrane.localPosition = Vector3.zero;
            Vector2 center = (a + b + c) / 3f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 p = i == 0 ? a : i == 1 ? b : c;
                brassPins[i].localPosition = V3(p, -.64f);
            }
        }

        void ShowEndVisual(string reason)
        {
            bool clear = reason == "clear";
            endTitle.text = clear ? "여섯 날개맥 짜기 완료" : reason == "thread" ? "실 타래가 모두 풀렸다" :
                reason == "time" ? "재봉 시간이 끝났다" : "첫 제출 5개에 닿을 수 없다";
            endTitle.color = clear ? Ultramarine : Ink;
            endStats.text = "완성 " + st.solved + "/6\n첫 제출 정답 " + st.firstCorrect + "/" + st.firstAttempts +
                "\n남은 실 " + st.lives + " · 남은 시간 " + Mathf.Max(0, Mathf.CeilToInt(secondsLeft)) + "초";
            displayedEndScore = 0f;
            endScore.text = "0";
            if (clear)
            {
                PlayerPrefs.SetInt("nabi-jaebongso.bestFirst", Mathf.Max(PlayerPrefs.GetInt("nabi-jaebongso.bestFirst", 0), st.firstCorrect));
                PlayerPrefs.Save();
            }
        }

        void ShowHelpVisual(bool on)
        {
            SetGroup(helpGroup, on);
            SetGroup(titleGroup, !on);
        }
    }
}
