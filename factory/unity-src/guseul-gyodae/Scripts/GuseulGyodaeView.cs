// 구슬 교대 — 심야 결정 관측실의 기계식 별자리 링, UI, 실제 드래그, 피드백 연출.
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
            MgfLook.Sky(MgfLook.Hex("071126"), MgfLook.Hex("11264A"), MgfLook.Hex("050814"), .9f);
            MgfLook.Sun(new Vector3(48f, -34f, -18f), MgfLook.Hex("BFE8FF"), 1.16f, .76f);
            cam = MgfLook.Camera(new Vector3(0f, 9.6f, -17.5f), new Vector3(0f, .5f, 1.2f), 38f);
            cam.orthographic = true;
            cam.orthographicSize = 7.2f;
            cam.backgroundColor = MgfLook.Hex("081225");

            // 파스텔 야외 정원 대신 깊은 결정 동굴 속 관측실을 만든다.
            // 둥근 젤리 물성은 쓰지 않고 절단 유리·현무암·황동 평면으로 통일한다.
            lemonMat = MgfLook.Lit(MgfLook.Hex("C8872F"), .42f, .38f, MgfLook.Hex("C8872F") * .04f);
            lemonDarkMat = MgfLook.Lit(MgfLook.Hex("17243D"), .24f, .05f);
            cobaltMat = MgfLook.Lit(MgfLook.Hex("2CB7D9"), .72f, .08f, MgfLook.Hex("16A5D3") * .16f);
            pearlMat = MgfLook.Lit(MgfLook.Hex("D7EFF5"), .68f, .04f, MgfLook.Hex("D7EFF5") * .05f);
            graphiteMat = MgfLook.Lit(MgfLook.Hex("0C1325"), .24f, .18f);
            chromeMat = MgfLook.Lit(MgfLook.Hex("72839D"), .52f, .34f);
            beltMat = MgfLook.Lit(MgfLook.Hex("493A73"), .38f, .22f);
            signalMat = MgfLook.Lit(MgfLook.Hex("62F0C8"), .54f, .02f, MgfLook.Hex("34E9B8") * .40f);
            glassMat = MgfLook.Alpha(new Color(.16f, .72f, .90f, .24f), MgfLook.SoftDot);
            clearBlueMat = MgfLook.Alpha(new Color(.12f, .66f, .90f, .30f), MgfLook.SoftDot);
            clearWhiteMat = MgfLook.Alpha(new Color(.70f, .92f, 1f, .24f), MgfLook.SoftDot);

            worldRoot = new GameObject("MidnightCrystalObservatory").transform;
            truckRoot = new GameObject("SteppedBasaltChamber").transform;
            truckRoot.SetParent(worldRoot, false);

            MgfLook.Block("BasaltLowerDeck", new Vector3(0f, -2.75f, 1.45f), new Vector3(18.4f, .72f, 7.9f), .08f, graphiteMat, truckRoot);
            MgfLook.Block("BasaltUpperDeck", new Vector3(0f, -2.20f, .25f), new Vector3(15.8f, .46f, 6.1f), .06f, lemonDarkMat, truckRoot);
            MgfLook.Block("BrassMeridian", new Vector3(0f, -1.91f, -.56f), new Vector3(13.6f, .12f, .28f), .02f, lemonMat, truckRoot);
            for (int i = 0; i < 12; i++)
            {
                float side = i < 6 ? -1f : 1f;
                int q = i % 6;
                float x = side * (6.8f + (q % 2) * .75f);
                float y = -1.55f + q * .82f;
                var crystal = MgfLook.Block("CaveCrystal" + i, new Vector3(x, y, 2.2f + q * .22f),
                    new Vector3(.34f + (q % 3) * .12f, 1.4f + q * .28f, .42f), .015f,
                    q % 2 == 0 ? cobaltMat : chromeMat, truckRoot);
                crystal.transform.localRotation = Quaternion.Euler(0f, side * (12f + q * 5f), side * (18f + q * 4f));
            }
            for (int i = 0; i < 11; i++)
            {
                float x = -8.0f + i * 1.6f;
                var mark = MgfLook.Block("ConstellationMark" + i, new Vector3(x, 3.2f + Mathf.Sin(i * 1.9f) * .65f, 2.9f),
                    new Vector3(.18f, .18f, .05f), .01f, signalMat, truckRoot);
                mark.transform.localRotation = Quaternion.Euler(0, 0, i * 23f);
            }
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
            var supplies = new GameObject("CrystalSampleRacks").transform;
            supplyRoot = supplies;
            supplies.SetParent(truckRoot, false);
            supplies.localPosition = new Vector3(-5.65f, -.10f, -.05f);
            MgfLook.Block("BlueRack", new Vector3(0f, 1.55f, .08f), new Vector3(2.80f, 1.86f, .92f), .05f, clearBlueMat, supplies);
            MgfLook.Block("WhiteRack", new Vector3(0f, -1.05f, .08f), new Vector3(2.80f, 1.86f, .92f), .05f, clearWhiteMat, supplies);
            MgfLook.Block("RackSpine", new Vector3(0f, .22f, .72f), new Vector3(.24f, 4.95f, .32f), .02f, lemonMat, supplies);
            blueSupply = MakeControlMarble("SupplyBlue", new Vector3(0f, 1.55f, -1.18f), true, supplies);
            whiteSupply = MakeControlMarble("SupplyWhite", new Vector3(0f, -1.05f, -1.18f), false, supplies);
            for (int i = 0; i < binMarbles.Length; i++)
            {
                if (i >= 4) continue;
                bool blue = i < 2;
                int row = blue ? i : i - 2;
                float x = row == 0 ? -.68f : .68f;
                float y = blue ? 1.72f : -.88f;
                var b = MgfLook.Block("BinCrystal" + i, new Vector3(x, y, -.82f), new Vector3(.52f, .52f, .38f), .025f, blue ? cobaltMat : pearlMat, supplies);
                b.transform.localRotation = Quaternion.Euler(18f, 26f, 45f + i * 11f);
                binMarbles[i] = b.transform;
            }
            var blueLabel = MgfText.World("파랑 결정 공급", new Vector3(0f, 2.74f, -1.08f), 2.35f, MgfLook.Hex("DDF7FF"), supplies);
            var whiteLabel = MgfText.World("흰 결정 공급", new Vector3(0f, .24f, -1.08f), 2.35f, MgfLook.Hex("DDF7FF"), supplies);
            blueLabel.outlineWidth = .14f; blueLabel.outlineColor = MgfLook.Hex("071126");
            whiteLabel.outlineWidth = .14f; whiteLabel.outlineColor = MgfLook.Hex("071126");
            blueLabel.transform.rotation = cam.transform.rotation;
            whiteLabel.transform.rotation = cam.transform.rotation;
        }

        Transform MakeControlMarble(string name, Vector3 pos, bool blue, Transform parent)
        {
            var crystal = MgfLook.Block(name, pos, new Vector3(1.16f, 1.16f, .72f), .035f, blue ? cobaltMat : pearlMat, parent);
            crystal.transform.localRotation = Quaternion.Euler(18f, 24f, 45f);
            crystal.GetComponent<BoxCollider>().size = Vector3.one * 1.62f;
            var mark = MgfLook.Block(name + "Mark", new Vector3(0, 0, -.58f), new Vector3(.34f, .34f, .08f), .01f,
                blue ? pearlMat : cobaltMat, crystal.transform);
            mark.GetComponent<Collider>().enabled = false;
            return crystal.transform;
        }

        void BuildCartridge()
        {
            cartridgeRoot = new GameObject("MechanicalConstellationRing").transform;
            cartridgeRoot.SetParent(truckRoot, false);
            cartridgeRoot.localPosition = new Vector3(0f, .25f, -.48f);
            cartridgeHome = cartridgeRoot.localPosition;
            var domeBack = MgfLook.Prim(PrimitiveType.Cylinder, "AstrolabeBack", new Vector3(0f, .05f, .45f), new Vector3(3.05f, .20f, 3.05f), graphiteMat, cartridgeRoot, false);
            domeBack.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var starGlass = MgfLook.Block("StarChartGlass", new Vector3(0f, .05f, -.80f), new Vector3(5.55f, 5.55f, .20f), .04f, glassMat, cartridgeRoot);
            starGlass.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            starGlass.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            starGlass.GetComponent<Collider>().enabled = false;
            for (int i = 0; i < 6; i++)
            {
                float angle = (90f - i * 60f) * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * 1.72f;
                float y = Mathf.Sin(angle) * 1.72f + .05f;
                var socket = MgfLook.Block("OrbitSocket" + (i + 1), new Vector3(x, y, -.66f), new Vector3(.90f, .90f, .18f), .02f, chromeMat, cartridgeRoot);
                socket.transform.localRotation = Quaternion.Euler(0, 0, 45f);
                socket.GetComponent<Collider>().enabled = false;
                var marble = MgfLook.Block("Slot" + i, new Vector3(x, y, -1.18f), new Vector3(1.12f, 1.12f, .68f), .035f, cobaltMat, cartridgeRoot);
                marble.transform.localRotation = Quaternion.Euler(18f, 24f, 45f + i * 12f);
                marble.GetComponent<BoxCollider>().size = Vector3.one * 1.45f;
                slots[i] = marble.transform;
                marbleRenderers[i] = marble.GetComponent<Renderer>();
                var dot = MgfLook.Block("SlotMark" + i, new Vector3(.25f, .15f, -.50f), new Vector3(.30f, .30f, .08f), .01f, pearlMat, marble.transform);
                dot.GetComponent<Collider>().enabled = false;
                marbleDots[i] = dot.GetComponent<Renderer>();
                var plate = MgfLook.Block("SlotNumberPlate" + i, new Vector3(x, y - .72f, -1.18f), new Vector3(.58f, .28f, .10f), .01f, lemonMat, cartridgeRoot);
                plate.GetComponent<Collider>().enabled = false;
                var t = MgfText.World((i + 1).ToString(), new Vector3(0, 0, -.08f), 3.0f, MgfLook.Hex("323B45"), plate.transform);
                t.transform.rotation = cam.transform.rotation;
            }
            var orbitRim = MgfLook.Prim(PrimitiveType.Cylinder, "BrassAstrolabeRim", new Vector3(0f, .05f, -.64f), new Vector3(3.28f, .13f, 3.28f), lemonMat, cartridgeRoot, false);
            orbitRim.transform.localRotation = Quaternion.Euler(90, 0, 0);
            dispatchHandle = MgfLook.Block("DispatchHandle", new Vector3(3.38f, .1f, -1.20f), new Vector3(.92f, 2.35f, .62f), .04f, signalMat, cartridgeRoot).transform;
            handleHome = dispatchHandle.localPosition;
            MgfText.World("관측", new Vector3(0f, .05f, -.36f), 2.3f, MgfLook.Hex("071126"), dispatchHandle).transform.rotation = cam.transform.rotation;
            latchRoot = new GameObject("OrbitHalo").transform;
            latchRoot.SetParent(cartridgeRoot, false);
            latchRoot.localPosition = new Vector3(0f, .05f, -1.30f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                var spark = MgfLook.Block("HaloMark" + i,
                    new Vector3(Mathf.Cos(angle) * 2.72f, Mathf.Sin(angle) * 2.72f, 0f),
                    Vector3.one * .20f, .01f, signalMat, latchRoot);
                spark.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg + 45f);
            }
            caseGlow = MgfLook.Prim(PrimitiveType.Sphere, "OrbitGlow", new Vector3(0f, .05f, -1.50f), new Vector3(6.25f, 6.25f, .06f),
                MgfLook.Additive(new Color(.45f, 1f, .84f, .58f), MgfLook.SoftDot), cartridgeRoot, false);
            caseGlow.GetComponent<Collider>().enabled = false;
            caseGlow.SetActive(false);
        }

        void BuildBelt()
        {
            beltRoot = new GameObject("ProbabilityOrbitGate").transform;
            beltRoot.SetParent(truckRoot, false);
            beltRoot.localPosition = new Vector3(5.65f, -.2f, -.12f);
            var gateBack = MgfLook.Prim(PrimitiveType.Cylinder, "OrbitGateBack", new Vector3(0, -.45f, -.18f), new Vector3(2.18f, .22f, 2.18f), graphiteMat, beltRoot, false);
            gateBack.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var gateGlass = MgfLook.Block("OrbitGateGlass", new Vector3(0, -.45f, -.62f), new Vector3(3.15f, 3.15f, .20f), .03f, clearBlueMat, beltRoot);
            gateGlass.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            gateGlass.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            gateGlass.GetComponent<Collider>().enabled = false;
            for (int i = 0; i < rollers.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / rollers.Length;
                var roller = MgfLook.Block("OrbitRune" + i,
                    new Vector3(Mathf.Cos(angle) * 1.72f, -.45f + Mathf.Sin(angle) * 1.72f, -1.05f),
                    new Vector3(.34f, .34f, .16f), .01f, i % 2 == 0 ? signalMat : pearlMat, beltRoot);
                roller.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg + 45f);
                roller.GetComponent<Collider>().enabled = false;
                rollers[i] = roller.transform;
            }
            var label = MgfText.World("관측 관문", new Vector3(0f, 2.15f, -1.0f), 3.3f, MgfLook.Hex("DDF7FF"), beltRoot);
            label.outlineWidth = .16f; label.outlineColor = MgfLook.Hex("071126"); label.transform.rotation = cam.transform.rotation;
            for (int i = 0; i < bolts.Length; i++)
            {
                var bolt = MgfLook.Block("LifeBolt" + i, new Vector3(-.72f + i * .72f, 2.92f, -.95f), new Vector3(.34f, .34f, .18f), .02f, lemonMat, beltRoot);
                bolt.GetComponent<Collider>().enabled = false;
                bolt.transform.localRotation = Quaternion.Euler(15, 0, 45f);
                bolts[i] = bolt.transform;
            }
        }

        void BuildBeetle()
        {
            beetleRoot = new GameObject("SurveyDrone").transform;
            beetleRoot.SetParent(truckRoot, false);
            beetleRoot.localPosition = new Vector3(5.35f, 2.28f, -1.38f);
            var body = MgfLook.Block("DroneCore", Vector3.zero, new Vector3(.92f, 1.10f, .76f), .04f, lemonDarkMat, beetleRoot);
            body.GetComponent<Collider>().enabled = false;
            body.transform.localRotation = Quaternion.Euler(8, 18f, 45f);
            var lens = MgfLook.Block("SurveyLens", new Vector3(0f, .58f, -.48f), new Vector3(.48f, .48f, .18f), .02f, signalMat, beetleRoot);
            lens.GetComponent<Collider>().enabled = false;
            lens.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            for (int i = 0; i < beetleLegs.Length; i++)
            {
                int side = i < 3 ? -1 : 1;
                int leg = i % 3;
                var wing = MgfLook.Block("ScannerArm" + i,
                    new Vector3(side * (.76f + leg * .20f), .20f - leg * .42f, .16f + leg * .16f),
                    new Vector3(.92f, .15f, .18f), .02f, side < 0 ? cobaltMat : chromeMat, beetleRoot);
                wing.GetComponent<Collider>().enabled = false;
                wing.transform.localRotation = Quaternion.Euler(12, side * (18 + leg * 9), side * (24 + leg * 8));
                beetleLegs[i] = wing.transform;
            }
            var antennaLeft = MgfLook.Block("AntennaLeft", new Vector3(-.23f, 1.18f, -.18f), new Vector3(.12f, .75f, .12f), .06f, signalMat, beetleRoot);
            antennaLeft.GetComponent<Collider>().enabled = false; antennaLeft.transform.localRotation = Quaternion.Euler(0, 0, -24f);
            var antennaRight = MgfLook.Block("AntennaRight", new Vector3(.23f, 1.18f, -.18f), new Vector3(.12f, .75f, .12f), .06f, signalMat, beetleRoot);
            antennaRight.GetComponent<Collider>().enabled = false; antennaRight.transform.localRotation = Quaternion.Euler(0, 0, 24f);
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

            // 타이틀도 실제 플레이의 관측실을 가리지 않고 같은 월드를 그대로 보여 준다.
            titleArt = null;
            logoUi = MakeText("구슬 교대", titleUi.transform, 44f, MgfLook.Hex("DDF7FF"), TextAlignmentOptions.Center);
            logoUi.outlineWidth = .16f; logoUi.outlineColor = MgfLook.Hex("071126");
            logoUi.fontStyle = FontStyles.Bold;
            titleTagUi = MakeText("결정을 바꿔 별자리 확률을 맞춰라", titleUi.transform, 19f, MgfLook.Hex("62F0C8"), TextAlignmentOptions.Center);
            titleTagUi.outlineWidth = .12f; titleTagUi.outlineColor = MgfLook.Hex("071126");
            titleMetaUi = MakeText("중2 확률 · 심야 결정 관측실", titleUi.transform, 14f, MgfLook.Hex("DDF7FF"), TextAlignmentOptions.Center);
            titleMetaUi.outlineWidth = .12f; titleMetaUi.outlineColor = MgfLook.Hex("071126");
            titleHandle = MakePanel("OrbitStartGrip", titleUi.transform, new Color32(0, 0, 0, 0), out _).GetComponent<Image>();
            titleStartUi = MakeText("관측 링을 눌러 시작", titleHandle.transform, 17f, MgfLook.Hex("62F0C8"), TextAlignmentOptions.Center);
            titleStartUi.outlineWidth = .14f; titleStartUi.outlineColor = MgfLook.Hex("071126");
            Rect((RectTransform)logoUi.transform, new Vector2(.5f, 1f), new Vector2(0, -72f), new Vector2(330f, 66f));
            Rect((RectTransform)titleTagUi.transform, new Vector2(.5f, 1f), new Vector2(0, -125f), new Vector2(350f, 38f));
            Rect((RectTransform)titleMetaUi.transform, new Vector2(.5f, 0f), new Vector2(0, 24f), new Vector2(350f, 30f));
            Rect((RectTransform)titleHandle.transform, new Vector2(.5f, 0f), new Vector2(0, 68f), new Vector2(246f, 48f));
            Rect((RectTransform)titleStartUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(232f, 46f));

            var header = MakePanel("ObservatoryHeader", playUi.transform, new Color32(12, 19, 37, 224), out _);
            hudUi = MakeText("", header.transform, 13f, MgfLook.Hex("DDF7FF"), TextAlignmentOptions.Center);
            Rect((RectTransform)header.transform, new Vector2(.5f, 1f), new Vector2(0, -36f), new Vector2(380f, 68f));
            Rect((RectTransform)hudUi.transform, new Vector2(.5f, .5f), new Vector2(-20f, 0), new Vector2(324f, 60f));

            orderCard = MakePanel("ObservationSlate", playUi.transform, new Color32(12, 19, 37, 226), out orderCardImage);
            orderUi = MakeText("", orderCard.transform, 15f, MgfLook.Hex("62F0C8"), TextAlignmentOptions.Center);
            promptUi = MakeText("", orderCard.transform, 17f, MgfLook.Hex("DDF7FF"), TextAlignmentOptions.Center);
            promptUi.textWrappingMode = TextWrappingModes.Normal;
            Rect((RectTransform)orderCard.transform, new Vector2(.5f, 1f), new Vector2(0, -127f), new Vector2(370f, 142f));
            Rect((RectTransform)orderUi.transform, new Vector2(.5f, 1f), new Vector2(0, -21f), new Vector2(340f, 28f));
            Rect((RectTransform)promptUi.transform, new Vector2(.5f, .5f), new Vector2(-18f, -10f), new Vector2(295f, 96f));

            targetRoot = MakePanel("TargetFraction", orderCard.transform, new Color32(200, 135, 47, 238), out _);
            targetNumUi = MakeText("1", targetRoot.transform, 25f, Color.white, TextAlignmentOptions.Center);
            targetDenUi = MakeText("2", targetRoot.transform, 25f, Color.white, TextAlignmentOptions.Center);
            var lineGo = new GameObject("FractionLine", typeof(RectTransform), typeof(Image));
            lineGo.transform.SetParent(targetRoot.transform, false);
            targetLine = lineGo.GetComponent<Image>(); targetLine.color = Color.white; targetLine.raycastTarget = false;
            Rect((RectTransform)targetRoot.transform, new Vector2(1f, .5f), new Vector2(-42f, -7f), new Vector2(74f, 94f));
            Rect((RectTransform)targetNumUi.transform, new Vector2(.5f, 1f), new Vector2(0, -21f), new Vector2(64f, 32f));
            Rect((RectTransform)targetLine.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(46f, 4f));
            Rect((RectTransform)targetDenUi.transform, new Vector2(.5f, 0f), new Vector2(0, 21f), new Vector2(64f, 32f));

            goalUi = MakeText("결정을 바꾸고 관측 링을 돌려 확률을 확인한다", playUi.transform, 17f, MgfLook.Hex("62F0C8"), TextAlignmentOptions.Center);
            goalUi.outlineWidth = .14f; goalUi.outlineColor = MgfLook.Hex("071126");
            countUi = MakeText("", playUi.transform, 16f, MgfLook.Hex("DDF7FF"), TextAlignmentOptions.Center);
            countUi.outlineWidth = .12f; countUi.outlineColor = MgfLook.Hex("071126");
            Rect((RectTransform)goalUi.transform, new Vector2(.5f, 1f), new Vector2(0, -214f), new Vector2(376f, 38f));
            Rect((RectTransform)countUi.transform, new Vector2(.5f, 0f), new Vector2(0, 35f), new Vector2(376f, 38f));

            toastPanel = MakePanel("ToastPanel", playUi.transform, new Color32(12, 19, 37, 226), out toastImage);
            toastUi = MakeText("", toastPanel.transform, 14f, MgfLook.Hex("62F0C8"), TextAlignmentOptions.Center);
            toastUi.textWrappingMode = TextWrappingModes.Normal;
            Rect((RectTransform)toastPanel.transform, new Vector2(.5f, 1f), new Vector2(0, -294f), new Vector2(344f, 54f));
            Rect((RectTransform)toastUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(326f, 48f));
            toastPanel.SetActive(false);

            receiptPanel = MakePanel("ObservationReceipt", playUi.transform, new Color32(12, 19, 37, 236), out receiptImage);
            receiptUi = MakeText("", receiptPanel.transform, 17f, MgfLook.Hex("DDF7FF"), TextAlignmentOptions.Center);
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
            receiptNoteUi = MakeText("", receiptPanel.transform, 13f, MgfLook.Hex("DDF7FF"), TextAlignmentOptions.Center);
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

            var endPanel = MakePanel("ResultPanel", endUi.transform, new Color32(7, 17, 38, 178), out endPanelImage);
            endTitleUi = MakeText("", endPanel.transform, 30f, MgfLook.Hex("62F0C8"), TextAlignmentOptions.Center);
            endStatsUi = MakeText("", endPanel.transform, 17f, MgfLook.Hex("DDF7FF"), TextAlignmentOptions.Center);
            endStatsUi.textWrappingMode = TextWrappingModes.Normal;
            endHintUi = MakeText("관측 링 다시 맞추기", endPanel.transform, 17f, MgfLook.Hex("62F0C8"), TextAlignmentOptions.Center);
            var restart = MakePanel("RestartHandle", endPanel.transform, new Color32(23, 36, 61, 236), out _);
            restartRect = (RectTransform)restart.transform;
            endHintUi.transform.SetParent(restart.transform, false);
            Rect((RectTransform)endPanel.transform, new Vector2(.5f, 0f), new Vector2(0, 194f), new Vector2(344f, 300f));
            Rect((RectTransform)endTitleUi.transform, new Vector2(.5f, 1f), new Vector2(0, -46f), new Vector2(326f, 64f));
            Rect((RectTransform)endStatsUi.transform, new Vector2(.5f, .5f), new Vector2(0, -4f), new Vector2(318f, 150f));
            Rect(restartRect, new Vector2(.5f, 0f), new Vector2(0, 34f), new Vector2(292f, 50f));
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
            worldRoot.gameObject.SetActive(true);
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
                ? (!practiceSwapped ? "파랑 구슬샘→흰 슬롯: 끌거나 차례로 누르시오" : "회전 손잡이→확률 궤도: 끌거나 차례로 누르시오")
                : "구슬을 바꾸고 돔을 돌려 확률을 확인한다";
            countUi.text = "현재  파랑 " + CountBlue() + "개 · 흰 " + (6 - CountBlue()) + "개 · 전체 6";
        }

        void UpdateUi(float dt)
        {
            displayScore = Mathf.MoveTowards(displayScore, st.score, Mathf.Max(120f, Mathf.Abs(st.score - displayScore) * 7f) * dt);
            int timer = phase == Phase.Playing ? Mathf.CeilToInt(orderLeft) : 0;
            if (hudUi && (phase == Phase.Practice || phase == Phase.Playing || phase == Phase.Reveal))
            {
                string time = phase == Phase.Practice ? "연습 · 시간 정지" : (repairMode ? "수리 " : "주문 ") + timer + "초";
                string mastery = phase == Phase.Practice
                    ? "숙련 목표  첫 시도 8/10 · 동시 사건 3/4"
                    : "첫 " + st.firstCorrect + "/10(목표 8) · 동시 " + st.band3FirstCorrect + "/4(목표 3) · "
                        + (MasteryStillPossible() ? "가능" : "불가 · 연습 회전");
                hudUi.text = "볼트 " + st.lives + "/3 · " + time + " · 점수 " + Mathf.RoundToInt(displayScore) + "\n" + mastery;
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
            for (int i = 0; i < rollers.Length; i++) if (rollers[i])
            {
                Vector3 p = rollers[i].localPosition;
                float angle = ambientClock * .55f + i * Mathf.PI * 2f / rollers.Length;
                p.x = Mathf.Cos(angle) * 1.72f;
                p.y = -.45f + Mathf.Sin(angle) * 1.72f;
                rollers[i].localPosition = p;
            }
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
                for (int i = 0; i < beetleLegs.Length; i++) if (beetleLegs[i])
                    beetleLegs[i].localRotation *= Quaternion.Euler(0, 0, Mathf.Sin(ambientClock * 7f + i) * 20f * dt);
            }
            if (phase == Phase.Reveal && cartridgeRoot)
            {
                float k = Mathf.Clamp01(revealClock / (revealCorrect ? .72f : .62f));
                if (revealCorrect)
                {
                    float eased = 1f - Mathf.Pow(1f - k, 3f);
                    cartridgeRoot.localPosition = cartridgeHome;
                    cartridgeRoot.localRotation = Quaternion.Euler(0, 0, -eased * 360f);
                    latchRoot.localRotation = Quaternion.Euler(0, 0, eased * 620f);
                    caseGlow.SetActive(revealClock < .9f);
                    float stretch = 1f + Mathf.Sin(k * Mathf.PI) * .05f;
                    cartridgeRoot.localScale = Vector3.one * stretch;
                }
                else
                {
                    float shake = Mathf.Sin(revealClock * 42f) * .18f * (1f - k);
                    cartridgeRoot.localPosition = cartridgeHome + Vector3.left * Mathf.Sin(k * Mathf.PI) * 1.25f + Vector3.up * shake;
                    cartridgeRoot.localRotation = Quaternion.Euler(0, 0, shake * 5f);
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
            worldRoot.localScale = Vector3.one * (currentLand ? .84f : .55f);
            worldRoot.localPosition = currentLand ? new Vector3(2.25f, -.35f, 0) : new Vector3(0, -.65f, 0);
            supplyRoot.localPosition = currentLand ? new Vector3(-5.65f, -.15f, -.05f) : new Vector3(-4.20f, -.15f, -.05f);
            beltRoot.localPosition = currentLand ? new Vector3(5.65f, -.2f, -.12f) : new Vector3(4.20f, -.2f, -.12f);
            if (currentLand)
            {
                // 1280×800~2000px는 왼쪽 정보 존과 오른쪽 월드 존으로 나눈다.
                // 카드 폭을 고정해 초와이드에서 화면 대부분을 덮던 띠 배치를 없앤다.
                Rect((RectTransform)hudUi.transform.parent, new Vector2(0f, 1f), new Vector2(164f, -34f), new Vector2(306f, 58f));
                Rect((RectTransform)hudUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(286f, 52f));
                Rect((RectTransform)orderCard.transform, new Vector2(0f, .5f), new Vector2(164f, -10f), new Vector2(306f, 224f));
                Rect((RectTransform)orderUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -24f), new Vector2(280f, 30f));
                Rect((RectTransform)promptUi.transform, new Vector2(.5f, .5f), new Vector2(-30f, -10f), new Vector2(218f, 154f));
                Rect((RectTransform)targetRoot.transform, new Vector2(1f, .5f), new Vector2(-40f, -8f), new Vector2(66f, 98f));
                Rect((RectTransform)goalUi.transform, new Vector2(0f, .5f), new Vector2(164f, -143f), new Vector2(306f, 56f));
                Rect((RectTransform)countUi.transform, new Vector2(.72f, 0f), new Vector2(0f, 28f), new Vector2(360f, 38f));
                Rect((RectTransform)toastPanel.transform, new Vector2(0f, .5f), new Vector2(164f, -188f), new Vector2(306f, 54f));
                Rect((RectTransform)toastUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(288f, 48f));
                promptUi.fontSize = 12.5f;
                goalUi.fontSize = 14f;
                hudUi.fontSize = 11.5f;
            }
            else
            {
                Rect((RectTransform)hudUi.transform.parent, new Vector2(.5f, 1f), new Vector2(0, -36f), new Vector2(380f, 68f));
                Rect((RectTransform)hudUi.transform, new Vector2(.5f, .5f), new Vector2(-20f, 0), new Vector2(324f, 60f));
                Rect((RectTransform)orderCard.transform, new Vector2(.5f, 1f), new Vector2(0, -151f), new Vector2(370f, 158f));
                Rect((RectTransform)orderUi.transform, new Vector2(.5f, 1f), new Vector2(0, -21f), new Vector2(340f, 28f));
                Rect((RectTransform)promptUi.transform, new Vector2(.5f, .5f), new Vector2(-42f, -10f), new Vector2(242f, 112f));
                Rect((RectTransform)targetRoot.transform, new Vector2(1f, .5f), new Vector2(-42f, -7f), new Vector2(74f, 94f));
                Rect((RectTransform)goalUi.transform, new Vector2(.5f, 1f), new Vector2(0, -244f), new Vector2(376f, 38f));
                Rect((RectTransform)countUi.transform, new Vector2(.5f, 0f), new Vector2(0, 35f), new Vector2(376f, 38f));
                Rect((RectTransform)toastPanel.transform, new Vector2(.5f, 1f), new Vector2(0, -294f), new Vector2(344f, 54f));
                Rect((RectTransform)toastUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(326f, 48f));
                promptUi.fontSize = 17f;
                goalUi.fontSize = 17f;
                hudUi.fontSize = 13f;
            }
        }

        void ResetWorldForOrder(bool practice)
        {
            cartridgeRoot.localPosition = cartridgeHome;
            cartridgeRoot.localScale = Vector3.one;
            cartridgeRoot.localRotation = Quaternion.identity;
            dispatchHandle.localPosition = handleHome;
            latchRoot.localRotation = Quaternion.identity;
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

        // 보이는 오브젝트 주변을 넉넉히 잡되, 연습과 실전에 같은 판정을 쓴다.
        bool WideSnapControl(Vector2 screen, out string control, out int slot)
        {
            control = ""; slot = -1;
            float unit = Mathf.Max(.85f, Mathf.Min(Screen.width / 390f, Screen.height / 844f));
            float radius = 112f * unit;
            Vector3 blue = cam.WorldToScreenPoint(blueSupply.position);
            Vector3 white = cam.WorldToScreenPoint(whiteSupply.position);
            Vector3 spin = cam.WorldToScreenPoint(dispatchHandle.position);
            if (!currentLand)
            {
                Vector3 center = cam.WorldToScreenPoint(cartridgeRoot.position);
                float stationY = (blue.y + white.y + center.y + spin.y) * .25f;
                if (Mathf.Abs(screen.y - stationY) <= 220f * unit)
                {
                    float leftMid = (blue.x + center.x) * .5f;
                    float rightMid = (center.x + spin.x) * .5f;
                    if (screen.x < leftMid)
                    {
                        control = Mathf.Abs(screen.y - blue.y) <= Mathf.Abs(screen.y - white.y) ? "blue" : "white";
                        return true;
                    }
                    if (screen.x < rightMid)
                    {
                        slot = NearestSlotIndex(screen);
                        control = "slot";
                        return slot >= 0;
                    }
                    control = "dispatch";
                    return true;
                }
            }
            float best = float.MaxValue;
            float d = Vector2.Distance(screen, new Vector2(blue.x, blue.y));
            if (d <= radius && d < best) { best = d; control = "blue"; }
            d = Vector2.Distance(screen, new Vector2(white.x, white.y));
            if (d <= radius && d < best) { best = d; control = "white"; }
            d = Vector2.Distance(screen, new Vector2(spin.x, spin.y));
            if (d <= radius * 1.10f && d < best) { best = d; control = "dispatch"; }
            for (int i = 0; i < slots.Length; i++)
            {
                Vector3 p = cam.WorldToScreenPoint(slots[i].position);
                d = Vector2.Distance(screen, new Vector2(p.x, p.y));
                if (d <= 96f * unit && d < best) { best = d; control = "slot"; slot = i; }
            }
            return best < float.MaxValue;
        }

        bool NearOrbitGate(Vector2 screen)
        {
            float unit = Mathf.Max(.85f, Mathf.Min(Screen.width / 390f, Screen.height / 844f));
            Vector3 gate = cam.WorldToScreenPoint(BeltGuidePoint());
            if (!currentLand)
            {
                Vector3 center = cam.WorldToScreenPoint(cartridgeRoot.position);
                if (screen.x >= (center.x + gate.x) * .5f && Mathf.Abs(screen.y - gate.y) <= 150f * unit) return true;
            }
            return Vector2.Distance(screen, new Vector2(gate.x, gate.y)) <= 124f * unit;
        }

        int NearestSlotIndex(Vector2 screen)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < slots.Length; i++)
            {
                Vector3 p = cam.WorldToScreenPoint(slots[i].position);
                float d = Vector2.Distance(screen, new Vector2(p.x, p.y));
                if (d < bestDistance) { bestDistance = d; best = i; }
            }
            return best;
        }

        int NearestSlotInWideSnap(Vector2 screen)
        {
            float unit = Mathf.Max(.85f, Mathf.Min(Screen.width / 390f, Screen.height / 844f));
            float radius = 96f * unit;
            if (!currentLand)
            {
                Vector3 center = cam.WorldToScreenPoint(cartridgeRoot.position);
                Vector3 supply = cam.WorldToScreenPoint(blueSupply.position);
                Vector3 spin = cam.WorldToScreenPoint(dispatchHandle.position);
                float leftMid = (supply.x + center.x) * .5f;
                float rightMid = (center.x + spin.x) * .5f;
                if (screen.x >= leftMid && screen.x < rightMid && Mathf.Abs(screen.y - center.y) <= 220f * unit)
                    return NearestSlotIndex(screen);
            }
            int best = -1;
            float bestDistance = radius;
            for (int i = 0; i < slots.Length; i++)
            {
                Vector3 p = cam.WorldToScreenPoint(slots[i].position);
                float d = Vector2.Distance(screen, new Vector2(p.x, p.y));
                if (d < bestDistance) { bestDistance = d; best = i; }
            }
            return best;
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
            cartridgeRoot.localScale = Vector3.one * .97f;
            MgfSfx.Play("tap", .20f);
        }

        void UpdateDispatchDrag(Vector2 screen, Vector2 start)
        {
            float logical = Mathf.Max(1f, Screen.width / 390f);
            float x = Mathf.Clamp((screen.x - start.x) / (90f * logical), 0f, 1f);
            cartridgeRoot.localPosition = cartridgeHome;
            cartridgeRoot.localRotation = Quaternion.Euler(0, 0, -x * 58f);
            cartridgeRoot.localScale = Vector3.Lerp(Vector3.one * .97f, Vector3.one, x);
            dispatchHandle.localPosition = handleHome + Vector3.right * (x * .42f);
        }

        void EndDispatchDrag(bool success)
        {
            if (!success)
            {
                cartridgeRoot.localPosition = cartridgeHome;
                cartridgeRoot.localScale = Vector3.one;
                cartridgeRoot.localRotation = Quaternion.identity;
                dispatchHandle.localPosition = handleHome;
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
                receiptUi.text = revealPractice ? "파랑 3개를 직접 만들었다" : "궤도 검증 통과";
                receiptNoteUi.text = revealPractice
                    ? "전체 6개 중 파랑 3개"
                    : current.band == 2
                        ? "관찰 파랑 " + current.observedBlue + "/" + current.observedTrials + " · 이론 파랑 " + current.k + "/6"
                        : "경우의 수를 정수 비율로 확인했다";
                receiptImage.color = new Color32(12, 19, 37, 240);
                MgfSfx.Play("correct", .45f);
                MgfSfx.Play("whoosh", .24f);
            }
            else
            {
                receiptUi.text = "궤도가 열리지 않는다";
                receiptNoteUi.text = "파랑 " + CountBlue() + "개 · 흰 " + (6 - CountBlue()) + "개";
                receiptImage.color = new Color32(54, 25, 38, 240);
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
            cartridgeRoot.localRotation = Quaternion.identity;
            dispatchHandle.localPosition = handleHome;
            latchRoot.localRotation = Quaternion.identity;
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
            ShowToast("파랑 3개가 되었다 · 돔 손잡이를 궤도 링까지 끌어 돌리시오", 2.8f);
        }

        Vector3 PracticeWhiteTarget()
        {
            for (int i = 0; i < slots.Length; i++) if (!blueSlots[i]) return slots[i].position;
            return slots[5].position;
        }

        Vector3 BeltGuidePoint() => beltRoot.TransformPoint(new Vector3(0f, -.45f, -1.05f));

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
            if (titleStartUi) titleStartUi.text = "관측 링 기동 중";
        }

        void UpdateTitlePress(Vector2 screen)
        {
            titlePulse += Time.deltaTime;
            if (titleHandle) titleHandle.transform.localScale = Vector3.one * (.94f + Mathf.Sin(titlePulse * 18f) * .015f);
        }

        void EndTitlePress()
        {
            if (titleHandle) titleHandle.transform.localScale = Vector3.one;
            if (titleStartUi) titleStartUi.text = "관측 링을 눌러 시작";
        }

        void ShowEnd(string reason)
        {
            bool clear = reason == "clear";
            endTitleUi.text = clear ? "확률 궤도 완성" : reason == "mastery" ? "관측 완료 · 검증 미달" : "관측 장치 정지";
            endTitleUi.color = clear ? MgfLook.Hex("62F0C8") : MgfLook.Hex("FFAF7A");
            endStatsUi.text = "관측 " + st.solved + "/10\n첫 시도 " + st.firstCorrect + "/10 · 동시 사건 " + st.band3FirstCorrect + "/4\n점수 " + st.score
                + (clear ? "\n숙련 인장 획득" : "\n목표: 첫 시도 8개 · 마지막 단계 3개");
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
