using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.HyeopgokSasu
{
    /// <summary>
    /// Runtime adapter for the compact AI-authored FBX/PNG pairs. Import stays
    /// material-free, so every asset shares the same one-pass game shader.
    /// </summary>
    static class HyeopgokAiAssets
    {
        const string Root = "HyeopgokSasu/AI3D/";

        public static Material CreateMaterial(string id, Color tint, float team = -1f, bool unlit = false)
        {
            Shader shader = Resources.Load<Shader>("HyeopgokSasu/Shaders/Horde");
            Material material = new Material(shader) { name = "AI3D " + id, enableInstancing = true };
            material.SetColor("_Color", tint);
            material.SetFloat("_Unlit", unlit ? 1f : 0f);
            material.SetFloat("_Team", team);
            Texture2D atlas = Resources.Load<Texture2D>(Root + id);
            if (atlas)
            {
                material.SetTexture("_MainTex", atlas);
                material.SetFloat("_UseMainTex", 1f);
            }
            return material;
        }

        public static Material CreateAdditiveMaterial(string id, Color tint)
        {
            Shader shader = Resources.Load<Shader>("HyeopgokSasu/Shaders/Spark");
            Material material = new Material(shader) { name = "AI3D additive " + id, enableInstancing = true };
            material.SetColor("_Color", tint);
            Texture2D atlas = Resources.Load<Texture2D>(Root + id);
            if (atlas)
            {
                material.SetTexture("_MainTex", atlas);
                material.SetFloat("_UseMainTex", 1f);
            }
            return material;
        }

        public static GameObject InstantiateModel(string id, Vector3 position, Quaternion rotation,
            float scale, Transform parent, Material material, bool castShadows)
        {
            GameObject prefab = Resources.Load<GameObject>(Root + id);
            if (!prefab) return null;
            GameObject instance = Object.Instantiate(prefab, position, rotation);
            instance.name = "AI3D " + id;
            instance.transform.localScale = Vector3.one * scale;
            if (parent) instance.transform.SetParent(parent, true);
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Material[] slots = new Material[renderers[r].sharedMaterials.Length];
                for (int m = 0; m < slots.Length; m++) slots[m] = material;
                renderers[r].sharedMaterials = slots;
                renderers[r].shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderers[r].receiveShadows = true;
            }
            return instance;
        }

        public static Mesh LoadInstancedMesh(string id)
        {
            GameObject prefab = Resources.Load<GameObject>(Root + id);
            if (!prefab) return null;
            MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>();
            if (filters.Length == 0) return null;
            CombineInstance[] combines = new CombineInstance[filters.Length];
            for (int i = 0; i < filters.Length; i++)
                combines[i] = new CombineInstance
                {
                    mesh = filters[i].sharedMesh,
                    transform = prefab.transform.worldToLocalMatrix * filters[i].transform.localToWorldMatrix
                };
            Mesh mesh = new Mesh { name = "AI3D instanced " + id };
            mesh.CombineMeshes(combines, true, true, false);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static void SetBlueprint(Renderer[] renderers, MaterialPropertyBlock block, bool blueprint, float flash)
        {
            block.SetFloat("_Blueprint", blueprint ? 1f : 0f);
            block.SetFloat("_Flash", flash);
            for (int i = 0; i < renderers.Length; i++) renderers[i].SetPropertyBlock(block);
        }
    }
}
