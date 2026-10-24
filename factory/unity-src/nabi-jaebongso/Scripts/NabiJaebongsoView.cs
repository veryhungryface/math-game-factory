// 나비 재봉소 — 푸른 공중 고치 디오라마, 수학 평면, UI와 피드백.
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
        static readonly Color SkyBlue = MgfLook.Hex("#63B8F2");
        static readonly Color SkyLight = MgfLook.Hex("#BFE7FF");
        static readonly Color Ivory = MgfLook.Hex("#FFF8E9");
        static readonly Color IvoryShadow = MgfLook.Hex("#D9CFB9");
        static readonly Color Ultramarine = MgfLook.Hex("#243D86");
        static readonly Color Saffron = MgfLook.Hex("#EAAF3D");
        static readonly Color Ink = MgfLook.Hex("#252A36");
        static readonly Color Brass = MgfLook.Hex("#D9A52B");
        static readonly Color Ghost = new Color(1f, .96f, .80f, .82f);

        Camera cam;
        Transform worldRoot, boardRoot, sourcePanel, targetPanel, activeButterfly, butterflyBody, wingLeft, wingRight;
        Transform hinge, pencil, guideHand, activeCocoon, membrane;
        readonly Transform[] sourcePins = new Transform[3];
        readonly Transform[] targetPins = new Transform[3];
        readonly Transform[] cocoons = new Transform[12];
        readonly Transform[] flowers = new Transform[18];
        readonly Transform[] sentButterflies = new Transform[NabiRules.TargetWings];
        readonly Transform[] antennae = new Transform[2];
        readonly Transform[] brassPins = new Transform[3];
        readonly Vector3[] cocoonHomes = new Vector3[12];
        readonly Vector3[] sentHomes = new Vector3[NabiRules.TargetWings];
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
        bool landscape;
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
        Sprite roundSprite, softOvalSprite, ringSprite, diamondSprite;
        float displayedScore, displayedEndScore, titleBreath;
        int shownSecond = -1;

        Vector2 TargetLeftLocal => targetLeftLocal;
        Vector2 TargetRightLocal => targetRightLocal;
        Vector2 HingeHomeLocal => hingeHomeLocal;
        Vector2 HingeLocal => hingeLocal;
        Vector2 PencilLocal => pencilLocal;

        void BuildMaterials()
        {
            ivoryMat = MgfLook.Lit(Ivory, .28f, .01f);
            ivorySoftMat = MgfLook.Lit(IvoryShadow, .16f, .01f);
            blueMat = MgfLook.Lit(Ultramarine, .30f, .06f);
            blueGlowMat = MgfLook.Lit(MgfLook.Hex("#5E83E6"), .46f, .10f, MgfLook.Hex("#314C9F") * .25f);
            saffronMat = MgfLook.Lit(Saffron, .40f, .04f);
            inkMat = MgfLook.Lit(Ink, .12f, .01f);
            brassMat = MgfLook.Lit(Brass, .78f, .64f, Saffron * .12f);
            bodyMat = MgfLook.Lit(MgfLook.Hex("#F6E8CE"), .25f, .01f);
            wingMat = MgfLook.Alpha(new Color(.78f, .91f, 1f, .64f));
            seamMat = MgfLook.Unlit(Ultramarine);
            ghostMat = MgfLook.Alpha(Ghost);
            flowerMat = MgfLook.Lit(Saffron, .35f, .02f);
            silkMat = MgfLook.Unlit(new Color(1f, .97f, .87f, .94f));
        }

        void BuildWorld()
        {
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<SphereCollider>();
            Destroy(colliderTypes);

            MgfLook.Sky(MgfLook.Hex("#3C9DEA"), SkyBlue, MgfLook.Hex("#D8F2FF"), .93f);
            MgfLook.Sun(new Vector3(43, -28, 12), MgfLook.Hex("#FFF4CD"), 1.18f, .62f);
            cam = MgfLook.Camera(new Vector3(0, 6.8f, -18f), new Vector3(0, .5f, 1.1f), 34f);
            cam.orthographic = true;
            cam.orthographicSize = 8.4f;
            cameraHome = cam.transform.position;

            worldRoot = new GameObject("HangingCocoonSkyWorld").transform;
            BuildSilkSky();
            BuildCocoonsAndFlowers();
            BuildSentButterflies();

            boardRoot = new GameObject("CameraParallelConstructionPlane").transform;
            boardRoot.position = new Vector3(0, .45f, .3f);
            boardRoot.rotation = cam.transform.rotation;
            sourcePanel = MgfLook.Block("SourceWingTag", new Vector3(0, 0, .28f), new Vector3(6.7f, 3.65f, .18f), .28f, ivoryMat, boardRoot).transform;
            targetPanel = MgfLook.Block("TargetWingTag", new Vector3(0, -3.5f, .28f), new Vector3(6.7f, 4.6f, .18f), .30f, ivoryMat, boardRoot).transform;
            Destroy(sourcePanel.GetComponent<Collider>());
            Destroy(targetPanel.GetComponent<Collider>());

            sourceTriangle = MakeLine("OriginalTriangle", seamMat, .095f, 4, boardRoot);
            targetBase = MakeLine("GivenBase", MgfLook.Unlit(Ink), .12f, 2, boardRoot);
            sourceDragLine = MakeLine("CompassWidthDrag", MgfLook.Unlit(Saffron), .11f, 2, boardRoot);
            sourceDragLine.gameObject.SetActive(false);
            compassLeg = MakeLine("CompassLeg", MgfLook.Unlit(Brass), .085f, 3, boardRoot);
            guidePath = MakeLine("OnboardingSilkPath", MgfLook.Unlit(Saffron), .12f, 24, boardRoot);
            guidePath.gameObject.SetActive(false);
            borderTrail = MakeLine("AnimatedThreadBorder", MgfLook.Additive(new Color(1f, .79f, .24f, .8f)), .075f, 20, boardRoot);
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
                brassPins[i] = MgfLook.Prim(PrimitiveType.Cylinder, "BrassLockPin" + i, Vector3.zero,
                    new Vector3(.18f, .07f, .18f), brassMat, boardRoot, false).transform;
                brassPins[i].localRotation = Quaternion.Euler(90, 0, 0);
                brassPins[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < 3; i++) sourceEdgeLabels[i] = WorldLabel("", .72f, Ultramarine);

            hinge = MgfLook.Prim(PrimitiveType.Sphere, "CompassHinge", Vector3.zero,
                new Vector3(.42f, .42f, .20f), brassMat, boardRoot, false).transform;
            pencil = MgfLook.Prim(PrimitiveType.Cylinder, "CompassPencil", Vector3.zero,
                new Vector3(.16f, .38f, .16f), saffronMat, boardRoot, false).transform;
            pencil.localRotation = Quaternion.Euler(90, 0, 0);
            guideHand = MgfLook.Prim(PrimitiveType.Sphere, "GuideFinger", Vector3.zero,
                new Vector3(.34f, .46f, .18f), saffronMat, boardRoot, false).transform;
            guideHand.gameObject.SetActive(false);

            BuildActiveButterfly();
            BuildMembrane();
        }

        void BuildSilkSky()
        {
            for (int row = 0; row < 7; row++)
            {
                var line = MakeLine("IvorySilkNet" + row, silkMat, .045f + row * .004f, 9, worldRoot);
                for (int i = 0; i < 9; i++)
                {
                    float x = -13f + i * 3.25f;
                    float y = 5.6f + row * .62f + Mathf.Sin(i * .9f + row) * .38f;
                    float z = 4f + row * 1.5f;
                    line.SetPosition(i, new Vector3(x, y, z));
                }
            }
            for (int i = 0; i < 9; i++)
            {
                var line = MakeLine("HangingThread" + i, silkMat, .035f, 2, worldRoot);
                float x = -10.5f + i * 2.65f;
                line.SetPosition(0, new Vector3(x, 10f, 7f + i % 3));
                line.SetPosition(1, new Vector3(x + Mathf.Sin(i) * .35f, 1.8f + i % 4 * .7f, 7f + i % 3));
            }
        }

        void BuildCocoonsAndFlowers()
        {
            for (int i = 0; i < cocoons.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (5.2f + (i % 4) * 1.38f);
                float y = 2.1f + (i % 5) * 1.2f;
                float z = 4f + (i % 3) * 1.8f;
                var root = new GameObject("SilkCocoon" + i).transform;
                root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(x, y, z);
                cocoonHomes[i] = root.localPosition;
                var shell = MgfLook.Prim(PrimitiveType.Capsule, "IvoryCocoonBody", Vector3.zero,
                    new Vector3(.74f, 1.15f, .74f), i % 3 == 0 ? ivorySoftMat : ivoryMat, root, false);
                shell.transform.localRotation = Quaternion.Euler(0, i * 17, side * (4 + i % 3 * 3));
                for (int band = 0; band < 5; band++)
                {
                    float radius = .42f + Mathf.Sin(band) * .04f;
                    var wrap = MakeLine("SilkWrap", silkMat, .025f, 25, root);
                    wrap.loop = true;
                    float yBand = -.55f + band * .28f;
                    for (int k = 0; k < 25; k++)
                    {
                        float a = k * Mathf.PI * 2f / 24f;
                        wrap.SetPosition(k, new Vector3(Mathf.Cos(a) * radius, yBand,
                            Mathf.Sin(a) * radius));
                    }
                }
            }
            for (int i = 0; i < flowers.Length; i++)
            {
                float a = i * Mathf.PI * 2f / flowers.Length;
                float r = 6.3f + (i % 4) * .9f;
                var root = new GameObject("SaffronFlower" + i).transform;
                root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(Mathf.Cos(a) * r, -2.2f + (i % 5) * .9f, 2.5f + Mathf.Sin(a) * r + (i % 3));
                flowers[i] = root;
                MgfLook.Prim(PrimitiveType.Sphere, "FlowerHeart", Vector3.zero, Vector3.one * .18f, brassMat, root, false);
                for (int p = 0; p < 5; p++)
                {
                    float pa = p * Mathf.PI * 2f / 5f;
                    var petal = MgfLook.Prim(PrimitiveType.Sphere, "Petal", new Vector3(Mathf.Cos(pa) * .25f, Mathf.Sin(pa) * .25f, 0),
                        new Vector3(.28f, .14f, .08f), flowerMat, root, false);
                    petal.transform.localRotation = Quaternion.Euler(0, 0, p * 72f);
                }
            }
        }

        void BuildSentButterflies()
        {
            for (int i = 0; i < sentButterflies.Length; i++)
            {
                sentButterflies[i] = MakeMiniButterfly("SentButterfly" + i, worldRoot);
                float a = 25f + i * 27f;
                sentHomes[i] = new Vector3(-4.7f + i * 1.75f, 4.2f + Mathf.Sin(i * 1.6f) * .9f, 3.2f + i % 3);
                sentButterflies[i].localPosition = sentHomes[i];
                sentButterflies[i].localRotation = Quaternion.Euler(0, a, i % 2 == 0 ? -8 : 8);
                sentButterflies[i].gameObject.SetActive(false);
            }
        }

        Transform MakeMiniButterfly(string name, Transform parent)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            MgfLook.Prim(PrimitiveType.Sphere, "RiceBody", Vector3.zero, new Vector3(.28f, .42f, .24f), bodyMat, root, false);
            for (int s = -1; s <= 1; s += 2)
            {
                var wing = MakeTriangle("Wing", new Color(.70f, .88f, 1f, .64f), root);
                wing.localPosition = new Vector3(s * .22f, .08f, .04f);
                wing.localScale = new Vector3(s * .75f, .75f, .75f);
            }
            return root;
        }

        void BuildActiveButterfly()
        {
            activeButterfly = new GameObject("ActiveButterfly").transform;
            activeButterfly.SetParent(boardRoot, false);
            butterflyBody = MgfLook.Prim(PrimitiveType.Capsule, "PlumpRiceBody", Vector3.zero,
                new Vector3(.42f, .68f, .34f), bodyMat, activeButterfly, false).transform;
            butterflyBody.localRotation = Quaternion.Euler(0, 0, 0);
            var head = MgfLook.Prim(PrimitiveType.Sphere, "RoundHead", new Vector3(0, .65f, -.04f),
                Vector3.one * .48f, bodyMat, activeButterfly, false).transform;
            for (int e = -1; e <= 1; e += 2)
                MgfLook.Prim(PrimitiveType.Sphere, "Eye", new Vector3(e * .16f, .72f, -.42f), Vector3.one * .08f, inkMat, activeButterfly, false);
            for (int a = 0; a < 2; a++)
            {
                antennae[a] = MgfLook.Block("Antenna", new Vector3((a == 0 ? -.16f : .16f), 1.18f, -.02f),
                    new Vector3(.055f, .7f, .055f), .02f, inkMat, activeButterfly).transform;
                antennae[a].localRotation = Quaternion.Euler(0, 0, a == 0 ? -18 : 18);
                Destroy(antennae[a].GetComponent<Collider>());
            }
            wingLeft = MakeTriangle("LeftTriangularWing", new Color(.70f, .88f, 1f, .66f), activeButterfly);
            wingLeft.localPosition = new Vector3(-.30f, .15f, .08f);
            wingLeft.localScale = new Vector3(-1.35f, 1.35f, 1.35f);
            wingRight = MakeTriangle("RightTriangularWing", new Color(.70f, .88f, 1f, .66f), activeButterfly);
            wingRight.localPosition = new Vector3(.30f, .15f, .08f);
            wingRight.localScale = Vector3.one * 1.35f;
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
            label.transform.localScale = Vector3.one * .12f;
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
            var tex = Resources.Load<Texture2D>("NabiJaebongso/title");
            if (tex) { titleArt.texture = tex; titleArt.color = Color.white; }
            else titleArt.color = Color.clear; // 코드 하늘/고치 세계가 그대로 폴백이다.

            titleArchRt = R("SilkLogoArch", titleGroup.transform, new Vector2(.5f, .80f), Vector2.zero, new Vector2(366, 190));
            titleArch = titleArchRt.gameObject.AddComponent<Image>();
            titleArch.sprite = softOvalSprite;
            titleArch.color = new Color(1f, .97f, .87f, .88f);
            titleArch.raycastTarget = false;
            titleShadow = Txt(titleArchRt, "나비 재봉소", new Vector2(.5f, .62f), new Vector2(3, -5), 51, new Color(.05f, .08f, .18f, .55f), 350);
            titleLogo = Txt(titleArchRt, "나비 재봉소", new Vector2(.5f, .62f), Vector2.zero, 51, Ultramarine, 350);
            titleTag = Txt(titleArchRt, "원을 그려 날개를 펴라", new Vector2(.5f, .27f), Vector2.zero, 18, Ink, 330);
            titleMeta = Txt(titleGroup.transform, "중학교 1학년 · 작도와 합동", new Vector2(.5f, .235f), Vector2.zero, 15, Ivory, 340);
            titleBest = Txt(titleGroup.transform, "", new Vector2(.5f, .19f), Vector2.zero, 14, Ivory, 320);

            titleCtaRt = R("OpeningCocoon", titleGroup.transform, new Vector2(.5f, .105f), Vector2.zero, new Vector2(318, 78));
            titleCta = titleCtaRt.gameObject.AddComponent<Image>();
            titleCta.sprite = softOvalSprite;
            titleCta.color = new Color(1f, .95f, .78f, .98f);
            titleCta.raycastTarget = false;
            titleStart = Txt(titleCtaRt, "고치 열어 재봉 시작", new Vector2(.5f, .5f), Vector2.zero, 22, Ultramarine, 300);
            helpLink = Txt(titleGroup.transform, "어떻게 놀아요?", new Vector2(.5f, .035f), Vector2.zero, 13, Ivory, 180);
            helpLinkRt = helpLink.rectTransform;
            helpLinkRt.sizeDelta = new Vector2(190, 46);

            headerBg = Img(hudGroup.transform, "SilkHudHeader", new Vector2(.5f, 1f), new Vector2(0, -34), new Vector2(374, 58), new Color(1f, .97f, .88f, .96f), roundSprite);
            headerRt = headerBg.rectTransform;
            scoreText = Txt(headerRt, "0", new Vector2(.075f, .5f), Vector2.zero, 20, Ultramarine, 54);
            progressText = Txt(headerRt, "날개 0/6", new Vector2(.35f, .5f), Vector2.zero, 14, Ink, 150);
            timeText = Txt(headerRt, "150초", new Vector2(.67f, .5f), Vector2.zero, 16, Ultramarine, 72);
            livesText = Txt(headerRt, "실 ◆◆◆", new Vector2(.88f, .5f), Vector2.zero, 13, Saffron, 82);

            goalBg = Img(hudGroup.transform, "GoalThreadTag", new Vector2(.5f, 1f), new Vector2(0, -90), new Vector2(360, 44), new Color(.13f, .24f, .53f, .96f), roundSprite);
            goalRt = goalBg.rectTransform;
            goalText = Txt(goalRt, "같은 길이로 날개를 그리면 나비가 난다", new Vector2(.5f, .5f), Vector2.zero, 16, Ivory, 342);

            promptBg = Img(hudGroup.transform, "PromptSilkTag", new Vector2(.5f, 1f), new Vector2(0, -145), new Vector2(364, 58), new Color(1f, .97f, .88f, .94f), roundSprite);
            promptRt = promptBg.rectTransform;
            promptText = Txt(promptRt, "", new Vector2(.5f, .5f), Vector2.zero, 13, Ink, 344);
            promptText.textWrappingMode = TextWrappingModes.Normal;

            toolDockRt = R("ToolDock", hudGroup.transform, new Vector2(.5f, 0f), new Vector2(0, 42), new Vector2(360, 72));
            compassButton = Img(toolDockRt, "CompassTool", new Vector2(.27f, .5f), Vector2.zero, new Vector2(132, 58), new Color(.93f, .70f, .22f, .98f), roundSprite);
            compassButtonRt = compassButton.rectTransform;
            Txt(compassButtonRt, "컴퍼스 C", new Vector2(.5f, .5f), Vector2.zero, 18, Ink, 120);
            rulerButton = Img(toolDockRt, "RulerTool", new Vector2(.73f, .5f), Vector2.zero, new Vector2(132, 58), new Color(.93f, .91f, .83f, .98f), roundSprite);
            rulerButtonRt = rulerButton.rectTransform;
            Txt(rulerButtonRt, "눈금 없는 자 R", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 124);
            toolHintText = Txt(hudGroup.transform, "원본 변의 두 꼭짓점을 이어 폭을 잡으시오", new Vector2(.5f, 0f), new Vector2(0, 92), 14, Ultramarine, 360);

            endRt = R("ResultCocoon", endGroup.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(366, 470));
            Img(endRt, "ResultIvory", new Vector2(.5f, .5f), Vector2.zero, new Vector2(362, 464), new Color(1f, .97f, .88f, .97f), softOvalSprite);
            endTitle = Txt(endRt, "비행 대열 완성", new Vector2(.5f, .80f), Vector2.zero, 28, Ultramarine, 330);
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
            landscape = (w >= h && w >= 640f) || w >= 1024f;
            cam.orthographicSize = landscape ? 7.1f : 8.55f;

            sourceOffset = landscape ? new Vector2(-4.45f, .55f) : new Vector2(0, 2.45f);
            targetOffset = landscape ? new Vector2(3.15f, .25f) : new Vector2(0, -1.65f);
            hingeHomeLocal = landscape ? new Vector2(0, -4.45f) : new Vector2(0, -6.05f);
            hingeLocal = hingeHomeLocal;
            sourcePanel.localPosition = new Vector3(sourceOffset.x, sourceOffset.y, .28f);
            sourcePanel.localScale = landscape ? new Vector3(1.07f, 1.55f, 1) : new Vector3(1f, 1f, 1);
            targetPanel.localPosition = new Vector3(targetOffset.x, targetOffset.y, .28f);
            targetPanel.localScale = landscape ? new Vector3(1.28f, 1.55f, 1) : new Vector3(1f, 1f, 1);

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
            titleArchRt.sizeDelta = new Vector2(Mathf.Min(520, w * .84f), landscape ? 168 : 190);
            titleLogo.fontSize = landscape ? 55 : 51;
            titleShadow.fontSize = titleLogo.fontSize;
            titleCtaRt.sizeDelta = new Vector2(Mathf.Min(350, w * .80f), 78);

            headerRt.anchoredPosition = landscape ? new Vector2(w * .27f, -34) : new Vector2(0, -34);
            goalRt.anchoredPosition = landscape ? new Vector2(-w * .23f, -38) : new Vector2(0, -90);
            promptRt.anchoredPosition = landscape ? new Vector2(-w * .23f, -92) : new Vector2(0, -145);
            promptRt.sizeDelta = landscape ? new Vector2(Mathf.Min(480, w * .39f), 74) : new Vector2(364, 58);
            toolDockRt.anchoredPosition = landscape ? new Vector2(0, 52) : new Vector2(0, 42);
            toolHintText.rectTransform.anchoredPosition = landscape ? new Vector2(0, 108) : new Vector2(0, 92);
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
            SetGroup(toastGroup, true);
            toastGroup.alpha = 0f;
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
            activeButterfly.localPosition = new Vector3(targetOffset.x - (landscape ? 4.3f : 3.0f), targetOffset.y - .2f, -.55f);
            activeButterfly.localScale = Vector3.one * (landscape ? .78f : .68f);
            membrane.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++) brassPins[i].gameObject.SetActive(false);
            borderTrail.gameObject.SetActive(true);
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
            float maxAbs = 0f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 q = raw[i] - center;
                if (p.reflected) q.x = -q.x;
                q = new Vector2(q.x * cs - q.y * sn, q.x * sn + q.y * cs);
                raw[i] = q;
                maxAbs = Mathf.Max(maxAbs, Mathf.Abs(q.x), Mathf.Abs(q.y));
            }
            float panelHalf = landscape ? 3.1f : 2.75f;
            geometryScale = Mathf.Min(.52f, panelHalf / Mathf.Max(1f, maxAbs));
            for (int i = 0; i < 3; i++) sourcePoints[i] = sourceOffset + raw[i] * geometryScale;
            float baseLength = p.EdgeLength(p.baseEdge) * geometryScale;
            targetLeftLocal = targetOffset + new Vector2(-baseLength * .5f, landscape ? -.8f : -1.0f);
            targetRightLocal = targetOffset + new Vector2(baseLength * .5f, landscape ? -.8f : -1.0f);
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
            for (int i = 0; i < 3; i++)
            {
                sourcePins[i].localPosition = V3(sourcePoints[i], -.42f);
                sourceLabels[i].text = "ABC"[i].ToString();
                sourceLabels[i].transform.localPosition = V3(sourcePoints[i] + new Vector2(0, .42f), -.46f);
            }
            int[] edgesA = { 0, 0, 1 };
            int[] edgesB = { 1, 2, 2 };
            for (int e = 0; e < 3; e++)
            {
                Vector2 mid = (sourcePoints[edgesA[e]] + sourcePoints[edgesB[e]]) * .5f;
                sourceEdgeLabels[e].text = p.EdgeName(e) + (p.band <= 1 ? "  " + p.EdgeLength(e) + " cm" : "");
                sourceEdgeLabels[e].transform.localPosition = V3(mid + new Vector2(0, .20f), -.45f);
            }
            targetPins[0].localPosition = V3(targetLeftLocal, -.43f);
            targetPins[1].localPosition = V3(targetRightLocal, -.43f);
            targetPins[2].gameObject.SetActive(false);
            targetLabels[0].text = p.LeftLabel.ToString();
            targetLabels[1].text = p.RightLabel.ToString();
            targetLabels[2].text = p.ApexLabel.ToString();
            targetLabels[0].transform.localPosition = V3(targetLeftLocal + new Vector2(0, -.45f), -.45f);
            targetLabels[1].transform.localPosition = V3(targetRightLocal + new Vector2(0, -.45f), -.45f);
            targetLabels[2].gameObject.SetActive(false);
        }

        static Vector3 V3(Vector2 p, float z) => new Vector3(p.x, p.y, z);

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
            st.sourceAScreenX = Mathf.RoundToInt(a.x); st.sourceAScreenY = Mathf.RoundToInt(Screen.height - a.y);
            st.sourceBScreenX = Mathf.RoundToInt(b.x); st.sourceBScreenY = Mathf.RoundToInt(Screen.height - b.y);
            st.sourceCScreenX = Mathf.RoundToInt(c.x); st.sourceCScreenY = Mathf.RoundToInt(Screen.height - c.y);
            st.targetLeftScreenX = Mathf.RoundToInt(l.x); st.targetLeftScreenY = Mathf.RoundToInt(Screen.height - l.y);
            st.targetRightScreenX = Mathf.RoundToInt(r.x); st.targetRightScreenY = Mathf.RoundToInt(Screen.height - r.y);
            st.hingeScreenX = Mathf.RoundToInt(h.x); st.hingeScreenY = Mathf.RoundToInt(Screen.height - h.y);
            st.pencilScreenX = Mathf.RoundToInt(p.x); st.pencilScreenY = Mathf.RoundToInt(Screen.height - p.y);
            Vector3 cb = compassButtonRt.TransformPoint(compassButtonRt.rect.center);
            Vector3 rb = rulerButtonRt.TransformPoint(rulerButtonRt.rect.center);
            st.compassButtonScreenX = Mathf.RoundToInt(cb.x); st.compassButtonScreenY = Mathf.RoundToInt(Screen.height - cb.y);
            st.rulerButtonScreenX = Mathf.RoundToInt(rb.x); st.rulerButtonScreenY = Mathf.RoundToInt(Screen.height - rb.y);
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
            hinge.localScale = Vector3.one * (draggingNow ? 1.16f : 1f);
            UpdateCompassGeometry();
        }

        void SetCompassWidthVisual(int edge)
        {
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
            float angle = drag == DragKind.Arc ? tracedCurrentAngle : compassCenter == 1 ? 2.82f : .32f;
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
            promptText.fontSize = landscape ? 15 : 13;
            if (phase == RunPhase.Practice)
            {
                goalText.text = "같은 길이로 날개를 그리면 나비가 난다";
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
            if (selectedSourceEdge < 0) return "원본의 B에서 C까지 이어 컴퍼스 폭을 잡으시오";
            if (compassCenter < 0) return "황동 중심 손잡이를 오른쪽 밑변 끝점으로 옮기시오";
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
            if (phase != RunPhase.Practice || guideBoost <= 0f)
            {
                guideHand.gameObject.SetActive(false);
                guidePath.gameObject.SetActive(false);
                return;
            }
            guideBoost -= dt;
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
            guideHand.localScale = Vector3.one * (1f + (guideBoost > 2f ? .28f : .12f) * Mathf.Sin(worldClock * 7f));
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
            for (int i = 0; i < cocoons.Length; i++)
            {
                if (!cocoons[i]) continue;
                float sway = Mathf.Sin(worldClock * (.55f + i * .017f) + i) * (2.2f + i % 3) * reduce;
                cocoons[i].localPosition = cocoonHomes[i] + Vector3.up * Mathf.Sin(worldClock * .65f + i) * .07f * reduce;
                cocoons[i].localRotation = Quaternion.Euler(0, sway * .35f, sway);
            }
            for (int i = 0; i < flowers.Length; i++)
                if (flowers[i]) flowers[i].localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 1.2f + i * .7f) * .06f * reduce);
            for (int i = 0; i < sentButterflies.Length; i++) if (sentButterflies[i] && sentButterflies[i].gameObject.activeSelf)
            {
                sentButterflies[i].localPosition = sentHomes[i] + new Vector3(Mathf.Sin(worldClock * .8f + i) * .18f,
                    Mathf.Sin(worldClock * 1.4f + i * 2f) * .22f, 0);
                sentButterflies[i].localScale = Vector3.one * (.86f + Mathf.Sin(worldClock * 5f + i) * .07f);
            }
            if (activeButterfly && activeButterfly.gameObject.activeSelf)
            {
                float flap = .78f + Mathf.Abs(Mathf.Sin(worldClock * 3.2f)) * .42f;
                if (!revealVisualCorrect) flap *= revealVisualActive ? .42f : 1f;
                if (wingLeft) wingLeft.localScale = new Vector3(-1.35f * flap, 1.35f, 1.35f);
                if (wingRight) wingRight.localScale = new Vector3(1.35f * flap, 1.35f, 1.35f);
                for (int i = 0; i < antennae.Length; i++) if (antennae[i]) antennae[i].localRotation = Quaternion.Euler(0, 0,
                    (i == 0 ? -18f : 18f) + Mathf.Sin(worldClock * 2.6f + i) * 5f);
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
            float rx = landscape ? 4.6f : 3.25f;
            float ry = landscape ? 3.15f : 3.0f;
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
                    float fly = Mathf.Clamp01((revealVisualClock - .78f) / .78f);
                    Vector3 start = V3(targetOffset + new Vector2(0, .3f), -.75f);
                    Vector3 end = V3(targetOffset + new Vector2(landscape ? 2.8f : 1.7f, 5.7f), -2f);
                    activeButterfly.localPosition = Vector3.Lerp(start, end, fly * fly * (3f - 2f * fly));
                    activeButterfly.localScale = Vector3.one * Mathf.Lerp(.72f, .38f, fly);
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
                displayedScore = Mathf.MoveTowards(displayedScore, st.score, dt * Mathf.Max(160f, Mathf.Abs(st.score - displayedScore) * 4f));
                scoreText.text = Mathf.RoundToInt(displayedScore).ToString();
                progressText.text = phase == RunPhase.Practice ? "고정 연습" : "날개 " + st.solved + "/6 · 첫 " + st.firstCorrect + "/" + Mathf.Max(st.firstAttempts, 1);
                int sec = phase == RunPhase.Practice ? 150 : Mathf.Max(0, Mathf.CeilToInt(secondsLeft));
                if (sec != shownSecond) { shownSecond = sec; timeText.text = phase == RunPhase.Practice ? "시간 정지" : sec + "초"; }
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
            if (phase == RunPhase.Practice) ReplayGuide(true);
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
            endTitle.text = clear ? "여섯 나비 비행 대열 완성" : reason == "thread" ? "실 타래가 모두 풀렸다" :
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
