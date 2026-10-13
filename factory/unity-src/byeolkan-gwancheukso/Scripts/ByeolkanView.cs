using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Mgf;

namespace Mgf.ByeolkanGwancheukso
{
    public partial class ByeolkanGame
    {
        // 2차 수정: 둥근 파스텔 원반을 화면 폭 전체의 계단식 천문 마을로 교체한다.
        // 거친 테라코타 모자이크·각진 황동 궤도·남색 밤하늘을 타이틀부터 결과까지 유지한다.
        static readonly Color Night = new Color32(25, 31, 46, 255);
        static readonly Color Cobalt = new Color32(38, 77, 100, 255);
        static readonly Color Opal = new Color32(239, 220, 174, 255);
        static readonly Color Violet = new Color32(87, 67, 101, 255);
        static readonly Color Star = new Color32(246, 168, 67, 255);
        static readonly Color Bubble = new Color32(95, 57, 72, 255);
        static readonly Color Pale = new Color32(255, 243, 207, 255);
        static readonly Color Basalt = new Color32(143, 68, 48, 255);
        static readonly Color Nickel = new Color32(145, 103, 48, 255);
        static readonly Color Wrong = new Color32(174, 72, 79, 255);
        static readonly Color SkyTeal = new Color32(119, 190, 196, 255);
        static readonly Color Leaf = new Color32(93, 145, 101, 255);

        Camera cam;
        Transform worldRoot, titleRing, orbitA, orbitB, rackWorldA, rackWorldB, nova, novaTail, dome;
        readonly Transform[] orbiters = new Transform[9];
        readonly Transform[] novaLimbs = new Transform[4];
        readonly Transform[] domeShards = new Transform[6];
        readonly Quaternion[] domeShardBaseRot = new Quaternion[6];
        readonly Transform[] towerHeads = new Transform[7];
        readonly Transform[] towerRays = new Transform[7];
        readonly Renderer[] towerLamps = new Renderer[7];
        readonly Transform[] starNodes = new Transform[36];
        readonly Renderer[] starRenderers = new Renderer[36];
        readonly Vector3[] starWorld = new Vector3[36];
        readonly Vector3[] rackTargetPos = new Vector3[2];
        readonly Quaternion[] rackTargetRot = new Quaternion[2];
        readonly Vector3[] rackVelocity = new Vector3[2];
        Material basaltMat, basaltLightMat, nickelMat, opalMat, opalGlowMat, starOffMat, starOnMat, bubbleMat, cobaltMat, glassMat;
        float worldTime, wrongShake, novaRun = -1f, dockPulse, crankVisual, pavilionFold, pavilionFoldTarget, towerAim = -1f;
        Vector3 camBase;

        RectTransform uiRoot, titleUi, gameUi, endUi, titleBannerRt, titleHandleRt, restartRt, boardRt, crankRt, foldRt;
        RectTransform hudStripRt, promptPlateRt, goalRt, actionRt;
        RectTransform rackHandleA, rackHandleB, guideRt, guideTrailRt, guideHandRt, guideCaptionRt, rippleRt, toastRt, revealRt;
        RectTransform endDockLeft, endDockRight, crossDock;
        CanvasGroup titleGroup, gameGroup, endGroup, toastGroup, revealGroup;
        CanvasScaler scaler;
        RawImage titleArt;
        Image titleHandleImage, crankImage, foldImage, boardPlateImage, guideImage, rippleImage, toastPlate, revealPlate;
        Image sealImage;
        TextMeshProUGUI logoText, taglineText, titleMetaText, titleHandleText;
        TextMeshProUGUI scoreText, livesText, timerText, progressText, goalText, promptText, conceptText, actionText;
        TextMeshProUGUI toastText, revealText, endTitle, endStats, endCta, guideHandText, guideCaptionText;
        TextMeshProUGUI endDockLeftText, crossDockText, endDockRightText;
        readonly RectTransform[] cellRt = new RectTransform[36];
        readonly Image[] cellImage = new Image[36];
        readonly TextMeshProUGUI[] cellText = new TextMeshProUGUI[36];
        readonly RectTransform[] trailDots = new RectTransform[7];
        readonly Image[] trailImages = new Image[7];
        Sprite discSprite, ringSprite, panelSprite, starSprite, crackSprite;
        int activeCells;
        int displayedScore;
        float displayedScoreVelocity;
        float rippleClock = 9f, toastClock = 9f, revealClock = 9f, refuseClock = 9f, guideBoost;
        int lastScreenW, lastScreenH;
        bool wideLayout;

        void BuildWorld()
        {
            // CreatePrimitive(Cylinder/Sphere)는 WebGL 런타임에서 콜라이더를 이름으로 붙인다.
            // 명시 참조가 없으면 managed stripping이 타입을 제거해 콘솔 오류가 난다.
            var colliderTypes = new GameObject("Primitive collider type keeper");
            colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<SphereCollider>();
            colliderTypes.AddComponent<BoxCollider>();
            UnityEngine.Object.Destroy(colliderTypes);
            MgfLook.Sky(MgfLook.Hex("#101A31"), MgfLook.Hex("#304866"), MgfLook.Hex("#9A4D38"), .48f);
            MgfLook.Sun(new Vector3(42f, -32f, 18f), new Color(1f, .72f, .43f), 1.16f, .82f);
            cam = MgfLook.Camera(new Vector3(0f, 17.8f, -18.8f), new Vector3(0f, 1.0f, 2.0f), 38f);
            cam.orthographic = true; cam.orthographicSize = 9.1f; camBase = cam.transform.position;

            basaltMat = MgfLook.Lit(Basalt, .14f, 0f);
            basaltLightMat = MgfLook.Lit(new Color32(190, 108, 70, 255), .07f, 0f);
            nickelMat = MgfLook.Lit(Nickel, .24f, .12f);
            opalMat = MgfLook.Lit(Opal, .08f, 0f);
            opalGlowMat = MgfLook.Lit(new Color32(255, 225, 155, 255), .16f, 0f, new Color(.12f, .06f, .01f));
            starOffMat = MgfLook.Lit(new Color32(91, 73, 65, 255), .06f, 0f);
            starOnMat = MgfLook.Lit(Star, .14f, .04f, Star * .26f);
            bubbleMat = MgfLook.Lit(Bubble, .12f, 0f);
            cobaltMat = MgfLook.Lit(Cobalt, .16f, 0f);
            glassMat = MgfLook.Lit(new Color32(49, 80, 96, 255), .18f, .04f);

            worldRoot = new GameObject("Stepped Terracotta Observatory Town").transform;
            BuildCrater();
            BuildObservatoryRings();
            BuildAimTowers();
            rackWorldA = BuildRack("Opal Rack A", 6);
            rackWorldB = BuildRack("Opal Rack B", 6);
            rackWorldA.SetParent(worldRoot); rackWorldB.SetParent(worldRoot);
            rackTargetPos[0] = new Vector3(-4.8f, .6f, -3.2f); rackTargetPos[1] = new Vector3(4.8f, .6f, -3.2f);
            rackTargetRot[0] = Quaternion.Euler(0, 0, 0); rackTargetRot[1] = Quaternion.Euler(0, 180, 0);
            rackWorldA.localPosition = rackTargetPos[0]; rackWorldB.localPosition = rackTargetPos[1];
            rackWorldA.localRotation = rackTargetRot[0]; rackWorldB.localRotation = rackTargetRot[1];
            BuildContactStars();
            BuildNova();
            BuildDome();
        }

        void BuildCrater()
        {
            // Three long terraces replace the familiar circular diorama silhouette and fill
            // both desktop gutters with playable-world architecture.
            MgfLook.Block("Lower mosaic terrace", new Vector3(0, -.72f, 1.4f), new Vector3(25f, .85f, 11f), .12f, basaltMat, worldRoot);
            MgfLook.Block("Middle observatory street", new Vector3(0, -.18f, 3.1f), new Vector3(21f, .72f, 7.2f), .10f, basaltLightMat, worldRoot);
            MgfLook.Block("Upper telescope walk", new Vector3(0, .28f, 5.8f), new Vector3(17f, .62f, 3.4f), .08f, cobaltMat, worldRoot);
            for (int i = 0; i < 36; i++)
            {
                int row = i % 3;
                float x = -11.4f + (i / 3) * 2.05f + (row == 1 ? .62f : 0f);
                float z = row == 0 ? -.5f : row == 1 ? 4.9f : 7.3f;
                float h = 1.0f + (i % 5) * .20f;
                var house = MgfLook.Block("Chiseled mosaic house", new Vector3(x, h * .5f, z),
                    new Vector3(1.28f + (i % 2) * .22f, h, 1.05f), .05f, i % 4 == 0 ? opalMat : basaltLightMat, worldRoot);
                var roof = MgfLook.Block("Angular brass roof", new Vector3(0, h * .54f, 0), new Vector3(1.48f, .14f, 1.20f), .025f,
                    i % 3 == 0 ? nickelMat : glassMat, house.transform);
                roof.transform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 7f : -7f);
                MgfLook.Block("Carved scale mark", new Vector3(0, .05f, -.54f), new Vector3(.55f, .12f, .035f), .01f, starOnMat, house.transform);
            }
            for (int i = 0; i < 18; i++)
            {
                float x = -10.8f + i * 1.27f;
                MgfLook.Block("Brass alley rail", new Vector3(x, .48f, 2.0f + (i % 2) * 1.1f), new Vector3(.08f, .72f, 1.05f), .02f, nickelMat, worldRoot);
            }
        }

