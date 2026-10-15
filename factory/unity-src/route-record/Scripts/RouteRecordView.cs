using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Mgf;

namespace Mgf.RouteRecord
{
    // 화면: 빗소리 나는 지하 음반 부스(방음재·음반 선반·빗물 창·릴·VU 미터) 한가운데의 2.5D 턴테이블.
    // 직교 카메라 56° — 음반 위의 홈·노치·브리지가 왜곡 없이 읽힌다.
    public partial class RouteRecordGame
    {
        static readonly Color Night = new Color32(9, 10, 15, 255);
        static readonly Color Burgundy = new Color32(58, 29, 45, 255);
        static readonly Color Cream = new Color32(232, 220, 194, 255);
        static readonly Color Cyan = new Color32(32, 217, 210, 255);
        static readonly Color Magenta = new Color32(240, 78, 152, 255);
        static readonly Color Ink = new Color32(28, 22, 27, 255);
        static readonly Color Nickel = new Color32(196, 204, 210, 255);
        static readonly Color Amber = new Color32(246, 177, 91, 255);

        const float RecordY = .62f;
        const float SurfaceLocal = .095f;
        const float Pitch = 56f;
        const int PulseMax = 24;
        static readonly Vector3 ArmPivot = new Vector3(4.6f, 1.42f, 5.4f);
        const float ArmLength = 3.287f;
        const float ArmPlayDeg = -142.98f;   // 바깥 홈 진입점(R0, 60°)을 가리키는 각
        const float ArmRestDeg = -175f;      // 음반 뒤쪽 받침대 — 아래로 끌어 내리면 진입점에 닿는다

        // ── 월드
        Camera cam;
        Transform boothRoot, recordRoot, armYaw, armLift, dragBridgeT, snapRing;
        Material vinylMat, labelMat, burgundyMat, creamMat, nickelMat, darkMetalMat, socketMat, socketRimMat, bridgeMat,
                 cyanLineMat, magentaLineMat, leadLineMat, cyanAddMat, magentaAddMat, pulseCyanMat, pulseMagentaMat,
                 inkFillMat, inkHoleMat, inkExcessMat, flowMat, stripeCyanMat, stripeMagentaMat;
        readonly Transform[] reels = new Transform[2];
        readonly Transform[] vuNeedles = new Transform[2];
        readonly Transform[] rain = new Transform[16];
        readonly Transform[] spareNeedles = new Transform[3];
        readonly Vector3[] spareHome = new Vector3[3];
        readonly float[] spareFall = new float[3];
        readonly LineRenderer[] grooves = new LineRenderer[10];
        int grooveUsed;
        Transform divider, entryMark;
        readonly Transform[,] sockets = new Transform[3, RouteRecordRules.MaxSlots];
        readonly Transform[,] bridges = new Transform[3, RouteRecordRules.MaxSlots];
        readonly Renderer[,] bridgeStripes = new Renderer[3, RouteRecordRules.MaxSlots];
        readonly float[,] socketAppear = new float[3, RouteRecordRules.MaxSlots];
        readonly float[,] bridgeSnap = new float[3, RouteRecordRules.MaxSlots];
        readonly float[,] socketWiggle = new float[3, RouteRecordRules.MaxSlots];
        readonly bool[,] bridgeHidden = new bool[3, RouteRecordRules.MaxSlots];
        readonly TextMeshPro[] groupLabels = new TextMeshPro[3];
        readonly TextMeshPro[] sideLabels = new TextMeshPro[2];
        TextMeshPro labelCount, labelCaption;
        readonly Transform[] inkMarks = new Transform[PulseMax];
        readonly Renderer[] inkRenderers = new Renderer[PulseMax];
        readonly Transform[] flowDots = new Transform[12];
        readonly int[] flowGroove = new int[12];
        readonly float[] flowPhase = new float[12];

        sealed class Pulse
        {
            public Transform t; public TrailRenderer trail; public Renderer body;
            public readonly List<Vector3> pts = new List<Vector3>(220);
            public readonly List<float> cum = new List<float>(220);
            public float len; public int seg; public bool active;
        }
        readonly Pulse[] pulses = new Pulse[PulseMax];
        int pulseUsed, arrived, shownCount;
        float testClock, testSpeed, testDoneAt, hitStop, countShown;
        bool testResultShown, testCorrect, testPractice;
        int testPaths;
        string testMisconception = "";
        readonly int[] tmpGroups = new int[3];
        readonly int[][] slotLists = { new int[RouteRecordRules.MaxSlots], new int[RouteRecordRules.MaxSlots], new int[RouteRecordRules.MaxSlots] };
        readonly int[] slotCounts = new int[3];
        readonly int[] odo = new int[3];
        readonly int[] choice = new int[3];

        // 카메라·월드 시계
        float worldClock, recordAngle, recordSpin, vuKick, vuCenter, labelGlow, framingT, armDeg = ArmRestDeg, armDrop, armTargetDeg = ArmRestDeg, armTargetDrop;
        bool armHeld;
        Vector2 armScreenRest, armScreenPlay;
        float armPullT;
        int lastAspectBucket = -1;
        bool targetsDirty = true;
        bool correctionNote;
        int lastScreenW, lastScreenH;

        // ── UI
        RectTransform uiRoot, titleRt, playRt, endRt, overlayRt;
        RectTransform cardRt, binRt, hudRt, feedbackRt, toastRt, guideRt, restartRt, armHintRt, notchHintRt, ghostRt, rippleRt, logoRt, endCardRt, timerFillRt;
        TextMeshProUGUI logoFill, logoShadow, logoInk, titleTag, titleBadge, titleBest, titleHint;
        TextMeshProUGUI cardStructure, cardPrompt, cardRule, binCount, binCarry, jacketText, timerText, scoreText, feedbackBig, feedbackSmall, toastText, guideText, armHintText;
        TextMeshProUGUI endTitle, endStats, endReasonText, restartText;
        readonly Image[] needleIcons = new Image[3];
        readonly Image[] binTokens = new Image[14];
        readonly Image[] ghostDots = new Image[6];
        CanvasGroup titleGroup, playGroup, endGroup, feedbackGroup, toastGroup, guideGroup, armHintGroup, notchHintGroup, ghostGroup;
        Image rippleImage;
        Sprite roundSprite, ringSprite, panelSprite, bridgeSprite, needleSprite;
        float toastClock, rippleClock, scoreShown, guideClock, endClock, cardIn, carryFlash;
        int endScore, endSolved, endFirst;

        // ── 오디오(합성 — 외부 음원 없음)
        AudioSource noteSrc, crackleSrc;
        readonly AudioClip[] notes = new AudioClip[12];

        float UiScale { get { return Mathf.Max(.5f, MgfText.Canvas.scaleFactor); } }

        // ═════════════════════════ 월드 ═════════════════════════

        void BuildWorld()
        {
            // CreatePrimitive(Cylinder/Sphere)는 콜라이더를 이름으로 붙인다 — 엔진 스트리핑이 지우지 않게 실제로 한 번 참조한다.
            var keep = new GameObject("Collider type keep");
            keep.AddComponent<CapsuleCollider>(); keep.AddComponent<SphereCollider>(); keep.AddComponent<MeshCollider>();
            Destroy(keep);

            MgfLook.Sky(new Color32(14, 9, 16, 255), new Color32(46, 22, 38, 255), new Color32(8, 7, 12, 255), .8f);
            MgfLook.Sun(new Vector3(52, -32, 0), new Color32(255, 226, 196, 255), 1.12f, .66f);
            cam = MgfLook.Camera(new Vector3(0, 20, -13), Vector3.zero, 30f);
            cam.orthographic = true; cam.orthographicSize = 7f;
            cam.transform.rotation = Quaternion.Euler(Pitch, 0, 0);
            cam.nearClipPlane = .1f; cam.farClipPlane = 90f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Night;

            vinylMat = MgfLook.Lit(Color.white, .88f, .12f, null, VinylTexture());
            labelMat = MgfLook.Lit(Color.white, .32f, 0f, null, LabelTexture());
            burgundyMat = MgfLook.Lit(Burgundy, .18f, .02f);
            creamMat = MgfLook.Lit(Cream, .42f, 0f);
            nickelMat = MgfLook.Lit(Nickel, .82f, .78f);
            darkMetalMat = MgfLook.Lit(new Color32(30, 30, 38, 255), .58f, .45f);
            socketMat = MgfLook.Lit(new Color32(4, 4, 7, 255), .9f, .1f);
            socketRimMat = MgfLook.Lit(new Color32(150, 140, 122, 255), .4f, .1f);
            bridgeMat = MgfLook.Lit(new Color32(214, 220, 226, 255), .86f, .85f);
            stripeCyanMat = MgfLook.Lit(Cyan, .6f, .1f, Cyan * .9f);
            stripeMagentaMat = MgfLook.Lit(Magenta, .6f, .1f, Magenta * .9f);
            cyanLineMat = MgfLook.Additive(new Color(Cyan.r, Cyan.g, Cyan.b, .55f));
            magentaLineMat = MgfLook.Additive(new Color(Magenta.r, Magenta.g, Magenta.b, .55f));
            leadLineMat = MgfLook.Additive(new Color(Cream.r, Cream.g, Cream.b, .28f));
            cyanAddMat = MgfLook.Additive(new Color(Cyan.r, Cyan.g, Cyan.b, .9f), MgfLook.SoftDot);
            magentaAddMat = MgfLook.Additive(new Color(Magenta.r, Magenta.g, Magenta.b, .9f), MgfLook.SoftDot);
            flowMat = MgfLook.Additive(new Color(Cyan.r, Cyan.g, Cyan.b, .35f), MgfLook.SoftDot);
            pulseCyanMat = MgfLook.Unlit(Color.Lerp(Cyan, Color.white, .35f));
            pulseMagentaMat = MgfLook.Unlit(Color.Lerp(Magenta, Color.white, .3f));
            var ringTex = RingTexture();
            inkFillMat = MgfLook.Alpha(Color.Lerp(Cyan, Ink, .25f), MgfLook.SoftDot);
            inkHoleMat = MgfLook.Alpha(Ink, ringTex);
            inkExcessMat = MgfLook.Alpha(new Color(Magenta.r * .8f, Magenta.g * .5f, Magenta.b * .6f, 1f), ringTex);

            boothRoot = new GameObject("Rainy underground record booth").transform;
            var staticRoot = new GameObject("Booth static").transform; staticRoot.SetParent(boothRoot, false);
            BuildBooth(staticRoot);
            BuildTurntable(staticRoot);
            StaticBatchingUtility.Combine(staticRoot.gameObject);
            BuildRecord();
            BuildArm();
            BuildPulses();
            BuildAudio();
        }

