using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Mgf;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        // Built once. All decorative randomness is independent of combat and pack RNG.
        Light environmentSun;
        readonly Transform[] townBuildings=new Transform[2];
        readonly GameObject[,] townStages=new GameObject[2,3];
        readonly Renderer[,][] townRenderers=new Renderer[2,3][];
        readonly GameObject[,] fences=new GameObject[3,2];
        readonly Transform[] fenceHealth=new Transform[3];
        readonly GameObject[] fenceHealthRoots=new GameObject[3];
        readonly int[] fenceDamageStage={-1,-1,-1};
        readonly Vector3[] fencePositions={new Vector3(-5.14f,.25f,.55f),new Vector3(-4.2f,.25f,.55f),new Vector3(-3.26f,.25f,.55f)};
        MaterialPropertyBlock constructionBlock;
        Transform environmentRoot;
        int townStage,environmentCorrect,environmentHp=100;
        float constructionTime,nightTime,fireTime;
        Transform kingContactShadow;
        readonly Color daylight=new Color(1f,243f/255,222f/255);
        readonly Color moonlight=new Color(150f/255,167f/255,238f/255);
        Mesh riverFoam;
        Material foamMaterial;
        readonly Matrix4x4[] foamMatrices=new Matrix4x4[12];
        readonly Vector3[] foamOrigins=new Vector3[12];
        static readonly Vector2[] PlateauOutline={
            new Vector2(-3.05f,-4.80f),new Vector2(-1.9f,-5.45f),new Vector2(.10f,-5.60f),new Vector2(1.75f,-4.85f),
            new Vector2(2.35f,-3.0f),new Vector2(2.70f,.10f),new Vector2(2.78f,3.2f),new Vector2(2.58f,5.85f),
            new Vector2(.70f,7.15f),new Vector2(-1.75f,6.85f),new Vector2(-3.30f,5.3f),new Vector2(-3.40f,1.0f)};

        void BuildEnvironment()
        {
            environmentRoot=new GameObject("Living settlement").transform;
            constructionBlock=new MaterialPropertyBlock();
            BuildTownStages(0,"barracks",new Vector3(-4.18f,.25f,-.30f),1.0f);
            BuildTownStages(1,"castle_gate",new Vector3(-4.4f,.25f,2.0f),1.05f);
            BuildFortifications();
            var detail=new DecorationMesh();
            BuildMeadowDressing(detail);
            BuildEnemyGround(detail);
            BuildRiver(detail);
            CreateDecoration("Meadow accents and river banks",detail,worldMat,false);
            PlaceSettlementProps();
            DressMesaToes();
            DressWorkshopVignettes();
        }
        static Color Hex(string value)=>MgfLook.Hex(value);

        void BuildTownStages(int at,string id,Vector3 p,float scale)
        {
            var root=new GameObject(id+" upgrade stages").transform;root.position=p;root.localScale=Vector3.one*scale;
            root.rotation=Quaternion.Euler(0,180,0);root.SetParent(environmentRoot,true);townBuildings[at]=root;
            for(int level=0;level<3;level++){
                var go=SpawnModel(id+"_l"+(level+1),p,1);go.transform.SetParent(root,false);go.transform.localPosition=Vector3.zero;
                go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;go.SetActive(level==0);
                townStages[at,level]=go;townRenderers[at,level]=go.GetComponentsInChildren<Renderer>(true);
            }
        }
        void BuildFortifications()
        {
            Material hp= MgfLook.Unlit(Hex("#63dc85")),back=MgfLook.Unlit(Hex("#173e35"));
            for(int i=0;i<3;i++){
                for(int d=0;d<2;d++){
                    var go=SpawnModel(d==0?"palisade":"palisade_damaged",fencePositions[i],.73f);
                    go.transform.SetParent(environmentRoot,true);go.SetActive(d==0);fences[i,d]=go;
                }
                var root=new GameObject("Gate segment health "+i);root.transform.SetParent(environmentRoot);
                root.transform.position=fencePositions[i]+new Vector3(0,1.05f,0);root.transform.rotation=cam.transform.rotation;
                MgfLook.Prim(PrimitiveType.Cube,"Health back",Vector3.zero,new Vector3(.81f,.105f,.012f),back,root.transform,false);
                fenceHealth[i]=MgfLook.Prim(PrimitiveType.Cube,"Health remaining",new Vector3(0,0,-.012f),new Vector3(.75f,.065f,.012f),hp,root.transform,false).transform;
                root.SetActive(false);fenceHealthRoots[i]=root;
            }
        }
        void BuildEnemyGround(DecorationMesh detail)
        {
            // Two independent red-roofed entrances share a warm, enemy-controlled forecourt.
            detail.Box(new Vector3(4.55f,.239f,9.15f),new Vector3(5.5f,.036f,4.8f),Hex("#d58b88"));
            detail.Disc(new Vector3(4.55f,.260f,9.1f),2.45f,.86f,Hex("#c87c7c"),14);
            for(int i=0;i<7;i++){
                float z=7.4f+i*.54f;
                detail.Box(new Vector3(2.20f,.272f,z),new Vector3(.12f,.04f,.34f),Hex("#dfaba0"));
                detail.Box(new Vector3(6.58f,.272f,z),new Vector3(.12f,.04f,.34f),Hex("#dfaba0"));
            }
            SpawnModel("enemy_watchtower",new Vector3(1.96f,.25f,10.35f),.76f).transform.Rotate(0,180,0);
            SpawnModel("enemy_watchtower",new Vector3(6.78f,.25f,10.35f),.76f).transform.Rotate(0,180,0);
        }
        void BuildRiver(DecorationMesh detail)
        {
            Vector3[] nodes={new Vector3(-22,-1.44f,-12),new Vector3(-12,-1.44f,-10.6f),new Vector3(-6,-1.44f,-9.7f),new Vector3(.2f,-1.44f,-7.1f),new Vector3(6,-1.44f,-10),new Vector3(13,-1.44f,-10.5f),new Vector3(25,-1.44f,-9.4f)};
            // Continuous banks: smooth the old ruler-straight V without moving
            // the bridge or any gameplay path. Generated once, then merged.
            var river=new Vector3[(nodes.Length-1)*10+1];
            for(int i=0;i<river.Length;i++){
                float u=i/10f;int k=Mathf.Min(nodes.Length-2,(int)u);float t=u-k;
                Vector3 a=nodes[Mathf.Max(0,k-1)],b=nodes[k],c=nodes[k+1],d=nodes[Mathf.Min(nodes.Length-1,k+2)];
                river[i]=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
            }
            detail.CurvedRibbon(river,3.02f,-.013f,Hex("#9ba48d"));
            detail.CurvedRibbon(river,2.55f,0,Hex("#358b9f"));
            detail.CurvedRibbon(river,1.78f,.004f,Hex("#559fb0"));
            // Deck follows the existing U-bend, so army movement and collisions are unchanged.
            for(int i=0;i<15;i++){
                float x=-1.35f+i*.193f;
                detail.Box(new Vector3(x,.252f,-7.08f),new Vector3(.178f,.14f,1.96f),Hex(i%3==0?"#ae8153":"#be9465"));
            }
            for(int side=-1;side<=1;side+=2){
                float z=-7.08f+side*1.03f;
                detail.Box(new Vector3(.01f,.33f,z),new Vector3(2.99f,.22f,.12f),Hex("#7e553b"));
                for(int i=0;i<4;i++){
                    float x=-1.39f+i*.93f;
                    detail.Box(new Vector3(x,-.39f,z),new Vector3(.14f,1.84f,.14f),Hex("#805b43"));
                    detail.Box(new Vector3(x,.41f,z),new Vector3(.22f,.16f,.22f),Hex("#c7a578"));
                }
            }
            var foam=new DecorationMesh();foam.Ribbon(new Vector3(-.37f,0,0),new Vector3(.37f,0,0),.025f,Hex("#8ac7d2"));
            riverFoam=foam.Build("Creek foam streak");foamMaterial=new Material(worldMat);foamMaterial.SetFloat("_Unlit",1);foamMaterial.enableInstancing=true;
            for(int i=0;i<12;i++){
                int k=i/2;
                foamOrigins[i]=Vector3.Lerp(nodes[k],nodes[k+1],i%2==0?.25f:.66f)+new Vector3(0,.013f,(i%3-1)*.35f);
            }
        }
        void BuildSceneryContactShadows()
        {
            // Contact AO is deliberately teal rather than black. One merged feathered
            // decal grounds the entire static settlement; actor shadow is reusable.
            var ao=new DecorationMesh();var foundations=new DecorationMesh();
            // Alpha uses raw linear RGB. Decode the authored sRGB colours once so
            // the teal contact shadow does not become a pale green wash.
            Color contact=new Color(.035f,.18f,.135f,.35f).linear;
            Color earth=new Color(200f/255,181f/255,156f/255,.65f).linear;
            for(int i=0;i<sceneryRoot.childCount;i++){
                var t=sceneryRoot.GetChild(i);if(t.name.StartsWith("terrain"))continue;
                string name=t.name;
                bool tree=name.StartsWith("tree"),rock=name.StartsWith("rock");
                bool gate=name.Contains("gate"),tower=name.Contains("watchtower");
                bool prop=name.StartsWith("barrel")||name.StartsWith("crate")||name.StartsWith("well")||name.StartsWith("bell")||name.StartsWith("sack")||name.StartsWith("logpile")||name.StartsWith("stump")||name.StartsWith("torch");
                if(!tree&&!rock&&!gate&&!tower&&!prop&&!name.StartsWith("palisade"))continue;
                var p=t.position;float s=t.localScale.x;
                float r=tree?.74f:gate?1.48f:tower?1.02f:name.Contains("large")?1.68f:name.Contains("medium")?.88f:.47f;
                ao.SoftDisc(p+Vector3.up*.018f,r*s,.78f,contact);
                if(gate||tower||name.StartsWith("well"))foundations.SoftDisc(p+Vector3.up*.008f,r*s*1.34f,.89f,earth);
            }
            for(int i=0;i<2;i++){
                Vector3 p=townBuildings[i].position;float r=i==0?1.12f:1.47f;
                foundations.SoftDisc(p+Vector3.up*.009f,r*1.31f,.82f,earth);
                ao.SoftDisc(p+Vector3.up*.019f,r,.78f,contact);
            }
            // These two friendly towers are created later by HyeopgokBattle,
            // outside sceneryRoot. Their fixed ground anchors also support the
            // unbuilt blueprint, so the shared static contact mesh can own them.
            for(int i=0;i<2;i++){
                Vector3 p=new Vector3(1.75f,1.2f,i==0?-3.5f:4.7f);
                foundations.SoftDisc(p+Vector3.up*.010f,1.09f,.91f,earth);
                ao.SoftDisc(p+Vector3.up*.020f,.84f,.83f,contact);
            }
            for(int i=0;i<fencePositions.Length;i++)ao.SoftDisc(fencePositions[i]+Vector3.up*.021f,.66f,.46f,contact);
            Material contactMaterial=MgfLook.Alpha(Color.white);
            CreateDecoration("Feathered building earth foundations",foundations,contactMaterial,false);
            CreateDecoration("Teal settlement contact shadows",ao,contactMaterial,false);
            var kingShadow=new DecorationMesh();kingShadow.SoftDisc(Vector3.zero,.44f,.77f,contact);
            kingContactShadow=CreateDecoration("King soft contact shadow",kingShadow,contactMaterial,false).transform;
            kingContactShadow.SetParent(null,true);kingContactShadow.position=new Vector3(king.position.x,1.213f,king.position.z);
        }
        void LateUpdate(){
            long before=HyeopgokArtProbe.Begin();
            try{LateUpdateArtFrame();}finally{HyeopgokArtProbe.End(2,before);}
        }
        void LateUpdateArtFrame()
        {
            if(environmentRoot==null)return;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            if(kingContactShadow&&king)kingContactShadow.position=new Vector3(king.position.x,1.213f,king.position.z);
            if(playStarted&&Rules.Correct!=environmentCorrect){
                bool reset=Rules.Correct<environmentCorrect;
                int stage=Mathf.Clamp(Rules.Correct/3,0,2);
                if(stage!=townStage){
                    townStage=stage;constructionTime=reset?0:.82f;
                    for(int b=0;b<2;b++)for(int l=0;l<3;l++)townStages[b,l].SetActive(l==townStage);
                    if(!reset)nightTime=1.35f;
                }
                if(reset){
                    nightTime=0;Shader.SetGlobalColor("_HyeopgokNightTint",Color.white);
                    environmentSun.color=daylight;RenderSettings.ambientIntensity=.78f;
                }
                environmentCorrect=Rules.Correct;
            }
            if(constructionTime>0){
                constructionTime=Mathf.Max(0,constructionTime-dt);float p=1-constructionTime/.82f;
                constructionBlock.SetFloat("_Flash",Mathf.Clamp01(1-p*1.55f));
                constructionBlock.SetFloat("_Unlit",Mathf.Clamp01(1-p*1.4f));
                for(int b=0;b<2;b++){
                    townStages[b,townStage].transform.localScale=new Vector3(1,Mathf.Lerp(.06f,1,Mathf.SmoothStep(0,1,p)),1);
                    var rs=townRenderers[b,townStage];for(int r=0;r<rs.Length;r++)rs[r].SetPropertyBlock(constructionBlock);
                }
            }
            if(playStarted&&Rules.Hp!=environmentHp){
                environmentHp=Rules.Hp;
                for(int i=0;i<3;i++){
                    float health=Mathf.Clamp01((environmentHp-i*33.333f)/33.333f);
                    int damage=health<=0?2:health<.68f?1:0;
                    if(damage!=fenceDamageStage[i]){
                        fences[i,0].SetActive(damage==0);fences[i,1].SetActive(damage>0);
                        fences[i,1].transform.localScale=Vector3.one*(damage==2?.44f:.73f);
                        fenceDamageStage[i]=damage;
                    }
                    fenceHealthRoots[i].SetActive(health<1&&health>0);
                    fenceHealth[i].localScale=new Vector3(.75f*health,.065f,.012f);
                    fenceHealth[i].localPosition=new Vector3(-.375f*(1-health),0,-.012f);
                }
            }
            fireTime-=dt;
            if(fireTime<=0&&playStarted&&environmentHp<67&&battle){
                fireTime=.16f;
                for(int i=0;i<3;i++)if(fenceDamageStage[i]>0)battle.EmitFire(fencePositions[i]+Vector3.up*.4f,1);
                if(environmentHp<34)battle.EmitFire(townBuildings[1].position+Vector3.up*1.45f,1.2f);
            }
            if(nightTime>0){
                nightTime=Mathf.Max(0,nightTime-dt);
                float pulse=Mathf.Sin((1-nightTime/1.35f)*Mathf.PI)*.70f;
                Shader.SetGlobalColor("_HyeopgokNightTint",Color.Lerp(Color.white,new Color(.46f,.49f,.80f),pulse));
                environmentSun.color=Color.Lerp(daylight,moonlight,pulse);
                RenderSettings.ambientIntensity=Mathf.Lerp(.78f,.46f,pulse);
            }
            if(riverFoam&&foamMaterial){
                float time=Time.unscaledTime;
                for(int i=0;i<foamMatrices.Length;i++){
                    float t=Mathf.Repeat(time*.18f+i*.137f,1);
                    Vector3 pos=foamOrigins[i]+new Vector3(t*.56f,0,Mathf.Sin(time*.4f+i)*.055f);
                    foamMatrices[i]=Matrix4x4.TRS(pos,Quaternion.Euler(0,i<6?-10:7,0),new Vector3(.4f+Mathf.Sin(t*Mathf.PI)*.6f,1,1));
                }
                Graphics.DrawMeshInstanced(riverFoam,0,foamMaterial,foamMatrices,foamMatrices.Length,null,ShadowCastingMode.Off,false,0,cam,LightProbeUsage.Off);
            }
        }
        void CombineScenery()
        {
            // A single authored palette permits a real mesh merge. Static batching alone
            // may split the many small tree renderers into culling and shadow submissions.
            var solid=new List<CombineInstance>();var ground=new List<CombineInstance>();
            var renderers=sceneryRoot.GetComponentsInChildren<MeshRenderer>();
            foreach(var r in renderers){
                if(r.sharedMaterial!=worldMat)continue;
                var filter=r.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
                var combine=new CombineInstance{mesh=filter.sharedMesh,transform=filter.transform.localToWorldMatrix};
                if(r.shadowCastingMode==ShadowCastingMode.Off)ground.Add(combine);else solid.Add(combine);
                r.enabled=false;
            }
            MergeSceneryPart("Merged canyon buildings and forest",solid,true);
            MergeSceneryPart("Merged grass and creek details",ground,false);
        }
        void MergeSceneryPart(string name,List<CombineInstance> parts,bool casts)
        {
            if(parts.Count==0)return;
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(parts.ToArray(),true,true,false);
            var go=new GameObject(name);go.transform.SetParent(sceneryRoot,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=worldMat;
            renderer.shadowCastingMode=casts?ShadowCastingMode.On:ShadowCastingMode.Off;renderer.receiveShadows=true;
        }
        GameObject CreateDecoration(string name,DecorationMesh data,Material material,bool shadows)
        {
            var go=new GameObject(name);go.transform.SetParent(sceneryRoot,false);
            go.AddComponent<MeshFilter>().sharedMesh=data.Build(name);
            var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;
            r.receiveShadows=true;return go;
        }
        sealed class DecorationMesh
        {
            readonly List<Vector3> vertices=new List<Vector3>(12000);
            readonly List<int> triangles=new List<int>(18000);
            readonly List<Color> colors=new List<Color>(12000);
            public void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color,bool both=false){
                int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);colors.Add(color);colors.Add(color);colors.Add(color);
                triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
                if(both){triangles.Add(n+2);triangles.Add(n+1);triangles.Add(n);}
            }
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color){Triangle(a,b,c,color);Triangle(a,c,d,color);}
            public void Ribbon(Vector3 a,Vector3 b,float width,Color color){
                var d=b-a;var n=new Vector3(-d.z,0,d.x).normalized*(width*.5f);
                Quad(a-n,a+n,b+n,b-n,color);
            }
            public void CurvedRibbon(Vector3[] points,float width,float lift,Color color){
                Vector3 left=Vector3.zero,right=Vector3.zero;
                for(int i=0;i<points.Length;i++){
                    Vector3 along=points[Mathf.Min(points.Length-1,i+1)]-points[Mathf.Max(0,i-1)];
                    Vector3 side=new Vector3(-along.z,0,along.x).normalized*(width*.5f)*(1+Mathf.Sin(i*.31f)*.08f);
                    Vector3 p=points[i]+Vector3.up*lift;
                    if(i>0)Quad(left,right,p+side,p-side,color);
                    left=p-side;right=p+side;
                }
            }
            public void Disc(Vector3 p,float radius,float aspect,Color color,int segments){
                for(int i=0;i<segments;i++){
                    float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                    Triangle(p,p+new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius*aspect),p+new Vector3(Mathf.Sin(b)*radius,0,Mathf.Cos(b)*radius*aspect),color);
                }
            }
            public void RoundedPad(Vector3 p,float halfWidth,float halfDepth,float radius,Color color){
                Vector3 previous=p+new Vector3(-halfWidth+radius,0,halfDepth);
                for(int corner=0;corner<4;corner++){
                    Vector3 center=p+new Vector3(corner<2?halfWidth-radius:-halfWidth+radius,0,corner==0||corner==3?halfDepth-radius:-halfDepth+radius);
                    for(int k=0;k<=4;k++){
                        float angle=(corner*90+k*22.5f)*Mathf.Deg2Rad;
                        Vector3 next=center+new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius);
                        Triangle(p,previous,next,color);previous=next;
                    }
                }
                Triangle(p,previous,p+new Vector3(-halfWidth+radius,0,halfDepth),color);
            }
            public void SoftDisc(Vector3 p,float radius,float aspect,Color color){
                Color edge=color;edge.a=0;
                for(int i=0;i<16;i++){
                    float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;
                    Vector3 da=new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius*aspect),db=new Vector3(Mathf.Sin(b)*radius,0,Mathf.Cos(b)*radius*aspect);
                    // A broad .35-alpha core remains visible outside the model base;
                    // the outer band then fades continuously to transparent.
                    Triangle(p,p+da*.53f,p+db*.53f,color);
                    int n=vertices.Count;
                    vertices.Add(p+da*.53f);vertices.Add(p+da);vertices.Add(p+db);vertices.Add(p+db*.53f);
                    colors.Add(color);colors.Add(edge);colors.Add(edge);colors.Add(color);
                    triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
                }
            }
            public void Box(Vector3 p,Vector3 s,Color color){
                s*=.5f;Vector3 a=p+new Vector3(-s.x,-s.y,-s.z),b=p+new Vector3(s.x,-s.y,-s.z),c=p+new Vector3(s.x,s.y,-s.z),d=p+new Vector3(-s.x,s.y,-s.z);
                Vector3 e=p+new Vector3(-s.x,-s.y,s.z),f=p+new Vector3(s.x,-s.y,s.z),g=p+new Vector3(s.x,s.y,s.z),h=p+new Vector3(-s.x,s.y,s.z);
                Quad(a,d,c,b,color);Quad(f,g,h,e,color);Quad(e,h,d,a,color);Quad(b,c,g,f,color);Quad(d,h,g,c,color);Quad(e,a,b,f,color);
            }
            public Mesh Build(string name){
                var mesh=new Mesh{name=name};if(vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            }
        }
    }
}
