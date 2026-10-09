// 구슬 교대 — 레몬 정비 트럭 2.5D 디오라마, UI, 실제 드래그, 피드백 연출.
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.GuseulGyodae
{
    public partial class GuseulGyodaeGame
    {
        Camera cam;
        Transform worldRoot, truckRoot, supplyRoot, cartridgeRoot, beltRoot, beetleRoot, latchRoot;
        Transform blueSupply, whiteSupply, dispatchHandle;
        readonly Transform[] slots = new Transform[6];
        readonly Renderer[] marbleRenderers = new Renderer[6];
        readonly Renderer[] marbleDots = new Renderer[6];
        readonly Transform[] rollers = new Transform[7];
        readonly Transform[] beetleLegs = new Transform[6];
        readonly Transform[] binMarbles = new Transform[8];
        readonly Transform[] bolts = new Transform[3];
        readonly GameObject[] ripples = new GameObject[8];
        readonly float[] rippleLife = new float[8];
        int rippleCursor;

        Material lemonMat, lemonDarkMat, cobaltMat, pearlMat, graphiteMat, chromeMat, glassMat;
        Material beltMat, signalMat, clearBlueMat, clearWhiteMat;
        GameObject marbleGhost, guideFinger, guideRing, caseGlow;
        LineRenderer dragTrail, guideTrail;
        Vector3 cartridgeHome, handleHome;
        float ambientClock, displayScore, toastLeft, guideLeft, titlePulse;
        bool guideDispatch, currentLand;
        int lastScreenW, lastScreenH, lastTimer = -1;
        string toastMessage = "";

        Canvas canvas;
        GameObject playUi, titleUi, endUi, toastPanel, receiptPanel, targetRoot, orderCard;
        RawImage titleArt;
        Image titleHandle, targetLine, orderCardImage, toastImage, receiptImage, endPanelImage;
        Image receiptMadeLine, receiptTargetLine;
        TextMeshProUGUI logoUi, titleTagUi, titleMetaUi, titleStartUi;
        TextMeshProUGUI hudUi, orderUi, promptUi, goalUi, countUi, targetNumUi, targetDenUi;
        TextMeshProUGUI toastUi, receiptUi, receiptNoteUi, receiptMadeNum, receiptMadeDen, receiptTargetNum, receiptTargetDen, receiptSignUi;
        TextMeshProUGUI endTitleUi, endStatsUi, endHintUi;
        RectTransform restartRect;

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("FFF3A8"), MgfLook.Hex("F8D95D"), MgfLook.Hex("D3B83F"), .9f);
            MgfLook.Sun(new Vector3(48f, -34f, -18f), MgfLook.Hex("FFF6D2"), 1.42f, .68f);
            cam = MgfLook.Camera(new Vector3(0f, 9.6f, -17.5f), new Vector3(0f, .5f, 1.2f), 38f);
            cam.orthographic = true;
            cam.orthographicSize = 7.2f;
            cam.backgroundColor = MgfLook.Hex("F1D34F");

            lemonMat = MgfLook.Lit(MgfLook.Hex("F1D34F"), .55f, .08f);
            lemonDarkMat = MgfLook.Lit(MgfLook.Hex("D5A91E"), .32f, .10f);
            cobaltMat = MgfLook.Lit(MgfLook.Hex("315CC8"), .84f, .12f, MgfLook.Hex("173D91") * .08f);
            pearlMat = MgfLook.Lit(MgfLook.Hex("F7F7EC"), .88f, .08f);
            graphiteMat = MgfLook.Lit(MgfLook.Hex("323B45"), .18f, .24f);
            chromeMat = MgfLook.Lit(MgfLook.Hex("C2D4DE"), .82f, .72f);
            beltMat = MgfLook.Lit(MgfLook.Hex("64717A"), .20f, .12f);
            signalMat = MgfLook.Lit(MgfLook.Hex("FFE98A"), .72f, .04f, MgfLook.Hex("F1D34F") * .38f);
            glassMat = MgfLook.Alpha(new Color(.78f, .90f, .95f, .25f), MgfLook.SoftDot);
            clearBlueMat = MgfLook.Alpha(new Color(.16f, .36f, .82f, .28f), MgfLook.SoftDot);
            clearWhiteMat = MgfLook.Alpha(new Color(.95f, .95f, .88f, .32f), MgfLook.SoftDot);

            worldRoot = new GameObject("LemonTruckWorld").transform;
            truckRoot = new GameObject("OpenServiceTruck").transform;
            truckRoot.SetParent(worldRoot, false);

            // 화면 대부분을 실제 레몬 트럭 외판이 차지한다. 별도 회화 배경은 없다.
            MgfLook.Block("TruckBackWall", new Vector3(0f, .15f, 3.6f), new Vector3(17.5f, 8.2f, .72f), .48f, lemonMat, truckRoot);
            MgfLook.Block("TruckRoof", new Vector3(0f, 4.35f, 1.0f), new Vector3(18.2f, .65f, 6.4f), .30f, lemonMat, truckRoot);
            MgfLook.Block("TruckFloor", new Vector3(0f, -3.30f, .7f), new Vector3(18.2f, .76f, 6.6f), .28f, lemonDarkMat, truckRoot);
            MgfLook.Block("ChromeTopRail", new Vector3(0f, 4.01f, .55f), new Vector3(17.4f, .17f, .20f), .07f, chromeMat, truckRoot).GetComponent<Collider>().enabled = false;
            MgfLook.Block("ChromeFloorRail", new Vector3(0f, -2.91f, .30f), new Vector3(17.4f, .15f, .22f), .06f, chromeMat, truckRoot).GetComponent<Collider>().enabled = false;
            for (int i = 0; i < 12; i++)
            {
                float x = -8.0f + i * 1.45f;
                var rivet = MgfLook.Prim(PrimitiveType.Sphere, "PanelRivet" + i, new Vector3(x, 3.7f, .05f), new Vector3(.16f, .16f, .10f), chromeMat, truckRoot, false);
                rivet.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            BuildWheel(-7.05f);
            BuildWheel(7.05f);
            BuildSupplies();
            BuildCartridge();
            BuildBelt();
            BuildBeetle();
            BuildGuideObjects();
        }

        void BuildWheel(float x)
        {
            var root = new GameObject("TruckWheel").transform;
            root.SetParent(truckRoot, false);
            root.localPosition = new Vector3(x, -3.25f, 1.15f);
            var tire = MgfLook.Prim(PrimitiveType.Cylinder, "RubberTire", Vector3.zero, new Vector3(1.55f, .48f, 1.55f), graphiteMat, root, false);
            tire.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var hub = MgfLook.Prim(PrimitiveType.Cylinder, "ChromeHub", new Vector3(0, 0, -.30f), new Vector3(.82f, .52f, .82f), chromeMat, root, false);
            hub.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        void BuildSupplies()
        {
            var supplies = new GameObject("SupplyBins").transform;
            supplyRoot = supplies;
            supplies.SetParent(truckRoot, false);
            supplies.localPosition = new Vector3(-5.65f, -.15f, -.05f);
            MgfLook.Block("SupplyCabinet", new Vector3(0f, .2f, .55f), new Vector3(3.55f, 5.15f, 2.0f), .34f, lemonDarkMat, supplies);
            MgfLook.Block("BlueBin", new Vector3(0f, 1.55f, -.38f), new Vector3(3.05f, 1.85f, 1.28f), .28f, cobaltMat, supplies);
            MgfLook.Block("WhiteBin", new Vector3(0f, -1.05f, -.38f), new Vector3(3.05f, 1.85f, 1.28f), .28f, pearlMat, supplies);
            blueSupply = MakeControlMarble("SupplyBlue", new Vector3(0f, 1.55f, -1.18f), true, supplies);
            whiteSupply = MakeControlMarble("SupplyWhite", new Vector3(0f, -1.05f, -1.18f), false, supplies);
            for (int i = 0; i < binMarbles.Length; i++)
            {
                bool blue = i < 4;
                float x = (i % 2 == 0 ? -.73f : .73f) + ((i / 2) % 2) * .16f;
                float y = blue ? 1.95f - (i / 2) * .62f : -.64f - ((i - 4) / 2) * .62f;
                var b = MgfLook.Prim(PrimitiveType.Sphere, "BinMarble" + i, new Vector3(x, y, -.82f), new Vector3(.56f, .56f, .56f), blue ? cobaltMat : pearlMat, supplies, false);
                binMarbles[i] = b.transform;
            }
            var blueLabel = MgfText.World("파랑 공급", new Vector3(0f, 2.74f, -1.08f), 2.5f, Color.white, supplies);
            var whiteLabel = MgfText.World("흰색 공급", new Vector3(0f, .24f, -1.08f), 2.5f, MgfLook.Hex("323B45"), supplies);
            blueLabel.outlineWidth = .14f; blueLabel.outlineColor = MgfLook.Hex("173D91");
            whiteLabel.outlineWidth = .12f; whiteLabel.outlineColor = MgfLook.Hex("F7F7EC");
            blueLabel.transform.rotation = cam.transform.rotation;
            whiteLabel.transform.rotation = cam.transform.rotation;
        }

        Transform MakeControlMarble(string name, Vector3 pos, bool blue, Transform parent)
        {
            var sphere = MgfLook.Prim(PrimitiveType.Sphere, name, pos, Vector3.one * 1.10f, blue ? cobaltMat : pearlMat, parent, true);
            sphere.GetComponent<SphereCollider>().radius = 1.02f;
            var dot = MgfLook.Prim(PrimitiveType.Sphere, name + "Mark", new Vector3(0, 0, -.52f), new Vector3(.30f, .30f, .12f), blue ? pearlMat : cobaltMat, sphere.transform, false);
            dot.transform.localRotation = Quaternion.Euler(0, 0, 18);
            return sphere.transform;
        }

        void BuildCartridge()
        {
            cartridgeRoot = new GameObject("TransparentCartridge").transform;
            cartridgeRoot.SetParent(truckRoot, false);
            cartridgeRoot.localPosition = new Vector3(0f, .25f, -.48f);
            cartridgeHome = cartridgeRoot.localPosition;
            MgfLook.Block("CartridgeBack", new Vector3(0f, .1f, .55f), new Vector3(5.20f, 4.85f, .58f), .34f, graphiteMat, cartridgeRoot);
            MgfLook.Block("GlassFront", new Vector3(0f, .1f, -1.05f), new Vector3(5.45f, 5.10f, .20f), .34f, glassMat, cartridgeRoot).GetComponent<Collider>().enabled = false;
            for (int r = 0; r < 2; r++)
            for (int c = 0; c < 3; c++)
            {
                int i = r * 3 + c;
                float x = -1.55f + c * 1.55f;
                float y = 1.45f - r * 2.15f;
                var socket = MgfLook.Prim(PrimitiveType.Cylinder, "Socket" + (i + 1), new Vector3(x, y, -.66f), new Vector3(.77f, .20f, .77f), chromeMat, cartridgeRoot, false);
                socket.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var marble = MgfLook.Prim(PrimitiveType.Sphere, "Slot" + i, new Vector3(x, y, -1.18f), Vector3.one * 1.28f, cobaltMat, cartridgeRoot, true);
                marble.GetComponent<SphereCollider>().radius = .78f;
                slots[i] = marble.transform;
                marbleRenderers[i] = marble.GetComponent<Renderer>();
                var dot = MgfLook.Prim(PrimitiveType.Sphere, "SlotMark" + i, new Vector3(.25f, .15f, -.50f), new Vector3(.32f, .32f, .11f), pearlMat, marble.transform, false);
                marbleDots[i] = dot.GetComponent<Renderer>();
                var plate = MgfLook.Block("SlotNumberPlate" + i, new Vector3(x, y - .82f, -1.18f), new Vector3(.60f, .32f, .12f), .09f, lemonMat, cartridgeRoot);
                plate.GetComponent<Collider>().enabled = false;
                var t = MgfText.World((i + 1).ToString(), new Vector3(0, 0, -.08f), 3.0f, MgfLook.Hex("323B45"), plate.transform);
                t.transform.rotation = cam.transform.rotation;
            }
            // 크롬 테두리와 실제 출고 손잡이.
            MgfLook.Block("FrameTop", new Vector3(0f, 2.61f, -1.03f), new Vector3(5.75f, .28f, .34f), .12f, chromeMat, cartridgeRoot);
            MgfLook.Block("FrameBottom", new Vector3(0f, -2.41f, -1.03f), new Vector3(5.75f, .28f, .34f), .12f, chromeMat, cartridgeRoot);
            MgfLook.Block("FrameLeft", new Vector3(-2.73f, .10f, -1.03f), new Vector3(.28f, 5.05f, .34f), .12f, chromeMat, cartridgeRoot);
            MgfLook.Block("FrameRight", new Vector3(2.73f, .10f, -1.03f), new Vector3(.28f, 5.05f, .34f), .12f, chromeMat, cartridgeRoot);
            dispatchHandle = MgfLook.Block("DispatchHandle", new Vector3(3.38f, .1f, -1.20f), new Vector3(.82f, 2.25f, .62f), .28f, signalMat, cartridgeRoot).transform;
            handleHome = dispatchHandle.localPosition;
            MgfText.World("출고", new Vector3(0f, .05f, -.36f), 2.3f, MgfLook.Hex("323B45"), dispatchHandle).transform.rotation = cam.transform.rotation;
            latchRoot = new GameObject("LatchAssembly").transform;
            latchRoot.SetParent(cartridgeRoot, false);
            latchRoot.localPosition = new Vector3(0f, 2.84f, -1.16f);
            MgfLook.Block("LatchBar", Vector3.zero, new Vector3(2.20f, .30f, .30f), .12f, chromeMat, latchRoot);
            MgfLook.Prim(PrimitiveType.Cylinder, "LatchPin", new Vector3(-1.05f, 0, 0), new Vector3(.34f, .26f, .34f), graphiteMat, latchRoot, false).transform.localRotation = Quaternion.Euler(90, 0, 0);
            caseGlow = MgfLook.Block("CaseGlow", new Vector3(0f, .1f, -1.50f), new Vector3(5.9f, 5.35f, .06f), .30f, MgfLook.Additive(new Color(1f, .86f, .20f, .65f), MgfLook.SoftDot), cartridgeRoot);
            caseGlow.GetComponent<Collider>().enabled = false;
            caseGlow.SetActive(false);
        }

        void BuildBelt()
        {
            beltRoot = new GameObject("DispatchBelt").transform;
            beltRoot.SetParent(truckRoot, false);
            beltRoot.localPosition = new Vector3(5.65f, -.2f, -.12f);
            MgfLook.Block("BeltBed", new Vector3(0, -1.72f, -.10f), new Vector3(4.5f, .62f, 3.05f), .22f, graphiteMat, beltRoot);
            MgfLook.Block("BeltSurface", new Vector3(0, -1.34f, -.48f), new Vector3(4.25f, .16f, 2.54f), .08f, beltMat, beltRoot);
            for (int i = 0; i < rollers.Length; i++)
            {
                var roller = MgfLook.Prim(PrimitiveType.Cylinder, "BeltRoller" + i,
                    new Vector3(-1.72f + i * .57f, -1.27f, -1.45f), new Vector3(.30f, 1.18f, .30f), chromeMat, beltRoot, false);
                roller.transform.localRotation = Quaternion.Euler(90, 0, 0);
                rollers[i] = roller.transform;
            }
            var target = MgfLook.Block("BeltTarget", new Vector3(0f, .25f, -.62f), new Vector3(4.25f, 3.15f, .14f), .28f, clearBlueMat, beltRoot);
            target.GetComponent<Collider>().enabled = false;
            var label = MgfText.World("출고 벨트", new Vector3(0f, 2.10f, -.9f), 3.3f, MgfLook.Hex("F7F7EC"), beltRoot);
            label.outlineWidth = .16f; label.outlineColor = MgfLook.Hex("323B45"); label.transform.rotation = cam.transform.rotation;
            for (int i = 0; i < bolts.Length; i++)
            {
                var bolt = MgfLook.Prim(PrimitiveType.Cylinder, "LifeBolt" + i, new Vector3(-.72f + i * .72f, 2.92f, -.95f), new Vector3(.26f, .18f, .26f), chromeMat, beltRoot, false);
                bolt.transform.localRotation = Quaternion.Euler(90, 0, 0);
                bolts[i] = bolt.transform;
            }
        }

        void BuildBeetle()
        {
            beetleRoot = new GameObject("WindupBeetleMechanic").transform;
            beetleRoot.SetParent(truckRoot, false);
            beetleRoot.localPosition = new Vector3(5.35f, 2.28f, -1.38f);
            var body = MgfLook.Prim(PrimitiveType.Sphere, "RoundedShell", Vector3.zero, new Vector3(1.34f, .70f, 1.55f), lemonMat, beetleRoot, false);
            body.transform.localRotation = Quaternion.Euler(10, 0, 0);
            MgfLook.Prim(PrimitiveType.Sphere, "ShellBack", new Vector3(0f, .14f, .52f), new Vector3(1.18f, .62f, .86f), cobaltMat, beetleRoot, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0f, -.02f, -.88f), new Vector3(.72f, .56f, .62f), graphiteMat, beetleRoot, false);
            for (int i = 0; i < beetleLegs.Length; i++)
            {
                int side = i < 3 ? -1 : 1;
                int leg = i % 3;
                var foot = MgfLook.Block("ShortLeg" + i, new Vector3(side * (.72f + leg * .08f), -.35f, -.48f + leg * .48f), new Vector3(.62f, .16f, .18f), .08f, graphiteMat, beetleRoot);
                foot.GetComponent<Collider>().enabled = false;
                foot.transform.localRotation = Quaternion.Euler(0, side * (18 + leg * 10), side * 14);
                beetleLegs[i] = foot.transform;
            }
            var keyStem = MgfLook.Prim(PrimitiveType.Cylinder, "WindingStem", new Vector3(0f, .76f, .30f), new Vector3(.16f, .42f, .16f), chromeMat, beetleRoot, false);
            var key = MgfLook.Block("WindingKey", new Vector3(0f, 1.10f, .30f), new Vector3(1.0f, .18f, .22f), .07f, chromeMat, beetleRoot);
            key.GetComponent<Collider>().enabled = false;
            var claw = MgfLook.Block("SmallClaw", new Vector3(-.82f, -.03f, -.96f), new Vector3(.48f, .18f, .18f), .07f, chromeMat, beetleRoot);
            claw.GetComponent<Collider>().enabled = false;
            claw.transform.localRotation = Quaternion.Euler(0, -26, -15);
        }

        void BuildGuideObjects()
        {
            marbleGhost = MgfLook.Prim(PrimitiveType.Sphere, "DraggedMarble", Vector3.zero, Vector3.one * 1.10f, cobaltMat, worldRoot, false);
            marbleGhost.SetActive(false);
            guideFinger = MgfLook.Block("GhostHand", Vector3.zero, new Vector3(.72f, .26f, .72f), .22f, signalMat, worldRoot);
            guideFinger.GetComponent<Collider>().enabled = false;
            guideFinger.SetActive(false);
            guideRing = MgfLook.Block("GuideRing", Vector3.zero, new Vector3(1.55f, 1.55f, .05f), .28f, MgfLook.Additive(new Color(1f, .90f, .28f, .78f), MgfLook.SoftDot), worldRoot);
            guideRing.GetComponent<Collider>().enabled = false;
            guideRing.SetActive(false);
            dragTrail = MakeLine("MarbleTrail", .10f, MgfLook.Hex("FFF1A6"), 5);
            dragTrail.gameObject.SetActive(false);
            guideTrail = MakeLine("OnboardingPath", .15f, MgfLook.Hex("FFF1A6"), 8);
            guideTrail.gameObject.SetActive(false);
            for (int i = 0; i < ripples.Length; i++)
            {
                ripples[i] = MgfLook.Block("TapRipple" + i, Vector3.zero, new Vector3(.2f, .2f, .03f), .08f,
                    MgfLook.Alpha(new Color(.25f, .38f, .86f, .55f), MgfLook.SoftDot), worldRoot);
                ripples[i].GetComponent<Collider>().enabled = false;
                ripples[i].SetActive(false);
            }
        }

        LineRenderer MakeLine(string name, float width, Color color, int cap)
        {
            var go = new GameObject(name);
            go.transform.SetParent(worldRoot, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = MgfLook.Alpha(color, MgfLook.SoftDot);
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = width;
            line.numCapVertices = cap;
            line.numCornerVertices = cap;
            line.useWorldSpace = true;
            line.positionCount = 0;
            return line;
        }

        void BuildUi()
        {
            canvas = MgfText.Canvas;
            playUi = Root("PlayUI");
            titleUi = Root("TitleUI");
            endUi = Root("EndUI");

            var art = new GameObject("TitleKeyArt", typeof(RectTransform), typeof(RawImage));
            art.transform.SetParent(titleUi.transform, false);
            Stretch((RectTransform)art.transform);
            titleArt = art.GetComponent<RawImage>();
            titleArt.texture = Resources.Load<Texture2D>("GuseulGyodae/title");
            titleArt.color = Color.white;
            titleArt.raycastTarget = false;
            if (!titleArt.texture) titleArt.enabled = false; // 생성 원화 로드 실패 시 런타임 트럭이 폴백.

            var sign = MakePanel("RooftopSign", titleUi.transform, new Color32(247, 247, 236, 246), out _);
            logoUi = MakeText("구슬\n교대", sign.transform, 64f, MgfLook.Hex("315CC8"), TextAlignmentOptions.Center);
            logoUi.outlineWidth = .24f; logoUi.outlineColor = MgfLook.Hex("F1D34F");
            logoUi.fontStyle = FontStyles.Bold;
            titleTagUi = MakeText("구슬을 바꿔 보내라", titleUi.transform, 22f, MgfLook.Hex("323B45"), TextAlignmentOptions.Center);
            titleMetaUi = MakeText("중학교 2학년 · 확률 · 최고 기록 0", titleUi.transform, 15f, MgfLook.Hex("323B45"), TextAlignmentOptions.Center);
            titleHandle = MakePanel("DoorHandle", titleUi.transform, new Color32(49, 92, 200, 244), out _).GetComponent<Image>();
            titleStartUi = MakeText("문 손잡이 눌러 열기", titleHandle.transform, 18f, Color.white, TextAlignmentOptions.Center);
            Rect((RectTransform)sign.transform, new Vector2(.5f, 1f), new Vector2(0, -124f), new Vector2(260f, 180f));
            Rect((RectTransform)logoUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(245f, 160f));
            Rect((RectTransform)titleTagUi.transform, new Vector2(.5f, 1f), new Vector2(0, -235f), new Vector2(350f, 46f));
            Rect((RectTransform)titleMetaUi.transform, new Vector2(.5f, 0f), new Vector2(0, 36f), new Vector2(350f, 34f));
            Rect((RectTransform)titleHandle.transform, new Vector2(.5f, 0f), new Vector2(0, 104f), new Vector2(278f, 64f));
            Rect((RectTransform)titleStartUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(260f, 54f));

            var header = MakePanel("TruckHeader", playUi.transform, new Color32(241, 211, 79, 248), out _);
            hudUi = MakeText("", header.transform, 15f, MgfLook.Hex("323B45"), TextAlignmentOptions.Center);
            Rect((RectTransform)header.transform, new Vector2(.5f, 1f), new Vector2(0, -28f), new Vector2(380f, 52f));
            Rect((RectTransform)hudUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(370f, 42f));

            orderCard = MakePanel("OrderBoard", playUi.transform, new Color32(247, 247, 236, 246), out orderCardImage);
            orderUi = MakeText("", orderCard.transform, 15f, MgfLook.Hex("315CC8"), TextAlignmentOptions.Center);
            promptUi = MakeText("", orderCard.transform, 17f, MgfLook.Hex("323B45"), TextAlignmentOptions.Center);
            promptUi.textWrappingMode = TextWrappingModes.Normal;
            Rect((RectTransform)orderCard.transform, new Vector2(.5f, 1f), new Vector2(0, -127f), new Vector2(370f, 142f));
            Rect((RectTransform)orderUi.transform, new Vector2(.5f, 1f), new Vector2(0, -21f), new Vector2(340f, 28f));
            Rect((RectTransform)promptUi.transform, new Vector2(.5f, .5f), new Vector2(-18f, -10f), new Vector2(295f, 96f));

            targetRoot = MakePanel("TargetFraction", orderCard.transform, new Color32(49, 92, 200, 244), out _);
            targetNumUi = MakeText("1", targetRoot.transform, 25f, Color.white, TextAlignmentOptions.Center);
            targetDenUi = MakeText("2", targetRoot.transform, 25f, Color.white, TextAlignmentOptions.Center);
            var lineGo = new GameObject("FractionLine", typeof(RectTransform), typeof(Image));
            lineGo.transform.SetParent(targetRoot.transform, false);
            targetLine = lineGo.GetComponent<Image>(); targetLine.color = Color.white; targetLine.raycastTarget = false;
            Rect((RectTransform)targetRoot.transform, new Vector2(1f, .5f), new Vector2(-42f, -7f), new Vector2(74f, 94f));
            Rect((RectTransform)targetNumUi.transform, new Vector2(.5f, 1f), new Vector2(0, -21f), new Vector2(64f, 32f));
            Rect((RectTransform)targetLine.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(46f, 4f));
            Rect((RectTransform)targetDenUi.transform, new Vector2(.5f, 0f), new Vector2(0, 21f), new Vector2(64f, 32f));

            goalUi = MakeText("구슬을 바꿔 확률을 맞추면 출고된다", playUi.transform, 17f, MgfLook.Hex("323B45"), TextAlignmentOptions.Center);
            goalUi.outlineWidth = .12f; goalUi.outlineColor = new Color32(247, 247, 236, 230);
            countUi = MakeText("", playUi.transform, 16f, MgfLook.Hex("323B45"), TextAlignmentOptions.Center);
            Rect((RectTransform)goalUi.transform, new Vector2(.5f, 1f), new Vector2(0, -214f), new Vector2(376f, 38f));
            Rect((RectTransform)countUi.transform, new Vector2(.5f, 0f), new Vector2(0, 35f), new Vector2(376f, 38f));

            toastPanel = MakePanel("ToastPanel", playUi.transform, new Color32(50, 59, 69, 235), out toastImage);
            toastUi = MakeText("", toastPanel.transform, 16f, Color.white, TextAlignmentOptions.Center);
            toastUi.textWrappingMode = TextWrappingModes.Normal;
            Rect((RectTransform)toastPanel.transform, new Vector2(.5f, .5f), new Vector2(0, -72f), new Vector2(354f, 86f));
            Rect((RectTransform)toastUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(334f, 74f));
            toastPanel.SetActive(false);

            receiptPanel = MakePanel("Receipt", playUi.transform, new Color32(247, 247, 236, 248), out receiptImage);
            receiptUi = MakeText("", receiptPanel.transform, 17f, MgfLook.Hex("323B45"), TextAlignmentOptions.Center);
            receiptUi.textWrappingMode = TextWrappingModes.Normal;
            var receiptMath = new GameObject("ReceiptFractionEquation", typeof(RectTransform));
            receiptMath.transform.SetParent(receiptPanel.transform, false);
            receiptMadeNum = MakeText("3", receiptMath.transform, 25f, MgfLook.Hex("315CC8"), TextAlignmentOptions.Center);
            receiptMadeDen = MakeText("6", receiptMath.transform, 25f, MgfLook.Hex("315CC8"), TextAlignmentOptions.Center);
            receiptTargetNum = MakeText("1", receiptMath.transform, 25f, MgfLook.Hex("315CC8"), TextAlignmentOptions.Center);
            receiptTargetDen = MakeText("2", receiptMath.transform, 25f, MgfLook.Hex("315CC8"), TextAlignmentOptions.Center);
            receiptSignUi = MakeText("=", receiptMath.transform, 27f, MgfLook.Hex("323B45"), TextAlignmentOptions.Center);
            var madeLineGo = new GameObject("MadeFractionLine", typeof(RectTransform), typeof(Image));
            madeLineGo.transform.SetParent(receiptMath.transform, false);
            receiptMadeLine = madeLineGo.GetComponent<Image>(); receiptMadeLine.color = MgfLook.Hex("315CC8"); receiptMadeLine.raycastTarget = false;
            var targetLineGo = new GameObject("TargetFractionLine", typeof(RectTransform), typeof(Image));
            targetLineGo.transform.SetParent(receiptMath.transform, false);
            receiptTargetLine = targetLineGo.GetComponent<Image>(); receiptTargetLine.color = MgfLook.Hex("315CC8"); receiptTargetLine.raycastTarget = false;
            receiptNoteUi = MakeText("", receiptPanel.transform, 13f, MgfLook.Hex("323B45"), TextAlignmentOptions.Center);
            receiptNoteUi.textWrappingMode = TextWrappingModes.Normal;
            Rect((RectTransform)receiptPanel.transform, new Vector2(.5f, .5f), new Vector2(0, 100f), new Vector2(330f, 190f));
            Rect((RectTransform)receiptUi.transform, new Vector2(.5f, 1f), new Vector2(0, -25f), new Vector2(306f, 38f));
            Rect((RectTransform)receiptMath.transform, new Vector2(.5f, .5f), new Vector2(0, 1f), new Vector2(240f, 86f));
            Rect((RectTransform)receiptMadeNum.transform, new Vector2(.5f, .5f), new Vector2(-63f, 23f), new Vector2(58f, 32f));
            Rect((RectTransform)receiptMadeLine.transform, new Vector2(.5f, .5f), new Vector2(-63f, 0), new Vector2(48f, 4f));
            Rect((RectTransform)receiptMadeDen.transform, new Vector2(.5f, .5f), new Vector2(-63f, -23f), new Vector2(58f, 32f));
            Rect((RectTransform)receiptSignUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(45f, 48f));
            Rect((RectTransform)receiptTargetNum.transform, new Vector2(.5f, .5f), new Vector2(63f, 23f), new Vector2(58f, 32f));
            Rect((RectTransform)receiptTargetLine.transform, new Vector2(.5f, .5f), new Vector2(63f, 0), new Vector2(48f, 4f));
            Rect((RectTransform)receiptTargetDen.transform, new Vector2(.5f, .5f), new Vector2(63f, -23f), new Vector2(58f, 32f));
            Rect((RectTransform)receiptNoteUi.transform, new Vector2(.5f, 0f), new Vector2(0, 23f), new Vector2(306f, 38f));
            receiptPanel.SetActive(false);

            var endPanel = MakePanel("ResultPanel", endUi.transform, new Color32(247, 247, 236, 248), out endPanelImage);
            endTitleUi = MakeText("", endPanel.transform, 32f, MgfLook.Hex("315CC8"), TextAlignmentOptions.Center);
            endStatsUi = MakeText("", endPanel.transform, 19f, MgfLook.Hex("323B45"), TextAlignmentOptions.Center);
            endStatsUi.textWrappingMode = TextWrappingModes.Normal;
            endHintUi = MakeText("트럭 문을 눌러 다시 출고", endPanel.transform, 18f, Color.white, TextAlignmentOptions.Center);
            var restart = MakePanel("RestartHandle", endPanel.transform, new Color32(49, 92, 200, 248), out _);
            restartRect = (RectTransform)restart.transform;
            endHintUi.transform.SetParent(restart.transform, false);
            Rect((RectTransform)endPanel.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(356f, 410f));
            Rect((RectTransform)endTitleUi.transform, new Vector2(.5f, 1f), new Vector2(0, -63f), new Vector2(330f, 90f));
            Rect((RectTransform)endStatsUi.transform, new Vector2(.5f, .5f), new Vector2(0, -15f), new Vector2(322f, 174f));
            Rect(restartRect, new Vector2(.5f, 0f), new Vector2(0, 52f), new Vector2(310f, 62f));
            Rect((RectTransform)endHintUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(298f, 54f));
        }

        GameObject Root(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)go.transform);
            return go;
        }

        GameObject MakePanel(string name, Transform parent, Color color, out Image image)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return go;
        }

        TextMeshProUGUI MakeText(string text, Transform parent, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.text = text; t.fontStyle = FontStyles.Bold;
            return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void Rect(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = offset; rt.sizeDelta = size;
        }

        void ShowPhaseUi()
        {
            bool title = phase == Phase.Title;
            bool end = phase == Phase.End;
            titleUi.SetActive(title);
            playUi.SetActive(!title && !end);
            endUi.SetActive(end);
            worldRoot.gameObject.SetActive(!title || titleArt == null || !titleArt.enabled);
            if (!title && !end) RefreshProblemUi();
        }

        void RefreshProblemUi()
        {
            if (current == null || !promptUi) return;
            string stage = phase == Phase.Practice ? "연습" : "주문 " + (orderIndex + 1) + "/10 · " + (current.band == 1 ? "기본 사건" : current.band == 2 ? "보완사건" : "동시 사건");
            orderUi.text = stage;
            IntFraction target = GuseulRules.Target(current);
            // 문제 은행에는 목표 수를 온전히 남기되, 화면에서는 옆의 정면 조판 분수와
            // 중복되는 1/2 평문 표기를 걷어 낸다.
            promptUi.text = current.prompt.Replace("확률이 " + target.Display + "이 되도록", "확률이 오른쪽 목표와 같도록");
            targetNumUi.text = target.n.ToString();
            targetDenUi.text = target.d.ToString();
            bool integer = target.d == 1;
            targetLine.gameObject.SetActive(!integer);
            targetDenUi.gameObject.SetActive(!integer);
            if (integer) Rect((RectTransform)targetNumUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(64f, 58f));
            else Rect((RectTransform)targetNumUi.transform, new Vector2(.5f, 1f), new Vector2(0, -21f), new Vector2(64f, 32f));
            goalUi.text = phase == Phase.Practice
                ? (!practiceSwapped ? "파랑 구슬 1개를 흰 슬롯으로 옮기시오" : "케이스 손잡이를 오른쪽 벨트로 끌어 보내시오")
                : "구슬을 바꿔 확률을 맞추면 출고된다";
            countUi.text = "현재  파랑 " + CountBlue() + "개 · 흰 " + (6 - CountBlue()) + "개 · 전체 6";
        }

        void UpdateUi(float dt)
        {
            displayScore = Mathf.MoveTowards(displayScore, st.score, Mathf.Max(120f, Mathf.Abs(st.score - displayScore) * 7f) * dt);
            int timer = phase == Phase.Playing ? Mathf.CeilToInt(orderLeft) : 0;
            if (hudUi && (phase == Phase.Practice || phase == Phase.Playing || phase == Phase.Reveal))
            {
                string time = phase == Phase.Practice ? "연습 · 시간 정지" : (repairMode ? "수리 " : "주문 ") + timer + "초";
                hudUi.text = "볼트 " + st.lives + "/3   ·   " + time + "   ·   점수 " + Mathf.RoundToInt(displayScore);
                lastTimer = timer;
            }
            if (toastLeft > 0f)
            {
                toastLeft -= dt;
                toastPanel.SetActive(true);
                toastUi.text = toastMessage;
                float pulse = 1f + Mathf.Sin((2.5f - toastLeft) * 13f) * .012f;
                toastPanel.transform.localScale = Vector3.one * pulse;
            }
            else if (toastPanel) toastPanel.SetActive(false);
            if (guideLeft > 0f) guideLeft -= dt;
        }

        void AnimateWorld(float dt)
        {
            ambientClock += dt;
            for (int i = 0; i < rollers.Length; i++) if (rollers[i]) rollers[i].Rotate(Vector3.forward, (55f + i * 3f) * dt, Space.Self);
            for (int i = 0; i < binMarbles.Length; i++) if (binMarbles[i])
            {
                Vector3 p = binMarbles[i].localPosition;
                p.z = -.82f + Mathf.Sin(ambientClock * 1.4f + i * .8f) * .035f;
                binMarbles[i].localPosition = p;
            }
            if (beetleRoot)
            {
                Vector3 p = beetleRoot.localPosition;
                p.x = 5.35f + Mathf.Sin(ambientClock * .72f) * .58f;
                p.y = 2.28f + Mathf.Sin(ambientClock * 1.44f) * .05f;
                if (phase == Phase.Reveal && revealCorrect) p.y += Mathf.Sin(Mathf.Clamp01(revealClock / .65f) * Mathf.PI) * .78f;
                beetleRoot.localPosition = p;
                Transform key = beetleRoot.Find("WindingKey"); if (key) key.Rotate(Vector3.up, 70f * dt, Space.Self);
                for (int i = 0; i < beetleLegs.Length; i++) if (beetleLegs[i]) beetleLegs[i].localRotation *= Quaternion.Euler(0, 0, Mathf.Sin(ambientClock * 6f + i) * 12f * dt);
            }
            if (phase == Phase.Reveal && cartridgeRoot)
            {
                float k = Mathf.Clamp01(revealClock / (revealCorrect ? .72f : .62f));
                if (revealCorrect)
                {
                    float eased = 1f - Mathf.Pow(1f - k, 3f);
                    cartridgeRoot.localPosition = cartridgeHome + Vector3.right * (eased * 4.65f);
                    latchRoot.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-22f, 0f, Mathf.Clamp01(k * 2f)));
                    caseGlow.SetActive(revealClock < .9f);
                    float stretch = 1f + Mathf.Sin(k * Mathf.PI) * .05f;
                    cartridgeRoot.localScale = new Vector3(1f + (stretch - 1f) * 1.8f, 1f - (stretch - 1f), 1f);
                }
                else
                {
                    float shake = Mathf.Sin(revealClock * 42f) * .18f * (1f - k);
                    cartridgeRoot.localPosition = cartridgeHome + Vector3.left * Mathf.Sin(k * Mathf.PI) * 1.25f + Vector3.up * shake;
                }
            }
            if (guideFinger && guideFinger.activeSelf)
            {
                Vector3 a = guideDispatch ? dispatchHandle.position : blueSupply.position;
                Vector3 b = guideDispatch ? BeltGuidePoint() : PracticeWhiteTarget();
                float t = (ambientClock % 1.2f) / 1.2f;
                float e = t * t * (3f - 2f * t);
                guideFinger.transform.position = Vector3.Lerp(a, b, e) + Vector3.forward * -.35f + Vector3.up * Mathf.Sin(t * Mathf.PI) * .35f;
                guideRing.transform.position = a + Vector3.forward * -.30f;
                float s = 1f + Mathf.Sin(ambientClock * 6f) * .16f;
                guideRing.transform.localScale = Vector3.one * s;
                guideTrail.positionCount = 3;
                guideTrail.SetPosition(0, a + Vector3.forward * -.20f);
                guideTrail.SetPosition(1, Vector3.Lerp(a, b, .5f) + Vector3.up * .75f + Vector3.forward * -.20f);
                guideTrail.SetPosition(2, b + Vector3.forward * -.20f);
            }
            for (int i = 0; i < ripples.Length; i++)
            {
                if (!ripples[i].activeSelf) continue;
                rippleLife[i] -= dt;
                float k = Mathf.Clamp01(1f - rippleLife[i] / .32f);
                ripples[i].transform.localScale = Vector3.one * Mathf.Lerp(.20f, 1.55f, k);
                if (rippleLife[i] <= 0f) ripples[i].SetActive(false);
            }
        }

        void ApplyResponsiveLayout()
        {
            if (Screen.width == lastScreenW && Screen.height == lastScreenH) return;
            lastScreenW = Screen.width; lastScreenH = Screen.height;
            float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            currentLand = aspect >= 1.28f;
            cam.orthographicSize = currentLand ? 6.45f : 7.55f;
            worldRoot.localScale = Vector3.one * (currentLand ? 1f : .55f);
            worldRoot.localPosition = currentLand ? new Vector3(0, -.15f, 0) : new Vector3(0, -.65f, 0);
            supplyRoot.localPosition = currentLand ? new Vector3(-5.65f, -.15f, -.05f) : new Vector3(-5.15f, -.15f, -.05f);
            beltRoot.localPosition = currentLand ? new Vector3(5.65f, -.2f, -.12f) : new Vector3(5.15f, -.2f, -.12f);
            if (titleArt && titleArt.texture)
            {
                float texAspect = titleArt.texture.width / (float)titleArt.texture.height;
                if (aspect > texAspect)
                {
                    float h = texAspect / aspect;
                    titleArt.uvRect = new Rect(0, (1f - h) * .5f, 1f, h);
                }
                else
                {
                    float w = aspect / texAspect;
                    titleArt.uvRect = new Rect((1f - w) * .5f, 0, w, 1f);
                }
            }
            if (currentLand)
            {
                Rect((RectTransform)orderCard.transform, new Vector2(.5f, 1f), new Vector2(0, -104f), new Vector2(760f, 116f));
                Rect((RectTransform)promptUi.transform, new Vector2(.5f, .5f), new Vector2(-20f, -8f), new Vector2(650f, 78f));
                Rect((RectTransform)goalUi.transform, new Vector2(.5f, 1f), new Vector2(0, -176f), new Vector2(760f, 38f));
            }
            else
            {
                Rect((RectTransform)orderCard.transform, new Vector2(.5f, 1f), new Vector2(0, -127f), new Vector2(370f, 142f));
                Rect((RectTransform)promptUi.transform, new Vector2(.5f, .5f), new Vector2(-18f, -10f), new Vector2(295f, 96f));
                Rect((RectTransform)goalUi.transform, new Vector2(.5f, 1f), new Vector2(0, -214f), new Vector2(376f, 38f));
            }
        }

        void ResetWorldForOrder(bool practice)
        {
            cartridgeRoot.localPosition = cartridgeHome;
            cartridgeRoot.localScale = Vector3.one;
            dispatchHandle.localPosition = handleHome;
            latchRoot.localRotation = Quaternion.Euler(0, 0, -22f);
            caseGlow.SetActive(false);
            receiptPanel.SetActive(false);
            for (int i = 0; i < bolts.Length; i++)
            {
                bolts[i].gameObject.SetActive(i < st.lives);
                bolts[i].localPosition = new Vector3(-.72f + i * .72f, 2.92f, -.95f);
            }
            RefreshMarbles(false);
        }

        void RefreshMarbles(bool punch)
        {
            for (int i = 0; i < 6; i++)
            {
                if (!marbleRenderers[i]) continue;
                marbleRenderers[i].sharedMaterial = blueSlots[i] ? cobaltMat : pearlMat;
                marbleDots[i].sharedMaterial = blueSlots[i] ? pearlMat : cobaltMat;
                if (punch) MgfFx.Punch(slots[i], .12f, .20f);
            }
        }

        void SnapMarbleVisual(int slot, bool blue)
        {
            RefreshMarbles(false);
            MgfFx.Punch(slots[slot], .24f, .30f);
            caseGlow.SetActive(true);
            CancelInvoke(nameof(HideCaseGlow));
            Invoke(nameof(HideCaseGlow), .24f);
        }

        void HideCaseGlow() { if (caseGlow) caseGlow.SetActive(false); }

        bool RaycastControl(out string control, out int slot)
        {
            control = ""; slot = -1;
            Ray ray = cam.ScreenPointToRay(MgfPointer.Position);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            for (int i = 0; i < hits.Length; i++)
            {
                string name = hits[i].collider.gameObject.name;
                if (name == "SupplyBlue") { control = "blue"; return true; }
                if (name == "SupplyWhite") { control = "white"; return true; }
                if (name == "DispatchHandle") { control = "dispatch"; return true; }
                if (name.StartsWith("Slot") && int.TryParse(name.Substring(4), out slot)) { control = "slot"; return true; }
            }
            return false;
        }

        int SlotAtPointer(Vector2 screen)
        {
            Ray ray = cam.ScreenPointToRay(screen);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            for (int i = 0; i < hits.Length; i++)
            {
                string name = hits[i].collider.gameObject.name;
                if (name.StartsWith("Slot") && int.TryParse(name.Substring(4), out int slot)) return slot;
            }
            return -1;
        }

        bool ScreenToWorld(Vector2 screen, float z, out Vector3 point)
        {
            var plane = new Plane(Vector3.forward, new Vector3(0, 0, z));
            Ray ray = cam.ScreenPointToRay(screen);
            if (plane.Raycast(ray, out float d)) { point = ray.GetPoint(d); return true; }
            point = Vector3.zero; return false;
        }

        void BeginMarbleDrag(bool blue)
        {
            marbleGhost.GetComponent<Renderer>().sharedMaterial = blue ? cobaltMat : pearlMat;
            marbleGhost.transform.position = blue ? blueSupply.position : whiteSupply.position;
            marbleGhost.transform.localScale = new Vector3(.78f, 1.22f, .78f);
            marbleGhost.SetActive(true);
            dragTrail.gameObject.SetActive(true);
            dragTrail.positionCount = 2;
            UpdateMarbleDrag(MgfPointer.Position);
            MgfSfx.Play("tap", .18f);
        }

        void UpdateMarbleDrag(Vector2 screen)
        {
            if (!ScreenToWorld(screen, -1.65f, out Vector3 p)) return;
            marbleGhost.transform.position = Vector3.Lerp(marbleGhost.transform.position, p, .48f);
            marbleGhost.transform.localScale = Vector3.Lerp(marbleGhost.transform.localScale, Vector3.one, .22f);
            dragTrail.SetPosition(0, dragBlue ? blueSupply.position : whiteSupply.position);
            dragTrail.SetPosition(1, marbleGhost.transform.position);
        }

        void EndMarbleDrag()
        {
            marbleGhost.SetActive(false);
            dragTrail.gameObject.SetActive(false);
        }

        void BeginDispatchDrag()
        {
            cartridgeRoot.localScale = new Vector3(.96f, 1.05f, .96f);
            MgfSfx.Play("tap", .20f);
        }

        void UpdateDispatchDrag(Vector2 screen, Vector2 start)
        {
            float logical = Mathf.Max(1f, Screen.width / 390f);
            float x = Mathf.Clamp((screen.x - start.x) / (90f * logical), 0f, 1f);
            cartridgeRoot.localPosition = cartridgeHome + Vector3.right * (x * 1.25f);
            cartridgeRoot.localScale = Vector3.Lerp(new Vector3(.96f, 1.05f, .96f), Vector3.one, x);
        }

        void EndDispatchDrag(bool success)
        {
            if (!success)
            {
                cartridgeRoot.localPosition = cartridgeHome;
                cartridgeRoot.localScale = Vector3.one;
                ShakeHandle();
            }
        }

        void BeginRevealVisual(bool correct)
        {
            guideFinger.SetActive(false); guideRing.SetActive(false); guideTrail.gameObject.SetActive(false);
            toastLeft = 0f;
            toastPanel.SetActive(false);
            IntFraction made = GuseulRules.Probability(current, CountBlue());
            IntFraction target = GuseulRules.Target(current);
            receiptPanel.SetActive(true);
            if (correct)
            {
                receiptUi.text = revealPractice ? "파랑 3개를 직접 만들었다" : "출고 검사 통과";
                receiptNoteUi.text = revealPractice ? "전체 6개 중 파랑 3개" : "이론 확률과 관찰 상대도수는 다를 수 있다";
                receiptImage.color = new Color32(247, 247, 236, 250);
                MgfSfx.Play("correct", .45f);
                MgfSfx.Play("whoosh", .24f);
            }
            else
            {
                receiptUi.text = "수리 레일로 돌아온다";
                receiptNoteUi.text = "파랑 " + CountBlue() + "개 · 흰 " + (6 - CountBlue()) + "개";
                receiptImage.color = new Color32(255, 226, 178, 250);
                DropBoltVisual();
                MgfFx.Shake(cam, .07f, .20f); // 화면 흔들림은 오답 전용.
                MgfSfx.Play("wrong", .36f);
            }
            SetReceiptEquation(made, target, correct);
        }

        void SetReceiptEquation(IntFraction made, IntFraction target, bool equal)
        {
            SetReceiptFraction(receiptMadeNum, receiptMadeDen, receiptMadeLine, made);
            SetReceiptFraction(receiptTargetNum, receiptTargetDen, receiptTargetLine, target);
            receiptSignUi.text = equal ? "=" : "≠";
            receiptSignUi.color = equal ? MgfLook.Hex("315CC8") : MgfLook.Hex("9A4D35");
        }

        void SetReceiptFraction(TextMeshProUGUI numerator, TextMeshProUGUI denominator, Image line, IntFraction value)
        {
            numerator.text = value.n.ToString();
            denominator.text = value.d.ToString();
            bool integer = value.d == 1;
            denominator.gameObject.SetActive(!integer);
            line.gameObject.SetActive(!integer);
            Vector2 pos = ((RectTransform)numerator.transform).anchoredPosition;
            pos.y = integer ? 0f : 23f;
            ((RectTransform)numerator.transform).anchoredPosition = pos;
        }

        void CompleteRevealVisual(bool correct)
        {
            cartridgeRoot.localPosition = cartridgeHome;
            cartridgeRoot.localScale = Vector3.one;
            latchRoot.localRotation = Quaternion.Euler(0, 0, -22f);
            caseGlow.SetActive(false);
            receiptPanel.SetActive(false);
        }

        void DropBoltVisual()
        {
            int lost = Mathf.Clamp(st.lives, 0, 2);
            for (int i = 0; i < bolts.Length; i++) bolts[i].gameObject.SetActive(i < st.lives || i == lost);
            if (lost >= 0 && lost < bolts.Length)
            {
                bolts[lost].localPosition += Vector3.down * 1.25f;
                MgfFx.Punch(bolts[lost], .20f, .24f);
            }
        }

        void PulseSlot(int slot) { if (slot >= 0 && slot < slots.Length) MgfFx.Punch(slots[slot], .18f, .24f); }

        void ShakeHandle()
        {
            dispatchHandle.localPosition = handleHome + Vector3.left * .16f;
            CancelInvoke(nameof(RestoreHandle)); Invoke(nameof(RestoreHandle), .16f);
            MgfSfx.Play("wrong", .16f);
        }

        void RestoreHandle() { if (dispatchHandle) dispatchHandle.localPosition = handleHome; }

        void ReplayGuide(bool enlarged)
        {
            guideDispatch = phase == Phase.Practice ? practiceSwapped : true;
            guideLeft = enlarged ? 4.8f : 3.5f;
            guideFinger.SetActive(true); guideRing.SetActive(true); guideTrail.gameObject.SetActive(true);
            guideFinger.transform.localScale = Vector3.one * (enlarged ? 1.35f : 1f);
        }

        void AdvancePracticeGuide()
        {
            guideDispatch = true;
            ReplayGuide(false);
            RefreshProblemUi();
            ShowToast("파랑 3개가 되었다 · 이제 케이스를 벨트로 보내시오", 2.8f);
        }

        Vector3 PracticeWhiteTarget()
        {
            for (int i = 0; i < slots.Length; i++) if (!blueSlots[i]) return slots[i].position;
            return slots[5].position;
        }

        Vector3 BeltGuidePoint() => beltRoot.TransformPoint(new Vector3(0f, .2f, -.85f));

        void ShowConceptBridge(string message)
        {
            ShowToast(message, 3.4f);
            orderLeft += 3.4f; // 새 단계 시범 동안 압박을 사실상 정지한다.
        }

        void ShowWrongReason(string id)
        {
            string msg;
            if (id == "equiprobable-half") msg = "두 색이 있어도 항상 1/2은 아니다 · 사건에 해당하는 구슬 수를 세시오";
            else if (id == "copy-reduced-numerator") msg = "기약분수의 분자를 그대로 복사하지 말고 전체 6개에서 다시 계산하시오";
            else if (id == "complement-not-inverted") msg = "파랑이 아닌 사건은 흰 구슬의 비율이다";
            else if (id == "add-independent-events") msg = "동시 사건은 A와 B의 유리한 순서쌍을 곱해 세시오";
            else msg = "만든 구성을 원자료 개수와 목표 확률로 다시 비교하시오";
            ShowToast(msg, 3.0f);
        }

        void RefuseInput(string message)
        {
            ShowToast(message, 2.2f);
            ShakeHandle();
            st.lastAction = "refuse";
            MgfBridge.NotifyChanged();
        }

        void ShowToast(string message, float seconds)
        {
            toastMessage = message; toastLeft = seconds;
            if (toastPanel) toastPanel.SetActive(true);
        }

        void SpawnTapRipple(Vector2 screen)
        {
            if (!ScreenToWorld(screen, -1.72f, out Vector3 p)) return;
            int i = rippleCursor++ % ripples.Length;
            ripples[i].transform.position = p;
            ripples[i].transform.localScale = Vector3.one * .2f;
            rippleLife[i] = .32f;
            ripples[i].SetActive(true);
        }

        void BeginTitlePress()
        {
            titlePulse = 0f;
            if (titleHandle) titleHandle.transform.localScale = new Vector3(.96f, .90f, 1f);
            if (titleStartUi) titleStartUi.text = "트럭 문 여는 중";
        }

        void UpdateTitlePress(Vector2 screen)
        {
            titlePulse += Time.deltaTime;
            if (titleHandle) titleHandle.transform.localScale = Vector3.one * (.94f + Mathf.Sin(titlePulse * 18f) * .015f);
        }

        void EndTitlePress()
        {
            if (titleHandle) titleHandle.transform.localScale = Vector3.one;
            if (titleStartUi) titleStartUi.text = "문 손잡이 눌러 열기";
        }

        void ShowEnd(string reason)
        {
            bool clear = reason == "clear";
            endTitleUi.text = clear ? "확률 검증 완료" : reason == "mastery" ? "출고 완료 · 검증 미달" : "트럭 정비 종료";
            endTitleUi.color = clear ? MgfLook.Hex("315CC8") : MgfLook.Hex("8C4F36");
            endStatsUi.text = "출고 " + st.solved + "/10\n첫 시도 " + st.firstCorrect + "/10\n동시 사건 첫 시도 " + st.band3FirstCorrect + "/4\n\n점수 " + st.score
                + (clear ? "\n숙련 스탬프 획득" : "\n목표: 첫 시도 8개 · 마지막 단계 3개");
            displayScore = 0f;
        }

        void FillScreenProbe()
        {
            st.screenW = Screen.width; st.screenH = Screen.height;
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = cam.WorldToScreenPoint(slots[i].position);
                st.slotPx[i * 2] = Mathf.RoundToInt(p.x);
                st.slotPx[i * 2 + 1] = Mathf.RoundToInt(Screen.height - p.y);
            }
            Vector3 b = cam.WorldToScreenPoint(blueSupply.position), w = cam.WorldToScreenPoint(whiteSupply.position);
            st.supplyPx[0] = Mathf.RoundToInt(b.x); st.supplyPx[1] = Mathf.RoundToInt(Screen.height - b.y);
            st.supplyPx[2] = Mathf.RoundToInt(w.x); st.supplyPx[3] = Mathf.RoundToInt(Screen.height - w.y);
            Vector3 h = cam.WorldToScreenPoint(dispatchHandle.position), t = cam.WorldToScreenPoint(BeltGuidePoint());
            st.dispatchPx[0] = Mathf.RoundToInt(h.x); st.dispatchPx[1] = Mathf.RoundToInt(Screen.height - h.y);
            st.dispatchPx[2] = Mathf.RoundToInt(t.x); st.dispatchPx[3] = Mathf.RoundToInt(Screen.height - t.y);
            Vector2 r = RectTransformUtility.WorldToScreenPoint(null, restartRect.TransformPoint(restartRect.rect.center));
            st.restartPx[0] = Mathf.RoundToInt(r.x); st.restartPx[1] = Mathf.RoundToInt(Screen.height - r.y);
        }
    }
}