        void BuildBooth(Transform root)
        {
            MgfLook.Prim(PrimitiveType.Cube, "Floor", new Vector3(0, -.62f, 2), new Vector3(40, .2f, 34), MgfLook.Lit(new Color32(24, 14, 22, 255), .25f, 0f), root, false);
            MgfLook.Prim(PrimitiveType.Cube, "Rear wall", new Vector3(0, 6, 8.4f), new Vector3(40, 14, .5f), burgundyMat, root, false);
            var foamDark = MgfLook.Lit(new Color32(44, 22, 34, 255), .1f, 0f);
            var foamLight = MgfLook.Lit(new Color32(74, 36, 56, 255), .12f, 0f);
            for (int x = -9; x <= 9; x++)
            for (int y = 0; y < 6; y++)
            {
                var f = MgfLook.Prim(PrimitiveType.Cube, "Foam wedge", new Vector3(x * 1.05f, .7f + y * 1.15f, 8.0f), new Vector3(.92f, 1.0f, .42f), ((x + y) & 1) == 0 ? foamDark : foamLight, root, false);
                f.transform.localRotation = Quaternion.Euler(((x + y) & 1) == 0 ? 12f : -12f, 0, 0);
            }
            // 빗물 창
            MgfLook.Block("Rain window frame", new Vector3(-4.2f, 5.6f, 7.55f), new Vector3(4.6f, 2.6f, .3f), .08f, darkMetalMat, root);
            MgfLook.Prim(PrimitiveType.Cube, "Rain glass", new Vector3(-4.2f, 5.6f, 7.35f), new Vector3(4.1f, 2.15f, .06f), MgfLook.Lit(new Color32(22, 48, 60, 255), .9f, .05f, new Color32(10, 30, 40, 255)), root, false);
            // 선반 두 개 + 꽂힌 음반 재킷
            var sleeveMats = new[] { MgfLook.Lit(Cream, .3f, 0f), MgfLook.Lit(Burgundy, .3f, 0f), MgfLook.Lit(new Color32(40, 72, 78, 255), .3f, 0f), MgfLook.Lit(new Color32(18, 18, 24, 255), .5f, 0f), MgfLook.Lit(new Color32(160, 60, 96, 255), .3f, 0f) };
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -9.6f : 10.2f;
                MgfLook.Prim(PrimitiveType.Cube, "Shelf body", new Vector3(sx, 2.6f, 4.2f), new Vector3(4.2f, 6.6f, 3.2f), darkMetalMat, root, false);
                for (int row = 0; row < 4; row++)
                {
                    MgfLook.Prim(PrimitiveType.Cube, "Shelf board", new Vector3(sx, .1f + row * 1.6f, 2.55f), new Vector3(4.2f, .12f, .4f), darkMetalMat, root, false);
                    for (int a = 0; a < 11; a++)
                    {
                        var s = MgfLook.Prim(PrimitiveType.Cube, "Record sleeve", new Vector3(sx - 1.8f + a * .36f, .78f + row * 1.6f, 2.4f), new Vector3(.08f, 1.25f, 1.2f), sleeveMats[(a * 3 + row * 2 + side) % sleeveMats.Length], root, false);
                        s.transform.localRotation = Quaternion.Euler(0, 0, (a % 5 == 2) ? 9f : 0f);
                    }
                }
            }
            // 바닥의 감긴 패치 케이블
            var cable = MgfLook.Lit(new Color32(120, 34, 60, 255), .5f, .1f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f * Mathf.Deg2Rad;
                MgfLook.Prim(PrimitiveType.Cylinder, "Patch cable coil", new Vector3(-6.9f + Mathf.Cos(a) * .55f, -.45f, -4.6f + Mathf.Sin(a) * .55f), new Vector3(.16f, .08f, .16f), cable, root, false);
            }
            // VU 미터·릴
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -1.7f : 1.7f;
                MgfLook.Block("VU case", new Vector3(x, 3.1f, 7.1f), new Vector3(2.9f, 1.7f, .9f), .2f, darkMetalMat, root);
                MgfLook.Block("VU face", new Vector3(x, 3.1f, 6.62f), new Vector3(2.4f, 1.2f, .1f), .1f, MgfLook.Lit(Cream, .3f, 0f, new Color32(60, 46, 30, 255)), root);
            }
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -5.6f : 5.6f;
                MgfLook.Block("Reel deck", new Vector3(x, 2.9f, 7.25f), new Vector3(3.0f, 2.6f, .7f), .2f, darkMetalMat, root);
            }
            // 바늘 컵
            MgfLook.Prim(PrimitiveType.Cylinder, "Needle cup", new Vector3(5.0f, .62f, -4.25f), new Vector3(1.1f, .26f, 1.1f), darkMetalMat, root, false);
        }

        void BuildTurntable(Transform root)
        {
            MgfLook.Block("Turntable plinth", new Vector3(.35f, 0, .55f), new Vector3(11.6f, 1.0f, 11.4f), .45f, MgfLook.Lit(new Color32(20, 18, 24, 255), .55f, .3f), root);
            MgfLook.Block("Plinth trim", new Vector3(.35f, .3f, -5.17f), new Vector3(11.0f, .06f, .05f), .02f, MgfLook.Lit(Magenta, .5f, .2f, Magenta * .25f), root);
            var platter = Mesh(root, "Platter", Disc(), nickelMat, new Vector3(0, .53f, 0), true); platter.transform.localScale = new Vector3(9.0f, .05f, 9.0f);
            var mat2 = Mesh(root, "Platter mat", Disc(), MgfLook.Lit(new Color32(16, 14, 18, 255), .2f, 0f), new Vector3(0, .585f, 0), false); mat2.transform.localScale = new Vector3(8.75f, .005f, 8.75f);
            BuildFrontProps(root);
            MgfLook.Prim(PrimitiveType.Cylinder, "Arm base", new Vector3(ArmPivot.x, .7f, ArmPivot.z), new Vector3(1.2f, .22f, 1.2f), darkMetalMat, root, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "Arm post", new Vector3(ArmPivot.x, 1.05f, ArmPivot.z), new Vector3(.36f, .35f, .36f), nickelMat, root, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "Arm rest", new Vector3(1.33f, .78f, 5.11f), new Vector3(.2f, .28f, .2f), nickelMat, root, false);
            // 노브 두 개
            MgfLook.Prim(PrimitiveType.Cylinder, "Speed knob", new Vector3(-4.6f, .58f, -4.4f), new Vector3(.6f, .1f, .6f), nickelMat, root, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "Pitch knob", new Vector3(-3.7f, .58f, -4.5f), new Vector3(.4f, .1f, .4f), MgfLook.Lit(Cyan, .6f, .2f, Cyan * .4f), root, false);

            // 움직이는 장식(정적 배칭 밖)
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -1.7f : 1.7f;
                var n = new GameObject("VU needle pivot").transform; n.SetParent(boothRoot, false); n.localPosition = new Vector3(x, 2.62f, 6.5f);
                MgfLook.Prim(PrimitiveType.Cube, "VU needle", new Vector3(0, .5f, 0), new Vector3(.05f, 1.0f, .03f), MgfLook.Lit(Magenta, .4f, .1f, Magenta * .3f), n, false);
                vuNeedles[i] = n;
            }
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -5.6f : 5.6f;
                var r = new GameObject("Reel").transform; r.SetParent(boothRoot, false); r.localPosition = new Vector3(x, 2.95f, 6.8f);
                var disc = MgfLook.Prim(PrimitiveType.Cylinder, "Reel disc", Vector3.zero, new Vector3(1.9f, .06f, 1.9f), nickelMat, r, false);
                disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
                for (int h = 0; h < 3; h++)
                {
                    float a = h * 120f * Mathf.Deg2Rad;
                    var hole = MgfLook.Prim(PrimitiveType.Cylinder, "Reel hole", new Vector3(Mathf.Cos(a) * .48f, Mathf.Sin(a) * .48f, -.07f), new Vector3(.42f, .02f, .42f), darkMetalMat, r, false);
                    hole.transform.localRotation = Quaternion.Euler(90, 0, 0);
                }
                reels[i] = r;
            }
            for (int i = 0; i < rain.Length; i++)
            {
                float x = -6.0f + (i * 37 % 360) / 100f;
                float y = 4.7f + (i * 29 % 170) / 100f;
                rain[i] = MgfLook.Prim(PrimitiveType.Cube, "Rain streak", new Vector3(x, y, 7.28f), new Vector3(.03f, .22f + (i % 4) * .08f, .02f), MgfLook.Lit(new Color32(150, 210, 220, 255), .9f, 0f, new Color32(40, 80, 90, 255)), boothRoot, false).transform;
            }
            for (int i = 0; i < 3; i++)
            {
                var tip = MgfLook.Prim(PrimitiveType.Cylinder, "Spare needle", new Vector3(4.7f + i * .3f, 1.05f, -4.25f), new Vector3(.09f, .38f, .09f), nickelMat, boothRoot, false);
                tip.transform.localRotation = Quaternion.Euler(0, 0, -10f + i * 10f);
                spareNeedles[i] = tip.transform; spareHome[i] = tip.transform.localPosition;
            }
        }

        void BuildRecord()
        {
            recordRoot = new GameObject("Route record").transform; recordRoot.SetParent(boothRoot, false);
            recordRoot.localPosition = new Vector3(0, RecordY, 0);
            var vinyl = Mesh(recordRoot, "Black vinyl", Disc(), vinylMat, Vector3.zero, false);
            vinyl.transform.localScale = new Vector3(RecordLayout.RecordRadius * 2, .04f, RecordLayout.RecordRadius * 2);
            vinyl.transform.localPosition = new Vector3(0, .045f, 0);
            var label = Mesh(recordRoot, "Cream label", Disc(), labelMat, new Vector3(0, .086f, 0), false);
            label.transform.localScale = new Vector3(RecordLayout.LabelRadius * 2, .006f, RecordLayout.LabelRadius * 2);
            var sheen = QuadObj("Vinyl sheen (camera-fixed)", MgfLook.Additive(new Color(1f, .93f, .96f, .22f), SheenTexture()), boothRoot, RecordLayout.RecordRadius * 2);
            sheen.transform.localPosition = new Vector3(0, RecordY + .092f, 0);
            MgfLook.Prim(PrimitiveType.Cylinder, "Spindle", new Vector3(0, .2f, 0), new Vector3(.16f, .14f, .16f), nickelMat, recordRoot, false);

            for (int i = 0; i < grooves.Length; i++)
            {
                var go = new GameObject("Route groove " + i); go.transform.SetParent(recordRoot, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false; lr.loop = false; lr.widthMultiplier = .05f; lr.numCapVertices = 2;
                lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false; lr.alignment = LineAlignment.TransformZ;
                go.transform.localRotation = Quaternion.Euler(90, 0, 0);
                lr.sharedMaterial = cyanLineMat; lr.positionCount = 0;
                grooves[i] = lr;
            }
            divider = MgfLook.Block("A/B divider strip", Vector3.zero, new Vector3(3.2f, .05f, .16f), .02f, creamMat, recordRoot).transform;
            entryMark = QuadObj("Entry glow", cyanAddMat, recordRoot, .9f).transform;

            var socketMesh = MgfLook.RoundedBox(new Vector3(.62f, .03f, .3f), .05f, 1);
            var rimMesh = MgfLook.RoundedBox(new Vector3(.76f, .02f, .42f), .08f, 1);
            for (int g = 0; g < 3; g++)
            for (int j = 0; j < RouteRecordRules.MaxSlots; j++)
            {
                var s = new GameObject("Notch " + g + "-" + j).transform; s.SetParent(recordRoot, false);
                Mesh(s, "Notch rim", rimMesh, socketRimMat, new Vector3(0, -.006f, 0), false);
                Mesh(s, "Notch cut", socketMesh, socketMat, Vector3.zero, false);
                sockets[g, j] = s;
                var b = new GameObject("Bridge " + g + "-" + j).transform; b.SetParent(recordRoot, false);
                Mesh(b, "Nickel bridge", MgfLook.RoundedBox(new Vector3(.9f, .13f, .26f), .06f, 1), bridgeMat, new Vector3(0, .07f, 0), true);
                var stripe = Mesh(b, "Magnet stripe", MgfLook.RoundedBox(new Vector3(.5f, .03f, .1f), .015f, 1), stripeCyanMat, new Vector3(0, .145f, 0), false);
                bridgeStripes[g, j] = stripe.GetComponent<Renderer>();
                bridges[g, j] = b; b.gameObject.SetActive(false);
            }
            for (int g = 0; g < 3; g++)
            {
                groupLabels[g] = FlatText("", 3.4f, Cream, recordRoot);
            }
            for (int s = 0; s < 2; s++) sideLabels[s] = FlatText("", 5.5f, s == 0 ? Cyan : Magenta, recordRoot);
            labelCount = FlatText("", 8f, Burgundy, recordRoot); labelCount.transform.localPosition = new Vector3(0, .1f, .36f);
            labelCaption = FlatText("", 2.8f, Burgundy, recordRoot); labelCaption.transform.localPosition = new Vector3(0, .1f, -.62f);
            for (int i = 0; i < PulseMax; i++)
            {
                var q = QuadObj("Ink note", inkFillMat, recordRoot, .24f);
                inkMarks[i] = q.transform; inkRenderers[i] = q.GetComponent<Renderer>(); q.SetActive(false);
            }
            for (int i = 0; i < flowDots.Length; i++)
            {
                var q = QuadObj("Groove flow", flowMat, recordRoot, .34f);
                flowDots[i] = q.transform; flowPhase[i] = i / (float)flowDots.Length; q.SetActive(false);
            }

            dragBridgeT = new GameObject("Dragged bridge").transform; dragBridgeT.SetParent(boothRoot, false);
            Mesh(dragBridgeT, "Nickel bridge", MgfLook.RoundedBox(new Vector3(.9f, .13f, .26f), .06f, 1), bridgeMat, Vector3.zero, true);
            Mesh(dragBridgeT, "Magnet stripe", MgfLook.RoundedBox(new Vector3(.5f, .03f, .1f), .015f, 1), stripeCyanMat, new Vector3(0, .075f, 0), false);
            dragBridgeT.gameObject.SetActive(false);
            snapRing = QuadObj("Snap glow", cyanAddMat, recordRoot, 1.1f).transform; snapRing.gameObject.SetActive(false);
        }

        static Mesh flatQuad;
        // 정점색(흰색)이 있는 바닥에 누운 사각형 — 킷 Alpha/Additive 셰이더가 정점색을 곱하므로 기본 Quad 대신 쓴다.
        static GameObject QuadObj(string name, Material mat, Transform parent, float size)
        {
            if (!flatQuad)
            {
                flatQuad = new Mesh { name = "Flat quad" };
                flatQuad.vertices = new[] { new Vector3(-.5f, 0, -.5f), new Vector3(.5f, 0, -.5f), new Vector3(.5f, 0, .5f), new Vector3(-.5f, 0, .5f) };
                flatQuad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                flatQuad.colors32 = new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) };
                flatQuad.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                flatQuad.triangles = new[] { 0, 3, 2, 0, 2, 1 };
                flatQuad.RecalculateBounds();
            }
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localScale = Vector3.one * size;
            go.AddComponent<MeshFilter>().sharedMesh = flatQuad;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return go;
        }

        static Mesh discMesh;
        // 96각 원기둥(프리미티브 원기둥은 20각이라 음반 테두리가 각져 보인다). 높이 2·반지름 .5 — 프리미티브와 같은 스케일 규약.
        static Mesh Disc()
        {
            if (discMesh) return discMesh;
            const int N = 96;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            v.Add(new Vector3(0, 1, 0)); n.Add(Vector3.up); uv.Add(new Vector2(.5f, .5f));
            for (int i = 0; i <= N; i++)
            {
                float a = i * Mathf.PI * 2 / N; float x = Mathf.Cos(a) * .5f, z = Mathf.Sin(a) * .5f;
                v.Add(new Vector3(x, 1, z)); n.Add(Vector3.up); uv.Add(new Vector2(.5f + x, .5f + z));
            }
            for (int i = 1; i <= N; i++) { tri.Add(0); tri.Add(i + 1); tri.Add(i); }
            int side = v.Count;
            for (int i = 0; i <= N; i++)
            {
                float a = i * Mathf.PI * 2 / N; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(d * .5f + Vector3.up); n.Add(d); uv.Add(new Vector2(i / (float)N, 1));
                v.Add(d * .5f - Vector3.up); n.Add(d); uv.Add(new Vector2(i / (float)N, 0));
            }
            for (int i = 0; i < N; i++)
            {
                int t0 = side + i * 2, b0 = t0 + 1, t1 = t0 + 2, b1 = t0 + 3;
                tri.Add(t0); tri.Add(t1); tri.Add(b0); tri.Add(b0); tri.Add(t1); tri.Add(b1);
                tri.Add(t0); tri.Add(b0); tri.Add(t1); tri.Add(b0); tri.Add(b1); tri.Add(t1);
            }
            discMesh = new Mesh { name = "Route disc" };
            discMesh.SetVertices(v); discMesh.SetNormals(n); discMesh.SetUVs(0, uv); discMesh.SetTriangles(tri, 0); discMesh.RecalculateBounds();
            return discMesh;
        }

        static Texture2D SheenTexture()
        {
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Vinyl sheen", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + .5f) / S * 2 - 1, dy = (y + .5f) / S * 2 - 1;
                float r = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
                float band = Mathf.Clamp01((r - .31f) / .04f) * Mathf.Clamp01((.985f - r) / .03f);
                float wedge = Mathf.Pow(Mathf.Max(0, Mathf.Cos(2f * (a - 2.2f))), 14f) + .35f * Mathf.Pow(Mathf.Max(0, Mathf.Cos(2f * (a - .5f))), 30f);
                float ring = .65f + .35f * Mathf.Sin(r * 260f);
                float al = Mathf.Clamp01(wedge * band * ring + band * .05f);
                px[y * S + x] = new Color32(255, 255, 255, (byte)(al * 255));
            }
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }

        void BuildFrontProps(Transform root)
        {
            // 앞쪽 바닥: 음반 상자(꽂힌 재킷들)와 작은 앰프 — 세로 화면 아래쪽도 부스로 채운다.
            var wood = MgfLook.Lit(new Color32(70, 36, 40, 255), .3f, 0f);
            MgfLook.Block("Record crate", new Vector3(-3.4f, -.15f, -6.9f), new Vector3(3.0f, .9f, 1.7f), .1f, wood, root);
            var sleeveMats = new[] { MgfLook.Lit(Cream, .3f, 0f), MgfLook.Lit(new Color32(40, 72, 78, 255), .3f, 0f), MgfLook.Lit(new Color32(160, 60, 96, 255), .3f, 0f), MgfLook.Lit(new Color32(18, 18, 24, 255), .5f, 0f) };
            for (int i = 0; i < 12; i++)
            {
                var sl = MgfLook.Prim(PrimitiveType.Cube, "Crate sleeve", new Vector3(-4.65f + i * .22f, .25f, -6.9f), new Vector3(.06f, 1.25f, 1.45f), sleeveMats[(i * 5) % sleeveMats.Length], root, false);
                sl.transform.localRotation = Quaternion.Euler(0, 0, -14f + (i % 3) * 3f);
            }
            MgfLook.Block("Little amp", new Vector3(3.3f, -.05f, -6.9f), new Vector3(2.6f, 1.1f, 1.5f), .16f, darkMetalMat, root);
            MgfLook.Block("Amp grille", new Vector3(3.0f, .52f, -6.9f), new Vector3(1.5f, .05f, 1.1f), .04f, MgfLook.Lit(new Color32(48, 30, 40, 255), .1f, 0f), root);
            var dial = MgfLook.Prim(PrimitiveType.Cylinder, "Amp dial", new Vector3(4.2f, .55f, -6.9f), new Vector3(.42f, .05f, .42f), MgfLook.Lit(Cyan, .6f, .2f, Cyan * .6f), root, false);
            dial.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            MgfLook.Prim(PrimitiveType.Cylinder, "Amp knob", new Vector3(4.2f, .55f, -7.4f), new Vector3(.22f, .08f, .22f), nickelMat, root, false);
        }

        static GameObject Mesh(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, bool shadow)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = shadow ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = true;
            return go;
        }

        static TextMeshPro FlatText(string text, float size, Color color, Transform parent)
        {
            var t = MgfText.World(text, Vector3.zero, size, color, parent);
            t.transform.localRotation = Quaternion.Euler(90, 0, 0);
            t.rectTransform.sizeDelta = new Vector2(4, 1);
            t.fontStyle = FontStyles.Bold;
            return t;
        }

        void BuildArm()
        {
            armYaw = new GameObject("Tonearm yaw").transform; armYaw.SetParent(boothRoot, false); armYaw.localPosition = ArmPivot;
            armLift = new GameObject("Tonearm lift").transform; armLift.SetParent(armYaw, false);
            var tube = MgfLook.Block("Tonearm tube", new Vector3(ArmLength * .5f - .2f, 0, 0), new Vector3(ArmLength - .2f, .13f, .13f), .06f, nickelMat, armLift);
            UnityEngine.Object.Destroy(tube.GetComponent<Collider>());
            var head = MgfLook.Block("Headshell", new Vector3(ArmLength - .05f, -.06f, 0), new Vector3(.62f, .14f, .36f), .06f, darkMetalMat, armLift);
            UnityEngine.Object.Destroy(head.GetComponent<Collider>());
            var cart = MgfLook.Block("Cartridge", new Vector3(ArmLength - .05f, -.17f, 0), new Vector3(.34f, .14f, .24f), .04f, MgfLook.Lit(Cyan, .6f, .2f, Cyan * .7f), armLift);
            UnityEngine.Object.Destroy(cart.GetComponent<Collider>());
            var lift = MgfLook.Block("Finger lift", new Vector3(ArmLength + .12f, .02f, -.22f), new Vector3(.08f, .05f, .3f), .02f, nickelMat, armLift);
            UnityEngine.Object.Destroy(lift.GetComponent<Collider>());
            var cw = MgfLook.Prim(PrimitiveType.Cylinder, "Counterweight", new Vector3(-.75f, 0, 0), new Vector3(.5f, .26f, .5f), darkMetalMat, armLift, false);
            cw.transform.localRotation = Quaternion.Euler(0, 0, 90);
            SetArmPose(ArmRestDeg, 0f);
        }

        void SetArmPose(float deg, float drop)
        {
            armYaw.localRotation = Quaternion.Euler(0, -deg, 0);
            armLift.localRotation = Quaternion.Euler(0, 0, -(3f + drop * 5.9f));
        }

        Vector3 ArmHeadWorld(float deg, float drop)
        {
            float a = deg * Mathf.Deg2Rad;
            float tilt = (3f + drop * 5.9f) * Mathf.Deg2Rad;
            float h = Mathf.Cos(tilt) * ArmLength;
            return boothRoot.TransformPoint(ArmPivot + new Vector3(Mathf.Cos(a) * h, -Mathf.Sin(tilt) * ArmLength - .17f, Mathf.Sin(a) * h));
        }

        void BuildPulses()
        {
            var trailMatC = MgfLook.Additive(new Color(Cyan.r, Cyan.g, Cyan.b, .85f));
            var trailMatM = MgfLook.Additive(new Color(Magenta.r, Magenta.g, Magenta.b, .85f));
            for (int i = 0; i < PulseMax; i++)
            {
                var p = new Pulse();
                var go = MgfLook.Prim(PrimitiveType.Sphere, "Route pulse", Vector3.zero, Vector3.one * .2f, i % 2 == 0 ? pulseCyanMat : pulseMagentaMat, recordRoot, false);
                go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                QuadObj("Pulse glow", i % 2 == 0 ? cyanAddMat : magentaAddMat, go.transform, 3.2f);
                var tr = go.AddComponent<TrailRenderer>();
                tr.time = .32f; tr.minVertexDistance = .06f; tr.widthMultiplier = .16f;
                tr.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
                tr.sharedMaterial = i % 2 == 0 ? trailMatC : trailMatM;
                tr.shadowCastingMode = ShadowCastingMode.Off; tr.receiveShadows = false; tr.emitting = false;
                p.t = go.transform; p.trail = tr; p.body = go.GetComponent<Renderer>();
                go.SetActive(false);
                pulses[i] = p;
            }
        }

        void BuildAudio()
        {
            var go = new GameObject("Route audio"); go.transform.SetParent(boothRoot, false);
            noteSrc = go.AddComponent<AudioSource>(); noteSrc.playOnAwake = false; noteSrc.spatialBlend = 0;
            crackleSrc = go.AddComponent<AudioSource>(); crackleSrc.playOnAwake = false; crackleSrc.loop = true; crackleSrc.spatialBlend = 0; crackleSrc.volume = .05f;
            crackleSrc.clip = CrackleClip();
            int[] pent = { 0, 2, 4, 7, 9 };
            for (int i = 0; i < notes.Length; i++)
            {
                int semis = pent[i % 5] + 12 * (i / 5);
                notes[i] = NoteClip(220f * Mathf.Pow(2f, (semis + 3) / 12f), "note" + i);
            }
        }

        static AudioClip NoteClip(float freq, string name)
        {
            const int rate = 44100; int len = (int)(rate * .55f);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Min(1f, t / .006f) * Mathf.Exp(-t * 6.5f);
                float ph = 2f * Mathf.PI * freq * t;
                data[i] = (Mathf.Sin(ph) * .7f + Mathf.Sin(ph * 2f) * .18f + Mathf.Sin(ph * 3.01f) * .08f) * env * .5f;
            }
            var c = AudioClip.Create("rr-" + name, len, 1, rate, false); c.SetData(data, 0); return c;
        }

        static AudioClip CrackleClip()
        {
            const int rate = 22050; int len = rate * 2;
            var data = new float[len]; var r = new System.Random(11); float lp = 0;
            for (int i = 0; i < len; i++)
            {
                lp += ((float)r.NextDouble() * 2 - 1 - lp) * .08f;
                float v = lp * .25f;
                if (r.NextDouble() < .0016) v += ((float)r.NextDouble() * 2 - 1) * .9f;
                data[i] = v;
            }
            var c = AudioClip.Create("rr-crackle", len, 1, rate, false); c.SetData(data, 0); return c;
        }

        void PlayNote(int index, float vol)
        {
            if (!noteSrc) return;
            noteSrc.PlayOneShot(notes[Mathf.Clamp(index, 0, notes.Length - 1)], vol);
        }

        // ═════════════════════════ 텍스처(부팅 1회) ═════════════════════════

        static Texture2D VinylTexture()
        {
            const int S = 512;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Vinyl grooves", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + .5f) / S * 2 - 1, dy = (y + .5f) / S * 2 - 1;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Atan2(dy, dx);
                float groove = .5f + .5f * Mathf.Sin(r * 520f);
                float band = (r > .3f && r < .97f) ? 1f : 0f;
                float gap = Mathf.Abs(Mathf.Repeat(r * 9.3f, 1f) - .5f) < .03f ? .55f : 1f;
                float sheen = Mathf.Pow(Mathf.Max(0, Mathf.Cos(2f * (a - .7f))), 10f) * band;
                float v = 11f + groove * 5f * band * gap + sheen * 34f + (r > .97f ? 10f : 0f);
                byte b = (byte)Mathf.Clamp(v, 0, 255);
                px[y * S + x] = new Color32(b, b, (byte)Mathf.Clamp(v * 1.12f, 0, 255), 255);
            }
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }

        static Texture2D LabelTexture()
        {
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Cream label", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            Color cream = Cream, burg = Burgundy, mag = Magenta;
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + .5f) / S * 2 - 1, dy = (y + .5f) / S * 2 - 1;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                Color c = cream;
                if (Mathf.Abs(r - .9f) < .025f) c = burg;
                else if (Mathf.Abs(r - .82f) < .01f) c = mag;
                else if (r > .93f) c = Color.Lerp(cream, burg, .35f);
                float grain = ((x * 73 + y * 151) % 17) / 17f * .04f;
                c = Color.Lerp(c, burg, grain);
                px[y * S + x] = c;
            }
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }

        static Texture2D RingTexture()
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Ink ring", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + .5f) / S * 2 - 1, dy = (y + .5f) / S * 2 - 1;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - Mathf.Abs(r - .72f) / .16f);
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return tex;
        }

        // ═════════════════════════ 문제 → 음반 ═════════════════════════

        void ApplyProblemVisuals(bool titleMode)
        {
            correctionNote = false;
            var p = current;
            grooveUsed = 0;
            bool sided = p.sideCount == 2;
            float y = SurfaceLocal;
            for (int s = 0; s < p.sideCount; s++)
            {
                Material m = (sided && s == 1) ? magentaLineMat : cyanLineMat;
                int n = layout.sideGaps[s];
                for (int k = 0; k <= n; k++)
                {
                    float from = RecordLayout.EntryDeg, to = layout.exitDeg[s];
                    if (k > 0)
                    {
                        int gPrev = GroupOf(s, k - 1);
                        from = layout.gapStartDeg[gPrev];
                    }
                    if (k < n) { int g = GroupOf(s, k); to = layout.gapEndDeg[g]; }
                    ArcLine(grooves[grooveUsed++], layout.sideRadii[s][k], from, to, layout.sideDir[s], y, m, .055f);
                }
                // 리드아웃(마지막 홈 → 라벨 출구)
                var lead = grooves[grooveUsed++];
                lead.sharedMaterial = leadLineMat; lead.widthMultiplier = .07f; lead.positionCount = 2;
                lead.SetPosition(0, Flat(RecordLayout.Polar(layout.LastRadius(s), layout.exitDeg[s], y)));
                lead.SetPosition(1, Flat(RecordLayout.Polar(RecordLayout.LabelRadius + .04f, layout.exitDeg[s], y)));
                lead.gameObject.SetActive(true);
            }
            for (int i = grooveUsed; i < grooves.Length; i++) grooves[i].gameObject.SetActive(false);

            divider.gameObject.SetActive(sided);
            if (sided)
            {
                float mid = (RecordLayout.RecordRadius + RecordLayout.LabelRadius) * .5f;
                divider.localPosition = RecordLayout.Polar(mid, RecordLayout.EntryDeg + 180f, y + .02f);
                divider.localRotation = Quaternion.Euler(0, -(RecordLayout.EntryDeg + 180f), 0);
            }
            entryMark.localPosition = RecordLayout.Polar(RecordLayout.OuterGroove, RecordLayout.EntryDeg, y + .01f);

            for (int g = 0; g < 3; g++)
            for (int j = 0; j < RouteRecordRules.MaxSlots; j++)
            {
                bool on = g < p.groupCount && j < p.maxima[g];
                sockets[g, j].gameObject.SetActive(on && !titleMode);
                bridges[g, j].gameObject.SetActive(false);
                bridgeHidden[g, j] = false;
                if (!on) continue;
                float deg = layout.slotDeg[g, j];
                var c = layout.SlotCenter(g, j, y);
                sockets[g, j].localPosition = c;
                sockets[g, j].localRotation = Quaternion.Euler(0, -deg, 0);
                float len = layout.GapLength(g);
                sockets[g, j].localScale = new Vector3(Mathf.Max(.6f, len + .05f) / .62f, 1, 1);
                bridges[g, j].localPosition = c;
                bridges[g, j].localRotation = Quaternion.Euler(0, -deg, 0);
                bridges[g, j].localScale = new Vector3((len + .2f) / .9f, 1, 1);
                bridgeStripes[g, j].sharedMaterial = (sided && p.side[g] == 1) ? stripeMagentaMat : stripeCyanMat;
                socketAppear[g, j] = -(g * .12f + j * .05f);
            }
            for (int g = 0; g < 3; g++)
            {
                bool on = g < p.groupCount && !titleMode;
                groupLabels[g].gameObject.SetActive(on);
                if (!on) continue;
                float midDeg = (layout.gapStartDeg[g] + layout.gapEndDeg[g]) * .5f;
                float r = layout.gapInner[g] - .5f;
                var pos = RecordLayout.Polar(r, midDeg, y + .01f);
                groupLabels[g].transform.localPosition = pos;
            }
            for (int s = 0; s < 2; s++)
            {
                bool on = sided && !titleMode;
                sideLabels[s].gameObject.SetActive(on);
                if (!on) continue;
                sideLabels[s].text = s == 0 ? "A면" : "B면";
                sideLabels[s].transform.localPosition = RecordLayout.Polar(RecordLayout.RecordRadius + .02f, RecordLayout.EntryDeg + (s == 0 ? 90f : -90f), y + .03f);
            }
            RefreshGroupLabels();
            for (int i = 0; i < PulseMax; i++) inkMarks[i].gameObject.SetActive(false);
            labelCount.text = titleMode ? "" : p.target.ToString();
            labelCount.color = Burgundy;
            labelCaption.text = titleMode ? "" : "목표 경로";
            for (int i = 0; i < flowDots.Length; i++)
            {
                flowGroove[i] = grooveUsed > 0 ? i % Mathf.Max(1, grooveUsed) : 0;
                flowDots[i].gameObject.SetActive(!titleMode);
            }
            SetCard();
            cardIn = 0;
        }

        int GroupOf(int s, int k)
        {
            for (int g = 0; g < current.groupCount; g++) if (current.side[g] == s && layout.gapInSide[g] == k) return g;
            return 0;
        }

        static Vector3 Flat(Vector3 p) { return new Vector3(p.x, p.z, -p.y); } // LineRenderer 를 X 90° 회전해 눕혔으므로 로컬 좌표 변환

        void ArcLine(LineRenderer lr, float r, float from, float to, int dir, float y, Material m, float width)
        {
            float span = (to - from) * dir;
            if (span < 0) span = 0;
            int steps = Mathf.Max(2, Mathf.CeilToInt(span / 3f) + 1);
            lr.sharedMaterial = m; lr.widthMultiplier = width; lr.positionCount = steps;
            for (int i = 0; i < steps; i++)
                lr.SetPosition(i, Flat(RecordLayout.Polar(r, from + dir * span * i / (steps - 1), y)));
            lr.gameObject.SetActive(true);
        }

        void RefreshGroupLabels()
        {
            if (current == null) return;
            for (int g = 0; g < current.groupCount; g++)
                groupLabels[g].text = current.GroupLabel(g) + " · " + counts[g];
        }

        void RefreshBridges()
        {
            if (current == null || bridges[0, 0] == null) return;
            for (int g = 0; g < 3; g++)
            for (int j = 0; j < RouteRecordRules.MaxSlots; j++)
            {
                bool on = g < current.groupCount && j < current.maxima[g] && placed[g, j] && !bridgeHidden[g, j];
                if (bridges[g, j].gameObject.activeSelf != on) bridges[g, j].gameObject.SetActive(on);
                if (!placed[g, j]) bridgeHidden[g, j] = false;
            }
            RefreshGroupLabels();
            if (binCount) RefreshBin();
        }

        // ═════════════════════════ 프레임 갱신 ═════════════════════════

        void UpdateWorld(float dt)
        {
            worldClock += dt;
            UpdateCamera(dt);

            // 음반 회전: 타이틀은 33⅓ 느낌으로 계속, 편집 중엔 정지(노치를 정확히 누를 수 있게), 시험 중엔 천천히.
            float spinTarget = phase == RunPhase.Title ? 26f : phase == RunPhase.Intro ? 60f : phase == RunPhase.Testing ? 14f : phase == RunPhase.End ? 22f : 0f;
            if (hitStop > 0) { hitStop -= dt; spinTarget = 0; }
            recordSpin = Mathf.MoveTowards(recordSpin, spinTarget, dt * 90f);
            if (Editable && recordSpin < .01f)
            {
                // 편집 중: 0° 로 되감기(스크래치)
                float a = Mathf.Repeat(recordAngle + 180f, 360f) - 180f;
                recordAngle = Mathf.MoveTowards(a, 0f, dt * 420f);
            }
            else recordAngle -= recordSpin * dt;
            recordRoot.localRotation = Quaternion.Euler(0, -recordAngle, 0);

            for (int i = 0; i < 2; i++) reels[i].Rotate(Vector3.forward, (i == 0 ? 40f : -34f) * dt * (phase == RunPhase.Testing ? 3f : 1f), Space.Self);
            for (int i = 0; i < rain.Length; i++)
            {
                var p = rain[i].localPosition; p.y -= dt * (.6f + (i % 5) * .15f);
                if (p.y < 4.45f) p.y = 6.75f;
                rain[i].localPosition = p;
            }
            vuKick = Mathf.MoveTowards(vuKick, 0, dt * 1.4f);
            vuCenter = Mathf.MoveTowards(vuCenter, 0, dt * .7f);
            for (int i = 0; i < 2; i++)
            {
                float idle = -38f + Mathf.Sin(worldClock * (1.7f + i * .4f) + i) * 5f + (phase == RunPhase.Title || phase == RunPhase.End ? Mathf.Abs(Mathf.Sin(worldClock * 3.1f + i)) * 18f : 0);
                float ang = Mathf.Lerp(idle + vuKick * 40f, 0f, Mathf.Clamp01(vuCenter * 1.4f));
                vuNeedles[i].localRotation = Quaternion.Euler(0, 0, -ang);
            }
            for (int i = 0; i < 3; i++)
            {
                if (spareFall[i] <= 0) continue;
                spareFall[i] += dt;
                float k = Mathf.Clamp01(spareFall[i] / .55f);
                spareNeedles[i].localPosition = spareHome[i] + new Vector3(Mathf.Sin(spareFall[i] * 20f) * .05f * (1 - k), -.55f * k * k, 0);
                spareNeedles[i].localRotation = Quaternion.Euler(0, 0, -10f + i * 10f + 70f * k);
            }

            // 톤암
            if (!armHeld)
            {
                armDeg = Mathf.MoveTowards(armDeg, armTargetDeg, dt * 70f);
                armDrop = Mathf.MoveTowards(armDrop, armTargetDrop, dt * 3.2f);
            }
            float bob = (phase == RunPhase.Title) ? Mathf.Sin(worldClock * 2.4f) * .12f : 0f;
            SetArmPose(armDeg, Mathf.Clamp01(armDrop) + bob);

            // 노치 등장(스케일 인) · 브리지 스냅 · 거절 흔들림
            if (current != null)
            {
                for (int g = 0; g < current.groupCount; g++)
                for (int j = 0; j < current.maxima[g]; j++)
                {
                    var s = sockets[g, j];
                    if (!s.gameObject.activeSelf) continue;
                    socketAppear[g, j] += dt;
                    float k = Mathf.Clamp01(socketAppear[g, j] / .28f);
                    float over = k < 1 ? Mathf.Sin(k * Mathf.PI) * .25f : 0;
                    float sx = Mathf.Max(.6f, layout.GapLength(g) + .05f) / .62f;
                    float wig = 0;
                    if (socketWiggle[g, j] > 0) { socketWiggle[g, j] -= dt; wig = Mathf.Sin(socketWiggle[g, j] * 60f) * 8f * socketWiggle[g, j] / .35f; }
                    s.localScale = new Vector3(sx * (k + over), 1, k + over);
                    s.localRotation = Quaternion.Euler(0, -layout.slotDeg[g, j] + wig, 0);
                    var b = bridges[g, j];
                    if (b.gameObject.activeSelf)
                    {
                        float bx = (layout.GapLength(g) + .2f) / .9f;
                        if (bridgeSnap[g, j] > 0)
                        {
                            bridgeSnap[g, j] -= dt;
                            float t = 1f - bridgeSnap[g, j] / .3f;      // 0→1
                            float drop = (1 - t) * (1 - t) * .55f;       // 내려앉기
                            float squash = t < .45f ? 0 : Mathf.Sin((t - .45f) / .55f * Mathf.PI) * .28f; // 착지 순간 납작
                            b.localPosition = layout.SlotCenter(g, j, SurfaceLocal) + new Vector3(0, drop, 0);
                            b.localScale = new Vector3(bx * (1 + squash * .4f), 1 - squash, 1 + squash * .3f);
                        }
                        else { b.localPosition = layout.SlotCenter(g, j, SurfaceLocal); b.localScale = new Vector3(bx, 1, 1); }
                        b.localRotation = Quaternion.Euler(0, -layout.slotDeg[g, j] + wig, 0);
                    }
                }
            }

            // 홈 흐름 점(방향 표시 — 정답 정보 없음, 모든 홈에 똑같이)
            bool flowOn = Editable;
            for (int i = 0; i < flowDots.Length; i++)
            {
                var d = flowDots[i];
                if (!flowOn || grooveUsed == 0) { if (d.gameObject.activeSelf) d.gameObject.SetActive(false); continue; }
                var lr = grooves[flowGroove[i] % grooveUsed];
                if (lr.positionCount < 3) { flowGroove[i]++; continue; }
                if (!d.gameObject.activeSelf) d.gameObject.SetActive(true);
                flowPhase[i] = Mathf.Repeat(flowPhase[i] + dt * .16f, 1f);
                float f = flowPhase[i] * (lr.positionCount - 1);
                int a = Mathf.FloorToInt(f);
                var p0 = lr.GetPosition(a); var p1 = lr.GetPosition(Mathf.Min(lr.positionCount - 1, a + 1));
                var lp = Vector3.Lerp(p0, p1, f - a);
                d.localPosition = new Vector3(lp.x, -lp.z, lp.y) + new Vector3(0, .012f, 0);
                d.localScale = Vector3.one * (.3f * Mathf.Sin(flowPhase[i] * Mathf.PI));
            }

            labelGlow = Mathf.MoveTowards(labelGlow, 0, dt * .9f);
            UpdateTest(dt);
        }

        void UpdateCamera(float dt)
        {
            float target = phase == RunPhase.Title ? 0f : 1f;
            framingT = Mathf.MoveTowards(framingT, target, dt / .95f);
            float e = framingT * framingT * (3 - 2 * framingT);
            float aspect = Mathf.Max(.2f, cam.aspect);
            bool land = aspect >= 1f;
            float playHalfW, playOrtho; Vector3 playFocus; Vector2 playVp;
            float titleHalfW, titleOrtho; Vector3 titleFocus; Vector2 titleVp;
            if (!land)
            {
                playHalfW = 4.75f; playOrtho = playHalfW / aspect; playFocus = new Vector3(.25f, RecordY, .9f); playVp = new Vector2(.5f, .5f);
                titleHalfW = 4.25f; titleOrtho = titleHalfW / aspect; titleFocus = new Vector3(.35f, RecordY, 1.0f); titleVp = new Vector2(.5f, .52f);
            }
            else
            {
                playOrtho = Mathf.Clamp(11.2f / aspect + .2f, 6.6f, 8.6f); playHalfW = playOrtho * aspect;
                playFocus = new Vector3(.3f, RecordY, .6f); playVp = new Vector2(.53f, .47f);
                titleOrtho = 6.1f; titleHalfW = titleOrtho * aspect; titleFocus = new Vector3(.4f, RecordY, .7f); titleVp = new Vector2(.5f, .5f);
            }
            float ortho = Mathf.Lerp(titleOrtho, playOrtho, e);
            float halfW = ortho * aspect;
            Vector3 focus = Vector3.Lerp(titleFocus, playFocus, e);
            Vector2 vp = Vector2.Lerp(titleVp, playVp, e);
            var rot = Quaternion.Euler(Pitch, 0, 0);
            Vector3 fwd = rot * Vector3.forward, up = rot * Vector3.up, right = rot * Vector3.right;
            Vector3 look = focus + right * ((.5f - vp.x) * 2f * halfW) + up * ((.5f - vp.y) * 2f * ortho);
            cam.orthographicSize = ortho;
            cam.transform.SetPositionAndRotation(look - fwd * 26f, rot);

            // 820×1180 같은 태블릿 세로 화면은 390px 모바일보다 Canvas 배율이 커서
            // 같은 하단 앵커를 쓰면 브리지 통 안내가 safe area 아래로 밀린다.
            bool tabletPortrait = !land && Screen.width >= 700;
            int bucket = land ? 2 : tabletPortrait ? 1 : 0;
            if (bucket != lastAspectBucket) { lastAspectBucket = bucket; LayoutUi(land, tabletPortrait); targetsDirty = true; }
        }

        // ═════════════════════════ 시험(톤암 → 펄스) ═════════════════════════

        void BeginTest(bool correct, int paths, string misconception, bool practice)
        {
            testCorrect = correct; testPaths = paths; testMisconception = misconception; testPractice = practice;
            testClock = 0; arrived = 0; testDoneAt = -1; testResultShown = false; countShown = 0;
            hitStop = .09f;
            armTargetDeg = ArmPlayDeg; armTargetDrop = 1f;
            ShowDragBridge(false);
            BuildPulsePaths();
            float maxLen = 0;
            for (int i = 0; i < pulseUsed; i++) maxLen = Mathf.Max(maxLen, pulses[i].len);
            testSpeed = Mathf.Max(5.5f, maxLen / 2.3f);
            labelCount.text = "0"; labelCount.color = Cyan; labelCaption.text = "도착 경로";
            int marks = Mathf.Min(PulseMax, Mathf.Max(current.target, paths));
            for (int i = 0; i < PulseMax; i++)
            {
                bool on = i < marks;
                inkMarks[i].gameObject.SetActive(false);
                if (!on) continue;
                float deg = 90f - i * 360f / marks;
                inkMarks[i].localPosition = RecordLayout.Polar(1.0f, deg, .1f);
                inkMarks[i].localScale = Vector3.one * .24f;
            }
            feedbackBig.text = "톤암 시험"; feedbackSmall.text = "모든 경로로 펄스가 갈라진다";
            feedbackGroup.alpha = 1; Punch(feedbackRt, .1f, .25f);
            guideGroup.alpha = 0; notchHintGroup.alpha = 0; ghostGroup.alpha = 0; armHintGroup.alpha = 0;
            MgfSfx.Play("whoosh", .25f);
        }

        void BuildPulsePaths()
        {
            pulseUsed = 0;
            for (int i = 0; i < PulseMax; i++) { pulses[i].active = false; pulses[i].trail.emitting = false; pulses[i].t.gameObject.SetActive(false); }
            for (int s = 0; s < current.sideCount; s++)
            {
                int n = 0;
                for (int g = 0; g < current.groupCount; g++)
                {
                    if (current.side[g] != s) continue;
                    tmpGroups[n] = g; slotCounts[n] = 0;
                    for (int j = 0; j < current.maxima[g]; j++) if (placed[g, j]) slotLists[n][slotCounts[n]++] = j;
                    n++;
                }
                bool empty = false;
                for (int k = 0; k < n; k++) { odo[k] = 0; if (slotCounts[k] == 0) empty = true; }
                if (empty || n == 0) continue;
                while (pulseUsed < PulseMax)
                {
                    for (int k = 0; k < n; k++) choice[k] = slotLists[k][odo[k]];
                    var p = pulses[pulseUsed++];
                    layout.PathPoints(s, choice, SurfaceLocal + .07f, p.pts);
                    p.cum.Clear(); p.cum.Add(0); float acc = 0;
                    for (int q = 1; q < p.pts.Count; q++) { acc += Vector3.Distance(p.pts[q - 1], p.pts[q]); p.cum.Add(acc); }
                    p.len = acc; p.seg = 0; p.active = true;
                    p.t.gameObject.SetActive(true);
                    p.t.localPosition = p.pts[0];
                    p.trail.Clear(); p.trail.emitting = true;
                    int k2 = n - 1;
                    while (k2 >= 0) { odo[k2]++; if (odo[k2] < slotCounts[k2]) break; odo[k2] = 0; k2--; }
                    if (k2 < 0) break;
                }
            }
        }

        void UpdateTest(float dt)
        {
            if (phase != RunPhase.Testing) return;
            if (hitStop > 0) return;
            testClock += dt;
            float dist = testClock * testSpeed;
            int stillMoving = 0;
            for (int i = 0; i < pulseUsed; i++)
            {
                var p = pulses[i];
                if (!p.active) continue;
                if (dist >= p.len)
                {
                    p.active = false; p.trail.emitting = false; p.t.gameObject.SetActive(false);
                    OnPulseArrive();
                    continue;
                }
                stillMoving++;
                while (p.seg < p.cum.Count - 2 && p.cum[p.seg + 1] < dist) p.seg++;
                float segLen = Mathf.Max(.0001f, p.cum[p.seg + 1] - p.cum[p.seg]);
                p.t.localPosition = Vector3.Lerp(p.pts[p.seg], p.pts[p.seg + 1], (dist - p.cum[p.seg]) / segLen);
                p.t.localScale = Vector3.one * (.2f + .05f * Mathf.Sin(testClock * 18f + i));
            }
            // 24개를 넘는 실제 경로 수는 마지막에 빠르게 마저 센다.
            if (stillMoving == 0 && countShown < testPaths)
            {
                countShown = Mathf.MoveTowards(countShown, testPaths, dt * 30f);
                labelCount.text = Mathf.FloorToInt(countShown).ToString();
            }
            if (stillMoving == 0 && countShown >= testPaths && !testResultShown) { ShowTestResult(); testDoneAt = testClock; }
            if (testResultShown)
            {
                float hold = testCorrect ? (testPractice ? 2.1f : 1.35f) : 2.0f;
                if (testClock - testDoneAt >= hold) FinishTest();
            }
        }

        void OnPulseArrive()
        {
            arrived++;
            countShown = arrived;
            labelCount.text = arrived.ToString();
            labelGlow = Mathf.Min(1f, labelGlow + .25f);
            vuKick = Mathf.Min(1f, vuKick + .35f);
            int marks = Mathf.Min(PulseMax, Mathf.Max(current.target, testPaths));
            int idx = arrived - 1;
            if (idx < marks)
            {
                inkMarks[idx].gameObject.SetActive(true);
                inkRenderers[idx].sharedMaterial = idx < current.target ? inkFillMat : inkExcessMat;
                inkMarks[idx].localScale = Vector3.one * (idx < current.target ? .26f : .34f);
            }
            PlayNote(arrived - 1, .42f / Mathf.Sqrt(Mathf.Max(1, testPaths)) + .06f);
        }

        void ShowTestResult()
        {
            testResultShown = true;
            int marks = Mathf.Min(PulseMax, Mathf.Max(current.target, testPaths));
            labelCount.text = testPaths.ToString();
            if (testCorrect)
            {
                labelCount.color = Cyan;
                labelCaption.text = "가지 완성";
                vuCenter = 1.4f; labelGlow = 1f;
                for (int i = 0; i < Mathf.Min(testPaths, notes.Length); i++) PlayNote(i, .5f / Mathf.Sqrt(Mathf.Max(1, testPaths)));
                MgfSfx.Play("correct", .22f);
                string f = current.Formula(counts) + " = " + testPaths + "가지";
                feedbackBig.text = f;
                feedbackSmall.text = testPractice ? "각 링에서 하나씩 지나므로 곱한다 · 이제 본판" : current.reveal + (st.carryBridges > 0 ? " · 브리지 " + st.carryBridges + "개 이월" : "");
                Punch(feedbackRt, .22f, .38f);
                if (!testPractice && st.carryBridges > 0) { carryFlash = 1.2f; Punch(binRt, .2f, .4f); }
                MgfFx.Glow(recordRoot.position + new Vector3(0, .3f, 0), Color.Lerp(Cyan, Color.white, .3f), 14, .9f);
            }
            else
            {
                labelCount.color = Amber;
                labelCaption.text = "목표 " + current.target;
                // 모자란 만큼 빈 음표 구멍, 넘친 만큼은 이미 겹친 잉크 고리로 남았다.
                for (int i = Mathf.Min(testPaths, marks); i < marks; i++)
                {
                    if (i >= current.target) continue;
                    inkMarks[i].gameObject.SetActive(true);
                    inkRenderers[i].sharedMaterial = inkHoleMat;
                    inkMarks[i].localScale = Vector3.one * .3f;
                }
                feedbackBig.text = "실제 " + testPaths + "가지 · 목표 " + current.target + "가지";
                string why = testPaths == 0 ? "출구까지 이어진 경로가 없다 · 빈 링이 있다"
                    : current.Formula(counts) + " = " + testPaths + " · " + RouteRecordRules.MisconceptionLine(testMisconception);
                feedbackSmall.text = why;
                Punch(feedbackRt, .12f, .3f);
                MgfSfx.Play("wrong", .26f);
                if (!testPractice) NeedleBreak(st.needles);
                armTargetDrop = .35f;
            }
        }

        void EndTestVisuals()
        {
            correctionNote = false;
            for (int i = 0; i < PulseMax; i++) inkMarks[i].gameObject.SetActive(false);
            hitStop = 0;
            for (int i = 0; i < PulseMax; i++) { pulses[i].active = false; pulses[i].trail.emitting = false; pulses[i].t.gameObject.SetActive(false); }
            armTargetDeg = ArmRestDeg; armTargetDrop = 0f;
            feedbackGroup.alpha = 0;
        }

        void CorrectionState()
        {
            labelCount.text = current.target.ToString(); labelCount.color = Burgundy; labelCaption.text = "목표 경로";
            guideText.text = "실제 경로 수를 보고 브리지 배치를 고치시오";
            correctionNote = true; feedbackGroup.alpha = 1;
            guideGroup.alpha = 1; guideClock = 0;
            for (int i = 0; i < PulseMax; i++) inkMarks[i].gameObject.SetActive(false);
        }

        void NeedleBreak(int index)
        {
            int i = Mathf.Clamp(index, 0, 2);
            spareFall[i] = .001f;
            MgfSfx.Play("pop", .2f);
            if (needleIcons[i]) Punch(needleIcons[i].rectTransform, .5f, .35f);
        }

        void EmptyRecord()
        {
            labelGlow = .5f; labelCaption.text = "경로가 없다";
            Punch(cardRt, .06f, .25f);
        }

        void BeginIntroVisuals()
        {
            armTargetDeg = ArmPlayDeg; armTargetDrop = .6f;
            vuKick = 1f;
            if (crackleSrc && !crackleSrc.isPlaying) crackleSrc.Play();
            MgfSfx.Play("whoosh", .3f);
            PlayNote(0, .3f); PlayNote(2, .22f);
        }

        void ResetWorldForRun()
        {
            for (int i = 0; i < 3; i++) { spareFall[i] = 0; spareNeedles[i].localPosition = spareHome[i]; spareNeedles[i].localRotation = Quaternion.Euler(0, 0, -10f + i * 10f); }
            armTargetDeg = ArmRestDeg; armTargetDrop = 0f;
            scoreShown = 0;
        }

        void EndSequence(bool clear)
        {
            endClock = 0; endScore = st.score; endSolved = st.solved; endFirst = st.firstCorrect;
            int best = PlayerPrefs.GetInt("route-record.bestFirst", 0);
            if (st.firstCorrect > best) { PlayerPrefs.SetInt("route-record.bestFirst", st.firstCorrect); PlayerPrefs.Save(); }
            endTitle.text = clear ? "마스터 컷 완성" : "바늘이 멈췄다";
            endTitle.color = clear ? Burgundy : Ink;
            endReasonText.text = endReason;
            if (clear) { MgfSfx.Play("win", .3f); vuCenter = 2f; }
            else MgfSfx.Play("lose", .22f);
            armTargetDeg = ArmRestDeg; armTargetDrop = 0f;
            for (int g = 0; g < 3; g++) groupLabels[g].gameObject.SetActive(false);
            titleBest.text = "최고 최초 정확 " + PlayerPrefs.GetInt("route-record.bestFirst", 0) + "/6";
        }

        // ═════════════════════════ UI ═════════════════════════

        void BuildUi()
        {
            roundSprite = MakeSprite(false, 64, 32, false);
            ringSprite = MakeSprite(true, 64, 32, false);
            panelSprite = MakeSprite(false, 64, 14, true);
            bridgeSprite = MakeBridgeSprite();
            needleSprite = MakeNeedleSprite();
            uiRoot = Full("Route Record UI", MgfText.Canvas.transform);
            BuildTitleUi(); BuildPlayUi(); BuildEndUi(); BuildOverlayUi();
        }

        void BuildTitleUi()
        {
            titleRt = Full("Title", uiRoot); titleGroup = titleRt.gameObject.AddComponent<CanvasGroup>();
            logoRt = R("Logo on label", titleRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(380, 150));
            logoShadow = Txt("Logo magenta echo", logoRt, "루트 레코드", new Vector2(.5f, .5f), new Vector2(4, -5), 54, Magenta, 380, 90);
            logoInk = Txt("Logo ink", logoRt, "루트 레코드", new Vector2(.5f, .5f), new Vector2(0, 0), 54, Cream, 380, 90);
            logoInk.outlineColor = Ink; logoInk.outlineWidth = .28f;
            logoFill = Txt("Logo cyan groove", logoRt, "루트 레코드", new Vector2(.5f, .5f), new Vector2(-2, 2), 54, new Color(Cyan.r, Cyan.g, Cyan.b, .0f), 380, 90);
            foreach (var t in new[] { logoShadow, logoInk, logoFill }) { t.fontStyle = FontStyles.Bold; t.characterSpacing = -2; }
            titleTag = Txt("Tagline", logoRt, "홈을 이어 트랙 수를 맞춰라", new Vector2(.5f, .5f), new Vector2(0, -52), 16, Cream, 340, 30);
            titleTag.outlineColor = Night; titleTag.outlineWidth = .25f;
            var badge = Img("Grade badge", titleRt, new Vector2(.5f, .08f), new Vector2(-70, 0), new Vector2(150, 40), Cream, panelSprite);
            titleBadge = Txt("Badge text", badge, "중2 · 경우의 수", new Vector2(.5f, .5f), Vector2.zero, 15, Burgundy, 150, 34);
            var best = Img("Best badge", titleRt, new Vector2(.5f, .08f), new Vector2(86, 0), new Vector2(150, 40), new Color(Night.r, Night.g, Night.b, .82f), panelSprite);
            titleBest = Txt("Best text", best, "최고 최초 정확 " + PlayerPrefs.GetInt("route-record.bestFirst", 0) + "/6", new Vector2(.5f, .5f), Vector2.zero, 13, Cream, 150, 34);
        }

        void BuildPlayUi()
        {
            playRt = Full("Play", uiRoot); playGroup = playRt.gameObject.AddComponent<CanvasGroup>();
            hudRt = Img("HUD strip", playRt, new Vector2(.5f, 1f), new Vector2(0, -24), new Vector2(370, 40), new Color(Night.r, Night.g, Night.b, .72f), panelSprite);
            for (int i = 0; i < 3; i++)
            {
                var n = Img("Needle tip " + i, hudRt, new Vector2(0, .5f), new Vector2(14 + i * 20, 0), new Vector2(14, 34), Nickel, needleSprite);
                needleIcons[i] = n.GetComponent<Image>();
            }
            jacketText = Txt("Jacket", hudRt, "재킷 0/6", new Vector2(.38f, .5f), Vector2.zero, 15, Cream, 100, 30);
            var bar = Img("Timer track", hudRt, new Vector2(.72f, .5f), new Vector2(0, -9), new Vector2(110, 6), new Color(Cream.r, Cream.g, Cream.b, .2f), panelSprite);
            timerFillRt = Img("Timer fill", bar, new Vector2(0, .5f), Vector2.zero, new Vector2(110, 6), Cyan, panelSprite);
            timerFillRt.pivot = new Vector2(0, .5f);
            timerText = Txt("Timer", hudRt, "110초", new Vector2(.72f, .5f), new Vector2(0, 6), 14, Cream, 110, 24);
            scoreText = Txt("Score", hudRt, "0", new Vector2(1f, .5f), new Vector2(-30, 0), 17, Cream, 70, 30);
            scoreText.alignment = TextAlignmentOptions.Right;

            cardRt = Img("Order sleeve", playRt, new Vector2(.5f, .87f), Vector2.zero, new Vector2(366, 112), Cream, panelSprite);
                        cardStructure = Txt("Structure", cardRt, "", new Vector2(.5f, .83f), new Vector2(-8, 0), 13, Burgundy, 330, 22);
            cardPrompt = Txt("Prompt", cardRt, "", new Vector2(.5f, .47f), new Vector2(-8, 0), 16, Ink, 330, 50);
            cardPrompt.textWrappingMode = TextWrappingModes.Normal;
            cardRule = Txt("Rule", cardRt, "", new Vector2(.5f, .13f), new Vector2(-8, 0), 12, new Color(Burgundy.r, Burgundy.g, Burgundy.b, .9f), 330, 20);

            binRt = Img("Bridge tin", playRt, new Vector2(.27f, .085f), Vector2.zero, new Vector2(176, 96), new Color32(58, 60, 70, 245), panelSprite);
            Img("Tin lip", binRt, new Vector2(.5f, 1f), new Vector2(0, -6), new Vector2(168, 10), Nickel, panelSprite);
            for (int i = 0; i < binTokens.Length; i++)
            {
                int col = i % 7, row = i / 7;
                var t = Img("Bridge token", binRt, new Vector2(0, .5f), new Vector2(18 + col * 14, 14 - row * 26), new Vector2(10, 24), Nickel, bridgeSprite);
                binTokens[i] = t.GetComponent<Image>();
            }
            binCount = Txt("Bridge count", binRt, "", new Vector2(1f, .5f), new Vector2(-30, 10), 26, Cream, 60, 36);
            binCarry = Txt("Carry", binRt, "", new Vector2(1f, .5f), new Vector2(-30, -18), 11, Magenta, 70, 18);
            // 통 바깥에 두면 820x1180 Canvas 배율에서 하단 선반선과 겹친다.
            // 통 안쪽 하단의 전용 띠에 넣어 모든 세로 화면에서 글자 외곽과 배경선 사이를 확보한다.
            Txt("Tin label", binRt, "끌어서 노치에", new Vector2(.5f, 0f), new Vector2(0, 12), 12, Cream, 170, 20);
        }

        void BuildEndUi()
        {
            endRt = Full("End", uiRoot); endGroup = endRt.gameObject.AddComponent<CanvasGroup>();
            endCardRt = Img("Result sleeve", endRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 390), Cream, panelSprite);
            Img("Result record peek", endCardRt, new Vector2(.5f, 1f), new Vector2(0, -6), new Vector2(150, 150), Ink, roundSprite);
            Img("Result label", endCardRt, new Vector2(.5f, 1f), new Vector2(0, -6), new Vector2(52, 52), Magenta, roundSprite);
            endTitle = Txt("Result title", endCardRt, "", new Vector2(.5f, .68f), Vector2.zero, 28, Burgundy, 300, 44);
            endStats = Txt("Result stats", endCardRt, "", new Vector2(.5f, .46f), Vector2.zero, 19, Ink, 300, 96);
            endReasonText = Txt("Result reason", endCardRt, "", new Vector2(.5f, .27f), Vector2.zero, 13, Burgundy, 290, 40);
            endReasonText.textWrappingMode = TextWrappingModes.Normal;
            restartRt = Img("Replay label", endCardRt, new Vector2(.5f, .1f), Vector2.zero, new Vector2(240, 56), Burgundy, panelSprite);
            restartText = Txt("Replay text", restartRt, "다시 재생", new Vector2(.5f, .5f), Vector2.zero, 20, Cream, 220, 40);
        }

        void BuildOverlayUi()
        {
            overlayRt = Full("Overlay", uiRoot);
            guideRt = Img("Goal banner", overlayRt, new Vector2(.5f, .755f), Vector2.zero, new Vector2(352, 34), new Color(Night.r, Night.g, Night.b, .78f), panelSprite);
            guideGroup = guideRt.gameObject.AddComponent<CanvasGroup>(); guideGroup.alpha = 0;
            guideText = Txt("Goal text", guideRt, "", new Vector2(.5f, .5f), Vector2.zero, 13, Cyan, 340, 30);

            feedbackRt = Img("Test result", overlayRt, new Vector2(.5f, .265f), Vector2.zero, new Vector2(352, 76), new Color(Night.r, Night.g, Night.b, .9f), panelSprite);
            feedbackGroup = feedbackRt.gameObject.AddComponent<CanvasGroup>(); feedbackGroup.alpha = 0;
            feedbackBig = Txt("Result formula", feedbackRt, "", new Vector2(.5f, .66f), Vector2.zero, 22, Cream, 340, 34);
            feedbackSmall = Txt("Result rule", feedbackRt, "", new Vector2(.5f, .24f), Vector2.zero, 12, Cyan, 340, 28);
            feedbackSmall.textWrappingMode = TextWrappingModes.Normal;

            toastRt = Img("Refusal", overlayRt, new Vector2(.5f, .195f), Vector2.zero, new Vector2(340, 40), new Color(Burgundy.r, Burgundy.g, Burgundy.b, .96f), panelSprite);
            toastGroup = toastRt.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha = 0;
            toastText = Txt("Refusal text", toastRt, "", new Vector2(.5f, .5f), Vector2.zero, 13, Cream, 326, 36);
            toastText.textWrappingMode = TextWrappingModes.Normal;

            armHintRt = Img("Tonearm hint", overlayRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(84, 84), Cyan, ringSprite);
            armHintGroup = armHintRt.gameObject.AddComponent<CanvasGroup>(); armHintGroup.alpha = 0;
            armHintText = Txt("Tonearm hint text", armHintRt, "", new Vector2(0f, .5f), new Vector2(-70, 0), 14, Cream, 130, 44);
            armHintText.outlineColor = Night; armHintText.outlineWidth = .3f; armHintText.textWrappingMode = TextWrappingModes.Normal;

            notchHintRt = Img("Notch hint", overlayRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(60, 60), Cyan, ringSprite);
            notchHintGroup = notchHintRt.gameObject.AddComponent<CanvasGroup>(); notchHintGroup.alpha = 0;
            ghostRt = Img("Ghost bridge", overlayRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(18, 40), new Color(Nickel.r, Nickel.g, Nickel.b, .85f), bridgeSprite);
            ghostGroup = ghostRt.gameObject.AddComponent<CanvasGroup>(); ghostGroup.alpha = 0;
            for (int i = 0; i < ghostDots.Length; i++)
            {
                var d = Img("Ghost path dot", overlayRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(7, 7), new Color(Cyan.r, Cyan.g, Cyan.b, .8f), roundSprite);
                ghostDots[i] = d.GetComponent<Image>(); ghostDots[i].enabled = false;
            }
            rippleRt = Img("Tap ripple", overlayRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(26, 26), Cyan, ringSprite);
            rippleImage = rippleRt.GetComponent<Image>(); rippleImage.enabled = false;
        }

        void LayoutUi(bool land, bool tabletPortrait)
        {
            if (!land)
            {
                SetAnchor(hudRt, new Vector2(.5f, 1f), new Vector2(-14, -24), new Vector2(330, 40));
                SetAnchor(cardRt, new Vector2(.5f, .846f), Vector2.zero, new Vector2(366, 112));
                SetAnchor(guideRt, new Vector2(.5f, .758f), Vector2.zero, new Vector2(352, 32));
                // 태블릿에서는 32 기준 px(실화면 약 67px) 올려 통과 하단 안내문을
                // safe area 안에 고정한다. 모바일의 기존 손가락 동선은 유지한다.
                // 820×1180에서는 하단 라벨까지 포함한 통 전체를 safe area 안으로 올린다.
                SetAnchor(binRt, new Vector2(.3f, .1f), tabletPortrait ? new Vector2(0, 76) : Vector2.zero, new Vector2(196, 100));
                SetAnchor(feedbackRt, new Vector2(.5f, .232f), Vector2.zero, new Vector2(352, 76));
                SetAnchor(toastRt, new Vector2(.5f, .315f), Vector2.zero, new Vector2(340, 40));
                SetAnchor(endCardRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 390));
            }
            else
            {
                SetAnchor(hudRt, new Vector2(.5f, 1f), new Vector2(0, -22), new Vector2(380, 40));
                SetAnchor(cardRt, new Vector2(.165f, .64f), Vector2.zero, new Vector2(224, 184));
                SetAnchor(guideRt, new Vector2(.165f, .385f), Vector2.zero, new Vector2(224, 48));
                SetAnchor(binRt, new Vector2(.86f, .2f), Vector2.zero, new Vector2(170, 96));
                SetAnchor(feedbackRt, new Vector2(.165f, .2f), Vector2.zero, new Vector2(224, 104));
                SetAnchor(toastRt, new Vector2(.5f, .07f), Vector2.zero, new Vector2(360, 36));
                SetAnchor(endCardRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(330, 390));
            }
            float w = cardRt.sizeDelta.x - 22;
            cardStructure.rectTransform.sizeDelta = new Vector2(w, land ? 36 : 22); cardStructure.fontSize = land ? 11.5f : 13f;
            cardStructure.textWrappingMode = land ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            cardPrompt.rectTransform.sizeDelta = new Vector2(w, land ? 76 : 50);
            cardRule.rectTransform.sizeDelta = new Vector2(w, land ? 30 : 20); cardRule.fontSize = land ? 10.5f : 12f;
            cardRule.textWrappingMode = land ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            cardStructure.rectTransform.anchorMin = cardStructure.rectTransform.anchorMax = new Vector2(.5f, land ? .84f : .83f);
            cardPrompt.rectTransform.anchorMin = cardPrompt.rectTransform.anchorMax = new Vector2(.5f, land ? .47f : .47f);
            cardRule.rectTransform.anchorMin = cardRule.rectTransform.anchorMax = new Vector2(.5f, land ? .12f : .13f);
            cardPrompt.fontSize = land ? 14 : 16;
            guideText.rectTransform.sizeDelta = new Vector2(guideRt.sizeDelta.x - 12, guideRt.sizeDelta.y - 4);
            guideText.textWrappingMode = TextWrappingModes.Normal;
            feedbackBig.rectTransform.sizeDelta = new Vector2(feedbackRt.sizeDelta.x - 12, 34);
            feedbackSmall.rectTransform.sizeDelta = new Vector2(feedbackRt.sizeDelta.x - 16, land ? 48 : 28);
            feedbackBig.fontSize = land ? 17 : 22; feedbackSmall.fontSize = land ? 11 : 12;
            feedbackSmall.rectTransform.anchorMin = feedbackSmall.rectTransform.anchorMax = new Vector2(.5f, land ? .3f : .24f);
            feedbackBig.rectTransform.anchorMin = feedbackBig.rectTransform.anchorMax = new Vector2(.5f, land ? .74f : .66f);
        }

        static void SetAnchor(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = offset; rt.sizeDelta = size;
        }

        void SetCard()
        {
            if (!cardPrompt || current == null) return;
            bool practice = current.id.StartsWith("practice");
            cardStructure.text = practice ? "연습 음반 · 직렬 링 2개" : current.structureLine;
            cardPrompt.text = practice
                ? "브리지로 <color=#F04E98><size=135%>6</size></color>개의 서로 다른 소리 경로를 만드시오."
                : "서로 다른 소리 경로가 <color=#F04E98><size=135%>" + current.target + "</size></color>가지가 되도록 브리지를 끼우시오.";
            cardRule.text = current.RuleLine;
            Punch(cardRt, .07f, .3f);
        }

        void SetScreen()
        {
            bool title = phase == RunPhase.Title, end = phase == RunPhase.End;
            SetGroup(titleGroup, title);
            SetGroup(playGroup, !title && !end && phase != RunPhase.Intro);
            SetGroup(endGroup, end);
            if (title || end || phase == RunPhase.Intro)
            {
                guideGroup.alpha = 0; feedbackGroup.alpha = 0; notchHintGroup.alpha = 0; ghostGroup.alpha = 0;
            }
            if ((phase == RunPhase.Practice || phase == RunPhase.Playing) && !correctionNote) feedbackGroup.alpha = 0;
            if (phase == RunPhase.Practice) { guideGroup.alpha = 1; guideClock = 0; }
            if (phase == RunPhase.Playing && st.solved == 0 && st.attempts == 0)
            {
                guideText.text = "각 링(면)의 노치에 브리지를 끼우고 톤암을 내리시오";
                guideGroup.alpha = 1; guideClock = 0;
            }
            else if (phase == RunPhase.Playing && st.misconceptionId == "") guideGroup.alpha = 0;
            if (title) titleBest.text = "최고 최초 정확 " + PlayerPrefs.GetInt("route-record.bestFirst", 0) + "/6";
        }

        void RefreshBin()
        {
            int left = Mathf.Max(0, currentBudget - st.bridgesUsed);
            binCount.text = left.ToString();
            for (int i = 0; i < binTokens.Length; i++)
            {
                bool on = i < left;
                binTokens[i].enabled = on;
                if (on) binTokens[i].color = (i >= currentBudget - st.carryBridges - st.bridgesUsed && st.carryBridges > 0 && !lastPractice) ? Magenta : Nickel;
            }
            binCarry.text = st.carryBridges > 0 && phase != RunPhase.Practice ? "+" + st.carryBridges + " 이월" : "";
        }

        void UpdateUi(float dt)
        {
            UpdatePunches(dt);
            if (toastClock > 0) { toastClock -= dt; toastGroup.alpha = Mathf.Clamp01(toastClock * 4f); }
            else if (toastGroup.alpha > 0) toastGroup.alpha = 0;
            if (rippleClock > 0)
            {
                rippleClock -= dt; rippleImage.enabled = true;
                float k = 1f - rippleClock / .4f; rippleRt.localScale = Vector3.one * (1f + k * 3f);
                rippleImage.color = new Color(Cyan.r, Cyan.g, Cyan.b, (1f - k) * .9f);
            }
            else if (rippleImage.enabled) rippleImage.enabled = false;

            if (phase == RunPhase.Title)
            {
                // 로고는 회전하는 라벨 위에 얹힌다
                logoRt.anchoredPosition = WorldToUi(recordRoot.position + new Vector3(0, .1f, 0)) + new Vector2(0, 8);
                float br = .5f + .5f * Mathf.Sin(worldClock * 2.2f);
                logoFill.color = new Color(Cyan.r, Cyan.g, Cyan.b, .0f);
                logoShadow.rectTransform.anchoredPosition = new Vector2(4 + Mathf.Sin(worldClock * 1.3f) * 1.5f, -5);
                logoRt.localScale = Vector3.one * (1f + .015f * br);
                ArmHint("톤암을 내려 재생", true);
            }
            else if (phase == RunPhase.Practice || phase == RunPhase.Playing)
            {
                UpdateInputTargets();
                UpdateHud(dt);
                UpdateOnboarding(dt);
            }
            else if (phase == RunPhase.Testing) { UpdateHud(dt); armHintGroup.alpha = 0; }
            else if (phase == RunPhase.End)
            {
                armHintGroup.alpha = 0;
                endClock += dt;
                float k = Mathf.Clamp01(endClock / 1.1f);
                endStats.text = "완성 재킷 " + Mathf.RoundToInt(endSolved * k) + "/6\n최초 정확 " + Mathf.RoundToInt(endFirst * k) + "/6\n점수 " + Mathf.RoundToInt(endScore * k);
                restartRt.localScale = Vector3.one * (1f + .04f * Mathf.Sin(worldClock * 4f));
            }
            else armHintGroup.alpha = 0;

            if (carryFlash > 0) { carryFlash -= dt; binCarry.color = Color.Lerp(Magenta, Color.white, Mathf.Abs(Mathf.Sin(carryFlash * 12f)) * .6f); }
        }

        void UpdateInputTargets()
        {
            if (Screen.width != lastScreenW || Screen.height != lastScreenH) { lastScreenW = Screen.width; lastScreenH = Screen.height; targetsDirty = true; }
            if (!targetsDirty || framingT < 1f || !Editable || current == null) return;
            targetsDirty = false;
            var sb = new System.Text.StringBuilder(256);
            AppendTarget(sb, "bin", RectTransformUtility.WorldToScreenPoint(null, binRt.position));
            AppendTarget(sb, "armFrom", cam.WorldToScreenPoint(ArmHeadWorld(ArmRestDeg, 0f)));
            AppendTarget(sb, "armTo", cam.WorldToScreenPoint(ArmHeadWorld(ArmPlayDeg, 1f)));
            for (int g = 0; g < current.groupCount; g++)
            for (int j = 0; j < current.maxima[g]; j++)
                AppendTarget(sb, "s" + g + "-" + j, cam.WorldToScreenPoint(recordRoot.parent.TransformPoint(new Vector3(0, RecordY, 0) + layout.SlotCenter(g, j, SurfaceLocal))));
            st.inputTargets = sb.ToString();
            MgfBridge.NotifyChanged();
        }

        static void AppendTarget(System.Text.StringBuilder sb, string key, Vector2 screen)
        {
            if (sb.Length > 0) sb.Append('|');
            sb.Append(key).Append(':').Append((screen.x / Mathf.Max(1, Screen.width)).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture))
              .Append(',').Append((1f - screen.y / Mathf.Max(1, Screen.height)).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
        }

        void UpdateHud(float dt)
        {
            scoreShown = Mathf.MoveTowards(scoreShown, st.score, dt * Mathf.Max(150f, Mathf.Abs(st.score - scoreShown) * 5f));
            scoreText.text = Mathf.RoundToInt(scoreShown).ToString();
            jacketText.text = phase == RunPhase.Practice || (phase == RunPhase.Testing && testPractice) ? "연습" : "재킷 " + st.solved + "/6";
            int left = Mathf.Max(0, RouteRecordRules.SessionSeconds - Mathf.FloorToInt(activeClock));
            timerText.text = (phase == RunPhase.Practice || testPractice && phase == RunPhase.Testing) ? "시간 정지" : left + "초";
            float frac = Mathf.Clamp01(1f - activeClock / RouteRecordRules.SessionSeconds);
            timerFillRt.sizeDelta = new Vector2(110f * frac, 6);
            timerFillRt.GetComponent<Image>().color = frac < .25f ? Amber : Cyan;
            for (int i = 0; i < 3; i++) needleIcons[i].color = i < st.needles ? Nickel : new Color(Nickel.r, Nickel.g, Nickel.b, .14f);
            cardIn = Mathf.MoveTowards(cardIn, 1, dt * 4f);
        }

        void UpdateOnboarding(float dt)
        {
            guideClock += dt;
            if (guideGroup.alpha > 0 && phase == RunPhase.Playing && guideClock > 6f && st.misconceptionId == "") guideGroup.alpha = Mathf.MoveTowards(guideGroup.alpha, 0, dt);
            bool practice = phase == RunPhase.Practice;
            // 연습: 빈 노치를 순서대로 가리키고, 다 채우면 톤암을 가리킨다.
            int ng = -1, nj = -1;
            if (practice)
            {
                for (int g = 0; g < current.groupCount && ng < 0; g++)
                for (int j = 0; j < current.maxima[g]; j++) if (!placed[g, j]) { ng = g; nj = j; break; }
                guideText.text = ng >= 0 ? "빛나는 노치를 눌러 브리지를 끼우시오 (링1에 2개, 링2에 3개)" : "톤암 머리를 음반 쪽으로 끌어 내리시오";
            }
            if (ng >= 0)
            {
                notchHintGroup.alpha = 1;
                var w = recordRoot.TransformPoint(layout.SlotCenter(ng, nj, SurfaceLocal));
                notchHintRt.anchoredPosition = WorldToUi(w);
                float big = idleGuide > 8f ? 1.35f : 1f;
                notchHintRt.localScale = Vector3.one * big * (1f + .18f * Mathf.Sin(worldClock * 6f));
                // 연습이 끝날 때까지 매 다음 노치에 「통 → 노치」 경로를 유지한다.
                // 첫 브리지 뒤 안내가 사라져 순진 첫 플레이가 멈추던 문제를 막는다.
                bool demo = practice;
                ghostGroup.alpha = demo ? 1 : 0;
                Vector2 from = UiOf(binRt), to = notchHintRt.anchoredPosition;
                float c = Mathf.Repeat(worldClock / 1.8f, 1f);
                float e = c * c * (3 - 2 * c);
                ghostRt.anchoredPosition = Vector2.Lerp(from, to, e) + new Vector2(0, Mathf.Sin(e * Mathf.PI) * 40f);
                ghostRt.localScale = Vector3.one * big * 1.3f;
                for (int i = 0; i < ghostDots.Length; i++)
                {
                    ghostDots[i].enabled = demo;
                    if (!demo) continue;
                    float f = Mathf.Repeat(i / (float)ghostDots.Length + worldClock * .5f, 1f);
                    ghostDots[i].rectTransform.anchoredPosition = Vector2.Lerp(from, to, f) + new Vector2(0, Mathf.Sin(f * Mathf.PI) * 40f);
                }
                armHintGroup.alpha = 0;
            }
            else
            {
                notchHintGroup.alpha = 0; ghostGroup.alpha = 0;
                for (int i = 0; i < ghostDots.Length; i++) ghostDots[i].enabled = false;
                bool showArm = practice || (st.bridgesUsed > 0 && idleGuide > 5f) || (phase == RunPhase.Playing && st.solved == 0 && st.attempts == 0 && st.bridgesUsed > 0);
                if (showArm) ArmHint(practice ? "끌어 내리기" : "톤암을 내려 시험", true);
                else armHintGroup.alpha = 0;
            }
        }

        void ArmHint(string text, bool on)
        {
            armHintGroup.alpha = on ? 1 : 0;
            if (!on) return;
            armHintRt.anchoredPosition = WorldToUi(ArmHeadWorld(armDeg, armDrop));
            armHintRt.localScale = Vector3.one * (1f + .14f * Mathf.Sin(worldClock * 5f));
            if (armHintText.text != text) armHintText.text = text;
        }

        // ── 입력 판정 도우미

        bool ScreenToRecord(Vector2 screen, out Vector3 local)
        {
            var ray = cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, new Vector3(0, RecordY + SurfaceLocal, 0));
            float d;
            if (plane.Raycast(ray, out d)) { local = recordRoot.InverseTransformPoint(ray.GetPoint(d)); local.y = SurfaceLocal; return true; }
            local = Vector3.zero; return false;
        }

        bool SlotAt(Vector2 screen, out int g, out int j)
        {
            g = -1; j = -1;
            Vector3 local;
            if (!ScreenToRecord(screen, out local)) return false;
            return layout.NearestSlot(local, .8f, out g, out j);
        }

        bool OnRecord(Vector2 screen)
        {
            Vector3 local;
            return ScreenToRecord(screen, out local) && new Vector2(local.x, local.z).magnitude < RecordLayout.RecordRadius;
        }

        bool HitTitleStart(Vector2 screen)
        {
            Vector2 head = cam.WorldToScreenPoint(ArmHeadWorld(armDeg, armDrop));
            if ((head - screen).magnitude < 90f * UiScale) return true;
            return OnRecord(screen);
        }

        bool HitTonearm(Vector2 screen)
        {
            Vector2 head = cam.WorldToScreenPoint(ArmHeadWorld(armDeg, armDrop));
            if ((head - screen).magnitude < 46f * UiScale) return true;
            Vector2 mid = cam.WorldToScreenPoint(Vector3.Lerp(boothRoot.TransformPoint(ArmPivot), ArmHeadWorld(armDeg, armDrop), .7f));
            return (mid - screen).magnitude < 30f * UiScale;
        }

        bool HitBin(Vector2 screen) { return RectTransformUtility.RectangleContainsScreenPoint(binRt, screen, null); }
        bool HitRestart(Vector2 screen) { return RectTransformUtility.RectangleContainsScreenPoint(restartRt, screen, null); }

        // ── 톤암 끌기: 휴식 위치 → 진입점 사이 호를 따라 내려놓는다.
        void GrabTonearm()
        {
            armHeld = true; armPullT = 0;
            armScreenRest = cam.WorldToScreenPoint(ArmHeadWorld(ArmRestDeg, 0f));
            armScreenPlay = cam.WorldToScreenPoint(ArmHeadWorld(ArmPlayDeg, 1f));
            MgfSfx.Play("tap", .12f);
        }

        void PullTonearm(Vector2 screen)
        {
            Vector2 ab = armScreenPlay - armScreenRest;
            float t = Mathf.Clamp01(Vector2.Dot(screen - armScreenRest, ab) / Mathf.Max(1f, ab.sqrMagnitude));
            armPullT = t;
            armDeg = Mathf.Lerp(ArmRestDeg, ArmPlayDeg, t);
            armDrop = t * .8f;
        }

        bool ReleaseTonearm()
        {
            armHeld = false;
            bool ok = armPullT >= .6f;
            if (!ok) { armTargetDeg = ArmRestDeg; armTargetDrop = 0; }
            return ok;
        }

        // ── 브리지 끌기
        void ShowDragBridge(bool on)
        {
            if (dragBridgeT.gameObject.activeSelf != on) dragBridgeT.gameObject.SetActive(on);
            if (!on) snapRing.gameObject.SetActive(false);
        }

        void MoveDragBridge(Vector2 screen)
        {
            Vector3 local;
            if (!ScreenToRecord(screen, out local)) return;
            int g, j;
            bool snap = layout.NearestSlot(local, .62f, out g, out j);
            snapRing.gameObject.SetActive(snap);
            float deg;
            if (snap)
            {
                snapRing.localPosition = layout.SlotCenter(g, j, SurfaceLocal + .02f);
                snapRing.localScale = Vector3.one * (1.0f + .12f * Mathf.Sin(worldClock * 14f));
                deg = layout.slotDeg[g, j];
            }
            else deg = Mathf.Atan2(local.z, local.x) * Mathf.Rad2Deg;
            var world = recordRoot.TransformPoint(local + new Vector3(0, .55f, 0));
            dragBridgeT.position = world;
            dragBridgeT.rotation = recordRoot.rotation * Quaternion.Euler(0, -deg, Mathf.Sin(worldClock * 9f) * 4f);
        }

        void LiftFromBin() { Punch(binRt, .08f, .2f); }
        void RefuseBin() { Punch(binRt, .16f, .3f); MgfSfx.Play("tap", .1f); }
        void PointAtBin() { Punch(binRt, .14f, .35f); }
        void ReturnToBin() { Punch(binRt, .1f, .25f); MgfSfx.Play("tap", .08f); }
        void PickBridge(int g, int j) { MgfSfx.Play("tap", .12f); }
        void HideBridge(int g, int j) { if (!bridgeHidden[g, j]) { bridgeHidden[g, j] = true; RefreshBridges(); } }
        void ShowBridgeAgain(int g, int j) { bridgeHidden[g, j] = false; RefreshBridges(); bridgeSnap[g, j] = .3f; }
        void WiggleBridge(int g, int j) { socketWiggle[g, j] = .35f; MgfSfx.Play("tap", .1f); }
        void WiggleSocket(int g, int j) { socketWiggle[g, j] = .35f; }

        void BridgeSnapped(int g, int j, bool viaTap)
        {
            if (correctionNote) { correctionNote = false; feedbackGroup.alpha = 0; }
            bridgeHidden[g, j] = false;
            RefreshBridges();
            bridgeSnap[g, j] = .3f;
            labelGlow = Mathf.Max(labelGlow, .2f);
            MgfSfx.Play("pop", .2f);
            if (viaTap) Punch(binRt, .06f, .2f);
        }

        void BridgeReturned(int g, int j) { bridgeHidden[g, j] = false; ReturnToBin(); if (correctionNote) { correctionNote = false; feedbackGroup.alpha = 0; } }

        void FlashNearestSlot(Vector2 screen)
        {
            Vector3 local;
            if (!ScreenToRecord(screen, out local)) return;
            int g, j;
            if (layout.NearestSlot(local, 99f, out g, out j)) socketWiggle[g, j] = .35f;
        }

        void Refuse(string message)
        {
            if (toastText.text != message) toastText.text = message;
            toastClock = 1.7f; toastGroup.alpha = 1;
            Punch(toastRt, .06f, .2f);
            MgfSfx.Play("tap", .06f);
        }

        void PointAtTonearm() { Punch(armHintRt, .3f, .35f); }
        void PulseRestart() { Punch(restartRt, .14f, .3f); }

        void SpawnRipple(Vector2 screen)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRt, screen, null, out local);
            rippleRt.anchoredPosition = local; rippleRt.localScale = Vector3.one; rippleClock = .4f;
        }

        Vector2 WorldToUi(Vector3 world)
        {
            Vector2 screen = cam.WorldToScreenPoint(world);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRt, screen, null, out local);
            return local;
        }

        Vector2 UiOf(RectTransform rt)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rt.position);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRt, screen, null, out local);
            return local;
        }

        // ── 펀치(스케일 기준값을 한 번만 저장 — 연속 펀치에도 크기가 새지 않는다)
        readonly Transform[] punchT = new Transform[16];
        readonly Vector3[] punchBase = new Vector3[16];
        readonly float[] punchClock = new float[16], punchDur = new float[16], punchAmt = new float[16];

        void Punch(Transform t, float amount, float duration)
        {
            if (!t) return;
            int free = -1;
            for (int i = 0; i < punchT.Length; i++)
            {
                if (punchT[i] == t) { punchClock[i] = 0; punchDur[i] = duration; punchAmt[i] = amount; return; }
                if (free < 0 && punchT[i] == null) free = i;
            }
            if (free < 0) return;
            punchT[free] = t; punchBase[free] = t.localScale; punchClock[free] = 0; punchDur[free] = duration; punchAmt[free] = amount;
        }

        void UpdatePunches(float dt)
        {
            for (int i = 0; i < punchT.Length; i++)
            {
                var t = punchT[i];
                if (!t) { punchT[i] = null; continue; }
                punchClock[i] += dt;
                float k = punchClock[i] / punchDur[i];
                if (k >= 1f) { t.localScale = punchBase[i]; punchT[i] = null; continue; }
                t.localScale = punchBase[i] * (1f + punchAmt[i] * Mathf.Sin(k * Mathf.PI) * (1f - k * .5f));
            }
        }

        // ── UI 빌더

        static void SetGroup(CanvasGroup g, bool on)
        {
            g.alpha = on ? 1 : 0; g.interactable = on; g.blocksRaycasts = false;
            if (g.gameObject.activeSelf != on) g.gameObject.SetActive(on);
        }

        static RectTransform Full(string name, Transform parent)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; return rt;
        }

        static RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = offset; rt.sizeDelta = size; return rt;
        }

        static RectTransform Img(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size, Color color, Sprite sprite)
        {
            var rt = R(name, parent, anchor, offset, size); var image = rt.gameObject.AddComponent<Image>(); image.sprite = sprite; image.type = sprite != null && sprite.border.x > 0 ? Image.Type.Sliced : Image.Type.Simple; image.color = color; image.raycastTarget = false; return rt;
        }

        static TextMeshProUGUI Txt(string name, Transform parent, string value, Vector2 anchor, Vector2 offset, float size, Color color, float width, float height)
        {
            var rt = R(name, parent, anchor, offset, new Vector2(width, height));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color;
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap; t.text = value; return t;
        }

        static Sprite MakeSprite(bool ring, int size, int radius, bool sliced)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "Route ring" : "Route round", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Abs(x - (size - 1) * .5f) - (size * .5f - radius));
                float dy = Mathf.Max(0, Mathf.Abs(y - (size - 1) * .5f) - (size * .5f - radius));
                float outside = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius + .5f - outside);
                if (ring) alpha *= Mathf.Clamp01(outside - (radius - 4.5f));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            float border = sliced ? radius : 0;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Sprite MakeBridgeSprite()
        {
            const int w = 24, h = 56;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Nickel bridge clip", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float nx = Mathf.Abs(x - 11.5f) / 11.5f, ny = Mathf.Abs(y - 27.5f) / 27.5f;
                bool body = nx < .55f && ny < .96f || (ny > .72f && ny < .96f && nx < .95f);
                bool stripe = nx < .2f && ny < .45f;
                byte v = (byte)(body ? 200 - (int)(nx * 60) : 0);
                px[y * w + x] = stripe ? new Color32(240, 120, 180, 255) : new Color32(v, v, (byte)Mathf.Min(255, v + 10), (byte)(body ? 255 : 0));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 56f);
        }

        static Sprite MakeNeedleSprite()
        {
            const int w = 16, h = 40;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Needle tip", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float half = y < 12 ? (y / 12f) * 3f + .5f : 5.5f;
                bool body = Mathf.Abs(x - 7.5f) < half;
                px[y * w + x] = new Color32(255, 255, 255, (byte)(body ? 255 : 0));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 40f);
        }
    }
}
