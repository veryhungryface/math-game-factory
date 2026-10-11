// 연 띄워 — 먹구름을 가르는 흑칠 연 관측옥상 2.5D 월드, 3×3 연틀, 비단 발사와 반응형 UI.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.YeonTtwiwo
{
    public partial class YeonTtwiwoGame
    {
        static readonly Color Night = MgfLook.Hex("151B26");
        static readonly Color Slate = MgfLook.Hex("33435C");
        static readonly Color Pearl = MgfLook.Hex("E7E0CF");
        static readonly Color Vermilion = MgfLook.Hex("D94A38");
        static readonly Color RainBlue = MgfLook.Hex("87B9C8");
        static readonly Color InkBlack = MgfLook.Hex("090D14");
        static readonly Color Brass = MgfLook.Hex("BDA56F");
        static readonly Color Bamboo = MgfLook.Hex("352826");
        static readonly Color Leaf = MgfLook.Hex("52687B");
        static readonly Color Sky = MgfLook.Hex("0C1422");

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
        Transform[] cartridgeNotches = new Transform[3];
        Transform[] cartridgeHandles = new Transform[3];
        Transform[] cartridgeLeftMarks = new Transform[3];
        Transform[] cartridgeRightMarks = new Transform[3];
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
        float guideLeft, guideTotal;
        bool guideLarge;
        int previewDock = -1;
        float toastLeft, titleCordPull, endCordPull, scoreDisplay, endScoreDisplay;
        int lastScore = -1, lastScreenW = -1, lastScreenH = -1;
        float lastCanvasW = -1f, lastCanvasH = -1f;
        bool land;

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("080D18"), MgfLook.Hex("18263B"), MgfLook.Hex("050810"), .78f);
            MgfLook.Sun(new Vector3(44f, -32f, -18f), MgfLook.Hex("CDE8F1"), 1.10f, .72f);
            cam = MgfLook.Camera(new Vector3(0f, 15.5f, -18.5f), new Vector3(0f, .3f, .1f), 35f);
            cam.orthographic = true;
            cam.orthographicSize = 7.3f;

            lacquerMat = MgfLook.Lit(MgfLook.Hex("0C121C"), .62f, .42f);
            lacquerGlowMat = MgfLook.Lit(MgfLook.Hex("20334B"), .68f, .36f);
            frameMat = MgfLook.Lit(Bamboo, .34f, .08f);
            fixedMat = MgfLook.Lit(MgfLook.Hex("8CBBC8"), .56f, .18f);
            silkMat = MgfLook.Lit(Pearl, .22f, .02f);
            knotMat = MgfLook.Lit(MgfLook.Hex("1B1220"), .30f, .08f);
            vermilionMat = MgfLook.Lit(Vermilion, .42f, .10f);
            tileMatA = MgfLook.Lit(MgfLook.Hex("111A29"), .38f, .22f);
            tileMatB = MgfLook.Lit(MgfLook.Hex("26354B"), .28f, .12f);

            worldRoot = new GameObject("StormKiteObservatory").transform;
            roofRoot = new GameObject("WetTileWindTower").transform; roofRoot.SetParent(worldRoot, false);
            boardRoot = new GameObject("NineKiteFrames").transform; boardRoot.SetParent(worldRoot, false);
            rackRoot = new GameObject("LacquerCartridgeRail").transform; rackRoot.SetParent(worldRoot, false);
            flagRoot = new GameObject("WindVanesAndFlags").transform; flagRoot.SetParent(worldRoot, false);

            BuildRoof();
            BuildCloudsAndRain();
            BuildFrames();
            BuildRack();
            BuildKites();
            BuildTensionVanes();
            BuildTrail();
        }

        void BuildRoof()
        {
            // 선택안의 핵심인 깊은 하늘 공허와 수직 바람탑을 화면 가장자리까지 잇는다.
            MgfLook.Block("RoofVoid", new Vector3(0f,-.70f,.4f), new Vector3(26f,.55f,18.5f), .35f, lacquerMat, roofRoot);
            MgfLook.Block("WetTileDeck", new Vector3(0f,-.10f,.7f), new Vector3(13.8f,.28f,13.8f), .22f, tileMatA, roofRoot);
            for(int z=-6;z<=6;z++)
            for(int x=-6;x<=6;x++)
            {
                if(((x+z)&1)!=0)continue;
                var tile=MgfLook.Block("RoofScale"+x+"_"+z,new Vector3(x*1.08f,.08f,z*1.03f+.7f),new Vector3(1.02f,.10f,.92f),.12f,((x+z)&2)==0?tileMatA:tileMatB,roofRoot);
                tile.transform.localRotation=Quaternion.Euler(0f,0f,(x&1)==0?1.5f:-1.5f);
            }
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*9.1f;
                MgfLook.Block("WindTowerBase"+side,new Vector3(x,1.25f,1.0f),new Vector3(2.4f,2.7f,12.8f),.30f,lacquerMat,roofRoot);
                MgfLook.Block("WindTowerSpine"+side,new Vector3(x,4.9f,1.0f),new Vector3(.52f,7.2f,.52f),.16f,frameMat,roofRoot);
                for(int i=0;i<5;i++)
                {
                    float z=-4.5f+i*2.35f;
                    MgfLook.Block("TensionDrum"+side+"_"+i,new Vector3(x-side*.82f,2.05f,z),new Vector3(.72f,.72f,.72f),.31f,i%2==0?vermilionMat:fixedMat,roofRoot);
                    CreateBar("TensionCord"+side+"_"+i,new Vector3(x-side*.55f,2.35f,z),new Vector3(side*6.9f,.45f,z*.45f),.035f,i%2==0?vermilionMat:fixedMat,roofRoot);
                }
                MgfLook.Block("TowerLantern"+side,new Vector3(x,7.6f,1.0f),new Vector3(1.25f,1.45f,1.25f),.22f,MgfLook.Lit(MgfLook.Hex("B9DCE4"),.26f,0f,MgfLook.Hex("B9DCE4")*.18f),roofRoot);
            }
            for (int i = 0; i < flags.Length; i++)
            {
                float side = (i & 1) == 0 ? -1f : 1f;
                MgfLook.Block("WindPole" + i, new Vector3(side * (9.5f + (i % 2) * .7f), 2.25f, -5.5f + i * 1.85f),
                    new Vector3(.11f, 4.7f, .11f), .025f, lacquerGlowMat, flagRoot);
                var flag = MgfLook.Block("VermilionStormPennant" + i, new Vector3(side * (8.9f + (i % 2) * .7f), 3.65f, -5.5f + i * 1.85f),
                    new Vector3(1.12f, .06f, .42f), .02f, i%3==0?vermilionMat:i%3==1?fixedMat:silkMat, flagRoot);
                flag.transform.rotation = Quaternion.Euler(0f, side < 0 ? 12f : -12f, side < 0 ? 8f : -8f);
                flags[i] = flag.transform;
            }
        }

        void BuildCloudsAndRain()
        {
            var cloudMat = MgfLook.Lit(MgfLook.Hex("222D40"), .18f, .04f);
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
            var rainMat = MgfLook.Unlit(new Color(RainBlue.r, RainBlue.g, RainBlue.b, .74f));
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
            var moonDisk = MgfLook.Block("MoonSignal", new Vector3(8.2f, 9.0f, 9.4f), new Vector3(1.15f, 1.15f, .20f), .50f,
                MgfLook.Lit(MgfLook.Hex("D7EEF2"), .16f, 0f, MgfLook.Hex("D7EEF2")*.24f), worldRoot);
            moonDisk.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
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

                MgfLook.Block("LacquerTensionFrame", Vector3.zero, new Vector3(2.65f, .22f, 2.38f), .18f, tileMatB, root);
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

                frameGoalText[i] = MgfText.World("", new Vector3(0f,.36f,1.32f), .72f, Pearl, root);
                frameGoalText[i].transform.localRotation = Quaternion.Euler(90f,0f,0f);
                frameGoalText[i].outlineWidth=.16f;frameGoalText[i].outlineColor=InkBlack;
                frameMeasureText[i] = MgfText.World("A — O — C", new Vector3(0f,.37f,-1.27f), .58f, Pearl, root);
                frameMeasureText[i].transform.localRotation = Quaternion.Euler(90f,0f,0f);
                frameMeasureText[i].outlineWidth=.18f;frameMeasureText[i].outlineColor=InkBlack;
            }
            boardRoot.position = new Vector3(0f, .25f, .75f);
        }

        void BuildRack()
        {
            MgfLook.Block("BlackLacquerRail", new Vector3(0f,.40f,0f), new Vector3(10.4f,.62f,2.4f), .24f, lacquerGlowMat, rackRoot);
            for (int i = 0; i < cartridgeRoots.Length; i++)
            {
                var root = new GameObject("DiagonalCartridge" + i).transform; root.SetParent(rackRoot, false);
                root.localPosition = new Vector3((i - 1) * 3.05f, .86f, 0f);
                cartridgeRoots[i] = root;
                cartridgeHomes[i] = root.localPosition;
                cartridgeBars[i] = MgfLook.Block("EngravedLacquerSpar", Vector3.zero, new Vector3(2.25f,.26f,.42f), .12f, lacquerMat, root).transform;
                cartridgeNotches[i] = MgfLook.Block("IntersectionONotch", new Vector3(0f,.17f,0f), new Vector3(.22f,.32f,.62f), .07f, vermilionMat, root).transform;
                cartridgeHandles[i] = MgfLook.Block("Handle", new Vector3(1.28f,.16f,0f), new Vector3(.62f,.38f,.72f), .18f, vermilionMat, root).transform;
                cartridgeLeftMarks[i] = MgfLook.Block("PointBPaperMark", new Vector3(-1.01f,.05f,0f), new Vector3(.52f,.06f,.86f), .03f, silkMat, root).transform;
                cartridgeRightMarks[i] = MgfLook.Block("PointDPaperMark", new Vector3(1.01f,.05f,0f), new Vector3(.52f,.06f,.86f), .03f, silkMat, root).transform;
                // 측정값은 회전하는 살대의 자식이 아니라 레일에 고정해 항상 정방향으로 읽힌다.
                cartridgeLabels[i] = MgfText.World("", new Vector3((i-1)*3.05f,1.32f,-.82f), 1.02f, Pearl, rackRoot);
                cartridgeLabels[i].transform.localRotation = Quaternion.Euler(90f,0f,0f);
                cartridgeLabels[i].outlineWidth=.20f;cartridgeLabels[i].outlineColor=InkBlack;
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

        void BuildTensionVanes()
        {
            // 캐릭터 대신 관측옥상의 장력 풍향계가 정답·오답에 반응한다.
            for(int i=0;i<windBirds.Length;i++)
            {
                var root=new GameObject("TensionVane"+i).transform;root.SetParent(worldRoot,false);
                windBirdHomes[i]=new Vector3(-7.0f+i*7.0f,1.2f,5.2f-(i%2)*.7f);root.position=windBirdHomes[i];
                MgfLook.Block("GaugeHub",Vector3.zero,new Vector3(.44f,.46f,.44f),.18f,brassMat(),root);
                var needle=MgfLook.Block("Needle",new Vector3(0f,.18f,-.42f),new Vector3(.16f,.12f,1.15f),.05f,i==1?vermilionMat:fixedMat,root);
                needle.transform.localRotation=Quaternion.Euler(0f,(i-1)*16f,0f);
                var vane=MgfLook.Block("PearlVane",new Vector3(0f,.12f,.62f),new Vector3(1.05f,.08f,.48f),.04f,silkMat,root);
                vane.transform.localRotation=Quaternion.Euler(0f,45f,0f);
                windBirds[i]=root;
            }
        }

        Material brassMat(){return MgfLook.Lit(Brass,.52f,.34f);}

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
            // 같은 런타임 관측옥상이 타이틀부터 보이도록 얇은 청회색 기상 막만 얹는다.
            titleArt.texture = null; titleArt.color = new Color(Slate.r,Slate.g,Slate.b,.16f);
            titleArt.raycastTarget = false;

            for (int i = 0; i < titleRain.Length; i++)
            {
                var drop = Panel("TitleRain" + i, titleRoot.transform, new Color32(135,185,200,80), out titleRain[i]);
                var rt = (RectTransform)drop.transform; rt.anchorMin = rt.anchorMax = new Vector2((i * 37 % 101) / 100f, .5f);
                rt.sizeDelta = new Vector2(2f, 70f + (i % 5) * 18f); rt.localRotation = Quaternion.Euler(0,0,12f);
                titleRainOffset[i] = (i * 53 % 97) / 97f;
            }

            var banner = Panel("TowerPennant", titleRoot.transform, new Color32(18,27,42,235), out titleBannerImage);
            titleBannerRt = (RectTransform)banner.transform;
            titleBannerImage.raycastTarget = false;
            var outline = banner.AddComponent<Outline>(); outline.effectColor = new Color32(217,74,56,230); outline.effectDistance = new Vector2(0f,-3f);
            titleLogoShadow = MakeText("연 띄워", banner.transform, 46f, InkBlack, TextAlignmentOptions.Center);
            titleLogo = MakeText("연 띄워", banner.transform, 46f, Pearl, TextAlignmentOptions.Center);
            titleLogo.outlineWidth = .16f; titleLogo.outlineColor = new Color32(217,74,56,255);
            titleTag = MakeText("대각선 살대로 먹구름을 가르시오", banner.transform, 15f, RainBlue, TextAlignmentOptions.Center);
            titleMeta = MakeText("중학교 2학년 · 사각형의 성질", titleRoot.transform, 14f, Pearl, TextAlignmentOptions.Center);
            titleMeta.outlineWidth=.18f; titleMeta.outlineColor=new Color32(9,13,20,245);
            Rect((RectTransform)titleLogoShadow.transform,new Vector2(.5f,.64f),new Vector2(2f,-2f),new Vector2(340f,64f));
            Rect((RectTransform)titleLogo.transform,new Vector2(.5f,.64f),Vector2.zero,new Vector2(340f,64f));
            Rect((RectTransform)titleTag.transform,new Vector2(.5f,.22f),Vector2.zero,new Vector2(340f,36f));

            var cord = Panel("LaunchCord", titleRoot.transform, new Color32(217,74,56,245), out titleCordImage);
            titleCordRt = (RectTransform)cord.transform; titleCordImage.raycastTarget=false;
            var handle = Panel("LaunchCordHandle", titleRoot.transform, new Color32(231,224,207,250), out titleHandleImage);
            titleHandleRt = (RectTransform)handle.transform; titleHandleImage.raycastTarget=false;
            titleCordText = MakeText("주홍 발사줄을 눌러 관측을 시작하시오", titleRoot.transform, 17f, Pearl, TextAlignmentOptions.Center);
            titleCordText.outlineWidth=.20f; titleCordText.outlineColor=new Color32(9,13,20,245);
        }

        void BuildPlayUi()
        {
            var goalPanel = Panel("GoalBanner", playRoot.transform, new Color32(21,27,38,226), out _); goalRt=(RectTransform)goalPanel.transform;
            goalUi = MakeText("살대를 돌려 꽂고 바람줄 3칸을 이으시오", goalPanel.transform, 17f, Pearl, TextAlignmentOptions.Center);
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
                frameGoalText[i].fontSize=focus?1.18f:.66f;
                frameMeasureText[i].fontSize=focus?.88f:.52f;
                float len=Mathf.Lerp(1.45f,2.02f,(d.uLength-10)/10f);
                fixedBars[i].localScale=new Vector3(len,.095f,.095f);
                fixedBars[i].localRotation=Quaternion.Euler(0f,d.ux==0?90f:0f,0f);
                bool active=gamePhase!=GamePhase.Practice||i==4;
                frameGoalText[i].color=active?(focus?Vermilion:Pearl):new Color(Pearl.r,Pearl.g,Pearl.b,.20f);
                frameMeasureText[i].color=active?Pearl:new Color(Pearl.r,Pearl.g,Pearl.b,.18f);
            }
            for(int i=0;i<3;i++)
            {
                CartridgeSpec c=current.cartridges[i];
                st.cartridgeGeometry[i*4]=c.vx;st.cartridgeGeometry[i*4+1]=c.vy;st.cartridgeGeometry[i*4+2]=c.p;st.cartridgeGeometry[i*4+3]=c.q;
                float len=1.45f+(c.p+c.q)*.16f;
                cartridgeBars[i].localScale=new Vector3(len,.26f,.42f);
                // 막대 끝을 B(-)와 D(+)로 두고 교점 O를 BO:DO 비에 맞춰 옮긴다.
                // p=q일 때만 O가 중앙이며, 오개념 살대의 p!=q는 도식에서도 즉시 보인다.
                float oX=len*(c.p-c.q)/(2f*(c.p+c.q));
                cartridgeNotches[i].localPosition=new Vector3(oX,.17f,0f);
                cartridgeLeftMarks[i].localPosition=new Vector3(-len*.46f,.05f,0f);
                cartridgeRightMarks[i].localPosition=new Vector3(len*.46f,.05f,0f);
                cartridgeHandles[i].localPosition=new Vector3(len*.55f+.22f,.16f,0f);
                cartridgeLabels[i].text=c.id+"\nBO="+c.BOLength+" cm · DO="+c.DOLength+" cm";
                cartridgeRoots[i].gameObject.SetActive(gamePhase!=GamePhase.Practice||i==0);
                RotateCartridgeVisual(i,cartridgeRotation[i],false);
            }
            promptUi.text=DisplayPrompt(current);
            evidenceUi.text=gamePhase==GamePhase.Practice
                ?"연습 · AO=CO=3 cm  |  BO=DO=4 cm\n두 대각선이 서로 다른 것을 이등분하는지 확인하시오"
                :"연틀·살대의 AO=CO / BO=DO / AC²=BD² / 수직 여부를 비교하시오";
            evidenceImage.color=new Color32(231,224,207,238);
            UpdateControlCoordinates();
        }

        string DisplayPrompt(BundleSpec bundle)
        {
            if(bundle==null)return "";
            DockSpec d=bundle.docks[bundle.preferredDock];
            int row=bundle.preferredDock/3+1,col=bundle.preferredDock%3+1;
            return row+"행 "+col+"열 · "+YeonRules.GoalName(d.goal)+" · AO=CO="+d.uLength+" cm · AC "+(d.ux!=0?"가로":"세로")
                +"\n아래 살대의 BO·DO를 읽고 돌려 꽂으시오";
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
            // 타이틀도 실제 관측옥상과 연틀을 보여 주어 플레이 화면과 같은 세계로 잇는다.
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
                Rect(promptRt,new Vector2(.5f,1f),new Vector2(0f,-120f),new Vector2(Mathf.Min(860f,stageW-40f),78f));
                Rect(evidenceRt,new Vector2(.5f,0f),new Vector2(0f,62f),new Vector2(Mathf.Min(820f,stageW-40f),84f));
                Rect(toastRt,new Vector2(.5f,.5f),new Vector2(0f,-logicalH*.34f),new Vector2(480f,64f));
                goalUi.fontSize=17f;promptUi.fontSize=17f;evidenceUi.fontSize=17.5f;hudUi.fontSize=16f;
            }
            else
            {
                cam.orthographicSize=10.1f;cam.transform.position=new Vector3(0f,18.8f,-13.8f);cam.transform.LookAt(new Vector3(0f,.2f,.25f));
                boardRoot.localScale=Vector3.one*.90f;boardRoot.position=new Vector3(0f,.25f,1.1f);rackRoot.position=new Vector3(0f,.18f,-5.4f);rackRoot.localScale=Vector3.one*.92f;
                Rect(goalRt,new Vector2(.5f,1f),new Vector2(-28f,-29f),new Vector2(Mathf.Min(306f,logicalW-82f),56f));
                Rect(promptRt,new Vector2(.5f,1f),new Vector2(0f,-103f),new Vector2(Mathf.Min(370f,logicalW-20f),82f));
                Rect(hudRt,new Vector2(.5f,1f),new Vector2(0f,-153f),new Vector2(Mathf.Min(350f,logicalW-38f),38f));
                Rect(evidenceRt,new Vector2(.5f,0f),new Vector2(0f,118f),new Vector2(Mathf.Min(370f,logicalW-20f),96f));
                Rect(toastRt,new Vector2(.5f,.5f),new Vector2(0f,-logicalH*.23f),new Vector2(Mathf.Min(370f,logicalW-14f),68f));
                goalUi.fontSize=15.5f;promptUi.fontSize=16.5f;evidenceUi.fontSize=16.5f;hudUi.fontSize=14.5f;
            }
            LayoutTitle(logicalW,logicalH);LayoutEnd(logicalW,logicalH);UpdateControlCoordinates();
        }

        void LayoutTitle(float w,float h)
        {
            float bannerW=Mathf.Min(360f,w*.76f),bannerH=118f;
            Rect(titleBannerRt,new Vector2(.5f,.74f),Vector2.zero,new Vector2(bannerW,bannerH));
            titleBannerRt.localRotation=Quaternion.identity;
            titleLogo.rectTransform.localRotation=Quaternion.identity;
            titleLogoShadow.rectTransform.localRotation=Quaternion.identity;
            titleTag.rectTransform.localRotation=Quaternion.identity;
            titleLogo.rectTransform.sizeDelta=new Vector2(bannerW*.94f,64f);
            titleLogoShadow.rectTransform.sizeDelta=titleLogo.rectTransform.sizeDelta;
            titleTag.rectTransform.sizeDelta=new Vector2(bannerW*.94f,38f);
            Rect((RectTransform)titleMeta.transform,new Vector2(.5f,.96f),Vector2.zero,new Vector2(Mathf.Min(350f,w-72f),34f));
            Rect(titleCordRt,new Vector2(.5f,.15f),new Vector2(0f,-titleCordPull*.16f),new Vector2(Mathf.Min(w*.88f,620f),8f));
            Rect(titleHandleRt,new Vector2(.5f,.15f),new Vector2(0f,-titleCordPull*.42f),new Vector2(72f,72f));
            titleHandleRt.localRotation=Quaternion.Euler(0,0,45f);
            Rect((RectTransform)titleCordText.transform,new Vector2(.5f,.065f),new Vector2(0f,-titleCordPull*.18f),new Vector2(Mathf.Min(360f,w-34f),44f));
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
            float cycle=1f-Mathf.Clamp01(guideLeft/Mathf.Max(.01f,guideTotal));Vector2 p;
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
                if(gamePhase==GamePhase.Playing&&current!=null)promptUi.text=DisplayPrompt(current);
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
            float radius=Mathf.Max(gamePhase==GamePhase.Practice?92f:76f,Screen.width*(gamePhase==GamePhase.Practice?.16f:.12f));int best=-1;float dist=float.MaxValue;
            for(int i=0;i<3;i++)if(cartridgeRoots[i].gameObject.activeSelf){float d=Vector2.Distance(p,CartridgeCenterScreen(i));if(d<radius&&d<dist){best=i;dist=d;}}
            return best;
        }
        Vector2 CartridgeCenterScreen(int i){if(i<0||i>=3||!cam)return Vector2.zero;Vector3 p=cam.WorldToScreenPoint(cartridgeRoots[i].position);return new Vector2(p.x,p.y);}
        Vector2 DockCenterScreen(int i){if(i<0||i>=9||!cam)return Vector2.zero;Vector3 p=cam.WorldToScreenPoint(frameRoots[i].position);return new Vector2(p.x,p.y);}

        int DockAtScreen(Vector2 p,bool requireEmpty)
        {
            if(gamePhase==GamePhase.Practice)return 4;
            int best=-1;float bestD=Mathf.Max(66f,Screen.width*.10f);
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
            if(dock<0||dock>=9){evidenceUi.text=c.id+" · BO="+c.BOLength+" cm  |  DO="+c.DOLength+" cm\n연틀 가까이 가져가 길이와 수직 여부를 비교하시오";return;}
            DockSpec d=current.docks[dock];int r=cartridgeRotation[cart];string right=YeonRules.Perpendicular(d,c,r)?"예":"아니요";
            evidenceUi.text=YeonRules.GoalName(d.goal)+" · AO=CO="+d.uLength+" cm · BO="+c.BOLength+" cm, DO="+c.DOLength+" cm\nAC²="+YeonRules.AcSquared(d)+" · BD²="+YeonRules.BdSquared(c)+" · 두 대각선이 수직: "+right;
            evidenceImage.color=new Color32(231,224,207,238);
        }

        void RefuseAt(Vector2 screen,string msg){SpawnRipple(screen);ShowToast(msg,2.0f);MgfSfx.Play("wrong",.08f);}
        void SpawnRipple(Vector2 screen){int i=rippleCursor++%ripples.Length;PositionScreen(ripples[i].rectTransform,screen);ripples[i].rectTransform.sizeDelta=Vector2.one*22f;ripples[i].color=new Color32(135,185,200,190);ripples[i].gameObject.SetActive(true);rippleLife[i]=.48f;}
        void ShakeNearestDock(Vector2 screen){int dock=DockAtScreen(screen,false);if(dock>=0)ShakeDock(dock);}
        void ShakeDock(int dock){if(dock<0)return;MgfFx.Punch(frameRoots[dock],.10f,.24f);}
        void ReplayGuide(bool large){guideTotal=guideLeft=large?1.55f:1.25f;guideLarge=large;guideDot.gameObject.SetActive(true);guideLine.gameObject.SetActive(true);guideArrow.gameObject.SetActive(true);}

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
            evidenceUi.text="성립 · BO=DO"+(d.goal==FrameGoal.Rectangle||d.goal==FrameGoal.Square?" · AC²=BD²":"")+(d.goal==FrameGoal.Rhombus||d.goal==FrameGoal.Square?" · 두 대각선이 수직":"")+extra;
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
