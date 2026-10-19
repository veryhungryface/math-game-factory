// 달팽이 극장 — 코드로 만든 밝은 2.5D 껍데기 정원과 잎 객석 UI/연출.
// 종이·직물·잎·껍데기 성장층과 살아 있는 달팽이 관객이 화면과 정답 사건을 만든다.
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.DalpaengiGeukjang
{
    public partial class DalpaengiGame
    {
        static readonly Color Rose = MgfLook.Hex("#E89A91");
        static readonly Color RoseLight = MgfLook.Hex("#F7C7B5");
        static readonly Color RoseDeep = MgfLook.Hex("#A94F68");
        static readonly Color Leaf = MgfLook.Hex("#6FA45B");
        static readonly Color LeafLight = MgfLook.Hex("#B8D976");
        static readonly Color Ivory = MgfLook.Hex("#FFF5D8");
        static readonly Color Ink = MgfLook.Hex("#4B3548");
        static readonly Color Amber = MgfLook.Hex("#F4B85F");
        static readonly Color Plum = MgfLook.Hex("#7B668B");

        Camera cam;
        Transform worldRoot, shellRoot, stageRoot, seatRoot, titleIrisRoot;
        Transform handleRoot, bellRoot, flowerSign, pulseRing, guideHand, guideArrow;
        Transform musicCylinder, ticketStripRoot;
        readonly Transform[] seats = new Transform[DalpaengiRules.MaxSeats];
        readonly Transform[] seatBacks = new Transform[DalpaengiRules.MaxSeats];
        readonly Transform[] chamberShutters = new Transform[DalpaengiRules.MaxSeats];
        readonly Transform[] ticketPunches = new Transform[DalpaengiRules.MaxSeats];
        readonly Transform[] seatSnails = new Transform[DalpaengiRules.MaxSeats];
        readonly Transform[] savedTicketTabs = new Transform[DalpaengiRules.Goal];
        readonly Transform[] shellRidges = new Transform[5];
        readonly Transform[] titleSnails = new Transform[3];
        Light stageGlow;
        Material roseMat, roseLightMat, roseDeepMat, leafMat, leafLightMat, ivoryMat, amberMat,
            inkMat, snailBodyMat, snailShellMat, ghostMat, crackMat;
        int visibleSeatTarget;
        float worldClock;
        float guideBoost;
        float revealProgress;
        float wrongFlash;
        float correctFlash;
        float drumWave = -1f;
        float seatCounterPunch;
        int previousCounter;
        bool lastLand;
        int lastScreenW = -1, lastScreenH = -1;

        RectTransform uiRoot, titleRoot, gameRoot, revealRoot, endRoot;
        CanvasGroup titleGroup, gameGroup, revealGroup, endGroup;
        Image promptPlate, goalPlate, countPlate, noticePlate, titlePetalPlate, endPlate, revealPlate;
        Image practiceEquationPlate, practiceTargetNotch, practiceGuideRail;
        readonly Image[] practiceTickets = new Image[5];
        TextMeshProUGUI titleLogo, titleSub, titleCta, titleMeta;
        TextMeshProUGUI goalText, bandText, promptText, conditionText, progressText, timerText;
        TextMeshProUGUI countText, countGhostText, handleLabel, noticeText;
        TextMeshProUGUI revealTitle, revealFormula, revealReason;
        TextMeshProUGUI practiceEquationText, practiceTargetText;
        TextMeshProUGUI endTitle, endStats, endReasonText, restartText;
        Sprite roundedSprite, leafSprite, petalSprite, ringSprite;
        CanvasScaler uiScaler;
        RectTransform invalidTapUi, invalidPathUi, assistZoneUi;
        Vector2 invalidGuideStart;
        float invalidGuideClock;
        float noticeClock;
        int shownSecond = -1;
        int resultCount;
        float resultCountFloat;

        void BuildWorld()
        {
            // CreatePrimitive가 내부적으로 붙이는 콜라이더 타입이 WebGL 엔진 코드 스트리핑으로
            // 제거되지 않게 명시 참조한다. 실제 판정은 화면 좌표 기반이다.
            var colliderTypes = new GameObject("Primitive collider type keeper");
            colliderTypes.AddComponent<SphereCollider>();
            colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<BoxCollider>();
            colliderTypes.AddComponent<MeshCollider>();
            UnityEngine.Object.Destroy(colliderTypes);
            // 광택 금속이 아닌 분필빛 종이·천·잎·껍데기 재질로 통일한다.
            roseMat = MgfLook.Lit(Rose, .08f, .01f);
            roseLightMat = MgfLook.Lit(RoseLight, .10f, .01f);
            roseDeepMat = MgfLook.Lit(RoseDeep, .12f, .02f);
            leafMat = MgfLook.Lit(Leaf, .10f, .01f);
            leafLightMat = MgfLook.Lit(LeafLight, .12f, .01f, LeafLight * .015f);
            ivoryMat = MgfLook.Lit(Ivory, .06f, .0f);
            amberMat = MgfLook.Lit(Amber, .10f, .01f, Amber * .02f);
            inkMat = MgfLook.Lit(Ink, .08f, .01f);
            snailBodyMat = MgfLook.Lit(MgfLook.Hex("#C7DEA0"), .10f, .01f);
            snailShellMat = MgfLook.Lit(MgfLook.Hex("#D56F72"), .12f, .01f);
            ghostMat = MgfLook.Alpha(new Color(1f, .88f, .60f, .48f));
            crackMat = MgfLook.Unlit(Ivory);

            MgfLook.Sky(MgfLook.Hex("#BFE7DF"), MgfLook.Hex("#F7D8C7"), MgfLook.Hex("#FFF3D2"), .68f);
            MgfLook.Sun(new Vector3(38f, -28f, -12f), MgfLook.Hex("#FFF0BE"), 1.22f, .42f);
            cam = MgfLook.Camera(new Vector3(0f, 8.2f, -19.5f), new Vector3(0f, 2.1f, 0f), 38f);
            cam.orthographic = true;
            cam.orthographicSize = 10.2f;
            cam.backgroundColor = MgfLook.Hex("#CBE9DE");

            worldRoot = new GameObject("Sunlit shell garden amphitheatre").transform;
            shellRoot = new GameObject("Soft shell growth layers").transform;
            shellRoot.SetParent(worldRoot, false);
            stageRoot = new GameObject("Leaf terraces and cloth stage").transform;
            stageRoot.SetParent(worldRoot, false);
            seatRoot = new GameObject("Folding leaf seat fan").transform;
            seatRoot.SetParent(worldRoot, false);

            // 풀 언덕과 커다란 분홍 껍데기가 실제 극장 공간을 화면 가득 만든다.
            MgfLook.Block("Moss garden mound", new Vector3(0f, -.42f, 2.3f),
                new Vector3(17.4f, 1.15f, 7.4f), .46f, leafMat, worldRoot);
            var shellBack = MgfLook.Prim(PrimitiveType.Cylinder, "Powdery rose shell theatre",
                new Vector3(-.35f, 3.55f, 3.3f), new Vector3(13.0f, 1.0f, 13.0f), roseMat, shellRoot, false);
            shellBack.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shellBack.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 0; i < shellRidges.Length; i++)
            {
                float r = 2.25f + i * 1.66f;
                var ridge = NewMeshObject("Spiral growth ridge " + i, Torus(r, .11f + i * .018f, 64, 8),
                    i % 2 == 0 ? roseLightMat : roseDeepMat, shellRoot);
                ridge.localPosition = new Vector3(-.5f + i * .08f, 3.9f, 1.62f - i * .04f);
                ridge.localScale = new Vector3(1.18f, .92f, 1f);
                shellRidges[i] = ridge;
            }
            // 나선 중심의 진주와 성장선은 기계 톱니 대신 살아 있는 껍데기 결을 만든다.
            for (int i = 0; i < 9; i++)
            {
                float a = i * .78f;
                float r = .22f + i * .22f;
                var bead = MgfLook.Prim(PrimitiveType.Sphere, "Fine spiral bead " + i,
                    new Vector3(-.6f + Mathf.Cos(a) * r, 4.0f + Mathf.Sin(a) * r, 1.45f),
                    Vector3.one * (.22f + i * .025f), roseDeepMat, shellRoot, false);
                bead.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            for (int i = 0; i < 13; i++)
            {
                float a = Mathf.Lerp(-148f, 126f, i / 12f) * Mathf.Deg2Rad;
                float len = 2.2f + (i % 4) * .62f;
                var vein = MgfLook.Prim(PrimitiveType.Cube, "Radial growth seam " + i,
                    new Vector3(-.55f + Mathf.Cos(a) * len * .54f, 4f + Mathf.Sin(a) * len * .54f, 1.05f),
                    new Vector3(.055f, len, .08f), i % 3 == 0 ? ivoryMat : roseDeepMat, shellRoot, false);
                vein.transform.localRotation = Quaternion.Euler(0f, 0f, -a * Mathf.Rad2Deg + 90f);
                vein.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            // 접힌 잎 테라스와 꽃잎 지지대가 껍데기 안쪽을 가로지른다.
            for (int i = 0; i < 4; i++)
            {
                float y = -.05f + i * 1.35f;
                float x = i % 2 == 0 ? -.95f : .75f;
                var passage = MgfLook.Prim(PrimitiveType.Cube, "Folded leaf terrace " + i,
                    new Vector3(x, y, .7f - i * .08f), new Vector3(8.9f - i * .72f, .14f, 1.25f),
                    i % 2 == 0 ? roseLightMat : roseDeepMat, stageRoot, false);
                passage.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 5f : -7f);
                for (int v = -3; v <= 3; v++)
                {
                    var vein = MgfLook.Prim(PrimitiveType.Cube, "Leaf terrace vein " + i + " " + v,
                        new Vector3(x + v * .78f, y + .11f, .02f), new Vector3(.07f, .22f, 1.36f),
                        amberMat, stageRoot, false);
                    vein.transform.localRotation = passage.transform.localRotation;
                }
            }

            // 제목은 잎줄기에 매단 천 현수막에 놓는다.
            flowerSign = MgfLook.Block("Soft theatre banner", new Vector3(-.55f, 6.15f, -.1f),
                new Vector3(5.9f, 1.18f, .12f), .18f, roseDeepMat, stageRoot).transform;
            flowerSign.localRotation = Quaternion.Euler(0f, 0f, -7f);
            for (int side = -1; side <= 1; side += 2)
            {
                var stem = MgfLook.Prim(PrimitiveType.Cylinder, "Banner vine " + side,
                    new Vector3(-.55f + side * 2.62f, 6.72f, .02f), new Vector3(.08f, .72f, .08f),
                    leafMat, stageRoot, false);
                stem.transform.localRotation = Quaternion.Euler(0f, 0f, side * 9f);
            }
            var signText = MgfText.World("달팽이 극장", new Vector3(-.55f, 6.15f, -.31f), .66f, Ivory, stageRoot);
            signText.fontStyle = FontStyles.Bold;
            signText.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            BuildSeats();
            BuildHandleAndBell();
            BuildTitleIris();
            BuildAmbientSnails();
            BuildTicketMusicBox();

            stageGlow = new GameObject("Warm garden firefly light").AddComponent<Light>();
            stageGlow.type = LightType.Point;
            stageGlow.transform.position = new Vector3(0f, 4.6f, -2.4f);
            stageGlow.color = MgfLook.Hex("#FFE08A");
            stageGlow.range = 13f;
            stageGlow.intensity = 1.05f;
            stageGlow.shadows = LightShadows.None;
            stageGlow.renderMode = LightRenderMode.ForceVertex;
        }

        void BuildSeats()
        {
            for (int i = 0; i < seats.Length; i++)
            {
                // 작은 답도 화면 중앙에서 달팽이 얼굴이 읽히도록 중앙→좌우 순서로 객석을 펼친다.
                int visualSlot = i == 0 ? 8 : (i % 2 == 1 ? 8 - (i + 1) / 2 : 8 + i / 2);
                float t = visualSlot / 16f;
                float angle = Mathf.Lerp(-76f, 76f, t) * Mathf.Deg2Rad;
                float radius = 4.0f + .65f * Mathf.Sin(t * Mathf.PI);
                Vector3 p = new Vector3(Mathf.Sin(angle) * radius, .48f + .18f * Mathf.Cos(angle),
                    -1.10f + Mathf.Cos(angle) * 2.4f);
                var root = new GameObject("Leaf seat " + (i + 1)).transform;
                root.SetParent(seatRoot, false);
                root.localPosition = p;
                root.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                seats[i] = root;

                var pad = MgfLook.Prim(PrimitiveType.Cube, "Layered leaf cushion", Vector3.zero,
                    new Vector3(.72f, .09f, .86f), leafMat, root, false);
                pad.transform.localPosition = new Vector3(0f, .02f, .04f);
                pad.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 4f : -4f);
                var back = MgfLook.Prim(PrimitiveType.Cube, "Folding leaf seat back", new Vector3(0f, .36f, .30f),
                    new Vector3(.74f, .78f, .08f), leafLightMat, root, false).transform;
                back.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                seatBacks[i] = back;
                MgfLook.Prim(PrimitiveType.Cube, "Leaf midrib", new Vector3(0f, .38f, .245f),
                    new Vector3(.055f, .66f, .035f), roseDeepMat, root, false);

                // 각 자리 뒤 꽃잎 커튼은 정답 때 달팽이가 들어오며 차례로 열린다.
                var shutter = new GameObject("Petal seat curtain " + (i + 1)).transform;
                shutter.SetParent(root, false);
                shutter.localPosition = new Vector3(0f, .73f, .43f);
                var shutterPanel = MgfLook.Prim(PrimitiveType.Cube, "Folded petal curtain", Vector3.zero,
                    new Vector3(.64f, .72f, .055f), i % 2 == 0 ? roseLightMat : roseDeepMat, shutter, false);
                shutterPanel.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? -5f : 5f);
                MgfLook.Prim(PrimitiveType.Cube, "Petal seam", new Vector3(0f, 0f, -.04f),
                    new Vector3(.035f, .58f, .025f), leafLightMat, shutter, false);
                chamberShutters[i] = shutter;

                var audience = BuildSnail("Audience snail " + (i + 1), root,
                    Color.Lerp(MgfLook.Hex("#C9D99B"), MgfLook.Hex("#F2C78D"), (i % 5) / 4f));
                audience.localPosition = new Vector3(0f, .42f, -.52f);
                audience.localRotation = Quaternion.identity;
                audience.localScale = Vector3.one * .02f;
                seatSnails[i] = audience;

                // 의자 번호는 정답 힌트가 아니라 현재 조작 수의 물리적 눈금이다.
                var number = MgfText.World((i + 1).ToString(), new Vector3(0f, .62f, -.13f), .29f, Ivory, root);
                number.fontStyle = FontStyles.Bold;
                number.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            }
        }

        void BuildHandleAndBell()
        {
            handleRoot = new GameObject("Broad leaf unfurl handle").transform;
            handleRoot.SetParent(worldRoot, false);
            handleRoot.localPosition = new Vector3(0f, .45f, -5.45f);
            var handleLeaf = MgfLook.Block("Layered leaf handle", Vector3.zero,
                new Vector3(1.85f, .24f, 1.18f), .28f, leafMat, handleRoot);
            var handleKnob = MgfLook.Prim(PrimitiveType.Sphere, "Soft seed-pod grip", new Vector3(0f, .48f, -.05f),
                new Vector3(.62f, .50f, .62f), amberMat, handleRoot, true);
            handleKnob.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            MgfLook.Prim(PrimitiveType.Cylinder, "Leaf stem", new Vector3(0f, .25f, .0f),
                new Vector3(.12f, .55f, .12f), leafLightMat, handleRoot, false);
            pulseRing = NewMeshObject("Handle pulse ring", Torus(.92f, .07f, 48, 6), leafLightMat, handleRoot);
            pulseRing.localPosition = new Vector3(0f, .12f, -.50f);
            pulseRing.localRotation = Quaternion.Euler(72f, 0f, 0f);

            bellRoot = new GameObject("Flower chime CTA").transform;
            bellRoot.SetParent(worldRoot, false);
            bellRoot.localPosition = new Vector3(0f, .72f, -4.9f);
            var bell = MgfLook.Prim(PrimitiveType.Sphere, "Golden flower chime", Vector3.zero,
                new Vector3(.72f, .36f, .72f), amberMat, bellRoot, true);
            bell.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bell.transform.localPosition = new Vector3(0f, .05f, 0f);
            MgfLook.Prim(PrimitiveType.Cylinder, "Flower stem", new Vector3(0f, .88f, .0f),
                new Vector3(.18f, .65f, .18f), leafMat, bellRoot, false);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                var petal = MgfLook.Prim(PrimitiveType.Sphere, "Chime petal " + i,
                    new Vector3(Mathf.Cos(a) * .72f, Mathf.Sin(a) * .36f, .04f),
                    new Vector3(.52f, .28f, .16f), roseLightMat, bellRoot, false);
                petal.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            }
        }

        void BuildTitleIris()
        {
            titleIrisRoot = new GameObject("Breathing shell flower").transform;
            titleIrisRoot.SetParent(worldRoot, false);
            titleIrisRoot.localPosition = new Vector3(-.55f, 3.95f, -1.75f);
            var disk = MgfLook.Prim(PrimitiveType.Cylinder, "Shell flower heart", Vector3.zero,
                new Vector3(2.35f, .06f, 2.35f), roseDeepMat, titleIrisRoot, false);
            disk.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                var petal = MgfLook.Prim(PrimitiveType.Sphere, "Soft shell petal " + i,
                    new Vector3(Mathf.Cos(a) * 1.48f, Mathf.Sin(a) * 1.48f, 0f),
                    new Vector3(.92f, .45f, .13f), i % 2 == 0 ? roseLightMat : ivoryMat, titleIrisRoot, false);
                petal.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            }
            var hub = MgfLook.Prim(PrimitiveType.Cylinder, "Pearl flower centre", new Vector3(0f, 0f, -.08f),
                new Vector3(.62f, .06f, .62f), amberMat, titleIrisRoot, false);
            hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        void BuildAmbientSnails()
        {
            for (int i = 0; i < titleSnails.Length; i++)
            {
                var snail = BuildSnail("Title usher snail " + i, worldRoot,
                    Color.Lerp(MgfLook.Hex("#83948D"), MgfLook.Hex("#C1B57E"), i / 2f));
                snail.localPosition = i == 1
                    ? new Vector3(-3.05f, .84f, -2.8f)
                    : new Vector3(i == 0 ? 3.75f : 5.45f, .72f, -1.55f + i * .22f);
                snail.localScale = Vector3.one * (i == 1 ? 1.95f : .82f);
                titleSnails[i] = snail;
            }
        }

        void BuildTicketMusicBox()
        {
            ticketStripRoot = new GameObject("Shell percussion flower vine").transform;
            ticketStripRoot.SetParent(stageRoot, false);
            ticketStripRoot.localPosition = new Vector3(0f, 2.30f, -.78f);
            MgfLook.Block("Soft festival ribbon", Vector3.zero,
                new Vector3(8.7f, .34f, .06f), .16f, leafLightMat, ticketStripRoot);
            for (int i = 0; i < ticketPunches.Length; i++)
            {
                float x = -3.82f + i * .478f;
                var punch = MgfLook.Prim(PrimitiveType.Sphere, "Shell percussion bead " + (i + 1),
                    new Vector3(x, 0f, -.11f), new Vector3(.22f, .22f, .12f),
                    i % 2 == 0 ? roseDeepMat : amberMat,
                    ticketStripRoot, false).transform;
                punch.localScale = Vector3.one * .02f;
                ticketPunches[i] = punch;
            }

            musicCylinder = MgfLook.Prim(PrimitiveType.Sphere, "Big shell hand drum",
                new Vector3(-4.95f, 2.30f, -.72f), new Vector3(.66f, .78f, .66f),
                roseLightMat, stageRoot, false).transform;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * .25f;
                MgfLook.Prim(PrimitiveType.Sphere, "Drum flower bead " + i,
                    new Vector3(-4.95f, 2.30f + Mathf.Sin(angle) * .62f,
                        -.72f + Mathf.Cos(angle) * .62f), Vector3.one * .13f,
                    leafLightMat, stageRoot, false);
            }

            // 공연마다 꽃등 하나가 켜져 8공연의 누적 흔적이 된다.
            for (int i = 0; i < savedTicketTabs.Length; i++)
            {
                float x = -3.55f + i * 1.02f;
                var tab = MgfLook.Prim(PrimitiveType.Sphere, "Saved flower lantern " + (i + 1),
                    new Vector3(x, 5.03f + (i % 2) * .16f, -.42f),
                    new Vector3(.48f, .48f, .18f), i % 2 == 0 ? amberMat : roseLightMat,
                    stageRoot, false).transform;
                tab.localScale = Vector3.one * .18f;
                savedTicketTabs[i] = tab;
            }
        }

        Transform BuildSnail(string name, Transform parent, Color bodyColor)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var bodyMat = MgfLook.Lit(bodyColor, .10f, .01f);
            MgfLook.Prim(PrimitiveType.Sphere, "Plump soft body", new Vector3(0f, .20f, 0f),
                new Vector3(.55f, .44f, .78f), bodyMat, root, false);
            var stampedShell = MgfLook.Prim(PrimitiveType.Sphere, "Rounded spiral shell", new Vector3(0f, .55f, .20f),
                new Vector3(.75f, .20f, .75f), snailShellMat, root, false);
            stampedShell.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var shellPearl = MgfLook.Prim(PrimitiveType.Sphere, "Shell spiral pearl", new Vector3(0f, .55f, -.02f),
                new Vector3(.18f, .08f, .18f), amberMat, root, false);
            for (int side = -1; side <= 1; side += 2)
            {
                var stalk = MgfLook.Prim(PrimitiveType.Cylinder, "Eye stalk", new Vector3(side * .19f, .75f, -.36f),
                    new Vector3(.055f, .32f, .055f), bodyMat, root, false);
                stalk.transform.localRotation = Quaternion.Euler(22f, 0f, side * 8f);
                MgfLook.Prim(PrimitiveType.Sphere, "Round eye", new Vector3(side * .27f, 1.03f, -.47f),
                    Vector3.one * .13f, inkMat, root, false);
            }
            return root;
        }

        void BuildUi()
        {
            roundedSprite = MakeRoundedSprite(72, 18f);
            leafSprite = MakeLeafSprite(96);
            petalSprite = MakePetalSprite(96);
            ringSprite = MakeRingSprite(96);
            uiRoot = Full("Dalpaengi UI", MgfText.Canvas.transform);
            uiScaler = MgfText.Canvas.GetComponent<CanvasScaler>();

            titleRoot = Full("Title screen", uiRoot);
            titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
            titlePetalPlate = Img("Cloth title banner", titleRoot, new Vector2(.5f, .79f), Vector2.zero,
                new Vector2(350f, 174f), RoseDeep, roundedSprite);
            titleLogo = Txt("Logo", titleRoot, "달팽이\n극장", new Vector2(.5f, .80f), new Vector2(0f, 4f), 56f, Ivory, 330f);
            titleLogo.fontStyle = FontStyles.Bold;
            titleLogo.outlineColor = Ink;
            titleLogo.outlineWidth = .22f;
            titleLogo.lineSpacing = -24f;
            titleSub = Txt("Tagline", titleRoot, "잎 객석을 펼쳐 달팽이를 입장시켜라", new Vector2(.5f, .66f), Vector2.zero, 18f, Ivory, 350f);
            titleCta = Txt("World CTA", titleRoot, "꽃종을 울려 극장 열기", new Vector2(.5f, .18f), Vector2.zero, 23f, RoseDeep, 350f);
            titleCta.outlineColor = Ivory;
            titleCta.outlineWidth = .14f;
            titleMeta = Txt("Title meta", titleRoot, "중2 · 경우의 수     한 판 8공연", new Vector2(.5f, .095f), Vector2.zero, 12f, Ink, 360f);

            gameRoot = Full("Game HUD", uiRoot);
            gameGroup = gameRoot.gameObject.AddComponent<CanvasGroup>();
            goalPlate = Img("Woven goal ribbon", gameRoot, new Vector2(.5f, .955f), Vector2.zero,
                new Vector2(364f, 54f), RoseLight, roundedSprite);
            goalText = Txt("Goal", goalPlate.transform, "8공연 · 첫 제출 6회 · 꽃종 3", new Vector2(.5f, .5f),
                Vector2.zero, 15f, Ink, 342f);
            promptPlate = Img("Ivory story leaf", gameRoot, new Vector2(.5f, .82f), Vector2.zero,
                new Vector2(366f, 176f), Ivory, roundedSprite);
            bandText = Txt("Law label", promptPlate.transform, "합의 법칙", new Vector2(.5f, .83f), Vector2.zero, 16f, RoseDeep, 330f);
            promptText = Txt("Problem", promptPlate.transform, "", new Vector2(.5f, .48f), Vector2.zero, 17f, Ink, 334f);
            promptText.textWrappingMode = TextWrappingModes.Normal;
            promptText.overflowMode = TextOverflowModes.Overflow;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.rectTransform.sizeDelta = new Vector2(334f, 98f);
            conditionText = Txt("Condition", promptPlate.transform, "", new Vector2(.5f, .15f), Vector2.zero, 13f, Leaf, 334f);

            progressText = Txt("Progress", gameRoot, "공연 1/8", new Vector2(.14f, .675f), Vector2.zero, 14f, Ivory, 112f);
            timerText = Txt("Timer", gameRoot, "120초", new Vector2(.86f, .675f), Vector2.zero, 14f, Ivory, 100f);
            countPlate = Img("Shell window seat counter", gameRoot, new Vector2(.5f, .30f), Vector2.zero,
                new Vector2(148f, 108f), RoseDeep, roundedSprite);
            countGhostText = Txt("Old counter glyph", countPlate.transform, "0", new Vector2(.5f, .5f), Vector2.zero, 50f, Ivory, 100f);
            countText = Txt("Current seats", countPlate.transform, "0", new Vector2(.5f, .5f), Vector2.zero, 52f, Ivory, 100f);
            countText.outlineColor = Ink;
            countText.outlineWidth = .15f;
            handleLabel = Txt("Handle label", gameRoot, "잎을 누른 채 펼치고 · 놓아 입장", new Vector2(.5f, .115f), Vector2.zero, 16f, Ivory, 356f);
            handleLabel.outlineColor = Ink;
            handleLabel.outlineWidth = .13f;
            noticePlate = Img("Cloth instruction ribbon", gameRoot, new Vector2(.5f, .54f), Vector2.zero,
                new Vector2(348f, 54f), Plum, roundedSprite);
            noticeText = Txt("Notice", noticePlate.transform, "경우마다 한 자리, 펼쳐 입장시켜라", new Vector2(.5f, .5f),
                Vector2.zero, 14f, Ivory, 324f);
            FitInside(noticeText, 324f, 46f, 10.5f);
            FitInside(handleLabel, 356f, 34f, 12f);

            revealRoot = Full("Reveal", uiRoot);
            revealGroup = revealRoot.gameObject.AddComponent<CanvasGroup>();
            revealPlate = Img("Compact petal result", revealRoot, new Vector2(.5f, .81f), Vector2.zero,
                new Vector2(366f, 144f), new Color(Ivory.r, Ivory.g, Ivory.b, .97f), roundedSprite);
            revealTitle = Txt("Reveal title", revealPlate.transform, "", new Vector2(.5f, .75f), Vector2.zero, 24f, Ink, 330f);
            revealFormula = Txt("Formula", revealPlate.transform, "", new Vector2(.5f, .47f), Vector2.zero, 28f, RoseDeep, 330f);
            revealReason = Txt("Reason", revealPlate.transform, "", new Vector2(.5f, .17f), Vector2.zero, 13f, Ink, 330f);
            revealReason.textWrappingMode = TextWrappingModes.Normal;

            endRoot = Full("Result", uiRoot);
            endGroup = endRoot.gameObject.AddComponent<CanvasGroup>();
            endPlate = Img("Result flower marquee", endRoot, new Vector2(.5f, .58f), Vector2.zero,
                new Vector2(370f, 410f), Ivory, petalSprite);
            endTitle = Txt("Result title", endPlate.transform, "", new Vector2(.5f, .78f), Vector2.zero, 42f, Ink, 330f);
            endTitle.outlineColor = Rose;
            endTitle.outlineWidth = .16f;
            endStats = Txt("Result stats", endPlate.transform, "", new Vector2(.5f, .48f), Vector2.zero, 25f, RoseDeep, 330f);
            endReasonText = Txt("Result reason", endPlate.transform, "", new Vector2(.5f, .24f), Vector2.zero, 15f, Ink, 318f);
            endReasonText.textWrappingMode = TextWrappingModes.Normal;
            restartText = Txt("Restart CTA", endRoot, "꽃종을 울려 다시 열기", new Vector2(.5f, .17f), Vector2.zero, 22f, Ivory, 350f);
            restartText.outlineColor = Ink;
            restartText.outlineWidth = .18f;

            CreateGuideObjects();
            UpdateLayoutIfNeeded(true);
        }

        void CreateGuideObjects()
        {
            var ring = Img("Guide pulse ring", gameRoot, new Vector2(.5f, .13f), Vector2.zero,
                new Vector2(106f, 106f), new Color(1f, .90f, .50f, .85f), ringSprite);
            pulseRingUi = ring.rectTransform;
            var hand = Img("Ghost hand", gameRoot, new Vector2(.5f, .17f), Vector2.zero,
                new Vector2(42f, 64f), Ivory, leafSprite);
            guideHandUi = hand.rectTransform;
            var arrow = Img("Flow arrow", gameRoot, new Vector2(.5f, .205f), Vector2.zero,
                new Vector2(34f, 70f), LeafLight, leafSprite);
            guideArrowUi = arrow.rectTransform;
            invalidTapUi = Img("Missed tap locator", gameRoot, new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(72f, 72f), new Color(1f, .82f, .35f, .92f), ringSprite).rectTransform;
            invalidPathUi = Img("Miss-to-handle path", gameRoot, new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(8f, 120f), new Color(1f, .91f, .55f, .78f), roundedSprite).rectTransform;
            assistZoneUi = Img("Progressive handle catchment", gameRoot, new Vector2(.5f, .18f), Vector2.zero,
                new Vector2(350f, 168f), new Color(LeafLight.r, LeafLight.g, LeafLight.b, .10f), roundedSprite).rectTransform;
            practiceEquationPlate = Img("Sum practice decomposition", gameRoot, new Vector2(.5f, .60f), Vector2.zero,
                new Vector2(324f, 90f), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), roundedSprite);
            practiceEquationText = Txt("Sum practice formula", practiceEquationPlate.transform, "2 + 3 = 5",
                new Vector2(.5f, .70f), Vector2.zero, 25f, RoseDeep, 290f);
            for (int i = 0; i < practiceTickets.Length; i++)
            {
                float x = i < 2 ? -70f + i * 28f : 16f + (i - 2) * 28f;
                practiceTickets[i] = Img("Practice ticket " + (i + 1), practiceEquationPlate.transform,
                    new Vector2(.5f, .27f), new Vector2(x, 0f), new Vector2(20f, 28f),
                    i < 2 ? RoseLight : Amber, roundedSprite);
            }
            practiceGuideRail = Img("Handle to five rail", gameRoot, new Vector2(.5f, .30f), Vector2.zero,
                new Vector2(10f, 270f), new Color(LeafLight.r, LeafLight.g, LeafLight.b, .80f), roundedSprite);
            practiceTargetNotch = Img("Persistent target five notch", gameRoot, new Vector2(.5f, .43f), Vector2.zero,
                new Vector2(184f, 12f), Amber, roundedSprite);
            practiceTargetText = Txt("Target five label", gameRoot, "5에서 놓기",
                new Vector2(.84f, .255f), Vector2.zero, 16f, Amber, 110f);
            practiceTargetText.outlineColor = Ink;
            practiceTargetText.outlineWidth = .2f;
            // 목표 노치·경로·잡기 영역은 계수판 뒤로 — 노치가 계수 숫자를 가로지르지 않게 한다.
            int behindCounter = countPlate.transform.GetSiblingIndex();
            assistZoneUi.SetSiblingIndex(behindCounter);
            practiceGuideRail.transform.SetSiblingIndex(behindCounter + 1);
            practiceTargetNotch.transform.SetSiblingIndex(behindCounter + 2);
            invalidTapUi.gameObject.SetActive(false);
            invalidPathUi.gameObject.SetActive(false);
            assistZoneUi.gameObject.SetActive(false);
            SetPracticeScaffoldVisible(false);
        }

        void SetPracticeScaffoldVisible(bool visible)
        {
            if (practiceEquationPlate) practiceEquationPlate.gameObject.SetActive(visible);
            if (practiceGuideRail) practiceGuideRail.gameObject.SetActive(visible);
            if (practiceTargetNotch) practiceTargetNotch.gameObject.SetActive(visible);
            if (practiceTargetText) practiceTargetText.gameObject.SetActive(visible);
        }

        RectTransform pulseRingUi, guideHandUi, guideArrowUi;

        void ResetWorldForRun()
        {
            visibleSeatTarget = 0;
            revealProgress = 0f;
            drumWave = -1f;
            wrongFlash = 0f;
            correctFlash = 0f;
            guideBoost = 0f;
            for (int i = 0; i < seats.Length; i++)
            {
                seats[i].localScale = new Vector3(1f, .18f, 1f);
                seatBacks[i].localRotation = Quaternion.Euler(76f, 0f, 0f);
                chamberShutters[i].localRotation = Quaternion.identity;
                chamberShutters[i].localScale = Vector3.one;
                ticketPunches[i].localScale = Vector3.one * .02f;
                seatSnails[i].localScale = Vector3.one * .02f;
            }
            for (int i = 0; i < savedTicketTabs.Length; i++)
                savedTicketTabs[i].localScale = Vector3.one * .18f;
            if (titleIrisRoot)
            {
                titleIrisRoot.localScale = Vector3.one;
                titleIrisRoot.localRotation = Quaternion.identity;
            }
            if (bellRoot) bellRoot.gameObject.SetActive(true);
            if (handleRoot) handleRoot.gameObject.SetActive(false);
            invalidGuideClock = 0f;
            SetPracticeScaffoldVisible(false);
        }

        void ShowTitleVisual()
        {
            titleGroup.alpha = 1f;
            titleGroup.blocksRaycasts = false;
            gameGroup.alpha = 0f;
            revealGroup.alpha = 0f;
            endGroup.alpha = 0f;
            titleIrisRoot.gameObject.SetActive(true);
            titleIrisRoot.localPosition = new Vector3(-.55f, 3.95f, -1.75f);
            titleIrisRoot.localScale = Vector3.one;
            titleIrisRoot.localRotation = Quaternion.identity;
            handleRoot.gameObject.SetActive(false);
            bellRoot.gameObject.SetActive(true);
            for (int i = 0; i < titleSnails.Length; i++) titleSnails[i].gameObject.SetActive(true);
        }

        void BeginPetalReveal()
        {
            titleGroup.alpha = 0f;
            gameGroup.alpha = 1f;
            revealGroup.alpha = 0f;
            endGroup.alpha = 0f;
            handleRoot.gameObject.SetActive(true);
            bellRoot.gameObject.SetActive(false);
            // 0.55초 나선 개구가 끝난 뒤 입력 화면을 가리지 않게 완전히 퇴장한다.
            titleIrisRoot.gameObject.SetActive(false);
            guideBoost = 1.4f;
            noticeClock = 999f;
            ShowNotice("경우마다 한 자리, 펼쳐 입장시켜라");
        }

        void RingEntranceBell()
        {
            MgfFx.Punch(bellRoot, .26f, .34f);
            MgfFx.Glow(bellRoot.position + Vector3.up * .6f, Amber, 18, .34f);
            correctFlash = .55f;
        }

        void PointToTitleBell()
        {
            MgfFx.Punch(bellRoot, .13f, .32f);
        }

        void StartProblemVisual(ShowProblem problem, bool practice)
        {
            gameGroup.alpha = 1f;
            titleGroup.alpha = 0f;
            revealGroup.alpha = 0f;
            endGroup.alpha = 0f;
            handleRoot.gameObject.SetActive(true);
            bellRoot.gameObject.SetActive(false);
            visibleSeatTarget = 0;
            SetOpenSeats(0);
            revealProgress = 0f;
            if (practice) guideBoost = 1.35f;
            SetPracticeScaffoldVisible(false);
            RefreshUi();
        }

        void SetOpenSeats(int count)
        {
            visibleSeatTarget = Mathf.Clamp(count, 0, DalpaengiRules.MaxSeats);
        }

        void BeginHandlePress()
        {
            guideBoost = 0f;
            MgfFx.Punch(handleRoot, -.08f, .20f);
        }

        void EndHandlePress()
        {
        }

        void AnimateSeatCounter(int previous, int current)
        {
            previousCounter = previous;
            countGhostText.text = previous.ToString();
            countText.text = current.ToString();
            seatCounterPunch = 1f;
        }

        void BeginCorrectReveal(ShowProblem problem, int submitted, bool practice, int combo)
        {
            revealProgress = 0f;
            correctFlash = 1f;
            drumWave = 0f;
            revealGroup.alpha = 1f;
            revealTitle.text = practice ? "첫 달팽이 관객 입장" : "계산한 수만큼 관객 입장";
            revealFormula.text = problem.formula;
            revealReason.text = problem.kind == ShowKind.Sum
                ? "겹치지 않는 두 목록 중 하나만 고르므로 더한다."
                : problem.kind == ShowKind.Product
                    ? "두 목록에서 각각 하나씩 고르므로 모든 조합을 센다."
                    : "전체에서 알려진 부분을 빼면 나머지 공연 수가 나온다.";
            if (!practice && st.solved > 0 && st.solved <= savedTicketTabs.Length)
                savedTicketTabs[st.solved - 1].localScale = Vector3.one;
            // 정답은 흔들림 없이 달팽이가 좌석을 채우고 껍데기 타악이 번지는 생물 사건이다.
            MgfFx.Glow(new Vector3(0f, 2.3f, -1.4f), LeafLight, 16 + Mathf.Min(18, combo * 3), .28f);
            noticeClock = 0f;
            RefreshUi();
        }

        void BeginWrongReveal(ShowProblem problem, int submitted, string misconceptionId, bool practice)
        {
            revealProgress = 0f;
            wrongFlash = 1f;
            SetPracticeScaffoldVisible(false);
            revealGroup.alpha = 1f;
            revealTitle.text = practice ? "종은 그대로 · 다시 펼치기" :
                (submitted < problem.answer ? "빈자리가 남았다" : "관객이 자리를 못 찾았다");
            revealFormula.text = submitted + "자리 → " + problem.answer + "자리";
            revealReason.text = ExplainMisconception(problem, misconceptionId);
            ShowGhostTickets(problem.answer, submitted);
            if (!practice) ShowBellCrack(st.lives);
            // 흔들림은 오답 전용이며 강도도 한 단계만 쓴다.
            MgfFx.Shake(cam, practice ? .045f : .105f, .24f);
            RefreshUi();
        }

        string ExplainMisconception(ShowProblem problem, string id)
        {
            switch (id)
            {
                case "sum_used_product": return "'또는 한 곡'이고 두 목록은 겹치지 않는다. 이때는 " + problem.m + " + " + problem.n + "이다.";
                case "product_used_sum": return "곡과 배경을 각각 하나씩 고른다. " + problem.m + "묶음마다 " + problem.n + "가지가 있다.";
                case "one_group_only": return "한 목록만 세었다. 두 목록의 조건을 모두 반영하시오.";
                case "reverse_used_total": return "전체 " + problem.total + "가지는 두 목록을 합한 수이다. 알려진 " + problem.m + "가지를 빼시오.";
                case "reverse_added_part": return "전체에 부분을 더하지 않는다. 전체에서 알려진 부분을 빼시오.";
                case "cases_missing": return "펼친 자리가 부족하다. 빠진 달팽이 관객 묶음을 확인하시오.";
                default: return "겹쳐 세거나 한 조건을 빠뜨렸다. 선택 조건부터 다시 확인하시오.";
            }
        }

        void ShowGhostTickets(int answer, int submitted)
        {
            int count = Mathf.Clamp(Mathf.Abs(answer - submitted), 1, 8);
            for (int i = 0; i < count; i++)
            {
                int idx = submitted < answer ? Mathf.Clamp(submitted + i, 0, 16) : Mathf.Clamp(answer + i, 0, 16);
                MgfFx.Glow(seats[idx].position + Vector3.up * .9f, submitted < answer ? Amber : RoseDeep, 3, .42f);
            }
        }

        void ShowBellCrack(int lives)
        {
            // 종 표면의 금 한 줄을 상아색 짧은 선으로 드러내되 대형 붉은 X는 쓰지 않는다.
            for (int i = 0; i < 3; i++)
            {
                var crack = MgfLook.Prim(PrimitiveType.Cube, "Bell crack " + lives + " " + i,
                    new Vector3(4.2f - lives * 1.0f + i * .08f, 1.4f - i * .17f, -3.15f),
                    new Vector3(.06f, .40f, .05f), crackMat, stageRoot, false);
                crack.transform.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 28f : -24f);
                Destroy(crack, 1.6f);
            }
        }

        void CompleteRevealVisual(bool correct)
        {
            revealGroup.alpha = 0f;
            if (ticketStripRoot) ticketStripRoot.localPosition = new Vector3(0f, 2.30f, -.78f);
            if (correct)
            {
                SetOpenSeats(0);
                for (int i = 0; i < chamberShutters.Length; i++)
                {
                    chamberShutters[i].localRotation = Quaternion.identity;
                    chamberShutters[i].localScale = Vector3.one;
                    ticketPunches[i].localScale = Vector3.one * .02f;
                    seatSnails[i].localScale = Vector3.one * .02f;
                }
            }
            wrongFlash = 0f;
            correctFlash = 0f;
            drumWave = -1f;
        }

        void ResumeAfterWrong(ShowProblem problem, string misconceptionId, bool wasFirst)
        {
            revealGroup.alpha = 0f;
            gameGroup.alpha = 1f;
            ShowNotice("같은 공연을 직접 다시 펼쳐 고치시오.");
            noticeClock = 999f;
            guideBoost = 1.05f;
            if (problem.kind == ShowKind.Sum && IsPractice() && st.invalidHandleTaps >= 1)
                SetPracticeScaffoldVisible(true);
        }

        void RefuseAt(Vector2 screen, string message)
        {
            ShowNotice(message);
            noticeClock = 1.4f;
            guideBoost = Mathf.Max(guideBoost, .85f);
            wrongFlash = Mathf.Max(wrongFlash, .22f);
            MgfSfx.Play("tap", .12f);
        }

        void RippleAt(Vector2 screen)
        {
            correctFlash = Mathf.Max(correctFlash, .12f);
        }

        void PointToHandle(Vector2 screen, int missCount)
        {
            guideBoost = Mathf.Max(guideBoost, 1.1f);
            invalidGuideClock = 2.6f;
            invalidGuideStart = ScreenToUi(screen);
            MgfFx.Punch(handleRoot, .11f, .28f);
            if (IsPractice() && current != null && current.kind == ShowKind.Sum && missCount >= 1)
                SetPracticeScaffoldVisible(true);
        }

        void BoostGuide()
        {
            guideBoost = Mathf.Max(guideBoost, 1.25f);
        }

        void ShowNotice(string text)
        {
            noticeText.text = text;
            noticePlate.gameObject.SetActive(true);
            noticeClock = Mathf.Max(noticeClock, 2.4f);
        }

        void ShowEndVisual(bool clear, string reason)
        {
            titleGroup.alpha = 0f;
            gameGroup.alpha = 0f;
            revealGroup.alpha = 0f;
            endGroup.alpha = 1f;
            bellRoot.gameObject.SetActive(true);
            handleRoot.gameObject.SetActive(false);
            endTitle.text = clear ? "껍데기 합주 개막" : "오늘 공연은 폐장";
            endReasonText.text = reason + "\n다음에는 '하나만'인지 '각각 하나씩'인지 먼저 확인하시오.";
            resultCount = 0;
            resultCountFloat = 0f;
            endStats.text = "완성 공연 0/8\n첫 제출 0/" + st.firstAttempts;
            restartText.text = "꽃종을 울려 다시 열기";
            drumWave = clear ? 0f : -1f;
            if (clear) MgfFx.Glow(new Vector3(0f, 3f, -1.5f), Amber, 34, .40f);
        }

        void RefreshUi()
        {
            if (goalText == null) return;
            int displayLives = Mathf.Clamp(st.lives, 0, 3);
            goalText.text = "8공연 · 첫 제출 6회 · 꽃종 " + displayLives;
            progressText.text = IsPractice() ? "비채점 연습" : "공연 " + Mathf.Min(8, st.solved + 1) + "/8";
            if (current != null)
            {
                bandText.text = current.kind == ShowKind.Sum ? "하나만 고르기 · 합의 법칙" :
                    current.kind == ShowKind.Product ? "각각 하나씩 · 곱의 법칙" : "전체에서 부분 찾기";
                promptText.text = current.prompt;
                conditionText.text = current.kind == ShowKind.Sum ? "두 목록은 겹치지 않음" :
                    current.kind == ShowKind.Product ? "모든 곡×배경 조합 가능" : "전체 − 알려진 부분";
            }
            countText.text = st.currentSeatCount.ToString();
            if (localPhase != LocalPhase.End) endGroup.alpha = 0f;
        }

        bool UpdateLayoutIfNeeded(bool force = false)
        {
            bool land = IsLand();
            if (!force && land == lastLand && st.layoutLand == land &&
                lastScreenW == Screen.width && lastScreenH == Screen.height) return false;
            lastLand = land;
            st.layoutLand = land;
            lastScreenW = Screen.width;
            lastScreenH = Screen.height;
            if (uiScaler)
            {
                // 가로는 1280×800 논리 좌표로 고정한다. 기존 390×844 기하평균 좌표에서
                // 1440/2000 화면의 좌우 카드가 캔버스 밖으로 밀리던 문제를 없앤다.
                uiScaler.referenceResolution = land ? new Vector2(1280f, 800f) : new Vector2(390f, 844f);
                uiScaler.matchWidthOrHeight = land ? 1f : .5f;
            }
            if (cam)
            {
                cam.orthographicSize = land ? 7.1f : 10.35f;
                cam.transform.position = land ? new Vector3(1.2f, 7.5f, -19f) : new Vector3(0f, 8.5f, -20f);
                cam.transform.LookAt(land ? new Vector3(.6f, 2.1f, 0f) : new Vector3(0f, 2.5f, 0f));
            }
            if (promptPlate)
            {
                if (land)
                {
                    Place(promptPlate.rectTransform, new Vector2(.16f, .55f), new Vector2(350f, 264f));
                    Place(goalPlate.rectTransform, new Vector2(.84f, .88f), new Vector2(330f, 60f));
                    Place(countPlate.rectTransform, new Vector2(.84f, .55f), new Vector2(170f, 122f));
                    Place(progressText.rectTransform, new Vector2(.80f, .73f), new Vector2(130f, 34f));
                    Place(timerText.rectTransform, new Vector2(.90f, .73f), new Vector2(90f, 34f));
                    Place(handleLabel.rectTransform, new Vector2(.56f, .08f), new Vector2(420f, 38f));
                    Place(noticePlate.rectTransform, new Vector2(.50f, .91f), new Vector2(500f, 58f));
                    Place(revealPlate.rectTransform, new Vector2(.16f, .55f), new Vector2(350f, 168f));
                    Place(pulseRingUi, new Vector2(.57f, .16f), new Vector2(112f, 112f));
                    Place(guideHandUi, new Vector2(.57f, .22f), new Vector2(44f, 66f));
                    Place(guideArrowUi, new Vector2(.57f, .27f), new Vector2(36f, 74f));
                    // 가로 타이틀도 왼쪽 글 블록이 아니라 음악상자 위 간판(가운데 위)으로 건다.
                    titleLogo.text = "달팽이 극장";
                    titleLogo.fontSize = 62f;
                    Place(titlePetalPlate.rectTransform, new Vector2(.5f, .885f), new Vector2(470f, 104f));
                    Place(titleLogo.rectTransform, new Vector2(.5f, .89f), new Vector2(450f, 96f));
                    Place(titleSub.rectTransform, new Vector2(.5f, .795f), new Vector2(470f, 40f));
                    Place(titleCta.rectTransform, new Vector2(.5f, .135f), new Vector2(520f, 64f));
                    Place(titleMeta.rectTransform, new Vector2(.5f, .075f), new Vector2(430f, 30f));
                    Place(assistZoneUi, new Vector2(.57f, .19f), new Vector2(610f, 245f));
                    Place(practiceEquationPlate.rectTransform, new Vector2(.16f, .81f), new Vector2(326f, 88f));
                    Place(practiceGuideRail.rectTransform, new Vector2(.57f, .24f), new Vector2(10f, 126f));
                    Place(practiceTargetNotch.rectTransform, new Vector2(.57f, .32f), new Vector2(210f, 12f));
                    Place(practiceTargetText.rectTransform, new Vector2(.67f, .32f), new Vector2(150f, 34f));
                }
                else
                {
                    Place(promptPlate.rectTransform, new Vector2(.5f, .81f), new Vector2(366f, 178f));
                    Place(goalPlate.rectTransform, new Vector2(.5f, .955f), new Vector2(364f, 54f));
                    Place(countPlate.rectTransform, new Vector2(.5f, .30f), new Vector2(148f, 108f));
                    Place(progressText.rectTransform, new Vector2(.14f, .675f), new Vector2(112f, 34f));
                    Place(timerText.rectTransform, new Vector2(.86f, .675f), new Vector2(100f, 34f));
                    Place(handleLabel.rectTransform, new Vector2(.5f, .115f), new Vector2(356f, 38f));
                    Place(noticePlate.rectTransform, new Vector2(.5f, .54f), new Vector2(348f, 54f));
                    Place(revealPlate.rectTransform, new Vector2(.5f, .81f), new Vector2(366f, 144f));
                    Place(pulseRingUi, new Vector2(.5f, .13f), new Vector2(106f, 106f));
                    Place(guideHandUi, new Vector2(.5f, .18f), new Vector2(42f, 64f));
                    Place(guideArrowUi, new Vector2(.5f, .225f), new Vector2(34f, 70f));
                    titleLogo.text = "달팽이\n극장";
                    titleLogo.fontSize = 56f;
                    Place(titlePetalPlate.rectTransform, new Vector2(.5f, .79f), new Vector2(350f, 174f));
                    Place(titleLogo.rectTransform, new Vector2(.5f, .80f), new Vector2(330f, 176f));
                    Place(titleSub.rectTransform, new Vector2(.5f, .66f), new Vector2(350f, 42f));
                    Place(titleCta.rectTransform, new Vector2(.5f, .18f), new Vector2(350f, 72f));
                    Place(titleMeta.rectTransform, new Vector2(.5f, .095f), new Vector2(360f, 30f));
                    Place(assistZoneUi, new Vector2(.5f, .18f), new Vector2(350f, 190f));
                    Place(practiceEquationPlate.rectTransform, new Vector2(.5f, .60f), new Vector2(324f, 90f));
                    Place(practiceGuideRail.rectTransform, new Vector2(.5f, .2125f), new Vector2(10f, 78f));
                    Place(practiceTargetNotch.rectTransform, new Vector2(.5f, .255f), new Vector2(184f, 12f));
                    // 계수판(가운데) 숫자를 가리지 않게 목표 노치 오른쪽 끝에 붙인다.
                    Place(practiceTargetText.rectTransform, new Vector2(.86f, .255f), new Vector2(104f, 34f));
                }
            }
            return true;
        }

        void UpdateWorldVisual(float dt)
        {
            worldClock += dt;
            guideBoost = Mathf.Max(0f, guideBoost - dt * .20f);
            wrongFlash = Mathf.Max(0f, wrongFlash - dt * 1.8f);
            correctFlash = Mathf.Max(0f, correctFlash - dt * 1.4f);
            seatCounterPunch = Mathf.Max(0f, seatCounterPunch - dt * 5.5f);

            // 껍데기 성장층과 달팽이가 서로 다른 호흡으로 조용히 살아 있다.
            for (int i = 0; i < shellRidges.Length; i++)
            {
                shellRidges[i].localRotation = Quaternion.Euler(0f, 0f,
                    worldClock * (i % 2 == 0 ? 1.8f : -1.25f) + i * 7f);
                shellRidges[i].localScale = new Vector3(1.18f, .92f, 1f);
            }
            for (int i = 0; i < titleSnails.Length; i++)
            {
                if (!titleSnails[i].gameObject.activeSelf) continue;
                var p = titleSnails[i].localPosition;
                p.y = .72f + Mathf.Sin(worldClock * 1.15f + i * 1.9f) * .06f;
                titleSnails[i].localPosition = p;
                titleSnails[i].localRotation = Quaternion.Euler(0f, Mathf.Sin(worldClock * .7f + i) * 8f, 0f);
            }
            if (musicCylinder)
                musicCylinder.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 2.2f) * .035f);

            // 꽃종이 울리면 껍데기 꽃이 빙글 돌며 중심으로 접힌다.
            if (titleOpening)
            {
                float k = EaseOutBack(Mathf.Clamp01(titleClock / .52f));
                float scale = Mathf.Lerp(1f, .12f, Mathf.Clamp01(k));
                titleIrisRoot.localScale = Vector3.one * scale;
                titleIrisRoot.localRotation = Quaternion.Euler(0f, 0f, k * 96f);
            }
            else if (localPhase == LocalPhase.Title && titleIrisRoot.gameObject.activeSelf)
                titleIrisRoot.localRotation = Quaternion.Euler(0f, 0f, -worldClock * 14f);
            else if (localPhase != LocalPhase.Title && titleIrisRoot.gameObject.activeSelf)
            {
                titleIrisRoot.localScale = Vector3.Lerp(titleIrisRoot.localScale, Vector3.one * .12f, dt * 9f);
                titleIrisRoot.Rotate(0f, 0f, dt * 120f);
            }

            for (int i = 0; i < seats.Length; i++)
            {
                float target = i < visibleSeatTarget ? 1f : .18f;
                float currentY = seats[i].localScale.y;
                float speed = i < visibleSeatTarget ? 10f : 13f;
                float next = Mathf.Lerp(currentY, target, 1f - Mathf.Exp(-dt * speed));
                seats[i].localScale = new Vector3(1f + (1f - next) * .08f, next, 1f);
                float targetAngle = i < visibleSeatTarget ? -12f : 76f;
                seatBacks[i].localRotation = Quaternion.Slerp(seatBacks[i].localRotation,
                    Quaternion.Euler(targetAngle, 0f, 0f), 1f - Mathf.Exp(-dt * speed));
            }

            if (localPhase == LocalPhase.Reveal)
            {
                revealProgress = Mathf.Clamp01(revealProgress + dt / (revealCorrect ? 1.28f : .9f));
                if (revealCorrect && current != null)
                {
                    for (int i = 0; i < current.answer; i++)
                    {
                        float delay = i * .035f;
                        float k = Mathf.Clamp01((revealProgress - delay) / Mathf.Max(.15f, 1f - delay));
                        k = k * k * (3f - 2f * k);
                        float hit = Mathf.Sin(k * Mathf.PI);
                        ticketPunches[i].localScale = Vector3.one * Mathf.Lerp(.02f, 1f, k);
                        float arrive = EaseOutBack(k);
                        seatSnails[i].localScale = Vector3.one * Mathf.Lerp(.02f, .78f, arrive);
                        seatSnails[i].localPosition = new Vector3(0f, .42f + hit * .30f, -.52f);
                        seatSnails[i].localRotation = Quaternion.Euler(0f, 0f, hit * (i % 2 == 0 ? 12f : -12f));
                        chamberShutters[i].localRotation = Quaternion.Euler(0f, 0f,
                            (i % 2 == 0 ? -1f : 1f) * hit * 38f);
                        chamberShutters[i].localScale = new Vector3(1f, 1f + hit * .08f, 1f);
                    }
                    ticketStripRoot.localPosition = new Vector3(Mathf.Sin(revealProgress * Mathf.PI) * .16f,
                        2.30f, -.78f);
                }
            }

            // 달팽이가 껍데기를 두드리면 성장층이 앞→뒤로 짧게 호흡한다. 정답 화면 흔들림은 없다.
            if (drumWave >= 0f)
            {
                drumWave += dt * (localPhase == LocalPhase.End ? .75f : 1.5f);
                for (int i = 0; i < shellRidges.Length; i++)
                {
                    float wave = Mathf.Max(0f, 1f - Mathf.Abs(drumWave * 4f - i) * 1.4f);
                    float scale = 1f + wave * .035f;
                    shellRidges[i].localScale = new Vector3(1.18f * scale, .92f * scale, 1f);
                }
                if (drumWave > 2.2f) drumWave = localPhase == LocalPhase.End ? 0f : -1f;
            }

            float guidePulse = .5f + .5f * Mathf.Sin(worldClock * 4.5f);
            if (pulseRing)
            {
                float s = 1f + (.08f + guideBoost * .08f) * guidePulse;
                pulseRing.localScale = Vector3.one * s;
            }
            if (handleRoot)
            {
                Vector3 baseScale = Vector3.one;
                if (holding) baseScale = new Vector3(1.08f, .86f, 1.08f); // squash on press
                handleRoot.localScale = Vector3.Lerp(handleRoot.localScale, baseScale, 1f - Mathf.Exp(-dt * 16f));
            }
            if (stageGlow)
                stageGlow.intensity = 1.20f + correctFlash * 1.15f - wrongFlash * .34f + Mathf.Sin(worldClock * .8f) * .08f;
        }

        void UpdateUiVisual(float dt)
        {
            if (gameGroup == null) return;
            if (localPhase == LocalPhase.Playing)
            {
                int sec = Mathf.Max(0, Mathf.CeilToInt(sessionLeft));
                if (sec != shownSecond) { shownSecond = sec; timerText.text = sec + "초"; }
            }
            else timerText.text = IsPractice() ? "연습" : "";

            if (noticeClock > 0f)
            {
                noticeClock -= dt;
                noticePlate.gameObject.SetActive(true);
            }
            else if (localPhase == LocalPhase.Playing && st.solved > 0)
                noticePlate.gameObject.SetActive(false);

            float pulse = .5f + .5f * Mathf.Sin(worldClock * (guideBoost > .9f ? 6f : 3.2f));
            invalidGuideClock = Mathf.Max(0f, invalidGuideClock - dt);
            bool invalidGuide = invalidGuideClock > 0f && localPhase != LocalPhase.Reveal;
            bool fixedPracticeGuide = IsPractice() && current != null && current.kind == ShowKind.Sum &&
                                      st.invalidHandleTaps >= 1 && localPhase != LocalPhase.Reveal;
            SetPracticeScaffoldVisible(fixedPracticeGuide);
            // 세로 화면에선 2+3=5 분해 판이 안내 레일 자리를 쓴다 — 두 판이 겹치지 않게 레일을 접는다.
            if (fixedPracticeGuide && !IsLand()) noticePlate.gameObject.SetActive(false);
            bool guideOn = (IsPractice() || guideBoost > .05f || invalidGuide || fixedPracticeGuide) &&
                           localPhase != LocalPhase.Reveal;
            pulseRingUi.gameObject.SetActive(guideOn);
            guideHandUi.gameObject.SetActive(guideOn);
            guideArrowUi.gameObject.SetActive(guideOn);
            invalidTapUi.gameObject.SetActive(invalidGuide && !fixedPracticeGuide);
            invalidPathUi.gameObject.SetActive(invalidGuide && !fixedPracticeGuide);
            assistZoneUi.gameObject.SetActive((invalidGuide || fixedPracticeGuide) && st.invalidHandleTaps > 0);
            if (guideOn)
            {
                pulseRingUi.localScale = Vector3.one * (1f + pulse * .13f);
                guideArrowUi.localScale = new Vector3(1f, .75f + pulse * .35f, 1f);
                if (fixedPracticeGuide)
                {
                    float cycle = Mathf.Repeat(worldClock, 2.4f) / 2.4f;
                    float travel = cycle < .16f ? 0f : cycle < .68f
                        ? Mathf.SmoothStep(0f, 1f, (cycle - .16f) / .52f) : 1f;
                    Vector2 start = ScreenToUi(new Vector2(Screen.width * (IsLand() ? .57f : .5f),
                        Screen.height * (IsLand() ? .16f : .17f)));
                    Vector2 target = ScreenToUi(new Vector2(Screen.width * (IsLand() ? .57f : .5f),
                        Screen.height * (IsLand() ? .32f : .255f)));
                    AnchorCenter(guideHandUi, Vector2.Lerp(start, target, travel));
                    Place(pulseRingUi, IsLand() ? new Vector2(.57f, .16f) : new Vector2(.5f, .17f),
                        IsLand() ? new Vector2(112f, 112f) : new Vector2(106f, 106f));
                    Place(guideArrowUi, IsLand() ? new Vector2(.57f, .24f) : new Vector2(.5f, .2125f),
                        IsLand() ? new Vector2(36f, 126f) : new Vector2(34f, 78f));
                    guideHandUi.localRotation = Quaternion.Euler(0f, 0f, -10f + pulse * 5f);
                }
                else if (invalidGuide)
                {
                    Vector2 target = ScreenToUi(new Vector2(Screen.width * (IsLand() ? .57f : .5f),
                        Screen.height * (IsLand() ? .16f : .13f)));
                    float travel = 1f - Mathf.Clamp01(invalidGuideClock / 2.6f);
                    travel = travel < .78f ? Mathf.SmoothStep(0f, 1f, travel / .78f) : 1f;
                    AnchorCenter(guideHandUi, Vector2.Lerp(invalidGuideStart, target, travel));
                    AnchorCenter(invalidTapUi, invalidGuideStart);
                    Vector2 delta = target - invalidGuideStart;
                    AnchorCenter(invalidPathUi, (target + invalidGuideStart) * .5f);
                    invalidPathUi.sizeDelta = new Vector2(7f + st.invalidHandleTaps * 2f, Mathf.Max(36f, delta.magnitude));
                    invalidPathUi.localRotation = Quaternion.Euler(0f, 0f,
                        -Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg);
                    invalidTapUi.localScale = Vector3.one * (1f + pulse * .22f);
                    assistZoneUi.localScale = Vector3.one * (1f + Mathf.Min(2, st.invalidHandleTaps) * .08f);
                    guideHandUi.localRotation = Quaternion.Euler(0f, 0f, -14f + pulse * 9f);
                }
                else
                {
                    Place(guideHandUi, IsLand() ? new Vector2(.57f, .22f) : new Vector2(.5f, .18f),
                        IsLand() ? new Vector2(44f, 66f) : new Vector2(42f, 64f));
                    Place(guideArrowUi, IsLand() ? new Vector2(.57f, .27f) : new Vector2(.5f, .225f),
                        IsLand() ? new Vector2(36f, 74f) : new Vector2(34f, 70f));
                    float amp = guideBoost > .9f ? 34f : 18f;
                    guideHandUi.anchoredPosition = new Vector2(0f, Mathf.Lerp(-amp * .35f, amp, pulse));
                    guideHandUi.localRotation = Quaternion.Euler(0f, 0f, -14f + pulse * 9f);
                }
            }

            if (seatCounterPunch > 0f)
            {
                float k = seatCounterPunch;
                countText.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin((1f - k) * Mathf.PI) * .24f);
                countText.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(12f, 0f, 1f - k));
                countGhostText.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 20f, 1f - k));
                countGhostText.color = new Color(Ivory.r, Ivory.g, Ivory.b, k * .48f);
            }
            else
            {
                countText.rectTransform.localScale = Vector3.one;
                countText.rectTransform.anchoredPosition = Vector2.zero;
                countGhostText.color = new Color(Ivory.r, Ivory.g, Ivory.b, 0f);
            }

            if (localPhase == LocalPhase.End)
            {
                resultCountFloat = Mathf.MoveTowards(resultCountFloat, st.solved, dt * 8f);
                int next = Mathf.RoundToInt(resultCountFloat);
                if (next != resultCount || endStats.text.Length == 0)
                {
                    resultCount = next;
                    int shownFirst = st.solved == 0 ? 0 : Mathf.RoundToInt(st.firstCorrect * (resultCount / (float)st.solved));
                    endStats.text = "완성 공연 " + resultCount + "/8\n첫 제출 " + shownFirst + "/" + st.firstAttempts +
                        "\n점수 " + Mathf.RoundToInt(st.score * (st.solved == 0 ? 0f : resultCount / (float)st.solved));
                }
            }
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        Transform NewMeshObject(string name, Mesh mesh, Material material, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return go.transform;
        }

        static Mesh Torus(float major, float minor, int majorSeg, int minorSeg)
        {
            var vertices = new Vector3[(majorSeg + 1) * (minorSeg + 1)];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[majorSeg * minorSeg * 6];
            int v = 0;
            for (int i = 0; i <= majorSeg; i++)
            {
                float a = i * Mathf.PI * 2f / majorSeg;
                Vector3 center = new Vector3(Mathf.Cos(a) * major, Mathf.Sin(a) * major, 0f);
                for (int j = 0; j <= minorSeg; j++)
                {
                    float b = j * Mathf.PI * 2f / minorSeg;
                    Vector3 n = new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(a) * Mathf.Cos(b), Mathf.Sin(b));
                    vertices[v] = center + n * minor;
                    normals[v] = n;
                    uv[v] = new Vector2(i / (float)majorSeg, j / (float)minorSeg);
                    v++;
                }
            }
            int t = 0;
            for (int i = 0; i < majorSeg; i++)
            for (int j = 0; j < minorSeg; j++)
            {
                int a = i * (minorSeg + 1) + j;
                int b = a + minorSeg + 1;
                triangles[t++] = a; triangles[t++] = b; triangles[t++] = a + 1;
                triangles[t++] = a + 1; triangles[t++] = b; triangles[t++] = b + 1;
            }
            var mesh = new Mesh { name = "Dalpaengi torus" };
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        static RectTransform Full(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Image Img(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI Txt(string name, Transform parent, string value, Vector2 anchor, Vector2 offset,
            float size, Color color, float width)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(width, size * 3.4f);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.text = value;
            return t;
        }

        static void FitInside(TextMeshProUGUI t, float width, float height, float minSize)
        {
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.alignment = TextAlignmentOptions.Center;
            t.fontSizeMax = t.fontSize;
            t.fontSizeMin = minSize;
            t.enableAutoSizing = true;
            t.rectTransform.sizeDelta = new Vector2(width, height);
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        static void AnchorCenter(RectTransform rt, Vector2 local)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = local;
        }

        Vector2 ScreenToUi(Vector2 screen)
        {
            if (uiRoot == null || Screen.width <= 0 || Screen.height <= 0) return Vector2.zero;
            return new Vector2((screen.x / Screen.width - .5f) * uiRoot.rect.width,
                (screen.y / Screen.height - .5f) * uiRoot.rect.height);
        }

        static Sprite MakeRoundedSprite(int s, float radius)
        {
            var texture = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "Rounded ivory plate" };
            var pixels = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - (s - 1) * .5f) - (s * .5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - (s - 1) * .5f) - (s * .5f - radius), 0f);
                float alpha = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * s + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100f);
        }

        static Sprite MakeLeafSprite(int s)
        {
            var texture = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "Leaf silhouette" };
            var pixels = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float nx = (x + .5f) / s * 2f - 1f;
                float ny = (y + .5f) / s * 2f - 1f;
                float body = nx * nx / (.78f * .78f) + ny * ny / (.94f * .94f);
                float tip = Mathf.Abs(nx) + Mathf.Abs(ny) * .56f;
                float a = Mathf.Clamp01((1.03f - Mathf.Min(body, tip)) * 7f);
                pixels[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100f);
        }

        static Sprite MakePetalSprite(int s)
        {
            var texture = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "Petal marquee" };
            var pixels = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float nx = (x + .5f) / s * 2f - 1f;
                float ny = (y + .5f) / s * 2f - 1f;
                float scallop = .90f + .08f * Mathf.Cos(Mathf.Atan2(ny, nx) * 10f);
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float a = Mathf.Clamp01((scallop - r) * 9f);
                pixels[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100f);
        }

        static Sprite MakeRingSprite(int s)
        {
            var texture = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = "Pulse ring" };
            var pixels = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float nx = (x + .5f) / s * 2f - 1f;
                float ny = (y + .5f) / s * 2f - 1f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float a = Mathf.Clamp01(1f - Mathf.Abs(r - .78f) * 18f);
                pixels[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100f);
        }
    }
}
