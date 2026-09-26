// 원 찍어 v3 — 모눈 트레이싱 필름과 「선이 생길 때의 접기 연출」 (WonJjigeoGame 의 partial)
//
// v3(DESIGN-v3.md): 2단계의 조작은 탭이다(변 = 수직이등분선, 꼭짓점 = 각의 이등분선 — WonBuild.cs). 접기는 선이 생길 때
// 0.7초 연출로만 남는다: 필름 한쪽이 새 선을 축으로 들려 넘어가 A 를 B 에 포갰다가(수직이등분선) / 한 변을 다른 변에 포갰다가
// (각의 이등분선) 펴진다. 연출은 입력을 막지 않는다(새 선을 바로 그으면 앞 연출은 즉시 끝난다).
// 접기 기하는 실제 종이접기 그대로다: 점 P 를 Q 로 포개는 접는 선 = 선분 PQ 의 수직이등분선.
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
        LineRenderer filmEdge, triStatic, triFlap, foldGuide;
        Transform carryDot;
        Texture2D gridTex1, gridTex2;
        double pX, pY;                // 접기 연출에서 들려 옮겨지는 점(격자 좌표)
        Coroutine foldCo, clampCo;

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
            var dot = MgfLook.Prim(PrimitiveType.Quad, "CarryDot", Vector3.zero, Vector3.one * 0.55f, glowMat, world, false);
            dot.transform.rotation = Quaternion.Euler(90, 0, 0);
            dot.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            carryDot = dot.transform; dot.SetActive(false);
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
            ShowTrace(step == Step.Build && cur != null);
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

        // ─────────────────────────────── 선이 생길 때의 접기 연출(입력을 막지 않는다)
        /// <summary>perp=true: 변 idx 의 수직이등분선 — 꼭짓점 idx 쪽 필름이 들려 idx+1 에 포개졌다 펴진다.
        /// perp=false: ∠idx 의 이등분선 — 변 idx 쪽 필름이 들려 이웃한 변에 포개졌다 펴진다.</summary>
        void FoldFx(bool perp, int idx)
        {
            if (cur == null) return;
            if (foldCo != null) { StopCoroutine(foldCo); foldCo = null; FlatFilm(); }
            Vector2 L0, U, N;
            if (perp)
            {
                var a = V2w(cur.V[idx]); var b = V2w(cur.V[(idx + 1) % 3]);
                L0 = (a + b) * 0.5f; N = (a - b).normalized; U = new Vector2(-N.y, N.x);
                pX = cur.V[idx].x; pY = cur.V[idx].y;
            }
            else
            {
                var v = cur.V[idx]; var u = cur.V[(idx + 1) % 3] - v; var w = cur.V[(idx + 2) % 3] - v;
                var du = new Vector2(u.x, u.y).normalized; var dw = new Vector2(w.x, w.y).normalized;
                U = (du + dw).normalized; L0 = V2w(v);
                N = new Vector2(-U.y, U.x); if (Vector2.Dot(N, du) < 0) N = -N;   // 변 idx 쪽이 들린다
                float r = Mathf.Min(Mathf.Sqrt(u.Len2), Mathf.Sqrt(w.Len2)) * 0.6f;
                pX = v.x + du.x * r; pY = v.y + du.y * r;
            }
            fL0 = L0; fU = U; fN = N;
            foldCo = StartCoroutine(FoldFxCo());
        }

        IEnumerator FoldFxCo()
        {
            Play(clFold, 0.45f);
            for (float e = 0; e < 0.3f; e += Time.deltaTime) { float k = e / 0.3f; RenderFold(180f * (1 - (1 - k) * (1 - k))); yield return null; }
            RenderFold(180f);
            Play(clCrease, 0.6f);
            yield return new WaitForSeconds(0.08f);
            for (float e = 0; e < 0.32f; e += Time.deltaTime) { float k = e / 0.32f; RenderFold(180f * (1 - k * k * (3 - 2 * k))); yield return null; }
            FlatFilm();
            foldCo = null;
        }

        void StopFoldFx()
        {
            if (foldCo != null) { StopCoroutine(foldCo); foldCo = null; }
            FlatFilm();
        }

        /// <summary>2단계로 넘어갈 때: 집게가 튕겨 나가고 필름이 우윳빛 트레이싱지로 바뀐다. 다음 명판은 이미 떠 있다(빈 판 금지) — 입력도 막지 않는다.</summary>
        IEnumerator ClampPopCo()
        {
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
                }
                if (k > 0.45f && filmMat.mainTexture != gridTex2) { filmMat.mainTexture = gridTex2; camPush = 1f; MgfFx.Glow(new Vector3(0, FilmY + 0.1f, 0), new Color(1f, 0.97f, 0.9f), 14, 0.9f); }
                yield return null;
            }
            FinishClampPop();
        }

        void FinishClampPop()
        {
            if (clampCo != null) { StopCoroutine(clampCo); clampCo = null; }
            for (int i = 0; i < 4; i++) clamps[i].gameObject.SetActive(false);
            filmMat.mainTexture = gridTex2;
        }

        void ResetClamps()
        {
            if (clampCo != null) { StopCoroutine(clampCo); clampCo = null; }
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
