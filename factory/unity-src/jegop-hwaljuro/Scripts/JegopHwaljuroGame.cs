using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.JegopHwaljuro
{
    public sealed class JegopHwaljuroGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int combo;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int pointerVersion;
            public int wasteBlocks;
            public string problemId = "";
            public string misconceptionId = "";
            public bool onboarding;
            public bool batteryReady;
            public string practiceStep = "";
        }

        enum Mode { Title, Practice, Playing, MergeFeedback, Battery, PracticeReveal, WrongFeedback, End }

        readonly State st = new State();
        readonly List<PanelProblem> catalog = new List<PanelProblem>();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly GameObject[] panelGo = new GameObject[5];
        readonly TextMeshPro[] panelText = new TextMeshPro[5];
        readonly Vector3[] panelHome = new Vector3[5];
        readonly Transform[] gliders = new Transform[3];
        readonly Transform[] windsocks = new Transform[4];
        readonly Transform[] shimmer = new Transform[5];
        readonly LineRenderer[] runwayLines = new LineRenderer[3];
        readonly Transform[] runwaySegments = new Transform[7];
        readonly Transform[] practiceFrames = new Transform[2];
        readonly List<Transform> wasteObstacles = new List<Transform>(3);

        // 청유리·유백색 장치 대신 야간 소금광산의 불투명 각인 타일·황동 클램프를 쓴다.
        readonly Color Salt = MgfLook.Hex("#4A2530");
        readonly Color Cobalt = MgfLook.Hex("#B74932");
        readonly Color Ceramic = MgfLook.Hex("#E8C98F");
        readonly Color Graphite = MgfLook.Hex("#211D2A");
        readonly Color Signal = MgfLook.Hex("#F0B94E");
        readonly Color Milk = MgfLook.Hex("#8F786B");

        Camera cam;
        Transform worldRoot, runwayRoot, hangarRoot, mergedBattery, dragTrail, serviceCart, serviceBot;
        Material saltMat, glassMat, ceramicMat, graphiteMat, signalMat, wasteMat, glowMat;
        PanelProblem current;
        Mode mode;
        int runSerial, selected = -1, targetPanel = -1;
        bool currentAttempted, isWide;
        int layoutBand = -1;
        float runLeft = 100f, feedbackT, idleGuide, titleT, launchT = -1f;
        Vector3 dragOffset;
        Vector2 dragStartScreen;

        RectTransform rootRt, titleRt, hudRt, endRt, guideHand, guidePath, rippleRt;
        CanvasGroup titleG, hudG, endG, toastG, revealG;
        TextMeshProUGUI promptUi, goalUi, scoreUi, fuseUi, progressUi, timerUi, toastUi, revealUi;
        TextMeshProUGUI endTitleUi, endScoreUi, endStatsUi;
        Image runwayPulse, rippleImage;
        Sprite cardSprite, ringSprite;
        float toastT, revealT, displayScore;
        int lastHudScore = -1, lastHudLives = -1, lastHudSolved = -1, lastHudTimer = -1;

        void Awake()
        {
            MgfLook.Quality(36f);
            MgfLook.Sky(MgfLook.Hex("#241A35"), MgfLook.Hex("#7A3C46"), MgfLook.Hex("#321F2B"), .88f);
            MgfLook.Sun(new Vector3(52, -34, -18), MgfLook.Hex("#FFD58A"), 1.18f, .78f);
            cam = MgfLook.Camera(new Vector3(0, 13f, -17f), new Vector3(0, 0, 1.2f), 36f);
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;

            catalog.AddRange(JegopRules.BuildCatalog());
            for (int i = 0; i < catalog.Count; i++) bank.Add(catalog[i].ToMgf());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("제곱활주로두판을합쳐빛길을열어라중학교2학년피타고라스정리청유리패널을다른패널위에포개시오두정사각형의넓이를합쳐목표대각선을밝히시오실수여유구간정답오답폐유리굳은경로합체완료전지를빛나는삼각홈으로옮기시오편대이륙활주로폐쇄다시가동넓이변길이빗변가장긴변직각삼각형나머지고르시오그림은길이비와다를수있습니다두판을포개시오잠긴패널입니다합체중입니다");
            sb.Append("각인타일황동클램프활주로조각소금광산정비격납고");
            for (int i = 0; i < catalog.Count; i++) { sb.Append(catalog[i].prompt); sb.Append(catalog[i].reveal); }
            MgfText.Prewarm(sb.ToString());
        }

        void BuildWorld()
        {
            worldRoot = new GameObject("SaltMineRailHangar").transform;
            saltMat = MgfLook.Lit(Salt, .16f);
            glassMat = MgfLook.Lit(Cobalt, .18f, .04f);
            ceramicMat = MgfLook.Lit(Ceramic, .22f);
            graphiteMat = MgfLook.Lit(Graphite, .18f, .16f);
            signalMat = MgfLook.Lit(Signal, .32f, .18f, MgfLook.Hex("#452600"));
            wasteMat = MgfLook.Lit(Milk, .08f);
            glowMat = MgfLook.Additive(new Color(1f, .58f, .16f, .86f), MgfLook.SoftDot);

            MgfLook.Block("MineFloor", new Vector3(0, -1.0f, 1.5f), new Vector3(30, 1.4f, 25), .25f, graphiteMat, worldRoot);
            MgfLook.Block("VerticalSaltFace", new Vector3(0, 2.15f, 10.4f), new Vector3(30, 6.4f, 2.2f), .30f, saltMat, worldRoot);
            for (int i = 0; i < 22; i++)
            {
                float x = -13f + (i % 11) * 2.6f;
                float z = -8f + (i / 11) * 15f;
                MgfLook.Block("RailTie" + i, new Vector3(x, -.27f, z), new Vector3(1.3f, .035f, .07f), .01f, signalMat, worldRoot);
            }
            for (int i = 0; i < 13; i++)
            {
                float x = -13f + i * 2.15f;
                float h = 1.0f + (i % 4) * .48f;
                var crystal = MgfLook.Block("SaltCrystal" + i, new Vector3(x, .4f + h * .5f, 8.8f + (i % 2) * .45f), new Vector3(.55f, h, .62f), .12f, ceramicMat, worldRoot);
                crystal.transform.localRotation = Quaternion.Euler(0, (i % 3 - 1) * 13f, (i % 2 == 0 ? -1 : 1) * 8f);
            }
            BuildHangar();
            BuildRunway();
            BuildPanels();
            BuildGliders();
            BuildWindsocks();
            BuildShimmer();
            BuildAirfieldLife();
        }

        void BuildHangar()
        {
            hangarRoot = new GameObject("CliffRailWorkshop").transform;
            hangarRoot.SetParent(worldRoot, false);
            MgfLook.Block("HangarBody", new Vector3(-4.8f, .35f, 5.9f), new Vector3(5.2f, 1.5f, 2.8f), .18f, saltMat, hangarRoot);
            var roof = MgfLook.Block("GantryRoof", new Vector3(-4.8f, 1.22f, 5.9f), new Vector3(5.7f, .22f, 3.15f), .10f, signalMat, hangarRoot);
            roof.transform.localRotation = Quaternion.Euler(0, 0, -3f);
            for (int i = 0; i < 3; i++)
                MgfLook.Block("LiftDoor" + i, new Vector3(-6.35f + i * 1.55f, .25f, 4.47f), new Vector3(1.2f, 1.15f, .10f), .04f, graphiteMat, hangarRoot);
        }

        void BuildRunway()
        {
            runwayRoot = new GameObject("TriangularSolarRunway").transform;
            runwayRoot.SetParent(worldRoot, false);
            MgfLook.Block("RunwayBed", new Vector3(4.1f, -.17f, 1.7f), new Vector3(7.4f, .22f, 8.7f), .12f, saltMat, runwayRoot);
            MgfLook.Block("PlateDock", new Vector3(2.25f, .02f, -1.55f), new Vector3(2.55f, .20f, 2.55f), .10f, signalMat, runwayRoot);
            for (int i = 0; i < 7; i++)
            {
                var seg = MgfLook.Block("LockedRailPlate" + i, new Vector3(4.25f, .02f, -1.8f + i * 1.05f), new Vector3(3.4f, .13f, .64f), .05f, i == 0 ? signalMat : graphiteMat, runwayRoot);
                seg.transform.localRotation = Quaternion.Euler(0, -19f, 0);
                runwaySegments[i] = seg.transform;
            }
            Vector3[] tri = { new Vector3(2.6f,.14f,-1.15f), new Vector3(6.55f,.14f,4.8f), new Vector3(1.7f,.14f,4.8f), new Vector3(2.6f,.14f,-1.15f) };
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("TriangleLight" + i); go.transform.SetParent(runwayRoot, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = MgfLook.Additive(new Color(1f,.58f,.12f,.88f)); lr.widthMultiplier = .08f;
                lr.positionCount = 2; lr.useWorldSpace = false; lr.SetPosition(0, tri[i]); lr.SetPosition(1, tri[i + 1]);
                lr.startColor = lr.endColor = new Color(.65f,.34f,.12f,.20f);
                runwayLines[i] = lr;
            }
        }

        void BuildPanels()
        {
            for (int i = 0; i < 5; i++)
            {
                var root = new GameObject("EngravedRunwayTile" + i);
                root.transform.SetParent(worldRoot, false);
                panelGo[i] = root;
                var slab = MgfLook.Block("StoneTile", Vector3.zero, new Vector3(2.05f, .30f, 2.05f), .13f, glassMat, root.transform);
                slab.GetComponent<Collider>().enabled = false;
                var hit = root.AddComponent<BoxCollider>(); hit.size = new Vector3(2.25f, .65f, 2.25f); hit.center = new Vector3(0,.15f,0);
                for (int g = -1; g <= 1; g++)
                {
                    MgfLook.Block("EngraveX" + g, new Vector3(g * .49f, .17f, 0), new Vector3(.045f, .024f, 1.84f), .008f, graphiteMat, root.transform).GetComponent<Collider>().enabled = false;
                    MgfLook.Block("EngraveZ" + g, new Vector3(0, .17f, g * .49f), new Vector3(1.84f, .024f, .045f), .008f, graphiteMat, root.transform).GetComponent<Collider>().enabled = false;
                }
                for (int c = 0; c < 4; c++)
                {
                    float sx = c < 2 ? -.91f : .91f, sz = c % 2 == 0 ? -.91f : .91f;
                    MgfLook.Block("Clamp" + c, new Vector3(sx,.23f,sz), new Vector3(.25f,.16f,.25f), .05f, signalMat, root.transform).GetComponent<Collider>().enabled=false;
                }
                // 격자와 수가 겹치지 않도록 불투명 계기판 위에 값을 올린다.
                var badge = MgfLook.Block("NumberBadge", new Vector3(0,.205f,-.02f), new Vector3(1.72f,.055f,.62f), .08f, graphiteMat, root.transform);
                badge.GetComponent<Collider>().enabled = false;
                panelText[i] = MgfText.World("", new Vector3(0,.245f,-.05f), 4.5f, Ceramic, root.transform);
                panelText[i].transform.localRotation = Quaternion.Euler(90, 0, 0);
                panelText[i].rectTransform.sizeDelta = new Vector2(1.66f, .54f);
                panelText[i].enableAutoSizing = true; panelText[i].fontSizeMin = 1.15f; panelText[i].fontSizeMax = 3.05f;
                panelText[i].fontStyle = FontStyles.Bold;
                panelText[i].outlineWidth = .10f; panelText[i].outlineColor = new Color(0,0,0,.82f);

                if (i < 2)
                {
                    var frame = new GameObject("PracticeFrame" + i).transform;
                    frame.SetParent(root.transform, false);
                    MgfLook.Block("North",new Vector3(0,.25f,.98f),new Vector3(2.18f,.055f,.075f),.02f,signalMat,frame).GetComponent<Collider>().enabled=false;
                    MgfLook.Block("South",new Vector3(0,.25f,-.98f),new Vector3(2.18f,.055f,.075f),.02f,signalMat,frame).GetComponent<Collider>().enabled=false;
                    MgfLook.Block("West",new Vector3(-.98f,.25f,0),new Vector3(.075f,.055f,2.18f),.02f,signalMat,frame).GetComponent<Collider>().enabled=false;
                    MgfLook.Block("East",new Vector3(.98f,.25f,0),new Vector3(.075f,.055f,2.18f),.02f,signalMat,frame).GetComponent<Collider>().enabled=false;
                    practiceFrames[i] = frame;
                }
            }
            mergedBattery = new GameObject("MergedBattery").transform;
            mergedBattery.SetParent(worldRoot, false);
            var core = MgfLook.Block("RunwayPlateCore", Vector3.zero, new Vector3(2.75f,.38f,2.75f), .16f, glassMat, mergedBattery);
            core.GetComponent<Collider>().enabled = false;
            mergedBattery.gameObject.AddComponent<BoxCollider>().size = new Vector3(3f,.7f,3f);
            for (int g = -2; g <= 2; g++)
            {
                MgfLook.Block("PlateGrooveX"+g,new Vector3(g*.48f,.21f,0),new Vector3(.04f,.025f,2.5f),.008f,graphiteMat,mergedBattery).GetComponent<Collider>().enabled=false;
                MgfLook.Block("PlateGrooveZ"+g,new Vector3(0,.21f,g*.48f),new Vector3(2.5f,.025f,.04f),.008f,graphiteMat,mergedBattery).GetComponent<Collider>().enabled=false;
            }
            mergedBattery.gameObject.SetActive(false);

            dragTrail = new GameObject("GlassTrail").transform; dragTrail.SetParent(worldRoot, false);
            var trail = dragTrail.gameObject.AddComponent<LineRenderer>(); trail.sharedMaterial = MgfLook.Additive(new Color(.35f,.8f,1f,.7f));
            trail.positionCount = 2; trail.widthMultiplier = .12f; trail.useWorldSpace = true; dragTrail.gameObject.SetActive(false);
        }

        void BuildGliders()
        {
            for (int i = 0; i < gliders.Length; i++)
            {
                var r = new GameObject("GraphiteGlider" + i).transform; r.SetParent(worldRoot, false);
                var fuselage=MgfLook.Block("Fuselage",Vector3.zero,new Vector3(.48f,.28f,1.85f),.20f,graphiteMat,r);
                Destroy(fuselage.GetComponent<Collider>());
                var wl=MgfLook.Block("WingL",new Vector3(-.7f,0,.03f),new Vector3(1.3f,.10f,.72f),.06f,graphiteMat,r); wl.transform.localRotation=Quaternion.Euler(0,-20,0);
                var wr=MgfLook.Block("WingR",new Vector3(.7f,0,.03f),new Vector3(1.3f,.10f,.72f),.06f,graphiteMat,r); wr.transform.localRotation=Quaternion.Euler(0,20,0);
                MgfLook.Block("Nose",new Vector3(0,.04f,-.82f),new Vector3(.28f,.20f,.48f),.10f,signalMat,r);
                // 정답 사건에서 활주로 조각을 실제 바퀴로 밟는 것이 보이도록 착륙 장치를 둔다.
                for(int w=0;w<3;w++)
                {
                    float x=w==0?0:(w==1?-.48f:.48f),z=w==0?-.55f:.48f;
                    var wheel=MgfLook.Block("Wheel"+w,new Vector3(x,-.20f,z),new Vector3(.22f,.22f,.16f),.08f,graphiteMat,r);
                    Destroy(wheel.GetComponent<Collider>());
                }
                gliders[i]=r;
            }
        }

        void BuildWindsocks()
        {
            for(int i=0;i<windsocks.Length;i++)
            {
                var root=new GameObject("Windsock"+i).transform; root.SetParent(worldRoot,false);
                root.localPosition=new Vector3(-8f+i*5.2f,.1f,7.1f+(i%2)*1.1f);
                var pole=MgfLook.Block("Pole",new Vector3(0,1.2f,0),new Vector3(.10f,2.4f,.10f),.025f,ceramicMat,root);
                Destroy(pole.GetComponent<Collider>());
                var sock=MgfLook.Block("Sock",new Vector3(.55f,2.25f,0),new Vector3(1.1f,.24f,.24f),.10f,signalMat,root); Destroy(sock.GetComponent<Collider>());
                windsocks[i]=sock.transform;
            }
        }

        void BuildShimmer()
        {
            for(int i=0;i<shimmer.Length;i++)
            {
                var q=MgfLook.Block("HeatShimmer"+i,new Vector3(-7f+i*3.5f,.05f,9f+(i%2)),new Vector3(2.4f,.025f,.22f),.01f,signalMat,worldRoot);
                Destroy(q.GetComponent<Collider>());
                shimmer[i]=q.transform;
            }
        }

        void BuildAirfieldLife()
        {
            // 빈 평면이 아니라 높낮이와 수평선이 읽히는 계단식 정비 기지로 만든다.
            MgfLook.Block("RearTerrace",new Vector3(0,-.05f,9.6f),new Vector3(30,.85f,4.2f),.18f,graphiteMat,worldRoot);
            MgfLook.Block("RearSaltCap",new Vector3(0,.42f,9.25f),new Vector3(29.2f,.18f,3.25f),.12f,ceramicMat,worldRoot);
            MgfLook.Block("LeftApron",new Vector3(-8.9f,-.18f,1.2f),new Vector3(7.0f,.55f,10.5f),.20f,saltMat,worldRoot);
            MgfLook.Block("LeftApronInset",new Vector3(-8.9f,.13f,1.2f),new Vector3(6.45f,.10f,9.9f),.10f,graphiteMat,worldRoot);
            for(int i=0;i<5;i++)
            {
                MgfLook.Block("GantryPost"+i,new Vector3(-11.2f+i*5.6f,1.35f,7.9f),new Vector3(.22f,2.7f,.22f),.04f,signalMat,worldRoot);
                if(i<4)MgfLook.Block("GantryBeam"+i,new Vector3(-8.4f+i*5.6f,2.62f,7.9f),new Vector3(5.5f,.20f,.22f),.04f,signalMat,worldRoot);
            }
            for(int i=0;i<6;i++)
                MgfLook.Block("Cargo"+i,new Vector3(8.5f+(i%2)*1.15f,.15f,6.8f+(i/2)*.9f),new Vector3(.85f,.72f,.72f),.09f,i%2==0?signalMat:ceramicMat,worldRoot);
            var serviceRail=MgfLook.Block("ForegroundServiceRail",new Vector3(0,-.12f,-7.0f),new Vector3(27f,.20f,.24f),.06f,graphiteMat,worldRoot);
            Destroy(serviceRail.GetComponent<Collider>());
            for(int i=0;i<9;i++)
            {
                var baseGo=MgfLook.Block("TaxiBeaconBase"+i,new Vector3(-12f+i*3f,.02f,-6.75f),new Vector3(.58f,.16f,.58f),.12f,graphiteMat,worldRoot);
                var lamp=MgfLook.Block("TaxiBeaconLamp"+i,new Vector3(-12f+i*3f,.27f,-6.75f),new Vector3(.24f,.34f,.24f),.10f,i%2==0?signalMat:ceramicMat,worldRoot);
                Destroy(baseGo.GetComponent<Collider>());Destroy(lamp.GetComponent<Collider>());
            }

            serviceCart = new GameObject("MovingMaintenanceCart").transform;
            serviceCart.SetParent(worldRoot,false);
            MgfLook.Block("CartBody",new Vector3(0,.38f,0),new Vector3(1.65f,.55f,.95f),.20f,signalMat,serviceCart);
            MgfLook.Block("CartCab",new Vector3(.42f,.78f,0),new Vector3(.58f,.48f,.80f),.16f,ceramicMat,serviceCart);
            for(int i=0;i<4;i++)
                MgfLook.Block("Wheel"+i,new Vector3(i<2?-.55f:.55f,.12f,i%2==0?-.48f:.48f),new Vector3(.32f,.32f,.18f),.14f,graphiteMat,serviceCart);

            // 활주로 전용 작은 정비 로봇. 대기·정답·오답에 몸짓으로 반응한다.
            serviceBot = new GameObject("RunwayDroidTori").transform;
            serviceBot.SetParent(worldRoot,false);
            MgfLook.Block("Body",new Vector3(0,.52f,0),new Vector3(.9f,.9f,.72f),.28f,signalMat,serviceBot);
            MgfLook.Block("Visor",new Vector3(0,.63f,-.38f),new Vector3(.62f,.20f,.06f),.05f,graphiteMat,serviceBot);
            MgfLook.Block("EyeL",new Vector3(-.17f,.64f,-.43f),new Vector3(.09f,.09f,.04f),.04f,signalMat,serviceBot);
            MgfLook.Block("EyeR",new Vector3(.17f,.64f,-.43f),new Vector3(.09f,.09f,.04f),.04f,signalMat,serviceBot);
            MgfLook.Block("FootL",new Vector3(-.25f,.08f,0),new Vector3(.30f,.20f,.50f),.09f,graphiteMat,serviceBot);
            MgfLook.Block("FootR",new Vector3(.25f,.08f,0),new Vector3(.30f,.20f,.50f),.09f,graphiteMat,serviceBot);
        }

        Material CobaltMaterial() => glassMat;

        void BuildUi()
        {
            cardSprite = MakeSprite(96, 16, false); ringSprite = MakeSprite(96, 10, true);
            rootRt = Rect("AirfieldUI", MgfText.Canvas.transform, new Vector2(.5f,.5f), Vector2.zero, Vector2.zero);
            rootRt.anchorMin=Vector2.zero; rootRt.anchorMax=Vector2.one; rootRt.sizeDelta=Vector2.zero;
            BuildTitleUi(); BuildHudUi(); BuildEndUi(); BuildOverlayUi();
        }

        void BuildTitleUi()
        {
            titleRt=Rect("Title",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero); titleRt.anchorMin=Vector2.zero; titleRt.anchorMax=Vector2.one; titleRt.sizeDelta=Vector2.zero;
            titleG=titleRt.gameObject.AddComponent<CanvasGroup>();
            // 세계 속 격납고 스텐실처럼 보이는 얇은 간판. 큰 둥근 카드/CTA 템플릿을 쓰지 않는다.
            var sign=Img(titleRt,"HangingGantrySign",new Vector2(.5f,.865f),Vector2.zero,new Vector2(370,108),new Color(.12f,.08f,.16f,.94f));
            var logo=Txt(sign.transform,"제곱 활주로",new Vector2(.5f,.64f),Vector2.zero,38,Signal,350); logo.fontStyle=FontStyles.Bold; logo.characterSpacing=4;
            var sub=Txt(sign.transform,"두 판을 합쳐 빛길을 열어라  ·  중2 피타고라스 정리",new Vector2(.5f,.22f),Vector2.zero,12,Ceramic,354); sub.fontStyle=FontStyles.Bold;
            var call=Img(titleRt,"WorldCallout",new Vector2(.5f,.10f),Vector2.zero,new Vector2(340,48),new Color(.14f,.09f,.17f,.94f));
            Txt(call.transform,"각인된 9·16 타일을 직접 포개 시동",new Vector2(.5f,.5f),Vector2.zero,14,Ceramic,320).fontStyle=FontStyles.Bold;
        }

        void BuildHudUi()
        {
            hudRt=Rect("Hud",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero); hudRt.anchorMin=Vector2.zero; hudRt.anchorMax=Vector2.one; hudRt.sizeDelta=Vector2.zero;
            hudG=hudRt.gameObject.AddComponent<CanvasGroup>();
            var rail=Img(hudRt,"StatusRail",new Vector2(.5f,.972f),Vector2.zero,new Vector2(374,42),new Color(.08f,.15f,.19f,.94f));
            scoreUi=Txt(rail.transform,"0000",new Vector2(.13f,.5f),Vector2.zero,16,Ceramic,86);
            fuseUi=Txt(rail.transform,"실수 여유 3",new Vector2(.47f,.5f),Vector2.zero,13,Signal,136);
            progressUi=Txt(rail.transform,"구간 0/7",new Vector2(.76f,.5f),Vector2.zero,14,Ceramic,92);
            timerUi=Txt(hudRt,"100",new Vector2(.84f,.90f),Vector2.zero,18,Ceramic,62); timerUi.outlineWidth=.15f; timerUi.outlineColor=Graphite;
            var goal=Img(hudRt,"Goal",new Vector2(.5f,.915f),Vector2.zero,new Vector2(370,34),new Color(.96f,.75f,.30f,.97f));
            goalUi=Txt(goal.transform,"두 정사각형의 넓이를 합쳐 목표 대각선을 밝히시오.",new Vector2(.5f,.5f),Vector2.zero,12,Graphite,352);
            var card=Img(hudRt,"Mission",new Vector2(.5f,.835f),Vector2.zero,new Vector2(370,78),new Color(.06f,.13f,.17f,.88f));
            promptUi=Txt(card.transform,"",new Vector2(.5f,.61f),Vector2.zero,15,Ceramic,352); promptUi.fontStyle=FontStyles.Bold; promptUi.enableAutoSizing=true; promptUi.fontSizeMin=11; promptUi.fontSizeMax=15;
            Txt(card.transform,"그림은 길이의 비율과 다를 수 있습니다.",new Vector2(.5f,.18f),Vector2.zero,10,Signal,350);
            runwayPulse=Img(hudRt,"DockPulse",new Vector2(.76f,.28f),Vector2.zero,new Vector2(96,96),new Color(.25f,.85f,1f,.18f),ringSprite);
            runwayPulse.enabled=false;
            guidePath=Img(hudRt,"GuidePath",new Vector2(.5f,.5f),Vector2.zero,new Vector2(210,8),new Color(.96f,.76f,.32f,.82f)).rectTransform;
            guideHand=Img(hudRt,"GuideHand",new Vector2(.5f,.5f),Vector2.zero,new Vector2(54,54),Signal,ringSprite).rectTransform;
        }

        void BuildEndUi()
        {
            endRt=Rect("End",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero); endRt.anchorMin=Vector2.zero; endRt.anchorMax=Vector2.one; endRt.sizeDelta=Vector2.zero;
            endG=endRt.gameObject.AddComponent<CanvasGroup>();
            var plate=Img(endRt,"HangarDoor",new Vector2(.5f,.52f),Vector2.zero,new Vector2(360,470),new Color(.10f,.19f,.23f,.96f),cardSprite);
            endTitleUi=Txt(plate.transform,"편대 이륙",new Vector2(.5f,.78f),Vector2.zero,38,Signal,330); endTitleUi.fontStyle=FontStyles.Bold;
            endScoreUi=Txt(plate.transform,"0",new Vector2(.5f,.57f),Vector2.zero,58,Ceramic,320); endScoreUi.fontStyle=FontStyles.Bold;
            endStatsUi=Txt(plate.transform,"",new Vector2(.5f,.40f),Vector2.zero,16,Ceramic,320);
            var restart=Img(plate.transform,"RestartPanel",new Vector2(.5f,.17f),Vector2.zero,new Vector2(300,88),Cobalt,cardSprite);
            Txt(restart.transform,"각인 타일을 눌러 다시 가동",new Vector2(.5f,.5f),Vector2.zero,17,Ceramic,270).fontStyle=FontStyles.Bold;
        }

        void BuildOverlayUi()
        {
            var toast=Img(rootRt,"Toast",new Vector2(.5f,.53f),Vector2.zero,new Vector2(350,58),new Color(.10f,.18f,.22f,.95f));
            toastG=toast.gameObject.AddComponent<CanvasGroup>(); toastUi=Txt(toast.transform,"",new Vector2(.5f,.5f),Vector2.zero,15,Ceramic,324);
            var reveal=Img(rootRt,"MathReveal",new Vector2(.5f,.60f),Vector2.zero,new Vector2(370,96),new Color(.96f,.77f,.32f,.98f));
            revealG=reveal.gameObject.AddComponent<CanvasGroup>(); revealUi=Txt(reveal.transform,"",new Vector2(.5f,.5f),Vector2.zero,21,Graphite,330); revealUi.fontStyle=FontStyles.Bold;
            rippleRt=Rect("Ripple",rootRt,new Vector2(.5f,.5f),Vector2.zero,new Vector2(70,70)); rippleImage=rippleRt.gameObject.AddComponent<Image>(); rippleImage.sprite=ringSprite; rippleImage.color=Signal; rippleImage.raycastTarget=false;
        }

        void ShowTitle()
        {
            ClearWaste();
            mode=Mode.Title; st.phase="title"; st.score=0; st.lives=JegopRules.StartLives; st.level=1; st.solved=0; st.combo=0;
            st.firstAttemptTotal=0; st.firstAttemptCorrect=0; st.pointerVersion=0; st.wasteBlocks=0; st.misconceptionId=""; st.onboarding=false; st.batteryReady=false; st.practiceStep="";
            runLeft=100f; selected=-1; current=JegopRules.Practice(); st.problemId=current.id; titleT=0; displayScore=0;
            ApplyProblem(); SetGroups(); MgfBridge.NotifyChanged();
        }

        void BeginRun(bool onboarding)
        {
            ClearWaste();
            runSerial++; mode=onboarding?Mode.Practice:Mode.Playing; st.phase="playing"; st.score=0; st.lives=JegopRules.StartLives; st.level=1; st.solved=0; st.combo=0;
            st.firstAttemptTotal=0; st.firstAttemptCorrect=0; st.wasteBlocks=0; st.misconceptionId=""; st.onboarding=onboarding; st.batteryReady=false; st.practiceStep=onboarding?"merge-panels":"";
            runLeft=100f; currentAttempted=false; selected=-1; targetPanel=-1; feedbackT=0; idleGuide=0; displayScore=0;
            current=onboarding?JegopRules.Practice():JegopRules.PickSession(catalog,0,runSerial); st.problemId=current.id;
            ApplyProblem(); SetGroups(); if(onboarding) StartGuide(); MgfSfx.Play("whoosh",.36f); MgfBridge.NotifyChanged();
        }

        void LoadNext()
        {
            mode=Mode.Playing; current=JegopRules.PickSession(catalog,st.solved,runSerial+st.solved*29); st.problemId=current.id; st.level=current.band;
            st.batteryReady=false; st.misconceptionId=""; currentAttempted=false; selected=-1; targetPanel=-1; idleGuide=0;
            ApplyProblem(); SetGroups(); MgfBridge.NotifyChanged();
        }

        void ApplyProblem()
        {
            LayoutWorld(true);
            for(int i=0;i<5;i++)
            {
                panelGo[i].SetActive(true); panelGo[i].transform.localPosition=panelHome[i]; panelGo[i].transform.localRotation=Quaternion.identity; panelGo[i].transform.localScale=Vector3.one;
                panelGo[i].GetComponentInChildren<Renderer>().sharedMaterial=glassMat; panelText[i].text=current.PanelLabel(i);
                panelText[i].color=current.panels[i].locked?new Color(.78f,.83f,.82f,1):Ceramic;
            }
            for(int i=0;i<practiceFrames.Length;i++) if(practiceFrames[i])
                practiceFrames[i].gameObject.SetActive(mode==Mode.Practice||mode==Mode.Title);
            mergedBattery.gameObject.SetActive(false); dragTrail.gameObject.SetActive(false); SetRunwayLight(.20f);
            for(int i=0;i<runwaySegments.Length;i++) if(runwaySegments[i])
                runwaySegments[i].GetComponentInChildren<Renderer>().sharedMaterial=i<st.solved?signalMat:graphiteMat;
            if(promptUi) promptUi.text=current.prompt;
            if(goalUi) goalUi.text=mode==Mode.Practice?"두 정사각형의 넓이를 합쳐 목표 대각선을 밝히시오.":"두 패널을 포개고, 완성 전지를 삼각 홈으로 옮기시오.";
            RefreshHud();
        }

        void Update()
        {
            float dt=Mathf.Min(.05f,Time.deltaTime);
            HandleInput();
            if(mode==Mode.Playing)
            {
                runLeft-=dt; idleGuide+=dt;
                if(runLeft<=0) End(false,"제한 시간이 끝났습니다.");
            }
            else if(mode==Mode.Practice)
            {
                idleGuide+=dt; if(idleGuide>8f){idleGuide=0;StartGuide(true);}
            }
            else if(mode==Mode.MergeFeedback || mode==Mode.WrongFeedback)
            {
                feedbackT+=dt;
                if(mode==Mode.MergeFeedback) UpdateMergeFeedback(); else if(feedbackT>1.05f) RecoverAfterWrong();
            }
            AnimateWorld(dt); UpdateUi(dt); LayoutWorld();
        }

        void HandleInput()
        {
            if(MgfPointer.Down)
            {
                st.pointerVersion++; SpawnRipple(MgfPointer.Position); MgfBridge.NotifyChanged();
                if(mode==Mode.End){BeginRun(true);return;}
                if(mode==Mode.MergeFeedback||mode==Mode.PracticeReveal||mode==Mode.WrongFeedback){Refuse("합체 결과를 확인하고 있습니다.");return;}
                RaycastHit hit;
                if(MgfPointer.DownHit(cam,out hit))
                {
                    int pi=PanelIndex(hit.collider.gameObject);
                    if(mode==Mode.Title)
                    {
                        BeginRun(true);
                        if(pi==0||pi==1)StartDrag(pi);
                        else Refuse("노란 테두리의 9 cm² 패널을 16 cm² 위로 옮기시오.");
                        return;
                    }
                    if(mode==Mode.Battery && IsBatteryHit(hit.collider.gameObject)){StartBatteryDrag();return;}
                    if((mode==Mode.Practice||mode==Mode.Playing)&&pi>=0)
                    {
                        if(current.panels[pi].locked){MgfFx.Punch(panelGo[pi].transform,.10f,.20f);Refuse("연습에서는 빛나는 두 패널만 움직일 수 있습니다.");MgfSfx.Play("wrong",.18f);}
                        else StartDrag(pi);
                        return;
                    }
                }
                if(mode==Mode.Title)
                {
                    BeginRun(true);Refuse("노란 테두리의 9 cm² 패널을 16 cm² 위로 옮기시오.");return;
                }
                if(mode==Mode.Practice)
                {
                    Refuse("노란 테두리 타일을 직접 잡아 다른 노란 타일 위에 포개시오.");return;
                }
                Refuse(mode==Mode.Battery?"완성 활주로 조각을 황동 삼각 홈으로 옮기시오.":"각인 타일을 잡아 다른 타일 위에 포개시오.");
                MgfSfx.Play("tap",.18f);
            }
            if(selected>=0 && MgfPointer.Held) DragSelected();
            if(selected>=0 && MgfPointer.Up) EndPanelDrag();
            if(selected==-2 && MgfPointer.Held) DragBattery();
            if(selected==-2 && MgfPointer.Up) EndBatteryDrag();
        }

        int PanelIndex(GameObject hit)
        {
            for(int i=0;i<5;i++) if(hit==panelGo[i]||hit.transform.IsChildOf(panelGo[i].transform)) return i;
            return -1;
        }
        bool IsBatteryHit(GameObject hit)=>hit==mergedBattery.gameObject||hit.transform.IsChildOf(mergedBattery);

        void StartDrag(int i)
        {
            selected=i; targetPanel=-1; idleGuide=0;dragStartScreen=MgfPointer.Position;panelGo[i].transform.localScale=new Vector3(1.08f,.84f,1.08f);
            if(MgfPointer.OnPlane(cam,.55f,out var p)) dragOffset=panelGo[i].transform.position-p; else dragOffset=Vector3.zero;
            dragTrail.gameObject.SetActive(true); MgfSfx.Play("tap",.26f);
        }
        void DragSelected()
        {
            if(!MgfPointer.OnPlane(cam,.55f,out var p))return;
            var pos=p+dragOffset;
            if(BlockedByWaste(pos))
            {
                if(toastT<=.05f){toastUi.text="굳은 폐유리를 피해 다른 경로로 옮기시오.";ShowToast(.75f);MgfSfx.Play("tap",.12f);}
                return;
            }
            panelGo[selected].transform.position=Vector3.Lerp(panelGo[selected].transform.position,pos,.48f);
            panelGo[selected].transform.localScale=Vector3.Lerp(panelGo[selected].transform.localScale,new Vector3(1.04f,.92f,1.04f),.25f);
            var lr=dragTrail.GetComponent<LineRenderer>();lr.SetPosition(0,panelHome[selected]+Vector3.up*.35f);lr.SetPosition(1,panelGo[selected].transform.position+Vector3.up*.1f);
            targetPanel=-1; float best=.68f;
            for(int i=0;i<5;i++)if(i!=selected&&panelGo[i].activeSelf){float d=Vector3.Distance(panelGo[selected].transform.position,panelGo[i].transform.position);if(d<best){best=d;targetPanel=i;}}
        }
        void EndPanelDrag()
        {
            int a=selected,b=targetPanel; selected=-1; dragTrail.gameObject.SetActive(false);
            if(b<0){panelGo[a].transform.localPosition=panelHome[a];panelGo[a].transform.localScale=Vector3.one;Refuse("두 판이 70% 이상 겹치도록 포개시오.");return;}
            panelGo[a].transform.localPosition=panelHome[a];panelGo[a].transform.localScale=Vector3.one;
            SubmitPair(a,b);
        }

        void SubmitPair(int a,int b)
        {
            if(mode!=Mode.Practice&&mode!=Mode.Playing)return;
            bool ok=current.IsCorrectPair(a,b);
            if(mode==Mode.Practice && !ok){Refuse("노란 테두리 두 타일을 다시 포개시오.");StartGuide(true);MgfFx.Shake(cam,.05f,.18f);MgfSfx.Play("wrong",.22f);return;}
            if(!currentAttempted)
            {
                currentAttempted=true;
                if(mode==Mode.Playing){st.firstAttemptTotal++;if(ok)st.firstAttemptCorrect++;}
            }
            if(ok)
            {
                mode=Mode.MergeFeedback; feedbackT=0; st.batteryReady=true; st.misconceptionId="";
                panelGo[a].transform.localPosition=new Vector3(-.72f,.7f,0);panelGo[b].transform.localPosition=new Vector3(.72f,.7f,0);
                for(int i=0;i<5;i++)if(i!=a&&i!=b)panelGo[i].SetActive(false);
                targetPanel=a*10+b; MgfSfx.Play("correct",.58f); SetRunwayLight(.72f); MgfBridge.NotifyChanged();
            }
            else
            {
                st.lives--;st.combo=0;
                st.misconceptionId=Misconception(a,b);
                mode=Mode.WrongFeedback;feedbackT=0;targetPanel=a*10+b;
                panelGo[a].GetComponentInChildren<Renderer>().sharedMaterial=wasteMat;panelGo[b].GetComponentInChildren<Renderer>().sharedMaterial=wasteMat;
                CreateWasteObstacle(panelText[a].text+" + "+panelText[b].text);
                panelGo[a].transform.localPosition=Vector3.Lerp(panelHome[a],WastePosition(wasteObstacles.Count-1),.75f);
                panelGo[b].transform.localPosition=panelGo[a].transform.localPosition+new Vector3(.35f,.18f,.25f);
                revealUi.text=current.WrongReveal(a,b);ShowReveal(1.0f,Milk);
                MgfFx.Shake(cam,.12f,.28f);MgfSfx.Play("wrong",.50f);RefreshHud();MgfBridge.NotifyChanged();
                // 종료 조건은 HUD에 보이는 실수 여유 하나뿐이다. 첫 시도 기록은 보너스 통계이며 숨은 사망 조건이 아니다.
                if(st.lives<=0) { feedbackT=-99f; End(false,"실수 여유를 모두 사용했습니다."); }
            }
        }

        string Misconception(int a,int b)
        {
            string x=current.panels[a].misconceptionId,y=current.panels[b].misconceptionId;
            if(!string.IsNullOrEmpty(x))return x;if(!string.IsNullOrEmpty(y))return y;return "pair-does-not-sum-to-c-squared";
        }

        void UpdateMergeFeedback()
        {
            int a=targetPanel/10,b=targetPanel%10;
            float k=Mathf.Clamp01(feedbackT/.72f);float lockEase=k*k*(3-2*k);
            // 두 등적 타일이 회전 마술 대신 레일을 따라 실제 한 활주로 조각으로 잠긴다.
            panelGo[a].transform.localPosition=Vector3.Lerp(panelGo[a].transform.localPosition,new Vector3(-.52f,.78f,0),.14f);
            panelGo[b].transform.localPosition=Vector3.Lerp(panelGo[b].transform.localPosition,new Vector3(.52f,.78f,0),.14f);
            panelGo[a].transform.localRotation=Quaternion.Euler(0,0,-3f*(1-lockEase));panelGo[b].transform.localRotation=Quaternion.Euler(0,0,3f*(1-lockEase));
            if(feedbackT>.82f)
            {
                panelGo[a].SetActive(false);panelGo[b].SetActive(false);mergedBattery.gameObject.SetActive(true);mergedBattery.localPosition=new Vector3(0,.72f,0);mergedBattery.localScale=Vector3.one*(.7f+.3f*Mathf.Min(1,(feedbackT-.82f)/.25f));
            }
            if(feedbackT>1.02f)
            {
                mode=Mode.Battery;selected=-1;goalUi.text="완성 활주로 조각을 황동 삼각 홈으로 옮기시오.";runwayPulse.enabled=true;
                if(st.onboarding){st.practiceStep="dock-plate";StartDockGuide();}
                revealUi.text=current.reveal;ShowReveal(1.2f,Signal);MgfFx.Glow(mergedBattery.position+Vector3.up*.5f,new Color(1f,.58f,.16f),16,.45f);MgfFx.Punch(mergedBattery,.16f,.32f);MgfBridge.NotifyChanged();
            }
        }

        void StartBatteryDrag(){selected=-2;if(MgfPointer.OnPlane(cam,.6f,out var p))dragOffset=mergedBattery.position-p;MgfSfx.Play("tap",.28f);}
        void DragBattery(){if(MgfPointer.OnPlane(cam,.6f,out var p)){mergedBattery.position=Vector3.Lerp(mergedBattery.position,p+dragOffset,.5f);mergedBattery.localScale=Vector3.Lerp(mergedBattery.localScale,new Vector3(1.08f,.88f,1.08f),.25f);}}
        void EndBatteryDrag()
        {
            selected=-1;Vector3 dock=DockWorld();float d=Vector3.Distance(mergedBattery.position,dock);
            if(d<2.25f){mergedBattery.position=dock;mergedBattery.localScale=Vector3.one;CompleteDock();}
            else{mergedBattery.localPosition=new Vector3(0,.72f,0);mergedBattery.localScale=Vector3.one;Refuse("활주로 조각을 오른쪽 황동 삼각 홈 안에 놓으시오.");if(st.onboarding)StartDockGuide();}
        }

        void CompleteDock()
        {
            bool practice=st.onboarding&&mode==Mode.Battery;
            runwayPulse.enabled=false;SetRunwayLight(1f);launchT=0;
            if(practice)
            {
                st.practiceStep="reveal";st.batteryReady=false;revealUi.text="9 + 16 = 25  ·  두 넓이가 활주로 한 조각이 됩니다.";ShowReveal(1.2f,Signal);
                guideHand.gameObject.SetActive(false);guidePath.gameObject.SetActive(false);mode=Mode.PracticeReveal;
                Invoke(nameof(FinishPracticeReveal),1.25f);MgfBridge.NotifyChanged();
            }
            else
            {
                if(st.solved<runwaySegments.Length&&runwaySegments[st.solved])
                {
                    runwaySegments[st.solved].GetComponentInChildren<Renderer>().sharedMaterial=signalMat;
                    MgfFx.Punch(runwaySegments[st.solved],.13f,.34f);
                }
                st.score+=120+st.combo*25+current.band*20;st.combo++;st.solved++;st.level=Math.Min(3,1+st.solved/2);st.batteryReady=false;
                MgfSfx.Play(st.solved>=JegopRules.Goal?"win":"whoosh",.58f);RefreshHud();MgfBridge.NotifyChanged();
                if(st.solved>=JegopRules.Goal)
                {
                    End(true,"첫 합체 "+st.firstAttemptCorrect+"/7 · 남은 여유 "+st.lives);
                }
                else Invoke(nameof(LoadNext),1.0f);
            }
        }
        void FinishPracticeReveal()
        {
            if(mode!=Mode.PracticeReveal)return;
            st.onboarding=false;st.practiceStep="";mode=Mode.Playing;current=JegopRules.PickSession(catalog,0,runSerial);st.problemId=current.id;currentAttempted=false;
            ApplyProblem();SetGroups();MgfBridge.NotifyChanged();
        }

        void RecoverAfterWrong()
        {
            if(mode!=Mode.WrongFeedback)return;
            mode=Mode.Playing;ApplyProblem();SetGroups();toastUi.text="두 변의 제곱의 합과 목표 제곱을 비교하시오.";ShowToast(1.15f);MgfBridge.NotifyChanged();
        }

        void End(bool win,string reason)
        {
            if(mode==Mode.End)return;mode=Mode.End;st.phase=win?"clear":"gameover";selected=-1;st.batteryReady=false;dragTrail.gameObject.SetActive(false);
            endTitleUi.text=win?"편대 이륙":"활주로 폐쇄";endScoreUi.text=st.score.ToString("0000");endStatsUi.text="완성 구간 "+st.solved+"/7\n첫 합체 "+st.firstAttemptCorrect+"/"+Math.Max(1,st.firstAttemptTotal)+"\n"+reason;
            SetGroups();MgfSfx.Play(win?"win":"lose",.58f);MgfBridge.NotifyChanged();
        }

        void AnimateWorld(float dt)
        {
            titleT+=dt;
            for(int i=0;i<windsocks.Length;i++)if(windsocks[i])windsocks[i].localRotation=Quaternion.Euler(0,Mathf.Sin(titleT*1.7f+i)*9f,Mathf.Sin(titleT*2.2f+i)*5f);
            for(int i=0;i<shimmer.Length;i++)if(shimmer[i]){float s=.85f+Mathf.Sin(titleT*1.2f+i)*.12f;shimmer[i].localScale=new Vector3(2.2f*s,.72f,1);}
            for(int i=0;i<gliders.Length;i++)if(gliders[i])
            {
                if(launchT>=0&&i==st.solved%3)
                {
                    float k=Mathf.Clamp01(launchT/1.2f);
                    if(k<.38f)
                    {
                        float roll=k/.38f;gliders[i].localPosition=Vector3.Lerp(GliderHome(i),runwayRoot.TransformPoint(new Vector3(4.4f,.48f,2.2f)),roll);
                        gliders[i].localRotation=Quaternion.Euler(0,-18f,0);
                    }
                    else
                    {
                        float fly=(k-.38f)/.62f;gliders[i].localPosition=Vector3.Lerp(runwayRoot.TransformPoint(new Vector3(4.4f,.48f,2.2f)),new Vector3(10f,5f,10f),fly*fly);
                        gliders[i].localRotation=Quaternion.Euler(-8,25,18*fly);
                    }
                }
                else gliders[i].localPosition=GliderHome(i)+Vector3.up*(Mathf.Sin(titleT*1.8f+i)*.06f);
            }
            if(launchT>=0){launchT+=dt;if(launchT>1.25f)launchT=-1;}
            float pulse=.22f+.18f*(.5f+.5f*Mathf.Sin(titleT*3f));if(mode==Mode.Battery)SetRunwayLight(.65f+pulse);
            if(serviceCart)
            {
                float span=layoutBand==2?24f:layoutBand==1?18f:10f;
                serviceCart.localPosition=new Vector3(-span*.5f+Mathf.Repeat(titleT*1.35f,span),.02f,layoutBand==0?8.7f:-6.2f);
            }
            if(serviceBot)
            {
                var p=serviceBot.localPosition;p.y=.12f+Mathf.Sin(titleT*2.4f)*.05f;serviceBot.localPosition=p;
                float reaction=mode==Mode.WrongFeedback?14f:(mode==Mode.MergeFeedback||mode==Mode.Battery)?-10f:Mathf.Sin(titleT*1.7f)*3f;
                serviceBot.localRotation=Quaternion.Euler(0,Mathf.Sin(titleT*.8f)*8f,reaction);
            }
        }

        Vector3 GliderHome(int i)
        {
            if(layoutBand==2)return new Vector3(5.7f+i*1.55f,.52f,3.6f+i*.58f);
            if(layoutBand==1)return new Vector3(3.7f+i*1.45f,.52f,3.6f+i*.58f);
            return new Vector3(-2.4f+i*2.35f,.52f,6.1f+i*.35f);
        }
        Vector3 DockWorld()=>runwayRoot.TransformPoint(new Vector3(2.25f,.55f,-1.55f));

        void LayoutWorld(bool force=false)
        {
            float aspect=(float)Screen.width/Mathf.Max(1,Screen.height);
            int wantedBand=aspect>=1.72f?2:aspect>=.92f?1:0;
            if(!force&&wantedBand==layoutBand)return;
            layoutBand=wantedBand;isWide=layoutBand>0;
            if(layoutBand==2)
            {
                cam.transform.position=new Vector3(0,12.8f,-17.2f);cam.transform.LookAt(new Vector3(0,0,-.2f));cam.orthographicSize=6.55f;
                Vector3[] homes={new Vector3(-9.1f,.55f,-3.3f),new Vector3(-6.35f,.55f,-3.3f),new Vector3(-9.1f,.55f,-.55f),new Vector3(-6.35f,.55f,-.55f),new Vector3(-7.72f,.55f,2.2f)};
                for(int i=0;i<5;i++)panelHome[i]=homes[i];runwayRoot.localPosition=new Vector3(3.1f,0,-.2f);hangarRoot.localPosition=new Vector3(-3.0f,0,-.1f);
                if(serviceBot)serviceBot.localPosition=new Vector3(-2.2f,.12f,2.1f);
            }
            else if(layoutBand==1)
            {
                cam.transform.position=new Vector3(0,12.8f,-17.4f);cam.transform.LookAt(new Vector3(0,0,-.1f));cam.orthographicSize=6.65f;
                Vector3[] homes={new Vector3(-7.45f,.55f,-3.1f),new Vector3(-4.75f,.55f,-3.1f),new Vector3(-7.45f,.55f,-.35f),new Vector3(-4.75f,.55f,-.35f),new Vector3(-6.1f,.55f,2.35f)};
                for(int i=0;i<5;i++)panelHome[i]=homes[i];runwayRoot.localPosition=new Vector3(1.45f,0,-.1f);hangarRoot.localPosition=new Vector3(-1.35f,0,0);
                if(serviceBot)serviceBot.localPosition=new Vector3(-.7f,.12f,2.2f);
            }
            else
            {
                cam.transform.position=new Vector3(0,16.8f,-19.3f);cam.transform.LookAt(new Vector3(0,0,1.2f));cam.orthographicSize=11.2f;
                Vector3[] homes={new Vector3(-2.45f,.55f,-4.25f),new Vector3(0,.55f,-4.25f),new Vector3(2.45f,.55f,-4.25f),new Vector3(-1.25f,.55f,-1.65f),new Vector3(1.25f,.55f,-1.65f)};
                for(int i=0;i<5;i++)panelHome[i]=homes[i];runwayRoot.localPosition=new Vector3(-2.25f,0,6.55f);hangarRoot.localPosition=new Vector3(4.25f,0,2.35f);
                if(serviceBot)serviceBot.localPosition=new Vector3(-3.5f,.12f,1.2f);
            }
            for(int i=0;i<5;i++)if(panelGo[i]&&selected!=i)panelGo[i].transform.localPosition=panelHome[i];
            for(int i=0;i<gliders.Length;i++)if(gliders[i])gliders[i].localPosition=GliderHome(i);
            for(int i=0;i<wasteObstacles.Count;i++)if(wasteObstacles[i])wasteObstacles[i].localPosition=WastePosition(i);
        }

        Vector3 WastePosition(int index)
        {
            if(layoutBand==2)return new Vector3(-3.2f+index*1.25f,.23f,-1.3f+index*.72f);
            if(layoutBand==1)return new Vector3(-2.1f+index*1.10f,.23f,-1.2f+index*.72f);
            return new Vector3(-1.35f+index*1.35f,.23f,1.1f+index*.95f);
        }

        void CreateWasteObstacle(string label)
        {
            var root=new GameObject("PersistentWaste"+wasteObstacles.Count).transform;
            root.SetParent(worldRoot,false);root.localPosition=WastePosition(wasteObstacles.Count);
            var lower=MgfLook.Block("MilkGlassA",Vector3.zero,new Vector3(2.15f,.36f,1.35f),.16f,wasteMat,root);
            var upper=MgfLook.Block("MilkGlassB",new Vector3(.28f,.28f,.10f),new Vector3(1.75f,.28f,1.10f),.13f,wasteMat,root);
            upper.transform.localRotation=Quaternion.Euler(0,18f,-4f);
            var tag=MgfText.World("폐유리",new Vector3(0,.43f,-.05f),2.3f,Graphite,root);
            tag.transform.localRotation=Quaternion.Euler(90,0,0);tag.fontStyle=FontStyles.Bold;
            wasteObstacles.Add(root);st.wasteBlocks=wasteObstacles.Count;
        }

        void ClearWaste()
        {
            for(int i=0;i<wasteObstacles.Count;i++)if(wasteObstacles[i])Destroy(wasteObstacles[i].gameObject);
            wasteObstacles.Clear();st.wasteBlocks=0;
        }

        bool BlockedByWaste(Vector3 world)
        {
            for(int i=0;i<wasteObstacles.Count;i++)if(wasteObstacles[i])
            {
                Vector3 d=world-wasteObstacles[i].position;
                if(d.x*d.x+d.z*d.z<2.15f)return true;
            }
            return false;
        }

        void SetRunwayLight(float a){for(int i=0;i<runwayLines.Length;i++)if(runwayLines[i])runwayLines[i].startColor=runwayLines[i].endColor=new Color(1f,.56f,.12f,Mathf.Clamp01(a));}

        void StartGuide(bool big=false)
        {
            guideHand.gameObject.SetActive(true);guidePath.gameObject.SetActive(true);guideHand.sizeDelta=Vector2.one*(big?74:54);goalUi.text="9 cm² 패널을 16 cm² 패널 위에 직접 포개시오.";
        }

        void StartDockGuide()
        {
            guideHand.gameObject.SetActive(true);guidePath.gameObject.SetActive(true);guideHand.sizeDelta=Vector2.one*62;
            goalUi.text="완성 활주로 조각을 황동 삼각 홈으로 직접 옮기시오.";
        }

        void UpdatePracticeGuide()
        {
            if(!guideHand.gameObject.activeSelf)return;
            Vector2 a,b;
            if(mode==Mode.Practice)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt,cam.WorldToScreenPoint(panelGo[0].transform.position+Vector3.up*.35f),null,out a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt,cam.WorldToScreenPoint(panelGo[1].transform.position+Vector3.up*.35f),null,out b);
            }
            else if(mode==Mode.Battery&&st.onboarding)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt,cam.WorldToScreenPoint(mergedBattery.position+Vector3.up*.35f),null,out a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt,cam.WorldToScreenPoint(DockWorld()+Vector3.up*.35f),null,out b);
            }
            else return;
            float travel=.5f+.5f*Mathf.Sin(titleT*1.25f);
            guideHand.anchoredPosition=Vector2.Lerp(a,b,travel);
            Vector2 delta=b-a;guidePath.anchoredPosition=(a+b)*.5f;guidePath.sizeDelta=new Vector2(delta.magnitude,8);
            guidePath.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }

        void SetGroups()
        {
            bool title=mode==Mode.Title,hud=mode!=Mode.Title&&mode!=Mode.End,end=mode==Mode.End;
            titleG.alpha=title?1:0;titleG.blocksRaycasts=false;hudG.alpha=hud?1:0;hudG.blocksRaycasts=false;endG.alpha=end?1:0;endG.blocksRaycasts=false;
            if(!hud){guideHand.gameObject.SetActive(false);guidePath.gameObject.SetActive(false);} else if(mode!=Mode.Practice&&!(mode==Mode.Battery&&st.onboarding)){guideHand.gameObject.SetActive(false);guidePath.gameObject.SetActive(false);}
        }

        void RefreshHud()
        {
            if(scoreUi==null)return;
            int score=Mathf.RoundToInt(displayScore),timer=Mathf.CeilToInt(runLeft);
            if(score!=lastHudScore){lastHudScore=score;scoreUi.text=score.ToString("0000");}
            if(st.lives!=lastHudLives){lastHudLives=st.lives;fuseUi.text="실수 여유 "+Math.Max(0,st.lives);}
            if(st.solved!=lastHudSolved){lastHudSolved=st.solved;progressUi.text="구간 "+st.solved+"/7";}
            if(timer!=lastHudTimer){lastHudTimer=timer;timerUi.text=timer.ToString();}
        }

        void UpdateUi(float dt)
        {
            displayScore=Mathf.MoveTowards(displayScore,st.score,Mathf.Max(60,Mathf.Abs(st.score-displayScore)*5)*dt);RefreshHud();
            if(toastT>0){toastT-=dt;toastG.alpha=Mathf.Clamp01(Mathf.Min(toastT*5f,(1.2f-toastT)*8f));}else toastG.alpha=0;
            if(revealT>0){revealT-=dt;revealG.alpha=Mathf.Clamp01(Mathf.Min(revealT*4f,(1.25f-revealT)*8f));}else revealG.alpha=0;
            float pulse=1f+.08f*Mathf.Sin(titleT*4.2f);if(mode==Mode.Practice||(mode==Mode.Battery&&st.onboarding)){guideHand.localScale=Vector3.one*pulse;UpdatePracticeGuide();}
            if(rippleImage.color.a>0){var c=rippleImage.color;c.a=Mathf.Max(0,c.a-dt*2.6f);rippleImage.color=c;rippleRt.localScale+=Vector3.one*dt*2.4f;}
        }

        void Refuse(string message){toastUi.text=message;ShowToast(1.2f);if(promptUi)MgfFx.Punch(promptUi.transform,.05f,.18f);}
        void ShowToast(float sec){toastT=sec;toastG.alpha=1;}
        void ShowReveal(float sec,Color c){revealT=sec;revealG.alpha=1;revealG.GetComponent<Image>().color=c;}
        void SpawnRipple(Vector2 screen){Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt,screen,null,out local);rippleRt.anchoredPosition=local;rippleRt.localScale=Vector3.one*.35f;rippleImage.color=new Color(Signal.r,Signal.g,Signal.b,.9f);}

        public void TestStart(){BeginRun(false);}
        public void TestAnswerCorrect()
        {
            if(mode==Mode.Title||mode==Mode.End)BeginRun(false);
            if(mode==Mode.Playing||mode==Mode.Practice){SubmitPair(current.correctI,current.correctJ);feedbackT=1.03f;UpdateMergeFeedback();CompleteDock();}
            else if(mode==Mode.Battery)CompleteDock();
        }
        public void TestAnswerWrong()
        {
            if(mode==Mode.Title||mode==Mode.End)BeginRun(false);
            if((mode==Mode.Battery||mode==Mode.MergeFeedback)&&!st.onboarding)LoadNext();
            if(mode!=Mode.Playing&&mode!=Mode.Practice)return;
            for(int i=0;i<5;i++)for(int j=i+1;j<5;j++)if(!current.IsCorrectPair(i,j)){SubmitPair(i,j);return;}
        }
        public string StateJson()=>JsonUtility.ToJson(st);
        public string ProblemBankJson()=>MgfJson.Bank(bank);

        RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=offset;rt.sizeDelta=size;return rt;
        }
        Image Img(Transform parent,string name,Vector2 anchor,Vector2 offset,Vector2 size,Color color,Sprite sprite=null)
        {
            var rt=Rect(name,parent,anchor,offset,size);var im=rt.gameObject.AddComponent<Image>();im.color=color;im.sprite=sprite;im.raycastTarget=false;if(sprite)im.type=Image.Type.Sliced;return im;
        }
        TextMeshProUGUI Txt(Transform parent,string text,Vector2 anchor,Vector2 offset,float size,Color color,float width)
        {
            var rt=Rect("Text",parent,anchor,offset,new Vector2(width,size*2.2f));var t=rt.gameObject.AddComponent<TextMeshProUGUI>();t.font=MgfText.Font;t.text=text;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;
        }
        Sprite MakeSprite(int size,int radius,bool ring)
        {
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};var px=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=Mathf.Max(Mathf.Abs(x-size*.5f)-(size*.5f-radius),0),dy=Mathf.Max(Mathf.Abs(y-size*.5f)-(size*.5f-radius),0);float dist=Mathf.Sqrt(dx*dx+dy*dy);float a=Mathf.Clamp01(radius-dist);
                if(ring){float inner=Mathf.Clamp01(radius-7-dist);a=Mathf.Max(0,a-inner);}px[y*size+x]=new Color32(255,255,255,(byte)(a*255));
            }
            tex.SetPixels32(px);tex.Apply(false,true);return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(radius,radius,radius,radius));
        }
    }
}
