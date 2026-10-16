// 연못 점프 — 연보라 수면 위 미니어처 연꽃 습지, 자두색 청개구리, 대응점 도약 피드백.
using System;
using System.Collections.Generic;
using TMPro;
using Mgf;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.YeonmotJeompeu
{
    public partial class YeonmotJeompeuGame
    {
        // 비 오는 심야 갈대 수로: 기존 라일락 파스텔 온실 계열과 배경·부품 물성을 분리한다.
        static readonly Color WaterLilac = MgfLook.Hex("#12343E");
        static readonly Color WaterDeep = MgfLook.Hex("#071A24");
        static readonly Color Olive = MgfLook.Hex("#487C69");
        static readonly Color OliveDark = MgfLook.Hex("#183B34");
        static readonly Color Plum = MgfLook.Hex("#9B5574");
        static readonly Color PlumDeep = MgfLook.Hex("#10222B");
        static readonly Color Apricot = MgfLook.Hex("#D77A4B");
        static readonly Color Cream = MgfLook.Hex("#EAF2E8");
        static readonly Color Petal = MgfLook.Hex("#72B7A6");
        static readonly Color Gold = MgfLook.Hex("#F0C56A");
        static readonly Color CanalPanel = MgfLook.Hex("#16303A");

        Camera cam;
        Transform worldRoot, pondRoot, sourceLeaf, frogRoot, frogBody, frogHead, frogThroat, titleLeaf;
        Transform[] candidateLeaves = new Transform[2];
        Transform[] sourceVertices = new Transform[3];
        Transform[] targetVertices = new Transform[6];
        Transform[] frogToes = new Transform[3];
        Transform[] reeds = new Transform[24];
        Transform[] dragonflies = new Transform[2];
        Transform[] flowers = new Transform[8];
        Transform[][] flowerPetals = new Transform[8][];
        float[] flowerOpen = new float[8];
        Vector3[] reedBase = new Vector3[24];
        Vector3[] dragonBase = new Vector3[2];
        LineRenderer jumpTrail, guideLine, routeLine;
        LineRenderer[] triangleLines = new LineRenderer[3];
        LineRenderer[] waterRings = new LineRenderer[5];
        Material waterMat, leafMat, leafGlowMat, leafWrongMat, lineMat, lineGlowMat, frogMat, toeMat, creamMat, petalMat, stemMat;
        Mesh leafMesh;
        Vector3 sourceBase;
        Vector3[] candidateBase = new Vector3[2];
        Vector3 frogHome;
        bool wideLayout;
        float worldClock, titleClock, leafFlex, flashClock, toastClock, rippleClock, guideClock, guideBoostClock, bandClock;
        int activeRipple;

        CanvasGroup titleG, hudG, revealG, endG, toastG, bandG;
        RectTransform titleLogoRt, titleCallRt, statsRt, sourcePanelRt, restartRt, dragLabelRt, rippleRt, guideHandRt, guideTargetRt;
        RectTransform[] candidatePanelRt = new RectTransform[2];
        RectTransform[] sourceVertexRt = new RectTransform[3];
        RectTransform[] targetVertexRt = new RectTransform[6];
        TextMeshProUGUI titleBest, goalTxt, progressTxt, timeTxt, livesTxt, scoreTxt;
        TextMeshProUGUI conditionTxt, sourceTxt, promptTxt, dragLabelTxt;
        TextMeshProUGUI[] candidateTxt = new TextMeshProUGUI[2];
        TextMeshProUGUI[] sourceVertexTxt = new TextMeshProUGUI[3];
        TextMeshProUGUI[] targetVertexTxt = new TextMeshProUGUI[6];
        TextMeshProUGUI revealTitleTxt, revealFormulaTxt, endTitleTxt, endStatsTxt, endScoreTxt, toastTxt, bandTxt;
        Image[] lifeBuds = new Image[3];
        Image[] progressPetals = new Image[8];
        Image flashImg, rippleImg, guideHandImg, guideTargetImg;
        Sprite roundedSprite, circleSprite, ringSprite, petalSprite;
        int displayedScore, displayedEndScore, displayedSolved;

        static readonly Vector3[] LocalTriangle = {
            new Vector3(0,.22f,1.34f), new Vector3(1.18f,.22f,-.82f), new Vector3(-1.18f,.22f,-.82f)
        };

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#081720"), WaterLilac, MgfLook.Hex("#071018"), .84f);
            MgfLook.Sun(new Vector3(48, -32, 16), MgfLook.Hex("#FFD69A"), 1.04f, .62f);
            cam = MgfLook.Camera(new Vector3(0, 16, -18), Vector3.zero, 38f);
            cam.orthographic = true; cam.orthographicSize = 10.2f; cam.backgroundColor = WaterLilac;

            worldRoot = new GameObject("LotusWetlandDiorama").transform;
            pondRoot = new GameObject("PaleLilacPond").transform; pondRoot.SetParent(worldRoot, false);
            waterMat = MgfLook.Lit(WaterLilac, .92f, .05f, new Color(.01f,.08f,.10f,1f));
            leafMat = MgfLook.Lit(Olive, .48f, .01f, Olive * .025f);
            leafGlowMat = MgfLook.Lit(MgfLook.Hex("#91A96B"), .62f, .02f, Gold * .18f);
            leafWrongMat = MgfLook.Lit(MgfLook.Hex("#665069"), .34f, .01f);
            lineMat = MgfLook.Lit(PlumDeep, .28f, .01f);
            lineGlowMat = MgfLook.Lit(Gold, .72f, .04f, Gold * .35f);
            frogMat = MgfLook.Lit(Plum, .48f, .02f);
            toeMat = MgfLook.Lit(Apricot, .62f, .01f, Apricot * .04f);
            creamMat = MgfLook.Lit(Cream, .34f, .01f);
            petalMat = MgfLook.Lit(Petal, .72f, .04f, Petal * .10f);
            stemMat = MgfLook.Lit(OliveDark, .22f, .01f);
            leafMesh = MakeLeafMesh();

            MgfLook.Prim(PrimitiveType.Cube,"LilacWater",new Vector3(0,-.70f,20f),new Vector3(38,.82f,88),waterMat,pondRoot,false);
            BuildBanksAndPlants();
            BuildWaterRings();

            sourceLeaf = CreateLeaf("CurrentLeaf", leafMat, 1.30f);
            triangleLines[0] = CreateTriangleLine("SourceDiagram", sourceLeaf, lineMat);
            for (int i=0;i<3;i++) sourceVertices[i] = CreateVertexMarker("SourceVertex-"+i, sourceLeaf, LocalTriangle[i], false);

            for (int leaf=0;leaf<2;leaf++)
            {
                candidateLeaves[leaf] = CreateLeaf("DestinationLeaf-"+leaf, leafMat, 1.48f);
                triangleLines[leaf+1] = CreateTriangleLine("CandidateDiagram-"+leaf, candidateLeaves[leaf], lineMat);
                for (int pos=0;pos<3;pos++) targetVertices[leaf*3+pos] = CreateVertexMarker("LandingVertex-"+(leaf*3+pos),candidateLeaves[leaf],LocalTriangle[pos],true);
            }
            BuildFrog();
            BuildLines();
            BuildTitleLeaf();
            BuildProgressFlowers();
        }

        void BuildBanksAndPlants()
        {
            Material bankMat=MgfLook.Lit(MgfLook.Hex("#19333B"),.22f,.02f);
            Material pebbleMat=MgfLook.Lit(MgfLook.Hex("#35545A"),.52f,.03f);
            for(int i=0;i<18;i++)
            {
                float side=i%2==0?-1f:1f; float z=-12f+i*4.15f;
                MgfLook.Block("SoftBank-"+i,new Vector3(side*(9.5f+(i%3)*.42f),-.15f,z),new Vector3(9.2f,.75f,3.8f),.7f,bankMat,pondRoot);
                for(int k=0;k<2;k++) MgfLook.Prim(PrimitiveType.Sphere,"ShorePebble",new Vector3(side*(6.5f+k*.65f),.18f,z-1+k*.7f),new Vector3(.48f,.22f,.38f),pebbleMat,pondRoot,false);
            }
            for(int i=0;i<reeds.Length;i++)
            {
                float side=i%2==0?-1f:1f; float x=side*(6.1f+(i%4)*.55f); float z=-9f+(i*2.93f)%31f;
                var r=MgfLook.Block("Reed-"+i,new Vector3(x,.72f,z),new Vector3(.12f,1.64f,.12f),.04f,stemMat,pondRoot).transform;
                r.rotation=Quaternion.Euler((i%3-1)*5f,0,side*(6+i%4)); reeds[i]=r; reedBase[i]=r.localPosition;
                if(i%4==0) MgfLook.Prim(PrimitiveType.Sphere,"SeedPod",new Vector3(0,1.02f,0),new Vector3(.28f,.38f,.28f),leafMat,r,false);
            }
            for(int i=0;i<dragonflies.Length;i++)
            {
                var d=new GameObject("Dragonfly-"+i).transform; d.SetParent(pondRoot,false); d.localPosition=new Vector3(i==0?-5.5f:5.8f,2.2f,i==0?4f:-1f); dragonBase[i]=d.localPosition;
                MgfLook.Prim(PrimitiveType.Sphere,"Body",Vector3.zero,new Vector3(.12f,.12f,.55f),frogMat,d,false);
                for(int s=-1;s<=1;s+=2) MgfLook.Prim(PrimitiveType.Sphere,"Wing",new Vector3(s*.28f,.02f,0),new Vector3(.46f,.035f,.16f),MgfLook.Alpha(new Color(1f,.92f,.98f,.58f)),d,false);
                dragonflies[i]=d;
            }
        }

        void BuildWaterRings()
        {
            Material ringMat=MgfLook.Additive(new Color(.38f,.88f,.80f,.30f));
            for(int r=0;r<waterRings.Length;r++)
            {
                var go=new GameObject("WaterContour-"+r); go.transform.SetParent(pondRoot,false);
                var line=go.AddComponent<LineRenderer>(); line.sharedMaterial=ringMat; line.useWorldSpace=false; line.loop=true; line.positionCount=40; line.startWidth=line.endWidth=.055f; line.numCornerVertices=2;
                float rad=1.2f+r*.72f; for(int i=0;i<40;i++){float a=i/40f*Mathf.PI*2f;line.SetPosition(i,new Vector3(Mathf.Cos(a)*rad,.04f,Mathf.Sin(a)*rad*.55f));}
                go.transform.localPosition=new Vector3((r%2==0?-1:1)*(2.2f+r),-.23f,-5f+r*5.2f); waterRings[r]=line;
            }
        }

        Transform CreateLeaf(string name,Material mat,float scale)
        {
            var go=new GameObject(name); go.transform.SetParent(pondRoot,false); go.AddComponent<MeshFilter>().sharedMesh=leafMesh;
            var mr=go.AddComponent<MeshRenderer>(); mr.sharedMaterial=mat; mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On; mr.receiveShadows=true;
            go.transform.localScale=new Vector3(scale,1f,scale*.94f);
            var bc=go.AddComponent<BoxCollider>(); bc.size=new Vector3(3.4f,.45f,3.1f); bc.center=new Vector3(0,.06f,0);
            var stem=MgfLook.Block("LeafStem",new Vector3(0,-.55f,-1.05f),new Vector3(.16f,1.52f,.16f),.05f,stemMat,go.transform); stem.transform.rotation=Quaternion.Euler(42,0,0);
            return go.transform;
        }

        Mesh MakeLeafMesh()
        {
            const int n=36; var verts=new List<Vector3>(); var tris=new List<int>(); var uv=new List<Vector2>();
            verts.Add(Vector3.zero); uv.Add(new Vector2(.5f,.5f));
            for(int i=0;i<=n;i++)
            {
                float a=(i/(float)n)*Mathf.PI*2f; float notch=Mathf.Abs(Mathf.DeltaAngle(a*Mathf.Rad2Deg,90f))<10f?.26f:1f;
                float radius=1.72f*notch; verts.Add(new Vector3(Mathf.Cos(a)*radius,.06f+Mathf.Sin(a*2f)*.035f,Mathf.Sin(a)*radius));
                uv.Add(new Vector2(.5f+.5f*Mathf.Cos(a)*notch,.5f+.5f*Mathf.Sin(a)*notch));
            }
            for(int i=1;i<=n;i++){tris.Add(0);tris.Add(i);tris.Add(i+1);}
            var m=new Mesh{name="WaxyLotusLeaf"};m.SetVertices(verts);m.SetTriangles(tris,0);m.SetUVs(0,uv);m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        LineRenderer CreateTriangleLine(string name,Transform parent,Material mat)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);var lr=go.AddComponent<LineRenderer>();
            lr.sharedMaterial=mat;lr.useWorldSpace=false;lr.loop=true;lr.positionCount=3;lr.startWidth=lr.endWidth=.065f;lr.numCornerVertices=4;
            for(int i=0;i<3;i++)lr.SetPosition(i,LocalTriangle[i]+Vector3.up*.03f);return lr;
        }

        Transform CreateVertexMarker(string name,Transform parent,Vector3 local,bool collider)
        {
            var o=MgfLook.Prim(PrimitiveType.Sphere,name,local+Vector3.up*.05f,new Vector3(.34f,.16f,.34f),toeMat,parent,collider);
            if(collider){var c=o.GetComponent<SphereCollider>();c.radius=1.35f;}
            return o.transform;
        }

        void BuildFrog()
        {
            frogRoot=new GameObject("PlumTreefrog").transform;frogRoot.SetParent(pondRoot,false);
            frogBody=MgfLook.Prim(PrimitiveType.Sphere,"RoundBody",new Vector3(0,.55f,0),new Vector3(1.38f,.88f,1.12f),frogMat,frogRoot,false).transform;
            frogHead=MgfLook.Prim(PrimitiveType.Sphere,"RoundHead",new Vector3(0,.94f,.36f),new Vector3(1.16f,.76f,.82f),frogMat,frogRoot,false).transform;
            frogThroat=MgfLook.Prim(PrimitiveType.Sphere,"WhiteThroat",new Vector3(0,.73f,.72f),new Vector3(.72f,.42f,.25f),creamMat,frogRoot,false).transform;
            for(int s=-1;s<=1;s+=2)
            {
                var eye=MgfLook.Prim(PrimitiveType.Sphere,"Eye",new Vector3(s*.35f,1.20f,.62f),new Vector3(.36f,.36f,.32f),creamMat,frogRoot,false).transform;
                MgfLook.Prim(PrimitiveType.Sphere,"Pupil",new Vector3(0,.01f,.18f),new Vector3(.14f,.17f,.09f),MgfLook.Lit(PlumDeep,.75f,.02f),eye,false);
                MgfLook.Prim(PrimitiveType.Sphere,"HindLeg",new Vector3(s*.68f,.40f,-.12f),new Vector3(.68f,.40f,.74f),frogMat,frogRoot,false);
            }
            for(int i=0;i<3;i++) frogToes[i]=MgfLook.Prim(PrimitiveType.Sphere,"ApricotToe-"+i,new Vector3((i-1)*.28f,.17f,.62f+Mathf.Abs(i-1)*.07f),new Vector3(.23f,.10f,.28f),toeMat,frogRoot,false).transform;
            var grab=frogRoot.gameObject.AddComponent<SphereCollider>();grab.radius=1.08f;grab.center=new Vector3(0,.64f,0);
        }

        void BuildLines()
        {
            jumpTrail=MakeLine("ToeArc",MgfLook.Alpha(new Color(Apricot.r,Apricot.g,Apricot.b,.74f)),.10f,26);
            guideLine=MakeLine("PracticeFootpath",MgfLook.Additive(new Color(1f,.84f,.48f,.74f)),.105f,32);
            routeLine=MakeLine("PassedLeafVein",MgfLook.Additive(new Color(.96f,.76f,.46f,.55f)),.12f,10);
            jumpTrail.enabled=false;guideLine.enabled=false;routeLine.enabled=false;
        }

        LineRenderer MakeLine(string name,Material mat,float width,int cap)
        {
            var go=new GameObject(name);go.transform.SetParent(pondRoot,false);var lr=go.AddComponent<LineRenderer>();
            lr.sharedMaterial=mat;lr.startWidth=lr.endWidth=width;lr.numCapVertices=4;lr.positionCount=cap;lr.useWorldSpace=true;lr.enabled=false;return lr;
        }

        void BuildTitleLeaf()
        {
            titleLeaf=CreateLeaf("TitleEmbossedLeaf",leafGlowMat,2.55f);titleLeaf.localPosition=new Vector3(0,.72f,2.8f);titleLeaf.localRotation=Quaternion.Euler(-4,12,0);
        }

        void BuildProgressFlowers()
        {
            for(int i=0;i<flowers.Length;i++)
            {
                var root=new GameObject("CanalBeacon-"+i).transform;root.SetParent(pondRoot,false);float side=i%2==0?-1f:1f;root.localPosition=new Vector3(side*(5.0f+(i%3)*.64f),.16f,-4f+i*2.25f);
                MgfLook.Block("CopperPost",new Vector3(0,.22f,0),new Vector3(.16f,.44f,.16f),.07f,toeMat,root);
                var petals=new Transform[4];for(int p=0;p<4;p++){var fin=MgfLook.Block("BeaconFin",Vector3.zero,new Vector3(.12f,.08f,.54f),.04f,petalMat,root).transform;fin.localRotation=Quaternion.Euler(0,p*90f,0);petals[p]=fin;}
                MgfLook.Prim(PrimitiveType.Sphere,"BeaconLamp",new Vector3(0,.52f,0),new Vector3(.28f,.22f,.28f),MgfLook.Lit(Gold,.72f,.04f,Gold*.26f),root,false);
                flowers[i]=root;flowerPetals[i]=petals;
            }
        }

        void BuildUi()
        {
            roundedSprite=MakeRoundedSprite(64,15,false);circleSprite=MakeRoundedSprite(64,31,false);ringSprite=MakeRoundedSprite(64,31,true);petalSprite=MakePetalSprite();
            var canvas=MgfText.Canvas.transform;
            titleG=Group("Title",canvas);hudG=Group("Hud",canvas);revealG=Group("Reveal",canvas);endG=Group("End",canvas);toastG=Group("Toast",canvas);bandG=Group("Band",canvas);

            titleLogoRt=R("LeafEmbossedLogo",titleG.transform,new Vector2(.5f,.67f),Vector2.zero,new Vector2(360,190));
            var logoTop=Txt(titleLogoRt,"연못",new Vector2(.5f,.67f),Vector2.zero,54,Cream,340);logoTop.fontStyle=FontStyles.Bold;logoTop.outlineWidth=.30f;logoTop.outlineColor=PlumDeep;
            var logo=Txt(titleLogoRt,"점프",new Vector2(.5f,.25f),Vector2.zero,68,Gold,350);logo.fontStyle=FontStyles.Bold;logo.characterSpacing=-3;logo.outlineWidth=.30f;logo.outlineColor=PlumDeep;
            var sub=Txt(titleG.transform,"대응점으로 뛰어라",new Vector2(.5f,.43f),Vector2.zero,21,Cream,340);sub.fontStyle=FontStyles.Bold;
            titleCallRt=Panel(titleG.transform,"FrogStartRipple",new Vector2(.5f,.16f),Vector2.zero,new Vector2(292,66),new Color(Apricot.r,Apricot.g,Apricot.b,.94f),roundedSprite);
            var call=Txt(titleCallRt,"개구리를 잡아 시작",new Vector2(.5f,.5f),Vector2.zero,21,Cream,266);call.fontStyle=FontStyles.Bold;
            Txt(titleG.transform,"중학교 2학년 · 도형의 닮음",new Vector2(.5f,.045f),Vector2.zero,14,Cream,340);
            titleBest=Txt(titleG.transform,"",new Vector2(.5f,.085f),Vector2.zero,15,Cream,300);

            statsRt=Panel(hudG.transform,"CanalHud",new Vector2(.5f,1f),new Vector2(0,-34),new Vector2(374,62),new Color(CanalPanel.r,CanalPanel.g,CanalPanel.b,.96f),roundedSprite);
            progressTxt=Txt(statsRt,"도약 0/8",new Vector2(.16f,.5f),Vector2.zero,16,Cream,104);
            timeTxt=Txt(statsRt,"90초",new Vector2(.50f,.5f),Vector2.zero,17,Cream,78);
            scoreTxt=Txt(statsRt,"0",new Vector2(.83f,.5f),Vector2.zero,18,Gold,94);
            goalTxt=Txt(hudG.transform,"닮은 잎의 대응점으로 뛰어 건너라",new Vector2(.5f,.905f),Vector2.zero,16,Cream,372);goalTxt.fontStyle=FontStyles.Bold;
            livesTxt=Txt(hudG.transform,"기회",new Vector2(.11f,.855f),Vector2.zero,13,Cream,62);
            for(int i=0;i<3;i++)lifeBuds[i]=Img(hudG.transform,"LifeBud-"+i,new Vector2(.20f+i*.062f,.855f),Vector2.zero,new Vector2(22,28),Petal,petalSprite);
            for(int i=0;i<8;i++)progressPetals[i]=Img(hudG.transform,"ProgressPetal-"+i,new Vector2(.56f+i*.043f,.855f),Vector2.zero,new Vector2(17,22),new Color(Olive.r,Olive.g,Olive.b,.22f),petalSprite);

            sourcePanelRt=Panel(hudG.transform,"SourcePlate",new Vector2(.5f,.75f),Vector2.zero,new Vector2(360,86),new Color(CanalPanel.r,CanalPanel.g,CanalPanel.b,.96f),roundedSprite);
            conditionTxt=Txt(sourcePanelRt,"SSS 닮음",new Vector2(.5f,.80f),Vector2.zero,13,Gold,334);
            sourceTxt=Txt(sourcePanelRt,"△ABC",new Vector2(.5f,.49f),Vector2.zero,16,Cream,340);sourceTxt.fontStyle=FontStyles.Bold;sourceTxt.enableAutoSizing=true;sourceTxt.fontSizeMin=11;sourceTxt.fontSizeMax=16;sourceTxt.textWrappingMode=TextWrappingModes.NoWrap;
            promptTxt=Txt(sourcePanelRt,"대응점을 찾으시오",new Vector2(.5f,.18f),Vector2.zero,14,Petal,340);

            for(int leaf=0;leaf<2;leaf++)
            {
                candidatePanelRt[leaf]=Panel(hudG.transform,"BuoyEvidence-"+leaf,new Vector2(.5f,.5f),Vector2.zero,new Vector2(176,72),new Color(CanalPanel.r,CanalPanel.g,CanalPanel.b,.95f),roundedSprite);
                candidateTxt[leaf]=Txt(candidatePanelRt[leaf],"△DEF",new Vector2(.5f,.5f),Vector2.zero,14,Cream,168);candidateTxt[leaf].textWrappingMode=TextWrappingModes.Normal;candidateTxt[leaf].lineSpacing=-11f;
            }
            for(int i=0;i<3;i++)
            {
                sourceVertexRt[i]=Panel(hudG.transform,"SourceBuoy-"+i,new Vector2(.5f,.5f),Vector2.zero,new Vector2(38,38),new Color(Apricot.r,Apricot.g,Apricot.b,.96f),circleSprite);
                sourceVertexTxt[i]=Txt(sourceVertexRt[i],((char)('A'+i)).ToString(),new Vector2(.5f,.5f),Vector2.zero,18,Cream,34);sourceVertexTxt[i].fontStyle=FontStyles.Bold;
            }
            for(int i=0;i<6;i++)
            {
                targetVertexRt[i]=Panel(hudG.transform,"TargetBuoy-"+i,new Vector2(.5f,.5f),Vector2.zero,new Vector2(44,44),new Color(Petal.r,Petal.g,Petal.b,.96f),circleSprite);
                targetVertexTxt[i]=Txt(targetVertexRt[i],"D",new Vector2(.5f,.5f),Vector2.zero,20,PlumDeep,40);targetVertexTxt[i].fontStyle=FontStyles.Bold;
            }

            dragLabelRt=Panel(hudG.transform,"FingerLabel",new Vector2(.5f,.5f),Vector2.zero,new Vector2(78,42),new Color(Cream.r,Cream.g,Cream.b,.94f),petalSprite);
            dragLabelTxt=Txt(dragLabelRt,"",new Vector2(.5f,.5f),Vector2.zero,18,PlumDeep,70);
            guideHandRt=Panel(hudG.transform,"GuideFoot",new Vector2(.5f,.5f),Vector2.zero,new Vector2(44,44),Apricot,circleSprite);guideHandImg=guideHandRt.GetComponent<Image>();
            guideTargetRt=Panel(hudG.transform,"GuideLanding",new Vector2(.5f,.5f),Vector2.zero,new Vector2(88,88),new Color(Gold.r,Gold.g,Gold.b,.70f),ringSprite);guideTargetImg=guideTargetRt.GetComponent<Image>();
            rippleRt=Panel(hudG.transform,"PointerRipple",Vector2.zero,Vector2.zero,new Vector2(52,52),Color.clear,ringSprite);rippleImg=rippleRt.GetComponent<Image>();
            flashImg=Img(hudG.transform,"PondFlash",new Vector2(.5f,.5f),Vector2.zero,new Vector2(1400,1400),Color.clear,roundedSprite);flashImg.transform.SetAsFirstSibling();

            Panel(revealG.transform,"RevealGate",new Vector2(.5f,.48f),Vector2.zero,new Vector2(368,148),new Color(CanalPanel.r,CanalPanel.g,CanalPanel.b,.97f),roundedSprite);
            revealTitleTxt=Txt(revealG.transform,"SSS 닮음",new Vector2(.5f,.515f),Vector2.zero,25,Gold,346);revealTitleTxt.fontStyle=FontStyles.Bold;
            revealFormulaTxt=Txt(revealG.transform,"",new Vector2(.5f,.455f),Vector2.zero,16,Cream,344);revealFormulaTxt.textWrappingMode=TextWrappingModes.Normal;

            Panel(endG.transform,"ResultGate",new Vector2(.5f,.54f),Vector2.zero,new Vector2(372,350),new Color(CanalPanel.r,CanalPanel.g,CanalPanel.b,.98f),roundedSprite);
            endTitleTxt=Txt(endG.transform,"연꽃 둑 도착",new Vector2(.5f,.67f),Vector2.zero,36,Cream,340);endTitleTxt.fontStyle=FontStyles.Bold;
            endScoreTxt=Txt(endG.transform,"0",new Vector2(.5f,.56f),Vector2.zero,56,Apricot,300);
            endStatsTxt=Txt(endG.transform,"",new Vector2(.5f,.45f),Vector2.zero,18,Petal,330);endStatsTxt.textWrappingMode=TextWrappingModes.Normal;
            restartRt=Panel(endG.transform,"RestartLeaf",new Vector2(.5f,.28f),Vector2.zero,new Vector2(254,66),new Color(Olive.r,Olive.g,Olive.b,.96f),petalSprite);
            Txt(endG.transform,"개구리를 눌러 다시 건너기",new Vector2(.5f,.28f),Vector2.zero,18,Cream,240);

            var toastPanel=Panel(toastG.transform,"ToastPetal",new Vector2(.5f,.145f),Vector2.zero,new Vector2(356,76),new Color(PlumDeep.r,PlumDeep.g,PlumDeep.b,.94f),petalSprite);
            toastTxt=Txt(toastPanel,"",new Vector2(.5f,.5f),Vector2.zero,15,Cream,336);toastTxt.textWrappingMode=TextWrappingModes.Normal;
            Panel(bandG.transform,"BandGate",new Vector2(.5f,.51f),Vector2.zero,new Vector2(350,92),new Color(CanalPanel.r,CanalPanel.g,CanalPanel.b,.97f),roundedSprite);
            bandTxt=Txt(bandG.transform,"SAS 닮음",new Vector2(.5f,.51f),Vector2.zero,24,Gold,330);bandTxt.fontStyle=FontStyles.Bold;
        }

        CanvasGroup Group(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasGroup));go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;return go.GetComponent<CanvasGroup>();
        }
        RectTransform R(string name,Transform parent,Vector2 anchor,Vector2 offset,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=offset;rt.sizeDelta=size;return rt;
        }
        RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 offset,Vector2 size,Color color,Sprite sprite=null)
        {
            var rt=R(name,parent,anchor,offset,size);var img=rt.gameObject.AddComponent<Image>();img.sprite=sprite??roundedSprite;img.type=Image.Type.Sliced;img.color=color;img.raycastTarget=false;return rt;
        }
        Image Img(Transform parent,string name,Vector2 anchor,Vector2 offset,Vector2 size,Color color,Sprite sprite)
        {
            var rt=R(name,parent,anchor,offset,size);var img=rt.gameObject.AddComponent<Image>();img.sprite=sprite;img.type=Image.Type.Sliced;img.color=color;img.raycastTarget=false;return img;
        }
        TextMeshProUGUI Txt(Transform parent,string text,Vector2 anchor,Vector2 offset,float size,Color color,float width)
        {
            var t=MgfText.Ui(text,anchor,offset,size,color,width);t.transform.SetParent(parent,false);var rt=(RectTransform)t.transform;rt.anchorMin=rt.anchorMax=anchor;rt.anchoredPosition=offset;t.raycastTarget=false;return t;
        }

        Sprite MakeRoundedSprite(int size,int radius,bool ring)
        {
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);tex.name=ring?"Ring":"Round";var px=new Color32[size*size];float c=(size-1)*.5f;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=Mathf.Max(0,Mathf.Abs(x-c)-(c-radius));float dy=Mathf.Max(0,Mathf.Abs(y-c)-(c-radius));bool inside=dx*dx+dy*dy<=radius*radius;
                if(ring){float d=Vector2.Distance(new Vector2(x,y),new Vector2(c,c));inside=d>size*.34f&&d<size*.48f;}
                px[y*size+x]=inside?new Color32(255,255,255,255):new Color32(255,255,255,0);
            }
            tex.SetPixels32(px);tex.Apply(false,true);return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(radius,radius,radius,radius));
        }

        Sprite MakePetalSprite()
        {
            const int s=64;var tex=new Texture2D(s,s,TextureFormat.RGBA32,false);var px=new Color32[s*s];Vector2 c=new Vector2(31.5f,31.5f);
            for(int y=0;y<s;y++)for(int x=0;x<s;x++)
            {
                Vector2 p=new Vector2(x,y)-c;float d=(p.x*p.x)/(31f*31f)+(p.y*p.y)/(27f*27f);bool inside=d<=1f;
                px[y*s+x]=inside?new Color32(255,255,255,255):new Color32(255,255,255,0);
            }
            tex.SetPixels32(px);tex.Apply(false,true);return Sprite.Create(tex,new Rect(0,0,s,s),new Vector2(.5f,.5f),100);
        }

        void SetScreen()
        {
            bool title=gamePhase==GamePhase.Title,end=gamePhase==GamePhase.End,reveal=gamePhase==GamePhase.Reveal;
            SetGroup(titleG,title);SetGroup(hudG,!title&&!end);SetGroup(revealG,reveal);SetGroup(endG,end);SetGroup(toastG,toastClock>0&&!title&&!end);SetGroup(bandG,bandClock>0&&!title&&!end);
            titleLeaf.gameObject.SetActive(title);sourceLeaf.gameObject.SetActive(!title);for(int i=0;i<2;i++)candidateLeaves[i].gameObject.SetActive(!title);
            guideHandRt.gameObject.SetActive(gamePhase==GamePhase.Practice);guideTargetRt.gameObject.SetActive(gamePhase==GamePhase.Practice);dragLabelRt.gameObject.SetActive(dragging);
        }
        void SetGroup(CanvasGroup g,bool on){g.alpha=on?1:0;g.gameObject.SetActive(on);}

        void ShowTitleVisual()
        {
            int best=PlayerPrefs.GetInt("yeonmot-jeompeu.best",0);titleBest.text=best>0?"최고 기록 "+best.ToString("N0"):"";
            titleClock=0;displayedScore=0;displayedEndScore=0;displayedSolved=0;
            frogRoot.gameObject.SetActive(true);frogRoot.position=new Vector3(0,.44f,-4.2f);frogRoot.localScale=Vector3.one*1.14f;
        }

        void ResetWorldForRun()
        {
            worldClock=0;leafFlex=0;flashClock=0;guideLine.enabled=false;jumpTrail.enabled=false;routeLine.enabled=false;activeRipple=0;
            for(int i=0;i<flowers.Length;i++)flowerOpen[i]=0;
            for(int i=0;i<2;i++){candidateLeaves[i].localRotation=Quaternion.identity;candidateLeaves[i].GetComponent<Renderer>().sharedMaterial=leafMat;triangleLines[i+1].sharedMaterial=lineMat;}
            sourceLeaf.localRotation=Quaternion.identity;sourceLeaf.GetComponent<Renderer>().sharedMaterial=leafMat;triangleLines[0].sharedMaterial=lineMat;
        }

        void ResetLeavesForProblem(PondProblem p)
        {
            for(int i=0;i<2;i++){candidateLeaves[i].localRotation=Quaternion.Euler(0,(p.serial*37+i*131)%360,0);candidateLeaves[i].GetComponent<Renderer>().sharedMaterial=leafMat;triangleLines[i+1].sharedMaterial=lineMat;}
            sourceLeaf.localRotation=Quaternion.Euler(0,(p.serial*23)%360,0);triangleLines[0].sharedMaterial=lineMat;
            for(int leaf=0;leaf<2;leaf++)for(int pos=0;pos<3;pos++)targetVertexTxt[leaf*3+pos].text=YeonmotRules.VertexChar(leaf,p.candidates[leaf].posToVertex[pos]).ToString();
            for(int i=0;i<3;i++)sourceVertexTxt[i].text=((char)('A'+i)).ToString();
            guideLine.enabled=false;jumpTrail.enabled=false;
        }

        void RefreshProblemUi()
        {
            if(current==null)return;
            conditionTxt.text=current.kind==PondKind.Sss?"SSS 닮음":current.kind==PondKind.Sas?"SAS 닮음":"AA 닮음";
            sourceTxt.text=current.sourceDisplay;promptTxt.text=current.prompt;
            candidateTxt[0].text=CompactCandidate(0);candidateTxt[1].text=CompactCandidate(1);
            goalTxt.text=gamePhase==GamePhase.Practice?"개구리를 B에서 F까지 끌어 놓으시오":"닮은 잎의 대응점으로 뛰어 건너라";
        }

        string CompactCandidate(int leaf)
        {
            TriangleDatum d=current.candidates[leaf];char a=YeonmotRules.VertexChar(leaf,0),b=YeonmotRules.VertexChar(leaf,1),c=YeonmotRules.VertexChar(leaf,2);
            if(current.kind==PondKind.Sss)return "△"+d.name+"  "+a+b+" "+d.sides[0]+" · "+b+c+" "+d.sides[1]+"\n"+c+a+" "+d.sides[2]+" cm";
            if(current.kind==PondKind.Sas)return "△"+d.name+"  "+a+b+" "+d.sides[0]+" · "+a+c+" "+d.sides[2]+" cm\n∠"+a+" "+d.angles[0]+"°";
            return "△"+d.name+"  ∠"+a+" "+d.angles[0]+"°\n∠"+b+" "+d.angles[1]+"°";
        }

        Vector3 SourceVertex(int canonical){return sourceLeaf.TransformPoint(LocalTriangle[Mathf.Clamp(canonical,0,2)])+Vector3.up*.18f;}
        Vector3 VertexLanding(int slot){return targetVertices[Mathf.Clamp(slot,0,5)].position+Vector3.up*.60f;}
        void SetFrogHome(){frogHome=SourceVertex(current==null?1:current.sourceActiveVertex);frogRoot.position=frogHome;frogRoot.rotation=Quaternion.identity;frogRoot.localScale=Vector3.one;}

        bool IsFrogGrab(Vector2 screen)
        {
            Vector2 p=cam.WorldToScreenPoint(frogRoot.position+Vector3.up*.68f);return Vector2.Distance(p,screen)<=Mathf.Clamp(Mathf.Min(Screen.width,Screen.height)*.22f,82f,145f);
        }

        int VertexAtScreen(Vector2 screen)
        {
            int best=-1;float bd=Mathf.Clamp(Mathf.Min(Screen.width,Screen.height)*.095f,54f,112f);
            for(int i=0;i<6;i++){Vector2 p=cam.WorldToScreenPoint(VertexLanding(i));float d=Vector2.Distance(p,screen);if(d<bd){bd=d;best=i;}}
            return best;
        }

        void BeginFrogGrab()
        {
            frogRoot.localScale=new Vector3(1.18f,.70f,1.14f);guideLine.enabled=false;MgfSfx.Play("tap",.22f);
        }

        void UpdateFrogDrag(Vector2 screen)
        {
            var ray=cam.ScreenPointToRay(screen);var plane=new Plane(Vector3.up,new Vector3(0,.82f,0));if(!plane.Raycast(ray,out float e))return;
            Vector3 p=ray.GetPoint(e);p.x=Mathf.Clamp(p.x,-8f,8f);p.z=Mathf.Clamp(p.z,-7f,8f);frogRoot.position=Vector3.Lerp(frogRoot.position,p,.38f);
            frogRoot.localScale=new Vector3(1.22f,.68f,1.16f);UpdateAimTrail();
            int slot=VertexAtScreen(screen);dragLabelTxt.text=slot>=0?YeonmotRules.ChoiceLabel(current,slot):"꼭짓점";dragLabelRt.position=new Vector2(screen.x,screen.y+52f);
        }

        void EndFrogDrag(){frogRoot.localScale=Vector3.one;jumpTrail.enabled=false;dragLabelRt.gameObject.SetActive(false);}
        void UpdateAimTrail(){jumpTrail.enabled=true;jumpTrail.positionCount=2;jumpTrail.SetPosition(0,frogHome+Vector3.up*.28f);jumpTrail.SetPosition(1,frogRoot.position+Vector3.down*.12f);}

        void BeginJumpVisual(int slot)
        {
            jumpTrail.enabled=true;jumpTrail.positionCount=26;
            for(int i=0;i<26;i++){float t=i/25f;Vector3 p=Vector3.Lerp(jumpFrom,jumpTo,t);p.y+=Mathf.Sin(t*Mathf.PI)*2.35f;jumpTrail.SetPosition(i,p);}
        }

        void UpdateJump(float t)
        {
            float e=1f-Mathf.Pow(1f-t,3f);Vector3 p=Vector3.Lerp(jumpFrom,jumpTo,e);p.y+=Mathf.Sin(t*Mathf.PI)*2.35f;frogRoot.position=p;
            float stretch=Mathf.Sin(t*Mathf.PI);frogRoot.localScale=new Vector3(1f-.22f*stretch,1f+.38f*stretch,1f-.12f*stretch);frogRoot.rotation=Quaternion.Euler(0,Mathf.Sin(t*Mathf.PI)*14f,0);
        }

        void BeginRevealVisual(bool correct,bool wasFirst,PondProblem p,int slot)
        {
            jumpTrail.enabled=false;frogRoot.localScale=new Vector3(1.24f,.66f,1.18f);leafFlex=correct?.62f:-.55f;
            int leaf=slot/3;
            if(correct)
            {
                candidateLeaves[leaf].GetComponent<Renderer>().sharedMaterial=leafGlowMat;triangleLines[leaf+1].sharedMaterial=lineGlowMat;
                revealTitleTxt.text=p.kind==PondKind.Sss?"SSS 닮음 · 대응점 확인":p.kind==PondKind.Sas?"SAS 닮음 · 대응점 확인":"AA 닮음 · 대응점 확인";
                revealFormulaTxt.text=p.explanation;flashClock=.58f;MgfSfx.Play("correct",.50f);MgfFx.Punch(frogBody,.22f+.025f*Math.Min(5,st.combo),.34f);
                for(int i=0;i<frogToes.Length;i++){frogToes[i].localScale=Vector3.one*(1.15f+i*.08f);}
                int flower=Mathf.Clamp(st.solved-1,0,flowers.Length-1);flowerOpen[flower]=.01f;WakeRoute(slot);
            }
            else
            {
                candidateLeaves[leaf].GetComponent<Renderer>().sharedMaterial=leafWrongMat;revealTitleTxt.text="기회 1개 사용";revealFormulaTxt.text=WrongReason(st.misconceptionId);
                flashClock=-.36f;MgfSfx.Play("wrong",.42f);MgfFx.Shake(cam,.10f,.24f);ActivateWaterRipple(candidateLeaves[leaf].position);
            }
        }

        void CompleteRevealVisual(bool correct,int slot)
        {
            frogRoot.localScale=Vector3.one;for(int i=0;i<frogToes.Length;i++)frogToes[i].localScale=Vector3.one;
            if(slot>=0){int leaf=slot/3;candidateLeaves[leaf].localRotation=Quaternion.identity;candidateLeaves[leaf].localScale=new Vector3(1.48f,1f,1.39f);candidateLeaves[leaf].GetComponent<Renderer>().sharedMaterial=leafMat;triangleLines[leaf+1].sharedMaterial=lineMat;}
            flashImg.color=Color.clear;
        }

        void WakeRoute(int slot)
        {
            routeLine.enabled=true;routeLine.positionCount=9;
            Vector3 a=frogHome+Vector3.down*.32f,b=VertexLanding(slot)+Vector3.down*.52f;
            Vector3 side=Vector3.Cross(Vector3.up,(b-a).normalized);
            for(int i=0;i<9;i++)
            {
                float t=i/8f;Vector3 p=Vector3.Lerp(a,b,t);
                p+=side*Mathf.Sin(t*Mathf.PI*2f)*.34f;p.y=-.14f;
                routeLine.SetPosition(i,p);
            }
        }

        void AdvancePond(int slot)
        {
            frogHome=VertexLanding(slot);frogRoot.position=frogHome;
        }

        void ReplayGuide(bool stronger)
        {
            if(current==null)return;guideLine.enabled=true;guideLine.positionCount=32;Vector3 a=SourceVertex(current.sourceActiveVertex)+Vector3.up*.42f,b=VertexLanding(current.correctSlot);
            for(int i=0;i<32;i++){float t=i/31f;Vector3 p=Vector3.Lerp(a,b,t);p.y+=Mathf.Sin(t*Mathf.PI)*(stronger?1.65f:1.15f);guideLine.SetPosition(i,p);}
            guideClock=0f;guideBoostClock=stronger?2.4f:1.2f;
            if(stronger){MgfFx.Punch(frogRoot,.18f,.30f);MgfFx.Punch(targetVertices[current.correctSlot],.20f,.32f);}
            ShowToast(stronger?"개구리를 누른 채 F까지 끌어 놓으시오":"B의 개구리를 F까지 직접 끌어 놓으시오",2.1f);
        }

        void InspectVertex(int slot){MgfFx.Punch(targetVertices[slot],.14f,.22f);ShowToast(YeonmotRules.ChoiceLabel(current,slot)+" · 개구리부터 끌어 놓으시오",1.25f);MgfSfx.Play("tap",.14f);st.inputVersion++;if(gamePhase==GamePhase.Practice)ReplayGuide(true);}
        void RefuseWater(Vector2 screen){ActivateWaterRipple(ScreenToWater(screen));Refuse("빈 물이 아니라 개구리를 잡아 꼭짓점에 놓으시오");if(gamePhase==GamePhase.Practice)ReplayGuide(true);}
        void Refuse(string message){ShowToast(message,1.25f);MgfSfx.Play("wrong",.15f);MgfFx.Punch(frogRoot,.10f,.18f);st.inputVersion++;MgfBridge.NotifyChanged();}
        void NudgeFrog(){MgfFx.Punch(frogRoot,.08f,.14f);ShowToast("현재 꼭짓점의 두 이웃 변 또는 각을 먼저 확인하시오",1.5f);}
        void PressTitleFrog(){MgfFx.Punch(frogRoot,.26f,.30f);flashClock=.42f;MgfSfx.Play("pop",.34f);ActivateWaterRipple(frogRoot.position);}

        Vector3 ScreenToWater(Vector2 screen)
        {
            var plane=new Plane(Vector3.up,new Vector3(0,-.22f,0));var ray=cam.ScreenPointToRay(screen);return plane.Raycast(ray,out float d)?ray.GetPoint(d):Vector3.zero;
        }

        void ActivateWaterRipple(Vector3 world)
        {
            LineRenderer lr=waterRings[activeRipple++%waterRings.Length];lr.transform.position=new Vector3(world.x,-.16f,world.z);lr.transform.localScale=Vector3.one*.15f;
        }

        string WrongReason(string id)
        {
            if(id=="additive_difference")return "같은 수를 더한 것이 아니라 세 대응변의 비를 확인하시오";
            if(id=="partial_ratio")return "두 변만 보지 말고 세 대응변의 비를 모두 확인하시오";
            if(id=="sas_wrong_angle")return "두 변 사이의 끼인각이 같은지 확인하시오";
            if(id=="sas_ratio_mismatch")return "끼인각뿐 아니라 두 대응변의 비도 확인하시오";
            if(id=="aa_one_angle")return "한 각만 같아서는 부족하다 · 두 대응각을 확인하시오";
            return "그림의 위치가 아니라 대응하는 변과 각을 확인하시오";
        }
        void ShowWrongReason(string id){ShowToast(WrongReason(id),2.0f);}
        void ShowBandTransition(string text){bandTxt.text=text;bandClock=1.2f;SetGroup(bandG,true);bandG.transform.localScale=Vector3.one*.86f;MgfSfx.Play("pop",.26f);}

        void ShowEnd(string reason)
        {
            endTitleTxt.text=reason=="clear"?"연꽃 둑 도착":reason=="not_mastered"?"이번 판은 아직 숙련 전":reason=="time"?"90초 물길 종료":"기회 소진";
            endStatsTxt.text="도약 "+st.solved+"/8\n첫 시도 "+st.firstCorrect+"/8 · 목표 6/8";displayedEndScore=0;
        }
        void ShowToast(string message,float seconds){toastTxt.text=message;toastClock=seconds;SetGroup(toastG,true);toastG.transform.localScale=new Vector3(.92f,.92f,1);MgfFx.Punch(toastG.transform,.09f,.22f);}
        void SpawnPointerRipple(Vector2 screen){RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hudG.transform,screen,null,out Vector2 local);rippleRt.anchoredPosition=local;rippleClock=.38f;rippleImg.color=new Color(Apricot.r,Apricot.g,Apricot.b,.82f);rippleRt.localScale=Vector3.one*.45f;}

        void AnimateWorld(float dt)
        {
            worldClock+=dt;titleClock+=dt;guideClock+=dt;
            for(int i=0;i<reeds.Length;i++)if(reeds[i]){reeds[i].localPosition=reedBase[i]+Vector3.up*Mathf.Sin(worldClock*1.1f+i*.7f)*.035f;reeds[i].localRotation=Quaternion.Euler((i%3-1)*5f,0,(i%2==0?-1:1)*(6+i%4)+Mathf.Sin(worldClock*.8f+i)*3f);}
            for(int i=0;i<dragonflies.Length;i++)if(dragonflies[i]){Vector3 p=dragonBase[i];p.x+=Mathf.Sin(worldClock*.8f+i*2f)*1.25f;p.z+=Mathf.Cos(worldClock*.63f+i)*.75f;p.y+=Mathf.Sin(worldClock*2.4f+i)*.18f;dragonflies[i].localPosition=p;dragonflies[i].localRotation=Quaternion.Euler(0,worldClock*70f*(i==0?1:-1),0);}
            for(int i=0;i<waterRings.Length;i++)if(waterRings[i]){float pulse=.96f+Mathf.Sin(worldClock*.72f+i)*.08f;waterRings[i].transform.localScale=Vector3.Lerp(waterRings[i].transform.localScale,Vector3.one*pulse,dt*.65f);}
            for(int i=0;i<flowers.Length;i++)
            {
                if(flowerOpen[i]>0&&flowerOpen[i]<1)flowerOpen[i]=Mathf.Min(1,flowerOpen[i]+dt*2.6f);float o=flowerOpen[i];
                for(int p=0;p<flowerPetals[i].Length;p++){float a=p*90f*Mathf.Deg2Rad;flowerPetals[i][p].localPosition=new Vector3(Mathf.Cos(a)*o*.34f,.30f+o*.05f,Mathf.Sin(a)*o*.34f);flowerPetals[i][p].localRotation=Quaternion.Euler(0,p*90f+o*45f,0);flowerPetals[i][p].localScale=Vector3.one*Mathf.Lerp(.58f,1f,o);}
                flowers[i].localPosition+=Vector3.up*Mathf.Sin(worldClock*1.2f+i)*.00035f;
            }
            if(gamePhase==GamePhase.Reveal&&selectedSlot>=0)
            {
                int leaf=selectedSlot/3;float k=Mathf.Clamp01(phaseClock/.62f);
                if(landingCorrect)
                {
                    float bend=Mathf.Sin(k*Mathf.PI);candidateLeaves[leaf].localScale=new Vector3(1.48f,Mathf.Lerp(1,.58f,bend),1.39f);frogRoot.localScale=new Vector3(1+.18f*bend,.68f+.42f*k,1+.10f*bend);
                    // 정답은 화면 흔들림 없이 세 잎이 맞물려 수로를 여는 사건으로 보인다.
                    sourceLeaf.localRotation=Quaternion.Euler(0,-18f*bend,0);
                    candidateLeaves[leaf].localRotation=Quaternion.Euler(0,22f*bend,0);
                    candidateLeaves[1-leaf].localRotation=Quaternion.Euler(0,-12f*bend,0);
                }
                else{candidateLeaves[leaf].localRotation=Quaternion.Euler(Mathf.Lerp(0,36f,k),candidateLeaves[leaf].localEulerAngles.y,Mathf.Sin(k*Mathf.PI)*16f);frogRoot.localPosition+=Vector3.down*dt*.24f;}
            }
            else
            {
                sourceLeaf.localPosition=sourceBase+Vector3.up*Mathf.Sin(worldClock*1.25f)*.045f;
                for(int i=0;i<2;i++)candidateLeaves[i].localPosition=candidateBase[i]+Vector3.up*Mathf.Sin(worldClock*1.25f+i*1.8f)*.045f;
            }
            if(gamePhase!=GamePhase.Jumping&&!dragging&&frogRoot){float breath=Mathf.Sin(worldClock*2.1f)*.038f;frogBody.localScale=new Vector3(1.38f,.88f+breath,1.12f);frogThroat.localScale=new Vector3(.72f,.42f+Mathf.Abs(breath)*2.1f,.25f);frogHead.localPosition=new Vector3(0,.94f+breath*.35f,.36f);}
            if(titleLeaf&&titleLeaf.gameObject.activeSelf)titleLeaf.localRotation=Quaternion.Euler(-4+Mathf.Sin(worldClock*.7f)*2f,12+Mathf.Sin(worldClock*.42f)*2f,0);
            if(guideLine.enabled){float w=.10f+Mathf.Sin(worldClock*5f)*.026f;guideLine.startWidth=guideLine.endWidth=w;}
            if(routeLine.enabled){float w=.11f+Mathf.Sin(worldClock*3.4f)*.032f;routeLine.startWidth=routeLine.endWidth=w;}
            if(flashClock!=0){float sign=Mathf.Sign(flashClock);flashClock-=sign*dt;if(Mathf.Sign(flashClock)!=sign)flashClock=0;float a=Mathf.Clamp01(Mathf.Abs(flashClock)*1.8f);flashImg.color=sign>0?new Color(Gold.r,Gold.g,Gold.b,a*.20f):new Color(PlumDeep.r,PlumDeep.g,PlumDeep.b,a*.20f);}
        }

        void UpdateUi(float dt)
        {
            if(guideBoostClock>0)guideBoostClock=Mathf.Max(0,guideBoostClock-dt);
            if(gamePhase==GamePhase.Title){float s=1+Mathf.Sin(titleClock*1.8f)*.026f;titleLogoRt.localScale=Vector3.one*s;titleCallRt.localScale=Vector3.one*(1+Mathf.Sin(titleClock*3.1f)*.032f);}
            int target=st.score;if(displayedScore!=target){displayedScore=Mathf.RoundToInt(Mathf.MoveTowards(displayedScore,target,Mathf.Max(1,Mathf.Abs(target-displayedScore)*dt*7f)));scoreTxt.text=displayedScore.ToString("N0");scoreTxt.transform.localScale=Vector3.one*(1+Mathf.Sin(worldClock*18f)*.06f);}else scoreTxt.transform.localScale=Vector3.one;
            if(displayedSolved!=st.solved){displayedSolved=st.solved;progressTxt.transform.localScale=Vector3.one*1.16f;}else progressTxt.transform.localScale=Vector3.Lerp(progressTxt.transform.localScale,Vector3.one,dt*8f);
            progressTxt.text="도약 "+st.solved+"/8";timeTxt.text=Mathf.CeilToInt(timeLeft)+"초";
            goalTxt.text=gamePhase==GamePhase.Practice?"개구리를 B에서 F까지 끌어 놓으시오":"첫 시도 "+st.firstCorrect+"/8 · 목표 6/8";
            for(int i=0;i<3;i++)lifeBuds[i].color=i<st.lives?new Color(Petal.r,Petal.g,Petal.b,.98f):new Color(PlumDeep.r,PlumDeep.g,PlumDeep.b,.18f);
            for(int i=0;i<8;i++)progressPetals[i].color=i<st.solved?new Color(Olive.r,Olive.g,Olive.b,.95f):new Color(Olive.r,Olive.g,Olive.b,.22f);
            if(gamePhase==GamePhase.End){displayedEndScore=Mathf.RoundToInt(Mathf.MoveTowards(displayedEndScore,st.score,Mathf.Max(1,Mathf.Abs(st.score-displayedEndScore)*dt*5f)));endScoreTxt.text=displayedEndScore.ToString("N0");}
            if(toastClock>0){toastClock-=dt;if(toastClock<=0)SetGroup(toastG,false);}
            if(bandClock>0){bandClock-=dt;float t=1-Mathf.Clamp01(bandClock/1.2f);float s=t<.24f?Mathf.Lerp(.86f,1.06f,t/.24f):Mathf.Lerp(1.06f,1f,(t-.24f)/.76f);bandG.transform.localScale=Vector3.one*s;if(bandClock<=0)SetGroup(bandG,false);}
            if(rippleClock>0){rippleClock-=dt;float k=1-Mathf.Clamp01(rippleClock/.38f);rippleRt.localScale=Vector3.one*Mathf.Lerp(.45f,1.75f,k);rippleImg.color=new Color(Apricot.r,Apricot.g,Apricot.b,1-k);}
            if(gamePhase==GamePhase.Practice&&current!=null)
            {
                float t=(Mathf.Sin(guideClock*2.6f)*.5f+.5f);Vector2 a=cam.WorldToScreenPoint(SourceVertex(current.sourceActiveVertex)+Vector3.up*.65f);Vector2 b=cam.WorldToScreenPoint(VertexLanding(current.correctSlot));
                float boost=guideBoostClock>0?1f+.38f*Mathf.Clamp01(guideBoostClock/2.4f):1f;
                // 손 표식은 꼭짓점 문자 위를 덮지 않고 B 아래에서 F 아래까지 왕복한다.
                guideHandRt.position=Vector2.Lerp(a,b,t*.86f)+new Vector2(0,-38f);guideHandRt.localScale=Vector3.one*boost;guideTargetRt.position=b;guideTargetRt.localScale=Vector3.one*boost*(1+Mathf.Sin(guideClock*4f)*.18f);guideTargetImg.color=new Color(Gold.r,Gold.g,Gold.b,.56f+.22f*Mathf.Sin(guideClock*4f));
            }
            UpdateProjectedLabels();
        }

        void UpdateProjectedLabels()
        {
            if(gamePhase==GamePhase.Title||gamePhase==GamePhase.End||current==null)return;
            float maxY=Screen.height*(wideLayout ? .50f : .65f),minY=Screen.height*.15f;
            float margin=Mathf.Clamp(Mathf.Min(Screen.width,Screen.height)*.078f,28f,70f);
            float screenAspect=Screen.width/(float)Math.Max(1,Screen.height);
            float buoyScale=screenAspect>=1.75f?.72f:screenAspect>=1.15f?.86f:1f;
            for(int i=0;i<3;i++)
            {
                Vector3 p=cam.WorldToScreenPoint(SourceVertex(i)+Vector3.up*.10f);p.x=Mathf.Clamp(p.x,margin,Screen.width-margin);p.y=Mathf.Clamp(p.y,minY,maxY);
                sourceVertexRt[i].position=p;sourceVertexRt[i].sizeDelta=new Vector2(38,38)*buoyScale;sourceVertexTxt[i].fontSize=18f*buoyScale;sourceVertexRt[i].localScale=i==current.sourceActiveVertex?Vector3.one*1.12f:Vector3.one;
                st.sourcePx[i*2]=Mathf.RoundToInt(p.x);st.sourcePx[i*2+1]=Screen.height-Mathf.RoundToInt(p.y);
            }
            for(int i=0;i<6;i++)
            {
                Vector3 p=cam.WorldToScreenPoint(VertexLanding(i)+Vector3.up*.12f);p.x=Mathf.Clamp(p.x,margin,Screen.width-margin);p.y=Mathf.Clamp(p.y,minY,maxY);
                targetVertexRt[i].position=p;targetVertexRt[i].sizeDelta=new Vector2(44,44)*buoyScale;targetVertexTxt[i].fontSize=20f*buoyScale;
                st.targetPx[i*2]=Mathf.RoundToInt(p.x);st.targetPx[i*2+1]=Screen.height-Mathf.RoundToInt(p.y);
            }
        }

        void ApplyResponsiveLayout()
        {
            float aspect=Screen.width/(float)Math.Max(1,Screen.height);wideLayout=aspect>=1.15f;
            if(wideLayout)
            {
                cam.orthographicSize=aspect<1f?8.15f:7.35f;Vector3 target=new Vector3(0,0,.25f);cam.transform.position=target+new Vector3(0,15.2f,-17.5f);cam.transform.LookAt(target);
                sourceBase=new Vector3(-5.1f,.18f,-.25f);candidateBase[0]=new Vector3(0,.18f,.15f);candidateBase[1]=new Vector3(5.1f,.18f,-.25f);
                sourcePanelRt.anchorMin=sourcePanelRt.anchorMax=new Vector2(.5f,.78f);sourcePanelRt.sizeDelta=new Vector2(430,82);statsRt.anchorMin=statsRt.anchorMax=new Vector2(.5f,.96f);statsRt.anchoredPosition=Vector2.zero;
                candidatePanelRt[0].anchorMin=candidatePanelRt[0].anchorMax=new Vector2(.27f,.61f);candidatePanelRt[0].anchoredPosition=Vector2.zero;candidatePanelRt[0].sizeDelta=new Vector2(202,72);
                candidatePanelRt[1].anchorMin=candidatePanelRt[1].anchorMax=new Vector2(.73f,.61f);candidatePanelRt[1].anchoredPosition=Vector2.zero;candidatePanelRt[1].sizeDelta=new Vector2(202,72);
            }
            else
            {
                cam.orthographicSize=11.2f;Vector3 target=new Vector3(0,0,.05f);cam.transform.position=target+new Vector3(0,16.4f,-18.7f);cam.transform.LookAt(target);
                sourceBase=new Vector3(0,.18f,-5.25f);candidateBase[0]=new Vector3(-2.75f,.18f,1.10f);candidateBase[1]=new Vector3(2.75f,.18f,1.10f);
                sourcePanelRt.anchorMin=sourcePanelRt.anchorMax=new Vector2(.5f,.775f);sourcePanelRt.sizeDelta=new Vector2(366,84);statsRt.anchorMin=statsRt.anchorMax=new Vector2(.5f,1f);statsRt.anchoredPosition=new Vector2(0,-34);
                candidatePanelRt[0].anchorMin=candidatePanelRt[0].anchorMax=new Vector2(.25f,.425f);candidatePanelRt[0].anchoredPosition=Vector2.zero;candidatePanelRt[0].sizeDelta=new Vector2(176,72);
                candidatePanelRt[1].anchorMin=candidatePanelRt[1].anchorMax=new Vector2(.75f,.425f);candidatePanelRt[1].anchoredPosition=Vector2.zero;candidatePanelRt[1].sizeDelta=new Vector2(176,72);
            }
            if(gamePhase!=GamePhase.Reveal){sourceLeaf.localPosition=sourceBase;for(int i=0;i<2;i++)candidateLeaves[i].localPosition=candidateBase[i];}
            if((gamePhase==GamePhase.Practice||gamePhase==GamePhase.Playing)&&!dragging&&gamePhase!=GamePhase.Jumping&&gamePhase!=GamePhase.Reveal)SetFrogHome();
        }
    }
}
