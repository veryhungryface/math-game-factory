// 딱 맞는 방울 — 레몬 타일 욕조 세계(코드 메시), 거품가오리 루루, 방울·벽·못·접는 선 연출.
//
// 좌표: stage = 카메라와 같은 회전을 가진 기울어진 욕조 등받이 평면. stage 로컬 (u, v) 가 화면과 평행하고
// z<0 이 카메라 쪽이다. 직교 카메라라 판 위의 수학 표기(각·길이·원)가 왜곡 없이 보인다(2.5D: 주변 사물은 45° 쿼터뷰로 입체).
// 판 격자 (x, y) ↔ stage (x/Sub, y/Sub).
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.TtakMatneunBangul
{
    public partial class TtakMatneunBangulGame
    {
        static readonly Color Lemon = MgfLook.Hex("FFE680");
        static readonly Color Aqua = MgfLook.Hex("7FE3F2");
        static readonly Color Coral = MgfLook.Hex("FF8FA3");
        static readonly Color Teal = MgfLook.Hex("2B7A9B");
        static readonly Color Deep = MgfLook.Hex("1D5670");
        static readonly Color Cream = MgfLook.Hex("FFF8E6");
        static readonly Color Mint = MgfLook.Hex("A8E8CF");
        const float Pitch = 40f, Yaw = -12f;
        const float CamDist = 60f;

        Camera cam;
        Light sunLight;
        Transform stage, worldRoot, boardRoot, frameRoot, overlayRoot;
        Transform water, caustics, wallT;
        Material causticMat, bubbleMat, seedBubbleMat, ghostMat, lineMat, dashMat, glowMat, ringMat, shadowMat;
        Texture2D bubbleTex, ringTex, dashTex;

        // 판·틀
        readonly Transform[] walls = new Transform[3];
        readonly Transform[] nails = new Transform[3];
        readonly Transform[] marks = new Transform[6];
        readonly TextMeshPro[] vLabels = new TextMeshPro[3];
        readonly TextMeshPro[] angLabels = new TextMeshPro[3];
        readonly TextMeshPro[] sideLabels = new TextMeshPro[3];
        LineRenderer outlineLr, rightMarkLr;
        readonly LineRenderer[] tickLr = new LineRenderer[2];
        float frameGrow = 1f, frameKindBlend = 0f, frameKindTarget;  // 0 = 벽(안쪽), 1 = 못(바깥)

        // 방울·씨앗
        Transform bubble, bubbleShadow, seed, seedCore, demoBubble, ghostRing;
        TextMeshPro centerLabel, ghostLabel;
        Vector2 bubbleC; float bubbleR, bubbleTargetR, bubbleAnim; bool bubbleOn;
        Vector2 squashDir; float squash;
        readonly LineRenderer[] distLr = new LineRenderer[3];   // 거리 막대 안내선(흰색)
        readonly LineRenderer[] gapLr = new LineRenderer[3];    // 오답 틈(산호)
        readonly LineRenderer[] radLr = new LineRenderer[3];    // 정답 반지름
        readonly LineRenderer[] perpLr = new LineRenderer[3];   // 접점 직각 표시
        readonly Transform[] contactGlow = new Transform[3];
        readonly float[] contactFlash = new float[3];
        readonly bool[] contactTapped = new bool[3];
        Vector2 seedRest, seedPos; bool seedHeld, seedFlying; float seedReturnT;
        Vector2 seedFrom;

        // 접는 선
        readonly LineRenderer[] foldLr = new LineRenderer[6];
        readonly TextMeshPro[] foldLabel = new TextMeshPro[6];
        readonly float[] foldGrow = new float[6];
        int foldCount;
        LineRenderer ribbonLr;
        Transform stripRoll; TextMeshPro stripCountLabel;
        readonly Transform[] stripRolls = new Transform[3];
        Vector2 ribbonA, ribbonB; bool ribbonOn; float ribbonRetract;

        // 루루
        Transform lulu; Mesh luluMesh; Vector3[] luluBase, luluVerts; Color[] luluCols;
        LineRenderer luluTail, luluOutline;
        Vector2 luluPos, luluVel, luluTarget; float luluHeading, luluSquash, luluFlap = 1f;
        const int LU = 13, LV = 7;
        Transform luluEyeL, luluEyeR, luluFinL, luluFinR, luluShine, luluShadow;

        // 비누 생물·선반
        readonly Transform[] creatures = new Transform[6];
        readonly Transform[] rafts = new Transform[6];
        readonly Transform[] niches = new Transform[6];
        readonly TextMeshPro[] nicheStamp = new TextMeshPro[6];
        readonly Transform[] nicheGhost = new Transform[6];
        Material silhouetteMat;
        readonly Material[] creatureMats = new Material[6];
        readonly int[] creatureState = new int[6];   // 0 숨김, 1 대기, 2 방울 안, 3 선반, 4 잃음
        readonly float[] creatureAlpha = new float[6];
        Vector2 waitSpot; int waitingIdx = -1;

        // 장식
        Transform mirror, logoQ, logoShadowQ, faucet, towel, beam;
        Material logoMat, logoShadowMat, fogMat;
        readonly Transform[] leaves = new Transform[6];
        readonly Transform[] plants = new Transform[3];
        Transform bottle, sponge;
        readonly Transform[] floaters = new Transform[14];
        readonly Vector3[] floaterSeed = new Vector3[14];
        readonly Transform[] rings = new Transform[8];
        readonly Material[] ringMats = new Material[8];
        readonly Color[] ringCol = new Color[8];
        readonly float[] ringLife = new float[8];
        int ringCursor;
        Transform drip; float dripT;
        float worldT, logoReveal;

        // ───────────────────────────── 생성
        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("FFF1C8"), MgfLook.Hex("F6D98A"), MgfLook.Hex("7CC9D6"), .78f);
            sunLight = MgfLook.Sun(new Vector3(52f, -38f, 0f), MgfLook.Hex("FFF0D0"), 1.0f, .55f);
            sunLight.shadowNormalBias = .2f;
            cam = MgfLook.Camera(Vector3.zero, Vector3.forward, 30f);
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.nearClipPlane = 30f; cam.farClipPlane = 110f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = MgfLook.Hex("F7E7A6");

            worldRoot = new GameObject("BathWorld").transform;
            stage = new GameObject("Stage").transform;
            stage.SetParent(worldRoot, false);
            stage.rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            // 창빛: 판 왼쪽 위 앞에서 들어와 벽·못의 그림자가 오른쪽 아래로 떨어진다
            sunLight.transform.rotation = Quaternion.LookRotation(stage.TransformDirection(new Vector3(.42f, -.52f, .74f).normalized), stage.up);

            BuildTextures();
            BuildWallAndWater();
            BuildBoard();
            BuildBubbleParts();
            BuildLulu();
            BuildShelfAndCreatures();
            BuildDecor();
        }

        Vector3 SP(float u, float v, float z) => stage.TransformPoint(u, v, z);
        static Vector2 G2S(IP p) => new Vector2(p.x / (float)BangulRules.Sub, p.y / (float)BangulRules.Sub);
        static Vector2 G2S(double x, double y) => new Vector2((float)(x / BangulRules.Sub), (float)(y / BangulRules.Sub));

        Transform Child(string name, Transform parent, Vector3 local)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false); t.localPosition = local;
            return t;
        }

        static Mesh quadMesh;
        static Mesh QuadMesh()
        {
            if (quadMesh) return quadMesh;
            quadMesh = new Mesh { name = "BangulQuad" };
            quadMesh.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
            quadMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            quadMesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quadMesh.RecalculateNormals(); quadMesh.RecalculateBounds();
            return quadMesh;
        }

        Mesh AtlasQuad(int i)
        {
            var m = new Mesh { name = "SoapQuad" + i };
            float u0 = (i % 3) / 3f, u1 = u0 + 1f / 3f;
            float v1 = i < 3 ? 1f : .5f, v0 = v1 - .5f;
            m.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
            m.uv = new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1) };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            return m;
        }

        Transform Quad(string name, Transform parent, Vector3 local, Vector2 size, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return go.transform;
        }

        Transform BlockAt(string name, Transform parent, Vector3 local, Vector3 size, float radius, Material mat, bool shadows = true)
        {
            var go = MgfLook.Block(name, Vector3.zero, size, radius, mat, parent);
            go.transform.localPosition = local;
            var col = go.GetComponent<Collider>(); if (col) Destroy(col);
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go.transform;
        }

        Transform Ball(string name, Transform parent, Vector3 local, Vector3 scale, Material mat) => MeshObj(name, parent, SphereMesh(), local, scale, mat, false);

        Transform Cyl(string name, Transform parent, Vector3 local, Vector3 scale, Material mat) => MeshObj(name, parent, CylMesh(), local, scale, mat, true);

        Transform MeshObj(string name, Transform parent, Mesh mesh, Vector3 local, Vector3 scale, Material mat, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go.transform;
        }

        static Mesh sphereMesh, cylMesh;
        /// <summary>지름 1 구(UV 구 18×12).</summary>
        static Mesh SphereMesh()
        {
            if (sphereMesh) return sphereMesh;
            const int SU = 18, SV = 12;
            var v = new Vector3[(SU + 1) * (SV + 1)]; var n = new Vector3[v.Length]; var uv = new Vector2[v.Length];
            for (int j = 0; j <= SV; j++)
                for (int i = 0; i <= SU; i++)
                {
                    float th = j / (float)SV * Mathf.PI, ph = i / (float)SU * Mathf.PI * 2f;
                    var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    int k = j * (SU + 1) + i; v[k] = d * .5f; n[k] = d; uv[k] = new Vector2(i / (float)SU, 1f - j / (float)SV);
                }
            var t = new System.Collections.Generic.List<int>();
            for (int j = 0; j < SV; j++)
                for (int i = 0; i < SU; i++)
                {
                    int a = j * (SU + 1) + i, b = a + 1, c = a + SU + 1, d = c + 1;
                    t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
                }
            sphereMesh = new Mesh { name = "BangulSphere", vertices = v, normals = n, uv = uv };
            sphereMesh.SetTriangles(t, 0);
            FixWinding(sphereMesh);
            sphereMesh.RecalculateBounds();
            return sphereMesh;
        }

        /// <summary>높이 2(y −1..1)·지름 1 원기둥(유니티 기본 원기둥과 같은 치수).</summary>
        static Mesh CylMesh()
        {
            if (cylMesh) return cylMesh;
            const int N = 20;
            var v = new System.Collections.Generic.List<Vector3>(); var n = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
            for (int i = 0; i <= N; i++)
            {
                float a = i / (float)N * Mathf.PI * 2f; var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(d * .5f + Vector3.down); n.Add(d); v.Add(d * .5f + Vector3.up); n.Add(d);
            }
            for (int i = 0; i < N; i++) { int a = i * 2; t.Add(a); t.Add(a + 1); t.Add(a + 2); t.Add(a + 1); t.Add(a + 3); t.Add(a + 2); }
            for (int cap = 0; cap < 2; cap++)
            {
                float y = cap == 0 ? -1f : 1f; var nn = cap == 0 ? Vector3.down : Vector3.up;
                int c0 = v.Count; v.Add(new Vector3(0f, y, 0f)); n.Add(nn);
                for (int i = 0; i <= N; i++) { float a = i / (float)N * Mathf.PI * 2f; v.Add(new Vector3(Mathf.Cos(a) * .5f, y, Mathf.Sin(a) * .5f)); n.Add(nn); }
                for (int i = 0; i < N; i++) { if (cap == 0) { t.Add(c0); t.Add(c0 + 1 + i); t.Add(c0 + 2 + i); } else { t.Add(c0); t.Add(c0 + 2 + i); t.Add(c0 + 1 + i); } }
            }
            cylMesh = new Mesh { name = "BangulCyl" };
            cylMesh.SetVertices(v); cylMesh.SetNormals(n); cylMesh.SetTriangles(t, 0);
            FixWinding(cylMesh);
            cylMesh.RecalculateBounds();
            return cylMesh;
        }

        /// <summary>삼각형 방향이 법선과 반대면 뒤집는다(MgfLook.RoundedBox 와 같은 검사, 삼각형마다).</summary>
        static void FixWinding(Mesh m)
        {
            var v = m.vertices; var nn = m.normals; var t = m.triangles;
            for (int k = 0; k < t.Length; k += 3)
            {
                var c = Vector3.Cross(v[t[k + 1]] - v[t[k]], v[t[k + 2]] - v[t[k]]);
                var avg = nn[t[k]] + nn[t[k + 1]] + nn[t[k + 2]];
                if (Vector3.Dot(c, avg) < 0f) { int tmp = t[k + 1]; t[k + 1] = t[k + 2]; t[k + 2] = tmp; }
            }
            m.triangles = t;
        }

        Material AlphaMat(Color c, Texture t, int queue)
        {
            var m = new Material(MgfLook.Alpha(c, t)) { renderQueue = queue };
            return m;
        }

        Material AddMat(Color c, Texture t, int queue)
        {
            var m = new Material(MgfLook.Additive(c, t)) { renderQueue = queue };
            return m;
        }

        LineRenderer Line(string name, Transform parent, Material mat, float width, Color c, int queue = 3100)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.sharedMaterial = mat;
            lr.widthMultiplier = width;
            lr.startColor = lr.endColor = c;
            lr.numCapVertices = 3;
            lr.numCornerVertices = 2;
            lr.positionCount = 2;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
            lr.alignment = LineAlignment.TransformZ;
            lr.enabled = false;
            return lr;
        }

        TextMeshPro WorldText(string s, Transform parent, Vector3 local, float size, Color c, int order = 20)
        {
            var t = MgfText.World(s, Vector3.zero, size, c, parent);
            t.transform.localPosition = local;
            t.transform.localRotation = Quaternion.identity;
            t.sortingOrder = order;
            t.rectTransform.sizeDelta = new Vector2(8f, 1.6f);
            t.fontStyle = FontStyles.Bold;
            return t;
        }

        // ───────────────────────────── 절차 텍스처(부팅 때 1회)
        void BuildTextures()
        {
            bubbleTex = MakeBubbleTex(256);
            ringTex = MakeRingTex(128);
            dashTex = new Texture2D(32, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, name = "Dash" };
            var dp = new Color32[32 * 4];
            for (int y = 0; y < 4; y++) for (int x = 0; x < 32; x++) dp[y * 32 + x] = new Color32(255, 255, 255, (byte)(x < 19 ? 255 : 0));
            dashTex.SetPixels32(dp); dashTex.Apply(false, true);

            bubbleMat = AlphaMat(Color.white, bubbleTex, 3050);
            seedBubbleMat = AlphaMat(Color.white, bubbleTex, 3060);
            ghostMat = AlphaMat(new Color(1, 1, 1, .9f), ringTex, 3055);
            lineMat = AlphaMat(Color.white, null, 3100);
            dashMat = AlphaMat(Color.white, dashTex, 3090);
            glowMat = AddMat(Color.white, MgfLook.SoftDot, 3120);
            ringMat = AddMat(Color.white, ringTex, 3040);
            shadowMat = AlphaMat(new Color(.25f, .32f, .34f, .22f), MgfLook.SoftDot, 3010);
        }

        static Texture2D MakeBubbleTex(int S)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "SoapFilm" };
            var px = new Color32[S * S];
            Color[] film = { MgfLook.Hex("7FE3F2"), MgfLook.Hex("C7B6FF"), MgfLook.Hex("FF8FA3"), MgfLook.Hex("FFE680"), MgfLook.Hex("A8F0D8"), MgfLook.Hex("7FE3F2") };
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + .5f) / S * 2f - 1f, dy = (y + .5f) / S * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r > 1f) { px[y * S + x] = new Color32(255, 255, 255, 0); continue; }
                    float ang = Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + .5f;
                    float h = Mathf.Repeat(ang * 1.6f + r * .9f, 1f) * (film.Length - 1);
                    int i0 = Mathf.FloorToInt(h); Color c = Color.Lerp(film[i0], film[Mathf.Min(film.Length - 1, i0 + 1)], h - i0);
                    float rim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.70f, .97f, r)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.975f, 1f, r)));
                    float a = .07f + .78f * rim;
                    // 반사 하이라이트(창문 빛)
                    float hx = dx + .38f, hy = dy - .42f;
                    float hl = Mathf.Clamp01(1f - Mathf.Sqrt(hx * hx * 1.6f + hy * hy) / .2f);
                    float hx2 = dx - .45f, hy2 = dy + .36f;
                    float hl2 = Mathf.Clamp01(1f - Mathf.Sqrt(hx2 * hx2 + hy2 * hy2 * 2.2f) / .1f);
                    c = Color.Lerp(c, Color.white, Mathf.Clamp01(hl * 1.4f + hl2 + (1f - rim) * .35f));
                    a = Mathf.Clamp01(a + hl * .85f + hl2 * .6f);
                    // 바깥 테두리 얇은 진한 선(밝은 타일 위에서 윤곽이 읽히게)
                    float edge = Mathf.Clamp01(1f - Mathf.Abs(r - .985f) / .018f);
                    c = Color.Lerp(c, MgfLook.Hex("3C93B0"), edge * .55f);
                    a = Mathf.Max(a, edge * .9f);
                    px[y * S + x] = new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        static Texture2D MakeRingTex(int S)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "Ring" };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + .5f) / S * 2f - 1f, dy = (y + .5f) / S * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - Mathf.Abs(r - .9f) / .07f);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        static Texture2D TileTex(Color tile, Color grout, Color shade, int S, int n, string name)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = name, filterMode = FilterMode.Trilinear, anisoLevel = 2 };
            var px = new Color32[S * S];
            int cell = S / n; int g = Mathf.Max(2, cell / 22);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int cx = x % cell, cy = y % cell;
                    bool gr = cx < g || cy < g;
                    Color c;
                    if (gr) c = grout;
                    else
                    {
                        float fx = (cx - g) / (float)(cell - g), fy = (cy - g) / (float)(cell - g);
                        // 타일 하나하나 볼록 유약: 위·왼쪽이 밝고 아래·오른쪽 가장자리가 살짝 어둡다
                        float bevel = Mathf.Min(Mathf.Min(fx, fy), Mathf.Min(1 - fx, 1 - fy));
                        c = Color.Lerp(shade, tile, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(bevel * 9f)));
                        c = Color.Lerp(c, Color.white, Mathf.Clamp01((fy - .55f) * (.45f - fx) * 1.4f));
                        int ti = (x / cell) * 7 + (y / cell) * 13;
                        c *= 1f - (ti % 3) * .018f; c.a = 1f;
                    }
                    px[y * S + x] = c;
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        static Texture2D CausticTex(int S)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "Caustic" };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S * Mathf.PI * 2f, v = y / (float)S * Mathf.PI * 2f;
                    float w = Mathf.Sin(u * 2 + Mathf.Sin(v * 3) * 1.3f) + Mathf.Sin(v * 2 + Mathf.Sin(u * 3 + 1.7f) * 1.2f) + Mathf.Sin((u + v) * 3 + .5f) * .6f;
                    float a = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(w) / 0.55f), 2.2f);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 200));
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        // ───────────────────────────── 벽·물
        void BuildWallAndWater()
        {
            var wallTex = TileTex(MgfLook.Hex("FFE074"), MgfLook.Hex("8ED6C0"), MgfLook.Hex("EDBF45"), 256, 2, "LemonTile");
            var wallMat = new Material(MgfLook.Lit(MgfLook.Hex("FFFFFF"), .6f, 0f, null, wallTex)) { name = "WallTile" };
            wallMat.mainTextureScale = new Vector2(40f, 28f);
            var w = BlockAt("TileWall", stage, new Vector3(0f, 0f, .9f), new Vector3(56f, 40f, .4f), .05f, wallMat, false);
            w.GetComponent<MeshRenderer>().receiveShadows = true;
            wallT = w;

            // 물: 수평면(월드). 높이는 레이아웃이 정한다(가로/세로).
            var waterMat = MgfLook.Lit(MgfLook.Hex("45BCD4"), .92f, 0f, MgfLook.Hex("0F4E62"));
            var wgo = MgfLook.Block("Water", Vector3.zero, new Vector3(70f, .2f, 70f), .05f, waterMat, worldRoot);
            var wc = wgo.GetComponent<Collider>(); if (wc) Destroy(wc);
            wgo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            water = wgo.transform;
            causticMat = AddMat(new Color(.75f, 1f, 1f, .55f), CausticTex(128), 3001);
            causticMat.mainTextureScale = new Vector2(9f, 9f);
            var cq = Quad("Caustics", worldRoot, Vector3.zero, new Vector2(70f, 70f), causticMat);
            cq.rotation = Quaternion.Euler(90f, 0f, 0f);
            caustics = cq;
            // 욕조 앞 테두리(흰 도기)
            tubRim = Child("TubRimAnchor", worldRoot, Vector3.zero);
        }

        Transform tubRim;

        void SetWaterLevel(float vLine, float vRim)
        {
            // stage 의 v=vLine 줄에서 물이 등받이와 만난다
            Vector3 p = SP(0f, vLine, 0f);
            water.position = new Vector3(0f, p.y - .1f, p.z - 20f);
            caustics.position = new Vector3(0f, p.y + .02f, p.z - 20f);
            // 앞 테두리: 화면 아래쪽 v=vRim 에 오도록 물 위로
            Vector3 r = SP(0f, vRim, 0f);
            Vector3 f = stage.forward;
            float t = (p.y + .2f - r.y) / f.y;
            Vector3 onWater = r + f * t;
            tubRim.position = new Vector3(onWater.x, p.y + .25f, onWater.z);
            tubRim.rotation = Quaternion.Euler(0f, Yaw, 0f);
        }

        /// <summary>stage 의 v 가 수면선 아래면, 그 점이 수면 위에 오도록 카메라 쪽으로 당길 깊이(음수).</summary>
        float WaterZ(float v, float lift)
        {
            float d = waterLineV - v;
            if (d <= -lift) return 0f;
            return -(Mathf.Max(0f, d) + lift) * (Mathf.Cos(Pitch * Mathf.Deg2Rad) / Mathf.Sin(Pitch * Mathf.Deg2Rad)) - lift;
        }

        Vector3 OnWater(float u, float v, float lift = .05f)
        {
            Vector3 p = SP(u, v, 0f);
            float wy = water.position.y + .1f + lift;
            Vector3 f = stage.forward;
            float t = (wy - p.y) / f.y;
            return p + f * t;
        }

        // ───────────────────────────── 판
        void BuildBoard()
        {
            boardRoot = Child("Board", stage, Vector3.zero);
            var trayTex = TileTex(MgfLook.Hex("FFFDF6"), MgfLook.Hex("BFE7E4"), MgfLook.Hex("EEF2E8"), 256, 4, "TrayTile");
            var trayMat = new Material(MgfLook.Lit(Color.white, .5f, 0f, null, trayTex)) { name = "TrayTile" };
            trayMat.mainTextureScale = new Vector2(2.4f, 2f);
            var tray = BlockAt("Tray", boardRoot, new Vector3(0f, 0f, .16f), new Vector3(8.4f + .3f, 7f + .3f, .32f), .12f, trayMat, false);
            tray.GetComponent<MeshRenderer>().receiveShadows = true;
            var rimMat = MgfLook.Lit(MgfLook.Hex("FFFFFF"), .78f, 0f, MgfLook.Hex("1A1A10"));
            BlockAt("TrayRimTop", boardRoot, new Vector3(0f, 3.66f, .02f), new Vector3(8.9f, .28f, .46f), .12f, rimMat);
            BlockAt("TrayRimBottom", boardRoot, new Vector3(0f, -3.66f, .02f), new Vector3(8.9f, .28f, .46f), .12f, rimMat);
            BlockAt("TrayRimLeft", boardRoot, new Vector3(-4.36f, 0f, .02f), new Vector3(.28f, 7.6f, .46f), .12f, rimMat);
            BlockAt("TrayRimRight", boardRoot, new Vector3(4.36f, 0f, .02f), new Vector3(.28f, 7.6f, .46f), .12f, rimMat);

            frameRoot = Child("Frame", boardRoot, Vector3.zero);
            overlayRoot = Child("MathOverlay", boardRoot, new Vector3(0f, 0f, -.03f));
            var wallMat = MgfLook.Lit(MgfLook.Hex("2E86A6"), .82f, 0f, MgfLook.Hex("06222E"));
            for (int i = 0; i < 3; i++)
            {
                var holder = Child("Wall" + i, frameRoot, Vector3.zero);
                var blk = BlockAt("WallBody", holder, new Vector3(0f, 0f, -.2f), new Vector3(1f, .2f, .4f), .07f, wallMat);
                walls[i] = holder;
                holder.gameObject.SetActive(false);
            }
            var nailMat = MgfLook.Lit(MgfLook.Hex("D7E2E8"), .85f, .75f);
            var shellMat = MgfLook.Lit(Coral, .6f, 0f, MgfLook.Hex("3A1018"));
            for (int i = 0; i < 3; i++)
            {
                var holder = Child("Nail" + i, frameRoot, Vector3.zero);
                var peg = Cyl("Peg", holder, new Vector3(0f, 0f, -.32f), new Vector3(.16f, .32f, .16f), nailMat);
                peg.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var cap = Ball("Shell", holder, new Vector3(0f, 0f, -.66f), new Vector3(.36f, .36f, .18f), shellMat);
                cap.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
                nails[i] = holder;
                holder.gameObject.SetActive(false);
            }
            var markMat = MgfLook.Lit(MgfLook.Hex("3FB59A"), .7f, 0f, MgfLook.Hex("0E3A30"));
            for (int i = 0; i < 6; i++)
            {
                marks[i] = Ball("Mark" + i, overlayRoot, Vector3.zero, new Vector3(.2f, .2f, .1f), markMat);
                marks[i].gameObject.SetActive(false);
            }
            outlineLr = Line("Outline", overlayRoot, lineMat, .045f, new Color(Teal.r, Teal.g, Teal.b, .75f));
            outlineLr.loop = true; outlineLr.positionCount = 3;
            rightMarkLr = Line("RightMark", overlayRoot, lineMat, .04f, Deep);
            rightMarkLr.positionCount = 3;
            for (int i = 0; i < 2; i++) { tickLr[i] = Line("Tick" + i, overlayRoot, lineMat, .045f, Deep); tickLr[i].positionCount = 4; }
            for (int i = 0; i < 3; i++)
            {
                vLabels[i] = WorldText(BangulRules.Names[i], overlayRoot, Vector3.zero, 5.2f, Deep);
                angLabels[i] = WorldText("", overlayRoot, Vector3.zero, 3.6f, Teal);
                sideLabels[i] = WorldText("", overlayRoot, Vector3.zero, 3.4f, Teal);
            }
            for (int i = 0; i < 6; i++)
            {
                foldLr[i] = Line("Fold" + i, overlayRoot, dashMat, .06f, Teal, 3090);
                foldLr[i].textureMode = LineTextureMode.Tile;
                foldLabel[i] = WorldText("", overlayRoot, Vector3.zero, 2.8f, Deep);
                foldLabel[i].gameObject.SetActive(false);
            }
            ribbonLr = Line("Ribbon", overlayRoot, lineMat, .22f, MgfLook.Hex("8FE0C8"), 3110);
            ribbonLr.numCapVertices = 0;
        }

        // ───────────────────────────── 방울
        void BuildBubbleParts()
        {
            bubbleShadow = Quad("BubbleShadow", boardRoot, new Vector3(0f, 0f, -.01f), Vector2.one, shadowMat);
            bubble = Quad("Bubble", boardRoot, new Vector3(0f, 0f, -.28f), Vector2.one, bubbleMat);
            bubble.gameObject.SetActive(false); bubbleShadow.gameObject.SetActive(false);
            demoBubble = Quad("DemoBubble", boardRoot, new Vector3(0f, 0f, -.28f), Vector2.one, AlphaMat(new Color(1, 1, 1, .75f), bubbleTex, 3049));
            demoBubble.gameObject.SetActive(false);
            ghostRing = Quad("GhostRing", boardRoot, new Vector3(0f, 0f, -.3f), Vector2.one, ghostMat);
            ghostRing.gameObject.SetActive(false);
            centerLabel = WorldText("", boardRoot, new Vector3(0f, 0f, -.5f), 4f, Deep, 40);
            centerLabel.outlineWidth = .25f; centerLabel.outlineColor = new Color32(255, 255, 255, 230);
            ghostLabel = WorldText("", boardRoot, new Vector3(0f, 0f, -.5f), 3.2f, Deep, 40);
            ghostLabel.outlineWidth = .25f; ghostLabel.outlineColor = new Color32(255, 255, 255, 230);
            for (int i = 0; i < 3; i++)
            {
                distLr[i] = Line("Dist" + i, boardRoot, dashMat, .05f, new Color(Deep.r, Deep.g, Deep.b, .85f), 3105);
                distLr[i].textureMode = LineTextureMode.Tile;
                gapLr[i] = Line("Gap" + i, boardRoot, lineMat, .16f, MgfLook.Hex("FF6F8E"), 3106);
                radLr[i] = Line("Radius" + i, boardRoot, lineMat, .06f, Deep, 3107);
                perpLr[i] = Line("Perp" + i, boardRoot, lineMat, .035f, Deep, 3107);
                perpLr[i].positionCount = 3;
                contactGlow[i] = Quad("Contact" + i, boardRoot, Vector3.zero, Vector2.one * .7f, glowMat);
                contactGlow[i].gameObject.SetActive(false);
            }
            // 씨앗: 작은 비누막 + 레몬 씨앗
            seed = Quad("Seed", stage, Vector3.zero, Vector2.one * .62f, seedBubbleMat);
            seedCore = Ball("SeedCore", seed, new Vector3(0f, 0f, -.05f), new Vector3(.32f, .42f, .3f), MgfLook.Lit(MgfLook.Hex("F6C945"), .7f, 0f, MgfLook.Hex("3A2A00")));
            // 잔물결 고리 풀
            for (int i = 0; i < rings.Length; i++)
            {
                ringMats[i] = AlphaMat(Color.white, ringTex, 3115);
                rings[i] = Quad("Ripple" + i, worldRoot, Vector3.zero, Vector2.one, ringMats[i]);
                rings[i].gameObject.SetActive(false);
            }
        }

        // ───────────────────────────── 거품가오리 루루(이 게임 전용 오리지널 생물)
        void BuildLulu()
        {
            lulu = Child("Lulu", stage, new Vector3(-5f, -2f, -.9f));
            var bodyGo = new GameObject("Membrane", typeof(MeshFilter), typeof(MeshRenderer));
            bodyGo.transform.SetParent(lulu, false);
            luluMesh = new Mesh { name = "LuluMembrane" };
            luluMesh.MarkDynamic();
            luluBase = new Vector3[LU * LV]; luluVerts = new Vector3[LU * LV]; luluCols = new Color[LU * LV];
            var uv = new Vector2[LU * LV];
            for (int j = 0; j < LV; j++)
                for (int i = 0; i < LU; i++)
                {
                    float s = i / (float)(LU - 1) * 2f - 1f, t = j / (float)(LV - 1);
                    float a = Mathf.Abs(s);
                    float front = .46f - .62f * Mathf.Pow(a, 1.2f) - .09f * Mathf.Exp(-(s / .16f) * (s / .16f));
                    float back = front - (1.02f * (1f - Mathf.Pow(a, 1.05f)) + .04f);
                    float y = Mathf.Lerp(front, back, t);
                    luluBase[j * LU + i] = new Vector3(s * 1.3f, y, -.16f * (1f - s * s) * Mathf.Sin(Mathf.PI * t));
                    uv[j * LU + i] = new Vector2(s * .5f + .5f, t);
                }
            var tris = new List<int>();
            for (int j = 0; j < LV - 1; j++)
                for (int i = 0; i < LU - 1; i++)
                {
                    int a = j * LU + i, b = a + 1, c = a + LU, d = c + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d);
                }
            luluMesh.vertices = luluBase;
            luluMesh.uv = uv;
            luluMesh.colors = luluCols;
            luluMesh.SetTriangles(tris, 0);
            luluMesh.RecalculateBounds();
            bodyGo.GetComponent<MeshFilter>().sharedMesh = luluMesh;
            var mr = bodyGo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = AlphaMat(Color.white, null, 3070);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            luluOutline = Line("LuluRim", lulu, lineMat, .055f, new Color(.13f, .45f, .58f, .9f), 3071);
            luluOutline.loop = true; luluOutline.positionCount = 2 * LU + 2 * (LV - 2);
            luluOutline.enabled = true;
            luluTail = Line("LuluTail", lulu, lineMat, .07f, new Color(.85f, .95f, 1f, .8f), 3069);
            luluTail.positionCount = 8; luluTail.enabled = true;
            luluTail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, .35f);
            Ball("TailDrop", luluTail.transform, new Vector3(0f, -1.6f, -.05f), new Vector3(.2f, .26f, .12f), MgfLook.Lit(MgfLook.Hex("FFB6C4"), .9f, 0f, MgfLook.Hex("5A2A33")));
            var eyeMat = MgfLook.Lit(MgfLook.Hex("10373F"), .95f, 0f);
            var shine = MgfLook.Unlit(Color.white);
            luluEyeL = Ball("EyeL", lulu, new Vector3(-.2f, .16f, -.22f), new Vector3(.17f, .2f, .08f), eyeMat);
            luluEyeR = Ball("EyeR", lulu, new Vector3(.2f, .16f, -.22f), new Vector3(.17f, .2f, .08f), eyeMat);
            luluShine = Quad("LuluGloss", lulu, new Vector3(-.25f, .05f, -.25f), new Vector2(.7f, .45f), AddMat(new Color(1f, 1f, 1f, .55f), MgfLook.SoftDot, 3072));
            luluShadow = Quad("LuluShadow", stage, Vector3.zero, new Vector2(2.2f, 1.2f), AlphaMat(new Color(.12f, .3f, .36f, .16f), MgfLook.SoftDot, 3008));
            Ball("ShineL", luluEyeL, new Vector3(-.2f, .25f, -.6f), new Vector3(.35f, .35f, .3f), shine);
            Ball("ShineR", luluEyeR, new Vector3(-.2f, .25f, -.6f), new Vector3(.35f, .35f, .3f), shine);
            var fin = MgfLook.Lit(MgfLook.Hex("FFE15A"), .55f, 0f, MgfLook.Hex("4A3A00"));
            luluFinL = Ball("FinL", lulu, new Vector3(-.15f, .38f, -.12f), new Vector3(.12f, .28f, .08f), fin);
            luluFinR = Ball("FinR", lulu, new Vector3(.15f, .38f, -.12f), new Vector3(.12f, .28f, .08f), fin);
            luluPos = new Vector2(-5f, -2f);
        }

        void AnimateLulu(float dt)
        {
            // 목표 쪽으로 부드럽게 헤엄친다(임계 감쇠 스프링)
            Vector2 d = luluTarget - luluPos;
            luluVel += (d * 26f - luluVel * 9f) * dt;
            luluPos += luluVel * dt;
            float sp = luluVel.magnitude;
            if (luluIdling && sp < 1.6f)
            {
                float want = Mathf.Sin(worldT * .7f) * 14f;
                luluHeading = Mathf.LerpAngle(luluHeading, want, 1f - Mathf.Exp(-dt * 3f));
            }
            else if (sp > .25f)
            {
                float want = Mathf.Atan2(-luluVel.x, luluVel.y) * Mathf.Rad2Deg;
                luluHeading = Mathf.LerpAngle(luluHeading, want, 1f - Mathf.Exp(-dt * 7f));
            }
            luluSquash = Mathf.Lerp(luluSquash, 0f, 1f - Mathf.Exp(-dt * 3f));
            float sc = land ? 1.3f : 1.12f;
            lulu.localPosition = new Vector3(luluPos.x, luluPos.y, -.9f + WaterZ(luluPos.y, .25f));
            lulu.localRotation = Quaternion.Euler(0f, 0f, luluHeading);
            lulu.localScale = new Vector3(sc * (1f + luluSquash * .25f), sc * (1f - luluSquash * .35f), sc);

            float t = worldT * (2.6f + Mathf.Min(sp, 6f) * .45f) * luluFlap;
            Color[] film = { MgfLook.Hex("63D3EA"), MgfLook.Hex("A996FF"), MgfLook.Hex("FF93AE"), MgfLook.Hex("FFDE6A") };
            for (int j = 0; j < LV; j++)
                for (int i = 0; i < LU; i++)
                {
                    int k = j * LU + i;
                    var b = luluBase[k];
                    float a = Mathf.Abs(b.x) / 1.3f;
                    float wave = Mathf.Sin(t - a * 2.4f) * a * a;
                    // 날개 파동: 펼침 폭이 숨쉬고 날개 끝이 앞뒤로 물결친다(위에서 본 가오리의 날갯짓)
                    luluVerts[k] = new Vector3(b.x * (1f - .1f * (1f - Mathf.Cos(t - a * 2.4f)) * a), b.y + wave * .16f, b.z + wave * .2f);
                    float jt = j / (float)(LV - 1);
                    float hue = Mathf.Repeat(.15f + b.x * .35f + jt * .4f + worldT * .05f, 1f) * film.Length;
                    int h0 = Mathf.FloorToInt(hue) % film.Length;
                    Color c = Color.Lerp(film[h0], film[(h0 + 1) % film.Length], hue - Mathf.Floor(hue));
                    float edge = Mathf.Max(a, Mathf.Abs(jt - .5f) * 2f);
                    c.a = .72f + .24f * edge * edge + .06f * wave;
                    c = Color.Lerp(c, Color.white, .18f * (1f - edge) + .22f * Mathf.Max(0f, wave));
                    luluCols[k] = c;
                }
            luluMesh.vertices = luluVerts;
            luluMesh.colors = luluCols;
            // 테두리: 앞 가장자리 → 오른쪽 끝 → 뒤 가장자리 → 왼쪽 끝
            int n = 0;
            for (int i = 0; i < LU; i++) luluOutline.SetPosition(n++, luluVerts[i] + Vector3.back * .03f);
            for (int j = 1; j < LV - 1; j++) luluOutline.SetPosition(n++, luluVerts[j * LU + LU - 1] + Vector3.back * .03f);
            for (int i = LU - 1; i >= 0; i--) luluOutline.SetPosition(n++, luluVerts[(LV - 1) * LU + i] + Vector3.back * .03f);
            for (int j = LV - 2; j >= 1; j--) luluOutline.SetPosition(n++, luluVerts[j * LU] + Vector3.back * .03f);
            for (int i = 0; i < 8; i++)
            {
                float f = i / 7f;
                luluTail.SetPosition(i, new Vector3(Mathf.Sin(t * .8f - f * 3f) * .18f * f, -.4f - f * 1.25f, -.02f));
            }
            var drop = luluTail.transform.GetChild(0);
            drop.localPosition = new Vector3(Mathf.Sin(t * .8f - 3f) * .18f, -1.68f, -.05f);
            float blink = Mathf.Repeat(worldT, 4.2f) < .12f ? .2f : 1f;
            luluEyeL.localScale = new Vector3(.17f, .2f * blink, .08f);
            luluEyeR.localScale = new Vector3(.17f, .2f * blink, .08f);
            luluShadow.localPosition = new Vector3(luluPos.x + .35f, luluPos.y - .45f, -.04f + WaterZ(luluPos.y - .45f, .05f));
            luluShadow.localRotation = Quaternion.Euler(0f, 0f, luluHeading);
            luluShadow.localScale = new Vector3(2.2f * sc, 1.3f * sc, 1f);
            float fl = Mathf.Sin(t * 1.3f) * 14f;
            luluFinL.localRotation = Quaternion.Euler(0f, 0f, 18f + fl);
            luluFinR.localRotation = Quaternion.Euler(0f, 0f, -18f - fl);
        }

        // ───────────────────────────── 선반·비누 생물
        void BuildShelfAndCreatures()
        {
            var soapTex = Resources.Load<Texture2D>("TtakMatneunBangul/soap");
            silhouetteMat = AlphaMat(new Color(.17f, .48f, .6f, .16f), soapTex, 3012);
            var frameMat = MgfLook.Lit(MgfLook.Hex("FFFFFF"), .8f, 0f, MgfLook.Hex("15150C"));
            var backMat = MgfLook.Lit(MgfLook.Hex("BFEFF2"), .6f, 0f, MgfLook.Hex("0E3036"));
            for (int i = 0; i < 6; i++)
            {
                var n = Child("Niche" + i, stage, Vector3.zero);
                BlockAt("Back", n, new Vector3(0f, 0f, .05f), new Vector3(1.3f, 1.42f, .1f), .2f, backMat, false);
                BlockAt("Ledge", n, new Vector3(0f, -.66f, -.14f), new Vector3(1.4f, .14f, .42f), .06f, frameMat);
                BlockAt("Top", n, new Vector3(0f, .7f, -.08f), new Vector3(1.4f, .1f, .26f), .04f, frameMat);
                BlockAt("SideL", n, new Vector3(-.68f, 0f, -.08f), new Vector3(.1f, 1.42f, .26f), .04f, frameMat);
                BlockAt("SideR", n, new Vector3(.68f, 0f, -.08f), new Vector3(.1f, 1.42f, .26f), .04f, frameMat);
                nicheStamp[i] = WorldText("", n, new Vector3(0f, -.66f, -.37f), 2.4f, Deep, 32);
                var gq = new GameObject("Silhouette");
                gq.transform.SetParent(n, false); gq.transform.localPosition = new Vector3(0f, .05f, -.12f); gq.transform.localScale = Vector3.one * .95f;
                gq.AddComponent<MeshFilter>().sharedMesh = AtlasQuad(i);
                var gr = gq.AddComponent<MeshRenderer>(); gr.shadowCastingMode = ShadowCastingMode.Off;
                gr.sharedMaterial = silhouetteMat;
                nicheGhost[i] = gq.transform;
                niches[i] = n;
            }
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject("SoapLife" + i);
                go.transform.SetParent(stage, false);
                go.AddComponent<MeshFilter>().sharedMesh = AtlasQuad(i);
                var r = go.AddComponent<MeshRenderer>();
                creatureMats[i] = AlphaMat(Color.white, soapTex, 3065);
                r.sharedMaterial = creatureMats[i];
                r.shadowCastingMode = ShadowCastingMode.Off;
                creatures[i] = go.transform;
                go.SetActive(false);
                rafts[i] = Quad("FoamRaft" + i, stage, Vector3.zero, new Vector2(1.5f, .55f), AlphaMat(new Color(1f, 1f, 1f, .85f), MgfLook.SoftDot, 3020));
                rafts[i].gameObject.SetActive(false);
            }
            if (!soapTex)
                for (int i = 0; i < 6; i++) creatureMats[i].color = Color.Lerp(Lemon, Coral, i / 5f);
        }

        void ResetCreatures()
        {
            for (int i = 0; i < 6; i++)
            {
                creatureState[i] = 0; creatureAlpha[i] = 1f;
                creatures[i].gameObject.SetActive(false);
                rafts[i].gameObject.SetActive(false);
                nicheStamp[i].text = "";
                nicheGhost[i].gameObject.SetActive(true);
                creatureMats[i].color = Color.white;
            }
            waitingIdx = -1;
        }

        void ShowCreatureWaiting(int i)
        {
            if (i < 0 || i >= 6) return;
            waitingIdx = i;
            creatureState[i] = 1; creatureAlpha[i] = 1f;
            creatures[i].gameObject.SetActive(true);
            rafts[i].gameObject.SetActive(true);
            creatures[i].localPosition = new Vector3(waitSpot.x - 3f, waitSpot.y, -.7f + WaterZ(waitSpot.y - .6f, .3f));
        }

        void LoseCreature(int i)
        {
            if (i < 0 || i >= 6) return;
            creatureState[i] = 4;
            creatures[i].gameObject.SetActive(false);
            rafts[i].gameObject.SetActive(false);
            nicheStamp[i].text = "빈칸";
            nicheStamp[i].color = new Color(Teal.r, Teal.g, Teal.b, .6f);
        }

        void SettleRescue()
        {
            ClearBubble();
            int i = roundIdx;
            if (phase != Phase.Play || i < 0 || i >= 6)
            {
                // 연습 생물은 방울과 함께 위로 떠나 화면 밖으로(선반에 넣지 않는다)
                if (waitingIdx >= 0) { creatures[waitingIdx].gameObject.SetActive(false); rafts[waitingIdx].gameObject.SetActive(false); creatureState[waitingIdx] = 0; }
                waitingIdx = -1;
                return;
            }
            creatureState[i] = 3;
            nicheGhost[i].gameObject.SetActive(false);
            rafts[i].gameObject.SetActive(false);
            creatures[i].gameObject.SetActive(true);
            var n = niches[i].localPosition;
            creatures[i].localPosition = new Vector3(n.x, n.y + .05f, n.z - .35f);
            creatures[i].localScale = Vector3.one * 1.15f;
            nicheStamp[i].text = board.kind == Kind.In ? "내심 I" : "외심 O";
            nicheStamp[i].color = Deep;
            SpawnRingAtStage(new Vector2(n.x, n.y), 1.6f);
        }


        // ───────────────────────────── 장식(창빛·거울·수도·수건·잎·떠다니는 방울)
        void BuildDecor()
        {
            // 김 서린 거울 + 제목 글씨
            mirror = Child("Mirror", stage, Vector3.zero);
            var mframe = MgfLook.Lit(MgfLook.Hex("FFFFFF"), .85f, .1f, MgfLook.Hex("1A1A14"));
            BlockAt("MirrorFrame", mirror, new Vector3(0f, 0f, -.05f), new Vector3(1f, 1f, .2f), .2f, mframe);
            fogMat = AlphaMat(new Color(.93f, .98f, 1f, .97f), MakeFogTex(128), 3005);
            Quad("Fog", mirror, new Vector3(0f, 0f, -.17f), new Vector2(.94f, .9f), fogMat);
            var logoTex = Resources.Load<Texture2D>("TtakMatneunBangul/logo");
            logoShadowMat = AlphaMat(new Color(.10f, .32f, .42f, .32f), logoTex, 3006);
            logoMat = AlphaMat(Color.white, logoTex, 3007);
            logoShadowQ = Quad("LogoShadow", mirror, new Vector3(.012f, -.02f, -.18f), new Vector2(.9f, .33f), logoShadowMat);
            logoQ = Quad("Logo", mirror, new Vector3(0f, 0f, -.19f), new Vector2(.9f, .33f), logoMat);
            if (!logoTex) { logoQ.gameObject.SetActive(false); logoShadowQ.gameObject.SetActive(false); fallbackLogo = WorldText("딱 맞는 방울", mirror, new Vector3(0f, 0f, -.2f), 1.2f, Deep); }

            // 수도꼭지(크롬)
            faucet = Child("Faucet", stage, Vector3.zero);
            var chrome = MgfLook.Lit(MgfLook.Hex("DCE8EE"), .9f, .85f);
            var b0 = Cyl("Base", faucet, new Vector3(0f, 0f, -.15f), new Vector3(.55f, .15f, .55f), chrome);
            b0.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var neck = Cyl("Neck", faucet, new Vector3(0f, 0f, -.6f), new Vector3(.24f, .45f, .24f), chrome);
            neck.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Cyl("Spout", faucet, new Vector3(0f, -.35f, -1.0f), new Vector3(.2f, .4f, .2f), chrome);
            var knob = Ball("Knob", faucet, new Vector3(.55f, .25f, -.35f), new Vector3(.36f, .36f, .3f), MgfLook.Lit(Coral, .7f, 0f, MgfLook.Hex("401418")));
            knob.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            var knob2 = Ball("Knob2", faucet, new Vector3(-.55f, .25f, -.35f), new Vector3(.36f, .36f, .3f), MgfLook.Lit(Aqua, .7f, 0f, MgfLook.Hex("0E3A44")));
            knob2.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            drip = Ball("Drip", stage, Vector3.zero, new Vector3(.12f, .17f, .12f), MgfLook.Lit(MgfLook.Hex("BFF4FF"), .95f, 0f, MgfLook.Hex("2A6070")));

            // 수건(줄무늬) — 화면 가장자리에서 흔들린다
            var towelTex = new Texture2D(8, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, name = "Towel" };
            var tp = new Color32[8 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 8; x++) tp[y * 8 + x] = (y / 8) % 3 == 0 ? new Color32(127, 227, 242, 255) : new Color32(255, 255, 255, 255);
            towelTex.SetPixels32(tp); towelTex.Apply(false, true);
            towel = Child("Towel", stage, Vector3.zero);
            BlockAt("Bar", towel, new Vector3(0f, .15f, -.3f), new Vector3(2.2f, .16f, .16f), .07f, chrome);
            var tb = BlockAt("Cloth", towel, new Vector3(0f, -1.3f, -.42f), new Vector3(1.6f, 2.8f, .16f), .07f, new Material(MgfLook.Lit(Color.white, .2f, 0f, null, towelTex)));

            // 비누잎 화분(둥근 잎이 바람 없이도 숨쉬듯 흔들린다)
            var leafMat = MgfLook.Lit(MgfLook.Hex("4DB884"), .55f, 0f, MgfLook.Hex("0D2E20"));
            var leafMat2 = MgfLook.Lit(MgfLook.Hex("86D6A0"), .55f, 0f, MgfLook.Hex("10301E"));
            var potMat = MgfLook.Lit(MgfLook.Hex("FFFFFF"), .8f, 0f, MgfLook.Hex("15150C"));
            var potBand = MgfLook.Lit(Coral, .6f, 0f, MgfLook.Hex("3A1018"));
            for (int p = 0; p < 3; p++)
            {
                var pl = Child("Plant" + p, stage, Vector3.zero);
                BlockAt("Pot", pl, new Vector3(0f, -.25f, -.35f), new Vector3(.9f, .7f, .7f), .25f, potMat);
                BlockAt("PotBand", pl, new Vector3(0f, -.05f, -.36f), new Vector3(.94f, .14f, .74f), .06f, potBand);
                for (int k = 0; k < 2; k++)
                {
                    int i = p * 2 + k;
                    var holder = Child("Leaf" + i, pl, new Vector3(0f, .05f, -.4f));
                    var blade = Ball("Blade", holder, new Vector3(0f, .55f, 0f), new Vector3(.42f, 1.1f, .14f), i % 2 == 0 ? leafMat : leafMat2);
                    blade.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
                    leaves[i] = holder;
                }
                var mid = Ball("BladeMid", pl, new Vector3(0f, .62f, -.42f), new Vector3(.36f, .95f, .14f), leafMat2);
                mid.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
                plants[p] = pl;
            }
            bottle = Child("ShampooBottle", stage, Vector3.zero);
            BlockAt("Body", bottle, new Vector3(0f, 0f, -.35f), new Vector3(.75f, 1.2f, .6f), .3f, MgfLook.Lit(MgfLook.Hex("FFB3C4"), .75f, 0f, MgfLook.Hex("3A1820")));
            BlockAt("Label", bottle, new Vector3(0f, -.05f, -.66f), new Vector3(.5f, .45f, .04f), .08f, MgfLook.Lit(Cream, .5f));
            BlockAt("Cap", bottle, new Vector3(0f, .72f, -.35f), new Vector3(.36f, .28f, .36f), .1f, MgfLook.Lit(MgfLook.Hex("DCE8EE"), .9f, .8f));
            Cyl("Pump", bottle, new Vector3(.12f, .98f, -.35f), new Vector3(.1f, .14f, .1f), MgfLook.Lit(MgfLook.Hex("DCE8EE"), .9f, .8f));
            sponge = Child("Sponge", stage, Vector3.zero);
            BlockAt("Foam", sponge, new Vector3(0f, 0f, -.3f), new Vector3(1.1f, .55f, .5f), .18f, MgfLook.Lit(MgfLook.Hex("FFD84D"), .25f, 0f, MgfLook.Hex("2A2000")));
            BlockAt("Scrub", sponge, new Vector3(0f, -.2f, -.3f), new Vector3(1.12f, .16f, .52f), .07f, MgfLook.Lit(MgfLook.Hex("6CCB9A"), .3f));

            // 창문 빛 띠
            var beamTex = new Texture2D(4, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Beam" };
            var bp = new Color32[4 * 64];
            for (int y = 0; y < 64; y++) { float f = Mathf.Sin(y / 63f * Mathf.PI); for (int x = 0; x < 4; x++) bp[y * 4 + x] = new Color32(255, 250, 225, (byte)(f * f * 255)); }
            beamTex.SetPixels32(bp); beamTex.Apply(false, true);
            beam = Quad("WindowBeam", stage, new Vector3(-1f, 2f, -1.6f), new Vector2(30f, 4.4f), AddMat(new Color(1f, .95f, .8f, .16f), beamTex, 3002));
            beam.localRotation = Quaternion.Euler(0f, 0f, -28f);

            // 떠다니는 작은 방울(풀 14개, 화면 위로 천천히 흐른다)
            var fMat = AlphaMat(new Color(1f, 1f, 1f, .8f), bubbleTex, 3045);
            for (int i = 0; i < floaters.Length; i++)
            {
                floaters[i] = Quad("Floater" + i, stage, Vector3.zero, Vector2.one * .3f, fMat);
                floaterSeed[i] = new Vector3((i * 37 % 100) / 100f, (i * 61 % 100) / 100f, .12f + (i % 5) * .07f);
            }
        }

        TextMeshPro fallbackLogo;

        static Texture2D MakeFogTex(int S)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "Fog" };
            var px = new Color32[S * S];
            var rng = new System.Random(5);
            var drops = new Vector3[60];
            for (int i = 0; i < drops.Length; i++) drops[i] = new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), .006f + (float)rng.NextDouble() * .018f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float n = .82f + .08f * Mathf.Sin(u * 17f + v * 5f) * Mathf.Sin(v * 13f - u * 3f);
                    float a = n;
                    Color c = new Color(.95f, .985f, 1f);
                    foreach (var d in drops)
                    {
                        float dx = u - d.x, dy = (v - d.y) * .7f;
                        float rr = Mathf.Sqrt(dx * dx + dy * dy);
                        if (rr < d.z) { a = .55f; c = Color.Lerp(c, MgfLook.Hex("CBEFF7"), .6f); }
                    }
                    px[y * S + x] = new Color(c.r, c.g, c.b, a);
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        // ───────────────────────────── 판 연출 갱신
        void ApplyBoardVisuals(bool rebuildFrame)
        {
            if (board == null) return;
            ClearBubble();
            HideDistance();
            for (int i = 0; i < 3; i++)
            {
                radLr[i].enabled = false; gapLr[i].enabled = false; perpLr[i].enabled = false;
                contactGlow[i].gameObject.SetActive(false);
            }
            ghostRing.gameObject.SetActive(false); ghostLabel.text = "";
            centerLabel.text = "";
            if (rebuildFrame)
            {
                foldCount = 0;
                for (int i = 0; i < 6; i++) { foldLr[i].enabled = false; foldLabel[i].gameObject.SetActive(false); }
            }
            Vector2[] P = { G2S(board.V[0]), G2S(board.V[1]), G2S(board.V[2]) };
            double area = BangulRules.Cross(board.V[0], board.V[1], board.V[2]);
            float orient = area > 0 ? 1f : -1f;
            for (int i = 0; i < 3; i++)
            {
                // 변 i = V[i+1] → V[i+2], 안쪽 법선은 V[i] 쪽
                Vector2 a = P[(i + 1) % 3], b = P[(i + 2) % 3];
                Vector2 d = (b - a); float len = d.magnitude; d /= len;
                Vector2 nIn = new Vector2(-d.y, d.x) * orient;
                const float thick = .2f;
                Vector2 c = (a + b) * .5f - nIn * thick * .5f;
                walls[i].localPosition = new Vector3(c.x, c.y, 0f);
                walls[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                walls[i].GetChild(0).localScale = new Vector3(len + thick * 1.4f, 1f, 1f);
                nails[i].localPosition = new Vector3(P[i].x, P[i].y, 0f);
                outlineLr.SetPosition(i, new Vector3(P[i].x, P[i].y, 0f));
                // 꼭짓점 이름: 외각 쪽 이등분 방향
                Vector2 toA = (P[(i + 1) % 3] - P[i]).normalized, toB = (P[(i + 2) % 3] - P[i]).normalized;
                Vector2 bis = (toA + toB).normalized;
                vLabels[i].transform.localPosition = new Vector3(P[i].x - bis.x * .5f, P[i].y - bis.y * .5f, -.05f);
                angLabels[i].transform.localPosition = new Vector3(P[i].x + bis.x * .95f, P[i].y + bis.y * .95f, -.05f);
                angLabels[i].text = board.angleNumbers && board.ang[i] > 0 && board.shape != Shape.Right ? board.ang[i] + "°" : "";
                sideLabels[i].text = board.lens != null ? board.lens[i] + " cm" : "";
                Vector2 sm = (a + b) * .5f - nIn * .42f;
                sideLabels[i].transform.localPosition = new Vector3(sm.x, sm.y, -.05f);
            }
            frameKindTarget = board.kind == Kind.In ? 0f : 1f;
            if (rebuildFrame) frameKindBlend = frameKindTarget;
            // 같은 변 표시(이등변: AB, AC 에 두 줄 눈금)
            bool iso = board.shape == Shape.Iso;
            for (int k = 0; k < 2; k++)
            {
                tickLr[k].enabled = iso;
                if (!iso) continue;
                Vector2 a = P[0], b = P[k + 1];
                Vector2 m = (a + b) * .5f, d = (b - a).normalized, n = new Vector2(-d.y, d.x);
                tickLr[k].SetPosition(0, new Vector3(m.x - d.x * .07f + n.x * .16f, m.y - d.y * .07f + n.y * .16f, 0f));
                tickLr[k].SetPosition(1, new Vector3(m.x - d.x * .07f - n.x * .16f, m.y - d.y * .07f - n.y * .16f, 0f));
                tickLr[k].SetPosition(2, new Vector3(m.x + d.x * .07f - n.x * .16f, m.y + d.y * .07f - n.y * .16f, 0f));
                tickLr[k].SetPosition(3, new Vector3(m.x + d.x * .07f + n.x * .16f, m.y + d.y * .07f + n.y * .16f, 0f));
            }
            rightMarkLr.enabled = board.shape == Shape.Right;
            if (board.shape == Shape.Right)
            {
                Vector2 c = P[2], ua = (P[0] - c).normalized * .3f, ub = (P[1] - c).normalized * .3f;
                rightMarkLr.SetPosition(0, new Vector3(c.x + ua.x, c.y + ua.y, 0f));
                rightMarkLr.SetPosition(1, new Vector3(c.x + ua.x + ub.x, c.y + ua.y + ub.y, 0f));
                rightMarkLr.SetPosition(2, new Vector3(c.x + ub.x, c.y + ub.y, 0f));
            }
            for (int i = 0; i < 6; i++)
            {
                double mx, my; BangulRules.Mark(board, 3 + i, out mx, out my);
                var m = G2S(mx, my);
                marks[i].localPosition = new Vector3(m.x, m.y, -.02f);
                marks[i].gameObject.SetActive(board.strips && phase == Phase.Play);
            }
            if (rebuildFrame) frameGrow = 0f;
        }

        void FinishIntroVisuals()
        {
            frameGrow = 1f;
            frameKindBlend = frameKindTarget;
            if (waitingIdx >= 0) creatures[waitingIdx].localPosition = new Vector3(waitSpot.x, waitSpot.y, -.7f + WaterZ(waitSpot.y - .6f, .3f));
        }

        void AddFoldLine(LineKind lk, int which, int t1, int t2)
        {
            if (foldCount >= foldLr.Length) return;
            double px, py, dx, dy;
            BangulRules.FoldLine(board, lk, which, out px, out py, out dx, out dy);
            Vector2 p = G2S(px, py), d = new Vector2((float)dx, (float)dy);
            // 판 사각형으로 자른다
            float tMin = -100f, tMax = 100f;
            ClipAxis(p.x, d.x, -4.15f, 4.15f, ref tMin, ref tMax);
            ClipAxis(p.y, d.y, -3.45f, 3.45f, ref tMin, ref tMax);
            Vector2 a = p + d * tMin, b = p + d * tMax;
            var lr = foldLr[foldCount];
            lr.enabled = true;
            lr.SetPosition(0, new Vector3(a.x, a.y, 0f)); lr.SetPosition(1, new Vector3(b.x, b.y, 0f));
            lr.sharedMaterial.mainTextureScale = new Vector2(1f, 1f);
            lr.startColor = lr.endColor = lk == LineKind.PerpBisector ? Teal : MgfLook.Hex("3FA07E");
            foldGrow[foldCount] = 0f;
            foldLineEnds[foldCount * 2] = a; foldLineEnds[foldCount * 2 + 1] = b;
            var lab = foldLabel[foldCount];
            lab.gameObject.SetActive(true);
            lab.text = lk == LineKind.PerpBisector ? BangulRules.SideNames[which] + "의 수직이등분선" : "∠" + BangulRules.Names[which] + "의 이등분선";
            lab.color = lr.startColor;
            Vector2 lp = Vector2.Lerp(a, b, .86f) + new Vector2(-d.y, d.x) * .28f;
            lab.transform.localPosition = new Vector3(lp.x, lp.y, -.06f);
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; if (ang > 90f) ang -= 180f; if (ang < -90f) ang += 180f;
            lab.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            foldCount++;
            ribbonOn = false; ribbonLr.enabled = false;
            SpawnRingAtStage(p, 1.2f);
        }

        readonly Vector2[] foldLineEnds = new Vector2[12];

        static void ClipAxis(float p, float d, float lo, float hi, ref float tMin, ref float tMax)
        {
            if (Mathf.Abs(d) < 1e-5f) return;
            float t1 = (lo - p) / d, t2 = (hi - p) / d;
            if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }
            tMin = Mathf.Max(tMin, t1); tMax = Mathf.Min(tMax, t2);
        }

        // ───────────────────────────── 방울 판정 연출
        void BeginBubble(IP p, Verdict v)
        {
            bubbleC = G2S(p);
            bubbleOn = true;
            bubbleAnim = 0f;
            bubbleR = 0f;
            bubbleTargetR = ContactDistance(bubbleC, v.nearest);
            if (instantBubble) bubbleR = bubbleTargetR;
            squash = 0f;
            for (int i = 0; i < 3; i++) { contactTapped[i] = false; contactFlash[i] = 0f; }
            bubble.gameObject.SetActive(true); bubbleShadow.gameObject.SetActive(true);
            bubbleMat.color = Color.white; poppedThisBad = false;
            bubble.localPosition = new Vector3(bubbleC.x, bubbleC.y, -.28f);
            seed.gameObject.SetActive(false);
            centerLabel.text = "";
            Sfx.Play("grow", .3f);
        }

        float ContactDistance(Vector2 c, int i)
        {
            if (board.kind == Kind.Out) return Vector2.Distance(c, G2S(board.V[i]));
            Vector2 a = G2S(BangulRules.SideFrom(board, i)), b = G2S(BangulRules.SideTo(board, i));
            Vector2 d = (b - a).normalized;
            Vector2 w = c - a;
            return Mathf.Abs(w.x * d.y - w.y * d.x);
        }

        Vector2 ContactPoint(Vector2 c, int i)
        {
            if (board.kind == Kind.Out) return G2S(board.V[i]);
            Vector2 a = G2S(BangulRules.SideFrom(board, i)), b = G2S(BangulRules.SideTo(board, i));
            Vector2 d = (b - a).normalized;
            return a + d * Vector2.Dot(c - a, d);
        }

        void ClearBubble()
        {
            bubbleOn = false;
            bubble.gameObject.SetActive(false); bubbleShadow.gameObject.SetActive(false);
            for (int i = 0; i < 3; i++) { gapLr[i].enabled = false; radLr[i].enabled = false; perpLr[i].enabled = false; contactGlow[i].gameObject.SetActive(false); }
            centerLabel.text = "";
            ghostRing.gameObject.SetActive(false); ghostLabel.text = "";
        }

        void PopSeedAt(IP p)
        {
            HideDistance();
            var s = G2S(p);
            SpawnRingAtStage(s, .9f);
            ReturnSeed();
        }

        void ReturnSeed()
        {
            seedHeld = false;
            seedFlying = true; seedReturnT = 0f; seedFrom = seedPos;
            seed.gameObject.SetActive(true);
        }

        void StartSeedDrag()
        {
            seedHeld = true; seedFlying = false;
            seed.gameObject.SetActive(true);
            MoveSeedDrag(MgfPointer.Position);
            Sfx.Play("tap", .18f);
            SpawnRingAtStage(seedPos, .7f);
            luluTarget = seedPos + new Vector2(-1.2f, -.9f);
        }

        void MoveSeedDrag(Vector2 sp)
        {
            Vector2 s = ScreenToStage(sp + new Vector2(0f, TouchLift()));
            seedPos = s;
            idleT = 0f;
            if (board != null && board.bars && (phase == Phase.Practice || phase == Phase.Play)) ShowDistance(s);
        }

        void EndSeedDrag(bool placedOnBoard)
        {
            seedHeld = false;
            if (!placedOnBoard) HideDistance();
        }

        bool SeedBoardPoint(Vector2 sp, out IP p)
        {
            Vector2 s = ScreenToStage(sp + new Vector2(0f, TouchLift()));
            p = new IP(Mathf.RoundToInt(s.x * BangulRules.Sub), Mathf.RoundToInt(s.y * BangulRules.Sub));
            return BangulRules.OnBoard(p);
        }

        float TouchLift() => Input.touchCount > 0 ? 24f * Mathf.Max(1f, Screen.dpi > 0 ? Screen.dpi / 160f : 1f) : 0f;

        void ShowDistance(Vector2 s)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 c = ContactPoint(s, i);
                distLr[i].enabled = true;
                distLr[i].SetPosition(0, new Vector3(s.x, s.y, -.06f));
                distLr[i].SetPosition(1, new Vector3(c.x, c.y, -.06f));
                barValue[i] = Vector2.Distance(s, c);
            }
            barsVisible = true;
        }

        void HideDistance()
        {
            for (int i = 0; i < 3; i++) distLr[i].enabled = false;
            barsVisible = false;
        }

        readonly float[] barValue = new float[3];
        bool barsVisible;

        // ───────────────────────────── 시퀀스 시작 훅
        void OnSeqStart(Seq s)
        {
            switch (s)
            {
                case Seq.Demo:
                    demoBubble.gameObject.SetActive(false);
                    break;
                case Seq.Good:
                    HideDistance();
                    for (int i = 0; i < 3; i++) contactTapped[i] = false;
                    // 실제 접점 세 곳의 각(방울 중심 기준)으로 루루의 한 바퀴 일정을 잡는다
                    {
                        float baseAng = Mathf.Atan2(ContactPoint(bubbleC, 0).y - bubbleC.y, ContactPoint(bubbleC, 0).x - bubbleC.x);
                        orbitStart = baseAng - .5f;
                    }
                    ShowReveal();
                    break;
                case Seq.Bad:
                    HideDistance();
                    {
                        Vector2 cp = ContactPoint(bubbleC, lastVerdict.nearest);
                        squashDir = (cp - bubbleC).normalized;
                        if (squashDir.sqrMagnitude < .01f) squashDir = Vector2.down;
                    }
                    HeartBreakFx();
                    break;
                case Seq.Answer:
                    {
                        Vector2 c = board.kind == Kind.In ? G2S(board.Ix, board.Iy) : G2S(board.Ox, board.Oy);
                        float r = (float)((board.kind == Kind.In ? board.r : board.R) / BangulRules.Sub);
                        ghostRing.gameObject.SetActive(true);
                        ghostRing.localPosition = new Vector3(c.x, c.y, -.3f);
                        ghostBase = r * 2.22f;
                        ghostRing.localScale = new Vector3(ghostBase, ghostBase, 1f);
                        ghostLabel.text = board.kind == Kind.In ? "여기가 내심 I" : "여기가 외심 O";
                        ghostLabel.transform.localPosition = new Vector3(c.x, c.y, -.55f);
                        ShowToast(board.kind == Kind.In ? "세 내각의 이등분선의 교점 = 내심 I" : "세 변의 수직이등분선의 교점 = 외심 O", 2.2f);
                    }
                    break;
                case Seq.Intro:
                    frameGrow = 0f;
                    break;
                case Seq.Morph:
                    break;
            }
        }

        float orbitStart, ghostBase = 1f;

        void OnDemoDone()
        {
            demoBubble.gameObject.SetActive(false);
            ShowGuide();
        }

        void FlushDemo()
        {
            if (seq != Seq.Demo) return;
            seq = Seq.None; st.seq = "";
            OnDemoDone();
        }

        // ───────────────────────────── 프레임별 연출
        void UpdateVisuals(float dt)
        {
            worldT += dt;
            AnimateWorld(dt);
            AnimateBoard(dt);
            AnimateBubble(dt);
            AnimateSeed(dt);
            AnimateLuluPlan(dt);
            AnimateLulu(dt);
            AnimateCreatures(dt);
            UpdateUi(dt);
        }

        void AnimateWorld(float dt)
        {
            causticMat.mainTextureOffset = new Vector2(worldT * .021f, worldT * .013f);
            for (int i = 0; i < leaves.Length; i++)
                leaves[i].localRotation = Quaternion.Euler(0f, 0f, leafBase[i] + Mathf.Sin(worldT * (.9f + i * .13f) + i) * 5f);
            towel.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(worldT * .7f) * 2.2f);
            beam.localScale = new Vector3(30f, 4.4f * (1f + Mathf.Sin(worldT * .4f) * .06f), 1f);
            // 떠다니는 방울
            for (int i = 0; i < floaters.Length; i++)
            {
                var fs = floaterSeed[i];
                float y = Mathf.Repeat(fs.y + worldT * fs.z * .08f, 1f);
                float u = viewU0 + (viewU1 - viewU0) * Mathf.Repeat(fs.x + Mathf.Sin(worldT * .5f + i) * .01f, 1f);
                float v = viewV0 + (viewV1 - viewV0) * y;
                floaters[i].localPosition = new Vector3(u, v, -2.2f - (i % 3) * .3f);
                float sz = .16f + (i % 4) * .07f;
                floaters[i].localScale = new Vector3(sz, sz, 1f);
            }
            // 물방울: 수도에서 떨어져 수면에 고리를 만든다
            dripT += dt;
            Vector3 fp = faucet.localPosition;
            float fall = dripT * dripT * 6f;
            drip.localPosition = new Vector3(fp.x, fp.y - .75f - fall, fp.z - 1.1f);
            if (fp.y - .75f - fall < waterLineV || dripT > 2.6f)
            {
                if (dripT <= 2.6f) SpawnRingOnWater(new Vector2(fp.x, waterLineV - .3f), .8f);
                dripT = -1.2f;
            }
            drip.gameObject.SetActive(dripT > 0f);
            // 잔물결
            for (int i = 0; i < rings.Length; i++)
            {
                if (ringLife[i] <= 0f) continue;
                ringLife[i] -= dt;
                float k = 1f - Mathf.Clamp01(ringLife[i] / ringDur[i]);
                float s = ringSize[i] * (.3f + k * 1.2f);
                rings[i].localScale = new Vector3(s, s, 1f);
                var rc = ringCol[i]; rc.a *= 1f - k; ringMats[i].color = rc;
                if (ringLife[i] <= 0f) rings[i].gameObject.SetActive(false);
            }
            // 타이틀 제목: 김 속에서 드러난다
            if (phase == Phase.Title && bubbleOn && board != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 cp = ContactPoint(bubbleC, i);
                    contactGlow[i].gameObject.SetActive(true);
                    contactGlow[i].localPosition = new Vector3(cp.x, cp.y, -.45f);
                    float ph = Mathf.Repeat(worldT * .8f - i * .33f, 1f);
                    float s = .35f + .5f * Mathf.Exp(-ph * 6f);
                    contactGlow[i].localScale = new Vector3(s, s, 1f);
                }
            }
            if (phase == Phase.Title) logoReveal = Mathf.Min(1f, logoReveal + dt * .7f);
            else logoReveal = Mathf.Min(1f, logoReveal + dt * 2f);
            float lr = Mathf.SmoothStep(0f, 1f, logoReveal);
            logoMat.color = new Color(1f, 1f, 1f, lr);
            logoShadowMat.color = new Color(.10f, .32f, .42f, .32f * lr);
            float bob = Mathf.Sin(worldT * 1.1f) * .01f;
            logoQ.localPosition = new Vector3(0f, bob, -.19f);
        }

        readonly float[] leafBase = { 26f, -24f, 22f, -28f, 30f, -20f };
        readonly float[] ringDur = new float[8];
        readonly float[] ringSize = new float[8];

        void SpawnRingAtStage(Vector2 s, float size)
        {
            int i = ringCursor++ % rings.Length;
            rings[i].SetParent(stage, false);
            rings[i].localPosition = new Vector3(s.x, s.y, -.35f);
            rings[i].localRotation = Quaternion.identity;
            ringLife[i] = ringDur[i] = .55f; ringSize[i] = size; ringCol[i] = new Color(Teal.r, Teal.g, Teal.b, .85f);
            rings[i].gameObject.SetActive(true);
        }

        void SpawnRingOnWater(Vector2 s, float size)
        {
            int i = ringCursor++ % rings.Length;
            rings[i].SetParent(worldRoot, false);
            rings[i].position = OnWater(s.x, s.y, .06f);
            rings[i].rotation = Quaternion.Euler(90f, 0f, 0f);
            ringLife[i] = ringDur[i] = 1.1f; ringSize[i] = size; ringCol[i] = new Color(1f, 1f, 1f, .9f);
            rings[i].gameObject.SetActive(true);
        }

        void AnimateBoard(float dt)
        {
            bool showFrame = board != null;
            frameRoot.gameObject.SetActive(showFrame);
            overlayRoot.gameObject.SetActive(showFrame);
            if (!showFrame) return;
            if (seq == Seq.Intro) frameGrow = Mathf.Clamp01(seqT / seqDur);
            if (seq == Seq.Morph) frameKindBlend = Mathf.Clamp01(seqT / seqDur);
            float g = EaseOutBack(frameGrow);
            float wallK = 1f - frameKindBlend, nailK = frameKindBlend;
            for (int i = 0; i < 3; i++)
            {
                walls[i].gameObject.SetActive(wallK > .01f);
                walls[i].localScale = new Vector3(1f, 1f, Mathf.Max(.01f, g * wallK));
                nails[i].gameObject.SetActive(nailK > .01f);
                float ns = Mathf.Max(.01f, g * nailK);
                nails[i].localScale = new Vector3(ns, ns, ns);
            }
            outlineLr.enabled = board.kind == Kind.Out || frameKindBlend > .5f;
            float pulse = .5f + .5f * Mathf.Sin(worldT * 5f);
            bool stripMode = drag == Drag.StripEnd1 || drag == Drag.StripEnd2 || stripPinned >= 0;
            for (int i = 0; i < 6; i++)
            {
                if (!marks[i].gameObject.activeSelf) continue;
                float s = stripMode ? .24f + .06f * pulse : .2f;
                marks[i].localScale = new Vector3(s, s, .1f);
            }
            for (int i = 0; i < foldCount; i++)
            {
                if (foldGrow[i] >= 1f) continue;
                foldGrow[i] = Mathf.Min(1f, foldGrow[i] + dt * 2.8f);
                Vector2 a = foldLineEnds[i * 2], b = foldLineEnds[i * 2 + 1], m = (a + b) * .5f;
                float k = EaseOutCubic(foldGrow[i]);
                foldLr[i].SetPosition(0, new Vector3(Mathf.Lerp(m.x, a.x, k), Mathf.Lerp(m.y, a.y, k), 0f));
                foldLr[i].SetPosition(1, new Vector3(Mathf.Lerp(m.x, b.x, k), Mathf.Lerp(m.y, b.y, k), 0f));
            }
            // 접기 띠 리본
            if (drag == Drag.StripEnd1) { ribbonOn = true; ribbonA = stripRollStage; }
            else if (stripPinned >= 0 && drag != Drag.StripEnd2) { ribbonOn = true; ribbonA = TargetStage(stripPinned); ribbonB = ribbonA + (stripRollStage - ribbonA).normalized * .7f; }
            else if (drag == Drag.StripEnd2) { ribbonOn = true; ribbonA = TargetStage(stripPinned < 0 ? 0 : stripPinned); }
            if (ribbonRetract > 0f)
            {
                ribbonRetract -= dt;
                float k = Mathf.Clamp01(ribbonRetract / .3f);
                ribbonLr.enabled = true;
                Vector2 bb = Vector2.Lerp(stripRollStage, ribbonB, k);
                ribbonLr.SetPosition(0, StageToBoardLocal(stripRollStage, -.4f)); ribbonLr.SetPosition(1, StageToBoardLocal(bb, -.4f));
                if (ribbonRetract <= 0f) ribbonLr.enabled = false;
            }
            else if (ribbonOn && (drag == Drag.StripEnd1 || drag == Drag.StripEnd2 || stripPinned >= 0))
            {
                if (drag != Drag.None) ribbonB = ScreenToStage(MgfPointer.Position);
                ribbonLr.enabled = true;
                ribbonLr.SetPosition(0, StageToBoardLocal(ribbonA, -.4f));
                ribbonLr.SetPosition(1, StageToBoardLocal(ribbonB, -.4f));
            }
            else ribbonLr.enabled = false;
        }

        Vector3 StageToBoardLocal(Vector2 s, float z) => new Vector3(s.x, s.y, z);   // boardRoot 은 stage 원점에 있다

        Vector2 TargetStage(int t)
        {
            double x, y; BangulRules.Mark(board, t, out x, out y);
            return G2S(x, y);
        }

        void ReturnStrip()
        {
            ribbonRetract = .3f;
            stripPinned = -1;
        }

        void PinStrip(int t)
        {
            SpawnRingAtStage(TargetStage(t), .6f);
        }

        void StripFollow(Vector2 sp) { idleT = 0f; }

        void AnimateBubble(float dt)
        {
            if (seq == Seq.Demo) AnimateDemo();
            if (!bubbleOn) return;
            float pulse = Mathf.Sin(worldT * 7f) * .012f;
            if (seq == Seq.Inflate)
            {
                bubbleAnim = Mathf.Clamp01(seqT / .45f);
                bubbleR = bubbleTargetR * EaseOutBack(bubbleAnim);
            }
            else if (seq == Seq.Good)
            {
                float t = seqT;
                // 0.0~0.25: 가장 먼 벽까지 살짝 부풀어(탄성 막) 세 곳에 닿는다
                float far = ContactDistance(bubbleC, lastVerdict.farthest), near = ContactDistance(bubbleC, lastVerdict.nearest);
                float full = near + (far - near) * .6f;
                bubbleR = Mathf.Lerp(near, full, EaseOutCubic(Mathf.Clamp01(t / .25f)));
                // 루루 한 바퀴 동안 접점을 톡톡톡
                float orbitK = Mathf.Clamp01((t - .2f) / 1.15f);
                float ang = orbitStart + orbitK * Mathf.PI * 2f;
                for (int i = 0; i < 3; i++)
                {
                    if (contactTapped[i]) continue;
                    Vector2 cp = ContactPoint(bubbleC, i);
                    float ca = Mathf.Atan2(cp.y - bubbleC.y, cp.x - bubbleC.x);
                    float rel = Mathf.Repeat(ca - orbitStart, Mathf.PI * 2f);
                    if (orbitK * Mathf.PI * 2f >= rel && t > .2f)
                    {
                        contactTapped[i] = true; contactFlash[i] = 1f;
                        radLr[i].enabled = true;
                        radLr[i].SetPosition(0, new Vector3(bubbleC.x, bubbleC.y, -.4f));
                        radLr[i].SetPosition(1, new Vector3(cp.x, cp.y, -.4f));
                        if (board.kind == Kind.In) DrawPerp(i, cp);
                        contactGlow[i].gameObject.SetActive(true);
                        contactGlow[i].localPosition = new Vector3(cp.x, cp.y, -.45f);
                        Sfx.Play("tok", .3f);
                    }
                }
                if (t > 1.35f && centerLabel.text == "")
                {
                    centerLabel.text = board.kind == Kind.In ? "I" : "O";
                    centerLabel.transform.localPosition = new Vector3(bubbleC.x + .28f, bubbleC.y + .28f, -.5f);
                    MgfFx.Punch(centerLabel.transform, .4f, .3f);
                }
                // 1.7~2.5: 생물을 태우고 선반 홈으로 떠오른다
                if (t > 1.7f)
                {
                    float k = EaseInOut(Mathf.Clamp01((t - 1.7f) / .8f));
                    Vector2 target = phase == Phase.Play && roundIdx >= 0 ? NicheStage(roundIdx) : bubbleC + new Vector2(0f, 6f);
                    Vector2 c = Vector2.Lerp(bubbleC, target, k);
                    float r = Mathf.Lerp(full, .7f, k);
                    bubble.localPosition = new Vector3(c.x, c.y, -1.1f * k - .28f);
                    bubble.localScale = new Vector3(r * 2f, r * 2f, 1f);
                    bubbleShadow.gameObject.SetActive(false);
                    for (int i = 0; i < 3; i++) { radLr[i].enabled = k < .2f && radLr[i].enabled; perpLr[i].enabled = k < .2f && perpLr[i].enabled; contactGlow[i].gameObject.SetActive(false); }
                    if (k > .1f) centerLabel.text = "";
                    if (waitingIdx >= 0)
                    {
                        creatures[waitingIdx].localPosition = new Vector3(c.x, c.y, -1.2f * k - .4f);
                        creatures[waitingIdx].localScale = Vector3.one * Mathf.Lerp(1f, 1.1f, k);
                        rafts[waitingIdx].gameObject.SetActive(false);
                        creatureState[waitingIdx] = 2;
                    }
                    return;
                }
                else if (t > 1.45f && waitingIdx >= 0)
                {
                    // 생물이 방울로 뛰어오른다
                    float k = Mathf.Clamp01((t - 1.45f) / .25f);
                    Vector2 from = waitSpot, to = bubbleC;
                    Vector2 c = Vector2.Lerp(from, to, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
                    creatures[waitingIdx].localPosition = new Vector3(c.x, c.y, -.6f);
                    creatureState[waitingIdx] = 2;
                }
            }
            else if (seq == Seq.Bad)
            {
                float t = seqT;
                squash = Mathf.Lerp(squash, .22f, 1f - Mathf.Exp(-dt * 14f));
                for (int k = 0; k < 3; k++)
                {
                    if (k == lastVerdict.nearest) continue;
                    float g = Mathf.Clamp01((t - .2f) / .4f);
                    Vector2 cp = ContactPoint(bubbleC, k);
                    Vector2 dir = (cp - bubbleC).normalized;
                    Vector2 edge = bubbleC + dir * bubbleR;
                    gapLr[k].enabled = g > 0f;
                    gapLr[k].SetPosition(0, new Vector3(edge.x, edge.y, -.42f));
                    Vector2 end = Vector2.Lerp(edge, cp, EaseOutCubic(g));
                    gapLr[k].SetPosition(1, new Vector3(end.x, end.y, -.42f));
                }
                if (t > 1.1f)
                {
                    float k = Mathf.Clamp01((t - 1.1f) / .25f);
                    bubble.localScale = new Vector3(bubbleR * 2f * (1f + k * .35f), bubbleR * 2f * (1f + k * .35f), 1f);
                    bubbleMat.color = new Color(1f, 1f, 1f, 1f - k);
                    if (!poppedThisBad && k > .05f) { poppedThisBad = true; Sfx.Play("pop", .3f); SpawnRingAtStage(bubbleC, bubbleR * 2.2f); }
                    return;
                }
            }
            else if (seq == Seq.Answer)
            {
                float pul = ghostBase * (1f + Mathf.Sin(worldT * 8f) * .03f);
                ghostRing.localScale = new Vector3(pul, pul, 1f);
            }
            if (seq != Seq.Bad) { squash = Mathf.Lerp(squash, 0f, 1f - Mathf.Exp(-dt * 10f)); poppedThisBad = false; bubbleMat.color = Color.white; }
            // 눌림: 먼저 닿은 쪽으로 납작
            float ang2 = Mathf.Atan2(squashDir.y, squashDir.x) * Mathf.Rad2Deg;
            bubble.localRotation = Quaternion.Euler(0f, 0f, ang2);
            bubble.localPosition = new Vector3(bubbleC.x + squashDir.x * squash * bubbleR * .2f, bubbleC.y + squashDir.y * squash * bubbleR * .2f, -.28f);
            float d2 = bubbleR * 2f * (1f + pulse);
            bubble.localScale = new Vector3(Mathf.Max(.01f, d2 * (1f - squash)), Mathf.Max(.01f, d2 * (1f + squash * .4f)), 1f);
            bubbleShadow.localPosition = new Vector3(bubbleC.x + .12f, bubbleC.y - .18f, -.01f);
            bubbleShadow.localScale = new Vector3(d2 * 1.05f, d2 * 1.05f, 1f);
            for (int i = 0; i < 3; i++)
            {
                if (contactFlash[i] <= 0f) continue;
                contactFlash[i] = Mathf.Max(0f, contactFlash[i] - dt * 1.4f);
                float s = .45f + (1f - contactFlash[i]) * .5f;
                contactGlow[i].localScale = new Vector3(s, s, 1f);
            }
        }

        bool poppedThisBad, instantBubble;

        void DrawPerp(int i, Vector2 cp)
        {
            Vector2 a = G2S(BangulRules.SideFrom(board, i)), b = G2S(BangulRules.SideTo(board, i));
            Vector2 d = (b - a).normalized, n = (bubbleC - cp).normalized;
            if (Vector2.Dot(d, bubbleC - cp) < 0) { }
            float s = .2f;
            perpLr[i].enabled = true;
            perpLr[i].SetPosition(0, new Vector3(cp.x + d.x * s, cp.y + d.y * s, -.41f));
            perpLr[i].SetPosition(1, new Vector3(cp.x + d.x * s + n.x * s, cp.y + d.y * s + n.y * s, -.41f));
            perpLr[i].SetPosition(2, new Vector3(cp.x + n.x * s, cp.y + n.y * s, -.41f));
        }

        void AnimateDemo()
        {
            // 유령 손가락이 씨앗을 밑변 가까이에 놓아 → 방울이 작게 막히는 장면(정답 위치는 가리키지 않는다)
            Vector2 near = new Vector2(-.6f, -1.75f);
            float t = seqT;
            if (t < 1.0f)
            {
                float k = EaseInOut(t / 1f);
                Vector2 p = Vector2.Lerp(seedRest, near, k);
                SetGhostFinger(p, true);
                seedPos = p; seed.gameObject.SetActive(true);
            }
            else if (t < 2.4f)
            {
                SetGhostFinger(near, t < 1.2f);
                seed.gameObject.SetActive(false);
                demoBubble.gameObject.SetActive(true);
                float r0 = near.y - (-86f / BangulRules.Sub);   // 밑변까지
                float k = EaseOutBack(Mathf.Clamp01((t - 1f) / .4f));
                float r = r0 * k;
                demoBubble.localPosition = new Vector3(near.x, near.y, -.28f);
                float sq = t > 1.4f ? .16f : 0f;
                demoBubble.localScale = new Vector3(r * 2f * (1f + sq * .4f), r * 2f * (1f - sq), 1f);
                if (t > 1.4f && !demoBlocked) { demoBlocked = true; Sfx.Play("rub", .2f); ShowToast("밑변에 먼저 막혀 방울이 작다", 1.6f); }
            }
            else
            {
                SetGhostFinger(near, false);
                demoBubble.gameObject.SetActive(false);
                if (!demoPopped) { demoPopped = true; SpawnRingAtStage(near, .8f); Sfx.Play("pop", .2f); }
                ReturnSeedInstant();
            }
        }

        bool demoBlocked, demoPopped;

        void ReturnSeedInstant()
        {
            seedPos = seedRest; seedFlying = false; seedHeld = false;
            seed.gameObject.SetActive(phase != Phase.End);
        }

        void AnimateSeed(float dt)
        {
            if (phase == Phase.Title)
            {
                // 수도꼭지 끝에 매달려 흔들린다
                Vector3 f = faucet.localPosition;
                seedPos = new Vector2(f.x + Mathf.Sin(worldT * 1.6f) * .08f, f.y - 1.25f + Mathf.Sin(worldT * 2.3f) * .06f);
                seed.gameObject.SetActive(true);
            }
            else if (seedHeld) { }
            else if (seedFlying)
            {
                seedReturnT += dt * 3.2f;
                float k = EaseOutBack(Mathf.Clamp01(seedReturnT));
                seedPos = Vector2.LerpUnclamped(seedFrom, seedRest, k);
                if (seedReturnT >= 1f) seedFlying = false;
            }
            else if (seq != Seq.Demo && !bubbleOn) seedPos = seedRest + new Vector2(0f, Mathf.Sin(worldT * 2f) * .05f);
            if (phase == Phase.End) seed.gameObject.SetActive(false);
            else if (!bubbleOn && seq != Seq.Demo) seed.gameObject.SetActive(true);
            float z = seedHeld ? -1.4f : -.9f;
            seed.localPosition = new Vector3(seedPos.x, seedPos.y, z);
            float s = seedHeld ? .74f : .62f;
            s *= 1f + Mathf.Sin(worldT * 5f) * .03f;
            seed.localScale = new Vector3(s, s, 1f);
            seedCore.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(worldT * 1.7f) * 20f);
        }

        void AnimateLuluPlan(float dt)
        {
            luluIdling = false;
            if (seq == Seq.Good && bubbleOn)
            {
                float t = seqT;
                float orbitK = Mathf.Clamp01((t - .2f) / 1.15f);
                float ang = orbitStart + orbitK * Mathf.PI * 2f;
                float rr = bubbleR + .55f;
                luluTarget = bubbleC + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rr;
                if (t > 1.45f) luluTarget = bubble.localPosition + new Vector3(-.9f, -.5f, 0f);
                luluFlap = 1.6f;
                return;
            }
            if (seq == Seq.Bad && bubbleOn)
            {
                Vector2 cp = ContactPoint(bubbleC, lastVerdict.nearest);
                luluTarget = cp + (cp - bubbleC).normalized * .45f;
                luluSquash = Mathf.Max(luluSquash, seqT > .25f && seqT < .9f ? .8f : 0f);
                luluFlap = .6f;
                return;
            }
            luluFlap = Mathf.Lerp(luluFlap, 1f, 1f - Mathf.Exp(-dt * 2f));
            if (phase == Phase.Title && bubbleOn)
            {
                float oa = worldT * .9f;
                luluTarget = bubbleC + new Vector2(Mathf.Cos(oa), Mathf.Sin(oa)) * (bubbleR + .75f);
                return;
            }
            if (seedHeld) { luluTarget = seedPos + new Vector2(-1.1f, -.8f); return; }
            // 대기: 쉬는 자리 주변을 작게 8자로 너울댄다
            luluIdling = true;
            float a = worldT * .55f;
            luluTarget = luluIdle + new Vector2(Mathf.Sin(a) * luluIdleR.x, Mathf.Sin(a * 2f) * luluIdleR.y);
        }

        Vector2 luluIdle = new Vector2(-5.6f, -2.6f), luluIdleR = new Vector2(1.0f, .5f);
        bool luluIdling;

        void AnimateCreatures(float dt)
        {
            for (int i = 0; i < 6; i++)
            {
                if (!creatures[i].gameObject.activeSelf) continue;
                if (creatureState[i] == 1)
                {
                    // 거품 뗏목 위에서 동동
                    var p = creatures[i].localPosition;
                    var target = new Vector3(waitSpot.x, waitSpot.y + Mathf.Sin(worldT * 2.2f + i) * .07f, -.7f + WaterZ(waitSpot.y - .6f, .3f));
                    creatures[i].localPosition = Vector3.Lerp(p, target, 1f - Mathf.Exp(-dt * 5f));
                    float cs = land ? 1.65f : 1.5f;
                    creatures[i].localScale = Vector3.one * cs;
                    rafts[i].localPosition = new Vector3(creatures[i].localPosition.x, waitSpot.y - .62f * cs, -.5f + WaterZ(waitSpot.y - .62f * cs, .1f));
                    rafts[i].localScale = new Vector3(1.5f * cs, .55f * cs, 1f);
                }
                else if (creatureState[i] == 3)
                {
                    var n = niches[i].localPosition;
                    creatures[i].localPosition = new Vector3(n.x, n.y + .05f + Mathf.Sin(worldT * 1.6f + i) * .03f, n.z - .35f);
                    creatures[i].localScale = Vector3.one * 1.15f;
                }
            }
        }

        Vector2 NicheStage(int i) { var n = niches[i].localPosition; return new Vector2(n.x, n.y); }

        static float EaseOutBack(float x) { const float c1 = 1.70158f, c3 = c1 + 1f; x = Mathf.Clamp01(x); return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f); }
        static float EaseOutCubic(float x) { x = Mathf.Clamp01(x); return 1f - Mathf.Pow(1f - x, 3f); }
        static float EaseInOut(float x) { x = Mathf.Clamp01(x); return x < .5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f; }
    }
}