        void BuildAimTowers()
        {
            for (int i = 0; i < towerHeads.Length; i++)
            {
                float x = -9.0f + i * 3.0f;
                float z = 5.7f + (i % 2) * 1.15f;
                float h = 2.0f + (i % 3) * .42f;
                var tower = MgfLook.Block("Terracotta observation tower " + i, new Vector3(x, h * .5f, z),
                    new Vector3(1.15f, h, 1.15f), .04f, i % 2 == 0 ? basaltMat : basaltLightMat, worldRoot).transform;
                towerHeads[i] = new GameObject("Aiming brass head " + i).transform;
                towerHeads[i].SetParent(tower, false); towerHeads[i].localPosition = new Vector3(0, h * .55f, 0);
                MgfLook.Block("Angular telescope " + i, new Vector3(0, .10f, -.72f), new Vector3(.34f, .34f, 1.55f), .035f, nickelMat, towerHeads[i]);
                towerRays[i] = MgfLook.Block("Starlight ray " + i, new Vector3(0, .10f, -1.72f), new Vector3(.055f, .055f, 2.0f), .01f, starOffMat, towerHeads[i]).transform;
                var lamp = MgfLook.Prim(PrimitiveType.Sphere, "Tower star lens " + i, new Vector3(0, .10f, -2.72f), Vector3.one * .28f, starOffMat, towerHeads[i]);
                towerLamps[i] = lamp.GetComponent<Renderer>();
            }
        }

        void BuildObservatoryRings()
        {
            titleRing = NewMeshObject("Great brass meridian", Torus(5.5f, .18f, 64, 8), nickelMat, worldRoot).transform;
            titleRing.localPosition = new Vector3(0, 5.0f, 5.8f);
            titleRing.localRotation = Quaternion.Euler(68f, 0, 0);
            orbitA = NewMeshObject("Outer brass calendar rail", Torus(8.7f, .055f, 64, 5), nickelMat, worldRoot).transform;
            orbitB = NewMeshObject("Inner cobalt calendar rail", Torus(6.7f, .045f, 64, 5), MgfLook.Lit(Violet, .10f, 0f), worldRoot).transform;
            orbitA.localPosition = new Vector3(0, 3.9f, 6f); orbitA.localRotation = Quaternion.Euler(74f, 12f, 0);
            orbitB.localPosition = new Vector3(0, 4.3f, 6f); orbitB.localRotation = Quaternion.Euler(64f, -20f, 0);
            for (int i = 0; i < orbiters.Length; i++)
            {
                float a = i * Mathf.PI * 2f / orbiters.Length;
                orbiters[i] = MgfLook.Block("Engraved calendar marker", new Vector3(Mathf.Cos(a) * (i % 2 == 0 ? 8.7f : 6.7f), 0, Mathf.Sin(a) * (i % 2 == 0 ? 8.7f : 6.7f)),
                    Vector3.one * (.22f + (i % 3) * .05f), .025f, i % 3 == 0 ? starOnMat : opalMat, i % 2 == 0 ? orbitA : orbitB).transform;
            }
        }

