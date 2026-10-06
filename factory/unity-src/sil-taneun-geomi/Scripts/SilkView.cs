// 실 타는 거미 — 라일락 온실 2.5D 디오라마와 계량 실 UI.
// 정답: 곡선 실이 팽팽해짐 → 거미 행렬 활주 → 꽃잎 개화.
// 오답: 실이 말려 돌아옴 → 거미가 고리에 매달림 → 안전 고리 낙하.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.SilTaneunGeomi
{
    public partial class SilkGame
    {
        static readonly Color Lilac=MgfLook.Hex("#DDD1F3");
        static readonly Color Plum=MgfLook.Hex("#704C8B");
        static readonly Color Ivory=MgfLook.Hex("#FFF7EC");
        static readonly Color Apricot=MgfLook.Hex("#F0AA83");
        static readonly Color Leaf=MgfLook.Hex("#99B89D");
        static readonly Color Ink=MgfLook.Hex("#3D3150");
        static readonly Color Mint=MgfLook.Hex("#BFD6C2");
        static readonly Color Gold=MgfLook.Hex("#FFD8A6");

        Camera cam;
        Vector3 cameraBase;
        Transform worldRoot,greenhouseRoot,routeSilk,silkTip,flowerLeft,flowerRight,titleSatchelWorld;
        readonly Transform[] leaves=new Transform[18];
        readonly Transform[] dew=new Transform[10];
        readonly Transform[] spiders=new Transform[9];
        readonly Transform[] spiderHeads=new Transform[9];
        readonly Transform[] petals=new Transform[10];
        readonly Transform[] pollen=new Transform[14];
        readonly Transform[] hooks=new Transform[3];
        readonly Transform[] hangingVines=new Transform[8];
        readonly Transform[] fireflies=new Transform[12];
        LineRenderer silkLine,dragTrail,lightRunner;
        Material floorMat,plumMat,ivoryMat,apricotMat,leafMat,mintMat,silkMat,glassMat,goldMat,darkMat,mossMat,waterMat;
        float worldClock,revealWorld,hitStop,wrongShake,flowerOpen,titlePush;
        bool worldCorrect;

        CanvasGroup titleG,hudG,revealG,endG,toastG;
        RectTransform rootRt,titleRt,titleSignRt,hudRt,titleStartRt,titleLeafL,titleLeafR;
        RectTransform goalRt,statusRt,lane0Rt,lane1Rt,problemRt,trackRt,handleRt,guideRt,guidePathRt,revealRt,endRt,toastRt,rippleRt,tpDiagramRt;
        TextMeshProUGUI titleLogo,titleTag,titleMeta,titleBest,titleCta;
        TextMeshProUGUI scoreTxt,progressTxt,livesTxt,timerTxt,goalTxt,lane0Txt,lane1Txt,promptTxt,evidenceTxt,diagramTxt,lengthTxt,targetKnotTxt;
        TextMeshProUGUI tpLuTxt,tpLlTxt,tpRuTxt,tpRlTxt;
        TextMeshProUGUI revealTitleTxt,revealFormulaTxt,endTitleTxt,endScoreTxt,endStatsTxt,endTapTxt,toastTxt;
        Image handleImg,guideImg,guidePathImg,revealBg,rippleImg,titleSatchelImg,lane0Bg,lane1Bg;
        readonly Image[] tickDots=new Image[24];
        readonly TextMeshProUGUI[] tickLabels=new TextMeshProUGUI[5];
        Sprite roundSprite,thinSprite,ringSprite,circleSprite;
        float guideClock,guideBoost,handlePulse,rippleClock,toastClock,displayScore,endDisplay,titleClock,titleStartBaseY=-60;
        int shownScore=-1,shownSecond=-1,layoutW=-1,layoutH=-1;
        float layoutCanvasW=-1,layoutCanvasH=-1;
        bool landscape;
        // 삼각형 거미줄 모식도: 0 AD, 1 DB, 2 AE, 3 EC, 4 DE, 5 BC
        RectTransform triDiagramRt;
        readonly Image[] triSeg=new Image[6];
        readonly TextMeshProUGUI[] triLab=new TextMeshProUGUI[6];
        readonly Image[] triKnot=new Image[5];
        readonly TextMeshProUGUI[] triPt=new TextMeshProUGUI[5];
        TextMeshProUGUI triNoteTxt,liveTargetTxt;
        Image liveTargetSeg;
        string liveTargetName="";
        float guideShow;

        RectTransform R(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);
            rt.anchoredPosition=pos;rt.sizeDelta=size;return rt;
        }

        Image Img(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color,Sprite sprite=null)
        {
            var rt=R(name,parent,anchor,pos,size);var im=rt.gameObject.AddComponent<Image>();
            im.color=color;im.sprite=sprite;im.raycastTarget=false;
            if(sprite&&sprite.border!=Vector4.zero)im.type=Image.Type.Sliced;return im;
        }

        TextMeshProUGUI Txt(Transform parent,string value,Vector2 anchor,Vector2 pos,float size,Color color,float width=360,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {
            var rt=R("Text",parent,anchor,pos,new Vector2(width,size*2.25f));
            var t=rt.gameObject.AddComponent<TextMeshProUGUI>();t.font=MgfText.Font;t.fontSize=size;t.color=color;t.alignment=align;
            t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;t.text=value;return t;
        }

        static Sprite MakeSprite(int size,float radius,bool ring)
        {
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){name=ring?"SilkRing":"SilkRound",wrapMode=TextureWrapMode.Clamp};
            var px=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float fx=x+.5f,fy=y+.5f,a;
                if(ring){float dx=fx-size*.5f,dy=fy-size*.5f,d=Mathf.Sqrt(dx*dx+dy*dy);a=Mathf.Clamp01(1-Mathf.Abs(d-size*.39f)/(size*.07f));}
                else{float qx=Mathf.Max(Mathf.Abs(fx-size*.5f)-(size*.5f-radius),0),qy=Mathf.Max(Mathf.Abs(fy-size*.5f)-(size*.5f-radius),0);a=Mathf.Clamp01(radius-Mathf.Sqrt(qx*qx+qy*qy)+.8f);}
                px[y*size+x]=new Color32(255,255,255,(byte)(a*255));
            }
            tex.SetPixels32(px);tex.Apply(false,true);float b=ring?0:radius+2;
            return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(b,b,b,b));
        }

        void BuildWorld()
        {
            // WebGL 엔진 스트리핑이 절차적 Capsule/Sphere의 collider 타입을 빼지 않게 보존한다.
            var colliderTypes=new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<CapsuleCollider>();colliderTypes.AddComponent<SphereCollider>();
            UnityEngine.Object.Destroy(colliderTypes);
            MgfLook.Sky(MgfLook.Hex("#6D5D91"),Lilac,MgfLook.Hex("#F4C8AD"),.84f);
            MgfLook.Sun(new Vector3(46,-32,18),MgfLook.Hex("#FFF1D8"),1.04f,.72f);
            cam=MgfLook.Camera(new Vector3(12.5f,13.4f,-16.2f),new Vector3(0,.5f,3.4f),36f);
            cam.orthographic=true;cam.orthographicSize=9.2f;cameraBase=cam.transform.position;

            floorMat=MgfLook.Lit(MgfLook.Hex("#78907D"),.18f,.01f);
            plumMat=MgfLook.Lit(Plum,.42f,.05f,Plum*.06f);
            ivoryMat=MgfLook.Lit(Ivory,.35f,.01f);
            apricotMat=MgfLook.Lit(Apricot,.42f,.01f,Apricot*.08f);
            leafMat=MgfLook.Lit(Leaf,.18f,.01f);
            mintMat=MgfLook.Lit(Mint,.22f,.01f);
            silkMat=MgfLook.Alpha(new Color(1f,.97f,.93f,.92f));
            glassMat=MgfLook.Alpha(new Color(.86f,.92f,1f,.18f));
            goldMat=MgfLook.Lit(Gold,.5f,.05f,Gold*.22f);
            darkMat=MgfLook.Lit(Ink,.14f,.02f);
            mossMat=MgfLook.Lit(MgfLook.Hex("#526B5B"),.12f,.01f);
            waterMat=MgfLook.Lit(MgfLook.Hex("#87BFC2"),.68f,.12f,MgfLook.Hex("#21495D")*.08f);

            worldRoot=new GameObject("LavenderGlasshouseMiniature").transform;
            MgfLook.Block("MossGlasshouseFloor",new Vector3(0,-1.0f,3.5f),new Vector3(25,1.4f,21),.8f,floorMat,worldRoot);
            MgfLook.Block("WaterRill",new Vector3(0,-.24f,4.0f),new Vector3(2.2f,.18f,18.5f),.46f,waterMat,worldRoot);
            for(int i=0;i<11;i++)
            {
                float z=-4.4f+i*1.65f;float x=(i%2==0?-1:1)*(.36f+(i%3)*.14f);
                MgfLook.Prim(PrimitiveType.Sphere,"RillStone"+i,new Vector3(x,-.04f,z),new Vector3(.72f,.16f,.55f),ivoryMat,worldRoot,false);
            }
            greenhouseRoot=new GameObject("GlasshouseFrame").transform;greenhouseRoot.SetParent(worldRoot,false);
            BuildGlasshouse();BuildPlants();BuildHangingCanopy();BuildFlowers();BuildSpiderQueue();BuildSilkRoute();
        }

        void BuildGlasshouse()
        {
            for(int side=-1;side<=1;side+=2)
            for(int z=-4;z<=12;z+=4)
            {
                MgfLook.Block("VioletFrame",new Vector3(side*9.3f,3.1f,z),new Vector3(.22f,8.2f,.22f),.08f,plumMat,greenhouseRoot);
                var pane=MgfLook.Block("GlassPane",new Vector3(side*9.18f,3.0f,z+1.85f),new Vector3(.06f,6.8f,3.55f),.05f,glassMat,greenhouseRoot);
                Destroy(pane.GetComponent<Collider>());
            }
            for(int z=-4;z<=12;z+=4)
            {
                var r=MgfLook.Block("RoofRib",new Vector3(0,7.0f,z),new Vector3(18.8f,.18f,.20f),.06f,plumMat,greenhouseRoot);
                r.transform.localRotation=Quaternion.Euler(0,0,(z%8==0?7:-7));
            }
            for(int i=0;i<8;i++)
            {
                float x=-7.6f+(i%4)*5.1f,z=10.5f+(i/4)*2.0f;
                MgfLook.Prim(PrimitiveType.Cylinder,"RoundPot"+i,new Vector3(x,-.05f,z),new Vector3(1.15f,1.05f,1.15f),i%2==0?apricotMat:ivoryMat,worldRoot,false);
            }
        }

        void BuildPlants()
        {
            for(int i=0;i<leaves.Length;i++)
            {
                int side=i%2==0?-1:1;float z=-3.8f+(i/2)*1.85f;
                leaves[i]=MgfLook.Prim(PrimitiveType.Capsule,"WaxyLeaf"+i,new Vector3(side*(6.1f+(i%3)*.55f),.45f+(i%4)*.22f,z),new Vector3(.55f,1.25f,.22f),i%3==0?mintMat:leafMat,worldRoot,false).transform;
                leaves[i].localRotation=Quaternion.Euler(18,side*20,side*(35+i%4*6));
            }
            for(int i=0;i<dew.Length;i++)
            {
                float x=-7.2f+(i%5)*3.6f,z=-1.5f+(i/5)*7.7f;
                dew[i]=MgfLook.Prim(PrimitiveType.Sphere,"DewBead"+i,new Vector3(x,2.0f+(i%3)*.55f,z),new Vector3(.28f,.34f,.28f),glassMat,worldRoot,false).transform;
            }
            for(int i=0;i<hooks.Length;i++)
            {
                hooks[i]=MgfLook.Prim(PrimitiveType.Cylinder,"SafetyHook"+i,new Vector3(7.8f,2.55f-i*.82f,1.4f),new Vector3(.42f,.10f,.42f),goldMat,worldRoot,false).transform;
                hooks[i].localRotation=Quaternion.Euler(90,0,0);
            }
        }

        void BuildHangingCanopy()
        {
            for(int i=0;i<hangingVines.Length;i++)
            {
                float x=-7.4f+i*(14.8f/(hangingVines.Length-1));
                float z=1.0f+(i%4)*3.1f;
                var root=new GameObject("HangingVine"+i).transform;root.SetParent(worldRoot,false);root.localPosition=new Vector3(x,6.75f,z);
                hangingVines[i]=root;
                MgfLook.Prim(PrimitiveType.Cylinder,"Vine",new Vector3(0,-1.55f,0),new Vector3(.075f,1.65f,.075f),mossMat,root,false);
                for(int j=0;j<3;j++)
                {
                    int side=(j%2==0?-1:1);
                    var leaf=MgfLook.Prim(PrimitiveType.Capsule,"CanopyLeaf",new Vector3(side*.34f,-.8f-j*.78f,.04f*j),new Vector3(.24f,.66f,.15f),j==2?mintMat:leafMat,root,false);
                    leaf.transform.localRotation=Quaternion.Euler(22,side*18,side*48);
                }
            }
            for(int i=0;i<fireflies.Length;i++)
            {
                float x=-7.2f+(i%6)*2.85f,z=-1.5f+(i/6)*8.4f;
                fireflies[i]=MgfLook.Prim(PrimitiveType.Sphere,"WarmMote"+i,new Vector3(x,2.0f+(i%4)*.7f,z),Vector3.one*.12f,goldMat,worldRoot,false).transform;
            }
        }

        void BuildFlowers()
        {
            flowerLeft=new GameObject("LeftCourierFlower").transform;flowerLeft.SetParent(worldRoot,false);flowerLeft.localPosition=new Vector3(-5.3f,.1f,2.2f);
            flowerRight=new GameObject("RightCourierFlower").transform;flowerRight.SetParent(worldRoot,false);flowerRight.localPosition=new Vector3(5.3f,.1f,4.2f);
            BuildFlower(flowerLeft,0);BuildFlower(flowerRight,5);
        }

        void BuildFlower(Transform root,int offset)
        {
            MgfLook.Prim(PrimitiveType.Cylinder,"Stem",new Vector3(0,.8f,0),new Vector3(.18f,1.45f,.18f),leafMat,root,false);
            MgfLook.Prim(PrimitiveType.Sphere,"FlowerCore",new Vector3(0,2.12f,0),new Vector3(.52f,.48f,.52f),goldMat,root,false);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5;var p=MgfLook.Prim(PrimitiveType.Sphere,"Petal"+i,new Vector3(Mathf.Cos(a)*.78f,2.12f+Mathf.Sin(a)*.55f,0),new Vector3(.72f,.42f,.25f),i%2==0?apricotMat:ivoryMat,root,false).transform;
                p.localRotation=Quaternion.Euler(0,0,i*72);petals[offset+i]=p;
            }
        }

        Transform BuildSpider(string name,Vector3 pos,float scale)
        {
            var root=new GameObject(name).transform;root.SetParent(worldRoot,false);root.localPosition=pos;root.localScale=Vector3.one*scale;
            MgfLook.Prim(PrimitiveType.Sphere,"Abdomen",new Vector3(0,.38f,.18f),new Vector3(.60f,.48f,.62f),plumMat,root,false);
            var head=MgfLook.Prim(PrimitiveType.Sphere,"Head",new Vector3(0,.42f,-.38f),new Vector3(.43f,.38f,.42f),plumMat,root,false).transform;
            for(int side=-1;side<=1;side+=2)
            for(int j=0;j<4;j++)
            {
                var leg=MgfLook.Prim(PrimitiveType.Capsule,"Leg",new Vector3(side*(.42f+j*.07f),.23f,-.25f+j*.2f),new Vector3(.10f,.45f,.10f),darkMat,root,false);
                leg.transform.localRotation=Quaternion.Euler(80,0,side*(48+j*9));
            }
            for(int side=-1;side<=1;side+=2)MgfLook.Prim(PrimitiveType.Sphere,"Eye",new Vector3(side*.13f,.52f,-.74f),new Vector3(.065f,.075f,.055f),ivoryMat,root,false);
            MgfLook.Prim(PrimitiveType.Sphere,"PollenSatchel",new Vector3(.42f,.48f,.30f),new Vector3(.28f,.34f,.20f),apricotMat,root,false);
            int idx=Array.IndexOf(spiders,null);if(idx>=0){spiders[idx]=root;spiderHeads[idx]=head;}
            return root;
        }

        void BuildSpiderQueue()
        {
            for(int i=0;i<spiders.Length;i++)BuildSpider("SilkCourier"+i,new Vector3(-4.4f+i*.62f,.42f,3.0f+(i%2)*.24f),i==0?1.2f:.82f);
            titleSatchelWorld=MgfLook.Prim(PrimitiveType.Sphere,"TitlePollenSatchel",new Vector3(0,1.1f,-.4f),new Vector3(.95f,.72f,.46f),apricotMat,worldRoot,false).transform;
        }

        void BuildSilkRoute()
        {
            silkLine=MakeLine("PearlSilkRoute",silkMat,.09f,24);dragTrail=MakeLine("FingerSilkTrail",MgfLook.Alpha(new Color(1f,.78f,.70f,.62f)),.16f,8);
            lightRunner=MakeLine("SilkLightRunner",MgfLook.Additive(new Color(1f,.75f,.45f,.85f)),.18f,4);
            dragTrail.enabled=false;lightRunner.enabled=false;
            routeSilk=new GameObject("RouteSilkAnchor").transform;routeSilk.SetParent(worldRoot,false);
            silkTip=MgfLook.Prim(PrimitiveType.Sphere,"SilkTip",new Vector3(0,.55f,2.95f),new Vector3(.34f,.34f,.34f),ivoryMat,worldRoot,false).transform;
            for(int i=0;i<pollen.Length;i++)pollen[i]=MgfLook.Prim(PrimitiveType.Sphere,"PollenMote"+i,new Vector3(0,-20,0),Vector3.one*.13f,goldMat,worldRoot,false).transform;
            SetWorldSilk(false,0);
        }

        LineRenderer MakeLine(string name,Material mat,float width,int points)
        {
            var go=new GameObject(name);go.transform.SetParent(worldRoot,false);var lr=go.AddComponent<LineRenderer>();
            lr.sharedMaterial=mat;lr.positionCount=points;lr.startWidth=lr.endWidth=width;lr.numCapVertices=4;lr.numCornerVertices=3;
            lr.useWorldSpace=true;lr.shadowCastingMode=ShadowCastingMode.Off;lr.receiveShadows=false;return lr;
        }

        void BuildUi()
        {
            roundSprite=MakeSprite(64,15,false);thinSprite=MakeSprite(48,8,false);ringSprite=MakeSprite(64,12,true);circleSprite=MakeSprite(64,31,false);
            rootRt=R("SilkUi",MgfText.Canvas.transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
            rootRt.anchorMin=Vector2.zero;rootRt.anchorMax=Vector2.one;rootRt.anchoredPosition=Vector2.zero;rootRt.sizeDelta=Vector2.zero;
            BuildTitleUi();BuildPlayUi();BuildRevealUi();BuildEndUi();BuildToastUi();
        }

        void BuildTitleUi()
        {
            titleRt=R("TitleScreen",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);titleRt.anchorMin=Vector2.zero;titleRt.anchorMax=Vector2.one;titleRt.sizeDelta=Vector2.zero;
            titleG=titleRt.gameObject.AddComponent<CanvasGroup>();
            titleLeafL=R("OpeningLeafLeft",titleRt,new Vector2(.5f,.5f),new Vector2(-112,112),new Vector2(188,162));
            Img(titleLeafL,"Leaf",new Vector2(.5f,.5f),Vector2.zero,titleLeafL.sizeDelta,new Color(Leaf.r,Leaf.g,Leaf.b,.78f),roundSprite);
            titleLeafR=R("OpeningLeafRight",titleRt,new Vector2(.5f,.5f),new Vector2(112,112),new Vector2(188,162));
            Img(titleLeafR,"Leaf",new Vector2(.5f,.5f),Vector2.zero,titleLeafR.sizeDelta,new Color(Mint.r,Mint.g,Mint.b,.78f),roundSprite);
            titleSignRt=R("HangingSilkBanner",titleRt,new Vector2(.5f,.5f),new Vector2(0,148),new Vector2(348,148));
            var sign=Img(titleSignRt,"DyedSilk",new Vector2(.5f,.5f),Vector2.zero,titleSignRt.sizeDelta,new Color(Plum.r,Plum.g,Plum.b,.92f),roundSprite);
            Img(titleSignRt,"LeftThread",new Vector2(.5f,1),new Vector2(-118,34),new Vector2(4,74),new Color(Ivory.r,Ivory.g,Ivory.b,.78f),thinSprite);
            Img(titleSignRt,"RightThread",new Vector2(.5f,1),new Vector2(118,34),new Vector2(4,74),new Color(Ivory.r,Ivory.g,Ivory.b,.78f),thinSprite);
            titleLogo=Txt(sign.transform,"실 타는 <color=#FFD8A6>거미</color>",new Vector2(.5f,.5f),new Vector2(0,22),41,Ivory,330);titleLogo.fontStyle=FontStyles.Bold;
            titleTag=Txt(sign.transform,"실을 늘려 길을 이어라",new Vector2(.5f,.5f),new Vector2(0,-31),17,Gold,310);
            titleMeta=Txt(titleRt,"중학교 2학년 · 평행선과 선분의 길이의 비",new Vector2(.5f,.5f),new Vector2(0,55),14,Ink,350);
            titleBest=Txt(titleRt,"",new Vector2(.5f,.5f),new Vector2(0,26),13,Plum,330);
            titleStartRt=R("PollenPodStart",titleRt,new Vector2(.5f,.5f),new Vector2(0,-60),new Vector2(176,150));
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI*2/6;var petal=Img(titleStartRt,"PodPetal"+i,new Vector2(.5f,.5f),new Vector2(Mathf.Cos(a)*46,Mathf.Sin(a)*35+17),new Vector2(48,27),i%2==0?new Color(Ivory.r,Ivory.g,Ivory.b,.92f):new Color(Mint.r,Mint.g,Mint.b,.92f),roundSprite);
                petal.rectTransform.localRotation=Quaternion.Euler(0,0,-i*60);
            }
            titleSatchelImg=Img(titleStartRt,"PollenPod",new Vector2(.5f,.5f),new Vector2(0,17),new Vector2(84,84),Apricot,circleSprite);
            Img(titleSatchelImg.transform,"Buckle",new Vector2(.5f,.5f),new Vector2(0,10),new Vector2(28,20),Gold,roundSprite);
            titleCta=Txt(titleStartRt,"꽃가루 주머니 열기",new Vector2(.5f,.5f),new Vector2(0,-57),18,Ink,174);titleCta.fontStyle=FontStyles.Bold;
        }

        void BuildPlayUi()
        {
            hudRt=R("PlayHud",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);hudRt.anchorMin=Vector2.zero;hudRt.anchorMax=Vector2.one;hudRt.sizeDelta=Vector2.zero;
            hudG=hudRt.gameObject.AddComponent<CanvasGroup>();
            statusRt=R("LeafStatus",hudRt,new Vector2(.5f,.5f),new Vector2(0,371),new Vector2(362,62));
            Img(statusRt,"StatusLeaf",new Vector2(.5f,.5f),Vector2.zero,statusRt.sizeDelta,new Color(Ivory.r,Ivory.g,Ivory.b,.96f),roundSprite);
            scoreTxt=Txt(statusRt,"0",new Vector2(0,0.5f),new Vector2(45,0),22,Plum,80,TextAlignmentOptions.Left);scoreTxt.fontStyle=FontStyles.Bold;
            progressTxt=Txt(statusRt,"0 / 9 · 120초",new Vector2(.5f,.5f),Vector2.zero,15,Ink,176);
            livesTxt=Txt(statusRt,"◆ ◆ ◆",new Vector2(1,.5f),new Vector2(-55,0),17,Apricot,110,TextAlignmentOptions.Right);
            timerTxt=Txt(statusRt,"",new Vector2(.5f,.5f),Vector2.zero,1,new Color(0,0,0,0),1);
            goalRt=R("GoalRibbon",hudRt,new Vector2(.5f,.5f),new Vector2(0,310),new Vector2(350,48));
            Img(goalRt,"Goal",new Vector2(.5f,.5f),Vector2.zero,goalRt.sizeDelta,new Color(Plum.r,Plum.g,Plum.b,.91f),roundSprite);
            goalTxt=Txt(goalRt,"실 길이를 맞추면 거미가 꽃으로 간다",new Vector2(.5f,.5f),Vector2.zero,16,Ivory,330);goalTxt.fontStyle=FontStyles.Bold;
            lane0Rt=BuildLaneCard("LaneA",new Vector2(-88,250),out lane0Bg,out lane0Txt);
            lane1Rt=BuildLaneCard("LaneB",new Vector2(88,250),out lane1Bg,out lane1Txt);
            problemRt=R("HangingGeometryGlass",hudRt,new Vector2(.5f,.5f),new Vector2(0,98),new Vector2(358,260));
            Img(problemRt,"SmokedGlass",new Vector2(.5f,.5f),Vector2.zero,problemRt.sizeDelta,new Color(.18f,.25f,.24f,.92f),roundSprite);
            Img(problemRt,"TopKnot",new Vector2(.5f,1),new Vector2(0,7),new Vector2(104,8),Gold,thinSprite);
            promptTxt=Txt(problemRt,"",new Vector2(.5f,1),new Vector2(0,-58),15,Ivory,332);promptTxt.fontStyle=FontStyles.Bold;promptTxt.rectTransform.sizeDelta=new Vector2(332,112);
            diagramTxt=Txt(problemRt,"",new Vector2(.5f,.5f),new Vector2(0,-18),24,Gold,330);diagramTxt.fontStyle=FontStyles.Bold;
            evidenceTxt=Txt(problemRt,"",new Vector2(.5f,0),new Vector2(0,31),13,Ivory,330);evidenceTxt.rectTransform.sizeDelta=new Vector2(330,48);
            Txt(problemRt,"모식도는 비례 관계만 나타냄",new Vector2(.5f,0),new Vector2(0,10),10,new Color(Mint.r,Mint.g,Mint.b,.92f),320);
            BuildThreeParallelDiagram();BuildTriangleDiagram();
            trackRt=R("TargetMeasuringSilk",hudRt,new Vector2(.5f,.5f),new Vector2(0,-198),new Vector2(356,138));
            Img(trackRt,"LeafVeinPlate",new Vector2(.5f,.5f),Vector2.zero,trackRt.sizeDelta,new Color(Plum.r,Plum.g,Plum.b,.90f),roundSprite);
            targetKnotTxt=Txt(trackRt,"선분에 잇는 실",new Vector2(.5f,1),new Vector2(-62,-18),12,Gold,210,TextAlignmentOptions.Left);targetKnotTxt.fontStyle=FontStyles.Bold;
            Img(trackRt,"PearlRail",new Vector2(.5f,.5f),new Vector2(0,-2),new Vector2(318,8),new Color(Ivory.r,Ivory.g,Ivory.b,.58f),thinSprite);
            for(int i=0;i<24;i++)
            {
                float x=-154+i*(308f/23f);tickDots[i]=Img(trackRt,"Tick"+(i+1),new Vector2(.5f,.5f),new Vector2(x,-2),new Vector2(i%5==0?7:5,i%5==0?24:15),Ivory,thinSprite);
            }
            int[] labs={1,6,12,18,24};for(int i=0;i<labs.Length;i++)
            {
                float x=-154+(labs[i]-1)*(308f/23f);tickLabels[i]=Txt(trackRt,labs[i].ToString(),new Vector2(.5f,.5f),new Vector2(x,-40),12,Ivory,42);
            }
            handleRt=R("SilkEndHandle",trackRt,new Vector2(.5f,.5f),new Vector2(-154,10),new Vector2(62,62));
            handleImg=Img(handleRt,"PearlHandle",new Vector2(.5f,.5f),Vector2.zero,new Vector2(56,56),Plum,ringSprite);
            lengthTxt=Txt(trackRt,"1 cm",new Vector2(1,1),new Vector2(-58,-18),19,Gold,100,TextAlignmentOptions.Right);lengthTxt.fontStyle=FontStyles.Bold;
            guidePathRt=R("GuidePath",trackRt,new Vector2(.5f,.5f),new Vector2(-89,5),new Vector2(132,6));
            guidePathImg=Img(guidePathRt,"FlowingDashes",new Vector2(.5f,.5f),Vector2.zero,guidePathRt.sizeDelta,new Color(Apricot.r,Apricot.g,Apricot.b,.72f),thinSprite);
            guideRt=R("GhostFinger",trackRt,new Vector2(.5f,.5f),new Vector2(-154,28),new Vector2(48,48));
            guideImg=Img(guideRt,"Finger",new Vector2(.5f,.5f),Vector2.zero,new Vector2(42,42),Ivory,roundSprite);
            Txt(guideRt,"↓",new Vector2(.5f,.5f),new Vector2(0,-1),25,Plum,42);
        }

        void BuildThreeParallelDiagram()
        {
            tpDiagramRt=R("TwoTransversalDiagram",problemRt,new Vector2(.5f,.5f),new Vector2(0,-31),new Vector2(306,88));
            for(int i=0;i<3;i++)
            {
                float y=32-i*32;Img(tpDiagramRt,"Parallel"+i,new Vector2(.5f,.5f),new Vector2(0,y),new Vector2(230,3),new Color(Gold.r,Gold.g,Gold.b,.86f),thinSprite);
                Txt(tpDiagramRt,i==0?"l":i==1?"m":"n",new Vector2(.5f,.5f),new Vector2(-136,y),11,Gold,24);
            }
            var left=Img(tpDiagramRt,"LeftTransversal",new Vector2(.5f,.5f),new Vector2(-70,0),new Vector2(4,86),Ivory,thinSprite);left.rectTransform.localRotation=Quaternion.Euler(0,0,-10);
            var right=Img(tpDiagramRt,"RightTransversal",new Vector2(.5f,.5f),new Vector2(70,0),new Vector2(4,86),Ivory,thinSprite);right.rectTransform.localRotation=Quaternion.Euler(0,0,10);
            tpLuTxt=Txt(tpDiagramRt,"",new Vector2(.5f,.5f),new Vector2(-46,17),10,Ivory,70);
            tpLlTxt=Txt(tpDiagramRt,"",new Vector2(.5f,.5f),new Vector2(-46,-17),10,Ivory,70);
            tpRuTxt=Txt(tpDiagramRt,"",new Vector2(.5f,.5f),new Vector2(47,17),10,Ivory,70);
            tpRlTxt=Txt(tpDiagramRt,"",new Vector2(.5f,.5f),new Vector2(47,-17),10,Ivory,70);
            tpDiagramRt.gameObject.SetActive(false);
        }

        void BuildTriangleDiagram()
        {
            triDiagramRt=R("TriangleWebDiagram",problemRt,new Vector2(.5f,.5f),new Vector2(0,-26),new Vector2(306,100));
            string[] names={"AD","DB","AE","EC","DE","BC"};
            for(int i=0;i<6;i++)
            {
                triSeg[i]=Img(triDiagramRt,"Silk"+names[i],new Vector2(.5f,.5f),Vector2.zero,new Vector2(10,3),Ivory,thinSprite);
                triLab[i]=Txt(triDiagramRt,"",new Vector2(.5f,.5f),Vector2.zero,11,Ivory,84);triLab[i].fontStyle=FontStyles.Bold;
            }
            for(int i=0;i<5;i++)
            {
                triKnot[i]=Img(triDiagramRt,"Knot"+i,new Vector2(.5f,.5f),Vector2.zero,new Vector2(7,7),Gold,circleSprite);
                triPt[i]=Txt(triDiagramRt,"",new Vector2(.5f,.5f),Vector2.zero,10,Gold,20);
            }
            triNoteTxt=Txt(triDiagramRt,"",new Vector2(.5f,.5f),new Vector2(118,38),10,Gold,80,TextAlignmentOptions.Right);
            triDiagramRt.gameObject.SetActive(false);
        }

        static void PlaceSeg(Image im,Vector2 a,Vector2 b,float thick)
        {
            var rt=im.rectTransform;Vector2 d=b-a;rt.anchoredPosition=(a+b)*.5f;rt.sizeDelta=new Vector2(d.magnitude,thick);
            rt.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
        }

        void RefreshTriangleDiagram()
        {
            // 선분 이름과 주어진 길이만 거미줄 위에 붙인다. 어느 선분끼리 대응하는지(부분:부분 /
            // 부분:전체)는 학생이 정한다 — 완성된 비례식은 정답을 맞힌 뒤 판정 카드에서만 보인다.
            var p=current;bool mid=p.kind==SilkKind.Midpoint;
            float t;
            if(mid)t=.5f;else t=Mathf.Clamp((float)p.a/(p.a+p.b),.32f,.68f);
            Vector2 A=new Vector2(0,40),B=new Vector2(-104,-38),C=new Vector2(104,-38);
            Vector2 D=Vector2.Lerp(A,B,t),E=Vector2.Lerp(A,C,t);
            Vector2[] from={A,D,A,E,D,B},to={D,B,E,C,E,C};
            string[] lab=new string[6];int target;
            if(p.kind==SilkKind.PartRatio){lab[0]="AD "+p.a+" cm";lab[1]="DB "+p.b+" cm";lab[2]="AE "+p.c+" cm";target=3;}
            else if(mid){lab[5]="BC "+p.a+" cm";target=4;}
            else{lab[0]="AD "+p.a+" cm";lab[1]="DB "+p.b+" cm";lab[5]="BC "+p.c+" cm";target=4;}
            Vector2 nl=new Vector2(-.80f,.60f),nr=new Vector2(.80f,.60f);
            for(int i=0;i<6;i++)
            {
                bool tg=i==target,given=!string.IsNullOrEmpty(lab[i]);
                PlaceSeg(triSeg[i],from[i],to[i],tg?5f:given?3f:2f);
                triSeg[i].color=tg?Apricot:given?Ivory:new Color(Ivory.r,Ivory.g,Ivory.b,.42f);
                Vector2 m=(from[i]+to[i])*.5f;
                // 옆변 길이는 선분 바깥쪽에서 시작하도록 정렬해 꼭짓점 이름(D·E)과 겹치지 않게 한다.
                Vector2 off=i<2?nl*22+new Vector2(-42,0):i<4?nr*22+new Vector2(42,0):i==4?new Vector2(0,-10):new Vector2(0,-12);
                triLab[i].alignment=i<2?TextAlignmentOptions.Right:i<4?TextAlignmentOptions.Left:TextAlignmentOptions.Center;
                triLab[i].rectTransform.anchoredPosition=m+off;
                triLab[i].color=tg?Apricot:Ivory;triLab[i].text=given?lab[i]:"";
            }
            liveTargetName=mid?"MN":target==3?"EC":"DE";
            liveTargetTxt=triLab[target];liveTargetSeg=triSeg[target];
            Vector2[] pts={A,B,C,D,E};string[] pn={"A","B","C",mid?"M":"D",mid?"N":"E"};
            Vector2[] po={new Vector2(0,11),new Vector2(-10,-6),new Vector2(10,-6),new Vector2(-11,4),new Vector2(11,4)};
            for(int i=0;i<5;i++){triKnot[i].rectTransform.anchoredPosition=pts[i];triPt[i].rectTransform.anchoredPosition=pts[i]+po[i];triPt[i].text=pn[i];}
            triNoteTxt.text=mid?"AM=MB\nAN=NC":"DE ∥ BC";
        }

        RectTransform BuildLaneCard(string name,Vector2 pos,out Image bg,out TextMeshProUGUI label)
        {
            var rt=R(name,hudRt,new Vector2(.5f,.5f),pos,new Vector2(170,54));
            bg=Img(rt,"LaneLeaf",new Vector2(.5f,.5f),Vector2.zero,rt.sizeDelta,new Color(Mint.r,Mint.g,Mint.b,.96f),roundSprite);
            label=Txt(rt,"",new Vector2(.5f,.5f),Vector2.zero,12,Ink,156);return rt;
        }

        void BuildRevealUi()
        {
            revealRt=R("SilkVerdict",rootRt,new Vector2(.5f,.5f),new Vector2(0,-38),new Vector2(350,118));
            revealBg=Img(revealRt,"VerdictLeaf",new Vector2(.5f,.5f),Vector2.zero,revealRt.sizeDelta,new Color(Ivory.r,Ivory.g,Ivory.b,.98f),roundSprite);
            revealG=revealRt.gameObject.AddComponent<CanvasGroup>();
            revealTitleTxt=Txt(revealRt,"",new Vector2(.5f,.5f),new Vector2(0,27),22,Plum,326);revealTitleTxt.fontStyle=FontStyles.Bold;
            revealFormulaTxt=Txt(revealRt,"",new Vector2(.5f,.5f),new Vector2(0,-23),15,Ink,326);
        }

        void BuildEndUi()
        {
            endRt=R("EndGreenhouse",rootRt,new Vector2(.5f,.5f),Vector2.zero,new Vector2(366,476));endG=endRt.gameObject.AddComponent<CanvasGroup>();
            Img(endRt,"EndLeaf",new Vector2(.5f,.5f),Vector2.zero,endRt.sizeDelta,new Color(Ivory.r,Ivory.g,Ivory.b,.98f),roundSprite);
            endTitleTxt=Txt(endRt,"",new Vector2(.5f,.5f),new Vector2(0,158),34,Plum,340);endTitleTxt.fontStyle=FontStyles.Bold;
            endScoreTxt=Txt(endRt,"0",new Vector2(.5f,.5f),new Vector2(0,68),54,Apricot,250);endScoreTxt.fontStyle=FontStyles.Bold;
            Txt(endRt,"꽃가루 배달 점수",new Vector2(.5f,.5f),new Vector2(0,21),14,Ink,250);
            endStatsTxt=Txt(endRt,"",new Vector2(.5f,.5f),new Vector2(0,-48),18,Ink,320);endStatsTxt.rectTransform.sizeDelta=new Vector2(320,105);
            var retry=Img(endRt,"RetrySatchel",new Vector2(.5f,.5f),new Vector2(0,-154),new Vector2(270,70),Apricot,roundSprite);
            endTapTxt=Txt(retry.transform,"주머니를 눌러 다시 배달",new Vector2(.5f,.5f),Vector2.zero,18,Ink,250);endTapTxt.fontStyle=FontStyles.Bold;
        }

        void BuildToastUi()
        {
            toastRt=R("CoachToast",rootRt,new Vector2(.5f,.5f),new Vector2(0,-310),new Vector2(344,62));toastG=toastRt.gameObject.AddComponent<CanvasGroup>();
            Img(toastRt,"ToastLeaf",new Vector2(.5f,.5f),Vector2.zero,toastRt.sizeDelta,new Color(Plum.r,Plum.g,Plum.b,.96f),roundSprite);
            toastTxt=Txt(toastRt,"",new Vector2(.5f,.5f),Vector2.zero,14,Ivory,322);
            rippleRt=R("TapRipple",rootRt,new Vector2(.5f,.5f),Vector2.zero,new Vector2(54,54));
            rippleImg=Img(rippleRt,"Ripple",new Vector2(.5f,.5f),Vector2.zero,rippleRt.sizeDelta,new Color(0,0,0,0),ringSprite);
        }

        void Layout()
        {
            float w=Mathf.Max(320,rootRt.rect.width),h=Mathf.Max(420,rootRt.rect.height);
            if(layoutW==Screen.width&&layoutH==Screen.height&&Mathf.Abs(layoutCanvasW-w)<.5f&&Mathf.Abs(layoutCanvasH-h)<.5f)return;
            layoutW=Screen.width;layoutH=Screen.height;layoutCanvasW=w;layoutCanvasH=h;
            landscape=w/h>=1.08f;
            float top=h*.5f-34f;
            if(!landscape)
            {
                statusRt.anchoredPosition=new Vector2(0,top);goalRt.anchoredPosition=new Vector2(0,top-61);
                lane0Rt.anchoredPosition=new Vector2(-88,top-121);lane1Rt.anchoredPosition=new Vector2(88,top-121);
                problemRt.anchoredPosition=new Vector2(0,98);trackRt.anchoredPosition=new Vector2(0,-198);
                problemRt.sizeDelta=new Vector2(358,260);trackRt.sizeDelta=new Vector2(356,138);
                toastRt.anchoredPosition=new Vector2(0,-h*.5f+72);
                titleSignRt.anchoredPosition=new Vector2(0,Mathf.Min(158,h*.5f-94));
                titleMeta.rectTransform.anchoredPosition=new Vector2(0,55);titleBest.rectTransform.anchoredPosition=new Vector2(0,26);
                titleStartBaseY=-60;titleLeafL.anchoredPosition=new Vector2(-112,112);titleLeafR.anchoredPosition=new Vector2(112,112);
            }
            else
            {
                float panelW=Mathf.Min(420,Mathf.Max(334,w*.46f));
                float leftX=-w*.245f,rightX=w*.245f;
                statusRt.anchoredPosition=new Vector2(0,top);goalRt.anchoredPosition=new Vector2(0,top-57);
                lane0Rt.anchoredPosition=new Vector2(leftX-88,top-111);lane1Rt.anchoredPosition=new Vector2(leftX+88,top-111);
                problemRt.anchoredPosition=new Vector2(leftX,-65);trackRt.anchoredPosition=new Vector2(rightX,-50);
                problemRt.sizeDelta=new Vector2(panelW,250);trackRt.sizeDelta=new Vector2(panelW,160);
                toastRt.anchoredPosition=new Vector2(rightX,-h*.5f+48);
                titleSignRt.anchoredPosition=new Vector2(0,h*.5f-112);
                titleMeta.rectTransform.anchoredPosition=new Vector2(0,18);titleBest.rectTransform.anchoredPosition=new Vector2(0,-9);
                titleStartBaseY=-82;titleLeafL.anchoredPosition=new Vector2(-112,42);titleLeafR.anchoredPosition=new Vector2(112,42);
            }
            float problemTextW=Mathf.Max(300,problemRt.sizeDelta.x-26);
            promptTxt.rectTransform.sizeDelta=new Vector2(problemTextW,112);evidenceTxt.rectTransform.sizeDelta=new Vector2(problemTextW,48);
            diagramTxt.rectTransform.sizeDelta=new Vector2(problemTextW,58);
        }

        void SetScreen()
        {
            bool title=phase==Phase.Title,end=phase==Phase.End,play=!title&&!end;
            titleG.alpha=title?1:0;titleG.blocksRaycasts=title;hudG.alpha=play?1:0;hudG.blocksRaycasts=play;
            endG.alpha=end?1:0;endG.blocksRaycasts=end;revealG.alpha=phase==Phase.Reveal?1:0;
            bool guide=phase==Phase.Practice||(phase==Phase.Playing&&guideShow>0);
            guideRt.gameObject.SetActive(guide);guidePathRt.gameObject.SetActive(guide);
            titleSatchelWorld.gameObject.SetActive(title);
            RefreshHudImmediate();RefreshLaneUi();
        }

        void ResetWorldForRun()
        {
            revealWorld=0;flowerOpen=0;worldCorrect=false;wrongShake=0;hitStop=0;
            for(int i=0;i<hooks.Length;i++){hooks[i].gameObject.SetActive(true);hooks[i].localPosition=new Vector3(7.8f,2.55f-i*.82f,1.4f);hooks[i].localRotation=Quaternion.Euler(90,0,0);}
            for(int i=0;i<pollen.Length;i++)pollen[i].localPosition=new Vector3(0,-20,0);
            for(int i=0;i<spiders.Length;i++){spiders[i].localPosition=new Vector3(-4.4f+i*.62f,.42f,3.0f+(i%2)*.24f);spiders[i].localRotation=Quaternion.identity;spiders[i].localScale=Vector3.one*(i==0?1.2f:.82f);}
            for(int i=0;i<petals.Length;i++)petals[i].localScale=Vector3.one*.72f;
            SetWorldSilk(false,0);dragTrail.enabled=false;lightRunner.enabled=false;
        }

        void ResetWorldForProblem(SilkProblem p)
        {
            revealWorld=0;worldCorrect=false;wrongShake=0;hitStop=0;lightRunner.enabled=false;dragTrail.enabled=false;
            for(int i=0;i<spiders.Length;i++)
            {
                float lane=(activeLane==0?2.75f:4.65f);spiders[i].localPosition=new Vector3(-4.45f+i*.52f,.42f,lane+(i%2)*.16f);
                spiders[i].localRotation=Quaternion.identity;
            }
            SetWorldSilk(false,st.selectedLength);
        }

        void SetWorldSilk(bool taut,float progress)
        {
            Vector3 a=new Vector3(-4.75f,2.05f,activeLane==0?2.75f:4.55f),b=new Vector3(4.75f,2.15f,activeLane==0?3.15f:4.95f);
            for(int i=0;i<silkLine.positionCount;i++)
            {
                float t=i/(float)(silkLine.positionCount-1);Vector3 p=Vector3.Lerp(a,b,t);
                if(!taut)p.y-=Mathf.Sin(t*Mathf.PI)*(1.25f-progress/24f*.28f);
                silkLine.SetPosition(i,p);
            }
            silkTip.position=Vector3.Lerp(a,b,Mathf.Clamp01(progress/24f));
        }

        void SetSilkLength(int value)
        {
            if(handleRt)handleRt.anchoredPosition=new Vector2(-154+(value-1)*(308f/23f),10);
            if(silkLine)SetWorldSilk(false,value);
        }

        void UpdateWorld(float dt)
        {
            Layout();worldClock+=dt;titleClock+=dt;
            cam.orthographicSize=landscape?8.6f:Mathf.Clamp(7.0f/Mathf.Max(.35f,cam.aspect),12.4f,15.8f);
            cameraBase=landscape?new Vector3(12.5f,13.4f,-16.2f):new Vector3(10.8f,14.8f,-17.8f);
            cam.transform.position=cameraBase;cam.transform.LookAt(landscape?new Vector3(0,.6f,3.6f):new Vector3(0,.8f,3.8f));

            for(int i=0;i<leaves.Length;i++)if(leaves[i])leaves[i].localRotation=Quaternion.Euler(18,(i%2==0?-1:1)*20,(i%2==0?-1:1)*(35+i%4*6+Mathf.Sin(worldClock*1.1f+i)*4));
            for(int i=0;i<dew.Length;i++)if(dew[i])dew[i].localScale=Vector3.one*(.27f+Mathf.Sin(worldClock*1.5f+i)*.025f);
            for(int i=0;i<hangingVines.Length;i++)if(hangingVines[i])hangingVines[i].localRotation=Quaternion.Euler(0,Mathf.Sin(worldClock*.55f+i)*4,Mathf.Sin(worldClock*.72f+i*.8f)*2.5f);
            for(int i=0;i<fireflies.Length;i++)if(fireflies[i])
            {
                var p=fireflies[i].localPosition;p.y+=Mathf.Sin(worldClock*1.7f+i)*.0015f;p.x+=Mathf.Cos(worldClock*.8f+i)*.0008f;fireflies[i].localPosition=p;
                fireflies[i].localScale=Vector3.one*(.10f+Mathf.Sin(worldClock*2.3f+i)*.025f);
            }
            if(phase==Phase.Title)
            {
                for(int i=0;i<spiders.Length;i++){var p=spiders[i].localPosition;p.x+=dt*(.18f+i*.008f);if(p.x>4.8f)p.x=-4.8f;spiders[i].localPosition=p;p.y=.42f+Mathf.Abs(Mathf.Sin(worldClock*3+i))*.08f;}
                titleSatchelWorld.localScale=Vector3.one*(1+Mathf.Sin(worldClock*2.2f)*.06f);
            }
            else if(phase!=Phase.Reveal)
            {
                for(int i=0;i<spiders.Length;i++)
                {
                    var p=spiders[i].localPosition;p.y=.42f+Mathf.Abs(Mathf.Sin(worldClock*4+i*.8f))*.06f;spiders[i].localPosition=p;
                    spiderHeads[i].localRotation=Quaternion.Euler(0,Mathf.Sin(worldClock*2+i)*8,0);
                }
            }
            if(phase==Phase.Reveal)
            {
                if(hitStop>0){hitStop=Mathf.Max(0,hitStop-dt);}
                else revealWorld+=dt;
                float u=Mathf.Clamp01(revealWorld/1.35f);
                if(worldCorrect)
                {
                    SetWorldSilk(true,24);float travel=Mathf.SmoothStep(0,1,Mathf.Clamp01((u-.12f)/.76f));
                    Vector3 a=new Vector3(-4.75f,2.05f,activeLane==0?2.75f:4.55f),b=new Vector3(4.75f,2.15f,activeLane==0?3.15f:4.95f);
                    int count=Mathf.Min(spiders.Length,st.combo>=3?5:3);
                    for(int i=0;i<count;i++)
                    {
                        float q=Mathf.Clamp01(travel-i*.10f);spiders[i].position=Vector3.Lerp(a,b,q)+Vector3.up*Mathf.Sin(q*Mathf.PI)*.18f;
                        float stretch=1+Mathf.Sin(Mathf.Clamp01(q/.18f)*Mathf.PI)*.16f;spiders[i].localScale=new Vector3(1/stretch,stretch,1)*(i==0?1.15f:.82f);
                    }
                    lightRunner.enabled=true;for(int j=0;j<4;j++){float q=Mathf.Clamp01(travel-j*.035f);lightRunner.SetPosition(j,Vector3.Lerp(a,b,q));}
                    int motes=Mathf.Min(pollen.Length,4+st.combo*2);for(int i=0;i<motes;i++){float q=Mathf.Clamp01((u-.45f)*1.8f);pollen[i].position=b+new Vector3(Mathf.Sin(i*2.4f)*.9f*q,.2f+q*(.4f+i*.08f),Mathf.Cos(i*1.7f)*.65f*q);}
                    flowerOpen=Mathf.Max(flowerOpen,Mathf.Clamp01((u-.52f)/.38f));for(int i=5;i<10;i++)petals[i].localScale=Vector3.one*Mathf.Lerp(.72f,1.08f+((i-5)%2)*.08f,flowerOpen);
                }
                else
                {
                    float rewind=Mathf.SmoothStep(0,1,u);SetWorldSilk(false,Mathf.Lerp(st.selectedLength,0,rewind));
                    if(spiders[0]){spiders[0].localRotation=Quaternion.Euler(0,0,Mathf.Sin(u*Mathf.PI)*32);spiderHeads[0].localScale=Vector3.one*(1+Mathf.Sin(u*Mathf.PI)*.18f);}
                    if(wrongShake>0){wrongShake=Mathf.Max(0,wrongShake-dt);float m=.06f*(wrongShake/.35f);cam.transform.position=cameraBase+new Vector3(Mathf.Sin(worldClock*66)*m,Mathf.Cos(worldClock*59)*m,0);cam.transform.LookAt(new Vector3(0,.8f,3.8f));}
                }
            }
        }

        void UpdateUi(float dt)
        {
            displayScore=Mathf.MoveTowards(displayScore,st.score,Mathf.Max(80,Mathf.Abs(st.score-displayScore)*6)*dt);
            int ds=Mathf.RoundToInt(displayScore);if(ds!=shownScore){shownScore=ds;scoreTxt.text=ds.ToString();scoreTxt.rectTransform.localScale=Vector3.one*1.14f;}else scoreTxt.rectTransform.localScale=Vector3.Lerp(scoreTxt.rectTransform.localScale,Vector3.one,dt*10);
            int sec=Mathf.CeilToInt(runLeft);if(sec!=shownSecond){shownSecond=sec;timerTxt.text="";progressTxt.text=st.solved+" / "+SilkRules.TargetRoutes+" · "+sec+"초";}
            if(phase==Phase.Title)
            {
                float bob=Mathf.Sin(titleClock*1.6f)*4;titleStartRt.anchoredPosition=new Vector2(0,titleStartBaseY+bob-titlePush);
                titleSatchelImg.rectTransform.localScale=Vector3.one*(1+Mathf.Sin(titleClock*2.6f)*.045f);
                titleLeafL.localRotation=Quaternion.Euler(0,0,-4+Mathf.Sin(titleClock*.7f)*1.5f);titleLeafR.localRotation=Quaternion.Euler(0,0,4-Mathf.Sin(titleClock*.7f)*1.5f);
            }
            if(phase==Phase.Playing&&guideShow>0)
            {
                guideShow-=dt;
                if(guideShow<=0){guideRt.gameObject.SetActive(false);guidePathRt.gameObject.SetActive(false);}
            }
            if(guideRt.gameObject.activeSelf)
            {
                // 유령 손가락은 지금 실 끝(손잡이)에서 출발해 옆 눈금 쪽으로 당기는 동작을 반복한다.
                guideClock+=dt*(guideBoost>0?1.8f:1);guideBoost=Mathf.Max(0,guideBoost-dt);float q=(guideClock%1.2f)/1.2f;
                float hx=handleRt.anchoredPosition.x,dir=st.selectedLength>=20?-1:1;
                guideRt.anchoredPosition=new Vector2(hx+dir*q*40,28+Mathf.Sin(q*Mathf.PI)*10);
                guideImg.color=new Color(Ivory.r,Ivory.g,Ivory.b,.35f+Mathf.Sin(q*Mathf.PI)*.65f);
                guidePathRt.anchoredPosition=new Vector2(hx+dir*24,5);
                guidePathImg.rectTransform.sizeDelta=new Vector2(40+q*8,6);
                handlePulse=Mathf.Max(handlePulse,.2f);
            }
            handlePulse=Mathf.Max(0,handlePulse-dt);float hs=1+(handlePulse>0?Mathf.Sin(worldClock*15)*.10f:0);handleRt.localScale=Vector3.Lerp(handleRt.localScale,Vector3.one*hs,dt*13);
            if(toastClock>0){toastClock-=dt;toastG.alpha=Mathf.Clamp01(toastClock*4);toastRt.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01(toastClock/.32f)*Mathf.PI)*.04f);}else toastG.alpha=0;
            if(rippleClock>0){rippleClock-=dt;float q=1-Mathf.Clamp01(rippleClock/.34f);rippleRt.localScale=Vector3.one*Mathf.Lerp(.25f,1.35f,q);rippleImg.color=new Color(Apricot.r,Apricot.g,Apricot.b,(1-q)*.72f);}else rippleImg.color=new Color(0,0,0,0);
            if(phase==Phase.End){endDisplay=Mathf.MoveTowards(endDisplay,st.score,Mathf.Max(100,st.score*1.4f)*dt);endScoreTxt.text=Mathf.RoundToInt(endDisplay).ToString();endScoreTxt.rectTransform.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01(endDisplay/Mathf.Max(1,st.score))*Mathf.PI)*.08f);}
            RefreshLaneWaitOnly();
        }

        void RefreshProblemUi()
        {
            if(current==null)return;
            bool three=current.kind==SilkKind.ThreeParallel;
            promptTxt.text=KeepUnits(current.prompt);
            promptTxt.fontSize=three?12.5f:14.5f;
            diagramTxt.gameObject.SetActive(false);tpDiagramRt.gameObject.SetActive(three);triDiagramRt.gameObject.SetActive(!three);
            if(three)
            {
                bool upperUnknown=current.target.Contains("l-m");
                tpLuTxt.text=current.a+" cm";tpLlTxt.text=current.b+" cm";
                tpRuTxt.text=upperUnknown?"? cm":current.c+" cm";
                tpRlTxt.text=upperUnknown?current.c+" cm":"? cm";
                tpRuTxt.color=upperUnknown?Apricot:Ivory;tpRlTxt.color=upperUnknown?Ivory:Apricot;
                liveTargetTxt=upperUnknown?tpRuTxt:tpRlTxt;liveTargetSeg=null;liveTargetName="";
            }
            else RefreshTriangleDiagram();
            evidenceTxt.text=current.evidence;
            targetKnotTxt.text=three?current.target.Replace(" 구간","")+" 실":current.target+"에 잇는 실";
            RefreshLengthUi(false);RefreshHudImmediate();
        }

        void RefreshLengthUi(bool animate)
        {
            if(lengthTxt)lengthTxt.text=st.selectedLength+" cm";
            // 실 끝을 당기는 동안 모식도의 구할 선분 위에 지금 길이가 실시간으로 붙는다.
            if(liveTargetTxt&&current!=null)liveTargetTxt.text=(liveTargetName.Length>0?liveTargetName+" ":"")+st.selectedLength+" cm?";
            for(int i=0;i<tickDots.Length;i++)if(tickDots[i])tickDots[i].color=i+1==st.selectedLength?Apricot:(i%5==0?Gold:new Color(Ivory.r,Ivory.g,Ivory.b,.56f));
            if(animate){handlePulse=.24f;lengthTxt.rectTransform.localScale=Vector3.one*1.12f;}
        }

        void RefreshHudImmediate()
        {
            if(progressTxt)progressTxt.text=st.solved+" / "+SilkRules.TargetRoutes+" · "+Mathf.CeilToInt(runLeft)+"초";
            if(livesTxt)livesTxt.text=st.lives>=3?"◆ ◆ ◆":st.lives==2?"◆ ◆ ◇":st.lives==1?"◆ ◇ ◇":"◇ ◇ ◇";
            if(titleBest){int best=PlayerPrefs.GetInt("sil-taneun-geomi.best",0);titleBest.text=best>0?"최고 첫 시도 "+best+" / 9":"안전 고리 3개 · 꽃길 9개";}
        }

        void RefreshLaneUi()
        {
            bool two=phase!=Phase.Title&&phase!=Phase.End&&st.solved>=3;
            lane0Rt.gameObject.SetActive(two);lane1Rt.gameObject.SetActive(two);
            if(!two)return;RefreshLaneWaitOnly();
        }

        void RefreshLaneWaitOnly()
        {
            if(!lane0Rt||!lane0Rt.gameObject.activeSelf)return;
            lane0Txt.text=lanes[0].present?"꽃길 A  "+Mathf.CeilToInt(lanes[0].waitLeft)+"초\n"+lanes[0].problem.target+" = ? cm":"꽃길 A  배달 완료";
            lane1Txt.text=lanes[1].present?"꽃길 B  "+Mathf.CeilToInt(lanes[1].waitLeft)+"초\n"+lanes[1].problem.target+" = ? cm":"꽃길 B  배달 완료";
            lane0Bg.color=activeLane==0?Apricot:new Color(Mint.r,Mint.g,Mint.b,.96f);lane1Bg.color=activeLane==1?Apricot:new Color(Mint.r,Mint.g,Mint.b,.96f);
        }

        void BeginRevealVisual(bool correct,bool wasFirst)
        {
            worldCorrect=correct;revealWorld=0;hitStop=correct?.080f:0;wrongShake=correct?0:.36f;
            revealTitleTxt.text=correct?"실이 팽팽해졌다":"실이 되감겼다";
            revealFormulaTxt.text=correct?current.formula+"\n<size=76%>거미가 "+current.target+" 꽃길을 건넌다</size>":WrongExplanation(st.misconceptionId)+"\n<size=76%>올바른 길이 "+current.answer+" cm</size>";
            revealBg.color=correct?new Color(.90f,1f,.92f,.98f):new Color(1f,.91f,.85f,.98f);
            if(correct){MgfSfx.Play("whoosh",.44f);MgfFx.Glow(new Vector3(4.7f,2.15f,activeLane==0?3.15f:4.95f),Gold,10+st.combo*2,.42f);}
            else{if(!revealWasPractice)DropSafetyHook();MgfSfx.Play("wrong",.46f);}
        }

        void CompleteRevealVisual(bool correct)
        {
            revealG.alpha=0;lightRunner.enabled=false;ResetSilkTrail();
            if(correct)MgfSfx.Play("correct",.45f);
        }

        void DropSafetyHook()
        {
            int idx=Mathf.Clamp(2-st.lives,0,2);if(idx<hooks.Length&&hooks[idx]){hooks[idx].localPosition+=new Vector3(.4f,-2.4f,.2f);hooks[idx].localRotation=Quaternion.Euler(30,0,75);}
        }

        string WrongExplanation(string id)
        {
            if(id=="part_part_for_whole")return "부분 AD가 아니라 전체 AB와 대응시켜야 한다";
            if(id=="reversed_correspondence")return "같은 위치의 두 구간 순서를 맞추어야 한다";
            if(id=="midpoint_same_side"||id=="midpoint_double_side")return "중점연결선분은 나머지 변 길이의 1/2이다";
            if(id=="additive_parallel_gap")return "차를 더하지 말고 대응 구간의 비를 같게 둔다";
            if(id=="queue_timeout")return "남은 시간이 짧은 꽃길을 먼저 연결해야 한다";
            // 오답 뒤 눈금은 무작위 위치로 되돌아가므로 판정은 반드시 제출 순간의 길이로 한다.
            if(id=="silk_too_short")return submittedLength+" cm는 필요한 길이보다 짧았다";
            if(id=="silk_too_long")return submittedLength+" cm는 필요한 길이보다 길었다";
            return submittedLength<current.answer?submittedLength+" cm는 필요한 길이보다 짧았다":submittedLength+" cm는 필요한 길이보다 길었다";
        }

        void ShowWrongReason(string id){ShowToast(WrongExplanation(id)+" · 같은 길을 다시 뽑으시오");ReplayGuide(true);ShowGuide(2.6f);}
        void ShowGuide(float seconds)
        {
            if(phase!=Phase.Playing&&phase!=Phase.Practice)return;
            guideShow=Mathf.Max(guideShow,seconds);guideClock=0;guideBoost=1.2f;
            guideRt.gameObject.SetActive(true);guidePathRt.gameObject.SetActive(true);
        }
        void ShowEnd(string reason)
        {
            endDisplay=0;endTitleTxt.text=reason=="clear"?"온실 배달 완료":"꽃길 배달 중단";
            string why=reason=="time"?"120초가 모두 지났다":reason=="hooks"?"안전 고리가 모두 떨어졌다":reason=="mastery"?"첫 시도 7길 목표에 도달할 수 없다":"";
            endStatsTxt.text="연결한 꽃길 "+st.solved+" / 9\n첫 시도 "+st.firstAttemptCorrect+" / "+Math.Max(1,st.firstAttemptTotal)+(why.Length>0?"\n"+why:"");
        }

        void ReplayGuide(bool strong){guideClock=0;guideBoost=strong?1.2f:.45f;handlePulse=strong?.9f:.35f;}
        void RefuseInput(string message){handlePulse=.7f;ReplayGuide(true);ShowGuide(2.6f);ShowToast(message);MgfSfx.Play("tap",.16f);SpiderWave(activeLane);}
        void ShowToast(string message){toastTxt.text=message;toastClock=2.0f;toastG.alpha=1;}
        void SpiderWave(int lane){if(spiders[0])spiderHeads[0].localRotation=Quaternion.Euler(-18,0,lane==0?-12:12);}
        void SpawnTapRipple(Vector2 screen){RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt,screen,null,out var local);rippleRt.anchoredPosition=local;rippleClock=.34f;}
        void HandleGrab(bool on){handleImg.color=on?Apricot:Plum;handleRt.localScale=on?new Vector3(1.18f,.86f,1):Vector3.one;}
        void TickSilk(){MgfSfx.Play("tap",.07f);handlePulse=.11f;}
        void UpdateSilkTrail()
        {
            dragTrail.enabled=true;Vector3 p=silkTip.position;for(int i=0;i<dragTrail.positionCount;i++)dragTrail.SetPosition(i,p+Vector3.left*(i*.18f)+Vector3.up*Mathf.Sin(i*.7f+worldClock*5)*.05f);
        }
        void ResetSilkTrail(){dragTrail.enabled=false;}
        void BeginTitlePress(){titlePush=8;titleSatchelImg.color=Ivory;titleLeafL.anchoredPosition+=Vector2.left*8;titleLeafR.anchoredPosition+=Vector2.right*8;}
        void UpdateTitlePress(Vector2 screen){titlePush=Mathf.Clamp(8+(Screen.height*.5f-screen.y)*.05f,4,20);}
        void EndTitlePress(){titlePush=0;titleSatchelImg.color=Apricot;MgfSfx.Play("whoosh",.36f);}

        bool IsTitleStartZone(Vector2 p)=>RectTransformUtility.RectangleContainsScreenPoint(titleStartRt,p,null);
        // 계량 실 띠: 눈금 카드보다 위 46·아래 120(논리 px) 넓게, 좌우는 카드 폭 +40.
        bool IsTrackZone(Vector2 p)
        {
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRt,p,null,out var local))return false;
            var r=trackRt.rect;return Mathf.Abs(local.x)<=r.width*.5f+40f&&local.y<=r.height*.5f+46f&&local.y>=-r.height*.5f-120f;
        }
        int TicksBetween(Vector2 a,Vector2 b)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRt,a,null,out var la);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRt,b,null,out var lb);
            return Mathf.RoundToInt((lb.x-la.x)/(308f/23f));
        }
        int LaneFromPointer(Vector2 p)
        {
            if(lane0Rt.gameObject.activeSelf&&RectTransformUtility.RectangleContainsScreenPoint(lane0Rt,p,null))return 0;
            if(lane1Rt.gameObject.activeSelf&&RectTransformUtility.RectangleContainsScreenPoint(lane1Rt,p,null))return 1;return -1;
        }
        int LengthFromPointer(Vector2 p)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRt,p,null,out var local);float u=Mathf.Clamp01((local.x+154)/308f);return Mathf.Clamp(1+Mathf.RoundToInt(u*23),1,24);
        }
        bool IsReleaseCancelled(Vector2 p)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRt,p,null,out var local);return local.y>trackRt.rect.height*.5f+110f||local.y<-trackRt.rect.height*.5f-190f;
        }
        static string KeepUnits(string text)=>System.Text.RegularExpressions.Regex.Replace(text,@"(\d+) cm","<nobr>$1 cm</nobr>");
    }
}
