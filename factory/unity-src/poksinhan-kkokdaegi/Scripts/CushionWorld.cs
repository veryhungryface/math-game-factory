// 폭신한 꼭대기 — 진홍 누비 방석 절벽. 눈금 틀 1칸이 누비 1칸과 정확히 같아 넓이를 칸으로 셀 수 있다.
// 정답 사건: 납작한 천의 네 변을 도마뱀이 지퍼 꼬리로 박고 돌면 방석이 부풀어 오른다(정답에는 화면 흔들림 없음).
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.PoksinhanKkokdaegi
{
    public sealed class CushionWorld
    {
        const float LayerH=.46f,GeckoScale=.74f;
        static readonly Color Ivory=MgfLook.Hex("#F5E7CC"), Ink=MgfLook.Hex("#352A32"), Lime=MgfLook.Hex("#B9CC45");

        public Camera Cam { get; private set; }
        Texture2D quilt, geckoTex;
        Material clothHot, stitchIvory, cloth, clothDim, clothDeep, ivory, ink, lime, spool, gridMat, shadowMat, fluffMat, stitchMat, tornMat;
        readonly Mesh[] pillows=new Mesh[9];
        Mesh cliffPillow, titlePillow, geckoStand, geckoBelly, tornMesh;
        Transform titleRoot, gameRoot, rig, active, gecko, geckoShadow, tear;
        MeshFilter activeMesh, geckoMesh;
        LineRenderer zip;
        Transform tearLine;
        readonly Transform[] stuffing=new Transform[4];
        readonly Transform[] titleLayers=new Transform[4];
        readonly Transform[] tower=new Transform[6];
        readonly MeshFilter[] towerMesh=new MeshFilter[6];
        readonly Transform[] towerBadge=new Transform[6];
        readonly Transform[] scraps=new Transform[3];
        readonly Transform[] fluffs=new Transform[14];
        readonly Vector3[] fluffSeed=new Vector3[14];
        readonly List<Transform> titleSway=new List<Transform>();
        Transform titleGecko;
        MeshFilter titleGeckoMesh;
        int side, scrapCount, solvedFloors;
        float rigY, rigTarget, tearLen, openClock, nudge, shake, landClock=9f, landAmp, geckoHop;
        bool isTitle=true, isEnd, endClear, revealOk, puffed;
        Vector3 geckoHome;

        struct CamPose { public Vector3 focus; public float pitch,yaw,halfW,halfH,sx,sy; }
        CamPose cur,goal;
        bool camSnap=true;

        public void Build()
        {
            quilt=Resources.Load<Texture2D>("PoksinhanKkokdaegi/quilt");
            quilt.wrapMode=TextureWrapMode.Repeat;quilt.filterMode=FilterMode.Trilinear;quilt.anisoLevel=4;
            geckoTex=Resources.Load<Texture2D>("PoksinhanKkokdaegi/gecko");
            geckoTex.wrapMode=TextureWrapMode.Clamp;

            MgfLook.Sky(MgfLook.Hex("#6E243A"),MgfLook.Hex("#A83C58"),MgfLook.Hex("#4E1A2C"),.95f);
            MgfLook.Sun(new Vector3(58,-38,0),MgfLook.Hex("#FFE9CC"),1.25f,.68f);
            Cam=MgfLook.Camera(new Vector3(0,20,-30),Vector3.zero,24f);
            cloth=MgfLook.Lit(Color.white,.05f,0,null,quilt);
            clothDim=MgfLook.Lit(new Color(.80f,.72f,.76f),.05f,0,null,quilt);
            clothDeep=MgfLook.Lit(new Color(.50f,.40f,.46f),.04f,0,null,quilt);
            clothHot=MgfLook.Lit(new Color(1.2f,1.12f,1.1f),.05f,0,new Color(.10f,.02f,.03f),quilt);
            stitchIvory=MgfLook.Unlit(Ivory);
            ivory=MgfLook.Lit(Ivory,.10f,0);ink=MgfLook.Lit(Ink,.30f,0);lime=MgfLook.Lit(Lime,.25f,0);
            spool=MgfLook.Lit(MgfLook.Hex("#D9B98A"),.22f,0);
            tornMat=MgfLook.Lit(new Color(.92f,.74f,.74f),.04f,0,null,quilt);
            gridMat=MgfLook.Alpha(new Color(Ivory.r,Ivory.g,Ivory.b,.62f));
            shadowMat=MgfLook.Alpha(new Color(.16f,.03f,.08f,.42f),MgfLook.SoftDot);
            fluffMat=MgfLook.Alpha(new Color(1f,.95f,.86f,.72f),MgfLook.SoftDot);
            stitchMat=MgfLook.Unlit(Lime);

            for(int s=1;s<=8;s++)pillows[s]=PillowMesh(s,4);
            cliffPillow=PillowMesh(9,3,0f,.75f);
            titlePillow=PillowMesh(8,4,.035f,.6f);
            tornMesh=PillowMesh(1,4);
            geckoStand=SpriteQuad(new Rect(0,.5f,1,.5f),3.0f);
            geckoBelly=SpriteQuad(new Rect(0,0,1,.5f),3.0f);

            titleRoot=new GameObject("TitleQuilt").transform;
            gameRoot=new GameObject("CushionCliff").transform;
            BuildCliff();
            BuildProps();
            BuildTitle();
            BuildGame();
            BuildFluff();
        }

        // ── 메시
        static Mesh PillowMesh(int cells,int perCell,float tuftAmp=.09f,float edge=.36f)
        {
            int n=cells*perCell,row=n+1;
            var v=new Vector3[row*row*2];var uv=new Vector2[v.Length];var tri=new int[n*n*12];
            const float mid=.20f;
            for(int k=0;k<2;k++)for(int j=0;j<=n;j++)for(int i=0;i<=n;i++)
            {
                float u=i/(float)perCell,w=j/(float)perCell;
                float d=Mathf.Min(u,cells-u,w,cells-w);
                float e=d>=edge?1f:Mathf.Sqrt(Mathf.Max(0,1f-(1f-d/edge)*(1f-d/edge)));
                float tuft=Mathf.Abs(Mathf.Sin(u*Mathf.PI)*Mathf.Sin(w*Mathf.PI));tuft=tuft*(2f-tuft);
                float y=k==0?mid+(.13f+tuftAmp*tuft)*e:mid-mid*.92f*e;
                int idx=k*row*row+j*row+i;
                v[idx]=new Vector3(u,y,-w);uv[idx]=new Vector2(u,w);
            }
            int t=0;
            for(int k=0;k<2;k++)for(int j=0;j<n;j++)for(int i=0;i<n;i++)
            {
                int a=k*row*row+j*row+i,b=a+1,c=a+row,d=c+1;
                if(k==0){tri[t++]=a;tri[t++]=b;tri[t++]=c;tri[t++]=b;tri[t++]=d;tri[t++]=c;}
                else{tri[t++]=a;tri[t++]=c;tri[t++]=b;tri[t++]=b;tri[t++]=c;tri[t++]=d;}
            }
            var m=new Mesh{name="Pillow"+cells};
            m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt16;
            m.vertices=v;m.uv=uv;m.triangles=tri;m.RecalculateNormals();m.RecalculateBounds();m.UploadMeshData(true);
            return m;
        }

        static Mesh SpriteQuad(Rect r,float width)
        {
            float h=width*r.height/r.width;
            var m=new Mesh{name="Sprite"};
            m.vertices=new[]{new Vector3(-width/2,0,0),new Vector3(width/2,0,0),new Vector3(-width/2,h,0),new Vector3(width/2,h,0)};
            m.uv=new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax)};
            m.triangles=new[]{0,2,1,1,2,3};m.RecalculateBounds();return m;
        }

        static Mesh TiledQuad(float w,float h,float cell)
        {
            var m=new Mesh{name="Tiled"};
            m.vertices=new[]{new Vector3(-w/2,0,-h/2),new Vector3(w/2,0,-h/2),new Vector3(-w/2,0,h/2),new Vector3(w/2,0,h/2)};
            m.uv=new[]{Vector2.zero,new Vector2(w/cell,0),new Vector2(0,h/cell),new Vector2(w/cell,h/cell)};
            m.normals=new[]{Vector3.up,Vector3.up,Vector3.up,Vector3.up};
            m.triangles=new[]{0,2,1,1,2,3};m.RecalculateBounds();return m;
        }

        static GameObject MeshObj(string name,Mesh mesh,Material mat,Transform parent,Vector3 pos,Vector3 scale,bool shadows=true)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;
            g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;
            r.shadowCastingMode=shadows?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=shadows;
            return g;
        }

        // ── 세계
        void BuildCliff()
        {
            var root=new GameObject("Cliff").transform;
            MeshObj("SofaFloor",TiledQuad(160,160,3.4f),clothDeep,root,new Vector3(0,-24,0),Vector3.one);
            var wall=MeshObj("QuiltWall",TiledQuad(160,70,3.4f),clothDeep,root,new Vector3(0,12,20),Vector3.one,false).transform;
            wall.localRotation=Quaternion.Euler(-90,0,0);
            // 중앙 절벽: 게임 내내 화면 아래를 채우는 큰 방석 더미.
            for(int i=0;i<9;i++)
            {
                float y=-LayerH*2.5f*(i+1);
                float jx=(i%3-1)*.34f,jz=((i*2)%3-1)*.26f;
                MeshObj("CliffCushion"+i,cliffPillow,i%2==0?clothDeep:clothDim,root,new Vector3(-4.5f+jx,y,4.5f+jz),new Vector3(1,2.5f,1));
            }
            // 양옆 방석 기둥: 가로 화면의 빈 자리를 같은 세계로 채운다.
            for(int side01=0;side01<2;side01++)
            for(int c=0;c<2;c++)
            for(int i=0;i<5;i++)
            {
                float sign=side01==0?-1:1;
                float x=sign*(12.5f+c*9.5f)-4.5f;
                float y=-1.8f-c*2.6f-i*1.9f+(side01==0?1.0f:0f);
                MeshObj("Pillar",cliffPillow,(i+c)%2==0?clothDim:clothDeep,root,new Vector3(x,y,8f-c*5f+(i%2)*.4f),new Vector3(1,4f,1));
            }
            // 뒤쪽 큰 등받이.
            for(int i=0;i<3;i++)
                MeshObj("BackRest",cliffPillow,i%2==0?clothDim:clothDeep,root,new Vector3(-15+i*10f,-2.2f,17f),new Vector3(1.1f,5f,.5f));
        }

        void BuildProps()
        {
            var root=new GameObject("Props").transform;
            Button(root,new Vector3(6.8f,.2f,5.5f),.9f,lime);
            Button(root,new Vector3(-7.4f,-.6f,2.2f),1.1f,ivory);
            Button(root,new Vector3(7.6f,-1.6f,-2.2f),.8f,ink);
            Spool(root,new Vector3(-7.6f,-.3f,-3.2f),1.0f);
            Spool(root,new Vector3(7.3f,-.9f,1.8f),.75f);
        }

        void Button(Transform root,Vector3 p,float s,Material m)
        {
            var b=MgfLook.Prim(PrimitiveType.Cylinder,"Button",p,new Vector3(s*1.5f,.10f,s*1.5f),m,root,false).transform;
            b.localRotation=Quaternion.Euler(6,0,-8);
            for(int i=0;i<4;i++)
            {
                float a=i*Mathf.PI/2+.78f;
                MgfLook.Prim(PrimitiveType.Cylinder,"Hole",new Vector3(Mathf.Cos(a)*.17f,1.05f,Mathf.Sin(a)*.17f),new Vector3(.14f,.1f,.14f),ink,b,false);
            }
        }

        void Spool(Transform root,Vector3 p,float s)
        {
            var g=new GameObject("Spool").transform;g.SetParent(root,false);g.localPosition=p;g.localScale=Vector3.one*s;
            MgfLook.Prim(PrimitiveType.Cylinder,"Core",new Vector3(0,.6f,0),new Vector3(1.05f,.42f,1.05f),ivory,g,false);
            MgfLook.Prim(PrimitiveType.Cylinder,"Lo",new Vector3(0,.1f,0),new Vector3(1.5f,.09f,1.5f),spool,g,false);
            MgfLook.Prim(PrimitiveType.Cylinder,"Hi",new Vector3(0,1.1f,0),new Vector3(1.5f,.09f,1.5f),spool,g,false);
        }

        void BuildTitle()
        {
            titleRoot.position=Vector3.zero;
            float[] sx={1.34f,1.22f,1.12f,1.0f};
            float[] jitter={-.25f,.30f,-.16f,.14f};
            for(int i=0;i<4;i++)
            {
                var pivot=new GameObject("TitleLayer"+i).transform;pivot.SetParent(titleRoot,false);
                pivot.localPosition=new Vector3(jitter[i],i*1.20f,0);
                MeshObj("Cushion",titlePillow,i%2==0?cloth:clothDim,pivot,new Vector3(-4*sx[i],0,4*sx[i]),new Vector3(sx[i],3.0f,sx[i]));
                titleLayers[i]=pivot;
            }
            // 제목은 앞쪽 방석에 꿰맨 천 라벨 위에 인쇄된다.
            var label=new GameObject("SewnLabel").transform;label.SetParent(titleRoot,false);label.localPosition=new Vector3(0,3.3f,-6.1f);
            label.localRotation=Quaternion.Euler(14,-8,0);
            var plate=MgfLook.Block("LabelCloth",Vector3.zero,new Vector3(8.8f,4.2f,.16f),.14f,ivory,label);
            for(int i=0;i<30;i++)
            {
                float u=(i+.5f)/30f;
                MgfLook.Block("St",new Vector3(-4.1f+u*8.2f,1.88f,-.1f),new Vector3(.17f,.05f,.03f),.01f,clothDeepStitch(),label);
                MgfLook.Block("St",new Vector3(-4.1f+u*8.2f,-1.88f,-.1f),new Vector3(.17f,.05f,.03f),.01f,clothDeepStitch(),label);
            }
            var t=MgfText.World("폭신한\n꼭대기",new Vector3(0,.42f,-.14f),11.5f,Ink,label);t.fontStyle=FontStyles.Bold;t.lineSpacing=-8;
            var tag=MgfText.World("늘려라 네모 방석",new Vector3(0,-1.42f,-.14f),4.2f,MgfLook.Hex("#8C2F49"),label);tag.fontStyle=FontStyles.Bold;

            // 시작 고리: 라벨 아래 솔기에 매달린 지퍼 손잡이 꼬리표.
            var hang=new GameObject("StartTag").transform;hang.SetParent(label,false);hang.localPosition=new Vector3(0,-3.05f,-.05f);
            MgfLook.Block("Strap",new Vector3(0,.72f,0),new Vector3(.32f,.8f,.07f),.03f,lime,hang);
            MgfLook.Block("TagPlate",Vector3.zero,new Vector3(6.0f,1.2f,.18f),.34f,lime,hang);
            var st=MgfText.World("고리를 당겨 시작",new Vector3(0,-.02f,-.14f),4.4f,Ink,hang);st.fontStyle=FontStyles.Bold;
            titleSway.Add(hang);

            titleGecko=new GameObject("TitleGecko").transform;titleGecko.SetParent(titleRoot,false);titleGecko.localPosition=new Vector3(-1.6f,4.9f,-2.6f);
            titleGeckoMesh=MeshObj("Sprite",geckoStand,GeckoMat(),titleGecko,Vector3.zero,Vector3.one*1.2f,false).GetComponent<MeshFilter>();
            MeshObj("Shadow",TiledQuad(3,1.5f,1),shadowMat,titleGecko,new Vector3(0,.03f,0),Vector3.one,false);
        }

        Material clothStitchCache;
        Material clothDeepStitch(){return clothStitchCache?clothStitchCache:(clothStitchCache=MgfLook.Lit(MgfLook.Hex("#8C2F49"),.1f,0));}
        Material geckoMatCache;
        Material GeckoMat(){return geckoMatCache?geckoMatCache:(geckoMatCache=MgfLook.Alpha(Color.white,geckoTex));}

        void BuildGame()
        {
            MeshObj("BaseCushion",cliffPillow,clothDeep,gameRoot,new Vector3(-4.5f,-LayerH*1.2f,4.5f),new Vector3(1,1.6f,1));
            for(int i=0;i<tower.Length;i++)
            {
                var g=MeshObj("Floor"+i,pillows[8],i%2==0?cloth:clothDim,gameRoot,Vector3.zero,Vector3.one);
                tower[i]=g.transform;towerMesh[i]=g.GetComponent<MeshFilter>();
                towerBadge[i]=MgfLook.Prim(PrimitiveType.Cylinder,"FirstButton",new Vector3(.34f,.46f,-.34f),new Vector3(.34f,.035f,.34f),lime,tower[i],false).transform;
                g.SetActive(false);
            }
            for(int i=0;i<scraps.Length;i++)
            {
                scraps[i]=MeshObj("TornScrap"+i,tornMesh,tornMat,gameRoot,Vector3.zero,new Vector3(1.05f,.28f,.8f)).transform;
                scraps[i].gameObject.SetActive(false);
            }
            rig=new GameObject("MeasureRig").transform;rig.SetParent(gameRoot,false);
            BuildFrame();
            var a=MeshObj("ActiveCushion",pillows[1],clothHot,rig,new Vector3(-4,0,4),new Vector3(1,.6f,1));
            active=a.transform;activeMesh=a.GetComponent<MeshFilter>();a.SetActive(false);
            tear=new GameObject("LooseSeam").transform;tear.SetParent(rig,false);
            tearLine=MgfLook.Block("SeamGap",Vector3.zero,new Vector3(.12f,.05f,1f),.02f,ink,tear).transform;
            for(int i=0;i<4;i++){stuffing[i]=MgfLook.Prim(PrimitiveType.Sphere,"Stuffing",Vector3.zero,Vector3.one*(.34f+i*.07f),ivory,tear,false).transform;}
            tear.gameObject.SetActive(false);
            var z=new GameObject("ZipperStitch");z.transform.SetParent(rig,false);
            zip=z.AddComponent<LineRenderer>();zip.sharedMaterial=stitchIvory;zip.startColor=zip.endColor=Lime;zip.startWidth=zip.endWidth=.13f;zip.useWorldSpace=false;zip.numCapVertices=2;zip.enabled=false;zip.positionCount=5;
            gecko=new GameObject("CottonGecko").transform;gecko.SetParent(gameRoot,false);
            geckoMesh=MeshObj("Sprite",geckoStand,GeckoMat(),gecko,Vector3.zero,Vector3.one*GeckoScale,false).GetComponent<MeshFilter>();
            geckoShadow=MeshObj("Shadow",TiledQuad(2.0f,1.0f,1),shadowMat,gecko,new Vector3(0,.02f,0),Vector3.one,false).transform;
            geckoShadow.SetParent(gecko,true);
        }

        void BuildFrame()
        {
            // 8×8 눈금 틀: 뒤·왼쪽 줄자 테이프와 1칸 간격 격자. 수치는 카메라와 무관하게 틀 위에서 읽힌다.
            var frame=new GameObject("Frame").transform;frame.SetParent(rig,false);frame.localPosition=new Vector3(0,.58f,0);
            MgfLook.Block("BackTape",new Vector3(0,0,4.42f),new Vector3(8.9f,.05f,.78f),.02f,ivory,frame);
            MgfLook.Block("LeftTape",new Vector3(-4.42f,0,0),new Vector3(.78f,.05f,8.9f),.02f,ivory,frame);
            var mb=new List<CombineInstance>();
            var line=TiledQuad(1,1,1);
            for(int i=0;i<=8;i++)
            {
                float w=i%4==0?.075f:.04f;
                mb.Add(new CombineInstance{mesh=line,transform=Matrix4x4.TRS(new Vector3(-4+i,0,0),Quaternion.identity,new Vector3(w,1,8))});
                mb.Add(new CombineInstance{mesh=line,transform=Matrix4x4.TRS(new Vector3(0,0,4-i),Quaternion.identity,new Vector3(8,1,w))});
            }
            var grid=new Mesh{name="Grid"};grid.CombineMeshes(mb.ToArray(),true,true);
            MeshObj("Grid",grid,gridMat,frame,new Vector3(0,.012f,0),Vector3.one,false);
            for(int i=1;i<=8;i++)
            {
                var a=MgfText.World(i.ToString(),new Vector3(-4+i,.05f,4.42f),6.6f,Ink,frame);a.fontStyle=FontStyles.Bold;a.transform.localRotation=Quaternion.Euler(90,0,0);
                MgfLook.Block("Tick",new Vector3(-4+i,.04f,4.08f),new Vector3(.07f,.02f,.1f),.005f,ink,frame);
                var b=MgfText.World(i.ToString(),new Vector3(-4.42f,.05f,4-i),6.6f,Ink,frame);b.fontStyle=FontStyles.Bold;b.transform.localRotation=Quaternion.Euler(90,0,0);
                MgfLook.Block("Tick",new Vector3(-4.08f,.04f,4-i),new Vector3(.1f,.02f,.07f),.005f,ink,frame);
            }
        }

        void BuildFluff()
        {
            var r=new System.Random(7);var dot=TiledQuad(1,1,1);
            var root=new GameObject("Fluff").transform;
            for(int i=0;i<fluffs.Length;i++)
            {
                fluffs[i]=MeshObj("Fluff",dot,fluffMat,root,Vector3.zero,Vector3.one*(.12f+(float)r.NextDouble()*.20f),false).transform;
                fluffSeed[i]=new Vector3((float)r.NextDouble()*22-11,(float)r.NextDouble()*10,(float)r.NextDouble()*12-7);
            }
        }

        // ── 상태 전환
        public void ShowTitle()
        {
            isTitle=true;isEnd=false;openClock=-1;titleRoot.gameObject.SetActive(true);gameRoot.gameObject.SetActive(false);
            for(int i=0;i<4;i++)titleLayers[i].localPosition=new Vector3(titleLayers[i].localPosition.x*0+((i%2==0)?-.2f:.2f),i*1.20f,0);
            camSnap=true;
        }

        public void OpenTitle(){openClock=0;}

        public void ShowGame()
        {
            if(isTitle)camSnap=false;
            isTitle=false;isEnd=false;titleRoot.gameObject.SetActive(false);gameRoot.gameObject.SetActive(true);
        }

        public void ResetTower()
        {
            for(int i=0;i<tower.Length;i++)tower[i].gameObject.SetActive(false);
            for(int i=0;i<scraps.Length;i++)scraps[i].gameObject.SetActive(false);
            scrapCount=0;solvedFloors=0;rigY=rigTarget=0;rig.localPosition=Vector3.zero;
        }

        public void LoadCushion(bool practice)
        {
            rig.gameObject.SetActive(true);SetSide(0);puffed=false;revealOk=false;tear.gameObject.SetActive(false);zip.enabled=false;
            geckoHome=new Vector3(2.7f,rigTarget+.62f,5.0f);
            gecko.localPosition=geckoHome;geckoMesh.sharedMesh=geckoStand;
        }

        public void SetSide(int s)
        {
            side=s;
            if(s<=0){active.gameObject.SetActive(false);zip.enabled=false;return;}
            active.gameObject.SetActive(true);activeMesh.sharedMesh=pillows[s];active.localScale=new Vector3(1,.34f,1);
            const float y=.26f;
            zip.sharedMaterial=stitchIvory;zip.enabled=true;
            zip.SetPosition(0,new Vector3(-4,y,4));zip.SetPosition(1,new Vector3(-4+s,y,4));zip.SetPosition(2,new Vector3(-4+s,y,4-s));
            zip.SetPosition(3,new Vector3(-4,y,4-s));zip.SetPosition(4,new Vector3(-4,y,4));
        }

        public void Nudge(){nudge=1f;}

        public void BeginReveal(int s,bool ok,int areaGap)
        {
            revealOk=ok;puffed=false;
            if(ok)
            {
                zip.sharedMaterial=stitchMat;zip.enabled=s>0;
                float y=.26f;
                zip.SetPosition(0,new Vector3(-4,y,4));
                for(int i=1;i<5;i++)zip.SetPosition(i,new Vector3(-4,y,4));
            }
            else
            {
                shake=.32f;
                // 찢어진 솔기 길이가 넓이 차이를 남긴다(최대 한 변).
                tearLen=Mathf.Clamp(.8f+areaGap*.13f,.8f,Mathf.Max(1.2f,s));
                if(s>0)
                {
                    tear.gameObject.SetActive(true);
                    zip.enabled=false;
                    tear.localPosition=new Vector3(-4+s,.2f,4-s*.5f);
                    tearLine.localScale=new Vector3(1,1,tearLen);
                    for(int i=0;i<stuffing.Length;i++)stuffing[i].localPosition=new Vector3(.05f+(i%2)*.14f,.12f,(i/3f-.5f)*tearLen*.8f);
                    Vector3 w=rig.TransformPoint(new Vector3(-4+s,.3f,4-s*.5f));
                    MgfFx.Burst(w,Ivory,16,3.2f,.26f);
                    gecko.localPosition=rig.TransformPoint(new Vector3(-4+s+.2f,.1f,4-s*.6f));
                }
            }
        }

        public void KeepTear()
        {
            int i=scrapCount%scraps.Length;scrapCount++;
            var t=scraps[i];t.gameObject.SetActive(true);
            t.localPosition=new Vector3(4.9f-i*.15f,rigTarget-.5f-i*.28f,-1.0f-i*2.2f);
            t.localRotation=Quaternion.Euler(0,0,-58-i*8);
        }

        public void LockLayer(int index,int s,bool first)
        {
            if(index<0||index>=tower.Length)return;
            var t=tower[index];t.gameObject.SetActive(true);towerMesh[index].sharedMesh=pillows[s];
            t.localPosition=new Vector3(-4,index*LayerH,4);t.localScale=Vector3.one;
            towerBadge[index].gameObject.SetActive(first);
            active.gameObject.SetActive(false);zip.enabled=false;tear.gameObject.SetActive(false);
            solvedFloors=index+1;rigTarget=solvedFloors*LayerH;
        }

        public void ShowEnd(bool clear){isEnd=true;endClear=clear;active.gameObject.SetActive(false);tear.gameObject.SetActive(false);zip.enabled=false;rig.gameObject.SetActive(false);
            gecko.localPosition=new Vector3(-1.6f,rigTarget+.15f,1.2f);geckoMesh.sharedMesh=clear?geckoBelly:geckoStand;}

        // ── 입력 기하
        float PlaneHeight=>rigY+.58f;
        public bool InFrame(bool cameraPointer)
        {
            if(!MgfPointer.OnPlane(Cam,PlaneHeight,out var p))return false;
            return p.x>-5.6f&&p.x<5.8f&&p.z>-6.2f&&p.z<5.8f;
        }

        public int SideFromPointer()
        {
            if(!MgfPointer.OnPlane(Cam,PlaneHeight,out var p))return side;
            float diag=((p.x+4f)+(4f-p.z))*.5f;
            return Mathf.Clamp(Mathf.RoundToInt(diag),0,8);
        }

        public Vector3 CornerWorld(int s)=>new Vector3(-4+s,PlaneHeight,4-s);
        public Vector3 CurrentCornerWorld()=>CornerWorld(side);

        public void CornerPermille(int[] xs,int[] ys)
        {
            if(!Cam)return;
            for(int i=0;i<=8;i++)
            {
                var sp=Cam.WorldToScreenPoint(CornerWorld(i));
                xs[i]=Mathf.RoundToInt(sp.x/Mathf.Max(1,Screen.width)*1000f);
                ys[i]=Mathf.RoundToInt((1f-sp.y/Mathf.Max(1,Screen.height))*1000f);
            }
        }

        // ── 매 프레임
        public void Tick(float dt,PoksinhanKkokdaegiGame.Flow flow,float revealClock,bool ok)
        {
            float t=Time.unscaledTime;
            UpdateCamera(dt);
            for(int i=0;i<fluffs.Length;i++)
            {
                var s=fluffSeed[i];float h=Mathf.Repeat(s.y+t*(.16f+i*.012f),10f);
                fluffs[i].position=new Vector3(s.x+Mathf.Sin(t*.5f+i)*.5f,rigY-1.4f+h,s.z);
                fluffs[i].rotation=Cam.transform.rotation*Quaternion.Euler(-90,0,0);
            }
            if(isTitle){TickTitle(dt,t);return;}
            rigY=Mathf.MoveTowards(rigY,rigTarget,dt*1.6f);rig.localPosition=new Vector3(0,rigY,0);
            TickGecko(dt,t,flow,revealClock);
            if(flow==PoksinhanKkokdaegiGame.Flow.Reveal&&revealOk)TickZip(revealClock);
            if(landClock<3f)
            {
                // 착지 진동은 방석이 넓을수록(표준편차가 클수록) 오래 간다.
                landClock+=dt;float dur=.18f+landAmp*.09f;float u=Mathf.Clamp01(landClock/dur);
                float y=1f+Mathf.Sin(u*Mathf.PI*5f)*(1f-u)*.22f;
                if(active.gameObject.activeSelf)active.localScale=new Vector3(1,y,1);
            }
            if(nudge>0)nudge=Mathf.MoveTowards(nudge,0,dt*3f);
        }

        void TickTitle(float dt,float t)
        {
            for(int i=0;i<4;i++)
            {
                var c=titleLayers[i];
                float bx=(i%2==0?-.2f:.2f);
                float open=openClock>=0?openClock/.42f:0;
                c.localPosition=new Vector3(bx+(i%2==0?-1:1)*open*open*16f,i*1.20f+Mathf.Sin(t*1.3f+i*.9f)*.035f,0);
            }
            if(openClock>=0)openClock+=dt;
            for(int i=0;i<titleSway.Count;i++)titleSway[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*1.6f)*2.4f);
            float walk=Mathf.PingPong(t*.55f,1f);
            bool right=Mathf.Repeat(t*.55f,2f)<1f;
            titleGecko.localPosition=new Vector3(Mathf.Lerp(-2.9f,2.6f,walk),4.82f+Mathf.Abs(Mathf.Sin(t*5.5f))*.08f,-1.9f);
            titleGecko.rotation=Cam.transform.rotation;
            float breathe=1f+Mathf.Sin(t*3f)*.018f;
            titleGeckoMesh.transform.localScale=new Vector3((right?-1:1)*1.2f,1.2f*breathe,1.2f);
        }

        void TickGecko(float dt,float t,PoksinhanKkokdaegiGame.Flow flow,float revealClock)
        {
            gecko.rotation=Cam.transform.rotation;
            geckoShadow.rotation=Quaternion.identity;
            bool reveal=flow==PoksinhanKkokdaegiGame.Flow.Reveal;
            var sprite=geckoMesh.transform;
            if(reveal&&!revealOk)
            {
                // 오답: 찢어진 솔기를 붙들고 달달 떤다.
                geckoMesh.sharedMesh=geckoBelly;sprite.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*22f)*7f-12f);
                sprite.localScale=Vector3.one*GeckoScale;return;
            }
            sprite.localRotation=Quaternion.identity;
            if(reveal&&revealOk)return;   // TickZip 이 움직인다
            if(isEnd)
            {
                float k=endClear?1f+Mathf.Abs(Mathf.Sin(t*5f))*.08f:1f;
                sprite.localScale=new Vector3(.8f,.8f*k,.8f);return;
            }
            float idle=1f+Mathf.Sin(t*2.6f)*.03f;
            var target=new Vector3(geckoHome.x,rigTarget+.62f,geckoHome.z);
            gecko.localPosition=Vector3.MoveTowards(gecko.localPosition,target,dt*9f);
            geckoHop=Mathf.Abs(Mathf.Sin(t*.9f))>.985f?.08f:Mathf.MoveTowards(geckoHop,0,dt);
            sprite.localScale=new Vector3(GeckoScale,GeckoScale*idle,GeckoScale);
            sprite.localPosition=new Vector3(0,geckoHop,0);
        }

        void TickZip(float c)
        {
            const float zipEnd=.85f,puffEnd=1.25f;
            int s=Mathf.Max(side,1);
            float y=.26f;
            var a=new Vector3(-4,y,4);var b=new Vector3(-4+s,y,4);var d=new Vector3(-4+s,y,4-s);var e=new Vector3(-4,y,4-s);
            float u=Mathf.Clamp01(c/zipEnd)*4f;
            var p=u<1?Vector3.Lerp(a,b,u):u<2?Vector3.Lerp(b,d,u-1):u<3?Vector3.Lerp(d,e,u-2):Vector3.Lerp(e,a,u-3);
            zip.SetPosition(1,u<1?p:b);zip.SetPosition(2,u<1?b:u<2?p:d);zip.SetPosition(3,u<2?d:u<3?p:e);zip.SetPosition(4,u<3?e:p);
            if(u<1){zip.SetPosition(2,p);zip.SetPosition(3,p);zip.SetPosition(4,p);}
            else if(u<2){zip.SetPosition(3,p);zip.SetPosition(4,p);}
            else if(u<3){zip.SetPosition(4,p);}
            var sprite=geckoMesh.transform;
            if(c<zipEnd)
            {
                // 도마뱀이 네 변을 달리며 박는다 — 한 변의 길이를 몸으로 네 번 잰다.
                gecko.localPosition=rig.TransformPoint(p)-Vector3.zero;
                gecko.localPosition=new Vector3(gecko.localPosition.x,gecko.localPosition.y+.04f,gecko.localPosition.z);
                bool goesRight=u<1||(u>=2&&u<3&&false);
                sprite.localScale=new Vector3((goesRight?-1:1)*.6f,.6f*(1f+Mathf.Abs(Mathf.Sin(c*30f))*.08f),.6f);
                geckoMesh.sharedMesh=geckoStand;
            }
            else
            {
                // 박음질이 끝나면 방석이 부풀고 도마뱀이 가운데로 배부터 떨어진다.
                if(!puffed){puffed=true;MgfSfx.Play("pop",.5f);MgfFx.Glow(rig.TransformPoint(new Vector3(-4+s*.5f,.5f,4-s*.5f)),Lime,10);}
                float k=Mathf.Clamp01((c-zipEnd)/(puffEnd-zipEnd));
                float over=k<.7f?Mathf.Lerp(.34f,1.18f,k/.7f):Mathf.Lerp(1.18f,1f,(k-.7f)/.3f);
                active.localScale=new Vector3(1,over,1);
                Vector3 centre=rig.TransformPoint(new Vector3(-4+s*.5f,.5f,4-s*.5f));
                Vector3 from=rig.TransformPoint(new Vector3(-4,.26f,4));
                float h=Mathf.Sin(k*Mathf.PI)*1.6f;
                gecko.localPosition=Vector3.Lerp(from,centre,k)+Vector3.up*h;
                geckoMesh.sharedMesh=k<.85f?geckoStand:geckoBelly;
                sprite.localScale=new Vector3(GeckoScale,GeckoScale*(k<.85f?1f:.72f),GeckoScale);
                if(k>=1f&&landClock>=3f){landClock=0;landAmp=s;}
            }
        }

        void UpdateCamera(float dt)
        {
            float a=Cam.aspect;bool land=MgfBridge.IsLandLayout;
            if(isTitle)
            {
                goal=new CamPose{focus=new Vector3(0,2.3f,-2f),pitch=15,yaw=-7,halfW=6.2f,halfH=5.6f,sx=0,sy=0};
                if(!land){goal.halfH=8f;goal.sy=.02f;}
            }
            else if(isEnd)
            {
                float top=rigTarget;
                goal=land?new CamPose{focus=new Vector3(0,top*.5f,0),pitch=38,yaw=-16,halfW=11f,halfH=7.2f,sx=.17f,sy=0}
                         :new CamPose{focus=new Vector3(0,top*.5f,0),pitch=38,yaw=-16,halfW=7.0f,halfH=12f,sx=0,sy=.21f};
            }
            else
            {
                goal=land?new CamPose{focus=new Vector3(0,rigY+.3f,.3f),pitch=56,yaw=-10,halfW=9.5f,halfH=6.9f,sx=.16f,sy=.045f}
                         :new CamPose{focus=new Vector3(0,rigY+.3f,.3f),pitch=56,yaw=-10,halfW=5.75f,halfH=8.2f,sx=0,sy=a>.6f?-.115f:.012f};
            }
            float lerp=camSnap?1f:1f-Mathf.Exp(-dt*7f);camSnap=false;
            cur.focus=Vector3.Lerp(cur.focus,goal.focus,lerp);
            cur.pitch=Mathf.Lerp(cur.pitch,goal.pitch,lerp);cur.yaw=Mathf.Lerp(cur.yaw,goal.yaw,lerp);
            cur.halfW=Mathf.Lerp(cur.halfW,goal.halfW,lerp);cur.halfH=Mathf.Lerp(cur.halfH,goal.halfH,lerp);
            cur.sx=Mathf.Lerp(cur.sx,goal.sx,lerp);cur.sy=Mathf.Lerp(cur.sy,goal.sy,lerp);
            float tan=Mathf.Tan(Cam.fieldOfView*.5f*Mathf.Deg2Rad);
            float d=Mathf.Max(cur.halfW/(tan*a),cur.halfH/tan);
            var rot=Quaternion.Euler(cur.pitch,cur.yaw,0);
            float wh=2f*d*tan,ww=wh*a;
            var pos=cur.focus-rot*Vector3.forward*d-rot*Vector3.right*(cur.sx*ww)-rot*Vector3.up*(cur.sy*wh);
            if(shake>0){shake=Mathf.MoveTowards(shake,0,dt);pos+=rot*new Vector3(Mathf.Sin(Time.unscaledTime*61f),Mathf.Sin(Time.unscaledTime*47f),0)*shake*.45f;}
            Cam.transform.SetPositionAndRotation(pos,rot);
        }
    }
}
