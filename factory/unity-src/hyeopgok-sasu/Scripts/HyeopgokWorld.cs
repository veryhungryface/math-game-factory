using UnityEngine;
using Mgf;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        Material worldMat;
        Transform sceneryRoot;
        Transform[] padRoots=new Transform[4];
        LineRenderer[] padBorders=new LineRenderer[4],padFill=new LineRenderer[4];
        LineRenderer[] cracks=new LineRenderer[4];
        void BuildWorld(){
            MgfLook.Sky(MgfLook.Hex("#9dd8c4"),MgfLook.Hex("#b4d2af"),MgfLook.Hex("#133f36"),.8f);
            var sun=MgfLook.Sun(new Vector3(48,-38,0),MgfLook.Hex("#fff0d2"),1.22f,.72f);
            sun.shadowBias=.025f;sun.shadowNormalBias=.18f;
            RenderSettings.ambientIntensity=.78f;
            cam=MgfLook.Camera(new Vector3(.5f,22,-13.6f),new Vector3(.5f,0,2.4f),36);cam.orthographic=true;cam.orthographicSize=14;
            cameraBase=cam.transform.position;cameraRot=cam.transform.rotation;
            worldMat=new Material(Resources.Load<Shader>("HyeopgokSasu/Shaders/Horde"));worldMat.SetColor("_Color",Color.white);worldMat.enableInstancing=true;
            sceneryRoot=new GameObject("Static scenery").transform;
            SpawnModel("terrain",Vector3.zero,1);
            SpawnModel("barracks",new Vector3(-4.1f,.25f,-.30f),.85f).transform.Rotate(0,180,0);
            SpawnModel("castle_gate",new Vector3(-4.4f,.25f,2.0f),.90f).transform.Rotate(0,180,0);
            SpawnModel("enemy_gate",new Vector3(3.1f,.25f,9.0f),1.15f).transform.Rotate(0,180,0);
            SpawnModel("enemy_gate",new Vector3(5.35f,.25f,9.0f),1.15f).transform.Rotate(0,180,0);
            king=SpawnModel("king",HyeopgokRules.Pads[2]+Vector3.back*2,1.4f).transform;king.rotation=Quaternion.Euler(0,180,0);
            king.SetParent(null,true);
            // The scenery is instantiated once. Horde entities use no GameObjects.
            var rng=new System.Random(20260926);
            for(int i=0;i<48;i++){
                float x=(i%2==0?-1:1)*(6.9f+(float)rng.NextDouble()*5);
                float z=-14+(float)rng.NextDouble()*31;
                // Respect the background mesas, rather than burying trunks in them.
                float ground=SceneryGround(x,z);
                var tree=SpawnModel(i%3==0?"tree_broadleaf":"tree",new Vector3(x,ground,z),.72f+(float)rng.NextDouble()*.65f);tree.transform.Rotate(0,rng.Next(360),0);
            }
            for(int i=0;i<11;i++)SpawnModel("rock",new Vector3(-6.2f-(i%3)*.75f,-1.4f,-8+i*1.9f),.55f+(i%3)*.2f);
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
                if(i<8&&i%2==0)SpawnModel("rock",new Vector3(p.x+.3f,p.y,p.z-.4f),.22f);
            }
            StaticBatchingUtility.Combine(sceneryRoot.gameObject);
            for(int i=0;i<4;i++){
                var root=new GameObject("Answer pad "+(i+1));root.transform.position=HyeopgokRules.Pads[i];padRoots[i]=root.transform;
                MgfLook.Prim(PrimitiveType.Cube,"Green stone",new Vector3(0,-.025f,0),new Vector3(1.7f,.045f,1.65f),MgfLook.Lit(MgfLook.Hex("#147855")),root.transform,false);
                padBorders[i]=Line("Dashed construction outline",MgfLook.Hex("#eef4cd"),.055f);
                var points=new Vector3[32];
                for(int k=0;k<32;k++)points[k]=PadEdge(i,k/31f);
                // Separate corner/dash strokes, sharing one material.
                padBorders[i].positionCount=0;
                for(int k=0;k<16;k++){
                    var dash=Line("Construction dash",MgfLook.Hex("#ecf1cc"),.065f);dash.positionCount=2;
                    dash.SetPosition(0,PadEdge(i,k/16f));dash.SetPosition(1,PadEdge(i,(k+.63f)/16f));dash.transform.SetParent(root.transform,true);
                }
                padFill[i]=Line("Hold progress",MgfLook.Hex("#ffd55d"),.095f);padFill[i].positionCount=0;
                cracks[i]=Line("Broken answer stone",MgfLook.Hex("#082c30"),.075f);cracks[i].positionCount=5;
                Vector3 p=HyeopgokRules.Pads[i]+Vector3.up*.06f;
                cracks[i].SetPositions(new[]{p+new Vector3(-.84f,0,.6f),p+new Vector3(-.3f,0,.12f),p+new Vector3(.1f,0,.28f),p+new Vector3(-.1f,0,-.25f),p+new Vector3(.65f,0,-.84f)});
                cracks[i].gameObject.SetActive(false);
            }
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
            foreach(var r in go.GetComponentsInChildren<Renderer>()){var mats=new Material[r.sharedMaterials.Length];for(int i=0;i<mats.Length;i++)mats[i]=worldMat;r.sharedMaterials=mats;}
            return go;
        }
        LineRenderer Line(string name,Color c,float w){
            var l=new GameObject(name).AddComponent<LineRenderer>();l.sharedMaterial=MgfLook.Unlit(c);l.startWidth=l.endWidth=w;l.useWorldSpace=true;l.numCapVertices=0;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return l;
        }
        Vector3 PadEdge(int i,float t){
            float f=(t%1)*4;Vector3 p;
            if(f<1)p=new Vector3(-.87f+1.74f*f,.025f,.85f);
            else if(f<2)p=new Vector3(.87f,.025f,.85f-1.7f*(f-1));
            else if(f<3)p=new Vector3(.87f-1.74f*(f-2),.025f,-.85f);
            else p=new Vector3(-.87f,.025f,-.85f+1.7f*(f-3));
            return HyeopgokRules.Pads[i]+p;
        }
    }
}