        Transform BuildRack(string name, int teeth)
        {
            var root = new GameObject(name).transform;
            MgfLook.Block("angular brass rack spine", Vector3.zero, new Vector3(6.0f, .34f, .58f), .035f, nickelMat, root);
            for (int i = 0; i < teeth; i++)
            {
                float x = (i - (teeth - 1) * .5f) * .92f;
                var tooth = MgfLook.Block("engraved terracotta tooth", new Vector3(x, -.24f, .32f), new Vector3(.54f, .72f, 1.05f), .035f, i % 2 == 0 ? basaltLightMat : opalMat, root);
                tooth.transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? -2.5f : 2.5f));
                MgfLook.Block("tooth scale mark", new Vector3(0, -.20f, -.54f), new Vector3(.28f, .07f, .03f), .005f, starOnMat, tooth.transform);
            }
            MgfLook.Block("brass latch left", new Vector3(-3.08f, 0, 0), new Vector3(.48f, .48f, .48f), .025f, nickelMat, root);
            MgfLook.Block("brass latch right", new Vector3(3.08f, 0, 0), new Vector3(.48f, .48f, .48f), .025f, nickelMat, root);
            return root;
        }

        void BuildContactStars()
        {
            for (int i = 0; i < starNodes.Length; i++)
            {
                var n = NewMeshObject("Star contact " + i, StarMesh(.24f, .11f), starOffMat, worldRoot);
                starNodes[i] = n.transform; starRenderers[i] = n.GetComponent<Renderer>();
                starNodes[i].gameObject.SetActive(false);
            }
        }

        void BuildNova()
        {
            nova = new GameObject("Gakso angular observatory runner").transform;
            nova.SetParent(worldRoot, false); nova.localPosition = new Vector3(-4.6f, 1.15f, 2.6f);
            var body = MgfLook.Block("Gakso faceted body", Vector3.zero, new Vector3(1.35f, .48f, .68f), .04f, cobaltMat, nova);
            body.transform.localRotation = Quaternion.Euler(0, 0, -8f);
            var head = MgfLook.Block("Gakso prism head", new Vector3(.75f, .12f, 0), new Vector3(.72f, .62f, .64f), .035f, opalMat, nova);
            MgfLook.Block("Gakso eye L", new Vector3(.18f, .20f, -.31f), Vector3.one * .18f, .015f, starOnMat, head.transform);
            MgfLook.Block("Gakso eye R", new Vector3(.18f, .20f, .31f), Vector3.one * .18f, .015f, starOnMat, head.transform);
            novaTail = new GameObject("Brass segmented tail").transform; novaTail.SetParent(nova, false); novaTail.localPosition = new Vector3(-.7f, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                var seg = MgfLook.Block("tail segment", new Vector3(-.34f - i * .38f, .05f + i * .08f, 0),
                    new Vector3(.13f - i * .018f, .42f, .13f - i * .018f), .015f, i == 3 ? starOnMat : nickelMat, novaTail);
                seg.transform.localRotation = Quaternion.Euler(0, 0, 70f - i * 8f);
            }
            MgfLook.Block("tail prism", new Vector3(-1.85f, .56f, 0), Vector3.one * .38f, .025f, starOnMat, novaTail);
            for (int i = 0; i < 4; i++)
            {
                float sx = i < 2 ? -.28f : .38f, sz = i % 2 == 0 ? -.34f : .34f;
                novaLimbs[i] = MgfLook.Block("Gakso leg", new Vector3(sx, -.25f, sz),
                    new Vector3(.10f, .42f, .10f), .012f, nickelMat, nova).transform;
                novaLimbs[i].localRotation = Quaternion.Euler(sz > 0 ? 62f : -62f, 0, sx > 0 ? 28f : -28f);
            }
        }

        void BuildDome()
        {
            dome = new GameObject("Central seven tower star focus").transform; dome.SetParent(worldRoot, false); dome.localPosition = new Vector3(0, .1f, 5.4f);
            for (int i = 0; i < 6; i++)
            {
                float a = (-75f + i * 30f) * Mathf.Deg2Rad;
                var shard = MgfLook.Block("Brass focus vane " + i, new Vector3(Mathf.Sin(a) * 3.4f, 2.1f + Mathf.Cos(a) * .7f, Mathf.Cos(a) * 1.2f),
                    new Vector3(.36f, 2.7f, .14f), .025f, i % 2 == 0 ? nickelMat : glassMat, dome);
                domeShards[i] = shard.transform;
                domeShardBaseRot[i] = Quaternion.Euler(-18f, -a * Mathf.Rad2Deg, -a * Mathf.Rad2Deg * .12f);
                domeShards[i].localRotation = domeShardBaseRot[i];
            }
        }

        void BuildUi()
        {
            discSprite = MakeDisc(false); ringSprite = MakeDisc(true); panelSprite = MakePanel(); starSprite = MakeStarSprite(); crackSprite = MakeCrackSprite();
            uiRoot = new GameObject("Byeolkan UI", typeof(RectTransform)).GetComponent<RectTransform>();
            uiRoot.SetParent(MgfText.Canvas.transform, false); uiRoot.anchorMin = Vector2.zero; uiRoot.anchorMax = Vector2.one; uiRoot.offsetMin = uiRoot.offsetMax = Vector2.zero;
            scaler = MgfText.Canvas.GetComponent<CanvasScaler>();

            titleUi = R("Title world overlay", uiRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            titleUi.anchorMin = Vector2.zero; titleUi.anchorMax = Vector2.one; titleUi.offsetMin = titleUi.offsetMax = Vector2.zero;
            titleGroup = titleUi.gameObject.AddComponent<CanvasGroup>();
            var villageBanner = Img("Brass meridian sign", titleUi, new Vector2(.5f,.72f), Vector2.zero, new Vector2(342,176), new Color(.10f,.16f,.24f,.96f), panelSprite);
            titleBannerRt = villageBanner.rectTransform;
            Img("Scale tooth L", villageBanner.transform, new Vector2(.06f,.04f), Vector2.zero, new Vector2(18,55), new Color(Star.r,Star.g,Star.b,.95f), panelSprite);
            Img("Scale tooth R", villageBanner.transform, new Vector2(.94f,.04f), Vector2.zero, new Vector2(18,55), new Color(Star.r,Star.g,Star.b,.95f), panelSprite);
            logoText = Txt("Painted village logo", villageBanner.transform, "별칸\n관측소", new Vector2(.5f, .60f), Vector2.zero, 47, Pale, 315, TextAlignmentOptions.Center);
            logoText.characterSpacing = 5; logoText.outlineWidth = .10f; logoText.outlineColor = Night;
            taglineText = Txt("Tagline", villageBanner.transform, "두 랙을 엮어 별칸을 밝히시오", new Vector2(.5f,.13f), Vector2.zero, 15, new Color32(255,224,166,255), 320);
            titleMetaText = Txt("Title meta", titleUi, "중학교 2학년 · 경우의 수    최고 최초 정답 0/6", new Vector2(.5f, .055f), Vector2.zero, 12, new Color(Night.r,Night.g,Night.b,.86f), 370);
            titleHandleImage = Img("Meridian observatory wheel", titleUi, new Vector2(.5f, .28f), Vector2.zero, new Vector2(146, 146), new Color(Star.r,Star.g,Star.b,.96f), ringSprite);
            titleHandleRt = titleHandleImage.rectTransform;
            titleHandleText = Txt("Handle label", titleUi, "황동 관측환\n눌러 시작", new Vector2(.5f, .28f), Vector2.zero, 16, Night, 130);

            gameUi = R("Game overlay", uiRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            gameUi.anchorMin = Vector2.zero; gameUi.anchorMax = Vector2.one; gameUi.offsetMin = gameUi.offsetMax = Vector2.zero;
            gameGroup = gameUi.gameObject.AddComponent<CanvasGroup>();
            hudStripRt = Img("HUD brass ledger", gameUi, new Vector2(.42f, .965f), Vector2.zero, new Vector2(318, 46), new Color(.08f,.13f,.20f,.96f), panelSprite).rectTransform;
            progressText = Txt("Progress", gameUi, "관측 1/6", new Vector2(.11f, .965f), Vector2.zero, 12, Pale, 82);
            livesText = Txt("Seals", gameUi, "봉인 ●●●", new Vector2(.32f, .965f), Vector2.zero, 12, new Color32(255,224,166,255), 96);
            scoreText = Txt("Score", gameUi, "0", new Vector2(.53f, .965f), Vector2.zero, 15, Star, 58);
            timerText = Txt("Timer", gameUi, "110초", new Vector2(.70f, .965f), Vector2.zero, 12, Pale, 62);
            goalText = Txt("Goal", gameUi, "두 사건을 엮어 정확한 별칸을 밝히시오.", new Vector2(.5f, .908f), Vector2.zero, 14, Pale, 370);
            goalRt = goalText.rectTransform;
            var promptPlate = Img("Prompt carved tablet", gameUi, new Vector2(.5f, .815f), Vector2.zero, new Vector2(365, 92), new Color(.94f,.79f,.57f,.98f), panelSprite);
            promptPlateRt = promptPlate.rectTransform;
            promptText = Txt("Prompt", promptPlate.transform, "", new Vector2(.5f, .56f), Vector2.zero, 15, Night, 337, TextAlignmentOptions.Center);
            conceptText = Txt("Concept", promptPlate.transform, "", new Vector2(.5f, .14f), Vector2.zero, 11, Violet, 330);

            boardRt = R("Terracotta outcome map", gameUi, new Vector2(.5f, .49f), Vector2.zero, new Vector2(350, 338));
            boardPlateImage = Img("Mosaic map field", boardRt, new Vector2(.5f,.5f), Vector2.zero, new Vector2(350,338), new Color(.47f,.24f,.19f,.96f), panelSprite);
            endDockLeft = Img("End dock A", boardRt, new Vector2(.12f,.54f), Vector2.zero, new Vector2(82, 218), new Color(.31f,.56f,.58f,.34f), panelSprite).rectTransform;
            crossDock = Img("Cross dock", boardRt, new Vector2(.5f,.54f), Vector2.zero, new Vector2(210,210), new Color(Star.r,Star.g,Star.b,.28f), ringSprite).rectTransform;
            endDockRight = Img("End dock B", boardRt, new Vector2(.88f,.54f), Vector2.zero, new Vector2(82,218), new Color(.31f,.56f,.58f,.34f), panelSprite).rectTransform;
            endDockLeftText = Txt("End dock A label", boardRt, "끝접속 A 홈", new Vector2(.13f,.94f), Vector2.zero, 10, Night, 92);
            crossDockText = Txt("Cross dock label", boardRt, "① 중앙 교차 홈", new Vector2(.5f,.94f), Vector2.zero, 11, Night, 150);
            endDockRightText = Txt("End dock B label", boardRt, "끝접속 B 홈", new Vector2(.87f,.94f), Vector2.zero, 10, Night, 92);
            for (int i = 0; i < cellRt.Length; i++)
            {
                cellImage[i] = Img("Contact " + i, boardRt, new Vector2(.5f,.5f), Vector2.zero, new Vector2(44,44), new Color(.91f,.78f,.59f,.98f), starSprite);
                cellRt[i] = cellImage[i].rectTransform;
                cellText[i] = Txt("Pair label " + i, cellRt[i], "", new Vector2(.5f,.5f), Vector2.zero, 12, Night, 52);
                cellRt[i].gameObject.SetActive(false);
            }
            foldImage = Img("Fold handle", gameUi, new Vector2(.91f,.49f), Vector2.zero, new Vector2(54,112), Violet, panelSprite);
            foldRt = foldImage.rectTransform;
            Txt("Fold text", foldRt, "접\n기", new Vector2(.5f,.5f), Vector2.zero, 14, Night, 40);

            rackHandleA = BuildRackHandle("A 랙 출발", new Vector2(.22f,.17f));
            rackHandleB = BuildRackHandle("B 랙 출발", new Vector2(.57f,.17f));
            crankImage = Img("Crank", gameUi, new Vector2(.86f,.17f), Vector2.zero, new Vector2(92,92), Star, ringSprite);
            crankRt = crankImage.rectTransform;
            Txt("Crank text", crankRt, "관측\n크랭크", new Vector2(.5f,.5f), Vector2.zero, 13, Night, 78);
            actionText = Txt("Action", gameUi, "", new Vector2(.5f,.085f), Vector2.zero, 12, Pale, 360);
            actionRt = actionText.rectTransform;

            guideImage = Img("Guide pulse", gameUi, new Vector2(.5f,.5f), Vector2.zero, new Vector2(62,62), new Color(Star.r,Star.g,Star.b,.70f), ringSprite);
            guideRt = guideImage.rectTransform;
            guideTrailRt = R("Guide path", gameUi, new Vector2(.5f,.5f), Vector2.zero, Vector2.zero);
            for (int i = 0; i < trailDots.Length; i++)
            {
                trailImages[i] = Img("Guide dot " + i, guideTrailRt, new Vector2(.5f,.5f), Vector2.zero, new Vector2(8,8), new Color(Star.r,Star.g,Star.b,.54f), discSprite);
                trailDots[i] = trailImages[i].rectTransform;
            }
            guideHandRt = Img("Animated drag hand", gameUi, new Vector2(.5f,.5f), Vector2.zero, new Vector2(58,58), new Color(Pale.r,Pale.g,Pale.b,.96f), ringSprite).rectTransform;
            guideHandText = Txt("Drag hand label", guideHandRt, "끌기", new Vector2(.5f,.5f), Vector2.zero, 11, Night, 52);
            guideCaptionRt = Img("Local action ledger", gameUi, new Vector2(.5f,.315f), Vector2.zero, new Vector2(292,40), new Color(.08f,.13f,.20f,.97f), panelSprite).rectTransform;
            guideCaptionText = Txt("Drag instruction", guideCaptionRt, "A 랙 출발 → 중앙 교차 홈", new Vector2(.5f,.5f), Vector2.zero, 12, Pale, 278);
            rippleImage = Img("Input ripple", gameUi, new Vector2(0,0), Vector2.zero, new Vector2(60,60), new Color(1,1,1,0), ringSprite);
            rippleRt = rippleImage.rectTransform;
            toastPlate = Img("Refusal plate", gameUi, new Vector2(.5f,.29f), Vector2.zero, new Vector2(330,52), new Color(.10f,.08f,.16f,.95f), panelSprite);
            toastRt = toastPlate.rectTransform; toastGroup = toastRt.gameObject.AddComponent<CanvasGroup>();
            toastText = Txt("Refusal text", toastRt, "", new Vector2(.5f,.5f), Vector2.zero, 12, Pale, 302);
            revealPlate = Img("Math reveal", gameUi, new Vector2(.5f,.49f), Vector2.zero, new Vector2(336,105), new Color(.05f,.08f,.15f,.97f), panelSprite);
            revealRt = revealPlate.rectTransform; revealGroup = revealRt.gameObject.AddComponent<CanvasGroup>();
            revealText = Txt("Reveal text", revealRt, "", new Vector2(.5f,.5f), Vector2.zero, 20, Star, 308);

            endUi = R("Result observatory", uiRoot, new Vector2(.5f,.5f), Vector2.zero, Vector2.zero);
            endUi.anchorMin = Vector2.zero; endUi.anchorMax = Vector2.one; endUi.offsetMin = endUi.offsetMax = Vector2.zero;
            endGroup = endUi.gameObject.AddComponent<CanvasGroup>();
            Img("Result mosaic ledger", endUi, new Vector2(.5f,.56f), Vector2.zero, new Vector2(358,292), new Color(.94f,.79f,.57f,.98f), panelSprite);
            Img("Result meridian wreath", endUi, new Vector2(.5f,.56f), Vector2.zero, new Vector2(300,300), new Color(.10f,.16f,.24f,.58f), ringSprite);
            endTitle = Txt("Result title", endUi, "", new Vector2(.5f,.66f), Vector2.zero, 35, Night, 340);
            endStats = Txt("Result stats", endUi, "", new Vector2(.5f,.51f), Vector2.zero, 16, new Color32(122,70,53,255), 330);
            endStats.rectTransform.sizeDelta = new Vector2(330, 108);
            endStats.overflowMode = TextOverflowModes.Overflow;
            endStats.enableAutoSizing = true;
            endStats.fontSizeMin = 12f;
            endStats.fontSizeMax = 16f;
            restartRt = Img("Restart orbit", endUi, new Vector2(.5f,.31f), Vector2.zero, new Vector2(170,92), new Color(.86f,.90f,.88f,.92f), ringSprite).rectTransform;
            endCta = Txt("Restart label", endUi, "다시 관측하기", new Vector2(.5f,.31f), Vector2.zero, 17, Night, 160);
            sealImage = Img("Seal crack", endUi, new Vector2(.5f,.16f), Vector2.zero, new Vector2(92,92), new Color(Wrong.r,Wrong.g,Wrong.b,.75f), crackSprite);

            LayoutUi(true);
        }

        RectTransform BuildRackHandle(string label, Vector2 anchor)
        {
            var rt = Img(label + " handle", gameUi, anchor, Vector2.zero, new Vector2(132,112), new Color(.16f,.22f,.25f,.99f), panelSprite).rectTransform;
            for (int i = 0; i < 5; i++) Img("engraved tooth", rt, new Vector2(.18f + i * .16f,.24f), Vector2.zero, new Vector2(13,34), i % 2 == 0 ? Star : Opal, panelSprite);
            Txt("label", rt, label, new Vector2(.5f,.74f), Vector2.zero, 13, Pale, 122);
            Txt("grab label", rt, "여기서 잡기", new Vector2(.5f,.50f), Vector2.zero, 10, new Color32(255,224,166,255), 120);
            return rt;
        }

        void LayoutUi(bool force = false)
        {
            if (!force && lastScreenW == Screen.width && lastScreenH == Screen.height) return;
            lastScreenW = Screen.width; lastScreenH = Screen.height;
            wideLayout = Screen.width >= 1024 && (float)Screen.width / Screen.height > 1.25f;
            scaler.matchWidthOrHeight = wideLayout ? .62f : .5f;
            titleBannerRt.anchorMin = titleBannerRt.anchorMax = wideLayout ? new Vector2(.5f,.72f) : new Vector2(.5f,.72f);
            titleBannerRt.sizeDelta = wideLayout ? new Vector2(430,190) : new Vector2(342,176);
            titleHandleRt.anchorMin = titleHandleRt.anchorMax = wideLayout ? new Vector2(.5f,.29f) : new Vector2(.5f,.28f);
            titleHandleText.rectTransform.anchorMin = titleHandleText.rectTransform.anchorMax = titleHandleRt.anchorMin;

            Vector2 boardSize = wideLayout ? new Vector2(610,360) : new Vector2(350,338);
            boardRt.sizeDelta = boardSize;
            boardPlateImage.rectTransform.sizeDelta = boardSize;
            boardRt.anchorMin = boardRt.anchorMax = wideLayout ? new Vector2(.55f,.55f) : new Vector2(.5f,.49f);
            rackHandleA.anchorMin = rackHandleA.anchorMax = wideLayout ? new Vector2(.40f,.105f) : new Vector2(.22f,.17f);
            rackHandleB.anchorMin = rackHandleB.anchorMax = wideLayout ? new Vector2(.58f,.105f) : new Vector2(.57f,.17f);
            crankRt.anchorMin = crankRt.anchorMax = wideLayout ? new Vector2(.87f,.11f) : new Vector2(.86f,.17f);
            foldRt.anchorMin = foldRt.anchorMax = wideLayout ? new Vector2(.83f,.58f) : new Vector2(.91f,.49f);
            guideCaptionRt.anchorMin = guideCaptionRt.anchorMax = wideLayout ? new Vector2(.55f,.235f) : new Vector2(.5f,.315f);

            hudStripRt.anchorMin = hudStripRt.anchorMax = wideLayout ? new Vector2(.50f,.965f) : new Vector2(.42f,.965f);
            hudStripRt.sizeDelta = wideLayout ? new Vector2(520,46) : new Vector2(318,46);
            progressText.rectTransform.anchorMin = progressText.rectTransform.anchorMax = wideLayout ? new Vector2(.15f,.965f) : new Vector2(.11f,.965f);
            livesText.rectTransform.anchorMin = livesText.rectTransform.anchorMax = wideLayout ? new Vector2(.36f,.965f) : new Vector2(.32f,.965f);
            scoreText.rectTransform.anchorMin = scoreText.rectTransform.anchorMax = wideLayout ? new Vector2(.55f,.965f) : new Vector2(.53f,.965f);
            timerText.rectTransform.anchorMin = timerText.rectTransform.anchorMax = wideLayout ? new Vector2(.73f,.965f) : new Vector2(.70f,.965f);

            goalRt.anchorMin = goalRt.anchorMax = wideLayout ? new Vector2(.15f,.895f) : new Vector2(.5f,.908f);
            goalRt.sizeDelta = wideLayout ? new Vector2(250,74) : new Vector2(370,43);
            promptPlateRt.anchorMin = promptPlateRt.anchorMax = wideLayout ? new Vector2(.15f,.68f) : new Vector2(.5f,.815f);
            promptPlateRt.sizeDelta = wideLayout ? new Vector2(260,190) : new Vector2(365,92);
            promptText.rectTransform.sizeDelta = wideLayout ? new Vector2(236,126) : new Vector2(337,46.5f);
            promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = wideLayout ? new Vector2(.5f,.60f) : new Vector2(.5f,.56f);
            conceptText.rectTransform.sizeDelta = wideLayout ? new Vector2(232,56) : new Vector2(330,34.1f);
            actionRt.anchorMin = actionRt.anchorMax = wideLayout ? new Vector2(.15f,.40f) : new Vector2(.5f,.085f);
            actionRt.sizeDelta = wideLayout ? new Vector2(250,74) : new Vector2(360,37.2f);
            if (current != null) LayoutCells();
        }

        void LayoutCells()
        {
            float w = boardRt.rect.width, h = boardRt.rect.height;
            float marginX = wideLayout ? 72f : 34f, marginY = wideLayout ? 48f : 34f;
            float cw = (w - marginX * 2f) / Mathf.Max(1,current.cols);
            float ch = (h - marginY * 2f) / Mathf.Max(1,current.rows);
            float size = Mathf.Clamp(Mathf.Min(cw,ch) * .94f, 44f, wideLayout ? 74f : 58f);
            for (int y = 0; y < current.rows; y++) for (int x = 0; x < current.cols; x++)
            {
                int i = y * current.cols + x;
                cellRt[i].anchorMin = cellRt[i].anchorMax = new Vector2(.5f,.5f);
                cellRt[i].anchoredPosition = new Vector2((x-(current.cols-1)*.5f)*cw, ((current.rows-1)*.5f-y)*ch);
                cellRt[i].sizeDelta = Vector2.one * size;
                cellText[i].fontSize = wideLayout ? (current.rows >= 6 ? 15f : 17f) : (current.rows >= 6 ? 12f : current.rows >= 5 ? 12.5f : 13f);
            }
        }

        void RefreshProblemUi()
        {
            promptText.text = current.prompt;
            conceptText.text = current.concept;
            actionText.text = CurrentInstruction();
            foldRt.gameObject.SetActive(current.needsFold);
            activeCells = Mathf.Min(36, current.rows * current.cols);
            for (int i = 0; i < cellRt.Length; i++)
            {
                bool active = i < activeCells;
                cellRt[i].gameObject.SetActive(active);
                if (!active) continue;
                int y = i / current.cols, x = i % current.cols;
                if (current.kind == ObservationKind.DiceSum || current.kind == ObservationKind.DigitCards || current.kind == ObservationKind.Representatives)
                    cellText[i].text = "(" + Strip(current.rowLabels[y]) + "," + Strip(current.colLabels[x]) + ")";
                else cellText[i].text = current.rows == 1 ? current.colLabels[x] : (y+1) + "·" + (x+1);
                cellImage[i].color = new Color(.91f,.78f,.59f,.98f);
                cellText[i].color = Night;
            }
            LayoutUi(true);
            PositionGuide();
            RefreshHud();
        }

        string Strip(string s)
        {
            int p = s.LastIndexOf(' ');
            return p >= 0 ? s.Substring(p + 1) : s;
        }

        void RefreshHud()
        {
            progressText.text = st.onboarding ? "고정 연습" : "관측 " + Mathf.Clamp(st.solved + 1,1,6) + "/6";
            livesText.text = "봉인 " + (st.lives >= 1 ? "●" : "○") + (st.lives >= 2 ? "●" : "○") + (st.lives >= 3 ? "●" : "○");
            timerText.text = st.onboarding ? "시간 정지" : st.remainingTime + "초";
            actionText.text = current == null ? "" : CurrentInstruction();
        }

        void SetScreen()
        {
            bool title = flow == Flow.Title, end = flow == Flow.End;
            pavilionFoldTarget = title ? .62f : end && st.phase == "clear" ? 1f : 0f;
            titleGroup.alpha = title ? 1 : 0; titleGroup.blocksRaycasts = title;
            gameGroup.alpha = !title && !end ? 1 : 0; gameGroup.blocksRaycasts = !title && !end;
            endGroup.alpha = end ? 1 : 0; endGroup.blocksRaycasts = end;
            revealGroup.alpha = 0; toastGroup.alpha = 0;
            if (title)
            {
                rackTargetPos[0] = new Vector3(-2.2f, 4.4f, 1.9f); rackTargetRot[0] = Quaternion.Euler(0,20,10);
                rackTargetPos[1] = new Vector3(2.0f, 4.1f, 2.2f); rackTargetRot[1] = Quaternion.Euler(0,100,-8);
            }
        }

        void ResetWorldForRun()
        {
            wrongShake = 0; novaRun = -1f; displayedScore = 0;
            pavilionFold = pavilionFoldTarget = 0f; towerAim = -1f;
            for (int i = 0; i < starNodes.Length; i++) { starNodes[i].gameObject.SetActive(false); starRenderers[i].sharedMaterial = starOffMat; }
            for (int i = 0; i < towerLamps.Length; i++) if (towerLamps[i])
            {
                towerLamps[i].sharedMaterial = starOffMat;
                if (towerRays[i]) towerRays[i].GetComponent<Renderer>().sharedMaterial = starOffMat;
            }
        }

        void ResetWorldForProblem()
        {
            // 각 관측의 실제 부품은 서랍에서 다시 시작한다. 이전 드래그 좌표를 재사용하면
            // 다음 문제에서 손잡이가 보드 위에 남아 조작 경로가 끊긴다.
            rackHandleA.anchoredPosition = Vector2.zero;
            rackHandleB.anchoredPosition = Vector2.zero;
            rackHandleA.localRotation = rackHandleB.localRotation = Quaternion.identity;
            rackHandleA.GetComponent<Image>().color = new Color(.16f,.22f,.25f,.99f);
            rackHandleB.GetComponent<Image>().color = new Color(.16f,.22f,.25f,.99f);
            foldRt.localRotation = Quaternion.identity;
            crankImage.color = Star;
            DockRack(0, RackTopology.None); DockRack(1, RackTopology.None);
            for (int i = 0; i < starNodes.Length; i++)
            {
                bool active = i < activeCells;
                starNodes[i].gameObject.SetActive(active);
                starRenderers[i].sharedMaterial = starOffMat;
            }
            PositionWorldStars();
            ClearFeedbackVisuals();
            novaRun = -1f;
            pavilionFoldTarget = 0f;
        }

        void PositionWorldStars()
        {
            if (current == null) return;
            float sx = current.cols <= 4 ? 1.15f : .76f, sz = current.rows <= 4 ? 1.05f : .72f;
            for (int y = 0; y < current.rows; y++) for (int x = 0; x < current.cols; x++)
            {
                int i = y * current.cols + x;
                starWorld[i] = new Vector3((x-(current.cols-1)*.5f)*sx, .98f, 1.5f+(y-(current.rows-1)*.5f)*sz);
                starNodes[i].localPosition = starWorld[i]; starNodes[i].localRotation = Quaternion.Euler(90,0,0);
            }
        }

        void DockRack(int rack, RackTopology topology)
        {
            if (topology == RackTopology.None)
            {
                rackTargetPos[rack] = new Vector3(rack == 0 ? -4.5f : 4.5f, .72f, -2.8f);
                rackTargetRot[rack] = Quaternion.Euler(0, rack == 0 ? 5f : 175f, 0);
            }
            else if (topology == RackTopology.Crossed)
            {
                rackTargetPos[rack] = new Vector3(0, rack == 0 ? .68f : .84f, 1.45f);
                rackTargetRot[rack] = Quaternion.Euler(0, rack == 0 ? 0 : 90f, 0);
            }
            else
            {
                rackTargetPos[rack] = new Vector3(rack == 0 ? -3.0f : 3.0f, rack == 0 ? .7f : .74f, 1.45f);
                rackTargetRot[rack] = Quaternion.Euler(0, 0, 0);
            }
            if (rackHandleA && topology != RackTopology.None) SnapRackHandleUi(rack, topology);
        }

        void SnapRackHandleUi(int rack, RackTopology topology)
        {
            RectTransform rt = rack == 0 ? rackHandleA : rackHandleB;
            Vector2 home = wideLayout ? (rack == 0 ? new Vector2(.40f,.105f) : new Vector2(.58f,.105f))
                                      : (rack == 0 ? new Vector2(.22f,.17f) : new Vector2(.57f,.17f));
            rt.anchorMin = rt.anchorMax = home;
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            rt.GetComponent<Image>().color = topology == RackTopology.Crossed ? new Color(Cobalt.r,Cobalt.g,Cobalt.b,.98f)
                                                                              : new Color(Star.r,Star.g,Star.b,.98f);
        }

        void SnapBoardToState()
        {
            DockRack(0, rackA); DockRack(1, rackB);
            rackWorldA.localPosition = rackTargetPos[0]; rackWorldA.localRotation = rackTargetRot[0];
            rackWorldB.localPosition = rackTargetPos[1]; rackWorldB.localRotation = rackTargetRot[1];
            for (int i = 0; i < activeCells; i++) CellPressed(i, marked[i]);
            FoldBoard(folded);
        }

        void UpdateWorld(float dt)
        {
            worldTime += dt;
            if (orbitA) orbitA.Rotate(Vector3.up, dt * 4.2f, Space.Self);
            if (orbitB) orbitB.Rotate(Vector3.up, -dt * 6.1f, Space.Self);
            if (titleRing) titleRing.localRotation = Quaternion.Euler(68f + Mathf.Sin(worldTime*.22f)*2f, worldTime * 1.4f, 0);
            if (dome) dome.localRotation = Quaternion.Euler(0, Mathf.Sin(worldTime*.12f)*5f,0);
            pavilionFold = Mathf.MoveTowards(pavilionFold, pavilionFoldTarget, dt * 1.9f);
            for (int i = 0; i < domeShards.Length; i++) if (domeShards[i])
            {
                float fan = (i - 2.5f) * 4.5f;
                domeShards[i].localRotation = domeShardBaseRot[i] * Quaternion.Euler(58f * pavilionFold, 0, fan * pavilionFold);
                domeShards[i].localScale = new Vector3(1f, .78f + .22f * pavilionFold, 1f);
            }

            Transform[] racks = { rackWorldA, rackWorldB };
            for (int i = 0; i < 2; i++)
            {
                racks[i].localPosition = Vector3.SmoothDamp(racks[i].localPosition, rackTargetPos[i], ref rackVelocity[i], .16f, 38f, dt);
                racks[i].localRotation = Quaternion.Slerp(racks[i].localRotation, rackTargetRot[i], 1f-Mathf.Exp(-dt*12f));
                float pulse = dockPulse > 0 ? Mathf.Sin((1f-dockPulse)*Mathf.PI)*.09f : 0;
                racks[i].localScale = Vector3.one * (1f+pulse);
            }
            dockPulse = Mathf.Max(0,dockPulse-dt*2.8f);

            if (towerAim >= 0f) towerAim += dt * 1.05f;
            Vector3 starFocus = new Vector3(0f, 2.25f, 1.35f);
            for (int i = 0; i < towerHeads.Length; i++) if (towerHeads[i])
            {
                bool aimed = towerAim >= i * .16f;
                Vector3 dir = starFocus - towerHeads[i].position;
                Quaternion desired = aimed
                    ? Quaternion.LookRotation(-dir.normalized, Vector3.up)
                    : Quaternion.Euler(-8f, Mathf.Sin(worldTime * .42f + i) * 24f, 0f);
                towerHeads[i].rotation = Quaternion.Slerp(towerHeads[i].rotation, desired, 1f - Mathf.Exp(-dt * (aimed ? 7f : 1.7f)));
                Material beamMat = aimed ? starOnMat : starOffMat;
                if (towerLamps[i].sharedMaterial != beamMat)
                {
                    towerLamps[i].sharedMaterial = beamMat;
                    towerRays[i].GetComponent<Renderer>().sharedMaterial = beamMat;
                }
                float rayGrow = aimed ? Mathf.Clamp01((towerAim - i * .16f) * 4f) : 0f;
                towerRays[i].localScale = new Vector3(1f, 1f, Mathf.Lerp(.04f, 1f, rayGrow));
            }

            if (nova)
            {
                if (novaRun >= 0 && current != null)
                {
                    novaRun += dt * (1.25f + st.combo * .08f);
                    int count = Mathf.Max(1, current.answerCount);
                    int idx = Mathf.Min(activeCells-1, Mathf.FloorToInt(novaRun * count) % Mathf.Max(1,activeCells));
                    if (current.cellAction != CellAction.None)
                    {
                        int wanted = Mathf.FloorToInt(novaRun * count) % count, seen = -1;
                        for (int i = 0; i < activeCells; i++)
                        {
                            bool lit = current.cellAction == CellAction.KeepTargets ? current.expectedCells[i] : !current.expectedCells[i];
                            if (lit && ++seen == wanted) { idx = i; break; }
                        }
                    }
                    nova.localPosition = Vector3.Lerp(nova.localPosition, starWorld[Mathf.Clamp(idx,0,35)] + Vector3.up*.48f, 1f-Mathf.Exp(-dt*9f));
                    nova.localRotation = Quaternion.Euler(0, Mathf.Sin(novaRun*7f)*18f, 0);
                }
                else
                {
                    nova.localPosition += Vector3.up * Mathf.Sin(worldTime*2.1f) * dt * .035f;
                    nova.localRotation = Quaternion.Euler(0, Mathf.Sin(worldTime*.8f)*12f, Mathf.Sin(worldTime*1.6f)*3f);
                }
                novaTail.localRotation = Quaternion.Euler(0, Mathf.Sin(worldTime*2.7f)*14f, Mathf.Sin(worldTime*1.9f)*10f);
                for (int i = 0; i < novaLimbs.Length; i++) novaLimbs[i].localRotation *= Quaternion.Euler(0,0,Mathf.Sin(worldTime*3f+i)*dt*6f);
            }

            Vector3 shake = wrongShake > 0 ? new Vector3(Mathf.Sin(worldTime*59f),Mathf.Cos(worldTime*47f),0)*wrongShake : Vector3.zero;
            wrongShake = Mathf.Max(0,wrongShake-dt*.42f);
            cam.transform.position = camBase + shake;
            cam.transform.LookAt(new Vector3(0,.5f,1.2f));
            cam.orthographicSize = wideLayout ? 8.0f : (Screen.width < Screen.height ? 10.1f : 8.7f);
        }

        void UpdateUi(float dt)
        {
            LayoutUi();
            if (displayedScore != st.score)
            {
                float next = Mathf.SmoothDamp(displayedScore, st.score, ref displayedScoreVelocity, .24f, 9999f, dt);
                int n = Mathf.Abs(st.score-next) < .6f ? st.score : Mathf.RoundToInt(next);
                if (n != displayedScore) { displayedScore = n; scoreText.text = displayedScore.ToString(); MgfFx.Punch(scoreText.rectTransform,.08f,.18f); }
            }
            rippleClock += dt;
            if (rippleClock < .42f)
            {
                float k = rippleClock/.42f; rippleRt.localScale = Vector3.one*(.45f+k*1.45f); rippleImage.color = new Color(Star.r,Star.g,Star.b,(1-k)*.7f);
            }
            else rippleImage.color = Color.clear;
            toastClock += dt; toastGroup.alpha = toastClock < 1.55f ? Mathf.Clamp01((1.55f-toastClock)*4f) : 0;
            revealClock += dt; revealGroup.alpha = revealClock < 1.2f ? Mathf.Clamp01((1.2f-revealClock)*5f) : 0;
            refuseClock += dt;
            if (refuseClock < .25f) toastRt.anchoredPosition = new Vector2(Mathf.Sin(refuseClock*85f)*5f*(1-refuseClock/.25f),0);
            else toastRt.anchoredPosition = Vector2.zero;
            guideBoost = Mathf.Max(0,guideBoost-dt*.45f);
            PositionGuide();
            float pulse = .84f + .16f*Mathf.Sin(worldTime*4.2f) + guideBoost*.15f;
            guideRt.localScale = Vector3.one*pulse;
            guideImage.color = new Color(Star.r,Star.g,Star.b,.46f+Mathf.Sin(worldTime*4.2f)*.18f);
            titleHandleRt.localScale = Vector3.one*(1f+Mathf.Sin(worldTime*2.4f)*.035f);
            restartRt.localRotation = Quaternion.Euler(0,0,Mathf.Sin(worldTime*.9f)*5f);
            crankRt.localRotation = Quaternion.Euler(0,0,-crankVisual);
            titleBannerRt.localRotation = Quaternion.Euler(0,0,Mathf.Sin(worldTime*.8f)*1.4f);
        }

        void PositionGuide()
        {
            if (flow != Flow.Practice && flow != Flow.Playing)
            {
                guideRt.gameObject.SetActive(false); guideTrailRt.gameObject.SetActive(false);
                guideHandRt.gameObject.SetActive(false); guideCaptionRt.gameObject.SetActive(false); return;
            }
            guideRt.gameObject.SetActive(true); guideTrailRt.gameObject.SetActive(true); guideHandRt.gameObject.SetActive(true);
            guideCaptionRt.gameObject.SetActive(flow == Flow.Practice || rackA == RackTopology.None || rackB == RackTopology.None);
            RectTransform target;
            RectTransform source = null;
            if (rackA == RackTopology.None) { source = rackHandleA; target = flow == Flow.Practice || current.expectedTopology == RackTopology.Crossed ? crossDock : endDockLeft; }
            else if (rackB == RackTopology.None) { source = rackHandleB; target = flow == Flow.Practice || current.expectedTopology == RackTopology.Crossed ? crossDock : endDockRight; }
            else if (current.needsFold && !folded) { source = foldRt; target = boardRt; }
            else target = crankRt;
            Vector2 targetLocal = RectCenterInGame(target);
            guideRt.anchoredPosition = targetLocal;
            if (source != null)
            {
                Vector2 start = RectCenterInGame(source);
                // Keep the moving cue beside the source control; the separate target ring marks
                // the home.  A full-width ribbon used to cover pair labels on the board.
                Vector2 localEnd = Vector2.Lerp(start, targetLocal, .34f);
                float handU = Mathf.SmoothStep(0f,1f,Mathf.PingPong(worldTime*.47f,1f));
                Vector2 handBend = (start+localEnd)*.5f + Vector2.up*24f;
                guideHandRt.anchoredPosition = Bezier(start,handBend,localEnd,handU);
                guideHandRt.localScale = Vector3.one * (.88f + Mathf.Sin(handU*Mathf.PI)*.18f);
                guideHandText.text = handU < .18f ? "잡기" : handU > .82f ? "놓기" : "끌기";
                guideCaptionText.text = source == rackHandleA ? "A 랙 출발 → 중앙 교차 홈" : source == rackHandleB ? "B 랙 출발 → 중앙 교차 홈" : "접이 손잡이 → 반대쪽";
                for (int i = 0; i < trailDots.Length; i++)
                {
                    float u = Mathf.Repeat(worldTime*.55f+i/(float)trailDots.Length,1f);
                    Vector2 bend = (start+localEnd)*.5f + Vector2.up*24f;
                    trailDots[i].anchoredPosition = Bezier(start,bend,localEnd,u);
                    trailImages[i].color = new Color(Star.r,Star.g,Star.b,.2f+.7f*Mathf.Sin(u*Mathf.PI));
                }
            }
            else
            {
                guideHandRt.anchoredPosition = targetLocal + new Vector2(Mathf.Cos(worldTime*3f)*24f,Mathf.Sin(worldTime*3f)*24f);
                guideHandText.text = flow == Flow.Practice ? "누르기" : "돌리기";
                guideCaptionRt.gameObject.SetActive(flow == Flow.Practice);
                if (flow == Flow.Practice) guideCaptionText.text = "B 랙 시범 완료 · 크랭크를 눌러 관측";
                for (int i = 0; i < trailDots.Length; i++) trailImages[i].color = Color.clear;
            }
        }

        Vector2 RectCenterInGame(RectTransform rt)
        {
            Vector3 screen = RectTransformUtility.WorldToScreenPoint(null,rt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(gameUi,screen,null,out Vector2 local);
            return local;
        }

        Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float q=1-t; return q*q*a+2*q*t*b+t*t*c;
        }

        void CorrectFeedback(string reveal, int gain)
        {
            revealText.text = reveal + "\n관측탑 일곱 대 조준 +" + gain;
            revealText.color = Star; revealClock = 0; revealGroup.alpha = 1;
            for (int i = 0; i < activeCells; i++)
            {
                bool lit = current.cellAction == CellAction.KeepTargets ? current.expectedCells[i]
                    : current.cellAction == CellAction.None || !current.expectedCells[i];
                if (lit)
                {
                    starRenderers[i].sharedMaterial = i % 2 == 0 ? opalMat : basaltMat;
                    cellImage[i].color = i % 2 == 0 ? new Color(Opal.r,Opal.g,Opal.b,.98f) : new Color(Cobalt.r,Cobalt.g,Cobalt.b,.96f);
                    cellText[i].color = i % 2 == 0 ? Night : Pale;
                }
            }
            novaRun = 0f; pavilionFoldTarget = 0f; towerAim = 0f; dockPulse = 1f; crankVisual += 220f;
            MgfFx.Punch(boardRt,.055f,.34f);
            MgfFx.Punch(nova,.14f,.40f);
        }

        void WrongFeedback(string misconception, string message)
        {
            revealText.text = message + "\n어두운 기포를 보고 같은 보드를 고치시오.";
            revealText.color = Pale; revealClock = 0; revealGroup.alpha = 1;
            wrongShake = st.lives <= 1 ? .22f : .13f;
            for (int i = 0; i < activeCells; i++)
            {
                if (feedbackExtra[i]) { cellImage[i].color = new Color(Wrong.r,Wrong.g,Wrong.b,.96f); starRenderers[i].sharedMaterial = bubbleMat; }
                else if (feedbackMissing[i]) { cellImage[i].color = new Color(Violet.r,Violet.g,Violet.b,.9f); }
            }
            MgfFx.Punch(crankRt,.12f,.26f);
        }

        void ClearFeedbackVisuals()
        {
            revealGroup.alpha = 0; revealClock = 9f;
            for (int i = 0; i < activeCells; i++)
            {
                cellImage[i].color = marked[i] ? (current.cellAction == CellAction.KeepTargets ? new Color(Cobalt.r,Cobalt.g,Cobalt.b,.96f) : new Color(Bubble.r,Bubble.g,Bubble.b,.92f)) : new Color(.91f,.78f,.59f,.98f);
                cellText[i].color = marked[i] && current.cellAction == CellAction.KeepTargets ? Pale : Night;
                starRenderers[i].sharedMaterial = marked[i] ? bubbleMat : starOffMat;
            }
        }

        void CellPressed(int index, bool on)
        {
            if (index < 0 || index >= activeCells) return;
            cellImage[index].color = on ? (current.cellAction == CellAction.KeepTargets ? new Color(Cobalt.r,Cobalt.g,Cobalt.b,.96f) : new Color(Bubble.r,Bubble.g,Bubble.b,.94f)) : new Color(.91f,.78f,.59f,.98f);
            cellText[index].color = on && current.cellAction == CellAction.KeepTargets ? Pale : Night;
            starRenderers[index].sharedMaterial = on ? bubbleMat : starOffMat;
            MgfFx.Punch(cellRt[index],.18f,.22f);
        }

        void FoldBoard(bool on)
        {
            foldImage.color = on ? Star : Violet;
            boardRt.localRotation = Quaternion.Euler(0,on ? -4f : 0,0);
            if (on) MgfFx.Punch(boardRt,.08f,.36f);
        }

        void ShowEnd(string phaseName, string reason)
        {
            SetScreen();
            endTitle.text = phaseName == "clear" ? "별빛 초점 완성" : phaseName == "practice_complete" ? "관측 기록 완성" : "관측환 봉인";
            endStats.text = reason + "\n최초 정답 " + st.firstAttemptCorrect + "/6\n마지막 두 관측 " + tailFirstCorrect + "/2\n점수 " + st.score;
            sealImage.color = phaseName == "clear" ? new Color(Star.r,Star.g,Star.b,.72f) : new Color(Wrong.r,Wrong.g,Wrong.b,.76f);
            MgfFx.Punch(endTitle.rectTransform,.15f,.5f);
        }

        void AdvanceGoalBanner()
        {
            goalText.text = "이제 발문을 읽고 랙 모양을 직접 정하시오.";
            goalText.color = Pale;
            MgfFx.Punch(goalText.rectTransform,.1f,.32f);
        }

        void Refuse(string message)
        {
            toastText.text = message; toastClock = 0; toastGroup.alpha = 1; refuseClock = 0;
            MgfSfx.Play("wrong", .12f);
        }

        void SpawnRipple(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(gameUi,screen,null,out Vector2 local);
            rippleRt.anchoredPosition = local; rippleClock = 0;
        }

        void StartGuide() { guideBoost = 1f; PositionGuide(); }
        void BoostGuide() { guideBoost = 1f; PositionGuide(); MgfFx.Punch(guideRt,.18f,.35f); }
        void BoostCrankGuide() { guideBoost = 1.2f; MgfFx.Punch(crankRt,.15f,.34f); }
        void PointToNextAction() { BoostGuide(); }
        void PointToTitleHandle() { MgfFx.Punch(titleHandleRt,.16f,.36f); }
        void PulseRestart() { MgfFx.Punch(restartRt,.15f,.32f); }
        void PulseDock(RackTopology topology) { dockPulse = 1f; MgfFx.Punch(topology==RackTopology.Crossed?crossDock:endDockLeft,.11f,.3f); }

        void DemonstrateSecondRack()
        {
            DockRack(1, RackTopology.Crossed);
            rackHandleB.GetComponent<Image>().color = new Color(Cobalt.r,Cobalt.g,Cobalt.b,.98f);
            MgfFx.Punch(rackHandleB, .15f, .42f);
            MgfFx.Punch(crossDock, .10f, .36f);
            MgfSfx.Play("whoosh", .24f);
            PositionGuide();
        }

        void BeginRackDrag(int rack) { MgfFx.Punch(rack==0?rackHandleA:rackHandleB,.1f,.2f); MgfSfx.Play("tap",.15f); }
        void UpdateRackDrag(int rack, Vector2 screen)
        {
            RectTransform rt = rack==0?rackHandleA:rackHandleB;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(gameUi,screen,null,out Vector2 local);
            rt.anchoredPosition = local - RectCenterInGame(rt) + rt.anchoredPosition;
        }
        void BeginFoldDrag() { MgfFx.Punch(foldRt,.12f,.2f); }
        void UpdateFoldDrag(Vector2 screen) { float d=Mathf.Clamp01((dragStart.x-screen.x)/Mathf.Max(80f,Screen.width*.16f)); foldRt.localRotation=Quaternion.Euler(0,0,d*16f); }
        void CancelFoldDrag() { foldRt.localRotation=Quaternion.identity; }
        void BeginCrankDrag() { MgfFx.Punch(crankRt,.1f,.2f); }
        void EndCrankDrag() { crankRt.localScale=Vector3.one; }
        void RotateCrank(float angle,float accumulated) { crankVisual=angle; crankImage.color=Color.Lerp(Star,Opal,Mathf.Clamp01(accumulated/140f)); }

        RackTopology TopologyAtDrop(Vector2 screen, int rack)
        {
            // ScreenPoint 좌표계는 WebGL 렌더 스케일과 CanvasScaler 조합에서 RectTransform의
            // 논리 픽셀과 달라질 수 있다. 화면 비율 구역을 판정 근거로 써 모바일/와이드 모두
            // 보이는 세 홈의 넓은 히트박스와 정확히 맞춘다.
            float nx = screen.x / Mathf.Max(1f, Screen.width);
            float ny = screen.y / Mathf.Max(1f, Screen.height);
            // 첫 연습은 중앙 홈의 보이는 테두리 가까이에 놓아도 스냅한다. 이후 문제는 기존 홈 경계를 유지한다.
            if (flow == Flow.Practice && nx >= .12f && nx <= .88f && ny >= .12f && ny <= .88f)
                return RackTopology.Crossed;
            if (ny < .28f || ny > .72f) return RackTopology.None;
            if (nx >= .34f && nx <= .66f) return RackTopology.Crossed;
            if (rack == 0 && nx < .34f) return RackTopology.EndJoined;
            if (rack == 1 && nx > .66f) return RackTopology.EndJoined;
            return RackTopology.None;
        }
        bool HitTitleHandle(Vector2 p)
        {
            if (titleHandleRt != null && RectTransformUtility.RectangleContainsScreenPoint(titleHandleRt, p, null)) return true;
            // The eclipse ring itself is the title-world CTA.  Keep its broad visible
            // interior tappable so a first-time player does not have to discover the
            // small handle before the camera descent can begin.
            float nx = p.x / Mathf.Max(1f, Screen.width);
            float ny = p.y / Mathf.Max(1f, Screen.height);
            return nx >= 0.12f && nx <= 0.88f && ny >= 0.25f && ny <= 0.78f;
        }
        bool HitRestart(Vector2 p) { return RectTransformUtility.RectangleContainsScreenPoint(restartRt,p,null); }
        bool HitRackHandle(int i,Vector2 p)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(i==0?rackHandleA:rackHandleB,p,null)) return true;
            if (flow != Flow.Practice) return false;
            // 고정 연습에서는 손잡이를 놓친 드래그도 현재 안내 중인 랙을 집는다.
            // 탭은 Game.cs의 30px 이동 문턱에서 거부되므로 실제 끌기만 배치된다.
            float ny = p.y / Mathf.Max(1f,Screen.height);
            if (ny < .12f || ny > .88f) return false;
            if (i == 0) return rackA == RackTopology.None;
            return rackA != RackTopology.None && rackB == RackTopology.None;
        }
        bool HitFoldHandle(Vector2 p) { return foldRt.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(foldRt,p,null); }
        bool HitCrank(Vector2 p) { return RectTransformUtility.RectangleContainsScreenPoint(crankRt,p,null); }
        Vector2 CrankCenterScreen() { return RectTransformUtility.WorldToScreenPoint(null,crankRt.position); }
        int HitCell(Vector2 p,int rows,int cols)
        {
            int n=Mathf.Min(activeCells,rows*cols);
            for(int i=0;i<n;i++) if(RectTransformUtility.RectangleContainsScreenPoint(cellRt[i],p,null)) return i;
            return -1;
        }

        RectTransform R(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); var rt=(RectTransform)go.transform;
            rt.anchorMin=rt.anchorMax=anchor; rt.pivot=new Vector2(.5f,.5f); rt.anchoredPosition=pos; rt.sizeDelta=size; return rt;
        }
        Image Img(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color,Sprite sprite)
        {
            var rt=R(name,parent,anchor,pos,size); var im=rt.gameObject.AddComponent<Image>(); im.sprite=sprite; im.color=color; im.raycastTarget=false;
            if(sprite&&sprite.border!=Vector4.zero) im.type=Image.Type.Sliced; return im;
        }
        TextMeshProUGUI Txt(string name,Transform parent,string text,Vector2 anchor,Vector2 pos,float size,Color color,float width,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {
            var rt=R(name,parent,anchor,pos,new Vector2(width,size*3.1f)); var t=rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font=MgfText.Font; t.fontSize=size; t.fontStyle=FontStyles.Bold; t.color=color; t.alignment=align; t.textWrappingMode=TextWrappingModes.Normal;
            t.overflowMode=TextOverflowModes.Ellipsis; t.raycastTarget=false; t.text=text; return t;
        }

        Sprite MakeDisc(bool ring)
        {
            const int S=96; var t=new Texture2D(S,S,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,name=ring?"EclipseRing":"OpalDisc"}; var px=new Color32[S*S];
            for(int y=0;y<S;y++)for(int x=0;x<S;x++){float dx=(x+.5f)/S*2-1,dy=(y+.5f)/S*2-1,d=Mathf.Sqrt(dx*dx+dy*dy);float a=ring?Mathf.Clamp01((.96f-d)*18f)*Mathf.Clamp01((d-.67f)*18f):Mathf.Clamp01((.96f-d)*13f);px[y*S+x]=new Color32(255,255,255,(byte)(a*255));}
            t.SetPixels32(px);t.Apply(false,true);return Sprite.Create(t,new Rect(0,0,S,S),new Vector2(.5f,.5f),100);
        }
        Sprite MakePanel()
        {
            const int S=96;var t=new Texture2D(S,S,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,name="CutBasaltPanel"};var px=new Color32[S*S];
            for(int y=0;y<S;y++)for(int x=0;x<S;x++){float nx=Mathf.Abs(x+0.5f-S*.5f),ny=Mathf.Abs(y+0.5f-S*.5f);float bevel=Mathf.Max(nx-(S*.5f-10),ny-(S*.5f-10));float a=Mathf.Clamp01(1-bevel/10f);if(x<8&&y<18)a*=Mathf.Clamp01((x+y-8)/10f);if(x>S-10&&y>S-20)a*=Mathf.Clamp01((S*2-x-y-10)/10f);px[y*S+x]=new Color32(255,255,255,(byte)(a*255));}
            t.SetPixels32(px);t.Apply(false,true);return Sprite.Create(t,new Rect(0,0,S,S),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(15,15,15,15));
        }
        Sprite MakeStarSprite()
        {
            const int S=96;var t=new Texture2D(S,S,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,name="FourPointStar"};var px=new Color32[S*S];
            for(int y=0;y<S;y++)for(int x=0;x<S;x++){float dx=Mathf.Abs((x+.5f)/S*2-1),dy=Mathf.Abs((y+.5f)/S*2-1);float d=Mathf.Min(dx+dy*.28f,dy+dx*.28f);float a=Mathf.Clamp01((.94f-d)*12f);px[y*S+x]=new Color32(255,255,255,(byte)(a*255));}
            t.SetPixels32(px);t.Apply(false,true);return Sprite.Create(t,new Rect(0,0,S,S),new Vector2(.5f,.5f),100);
        }
        Sprite MakeCrackSprite()
        {
            const int S=96;var t=new Texture2D(S,S,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,name="RadialSeal"};var px=new Color32[S*S];
            for(int y=0;y<S;y++)for(int x=0;x<S;x++){float dx=x-S*.5f,dy=y-S*.5f,d=Mathf.Sqrt(dx*dx+dy*dy),a=0; if(Mathf.Abs(d-34)<3)a=1;float ang=Mathf.Atan2(dy,dx);float spoke=Mathf.Abs(Mathf.Sin(ang*5+d*.035f));if(d<34&&spoke<.075f)a=1;px[y*S+x]=new Color32(255,255,255,(byte)(a*255));}
            t.SetPixels32(px);t.Apply(false,true);return Sprite.Create(t,new Rect(0,0,S,S),new Vector2(.5f,.5f),100);
        }

        GameObject NewMeshObject(string name,Mesh mesh,Material material,Transform parent)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;return go;
        }
        Mesh Torus(float major,float minor,int seg,int tube)
        {
            var v=new Vector3[seg*tube];var n=new Vector3[v.Length];var uv=new Vector2[v.Length];var tri=new int[seg*tube*6];
            for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg;Vector3 c=new Vector3(Mathf.Cos(a)*major,0,Mathf.Sin(a)*major);for(int j=0;j<tube;j++){float b=j*Mathf.PI*2/tube;Vector3 norm=new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(b),Mathf.Sin(a)*Mathf.Cos(b));int k=i*tube+j;v[k]=c+norm*minor;n[k]=norm;uv[k]=new Vector2((float)i/seg,(float)j/tube);}}
            int q=0;for(int i=0;i<seg;i++)for(int j=0;j<tube;j++){int a=i*tube+j,b=((i+1)%seg)*tube+j,c=((i+1)%seg)*tube+(j+1)%tube,d=i*tube+(j+1)%tube;tri[q++]=a;tri[q++]=b;tri[q++]=c;tri[q++]=a;tri[q++]=c;tri[q++]=d;}
            var m=new Mesh{name="ObservatoryTorus"};m.vertices=v;m.normals=n;m.uv=uv;m.triangles=tri;m.RecalculateBounds();return m;
        }
        Mesh StarMesh(float outer,float inner)
        {
            var v=new Vector3[10];for(int i=0;i<10;i++){float a=(i*36-90)*Mathf.Deg2Rad,r=i%2==0?outer:inner;v[i]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);}
            var verts=new Vector3[12];verts[0]=Vector3.zero;for(int i=0;i<10;i++)verts[i+1]=v[i];verts[11]=v[0];var tri=new int[30];for(int i=0;i<10;i++){tri[i*3]=0;tri[i*3+1]=i+1;tri[i*3+2]=i+2;}var m=new Mesh{name="StarCell"};m.vertices=verts;m.triangles=tri;m.RecalculateNormals();m.RecalculateBounds();return m;
        }
    }
}
