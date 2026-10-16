using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.HwamulJam
{
    public sealed class HwamulJamGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int combo;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int lateFirstCorrect;
            public int moves;
            public int pointerVersion;
            public int selectedCargo = -1;
            public string problemId = "";
            public string misconceptionId = "";
            public bool onboarding;
            public string lastResult = "";
            public string endReason = "";
            public string inputTargets = "";
            public int tutorialStep;
        }

        enum Mode { Title, Practice, Playing, Feedback, End }

        static readonly Color Plaster = MgfLook.Hex("#D8D0BD");
        static readonly Color Ink = MgfLook.Hex("#17232B");
        static readonly Color Coral = MgfLook.Hex("#C85F43");
        static readonly Color Cyan = MgfLook.Hex("#63D5DB");
        static readonly Color Sage = MgfLook.Hex("#34464B");
        static readonly Color Mist = MgfLook.Hex("#AEB7B2");

        readonly State st = new State();
        readonly List<SimilarityOrder> catalog = new List<SimilarityOrder>();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        List<SimilarityOrder> deck;
        SimilarityOrder current;
        Mode mode = Mode.Title;
        int runSerial;
        bool firstAttempt;
        bool dragging;
        int dragCargo = -1;
        float timeLeft;
        float feedbackT;
        bool feedbackCorrect;
        bool feedbackPractice;
        float guideIdle;
        string endReason = "";

        Camera cam;
        Transform worldRoot, boardRoot, signalCargo, truck, liftA, liftB, craneHook;
        readonly Transform[] fog = new Transform[18];
        readonly Vector3[] fogHome = new Vector3[18];
        readonly Transform[] cargoRoot = new Transform[8];
        readonly Renderer[] cargoRenderer = new Renderer[8];
        readonly TextMeshPro[] cargoText = new TextMeshPro[8];
        readonly TextMeshProUGUI[] cargoOverlay = new TextMeshProUGUI[8];
        readonly Image[] cargoCard = new Image[8];
        readonly int[] cellX = new int[8];
        readonly int[] cellY = new int[8];
        readonly bool[] cargoActive = new bool[8];
        readonly bool[] cargoQuarantine = new bool[8];
        readonly Vector3[] cargoTarget = new Vector3[8];
        readonly Transform[] quarantineSlots = new Transform[3];
        readonly Transform[] pins = new Transform[3];
        readonly Renderer[] pinRenderer = new Renderer[3];
        readonly Transform[] railSegments = new Transform[3];
        Transform guideRing, guideArrow, guideFinger, tapRipple, wallFlash;
        readonly Transform[] cliffSeams = new Transform[6];
        readonly Renderer[] cliffSeamRenderers = new Renderer[6];
        readonly Transform[] stoneShutters = new Transform[3];
        readonly Vector3[] shutterHome = new Vector3[3];
        Material plasterMat, selectedMat, inkMat, coralMat, cyanMat, cyanGlowMat, sageMat, fogMat, glassMat, rockMat, dustMat;
        int quarantineCount;
        float worldClock, rippleT=10f, refuseT=10f, scoreDisplay, endScoreDisplay;
        float movePunchT, successT=-1f;

        TextMeshProUGUI titleLogo, titleTag, titleMeta, titlePrompt;
        TextMeshProUGUI hudText, goalText, targetText, measureText, hintText, revealText;
        TextMeshProUGUI endTitle, endStats, endPrompt;
        RectTransform measurePanel;
        int practiceStep;
        Vector3 guideFrom, guideTo;

        void Awake()
        {
            MgfLook.Quality(34f);
            catalog.AddRange(HwamulRules.BuildBank());
            for(int i=0;i<catalog.Count;i++) bank.Add(HwamulRules.ToMgf(catalog[i]));
            Prewarm(); BuildWorld(); BuildUi(); ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var b=new StringBuilder("화물잼닮은화물의길을터라안개절벽백색석고승강장도크격리선반중학교2학년도형의닮음대응변대응각닮음비");
            b.Append("목표화물을밀어도크로보내시오주문남은시간첫시도정확도이동완료다시가동SSS닮음SAS닮음AA닮음끼인각");
            for(int i=0;i<catalog.Count;i++){b.Append(catalog[i].prompt);b.Append(catalog[i].targetLabel);b.Append(catalog[i].reveal);}
            MgfText.Prewarm(b.ToString());
        }

        void BuildWorld()
        {
            // WebGL IL2CPP에서 사용하는 collider 타입을 보존한다.
            // CreatePrimitive(Cylinder)는 내부적으로 CapsuleCollider를 먼저 붙이므로 WebGL 스트리핑에서 타입을 보존한다.
            var keep=new GameObject("CargoColliderTypes");keep.AddComponent<BoxCollider>();keep.AddComponent<SphereCollider>();keep.AddComponent<CapsuleCollider>();Destroy(keep);
            MgfLook.Sky(MgfLook.Hex("#526B72"),MgfLook.Hex("#A6B4B3"),MgfLook.Hex("#1A2930"),.9f);
            MgfLook.Sun(new Vector3(48,-38,4),MgfLook.Hex("#FFE7C2"),1.24f,.72f);
            cam=MgfLook.Camera(new Vector3(10.8f,12.8f,-15.2f),new Vector3(0,.5f,.2f),34f);
            cam.orthographic=true;cam.orthographicSize=8.4f;
            plasterMat=MgfLook.Lit(Plaster,.08f,.01f);
            selectedMat=MgfLook.Lit(MgfLook.Hex("#EEE4CD"),.16f,.02f,Cyan*.24f);
            inkMat=MgfLook.Lit(Ink,.12f,.04f);
            coralMat=MgfLook.Lit(Coral,.16f,.04f,Coral*.06f);
            cyanMat=MgfLook.Lit(Cyan,.62f,.18f);
            cyanGlowMat=MgfLook.Lit(Cyan,.75f,.25f,Cyan*.85f);
            sageMat=MgfLook.Lit(Sage,.1f,.02f);
            rockMat=MgfLook.Lit(MgfLook.Hex("#23343B"),.06f,.01f);
            dustMat=MgfLook.Lit(MgfLook.Hex("#8D897D"),.04f,.0f);
            fogMat=MgfLook.Alpha(new Color(.72f,.79f,.78f,.20f));
            glassMat=MgfLook.Lit(MgfLook.Hex("#86D8DD"),.74f,.18f,Cyan*.54f);
            worldRoot=new GameObject("FogCliffFreightWorld").transform;
            BuildCliffs();BuildBoard();BuildCargo();BuildDock();BuildTitleSignal();
        }

        void BuildCliffs()
        {
            // 둥근 파스텔 탁자 대신 수직 단면이 화면을 채우는 거친 현무암 물류 절벽.
            Hard("LowerBasalt",new Vector3(0,-2.35f,1.2f),new Vector3(15.8f,4.1f,13.5f),rockMat,worldRoot);
            for(int i=0;i<9;i++)
            {
                float x=-8.2f+i*2.05f;
                float y=-.7f+(i%3)*.62f;
                var stratum=Hard("CliffStratum"+i,new Vector3(x,y,5.3f+(i%2)*.42f),new Vector3(2.2f,3.2f+(i%4)*.35f,3.8f),i%2==0?sageMat:rockMat,worldRoot);
                stratum.transform.rotation=Quaternion.Euler(0,(i%3-1)*4f,(i%2==0?-1:1)*2.5f);
                Hard("FreshBreak"+i,new Vector3(x,y+1.25f,3.25f+(i%2)*.18f),new Vector3(1.55f,.18f,.24f),dustMat,worldRoot).transform.rotation=Quaternion.Euler(0,0,(i%3-1)*9f);
            }
            Hard("UpperQuarry",new Vector3(-7,.25f,5.2f),new Vector3(6.8f,1.1f,4.6f),dustMat,worldRoot).transform.rotation=Quaternion.Euler(0,-3,0);
            Hard("LowerQuarry",new Vector3(7,-.55f,4.7f),new Vector3(7.2f,1.25f,5.1f),sageMat,worldRoot).transform.rotation=Quaternion.Euler(0,4,0);
            Hard("Tunnel",new Vector3(6.4f,-.05f,1.15f),new Vector3(3.7f,2.8f,3.1f),rockMat,worldRoot);
            Hard("TunnelMouth",new Vector3(5.75f,.45f,-.47f),new Vector3(2.15f,1.35f,.30f),inkMat,worldRoot);
            for(int i=0;i<6;i++)
            {
                cliffSeams[i]=Hard("SignalSeam"+i,new Vector3(-5.0f+i*1.85f,-.35f,6.62f),new Vector3(1.45f,.10f,.10f),inkMat,worldRoot).transform;
                cliffSeamRenderers[i]=cliffSeams[i].GetComponent<Renderer>();
                cliffSeams[i].rotation=Quaternion.Euler(0,0,(i%2==0?1:-1)*12f);
            }
            for(int i=0;i<3;i++)
            {
                stoneShutters[i]=Hard("RatioShutter"+i,new Vector3(3.9f+i*.82f,.58f,-.64f),new Vector3(.72f,1.55f,.26f),dustMat,worldRoot).transform;
                shutterHome[i]=stoneShutters[i].position;
            }
            for(int i=0;i<18;i++)
            {
                var f=MgfLook.Prim(PrimitiveType.Sphere,"FogBank"+i,new Vector3(-13+(i*3.17f)%26,-1.4f+(i%3)*.48f,-1+(i*2.31f)%13),new Vector3(3.2f+(i%4)*.5f,.42f,.95f),fogMat,worldRoot,false);
                f.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;fog[i]=f.transform;fogHome[i]=f.transform.localPosition;
            }
            liftA=Hard("FreightLiftA",new Vector3(-7.4f,1.4f,1.7f),new Vector3(2.5f,.45f,2.6f),dustMat,worldRoot).transform;
            liftB=Hard("FreightLiftB",new Vector3(7.6f,1.0f,5.3f),new Vector3(2.2f,.42f,2.2f),dustMat,worldRoot).transform;
            for(int i=0;i<8;i++)
            {
                float x=-8.5f+i*2.45f;
                Hard("BridgePier"+i,new Vector3(x,-.2f,7.1f),new Vector3(.34f,3.5f,.34f),inkMat,worldRoot);
                if(i<7)Hard("BridgeSpan"+i,new Vector3(x+1.22f,1.42f,7.1f),new Vector3(2.2f,.18f,.36f),dustMat,worldRoot);
            }
        }

        GameObject Hard(string name,Vector3 pos,Vector3 scale,Material mat,Transform parent=null)
        {
            return MgfLook.Prim(PrimitiveType.Cube,name,pos,scale,mat,parent,false);
        }

        Vector3 CellWorld(int x,int y){return new Vector3(-2.25f+x*1.5f,.74f,-2.15f+y*1.5f);}

        void BuildBoard()
        {
            boardRoot=new GameObject("FourByFourLiftDeck").transform;boardRoot.SetParent(worldRoot,false);
            Hard("Deck",new Vector3(0,.14f,.1f),new Vector3(7.15f,.50f,7.15f),dustMat,boardRoot);
            for(int y=0;y<4;y++)for(int x=0;x<4;x++)
            {
                Vector3 p=CellWorld(x,y);p.y=.47f;
                Hard("RubberCell_"+x+"_"+y,p,new Vector3(1.34f,.07f,1.34f),inkMat,boardRoot);
                Hard("CellInset",p+Vector3.up*.052f,new Vector3(1.17f,.045f,1.17f),sageMat,boardRoot);
            }
            // 단 하나의 활성 도크: 보드 오른쪽, 둘째 줄.
            MgfLook.Block("ActiveDock",new Vector3(4.05f,.48f,CellWorld(3,1).z),new Vector3(2.45f,.42f,1.36f),.18f,inkMat,boardRoot);
            for(int i=0;i<3;i++)
            {
                railSegments[i]=MgfLook.Block("FoldingRail"+i,new Vector3(3.1f+i*1.15f,.73f,CellWorld(3,1).z),new Vector3(1.05f,.12f,.24f),.04f,cyanMat,boardRoot).transform;
                pins[i]=MgfLook.Prim(PrimitiveType.Cylinder,"CorrespondencePin"+i,new Vector3(3.2f+i*.58f,1.0f,CellWorld(3,1).z-.72f),new Vector3(.12f,.1f,.12f),inkMat,boardRoot,false).transform;
                pinRenderer[i]=pins[i].GetComponent<Renderer>();
            }
            for(int i=0;i<3;i++)
            {
                var s=Hard("QuarantineSlot"+i,new Vector3(-2.2f+i*2.15f,.38f,-4.0f),new Vector3(1.65f,.28f,1.35f),coralMat,boardRoot);
                quarantineSlots[i]=s.transform;
            }
            wallFlash=MgfLook.Block("BlockedGrooveFlash",new Vector3(0,.86f,3.18f),new Vector3(4.8f,.08f,.12f),.03f,cyanGlowMat,boardRoot).transform;
            wallFlash.gameObject.SetActive(false);
            guideRing=MgfLook.Prim(PrimitiveType.Cylinder,"GuideRing",Vector3.zero,new Vector3(1.2f,.025f,1.2f),cyanGlowMat,worldRoot,false).transform;
            guideArrow=MgfLook.Block("GuideArrow",Vector3.zero,new Vector3(.75f,.07f,.18f),.05f,cyanGlowMat,worldRoot).transform;
            guideFinger=MgfLook.Prim(PrimitiveType.Sphere,"GuideFinger",Vector3.zero,new Vector3(.28f,.28f,.28f),cyanGlowMat,worldRoot,false).transform;
            guideRing.gameObject.SetActive(false);guideArrow.gameObject.SetActive(false);guideFinger.gameObject.SetActive(false);
            tapRipple=MgfLook.Prim(PrimitiveType.Cylinder,"TapRipple",Vector3.zero,new Vector3(.3f,.02f,.3f),cyanGlowMat,worldRoot,false).transform;
            tapRipple.gameObject.SetActive(false);
        }

        void BuildCargo()
        {
            for(int i=0;i<8;i++)
            {
                var root=new GameObject("Cargo_"+i).transform;root.SetParent(worldRoot,false);
                var body=Hard("ChippedPlasterCase",Vector3.zero,new Vector3(1.24f,.58f,1.24f),plasterMat,root);
                cargoRenderer[i]=body.GetComponent<Renderer>();
                var col=root.gameObject.AddComponent<BoxCollider>();col.size=new Vector3(1.42f,.92f,1.42f);col.center=new Vector3(0,.12f,0);
                Hard("RubberGroove",new Vector3(0,.34f,0),new Vector3(1.08f,.05f,.16f),inkMat,root);
                var chip=Hard("BrokenCorner",new Vector3(.48f,.31f,-.58f),new Vector3(.27f,.14f,.08f),dustMat,root);
                chip.transform.localRotation=Quaternion.Euler(0,0,28f-(i%3)*14f);
                var crack=Hard("GraphiteCrack",new Vector3(-.18f,.03f,-.638f),new Vector3(.48f,.035f,.018f),inkMat,root);
                crack.transform.localRotation=Quaternion.Euler(0,0,-18f+(i%2)*31f);
                // 비스듬한 카메라에서도 수치가 눕지 않도록 전면에 직접 각인한다.
                var txt=MgfText.World("",new Vector3(0,.02f,-.642f),2.25f,Ink,root);
                txt.transform.localRotation=Quaternion.identity;txt.alignment=TextAlignmentOptions.Center;txt.fontStyle=FontStyles.Bold;txt.characterSpacing=-2f;
                txt.textWrappingMode=TextWrappingModes.Normal;txt.lineSpacing=-18f;txt.enableAutoSizing=true;txt.fontSizeMin=1.35f;txt.fontSizeMax=2.45f;
                txt.rectTransform.sizeDelta=new Vector2(1.16f,.86f);
                cargoText[i]=txt;cargoRoot[i]=root;
            }
        }

        void BuildDock()
        {
            truck=new GameObject("WhiteFreightCart").transform;truck.SetParent(worldRoot,false);
            Hard("CarrierBody",new Vector3(0,.25f,0),new Vector3(1.15f,.46f,.72f),dustMat,truck);
            Hard("CarrierCab",new Vector3(.32f,.64f,0),new Vector3(.45f,.38f,.66f),glassMat,truck);
            for(int i=0;i<4;i++)MgfLook.Prim(PrimitiveType.Cylinder,"Wheel"+i,new Vector3(i<2?-.34f:.34f,.02f,i%2==0?-.39f:.39f),new Vector3(.15f,.09f,.15f),inkMat,truck,false).transform.localRotation=Quaternion.Euler(90,0,0);
            truck.gameObject.SetActive(false);
        }

        void BuildTitleSignal()
        {
            Hard("CraneBeam",new Vector3(0,5.35f,1.1f),new Vector3(10.2f,.40f,.58f),inkMat,worldRoot);
            craneHook=Hard("CraneHook",new Vector3(0,4.25f,1.1f),new Vector3(.14f,2.0f,.14f),inkMat,worldRoot).transform;
            signalCargo=new GameObject("TitleSignalCargo").transform;signalCargo.SetParent(worldRoot,false);signalCargo.position=new Vector3(0,3.1f,1.1f);
            Hard("SignalCase",Vector3.zero,new Vector3(2.35f,1.05f,1.35f),dustMat,signalCargo);
            Hard("CyanSignal",new Vector3(0,.12f,-.7f),new Vector3(1.25f,.42f,.08f),cyanGlowMat,signalCargo);
            signalCargo.gameObject.AddComponent<BoxCollider>().size=new Vector3(2.7f,1.4f,1.7f);
        }

        TextMeshProUGUI U(string text,Vector2 anchor,Vector2 offset,float size,Color color,float width=370)
        {
            var t=MgfText.Ui(text,anchor,offset,size,color,width);t.fontStyle=FontStyles.Bold;t.textWrappingMode=TextWrappingModes.Normal;return t;
        }

        void BuildUi()
        {
            titleLogo=U("화 물  잼",new Vector2(.5f,.72f),Vector2.zero,54,Color.white,370);titleLogo.outlineWidth=.24f;titleLogo.outlineColor=Ink;
            titleTag=U("닮은 화물의 길을 터라",new Vector2(.5f,.63f),Vector2.zero,22,Plaster,360);titleTag.outlineWidth=.16f;titleTag.outlineColor=Ink;
            titleMeta=U("중학교 2학년 · 도형의 닮음",new Vector2(.5f,.57f),Vector2.zero,15,Color.white,340);titleMeta.outlineWidth=.14f;titleMeta.outlineColor=Ink;
            titlePrompt=U("매달린 청록 신호 화물을 밀어 도크 가동",new Vector2(.5f,.15f),Vector2.zero,20,Color.white,360);titlePrompt.outlineWidth=.18f;titlePrompt.outlineColor=Ink;
            hudText=U("",new Vector2(.5f,.975f),Vector2.zero,17,Color.white,370);hudText.outlineWidth=.16f;hudText.outlineColor=Ink;
            targetText=U("",new Vector2(.5f,.895f),Vector2.zero,18,Ink,376);
            goalText=U("닮은 화물 하나의 길을 열어 도크로 보내라",new Vector2(.5f,.82f),Vector2.zero,16,Color.white,370);goalText.outlineWidth=.16f;goalText.outlineColor=Ink;
            var panelGo=new GameObject("MeasurePanel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));panelGo.transform.SetParent(MgfText.Canvas.transform,false);
            measurePanel=(RectTransform)panelGo.transform;measurePanel.anchorMin=measurePanel.anchorMax=new Vector2(.5f,.145f);measurePanel.pivot=new Vector2(.5f,.5f);measurePanel.sizeDelta=new Vector2(374,86);
            panelGo.GetComponent<Image>().color=new Color(.055f,.09f,.11f,.88f);panelGo.GetComponent<Image>().raycastTarget=false;
            measureText=U("",new Vector2(.5f,.145f),Vector2.zero,25,Color.white,360);measureText.enableAutoSizing=true;measureText.fontSizeMin=17;measureText.fontSizeMax=25;
            hintText=U("",new Vector2(.5f,.065f),Vector2.zero,15,Color.white,370);hintText.outlineWidth=.15f;hintText.outlineColor=Ink;
            revealText=U("",new Vector2(.5f,.46f),Vector2.zero,24,Color.white,370);revealText.outlineWidth=.22f;revealText.outlineColor=Ink;
            endTitle=U("",new Vector2(.5f,.66f),Vector2.zero,48,Color.white,370);endTitle.outlineWidth=.24f;endTitle.outlineColor=Ink;
            endStats=U("",new Vector2(.5f,.49f),Vector2.zero,23,Ink,370);
            endPrompt=U("신호 화물을 눌러 다시 가동",new Vector2(.5f,.20f),Vector2.zero,21,Color.white,370);endPrompt.outlineWidth=.18f;endPrompt.outlineColor=Ink;
            for(int i=0;i<8;i++)
            {
                float x=-142.5f+(i%4)*95f,y=i<4?-145f:-218f;
                var cardGo=new GameObject("CargoDataCard"+(i+1),typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));cardGo.transform.SetParent(MgfText.Canvas.transform,false);
                var cardRect=(RectTransform)cardGo.transform;cardRect.anchorMin=cardRect.anchorMax=new Vector2(.5f,.5f);cardRect.pivot=new Vector2(.5f,.5f);cardRect.anchoredPosition=new Vector2(x,y);cardRect.sizeDelta=new Vector2(91,65);
                cargoCard[i]=cardGo.GetComponent<Image>();cargoCard[i].color=new Color(.86f,.82f,.72f,.96f);cargoCard[i].raycastTarget=false;
                cargoOverlay[i]=U("",new Vector2(.5f,.5f),new Vector2(x,y),15,Ink,87);
                cargoOverlay[i].rectTransform.sizeDelta=new Vector2(87,61);cargoOverlay[i].lineSpacing=-8f;
                cargoOverlay[i].enableAutoSizing=true;cargoOverlay[i].fontSizeMin=11;cargoOverlay[i].fontSizeMax=15;
            }
        }

        void SetUiGroup(bool title,bool play,bool end,bool reveal)
        {
            titleLogo.gameObject.SetActive(title);titleTag.gameObject.SetActive(title);titleMeta.gameObject.SetActive(title);titlePrompt.gameObject.SetActive(title);
            hudText.gameObject.SetActive(play);targetText.gameObject.SetActive(play);goalText.gameObject.SetActive(play);measurePanel.gameObject.SetActive(play);measureText.gameObject.SetActive(play);hintText.gameObject.SetActive(play);
            for(int i=0;i<8;i++){bool show=play&&cargoActive[i]&&!cargoQuarantine[i];cargoCard[i].gameObject.SetActive(show);cargoOverlay[i].gameObject.SetActive(show);}
            endTitle.gameObject.SetActive(end);endStats.gameObject.SetActive(end);endPrompt.gameObject.SetActive(end);
            revealText.gameObject.SetActive(reveal);
        }

        void ShowTitle()
        {
            mode=Mode.Title;endReason="";current=null;deck=null;dragging=false;dragCargo=-1;quarantineCount=0;
            st.score=0;st.lives=HwamulRules.StartLives;st.level=1;st.solved=0;st.combo=0;st.firstAttemptTotal=0;st.firstAttemptCorrect=0;st.lateFirstCorrect=0;st.moves=0;st.pointerVersion=0;st.selectedCargo=-1;st.problemId="";st.misconceptionId="";st.onboarding=false;st.lastResult="";st.endReason="";st.tutorialStep=0;st.phase="title";
            titleMeta.text="중학교 2학년 · 도형의 닮음 · 최고 "+PlayerPrefs.GetInt("hwamul-jam-best",0)+"점";
            timeLeft=HwamulRules.RunSeconds;signalCargo.gameObject.SetActive(true);SetCargoVisible(false);ResetFeedbackVisuals();SetUiGroup(true,false,false,false);MgfBridge.NotifyChanged();
        }

        void BeginRun(bool onboarding)
        {
            runSerial++;deck=HwamulRules.RunDeck(catalog,runSerial);quarantineCount=0;endReason="";
            st.score=0;st.lives=HwamulRules.StartLives;st.level=1;st.solved=0;st.combo=0;st.firstAttemptTotal=0;st.firstAttemptCorrect=0;st.lateFirstCorrect=0;st.moves=0;st.selectedCargo=-1;st.misconceptionId="";st.onboarding=onboarding;st.lastResult="";st.endReason="";st.tutorialStep=0;st.phase="playing";
            timeLeft=HwamulRules.RunSeconds;signalCargo.gameObject.SetActive(false);SetCargoVisible(true);ResetFeedbackVisuals();
            goalText.text=onboarding?"연습: 손가락 경로를 따라 통로를 열어라":"클리어: 8개 완주 · 첫 제출 6/8 이상";
            mode=onboarding?Mode.Practice:Mode.Playing;current=onboarding?HwamulRules.FixedPractice():deck[0];LoadOrder(onboarding);SetUiGroup(false,true,false,false);MgfSfx.Play("whoosh",.32f);MgfBridge.NotifyChanged();
        }

        void LoadOrder(bool practice=false)
        {
            firstAttempt=true;dragging=false;dragCargo=-1;feedbackT=0;guideIdle=0;practiceStep=0;st.selectedCargo=-1;st.misconceptionId="";st.lastResult="";st.problemId=current.id;st.level=practice?1:current.band;st.tutorialStep=0;
            ResetCargoLayout(practice);ResetFeedbackVisuals();RefreshText();if(practice)ShowGuide();MgfBridge.NotifyChanged();
        }

        void ResetCargoLayout(bool practice)
        {
            int[] xs={3,2,1,0,0,1,2,3};int[] ys={1,1,1,1,2,2,2,2};
            for(int i=0;i<8;i++)
            {
                cellX[i]=xs[i];cellY[i]=ys[i];cargoActive[i]=true;cargoQuarantine[i]=false;
                cargoRoot[i].gameObject.SetActive(true);cargoRoot[i].GetComponent<Collider>().enabled=true;
                cargoTarget[i]=CellWorld(cellX[i],cellY[i]);cargoRoot[i].position=cargoTarget[i];cargoRoot[i].localScale=Vector3.one;cargoRenderer[i].sharedMaterial=plasterMat;cargoCard[i].color=new Color(.86f,.82f,.72f,.96f);
                int[] v=current.cargo[i].v;
                cargoText[i].text="H-"+(i+1).ToString("D2");
                cargoOverlay[i].text=current.kind==SimilarityKind.SSS?"H-"+(i+1).ToString("D2")+" · SSS\nDE "+v[0]+" · EF "+v[1]+"\nFD "+v[2]+" cm":
                    current.kind==SimilarityKind.SAS?"H-"+(i+1).ToString("D2")+" · SAS\nDE "+v[0]+" · DF "+v[1]+"\n∠D "+v[2]+"°":
                    "H-"+(i+1).ToString("D2")+" · AA\n∠D "+v[0]+"° · ∠E "+v[1]+"°\n∠F "+v[2]+"°";
                cargoCard[i].gameObject.SetActive(true);cargoOverlay[i].gameObject.SetActive(true);
            }
        }

        void SetCargoVisible(bool visible){for(int i=0;i<8;i++){cargoRoot[i].gameObject.SetActive(visible);if(cargoCard[i])cargoCard[i].gameObject.SetActive(visible);if(cargoOverlay[i])cargoOverlay[i].gameObject.SetActive(visible);}}

        void RefreshText()
        {
            int seconds=Mathf.CeilToInt(timeLeft);
            hudText.text="주문"+Mathf.Min(HwamulRules.Goal,st.solved+1)+"/"+HwamulRules.Goal+" · 정확"+st.firstAttemptCorrect+"/6 · 격리"+quarantineCount+" · "+seconds+"초";
            targetText.text=current==null?"":current.targetLabel;
            if(st.selectedCargo>=0&&st.selectedCargo<8)measureText.text="계측 H-"+(st.selectedCargo+1).ToString("D2")+"  "+current.cargo[st.selectedCargo].label;
            else measureText.text="화물을 길게 잡으면 각인 자료를 크게 확인할 수 있다";
            if(mode==Mode.Practice)hintText.text=practiceStep==0?"1. 움직이는 손가락대로 H-01을 아래 빈칸까지 길게 끄시오":"2. H-02를 오른쪽 청록 도크 끝까지 한 번에 끄시오";
            else hintText.text="클리어 조건: 8개 완주 · 첫 제출 6개 이상";
        }

        void Update()
        {
            float dt=Mathf.Min(.05f,Time.deltaTime);worldClock+=dt;HandleInput();
            if(mode==Mode.Practice){guideIdle+=dt;if(guideIdle>=8f){guideIdle=0;ShowGuide();}}
            else if(mode==Mode.Playing){timeLeft-=dt;if(timeLeft<=0){timeLeft=0;EndRun("time");}}
            else if(mode==Mode.Feedback){feedbackT+=dt;if(feedbackT>=(feedbackCorrect?1.75f:1.25f))FinishFeedback();}
            UpdateWorld(dt);UpdateUi(dt);
        }

        void HandleInput()
        {
            if(MgfPointer.Down)
            {
                st.pointerVersion++;SpawnRipple();MgfBridge.NotifyChanged();
                if(mode==Mode.Title){NudgeSignal();BeginRun(true);return;}
                if(mode==Mode.End){BeginRun(true);return;}
                if(mode==Mode.Feedback){Refuse("레일이 움직이는 동안 잠깐 기다리시오");return;}
                if(mode!=Mode.Practice&&mode!=Mode.Playing)return;
                if(TryCargoHit(out int index))
                {
                    dragging=true;dragCargo=index;st.selectedCargo=index;guideIdle=0;cargoRenderer[index].sharedMaterial=selectedMat;cargoCard[index].color=new Color(.39f,.84f,.86f,.98f);movePunchT=.22f;HideGuide();RefreshText();MgfSfx.Play("tap",.22f);MgfBridge.NotifyChanged();
                }
                else Refuse("석고 화물을 잡아 검은 홈을 따라 미시오");
            }
            if(dragging&&MgfPointer.Held)DragTowardPointer();
            if(dragging&&MgfPointer.Up)
            {
                bool submitted=TrySubmitAtDock();
                if(!submitted&&dragCargo>=0)cargoRenderer[dragCargo].sharedMaterial=plasterMat;
                dragging=false;dragCargo=-1;MgfBridge.NotifyChanged();
            }
        }

        bool TryCargoHit(out int index)
        {
            index=-1;if(!MgfPointer.DownHit(cam,out var hit))return false;
            Transform t=hit.collider.transform;
            for(int i=0;i<8;i++)if(cargoActive[i]&&!cargoQuarantine[i]&&(t==cargoRoot[i]||t.IsChildOf(cargoRoot[i]))){index=i;return true;}
            return false;
        }

        bool Occupied(int x,int y,int except=-1)
        {
            for(int i=0;i<8;i++)if(i!=except&&cargoActive[i]&&!cargoQuarantine[i]&&cellX[i]==x&&cellY[i]==y)return true;return false;
        }

        void DragTowardPointer()
        {
            if(dragCargo<0||!MgfPointer.OnPlane(cam,.74f,out var p))return;
            int nx=Mathf.Clamp(Mathf.RoundToInt((p.x+2.25f)/1.5f),0,3);
            int ny=Mathf.Clamp(Mathf.RoundToInt((p.z+2.15f)/1.5f),0,3);
            int guard=0;bool moved=false;
            while((cellX[dragCargo]!=nx||cellY[dragCargo]!=ny)&&guard++<8)
            {
                int dx=nx-cellX[dragCargo],dy=ny-cellY[dragCargo];
                bool useX=Mathf.Abs(dx)>=Mathf.Abs(dy)&&dx!=0;
                int sx=useX?Math.Sign(dx):0,sy=useX?0:Math.Sign(dy);
                int tx=cellX[dragCargo]+sx,ty=cellY[dragCargo]+sy;
                if(Occupied(tx,ty,dragCargo))
                {
                    if(useX&&dy!=0&&!Occupied(cellX[dragCargo],cellY[dragCargo]+Math.Sign(dy),dragCargo)){tx=cellX[dragCargo];ty=cellY[dragCargo]+Math.Sign(dy);}
                    else if(!useX&&dx!=0&&!Occupied(cellX[dragCargo]+Math.Sign(dx),cellY[dragCargo],dragCargo)){tx=cellX[dragCargo]+Math.Sign(dx);ty=cellY[dragCargo];}
                    else{Refuse("앞 칸이 막혀 있다 · 다른 화물을 먼저 옮기시오");break;}
                }
                cellX[dragCargo]=tx;cellY[dragCargo]=ty;cargoTarget[dragCargo]=CellWorld(tx,ty);st.moves++;moved=true;
                if(mode==Mode.Practice&&practiceStep==0&&dragCargo==0&&cellX[0]==3&&cellY[0]==0){practiceStep=1;st.tutorialStep=1;ShowGuide();RefreshText();}
            }
            if(moved){movePunchT=.16f;MgfSfx.Play("pop",.14f);MgfBridge.NotifyChanged();}
        }

        bool TrySubmitAtDock()
        {
            if(dragCargo<0||cellX[dragCargo]!=3||cellY[dragCargo]!=1)return false;
            if(!MgfPointer.OnPlane(cam,.74f,out var p)||p.x<3.15f)return false;
            SubmitCargo(dragCargo);return true;
        }

        void SubmitCargo(int index)
        {
            if(mode!=Mode.Practice&&mode!=Mode.Playing)return;
            bool practice=mode==Mode.Practice;bool ok=HwamulRules.IsCorrect(current,index);
            feedbackCorrect=ok;feedbackPractice=practice;feedbackT=0;mode=Mode.Feedback;st.lastResult=ok?"correct":"wrong";
            cargoRenderer[index].sharedMaterial=ok?cyanGlowMat:coralMat;
            if(ok)
            {
                if(practice){st.score+=120;st.solved=1;st.combo=1;}
                else
                {
                    if(firstAttempt){st.firstAttemptTotal++;st.firstAttemptCorrect++;if(st.solved>=5)st.lateFirstCorrect++;}
                    st.solved++;st.combo++;st.score+=160+st.level*40+Mathf.Min(4,st.combo)*25;
                }
                st.misconceptionId="";successT=0;revealText.text=current.reveal;MgfSfx.Play("correct",.52f);
            }
            else
            {
                if(!practice)
                {
                    if(firstAttempt)st.firstAttemptTotal++;
                    st.lives--;st.combo=0;quarantineCount++;
                }
                st.misconceptionId=current.cargo[index].misconceptionId;
                Quarantine(index,practice?0:quarantineCount-1);revealText.text=WrongText(st.misconceptionId);MgfFx.Shake(cam,.09f,.24f);MgfSfx.Play("wrong",.45f);
            }
            firstAttempt=false;SetUiGroup(false,true,false,true);RefreshText();MgfBridge.NotifyChanged();
        }

        static string WrongText(string id)
        {
            if(id=="additive_reasoning")return "길이의 차가 아니라 대응변의 비가 모두 같아야 한다";
            if(id=="correspondence_order")return "△ABC∽△DEF의 꼭짓점 순서대로 대응변·대응각을 확인하시오";
            if(id=="non_included_angle")return "SAS에서는 비가 같은 두 변 사이의 끼인각이 같아야 한다";
            if(id=="one_angle_only")return "한 각만 같아서는 부족하다 · 두 대응각을 확인하시오";
            return "세 대응 자료 중 어긋난 관계가 있다";
        }

        void Quarantine(int index,int slot)
        {
            cargoQuarantine[index]=true;cargoActive[index]=false;cargoRoot[index].GetComponent<Collider>().enabled=false;
            cargoCard[index].gameObject.SetActive(false);cargoOverlay[index].gameObject.SetActive(false);
            cargoTarget[index]=quarantineSlots[Mathf.Clamp(slot,0,2)].position+Vector3.up*.52f;
        }

        void FinishFeedback()
        {
            SetUiGroup(false,true,false,false);ResetFeedbackVisuals();
            if(feedbackPractice)
            {
                if(feedbackCorrect){st.score=0;st.solved=0;st.combo=0;st.onboarding=false;mode=Mode.Playing;current=deck[0];LoadOrder();goalText.text="클리어: 8개 완주 · 첫 제출 6/8 이상";}
                else{mode=Mode.Practice;current=HwamulRules.FixedPractice();LoadOrder(true);}
                return;
            }
            if(st.lives<=0||quarantineCount>=3){EndRun("lives");return;}
            if(feedbackCorrect)
            {
                if(st.solved>=HwamulRules.Goal){EndRun(st.firstAttemptCorrect>=6?"clear":"mastery");return;}
                current=deck[st.solved];mode=Mode.Playing;LoadOrder();
            }
            else{mode=Mode.Playing;RefreshText();MgfBridge.NotifyChanged();}
        }

        void EndRun(string reason)
        {
            endReason=reason;mode=Mode.End;dragging=false;st.endReason=reason;st.phase=reason=="clear"?"clear":"gameover";
            if(st.score>PlayerPrefs.GetInt("hwamul-jam-best",0)){PlayerPrefs.SetInt("hwamul-jam-best",st.score);PlayerPrefs.Save();}
            endTitle.text=reason=="clear"?"도크 전면 개통":"승강기 잠금";
            endStats.text="첫 제출 "+st.firstAttemptCorrect+"/8 (기준 6) · 통과 "+st.solved+"/8\n이동 "+st.moves+"회 · "+st.score+"점";
            signalCargo.gameObject.SetActive(true);signalCargo.position=new Vector3(0,3.05f,1.1f);SetUiGroup(false,false,true,false);endScoreDisplay=0;MgfSfx.Play(reason=="clear"?"win":"lose",.55f);MgfBridge.NotifyChanged();
        }

        void EnsureTestPlayable()
        {
            if(mode==Mode.Title||mode==Mode.End)BeginRun(false);
            else if(mode==Mode.Feedback)
            {
                SetUiGroup(false,true,false,false);ResetFeedbackVisuals();
                if(st.lives<=0||st.solved>=HwamulRules.Goal)BeginRun(false);
                else{mode=Mode.Playing;if(feedbackCorrect){current=deck[st.solved];LoadOrder();}else RefreshText();}
            }
        }

        void ShowGuide()
        {
            int guided=practiceStep==0?0:1;
            guideFrom=cargoRoot[guided].position+Vector3.up*.76f;
            guideTo=(practiceStep==0?CellWorld(3,0):CellWorld(3,1)+Vector3.right*2.25f)+Vector3.up*.76f;
            guideRing.gameObject.SetActive(true);guideArrow.gameObject.SetActive(true);guideFinger.gameObject.SetActive(true);
            guideRing.position=guideFrom;guideArrow.position=(guideFrom+guideTo)*.5f;
            Vector3 d=guideTo-guideFrom;guideArrow.localScale=new Vector3(Mathf.Max(.75f,d.magnitude*.78f),.07f,.18f);
            guideArrow.rotation=Quaternion.LookRotation(d.normalized,Vector3.up)*Quaternion.Euler(0,90,0);
        }
        void HideGuide(){guideRing.gameObject.SetActive(false);guideArrow.gameObject.SetActive(false);guideFinger.gameObject.SetActive(false);}

        void Refuse(string message)
        {
            refuseT=0;hintText.text=message;wallFlash.gameObject.SetActive(true);MgfSfx.Play("wrong",.16f);
        }

        void SpawnRipple()
        {
            if(MgfPointer.OnPlane(cam,.82f,out var p)){tapRipple.position=p;tapRipple.gameObject.SetActive(true);rippleT=0;}
        }

        void NudgeSignal(){MgfFx.Punch(signalCargo,.16f,.22f);MgfSfx.Play("tap",.28f);}

        void ResetFeedbackVisuals()
        {
            successT=-1;truck.gameObject.SetActive(false);
            for(int i=0;i<3;i++){pinRenderer[i].sharedMaterial=inkMat;railSegments[i].localRotation=Quaternion.identity;}
            for(int i=0;i<6;i++)cliffSeamRenderers[i].sharedMaterial=inkMat;
            for(int i=0;i<3;i++)stoneShutters[i].position=shutterHome[i];
        }

        void UpdateWorld(float dt)
        {
            float t=worldClock;
            for(int i=0;i<fog.Length;i++)fog[i].localPosition=fogHome[i]+Vector3.right*Mathf.Repeat(t*(.16f+i%3*.035f)+i*.7f,5f);
            liftA.localPosition=new Vector3(-7.4f,1.4f+Mathf.Sin(t*.45f)*1.0f,1.7f);
            liftB.localPosition=new Vector3(7.6f,1.0f+Mathf.Sin(t*.38f+2f)*.8f,5.3f);
            if(signalCargo.gameObject.activeSelf){signalCargo.rotation=Quaternion.Euler(0,Mathf.Sin(t*.8f)*5f,Mathf.Sin(t*1.15f)*2.5f);signalCargo.localScale=Vector3.one*(1f+Mathf.Sin(t*2.2f)*.025f);}
            if(craneHook)craneHook.localScale=new Vector3(1,1+Mathf.Sin(t*.8f)*.02f,1);
            for(int i=0;i<8;i++)if(cargoRoot[i].gameObject.activeSelf)
            {
                cargoRoot[i].position=Vector3.Lerp(cargoRoot[i].position,cargoTarget[i],1f-Mathf.Exp(-dt*12f));
                float idle=cargoQuarantine[i]?0:Mathf.Sin(t*1.4f+i*.8f)*.018f;
                float punch=(i==dragCargo&&movePunchT>0)?Mathf.Sin((.22f-movePunchT)*28f)*.08f:0;
                cargoRoot[i].localScale=new Vector3(1f+punch,1f-punch*.7f+idle,1f+punch);
            }
            if(movePunchT>0)movePunchT-=dt;
            if(guideRing.gameObject.activeSelf)
            {
                float s=1f+Mathf.Sin(t*5f)*.16f;guideRing.localScale=new Vector3(s,.08f,s);
                float q=Mathf.SmoothStep(0,1,Mathf.Repeat(t*.55f,1f));guideFinger.position=Vector3.Lerp(guideFrom,guideTo,q);
            }
            if(rippleT<.5f){rippleT+=dt;float q=rippleT/.5f;tapRipple.localScale=new Vector3(.3f+q*1.8f,.02f,.3f+q*1.8f);if(q>=1)tapRipple.gameObject.SetActive(false);}
            if(refuseT<.5f){refuseT+=dt;wallFlash.gameObject.SetActive(refuseT<.28f);}
            if(successT>=0)
            {
                successT+=dt;
                for(int i=0;i<3;i++)if(successT>.18f+i*.18f)pinRenderer[i].sharedMaterial=cyanGlowMat;
                for(int i=0;i<6;i++)if(successT>.16f+i*.11f)cliffSeamRenderers[i].sharedMaterial=cyanGlowMat;
                for(int i=0;i<3;i++){float q=Mathf.Clamp01((successT-.55f-i*.12f)/.36f);railSegments[i].localRotation=Quaternion.Euler(0,0,-16f*Mathf.SmoothStep(0,1,q));}
                for(int i=0;i<3;i++){float q=Mathf.Clamp01((successT-.36f-i*.12f)/.42f);stoneShutters[i].position=shutterHome[i]+Vector3.up*(1.35f*Mathf.SmoothStep(0,1,q));}
                if(successT>.72f)
                {
                    truck.gameObject.SetActive(true);float q=Mathf.Clamp01((successT-.72f)/.85f);truck.position=Vector3.Lerp(new Vector3(3.1f,1.05f,CellWorld(3,1).z),new Vector3(6.2f,-.05f,-.35f),q*q*(3-2*q));truck.rotation=Quaternion.Euler(0,-8f,-8f*q);
                }
            }
            float aspect=(float)Screen.width/Mathf.Max(1,Screen.height);cam.orthographicSize=aspect<.7f?10.9f:aspect<1.2f?9.5f:7.1f;
        }

        void UpdateUi(float dt)
        {
            scoreDisplay=Mathf.MoveTowards(scoreDisplay,st.score,dt*Mathf.Max(80,Mathf.Abs(st.score-scoreDisplay)*5));
            if(mode==Mode.Playing||mode==Mode.Practice||mode==Mode.Feedback)RefreshText();
            if(mode==Mode.End){endScoreDisplay=Mathf.MoveTowards(endScoreDisplay,st.score,dt*Mathf.Max(120,st.score*2));endStats.text="첫 제출 "+st.firstAttemptCorrect+"/8 (기준 6) · 통과 "+st.solved+"/8\n이동 "+st.moves+"회 · "+Mathf.RoundToInt(endScoreDisplay)+"점";}
        }

        public void TestStart(){BeginRun(false);}
        public void TestAnswerCorrect(){EnsureTestPlayable();SubmitCargo(current.correctIndex);}
        public void TestAnswerWrong()
        {
            EnsureTestPlayable();for(int i=0;i<8;i++)if(cargoActive[i]&&!HwamulRules.IsCorrect(current,i)){SubmitCargo(i);return;}
        }
        void UpdateInputTargets()
        {
            var sb=new StringBuilder();
            for(int i=0;i<8;i++)if(cargoActive[i]&&!cargoQuarantine[i])
            {
                Vector3 p=cam.WorldToScreenPoint(cargoRoot[i].position+Vector3.up*.15f);
                if(sb.Length>0)sb.Append('|');
                sb.Append('c').Append(i).Append(':').Append((p.x/Mathf.Max(1,Screen.width)).ToString("F4",CultureInfo.InvariantCulture)).Append(',').Append((1f-p.y/Mathf.Max(1,Screen.height)).ToString("F4",CultureInfo.InvariantCulture));
            }
            Vector3 dock=cam.WorldToScreenPoint(CellWorld(3,1)+Vector3.right*2.6f+Vector3.up*.15f);
            sb.Append("|dock:").Append((dock.x/Mathf.Max(1,Screen.width)).ToString("F4",CultureInfo.InvariantCulture)).Append(',').Append((1f-dock.y/Mathf.Max(1,Screen.height)).ToString("F4",CultureInfo.InvariantCulture));
            for(int y=0;y<4;y++)for(int x=0;x<4;x++)
            {
                Vector3 p=cam.WorldToScreenPoint(CellWorld(x,y)+Vector3.up*.15f);
                sb.Append("|g").Append(x).Append(y).Append(':').Append((p.x/Mathf.Max(1,Screen.width)).ToString("F4",CultureInfo.InvariantCulture)).Append(',').Append((1f-p.y/Mathf.Max(1,Screen.height)).ToString("F4",CultureInfo.InvariantCulture));
            }
            st.inputTargets=sb.ToString();
        }
        public string StateJson(){UpdateInputTargets();return JsonUtility.ToJson(st);}
        public string ProblemBankJson(){return MgfJson.Bank(bank);}
    }
}
