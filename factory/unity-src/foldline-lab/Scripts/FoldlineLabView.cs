using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.FoldlineLab
{
    public partial class FoldlineLabGame
    {
        static readonly Color Cobalt = MgfLook.Hex("#2B5DAA");
        static readonly Color Persimmon = MgfLook.Hex("#F36A3D");
        static readonly Color Cyan = MgfLook.Hex("#12B8C4");
        static readonly Color Cream = MgfLook.Hex("#F6F2DF");
        static readonly Color Graphite = MgfLook.Hex("#5A6373");
        static readonly Color White = Color.white;
        static readonly Color SkyBlue = MgfLook.Hex("#78C9ED");
        static readonly Color DeepBlue = MgfLook.Hex("#163B74");

        const float SailY = .72f;
        const int CloudCount = 24;
        const int HangingCount = 6;
        const int RippleCount = 8;

        Camera cam;
        Vector3 titleCamPos = new Vector3(9.4f, 10.8f, -14.6f);
        Vector3 playCamPos = new Vector3(0, 18f, -3.9f);
        Vector3 camBase;
        Transform worldRoot, stageRoot, sailRoot, fixedSail, foldPivot, foldSail, flagRoot, cordRoot;
        Transform titleHalo, scanner, guideHand, guideArrow, haloPoint, titleCueKnob;
        Transform dragHandleA, dragHandleB;
        readonly Transform[] clouds = new Transform[CloudCount];
        readonly Vector3[] cloudHome = new Vector3[CloudCount];
        readonly Transform[] vanes = new Transform[8];
        readonly Transform[] hangingSails = new Transform[HangingCount];
        readonly Transform[] pins = new Transform[4];
        readonly Transform[] guideDots = new Transform[12];
        readonly LineRenderer[] acceptedLines = new LineRenderer[2];
        readonly LineRenderer[] proofSpokes = new LineRenderer[3];
        readonly Transform[] seamClamps = new Transform[9];
        LineRenderer previewLine, cyanBand, orangeBand, correctGhost, haloRing, innerRing;
        LineRenderer tickA, tickB;
        Mesh fullMesh, fixedMesh, foldMesh;
        MeshFilter fixedFilter, foldFilter;
        MeshRenderer fixedRenderer, foldRenderer;
        Material cobaltMat, cobaltDarkMat, persimmonMat, creamMat, graphiteMat, whiteGlowMat;
        Material sailMat, sailFoldMat, cloudMat, cyanLineMat, orangeLineMat, whiteLineMat, flagMat;
        Texture2D mylar, titleArt;
        float worldTime, cameraBlend, foldVisualT, scannerVisual, haloVisual, haloRadius, bandMerge;
        float wrongShake, pinBounce, cordPull, titlePulse, guideBoost;
        bool folding, foldCorrect, haloActive, haloCircum;
        Vector2 haloCenter;

        // UI
        CanvasGroup titleG, playG, endG, evidenceG, toastG, tutorialG;
        RectTransform titleRt, titleArtRt, playRt, endRt, evidenceRt, toastRt, restartRt, rippleRoot, promptPlateRt, tutorialRt, masteryPlateRt;
        RawImage titleArtImage;
        TextMeshProUGUI logoBack, logoFront, titleTag, titleHint, titleBest;
        TextMeshProUGUI progressText, livesText, scoreText, masteryText, promptText, modeText, scannerText, tutorialText;
        readonly TextMeshProUGUI[] vertexLabels = new TextMeshProUGUI[3];
        TextMeshProUGUI evidenceTitle, evidenceBody, toastText, endTitle, endStats, endReason, restartText;
        readonly Image[] ripples = new Image[RippleCount];
        readonly float[] rippleLife = new float[RippleCount];
        Sprite roundSprite, ringSprite, flagSprite;
        int rippleCursor, lastW = -1, lastH = -1;
        float toastClock, evidenceClock, displayedScore, endCount;

        void BuildWorld()
        {
            // CreatePrimitive가 내부적으로 요구하는 콜라이더 타입을 엔진 스트리핑에서 보존한다.
            var keep = new GameObject("Collider type keep");
            keep.AddComponent<SphereCollider>(); keep.AddComponent<CapsuleCollider>(); keep.AddComponent<MeshCollider>();
            Destroy(keep);
            // 폐쇄형 돛 직물 공방: 기존 광학 프리즘 계열의 밝은 구름 바다와 분리한다.
            MgfLook.Sky(MgfLook.Hex("#1D3444"), MgfLook.Hex("#667B78"), MgfLook.Hex("#D8C7A1"), .72f);
            MgfLook.Sun(new Vector3(48, -35, 10), MgfLook.Hex("#FFE0A3"), 1.05f, .48f);
            cam = MgfLook.Camera(titleCamPos, new Vector3(0, .2f, .4f), 32f);
            cam.orthographic = true;
            cam.orthographicSize = 8.4f;
            camBase = cam.transform.position;

            mylar = Resources.Load<Texture2D>("FoldlineLab/mylar-noise");
            // 타이틀도 인게임과 같은 절차형 디오라마를 그대로 보여 준다.
            // 생성 원화는 ArtSource 참고 자료로만 남기고 런타임 화면에는 합성하지 않는다.
            titleArt = null;
            cobaltMat = MgfLook.Lit(Cobalt, .42f, .36f);
            cobaltDarkMat = MgfLook.Lit(DeepBlue, .36f, .46f);
            persimmonMat = MgfLook.Lit(Persimmon, .56f, .08f, Persimmon * .18f);
            creamMat = MgfLook.Lit(Cream, .55f, .02f);
            graphiteMat = MgfLook.Lit(Graphite, .34f, .4f);
            whiteGlowMat = MgfLook.Lit(White, .72f, .15f, White * 1.15f);
            cloudMat = MgfLook.Lit(MgfLook.Hex("#9B7454"), .22f, .02f);
            sailMat = MgfLook.Lit(MgfLook.Hex("#E9D6A7"), .28f, .01f, MgfLook.Hex("#251A0E") * .02f);
            sailFoldMat = MgfLook.Lit(MgfLook.Hex("#C96D43"), .32f, .02f, MgfLook.Hex("#32120A") * .03f);
            cyanLineMat = MgfLook.Unlit(MgfLook.Hex("#2F6663"));
            orangeLineMat = MgfLook.Unlit(MgfLook.Hex("#C44F2E"));
            whiteLineMat = MgfLook.Unlit(MgfLook.Hex("#FFF2C9"));
            flagMat = MgfLook.Lit(MgfLook.Hex("#D83D37"), .28f, .05f);

            worldRoot = new GameObject("Enclosed mechanical sail loft").transform;
            BuildCloudSea();
            BuildGantry();
            BuildStage();
            BuildSideStructures();
            BuildCord();
        }

        void BuildCloudSea()
        {
            var sea = new GameObject("Moving fabric roller gallery").transform;
            sea.SetParent(worldRoot, false);
            for (int i = 0; i < clouds.Length; i++)
            {
                float x = -16f + (i * 37 % 320) / 10f;
                float z = -10f + (i * 53 % 220) / 10f;
                float y = -2.3f - (i % 3) * .28f;
                Vector3 scale = new Vector3(.62f + (i % 3) * .13f, 1.5f + (i % 5) * .28f, .62f + (i % 3) * .13f);
                var c = MgfLook.Prim(PrimitiveType.Cylinder, "Rolled sail cloth", new Vector3(x, y, z), scale, cloudMat, sea, false).transform;
                c.localRotation = Quaternion.Euler(0, i * 23f, 90f);
                clouds[i] = c; cloudHome[i] = c.localPosition;
            }
        }

        void BuildGantry()
        {
            var g = new GameObject("Cobalt aerial truss").transform; g.SetParent(worldRoot, false);
            MgfLook.Block("North beam", new Vector3(0, 1.05f, 5.0f), new Vector3(11.8f, .38f, .46f), .09f, cobaltDarkMat, g);
            MgfLook.Block("South beam", new Vector3(0, 1.05f, -5.0f), new Vector3(11.8f, .38f, .46f), .09f, cobaltDarkMat, g);
            MgfLook.Block("West beam", new Vector3(-5.75f, 1.05f, 0), new Vector3(.46f, .38f, 10.4f), .09f, cobaltDarkMat, g);
            MgfLook.Block("East beam", new Vector3(5.75f, 1.05f, 0), new Vector3(.46f, .38f, 10.4f), .09f, cobaltDarkMat, g);
            for (int i = 0; i < 4; i++)
            {
                float sx = i < 2 ? -5.75f : 5.75f;
                float sz = (i % 2 == 0) ? -5f : 5f;
                MgfLook.Block("Suspension leg", new Vector3(sx, -1f, sz), new Vector3(.64f, 4.4f, .64f), .14f, cobaltMat, g);
                pins[i] = MgfLook.Prim(PrimitiveType.Sphere, "Ceramic pin", new Vector3(sx * .88f, 1.12f, sz * .86f), Vector3.one * .42f, persimmonMat, g, false).transform;
            }
            for (int i = -2; i <= 2; i++)
            {
                var cable = MakeLine("Cable", graphiteMat, .035f);
                SetLine(cable, new Vector2(i * 2.25f, -5), new Vector2(i * 2.25f, 5), .28f);
            }
        }

        void BuildStage()
        {
            stageRoot = new GameObject("Orthographic calibration frame").transform; stageRoot.SetParent(worldRoot, false);
            MgfLook.Block("Frame floor", new Vector3(0, .18f, 0), new Vector3(10.8f, .32f, 9.2f), .26f,
                MgfLook.Lit(MgfLook.Hex("#DCE8E5"), .25f, .03f), stageRoot);
            MgfLook.Block("Frame inset", new Vector3(0, .38f, 0), new Vector3(10.0f, .12f, 8.5f), .16f,
                MgfLook.Lit(MgfLook.Hex("#BDD1D2"), .22f, .02f), stageRoot);

            // 정밀 괘선. 배경은 회화가 아니라 프레임/구름 세계 자체다.
            for (int i = -4; i <= 4; i++)
            {
                var gx = MakeLine("Graphite grid X", graphiteMat, .018f);
                SetLine(gx, new Vector2(i * 1.05f, -4.15f), new Vector2(i * 1.05f, 4.15f), .47f);
            }
            for (int i = -3; i <= 3; i++)
            {
                var gz = MakeLine("Graphite grid Z", graphiteMat, .018f);
                SetLine(gz, new Vector2(-5f, i * 1.05f), new Vector2(5f, i * 1.05f), .47f);
            }

            sailRoot = new GameObject("Polarized triangular solar sail").transform; sailRoot.SetParent(stageRoot, false);
            fixedSail = new GameObject("Fixed sail half").transform; fixedSail.SetParent(sailRoot, false);
            fixedFilter = fixedSail.gameObject.AddComponent<MeshFilter>();
            fixedRenderer = fixedSail.gameObject.AddComponent<MeshRenderer>(); fixedRenderer.sharedMaterial = sailMat;
            fixedRenderer.shadowCastingMode = ShadowCastingMode.On; fixedRenderer.receiveShadows = true;
            foldPivot = new GameObject("Fold axis pivot").transform; foldPivot.SetParent(sailRoot, false);
            foldSail = new GameObject("Folding sail half").transform; foldSail.SetParent(foldPivot, false);
            foldFilter = foldSail.gameObject.AddComponent<MeshFilter>();
            foldRenderer = foldSail.gameObject.AddComponent<MeshRenderer>(); foldRenderer.sharedMaterial = sailFoldMat;
            foldRenderer.shadowCastingMode = ShadowCastingMode.On; foldRenderer.receiveShadows = true;
            foldSail.gameObject.SetActive(false);
            fullMesh = new Mesh { name = "Full solar sail" };
            fixedMesh = new Mesh { name = "Fixed clipped sail" };
            foldMesh = new Mesh { name = "Folding clipped sail" };
            fixedFilter.sharedMesh = fullMesh;

            previewLine = MakeLine("Live crease", cyanLineMat, .075f);
            cyanBand = MakeLine("Cyan polarized band", cyanLineMat, .12f);
            orangeBand = MakeLine("Vermilion polarized band", orangeLineMat, .12f);
            correctGhost = MakeLine("Correct dotted reveal", whiteLineMat, .052f);
            previewLine.gameObject.SetActive(false); cyanBand.gameObject.SetActive(false); orangeBand.gameObject.SetActive(false); correctGhost.gameObject.SetActive(false);
            tickA = MakeLine("Equal side tick A", graphiteMat, .055f);
            tickB = MakeLine("Equal side tick B", graphiteMat, .055f);
            tickA.gameObject.SetActive(false); tickB.gameObject.SetActive(false);
            dragHandleA = MgfLook.Prim(PrimitiveType.Sphere, "Crease handle A", Vector3.zero, Vector3.one * .42f, persimmonMat, stageRoot, false).transform;
            dragHandleB = MgfLook.Prim(PrimitiveType.Sphere, "Crease handle B", Vector3.zero, Vector3.one * .42f, persimmonMat, stageRoot, false).transform;
            dragHandleA.gameObject.SetActive(false); dragHandleB.gameObject.SetActive(false);
            for (int i = 0; i < acceptedLines.Length; i++)
            {
                acceptedLines[i] = MakeLine("Accepted white crease " + i, whiteLineMat, .045f);
                acceptedLines[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < seamClamps.Length; i++)
            {
                seamClamps[i] = MgfLook.Block("Brass seam clamp " + i, Vector3.zero,
                    new Vector3(.22f, .16f, .44f), .05f, persimmonMat, stageRoot).transform;
                seamClamps[i].gameObject.SetActive(false);
            }

            scanner = MgfLook.Block("Heat scanner", new Vector3(0, .98f, 4.25f), new Vector3(10.2f, .13f, .2f), .06f, persimmonMat, stageRoot).transform;
            haloRing = MakeCircle("Outer halo frame", whiteLineMat, 128, .12f);
            innerRing = MakeCircle("Inner halo frame", cyanLineMat, 96, .06f);
            haloRing.gameObject.SetActive(false); innerRing.gameObject.SetActive(false);
            haloPoint = MgfLook.Prim(PrimitiveType.Sphere, "Halo center flare", Vector3.zero, Vector3.one * .22f, whiteGlowMat, stageRoot, false).transform;
            haloPoint.gameObject.SetActive(false);
            for (int i = 0; i < proofSpokes.Length; i++)
            {
                proofSpokes[i] = MakeLine("Equal-distance measuring rib " + i, i == 0 ? orangeLineMat : graphiteMat, .065f);
                proofSpokes[i].gameObject.SetActive(false);
            }

            BuildFlag();
            BuildGuide();
        }

        void BuildFlag()
        {
            flagRoot = new GameObject("Red physical safety flag").transform; flagRoot.SetParent(stageRoot, false);
            flagRoot.localPosition = new Vector3(4.05f, .9f, -3.15f);
            MgfLook.Block("Flag pole", new Vector3(0, .36f, 0), new Vector3(.12f, .75f, .12f), .04f, graphiteMat, flagRoot);
            var f = new GameObject("Flag cloth"); f.transform.SetParent(flagRoot, false); f.transform.localPosition = new Vector3(.38f, .66f, 0);
            var mf = f.AddComponent<MeshFilter>(); mf.sharedMesh = QuadMesh(.85f, .52f);
            var mr = f.AddComponent<MeshRenderer>(); mr.sharedMaterial = flagMat;
        }

        void BuildGuide()
        {
            guideHand = MgfLook.Prim(PrimitiveType.Sphere, "Ghost fingertip", new Vector3(0, 1.12f, 0), Vector3.one * .28f, whiteGlowMat, stageRoot, false).transform;
            guideArrow = MgfLook.Prim(PrimitiveType.Cube, "Guide arrowhead", new Vector3(0, 1.1f, 0), new Vector3(.28f, .07f, .38f), whiteGlowMat, stageRoot, false).transform;
            guideHand.gameObject.SetActive(false); guideArrow.gameObject.SetActive(false);
            for (int i = 0; i < guideDots.Length; i++)
            {
                guideDots[i] = MgfLook.Prim(PrimitiveType.Sphere, "Flowing guide chevron", Vector3.zero, Vector3.one * .08f, whiteGlowMat, stageRoot, false).transform;
                guideDots[i].gameObject.SetActive(false);
            }
        }

        void BuildSideStructures()
        {
            // 가로 화면에서 왼쪽 방향계 / 중앙 조율판 / 오른쪽 완성 헤일로가 3단 구조를 이룬다.
            var left = new GameObject("Wind direction observatory").transform; left.SetParent(worldRoot, false); left.localPosition = new Vector3(-8.4f, 0, .5f);
            MgfLook.Block("Vane mast", new Vector3(0, 2.5f, 0), new Vector3(.28f, 6.2f, .28f), .08f, cobaltDarkMat, left);
            for (int i = 0; i < 4; i++)
            {
                var v = new GameObject("Persimmon vane").transform; v.SetParent(left, false); v.localPosition = new Vector3(0, 1.0f + i * 1.15f, 0);
                MgfLook.Block("Vane blade", new Vector3(.65f, 0, 0), new Vector3(1.35f, .14f, .42f), .06f, persimmonMat, v);
                vanes[i] = v;
            }
            var right = new GameObject("Stored measuring rib rack").transform; right.SetParent(worldRoot, false); right.localPosition = new Vector3(8.35f, 0, .5f);
            MgfLook.Block("Rib rack mast", new Vector3(0, 2.4f, 0), new Vector3(.26f, 6f, .26f), .08f, cobaltDarkMat, right);
            for (int i = 0; i < 4; i++)
            {
                var lr = MakeLine("Stored measuring rib", i % 2 == 0 ? graphiteMat : orangeLineMat, .09f, right);
                float z = -.9f + i * 1.55f;
                SetLine(lr, new Vector2(-.72f, z - .2f), new Vector2(.72f, z + .2f), 1.25f);
                vanes[4 + i] = lr.transform;
            }
            for (int i = 0; i < hangingSails.Length; i++)
            {
                float x = -5.5f + i * 2.2f;
                var h = new GameObject("Queued sun sail " + (i + 1)).transform; h.SetParent(worldRoot, false); h.localPosition = new Vector3(x, 4.0f + (i % 2) * .35f, 6.5f);
                var mf = h.gameObject.AddComponent<MeshFilter>(); mf.sharedMesh = TriangleMesh(new Vector2(-.45f, -.3f), new Vector2(.45f, -.3f), new Vector2(0, .55f), 0f);
                var mr = h.gameObject.AddComponent<MeshRenderer>(); mr.sharedMaterial = i % 2 == 0 ? sailMat : sailFoldMat;
                hangingSails[i] = h;
            }
        }

        void BuildCord()
        {
            cordRoot = new GameObject("Title pull cord").transform; cordRoot.SetParent(worldRoot, false); cordRoot.localPosition = new Vector3(0, 1.2f, -5.8f);
            MgfLook.Block("Cord", new Vector3(0, -.9f, 0), new Vector3(.1f, 1.9f, .1f), .03f, graphiteMat, cordRoot);
            MgfLook.Prim(PrimitiveType.Sphere, "Ceramic pull", new Vector3(0, -1.9f, 0), new Vector3(.75f, .42f, .55f), persimmonMat, cordRoot, true);
            titleCueKnob = MgfLook.Prim(PrimitiveType.Sphere, "Animated pull cue", new Vector3(0, -1.55f, -.18f), Vector3.one * .22f, whiteGlowMat, cordRoot, false).transform;
            titleHalo = new GameObject("Title rotating winding wheel").transform; titleHalo.SetParent(worldRoot, false); titleHalo.localPosition = new Vector3(0, 2.3f, 1.2f);
            var h1 = MakeCircle("Winding wheel rim", graphiteMat, 96, .13f, titleHalo); SetCircle(h1, Vector2.zero, 4.35f, 0f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var spoke = MakeLine("Winding wheel spoke", i % 2 == 0 ? orangeLineMat : graphiteMat, .07f, titleHalo);
                SetLine(spoke, Vector2.zero, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 4.15f, .01f);
            }
        }

        void BuildUi()
        {
            roundSprite = MakeSprite(96, 22, 0);
            ringSprite = MakeSprite(96, 40, 1);
            flagSprite = MakeSprite(96, 10, 2);
            var canvas = MgfText.Canvas;
            var root = R("Foldline UI", canvas.transform, Vector2.one * .5f, Vector2.zero, new Vector2(390, 844));
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;

            titleRt = ScreenRoot("Title", root); titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            titleArtRt = R("Generated stratosphere key art", titleRt, Vector2.one * .5f, Vector2.zero, new Vector2(390, 844));
            titleArtImage = titleArtRt.gameObject.AddComponent<RawImage>();
            titleArtImage.texture = titleArt; titleArtImage.raycastTarget = false;
            titleArtImage.color = titleArt ? Color.white : new Color(1, 1, 1, 0);
            var titleFit = titleArtRt.gameObject.AddComponent<AspectRatioFitter>();
            titleFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            titleFit.aspectRatio = titleArt ? titleArt.width / (float)titleArt.height : .667f;
            logoBack = Txt(titleRt, "폴 드 라 인", new Vector2(.5f, .78f), new Vector2(2, -2), 49, DeepBlue, 370, TextAlignmentOptions.Center);
            logoBack.characterSpacing = 12; logoBack.fontStyle = FontStyles.Bold;
            logoFront = Txt(titleRt, "폴 드 라 인", new Vector2(.5f, .78f), Vector2.zero, 49, Cream, 370, TextAlignmentOptions.Center);
            logoFront.characterSpacing = 12; logoFront.fontStyle = FontStyles.Bold;
            var titleRibbon = Img(titleRt, "Hanging cloth title ribbon", new Vector2(.5f, .64f), Vector2.zero,
                new Vector2(350, 46), new Color(DeepBlue.r, DeepBlue.g, DeepBlue.b, .90f), roundSprite);
            titleTag = Txt(titleRibbon.transform, "접는 선을 그어 중심 리브를 잠가라", Vector2.one * .5f, Vector2.zero, 18, Cream, 330);
            titleHint = Txt(titleRt, "●  당김끈을 아래로 끌어 돛 공방 열기", new Vector2(.5f, .10f), Vector2.zero, 18, DeepBlue, 360);
            titleBest = Txt(titleRt, "중학교 2학년 · 삼각형의 성질", new Vector2(.5f, .045f), Vector2.zero, 14, Graphite, 350);

            playRt = ScreenRoot("Play", root); playG = playRt.gameObject.AddComponent<CanvasGroup>();
            progressText = Txt(playRt, "돛 1/6", new Vector2(.12f, .955f), Vector2.zero, 16, DeepBlue, 100);
            livesText = Txt(playRt, "오접힘 ◇◇◇", new Vector2(.50f, .955f), Vector2.zero, 16, DeepBlue, 170);
            scoreText = Txt(playRt, "점수 0", new Vector2(.79f, .955f), Vector2.zero, 16, Persimmon, 120);
            var masteryBg = Img(playRt, "Mastery status plate", new Vector2(.5f, .915f), Vector2.zero, new Vector2(356, 38), new Color(Cream.r, Cream.g, Cream.b, .94f), roundSprite);
            masteryPlateRt = masteryBg.rectTransform;
            masteryText = Txt(masteryPlateRt, "첫 시도 0/5 · 남은 허용 실수 1회", Vector2.one * .5f, Vector2.zero, 13, DeepBlue, 342);
            var promptBg = Img(playRt, "Prompt sailplate", new Vector2(.5f, .865f), Vector2.zero, new Vector2(362, 82), new Color(Cream.r, Cream.g, Cream.b, .94f), roundSprite);
            promptPlateRt = promptBg.rectTransform;
            promptText = Txt(promptBg.transform, "", new Vector2(.5f, .55f), Vector2.zero, 18, DeepBlue, 334);
            promptText.textWrappingMode = TextWrappingModes.Normal;
            modeText = Txt(promptBg.transform, "", new Vector2(.5f, .12f), Vector2.zero, 12, Graphite, 330);
            scannerText = Txt(playRt, "열 스캐너", new Vector2(.5f, .79f), Vector2.zero, 12, Persimmon, 180);
            for (int i = 0; i < vertexLabels.Length; i++)
            {
                vertexLabels[i] = Txt(playRt, ((char)('A' + i)).ToString(), Vector2.zero, Vector2.zero, 22, DeepBlue, 48);
                vertexLabels[i].fontStyle = FontStyles.Bold;
            }

            evidenceRt = R("Evidence", playRt, new Vector2(.5f, .22f), Vector2.zero, new Vector2(360, 104));
            var evidenceImg = evidenceRt.gameObject.AddComponent<Image>(); evidenceImg.sprite = roundSprite; evidenceImg.type = Image.Type.Sliced; evidenceImg.color = new Color(1, 1, 1, .95f); evidenceImg.raycastTarget = false;
            evidenceG = evidenceRt.gameObject.AddComponent<CanvasGroup>(); evidenceG.alpha = 0;
            evidenceTitle = Txt(evidenceRt, "", new Vector2(.5f, .72f), Vector2.zero, 20, DeepBlue, 334);
            evidenceBody = Txt(evidenceRt, "", new Vector2(.5f, .34f), Vector2.zero, 14, Graphite, 334);
            evidenceBody.textWrappingMode = TextWrappingModes.Normal;

            toastRt = R("Toast", playRt, new Vector2(.5f, .115f), Vector2.zero, new Vector2(350, 56));
            var toastImg = toastRt.gameObject.AddComponent<Image>(); toastImg.sprite = roundSprite; toastImg.type = Image.Type.Sliced; toastImg.color = new Color(Cobalt.r, Cobalt.g, Cobalt.b, .93f); toastImg.raycastTarget = false;
            toastG = toastRt.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0;
            toastText = Txt(toastRt, "", Vector2.one * .5f, Vector2.zero, 14, White, 326);

            tutorialRt = R("Practice three-step guide", playRt, new Vector2(.5f, .14f), Vector2.zero, new Vector2(360, 86));
            var tutorialImg = tutorialRt.gameObject.AddComponent<Image>(); tutorialImg.sprite = roundSprite; tutorialImg.type = Image.Type.Sliced;
            tutorialImg.color = new Color(DeepBlue.r, DeepBlue.g, DeepBlue.b, .94f); tutorialImg.raycastTarget = false;
            tutorialG = tutorialRt.gameObject.AddComponent<CanvasGroup>(); tutorialG.alpha = 0;
            tutorialText = Txt(tutorialRt, "① 시작점 누르기\n② 점선을 따라 끌기  →  ③ 끝점에서 놓기", Vector2.one * .5f, Vector2.zero, 14, White, 332);
            tutorialText.textWrappingMode = TextWrappingModes.Normal;

            rippleRoot = R("Input ripples", root, Vector2.zero, Vector2.zero, Vector2.zero);
            rippleRoot.anchorMin = Vector2.zero; rippleRoot.anchorMax = Vector2.one; rippleRoot.offsetMin = rippleRoot.offsetMax = Vector2.zero;
            for (int i = 0; i < ripples.Length; i++)
            {
                ripples[i] = Img(rippleRoot, "Ripple", Vector2.zero, Vector2.zero, new Vector2(64, 64), new Color(Cyan.r, Cyan.g, Cyan.b, 0), ringSprite);
                ripples[i].rectTransform.pivot = Vector2.one * .5f; rippleLife[i] = 9f;
            }

            endRt = ScreenRoot("End", root); endG = endRt.gameObject.AddComponent<CanvasGroup>();
            var endCard = Img(endRt, "Result optical plate", new Vector2(.5f, .35f), Vector2.zero, new Vector2(356, 300), new Color(Cream.r, Cream.g, Cream.b, .96f), roundSprite);
            endTitle = Txt(endCard.transform, "", new Vector2(.5f, .78f), Vector2.zero, 34, DeepBlue, 330);
            endStats = Txt(endCard.transform, "", new Vector2(.5f, .54f), Vector2.zero, 22, Persimmon, 326);
            endReason = Txt(endCard.transform, "", new Vector2(.5f, .34f), Vector2.zero, 15, Graphite, 326);
            restartRt = R("Restart", endCard.transform, new Vector2(.5f, .12f), Vector2.zero, new Vector2(270, 60));
            var rr = restartRt.gameObject.AddComponent<Image>(); rr.sprite = roundSprite; rr.type = Image.Type.Sliced; rr.color = Cobalt; rr.raycastTarget = false;
            restartText = Txt(restartRt, "다시 조율", Vector2.one * .5f, Vector2.zero, 19, White, 250);
            SetScreen();
        }

        RectTransform ScreenRoot(string name, Transform parent)
        {
            var rt = R(name, parent, Vector2.one * .5f, Vector2.zero, new Vector2(390, 844));
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = Vector2.one * .5f; rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite)
        {
            var rt = R(name, parent, anchor, pos, size); var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.type = sprite && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple; img.color = color; img.raycastTarget = false; return img;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color, float width, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 3.0f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis; t.text = text; return t;
        }

        void SetScreen()
        {
            bool title = flow == Flow.Title, end = flow == Flow.End;
            titleG.alpha = title ? 1 : 0; titleG.blocksRaycasts = title;
            playG.alpha = (!title && !end) ? 1 : 0; playG.blocksRaycasts = !title && !end;
            endG.alpha = end ? 1 : 0; endG.blocksRaycasts = end;
            if (titleHalo) titleHalo.gameObject.SetActive(title);
            if (cordRoot) cordRoot.gameObject.SetActive(title);
            if (titleCueKnob) titleCueKnob.gameObject.SetActive(title);
            for (int i = 0; i < vertexLabels.Length; i++) if (vertexLabels[i]) vertexLabels[i].gameObject.SetActive(!title && !end);
            cameraBlend = title ? 0f : 1f;
            lastW = -1; // 같은 뷰포트에서도 title→play 전환 시 카메라·3존 UI를 다시 계산한다.
            cam.transform.position = title ? titleCamPos : playCamPos;
            cam.transform.LookAt(new Vector3(0, .2f, .2f));
            ApplyLayoutIfNeeded();
            if (title)
            {
                int best = PlayerPrefs.GetInt("foldline_best", 0);
                titleBest.text = "중학교 2학년 · 삼각형의 성질 · 최고 첫 시도 " + best + "/6";
            }
        }

        void SetGoal(string text)
        {
            promptText.text = text;
            modeText.text = task == null ? "" : task.kind == FoldTaskKind.VertexPair ? "꼭짓점 포개기 → 수직이등분선 → 외심 O"
                : task.kind == FoldTaskKind.SidePair ? "변 포개기 → 각의 이등분선 → 내심 I"
                : task.kind == FoldTaskKind.Impossible ? "세 변 3·4·5 cm가 모두 다름 → 빨간 안전 깃"
                : "같은 두 변의 끼인각을 찾으시오";
        }

        void LoadTaskVisual(FoldTask t, bool isPractice)
        {
            BuildFullTriangle(t.a, t.b, t.c);
            foldSail.gameObject.SetActive(false); fixedSail.gameObject.SetActive(true);
            previewLine.gameObject.SetActive(false); cyanBand.gameObject.SetActive(false); orangeBand.gameObject.SetActive(false); correctGhost.gameObject.SetActive(false);
            haloRing.gameObject.SetActive(false); innerRing.gameObject.SetActive(false); haloPoint.gameObject.SetActive(false); haloActive = false;
            for (int i = 0; i < acceptedLines.Length; i++)
            {
                bool on = i < acceptedCreases.Count;
                acceptedLines[i].gameObject.SetActive(on);
                if (on) SetInfiniteLine(acceptedLines[i], acceptedCreases[i], .93f);
            }
            flagRoot.gameObject.SetActive(t.kind == FoldTaskKind.Impossible);
            flagRoot.localPosition = new Vector3(4.05f, .9f, -3.15f);
            scannerVisual = practice ? 0f : Mathf.Clamp01(runClock / FoldlineRules.SessionSeconds);
            scanner.localPosition = new Vector3(0, .98f, Mathf.Lerp(4.25f, -4.25f, scannerVisual));
            ApplySailColor(t.kind == FoldTaskKind.Impossible ? .55f : 0f);
            SetTickMarks(t);
        }

        void BuildFullTriangle(Vector2 a, Vector2 b, Vector2 c)
        {
            AssignMesh(fullMesh, new List<Vector2> { a, b, c }, Vector2.zero);
            fixedFilter.sharedMesh = fullMesh;
            fixedSail.localPosition = Vector3.zero; fixedSail.localRotation = Quaternion.identity;
        }

        void ResetStageVisuals()
        {
            var p = FoldlineRules.Practice();
            BuildFullTriangle(p.a, p.b, p.c);
            fixedSail.gameObject.SetActive(true); foldSail.gameObject.SetActive(false);
            flagRoot.gameObject.SetActive(false);
            previewLine.gameObject.SetActive(false); cyanBand.gameObject.SetActive(false); orangeBand.gameObject.SetActive(false); correctGhost.gameObject.SetActive(false);
            haloRing.gameObject.SetActive(false); innerRing.gameObject.SetActive(false); haloPoint.gameObject.SetActive(false);
            tickA.gameObject.SetActive(false); tickB.gameObject.SetActive(false);
            dragHandleA.gameObject.SetActive(false); dragHandleB.gameObject.SetActive(false);
            for (int i = 0; i < acceptedLines.Length; i++) acceptedLines[i].gameObject.SetActive(false);
            for (int i = 0; i < seamClamps.Length; i++) seamClamps[i].gameObject.SetActive(false);
            for (int i = 0; i < proofSpokes.Length; i++) proofSpokes[i].gameObject.SetActive(false);
            scanner.localPosition = new Vector3(0, .98f, 4.25f);
        }

        void ApplySailColor(float warning)
        {
            var block = new MaterialPropertyBlock();
            fixedRenderer.GetPropertyBlock(block);
            Color c = Color.Lerp(new Color(.78f, .95f, .94f, 1), new Color(.98f, .72f, .5f, 1), warning);
            block.SetColor("_Color", c); block.SetColor("_EmissionColor", c * .08f); fixedRenderer.SetPropertyBlock(block);
        }

        void BeginLinePreview(Vector2 start)
        {
            previewLine.gameObject.SetActive(true);
            dragHandleA.gameObject.SetActive(true); dragHandleB.gameObject.SetActive(true);
            dragHandleA.localPosition = new Vector3(start.x, 1.02f, start.y);
            dragHandleB.localPosition = new Vector3(start.x, 1.02f, start.y);
            SetLine(previewLine, start, start + Vector2.up * .02f, .94f);
            guideHand.gameObject.SetActive(false); guideArrow.gameObject.SetActive(false);
            for (int i = 0; i < guideDots.Length; i++) guideDots[i].gameObject.SetActive(false);
            MgfSfx.Play("tap", .18f);
        }

        void UpdateLinePreview(Vector2 a, Vector2 b)
        {
            if (Vector2.Distance(a, b) < .03f) return;
            var l = new FoldLine(a, b - a);
            SetInfiniteLine(previewLine, l, .96f);
            dragHandleA.localPosition = new Vector3(a.x, 1.02f, a.y);
            dragHandleB.localPosition = new Vector3(b.x, 1.02f, b.y);
            // 선을 끄는 동안 접힐 쪽 윤곽은 움직이지만 정답 근접색/스냅은 없다.
            PreviewGhostReflection(l);
        }

        void PreviewGhostReflection(FoldLine l)
        {
            Vector2 center = (task.a + task.b + task.c) / 3f;
            Vector2 r = FoldlineRules.Reflect(center, l);
            float side = Mathf.Sign(Vector2.Dot(l.Normal, center) - l.Offset);
            foldVisualT = Mathf.Lerp(foldVisualT, side * Vector2.Distance(center, r), .2f);
        }

        void FinishLinePreview() { previewLine.gameObject.SetActive(false); dragHandleA.gameObject.SetActive(false); dragHandleB.gameObject.SetActive(false); }

        void SetTickMarks(FoldTask t)
        {
            bool on = t != null && t.kind == FoldTaskKind.Symmetry;
            tickA.gameObject.SetActive(on); tickB.gameObject.SetActive(on);
            if (!on) return;
            SetSideTick(tickA, t.a, t.b);
            SetSideTick(tickB, t.a, t.c);
        }

        void SetSideTick(LineRenderer lr, Vector2 a, Vector2 b)
        {
            Vector2 m = (a + b) * .5f;
            Vector2 d = (b - a).normalized;
            Vector2 n = new Vector2(-d.y, d.x) * .22f;
            SetLine(lr, m - n, m + n, 1.01f);
        }

        void BeginFoldAnimation(FoldLine line, bool correct, bool showHalo, bool circum, List<FoldLine> creases)
        {
            folding = line != null;
            foldCorrect = correct;
            foldVisualT = 0f; bandMerge = 0f;
            if (line != null)
            {
                SetupSplitMeshes(line);
                foldPivot.localPosition = StageWorld(line.point) - stageRoot.position;
                foldPivot.localRotation = Quaternion.identity;
                foldSail.localRotation = Quaternion.identity;
                foldSail.gameObject.SetActive(true);
                fixedSail.gameObject.SetActive(true);
                SetInfiniteLine(cyanBand, FoldlineRules.Shift(line, .22f), 1.03f);
                SetInfiniteLine(orangeBand, FoldlineRules.Shift(line, -.22f), 1.04f);
                cyanBand.gameObject.SetActive(!correct); orangeBand.gameObject.SetActive(!correct);
                if (correct)
                {
                    SegmentInsideFrame(line, out Vector2 seamA, out Vector2 seamB);
                    float yaw = -Mathf.Atan2((seamB - seamA).y, (seamB - seamA).x) * Mathf.Rad2Deg;
                    for (int i = 0; i < seamClamps.Length; i++)
                    {
                        float u = (i + 1f) / (seamClamps.Length + 1f);
                        Vector2 p = Vector2.Lerp(seamA, seamB, u);
                        seamClamps[i].localPosition = new Vector3(p.x, 1.05f, p.y);
                        seamClamps[i].localRotation = Quaternion.Euler(0, yaw, 0);
                        seamClamps[i].localScale = Vector3.one * .15f;
                        seamClamps[i].gameObject.SetActive(true);
                    }
                }
            }
            if (!correct && task.target != null)
            {
                correctGhost.gameObject.SetActive(true);
                SetInfiniteLine(correctGhost, task.target, 1.08f);
            }
            if (showHalo && creases.Count >= 2 && FoldlineRules.Intersect(creases[0], creases[1], out Vector2 center))
            {
                haloActive = true; haloCircum = circum; haloCenter = center; haloVisual = 0f;
                Vector2[] ends = circum
                    ? new[] { task.a, task.b, task.c }
                    : new[] { FootOnLine(center, task.a, task.b), FootOnLine(center, task.b, task.c), FootOnLine(center, task.c, task.a) };
                float min = float.MaxValue, max = 0f;
                for (int i = 0; i < proofSpokes.Length; i++)
                {
                    float d = Vector2.Distance(center, ends[i]); min = Mathf.Min(min, d); max = Mathf.Max(max, d);
                    SetLine(proofSpokes[i], center, ends[i], 1.08f);
                    proofSpokes[i].gameObject.SetActive(true);
                }
                st.exactCenterSpread = max - min;
                haloRing.gameObject.SetActive(false); innerRing.gameObject.SetActive(false); haloPoint.gameObject.SetActive(true);
            }
        }

        void StepFoldAnimation(float t, float duration, bool correct)
        {
            float k = Mathf.Clamp01(t / Mathf.Max(.01f, duration));
            float foldK = EaseBack(Mathf.Clamp01(k / .44f));
            if (folding)
            {
                // stageRoot가 회전하지 않으므로 제출선의 월드 축을 local 회전축으로 그대로 쓴다.
                Vector3 axis = new Vector3(submitted.direction.x, 0, submitted.direction.y).normalized;
                foldPivot.localRotation = Quaternion.AngleAxis(180f * foldK, axis);
                float squash = 1f - .08f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(k / .44f));
                foldSail.localScale = new Vector3(1f / squash, squash, 1f);
                bandMerge = correct ? Mathf.Clamp01((k - .22f) / .25f) : .15f * Mathf.Sin(k * Mathf.PI);
                float sep = Mathf.Lerp(.22f, correct ? 0f : .28f, bandMerge);
                SetInfiniteLine(cyanBand, FoldlineRules.Shift(submitted, sep), 1.03f);
                SetInfiniteLine(orangeBand, FoldlineRules.Shift(submitted, -sep), 1.04f);
            }
            if (haloActive)
            {
                haloVisual = EaseOutBack(Mathf.Clamp01((k - .28f) / .62f));
                haloPoint.localPosition = StageWorld(haloCenter) - stageRoot.position + Vector3.up * .45f;
                haloPoint.localScale = Vector3.one * (.18f + haloVisual * .65f);
                for (int i = 0; i < seamClamps.Length; i++) if (seamClamps[i].gameObject.activeSelf)
                    seamClamps[i].localScale = Vector3.one * Mathf.Lerp(.15f, 1f, haloVisual);
            }
        }

        void ResetAfterFold(bool accepted)
        {
            folding = false; foldSail.gameObject.SetActive(false); fixedSail.gameObject.SetActive(true); foldSail.localScale = Vector3.one;
            foldPivot.localRotation = Quaternion.identity;
            cyanBand.gameObject.SetActive(false); orangeBand.gameObject.SetActive(false); correctGhost.gameObject.SetActive(false);
            if (accepted && submitted != null && !task.haloAfter)
            {
                int at = Mathf.Clamp(acceptedCreases.Count - 1, 0, acceptedLines.Length - 1);
                acceptedLines[at].gameObject.SetActive(true); SetInfiniteLine(acceptedLines[at], submitted, .93f);
            }
            haloActive = false; haloRing.gameObject.SetActive(false); innerRing.gameObject.SetActive(false); haloPoint.gameObject.SetActive(false);
            for (int i = 0; i < seamClamps.Length; i++) seamClamps[i].gameObject.SetActive(false);
            for (int i = 0; i < proofSpokes.Length; i++) proofSpokes[i].gameObject.SetActive(false);
            BuildFullTriangle(task.a, task.b, task.c);
        }

        void SetupSplitMeshes(FoldLine line)
        {
            var tri = new List<Vector2> { task.a, task.b, task.c };
            var positive = Clip(tri, line, true);
            var negative = Clip(tri, line, false);
            if (positive.Count < 3 || negative.Count < 3)
            {
                positive = tri; negative = tri;
            }
            AssignMesh(fixedMesh, negative, Vector2.zero);
            AssignMesh(foldMesh, positive, line.point);
            fixedFilter.sharedMesh = fixedMesh; foldFilter.sharedMesh = foldMesh;
        }

        static List<Vector2> Clip(List<Vector2> src, FoldLine line, bool keepPositive)
        {
            var dst = new List<Vector2>(6);
            for (int i = 0; i < src.Count; i++)
            {
                Vector2 a = src[i], b = src[(i + 1) % src.Count];
                float da = Vector2.Dot(line.Normal, a) - line.Offset;
                float db = Vector2.Dot(line.Normal, b) - line.Offset;
                bool ina = keepPositive ? da >= 0 : da <= 0;
                bool inb = keepPositive ? db >= 0 : db <= 0;
                if (ina) dst.Add(a);
                if (ina != inb)
                {
                    float q = da / (da - db);
                    dst.Add(Vector2.Lerp(a, b, q));
                }
            }
            return dst;
        }

        void AssignMesh(Mesh mesh, List<Vector2> poly, Vector2 pivot)
        {
            mesh.Clear();
            if (poly == null || poly.Count < 3) return;
            var verts = new List<Vector3>(poly.Count * 2);
            var norms = new List<Vector3>(poly.Count * 2);
            var uvs = new List<Vector2>(poly.Count * 2);
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 p = poly[i] - pivot;
                verts.Add(new Vector3(p.x, SailY, p.y)); norms.Add(Vector3.up); uvs.Add(new Vector2((poly[i].x + 5.5f) / 11f, (poly[i].y + 4.5f) / 9f));
            }
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 p = poly[i] - pivot;
                verts.Add(new Vector3(p.x, SailY - .035f, p.y)); norms.Add(Vector3.down); uvs.Add(new Vector2((poly[i].x + 5.5f) / 11f, (poly[i].y + 4.5f) / 9f));
            }
            var tris = new List<int>((poly.Count - 2) * 6);
            for (int i = 1; i < poly.Count - 1; i++) { tris.Add(0); tris.Add(i + 1); tris.Add(i); }
            int o = poly.Count;
            for (int i = 1; i < poly.Count - 1; i++) { tris.Add(o); tris.Add(o + i); tris.Add(o + i + 1); }
            mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
        }

        static Mesh TriangleMesh(Vector2 a, Vector2 b, Vector2 c, float y)
        {
            var m = new Mesh { name = "Small sail" };
            m.vertices = new[] { new Vector3(a.x, y, a.y), new Vector3(b.x, y, b.y), new Vector3(c.x, y, c.y), new Vector3(a.x, y - .02f, a.y), new Vector3(c.x, y - .02f, c.y), new Vector3(b.x, y - .02f, b.y) };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.down, Vector3.down, Vector3.down };
            m.uv = new[] { Vector2.zero, Vector2.right, new Vector2(.5f, 1), Vector2.zero, new Vector2(.5f, 1), Vector2.right };
            m.triangles = new[] { 0, 2, 1, 3, 4, 5 }; return m;
        }

        static Mesh QuadMesh(float w, float h)
        {
            var m = new Mesh { name = "Flag cloth" };
            m.vertices = new[] { new Vector3(-w/2, -h/2, 0), new Vector3(w/2, -h/2, 0), new Vector3(w/2, h/2, 0), new Vector3(-w/2, h/2, 0), new Vector3(-w/2, -h/2, -.02f), new Vector3(-w/2, h/2, -.02f), new Vector3(w/2, h/2, -.02f), new Vector3(w/2, -h/2, -.02f) };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back, Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
            m.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up, Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 }; return m;
        }

        LineRenderer MakeLine(string name, Material mat, float width, Transform parent = null)
        {
            var go = new GameObject(name); go.transform.SetParent(parent ? parent : stageRoot, false);
            var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = mat; lr.useWorldSpace = false; lr.widthMultiplier = width;
            lr.positionCount = 2; lr.numCapVertices = 4; lr.numCornerVertices = 2; lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
            return lr;
        }

        LineRenderer MakeCircle(string name, Material mat, int segments, float width, Transform parent = null)
        {
            var lr = MakeLine(name, mat, width, parent); lr.loop = true; lr.positionCount = segments; return lr;
        }

        void SetLine(LineRenderer lr, Vector2 a, Vector2 b, float y)
        {
            lr.positionCount = 2; lr.SetPosition(0, new Vector3(a.x, y, a.y)); lr.SetPosition(1, new Vector3(b.x, y, b.y));
        }

        void SetInfiniteLine(LineRenderer lr, FoldLine line, float y)
        {
            float len = 8.2f;
            SetLine(lr, line.point - line.direction * len, line.point + line.direction * len, y);
        }

        void SetCircle(LineRenderer lr, Vector2 center, float radius, float y)
        {
            for (int i = 0; i < lr.positionCount; i++)
            {
                float a = i * Mathf.PI * 2f / lr.positionCount;
                lr.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * radius, y, center.y + Mathf.Sin(a) * radius));
            }
        }

        void SetScannerProgress(float p)
        {
            scannerVisual = Mathf.Clamp01(p);
            st.scannerStep = Mathf.FloorToInt(scannerVisual * 12f);
        }

        void ShowGuide(bool on)
        {
            bool visible = on && task != null && task.kind != FoldTaskKind.Impossible;
            guideHand.gameObject.SetActive(visible); guideArrow.gameObject.SetActive(visible);
            for (int i = 0; i < guideDots.Length; i++) guideDots[i].gameObject.SetActive(visible);
            guideBoost = visible ? 1f : 0f;
        }

        void PointAtTarget() { guideBoost = 1.4f; ShowGuide(task != null && task.kind != FoldTaskKind.Impossible); }
        void BouncePins() { pinBounce = .45f; }
        void ShakeWrong(float amount) { wrongShake = Mathf.Max(wrongShake, amount); }

        void ShowCorrectEvidence(string text)
        {
            evidenceTitle.text = "중심 리브 체결";
            evidenceTitle.color = Cobalt; evidenceBody.text = text; evidenceClock = resolveDuration; evidenceG.alpha = 1;
            toastClock = 0f; toastG.alpha = 0f;
        }

        void ShowWrongEvidence(string text)
        {
            evidenceTitle.text = "접힌 두 면이 어긋났다";
            evidenceTitle.color = Persimmon; evidenceBody.text = text; evidenceClock = resolveDuration; evidenceG.alpha = 1;
            toastClock = 0f; toastG.alpha = 0f;
        }

        void HideEvidence() { evidenceClock = 0; evidenceG.alpha = 0; }

        void Refuse(string text)
        {
            toastText.text = text; toastClock = 1.55f; toastG.alpha = 1; MgfSfx.Play("tap", .12f); guideBoost = Mathf.Max(guideBoost, .65f);
        }

        void ShowPracticeAssist()
        {
            toastText.text = "연습 보정 ON · 손동작을 흰 점선에 맞춰 접는다";
            toastClock = 1.2f; toastG.alpha = 1;
        }

        void ShowEnd(bool clear, string reason)
        {
            SetScreen(); endTitle.text = clear ? "중심 리브 체결 완료" : "조율 프레임 잠금";
            endTitle.color = clear ? Cobalt : Persimmon; endReason.text = reason; endCount = 0;
            endStats.text = "0점 · 첫 시도 0/6";
            if (clear)
            {
                haloActive = true; haloCenter = Vector2.zero; haloRadius = 4.6f; haloVisual = 0f;
                haloRing.gameObject.SetActive(true); innerRing.gameObject.SetActive(true); haloPoint.gameObject.SetActive(true);
            }
        }

        bool HitRestart(Vector2 screen)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(restartRt, screen, null);
        }

        void PulseRestart() { MgfFx.Punch(restartRt, .08f, .24f); }

        bool HitSafetyFlag(Vector2 screen)
        {
            Vector3 s = cam.WorldToScreenPoint(flagRoot.position + Vector3.up * .5f);
            return Vector2.Distance(screen, new Vector2(s.x, s.y)) <= Mathf.Max(64f, Screen.width * .14f);
        }

        void GrabFlag() { MgfFx.Punch(flagRoot, .14f, .2f); MgfSfx.Play("tap", .18f); }
        void MoveFlag(Vector2 p) { flagRoot.position = StageWorld(p) + Vector3.up * .45f; }
        void ReleaseFlag() { }
        void ReturnFlag() { flagRoot.localPosition = new Vector3(4.05f, .9f, -3.15f); }

        void GrabCord() { cordPull = .1f; MgfSfx.Play("tap", .16f); }
        void PullCord(float pixels) { cordPull = Mathf.Clamp01(pixels / 170f); cordRoot.localPosition = new Vector3(0, 1.2f - cordPull * 1.0f, -5.8f); }
        void ReleaseCord() { cordRoot.localPosition = new Vector3(0, 1.2f, -5.8f); cordPull = 0; }
        void PulseCord() { MgfFx.Punch(cordRoot, .18f, .3f); }

        Vector2 ScreenToStage(Vector2 screen)
        {
            Ray r = cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, new Vector3(0, SailY, 0));
            if (plane.Raycast(r, out float d))
            {
                Vector3 p = r.GetPoint(d) - stageRoot.position;
                return new Vector2(p.x, p.z);
            }
            return Vector2.zero;
        }

        Vector3 StageWorld(Vector2 p) { return stageRoot.position + new Vector3(p.x, SailY, p.y); }

        string BuildInputTargetString()
        {
            if (!cam) return "";
            if (flow == Flow.Title)
            {
                Vector3 s = cam.WorldToScreenPoint(cordRoot.position + Vector3.down * 1.2f);
                return "cord:" + N(s.x / Screen.width) + "," + N(1f - s.y / Screen.height);
            }
            if (flow == Flow.End) return "restart:0.5,0.72";
            if (task == null) return "";
            // 검증·접근성 좌표는 화면 안쪽의 충분히 긴 선분만 노출한다. 무한 직선 판정은 동일하다.
            Vector2 lineStart, lineEnd;
            if (task.kind == FoldTaskKind.Impossible)
            {
                lineStart = task.a;
                lineEnd = (task.a + task.b + task.c) / 3f;
            }
            else SegmentInsideFrame(task.target, out lineStart, out lineEnd);
            Vector3 a = cam.WorldToScreenPoint(StageWorld(lineStart));
            Vector3 b = cam.WorldToScreenPoint(StageWorld(lineEnd));
            // Keep the exported pointer target identical to HitSafetyFlag's visual center.
            Vector3 f = cam.WorldToScreenPoint(flagRoot.position + Vector3.up * .5f);
            return "line:" + N(a.x / Screen.width) + "," + N(1f - a.y / Screen.height) + ">" + N(b.x / Screen.width) + "," + N(1f - b.y / Screen.height)
                + ";flag:" + N(f.x / Screen.width) + "," + N(1f - f.y / Screen.height);
        }

        string BuildDiagnosticTargetString()
        {
            if (!cam || task == null || task.misconceptionA == null || flow == Flow.Title || flow == Flow.End) return "";
            SegmentInsideFrame(task.misconceptionA, out Vector2 start, out Vector2 end);
            Vector3 a = cam.WorldToScreenPoint(StageWorld(start));
            Vector3 b = cam.WorldToScreenPoint(StageWorld(end));
            return "line:" + N(a.x / Screen.width) + "," + N(1f - a.y / Screen.height) + ">" + N(b.x / Screen.width) + "," + N(1f - b.y / Screen.height);
        }

        static string N(float x) { return Mathf.Clamp01(x).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture); }

        static void SegmentInsideFrame(FoldLine line, out Vector2 start, out Vector2 end)
        {
            float lo = -100f, hi = 100f;
            ClipLineAxis(line.point.x, line.direction.x, -4.65f, 4.65f, ref lo, ref hi);
            ClipLineAxis(line.point.y, line.direction.y, -3.8f, 3.8f, ref lo, ref hi);
            float mid = (lo + hi) * .5f;
            float half = Mathf.Min(1.65f, Mathf.Max(.7f, (hi - lo) * .5f - .12f));
            start = line.point + line.direction * (mid - half);
            end = line.point + line.direction * (mid + half);
        }

        static void ClipLineAxis(float origin, float direction, float min, float max, ref float lo, ref float hi)
        {
            if (Mathf.Abs(direction) < .0001f) return;
            float a = (min - origin) / direction, b = (max - origin) / direction;
            if (a > b) { float t = a; a = b; b = t; }
            lo = Mathf.Max(lo, a); hi = Mathf.Min(hi, b);
        }

        void ApplyLayoutIfNeeded()
        {
            if (lastW == Screen.width && lastH == Screen.height) return;
            lastW = Screen.width; lastH = Screen.height;
            UnityEngine.Canvas.ForceUpdateCanvases();
            float a = Mathf.Max(.35f, cam.aspect);
            float targetSize = Mathf.Max(6.2f, 5.35f / a);
            cam.orthographicSize = flow == Flow.Title ? Mathf.Max(targetSize, 8.2f) : targetSize;
            bool land = a >= 1.25f;
            if (land)
            {
                // 극장형 3존: 좌측 문제·목표, 중앙 조율판, 우측 결과/헤일로.
                // 고정 왼쪽 피벗으로 잡아 초와이드에서도 카드가 화면 밖으로 나가지 않는다.
                Place(progressText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -10), new Vector2(112, 40));
                Place(livesText.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -10), new Vector2(184, 40));
                Place(scoreText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-70, -10), new Vector2(130, 40));
                Place(masteryPlateRt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -48), new Vector2(260, 38));
                Place(masteryText.rectTransform, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(244, 34));
                Place(promptPlateRt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -94), new Vector2(260, 148));
                Place(promptText.rectTransform, new Vector2(.5f, .61f), Vector2.one * .5f, Vector2.zero, new Vector2(232, 98));
                Place(modeText.rectTransform, new Vector2(.5f, .14f), Vector2.one * .5f, Vector2.zero, new Vector2(232, 46));
                Place(scannerText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -250), new Vector2(260, 34));
                Place(tutorialRt, Vector2.zero, Vector2.zero, new Vector2(18, 18), new Vector2(260, 82));
                Place(evidenceRt, Vector2.one, Vector2.one, new Vector2(-24, -82), new Vector2(330, 126));
                Place(toastRt, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 18), new Vector2(350, 56));
                promptText.fontSize = 15; modeText.fontSize = 11; masteryText.fontSize = 12;
                promptText.textWrappingMode = TextWrappingModes.Normal; modeText.textWrappingMode = TextWrappingModes.Normal;
            }
            else
            {
                Place(progressText.rectTransform, new Vector2(.12f, .965f), Vector2.one * .5f, Vector2.zero, new Vector2(100, 42));
                Place(livesText.rectTransform, new Vector2(.49f, .965f), Vector2.one * .5f, Vector2.zero, new Vector2(170, 42));
                Place(scoreText.rectTransform, new Vector2(.79f, .965f), Vector2.one * .5f, Vector2.zero, new Vector2(120, 42));
                Place(masteryPlateRt, new Vector2(.5f, .918f), Vector2.one * .5f, Vector2.zero, new Vector2(356, 38));
                Place(masteryText.rectTransform, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, new Vector2(342, 34));
                Place(promptPlateRt, new Vector2(.5f, .84f), Vector2.one * .5f, Vector2.zero, new Vector2(362, 104));
                Place(promptText.rectTransform, new Vector2(.5f, .59f), Vector2.one * .5f, Vector2.zero, new Vector2(334, 72));
                Place(modeText.rectTransform, new Vector2(.5f, .13f), Vector2.one * .5f, Vector2.zero, new Vector2(330, 40));
                Place(scannerText.rectTransform, new Vector2(.5f, .755f), Vector2.one * .5f, Vector2.zero, new Vector2(240, 34));
                Place(tutorialRt, new Vector2(.5f, .13f), Vector2.one * .5f, Vector2.zero, new Vector2(360, 86));
                Place(evidenceRt, new Vector2(.5f, .25f), Vector2.one * .5f, Vector2.zero, new Vector2(360, 126));
                Place(toastRt, new Vector2(.5f, .08f), Vector2.one * .5f, Vector2.zero, new Vector2(350, 56));
                promptText.fontSize = 17; modeText.fontSize = 12; masteryText.fontSize = 13;
                promptText.textWrappingMode = TextWrappingModes.Normal; modeText.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        void UpdateWorld(float dt)
        {
            worldTime += dt;
            titlePulse = .5f + .5f * Mathf.Sin(worldTime * 1.5f);
            for (int i = 0; i < clouds.Length; i++)
            {
                Vector3 p = cloudHome[i];
                p.x += Mathf.Repeat(worldTime * (.10f + (i % 5) * .018f) + i * 1.7f, 5f) - 2.5f;
                p.y += Mathf.Sin(worldTime * .35f + i) * .08f;
                clouds[i].localPosition = p;
            }
            for (int i = 0; i < 4; i++) if (vanes[i]) vanes[i].localRotation = Quaternion.Euler(0, worldTime * (16f + i * 5f) + i * 43f, 0);
            for (int i = 4; i < vanes.Length; i++) if (vanes[i]) vanes[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(worldTime * .45f + i) * 4f);
            for (int i = 0; i < hangingSails.Length; i++) if (hangingSails[i]) hangingSails[i].localRotation = Quaternion.Euler(0, Mathf.Sin(worldTime * .7f + i) * 8f, Mathf.Sin(worldTime * .9f + i * .6f) * 5f);
            if (titleHalo) titleHalo.localRotation = Quaternion.Euler(0, worldTime * 4f, worldTime * 2f);
            if (flow == Flow.Title)
            {
                cordRoot.localScale = Vector3.one * (1f + .06f * titlePulse);
                if (titleCueKnob)
                {
                    float q = Mathf.Repeat(worldTime * .62f, 1f);
                    titleCueKnob.localPosition = new Vector3(0, Mathf.Lerp(-1.45f, -2.65f, Smooth(q)), -.18f);
                    titleCueKnob.localScale = Vector3.one * (.18f + .08f * Mathf.Sin(q * Mathf.PI));
                }
            }
            else cordRoot.localScale = Vector3.one;

            scanner.localPosition = new Vector3(0, .98f, Mathf.Lerp(4.25f, -4.25f, scannerVisual));
            scanner.localScale = new Vector3(1f + .03f * Mathf.Sin(worldTime * 9f), 1, 1);
            if (pinBounce > 0)
            {
                pinBounce -= dt;
                for (int i = 0; i < pins.Length; i++) pins[i].localScale = Vector3.one * (1f + .22f * Mathf.Abs(Mathf.Sin(pinBounce * 28f)));
            }
            else for (int i = 0; i < pins.Length; i++) pins[i].localScale = Vector3.one;

            UpdateGuide();

            float targetBlend = flow == Flow.Title ? 0f : 1f;
            cameraBlend = Mathf.MoveTowards(cameraBlend, targetBlend, dt * 2.4f);
            Vector3 basePos = Vector3.Lerp(titleCamPos, playCamPos, Smooth(cameraBlend));
            if (wrongShake > 0f)
            {
                wrongShake = Mathf.Max(0f, wrongShake - dt * .72f);
                basePos += new Vector3(Mathf.Sin(worldTime * 91f), 0, Mathf.Cos(worldTime * 77f)) * wrongShake;
            }
            cam.transform.position = basePos;
            cam.transform.LookAt(new Vector3(0, .2f, .2f));
            camBase = cam.transform.position;
        }

        void UpdateGuide()
        {
            bool active = (flow == Flow.Practice || flow == Flow.Drawing) && task != null && task.kind != FoldTaskKind.Impossible && (practice || index == 0 || guideBoost > 0f);
            if (!active)
            {
                guideHand.gameObject.SetActive(false); guideArrow.gameObject.SetActive(false);
                for (int i = 0; i < guideDots.Length; i++) guideDots[i].gameObject.SetActive(false);
                return;
            }
            if (idleClock > 8f) guideBoost = 1.5f;
            float phase = Mathf.Repeat(worldTime / 1.35f, 1f);
            Vector2 start, end;
            if (task.kind == FoldTaskKind.Symmetry) { start = task.a; end = (task.b + task.c) * .5f; }
            else SegmentInsideFrame(task.target, out start, out end);
            Vector2 hand = Vector2.Lerp(start, end, Smooth(phase));
            guideHand.gameObject.SetActive(true); guideArrow.gameObject.SetActive(true);
            guideHand.localPosition = new Vector3(hand.x, 1.18f, hand.y);
            float s = .28f * (1f + .3f * Mathf.Sin(worldTime * 5f)) * (1f + .25f * guideBoost);
            guideHand.localScale = Vector3.one * s;
            guideArrow.localPosition = new Vector3(end.x, 1.12f, end.y);
            float ang = -Mathf.Atan2((end - start).y, (end - start).x) * Mathf.Rad2Deg;
            guideArrow.localRotation = Quaternion.Euler(0, ang, 0);
            for (int i = 0; i < guideDots.Length; i++)
            {
                guideDots[i].gameObject.SetActive(true);
                float u = Mathf.Repeat(i / (float)guideDots.Length + worldTime * .32f, 1f);
                Vector2 p = Vector2.Lerp(start, end, u);
                guideDots[i].localPosition = new Vector3(p.x, 1.08f, p.y);
                guideDots[i].localScale = Vector3.one * (.065f + .045f * Mathf.Sin(u * Mathf.PI));
            }
            guideBoost = Mathf.Max(0f, guideBoost - Time.deltaTime * .45f);
        }

        void UpdateUi(float dt)
        {
            if (titleArtRt && flow == Flow.Title)
            {
                float drift = Mathf.Sin(worldTime * .23f);
                titleArtRt.localScale = Vector3.one * (1.035f + .012f * drift);
                titleArtRt.anchoredPosition = new Vector2(drift * 3f, Mathf.Cos(worldTime * .19f) * 4f);
                titleHint.rectTransform.anchoredPosition = new Vector2(0, Mathf.Sin(worldTime * 3.2f) * 7f);
            }
            displayedScore = Mathf.Lerp(displayedScore, st.score, 1f - Mathf.Exp(-dt * 10f));
            scoreText.text = "점수 " + Mathf.RoundToInt(displayedScore);
            progressText.text = practice ? "연습 돛" : "돛 " + Mathf.Clamp(st.sail, 1, 6) + "/6";
            livesText.text = "오접힘 " + Diamonds(st.lives);
            int firstMisses = Mathf.Max(0, st.firstAttempted - st.firstCorrect);
            masteryText.text = practice ? "연습 보정 ON · 본판 목표 첫 시도 5/6"
                : "첫 시도 " + st.firstCorrect + "/5 · 남은 허용 실수 " + Mathf.Max(0, 1 - firstMisses) + "회";
            scannerText.text = practice ? "첫 접기 동안 열 스캐너 정지" : "열 스캐너  " + Mathf.RoundToInt((1f - scannerVisual) * 100f) + "% 여유";
            tutorialG.alpha = practice && flow != Flow.Resolving ? 1f : 0f;
            if (practice && flow != Flow.Resolving)
            {
                if (drag != DragMode.Line)
                    tutorialText.text = "① 빛나는 시작점 누르기\n② 흰 점선을 따라 끌기  →  ③ 끝점에서 놓기";
                else if (Vector2.Distance(dragStartWorld, dragEndWorld) < 1.15f)
                    tutorialText.text = "① 완료  ·  ② 점선을 따라 길게 끌기\n③ 끝점에서 손 놓기";
                else
                    tutorialText.text = "① 완료  ·  ② 완료\n③ 지금 손을 놓아 접기";
            }
            UpdateVertexLabels();
            if (toastClock > 0f && evidenceClock <= 0f) { toastClock -= dt; toastG.alpha = Mathf.Clamp01(toastClock * 4f); }
            else toastG.alpha = 0;
            if (evidenceClock > 0f) { evidenceClock -= dt; evidenceG.alpha = Mathf.Clamp01(Mathf.Min(1f, evidenceClock * 5f)); }
            for (int i = 0; i < ripples.Length; i++)
            {
                rippleLife[i] += dt;
                float k = rippleLife[i] / .48f;
                ripples[i].color = new Color(Cyan.r, Cyan.g, Cyan.b, Mathf.Clamp01(1f - k) * .55f);
                ripples[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(.25f, 1.5f, Smooth(Mathf.Clamp01(k)));
            }
            if (flow == Flow.End)
            {
                endCount = Mathf.MoveTowards(endCount, 1f, dt * 1.15f);
                int score = Mathf.RoundToInt(st.score * EaseOut(endCount));
                int first = Mathf.RoundToInt(st.firstCorrect * EaseOut(endCount));
                endStats.text = score + "점 · 첫 시도 " + first + "/6";
                if (st.phase == "clear")
                {
                    haloVisual = EaseOutBack(Mathf.Clamp01(endCount));
                    SetCircle(haloRing, Vector2.zero, 4.6f * haloVisual, 1.08f);
                    SetCircle(innerRing, Vector2.zero, 4.1f * haloVisual, 1.09f);
                    haloPoint.localPosition = new Vector3(0, 1.25f, 0); haloPoint.localScale = Vector3.one * (.2f + haloVisual * .8f);
                }
            }
        }

        void UpdateVertexLabels()
        {
            if (task == null || flow == Flow.Title || flow == Flow.End) return;
            Vector2[] pts = { task.a, task.b, task.c };
            Vector2 center = (task.a + task.b + task.c) / 3f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 outward = (pts[i] - center).normalized * .38f;
                Vector3 s = cam.WorldToScreenPoint(StageWorld(pts[i] + outward) + Vector3.up * .15f);
                vertexLabels[i].rectTransform.position = new Vector3(s.x, s.y, 0);
            }
        }

        void SpawnRipple(Vector2 screen)
        {
            int i = rippleCursor++ % ripples.Length;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rippleRoot, screen, null, out Vector2 local);
            ripples[i].rectTransform.anchoredPosition = local; rippleLife[i] = 0f; ripples[i].rectTransform.localScale = Vector3.one * .25f;
        }

        static string Diamonds(int n)
        {
            n = Mathf.Clamp(n, 0, 3); return new string('◆', n) + new string('◇', 3 - n);
        }

        static float DistancePointLine(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 d = (b - a).normalized; return Mathf.Abs(FoldlineRules.Cross(d, p - a));
        }
        static Vector2 FootOnLine(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 d = (b - a).normalized;
            return a + d * Vector2.Dot(p - a, d);
        }
        static float Smooth(float t) { return t * t * (3f - 2f * t); }
        static float EaseOut(float t) { return 1f - (1f - t) * (1f - t) * (1f - t); }
        static float EaseBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f; float q = t - 1f; return 1f + c3 * q * q * q + c1 * q * q;
        }
        static float EaseOutBack(float t) { return Mathf.Max(0f, EaseBack(t)); }

        static Sprite MakeSprite(int size, float radius, int kind)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "FoldlineUI", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = (x + .5f) / size, v = (y + .5f) / size, a;
                if (kind == 1)
                {
                    float d = Vector2.Distance(new Vector2(u, v), Vector2.one * .5f);
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - .34f) * size * .42f);
                }
                else if (kind == 2)
                {
                    a = (u < .22f || (v > .2f && v < .8f && u < .9f)) ? 1f : 0f;
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
            float border = kind == 0 ? radius + 2 : 0;
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
    }
}
