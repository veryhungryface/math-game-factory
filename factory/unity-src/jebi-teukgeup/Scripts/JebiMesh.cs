// 제비 특급 — 절차 메시·텍스처·합성음. 부팅 때 한 번 만들고 재사용한다(매 프레임 new 금지).
using System.Collections.Generic;
using UnityEngine;

namespace Mgf.JebiTeukgeup
{
    public static class JebiMesh
    {
        /// <summary>XY 평면에 누운 원환(z 가 두께 방향). 반지름 1, 관 반지름 tube.</summary>
        public static Mesh Torus(float tube, int seg = 72, int side = 10)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var c = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                for (int j = 0; j <= side; j++)
                {
                    float b = j * Mathf.PI * 2f / side;
                    var nr = c * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                    v.Add(c + nr * tube); n.Add(nr);
                }
            }
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < side; j++)
                {
                    int a = i * (side + 1) + j, b = a + side + 1;
                    tri.Add(a); tri.Add(a + 1); tri.Add(b);
                    tri.Add(b); tri.Add(a + 1); tri.Add(b + 1);
                }
            var m = new Mesh { name = "JebiTorus" };
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(tri, 0); m.RecalculateBounds();
            FixWinding(m);
            return m;
        }

        /// <summary>회전체(프로파일: x=반지름, y=높이). 종·기구 풍선·우편함 지붕에 쓴다.</summary>
        public static Mesh Lathe(Vector2[] profile, int seg = 28)
        {
            var v = new List<Vector3>(); var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                for (int j = 0; j < profile.Length; j++) v.Add(new Vector3(profile[j].x * c, profile[j].y, profile[j].x * s));
            }
            int P = profile.Length;
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < P - 1; j++)
                {
                    int a = i * P + j, b = a + P;
                    tri.Add(a); tri.Add(a + 1); tri.Add(b);
                    tri.Add(a + 1); tri.Add(b + 1); tri.Add(b);
                }
            var m = new Mesh { name = "JebiLathe" };
            m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
            FixWinding(m);
            return m;
        }

        /// <summary>반지름 0.5 구(유니티 기본 구와 같은 크기). 콜라이더 없이 메시만 — 물리 모듈이 빠진 빌드에서 CreatePrimitive 는 에러를 낸다.</summary>
        public static Mesh Sphere(int seg = 24, int rings = 14)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
                for (int j = 0; j <= rings; j++)
                {
                    float th = i * Mathf.PI * 2f / seg, ph = j * Mathf.PI / rings;
                    var d = new Vector3(Mathf.Sin(ph) * Mathf.Cos(th), -Mathf.Cos(ph), Mathf.Sin(ph) * Mathf.Sin(th));
                    v.Add(d * 0.5f); n.Add(d);
                }
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < rings; j++)
                {
                    int a = i * (rings + 1) + j, b = a + rings + 1;
                    tri.Add(a); tri.Add(a + 1); tri.Add(b);
                    tri.Add(a + 1); tri.Add(b + 1); tri.Add(b);
                }
            var m = new Mesh { name = "JebiSphere" };
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(tri, 0); m.RecalculateBounds();
            FixWinding(m);
            return m;
        }

        /// <summary>반지름 0.5, 높이 2(y −1..1) 원기둥 + 뚜껑. 기본 원기둥과 같은 크기.</summary>
        public static Mesh Cylinder(int seg = 20)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float th = i * Mathf.PI * 2f / seg;
                var d = new Vector3(Mathf.Cos(th), 0, Mathf.Sin(th));
                v.Add(d * 0.5f + Vector3.down); n.Add(d);
                v.Add(d * 0.5f + Vector3.up); n.Add(d);
            }
            for (int i = 0; i < seg; i++)
            {
                int a = i * 2;
                tri.Add(a); tri.Add(a + 1); tri.Add(a + 2);
                tri.Add(a + 1); tri.Add(a + 3); tri.Add(a + 2);
            }
            for (int cap = 0; cap < 2; cap++)
            {
                float y = cap == 0 ? -1f : 1f;
                var nn = cap == 0 ? Vector3.down : Vector3.up;
                int c = v.Count; v.Add(new Vector3(0, y, 0)); n.Add(nn);
                for (int i = 0; i <= seg; i++)
                {
                    float th = i * Mathf.PI * 2f / seg;
                    v.Add(new Vector3(Mathf.Cos(th) * 0.5f, y, Mathf.Sin(th) * 0.5f)); n.Add(nn);
                }
                for (int i = 0; i < seg; i++)
                {
                    if (cap == 1) { tri.Add(c); tri.Add(c + 2 + i); tri.Add(c + 1 + i); }
                    else { tri.Add(c); tri.Add(c + 1 + i); tri.Add(c + 2 + i); }
                }
            }
            var m = new Mesh { name = "JebiCylinder" };
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(tri, 0); m.RecalculateBounds();
            return m;
        }

        public static Mesh Cone(float radius, float height, int seg = 16)
        {
            return Lathe(new[] { new Vector2(0, 0), new Vector2(radius, 0.001f), new Vector2(0.0001f, height) }, seg);
        }

        /// <summary>두 갈래 제비 꼬리(얇은 판). 원점=꼬리 뿌리, -z 방향으로 뻗는다.</summary>
        public static Mesh ForkTail()
        {
            var m = new Mesh { name = "JebiTail" };
            var v = new[]
            {
                new Vector3(-0.16f, 0, 0), new Vector3(0.16f, 0, 0), new Vector3(0, 0, -0.32f),
                new Vector3(-0.30f, 0, -0.78f), new Vector3(0.30f, 0, -0.78f),
                new Vector3(-0.08f, 0, -0.36f), new Vector3(0.08f, 0, -0.36f),
            };
            var tri = new[] { 0, 2, 1, 0, 5, 2, 0, 3, 5, 1, 2, 6, 1, 6, 4, 0, 1, 2 };
            var all = new List<int>(tri);
            for (int i = 0; i < tri.Length; i += 3) { all.Add(tri[i]); all.Add(tri[i + 2]); all.Add(tri[i + 1]); } // 양면
            m.vertices = v; m.triangles = all.ToArray(); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>안전 그물: 가로 cols × 세로 rows 칸, 아래로 처진 해먹. XY 평면(z 약간 깊이).</summary>
        public static Mesh Net(float w, float h, int cols, int rows, float sag, float thick)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            System.Func<float, float, Vector3> P = (x, y) =>
            {
                float k = x / w; float drop = sag * 4f * k * (1f - k);
                return new Vector3(x - w * 0.5f, y - drop, (y / h) * 0.6f);
            };
            void Strip(Vector3 a, Vector3 b)
            {
                var d = (b - a); var nrm = new Vector3(-d.y, d.x, 0).normalized * thick * 0.5f;
                int s = v.Count;
                v.Add(a - nrm); v.Add(a + nrm); v.Add(b - nrm); v.Add(b + nrm);
                uv.Add(Vector2.zero); uv.Add(Vector2.zero); uv.Add(Vector2.zero); uv.Add(Vector2.zero);
                tri.Add(s); tri.Add(s + 1); tri.Add(s + 2); tri.Add(s + 2); tri.Add(s + 1); tri.Add(s + 3);
            }
            for (int r = 0; r <= rows; r++)
                for (int c = 0; c < cols * 3; c++)
                {
                    float x0 = w * c / (cols * 3f), x1 = w * (c + 1) / (cols * 3f), y = h * r / rows;
                    Strip(P(x0, y), P(x1, y));
                }
            for (int c = 0; c <= cols; c++)
                for (int r = 0; r < rows; r++)
                {
                    float x = w * c / cols, y0 = h * r / rows, y1 = h * (r + 1) / rows;
                    Strip(P(x, y0), P(x, y1));
                }
            var m = new Mesh { name = "JebiNet" };
            m.SetVertices(v); m.SetUVs(0, uv);
            var col = new Color[v.Count]; for (int i = 0; i < col.Length; i++) col[i] = Color.white;
            m.colors = col;
            m.SetTriangles(tri, 0); m.RecalculateBounds();
            return m;
        }

        public static Mesh Quad()
        {
            var m = new Mesh { name = "JebiQuad" };
            m.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            m.RecalculateBounds();
            return m;
        }

        /// <summary>여러 구를 하나로 합친 뭉게구름(드로콜 1개).</summary>
        public static Mesh Cloud(Mesh sphere, System.Random rng, int puffs)
        {
            var list = new List<CombineInstance>();
            float x = 0;
            for (int i = 0; i < puffs; i++)
            {
                float s = 1.2f + (float)rng.NextDouble() * 1.4f;
                if (i == 0 || i == puffs - 1) s *= 0.75f;
                var pos = new Vector3(x, (float)rng.NextDouble() * 0.5f + s * 0.25f, (float)rng.NextDouble() * 0.8f);
                list.Add(new CombineInstance { mesh = sphere, transform = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(s * 1.2f, s, s)) });
                x += s * 0.85f;
            }
            // 평평한 밑면
            list.Add(new CombineInstance { mesh = sphere, transform = Matrix4x4.TRS(new Vector3(x * 0.45f, 0.1f, 0.3f), Quaternion.identity, new Vector3(x * 1.05f, 0.9f, 1.6f)) });
            var m = new Mesh { name = "JebiCloud" };
            m.CombineMeshes(list.ToArray(), true, true);
            var b = m.bounds; var v = m.vertices;
            for (int i = 0; i < v.Length; i++) v[i] -= new Vector3(b.center.x, b.min.y, b.center.z);
            m.vertices = v; m.RecalculateBounds();
            return m;
        }

        static void FixWinding(Mesh m)
        {
            // 첫 삼각형 법선이 정점 법선과 반대면 전체를 뒤집는다(카메라 쪽에서 안 보이는 사고 방지).
            var v = m.vertices; var n = m.normals; var t = m.triangles;
            if (t.Length < 3 || n.Length == 0) return;
            int k = 0;
            for (int i = 0; i < t.Length; i += 3)
            {
                var c = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                if (c.sqrMagnitude > 1e-10f) { k = i; break; }
            }
            var cr = Vector3.Cross(v[t[k + 1]] - v[t[k]], v[t[k + 2]] - v[t[k]]);
            if (Vector3.Dot(cr, n[t[k]]) < 0)
            {
                for (int i = 0; i < t.Length; i += 3) { int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp; }
                m.triangles = t;
            }
        }

        // ───────── 텍스처 ─────────
        public static Texture2D Gradient(Color top, Color mid, Color bottom, float midAt)
        {
            const int H = 128;
            var tex = new Texture2D(4, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "JebiSkyGrad" };
            var px = new Color[4 * H];
            for (int y = 0; y < H; y++)
            {
                float k = y / (H - 1f);
                Color c = k > midAt ? Color.Lerp(mid, top, Mathf.SmoothStep(0, 1, (k - midAt) / (1 - midAt))) : Color.Lerp(bottom, mid, Mathf.SmoothStep(0, 1, k / midAt));
                for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
            }
            tex.SetPixels(px); tex.Apply(false, true);
            return tex;
        }

        public static Texture2D Dash()
        {
            var tex = new Texture2D(32, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, name = "JebiDash", filterMode = FilterMode.Bilinear };
            var px = new Color32[32 * 4];
            for (int y = 0; y < 4; y++) for (int x = 0; x < 32; x++) px[y * 32 + x] = new Color32(255, 255, 255, (byte)(x < 18 ? 255 : 0));
            tex.SetPixels32(px); tex.Apply(false, true);
            return tex;
        }

        /// <summary>UI 스프라이트. kind 0 둥근 사각(9-slice), 1 원, 2 고리, 3 삼각 화살표, 4 깃발 꼬리표(왼쪽 구멍).</summary>
        public static UnityEngine.Sprite MakeSprite(int kind, int size = 64, float radius = 18f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "JebiSprite" + kind };
            var px = new Color32[size * size];
            float h = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fx = x + .5f, fy = y + .5f, a;
                    float dx = fx - h, dy = fy - h, d = Mathf.Sqrt(dx * dx + dy * dy);
                    switch (kind)
                    {
                        case 1: a = Mathf.Clamp01(h - 1f - d + 0.5f); break;
                        case 2: a = Mathf.Clamp01(1f - Mathf.Abs(d - size * 0.38f) / (size * 0.09f) * 1f); a = Mathf.Clamp01(a * 2.2f); break;
                        case 3:
                            {
                                // 위를 향한 둥근 삼각형
                                float yy = fy / size, xx = Mathf.Abs(fx - h) / size;
                                a = (yy > 0.12f && yy < 0.88f && xx < (0.88f - yy) * 0.55f) ? 1f : 0f;
                                break;
                            }
                        default:
                            {
                                float qx = Mathf.Max(Mathf.Abs(fx - h) - (h - radius), 0);
                                float qy = Mathf.Max(Mathf.Abs(fy - h) - (h - radius), 0);
                                a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .5f);
                                if (kind == 4)
                                {
                                    float hx = fx - size * 0.16f, hy = fy - h;
                                    if (hx * hx + hy * hy < 16f) a = 0f;
                                }
                                break;
                            }
                    }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply(false, true);
            float b = (kind == 0 || kind == 4) ? radius + 1 : 0;
            return UnityEngine.Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }
    }

    /// <summary>게임 전용 합성음(고리 걸림 금속음·눈금 딸깍·종·장력파·그물). 킷 MgfSfx 와 같은 방식으로 1회 생성 후 재사용.</summary>
    public static class JebiAudio
    {
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        const int Rate = 44100;

        public static void Play(string kind, float vol = 0.5f, float pitch = 1f)
        {
            if (!src)
            {
                var go = new GameObject("JebiAudio"); Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0;
            }
            if (!clips.TryGetValue(kind, out var c)) clips[kind] = c = Make(kind);
            if (!c) return;
            src.pitch = pitch;
            src.PlayOneShot(c, vol);
        }

        static AudioClip Make(string kind)
        {
            switch (kind)
            {
                case "tick": return Synth(kind, 0.035f, (t, i) => Mathf.Sin(2 * Mathf.PI * 2400 * t) * Mathf.Exp(-t * 160f) * 0.5f);
                case "clink":
                    return Synth(kind, 0.45f, (t, i) =>
                        (Mathf.Sin(2 * Mathf.PI * 1480 * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 2730 * t) * 0.32f + Mathf.Sin(2 * Mathf.PI * 4110 * t) * 0.18f)
                        * Mathf.Exp(-t * 11f) * 0.55f);
                case "bell":
                    return Synth(kind, 1.3f, (t, i) =>
                        (Mathf.Sin(2 * Mathf.PI * 784 * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 1568 * t) * 0.25f + Mathf.Sin(2 * Mathf.PI * 2010 * t) * 0.15f + Mathf.Sin(2 * Mathf.PI * 392 * t) * 0.2f)
                        * Mathf.Exp(-t * 3.2f) * Mathf.Min(1f, t * 400f) * 0.5f);
                case "wave":
                    return Synth(kind, 0.42f, (t, i) =>
                    {
                        float f = 320f + 900f * t / 0.42f;
                        return Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Sin(Mathf.PI * t / 0.42f) * 0.35f;
                    });
                case "kick":
                    return Synth(kind, 0.18f, (t, i) => Mathf.Sin(2 * Mathf.PI * (160f - 300f * t) * t) * Mathf.Exp(-t * 22f) * 0.7f);
                case "net":
                    return Synth(kind, 0.5f, (t, i) =>
                    {
                        float f = 260f - 130f * t + 18f * Mathf.Sin(t * 70f);
                        return Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 6f) * 0.5f;
                    });
                case "snap":
                    return Synth(kind, 0.12f, (t, i) => ((i * 7919 % 97) / 48.5f - 1f) * Mathf.Exp(-t * 60f) * 0.45f + Mathf.Sin(2 * Mathf.PI * 900 * t) * Mathf.Exp(-t * 40f) * 0.3f);
                case "refuse":
                    return Synth(kind, 0.16f, (t, i) => Mathf.Sin(2 * Mathf.PI * (t < 0.07f ? 330f : 262f) * t) * Mathf.Exp(-t * 12f) * 0.35f);
                default: return null;
            }
        }

        static AudioClip Synth(string name, float dur, System.Func<float, int, float> f)
        {
            int n = (int)(Rate * dur);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float fade = Mathf.Min(1f, (n - i) / (Rate * 0.01f));
                d[i] = f(t, i) * fade;
            }
            var c = AudioClip.Create("jebi-" + name, n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }
    }
}
