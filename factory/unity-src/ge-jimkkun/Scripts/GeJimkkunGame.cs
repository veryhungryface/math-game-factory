// 게 짐꾼 — 환초 환승장의 2.5D 세계, 실제 드래그 입력, 진행·실패·브리지 정본.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.GeJimkkun
{
    static class UiMesh
    {
        public static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = b - a; if (d.sqrMagnitude < .01f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * width * .5f;
            int i = vh.currentVertCount;
            vh.AddVert(a - n, color, Vector2.zero); vh.AddVert(a + n, color, Vector2.up);
            vh.AddVert(b + n, color, Vector2.one); vh.AddVert(b - n, color, Vector2.right);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        public static void Ring(VertexHelper vh, Vector2 c, float r, float width, Color color, int seg = 30)
        {
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2 / seg, b = (i + 1) * Mathf.PI * 2 / seg;
                Line(vh, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r,
                    c + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * r, width, color);
            }
        }
    }

    public sealed class HarborTriangleGraphic : MaskableGraphic
    {
        public bool Target;
        public int[] GivenEdges = new int[3];
        public int[] GivenAngles = new int[3];
        public int ConflictVertex = -1;
        public float Pulse;
        public float Trace;

        public Vector2 Vertex(int i)
        {
            Rect r = rectTransform.rect;
            if (i == 0) return new Vector2(r.xMin + r.width * .18f, r.yMin + r.height * .22f);
            if (i == 1) return new Vector2(r.center.x + r.width * .02f, r.yMax - r.height * .15f);
            return new Vector2(r.xMax - r.width * .16f, r.yMin + r.height * .24f);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Color ink = MgfLook.Hex("#193E4A"), coral = MgfLook.Hex("#E66E62"), white = MgfLook.Hex("#FFFDF1");
            Color turquoise = MgfLook.Hex("#2C8FA5"), dim = new Color(ink.r, ink.g, ink.b, .20f);
            Vector2[] v = { Vertex(0), Vertex(1), Vertex(2) };
            for (int e = 0; e < 3; e++)
            {
                int j = (e + 1) % 3;
                UiMesh.Line(vh, v[e], v[j], 9f, white);
                UiMesh.Line(vh, v[e], v[j], GivenEdges[e] != 0 ? 4.5f : 3.2f, GivenEdges[e] != 0 ? ink : dim);
            }
            for (int i = 0; i < 3; i++)
            {
                float rr = Target ? 24f + Mathf.Sin(Pulse * 4f + i) * 2f : 18f;
                UiMesh.Ring(vh, v[i], rr, Target ? 4.2f : 2.6f, Target ? turquoise : new Color(ink.r, ink.g, ink.b, .35f));
                if (GivenAngles[i] != 0) UiMesh.Ring(vh, v[i], 34f, 4f, coral, 18);
                if (ConflictVertex == i) UiMesh.Ring(vh, v[i], rr + 8f, 6f, coral, 24);
            }
            if (Trace > 0)
            {
                float total = Mathf.Clamp01(Trace) * 3f;
                for (int e = 0; e < 3; e++)
                {
                    float t = Mathf.Clamp01(total - e); if (t <= 0) continue;
                    int j = (e + 1) % 3;
                    UiMesh.Line(vh, v[e], Vector2.Lerp(v[e], v[j], t), 8f, new Color(1f, 1f, .92f, .95f));
                }
            }
        }

        public void Refresh() => SetVerticesDirty();
    }

    public sealed class GeJimkkunGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int round;
            public int firstAttemptCorrect;
            public int firstAttemptCount;
            public int firstFailures;
            public int combo;
            public int bonusSeconds;
            public int[] assignments = { -1, -1, -1 };
            public string currentProblemId = "";
            public string tutorialStep = "title";
            public int submissionSerial;
            public int inputSerial;
            public int invalidCount;
            public string misconceptionId = "";
            public string lastResult = "";
            public bool firstAttemptUsed;
            public bool frozen;
            public bool dragActive;
            public int draggingCrab = -1;
            public int layoutVersion;
        }

        enum Flow { Title, Practice, Playing, Reveal, End }

        static readonly Color Water = MgfLook.Hex("#67BED1");
        static readonly Color Peach = MgfLook.Hex("#F4C5A5");
        static readonly Color Ivory = MgfLook.Hex("#F5F4E8");
        static readonly Color Moss = MgfLook.Hex("#385B53");
        static readonly Color Ink = MgfLook.Hex("#193E4A");
        static readonly Color Coral = MgfLook.Hex("#E66E62");
        static readonly Color White = MgfLook.Hex("#FFFDF1");

        readonly State st = new State();
        readonly List<CargoProblem> bank = new List<CargoProblem>(360);
        readonly List<CargoProblem> sss = new List<CargoProblem>(120);
        readonly List<CargoProblem> sas = new List<CargoProblem>(120);
        readonly List<CargoProblem> asa = new List<CargoProblem>(120);
        readonly RectTransform[] crabs = new RectTransform[3];
        readonly RectTransform[] crabShadows = new RectTransform[3];
        readonly TextMeshProUGUI[] crabTags = new TextMeshProUGUI[3];
        readonly TextMeshProUGUI[] sourceVertexLabels = new TextMeshProUGUI[3];
        readonly TextMeshProUGUI[] targetVertexLabels = new TextMeshProUGUI[3];
        readonly TextMeshProUGUI[] sourceEdgeLabels = new TextMeshProUGUI[3];
        readonly TextMeshProUGUI[] targetEdgeLabels = new TextMeshProUGUI[3];
        readonly TextMeshProUGUI[] sourceAngleLabels = new TextMeshProUGUI[3];
        readonly TextMeshProUGUI[] targetAngleLabels = new TextMeshProUGUI[3];
        readonly Image[] knots = new Image[3];
        readonly Image[] raftPips = new Image[8];
        readonly List<Transform> worldProps = new List<Transform>();
        readonly List<Transform> worldCrabs = new List<Transform>();

        Camera cam;
        Transform world, titleCrab3d, raft3d;
        Material waterMat, shellMat, coralMat, ivoryMat, mossMat, inkMat;
        CanvasGroup titleGroup, playGroup, revealGroup, endGroup, feedbackGroup;
        RectTransform canvasRt, titleStart, sourceBoardRt, targetBoardRt, promptPanel, goalPanel, hudPanel, progressPanel;
        RectTransform endButton, guideRing, guideTarget, guideLine, guideHand, ripple, toastPanel;
        HarborTriangleGraphic sourceDiagram, targetDiagram;
        TextMeshProUGUI titleBest, promptText, goalText, hudText, progressText, scoreText, toastText;
        TextMeshProUGUI revealTitle, revealBody, endTitle, endStats, endScore;
        Sprite roundedSprite, softSprite, ringSprite;
        CargoProblem current;
        List<CargoProblem> deck;
        System.Random rng = new System.Random(20261025);
        Flow flow = Flow.Title;
        int runSerial, dragging = -1;
        float secondsLeft = GeJimkkunRules.RunSeconds, revealClock, titleClock, idleClock, scoreShown;
        float rippleClock = 9f, toastClock = 9f, wrongJolt, hitStop, launchClock;
        bool revealCorrect, failAfterReveal, tutorialFirstCarryDone, didDrop;
        Vector2 dragOffset, dragStart;
        float dragDistance;
        float lastCanvasW = -1, lastCanvasH = -1;

        void Awake()
        {
            MgfLook.Quality(30f);
            // CreatePrimitive가 런타임에 붙이는 collider 타입을 IL2CPP 스트리핑에서 보존한다.
            if (Time.frameCount < 0)
            {
                gameObject.AddComponent<BoxCollider>(); gameObject.AddComponent<SphereCollider>();
                gameObject.AddComponent<CapsuleCollider>(); gameObject.AddComponent<MeshCollider>();
            }
            bank.AddRange(GeJimkkunRules.BuildBank());
            for (int i = 0; i < bank.Count; i++)
            {
                if (bank[i].band == 1) sss.Add(bank[i]); else if (bank[i].band == 2) sas.Add(bank[i]); else asa.Add(bank[i]);
            }
            BuildMaterials(); BuildWorld(); BuildUi(); Prewarm(); ShowTitle();
            MgfBridge.Register(this);
        }

        void BuildMaterials()
        {
            waterMat = MgfLook.Lit(Water, .72f, .02f, new Color(.02f, .09f, .10f));
            shellMat = MgfLook.Lit(Peach, .43f, .03f);
            coralMat = MgfLook.Lit(Coral, .28f, .02f);
            ivoryMat = MgfLook.Lit(Ivory, .50f, .04f);
            mossMat = MgfLook.Lit(Moss, .22f, .02f);
            inkMat = MgfLook.Lit(Ink, .18f, .02f);
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#BFE8EA"), MgfLook.Hex("#F7E3C7"), Water, .92f);
            MgfLook.Sun(new Vector3(38, -32, -20), MgfLook.Hex("#FFF2D2"), 1.18f, .46f);
            cam = MgfLook.Camera(new Vector3(0, 13.5f, -17.5f), new Vector3(0, 0, 2.5f), 36f);
            cam.orthographic = true; cam.orthographicSize = 8.2f;
            world = new GameObject("TurquoiseAtollHarbor").transform;
            MgfLook.Block("TidalWater", new Vector3(0, -1.15f, 4), new Vector3(30, .7f, 30), .18f, waterMat, world);
            for (int i = 0; i < 14; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * (4.9f + (i % 4) * 1.35f), z = -4f + (i % 7) * 2.15f;
                var c = MgfLook.Prim(PrimitiveType.Capsule, "TinyCoral" + i, new Vector3(x, -.22f, z),
                    new Vector3(.24f + (i % 3) * .08f, .55f + (i % 4) * .12f, .24f), i % 3 == 0 ? coralMat : shellMat, world, false).transform;
                c.localRotation = Quaternion.Euler((i % 3) * 9, 0, side * (8 + i % 5 * 4)); worldProps.Add(c);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2 / 12f, r = 5.4f + (i % 3) * .7f;
                var buoy = MgfLook.Prim(PrimitiveType.Sphere, "CeramicBuoy" + i,
                    new Vector3(Mathf.Sin(a) * r, -.32f, 2.8f + Mathf.Cos(a) * r), new Vector3(.35f, .22f, .35f),
                    i % 2 == 0 ? ivoryMat : shellMat, world, false).transform; worldProps.Add(buoy);
            }
            MakeDock(new Vector3(-3.5f, -.42f, 1.6f), -8f, "DepartureShellDock");
            MakeDock(new Vector3(3.5f, -.42f, 4.2f), 8f, "ArrivalShellDock");
            raft3d = MgfLook.Block("ShellCargoRaft", new Vector3(0, -.20f, 7.8f), new Vector3(3.0f, .30f, 1.75f), .35f, shellMat, world).transform;
            MgfLook.Prim(PrimitiveType.Sphere, "CargoPearl1", new Vector3(-.6f, .38f, 0), Vector3.one * .55f, ivoryMat, raft3d, false);
            MgfLook.Prim(PrimitiveType.Sphere, "CargoPearl2", new Vector3(.45f, .38f, .15f), Vector3.one * .45f, coralMat, raft3d, false);
            for (int i = 0; i < 3; i++)
            {
                var crab = MakeWorldCrab(new Vector3((i - 1) * 1.5f, .15f, 1.2f + (i % 2) * .25f), "HarborCrab" + i);
                worldCrabs.Add(crab);
            }
            titleCrab3d = worldCrabs[1];
        }

        void MakeDock(Vector3 pos, float yaw, string name)
        {
            var root = new GameObject(name).transform; root.SetParent(world, false); root.localPosition = pos; root.localRotation = Quaternion.Euler(0, yaw, 0);
            MgfLook.Block("ShellInterior", Vector3.zero, new Vector3(6.2f, .55f, 4.1f), .7f, ivoryMat, root);
            for (int i = -3; i <= 3; i++)
            {
                var rib = MgfLook.Block("ShellRib" + i, new Vector3(i * .74f, .34f, 0), new Vector3(.10f, .24f, 3.55f), .045f, shellMat, root).transform;
                rib.localRotation = Quaternion.Euler(0, i * 1.7f, 0);
            }
            MgfLook.Prim(PrimitiveType.Cylinder, "ShellBell", new Vector3(0, 1.05f, 1.75f), new Vector3(.36f, .62f, .36f), coralMat, root, false);
        }

        Transform MakeWorldCrab(Vector3 pos, string name)
        {
            var root = new GameObject(name).transform; root.SetParent(world, false); root.localPosition = pos;
            MgfLook.Prim(PrimitiveType.Sphere, "Body", Vector3.zero, new Vector3(.88f, .38f, .62f), coralMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Shell", new Vector3(0, .42f, .10f), new Vector3(1.0f, .78f, .84f), shellMat, root, false);
            for (int s = -1; s <= 1; s += 2)
            {
                MgfLook.Prim(PrimitiveType.Sphere, "Eye" + s, new Vector3(s * .25f, .36f, -.48f), Vector3.one * .16f, inkMat, root, false);
                var claw = MgfLook.Prim(PrimitiveType.Sphere, "Claw" + s, new Vector3(s * .65f, .08f, -.20f), new Vector3(.40f, .22f, .30f), coralMat, root, false).transform;
                claw.localRotation = Quaternion.Euler(0, 0, s * 18f);
            }
            return root;
        }

        void BuildUi()
        {
            roundedSprite = MakeRoundedSprite(64, 15, false); ringSprite = MakeRoundedSprite(64, 10, true);
            softSprite = Sprite.Create(MgfLook.SoftDot, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64);
            canvasRt = (RectTransform)MgfText.Canvas.transform;
            titleGroup = Group("TitleUI"); playGroup = Group("PlayUI"); revealGroup = Group("RevealUI"); endGroup = Group("EndUI"); feedbackGroup = Group("FeedbackUI");
            BuildTitle(); BuildPlay(); BuildReveal(); BuildEnd(); BuildFeedback();
        }

        CanvasGroup Group(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup)); go.transform.SetParent(canvasRt, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go.GetComponent<CanvasGroup>();
        }

        void BuildTitle()
        {
            var sign = Panel(titleGroup.transform, "GiantShellStationSign", new Vector2(.5f, .73f), Vector2.zero, new Vector2(352, 230), new Color(Ivory.r, Ivory.g, Ivory.b, .94f));
            var rim = Img(sign, "PeachShellRim", new Vector2(.5f, .5f), Vector2.zero, new Vector2(338, 214), new Color(Peach.r, Peach.g, Peach.b, .78f), ringSprite);
            rim.type = Image.Type.Simple;
            var shadow = Txt(sign, "게 짐꾼", new Vector2(.5f, .62f), new Vector2(4, -5), 60, Moss, 310); shadow.characterSpacing = 4;
            var logo = Txt(sign, "게 짐꾼", new Vector2(.5f, .62f), Vector2.zero, 60, Ink, 310); logo.characterSpacing = 4; logo.outlineColor = White; logo.outlineWidth = .18f;
            Txt(sign, "게를 대응점으로 옮겨라", new Vector2(.5f, .25f), Vector2.zero, 17, Coral, 300);
            var start = CrabWidget(titleGroup.transform, "TitleCrabStart", "출", new Vector2(.5f, .32f), Vector2.zero, 1.48f, -1);
            titleStart = start; Img(start, "TitlePulse", new Vector2(.5f, .5f), Vector2.zero, new Vector2(142, 142), new Color(White.r, White.g, White.b, .28f), ringSprite).transform.SetAsFirstSibling();
            Txt(titleGroup.transform, "게를 눌러 짐 싣기", new Vector2(.5f, .20f), Vector2.zero, 17, Ink, 270);
            Txt(titleGroup.transform, "중학교 1학년 · 작도와 합동", new Vector2(.5f, .075f), Vector2.zero, 13, Ink, 330);
            titleBest = Txt(titleGroup.transform, "최고 첫 제출 0/8", new Vector2(.5f, .125f), Vector2.zero, 13, Moss, 260);
        }

        void BuildPlay()
        {
            hudPanel = Panel(playGroup.transform, "ShellHud", new Vector2(.5f, .5f), Vector2.zero, new Vector2(370, 48), new Color(Ivory.r, Ivory.g, Ivory.b, .96f));
            hudText = Txt(hudPanel, "배 0/8 · 첫 제출 0/0 · 90초", new Vector2(.42f, .5f), Vector2.zero, 13, Ink, 265);
            scoreText = Txt(hudPanel, "0", new Vector2(.88f, .5f), Vector2.zero, 16, Coral, 60);
            for (int i = 0; i < 3; i++) knots[i] = Img(hudPanel, "SafetyKnot" + i, new Vector2(.69f + i * .055f, .5f), Vector2.zero, new Vector2(13, 28), Moss, roundedSprite);
            goalPanel = Panel(playGroup.transform, "PermanentGoal", new Vector2(.5f, .5f), Vector2.zero, new Vector2(368, 38), new Color(Moss.r, Moss.g, Moss.b, .94f));
            goalText = Txt(goalPanel, "게를 대응점에 옮기면 배가 출항한다", new Vector2(.5f, .5f), Vector2.zero, 14, White, 352);
            promptPanel = Panel(playGroup.transform, "ConditionSheet", new Vector2(.5f, .5f), Vector2.zero, new Vector2(370, 82), new Color(Ivory.r, Ivory.g, Ivory.b, .94f));
            promptText = Txt(promptPanel, "", new Vector2(.5f, .57f), Vector2.zero, 12.5f, Ink, 350); promptText.enableAutoSizing = true; promptText.fontSizeMin = 9; promptText.fontSizeMax = 12.5f;
            Txt(promptPanel, "그림은 실제 비율과 다릅니다", new Vector2(.5f, .10f), Vector2.zero, 9.5f, Coral, 300);

            sourceBoardRt = Panel(playGroup.transform, "DepartureDiagramDock", new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 220), new Color(Ivory.r, Ivory.g, Ivory.b, .91f));
            targetBoardRt = Panel(playGroup.transform, "ArrivalDiagramRaft", new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 220), new Color(Ivory.r, Ivory.g, Ivory.b, .91f));
            sourceDiagram = Graphic(sourceBoardRt, "TriangleABC", false); targetDiagram = Graphic(targetBoardRt, "TriangleDEF", true);
            Txt(sourceBoardRt, "출발 △ABC", new Vector2(.5f, .94f), Vector2.zero, 13, Moss, 160);
            Txt(targetBoardRt, "도착 △DEF", new Vector2(.5f, .94f), Vector2.zero, 13, Moss, 160);
            for (int i = 0; i < 3; i++)
            {
                sourceVertexLabels[i] = Txt(sourceBoardRt, "ABC"[i].ToString(), new Vector2(.5f, .5f), Vector2.zero, 17, Ink, 30);
                targetVertexLabels[i] = Txt(targetBoardRt, "DEF"[i].ToString(), new Vector2(.5f, .5f), Vector2.zero, 17, Ink, 30);
                sourceEdgeLabels[i] = Txt(sourceBoardRt, "", new Vector2(.5f, .5f), Vector2.zero, 13, Ink, 90);
                targetEdgeLabels[i] = Txt(targetBoardRt, "", new Vector2(.5f, .5f), Vector2.zero, 13, Ink, 90);
                sourceAngleLabels[i] = Txt(sourceBoardRt, "", new Vector2(.5f, .5f), Vector2.zero, 12, Coral, 70);
                targetAngleLabels[i] = Txt(targetBoardRt, "", new Vector2(.5f, .5f), Vector2.zero, 12, Coral, 70);
                crabs[i] = CrabWidget(playGroup.transform, "CargoCrab" + i, "ABC"[i].ToString(), new Vector2(.5f, .5f), Vector2.zero, 1f, i);
                crabShadows[i] = (RectTransform)crabs[i].Find("CrabShadow"); crabTags[i] = crabs[i].GetComponentInChildren<TextMeshProUGUI>();
            }
            progressPanel = Panel(playGroup.transform, "RaftLedger", new Vector2(.5f, .5f), Vector2.zero, new Vector2(370, 46), new Color(Ink.r, Ink.g, Ink.b, .90f));
            progressText = Txt(progressPanel, "게 0/3 · 세 자리가 차면 자동 제출", new Vector2(.43f, .5f), Vector2.zero, 13, White, 270);
            for (int i = 0; i < 8; i++) raftPips[i] = Img(progressPanel, "SailedRaft" + i, new Vector2(.72f + i * .032f, .5f), Vector2.zero, new Vector2(8, 21), i == 0 ? Peach : new Color(White.r, White.g, White.b, .22f), roundedSprite);
            guideLine = Img(playGroup.transform, "FlowingCarryRoute", new Vector2(.5f, .5f), Vector2.zero, new Vector2(120, 8), new Color(Coral.r, Coral.g, Coral.b, .88f), roundedSprite).rectTransform;
            guideRing = Img(playGroup.transform, "CrabGuideRing", new Vector2(.5f, .5f), Vector2.zero, new Vector2(88, 88), new Color(Coral.r, Coral.g, Coral.b, .45f), ringSprite).rectTransform;
            guideTarget = Img(playGroup.transform, "TargetGuideRing", new Vector2(.5f, .5f), Vector2.zero, new Vector2(92, 92), new Color(Coral.r, Coral.g, Coral.b, .60f), ringSprite).rectTransform;
            guideHand = Img(playGroup.transform, "MovingGhostHand", new Vector2(.5f, .5f), Vector2.zero, new Vector2(42, 54), new Color(White.r, White.g, White.b, .90f), softSprite).rectTransform;
        }

        HarborTriangleGraphic Graphic(RectTransform parent, string name, bool target)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), new Vector2(0, -5), new Vector2(310, 180));
            var g = rt.gameObject.AddComponent<HarborTriangleGraphic>(); g.Target = target; g.raycastTarget = false; return g;
        }

        void BuildReveal()
        {
            var card = Panel(revealGroup.transform, "MeasurementRopeReveal", new Vector2(.5f, .5f), Vector2.zero, new Vector2(352, 132), new Color(Ink.r, Ink.g, Ink.b, .95f));
            revealTitle = Txt(card, "", new Vector2(.5f, .69f), Vector2.zero, 24, White, 325);
            revealBody = Txt(card, "", new Vector2(.5f, .31f), Vector2.zero, 13, Ivory, 326); revealBody.enableAutoSizing = true; revealBody.fontSizeMin = 10; revealBody.fontSizeMax = 13;
        }

        void BuildEnd()
        {
            var card = Panel(endGroup.transform, "ShellResultMonument", new Vector2(.5f, .56f), Vector2.zero, new Vector2(360, 360), new Color(Ivory.r, Ivory.g, Ivory.b, .97f));
            endTitle = Txt(card, "", new Vector2(.5f, .76f), Vector2.zero, 30, Ink, 340); endTitle.outlineColor = Peach; endTitle.outlineWidth = .14f;
            endTitle.enableAutoSizing = true; endTitle.fontSizeMin = 22; endTitle.fontSizeMax = 30; endTitle.textWrappingMode = TextWrappingModes.NoWrap;
            endStats = Txt(card, "", new Vector2(.5f, .50f), Vector2.zero, 17, Moss, 320);
            endScore = Txt(card, "0", new Vector2(.5f, .31f), Vector2.zero, 42, Coral, 260);
            endButton = Panel(card, "RestartShell", new Vector2(.5f, .12f), Vector2.zero, new Vector2(230, 62), Moss);
            Txt(endButton, "게를 눌러 다시 싣기", new Vector2(.5f, .5f), Vector2.zero, 16, White, 214);
        }

        void BuildFeedback()
        {
            toastPanel = Panel(feedbackGroup.transform, "HarborToast", new Vector2(.5f, .5f), Vector2.zero, new Vector2(350, 70), new Color(Ink.r, Ink.g, Ink.b, .94f));
            toastText = Txt(toastPanel, "", new Vector2(.5f, .5f), Vector2.zero, 13, White, 330); toastText.enableAutoSizing = true; toastText.fontSizeMin = 10; toastText.fontSizeMax = 13;
            ripple = Img(feedbackGroup.transform, "InputWaterRipple", new Vector2(.5f, .5f), Vector2.zero, new Vector2(42, 42), new Color(White.r, White.g, White.b, .58f), ringSprite).rectTransform;
        }

        RectTransform CrabWidget(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, float scale, int index)
        {
            var root = R(name, parent, anchor, pos, new Vector2(78, 66)); root.localScale = Vector3.one * scale;
            Img(root, "CrabShadow", new Vector2(.5f, .5f), new Vector2(0, -20), new Vector2(72, 24), new Color(Ink.r, Ink.g, Ink.b, .18f), softSprite);
            Img(root, "LeftClaw", new Vector2(.16f, .50f), new Vector2(-3, -2), new Vector2(25, 22), Coral, softSprite).rectTransform.localRotation = Quaternion.Euler(0, 0, -22);
            Img(root, "RightClaw", new Vector2(.84f, .50f), new Vector2(3, -2), new Vector2(25, 22), Coral, softSprite).rectTransform.localRotation = Quaternion.Euler(0, 0, 22);
            Img(root, "CoralBody", new Vector2(.5f, .40f), Vector2.zero, new Vector2(57, 30), Coral, softSprite);
            Img(root, "PeachShell", new Vector2(.5f, .63f), Vector2.zero, new Vector2(64, 50), Peach, softSprite);
            Img(root, "ShellShine", new Vector2(.42f, .72f), Vector2.zero, new Vector2(24, 13), new Color(White.r, White.g, White.b, .50f), softSprite);
            Img(root, "LeftEye", new Vector2(.39f, .37f), Vector2.zero, new Vector2(8, 11), Ink, softSprite);
            Img(root, "RightEye", new Vector2(.61f, .37f), Vector2.zero, new Vector2(8, 11), Ink, softSprite);
            var tag = Panel(root, "LetterTag", new Vector2(.5f, .54f), Vector2.zero, new Vector2(31, 31), Ink, softSprite);
            Txt(tag, label, new Vector2(.5f, .5f), Vector2.zero, 15, White, 26);
            return root;
        }

        void Prewarm()
        {
            var sb = new StringBuilder(50000);
            sb.Append("게짐꾼게를대응점으로옮겨라출항도착출발연습실전배첫제출보호매듭남은시간수선성공실패다시싣기");
            sb.Append("주어진조건을이용하여대응하는꼭짓점으로게를옮기시오그림은실제비율과다릅니다같은두변이만나는꼭짓점끼인각한변의양끝각");
            sb.Append("대응을확인했습니다밧줄이느슨합니다빈자리로옮기시오꼭짓점자리에놓으시오세자리가차면자동제출중학교학년작도와합동");
            sb.Append("△∠≡→=°cmSSSSASASAABCDEF0123456789");
            for (int i = 0; i < bank.Count; i++) sb.Append(bank[i].prompt).Append(bank[i].answer).Append(bank[i].concept);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            titleClock += dt; idleClock += dt; rippleClock += dt; toastClock += dt;
            if (hitStop > 0) hitStop -= dt; else AnimateWorld(dt);
            Relayout(); HandlePointer();
            if (flow == Flow.Playing)
            {
                secondsLeft -= dt;
                if (secondsLeft <= 0) { secondsLeft = 0; EndRun("time"); }
            }
            else if (flow == Flow.Reveal)
            {
                revealClock += dt; targetDiagram.Trace = revealCorrect ? Mathf.Clamp01(revealClock / .70f) : 0; targetDiagram.Refresh();
                if (revealClock >= (revealCorrect ? 1.35f : .95f)) FinishReveal();
            }
            UpdateGuides(); UpdateHud(dt); UpdateFeedback();
        }

        void AnimateWorld(float dt)
        {
            for (int i = 0; i < worldProps.Count; i++)
            {
                var t = worldProps[i]; Vector3 p = t.localPosition; p.y += Mathf.Sin(titleClock * 1.2f + i * .7f) * .0008f; t.localPosition = p;
            }
            for (int i = 0; i < worldCrabs.Count; i++)
            {
                var t = worldCrabs[i]; t.localRotation = Quaternion.Euler(0, Mathf.Sin(titleClock * 1.7f + i) * 7f, Mathf.Sin(titleClock * 2.3f + i) * 2f);
                t.localScale = new Vector3(1f + Mathf.Sin(titleClock * 2.1f + i) * .025f, 1f - Mathf.Sin(titleClock * 2.1f + i) * .02f, 1f);
            }
            if (raft3d)
            {
                raft3d.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(titleClock * 1.25f) * 1.6f);
                if (launchClock > 0) { launchClock -= dt; raft3d.localPosition += Vector3.forward * dt * 2.6f; }
            }
        }

        void ShowTitle()
        {
            ResetState(); flow = Flow.Title; st.phase = "title"; titleGroup.alpha = 1; titleGroup.blocksRaycasts = true;
            SetGroup(playGroup, false); SetGroup(revealGroup, false); SetGroup(endGroup, false); SetGroup(feedbackGroup, true);
            titleBest.text = "최고 첫 제출 " + PlayerPrefs.GetInt("ge-jimkkun-best", 0) + "/8";
            for (int i = 0; i < worldCrabs.Count; i++) worldCrabs[i].gameObject.SetActive(true);
            Notify();
        }

        void ResetState()
        {
            st.score = 0; st.lives = GeJimkkunRules.StartLives; st.level = 1; st.phase = "title"; st.solved = 0; st.round = 0;
            st.firstAttemptCorrect = 0; st.firstAttemptCount = 0; st.firstFailures = 0; st.combo = 0; st.bonusSeconds = 0;
            st.assignments = new[] { -1, -1, -1 }; st.currentProblemId = ""; st.tutorialStep = "title"; st.submissionSerial = 0;
            st.inputSerial = 0; st.invalidCount = 0; st.misconceptionId = ""; st.lastResult = ""; st.firstAttemptUsed = false;
            st.frozen = false; st.dragActive = false; st.draggingCrab = -1; dragging = -1; scoreShown = 0; didDrop = false;
        }

        void BeginPractice()
        {
            ResetState(); flow = Flow.Practice; st.phase = "playing"; st.frozen = true; st.tutorialStep = "A_to_F";
            current = GeJimkkunRules.Practice(); secondsLeft = GeJimkkunRules.RunSeconds; tutorialFirstCarryDone = false;
            titleGroup.alpha = 0; titleGroup.blocksRaycasts = false; SetGroup(playGroup, true); SetGroup(revealGroup, false); SetGroup(endGroup, false);
            LoadCurrent(); goalText.text = "게를 대응점에 옮기면 배가 출항한다"; toastClock = 99; Notify();
        }

        void BeginRun(bool test)
        {
            runSerial++; int seed = unchecked(20261025 + runSerial * 1009 + (test ? 0 : Environment.TickCount)); rng = new System.Random(seed);
            deck = new List<CargoProblem>(8);
            deck.Add(Pick(sss)); deck.Add(Pick(sss)); deck.Add(Pick(sas)); deck.Add(Pick(sas)); deck.Add(Pick(sas)); deck.Add(Pick(asa)); deck.Add(Pick(asa)); deck.Add(Pick(asa));
            ResetState(); flow = Flow.Playing; st.phase = "playing"; st.frozen = false; secondsLeft = GeJimkkunRules.RunSeconds;
            titleGroup.alpha = 0; titleGroup.blocksRaycasts = false; SetGroup(playGroup, true); SetGroup(revealGroup, false); SetGroup(endGroup, false);
            LoadRound(0); Notify();
        }

        CargoProblem Pick(List<CargoProblem> pool) => pool[rng.Next(pool.Count)];

        void LoadRound(int index)
        {
            if (index >= GeJimkkunRules.RoundCount) { EndRun("complete"); return; }
            st.round = index + 1; current = deck[index]; st.level = current.band; LoadCurrent();
        }

        void LoadCurrent()
        {
            st.currentProblemId = current.id; st.assignments = new[] { -1, -1, -1 }; st.firstAttemptUsed = false; st.lastResult = ""; st.misconceptionId = "";
            st.dragActive = false; st.draggingCrab = -1; dragging = -1; idleClock = 0; wrongJolt = 0;
            sourceDiagram.GivenEdges = current.sourceEdges; sourceDiagram.GivenAngles = current.sourceAngles; sourceDiagram.ConflictVertex = -1; sourceDiagram.Trace = 0; sourceDiagram.Refresh();
            targetDiagram.GivenEdges = current.targetEdges; targetDiagram.GivenAngles = current.targetAngles; targetDiagram.ConflictVertex = -1; targetDiagram.Trace = 0; targetDiagram.Refresh();
            promptText.text = current.prompt; ConfigureConditionLabels(); UpdateCrabPositions();
            progressText.text = flow == Flow.Practice ? "연습 · 게를 한 마리씩 직접 옮기시오" : "게 0/3 · 세 자리가 차면 자동 제출";
            Notify();
        }

        void ConfigureConditionLabels()
        {
            string[] sEdge = { "AB", "BC", "CA" }, tEdge = { "DE", "EF", "FD" };
            for (int i = 0; i < 3; i++)
            {
                sourceEdgeLabels[i].text = current.sourceEdges[i] == 0 ? "" : sEdge[i] + " " + current.sourceEdges[i] + " cm";
                targetEdgeLabels[i].text = current.targetEdges[i] == 0 ? "" : tEdge[i] + " " + current.targetEdges[i] + " cm";
                sourceAngleLabels[i].text = current.sourceAngles[i] == 0 ? "" : current.sourceAngles[i] + "°";
                targetAngleLabels[i].text = current.targetAngles[i] == 0 ? "" : current.targetAngles[i] + "°";
            }
            PositionBoardLabels(sourceDiagram, sourceVertexLabels, sourceEdgeLabels, sourceAngleLabels);
            PositionBoardLabels(targetDiagram, targetVertexLabels, targetEdgeLabels, targetAngleLabels);
        }

        void PositionBoardLabels(HarborTriangleGraphic diagram, TextMeshProUGUI[] vertices, TextMeshProUGUI[] edges, TextMeshProUGUI[] angles)
        {
            Vector2[] v = { diagram.Vertex(0), diagram.Vertex(1), diagram.Vertex(2) };
            Vector2 center = (v[0] + v[1] + v[2]) / 3f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 outDir = (v[i] - center).normalized;
                vertices[i].rectTransform.anchoredPosition = v[i] + outDir * 29f;
                angles[i].rectTransform.anchoredPosition = v[i] - outDir * 30f;
                int j = (i + 1) % 3; Vector2 mid = (v[i] + v[j]) * .5f;
                Vector2 edge = v[j] - v[i]; Vector2 n = new Vector2(-edge.y, edge.x).normalized;
                if (Vector2.Dot(n, mid - center) < 0) n = -n;
                edges[i].rectTransform.anchoredPosition = mid + n * 18f;
            }
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                st.inputSerial++; idleClock = 0; Vector2 local = ToCanvas(MgfPointer.Position); ShowRipple(local);
                if (flow == Flow.Title)
                {
                    if (Contains(titleStart, MgfPointer.Position, 18)) { MgfSfx.Play("pop", .42f); BeginPractice(); }
                    else Refuse("집게를 흔드는 게를 눌러 시작하시오.", local, true);
                    return;
                }
                if (flow == Flow.End)
                {
                    if (Contains(endButton, MgfPointer.Position, 12)) BeginPractice(); else Refuse("가운데 조개판을 눌러 다시 시작하시오.", local, false);
                    return;
                }
                if (flow == Flow.Reveal) { Refuse("밧줄이 조건을 확인하는 동안 잠시 보시오.", local, false); return; }
                if (flow != Flow.Playing && flow != Flow.Practice) return;
                int hit = HitCrab(local);
                if (hit < 0) { Refuse("문자 표찰이 달린 게를 잡아 꼭짓점으로 옮기시오.", local, true); return; }
                if (flow == Flow.Practice && st.tutorialStep == "A_to_F" && hit != 0) { Refuse("먼저 A 게를 흰 경로를 따라 F로 옮기시오.", local, true); return; }
                BeginDrag(hit, local);
            }
            if (dragging >= 0 && MgfPointer.Held)
            {
                Vector2 local = ToCanvas(MgfPointer.Position); dragDistance = Mathf.Max(dragDistance, Vector2.Distance(local, dragStart));
                crabs[dragging].anchoredPosition = local + dragOffset;
                float stretch = Mathf.Clamp01(dragDistance / 70f); crabs[dragging].localScale = new Vector3(1f + stretch * .12f, 1f - stretch * .10f, 1f);
                st.inputSerial++; if ((st.inputSerial & 3) == 0) Notify();
            }
            if (dragging >= 0 && MgfPointer.Up)
            {
                int crab = dragging; dragging = -1; st.dragActive = false; st.draggingCrab = -1;
                crabs[crab].localScale = Vector3.one; didDrop = true;
                if (dragDistance < 7f) { ReturnCrab(crab); Refuse("게를 누른 채 대응하는 꼭짓점까지 끌어 놓으시오.", ToCanvas(MgfPointer.Position), true); return; }
                DropCrab(crab, ToCanvas(MgfPointer.Position));
            }
        }

        void BeginDrag(int crab, Vector2 local)
        {
            dragging = crab; st.dragActive = true; st.draggingCrab = crab; dragStart = local; dragDistance = 0; didDrop = false;
            dragOffset = crabs[crab].anchoredPosition - local;
            if (st.assignments[crab] >= 0) st.assignments[crab] = -1;
            crabs[crab].SetAsLastSibling(); MgfSfx.Play("tap", .22f); MgfFx.Punch(crabs[crab], .10f, .18f); Notify();
        }

        void DropCrab(int crab, Vector2 local)
        {
            int nearest = -1; float best = 9999;
            for (int i = 0; i < 3; i++)
            {
                float d = Vector2.Distance(local, TargetPoint(i)); if (d < best) { best = d; nearest = i; }
            }
            if (best > 72f) { ReturnCrab(crab); Refuse("꼭짓점 자리 안에 놓으시오.", local, true); return; }
            for (int i = 0; i < 3; i++) if (i != crab && st.assignments[i] == nearest)
            { ReturnCrab(crab); Refuse("이미 게가 있습니다. 빈자리로 옮기시오.", local, true); return; }
            st.assignments[crab] = nearest; crabs[crab].anchoredPosition = TargetPoint(nearest);
            crabs[crab].localScale = new Vector3(1.10f, .90f, 1); MgfFx.Punch(crabs[crab], .15f, .24f); MgfSfx.Play("pop", .26f);
            if (flow == Flow.Practice && !tutorialFirstCarryDone && crab == 0 && nearest == 2)
            {
                tutorialFirstCarryDone = true; st.tutorialStep = "B_to_E";
                Toast("4 cm와 5 cm가 함께 만나는 A와 F가 대응합니다.", 1.6f); goalText.text = "남은 B와 C 게도 대응점으로 옮기시오";
            }
            else if (flow == Flow.Practice && st.tutorialStep == "A_to_F" && crab == 0)
            {
                st.assignments[crab] = -1; ReturnCrab(crab); Refuse("A의 4 cm·5 cm와 같은 두 변이 만나는 F를 찾으시오.", local, true); return;
            }
            int placed = PlacedCount(); progressText.text = (flow == Flow.Practice ? "연습 " : "게 ") + placed + "/3 · " + (placed == 3 ? "밧줄 조건 확인" : "빈 꼭짓점으로 계속 옮기시오");
            Notify(); if (placed == 3) Submit();
        }

        void ReturnCrab(int crab)
        {
            st.assignments[crab] = -1; crabs[crab].anchoredPosition = SourcePoint(crab); crabs[crab].localScale = Vector3.one;
        }

        void Submit()
        {
            bool practice = flow == Flow.Practice;
            bool correct = GeJimkkunRules.IsCorrect(current, st.assignments);
            bool first = !st.firstAttemptUsed; st.submissionSerial++; st.lastResult = correct ? "correct" : "wrong";
            st.misconceptionId = correct ? "" : GeJimkkunRules.Diagnose(current, st.assignments);
            if (!practice && first)
            {
                st.firstAttemptUsed = true; st.firstAttemptCount++;
                if (correct) st.firstAttemptCorrect++; else st.firstFailures++;
            }
            if (!practice && !correct) st.lives = Math.Max(0, st.lives - 1);
            if (correct)
            {
                if (!practice)
                {
                    st.solved++; st.score += first ? 100 : 25;
                    if (first) st.combo++; else st.combo = 0;
                    if (first && st.combo > 0 && st.combo % 2 == 0 && st.bonusSeconds < 8)
                    { int add = Math.Min(4, 8 - st.bonusSeconds); st.bonusSeconds += add; secondsLeft += add; }
                }
                revealCorrect = true; failAfterReveal = false; hitStop = .075f; launchClock = .75f;
                revealTitle.text = practice ? "대응 완성 · SSS 합동" : "출항 성공 · " + current.kind + " 합동";
                revealBody.text = current.answer + (first && !practice ? "\n첫 제출 기록에 반영했습니다." : practice ? "\n같은 조건이 만나는 꼭짓점을 확인했습니다." : "\n수선하여 출항했습니다.");
                MgfSfx.Play(practice ? "correct" : "win", .48f); MgfFx.Glow(new Vector3(0, 1.1f, 2.4f), White, 18 + st.combo * 3, .34f);
            }
            else
            {
                revealCorrect = false; st.combo = 0; wrongJolt = .42f;
                int bad = FirstMismatch(); targetDiagram.ConflictVertex = st.assignments[bad]; targetDiagram.Refresh();
                revealTitle.text = "밧줄 한 가닥이 느슨합니다";
                revealBody.text = ConflictExplanation(bad) + "\n게를 직접 다시 옮겨 수선하시오.";
                int remainingFirst = GeJimkkunRules.RoundCount - st.firstAttemptCount;
                failAfterReveal = !practice && (st.lives <= 0 || st.firstAttemptCorrect + remainingFirst < GeJimkkunRules.MasteryNeeded);
                MgfSfx.Play("wrong", .42f);
            }
            flow = Flow.Reveal; st.phase = "reveal"; st.frozen = true; revealClock = 0; SetGroup(revealGroup, true); revealGroup.alpha = 1; Notify();
        }

        int FirstMismatch()
        {
            for (int i = 0; i < 3; i++) if (st.assignments[i] != current.correspondence[i]) return i;
            return 0;
        }

        string ConflictExplanation(int i)
        {
            string s = "ABC"[i].ToString(), t = "DEF"[st.assignments[i]].ToString();
            if (current.band == 1) return s + "와 " + t + "에 붙은 두 변의 길이 조합이 다릅니다.";
            if (current.band == 2) return s + "의 두 변과 끼인각이 " + t + "에서 함께 만나지 않습니다.";
            return s + "의 한 변과 끝 각이 " + t + "의 조건과 다릅니다.";
        }

        void FinishReveal()
        {
            if (flow != Flow.Reveal) return;
            SetGroup(revealGroup, false); targetDiagram.Trace = 0; targetDiagram.Refresh();
            if (flow == Flow.End) return;
            if (current.id == "practice-fixed")
            {
                if (revealCorrect) { BeginRun(false); goalText.text = "게를 대응점에 옮기면 배가 출항한다"; }
                else { flow = Flow.Practice; st.phase = "playing"; st.frozen = true; st.tutorialStep = tutorialFirstCarryDone ? "repair" : "A_to_F"; }
                Notify(); return;
            }
            if (failAfterReveal) { EndRun(st.lives <= 0 ? "knots" : "mastery"); return; }
            if (!revealCorrect)
            {
                flow = Flow.Playing; st.phase = "playing"; st.frozen = false; progressText.text = "수선 중 · 충돌한 대응을 바꾸시오"; Notify(); return;
            }
            if (st.solved >= GeJimkkunRules.RoundCount) { EndRun(st.firstAttemptCorrect >= GeJimkkunRules.MasteryNeeded ? "complete" : "mastery"); return; }
            flow = Flow.Playing; st.phase = "playing"; st.frozen = false; LoadRound(st.solved);
        }

        void EndRun(string reason)
        {
            bool clear = reason == "complete" && st.solved == GeJimkkunRules.RoundCount && st.firstAttemptCorrect >= GeJimkkunRules.MasteryNeeded && st.lives > 0;
            flow = Flow.End; st.phase = clear ? "clear" : "gameover"; st.frozen = true; dragging = -1; st.dragActive = false; st.draggingCrab = -1;
            SetGroup(playGroup, false); SetGroup(revealGroup, false); SetGroup(endGroup, true);
            endTitle.text = clear ? "환초 항로 개통" : reason == "time" ? "밀물이 닿았습니다" : reason == "knots" ? "매듭이 모두 풀렸습니다" : "첫 제출 기준 미달";
            endStats.text = "출항 " + st.solved + "/8\n첫 제출 " + st.firstAttemptCorrect + "/" + st.firstAttemptCount + " · 매듭 " + st.lives + "/3";
            endScore.text = "0"; scoreShown = 0;
            toastPanel.gameObject.SetActive(false); ripple.gameObject.SetActive(false);
            int best = PlayerPrefs.GetInt("ge-jimkkun-best", 0); if (st.firstAttemptCorrect > best) PlayerPrefs.SetInt("ge-jimkkun-best", st.firstAttemptCorrect);
            MgfSfx.Play(clear ? "win" : "lose", .52f); Notify();
        }

        void Refuse(string message, Vector2 local, bool pointToCrabs)
        {
            st.invalidCount++; st.inputSerial++; wrongJolt = .18f; Toast(message, 1.3f); ShowRipple(local); MgfSfx.Play("wrong", .10f);
            if (pointToCrabs && crabs[0] != null) for (int i = 0; i < 3; i++) MgfFx.Punch(crabs[i], .08f, .20f);
            Notify();
        }

        void Toast(string message, float seconds)
        {
            toastText.text = message; toastClock = 0; toastPanel.gameObject.SetActive(true); toastPanel.localScale = new Vector3(.94f, .94f, 1);
        }

        void ShowRipple(Vector2 local)
        {
            ripple.anchoredPosition = local; rippleClock = 0; ripple.gameObject.SetActive(true); ripple.localScale = Vector3.one * .5f;
        }

        void UpdateFeedback()
        {
            if (ripple.gameObject.activeSelf)
            {
                float t = Mathf.Clamp01(rippleClock / .34f); ripple.localScale = Vector3.one * Mathf.Lerp(.45f, 1.7f, t);
                var im = ripple.GetComponent<Image>(); Color c = im.color; c.a = (1 - t) * .7f; im.color = c; if (t >= 1) ripple.gameObject.SetActive(false);
            }
            if (toastPanel.gameObject.activeSelf)
            {
                toastPanel.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, Mathf.Clamp01(toastClock * 8f));
                if (toastClock > 1.55f) toastPanel.gameObject.SetActive(false);
            }
        }

        void UpdateGuides()
        {
            bool show = flow == Flow.Practice && st.tutorialStep == "A_to_F";
            guideLine.gameObject.SetActive(show); guideRing.gameObject.SetActive(show); guideTarget.gameObject.SetActive(show); guideHand.gameObject.SetActive(show);
            if (!show) return;
            Vector2 a = SourcePoint(0), b = TargetPoint(2), d = b - a; float len = d.magnitude;
            guideLine.anchoredPosition = (a + b) * .5f; guideLine.sizeDelta = new Vector2(len, 7); guideLine.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            float pulse = 1f + Mathf.Sin(titleClock * (idleClock > 8 ? 9f : 5f)) * .12f;
            guideRing.anchoredPosition = a; guideTarget.anchoredPosition = b; guideRing.localScale = guideTarget.localScale = Vector3.one * pulse;
            float travel = Mathf.PingPong(titleClock * .65f, 1f); guideHand.anchoredPosition = Vector2.Lerp(a, b, Mathf.SmoothStep(0, 1, travel)) + new Vector2(26, 18);
        }

        void UpdateHud(float dt)
        {
            if (flow == Flow.Title) { titleStart.localScale = Vector3.one * (1.48f + Mathf.Sin(titleClock * 3.2f) * .035f); return; }
            scoreShown = Mathf.MoveTowards(scoreShown, st.score, Mathf.Max(70, Mathf.Abs(st.score - scoreShown) * 8f) * dt);
            if (flow == Flow.End) { endScore.text = Mathf.RoundToInt(scoreShown).ToString("N0") + "점"; return; }
            if (flow == Flow.Practice) hudText.text = "연습 · 시계 정지 · 매듭 안전";
            else hudText.text = "배 " + st.solved + "/8 · 첫 제출 " + st.firstAttemptCorrect + "/" + st.firstAttemptCount + " · " + Mathf.CeilToInt(secondsLeft) + "초";
            scoreText.text = Mathf.RoundToInt(scoreShown).ToString("N0");
            for (int i = 0; i < 3; i++) knots[i].color = i < st.lives ? Moss : new Color(Coral.r, Coral.g, Coral.b, .24f);
            for (int i = 0; i < 8; i++) raftPips[i].color = i < st.solved ? Peach : new Color(White.r, White.g, White.b, .22f);
            sourceDiagram.Pulse = targetDiagram.Pulse = titleClock; sourceDiagram.Refresh(); targetDiagram.Refresh();
            if (wrongJolt > 0)
            {
                wrongJolt -= dt; float amp = 7f * (wrongJolt / .42f); targetBoardRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(titleClock * 55f) * amp * .18f);
            }
            else targetBoardRt.localRotation = Quaternion.identity;
        }

        void Relayout()
        {
            float w = canvasRt.rect.width, h = canvasRt.rect.height; if (w < 100 || h < 100) return;
            if (Mathf.Abs(w - lastCanvasW) < .5f && Mathf.Abs(h - lastCanvasH) < .5f) return;
            lastCanvasW = w; lastCanvasH = h; st.layoutVersion++; bool land = w >= h * 1.08f;
            if (land)
            {
                float boardW = Mathf.Clamp(w * .34f, 330, 430), boardH = Mathf.Clamp(h * .48f, 260, 340);
                Center(hudPanel, new Vector2(0, h * .5f - 30), new Vector2(Mathf.Min(w - 40, 680), 52));
                Center(goalPanel, new Vector2(0, h * .5f - 83), new Vector2(Mathf.Min(w - 80, 600), 40));
                Center(promptPanel, new Vector2(0, h * .5f - 150), new Vector2(Mathf.Min(w * .55f, 680), 84));
                Center(sourceBoardRt, new Vector2(-w * .26f, -20), new Vector2(boardW, boardH));
                Center(targetBoardRt, new Vector2(w * .26f, -20), new Vector2(boardW, boardH));
                Center(progressPanel, new Vector2(0, -h * .5f + 32), new Vector2(Mathf.Min(w - 80, 620), 48));
                toastPanel.anchoredPosition = new Vector2(0, h * .5f - 210);
                cam.orthographicSize = 7.0f;
            }
            else
            {
                float boardW = Mathf.Min(348, w - 34), boardH = Mathf.Min(220, h * .265f);
                Center(hudPanel, new Vector2(0, h * .5f - 28), new Vector2(Mathf.Min(374, w - 16), 48));
                Center(goalPanel, new Vector2(0, h * .5f - 74), new Vector2(Mathf.Min(370, w - 20), 36));
                Center(promptPanel, new Vector2(0, h * .5f - 137), new Vector2(Mathf.Min(370, w - 20), 82));
                Center(targetBoardRt, new Vector2(0, h * .5f - 300), new Vector2(boardW, boardH));
                Center(sourceBoardRt, new Vector2(0, -h * .5f + 275), new Vector2(boardW, boardH));
                Center(progressPanel, new Vector2(0, -h * .5f + 29), new Vector2(Mathf.Min(372, w - 18), 46));
                toastPanel.anchoredPosition = new Vector2(0, 28);
                cam.orthographicSize = 8.2f;
            }
            var sg = sourceDiagram.rectTransform; sg.sizeDelta = new Vector2(sourceBoardRt.sizeDelta.x - 20, sourceBoardRt.sizeDelta.y - 34);
            var tg = targetDiagram.rectTransform; tg.sizeDelta = new Vector2(targetBoardRt.sizeDelta.x - 20, targetBoardRt.sizeDelta.y - 34);
            if (current != null) { ConfigureConditionLabels(); UpdateCrabPositions(); }
            Notify();
        }

        void UpdateCrabPositions()
        {
            if (current == null) return;
            for (int i = 0; i < 3; i++) if (dragging != i)
                crabs[i].anchoredPosition = st.assignments[i] >= 0 ? TargetPoint(st.assignments[i]) : SourcePoint(i);
        }

        Vector2 SourcePoint(int i) => sourceBoardRt.anchoredPosition + sourceDiagram.rectTransform.anchoredPosition + sourceDiagram.Vertex(i);
        Vector2 TargetPoint(int i) => targetBoardRt.anchoredPosition + targetDiagram.rectTransform.anchoredPosition + targetDiagram.Vertex(i);

        int HitCrab(Vector2 local)
        {
            int best = -1; float dist = 999;
            for (int i = 0; i < 3; i++) { float d = Vector2.Distance(local, crabs[i].anchoredPosition); if (d < 54 && d < dist) { dist = d; best = i; } }
            return best;
        }

        int PlacedCount() { int n = 0; for (int i = 0; i < 3; i++) if (st.assignments[i] >= 0) n++; return n; }
        Vector2 ToCanvas(Vector2 screen) { RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var p); return p; }
        bool Contains(RectTransform rt, Vector2 screen, float pad) { Rect r = rt.rect; r.xMin -= pad; r.xMax += pad; r.yMin -= pad; r.yMax += pad; RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, null, out var p); return r.Contains(p); }
        void Notify() => MgfBridge.NotifyChanged();

        public void TestStart() => BeginRun(true);

        void EnsureTestPlaying()
        {
            if (flow == Flow.Reveal) { revealClock = 99; FinishReveal(); }
            if (flow != Flow.Playing) BeginRun(true);
        }

        public void TestAnswerCorrect()
        {
            EnsureTestPlaying(); st.assignments = new[] { current.correspondence[0], current.correspondence[1], current.correspondence[2] }; UpdateCrabPositions(); Submit();
        }

        public void TestAnswerWrong()
        {
            EnsureTestPlaying(); st.assignments = new[] { current.correspondence[1], current.correspondence[2], current.correspondence[0] };
            if (GeJimkkunRules.IsCorrect(current, st.assignments)) st.assignments = new[] { current.correspondence[1], current.correspondence[0], current.correspondence[2] };
            UpdateCrabPositions(); Submit();
        }

        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson()
        {
            var list = new List<MgfProblem>(bank.Count); for (int i = 0; i < bank.Count; i++) list.Add(GeJimkkunRules.ToMgf(bank[i])); return MgfJson.Bank(list);
        }

        void SetGroup(CanvasGroup g, bool on) { g.alpha = on ? 1 : 0; g.interactable = on; g.blocksRaycasts = on; }
        static void Center(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.anchoredPosition = pos; rt.sizeDelta = size; }

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size); var im = rt.gameObject.AddComponent<Image>(); im.sprite = sprite ?? roundedSprite; im.type = Image.Type.Sliced; im.color = color; im.raycastTarget = false; return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size); var im = rt.gameObject.AddComponent<Image>(); im.sprite = sprite ?? roundedSprite;
            im.type = sprite == roundedSprite ? Image.Type.Sliced : Image.Type.Simple; im.color = color; im.raycastTarget = false; return im;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color, float width)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 2.5f)); var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.fontStyle = FontStyles.Bold; t.color = color; t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false; t.text = text; return t;
        }

        static Sprite MakeRoundedSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                if (ring)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(1 - Mathf.Abs(d - size * .40f) / (size * .055f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * .5f) - (size * .5f - radius), 0), qy = Mathf.Max(Mathf.Abs(fy - size * .5f) - (size * .5f - radius), 0);
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true); float b = ring ? 0 : radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }
    }
}
