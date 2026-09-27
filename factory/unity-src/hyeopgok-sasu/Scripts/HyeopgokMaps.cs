using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.HyeopgokSasu
{
    public partial class HyeopgokGame
    {
        static readonly string[] V32MapNames={"초원 협곡","사막 강 다리","설원 요새"};
        readonly Transform[] v32MapRoots=new Transform[3];
        readonly Material[] v32MapMaterials=new Material[3];
        int visibleMap=1;

        Color V32DaylightColor=>visibleMap==2?new Color(1f,.84f,.63f):visibleMap==3?new Color(.85f,.93f,1f):new Color(1f,.953f,.871f);
        Color V32MoonlightColor=>visibleMap==2?new Color(.67f,.58f,.66f):visibleMap==3?new Color(.48f,.66f,.82f):new Color(.588f,.655f,.933f);
        Color V32NightTintColor=>visibleMap==2?new Color(.58f,.47f,.62f):visibleMap==3?new Color(.38f,.57f,.76f):new Color(.46f,.49f,.80f);
        float V32DayAmbient=>visibleMap==2?.72f:visibleMap==3?.76f:.68f;

        string V32MapName(int map)=>V32MapNames[Mathf.Clamp(map-1,0,2)];

        void BuildV32Maps()
        {
            v32MapRoots[0]=sceneryRoot;
            v32MapMaterials[0]=worldMat;
            v32MapMaterials[1]=MakeBiomeMaterial("사막 정점색",MgfLook.Hex("#fff1cf"));
            v32MapMaterials[2]=MakeBiomeMaterial("설원 정점색",MgfLook.Hex("#e8f5ff"));
            v32MapRoots[1]=BuildBiomeRoot(2,"terrain_desert",v32MapMaterials[1]);
            v32MapRoots[2]=BuildBiomeRoot(3,"terrain_snow",v32MapMaterials[2]);
            for(int i=1;i<3;i++)v32MapRoots[i].gameObject.SetActive(false);
        }

        Material MakeBiomeMaterial(string name,Color tint)
        {
            var material=new Material(Resources.Load<Shader>("HyeopgokSasu/Shaders/Horde")){name=name,enableInstancing=true};
            material.SetColor("_Color",tint.linear);material.SetFloat("_Unlit",.04f);return material;
        }

        Transform BuildBiomeRoot(int map,string resource,Material material)
        {
            var root=new GameObject(map==2?"사막 강 다리 맵":"설원 요새 맵").transform;
            GameObject prefab=Resources.Load<GameObject>("HyeopgokSasu/Maps/"+resource);
            if(!prefab)prefab=Resources.Load<GameObject>("HyeopgokSasu/Models/terrain");
            if(prefab){GameObject terrain=Instantiate(prefab,Vector3.zero,Quaternion.identity,root);terrain.name=resource;PaintBiome(terrain,material);}
            SpawnBiomeModel(root,"enemy_gate",new Vector3(3.1f,.25f,9),.93f,180,material);
            SpawnBiomeModel(root,"enemy_gate",new Vector3(5.35f,.25f,9),.93f,180,material);
            // Reuse the compact authored props. Biome-only cactus, dead tree and snow
            // silhouettes are already merged into the generated one-draw terrain FBX.
            Vector4[] accents=map==2?new[]{new Vector4(-5.0f,1.2f,5.1f,.72f),new Vector4(5.5f,1.2f,3.4f,.54f),new Vector4(-4.7f,1.2f,-4.8f,.62f),new Vector4(4.8f,1.2f,-5.3f,.48f)}:
                new[]{new Vector4(-5.0f,1.2f,5.0f,.72f),new Vector4(5.4f,1.2f,3.6f,.62f),new Vector4(-4.9f,1.2f,-4.7f,.68f),new Vector4(4.7f,1.2f,-5.2f,.54f)};
            for(int i=0;i<accents.Length;i++){
                Vector4 p=accents[i];SpawnBiomeModel(root,map==2?"rock_medium":i%2==0?"tree":"rock_large",new Vector3(p.x,p.y,p.z),p.w,i*73,material);
            }
            return root;
        }

        void SpawnBiomeModel(Transform root,string id,Vector3 position,float scale,float yaw,Material material)
        {
            GameObject prefab=Resources.Load<GameObject>("HyeopgokSasu/Models/"+id);if(!prefab)return;
            GameObject go=Instantiate(prefab,position,Quaternion.Euler(0,yaw,0),root);go.transform.localScale=Vector3.one*scale;PaintBiome(go,material);
        }

        static void PaintBiome(GameObject go,Material material)
        {
            var renderers=go.GetComponentsInChildren<Renderer>(true);
            for(int r=0;r<renderers.Length;r++){
                var slots=new Material[renderers[r].sharedMaterials.Length];for(int m=0;m<slots.Length;m++)slots[m]=material;
                renderers[r].sharedMaterials=slots;renderers[r].shadowCastingMode=ShadowCastingMode.On;renderers[r].receiveShadows=true;
            }
        }

        void ApplyV32Map(int map)
        {
            map=Mathf.Clamp(map,1,3);visibleMap=map;
            for(int i=0;i<v32MapRoots.Length;i++)if(v32MapRoots[i])v32MapRoots[i].gameObject.SetActive(i==map-1);
            if(environmentRoot)environmentRoot.gameObject.SetActive(map==1);
            if(map==1){
                MgfLook.Sky(MgfLook.Hex("#bfe6e0"),MgfLook.Hex("#75bca8"),MgfLook.Hex("#315f58"),.8f);
                RenderSettings.ambientSkyColor=MgfLook.Hex("#a9d8d3");RenderSettings.ambientEquatorColor=MgfLook.Hex("#579384");RenderSettings.ambientGroundColor=MgfLook.Hex("#244f4d");RenderSettings.fogColor=MgfLook.Hex("#9bcfc6");
                if(environmentSun){environmentSun.color=MgfLook.Hex("#fff3de");environmentSun.intensity=1.03f;}
                Shader.SetGlobalColor("_HyeopgokNightTint",Color.white);
            }else if(map==2){
                MgfLook.Sky(MgfLook.Hex("#f8d69e"),MgfLook.Hex("#d89459"),MgfLook.Hex("#694b48"),.72f);
                RenderSettings.ambientSkyColor=MgfLook.Hex("#f0c88f");RenderSettings.ambientEquatorColor=MgfLook.Hex("#bc815d");RenderSettings.ambientGroundColor=MgfLook.Hex("#5e4942");RenderSettings.fogColor=MgfLook.Hex("#e9c38e");
                if(environmentSun){environmentSun.color=MgfLook.Hex("#ffd6a0");environmentSun.intensity=1.12f;}
                Shader.SetGlobalColor("_HyeopgokNightTint",MgfLook.Hex("#fff2dc"));
            }else{
                MgfLook.Sky(MgfLook.Hex("#d9efff"),MgfLook.Hex("#8abbd0"),MgfLook.Hex("#334f68"),.88f);
                RenderSettings.ambientSkyColor=MgfLook.Hex("#c8e8f5");RenderSettings.ambientEquatorColor=MgfLook.Hex("#759bae");RenderSettings.ambientGroundColor=MgfLook.Hex("#344b5c");RenderSettings.fogColor=MgfLook.Hex("#bed9e7");
                if(environmentSun){environmentSun.color=MgfLook.Hex("#d8eeff");environmentSun.intensity=.88f;}
                Shader.SetGlobalColor("_HyeopgokNightTint",MgfLook.Hex("#cbe9ff"));
            }
            RenderSettings.ambientIntensity=V32DayAmbient;
        }
    }
}
