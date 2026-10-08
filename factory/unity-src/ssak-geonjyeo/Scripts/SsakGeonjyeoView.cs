// 싹 건져 — 옥빛 조수 웅덩이 2.5D 디오라마, UI, 입력 좌표와 피드백.
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
        readonly GameObject[] netCells = new GameObject[12];
        readonly GameObject[] knots = new GameObject[3];
        readonly Transform[] seaweed = new Transform[14];
        readonly GameObject[] ripples = new GameObject[8];
        readonly float[] rippleLife = new float[8];
        readonly Vector3[] rippleBase = new Vector3[8];
        int rippleCursor;

        LineRenderer sweepTrail, guideLine;
        GameObject guideRing, ghostFinger;
        TextMeshPro capacityWorld;
        Material waterMat, shellMat, basaltMat, ivoryMat, coralMat, goldMat, netMat;
        Material[] crabMats;
        bool practiceLayout, currentLand;
        int lastScreenW, lastScreenH, lastTimer = -1, lastScoreShown = -1;
        float displayScore, toastLeft, guideLeft, titlePulse;
        string toastMessage = "";

        GameObject playUiRoot, titleUiRoot, endUiRoot, conditionPanel;
        TextMeshProUGUI conditionUi, goalUi, hudUi, counterUi, selectedUi, toastUi;
        TextMeshProUGUI titleTagUi, titleMetaUi, titleHintUi, endTitleUi, endStatsUi, endHintUi;
        Image conditionImage, toastImage, endImage;

        Vector3 RailStart => controlRoot.TransformPoint(new Vector3(-3.0f, 0.33f, 0f));
        Vector3 RailEnd => controlRoot.TransformPoint(new Vector3(2.45f, 0.33f, 0f));
        Vector3 SweepHome => controlRoot.TransformPoint(new Vector3(3.55f, 0.42f, 0f));

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("9EDCCB"), MgfLook.Hex("DDF0D5"), MgfLook.Hex("506B62"), 0.88f);
            MgfLook.Sun(new Vector3(48f, -28f, -18f), MgfLook.Hex("FFF2D2"), 1.16f, 0.62f);
            cam = MgfLook.Camera(new Vector3(0f, 12.8f, -13.8f), new Vector3(0f, 0f, 0.1f), 38f);
            cam.orthographic = true;
            cam.orthographicSize = 7.25f;
            cam.backgroundColor = MgfLook.Hex("8BCBB9");

            worldRoot = new GameObject("TidePoolWorld").transform;
            tidePoolRoot = new GameObject("LivingTidePool").transform;
            tidePoolRoot.SetParent(worldRoot, false);
            waterMat = MgfLook.Lit(MgfLook.Hex("8BCBB9"), 0.72f, 0.02f, MgfLook.Hex("173E36") * 0.08f);
            shellMat = MgfLook.Lit(MgfLook.Hex("F7F3DC"), 0.76f, 0.02f);
            basaltMat = MgfLook.Lit(MgfLook.Hex("354A45"), 0.16f);
            ivoryMat = MgfLook.Lit(MgfLook.Hex("FFF8DF"), 0.42f);
            coralMat = MgfLook.Lit(MgfLook.Hex("D96948"), 0.38f, 0f, MgfLook.Hex("4B1108") * 0.12f);
            goldMat = MgfLook.Lit(MgfLook.Hex("E4BE58"), 0.62f, 0.12f);
            netMat = MgfLook.Lit(MgfLook.Hex("EAD79A"), 0.3f);
            crabMats = new[] {
                MgfLook.Lit(MgfLook.Hex("C97861"), .35f),
                MgfLook.Lit(MgfLook.Hex("E49B68"), .35f),
                MgfLook.Lit(MgfLook.Hex("B86F68"), .35f),
                MgfLook.Lit(MgfLook.Hex("D78B75"), .35f)
            };

            water = MgfLook.Block("SeaGlassWater", new Vector3(0f, -0.55f, 0.4f), new Vector3(18f, 0.65f, 16f), 1.8f, waterMat, tidePoolRoot);
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
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                float x = Mathf.Cos(a) * 8.25f;
                float z = Mathf.Sin(a) * 7.2f + 0.4f;
                float sx = 1.5f + (i % 3) * .23f;
                float sz = 1.15f + ((i + 1) % 4) * .13f;
                var rock = MgfLook.Block("RoundedBasalt" + i, new Vector3(x, -0.46f + (i % 2) * .05f, z), new Vector3(sx, .72f, sz), .34f, basaltMat, tidePoolRoot);
                rock.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 17f, (i % 3 - 1) * 3f);
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
        }

        void BuildCrabs()
        {
            for (int i = 0; i < crabs.Length; i++)
            {
                int back = SsakRules.BackOfIndex(i), claw = SsakRules.ClawOfIndex(i);
                var root = new GameObject("Crab-" + back + "-" + claw);
                root.transform.SetParent(worldRoot, false);
                crabs[i] = root;
                Material bodyMat = crabMats[(i * 5 + 1) % crabMats.Length];
                var body = Rounded("PebbleShell", new Vector3(0f, .18f, 0f), new Vector3(1.05f, .5f, .78f), .24f, bodyMat, root.transform);
                body.transform.localRotation = Quaternion.Euler(0f, (i % 3 - 1) * 7f, 0f);
                for (int side = -1; side <= 1; side += 2)
                {
                    var clawObj = Rounded("Claw", new Vector3(side * .68f, .25f, -.05f), new Vector3(.34f, .22f, .32f), .11f, coralMat, root.transform);
                    clawObj.transform.localRotation = Quaternion.Euler(0f, side * 20f, side * 16f);
                    Rounded("Eye", new Vector3(side * .24f, .56f, -.24f), new Vector3(.13f, .17f, .13f), .055f, basaltMat, root.transform);
                }
                var labelPlate = MgfLook.Block("IvoryNumberPlate", new Vector3(0f, .83f, -.05f), new Vector3(1.36f, .58f, .09f), .18f, ivoryMat, root.transform);
                labelPlate.GetComponent<Collider>().enabled = false;
                crabLabels[i] = MgfText.World("등 " + back + "  집게 " + claw, new Vector3(0f, 0f, -.065f), 1.35f, MgfLook.Hex("354A45"), labelPlate.transform);
                crabLabels[i].alignment = TextAlignmentOptions.Center;
                crabLabels[i].outlineWidth = .08f;
                crabLabels[i].outlineColor = new Color32(255, 248, 225, 220);
                var tether = MakeLine("NetTether" + i, .055f, MgfLook.Hex("EBD997"), 3);
                tether.gameObject.SetActive(false);
                tethers[i] = tether;
            }
        }

        void BuildControls()
        {
            controlRoot = new GameObject("ShellNetControls").transform;
            controlRoot.SetParent(worldRoot, false);
            MgfLook.Block("PearlRail", new Vector3(-.25f, .12f, 0f), new Vector3(6.3f, .17f, .38f), .16f, shellMat, controlRoot).GetComponent<Collider>().enabled = false;
            for (int i = 0; i < 12; i++)
            {
                float x = -2.75f + i * .47f;
                netCells[i] = MgfLook.Block("NetCell" + (i + 1), new Vector3(x, .42f, .72f), new Vector3(.38f, .16f, .48f), .09f, netMat, controlRoot);
                netCells[i].GetComponent<Collider>().enabled = false;
                netCells[i].SetActive(false);
            }
            capacityHandle = Rounded("CapacityShellHandle", new Vector3(-3.0f, .38f, 0f), new Vector3(.95f, .34f, .82f), .2f, shellMat, controlRoot);
            capacityHandle.transform.localRotation = Quaternion.Euler(0f, 0f, -9f);
            sweepHandle = Rounded("SweepNetHandle", new Vector3(3.55f, .45f, 0f), new Vector3(1.05f, .42f, .9f), .22f, goldMat, controlRoot);
            MgfLook.Prim(PrimitiveType.Cube, "SweepGrip", new Vector3(3.55f, .82f, .05f), new Vector3(.18f, .72f, .18f), basaltMat, controlRoot, false);
            capacityWorld = MgfText.World("그물 0칸", new Vector3(-.15f, 1.12f, 0f), 1.65f, MgfLook.Hex("354A45"), controlRoot);
            capacityWorld.outlineWidth = .12f;
            capacityWorld.outlineColor = new Color32(247, 243, 220, 230);
            for (int i = 0; i < 3; i++)
                knots[i] = Rounded("NetKnot" + i, new Vector3(2.7f + i * .48f, 1.25f, .75f), new Vector3(.32f, .26f, .32f), .1f, goldMat, controlRoot);
            sweepTrail = MakeLine("SweepTrail", .13f, MgfLook.Hex("F7F3DC"), 48);
            sweepTrail.gameObject.SetActive(false);
        }

        void BuildTitleWorld()
        {
            titleRoot = new GameObject("TitleWorld").transform;
            titleRoot.SetParent(worldRoot, false);
            var left = Rounded("TitleShellLeft", new Vector3(-1.45f, 1.9f, 2.5f), new Vector3(2.5f, .45f, 1.62f), .34f, shellMat, titleRoot);
            var right = Rounded("TitleShellRight", new Vector3(1.45f, 1.7f, 2.35f), new Vector3(2.8f, .45f, 1.72f), .34f, shellMat, titleRoot);
            left.transform.localRotation = Quaternion.Euler(0f, -12f, -5f);
            right.transform.localRotation = Quaternion.Euler(0f, 12f, 5f);
            var t1 = MgfText.World("싹", new Vector3(-1.45f, 2.18f, 1.92f), 12.5f, MgfLook.Hex("354A45"), titleRoot);
            var t2 = MgfText.World("건져", new Vector3(1.45f, 1.98f, 1.78f), 10.8f, MgfLook.Hex("D96948"), titleRoot);
            t1.outlineWidth = .18f; t1.outlineColor = MgfLook.Hex("F7F3DC");
            t2.outlineWidth = .18f; t2.outlineColor = MgfLook.Hex("F7F3DC");
            titleNet = new GameObject("TitleNet");
            titleNet.transform.SetParent(titleRoot, false);
            titleNet.transform.localPosition = new Vector3(0f, .55f, -3.4f);
            titleNet.transform.localScale = new Vector3(1.32f, 1.05f, 1.32f);
            for (int i = 0; i < 6; i++)
            {
                float x = -2.2f + i * .88f;
                MgfLook.Block("TitleNetCell" + i, new Vector3(x, 0f, 0f), new Vector3(.68f, .12f, .82f), .14f, netMat, titleNet.transform).GetComponent<Collider>().enabled = false;
            }
            Rounded("TitlePearlHandle", new Vector3(0f, .28f, -.72f), new Vector3(1.5f, .4f, .72f), .22f, shellMat, titleNet.transform);
        }

        void BuildFeedbackObjects()
        {
            guideLine = MakeLine("OnboardingPath", .1f, MgfLook.Hex("E4BE58"), 12);
            guideLine.gameObject.SetActive(false);
            guideRing = Rounded("GuideRing", Vector3.zero, new Vector3(1.15f, .025f, 1.15f), .18f, MgfLook.Additive(new Color(1f, .82f, .28f, .85f), MgfLook.SoftDot), worldRoot);
            guideRing.gameObject.SetActive(false);
            ghostFinger = Rounded("GhostFinger", Vector3.zero, new Vector3(.48f, .18f, .48f), .14f, MgfLook.Lit(MgfLook.Hex("FFF8DF"), .75f), worldRoot);
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

            conditionPanel = MakePanel("ConditionCard", playUiRoot.transform, new Color32(247, 243, 220, 242), out conditionImage);
            conditionUi = MakeUiText("", conditionPanel.transform, 19f, MgfLook.Hex("354A45"), TextAlignmentOptions.Center);
            conditionUi.textWrappingMode = TextWrappingModes.Normal;
            goalUi = MakeUiText("경우만큼 그물을 펴고 모두 건져라", playUiRoot.transform, 17f, MgfLook.Hex("F7F3DC"), TextAlignmentOptions.Center);
            goalUi.outlineWidth = .16f; goalUi.outlineColor = new Color32(53, 74, 69, 230);
            hudUi = MakeUiText("", playUiRoot.transform, 15f, MgfLook.Hex("354A45"), TextAlignmentOptions.Center);
            counterUi = MakeUiText("", playUiRoot.transform, 18f, MgfLook.Hex("FFF8DF"), TextAlignmentOptions.Center);
            counterUi.outlineWidth = .16f; counterUi.outlineColor = new Color32(53, 74, 69, 235);
            selectedUi = MakeUiText("", playUiRoot.transform, 15f, MgfLook.Hex("F7F3DC"), TextAlignmentOptions.Center);

            var toastPanel = MakePanel("Toast", playUiRoot.transform, new Color32(53, 74, 69, 232), out toastImage);
            toastUi = MakeUiText("", toastPanel.transform, 16f, Color.white, TextAlignmentOptions.Center);
            toastUi.textWrappingMode = TextWrappingModes.Normal;
            toastPanel.SetActive(false);

            titleTagUi = MakeUiText("경우를 한 번에 건져라", titleUiRoot.transform, 25f, MgfLook.Hex("F7F3DC"), TextAlignmentOptions.Center);
            titleTagUi.outlineWidth = .2f; titleTagUi.outlineColor = new Color32(53, 74, 69, 220);
            titleMetaUi = MakeUiText("중학교 2학년 · 경우의 수", titleUiRoot.transform, 14f, MgfLook.Hex("354A45"), TextAlignmentOptions.Center);
            titleHintUi = MakeUiText("조개 그물을 눌러 건지기 시작", titleUiRoot.transform, 19f, MgfLook.Hex("F7F3DC"), TextAlignmentOptions.Center);
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
                    float z = (s < 2 ? 1.4f : -1.0f);
                    crabHome[i] = new Vector3(x, .12f, z);
                    crabs[i].transform.position = crabHome[i];
                }
                return;
            }
            for (int i = 0; i < crabs.Length; i++) crabs[i].SetActive(true);
            for (int slot = 0; slot < crabOrder.Length; slot++)
            {
                int i = crabOrder[slot];
                int cols = currentLand ? 4 : 3;
                int rows = currentLand ? 3 : 4;
                int c = slot % cols, r = slot / cols;
                float x = (c - (cols - 1) * .5f) * (currentLand ? 1.82f : 2.05f);
                float z = (rows - 1) * .5f * 1.62f - r * 1.62f + (currentLand ? .35f : .55f);
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
            practiceLayout = practice;
            PositionCrabsForLayout();
            for (int i = 0; i < crabs.Length; i++)
            {
                if (!crabs[i].activeSelf) continue;
                crabs[i].transform.localScale = Vector3.one;
                tethers[i].gameObject.SetActive(false);
                crabLabels[i].color = MgfLook.Hex("354A45");
            }
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
                if (guideLeft <= 0f) { guideRing.SetActive(false); guideLine.gameObject.SetActive(false); ghostFinger.SetActive(false); }
            }
        }

        void AnimateRevealCrab(int i)
        {
            bool selected = (selectedMask & (1 << i)) != 0;
            if (revealCorrect && selected)
            {
                int rank = 0;
                for (int j = 0; j < i; j++) if ((selectedMask & (1 << j)) != 0) rank++;
                float span = Mathf.Min(6.2f, current.answerCount * 1.0f);
                Vector3 target = controlRoot.TransformPoint(new Vector3(-span * .5f + .5f + rank * (span / Mathf.Max(1, current.answerCount - 1)), 1.9f, .5f));
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(revealClock / .72f));
                Vector3 p = Vector3.Lerp(revealHome[i], target, k);
                if (revealClock > 1.04f) p += Vector3.down * Mathf.Pow((revealClock - 1.04f) * 3f, 2f);
                crabs[i].transform.position = p;
                crabs[i].transform.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * .18f);
            }
            else if (!revealCorrect && selected)
            {
                float x = Mathf.Sin(revealClock * 32f + i) * .18f * (1f - Mathf.Clamp01(revealClock));
                crabs[i].transform.position = revealHome[i] + Vector3.right * x;
            }
            else crabs[i].transform.position = revealHome[i];
        }

        void ApplyResponsiveLayout()
        {
            bool land = Screen.width >= 1024;
            if (Screen.width == lastScreenW && Screen.height == lastScreenH && land == currentLand) return;
            lastScreenW = Screen.width; lastScreenH = Screen.height; currentLand = land;
            cam.orthographicSize = land ? 5.9f : 7.25f;
            cam.transform.position = land ? new Vector3(0f, 12.8f, -13.8f) : new Vector3(0f, 12.8f, -13.8f);
            cam.transform.LookAt(new Vector3(0f, 0f, 0.1f));
            controlRoot.localPosition = land ? new Vector3(5.05f, 0f, .4f) : new Vector3(0f, 0f, -3.78f);
            controlRoot.localRotation = Quaternion.identity;
            if (!gamePhase.Equals(GamePhase.Title)) PositionCrabsForLayout();
            if (land)
            {
                Rect((RectTransform)conditionPanel.transform, new Vector2(0f, .5f), new Vector2(134f, 88f), new Vector2(244f, 142f));
                Rect((RectTransform)conditionUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(224f, 126f));
                Rect((RectTransform)goalUi.transform, new Vector2(0f, .5f), new Vector2(134f, -6f), new Vector2(246f, 48f));
                Rect((RectTransform)counterUi.transform, new Vector2(1f, .5f), new Vector2(-142f, 154f), new Vector2(250f, 38f));
                Rect((RectTransform)selectedUi.transform, new Vector2(1f, .5f), new Vector2(-142f, 116f), new Vector2(250f, 34f));
            }
            else
            {
                Rect((RectTransform)conditionPanel.transform, new Vector2(.5f, 1f), new Vector2(0f, -95f), new Vector2(366f, 132f));
                Rect((RectTransform)conditionUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(344f, 116f));
                Rect((RectTransform)goalUi.transform, new Vector2(.5f, 1f), new Vector2(0f, -184f), new Vector2(370f, 48f));
                Rect((RectTransform)counterUi.transform, new Vector2(.5f, 0f), new Vector2(0f, 142f), new Vector2(360f, 38f));
                Rect((RectTransform)selectedUi.transform, new Vector2(.5f, 0f), new Vector2(0f, 102f), new Vector2(360f, 34f));
            }
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
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
            tethers[index].gameObject.SetActive(true);
            tethers[index].positionCount = 2;
            tethers[index].SetPosition(0, CrabHitCenter(index));
            tethers[index].SetPosition(1, previousSweepPoint + Vector3.up * .2f);
        }

        void BeginRevealVisual(bool correct)
        {
            for (int i = 0; i < crabs.Length; i++) revealHome[i] = crabs[i].transform.position;
            guideRing.SetActive(false); guideLine.gameObject.SetActive(false); ghostFinger.SetActive(false);
            if (correct)
            {
                MgfSfx.Play("correct", .52f);
                MgfFx.Burst(controlRoot.position + new Vector3(0f, 1.4f, 0f), MgfLook.Hex("F7F3DC"), 20 + st.combo * 5, 3.4f, .18f);
                MgfFx.Glow(controlRoot.position + new Vector3(0f, 1.2f, 0f), MgfLook.Hex("E4BE58"), 12 + st.combo * 2, .42f);
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
                crabs[i].transform.localScale = Vector3.one;
                tethers[i].gameObject.SetActive(false);
                crabLabels[i].color = MgfLook.Hex("354A45");
            }
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
            for (int i = 0; i < tethers.Length; i++)
                if (tethers[i].gameObject.activeSelf) tethers[i].SetPosition(1, points[Mathf.Max(0, count - 1)] + Vector3.up * .24f);
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

        bool IsCapacityHandle(Vector2 screen) => Vector2.Distance(screen, cam.WorldToScreenPoint(capacityHandle.transform.position)) <= 68f;
        bool IsSweepHandle(Vector2 screen) => Vector2.Distance(screen, cam.WorldToScreenPoint(SweepHome)) <= 72f;

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
            ShowToast(st.capacity == 2 ? "그물 손잡이에서 등번호 1인 두 게를 이어 쓸어 보시오" : "조개 손잡이를 2칸 눈금까지 끌어 보시오", 2.6f);
        }

        void AdvancePracticeGuideToSweep()
        {
            guideLeft = 4.4f;
            ShowToast("이제 그물 손잡이에서 등번호 1인 두 게를 이어 쓸어 보시오", 2.8f);
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
