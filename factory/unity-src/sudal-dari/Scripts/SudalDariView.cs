// 다리 놓는 수달 — 따뜻한 점토 강마을 2.5D 월드와 UI.
// 정답은 물줄기→풀 충전→판자 펴짐→수달 건너기, 오답은 마른 칸/넘침→판자 침하로 구분한다.
// 정답 경로에는 화면 흔들림을 쓰지 않는다. 파티클 버스트와 점수 팝업도 쓰지 않는다.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.SudalDari
{
    public partial class SudalDariGame
    {
        static readonly Color SkyBlue = MgfLook.Hex("#CFEEF0");
        static readonly Color Turquoise = MgfLook.Hex("#2EC4B6");
        static readonly Color Apricot = MgfLook.Hex("#F4A259");
        static readonly Color Wood = MgfLook.Hex("#8B5E3C");
        static readonly Color Cream = MgfLook.Hex("#FFF4E0");
        static readonly Color Ink = MgfLook.Hex("#173A3D");
        static readonly Color Clay = MgfLook.Hex("#B96F4B");
        static readonly Color Moss = MgfLook.Hex("#7BAE72");
        static readonly Color Amber = MgfLook.Hex("#E88636");

        Camera cam;
        Vector3 cameraHome;
        Transform worldRoot, waterWheel, otterRoot, otterArm, otterHead, bridgeRoot, bridgePlank, bridgeHandle;
        Transform tankA, tankB, targetPool, poolWater, riverSurface, bankRight;
        readonly Transform[] reeds = new Transform[12];
        readonly Transform[] smoke = new Transform[7];
        readonly Transform[] bridgePosts = new Transform[8];
        readonly GameObject[] gridMarks = new GameObject[44];
        int gridMarkCount;
        LineRenderer arcA, arcB, plankTrail;
        Material waterMat, waterGlowMat, woodMat, wetWoodMat, clayMat, grassMat, creamMat, inkMat, orangeMat, dryMat;
        float worldClock, worldReveal, hitStop, wrongShake, handlePulse, titlePull;
        bool worldCorrect, worldTimeout;
        Vector3 otterHome, bankRightHome;

        CanvasGroup titleG, hudG, endG, revealG, toastG;
        RectTransform rootRt, titleRt, hudRt, endRt, titleHandleRt, titleGripRt;
        RectTransform topRt, goalRt, promptRt, infoRt, trackRt, handleRt, guideRt, guidePathRt, revealRt, toastRt, rippleRt;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleBest, titleStart;
        TextMeshProUGUI scoreTxt, progressTxt, livesTxt, timerTxt, goalTxt, promptTxt, leftTankTxt, rightTankTxt, targetTxt, lengthTxt;
        TextMeshProUGUI revealTitleTxt, revealFormulaTxt, toastTxt, endTitleTxt, endScoreTxt, endStatsTxt, endTapTxt;
        Image titleGripImg, handleImg, guideImg, guidePathImg, tideFillImg, revealBg, toastBg, rippleImg;
        readonly Image[] tickDots = new Image[25];
        readonly TextMeshProUGUI[] tickLabels = new TextMeshProUGUI[6];
        Sprite roundSprite, thinSprite, ringSprite;
        float titleClock, displayedScore, endDisplayedScore, revealUiClock, toastClock, rippleClock, guideClock, guideBoost;
        int shownScore = -1, shownSecond = -1, layoutW = -1, layoutH = -1;
        bool landscape;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var im = rt.gameObject.AddComponent<Image>();
            im.color = color; im.sprite = sprite; im.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color,
            float width = 360, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 1.9f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text;
            return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "SudalRing" : "SudalRound", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                if (ring)
                {
                    float dx = fx - size*.5f, dy = fy-size*.5f, d = Mathf.Sqrt(dx*dx+dy*dy);
                    a = Mathf.Clamp01(1f - Mathf.Abs(d-size*.40f)/(size*.065f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx-size*.5f)-(size*.5f-radius),0);
                    float qy = Mathf.Max(Mathf.Abs(fy-size*.5f)-(size*.5f-radius),0);
                    a = Mathf.Clamp01(radius-Mathf.Sqrt(qx*qx+qy*qy)+.8f);
                }
                px[y*size+x] = new Color32(255,255,255,(byte)(a*255));
            }
            tex.SetPixels32(px); tex.Apply(false,true);
            float b = ring ? 0 : radius+2;
            return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(b,b,b,b));
        }

        void BuildWorld()
        {
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<CapsuleCollider>(); colliderTypes.AddComponent<SphereCollider>();
            Destroy(colliderTypes);

            MgfLook.Sky(MgfLook.Hex("#A9DBE1"), SkyBlue, MgfLook.Hex("#F7D7A8"), .82f);
            MgfLook.Sun(new Vector3(48,-38,14), MgfLook.Hex("#FFF1D2"), 1.18f, .68f);
            cam = MgfLook.Camera(new Vector3(11.8f,10.6f,-14.4f),new Vector3(0,.4f,3.0f),34f);
            cam.orthographic = true; cam.orthographicSize = 8.0f; cameraHome = cam.transform.position;

            waterMat = MgfLook.Lit(new Color(Turquoise.r,Turquoise.g,Turquoise.b,.92f),.68f,.03f,MgfLook.Hex("#0A6F70")*.22f);
            waterGlowMat = MgfLook.Lit(MgfLook.Hex("#73E1D6"),.88f,.02f,MgfLook.Hex("#35C9BD")*.72f);
            woodMat = MgfLook.Lit(Wood,.18f,.02f);
            wetWoodMat = MgfLook.Lit(MgfLook.Hex("#68422D"),.48f,.03f);
            clayMat = MgfLook.Lit(Clay,.08f,.02f);
            grassMat = MgfLook.Lit(Moss,.10f,.01f);
            creamMat = MgfLook.Lit(Cream,.24f,.01f);
            inkMat = MgfLook.Lit(Ink,.12f,.08f);
            orangeMat = MgfLook.Lit(Apricot,.24f,.01f,Apricot*.08f);
            dryMat = MgfLook.Lit(MgfLook.Hex("#D9B37A"),.04f,.01f);

            worldRoot = new GameObject("ClayWaterwayVillage").transform;
            MgfLook.Block("ValleyBase",new Vector3(0,-1.1f,3.5f),new Vector3(20,1.6f,19),.55f,clayMat,worldRoot);
            MgfLook.Block("LeftBank",new Vector3(-5.0f,-.12f,3.0f),new Vector3(7.1f,1.15f,15.8f),.65f,grassMat,worldRoot);
            bankRight = MgfLook.Block("RightBank",new Vector3(5.0f,-.12f,3.0f),new Vector3(7.1f,1.15f,15.8f),.65f,grassMat,worldRoot).transform;
            bankRightHome = bankRight.position;
            riverSurface = MgfLook.Block("JellyRiver",new Vector3(0,-.25f,3.0f),new Vector3(3.4f,.20f,15.2f),.25f,waterMat,worldRoot).transform;
            Destroy(riverSurface.GetComponent<Collider>());

            // 낮은 폴리 산 실루엣과 아침 안개 — 회화 배경 없이 코드 프리미티브만 사용.
            var mountainMat = MgfLook.Lit(MgfLook.Hex("#8FB8A2"),.03f,0);
            for (int i=0;i<7;i++)
            {
                var m=MgfLook.Prim(PrimitiveType.Cylinder,"MistHill"+i,new Vector3(-9+i*3.1f,1.0f,12.0f+(i%2)),new Vector3(2.8f,2.4f+(i%3)*.5f,2.8f),mountainMat,worldRoot,false);
                m.transform.localRotation=Quaternion.Euler(0,i*17,0);
            }

            BuildVillage();
            BuildTanksAndBridge();
            BuildOtter();
            BuildWaterArcs();
        }

        void BuildVillage()
        {
            waterWheel = new GameObject("WaterWheel").transform; waterWheel.SetParent(worldRoot,false); waterWheel.localPosition=new Vector3(5.6f,1.3f,6.7f);
            MgfLook.Prim(PrimitiveType.Cylinder,"WheelHub",Vector3.zero,new Vector3(.42f,.26f,.42f),woodMat,waterWheel,false).transform.localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI*2/10;
                var spoke=MgfLook.Block("WheelPaddle"+i,new Vector3(Mathf.Cos(a)*1.0f,Mathf.Sin(a)*1.0f,0),new Vector3(.34f,.72f,.24f),.10f,woodMat,waterWheel);
                Destroy(spoke.GetComponent<Collider>()); spoke.transform.localRotation=Quaternion.Euler(0,0,-i*36);
            }
            for(int h=0;h<4;h++)
            {
                float x=h<2?-6.2f:5.7f, z=7.8f+(h%2)*2.2f;
                MgfLook.Block("ClayHouse"+h,new Vector3(x,.65f,z),new Vector3(1.5f,1.6f,1.5f),.3f,h%2==0?creamMat:orangeMat,worldRoot);
                var roof=MgfLook.Prim(PrimitiveType.Cylinder,"RoundRoof"+h,new Vector3(x,1.65f,z),new Vector3(1.1f,.45f,1.1f),woodMat,worldRoot,false);
                roof.transform.localRotation=Quaternion.Euler(0,22.5f,0);
                if(h<2)
                {
                    for(int s=0;s<2;s++) smoke[h*2+s]=MgfLook.Prim(PrimitiveType.Sphere,"FeltSmoke"+h+"_"+s,new Vector3(x+.4f,2.2f+s*.5f,z),new Vector3(.28f,.35f,.28f),creamMat,worldRoot,false).transform;
                }
            }
            for(int i=0;i<reeds.Length;i++)
            {
                int side=i%2==0?-1:1; float z=-2.0f+(i/2)*1.8f;
                reeds[i]=MgfLook.Prim(PrimitiveType.Capsule,"FeltReed"+i,new Vector3(side*1.9f,.45f,z),new Vector3(.12f,.72f,.12f),grassMat,worldRoot,false).transform;
                reeds[i].localRotation=Quaternion.Euler(0,0,side*(7+i%3*4));
            }
            // 마을 등불은 클리어 때 차례로 켜진다.
            for(int i=0;i<8;i++)
            {
                bridgePosts[i]=MgfLook.Prim(PrimitiveType.Sphere,"VillageLamp"+i,new Vector3(3.4f+(i%2)*1.4f,.75f,-2.2f+i*.9f),new Vector3(.22f,.22f,.22f),creamMat,worldRoot,false).transform;
                MgfLook.Block("LampPost"+i,new Vector3(bridgePosts[i].position.x,.25f,bridgePosts[i].position.z),new Vector3(.12f,.8f,.12f),.04f,woodMat,worldRoot);
            }
        }

        void BuildTanksAndBridge()
        {
            tankA=new GameObject("SquareTankA").transform; tankA.SetParent(worldRoot,false); tankA.localPosition=new Vector3(-5.3f,.65f,1.5f);
            tankB=new GameObject("SquareTankB").transform; tankB.SetParent(worldRoot,false); tankB.localPosition=new Vector3(-5.0f,.65f,5.0f);
            CreateTank(tankA,"A"); CreateTank(tankB,"B");
            targetPool=MgfLook.Block("HypotenusePool",new Vector3(3.8f,.18f,2.8f),new Vector3(3.0f,.36f,3.0f),.38f,creamMat,worldRoot).transform;
            poolWater=MgfLook.Block("PoolWater",new Vector3(3.8f,.40f,2.8f),new Vector3(2.62f,.10f,2.62f),.28f,waterMat,worldRoot).transform;
            Destroy(poolWater.GetComponent<Collider>());
            // 직각 표식은 항상 보이는 두 짧은 크림 막대다.
            MgfLook.Block("RightAngleA",new Vector3(-2.15f,.23f,1.05f),new Vector3(.62f,.12f,.12f),.04f,creamMat,worldRoot);
            MgfLook.Block("RightAngleB",new Vector3(-2.42f,.23f,1.33f),new Vector3(.12f,.12f,.62f),.04f,creamMat,worldRoot);
            bridgeRoot=new GameObject("StretchBridge").transform; bridgeRoot.SetParent(worldRoot,false); bridgeRoot.localPosition=new Vector3(-.05f,.82f,1.2f);
            bridgePlank=MgfLook.Block("WoodenPlank",Vector3.zero,new Vector3(4.0f,.34f,1.05f),.18f,woodMat,bridgeRoot).transform;
            bridgeHandle=MgfLook.Prim(PrimitiveType.Sphere,"OrangeRopeHandle",new Vector3(2.15f,.15f,0),new Vector3(.52f,.52f,.52f),orangeMat,bridgeRoot,true).transform;
            plankTrail=MakeLine("PlankGrainTrail",MgfLook.Alpha(new Color(Apricot.r,Apricot.g,Apricot.b,.25f)),.20f,3);
            plankTrail.enabled=false;
        }

        void CreateTank(Transform root,string id)
        {
            MgfLook.Block("TankFloor"+id,Vector3.zero,new Vector3(2.2f,.32f,2.2f),.25f,creamMat,root);
            var w=MgfLook.Block("TankWater"+id,new Vector3(0,.23f,0),new Vector3(1.82f,.18f,1.82f),.18f,waterMat,root);
            Destroy(w.GetComponent<Collider>());
            for(int side=0;side<4;side++)
            {
                Vector3 p=side<2?new Vector3(side==0?-1.02f:1.02f,.48f,0):new Vector3(0,.48f,side==2?-1.02f:1.02f);
                Vector3 s=side<2?new Vector3(.15f,.9f,2.15f):new Vector3(2.15f,.9f,.15f);
                MgfLook.Block("TankRim"+id+side,p,s,.07f,woodMat,root);
            }
        }

        void BuildOtter()
        {
            otterRoot=new GameObject("MoruOtterCarpenter").transform; otterRoot.SetParent(worldRoot,false); otterRoot.localPosition=new Vector3(5.7f,.45f,.1f); otterHome=otterRoot.localPosition;
            var fur=MgfLook.Lit(MgfLook.Hex("#9A6A48"),.18f,.01f); var dark=MgfLook.Lit(MgfLook.Hex("#583B2C"),.12f,.01f); var hat=MgfLook.Lit(MgfLook.Hex("#F4C542"),.24f,.02f);
            var body=MgfLook.Prim(PrimitiveType.Sphere,"RoundBody",new Vector3(0,.95f,0),new Vector3(1.02f,1.18f,.78f),fur,otterRoot,false); otterRoot.localScale=Vector3.one*1.18f;
            otterHead=MgfLook.Prim(PrimitiveType.Sphere,"RoundHead",new Vector3(0,2.02f,-.02f),new Vector3(.92f,.85f,.76f),fur,otterRoot,false).transform;
            MgfLook.Prim(PrimitiveType.Sphere,"Muzzle",new Vector3(0,1.88f,-.68f),new Vector3(.55f,.38f,.28f),creamMat,otterRoot,false);
            MgfLook.Prim(PrimitiveType.Sphere,"Nose",new Vector3(0,1.98f,-.92f),new Vector3(.16f,.13f,.10f),dark,otterRoot,false);
            for(int s=-1;s<=1;s+=2)
            {
                MgfLook.Prim(PrimitiveType.Sphere,"Ear"+s,new Vector3(s*.63f,2.44f,-.02f),new Vector3(.28f,.30f,.22f),dark,otterRoot,false);
                MgfLook.Prim(PrimitiveType.Sphere,"Eye"+s,new Vector3(s*.28f,2.17f,-.70f),new Vector3(.09f,.12f,.07f),inkMat,otterRoot,false);
                MgfLook.Prim(PrimitiveType.Capsule,"Foot"+s,new Vector3(s*.36f,.08f,-.08f),new Vector3(.25f,.40f,.28f),dark,otterRoot,false).transform.localRotation=Quaternion.Euler(90,0,0);
            }
            var brim=MgfLook.Prim(PrimitiveType.Cylinder,"HardHatBrim",new Vector3(0,2.62f,0),new Vector3(.83f,.12f,.83f),hat,otterRoot,false); brim.transform.localRotation=Quaternion.Euler(0,0,0);
            var crown=MgfLook.Prim(PrimitiveType.Sphere,"HardHatCrown",new Vector3(0,2.82f,0),new Vector3(.70f,.35f,.64f),hat,otterRoot,false);
            otterArm=MgfLook.Prim(PrimitiveType.Capsule,"HammerArm",new Vector3(-.72f,1.18f,-.05f),new Vector3(.22f,.62f,.22f),fur,otterRoot,false).transform;
            otterArm.localRotation=Quaternion.Euler(0,0,28);
            MgfLook.Block("ToyHammer",new Vector3(-1.06f,1.55f,-.04f),new Vector3(.62f,.28f,.26f),.10f,woodMat,otterRoot);
            var tail=MgfLook.Prim(PrimitiveType.Capsule,"WideTail",new Vector3(.72f,.52f,.28f),new Vector3(.32f,.82f,.18f),dark,otterRoot,false); tail.transform.localRotation=Quaternion.Euler(18,0,-48);
        }

        void BuildWaterArcs()
        {
            arcA=MakeLine("WaterArcA",MgfLook.Alpha(new Color(.35f,.95f,.92f,.90f)),.24f,20);
            arcB=MakeLine("WaterArcB",MgfLook.Alpha(new Color(.35f,.95f,.92f,.90f)),.24f,20);
            arcA.enabled=arcB.enabled=false;
        }

        LineRenderer MakeLine(string name,Material mat,float width,int points)
        {
            var go=new GameObject(name); go.transform.SetParent(worldRoot,false);
            var lr=go.AddComponent<LineRenderer>(); lr.sharedMaterial=mat; lr.positionCount=points; lr.startWidth=lr.endWidth=width;
            lr.numCapVertices=4; lr.numCornerVertices=3; lr.useWorldSpace=true; lr.shadowCastingMode=ShadowCastingMode.Off; lr.receiveShadows=false;
            return lr;
        }

        void BuildUi()
        {
            roundSprite=MakeSprite(64,15,false); thinSprite=MakeSprite(48,9,false); ringSprite=MakeSprite(64,12,true);
            var canvas=MgfText.Canvas;
            rootRt=R("SudalUi",canvas.transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);

            titleRt=R("TitleScreen",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero); titleG=titleRt.gameObject.AddComponent<CanvasGroup>();
            var ropeL=Img(titleRt,"BannerRopeL",new Vector2(.5f,.5f),new Vector2(-145,204),new Vector2(4,92),Wood,thinSprite);
            var ropeR=Img(titleRt,"BannerRopeR",new Vector2(.5f,.5f),new Vector2(145,204),new Vector2(4,92),Wood,thinSprite);
            var banner=Img(titleRt,"FeltBanner",new Vector2(.5f,.5f),new Vector2(0,150),new Vector2(336,154),new Color(Cream.r,Cream.g,Cream.b,.96f),roundSprite);
            Img(banner.transform,"BannerStitch",new Vector2(.5f,.5f),Vector2.zero,new Vector2(316,134),new Color(Wood.r,Wood.g,Wood.b,.18f),ringSprite);
            titleLogo=Txt(banner.transform,"다리 놓는\n<color=#E88636>수달</color>",new Vector2(.5f,.5f),new Vector2(0,18),42,Ink,310); titleLogo.fontStyle=FontStyles.Bold;
            titleTag=Txt(banner.transform,"물의 넓이로 다리를 잇다",new Vector2(.5f,0),new Vector2(0,23),15,Wood,300);
            titleMeta=Txt(titleRt,"중학교 2학년 · 피타고라스 정리",new Vector2(.5f,.5f),new Vector2(0,47),14,Ink,320);
            titleBest=Txt(titleRt,"",new Vector2(.5f,.5f),new Vector2(0,17),13,Wood,280);
            // The title plank is the sole CTA.  Keep its grab zone around the
            // visible world object broad enough for an imprecise first touch.
            titleHandleRt=R("WorldPlankStart",titleRt,new Vector2(.5f,.5f),new Vector2(0,-105),new Vector2(360,230));
            Img(titleHandleRt,"TitlePlank",new Vector2(.5f,.5f),new Vector2(-34,0),new Vector2(250,26),Wood,thinSprite);
            titleGripRt=Img(titleHandleRt,"TitleGrip",new Vector2(.5f,.5f),new Vector2(100,0),new Vector2(76,76),Apricot,ringSprite).rectTransform;
            titleGripImg=titleGripRt.GetComponent<Image>();
            titleStart=Txt(titleRt,"판자를 누르거나 끌어 시작",new Vector2(.5f,.5f),new Vector2(0,-252),18,Cream,350);

            hudRt=R("GameHud",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero); hudG=hudRt.gameObject.AddComponent<CanvasGroup>();
            topRt=Img(hudRt,"TopBar",new Vector2(.5f,1),new Vector2(0,-32),new Vector2(360,54),new Color(Cream.r,Cream.g,Cream.b,.96f),thinSprite).rectTransform;
            scoreTxt=Txt(topRt,"0",new Vector2(0,.5f),new Vector2(45,0),19,Ink,80,TextAlignmentOptions.Left);
            progressTxt=Txt(topRt,"0 / 8",new Vector2(.5f,.5f),Vector2.zero,18,Wood,90);
            livesTxt=Txt(topRt,"● ● ●",new Vector2(1,.5f),new Vector2(-62,0),18,Amber,118,TextAlignmentOptions.Right);
            timerTxt=Txt(topRt,"",new Vector2(1,.5f),new Vector2(-18,-18),11,Ink,55,TextAlignmentOptions.Right);
            goalRt=Img(hudRt,"GoalRibbon",new Vector2(.5f,1),new Vector2(0,-88),new Vector2(360,44),new Color(Turquoise.r,Turquoise.g,Turquoise.b,.92f),thinSprite).rectTransform;
            goalTxt=Txt(goalRt,"판자 길이를 정해 두 탱크의 물로 다리를 완성하라",new Vector2(.5f,.5f),Vector2.zero,15,Ink,344);
            promptRt=Img(hudRt,"ProblemScroll",new Vector2(.5f,1),new Vector2(0,-161),new Vector2(360,92),new Color(Cream.r,Cream.g,Cream.b,.97f),roundSprite).rectTransform;
            promptTxt=Txt(promptRt,"",new Vector2(.5f,.5f),Vector2.zero,15,Ink,336);
            infoRt=R("TankInfo",hudRt,new Vector2(.5f,.5f),new Vector2(0,35),new Vector2(360,126));
            var aCard=Img(infoRt,"TankAInfo",new Vector2(0,.5f),new Vector2(86,0),new Vector2(158,88),new Color(.98f,.94f,.84f,.94f),roundSprite);
            var bCard=Img(infoRt,"TankBInfo",new Vector2(1,.5f),new Vector2(-86,0),new Vector2(158,88),new Color(.98f,.94f,.84f,.94f),roundSprite);
            leftTankTxt=Txt(aCard.transform,"",new Vector2(.5f,.5f),Vector2.zero,18,Wood,148);
            rightTankTxt=Txt(bCard.transform,"",new Vector2(.5f,.5f),Vector2.zero,18,Wood,148);
            targetTxt=Txt(infoRt,"",new Vector2(.5f,0),new Vector2(0,-17),16,Cream,350);

            trackRt=Img(hudRt,"LengthRail",new Vector2(.5f,0),new Vector2(0,98),new Vector2(360,142),new Color(.98f,.94f,.84f,.97f),roundSprite).rectTransform;
            Img(trackRt,"Rail",new Vector2(.5f,.5f),new Vector2(0,-6),new Vector2(314,12),Wood,thinSprite);
            for(int i=0;i<25;i++)
            {
                float x=-151+i*(302f/24f);
                tickDots[i]=Img(trackRt,"Tick"+(i+1),new Vector2(.5f,.5f),new Vector2(x,-6),new Vector2(i%5==4||i==0?5:3,i%5==4||i==0?24:14),Ink,thinSprite);
            }
            int[] nums={1,5,10,15,20,25};
            for(int i=0;i<nums.Length;i++)
            {
                float x=-151+(nums[i]-1)*(302f/24f);
                tickLabels[i]=Txt(trackRt,nums[i].ToString(),new Vector2(.5f,.5f),new Vector2(x,-38),12,Ink,35);
            }
            handleRt=Img(trackRt,"OrangePlankHandle",new Vector2(.5f,.5f),new Vector2(-151,17),new Vector2(62,62),Apricot,ringSprite).rectTransform;
            handleImg=handleRt.GetComponent<Image>();
            lengthTxt=Txt(trackRt,"1 cm",new Vector2(.5f,1),new Vector2(0,-25),27,Ink,180); lengthTxt.fontStyle=FontStyles.Bold;
            tideFillImg=Img(trackRt,"TideGauge",new Vector2(0,0),new Vector2(18,9),new Vector2(324,6),Turquoise,thinSprite); tideFillImg.rectTransform.pivot=new Vector2(0,.5f);
            guidePathRt=Img(trackRt,"GuidePath",new Vector2(.5f,.5f),new Vector2(0,18),new Vector2(210,5),new Color(Apricot.r,Apricot.g,Apricot.b,.65f),thinSprite).rectTransform;
            guidePathImg=guidePathRt.GetComponent<Image>();
            guideRt=Img(trackRt,"GuideHand",new Vector2(.5f,.5f),new Vector2(-82,30),new Vector2(54,54),new Color(Cream.r,Cream.g,Cream.b,.96f),ringSprite).rectTransform;
            guideImg=guideRt.GetComponent<Image>();

            revealRt=R("MathReveal",hudRt,new Vector2(.5f,.5f),Vector2.zero,new Vector2(364,154)); revealG=revealRt.gameObject.AddComponent<CanvasGroup>();
            revealBg=Img(revealRt,"RevealBg",new Vector2(.5f,.5f),Vector2.zero,new Vector2(354,144),new Color(Cream.r,Cream.g,Cream.b,.98f),roundSprite);
            revealTitleTxt=Txt(revealRt,"",new Vector2(.5f,.5f),new Vector2(0,34),22,Ink,330);
            revealFormulaTxt=Txt(revealRt,"",new Vector2(.5f,.5f),new Vector2(0,-22),19,Wood,332);

            toastRt=R("Toast",hudRt,new Vector2(.5f,0),new Vector2(0,262),new Vector2(340,56)); toastG=toastRt.gameObject.AddComponent<CanvasGroup>();
            toastBg=Img(toastRt,"ToastBg",new Vector2(.5f,.5f),Vector2.zero,new Vector2(330,52),new Color(Ink.r,Ink.g,Ink.b,.92f),roundSprite);
            toastTxt=Txt(toastRt,"",new Vector2(.5f,.5f),Vector2.zero,15,Cream,308);
            rippleRt=Img(hudRt,"TapRipple",new Vector2(0,0),Vector2.zero,new Vector2(70,70),new Color(Apricot.r,Apricot.g,Apricot.b,.0f),ringSprite).rectTransform;
            rippleImg=rippleRt.GetComponent<Image>();

            endRt=R("EndScreen",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero); endG=endRt.gameObject.AddComponent<CanvasGroup>();
            var endPanel=Img(endRt,"EndClaySign",new Vector2(.5f,.5f),Vector2.zero,new Vector2(354,390),new Color(Cream.r,Cream.g,Cream.b,.98f),roundSprite);
            endTitleTxt=Txt(endPanel.transform,"",new Vector2(.5f,1),new Vector2(0,-62),34,Ink,330);
            endScoreTxt=Txt(endPanel.transform,"0",new Vector2(.5f,.5f),new Vector2(0,70),56,Amber,300); endScoreTxt.fontStyle=FontStyles.Bold;
            endStatsTxt=Txt(endPanel.transform,"",new Vector2(.5f,.5f),new Vector2(0,-16),18,Wood,315);
            endTapTxt=Txt(endPanel.transform,"강마을을 눌러 다시 시작",new Vector2(.5f,0),new Vector2(0,48),18,Ink,315);
        }

        void SetScreen()
        {
            bool title=phase==Phase.Title, end=phase==Phase.End;
            titleG.alpha=title?1:0; titleG.blocksRaycasts=title;
            hudG.alpha=!title&&!end?1:0; hudG.blocksRaycasts=!title&&!end;
            endG.alpha=end?1:0; endG.blocksRaycasts=end;
            revealG.alpha=phase==Phase.Reveal?1:0;
            guideImg.enabled=guidePathImg.enabled=phase==Phase.Practice;
            RefreshHudImmediate();
        }

        void Layout()
        {
            if(layoutW==Screen.width&&layoutH==Screen.height)return;
            layoutW=Screen.width; layoutH=Screen.height; landscape=Screen.width>=1024;
            if(landscape)
            {
                topRt.anchorMin=topRt.anchorMax=new Vector2(.18f,1); topRt.anchoredPosition=new Vector2(0,-34); topRt.sizeDelta=new Vector2(400,54);
                goalRt.anchorMin=goalRt.anchorMax=new Vector2(.18f,1); goalRt.anchoredPosition=new Vector2(0,-91); goalRt.sizeDelta=new Vector2(400,44);
                promptRt.anchorMin=promptRt.anchorMax=new Vector2(.18f,1); promptRt.anchoredPosition=new Vector2(0,-169); promptRt.sizeDelta=new Vector2(400,100);
                infoRt.anchorMin=infoRt.anchorMax=new Vector2(.19f,.46f); infoRt.anchoredPosition=Vector2.zero;
                trackRt.anchorMin=trackRt.anchorMax=new Vector2(.78f,0); trackRt.anchoredPosition=new Vector2(0,103); trackRt.sizeDelta=new Vector2(420,148);
                toastRt.anchorMin=toastRt.anchorMax=new Vector2(.78f,0); toastRt.anchoredPosition=new Vector2(0,278);
                revealRt.anchorMin=revealRt.anchorMax=new Vector2(.76f,.52f); revealRt.anchoredPosition=Vector2.zero;
            }
            else
            {
                topRt.anchorMin=topRt.anchorMax=new Vector2(.5f,1); topRt.anchoredPosition=new Vector2(0,-32); topRt.sizeDelta=new Vector2(360,54);
                goalRt.anchorMin=goalRt.anchorMax=new Vector2(.5f,1); goalRt.anchoredPosition=new Vector2(0,-88); goalRt.sizeDelta=new Vector2(360,44);
                promptRt.anchorMin=promptRt.anchorMax=new Vector2(.5f,1); promptRt.anchoredPosition=new Vector2(0,-161); promptRt.sizeDelta=new Vector2(360,92);
                infoRt.anchorMin=infoRt.anchorMax=new Vector2(.5f,.5f); infoRt.anchoredPosition=new Vector2(0,35);
                trackRt.anchorMin=trackRt.anchorMax=new Vector2(.5f,0); trackRt.anchoredPosition=new Vector2(0,98); trackRt.sizeDelta=new Vector2(360,142);
                toastRt.anchorMin=toastRt.anchorMax=new Vector2(.5f,0); toastRt.anchoredPosition=new Vector2(0,262);
                revealRt.anchorMin=revealRt.anchorMax=new Vector2(.5f,.5f); revealRt.anchoredPosition=Vector2.zero;
            }
        }

        void ResetWorldForRun()
        {
            worldReveal=0; hitStop=0; wrongShake=0; titlePull=0;
            if(otterRoot) { otterRoot.localPosition=otterHome; otterRoot.localRotation=Quaternion.identity; }
            if(bankRight) bankRight.position=bankRightHome;
            if(poolWater) { poolWater.localScale=Vector3.one; poolWater.GetComponent<Renderer>().sharedMaterial=waterMat; }
            if(arcA) arcA.enabled=arcB.enabled=false;
            for(int i=0;i<bridgePosts.Length;i++) if(bridgePosts[i]) bridgePosts[i].GetComponent<Renderer>().sharedMaterial=creamMat;
        }

        void ResetWorldForProblem(BridgeProblem p)
        {
            worldReveal=0; hitStop=0; wrongShake=0;
            bridgeRoot.localRotation=Quaternion.identity; bridgeRoot.localPosition=new Vector3(-.05f,.82f,1.2f);
            bridgePlank.GetComponent<Renderer>().sharedMaterial=woodMat;
            poolWater.localScale=Vector3.one; poolWater.localPosition=new Vector3(3.8f,.40f,2.8f); poolWater.GetComponent<Renderer>().sharedMaterial=waterMat;
            arcA.enabled=arcB.enabled=false; plankTrail.enabled=false;
            otterRoot.localPosition=otterHome; otterRoot.localRotation=Quaternion.identity;
            bankRight.position=bankRightHome;
            bool grid=p!=null&&p.band<=1;
            BuildGridVisuals(grid?p.a:0,grid?p.b:0);
            if(p!=null)
            {
                float sa=.74f+Mathf.Min(12,p.a)*.035f, sb=.74f+Mathf.Min(12,p.b)*.035f;
                tankA.localScale=new Vector3(sa,1,sa); tankB.localScale=new Vector3(sb,1,sb);
            }
        }

        void BuildGridVisuals(int a,int b)
        {
            for(int i=0;i<gridMarks.Length;i++) if(gridMarks[i]) gridMarks[i].SetActive(false);
            gridMarkCount=0;
            if(a<=0||b<=0)return;
            AddTankGrid(tankA,Math.Min(a,12)); AddTankGrid(tankB,Math.Min(b,12));
        }

        void AddTankGrid(Transform tank,int n)
        {
            for(int i=1;i<n&&gridMarkCount+1<gridMarks.Length;i++)
            {
                float p=-.9f+i*(1.8f/n);
                GridMark(tank,new Vector3(p,.36f,0),new Vector3(.025f,.025f,1.78f));
                GridMark(tank,new Vector3(0,.36f,p),new Vector3(1.78f,.025f,.025f));
            }
        }

        GameObject GridMark(Transform parent,Vector3 pos,Vector3 size)
        {
            int index=gridMarkCount++;
            GameObject o=gridMarks[index];
            if(!o)
            {
                o=MgfLook.Block("AreaGrid"+index,pos,size,.004f,inkMat,parent); Destroy(o.GetComponent<Collider>()); gridMarks[index]=o;
            }
            o.transform.SetParent(parent,false); o.transform.localPosition=pos; o.transform.localScale=Vector3.one; o.SetActive(true); return o;
        }

        void SetPlankLength(int length)
        {
            if(!bridgePlank)return;
            float w=1.65f+length*.145f;
            bridgePlank.localScale=new Vector3(w/4.0f,1,1);
            bridgePlank.localPosition=new Vector3((w-4.0f)*.5f,0,0);
            bridgeHandle.localPosition=new Vector3(w-.0f,.15f,0);
            if(handleRt)
            {
                float x=-151+(length-1)*(302f/24f);
                handleRt.anchoredPosition=new Vector2(x,17);
            }
        }

        void UpdateWorld(float dt)
        {
            Layout(); worldClock+=dt; titleClock+=dt;
            float asp=Mathf.Max(.2f,cam.aspect);
            cam.orthographicSize=landscape?7.25f:Mathf.Clamp(6.3f/asp,11.8f,14.5f);
            cameraHome=landscape?new Vector3(11.8f,10.6f,-14.4f):new Vector3(10.4f,11.4f,-15.8f);
            cam.transform.position=cameraHome;
            cam.transform.LookAt(landscape?new Vector3(0,.45f,3):new Vector3(0,.65f,2.9f));

            if(waterWheel) waterWheel.localRotation=Quaternion.Euler(0,0,-worldClock*34f);
            for(int i=0;i<reeds.Length;i++) if(reeds[i]) reeds[i].localRotation=Quaternion.Euler(0,0,(i%2==0?-1:1)*(8+Mathf.Sin(worldClock*1.5f+i)*5));
            for(int i=0;i<smoke.Length;i++) if(smoke[i])
            {
                var p=smoke[i].localPosition; p.y=2.2f+((worldClock*.32f+i*.43f)%1.7f); p.x+=Mathf.Sin(worldClock*.7f+i)*dt*.05f; smoke[i].localPosition=p;
            }
            if(riverSurface) riverSurface.localScale=new Vector3(1+Mathf.Sin(worldClock*1.2f)*.012f,1,1+Mathf.Sin(worldClock*.9f)*.01f);
            if(otterArm&&phase!=Phase.Reveal) otterArm.localRotation=Quaternion.Euler(0,0,28+Mathf.Sin(worldClock*4.2f)*18);
            if(otterHead&&toastClock>0) otterHead.localRotation=Quaternion.Euler(0,Mathf.Sin(worldClock*7)*9,-8);

            if(phase==Phase.Reveal)
            {
                if(hitStop>0) hitStop-=dt; else worldReveal+=dt;
                float u=Mathf.Clamp01(worldReveal/1.55f);
                UpdateArcs(u);
                if(worldCorrect)
                {
                    float fill=Mathf.SmoothStep(.08f,1f,Mathf.Clamp01((u-.12f)/.62f));
                    poolWater.localScale=new Vector3(1,Mathf.Lerp(.12f,1.55f,fill),1);
                    poolWater.GetComponent<Renderer>().sharedMaterial=fill>.82f?waterGlowMat:waterMat;
                    float overshoot=1+Mathf.Sin(Mathf.Clamp01(u/.45f)*Mathf.PI)*.08f;
                    bridgeRoot.localScale=new Vector3(overshoot,Mathf.Lerp(.82f,1f,u),1);
                    if(u>.58f)
                    {
                        float cross=Mathf.SmoothStep(0,1,(u-.58f)/.42f);
                        otterRoot.localPosition=Vector3.Lerp(otterHome,new Vector3(-4.3f,.45f,1.15f),cross);
                        otterRoot.localPosition+=Vector3.up*Mathf.Abs(Mathf.Sin(cross*Mathf.PI*5))*.20f;
                    }
                    int lit=Mathf.Clamp(st.solved,0,bridgePosts.Length);
                    for(int i=0;i<lit;i++) bridgePosts[i].GetComponent<Renderer>().sharedMaterial=waterGlowMat;
                }
                else
                {
                    float fall=Mathf.SmoothStep(0,1,Mathf.Clamp01((u-.28f)/.55f));
                    bridgeRoot.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(0,st.selectedLength<current.answer?-14:10,fall));
                    bridgeRoot.localPosition=new Vector3(-.05f,Mathf.Lerp(.82f,.15f,fall),1.2f);
                    bridgePlank.GetComponent<Renderer>().sharedMaterial=wetWoodMat;
                    float ratio=Mathf.Clamp01((st.selectedLength*st.selectedLength)/(float)Math.Max(1,current.answer*current.answer));
                    poolWater.localScale=new Vector3(1,Mathf.Lerp(.10f,ratio<1?ratio:1.55f,fall),1);
                    if(st.selectedLength>current.answer) bankRight.position=bankRightHome+Vector3.right*Mathf.Sin(Mathf.Clamp01(u/.8f)*Mathf.PI)*.35f;
                    wrongShake=Mathf.Max(0,wrongShake-dt);
                    if(wrongShake>0)
                    {
                        float mag=.075f*(wrongShake/.45f); cam.transform.position=cameraHome+new Vector3(Mathf.Sin(worldClock*75)*mag,Mathf.Cos(worldClock*63)*mag,0);
                        cam.transform.LookAt(landscape?new Vector3(0,.45f,3):new Vector3(0,.65f,2.9f));
                    }
                }
            }
        }

        void UpdateArcs(float u)
        {
            if(!arcA.enabled)return;
            SetArc(arcA,new Vector3(-5.3f,1.8f,1.5f),new Vector3(3.4f,.7f,2.5f),u,3.6f);
            SetArc(arcB,new Vector3(-5.0f,1.8f,5.0f),new Vector3(3.4f,.7f,3.1f),u,3.2f);
        }

        void SetArc(LineRenderer lr,Vector3 from,Vector3 to,float reveal,float height)
        {
            int n=lr.positionCount;
            for(int i=0;i<n;i++)
            {
                float t=i/(float)(n-1), shown=Mathf.Min(t,Mathf.Clamp01(reveal*1.35f));
                float q=shown/Mathf.Max(.001f,Mathf.Clamp01(reveal*1.35f));
                Vector3 p=Vector3.Lerp(from,to,shown); p.y+=Mathf.Sin(shown*Mathf.PI)*height;
                lr.SetPosition(i,p);
            }
        }

        void UpdateUi(float dt)
        {
            float target=st.score;
            displayedScore=Mathf.MoveTowards(displayedScore,target,Mathf.Max(70,Mathf.Abs(target-displayedScore)*5)*dt);
            int ds=Mathf.RoundToInt(displayedScore);
            if(ds!=shownScore){shownScore=ds;scoreTxt.text=ds.ToString();float p=1+Mathf.Sin(Mathf.Clamp01(Mathf.Abs(target-displayedScore)/80)*Mathf.PI)*.12f;scoreTxt.rectTransform.localScale=Vector3.one*p;}
            else scoreTxt.rectTransform.localScale=Vector3.Lerp(scoreTxt.rectTransform.localScale,Vector3.one,dt*10);

            if(phase==Phase.Title)
            {
                float bob=Mathf.Sin(titleClock*1.5f)*4;
                titleRt.localRotation=Quaternion.Euler(0,0,Mathf.Sin(titleClock*.65f)*.45f);
                titleGripRt.localScale=Vector3.one*(1+Mathf.Sin(titleClock*3.2f)*.08f);
                titleGripRt.anchoredPosition=new Vector2(100+titlePull,bob);
            }
            if(phase==Phase.Practice)
            {
                guideClock+=dt*(guideBoost>0?1.8f:1f); guideBoost=Mathf.Max(0,guideBoost-dt);
                float q=(guideClock%1.35f)/1.35f;
                guideRt.anchoredPosition=new Vector2(Mathf.Lerp(-94,96,q),30+Mathf.Sin(q*Mathf.PI)*10);
                float a=Mathf.Sin(q*Mathf.PI); guideImg.color=new Color(Cream.r,Cream.g,Cream.b,.35f+.65f*a);
                handlePulse=Mathf.Max(handlePulse,guideBoost>0?.8f:0);
            }
            else guideClock=0;
            handlePulse=Mathf.Max(0,handlePulse-dt);
            float hs=1+(handlePulse>0?Mathf.Sin(worldClock*12)*.11f:.0f);
            handleRt.localScale=Vector3.Lerp(handleRt.localScale,Vector3.one*hs,dt*14);

            int sec=current!=null&&current.band>=2&&phase==Phase.Playing?Mathf.CeilToInt(Mathf.Max(0,tideLeft)):-1;
            if(sec!=shownSecond){shownSecond=sec;timerTxt.text=sec>=0?"밀물 "+sec+"초":"";}
            if(tideFillImg)
            {
                float ratio=current!=null&&current.band>=2?Mathf.Clamp01(tideLeft/SudalRules.TideSeconds):1;
                tideFillImg.rectTransform.sizeDelta=new Vector2(324*ratio,6);
                tideFillImg.color=ratio<.28f?Amber:Turquoise;
            }
            if(revealG.alpha>0)
            {
                revealUiClock+=dt;
                float s=1+Mathf.Sin(Mathf.Clamp01(revealUiClock/.34f)*Mathf.PI)*.08f;
                revealRt.localScale=Vector3.one*s;
            }
            if(toastClock>0){toastClock-=dt;toastG.alpha=Mathf.Clamp01(toastClock*4);toastRt.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01(toastClock/.35f)*Mathf.PI)*.04f);}else toastG.alpha=0;
            if(rippleClock>0){rippleClock-=dt;float q=1-Mathf.Clamp01(rippleClock/.35f);rippleRt.localScale=Vector3.one*Mathf.Lerp(.25f,1.3f,q);rippleImg.color=new Color(Apricot.r,Apricot.g,Apricot.b,(1-q)*.7f);}else rippleImg.color=new Color(0,0,0,0);

            if(phase==Phase.End)
            {
                endDisplayedScore=Mathf.MoveTowards(endDisplayedScore,st.score,Mathf.Max(80,st.score*1.5f)*dt);
                endScoreTxt.text=Mathf.RoundToInt(endDisplayedScore).ToString();
                endScoreTxt.rectTransform.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01(endDisplayedScore/Mathf.Max(1,st.score))*Mathf.PI)*.08f);
            }
        }

        void RefreshProblemUi()
        {
            if(current==null)return;
            promptTxt.text=current.prompt;
            if(current.kind==BridgeKind.Hypotenuse)
            {
                if(current.band<=1)
                {
                    leftTankTxt.text=current.a+" × "+current.a+"\n<size=80%>"+(current.a*current.a)+" cm²</size>";
                    rightTankTxt.text=current.b+" × "+current.b+"\n<size=80%>"+(current.b*current.b)+" cm²</size>";
                }
                else {leftTankTxt.text="둑 말뚝\n"+current.a+" cm";rightTankTxt.text="둑 말뚝\n"+current.b+" cm";}
                targetTxt.text="직각의 맞은편 판자 길이 = ? cm";
            }
            else if(current.kind==BridgeKind.MissingLeg)
            {
                leftTankTxt.text="빗변\n"+current.c+" cm"; rightTankTxt.text="다른 한 변\n"+current.shownLeg+" cm";
                targetTxt.text="남은 직각변 길이 = ? cm";
            }
            else
            {
                leftTankTxt.text="두 변\n"+current.a+" cm · "+current.b+" cm"; rightTankTxt.text="현재 긴 변\n"+current.startLongest+" cm";
                targetTxt.text="가장 긴 변을 고쳐 직각 프레임 만들기";
            }
            RefreshLengthUi(false); RefreshHudImmediate();
        }

        void RefreshLengthUi(bool animate)
        {
            if(lengthTxt)lengthTxt.text=st.selectedLength+" cm";
            if(animate){handlePulse=.22f;lengthTxt.rectTransform.localScale=Vector3.one*1.12f;}
            for(int i=0;i<tickDots.Length;i++) if(tickDots[i]) tickDots[i].color=i+1==st.selectedLength?Apricot:Ink;
        }

        void RefreshHudImmediate()
        {
            if(progressTxt)progressTxt.text=st.solved+" / "+SudalRules.TargetBridges;
            if(livesTxt)livesTxt.text=st.lives>=3?"● ● ●":st.lives==2?"● ● ○":st.lives==1?"● ○ ○":"○ ○ ○";
            if(titleBest)
            {
                int best=PlayerPrefs.GetInt("sudal-dari.best",0);
                titleBest.text=best>0?"최고 기록 "+best:"구명튜브 3개 · 다리 8개";
            }
        }

        void BeginRevealVisual(bool correct,bool wasFirst,bool timeout)
        {
            worldCorrect=correct; worldTimeout=timeout; worldReveal=0; revealUiClock=0;
            arcA.enabled=arcB.enabled=true; UpdateArcs(0);
            revealTitleTxt.text=correct?"물이 가장자리에 딱 맞았다":"물이 맞지 않아 판자가 기울었다";
            if(correct) revealFormulaTxt.text=current.formula+"\n<size=72%>"+current.answer+" cm 판자로 다리 완성</size>";
            else if(timeout) revealFormulaTxt.text="밀물이 닿았다. 길이를 정해 다시 놓으시오.";
            else
            {
                int have=st.selectedLength*st.selectedLength, need=current.answer*current.answer;
                revealFormulaTxt.text=have<need?"판자 풀에 "+(need-have)+" cm²만큼 물이 모자랐다":"판자 풀에서 "+(have-need)+" cm²만큼 물이 넘쳤다";
            }
            revealBg.color=correct?new Color(.85f,1f,.94f,.98f):new Color(1f,.93f,.78f,.98f);
            if(correct)
            {
                hitStop=.075f;
                MgfSfx.Play("whoosh",.42f);
            }
            else
            {
                wrongShake=.42f;
                MgfSfx.Play("wrong",.44f);
            }
        }

        void CompleteRevealVisual(bool correct)
        {
            arcA.enabled=arcB.enabled=false; revealG.alpha=0;
            bridgeRoot.localScale=Vector3.one; bankRight.position=bankRightHome;
            if(correct)MgfSfx.Play("correct",.40f);
        }

        void ShowWrongReason(string id)
        {
            string msg=id=="sum_without_squares"?"길이가 아니라 제곱한 넓이를 더하시오":
                id=="add_instead_of_subtract"?"빗변이 주어지면 제곱의 차를 구하시오":
                id=="apply_to_non_right"?"가장 긴 변을 c로 두고 다시 비교하시오":
                id=="tide_timeout"?"밀물 전에는 판자 길이를 놓아야 한다":
                id=="bridge_too_short"?"풀이 마른 칸이 남았다. 더 긴 판자가 필요하다":"물이 넘쳤다. 더 짧은 판자가 필요하다";
            Toast(msg); ReplayGuide(true);
        }

        void ShowEnd(string reason)
        {
            endDisplayedScore=0;
            endTitleTxt.text=reason=="clear"?"강마을 다리 완성":"구명튜브가 모두 떠내려갔다";
            endStatsTxt.text="놓은 다리 "+st.solved+" / "+SudalRules.TargetBridges+"\n첫 시도 "+st.firstAttemptCorrect+" / "+Math.Max(1,st.firstAttemptTotal)+"\n"+(st.lifeRecovered?"연속 정답으로 튜브 1개 복구":"피타고라스 수 도감에 기록 완료");
        }

        void ReplayGuide(bool strong){guideClock=0;guideBoost=strong?1.2f:.45f;handlePulse=strong?.9f:.35f;}
        void RefuseInput(string message){handlePulse=.75f;guideBoost=.8f;Toast(message);MgfSfx.Play("tap",.18f);}
        void Toast(string message){toastTxt.text=message;toastClock=2.1f;toastG.alpha=1;}
        void SpawnTapRipple(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRt,screen,null,out var local);
            rippleRt.anchoredPosition=local;rippleClock=.35f;
        }
        void HandleGrab(bool on){handleImg.color=on?Cream:Apricot;handleRt.localScale=on?new Vector3(1.18f,.86f,1):Vector3.one;}
        void TickPlank(){MgfSfx.Play("tap",.08f);handlePulse=.12f;}
        void UpdatePlankTrail(){plankTrail.enabled=true;plankTrail.SetPosition(0,bridgeHandle.position);plankTrail.SetPosition(1,bridgeHandle.position+Vector3.left*.65f);plankTrail.SetPosition(2,bridgeHandle.position+Vector3.left*1.25f);}
        void BeginTitleGrab(){titlePull=0;titleGripImg.color=Cream;}
        void UpdateTitleGrab(Vector2 screen){titlePull=Mathf.Clamp((screen.x-Screen.width*.5f)/Mathf.Max(1,Screen.width)*180,-45,60);}
        void EndTitleGrab(){titleGripImg.color=Apricot;titlePull=0;MgfSfx.Play("whoosh",.35f);}

        bool IsTrackZone(Vector2 p)=>RectTransformUtility.RectangleContainsScreenPoint(trackRt,p,null);
        bool IsTitleHandleZone(Vector2 p)=>RectTransformUtility.RectangleContainsScreenPoint(titleHandleRt,p,null);
        int LengthFromPointer(Vector2 p)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRt,p,null,out var local);
            float u=Mathf.Clamp01((local.x+151f)/302f);
            return Mathf.Clamp(1+Mathf.RoundToInt(u*24),1,25);
        }
    }
}
