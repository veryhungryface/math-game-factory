// 달팽이 극장 — 코드로 만든 2.5D 분홍 나선 껍데기 극장과 UI/연출.
// 별도 회화 배경이나 검은 HUD 없이 장밋빛 껍데기 벽이 화면의 최빈색이 되게 한다.
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
        static readonly Color Rose = MgfLook.Hex("#DDA6BA");
        static readonly Color RoseLight = MgfLook.Hex("#F2C9D6");
        static readonly Color RoseDeep = MgfLook.Hex("#A96079");
        static readonly Color Leaf = MgfLook.Hex("#71934D");
        static readonly Color LeafLight = MgfLook.Hex("#A7BF78");
        static readonly Color Ivory = MgfLook.Hex("#F8F0DA");
        static readonly Color Ink = MgfLook.Hex("#4D3445");
        static readonly Color Amber = MgfLook.Hex("#DC9851");
        static readonly Color Plum = MgfLook.Hex("#6E3E5C");

        Camera cam;
        Transform worldRoot, shellRoot, stageRoot, seatRoot, audienceRoot, titleCurtainRoot;
        Transform handleRoot, bellRoot, flowerSign, pulseRing, guideHand, guideArrow;
        readonly Transform[] seats = new Transform[DalpaengiRules.MaxSeats];
        readonly Transform[] seatBacks = new Transform[DalpaengiRules.MaxSeats];
        readonly Transform[] audience = new Transform[DalpaengiRules.MaxSeats];
        readonly Vector3[] audienceHomes = new Vector3[DalpaengiRules.MaxSeats];
        readonly Vector3[] audienceStarts = new Vector3[DalpaengiRules.MaxSeats];
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

        RectTransform uiRoot, titleRoot, gameRoot, revealRoot, endRoot;
        CanvasGroup titleGroup, gameGroup, revealGroup, endGroup;
        Image promptPlate, goalPlate, countPlate, noticePlate, titlePetalPlate, endPlate;
        TextMeshProUGUI titleLogo, titleSub, titleCta, titleMeta;
        TextMeshProUGUI goalText, bandText, promptText, conditionText, progressText, timerText;
        TextMeshProUGUI countText, countGhostText, handleLabel, noticeText;
        TextMeshProUGUI revealTitle, revealFormula, revealReason;
        TextMeshProUGUI endTitle, endStats, endReasonText, restartText;
        Sprite roundedSprite, leafSprite, petalSprite, ringSprite;
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
            roseMat = MgfLook.Lit(Rose, .52f, .02f);
            roseLightMat = MgfLook.Lit(RoseLight, .62f, .01f, RoseLight * .035f);
            roseDeepMat = MgfLook.Lit(RoseDeep, .44f, .02f);
            leafMat = MgfLook.Lit(Leaf, .36f, .01f);
            leafLightMat = MgfLook.Lit(LeafLight, .40f, .01f, LeafLight * .03f);
            ivoryMat = MgfLook.Lit(Ivory, .48f, .01f);
            amberMat = MgfLook.Lit(Amber, .66f, .16f, Amber * .08f);
            inkMat = MgfLook.Lit(Ink, .32f, .02f);
            snailBodyMat = MgfLook.Lit(MgfLook.Hex("#F3C6A9"), .60f, .01f);
            snailShellMat = MgfLook.Lit(MgfLook.Hex("#C77491"), .64f, .02f);
            ghostMat = MgfLook.Alpha(new Color(1f, .88f, .60f, .48f));
            crackMat = MgfLook.Unlit(Ivory);

            MgfLook.Sky(MgfLook.Hex("#E9B8C8"), MgfLook.Hex("#F6D8DD"), MgfLook.Hex("#BA718A"), .78f);
            MgfLook.Sun(new Vector3(42f, -34f, 0f), MgfLook.Hex("#FFE8C9"), 1.20f, .56f);
            cam = MgfLook.Camera(new Vector3(0f, 8.2f, -19.5f), new Vector3(0f, 2.1f, 0f), 38f);
            cam.orthographic = true;
            cam.orthographicSize = 10.2f;
            cam.backgroundColor = Rose;

            worldRoot = new GameObject("Rose shell theater").transform;
            shellRoot = new GameObject("Spiral shell walls").transform;
            shellRoot.SetParent(worldRoot, false);
            stageRoot = new GameObject("Petal stage").transform;
            stageRoot.SetParent(worldRoot, false);
            seatRoot = new GameObject("Leaf seat fan").transform;
            seatRoot.SetParent(worldRoot, false);
            audienceRoot = new GameObject("Snail audience pool").transform;
            audienceRoot.SetParent(worldRoot, false);

            // 껍데기 자체가 배경이다. 납작한 탁자나 전면 어두운 판을 만들지 않는다.
            var shellBack = MgfLook.Prim(PrimitiveType.Sphere, "Satin rose shell interior",
                new Vector3(0f, 3.4f, 3.3f), new Vector3(15.5f, 12.4f, 2.4f), roseMat, shellRoot, false);
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
            // 나선 중심을 한쪽으로 살짝 밀어 생성 이미지의 껍데기 단면 인상을 낸다.
            for (int i = 0; i < 9; i++)
            {
                float a = i * .78f;
                float r = .22f + i * .22f;
                var bead = MgfLook.Prim(PrimitiveType.Sphere, "Fine spiral bead " + i,
                    new Vector3(-.6f + Mathf.Cos(a) * r, 4.0f + Mathf.Sin(a) * r, 1.45f),
                    Vector3.one * (.22f + i * .025f), roseDeepMat, shellRoot, false);
                bead.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            // 꽃잎 지붕과 무대. 타이틀의 꽃잎 막은 같은 세계에서 양옆으로 실제로 열린다.
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.Lerp(-74f, 74f, i / 6f) * Mathf.Deg2Rad;
                var petal = MgfLook.Prim(PrimitiveType.Sphere, "Canopy petal " + i,
                    new Vector3(Mathf.Sin(a) * 6.25f, 8.25f + Mathf.Cos(a) * .32f, 2.4f),
                    new Vector3(1.48f, .38f, .24f), i % 2 == 0 ? roseLightMat : roseDeepMat, stageRoot, false);
                petal.transform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Lerp(-30f, 30f, i / 6f));
            }
            var stage = MgfLook.Prim(PrimitiveType.Cylinder, "Round satin stage",
                new Vector3(0f, .05f, 1.0f), new Vector3(7.5f, .35f, 5.2f), roseLightMat, stageRoot, false);
            stage.transform.rotation = Quaternion.identity;
            var stageRim = MgfLook.Prim(PrimitiveType.Cylinder, "Amber stage rim",
                new Vector3(0f, -.05f, .85f), new Vector3(8.0f, .22f, 5.7f), amberMat, stageRoot, false);
            stageRim.transform.rotation = Quaternion.identity;
            var carpet = MgfLook.Prim(PrimitiveType.Cube, "Ivory entry carpet",
                new Vector3(0f, .18f, -3.1f), new Vector3(2.2f, .12f, 5.5f), ivoryMat, stageRoot, false);
            carpet.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            // 무대 간판은 세계 안 사물이며 타이틀 카드가 아니다.
            flowerSign = MgfLook.Block("Flower marquee", new Vector3(0f, 6.15f, -.1f),
                new Vector3(6.6f, 1.5f, .34f), .58f, ivoryMat, stageRoot).transform;
            var signText = MgfText.World("달팽이 극장", new Vector3(0f, 6.15f, -.31f), .72f, Ink, stageRoot);
            signText.fontStyle = FontStyles.Bold;
            signText.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                var bulb = MgfLook.Prim(PrimitiveType.Sphere, "Marquee bulb " + i,
                    new Vector3(Mathf.Cos(a) * 3.0f, 6.15f + Mathf.Sin(a) * .56f, -.34f),
                    Vector3.one * .20f, amberMat, stageRoot, false);
                bulb.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            BuildSeats();
            BuildHandleAndBell();
            BuildTitleCurtains();
            BuildAmbientSnails();

            stageGlow = new GameObject("Warm translucent edge light").AddComponent<Light>();
            stageGlow.type = LightType.Point;
            stageGlow.transform.position = new Vector3(0f, 4.6f, -2.4f);
            stageGlow.color = MgfLook.Hex("#FFD7A0");
            stageGlow.range = 13f;
            stageGlow.intensity = 1.25f;
            stageGlow.shadows = LightShadows.None;
            stageGlow.renderMode = LightRenderMode.ForceVertex;
        }

        void BuildSeats()
        {
            for (int i = 0; i < seats.Length; i++)
            {
                float t = i / 16f;
                float angle = Mathf.Lerp(-76f, 76f, t) * Mathf.Deg2Rad;
                float radius = 4.0f + .65f * Mathf.Sin(t * Mathf.PI);
                Vector3 p = new Vector3(Mathf.Sin(angle) * radius, .48f + .18f * Mathf.Cos(angle),
                    -1.10f + Mathf.Cos(angle) * 2.4f);
                var root = new GameObject("Leaf seat " + (i + 1)).transform;
                root.SetParent(seatRoot, false);
                root.localPosition = p;
                root.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                seats[i] = root;

                var pad = MgfLook.Prim(PrimitiveType.Sphere, "Leaf cushion", Vector3.zero,
                    new Vector3(.68f, .18f, .82f), leafMat, root, false);
                pad.transform.localPosition = new Vector3(0f, .02f, .04f);
                var back = MgfLook.Prim(PrimitiveType.Sphere, "Folding leaf back", new Vector3(0f, .36f, .30f),
                    new Vector3(.72f, .80f, .18f), leafLightMat, root, false).transform;
                back.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                seatBacks[i] = back;

                // 의자 번호는 정답 힌트가 아니라 현재 조작 수의 물리적 눈금이다.
                var number = MgfText.World((i + 1).ToString(), new Vector3(0f, .62f, -.13f), .29f, Ivory, root);
                number.fontStyle = FontStyles.Bold;
                number.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                var snail = BuildSnail("Audience snail " + (i + 1), audienceRoot,
                    new Color(.92f - .16f * (i % 3) / 2f, .70f + .12f * (i % 4) / 3f, .62f + .18f * (i % 5) / 4f));
                audienceHomes[i] = p + new Vector3(0f, .55f, -.20f);
                audienceStarts[i] = new Vector3((i % 2 == 0 ? -1f : 1f) * (8.5f + i * .10f), .25f, -3.7f - (i % 3) * .35f);
                snail.localPosition = audienceStarts[i];
                snail.localScale = Vector3.one * .62f;
                snail.gameObject.SetActive(false);
                audience[i] = snail;
            }
        }

        void BuildHandleAndBell()
        {
            handleRoot = new GameObject("Round leaf handle").transform;
            handleRoot.SetParent(worldRoot, false);
            handleRoot.localPosition = new Vector3(0f, .45f, -5.45f);
            var handleLeaf = MgfLook.Prim(PrimitiveType.Sphere, "Handle leaf", Vector3.zero,
                new Vector3(1.25f, .32f, .95f), leafLightMat, handleRoot, true);
            handleLeaf.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            var handleKnob = MgfLook.Prim(PrimitiveType.Sphere, "Handle bud", new Vector3(0f, .48f, -.05f),
                new Vector3(.62f, .62f, .62f), amberMat, handleRoot, true);
            pulseRing = NewMeshObject("Handle pulse ring", Torus(.92f, .07f, 48, 6), leafLightMat, handleRoot);
            pulseRing.localPosition = new Vector3(0f, .12f, -.50f);
            pulseRing.localRotation = Quaternion.Euler(72f, 0f, 0f);

            bellRoot = new GameObject("Entrance bell CTA").transform;
            bellRoot.SetParent(worldRoot, false);
            bellRoot.localPosition = new Vector3(0f, .72f, -4.9f);
            var bell = MgfLook.Prim(PrimitiveType.Sphere, "Petal entrance bell", Vector3.zero,
                new Vector3(1.25f, .92f, 1.0f), amberMat, bellRoot, true);
            bell.transform.localPosition = new Vector3(0f, .05f, 0f);
            MgfLook.Prim(PrimitiveType.Cylinder, "Bell stem", new Vector3(0f, .88f, .0f),
                new Vector3(.22f, .65f, .22f), inkMat, bellRoot, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Bell striker", new Vector3(0f, -.55f, -.18f),
                Vector3.one * .32f, ivoryMat, bellRoot, false);
        }

        void BuildTitleCurtains()
        {
            titleCurtainRoot = new GameObject("Split reveal petal curtains").transform;
            titleCurtainRoot.SetParent(worldRoot, false);
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 4; i++)
            {
                var petal = MgfLook.Prim(PrimitiveType.Sphere, "Curtain petal " + side + " " + i,
                    new Vector3(side * (1.15f + i * .95f), 3.45f + i * .35f, -2.2f - i * .10f),
                    new Vector3(2.4f, 4.7f - i * .42f, .42f), i % 2 == 0 ? roseDeepMat : roseMat,
                    titleCurtainRoot, false);
                petal.transform.localRotation = Quaternion.Euler(0f, side * 8f, side * (18f + i * 5f));
            }
        }

        void BuildAmbientSnails()
        {
            for (int i = 0; i < titleSnails.Length; i++)
            {
                var snail = BuildSnail("Title usher snail " + i, worldRoot,
                    Color.Lerp(MgfLook.Hex("#E8B89A"), MgfLook.Hex("#F5D8B5"), i / 2f));
                snail.localPosition = i == 1
                    ? new Vector3(-3.05f, .84f, -2.8f)
                    : new Vector3(i == 0 ? 3.75f : 5.45f, .72f, -1.55f + i * .22f);
                snail.localScale = Vector3.one * (i == 1 ? 1.95f : .82f);
                titleSnails[i] = snail;
            }
        }

        Transform BuildSnail(string name, Transform parent, Color bodyColor)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var bodyMat = MgfLook.Lit(bodyColor, .62f, .01f);
            MgfLook.Prim(PrimitiveType.Sphere, "Plump body", new Vector3(0f, .20f, 0f),
                new Vector3(.55f, .44f, .78f), bodyMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Spiral shell", new Vector3(0f, .55f, .20f),
                new Vector3(.75f, .72f, .32f), snailShellMat, root, false);
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

            titleRoot = Full("Title screen", uiRoot);
            titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
            titlePetalPlate = Img("Flower sign plate", titleRoot, new Vector2(.5f, .79f), Vector2.zero,
                new Vector2(350f, 174f), Ivory, petalSprite);
            titleLogo = Txt("Logo", titleRoot, "달팽이\n극장", new Vector2(.5f, .80f), new Vector2(0f, 4f), 56f, Ink, 330f);
            titleLogo.fontStyle = FontStyles.Bold;
            titleLogo.outlineColor = RoseDeep;
            titleLogo.outlineWidth = .22f;
            titleLogo.lineSpacing = -24f;
            titleSub = Txt("Tagline", titleRoot, "객석을 펼쳐 입장시켜라", new Vector2(.5f, .66f), Vector2.zero, 18f, Ink, 350f);
            titleCta = Txt("World CTA", titleRoot, "입장종을 울려 극장 열기", new Vector2(.5f, .18f), Vector2.zero, 23f, Ivory, 350f);
            titleCta.outlineColor = Ink;
            titleCta.outlineWidth = .19f;
            titleMeta = Txt("Title meta", titleRoot, "중2 · 경우의 수     최고 기록은 첫 공연 뒤 저장", new Vector2(.5f, .095f), Vector2.zero, 12f, Ivory, 360f);

            gameRoot = Full("Game HUD", uiRoot);
            gameGroup = gameRoot.gameObject.AddComponent<CanvasGroup>();
            goalPlate = Img("Goal plate", gameRoot, new Vector2(.5f, .955f), Vector2.zero,
                new Vector2(364f, 54f), RoseLight, roundedSprite);
            goalText = Txt("Goal", goalPlate.transform, "8공연 · 첫 제출 6회 · 입장종 3", new Vector2(.5f, .5f),
                Vector2.zero, 15f, Ink, 342f);
            promptPlate = Img("Ivory music stand", gameRoot, new Vector2(.5f, .82f), Vector2.zero,
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
            countPlate = Img("Seat counter leaf", gameRoot, new Vector2(.5f, .30f), Vector2.zero,
                new Vector2(148f, 108f), Leaf, leafSprite);
            countGhostText = Txt("Old counter glyph", countPlate.transform, "0", new Vector2(.5f, .5f), Vector2.zero, 50f, Ivory, 100f);
            countText = Txt("Current seats", countPlate.transform, "0", new Vector2(.5f, .5f), Vector2.zero, 52f, Ivory, 100f);
            countText.outlineColor = Ink;
            countText.outlineWidth = .15f;
            handleLabel = Txt("Handle label", gameRoot, "누른 채 펼치고 · 놓아 입장", new Vector2(.5f, .115f), Vector2.zero, 16f, Ivory, 356f);
            handleLabel.outlineColor = Ink;
            handleLabel.outlineWidth = .13f;
            noticePlate = Img("Notice leaf vein", gameRoot, new Vector2(.5f, .54f), Vector2.zero,
                new Vector2(348f, 54f), RoseDeep, roundedSprite);
            noticeText = Txt("Notice", noticePlate.transform, "경우마다 한 자리, 펼쳐 입장시켜라", new Vector2(.5f, .5f),
                Vector2.zero, 14f, Ivory, 324f);

            revealRoot = Full("Reveal", uiRoot);
            revealGroup = revealRoot.gameObject.AddComponent<CanvasGroup>();
            var revealPlate = Img("Reveal petal", revealRoot, new Vector2(.5f, .57f), Vector2.zero,
                new Vector2(360f, 224f), Ivory, petalSprite);
            revealTitle = Txt("Reveal title", revealPlate.transform, "", new Vector2(.5f, .75f), Vector2.zero, 24f, Ink, 330f);
            revealFormula = Txt("Formula", revealPlate.transform, "", new Vector2(.5f, .48f), Vector2.zero, 34f, RoseDeep, 330f);
            revealReason = Txt("Reason", revealPlate.transform, "", new Vector2(.5f, .21f), Vector2.zero, 15f, Ink, 320f);
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
            restartText = Txt("Restart CTA", endRoot, "입장종을 울려 다시 열기", new Vector2(.5f, .17f), Vector2.zero, 22f, Ivory, 350f);
            restartText.outlineColor = Ink;
            restartText.outlineWidth = .18f;

            CreateGuideObjects();
            UpdateLayoutIfNeeded(true);
        }

        void CreateGuideObjects()
        {
            var canvas = MgfText.Canvas.transform;
            var ring = Img("Guide pulse ring", gameRoot, new Vector2(.5f, .13f), Vector2.zero,
                new Vector2(106f, 106f), new Color(1f, .90f, .50f, .85f), ringSprite);
            pulseRingUi = ring.rectTransform;
            var hand = Img("Ghost hand", gameRoot, new Vector2(.5f, .17f), Vector2.zero,
                new Vector2(42f, 64f), Ivory, leafSprite);
            guideHandUi = hand.rectTransform;
            var arrow = Img("Flow arrow", gameRoot, new Vector2(.5f, .205f), Vector2.zero,
                new Vector2(34f, 70f), LeafLight, leafSprite);
            guideArrowUi = arrow.rectTransform;
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
                audience[i].gameObject.SetActive(false);
                audience[i].localPosition = audienceStarts[i];
                audience[i].localScale = Vector3.one * .62f;
            }
            if (titleCurtainRoot) titleCurtainRoot.localScale = Vector3.one;
            if (bellRoot) bellRoot.gameObject.SetActive(true);
            if (handleRoot) handleRoot.gameObject.SetActive(false);
        }

        void ShowTitleVisual()
        {
            titleGroup.alpha = 1f;
            titleGroup.blocksRaycasts = false;
            gameGroup.alpha = 0f;
            revealGroup.alpha = 0f;
            endGroup.alpha = 0f;
            titleCurtainRoot.gameObject.SetActive(true);
            titleCurtainRoot.localPosition = Vector3.zero;
            titleCurtainRoot.localScale = Vector3.one;
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
            // 0.55초 Split Reveal이 끝난 뒤 막은 입력 화면을 가리지 않게 완전히 퇴장한다.
            titleCurtainRoot.gameObject.SetActive(false);
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
            revealTitle.text = practice ? "첫 객석 만석" : "마지막 자리까지 만석";
            revealFormula.text = problem.formula;
            revealReason.text = problem.kind == ShowKind.Sum
                ? "겹치지 않는 두 목록 중 하나만 고르므로 더한다."
                : problem.kind == ShowKind.Product
                    ? "두 목록에서 각각 하나씩 고르므로 모든 조합을 센다."
                    : "전체에서 알려진 부분을 빼면 나머지 공연 수가 나온다.";
            for (int i = 0; i < problem.answer; i++)
            {
                audience[i].gameObject.SetActive(true);
                audience[i].localPosition = audienceStarts[i];
            }
            // 정답은 화면 흔들림/색종이 대신 따뜻한 빛, 입장 이동, 껍데기 합주로 표현한다.
            MgfFx.Glow(new Vector3(0f, 2.3f, -1.4f), LeafLight, 16 + Mathf.Min(18, combo * 3), .28f);
            noticeClock = 0f;
            RefreshUi();
        }

        void BeginWrongReveal(ShowProblem problem, int submitted, string misconceptionId, bool practice)
        {
            revealProgress = 0f;
            wrongFlash = 1f;
            revealGroup.alpha = 1f;
            revealTitle.text = practice ? "종은 그대로 · 다시 펼치기" :
                (submitted < problem.answer ? "빈자리가 남았다" : "관객이 자리를 못 찾았다");
            revealFormula.text = submitted + "자리 → " + problem.answer + "자리";
            revealReason.text = ExplainMisconception(problem, misconceptionId);
            ShowGhostTickets(problem.answer, submitted);
            if (!practice) ShowBellCrack(st.lives);
            // 흔들림은 오답 전용이며 강도도 한 단계만 쓴다.
            MgfFx.Shake(cam, practice ? .045f : .105f, .24f);
            ReactAudience(false);
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
                case "cases_missing": return "펼친 자리가 부족하다. 빠진 공연표 묶음을 확인하시오.";
                default: return "겹쳐 세거나 한 조건을 빠뜨렸다. 선택 조건부터 다시 확인하시오.";
            }
        }

        void ShowGhostTickets(int answer, int submitted)
        {
            int count = Mathf.Clamp(Mathf.Abs(answer - submitted), 1, 8);
            for (int i = 0; i < count; i++)
            {
                int idx = submitted < answer ? Mathf.Clamp(submitted + i, 0, 16) : Mathf.Clamp(answer + i, 0, 16);
                MgfFx.Glow(audienceHomes[idx] + Vector3.up * .9f, submitted < answer ? Amber : RoseDeep, 3, .42f);
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
            if (correct)
            {
                for (int i = 0; i < audience.Length; i++) audience[i].gameObject.SetActive(false);
                SetOpenSeats(0);
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
            ReactAudience(false);
        }

        void ReactAudience(bool happy)
        {
            for (int i = 0; i < audience.Length; i++)
                if (audience[i].gameObject.activeSelf)
                    audience[i].localRotation = Quaternion.Euler(happy ? -8f : 8f, 0f, happy ? -5f : 5f);
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

        void PointToHandle()
        {
            guideBoost = Mathf.Max(guideBoost, 1.1f);
            MgfFx.Punch(handleRoot, .11f, .28f);
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
            restartText.text = "입장종을 울려 다시 열기";
            drumWave = clear ? 0f : -1f;
            if (clear) MgfFx.Glow(new Vector3(0f, 3f, -1.5f), Amber, 34, .40f);
        }

        void RefreshUi()
        {
            if (goalText == null) return;
            int displayLives = Mathf.Clamp(st.lives, 0, 3);
            goalText.text = "8공연 · 첫 제출 6회 · 입장종 " + displayLives;
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
            if (!force && land == lastLand && st.layoutLand == land) return false;
            lastLand = land;
            st.layoutLand = land;
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
                    Place(promptPlate.rectTransform, new Vector2(.17f, .58f), new Vector2(300f, 260f));
                    Place(goalPlate.rectTransform, new Vector2(.83f, .88f), new Vector2(300f, 60f));
                    Place(countPlate.rectTransform, new Vector2(.83f, .56f), new Vector2(160f, 118f));
                    Place(progressText.rectTransform, new Vector2(.80f, .74f), new Vector2(130f, 34f));
                    Place(timerText.rectTransform, new Vector2(.90f, .74f), new Vector2(90f, 34f));
                    Place(handleLabel.rectTransform, new Vector2(.56f, .09f), new Vector2(420f, 38f));
                    Place(noticePlate.rectTransform, new Vector2(.50f, .91f), new Vector2(500f, 58f));
                    Place(pulseRingUi, new Vector2(.57f, .16f), new Vector2(112f, 112f));
                    Place(guideHandUi, new Vector2(.57f, .22f), new Vector2(44f, 66f));
                    Place(guideArrowUi, new Vector2(.57f, .27f), new Vector2(36f, 74f));
                    titleLogo.fontSize = 58f;
                    Place(titlePetalPlate.rectTransform, new Vector2(.27f, .62f), new Vector2(420f, 220f));
                    Place(titleLogo.rectTransform, new Vector2(.27f, .64f), new Vector2(390f, 190f));
                    Place(titleSub.rectTransform, new Vector2(.27f, .45f), new Vector2(390f, 42f));
                    Place(titleCta.rectTransform, new Vector2(.70f, .18f), new Vector2(430f, 70f));
                    Place(titleMeta.rectTransform, new Vector2(.70f, .10f), new Vector2(430f, 30f));
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
                    Place(pulseRingUi, new Vector2(.5f, .13f), new Vector2(106f, 106f));
                    Place(guideHandUi, new Vector2(.5f, .18f), new Vector2(42f, 64f));
                    Place(guideArrowUi, new Vector2(.5f, .225f), new Vector2(34f, 70f));
                    titleLogo.fontSize = 56f;
                    Place(titlePetalPlate.rectTransform, new Vector2(.5f, .79f), new Vector2(350f, 174f));
                    Place(titleLogo.rectTransform, new Vector2(.5f, .80f), new Vector2(330f, 176f));
                    Place(titleSub.rectTransform, new Vector2(.5f, .66f), new Vector2(350f, 42f));
                    Place(titleCta.rectTransform, new Vector2(.5f, .18f), new Vector2(350f, 72f));
                    Place(titleMeta.rectTransform, new Vector2(.5f, .095f), new Vector2(360f, 30f));
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

            // ambient idle motion: 꽃잎과 달팽이 더듬이가 조용히 살아 있다.
            for (int i = 0; i < shellRidges.Length; i++)
            {
                float breath = 1f + Mathf.Sin(worldClock * .55f + i * .8f) * .008f;
                shellRidges[i].localScale = new Vector3(1.18f * breath, .92f * breath, 1f);
            }
            for (int i = 0; i < titleSnails.Length; i++)
            {
                if (!titleSnails[i].gameObject.activeSelf) continue;
                var p = titleSnails[i].localPosition;
                p.y = .72f + Mathf.Sin(worldClock * 1.15f + i * 1.9f) * .06f;
                titleSnails[i].localPosition = p;
                titleSnails[i].localRotation = Quaternion.Euler(0f, Mathf.Sin(worldClock * .7f + i) * 8f, 0f);
            }

            // Split Reveal: 종이 울리면 꽃잎 막이 좌우로 벌어진다.
            if (titleOpening)
            {
                float k = EaseOutBack(Mathf.Clamp01(titleClock / .52f));
                titleCurtainRoot.localScale = new Vector3(1f + k * .90f, 1f - k * .07f, 1f);
                titleCurtainRoot.localPosition = new Vector3(0f, k * .18f, 0f);
            }
            else if (localPhase != LocalPhase.Title && titleCurtainRoot.localScale.x < 1.85f)
            {
                titleCurtainRoot.localScale = Vector3.Lerp(titleCurtainRoot.localScale, new Vector3(1.9f, .93f, 1f), dt * 7f);
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
                        audience[i].localPosition = Vector3.Lerp(audienceStarts[i], audienceHomes[i], k);
                        float squash = 1f + Mathf.Sin(k * Mathf.PI) * .18f;
                        audience[i].localScale = new Vector3(.62f / squash, .62f * squash, .62f);
                    }
                }
            }

            // 껍데기 두드림은 앞→뒤로 번지는 파동이며 정답 사건의 시그니처다.
            if (drumWave >= 0f)
            {
                drumWave += dt * (localPhase == LocalPhase.End ? .75f : 1.5f);
                for (int i = 0; i < shellRidges.Length; i++)
                {
                    float wave = Mathf.Max(0f, 1f - Mathf.Abs(drumWave * 4f - i) * 1.4f);
                    float scale = 1f + wave * .055f;
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
            bool guideOn = (IsPractice() || guideBoost > .05f) && localPhase != LocalPhase.Reveal;
            pulseRingUi.gameObject.SetActive(guideOn);
            guideHandUi.gameObject.SetActive(guideOn);
            guideArrowUi.gameObject.SetActive(guideOn);
            if (guideOn)
            {
                float amp = guideBoost > .9f ? 34f : 18f;
                guideHandUi.anchoredPosition = new Vector2(0f, Mathf.Lerp(-amp * .35f, amp, pulse));
                guideHandUi.localRotation = Quaternion.Euler(0f, 0f, -14f + pulse * 9f);
                pulseRingUi.localScale = Vector3.one * (1f + pulse * .13f);
                guideArrowUi.localScale = new Vector3(1f, .75f + pulse * .35f, 1f);
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

        static void Place(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
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
