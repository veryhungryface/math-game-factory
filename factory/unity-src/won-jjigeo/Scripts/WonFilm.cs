// 원 찍어 v2 — 모눈 트레이싱 필름과 2단계 접기 (WonJjigeoGame 의 partial)
//
// 필름은 모눈이 인쇄된 반투명 시트다. 1단계에는 네 모서리 집게로 고정돼 있고(모눈 역할만), 2단계에서 집게가 튕겨 나가면 접을 수 있다.
// 접기는 실제 종이접기 기하 그대로다: 필름의 점 P 를 잡아 Q 로 옮기면 접는 선은 선분 PQ 의 수직이등분선이다.
//  - 꼭짓점 Vi 를 다른 꼭짓점 Vj 에 포개면 → 주름 = 변 ViVj 의 수직이등분선(Prob.PerpBisector, 정수 계수)
//  - 변을 이웃한 변에 포개면(두 변이 만나는 꼭짓점 Vk 에서 같은 거리의 점끼리) → 주름 = ∠Vk 의 이등분선(Prob.AngleBisector)
// 코드는 학생 대신 접지 않는다: 어떤 두 점/두 변을 포갤지는 학생이 끌어서 정하고, 포개는 순간의 정렬(스냅)만 돕는다.
// 주름을 낸 뒤에는 필름을 다시 펴고, 주름 위 한 점이 두 꼭짓점(두 변)까지 같은 거리임을 1초 보여 준다(주름의 성질).
using System.Collections;
using System.Collections.Generic;
using Mgf;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mgf.WonJjigeo
{
    public partial class WonJjigeoGame
    {
        Mesh filmMesh, flapMesh, shadowMesh;
        GameObject filmGo, flapGo, shadowGo;
        LineRenderer filmEdge, triStatic, triFlap, foldGuide, snapRing;
        Transform carryDot;
        Texture2D gridTex1, gridTex2;

        enum FoldKind { None, Vertex, Side }
        FoldKind pressFold;           // 누른 곳이 꼭짓점/변 손잡이였나
        int pressIdx;                 // 꼭짓점 i 또는 변 i
        bool folding;                 // 실제로 끌기 시작함
        double pX, pY;                // 잡은 점 P (격자 좌표)
        double qX, qY;                // 지금 P 가 가 있는 점 Q
        int snapJ = -1;               // 스냅된 꼭짓점 j 또는 변 j
        float foldAng;                // 0 = 편 상태, 180 = 완전히 접힘
        bool foldBusy;
        Coroutine foldCo;            // 접기 연출(되돌아가기·주름 새기기) — 명판이 바뀌거나 시간이 끝나면 멈춘다

        class Crease { public Line line; public bool perp; public int a, b; public LineRenderer dark, light; public string name; }
        readonly List<Crease> creases = new List<Crease>();
        readonly Crease[] creasePool = new Crease[Rules.MaxCreases];
        readonly List<RP> markers = new List<RP>();
        readonly LineRenderer[] markerRings = new LineRenderer[8];

        void BuildFilm()
        {
            gridTex1 = GridTex(0.035f); gridTex2 = GridTex(0.2f);
            filmMat.mainTexture = gridTex1;
            filmMesh = NewMesh("Film", filmMat, out filmGo);
            flapMesh = NewMesh("Flap", filmMat, out flapGo);
            shadowMesh = NewMesh("FlapShadow", shadowMat, out shadowGo);
            flapGo.SetActive(false); shadowGo.SetActive(false);
            var rect = new List<Vector2> { new Vector2(-FX, -FZ), new Vector2(FX, -FZ), new Vector2(FX, FZ), new Vector2(-FX, FZ) };
            BuildPoly(filmMesh, rect, false, 0, Color.white);
            filmEdge = Line("FilmEdge", 0.035f, 4, true);
            filmEdge.SetPosition(0, new Vector3(-FX, FilmY, -FZ)); filmEdge.SetPosition(1, new Vector3(FX, FilmY, -FZ));
            filmEdge.SetPosition(2, new Vector3(FX, FilmY, FZ)); filmEdge.SetPosition(3, new Vector3(-FX, FilmY, FZ));
            SetColor(filmEdge, new Color(1, 0.97f, 0.9f, 0.35f));
            filmEdge.gameObject.SetActive(true);
            triStatic = Line("TriStatic", 0.05f, 4, false);
            triFlap = Line("TriFlap", 0.05f, 4, false);
            foldGuide = Line("FoldGuide", 0.045f, 2, false, dashMat);
            snapRing = Line("SnapRing", 0.06f, 33, true);
            var dot = MgfLook.Prim(PrimitiveType.Quad, "CarryDot", Vector3.zero, Vector3.one * 0.55f, glowMat, world, false);
            dot.transform.rotation = Quaternion.Euler(90, 0, 0);
            dot.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            carryDot = dot.transform; dot.SetActive(false);
            for (int i = 0; i < Rules.MaxCreases; i++)
            {
                creasePool[i] = new Crease { dark = Line("CreaseDark" + i, 0.08f, 2, false), light = Line("CreaseLight" + i, 0.04f, 2, false) };
                SetColor(creasePool[i].dark, new Color(0.05f, 0.06f, 0.08f, 0.92f));
                SetColor(creasePool[i].light, new Color(1f, 0.98f, 0.92f, 0.9f));
            }
            for (int i = 0; i < markerRings.Length; i++) markerRings[i] = Line("Marker" + i, 0.055f, 25, true);
        }

        Mesh NewMesh(string name, Material mat, out GameObject go)
        {
            go = new GameObject(name);
            go.transform.SetParent(world, false);
            var m = new Mesh { name = name };
            m.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            return m;
        }

        /// <summary>모눈 텍스처: 5×5 칸 = 1 타일(칸당 48px). 바탕 흰색(알파 baseA), 칸 경계 먹선, 5칸마다 굵은 선.</summary>
        static Texture2D GridTex(float baseA)
        {
            const int C = 48, S = C * 5;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Grid", wrapMode = TextureWrapMode.Repeat, anisoLevel = MgfBridge.LowGfx ? 0 : 4, filterMode = MgfBridge.LowGfx ? FilterMode.Bilinear : FilterMode.Trilinear };
            var px = new Color32[S * S];
            var ink = new Color32(26, 36, 46, 0); var paper = new Color32(255, 252, 244, 0);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int dx = Mathf.Min(x % C, C - 1 - x % C), dy = Mathf.Min(y % C, C - 1 - y % C);
                    bool majX = x < 2 || x >= S - 2, majY = y < 2 || y >= S - 2;
                    var c = paper; float a = baseA;
                    if (dx == 0 || dy == 0) { c = ink; a = 0.5f; }
                    if ((majX && dx <= 1) || (majY && dy <= 1)) { c = ink; a = 0.72f; }
                    c.a = (byte)(a * 255);
                    px[y * S + x] = c;
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        readonly List<Vector3> fv = new List<Vector3>(16);
        readonly List<Vector2> fu = new List<Vector2>(16);
        readonly List<Color> fc = new List<Color>(16);
        readonly List<int> ft = new List<int>(48);

        /// <summary>볼록 다각형(월드 xz)을 필름 메시로. rotate 면 접는 선 둘레로 ang 만큼 들어 올린다. UV 는 접기 전 월드 좌표(모눈이 필름을 따라간다).</summary>
        void BuildPoly(Mesh m, List<Vector2> poly, bool rotate, float ang, Color col, bool shadow = false)
        {
            fv.Clear(); fu.Clear(); fc.Clear(); ft.Clear();
            float tile = 5 * CELL;
            for (int i = 0; i < poly.Count; i++)
            {
                var p = poly[i];
                var w = rotate ? FoldPoint(p, ang) : new Vector3(p.x, FilmY, p.y);
                if (shadow)
                {
                    float h = w.y - FilmY;
                    fv.Add(new Vector3(w.x + h * 0.32f, FilmY + 0.004f, w.z - h * 0.42f));
                    fc.Add(new Color(0.05f, 0.03f, 0.02f, 0.34f * Mathf.Clamp01(h * 2.5f)));
                }
                else { fv.Add(w); fc.Add(col); }
                fu.Add(new Vector2(p.x / tile, p.y / tile));
            }
            for (int i = 1; i + 1 < poly.Count; i++) { ft.Add(0); ft.Add(i); ft.Add(i + 1); }
            m.Clear();
            m.SetVertices(fv); m.SetUVs(0, fu); m.SetColors(fc); m.SetTriangles(ft, 0);
            m.RecalculateBounds();
        }

        // 접는 선(월드 xz): 점 L0, 단위 방향 U, 들리는 쪽 법선 N(플랩 쪽을 가리킴)
        Vector2 fL0, fU, fN;

        Vector3 FoldPoint(Vector2 p, float ang)
        {
            var w = p - fL0;
            float along = Vector2.Dot(w, fU), perp = Vector2.Dot(w, fN);   // perp ≥ 0 (플랩 쪽)
            float a = ang * Mathf.Deg2Rad;
            var xz = fL0 + fU * along + fN * (perp * Mathf.Cos(a));
            float y = FilmY + perp * Mathf.Sin(a) + 0.014f * Mathf.Clamp01(ang / 90f);
            return new Vector3(xz.x, y, xz.y);
        }

        readonly List<Vector2> clipA = new List<Vector2>(8), clipB = new List<Vector2>(8), rectPoly = new List<Vector2>(4);

        /// <summary>다각형을 반평면 N·(x−L0) ≥ 0 (keep=+1) 또는 ≤ 0 (keep=−1) 으로 자른다.</summary>
        void ClipPoly(List<Vector2> src, List<Vector2> dst, int keep)
        {
            dst.Clear();
            for (int i = 0; i < src.Count; i++)
            {
                var a = src[i]; var b = src[(i + 1) % src.Count];
                float sa = keep * Vector2.Dot(a - fL0, fN), sb = keep * Vector2.Dot(b - fL0, fN);
                if (sa >= 0) dst.Add(a);
                if ((sa >= 0) != (sb >= 0)) dst.Add(a + (b - a) * (sa / (sa - sb)));
            }
        }

        /// <summary>필름 전체를 접는 선 기준으로 두 쪽(고정/플랩)으로 나눠 그린다. 삼각형 트레이스와 잡은 점 표시도 같이 옮긴다.</summary>
        void RenderFold(float ang)
        {
            rectPoly.Clear();
            rectPoly.Add(new Vector2(-FX, -FZ)); rectPoly.Add(new Vector2(FX, -FZ)); rectPoly.Add(new Vector2(FX, FZ)); rectPoly.Add(new Vector2(-FX, FZ));
            ClipPoly(rectPoly, clipA, -1);
            BuildPoly(filmMesh, clipA, false, 0, Color.white);
            ClipPoly(rectPoly, clipB, +1);
            float s = Mathf.Sin(ang * Mathf.Deg2Rad);
            float k = Mathf.Lerp(1f, 0.8f, s);
            BuildPoly(flapMesh, clipB, true, ang, new Color(k, k * 0.99f, k * 0.97f, 1f + 0.6f * s));
            BuildPoly(shadowMesh, clipB, true, ang, Color.black, true);
            flapGo.SetActive(clipB.Count >= 3); shadowGo.SetActive(clipB.Count >= 3);
            // 트레이스한 삼각형: 고정 쪽 사슬 + 플랩 쪽 사슬(회전)
            TriChain(triStatic, -1, false, ang);
            TriChain(triFlap, +1, true, ang);
            // 잡은 점(꼭짓점 또는 변 위의 점)이 필름과 함께 옮겨 가는 것
            carryDot.gameObject.SetActive(true);
            carryDot.position = FoldPoint(new Vector2((float)pX * CELL, (float)pY * CELL), ang) + Vector3.up * 0.02f;
            // 접는 선 안내(점선)
            foldGuide.gameObject.SetActive(ang > 1f);
            if (foldGuide.gameObject.activeSelf)
            {
                ClipLineToFilm(fL0, fU, out var e0, out var e1);
                foldGuide.SetPosition(0, new Vector3(e0.x, FilmY + 0.01f, e0.y)); foldGuide.SetPosition(1, new Vector3(e1.x, FilmY + 0.01f, e1.y));
                SetColor(foldGuide, new Color(1f, 0.97f, 0.88f, 0.55f));
            }
        }

        readonly List<Vector2> chain = new List<Vector2>(6);
        void TriChain(LineRenderer lr, int keep, bool rotate, float ang)
        {
            chain.Clear();
            var V = cur.V; int start = -1;
            for (int i = 0; i < 3; i++) if (keep * Vector2.Dot(V2w(V[i]) - fL0, fN) < 0) { start = i; break; }
            bool loop = false;
            if (start < 0) { for (int i = 0; i < 3; i++) chain.Add(V2w(V[i])); loop = true; }
            else
            {
                // 잘린 바깥 꼭짓점에서 출발해 한 바퀴: 안으로 들어가는 교점 → 안쪽 꼭짓점들 → 나가는 교점
                for (int e = 0; e < 3; e++)
                {
                    var a = V2w(V[(start + e) % 3]); var b = V2w(V[(start + e + 1) % 3]);
                    float sa = keep * Vector2.Dot(a - fL0, fN), sb = keep * Vector2.Dot(b - fL0, fN);
                    if (sa >= 0 && e > 0) chain.Add(a);
                    if ((sa >= 0) != (sb >= 0)) chain.Add(a + (b - a) * (sa / (sa - sb)));
                }
            }
            lr.loop = loop;
            if (chain.Count < 2) { lr.gameObject.SetActive(false); return; }
            lr.positionCount = chain.Count;
            for (int i = 0; i < chain.Count; i++) lr.SetPosition(i, rotate ? FoldPoint(chain[i], ang) + Vector3.up * 0.012f : new Vector3(chain[i].x, FilmY + 0.012f, chain[i].y));
            lr.gameObject.SetActive(true);
        }

        static Vector2 V2w(IP p) => new Vector2(p.x * CELL, p.y * CELL);

        /// <summary>편 필름(접기 없음)으로 되돌린다.</summary>
        void FlatFilm()
        {
            rectPoly.Clear();
            rectPoly.Add(new Vector2(-FX, -FZ)); rectPoly.Add(new Vector2(FX, -FZ)); rectPoly.Add(new Vector2(FX, FZ)); rectPoly.Add(new Vector2(-FX, FZ));
            BuildPoly(filmMesh, rectPoly, false, 0, Color.white);
            flapGo.SetActive(false); shadowGo.SetActive(false); carryDot.gameObject.SetActive(false); foldGuide.gameObject.SetActive(false);
            triFlap.gameObject.SetActive(false);
            ShowTrace(st.stage == 2 && cur != null);
        }

        void ShowTrace(bool on)
        {
            triStatic.gameObject.SetActive(on);
            if (!on) return;
            triStatic.loop = true; triStatic.positionCount = 3;
            for (int i = 0; i < 3; i++) triStatic.SetPosition(i, IW(cur.V[i], FilmY + 0.012f));
            SetColor(triStatic, new Color(0.1f, 0.13f, 0.17f, 0.9f)); SetColor(triFlap, new Color(0.1f, 0.13f, 0.17f, 0.9f));
        }

        void ClipLineToFilm(Vector2 p0, Vector2 u, out Vector2 e0, out Vector2 e1)
        {
            float t0 = -1e5f, t1 = 1e5f;
            Clip1(p0.x, u.x, -FX, FX, ref t0, ref t1);
            Clip1(p0.y, u.y, -FZ, FZ, ref t0, ref t1);
            e0 = p0 + u * t0; e1 = p0 + u * t1;
        }
        static void Clip1(float p, float d, float lo, float hi, ref float t0, ref float t1)
        {
            if (Mathf.Abs(d) < 1e-6f) return;
            float a = (lo - p) / d, b = (hi - p) / d;
            if (a > b) { var t = a; a = b; b = t; }
            t0 = Mathf.Max(t0, a); t1 = Mathf.Min(t1, b);
        }

        // ─────────────────────────────── 접기 입력
        /// <summary>누른 점(격자 좌표)이 꼭짓점 손잡이·변 손잡이인가.</summary>
        FoldKind FoldHandleAt(double gx, double gy, out int idx, out double px, out double py)
        {
            idx = -1; px = gx; py = gy;
            if (st.stage != 2 || cur == null || creases.Count >= Rules.MaxCreases) return FoldKind.None;
            double best = 1.25;   // 꼭짓점 반경 1.25칸
            for (int i = 0; i < 3; i++)
            {
                double d = Dist(gx, gy, cur.V[i].x, cur.V[i].y);
                if (d < best) { best = d; idx = i; }
            }
            if (idx >= 0) { px = cur.V[idx].x; py = cur.V[idx].y; return FoldKind.Vertex; }
            best = 0.7;           // 변 반경 0.7칸(양 끝 꼭짓점 근처는 제외)
            for (int i = 0; i < 3; i++)
            {
                cur.Foot(gx, gy, i, out double fx, out double fy, out double t);
                if (t < 0.12 || t > 0.88) continue;
                double d = Dist(gx, gy, fx, fy);
                if (d < best) { best = d; idx = i; px = fx; py = fy; }
            }
            return idx >= 0 ? FoldKind.Side : FoldKind.None;
        }

        static double Dist(double ax, double ay, double bx, double by) { double dx = ax - bx, dy = ay - by; return System.Math.Sqrt(dx * dx + dy * dy); }

        /// <summary>끌고 있는 동안: 스냅 대상을 찾고, 접는 선 = PQ 의 수직이등분선으로 필름을 들어 보인다.</summary>
        void UpdateFoldDrag(double gx, double gy)
        {
            qX = gx; qY = gy; snapJ = -1;
            if (pressFold == FoldKind.Vertex)
            {
                double best = 1.4;
                for (int j = 0; j < 3; j++)
                {
                    if (j == pressIdx) continue;
                    double d = Dist(gx, gy, cur.V[j].x, cur.V[j].y);
                    if (d < best) { best = d; snapJ = j; }
                }
                if (snapJ >= 0) { qX = cur.V[snapJ].x; qY = cur.V[snapJ].y; }
            }
            else
            {
                // 이웃한 변 위로 가져가면, 두 변이 만나는 꼭짓점에서 같은 거리인 점으로 맞춘다(= 각의 이등분선으로 접힌다)
                double best = 1.1;
                for (int j = 0; j < 3; j++)
                {
                    if (j == pressIdx) continue;
                    cur.Foot(gx, gy, j, out double fx, out double fy, out double t);
                    if (t < -0.05 || t > 1.05) continue;
                    double d = Dist(gx, gy, fx, fy);
                    if (d < best) { best = d; snapJ = j; }
                }
                if (snapJ >= 0)
                {
                    int k = SharedVertex(pressIdx, snapJ);
                    var vk = cur.V[k]; var other = cur.V[OtherEnd(snapJ, k)];
                    double r = Dist(pX, pY, vk.x, vk.y), L = Dist(other.x, other.y, vk.x, vk.y);
                    qX = vk.x + (other.x - vk.x) * r / L; qY = vk.y + (other.y - vk.y) * r / L;
                }
            }
            SetFoldLine();
            foldAng = Mathf.MoveTowards(foldAng, snapJ >= 0 ? 176f : 160f, Time.deltaTime * 900f);
            RenderFold(foldAng);
            // 스냅 대상 표시
            snapRing.gameObject.SetActive(snapJ >= 0);
            if (snapJ >= 0)
            {
                Vector3 c = pressFold == FoldKind.Vertex ? IW(cur.V[snapJ], FilmY + 0.03f) : GW(qX, qY, FilmY + 0.03f);
                CirclePts(snapRing, c, 0.32f + Mathf.Sin(Time.time * 10f) * 0.04f, 33);
                SetColor(snapRing, new Color(1f, 0.93f, 0.6f, 0.95f));
            }
        }

        void SetFoldLine()
        {
            var P = new Vector2((float)pX * CELL, (float)pY * CELL); var Q = new Vector2((float)qX * CELL, (float)qY * CELL);
            var d = Q - P;
            if (d.sqrMagnitude < 1e-6f) d = new Vector2(0, 0.001f);
            fL0 = (P + Q) * 0.5f;
            var n = d.normalized;            // PQ 방향
            fU = new Vector2(-n.y, n.x);     // 접는 선 방향
            fN = -n;                         // P 쪽(플랩) 법선
        }

        int SharedVertex(int s1, int s2)
        {
            // 변 i = Vi→V(i+1). 두 변의 공통 꼭짓점
            for (int k = 0; k < 3; k++)
            {
                bool in1 = k == s1 || k == (s1 + 1) % 3, in2 = k == s2 || k == (s2 + 1) % 3;
                if (in1 && in2) return k;
            }
            return 0;
        }
        static int OtherEnd(int side, int k) => side == k ? (side + 1) % 3 : side;

        /// <summary>손을 뗌: 스냅돼 있으면 주름을 새기고, 아니면 필름이 되돌아간다.</summary>
        void EndFoldDrag()
        {
            snapRing.gameObject.SetActive(false);
            if (snapJ < 0)
            {
                foldCo = StartCoroutine(SpringBack());
                Refuse(null, pressFold == FoldKind.Vertex ? "꼭짓점은 다른 꼭짓점에 포개어 접어라" : "변은 이웃한 다른 변에 포개어 접어라");
                return;
            }
            Crease c;
            if (pressFold == FoldKind.Vertex)
            {
                int i = Mathf.Min(pressIdx, snapJ), j = Mathf.Max(pressIdx, snapJ);
                foreach (var e in creases) if (e.perp && e.a == i && e.b == j) { foldCo = StartCoroutine(SpringBack()); Refuse(null, "이미 접은 주름이다"); return; }
                c = creasePool[creases.Count];
                c.perp = true; c.a = i; c.b = j; c.line = cur.PerpBisector(i, j);
                c.name = $"{Geo.Names[i]}{Geo.Names[j]}의 수직이등분선";
            }
            else
            {
                int k = SharedVertex(pressIdx, snapJ);
                foreach (var e in creases) if (!e.perp && e.a == k) { foldCo = StartCoroutine(SpringBack()); Refuse(null, "이미 접은 주름이다"); return; }
                c = creasePool[creases.Count];
                c.perp = false; c.a = k; c.b = -1; c.line = cur.AngleBisector(k);
                c.name = $"∠{Geo.Names[k]}의 이등분선";
            }
            foldCo = StartCoroutine(CommitFold(c));
        }

        IEnumerator SpringBack()
        {
            foldBusy = true;
            float a0 = foldAng;
            for (float e = 0; e < 0.28f; e += Time.deltaTime)
            {
                float k = e / 0.28f;
                foldAng = Mathf.Lerp(a0, 0, 1 - (1 - k) * (1 - k));
                RenderFold(foldAng);
                yield return null;
            }
            foldAng = 0; FlatFilm(); foldBusy = false; folding = false; foldCo = null;
        }

        IEnumerator CommitFold(Crease c)
        {
            foldBusy = true; folding = false;
            Play(clFold, 0.6f);
            // 완전히 포개기(오버슈트 없이 납작하게) → 주름 누르기 → 펴기
            float a0 = foldAng;
            for (float e = 0; e < 0.16f; e += Time.deltaTime) { foldAng = Mathf.Lerp(a0, 180f, e / 0.16f); RenderFold(foldAng); yield return null; }
            foldAng = 180f; RenderFold(180f);
            Play(clCrease, 0.8f);
            MgfFx.Glow(GW(qX, qY, FilmY + 0.05f), new Color(1f, 0.95f, 0.8f), 5, 0.3f);
            camPush = 1f;
            yield return new WaitForSeconds(0.22f);
            AddCrease(c);
            for (float e = 0; e < 0.42f; e += Time.deltaTime)
            {
                float k = e / 0.42f;
                foldAng = 180f * (1 - k * k * (3 - 2 * k));
                RenderFold(foldAng);
                yield return null;
            }
            foldAng = 0; FlatFilm();
            foldBusy = false; foldCo = null;
            StartCoroutine(TracerDemo(c));
            RefreshFoldGauge();
            MgfBridge.NotifyChanged();
        }

        /// <summary>주름을 새기고 교점 표식을 갱신한다(정수 교점).</summary>
        void AddCrease(Crease c)
        {
            creases.Add(c);
            st.creases = creases.Count;
            DrawCrease(c);
            if (c.perp) AddMarker(cur.SideMid(c.a, c.b));     // 꼭짓점끼리 포개면 주름이 그 변의 중점을 지난다
            for (int i = 0; i < creases.Count - 1; i++)
                if (Prob.Meet(creases[i].line, c.line, out var p) && System.Math.Abs(p.fx) <= Rules.GX + 0.5 && System.Math.Abs(p.fy) <= Rules.GY + 0.5)
                    AddMarker(p);
            // 세 주름이 한 점에서 만나는가(보너스) — 정수 판정
            if (creases.Count == 3 && Prob.Meet(creases[0].line, creases[1].line, out var q) && creases[2].line.Through(q))
            {
                concurrentBonus = true;
                ShowBanner(creases[0].perp && creases[1].perp && creases[2].perp ? "세 수직이등분선이 한 점에서 만난다 +50" :
                           !creases[0].perp && !creases[1].perp && !creases[2].perp ? "세 각의 이등분선이 한 점에서 만난다 +50" : "세 주름이 한 점에서 만난다 +50", 2.2f);
                Play(clMatch, 0.6f);
                MgfFx.Glow(PW(q, FilmY + 0.1f), new Color(1f, 0.9f, 0.6f), 10, 0.45f);
            }
        }

        void DrawCrease(Crease c)
        {
            // 직선 a·x+b·y=c (격자 좌표) → 필름 안 선분(월드)
            double a = c.line.fa, b = c.line.fb, cc = c.line.fc, n2 = a * a + b * b;
            var p0 = new Vector2((float)(a * cc / n2) * CELL, (float)(b * cc / n2) * CELL);
            var u = new Vector2((float)-b, (float)a).normalized;
            ClipLineToFilm(p0, u, out var e0, out var e1);
            var nrm = new Vector2(-u.y, u.x) * 0.028f;
            c.dark.SetPosition(0, new Vector3(e0.x, FilmY + 0.008f, e0.y)); c.dark.SetPosition(1, new Vector3(e1.x, FilmY + 0.008f, e1.y));
            c.light.SetPosition(0, new Vector3(e0.x + nrm.x, FilmY + 0.01f, e0.y + nrm.y)); c.light.SetPosition(1, new Vector3(e1.x + nrm.x, FilmY + 0.01f, e1.y + nrm.y));
            c.dark.gameObject.SetActive(true); c.light.gameObject.SetActive(true);
        }

        void AddMarker(RP p)
        {
            foreach (var m in markers) if (m.SameAs(p)) return;
            if (markers.Count >= markerRings.Length) return;
            markers.Add(p);
            var r = markerRings[markers.Count - 1];
            CirclePts(r, PW(p, FilmY + 0.02f), 0.2f, 25);
            SetColor(r, new Color(0.1f, 0.12f, 0.16f, 0.95f));
            r.gameObject.SetActive(true);
            MgfFx.Punch(r.transform, 0.3f, 0.25f);
        }

        void ClearFolds()
        {
            if (foldCo != null) { StopCoroutine(foldCo); foldCo = null; }
            tracerOn = false;
            foreach (var c in creasePool) { c.dark.gameObject.SetActive(false); c.light.gameObject.SetActive(false); }
            creases.Clear(); markers.Clear(); st.creases = 0; concurrentBonus = false;
            foreach (var r in markerRings) r.gameObject.SetActive(false);
            folding = false; foldBusy = false; foldAng = 0; pressFold = FoldKind.None;
            snapRing.gameObject.SetActive(false);
            FlatFilm();
        }

        /// <summary>주름의 성질을 1초 보여 준다: 주름 위를 미끄러지는 점에서 두 꼭짓점(두 변)까지 같은 길이의 선.
        /// (어느 종류의 주름이 이번 미션에 맞는지는 말하지 않는다 — 주름이 무엇을 같게 하는지만 보인다.)</summary>
        IEnumerator TracerDemo(Crease c)
        {
            tracerOn = true;
            // 주름이 삼각형을 지나는 구간에서 점을 미끄러뜨린다
            double a = c.line.fa, b = c.line.fb, cc = c.line.fc, n2 = a * a + b * b;
            double px0 = a * cc / n2, py0 = b * cc / n2, ux = -b / System.Math.Sqrt(n2), uy = a / System.Math.Sqrt(n2);
            var ctr = new Vector2((cur.V[0].x + cur.V[1].x + cur.V[2].x) / 3f, (cur.V[0].y + cur.V[1].y + cur.V[2].y) / 3f);
            double t0 = (ctr.x - px0) * ux + (ctr.y - py0) * uy;
            for (float e = 0; e < 1.1f && tracerOn; e += Time.deltaTime)
            {
                double t = t0 + System.Math.Sin(e / 1.1f * System.Math.PI * 2) * 1.6;
                double x = px0 + ux * t, y = py0 + uy * t;
                var P = GW(x, y, LineY);
                for (int k = 0; k < 2; k++)
                {
                    Vector3 end;
                    if (c.perp) end = IW(cur.V[k == 0 ? c.a : c.b], LineY);
                    else
                    {
                        int side = k == 0 ? c.a : (c.a + 2) % 3;     // 꼭짓점 a 에 모인 두 변
                        cur.Foot(x, y, side, out double fx, out double fy, out _);
                        end = GW(fx, fy, LineY);
                    }
                    gl[k].SetPosition(0, P); gl[k].SetPosition(1, end);
                    SetColor(gl[k], new Color(Cream.r, Cream.g, Cream.b, 0.95f));
                    gl[k].gameObject.SetActive(true);
                    TickAt(tk[k], P, end, 1, Cream);
                }
                yield return null;
            }
            for (int k = 0; k < 2; k++) { gl[k].gameObject.SetActive(false); tk[k].gameObject.SetActive(false); }
            tracerOn = false;
        }
        bool tracerOn, concurrentBonus;

        /// <summary>핀을 옮길 때 표식(주름 교점·변의 중점) 0.6칸 안이면 거기에 맞춘다.</summary>
        RP SnapTarget(double gx, double gy)
        {
            if (st.stage == 2)
            {
                RP best = default; double bd = 0.6; bool found = false;
                foreach (var m in markers) { double d = Dist(gx, gy, m.fx, m.fy); if (d < bd) { bd = d; best = m; found = true; } }
                if (found) return best;
            }
            int x = Mathf.Clamp(Mathf.RoundToInt((float)gx), -Rules.GX, Rules.GX), y = Mathf.Clamp(Mathf.RoundToInt((float)gy), -Rules.GY, Rules.GY);
            return RP.Of(new IP(x, y));
        }

        /// <summary>2단계 해금 연출: 집게가 튕겨 나가고 필름이 트레이싱지로 바뀐다.</summary>
        IEnumerator UnlockCo()
        {
            busy = true;
            HideMeasure(); HideMarks();
            st.red = false;
            badgeBig.text = "접기"; badgeSub.text = "필름 집게가 풀린다";
            MgfFx.Punch(badgeRt, 0.2f, 0.35f);
            lastGaugeKey = "unlock";
            gaugeHead.text = "2단계: 거리선 대신 필름을 접는다 · 주름 3개까지";
            for (int i = 0; i < 3; i++) { gName[i].text = $"주름 {i + 1}"; gVal[i].text = "·"; gVal[i].color = new Color(1, 1, 1, 0.3f); gBar[i].color = new Color(1, 1, 1, 0.12f); }
            ShowBanner("2단계 해금 · 이제 거리선은 없다. 필름을 접어 찾아라", 0);
            Play(clUnlock, 0.7f);
            var from = new Vector3[4];
            for (int i = 0; i < 4; i++) from[i] = clamps[i].position;
            for (float e = 0; e < 0.9f; e += Time.deltaTime)
            {
                float k = e / 0.9f;
                for (int i = 0; i < 4; i++)
                {
                    float d = Mathf.Clamp01(k * 1.6f - i * 0.12f);
                    var dir = new Vector3(Mathf.Sign(from[i].x), 0, Mathf.Sign(from[i].z));
                    clamps[i].position = from[i] + dir * d * 3.5f + Vector3.up * Mathf.Sin(d * Mathf.PI) * 2.2f;
                    clamps[i].rotation = Quaternion.Euler(d * 540f, 0, d * 200f);
                    if (d > 0.02f && d < 0.06f) Play(clSeat, 0.4f);
                }
                if (k > 0.45f && filmMat.mainTexture != gridTex2) { filmMat.mainTexture = gridTex2; camPush = 1f; MgfFx.Glow(new Vector3(0, FilmY + 0.1f, 0), new Color(1f, 0.97f, 0.9f), 14, 0.9f); }
                yield return null;
            }
            for (int i = 0; i < 4; i++) clamps[i].gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);
            busy = false;
        }

        void ResetClamps()
        {
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? -1 : 1, sz = i < 2 ? -1 : 1;
                clamps[i].gameObject.SetActive(true);
                clamps[i].position = new Vector3(sx * (FX - 0.12f), FilmY + 0.06f, sz * (FZ - 0.12f));
                clamps[i].rotation = Quaternion.identity;
            }
            filmMat.mainTexture = gridTex1;
        }
    }
}
