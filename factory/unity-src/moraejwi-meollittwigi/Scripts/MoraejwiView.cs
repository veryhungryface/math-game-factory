using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.MoraejwiMeollittwigi
{
    // 붉은 협곡 디오라마와 평면 측량판. 모든 오브젝트는 부팅 때 만들고 재사용한다.
    public sealed class MoraejwiView
    {
        static readonly Color Rust = MgfLook.Hex("#CC7055");
        static readonly Color Cream = MgfLook.Hex("#F8E9CC");
        static readonly Color Sage = MgfLook.Hex("#647954");
        static readonly Color Brown = MgfLook.Hex("#583B31");
        static readonly Color Clay = MgfLook.Hex("#E2B59A");
        static readonly Color Sun = MgfLook.Hex("#FFD7A0");
        static readonly Color DeepRust = MgfLook.Hex("#8F493D");
        static readonly Color Correct = MgfLook.Hex("#466E54");
        static readonly Color Wrong = MgfLook.Hex("#8B3F3A");

        readonly MoraejwiMeollittwigiGame game;
        readonly Camera cam;
        readonly Transform worldRoot;
        readonly Transform canyonRoot;
        readonly Transform boardRoot;
        readonly Transform mouseRoot;
        readonly Transform mouseBody;
        readonly Transform head;
        readonly Transform earL;
        readonly Transform earR;
        readonly Transform cheek;
        readonly Transform[] paws = new Transform[4];
        readonly LineRenderer tail;
        readonly LineRenderer lineAC;
        readonly LineRenderer lineCB;
        readonly LineRenderer lineAB;
        readonly LineRenderer rightAngle;
        readonly LineRenderer guideWorldPath;
        readonly LineRenderer[] gridLines = new LineRenderer[32];
        readonly Transform[] windBits = new Transform[10];
        readonly Transform[] plants = new Transform[8];
        readonly Transform[] footprints = new Transform[16];
        readonly Transform[] seeds = new Transform[8];
        readonly Transform[] ropeKnots = new Transform[3];

        readonly Canvas canvas;
        readonly RectTransform rootRt;
        readonly GameObject titleGroup;
        readonly GameObject playGroup;
        readonly GameObject guideGroup;
        readonly GameObject revealGroup;
        readonly GameObject endGroup;
        readonly RectTransform titleCta;
        readonly RectTransform restartCta;
        readonly RectTransform promptPanel;
        readonly RectTransform evidencePanel;
        readonly RectTransform magnifier;
        readonly RectTransform ripple;
        readonly RectTransform guideRing;
        readonly RectTransform guideTarget;
        readonly RectTransform guidePath;
        readonly RectTransform guideFinger;
        readonly CanvasGroup toastGroup;
        readonly Image rippleImage;
        readonly Image guideRingImage;
        readonly Image guideTargetImage;
        readonly Image revealPanelImage;
        readonly TextMeshProUGUI titleLogo;
        readonly TextMeshProUGUI goalText;
        readonly TextMeshProUGUI promptText;
        readonly TextMeshProUGUI targetText;
        readonly TextMeshProUGUI progressText;
        readonly TextMeshProUGUI scoreText;
        readonly TextMeshProUGUI livesText;
        readonly TextMeshProUGUI timerText;
        readonly TextMeshProUGUI measureText;
        readonly TextMeshProUGUI squareTableText;
        readonly TextMeshProUGUI magnifierText;
        readonly TextMeshProUGUI toastText;
        readonly TextMeshProUGUI revealTitleText;
        readonly TextMeshProUGUI revealEquationText;
        readonly TextMeshProUGUI revealReasonText;
        readonly TextMeshProUGUI endTitleText;
        readonly TextMeshProUGUI endStatsText;
        readonly TextMeshProUGUI endScoreText;
        readonly TextMeshProUGUI labelA;
        readonly TextMeshProUGUI labelB;
        readonly TextMeshProUGUI labelC;
        readonly Image[] revealTiles = new Image[25];
        readonly Image[] knotIcons = new Image[3];
        readonly Image[] seedIcons = new Image[8];

        LandingProblem current;
        Vector2 previewGrid;
        Vector3 mouseHome;
        Vector3 revealFrom;
        Vector3 revealTo;
        float worldClock;
        float titlePress;
        float cameraBlend;
        float revealClock;
        float toastClock;
        float rippleClock;
        float guideClock;
        float scoreShown;
        float endScoreShown;
        int lastSecond = -1;
        int lastScore = -1;
        int lastW;
        int lastH;
        bool targetPlayCamera;
        bool revealActive;
        bool revealCorrect;
        bool revealPractice;
        bool dragging;
        bool guideStrong;
        bool wide;
        bool portraitTablet;
        Sprite roundedSprite;
        Sprite circleSprite;

        const float BoardY = 0.18f;
        const float BoardSpan = 7.2f;

        public MoraejwiView(MoraejwiMeollittwigiGame game)
        {
            this.game = game;
            MgfLook.Sky(MgfLook.Hex("#E9B58B"), MgfLook.Hex("#F4D7B5"), Rust, .82f);
            MgfLook.Sun(new Vector3(42f, -32f, 0f), Sun, 1.23f, .62f);
            cam = MgfLook.Camera(new Vector3(9.2f, 9.1f, -11.8f), new Vector3(0, .5f, 1.2f), 35f);
            cam.orthographic = true;
            cam.orthographicSize = 7.2f;
            cam.backgroundColor = Rust;

            worldRoot = new GameObject("Rust Canyon World").transform;
            canyonRoot = new GameObject("Burrow Canyon").transform;
            canyonRoot.SetParent(worldRoot, false);
            BuildWorld();
            boardRoot = BuildBoard();
            mouseRoot = BuildMouse();
            mouseBody = mouseRoot.Find("Body");
            head = mouseRoot.Find("Head");
            earL = head.Find("Ear L");
            earR = head.Find("Ear R");
            cheek = head.Find("Cheek");
            for (int i = 0; i < 4; i++) paws[i] = mouseRoot.Find("Paw " + i);
            tail = mouseRoot.GetComponent<LineRenderer>();
            lineAC = MakeLine("AC ivory survey groove", Cream, .055f, 2, boardRoot);
            lineCB = MakeLine("CB ivory survey groove", Cream, .055f, 2, boardRoot);
            lineAB = MakeLine("AB sage hypotenuse", Sage, .075f, 2, boardRoot);
            rightAngle = MakeLine("Right angle", Brown, .052f, 3, boardRoot);
            guideWorldPath = MakeLine("Practice drag trail", Cream, .04f, 16, boardRoot);
            guideWorldPath.textureMode = LineTextureMode.Stretch;
            guideWorldPath.enabled = false;

            canvas = MgfText.Canvas;
            rootRt = canvas.GetComponent<RectTransform>();
            BuildSprites();
            titleGroup = Group("Title World UI");
            playGroup = Group("Play UI");
            guideGroup = Group("Practice Guide UI");
            revealGroup = Group("Evidence Reveal UI");
            endGroup = Group("Result UI");

            BuildTitleUi(out titleCta, out titleLogo);
            BuildPlayUi(out promptPanel, out evidencePanel, out goalText, out promptText, out targetText,
                out progressText, out scoreText, out livesText, out timerText, out measureText,
                out squareTableText, out labelA, out labelB, out labelC, out magnifier, out magnifierText,
                out toastGroup, out toastText, out ripple, out rippleImage);
            BuildGuideUi(out guideRing, out guideRingImage, out guideTarget, out guideTargetImage,
                out guidePath, out guideFinger);
            BuildRevealUi(out revealPanelImage, out revealTitleText, out revealEquationText, out revealReasonText);
            BuildEndUi(out restartCta, out endTitleText, out endStatsText, out endScoreText);
            ApplyLayout(true);
        }

        void BuildWorld()
        {
            var rustMat = MgfLook.Lit(Rust, .13f);
            var deepMat = MgfLook.Lit(DeepRust, .08f);
            var clayMat = MgfLook.Lit(Clay, .18f);
            var creamMat = MgfLook.Lit(Cream, .28f);
            var sageMat = MgfLook.Lit(Sage, .22f);
            var darkMat = MgfLook.Lit(Brown * .35f, .05f);
            MgfLook.Block("Canyon floor", new Vector3(0, -.72f, 1.3f), new Vector3(25, 1.2f, 25), .55f, rustMat, canyonRoot);
            // 붉은 풍식 협곡: 좌우 높이와 실루엣을 달리해 탁자/매트처럼 보이지 않게 한다.
            float[] leftY = { 1.2f, 2.0f, 1.45f, 2.65f, 1.75f, 2.3f };
            float[] rightY = { 2.35f, 1.55f, 2.85f, 1.35f, 2.15f, 1.7f };
            for (int i = 0; i < 6; i++)
            {
                float z = -7.5f + i * 3.2f;
                MgfLook.Block("Left wind wall " + i, new Vector3(-7.2f, leftY[i] * .5f - .1f, z),
                    new Vector3(5.7f, leftY[i], 3.6f), .65f, i % 2 == 0 ? deepMat : rustMat, canyonRoot);
                MgfLook.Block("Right wind wall " + i, new Vector3(7.2f, rightY[i] * .5f - .1f, z + .8f),
                    new Vector3(5.9f, rightY[i], 3.8f), .72f, i % 2 == 0 ? rustMat : deepMat, canyonRoot);
            }
            // 굴 입구와 바느질 천막.
            for (int side = -1; side <= 1; side += 2)
            {
                var cave = MgfLook.Prim(PrimitiveType.Sphere, side < 0 ? "West burrow" : "Seed burrow",
                    new Vector3(side * 4.95f, .45f, 3.8f), new Vector3(2.2f, 1.55f, .65f), darkMat, canyonRoot, false).transform;
                cave.localRotation = Quaternion.Euler(0, side < 0 ? 12 : -12, 0);
                var awning = MgfLook.Block("Stitched awning", new Vector3(side * 4.9f, 1.55f, 3.55f),
                    new Vector3(2.9f, .18f, 1.25f), .12f, creamMat, canyonRoot).transform;
                awning.localRotation = Quaternion.Euler(0, side * 8f, side * 4f);
                for (int s = -1; s <= 1; s += 2)
                    MgfLook.Prim(PrimitiveType.Sphere, "Peeking ear", new Vector3(side * 4.8f + s * .3f, 1.0f, 3.2f),
                        new Vector3(.34f, .5f, .24f), creamMat, canyonRoot, false);
            }
            // 다육식물은 세 잎짜리 작은 실루엣으로 반복한다.
            for (int i = 0; i < plants.Length; i++)
            {
                var p = new GameObject("Succulent " + i).transform;
                p.SetParent(canyonRoot, false);
                float side = i % 2 == 0 ? -1f : 1f;
                p.position = new Vector3(side * (4.0f + (i % 3) * .7f), .18f, -4.7f + i * 1.35f);
                for (int k = 0; k < 3; k++)
                {
                    var leaf = MgfLook.Prim(PrimitiveType.Capsule, "Leaf", new Vector3((k - 1) * .22f, .36f, 0),
                        new Vector3(.2f, .38f + k * .04f, .2f), sageMat, p, false).transform;
                    leaf.localRotation = Quaternion.Euler(0, 0, (k - 1) * -28f);
                }
                plants[i] = p;
            }
            // 바람 속 모래 조각. 생성/삭제 없이 위치만 순환한다.
            for (int i = 0; i < windBits.Length; i++)
            {
                windBits[i] = MgfLook.Prim(PrimitiveType.Sphere, "Wind grain " + i,
                    new Vector3(-7 + i * 1.4f, .4f + (i % 3) * .55f, -5 + (i * 1.9f) % 11f),
                    new Vector3(.08f, .035f, .035f), creamMat, canyonRoot, false).transform;
            }
            // 경로에 남는 발자국과 씨앗은 풀링한다.
            for (int i = 0; i < footprints.Length; i++)
            {
                footprints[i] = MgfLook.Prim(PrimitiveType.Sphere, "Footprint " + i, Vector3.zero,
                    new Vector3(.16f, .035f, .24f), deepMat, canyonRoot, false).transform;
                footprints[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < seeds.Length; i++)
            {
                seeds[i] = MgfLook.Prim(PrimitiveType.Sphere, "Seed " + i, new Vector3(5.2f + (i % 3) * .22f, .25f + (i / 3) * .12f, 3.0f),
                    new Vector3(.20f, .12f, .13f), sageMat, canyonRoot, false).transform;
                seeds[i].localRotation = Quaternion.Euler(0, i * 31f, 18f);
                seeds[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < ropeKnots.Length; i++)
            {
                var knot = new GameObject("Safety knot " + i).transform;
                knot.SetParent(canyonRoot, false);
                knot.position = new Vector3(-5.4f + i * .48f, .62f, 3.0f);
                var a = MgfLook.Prim(PrimitiveType.Capsule, "Rope A", Vector3.zero, new Vector3(.08f, .28f, .08f), creamMat, knot, false).transform;
                var b = MgfLook.Prim(PrimitiveType.Capsule, "Rope B", Vector3.zero, new Vector3(.08f, .28f, .08f), creamMat, knot, false).transform;
                a.localRotation = Quaternion.Euler(0, 0, 45); b.localRotation = Quaternion.Euler(0, 0, -45);
                ropeKnots[i] = knot;
            }
        }

        Transform BuildBoard()
        {
            var root = new GameObject("Top-down survey ground").transform;
            root.SetParent(worldRoot, false);
            root.position = new Vector3(0, 0, 1.15f);
            var boardMat = MgfLook.Lit(MgfLook.Hex("#B95E49"), .12f);
            MgfLook.Block("Chalky terracotta survey field", new Vector3(0, -.12f, 0), new Vector3(8.25f, .3f, 8.25f), .35f, boardMat, root);
            var rimMat = MgfLook.Lit(Clay, .18f);
            MgfLook.Block("North survey rim", new Vector3(0, .02f, 4.18f), new Vector3(8.7f, .32f, .25f), .11f, rimMat, root);
            MgfLook.Block("South survey rim", new Vector3(0, .02f, -4.18f), new Vector3(8.7f, .32f, .25f), .11f, rimMat, root);
            MgfLook.Block("East survey rim", new Vector3(4.18f, .02f, 0), new Vector3(.25f, .32f, 8.7f), .11f, rimMat, root);
            MgfLook.Block("West survey rim", new Vector3(-4.18f, .02f, 0), new Vector3(.25f, .32f, 8.7f), .11f, rimMat, root);
            for (int i = 0; i < gridLines.Length; i++)
                gridLines[i] = MakeLine("Ivory grid " + i, new Color(Cream.r, Cream.g, Cream.b, .72f), .027f, 2, root);
            root.gameObject.SetActive(false);
            return root;
        }

        Transform BuildMouse()
        {
            var root = new GameObject("Original cream desert mouse").transform;
            root.SetParent(worldRoot, false);
            var creamMat = MgfLook.Lit(Cream, .36f);
            var innerMat = MgfLook.Lit(Clay, .28f);
            var darkMat = MgfLook.Lit(Brown, .32f);
            var body = MgfLook.Prim(PrimitiveType.Sphere, "Body", new Vector3(0, .42f, 0), new Vector3(1.35f, .83f, .95f), creamMat, root).transform;
            var headT = MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0, .52f, .56f), new Vector3(.95f, .78f, .82f), creamMat, root).transform;
            var left = MgfLook.Prim(PrimitiveType.Sphere, "Ear L", new Vector3(-.31f, .42f, .03f), new Vector3(.43f, .52f, .25f), innerMat, headT, false).transform;
            var right = MgfLook.Prim(PrimitiveType.Sphere, "Ear R", new Vector3(.31f, .42f, .03f), new Vector3(.43f, .52f, .25f), innerMat, headT, false).transform;
            MgfLook.Prim(PrimitiveType.Sphere, "Eye L", new Vector3(-.23f, .1f, .37f), Vector3.one * .11f, darkMat, headT, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Eye R", new Vector3(.23f, .1f, .37f), Vector3.one * .11f, darkMat, headT, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Nose", new Vector3(0, -.04f, .49f), Vector3.one * .13f, darkMat, headT, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Cheek", new Vector3(.39f, -.02f, .24f), new Vector3(.30f, .23f, .28f), creamMat, headT, false);
            for (int i = 0; i < paws.Length; i++)
            {
                float x = i % 2 == 0 ? -.38f : .38f;
                float z = i < 2 ? .30f : -.34f;
                paws[i] = MgfLook.Prim(PrimitiveType.Sphere, "Paw " + i, new Vector3(x, .08f, z),
                    new Vector3(.30f, .14f, .38f), innerMat, root, false).transform;
            }
            var lr = root.gameObject.AddComponent<LineRenderer>();
            lr.name = "Long rounded tail";
            lr.sharedMaterial = MgfLook.Unlit(Cream);
            lr.useWorldSpace = false;
            lr.positionCount = 8;
            lr.widthCurve = new AnimationCurve(new Keyframe(0, .18f), new Keyframe(.8f, .14f), new Keyframe(1, .25f));
            lr.numCapVertices = 6;
            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                lr.SetPosition(i, new Vector3(-.45f - t * 1.2f, .35f + Mathf.Sin(t * Mathf.PI) * .18f, -.28f - t * .55f));
            }
            root.localScale = Vector3.one * .9f;
            return root;
        }

        LineRenderer MakeLine(string name, Color color, float width, int count, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = MgfLook.Unlit(color);
            lr.useWorldSpace = true;
            lr.positionCount = count;
            lr.startWidth = lr.endWidth = width;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 3;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return lr;
        }

        void BuildSprites()
        {
            roundedSprite = MakeSprite(false);
            circleSprite = MakeSprite(true);
        }

        Sprite MakeSprite(bool circle)
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = circle ? "Circle UI" : "Rounded cloth UI", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float nx = (x + .5f) / S * 2 - 1, ny = (y + .5f) / S * 2 - 1;
                bool on;
                if (circle) on = nx * nx + ny * ny <= 1f;
                else
                {
                    float ax = Mathf.Abs(nx), ay = Mathf.Abs(ny), r = .24f;
                    float dx = Mathf.Max(0, ax - (1 - r)), dy = Mathf.Max(0, ay - (1 - r));
                    on = dx * dx + dy * dy <= r * r;
                }
                px[y * S + x] = on ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), Vector2.one * .5f, 64, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
        }

        GameObject Group(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(rootRt, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        Image Panel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = Vector2.one * .5f; rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.sprite = sprite ? sprite : roundedSprite; img.type = Image.Type.Sliced; img.color = color; img.raycastTarget = false;
            return img;
        }

        TextMeshProUGUI Text(string name, Transform parent, string value, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = Vector2.one * .5f; rt.anchoredPosition = pos; rt.sizeDelta = size;
            var t = go.GetComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = fontSize; t.fontStyle = FontStyles.Bold;
            t.color = color; t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = value;
            return t;
        }

        void BuildTitleUi(out RectTransform cta, out TextMeshProUGUI logo)
        {
            var banner = Panel("Stitched canyon banner", titleGroup.transform, new Vector2(.5f, 1), new Vector2(0, -137), new Vector2(358, 188), Cream);
            Panel("Banner rust stripe", banner.transform, new Vector2(.5f, .5f), new Vector2(0, 62), new Vector2(322, 9), DeepRust);
            Panel("Banner sage stitch", banner.transform, new Vector2(.5f, .5f), new Vector2(0, -66), new Vector2(322, 6), Sage);
            logo = Text("Logo", banner.transform, "모래쥐\n멀리뛰기", new Vector2(.5f, .5f), new Vector2(0, 8), new Vector2(330, 132), 51, Brown);
            logo.characterSpacing = -2f; logo.lineSpacing = -15f;
            logo.outlineColor = DeepRust; logo.outlineWidth = .16f;
            Text("Tagline", titleGroup.transform, "착지점을 끌어라", new Vector2(.5f, .5f), new Vector2(0, 93), new Vector2(300, 40), 21, Cream);
            var c = Panel("Open the burrow CTA", titleGroup.transform, new Vector2(.5f, 0), new Vector2(0, 78), new Vector2(348, 72), Sage);
            cta = c.rectTransform;
            Text("CTA label", c.transform, "모래쥐를 눌러 굴길 열기", new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 62), 23, Cream);
            Text("Title meta", titleGroup.transform, "중학교 2학년 · 피타고라스 정리 · 최고 첫 제출 0/8", new Vector2(.5f, 0), new Vector2(0, 28), new Vector2(370, 34), 13, Brown);
            Text("Title hint", titleGroup.transform, "협곡 속 모래쥐도 시작 스위치입니다", new Vector2(.5f, .5f), new Vector2(0, 145), new Vector2(340, 32), 14, Brown);
        }

        void BuildPlayUi(out RectTransform promptRt, out RectTransform evidenceRt,
            out TextMeshProUGUI goal, out TextMeshProUGUI prompt, out TextMeshProUGUI target,
            out TextMeshProUGUI progress, out TextMeshProUGUI score, out TextMeshProUGUI lives,
            out TextMeshProUGUI timer, out TextMeshProUGUI measure, out TextMeshProUGUI squareTable,
            out TextMeshProUGUI aLabel, out TextMeshProUGUI bLabel, out TextMeshProUGUI cLabel,
            out RectTransform magRt, out TextMeshProUGUI magText,
            out CanvasGroup toastG, out TextMeshProUGUI toast, out RectTransform rippleRt, out Image rippleImg)
        {
            var top = Panel("Ivory field note", playGroup.transform, new Vector2(.5f, 1), new Vector2(0, -60), new Vector2(370, 108), new Color(Cream.r, Cream.g, Cream.b, .96f));
            goal = Text("Goal", top.transform, "착지점을 맞추면 모래쥐가 전진한다", new Vector2(.5f, 1), new Vector2(0, -17), new Vector2(350, 30), 15, Brown);
            progress = Text("Progress", top.transform, "연습", new Vector2(0, .5f), new Vector2(63, -7), new Vector2(105, 42), 19, Brown);
            score = Text("Score", top.transform, "0점", new Vector2(.5f, .5f), new Vector2(0, -7), new Vector2(95, 42), 20, Brown);
            lives = Text("Ropes", top.transform, "끈 ●●●", new Vector2(1, .5f), new Vector2(-64, -7), new Vector2(116, 42), 18, Wrong);
            timer = Text("Timer", top.transform, "120초", new Vector2(.5f, 0), new Vector2(0, 9), new Vector2(110, 28), 15, Brown);
            for (int i = 0; i < knotIcons.Length; i++)
            {
                knotIcons[i] = Panel("Knot icon " + i, top.transform, new Vector2(1, .5f), new Vector2(-104 + i * 22, -7), new Vector2(16, 16), Sage, circleSprite);
            }
            for (int i = 0; i < seedIcons.Length; i++)
            {
                seedIcons[i] = Panel("Seed progress " + i, top.transform, new Vector2(.5f, 0), new Vector2(-77 + i * 22, 8), new Vector2(13, 13), new Color(Clay.r, Clay.g, Clay.b, .3f), circleSprite);
                seedIcons[i].rectTransform.localRotation = Quaternion.Euler(0, 0, 28f);
            }

            var p = Panel("Target cloth", playGroup.transform, new Vector2(.5f, 1), new Vector2(0, -147), new Vector2(368, 60), Cream);
            promptRt = p.rectTransform;
            prompt = Text("Prompt", p.transform, "", new Vector2(.5f, .5f), new Vector2(0, 0), new Vector2(344, 54), 16, Brown);
            // 목표를 발문과 같은 천 패널의 하위 요소로 묶는다. 뷰포트가
            // 바뀌어도 두 텍스트가 독립 절대 좌표로 격자 위에 떠오르지 않는다.
            target = Text("Target area", p.transform, "25 m²", new Vector2(.5f, .5f), new Vector2(0, -27), new Vector2(340, 34), 23, Wrong);

            var bottom = Panel("Square reference cloth", playGroup.transform, new Vector2(.5f, 0), new Vector2(0, 67), new Vector2(368, 112), new Color(Cream.r, Cream.g, Cream.b, .96f));
            squareTable = Text("Square table", bottom.transform,
                "제곱표  1² 1 · 2² 4 · 3² 9 · 4² 16 · 5² 25\n6² 36 · 7² 49 · 8² 64 · 9² 81 · 10² 100 · 12² 144 · 15² 225",
                new Vector2(.5f, 1), new Vector2(0, -29), new Vector2(348, 52), 12, Brown);
            measure = Text("Measurements", bottom.transform, "모래쥐를 잡아 끌면 AC와 CB가 나타납니다", new Vector2(.5f, 0), new Vector2(0, 20), new Vector2(348, 34), 15, Sage);
            evidenceRt = bottom.rectTransform;

            aLabel = Text("A label", playGroup.transform, "A", new Vector2(.5f, .5f), Vector2.zero, new Vector2(44, 36), 20, Cream);
            bLabel = Text("B label", playGroup.transform, "B", new Vector2(.5f, .5f), Vector2.zero, new Vector2(44, 36), 20, Cream);
            cLabel = Text("C label", playGroup.transform, "C ∟", new Vector2(.5f, .5f), Vector2.zero, new Vector2(58, 36), 18, Cream);

            var mag = Panel("Precision magnifier", playGroup.transform, new Vector2(.5f, .5f), new Vector2(112, 65), new Vector2(112, 94), new Color(Cream.r, Cream.g, Cream.b, .98f));
            magRt = mag.rectTransform;
            magText = Text("Magnifier numbers", mag.transform, "현재\nAC 0 m · CB 0 m", new Vector2(.5f, .5f), Vector2.zero, new Vector2(102, 84), 15, Brown);
            mag.gameObject.SetActive(false);

            var toastPanel = Panel("Refusal message", playGroup.transform, new Vector2(.5f, .5f), new Vector2(0, 168), new Vector2(338, 52), DeepRust);
            toastG = toastPanel.gameObject.AddComponent<CanvasGroup>();
            toast = Text("Refusal text", toastPanel.transform, "", new Vector2(.5f, .5f), Vector2.zero, new Vector2(318, 44), 15, Cream);
            toastG.alpha = 0;

            var rip = Panel("Sand tap ripple", playGroup.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(38, 38), new Color(Cream.r, Cream.g, Cream.b, .8f), circleSprite);
            rippleRt = rip.rectTransform; rippleImg = rip; rip.enabled = false;
        }

        void BuildGuideUi(out RectTransform ringRt, out Image ringImg, out RectTransform targetRt,
            out Image targetImg, out RectTransform pathRt, out RectTransform fingerRt)
        {
            var ring = Panel("Double pulse ring", guideGroup.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(84, 84), new Color(Cream.r, Cream.g, Cream.b, .38f), circleSprite);
            ringRt = ring.rectTransform; ringImg = ring;
            var inner = Panel("Pulse ring center", ring.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(56, 56), new Color(Rust.r, Rust.g, Rust.b, .72f), circleSprite);
            var tar = Panel("Ghost landing", guideGroup.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(58, 58), new Color(Sage.r, Sage.g, Sage.b, .46f), circleSprite);
            targetRt = tar.rectTransform; targetImg = tar;
            var path = Panel("Flowing dotted path", guideGroup.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(100, 10), new Color(Cream.r, Cream.g, Cream.b, .75f));
            pathRt = path.rectTransform;
            var finger = Panel("Ghost finger", guideGroup.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(32, 42), Cream, circleSprite);
            fingerRt = finger.rectTransform;
            Text("Guide instruction", guideGroup.transform, "모래쥐를 잡아 3 m, 4 m 착지점으로 끌어 놓으시오", new Vector2(.5f, 0), new Vector2(0, 139), new Vector2(360, 44), 16, Cream);
        }

        void BuildRevealUi(out Image panelImage, out TextMeshProUGUI title, out TextMeshProUGUI equation, out TextMeshProUGUI reason)
        {
            var panel = Panel("Math evidence cloth", revealGroup.transform, new Vector2(.5f, .5f), new Vector2(0, 22), new Vector2(354, 222), Cream);
            panelImage = panel;
            title = Text("Reveal title", panel.transform, "착지 성공", new Vector2(.5f, 1), new Vector2(0, -35), new Vector2(330, 52), 26, Correct);
            equation = Text("Reveal equation", panel.transform, "3² + 4² = 25 m²", new Vector2(.5f, .5f), new Vector2(0, 22), new Vector2(330, 62), 27, Brown);
            reason = Text("Reveal reason", panel.transform, "직각의 맞은편 AB가 빗변입니다", new Vector2(.5f, 0), new Vector2(0, 33), new Vector2(328, 52), 14, Brown);
            for (int i = 0; i < revealTiles.Length; i++)
            {
                revealTiles[i] = Panel("Area tile " + i, panel.transform, new Vector2(.5f, .5f), Vector2.zero,
                    new Vector2(13, 13), i < 9 ? Clay : Sage);
            }
            revealGroup.SetActive(false);
        }

        void BuildEndUi(out RectTransform restart, out TextMeshProUGUI title, out TextMeshProUGUI stats, out TextMeshProUGUI score)
        {
            var banner = Panel("Result cave awning", endGroup.transform, new Vector2(.5f, .5f), new Vector2(0, 66), new Vector2(360, 332), Cream);
            title = Text("Result title", banner.transform, "씨앗 굴 도착", new Vector2(.5f, 1), new Vector2(0, -57), new Vector2(334, 72), 34, Brown);
            score = Text("Result score", banner.transform, "0점", new Vector2(.5f, .5f), new Vector2(0, 50), new Vector2(320, 70), 34, Sage);
            stats = Text("Result stats", banner.transform, "", new Vector2(.5f, .5f), new Vector2(0, -38), new Vector2(330, 112), 18, Brown);
            var c = Panel("Restart CTA", endGroup.transform, new Vector2(.5f, 0), new Vector2(0, 78), new Vector2(348, 72), Sage);
            restart = c.rectTransform;
            Text("Restart label", c.transform, "다시 굴길 열기", new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 60), 24, Cream);
            endGroup.SetActive(false);
        }

        public void ShowTitle()
        {
            revealActive = false;
            current = null;
            boardRoot.gameObject.SetActive(false);
            titleGroup.SetActive(true); playGroup.SetActive(false); guideGroup.SetActive(false); revealGroup.SetActive(false); endGroup.SetActive(false);
            targetPlayCamera = false; cameraBlend = 0;
            mouseRoot.gameObject.SetActive(true);
            mouseRoot.position = new Vector3(0, .12f, -1.0f);
            mouseHome = mouseRoot.position;
            mouseRoot.localScale = Vector3.one * 1.15f;
            for (int i = 0; i < footprints.Length; i++) footprints[i].gameObject.SetActive(false);
            for (int i = 0; i < seeds.Length; i++) seeds[i].gameObject.SetActive(false);
            for (int i = 0; i < ropeKnots.Length; i++) ropeKnots[i].gameObject.SetActive(true);
        }

        public void ShowPractice(LandingProblem problem)
        {
            revealActive = false;
            current = problem;
            boardRoot.gameObject.SetActive(true);
            titleGroup.SetActive(false); playGroup.SetActive(true); guideGroup.SetActive(true); revealGroup.SetActive(false); endGroup.SetActive(false);
            targetPlayCamera = true; cameraBlend = 0;
            SetupBoard(problem);
            previewGrid = Vector2.zero;
            mouseRoot.localScale = Vector3.one * .78f;
            mouseRoot.position = GridWorld(Vector2.zero);
            mouseHome = mouseRoot.position;
            progressText.text = "연습 · 압박 정지";
            scoreText.text = "0점";
            livesText.text = "끈 ●●●";
            timerText.text = "타이머 정지";
            goalText.text = "착지점을 맞추면 모래쥐가 전진한다";
            promptText.text = "빗변 위 정사각형의 넀이 25 m²가 되도록 착지점을 정하시오.".Replace("넀이", "넓이");
            targetText.text = "목표 넓이  25 m²";
            measureText.text = "모래쥐를 잡아 가로 3 m · 세로 4 m로 끌어 보시오";
            squareTableText.text = "첫 착지  AC²=9 m² · CB²=16 m²\n두 넓이를 따로 보고 착지점을 구성하시오";
            SetKnotUi(3); SetSeedUi(0);
            guideClock = 0; guideStrong = true;
            RefreshGeometry(Vector2.zero, 0, 0);
        }

        public void ShowProblem(LandingProblem problem, int index, int lives, int score)
        {
            revealActive = false;
            current = problem;
            boardRoot.gameObject.SetActive(true);
            titleGroup.SetActive(false); playGroup.SetActive(true); guideGroup.SetActive(false); revealGroup.SetActive(false); endGroup.SetActive(false);
            targetPlayCamera = true;
            SetupBoard(problem);
            previewGrid = problem.LockedAxis ? new Vector2(problem.targetA, problem.startB) : Vector2.zero;
            mouseRoot.localScale = Vector3.one * .78f;
            mouseRoot.position = GridWorld(previewGrid);
            mouseHome = mouseRoot.position;
            progressText.text = (index + 1) + " / 8 · " + (problem.band == 1 ? "넓이 보기" : problem.band == 2 ? "제곱표 계산" : "발자국 수리");
            promptText.text = problem.prompt;
            targetText.text = "빗변 위 정사각형  " + problem.targetArea + " m²";
            scoreShown = Mathf.Min(scoreShown, score);
            livesText.text = "안전끈 " + lives + "개";
            timerText.text = Mathf.CeilToInt(game.TimeLeft) + "초";
            goalText.text = index < 5 ? "두 이동량을 놓아 목표 넓이를 만드시오" : "주어진 AC를 두고 CB 발자국만 고치시오";
            squareTableText.text = problem.band == 1
                ? "AC와 CB 위 정사각형 넓이를 따로 확인하시오\n합은 손을 놓은 뒤에만 판정합니다"
                : "제곱표  1² 1 · 2² 4 · 3² 9 · 4² 16 · 5² 25\n6² 36 · 7² 49 · 8² 64 · 9² 81 · 10² 100 · 12² 144 · 15² 225";
            measureText.text = problem.LockedAxis
                ? "AC=" + problem.targetA + " m 고정 · CB 발자국을 위아래로 옮기시오"
                : "모래쥐를 잡아 AC와 CB를 모두 정하시오";
            SetKnotUi(lives); SetSeedUi(index);
            RefreshGeometry(previewGrid, Mathf.RoundToInt(previewGrid.x), Mathf.RoundToInt(previewGrid.y));
        }

        void SetupBoard(LandingProblem problem)
        {
            int n = problem.gridN;
            for (int i = 0; i < gridLines.Length; i++) gridLines[i].enabled = false;
            for (int i = 0; i <= n; i++)
            {
                var a = GridWorld(new Vector2(i, 0)); var b = GridWorld(new Vector2(i, n));
                gridLines[i].enabled = true; gridLines[i].SetPosition(0, a); gridLines[i].SetPosition(1, b);
                var c = GridWorld(new Vector2(0, i)); var d = GridWorld(new Vector2(n, i));
                gridLines[n + 1 + i].enabled = true; gridLines[n + 1 + i].SetPosition(0, c); gridLines[n + 1 + i].SetPosition(1, d);
            }
            lineAC.enabled = lineCB.enabled = lineAB.enabled = rightAngle.enabled = true;
        }

        public void BeginDrag()
        {
            dragging = true;
            MgfFx.Punch(mouseBody, .15f, .18f);
            guideStrong = false;
            magnifier.gameObject.SetActive(current != null && current.gridN >= 10);
        }

        public void UpdateDrag(Vector2 raw, int a, int b, bool locked)
        {
            previewGrid = raw;
            mouseRoot.position = GridWorld(raw) + Vector3.up * .28f;
            mouseRoot.localScale = new Vector3(.83f, .69f, .83f);
            RefreshGeometry(raw, a, b);
            measureText.text = "AC=" + a + " m   ·   CB=" + b + " m" + (current != null && current.band == 1 ? "\n각 정사각형: " + (a * a) + " m² · " + (b * b) + " m²" : "");
            magnifierText.text = "현재 눈금\nAC " + a + " m\nCB " + b + " m";
            PositionMagnifier(MgfPointer.Position);
        }

        public void CancelDrag(LandingProblem problem, string message)
        {
            dragging = false;
            magnifier.gameObject.SetActive(false);
            previewGrid = problem.LockedAxis ? new Vector2(problem.targetA, problem.startB) : Vector2.zero;
            mouseRoot.position = GridWorld(previewGrid);
            mouseRoot.localScale = Vector3.one * .78f;
            RefreshGeometry(previewGrid, Mathf.RoundToInt(previewGrid.x), Mathf.RoundToInt(previewGrid.y));
            ShowToast(message, 2.1f);
            MgfFx.Punch(boardRoot, .025f, .18f);
            if (problem.band == 0) { guideClock = 0; guideStrong = true; }
        }

        public void BeginReveal(bool correct, bool practice, int a, int b, int s, string evidence, int solved, int score)
        {
            dragging = false;
            magnifier.gameObject.SetActive(false);
            guideGroup.SetActive(false);
            revealGroup.SetActive(true);
            revealActive = true; revealCorrect = correct; revealPractice = practice; revealClock = 0;
            revealFrom = GridWorld(Vector2.zero);
            revealTo = GridWorld(new Vector2(a, b));
            revealPanelImage.color = correct ? new Color(Cream.r, Cream.g, Cream.b, .98f) : new Color(1f, .83f, .76f, .98f);
            revealTitleText.text = correct ? (practice ? "첫 착지 성공" : "두 발자국 착지") : (practice ? "착지점을 다시 읽으시오" : "안전끈 하나가 풀렸습니다");
            revealTitleText.color = correct ? Correct : Wrong;
            revealEquationText.text = evidence;
            revealReasonText.text = correct
                ? (practice ? "직각의 맞은편 AB가 빗변 · 9칸과 16칸이 25칸을 채웁니다" : "직각을 낀 AC, CB의 제곱 넓이가 목표 넓이로 이어졌습니다")
                : "정답 좌표는 숨긴 채, 만든 넓이와 목표의 차만 남깁니다";
            for (int i = 0; i < revealTiles.Length; i++) revealTiles[i].gameObject.SetActive(practice && correct);
            if (correct)
            {
                MgfSfx.Play("whoosh", .25f);
                if (!practice)
                {
                    SetHistory(solved);
                    SetSeedUi(solved);
                    cheek.localScale = new Vector3(.38f, .28f, .36f);
                }
            }
            else
            {
                MgfSfx.Play("wrong", .40f);
                MgfFx.Shake(cam, practice ? .045f : .09f, .24f);
                int lives = practice ? 3 : game.State.lives;
                SetKnotUi(lives);
                if (!practice && lives >= 0 && lives < ropeKnots.Length) ropeKnots[lives].gameObject.SetActive(false);
            }
            scoreShown = Mathf.Min(scoreShown, score);
        }

        public void ReturnAfterWrong(LandingProblem problem, bool practice)
        {
            current = problem;
            revealActive = false; revealGroup.SetActive(false);
            guideGroup.SetActive(practice);
            previewGrid = problem.LockedAxis ? new Vector2(problem.targetA, problem.startB) : Vector2.zero;
            mouseRoot.position = GridWorld(previewGrid); mouseRoot.localScale = Vector3.one * .78f;
            earL.localRotation = earR.localRotation = Quaternion.identity;
            RefreshGeometry(previewGrid, Mathf.RoundToInt(previewGrid.x), Mathf.RoundToInt(previewGrid.y));
            if (practice) { guideClock = 0; guideStrong = true; }
            measureText.text = practice ? "목표 25 m² · 가로와 세로를 모두 움직이시오" : game.State.lastFeedback;
        }

        public void ShowEnd(bool clear, string reason, int score, int solved, int firstCorrect, int firstAttempts, int lives)
        {
            revealActive = false; revealGroup.SetActive(false); playGroup.SetActive(false); guideGroup.SetActive(false); titleGroup.SetActive(false); endGroup.SetActive(true);
            boardRoot.gameObject.SetActive(false);
            targetPlayCamera = false;
            mouseRoot.position = new Vector3(clear ? 4.5f : 0f, .15f, clear ? 3.0f : -1.0f);
            mouseHome = mouseRoot.position;
            mouseRoot.localScale = Vector3.one * (clear ? 1.2f : 1.0f);
            endTitleText.text = clear ? "씨앗 굴 도착" : reason;
            endTitleText.color = clear ? Correct : Wrong;
            endStatsText.text = "착지 " + solved + " / 8\n첫 제출 " + firstCorrect + " / " + firstAttempts + " · 안전끈 " + lives + "개\n" +
                (clear ? "제곱 넓이 관계로 협곡을 건넜습니다" : "통과 조건: 8번 착지 · 첫 제출 6/8");
            endScoreShown = 0; endScoreText.text = "0점";
            for (int i = 0; i < seeds.Length; i++) seeds[i].gameObject.SetActive(i < solved);
        }

        public bool TitleStartHit(Vector2 screen)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(titleCta, screen, null)) return true;
            Vector3 p = cam.WorldToScreenPoint(mouseRoot.position + Vector3.up * .4f);
            if (p.z > 0 && Vector2.Distance(screen, new Vector2(p.x, p.y)) < Mathf.Max(92f, Screen.width * .20f)) return true;
            // 타이틀 중앙은 협곡과 모래쥐가 함께 차지하는 하나의 세계 진입 오브젝트다.
            // 첫플레이 하네스의 중앙 탭도 이 세계 진입으로 해석하되, 바깥 탭은 리플만 낸다.
            return screen.x >= Screen.width * .18f && screen.x <= Screen.width * .82f
                && screen.y >= Screen.height * .28f && screen.y <= Screen.height * .72f;
        }

        public bool RestartHit(Vector2 screen) => RectTransformUtility.RectangleContainsScreenPoint(restartCta, screen, null);

        public void TitlePress()
        {
            titlePress = .24f;
            MgfFx.Punch(mouseRoot, .19f, .23f);
            MgfFx.Punch(titleCta, .08f, .20f);
        }

        public bool NearHandle(Vector2 screen)
        {
            Vector3 p = cam.WorldToScreenPoint(mouseRoot.position + Vector3.up * .28f);
            float radius = current != null && current.gridN >= 10 ? 86f : 76f;
            if (p.z > 0 && Vector2.Distance(screen, new Vector2(p.x, p.y)) <= radius) return true;

            if (current == null || current.band != 0) return false;

            // 연습에서는 보이는 맥동 링 전체와 유령 경로가 하나의 손잡이다.
            // 지도 손가락 위에서 눌러도 원점의 모래쥐를 잡은 것으로 해석한다.
            Vector3 from3 = cam.WorldToScreenPoint(GridWorld(Vector2.zero) + Vector3.up * .25f);
            Vector3 to3 = cam.WorldToScreenPoint(GridWorld(new Vector2(3, 4)) + Vector3.up * .25f);
            if (from3.z <= 0 || to3.z <= 0) return false;
            Vector2 from = new Vector2(from3.x, from3.y);
            Vector2 to = new Vector2(to3.x, to3.y);
            if (Vector2.Distance(screen, from) <= 108f || Vector2.Distance(screen, to) <= 72f) return true;
            return DistanceToSegment(screen, from, to) <= 34f;
        }

        static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float denom = ab.sqrMagnitude;
            if (denom <= .001f) return Vector2.Distance(point, a);
            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / denom);
            return Vector2.Distance(point, a + ab * t);
        }

        public bool ScreenToGrid(Vector2 screen, out Vector2 grid)
        {
            var ray = cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, new Vector3(0, BoardY, 0));
            if (plane.Raycast(ray, out float d))
            {
                Vector3 local = boardRoot.InverseTransformPoint(ray.GetPoint(d));
                float px = (local.x / BoardSpan + .5f) * current.gridN;
                float py = (local.z / BoardSpan + .5f) * current.gridN;
                grid = InverseOrient(new Vector2(px, py), current.gridN, current.orientation);
                return true;
            }
            grid = Vector2.zero; return false;
        }

        public bool GridContainsRaw(Vector2 raw)
        {
            if (current == null) return false;
            return raw.x >= -.35f && raw.y >= -.35f && raw.x <= current.gridN + .35f && raw.y <= current.gridN + .35f;
        }

        Vector3 GridWorld(Vector2 grid)
        {
            int n = current == null ? 6 : current.gridN;
            int orientation = current == null ? 0 : current.orientation;
            Vector2 q = Orient(grid, n, orientation);
            Vector3 local = new Vector3((q.x / n - .5f) * BoardSpan, BoardY, (q.y / n - .5f) * BoardSpan);
            return boardRoot.TransformPoint(local);
        }

        static Vector2 Orient(Vector2 p, int n, int orientation)
        {
            float x = p.x, y = p.y;
            if (orientation >= 4) x = n - x;
            switch (orientation & 3)
            {
                case 1: return new Vector2(n - y, x);
                case 2: return new Vector2(n - x, n - y);
                case 3: return new Vector2(y, n - x);
                default: return new Vector2(x, y);
            }
        }

        static Vector2 InverseOrient(Vector2 p, int n, int orientation)
        {
            float x, y;
            switch (orientation & 3)
            {
                case 1: x = p.y; y = n - p.x; break;
                case 2: x = n - p.x; y = n - p.y; break;
                case 3: x = n - p.y; y = p.x; break;
                default: x = p.x; y = p.y; break;
            }
            if (orientation >= 4) x = n - x;
            return new Vector2(x, y);
        }

        void RefreshGeometry(Vector2 grid, int a, int b)
        {
            if (current == null) return;
            Vector3 A = GridWorld(Vector2.zero) + Vector3.up * .035f;
            Vector3 C = GridWorld(new Vector2(grid.x, 0)) + Vector3.up * .035f;
            Vector3 B = GridWorld(grid) + Vector3.up * .035f;
            lineAC.SetPosition(0, A); lineAC.SetPosition(1, C);
            lineCB.SetPosition(0, C); lineCB.SetPosition(1, B);
            lineAB.SetPosition(0, A); lineAB.SetPosition(1, B);
            Vector3 u = (A - C).normalized * .24f, v = (B - C).normalized * .24f;
            if (u.sqrMagnitude < .1f) u = Vector3.right * .24f;
            if (v.sqrMagnitude < .1f) v = Vector3.forward * .24f;
            rightAngle.SetPosition(0, C + u); rightAngle.SetPosition(1, C + u + v); rightAngle.SetPosition(2, C + v);
            bool hasTriangle = a > 0 && b > 0;
            labelA.enabled = true;
            labelB.enabled = labelC.enabled = hasTriangle;
            PositionWorldLabels(A, B, C);
        }

        void PositionWorldLabels(Vector3 A, Vector3 B, Vector3 C)
        {
            PlaceUiAtWorld(labelA.rectTransform, A + Vector3.up * .18f, new Vector2(-18, -31));
            PlaceUiAtWorld(labelB.rectTransform, B + Vector3.up * .18f, new Vector2(16, 22));
            PlaceUiAtWorld(labelC.rectTransform, C + Vector3.up * .18f, new Vector2(24, -18));
        }

        void PlaceUiAtWorld(RectTransform rt, Vector3 world, Vector2 offset)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            rt.anchoredPosition = ScreenToCanvas(new Vector2(sp.x, sp.y)) + offset;
        }

        Vector2 ScreenToCanvas(Vector2 screen)
        {
            Rect r = rootRt.rect;
            return new Vector2(r.xMin + screen.x / Mathf.Max(1, Screen.width) * r.width,
                r.yMin + screen.y / Mathf.Max(1, Screen.height) * r.height);
        }

        void PositionMagnifier(Vector2 screen)
        {
            Vector2 p = ScreenToCanvas(screen);
            float side = screen.x > Screen.width * .5f ? -1f : 1f;
            p += new Vector2(side * 92f, 105f);
            p.x = Mathf.Clamp(p.x, rootRt.rect.xMin + 64, rootRt.rect.xMax - 64);
            p.y = Mathf.Clamp(p.y, rootRt.rect.yMin + 80, rootRt.rect.yMax - 80);
            magnifier.anchoredPosition = p;
        }

        public void RefuseAt(Vector2 screen, string message, bool replayGuide)
        {
            rippleImage.enabled = true; rippleClock = .34f; ripple.localScale = Vector3.one * .5f;
            ripple.anchoredPosition = ScreenToCanvas(screen);
            ShowToast(message, 2.2f);
            MgfFx.Punch(mouseRoot, .10f, .22f);
            MgfSfx.Play("wrong", .10f);
            if (replayGuide) { guideClock = 0; guideStrong = true; guideGroup.SetActive(true); }
        }

        void ShowToast(string message, float duration)
        {
            toastText.text = message; toastClock = duration; toastGroup.alpha = 1;
            MgfFx.Punch(toastGroup.transform, .04f, .18f);
        }

        void SetKnotUi(int lives)
        {
            for (int i = 0; i < knotIcons.Length; i++)
            {
                bool on = i < lives;
                knotIcons[i].color = on ? Sage : new Color(Wrong.r, Wrong.g, Wrong.b, .24f);
                ropeKnots[i].gameObject.SetActive(on);
            }
            livesText.text = "안전끈 " + lives + "개";
        }

        void SetSeedUi(int count)
        {
            for (int i = 0; i < seedIcons.Length; i++) seedIcons[i].color = i < count ? Sage : new Color(Clay.r, Clay.g, Clay.b, .3f);
            for (int i = 0; i < seeds.Length; i++) seeds[i].gameObject.SetActive(i < count);
        }

        void SetHistory(int solved)
        {
            int idx = solved - 1;
            if (idx < 0 || idx >= 8) return;
            for (int foot = 0; foot < 2; foot++)
            {
                var f = footprints[idx * 2 + foot];
                f.position = new Vector3(-3.2f + idx * .88f + foot * .2f, .025f, 5.0f + (idx % 2) * .2f);
                f.localRotation = Quaternion.Euler(0, idx * 13 + foot * 8, 0);
                f.gameObject.SetActive(true);
            }
        }

        public void Tick(float dt)
        {
            worldClock += dt;
            if (lastW != Screen.width || lastH != Screen.height) ApplyLayout(false);
            AnimateCamera(dt);
            AnimateWorld(dt);
            AnimateUi(dt);
            if (current != null && (game.Phase == MoraejwiMeollittwigiGame.GamePhase.Practice || game.Phase == MoraejwiMeollittwigiGame.GamePhase.Playing))
                RefreshGeometry(previewGrid, Mathf.RoundToInt(previewGrid.x), Mathf.RoundToInt(previewGrid.y));
        }

        void AnimateCamera(float dt)
        {
            float speed = targetPlayCamera ? 9.5f : 4f;
            cameraBlend = Mathf.MoveTowards(cameraBlend, targetPlayCamera ? 1f : 0f, dt * speed);
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float titleSize = aspect < .72f ? 7.2f : aspect > 1.25f ? 5.5f : 6.2f;
            float playSize = portraitTablet ? 12.2f : aspect < .72f ? 9.45f : aspect > 1.25f ? 5.3f : 7.0f;
            Vector3 titlePos = aspect > 1.25f ? new Vector3(10.5f, 8.3f, -11.5f) : new Vector3(9.2f, 9.1f, -11.8f);
            Vector3 playPos = new Vector3(0, 20f, 1.15f);
            Quaternion titleRot = Quaternion.LookRotation(new Vector3(0, .45f, 1.2f) - titlePos, Vector3.up);
            Quaternion playRot = Quaternion.Euler(90, 0, 0);
            float eased = cameraBlend * cameraBlend * (3f - 2f * cameraBlend);
            cam.transform.position = Vector3.Lerp(titlePos, playPos, eased);
            cam.transform.rotation = Quaternion.Slerp(titleRot, playRot, eased);
            cam.orthographicSize = Mathf.Lerp(titleSize, playSize, eased);
        }

        void AnimateWorld(float dt)
        {
            for (int i = 0; i < plants.Length; i++)
                plants[i].localRotation = Quaternion.Euler(0, Mathf.Sin(worldClock * .55f + i) * 4f, Mathf.Sin(worldClock * .8f + i * .7f) * 3f);
            for (int i = 0; i < windBits.Length; i++)
            {
                Vector3 p = windBits[i].position; p.x += dt * (.5f + i * .035f);
                p.y += Mathf.Sin(worldClock * 1.2f + i) * dt * .08f;
                if (p.x > 8f) p.x = -8f;
                windBits[i].position = p;
            }
            float sniff = .5f + .5f * Mathf.Sin(worldClock * 4.2f);
            if (!revealActive && !dragging)
            {
                mouseRoot.position = mouseHome + Vector3.up * Mathf.Abs(Mathf.Sin(worldClock * 2.1f)) * .055f;
                mouseRoot.localScale = Vector3.one * (game.Phase == MoraejwiMeollittwigiGame.GamePhase.Title ? 1.15f : .78f);
                head.localPosition = new Vector3(0, .52f + sniff * .018f, .56f + sniff * .025f);
                earL.localRotation = Quaternion.Euler(0, 0, -5 + Mathf.Sin(worldClock * 1.7f) * 4);
                earR.localRotation = Quaternion.Euler(0, 0, 5 + Mathf.Sin(worldClock * 1.7f + .8f) * 4);
            }
            UpdateTail(sniff);
            if (revealActive) AnimateRevealWorld();
            if (titlePress > 0) titlePress = Mathf.Max(0, titlePress - dt);
        }

        void UpdateTail(float sniff)
        {
            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                float curl = revealCorrect && revealActive ? Mathf.Sin(revealClock * 8f + t * 3f) * .24f : Mathf.Sin(worldClock * 1.3f + t * 2f) * .12f;
                tail.SetPosition(i, new Vector3(-.45f - t * 1.15f, .34f + Mathf.Sin(t * Mathf.PI) * (.18f + curl), -.28f - t * .55f + curl));
            }
        }

        void AnimateRevealWorld()
        {
            if (revealCorrect)
            {
                float hold = .07f;
                float t = Mathf.Clamp01((revealClock - hold) / .65f);
                float e = 1f - Mathf.Pow(1f - t, 3f);
                Vector3 p = Vector3.Lerp(revealFrom, revealTo, e);
                p.y += Mathf.Sin(t * Mathf.PI) * 2.0f;
                mouseRoot.position = p;
                float squash = t < .16f ? 1f - Mathf.Sin(t / .16f * Mathf.PI) * .18f : 1f;
                mouseRoot.localScale = new Vector3(.82f / squash, .72f * squash, .82f / squash);
                for (int i = 0; i < paws.Length; i++) paws[i].localRotation = Quaternion.Euler(Mathf.Sin(t * Mathf.PI * 2 + i) * 18f, 0, 0);
                if (t >= 1f) { mouseHome = revealTo; mouseRoot.position = revealTo; mouseRoot.localScale = Vector3.one * .78f; }
            }
            else
            {
                float t = Mathf.Clamp01(revealClock / .72f);
                mouseRoot.position = Vector3.Lerp(revealTo, revealFrom, t * t);
                earL.localRotation = Quaternion.Euler(0, 0, -24f * Mathf.Sin(t * Mathf.PI));
                earR.localRotation = Quaternion.Euler(0, 0, 24f * Mathf.Sin(t * Mathf.PI));
                mouseRoot.localScale = new Vector3(.82f, .64f + t * .14f, .82f);
            }
        }

        void AnimateUi(float dt)
        {
            int sec = Mathf.CeilToInt(game.TimeLeft);
            if (playGroup.activeSelf && sec != lastSecond)
            {
                lastSecond = sec; timerText.text = game.Phase == MoraejwiMeollittwigiGame.GamePhase.Practice ? "타이머 정지" : sec + "초";
                timerText.color = sec <= 15 ? Wrong : Brown;
            }
            scoreShown = Mathf.MoveTowards(scoreShown, game.State.score, Mathf.Max(45f, Mathf.Abs(game.State.score - scoreShown) * 6f) * dt);
            int shown = Mathf.RoundToInt(scoreShown);
            if (playGroup.activeSelf && shown != lastScore) { lastScore = shown; scoreText.text = shown.ToString("N0") + "점"; MgfFx.Punch(scoreText.transform, .04f, .14f); }
            if (toastClock > 0) { toastClock -= dt; toastGroup.alpha = Mathf.Clamp01(toastClock * 3.8f); } else toastGroup.alpha = 0;
            if (rippleClock > 0)
            {
                rippleClock -= dt; float t = 1f - Mathf.Clamp01(rippleClock / .34f);
                ripple.localScale = Vector3.one * (.5f + t * 1.5f);
                rippleImage.color = new Color(Cream.r, Cream.g, Cream.b, 1f - t);
                if (rippleClock <= 0) rippleImage.enabled = false;
            }
            if (guideGroup.activeSelf && current != null)
            {
                guideClock += dt; PositionGuide();
                if (guideClock > 8f) { guideClock = 0; guideStrong = true; }
            }
            if (revealActive)
            {
                revealClock += dt;
                float pop = 1f + Mathf.Sin(Mathf.Clamp01(revealClock / .28f) * Mathf.PI) * .08f;
                revealPanelImage.rectTransform.localScale = Vector3.one * pop;
                if (revealPractice && revealCorrect) AnimateTiles();
            }
            if (endGroup.activeSelf)
            {
                endScoreShown = Mathf.MoveTowards(endScoreShown, game.State.score, Mathf.Max(80f, game.State.score * 1.8f) * dt);
                endScoreText.text = Mathf.RoundToInt(endScoreShown).ToString("N0") + "점";
            }
        }

        void PositionGuide()
        {
            Vector3 fromW = GridWorld(Vector2.zero) + Vector3.up * .25f;
            Vector3 toW = GridWorld(new Vector2(3, 4)) + Vector3.up * .25f;
            Vector3 fromS = cam.WorldToScreenPoint(fromW), toS = cam.WorldToScreenPoint(toW);
            Vector2 a = ScreenToCanvas(new Vector2(fromS.x, fromS.y));
            Vector2 b = ScreenToCanvas(new Vector2(toS.x, toS.y));
            float pulse = .5f + .5f * Mathf.Sin(worldClock * 5.2f);
            float boost = guideStrong ? 1.25f : 1f;
            guideRing.anchoredPosition = a; guideRing.sizeDelta = Vector2.one * (72 + pulse * 22) * boost;
            guideRingImage.color = new Color(Cream.r, Cream.g, Cream.b, .24f + pulse * .35f);
            guideTarget.anchoredPosition = b; guideTarget.sizeDelta = Vector2.one * (52 + pulse * 10);
            Vector2 d = b - a; guidePath.anchoredPosition = (a + b) * .5f; guidePath.sizeDelta = new Vector2(d.magnitude, 5 + pulse * 2);
            guidePath.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            float loop = (guideClock % 1.2f) / 1.2f; float e = loop * loop * (3 - 2 * loop);
            guideFinger.anchoredPosition = Vector2.Lerp(a, b, e);
            for (int i = 0; i < 16; i++) guideWorldPath.SetPosition(i, Vector3.Lerp(fromW, toW, i / 15f));
            guideWorldPath.enabled = true;
        }

        void AnimateTiles()
        {
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01((revealClock - .22f) / .85f));
            for (int i = 0; i < revealTiles.Length; i++)
            {
                Vector2 start;
                if (i < 9) start = new Vector2(-115 + (i % 3) * 16, -8 + (i / 3) * 16);
                else { int j = i - 9; start = new Vector2(60 + (j % 4) * 16, -16 + (j / 4) * 16); }
                Vector2 end = new Vector2(-32 + (i % 5) * 16, -24 + (i / 5) * 16);
                revealTiles[i].rectTransform.anchoredPosition = Vector2.Lerp(start, end, t);
                revealTiles[i].rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * .12f);
            }
        }

        void ApplyLayout(bool initial)
        {
            lastW = Screen.width; lastH = Screen.height;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            // WebGL의 Screen.width는 devicePixelRatio가 반영된다. 폭 700만 보면
            // DPR 2인 390px 휴대폰(내부 780px)을 태블릿으로 오인하므로 판형 비율로 나눈다.
            portraitTablet = Screen.height > Screen.width && aspect >= .68f;
            wide = Screen.width >= Screen.height && Screen.width >= 640;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler) scaler.matchWidthOrHeight = (wide || portraitTablet) ? 1f : .5f;
            if (wide)
            {
                Place(promptPanel, new Vector2(0, .5f), new Vector2(170, 92), new Vector2(326, 146));
                Place(evidencePanel, new Vector2(1, .5f), new Vector2(-170, -36), new Vector2(326, 190));
                ConfigurePromptChildren(new Vector2(304, 78), new Vector2(0, 23), 15,
                    new Vector2(304, 42), new Vector2(0, -43), 22);
                ConfigureEvidenceChildren(new Vector2(302, 86), new Vector2(0, 24), 12,
                    new Vector2(302, 48), new Vector2(0, -60), 14);
            }
            else if (portraitTablet)
            {
                // 태블릿 세로형은 중앙 격자를 줄이고 발문과 근거를 양쪽 열로 보낸다.
                Place(promptPanel, new Vector2(0, .5f), new Vector2(92, 66), new Vector2(168, 178));
                Place(evidencePanel, new Vector2(1, .5f), new Vector2(-92, -54), new Vector2(168, 214));
                ConfigurePromptChildren(new Vector2(148, 106), new Vector2(0, 26), 13,
                    new Vector2(148, 45), new Vector2(0, -57), 18);
                ConfigureEvidenceChildren(new Vector2(148, 132), new Vector2(0, 26), 10,
                    new Vector2(148, 58), new Vector2(0, -72), 12);
            }
            else
            {
                Place(promptPanel, new Vector2(.5f, 1), new Vector2(0, -162), new Vector2(368, 92));
                Place(evidencePanel, new Vector2(.5f, 0), new Vector2(0, 67), new Vector2(368, 112));
                ConfigurePromptChildren(new Vector2(344, 49), new Vector2(0, 18), 15,
                    new Vector2(344, 34), new Vector2(0, -28), 22);
                ConfigureEvidenceChildren(new Vector2(348, 52), new Vector2(0, 25), 12,
                    new Vector2(348, 34), new Vector2(0, -35), 15);
            }
            // 목표는 항상 HUD 천 안에 남겨 별도 안전 영역을 유지한다.
            Place(goalText.rectTransform, new Vector2(.5f, 1), new Vector2(0, -17), new Vector2(350, 30));
            game.State.layout = wide ? "wide-safe-columns" : portraitTablet ? "tablet-safe-columns" : "portrait-safe-stack";
        }

        void ConfigurePromptChildren(Vector2 promptSize, Vector2 promptPos, float promptFont,
            Vector2 targetSize, Vector2 targetPos, float targetFont)
        {
            Place(promptText.rectTransform, new Vector2(.5f, .5f), promptPos, promptSize);
            promptText.fontSize = promptFont;
            Place(targetText.rectTransform, new Vector2(.5f, .5f), targetPos, targetSize);
            targetText.fontSize = targetFont;
        }

        void ConfigureEvidenceChildren(Vector2 tableSize, Vector2 tablePos, float tableFont,
            Vector2 measureSize, Vector2 measurePos, float measureFont)
        {
            Place(squareTableText.rectTransform, new Vector2(.5f, .5f), tablePos, tableSize);
            squareTableText.fontSize = tableFont;
            Place(measureText.rectTransform, new Vector2(.5f, .5f), measurePos, measureSize);
            measureText.fontSize = measureFont;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = Vector2.one * .5f; rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        public void FillScreenProbe(MoraejwiMeollittwigiGame.GameState st)
        {
            if (current == null || !boardRoot.gameObject.activeSelf) return;
            WriteTopLeft(st.originPx, GridWorld(Vector2.zero));
            WriteTopLeft(st.handlePx, mouseRoot.position);
            Vector2[] basis = { Vector2.zero, new Vector2(1, 0), new Vector2(0, 1) };
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = cam.WorldToScreenPoint(GridWorld(basis[i]));
                st.gridPx[i * 2] = Mathf.RoundToInt(p.x);
                st.gridPx[i * 2 + 1] = Mathf.RoundToInt(Screen.height - p.y);
            }
        }

        void WriteTopLeft(int[] target, Vector3 world)
        {
            Vector3 p = cam.WorldToScreenPoint(world);
            target[0] = Mathf.RoundToInt(p.x); target[1] = Mathf.RoundToInt(Screen.height - p.y);
        }
    }
}
