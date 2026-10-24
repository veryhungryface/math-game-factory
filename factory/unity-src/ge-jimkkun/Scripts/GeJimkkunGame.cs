// 게 짐꾼 — 세워지는 팝업 항로책의 2.5D 세계, 실제 드래그 입력, 진행·실패·브리지 정본.
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

    public sealed class PopupTriangleGraphic : MaskableGraphic
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

        // 목제 작업대·황동 부품 계열을 버리고, 얇은 채색 종이·먹선·실 접철 무대로 분화한다.
        static readonly Color Stage = MgfLook.Hex("#2B2458");
        static readonly Color Sun = MgfLook.Hex("#FFD45A");
        static readonly Color Page = MgfLook.Hex("#FFF0D2");
        static readonly Color Indigo = MgfLook.Hex("#171B3D");
        static readonly Color Ink = MgfLook.Hex("#102B3C");
        static readonly Color Coral = MgfLook.Hex("#F0645C");
        static readonly Color Mint = MgfLook.Hex("#55D6BE");
        static readonly Color White = MgfLook.Hex("#FFF9EA");

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
        Transform world, titleCrab3d, popupRoute3d, foldLeft3d, foldRight3d;
        readonly Transform[] pullThreads3d = new Transform[3];
        Material stageMat, sunMat, coralMat, pageMat, indigoMat, inkMat, mintMat;
        CanvasGroup titleGroup, playGroup, revealGroup, endGroup, feedbackGroup;
        RectTransform canvasRt, titleStart, sourceBoardRt, targetBoardRt, promptPanel, goalPanel, hudPanel, progressPanel;
        RectTransform endButton, guideRing, guideTarget, guideLine, guideHand, ripple, toastPanel;
        PopupTriangleGraphic sourceDiagram, targetDiagram;
        TextMeshProUGUI titleBest, promptText, goalText, hudText, progressText, scoreText, toastText;
        TextMeshProUGUI revealTitle, revealBody, endTitle, endStats, endScore;
        Sprite roundedSprite, paperSprite, softSprite, ringSprite;
        CargoProblem current;
        List<CargoProblem> deck;
        System.Random rng = new System.Random(20261025);
        Flow flow = Flow.Title;
        int runSerial, dragging = -1;
        float secondsLeft = GeJimkkunRules.RunSeconds, revealClock, titleClock, idleClock, scoreShown;
        float practiceNoProgressClock;
        float rippleClock = 9f, toastClock = 9f, wrongJolt, hitStop, foldEventClock = -1f;
        bool revealCorrect, failAfterReveal, tutorialFirstCarryDone;
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
            stageMat = MgfLook.Lit(Stage, .76f, .01f, new Color(.025f, .02f, .07f));
            sunMat = MgfLook.Lit(Sun, .44f, .02f);
            coralMat = MgfLook.Lit(Coral, .58f, .01f);
            pageMat = MgfLook.Lit(Page, .82f, .00f);
            indigoMat = MgfLook.Lit(Indigo, .70f, .01f);
            inkMat = MgfLook.Lit(Ink, .80f, .01f);
            mintMat = MgfLook.Lit(Mint, .54f, .01f);
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#4A3A86"), MgfLook.Hex("#251D4E"), Indigo, .72f);
            MgfLook.Sun(new Vector3(36, -24, -18), MgfLook.Hex("#FFF0C2"), 1.06f, .62f);
            cam = MgfLook.Camera(new Vector3(0, 10.8f, -18.8f), new Vector3(0, 1.6f, 3.6f), 34f);
            cam.orthographic = true; cam.orthographicSize = 8.2f;
            world = new GameObject("PopupRouteBookTheatre").transform;

            // 수평 작업대가 아니라 책등을 중심으로 좌우 면이 일어나고, 그 뒤에 항로 무대가 세워지는 구조다.
            var cover = MgfLook.Block("IndigoBookCover", new Vector3(0, -1.04f, 4.2f), new Vector3(15.8f, .34f, 12.4f), .05f, indigoMat, world).transform;
            cover.localRotation = Quaternion.Euler(0, 0, 0);
            var leftPage = MgfLook.Block("CoralPaperLeaf", new Vector3(-3.65f, -.76f, 3.7f), new Vector3(7.2f, .12f, 10.6f), .018f, coralMat, world).transform;
            leftPage.localRotation = Quaternion.Euler(0, 0, -3.5f);
            var rightPage = MgfLook.Block("MintPaperLeaf", new Vector3(3.65f, -.76f, 3.7f), new Vector3(7.2f, .12f, 10.6f), .018f, mintMat, world).transform;
            rightPage.localRotation = Quaternion.Euler(0, 0, 3.5f);
            MgfLook.Block("YellowThreadSpine", new Vector3(0, -.45f, 3.8f), new Vector3(.18f, .24f, 10.9f), .025f, sunMat, world);

            MakePaperWing(new Vector3(-4.2f, 1.2f, 5.9f), -9f, "DeparturePopupWing", coralMat);
            MakePaperWing(new Vector3(4.2f, 1.2f, 5.9f), 9f, "ArrivalPopupWing", mintMat);

            // 작은 접힌 표식과 먹실 항로가 책의 세로 공간을 채운다.
            for (int i = 0; i < 16; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * (5.4f + (i % 4) * .88f), y = .15f + (i % 5) * .66f, z = 2.1f + (i % 6) * 1.28f;
                var tab = MgfLook.Block("FoldedRouteTab" + i, new Vector3(x, y, z),
                    new Vector3(.56f + (i % 3) * .16f, .08f, 1.05f + (i % 2) * .34f), .012f,
                    i % 3 == 0 ? sunMat : i % 3 == 1 ? coralMat : mintMat, world).transform;
                tab.localRotation = Quaternion.Euler(side * (26 + i % 3 * 7), side * (8 + i % 4 * 6), side * 12); worldProps.Add(tab);
            }

            Vector3[] route = {
                new Vector3(-6.6f, .25f, 2.0f), new Vector3(-4.8f, 1.5f, 4.0f),
                new Vector3(-2.2f, .65f, 6.1f), new Vector3(0, 2.65f, 7.0f),
                new Vector3(2.4f, .85f, 6.0f), new Vector3(4.8f, 1.65f, 4.1f), new Vector3(6.7f, .35f, 2.2f)
            };
            for (int i = 0; i < route.Length - 1; i++)
                MakeThread("InkThreadRoute" + i, route[i], route[i + 1], .055f, i % 2 == 0 ? inkMat : sunMat, world);

            for (int i = 0; i < 10; i++)
            {
                float x = -6.5f + i * 1.45f, z = 1.2f + (i % 4) * 2.25f;
                var stitch = MgfLook.Prim(PrimitiveType.Cylinder, "ThreadStitch" + i,
                    new Vector3(x, -.42f, z), new Vector3(.08f, .14f, .08f), i % 2 == 0 ? sunMat : pageMat, world, false).transform;
                stitch.localRotation = Quaternion.Euler(0, 0, 90);
            }

            // 정답 때 세 실이 당기면 이 한 장이 가운데 책등에서 실제로 V자로 접혀 일어난다.
            popupRoute3d = new GameObject("AnswerPopupRoute").transform; popupRoute3d.SetParent(world, false);
            popupRoute3d.localPosition = new Vector3(0, -.25f, 7.6f);
            foldLeft3d = MgfLook.Block("AnswerFoldLeft", new Vector3(-1.25f, 0, 0), new Vector3(2.48f, .09f, 2.0f), .012f, pageMat, popupRoute3d).transform;
            foldRight3d = MgfLook.Block("AnswerFoldRight", new Vector3(1.25f, 0, 0), new Vector3(2.48f, .09f, 2.0f), .012f, pageMat, popupRoute3d).transform;
            MakeThread("AnswerInkLineLeft", new Vector3(-2.25f, .08f, 0), new Vector3(0, .08f, .55f), .04f, inkMat, popupRoute3d);
            MakeThread("AnswerInkLineRight", new Vector3(0, .08f, .55f), new Vector3(2.25f, .08f, 0), .04f, inkMat, popupRoute3d);
            for (int i = 0; i < 3; i++)
            {
                pullThreads3d[i] = MakeThread("VertexPullThread" + i,
                    new Vector3((i - 1) * 1.35f, .12f, 1.4f), new Vector3((i - 1) * .9f, 1.8f, 7.4f),
                    .045f, i == 1 ? sunMat : mintMat, world);
                pullThreads3d[i].gameObject.SetActive(false);
            }
            ResetFoldRoute();
            for (int i = 0; i < 3; i++)
            {
                var crab = MakeWorldCrab(new Vector3((i - 1) * 1.55f, .15f, 1.2f + (i % 2) * .25f), "PaperCrab" + i);
                worldCrabs.Add(crab);
            }
            titleCrab3d = worldCrabs[1];
        }

        void MakePaperWing(Vector3 pos, float yaw, string name, Material face)
        {
            var root = new GameObject(name).transform; root.SetParent(world, false); root.localPosition = pos; root.localRotation = Quaternion.Euler(0, yaw, 0);
            var sheet = MgfLook.Block("StandingPaperSheet", Vector3.zero, new Vector3(5.6f, 3.8f, .12f), .018f, face, root).transform;
            sheet.localRotation = Quaternion.Euler(-4, 0, 0);
            MgfLook.Block("DarkFoldHinge", new Vector3(0, -1.82f, -.10f), new Vector3(5.7f, .16f, .18f), .018f, indigoMat, root);
            for (int i = -2; i <= 2; i++)
            {
                var fold = MgfLook.Block("InkFold" + i, new Vector3(i * .92f, .1f + Mathf.Abs(i) * .18f, -.10f), new Vector3(.075f, 2.65f, .10f), .008f, i == 0 ? sunMat : inkMat, root).transform;
                fold.localRotation = Quaternion.Euler(0, 0, i * 7f);
            }
            var flag = MgfLook.Block("FoldedSignalFlag", new Vector3(0, 1.25f, -.22f), new Vector3(1.3f, .55f, .08f), .01f, pageMat, root).transform;
            flag.localRotation = Quaternion.Euler(0, 0, yaw * .7f); worldProps.Add(flag);
        }

        Transform MakeThread(string name, Vector3 a, Vector3 b, float radius, Material mat, Transform parent)
        {
            Vector3 d = b - a;
            var thread = MgfLook.Prim(PrimitiveType.Cylinder, name, (a + b) * .5f,
                new Vector3(radius, d.magnitude * .5f, radius), mat, parent, false).transform;
            thread.up = d.normalized;
            return thread;
        }

        Transform MakeWorldCrab(Vector3 pos, string name)
        {
            var root = new GameObject(name).transform; root.SetParent(world, false); root.localPosition = pos;
            MgfLook.Block("CoralFoldedBody", Vector3.zero, new Vector3(.88f, .24f, .62f), .012f, coralMat, root);
            var shell = MgfLook.Block("YellowDiamondFold", new Vector3(0, .38f, .10f), new Vector3(.76f, .55f, .68f), .012f, sunMat, root).transform;
            shell.localRotation = Quaternion.Euler(0, 45, 0);
            for (int s = -1; s <= 1; s += 2)
            {
                MgfLook.Prim(PrimitiveType.Cylinder, "InkEye" + s, new Vector3(s * .25f, .27f, -.48f), new Vector3(.09f, .09f, .09f), inkMat, root, false);
                var claw = MgfLook.Block("PaperClaw" + s, new Vector3(s * .65f, .06f, -.20f), new Vector3(.40f, .12f, .24f), .01f, coralMat, root).transform;
                claw.localRotation = Quaternion.Euler(0, 0, s * 18f);
            }
            return root;
        }

        void BuildUi()
        {
            roundedSprite = MakeRoundedSprite(64, 15, false); paperSprite = MakeRoundedSprite(64, 3, false); ringSprite = MakeRoundedSprite(64, 10, true);
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
            var sign = Panel(titleGroup.transform, "StandingPopupBanner", new Vector2(.5f, .73f), Vector2.zero, new Vector2(354, 210), new Color(Coral.r, Coral.g, Coral.b, .97f), paperSprite);
            var leftFold = Img(sign, "LeftFoldTab", new Vector2(.08f, .22f), Vector2.zero, new Vector2(76, 26), Sun, paperSprite).rectTransform;
            leftFold.localRotation = Quaternion.Euler(0, 0, 16);
            var rightFold = Img(sign, "RightFoldTab", new Vector2(.92f, .72f), Vector2.zero, new Vector2(86, 24), Mint, paperSprite).rectTransform;
            rightFold.localRotation = Quaternion.Euler(0, 0, -14);
            Img(sign, "BookSpineInk", new Vector2(.5f, .08f), Vector2.zero, new Vector2(300, 5), Indigo, paperSprite);
            var shadow = Txt(sign, "게 짐꾼", new Vector2(.5f, .63f), new Vector2(3, -4), 60, Indigo, 310); shadow.characterSpacing = 4;
            var logo = Txt(sign, "게 짐꾼", new Vector2(.5f, .63f), Vector2.zero, 60, White, 310); logo.characterSpacing = 4; logo.outlineColor = Ink; logo.outlineWidth = .13f;
            Txt(sign, "게를 대응점으로 옮겨라", new Vector2(.5f, .27f), Vector2.zero, 17, Indigo, 300);
            var start = CrabWidget(titleGroup.transform, "TitleCrabStart", "출", new Vector2(.5f, .32f), Vector2.zero, 1.48f, -1);
            titleStart = start; Img(start, "TitlePulse", new Vector2(.5f, .5f), Vector2.zero, new Vector2(142, 142), new Color(White.r, White.g, White.b, .28f), ringSprite).transform.SetAsFirstSibling();
            Txt(titleGroup.transform, "종이 게를 눌러 항로책 펼치기", new Vector2(.5f, .20f), Vector2.zero, 17, White, 310);
            Txt(titleGroup.transform, "중학교 1학년 · 작도와 합동", new Vector2(.5f, .075f), Vector2.zero, 13, Page, 330);
            titleBest = Txt(titleGroup.transform, "최고 첫 제출 0/8", new Vector2(.5f, .125f), Vector2.zero, 13, Mint, 260);
        }

        void BuildPlay()
        {
            hudPanel = Panel(playGroup.transform, "ThreadSpineHud", new Vector2(.5f, .5f), Vector2.zero, new Vector2(370, 48), new Color(Indigo.r, Indigo.g, Indigo.b, .96f), paperSprite);
            hudText = Txt(hudPanel, "지도 0/8 · 첫 제출 0/0 · 90초", new Vector2(.42f, .5f), Vector2.zero, 13, White, 265);
            scoreText = Txt(hudPanel, "0", new Vector2(.88f, .5f), Vector2.zero, 16, Coral, 60);
            for (int i = 0; i < 3; i++) knots[i] = Img(hudPanel, "SafetyThread" + i, new Vector2(.69f + i * .055f, .5f), Vector2.zero, new Vector2(13, 28), Sun, paperSprite);
            goalPanel = Panel(playGroup.transform, "PermanentGoal", new Vector2(.5f, .5f), Vector2.zero, new Vector2(368, 38), new Color(Coral.r, Coral.g, Coral.b, .95f), paperSprite);
            goalText = Txt(goalPanel, "게 토큰을 대응점에 옮기면 지도가 입체로 접힌다", new Vector2(.5f, .5f), Vector2.zero, 14, White, 352);
            promptPanel = Panel(playGroup.transform, "ConditionSheet", new Vector2(.5f, .5f), Vector2.zero, new Vector2(370, 82), new Color(Page.r, Page.g, Page.b, .96f), paperSprite);
            promptText = Txt(promptPanel, "", new Vector2(.5f, .57f), Vector2.zero, 12.5f, Ink, 350); promptText.enableAutoSizing = true; promptText.fontSizeMin = 9; promptText.fontSizeMax = 12.5f;
            Txt(promptPanel, "그림은 실제 비율과 다릅니다", new Vector2(.5f, .10f), Vector2.zero, 9.5f, Coral, 300);

            sourceBoardRt = Panel(playGroup.transform, "DeparturePaperLeaf", new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 220), new Color(Page.r, Page.g, Page.b, .94f), paperSprite);
            Color mintPage = Color.Lerp(Mint, White, .78f);
            targetBoardRt = Panel(playGroup.transform, "ArrivalPaperLeaf", new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 220), new Color(mintPage.r, mintPage.g, mintPage.b, .94f), paperSprite);
            sourceDiagram = Graphic(sourceBoardRt, "TriangleABC", false); targetDiagram = Graphic(targetBoardRt, "TriangleDEF", true);
            Txt(sourceBoardRt, "출발 △ABC", new Vector2(.5f, .94f), Vector2.zero, 13, Indigo, 160);
            Txt(targetBoardRt, "도착 △DEF", new Vector2(.5f, .94f), Vector2.zero, 13, Indigo, 160);
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
            progressPanel = Panel(playGroup.transform, "FoldLedger", new Vector2(.5f, .5f), Vector2.zero, new Vector2(370, 46), new Color(Indigo.r, Indigo.g, Indigo.b, .94f), paperSprite);
            progressText = Txt(progressPanel, "게 0/3 · 세 자리가 차면 자동 제출", new Vector2(.43f, .5f), Vector2.zero, 13, White, 270);
            for (int i = 0; i < 8; i++) raftPips[i] = Img(progressPanel, "FoldedRoute" + i, new Vector2(.72f + i * .032f, .5f), Vector2.zero, new Vector2(8, 21), i == 0 ? Mint : new Color(White.r, White.g, White.b, .22f), paperSprite);
            guideLine = Img(playGroup.transform, "ThreadCarryRoute", new Vector2(.5f, .5f), Vector2.zero, new Vector2(120, 8), new Color(Sun.r, Sun.g, Sun.b, .94f), paperSprite).rectTransform;
            guideRing = Img(playGroup.transform, "CrabGuideRing", new Vector2(.5f, .5f), Vector2.zero, new Vector2(88, 88), new Color(Coral.r, Coral.g, Coral.b, .45f), ringSprite).rectTransform;
            guideTarget = Img(playGroup.transform, "TargetGuideRing", new Vector2(.5f, .5f), Vector2.zero, new Vector2(92, 92), new Color(Coral.r, Coral.g, Coral.b, .60f), ringSprite).rectTransform;
            guideHand = Img(playGroup.transform, "MovingGhostHand", new Vector2(.5f, .5f), Vector2.zero, new Vector2(42, 54), new Color(White.r, White.g, White.b, .90f), softSprite).rectTransform;
            // 문제지는 도형판보다 위에 그리되 실제 조작 토큰과 안내선은 그보다 앞에 둔다.
            promptPanel.SetAsLastSibling();
            for (int i = 0; i < crabs.Length; i++) crabs[i].SetAsLastSibling();
            guideLine.SetAsLastSibling(); guideRing.SetAsLastSibling(); guideTarget.SetAsLastSibling(); guideHand.SetAsLastSibling();
        }

        PopupTriangleGraphic Graphic(RectTransform parent, string name, bool target)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), new Vector2(0, -5), new Vector2(310, 180));
            var g = rt.gameObject.AddComponent<PopupTriangleGraphic>(); g.Target = target; g.raycastTarget = false; return g;
        }

        void BuildReveal()
        {
            var card = Panel(revealGroup.transform, "ThreadMeasureReveal", new Vector2(.5f, .5f), Vector2.zero, new Vector2(352, 132), new Color(Indigo.r, Indigo.g, Indigo.b, .96f), paperSprite);
            revealTitle = Txt(card, "", new Vector2(.5f, .69f), Vector2.zero, 24, White, 325);
            revealBody = Txt(card, "", new Vector2(.5f, .31f), Vector2.zero, 13, Page, 326); revealBody.enableAutoSizing = true; revealBody.fontSizeMin = 10; revealBody.fontSizeMax = 13;
        }

        void BuildEnd()
        {
            var card = Panel(endGroup.transform, "PopupResultPage", new Vector2(.5f, .56f), Vector2.zero, new Vector2(360, 360), new Color(Page.r, Page.g, Page.b, .98f), paperSprite);
            endTitle = Txt(card, "", new Vector2(.5f, .76f), Vector2.zero, 30, Ink, 340); endTitle.outlineColor = Coral; endTitle.outlineWidth = .14f;
            endTitle.enableAutoSizing = true; endTitle.fontSizeMin = 22; endTitle.fontSizeMax = 30; endTitle.textWrappingMode = TextWrappingModes.NoWrap;
            endStats = Txt(card, "", new Vector2(.5f, .50f), Vector2.zero, 17, Indigo, 320);
            endScore = Txt(card, "0", new Vector2(.5f, .31f), Vector2.zero, 42, Coral, 260);
            endButton = Panel(card, "RestartFold", new Vector2(.5f, .12f), Vector2.zero, new Vector2(230, 62), Indigo, paperSprite);
            Txt(endButton, "게를 눌러 다시 싣기", new Vector2(.5f, .5f), Vector2.zero, 16, White, 214);
        }

        void BuildFeedback()
        {
            toastPanel = Panel(feedbackGroup.transform, "BookToast", new Vector2(.5f, .5f), Vector2.zero, new Vector2(350, 70), new Color(Indigo.r, Indigo.g, Indigo.b, .95f), paperSprite);
            toastText = Txt(toastPanel, "", new Vector2(.5f, .5f), Vector2.zero, 13, White, 330); toastText.enableAutoSizing = true; toastText.fontSizeMin = 10; toastText.fontSizeMax = 13;
            ripple = Img(feedbackGroup.transform, "InputPaperRipple", new Vector2(.5f, .5f), Vector2.zero, new Vector2(42, 42), new Color(White.r, White.g, White.b, .58f), ringSprite).rectTransform;
        }

        RectTransform CrabWidget(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, float scale, int index)
        {
            var root = R(name, parent, anchor, pos, new Vector2(78, 66)); root.localScale = Vector3.one * scale;
            Img(root, "CrabShadow", new Vector2(.5f, .5f), new Vector2(0, -20), new Vector2(72, 24), new Color(Ink.r, Ink.g, Ink.b, .22f), softSprite);
            Img(root, "LeftClaw", new Vector2(.16f, .50f), new Vector2(-3, -2), new Vector2(25, 15), Coral, paperSprite).rectTransform.localRotation = Quaternion.Euler(0, 0, -28);
            Img(root, "RightClaw", new Vector2(.84f, .50f), new Vector2(3, -2), new Vector2(25, 15), Coral, paperSprite).rectTransform.localRotation = Quaternion.Euler(0, 0, 28);
            Img(root, "FoldedPaperBody", new Vector2(.5f, .40f), Vector2.zero, new Vector2(57, 25), Coral, paperSprite);
            var shell = Img(root, "YellowDiamondFold", new Vector2(.5f, .63f), Vector2.zero, new Vector2(48, 48), Sun, paperSprite).rectTransform;
            shell.localRotation = Quaternion.Euler(0, 0, 45);
            Img(root, "InkFold", new Vector2(.5f, .66f), Vector2.zero, new Vector2(27, 5), new Color(Ink.r, Ink.g, Ink.b, .48f), paperSprite).rectTransform.localRotation = Quaternion.Euler(0, 0, -20);
            Img(root, "LeftEyeRivet", new Vector2(.39f, .37f), Vector2.zero, new Vector2(8, 8), Ink, roundedSprite);
            Img(root, "RightEyeRivet", new Vector2(.61f, .37f), Vector2.zero, new Vector2(8, 8), Ink, roundedSprite);
            var tag = Panel(root, "LetterTag", new Vector2(.5f, .54f), Vector2.zero, new Vector2(31, 31), Ink, paperSprite);
            Txt(tag, label, new Vector2(.5f, .5f), Vector2.zero, 15, White, 26);
            return root;
        }

        void Prewarm()
        {
            var sb = new StringBuilder(50000);
            sb.Append("게짐꾼게를대응점으로옮겨라출항도착출발연습실전배지도첫제출보호매듭남은시간수선성공실패다시싣기");
            sb.Append("종이항로책펼치기입체접기완성팝업극장실매듭");
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
            if (flow == Flow.Practice && st.tutorialStep == "A_to_F") practiceNoProgressClock += dt;
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
                var t = worldProps[i];
                float flutter = Mathf.Sin(titleClock * 1.35f + i * .73f) * (i < 16 ? 4.2f : 1.6f);
                Vector3 e = t.localEulerAngles; t.localRotation = Quaternion.Euler(e.x, e.y, flutter);
            }
            for (int i = 0; i < worldCrabs.Count; i++)
            {
                var t = worldCrabs[i]; t.localRotation = Quaternion.Euler(0, Mathf.Sin(titleClock * 1.7f + i) * 7f, Mathf.Sin(titleClock * 2.3f + i) * 2f);
                t.localScale = new Vector3(1f + Mathf.Sin(titleClock * 2.1f + i) * .025f, 1f - Mathf.Sin(titleClock * 2.1f + i) * .02f, 1f);
            }
            if (popupRoute3d)
            {
                if (foldEventClock >= 0)
                {
                    foldEventClock += dt;
                    float rise = Mathf.SmoothStep(0, 1, Mathf.Clamp01(foldEventClock / .70f));
                    float settle = Mathf.SmoothStep(0, 1, Mathf.Clamp01((foldEventClock - .70f) / .36f));
                    popupRoute3d.localPosition = new Vector3(0, Mathf.Lerp(-.25f, 1.15f, rise) - settle * .18f, 7.6f);
                    foldLeft3d.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, -56f, rise) + settle * 4f);
                    foldRight3d.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, 56f, rise) - settle * 4f);
                    float pull = Mathf.Lerp(.25f, 1f, rise);
                    for (int i = 0; i < pullThreads3d.Length; i++) pullThreads3d[i].localScale = new Vector3(pull, 1, pull);
                }
                else
                {
                    popupRoute3d.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(titleClock * 1.15f) * 1.2f);
                }
            }
        }

        void BeginFoldRoute()
        {
            foldEventClock = 0;
            for (int i = 0; i < pullThreads3d.Length; i++) pullThreads3d[i].gameObject.SetActive(true);
        }

        void ResetFoldRoute()
        {
            foldEventClock = -1f;
            if (!popupRoute3d) return;
            popupRoute3d.localPosition = new Vector3(0, -.25f, 7.6f);
            popupRoute3d.localRotation = Quaternion.identity;
            foldLeft3d.localRotation = Quaternion.identity;
            foldRight3d.localRotation = Quaternion.identity;
            for (int i = 0; i < pullThreads3d.Length; i++) if (pullThreads3d[i]) pullThreads3d[i].gameObject.SetActive(false);
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
            st.frozen = false; st.dragActive = false; st.draggingCrab = -1; dragging = -1; scoreShown = 0;
        }

        void BeginPractice()
        {
            ResetState(); flow = Flow.Practice; st.phase = "playing"; st.frozen = true; st.tutorialStep = "A_to_F";
            current = GeJimkkunRules.Practice(); secondsLeft = GeJimkkunRules.RunSeconds; tutorialFirstCarryDone = false;
            practiceNoProgressClock = 0;
            titleGroup.alpha = 0; titleGroup.blocksRaycasts = false; SetGroup(playGroup, true); SetGroup(revealGroup, false); SetGroup(endGroup, false);
            LoadCurrent(); goalText.text = "게 토큰을 대응점에 옮기면 지도가 입체로 접힌다"; toastClock = 99; Notify();
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
            ResetFoldRoute();
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

        void PositionBoardLabels(PopupTriangleGraphic diagram, TextMeshProUGUI[] vertices, TextMeshProUGUI[] edges, TextMeshProUGUI[] angles)
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
                    // 첫 화면 전체가 하나의 팝업 항로책이다. 첫 탭을 놓치지 않게 어느 지점에서든 연습으로 연다.
                    MgfSfx.Play("pop", .42f); BeginPractice();
                    return;
                }
                if (flow == Flow.End)
                {
                    if (Contains(endButton, MgfPointer.Position, 12)) BeginPractice(); else Refuse("가운데 조개판을 눌러 다시 시작하시오.", local, false);
                    return;
                }
                if (flow == Flow.Reveal) { Refuse("밧줄이 조건을 확인하는 동안 잠시 보시오.", local, false); return; }
                if (flow != Flow.Playing && flow != Flow.Practice) return;
                bool tutorialAssist = flow == Flow.Practice && st.tutorialStep == "A_to_F" && practiceNoProgressClock >= 8f;
                int hit = HitCrab(local);
                // 무진도 8초 뒤에는 다음 입력을 유령 경로의 시작점으로 흡착한다.
                // 실제 드래그는 경로 끝에 스냅하고, 탭만 반복되면 Up 단계의 짧은 시범으로 이어진다.
                if (tutorialAssist) hit = 0;
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
                crabs[crab].localScale = Vector3.one;
                if (dragDistance < 7f)
                {
                    // 유령 경로가 8초 이상 반복됐는데도 이동이 없으면 같은 링의 탭도 경로 시범으로 받아들인다.
                    // 실전 문항에는 적용하지 않아 실제 대응 판단과 드래그 규칙은 그대로 남는다.
                    if (flow == Flow.Practice && st.tutorialStep == "A_to_F" && practiceNoProgressClock >= 8f)
                    { DropCrab(crab, TargetPoint(2)); return; }
                    ReturnCrab(crab); Refuse("게를 누른 채 대응하는 꼭짓점까지 끌어 놓으시오.", ToCanvas(MgfPointer.Position), true); return;
                }
                DropCrab(crab, ToCanvas(MgfPointer.Position));
            }
        }

        void BeginDrag(int crab, Vector2 local)
        {
            dragging = crab; st.dragActive = true; st.draggingCrab = crab; dragStart = local; dragDistance = 0;
            dragOffset = crabs[crab].anchoredPosition - local;
            if (st.assignments[crab] >= 0) st.assignments[crab] = -1;
            crabs[crab].SetAsLastSibling(); MgfSfx.Play("tap", .22f); MgfFx.Punch(crabs[crab], .10f, .18f); Notify();
        }

        void DropCrab(int crab, Vector2 local)
        {
            int nearest = -1; float best = 9999;
            bool tutorialSnap = flow == Flow.Practice && st.tutorialStep == "A_to_F" && crab == 0 && practiceNoProgressClock >= 8f;
            for (int i = 0; i < 3; i++)
            {
                float d = Vector2.Distance(local, TargetPoint(i)); if (d < best) { best = d; nearest = i; }
            }
            if (tutorialSnap) { nearest = 2; best = 0; }
            if (best > 72f) { ReturnCrab(crab); Refuse("꼭짓점 자리 안에 놓으시오.", local, true); return; }
            for (int i = 0; i < 3; i++) if (i != crab && st.assignments[i] == nearest)
            { ReturnCrab(crab); Refuse("이미 게가 있습니다. 빈자리로 옮기시오.", local, true); return; }
            st.assignments[crab] = nearest; crabs[crab].anchoredPosition = TargetPoint(nearest);
            crabs[crab].localScale = new Vector3(1.10f, .90f, 1); MgfFx.Punch(crabs[crab], .15f, .24f); MgfSfx.Play("pop", .26f);
            if (flow == Flow.Practice && !tutorialFirstCarryDone && crab == 0 && nearest == 2)
            {
                tutorialFirstCarryDone = true; st.tutorialStep = "crew_demo"; practiceNoProgressClock = 0;
                Toast("A와 F가 대응합니다. 나머지 두 토큰도 같은 조건으로 맞춥니다.", 1.6f);
                goalText.text = "같은 길이가 만나는 꼭짓점끼리 대응한다";
                // 첫 성공 뒤 나머지 두 대응은 짧은 승무원 시범으로 완성해 연습 정체를 막는다.
                st.assignments[1] = current.correspondence[1]; st.assignments[2] = current.correspondence[2];
                UpdateCrabPositions(); progressText.text = "연습 3/3 · 밧줄 조건 확인"; Notify(); Submit(); return;
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
                revealCorrect = true; failAfterReveal = false; hitStop = .075f; BeginFoldRoute();
                revealTitle.text = practice ? "대응 완성 · SSS 합동" : "항로 접기 성공 · " + current.kind + " 합동";
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
                if (revealCorrect) { BeginRun(false); goalText.text = "게 토큰을 대응점에 옮기면 지도가 입체로 접힌다"; }
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
            endTitle.text = clear ? "입체 항로책 완성" : reason == "time" ? "접기 시간이 끝났습니다" : reason == "knots" ? "실 매듭이 모두 풀렸습니다" : "첫 제출 기준 미달";
            endStats.text = "완성 지도 " + st.solved + "/8\n첫 제출 " + st.firstAttemptCorrect + "/" + st.firstAttemptCount + " · 매듭 " + st.lives + "/3";
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
            float pulse = 1f + Mathf.Sin(titleClock * (practiceNoProgressClock > 8 ? 9f : 5f)) * .12f;
            guideRing.anchoredPosition = a; guideTarget.anchoredPosition = b; guideRing.localScale = guideTarget.localScale = Vector3.one * pulse;
            float travel = Mathf.PingPong(titleClock * .65f, 1f); guideHand.anchoredPosition = Vector2.Lerp(a, b, Mathf.SmoothStep(0, 1, travel)) + new Vector2(26, 18);
        }

        void UpdateHud(float dt)
        {
            if (flow == Flow.Title) { titleStart.localScale = Vector3.one * (1.48f + Mathf.Sin(titleClock * 3.2f) * .035f); return; }
            scoreShown = Mathf.MoveTowards(scoreShown, st.score, Mathf.Max(70, Mathf.Abs(st.score - scoreShown) * 8f) * dt);
            if (flow == Flow.End) { endScore.text = Mathf.RoundToInt(scoreShown).ToString("N0") + "점"; return; }
            if (flow == Flow.Practice) hudText.text = "연습 · 시계 정지 · 매듭 안전";
            else hudText.text = "지도 " + st.solved + "/8 · 첫 제출 " + st.firstAttemptCorrect + "/" + st.firstAttemptCount + " · " + Mathf.CeilToInt(secondsLeft) + "초";
            scoreText.text = Mathf.RoundToInt(scoreShown).ToString("N0");
            for (int i = 0; i < 3; i++) knots[i].color = i < st.lives ? Sun : new Color(Coral.r, Coral.g, Coral.b, .24f);
            for (int i = 0; i < 8; i++) raftPips[i].color = i < st.solved ? Mint : new Color(White.r, White.g, White.b, .22f);
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
                float boardW = Mathf.Min(430, (w - 72) * .5f);
                Center(hudPanel, new Vector2(0, h * .5f - 30), new Vector2(Mathf.Min(w - 40, 680), 52));
                Center(goalPanel, new Vector2(0, h * .5f - 83), new Vector2(Mathf.Min(w - 80, 600), 40));
                const float promptH = 84f;
                float promptY = h * .5f - 150, boardTop = promptY - promptH * .5f - 24;
                float boardBottom = -h * .5f + 80, boardH = Mathf.Clamp(boardTop - boardBottom, 110, 320);
                float boardY = (boardTop + boardBottom) * .5f, boardX = boardW * .5f + 18;
                Center(promptPanel, new Vector2(0, promptY), new Vector2(Mathf.Min(w - 120, 720), promptH));
                Center(sourceBoardRt, new Vector2(-boardX, boardY), new Vector2(boardW, boardH));
                Center(targetBoardRt, new Vector2(boardX, boardY), new Vector2(boardW, boardH));
                Center(progressPanel, new Vector2(0, -h * .5f + 32), new Vector2(Mathf.Min(w - 80, 620), 48));
                toastPanel.anchoredPosition = new Vector2(0, h * .5f - 210);
                cam.orthographicSize = 7.0f;
            }
            else
            {
                bool tabletPortrait = w >= 500;
                float boardW = tabletPortrait ? Mathf.Min(460, w - 44) : Mathf.Min(348, w - 34);
                Center(hudPanel, new Vector2(0, h * .5f - 28), new Vector2(Mathf.Min(374, w - 16), 48));
                Center(goalPanel, new Vector2(0, h * .5f - 74), new Vector2(Mathf.Min(370, w - 20), 36));
                const float promptH = 82f;
                float promptY = h * .5f - 137, boardTop = promptY - promptH * .5f - 24;
                float boardBottom = -h * .5f + 76;
                float boardH = Mathf.Min(tabletPortrait ? 230 : 220, Mathf.Max(180, (boardTop - boardBottom - 24) * .5f));
                Center(promptPanel, new Vector2(0, promptY), new Vector2(Mathf.Min(tabletPortrait ? 480 : 370, w - 20), promptH));
                Center(targetBoardRt, new Vector2(0, boardTop - boardH * .5f), new Vector2(boardW, boardH));
                Center(sourceBoardRt, new Vector2(0, boardBottom + boardH * .5f), new Vector2(boardW, boardH));
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
            float radius = flow == Flow.Practice && st.tutorialStep == "A_to_F" ? 82f : 54f;
            for (int i = 0; i < 3; i++) { float d = Vector2.Distance(local, crabs[i].anchoredPosition); if (d < radius && d < dist) { dist = d; best = i; } }
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
