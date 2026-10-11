// 삐뚤 대문 — 자정 벨벳 봉제극장 2.5D 무대와 UI.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.PpittulDaemun
{
    public partial class PpittulDaemunGame
    {
        static readonly Color Plum = MgfLook.Hex("#2D1738");
        static readonly Color Wine = MgfLook.Hex("#6E315F");
        static readonly Color Ivory = MgfLook.Hex("#E9DEC9");
        static readonly Color Cyan = MgfLook.Hex("#3BB6B0");
        static readonly Color Coral = MgfLook.Hex("#F06C5C");
        static readonly Color Ink = MgfLook.Hex("#160E1D");
        static readonly Color Brass = MgfLook.Hex("#C7A25B");

        Camera cam;
        Transform stageRoot, curtainRoot, gateRoot, lambRoot, needleRoot, stitchRail;
        readonly Transform[] curtainTassels = new Transform[8];
        readonly Transform[] lambs = new Transform[12];
        readonly Transform[] lambBodies = new Transform[12];
        readonly Transform[] lambFaces = new Transform[12];
        readonly Transform[] needles = new Transform[3];
        readonly Transform[] footlights = new Transform[7];
        readonly Transform[] stitchTokens = new Transform[11];
        Transform latch, worldHandle, bellWorld;
        LineRenderer[] bars = new LineRenderer[4];
        LineRenderer[] stitches = new LineRenderer[4];
        LineRenderer diagAC, diagBD, angleArcA, angleArcB, wrongLooseThread;
        readonly TextMeshPro[] vertexLabels = new TextMeshPro[5];
        Material bakeliteMat, brassMat, ivoryMat, cyanMat, coralMat, wineMat, plumMat, threadDarkMat;
        Vector3 gateA, gateB, gateC, gateD, gateO;
        float shownDetent = 2f, shownVelocity;
        float hitStop;
        float latchKick;
        float wrongThreadT;
        float stitchBurst;
        float lambCheer;
        int stitchBurstIndex = -1;

        CanvasGroup titleG, hudG, endG;
        RectTransform titleRt, hudRt, endRt;
        RawImage titleArt;
        RectTransform bellRopeRt, bellKnobRt;
        TextMeshProUGUI titleLogoBack, titleLogoMid, titleLogoFront, titleTag, titleBadge, titleBest, titlePull;
        TextMeshProUGUI scoreTxt, progressTxt, needlesTxt, timerTxt, goalTxt, promptTxt, relationTxt, angleTxt, handleLabelTxt;
        TextMeshProUGUI feedbackTxt, endTitleTxt, endScoreTxt, endStatsTxt, endHintTxt;
        RectTransform scoreRt, goalRt, promptRt, relationRt, trackRt, handleRt, guideFingerRt, guideArrowRt, toastRt, restartRt;
        Image handleImage, guideFingerImage, guideArrowImage, toastImage, flashImage;
        readonly RectTransform[] detentRt = new RectTransform[11];
        readonly TextMeshProUGUI[] detentTxt = new TextMeshProUGUI[11];
        readonly Image[] detentMark = new Image[11];
        readonly Image[] hudNeedleIcons = new Image[3];
        readonly RectTransform[] guideDots = new RectTransform[7];
        readonly RectTransform[] ripples = new RectTransform[6];
        readonly Image[] rippleImages = new Image[6];
        readonly float[] rippleT = { 9, 9, 9, 9, 9, 9 };
        readonly RectTransform[] handleTrails = new RectTransform[3];
        readonly Image[] handleTrailImages = new Image[3];
        Sprite roundSprite, ringSprite, diamondSprite;
        int rippleCursor;
        int layoutW = -1, layoutH = -1;
        float toastT, guideBoost, bellPullPx;
        float displayedScore, displayedEndScore;
        int lastDisplayedScore = -1;
        int lastRelationDetent = -999;
        bool guideTargetMode;
        readonly Vector3[] uiCorners = new Vector3[4];

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var im = rt.gameObject.AddComponent<Image>();
            im.color = color; im.sprite = sprite; im.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color,
            float width = 360, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 1.75f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text;
            return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring, bool diamond = false)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                if (diamond)
                {
                    float d = Mathf.Abs(fx - size * .5f) + Mathf.Abs(fy - size * .5f);
                    a = Mathf.Clamp01((size * .42f - d) * .7f);
                }
                else if (ring)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(1 - Mathf.Abs(d - size * .40f) / (size * .075f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * .5f) - (size * .5f - radius), 0);
                    float qy = Mathf.Max(Mathf.Abs(fy - size * .5f) - (size * .5f - radius), 0);
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            float b = ring || diamond ? 0 : radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        void BuildWorld()
        {
            // WebGL 엔진 코드 스트리핑이 CreatePrimitive가 런타임에 붙이는 콜라이더 타입을
            // 지우지 않게 명시적으로 보존한다. (Cylinder→CapsuleCollider, Sphere→SphereCollider)
            var keepColliderTypes = new GameObject("PrimitiveColliderTypes");
            keepColliderTypes.AddComponent<CapsuleCollider>();
            keepColliderTypes.AddComponent<SphereCollider>();
            Destroy(keepColliderTypes);

            MgfLook.Sky(Plum * .55f, Wine * .65f, Ink, .62f);
            MgfLook.Sun(new Vector3(42, -28, 18), MgfLook.Hex("#FFE4BC"), 1.28f, .72f);
            cam = MgfLook.Camera(new Vector3(0, 3.0f, -18f), new Vector3(0, 2.05f, 0), 32f);
            cam.orthographic = true; cam.orthographicSize = 5.4f;

            Texture velvet = Resources.Load<Texture2D>("PpittulDaemun/velvet_tile");
            plumMat = MgfLook.Lit(Plum, .08f, .02f, null, velvet);
            wineMat = MgfLook.Lit(Wine, .12f, .03f, null, velvet);
            bakeliteMat = MgfLook.Lit(MgfLook.Hex("#211726"), .48f, .16f);
            brassMat = MgfLook.Lit(Brass, .52f, .68f);
            ivoryMat = MgfLook.Lit(Ivory, .08f, .02f);
            cyanMat = MgfLook.Lit(Cyan, .34f, .12f, Cyan * .10f);
            coralMat = MgfLook.Lit(Coral, .24f, .08f, Coral * .10f);
            threadDarkMat = MgfLook.Unlit(MgfLook.Hex("#BFAE95"));
            if (velvet)
            {
                plumMat.SetTextureScale("_MainTex", new Vector2(4, 3));
                wineMat.SetTextureScale("_MainTex", new Vector2(3, 5));
            }

            stageRoot = new GameObject("VelvetToyTheatre").transform;
            curtainRoot = new GameObject("LayeredCurtains").transform;
            curtainRoot.SetParent(stageRoot, false);
            gateRoot = new GameObject("ParallelogramGate").transform;
            gateRoot.SetParent(stageRoot, false);
            lambRoot = new GameObject("ThreadedLambParade").transform;
            lambRoot.SetParent(stageRoot, false);
            needleRoot = new GameObject("NeedleStand").transform;
            needleRoot.SetParent(stageRoot, false);

            // 화면 전체를 채우는 패치워크 프로시니엄. 빈 탁자 대신 층진 천·객석·풋라이트가 깊이를 만든다.
            MgfLook.Block("StageBack", new Vector3(0, 2.1f, 2.2f), new Vector3(16.5f, 9.3f, .7f), .32f, plumMat, stageRoot);
            MgfLook.Block("StageFloor", new Vector3(0, -1.45f, 1.0f), new Vector3(17.5f, .55f, 5.5f), .18f, wineMat, stageRoot);
            for (int row = 0; row < 3; row++)
            for (int i = 0; i < 10 + row * 2; i++)
            {
                float x = -7.7f + i * (15.4f / (9 + row * 2));
                var seat = MgfLook.Block("PatchSeat" + row + "_" + i,
                    new Vector3(x, -1.12f + row * .28f, 3.0f + row * .7f), new Vector3(.72f, .45f, .72f), .20f,
                    (i + row) % 3 == 0 ? coralMat : ((i + row) % 3 == 1 ? wineMat : cyanMat), stageRoot);
                seat.transform.localRotation = Quaternion.Euler(-7, 0, 0);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                for (int layer = 0; layer < 3; layer++)
                {
                    float x = side * (6.1f + layer * .85f);
                    var c = MgfLook.Block("Curtain" + side + "_" + layer, new Vector3(x, 2.7f, 1.1f + layer * .3f),
                        new Vector3(2.4f, 8.6f, .42f), .26f, layer == 1 ? plumMat : wineMat, curtainRoot);
                    c.transform.localRotation = Quaternion.Euler(0, side * (7 + layer * 2), side * (2 + layer));
                }
                MgfLook.Block("ProsceniumPillar" + side, new Vector3(side * 7.45f, 2.2f, -.2f),
                    new Vector3(1.05f, 8.8f, 1.1f), .18f, bakeliteMat, stageRoot);
            }
            MgfLook.Block("Arch", new Vector3(0, 6.05f, -.2f), new Vector3(15.9f, .95f, 1.2f), .28f, bakeliteMat, stageRoot);
            for (int i = 0; i < curtainTassels.Length; i++)
            {
                float x = -5.25f + i * 1.5f;
                var cord = MgfLook.Prim(PrimitiveType.Cylinder, "TasselCord" + i, new Vector3(x, 5.25f, -.9f),
                    new Vector3(.075f, .72f, .075f), brassMat, curtainRoot, false);
                var tassel = MgfLook.Prim(PrimitiveType.Sphere, "Tassel" + i, new Vector3(x, 4.45f, -.9f),
                    new Vector3(.22f, .34f, .22f), i % 2 == 0 ? coralMat : cyanMat, curtainRoot, false);
                curtainTassels[i] = tassel.transform;
            }
            // 실감개 탑과 재봉대.
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 4; i++)
            {
                var spool = MgfLook.Prim(PrimitiveType.Cylinder, "Spool" + side + "_" + i,
                    new Vector3(side * (6.0f + (i % 2) * .55f), -.55f + i * .65f, .2f),
                    new Vector3(.48f, .28f, .48f), i % 2 == 0 ? cyanMat : coralMat, stageRoot);
                spool.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            for (int i = 0; i < footlights.Length; i++)
            {
                float x = -4.8f + i * 1.6f;
                footlights[i] = MgfLook.Prim(PrimitiveType.Sphere, "CyanFootlight" + i,
                    new Vector3(x, -1.25f, -.8f), new Vector3(.26f, .18f, .24f), cyanMat, stageRoot, false).transform;
            }

            // 대상을 문으로 통과시키는 대신, 정답마다 관계 조각 하나를 무대 아치에 꿰맨다.
            stitchRail = new GameObject("RelationshipStitchRail").transform;
            stitchRail.SetParent(stageRoot, false);
            MgfLook.Block("BrassPatternRail", new Vector3(0, 4.48f, -.35f), new Vector3(10.5f, .12f, .18f), .04f,
                brassMat, stitchRail);
            for (int i = 0; i < stitchTokens.Length; i++)
            {
                float x = -4.55f + i * .91f;
                var token = MgfLook.Block("RelationPatch" + (i + 1), new Vector3(x, 4.48f, -.55f),
                    new Vector3(.52f, .52f, .14f), .16f, i % 2 == 0 ? coralMat : cyanMat, stitchRail);
                token.transform.localRotation = Quaternion.Euler(0, 0, 45);
                stitchTokens[i] = token.transform;
            }

            BuildGate();
            BuildLambs();
            BuildNeedles();

            bellWorld = new GameObject("TitleBellRopeWorld").transform;
            bellWorld.SetParent(stageRoot, false);
            var rope = MgfLook.Prim(PrimitiveType.Cylinder, "BellCord", new Vector3(0, 2.6f, -.8f),
                new Vector3(.075f, 2.7f, .075f), brassMat, bellWorld, false);
            rope.transform.localRotation = Quaternion.identity;
            MgfLook.Prim(PrimitiveType.Sphere, "BellPull", new Vector3(0, -.15f, -.8f),
                new Vector3(.48f, .48f, .28f), coralMat, bellWorld, false);
        }

        void BuildGate()
        {
            for (int i = 0; i < 4; i++)
            {
                bars[i] = MakeLine("BakeliteBar" + i, MgfLook.Unlit(MgfLook.Hex("#1E1622")), .26f, gateRoot);
                stitches[i] = MakeLine("CoralStitch" + i, i % 2 == 0 ? MgfLook.Unlit(Coral) : MgfLook.Unlit(Cyan), .055f, gateRoot);
            }
            diagAC = MakeLine("DiagonalAC", MgfLook.Unlit(Cyan), .065f, gateRoot);
            diagBD = MakeLine("DiagonalBD", MgfLook.Unlit(Coral), .065f, gateRoot);
            diagAC.textureMode = LineTextureMode.Tile; diagBD.textureMode = LineTextureMode.Tile;
            angleArcA = MakeLine("OppositeAngleArcA", MgfLook.Unlit(Coral), .075f, gateRoot);
            angleArcB = MakeLine("AdjacentAngleArcB", MgfLook.Unlit(Cyan), .075f, gateRoot);
            wrongLooseThread = MakeLine("LooseThread", MgfLook.Unlit(Ivory), .055f, gateRoot);
            wrongLooseThread.positionCount = 12; wrongLooseThread.enabled = false;
            latch = MgfLook.Block("BrassLatch", new Vector3(0, 0, -.12f), new Vector3(.75f, .22f, .32f), .10f, brassMat, gateRoot).transform;
            worldHandle = MgfLook.Prim(PrimitiveType.Sphere, "BHandle", Vector3.zero, new Vector3(.48f, .48f, .36f), brassMat, gateRoot).transform;
            for (int i = 0; i < vertexLabels.Length; i++)
            {
                string s = i < 4 ? ((char)('A' + i)).ToString() : "O";
                vertexLabels[i] = MgfText.World(s, Vector3.zero, 2.2f, i == 4 ? Ivory : Brass, gateRoot);
                vertexLabels[i].fontStyle = FontStyles.Bold;
                vertexLabels[i].transform.rotation = cam.transform.rotation;
            }
            SetGateGeometry(2f);
        }

        LineRenderer MakeLine(string name, Material mat, float width, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat; lr.startWidth = lr.endWidth = width;
            lr.useWorldSpace = false; lr.numCapVertices = 6; lr.numCornerVertices = 5;
            lr.positionCount = 2; lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
            return lr;
        }

        void BuildLambs()
        {
            for (int i = 0; i < lambs.Length; i++)
            {
                var root = new GameObject("ThreadedLamb" + i).transform;
                root.SetParent(lambRoot, false);
                lambs[i] = root;
                var body = MgfLook.Prim(PrimitiveType.Sphere, "FeltBody", Vector3.zero,
                    new Vector3(.72f, .56f, .50f), ivoryMat, root, false).transform;
                lambBodies[i] = body;
                var face = MgfLook.Block("BakeliteFace", new Vector3(.45f, .05f, -.18f),
                    new Vector3(.34f, .42f, .16f), .08f, bakeliteMat, root).transform;
                lambFaces[i] = face;
                for (int leg = 0; leg < 4; leg++)
                {
                    float lx = leg < 2 ? -.27f : .27f;
                    float lz = leg % 2 == 0 ? -.17f : .17f;
                    MgfLook.Prim(PrimitiveType.Cylinder, "Foot" + leg, new Vector3(lx, -.45f, lz),
                        new Vector3(.07f, .18f, .07f), bakeliteMat, root, false);
                }
                var tag = MgfLook.Block("RibbonTag", new Vector3(-.48f, .03f, -.22f), new Vector3(.30f, .38f, .08f), .03f,
                    i % 2 == 0 ? coralMat : cyanMat, root);
                tag.transform.localRotation = Quaternion.Euler(0, 0, 18);
                var tail = MgfLook.Prim(PrimitiveType.Cylinder, "SpoolTail", new Vector3(-.56f, .08f, .08f),
                    new Vector3(.17f, .13f, .17f), brassMat, root, false);
                tail.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
        }

        void BuildNeedles()
        {
            MgfLook.Block("NeedleBench", new Vector3(5.8f, -.84f, -.1f), new Vector3(2.1f, .45f, 1.15f), .12f, bakeliteMat, needleRoot);
            for (int i = 0; i < needles.Length; i++)
            {
                var n = MgfLook.Prim(PrimitiveType.Cylinder, "Needle" + i,
                    new Vector3(5.25f + i * .55f, -.12f, -.3f), new Vector3(.055f, .68f, .055f), brassMat, needleRoot, false);
                n.transform.localRotation = Quaternion.Euler(0, 0, -18);
                needles[i] = n.transform;
            }
        }

        void BuildUi()
        {
            var canvas = MgfText.Canvas;
            roundSprite = MakeSprite(64, 14, false);
            ringSprite = MakeSprite(96, 0, true);
            diamondSprite = MakeSprite(48, 0, false, true);

            titleRt = R("Title", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one; titleRt.sizeDelta = Vector2.zero;
            titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            var artRt = R("TitleArt", titleRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            artRt.anchorMin = Vector2.zero; artRt.anchorMax = Vector2.one; artRt.sizeDelta = Vector2.zero;
            titleArt = artRt.gameObject.AddComponent<RawImage>();
            // 타이틀도 플레이와 같은 절차적 로우폴리 봉제극장을 그대로 보여 준다.
            titleArt.texture = null;
            titleArt.color = new Color(1, 1, 1, 0);
            titleArt.raycastTarget = false;

            // 제목은 무대 아치에 꿰맨 세 겹 자수 로고. 독립 텍스트 블록/사각 CTA가 아니다.
            var marquee = Img(titleRt, "ArchedMarquee", new Vector2(.5f, .82f), Vector2.zero, new Vector2(350, 150), Wine, roundSprite);
            marquee.rectTransform.localRotation = Quaternion.Euler(0, 0, -1.5f);
            Img(marquee.transform, "MarqueeStitch", new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 130), new Color(0, 0, 0, 0), ringSprite);
            titleLogoBack = Txt(marquee.transform, "삐뚤 대문", new Vector2(.5f, .60f), new Vector2(5, -5), 48, Ink, 330);
            titleLogoMid = Txt(marquee.transform, "삐뚤 대문", new Vector2(.5f, .60f), new Vector2(2, -2), 48, Coral, 330);
            titleLogoFront = Txt(marquee.transform, "삐뚤 대문", new Vector2(.5f, .60f), Vector2.zero, 48, Ivory, 330);
            titleLogoFront.fontStyle = titleLogoMid.fontStyle = titleLogoBack.fontStyle = FontStyles.Bold;
            titleTag = Txt(marquee.transform, "틀을 밀어 문을 맞춰라", new Vector2(.5f, .20f), Vector2.zero, 16, Cyan, 320);
            titleBadge = Txt(titleRt, "중2 · 사각형의 성질 · 90초", new Vector2(.5f, .68f), Vector2.zero, 14, Ivory, 340);
            titleBest = Txt(titleRt, "", new Vector2(.5f, .12f), Vector2.zero, 13, Ivory, 340);

            bellRopeRt = R("BellRope", titleRt, new Vector2(.5f, .36f), Vector2.zero, new Vector2(18, 180));
            Img(bellRopeRt, "BrassCord", new Vector2(.5f, .5f), Vector2.zero, new Vector2(8, 180), Brass, roundSprite);
            bellKnobRt = Img(bellRopeRt, "BellKnob", new Vector2(.5f, 0), new Vector2(0, -22), new Vector2(88, 64), Coral, roundSprite).rectTransform;
            titlePull = Txt(bellKnobRt, "당겨 열기", new Vector2(.5f, .5f), Vector2.zero, 18, Ivory, 116);
            titlePull.fontStyle = FontStyles.Bold;

            hudRt = R("Hud", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            hudRt.anchorMin = Vector2.zero; hudRt.anchorMax = Vector2.one; hudRt.sizeDelta = Vector2.zero;
            hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            var top = Img(hudRt, "TopStitchRail", new Vector2(.5f, 1f), new Vector2(0, -25), new Vector2(10, 50), new Color(Plum.r, Plum.g, Plum.b, .96f));
            top.rectTransform.anchorMin = new Vector2(0, 1); top.rectTransform.anchorMax = new Vector2(1, 1); top.rectTransform.sizeDelta = new Vector2(0, 50);
            scoreTxt = Txt(hudRt, "0", new Vector2(.04f, .97f), Vector2.zero, 23, Coral, 100, TextAlignmentOptions.Left);
            scoreTxt.rectTransform.pivot = new Vector2(0, .5f); scoreRt = scoreTxt.rectTransform;
            progressTxt = Txt(hudRt, "문 0/11 · 첫 시도 0", new Vector2(.50f, .97f), Vector2.zero, 14, Ivory, 250);
            needlesTxt = Txt(hudRt, "바늘 3", new Vector2(.96f, .97f), Vector2.zero, 14, Brass, 96, TextAlignmentOptions.Right);
            needlesTxt.rectTransform.pivot = new Vector2(1, .5f);
            timerTxt = Txt(hudRt, "90초", new Vector2(.78f, .97f), Vector2.zero, 13, Cyan, 76);
            for (int i = 0; i < hudNeedleIcons.Length; i++)
            {
                hudNeedleIcons[i] = Img(hudRt, "HudNeedle" + i, new Vector2(.865f + i * .018f, .955f),
                    Vector2.zero, new Vector2(4, 19), Brass, roundSprite);
                hudNeedleIcons[i].rectTransform.localRotation = Quaternion.Euler(0, 0, -18);
            }

            var goal = Img(hudRt, "GoalRibbon", new Vector2(.5f, .91f), Vector2.zero, new Vector2(360, 38), Wine, roundSprite);
            goalRt = goal.rectTransform;
            Img(goal.transform, "GoalTailL", new Vector2(0, .5f), new Vector2(-10, 0), new Vector2(28, 28), Coral, diamondSprite);
            Img(goal.transform, "GoalTailR", new Vector2(1, .5f), new Vector2(10, 0), new Vector2(28, 28), Coral, diamondSprite);
            goalTxt = Txt(goal.transform, "대문을 밀어 문표에 맞는 각으로 잠그시오", new Vector2(.5f, .5f), Vector2.zero, 14, Ivory, 335);
            goalTxt.fontStyle = FontStyles.Bold;

            promptRt = Img(hudRt, "LambTag", new Vector2(.5f, .80f), Vector2.zero, new Vector2(356, 88), new Color(Ivory.r, Ivory.g, Ivory.b, .97f), roundSprite).rectTransform;
            Img(promptRt, "TagHole", new Vector2(.05f, .5f), Vector2.zero, new Vector2(18, 18), Plum, ringSprite);
            promptTxt = Txt(promptRt, "", new Vector2(.54f, .5f), Vector2.zero, 18, Plum, 308);
            promptTxt.fontStyle = FontStyles.Bold;
            relationRt = Img(hudRt, "RelationRibbon", new Vector2(.5f, .705f), Vector2.zero, new Vector2(332, 38), new Color(Plum.r, Plum.g, Plum.b, .91f), roundSprite).rectTransform;
            relationTxt = Txt(relationRt, "A=C · A+B=180° · AO=CO · BO=DO", new Vector2(.5f, .5f), Vector2.zero, 13, Cyan, 318);

            angleTxt = Txt(hudRt, "∠A 60°", new Vector2(.5f, .36f), Vector2.zero, 35, Ivory, 260);
            angleTxt.fontStyle = FontStyles.Bold;
            trackRt = R("BrassDetentRail", hudRt, new Vector2(.5f, .25f), Vector2.zero, new Vector2(360, 108));
            Img(trackRt, "Rail", new Vector2(.5f, .5f), Vector2.zero, new Vector2(326, 10), Brass, roundSprite);
            for (int i = 0; i < 11; i++)
            {
                float x = Mathf.Lerp(-156, 156, i / 10f);
                detentRt[i] = R("Detent" + i, trackRt, new Vector2(.5f, .5f), new Vector2(x, 0), new Vector2(28, 70));
                detentMark[i] = Img(detentRt[i], "Notch", new Vector2(.5f, .5f), Vector2.zero, new Vector2(7, i == 5 ? 28 : 20), i == 5 ? Coral : Brass, roundSprite);
                detentTxt[i] = Txt(detentRt[i], (40 + i * 10).ToString(), new Vector2(.5f, 0), new Vector2(0, -10), 10, Ivory, 34);
            }
            handleRt = Img(trackRt, "BHandle", new Vector2(.5f, .5f), new Vector2(-93.6f, 4), new Vector2(68, 68), Brass, roundSprite).rectTransform;
            handleImage = handleRt.GetComponent<Image>();
            Img(handleRt, "ThreadCross", new Vector2(.5f, .5f), Vector2.zero, new Vector2(42, 10), Coral, roundSprite);
            handleLabelTxt = Txt(handleRt, "B", new Vector2(.5f, .5f), Vector2.zero, 22, Plum, 58);
            handleLabelTxt.fontStyle = FontStyles.Bold;
            for (int i = 0; i < handleTrails.Length; i++)
            {
                handleTrailImages[i] = Img(trackRt, "ThreadTrail" + i, new Vector2(.5f, .5f), Vector2.zero,
                    new Vector2(56 - i * 8, 56 - i * 8), new Color(Coral.r, Coral.g, Coral.b, .18f - i * .035f), ringSprite);
                handleTrails[i] = handleTrailImages[i].rectTransform;
            }

            guideFingerRt = Img(hudRt, "GuideFinger", new Vector2(.5f, .25f), Vector2.zero, new Vector2(58, 58), Coral, ringSprite).rectTransform;
            guideFingerImage = guideFingerRt.GetComponent<Image>();
            guideArrowRt = Img(hudRt, "GuideArrow", new Vector2(.5f, .25f), Vector2.zero, new Vector2(36, 36), Coral, diamondSprite).rectTransform;
            guideArrowImage = guideArrowRt.GetComponent<Image>();
            for (int i = 0; i < guideDots.Length; i++)
                guideDots[i] = Img(hudRt, "GuideChevron" + i, new Vector2(.5f, .25f), Vector2.zero, new Vector2(18, 18), Cyan, diamondSprite).rectTransform;

            toastRt = Img(hudRt, "Toast", new Vector2(.5f, .49f), Vector2.zero, new Vector2(344, 72), new Color(Plum.r, Plum.g, Plum.b, .95f), roundSprite).rectTransform;
            toastImage = toastRt.GetComponent<Image>();
            feedbackTxt = Txt(toastRt, "", new Vector2(.5f, .5f), Vector2.zero, 15, Ivory, 324);
            toastRt.gameObject.SetActive(false);

            flashImage = Img(hudRt, "VerdictFlash", new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
            flashImage.rectTransform.anchorMin = Vector2.zero; flashImage.rectTransform.anchorMax = Vector2.one; flashImage.rectTransform.sizeDelta = Vector2.zero;
            for (int i = 0; i < ripples.Length; i++)
            {
                rippleImages[i] = Img(hudRt, "TapRipple" + i, new Vector2(0, 0), Vector2.zero, new Vector2(30, 30), new Color(Cyan.r, Cyan.g, Cyan.b, 0), ringSprite);
                ripples[i] = rippleImages[i].rectTransform;
            }

            endRt = R("End", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            endRt.anchorMin = Vector2.zero; endRt.anchorMax = Vector2.one; endRt.sizeDelta = Vector2.zero;
            endG = endRt.gameObject.AddComponent<CanvasGroup>();
            var veil = Img(endRt, "CurtainClose", new Vector2(.5f, .5f), Vector2.zero, Vector2.zero, new Color(Plum.r, Plum.g, Plum.b, .78f));
            veil.rectTransform.anchorMin = Vector2.zero; veil.rectTransform.anchorMax = Vector2.one; veil.rectTransform.sizeDelta = Vector2.zero;
            var medal = Img(endRt, "ResultMarquee", new Vector2(.5f, .67f), Vector2.zero, new Vector2(354, 220), Wine, roundSprite);
            endTitleTxt = Txt(medal.transform, "공연 완료", new Vector2(.5f, .72f), Vector2.zero, 34, Ivory, 330);
            endTitleTxt.fontStyle = FontStyles.Bold;
            endScoreTxt = Txt(medal.transform, "0점", new Vector2(.5f, .40f), Vector2.zero, 38, Coral, 330);
            endStatsTxt = Txt(medal.transform, "", new Vector2(.5f, .13f), Vector2.zero, 14, Cyan, 328);
            endHintTxt = Txt(endRt, "", new Vector2(.5f, .41f), Vector2.zero, 15, Ivory, 352);
            restartRt = R("RestartBellRope", endRt, new Vector2(.5f, .20f), Vector2.zero, new Vector2(120, 160));
            Img(restartRt, "Cord", new Vector2(.5f, .65f), Vector2.zero, new Vector2(8, 120), Brass, roundSprite);
            Img(restartRt, "Knob", new Vector2(.5f, .17f), Vector2.zero, new Vector2(116, 66), Coral, roundSprite);
            Txt(restartRt, "다시 공연", new Vector2(.5f, .17f), Vector2.zero, 18, Ivory, 120).fontStyle = FontStyles.Bold;
            SetGuideVisible(false);
        }

        void SetScreen()
        {
            bool title = flow == Flow.Title;
            bool end = flow == Flow.End;
            titleG.alpha = title ? 1 : 0; titleG.interactable = title; titleG.blocksRaycasts = false;
            hudG.alpha = !title && !end ? 1 : 0; hudG.interactable = !title && !end; hudG.blocksRaycasts = false;
            endG.alpha = end ? 1 : 0; endG.interactable = end; endG.blocksRaycasts = false;
            if (bellWorld) bellWorld.gameObject.SetActive(title);
            if (end) displayedEndScore = 0;
        }

        void ResetWorldForRun()
        {
            shownDetent = st.selectedDetent; shownVelocity = 0; hitStop = 0; latchKick = 0; wrongThreadT = 0;
            stitchBurst = 0; lambCheer = 0; stitchBurstIndex = -1;
            wrongLooseThread.enabled = false;
            for (int i = 0; i < needles.Length; i++) if (needles[i]) needles[i].gameObject.SetActive(true);
            RefreshStitchTokens();
            ArrangeLambs();
        }

        void ResetWorldForProblem()
        {
            latchKick = 0; wrongThreadT = 0; stitchBurst = 0; stitchBurstIndex = -1;
            wrongLooseThread.enabled = false;
            RefreshStitchTokens();
            ArrangeLambs();
        }

        void SetGateDetent(int detent, bool animate)
        {
            if (!animate) { shownDetent = detent; shownVelocity = 0; SetGateGeometry(shownDetent); }
        }

        void SetGateGeometry(float detent)
        {
            int selected = Mathf.Clamp(Mathf.RoundToInt(detent), 0, 10);
            float angle = PpittulRules.AngleForSelection(current, selected);
            bool equalSides = PpittulRules.SelectionHasEqualSides(current, selected);
            float baseLength = equalSides ? 3.8f : 5.2f;
            float sideLength = equalSides ? baseLength : 2.4f / Mathf.Sin(angle * Mathf.Deg2Rad);
            float h = sideLength * Mathf.Sin(angle * Mathf.Deg2Rad);
            float off = sideLength * Mathf.Cos(angle * Mathf.Deg2Rad);
            // 중심을 고정해 가장 기울어진 문도 390px 세로 화면 안에 남긴다.
            gateA = new Vector3(-baseLength * .5f - off * .5f, -.68f, 0);
            gateD = gateA + new Vector3(baseLength, 0, 0);
            gateB = gateA + new Vector3(off, h, 0);
            gateC = gateD + new Vector3(off, h, 0);
            gateO = (gateA + gateC) * .5f;
            Vector3[] v = { gateA, gateB, gateC, gateD };
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = v[i], q = v[(i + 1) % 4];
                bars[i].SetPosition(0, p); bars[i].SetPosition(1, q);
                stitches[i].SetPosition(0, Vector3.Lerp(p, q, .04f) + Vector3.back * .02f);
                stitches[i].SetPosition(1, Vector3.Lerp(p, q, .96f) + Vector3.back * .02f);
            }
            diagAC.SetPosition(0, gateA + Vector3.back * .03f); diagAC.SetPosition(1, gateC + Vector3.back * .03f);
            diagBD.SetPosition(0, gateB + Vector3.back * .04f); diagBD.SetPosition(1, gateD + Vector3.back * .04f);
            worldHandle.localPosition = gateB + new Vector3(0, .02f, -.15f);
            latch.localPosition = gateB + new Vector3(.45f, .1f, -.12f);
            vertexLabels[0].transform.localPosition = gateA + new Vector3(.18f, -.34f, -.08f);
            vertexLabels[1].transform.localPosition = gateB + new Vector3(.18f, .34f, -.08f);
            vertexLabels[2].transform.localPosition = gateC + new Vector3(-.18f, .34f, -.08f);
            vertexLabels[3].transform.localPosition = gateD + new Vector3(-.18f, -.34f, -.08f);
            vertexLabels[4].transform.localPosition = gateO + new Vector3(0, .24f, -.08f);
            SetArc(angleArcA, gateA, 0, angle, .56f);
            SetArc(angleArcB, gateB, -angle, 180 - angle, .48f);
        }

        void SetArc(LineRenderer lr, Vector3 center, float startDeg, float spanDeg, float r)
        {
            const int N = 14; lr.positionCount = N;
            for (int i = 0; i < N; i++)
            {
                float a = (startDeg + spanDeg * i / (N - 1f)) * Mathf.Deg2Rad;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, -.05f));
            }
        }

        void ArrangeLambs()
        {
            for (int i = 0; i < lambs.Length; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                int row = i / 2;
                float x = side * (4.25f + (row % 3) * .68f);
                float y = -.72f + (row / 3) * .30f;
                float z = .18f + (row % 3) * .30f;
                lambs[i].localPosition = new Vector3(x, y, z);
                lambs[i].localScale = Vector3.one * .72f;
                lambs[i].gameObject.SetActive(true);
            }
        }

        void RefreshStitchTokens()
        {
            for (int i = 0; i < stitchTokens.Length; i++)
            {
                bool earned = flow == Flow.Title || flow == Flow.End || i < st.solved;
                stitchTokens[i].gameObject.SetActive(earned);
                if (earned && i != stitchBurstIndex) stitchTokens[i].localScale = Vector3.one;
            }
        }

        void UpdateWorld(float dt)
        {
            if (!cam) return;
            bool portrait = Screen.height > Screen.width;
            cam.orthographicSize = portrait ? 7.2f : 5.4f;
            cam.transform.position = portrait ? new Vector3(0, 2.65f, -18f) : new Vector3(0, 3.0f, -18f);
            cam.transform.LookAt(new Vector3(0, portrait ? 1.65f : 2.0f, 0));
            gateRoot.localScale = Vector3.one * (portrait ? .78f : 1f);

            float time = Time.unscaledTime;
            for (int i = 0; i < curtainTassels.Length; i++)
                if (curtainTassels[i]) curtainTassels[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * .8f + i) * 6f);
            for (int i = 0; i < footlights.Length; i++)
            {
                float s = .92f + .12f * Mathf.Sin(time * 2.1f + i * .7f);
                footlights[i].localScale = new Vector3(.26f, .18f, .24f) * s * (1 + latchKick * .35f);
            }
            for (int i = 0; i < lambs.Length; i++)
            {
                if (!lambs[i].gameObject.activeSelf) continue;
                var p = lambs[i].localPosition;
                float cheer = lambCheer > 0 ? Mathf.Abs(Mathf.Sin((.9f - lambCheer) * 13f + i * .4f)) * .16f : 0;
                p.y += Mathf.Sin(time * 2.4f + i * 1.7f) * .0025f;
                lambs[i].localPosition = p;
                lambs[i].localScale = Vector3.one * (.72f + cheer);
                lambFaces[i].localRotation = Quaternion.Euler(0, Mathf.Sin(time * 1.4f + i) * 5f, 0);
            }
            lambCheer = Mathf.Max(0, lambCheer - dt);

            if (hitStop > 0) { hitStop -= dt; dt = 0; }
            shownDetent = Mathf.SmoothDamp(shownDetent, st.selectedDetent, ref shownVelocity, dragging ? .055f : .12f, 30f, Mathf.Max(.0001f, dt));
            SetGateGeometry(shownDetent);
            latchKick = Mathf.MoveTowards(latchKick, 0, dt * 2.6f);
            latch.localRotation = Quaternion.Euler(0, 0, -18f * latchKick);

            if (wrongThreadT > 0)
            {
                wrongThreadT = Mathf.Max(0, wrongThreadT - dt);
                wrongLooseThread.enabled = true;
                wrongLooseThread.positionCount = 12;
                for (int i = 0; i < 12; i++)
                {
                    float k = i / 11f;
                    Vector3 p = Vector3.Lerp(gateA, gateC, k);
                    p += new Vector3(0, Mathf.Sin(k * 22f + time * 18f) * .14f * (1 - k), -.12f);
                    wrongLooseThread.SetPosition(i, p);
                }
                if (wrongThreadT <= 0) wrongLooseThread.enabled = false;
            }
            if (stitchBurst > 0 && stitchBurstIndex >= 0)
            {
                stitchBurst = Mathf.Max(0, stitchBurst - dt);
                float k = 1f + Mathf.Sin((1f - stitchBurst) * Mathf.PI) * .75f;
                stitchTokens[stitchBurstIndex].localScale = Vector3.one * k;
                stitchTokens[stitchBurstIndex].localRotation = Quaternion.Euler(0, 0, 45 + (1f - stitchBurst) * 180f);
                if (stitchBurst <= 0)
                    stitchTokens[stitchBurstIndex].localRotation = Quaternion.Euler(0, 0, 45);
            }
        }

        void UpdateUi(float dt)
        {
            LayoutUi();
            if (flow == Flow.Title)
            {
                float pulse = 1f + .045f * Mathf.Sin(Time.unscaledTime * 2.8f);
                bellKnobRt.localScale = new Vector3(pulse, pulse, 1);
                titleLogoFront.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * .7f) * .7f);
                titleBest.text = bestScore > 0 ? "최고 " + bestScore + "점 · 첫 시도 " + bestFirst + "/11" : "첫 공연을 기다리는 중";
            }

            displayedScore = Mathf.MoveTowards(displayedScore, st.score, Mathf.Max(90, Mathf.Abs(st.score - displayedScore) * 5f) * dt);
            int shownScore = Mathf.RoundToInt(displayedScore);
            if (shownScore != lastDisplayedScore)
            {
                scoreTxt.text = shownScore.ToString("N0"); lastDisplayedScore = shownScore;
            }
            progressTxt.text = "문 " + st.solved + "/11 · 첫 시도 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal;
            needlesTxt.text = "바늘 " + Mathf.Max(0, st.lives);
            timerTxt.text = flow == Flow.Practice ? "연습" : Mathf.Max(0, Mathf.CeilToInt(90 - playTime)) + "초";
            bool propertyMode = PpittulRules.IsPropertyProblem(current);
            angleTxt.text = propertyMode ? "조건  " + PpittulRules.ConditionLabel(current, st.selectedDetent) : "∠A=" + st.selectedAngle + "°";
            for (int i = 0; i < hudNeedleIcons.Length; i++)
            {
                bool remaining = i < st.lives;
                hudNeedleIcons[i].color = remaining ? Brass : new Color(Coral.r, Coral.g, Coral.b, .28f);
                hudNeedleIcons[i].rectTransform.localRotation = Quaternion.Euler(0, 0, remaining ? -18 : 55);
            }
            handleRt.anchoredPosition = new Vector2(Mathf.Lerp(-156, 156, st.selectedDetent / 10f), 4);
            for (int i = 0; i < handleTrails.Length; i++)
            {
                Vector2 p = handleRt.anchoredPosition + new Vector2((i + 1) * (shownVelocity < 0 ? 9 : -9), 0);
                handleTrails[i].anchoredPosition = p;
                handleTrailImages[i].color = new Color(Coral.r, Coral.g, Coral.b, dragging ? .17f - i * .035f : 0);
            }
            handleRt.localScale = Vector3.Lerp(handleRt.localScale,
                dragging ? new Vector3(1.18f, .84f, 1) : Vector3.one, 1 - Mathf.Exp(-dt * 16f));
            for (int i = 0; i < 11; i++)
            {
                bool here = i == st.selectedDetent;
                detentMark[i].color = here ? Coral : Brass;
                detentTxt[i].color = here ? Ivory : new Color(Ivory.r, Ivory.g, Ivory.b, .72f);
            }
            if (propertyMode && lastRelationDetent != st.selectedDetent)
            {
                lastRelationDetent = st.selectedDetent;
                RefreshPropertyReadout();
            }

            if (flow == Flow.Practice || guideBoost > 0)
            {
                guideBoost = Mathf.Max(0, guideBoost - dt);
                UpdateGuide();
            }
            else SetGuideVisible(false);

            if (toastT > 0)
            {
                toastT -= dt;
                toastRt.gameObject.SetActive(true);
                float k = Mathf.Clamp01(toastT * 5f);
                toastRt.localScale = Vector3.one * (1f + .05f * Mathf.Sin(k * Mathf.PI));
            }
            else toastRt.gameObject.SetActive(false);

            for (int i = 0; i < ripples.Length; i++)
            {
                if (rippleT[i] >= 1) continue;
                rippleT[i] += dt * 2.8f;
                float k = Mathf.Clamp01(rippleT[i]);
                ripples[i].sizeDelta = Vector2.one * Mathf.Lerp(24, 110, k);
                rippleImages[i].color = new Color(Cyan.r, Cyan.g, Cyan.b, (1 - k) * .7f);
            }

            if (flow == Flow.Reveal)
            {
                float a = Mathf.Sin(Mathf.Clamp01(revealTime / .45f) * Mathf.PI) * .18f;
                flashImage.color = revealCorrect ? new Color(Cyan.r, Cyan.g, Cyan.b, a) : new Color(Coral.r, Coral.g, Coral.b, a * .55f);
            }
            else flashImage.color = new Color(0, 0, 0, 0);

            if (flow == Flow.End)
            {
                displayedEndScore = Mathf.MoveTowards(displayedEndScore, st.score, Mathf.Max(120, st.score * 1.8f) * dt);
                endScoreTxt.text = Mathf.RoundToInt(displayedEndScore).ToString("N0") + "점";
                restartRt.localScale = Vector3.one * (1f + .035f * Mathf.Sin(Time.unscaledTime * 3.2f));
            }
        }

        void LayoutUi()
        {
            if (layoutW == Screen.width && layoutH == Screen.height) return;
            layoutW = Screen.width; layoutH = Screen.height;
            bool land = Screen.width >= 1024 && Screen.width > Screen.height;
            float trackY = land ? .205f : .245f;
            trackRt.anchorMin = trackRt.anchorMax = new Vector2(.5f, trackY);
            angleTxt.rectTransform.anchorMin = angleTxt.rectTransform.anchorMax = new Vector2(.5f, land ? .305f : .35f);
            promptRt.anchorMin = promptRt.anchorMax = new Vector2(land ? .30f : .5f, land ? .74f : .79f);
            promptRt.sizeDelta = land ? new Vector2(280, 96) : new Vector2(356, 88);
            relationRt.anchorMin = relationRt.anchorMax = new Vector2(land ? .70f : .5f, land ? .74f : .695f);
            relationRt.sizeDelta = land ? new Vector2(280, 58) : new Vector2(332, 38);
            relationTxt.fontSize = land ? 15 : 13;
            promptTxt.rectTransform.sizeDelta = new Vector2(land ? 246 : 308, land ? 72 : 64);
            relationTxt.rectTransform.sizeDelta = new Vector2(land ? 250 : 318, land ? 48 : 24);
            bool portrait = Screen.height > Screen.width;
            if (portrait)
            {
                // 우상단 64px은 HTML 음소거 버튼의 안전영역이다. 바늘 수와 아이콘을 그 왼쪽에 둔다.
                needlesTxt.rectTransform.anchorMin = needlesTxt.rectTransform.anchorMax = new Vector2(.62f, .975f);
                needlesTxt.rectTransform.sizeDelta = new Vector2(62, 25);
                timerTxt.rectTransform.anchorMin = timerTxt.rectTransform.anchorMax = new Vector2(.78f, .97f);
                progressTxt.rectTransform.anchorMin = progressTxt.rectTransform.anchorMax = new Vector2(.35f, .97f);
                progressTxt.rectTransform.sizeDelta = new Vector2(170, 25);
                for (int i = 0; i < hudNeedleIcons.Length; i++)
                    hudNeedleIcons[i].rectTransform.anchorMin = hudNeedleIcons[i].rectTransform.anchorMax = new Vector2(.58f + i * .024f, .946f);
                goalRt.sizeDelta = new Vector2(360, 38);
            }
            else
            {
                needlesTxt.rectTransform.anchorMin = needlesTxt.rectTransform.anchorMax = new Vector2(.91f, .97f);
                needlesTxt.rectTransform.sizeDelta = new Vector2(96, 25);
                timerTxt.rectTransform.anchorMin = timerTxt.rectTransform.anchorMax = new Vector2(.78f, .97f);
                progressTxt.rectTransform.anchorMin = progressTxt.rectTransform.anchorMax = new Vector2(.50f, .97f);
                progressTxt.rectTransform.sizeDelta = new Vector2(250, 25);
                for (int i = 0; i < hudNeedleIcons.Length; i++)
                    hudNeedleIcons[i].rectTransform.anchorMin = hudNeedleIcons[i].rectTransform.anchorMax = new Vector2(.842f + i * .014f, .955f);
                goalRt.sizeDelta = new Vector2(450, 38);
            }
            // cover 방식 UV. 이미지가 없으면 절차적 무대가 그대로 보인다.
            if (titleArt.texture)
            {
                float texA = (float)titleArt.texture.width / titleArt.texture.height;
                float scrA = (float)Screen.width / Screen.height;
                if (scrA > texA)
                {
                    float h = texA / scrA; titleArt.uvRect = new Rect(0, (1 - h) * .5f, 1, h);
                }
                else
                {
                    float w = scrA / texA; titleArt.uvRect = new Rect((1 - w) * .5f, 0, w, 1);
                }
            }
            Canvas.ForceUpdateCanvases();
            MeasureUiBounds();
        }

        void MeasureUiBounds()
        {
            promptRt.GetWorldCorners(uiCorners);
            st.promptLeftPx = Mathf.RoundToInt(uiCorners[0].x);
            relationRt.GetWorldCorners(uiCorners);
            st.relationRightPx = Mathf.RoundToInt(uiCorners[2].x);
            needlesTxt.rectTransform.GetWorldCorners(uiCorners);
            bool textSafe = uiCorners[0].x >= 0 && uiCorners[2].x <= Screen.width - (Screen.height > Screen.width ? 58 : 42);
            bool iconsSafe = true;
            for (int i = 0; i < hudNeedleIcons.Length; i++)
            {
                hudNeedleIcons[i].rectTransform.GetWorldCorners(uiCorners);
                if (uiCorners[0].x < 0 || uiCorners[2].x > Screen.width - (Screen.height > Screen.width ? 58 : 42)) iconsSafe = false;
            }
            st.hudNeedlesVisible = textSafe && iconsSafe;
        }

        void LoadProblemUi()
        {
            if (current == null) return;
            promptTxt.text = current.prompt;
            promptTxt.enableAutoSizing = true; promptTxt.fontSizeMin = 12; promptTxt.fontSizeMax = 18;
            bool propertyMode = PpittulRules.IsPropertyProblem(current);
            for (int i = 0; i < detentRt.Length; i++)
            {
                bool visible = !propertyMode || PpittulRules.IsPropertyDetent(i);
                detentRt[i].gameObject.SetActive(visible);
                if (!visible) continue;
                detentTxt[i].text = propertyMode ? PpittulRules.ConditionLabel(current, i) : (40 + i * 10).ToString();
                detentTxt[i].fontSize = propertyMode ? 8 : 10;
                detentTxt[i].rectTransform.sizeDelta = new Vector2(propertyMode ? 82 : 34, propertyMode ? 30 : 18);
            }
            handleLabelTxt.text = propertyMode ? "조건" : "B";
            handleLabelTxt.fontSize = propertyMode ? 13 : 22;
            lastRelationDetent = -999;
            if (propertyMode)
                RefreshPropertyReadout();
            else if (current.type == GatePromptType.AdjacentB || current.type == GatePromptType.AdjacentD)
                relationTxt.text = "이웃한 두 각의 합은 180° · A+B=180°";
            else relationTxt.text = "마주 보는 두 각의 크기는 같다 · ∠A=∠C";
            goalTxt.text = flow == Flow.Practice ? "손잡이를 실제로 밀어 첫 문을 여시오" :
                propertyMode ? "측정값을 읽고 최소 조건 걸쇠에 잠그시오" : "대문을 밀어 문표에 맞는 각으로 잠그시오";
        }

        void RefreshPropertyReadout()
        {
            if (current == null || !PpittulRules.IsPropertyProblem(current)) return;
            float ac = Vector3.Distance(gateA, gateC);
            float bd = Vector3.Distance(gateB, gateD);
            float ao = Vector3.Distance(gateA, gateO);
            float co = Vector3.Distance(gateC, gateO);
            float bo = Vector3.Distance(gateB, gateO);
            float dO = Vector3.Distance(gateD, gateO);
            float ab = Vector3.Distance(gateA, gateB);
            float bc = Vector3.Distance(gateB, gateC);
            if (current.type == GatePromptType.BisectAC)
                relationTxt.text = "AO=" + ao.ToString("0.0") + " cm · CO=" + co.ToString("0.0") + " cm";
            else if (current.type == GatePromptType.BisectBD)
                relationTxt.text = "BO=" + bo.ToString("0.0") + " cm · DO=" + dO.ToString("0.0") + " cm";
            else if (current.type == GatePromptType.EqualDiagonals)
                relationTxt.text = "AC=" + ac.ToString("0.0") + " cm · BD=" + bd.ToString("0.0") + " cm";
            else if (current.type == GatePromptType.PerpendicularDiagonals)
            {
                Vector3 u = (gateC - gateA).normalized, v = (gateD - gateB).normalized;
                bool perpendicular = Mathf.Abs(Vector3.Dot(u, v)) < .03f;
                relationTxt.text = "AB=" + ab.ToString("0.0") + " · BC=" + bc.ToString("0.0") + " · " + (perpendicular ? "AC⊥BD" : "AC와 BD 비수직");
            }
            else
                relationTxt.text = "AB" + (Mathf.Abs(ab - bc) < .03f ? "=BC" : "≠BC") + " · ∠A=" + st.selectedAngle + "°";
        }

        void RefreshHudImmediate()
        {
            MgfFx.Punch(scoreRt, .18f, .30f);
            MgfFx.Punch(handleRt, .12f, .24f);
        }

        void TickDetent()
        {
            if (dragging || keyboardGrab) MgfSfx.Play("tap", .12f);
        }

        void HandleGrab(bool on)
        {
            guideBoost = 0;
            if (on) MgfFx.Punch(handleRt, .10f, .18f);
        }

        void BeginRevealVisual(bool correct, bool wasFirst, int target, int submitted)
        {
            SetGuideVisible(false);
            if (correct)
            {
                hitStop = .075f;
                latchKick = 1f;
                stitchBurstIndex = Mathf.Clamp(st.solved - 1, 0, stitchTokens.Length - 1);
                stitchBurst = 1f;
                stitchTokens[stitchBurstIndex].gameObject.SetActive(true);
                lambCheer = .9f;
                MgfSfx.Play("correct", .54f);
                MgfFx.Glow(gateO + new Vector3(0, .2f, -.4f), Cyan, 14 + Mathf.Min(18, st.firstAttemptCorrect * 2), .28f);
                feedbackTxt.text = current != null && current.type == GatePromptType.RectangleAndRhombus
                    ? "∠A=90°이고 네 변이 같으므로 정사각형이다"
                    : current != null && (current.type == GatePromptType.BisectAC || current.type == GatePromptType.BisectBD)
                        ? "평행사변형의 대각선은 항상 서로를 이등분한다"
                    : current != null && current.type == GatePromptType.PerpendicularDiagonals
                        ? "네 변이 같은 평행사변형의 대각선은 서로 수직이다"
                    : current != null && current.type == GatePromptType.EqualDiagonals
                        ? "두 대각선의 길이가 같은 평행사변형은 직사각형이다"
                    : current != null && (current.type == GatePromptType.AdjacentB || current.type == GatePromptType.AdjacentD)
                        ? "이웃각의 합 " + current.angleA + "°+" + current.angleB + "°=180°"
                        : "마주 보는 각 ∠A=∠C=" + current.angleA + "°";
                toastImage.color = new Color(Cyan.r * .35f, Cyan.g * .35f, Cyan.b * .35f, .96f);
                toastT = 1.25f;
            }
            else
            {
                wrongThreadT = 1.0f;
                int broken = Mathf.Clamp(st.brokenNeedles - 1, 0, needles.Length - 1);
                if (flow == Flow.Reveal && current != null && current.band > 0 && needles[broken])
                    needles[broken].localRotation = Quaternion.Euler(0, 0, 62);
                MgfSfx.Play("wrong", .46f);
                feedbackTxt.text = WrongReasonText(st.misconceptionId, current, submitted) +
                    (PpittulRules.IsPropertyProblem(current) ? " · 최소 조건은 " + current.answer : " · 필요한 A는 " + current.angleA + "°");
                toastImage.color = new Color(Wine.r, Wine.g, Wine.b, .97f);
                toastT = 1.32f;
            }
        }

        void CompleteRevealVisual(bool correct)
        {
            wrongLooseThread.enabled = false; wrongThreadT = 0;
            if (correct) RefreshStitchTokens();
        }

        void ShowWrongReason(string misconception, GateProblem p, int submitted)
        {
            feedbackTxt.text = WrongReasonText(misconception, p, submitted) + " · 다시 손잡이를 미시오";
            toastImage.color = new Color(Wine.r, Wine.g, Wine.b, .97f); toastT = 2.2f;
        }

        string WrongReasonText(string misconception, GateProblem p, int submitted)
        {
            if (misconception == "adjacent_angles_equal") return "B·D는 A와 같지 않고 합이 180°이다";
            if (misconception == "bisected_means_equal_diagonals") return "이등분과 두 대각선의 길이 같음은 다른 성질이다";
            if (misconception == "adjacent_instead_of_opposite") return "마주 보는 ∠A와 ∠C의 크기는 같다";
            if (misconception == "halved_opposite_angle") return "마주 보는 각은 반으로 줄이지 않고 그대로 같다";
            if (misconception == "used_90_sum_instead_of_180") return "이웃각은 90°가 아니라 180°를 이룬다";
            if (misconception == "only_rectangles_bisect" || misconception == "only_rhombi_bisect" || misconception == "only_squares_bisect")
                return "대각선의 서로 이등분은 모든 평행사변형에서 성립한다";
            if (misconception == "perpendicular_means_equal_diagonals") return "수직인 대각선과 길이가 같은 대각선은 다른 성질이다";
            if (misconception == "square_only_equal_diagonals") return "정사각형뿐 아니라 직사각형의 대각선도 길이가 같다";
            if (misconception == "all_parallelograms_perpendicular") return "모든 평행사변형의 대각선이 수직인 것은 아니다";
            if (misconception == "equal_diagonals_means_perpendicular") return "길이가 같은 대각선이 반드시 수직인 것은 아니다";
            if (misconception == "square_only_perpendicular") return "정사각형뿐 아니라 마름모의 대각선도 서로 수직이다";
            if (misconception == "rectangle_not_rhombus") return "90°만으로는 네 변의 길이가 같다고 할 수 없다";
            if (misconception == "rhombus_not_rectangle") return "네 변이 같아도 네 각이 90°가 아닐 수 있다";
            if (misconception == "parallelogram_is_square") return "정사각형에는 직각과 네 변 같음이 모두 필요하다";
            return "문표가 말한 각과 A 눈금의 관계를 다시 확인하시오";
        }

        void ShowEnd(string reason)
        {
            if (reason == "clear")
            {
                endTitleTxt.text = "관계 아치 완성";
                endHintTxt.text = "11개의 관계 조각을 꿰매 정사각형 매듭을 완성했다";
                RefreshStitchTokens();
            }
            else if (reason == "mastery_failed")
            {
                endTitleTxt.text = "문은 열렸지만 재봉이 흐트러졌다";
                endHintTxt.text = "첫 시도 9/11, 마지막 세 문 중 두 문을 맞혀 다시 공연하시오";
            }
            else if (reason == "timeout")
            {
                endTitleTxt.text = "커튼이 닫혔다";
                endHintTxt.text = "이웃각은 180°에서 빼고 대각은 같은 크기로 다시 맞추시오";
            }
            else
            {
                endTitleTxt.text = "바늘이 모두 부러졌다";
                endHintTxt.text = "같은 문표에서 관계를 바꾸어 다시 잠그시오";
            }
            endStatsTxt.text = "통과 " + st.solved + "/11 · 첫 시도 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal + " · 최고 " + bestScore + "점";
        }

        void ReplayGuide(bool boosted)
        {
            guideBoost = boosted ? 4.6f : 3.2f;
            guideTargetMode = flow == Flow.Practice;
            SetGuideVisible(true);
        }

        void ReplayBellGuide()
        {
            bellPullPx = 0;
            titlePull.text = "가운데 종끈을 아래로";
            MgfFx.Punch(bellKnobRt, .24f, .40f);
        }

        void UpdateGuide()
        {
            if (flow != Flow.Practice && guideBoost <= 0) { SetGuideVisible(false); return; }
            SetGuideVisible(true);
            int from = st.selectedDetent;
            int to = guideTargetMode && current != null ? current.targetDetent : from;
            float phase = Mathf.Repeat(Time.unscaledTime * .72f, 1f);
            float fx = Mathf.Lerp(.10f + .80f * from / 10f, .10f + .80f * to / 10f, phase);
            float fy = Screen.height > Screen.width ? .245f : .205f;
            guideFingerRt.anchorMin = guideFingerRt.anchorMax = new Vector2(fx, fy);
            float pulse = 1f + (guideBoost > 3.5f ? .18f : .10f) * Mathf.Sin(Time.unscaledTime * 5f);
            guideFingerRt.localScale = Vector3.one * pulse;
            guideArrowRt.anchorMin = guideArrowRt.anchorMax = new Vector2(.10f + .80f * to / 10f, fy);
            for (int i = 0; i < guideDots.Length; i++)
            {
                float k = (i + 1f) / (guideDots.Length + 1f);
                float x = Mathf.Lerp(.10f + .80f * from / 10f, .10f + .80f * to / 10f, k);
                guideDots[i].anchorMin = guideDots[i].anchorMax = new Vector2(x, fy);
                guideDots[i].localScale = Vector3.one * (.7f + .35f * Mathf.Sin(Time.unscaledTime * 5f - i * .8f));
            }
        }

        void SetGuideVisible(bool on)
        {
            guideFingerRt.gameObject.SetActive(on);
            guideArrowRt.gameObject.SetActive(on);
            for (int i = 0; i < guideDots.Length; i++) guideDots[i].gameObject.SetActive(on);
        }

        void SpawnTapRipple(Vector2 screen)
        {
            int i = rippleCursor++ % ripples.Length;
            rippleT[i] = 0;
            ripples[i].anchorMin = ripples[i].anchorMax = Vector2.zero;
            ripples[i].anchoredPosition = screen;
            ripples[i].sizeDelta = new Vector2(24, 24);
        }

        void RefuseInput(string reason)
        {
            feedbackTxt.text = reason;
            toastImage.color = new Color(Plum.r, Plum.g, Plum.b, .96f);
            toastT = 1.5f;
            if (handleRt) { handleRt.gameObject.SetActive(false); handleRt.gameObject.SetActive(true); MgfFx.Punch(handleRt, .16f, .28f); }
            MgfSfx.Play("wrong", .20f);
            guideBoost = 2.4f; guideTargetMode = false;
        }

        void PullBell(float pixels)
        {
            bellPullPx = Mathf.Clamp(pixels, 0, 88);
            bellKnobRt.anchoredPosition = new Vector2(0, -22 - bellPullPx);
            titlePull.text = bellPullPx > 28 ? "놓아 공연 시작" : "당겨 열기";
        }

        void ReleaseBell()
        {
            bellKnobRt.anchoredPosition = new Vector2(0, -22);
            bellPullPx = 0;
            MgfSfx.Play("whoosh", .38f);
        }

        bool IsBellZone(Vector2 p)
        {
            float x = p.x / Mathf.Max(1, Screen.width), y = p.y / Mathf.Max(1, Screen.height);
            return x > .30f && x < .70f && y > .14f && y < .58f;
        }

        bool IsRestartZone(Vector2 p)
        {
            float x = p.x / Mathf.Max(1, Screen.width), y = p.y / Mathf.Max(1, Screen.height);
            return x > .30f && x < .70f && y > .07f && y < .36f;
        }

        bool IsHandleZone(Vector2 p)
        {
            float x = p.x / Mathf.Max(1, Screen.width), y = p.y / Mathf.Max(1, Screen.height);
            float hx = Mathf.Lerp(.10f, .90f, st.selectedDetent / 10f);
            float hy = Screen.height > Screen.width ? .245f : .205f;
            float rx = Mathf.Max(.065f, 38f / Mathf.Max(1, Screen.width));
            float ry = Mathf.Max(.060f, 52f / Mathf.Max(1, Screen.height));
            return Mathf.Abs(x - hx) <= rx && Mathf.Abs(y - hy) <= ry;
        }

        int DetentFromScreen(Vector2 p)
        {
            float x = p.x / Mathf.Max(1, Screen.width);
            int detent = Mathf.Clamp(Mathf.RoundToInt((x - .10f) / .80f * 10f), 0, 10);
            return PpittulRules.IsPropertyProblem(current) ? PpittulRules.SnapPropertyDetent(detent) : detent;
        }
    }
}
