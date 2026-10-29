// 제비 특급 — 화면. 밝은 공중 우편망 디오라마(직교 2.5D) + 현재 역의 정면 도면 + 오른쪽 지급 눈금.
//
// 좌표: frame(카메라와 같은 회전) 의 로컬 xy = 화면 평면, z = 화면 안쪽 깊이. 1 논리 px = U 월드 단위.
// 도면은 개략도다 — 원·삼각형 모양은 정답 t 와 무관한 템플릿(배치 난수만 사용)이고, 눈금은 지급 장치에만 있다.
// 제출 전에는 정답 근접 신호(색·흡착·접선 짝 강조)를 하나도 내보내지 않는다. 짝 강조·치수 괄호 설명은 제출 뒤에만.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.JebiTeukgeup
{
    public partial class JebiGame
    {
        const float U = 10f / 390f;
        const string ViewStrings = "제비특급길이를뽑아공중항로를이어라출발여분고리첫제출통과기준남은시간초역점수원밖의한점에서그은두접선내접원접점견습제비가뽑아둔점검고리를잡아끌어뽑아라도착노선끊김다시최고중3원과직선안전그물맞게뽑으면잠금쇠와노선등이차례로켜진다같은이므로꼭짓점전체부분내가뽑은줄짧다길다눈금은누르는곳이아니다올려오른쪽놓아야나간다안에서1~24cm도착하면다음종을누르면반지름의길이PB=?BE=?AB=AD=PA+PB=×−→·이고만큼실패원인통과했다모두잃었다시간이끝났다더이상기준에못미친다점검역그대로고쳐서맞으면틀리면이미맞는줄일수도있다받는다우편차연습이면바깥점끝났습니다0123456789OPABCDEF△:./";

        static readonly Color Sky = MgfLook.Hex("#527ED6");
        static readonly Color Cream = MgfLook.Hex("#FFF4D9");
        static readonly Color Apricot = MgfLook.Hex("#E65F3C");
        static readonly Color Navy = MgfLook.Hex("#102D3A");
        static readonly Color Mist = MgfLook.Hex("#C7D9F4");
        static readonly Color Tension = MgfLook.Hex("#52E0D1");
        static readonly Color Brass = MgfLook.Hex("#D5A743");
        static readonly Color Rose = MgfLook.Hex("#D94732");
        static readonly Color Leaf = MgfLook.Hex("#4E9D91");
        static readonly Color Balsa = MgfLook.Hex("#C88955");

        enum RefuseTarget { Handle, RailPath, Cart, Bell }

        // ── 세계
        Camera cam;
        Transform frame, worldRoot, boardRoot, titleRoot, miniRoot;
        Vector2 logical = new Vector2(390, 844);
        bool land;
        int lastW, lastH; float lastScale;
        Transform bg;
        readonly List<Transform> clouds = new List<Transform>();
        readonly List<float> cloudSpeed = new List<float>();
        readonly List<Transform> farBalloons = new List<Transform>();
        readonly Vector3[] farBase = new Vector3[3];
        Mesh torusMesh, thinTorusMesh, quadMesh, sphereMesh, cylMesh;
        Material lineMat, dashMat, glowMat;
        Transform net;

        // ── 도면
        sealed class Seg
        {
            public LineRenderer lr; public Vector3[] pts = new Vector3[24]; public int n;
        }
        readonly List<Seg> segs = new List<Seg>();
        Seg sGiven, sTarget, sOA, sOB, sRaA, sRaB, sSum1, sSum2, sTriAB, sTriBC, sTriCA, sWhole, sPart, sFail, sCable, sBand1, sBand2, sBand3, sBand4;
        Transform ring, disc, incircle, dotO, platform, hookA, hookB, dotD, dotE, dotF, waveBead;
        Transform stationRotor, foldL, foldR;
        readonly List<Transform> routeLocks = new List<Transform>();
        readonly List<Transform> routeLights = new List<Transform>();
        sealed class Tag
        {
            public TextMeshPro text; public Transform back; public MeshRenderer backR; public Transform root; public float w, h;
            public readonly List<Transform> bars = new List<Transform>();
        }
        readonly List<Tag> tags = new List<Tag>();
        Tag tGiven, tTarget, tRadius, tSum, tWhole, tPart, tFail, tReveal, tPlaque, tPair2;
        readonly List<TextMeshPro> letters = new List<TextMeshPro>();
        // 도면 좌표(템플릿 단위)
        Vector2 dO, dP, dA, dB, dC, dD, dE, dF, dI; float dR, dRin;
        bool triMode;
        Vector2 dStart, dEnd;     // 우편차가 타는 목표 선분(PB 또는 BE)
        Vector2 bbMin, bbMax; float boardScale = 1f;
        Vector3 boardCenter; float boardSlide; float boardSlideFrom, boardSlideTo; float slideT = 1f, slideDur = 0.5f;
        float failRatio; float revealShown;

        // ── 우편차·제비
        Transform cart, cartPivot, wheelL, wheelR, hero, heroBody, wingL, wingR, heroTail;
        float wheelSpin, flap, heroHop;
        Vector3 cartPos; float cartRock; float cartAlong; int cartMode; // 0 대기 1 주행 2 낙하 3 복귀 4 비행
        float cartModeT;
        Vector3 cartFallFrom;
        // ── 주변 제비
        sealed class Bird { public Transform root, wl, wr; public Vector3 center; public float radius, speed, phase, flapK; public bool escort; }
        readonly List<Bird> birds = new List<Bird>();
        float escortT;

        // ── 노선 지도(완성한 역)
        readonly List<Transform> miniNodes = new List<Transform>();
        readonly List<Transform> miniLids = new List<Transform>();
        readonly List<Seg> miniLinks = new List<Seg>();
        Transform miniBead; float clearRunT = -1f;

        // ── 타이틀
        Transform bannerL, bannerR, bell, bellRope, titleRing, titleCart;
        TextMeshPro bannerTextL, bannerTextR, bannerSub, bellTag;
        Seg tRopeL, tRopeR, tTan1, tTan2;
        float revealK;

        // ── UI
        Sprite sRound, sCircle, sRing, sArrow, sTagHole;
        RectTransform uiRoot, hudRoot, railRoot, titleUi, endUi, bridgeUi;
        Image railPost, spool, spoolSpokes, railCable, handleOuter, handleInner, ghostRing, guideFinger, arrowImg, handleHalo;
        readonly List<Image> ticks = new List<Image>();
        readonly List<TextMeshProUGUI> tickNums = new List<TextMeshProUGUI>();
        readonly List<Image> guideDots = new List<Image>();
        TextMeshProUGUI valueTxt, ghostTxt, goalTxt, firstTxt, timeTxt, scoreTxt, stationTxt, slipTxt, toastTxt, titleBadge, titleBest, titleHint;
        Image valueTag, goalBg, slipBg, toastBg, timeBar, timeBarBg, hudTopPlate, miniRoutePlate;
        readonly List<Image> ringIcons = new List<Image>();
        readonly List<Image> pips = new List<Image>();
        TextMeshProUGUI endTitle, endFirst, endReason, endScore, endHint, bridgeTitle, bridgeBody;
        Image endCard, bridgeCard;
        readonly List<Image> ripples = new List<Image>();
        readonly List<float> rippleT = new List<float>();
        int rippleNext;
        float toastT = -1f, arrowT = -1f, handleShakeT = -1f, guideT, guideBoost, valuePunch, firstPunch, ringLossT = -1f;
        Vector2 arrowFrom, arrowTo;
        bool guideOn;
        int shownTime = -1, shownFirst = -1, shownScore = -1;
        float scoreShown, endCount;
        readonly List<Vector3> overlineTmp = new List<Vector3>();
        // 오버라인 막대 (UI 전표용)
        readonly List<Image> slipBars = new List<Image>();

        // ───────────────────────── 구축 ─────────────────────────
        void BuildView()
        {
            MgfLook.Quality(80f);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = MgfLook.Hex("#D9ECFF") * 0.95f;
            RenderSettings.ambientEquatorColor = MgfLook.Hex("#91B7E5") * 0.9f;
            RenderSettings.ambientGroundColor = MgfLook.Hex("#456A9B") * 0.72f;
            var sun = MgfLook.Sun(new Vector3(38, -48, 0), MgfLook.Hex("#FFE7B8"), 1.05f, 0.5f);
            sun.shadows = LightShadows.None;

            frame = new GameObject("SkyFrame").transform;
            frame.rotation = Quaternion.Euler(30f, -34f, 0f);
            cam = MgfLook.Camera(Vector3.zero, Vector3.forward, 30f);
            cam.transform.SetParent(frame, false);
            cam.transform.localPosition = new Vector3(0, 0, -60f);
            cam.transform.localRotation = Quaternion.identity;
            cam.orthographic = true; cam.orthographicSize = 10.8f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Sky;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 400f;

            sphereMesh = JebiMesh.Sphere();
            cylMesh = JebiMesh.Cylinder();
            torusMesh = JebiMesh.Torus(0.055f);
            thinTorusMesh = JebiMesh.Torus(0.035f);
            quadMesh = JebiMesh.Quad();
            lineMat = MgfLook.Alpha(Color.white);
            dashMat = new Material(MgfLook.Shader("MgfAlpha")) { name = "JebiDash" };
            dashMat.SetColor("_Color", Color.white); dashMat.mainTexture = JebiMesh.Dash(); dashMat.mainTextureScale = new Vector2(2.2f, 1f);
            glowMat = MgfLook.Additive(Color.white, MgfLook.SoftDot);

            worldRoot = new GameObject("World").transform; worldRoot.SetParent(frame, false);
            BuildSky();
            BuildMiniMap();
            BuildNet();
            BuildBoard();
            BuildCart();
            BuildBirds();
            BuildTitleWorld();
            BuildUi();
            Relayout(true);
        }

        Transform MeshObj(string name, Transform parent, Mesh mesh, Material mat)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return go.transform;
        }

        Transform Ball(string name, Transform parent, Vector3 pos, Vector3 scale, Color c, float smooth = 0.35f)
        {
            var t = MeshObj(name, parent, sphereMesh, MgfLook.Lit(c, smooth));
            t.localPosition = pos; t.localScale = scale; return t;
        }

        Transform Box(string name, Transform parent, Vector3 pos, Vector3 size, float radius, Color c, float smooth = 0.3f)
        {
            var t = MeshObj(name, parent, MgfLook.RoundedBox(size, radius), MgfLook.Lit(c, smooth));
            t.localPosition = pos; return t;
        }

        Transform Enamel(string name, Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            var t = MeshObj(name, parent, MgfLook.RoundedBox(size, 0.035f), MgfLook.Lit(c, 0.72f, 0.48f));
            t.localPosition = pos; return t;
        }

        Seg NewSeg(Transform parent, Material mat, float width, Color c, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false; lr.sharedMaterial = mat; lr.alignment = LineAlignment.TransformZ;
            lr.widthMultiplier = width; lr.startColor = lr.endColor = c;
            lr.numCapVertices = 3; lr.numCornerVertices = 2;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
            lr.textureMode = LineTextureMode.Tile;
            lr.positionCount = 0;
            var s = new Seg { lr = lr };
            segs.Add(s);
            return s;
        }

        static void SetLine(Seg s, Vector3 a, Vector3 b)
        {
            s.pts[0] = a; s.pts[1] = b; s.n = 2;
            s.lr.positionCount = 2; s.lr.SetPosition(0, a); s.lr.SetPosition(1, b);
        }

        static void SetPoly(Seg s, int n)
        {
            s.n = n; s.lr.positionCount = n;
            for (int i = 0; i < n; i++) s.lr.SetPosition(i, s.pts[i]);
        }

        static void HideSeg(Seg s) { s.n = 0; s.lr.positionCount = 0; }
        static void SegColor(Seg s, Color c) { s.lr.startColor = s.lr.endColor = c; }

        void BuildSky()
        {
            bg = MeshObj("SkyGradient", cam.transform, quadMesh,
                MgfLook.Unlit(Color.white, JebiMesh.Gradient(MgfLook.Hex("#3B6FC7"), MgfLook.Hex("#6FA7E5"), MgfLook.Hex("#C9E6FA"), 0.32f)));
            bg.localPosition = new Vector3(0, 0, 330f);

            var rng = new System.Random(7);
            var cloudMat = MgfLook.Lit(MgfLook.Hex("#FFF8E9"), 0.38f, 0.04f);
            var cloudMat2 = MgfLook.Lit(MgfLook.Hex("#D7E8F7"), 0.32f, 0.04f);
            for (int i = 0; i < 9; i++)
            {
                var m = JebiMesh.Cloud(sphereMesh, rng, 3 + rng.Next(3));
                var c = MeshObj("Cloud" + i, worldRoot, m, i % 3 == 0 ? cloudMat2 : cloudMat);
                float depth = 14f + i * 7f;
                float k = 0.55f + (float)rng.NextDouble() * 0.6f;
                c.localScale = Vector3.one * k * (i < 3 ? 1.25f : 1f);
                c.localPosition = new Vector3(-12f + (float)rng.NextDouble() * 24f, -11f + i * 2.6f + (float)rng.NextDouble(), depth);
                c.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 40f - 20f, 0);
                clouds.Add(c); cloudSpeed.Add(0.12f + (float)rng.NextDouble() * 0.2f);
            }

            // 먼 기구 역(크림 기구 + 매달린 원형 역)
            var balloonMesh = JebiMesh.Lathe(new[]
            {
                new Vector2(0.0001f, -1.0f), new Vector2(0.35f, -0.9f), new Vector2(0.75f, -0.45f), new Vector2(0.95f, 0.1f),
                new Vector2(0.88f, 0.6f), new Vector2(0.55f, 0.95f), new Vector2(0.0001f, 1.08f)
            }, 24);
            for (int i = 0; i < 3; i++)
            {
                var root = new GameObject("FarBalloon" + i).transform; root.SetParent(worldRoot, false);
                var b = MeshObj("Balloon", root, balloonMesh, MgfLook.Lit(i == 1 ? Apricot : Cream, 0.4f));
                b.localPosition = new Vector3(0, 1.6f, 0);
                var band = MeshObj("Band", root, thinTorusMesh, MgfLook.Lit(i == 1 ? Cream : Apricot, 0.3f));
                band.localPosition = new Vector3(0, 1.2f, 0); band.localRotation = Quaternion.Euler(90, 0, 0); band.localScale = Vector3.one * 0.85f;
                var r = MeshObj("Ring", root, torusMesh, MgfLook.Lit(Cream, 0.5f));
                r.localPosition = new Vector3(0, -0.4f, 0); r.localScale = Vector3.one * 0.9f;
                // 멀리서도 깊이를 읽을 수 있는 작은 목재 부유 갑판과 우편 소품.
                var deck = MeshObj("FloatingDeck", root, cylMesh, MgfLook.Lit(Balsa, 0.42f));
                deck.localPosition = new Vector3(0, -1.05f, 0); deck.localScale = new Vector3(1.25f, 0.12f, 1.25f);
                deck.localRotation = Quaternion.Euler(90f, 0, 0);
                for (int p = 0; p < 4; p++)
                {
                    float a = p * Mathf.PI * 0.5f + 0.4f;
                    Box("RailPost" + p, root, new Vector3(Mathf.Cos(a) * 0.82f, -0.82f, Mathf.Sin(a) * 0.82f), new Vector3(0.1f, 0.65f, 0.1f), 0.03f, Brass);
                }
                Box("Mailbox", root, new Vector3(0.35f, -0.73f, 0), new Vector3(0.4f, 0.34f, 0.32f), 0.06f, Navy);
                var flag = Enamel("RouteFlag", root, new Vector3(-0.58f, -0.44f, 0), new Vector3(0.65f, 0.28f, 0.06f), i == 1 ? Cream : Apricot);
                flag.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -8f : 8f);
                float depth = 30f + i * 18f;
                root.localPosition = new Vector3(i == 0 ? -7.5f : i == 1 ? 8f : 2f, 6f - i * 2f, depth);
                root.localScale = Vector3.one * (1.1f - i * 0.2f);
                farBalloons.Add(root);
            }
        }

        void BuildNet()
        {
            net = MeshObj("SafetyNet", worldRoot, JebiMesh.Net(8f, 0.9f, 10, 3, 0.35f, 0.045f), MgfLook.Alpha(new Color(1f, 0.96f, 0.86f, 0.85f)));
            // 그물 양끝 기둥
            for (int s = -1; s <= 1; s += 2)
            {
                var post = MeshObj("NetPost", net, cylMesh, MgfLook.Lit(Navy, 0.3f));
                post.localPosition = new Vector3(s * 4.05f, 0.25f, 0.3f); post.localScale = new Vector3(0.1f, 0.45f, 0.1f);
                var knob = Ball("NetKnob", net, new Vector3(s * 4.05f, 0.72f, 0.3f), Vector3.one * 0.2f, Apricot);
            }
        }

        void BuildMiniMap()
        {
            miniRoot = new GameObject("RouteMap").transform; miniRoot.SetParent(worldRoot, false);
            var ringMat = MgfLook.Lit(Cream, 0.5f);
            var boxMat = Apricot;
            for (int i = 0; i <= JebiRules.Stations; i++)
            {
                var node = new GameObject("Node" + i).transform; node.SetParent(miniRoot, false);
                var r = MeshObj("Ring", node, torusMesh, ringMat); r.localScale = Vector3.one * 0.42f;
                var box = Box("Box", node, new Vector3(0, -0.62f, 0), new Vector3(0.34f, 0.28f, 0.26f), 0.05f, i == 0 ? Navy : boxMat);
                var lid = Box("Lid", box, new Vector3(0, 0.16f, 0), new Vector3(0.38f, 0.07f, 0.3f), 0.03f, Navy);
                miniNodes.Add(node); miniLids.Add(lid);
                if (i > 0) miniLinks.Add(NewSeg(miniRoot, dashMat, 0.07f, new Color(1, 1, 1, 0.75f), "Link" + i));
            }
            miniBead = MeshObj("Bead", miniRoot, quadMesh, glowMat);
            miniBead.localScale = Vector3.one * 0.9f; miniBead.gameObject.SetActive(false);
        }

        void BuildBoard()
        {
            boardRoot = new GameObject("Board").transform; boardRoot.SetParent(frame, false);
            // 도면 뒤쪽은 빈 작업면이 아니라 실제 공중역의 목재 갑판과 현수 프레임이다.
            var stationDeck = MeshObj("StationDeck", boardRoot, cylMesh, MgfLook.Lit(Balsa, 0.42f));
            stationDeck.localPosition = new Vector3(0, 0, 0.78f); stationDeck.localScale = new Vector3(4.5f, 0.16f, 4.5f);
            stationDeck.localRotation = Quaternion.Euler(90f, 0, 0);
            var deckRim = MeshObj("StationDeckRim", boardRoot, torusMesh, MgfLook.Lit(Brass, 0.72f, 0.4f));
            deckRim.localPosition = new Vector3(0, 0, 0.55f); deckRim.localScale = Vector3.one * 4.25f;
            for (int p = 0; p < 6; p++)
            {
                float a = p * Mathf.PI / 3f;
                Vector3 pos = new Vector3(Mathf.Cos(a) * 3.75f, Mathf.Sin(a) * 3.75f, 0.35f);
                Box("SuspensionPost" + p, boardRoot, pos, new Vector3(0.18f, 0.78f, 0.18f), 0.04f, p % 2 == 0 ? Cream : Apricot);
            }
            disc = MeshObj("Disc", boardRoot, quadMesh, MgfLook.Alpha(new Color(1f, 0.97f, 0.88f, 0.33f), MgfLook.SoftDot));
            ring = MeshObj("StationRing", boardRoot, torusMesh, MgfLook.Lit(Cream, 0.55f));
            incircle = MeshObj("Incircle", boardRoot, torusMesh, MgfLook.Lit(Cream, 0.55f));
            dotO = Ball("O", boardRoot, Vector3.zero, Vector3.one * 0.16f, Navy);
            platform = new GameObject("Platform").transform; platform.SetParent(boardRoot, false);
            var deck = MeshObj("Deck", platform, cylMesh, MgfLook.Lit(Navy, 0.75f, 0.5f));
            deck.localScale = new Vector3(0.7f, 0.06f, 0.7f); deck.localRotation = Quaternion.Euler(-60, 0, 0);
            Ball("Post", platform, new Vector3(0, 0, 0.1f), Vector3.one * 0.22f, Brass, 0.78f);
            hookA = Ball("HookA", boardRoot, Vector3.zero, Vector3.one * 0.2f, Brass, 0.7f);
            hookB = Ball("HookB", boardRoot, Vector3.zero, Vector3.one * 0.2f, Brass, 0.7f);
            dotD = Ball("D", boardRoot, Vector3.zero, Vector3.one * 0.15f, Navy);
            dotE = Ball("E", boardRoot, Vector3.zero, Vector3.one * 0.15f, Navy);
            dotF = Ball("F", boardRoot, Vector3.zero, Vector3.one * 0.15f, Navy);
            waveBead = MeshObj("TensionWave", boardRoot, quadMesh, glowMat);
            waveBead.localScale = Vector3.one * 0.9f; waveBead.gameObject.SetActive(false);

            // 정답 사건의 주인공: 장력파가 분기 잠금쇠를 체결하고 노선등을 순서대로 켠다.
            stationRotor = new GameObject("AerialRouteJunction").transform; stationRotor.SetParent(boardRoot, false);
            var rotorOuter = MeshObj("RotorOuter", stationRotor, torusMesh, MgfLook.Lit(Navy, 0.78f, 0.55f));
            rotorOuter.localScale = Vector3.one * 1.28f;
            var rotorInner = MeshObj("RotorInner", stationRotor, thinTorusMesh, MgfLook.Lit(Brass, 0.82f, 0.65f));
            rotorInner.localScale = Vector3.one * 0.96f; rotorInner.localPosition = new Vector3(0, 0, -0.04f);
            foldL = Enamel("FoldWingL", stationRotor, new Vector3(-1.55f, 0, 0), new Vector3(1.7f, 0.5f, 0.14f), Navy);
            foldR = Enamel("FoldWingR", stationRotor, new Vector3(1.55f, 0, 0), new Vector3(1.7f, 0.5f, 0.14f), Navy);
            Enamel("FoldStripeL", foldL, new Vector3(0, 0, -0.09f), new Vector3(1.25f, 0.08f, 0.05f), Brass);
            Enamel("FoldStripeR", foldR, new Vector3(0, 0, -0.09f), new Vector3(1.25f, 0.08f, 0.05f), Brass);
            for (int i = 0; i < 3; i++)
            {
                float a = (-46f + i * 46f) * Mathf.Deg2Rad;
                var branch = new GameObject("RouteBranch" + i).transform; branch.SetParent(stationRotor, false);
                branch.localPosition = new Vector3(Mathf.Sin(a) * 1.46f, -Mathf.Cos(a) * 1.46f, -0.14f);
                branch.localRotation = Quaternion.Euler(0, 0, -46f + i * 46f);
                Enamel("ClampBase", branch, Vector3.zero, new Vector3(0.68f, 0.32f, 0.14f), Navy);
                var latch = Enamel("Latch", branch, new Vector3(0, 0.26f, -0.05f), new Vector3(0.45f, 0.16f, 0.12f), Brass);
                var light = MeshObj("RouteLight", branch, quadMesh, MgfLook.Additive(Tension, MgfLook.SoftDot));
                light.localPosition = new Vector3(0, 0.04f, -0.18f); light.localScale = Vector3.one * 0.8f; light.gameObject.SetActive(false);
                routeLocks.Add(latch); routeLights.Add(light);
            }

            Color ink = Navy;
            sBand1 = NewSeg(boardRoot, lineMat, 0.34f, new Color(1, 1, 1, 0.6f), "Band1");
            sBand2 = NewSeg(boardRoot, lineMat, 0.34f, new Color(1, 1, 1, 0.6f), "Band2");
            sBand3 = NewSeg(boardRoot, lineMat, 0.34f, new Color(1, 1, 1, 0.6f), "Band3");
            sBand4 = NewSeg(boardRoot, lineMat, 0.34f, new Color(1, 1, 1, 0.6f), "Band4");
            sOA = NewSeg(boardRoot, lineMat, 0.05f, ink, "OA");
            sOB = NewSeg(boardRoot, lineMat, 0.05f, ink, "OB");
            sRaA = NewSeg(boardRoot, lineMat, 0.04f, ink, "RightA");
            sRaB = NewSeg(boardRoot, lineMat, 0.04f, ink, "RightB");
            sTriAB = NewSeg(boardRoot, lineMat, 0.085f, ink, "AB");
            sTriBC = NewSeg(boardRoot, lineMat, 0.085f, ink, "BC");
            sTriCA = NewSeg(boardRoot, lineMat, 0.085f, ink, "CA");
            sGiven = NewSeg(boardRoot, lineMat, 0.1f, ink, "Given");
            sTarget = NewSeg(boardRoot, dashMat, 0.11f, Apricot, "Target");
            sSum1 = NewSeg(boardRoot, lineMat, 0.045f, MgfLook.Hex("#B5652E"), "Sum1");
            sSum2 = NewSeg(boardRoot, lineMat, 0.045f, MgfLook.Hex("#B5652E"), "Sum2");
            sWhole = NewSeg(boardRoot, lineMat, 0.045f, ink, "Whole");
            sPart = NewSeg(boardRoot, lineMat, 0.05f, MgfLook.Hex("#B5652E"), "Part");
            sFail = NewSeg(boardRoot, lineMat, 0.08f, new Color(0.92f, 0.45f, 0.32f, 0.9f), "FailTrail");
            sCable = NewSeg(boardRoot, lineMat, 0.1f, ink, "Cable");

            tGiven = NewTag("Given", Cream, Navy, 15f);
            tTarget = NewTag("Target", Apricot, Navy, 17f);
            tRadius = NewTag("Radius", Mist, Navy, 13f);
            tSum = NewTag("Sum", Cream, MgfLook.Hex("#8A4A1E"), 15f);
            tWhole = NewTag("Whole", Cream, Navy, 15f);
            tPart = NewTag("Part", Cream, MgfLook.Hex("#8A4A1E"), 15f);
            tFail = NewTag("Fail", MgfLook.Hex("#FBE1D6"), MgfLook.Hex("#9C3D25"), 13f);
            tReveal = NewTag("Reveal", Navy, Cream, 15f);
            tPair2 = NewTag("Pair2", Navy, Cream, 13f);
            tPlaque = NewTag("Plaque", Navy, Cream, 13f);
            for (int i = 0; i < 7; i++)
            {
                var l = MgfText.World("", Vector3.zero, FS(17f), Navy, boardRoot);
                l.fontStyle = FontStyles.Bold;
                l.outlineWidth = 0.22f; l.outlineColor = Cream;
                letters.Add(l);
            }
        }

        static float FS(float px) => px * U * 10f;

        Tag NewTag(string name, Color back, Color ink, float px)
        {
            var tag = new Tag();
            tag.root = new GameObject("Tag" + name).transform; tag.root.SetParent(boardRoot, false);
            tag.back = MeshObj("Back", tag.root, MgfLook.RoundedBox(new Vector3(1f, 1f, 0.1f), 0.12f), MgfLook.Lit(back, 0.25f));
            tag.backR = tag.back.GetComponent<MeshRenderer>();
            tag.text = MgfText.World("", new Vector3(0, 0, -0.12f), FS(px), ink, tag.root);
            tag.text.fontStyle = FontStyles.Bold;
            tag.root.gameObject.SetActive(false);
            tags.Add(tag);
            return tag;
        }

        void SetTag(Tag tag, string text, Vector3 pos, float maxW = 0f)
        {
            tag.root.gameObject.SetActive(true);
            tag.root.localPosition = pos;
            tag.text.text = text;
            Vector2 pref;
            if (maxW > 0.5f)
            {
                tag.text.textWrappingMode = TextWrappingModes.Normal;
                pref = tag.text.GetPreferredValues(text, maxW - 0.36f, 0f);
                pref.x = Mathf.Min(pref.x, maxW - 0.36f);
            }
            else
            {
                tag.text.textWrappingMode = TextWrappingModes.NoWrap;
                pref = tag.text.GetPreferredValues(text);
            }
            float w = Mathf.Ceil((pref.x + 0.36f) * 10f) / 10f, h = Mathf.Ceil((pref.y + 0.16f) * 10f) / 10f;
            tag.w = w; tag.h = h;
            tag.back.GetComponent<MeshFilter>().sharedMesh = MgfLook.RoundedBox(new Vector3(w, h, 0.12f), Mathf.Min(0.16f, h * 0.45f));
            tag.text.rectTransform.sizeDelta = new Vector2(w, h);
            ApplyOverlines(tag);
        }

        /// <summary>선분 이름(대문자 두 글자)에 윗줄을 긋는다 — 교과서 선분 기호.</summary>
        void ApplyOverlines(Tag tag)
        {
            for (int i = 0; i < tag.bars.Count; i++) tag.bars[i].gameObject.SetActive(false);
            var t = tag.text;
            t.ForceMeshUpdate();
            string s = t.text;
            var info = t.textInfo;
            int used = 0;
            for (int i = 0; i + 1 < s.Length && i + 1 < info.characterCount; i++)
            {
                if (!IsSegPair(s, i)) continue;
                var c0 = info.characterInfo[i]; var c1 = info.characterInfo[i + 1];
                float x0 = c0.topLeft.x, x1 = c1.topRight.x, y = Mathf.Max(c0.topLeft.y, c1.topRight.y) + t.fontSize * 0.012f;
                Transform bar;
                if (used < tag.bars.Count) bar = tag.bars[used];
                else
                {
                    bar = MeshObj("Overline", t.transform, quadMesh, MgfLook.Unlit(Color.white));
                    tag.bars.Add(bar);
                }
                bar.GetComponent<MeshRenderer>().sharedMaterial = MgfLook.Unlit(t.color);
                bar.gameObject.SetActive(true);
                bar.localPosition = new Vector3((x0 + x1) * 0.5f, y, -0.01f);
                bar.localScale = new Vector3(x1 - x0, t.fontSize * 0.012f, 1f);
                used++;
                i++;
            }
        }

        static bool IsSegPair(string s, int i)
        {
            bool Up(char c) => c >= 'A' && c <= 'Z';
            if (!Up(s[i]) || !Up(s[i + 1])) return false;
            if (i > 0 && (Up(s[i - 1]) || s[i - 1] == '△')) return false;
            if (i + 2 < s.Length && Up(s[i + 2])) return false;
            return true;
        }

        void BuildCart()
        {
            cart = new GameObject("MailCart").transform; cart.SetParent(frame, false);
            cartPivot = new GameObject("Pivot").transform; cartPivot.SetParent(cart, false);
            var wheelMat = MgfLook.Lit(Navy, 0.45f, 0.2f);
            wheelL = MeshObj("WheelL", cartPivot, cylMesh, wheelMat);
            wheelL.localPosition = new Vector3(0, 0.15f, 0.34f); wheelL.localScale = new Vector3(0.3f, 0.05f, 0.3f);
            wheelR = MeshObj("WheelR", cartPivot, cylMesh, wheelMat);
            wheelR.localPosition = new Vector3(0, 0.15f, -0.34f); wheelR.localScale = new Vector3(0.3f, 0.05f, 0.3f);
            var hub1 = Ball("Hub", wheelL, new Vector3(0, 1.2f, 0.3f), new Vector3(0.35f, 0.5f, 0.35f), Brass, 0.7f);
            var hub2 = Ball("Hub", wheelR, new Vector3(0, 1.2f, 0.3f), new Vector3(0.35f, 0.5f, 0.35f), Brass, 0.7f);
            Box("Chassis", cartPivot, new Vector3(0, 0.32f, 0), new Vector3(0.32f, 0.1f, 1.0f), 0.04f, Navy);
            Enamel("Body", cartPivot, new Vector3(0, 0.62f, 0), new Vector3(0.66f, 0.5f, 1.02f), Navy);
            Enamel("Stripe", cartPivot, new Vector3(0, 0.6f, 0), new Vector3(0.69f, 0.09f, 1.05f), Apricot);
            Enamel("Roof", cartPivot, new Vector3(0, 0.92f, 0), new Vector3(0.76f, 0.1f, 1.14f), Brass);
            var lamp = MeshObj("Lamp", cartPivot, sphereMesh, MgfLook.Lit(MgfLook.Hex("#FFE7A0"), 0.6f, 0f, MgfLook.Hex("#FFD36A") * 0.9f));
            lamp.localPosition = new Vector3(0, 0.66f, 0.53f); lamp.localScale = Vector3.one * 0.16f;
            Box("Letter", cartPivot, new Vector3(-0.34f, 0.7f, -0.12f), new Vector3(0.04f, 0.2f, 0.3f), 0.02f, Cream);

            hero = BuildSwallow(cartPivot, true);
            hero.localPosition = new Vector3(0, 0.97f, -0.05f);
            hero.localScale = Vector3.one * 0.95f;
            cart.localScale = Vector3.one * 1.05f;
        }

        Transform BuildSwallow(Transform parent, bool detailed)
        {
            var root = new GameObject(detailed ? "HeroSwallow" : "Swallow").transform; root.SetParent(parent, false);
            var body = new GameObject("Body").transform; body.SetParent(root, false);
            Ball("Back", body, new Vector3(0, 0.26f, 0), new Vector3(0.5f, 0.44f, 0.74f), Navy, 0.45f);
            Ball("Belly", body, new Vector3(0, 0.2f, 0.07f), new Vector3(0.42f, 0.34f, 0.58f), Cream, 0.3f);
            Ball("Head", body, new Vector3(0, 0.52f, 0.3f), Vector3.one * 0.42f, Navy, 0.45f);
            if (detailed)
            {
                Ball("Throat", body, new Vector3(0, 0.42f, 0.46f), Vector3.one * 0.22f, Rose, 0.3f);
                var beak = MeshObj("Beak", body, JebiMesh.Cone(0.075f, 0.2f), MgfLook.Lit(MgfLook.Hex("#F08A3C"), 0.4f));
                beak.localPosition = new Vector3(0, 0.5f, 0.48f); beak.localRotation = Quaternion.Euler(90, 0, 0);
                Ball("EyeL", body, new Vector3(-0.15f, 0.58f, 0.42f), Vector3.one * 0.075f, MgfLook.Hex("#0E1622"), 0.9f);
                Ball("EyeR", body, new Vector3(0.15f, 0.58f, 0.42f), Vector3.one * 0.075f, MgfLook.Hex("#0E1622"), 0.9f);
                Box("Bag", body, new Vector3(0.27f, 0.17f, -0.02f), new Vector3(0.12f, 0.2f, 0.26f), 0.04f, MgfLook.Hex("#E89A5E"));
            }
            else
            {
                var beak = MeshObj("Beak", body, JebiMesh.Cone(0.07f, 0.18f), MgfLook.Lit(MgfLook.Hex("#F08A3C"), 0.4f));
                beak.localPosition = new Vector3(0, 0.5f, 0.48f); beak.localRotation = Quaternion.Euler(90, 0, 0);
            }
            var wl = new GameObject("WingL").transform; wl.SetParent(body, false); wl.localPosition = new Vector3(-0.22f, 0.34f, 0.04f);
            Ball("Feather", wl, new Vector3(-0.06f, -0.02f, -0.14f), new Vector3(0.09f, 0.3f, 0.66f), Navy, 0.45f).localRotation = Quaternion.Euler(0, 0, 20);
            var wr = new GameObject("WingR").transform; wr.SetParent(body, false); wr.localPosition = new Vector3(0.22f, 0.34f, 0.04f);
            Ball("Feather", wr, new Vector3(0.06f, -0.02f, -0.14f), new Vector3(0.09f, 0.3f, 0.66f), Navy, 0.45f).localRotation = Quaternion.Euler(0, 0, -20);
            var tail = MeshObj("Tail", body, JebiMesh.ForkTail(), MgfLook.Lit(Navy, 0.4f));
            tail.localPosition = new Vector3(0, 0.3f, -0.3f); tail.localRotation = Quaternion.Euler(-12, 0, 0);
            if (detailed) { heroBody = body; wingL = wl; wingR = wr; heroTail = tail; }
            return root;
        }

        void BuildBirds()
        {
            var rng = new System.Random(11);
            for (int i = 0; i < 5; i++)
            {
                var b = new Bird();
                b.root = BuildSwallow(worldRoot, false);
                b.wl = b.root.Find("Body/WingL"); b.wr = b.root.Find("Body/WingR");
                b.radius = 1.6f + (float)rng.NextDouble() * 2.5f;
                b.speed = (0.45f + (float)rng.NextDouble() * 0.4f) * (i % 2 == 0 ? 1 : -1);
                b.phase = (float)rng.NextDouble() * 6.28f;
                b.flapK = 9f + (float)rng.NextDouble() * 5f;
                b.root.localScale = Vector3.one * (0.55f + (float)rng.NextDouble() * 0.25f);
                birds.Add(b);
            }
        }

        void BuildTitleWorld()
        {
            titleRoot = new GameObject("TitleWorld").transform; titleRoot.SetParent(frame, false);
            titleRing = new GameObject("TitleStation").transform; titleRing.SetParent(titleRoot, false);
            var r = MeshObj("Ring", titleRing, torusMesh, MgfLook.Lit(Cream, 0.55f)); r.localScale = Vector3.one * 2.3f;
            var d = MeshObj("Disc", titleRing, quadMesh, MgfLook.Alpha(new Color(1f, 0.97f, 0.88f, 0.35f), MgfLook.SoftDot)); d.localScale = Vector3.one * 5.2f; d.localPosition = new Vector3(0, 0, 0.2f);
            var balloonMesh = JebiMesh.Lathe(new[]
            {
                new Vector2(0.0001f, -1.0f), new Vector2(0.35f, -0.9f), new Vector2(0.75f, -0.45f), new Vector2(0.95f, 0.1f),
                new Vector2(0.88f, 0.6f), new Vector2(0.55f, 0.95f), new Vector2(0.0001f, 1.08f)
            }, 28);
            var bal = MeshObj("Balloon", titleRing, balloonMesh, MgfLook.Lit(Apricot, 0.45f));
            bal.localPosition = new Vector3(0, 4.1f, 0.4f); bal.localScale = Vector3.one * 1.25f;
            var band = MeshObj("BalloonBand", bal, thinTorusMesh, MgfLook.Lit(Cream, 0.4f)); band.localRotation = Quaternion.Euler(90, 0, 0); band.localScale = Vector3.one * 0.92f; band.localPosition = new Vector3(0, 0.1f, 0);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + 0.4f;
                Box("Mailbox" + i, titleRing, new Vector3(Mathf.Cos(a) * 2.3f, Mathf.Sin(a) * 2.3f, -0.15f), new Vector3(0.38f, 0.3f, 0.3f), 0.06f, i % 2 == 0 ? Navy : Apricot);
            }
            tTan1 = NewSeg(titleRoot, lineMat, 0.1f, Navy, "TitleTan1");
            tTan2 = NewSeg(titleRoot, lineMat, 0.1f, Navy, "TitleTan2");
            tRopeL = NewSeg(titleRoot, lineMat, 0.05f, Navy, "RopeL");
            tRopeR = NewSeg(titleRoot, lineMat, 0.05f, Navy, "RopeR");

            // 현수막 두 자락(가운데에서 갈라져 열린다)
            bannerL = Enamel("BannerL", titleRoot, Vector3.zero, new Vector3(4.6f, 2.3f, 0.14f), Navy);
            bannerR = Enamel("BannerR", titleRoot, Vector3.zero, new Vector3(4.6f, 2.3f, 0.14f), Navy);
            Enamel("TrimL", bannerL, new Vector3(0, -1.05f, -0.02f), new Vector3(4.6f, 0.18f, 0.16f), Brass);
            Enamel("TrimR", bannerR, new Vector3(0, -1.05f, -0.02f), new Vector3(4.6f, 0.18f, 0.16f), Brass);
            bannerTextL = MgfText.World("제비", new Vector3(-0.25f, 0.28f, -0.12f), FS(60f), Cream, bannerL);
            bannerTextR = MgfText.World("특급", new Vector3(0.25f, 0.28f, -0.12f), FS(60f), Cream, bannerR);
            foreach (var bt in new[] { bannerTextL, bannerTextR })
            {
                bt.fontStyle = FontStyles.Bold; bt.outlineWidth = 0.18f; bt.outlineColor = Apricot;
                bt.rectTransform.sizeDelta = new Vector2(4.4f, 2f);
            }
            bannerTextL.alignment = TextAlignmentOptions.Right; bannerTextR.alignment = TextAlignmentOptions.Left;
            bannerSub = MgfText.World("접선 길이로 공중 항로를 이어라", new Vector3(2.3f, -0.78f, -0.12f), FS(15f), Brass, bannerL);
            bannerSub.rectTransform.sizeDelta = new Vector2(8f, 1f);

            // 우편 종
            bell = new GameObject("Bell").transform; bell.SetParent(titleRoot, false);
            var bellMesh = JebiMesh.Lathe(new[]
            {
                new Vector2(0.0001f, 0.9f), new Vector2(0.28f, 0.86f), new Vector2(0.42f, 0.62f), new Vector2(0.48f, 0.25f),
                new Vector2(0.62f, -0.05f), new Vector2(0.78f, -0.2f), new Vector2(0.74f, -0.26f), new Vector2(0.0001f, -0.22f)
            }, 30);
            var bm = MeshObj("BellBody", bell, bellMesh, MgfLook.Lit(Brass, 0.75f, 0.55f));
            bm.localPosition = new Vector3(0, -0.9f, 0); bm.localScale = Vector3.one * 1.25f;
            Ball("Clapper", bell, new Vector3(0, -1.32f, 0), Vector3.one * 0.26f, Navy);
            Ball("Cap", bell, new Vector3(0, 0.25f, 0), Vector3.one * 0.3f, Navy);
            var tagBack = Box("BellTag", bell, new Vector3(0, -2.05f, -0.2f), new Vector3(1.9f, 0.85f, 0.12f), 0.2f, Navy);
            bellTag = MgfText.World("출발", new Vector3(0, -2.05f, -0.34f), FS(26f), Cream, bell);
            bellTag.fontStyle = FontStyles.Bold;
            bellRope = new GameObject("BellRopeAnchor").transform; bellRope.SetParent(titleRoot, false);

            // 타이틀 우편차(접선 줄 위)
            titleCart = new GameObject("TitleCartAnchor").transform; titleCart.SetParent(titleRoot, false);
        }

        // ───────────────────────── UI ─────────────────────────
        void BuildUi()
        {
            sRound = JebiMesh.MakeSprite(0, 64, 16f);
            sCircle = JebiMesh.MakeSprite(1, 96);
            sRing = JebiMesh.MakeSprite(2, 128);
            sArrow = JebiMesh.MakeSprite(3, 64);
            sTagHole = JebiMesh.MakeSprite(4, 64, 14f);
            var canvas = MgfText.Canvas;
            uiRoot = NewRt("UiRoot", (RectTransform)canvas.transform);
            uiRoot.anchorMin = Vector2.zero; uiRoot.anchorMax = Vector2.one; uiRoot.offsetMin = uiRoot.offsetMax = Vector2.zero;

            hudRoot = Full("Hud", uiRoot);
            railRoot = Full("Rail", uiRoot);
            titleUi = Full("TitleUi", uiRoot);
            bridgeUi = Full("BridgeUi", uiRoot);
            endUi = Full("EndUi", uiRoot);

            // ── HUD
            hudTopPlate = Img(hudRoot, sRound, new Color(Navy.r, Navy.g, Navy.b, 0.95f), new Vector2(380, 46));
            hudTopPlate.type = Image.Type.Sliced;
            miniRoutePlate = Img(hudRoot, sRound, new Color(Navy.r, Navy.g, Navy.b, 0.78f), new Vector2(280, 100));
            miniRoutePlate.type = Image.Type.Sliced;
            for (int i = 0; i < 3; i++)
            {
                var ic = Img(hudRoot, sRing, Apricot, new Vector2(30, 30));
                var inner = Img(ic.rectTransform, sRing, Navy, new Vector2(38, 38)); inner.transform.SetAsFirstSibling();
                inner.rectTransform.anchoredPosition = Vector2.zero; inner.rectTransform.anchorMin = inner.rectTransform.anchorMax = new Vector2(.5f, .5f);
                ringIcons.Add(ic);
            }
            for (int i = 0; i < JebiRules.Stations; i++) pips.Add(Img(hudRoot, sRound, new Color(1, 1, 1, 0.45f), new Vector2(18, 14)));
            stationTxt = Txt(hudRoot, "", 12, Cream, 80);
            timeBarBg = Img(hudRoot, sRound, new Color(0.12f, 0.22f, 0.36f, 0.35f), new Vector2(110, 10));
            timeBar = Img(hudRoot, sRound, Cream, new Vector2(110, 10));
            timeBar.rectTransform.pivot = new Vector2(0, 0.5f);
            timeTxt = Txt(hudRoot, "", 14, Cream, 120); timeTxt.outlineWidth = 0.2f; timeTxt.outlineColor = Navy;
            goalBg = Img(hudRoot, sTagHole, Cream, new Vector2(300, 34)); goalBg.type = Image.Type.Sliced;
            goalTxt = Txt(goalBg.rectTransform, "", 15, Navy, 280); Center(goalTxt.rectTransform);
            firstTxt = Txt(hudRoot, "", 14, Cream, 200); firstTxt.outlineWidth = 0.2f; firstTxt.outlineColor = Navy;
            scoreTxt = Txt(hudRoot, "", 12, Cream, 120); scoreTxt.outlineWidth = 0.2f; scoreTxt.outlineColor = Navy;

            slipBg = Img(hudRoot, sRound, Cream, new Vector2(300, 100)); slipBg.type = Image.Type.Sliced;
            var stripe = Img(slipBg.rectTransform, sRound, Apricot, new Vector2(8, 80)); stripe.type = Image.Type.Sliced;
            stripe.rectTransform.anchorMin = stripe.rectTransform.anchorMax = new Vector2(0, 0.5f); stripe.rectTransform.anchoredPosition = new Vector2(10, 0);
            stripe.rectTransform.anchorMin = new Vector2(0, 0.12f); stripe.rectTransform.anchorMax = new Vector2(0, 0.88f); stripe.rectTransform.sizeDelta = new Vector2(7, 0);
            slipTxt = Txt(slipBg.rectTransform, "", 13, Navy, 270);
            slipTxt.alignment = TextAlignmentOptions.Left; slipTxt.textWrappingMode = TextWrappingModes.Normal;
            slipTxt.enableAutoSizing = true; slipTxt.fontSizeMin = 10.5f; slipTxt.fontSizeMax = 14f;
            slipTxt.rectTransform.anchorMin = Vector2.zero; slipTxt.rectTransform.anchorMax = Vector2.one;
            slipTxt.rectTransform.offsetMin = new Vector2(22, 6); slipTxt.rectTransform.offsetMax = new Vector2(-10, -6);
            slipTxt.lineSpacing = -8f;

            toastBg = Img(hudRoot, sRound, Navy, new Vector2(300, 38)); toastBg.type = Image.Type.Sliced;
            toastTxt = Txt(toastBg.rectTransform, "", 14, Cream, 290); Center(toastTxt.rectTransform);
            toastBg.gameObject.SetActive(false);

            // ── 지급 눈금
            railPost = Img(railRoot, sRound, Cream, new Vector2(30, 400)); railPost.type = Image.Type.Sliced;
            railCable = Img(railRoot, sRound, Navy, new Vector2(4, 100)); railCable.rectTransform.pivot = new Vector2(0.5f, 0f);
            for (int i = 1; i <= JebiRules.MaxLen; i++)
            {
                bool major = i % 5 == 0;
                ticks.Add(Img(railRoot, sRound, Navy, new Vector2(major ? 18 : 11, major ? 3.2f : 2.2f)));
                var n = Txt(railRoot, i.ToString(), major ? 14f : 12f, Navy, 30);
                tickNums.Add(n);
            }
            spool = Img(railRoot, sCircle, Navy, new Vector2(46, 46));
            var spoolRing = Img(spool.rectTransform, sRing, Apricot, new Vector2(46, 46)); Center(spoolRing.rectTransform);
            spoolSpokes = Img(spool.rectTransform, sRound, Cream, new Vector2(30, 6)); Center(spoolSpokes.rectTransform);
            var spoke2 = Img(spoolSpokes.rectTransform, sRound, Cream, new Vector2(6, 30)); Center(spoke2.rectTransform);
            ghostRing = Img(railRoot, sRing, new Color(1, 1, 1, 0.75f), new Vector2(54, 54));
            ghostTxt = Txt(ghostRing.rectTransform, "7 cm", 13, Navy, 70); Center(ghostTxt.rectTransform); ghostTxt.rectTransform.anchoredPosition = new Vector2(-60, 0);
            handleHalo = Img(railRoot, sCircle, new Color(1f, 0.95f, 0.8f, 0.35f), new Vector2(74, 74));
            handleOuter = Img(railRoot, sRing, Navy, new Vector2(62, 62));
            handleInner = Img(handleOuter.rectTransform, sRing, Apricot, new Vector2(52, 52)); Center(handleInner.rectTransform);
            var grip = Img(handleOuter.rectTransform, sCircle, Navy, new Vector2(12, 12)); Center(grip.rectTransform);
            valueTag = Img(railRoot, sTagHole, Navy, new Vector2(84, 36)); valueTag.type = Image.Type.Sliced;
            valueTxt = Txt(valueTag.rectTransform, "", 19, Cream, 84); Center(valueTxt.rectTransform);
            for (int i = 0; i < 7; i++) guideDots.Add(Img(railRoot, sCircle, Cream, new Vector2(8, 8)));
            guideFinger = Img(railRoot, sCircle, Cream, new Vector2(30, 30));
            var fingerRing = Img(guideFinger.rectTransform, sRing, Navy, new Vector2(36, 36)); Center(fingerRing.rectTransform);
            arrowImg = Img(hudRoot, sArrow, Apricot, new Vector2(30, 30)); arrowImg.gameObject.SetActive(false);

            // ── 리플
            for (int i = 0; i < 5; i++)
            {
                var rp = Img(uiRoot, sRing, new Color(1, 1, 1, 0), new Vector2(60, 60));
                rp.raycastTarget = false; ripples.Add(rp); rippleT.Add(-1f);
            }

            // ── 타이틀 UI
            titleBadge = Txt(titleUi, "중3 · 원과 직선", 14, Navy, 200);
            var badgeBg = Img(titleUi, sTagHole, Cream, new Vector2(170, 32)); badgeBg.type = Image.Type.Sliced; badgeBg.transform.SetSiblingIndex(0);
            titleBadge.transform.SetParent(badgeBg.transform, false); Center(titleBadge.rectTransform);
            titleBest = Txt(titleUi, "", 13, Cream, 300); titleBest.outlineWidth = 0.2f; titleBest.outlineColor = Navy;
            titleHint = Txt(titleUi, "종을 눌러 출발", 15, Cream, 300); titleHint.outlineWidth = 0.25f; titleHint.outlineColor = Navy;

            // ── 개념 다리 카드
            bridgeCard = Img(bridgeUi, sRound, Cream, new Vector2(330, 170)); bridgeCard.type = Image.Type.Sliced;
            bridgeTitle = Txt(bridgeCard.rectTransform, "", 18, Navy, 300); Center(bridgeTitle.rectTransform); bridgeTitle.rectTransform.anchoredPosition = new Vector2(0, 48);
            bridgeBody = Txt(bridgeCard.rectTransform, "", 14, Navy, 300); bridgeBody.textWrappingMode = TextWrappingModes.Normal;
            Center(bridgeBody.rectTransform); bridgeBody.rectTransform.sizeDelta = new Vector2(300, 110); bridgeBody.rectTransform.anchoredPosition = new Vector2(0, -16);
            bridgeUi.gameObject.SetActive(false);

            // ── 결과
            endCard = Img(endUi, sRound, Cream, new Vector2(320, 300)); endCard.type = Image.Type.Sliced;
            var endStripe = Img(endCard.rectTransform, sRound, Apricot, new Vector2(320, 14)); endStripe.type = Image.Type.Sliced;
            endStripe.rectTransform.anchorMin = endStripe.rectTransform.anchorMax = new Vector2(0.5f, 1f); endStripe.rectTransform.anchoredPosition = new Vector2(0, -7);
            endTitle = Txt(endCard.rectTransform, "", 26, Navy, 300); Center(endTitle.rectTransform); endTitle.rectTransform.anchoredPosition = new Vector2(0, 100);
            endTitle.outlineWidth = 0.12f; endTitle.outlineColor = Apricot;
            endFirst = Txt(endCard.rectTransform, "", 40, Navy, 300); Center(endFirst.rectTransform); endFirst.rectTransform.anchoredPosition = new Vector2(0, 40);
            endReason = Txt(endCard.rectTransform, "", 14, Navy, 290); endReason.textWrappingMode = TextWrappingModes.Normal;
            Center(endReason.rectTransform); endReason.rectTransform.sizeDelta = new Vector2(290, 80); endReason.rectTransform.anchoredPosition = new Vector2(0, -38);
            endScore = Txt(endCard.rectTransform, "", 14, MgfLook.Hex("#8A4A1E"), 290); Center(endScore.rectTransform); endScore.rectTransform.anchoredPosition = new Vector2(0, -112);
            endHint = Txt(endUi, "종을 눌러 다시 출발", 15, Cream, 300); endHint.outlineWidth = 0.25f; endHint.outlineColor = Navy;
            endUi.gameObject.SetActive(false);
        }

        RectTransform NewRt(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            return rt;
        }

        RectTransform Full(string name, RectTransform parent)
        {
            var rt = NewRt(name, parent);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        Image Img(RectTransform parent, Sprite sprite, Color c, Vector2 size)
        {
            var rt = NewRt("Img", parent);
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = size;
            var im = rt.gameObject.AddComponent<Image>(); im.sprite = sprite; im.color = c; im.raycastTarget = false;
            return im;
        }

        TextMeshProUGUI Txt(RectTransform parent, string text, float size, Color c, float width)
        {
            var rt = NewRt("Txt", parent);
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(width, size * 1.7f);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = c; t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.raycastTarget = false; t.fontStyle = FontStyles.Bold; t.text = text;
            return t;
        }

        static void Center(RectTransform rt) { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero; }
        static void Place(Graphic g, Vector2 p) { g.rectTransform.anchoredPosition = p; }

        // ───────────────────────── 판형 ─────────────────────────
        float Scale => Mathf.Max(0.01f, MgfText.Canvas.scaleFactor);
        Vector2 PointerLogical() => MgfPointer.Position / Scale;

        Rect BoardRect => land
            ? new Rect(logical.x * 0.205f, logical.y * 0.2f, logical.x * 0.57f, logical.y * 0.63f)
            : new Rect(8f, logical.y * 0.335f, logical.x * 0.775f, logical.y * 0.525f);
        float NetY => land ? logical.y * 0.175f : logical.y * 0.29f;
        float RailX => land ? logical.x - Mathf.Min(100f, logical.x * 0.1f) : logical.x - 46f;
        float RailY1 => land ? logical.y * 0.15f : logical.y * 0.19f;
        float RailY24 => land ? logical.y * 0.79f : logical.y * 0.75f;
        float Spacing => (RailY24 - RailY1) / (JebiRules.MaxLen - 1);
        float YOfValue(int v) => RailY1 + (Mathf.Clamp(v, 0, JebiRules.MaxLen + 1) - 1) * Spacing;

        int ValueFromY(float y)
        {
            float raw = (y - RailY1) / Spacing + 1f;
            int v = Mathf.RoundToInt(raw);
            if (v < 1) return 0;
            if (v > JebiRules.MaxLen) return JebiRules.MaxLen + 1;
            return v;
        }

        Vector2 HandlePos => new Vector2(RailX, YOfValue(handleValue));
        bool NearHandle(Vector2 lp) => (lp - HandlePos).magnitude <= (phase == RunPhase.Practice ? 72f : 50f);
        bool NearPracticeCapture(Vector2 lp) => lp.x >= logical.x * 0.42f && lp.y >= RailY1 - 90f && lp.y <= RailY24 + 70f;
        bool NearRail(Vector2 lp) => Mathf.Abs(lp.x - RailX) < 48f && lp.y > RailY1 - 40f && lp.y < RailY24 + 60f;

        Vector3 ToLocal(Vector2 lp, float z = 0f) => new Vector3((lp.x - logical.x * 0.5f) * U, (lp.y - logical.y * 0.5f) * U, z);

        Vector2 ToLogical(Vector3 world)
        {
            var sp = cam.WorldToScreenPoint(world);
            return new Vector2(sp.x, sp.y) / Scale;
        }

        void Relayout(bool force)
        {
            float scale = Scale;
            if (!force && lastW == Screen.width && lastH == Screen.height && Mathf.Abs(lastScale - scale) < 0.001f) return;
            lastW = Screen.width; lastH = Screen.height; lastScale = scale;
            land = MgfBridge.IsLandLayout;
            logical = new Vector2(Screen.width / scale, Screen.height / scale);
            if (logical.x < 100 || logical.y < 100) logical = new Vector2(390, 844);
            cam.orthographicSize = logical.y * U * 0.5f;
            bg.localScale = new Vector3(logical.x * U * 1.3f, logical.y * U * 1.3f, 1f);

            // 노선 지도
            for (int i = 0; i < miniNodes.Count; i++)
            {
                Vector2 p;
                if (land)
                {
                    float k = i / (float)JebiRules.Stations;
                    p = new Vector2(logical.x * (0.07f + (i % 2) * 0.07f), logical.y * (0.13f + 0.72f * k));
                }
                else
                {
                    float k = i / (float)JebiRules.Stations;
                    float y0 = phase == RunPhase.End ? 0.045f : 0.195f;
                    float x0 = phase == RunPhase.End ? 0.1f : 0.06f, xs = phase == RunPhase.End ? 0.8f : 0.62f;
                    p = new Vector2(logical.x * (x0 + xs * k), logical.y * (y0 + (i % 2) * 0.042f));
                }
                miniNodes[i].localPosition = ToLocal(p, 4f);
                miniNodes[i].localScale = Vector3.one * (land ? 0.95f : 0.85f);
            }
            for (int i = 0; i < miniLinks.Count; i++)
            {
                Vector3 na = miniNodes[i].localPosition, nb = miniNodes[i + 1].localPosition;
                Vector3 dd = (nb - na).normalized * 0.42f * miniNodes[i].localScale.x;
                SetLine(miniLinks[i], na + dd + Vector3.forward * 0.1f, nb - dd + Vector3.forward * 0.1f);
            }
            for (int i = 0; i < farBalloons.Count; i++)
            {
                Vector2 fp = land ? (i == 0 ? new Vector2(logical.x * 0.06f, logical.y * 0.84f) : i == 1 ? new Vector2(logical.x * 0.93f, logical.y * 0.52f) : new Vector2(logical.x * 0.2f, logical.y * 0.3f))
                                  : (i == 0 ? new Vector2(logical.x * 0.08f, logical.y * 0.86f) : i == 1 ? new Vector2(logical.x * 0.95f, logical.y * 0.62f) : new Vector2(logical.x * 0.12f, logical.y * 0.2f));
                farBase[i] = ToLocal(fp, 30f + i * 18f);
            }

            var br = BoardRect;
            net.localPosition = ToLocal(new Vector2(br.center.x, NetY), 1.2f);
            net.localScale = new Vector3(br.width * U / 8f, 1f, 1f);

            ApplyBoardLayout();
            LayoutTitle();
            LayoutUi();
        }

        void LayoutUi()
        {
            float W = logical.x, H = logical.y;
            // HUD — 오른쪽 위 48px 음소거 버튼 자리(템플릿)는 비워 둔다
            float top = H - 24f;
            hudTopPlate.rectTransform.sizeDelta = new Vector2(W - 60f, 46f);
            Place(hudTopPlate, new Vector2((W - 60f) * 0.5f, top));
            for (int i = 0; i < ringIcons.Count; i++) Place(ringIcons[i], new Vector2(24 + i * 34, top));
            float pipX0 = 122f;
            for (int i = 0; i < pips.Count; i++) Place(pips[i], new Vector2(pipX0 + i * 21, top));
            Place(stationTxt, new Vector2(pipX0 + 7 * 21 + 44, top));
            stationTxt.gameObject.SetActive(land);
            float goalW = land ? Mathf.Min(470f, BoardRect.width) : W - 20f;
            goalBg.rectTransform.sizeDelta = new Vector2(goalW, 34);
            goalTxt.rectTransform.sizeDelta = new Vector2(goalW - 30, 30);
            if (land)
            {
                Place(goalBg, new Vector2(BoardRect.center.x, H - 58f));
                Place(firstTxt, new Vector2(W * 0.6f, top));
                Place(timeTxt, new Vector2(W - 150f, top + 3));
                timeBarBg.rectTransform.anchoredPosition = new Vector2(W - 150f, top - 13); timeBarBg.rectTransform.sizeDelta = new Vector2(100, 7);
                timeBar.rectTransform.anchoredPosition = new Vector2(W - 200f, top - 13);
                Place(scoreTxt, new Vector2(W * 0.56f, top - 17f));
            }
            else
            {
                Place(goalBg, new Vector2(W * 0.5f, H - 62f));
                Place(firstTxt, new Vector2(98f, H - 96f));
                Place(timeTxt, new Vector2(W - 128f, H - 92f));
                timeBarBg.rectTransform.anchoredPosition = new Vector2(W - 128f, H - 107f); timeBarBg.rectTransform.sizeDelta = new Vector2(100, 7);
                timeBar.rectTransform.anchoredPosition = new Vector2(W - 178f, H - 107f);
                Place(scoreTxt, new Vector2(W * 0.5f + 8f, H - 96f));
            }

            var br = BoardRect;
            if (land)
            {
                miniRoutePlate.rectTransform.sizeDelta = new Vector2(Mathf.Max(110f, W * 0.16f), H * 0.78f);
                Place(miniRoutePlate, new Vector2(W * 0.085f, H * 0.49f));
            }
            else
            {
                miniRoutePlate.rectTransform.sizeDelta = new Vector2(W * 0.76f, H * 0.115f);
                Place(miniRoutePlate, new Vector2(W * 0.38f, H * 0.225f));
            }
            float slipW = land ? br.width : W * 0.79f;
            float slipH = land ? H * 0.135f : H * 0.12f;
            slipBg.rectTransform.sizeDelta = new Vector2(slipW, slipH);
            Place(slipBg, land ? new Vector2(br.center.x, H * 0.012f + slipH * 0.5f) : new Vector2(8f + slipW * 0.5f, 8f + slipH * 0.5f));
            toastBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(360f, W - 24f), 40);
            toastTxt.rectTransform.sizeDelta = new Vector2(Mathf.Min(350f, W - 34f), 36);

            // 눈금 기둥
            float x = RailX;
            railPost.rectTransform.sizeDelta = new Vector2(72, RailY24 - RailY1 + 2 * Spacing + 30);
            Place(railPost, new Vector2(x, (RailY1 + RailY24) * 0.5f - Spacing * 0.5f));
            for (int i = 0; i < ticks.Count; i++)
            {
                float y = YOfValue(i + 1);
                Place(ticks[i], new Vector2(x - 15, y));
                Place(tickNums[i], new Vector2(x + 19, y));
            }
            Place(spool, new Vector2(x, RailY24 + Spacing + 34));
            LayoutTitle();
        }

        void LayoutTitle()
        {
            if (titleRoot == null) return;
            float W = logical.x, H = logical.y;
            Vector2 ringC = land ? new Vector2(W * 0.6f, H * 0.53f) : new Vector2(W * 0.56f, H * 0.56f);
            float ringR = land ? H * 0.135f : W * 0.24f;
            titleRing.localPosition = ToLocal(ringC, 0.5f);
            titleRing.localScale = Vector3.one * (ringR * U / 2.3f);
            Vector2 p = land ? new Vector2(W * 0.3f, H * 0.36f) : new Vector2(W * 0.12f, H * 0.42f);
            // 타이틀에도 접선 두 개(원 밖 한 점 P 에서)
            Vector2 d = ringC - p; float D = d.magnitude; float phi = Mathf.Acos(Mathf.Clamp(ringR / D, -1f, 1f));
            float th = Mathf.Atan2(-d.y, -d.x);
            Vector2 a = ringC + new Vector2(Mathf.Cos(th + phi), Mathf.Sin(th + phi)) * ringR;
            Vector2 b = ringC + new Vector2(Mathf.Cos(th - phi), Mathf.Sin(th - phi)) * ringR;
            SetLine(tTan1, ToLocal(p, 0.3f), ToLocal(a, 0.3f));
            SetLine(tTan2, ToLocal(p, 0.3f), ToLocal(b, 0.3f));
            Vector2 flat = Mathf.Abs((a - p).normalized.y) < Mathf.Abs((b - p).normalized.y) ? a : b;
            titleCart.localPosition = ToLocal(Vector2.Lerp(p, flat, 0.42f), 0f);
            titleCart.localRotation = Quaternion.identity;
            titleTanDir = (flat - p).normalized;
            Vector2 bellP = land ? new Vector2(W * 0.6f, H * 0.3f) : new Vector2(W * 0.56f, H * 0.3f);
            bell.localPosition = ToLocal(bellP, -0.5f);
            bell.localScale = Vector3.one * (land ? 0.8f : 1f);
            bellRope.localPosition = ToLocal(new Vector2(ringC.x, ringC.y - ringR), 0f);
            SetLine(tRopeL, bellRope.localPosition, bell.localPosition + new Vector3(0, 0.3f, 0));
            bannerHome = ToLocal(land ? new Vector2(W * 0.5f, H * 0.83f) : new Vector2(W * 0.5f, H * 0.84f), -1.5f);
            float bannerScale = land ? 1.05f : Mathf.Min(1f, W * U / 9.6f);
            bannerL.localScale = bannerR.localScale = Vector3.one * bannerScale;
            bannerHalfW = 2.3f * bannerScale;
            Place(titleHint, land ? new Vector2(W * 0.6f, H * 0.065f) : new Vector2(W * 0.56f, H * 0.155f));
            titleBadge.transform.parent.GetComponent<Image>().rectTransform.anchoredPosition = land ? new Vector2(W * 0.2f, H * 0.12f) : new Vector2(W * 0.5f, H * 0.035f + 30f);
            Place(titleBest, land ? new Vector2(W * 0.2f, H * 0.12f - 30f) : new Vector2(W * 0.5f, H * 0.035f - 4f));
            Place(endCard, land ? new Vector2(W * 0.45f, H * 0.52f) : new Vector2(W * 0.5f, H * 0.64f));
            Place(endHint, land ? new Vector2(W * 0.82f, H * 0.2f) : new Vector2(W * 0.5f, H * 0.15f));
            float bw = land ? Mathf.Min(420f, BoardRect.width - 20f) : W - 92f;
            bridgeCard.rectTransform.sizeDelta = new Vector2(bw, 170);
            bridgeTitle.rectTransform.sizeDelta = new Vector2(bw - 24, 30); bridgeBody.rectTransform.sizeDelta = new Vector2(bw - 28, 110);
            Place(bridgeCard, land ? BoardRect.center : new Vector2(8f + bw * 0.5f, BoardRect.center.y));
        }

        Vector3 bannerHome; float bannerHalfW = 2.3f; Vector2 titleTanDir = Vector2.right;

        // ───────────────────────── 도면 ─────────────────────────
        void LoadBoard(JebiProblem p, bool inspect, bool offscreen)
        {
            triMode = p.kind == StationKind.Incircle;
            var rng = layoutRng ?? new JebiRng(3);
            if (p.practice) rng = new JebiRng(12);
            if (!triMode)
            {
                float[] dirs = { 205f, 220f, 235f, 305f, 320f, 335f };
                float th = dirs[rng.Next(dirs.Length)] * Mathf.Deg2Rad;
                float D = 2.2f + rng.Next(3) * 0.2f;
                dR = 1f; dO = Vector2.zero;
                dP = new Vector2(Mathf.Cos(th), Mathf.Sin(th)) * D;
                float phi = Mathf.Acos(dR / D);
                int m = rng.Next(2) == 0 ? 1 : -1;
                dA = new Vector2(Mathf.Cos(th + phi * m), Mathf.Sin(th + phi * m)) * dR;
                dB = new Vector2(Mathf.Cos(th - phi * m), Mathf.Sin(th - phi * m)) * dR;
                dStart = dP; dEnd = dB;
                bbMin = Vector2.Min(new Vector2(-dR, -dR), dP); bbMax = Vector2.Max(new Vector2(dR, dR), dP);
            }
            else
            {
                Vector2[][] T =
                {
                    new[] { new Vector2(-0.3f, 1.75f), new Vector2(-2.0f, -1.05f), new Vector2(2.0f, -1.05f) },
                    new[] { new Vector2(0.55f, 1.85f), new Vector2(-2.1f, -1.0f), new Vector2(1.8f, -1.0f) },
                    new[] { new Vector2(-0.95f, 1.6f), new Vector2(-1.9f, -1.1f), new Vector2(2.1f, -1.1f) },
                };
                var tpl = T[rng.Next(T.Length)];
                bool mirror = rng.Next(2) == 1;
                Vector2 Mx(Vector2 v) => mirror ? new Vector2(-v.x, v.y) : v;
                dA = Mx(tpl[0]); dB = Mx(tpl[1]); dC = Mx(tpl[2]);
                float a = (dB - dC).magnitude, b = (dC - dA).magnitude, c = (dA - dB).magnitude, s = (a + b + c) * 0.5f;
                dI = (a * dA + b * dB + c * dC) / (a + b + c);
                float area = Mathf.Abs((dB.x - dA.x) * (dC.y - dA.y) - (dC.x - dA.x) * (dB.y - dA.y)) * 0.5f;
                dRin = area / s;
                dD = dA + (dB - dA).normalized * (s - a);
                dE = dB + (dC - dB).normalized * (s - b);
                dF = dC + (dA - dC).normalized * (s - c);
                dStart = dB; dEnd = dE;
                bbMin = Vector2.Min(Vector2.Min(dA, dB), dC); bbMax = Vector2.Max(Vector2.Max(dA, dB), dC);
            }
            revealShown = 0f;
            HideFailOverlay();
            stationRotor.localRotation = Quaternion.identity;
            foldL.localRotation = foldR.localRotation = Quaternion.identity;
            for (int i = 0; i < routeLocks.Count; i++)
            {
                routeLocks[i].localRotation = Quaternion.Euler(0, 0, -34f);
                routeLocks[i].localScale = Vector3.one;
                routeLights[i].gameObject.SetActive(false);
            }
            boardSlide = offscreen ? 1f : 0f; slideT = 1f;
            cartMode = 0; cartAlong = 0f; cartRock = 0f;
            ApplyBoardLayout();
            ShowBoardTexts(p, inspect);
            UpdateSlip(p, inspect);
        }

        Vector3 diagOff, capBase; float capMaxW = 6f, diagMinX = -4f, diagMaxX = 4f;

        void SetTagOut(Tag tag, string text, Vector3 anchor, Vector2 dir, float gap)
        {
            SetTag(tag, text, anchor);
            dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.up;
            float shift = gap + 0.5f * (Mathf.Abs(dir.x) * tag.w + Mathf.Abs(dir.y) * tag.h);
            tag.root.localPosition = anchor + (Vector3)(dir * shift);
            ClampTag(tag);
        }

        void ClampTag(Tag tag)
        {
            var p = tag.root.localPosition;
            float lo = diagMinX + tag.w * 0.5f, hi = diagMaxX - tag.w * 0.5f;
            p.x = lo > hi ? (diagMinX + diagMaxX) * 0.5f : Mathf.Clamp(p.x, lo, hi);
            tag.root.localPosition = p;
        }

        /// <summary>설명 띠의 꼬리표를 정해진 순서로 쌓는다(명패 → 실패 길이 → 짝 설명 → 보조 설명).</summary>
        void LayoutCaptions()
        {
            Tag[] order = { tPlaque, tFail, tReveal, tPair2 };
            float total = 0f; int n = 0;
            foreach (var t in order) if (t.root.gameObject.activeSelf) { total += t.h + 0.14f; n++; }
            if (n == 0) return;
            float y = land ? capBase.y + total * 0.5f : capBase.y - 0.1f;
            foreach (var t in order)
            {
                if (!t.root.gameObject.activeSelf) continue;
                t.root.localPosition = new Vector3(capBase.x, y - t.h * 0.5f, capBase.z);
                y -= t.h + 0.14f;
            }
        }

        Vector2 OutNormal(Vector2 a, Vector2 b, Vector2 inside)
        {
            Vector2 n = Perp(b - a);
            if (Vector2.Dot(n, inside - (a + b) * 0.5f) > 0f) n = -n;
            return n;
        }

        Vector3 BoardPos(Vector2 d, float z = 0f)
        {
            var c = (bbMin + bbMax) * 0.5f;
            var v = (d - c) * boardScale;
            return new Vector3(v.x + diagOff.x, v.y + diagOff.y, z);
        }

        Vector3 Outward(Vector2 at, Vector2 from, float dist)
        {
            var dir = (at - from); if (dir.sqrMagnitude < 1e-6f) dir = Vector2.up;
            return BoardPos(at, -0.3f) + (Vector3)(dir.normalized * dist);
        }

        void ApplyBoardLayout()
        {
            if (boardRoot == null) return;
            var br = BoardRect;
            float padX = land ? 1.3f : 0.95f, padY = land ? 1.25f : 1.15f;
            float bw = Mathf.Max(0.5f, bbMax.x - bbMin.x), bh = Mathf.Max(0.5f, bbMax.y - bbMin.y);
            // 결과 설명 띠: 세로 판형은 도면 아래, 가로 판형은 도면 오른쪽. 도면 라벨과 겹치지 않게 공간을 따로 둔다.
            float bandH = land ? 0f : 2.75f;
            float bandW = land ? Mathf.Clamp(br.width * U * 0.36f, 3.6f, 5.6f) : 0f;
            boardScale = Mathf.Min((br.width * U - bandW - 2 * padX) / bw, (br.height * U - bandH - 2 * padY) / bh);
            diagOff = new Vector3(land ? -bandW * 0.5f : 0f, land ? 0f : bandH * 0.5f, 0f);
            float hx = br.width * U * 0.5f;
            diagMinX = -hx + 0.05f; diagMaxX = hx - bandW - 0.05f;
            capBase = land ? new Vector3(hx - bandW * 0.5f, 0f, -0.3f) : new Vector3(0f, -br.height * U * 0.5f + bandH, -0.3f);
            capMaxW = land ? bandW - 0.15f : br.width * U - 0.3f;
            boardScale = Mathf.Clamp(boardScale, 0.6f, land ? 2.6f : 2.2f);
            boardCenter = ToLocal(br.center, 0f);
            boardRoot.localPosition = boardCenter + new Vector3(boardSlide * logical.x * U, 0, 0);

            bool circ = !triMode;
            ring.gameObject.SetActive(circ); disc.gameObject.SetActive(circ); dotO.gameObject.SetActive(circ);
            hookA.gameObject.SetActive(circ); hookB.gameObject.SetActive(circ);
            incircle.gameObject.SetActive(!circ); dotD.gameObject.SetActive(!circ); dotE.gameObject.SetActive(!circ); dotF.gameObject.SetActive(!circ);
            platform.gameObject.SetActive(true);
            if (circ)
            {
                ring.localPosition = BoardPos(dO); ring.localScale = Vector3.one * dR * boardScale;
                disc.localPosition = BoardPos(dO, 0.15f); disc.localScale = Vector3.one * dR * boardScale * 2.5f;
                dotO.localPosition = BoardPos(dO, -0.05f);
                hookA.localPosition = BoardPos(dA, -0.1f); hookB.localPosition = BoardPos(dB, -0.1f);
                platform.localPosition = BoardPos(dP, 0.05f);
                SetLine(sOA, BoardPos(dO, -0.02f), BoardPos(dA, -0.02f));
                SetLine(sOB, BoardPos(dO, -0.02f), BoardPos(dB, -0.02f));
                RightMark(sRaA, dA, dO, dP);
                RightMark(sRaB, dB, dO, dP);
                SetLine(sGiven, BoardPos(dP, -0.04f), BoardPos(dA, -0.04f));
                HideSeg(sTriAB); HideSeg(sTriBC); HideSeg(sTriCA);
                stationRotor.localPosition = BoardPos(dO, 0.22f);
            }
            else
            {
                incircle.localPosition = BoardPos(dI); incircle.localScale = Vector3.one * dRin * boardScale;
                dotD.localPosition = BoardPos(dD, -0.08f); dotE.localPosition = BoardPos(dE, -0.08f); dotF.localPosition = BoardPos(dF, -0.08f);
                platform.localPosition = BoardPos(dB, 0.05f);
                SetLine(sTriAB, BoardPos(dA, -0.02f), BoardPos(dB, -0.02f));
                SetLine(sTriBC, BoardPos(dB, -0.02f), BoardPos(dC, -0.02f));
                SetLine(sTriCA, BoardPos(dC, -0.02f), BoardPos(dA, -0.02f));
                HideSeg(sOA); HideSeg(sOB); HideSeg(sRaA); HideSeg(sRaB); HideSeg(sGiven);
                stationRotor.localPosition = BoardPos(dI, 0.22f);
            }
            stationRotor.localScale = Vector3.one * Mathf.Clamp(boardScale * (circ ? 0.58f : 0.46f), 0.62f, 1.05f);
            platform.localScale = Vector3.one * Mathf.Clamp(boardScale * 0.55f, 0.7f, 1.2f);
            if (current != null) LayoutBoardTexts();
        }

        void RightMark(Seg s, Vector2 at, Vector2 o, Vector2 p)
        {
            float k = 0.2f;
            Vector2 u1 = (o - at).normalized * k, u2 = (p - at).normalized * k;
            s.pts[0] = BoardPos(at + u1, -0.03f); s.pts[1] = BoardPos(at + u1 + u2, -0.03f); s.pts[2] = BoardPos(at + u2, -0.03f);
            SetPoly(s, 3);
        }

        void Bracket(Seg s, Vector2 a, Vector2 b, Vector2 awayFrom, float off)
        {
            Vector2 dir = (b - a).normalized; Vector2 n = new Vector2(-dir.y, dir.x);
            Vector2 mid = (a + b) * 0.5f;
            if (Vector2.Dot(n, mid - awayFrom) < 0) n = -n;
            float o = off / Mathf.Max(0.2f, boardScale);
            float tick = 0.12f / Mathf.Max(0.2f, boardScale);
            s.pts[0] = BoardPos(a + n * (o - tick), -0.03f);
            s.pts[1] = BoardPos(a + n * o, -0.03f);
            s.pts[2] = BoardPos(b + n * o, -0.03f);
            s.pts[3] = BoardPos(b + n * (o - tick), -0.03f);
            SetPoly(s, 4);
        }

        Vector3 BracketMid(Vector2 a, Vector2 b, Vector2 awayFrom, float off)
        {
            Vector2 dir = (b - a).normalized; Vector2 n = new Vector2(-dir.y, dir.x);
            Vector2 mid = (a + b) * 0.5f;
            if (Vector2.Dot(n, mid - awayFrom) < 0) n = -n;
            return BoardPos(mid, -0.3f) + (Vector3)(n * off);
        }

        string LabelTarget(JebiProblem p) => p.Target + "=? cm";

        void ShowBoardTexts(JebiProblem p, bool inspect)
        {
            foreach (var t in tags) t.root.gameObject.SetActive(false);
            HideSeg(sSum1); HideSeg(sSum2); HideSeg(sWhole); HideSeg(sPart);
            LayoutBoardTexts();
        }

        void LayoutBoardTexts()
        {
            var p = current;
            if (p == null) return;
            foreach (var l in letters) l.gameObject.SetActive(false);
            float lo = 0.38f;
            if (!triMode)
            {
                Letter(0, "O", BoardPos(dO, -0.3f) + new Vector3(0.05f, -0.32f, 0));
                Letter(1, "P", Outward(dP, dO, 0.42f));
                Letter(2, "A", Outward(dA, dO, lo));
                Letter(3, "B", Outward(dB, dO, lo));
                Vector2 midA = (dP + dA) * 0.5f, midB = (dP + dB) * 0.5f;
                if (p.kind == StationKind.Pair) SetTagOut(tGiven, "PA=" + p.t + " cm", BoardPos(midA, -0.3f), OutNormal(dP, dA, dO), 0.14f);
                else tGiven.root.gameObject.SetActive(false);
                SetTagOut(tTarget, targetText ?? LabelTarget(p), BoardPos(midB, -0.3f), OutNormal(dP, dB, dO), 0.14f);
                Vector2 away = (dO - dP).normalized;
                SetTag(tRadius, "반지름 " + p.r + " cm", BoardPos(dO + away * 0.5f, -0.3f));
                ClampTag(tRadius);
                if (p.kind == StationKind.Sum)
                {
                    Bracket(sSum1, dP, dA, dO, 0.3f);
                    Bracket(sSum2, dP, dB, dO, 0.3f);
                    SetTagOut(tSum, "PA+PB=" + p.SumPAPB + " cm", BoardPos(dP, -0.3f), (dP - dO), 0.75f);
                }
                else { HideSeg(sSum1); HideSeg(sSum2); tSum.root.gameObject.SetActive(false); }
                if (st.round <= 3) SetTag(tPlaque, "원 O 밖의 점 P에서 원 O에 그은 두 접선", capBase, capMaxW);
                else tPlaque.root.gameObject.SetActive(false);
                if (revealShown <= 0f) SetLine(sTarget, BoardPos(dP, -0.05f), BoardPos(dB, -0.05f));
            }
            else
            {
                Vector2 bc = (dC - dB).normalized;
                Letter(0, "A", Outward(dA, dI, lo + 0.05f));
                Letter(1, "B", Outward(dB, dI, lo + 0.05f));
                Letter(2, "C", Outward(dC, dI, lo + 0.05f));
                Letter(3, "D", Outward(dD, dI, 0.34f));
                Letter(4, "E", Outward(dE, dI, 0.34f) + (Vector3)(bc * 0.22f));
                Letter(5, "F", Outward(dF, dI, 0.34f));
                Bracket(sWhole, dA, dB, dI, 0.62f);
                Bracket(sPart, dA, dD, dI, 0.28f);
                Vector2 nAB = OutNormal(dA, dB, dI);
                SetTagOut(tWhole, "AB=" + p.SideAB + " cm", BoardPos((dA + dB) * 0.5f, -0.3f) + (Vector3)(nAB * 0.62f), nAB, 0.1f);
                SetTag(tPart, "AD=" + p.u + " cm", BoardPos(Vector2.Lerp(dA, dI, 0.42f), -0.3f));
                ClampTag(tPart);
                SetTagOut(tTarget, targetText ?? LabelTarget(p), BoardPos(Vector2.Lerp(dB, dE, 0.45f), -0.3f), OutNormal(dB, dC, dI), 0.14f);
                tGiven.root.gameObject.SetActive(false); tRadius.root.gameObject.SetActive(false); tSum.root.gameObject.SetActive(false);
                tPlaque.root.gameObject.SetActive(false);
                HideSeg(sSum1); HideSeg(sSum2);
                if (revealShown <= 0f) SetLine(sTarget, BoardPos(dB, -0.05f), BoardPos(dE, -0.05f));
            }
            LayoutCaptions();
        }

        static Vector2 Perp(Vector2 v) { v = v.normalized; return new Vector2(-v.y, v.x); }

        void Letter(int i, string s, Vector3 pos)
        {
            var l = letters[i];
            l.gameObject.SetActive(true);
            l.text = s; l.transform.localPosition = pos;
            l.fontSize = FS(17f);
        }

        string targetText;

        void UpdateSlip(JebiProblem p, bool inspect)
        {
            string s = p.prompt;
            if (inspect) s += "\n견습 제비가 뽑아 둔 줄: " + inspectQ + " cm. 맞으면 그대로, 틀리면 고쳐서 놓아라.";
            slipTxt.text = s;
        }

        void HideFailOverlay()
        {
            HideSeg(sFail); HideSeg(sBand1); HideSeg(sBand2); HideSeg(sBand3); HideSeg(sBand4); HideSeg(sCable);
            tFail.root.gameObject.SetActive(false); tReveal.root.gameObject.SetActive(false); tPair2.root.gameObject.SetActive(false);
            waveBead.gameObject.SetActive(false);
            targetText = null;
        }

        // ───────────────────────── 화면 전환 ─────────────────────────
        void ShowTitleVisual()
        {
            titleRoot.gameObject.SetActive(true);
            boardRoot.gameObject.SetActive(false);
            cart.gameObject.SetActive(true);
            revealK = 0f;
            int best = PlayerPrefs.GetInt("jebi-teukgeup.bestFirst", 0);
            titleBest.text = best > 0 ? "최고 첫 제출 " + best + "/8" : "실전 8역 · 첫 제출 6역 이상이면 도착";
            for (int i = 0; i < miniLids.Count; i++) miniLids[i].localRotation = Quaternion.identity;
            SetScreen();
        }

        void RevealVisual() { revealK = 0f; }

        void ResetRunVisual()
        {
            titleRoot.gameObject.SetActive(false);
            boardRoot.gameObject.SetActive(true);
            cart.gameObject.SetActive(true);
            lastW = -1;
            endUi.gameObject.SetActive(false);
            bridgeUi.gameObject.SetActive(false);
            clearRunT = -1f; miniBead.gameObject.SetActive(false);
            for (int i = 0; i < miniLinks.Count; i++) { miniLinks[i].lr.sharedMaterial = dashMat; SegColor(miniLinks[i], new Color(1, 1, 1, 0.75f)); miniLinks[i].lr.widthMultiplier = 0.07f; }
            for (int i = 0; i < miniLids.Count; i++) miniLids[i].localRotation = Quaternion.identity;
            shownTime = shownFirst = shownScore = -1;
            scoreShown = 0f;
            for (int i = 0; i < ringIcons.Count; i++) { ringIcons[i].color = Apricot; ringIcons[i].rectTransform.localScale = Vector3.one; ringIcons[i].rectTransform.localRotation = Quaternion.identity; }
            HideGuide();
        }

        void SetScreen()
        {
            bool title = phase == RunPhase.Title || phase == RunPhase.Reveal;
            bool end = phase == RunPhase.End;
            titleUi.gameObject.SetActive(title);
            hudRoot.gameObject.SetActive(!title);
            railRoot.gameObject.SetActive(!title && !end);
            miniRoot.gameObject.SetActive(!title);
            net.gameObject.SetActive(!title && !end);
            slipBg.gameObject.SetActive(!title && !end);
            goalBg.gameObject.SetActive(!title && !end);
            if (phase == RunPhase.Practice) goalTxt.text = "7 cm를 뽑아 공중 항로를 이어라";
            else if (current != null && !title) goalTxt.text = st.inspect ? "견습 줄 " + current.Target + "를 점검하고 놓아라" : current.Target + "의 길이만큼 줄을 뽑아라";
            bool practice = phase == RunPhase.Practice || (phase == RunPhase.Feedback && feedbackPractice) || st.onboarding;
            ghostRing.gameObject.SetActive(practice && phase == RunPhase.Practice);
            timeTxt.gameObject.SetActive(!practice); timeBar.gameObject.SetActive(!practice); timeBarBg.gameObject.SetActive(!practice);
            firstTxt.gameObject.SetActive(!practice);
            scoreTxt.gameObject.SetActive(false);
            for (int i = 0; i < ringIcons.Count; i++) ringIcons[i].gameObject.SetActive(!practice);
            for (int i = 0; i < pips.Count; i++) pips[i].gameObject.SetActive(!practice);
        }

        void TransitionVisual(int bridge)
        {
            slideT = 0f; slideDur = TransitionOut; boardSlideFrom = 0f; boardSlideTo = -1.15f;
            cartMode = 4; cartModeT = 0f;
            if (bridge > 0)
            {
                bridgeUi.gameObject.SetActive(true);
                if (bridge == 1)
                {
                    bridgeTitle.text = "두 접선의 길이는 같다";
                    bridgeBody.text = "원 O 밖의 점 P에서 원 O에 그은 두 접선: PA=PB\n그래서 PA+PB=PB+PB=2×PB";
                }
                else
                {
                    bridgeTitle.text = "꼭짓점도 원 밖의 한 점이다";
                    bridgeBody.text = "삼각형의 내접원에서 꼭짓점 B는 원 밖의 점, D와 E는 접점이다.\n같은 점 B에서 그은 접선: BD=BE";
                }
                bridgeCard.rectTransform.localScale = Vector3.one * 0.6f;
            }
        }

        void ArriveVisual()
        {
            bridgeUi.gameObject.SetActive(false);
            cartMode = 0; cartModeT = 0f; cartAlong = 0f;
            if (boardSlide != 0f && slideT >= 1f) { slideT = 0f; slideDur = TransitionIn * 0.6f; boardSlideFrom = boardSlide; boardSlideTo = 0f; }
            JebiAudio.Play("clink", 0.25f, 1.3f);
        }

        void EndVisual(string reason)
        {
            endUi.gameObject.SetActive(true);
            bridgeUi.gameObject.SetActive(false);
            HideGuide();
            titleRoot.gameObject.SetActive(true);
            bannerL.gameObject.SetActive(false); bannerR.gameObject.SetActive(false); titleRing.gameObject.SetActive(false);
            tTan1.lr.enabled = false; tTan2.lr.enabled = false; tRopeL.lr.enabled = false;
            bell.gameObject.SetActive(true);
            boardRoot.gameObject.SetActive(false);
            cart.gameObject.SetActive(false);
            bool clear = reason == "clear";
            endTitle.text = clear ? "노선 완성, 도착!" : reason == "time" ? "시간이 끝났다" : reason == "rings" ? "여분 고리를 모두 잃었다" : "통과 기준에 못 미친다";
            endCount = 0f;
            endReason.text = clear
                ? "여덟 역의 줄을 모두 이었다. 같은 점에서 그은 두 접선을 짝지은 덕분이다."
                : reason == "time" ? "90초 안에 여덟 역을 잇지 못했다.\n다음 판: 출발점 문자를 먼저 보고 같은 점의 접선 짝부터 찾아라."
                : reason == "rings" ? "틀린 길이로 고리 3개가 끊어졌다.\n다음 판: 꼭짓점마다 두 접선을 짝지어 보아라."
                : "첫 제출로 6역 이상을 맞혀야 도착한다.\n다음 판: 전체 변에서 어느 부분을 빼야 하는지 먼저 확인하라.";
            endScore.text = "점수 " + st.score + " · 연결 " + st.stationCount + "/8";
            if (clear) { clearRunT = 0f; miniBead.gameObject.SetActive(true); JebiAudio.Play("wave", 0.4f, 0.8f); }
            MgfSfx.Play(clear ? "win" : "lose", 0.5f);
            Relayout(true);
        }

        bool HitRestartBell(Vector2 lp)
        {
            var b = ToLogical(bell.position + frame.up * -0.9f);
            return (lp - b).magnitude < 90f || (lp - (Vector2)endHint.rectTransform.anchoredPosition).magnitude < 80f;
        }

        // ───────────────────────── 안내·거절 ─────────────────────────
        void StartGuide() { guideOn = true; guideT = 0f; guideBoost = 0f; }
        void BoostGuide() { guideOn = true; guideBoost = 1f; guideT = 0f; JebiAudio.Play("tick", 0.2f, 0.6f); }
        void HideGuide()
        {
            guideOn = false;
            if (guideFinger == null) return;
            guideFinger.gameObject.SetActive(false);
            foreach (var d in guideDots) d.gameObject.SetActive(false);
        }

        void Ripple(Vector2 lp)
        {
            var r = ripples[rippleNext];
            rippleT[rippleNext] = 0f;
            r.rectTransform.anchoredPosition = lp;
            rippleNext = (rippleNext + 1) % ripples.Count;
        }

        void Toast(string msg)
        {
            toastTxt.text = msg;
            toastBg.gameObject.SetActive(true);
            toastT = 0f;
            var br = BoardRect;
            Place(toastBg, land ? new Vector2(br.center.x, logical.y * 0.2f) : new Vector2(logical.x * 0.5f - 20f, logical.y * 0.155f));
        }

        void Refuse(string msg, RefuseTarget target, Vector2 lp)
        {
            Toast(msg);
            JebiAudio.Play("refuse", 0.3f);
            switch (target)
            {
                case RefuseTarget.Handle:
                case RefuseTarget.RailPath:
                    handleShakeT = 0f;
                    arrowFrom = lp; arrowTo = HandlePos; arrowT = 0f;
                    if (target == RefuseTarget.RailPath) { guideOn = true; guideT = 0f; guideBoost = 0.5f; }
                    break;
                case RefuseTarget.Cart:
                    heroHop = 0.6f;
                    break;
                case RefuseTarget.Bell:
                    bellSwing = 1f;
                    arrowFrom = lp; arrowTo = ToLogical(bell.position + frame.up * -0.9f); arrowT = 0f;
                    break;
            }
        }

        float bellSwing;

        // ───────────────────────── 지급 고리 연출 ─────────────────────────
        void OnGrabVisual() { valuePunch = 1f; HideGuide(); }
        void OnHandleStep(int dir)
        {
            wheelSpin += dir * 30f;
            valuePunch = 0.6f;
            JebiAudio.Play("tick", 0.16f, dir > 0 ? 1.05f : 0.9f);
        }
        void OnReleaseVisual() { }

        // ───────────────────────── 제출 연출 ─────────────────────────
        void OnSubmitVisual(bool correct, int value, bool practice)
        {
            HideGuide();
            if (correct)
            {
                HideSeg(sTarget);
                HideFailOverlay();
                // 정답 보상은 제비 활주가 아니라 장력파→잠금쇠→노선등 연쇄가 중심이다.
                cartMode = 5; cartModeT = 0f; cartAlong = 0f;
                revealShown = 1f;
                targetText = current.Target + "=" + current.t + " cm";
                LayoutBoardTexts();
                ShowPairBands(true);
                string reveal;
                if (practice) reveal = "점 P에서 원 O에 그은 두 접선: PA=PB=" + current.t + " cm";
                else if (current.kind == StationKind.Pair) reveal = "PA=PB=" + current.t + " cm";
                else if (current.kind == StationKind.Sum) reveal = "PA=PB이므로 PB=" + current.SumPAPB + "÷2=" + current.t + " cm";
                else reveal = "BE=BD=" + current.SideAB + "−" + current.u + "=" + current.t + " cm";
                SetTag(tReveal, reveal, capBase, capMaxW);
                LayoutCaptions();
                JebiAudio.Play("clink", 0.5f);
                if (combo >= 3 && !practice) escortT = 2.2f;
                if (!practice && st.round >= 1 && st.round <= miniLinks.Count) LightLink(st.round - 1);
                firstPunch = 1f;
            }
            else
            {
                failRatio = value / (float)current.t;
                cartMode = 2; cartModeT = 0f;
                DrawFailTrail(value);
                ShowPairBands(false);
                LayoutCaptions();
                JebiAudio.Play("snap", 0.45f);
                MgfSfx.Play("wrong", 0.35f);
                if (!practice) { ringLossT = 0f; MgfFx.Shake(cam, 0.08f, 0.25f); }
            }
        }

        Vector3 RevealPos()
        {
            return BoardPos(new Vector2((bbMin.x + bbMax.x) * 0.5f, bbMin.y), -0.4f) + new Vector3(0, -0.95f, 0);
        }

        void DrawFailTrail(int value)
        {
            Vector3 a = BoardPos(dStart, -0.06f), b = BoardPos(dEnd, -0.06f);
            float ratio = Mathf.Clamp(failRatio, 0.08f, 3f);
            int n = 16;
            if (ratio < 1f)
            {
                Vector3 end = Vector3.Lerp(a, b, ratio);
                for (int i = 0; i < n; i++)
                {
                    float k = i / (n - 1f);
                    sFail.pts[i] = Vector3.Lerp(a, end, k) + new Vector3(0, -Mathf.Sin(k * Mathf.PI * 0.5f) * 0.35f * k, 0);
                }
            }
            else
            {
                float sag = Mathf.Min(1.6f, (ratio - 1f) * 0.9f + 0.25f);
                for (int i = 0; i < n; i++)
                {
                    float k = i / (n - 1f);
                    sFail.pts[i] = Vector3.Lerp(a, b, k) + new Vector3(0, -4f * k * (1 - k) * sag, 0);
                }
            }
            SetPoly(sFail, n);
            SetTag(tFail, "내가 뽑은 줄 " + value + " cm · " + (value < current.t ? "짧다" : "길다"), capBase, capMaxW);
        }

        void ShowPairBands(bool correct)
        {
            HideSeg(sBand1); HideSeg(sBand2); HideSeg(sBand3); HideSeg(sBand4);
            tPair2.root.gameObject.SetActive(false);
            Color w = new Color(1f, 1f, 1f, 0.55f), blue = new Color(0.5f, 0.83f, 1f, 0.55f);
            if (!triMode)
            {
                SetLine(sBand1, BoardPos(dP, 0.02f), BoardPos(dA, 0.02f)); SegColor(sBand1, correct ? blue : w);
                SetLine(sBand2, BoardPos(dP, 0.02f), BoardPos(dB, 0.02f)); SegColor(sBand2, correct ? blue : w);
                if (!correct)
                {
                    string hint = current.kind == StationKind.Sum ? "같은 점 P의 두 접선: PA=PB이므로 PA+PB=2×PB" : "같은 점 P에서 그은 두 접선: PA=PB";
                    SetTag(tReveal, hint, capBase, capMaxW);
                }
            }
            else
            {
                SetLine(sBand1, BoardPos(dA, 0.02f), BoardPos(dD, 0.02f)); SegColor(sBand1, new Color(1f, 0.8f, 0.6f, 0.6f));
                SetLine(sBand2, BoardPos(dA, 0.02f), BoardPos(dF, 0.02f)); SegColor(sBand2, new Color(1f, 0.8f, 0.6f, 0.6f));
                SetLine(sBand3, BoardPos(dB, 0.02f), BoardPos(dD, 0.02f)); SegColor(sBand3, blue);
                SetLine(sBand4, BoardPos(dB, 0.02f), BoardPos(dE, 0.02f)); SegColor(sBand4, blue);
                if (!correct)
                {
                    SetTag(tReveal, "B에서 그은 두 접선: BD=BE", capBase, capMaxW);
                    SetTag(tPair2, "전체에서 부분 빼기: BD=AB−AD", capBase, capMaxW);
                }
            }
        }

        void ResumeRepairVisual()
        {
            cartMode = 3; cartModeT = 0f;
            if (phase == RunPhase.Practice) { guideOn = true; guideT = 0f; }
        }

        void LightLink(int i)
        {
            var l = miniLinks[i];
            l.lr.sharedMaterial = lineMat; SegColor(l, Navy); l.lr.widthMultiplier = 0.1f;
            liftLid = i + 1; liftLidT = 0f;
        }
        int liftLid = -1; float liftLidT;

        // ───────────────────────── 매 프레임 ─────────────────────────
        void UpdateView(float dt)
        {
            float time = Time.time;
            // 구름·먼 기구
            float halfW = logical.x * U * 0.5f + 6f;
            for (int i = 0; i < clouds.Count; i++)
            {
                // 플레이 중에는 발문·HUD·미니 노선과 겹치지 않는 가로 가장자리 구름만 남긴다.
                bool decorZone = phase == RunPhase.Title || phase == RunPhase.Reveal || phase == RunPhase.End || (land && i >= 7);
                if (clouds[i].gameObject.activeSelf != decorZone) clouds[i].gameObject.SetActive(decorZone);
                var p = clouds[i].localPosition;
                p.x += cloudSpeed[i] * dt;
                if (p.x > halfW + 4f) p.x = -halfW - 6f;
                clouds[i].localPosition = p;
            }
            for (int i = 0; i < farBalloons.Count; i++)
            {
                bool showFar = phase == RunPhase.Title || phase == RunPhase.Reveal || phase == RunPhase.End;
                if (farBalloons[i].gameObject.activeSelf != showFar) farBalloons[i].gameObject.SetActive(showFar);
                farBalloons[i].localPosition = farBase[i] + new Vector3(0, Mathf.Sin(time * 0.6f + i * 2f) * 0.25f, 0);
                farBalloons[i].localRotation = Quaternion.Euler(0, time * 8f + i * 40f, 0);
            }

            UpdateBirds(dt, time);
            UpdateCart(dt, time);
            UpdateTitle(dt, time);
            UpdateBoardAnim(dt, time);
            UpdateRail(dt, time);
            UpdateHud(dt, time);
            UpdateMini(dt, time);

            // 리플·화살표·토스트
            for (int i = 0; i < ripples.Count; i++)
            {
                if (rippleT[i] < 0f) continue;
                rippleT[i] += dt;
                float k = rippleT[i] / 0.45f;
                if (k >= 1f) { rippleT[i] = -1f; ripples[i].color = new Color(1, 1, 1, 0); continue; }
                ripples[i].rectTransform.localScale = Vector3.one * (0.35f + k * 1.4f);
                ripples[i].color = new Color(1f, 0.98f, 0.9f, (1f - k) * 0.85f);
            }
            if (arrowT >= 0f)
            {
                arrowT += dt;
                float k = arrowT / 0.9f;
                if (k >= 1f) { arrowT = -1f; arrowImg.gameObject.SetActive(false); }
                else
                {
                    arrowImg.gameObject.SetActive(true);
                    Vector2 d = arrowTo - arrowFrom;
                    float e = 1f - (1f - k) * (1f - k);
                    arrowImg.rectTransform.anchoredPosition = arrowFrom + d * Mathf.Min(0.85f, e * 0.85f);
                    arrowImg.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
                    arrowImg.color = new Color(Apricot.r, Apricot.g, Apricot.b, k < 0.75f ? 1f : (1f - k) * 4f);
                }
            }
            if (toastT >= 0f)
            {
                toastT += dt;
                float a = toastT < 1.6f ? 1f : Mathf.Clamp01(1f - (toastT - 1.6f) / 0.3f);
                toastBg.color = new Color(Navy.r, Navy.g, Navy.b, 0.94f * a);
                toastTxt.color = new Color(Cream.r, Cream.g, Cream.b, a);
                toastBg.rectTransform.localScale = Vector3.one * (toastT < 0.12f ? 0.9f + toastT : 1f);
                if (toastT > 1.9f) { toastT = -1f; toastBg.gameObject.SetActive(false); }
            }
        }

        void UpdateBirds(float dt, float time)
        {
            if (escortT > 0f) escortT -= dt;
            for (int i = 0; i < birds.Count; i++)
            {
                var b = birds[i];
                bool title = phase == RunPhase.Title || phase == RunPhase.Reveal;
                // 플레이 화면의 미니 노선은 가장자리 장식이 아니라 진도 정보다. 새 무리가 그 위를 지나지 않는다.
                bool allowed = title || phase == RunPhase.End;
                if (b.root.gameObject.activeSelf != allowed) b.root.gameObject.SetActive(allowed);
                if (!allowed) continue;
                Vector3 center;
                float rx, ry;
                if (title)
                {
                    center = titleRing.localPosition + new Vector3(0, 0, -0.5f);
                    rx = titleRing.localScale.x * (2.9f + i * 0.45f); ry = rx * 0.45f;
                }
                else
                {
                    Vector3 m0 = miniNodes[0].localPosition, m1 = miniNodes[miniNodes.Count - 1].localPosition;
                    center = (m0 + m1) * 0.5f + new Vector3(0, 0, -1.5f - i * 0.3f);
                    float span = (m1 - m0).magnitude * 0.5f;
                    if (land) { rx = 1.2f + i * 0.35f; ry = span * 2.2f; }
                    else { rx = span * (0.75f + 0.08f * i); ry = 1.2f + i * 0.25f; }
                }
                float a = time * b.speed * 0.6f + b.phase;
                Vector3 pos = title ? center + new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a * 1.3f) * ry * 0.35f, Mathf.Sin(a) * 1.5f)
                                    : center + new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a * (land ? 1f : 1.3f)) * ry * (land ? 0.5f : 0.35f), Mathf.Sin(a) * 1.0f);
                if (escortT > 0f && i < 2)
                {
                    Vector3 target = cart.localPosition + new Vector3(i == 0 ? -0.9f : 0.9f, 1.1f + Mathf.Sin(time * 6f + i) * 0.15f, -0.8f);
                    b.root.localPosition = Vector3.Lerp(b.root.localPosition, target, Mathf.Min(1f, dt * 6f));
                }
                else b.root.localPosition = Vector3.Lerp(b.root.localPosition, pos, Mathf.Min(1f, dt * 3f));
                Vector3 vel = new Vector3(-Mathf.Sin(a) * rx, 0, Mathf.Cos(a) * 1.5f) * Mathf.Sign(b.speed);
                var fwdWorld = frame.TransformDirection(vel.sqrMagnitude > 0.001f ? vel : Vector3.right);
                fwdWorld.y = 0f;
                if (fwdWorld.sqrMagnitude > 0.0001f) b.root.rotation = Quaternion.LookRotation(fwdWorld.normalized, Vector3.up);
                float f = Mathf.Sin(time * b.flapK + b.phase) * 55f;
                b.wl.localRotation = Quaternion.Euler(0, 0, -f); b.wr.localRotation = Quaternion.Euler(0, 0, f);
            }
        }

        void UpdateCart(float dt, float time)
        {
            bool title = phase == RunPhase.Title || phase == RunPhase.Reveal;
            Vector3 a, b; Vector2 dir2;
            if (title)
            {
                a = titleCart.localPosition; b = a; dir2 = titleTanDir;
                cart.localPosition = a + new Vector3(0, Mathf.Sin(time * 2f) * 0.03f, -0.4f);
                cart.localScale = Vector3.one * (land ? 1.5f : 1.45f);
                OrientCart(dir2, 0f);
                AnimateHero(dt, time, 0);
                return;
            }
            cart.localScale = Vector3.one * Mathf.Clamp(boardScale * 0.5f, 0.75f, 1.15f);
            a = boardRoot.localPosition + BoardPos(dStart, -0.45f);
            b = boardRoot.localPosition + BoardPos(dEnd, -0.45f);
            dir2 = (Vector2)(b - a);
            cartModeT += dt;
            Vector3 netPos = net.localPosition + new Vector3(0, 0.15f, -0.6f);
            switch (cartMode)
            {
                case 0:
                    cartPos = a; cartRock = Mathf.Lerp(cartRock, 0f, dt * 6f);
                    AnimateHero(dt, time, 0);
                    break;
                case 1: // 정답: 줄 팽팽 → 장력파 → 예압 → 발차기 → 주행
                    {
                        float t = cartModeT;
                        if (t < 0.45f) cartRock = Mathf.Lerp(cartRock, 0f, dt * 8f);
                        else if (t < 0.62f) cartRock = -14f * Mathf.Sin((t - 0.45f) / 0.17f * Mathf.PI);
                        else cartRock = Mathf.Lerp(cartRock, 4f, dt * 5f);
                        float k = Mathf.Clamp01((t - 0.62f) / 0.85f);
                        float e = k < 0.5f ? 2f * k * k : 1f - Mathf.Pow(-2f * k + 2f, 2f) * 0.5f;
                        cartAlong = e;
                        cartPos = Vector3.Lerp(a, b, e);
                        if (k > 0f && k < 1f) wheelSpin += dt * 900f;
                        if (t >= 0.58f && t - dt < 0.58f) { heroHop = 1f; JebiAudio.Play("kick", 0.5f); }
                        if (t >= 1.47f && t - dt < 1.47f)
                        {
                            JebiAudio.Play("clink", 0.45f, 1.2f);
                            MgfFx.Glow(frame.TransformPoint(b), new Color(0.6f, 0.86f, 1f), 14, 0.6f);
                            MgfSfx.Play("correct", 0.35f);
                        }
                        AnimateHero(dt, time, 1);
                        break;
                    }
                case 2: // 오답: 줄 끝에서 미끄러져 그물로
                    {
                        float t = cartModeT;
                        Vector3 slipEnd = Vector3.Lerp(a, b, Mathf.Clamp(failRatio, 0.1f, 0.6f) * 0.35f);
                        if (t < 0.25f) { cartPos = Vector3.Lerp(a, slipEnd, t / 0.25f); cartRock = Mathf.Lerp(0, 25f, t / 0.25f); }
                        else
                        {
                            float k = Mathf.Clamp01((t - 0.25f) / 0.55f);
                            Vector3 fromP = slipEnd;
                            cartPos = Vector3.Lerp(fromP, netPos, k);
                            cartPos.y = Mathf.Lerp(fromP.y, netPos.y, k * k);
                            if (k >= 1f)
                            {
                                float bt = t - 0.8f;
                                cartPos = netPos + new Vector3(0, Mathf.Abs(Mathf.Sin(bt * 9f)) * 0.35f * Mathf.Exp(-bt * 4f), 0);
                            }
                            if (t >= 0.8f && t - dt < 0.8f) JebiAudio.Play("net", 0.5f);
                            cartRock = Mathf.Lerp(25f, 160f, k);
                        }
                        AnimateHero(dt, time, 2);
                        break;
                    }
                case 3: // 제비가 다시 출발점으로 끌어 올린다
                    {
                        float k = Mathf.Clamp01(cartModeT / 0.55f);
                        float e = 1f - (1f - k) * (1f - k);
                        cartPos = Vector3.Lerp(netPos, a, e) + new Vector3(0, Mathf.Sin(k * Mathf.PI) * 0.8f, 0);
                        cartRock = Mathf.Lerp(160f, 0f, e);
                        if (k >= 1f) cartMode = 0;
                        AnimateHero(dt, time, 1);
                        break;
                    }
                case 4: // 다음 역으로 비행(판이 밀려 나간다)
                    {
                        cartPos = b + new Vector3(0, Mathf.Sin(Mathf.Clamp01(cartModeT / 0.5f) * Mathf.PI) * 0.6f, 0);
                        if (transitionLoaded) cartPos = a + new Vector3(0, 0.4f * Mathf.Exp(-cartModeT * 3f), 0);
                        cartRock = Mathf.Lerp(cartRock, 0f, dt * 5f);
                        AnimateHero(dt, time, 1);
                        break;
                    }
                case 5: // 정답: 차는 제자리에 잠기고 장력파·분기 잠금 사건을 지켜본다
                    {
                        cartPos = a + new Vector3(0, Mathf.Sin(Mathf.Clamp01(cartModeT / 0.45f) * Mathf.PI) * 0.16f, 0);
                        cartRock = Mathf.Sin(Mathf.Clamp01(cartModeT / 0.35f) * Mathf.PI) * -7f;
                        if (cartModeT >= 0.18f && cartModeT - dt < 0.18f) heroHop = 0.72f;
                        AnimateHero(dt, time, 0);
                        break;
                    }
            }
            cart.localPosition = cartPos;
            OrientCart(dir2, cartRock);
        }

        void OrientCart(Vector2 dir2, float rock)
        {
            if (dir2.sqrMagnitude < 1e-5f) dir2 = Vector2.right;
            Vector3 fwd = new Vector3(dir2.x, dir2.y, 0).normalized;
            Vector3 up = new Vector3(-fwd.y, fwd.x, 0);
            if (up.y < 0) up = -up;
            Vector3 upTilt = (up * Mathf.Cos(28f * Mathf.Deg2Rad) + Vector3.back * Mathf.Sin(28f * Mathf.Deg2Rad)).normalized;
            cart.localRotation = Quaternion.LookRotation(fwd, upTilt);
            cartPivot.localRotation = Quaternion.Euler(rock, 0, 0);
            wheelL.localRotation = Quaternion.Euler(wheelSpin, 0, 90); wheelR.localRotation = Quaternion.Euler(wheelSpin, 0, 90);
        }

        void AnimateHero(float dt, float time, int mood)
        {
            if (heroHop > 0f) heroHop = Mathf.Max(0f, heroHop - dt * 2.4f);
            float hop = Mathf.Sin((1f - heroHop) * Mathf.PI) * (heroHop > 0f ? 1f : 0f);
            float squash = heroHop > 0.8f ? 1f - (heroHop - 0.8f) * 1.2f : 1f + hop * 0.12f;
            hero.localPosition = new Vector3(0, 0.97f + hop * 0.45f, -0.05f);
            hero.localScale = new Vector3(0.95f / Mathf.Sqrt(squash), 0.95f * squash, 0.95f / Mathf.Sqrt(squash));
            float flapAmp = mood == 1 ? 55f : mood == 2 ? 70f : (Mathf.Sin(time * 0.7f) > 0.6f ? 30f : 4f);
            float flapSpeed = mood == 0 ? 6f : 16f;
            flap += dt * flapSpeed;
            float f = Mathf.Sin(flap) * flapAmp;
            wingL.localRotation = Quaternion.Euler(0, 0, -f); wingR.localRotation = Quaternion.Euler(0, 0, f);
            heroTail.localRotation = Quaternion.Euler(-12 + Mathf.Sin(time * 2.2f) * 6f, 0, 0);
            heroBody.localRotation = mood == 2 ? Quaternion.Euler(0, 0, Mathf.Sin(time * 10f) * 8f) : Quaternion.Euler(Mathf.Sin(time * 1.3f) * 3f, Mathf.Sin(time * 0.9f) * 12f, 0);
        }

        void UpdateTitle(float dt, float time)
        {
            if (!titleRoot.gameObject.activeSelf) return;
            bool titleish = phase == RunPhase.Title || phase == RunPhase.Reveal;
            if (titleish)
            {
                bannerL.gameObject.SetActive(true); bannerR.gameObject.SetActive(true); titleRing.gameObject.SetActive(true);
                tTan1.lr.enabled = tTan2.lr.enabled = tRopeL.lr.enabled = true;
            }
            if (phase == RunPhase.Reveal) revealK = Mathf.Clamp01(phaseT / 0.38f);
            float open = revealK * revealK * (3f - 2f * revealK);
            float sway = Mathf.Sin(time * 1.1f) * 0.06f;
            bannerL.localPosition = bannerHome + new Vector3(-bannerHalfW - open * 7f, Mathf.Sin(time * 1.3f) * 0.08f + open * 1.5f, 0);
            bannerR.localPosition = bannerHome + new Vector3(bannerHalfW + open * 7f, Mathf.Sin(time * 1.3f + 0.4f) * 0.08f + open * 1.5f, 0);
            bannerL.localRotation = Quaternion.Euler(0, 0, sway * 20f + open * 12f);
            bannerR.localRotation = Quaternion.Euler(0, 0, sway * 20f - open * 12f);
            titleRing.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 0.5f) * 3f);
            if (bellSwing > 0f) bellSwing = Mathf.Max(0f, bellSwing - dt * 1.2f);
            float swing = Mathf.Sin(time * 2.2f) * 6f + Mathf.Sin(time * 14f) * 22f * bellSwing + (phase == RunPhase.Reveal ? Mathf.Sin(phaseT * 30f) * 18f * (1 - revealK) : 0f);
            bell.localRotation = Quaternion.Euler(0, 0, swing);
            float pulse = 1f + Mathf.Sin(time * 3.2f) * 0.04f;
            if (phase == RunPhase.Reveal) pulse = 1f + (1f - revealK) * 0.15f;
            bell.localScale = Vector3.one * (land ? 0.8f : 1f) * pulse;
            if (phase == RunPhase.Title)
            {
                titleHint.color = new Color(Cream.r, Cream.g, Cream.b, 0.75f + Mathf.Sin(time * 3f) * 0.25f);
            }
            if (phase == RunPhase.End)
            {
                Vector2 bp = land ? new Vector2(logical.x * 0.82f, logical.y * 0.5f) : new Vector2(logical.x * 0.5f, logical.y * 0.33f);
                bell.localPosition = ToLocal(bp, -2f);
            }
            else
            {
                Vector2 bellP = land ? new Vector2(logical.x * 0.6f, logical.y * 0.3f) : new Vector2(logical.x * 0.56f, logical.y * 0.3f);
                bell.localPosition = ToLocal(bellP, -0.5f);
            }
        }

        void UpdateBoardAnim(float dt, float time)
        {
            if (!boardRoot.gameObject.activeSelf) return;
            if (slideT < 1f)
            {
                slideT = Mathf.Min(1f, slideT + dt / Mathf.Max(0.05f, slideDur));
                float e = slideT * slideT * (3f - 2f * slideT);
                boardSlide = Mathf.Lerp(boardSlideFrom, boardSlideTo, e);
                if (slideT >= 1f && phase == RunPhase.Transition && boardSlideTo < 0f)
                {
                    // 화면 밖으로 나간 판은 오른쪽에서 새 역으로 다시 들어온다
                    boardSlide = 1.15f;
                }
                boardRoot.localPosition = boardCenter + new Vector3(boardSlide * logical.x * U, 0, 0);
            }
            if (phase == RunPhase.Transition && transitionLoaded)
            {
                float inStart = TransitionOut + BridgeHold(bridgeKind);
                if (phaseT >= inStart)
                {
                    float k = Mathf.Clamp01((phaseT - inStart) / TransitionIn);
                    float e = 1f - Mathf.Pow(1f - k, 3f);
                    boardSlide = Mathf.Lerp(1.15f, 0f, e) + Mathf.Sin(k * Mathf.PI) * -0.03f;
                    boardRoot.localPosition = boardCenter + new Vector3(boardSlide * logical.x * U, 0, 0);
                }
                else { boardSlide = 1.15f; boardRoot.localPosition = boardCenter + new Vector3(boardSlide * logical.x * U, 0, 0); }
                if (bridgeUi.gameObject.activeSelf)
                {
                    float bk = Mathf.Clamp01((phaseT - TransitionOut) / 0.3f);
                    float over = bk < 1f ? 1f + Mathf.Sin(bk * Mathf.PI) * 0.08f : 1f;
                    bridgeCard.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, bk) * over;
                    if (phaseT > inStart - 0.2f) bridgeCard.rectTransform.localScale = Vector3.one * Mathf.Max(0f, 1f - (phaseT - inStart + 0.2f) * 4f);
                }
            }

            // 정답 연출: 줄 팽팽 + 장력파
            if (phase == RunPhase.Feedback && feedbackCorrect)
            {
                float t = phaseT;
                Vector3 a = BoardPos(dStart, -0.08f), b = BoardPos(dEnd, -0.08f);
                float sag = Mathf.Max(0f, 0.5f * (1f - t / 0.16f));
                int n = 12;
                for (int i = 0; i < n; i++)
                {
                    float k = i / (n - 1f);
                    sCable.pts[i] = Vector3.Lerp(a, b, k) + new Vector3(0, -4f * k * (1f - k) * sag, 0);
                }
                SetPoly(sCable, n);
                SegColor(sCable, t < 0.6f ? Color.Lerp(Tension, Navy, Mathf.Clamp01((t - 0.35f) / 0.25f)) : Navy);
                if (t > 0.1f && t < 0.52f)
                {
                    float k = (t - 0.1f) / 0.42f;
                    waveBead.gameObject.SetActive(true);
                    waveBead.localPosition = Vector3.Lerp(b, a, k) + new Vector3(0, 0, -0.2f);
                    waveBead.localScale = Vector3.one * (0.8f + Mathf.Sin(k * Mathf.PI) * 0.6f);
                    if (t - dt <= 0.1f) JebiAudio.Play("wave", 0.35f);
                }
                else waveBead.gameObject.SetActive(false);
                float rk = Mathf.Clamp01(t / 0.25f);
                tReveal.root.localScale = Vector3.one * (rk < 1f ? Mathf.Lerp(0.6f, 1.08f, rk) : Mathf.Lerp(1.08f, 1f, Mathf.Clamp01((t - 0.25f) * 5f)));

                // 공중 항로 분기판: 작은 방향 정렬 → 양쪽 케이블 암 체결 → 세 잠금쇠·노선등 순차 점등.
                float turn = Mathf.Clamp01(t / 0.55f);
                stationRotor.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(turn * Mathf.PI * 0.5f) * 18f);
                float fold = Mathf.Clamp01((t - 0.16f) / 0.34f);
                foldL.localRotation = Quaternion.Euler(0, 0, -28f * fold);
                foldR.localRotation = Quaternion.Euler(0, 0, 28f * fold);
                for (int i = 0; i < routeLocks.Count; i++)
                {
                    float lockAt = 0.48f + i * 0.22f;
                    float k = Mathf.Clamp01((t - lockAt) / 0.14f);
                    float over = k < 1f ? 1f + Mathf.Sin(k * Mathf.PI) * 0.32f : 1f;
                    routeLocks[i].localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-34f, 0f, k));
                    routeLocks[i].localScale = Vector3.one * over;
                    routeLights[i].gameObject.SetActive(k > 0.55f);
                    if (t >= lockAt && t - dt < lockAt) JebiAudio.Play("clink", 0.2f, 0.95f + i * 0.14f);
                }
            }
            else if (phase == RunPhase.Feedback && !feedbackCorrect)
            {
                float k = Mathf.Clamp01(phaseT / 0.3f);
                tFail.root.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, 1f - (1f - k) * (1f - k));
            }
            // 목표 선분 점선이 천천히 흐른다(대상 표시, 정답 정보 없음)
            dashMat.mainTextureOffset = new Vector2(-time * 0.8f, 0);
            float glow = 0.9f + Mathf.Sin(time * 3f) * 0.1f;
            tTarget.root.localScale = Vector3.one * (phase == RunPhase.Playing || phase == RunPhase.Practice ? glow * 0.04f + 0.96f : 1f);
        }

        void UpdateRail(float dt, float time)
        {
            if (!railRoot.gameObject.activeSelf) return;
            float x = RailX;
            Vector2 hp = HandlePos;
            if (handleShakeT >= 0f)
            {
                handleShakeT += dt;
                if (handleShakeT > 0.4f) handleShakeT = -1f;
                else hp.x += Mathf.Sin(handleShakeT * 50f) * 7f * (1f - handleShakeT / 0.4f);
            }
            Place(handleOuter, hp);
            Place(handleHalo, hp);
            float halo = grabbing ? 1.15f : 1f + Mathf.Sin(time * 4f) * 0.12f * (phase == RunPhase.Practice || phase == RunPhase.Playing ? 1f : 0f);
            handleHalo.rectTransform.localScale = Vector3.one * halo;
            handleHalo.color = grabbing ? new Color(1f, 0.95f, 0.8f, 0.55f) : new Color(1f, 0.95f, 0.8f, 0.3f);
            handleOuter.rectTransform.localScale = Vector3.one * (grabbing ? 1.12f : 1f);
            // 줄(스풀 → 고리)
            float spoolY = RailY24 + Spacing + 34;
            Place(railCable, new Vector2(x, hp.y + 26));
            railCable.rectTransform.sizeDelta = new Vector2(3.5f, Mathf.Max(0f, spoolY - hp.y - 26f));
            spoolSpokes.rectTransform.localRotation = Quaternion.Euler(0, 0, -wheelSpin * 0.6f);
            // 값 꼬리표
            if (valuePunch > 0f) valuePunch = Mathf.Max(0f, valuePunch - dt * 4f);
            Place(valueTag, new Vector2(x - 78, hp.y));
            valueTag.rectTransform.localScale = Vector3.one * (1f + valuePunch * 0.15f);
            string vt = handleValue <= 0 ? "끌어 올리기" : handleValue > JebiRules.MaxLen ? "눈금 밖" : handleValue + " cm";
            if (valueTxt.text != vt) { valueTxt.text = vt; valueTxt.fontSize = handleValue >= 1 && handleValue <= JebiRules.MaxLen ? 19 : 12; }
            valueTag.color = handleValue >= 1 && handleValue <= JebiRules.MaxLen ? Navy : new Color(Navy.r, Navy.g, Navy.b, 0.75f);
            // 눈금 강조(현재 값만 — 정답 정보 없음)
            for (int i = 0; i < ticks.Count; i++)
            {
                bool on = i + 1 == handleValue;
                ticks[i].color = on ? Apricot : Navy;
                tickNums[i].color = on ? MgfLook.Hex("#B5652E") : new Color(0.13f, 0.22f, 0.33f, (i + 1) % 5 == 0 ? 1f : 0.8f);
            }
            // 연습 안내: 유령 고리 + 손가락 경로
            bool practice = phase == RunPhase.Practice;
            ghostRing.gameObject.SetActive(practice);
            if (practice)
            {
                Vector2 gp = new Vector2(x, YOfValue(7));
                Place(ghostRing, gp);
                ghostRing.color = new Color(1, 1, 1, 0.55f + Mathf.Sin(time * 4f) * 0.25f);
            }
            if (guideOn && (practice || phase == RunPhase.Playing) && !grabbing)
            {
                guideT += dt;
                Vector2 gFrom = HandlePos, gTo = new Vector2(x, practice ? YOfValue(7) : YOfValue(Mathf.Min(JebiRules.MaxLen, Mathf.Max(1, handleValue) + 6)));
                if (!practice && phase == RunPhase.Playing && guideT > 2.4f) { HideGuide(); return; }
                float cyc = 1.25f;
                float k = Mathf.Repeat(guideT, cyc) / cyc;
                float big = 1f + guideBoost * 0.5f;
                guideFinger.gameObject.SetActive(true);
                float mv = Mathf.Clamp01((k - 0.15f) / 0.6f);
                float e = mv * mv * (3f - 2f * mv);
                Place(guideFinger, Vector2.Lerp(gFrom, gTo, e) + new Vector2(-28f, -14f));
                guideFinger.rectTransform.localScale = Vector3.one * big * (k < 0.15f ? 1f - k : k > 0.75f ? 0.85f : 0.85f);
                guideFinger.color = new Color(Cream.r, Cream.g, Cream.b, k > 0.9f ? (1f - k) * 10f : 1f);
                for (int i = 0; i < guideDots.Count; i++)
                {
                    float dk = (i + 1f) / (guideDots.Count + 1f);
                    guideDots[i].gameObject.SetActive(true);
                    Place(guideDots[i], Vector2.Lerp(gFrom, gTo, dk) + new Vector2(-14f, 0));
                    float flow = Mathf.Repeat(time * 1.5f - dk, 1f);
                    guideDots[i].color = new Color(1f, 0.96f, 0.85f, 0.35f + 0.65f * Mathf.Clamp01(1f - flow * 2f));
                    guideDots[i].rectTransform.localScale = Vector3.one * big;
                }
            }
        }

        void UpdateHud(float dt, float time)
        {
            if (!hudRoot.gameObject.activeSelf) return;
            int secs = Mathf.CeilToInt(activeLeft);
            if (secs != shownTime) { shownTime = secs; timeTxt.text = "남은 시간 " + secs + "초"; }
            timeBar.rectTransform.sizeDelta = new Vector2(100f * Mathf.Clamp01(activeLeft / JebiRules.ActiveSeconds), 7);
            timeBar.color = activeLeft < 15f ? Color.Lerp(Cream, Apricot, 0.5f + Mathf.Sin(time * 8f) * 0.5f) : Cream;
            if (st.firstCorrect != shownFirst || st.firstAttempts != lastFirstAttempts)
            {
                shownFirst = st.firstCorrect; lastFirstAttempts = st.firstAttempts;
                firstTxt.text = "첫 제출 " + st.firstCorrect + "/8 · 기준 6";
            }
            if (firstPunch > 0f) firstPunch = Mathf.Max(0f, firstPunch - dt * 3f);
            firstTxt.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(firstPunch * Mathf.PI) * 0.18f);
            scoreShown = Mathf.MoveTowards(scoreShown, st.score, dt * 400f);
            int sc = Mathf.RoundToInt(scoreShown);
            if (sc != shownScore) { shownScore = sc; scoreTxt.text = "점수 " + sc; }
            for (int i = 0; i < ringIcons.Count; i++)
            {
                bool alive = i < st.lives;
                var ic = ringIcons[i];
                if (!alive && ringLossT >= 0f && i == st.lives)
                {
                    float k = Mathf.Clamp01(ringLossT / 0.6f);
                    ic.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.4f);
                    ic.rectTransform.localRotation = Quaternion.Euler(0, 0, k * 70f);
                    ic.color = Color.Lerp(Apricot, new Color(0.5f, 0.55f, 0.65f, 0.5f), k);
                }
                else ic.color = alive ? Apricot : new Color(0.5f, 0.55f, 0.65f, 0.5f);
            }
            if (ringLossT >= 0f) { ringLossT += dt; if (ringLossT > 0.6f) ringLossT = -1f; }
            for (int i = 0; i < pips.Count; i++)
            {
                bool done = i < st.stationCount;
                bool cur = i == st.round - 1 && !done;
                pips[i].color = done ? Navy : cur ? Apricot : new Color(1, 1, 1, 0.5f);
                pips[i].rectTransform.localScale = Vector3.one * (cur ? 1.15f + Mathf.Sin(time * 5f) * 0.1f : 1f);
            }
            if (st.round != shownRound) { shownRound = st.round; stationTxt.text = st.round > 0 ? "역 " + Mathf.Min(8, st.round) + "/8" : ""; }
            if (phase == RunPhase.End)
            {
                endCount = Mathf.MoveTowards(endCount, st.firstCorrect, dt * 6f);
                int ec = Mathf.FloorToInt(endCount + 0.001f);
                string s = "첫 제출 " + ec + "/8";
                if (endFirst.text != s) { endFirst.text = s; endFirst.rectTransform.localScale = Vector3.one * 1.2f; }
                endFirst.rectTransform.localScale = Vector3.Lerp(endFirst.rectTransform.localScale, Vector3.one, dt * 8f);
                float ck = Mathf.Clamp01(phaseT / 0.35f);
                endCard.rectTransform.localScale = Vector3.one * (ck < 1f ? Mathf.Lerp(0.7f, 1.05f, ck) : Mathf.Lerp(1.05f, 1f, Mathf.Clamp01((phaseT - 0.35f) * 6f)));
                endHint.color = new Color(Cream.r, Cream.g, Cream.b, phaseT < 0.9f ? 0.3f : 0.75f + Mathf.Sin(time * 3f) * 0.25f);
            }
        }
        int lastFirstAttempts = -1, shownRound = -1;

        void UpdateMini(float dt, float time)
        {
            if (!miniRoot.gameObject.activeSelf) return;
            for (int i = 0; i < miniNodes.Count; i++)
            {
                bool cur = i == st.stationCount && phase != RunPhase.End && st.round > 0;
                var p = miniNodes[i].localPosition;
                miniNodes[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(time * 0.8f + i) * 4f);
                float s = (land ? 0.95f : 0.85f) * (cur ? 1.12f + Mathf.Sin(time * 4f) * 0.06f : 1f);
                miniNodes[i].localScale = Vector3.one * s;
            }
            if (liftLid >= 0)
            {
                liftLidT += dt;
                float k = Mathf.Clamp01(liftLidT / 0.5f);
                if (liftLid < miniLids.Count) miniLids[liftLid].localRotation = Quaternion.Euler(-Mathf.Sin(k * Mathf.PI) * 70f, 0, 0);
                if (k >= 1f) liftLid = -1;
            }
            if (clearRunT >= 0f)
            {
                clearRunT += dt;
                float per = 0.24f;
                int seg = Mathf.FloorToInt(clearRunT / per);
                if (seg < miniLinks.Count)
                {
                    int li = miniLinks.Count - 1 - seg;
                    float k = (clearRunT - seg * per) / per;
                    miniBead.localPosition = Vector3.Lerp(miniNodes[li + 1].localPosition, miniNodes[li].localPosition, k) + new Vector3(0, 0, -0.5f);
                    miniLids[li].localRotation = Quaternion.Euler(-Mathf.Sin(k * Mathf.PI) * 80f, 0, 0);
                    SegColor(miniLinks[li], Color.Lerp(Tension, Navy, k));
                    if (k < dt / per * 1.5f) JebiAudio.Play("tick", 0.3f, 1.2f + seg * 0.08f);
                }
                else { miniBead.gameObject.SetActive(false); clearRunT = -1f; }
            }
        }
    }
}
