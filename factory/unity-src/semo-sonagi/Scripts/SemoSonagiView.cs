// 세모 소나기 — 공중 도자 빗구름 군도와 거친 직조 우산이 이어지는 2.5D 세계.
// 정답: 우산 관절 전개 → 도자 홈통 연결 → 천 면에 정수식 물자국 리빌.
// 오답: 선택 천의 한 모서리만 접히고 보수 천이 떨어짐. 화면 흔들림은 이 경로에만 쓴다.
using System;
using System.Collections;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.SemoSonagi
{
    public partial class SemoSonagiGame
    {
        static readonly Color Coral=MgfLook.Hex("#F4B6A8");
        static readonly Color CoralDeep=MgfLook.Hex("#D96B66");
        static readonly Color Leaf=MgfLook.Hex("#4C8B78");
        static readonly Color LeafDark=MgfLook.Hex("#28594F");
        static readonly Color Rain=MgfLook.Hex("#66B9C5");
        static readonly Color RainLight=MgfLook.Hex("#BCE6E3");
        static readonly Color Cream=MgfLook.Hex("#FFF4D8");
        static readonly Color Ink=MgfLook.Hex("#263C3A");
        static readonly Color Gold=MgfLook.Hex("#F0C35B");
        static readonly Color Plum=MgfLook.Hex("#754C67");
        static readonly Color Cobalt=MgfLook.Hex("#304B70");

        Camera cam;
        Transform worldRoot,gardenRoot,titleUmbrella,titleCanopy,titleHandle;
        readonly Transform[] candidateRoots=new Transform[4];
        readonly Transform[] candidateCanopies=new Transform[4];
        readonly Transform[] candidateCorners=new Transform[4];
        readonly Transform[] shelters=new Transform[6];
        readonly Transform[] shelterCanopies=new Transform[6];
        readonly Transform[] snails=new Transform[5];
        readonly Transform[] snailBodies=new Transform[5];
        readonly Transform[] snailFeelers=new Transform[10];
        readonly Transform[] leaves=new Transform[18];
        readonly Transform[] rainDrops=new Transform[36];
        readonly Transform[] waterBeads=new Transform[12];
        readonly Vector2[] candidateScreen=new Vector2[4];
        readonly Vector3[] candidateHome=new Vector3[4];
        Material leafMat,leafDarkMat,coralMat,coralDeepMat,rainMat,creamMat,inkMat,goldMat,plumMat,soilMat,waterMat,canopyResinMat,fiberMat;
        Material cloudMat,ceramicMat,cobaltMat,terracottaMat;
        Mesh canopyMesh,leafMesh;
        LineRenderer swipeTrail,guideTrail;
        Transform pulseRing,wrongCloth,formulaGlow;
        int trailCount,layoutW=-1,layoutH=-1,layoutMode=-1;
        float worldClock,titlePulse,guideBoost,guideDemoClock,feedbackAnim,hitStop,wrongKick,snailReact,clothFall;
        bool animCorrect,animWrong;

        CanvasGroup titleG,hudG,revealG,endG,toastG;
        RectTransform titleRt,hudRt,revealRt,endRt,toastRt,rippleRt,titleHintRt,guideHandRt,guideReleaseRt;
        TextMeshProUGUI titleLogo,titleLogoShadow,titleTag,titleMeta,titleHint;
        TextMeshProUGUI scoreTxt,livesTxt,timeTxt,progressTxt,goalTxt,promptTxt,referenceTxt;
        readonly TextMeshProUGUI[] candidateTxt=new TextMeshProUGUI[4];
        readonly Image[] candidateBg=new Image[4];
        TextMeshProUGUI revealTitleTxt,revealFormulaTxt,toastTxt,endTitleTxt,endScoreTxt,endStatsTxt,endTapTxt;
        Image titleArt,titleBanner,statusBg,goalBg,promptBg,referenceBg,revealBg,rippleImg,guideHandImg;
        readonly Image[] guideDots=new Image[9];
        Sprite softSprite,ringSprite;
        float displayedScore,endDisplayedScore,rippleClock,toastClock,revealUiClock;
        int shownScore=-1,shownSecond=-1;

        RectTransform R(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);
            rt.anchoredPosition=pos;rt.sizeDelta=size;return rt;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.pivot=new Vector2(.5f,.5f);
            rt.offsetMin=Vector2.zero;rt.offsetMax=Vector2.zero;rt.anchoredPosition=Vector2.zero;
        }

        static void Place(RectTransform rt,Vector2 anchor,Vector2 pos,Vector2 size)
        {
            rt.anchorMin=rt.anchorMax=anchor;rt.anchoredPosition=pos;rt.sizeDelta=size;
        }

        Image Img(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color,Sprite sprite=null)
        {
            var rt=R(name,parent,anchor,pos,size);var im=rt.gameObject.AddComponent<Image>();im.color=color;im.sprite=sprite;
            im.raycastTarget=false;if(sprite&&sprite.border!=Vector4.zero)im.type=Image.Type.Sliced;return im;
        }

        TextMeshProUGUI Txt(Transform parent,string text,Vector2 anchor,Vector2 pos,float size,Color color,float width=360,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {
            var rt=R("Text",parent,anchor,pos,new Vector2(width,size*2.35f));var t=rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font=MgfText.Font;t.fontSize=size;t.color=color;t.alignment=align;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;t.text=text;return t;
        }

        static Sprite MakeSprite(int size,float radius,bool ring)
        {
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){name=ring?"SonagiRing":"SonagiCloth",wrapMode=TextureWrapMode.Clamp};
            var px=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float fx=x+.5f,fy=y+.5f,a;
                if(ring){float dx=fx-size*.5f,dy=fy-size*.5f,d=Mathf.Sqrt(dx*dx+dy*dy);a=Mathf.Clamp01(1-Mathf.Abs(d-size*.39f)/(size*.055f));}
                else{float qx=Mathf.Max(Mathf.Abs(fx-size*.5f)-(size*.5f-radius),0),qy=Mathf.Max(Mathf.Abs(fy-size*.5f)-(size*.5f-radius),0);a=Mathf.Clamp01(radius-Mathf.Sqrt(qx*qx+qy*qy)+.8f);}
                px[y*size+x]=new Color32(255,255,255,(byte)(a*255));
            }
            tex.SetPixels32(px);tex.Apply(false,true);float b=ring?0:radius+2;
            return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(b,b,b,b));
        }

        void BuildWorld()
        {
            // CreatePrimitive(Cylinder)는 WebGL에서 CapsuleCollider를 내부적으로 붙인다.
            // 직접 참조해 IL2CPP 스트리핑으로 타입이 사라지는 것을 막은 뒤 즉시 제거한다.
            var colliderTypes=new GameObject("ColliderTypeAnchor");
            colliderTypes.AddComponent<CapsuleCollider>();colliderTypes.AddComponent<SphereCollider>();Destroy(colliderTypes);
            MgfLook.Sky(MgfLook.Hex("#5D7191"),MgfLook.Hex("#A9BAD1"),MgfLook.Hex("#E8DCC8"),.66f);
            MgfLook.Sun(new Vector3(-28,-42,18),Cream,1.18f,.58f);
            cam=MgfLook.Camera(new Vector3(0,12.5f,-16.8f),new Vector3(0,.6f,2.5f),38f);
            leafMat=MgfLook.Lit(Leaf,.20f,.02f,Leaf*.05f);leafDarkMat=MgfLook.Lit(LeafDark,.16f,.01f);
            coralMat=MgfLook.Lit(Coral,.42f,.02f,Coral*.07f);coralDeepMat=MgfLook.Lit(CoralDeep,.34f,.02f,CoralDeep*.06f);
            rainMat=MgfLook.Additive(new Color(RainLight.r,RainLight.g,RainLight.b,.64f),MgfLook.SoftDot);
            waterMat=MgfLook.Lit(Rain,.82f,.08f,Rain*.10f);creamMat=MgfLook.Lit(Cream,.30f,.01f);
            // 후보 천은 반투명 수지가 아니라 무광 직조 천과 굵은 섬유 리브로 보이게 한다.
            canopyResinMat=MgfLook.Lit(MgfLook.Hex("#C96E55"),.12f,.00f,MgfLook.Hex("#2B3144")*.02f);
            fiberMat=MgfLook.Lit(MgfLook.Hex("#F2DFC1"),.08f,.00f);
            inkMat=MgfLook.Lit(Ink,.18f,.01f);goldMat=MgfLook.Lit(Gold,.55f,.12f,Gold*.11f);
            plumMat=MgfLook.Lit(Plum,.28f,.01f);soilMat=MgfLook.Lit(MgfLook.Hex("#8D6655"),.12f,.01f);
            cloudMat=MgfLook.Lit(MgfLook.Hex("#E9E0D1"),.16f,.00f,MgfLook.Hex("#8099B8")*.025f);
            ceramicMat=MgfLook.Lit(MgfLook.Hex("#E9C9A4"),.30f,.02f);
            cobaltMat=MgfLook.Lit(Cobalt,.24f,.03f,MgfLook.Hex("#7398C1")*.035f);
            terracottaMat=MgfLook.Lit(MgfLook.Hex("#B85C45"),.14f,.00f);
            canopyMesh=MakeCanopyMesh();leafMesh=MakeLeafMesh();
            worldRoot=new GameObject("CeramicStormArchipelago").transform;gardenRoot=new GameObject("FloatingRainIslands").transform;gardenRoot.SetParent(worldRoot,false);
            BuildGarden();BuildCandidates();BuildSnails();BuildWeather();BuildFxObjects();
        }

        void BuildGarden()
        {
            // 평면 탁자 대신 서로 높이가 다른 공중 섬과 아래로 떨어지는 낙수층을 만든다.
            for(int i=0;i<7;i++)
            {
                float x=-7.2f+(i%4)*4.8f,z=-2.2f+(i/4)*8.0f+(i%2)*.7f,y=-.45f+(i%3)*.22f;
                var island=MgfLook.Block("CloudIsland"+i,new Vector3(x,y,z),new Vector3(4.2f,.68f,5.4f),1.05f,cloudMat,gardenRoot);
                island.transform.localRotation=Quaternion.Euler(0,(i%2==0?-7:9),0);
                MgfLook.Block("PaintedTileInlay"+i,new Vector3(x,y+.37f,z-.12f),new Vector3(3.45f,.10f,4.55f),.42f,i%2==0?ceramicMat:cobaltMat,gardenRoot);
                var fall=MgfLook.Block("VerticalWaterfall"+i,new Vector3(x,y-2.1f,z+1.25f),new Vector3(.72f,3.4f,.14f),.22f,waterMat,gardenRoot);
                fall.transform.localRotation=Quaternion.Euler(0,(i%3-1)*9f,0);
            }
            // 정답 사건의 주인공인 세 갈래 도자 홈통. 생물 이동 대신 물길 방향이 바뀐다.
            for(int i=0;i<3;i++)
            {
                var channel=MgfLook.Block("CeramicRainGutter"+i,new Vector3((i-1)*2.4f,.20f,2.6f+i*.25f),new Vector3(1.35f,.16f,10.5f),.18f,i==1?cobaltMat:ceramicMat,gardenRoot);
                channel.transform.localRotation=Quaternion.Euler(0,(i-1)*7f,0);
            }
            for(int i=0;i<leaves.Length;i++)
            {
                float side=i%2==0?-1:1;float x=side*(5.3f+(i%3)*.72f),z=-1.8f+(i/6)*3.6f+(i%2)*.5f;
                var go=new GameObject("RainLeaf"+i);go.transform.SetParent(gardenRoot,false);go.transform.localPosition=new Vector3(x,.28f+(i%3)*.12f,z);
                var mf=go.AddComponent<MeshFilter>();mf.sharedMesh=leafMesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=i%3==0?terracottaMat:(i%3==1?ceramicMat:cobaltMat);
                go.transform.localScale=new Vector3(1.4f+(i%4)*.18f,1,1.2f+(i%3)*.14f);go.transform.localRotation=Quaternion.Euler(8,i*37,side*12);leaves[i]=go.transform;
            }
            // 뒤쪽은 벽이 아니라 공중에 매달린 구름 간판과 타일 수문이다.
            MgfLook.Block("CloudGate",new Vector3(0,3.5f,9.4f),new Vector3(13.5f,5.6f,.48f),1.10f,cloudMat,gardenRoot);
            MgfLook.Block("CobaltSluiceLintel",new Vector3(0,6.35f,8.9f),new Vector3(11.2f,.48f,1.55f),.28f,cobaltMat,gardenRoot);
            for(int i=0;i<7;i++)MgfLook.Block("CeramicMosaic"+i,new Vector3(-4.8f+i*1.6f,3.8f+(i%2)*.5f,9.05f),new Vector3(1.28f,1.28f,.18f),.20f,i%3==0?terracottaMat:(i%3==1?ceramicMat:cobaltMat),gardenRoot);
            for(int i=0;i<shelters.Length;i++)
            {
                shelters[i]=CreateUmbrella("SolvedShelter"+i,gardenRoot,out shelterCanopies[i],out _,false);
                shelters[i].position=new Vector3(-5.5f+i*2.2f,.55f,7.6f+(i%2)*.32f);shelters[i].localScale=Vector3.one*.52f;shelters[i].gameObject.SetActive(false);
            }
        }

        void BuildCandidates()
        {
            for(int i=0;i<4;i++)
            {
                candidateRoots[i]=CreateUmbrella("TriangleUmbrella"+i,worldRoot,out candidateCanopies[i],out candidateCorners[i],true);
                candidateRoots[i].gameObject.SetActive(false);
            }
            titleUmbrella=CreateUmbrella("FoldedStartUmbrella",worldRoot,out titleCanopy,out titleHandle,true);
            titleUmbrella.localScale=new Vector3(.46f,1.28f,.46f);titleUmbrella.position=new Vector3(0,.9f,1.0f);
        }

        Transform CreateUmbrella(string name,Transform parent,out Transform canopy,out Transform corner,bool collider)
        {
            var root=new GameObject(name).transform;root.SetParent(parent,false);
            var cgo=new GameObject("EqualSilhouetteTriangularCanopy");cgo.transform.SetParent(root,false);cgo.transform.localPosition=new Vector3(0,1.15f,0);canopy=cgo.transform;
            var mf=cgo.AddComponent<MeshFilter>();mf.sharedMesh=canopyMesh;var mr=cgo.AddComponent<MeshRenderer>();mr.sharedMaterial=canopyResinMat;mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;
            AddFiber(canopy,new Vector3(0,.48f,0),new Vector3(-1.18f,0,-.78f));
            AddFiber(canopy,new Vector3(0,.48f,0),new Vector3(1.18f,0,-.78f));
            AddFiber(canopy,new Vector3(0,.48f,0),new Vector3(0,0,1.22f));
            AddFiber(canopy,new Vector3(-.78f,.12f,-.52f),new Vector3(.78f,.12f,-.52f));
            AddFiber(canopy,new Vector3(-.42f,.17f,.28f),new Vector3(.42f,.17f,.28f));
            corner=new GameObject("FoldableCorner").transform;corner.SetParent(canopy,false);corner.localPosition=new Vector3(0,0,-.82f);
            MgfLook.Prim(PrimitiveType.Sphere,"RainPearl",Vector3.zero,new Vector3(.16f,.10f,.16f),goldMat,corner,false);
            MgfLook.Prim(PrimitiveType.Cylinder,"Handle",new Vector3(0,.08f,0),new Vector3(.075f,1.05f,.075f),inkMat,root,false);
            var hook=MgfLook.Prim(PrimitiveType.Cylinder,"HandleHook",new Vector3(.18f,-.72f,0),new Vector3(.08f,.30f,.08f),inkMat,root,false);hook.transform.localRotation=Quaternion.Euler(0,0,58);
            if(collider){var box=root.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,1.05f,0);box.size=new Vector3(2.45f,1.8f,2.35f);}
            return root;
        }

        void AddFiber(Transform parent,Vector3 a,Vector3 b)
        {
            Vector3 delta=b-a;var fiber=MgfLook.Prim(PrimitiveType.Cylinder,"WovenCanopyRib",(a+b)*.5f,new Vector3(.026f,delta.magnitude*.5f,.026f),fiberMat,parent,false);
            fiber.transform.localRotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);
        }

        Mesh MakeCanopyMesh()
        {
            var v=new[]{
                new Vector3(0,.48f,0),new Vector3(-1.18f,0,-.78f),new Vector3(1.18f,0,-.78f),new Vector3(0,0,1.22f),
                new Vector3(0,.42f,0),new Vector3(-1.10f,-.10f,-.72f),new Vector3(1.10f,-.10f,-.72f),new Vector3(0,-.10f,1.12f)
            };
            var t=new[]{0,1,2,0,2,3,0,3,1,4,6,5,4,7,6,4,5,7,1,5,6,1,6,2,2,6,7,2,7,3,3,7,5,3,5,1};
            var m=new Mesh{name="EqualTriangleCanopy"};m.vertices=v;m.triangles=t;m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        Mesh MakeLeafMesh()
        {
            var v=new[]{new Vector3(0,0,-1.15f),new Vector3(-.78f,.10f,-.35f),new Vector3(-.62f,.18f,.55f),new Vector3(0,.23f,1.20f),new Vector3(.62f,.18f,.55f),new Vector3(.78f,.10f,-.35f),new Vector3(0,.32f,0)};
            var t=new[]{0,1,6,1,2,6,2,3,6,3,4,6,4,5,6,5,0,6};
            var m=new Mesh{name="WetBroadLeaf"};m.vertices=v;m.triangles=t;m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        void BuildSnails()
        {
            Vector3[] homes={new Vector3(-5.4f,.42f,1.7f),new Vector3(5.1f,.42f,2.2f),new Vector3(-4.7f,.42f,5.7f),new Vector3(4.4f,.42f,6.2f),new Vector3(.2f,.42f,7.0f)};
            for(int i=0;i<snails.Length;i++)
            {
                var root=new GameObject("OriginalMiniatureSnail"+i).transform;root.SetParent(gardenRoot,false);root.position=homes[i];root.localRotation=Quaternion.Euler(0,i%2==0?22:-22,0);snails[i]=root;
                var body=MgfLook.Prim(PrimitiveType.Sphere,"SoftBody",new Vector3(.18f,.18f,0),new Vector3(.40f,.68f,.34f),i%2==0?ceramicMat:cloudMat,root,false);body.transform.localRotation=Quaternion.Euler(0,0,90);snailBodies[i]=body.transform;
                MgfLook.Prim(PrimitiveType.Sphere,"PaintedCeramicShell",new Vector3(-.22f,.62f,.02f),new Vector3(.66f,.66f,.40f),i%2==0?terracottaMat:cobaltMat,root,false);
                MgfLook.Prim(PrimitiveType.Sphere,"Head",new Vector3(.72f,.48f,0),new Vector3(.38f,.38f,.34f),i%2==0?ceramicMat:cloudMat,root,false);
                for(int k=0;k<2;k++)
                {
                    var f=MgfLook.Prim(PrimitiveType.Cylinder,"Feeler"+k,new Vector3(.82f,.80f,(k==0?-.15f:.15f)),new Vector3(.035f,.30f,.035f),inkMat,root,false).transform;
                    f.localRotation=Quaternion.Euler(0,0,k==0?-15:15);snailFeelers[i*2+k]=f;
                    MgfLook.Prim(PrimitiveType.Sphere,"FeelerTip"+k,new Vector3(0,.53f,0),new Vector3(.08f,.08f,.08f),inkMat,f,false);
                }
                root.localScale=Vector3.one*1.25f;
            }
        }

        void BuildWeather()
        {
            for(int i=0;i<rainDrops.Length;i++)
            {
                float x=-8f+(i%12)*1.45f,z=-1f+(i/12)*4.4f;
                rainDrops[i]=MgfLook.Prim(PrimitiveType.Cylinder,"SoftRain"+i,new Vector3(x,2f+(i%7)*1.15f,z),new Vector3(.025f,.28f,.025f),rainMat,worldRoot,false).transform;
                rainDrops[i].localRotation=Quaternion.Euler(0,0,8);
            }
            // 화면 위쪽의 도자 구름은 수문과 같은 무광 재질로 잇는다.
            for(int i=0;i<11;i++)MgfLook.Prim(PrimitiveType.Sphere,"CeramicCloud"+i,new Vector3(-7.5f+i*1.5f,7.4f+Mathf.Sin(i)*.35f,7.9f),new Vector3(1.55f,1.0f,.78f),i%3==0?ceramicMat:cloudMat,worldRoot,false);
        }

        void BuildFxObjects()
        {
            swipeTrail=new GameObject("ContinuousWaterSwipe").AddComponent<LineRenderer>();swipeTrail.positionCount=24;swipeTrail.widthMultiplier=.14f;
            swipeTrail.sharedMaterial=MgfLook.Additive(new Color(RainLight.r,RainLight.g,RainLight.b,.82f),MgfLook.SoftDot);swipeTrail.enabled=false;
            guideTrail=new GameObject("OnboardingChevronPath").AddComponent<LineRenderer>();guideTrail.positionCount=18;guideTrail.widthMultiplier=.10f;
            guideTrail.sharedMaterial=MgfLook.Additive(new Color(Gold.r,Gold.g,Gold.b,.72f),MgfLook.SoftDot);guideTrail.enabled=false;
            pulseRing=MgfLook.Prim(PrimitiveType.Cylinder,"LiveTargetPulse",Vector3.zero,new Vector3(1.2f,.025f,1.2f),MgfLook.Additive(new Color(Gold.r,Gold.g,Gold.b,.45f),MgfLook.SoftDot),worldRoot,false).transform;pulseRing.gameObject.SetActive(false);
            formulaGlow=MgfLook.Prim(PrimitiveType.Cylinder,"FormulaWaterGlow",Vector3.zero,new Vector3(1.5f,.025f,1.5f),MgfLook.Additive(new Color(RainLight.r,RainLight.g,RainLight.b,.52f),MgfLook.SoftDot),worldRoot,false).transform;formulaGlow.gameObject.SetActive(false);
            wrongCloth=MgfLook.Block("FallingRepairCloth",Vector3.zero,new Vector3(.72f,.08f,.62f),.12f,coralDeepMat,worldRoot).transform;wrongCloth.gameObject.SetActive(false);
            for(int i=0;i<waterBeads.Length;i++)waterBeads[i]=MgfLook.Prim(PrimitiveType.Sphere,"WaterBead"+i,Vector3.zero,new Vector3(.10f,.10f,.10f),rainMat,worldRoot,false).transform;
            for(int i=0;i<waterBeads.Length;i++)waterBeads[i].gameObject.SetActive(false);
        }

        void BuildUi()
        {
            softSprite=MakeSprite(96,16,false);ringSprite=MakeSprite(96,0,true);var canvas=MgfText.Canvas;
            titleRt=R("TitleScreen",canvas.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(390,844));titleG=titleRt.gameObject.AddComponent<CanvasGroup>();
            StretchFull(titleRt);
            // 생성 키 아트는 공유 카드 전용이다. 타이틀도 플레이와 같은 실시간 도자/직조 월드를 보여 준다.
            titleArt=Img(titleRt,"GeneratedTitleKeyArt",new Vector2(.5f,.5f),Vector2.zero,new Vector2(1,1),Color.clear);titleArt.gameObject.SetActive(false);
            // 중앙 패널을 없애고 뒤쪽 3D 구름 수문 위에 제목을 직접 얹는다.
            titleBanner=Img(titleRt,"NoCentralPanel",new Vector2(.5f,.83f),Vector2.zero,new Vector2(1,1),Color.clear,softSprite);
            titleLogoShadow=Txt(titleRt,"세모 소나기",new Vector2(.5f,.83f),new Vector2(3,-3),52,Cobalt,365);titleLogoShadow.fontStyle=FontStyles.Bold;
            titleLogo=Txt(titleRt,"세모 소나기",new Vector2(.5f,.83f),Vector2.zero,52,Cream,365);titleLogo.fontStyle=FontStyles.Bold;titleLogo.outlineColor=MgfLook.Hex("#B85C45");titleLogo.outlineWidth=.17f;
            titleTag=Txt(titleRt,"직각삼각형 우산 두 개를 골라 한 획으로 펼쳐라",new Vector2(.5f,.735f),Vector2.zero,16,Cream,354);titleTag.fontStyle=FontStyles.Bold;titleTag.outlineColor=Cobalt;titleTag.outlineWidth=.15f;
            titleMeta=Txt(titleRt,"중학교 2학년 · 피타고라스 정리\n6묶음 중 첫 판단 5묶음에 도전",new Vector2(.5f,.64f),Vector2.zero,14,Cream,350);titleMeta.fontStyle=FontStyles.Bold;titleMeta.outlineColor=Cobalt;titleMeta.outlineWidth=.14f;
            titleHintRt=R("UmbrellaStartHint",titleRt,new Vector2(.5f,.11f),Vector2.zero,new Vector2(300,64));
            titleHint=Txt(titleHintRt,"접힌 우산을 눌러 빗물 수문을 열어라",new Vector2(.5f,.5f),Vector2.zero,18,Cobalt,300);titleHint.fontStyle=FontStyles.Bold;titleHint.outlineColor=Cream;titleHint.outlineWidth=.20f;

            hudRt=R("GameHud",canvas.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(390,844));hudG=hudRt.gameObject.AddComponent<CanvasGroup>();StretchFull(hudRt);
            statusBg=Img(hudRt,"CobaltStatusTile",new Vector2(.5f,.955f),Vector2.zero,new Vector2(374,60),new Color(Cobalt.r,Cobalt.g,Cobalt.b,.97f),softSprite);
            scoreTxt=Txt(hudRt,"0",new Vector2(.12f,.958f),Vector2.zero,21,Cream,70);scoreTxt.fontStyle=FontStyles.Bold;
            progressTxt=Txt(hudRt,"0 / 6",new Vector2(.32f,.958f),Vector2.zero,17,RainLight,82);progressTxt.fontStyle=FontStyles.Bold;
            livesTxt=Txt(hudRt,"보수천 ◆◆◆",new Vector2(.58f,.958f),Vector2.zero,14,Cream,126);livesTxt.fontStyle=FontStyles.Bold;
            timeTxt=Txt(hudRt,"120초",new Vector2(.80f,.958f),Vector2.zero,14,Cream,54);timeTxt.fontStyle=FontStyles.Bold;
            goalBg=Img(hudRt,"TerracottaGoalTile",new Vector2(.5f,.872f),Vector2.zero,new Vector2(360,58),new Color(.72f,.36f,.27f,.98f),softSprite);
            goalTxt=Txt(goalBg.transform,"직각삼각형 우산 두 개를 모두 골라 한 획으로 쓸어라",new Vector2(.5f,.5f),Vector2.zero,14,Cream,344);goalTxt.fontStyle=FontStyles.Bold;
            promptBg=Img(hudRt,"ReadingSlip",new Vector2(.5f,.792f),Vector2.zero,new Vector2(356,56),new Color(Cream.r,Cream.g,Cream.b,.92f),softSprite);
            promptTxt=Txt(hudRt,"",new Vector2(.5f,.792f),Vector2.zero,14,Ink,340);promptTxt.fontStyle=FontStyles.Bold;
            for(int i=0;i<4;i++)
            {
                candidateBg[i]=Img(hudRt,"CandidateCeramicTile"+i,new Vector2(.5f,.5f),Vector2.zero,new Vector2(174,54),new Color(.93f,.86f,.74f,1f),softSprite);
                candidateTxt[i]=Txt(hudRt,"",new Vector2(.5f,.5f),Vector2.zero,16,Cobalt,168);candidateTxt[i].fontStyle=FontStyles.Bold;
                candidateTxt[i].outlineColor=Color.clear;candidateTxt[i].outlineWidth=0;
            }
            referenceTxt=Txt(hudRt,"관계 띠  |  가장 긴 변 c  ·  a²+b²=c²",new Vector2(.5f,.075f),Vector2.zero,14,Cream,362);referenceTxt.fontStyle=FontStyles.Bold;
            referenceBg=Img(hudRt,"CobaltReferenceTile",new Vector2(.5f,.075f),Vector2.zero,new Vector2(372,48),new Color(Cobalt.r,Cobalt.g,Cobalt.b,.97f),softSprite);referenceBg.transform.SetSiblingIndex(referenceTxt.transform.GetSiblingIndex());

            // 연습 경로는 월드 선뿐 아니라 화면 공간 손가락·점선·놓기 순간으로도 보여 준다.
            for(int i=0;i<guideDots.Length;i++)guideDots[i]=Img(hudRt,"GuidePathDot"+i,new Vector2(.5f,.5f),Vector2.zero,new Vector2(9,9),new Color(Gold.r,Gold.g,Gold.b,.78f),softSprite);
            guideHandRt=R("GuideFinger",hudRt,new Vector2(.5f,.5f),Vector2.zero,new Vector2(26,42));
            guideHandImg=guideHandRt.gameObject.AddComponent<Image>();guideHandImg.sprite=softSprite;guideHandImg.type=Image.Type.Sliced;guideHandImg.color=Cream;guideHandImg.raycastTarget=false;
            Img(guideHandRt,"FingerTip",new Vector2(.5f,.92f),Vector2.zero,new Vector2(18,18),Gold,softSprite);
            guideReleaseRt=R("GuideRelease",hudRt,new Vector2(.5f,.5f),Vector2.zero,new Vector2(90,28));
            var release=guideReleaseRt.gameObject.AddComponent<TextMeshProUGUI>();release.font=MgfText.Font;release.fontSize=13;release.color=Cream;release.alignment=TextAlignmentOptions.Center;release.raycastTarget=false;release.fontStyle=FontStyles.Bold;release.text="여기서 놓기";

            revealRt=R("MathReveal",canvas.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(390,844));revealG=revealRt.gameObject.AddComponent<CanvasGroup>();StretchFull(revealRt);
            revealBg=Img(revealRt,"RainEquationCloth",new Vector2(.5f,.245f),Vector2.zero,new Vector2(356,164),new Color(Cream.r,Cream.g,Cream.b,.97f),softSprite);
            revealTitleTxt=Txt(revealBg.transform,"두 우산이 펼쳐졌어요",new Vector2(.5f,.72f),Vector2.zero,23,LeafDark,336);revealTitleTxt.fontStyle=FontStyles.Bold;
            revealFormulaTxt=Txt(revealBg.transform,"",new Vector2(.5f,.35f),Vector2.zero,16,Ink,334);revealFormulaTxt.fontStyle=FontStyles.Bold;

            toastRt=R("Toast",canvas.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(390,844));toastG=toastRt.gameObject.AddComponent<CanvasGroup>();StretchFull(toastRt);
            Img(toastRt,"ToastLeaf",new Vector2(.5f,.25f),Vector2.zero,new Vector2(348,70),new Color(Ink.r,Ink.g,Ink.b,.94f),softSprite);
            toastTxt=Txt(toastRt,"",new Vector2(.5f,.25f),Vector2.zero,14,Cream,330);toastTxt.fontStyle=FontStyles.Bold;
            rippleRt=R("PuddleRipple",hudRt,new Vector2(.5f,.5f),Vector2.zero,new Vector2(76,76));rippleImg=rippleRt.gameObject.AddComponent<Image>();rippleImg.sprite=ringSprite;rippleImg.color=Rain;rippleImg.raycastTarget=false;rippleRt.gameObject.SetActive(false);

            endRt=R("EndScreen",canvas.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(390,844));endG=endRt.gameObject.AddComponent<CanvasGroup>();StretchFull(endRt);
            Img(endRt,"EndRainCloth",new Vector2(.5f,.50f),Vector2.zero,new Vector2(358,490),new Color(LeafDark.r,LeafDark.g,LeafDark.b,.97f),softSprite);
            endTitleTxt=Txt(endRt,"소나기 정원 완성",new Vector2(.5f,.66f),Vector2.zero,34,Cream,338);endTitleTxt.fontStyle=FontStyles.Bold;
            endScoreTxt=Txt(endRt,"0",new Vector2(.5f,.55f),Vector2.zero,58,Gold,330);endScoreTxt.fontStyle=FontStyles.Bold;
            endStatsTxt=Txt(endRt,"",new Vector2(.5f,.43f),Vector2.zero,18,RainLight,320);endStatsTxt.fontStyle=FontStyles.Bold;
            var again=Img(endRt,"FoldedAgainUmbrella",new Vector2(.5f,.28f),Vector2.zero,new Vector2(298,72),CoralDeep,softSprite);
            endTapTxt=Txt(again.transform,"우산 다시 펼치기",new Vector2(.5f,.5f),Vector2.zero,21,Cream,286);endTapTxt.fontStyle=FontStyles.Bold;
            SetGuideUi(false);
        }

        IEnumerator LoadTitleKeyArt()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string page=Application.absoluteURL;string url="./assets/title.png";
            try{if(!string.IsNullOrEmpty(page))url=new Uri(new Uri(page),"./assets/title.png").ToString();}catch(Exception){}
            using(var req=UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();
                if(req.result==UnityWebRequest.Result.Success)
                {
                    var tex=DownloadHandlerTexture.GetContent(req);titleArt.sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),100);titleArt.color=new Color(1,1,1,.88f);
                }
                else titleArt.color=Color.clear; // 코드 비정원 폴백. 콘솔 오류를 만들지 않는다.
            }
