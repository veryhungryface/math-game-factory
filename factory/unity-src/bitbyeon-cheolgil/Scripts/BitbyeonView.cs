// 빗변 철길 화면 — 도색 금속 기계식 신호소, 벽면 릴레이 노선반, 펀칭 티켓 셔틀과 UI 크랭크.
// 정답은 릴레이 연쇄 잠금+신호 로봇 반응이며 점수 팝업/정답 화면 흔들림/원형 파티클 버스트를 쓰지 않는다.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.BitbyeonCheolgil
{
    public partial class BitbyeonGame
    {
        static readonly Color Indigo = MgfLook.Hex("#102A56");
        static readonly Color Blue = MgfLook.Hex("#2E78B7");
        static readonly Color Ice = MgfLook.Hex("#BFEAF5");
        static readonly Color Snow = MgfLook.Hex("#F2F7FA");
        static readonly Color Coral = MgfLook.Hex("#F05D5E");
        static readonly Color BlackRubber = MgfLook.Hex("#111923");
        static readonly Color Deep = MgfLook.Hex("#07152E");
        static readonly Color Brass = MgfLook.Hex("#F0B44D");
        static readonly Color Teal = MgfLook.Hex("#197A78");
        static readonly Color WarmPaper = MgfLook.Hex("#FFF1CE");
        static readonly Color RoomCream = MgfLook.Hex("#D9E2CF");
        static readonly Color MintEnamel = MgfLook.Hex("#6FA69A");
        static readonly Color PanelNavy = MgfLook.Hex("#173C4A");

        Camera cam;
        Vector3 cameraHome;
        Transform worldRoot, auroraRoot, trainRoot, trainCab, safetyHook, operatorRoot, operatorAntenna;
        readonly Transform[] bridgeSegments = new Transform[12];
        readonly Transform[] snowBits = new Transform[42];
        readonly Vector3[] snowSeeds = new Vector3[42];
        Material snowMat, ridgeMat, ridgeFarMat, cobaltMat, iceMat, iceGlowMat, rubberMat, coralMat, cableMat, cableRedMat;
        LineRenderer mainCable, safetyCable;
        float worldClock;

        RectTransform rootRt, titleRt, titleSignRt, titleTopBusRt, titleBottomBusRt, titleCtaRailRt;
        RectTransform hudRt, endRt, controlsRt, problemRt, crankRt, wheelRt, brakeTrackRt, brakeRt;
        RectTransform guideHandRt, guideArrowRt, rippleRt, toastRt, revealRt, snowWipeRt;
        RectTransform triangleRt, lineARt, lineBRt, lineCRt;
        CanvasGroup titleG, hudG, endG, toastG, revealG;
        RawImage titleArt;
        TextMeshProUGUI titleLogo, titleSub, titleMeta, titleBest, titleCta;
        TextMeshProUGUI goalText, progressText, pressureText, cargoText, scoreText, promptText, modeText;
        TextMeshProUGUI sideAText, sideBText, sideCText, areaHintText, tickText, tickUnitText, brakeText;
        TextMeshProUGUI toastText, revealTitle, revealText, endTitle, endScore, endStats, endCta;
        readonly RectTransform[] tickMarks = new RectTransform[22];
        readonly RectTransform[] wipeFlakes = new RectTransform[18];
        Sprite roundSprite, ringSprite, softSprite;
        float toastTime, revealTime, rippleTime, guideBoost, titlePulse, displayedScore, endDisplayedScore;
        float wheelVisualAngle;
        int layoutW = -1, layoutH = -1, lastScore = -1;

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
            RectTransform rt = R(name, parent, anchor, pos, size);
            var im = rt.gameObject.AddComponent<Image>();
            im.color = color; im.sprite = sprite; im.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color, float width = 360f,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            RectTransform rt = R("Text", parent, anchor, pos, new Vector2(width, size * 1.75f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text;
            return t;
        }

        static Sprite MakeDisc(int size, bool ring, float radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "RailRing" : "RailDisc", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float c = size * .5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + .5f - c, dy = y + .5f - c, d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = ring ? Mathf.Clamp01(1f - Mathf.Abs(d - c * .78f) / (c * .13f)) : Mathf.Clamp01(radius - d + .7f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100);
        }

        void BuildWorld()
        {
            var keep = new GameObject("PrimitiveColliderTypes");
            keep.AddComponent<CapsuleCollider>(); keep.AddComponent<SphereCollider>(); Destroy(keep);

            MgfLook.Sky(MgfLook.Hex("#9EBDB2"), MgfLook.Hex("#D8E2CF"), MgfLook.Hex("#557C76"), .92f);
            MgfLook.Sun(new Vector3(42, -32, 12), MgfLook.Hex("#FFF0C9"), 1.42f, .58f);
            cam = MgfLook.Camera(new Vector3(11.8f, 10.5f, -13.5f), new Vector3(0, 1.8f, 3.6f), 35f);
            cam.orthographic = true; cam.orthographicSize = 7.35f; cameraHome = cam.transform.position;

            snowMat = MgfLook.Lit(WarmPaper, .28f, .04f);
            ridgeMat = MgfLook.Lit(MintEnamel, .18f, .20f, Teal * .03f);
            ridgeFarMat = MgfLook.Lit(RoomCream, .08f, .14f);
            cobaltMat = MgfLook.Lit(PanelNavy, .30f, .24f, MgfLook.Hex("#123E55") * .08f);
            iceMat = MgfLook.Lit(MgfLook.Hex("#315D62"), .18f, .20f, Teal * .03f);
            iceGlowMat = MgfLook.Lit(Brass, .82f, .58f, Brass * .72f);
            rubberMat = MgfLook.Lit(BlackRubber, .08f, .18f);
            coralMat = MgfLook.Lit(Coral, .3f, .34f, Coral * .12f);
            cableMat = MgfLook.Unlit(Brass);
            cableRedMat = MgfLook.Unlit(Coral);

            worldRoot = new GameObject("AuroraRelaySignalRoom").transform;
            auroraRoot = new GameObject("MovingSignalRails").transform; auroraRoot.SetParent(worldRoot, false);
            for (int i = 0; i < 4; i++)
            {
                var ribbon = MgfLook.Block("TickerRail" + i, new Vector3(-4.8f + i * 3.2f, 5.8f - (i % 2) * .35f, 6.15f),
                    new Vector3(2.6f, .10f, .22f), .04f, i % 2 == 0 ? iceGlowMat : coralMat, auroraRoot);
                Destroy(ribbon.GetComponent<Collider>()); ribbon.transform.localRotation = Quaternion.Euler(0, 0, -4 + i * 2.5f);
            }

            var floor = MgfLook.Block("PaintedMetalFloor", new Vector3(0, -1.05f, 3.7f), new Vector3(21f, .45f, 18f), .04f, ridgeFarMat, worldRoot);
            var wall = MgfLook.Block("RelayRoomWall", new Vector3(0, 3.05f, 7.65f), new Vector3(20f, 9.2f, .55f), .04f, ridgeFarMat, worldRoot);
            var board = MgfLook.Block("MechanicalRouteBoard", new Vector3(0, 2.75f, 7.16f), new Vector3(12.5f, 5.4f, .42f), .06f, ridgeMat, worldRoot);
            var desk = MgfLook.Block("SignalDesk", new Vector3(0, -.10f, 2.6f), new Vector3(15.8f, 1.0f, 5.1f), .06f, cobaltMat, worldRoot);
            Destroy(floor.GetComponent<Collider>()); Destroy(wall.GetComponent<Collider>()); Destroy(board.GetComponent<Collider>()); Destroy(desk.GetComponent<Collider>());

            // 화면 가장자리를 구조물로 닫아 '어둠 속 떠 있는 작업대'가 아니라 다층 신호실로 읽히게 한다.
            for (int side = -1; side <= 1; side += 2)
            {
                var cabinet = MgfLook.Block("SideRelayCabinet" + side, new Vector3(side * 7.15f, 2.25f, 5.85f),
                    new Vector3(2.0f, 6.5f, 2.2f), .05f, cobaltMat, worldRoot);
                Destroy(cabinet.GetComponent<Collider>());
                for (int row = 0; row < 4; row++)
                {
                    var drawer = MgfLook.Block("TicketDrawer" + side + "_" + row,
                        new Vector3(side * 7.12f, .15f + row * 1.35f, 4.68f), new Vector3(1.48f, .76f, .12f), .025f,
                        row % 2 == 0 ? snowMat : ridgeMat, worldRoot);
                    Destroy(drawer.GetComponent<Collider>());
                    var lever = MgfLook.Block("EnamelLever" + side + "_" + row,
                        new Vector3(side * (6.82f + row % 2 * .18f), .15f + row * 1.35f, 4.48f),
                        new Vector3(.18f, .52f, .18f), .018f, row == 3 ? coralMat : iceGlowMat, worldRoot);
                    Destroy(lever.GetComponent<Collider>());
                }
            }
            for (int i = 0; i < 5; i++)
            {
                var ceiling = MgfLook.Block("OverheadBus" + i, new Vector3(-6.4f + i * 3.2f, 6.65f, 5.55f),
                    new Vector3(2.6f, .30f, 1.1f), .025f, i % 2 == 0 ? cobaltMat : ridgeMat, worldRoot);
                Destroy(ceiling.GetComponent<Collider>());
            }
            for (int i = 0; i < 9; i++)
            {
                float x = -5.6f + i * 1.4f;
                var bank = MgfLook.Block("ExposedCableBank" + i, new Vector3(x, .58f, 2.12f),
                    new Vector3(.92f, .48f, .78f), .025f, i % 3 == 0 ? coralMat : (i % 2 == 0 ? ridgeMat : snowMat), worldRoot);
                Destroy(bank.GetComponent<Collider>());
                var slot = MgfLook.Block("CableSlot" + i, new Vector3(x, .64f, 1.69f),
                    new Vector3(.48f, .13f, .05f), .01f, rubberMat, worldRoot);
                Destroy(slot.GetComponent<Collider>());
            }
            for (int i = 0; i < bridgeSegments.Length; i++)
            {
                float x = -3.9f + (i % 4) * 2.6f;
                float y = 1.35f + (i / 4) * 1.35f;
                var seg = MgfLook.Block("RelayCell" + i, new Vector3(x, y, 6.78f),
                    new Vector3(1.45f, .72f, .30f), .035f, iceMat, worldRoot);
                Destroy(seg.GetComponent<Collider>()); bridgeSegments[i] = seg.transform;
            }
            mainCable = CreateCable("BrassRouteTrace", cableMat, new Vector3(-5.4f, 4.95f, 6.52f), new Vector3(5.4f, 4.95f, 6.52f), 22);
            safetyCable = CreateCable("FaultRouteTrace", cableRedMat, new Vector3(-1.2f, 3.2f, 6.42f), new Vector3(1.2f, .65f, 3.0f), 10);
            safetyCable.gameObject.SetActive(false);
            safetyHook = MgfLook.Prim(PrimitiveType.Cylinder, "FaultLamp", new Vector3(0, .42f, 2.8f), new Vector3(.34f, .20f, .34f), coralMat, worldRoot, false).transform;
            safetyHook.gameObject.SetActive(false);
            CreateTrain();
            CreateOperator();

            for (int i = 0; i < snowBits.Length; i++)
            {
                float x = -8f + (i * 37 % 160) / 10f;
                float y = .4f + (i * 53 % 58) / 10f;
                float z = 1f + (i * 29 % 58) / 10f;
                snowSeeds[i] = new Vector3(x, y, z);
                Transform s = MgfLook.Prim(PrimitiveType.Sphere, "BrassDust" + i, snowSeeds[i],
                    Vector3.one * (.018f + (i % 4) * .009f), i % 3 == 0 ? iceGlowMat : snowMat, worldRoot, false).transform;
                snowBits[i] = s;
            }
        }

        void CreateMountain(string name, Vector3 pos, float width, float height, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(worldRoot, false); go.transform.localPosition = pos;
            var mesh = new Mesh { name = name + "Mesh" };
            float w = width * .5f;
            mesh.vertices = new[]
            {
                new Vector3(-w, 0, -w*.28f), new Vector3(w, 0, -w*.28f), new Vector3(0, height, 0),
                new Vector3(w, 0, -w*.28f), new Vector3(w*.8f, 0, w*.5f), new Vector3(0, height, 0),
                new Vector3(w*.8f, 0, w*.5f), new Vector3(-w*.8f, 0, w*.5f), new Vector3(0, height, 0),
                new Vector3(-w*.8f, 0, w*.5f), new Vector3(-w, 0, -w*.28f), new Vector3(0, height, 0)
            };
            mesh.triangles = new[] { 0,1,2, 3,4,5, 6,7,8, 9,10,11 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        LineRenderer CreateCable(string name, Material mat, Vector3 a, Vector3 b, int points)
        {
            var go = new GameObject(name); go.transform.SetParent(worldRoot, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat; lr.positionCount = points; lr.startWidth = lr.endWidth = .075f;
            lr.numCapVertices = 4; lr.numCornerVertices = 2; lr.useWorldSpace = false;
            for (int i = 0; i < points; i++)
            {
                float t = (float)i / (points - 1);
                Vector3 p = Vector3.Lerp(a, b, t); p.y -= Mathf.Sin(t * Mathf.PI) * .55f;
                lr.SetPosition(i, p);
            }
            return lr;
        }

        void CreateTrain()
        {
            // 다리를 건너는 미니어처 열차 대신 노선반 안에서 움직이는 펀칭 티켓
            // 셔틀이다. 정답 사건은 횡단이 아니라 릴레이 개통으로 읽힌다.
            trainRoot = new GameObject("PunchTicketShuttle").transform; trainRoot.SetParent(worldRoot, false);
            trainCab = MgfLook.Block("TicketCarrier", Vector3.zero, new Vector3(1.75f, .82f, .34f), .22f, cobaltMat, trainRoot).transform;
            Destroy(trainCab.GetComponent<Collider>());
            var nose = MgfLook.Prim(PrimitiveType.Sphere, "CarrierLamp", new Vector3(.78f, .02f, -.20f), new Vector3(.30f, .30f, .12f), iceGlowMat, trainRoot, false);
            var ticket = MgfLook.Block("PunchedTicket", new Vector3(-.24f, .05f, -.22f), new Vector3(.90f, .52f, .08f), .08f,
                snowMat, trainRoot); Destroy(ticket.GetComponent<Collider>());
            for (int i = 0; i < 3; i++)
            {
                var hole = MgfLook.Prim(PrimitiveType.Cylinder, "TicketPunch" + i, new Vector3(-.52f + i * .28f, .05f, -.28f),
                    new Vector3(.075f, .04f, .075f), rubberMat, trainRoot, false);
                hole.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            trainRoot.localPosition = new Vector3(-5.2f, 5.02f, 6.28f);
        }

        void CreateOperator()
        {
            operatorRoot = new GameObject("RelayOperatorPip").transform; operatorRoot.SetParent(worldRoot, false);
            operatorRoot.localPosition = new Vector3(-5.45f, 1.05f, 3.15f);
            var body = MgfLook.Prim(PrimitiveType.Sphere, "PipBody", Vector3.zero, new Vector3(.78f, .92f, .68f), cobaltMat, operatorRoot, false);
            var face = MgfLook.Prim(PrimitiveType.Sphere, "PipFace", new Vector3(0, .16f, -.58f), new Vector3(.58f, .48f, .16f), snowMat, operatorRoot, false);
            MgfLook.Prim(PrimitiveType.Sphere, "PipEyeL", new Vector3(-.18f, .24f, -.72f), Vector3.one * .085f, rubberMat, operatorRoot, false);
            MgfLook.Prim(PrimitiveType.Sphere, "PipEyeR", new Vector3(.18f, .24f, -.72f), Vector3.one * .085f, rubberMat, operatorRoot, false);
            MgfLook.Prim(PrimitiveType.Capsule, "PipArmL", new Vector3(-.58f, -.05f, -.05f), new Vector3(.15f, .45f, .15f), coralMat, operatorRoot, false).transform.localRotation = Quaternion.Euler(0, 0, -35);
            MgfLook.Prim(PrimitiveType.Capsule, "PipArmR", new Vector3(.58f, -.05f, -.05f), new Vector3(.15f, .45f, .15f), coralMat, operatorRoot, false).transform.localRotation = Quaternion.Euler(0, 0, 35);
            operatorAntenna = MgfLook.Prim(PrimitiveType.Cylinder, "PipAntenna", new Vector3(0, .82f, 0), new Vector3(.07f, .32f, .07f), iceGlowMat, operatorRoot, false).transform;
            MgfLook.Prim(PrimitiveType.Sphere, "PipSignal", new Vector3(0, 1.12f, 0), Vector3.one * .16f, coralMat, operatorRoot, false);
        }

        void BuildUi()
        {
            roundSprite = MakeDisc(96, false, 43f);
            ringSprite = MakeDisc(128, true, 0f);
            softSprite = MakeDisc(64, false, 30f);
            Canvas canvas = MgfText.Canvas;
            rootRt = R("RailUi", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one; rootRt.sizeDelta = Vector2.zero;
            BuildTitleUi();
            BuildHudUi();
            BuildControlsUi();
            BuildEndUi();
            BuildOverlayUi();
            LayoutUi(true);
        }

        void BuildTitleUi()
        {
            titleRt = R("TitleWorld", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one; titleRt.sizeDelta = Vector2.zero;
            titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            var artRt = R("GeneratedTitleArt", titleRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844));
            titleArt = artRt.gameObject.AddComponent<RawImage>();
            // 생성 회화 이미지를 덮지 않고 실제 인게임 신호소가 타이틀 세계가 된다.
            titleArt.texture = null;
            titleArt.color = new Color(0, 0, 0, 0);
            titleArt.raycastTarget = false;
            titleSignRt = Img(titleRt, "HangingRouteSign", new Vector2(.5f, .76f), Vector2.zero, new Vector2(338, 148), new Color(.03f, .18f, .20f, .93f)).rectTransform;
            titleTopBusRt = Img(titleRt, "SignTopBus", new Vector2(.5f, .86f), Vector2.zero, new Vector2(366, 10), Brass).rectTransform;
            titleBottomBusRt = Img(titleRt, "SignBottomBus", new Vector2(.5f, .66f), Vector2.zero, new Vector2(366, 10), Coral).rectTransform;
            titleLogo = Txt(titleRt, "빗변  철길", new Vector2(.5f, .79f), Vector2.zero, 47, WarmPaper, 350);
            titleLogo.fontStyle = FontStyles.Bold; titleLogo.characterSpacing = 5; titleLogo.outlineWidth = .23f; titleLogo.outlineColor = Indigo;
            titleSub = Txt(titleRt, "기계식 신호소의 대각선을 맞춰라", new Vector2(.5f, .71f), Vector2.zero, 17, Brass, 350);
            titleSub.characterSpacing = 2;
            titleMeta = Txt(titleRt, "중학교 2학년 · 피타고라스 정리", new Vector2(.5f, .64f), Vector2.zero, 14, new Color(WarmPaper.r, WarmPaper.g, WarmPaper.b, .82f), 330);
            int best = PlayerPrefs.GetInt("bitbyeon-cheolgil.bestCargo", 0);
            titleBest = Txt(titleRt, "최고 개통 " + best + "/9", new Vector2(.5f, .59f), Vector2.zero, 14, Ice, 260);
            titleCtaRailRt = Img(titleRt, "StartInstructionRail", new Vector2(.5f, .44f), Vector2.zero, new Vector2(326, 46), new Color(.03f, .18f, .20f, .90f)).rectTransform;
            titleCta = Txt(titleRt, "●  화면을 눌러 신호소 가동  ●", new Vector2(.5f, .44f), Vector2.zero, 18, WarmPaper, 320);
            titleCta.fontStyle = FontStyles.Bold;
        }

        void BuildHudUi()
        {
            hudRt = R("PlayHud", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            hudRt.anchorMin = Vector2.zero; hudRt.anchorMax = Vector2.one; hudRt.sizeDelta = Vector2.zero;
            hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            var top = Img(hudRt, "TopRail", new Vector2(.5f, .955f), Vector2.zero, new Vector2(372, 64), new Color(.03f, .18f, .20f, .96f));
            progressText = Txt(top.transform, "릴레이 1/9", new Vector2(.13f, .5f), Vector2.zero, 13, Snow, 90);
            pressureText = Txt(top.transform, "압력 ◆◆◆", new Vector2(.39f, .5f), Vector2.zero, 13, Coral, 126);
            cargoText = Txt(top.transform, "개통 0", new Vector2(.61f, .5f), Vector2.zero, 13, Brass, 76);
            scoreText = Txt(top.transform, "0000", new Vector2(.78f, .5f), Vector2.zero, 13, Snow, 56);

            goalText = Txt(hudRt, "크랭크를 감아 릴레이를 정확한 눈금에 잠가라", new Vector2(.5f, .875f), Vector2.zero, 16, WarmPaper, 370);
            goalText.fontStyle = FontStyles.Bold;

            problemRt = R("ProblemBoard", hudRt, new Vector2(.5f, .68f), Vector2.zero, new Vector2(360, 250));
            Img(problemRt, "MechanicalProblemPlate", new Vector2(.5f, .5f), Vector2.zero, new Vector2(360, 250), new Color(.035f, .20f, .21f, .97f));
            Img(problemRt, "ProblemHeaderRail", new Vector2(.5f, .94f), Vector2.zero, new Vector2(340, 7), Brass);
            modeText = Txt(problemRt, "빗변 릴레이", new Vector2(.5f, .87f), Vector2.zero, 15, Brass, 310);
            modeText.fontStyle = FontStyles.Bold;
            promptText = Txt(problemRt, "", new Vector2(.5f, .72f), Vector2.zero, 13, WarmPaper, 330);
            promptText.fontStyle = FontStyles.Bold;
            promptText.rectTransform.sizeDelta = new Vector2(330, 66);

            triangleRt = R("RightTriangle", problemRt, new Vector2(.5f, .33f), Vector2.zero, new Vector2(230, 105));
            lineARt = MakeLine(triangleRt, "VerticalLeg", new Vector2(-75, 0), new Vector2(-75, 76), 5, Teal);
            lineBRt = MakeLine(triangleRt, "BaseLeg", new Vector2(-75, 0), new Vector2(76, 0), 5, Teal);
            lineCRt = MakeLine(triangleRt, "Hypotenuse", new Vector2(-75, 76), new Vector2(76, 0), 6, Brass);
            MakeLine(triangleRt, "RightMarkA", new Vector2(-75, 0), new Vector2(-58, 0), 3, Coral);
            MakeLine(triangleRt, "RightMarkB", new Vector2(-58, 0), new Vector2(-58, 17), 3, Coral);
            MakeLine(triangleRt, "RightMarkC", new Vector2(-58, 17), new Vector2(-75, 17), 3, Coral);
            sideAText = Txt(triangleRt, "", new Vector2(.5f, .5f), new Vector2(-98, 36), 18, WarmPaper, 70);
            sideBText = Txt(triangleRt, "", new Vector2(.5f, .5f), new Vector2(0, -22), 18, WarmPaper, 90);
            sideCText = Txt(triangleRt, "", new Vector2(.5f, .5f), new Vector2(20, 52), 19, Brass, 100);
            areaHintText = Txt(problemRt, "", new Vector2(.5f, .10f), Vector2.zero, 17, Brass, 330);
            areaHintText.fontStyle = FontStyles.Bold;
        }

        RectTransform MakeLine(Transform parent, string name, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = b - a;
            var im = Img(parent, name, new Vector2(.5f, .5f), (a + b) * .5f, new Vector2(d.magnitude, width), color, softSprite);
            im.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            return im.rectTransform;
        }

        void BuildControlsUi()
        {
            controlsRt = R("WorldCrankControls", rootRt, new Vector2(.5f, .23f), Vector2.zero, new Vector2(286, 286));
            Img(controlsRt, "AngularConsole", new Vector2(.5f, .54f), Vector2.zero, new Vector2(278, 264), new Color(.04f, .20f, .22f, .96f));
            Img(controlsRt, "ConsoleTopRail", new Vector2(.5f, .97f), Vector2.zero, new Vector2(278, 8), Brass);
            Img(controlsRt, "ConsoleSideLeverL", new Vector2(.06f, .40f), Vector2.zero, new Vector2(13, 88), Coral);
            Img(controlsRt, "ConsoleSideLeverR", new Vector2(.94f, .40f), Vector2.zero, new Vector2(13, 88), MintEnamel);
            crankRt = R("RatchetCrankHit", controlsRt, new Vector2(.5f, .55f), Vector2.zero, new Vector2(246, 246));
            Img(crankRt, "OuterCable", new Vector2(.5f, .5f), Vector2.zero, new Vector2(246, 246), new Color(Ice.r, Ice.g, Ice.b, .30f), ringSprite);
            wheelRt = R("BlackRubberWheel", crankRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(218, 218));
            Img(wheelRt, "Wheel", new Vector2(.5f, .5f), Vector2.zero, new Vector2(218, 218), new Color(BlackRubber.r, BlackRubber.g, BlackRubber.b, .97f), roundSprite);
            Img(wheelRt, "EnamelRim", new Vector2(.5f, .5f), Vector2.zero, new Vector2(193, 193), new Color(Blue.r, Blue.g, Blue.b, .72f), ringSprite);
            for (int i = 0; i < tickMarks.Length; i++)
            {
                float a = i * Mathf.PI * 2f / tickMarks.Length + Mathf.PI * .5f;
                Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 91f;
                var mark = Img(wheelRt, "RatchetTick" + (i + 4), new Vector2(.5f, .5f), p, new Vector2(i % 5 == 0 ? 5 : 3, i % 5 == 0 ? 16 : 10), Snow, softSprite);
                mark.rectTransform.localRotation = Quaternion.Euler(0, 0, -i * 360f / tickMarks.Length);
                tickMarks[i] = mark.rectTransform;
            }
            tickText = Txt(wheelRt, "4", new Vector2(.5f, .5f), new Vector2(0, 43), 52, Snow, 130);
            tickText.fontStyle = FontStyles.Bold;
            tickUnitText = Txt(wheelRt, "m", new Vector2(.5f, .5f), new Vector2(0, 8), 14, Ice, 60);
            var crankHandle = Img(wheelRt, "CrankHandle", new Vector2(.5f, .5f), new Vector2(76, 0), new Vector2(42, 42), Coral, roundSprite);
            Img(crankHandle.transform, "HandleCap", new Vector2(.5f, .5f), Vector2.zero, new Vector2(20, 20), Snow, roundSprite);

            brakeTrackRt = R("BrakeTrack", wheelRt, new Vector2(.5f, .5f), new Vector2(0, -43), new Vector2(54, 116));
            Img(brakeTrackRt, "Track", new Vector2(.5f, .5f), Vector2.zero, new Vector2(18, 116), new Color(.02f, .05f, .09f, .95f), roundSprite);
            brakeRt = Img(brakeTrackRt, "BrakeHandle", new Vector2(.5f, .73f), Vector2.zero, new Vector2(50, 54), Coral, roundSprite).rectTransform;
            brakeText = Txt(brakeRt, "잠금", new Vector2(.5f, .5f), Vector2.zero, 13, Snow, 52);
            brakeText.fontStyle = FontStyles.Bold;

            guideArrowRt = R("GuideArrow", controlsRt, new Vector2(.5f, .55f), Vector2.zero, new Vector2(260, 260));
            guideHandRt = Img(guideArrowRt, "GuideHand", new Vector2(.5f, .5f), new Vector2(102, 0), new Vector2(38, 38), new Color(Snow.r, Snow.g, Snow.b, .90f), roundSprite).rectTransform;
            Txt(guideHandRt, "●", new Vector2(.5f, .5f), Vector2.zero, 18, Indigo, 28);
        }

        void BuildEndUi()
        {
            endRt = R("EndScreen", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            endRt.anchorMin = Vector2.zero; endRt.anchorMax = Vector2.one; endRt.sizeDelta = Vector2.zero;
            endG = endRt.gameObject.AddComponent<CanvasGroup>();
            Img(endRt, "EndAuroraGlass", new Vector2(.5f, .5f), Vector2.zero, new Vector2(365, 510), new Color(.02f, .08f, .18f, .94f), roundSprite);
            endTitle = Txt(endRt, "종착역 도착", new Vector2(.5f, .68f), Vector2.zero, 38, Snow, 350);
            endTitle.fontStyle = FontStyles.Bold; endTitle.outlineWidth = .18f; endTitle.outlineColor = Blue;
            endScore = Txt(endRt, "0000", new Vector2(.5f, .56f), Vector2.zero, 54, Ice, 320);
            endScore.fontStyle = FontStyles.Bold;
            endStats = Txt(endRt, "", new Vector2(.5f, .43f), Vector2.zero, 20, Snow, 330);
            endCta = Txt(endRt, "화면을 눌러 다시 출발", new Vector2(.5f, .27f), Vector2.zero, 18, Ice, 330);
            endCta.fontStyle = FontStyles.Bold;
        }

        void BuildOverlayUi()
        {
            toastRt = R("RefuseToast", rootRt, new Vector2(.5f, .48f), Vector2.zero, new Vector2(356, 72));
            Img(toastRt, "ToastBg", new Vector2(.5f, .5f), Vector2.zero, new Vector2(356, 72), new Color(.03f, .07f, .13f, .94f), roundSprite);
            toastText = Txt(toastRt, "", new Vector2(.5f, .5f), Vector2.zero, 16, Snow, 330);
            toastG = toastRt.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0;

            revealRt = R("MathReveal", rootRt, new Vector2(.5f, .52f), Vector2.zero, new Vector2(365, 160));
            Img(revealRt, "RevealBg", new Vector2(.5f, .5f), Vector2.zero, new Vector2(365, 160), new Color(.03f, .14f, .20f, .97f), roundSprite);
            revealTitle = Txt(revealRt, "교각 결빙 완료", new Vector2(.5f, .72f), Vector2.zero, 19, Ice, 330);
            revealTitle.fontStyle = FontStyles.Bold;
            revealText = Txt(revealRt, "", new Vector2(.5f, .40f), Vector2.zero, 19, Snow, 340);
            revealG = revealRt.gameObject.AddComponent<CanvasGroup>(); revealG.alpha = 0;

            rippleRt = Img(rootRt, "InputRipple", new Vector2(.5f, .5f), Vector2.zero, new Vector2(54, 54), new Color(Ice.r, Ice.g, Ice.b, 0), ringSprite).rectTransform;
            snowWipeRt = R("SnowWipe", rootRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            snowWipeRt.anchorMin = Vector2.zero; snowWipeRt.anchorMax = Vector2.one; snowWipeRt.sizeDelta = Vector2.zero;
            for (int i = 0; i < wipeFlakes.Length; i++)
            {
                var flake = Img(snowWipeRt, "WipeShard" + i, new Vector2(0, .5f), new Vector2(-50, (i - 9) * 52), new Vector2(12 + i % 4 * 5, 4 + i % 3 * 3), Snow, softSprite);
                flake.color = new Color(Snow.r, Snow.g, Snow.b, 0); wipeFlakes[i] = flake.rectTransform;
            }
        }

        void LayoutUi(bool force)
        {
            if (!force && layoutW == Screen.width && layoutH == Screen.height) return;
            layoutW = Screen.width; layoutH = Screen.height;
            bool land = Screen.width >= 900 && Screen.width > Screen.height;
            if (land)
            {
                problemRt.anchorMin = problemRt.anchorMax = new Vector2(.28f, .53f);
                problemRt.sizeDelta = new Vector2(430, 276);
                promptText.rectTransform.sizeDelta = new Vector2(390, 66);
                controlsRt.anchorMin = controlsRt.anchorMax = new Vector2(.76f, .48f);
                controlsRt.sizeDelta = new Vector2(300, 300);
                goalText.rectTransform.anchorMin = goalText.rectTransform.anchorMax = new Vector2(.5f, .87f);
                titleSignRt.anchorMin = titleSignRt.anchorMax = new Vector2(.31f, .76f);
                titleTopBusRt.anchorMin = titleTopBusRt.anchorMax = new Vector2(.31f, .86f);
                titleBottomBusRt.anchorMin = titleBottomBusRt.anchorMax = new Vector2(.31f, .66f);
                titleLogo.rectTransform.anchorMin = titleLogo.rectTransform.anchorMax = new Vector2(.31f, .79f);
                titleSub.rectTransform.anchorMin = titleSub.rectTransform.anchorMax = new Vector2(.31f, .71f);
                titleMeta.rectTransform.anchorMin = titleMeta.rectTransform.anchorMax = new Vector2(.31f, .64f);
                titleBest.rectTransform.anchorMin = titleBest.rectTransform.anchorMax = new Vector2(.31f, .59f);
                titleCtaRailRt.anchorMin = titleCtaRailRt.anchorMax = new Vector2(.31f, .43f);
                titleCta.rectTransform.anchorMin = titleCta.rectTransform.anchorMax = new Vector2(.31f, .43f);
                titleArt.rectTransform.sizeDelta = new Vector2(1280, 900);
            }
            else
            {
                problemRt.anchorMin = problemRt.anchorMax = new Vector2(.5f, .68f);
                problemRt.sizeDelta = new Vector2(360, 250);
                promptText.rectTransform.sizeDelta = new Vector2(330, 66);
                controlsRt.anchorMin = controlsRt.anchorMax = new Vector2(.5f, .23f);
                controlsRt.sizeDelta = new Vector2(260, 270);
                goalText.rectTransform.anchorMin = goalText.rectTransform.anchorMax = new Vector2(.5f, .875f);
                titleSignRt.anchorMin = titleSignRt.anchorMax = new Vector2(.5f, .76f);
                titleTopBusRt.anchorMin = titleTopBusRt.anchorMax = new Vector2(.5f, .86f);
                titleBottomBusRt.anchorMin = titleBottomBusRt.anchorMax = new Vector2(.5f, .66f);
                titleLogo.rectTransform.anchorMin = titleLogo.rectTransform.anchorMax = new Vector2(.5f, .79f);
                titleSub.rectTransform.anchorMin = titleSub.rectTransform.anchorMax = new Vector2(.5f, .71f);
                titleMeta.rectTransform.anchorMin = titleMeta.rectTransform.anchorMax = new Vector2(.5f, .64f);
                titleBest.rectTransform.anchorMin = titleBest.rectTransform.anchorMax = new Vector2(.5f, .59f);
                titleCtaRailRt.anchorMin = titleCtaRailRt.anchorMax = new Vector2(.5f, .44f);
                titleCta.rectTransform.anchorMin = titleCta.rectTransform.anchorMax = new Vector2(.5f, .44f);
                titleArt.rectTransform.sizeDelta = new Vector2(390, 844);
            }
        }

        void SetScreen()
        {
            bool title = phase == Phase.Title, end = phase == Phase.End;
            titleG.alpha = title ? 1 : 0; titleRt.gameObject.SetActive(title);
            endG.alpha = end ? 1 : 0; endRt.gameObject.SetActive(end);
            hudG.alpha = (!title && !end) ? 1 : 0; hudRt.gameObject.SetActive(!title && !end);
            controlsRt.gameObject.SetActive(!end);
            if (title)
            {
                tickText.text = "4"; brakeRt.gameObject.SetActive(false);
                guideHandRt.gameObject.SetActive(true);
            }
            else brakeRt.gameObject.SetActive(true);
            toastG.alpha = 0; revealG.alpha = 0;
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            modeText.text = current.mode == RailMode.Hypotenuse ? "빗변 릴레이를 구하시오" : "나머지 한 변을 구하시오";
            // 학생 화면과 ProblemBankJson이 같은 RailProblem.prompt를 직접 사용한다.
            promptText.text = current.prompt;
            if (current.mode == RailMode.Hypotenuse)
            {
                sideAText.text = current.a + " m"; sideBText.text = current.b + " m"; sideCText.text = "? m";
                areaHintText.text = roundIndex < 2 ? current.a + "<sup>2</sup> + " + current.b + "<sup>2</sup> = □<sup>2</sup>" : "a<sup>2</sup> + b<sup>2</sup> = c<sup>2</sup>";
            }
            else
            {
                int missingIsA = current.knownLeg == current.b ? 1 : 0;
                sideAText.text = missingIsA == 1 ? "? m" : current.knownLeg + " m";
                sideBText.text = missingIsA == 1 ? current.knownLeg + " m" : "? m";
                sideCText.text = current.c + " m";
                areaHintText.text = current.c + "<sup>2</sup> − " + current.knownLeg + "<sup>2</sup> = □<sup>2</sup>";
            }
            tickText.text = currentTick.ToString();
            progressText.text = "릴레이 " + (roundIndex + 1) + "/9";
            pressureText.text = "압력 " + new string('◆', Mathf.Max(0, st.lives)) + new string('◇', Mathf.Max(0, 3 - st.lives));
            cargoText.text = "개통 " + st.cargo;
            goalText.text = st.onboarding && !rotatedThisRound ? "크랭크를 원을 따라 돌려 눈금을 바꾸시오" :
                rotatedThisRound ? "중앙 브레이크를 아래로 쓸어 릴레이를 잠그시오" : "대각선 길이를 계산하고 크랭크를 돌리시오";
            SetCrankToTick(currentTick);
        }

        void UpdateWorld(float dt)
        {
            worldClock += dt;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            cam.orthographicSize = aspect < .75f ? 7.35f * .75f / aspect : 7.35f;
            if (phase != Phase.Feedback) cam.transform.position = cameraHome;
            auroraRoot.localPosition = new Vector3(Mathf.Sin(worldClock * .11f) * .55f, Mathf.Sin(worldClock * .17f) * .12f, 0);
            auroraRoot.localRotation = Quaternion.Euler(0, Mathf.Sin(worldClock * .09f) * 2.5f, 0);
            for (int i = 0; i < snowBits.Length; i++)
            {
                Vector3 p = snowBits[i].localPosition;
                p.y += dt * (.10f + i % 5 * .025f); p.x += dt * (.025f + i % 3 * .018f);
                if (p.y > 6.4f) { p.y = snowSeeds[i].y; p.x = snowSeeds[i].x; }
                snowBits[i].localPosition = p;
            }

            if (operatorRoot)
            {
                float hop = phase == Phase.Feedback && feedbackCorrect ? Mathf.Sin(Mathf.Clamp01(feedbackTime / 1.2f) * Mathf.PI) * .72f : 0f;
                operatorRoot.localPosition = new Vector3(-5.45f, 1.05f + Mathf.Sin(worldClock * 2.2f) * .055f + hop, 3.15f);
                float lean = phase == Phase.Feedback && !feedbackCorrect ? -12f : Mathf.Sin(worldClock * 1.6f) * 3f;
                operatorRoot.localRotation = Quaternion.Euler(0, phase == Phase.Feedback && feedbackCorrect ? feedbackTime * 210f : 0, lean);
                operatorAntenna.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 4.5f) * .08f);
            }

            if (phase == Phase.Title)
            {
                float loop = Mathf.Repeat(worldClock * 1.15f, 10.4f) - 5.2f;
                trainRoot.localPosition = new Vector3(loop, 5.02f + Mathf.Sin(worldClock * 2f) * .05f, 6.28f);
            }
            else if (phase == Phase.Feedback && feedbackCorrect)
            {
                float t = Mathf.Clamp01((feedbackTime - .08f) / 1.25f);
                for (int i = 0; i < bridgeSegments.Length; i++)
                {
                    float k = Mathf.Clamp01(t * 1.55f - i * .045f);
                    float e = 1f - Mathf.Pow(1f - k, 3f);
                    bridgeSegments[i].localScale = new Vector3(1, Mathf.Lerp(.28f, 1f, e), 1);
                    bridgeSegments[i].GetComponent<Renderer>().sharedMaterial = k > .65f ? iceGlowMat : iceMat;
                }
                float trainT = Mathf.Clamp01((t - .35f) / .65f);
                float overshoot = Mathf.Sin(trainT * Mathf.PI) * .22f;
                trainRoot.localPosition = new Vector3(Mathf.Lerp(-5.2f, 5.2f, trainT), 5.02f + overshoot, 6.28f);
            }
            else if (phase != Phase.Title)
            {
                trainRoot.localPosition = new Vector3(-5.2f + st.bridgeIndex * .32f, 5.02f, 6.28f);
            }
        }

        void UpdateUi(float dt)
        {
            LayoutUi(false);
            titlePulse += dt;
            if (phase == Phase.Title)
            {
                titleLogo.color = Color.Lerp(Snow, Ice, .5f + .5f * Mathf.Sin(titlePulse * 1.4f));
                titleArt.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(titlePulse * .35f) * .018f);
                titleCta.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(titlePulse * 2.2f) * .035f);
            }

            if (lastScore != st.score) lastScore = st.score;
            displayedScore = Mathf.Lerp(displayedScore, st.score, 1f - Mathf.Exp(-dt * 7f));
            if (scoreText) scoreText.text = Mathf.RoundToInt(displayedScore).ToString("0000");
            if (tickText) tickText.text = currentTick.ToString();
            if (pressureText && phase != Phase.Title) pressureText.text = "압력 " + new string('◆', Mathf.Max(0, st.lives)) + new string('◇', Mathf.Max(0, 3 - st.lives));
            if (cargoText && phase != Phase.Title) cargoText.text = "개통 " + st.cargo;

            if (toastTime > 0)
            {
                toastTime -= dt; toastG.alpha = Mathf.Clamp01(toastTime * 3f);
                toastRt.anchoredPosition = new Vector2(Mathf.Sin(toastTime * 45f) * Mathf.Min(6f, toastTime * 8f), 0);
            }
            else toastG.alpha = 0;
            if (rippleTime > 0)
            {
                rippleTime -= dt; float k = 1f - rippleTime / .38f;
                rippleRt.localScale = Vector3.one * Mathf.Lerp(.3f, 1.9f, k);
                rippleRt.GetComponent<Image>().color = new Color(Ice.r, Ice.g, Ice.b, (1f - k) * .55f);
            }
            else rippleRt.GetComponent<Image>().color = new Color(Ice.r, Ice.g, Ice.b, 0);

            UpdateGuide();
            UpdateSnowWipe();
            if (phase == Phase.End)
            {
                endDisplayedScore = Mathf.Lerp(endDisplayedScore, st.score, 1f - Mathf.Exp(-dt * 6f));
                endScore.text = Mathf.RoundToInt(endDisplayedScore).ToString("0000");
            }
        }

        void UpdateGuide()
        {
            bool show = phase == Phase.Title || phase == Phase.Practice || guideBoost > 0f;
            guideHandRt.gameObject.SetActive(show);
            if (!show) return;
            if (guideBoost > 0) guideBoost -= Time.deltaTime;
            if (rotatedThisRound && phase != Phase.Title)
            {
                Vector2 c = ToRoot(brakeRt.position);
                float t = Mathf.Repeat(Time.unscaledTime * .9f, 1f);
                guideHandRt.anchoredPosition = c + new Vector2(0, Mathf.Lerp(28f, -45f, t * t * (3 - 2 * t)));
                guideHandRt.localScale = Vector3.one * (guideBoost > 0 ? 1.18f : .9f);
            }
            else
            {
                float a = Time.unscaledTime * 2.1f;
                guideHandRt.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 101f;
                guideHandRt.localScale = Vector3.one * (guideBoost > 0 ? 1.24f : .9f);
            }
        }

        void UpdateSnowWipe()
        {
            bool active = phase == Phase.Feedback && feedbackCorrect && feedbackTime > .55f;
            float t = Mathf.Clamp01((feedbackTime - .55f) / .72f);
            for (int i = 0; i < wipeFlakes.Length; i++)
            {
                Image im = wipeFlakes[i].GetComponent<Image>();
                if (!active) { im.color = new Color(Snow.r, Snow.g, Snow.b, 0); continue; }
                float stagger = Mathf.Clamp01(t * 1.35f - i * .018f);
                wipeFlakes[i].anchorMin = wipeFlakes[i].anchorMax = new Vector2(0, .5f);
                wipeFlakes[i].anchoredPosition = new Vector2(Mathf.Lerp(-35f, rootRt.rect.width + 45f, stagger), (i - 9) * 48f + Mathf.Sin(i * 2.1f) * 18f);
                wipeFlakes[i].localRotation = Quaternion.Euler(0, 0, stagger * 240f + i * 17f);
                im.color = new Color(Snow.r, Snow.g, Snow.b, Mathf.Sin(stagger * Mathf.PI) * .88f);
            }
        }

        Vector2 ToRoot(Vector3 world)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, screen, null, out Vector2 local);
            return local;
        }

        bool HitCrank(Vector2 p)
        {
            Vector2 c = RectTransformUtility.WorldToScreenPoint(null, crankRt.position);
            float scale = MgfText.Canvas.scaleFactor;
            float d = Vector2.Distance(p, c) / Mathf.Max(.01f, scale);
            return d >= 42f && d <= 145f;
        }

        bool HitBrake(Vector2 p)
        {
            Vector2 c = RectTransformUtility.WorldToScreenPoint(null, brakeRt.position);
            float scale = MgfText.Canvas.scaleFactor;
            return Mathf.Abs(p.x - c.x) <= 38f * scale && Mathf.Abs(p.y - c.y) <= 50f * scale;
        }

        float PointerAngle(Vector2 p)
        {
            Vector2 c = RectTransformUtility.WorldToScreenPoint(null, crankRt.position);
            return Mathf.Atan2(p.y - c.y, p.x - c.x) * Mathf.Rad2Deg;
        }

        void RotateWheel(float d)
        {
            wheelVisualAngle += d; wheelRt.localRotation = Quaternion.Euler(0, 0, wheelVisualAngle);
            tickText.rectTransform.localRotation = Quaternion.Euler(0, 0, -wheelVisualAngle);
            tickUnitText.rectTransform.localRotation = Quaternion.Euler(0, 0, -wheelVisualAngle);
            brakeTrackRt.localRotation = Quaternion.Euler(0, 0, -wheelVisualAngle);
        }

        void SetCrankToTick(int tick)
        {
            wheelVisualAngle = (tick - BitbyeonRules.MinTick) * 360f / 22f;
            wheelRt.localRotation = Quaternion.Euler(0, 0, wheelVisualAngle);
            tickText.rectTransform.localRotation = Quaternion.Euler(0, 0, -wheelVisualAngle);
            tickUnitText.rectTransform.localRotation = Quaternion.Euler(0, 0, -wheelVisualAngle);
            brakeTrackRt.localRotation = Quaternion.Euler(0, 0, -wheelVisualAngle);
        }

        void GrabCrank(bool held)
        {
            crankRt.localScale = held ? new Vector3(1.04f, .97f, 1f) : Vector3.one;
        }

        void GrabBrake(bool held)
        {
            brakeRt.localScale = held ? new Vector3(1.12f, .88f, 1f) : Vector3.one;
        }

        void MoveBrake(float localY)
        {
            brakeRt.anchoredPosition = new Vector2(0, Mathf.Clamp(localY, -62f, 8f));
            brakeRt.localScale = new Vector3(1f + Mathf.Abs(localY) * .0015f, 1f - Mathf.Abs(localY) * .0012f, 1f);
        }

        void BounceBrake()
        {
            MgfFx.Punch(brakeRt, .12f, .24f);
        }

        void RatchetPulse(int dir)
        {
            tickText.color = Ice;
            MgfFx.Punch(tickText.rectTransform, .10f, .12f);
            cableMat = MgfLook.Unlit(Snow);
            mainCable.sharedMaterial = cableMat;
        }

        void SpawnRipple(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, screen, null, out Vector2 local);
            rippleRt.anchoredPosition = local; rippleRt.localScale = Vector3.one * .3f; rippleTime = .38f;
        }

        void Refuse(string msg)
        {
            toastText.text = msg; toastTime = 1.45f; toastG.alpha = 1;
            MgfSfx.Play("wrong", .20f);
            MgfFx.Punch(rotatedThisRound ? brakeRt : crankRt, .07f, .22f);
        }

        void StartGuide() { guideBoost = 1.2f; }
        void BoostGuide() { guideBoost = 1.4f; MgfFx.Punch(guideHandRt, .22f, .32f); }

        void ResetWorldForRun()
        {
            safetyCable.gameObject.SetActive(false); safetyHook.gameObject.SetActive(false);
            for (int i = 0; i < bridgeSegments.Length; i++)
            {
                bridgeSegments[i].localScale = Vector3.one;
                bridgeSegments[i].GetComponent<Renderer>().sharedMaterial = iceMat;
            }
            trainRoot.localPosition = new Vector3(-5.2f, 5.02f, 6.28f);
            displayedScore = 0; endDisplayedScore = 0;
        }

        void ResetWorldForProblem()
        {
            safetyCable.gameObject.SetActive(false); safetyHook.gameObject.SetActive(false);
            for (int i = 0; i < bridgeSegments.Length; i++)
            {
                bridgeSegments[i].localScale = new Vector3(1, .28f, 1);
                bridgeSegments[i].GetComponent<Renderer>().sharedMaterial = iceMat;
            }
            mainCable.sharedMaterial = cableMat; wheelVisualAngle = 0f; SetCrankToTick(BitbyeonRules.MinTick);
        }

        void BeginFeedbackVisual(bool correct, bool wasFirst, int tick, bool timeout)
        {
            revealTime = correct ? 1.55f : 1.28f; revealG.alpha = 1;
            goalText.text = correct ? "릴레이 연쇄 잠금 — 신호 개통" : "안전 브레이크 작동 — 제곱 관계를 확인하시오";
            if (correct)
            {
                revealTitle.text = wasFirst ? "첫 신호 개통" : "릴레이 재잠금 성공";
                revealTitle.color = Ice; revealText.color = Snow; revealText.text = current.reveal;
                MgfSfx.Play("correct", .50f); MgfSfx.Play("whoosh", .30f);
                MgfFx.Punch(crankRt, .16f + st.streak * .025f, .34f);
            }
            else
            {
                revealTitle.text = timeout ? "시간 초과 · 안전 정지" : "눈금 불일치 · 안전 정지";
                revealTitle.color = Coral; revealText.color = Snow;
                revealText.text = WrongMessage(st.misconceptionId, tick) + "\n<color=#BFEAF5>" + current.reveal + "</color>";
                safetyCable.gameObject.SetActive(true); safetyHook.gameObject.SetActive(true);
                mainCable.sharedMaterial = cableRedMat;
                MgfSfx.Play("wrong", .46f);
                MgfFx.Shake(cam, .07f + Mathf.Max(0, 3 - st.lives) * .025f, .24f);
            }
        }

        string WrongMessage(string id, int tick)
        {
            if (id == "sum-without-squares") return tick + " m는 두 변을 그대로 더한 값입니다. 제곱의 합을 쓰시오.";
            if (id == "sum-instead-of-difference") return "빗변이 주어졌으므로 제곱의 차를 쓰시오.";
            if (id == "approximate-square-gap") return "제곱 차가 작아도 같은 값은 아닙니다.";
            if (id == "hypotenuse-not-longest") return "빗변은 직각의 맞은편이며 가장 긴 변입니다.";
            if (id == "no-input-timeout") return "케이블을 잠그지 못했습니다. 정수 눈금을 계산하시오.";
            return tick < current.answer ? "케이블이 짧습니다. 제곱 관계를 다시 보시오." : "케이블이 깁니다. 제곱 관계를 다시 보시오.";
        }

        void CompleteFeedbackVisual(bool correct)
        {
            revealG.alpha = 0; revealTime = 0;
            if (!correct) { safetyCable.gameObject.SetActive(false); safetyHook.gameObject.SetActive(false); mainCable.sharedMaterial = cableMat; }
        }

        void ShowEnd(string reason, int best)
        {
            bool clear = reason == "clear";
            endTitle.text = clear ? (st.cargo == 9 ? "9개 노선 완전 개통" : "신호 점검 종료") : "압력 실린더 정지";
            endTitle.color = clear ? Ice : Coral;
            endStats.text = "점검 릴레이 " + (roundIndex + (clear ? 1 : 0)) + "/9\n개통 신호 " + st.cargo + "/9\n첫 시도 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal + "\n최고 개통 " + best + "/9";
            endDisplayedScore = 0;
            endCta.text = clear ? "화면을 눌러 완편성에 재도전" : "화면을 눌러 압력을 복구하고 재출발";
        }
    }
}
