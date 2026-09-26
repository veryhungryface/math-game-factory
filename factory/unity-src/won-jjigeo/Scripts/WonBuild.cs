// 원 찍어 v3 — 2단계 「작도로 찾기」 (WonJjigeoGame 의 partial)
//
// DESIGN-v3.md: 거리선 없이, 학생이 어떤 작도를 고를지가 수학 판단이다.
//   · 변을 탭하면 그 변의 수직이등분선(중점 같은 길이 눈금 + 직각 표시), 꼭짓점(각 안쪽)을 탭하면 그 각의 이등분선(같은 각 호 2개)이
//     판 끝까지 그어진다. 선마다 교과서 이름 라벨(「AB의 수직이등분선」「∠B의 이등분선」). 선이 생길 때 필름 접기 연출(WonFilm.FoldFx).
//   · 두 선이 만나면 교점 표식이 생기고 핀이 그 교점으로 옮겨 선다 → 「핀 박기」. 선은 판당 3개까지.
//     세 번째 선이 같은 점을 지나면 「세 변의 수직이등분선은 한 점에서 만난다」 보너스.
//   · 외심 명판에 각의 이등분선 교점(=내심)이나 섞은 교점에 박으면 오답 → 1단계 거리선(PA·PB·PC)이 1초 되살아나 다름을 보여 준다.
//   · 첫 외심·첫 내심 명판: 유령 손가락이 변(꼭짓점)을 탭하는 시범 1회(가만히 있으면 다시) + 배너에 교과서 성질 문장.
// 판정은 여전히 정수: 선 = 정수 계수 직선(Prob.PerpBisector / AngleBisector), 교점 = 유리수 점(Prob.Meet), 정답 = RP.Is(ans).
// 코드는 학생 대신 선을 고르지 않는다. 핀이 교점으로 옮겨 서는 것은 학생이 그은 두 선의 결과일 뿐이다.
using System.Collections;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.WonJjigeo
{
    public partial class WonJjigeoGame
    {
        /// <summary>작도 선 하나의 그림: 선(밑선+색선)·이름 라벨·표시(수직이등분선=눈금 2+직각, 각의 이등분선=호 2+호 눈금 2).</summary>
        class ConsViz
        {
            public LineRenderer line, under, m0, m1, m2, m3, pill;
            public TextMeshPro label;
            public Vector3 e0, e1, anchor;
            public float growT = 9f;
            public bool on;
        }

        class BLine { public bool perp; public int idx; public Line geo; public string name; public ConsViz viz; }

        readonly ConsViz[] viz = new ConsViz[4];          // 0..2 = 2단계 선, 3 = 다리 단계 선
        readonly List<BLine> lines = new List<BLine>();
        readonly List<RP> markers = new List<RP>();
        readonly LineRenderer[] markerRings = new LineRenderer[4], markerUnder = new LineRenderer[4];
        readonly LineRenderer[] handles = new LineRenderer[6];   // 0..2 = 꼭짓점(각 안쪽), 3..5 = 변의 중점
        readonly Vector3[] handlePos = new Vector3[6];
        bool concurrentBonus, tapsSet;
        LineRenderer ghostLine, ghostPill;
        TextMeshPro ghostLabel;
        Coroutine ghostCo;
        bool ghostOn;
        int ghostPlays;

        static readonly Color PerpCol = Hex("46C9A4"), BisCol = Hex("F2A93B");

        void BuildConsViz()
        {
            for (int k = 0; k < viz.Length; k++)
            {
                var v = new ConsViz
                {
                    under = Line("ConsUnder" + k, 0.13f, 2, false),
                    line = Line("Cons" + k, 0.07f, 2, false),
                    m0 = Line("ConsM0" + k, 0.045f, 13, false), m1 = Line("ConsM1" + k, 0.045f, 13, false),
                    m2 = Line("ConsM2" + k, 0.045f, 3, false), m3 = Line("ConsM3" + k, 0.045f, 2, false),
                    label = FlatText("", 3.0f, Cream),
                    pill = Line("ConsPill" + k, 0.4f, 2, false)
                };
                v.pill.numCapVertices = 6;
                SetColor(v.pill, new Color(0.1f, 0.075f, 0.06f, 0.82f));
                SetColor(v.under, new Color(0.06f, 0.04f, 0.03f, 0.62f));
                v.label.outlineWidth = 0.34f;
                viz[k] = v;
            }
            for (int i = 0; i < markerRings.Length; i++)
            {
                markerUnder[i] = Line("MarkerU" + i, 0.13f, 25, true);
                markerRings[i] = Line("Marker" + i, 0.06f, 25, true);
                SetColor(markerUnder[i], new Color(0.05f, 0.03f, 0.02f, 0.7f));
            }
            for (int i = 0; i < handles.Length; i++) handles[i] = Line("Handle" + i, 0.065f, 25, true);
            ghostLine = Line("GhostLine", 0.09f, 2, false, dashMat);
            ghostLabel = FlatText("", 3.0f, Cream);
            ghostPill = Line("GhostPill", 0.4f, 2, false);
            ghostPill.numCapVertices = 6;
        }

        // ─────────────────────────────── 작도 선 그리기(다리 단계와 2단계가 같이 쓴다)
        /// <summary>perp: 변 idx 의 수직이등분선, 아니면 ∠idx 의 이등분선을 판 끝까지(자라나며) 그린다. 표시·라벨 포함.</summary>
        void ShowCons(ConsViz v, bool perp, int idx, string name)
        {
            Vector2 anchor, U;
            if (perp)
            {
                var a = V2w(cur.V[idx]); var b = V2w(cur.V[(idx + 1) % 3]);
                anchor = (a + b) * 0.5f; var s = (b - a).normalized; U = new Vector2(-s.y, s.x);
            }
            else
            {
                var vk = cur.V[idx]; var u = cur.V[(idx + 1) % 3] - vk; var w = cur.V[(idx + 2) % 3] - vk;
                U = (new Vector2(u.x, u.y).normalized + new Vector2(w.x, w.y).normalized).normalized;
                anchor = V2w(vk);
            }
            ClipLineToFilm(anchor, U, out var e0, out var e1);
            v.anchor = new Vector3(anchor.x, LineY + 0.004f, anchor.y);
            v.e0 = new Vector3(e0.x, LineY + 0.004f, e0.y); v.e1 = new Vector3(e1.x, LineY + 0.004f, e1.y);
            var col = perp ? PerpCol : BisCol;
            SetColor(v.line, col);
            foreach (var m in new[] { v.m0, v.m1, v.m2, v.m3 }) { SetColor(m, col); m.gameObject.SetActive(false); }
            v.growT = 0f; v.on = true;
            v.line.gameObject.SetActive(true); v.under.gameObject.SetActive(true);
            SetConsEnds(v, 0f);
            // 표시
            if (perp)
            {
                var A = IW(cur.V[idx], LineY + 0.006f); var B = IW(cur.V[(idx + 1) % 3], LineY + 0.006f); var M = (A + B) * 0.5f;
                v.m0.positionCount = 2; v.m1.positionCount = 2;
                TickAt(v.m0, A, M, 1, col); TickAt(v.m1, M, B, 1, col);
                var sd = (B - A).normalized; var nd = new Vector3(U.x, 0, U.y); const float q = 0.2f;
                v.m2.positionCount = 3;
                v.m2.SetPosition(0, M + sd * q); v.m2.SetPosition(1, M + (sd + nd) * q); v.m2.SetPosition(2, M + nd * q);
                v.m2.gameObject.SetActive(true);
            }
            else
            {
                var vk = cur.V[idx]; var u = cur.V[(idx + 1) % 3] - vk; var w = cur.V[(idx + 2) % 3] - vk;
                float au = Mathf.Atan2(u.y, u.x), aw = Mathf.Atan2(w.y, w.x), ab = Mathf.Atan2(U.y, U.x);
                var c = IW(vk, LineY + 0.006f);
                const float r = 0.55f;
                Arc(v.m0, c, r, au, ab); Arc(v.m1, c, r, ab, aw);
                ArcTick(v.m2, c, r, au, ab, col); ArcTick(v.m3, c, r, ab, aw, col);
            }
            // 라벨: 선 위의 여러 자리 중 삼각형·다른 라벨과 가장 먼, 판 안에 온전히 들어가는 자리
            v.label.text = name;
            v.label.color = Color.Lerp(col, Color.white, 0.35f);
            v.label.gameObject.SetActive(true);
            PlaceLabel(v, U);
            MgfFx.Punch(v.label.transform, 0.25f, 0.3f);
        }

        void SetConsEnds(ConsViz v, float s)
        {
            var a = Vector3.Lerp(v.anchor, v.e0, s); var b = Vector3.Lerp(v.anchor, v.e1, s);
            v.line.SetPosition(0, a); v.line.SetPosition(1, b);
            v.under.SetPosition(0, a + Vector3.down * 0.002f); v.under.SetPosition(1, b + Vector3.down * 0.002f);
        }

        static void Arc(LineRenderer lr, Vector3 c, float r, float a0, float a1)
        {
            float d = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            int n = 13; lr.positionCount = n;
            for (int i = 0; i < n; i++) { float a = a0 + d * i / (n - 1); lr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r)); }
            lr.gameObject.SetActive(true);
        }

        static void ArcTick(LineRenderer lr, Vector3 c, float r, float a0, float a1, Color col)
        {
            float d = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float a = a0 + d * 0.5f; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            lr.positionCount = 2;
            lr.SetPosition(0, c + dir * (r - 0.11f)); lr.SetPosition(1, c + dir * (r + 0.11f));
            SetColor(lr, col);
            lr.gameObject.SetActive(true);
        }

        /// <summary>라벨 자리 고르기(매 선 1회, 후보 26곳): 판 안에 온전히 들어가고, 글자 상자가 삼각형 변·다른 선·다른 라벨·꼭짓점 이름에서
        /// 멀수록 좋다(상자-직선 간격 = 중심 거리 − 상자의 법선 방향 반폭).</summary>
        void PlaceLabel(ConsViz v, Vector2 U)
        {
            var size = v.label.GetPreferredValues(v.label.text);
            float hw = size.x * 0.5f + 0.06f, hh = size.y * 0.5f + 0.03f;
            var nrm = new Vector2(-U.y, U.x);
            float off = Mathf.Abs(nrm.x) * hw + Mathf.Abs(nrm.y) * hh + 0.1f;   // 자기 선에서 상자가 떨어지는 거리
            Vector3 best = v.anchor; float bestScore = float.MinValue;
            for (int k = 0; k <= 12; k++)
            {
                float t = 0.08f + 0.84f * k / 12f;
                var on = Vector3.Lerp(v.e0, v.e1, t);
                for (int sgn = -1; sgn <= 1; sgn += 2)
                {
                    float cx0 = on.x + nrm.x * sgn * off, cz0 = on.z + nrm.y * sgn * off;
                    float cx = Mathf.Clamp(cx0, -FX + hw, FX - hw), cz = Mathf.Clamp(cz0, -FZ + hh, FZ - hh);
                    float clampPen = Mathf.Abs(cx - cx0) + Mathf.Abs(cz - cz0);
                    var p = new Vector2(cx, cz);
                    float clear = 1.2f;
                    for (int i = 0; i < 3; i++) clear = Mathf.Min(clear, BoxSegGap(p, hw, hh, V2w(cur.V[i]), V2w(cur.V[(i + 1) % 3])));
                    foreach (var o in viz)
                    {
                        if (!o.on) continue;
                        clear = Mathf.Min(clear, BoxSegGap(p, hw, hh, new Vector2(o.e0.x, o.e0.z), new Vector2(o.e1.x, o.e1.z)));
                        if (o != v && o.label.gameObject.activeSelf)
                        {
                            var q = o.label.transform.position;
                            clear = Mathf.Min(clear, Mathf.Max(Mathf.Abs(q.x - cx) - hw * 2f, Mathf.Abs(q.z - cz) - hh * 2f));
                        }
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        var q = vLabels[i].transform.position;
                        clear = Mathf.Min(clear, Mathf.Max(Mathf.Abs(q.x - cx) - hw - 0.2f, Mathf.Abs(q.z - cz) - hh - 0.2f));
                    }
                    float s = Mathf.Min(clear, 0.8f) * 3f - clampPen * 4f - Mathf.Abs(t - 0.5f) * 0.3f;
                    if (s > bestScore) { bestScore = s; best = new Vector3(cx, LineY + 0.03f, cz); }
                }
            }
            v.label.transform.position = best;
            // 라벨 뒤 어두운 알약(모래·청동·우윳빛 필름 어디서든 읽히게)
            float half = Mathf.Max(0.01f, size.x * 0.5f - size.y * 0.25f);
            v.pill.widthMultiplier = size.y + 0.1f;
            v.pill.SetPosition(0, best + new Vector3(-half, -0.012f, 0)); v.pill.SetPosition(1, best + new Vector3(half, -0.012f, 0));
            v.pill.gameObject.SetActive(true);
        }

        /// <summary>축 정렬 글자 상자(중심 c, 반폭 hw·hh)와 선분 ab 의 대략적 간격(음수면 겹침).</summary>
        static float BoxSegGap(Vector2 c, float hw, float hh, Vector2 a, Vector2 b)
        {
            var ab = b - a; float L2 = ab.sqrMagnitude;
            if (L2 < 1e-6f) return (c - a).magnitude;
            float t = Mathf.Clamp01(Vector2.Dot(c - a, ab) / L2);
            var q = a + ab * t; var d = c - q;
            var n = new Vector2(-ab.y, ab.x).normalized;
            float ext = Mathf.Abs(n.x) * hw + Mathf.Abs(n.y) * hh;
            // 선분 끝 밖이면 끝점까지의 상자 거리
            if (t <= 0f || t >= 1f) return Mathf.Max(Mathf.Abs(d.x) - hw, Mathf.Abs(d.y) - hh);
            return Mathf.Abs(Vector2.Dot(d, n)) - ext;
        }

        void HideCons(ConsViz v)
        {
            v.on = false; v.growT = 9f;
            v.line.gameObject.SetActive(false); v.under.gameObject.SetActive(false); v.label.gameObject.SetActive(false); v.pill.gameObject.SetActive(false);
            v.m0.gameObject.SetActive(false); v.m1.gameObject.SetActive(false); v.m2.gameObject.SetActive(false); v.m3.gameObject.SetActive(false);
        }

        void DimCons(bool dim)
        {
            for (int k = 0; k < lines.Count; k++)
            {
                var v = lines[k].viz; var c = lines[k].perp ? PerpCol : BisCol;
                SetColor(v.line, dim ? new Color(c.r, c.g, c.b, 0.3f) : c);
                SetColor(v.under, new Color(0.06f, 0.04f, 0.03f, dim ? 0.2f : 0.62f));
            }
        }

        void UpdateConsViz(float dt)
        {
            foreach (var v in viz)
            {
                if (!v.on || v.growT >= 1f) continue;
                v.growT = Mathf.Min(1f, v.growT + dt / 0.35f);
                float k = v.growT; SetConsEnds(v, 1 - (1 - k) * (1 - k) * (1 - k));
            }
        }

        // ─────────────────────────────── 2단계 상태
        void ClearBuild()
        {
            StopGhost();
            for (int k = 0; k < 3; k++) HideCons(viz[k]);
            lines.Clear(); markers.Clear(); st.lines.Clear(); concurrentBonus = false;
            foreach (var r in markerRings) r.gameObject.SetActive(false);
            foreach (var r in markerUnder) r.gameObject.SetActive(false);
            foreach (var h in handles) h.gameObject.SetActive(false);
            if (foldCo != null) StopFoldFx();
        }

        /// <summary>탭 대상(꼭짓점 손잡이·변 손잡이) 위치 — 명판이 바뀔 때 1회.</summary>
        void LayoutHandles()
        {
            for (int k = 0; k < 3; k++)
            {
                var vk = cur.V[k]; var u = cur.V[(k + 1) % 3] - vk; var w = cur.V[(k + 2) % 3] - vk;
                var U = (new Vector2(u.x, u.y).normalized + new Vector2(w.x, w.y).normalized).normalized;
                handlePos[k] = IW(vk, LineY + 0.01f) + new Vector3(U.x, 0, U.y) * 0.62f;
                var a = cur.V[k]; var b = cur.V[(k + 1) % 3];
                handlePos[3 + k] = GW((a.x + b.x) * 0.5, (a.y + b.y) * 0.5, LineY + 0.01f);
            }
        }

        bool Used(bool perp, int idx) { foreach (var l in lines) if (l.perp == perp && l.idx == idx) return true; return false; }

        void UpdateHandles()
        {
            bool show = step == Step.Build && cur != null && st.phase == "playing" && !roundPending && lines.Count < Rules.MaxLines && pinReady;
            float pulse = 0.2f + 0.05f * Mathf.Sin(Time.time * 5f);
            float a = 0.55f + 0.35f * Mathf.Sin(Time.time * 5f);
            for (int i = 0; i < 6; i++)
            {
                bool on = show && !Used(i >= 3, i % 3);
                if (handles[i].gameObject.activeSelf != on) handles[i].gameObject.SetActive(on);
                if (!on) continue;
                CirclePts(handles[i], handlePos[i], pulse, 25);
                SetColor(handles[i], new Color(1f, 0.97f, 0.88f, a));
            }
            for (int i = 0; i < markers.Count && i < markerRings.Length; i++)
            {
                float r = 0.2f + (pinPos.SameAs(markers[i]) ? 0.06f : 0.03f * Mathf.Sin(Time.time * 7f));
                var c = PW(markers[i], LineY + 0.02f);
                CirclePts(markerRings[i], c, r, 25); CirclePts(markerUnder[i], c + Vector3.down * 0.003f, r, 25);
            }
        }

        // ─────────────────────────────── 2단계 입력: 교점 표식 > 꼭짓점 > 변
        enum TapKind { None, Marker, Vertex, Side }

        TapKind ClassifyTap(double gx, double gy, out int idx, out int marker)
        {
            idx = -1; marker = -1;
            double bm = 0.75;
            for (int i = 0; i < markers.Count; i++) { double d = Dist(gx, gy, markers[i].fx, markers[i].fy); if (d < bm) { bm = d; marker = i; } }
            if (marker >= 0) return TapKind.Marker;
            // 1) 깜빡이는 손잡이(꼭짓점 3·변 3)를 정확히 눌렀으면 그 손잡이
            double bh = 0.8; int hi = -1;
            for (int i = 0; i < 6; i++) { double d = Dist(gx, gy, handlePos[i].x / CELL, handlePos[i].z / CELL); if (d < bh) { bh = d; hi = i; } }
            if (hi >= 0) { idx = hi % 3; return hi < 3 ? TapKind.Vertex : TapKind.Side; }
            // 2) 꼭짓점 구역(꼭짓점에서 1.3칸 안)이 변보다 먼저 — 뾰족한 각 근처는 변과도 가까워서
            double bestV = 1.3; int vi = -1;
            for (int i = 0; i < 3; i++) { double dv = Dist(gx, gy, cur.V[i].x, cur.V[i].y); if (dv < bestV) { bestV = dv; vi = i; } }
            if (vi >= 0) { idx = vi; return TapKind.Vertex; }
            double bestS = 1.0; int si = -1;
            for (int i = 0; i < 3; i++)
            {
                cur.Foot(gx, gy, i, out double fx, out double fy, out double t);
                if (t < 0.02 || t > 0.98) continue;
                double d = Dist(gx, gy, fx, fy);
                if (d < bestS) { bestS = d; si = i; }
            }
            if (si >= 0) { idx = si; return TapKind.Side; }
            return TapKind.None;
        }

        static double Dist(double ax, double ay, double bx, double by) { double dx = ax - bx, dy = ay - by; return System.Math.Sqrt(dx * dx + dy * dy); }

        void BuildTap(double gx, double gy, Vector3 w)
        {
            StopGhost();
            var k = ClassifyTap(gx, gy, out int idx, out int mk);
            Ripple(w);
            if (k == TapKind.Marker) { SetTarget(markers[mk]); Play(clTick, 0.35f); return; }
            if (k == TapKind.Vertex) { TryAddLine(false, idx); return; }
            if (k == TapKind.Side) { TryAddLine(true, idx); return; }
            hintTxt.text = "변을 누르면 수직이등분선 · 꼭짓점을 누르면 각의 이등분선";
            Play(clRefuse, 0.3f);
        }

        /// <summary>선 하나를 긋는다(실제 탭과 QA 훅이 같은 함수를 탄다). 교점이 생기면 핀이 그 교점으로 옮겨 선다.</summary>
        bool TryAddLine(bool perp, int idx)
        {
            if (step != Step.Build || cur == null) return false;
            if (lines.Count >= Rules.MaxLines) { Refuse(null, "선은 3개까지다 · 교점에 핀을 박아라"); return false; }
            if (Used(perp, idx)) { Refuse(null, "이미 그은 선이다"); return false; }
            var l = new BLine { perp = perp, idx = idx, geo = perp ? cur.PerpBisector(idx, (idx + 1) % 3) : cur.AngleBisector(idx), name = perp ? Words.PerpName(idx) : Words.BisName(idx) };
            l.viz = viz[lines.Count];
            lines.Add(l); st.lines.Add(l.name);
            ShowCons(l.viz, perp, idx, l.name);
            FoldFx(perp, idx);
            Play(clScrape, 0.35f);
            // 교점(정수·유리수 — 각의 이등분선이 무리수 방향이면 근사점: 격자점 정답과 같을 수 없다)
            RP newest = default; bool got = false;
            for (int i = 0; i < lines.Count - 1; i++)
                if (Prob.Meet(lines[i].geo, l.geo, out var p) && System.Math.Abs(p.fx) <= Rules.GX + 0.5 && System.Math.Abs(p.fy) <= Rules.GY + 0.5)
                {
                    bool dup = AddMarker(p);
                    if (!got) { newest = p; got = true; }
                    if (dup) { newest = p; }
                }
            if (got) { SetTarget(newest); hintTxt.text = ""; }
            else if (lines.Count >= 2) hintTxt.text = "두 선이 판 밖에서 만난다 · 다른 선을 그어라";
            // 세 선이 한 점에서 만나는가(보너스) — 정수 판정
            if (lines.Count == 3 && Prob.Meet(lines[0].geo, lines[1].geo, out var q) && lines[2].geo.Through(q))
            {
                concurrentBonus = true;
                bool allP = lines[0].perp && lines[1].perp && lines[2].perp, allB = !lines[0].perp && !lines[1].perp && !lines[2].perp;
                ShowBanner(allP ? "세 변의 수직이등분선은 한 점에서 만난다 +50" : allB ? "세 내각의 이등분선은 한 점에서 만난다 +50" : "세 선이 한 점에서 만난다 +50", 2.4f);
                Play(clMatch, 0.6f);
                MgfFx.Glow(PW(q, FilmY + 0.1f), new Color(1f, 0.9f, 0.6f), 10, 0.45f);
            }
            RefreshBuildGauge();
            MgfBridge.NotifyChanged();
            return true;
        }

        /// <summary>교점 표식. 이미 같은 점이 있으면 true(세 선이 한 점에서 만남).</summary>
        bool AddMarker(RP p)
        {
            foreach (var m in markers) if (m.SameAs(p)) return true;
            if (markers.Count >= markerRings.Length) return false;
            markers.Add(p);
            var r = markerRings[markers.Count - 1];
            SetColor(r, new Color(1f, 0.98f, 0.92f, 1f));
            r.gameObject.SetActive(true); markerUnder[markers.Count - 1].gameObject.SetActive(true);
            MgfFx.Glow(PW(p, LineY + 0.05f), new Color(1f, 0.95f, 0.8f), 6, 0.3f);
            Play(clPing, 0.5f);
            return false;
        }

        bool PinAtMarker() { foreach (var m in markers) if (pinTarget.SameAs(m)) return true; return false; }

        // ─────────────────────────────── 2단계 계기판: 그은 선 목록 + 남은 선 수
        void RefreshBuildGauge()
        {
            if (step != Step.Build || cur == null || measureOn) return;
            GaugeList(true);
            int n = lines.Count;
            gaugeHead.text = n == 0 ? "변을 누르면 수직이등분선 · 꼭짓점을 누르면 각의 이등분선"
                : markers.Count > 0 ? $"그은 선 {n}/{Rules.MaxLines} · 두 선의 교점에 핀이 섰다 → 「핀 박기」"
                : $"그은 선 {n}/{Rules.MaxLines} · 남은 선 {Rules.MaxLines - n}개";
            for (int i = 0; i < 3; i++)
            {
                bool has = i < n;
                listTxt[i].text = has ? lines[i].name + "  ✓" : i == n ? $"남은 선 {Rules.MaxLines - n}개" : "·";
                listTxt[i].color = has ? Cream : new Color(1, 1, 1, i == n ? 0.45f : 0.25f);
                listBar[i].color = has ? (lines[i].perp ? PerpCol : BisCol) : new Color(1, 1, 1, 0.12f);
            }
        }

        // ─────────────────────────────── QA·스크린샷용: 탭 대상의 화면 위치(명판마다 1회, 정규화 · 위가 0)
        void UpdateTaps()
        {
            if (tapsSet || step != Step.Build || cur == null || camBlend < 1f || !pinReady) return;
            tapsSet = true;
            var sb = new System.Text.StringBuilder(96);
            for (int i = 0; i < 6; i++)
            {
                var s = cam.WorldToScreenPoint(i < 3 ? IW(cur.V[i], LineY) * 0.6f + handlePos[i] * 0.4f : handlePos[i]);
                if (i > 0) sb.Append(';');
                sb.Append((s.x / Screen.width).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                  .Append((1f - s.y / Screen.height).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));
            }
            st.taps = sb.ToString();
            MgfBridge.NotifyChanged();
        }

        // ─────────────────────────────── 유령 손가락 시범(첫 외심 = 변 AB 탭, 첫 내심 = 꼭짓점 A 탭)
        void StartGhost()
        {
            StopGhost();
            ghostCo = StartCoroutine(GhostCo(cur.m == Mission.Circum));
        }

        void StopGhost()
        {
            if (ghostCo != null) { StopCoroutine(ghostCo); ghostCo = null; }
            ghostOn = false;
            if (fingerRt) fingerRt.gameObject.SetActive(false);
            if (ghostLine) ghostLine.gameObject.SetActive(false);
            if (ghostLabel) ghostLabel.gameObject.SetActive(false);
            if (ghostPill) ghostPill.gameObject.SetActive(false);
        }

        Vector2 WorldToHud(Vector3 w)
        {
            var s = cam.WorldToScreenPoint(w);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRoot, s, null, out var lp);
            return lp;
        }

        IEnumerator GhostCo(bool perp)
        {
            ghostOn = true;
            while (!pinReady || plateEnterT < 1f || camBlend < 1f) yield return null;
            yield return new WaitForSeconds(0.35f);
            ghostPlays++;
            int idx = 0;
            var target = perp ? handlePos[3 + idx] : IW(cur.V[idx], LineY) * 0.45f + handlePos[idx] * 0.55f;
            var to = WorldToHud(target);
            var from = to + new Vector2(120, -170);
            fingerRt.gameObject.SetActive(true);
            fingerImg.color = new Color(1, 1, 1, 0);
            for (float e = 0; e < 0.7f; e += Time.deltaTime)
            {
                float k = e / 0.7f, s = k * k * (3 - 2 * k);
                fingerRt.anchoredPosition = Vector2.Lerp(from, to, s);
                fingerImg.color = new Color(1, 1, 1, Mathf.Min(1, k * 3) * 0.95f);
                fingerRt.localScale = Vector3.one;
                yield return null;
            }
            fingerRt.anchoredPosition = to;
            for (float e = 0; e < 0.14f; e += Time.deltaTime) { fingerRt.localScale = Vector3.one * Mathf.Lerp(1f, 0.84f, e / 0.14f); yield return null; }
            Ripple(target); Play(clTick, 0.4f);
            for (float e = 0; e < 0.12f; e += Time.deltaTime) { fingerRt.localScale = Vector3.one * Mathf.Lerp(0.84f, 1f, e / 0.12f); yield return null; }
            // 유령 선(점선)과 이름이 잠깐 — 학생이 직접 그어야 진짜 선이 된다
            Vector2 anchor, U;
            if (perp)
            {
                var a = V2w(cur.V[0]); var b = V2w(cur.V[1]); anchor = (a + b) * 0.5f; var sd = (b - a).normalized; U = new Vector2(-sd.y, sd.x);
            }
            else
            {
                var vk = cur.V[0]; var u = cur.V[1] - vk; var w = cur.V[2] - vk;
                U = (new Vector2(u.x, u.y).normalized + new Vector2(w.x, w.y).normalized).normalized; anchor = V2w(vk);
            }
            ClipLineToFilm(anchor, U, out var e0, out var e1);
            var A3 = new Vector3(anchor.x, LineY + 0.02f, anchor.y); var E0 = new Vector3(e0.x, LineY + 0.02f, e0.y); var E1 = new Vector3(e1.x, LineY + 0.02f, e1.y);
            ghostLine.gameObject.SetActive(true);
            ghostLabel.text = perp ? Words.PerpName(0) : Words.BisName(0);
            ghostLabel.transform.position = Vector3.Lerp(A3, Vector3.Distance(A3, E0) > Vector3.Distance(A3, E1) ? E0 : E1, 0.55f) + new Vector3(0.45f, 0.01f, 0.25f);
            ClampLabelToFilm(ghostLabel);
            ghostLabel.color = new Color(1f, 0.97f, 0.88f, 0.95f);
            ghostLabel.gameObject.SetActive(true);
            var gsz = ghostLabel.GetPreferredValues(ghostLabel.text); float gh = Mathf.Max(0.01f, gsz.x * 0.5f - gsz.y * 0.25f);
            var gp = ghostLabel.transform.position;
            ghostPill.widthMultiplier = gsz.y + 0.1f;
            ghostPill.SetPosition(0, gp + new Vector3(-gh, -0.012f, 0)); ghostPill.SetPosition(1, gp + new Vector3(gh, -0.012f, 0));
            ghostPill.gameObject.SetActive(true);
            for (float e = 0; e < 1.9f; e += Time.deltaTime)
            {
                float k = Mathf.Clamp01(e / 0.45f), s = 1 - (1 - k) * (1 - k);
                ghostLine.SetPosition(0, Vector3.Lerp(A3, E0, s)); ghostLine.SetPosition(1, Vector3.Lerp(A3, E1, s));
                float fade = e > 1.5f ? 1 - (e - 1.5f) / 0.4f : 1f;
                SetColor(ghostLine, new Color(1f, 0.97f, 0.88f, 0.85f * fade));
                ghostLabel.alpha = fade;
                SetColor(ghostPill, new Color(0.1f, 0.075f, 0.06f, 0.8f * fade));
                if (e > 0.6f) { float f = Mathf.Clamp01((e - 0.6f) / 0.5f); fingerRt.anchoredPosition = Vector2.Lerp(to, to + new Vector2(90, -120), f * f); fingerImg.color = new Color(1, 1, 1, 0.95f * (1 - f)); }
                yield return null;
            }
            ghostLine.gameObject.SetActive(false); ghostLabel.gameObject.SetActive(false); ghostPill.gameObject.SetActive(false); fingerRt.gameObject.SetActive(false);
            ghostOn = false; ghostCo = null;
        }

        void ClampLabelToFilm(TextMeshPro t)
        {
            var size = t.GetPreferredValues(t.text);
            var p = t.transform.position;
            float hw = size.x * 0.5f + 0.08f, hh = size.y * 0.5f + 0.04f;
            p.x = Mathf.Clamp(p.x, -FX + hw, FX - hw); p.z = Mathf.Clamp(p.z, -FZ + hh, FZ - hh);
            t.transform.position = p;
        }

        /// <summary>안내 명판에서 선 없이 가만히 있으면 시범을 다시(최대 3번).</summary>
        void UpdateGhostIdle()
        {
            if (step != Step.Build || cur == null || !cur.tutorial || ghostOn || lines.Count > 0 || roundPending || ghostPlays >= 3) return;
            if (idleT > 9f) { idleT = 0; StartGhost(); }
        }

        // ─────────────────────────────── 2단계 진입(다리 단계 뒤)
        void StartBuild()
        {
            step = Step.Build; st.stage = "2"; st.level = 3; idx = 0;
            ShowCard("2단계 · 작도", "이제 거리선 없이, 성질로 찾는다");
            clampCo = StartCoroutine(ClampPopCo());
            SpawnPlate(gen.Next(2, 0, rng));
        }
    }
}
