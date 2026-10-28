using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.YangmalDaetalchul
{
    public sealed class YangmalDaetalchulView
    {
        readonly YangmalDaetalchulGame game;
        readonly Color indigo=MgfLook.Hex("#202C49"), rust=MgfLook.Hex("#B45735"), teal=MgfLook.Hex("#2E6972"), cream=MgfLook.Hex("#F1E3C5"), ink=MgfLook.Hex("#171B23"), brass=MgfLook.Hex("#C99A48");
        readonly List<Transform> drums=new List<Transform>(), ambientSocks=new List<Transform>(), rescuedSocks=new List<Transform>(), fluffTrail=new List<Transform>();
        Camera cam; Transform world, clothesline, activeSock, handle3d, chute3d; float clock, split, revealAnim, wireKick, sockFlight, invalidGuideClock; bool wide, titleSplitting, correctReveal, practice, flying;
        Vector2 invalidGuidePoint;
        Vector3 sockHome, chuteHome;
        Canvas canvas; CanvasScaler scaler; RectTransform canvasRt;
        CanvasGroup titleGroup, playGroup, revealGroup, endGroup, guideGroup;
        RectTransform titleStart, curtainL, curtainR, diagramRt, promptRt, goalRt, rulerRt, handleRt, handlePulseRt, guideHandRt, guideTargetRt, invalidRippleRt, revealCardRt;
        TMP_Text hudRound,hudTime,hudLives,hudFirst,prompt,goal,valueText,feedback,evidence,endTitle,endStats,bestText,stageLabel;
        readonly Image[] diagramLines=new Image[12], diagramDots=new Image[8], tickMarks=new Image[24];
        readonly TMP_Text[] diagramLabels=new TMP_Text[16], tickLabels=new TMP_Text[6];
        Image circle, circleInner, handleImage, chuteFill, screenFlash;
        readonly Image[] guideDots=new Image[7];
        int lastW,lastH,lastSecond=-1,shownLength=-1; SockProblem shown;

        static Sprite softSprite;
        static Sprite SoftSprite { get { if(!softSprite) softSprite=Sprite.Create(MgfLook.SoftDot,new Rect(0,0,64,64),new Vector2(.5f,.5f),64); return softSprite; } }
        public Camera Camera=>cam;

        public YangmalDaetalchulView(YangmalDaetalchulGame game){this.game=game;BuildWorld();BuildUi();Relayout(true);}

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#111827"),indigo,MgfLook.Hex("#090D17"),.88f);
            MgfLook.Sun(new Vector3(42,-34,-16),new Color(1f,.78f,.51f),1.12f,.5f);
            cam=MgfLook.Camera(new Vector3(0,9.5f,-22f),new Vector3(0,3.2f,3f),34f);cam.orthographic=true;cam.orthographicSize=9.3f;cam.backgroundColor=indigo;
            world=new GameObject("IndigoVerticalTextileAlley").transform;
            var cloth=MgfLook.Lit(MgfLook.Hex("#273554"),.12f,.01f); var trim=MgfLook.Lit(rust,.2f,.03f); var steel=MgfLook.Lit(MgfLook.Hex("#777B82"),.34f,.72f); var glass=MgfLook.Lit(MgfLook.Hex("#214E59"),.68f,.12f,MgfLook.Hex("#102C37"));
            MgfLook.Prim(PrimitiveType.Cube,"DeepTextileVoid",new Vector3(0,4.2f,6.4f),new Vector3(22,14,.55f),cloth,world,false);
            BuildAlleyFrames(steel,cloth);
            for(int floor=0;floor<3;floor++)
            {
                float y=1.3f+floor*3.7f,x=floor==1?1.65f:-1.35f;
                MgfLook.Prim(PrimitiveType.Cube,"LoomFrame"+(floor+1),new Vector3(x,y,4.95f),new Vector3(7.2f,3.25f,.72f),steel,world,false);
                MgfLook.Prim(PrimitiveType.Cube,"HangingCloth",new Vector3(x,y,4.38f),new Vector3(4.25f,2.55f,.28f),trim,world,false);
                var drum=MgfLook.Prim(PrimitiveType.Cylinder,"TurningDrum",new Vector3(x,y,4.13f),new Vector3(1.72f,.13f,1.72f),glass,world,false).transform;drum.rotation=Quaternion.Euler(90,0,0);drums.Add(drum);
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;MgfLook.Prim(PrimitiveType.Cube,"IronStitch",new Vector3(x+Mathf.Cos(a)*1.38f,y+Mathf.Sin(a)*1.05f,3.9f),Vector3.one*.13f,steel,world,false);}
                for(int i=-4;i<=4;i++) Beam("WarpThread",new Vector3(x+i*.42f,y-1.08f,3.95f),new Vector3(x+i*.42f,y+1.08f,3.95f),.022f,i%2==0?MgfLook.Lit(cream,.1f):MgfLook.Lit(teal,.08f));
                MgfLook.Prim(PrimitiveType.Cube,"MeshDrawer",new Vector3(x+2.75f,y-.55f,4.28f),new Vector3(1.05f,.72f,.32f),trim,world,false);
            }
            BuildPipes(steel);BuildProps();BuildClothesline();
            chute3d=MgfLook.Prim(PrimitiveType.Cube,"WovenEscapeBridge",new Vector3(1.8f,.45f,2.8f),new Vector3(5.2f,.18f,1.2f),MgfLook.Lit(MgfLook.Hex("#D6B45F"),.16f,.03f),world,false).transform;chuteHome=chute3d.position;
            for(int i=-5;i<=5;i++) Beam("BridgeWeft",new Vector3(-.8f+i*.48f,.55f,2.22f),new Vector3(-.8f+i*.48f,.55f,3.38f),.025f,MgfLook.Lit(rust,.12f));
            handle3d=MgfLook.Prim(PrimitiveType.Cube,"BrassWeaverHandle",new Vector3(4.55f,.65f,2.45f),new Vector3(1.05f,.7f,.72f),MgfLook.Lit(brass,.3f,.42f,MgfLook.Hex("#6B4918")),world,false).transform;
            activeSock=BuildSock(new Vector3(-1.15f,1.05f,2.25f),1.0f,0);sockHome=activeSock.position;
            for(int i=0;i<7;i++){var f=MgfLook.Prim(PrimitiveType.Capsule,"LintMote",new Vector3(-7+i*2.2f,2+(i%3)*2.1f,2.3f),Vector3.one*(.07f+(i%2)*.03f),MgfLook.Lit(cream,.12f),world,false).transform;fluffTrail.Add(f);}
        }

        void BuildAlleyFrames(Material steel,Material cloth)
        {
            for(int lane=-2;lane<=2;lane++)
            {
                float x=lane*4.3f;
                Beam("VerticalIronRail",new Vector3(x,-2f,5.45f),new Vector3(x,11.4f,5.45f),.09f,steel);
                if(lane<2) for(int row=0;row<7;row++)
                {
                    float y=-.7f+row*2f;
                    var panel=MgfLook.Prim(PrimitiveType.Cube,"FoldedLaundryPanel",new Vector3(x+2.12f,y,5.18f+(row%2)*.18f),new Vector3(3.65f,1.05f,.18f),row%3==0?MgfLook.Lit(teal,.1f):row%3==1?MgfLook.Lit(MgfLook.Hex("#8A5B48"),.1f):cloth,world,false);
                    panel.transform.rotation=Quaternion.Euler(0,row%2==0?7:-7,lane%2==0?2:-2);
                }
            }
            for(int row=0;row<6;row++) Beam("CrossAlleyCable",new Vector3(-9.2f,.1f+row*2.2f,5.1f),new Vector3(9.2f,.55f+row*2.2f,4.8f),.035f,row%2==0?MgfLook.Lit(brass,.2f):steel);
        }

        void BuildPipes(Material m)
        {
            Beam("PipeTop",new Vector3(-9,10.2f,4.6f),new Vector3(9,10.2f,4.6f),.18f,m);
            for(int i=0;i<4;i++){float x=-7.8f+i*5.1f;Beam("BentPipe",new Vector3(x,10.2f,4.6f),new Vector3(x,8.9f-(i%2),4.6f),.15f,m);}
        }
        void BuildProps()
        {
            var mesh=MgfLook.Lit(MgfLook.Hex("#6E737C"),.24f,.55f);var yarn=MgfLook.Lit(cream,.12f,.01f);
            for(int i=0;i<4;i++){float x=-7.3f+i*4.8f;Beam("BasketTop",new Vector3(x-1.1f,.38f,4.1f),new Vector3(x+1.1f,.38f,4.1f),.07f,mesh);Beam("BasketBottom",new Vector3(x-.9f,-.68f,4.1f),new Vector3(x+.9f,-.68f,4.1f),.07f,mesh);for(int k=-2;k<=2;k++)Beam("BasketWire",new Vector3(x+k*.42f,-.68f,4.1f),new Vector3(x+k*.5f,.38f,4.1f),.04f,mesh);}
            for(int i=0;i<8;i++){var roll=MgfLook.Prim(PrimitiveType.Cylinder,"ClothRoll",new Vector3(-8.7f+i*2.35f,7.85f+(i%2)*.25f,3.9f),new Vector3(.36f,.48f,.36f),i%2==0?yarn:MgfLook.Lit(rust,.12f),world,false);roll.transform.rotation=Quaternion.Euler(0,0,90);}
        }
        void BuildClothesline()
        {
            clothesline=new GameObject("EscapeClothesline").transform;clothesline.SetParent(world,false);
            Beam("Clothesline",new Vector3(-8.8f,8.8f,2.6f),new Vector3(8.8f,8.15f,2.6f),.055f,MgfLook.Lit(brass,.22f,.48f));
            for(int i=0;i<9;i++){float x=-7.8f+i*1.95f;MgfLook.Prim(PrimitiveType.Cube,"IronPeg",new Vector3(x,8.55f-i*.035f,2.35f),new Vector3(.2f,.72f,.25f),MgfLook.Lit(MgfLook.Hex("#777B82"),.3f,.62f),world,false);}
            for(int i=0;i<9;i++){var s=BuildSock(new Vector3(-7.8f+i*1.95f,7.45f-i*.035f,2.25f),.58f,i);s.gameObject.SetActive(false);rescuedSocks.Add(s);}
        }
        Transform BuildSock(Vector3 pos,float scale,int style)
        {
            var root=new GameObject("KnittedSockCreature").transform;root.SetParent(world,false);root.position=pos;root.localScale=Vector3.one*scale;
            var yarn=MgfLook.Lit(style%3==0?MgfLook.Hex("#E9D8B8"):style%3==1?MgfLook.Hex("#A9C7C2"):MgfLook.Hex("#C98770"),.1f);
            MgfLook.Prim(PrimitiveType.Cube,"SockAnkle",new Vector3(-.25f,.38f,0),new Vector3(.82f,1.5f,.62f),yarn,root,false);
            MgfLook.Prim(PrimitiveType.Cube,"SockFoot",new Vector3(.32f,-.15f,-.03f),new Vector3(1.65f,.72f,.62f),yarn,root,false);
            for(int i=0;i<4;i++) MgfLook.Prim(PrimitiveType.Cube,"WovenRib",new Vector3(-.25f,-.1f+i*.32f,-.34f),new Vector3(.88f,.055f,.035f),MgfLook.Lit(i%2==0?rust:teal,.08f),root,false);
            var black=MgfLook.Lit(ink,.55f);MgfLook.Prim(PrimitiveType.Sphere,"StitchEyeL",new Vector3(-.46f,.62f,-.34f),Vector3.one*.11f,black,root,false);MgfLook.Prim(PrimitiveType.Sphere,"StitchEyeR",new Vector3(-.12f,.62f,-.34f),Vector3.one*.11f,black,root,false);
            MgfLook.Prim(PrimitiveType.Sphere,"Button",new Vector3(-.29f,.23f,-.35f),Vector3.one*.14f,MgfLook.Lit(style%2==0?rust:teal,.55f),root,false);ambientSocks.Add(root);return root;
        }
        GameObject Beam(string name,Vector3 a,Vector3 b,float radius,Material mat)
        {Vector3 d=b-a;var o=MgfLook.Prim(PrimitiveType.Cylinder,name,(a+b)*.5f,new Vector3(radius,d.magnitude*.5f,radius),mat,world,false);o.transform.rotation=Quaternion.FromToRotation(Vector3.up,d.normalized);return o;}

        void BuildUi()
        {
            canvas=MgfText.Canvas;canvasRt=(RectTransform)canvas.transform;scaler=canvas.GetComponent<CanvasScaler>();
            titleGroup=Group("TitleWorldOverlay");playGroup=Group("PlayLabels");revealGroup=Group("RevealLabel");endGroup=Group("EndBanner");guideGroup=Group("GuidePath");
            BuildTitleUi();BuildPlayUi();BuildRevealUi();BuildEndUi();
            screenFlash=Img("LightWash",canvasRt,new Color(1f,.92f,.5f,0),new Vector2(2000,1600),new Vector2(.5f,.5f),Vector2.zero,true);
        }
        CanvasGroup Group(string name){var go=new GameObject(name,typeof(RectTransform),typeof(CanvasGroup));go.transform.SetParent(canvasRt,false);var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return go.GetComponent<CanvasGroup>();}
        Image Img(string name,Transform parent,Color color,Vector2 size,Vector2 anchor,Vector2 pos,bool soft=false){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;var im=go.GetComponent<Image>();im.color=color;im.raycastTarget=false;if(soft)im.sprite=SoftSprite;return im;}
        TMP_Text Txt(string name,Transform parent,string text,float size,Color color,Vector2 box,Vector2 anchor,Vector2 pos,TextAlignmentOptions align=TextAlignmentOptions.Center){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.sizeDelta=box;r.anchoredPosition=pos;var t=go.GetComponent<TextMeshProUGUI>();t.font=MgfText.Font;t.fontSize=size;t.fontStyle=FontStyles.Bold;t.color=color;t.alignment=align;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;t.text=text;return t;}

        void BuildTitleUi()
        {
            var banner=Img("LaundryClothBanner",titleGroup.transform,cream,new Vector2(360,126),new Vector2(.5f,.77f),Vector2.zero);
            Txt("LogoShadow",banner.transform,"양말 대탈출",43,new Color(.15f,.09f,.08f,.36f),new Vector2(350,70),new Vector2(.5f,.5f),new Vector2(3,-4));
            Txt("Logo",banner.transform,"양말 대탈출",43,rust,new Vector2(350,70),new Vector2(.5f,.5f),Vector2.zero);
            Txt("Tagline",banner.transform,"미끄럼틀을 늘려 양말을 내보내라",16,ink,new Vector2(340,38),new Vector2(.5f,.14f),Vector2.zero);
            var start=Img("WasherDoorStart",titleGroup.transform,new Color(.18f,.34f,.42f,.97f),new Vector2(150,150),new Vector2(.5f,.31f),Vector2.zero,true);titleStart=start.rectTransform;
            Img("IronSpoolRing",start.transform,rust,new Vector2(128,128),new Vector2(.5f,.5f),Vector2.zero,true);
            Img("WovenSpool",start.transform,teal,new Vector2(94,94),new Vector2(.5f,.5f),Vector2.zero,true);
            Txt("DoorVerb",start.transform,"문을 눌러\n구조 시작",17,cream,new Vector2(110,65),new Vector2(.5f,.5f),Vector2.zero);
            bestText=Txt("Best",titleGroup.transform,"최고 첫 시도 0/9",14,cream,new Vector2(260,30),new Vector2(.5f,.16f),Vector2.zero);
            Txt("Grade",titleGroup.transform,"중학교 3학년 · 원의 접선",13,cream,new Vector2(280,28),new Vector2(.5f,.08f),Vector2.zero);
            curtainL=Img("SplitClothLeft",titleGroup.transform,rust,new Vector2(220,900),new Vector2(0,.5f),new Vector2(105,0)).rectTransform;
            curtainR=Img("SplitClothRight",titleGroup.transform,teal,new Vector2(220,900),new Vector2(1,.5f),new Vector2(-105,0)).rectTransform;
            curtainL.gameObject.SetActive(false);curtainR.gameObject.SetActive(false);
        }

        void BuildPlayUi()
        {
            var hud=Img("CreamMachineLabel",playGroup.transform,new Color(.96f,.9f,.76f,.98f),new Vector2(374,58),new Vector2(.5f,1),new Vector2(0,-31));
            hudRound=Txt("Round",hud.transform,"연습",15,ink,new Vector2(90,36),new Vector2(.12f,.5f),Vector2.zero);
            hudTime=Txt("Time",hud.transform,"90초",15,ink,new Vector2(74,36),new Vector2(.34f,.5f),Vector2.zero);
            hudLives=Txt("Lives",hud.transform,"실 ●●●",14,rust,new Vector2(88,36),new Vector2(.59f,.5f),Vector2.zero);
            hudFirst=Txt("First",hud.transform,"첫 0/9\n통과 7",12,teal,new Vector2(84,48),new Vector2(.78f,.5f),Vector2.zero);
            var g=Img("GoalFabricLabel",playGroup.transform,cream,new Vector2(374,42),new Vector2(.5f,1),new Vector2(0,-83));goalRt=g.rectTransform;goal=Txt("Goal",g.transform,"길이에 맞게 늘려 양말을 내보내라",14,ink,new Vector2(360,36),new Vector2(.5f,.5f),Vector2.zero);
            diagramRt=(RectTransform)new GameObject("WasherGlassDiagram",typeof(RectTransform)).transform;diagramRt.SetParent(playGroup.transform,false);
            Img("DiagramGlass",diagramRt,new Color(.12f,.27f,.32f,.93f),new Vector2(360,305),new Vector2(.5f,.5f),Vector2.zero,true);
            circle=Img("CircleOutline",diagramRt,new Color(.96f,.9f,.7f,.92f),new Vector2(150,150),new Vector2(.5f,.5f),Vector2.zero,true);
            circleInner=Img("CircleInterior",diagramRt,new Color(.12f,.27f,.32f,.98f),new Vector2(142,142),new Vector2(.5f,.5f),Vector2.zero,true);
            for(int i=0;i<diagramLines.Length;i++) diagramLines[i]=Img("GeometryLine"+i,diagramRt,cream,new Vector2(120,5),new Vector2(.5f,.5f),Vector2.zero);
            for(int i=0;i<diagramDots.Length;i++) diagramDots[i]=Img("Contact"+i,diagramRt,i<3?MgfLook.Hex("#FFE15B"):cream,new Vector2(12,12),new Vector2(.5f,.5f),Vector2.zero,true);
            string chars="ABCDEFOP?I";for(int i=0;i<diagramLabels.Length;i++)diagramLabels[i]=Txt("DiagramLabel"+i,diagramRt,i<chars.Length?chars[i].ToString():"",18,cream,new Vector2(116,34),new Vector2(.5f,.5f),Vector2.zero);
            stageLabel=Txt("StageTag",diagramRt,"1단계 · 같은 점",13,MgfLook.Hex("#FFE15B"),new Vector2(220,28),new Vector2(.5f,.91f),Vector2.zero);
            var pb=Img("PromptCreamStrip",playGroup.transform,new Color(.98f,.94f,.83f,.99f),new Vector2(374,104),new Vector2(.5f,.5f),Vector2.zero);promptRt=pb.rectTransform;
            prompt=Txt("Prompt",pb.transform,"",15,ink,new Vector2(410,102),new Vector2(.5f,.5f),Vector2.zero);
            rulerRt=(RectTransform)new GameObject("FoldedMeasureRibbon",typeof(RectTransform)).transform;rulerRt.SetParent(playGroup.transform,false);
            Img("RulerBacking",rulerRt,new Color(.45f,.18f,.21f,.96f),new Vector2(82,350),new Vector2(.5f,.5f),Vector2.zero);
            chuteFill=Img("ChuteLength",rulerRt,new Color(1f,.82f,.27f,.82f),new Vector2(25,1),new Vector2(.42f,0),Vector2.zero);chuteFill.rectTransform.pivot=new Vector2(.5f,0);
            for(int i=0;i<24;i++){tickMarks[i]=Img("Tick"+(i+1),rulerRt,cream,new Vector2(i%4==3?28:17,2),new Vector2(.68f,.5f),Vector2.zero);}
            for(int i=0;i<6;i++)tickLabels[i]=Txt("TickLabel"+(i+1),rulerRt,((i+1)*4).ToString(),12,cream,new Vector2(32,20),new Vector2(.24f,.5f),Vector2.zero);
            handleImage=Img("YellowRubberHandle",rulerRt,MgfLook.Hex("#FFE15B"),new Vector2(68,68),new Vector2(.5f,0),Vector2.zero,true);handleRt=handleImage.rectTransform;
            handleRt.anchorMin=handleRt.anchorMax=new Vector2(.5f,.5f);
            valueText=Txt("HandleValue",handleRt.transform,"0 cm",15,ink,new Vector2(72,30),new Vector2(.5f,.5f),Vector2.zero);
            handlePulseRt=Img("HandlePulse",guideGroup.transform,new Color(1f,.88f,.3f,.42f),new Vector2(92,92),new Vector2(.5f,.5f),Vector2.zero,true).rectTransform;
            invalidRippleRt=Img("InvalidInputRipple",guideGroup.transform,new Color(.96f,.35f,.22f,.72f),new Vector2(54,54),new Vector2(.5f,.5f),Vector2.zero,true).rectTransform;
            guideHandRt=(RectTransform)new GameObject("GhostFinger",typeof(RectTransform)).transform;guideHandRt.SetParent(guideGroup.transform,false);guideHandRt.anchorMin=guideHandRt.anchorMax=new Vector2(.5f,.5f);guideHandRt.sizeDelta=new Vector2(62,78);
            Img("FingerPalm",guideHandRt,rust,new Vector2(42,34),new Vector2(.5f,.26f),Vector2.zero,true);
            Img("PointingFinger",guideHandRt,rust,new Vector2(19,58),new Vector2(.62f,.67f),Vector2.zero,true);
            Img("FingerThumb",guideHandRt,rust,new Vector2(25,15),new Vector2(.28f,.34f),Vector2.zero,true);
            guideTargetRt=Img("SixCmTarget",guideGroup.transform,new Color(1f,.76f,.22f,.22f),new Vector2(86,86),new Vector2(.5f,.5f),Vector2.zero,true).rectTransform;
            for(int i=0;i<guideDots.Length;i++) guideDots[i]=Img("GuideDash"+i,guideGroup.transform,new Color(1f,.9f,.58f,.76f),new Vector2(9,9),new Vector2(.5f,.5f),Vector2.zero,true);
            feedback=Txt("Feedback",playGroup.transform,"",14,cream,new Vector2(350,42),new Vector2(.5f,.13f),Vector2.zero);
        }

        void BuildRevealUi(){var c=Img("EvidenceCloth",revealGroup.transform,new Color(.98f,.92f,.77f,.98f),new Vector2(360,108),new Vector2(.5f,.16f),Vector2.zero);revealCardRt=c.rectTransform;evidence=Txt("Evidence",c.transform,"",15,ink,new Vector2(340,94),new Vector2(.5f,.5f),Vector2.zero);}
        void BuildEndUi(){var p=Img("ResultLaundryBanner",endGroup.transform,cream,new Vector2(360,270),new Vector2(.5f,.5f),Vector2.zero);endTitle=Txt("EndTitle",p.transform,"",35,rust,new Vector2(340,70),new Vector2(.5f,.78f),Vector2.zero);endStats=Txt("EndStats",p.transform,"",18,ink,new Vector2(330,110),new Vector2(.5f,.47f),Vector2.zero);Txt("Restart",p.transform,"세탁기 문을 눌러 다시 시작",16,teal,new Vector2(300,40),new Vector2(.5f,.13f),Vector2.zero);}

        public void ShowTitle(){Set(titleGroup,true);Set(playGroup,false);Set(revealGroup,false);Set(endGroup,false);Set(guideGroup,false);titleSplitting=false;curtainL.gameObject.SetActive(false);curtainR.gameObject.SetActive(false);bestText.text="최고 첫 시도 "+PlayerPrefs.GetInt("yangmal-best",0)+"/9";ResetRunWorld();}
        public void BeginSplit(){titleSplitting=true;split=0;curtainL.gameObject.SetActive(true);curtainR.gameObject.SetActive(true);}
        public void ShowPractice(){Set(titleGroup,false);Set(playGroup,true);Set(revealGroup,false);Set(endGroup,false);Set(guideGroup,true);invalidGuideClock=0;invalidRippleRt.gameObject.SetActive(false);goal.text="노란 손잡이를 6 cm까지 끌어 놓으시오";hudRound.text="연습";hudTime.text="시계 정지";feedback.text="화면을 위로 끌어 6 cm 흡착을 연습하시오";}
        public void ShowRun(){Set(titleGroup,false);Set(playGroup,true);Set(revealGroup,false);Set(endGroup,false);Set(guideGroup,false);invalidGuideClock=0;goal.text="길이에 맞게 늘려 양말을 내보내라";}
        public void LoadProblem(SockProblem p,bool isPractice){shown=p;practice=isPractice;shownLength=-1;Set(revealGroup,false);SetLength(0);DrawDiagram(p);UpdatePrompt(p);feedback.text=isPractice?"손잡이를 실제로 끌어 연습을 끝내시오":"도형과 치수의 축척은 다릅니다";activeSock.position=sockHome;activeSock.localScale=Vector3.one;flying=false;sockFlight=0;}

        void UpdatePrompt(SockProblem p){prompt.text=p.prompt;stageLabel.text=p.band==1?"1단계 · 같은 점 P":p.band==2?"2단계 · 전체에서 부분 빼기":"3단계 · 잘못된 기록 수리";}
        void DrawDiagram(SockProblem p)
        {
            for(int i=0;i<diagramLines.Length;i++)diagramLines[i].gameObject.SetActive(false);for(int i=0;i<diagramDots.Length;i++)diagramDots[i].gameObject.SetActive(false);for(int i=0;i<diagramLabels.Length;i++)diagramLabels[i].gameObject.SetActive(false);
            if(p.band==1)
            {
                Vector2 O=new Vector2(-35,0),P=new Vector2(105,0),A,B;const float radius=69f;
                TangentPoints(O,radius,P,out A,out B);ValidateTangentDiagram(O,radius,P,A,B);
                SetCircle(O,radius);
                Line(0,A,P,cream);Line(1,B,P,cream);Line(2,O,A,new Color(.8f,.9f,.95f,.9f));Line(3,O,B,new Color(.8f,.9f,.95f,.9f));
                Dot(0,P);Dot(1,A);Dot(2,B);Label(0,"P",P+new Vector2(21,0));Label(1,"A",A+new Vector2(-15,-13));Label(2,"B",B+new Vector2(-15,13));Label(3,"O",O);Label(4,"PA="+p.answerCm+" cm",new Vector2(45,-73));Label(5,"PB=?",new Vector2(45,73));Label(6,"⊥",A+new Vector2(14,11));Label(7,"⊥",B+new Vector2(14,-11));
            }
            else
            {
                Vector2 A=new Vector2(0,112),B=new Vector2(-132,-90),C=new Vector2(136,-90);
                float sideA=Vector2.Distance(B,C),sideB=Vector2.Distance(C,A),sideC=Vector2.Distance(A,B);
                Vector2 I=(sideA*A+sideB*B+sideC*C)/(sideA+sideB+sideC);
                Vector2 D=PerpendicularFoot(I,A,B),E=PerpendicularFoot(I,B,C),F=PerpendicularFoot(I,C,A);float radius=Vector2.Distance(I,D);
                ValidateIncircleDiagram(A,B,C,I,D,E,F,radius);
                SetCircle(I,radius);
                Line(0,A,B,cream);Line(1,B,C,cream);Line(2,C,A,cream);Line(3,D,I,new Color(.75f,.88f,.94f,.8f));Line(4,E,I,new Color(.75f,.88f,.94f,.8f));Line(5,F,I,new Color(.75f,.88f,.94f,.8f));
                Dot(0,D);Dot(1,E);Dot(2,F);Dot(3,I);Label(0,"A",A+new Vector2(0,17));Label(1,"B",B+new Vector2(-16,-4));Label(2,"C",C+new Vector2(16,-4));Label(3,"D",D+new Vector2(-14,10));Label(4,"E",E+new Vector2(0,-17));Label(5,"F",F+new Vector2(14,10));Label(13,"I",I+new Vector2(0,16));
                if(p.band==2){Label(6,"AB="+(p.x+p.y)+" cm",new Vector2(-116,53));Label(7,"BD="+p.y+" cm",new Vector2(-108,-68));Label(8,"AF=?",new Vector2(78,58));}
                else{Label(6,"AB="+(p.x+p.y),new Vector2(-116,53));Label(7,"BC="+(p.y+p.z),new Vector2(0,-112));Label(8,"CF="+p.z,new Vector2(112,-25));Label(9,"AF="+p.z+"?",new Vector2(78,58));}
                Label(10,"⊥",D+new Vector2(13,-2));Label(11,"⊥",E+new Vector2(12,12));Label(12,"⊥",F+new Vector2(-13,-2));
            }
        }
        static void TangentPoints(Vector2 center,float radius,Vector2 external,out Vector2 lower,out Vector2 upper)
        {
            Vector2 v=external-center;float d2=v.sqrMagnitude;if(d2<=radius*radius)throw new InvalidOperationException("접선 도식: 점 P가 원 밖에 있지 않습니다");
            Vector2 basePoint=center+v*(radius*radius/d2),perp=new Vector2(-v.y,v.x)*(radius*Mathf.Sqrt(d2-radius*radius)/d2);
            upper=basePoint+perp;lower=basePoint-perp;
        }
        static Vector2 PerpendicularFoot(Vector2 p,Vector2 a,Vector2 b){Vector2 ab=b-a;return a+ab*(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);}
        static void ValidateTangentDiagram(Vector2 o,float radius,Vector2 p,Vector2 a,Vector2 b)
        {
            RequireNear("OA=반지름",Vector2.Distance(o,a),radius,.02f);RequireNear("OB=반지름",Vector2.Distance(o,b),radius,.02f);
            RequireNear("OA⊥PA",Vector2.Dot(a-o,p-a),0,.08f);RequireNear("OB⊥PB",Vector2.Dot(b-o,p-b),0,.08f);
        }
        static void ValidateIncircleDiagram(Vector2 a,Vector2 b,Vector2 c,Vector2 i,Vector2 d,Vector2 e,Vector2 f,float radius)
        {
            RequireOnSegment("D∈AB",d,a,b);RequireOnSegment("E∈BC",e,b,c);RequireOnSegment("F∈CA",f,c,a);
            RequireNear("ID=반지름",Vector2.Distance(i,d),radius,.02f);RequireNear("IE=반지름",Vector2.Distance(i,e),radius,.02f);RequireNear("IF=반지름",Vector2.Distance(i,f),radius,.02f);
            RequireNear("ID⊥AB",Vector2.Dot(i-d,b-a),0,.08f);RequireNear("IE⊥BC",Vector2.Dot(i-e,c-b),0,.08f);RequireNear("IF⊥CA",Vector2.Dot(i-f,a-c),0,.08f);
        }
        static void RequireOnSegment(string label,Vector2 p,Vector2 a,Vector2 b){Vector2 ab=b-a;float t=Vector2.Dot(p-a,ab)/ab.sqrMagnitude;RequireNear(label+" 직선",Cross(ab,p-a),0,.08f);if(t<0||t>1)throw new InvalidOperationException("내접원 도식 검증 실패: "+label);}
        static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
        static void RequireNear(string label,float actual,float expected,float tolerance){if(Mathf.Abs(actual-expected)>tolerance)throw new InvalidOperationException("도형 좌표 검증 실패: "+label+" ("+actual+" vs "+expected+")");}
        void SetCircle(Vector2 center,float radius){circle.gameObject.SetActive(true);circleInner.gameObject.SetActive(true);circle.rectTransform.sizeDelta=Vector2.one*(radius*2);circle.rectTransform.anchoredPosition=center;circleInner.rectTransform.sizeDelta=Vector2.one*Mathf.Max(2,radius*2-8);circleInner.rectTransform.anchoredPosition=center;}
        void Line(int i,Vector2 a,Vector2 b,Color c){var im=diagramLines[i];im.gameObject.SetActive(true);im.color=c;var d=b-a;im.rectTransform.sizeDelta=new Vector2(d.magnitude,5);im.rectTransform.anchoredPosition=(a+b)*.5f;im.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);}
        void Dot(int i,Vector2 p){diagramDots[i].gameObject.SetActive(true);diagramDots[i].rectTransform.anchoredPosition=p;}
        void Label(int i,string s,Vector2 p){diagramLabels[i].gameObject.SetActive(true);diagramLabels[i].text=s;diagramLabels[i].rectTransform.anchoredPosition=p;}

        public void SetLength(int cm){shownLength=cm;valueText.text=cm+" cm";float t=cm<=0?0:(cm-1)/23f;float h=292f;handleRt.anchoredPosition=new Vector2(0,-146f+t*h);chuteFill.rectTransform.sizeDelta=new Vector2(25,Mathf.Max(1,t*h));UpdateInputState();}
        public void HandlePressed(){handleImage.color=MgfLook.Hex("#FFF1A2");MgfFx.Punch(handleRt,.13f,.18f);MgfSfx.Play("tap",.2f);}
        public void HandleReleased(){handleImage.color=MgfLook.Hex("#FFE15B");}
        public bool HitHandle(Vector2 screen){Vector2 p=RectTransformUtility.WorldToScreenPoint(null,handleRt.position);return Vector2.Distance(screen,p)<=Mathf.Max(48,handleRt.rect.width*.65f*canvas.scaleFactor);}
        public bool HitRulerGrab(Vector2 screen)
        {
            Vector2 local;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rulerRt,screen,null,out local)) return false;
            return Mathf.Abs(local.x)<=64f&&local.y>=-188f&&local.y<=188f;
        }
        public int LengthFromScreen(Vector2 screen)
        {
            Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(rulerRt,screen,null,out local);float t=Mathf.InverseLerp(-146,146,local.y);return Mathf.Clamp(Mathf.RoundToInt(t*23)+1,1,24);
        }
        void UpdateInputState(){Vector2 hp=RectTransformUtility.WorldToScreenPoint(null,handleRt.position);Vector2 lo=RectTransformUtility.WorldToScreenPoint(null,rulerRt.TransformPoint(new Vector3(0,-146,0)));Vector2 hi=RectTransformUtility.WorldToScreenPoint(null,rulerRt.TransformPoint(new Vector3(0,146,0)));game.GameState.handleX=Mathf.RoundToInt(hp.x);game.GameState.handleY=Mathf.RoundToInt(hp.y);game.GameState.rulerMin=Mathf.RoundToInt(lo.y);game.GameState.rulerMax=Mathf.RoundToInt(hi.y);game.GameState.layout=wide?"land":"portrait";}

        public void RefuseAt(Vector2 screen,string msg){feedback.text=msg+" · 빛나는 손가락을 따라 끌어 보시오";RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt,screen,null,out invalidGuidePoint);invalidGuideClock=1.2f;invalidRippleRt.gameObject.SetActive(true);invalidRippleRt.anchoredPosition=invalidGuidePoint;var rippleColor=invalidRippleRt.GetComponent<Image>().color;rippleColor.a=.72f;invalidRippleRt.GetComponent<Image>().color=rippleColor;handlePulseRt.anchoredPosition=canvasRt.InverseTransformPoint(handleRt.position);handlePulseRt.localScale=Vector3.one*.7f;Set(guideGroup,true);MgfFx.Punch(handleRt,.1f,.22f);MgfSfx.Play("tap",.14f);}
        public void PracticeMiss(int value,int answer){feedback.text=value+" cm는 연습 길이와 다릅니다 · PA와 PB를 다시 비교하시오";correctReveal=false;SetLength(0);}
        public void Reveal(bool correct,int value,SockProblem p,bool tutorial)
        {
            correctReveal=correct;revealAnim=0;invalidGuideClock=0;Set(revealGroup,true);Set(guideGroup,false);evidence.text=correct?p.evidence:WrongMessage(p,value)+"\n정답 상태: "+p.evidence;screenFlash.color=correct?new Color(1f,.87f,.35f,.32f):new Color(.45f,.16f,.18f,.22f);
            flying=true;sockFlight=0;wireKick=correct?1f:0;activeSock.localScale=correct?new Vector3(1.18f,.82f,1):new Vector3(.86f,1.08f,1);
            if(correct){int idx=Mathf.Clamp(game.GameState.rescued-1,0,8);rescuedSocks[idx].gameObject.SetActive(true);MgfFx.Glow(new Vector3(-5+idx*1.2f,7.4f,2),MgfLook.Hex("#FFE26A"),10,.3f);}else MgfFx.Burst(new Vector3(2,.4f,2),rust,10,1.8f,.12f);
        }
        string WrongMessage(SockProblem p,int value){string id=SockRules.Diagnose(p,value);if(id=="center_distance")return "접선의 길이는 점 P에서 접점까지입니다";if(id=="whole_side")return "전체 변과 꼭짓점에서 접점까지의 부분을 구분하시오";if(id=="different_vertex")return "접선은 같은 꼭짓점에서 시작한 두 선분끼리 비교합니다";if(id=="stopped_midway")return "점 B의 중간 길이에서 멈추지 말고 점 A까지 연결하시오";return value<p.answerCm?"미끄럼틀이 짧아 안전 주머니에 떨어졌습니다":"미끄럼틀이 길어 집게를 지나쳤습니다";}
        public void ResumeRepair(){Set(revealGroup,false);feedback.text="이전 보풀 자국과 도형을 비교해 길이를 수리하시오";SetLength(0);activeSock.position=sockHome;activeSock.localScale=Vector3.one;}
        public void ShowEnd(bool clear,string reason)
        {
            invalidGuideClock=0;Set(playGroup,false);Set(revealGroup,false);Set(endGroup,true);Set(guideGroup,false);endTitle.text=clear?"아홉 양말 탈출":"안전 주머니 닫힘";endStats.text="탈출 "+game.GameState.rescued+"/9\n첫 시도 "+game.GameState.firstCorrect+"/9 · 통과 7\n"+reason;int best=Mathf.Max(PlayerPrefs.GetInt("yangmal-best",0),game.GameState.firstCorrect);PlayerPrefs.SetInt("yangmal-best",best);PlayerPrefs.Save();
        }

        public void Tick(float dt)
        {
            clock+=dt;if(Screen.width!=lastW||Screen.height!=lastH)Relayout(false);
            for(int i=0;i<drums.Count;i++)drums[i].Rotate(0,0,dt*(i%2==0?13:-11));
            for(int i=0;i<ambientSocks.Count;i++)if(ambientSocks[i]&&ambientSocks[i].gameObject.activeSelf){var p=ambientSocks[i].localPosition;p.y+=Mathf.Sin(clock*1.8f+i*.7f)*.0018f;ambientSocks[i].localPosition=p;ambientSocks[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(clock*1.3f+i)*2f);}
            if(titleSplitting){split=Mathf.Min(1,split+dt/.45f);float e=1-Mathf.Pow(1-split,3);curtainL.anchoredPosition=new Vector2(-105-430*e,0);curtainR.anchoredPosition=new Vector2(105+430*e,0);}
            titleStart.localScale=Vector3.one*(1+Mathf.Sin(clock*2.4f)*.025f);
            if(game.CurrentFlow==YangmalDaetalchulGame.Flow.Practice)
            {
                float t=(clock%1.5f)/1.5f;
                Vector2 a=canvasRt.InverseTransformPoint(rulerRt.TransformPoint(new Vector3(0,-146,0))),b=canvasRt.InverseTransformPoint(rulerRt.TransformPoint(new Vector3(0,-146+292*(5f/23f),0)));
                AnimateGuide(a,b,t);invalidRippleRt.gameObject.SetActive(false);
            }
            else if(invalidGuideClock>0)
            {
                invalidGuideClock=Mathf.Max(0,invalidGuideClock-dt);float elapsed=1.2f-invalidGuideClock,t=Mathf.Clamp01(elapsed/.72f);
                float startY=handleRt.anchoredPosition.y,targetY=Mathf.Clamp(startY+(startY>70?-76:76),-146,146);
                Vector2 a=canvasRt.InverseTransformPoint(handleRt.position),b=canvasRt.InverseTransformPoint(rulerRt.TransformPoint(new Vector3(0,targetY,0)));
                AnimateGuide(a,b,t);invalidRippleRt.anchoredPosition=invalidGuidePoint;invalidRippleRt.localScale=Vector3.one*(.5f+elapsed*1.35f);var rc=invalidRippleRt.GetComponent<Image>().color;rc.a=.72f*(invalidGuideClock/1.2f);invalidRippleRt.GetComponent<Image>().color=rc;
                if(invalidGuideClock<=0){invalidRippleRt.gameObject.SetActive(false);Set(guideGroup,false);}
            }
            if(flying){sockFlight+=dt;float t=Mathf.Clamp01(sockFlight/1.15f);Vector3 end=correctReveal?new Vector3(-6.8f+Mathf.Clamp(game.GameState.rescued-1,0,8)*1.55f,7.25f,2.2f):new Vector3(3.4f,-.35f,2.6f);activeSock.position=Bezier(sockHome,new Vector3(2.8f,4.4f,1.5f),end,t);activeSock.localScale=Vector3.Lerp(new Vector3(1.2f,.8f,1),Vector3.one,t);if(t>=1)flying=false;}
            if(wireKick>0){wireKick=Mathf.Max(0,wireKick-dt*1.8f);clothesline.localPosition=new Vector3(0,-Mathf.Sin(wireKick*Mathf.PI)*.18f,0);}
            if(screenFlash.color.a>0){var c=screenFlash.color;c.a=Mathf.Max(0,c.a-dt*.7f);screenFlash.color=c;}
            if(revealGroup.alpha>0){revealAnim+=dt;revealCardRt.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Min(1,revealAnim/.35f)*Mathf.PI)*.08f);}
            if(game.CurrentFlow==YangmalDaetalchulGame.Flow.Playing){int sec=Mathf.CeilToInt(game.TimeLeft);if(sec!=lastSecond){lastSecond=sec;hudTime.text=sec+"초";}hudRound.text="양말 "+(game.GameState.round)+"/9";hudLives.text="실 "+new string('●',Mathf.Max(0,game.GameState.lives));hudFirst.text="첫 "+game.GameState.firstCorrect+"/9\n통과 7";}
            UpdateInputState();
        }
        void AnimateGuide(Vector2 a,Vector2 b,float t)
        {
            guideHandRt.anchoredPosition=Vector2.Lerp(a,b,t);guideHandRt.localRotation=Quaternion.Euler(0,0,-8+Mathf.Sin(t*Mathf.PI)*12);
            guideTargetRt.anchoredPosition=b;guideTargetRt.localScale=Vector3.one*(.86f+.18f*Mathf.Sin(clock*4.4f));handlePulseRt.anchoredPosition=a;handlePulseRt.localScale=Vector3.one*(.8f+.28f*Mathf.Sin(clock*5f));
            for(int i=0;i<guideDots.Length;i++){guideDots[i].rectTransform.anchoredPosition=Vector2.Lerp(a,b,(i+1f)/(guideDots.Length+1f));guideDots[i].color=new Color(1f,.9f,.58f,.35f+.4f*Mathf.Sin(clock*4f+i*.7f)*.5f+.2f);}
        }
        Vector3 Bezier(Vector3 a,Vector3 b,Vector3 c,float t){float u=1-t;return u*u*a+2*u*t*b+t*t*c;}
        public void ResetRunWorld(){invalidGuideClock=0;flying=false;sockFlight=0;wireKick=0;activeSock.position=sockHome;activeSock.localScale=Vector3.one;clothesline.localPosition=Vector3.zero;if(screenFlash){var c=screenFlash.color;c.a=0;screenFlash.color=c;}for(int i=0;i<rescuedSocks.Count;i++)rescuedSocks[i].gameObject.SetActive(false);}
        void Relayout(bool first)
        {
            lastW=Screen.width;lastH=Screen.height;wide=(Screen.width>=Screen.height&&Screen.width>=640)||Screen.width>=1024;
            scaler.referenceResolution=wide?new Vector2(1280,800):new Vector2(390,844);scaler.matchWidthOrHeight=wide?.5f:.5f;
            cam.orthographicSize=wide?8.4f:10.2f;cam.transform.position=wide?new Vector3(0,9,-22):new Vector3(0,9.8f,-23);cam.transform.LookAt(new Vector3(wide?-1.8f:0,3.4f,3.2f));
            if(wide)
            {
                diagramRt.anchorMin=diagramRt.anchorMax=new Vector2(.27f,.52f);diagramRt.sizeDelta=new Vector2(600,430);
                promptRt.anchorMin=promptRt.anchorMax=new Vector2(.71f,.74f);promptRt.sizeDelta=new Vector2(430,120);
                prompt.fontSize=15f;prompt.rectTransform.sizeDelta=new Vector2(410,104);
                rulerRt.anchorMin=rulerRt.anchorMax=new Vector2(.82f,.39f);rulerRt.sizeDelta=new Vector2(110,380);
                goalRt.anchorMin=goalRt.anchorMax=new Vector2(.5f,1);goalRt.sizeDelta=new Vector2(760,42);goalRt.anchoredPosition=new Vector2(0,-80);
                revealCardRt.anchorMin=revealCardRt.anchorMax=new Vector2(.72f,.14f);revealCardRt.sizeDelta=new Vector2(430,112);
            }
            else
            {
                diagramRt.anchorMin=diagramRt.anchorMax=new Vector2(.47f,.58f);diagramRt.sizeDelta=new Vector2(360,305);
                promptRt.anchorMin=promptRt.anchorMax=new Vector2(.38f,.33f);promptRt.sizeDelta=new Vector2(282,112);
                prompt.fontSize=13.5f;prompt.rectTransform.sizeDelta=new Vector2(266,102);
                rulerRt.anchorMin=rulerRt.anchorMax=new Vector2(.84f,.24f);rulerRt.sizeDelta=new Vector2(82,350);
                goalRt.anchorMin=goalRt.anchorMax=new Vector2(.5f,1);goalRt.sizeDelta=new Vector2(374,42);goalRt.anchoredPosition=new Vector2(0,-83);
                revealCardRt.anchorMin=revealCardRt.anchorMax=new Vector2(.5f,.16f);revealCardRt.sizeDelta=new Vector2(360,108);
            }
            for(int i=0;i<24;i++){float y=-146+i*(292f/23f);tickMarks[i].rectTransform.anchoredPosition=new Vector2(0,y);}
            for(int i=0;i<6;i++)tickLabels[i].rectTransform.anchoredPosition=new Vector2(0,-146+((i+1)*4-1)*(292f/23f));
            game.GameState.layout=wide?"land":"portrait";UpdateInputState();
        }
        void Set(CanvasGroup g,bool on){g.alpha=on?1:0;g.interactable=false;g.blocksRaycasts=false;}
    }
}