#else
            yield break;
#endif
        }

        void ResetWorldForRun()
        {
            for(int i=0;i<candidateRoots.Length;i++){candidateRoots[i].gameObject.SetActive(false);candidateRoots[i].localScale=Vector3.one;candidateCanopies[i].localScale=Vector3.one;candidateCanopies[i].localRotation=Quaternion.identity;candidateCorners[i].localRotation=Quaternion.identity;}
            for(int i=0;i<shelters.Length;i++)shelters[i].gameObject.SetActive(false);
            titleUmbrella.gameObject.SetActive(true);titleUmbrella.localScale=new Vector3(.46f,1.28f,.46f);titleCanopy.localRotation=Quaternion.identity;
            swipeTrail.enabled=false;guideTrail.enabled=false;pulseRing.gameObject.SetActive(false);formulaGlow.gameObject.SetActive(false);wrongCloth.gameObject.SetActive(false);
            for(int i=0;i<waterBeads.Length;i++)waterBeads[i].gameObject.SetActive(false);
            animCorrect=animWrong=false;feedbackAnim=hitStop=wrongKick=clothFall=0;trailCount=0;
        }

        void ResetWorldForProblem(SonagiProblem p,int count)
        {
            titleUmbrella.gameObject.SetActive(false);animCorrect=animWrong=false;feedbackAnim=hitStop=wrongKick=clothFall=0;
            swipeTrail.enabled=false;trailCount=0;formulaGlow.gameObject.SetActive(false);wrongCloth.gameObject.SetActive(false);
            for(int i=0;i<waterBeads.Length;i++)waterBeads[i].gameObject.SetActive(false);
            for(int i=0;i<4;i++)
            {
                candidateRoots[i].gameObject.SetActive(i<count);candidateRoots[i].localScale=Vector3.one;
                candidateCanopies[i].localScale=Vector3.one;candidateCanopies[i].localRotation=Quaternion.Euler(0,(p.rotatedLabels?i*67:0),0);candidateCorners[i].localRotation=Quaternion.identity;
                var renderer=candidateCanopies[i].GetComponent<MeshRenderer>();renderer.sharedMaterial=canopyResinMat;
            }
            for(int i=0;i<shelters.Length;i++)shelters[i].gameObject.SetActive(i<st.solved);
            layoutMode=-1;UpdateLayoutIfNeeded();guideTrail.enabled=phase==GamePhase.Practice;pulseRing.gameObject.SetActive(phase==GamePhase.Practice);UpdatePracticeGuide();
        }

        void SetScreen()
        {
            bool title=phase==GamePhase.Title,game=phase==GamePhase.Practice||phase==GamePhase.Playing||phase==GamePhase.Reveal,end=phase==GamePhase.End;
            titleG.alpha=title?1:0;titleG.blocksRaycasts=false;hudG.alpha=game?1:0;hudG.blocksRaycasts=false;
            revealG.alpha=phase==GamePhase.Reveal?1:0;revealG.blocksRaycasts=false;endG.alpha=end?1:0;endG.blocksRaycasts=false;
            bool showPrompt=phase!=GamePhase.Reveal;goalBg.gameObject.SetActive(showPrompt);goalTxt.gameObject.SetActive(showPrompt);promptBg.gameObject.SetActive(showPrompt);promptTxt.gameObject.SetActive(showPrompt);
            toastG.alpha=toastClock>0&&game?1:0;worldRoot.gameObject.SetActive(true);titleUmbrella.gameObject.SetActive(title);
            SetGuideUi(phase==GamePhase.Practice);
            layoutMode=-1;UpdateLayoutIfNeeded();
        }

        void RefreshProblemUi()
        {
            if(current==null)return;string[] ids={"ㄱ","ㄴ","ㄷ","ㄹ"};
            promptTxt.text=current.prompt;
            for(int i=0;i<4;i++)
            {
                candidateTxt[i].text=ids[i]+(current.certificationAudit?"  [인증 주장]":"")+"\n"+SemoSonagiRules.CandidateLabel(current,i);
                candidateTxt[i].gameObject.SetActive(phase!=GamePhase.Practice||i<2);
                candidateBg[i].gameObject.SetActive(phase!=GamePhase.Practice||i<2);
            }
            goalTxt.text=phase==GamePhase.Practice?"첫 연습  ·  정답 우산 하나만 골라 쓸어라":"직각삼각형 우산 두 개를 모두 골라 한 획으로 쓸어라";
            referenceTxt.text=current.mode==SonagiMode.Area?"관계 띠  |  작은 두 넓이의 합 = 가장 큰 넓이":"관계 띠  |  가장 긴 변 c  ·  a²+b²=c²";
            PositionLabels();
        }

        void UpdateLayoutIfNeeded()
        {
            int mode=(phase==GamePhase.Practice?1:0)|(Screen.width>Screen.height?2:0)|(phase==GamePhase.Title?4:0);
            if(layoutW==Screen.width&&layoutH==Screen.height&&layoutMode==mode)return;
            layoutW=Screen.width;layoutH=Screen.height;layoutMode=mode;float aspect=(float)Screen.width/Mathf.Max(1,Screen.height);
            cam.fieldOfView=aspect<.72f?Mathf.Min(66f,38f*.72f/aspect):38f;
            bool landscape=Screen.width>Screen.height;ConfigureUiLayout(landscape);
            if(phase==GamePhase.Title)
            {
                Vector2 s=new Vector2(Screen.width*.5f,Screen.height*.28f);titleUmbrella.position=ScreenToPlane(s,.72f);titleUmbrella.localScale=new Vector3(.46f,1.28f,.46f);return;
            }
            bool practice=phase==GamePhase.Practice||(phase==GamePhase.Reveal&&revealPractice);
            if(landscape)
            {
                if(practice){candidateScreen[0]=new Vector2(Screen.width*.40f,Screen.height*.46f);candidateScreen[1]=new Vector2(Screen.width*.60f,Screen.height*.46f);}
                else for(int i=0;i<4;i++)candidateScreen[i]=new Vector2(Screen.width*(.17f+i*.22f),Screen.height*.46f);
            }
            else
            {
                if(practice){candidateScreen[0]=new Vector2(Screen.width*.30f,Screen.height*.46f);candidateScreen[1]=new Vector2(Screen.width*.70f,Screen.height*.46f);}
                else
                {
                    candidateScreen[0]=new Vector2(Screen.width*.27f,Screen.height*.59f);candidateScreen[1]=new Vector2(Screen.width*.73f,Screen.height*.59f);
                    candidateScreen[2]=new Vector2(Screen.width*.27f,Screen.height*.34f);candidateScreen[3]=new Vector2(Screen.width*.73f,Screen.height*.34f);
                }
            }
            int count=practice?2:4;
            for(int i=0;i<count;i++)
            {
                candidateHome[i]=ScreenToPlane(candidateScreen[i],.72f);candidateRoots[i].position=candidateHome[i];
                candidateRoots[i].localScale=Vector3.one*(landscape?1.03f:1.06f);
            }
            PositionLabels();UpdatePracticeGuide();
        }

        Vector3 ScreenToPlane(Vector2 screen,float height)
        {
            var ray=cam.ScreenPointToRay(screen);var plane=new Plane(Vector3.up,new Vector3(0,height,0));
            return plane.Raycast(ray,out float d)?ray.GetPoint(d):Vector3.zero;
        }

        void PositionLabels()
        {
            bool practice=phase==GamePhase.Practice||(phase==GamePhase.Reveal&&revealPractice);int count=practice?2:4;
            for(int i=0;i<count;i++)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRt,candidateScreen[i],null,out Vector2 p);
                p.y-=Screen.width>Screen.height?58f:69f;
                ((RectTransform)candidateTxt[i].transform).anchoredPosition=p;((RectTransform)candidateBg[i].transform).anchoredPosition=p;
            }
        }

        void ConfigureUiLayout(bool landscape)
        {
            float w=Mathf.Max(390,hudRt.rect.width),top=landscape?.91f:.955f;
            Place((RectTransform)statusBg.transform,new Vector2(.5f,top),Vector2.zero,new Vector2(Mathf.Min(w-24,720),landscape?48:60));
            Place((RectTransform)scoreTxt.transform,new Vector2(.12f,top),Vector2.zero,new Vector2(70,landscape?42:49));
            Place((RectTransform)progressTxt.transform,new Vector2(.32f,top),Vector2.zero,new Vector2(82,landscape?40:40));
            Place((RectTransform)livesTxt.transform,new Vector2(.58f,top),Vector2.zero,new Vector2(126,landscape?38:33));
            Place((RectTransform)timeTxt.transform,new Vector2(.80f,top),Vector2.zero,new Vector2(60,landscape?38:33));
            float goalY=landscape?.79f:.872f,promptY=landscape?.66f:.792f;
            Place((RectTransform)goalBg.transform,new Vector2(.5f,goalY),Vector2.zero,new Vector2(Mathf.Min(w-40,680),landscape?42:58));
            Place((RectTransform)goalTxt.transform,new Vector2(.5f,goalY),Vector2.zero,new Vector2(Mathf.Min(w-56,650),landscape?38:35));
            float promptWidth=landscape?Mathf.Min(w-58,650):Mathf.Min(w-54,520);
            Place((RectTransform)promptBg.transform,new Vector2(.5f,promptY),Vector2.zero,new Vector2(promptWidth+18,landscape?48:70));
            Place((RectTransform)promptTxt.transform,new Vector2(.5f,promptY),Vector2.zero,new Vector2(promptWidth,landscape?44:62));
            promptTxt.fontSize=landscape?13:Mathf.Min(14,12.6f+390f/Mathf.Max(390,w)*1.4f);
            Place((RectTransform)referenceBg.transform,new Vector2(.5f,.065f),Vector2.zero,new Vector2(Mathf.Min(w-28,700),landscape?40:48));
            Place((RectTransform)referenceTxt.transform,new Vector2(.5f,.065f),Vector2.zero,new Vector2(Mathf.Min(w-44,670),landscape?36:33));
            for(int i=0;i<4;i++)
            {
                ((RectTransform)candidateBg[i].transform).sizeDelta=new Vector2(landscape?144:174,landscape?48:54);
                ((RectTransform)candidateTxt[i].transform).sizeDelta=new Vector2(landscape?138:168,landscape?44:38);
                candidateTxt[i].fontSize=landscape?13:16;
            }
            Place((RectTransform)revealBg.transform,new Vector2(.5f,landscape?.72f:.245f),Vector2.zero,new Vector2(landscape?520:356,landscape?126:164));
            // 고정 390×844 루트가 가로 화면에서 위아래를 잘라 먹지 않도록 모든 패널을 실제 캔버스에 맞춘다.
            if(landscape)
            {
                Place((RectTransform)titleBanner.transform,new Vector2(.5f,.78f),Vector2.zero,new Vector2(1,1));
                Place((RectTransform)titleLogoShadow.transform,new Vector2(.5f,.80f),new Vector2(3,-4),new Vector2(365,105));
                Place((RectTransform)titleLogo.transform,new Vector2(.5f,.80f),Vector2.zero,new Vector2(365,105));
                Place((RectTransform)titleTag.transform,new Vector2(.5f,.66f),Vector2.zero,new Vector2(390,44));
                Place((RectTransform)titleMeta.transform,new Vector2(.5f,.50f),Vector2.zero,new Vector2(350,66));
            }
            else
            {
                Place((RectTransform)titleBanner.transform,new Vector2(.5f,.83f),Vector2.zero,new Vector2(1,1));
                Place((RectTransform)titleLogoShadow.transform,new Vector2(.5f,.85f),new Vector2(3,-4),new Vector2(365,122));
                Place((RectTransform)titleLogo.transform,new Vector2(.5f,.85f),Vector2.zero,new Vector2(365,122));
                Place((RectTransform)titleTag.transform,new Vector2(.5f,.735f),Vector2.zero,new Vector2(356,48));
                Place((RectTransform)titleMeta.transform,new Vector2(.5f,.64f),Vector2.zero,new Vector2(350,66));
            }
        }

        void BeginSwipeVisual(Vector2 screen)
        {
            trailCount=1;swipeTrail.enabled=true;Vector3 p=ScreenToPlane(screen,1.48f);
            for(int i=0;i<swipeTrail.positionCount;i++)swipeTrail.SetPosition(i,p);swipeTrail.widthMultiplier=.13f;
            pulseRing.gameObject.SetActive(false);guideTrail.enabled=false;
        }

        void MoveSwipeVisual(Vector2 screen)
        {
            Vector3 p=ScreenToPlane(screen,1.48f);trailCount=Mathf.Min(swipeTrail.positionCount,trailCount+1);
            for(int i=0;i<swipeTrail.positionCount-1;i++)swipeTrail.SetPosition(i,swipeTrail.GetPosition(i+1));swipeTrail.SetPosition(swipeTrail.positionCount-1,p);
        }

        void EndSwipeVisual(){if(st.touchedMask==0||swipeLength<24f)swipeTrail.enabled=false;}

        void TouchCanopyVisual(int i)
        {
            if(i<0||i>=4)return;MgfFx.Punch(candidateRoots[i],.13f,.25f);candidateCanopies[i].GetComponent<MeshRenderer>().sharedMaterial=goldMat;
        }

        void FoldTappedCanopy(int mask)
        {
            for(int i=0;i<4;i++)if((mask&(1<<i))!=0)candidateCorners[i].localRotation=Quaternion.Euler(18,0,22);
        }

        void BeginRevealVisual(bool correct,bool practice,int mask,string misconception)
        {
            // 직전 재시도 안내가 다음 판정식 위에 겹치지 않게 새 reveal이 소유권을 가진다.
            toastClock=0;toastG.alpha=0;
            feedbackAnim=0;revealUiClock=0;animCorrect=correct;animWrong=!correct;
            if(correct)
            {
                hitStop=.075f;swipeTrail.enabled=true;swipeTrail.widthMultiplier=.22f;MgfSfx.Play("correct",.46f);snailReact=1f;
                revealTitleTxt.text=practice?"첫 우산이 펼쳐졌어요":current.certificationAudit?"인증표를 검산했어요":"도자 홈통이 연결됐어요";revealTitleTxt.color=Leaf;
                revealFormulaTxt.text=practice?"9 + 16 = 25\n작은 두 정사각형의 넓이를 더했어요":current.certificationAudit?AuditFormulaText():CorrectFormulaText(mask);
                for(int i=0;i<waterBeads.Length;i++){waterBeads[i].gameObject.SetActive(true);waterBeads[i].position=ScreenToPlane(Vector2.Lerp(candidateScreen[FirstBit(mask)],candidateScreen[LastBit(mask)],i/(float)(waterBeads.Length-1)),1.44f);}
            }
            else
            {
                wrongKick=1f;clothFall=1f;MgfFx.Shake(cam,.075f,.20f);MgfSfx.Play("wrong",.34f);
                int selected=FirstBit(mask);wrongCloth.position=(selected>=0?candidateRoots[selected].position:new Vector3(0,2,1))+Vector3.up*1.6f;wrongCloth.gameObject.SetActive(true);
                revealTitleTxt.text=practice?"비교 우산은 함께 쓸지 않아요":"보수 천 한 장이 젖었어요";revealTitleTxt.color=CoralDeep;
                revealFormulaTxt.text=current.certificationAudit?WrongFormula(current.mode,misconception)+"\n"+AuditFormulaText():WrongFormula(current.mode,misconception)+"\n"+CorrectFormulaText(current.correctMask);
                for(int i=0;i<4;i++)if((mask&(1<<i))!=0)candidateCorners[i].localRotation=Quaternion.Euler(32,0,(i%2==0?-34:34));
            }
            if(current.certificationAudit)ShowAuditCandidateResults();
        }

        int FirstBit(int mask){for(int i=0;i<4;i++)if((mask&(1<<i))!=0)return i;return 0;}
        int LastBit(int mask){for(int i=3;i>=0;i--)if((mask&(1<<i))!=0)return i;return 0;}

        string CorrectFormulaText(int mask)
        {
            string s="";string[] ids={"ㄱ","ㄴ","ㄷ","ㄹ"};
            for(int i=0;i<4;i++)if((mask&(1<<i))!=0)s+=(s.Length==0?"":"\n")+ids[i]+"  "+SemoSonagiRules.Formula(current,i);
            return s;
        }

        string AuditFormulaText()
        {
            string s="";string[] ids={"ㄱ","ㄴ","ㄷ","ㄹ"};
            for(int i=0;i<4;i++)s+=(i==0?"":"\n")+ids[i]+"  "+SemoSonagiRules.Formula(current,i);
            return s;
        }

        void ShowAuditCandidateResults()
        {
            string[] ids={"ㄱ","ㄴ","ㄷ","ㄹ"};
            for(int i=0;i<4;i++)
            {
                bool right=SemoSonagiRules.IsRight(current.mode,current.candidates[i]);
                candidateTxt[i].text=ids[i]+(right?"  [직각 인증 완료]":"  [인증 반증]")+"\n"+SemoSonagiRules.CandidateLabel(current,i);
            }
        }

        string WrongFormula(SonagiMode mode,string id)
        {
            if(mode==SonagiMode.Area)
            {
                switch(id)
                {
                    case "near_square":return "가까운 값이 아니라 작은 두 넓이의 합과 가장 큰 넓이가 정확히 같아야 한다.";
                    case "no_square":return "세 수는 이미 정사각형의 넓이다. 작은 두 넓이를 그대로 더해 가장 큰 넓이와 비교하라.";
                    case "timeout":return "가장 큰 넓이를 찾고, 나머지 두 넓이의 합과 비교하라.";
                    default:break;
                }
            }
            switch(id)
            {
                case "near_square":return "비슷한 값이 아니라 두 제곱의 합이 정확히 같아야 한다.";
                case "no_square":return "변의 길이는 그대로 더하지 말고 각각 제곱하라.";
                case "hypotenuse_by_position":return "그림 방향보다 가장 긴 변을 먼저 찾아라.";
                case "cardinality":return "이번 소나기에는 직각삼각형 우산이 정확히 두 개다.";
                case "timeout":return "가장 긴 변부터 다시 살펴보라.";
                case "empty_sweep":return "우산 천의 중심을 지나도록 쓸어라.";
                default:return "같은 개략도라도 세 수의 관계를 따로 확인하라.";
            }
        }

        void CompleteRevealVisual(bool correct,int mask)
        {
            if(correct&&!revealPractice&&st.solved>0)
            {
                int idx=Mathf.Clamp(st.solved-1,0,shelters.Length-1);shelters[idx].gameObject.SetActive(true);shelterCanopies[idx].localScale=Vector3.one*1.14f;
            }
            animCorrect=animWrong=false;hitStop=0;swipeTrail.enabled=false;formulaGlow.gameObject.SetActive(false);wrongCloth.gameObject.SetActive(false);
            for(int i=0;i<waterBeads.Length;i++)waterBeads[i].gameObject.SetActive(false);
        }

        void ShowWrongReason(string id){toastTxt.text=WrongFormula(current.mode,id);toastClock=2.5f;toastG.alpha=1;}

        void ShowEndVisual(string reason)
        {
            endDisplayedScore=0;endScoreTxt.text="0";
            if(reason=="clear")
            {
                endTitleTxt.text="달팽이 비정원 완성";endTitleTxt.color=Gold;
                endStatsTxt.text="우산 묶음 "+st.solved+" / 6\n첫 판단 "+st.firstAttemptCorrect+" / 6 정답\n모든 달팽이가 비를 피했어요";
                MgfFx.Glow(new Vector3(0,3,5),RainLight,28,.55f);
            }
            else
            {
                endTitleTxt.text=reason=="timer"?"소나기가 지나갔어요":reason=="cloth"?"보수 천을 모두 썼어요":"첫 판단 목표를 다시 노려요";endTitleTxt.color=Coral;
                endStatsTxt.text="완성한 우산 묶음 "+st.solved+" / 6\n첫 판단 "+st.firstAttemptCorrect+" / "+Math.Max(1,st.firstAttemptTotal)+" 정답\n가장 긴 변부터 찾고 다시 검산해요";
            }
        }

        void PulseTitleUmbrella(){titlePulse=1f;MgfFx.Punch(titleUmbrella,.18f,.30f);MgfSfx.Play("pop",.28f);}
        void ReactSnails(){snailReact=1f;}

        void BoostGuide()
        {
            guideBoost=1f;guideDemoClock=0;if(phase==GamePhase.Practice){guideTrail.enabled=true;pulseRing.gameObject.SetActive(true);SetGuideUi(true);UpdatePracticeGuide();}
        }

        void UpdatePracticeGuide()
        {
            if(phase!=GamePhase.Practice&&!(phase==GamePhase.Reveal&&revealPractice))return;
            Vector2 a=candidateScreen[0]+new Vector2(-TouchRadiusPixels()*.70f,0),b=candidateScreen[0]+new Vector2(TouchRadiusPixels()*.70f,0);
            for(int i=0;i<guideTrail.positionCount;i++)
            {
                float t=i/(float)(guideTrail.positionCount-1);Vector2 s=Vector2.Lerp(a,b,t);s.y+=Mathf.Sin(t*Mathf.PI)*18f;
                guideTrail.SetPosition(i,ScreenToPlane(s,1.55f));
            }
            pulseRing.position=candidateRoots[0].position+Vector3.up*.03f;
        }

        void SetGuideUi(bool visible)
        {
            if(!guideHandRt)return;
            guideHandRt.gameObject.SetActive(visible);guideReleaseRt.gameObject.SetActive(visible);
            for(int i=0;i<guideDots.Length;i++)guideDots[i].gameObject.SetActive(visible);
        }

        void UpdateGuideUi(float dt)
        {
            bool visible=phase==GamePhase.Practice;
            SetGuideUi(visible);if(!visible)return;
            guideDemoClock+=dt;float cycle=(guideDemoClock%1.2f)/1.2f;
            float move=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.72f,cycle));
            Vector2 a=candidateScreen[0]+new Vector2(-TouchRadiusPixels()*.82f,0);
            Vector2 b=candidateScreen[0]+new Vector2(TouchRadiusPixels()*.82f,0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRt,a,null,out Vector2 la);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRt,b,null,out Vector2 lb);
            guideHandRt.anchoredPosition=Vector2.Lerp(la,lb,move)+new Vector2(0,26+Mathf.Sin(move*Mathf.PI)*8);
            guideHandRt.localScale=Vector3.one*(cycle>.72f?Mathf.Lerp(1,.72f,Mathf.InverseLerp(.72f,.92f,cycle)):1);
            guideReleaseRt.anchoredPosition=lb+new Vector2(0,58);guideReleaseRt.gameObject.SetActive(cycle>.68f);
            for(int i=0;i<guideDots.Length;i++)
            {
                float t=i/(float)(guideDots.Length-1);var rt=(RectTransform)guideDots[i].transform;
                rt.anchoredPosition=Vector2.Lerp(la,lb,t)+new Vector2(0,Mathf.Sin(t*Mathf.PI)*7);
                guideDots[i].color=new Color(Gold.r,Gold.g,Gold.b,i/(float)guideDots.Length<=move ? .85f : .28f);
            }
        }

        void RippleAtScreen(Vector2 screen,string msg)
        {
            float sx=screen.x/Mathf.Max(1,Screen.width)*390f-195f,sy=screen.y/Mathf.Max(1,Screen.height)*844f-422f;
            rippleRt.anchoredPosition=new Vector2(sx,sy);rippleRt.localScale=Vector3.one*.35f;rippleImg.color=Rain;rippleClock=.48f;rippleRt.gameObject.SetActive(true);
            toastTxt.text=msg;toastClock=1.35f;toastG.alpha=1;
        }

        void UpdateWorld(float dt)
        {
            worldClock+=dt;
            for(int i=0;i<rainDrops.Length;i++)
            {
                var p=rainDrops[i].position;p.y-=dt*(2.7f+(i%5)*.18f);p.x+=dt*.22f;if(p.y<-.1f)p.y=7.8f+(i%4)*.45f;rainDrops[i].position=p;
            }
            for(int i=0;i<leaves.Length;i++)leaves[i].localRotation=Quaternion.Euler(8,i*37,Mathf.Sin(worldClock*.75f+i*.7f)*8f);
            for(int i=0;i<snails.Length;i++)
            {
                float breathe=1f+.035f*Mathf.Sin(worldClock*1.4f+i);snailBodies[i].localScale=new Vector3(.40f*breathe,.68f/breathe,.34f*breathe);
                for(int k=0;k<2;k++)snailFeelers[i*2+k].localRotation=Quaternion.Euler(0,0,(k==0?-15:15)+Mathf.Sin(worldClock*1.8f+i+k)*7f*(1+snailReact));
            }
            snailReact=Mathf.Max(0,snailReact-dt*1.8f);
            if(phase==GamePhase.Title)
            {
                float p=1f+.035f*Mathf.Sin(worldClock*2.0f)+titlePulse*.13f;titleUmbrella.localScale=new Vector3(.46f/p,1.28f*p,.46f/p);titlePulse=Mathf.Max(0,titlePulse-dt*5f);
            }
            if(guideBoost>0)guideBoost=Mathf.Max(0,guideBoost-dt*.55f);
            if(pulseRing.gameObject.activeSelf)
            {
                float p=1+.18f*Mathf.Sin(worldClock*5.1f)+guideBoost*.38f;pulseRing.localScale=new Vector3(p,.08f,p);
            }
            if(guideTrail.enabled)guideTrail.material.mainTextureOffset=new Vector2(-worldClock*1.6f,0);
            if(swipeTrail.enabled)swipeTrail.material.mainTextureOffset=new Vector2(-worldClock*2.2f,0);
            if(hitStop>0){hitStop=Mathf.Max(0,hitStop-dt);return;}
            if(animCorrect)
            {
                feedbackAnim=Mathf.Min(1,feedbackAnim+dt*.82f);
                int order=0;
                for(int i=0;i<4;i++)if((revealMask&(1<<i))!=0)
                {
                    float local=Mathf.Clamp01((feedbackAnim-order*.28f)/.56f);float overshoot=Mathf.Sin(local*Mathf.PI)*.28f;
                    candidateCanopies[i].localScale=new Vector3(1+overshoot,Mathf.Lerp(.72f,1.18f,local),1+overshoot);
                    candidateCanopies[i].localRotation=Quaternion.Euler(0,current.rotatedLabels?i*67:0,Mathf.Sin(local*Mathf.PI)*-7f);order++;
                    candidateCorners[i].localRotation=Quaternion.Euler(Mathf.Lerp(34f,0,local),0,Mathf.Lerp(i%2==0?-28f:28f,0,local));
                }
                for(int i=0;i<waterBeads.Length;i++)
                {
                    float u=(i/(float)waterBeads.Length+worldClock*.45f)%1f;waterBeads[i].localScale=Vector3.one*(.08f+.12f*Mathf.Sin(u*Mathf.PI));waterBeads[i].position+=Vector3.up*dt*.16f;
                }
                int first=FirstBit(revealMask);formulaGlow.position=candidateRoots[first].position+Vector3.up*.05f;formulaGlow.gameObject.SetActive(true);formulaGlow.localScale=Vector3.one*(1.0f+.14f*Mathf.Sin(worldClock*4f));
                // 생물 활주 대신 관절 전개와 수로의 물방울 방향 전환이 정답 사건을 맡는다.
            }
            if(animWrong)
            {
                feedbackAnim=Mathf.Min(1,feedbackAnim+dt*1.45f);float wobble=Mathf.Sin(feedbackAnim*Mathf.PI*7)*(1-feedbackAnim)*13f;
                for(int i=0;i<4;i++)if((revealMask&(1<<i))!=0)candidateCanopies[i].localRotation=Quaternion.Euler(0,current.rotatedLabels?i*67:0,wobble);
                if(feedbackAnim>.48f)
                {
                    for(int i=0;i<4;i++)if((current.correctMask&(1<<i))!=0)
                    {
                        float q=1+.055f*Mathf.Sin(worldClock*7f);candidateRoots[i].localScale=Vector3.one*q;
                    }
                }
                if(wrongCloth.gameObject.activeSelf){clothFall=Mathf.Max(0,clothFall-dt*.75f);wrongCloth.position+=new Vector3(dt*.32f,-dt*2.1f,0);wrongCloth.localRotation*=Quaternion.Euler(dt*110f,dt*55f,0);}
            }
        }

        void UpdateUi(float dt)
        {
            // CanvasScaler의 최종 스케일은 첫 레이아웃 뒤 확정된다. 실제 캔버스 좌표로 계속 붙여
            // 초와이드에서도 첫/마지막 카드가 화면 가장자리로 밀리지 않게 한다.
            if(current!=null&&(phase==GamePhase.Practice||phase==GamePhase.Playing||phase==GamePhase.Reveal))PositionLabels();
            UpdateGuideUi(dt);
            displayedScore=Mathf.MoveTowards(displayedScore,st.score,Mathf.Max(120,Mathf.Abs(st.score-displayedScore)*5)*dt);
            int score=(int)displayedScore;if(score!=shownScore){shownScore=score;scoreTxt.text=score.ToString("N0");scoreTxt.transform.localScale=Vector3.one*(1+.08f*Mathf.Sin(Mathf.Clamp01(Mathf.Abs(st.score-displayedScore)/80f)*Mathf.PI));}
            progressTxt.text=st.solved+" / "+SemoSonagiRules.TargetSolved;
            string cloth="";for(int i=0;i<st.lives;i++)cloth+="◆";for(int i=st.lives;i<SemoSonagiRules.StartLives;i++)cloth+="◇";livesTxt.text="보수천 "+cloth;
            int sec=Mathf.Max(0,Mathf.CeilToInt(sessionLeft));if(sec!=shownSecond){shownSecond=sec;timeTxt.text=phase==GamePhase.Practice?"연습":sec+"초";}
            if(revealG.alpha>0){revealUiClock+=dt;float p=1+Mathf.Sin(Mathf.Min(1,revealUiClock/.35f)*Mathf.PI)*.09f;revealBg.transform.localScale=Vector3.one*p;}
            if(toastClock>0){toastClock=Mathf.Max(0,toastClock-dt);toastG.alpha=Mathf.Min(1,toastClock*5);}
            if(rippleClock>0){rippleClock=Mathf.Max(0,rippleClock-dt);float u=1-rippleClock/.48f;rippleRt.localScale=Vector3.one*Mathf.Lerp(.35f,1.7f,u);rippleImg.color=new Color(Rain.r,Rain.g,Rain.b,1-u);if(rippleClock<=0)rippleRt.gameObject.SetActive(false);}
            if(phase==GamePhase.Title)
            {
                float p=1+.018f*Mathf.Sin(worldClock*1.15f);titleLogo.transform.localScale=Vector3.one*p;titleHintRt.localScale=Vector3.one*(1+.035f*Mathf.Sin(worldClock*2.2f));
            }
            if(phase==GamePhase.End)
            {
                endDisplayedScore=Mathf.MoveTowards(endDisplayedScore,st.score,Mathf.Max(180,st.score*1.8f)*dt);endScoreTxt.text=((int)endDisplayedScore).ToString("N0");endScoreTxt.transform.localScale=Vector3.one*(1+.05f*Mathf.Sin(worldClock*2.6f));
            }
        }
    }
}
