// 싹 건져 — 층리 암반 조수 수조 2.5D 디오라마, UI, 입력 좌표와 피드백.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.SsakGeonjyeo
{
    public partial class SsakGeonjyeoGame
    {
        Camera cam;
        Transform worldRoot, tidePoolRoot, controlRoot, titleRoot;
        GameObject water, capacityHandle, sweepHandle, titleNet;
        readonly GameObject[] crabs = new GameObject[SsakRules.UniverseCount];
        readonly TextMeshPro[] crabLabels = new TextMeshPro[SsakRules.UniverseCount];
        readonly Vector3[] crabHome = new Vector3[SsakRules.UniverseCount];
        readonly Vector3[] revealHome = new Vector3[SsakRules.UniverseCount];
        readonly LineRenderer[] tethers = new LineRenderer[SsakRules.UniverseCount];
        readonly GameObject[] clawSignals = new GameObject[SsakRules.UniverseCount];
        readonly GameObject[] netCells = new GameObject[12];
        readonly GameObject[] outcomeSignals = new GameObject[12];
        readonly GameObject[] knots = new GameObject[3];
        readonly Transform[] seaweed = new Transform[14];
        readonly GameObject[] ripples = new GameObject[8];
        readonly float[] rippleLife = new float[8];
        readonly Vector3[] rippleBase = new Vector3[8];
        int rippleCursor;

        LineRenderer sweepTrail, guideLine;
        GameObject guideRing, ghostFinger;
        TextMeshPro capacityWorld, capacityActionWorld, sweepActionWorld;
        Material waterMat, deepWaterMat, sandMat, shellMat, basaltMat, ivoryMat, coralMat, goldMat, netMat, ropeMat, barnacleMat, mossMat;
        Material shaleMat, ochreStoneMat, wetRockMat, signalMat;
        Material[] crabMats;
        bool practiceLayout, currentLand, currentTabletPortrait, practiceDemoVisible;
        int lastScreenW, lastScreenH, lastTimer = -1, lastScoreShown = -1;
        float displayScore, toastLeft, guideLeft, titlePulse;
        string toastMessage = "";

        GameObject playUiRoot, titleUiRoot, endUiRoot, conditionPanel, headerBand, footerBand;
        TextMeshProUGUI conditionUi, goalUi, hudUi, counterUi, selectedUi, toastUi;
        TextMeshProUGUI titleTagUi, titleMetaUi, titleHintUi, endTitleUi, endStatsUi, endHintUi;
        Image conditionImage, toastImage, endImage;

        Vector3 RailStart => controlRoot.TransformPoint(new Vector3(-3.0f, 0.33f, 0f));
        Vector3 RailEnd => controlRoot.TransformPoint(new Vector3(2.45f, 0.33f, 0f));
        Vector3 SweepHome => controlRoot.TransformPoint(new Vector3(2.82f, 0.42f, 0f));

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("152933"), MgfLook.Hex("88B9B5"), MgfLook.Hex("0B1820"), 0.82f);
            MgfLook.Sun(new Vector3(52f, -32f, -14f), MgfLook.Hex("FFD9A0"), 1.36f, 0.66f);
            cam = MgfLook.Camera(new Vector3(0f, 12.8f, -13.8f), new Vector3(0f, 0f, 0.1f), 38f);
            cam.orthographic = true;
            cam.orthographicSize = 7.25f;
            cam.backgroundColor = MgfLook.Hex("244D55");

            worldRoot = new GameObject("TidePoolWorld").transform;
            tidePoolRoot = new GameObject("StrataTideCutaway").transform;
            tidePoolRoot.SetParent(worldRoot, false);
            deepWaterMat = MgfLook.Lit(MgfLook.Hex("0A4752"), 0.48f, 0.04f, MgfLook.Hex("05222C") * 0.22f);
            waterMat = MgfLook.Alpha(new Color(0.20f, 0.70f, 0.72f, 0.67f), MgfLook.SoftDot);
            sandMat = MgfLook.Lit(MgfLook.Hex("B79362"), 0.10f);
            shellMat = MgfLook.Lit(MgfLook.Hex("EAD7B5"), 0.34f, 0.01f);
            basaltMat = MgfLook.Lit(MgfLook.Hex("253A43"), 0.10f);
            ivoryMat = MgfLook.Lit(MgfLook.Hex("FFF8DF"), 0.42f);
            coralMat = MgfLook.Lit(MgfLook.Hex("D65938"), 0.18f, 0f, MgfLook.Hex("641C10") * 0.12f);
            goldMat = MgfLook.Lit(MgfLook.Hex("F0B945"), 0.38f, 0.08f);
            netMat = MgfLook.Lit(MgfLook.Hex("B8753B"), 0.18f, 0.16f);
            ropeMat = MgfLook.Lit(MgfLook.Hex("77502F"), 0.08f);
            barnacleMat = MgfLook.Lit(MgfLook.Hex("C9B58C"), 0.12f);
            mossMat = MgfLook.Lit(MgfLook.Hex("426B55"), 0.08f);
            shaleMat = MgfLook.Lit(MgfLook.Hex("4F6570"), 0.08f);
            ochreStoneMat = MgfLook.Lit(MgfLook.Hex("9D6B45"), 0.08f);
            wetRockMat = MgfLook.Lit(MgfLook.Hex("182C35"), 0.32f, 0.03f);
            signalMat = MgfLook.Lit(MgfLook.Hex("FFD36B"), 0.54f, 0.04f, MgfLook.Hex("E77525") * 0.52f);
            crabMats = new[] {
                MgfLook.Lit(MgfLook.Hex("8F3A2B"), .12f),
                MgfLook.Lit(MgfLook.Hex("C15334"), .10f),
                MgfLook.Lit(MgfLook.Hex("65363A"), .08f),
                MgfLook.Lit(MgfLook.Hex("A74B36"), .10f)
            };

            MgfLook.Prim(PrimitiveType.Cube, "OchreCutawayBase", new Vector3(0f, -1.04f, .6f), new Vector3(17.4f, .62f, 15.2f), ochreStoneMat, tidePoolRoot, false);
            MgfLook.Prim(PrimitiveType.Cube, "DeepPool", new Vector3(0f, -0.62f, .35f), new Vector3(15.6f, .42f, 13.7f), deepWaterMat, tidePoolRoot, false);
            water = MgfLook.Prim(PrimitiveType.Cube, "RecedingWaterPlane", new Vector3(0f, -0.20f, 0.35f), new Vector3(15.35f, .055f, 13.35f), waterMat, tidePoolRoot, false);
            water.GetComponent<Collider>().enabled = false;
            BuildBasaltRim();
            BuildProps();
            BuildCrabs();
            BuildControls();
            BuildTitleWorld();
            BuildFeedbackObjects();
        }

        void BuildBasaltRim()
        {
            // 수면을 둘러싼 둥근 장난감 테두리 대신, 수면 위·아래가 동시에 읽히는
            // 절단 암반층을 만든다. 층마다 각도·두께가 달라 실물 지질 표본처럼 보인다.
            Material[] layers = { wetRockMat, shaleMat, ochreStoneMat, basaltMat };
            for (int band = 0; band < 4; band++)
            {
                float y = -.80f + band * .27f;
                for (int seg = 0; seg < 7; seg++)
                {
                    float x = -6.45f + seg * 2.15f + ((seg + band) % 2) * .12f;
                    var back = MgfLook.Prim(PrimitiveType.Cube, "RearStratum-" + band + "-" + seg,
                        new Vector3(x, y, 6.35f - band * .10f), new Vector3(2.25f, .24f + (seg % 2) * .05f, 1.38f),
                        layers[(band + seg) % layers.Length], tidePoolRoot, false);
                    back.transform.localRotation = Quaternion.Euler((seg % 3 - 1) * 2f, (seg % 2 == 0 ? -4f : 5f), (seg % 3 - 1) * 2.5f);
                }
            }
            for (int side = -1; side <= 1; side += 2)
            for (int seg = 0; seg < 7; seg++)
            {
                float z = -5.4f + seg * 1.85f;
                var wall = MgfLook.Prim(PrimitiveType.Cube, "SideStratum-" + side + "-" + seg,
                    new Vector3(side * 7.65f, -.42f + (seg % 3) * .08f, z), new Vector3(1.35f, .72f, 2.02f),
                    layers[(seg + (side > 0 ? 1 : 3)) % layers.Length], tidePoolRoot, false);
                wall.transform.localRotation = Quaternion.Euler((seg % 2) * 3f, side * (6f + seg % 3), side * (seg % 3 - 1) * 2f);
            }
            for (int seg = 0; seg < 8; seg++)
            {
                float x = -7.0f + seg * 2f;
                var lip = MgfLook.Prim(PrimitiveType.Cube, "FrontCutFace" + seg,
                    new Vector3(x, -.77f + (seg % 2) * .05f, -6.25f), new Vector3(2.15f, .72f, 1.15f),
                    layers[(seg + 2) % layers.Length], tidePoolRoot, false);
                lip.transform.localRotation = Quaternion.Euler(0f, seg % 2 == 0 ? -4f : 5f, (seg % 3 - 1) * 2f);
            }
            for (int i = 0; i < 12; i++)
            {
                float x = -5.6f + (i % 6) * 2.2f;
                float z = i < 6 ? 5.45f : -5.55f;
                FacetedShell("StrataCap" + i, new Vector3(x, -.18f, z), new Vector3(1.35f, .38f, .88f),
                    i % 3 == 0 ? mossMat : layers[i % layers.Length], tidePoolRoot, 6);
            }
        }

        void BuildProps()
        {
            var green = MgfLook.Lit(MgfLook.Hex("5C8D71"), .22f);
            var green2 = MgfLook.Lit(MgfLook.Hex("76A979"), .18f);
            for (int i = 0; i < seaweed.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (5.2f + (i % 3) * .55f);
                float z = -4.4f + (i / 2) * 1.45f;
                var blade = MgfLook.Prim(PrimitiveType.Cube, "Seaweed" + i, new Vector3(x, .12f, z), new Vector3(.16f, 1.1f + (i % 4) * .16f, .12f), i % 2 == 0 ? green : green2, tidePoolRoot, false);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, side * (8f + i % 4));
                seaweed[i] = blade.transform;
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * 1.77f;
                float r = 5.8f + (i % 3) * .45f;
                Vector3 p = new Vector3(Mathf.Cos(a) * r, -.05f, Mathf.Sin(a) * r + .4f);
                var shell = Rounded("SmallShell" + i, p, new Vector3(.38f, .12f, .28f), .12f, i % 3 == 0 ? coralMat : shellMat, tidePoolRoot);
                shell.transform.localRotation = Quaternion.Euler(0f, i * 31f, 0f);
            }
            for (int i = 0; i < 22; i++)
            {
                float a = i * 2.31f;
                float r = 3.8f + (i % 5) * .62f;
                Vector3 p = new Vector3(Mathf.Cos(a) * r, -.10f, Mathf.Sin(a) * r + .25f);
                Material pebbleMat = i % 4 == 0 ? shellMat : i % 3 == 0 ? barnacleMat : basaltMat;
                var pebble = Rounded("TidePebble" + i, p, new Vector3(.20f + (i % 3) * .08f, .08f, .16f + (i % 2) * .08f), .07f, pebbleMat, tidePoolRoot);
                pebble.transform.localRotation = Quaternion.Euler(0f, i * 47f, 0f);
            }
            // 수면에 놓인 두 겹의 밝은 물결은 깊이 있는 투명 수면층을 읽게 한다.
            for (int i = 0; i < 7; i++)
            {
                var shimmer = Rounded("WaterShimmer" + i, new Vector3(-4.4f + i * 1.48f, -.12f, -4.1f + (i % 2) * .42f), new Vector3(.86f, .012f, .07f), .035f, shellMat, tidePoolRoot);
                shimmer.transform.localRotation = Quaternion.Euler(0f, -12f + i * 4f, 0f);
            }
        }

        void BuildCrabs()
        {
            for (int i = 0; i < crabs.Length; i++)
            {
                int back = SsakRules.BackOfIndex(i), claw = SsakRules.ClawOfIndex(i);
                var root = new GameObject("SignalCrab-" + back + "-" + claw);
                root.transform.SetParent(worldRoot, false);
                crabs[i] = root;
                Material bodyMat = crabMats[(i * 5 + 1) % crabMats.Length];
                var body = FacetedShell("LayeredCarapace", new Vector3(0f, .20f, 0f), new Vector3(1.08f, .52f, .80f), bodyMat, root.transform, 8);
                body.transform.localRotation = Quaternion.Euler(0f, (i % 3 - 1) * 7f, 0f);
                FacetedShell("CarapaceRidge", new Vector3(0f, .48f, .02f), new Vector3(.78f, .12f, .56f), i % 2 == 0 ? ochreStoneMat : barnacleMat, root.transform, 7);
                for (int side = -1; side <= 1; side += 2)
                {
                    var clawObj = FacetedShell("SignalClaw", new Vector3(side * .68f, .25f, -.05f), new Vector3(.36f, .24f, .34f), coralMat, root.transform, 6);
                    clawObj.transform.localRotation = Quaternion.Euler(0f, side * 20f, side * 16f);
                    for (int leg = 0; leg < 3; leg++)
                    {
                        var foot = MgfLook.Prim(PrimitiveType.Cube, "JointedLeg", new Vector3(side * (.46f + leg * .12f), .06f, .18f + leg * .17f), new Vector3(.31f, .07f, .11f), coralMat, root.transform, false);
                        foot.transform.localRotation = Quaternion.Euler(0f, side * (22f + leg * 13f), side * 5f);
                    }
                    FacetedShell("Eye", new Vector3(side * .24f, .57f, -.24f), new Vector3(.13f, .18f, .13f), wetRockMat, root.transform, 6);
                }
                var labelPlate = MgfLook.Block("EtchedNumberSlate", new Vector3(0f, .83f, -.05f), new Vector3(1.34f, .56f, .09f), .035f, ivoryMat, root.transform);
                labelPlate.GetComponent<Collider>().enabled = false;
                crabLabels[i] = MgfText.World("등 " + back + "  집게 " + claw, new Vector3(0f, 0f, -.065f), 1.35f, MgfLook.Hex("354A45"), labelPlate.transform);
                crabLabels[i].alignment = TextAlignmentOptions.Center;
                crabLabels[i].outlineWidth = .08f;
                crabLabels[i].outlineColor = new Color32(255, 248, 225, 220);
                var tether = MakeLine("NetTether" + i, .055f, MgfLook.Hex("EBD997"), 3);
                tether.gameObject.SetActive(false);
                tethers[i] = tether;
                clawSignals[i] = FacetedShell("ClawSignal", new Vector3(.69f, .48f, -.08f), new Vector3(.22f, .10f, .22f), signalMat, root.transform, 6);
                clawSignals[i].SetActive(false);
            }
        }

        void BuildControls()
        {
            controlRoot = new GameObject("ShellNetControls").transform;
            controlRoot.SetParent(worldRoot, false);
            MgfLook.Block("RopeRailShadow", new Vector3(-.25f, .10f, .05f), new Vector3(6.3f, .13f, .34f), .12f, basaltMat, controlRoot).GetComponent<Collider>().enabled = false;
            MgfLook.Block("PearlRail", new Vector3(-.25f, .18f, 0f), new Vector3(6.3f, .14f, .28f), .13f, ropeMat, controlRoot).GetComponent<Collider>().enabled = false;
            for (int i = 0; i < 12; i++)
            {
                float x = -2.75f + i * .47f;
                var cell = new GameObject("NetCell" + (i + 1));
                cell.transform.SetParent(controlRoot, false);
                cell.transform.localPosition = new Vector3(x, .42f, .72f);
                netCells[i] = cell;
                Rounded("RopeTop", new Vector3(0f, .07f, .20f), new Vector3(.42f, .08f, .07f), .025f, ropeMat, cell.transform);
                Rounded("RopeBottom", new Vector3(0f, .07f, -.20f), new Vector3(.42f, .08f, .07f), .025f, ropeMat, cell.transform);
                Rounded("RopeLeft", new Vector3(-.18f, .07f, 0f), new Vector3(.07f, .08f, .43f), .025f, ropeMat, cell.transform);
                Rounded("RopeRight", new Vector3(.18f, .07f, 0f), new Vector3(.07f, .08f, .43f), .025f, ropeMat, cell.transform);
                Rounded("PearlKnot", new Vector3(.18f, .13f, .20f), new Vector3(.11f, .10f, .11f), .04f, netMat, cell.transform);
                outcomeSignals[i] = FacetedShell("OutcomeMarker" + (i + 1), new Vector3(0f, .19f, 0f), new Vector3(.24f, .10f, .24f), signalMat, cell.transform, 6);
                outcomeSignals[i].SetActive(false);
                netCells[i].SetActive(false);
            }
            capacityHandle = Rounded("CapacityShellHandle", new Vector3(-3.0f, .38f, 0f), new Vector3(.95f, .34f, .82f), .2f, shellMat, controlRoot);
            capacityHandle.transform.localRotation = Quaternion.Euler(0f, 0f, -9f);
            for (int ridge = -1; ridge <= 1; ridge++)
                Rounded("ShellRidge" + ridge, new Vector3(-3f + ridge * .18f, .58f, -.02f), new Vector3(.07f, .08f, .52f), .03f, barnacleMat, controlRoot);
            sweepHandle = Rounded("SweepNetHandle", new Vector3(2.82f, .45f, 0f), new Vector3(1.05f, .42f, .9f), .22f, goldMat, controlRoot);
            MgfLook.Prim(PrimitiveType.Cube, "SweepGrip", new Vector3(2.82f, .82f, .05f), new Vector3(.18f, .72f, .18f), ropeMat, controlRoot, false);
            Rounded("SweepShellCap", new Vector3(2.82f, 1.13f, .05f), new Vector3(.45f, .22f, .44f), .11f, shellMat, controlRoot);
            capacityWorld = MgfText.World("그물 0칸", new Vector3(-.15f, 1.10f, 0f), 2.0f, MgfLook.Hex("FFF8DF"), controlRoot);
            capacityWorld.outlineWidth = .12f;
            capacityWorld.outlineColor = new Color32(38, 60, 61, 235);
            capacityActionWorld = MgfText.World("① 조개를 끌어 칸 수", new Vector3(-2.15f, 1.62f, .02f), 1.55f, MgfLook.Hex("FFF8DF"), controlRoot);
            capacityActionWorld.outlineWidth = .12f; capacityActionWorld.outlineColor = new Color32(38, 60, 61, 235);
            sweepActionWorld = MgfText.World("② 여기서 게까지 쓸기", new Vector3(2.05f, 1.62f, .02f), 1.55f, MgfLook.Hex("F1C75B"), controlRoot);
            sweepActionWorld.outlineWidth = .12f; sweepActionWorld.outlineColor = new Color32(38, 60, 61, 235);
            for (int i = 0; i < 3; i++)
                knots[i] = Rounded("NetKnot" + i, new Vector3(2.28f + i * .40f, 1.31f, .72f), new Vector3(.27f, .23f, .27f), .09f, goldMat, controlRoot);
            sweepTrail = MakeLine("SweepTrail", .16f, MgfLook.Hex("F6E4C0"), 48);
            sweepTrail.gameObject.SetActive(false);
        }

        void BuildTitleWorld()
        {
            titleRoot = new GameObject("TitleWorld").transform;
            titleRoot.SetParent(worldRoot, false);
            var left = FacetedShell("TitleShaleLeft", new Vector3(-1.45f, 1.9f, 2.5f), new Vector3(2.55f, .48f, 1.68f), shaleMat, titleRoot, 7);
            var right = FacetedShell("TitleOchreRight", new Vector3(1.45f, 1.7f, 2.35f), new Vector3(2.85f, .48f, 1.78f), ochreStoneMat, titleRoot, 6);
            left.transform.localRotation = Quaternion.Euler(0f, -12f, -5f);
            right.transform.localRotation = Quaternion.Euler(0f, 12f, 5f);
            var t1 = MgfText.World("싹", new Vector3(-1.45f, 2.55f, 1.30f), 12.5f, MgfLook.Hex("FFF2CF"), titleRoot);
            var t2 = MgfText.World("건져", new Vector3(1.45f, 2.35f, 1.18f), 10.8f, MgfLook.Hex("FFB34E"), titleRoot);
            t1.transform.localRotation = Quaternion.Euler(38f, -8f, 0f);
            t2.transform.localRotation = Quaternion.Euler(38f, 8f, 0f);
            t1.outlineWidth = .18f; t1.outlineColor = MgfLook.Hex("182C35");
            t2.outlineWidth = .18f; t2.outlineColor = MgfLook.Hex("182C35");
            titleNet = new GameObject("TitleNet");
            titleNet.transform.SetParent(titleRoot, false);
            titleNet.transform.localPosition = new Vector3(0f, .55f, -3.4f);
            titleNet.transform.localScale = new Vector3(1.32f, 1.05f, 1.32f);
            FacetedShell("TitleCutawayPlinth", new Vector3(0f, -.12f, 0f), new Vector3(5.9f, .38f, 1.25f), wetRockMat, titleNet.transform, 8);
            for (int i = 0; i < 6; i++)
            {
                float x = -2.2f + i * .88f;
                FacetedShell("TitleOutcomeStone" + i, new Vector3(x, .08f, 0f), new Vector3(.58f, .16f, .68f), i % 2 == 0 ? ochreStoneMat : shaleMat, titleNet.transform, 6);
            }
            FacetedShell("TitleSignalHandle", new Vector3(0f, .30f, -.72f), new Vector3(1.5f, .42f, .72f), signalMat, titleNet.transform, 7);
        }

        void BuildFeedbackObjects()
        {
            guideLine = MakeLine("OnboardingPath", .22f, MgfLook.Hex("F1C75B"), 12);
            guideLine.gameObject.SetActive(false);
            guideRing = Rounded("GuideRing", Vector3.zero, new Vector3(1.55f, .035f, 1.55f), .22f, MgfLook.Additive(new Color(1f, .73f, .18f, .94f), MgfLook.SoftDot), worldRoot);
            guideRing.gameObject.SetActive(false);
            ghostFinger = Rounded("GhostFinger", Vector3.zero, new Vector3(.62f, .24f, .62f), .17f, MgfLook.Lit(MgfLook.Hex("FF8B5F"), .68f, 0f, MgfLook.Hex("7E1C0B") * .12f), worldRoot);
            ghostFinger.gameObject.SetActive(false);
            for (int i = 0; i < ripples.Length; i++)
            {
                ripples[i] = Rounded("TapRipple" + i, Vector3.zero, new Vector3(.2f, .015f, .2f), .08f, MgfLook.Alpha(new Color(.95f, 1f, .9f, .62f), MgfLook.SoftDot), worldRoot);
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

        GameObject Rounded(string name, Vector3 position, Vector3 scale, float radius, Material material, Transform parent)
        {
            var go = MgfLook.Block(name, position, scale, radius, material, parent);
            var collider = go.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            return go;
        }

        GameObject FacetedShell(string name, Vector3 position, Vector3 scale, Material material, Transform parent, int sides)
        {
            sides = Mathf.Clamp(sides, 5, 12);
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            var vertices = new Vector3[sides * 2 + 2];
            for (int i = 0; i < sides; i++)
            {
                float a = (i / (float)sides) * Mathf.PI * 2f + Mathf.PI * .5f;
                float x = Mathf.Cos(a) * .5f;
                float z = Mathf.Sin(a) * .5f;
                vertices[i] = new Vector3(x, -.5f, z);
                vertices[sides + i] = new Vector3(x * .91f, .5f, z * .91f);
            }
            int bottom = sides * 2, top = bottom + 1;
            vertices[bottom] = new Vector3(0f, -.5f, 0f);
            vertices[top] = new Vector3(0f, .5f, 0f);
            var triangles = new int[sides * 12];
            int ti = 0;
            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                triangles[ti++] = i; triangles[ti++] = sides + i; triangles[ti++] = sides + n;
                triangles[ti++] = i; triangles[ti++] = sides + n; triangles[ti++] = n;
                triangles[ti++] = bottom; triangles[ti++] = n; triangles[ti++] = i;
                triangles[ti++] = top; triangles[ti++] = sides + i; triangles[ti++] = sides + n;
            }
            var mesh = new Mesh { name = name + "Mesh", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        void BuildUi()
        {
            Canvas canvas = MgfText.Canvas;
            playUiRoot = new GameObject("PlayUI", typeof(RectTransform));
            playUiRoot.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)playUiRoot.transform);
            titleUiRoot = new GameObject("TitleUI", typeof(RectTransform));
            titleUiRoot.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)titleUiRoot.transform);
            endUiRoot = new GameObject("EndUI", typeof(RectTransform));
            endUiRoot.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)endUiRoot.transform);

            headerBand = MakePanel("HeaderBand", playUiRoot.transform, new Color32(20, 52, 55, 214), out _);
            footerBand = MakePanel("FooterBand", playUiRoot.transform, new Color32(20, 52, 55, 214), out _);
            conditionPanel = MakePanel("ConditionCard", playUiRoot.transform, new Color32(247, 243, 220, 242), out conditionImage);
            conditionUi = MakeUiText("", conditionPanel.transform, 19f, MgfLook.Hex("354A45"), TextAlignmentOptions.Center);
            conditionUi.textWrappingMode = TextWrappingModes.Normal;
            goalUi = MakeUiText("경우만큼 그물을 펴고 모두 건져라", playUiRoot.transform, 17f, MgfLook.Hex("F7F3DC"), TextAlignmentOptions.Center);
            goalUi.outlineWidth = .16f; goalUi.outlineColor = new Color32(53, 74, 69, 230);
            hudUi = MakeUiText("", playUiRoot.transform, 15f, MgfLook.Hex("F7F3DC"), TextAlignmentOptions.Center);
            counterUi = MakeUiText("", playUiRoot.transform, 18f, MgfLook.Hex("FFF8DF"), TextAlignmentOptions.Center);
            counterUi.outlineWidth = .16f; counterUi.outlineColor = new Color32(53, 74, 69, 235);
            selectedUi = MakeUiText("", playUiRoot.transform, 15f, MgfLook.Hex("F7F3DC"), TextAlignmentOptions.Center);

            var toastPanel = MakePanel("Toast", playUiRoot.transform, new Color32(53, 74, 69, 232), out toastImage);
            toastUi = MakeUiText("", toastPanel.transform, 16f, Color.white, TextAlignmentOptions.Center);
            toastUi.textWrappingMode = TextWrappingModes.Normal;
            toastPanel.SetActive(false);

            titleTagUi = MakeUiText("경우를 한 번에 건져라", titleUiRoot.transform, 25f, MgfLook.Hex("F7F3DC"), TextAlignmentOptions.Center);
            titleTagUi.outlineWidth = .2f; titleTagUi.outlineColor = new Color32(53, 74, 69, 220);
            titleMetaUi = MakeUiText("중학교 2학년 · 경우의 수", titleUiRoot.transform, 15f, MgfLook.Hex("F7F3DC"), TextAlignmentOptions.Center);
            titleMetaUi.outlineWidth = .2f; titleMetaUi.outlineColor = new Color32(20, 52, 55, 235);
            titleHintUi = MakeUiText("황금 조수 표본을 눌러 건지기 시작", titleUiRoot.transform, 19f, MgfLook.Hex("F7F3DC"), TextAlignmentOptions.Center);
            titleHintUi.outlineWidth = .16f; titleHintUi.outlineColor = new Color32(53, 74, 69, 220);

            var endPanel = MakePanel("ResultShell", endUiRoot.transform, new Color32(247, 243, 220, 247), out endImage);
            endTitleUi = MakeUiText("", endPanel.transform, 38f, MgfLook.Hex("354A45"), TextAlignmentOptions.Center);
            endStatsUi = MakeUiText("", endPanel.transform, 20f, MgfLook.Hex("354A45"), TextAlignmentOptions.Center);
            endStatsUi.textWrappingMode = TextWrappingModes.Normal;
            endHintUi = MakeUiText("웅덩이를 눌러 다시 건지기", endPanel.transform, 18f, MgfLook.Hex("D96948"), TextAlignmentOptions.Center);

            Rect((RectTransform)conditionPanel.transform, new Vector2(.5f, 1f), new Vector2(0f, -95f), new Vector2(366f, 132f));
            Rect((RectTransform)conditionUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(344f, 116f));
            Rect((RectTransform)goalUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -176f), new Vector2(370f, 38f));
            Rect((RectTransform)hudUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -24f), new Vector2(360f, 34f));
            Rect((RectTransform)counterUi.transform, new Vector2(.5f, 0f), new Vector2(0f, 142f), new Vector2(360f, 38f));
            Rect((RectTransform)selectedUi.transform, new Vector2(.5f, 0f), new Vector2(0f, 102f), new Vector2(360f, 34f));
            Rect((RectTransform)toastPanel.transform, new Vector2(.5f, .5f), new Vector2(0f, -32f), new Vector2(346f, 84f));
            Rect((RectTransform)toastUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(326f, 72f));

            Rect((RectTransform)titleTagUi.transform, new Vector2(.5f, .5f), new Vector2(0f, -18f), new Vector2(370f, 55f));
            Rect((RectTransform)titleMetaUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -34f), new Vector2(300f, 36f));
            Rect((RectTransform)titleHintUi.transform, new Vector2(.5f, 0f), new Vector2(0f, 72f), new Vector2(360f, 52f));

            Rect((RectTransform)endPanel.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(348f, 330f));
            Rect((RectTransform)endTitleUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -60f), new Vector2(320f, 68f));
            Rect((RectTransform)endStatsUi.transform, new Vector2(.5f, .5f), new Vector2(0f, 5f), new Vector2(310f, 150f));
            Rect((RectTransform)endHintUi.transform, new Vector2(.5f, 0f), new Vector2(0f, 50f), new Vector2(320f, 56f));
        }

        static GameObject MakePanel(string name, Transform parent, Color color, out Image image)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        static TextMeshProUGUI MakeUiText(string text, Transform parent, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.text = text;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void Band(GameObject band, bool top, float height)
        {
            band.SetActive(height > 0f);
            var rt = (RectTransform)band.transform;
            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.pivot = new Vector2(.5f, top ? 1f : 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);
        }

        static void Rect(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }

        void SetScreen()
        {
            bool title = gamePhase == GamePhase.Title;
            bool end = gamePhase == GamePhase.End;
            titleUiRoot.SetActive(title);
            titleRoot.gameObject.SetActive(title);
            playUiRoot.SetActive(!title && !end);
            endUiRoot.SetActive(end);
            controlRoot.gameObject.SetActive(!title && !end);
            guideRing.SetActive(false); guideLine.gameObject.SetActive(false); ghostFinger.SetActive(false);
            if (title)
            {
                practiceLayout = false;
                PositionCrabsTitle();
            }
            else if (!end)
            {
                practiceLayout = gamePhase == GamePhase.Practice || revealPractice && gamePhase == GamePhase.Reveal;
                PositionCrabsForLayout();
                RefreshProblemUi();
                RefreshCapacityVisual(false);
                RefreshKnots();
            }
            RefreshHud(true);
        }

        void PositionCrabsTitle()
        {
            for (int i = 0; i < crabs.Length; i++)
            {
                bool on = i < 8;
                crabs[i].SetActive(on);
                if (!on) continue;
                float a = i / 8f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * (3.7f + (i % 2) * .5f), .1f, Mathf.Sin(a) * 2.7f - .5f);
                crabHome[i] = p; crabs[i].transform.position = p;
            }
        }

        void PositionCrabsForLayout()
        {
            if (practiceLayout)
            {
                int[] active = { 0, 1, 3, 4 };
                for (int i = 0; i < crabs.Length; i++) crabs[i].SetActive(false);
                for (int s = 0; s < active.Length; s++)
                {
                    int i = active[s]; crabs[i].SetActive(true);
                    float x = (s % 2 == 0 ? -1.25f : 1.25f);
                    float z = (s < 2 ? (currentTabletPortrait ? 1.0f : 1.4f) : (currentTabletPortrait ? -.8f : -1.0f));
                    crabHome[i] = new Vector3(x, .12f, z);
                    crabs[i].transform.position = crabHome[i];
                }
                return;
            }
            for (int i = 0; i < crabs.Length; i++) crabs[i].SetActive(true);
            for (int slot = 0; slot < crabOrder.Length; slot++)
            {
                int i = crabOrder[slot];
                int cols = currentLand || currentTabletPortrait ? 4 : 3;
                int rows = currentLand || currentTabletPortrait ? 3 : 4;
                int c = slot % cols, r = slot / cols;
                float spacingX = currentLand ? 1.82f : currentTabletPortrait ? 1.68f : 2.05f;
                float spacingZ = currentTabletPortrait ? 1.55f : 1.62f;
                float zBias = currentLand ? .35f : currentTabletPortrait ? .05f : .55f;
                float x = (c - (cols - 1) * .5f) * spacingX;
                float z = (rows - 1) * .5f * spacingZ - r * spacingZ + zBias;
                crabHome[i] = new Vector3(x, .12f, z);
                crabs[i].transform.position = crabHome[i];
            }
        }

        void SetPracticeCrabs(bool practice)
        {
            practiceLayout = practice;
            PositionCrabsForLayout();
        }

        bool IsCrabActive(int index) => index >= 0 && index < crabs.Length && crabs[index].activeInHierarchy;
        Vector3 CrabHitCenter(int index) => crabHome[index] + new Vector3(0f, .25f, 0f);

        void ResetWorldForProblem(bool practice)
        {
            EndPracticeDemonstration();
            practiceLayout = practice;
            PositionCrabsForLayout();
            for (int i = 0; i < crabs.Length; i++)
            {
                crabs[i].transform.localScale = Vector3.one;
                tethers[i].gameObject.SetActive(false);
                clawSignals[i].SetActive(false);
                crabLabels[i].color = MgfLook.Hex("354A45");
            }
            for (int i = 0; i < outcomeSignals.Length; i++) outcomeSignals[i].SetActive(false);
            water.transform.localPosition = new Vector3(0f, -.20f, .35f);
            selectedMask = 0;
            BindCurrent();
            sweepTrail.gameObject.SetActive(false);
            RefreshCapacityVisual(false);
        }

        void AnimateWorld(float dt)
        {
            float t = Time.time;
            for (int i = 0; i < seaweed.Length; i++)
                seaweed[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * (1.1f + i * .03f) + i) * (7f + i % 4));
            if (gamePhase != GamePhase.Reveal)
                water.transform.localPosition = new Vector3(0f, -.20f + Mathf.Sin(t * .82f) * .014f, .35f);
            titlePulse += dt;
            if (gamePhase == GamePhase.Title && titleNet)
            {
                float s = 1f + Mathf.Sin(titlePulse * 2.1f) * .045f;
                titleNet.transform.localScale = new Vector3(1.32f * s, 1.05f, 1.32f * s);
            }
            for (int i = 0; i < crabs.Length; i++)
            {
                if (!crabs[i].activeSelf) continue;
                if (gamePhase == GamePhase.Reveal)
                {
                    AnimateRevealCrab(i);
                    continue;
                }
                float bob = Mathf.Sin(t * 2.1f + i * .71f) * .055f;
                crabs[i].transform.position = crabHome[i] + Vector3.up * bob;
                crabs[i].transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 1.25f + i) * 4.5f, 0f);
            }
            for (int i = 0; i < ripples.Length; i++)
            {
                if (rippleLife[i] <= 0f) continue;
                rippleLife[i] -= dt;
                float k = 1f - Mathf.Clamp01(rippleLife[i] / .55f);
                ripples[i].transform.position = rippleBase[i] + Vector3.up * .02f;
                ripples[i].transform.localScale = new Vector3(.25f + k * 1.8f, .015f, .25f + k * 1.8f);
                if (rippleLife[i] <= 0f) ripples[i].SetActive(false);
            }
            if (guideLeft > 0f)
            {
                guideLeft -= dt;
                AnimateGuide(t);
                if (guideLeft <= 0f)
                {
                    guideRing.SetActive(false); guideLine.gameObject.SetActive(false); ghostFinger.SetActive(false);
                    EndPracticeDemonstration();
                }
            }
        }

        void AnimateRevealCrab(int i)
        {
            bool selected = (selectedMask & (1 << i)) != 0;
            if (revealCorrect && selected)
            {
                int rank = 0;
                for (int j = 0; j < i; j++) if ((selectedMask & (1 << j)) != 0) rank++;
                float start = rank * .075f;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((revealClock - start) / .28f));
                crabs[i].transform.position = revealHome[i] + Vector3.up * (Mathf.Sin(k * Mathf.PI) * .10f);
                crabs[i].transform.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * .10f);
                clawSignals[i].SetActive(k > .01f);
                clawSignals[i].transform.localScale = new Vector3(.22f, .10f, .22f) * (1f + Mathf.Sin(k * Mathf.PI) * 1.1f);
                if (rank < outcomeSignals.Length) outcomeSignals[rank].SetActive(k >= .62f);
            }
            else if (!revealCorrect && selected)
            {
                float x = Mathf.Sin(revealClock * 32f + i) * .18f * (1f - Mathf.Clamp01(revealClock));
                crabs[i].transform.position = revealHome[i] + Vector3.right * x;
            }
            else crabs[i].transform.position = revealHome[i];
            if (revealCorrect)
            {
                float drain = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(revealClock / .72f));
                water.transform.localPosition = new Vector3(0f, Mathf.Lerp(-.20f, -.40f, drain), .35f);
            }
        }

        void ApplyResponsiveLayout()
        {
            float aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 1f;
            bool land = aspect >= 1.05f;
            bool tabletPortrait = !land && aspect >= .62f;
            if (Screen.width == lastScreenW && Screen.height == lastScreenH && land == currentLand && tabletPortrait == currentTabletPortrait) return;
            lastScreenW = Screen.width; lastScreenH = Screen.height; currentLand = land; currentTabletPortrait = tabletPortrait;
            cam.orthographicSize = land ? 5.9f : tabletPortrait ? 7.85f : 7.25f;
            cam.transform.position = land ? new Vector3(0f, 12.8f, -13.8f) : new Vector3(0f, 12.8f, -13.8f);
            cam.transform.LookAt(new Vector3(0f, 0f, 0.1f));
            controlRoot.localPosition = land ? new Vector3(5.05f, 0f, .4f) : tabletPortrait ? new Vector3(0f, 0f, -4.55f) : new Vector3(0f, 0f, -3.78f);
            controlRoot.localRotation = Quaternion.identity;
            // 세로 폰은 화면 반폭이 약 3.35라 레일 양 끝 손잡이가 잘렸다. 레일 전체를 화면 안으로 줄인다.
            controlRoot.localScale = Vector3.one * (land ? 1f : tabletPortrait ? .94f : .82f);
            if (!gamePhase.Equals(GamePhase.Title)) PositionCrabsForLayout();
            if (land)
            {
                conditionUi.fontSize = 17f; goalUi.fontSize = 16f;
                Rect((RectTransform)conditionPanel.transform, new Vector2(0f, .5f), new Vector2(134f, 88f), new Vector2(244f, 142f));
                Rect((RectTransform)conditionUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(224f, 126f));
                Rect((RectTransform)goalUi.transform, new Vector2(0f, .5f), new Vector2(134f, -6f), new Vector2(246f, 48f));
                Rect((RectTransform)counterUi.transform, new Vector2(1f, .5f), new Vector2(-142f, 154f), new Vector2(250f, 38f));
                Rect((RectTransform)selectedUi.transform, new Vector2(1f, .5f), new Vector2(-142f, 116f), new Vector2(250f, 34f));
                Rect((RectTransform)toastUi.transform.parent, new Vector2(.5f, .16f), Vector2.zero, new Vector2(346f, 78f));
                Rect((RectTransform)hudUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -22f), new Vector2(420f, 32f));
                Band(headerBand, true, 44f); Band(footerBand, false, 0f);
            }
            else if (tabletPortrait)
            {
                // CanvasScaler는 고DPI 태블릿에서 Screen.width를 물리 픽셀로 보고했다.
                // 가로세로비로 분기해 HUD/조건/게/조작 레일을 네 개의 세로 존에 고정한다.
                // 네 존: 헤더(HUD 0~36 · 조건 카드 40~132 · 목표 134~170) / 게 무대 / 조작 레일 / 푸터(점수·선택 0~120).
                conditionUi.fontSize = 16f; goalUi.fontSize = 15f;
                Rect((RectTransform)hudUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -19f), new Vector2(420f, 30f));
                Rect((RectTransform)conditionPanel.transform, new Vector2(.5f, 1f), new Vector2(0f, -86f), new Vector2(440f, 90f));
                Rect((RectTransform)conditionUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(416f, 80f));
                Rect((RectTransform)goalUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -153f), new Vector2(440f, 36f));
                Rect((RectTransform)counterUi.transform, new Vector2(.5f, 0f), new Vector2(0f, 82f), new Vector2(420f, 32f));
                Rect((RectTransform)selectedUi.transform, new Vector2(.5f, 0f), new Vector2(0f, 48f), new Vector2(420f, 30f));
                Rect((RectTransform)toastUi.transform.parent, new Vector2(.5f, .5f), new Vector2(0f, 40f), new Vector2(410f, 70f));
                Band(headerBand, true, 176f); Band(footerBand, false, 112f);
            }
            else
            {
                conditionUi.fontSize = 19f; goalUi.fontSize = 17f;
                Rect((RectTransform)hudUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -22f), new Vector2(330f, 32f));
                Rect((RectTransform)conditionPanel.transform, new Vector2(.5f, 1f), new Vector2(0f, -104f), new Vector2(366f, 120f));
                Rect((RectTransform)conditionUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(344f, 108f));
                Rect((RectTransform)goalUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -190f), new Vector2(370f, 44f));
                Rect((RectTransform)counterUi.transform, new Vector2(.5f, 0f), new Vector2(0f, 120f), new Vector2(360f, 36f));
                Rect((RectTransform)selectedUi.transform, new Vector2(.5f, 0f), new Vector2(0f, 86f), new Vector2(360f, 32f));
                Band(headerBand, true, 216f); Band(footerBand, false, 150f);
                Rect((RectTransform)toastUi.transform.parent, new Vector2(.5f, .24f), Vector2.zero, new Vector2(346f, 78f));
            }
            if (current != null) RefreshProblemUi();
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            bool complexPrompt = current.prompt.IndexOf('\n') >= 0 || current.prompt.Length > 42;
            conditionUi.fontSize = complexPrompt
                ? (currentTabletPortrait ? 12.5f : currentLand ? 13f : 13.5f)
                : (currentTabletPortrait ? 16f : currentLand ? 17f : 19f);
            conditionUi.text = current.prompt;
            goalUi.text = gamePhase == GamePhase.Practice ? "그물 2칸을 만들고\n등번호 1인 두 게를 한 번에 건지시오" : "경우만큼 그물을 펴고 모두 건져라";
            selectedUi.text = "그물 " + st.capacity + "칸  ·  건진 경우 " + SsakRules.CountBits(selectedMask) + "개";
        }

        void RefreshHud(bool force)
        {
            int timer = gamePhase == GamePhase.Playing ? Mathf.CeilToInt(runLeft) : st.remainingSec;
            int shownScore = Mathf.RoundToInt(displayScore);
            if (!force && timer == lastTimer && shownScore == lastScoreShown) return;
            lastTimer = timer; lastScoreShown = shownScore;
            hudUi.text = "매듭 " + new string('●', Mathf.Max(0, st.lives)) + new string('○', Mathf.Max(0, 3 - st.lives)) + "   조수 " + Mathf.Max(1, st.tide) + "/7   " + timer + "초";
            counterUi.text = "점수 " + shownScore + "   첫 시도 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal;
        }

        void UpdateUi(float dt)
        {
            if (displayScore != st.score)
            {
                displayScore = Mathf.MoveTowards(displayScore, st.score, Mathf.Max(40f, Mathf.Abs(st.score - displayScore) * 5f) * dt);
                if (Mathf.Abs(displayScore - st.score) < .5f) displayScore = st.score;
            }
            RefreshHud(false);
            if (selectedUi && playUiRoot.activeSelf)
                selectedUi.text = "그물 " + st.capacity + "칸  ·  건진 경우 " + SsakRules.CountBits(selectedMask) + "개";
            if (toastLeft > 0f)
            {
                toastLeft -= dt;
                if (toastLeft <= 0f) toastUi.transform.parent.gameObject.SetActive(false);
            }
        }

        void RefreshCapacityVisual(bool animate)
        {
            if (!controlRoot) return;
            for (int i = 0; i < netCells.Length; i++) netCells[i].SetActive(i < st.capacity);
            float t = st.capacity <= 0 ? -.09f : (st.capacity - 1) / 11f;
            Vector3 a = new Vector3(-3f, .38f, 0f), b = new Vector3(2.45f, .38f, 0f);
            capacityHandle.transform.localPosition = Vector3.Lerp(a, b, Mathf.Clamp01(t));
            capacityWorld.text = "그물 " + st.capacity + "칸";
            bool capacityStep = gamePhase == GamePhase.Practice && st.capacity != 2;
            capacityActionWorld.color = capacityStep ? MgfLook.Hex("F1C75B") : MgfLook.Hex("FFF8DF");
            sweepActionWorld.color = gamePhase == GamePhase.Practice && st.capacity == 2 ? MgfLook.Hex("F1C75B") : MgfLook.Hex("9FB7AC");
            if (animate)
            {
                MgfFx.Punch(capacityHandle.transform, .13f, .16f);
                MgfSfx.Play("tap", .09f);
            }
        }

        void RefreshKnots()
        {
            for (int i = 0; i < knots.Length; i++) knots[i].SetActive(i < st.lives);
        }

        void CaptureCrab(int index, int order)
        {
            MgfFx.Punch(crabs[index].transform, .22f, .22f);
            clawSignals[index].SetActive(true);
            clawSignals[index].transform.localScale = new Vector3(.30f, .14f, .30f);
            MgfFx.Punch(clawSignals[index].transform, .12f, .16f);
        }

        void BeginRevealVisual(bool correct)
        {
            for (int i = 0; i < crabs.Length; i++)
            {
                revealHome[i] = crabs[i].transform.position;
                tethers[i].gameObject.SetActive(false);
                clawSignals[i].SetActive(false);
            }
            for (int i = 0; i < outcomeSignals.Length; i++) outcomeSignals[i].SetActive(false);
            guideRing.SetActive(false); guideLine.gameObject.SetActive(false); ghostFinger.SetActive(false);
            if (correct)
            {
                MgfSfx.Play("correct", .52f);
                MgfFx.Burst(new Vector3(0f, .55f, .35f), MgfLook.Hex("D65938"), 14 + st.combo * 4, 2.8f, .16f);
                MgfFx.Glow(new Vector3(0f, .48f, .35f), MgfLook.Hex("FFD36B"), 14 + st.combo * 2, .38f);
                MgfFx.Punch(counterUi.transform, .2f, .34f);
                ShowToast(current.reveal, 1.5f);
            }
            else
            {
                MgfSfx.Play("wrong", .48f);
                MgfFx.Shake(cam, .095f, .24f);
                RefreshKnots();
                ShowToast(WrongText(st.misconceptionId), 1.4f);
                for (int i = 0; i < crabs.Length; i++)
                    if ((current.answerMask & (1 << i)) != 0 && (selectedMask & (1 << i)) == 0)
                        crabLabels[i].color = MgfLook.Hex("E4BE58");
            }
        }

        void CompleteRevealVisual(bool correct)
        {
            for (int i = 0; i < crabs.Length; i++)
            {
                crabs[i].transform.position = revealHome[i];
                crabs[i].transform.localScale = Vector3.one;
                tethers[i].gameObject.SetActive(false);
                clawSignals[i].SetActive(false);
                crabLabels[i].color = MgfLook.Hex("354A45");
            }
            for (int i = 0; i < outcomeSignals.Length; i++) outcomeSignals[i].SetActive(false);
            water.transform.localPosition = new Vector3(0f, -.20f, .35f);
            sweepTrail.gameObject.SetActive(false);
        }

        string WrongText(string id)
        {
            switch (id)
            {
                case "sum_as_product": return "‘또는’인 두 사건은 겹치지 않을 때 더하시오";
                case "product_as_sum": return "두 표식을 각각 고르면 경우의 수를 곱하시오";
                case "unordered_pair_roles": return "등번호와 집게번호의 역할을 구별하시오";
                case "one_axis_only": return "다른 번호와 짝이 되는 경우도 빠짐없이 확인하시오";
                case "right_count_wrong_set": return "수는 맞지만 빠진 경우와 잘못 넣은 경우가 있다";
                case "extra_case": return "조건에 없는 번호 게가 그물에 들어왔다";
                case "missing_case": return "조건에 맞는 번호 게가 아직 물에 남아 있다";
                case "net_too_small": return "그물 칸이 경우의 수보다 적다";
                default: return "그물 칸이 경우의 수보다 많다";
            }
        }

        void ShowWrongReason(string id)
        {
            ShowToast(WrongText(id) + " · 같은 조수를 다시 구성하시오", 3.0f);
            ShowGuide(2.6f);
        }

        void ShowToast(string message, float seconds = 2.2f)
        {
            toastMessage = message;
            toastLeft = seconds;
            toastUi.text = message;
            toastUi.transform.parent.gameObject.SetActive(true);
            MgfFx.Punch(toastUi.transform.parent, .05f, .18f);
        }

        void RefuseInput(string message)
        {
            ShowToast(message, 1.35f);
            MgfSfx.Play("wrong", .12f);
            ShowGuide(1.6f);
        }

        void ShowConceptBridge(string message)
        {
            ShowToast(message, 2.4f);
            MgfSfx.Play("whoosh", .25f);
        }

        void ShowEnd(string reason)
        {
            bool clear = reason == "clear";
            endTitleUi.text = clear ? "조수 도감 완성" : reason == "time" ? "조수가 빠졌다" : reason == "knots" ? "매듭이 모두 끊겼다" : "첫 시도를 더 정확하게";
            endStatsUi.text = "해결한 조수  " + st.solved + "/7\n첫 시도 공동 정답  " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal + "\n최종 점수  " + st.score;
            endHintUi.text = "웅덩이를 눌러 다시 건지기";
            displayScore = 0f;
            MgfFx.Punch(endUiRoot.transform, .08f, .42f);
        }

        void BeginTitlePress() { MgfFx.Punch(titleNet.transform, .12f, .26f); }
        void UpdateTitlePress(Vector2 screen) { }
        void EndTitlePress() { MgfFx.Burst(titleNet.transform.position + Vector3.up, MgfLook.Hex("F7F3DC"), 28, 3f, .18f); }

        void BeginCapacityDrag() { capacityHandle.transform.localScale = new Vector3(1.12f, .82f, 1.12f); }
        void EndCapacityDrag() { capacityHandle.transform.localScale = Vector3.one; MgfSfx.Play("pop", .16f); }
        void BeginSweep() { sweepHandle.transform.localScale = new Vector3(1.18f, .82f, 1.18f); sweepTrail.gameObject.SetActive(true); }
        void EndSweep() { sweepHandle.transform.localScale = Vector3.one; MgfSfx.Play("whoosh", .22f); }

        void UpdateSweepTrail(Vector3[] points, int count)
        {
            sweepTrail.gameObject.SetActive(true);
            sweepTrail.positionCount = count + 1;
            sweepTrail.SetPosition(0, SweepHome + Vector3.up * .2f);
            for (int i = 0; i < count; i++) sweepTrail.SetPosition(i + 1, points[i] + Vector3.up * .24f);
        }

        void ShakeCapacityHandle() { MgfFx.Punch(capacityHandle.transform, .12f, .18f); }
        void PulseCrabLabel(int index) { MgfFx.Punch(crabLabels[index].transform.parent, .14f, .22f); }

        void SpawnTapRipple(Vector2 screen)
        {
            if (!ScreenToWater(screen, out Vector3 p)) return;
            int i = rippleCursor++ % ripples.Length;
            rippleBase[i] = p;
            rippleLife[i] = .55f;
            ripples[i].transform.position = p;
            ripples[i].SetActive(true);
        }

        bool ScreenToWater(Vector2 screen, out Vector3 point)
        {
            Ray r = cam.ScreenPointToRay(screen);
            Plane plane = new Plane(Vector3.up, new Vector3(0f, .12f, 0f));
            if (plane.Raycast(r, out float d)) { point = r.GetPoint(d); return true; }
            point = Vector3.zero; return false;
        }

        int CapacityFromPointer(Vector2 screen)
        {
            Vector2 a = cam.WorldToScreenPoint(RailStart), b = cam.WorldToScreenPoint(RailEnd);
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < 1f ? 0f : Mathf.Clamp01(Vector2.Dot(screen - a, ab) / ab.sqrMagnitude);
            return 1 + Mathf.RoundToInt(t * 11f);
        }

        float UiPx(float units) => units * Mathf.Max(1f, MgfText.Canvas.scaleFactor);
        bool IsCapacityHandle(Vector2 screen) => Vector2.Distance(screen, cam.WorldToScreenPoint(capacityHandle.transform.position)) <= Mathf.Max(68f, UiPx(46f));
        bool IsSweepHandle(Vector2 screen) => Vector2.Distance(screen, cam.WorldToScreenPoint(SweepHome)) <= Mathf.Max(72f, UiPx(48f));
        bool IsPracticeControlZone(Vector2 screen)
        {
            // 연습은 화면 아래쪽 전체가 조개 레일의 넓은 드래그 시작 영역이다.
            // 탭만으로는 Game.cs의 이동 거리 게이트를 넘지 않아 자동 정답이 되지 않는다.
            float h = Mathf.Max(1f, Screen.height);
            return screen.y >= h * .12f && screen.y <= h * .62f;
        }

        int CrabAtScreen(Vector2 screen)
        {
            int best = -1; float dBest = 48f;
            for (int i = 0; i < crabs.Length; i++)
            {
                if (!IsCrabActive(i)) continue;
                float d = Vector2.Distance(screen, cam.WorldToScreenPoint(CrabHitCenter(i)));
                if (d < dBest) { best = i; dBest = d; }
            }
            return best;
        }

        void ReplayGuide(bool enlarged)
        {
            guideLeft = enlarged ? 4.6f : 3.6f;
            guideRing.SetActive(true); guideLine.gameObject.SetActive(true); ghostFinger.SetActive(true);
            ShowGuide(guideLeft);
            ShowToast(st.capacity == 2 ? "② 노란 시작점에서 등번호 1인 두 게까지 드래그" : "① 밝은 조개 레일을 옆으로 끌어 2칸", 2.6f);
        }

        void AdvancePracticeGuideToSweep()
        {
            guideLeft = 4.4f;
            RefreshCapacityVisual(false);
            ShowGuide(guideLeft);
            ShowToast("② 같은 레일의 노란 시작점에서 두 게까지 드래그", 2.8f);
        }

        void ShowPracticeSweepDemonstration()
        {
            practiceDemoVisible = true;
            guideLeft = 5.8f;
            guideRing.SetActive(true); guideLine.gameObject.SetActive(true); ghostFinger.SetActive(true);
            crabLabels[0].color = MgfLook.Hex("E4BE58");
            crabLabels[1].color = MgfLook.Hex("E4BE58");
            ShowToast("시범 경로만 표시했다 · 노란 시작점에서 (1,1), (1,2)를 직접 지나 놓으시오", 4.8f);
            MgfSfx.Play("whoosh", .22f);
        }

        void EndPracticeDemonstration()
        {
            if (!practiceDemoVisible) return;
            practiceDemoVisible = false;
            guideLeft = 0f;
            if (guideRing != null) guideRing.SetActive(false);
            if (guideLine != null) guideLine.gameObject.SetActive(false);
            if (ghostFinger != null) ghostFinger.SetActive(false);
            if (crabLabels == null) return;
            if (crabLabels.Length > 0 && crabLabels[0] != null) crabLabels[0].color = MgfLook.Hex("354A45");
            if (crabLabels.Length > 1 && crabLabels[1] != null) crabLabels[1].color = MgfLook.Hex("354A45");
        }

        void ShowGuide(float seconds)
        {
            guideLeft = Mathf.Max(guideLeft, seconds);
            guideRing.SetActive(true); guideLine.gameObject.SetActive(true); ghostFinger.SetActive(true);
        }

        void AnimateGuide(float t)
        {
            bool sweep = st.capacity == 2 && gamePhase == GamePhase.Practice || gamePhase == GamePhase.Playing && st.capacity > 0;
            if (!sweep)
            {
                Vector3 from = capacityHandle.transform.position + Vector3.up * .18f;
                Vector3 localTwo = Vector3.Lerp(new Vector3(-3f, .38f, 0f), new Vector3(2.45f, .38f, 0f), 1f / 11f);
                Vector3 to = controlRoot.TransformPoint(localTwo) + Vector3.up * .18f;
                float k = Mathf.PingPong(t * .85f, 1f);
                guideRing.transform.position = from;
                guideRing.transform.localScale = Vector3.one * (1.05f + Mathf.Sin(t * 5f) * .16f);
                ghostFinger.transform.position = Vector3.Lerp(from, to, k);
                guideLine.positionCount = 2; guideLine.SetPosition(0, from); guideLine.SetPosition(1, to);
            }
            else
            {
                Vector3 from = SweepHome + Vector3.up * .2f;
                Vector3 p1 = practiceLayout ? CrabHitCenter(0) : CrabHitCenter(crabOrder[0]);
                Vector3 p2 = practiceLayout ? CrabHitCenter(1) : CrabHitCenter(crabOrder[1]);
                float k = Mathf.Repeat(t * .46f, 1f);
                Vector3 pos = k < .5f ? Vector3.Lerp(from, p1, k * 2f) : Vector3.Lerp(p1, p2, (k - .5f) * 2f);
                guideRing.transform.position = from;
                guideRing.transform.localScale = Vector3.one * (1.05f + Mathf.Sin(t * 5f) * .16f);
                ghostFinger.transform.position = pos;
                guideLine.positionCount = 3; guideLine.SetPosition(0, from); guideLine.SetPosition(1, p1); guideLine.SetPosition(2, p2);
            }
        }
    }
}
