// 연 띄워 — 햇빛 드는 종이바람 공방 2.5D 월드, 3×3 연틀, 비단 발사와 반응형 UI.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.YeonTtwiwo
{
    public partial class YeonTtwiwoGame
    {
        static readonly Color Night = MgfLook.Hex("243A43");
        static readonly Color Slate = MgfLook.Hex("6B8F8A");
        static readonly Color Pearl = MgfLook.Hex("FFF4D8");
        static readonly Color Vermilion = MgfLook.Hex("E96B4C");
        static readonly Color RainBlue = MgfLook.Hex("5FA8B8");
        static readonly Color InkBlack = MgfLook.Hex("173239");
        static readonly Color Brass = MgfLook.Hex("C98B45");
        static readonly Color Bamboo = MgfLook.Hex("B9864E");
        static readonly Color Leaf = MgfLook.Hex("83AA6B");
        static readonly Color Sky = MgfLook.Hex("BFE8ED");

        Camera cam;
        Transform worldRoot, roofRoot, boardRoot, rackRoot, flagRoot;
        Transform[] cloudPlates = new Transform[14];
        Transform[] rainNeedles = new Transform[44];
        Vector3[] rainHomes = new Vector3[44];
        Transform[] flags = new Transform[7];
        Transform[] frameRoots = new Transform[9];
        Transform[] fixedBars = new Transform[9];
        GameObject[] silkPanels = new GameObject[9];
        GameObject[] knotRoots = new GameObject[9];
        GameObject[] previewRings = new GameObject[9];
        TextMeshPro[] frameGoalText = new TextMeshPro[9];
        TextMeshPro[] frameMeasureText = new TextMeshPro[9];
        Transform[] cartridgeRoots = new Transform[3];
        Transform[] cartridgeBars = new Transform[3];
        Transform[] cartridgeHandles = new Transform[3];
        TextMeshPro[] cartridgeLabels = new TextMeshPro[3];
        Vector3[] cartridgeHomes = new Vector3[3];
        Transform[] kiteRoots = new Transform[6];
        Vector3[] kiteStart = new Vector3[6];
        Transform[] windBirds = new Transform[3];
        Vector3[] windBirdHomes = new Vector3[3];
        LineRenderer dragTrail;

        Material lacquerMat, lacquerGlowMat, frameMat, fixedMat, silkMat, knotMat, vermilionMat, tileMatA, tileMatB;
        float worldClock, launchClock, launchDuration, skyOpen, skyOpenTarget, cameraZoomKick, birdCheer, birdStartle;
        int launchKiteCount;

        GameObject titleRoot, playRoot, endRoot;
        RawImage titleArt;
        RectTransform titleArtRt, titleBannerRt, titleCordRt, titleHandleRt;
        TextMeshProUGUI titleLogoShadow, titleLogo, titleTag, titleMeta, titleCordText;
        Image titleBannerImage, titleCordImage, titleHandleImage;
        readonly Image[] titleRain = new Image[26];
        readonly float[] titleRainOffset = new float[26];

        RectTransform goalRt, promptRt, hudRt, evidenceRt, toastRt, endPanelRt, endCordRt, endHandleRt;
        TextMeshProUGUI goalUi, promptUi, hudUi, evidenceUi, toastUi, endTitleUi, endStatsUi, endCordUi;
        Image evidenceImage, toastImage, endPanelImage, endCordImage, endHandleImage;
        readonly Image[] ripples = new Image[8];
        readonly float[] rippleLife = new float[8];
        int rippleCursor;
        Image guideDot, guideLine, guideArrow;
        float guideLeft;
        bool guideLarge;
        int previewDock = -1;
        float toastLeft, titleCordPull, endCordPull, scoreDisplay, endScoreDisplay;
        int lastScore = -1, lastScreenW = -1, lastScreenH = -1;
        float lastCanvasW = -1f, lastCanvasH = -1f;
        bool land;

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("A9DDE4"), MgfLook.Hex("EAF4DF"), MgfLook.Hex("D7C895"), .82f);
            MgfLook.Sun(new Vector3(52f, -28f, -24f), MgfLook.Hex("FFF1C7"), 1.18f, .76f);
            cam = MgfLook.Camera(new Vector3(0f, 15.5f, -18.5f), new Vector3(0f, .3f, .1f), 35f);
            cam.orthographic = true;
            cam.orthographicSize = 7.3f;

            lacquerMat = MgfLook.Lit(MgfLook.Hex("7C573A"), .16f, .02f);
            lacquerGlowMat = MgfLook.Lit(MgfLook.Hex("A56F43"), .20f, .01f);
            frameMat = MgfLook.Lit(Bamboo, .12f, 0f);
            fixedMat = MgfLook.Lit(MgfLook.Hex("466F73"), .18f, .02f);
            silkMat = MgfLook.Lit(Pearl, .10f, 0f);
            knotMat = MgfLook.Lit(MgfLook.Hex("3C3446"), .10f, .01f);
            vermilionMat = MgfLook.Lit(Vermilion, .16f, .01f);
            tileMatA = MgfLook.Lit(MgfLook.Hex("D7B77C"), .12f, 0f);
            tileMatB = MgfLook.Lit(MgfLook.Hex("F4DFAD"), .10f, 0f);

            worldRoot = new GameObject("SunnyPaperWindWorkshop").transform;
            roofRoot = new GameObject("MiniatureKiteCourtyard").transform; roofRoot.SetParent(worldRoot, false);
            boardRoot = new GameObject("NineKiteFrames").transform; boardRoot.SetParent(worldRoot, false);
            rackRoot = new GameObject("LacquerCartridgeRail").transform; rackRoot.SetParent(worldRoot, false);
            flagRoot = new GameObject("WindVanesAndFlags").transform; flagRoot.SetParent(worldRoot, false);

            BuildRoof();
            BuildCloudsAndRain();
            BuildFrames();
            BuildRack();
            BuildKites();
            BuildWindBirds();
            BuildTrail();
        }

        void BuildRoof()
        {
            // 밝은 낮의 미니어처 종이바람 공방. 큰 무광 한지 마당과 대나무 작업대,
            // 종이 조각·실패·화분이 화면을 채워 이전 우중 산업 무대와 물성을 분리한다.
            var grass = MgfLook.Lit(MgfLook.Hex("9FBE78"), .10f, 0f);
            var paperBlue = MgfLook.Lit(MgfLook.Hex("D7EEF0"), .08f, 0f);
            var paperCoral = MgfLook.Lit(MgfLook.Hex("F4A681"), .08f, 0f);
            var thread = MgfLook.Lit(MgfLook.Hex("3D7778"), .12f, 0f);
            MgfLook.Block("GrassDiorama", new Vector3(0f,-.42f,.2f), new Vector3(25f,.58f,17.5f), .55f, grass, roofRoot);
            MgfLook.Block("HanjiWorkMat", new Vector3(0f,-.06f,.7f), new Vector3(13.0f,.18f,13.2f), .38f, tileMatB, roofRoot);
            for(int i=-5;i<=5;i++)
                MgfLook.Block("BambooSlat"+i,new Vector3(i*1.12f,.05f,.7f),new Vector3(.065f,.06f,12.8f),.02f,frameMat,roofRoot);
            for(int side=-1;side<=1;side+=2)
            {
                MgfLook.Block("LowShelf"+side,new Vector3(side*8.7f,.38f,.3f),new Vector3(2.5f,.65f,13.5f),.25f,tileMatA,roofRoot);
                for(int i=0;i<6;i++)
                {
                    float z=-5.5f+i*2.2f;
                    var spool=MgfLook.Block("ThreadSpool"+side+"_"+i,new Vector3(side*(8.35f+(i%2)*.42f),.92f,z),new Vector3(.62f,.62f,.62f),.28f,i%2==0?thread:vermilionMat,roofRoot);
                    spool.transform.localRotation=Quaternion.Euler(0f,i*17f,0f);
                    var scrap=MgfLook.Block("PaperScrap"+side+"_"+i,new Vector3(side*(7.0f+(i%3)*.45f),.14f,z+.35f),new Vector3(.75f,.05f,.75f),.03f,i%2==0?paperBlue:paperCoral,roofRoot);
                    scrap.transform.localRotation=Quaternion.Euler(0f,45f+i*9f,0f);
                }
            }
            for(int i=-6;i<=6;i+=2)
            {
                MgfLook.Block("GardenStake"+i,new Vector3(i*1.55f,.65f,7.2f),new Vector3(.13f,1.5f,.13f),.04f,frameMat,roofRoot);
                var leaf=MgfLook.Block("PinwheelLeaf"+i,new Vector3(i*1.55f,1.48f,7.2f),new Vector3(.82f,.07f,.82f),.04f,i%4==0?paperCoral:paperBlue,roofRoot);
                leaf.transform.localRotation=Quaternion.Euler(0f,45f,0f);
            }
            for (int i = 0; i < flags.Length; i++)
            {
                float side = (i & 1) == 0 ? -1f : 1f;
                MgfLook.Block("WindPole" + i, new Vector3(side * (9.5f + (i % 2) * .7f), 2.25f, -5.5f + i * 1.85f),
                    new Vector3(.11f, 4.7f, .11f), .025f, frameMat, flagRoot);
                var flag = MgfLook.Block("HanjiStreamer" + i, new Vector3(side * (8.9f + (i % 2) * .7f), 3.65f, -5.5f + i * 1.85f),
                    new Vector3(1.12f, .06f, .42f), .02f, i%3==0?paperBlue:i%3==1?paperCoral:silkMat, flagRoot);
                flag.transform.rotation = Quaternion.Euler(0f, side < 0 ? 12f : -12f, side < 0 ? 8f : -8f);
                flags[i] = flag.transform;
            }
        }

        void BuildCloudsAndRain()
        {
            var cloudMat = MgfLook.Lit(MgfLook.Hex("F8F0D8"), .08f, 0f);
            for (int i = 0; i < cloudPlates.Length; i++)
            {
                float x = -14f + (i * 43 % 101) / 100f * 28f;
                float y = 8.4f + (i % 4) * .38f;
                float z = 7.5f + (i % 5) * 1.1f;
                var c = MgfLook.Block("LayeredCloud" + i, new Vector3(x, y, z),
                    new Vector3(4.0f + (i % 3) * 1.2f, .48f + (i % 2) * .20f, 1.2f), .22f, cloudMat, worldRoot);
                c.transform.rotation = Quaternion.Euler(0f, -8f + i * 3f, 0f);
                cloudPlates[i] = c.transform;
            }
            var rainMat = MgfLook.Unlit(new Color(Pearl.r, Pearl.g, Pearl.b, .76f));
            for (int i = 0; i < rainNeedles.Length; i++)
            {
                float x = -12f + ((i * 47) % 97) / 96f * 24f;
                float y = .7f + ((i * 71) % 89) / 88f * 10f;
                float z = -7f + ((i * 37) % 83) / 82f * 16f;
                rainHomes[i] = new Vector3(x, y, z);
                var needle = MgfLook.Block("WindThread" + i, rainHomes[i], new Vector3(.018f, .55f + (i % 4) * .14f, .018f), .006f, rainMat, worldRoot);
                needle.transform.rotation = Quaternion.Euler(0f, 0f, 58f);
                rainNeedles[i] = needle.transform;
            }
            var sunDisk = MgfLook.Block("PaperSun", new Vector3(8.2f, 9.0f, 9.4f), new Vector3(1.15f, 1.15f, .20f), .50f,
                MgfLook.Lit(MgfLook.Hex("FFD783"), .08f, 0f), worldRoot);
            sunDisk.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        }

        void BuildFrames()
        {
            for (int i = 0; i < frameRoots.Length; i++)
            {
                int row = i / 3, col = i % 3;
                var root = new GameObject("KiteFrame" + (i + 1)).transform;
                root.SetParent(boardRoot, false);
                root.localPosition = new Vector3((col - 1) * 3.05f, .22f, (row - 1) * 2.86f + .6f);
                frameRoots[i] = root;

                MgfLook.Block("HanjiFrameBoard", Vector3.zero, new Vector3(2.65f, .22f, 2.38f), .18f, tileMatB, root);
                Vector3[] pts = {
                    new Vector3(0f,.18f,-1.02f), new Vector3(1.13f,.18f,0f),
                    new Vector3(0f,.18f,1.02f), new Vector3(-1.13f,.18f,0f)
                };
                for (int k = 0; k < 4; k++) CreateBar("FrameEdge" + k, pts[k], pts[(k + 1) % 4], .09f, frameMat, root);
                fixedBars[i] = CreateBar("FixedAC", new Vector3(-.93f,.25f,0f), new Vector3(.93f,.25f,0f), .095f, fixedMat, root).transform;
                MgfLook.Block("MidpointO", new Vector3(0f,.31f,0f), new Vector3(.24f,.13f,.24f), .09f, vermilionMat, root);

                silkPanels[i] = MgfLook.Block("PearlSilk", new Vector3(0f,.30f,0f), new Vector3(1.46f,.07f,1.46f), .04f, silkMat, root);
                silkPanels[i].transform.localRotation = Quaternion.Euler(0f,45f,0f);
                silkPanels[i].SetActive(false);

                knotRoots[i] = new GameObject("InkKnot"); knotRoots[i].transform.SetParent(root, false); knotRoots[i].transform.localPosition = new Vector3(0f,.42f,0f);
                for (int k = 0; k < 3; k++)
                {
                    var strand = MgfLook.Block("KnotStrand" + k, Vector3.zero, new Vector3(.88f,.18f,.20f), .09f, knotMat, knotRoots[i].transform);
                    strand.transform.localRotation = Quaternion.Euler(0f, k * 60f, 0f);
                }
                knotRoots[i].SetActive(false);

                previewRings[i] = new GameObject("SlotEntrance"); previewRings[i].transform.SetParent(root, false); previewRings[i].transform.localPosition = new Vector3(0f,.36f,0f);
                for (int k = 0; k < 4; k++)
                {
                    float a = k * 90f * Mathf.Deg2Rad;
                    var mark = MgfLook.Block("WhiteInk" + k, new Vector3(Mathf.Cos(a) * 1.05f, 0f, Mathf.Sin(a) * 1.05f),
                        new Vector3(.30f,.07f,.07f), .02f, fixedMat, previewRings[i].transform);
                    mark.transform.localRotation = Quaternion.Euler(0f, -k * 90f, 0f);
                }
                previewRings[i].SetActive(false);

                frameGoalText[i] = MgfText.World("", new Vector3(0f,.34f,1.32f), .62f, InkBlack, root);
                frameGoalText[i].transform.localRotation = Quaternion.Euler(90f,0f,0f);
                frameMeasureText[i] = MgfText.World("A — O — C", new Vector3(0f,.35f,-1.27f), .48f, Night, root);
                frameMeasureText[i].transform.localRotation = Quaternion.Euler(90f,0f,0f);
            }
            boardRoot.position = new Vector3(0f, .25f, .75f);
        }

        void BuildRack()
        {
            MgfLook.Block("BambooToolTray", new Vector3(0f,.40f,0f), new Vector3(10.4f,.62f,2.4f), .24f, tileMatA, rackRoot);
            for (int i = 0; i < cartridgeRoots.Length; i++)
            {
                var root = new GameObject("DiagonalCartridge" + i).transform; root.SetParent(rackRoot, false);
                root.localPosition = new Vector3((i - 1) * 3.05f, .86f, 0f);
                cartridgeRoots[i] = root;
                cartridgeHomes[i] = root.localPosition;
                cartridgeBars[i] = MgfLook.Block("BambooSpar", Vector3.zero, new Vector3(2.25f,.26f,.42f), .12f, frameMat, root).transform;
                MgfLook.Block("MidpointNotch", new Vector3(0f,.17f,0f), new Vector3(.22f,.32f,.62f), .07f, vermilionMat, root);
                cartridgeHandles[i] = MgfLook.Block("Handle", new Vector3(1.28f,.16f,0f), new Vector3(.62f,.38f,.72f), .18f, vermilionMat, root).transform;
                MgfLook.Block("PaperTestLeft", new Vector3(-1.35f,.05f,0f), new Vector3(.52f,.06f,.86f), .03f, silkMat, root);
                MgfLook.Block("PaperTestRight", new Vector3(1.35f,.05f,0f), new Vector3(.52f,.06f,.86f), .03f, silkMat, root);
                cartridgeLabels[i] = MgfText.World("", new Vector3(0f,.42f,-.72f), .52f, InkBlack, root);
                cartridgeLabels[i].transform.localRotation = Quaternion.Euler(90f,0f,0f);
            }
            rackRoot.position = new Vector3(0f,.18f,-5.0f);
        }

        void BuildKites()
        {
            for (int i = 0; i < kiteRoots.Length; i++)
            {
                var root = new GameObject("LaunchKite" + i).transform; root.SetParent(worldRoot, false);
                var paper = MgfLook.Block("Paper", Vector3.zero, new Vector3(1.22f,.06f,1.22f), .04f, silkMat, root);
                paper.transform.localRotation = Quaternion.Euler(0f,45f,0f);
                CreateBar("SpineA", new Vector3(-.82f,.08f,0f), new Vector3(.82f,.08f,0f), .045f, lacquerMat, root);
                CreateBar("SpineB", new Vector3(0f,.08f,-.82f), new Vector3(0f,.08f,.82f), .045f, lacquerMat, root);
                MgfLook.Block("Tail", new Vector3(0f,.02f,-1.02f), new Vector3(.08f,.04f,.78f), .02f, vermilionMat, root);
                root.gameObject.SetActive(false); kiteRoots[i] = root;
            }
        }

        void BuildWindBirds()
        {
            // 종이 연에서 태어난 작은 바람새 세 마리. 정답에는 위로 폴짝이고,
            // 오답에는 뒤로 움츠러들어 수학 사건의 살아 있는 주인공이 된다.
            var eyeMat = MgfLook.Lit(InkBlack, .08f, 0f);
            for(int i=0;i<windBirds.Length;i++)
            {
                var root=new GameObject("PaperWindBird"+i).transform;root.SetParent(worldRoot,false);
                windBirdHomes[i]=new Vector3(-7.0f+i*7.0f,1.15f,5.2f-(i%2)*.7f);root.position=windBirdHomes[i];
                var body=MgfLook.Block("FoldedPaperBody",Vector3.zero,new Vector3(.78f,.13f,.78f),.06f,i==1?silkMat:i==0?vermilionMat:tileMatB,root);
                body.transform.localRotation=Quaternion.Euler(0f,45f,0f);
                var left=MgfLook.Block("LeftWing",new Vector3(-.58f,.02f,0f),new Vector3(.70f,.07f,.42f),.04f,i==1?vermilionMat:silkMat,root);
                var right=MgfLook.Block("RightWing",new Vector3(.58f,.02f,0f),new Vector3(.70f,.07f,.42f),.04f,i==1?vermilionMat:silkMat,root);
                left.transform.localRotation=Quaternion.Euler(0f,-18f,8f);right.transform.localRotation=Quaternion.Euler(0f,18f,-8f);
                MgfLook.Block("EyeL",new Vector3(-.16f,.18f,-.36f),new Vector3(.09f,.09f,.06f),.04f,eyeMat,root);
                MgfLook.Block("EyeR",new Vector3(.16f,.18f,-.36f),new Vector3(.09f,.09f,.06f),.04f,eyeMat,root);
                windBirds[i]=root;
            }
        }

        void BuildTrail()
        {
            var go = new GameObject("VermilionDragTrail"); go.transform.SetParent(worldRoot, false);
            dragTrail = go.AddComponent<LineRenderer>();
            dragTrail.positionCount = 8; dragTrail.widthMultiplier = .07f;
            dragTrail.sharedMaterial = MgfLook.Unlit(Vermilion);
            dragTrail.useWorldSpace = true; dragTrail.enabled = false;
        }

        GameObject CreateBar(string name, Vector3 a, Vector3 b, float thickness, Material mat, Transform parent)
        {
            Vector3 d = b - a; float len = Mathf.Sqrt(d.x * d.x + d.z * d.z);
            var bar = MgfLook.Block(name, (a + b) * .5f, new Vector3(len, thickness, thickness), thickness * .44f, mat, parent);
            bar.transform.localRotation = Quaternion.Euler(0f, -Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg, 0f);
            return bar;
        }

        void BuildUi()
        {
            Canvas canvas = MgfText.Canvas;
            titleRoot = Root("TitleUI", canvas.transform);
            playRoot = Root("PlayUI", canvas.transform);
            endRoot = Root("EndUI", canvas.transform);
            BuildTitleUi(); BuildPlayUi(); BuildEndUi();
        }

        void BuildTitleUi()
        {
            var artGo = new GameObject("TitleKeyArt", typeof(RectTransform), typeof(RawImage)); artGo.transform.SetParent(titleRoot.transform, false);
            titleArt = artGo.GetComponent<RawImage>(); titleArtRt = (RectTransform)artGo.transform; Stretch(titleArtRt);
            // 회화형 생성 이미지는 사용하지 않는다. 같은 런타임 공방 월드가 타이틀부터 보인다.
            titleArt.texture = null; titleArt.color = new Color(Sky.r,Sky.g,Sky.b,.07f);
            titleArt.raycastTarget = false;

            for (int i = 0; i < titleRain.Length; i++)
            {
                var drop = Panel("TitleRain" + i, titleRoot.transform, new Color32(135,185,200,80), out titleRain[i]);
                var rt = (RectTransform)drop.transform; rt.anchorMin = rt.anchorMax = new Vector2((i * 37 % 101) / 100f, .5f);
                rt.sizeDelta = new Vector2(2f, 70f + (i % 5) * 18f); rt.localRotation = Quaternion.Euler(0,0,12f);
                titleRainOffset[i] = (i * 53 % 97) / 97f;
            }

            var banner = Panel("KiteSign", titleRoot.transform, new Color32(255,244,216,246), out titleBannerImage);
            titleBannerRt = (RectTransform)banner.transform;
            titleBannerImage.raycastTarget = false;
            var outline = banner.AddComponent<Outline>(); outline.effectColor = new Color32(21,27,38,230); outline.effectDistance = new Vector2(3f,-3f);
            titleLogoShadow = MakeText("연 띄워", banner.transform, 50f, Night, TextAlignmentOptions.Center);
            titleLogoShadow.outlineWidth = .35f; titleLogoShadow.outlineColor = new Color32(217,74,56,255);
            titleLogo = MakeText("연 띄워", banner.transform, 50f, Vermilion, TextAlignmentOptions.Center);
            titleLogo.outlineWidth = .12f; titleLogo.outlineColor = new Color32(255,244,216,255);
            titleTag = MakeText("대각선 살대를 맞춰 바람길을 열어라", banner.transform, 16f, Night, TextAlignmentOptions.Center);
            titleMeta = MakeText("중학교 2학년 · 사각형의 성질", titleRoot.transform, 14f, Night, TextAlignmentOptions.Center);
            titleMeta.outlineWidth=.16f; titleMeta.outlineColor=new Color32(9,13,20,230);
            Rect((RectTransform)titleLogoShadow.transform,new Vector2(.5f,.62f),new Vector2(3f,-3f),new Vector2(360f,84f));
            Rect((RectTransform)titleLogo.transform,new Vector2(.5f,.62f),Vector2.zero,new Vector2(360f,84f));
            Rect((RectTransform)titleTag.transform,new Vector2(.5f,.20f),Vector2.zero,new Vector2(360f,42f));

            var cord = Panel("LaunchCord", titleRoot.transform, new Color32(217,74,56,245), out titleCordImage);
            titleCordRt = (RectTransform)cord.transform; titleCordImage.raycastTarget=false;
            var handle = Panel("LaunchCordHandle", titleRoot.transform, new Color32(231,224,207,250), out titleHandleImage);
            titleHandleRt = (RectTransform)handle.transform; titleHandleImage.raycastTarget=false;
            titleCordText = MakeText("화면을 탭해 종이바람 공방을 연다", titleRoot.transform, 18f, Night, TextAlignmentOptions.Center);
            titleCordText.outlineWidth=.18f; titleCordText.outlineColor=new Color32(9,13,20,240);
        }

        void BuildPlayUi()
        {
            var goalPanel = Panel("GoalBanner", playRoot.transform, new Color32(21,27,38,226), out _); goalRt=(RectTransform)goalPanel.transform;
            goalUi = MakeText("살대를 돌려 꽂고 바람줄 세 칸을 이으시오", goalPanel.transform, 17f, Pearl, TextAlignmentOptions.Center);
            goalUi.outlineWidth=.10f; goalUi.outlineColor=new Color32(0,0,0,220); StretchInset((RectTransform)goalUi.transform,5f);

            var promptPanel = Panel("OrderPlaque", playRoot.transform, new Color32(42,56,76,232), out _); promptRt=(RectTransform)promptPanel.transform;
            var po=promptPanel.AddComponent<Outline>(); po.effectColor=new Color32(135,185,200,150); po.effectDistance=new Vector2(2f,-2f);
            promptUi=MakeText("",promptPanel.transform,14.5f,Pearl,TextAlignmentOptions.Center); promptUi.textWrappingMode=TextWrappingModes.Normal; StretchInset((RectTransform)promptUi.transform,8f);

            hudUi=MakeText("",playRoot.transform,15f,Night,TextAlignmentOptions.Center); hudRt=(RectTransform)hudUi.transform;
            hudUi.outlineWidth=.12f;hudUi.outlineColor=new Color32(255,244,216,230);

            var evidencePanel=Panel("MeasurementBands",playRoot.transform,new Color32(231,224,207,238),out evidenceImage); evidenceRt=(RectTransform)evidencePanel.transform;
            var eo=evidencePanel.AddComponent<Outline>();eo.effectColor=new Color32(217,74,56,170);eo.effectDistance=new Vector2(2f,-2f);
            evidenceUi=MakeText("살대의 B·O·D 눈금과 연틀의 A·O·C를 비교하시오",evidencePanel.transform,14f,Night,TextAlignmentOptions.Center);
            evidenceUi.textWrappingMode=TextWrappingModes.Normal;StretchInset((RectTransform)evidenceUi.transform,7f);

            var toast=Panel("Toast",playRoot.transform,new Color32(9,13,20,244),out toastImage);toastRt=(RectTransform)toast.transform;
            toastUi=MakeText("",toast.transform,14f,Pearl,TextAlignmentOptions.Center);toastUi.textWrappingMode=TextWrappingModes.Normal;StretchInset((RectTransform)toastUi.transform,7f);toast.SetActive(false);

            for(int i=0;i<ripples.Length;i++)
            {
                var r=Panel("Ripple"+i,playRoot.transform,new Color32(135,185,200,180),out ripples[i]);
                ripples[i].raycastTarget=false;r.SetActive(false);
            }
            var gd=Panel("GuideDot",playRoot.transform,new Color32(231,224,207,245),out guideDot);guideDot.raycastTarget=false;
            Rect((RectTransform)gd.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(22f,22f));gd.transform.localRotation=Quaternion.Euler(0,0,45f);gd.SetActive(false);
            var gl=Panel("GuidePath",playRoot.transform,new Color32(217,74,56,190),out guideLine);guideLine.raycastTarget=false;gl.SetActive(false);
            var ga=Panel("GuideArrow",playRoot.transform,new Color32(217,74,56,245),out guideArrow);guideArrow.raycastTarget=false;
            Rect((RectTransform)ga.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(28f,28f));ga.transform.localRotation=Quaternion.Euler(0,0,45f);ga.SetActive(false);
        }

        void BuildEndUi()
        {
            var panel=Panel("ResultBanner",endRoot.transform,new Color32(231,224,207,246),out endPanelImage);endPanelRt=(RectTransform)panel.transform;
            var outline=panel.AddComponent<Outline>();outline.effectColor=new Color32(21,27,38,245);outline.effectDistance=new Vector2(4f,-4f);
            endTitleUi=MakeText("",panel.transform,37f,Night,TextAlignmentOptions.Center);endTitleUi.outlineWidth=.16f;endTitleUi.outlineColor=new Color32(217,74,56,220);
            endStatsUi=MakeText("",panel.transform,18f,Night,TextAlignmentOptions.Center);endStatsUi.textWrappingMode=TextWrappingModes.Normal;
            Rect((RectTransform)endTitleUi.transform,new Vector2(.5f,.77f),Vector2.zero,new Vector2(360f,70f));
            Rect((RectTransform)endStatsUi.transform,new Vector2(.5f,.45f),Vector2.zero,new Vector2(360f,180f));
            var cord=Panel("RestartCord",endRoot.transform,new Color32(217,74,56,248),out endCordImage);endCordRt=(RectTransform)cord.transform;
            var handle=Panel("RestartHandle",endRoot.transform,new Color32(231,224,207,255),out endHandleImage);endHandleRt=(RectTransform)handle.transform;
            endCordUi=MakeText("다시 띄우기",endRoot.transform,20f,Pearl,TextAlignmentOptions.Center);endCordUi.outlineWidth=.15f;endCordUi.outlineColor=new Color32(9,13,20,240);
        }

        GameObject Root(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);Stretch((RectTransform)go.transform);return go;}
        GameObject Panel(string name,Transform parent,Color32 color,out Image image){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return go;}
        TextMeshProUGUI MakeText(string text,Transform parent,float size,Color color,TextAlignmentOptions align)
        {
            var go=new GameObject("Text",typeof(RectTransform));go.transform.SetParent(parent,false);var t=go.AddComponent<TextMeshProUGUI>();t.font=MgfText.Font;t.fontSize=size;t.fontStyle=FontStyles.Bold;t.color=color;t.alignment=align;t.raycastTarget=false;t.text=text;return t;
        }
        static void Stretch(RectTransform rt){rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;}
        static void StretchInset(RectTransform rt,float inset){rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=Vector2.one*inset;rt.offsetMax=Vector2.one*-inset;}
        static void Rect(RectTransform rt,Vector2 anchor,Vector2 offset,Vector2 size){rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=offset;rt.sizeDelta=size;}

        void BindBundle()
        {
            if(current==null)return;
            for(int i=0;i<9;i++)
            {
                DockSpec d=current.docks[i];
                st.dockGeometry[i*3]=(int)d.goal;st.dockGeometry[i*3+1]=d.ux;st.dockGeometry[i*3+2]=d.uy;
                frameGoalText[i].text=YeonRules.GoalName(d.goal);
                bool focus=i==current.preferredDock;
                frameMeasureText[i].text=focus?"AO=CO="+d.uLength+" cm":"";
                frameGoalText[i].fontSize=focus?.78f:.50f;
                frameMeasureText[i].fontSize=focus?.56f:.42f;
                float len=Mathf.Lerp(1.45f,2.02f,(d.uLength-10)/10f);
                fixedBars[i].localScale=new Vector3(len,.095f,.095f);
                fixedBars[i].localRotation=Quaternion.Euler(0f,d.ux==0?90f:0f,0f);
                bool active=gamePhase!=GamePhase.Practice||i==4;
                frameGoalText[i].color=active?(focus?Vermilion:InkBlack):new Color(InkBlack.r,InkBlack.g,InkBlack.b,.20f);
                frameMeasureText[i].color=active?Night:new Color(Night.r,Night.g,Night.b,.18f);
            }
            for(int i=0;i<3;i++)
            {
                CartridgeSpec c=current.cartridges[i];
                st.cartridgeGeometry[i*4]=c.vx;st.cartridgeGeometry[i*4+1]=c.vy;st.cartridgeGeometry[i*4+2]=c.p;st.cartridgeGeometry[i*4+3]=c.q;
                float len=1.45f+(c.p+c.q)*.16f;
                cartridgeBars[i].localScale=new Vector3(len,.26f,.42f);
                cartridgeHandles[i].localPosition=new Vector3(len*.55f+.22f,.16f,0f);
                cartridgeLabels[i].text=c.id+"\nBO="+c.BOLength+"  DO="+c.DOLength+" cm";
                cartridgeRoots[i].gameObject.SetActive(gamePhase!=GamePhase.Practice||i==0);
                RotateCartridgeVisual(i,cartridgeRotation[i],false);
            }
            promptUi.text=current.prompt;
            evidenceUi.text=gamePhase==GamePhase.Practice
                ?"연습 · AO=CO=3 cm  |  BO=DO=4 cm\n두 대각선이 서로 다른 것을 이등분하는지 확인하시오"
                :"활성 연틀과 잡은 살대의 AO=CO / BO=DO / AC²=BD² / 내적을 크게 표시한다";
            evidenceImage.color=new Color32(231,224,207,238);
            UpdateControlCoordinates();
        }

        void ResetAllVisuals(){ResetBoardVisuals();for(int i=0;i<kiteRoots.Length;i++)kiteRoots[i].gameObject.SetActive(false);skyOpenTarget=0f;}
        void ResetBoardVisuals()
        {
            for(int i=0;i<9;i++){silkPanels[i].SetActive(false);knotRoots[i].SetActive(false);previewRings[i].SetActive(false);silkPanels[i].transform.localScale=Vector3.one;}
            for(int i=0;i<3;i++){cartridgeRoots[i].localPosition=cartridgeHomes[i];cartridgeRoots[i].localScale=Vector3.one;}
            dragTrail.enabled=false;previewDock=-1;
        }

        void SetPhaseUi()
        {
            bool title=gamePhase==GamePhase.Title,play=gamePhase==GamePhase.Practice||gamePhase==GamePhase.Playing||gamePhase==GamePhase.Reveal,end=gamePhase==GamePhase.End;
            titleRoot.SetActive(title);playRoot.SetActive(play);endRoot.SetActive(end);
            // 타이틀도 실제 공방과 연틀을 보여 주어 플레이 화면과 같은 세계로 잇는다.
            boardRoot.gameObject.SetActive(true);rackRoot.gameObject.SetActive(play); 
            if(title){roofRoot.gameObject.SetActive(true);flagRoot.gameObject.SetActive(true);}
            if(play&&current!=null)BindBundle();
        }

        void ApplyResponsiveLayout()
        {
            RectTransform canvasRt=(RectTransform)MgfText.Canvas.transform;
            float logicalW=Mathf.Max(1f,canvasRt.rect.width),logicalH=Mathf.Max(1f,canvasRt.rect.height);
            // WebGL의 브라우저 resize에서는 Screen 크기와 CanvasScaler rect가 서로 다른
            // 프레임에 갱신될 수 있다. 둘 다 캐시해야 초와이드 전환 때 이전 폭으로 고정되지 않는다.
            if(Screen.width==lastScreenW&&Screen.height==lastScreenH&&
                Mathf.Abs(logicalW-lastCanvasW)<.5f&&Mathf.Abs(logicalH-lastCanvasH)<.5f)return;
            lastScreenW=Screen.width;lastScreenH=Screen.height;lastCanvasW=logicalW;lastCanvasH=logicalH;
            float aspect=Mathf.Max(.1f,(float)Screen.width/Mathf.Max(1,Screen.height));land=aspect>=1.20f;
            if(land)
            {
                cam.orthographicSize=7.1f;cam.transform.position=new Vector3(0f,15.6f,-18.8f);cam.transform.LookAt(new Vector3(0f,.25f,.3f));
                boardRoot.localScale=Vector3.one;boardRoot.position=new Vector3(0f,.25f,.8f);rackRoot.position=new Vector3(0f,.18f,-5.0f);rackRoot.localScale=Vector3.one;
                float stageW=Mathf.Min(1328f,logicalW-32f);
                Rect(goalRt,new Vector2(.5f,1f),new Vector2(0f,-23f),new Vector2(Mathf.Min(700f,stageW-40f),42f));
                Rect(hudRt,new Vector2(.5f,1f),new Vector2(0f,-61f),new Vector2(Mathf.Min(620f,stageW-40f),38f));
                Rect(promptRt,new Vector2(.5f,1f),new Vector2(0f,-132f),new Vector2(Mathf.Min(940f,stageW-40f),104f));
                Rect(evidenceRt,new Vector2(.5f,0f),new Vector2(0f,78f),new Vector2(Mathf.Min(820f,stageW-40f),112f));
                Rect(toastRt,new Vector2(.5f,.5f),new Vector2(0f,-logicalH*.34f),new Vector2(480f,64f));
                promptUi.fontSize=16.5f;evidenceUi.fontSize=17.5f;hudUi.fontSize=16f;
            }
            else
            {
                cam.orthographicSize=10.6f;cam.transform.position=new Vector3(0f,18.8f,-13.8f);cam.transform.LookAt(new Vector3(0f,.2f,.25f));
                boardRoot.localScale=Vector3.one*.84f;boardRoot.position=new Vector3(0f,.25f,1.1f);rackRoot.position=new Vector3(0f,.18f,-5.4f);rackRoot.localScale=Vector3.one*.88f;
                Rect(goalRt,new Vector2(.5f,1f),new Vector2(0f,-22f),new Vector2(Mathf.Min(370f,logicalW-16f),42f));
                Rect(promptRt,new Vector2(.5f,1f),new Vector2(0f,-103f),new Vector2(Mathf.Min(374f,logicalW-12f),114f));
                Rect(hudRt,new Vector2(.5f,1f),new Vector2(0f,-171f),new Vector2(Mathf.Min(365f,logicalW-18f),42f));
                Rect(evidenceRt,new Vector2(.5f,0f),new Vector2(0f,154f),new Vector2(Mathf.Min(374f,logicalW-12f),118f));
                Rect(toastRt,new Vector2(.5f,.5f),new Vector2(0f,-logicalH*.23f),new Vector2(Mathf.Min(370f,logicalW-14f),68f));
                promptUi.fontSize=15.5f;evidenceUi.fontSize=17.5f;hudUi.fontSize=15f;
            }
            LayoutTitle(logicalW,logicalH);LayoutEnd(logicalW,logicalH);UpdateControlCoordinates();
        }

        void LayoutTitle(float w,float h)
        {
            float kiteSize=Mathf.Min(270f,w*.66f);
            Rect(titleBannerRt,new Vector2(.5f,.68f),Vector2.zero,new Vector2(kiteSize,kiteSize));
            titleBannerRt.localRotation=Quaternion.Euler(0f,0f,45f);
            titleLogo.rectTransform.localRotation=Quaternion.Euler(0f,0f,-45f);
            titleLogoShadow.rectTransform.localRotation=Quaternion.Euler(0f,0f,-45f);
            titleTag.rectTransform.localRotation=Quaternion.Euler(0f,0f,-45f);
            titleLogo.rectTransform.sizeDelta=new Vector2(kiteSize*.92f,82f);
            titleLogoShadow.rectTransform.sizeDelta=titleLogo.rectTransform.sizeDelta;
            titleTag.rectTransform.sizeDelta=new Vector2(kiteSize*.94f,68f);
            Rect((RectTransform)titleMeta.transform,new Vector2(.5f,.95f),Vector2.zero,new Vector2(Mathf.Min(350f,w-40f),34f));
            Rect(titleCordRt,new Vector2(.5f,.18f),new Vector2(0f,-titleCordPull*.16f),new Vector2(Mathf.Min(w*.94f,620f),10f));
            Rect(titleHandleRt,new Vector2(.5f,.18f),new Vector2(0f,-titleCordPull*.42f),new Vector2(82f,82f));
            titleHandleRt.localRotation=Quaternion.Euler(0,0,45f);
            Rect((RectTransform)titleCordText.transform,new Vector2(.5f,.09f),new Vector2(0f,-titleCordPull*.18f),new Vector2(Mathf.Min(360f,w-26f),52f));
        }

        void LayoutEnd(float w,float h)
        {
            Rect(endPanelRt,new Vector2(.5f,.56f),Vector2.zero,new Vector2(Mathf.Min(520f,w-28f),Mathf.Min(390f,h*.52f)));
            Rect(endCordRt,new Vector2(.5f,.22f),new Vector2(0f,-endCordPull*.20f),new Vector2(Mathf.Min(430f,w*.84f),10f));
            Rect(endHandleRt,new Vector2(.5f,.22f),new Vector2(0f,-endCordPull*.40f),new Vector2(72f,72f));endHandleRt.localRotation=Quaternion.Euler(0,0,45f);
            Rect((RectTransform)endCordUi.transform,new Vector2(.5f,.12f),Vector2.zero,new Vector2(260f,48f));
        }

        void AnimateWorld(float dt)
        {
            worldClock+=dt;float t=worldClock;
            if(!MgfBridge.LowGfx)
            {
                for(int i=0;i<flags.Length;i++)if(flags[i])flags[i].localRotation=Quaternion.Euler(0f,Mathf.Sin(t*.46f+i)*14f,(i%2==0?1f:-1f)*(7f+Mathf.Sin(t*1.1f+i)*8f));
                float threadSpeed=Mathf.Lerp(2.6f,4.2f,Mathf.Clamp01(skyOpen*.75f));
                for(int i=0;i<rainNeedles.Length;i++)if(rainNeedles[i])
                {
                    Vector3 p=rainHomes[i];p.x+=Mathf.Repeat(t*threadSpeed+i*.37f,24f)-12f;
                    p.y+=Mathf.Sin(t*.9f+i)*.22f;rainNeedles[i].localPosition=p;
                }
            }
            birdCheer=Mathf.Max(0f,birdCheer-dt);birdStartle=Mathf.Max(0f,birdStartle-dt);
            for(int i=0;i<windBirds.Length;i++)if(windBirds[i])
            {
                float hop=birdCheer>0f?Mathf.Sin(Mathf.Clamp01(birdCheer/1.15f)*Mathf.PI)*1.35f:0f;
                float recoil=birdStartle>0f?Mathf.Sin(Mathf.Clamp01(birdStartle/.8f)*Mathf.PI)*.48f:0f;
                windBirds[i].position=windBirdHomes[i]+Vector3.up*(Mathf.Sin(t*1.7f+i)*.15f+hop)+Vector3.back*recoil;
                windBirds[i].localRotation=Quaternion.Euler(Mathf.Sin(t*2.1f+i)*4f,Mathf.Sin(t*.7f+i)*10f,birdCheer>0f?Mathf.Sin(t*18f+i)*13f:0f);
            }
            skyOpen=Mathf.Lerp(skyOpen,skyOpenTarget,dt*1.6f);
            for(int i=0;i<cloudPlates.Length;i++)if(cloudPlates[i])
            {
                float side=(i&1)==0?-1f:1f;Vector3 p=cloudPlates[i].position;p.x+=side*skyOpen*dt*(.3f+(i%3)*.08f);p.x+=Mathf.Sin(t*.08f+i)*dt*.08f;cloudPlates[i].position=p;
            }
            if(launchKiteCount>0)
            {
                launchClock+=dt;float k=Mathf.Clamp01(launchClock/Mathf.Max(.1f,launchDuration));
                for(int i=0;i<launchKiteCount;i++)if(kiteRoots[i].gameObject.activeSelf)
                {
                    float delay=i*.07f;float u=Mathf.Clamp01((k-delay)/(1f-delay));float eased=1f-Mathf.Pow(1f-u,3f);
                    Vector3 end=kiteStart[i]+new Vector3((i-2.5f)*1.1f,8.8f,9.0f+i*.35f);
                    kiteRoots[i].position=Vector3.Lerp(kiteStart[i],end,eased)+Vector3.up*Mathf.Sin(u*Mathf.PI)*1.2f;
                    kiteRoots[i].localScale=Vector3.one*Mathf.Lerp(.35f,1.15f,Mathf.Sin(Mathf.Min(1f,u*1.15f)*Mathf.PI*.5f));
                    kiteRoots[i].localRotation=Quaternion.Euler(Mathf.Lerp(0f,-22f,eased),Mathf.Sin(u*9f+i)*10f,Mathf.Sin(u*7f+i)*12f);
                }
                if(k>=1f){for(int i=0;i<launchKiteCount;i++)kiteRoots[i].gameObject.SetActive(false);launchKiteCount=0;}
            }
            if(cameraZoomKick>0f){cameraZoomKick=Mathf.Max(0f,cameraZoomKick-dt*1.8f);cam.orthographicSize-=Mathf.Sin(cameraZoomKick*Mathf.PI)*dt*.45f;}
            AnimateTitle(dt);AnimateGuide(dt);AnimateRipples(dt);AnimateTokens(dt);
        }

        void AnimateTitle(float dt)
        {
            if(!titleRoot.activeSelf)return;
            float pulse=1f+Mathf.Sin(worldClock*.75f)*.012f;titleArtRt.localScale=Vector3.one*pulse;titleArtRt.anchoredPosition=new Vector2(Mathf.Sin(worldClock*.19f)*5f,Mathf.Sin(worldClock*.13f)*4f);
            float logo=1f+Mathf.Max(0f,Mathf.Sin(worldClock*1.25f))*.018f;titleLogo.rectTransform.localScale=Vector3.one*logo;titleLogoShadow.rectTransform.localScale=titleLogo.rectTransform.localScale;
            for(int i=0;i<titleRain.Length;i++)
            {
                RectTransform rt=titleRain[i].rectTransform;float y=Mathf.Repeat(titleRainOffset[i]+worldClock*(.16f+(i%4)*.025f),1.25f)-.15f;rt.anchorMin=rt.anchorMax=new Vector2((i*37%101)/100f,1f-y);
            }
        }

        void AnimateGuide(float dt)
        {
            if(guideLeft<=0f)return;guideLeft-=dt;
            if(guideLeft<=0f){guideDot.gameObject.SetActive(false);guideLine.gameObject.SetActive(false);guideArrow.gameObject.SetActive(false);return;}
            Vector2 start=CartridgeCenterScreen(0);Vector2 end=gamePhase==GamePhase.Practice?DockCenterScreen(4):new Vector2(Screen.width*.5f,Screen.height*.54f);
            float cycle=1f-(guideLeft%1.65f)/1.65f;Vector2 p;
            if(cycle<.42f){float a=Mathf.Lerp(0f,90f,cycle/.42f)*Mathf.Deg2Rad;p=start+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Mathf.Max(34f,Screen.width*.085f);}
            else p=Vector2.Lerp(start+Vector2.up*Mathf.Max(34f,Screen.width*.085f),end,Mathf.SmoothStep(0,1,(cycle-.42f)/.58f));
            PositionScreen(guideDot.rectTransform,p);PositionScreen(guideArrow.rectTransform,end);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)playRoot.transform,start,null,out var a0);RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)playRoot.transform,end,null,out var b0);PlaceUiLine(guideLine.rectTransform,a0,b0,guideLarge?8f:5f);
            float s=(guideLarge?1.45f:1f)+Mathf.Sin(worldClock*8f)*.08f;guideDot.rectTransform.localScale=Vector3.one*s;
        }

        void AnimateRipples(float dt)
        {
            for(int i=0;i<ripples.Length;i++)if(rippleLife[i]>0f)
            {
                rippleLife[i]-=dt;float k=1f-Mathf.Clamp01(rippleLife[i]/.48f);ripples[i].rectTransform.sizeDelta=Vector2.one*Mathf.Lerp(22f,92f,k);ripples[i].color=new Color32(135,185,200,(byte)Mathf.Lerp(190f,0f,k));if(rippleLife[i]<=0f)ripples[i].gameObject.SetActive(false);
            }
        }

        void AnimateTokens(float dt)
        {
            for(int i=0;i<9;i++)if(silkPanels[i].activeSelf)
            {
                Vector3 s=silkPanels[i].transform.localScale;silkPanels[i].transform.localScale=Vector3.Lerp(s,Vector3.one,dt*7f);
                silkPanels[i].transform.localPosition=new Vector3(0f,.30f+Mathf.Sin(worldClock*.9f+i)*.018f,0f);
            }
        }

        void UpdateUi(float dt)
        {
            if(scoreDisplay!=st.score){scoreDisplay=Mathf.MoveTowards(scoreDisplay,st.score,Mathf.Max(90f,Mathf.Abs(st.score-scoreDisplay)*4f)*dt);if(Mathf.Abs(scoreDisplay-st.score)<.5f)scoreDisplay=st.score;if(lastScore!=st.score){lastScore=st.score;hudUi.rectTransform.localScale=Vector3.one*1.12f;}}
            if(hudUi)hudUi.rectTransform.localScale=Vector3.Lerp(hudUi.rectTransform.localScale,Vector3.one,dt*9f);
            if(playRoot.activeSelf)
            {
                int sec=Mathf.CeilToInt(runLeft);
                hudUi.text="연 "+st.launched+"/12   매듭 여유 "+st.lives+"   "+sec+"초   "+Mathf.RoundToInt(scoreDisplay)+"점";
                if(gamePhase==GamePhase.Playing&&current!=null)promptUi.text=current.prompt;
            }
            if(toastLeft>0f){toastLeft-=dt;toastRt.localScale=Vector3.Lerp(toastRt.localScale,Vector3.one,dt*12f);if(toastLeft<=0f)toastRt.gameObject.SetActive(false);}
            if(endRoot.activeSelf)
            {
                endScoreDisplay=Mathf.MoveTowards(endScoreDisplay,st.score,Mathf.Max(120f,st.score*1.8f)*dt);
                endStatsUi.text="띄운 연 "+st.launched+" / 12\n첫 시도 "+st.firstCorrect+" / "+st.firstAttempts+"\n점수 "+Mathf.RoundToInt(endScoreDisplay)+"\n"+EndReasonText();
                endPanelRt.localScale=Vector3.Lerp(endPanelRt.localScale,Vector3.one,dt*6f);
            }
        }

        string EndReasonText()
        {
            switch(endReason){case"clear":return"먹구름에 네 번째 바람길을 열었다";case"mastery_failed":return"완주는 했지만 첫 시도 9회에 못 미쳤다";case"timeout":return"90초가 지나 발사줄이 잠겼다";case"no_moves":return"빈 연틀에 맞는 살대가 없어 판이 끝났다";default:return"먹물 매듭 세 개가 연틀을 잠갔다";}
        }

        void SetTitleCordPull(float pixels){titleCordPull=Mathf.Clamp(pixels,0f,110f*Mathf.Max(1f,MgfText.Canvas.scaleFactor));LayoutTitle(Screen.width/Mathf.Max(.01f,MgfText.Canvas.scaleFactor),Screen.height/Mathf.Max(.01f,MgfText.Canvas.scaleFactor));float k=Mathf.Clamp01(titleCordPull/70f);titleCordImage.color=Color.Lerp(Vermilion,Pearl,k);titleHandleRt.localScale=new Vector3(1f+k*.14f,1f-k*.18f,1f);}
        void PulseTitleCord(){titleHandleRt.localScale=Vector3.one*1.24f;MgfSfx.Play("wrong",.06f);}
        void ShowTitleHint(){titleCordText.text="주홍 발사줄을 아래로 당기시오";}
        void SetEndCordPull(float pixels){endCordPull=Mathf.Clamp(pixels,0f,90f*Mathf.Max(1f,MgfText.Canvas.scaleFactor));LayoutEnd(Screen.width/Mathf.Max(.01f,MgfText.Canvas.scaleFactor),Screen.height/Mathf.Max(.01f,MgfText.Canvas.scaleFactor));}
        void PulseEndCord(){endHandleRt.localScale=Vector3.one*1.2f;}

        int CartridgeAtScreen(Vector2 p)
        {
            float radius=Mathf.Max(56f,Screen.width*.09f);int best=-1;float dist=float.MaxValue;
            for(int i=0;i<3;i++)if(cartridgeRoots[i].gameObject.activeSelf){float d=Vector2.Distance(p,CartridgeCenterScreen(i));if(d<radius&&d<dist){best=i;dist=d;}}
            return best;
        }
        Vector2 CartridgeCenterScreen(int i){if(i<0||i>=3||!cam)return Vector2.zero;Vector3 p=cam.WorldToScreenPoint(cartridgeRoots[i].position);return new Vector2(p.x,p.y);}
        Vector2 DockCenterScreen(int i){if(i<0||i>=9||!cam)return Vector2.zero;Vector3 p=cam.WorldToScreenPoint(frameRoots[i].position);return new Vector2(p.x,p.y);}

        int DockAtScreen(Vector2 p,bool requireEmpty)
        {
            int best=-1;float bestD=Mathf.Max(62f,Screen.width*.095f);
            for(int i=0;i<9;i++){if(requireEmpty&&cells[i]!=0)continue;float d=Vector2.Distance(p,DockCenterScreen(i));if(d<bestD){bestD=d;best=i;}}
            return best;
        }

        void SelectCartridgeVisual(int cart,bool on){if(cart<0||cart>=3)return;cartridgeRoots[cart].localScale=on?new Vector3(1.10f,.92f,1.10f):Vector3.one;MgfFx.Punch(cartridgeHandles[cart],on?.20f:.10f,.22f);if(on)UpdateEvidence(cart,-1);}
        void RotateCartridgeVisual(int cart,int rotation,bool punch)
        {
            if(current==null||cart<0||cart>=3)return;IVec2 v=current.cartridges[cart].V.Rotate(rotation);float angle=-Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg;cartridgeRoots[cart].localRotation=Quaternion.Euler(0f,angle,0f);if(punch)MgfFx.Punch(cartridgeRoots[cart],.13f,.20f);UpdateEvidence(cart,previewDock);
        }
        void MoveCartridgeToScreen(int cart,Vector2 screen)
        {
            if(!ScreenToPlane(screen,1.12f,out var point))return;cartridgeRoots[cart].position=point;cartridgeRoots[cart].localScale=Vector3.one*1.08f;
        }
        void ReturnCartridgeVisual(int cart,int rotation){cartridgeRoots[cart].SetParent(rackRoot,false);cartridgeRoots[cart].localPosition=cartridgeHomes[cart];RotateCartridgeVisual(cart,rotation,false);cartridgeRoots[cart].localScale=Vector3.one;}
        bool ScreenToPlane(Vector2 screen,float height,out Vector3 point){Ray ray=cam.ScreenPointToRay(screen);Plane plane=new Plane(Vector3.up,new Vector3(0,height,0));if(plane.Raycast(ray,out float d)){point=ray.GetPoint(d);return true;}point=Vector3.zero;return false;}

        void UpdateTrail(Vector2 start,Vector2 end,bool visible)
        {
            dragTrail.enabled=visible;if(!visible)return;
            ScreenToPlane(start,1.02f,out var a);ScreenToPlane(end,1.02f,out var b);
            for(int i=0;i<8;i++){float k=i/7f;Vector3 p=Vector3.Lerp(a,b,k);p.y+=Mathf.Sin(k*Mathf.PI)*.24f;dragTrail.SetPosition(i,p);}
        }
        void PreviewDock(int dock){if(previewDock==dock)return;if(previewDock>=0)previewRings[previewDock].SetActive(false);previewDock=dock;if(dock>=0&&cells[dock]==0){previewRings[dock].SetActive(true);UpdateEvidence(selectedCartridge,dock);}}
        void UpdateEvidence(int cart,int dock)
        {
            if(current==null||cart<0||cart>=3)return;CartridgeSpec c=current.cartridges[cart];
            if(dock<0||dock>=9){evidenceUi.text=c.id+" · BO="+c.BOLength+" cm  |  DO="+c.DOLength+" cm\n활성 연틀 가까이 가져가 AC²·BD²와 내적을 비교하시오";return;}
            DockSpec d=current.docks[dock];int r=cartridgeRotation[cart];string right=YeonRules.Perpendicular(d,c,r)?"0 (수직)":YeonRules.DotValue(d,c,r).ToString();
            evidenceUi.text=YeonRules.GoalName(d.goal)+" 연틀\nAO=CO="+d.uLength+" cm   |   BO="+c.BOLength+" cm, DO="+c.DOLength+" cm\nAC²="+YeonRules.AcSquared(d)+"   |   BD²="+YeonRules.BdSquared(c)+"   |   내적="+right;
            evidenceImage.color=new Color32(231,224,207,238);
        }

        void RefuseAt(Vector2 screen,string msg){SpawnRipple(screen);ShowToast(msg,2.0f);MgfSfx.Play("wrong",.08f);}
        void SpawnRipple(Vector2 screen){int i=rippleCursor++%ripples.Length;PositionScreen(ripples[i].rectTransform,screen);ripples[i].rectTransform.sizeDelta=Vector2.one*22f;ripples[i].color=new Color32(135,185,200,190);ripples[i].gameObject.SetActive(true);rippleLife[i]=.48f;}
        void ShakeNearestDock(Vector2 screen){int dock=DockAtScreen(screen,false);if(dock>=0)ShakeDock(dock);}
        void ShakeDock(int dock){if(dock<0)return;MgfFx.Punch(frameRoots[dock],.10f,.24f);}
        void ReplayGuide(bool large){guideLeft=large?3.4f:2.5f;guideLarge=large;guideDot.gameObject.SetActive(true);guideLine.gameObject.SetActive(true);guideArrow.gameObject.SetActive(true);}

        void PlacePracticeSilk(int dock,int cart,int rotation){PlaceWindToken(dock,cart,rotation);}
        void ShowPracticeReveal(){evidenceUi.text="AO=CO=3 cm   |   BO=DO=4 cm\n두 대각선이 서로 다른 것을 이등분 → 평행사변형";evidenceImage.color=new Color32(231,224,207,252);ShowToast("대각선 길이는 달라도 서로 이등분하면 평행사변형이다",1.45f);LaunchPracticeKite();}
        void ClearPracticeSilk(){silkPanels[4].SetActive(false);}
        void LaunchPracticeKite(){launchKiteCount=1;launchClock=0f;launchDuration=1.5f;kiteStart[0]=frameRoots[4].position+Vector3.up*.7f;kiteRoots[0].position=kiteStart[0];kiteRoots[0].localScale=Vector3.one*.25f;kiteRoots[0].gameObject.SetActive(true);}

        void PlaceWindToken(int dock,int cart,int rotation)
        {
            silkPanels[dock].SetActive(true);silkPanels[dock].transform.localScale=new Vector3(.12f,1f,.12f);knotRoots[dock].SetActive(false);MgfFx.Glow(frameRoots[dock].position+Vector3.up*.8f,Pearl,10,.26f);MgfFx.Punch(frameRoots[dock],.14f,.26f);birdCheer=1.15f;
        }
        void PlaceInkKnot(int dock,int cart,int rotation){knotRoots[dock].SetActive(true);knotRoots[dock].transform.localScale=Vector3.one*.22f;silkPanels[dock].SetActive(false);MgfFx.Punch(knotRoots[dock].transform,.34f,.34f);birdStartle=.8f;}
        void ShowCorrectEvidence(DockSpec d,CartridgeSpec c,int rotation)
        {
            string extra=d.goal==FrameGoal.Parallelogram&&!YeonRules.EqualDiagonals(d,c)?" · 길이가 달라도 성립한다":"";
            evidenceUi.text="성립 · BO=DO"+(d.goal==FrameGoal.Rectangle||d.goal==FrameGoal.Square?" · AC²=BD²":"")+(d.goal==FrameGoal.Rhombus||d.goal==FrameGoal.Square?" · 내적 0":"")+extra;
            evidenceImage.color=new Color32(214,230,220,248);
        }
        void ShowWrongEvidence(DockSpec d,CartridgeSpec c,int rotation,bool practice)
        {
            string explanation=YeonRules.WrongExplanation(d,c,rotation);
            evidenceUi.text="빠진 성질 · "+explanation+"\nAO=CO="+d.uLength+" cm  |  BO="+c.BOLength+" cm, DO="+c.DOLength+" cm";
            evidenceImage.color=new Color32(236,205,194,248);ShowToast(practice?"90° 돌린 뒤 가운데 연틀에 꽂으시오":explanation,2.2f);
        }
        void ShakeWrong(int dock,bool severe){MgfFx.Shake(cam,severe?.18f:.10f,severe?.34f:.24f);MgfFx.Punch(knotRoots[dock].transform,severe?.42f:.28f,.34f);}

        void StartLineLaunch(int clearMask,int knotMask,int lineCount)
        {
            launchKiteCount=Mathf.Min(6,lineCount*3);launchClock=0f;launchDuration=lineCount>=2?1.85f:1.55f;int cursor=0;
            for(int i=0;i<9&&cursor<launchKiteCount;i++)if((clearMask&(1<<i))!=0){kiteStart[cursor]=frameRoots[i].position+Vector3.up*.62f;kiteRoots[cursor].position=kiteStart[cursor];kiteRoots[cursor].localScale=Vector3.one*.22f;kiteRoots[cursor].gameObject.SetActive(true);cursor++;}
            while(cursor<launchKiteCount){int src=cursor%9;kiteStart[cursor]=frameRoots[src].position+Vector3.up*.62f;kiteRoots[cursor].position=kiteStart[cursor];kiteRoots[cursor].gameObject.SetActive(true);cursor++;}
            skyOpenTarget=Mathf.Clamp01(skyOpenTarget+(lineCount>=2?.52f:.24f));cameraZoomKick=lineCount>=2?1f:.45f;
            evidenceUi.text=lineCount>=2?"가로·세로 두 바람길 연결 · 여섯 연 동시 발사":"바람줄 세 칸 연결 · 연 세 장 발사";
            evidenceImage.color=new Color32(218,232,232,248);MgfFx.Glow(new Vector3(0,3f,2f),RainBlue,lineCount>=2?28:16,.42f);
        }
        void ClearLineVisuals(int clearMask,int knotMask){for(int i=0;i<9;i++){if((clearMask&(1<<i))!=0)silkPanels[i].SetActive(false);if((knotMask&(1<<i))!=0)knotRoots[i].SetActive(false);}}

        void ShowToast(string text,float seconds){toastUi.text=text;toastRt.gameObject.SetActive(true);toastRt.localScale=new Vector3(.92f,.92f,1f);toastLeft=seconds;}
        void ShowEnd(string reason){endTitleUi.text=reason=="clear"?"하늘 개방":reason=="mastery_failed"?"숙련 인장 보류":"발사줄 잠김";endScoreDisplay=0f;endPanelRt.localScale=new Vector3(.82f,.82f,1f);skyOpenTarget=reason=="clear"?1f:skyOpenTarget;}

        void PositionScreen(RectTransform rt,Vector2 screen){RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)playRoot.transform,screen,null,out var local);rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.anchoredPosition=local;}
        static void PlaceUiLine(RectTransform rt,Vector2 a,Vector2 b,float width){Vector2 d=b-a;rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=(a+b)*.5f;rt.sizeDelta=new Vector2(d.magnitude,width);rt.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);}

        void UpdateControlCoordinates()
        {
            st.screenW=Screen.width;st.screenH=Screen.height;
            for(int i=0;i<9;i++){Vector2 p=DockCenterScreen(i);st.dockPx[i*2]=Mathf.RoundToInt(p.x);st.dockPx[i*2+1]=Mathf.RoundToInt(Screen.height-p.y);}
            for(int i=0;i<3;i++){Vector2 p=CartridgeCenterScreen(i);st.cartridgePx[i*2]=Mathf.RoundToInt(p.x);st.cartridgePx[i*2+1]=Mathf.RoundToInt(Screen.height-p.y);}
            Vector2 t=RectCenterScreen(titleHandleRt),e=RectCenterScreen(endHandleRt);st.chromePx[0]=Mathf.RoundToInt(t.x);st.chromePx[1]=Mathf.RoundToInt(Screen.height-t.y);st.chromePx[2]=Mathf.RoundToInt(e.x);st.chromePx[3]=Mathf.RoundToInt(Screen.height-e.y);
            StoreRectPixels(goalRt,0);StoreRectPixels(promptRt,4);StoreRectPixels(hudRt,8);StoreRectPixels(evidenceRt,12);
        }
        void StoreRectPixels(RectTransform rt,int offset)
        {
            Vector3[] c=new Vector3[4];rt.GetWorldCorners(c);
            st.panelPx[offset]=Mathf.RoundToInt(c[0].x);
            st.panelPx[offset+1]=Mathf.RoundToInt(Screen.height-c[2].y);
            st.panelPx[offset+2]=Mathf.RoundToInt(c[2].x-c[0].x);
            st.panelPx[offset+3]=Mathf.RoundToInt(c[2].y-c[0].y);
        }
        static Vector2 RectCenterScreen(RectTransform rt){if(!rt)return Vector2.zero;Vector3[] c=new Vector3[4];rt.GetWorldCorners(c);return RectTransformUtility.WorldToScreenPoint(null,(c[0]+c[2])*.5f);}
    }
}
