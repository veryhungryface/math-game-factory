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
            MgfLook.Sun(new Vector3(44,-36,0),MgfLook.Hex("#fff1cf"),1.15f,.6f);
            RenderSettings.ambientIntensity=.85f;
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
            for(int i=0;i<42;i++){
                float x=(i%2==0?-1:1)*(6.9f+(float)rng.NextDouble()*5);
                float z=-14+(float)rng.NextDouble()*31;
                var tree=SpawnModel("tree",new Vector3(x,-1.45f,z),.85f+(float)rng.NextDouble()*.8f);tree.transform.Rotate(0,rng.Next(360),0);
            }
            for(int i=0;i<11;i++)SpawnModel("rock",new Vector3(-6.2f-(i%3)*.75f,-1.4f,-8+i*1.9f),.55f+(i%3)*.2f);
            SpawnModel("tree",new Vector3(-2.45f,1.2f,4.35f),.66f);
            SpawnModel("tree",new Vector3(-2.75f,1.2f,-3.95f),.62f);
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
