// 도토리 낙하 — 지하 뿌리 저장고 로우폴리 메시 빌더. 모든 부품은 32색 팔레트 텍스처의 한 텍셀을 UV 로 가리키므로
// 여러 부품을 한 메시로 합쳐도 재질 하나(드로콜 하나)로 그린다.
using System.Collections.Generic;
using Mgf;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.DotoriNakha
{
    public static class Pal
    {
        public const int Canopy = 0, LeafLight = 1, LeafDark = 2, Bark = 3, BarkDark = 4, Acorn = 5, AcornCap = 6, Paper = 7,
            Ink = 8, Flax = 9, FlaxDark = 10, Fur = 11, Belly = 12, FurDark = 13, Good = 14, Bad = 15, Moss = 16, Cream = 17,
            Roof = 18, Pink = 19, LeafDeep = 20, BarkLight = 21, Sack = 22, SackDark = 23, White = 24, Sunny = 25;

        static readonly string[] Hexes =
        {
            "#29283A", "#6D594A", "#393044", "#A95F42", "#1C1925", "#E7A84B", "#75432F", "#F3E7C4",
            "#211B24", "#CF815A", "#824B3E", "#6BC9AA", "#D8F0D7", "#3D8E79", "#62C8A6", "#D64C4C",
            "#514154", "#FFF4D6", "#B56645", "#E98B78", "#203A3C", "#CC7950", "#73503F", "#53372F",
            "#FFFFFF", "#F2D36A", "#000000", "#000000", "#000000", "#000000", "#000000", "#000000"
        };

        static Texture2D tex;
        static Material lit, litSoft;

        public static Color C(int i) => MgfLook.Hex(Hexes[i]);

        public static Texture2D Tex
        {
            get
            {
                if (tex) return tex;
                tex = new Texture2D(32, 1, TextureFormat.RGBA32, false, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DotoriPalette" };
                var px = new Color32[32];
                for (int i = 0; i < 32; i++) px[i] = MgfLook.Hex(Hexes[i]);
                tex.SetPixels32(px); tex.Apply(false, true);
                return tex;
            }
        }

        /// <summary>팔레트 재질(무광). 나무·잎·천.</summary>
        public static Material Lit => lit ? lit : (lit = MgfLook.Lit(Color.white, .12f, 0f, null, Tex));
        /// <summary>팔레트 재질(약한 광택). 도토리·다람쥐 눈.</summary>
        public static Material Soft => litSoft ? litSoft : (litSoft = MgfLook.Lit(new Color(1f, 1f, .995f), .42f, 0f, null, Tex));

        public static Vector2 UV(int i) => new Vector2((i + .5f) / 32f, .5f);
    }

    public sealed class LP
    {
        readonly List<Vector3> v = new List<Vector3>(4096);
        readonly List<Vector3> n = new List<Vector3>(4096);
        readonly List<Vector2> uv = new List<Vector2>(4096);
        readonly List<int> t = new List<int>(8192);

        static readonly Dictionary<int, List<Vector3>> icoCache = new Dictionary<int, List<Vector3>>();

        public void Clear() { v.Clear(); n.Clear(); uv.Clear(); t.Clear(); }
        public int Count => v.Count;

        void Tri(Matrix4x4 m, bool flip, Vector3 a, Vector3 b, Vector3 c, Vector3 hint, int col, bool flat, Vector3 na, Vector3 nb, Vector3 nc)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), hint) < 0) { var tmp = b; b = c; c = tmp; var tn = nb; nb = nc; nc = tn; }
            if (flip) { var tmp = b; b = c; c = tmp; var tn = nb; nb = nc; nc = tn; }
            Vector3 A = m.MultiplyPoint3x4(a), B = m.MultiplyPoint3x4(b), C = m.MultiplyPoint3x4(c);
            int i0 = v.Count;
            v.Add(A); v.Add(B); v.Add(C);
            if (flat)
            {
                var fn = Vector3.Cross(B - A, C - A).normalized;
                n.Add(fn); n.Add(fn); n.Add(fn);
            }
            else
            {
                var it = m.inverse.transpose;
                n.Add(it.MultiplyVector(na).normalized); n.Add(it.MultiplyVector(nb).normalized); n.Add(it.MultiplyVector(nc).normalized);
            }
            var u = Pal.UV(col); uv.Add(u); uv.Add(u); uv.Add(u);
            t.Add(i0); t.Add(i0 + 1); t.Add(i0 + 2);
        }

        static List<Vector3> Ico(int subdiv)
        {
            if (icoCache.TryGetValue(subdiv, out var cached)) return cached;
            float p = (1f + Mathf.Sqrt(5f)) * .5f;
            var verts = new List<Vector3>
            {
                new Vector3(-1, p, 0), new Vector3(1, p, 0), new Vector3(-1, -p, 0), new Vector3(1, -p, 0),
                new Vector3(0, -1, p), new Vector3(0, 1, p), new Vector3(0, -1, -p), new Vector3(0, 1, -p),
                new Vector3(p, 0, -1), new Vector3(p, 0, 1), new Vector3(-p, 0, -1), new Vector3(-p, 0, 1)
            };
            int[] f =
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };
            var tris = new List<Vector3>(f.Length);
            for (int i = 0; i < f.Length; i++) tris.Add(verts[f[i]].normalized * .5f);
            for (int s = 0; s < subdiv; s++)
            {
                var next = new List<Vector3>(tris.Count * 4);
                for (int i = 0; i < tris.Count; i += 3)
                {
                    Vector3 a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    Vector3 ab = ((a + b) * .5f).normalized * .5f, bc = ((b + c) * .5f).normalized * .5f, ca = ((c + a) * .5f).normalized * .5f;
                    next.Add(a); next.Add(ab); next.Add(ca);
                    next.Add(b); next.Add(bc); next.Add(ab);
                    next.Add(c); next.Add(ca); next.Add(bc);
                    next.Add(ab); next.Add(bc); next.Add(ca);
                }
                tris = next;
            }
            icoCache[subdiv] = tris;
            return tris;
        }

        static Vector3 Jit(Vector3 p, float amount, int seed)
        {
            if (amount <= 0f) return p;
            int x = Mathf.RoundToInt(p.x * 1000f), y = Mathf.RoundToInt(p.y * 1000f), z = Mathf.RoundToInt(p.z * 1000f);
            uint h = (uint)(x * 73856093 ^ y * 19349663 ^ z * 83492791 ^ seed * 2654435761u);
            h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
            float k = 1f + ((h & 1023) / 1023f - .5f) * 2f * amount;
            return p * k;
        }

        /// <summary>단위 지름 구(반지름 .5). flat=true 면 각진 로우폴리, false 면 매끈.</summary>
        public void Sphere(Matrix4x4 m, int col, int subdiv = 1, bool flat = true, float jitter = 0f, int seed = 0)
        {
            var tris = Ico(subdiv);
            bool flip = m.determinant < 0;
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 a = Jit(tris[i], jitter, seed), b = Jit(tris[i + 1], jitter, seed), c = Jit(tris[i + 2], jitter, seed);
                Tri(m, flip, a, b, c, (a + b + c), col, flat, tris[i], tris[i + 1], tris[i + 2]);
            }
        }

        /// <summary>y 축 원기둥/원뿔대(높이 1, 아래 반지름 rBot, 위 반지름 rTop).</summary>
        public void Cyl(Matrix4x4 m, int col, int sides = 8, float rBot = .5f, float rTop = .5f, bool caps = true, bool flat = true)
        {
            bool flip = m.determinant < 0;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                Vector3 b0 = d0 * rBot + Vector3.down * .5f, b1 = d1 * rBot + Vector3.down * .5f;
                Vector3 t0 = d0 * rTop + Vector3.up * .5f, t1 = d1 * rTop + Vector3.up * .5f;
                Vector3 hint = (d0 + d1);
                Tri(m, flip, b0, t0, t1, hint, col, flat, d0, d0, d1);
                Tri(m, flip, b0, t1, b1, hint, col, flat, d0, d1, d1);
                if (caps)
                {
                    if (rTop > .001f) Tri(m, flip, Vector3.up * .5f, t0, t1, Vector3.up, col, true, Vector3.up, Vector3.up, Vector3.up);
                    if (rBot > .001f) Tri(m, flip, Vector3.down * .5f, b0, b1, Vector3.down, col, true, Vector3.down, Vector3.down, Vector3.down);
                }
            }
        }

        public void Box(Matrix4x4 m, int col)
        {
            bool flip = m.determinant < 0;
            Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var d in dirs)
            {
                Vector3 u = Mathf.Abs(d.y) > .5f ? Vector3.right : Vector3.up;
                Vector3 w = Vector3.Cross(d, u);
                Vector3 c = d * .5f; u *= .5f; w *= .5f;
                Tri(m, flip, c - u - w, c + u - w, c + u + w, d, col, true, d, d, d);
                Tri(m, flip, c - u - w, c + u + w, c - u + w, d, col, true, d, d, d);
            }
        }

        /// <summary>두 점 사이 가지/줄(로컬 좌표).</summary>
        public void Tube(Vector3 a, Vector3 b, float r0, float r1, int col, int sides = 7, bool flat = true)
        {
            Vector3 d = b - a; float len = d.magnitude; if (len < 1e-4f) return;
            var m = Matrix4x4.TRS((a + b) * .5f, Quaternion.FromToRotation(Vector3.up, d / len), new Vector3(1f, len, 1f));
            Cyl(m, col, sides, r0, r1, true, flat);
        }

        /// <summary>XY 평면 위 원호 관(rim·handle).</summary>
        public void Arc(Matrix4x4 m, int col, float radius, float tube, float a0, float a1, int segs, int sides = 6)
        {
            for (int i = 0; i < segs; i++)
            {
                float u0 = Mathf.Lerp(a0, a1, i / (float)segs), u1 = Mathf.Lerp(a0, a1, (i + 1) / (float)segs);
                Vector3 p0 = new Vector3(Mathf.Cos(u0), Mathf.Sin(u0), 0) * radius, p1 = new Vector3(Mathf.Cos(u1), Mathf.Sin(u1), 0) * radius;
                var seg = Matrix4x4.TRS((p0 + p1) * .5f, Quaternion.FromToRotation(Vector3.up, (p1 - p0).normalized), new Vector3(tube * 2f, (p1 - p0).magnitude * 1.08f, tube * 2f));
                Cyl(m * seg, col, sides, .5f, .5f, false, true);
            }
        }

        public Mesh ToMesh(string name, Mesh reuse = null)
        {
            var mesh = reuse ? reuse : new Mesh { name = name };
            mesh.Clear();
            mesh.indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Matrix4x4 M(Vector3 p, Vector3 s) => Matrix4x4.TRS(p, Quaternion.identity, s);
        public static Matrix4x4 M(Vector3 p, Vector3 euler, Vector3 s) => Matrix4x4.TRS(p, Quaternion.Euler(euler), s);
    }
}
