// MGF Unity kit — 조명·재질·카메라·메시 도우미. 모든 재질은 킷 셰이더(Resources/MgfKit/Shaders)를 쓴다.
// Shader.Find("Standard") 류는 WebGL 빌드에서 스트리핑돼 분홍/검정 화면이 된다(Coral Rescue 실사고) — 여기 것을 써라.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf
{
    public static class MgfLook
    {
        static readonly Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Shader Shader(string name)
        {
            if (shaders.TryGetValue(name, out var s) && s) return s;
            s = Resources.Load<Shader>("MgfKit/Shaders/" + name);
            if (!s) Debug.LogError("[MGF] 킷 셰이더 없음: " + name);
            shaders[name] = s;
            return s;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c);
            return c;
        }

        /// <summary>PBR 불투명 재질(같은 인자면 캐시 공유 — 매 프레임 new Material 금지).</summary>
        public static Material Lit(Color color, float smoothness = 0.3f, float metallic = 0f, Color? emission = null, Texture tex = null)
        {
            var e = emission ?? Color.black;
            string key = "lit" + color + smoothness + metallic + e + (tex ? tex.GetInstanceID().ToString() : "");
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(Shader("MgfLit")) { name = "MgfLit" };
            m.SetColor("_Color", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            m.SetColor("_EmissionColor", e);
            if (tex) m.SetTexture("_MainTex", tex);
            cache[key] = m;
            return m;
        }

        public static Material Unlit(Color color, Texture tex = null) => Simple("MgfUnlit", color, tex);
        /// <summary>반투명(알파 블렌드). 정점색을 곱한다 — 파티클·LineRenderer 용. 정점색 없는 메시에 쓰면 결과를 눈으로 확인하라.</summary>
        public static Material Alpha(Color color, Texture tex = null) => Simple("MgfAlpha", color, tex);
        public static Material Additive(Color color, Texture tex = null) => Simple("MgfAdditive", color, tex);

        static Material Simple(string shader, Color color, Texture tex)
        {
            string key = shader + color + (tex ? tex.GetInstanceID().ToString() : "");
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(Shader(shader)) { name = shader };
            m.SetColor("_Color", color);
            if (tex) m.SetTexture("_MainTex", tex);
            cache[key] = m;
            return m;
        }

        /// <summary>기본 품질 설정(그림자·MSAA). 게임 Awake 에서 1회.</summary>
        public static void Quality(float shadowDistance = 40f)
        {
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.shadowCascades = 1;
            QualitySettings.pixelLightCount = 4;
            QualitySettings.antiAliasing = 4;
            if (MgfBridge.LowGfx)
            {
                // GPU 없는 환경(소프트웨어 렌더러): MSAA 끄고 그림자 해상도·거리를 낮춘다. 렌더 픽셀 수는 템플릿이
                // 약 36만으로 묶는다. 그림자는 유지 — 첫플레이 하네스(swiftshader) 캡처가 이 모드로 찍힌다.
                QualitySettings.antiAliasing = 0;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.shadowDistance = Mathf.Min(shadowDistance, 25f);
                QualitySettings.pixelLightCount = 1;
            }
        }

        /// <summary>그라디언트 하늘 + 삼색 환경광.</summary>
        public static void Sky(Color top, Color horizon, Color bottom, float ambient = 0.9f)
        {
            var m = new Material(Shader("MgfSky")) { name = "MgfSky" };
            m.SetColor("_Top", top);
            m.SetColor("_Horizon", horizon);
            m.SetColor("_Bottom", bottom);
            RenderSettings.skybox = m;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = top * ambient;
            RenderSettings.ambientEquatorColor = horizon * ambient * 0.85f;
            RenderSettings.ambientGroundColor = bottom * ambient * 0.6f;
        }

        /// <summary>그림자 있는 태양광.</summary>
        public static Light Sun(Vector3 euler, Color color, float intensity = 1.1f, float shadowStrength = 0.65f)
        {
            var l = new GameObject("Sun").AddComponent<Light>();
            l.type = LightType.Directional;
            l.transform.rotation = Quaternion.Euler(euler);
            l.color = color;
            l.intensity = intensity;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = shadowStrength;
            l.shadowBias = 0.04f;
            l.shadowNormalBias = 0.3f;
            l.renderMode = LightRenderMode.ForcePixel;
            RenderSettings.sun = l;
            return l;
        }

        /// <summary>Camera.main 을 찾거나 만든다(생성 씬에는 Main Camera 가 이미 있다).</summary>
        public static Camera Camera(Vector3 position, Vector3 lookAt, float fov = 40f)
        {
            var cam = UnityEngine.Camera.main;
            if (!cam)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<UnityEngine.Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.transform.position = position;
            cam.transform.LookAt(lookAt);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            cam.clearFlags = RenderSettings.skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.allowMSAA = true;
            cam.allowHDR = false;
            cam.renderingPath = RenderingPath.Forward;
            return cam;
        }

        /// <summary>세로 화면에서도 대상 폭이 잘리지 않게 FOV 를 맞춘다(매 프레임 불러도 싸다).</summary>
        public static void FitWidth(UnityEngine.Camera cam, float baseFov, float minAspect = 0.75f)
        {
            float a = Mathf.Max(0.01f, cam.aspect);
            if (a >= minAspect) { cam.fieldOfView = baseFov; return; }
            float h = Mathf.Tan(baseFov * 0.5f * Mathf.Deg2Rad) * minAspect / a;
            cam.fieldOfView = Mathf.Min(100f, 2f * Mathf.Atan(h) * Mathf.Rad2Deg);
        }

        /// <summary>프리미티브 + 재질. collider=false 면 콜라이더 제거.</summary>
        public static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent = null, bool collider = true)
        {
            var o = GameObject.CreatePrimitive(type);
            o.name = name;
            if (parent) o.transform.SetParent(parent, false);
            o.transform.localPosition = pos;
            o.transform.localScale = scale;
            if (!collider) Object.Destroy(o.GetComponent<Collider>());
            o.GetComponent<Renderer>().sharedMaterial = mat;
            return o;
        }

        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

        /// <summary>모서리가 둥근 상자 메시(크기 size, 둥글기 radius). 프리미티브 큐브보다 훨씬 덜 "시제품" 같다.</summary>
        public static Mesh RoundedBox(Vector3 size, float radius, int seg = 6)
        {
            string key = "rb" + size + radius + seg;
            if (meshes.TryGetValue(key, out var cached) && cached) return cached;
            radius = Mathf.Min(radius, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.5f - 0.0001f);
            var half = size * 0.5f;
            var inner = half - Vector3.one * radius;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            int n = seg * 2 + 1; // 면당 격자 분할
            Vector3[] fn = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var f in fn)
            {
                Vector3 u = Mathf.Abs(f.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(f, u);
                int start = verts.Count;
                for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    float s = (float)i / n * 2f - 1f, t = (float)j / n * 2f - 1f;
                    var p = Vector3.Scale(f + u * s + v * t, half); // 상자 표면 점
                    var c = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    var nrm = (p - c).normalized;
                    if (nrm == Vector3.zero) nrm = f;
                    verts.Add(c + nrm * radius);
                    norms.Add(nrm);
                    uvs.Add(new Vector2((float)i / n, (float)j / n));
                }
                for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int a = start + j * (n + 1) + i, b = a + 1, c2 = a + n + 1, d = c2 + 1;
                    tris.Add(a); tris.Add(c2); tris.Add(b);
                    tris.Add(b); tris.Add(c2); tris.Add(d);
                }
            }
            var mesh = new Mesh { name = "MgfRoundedBox" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            // 면 방향 검사: 첫 삼각형 법선이 바깥을 향하지 않으면 뒤집는다.
            var t0 = verts[tris[0]]; var t1 = verts[tris[1]]; var t2 = verts[tris[2]];
            if (Vector3.Dot(Vector3.Cross(t1 - t0, t2 - t0), norms[tris[0]]) < 0)
            {
                for (int k = 0; k < tris.Count; k += 3) { int tmp = tris[k + 1]; tris[k + 1] = tris[k + 2]; tris[k + 2] = tmp; }
                mesh.SetTriangles(tris, 0);
            }
            mesh.RecalculateBounds();
            meshes[key] = mesh;
            return mesh;
        }

        /// <summary>둥근 상자 오브젝트(BoxCollider 포함).</summary>
        public static GameObject Block(string name, Vector3 pos, Vector3 size, float radius, Material mat, Transform parent = null)
        {
            var o = new GameObject(name);
            if (parent) o.transform.SetParent(parent, false);
            o.transform.localPosition = pos;
            o.AddComponent<MeshFilter>().sharedMesh = RoundedBox(size, radius);
            var r = o.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;
            o.AddComponent<BoxCollider>().size = size;
            return o;
        }

        static Texture2D softDot;
        /// <summary>부드러운 원형 알파 텍스처(파티클·그림자 원판용, 1회 생성).</summary>
        public static Texture2D SoftDot
        {
            get
            {
                if (softDot) return softDot;
                const int S = 64;
                softDot = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "MgfSoftDot", wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[S * S];
                for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / S * 2 - 1, dy = (y + 0.5f) / S * 2 - 1;
                    float a = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
                softDot.SetPixels32(px);
                softDot.Apply(false, true);
                return softDot;
            }
        }
    }
}
