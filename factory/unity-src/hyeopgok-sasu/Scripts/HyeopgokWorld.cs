using UnityEngine;
using Mgf;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        Material worldMat;
        Material kingAiMaterial,crownAiMaterial;
        Transform sceneryRoot;
        Transform[] padRoots=new Transform[4];
        LineRenderer[] padFill=new LineRenderer[4];
        LineRenderer[] cracks=new LineRenderer[4];
        void BuildWorld(){
            MgfLook.Sky(MgfLook.Hex("#bfe6e0"),MgfLook.Hex("#75bca8"),MgfLook.Hex("#315f58"),.8f);
            Shader.SetGlobalColor("_HyeopgokNightTint",Color.white);
            var sun=MgfLook.Sun(new Vector3(50,-38,0),MgfLook.Hex("#fff3de"),1.03f,.62f);environmentSun=sun;
            sun.shadowBias=.025f;sun.shadowNormalBias=.085f;
            sun.shadows=LightShadows.Soft;sun.shadowCustomResolution=1024;sun.shadowStrength=.46f;
            QualitySettings.shadowCascades=0;QualitySettings.shadowDistance=52;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=MgfLook.Hex("#a9d8d3");RenderSettings.ambientEquatorColor=MgfLook.Hex("#579384");RenderSettings.ambientGroundColor=MgfLook.Hex("#244f4d");
            RenderSettings.ambientIntensity=.68f;
            RenderSettings.fog=true;RenderSettings.fogColor=MgfLook.Hex("#9bcfc6");RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=48;RenderSettings.fogEndDistance=82;
            Vector3 cameraFocus=new Vector3(.5f,0,2.4f);
            cam=MgfLook.Camera(cameraFocus+Quaternion.Euler(55,-8,0)*Vector3.back*46.5f,cameraFocus,32);cam.orthographic=false;cam.nearClipPlane=.2f;cam.farClipPlane=160;
            cam.transform.rotation=Quaternion.Euler(55,-8,0);
            cameraBase=cam.transform.position;cameraRot=cam.transform.rotation;
            worldMat=new Material(Resources.Load<Shader>("HyeopgokSasu/Shaders/Horde"));worldMat.SetColor("_Color",Color.white);worldMat.enableInstancing=true;
            sceneryRoot=new GameObject("Static scenery").transform;
            SpawnModel("terrain",Vector3.zero,1);
            BuildEnvironment();
            SpawnModel("enemy_gate",new Vector3(3.1f,.25f,9.0f),.93f).transform.Rotate(0,180,0);
            SpawnModel("enemy_gate",new Vector3(5.35f,.25f,9.0f),.93f).transform.Rotate(0,180,0);
            Vector3 kingPosition=HyeopgokRules.Pads[2]+Vector3.back*2;
            kingAiMaterial=HyeopgokAiAssets.CreateMaterial("king",Color.white);kingAiMaterial.SetFloat("_Rim",.24f);
            GameObject aiKing=HyeopgokAiAssets.InstantiateModel("king",kingPosition,Quaternion.Euler(0,180,0),1.25f,sceneryRoot,kingAiMaterial,true);
            king=(aiKing?aiKing:SpawnModel("king",kingPosition,1.25f)).transform;king.rotation=Quaternion.Euler(0,180,0);
            king.SetParent(null,true);
            if(aiKing){
                crownAiMaterial=HyeopgokAiAssets.CreateMaterial("king_crown",MgfLook.Hex("#F2B705").linear);
                crownAiMaterial.SetFloat("_Metallic",.72f);crownAiMaterial.SetFloat("_Rim",.16f);
                HyeopgokAiAssets.InstantiateModel("king_crown",king.position,king.rotation,1.25f,king,crownAiMaterial,true);
            }else{
                var heroMat=new Material(worldMat);heroMat.SetFloat("_Rim",.24f);
                foreach(var renderer in king.GetComponentsInChildren<Renderer>()){
                    renderer.sharedMaterial=heroMat;
                    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                    renderer.receiveShadows=true;
                }
            }
            BuildKingRing();
            // Forests are authored in asymmetrical groups, then merged once.
            var rng=new System.Random(20260926);
            PlantSandstoneRim();
            // Round 2b: the old near-black loose rocks west of the road are replaced by
            // the clustered sandstone block ridge baked into terrain.fbx.
            // Small asymmetric groves follow the plateau rim. Leave the four pads,
            // king approach, cannon sight lines and red/blue collision throat clear.
            var rim=new Vector4[]{
                new Vector4(-2.73f,1.2f,4.15f,.64f),new Vector4(-2.08f,1.2f,5.38f,.81f),
                new Vector4(-1.31f,1.2f,6.05f,.67f),new Vector4(-.43f,1.2f,6.39f,.73f),
                new Vector4(.48f,1.2f,6.38f,.51f),new Vector4(-2.91f,1.2f,-2.92f,.55f),
                new Vector4(-2.50f,1.2f,-4.05f,.64f),new Vector4(-1.47f,1.2f,-4.82f,.48f),
                new Vector4(5.73f,.48f,4.42f,.57f),new Vector4(5.70f,.48f,2.18f,.61f),
                new Vector4(5.23f,.48f,-2.25f,.57f),new Vector4(4.18f,.48f,-5.05f,.50f),
                new Vector4(3.05f,-1.52f,-10.45f,.60f),new Vector4(-3.10f,-1.52f,-10.55f,.58f)
            };
            for(int i=0;i<rim.Length;i++){
                var p=rim[i];var tree=SpawnModel(i%3==1?"tree_broadleaf":"tree",new Vector3(p.x,p.y,p.z),p.w);
                tree.transform.Rotate(0,rng.Next(360),0);
                if(i<8&&i%2==0)SpawnModel("rock_small",new Vector3(p.x+.3f,p.y,p.z-.4f),.34f);
            }
            BuildSceneryContactShadows();
            CombineScenery();

            // The alpha shader has no sRGB decode, unlike the authored-model shader.
            Material padStone=MgfLook.Alpha(new Color(14f/255,94f/255,68f/255,.7f).linear);
            for(int i=0;i<4;i++){
                var padStrokes=new DecorationMesh();
                var root=new GameObject("Answer pad "+(i+1));root.transform.position=HyeopgokRules.Pads[i];padRoots[i]=root.transform;
                var padSurface=new DecorationMesh();padSurface.RoundedPad(HyeopgokRules.Pads[i]+Vector3.up*.009f,.85f,.825f,.14f,Color.white);
                var padGround=CreateDecoration("Dark green pad "+i,padSurface,padStone,false);padGround.transform.SetParent(root.transform,true);
                // One shared mesh replaces 64 independent LineRenderer draw calls.
                for(int k=0;k<16;k++)
                    padStrokes.Ribbon(PadEdge(i,k/16f),PadEdge(i,(k+.67f)/16f),.085f,Color.white);
                padFill[i]=Line("Hold progress",MgfLook.Hex("#ffd55d"),.095f);padFill[i].positionCount=0;
                cracks[i]=Line("Broken answer stone",MgfLook.Hex("#082c30"),.075f);cracks[i].positionCount=5;
                Vector3 p=HyeopgokRules.Pads[i]+Vector3.up*.06f;
                cracks[i].SetPositions(new[]{p+new Vector3(-.84f,0,.6f),p+new Vector3(-.3f,0,.12f),p+new Vector3(.1f,0,.28f),p+new Vector3(-.1f,0,-.25f),p+new Vector3(.65f,0,-.84f)});
                cracks[i].gameObject.SetActive(false);
                var chalk=CreateDecoration("Answer pad chalk "+i,padStrokes,MgfLook.Unlit(MgfLook.Hex("#faf8e8")),false);
                chalk.transform.SetParent(root.transform,true);
            }

        }
        void SetPadVisibility(){
            for(int i=0;i<4;i++){bool active=i<Rules.PadCount;padRoots[i].gameObject.SetActive(active);padFill[i].gameObject.SetActive(active);if(!active)cracks[i].gameObject.SetActive(false);}
        }
        static readonly Vector2[] WestMesa={new Vector2(-10,4),new Vector2(-5.7f,4),new Vector2(-5.65f,5.25f),new Vector2(-3.5f,7),new Vector2(-.5f,9),new Vector2(-1,14),new Vector2(-10,14)};
        static readonly Vector2[] EastMesa={new Vector2(7.1f,5),new Vector2(9,5),new Vector2(13,9),new Vector2(13,16),new Vector2(6.9f,16)};
        static bool InPolygon(Vector2[] polygon,float x,float z){
            bool inside=false;
            for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++){
                var a=polygon[i];var b=polygon[j];
                if((a.y>z)!=(b.y>z)&&x<(b.x-a.x)*(z-a.y)/(b.y-a.y)+a.x)inside=!inside;
            }
            return inside;
        }
        static float SceneryGround(float x,float z)=>InPolygon(WestMesa,x,z)?3.1f:InPolygon(EastMesa,x,z)?1.2f:-1.52f;
        public GameObject SpawnModel(string name,Vector3 pos,float scale){
            var prefab=Resources.Load<GameObject>("HyeopgokSasu/Models/"+name);
            GameObject go;
            if(prefab)go=Instantiate(prefab,pos,Quaternion.identity);
            else{go=new GameObject("Missing "+name);go.transform.position=pos;Debug.LogWarning("Missing model "+name);}
            go.transform.localScale=Vector3.one*scale;
            go.transform.SetParent(sceneryRoot,true);
            foreach(var r in go.GetComponentsInChildren<Renderer>()){
                var mats=new Material[r.sharedMaterials.Length];for(int i=0;i<mats.Length;i++)mats[i]=worldMat;r.sharedMaterials=mats;
                // Vertex AO and the merged teal contact mesh ground static scenery.
                // Only the king and the two live battle towers spend shadow-map draws.
                r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=true;
            }
            return go;
        }
        LineRenderer Line(string name,Color c,float w){
            var l=new GameObject(name).AddComponent<LineRenderer>();l.sharedMaterial=MgfLook.Unlit(c);l.startWidth=l.endWidth=w;l.useWorldSpace=true;l.numCapVertices=0;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return l;
        }
        Vector3 PadEdge(int i,float t){
            // Uniform rounded perimeter keeps the white dashes thick at corners.
            const float x=.87f,z=.85f,r=.16f;
            float perimeter=4*(x+z-2*r)+Mathf.PI*2*r;
            float along=Mathf.Repeat(t,1)*perimeter;Vector3 p;
            float horizontal=2*(x-r),vertical=2*(z-r),arc=Mathf.PI*.5f*r;
            if(along<horizontal)p=new Vector3(-x+r+along,.025f,z);
            else if((along-=horizontal)<arc){float a=along/r;p=new Vector3(x-r+Mathf.Sin(a)*r,.025f,z-r+Mathf.Cos(a)*r);}
            else if((along-=arc)<vertical)p=new Vector3(x,.025f,z-r-along);
            else if((along-=vertical)<arc){float a=along/r;p=new Vector3(x-r+Mathf.Cos(a)*r,.025f,-z+r-Mathf.Sin(a)*r);}
            else if((along-=arc)<horizontal)p=new Vector3(x-r-along,.025f,-z);
            else if((along-=horizontal)<arc){float a=along/r;p=new Vector3(-x+r-Mathf.Sin(a)*r,.025f,-z+r-Mathf.Cos(a)*r);}
            else if((along-=arc)<vertical)p=new Vector3(-x,.025f,-z+r+along);
            else {along-=vertical;float a=along/r;p=new Vector3(-x+r-Mathf.Cos(a)*r,.025f,z-r+Mathf.Sin(a)*r);}
            return HyeopgokRules.Pads[i]+p;
        }
    }
}
