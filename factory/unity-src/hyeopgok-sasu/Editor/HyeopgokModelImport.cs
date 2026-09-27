using UnityEditor;
using UnityEngine;

namespace Mgf.HyeopgokSasu.Editor
{
    // Source FBX assets use metres, +Y up / +Z forward, one vertex-colour submesh.
    // Readability is required for the shared instanced troop mesh and diagnostics.
    // FBX COLOR alpha is Blender-baked AO; UV0.x is team mask, UV0.y=1 marks it.
    // Keep both channels intact when correcting FBX handedness.
    public sealed class HyeopgokModelImport : AssetPostprocessor
    {
        // Version bump invalidates the imported FBX cache when this correction changes.
        public override uint GetVersion() { return 5; }

        static bool IsHyeopgokModel(string path)
        {
            return path.Contains("/Resources/HyeopgokSasu/Models/") ||
                   path.Contains("/Resources/HyeopgokSasu/AI3D/") ||
                   path.Contains("/Resources/HyeopgokSasu/Maps/");
        }

        static bool NeedsRuntimeReadback(string path)
        {
            // Only meshes combined into Graphics.DrawMeshInstanced buffers need a
            // CPU-readable copy in the player.  Static AI buildings/tower stages
            // otherwise duplicated several megabytes of vertex data in WebGL.
            if (path.Contains("/Resources/HyeopgokSasu/Models/")) return true;
            return path.EndsWith("/enemy_soldier.fbx") ||
                   path.EndsWith("/ally_soldier.fbx") ||
                   path.EndsWith("/giant.fbx") ||
                   path.EndsWith("/giant_blade_glow.fbx");
        }

        void OnPreprocessModel()
        {
            if (!IsHyeopgokModel(assetPath)) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            bool readable = NeedsRuntimeReadback(assetPath);
            importer.isReadable = readable;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.meshCompression = readable ? ModelImporterMeshCompression.Off : ModelImporterMeshCompression.High;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.addCollider = false;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/HyeopgokSasu/AI3D/")) return;
            var importer = (TextureImporter)assetImporter;
            // AI atlases use alpha as a material/team mask, never transparency.
            // Preserve it through WebGL compression and do not bleed RGB at edges.
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Compressed;
        }

        void OnPostprocessModel(GameObject root)
        {
            if (!IsHyeopgokModel(assetPath)) return;
            var filters = root.GetComponentsInChildren<MeshFilter>();
            var toRoot = new Matrix4x4[filters.Length];
            for (int i = 0; i < filters.Length; i++)
                toRoot[i] = root.transform.worldToLocalMatrix * filters[i].transform.localToWorldMatrix;

            // Unity's FBX right/left-handed conversion reflects the authored +Z.
            // Correct it in vertices, not a negative scene scale, so instances and
            // static prefabs use exactly the same metre-scale, +Z-forward mesh.
            var reflectZ = Matrix4x4.Scale(new Vector3(1, 1, -1));
            for (int i = 0; i < filters.Length; i++)
            {
                var filter = filters[i];
                var mesh = filter.sharedMesh;
                var bake = reflectZ * toRoot[i];
                var normalMatrix = bake.inverse.transpose;
                bool reflected = bake.determinant < 0;
                Debug.Log("[HYEOPGOK RAW] " + root.name + " mesh=" + mesh.bounds.min.ToString("F3") + ".." + mesh.bounds.max.ToString("F3") +
                    " childScale=" + filter.transform.localScale.ToString("F3") + " childEuler=" + filter.transform.localEulerAngles.ToString("F3") +
                    " rootScale=" + root.transform.localScale.ToString("F3") + " rootEuler=" + root.transform.localEulerAngles.ToString("F3"));
                var vertices = mesh.vertices;
                for (int v = 0; v < vertices.Length; v++) vertices[v] = bake.MultiplyPoint3x4(vertices[v]);
                mesh.vertices = vertices;
                var normals = mesh.normals;
                for (int n = 0; n < normals.Length; n++) normals[n] = normalMatrix.MultiplyVector(normals[n]).normalized;
                mesh.normals = normals;
                var tangents = mesh.tangents;
                for (int t = 0; t < tangents.Length; t++)
                {
                    var direction = bake.MultiplyVector(new Vector3(tangents[t].x, tangents[t].y, tangents[t].z)).normalized;
                    tangents[t] = new Vector4(direction.x, direction.y, direction.z, tangents[t].w * (reflected ? -1 : 1));
                }
                if (tangents.Length > 0) mesh.tangents = tangents;
                if (reflected)
                    for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    {
                        var indices = mesh.GetTriangles(submesh);
                        for (int t = 0; t < indices.Length; t += 3)
                        {
                            int swap = indices[t + 1]; indices[t + 1] = indices[t + 2]; indices[t + 2] = swap;
                        }
                        mesh.SetTriangles(indices, submesh, false);
                    }
                mesh.RecalculateBounds();
                if (filter.transform != root.transform) filter.transform.SetParent(root.transform, false);
                filter.transform.localPosition = Vector3.zero;
                filter.transform.localRotation = Quaternion.identity;
                filter.transform.localScale = Vector3.one;
            }
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            Bounds bounds = new Bounds();
            bool first = true;
            int triangles = 0;
            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh;
                triangles += mesh.triangles.Length / 3;
                foreach (var point in mesh.vertices)
                {
                    var p = root.transform.InverseTransformPoint(filter.transform.TransformPoint(point));
                    if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                    else bounds.Encapsulate(p);
                }
            }
            Debug.Log("[HYEOPGOK MODEL] " + root.name + " bounds=" + bounds.min.ToString("F3") + ".." + bounds.max.ToString("F3") + " triangles=" + triangles);
        }
    }
}
