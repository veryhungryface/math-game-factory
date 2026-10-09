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
        GameObject promptPanel, toastPanel, capsuleFxRoot, titleHandleGo, leverGo, complementGo, endPanel, endHandleGo;
        RectTransform promptRt, boardRt, leverRt, complementRt, titleHandleRt, endHandleRt, capsuleRt;
        TextMeshProUGUI promptUi, goalUi, hudUi, rawFractionUi, toastUi, leverUi, complementUi;
        TextMeshProUGUI titleLogoBackUi, titleLogoUi, titleTagUi, titleMetaUi, titleCtaUi;
        TextMeshProUGUI capsuleUi, endTitleUi, endStatsUi, endCtaUi;
        Image leverImage, complementImage, titleHandleImage, endHandleImage, capsuleImage;

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
        float toastLeft, leverRecoil;
        string wrongMessage = "";
        int lastScreenW = -1, lastScreenH = -1;
        bool currentLand;
        int activeCols, activeRows;
        float displayScore;
        int lastScoreTarget = -1;

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("061522"), MgfLook.Hex("0B3550"), MgfLook.Hex("020A13"));
            MgfLook.Sun(new Vector3(42f, -28f, -12f), MgfLook.Hex("BFFCF4"), 1.12f);
            cam = MgfLook.Camera(new Vector3(0f, 8.8f, -14.6f), new Vector3(0f, .2f, 0f), 34f);
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;

            stationRoot = new GameObject("DeepSeaStation").transform;
            var hull = MgfLook.Lit(MgfLook.Hex("102C42"), .22f, .34f);
            var acrylic = MgfLook.Lit(MgfLook.Hex("183D52"), .48f, .14f, MgfLook.Hex("05222C"));
            var rim = MgfLook.Lit(MgfLook.Hex("58758A"), .62f, .45f);
            var coral = MgfLook.Lit(Coral, .38f, .22f, MgfLook.Hex("45120F"));
            var cyan = MgfLook.Lit(MgfLook.Hex("0A9C96"), .45f, .15f, MgfLook.Hex("0ED7C7"));

            MgfLook.Block("StationDeck", new Vector3(0f, -1.25f, .5f), new Vector3(15.8f, .7f, 9.8f), .46f, hull).transform.SetParent(stationRoot, true);
            MgfLook.Block("AcrylicChamber", new Vector3(0f, -.66f, .4f), new Vector3(11.8f, .35f, 7.0f), .28f, acrylic).transform.SetParent(stationRoot, true);
            MgfLook.Block("LeftConsole", new Vector3(-6.8f, .25f, .9f), new Vector3(2.3f, 2.8f, 5.8f), .35f, hull).transform.SetParent(stationRoot, true);
            MgfLook.Block("RightConsole", new Vector3(6.8f, .15f, .9f), new Vector3(2.3f, 2.6f, 5.8f), .35f, hull).transform.SetParent(stationRoot, true);

            // 파노라마 관측창 테두리: 빈 배경 이미지 없이 저비용 3D 부품으로 화면을 채운다.
            for (int i = 0; i < 20; i++)
            {
                float a = i / 20f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 7.1f, 3.15f + Mathf.Sin(a) * 2.55f, 3.65f);
                var b = MgfLook.Block("PortholeRim" + i, p, new Vector3(.8f, .58f, .55f), .18f, rim);
                b.transform.rotation = Quaternion.Euler(0f, 0f, -a * Mathf.Rad2Deg + 90f);
                b.transform.SetParent(stationRoot, true);
            }
            MgfLook.Block("TitleRimSign", new Vector3(0f, 5.82f, 3.5f), new Vector3(5.7f, .7f, .42f), .22f, hull).transform.SetParent(stationRoot, true);

            // 수압관과 산호색 안전 레버가 타이틀부터 같은 세계에 보인다.
            for (int i = 0; i < pressureLamps.Length; i++)
            {
                float x = -4.4f + i * 1.76f;
                MgfLook.Block("PressureTube" + i, new Vector3(x, -.5f, 3.2f), new Vector3(.82f, .38f, 2.1f), .19f, rim).transform.SetParent(stationRoot, true);
                pressureLamps[i] = MgfLook.Block("TubeLight" + i, new Vector3(x, -.28f, 2.12f), new Vector3(.52f, .18f, .34f), .14f, cyan);
                pressureLamps[i].transform.SetParent(stationRoot, true);
            }
            MgfLook.Block("SafetyLeverBase", new Vector3(6.45f, .25f, -1.2f), new Vector3(1.15f, 1.9f, 1.2f), .28f, rim).transform.SetParent(stationRoot, true);
            MgfLook.Block("SafetyLever", new Vector3(6.45f, 1.52f, -1.18f), new Vector3(.55f, 2.25f, .55f), .24f, coral).transform.SetParent(stationRoot, true);

            // 해초 실루엣과 미립자는 고정 풀: 매 프레임 Instantiate/Destroy 없음.
            var kelpMat = MgfLook.Lit(MgfLook.Hex("0A524D"), .08f, 0f, MgfLook.Hex("001C1A"));
            for (int i = 0; i < kelp.Length; i++)
            {
                float x = -7.4f + i * 1.35f;
                var k = MgfLook.Block("Kelp" + i, new Vector3(x, .25f + (i % 3) * .22f, 4.4f), new Vector3(.22f, 2.4f + (i % 4) * .45f, .24f), .18f, kelpMat);
                k.transform.SetParent(stationRoot, true);
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
            meshGo.GetComponent<MeshRenderer>().sharedMaterial = MgfLook.Lit(MgfLook.Hex("3E6B7D"), .82f, .58f, MgfLook.Hex("071E29"));
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
            titleLogoBackUi.outlineWidth = .36f; titleLogoBackUi.outlineColor = new Color32(185, 244, 240, 235);
            titleLogoUi = MakeText("캡슐 찰칵", titleUiRoot.transform, 50f, Milk, TextAlignmentOptions.Center);
            titleLogoUi.outlineWidth = .17f; titleLogoUi.outlineColor = new Color32(7, 26, 43, 255);
            titleTagUi = MakeText("경우를 엮어 캡슐을 뽑아라", titleUiRoot.transform, 20f, Ink, TextAlignmentOptions.Center);
            titleTagUi.outlineWidth = .18f; titleTagUi.outlineColor = new Color32(7, 26, 43, 230);
            titleMetaUi = MakeText("중학교 2학년 · 확률   최고 0점", titleUiRoot.transform, 14f, Ink, TextAlignmentOptions.Center);
            titleMetaUi.outlineWidth = .15f; titleMetaUi.outlineColor = new Color32(7, 26, 43, 220);

            titleHandleGo = Panel("TitlePressureHandle", titleUiRoot.transform, new Color32(255, 107, 98, 245), out titleHandleImage);
            titleHandleRt = (RectTransform)titleHandleGo.transform;
            titleCtaUi = MakeText("수압 핸들 열기", titleHandleGo.transform, 19f, Color.white, TextAlignmentOptions.Center);
            titleCtaUi.outlineWidth = .12f; titleCtaUi.outlineColor = new Color32(71, 18, 15, 210);
            Rect((RectTransform)titleLogoBackUi.transform, new Vector2(.5f, .78f), new Vector2(3f, -4f), new Vector2(370f, 76f));
            Rect((RectTransform)titleLogoUi.transform, new Vector2(.5f, .78f), Vector2.zero, new Vector2(370f, 76f));
            Rect((RectTransform)titleTagUi.transform, new Vector2(.5f, .68f), Vector2.zero, new Vector2(360f, 42f));
            Rect((RectTransform)titleMetaUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -26f), new Vector2(330f, 28f));
            Rect(titleHandleRt, new Vector2(.5f, 0f), new Vector2(0f, 74f), new Vector2(238f, 72f));
            StretchInset((RectTransform)titleCtaUi.transform, 5f);
        }

        void BuildPlayUi()
        {
            promptPanel = Panel("OrderCard", playUiRoot.transform, new Color32(7, 26, 43, 232), out _);
            promptRt = (RectTransform)promptPanel.transform;
            promptUi = MakeText("", promptPanel.transform, 16f, Ink, TextAlignmentOptions.Center);
            promptUi.textWrappingMode = TextWrappingModes.Normal;
            goalUi = MakeText("조건에 맞는 경우를 엮어 레버를 당겨라", playUiRoot.transform, 16f, Ink, TextAlignmentOptions.Center);
            goalUi.outlineWidth = .16f; goalUi.outlineColor = new Color32(7, 26, 43, 235);
            hudUi = MakeText("", playUiRoot.transform, 15f, Ink, TextAlignmentOptions.Center);
            hudUi.outlineWidth = .14f; hudUi.outlineColor = new Color32(7, 26, 43, 235);
            rawFractionUi = MakeText("선택 0 / 전체 0", playUiRoot.transform, 18f, Milk, TextAlignmentOptions.Center);
            rawFractionUi.outlineWidth = .14f; rawFractionUi.outlineColor = new Color32(7, 26, 43, 235);

            boardRt = (RectTransform)Root("OutcomeBoard", playUiRoot.transform).transform;
            var boardBg = boardRt.gameObject.AddComponent<Image>(); boardBg.color = new Color32(10, 44, 62, 186); boardBg.raycastTarget = false;
            for (int i = 0; i < tileGo.Length; i++)
            {
                tileGo[i] = Panel("Outcome" + i, boardRt, new Color32(28, 67, 86, 245), out tileBg[i]);
                tileRt[i] = (RectTransform)tileGo[i].transform;
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

            leverGo = Panel("PressureLever", playUiRoot.transform, new Color32(91, 108, 134, 238), out leverImage);
            leverRt = (RectTransform)leverGo.transform;
            leverUi = MakeText("수압\n레버\n▼", leverGo.transform, 15f, Color.white, TextAlignmentOptions.Center);
            StretchInset((RectTransform)leverUi.transform, 4f);
            complementGo = Panel("ComplementRing", playUiRoot.transform, new Color32(16, 66, 82, 238), out complementImage);
            complementRt = (RectTransform)complementGo.transform;
            complementUi = MakeText("여사건\n반전 2", complementGo.transform, 13f, Milk, TextAlignmentOptions.Center);
            StretchInset((RectTransform)complementUi.transform, 3f);

            toastPanel = Panel("Toast", playUiRoot.transform, new Color32(7, 26, 43, 244), out _);
            toastUi = MakeText("", toastPanel.transform, 14f, Color.white, TextAlignmentOptions.Center);
            toastUi.textWrappingMode = TextWrappingModes.Normal;
            StretchInset((RectTransform)toastUi.transform, 8f); toastPanel.SetActive(false);

            capsuleFxRoot = Panel("CapsuleFx", playUiRoot.transform, new Color32(185, 244, 240, 242), out capsuleImage);
            capsuleRt = (RectTransform)capsuleFxRoot.transform;
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
            capsuleFxRoot.SetActive(false); toastPanel.SetActive(false);
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
                tileBg[i].color = new Color32(28, 67, 86, 245);
                tileRt[i].localScale = Vector3.one;
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
            if (Screen.width == lastScreenW && Screen.height == lastScreenH && land == currentLand) return;
            lastScreenW = Screen.width; lastScreenH = Screen.height; currentLand = land;
            cam.orthographicSize = land ? 5.55f : 7.65f;
            cam.transform.position = land ? new Vector3(0f, 8.1f, -14.8f) : new Vector3(0f, 9.2f, -15.8f);
            cam.transform.LookAt(new Vector3(0f, .4f, .5f));

            if (gamePhase == GamePhase.Title)
            {
                Rect((RectTransform)titleLogoBackUi.transform, new Vector2(.5f, land ? .72f : .78f), new Vector2(3f, -4f), new Vector2(land ? 560f : 370f, 86f));
                Rect((RectTransform)titleLogoUi.transform, new Vector2(.5f, land ? .72f : .78f), Vector2.zero, new Vector2(land ? 560f : 370f, 86f));
                Rect((RectTransform)titleTagUi.transform, new Vector2(.5f, land ? .62f : .68f), Vector2.zero, new Vector2(land ? 520f : 360f, 42f));
                Rect(titleHandleRt, new Vector2(.5f, 0f), new Vector2(0f, land ? 62f : 74f), new Vector2(land ? 280f : 238f, 72f));
                titleLogoUi.fontSize = land ? 62f : 50f; titleLogoBackUi.fontSize = titleLogoUi.fontSize;
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
                Rect(promptRt, new Vector2(.17f, .58f), Vector2.zero, new Vector2(330f, 205f));
                Rect((RectTransform)promptUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(304f, 184f));
                Rect((RectTransform)goalUi.transform, new Vector2(.18f, .30f), Vector2.zero, new Vector2(350f, 44f));
                Rect((RectTransform)hudUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -22f), new Vector2(580f, 34f));
                Rect(boardRt, new Vector2(.58f, .50f), Vector2.zero, new Vector2(count == 36 ? 474f : 420f, count == 36 ? 474f : 330f));
                Rect(leverRt, new Vector2(.90f, .49f), Vector2.zero, new Vector2(82f, 250f));
                Rect(complementRt, new Vector2(.90f, .77f), Vector2.zero, new Vector2(94f, 78f));
                Rect((RectTransform)rawFractionUi.transform, new Vector2(.90f, .27f), Vector2.zero, new Vector2(190f, 44f));
                Rect((RectTransform)toastPanel.transform, new Vector2(.50f, .12f), Vector2.zero, new Vector2(430f, 72f));
                promptUi.fontSize = current != null && current.prompt.Length > 90 ? 13f : 15f;
            }
            else
            {
                Rect((RectTransform)hudUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -19f), new Vector2(370f, 32f));
                Rect(promptRt, new Vector2(.5f, 1f), new Vector2(0f, -102f), new Vector2(370f, 126f));
                Rect((RectTransform)promptUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(348f, 110f));
                Rect((RectTransform)goalUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -179f), new Vector2(370f, 40f));
                Rect(boardRt, new Vector2(.46f, .51f), new Vector2(-8f, count == 36 ? -2f : -16f), new Vector2(count == 36 ? 330f : 310f, count == 36 ? 330f : 286f));
                Rect(leverRt, new Vector2(1f, .49f), new Vector2(-29f, -24f), new Vector2(56f, 228f));
                Rect(complementRt, new Vector2(1f, .73f), new Vector2(-43f, -6f), new Vector2(76f, 70f));
                Rect((RectTransform)rawFractionUi.transform, new Vector2(.42f, .25f), Vector2.zero, new Vector2(310f, 42f));
                Rect((RectTransform)toastPanel.transform, new Vector2(.5f, .17f), Vector2.zero, new Vector2(360f, 76f));
                promptUi.fontSize = current != null && current.prompt.Length > 90 ? 12.5f : 14f;
            }
            complementGo.SetActive(current != null && current.allowComplement);
            LayoutTiles();
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            promptUi.text = current.prompt;
            goalUi.text = gamePhase == GamePhase.Practice || revealPractice && gamePhase == GamePhase.Reveal
                ? "파란 공 두 개를 직접 엮고 레버를 당기시오"
                : "조건에 맞는 모든 경우를 엮고 레버를 당기시오";
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

        bool InLever(Vector2 screen) => leverRt != null && RectTransformUtility.RectangleContainsScreenPoint(leverRt, screen, null);
        bool InComplementRing(Vector2 screen) => complementGo.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(complementRt, screen, null);
        bool InTitleHandle(Vector2 screen) => titleHandleRt != null && RectTransformUtility.RectangleContainsScreenPoint(titleHandleRt, screen, null);
        bool InEndHandle(Vector2 screen) => endHandleRt != null && RectTransformUtility.RectangleContainsScreenPoint(endHandleRt, screen, null);

        void RefreshChainVisuals()
        {
            for (int i = 0; i < chainSeg.Length; i++) chainSeg[i].gameObject.SetActive(false);
            if (current == null) return;
            for (int i = 0; i < tileBg.Length; i++) if (tileGo[i].activeSelf)
                tileBg[i].color = (selectedMask & (1UL << i)) != 0 ? new Color32(14, 115, 115, 245) : new Color32(28, 67, 86, 245);
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
            for (int i = 0; i < tileBg.Length; i++) if (tileBg[i]) tileBg[i].color = new Color32(28, 67, 86, 245);
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
            leverImage.color = Color.Lerp(new Color32(91, 108, 134, 238), new Color32(255, 107, 98, 250), leverPull);
            leverRt.localScale = new Vector3(1f + leverPull * .08f, 1f - leverPull * .10f, 1f);
            leverUi.text = leverPull >= 1f ? "손 떼어\n압축" : "수압\n레버\n" + (touching ? "▼" : "↓");
        }

        void RecoilLever(bool hard)
        {
            leverRecoil = hard ? 1f : .55f;
        }

        void PressTitleHandle(bool down)
        {
            titleHandleRt.localScale = down ? new Vector3(1.04f, .88f, 1f) : Vector3.one;
            titleHandleImage.color = down ? Cyan : new Color32(255, 107, 98, 245);
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
            capsuleFxRoot.SetActive(false); leverRecoil = 0f; wrongMessage = "";
            SetLeverPull(0f, false); BindTiles(); LayoutTiles();
        }

        void StartCorrectCompression(CapsuleProblem p, ulong mask, bool complement)
        {
            capsuleFxRoot.SetActive(true);
            Fraction raw = complement ? new Fraction(p.labels.Length - p.answerCount, p.labels.Length) : new Fraction(p.answerCount, p.labels.Length);
            string rawText = complement ? "1 − " + (p.labels.Length - p.answerCount) + "/" + p.labels.Length : (complement ? "" : CapsuleRules.CountBits(mask) + "/" + p.labels.Length);
            if (gamePhase == GamePhase.Reveal && revealPractice) rawText = "2/4";
            capsuleUi.text = rawText + "  →  " + p.answer;
            Rect(capsuleRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(210f, 76f));
            capsuleRt.localScale = new Vector3(.35f, .35f, 1f);
            capsuleImage.color = new Color32(185, 244, 240, 242);
            mantaKick = 1f;
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
                kelp[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * (.55f + i * .015f) + i) * (8f + i % 4));
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
                titleHandleRt.localScale = Vector3.one * (1f + Mathf.Max(0f, Mathf.Sin(t * 2.0f)) * .035f);
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
            float scale = k < .35f ? Mathf.Lerp(.35f, 1.12f, Mathf.SmoothStep(0f, 1f, k / .35f)) : Mathf.Lerp(1.12f, .82f, (k - .35f) / .65f);
            capsuleRt.localScale = new Vector3(scale, scale, 1f);
            Vector2 start = currentLand ? new Vector2(0f, 0f) : new Vector2(-10f, -5f);
            Vector2 end = currentLand ? new Vector2(-310f, 165f) : new Vector2(0f, 250f);
            capsuleRt.anchoredPosition = Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - .42f) / .58f)));
            for (int rank = 0; rank < chain.Count; rank++)
            {
                int idx = chain[rank]; float on = Mathf.Clamp01((revealClock - rank * .045f) / .20f);
                if (on > 0f) tileBg[idx].color = Color.Lerp(new Color32(14, 115, 115, 245), new Color32(185, 244, 240, 245), on);
            }
            for (int i = 0; i < pressureLamps.Length; i++)
            {
                float on = Mathf.Clamp01((revealClock - .35f - i * .07f) / .18f);
                pressureLamps[i].transform.localScale = Vector3.one * (1f + Mathf.Sin(on * Mathf.PI) * .55f);
            }
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
        }

        static Vector2 RectCenterScreen(RectTransform rt)
        {
            Vector3[] c = new Vector3[4]; rt.GetWorldCorners(c); return RectTransformUtility.WorldToScreenPoint(null, (c[0] + c[2]) * .5f);
        }
    }
}
