// 캡슐 찰칵 — 코드 프리미티브 심해 관측정, 반응형 표본공간 UI, 광섬유·캡슐 연출.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.CapsuleChalkak
{
    public partial class CapsuleChalkakGame
    {
        static readonly Color Navy = MgfLook.Hex("071A2B");
        static readonly Color Cyan = MgfLook.Hex("0ED7C7");
        static readonly Color Milk = MgfLook.Hex("B9F4F0");
        static readonly Color Coral = MgfLook.Hex("FF6B62");
        static readonly Color Slate = MgfLook.Hex("5B6C86");
        static readonly Color Ink = MgfLook.Hex("DDFCF9");

        Camera cam;
        Transform stationRoot, mantaRoot;
        Transform[] kelp = new Transform[12];
        Transform[] plankton = new Transform[30];
        Vector3[] planktonHome = new Vector3[30];
        GameObject[] pressureLamps = new GameObject[6];
        float worldClock, mantaKick;

        GameObject titleUiRoot, playUiRoot, endUiRoot;
        GameObject promptPanel, toastPanel, capsuleFxRoot, waterColumnGo, vortexGo;
        GameObject titleHandleGo, leverGo, complementGo, endPanel, endHandleGo;
        RectTransform promptRt, boardRt, leverRt, complementRt, titleHandleRt, endHandleRt, capsuleRt, waterColumnRt, vortexRt;
        TextMeshProUGUI promptUi, goalUi, hudUi, rawFractionUi, toastUi, leverUi, complementUi;
        TextMeshProUGUI titleLogoBackUi, titleLogoUi, titleTagUi, titleMetaUi, titleCtaUi;
        TextMeshProUGUI capsuleUi, endTitleUi, endStatsUi, endCtaUi;
        Image leverImage, complementImage, titleHandleImage, endHandleImage, capsuleImage, waterColumnImage, vortexImage;

        readonly GameObject[] tileGo = new GameObject[36];
        readonly RectTransform[] tileRt = new RectTransform[36];
        readonly Image[] tileBg = new Image[36];
        readonly TextMeshProUGUI[] tileText = new TextMeshProUGUI[36];
        readonly float[] tilePulse = new float[36];
        readonly Image[] chainSeg = new Image[36];
        Image chainTail;
        readonly Image[] ripples = new Image[8];
        readonly float[] rippleLife = new float[8];
        int rippleCursor;
        Image guideDot, guideLine, guideArrow;
        float guideLeft;
        bool guideLarge;
        float toastLeft, leverRecoil, correctFractionHeight;
        string wrongMessage = "";
        int lastScreenW = -1, lastScreenH = -1;
        bool currentLand;
        float stageLogicalWidth, stageLogicalHeight;
        int activeCols, activeRows;
        float displayScore;
        int lastScoreTarget = -1;

        void BuildWorld()
        {
            // 빈 어두운 기계실 대신 산호 협곡을 세로로 자른 관측 갱도다. 청록 단색 금속이
            // 아니라 남보라 수층, 유백 아크릴, 산호 선반, 직조 광섬유가 층을 만든다.
            MgfLook.Sky(MgfLook.Hex("19295B"), MgfLook.Hex("147E91"), MgfLook.Hex("06152E"), 1.05f);
            MgfLook.Sun(new Vector3(38f, -32f, -16f), MgfLook.Hex("E8FFF6"), 1.02f);
            cam = MgfLook.Camera(new Vector3(0f, 8.8f, -14.6f), new Vector3(0f, .2f, 0f), 34f);
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;

            stationRoot = new GameObject("CoralObservationShaft").transform;
            var cliff = MgfLook.Lit(MgfLook.Hex("24305B"), .12f, .02f);
            var acrylic = MgfLook.Lit(MgfLook.Hex("3E8792"), .72f, .03f, MgfLook.Hex("062736"));
            var rim = MgfLook.Lit(MgfLook.Hex("79B9B5"), .80f, .04f, MgfLook.Hex("0D3448"));
            var coral = MgfLook.Lit(MgfLook.Hex("FF776E"), .34f, .02f, MgfLook.Hex("5A171F"));
            var violet = MgfLook.Lit(MgfLook.Hex("9D8BEF"), .4f, .02f, MgfLook.Hex("241C5D"));
            var cyan = MgfLook.Lit(MgfLook.Hex("15CFC3"), .58f, .02f, Cyan);

            // 양쪽 산호 절벽과 층층이 떠 있는 수조 선반. 중앙은 위로 열린 수주 통로라
            // 쿼터뷰에서도 '수직 갱도' 공간 구조가 한눈에 읽힌다.
            MgfLook.Block("LeftReefWall", new Vector3(-7.25f, .7f, 1.3f), new Vector3(2.4f, 9.4f, 6.8f), .10f, cliff).transform.SetParent(stationRoot, true);
            MgfLook.Block("RightReefWall", new Vector3(7.25f, .7f, 1.3f), new Vector3(2.4f, 9.4f, 6.8f), .10f, cliff).transform.SetParent(stationRoot, true);
            MgfLook.Block("LowerPool", new Vector3(0f, -1.55f, .8f), new Vector3(12.7f, .40f, 7.8f), .06f, acrylic).transform.SetParent(stationRoot, true);
            for (int level = 0; level < 4; level++)
            {
                float y = -.55f + level * 1.55f;
                float inset = level % 2 == 0 ? .2f : .75f;
                var shelf = MgfLook.Block("AquariumShelf" + level, new Vector3(0f, y, 3.28f), new Vector3(11.6f - inset, .12f, 1.10f), .03f, rim);
                shelf.transform.SetParent(stationRoot, true);
                for (int side = -1; side <= 1; side += 2)
                {
                    var branch = MgfLook.Block("CoralBranch" + level + "_" + side, new Vector3(side * (5.5f - inset), y + .62f, 3.08f), new Vector3(.22f, 1.35f, .24f), .04f, level % 2 == 0 ? coral : violet);
                    branch.transform.rotation = Quaternion.Euler(0f, 0f, side * (18f + level * 4f));
                    branch.transform.SetParent(stationRoot, true);
                }
            }

            // 각진 에칭 아크릴 프레임과 세로 수주. 둥근 무광 금속 설비 인상을 버린다.
            for (int side = -1; side <= 1; side += 2)
            {
                MgfLook.Block("EtchedRail" + side, new Vector3(side * 5.65f, 2.0f, 2.75f), new Vector3(.16f, 7.6f, .22f), .02f, rim).transform.SetParent(stationRoot, true);
                for (int i = 0; i < 4; i++)
                {
                    var brace = MgfLook.Block("AcrylicBrace" + side + "_" + i, new Vector3(side * (4.35f + i * .18f), -.15f + i * 1.55f, 2.85f), new Vector3(2.25f, .10f, .18f), .02f, acrylic);
                    brace.transform.rotation = Quaternion.Euler(0f, 0f, side * (12f + i * 2f));
                    brace.transform.SetParent(stationRoot, true);
                }
            }

            // 여섯 분절 유리관이 중앙 수주로 모인다. 정답 때 램프를 차례로 켜는 대신
            // 선택 경우가 물살로 합쳐지고 캡슐이 위로 떠오르는 사건의 배경 장치다.
            for (int i = 0; i < pressureLamps.Length; i++)
            {
                float x = -4.4f + i * 1.76f;
                MgfLook.Block("GlassBranch" + i, new Vector3(x, -.72f, 2.92f), new Vector3(1.28f, .12f, .16f), .02f, rim).transform.SetParent(stationRoot, true);
                pressureLamps[i] = MgfLook.Block("CurrentBead" + i, new Vector3(x, -.58f, 2.70f), new Vector3(.34f, .16f, .24f), .03f, cyan);
                pressureLamps[i].transform.SetParent(stationRoot, true);
            }
            MgfLook.Block("CentralWaterColumn", new Vector3(0f, 2.0f, 3.25f), new Vector3(1.15f, 7.1f, .55f), .08f, acrylic).transform.SetParent(stationRoot, true);

            // 서로 교차하는 얇은 광섬유 다발이 정적인 배관보다 '직조'된 인상을 만든다.
            for (int i = 0; i < 8; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                var fiber = MgfLook.Block("WovenFiber" + i, new Vector3(side * (2.2f + (i % 4) * .7f), .25f + i * .62f, 3.48f), new Vector3(2.3f, .055f, .07f), .015f, i % 3 == 0 ? violet : cyan);
                fiber.transform.rotation = Quaternion.Euler(0f, 0f, side * (24f + (i % 3) * 8f));
                fiber.transform.SetParent(stationRoot, true);
            }

            // 산호 가지와 미립자는 고정 풀: 매 프레임 Instantiate/Destroy 없음.
            var kelpMatA = MgfLook.Lit(MgfLook.Hex("E85F72"), .18f, 0f, MgfLook.Hex("3A1029"));
            var kelpMatB = MgfLook.Lit(MgfLook.Hex("7C6ED8"), .22f, 0f, MgfLook.Hex("221848"));
            for (int i = 0; i < kelp.Length; i++)
            {
                float x = -7.4f + i * 1.35f;
                var k = MgfLook.Block("FanCoral" + i, new Vector3(x, .05f + (i % 3) * .20f, 4.4f), new Vector3(.16f, 1.9f + (i % 4) * .38f, .18f), .035f, i % 2 == 0 ? kelpMatA : kelpMatB);
                k.transform.SetParent(stationRoot, true);
                k.transform.rotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? -1f : 1f) * (11f + i % 3 * 5f));
                kelp[i] = k.transform;
            }
            var dotMat = MgfLook.Unlit(MgfLook.Hex("6FD9D1"));
            for (int i = 0; i < plankton.Length; i++)
            {
                float x = -7.5f + ((i * 47) % 101) / 100f * 15f;
                float y = .2f + ((i * 71) % 97) / 96f * 6.2f;
                float z = 2.4f + (i % 5) * .25f;
                planktonHome[i] = new Vector3(x, y, z);
                // WebGL 킷은 SphereCollider를 스트립하므로 프리미티브 Sphere를 만들지 않는다.
                float size = .025f + (i % 4) * .012f;
                var d = MgfLook.Block("Plankton" + i, planktonHome[i], Vector3.one * size, size * .48f, dotMat);
                d.transform.SetParent(stationRoot, true); plankton[i] = d.transform;
            }
            BuildManta();
        }

        void BuildManta()
        {
            mantaRoot = new GameObject("RibbonMantaCourier").transform;
            var meshGo = new GameObject("DiamondBody", typeof(MeshFilter), typeof(MeshRenderer));
            meshGo.transform.SetParent(mantaRoot, false);
            var mesh = new Mesh { name = "RibbonMantaMesh" };
            mesh.vertices = new[] {
                new Vector3(0f,.08f,1.0f), new Vector3(-1.9f,0f,0f), new Vector3(0f,.24f,-.9f), new Vector3(1.9f,0f,0f),
                new Vector3(0f,-.16f,.05f)
            };
            mesh.triangles = new[] { 0,1,4, 1,2,4, 2,3,4, 3,0,4, 0,3,2, 0,2,1 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            meshGo.GetComponent<MeshFilter>().sharedMesh = mesh;
            meshGo.GetComponent<MeshRenderer>().sharedMaterial = MgfLook.Lit(MgfLook.Hex("B7A9F2"), .82f, .08f, MgfLook.Hex("241C5D"));
            var tailMat = MgfLook.Lit(MgfLook.Hex("0ED7C7"), .34f, .08f, Cyan);
            for (int i = 0; i < 2; i++)
            {
                var tail = MgfLook.Block("FiberTail" + i, new Vector3((i == 0 ? -.32f : .32f), 0f, -1.7f), new Vector3(.12f, .12f, 2.0f), .08f, tailMat);
                tail.transform.SetParent(mantaRoot, false);
                tail.transform.rotation = Quaternion.Euler(0f, i == 0 ? -8f : 8f, 0f);
            }
            var socketMat = MgfLook.Lit(Milk, .72f, .18f, MgfLook.Hex("073D43"));
            for (int i = 0; i < 3; i++)
            {
                var socket = MgfLook.Block("CapsuleSocket" + i, Vector3.zero, new Vector3(.34f, .20f, .30f), .12f, socketMat);
                socket.transform.SetParent(mantaRoot, false);
                socket.transform.localPosition = new Vector3(-.55f + i * .55f, .18f, -.15f);
            }
            mantaRoot.position = new Vector3(3.4f, 3.25f, 2.7f);
            mantaRoot.localScale = Vector3.one * .78f;
        }

        void BuildUi()
        {
            Canvas canvas = MgfText.Canvas;
            titleUiRoot = Root("TitleUI", canvas.transform);
            playUiRoot = Root("PlayUI", canvas.transform);
            endUiRoot = Root("EndUI", canvas.transform);

            BuildTitleUi();
            BuildPlayUi();
            BuildEndUi();
        }

        GameObject Root(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); Stretch((RectTransform)go.transform); return go;
        }

        void BuildTitleUi()
        {
            titleLogoBackUi = MakeText("캡슐 찰칵", titleUiRoot.transform, 50f, Navy, TextAlignmentOptions.Center);
            titleLogoBackUi.outlineWidth = .30f; titleLogoBackUi.outlineColor = new Color32(210, 245, 234, 235);
            titleLogoUi = MakeText("캡슐 찰칵", titleUiRoot.transform, 50f, Milk, TextAlignmentOptions.Center);
            titleLogoUi.outlineWidth = .14f; titleLogoUi.outlineColor = new Color32(25, 41, 91, 255);
            titleTagUi = MakeText("물살로 경우를 엮어 확률을 띄워라", titleUiRoot.transform, 20f, Ink, TextAlignmentOptions.Center);
            titleTagUi.outlineWidth = .18f; titleTagUi.outlineColor = new Color32(7, 26, 43, 230);
            titleMetaUi = MakeText("심해 확률 관측 07  ·  중학교 2학년", titleUiRoot.transform, 14f, Ink, TextAlignmentOptions.Center);
            titleMetaUi.outlineWidth = .15f; titleMetaUi.outlineColor = new Color32(7, 26, 43, 220);

            // 별도 CTA 상자를 없애고 화면 전체 관측창을 시작 표면으로 쓴다. 안내는
            // 물속 표지처럼 떠 있으며 어떤 첫 탭도 실제 연습으로 이어진다.
            titleHandleGo = Panel("TitleDiveSurface", titleUiRoot.transform, new Color32(255, 255, 255, 0), out titleHandleImage);
            titleHandleRt = (RectTransform)titleHandleGo.transform;
            Stretch(titleHandleRt);
            titleCtaUi = MakeText("관측창 아무 곳이나 눌러 잠수", titleHandleGo.transform, 18f, Color.white, TextAlignmentOptions.Center);
            titleCtaUi.outlineWidth = .16f; titleCtaUi.outlineColor = new Color32(25, 41, 91, 225);
            Rect((RectTransform)titleLogoBackUi.transform, new Vector2(.5f, .76f), new Vector2(3f, -4f), new Vector2(370f, 76f));
            Rect((RectTransform)titleLogoUi.transform, new Vector2(.5f, .76f), Vector2.zero, new Vector2(370f, 76f));
            Rect((RectTransform)titleTagUi.transform, new Vector2(.5f, .66f), Vector2.zero, new Vector2(372f, 42f));
            Rect((RectTransform)titleMetaUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -26f), new Vector2(330f, 28f));
            Rect((RectTransform)titleCtaUi.transform, new Vector2(.5f, .10f), Vector2.zero, new Vector2(330f, 52f));
        }

        void BuildPlayUi()
        {
            promptPanel = Panel("OrderCard", playUiRoot.transform, new Color32(7, 26, 43, 232), out _);
            promptRt = (RectTransform)promptPanel.transform;
            var promptEtch = promptPanel.AddComponent<Outline>();
            promptEtch.effectColor = new Color32(185, 244, 240, 135); promptEtch.effectDistance = new Vector2(2f, -2f);
            promptUi = MakeText("", promptPanel.transform, 16f, Ink, TextAlignmentOptions.Center);
            promptUi.textWrappingMode = TextWrappingModes.Normal;
            goalUi = MakeText("조건에 맞는 경우를 엮어 레버를 당겨라", playUiRoot.transform, 16f, Ink, TextAlignmentOptions.Center);
            goalUi.textWrappingMode = TextWrappingModes.Normal;
            goalUi.outlineWidth = .16f; goalUi.outlineColor = new Color32(7, 26, 43, 235);
            hudUi = MakeText("", playUiRoot.transform, 15f, Ink, TextAlignmentOptions.Center);
            hudUi.outlineWidth = .14f; hudUi.outlineColor = new Color32(7, 26, 43, 235);
            rawFractionUi = MakeText("선택 0 / 전체 0", playUiRoot.transform, 18f, Milk, TextAlignmentOptions.Center);
            rawFractionUi.outlineWidth = .14f; rawFractionUi.outlineColor = new Color32(7, 26, 43, 235);

            boardRt = (RectTransform)Root("OutcomeBoard", playUiRoot.transform).transform;
            var boardBg = boardRt.gameObject.AddComponent<Image>(); boardBg.color = new Color32(22, 70, 91, 205); boardBg.raycastTarget = false;
            var boardEtch = boardRt.gameObject.AddComponent<Outline>();
            boardEtch.effectColor = new Color32(210, 245, 234, 185); boardEtch.effectDistance = new Vector2(3f, -3f);
            for (int i = 0; i < tileGo.Length; i++)
            {
                tileGo[i] = Panel("Outcome" + i, boardRt, new Color32(32, 78, 104, 242), out tileBg[i]);
                tileRt[i] = (RectTransform)tileGo[i].transform;
                var etch = tileGo[i].AddComponent<Outline>();
                etch.effectColor = i % 2 == 0 ? new Color32(185, 244, 240, 130) : new Color32(157, 139, 239, 125);
                etch.effectDistance = new Vector2(1.5f, -1.5f);
                tileText[i] = MakeText("", tileGo[i].transform, 15f, Ink, TextAlignmentOptions.Center);
                tileText[i].textWrappingMode = TextWrappingModes.NoWrap;
                StretchInset((RectTransform)tileText[i].transform, 2f);
            }

            // 광섬유는 고정 36개 선분을 재사용한다.
            for (int i = 0; i < chainSeg.Length; i++)
            {
                var go = Panel("Fiber" + i, boardRt, new Color32(14, 215, 199, 220), out chainSeg[i]);
                go.transform.SetAsLastSibling(); go.SetActive(false); chainSeg[i].raycastTarget = false;
            }
            var tail = Panel("FiberTail", boardRt, new Color32(185, 244, 240, 150), out chainTail);
            tail.transform.SetAsLastSibling(); tail.SetActive(false); chainTail.raycastTarget = false;

            leverGo = Panel("PressureLever", playUiRoot.transform, new Color32(255, 119, 110, 242), out leverImage);
            leverRt = (RectTransform)leverGo.transform;
            var leverEtch = leverGo.AddComponent<Outline>();
            leverEtch.effectColor = new Color32(255, 222, 204, 210); leverEtch.effectDistance = new Vector2(3f, -3f);
            leverUi = MakeText("수주\n레버\n▼", leverGo.transform, 15f, Color.white, TextAlignmentOptions.Center);
            StretchInset((RectTransform)leverUi.transform, 4f);
            complementGo = Panel("ComplementRing", playUiRoot.transform, new Color32(16, 66, 82, 238), out complementImage);
            complementRt = (RectTransform)complementGo.transform;
            complementUi = MakeText("여사건\n반전 2", complementGo.transform, 13f, Milk, TextAlignmentOptions.Center);
            StretchInset((RectTransform)complementUi.transform, 3f);

            toastPanel = Panel("Toast", playUiRoot.transform, new Color32(7, 26, 43, 244), out _);
            toastUi = MakeText("", toastPanel.transform, 14f, Color.white, TextAlignmentOptions.Center);
            toastUi.textWrappingMode = TextWrappingModes.Normal;
            StretchInset((RectTransform)toastUi.transform, 8f); toastPanel.SetActive(false);

            // 정답 사건의 주인공: 선택 경우가 회전하며 합쳐지는 물살 마름모와,
            // 분수 높이만큼 차오른 뒤 캡슐을 위로 띄우는 세로 수주.
            waterColumnGo = Panel("FractionWaterColumn", playUiRoot.transform, new Color32(21, 207, 195, 128), out waterColumnImage);
            waterColumnRt = (RectTransform)waterColumnGo.transform;
            var columnEtch = waterColumnGo.AddComponent<Outline>();
            columnEtch.effectColor = new Color32(210, 245, 234, 210); columnEtch.effectDistance = new Vector2(3f, -3f);
            waterColumnGo.SetActive(false);
            vortexGo = Panel("OutcomeCurrentVortex", playUiRoot.transform, new Color32(157, 139, 239, 145), out vortexImage);
            vortexRt = (RectTransform)vortexGo.transform;
            var vortexEtch = vortexGo.AddComponent<Outline>();
            vortexEtch.effectColor = new Color32(185, 244, 240, 180); vortexEtch.effectDistance = new Vector2(3f, -3f);
            vortexGo.SetActive(false);

            capsuleFxRoot = Panel("CapsuleFx", playUiRoot.transform, new Color32(185, 244, 240, 242), out capsuleImage);
            capsuleRt = (RectTransform)capsuleFxRoot.transform;
            var capsuleEtch = capsuleFxRoot.AddComponent<Outline>();
            capsuleEtch.effectColor = new Color32(255, 119, 110, 220); capsuleEtch.effectDistance = new Vector2(3f, -3f);
            capsuleUi = MakeText("", capsuleFxRoot.transform, 20f, Navy, TextAlignmentOptions.Center);
            StretchInset((RectTransform)capsuleUi.transform, 5f); capsuleFxRoot.SetActive(false);

            for (int i = 0; i < ripples.Length; i++)
            {
                var go = Panel("Ripple" + i, playUiRoot.transform, new Color32(185, 244, 240, 0), out ripples[i]);
                Rect((RectTransform)go.transform, Vector2.zero, Vector2.zero, new Vector2(24f, 24f)); go.SetActive(false);
            }
            var gd = Panel("GuideDot", playUiRoot.transform, new Color32(185, 244, 240, 235), out guideDot);
            Rect((RectTransform)gd.transform, Vector2.zero, Vector2.zero, new Vector2(30f, 30f)); gd.SetActive(false);
            var gl = Panel("GuidePath", playUiRoot.transform, new Color32(14, 215, 199, 150), out guideLine);
            Rect((RectTransform)gl.transform, Vector2.zero, Vector2.zero, new Vector2(5f, 80f)); gl.SetActive(false);
            var ga = Panel("GuideArrow", playUiRoot.transform, new Color32(255, 107, 98, 235), out guideArrow);
            Rect((RectTransform)ga.transform, Vector2.zero, Vector2.zero, new Vector2(18f, 28f)); ga.SetActive(false);
        }

        void BuildEndUi()
        {
            endPanel = Panel("ResultCapsuleBay", endUiRoot.transform, new Color32(7, 26, 43, 244), out _);
            endTitleUi = MakeText("", endPanel.transform, 38f, Milk, TextAlignmentOptions.Center);
            endTitleUi.outlineWidth = .14f; endTitleUi.outlineColor = new Color32(14, 215, 199, 220);
            endStatsUi = MakeText("", endPanel.transform, 20f, Ink, TextAlignmentOptions.Center);
            endStatsUi.textWrappingMode = TextWrappingModes.Normal;
            endHandleGo = Panel("RestartHandle", endPanel.transform, new Color32(255, 107, 98, 245), out endHandleImage);
            endHandleRt = (RectTransform)endHandleGo.transform;
            endCtaUi = MakeText("관측정 다시 가동", endHandleGo.transform, 18f, Color.white, TextAlignmentOptions.Center);
            StretchInset((RectTransform)endCtaUi.transform, 4f);
            Rect((RectTransform)endPanel.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(352f, 410f));
            Rect((RectTransform)endTitleUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -70f), new Vector2(320f, 76f));
            Rect((RectTransform)endStatsUi.transform, new Vector2(.5f, .5f), new Vector2(0f, 15f), new Vector2(314f, 165f));
            Rect(endHandleRt, new Vector2(.5f, 0f), new Vector2(0f, 55f), new Vector2(270f, 68f));
        }

        static GameObject Panel(string name, Transform parent, Color color, out Image image)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            image = go.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return go;
        }

        static TextMeshProUGUI MakeText(string text, Transform parent, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.fontStyle = FontStyles.Bold;
            t.color = color; t.alignment = align; t.text = text; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap; return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void StretchInset(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        static void Rect(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = offset; rt.sizeDelta = size;
        }

        void SetScreen()
        {
            bool title = gamePhase == GamePhase.Title;
            bool end = gamePhase == GamePhase.End;
            titleUiRoot.SetActive(title); playUiRoot.SetActive(!title && !end); endUiRoot.SetActive(end);
            capsuleFxRoot.SetActive(false); waterColumnGo.SetActive(false); vortexGo.SetActive(false); toastPanel.SetActive(false);
            guideDot.gameObject.SetActive(false); guideLine.gameObject.SetActive(false); guideArrow.gameObject.SetActive(false);
            if (!title && !end && current != null)
            {
                BindTiles(); LayoutTiles(); RefreshProblemUi(); RefreshChainVisuals();
            }
            if (title) mantaKick = 0f;
            lastScreenW = -1;
            ApplyResponsiveLayout();
        }

        void BindTiles()
        {
            int count = current == null ? 0 : current.labels.Length;
            for (int i = 0; i < tileGo.Length; i++)
            {
                bool on = i < count; tileGo[i].SetActive(on);
                if (!on) continue;
                tileText[i].text = current.labels[i];
                tileBg[i].color = i % 2 == 0 ? new Color32(32, 78, 104, 242) : new Color32(39, 72, 111, 242);
                tileRt[i].localScale = Vector3.one;
                tileRt[i].localRotation = Quaternion.identity;
            }
        }

        void LayoutTiles()
        {
            if (current == null) return;
            int count = current.labels.Length;
            activeCols = count <= 4 ? 2 : count <= 10 ? (currentLand ? 5 : 4) : count <= 12 ? (currentLand ? 6 : 4) : 6;
            activeRows = Mathf.CeilToInt(count / (float)activeCols);
            float gap = count == 36 ? 4f : 7f;
            float pad = count == 36 ? 8f : 12f;
            float cellW = (boardRt.rect.width - pad * 2f - gap * (activeCols - 1)) / activeCols;
            float cellH = (boardRt.rect.height - pad * 2f - gap * (activeRows - 1)) / activeRows;
            float cell = Mathf.Min(cellW, cellH);
            float usedW = activeCols * cell + (activeCols - 1) * gap;
            float usedH = activeRows * cell + (activeRows - 1) * gap;
            float x0 = -usedW * .5f + cell * .5f;
            float y0 = usedH * .5f - cell * .5f;
            for (int i = 0; i < count; i++)
            {
                int c = i % activeCols, r = i / activeCols;
                Rect(tileRt[i], new Vector2(.5f, .5f), new Vector2(x0 + c * (cell + gap), y0 - r * (cell + gap)), new Vector2(cell, cell));
                tileText[i].fontSize = count == 36 ? Mathf.Clamp(cell * .22f, 12f, 17f) : Mathf.Clamp(cell * .22f, 14f, 20f);
            }
            RefreshChainVisuals();
            UpdateControlCoordinates();
        }

        void ApplyResponsiveLayout()
        {
            float aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 1f;
            bool land = aspect >= 1.05f;
            var rootRt = (RectTransform)playUiRoot.transform;
            float nextStageW = Mathf.Max(360f, Mathf.Min(rootRt.rect.width - 18f, 760f));
            float nextStageH = Mathf.Max(400f, rootRt.rect.height);
            if (Screen.width == lastScreenW && Screen.height == lastScreenH && land == currentLand
                && Mathf.Abs(nextStageW - stageLogicalWidth) < .5f && Mathf.Abs(nextStageH - stageLogicalHeight) < .5f) return;
            lastScreenW = Screen.width; lastScreenH = Screen.height; currentLand = land;
            stageLogicalWidth = nextStageW;
            stageLogicalHeight = nextStageH;
            cam.orthographicSize = land ? 5.55f : 7.65f;
            cam.transform.position = land ? new Vector3(0f, 8.1f, -14.8f) : new Vector3(0f, 9.2f, -15.8f);
            cam.transform.LookAt(new Vector3(0f, .4f, .5f));

            if (gamePhase == GamePhase.Title)
            {
                Stretch(titleHandleRt);
                Rect((RectTransform)titleLogoBackUi.transform, new Vector2(.5f, land ? .70f : .76f), new Vector2(3f, -4f), new Vector2(land ? 520f : 370f, 86f));
                Rect((RectTransform)titleLogoUi.transform, new Vector2(.5f, land ? .70f : .76f), Vector2.zero, new Vector2(land ? 520f : 370f, 86f));
                Rect((RectTransform)titleTagUi.transform, new Vector2(.5f, land ? .59f : .66f), Vector2.zero, new Vector2(land ? 560f : 372f, 42f));
                Rect((RectTransform)titleCtaUi.transform, new Vector2(.5f, land ? .12f : .10f), Vector2.zero, new Vector2(land ? 430f : 330f, 52f));
                titleLogoUi.fontSize = land ? 58f : 50f; titleLogoBackUi.fontSize = titleLogoUi.fontSize;
                UpdateControlCoordinates();
                return;
            }
            if (gamePhase == GamePhase.End)
            {
                Rect((RectTransform)endPanel.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(land ? 470f : 352f, land ? 380f : 410f));
                UpdateControlCoordinates(); return;
            }

            int count = current == null ? 0 : current.labels.Length;
            if (land)
            {
                // CanvasScaler의 logical width를 한 좌표계로 사용한다. 1280/1440/2000에서
                // anchor 백분율이 벌어져 좌측 카드가 잘리던 기존 배치를 제거한다.
                float gap = 13f;
                float promptW = Mathf.Clamp(stageLogicalWidth * .285f, 196f, 222f);
                float leverW = Mathf.Clamp(stageLogicalWidth * .09f, 64f, 72f);
                float boardMax = count == 36 ? 354f : 336f;
                float boardW = Mathf.Min(boardMax, stageLogicalWidth - promptW - leverW - gap * 4f);
                float boardH = count == 36 ? Mathf.Min(boardW, stageLogicalHeight - 82f)
                    : Mathf.Min(count <= 10 ? 232f : 256f, stageLogicalHeight - 116f);
                float left = -stageLogicalWidth * .5f + 10f;
                float promptX = left + promptW * .5f;
                float boardX = left + promptW + gap + boardW * .5f;
                float leverX = stageLogicalWidth * .5f - 10f - leverW * .5f;
                float contentY = -3f;
                float promptH = Mathf.Min(204f, stageLogicalHeight - 128f);

                Rect((RectTransform)hudUi.transform, new Vector2(.5f, .5f), new Vector2(0f, stageLogicalHeight * .5f - 20f), new Vector2(Mathf.Min(640f, stageLogicalWidth - 40f), 34f));
                Rect(promptRt, new Vector2(.5f, .5f), new Vector2(promptX, contentY + 18f), new Vector2(promptW, promptH));
                Rect((RectTransform)promptUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(promptW - 22f, promptH - 18f));
                Rect((RectTransform)goalUi.transform, new Vector2(.5f, .5f), new Vector2(promptX, -stageLogicalHeight * .5f + 34f), new Vector2(promptW, 50f));
                Rect(boardRt, new Vector2(.5f, .5f), new Vector2(boardX, contentY), new Vector2(boardW, boardH));
                Rect(leverRt, new Vector2(.5f, .5f), new Vector2(leverX, contentY - 8f), new Vector2(leverW, Mathf.Min(224f, stageLogicalHeight * .54f)));
                Rect(complementRt, new Vector2(.5f, .5f), new Vector2(leverX, stageLogicalHeight * .5f - 91f), new Vector2(leverW + 12f, 72f));
                Rect((RectTransform)rawFractionUi.transform, new Vector2(.5f, .5f), new Vector2(boardX, -stageLogicalHeight * .5f + 31f), new Vector2(boardW, 42f));
                Rect((RectTransform)toastPanel.transform, new Vector2(.5f, .5f), new Vector2(0f, stageLogicalHeight * .5f - 64f), new Vector2(Mathf.Clamp(stageLogicalWidth - 280f, 180f, 350f), 44f));
                promptUi.fontSize = current != null && current.prompt.Length > 100 ? 11.5f : current != null && current.prompt.Length > 78 ? 12.5f : 14f;
                goalUi.fontSize = 11.5f;
            }
            else
            {
                Rect((RectTransform)hudUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -19f), new Vector2(370f, 32f));
                Rect(promptRt, new Vector2(.5f, 1f), new Vector2(0f, -102f), new Vector2(370f, 126f));
                Rect((RectTransform)promptUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(348f, 110f));
                Rect((RectTransform)goalUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -191f), new Vector2(370f, 40f));
                Rect(boardRt, new Vector2(.5f, .5f), new Vector2(count == 36 ? -12f : -20f, count == 36 ? -2f : 2f), new Vector2(count == 36 ? 330f : 300f, count == 36 ? 330f : 276f));
                // 순진 firstplay의 캔버스 드래그 범위(가로 20~80%, 세로 20~80%)와
                // 겹치도록 레버를 하단 안쪽에 둔다. 보드와는 세로 존이 분리된다.
                Rect(leverRt, new Vector2(.5f, .5f), new Vector2(112f, count == 36 ? -277f : -252f), new Vector2(106f, count == 36 ? 152f : 142f));
                Rect(complementRt, new Vector2(.5f, .5f), new Vector2(-140f, -270f), new Vector2(82f, 72f));
                Rect((RectTransform)rawFractionUi.transform, new Vector2(.5f, .5f), new Vector2(-5f, -287f), new Vector2(170f, 42f));
                Rect((RectTransform)toastPanel.transform, new Vector2(.5f, .5f), new Vector2(0f, -355f), new Vector2(360f, 76f));
                promptUi.fontSize = current != null && current.prompt.Length > 90 ? 12.5f : 14f;
                goalUi.fontSize = 16f;
            }
            complementGo.SetActive(current != null && current.allowComplement);
            LayoutTiles();
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            promptUi.text = current.prompt;
            goalUi.text = gamePhase == GamePhase.Practice || revealPractice && gamePhase == GamePhase.Reveal
                ? "파란 공 두 개를 직접 엮으시오"
                : "조건에 맞는 경우를 모두 엮으시오";
            int total = current.labels.Length;
            rawFractionUi.text = st.complementMode
                ? "반대 집합  " + st.selectedCount + " / " + total
                : "선택  " + st.selectedCount + " / " + total;
            complementUi.text = st.complementMode ? "여사건\nON · " + st.complementRings : "여사건\n반전 " + st.complementRings;
            complementImage.color = st.complementMode ? new Color32(14, 215, 199, 245) : new Color32(16, 66, 82, 238);
            int shownTime = gamePhase == GamePhase.Playing ? Mathf.CeilToInt(runLeft) : 105;
            hudUi.text = "산소 " + new string('◆', Mathf.Max(0, st.lives)) + new string('◇', Mathf.Max(0, 3 - st.lives))
                + "   주문 " + Mathf.Max(1, st.order) + "/6   " + shownTime + "초   점수 " + Mathf.RoundToInt(displayScore);
        }

        int TileAt(Vector2 screen)
        {
            if (current == null) return -1;
            for (int i = current.labels.Length - 1; i >= 0; i--)
                if (RectTransformUtility.RectangleContainsScreenPoint(tileRt[i], screen, null)) return i;
            return -1;
        }

        bool InLever(Vector2 screen) => ContainsScreen(leverRt, screen, 12f);
        bool InComplementRing(Vector2 screen) => complementGo.activeInHierarchy && ContainsScreen(complementRt, screen, 8f);
        bool InTitleHandle(Vector2 screen) => ContainsScreen(titleHandleRt, screen, 0f);
        bool InEndHandle(Vector2 screen) => ContainsScreen(endHandleRt, screen, 10f);

        static bool ContainsScreen(RectTransform rt, Vector2 screen, float padding)
        {
            if (!rt) return false;
            Vector3[] corners = new Vector3[4]; rt.GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return screen.x >= Mathf.Min(a.x, b.x) - padding && screen.x <= Mathf.Max(a.x, b.x) + padding
                && screen.y >= Mathf.Min(a.y, b.y) - padding && screen.y <= Mathf.Max(a.y, b.y) + padding;
        }

        void RefreshChainVisuals()
        {
            for (int i = 0; i < chainSeg.Length; i++) chainSeg[i].gameObject.SetActive(false);
            if (current == null) return;
            for (int i = 0; i < tileBg.Length; i++) if (tileGo[i].activeSelf)
                tileBg[i].color = (selectedMask & (1UL << i)) != 0
                    ? new Color32(21, 152, 146, 245)
                    : (i % 2 == 0 ? new Color32(32, 78, 104, 242) : new Color32(39, 72, 111, 242));
            for (int i = 1; i < chain.Count && i - 1 < chainSeg.Length; i++)
            {
                Vector2 a = tileRt[chain[i - 1]].anchoredPosition;
                Vector2 b = tileRt[chain[i]].anchoredPosition;
                PlaceLine(chainSeg[i - 1].rectTransform, a, b, 6f);
                chainSeg[i - 1].gameObject.SetActive(true);
                chainSeg[i - 1].transform.SetAsLastSibling();
            }
            for (int i = 0; i < tileGo.Length; i++) if (tileGo[i].activeSelf) tileGo[i].transform.SetAsLastSibling();
        }

        static void PlaceLine(RectTransform rt, Vector2 a, Vector2 b, float width)
        {
            Vector2 d = b - a; rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = (a + b) * .5f; rt.sizeDelta = new Vector2(d.magnitude, width);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        void SetChainTail(Vector2 screen, bool visible)
        {
            if (!visible || chain.Count == 0 || current == null) { chainTail.gameObject.SetActive(false); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRt, screen, null, out var local);
            PlaceLine(chainTail.rectTransform, tileRt[chain[chain.Count - 1]].anchoredPosition, local, 4f);
            chainTail.gameObject.SetActive(true); chainTail.transform.SetAsLastSibling();
            for (int i = 0; i < tileGo.Length; i++) if (tileGo[i].activeSelf) tileGo[i].transform.SetAsLastSibling();
        }

        void ClearChainVisuals()
        {
            for (int i = 0; i < chainSeg.Length; i++) if (chainSeg[i]) chainSeg[i].gameObject.SetActive(false);
            if (chainTail) chainTail.gameObject.SetActive(false);
            for (int i = 0; i < tileBg.Length; i++) if (tileBg[i])
            {
                tileBg[i].color = i % 2 == 0 ? new Color32(32, 78, 104, 242) : new Color32(39, 72, 111, 242);
                tileRt[i].localRotation = Quaternion.identity;
                tileRt[i].localScale = Vector3.one;
            }
        }

        void PulseTile(int index, bool add)
        {
            if (index < 0 || index >= tilePulse.Length) return;
            tilePulse[index] = add ? .22f : .14f;
        }

        void RefuseTile(int index)
        {
            tilePulse[index] = -.24f;
            ShowToast("이미 지난 칸이다 · 직전 칸으로 되짚어 해제하시오", 2f);
            MgfSfx.Play("wrong", .05f);
        }

        void SetLeverPull(float pixels, bool touching)
        {
            float threshold = 64f * Mathf.Max(1f, MgfText.Canvas.scaleFactor);
            leverPull = Mathf.Clamp01(pixels / threshold);
            st.leverPullRatio = leverPull;
            leverImage.color = Color.Lerp(new Color32(208, 74, 82, 242), new Color32(255, 196, 149, 250), leverPull);
            leverRt.localScale = new Vector3(1f + leverPull * .08f, 1f - leverPull * .10f, 1f);
            leverUi.text = leverPull >= 1f ? "손 떼어\n수주 상승" : "수주\n레버\n" + (touching ? "▼" : "↓");
        }

        void RecoilLever(bool hard)
        {
            leverRecoil = hard ? 1f : .55f;
        }

        void PressTitleHandle(bool down)
        {
            titleCtaUi.rectTransform.localScale = down ? new Vector3(1.05f, .90f, 1f) : Vector3.one;
            titleCtaUi.color = down ? Milk : Color.white;
            titleHandleImage.color = new Color32(255, 255, 255, 0);
        }

        void PressEndHandle(bool down)
        {
            endHandleRt.localScale = down ? new Vector3(1.04f, .88f, 1f) : Vector3.one;
            endHandleImage.color = down ? Cyan : new Color32(255, 107, 98, 245);
        }

        void RotateComplementRing(bool on)
        {
            complementRt.localRotation = Quaternion.Euler(0f, 0f, on ? 90f : 0f);
            complementUi.rectTransform.localRotation = Quaternion.Euler(0f, 0f, on ? -90f : 0f);
        }

        void SpawnRipple(Vector2 screen)
        {
            int i = rippleCursor++ % ripples.Length;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)playUiRoot.transform, screen, null, out var local);
            var rt = ripples[i].rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.anchoredPosition = local;
            rt.sizeDelta = new Vector2(24f, 24f); ripples[i].color = new Color32(185, 244, 240, 210); ripples[i].gameObject.SetActive(true); rippleLife[i] = .48f;
        }

        void ShowToast(string text, float seconds)
        {
            toastUi.text = text; toastPanel.SetActive(true); toastLeft = seconds;
            ((RectTransform)toastPanel.transform).localScale = new Vector3(.94f, .94f, 1f);
        }

        void PointAtActiveTarget()
        {
            guideLeft = 1.3f; guideLarge = false;
            guideDot.gameObject.SetActive(true); guideArrow.gameObject.SetActive(true);
        }

        void ReplayPracticeGuide(bool large)
        {
            if (gamePhase != GamePhase.Practice && gamePhase != GamePhase.Playing) return;
            guideLeft = large ? 3.2f : 2.4f; guideLarge = large;
            guideDot.gameObject.SetActive(true); guideLine.gameObject.SetActive(true); guideArrow.gameObject.SetActive(true);
        }

        void ResetWorldForProblem()
        {
            for (int i = 0; i < pressureLamps.Length; i++) pressureLamps[i].transform.localScale = Vector3.one;
            capsuleFxRoot.SetActive(false); waterColumnGo.SetActive(false); vortexGo.SetActive(false);
            leverRecoil = 0f; wrongMessage = "";
            SetLeverPull(0f, false); BindTiles(); LayoutTiles();
        }

        void StartCorrectCompression(CapsuleProblem p, ulong mask, bool complement)
        {
            capsuleFxRoot.SetActive(true); waterColumnGo.SetActive(true); vortexGo.SetActive(true);
            Fraction raw = complement ? new Fraction(p.labels.Length - p.answerCount, p.labels.Length) : new Fraction(p.answerCount, p.labels.Length);
            string rawText = complement ? "1 − " + (p.labels.Length - p.answerCount) + "/" + p.labels.Length : (complement ? "" : CapsuleRules.CountBits(mask) + "/" + p.labels.Length);
            if (gamePhase == GamePhase.Reveal && revealPractice) rawText = "2/4";
            capsuleUi.text = rawText + "  →  " + p.answer;
            Vector2 boardCenter = boardRt.anchoredPosition;
            Rect(capsuleRt, new Vector2(.5f, .5f), boardCenter, new Vector2(210f, 76f));
            capsuleRt.localScale = new Vector3(.35f, .35f, 1f);
            capsuleImage.color = new Color32(185, 244, 240, 242);
            Rect(vortexRt, new Vector2(.5f, .5f), boardCenter, new Vector2(72f, 72f));
            vortexRt.localRotation = Quaternion.identity;
            vortexImage.color = new Color32(157, 139, 239, 150);
            float boardH = Mathf.Max(180f, boardRt.rect.height);
            correctFractionHeight = boardH * (.25f + .58f * Mathf.Clamp01((float)p.answer.Numeric));
            waterColumnRt.anchorMin = waterColumnRt.anchorMax = new Vector2(.5f, .5f);
            waterColumnRt.pivot = new Vector2(.5f, 0f);
            waterColumnRt.anchoredPosition = boardCenter + new Vector2(0f, -boardH * .40f);
            waterColumnRt.sizeDelta = new Vector2(currentLand ? 68f : 62f, 8f);
            waterColumnImage.color = new Color32(21, 207, 195, 118);
        }

        void StartWrongRecoil(string message)
        {
            wrongMessage = message; RecoilLever(true); ShowToast(message, 2.4f);
            capsuleFxRoot.SetActive(false);
        }

        void ShowEnd(bool clear)
        {
            endTitleUi.text = clear ? "배송 완료" : "관측정 셧다운";
            endStatsUi.text = "납품 " + st.solved + "/6\n첫 시도 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal
                + "\n최종 점수 " + st.score + "\n" + (clear ? "리본가오리 소켓이 모두 채워졌다" : "산소 셀 3개가 모두 깨졌다");
            displayScore = 0f;
            endPanel.transform.localScale = new Vector3(.86f, .86f, 1f);
        }

        void AnimateWorld(float dt)
        {
            worldClock += dt;
            float t = worldClock;
            for (int i = 0; i < kelp.Length; i++) if (kelp[i])
            {
                float baseAngle = (i % 2 == 0 ? -1f : 1f) * (11f + i % 3 * 5f);
                kelp[i].localRotation = Quaternion.Euler(0f, 0f, baseAngle + Mathf.Sin(t * (.55f + i * .015f) + i) * (5f + i % 3));
            }
            for (int i = 0; i < plankton.Length; i++) if (plankton[i])
                plankton[i].position = planktonHome[i] + new Vector3(Mathf.Sin(t * .27f + i) * .18f, Mathf.Sin(t * .42f + i * .7f) * .12f, 0f);
            if (mantaRoot)
            {
                float kick = mantaKick > 0f ? Mathf.Sin((1f - mantaKick) * Mathf.PI) * .55f : 0f;
                mantaRoot.position = new Vector3(Mathf.Sin(t * .22f) * 3.7f, 3.18f + Mathf.Sin(t * .41f) * .55f + kick, 2.72f);
                mantaRoot.localRotation = Quaternion.Euler(Mathf.Sin(t * .52f) * 7f, Mathf.Sin(t * .22f) * 13f, -Mathf.Cos(t * .22f) * 7f);
                if (mantaKick > 0f) mantaKick = Mathf.Max(0f, mantaKick - dt * 1.7f);
            }
            if (gamePhase == GamePhase.Title)
            {
                float pulse = 1f + Mathf.Sin(t * 1.35f) * .025f;
                titleLogoUi.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
                titleLogoBackUi.rectTransform.localScale = titleLogoUi.rectTransform.localScale;
                float hintPulse = 1f + Mathf.Max(0f, Mathf.Sin(t * 2.0f)) * .045f;
                titleCtaUi.rectTransform.localScale = Vector3.one * hintPulse;
            }
        }

        void UpdateVisuals(float dt)
        {
            if (displayScore != st.score)
            {
                displayScore = Mathf.MoveTowards(displayScore, st.score, Mathf.Max(55f, Mathf.Abs(st.score - displayScore) * 4.5f) * dt);
                if (Mathf.Abs(displayScore - st.score) < .5f) displayScore = st.score;
                if (lastScoreTarget != st.score) { lastScoreTarget = st.score; hudUi.rectTransform.localScale = new Vector3(1.08f, 1.08f, 1f); }
            }
            if (hudUi) hudUi.rectTransform.localScale = Vector3.Lerp(hudUi.rectTransform.localScale, Vector3.one, dt * 8f);
            if (toastLeft > 0f)
            {
                toastLeft -= dt; toastPanel.transform.localScale = Vector3.Lerp(toastPanel.transform.localScale, Vector3.one, dt * 14f);
                if (toastLeft <= 0f) toastPanel.SetActive(false);
            }
            if (leverRecoil > 0f)
            {
                leverRecoil = Mathf.Max(0f, leverRecoil - dt * 2.7f);
                float x = Mathf.Sin((1f - leverRecoil) * 42f) * leverRecoil * 7f;
                leverRt.localRotation = Quaternion.Euler(0f, 0f, x);
                if (leverRecoil <= 0f) leverRt.localRotation = Quaternion.identity;
            }
            for (int i = 0; i < tilePulse.Length; i++)
            {
                if (tilePulse[i] == 0f || !tileGo[i].activeSelf) continue;
                float sign = Mathf.Sign(tilePulse[i]); tilePulse[i] = Mathf.MoveTowards(tilePulse[i], 0f, dt);
                float s = 1f + Mathf.Sin(Mathf.Abs(tilePulse[i]) * 45f) * .08f * sign;
                tileRt[i].localScale = new Vector3(s, s, 1f);
                if (tilePulse[i] == 0f) tileRt[i].localScale = Vector3.one;
            }
            for (int i = 0; i < ripples.Length; i++)
            {
                if (rippleLife[i] <= 0f) continue;
                rippleLife[i] -= dt; float k = 1f - Mathf.Clamp01(rippleLife[i] / .48f);
                ripples[i].rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(24f, 82f, k);
                ripples[i].color = new Color32(185, 244, 240, (byte)Mathf.Lerp(210f, 0f, k));
                if (rippleLife[i] <= 0f) ripples[i].gameObject.SetActive(false);
            }
            if (guideLeft > 0f) AnimateGuide(dt);
            if (gamePhase == GamePhase.Reveal && revealCorrect) AnimateCompression();
            RefreshProblemUi();
        }

        void AnimateGuide(float dt)
        {
            guideLeft -= dt;
            if (guideLeft <= 0f)
            {
                guideDot.gameObject.SetActive(false); guideLine.gameObject.SetActive(false); guideArrow.gameObject.SetActive(false); return;
            }
            // 정답 칸을 가리키지 않는다. 보드 바깥 빈 연습 홈에서 드래그 동작만 시범 보인다.
            Vector3[] corners = new Vector3[4]; boardRt.GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(null, (corners[1] + corners[2]) * .5f) + new Vector2(22f, 42f);
            Vector2 b = a + new Vector2(0f, -86f);
            float cycle = 1f - (guideLeft % 1.15f) / 1.15f;
            Vector2 p = Vector2.Lerp(a, b, Mathf.SmoothStep(0f, 1f, cycle));
            PositionScreenRect(guideDot.rectTransform, p);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)playUiRoot.transform, a, null, out var al);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)playUiRoot.transform, b, null, out var bl);
            PlaceLine(guideLine.rectTransform, al, bl, guideLarge ? 8f : 5f);
            PositionScreenRect(guideArrow.rectTransform, b);
            float s = guideLarge ? 1.45f : 1f; guideDot.rectTransform.localScale = Vector3.one * (s + Mathf.Sin(worldClock * 7f) * .08f);
        }

        void PositionScreenRect(RectTransform rt, Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)playUiRoot.transform, screen, null, out var local);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.anchoredPosition = local;
        }

        void AnimateCompression()
        {
            float k = Mathf.Clamp01(revealClock / 1.5f);
            float gather = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / .42f));
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - .34f) / .66f));
            float scale = k < .40f ? Mathf.Lerp(.35f, 1.08f, gather) : Mathf.Lerp(1.08f, .78f, rise);
            capsuleRt.localScale = new Vector3(scale, scale, 1f);
            Vector2 start = boardRt.anchoredPosition;
            Vector2 end = start + new Vector2(Mathf.Sin(rise * Mathf.PI * 2f) * 16f, correctFractionHeight + 52f);
            capsuleRt.anchoredPosition = Vector2.Lerp(start, end, rise);

            // 선택 경우가 보드 중앙의 물살 마름모로 모인 뒤, 답의 분수 크기만큼
            // 수주가 실제로 차오르고 캡슐이 부력으로 상승한다.
            vortexRt.anchoredPosition = start;
            vortexRt.localRotation = Quaternion.Euler(0f, 0f, k * 330f);
            float vortexScale = Mathf.Lerp(.45f, 1.55f, Mathf.Sin(Mathf.Clamp01(k / .72f) * Mathf.PI));
            vortexRt.localScale = new Vector3(vortexScale, vortexScale, 1f);
            vortexImage.color = new Color32(157, 139, 239, (byte)Mathf.Lerp(165f, 0f, rise));
            waterColumnRt.sizeDelta = new Vector2(currentLand ? 68f : 62f, Mathf.Lerp(8f, correctFractionHeight, rise));
            waterColumnImage.color = new Color32(21, 207, 195, (byte)Mathf.Lerp(105f, 205f, rise));
            for (int rank = 0; rank < chain.Count; rank++)
            {
                int idx = chain[rank]; float on = Mathf.Clamp01((revealClock - rank * .045f) / .20f);
                if (on > 0f)
                {
                    tileBg[idx].color = Color.Lerp(new Color32(21, 152, 146, 245), new Color32(210, 245, 234, 245), on);
                    float tileScale = Mathf.Lerp(1f, .72f, gather) + Mathf.Sin((k + rank * .11f) * Mathf.PI * 4f) * .035f;
                    tileRt[idx].localScale = new Vector3(tileScale, tileScale, 1f);
                    tileRt[idx].localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, rank % 2 == 0 ? 12f : -12f, gather));
                }
            }
            for (int i = 0; i < pressureLamps.Length; i++)
            {
                float wave = Mathf.Sin((k * 2f + i * .17f) * Mathf.PI);
                pressureLamps[i].transform.localScale = Vector3.one * (1f + Mathf.Max(0f, wave) * .28f * (1f - rise * .45f));
            }
            if (k > .82f) mantaKick = Mathf.Max(mantaKick, .35f);
        }

        void UpdateControlCoordinates()
        {
            st.screenW = Screen.width; st.screenH = Screen.height;
            if (current != null)
            {
                st.tilePx = new int[current.labels.Length * 2];
                for (int i = 0; i < current.labels.Length; i++)
                {
                    Vector2 p = RectCenterScreen(tileRt[i]); st.tilePx[i * 2] = Mathf.RoundToInt(p.x); st.tilePx[i * 2 + 1] = Mathf.RoundToInt(Screen.height - p.y);
                }
            }
            else st.tilePx = new int[0];
            Vector2 l = leverRt ? RectCenterScreen(leverRt) : Vector2.zero;
            Vector2 c = complementRt ? RectCenterScreen(complementRt) : Vector2.zero;
            Vector2 t = titleHandleRt ? RectCenterScreen(titleHandleRt) : Vector2.zero;
            Vector2 e = endHandleRt ? RectCenterScreen(endHandleRt) : Vector2.zero;
            st.controlPx = new[] {
                Mathf.RoundToInt(l.x), Mathf.RoundToInt(Screen.height-l.y),
                Mathf.RoundToInt(c.x), Mathf.RoundToInt(Screen.height-c.y),
                Mathf.RoundToInt(t.x), Mathf.RoundToInt(Screen.height-t.y),
                Mathf.RoundToInt(e.x), Mathf.RoundToInt(Screen.height-e.y)
            };
            if (current != null)
            {
                st.layoutPx = new int[20];
                WriteBoundsTop(st.layoutPx, 0, promptRt);
                WriteBoundsTop(st.layoutPx, 4, (RectTransform)goalUi.transform);
                WriteBoundsTop(st.layoutPx, 8, boardRt);
                WriteBoundsTop(st.layoutPx, 12, leverRt);
                WriteBoundsTop(st.layoutPx, 16, (RectTransform)rawFractionUi.transform);
            }
            else st.layoutPx = new int[0];
        }

        static void WriteBoundsTop(int[] target, int offset, RectTransform rt)
        {
            Vector3[] c = new Vector3[4]; rt.GetWorldCorners(c);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(null, c[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(null, c[2]);
            target[offset] = Mathf.RoundToInt(Mathf.Min(a.x, b.x));
            target[offset + 1] = Mathf.RoundToInt(Screen.height - Mathf.Max(a.y, b.y));
            target[offset + 2] = Mathf.RoundToInt(Mathf.Max(a.x, b.x));
            target[offset + 3] = Mathf.RoundToInt(Screen.height - Mathf.Min(a.y, b.y));
        }

        static Vector2 RectCenterScreen(RectTransform rt)
        {
            Vector3[] c = new Vector3[4]; rt.GetWorldCorners(c); return RectTransformUtility.WorldToScreenPoint(null, (c[0] + c[2]) * .5f);
        }
    }
}
