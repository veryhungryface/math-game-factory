// 만두 묶기 — 반죽 트레이 한 칸(레인 1개). 판(스테인리스) 위에 이등변삼각형 반죽을 올리고, 수학 레이어(변·호·
// 꼭짓점 이름·수치·대상)를 같은 로컬 평면에 그린다. 트레이 면은 카메라와 같은 회전이라 정사영에서 왜곡이 없다.
// 히트 테스트도 이 로컬 평면 좌표만 쓴다(2.5D 본체와 수학 레이어의 좌표 어긋남 방지).
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.ManduMukgi
{
    public sealed class TrayMats
    {
        public Material plate, plateEdge, dough, doughShade, overlay, ticket, ticketOk, ticketBad, gaugeBack, post, bolt;
        public Color ink = new Color32(0x20, 0x24, 0x2B, 255);
        public Color steelText = new Color32(0x20, 0x24, 0x2B, 255);
    }

    public sealed class Tray
    {
        public enum Phase { Hidden, Arriving, Ready, Feedback, Sealing }

        public const float PlateW = 5.4f, PlateH = 4.7f;
        public const float BoxW = 3.9f, BoxH = 3.25f, BoxY = -0.12f;
        public const float HitR = 0.62f;
        const float Depth = 0.2f;
        const float ZOverlay = -0.26f, ZReveal = -0.29f, ZDyn = -0.33f, ZHint = -0.36f;

        public readonly int lane;
        public Transform stand, face, content, pivot, ticketRoot;
        Transform post, halfLT, halfRT, gaugeFillT, plateT, wheelBase;
        Mesh mL, mR, mWhole, mStatic, mDyn, mReveal, mHint;
        Transform wholeT;
        readonly MeshBuf buf = new MeshBuf();
        readonly TextMeshPro[] nameT = new TextMeshPro[3];
        readonly TextMeshPro[] valT = new TextMeshPro[3];
        TextMeshPro tagMain, tagSub, dLabel, preLabel;
        Renderer ticketBack, gaugeFillR;
        Transform ticketBackT;
        TrayMats mats;

        // ── 반죽(문항) 런타임 상태 ──
        public DoughProblem prob;
        public Phase phase = Phase.Hidden;
        public float life, maxLife, freeze, phaseT, arriveT;
        public bool attempted, failed, firstCorrect;
        public int serial;
        public float drift;
        public Vector3 station, fromPos;
        public float trayScale = 1f;
        public int selectedMask, lastMask;
        public string lastMisconception = "";

        // 기하(로컬). 이름 번호 k 기준.
        public readonly Vector2[] vpos = new Vector2[3];
        public readonly Vector2[] tpos = new Vector2[3];
        public readonly Vector2[] bis = new Vector2[3];
        public Vector2 footD;
        public int rotDeg;
        public int nLeft, nRight;
        float foldSign = 1f;

        public Tray(int lane) { this.lane = lane; }

        static Mesh NewMesh(string n) { var m = new Mesh { name = n }; m.MarkDynamic(); return m; }

        static Transform MeshChild(string n, Transform parent, Mesh m, Material mat, int order, bool shadows = false)
        {
            var go = new GameObject(n);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.sortingOrder = order;
            r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = shadows;
            return go.transform;
        }

        static TextMeshPro Txt(string s, Vector3 pos, float size, Color col, Transform parent, int order)
        {
            var t = MgfText.World(s, pos, size, col, parent);
            t.sortingOrder = order;
            t.fontStyle = FontStyles.Bold;
            t.richText = true;
            return t;
        }

        public void Build(Transform parent, TrayMats m)
        {
            mats = m;
            stand = new GameObject("Tray" + lane).transform;
            stand.SetParent(parent, false);
            // 바퀴 달린 받침 + 기둥(월드 수직)
            wheelBase = MgfLook.Block("Base", Vector3.zero, new Vector3(1.6f, 0.22f, 1.1f), 0.1f, m.post, stand).transform;
            Object.Destroy(wheelBase.GetComponent<Collider>());
            post = MgfLook.Prim(PrimitiveType.Cylinder, "Post", new Vector3(0, 1.4f, 0), new Vector3(0.18f, 1.4f, 0.18f), m.post, stand, false).transform;

            face = new GameObject("Face").transform;
            face.SetParent(stand, false);
            face.localPosition = new Vector3(0, 2.8f, 0);

            plateT = MgfLook.Block("Plate", new Vector3(0, 0, 0.12f), new Vector3(PlateW, PlateH, 0.22f), 0.22f, m.plate, face).transform;
            Object.Destroy(plateT.GetComponent<Collider>());
            var lip = MgfLook.Block("Lip", new Vector3(0, -PlateH * 0.5f + 0.05f, -0.02f), new Vector3(PlateW - 0.2f, 0.14f, 0.12f), 0.05f, m.plateEdge, plateT);
            lip.transform.localPosition = new Vector3(0, -PlateH * 0.5f + 0.1f, -0.13f);
            Object.Destroy(lip.GetComponent<Collider>());
            for (int i = 0; i < 4; i++)
            {
                var b = MgfLook.Prim(PrimitiveType.Sphere, "Bolt", new Vector3((i % 2 == 0 ? -1 : 1) * (PlateW * 0.5f - 0.24f), (i < 2 ? -1 : 1) * (PlateH * 0.5f - 0.24f), -0.1f), Vector3.one * 0.16f, m.bolt, plateT, false);
                b.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            content = new GameObject("Content").transform;
            content.SetParent(face, false);

            mL = new Mesh { name = "DoughL" }; mR = new Mesh { name = "DoughR" }; mWhole = new Mesh { name = "DoughWhole" };
            wholeT = MeshChild("DoughWhole", content, mWhole, m.dough, 0, true);
            halfLT = MeshChild("DoughL", content, mL, m.dough, 0, true);
            pivot = new GameObject("FoldPivot").transform;
            pivot.SetParent(content, false);
            halfRT = MeshChild("DoughR", pivot, mR, m.dough, 0, true);
            foreach (var tr in new[] { wholeT, halfLT, halfRT }) tr.GetComponent<MeshRenderer>().receiveShadows = false;

            mStatic = NewMesh("Overlay"); mDyn = NewMesh("Dyn"); mReveal = NewMesh("Reveal"); mHint = NewMesh("Hint");
            MeshChild("Overlay", content, mStatic, m.overlay, 2);
            MeshChild("Reveal", content, mReveal, m.overlay, 3);
            MeshChild("Dyn", content, mDyn, m.overlay, 4);
            MeshChild("Hint", content, mHint, m.overlay, 6);

            for (int k = 0; k < 3; k++)
            {
                nameT[k] = Txt(DoughProblem.V[k], new Vector3(0, 0, -0.3f), 4.4f, m.ink, content, 8);
                valT[k] = Txt("", new Vector3(0, 0, -0.3f), 3.9f, m.ink, content, 8);
            }
            dLabel = Txt("D", new Vector3(0, 0, -0.42f), 3.6f, m.ink, content, 9);
            preLabel = Txt("잘못된 연결", new Vector3(0, 0, -0.4f), 2.4f, new Color32(0xB0, 0x22, 0x42, 255), content, 9);

            // 발문 티켓(판 위쪽에 매달린 주문표)
            ticketRoot = new GameObject("Ticket").transform;
            ticketRoot.SetParent(face, false);
            ticketRoot.localPosition = new Vector3(0, PlateH * 0.5f + 0.42f, -0.05f);
            var tb = MgfLook.Block("TicketBack", Vector3.zero, new Vector3(PlateW - 0.1f, 0.78f, 0.1f), 0.12f, m.ticket, ticketRoot);
            Object.Destroy(tb.GetComponent<Collider>());
            ticketBackT = tb.transform; ticketBack = tb.GetComponent<Renderer>();
            ticketBack.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tagMain = Txt("", new Vector3(0, 0.02f, -0.1f), 3.3f, Color.white, ticketRoot, 10);
            tagMain.rectTransform.sizeDelta = new Vector2(PlateW - 0.4f, 0.7f);
            tagMain.enableAutoSizing = true; tagMain.fontSizeMin = 2.2f; tagMain.fontSizeMax = 3.4f;
            tagSub = Txt("", new Vector3(0, 0.62f, -0.1f), 3.0f, Color.white, ticketRoot, 10);
            tagSub.rectTransform.sizeDelta = new Vector2(PlateW - 0.4f, 1.0f);
            tagSub.enableAutoSizing = true; tagSub.fontSizeMin = 2.0f; tagSub.fontSizeMax = 3.0f;
            tagSub.textWrappingMode = TextWrappingModes.Normal;
            tagSub.lineSpacing = -18f;

            // 수명 게이지(판 아래)
            var gb = MgfLook.Block("GaugeBack", new Vector3(0, -PlateH * 0.5f - 0.24f, -0.02f), new Vector3(PlateW - 0.6f, 0.2f, 0.08f), 0.08f, m.gaugeBack, face);
            Object.Destroy(gb.GetComponent<Collider>());
            gb.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var gf = MgfLook.Prim(PrimitiveType.Quad, "GaugeFill", new Vector3(0, -PlateH * 0.5f - 0.24f, -0.09f), new Vector3(PlateW - 0.7f, 0.12f, 1f), m.overlay, face, false);
            gaugeFillT = gf.transform; gaugeFillR = gf.GetComponent<Renderer>();
            gaugeFillR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            gaugeFillR.sortingOrder = 5;
            gf.GetComponent<MeshFilter>().sharedMesh = ColoredQuad();

            stand.gameObject.SetActive(false);
        }

        static Mesh coloredQuad;
        /// <summary>정점색 흰색 사각형(Alpha 셰이더는 정점색을 곱한다).</summary>
        static Mesh ColoredQuad()
        {
            if (coloredQuad) return coloredQuad;
            coloredQuad = new Mesh { name = "CQuad" };
            coloredQuad.SetVertices(new List<Vector3> { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) });
            coloredQuad.SetColors(new List<Color> { Color.white, Color.white, Color.white, Color.white });
            coloredQuad.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            coloredQuad.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            coloredQuad.RecalculateBounds();
            return coloredQuad;
        }

        // ───────────────────────── 기하 ─────────────────────────

        /// <summary>문항을 판에 올린다. 회전·반사·밑각 이름 배치는 정답과 독립인 난수로 정한다.</summary>
        public void Configure(DoughProblem p, int rot, bool reflect, bool swapBase)
        {
            prob = p; rotDeg = rot;
            Vector2 g0, g1, g2;
            if (p.AngleMode)
            {
                float b = p.b, s = p.s;
                float h = Mathf.Sqrt(s * s - b * b * 0.25f); // 그리기 좌표용(학생에게 높이를 요구하지 않는다)
                g0 = new Vector2(0, h); g1 = new Vector2(-b * 0.5f, 0); g2 = new Vector2(b * 0.5f, 0);
            }
            else
            {
                g0 = new Vector2(0, Mathf.Tan(p.beta * Mathf.Deg2Rad)); g1 = new Vector2(-1, 0); g2 = new Vector2(1, 0);
            }
            if (reflect) { g0.x = -g0.x; g1.x = -g1.x; g2.x = -g2.x; }
            float th = rot * Mathf.Deg2Rad, cs = Mathf.Cos(th), sn = Mathf.Sin(th);
            Vector2 R(Vector2 q) => new Vector2(q.x * cs - q.y * sn, q.x * sn + q.y * cs);
            g0 = R(g0); g1 = R(g1); g2 = R(g2);
            float minX = Mathf.Min(g0.x, Mathf.Min(g1.x, g2.x)), maxX = Mathf.Max(g0.x, Mathf.Max(g1.x, g2.x));
            float minY = Mathf.Min(g0.y, Mathf.Min(g1.y, g2.y)), maxY = Mathf.Max(g0.y, Mathf.Max(g1.y, g2.y));
            float sc = Mathf.Min(BoxW / Mathf.Max(0.01f, maxX - minX), BoxH / Mathf.Max(0.01f, maxY - minY));
            var mid = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            Vector2 F(Vector2 q) => (q - mid) * sc + new Vector2(0, BoxY);
            g0 = F(g0); g1 = F(g1); g2 = F(g2);

            int n1 = p.Base1, n2 = p.Base2;
            if (swapBase) { int t = n1; n1 = n2; n2 = t; }
            vpos[p.apex] = g0; vpos[n1] = g1; vpos[n2] = g2;
            nLeft = n1; nRight = n2;
            footD = (g1 + g2) * 0.5f;

            for (int k = 0; k < 3; k++)
            {
                var v = vpos[k]; var a = vpos[(k + 1) % 3]; var c = vpos[(k + 2) % 3];
                bis[k] = ((a - v).normalized + (c - v).normalized).normalized;
            }
            for (int k = 0; k < 3; k++)
            {
                if (p.AngleMode) tpos[k] = vpos[k] - bis[k] * 0.08f;
                else
                {
                    var a = vpos[(k + 1) % 3]; var c = vpos[(k + 2) % 3];
                    var m = (a + c) * 0.5f; var d = (c - a).normalized; var nrm = new Vector2(-d.y, d.x);
                    if (Vector2.Dot(nrm, m - vpos[k]) < 0) nrm = -nrm;
                    tpos[k] = m + nrm * 0.4f;
                }
            }

            // 반죽 두 조각: 꼭지각 이등분선 AD 로 나뉜다(정답 뒤 접기 시범용). 공급 전에는 하나로 보인다.
            var apexP = vpos[p.apex];
            var leftHalf = new[] { apexP, vpos[n1], footD };
            var rightHalf = new[] { apexP, footD, vpos[n2] };
            DoughMesh.Build(mWhole, new[] { apexP, vpos[n1], vpos[n2] }, Depth, 0.26f);
            DoughMesh.Build(mL, leftHalf, Depth, 0.22f);
            var rel = new Vector2[3];
            for (int i = 0; i < 3; i++) rel[i] = rightHalf[i] - apexP;
            DoughMesh.Build(mR, rel, Depth, 0.22f);
            pivot.localPosition = new Vector3(apexP.x, apexP.y, -Depth);
            pivot.localRotation = Quaternion.identity;
            halfRT.localPosition = new Vector3(0, 0, Depth);
            halfLT.localPosition = Vector3.zero;
            var axis = new Vector3(footD.x - apexP.x, footD.y - apexP.y, 0).normalized;
            var cRel = new Vector3((rel[0].x + rel[1].x + rel[2].x) / 3f, (rel[0].y + rel[1].y + rel[2].y) / 3f, Depth * 0.5f);
            float zPlus = (Quaternion.AngleAxis(90f, axis) * cRel).z, zMinus = (Quaternion.AngleAxis(-90f, axis) * cRel).z;
            foldSign = zPlus < zMinus ? 1f : -1f;

            // 라벨
            for (int k = 0; k < 3; k++)
            {
                nameT[k].text = DoughProblem.V[k];
                nameT[k].transform.localPosition = new Vector3(vpos[k].x - bis[k].x * 0.46f, vpos[k].y - bis[k].y * 0.46f, -0.3f);
                if (p.AngleMode)
                {
                    var a = vpos[(k + 1) % 3]; var c = vpos[(k + 2) % 3];
                    var m = (a + c) * 0.5f; var d = (c - a).normalized; var nrm = new Vector2(-d.y, d.x);
                    if (Vector2.Dot(nrm, m - vpos[k]) < 0) nrm = -nrm;
                    int len = k == p.apex ? p.b : p.s;
                    valT[k].text = len + "<size=75%> cm</size>";
                    var lp = m + nrm * 0.44f;
                    valT[k].transform.localPosition = new Vector3(lp.x, lp.y, -0.3f);
                }
                else
                {
                    int deg = k == p.apex ? p.alpha : p.beta;
                    float half = (k == p.apex ? p.alpha : p.beta) * 0.5f * Mathf.Deg2Rad;
                    float dist = Mathf.Clamp(0.52f / Mathf.Sin(half), 0.72f, 1.3f);
                    valT[k].text = deg + "°";
                    var lp = vpos[k] + bis[k] * dist;
                    valT[k].transform.localPosition = new Vector3(lp.x, lp.y, -0.3f);
                }
                nameT[k].gameObject.SetActive(true); valT[k].gameObject.SetActive(true);
            }
            dLabel.gameObject.SetActive(false);
            preLabel.gameObject.SetActive(p.Fix);
            if (p.Fix)
            {
                Vector2 a = Vector2.zero, c = Vector2.zero; int seen = 0;
                for (int k = 0; k < 3; k++) if ((p.preMask & (1 << k)) != 0) { if (seen++ == 0) a = tpos[k]; else c = tpos[k]; }
                PreCurve(a, c, out var ctrl);
                var mid2 = 0.25f * a + 0.5f * ctrl + 0.25f * c;
                var off = (ctrl - (a + c) * 0.5f).normalized * 0.28f;
                preLabel.transform.localPosition = new Vector3(mid2.x + off.x, mid2.y + off.y, -0.4f);
            }
            RebuildStatic();
            selectedMask = 0;
            ClearDyn(); ClearReveal(); ClearHint();
            content.localScale = Vector3.one;
            content.gameObject.SetActive(true);
            SetSplit(false);
            SetTag(p.Instruction, "", 0);
        }

        public void RebuildStatic()
        {
            var ink = mats.ink;
            buf.Clear(); buf.z = ZOverlay;
            for (int k = 0; k < 3; k++) buf.Segment(vpos[(k + 1) % 3], vpos[(k + 2) % 3], 0.075f, ink);
            for (int k = 0; k < 3; k++) buf.Disc(vpos[k], 0.05f, ink, 10);
            for (int k = 0; k < 3; k++)
            {
                var v = vpos[k]; var a = vpos[(k + 1) % 3]; var c = vpos[(k + 2) % 3];
                float a0 = Mathf.Atan2(a.y - v.y, a.x - v.x), a1 = Mathf.Atan2(c.y - v.y, c.x - v.x);
                float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                buf.Arc(v, prob.AngleMode ? 0.42f : 0.34f, prob.AngleMode ? 0.07f : 0.05f, a0, a0 + da, ink, 14);
            }
            // 대상 표시: 세 곳 모두 똑같은 모양·색(정답 누설 없음).
            var ringCol = new Color(0.086f, 0.29f, 0.48f, 0.55f);
            for (int k = 0; k < 3; k++)
            {
                if (prob.AngleMode) buf.Ring(tpos[k], 0.6f, 0.05f, ringCol, 30);
                else
                {
                    buf.Disc(tpos[k], 0.27f, new Color(0.96f, 0.94f, 0.9f, 1f), 16);
                    buf.Ring(tpos[k], 0.27f, 0.05f, ink, 16);
                    buf.Ring(tpos[k], 0.55f, 0.04f, ringCol, 28);
                }
            }
            if (prob.Fix)
            {
                Vector2 a = Vector2.zero, c = Vector2.zero; int seen = 0;
                for (int k = 0; k < 3; k++) if ((prob.preMask & (1 << k)) != 0) { if (seen++ == 0) a = tpos[k]; else c = tpos[k]; }
                var col = new Color32(0xB0, 0x22, 0x42, 255);
                PreCurve(a, c, out var ctrl);
                curve.Clear();
                for (int i = 0; i <= 16; i++) { float u = i / 16f, iu = 1f - u; curve.Add(iu * iu * a + 2f * iu * u * ctrl + u * u * c); }
                for (int i = 1; i < curve.Count; i += 2) buf.Segment(curve[i - 1], curve[i], 0.09f, col);
                buf.Disc(a, 0.1f, col, 10); buf.Disc(c, 0.1f, col, 10);
            }
            buf.Apply(mStatic);
        }

        readonly List<Vector2> curve = new List<Vector2>(20);

        /// <summary>미리 그려진 잘못된 연결은 변과 겹치지 않게 휜 실로 그린다(각 모드: 안쪽, 변 모드: 바깥쪽으로).</summary>
        void PreCurve(Vector2 a, Vector2 c, out Vector2 ctrl)
        {
            var cen = (vpos[0] + vpos[1] + vpos[2]) / 3f;
            var mid = (a + c) * 0.5f;
            var dir = (cen - mid); if (dir.sqrMagnitude < 1e-6f) dir = Vector2.up; dir.Normalize();
            if (!prob.AngleMode) dir = -dir;
            ctrl = mid + dir * 0.9f;
        }

        public void SetPreVisible(bool on)
        {
            preLabel.gameObject.SetActive(on && prob != null && prob.Fix);
        }

        /// <summary>판 로컬 좌표 → 가장 가까운 대상(반경 안). 겹치는 원은 가장 가까운 쪽이 이긴다.</summary>
        public int NearestTarget(Vector2 local, float r = HitR)
        {
            int best = -1; float bd = r * r;
            for (int k = 0; k < 3; k++)
            {
                float d = (tpos[k] - local).sqrMagnitude;
                if (d < bd) { bd = d; best = k; }
            }
            return best;
        }

        public bool InsidePlate(Vector2 local)
        {
            return Mathf.Abs(local.x) <= PlateW * 0.5f + 0.1f && local.y <= PlateH * 0.5f + 0.9f && local.y >= -PlateH * 0.5f - 0.4f;
        }

        public bool ScreenToLocal(Camera cam, Vector2 screen, out Vector2 local)
        {
            var ray = cam.ScreenPointToRay(screen);
            var plane = new Plane(face.forward, content.position);
            if (plane.Raycast(ray, out float d))
            {
                var lp = content.InverseTransformPoint(ray.GetPoint(d));
                local = new Vector2(lp.x, lp.y);
                return true;
            }
            local = default;
            return false;
        }

        public Vector2 LocalToScreen(Camera cam, Vector2 local)
        {
            var w = content.TransformPoint(new Vector3(local.x, local.y, ZOverlay));
            var s = cam.WorldToScreenPoint(w);
            return new Vector2(s.x, s.y);
        }

        // ───────────────────────── 동적 레이어 ─────────────────────────

        public void ClearDyn() { buf.Clear(); buf.Apply(mDyn); }
        public void ClearReveal() { buf.Clear(); buf.Apply(mReveal); dLabel.gameObject.SetActive(false); }
        public void ClearHint() { buf.Clear(); buf.Apply(mHint); }

        /// <summary>선택 중: 지나간 대상에 중립 점선 고리, 손 궤적은 중립색. 정오는 손을 뗀 뒤에만.</summary>
        public void DrawStroke(List<Vector2> trail, int visited, Color trailCol, float width, float phase)
        {
            buf.Clear(); buf.z = ZDyn;
            var shade = new Color(0.08f, 0.1f, 0.13f, 0.55f * trailCol.a);
            if (trail != null && trail.Count > 1)
            {
                buf.Polyline(trail, width + 0.08f, shade);
                buf.z = ZDyn - 0.01f;
                buf.Polyline(trail, width, trailCol);
            }
            buf.z = ZDyn - 0.02f;
            for (int k = 0; k < 3; k++)
                if ((visited & (1 << k)) != 0) buf.Ring(tpos[k], 0.5f, 0.07f, new Color(1f, 1f, 1f, 0.95f), 30, true, phase);
            buf.Apply(mDyn);
        }

        /// <summary>정답 공개: 같은 두 변(또는 같은 두 각)과 정답 두 대상을 노란 빛으로 연결해 보인다.</summary>
        public void DrawReveal(bool showCorrectPair, int wrongMask, bool showBisector, float pulse)
        {
            var gold = new Color(0.95f, 0.76f, 0.31f, 0.9f);
            var coral = new Color(1f, 0.36f, 0.48f, 0.95f);
            buf.Clear(); buf.z = ZReveal;
            int cm = prob.CorrectMask;
            int b1 = prob.Base1, b2 = prob.Base2;
            // 근거: 각 모드는 같은 두 변(밑각의 맞은편 변)을, 변 모드는 같은 두 각의 호를 함께 빛낸다.
            if (prob.AngleMode)
            {
                buf.Segment(vpos[prob.apex], vpos[b2], 0.2f, new Color(gold.r, gold.g, gold.b, 0.55f));
                buf.Segment(vpos[prob.apex], vpos[b1], 0.2f, new Color(gold.r, gold.g, gold.b, 0.55f));
            }
            else
            {
                foreach (int k in new[] { b1, b2 })
                {
                    var v = vpos[k]; var a = vpos[(k + 1) % 3]; var c = vpos[(k + 2) % 3];
                    float a0 = Mathf.Atan2(a.y - v.y, a.x - v.x), a1 = Mathf.Atan2(c.y - v.y, c.x - v.x);
                    float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    buf.Arc(v, 0.34f, 0.2f, a0, a0 + da, new Color(gold.r, gold.g, gold.b, 0.6f), 14);
                }
            }
            if (showCorrectPair)
            {
                Vector2 a = tpos[b1], c = tpos[b2];
                buf.Dashed(a, c, 0.1f, 0.24f, 0.12f, gold, pulse);
                float rr = 0.5f + 0.05f * Mathf.Sin(pulse * 6.28f);
                buf.Ring(a, rr, 0.1f, gold, 30); buf.Ring(c, rr, 0.1f, gold, 30);
            }
            for (int k = 0; k < 3; k++) if ((wrongMask & (1 << k)) != 0 && (cm & (1 << k)) == 0) buf.Ring(tpos[k], 0.5f, 0.1f, coral, 30);
            if (showBisector)
            {
                buf.Dashed(vpos[prob.apex], footD, 0.07f, 0.16f, 0.1f, mats.ink, pulse * 0.5f);
                buf.Disc(footD, 0.08f, mats.ink, 10);
                var up = (vpos[prob.apex] - footD).normalized; var side = new Vector2(-up.y, up.x);
                var dl = footD + up * 0.34f + side * 0.22f;
                dLabel.transform.localPosition = new Vector3(dl.x, dl.y, -0.42f);
                dLabel.gameObject.SetActive(true);
            }
            else dLabel.gameObject.SetActive(false);
            buf.Apply(mReveal);
        }

        /// <summary>유령 손가락: from→to 로 흐르는 점선 + 화살촉 + 손끝 원. t 0..1 반복.</summary>
        public void DrawHint(int from, int to, float t, float big, bool oppositeDemo, float demoT)
        {
            buf.Clear(); buf.z = ZHint;
            var white = new Color(1f, 1f, 1f, 0.9f);
            var cob = new Color(0.086f, 0.29f, 0.48f, 0.95f);
            if (oppositeDemo)
            {
                // 같은 두 변의 길이 라벨 → 맞은편 꼭짓점으로 점선이 한 번 이동('맞은편').
                int b1 = prob.Base1, b2 = prob.Base2;
                foreach (int k in new[] { b1, b2 })
                {
                    var a = vpos[(k + 1) % 3]; var c = vpos[(k + 2) % 3];
                    var m = (a + c) * 0.5f;
                    var end = Vector2.Lerp(m, vpos[k], Mathf.Clamp01(demoT));
                    buf.Dashed(m, end, 0.08f, 0.16f, 0.1f, cob, demoT * 3f);
                    if (demoT > 0.15f) buf.Arrow(m, end, 0.22f, cob);
                }
            }
            if (from >= 0 && to >= 0)
            {
                var a = tpos[from]; var c = tpos[to];
                buf.Dashed(a, c, 0.09f, 0.2f, 0.14f, white, t * 3f);
                buf.Arrow(a, c + (a - c).normalized * 0.55f, 0.3f, white);
                float pr = 0.55f + 0.12f * Mathf.Sin(t * Mathf.PI * 4f);
                buf.Ring(a, pr * big, 0.09f, white, 30);
                var tip = Vector2.Lerp(a, c, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.25f)));
                buf.Disc(tip, 0.24f * big, new Color(1f, 1f, 1f, 0.55f), 18);
                buf.Ring(tip, 0.26f * big, 0.06f, cob, 18);
            }
            buf.Apply(mHint);
        }

        /// <summary>짧은 유령 획: 한 대상에서 반죽 안쪽으로 조금 나왔다 사라진다(정답 짝을 가리키지 않는다).</summary>
        public void DrawStub(int k, float t)
        {
            buf.Clear(); buf.z = ZHint;
            var white = new Color(1f, 1f, 1f, 0.9f * (1f - t * 0.6f));
            var c = (vpos[0] + vpos[1] + vpos[2]) / 3f;
            var a = tpos[k];
            var end = Vector2.Lerp(a, c, 0.45f * Mathf.Clamp01(t * 1.6f));
            buf.Dashed(a, end, 0.09f, 0.18f, 0.12f, white, t * 3f);
            buf.Ring(a, 0.5f + 0.15f * Mathf.Sin(t * Mathf.PI * 3f), 0.08f, white, 28);
            buf.Apply(mHint);
        }

        // ───────────────────────── 티켓·게이지·접기 ─────────────────────────

        /// <summary>mode 0=평상(코발트) 1=정답(노랑) 2=오답(산호)</summary>
        public void SetTag(string main, string sub, int mode)
        {
            tagMain.text = main;
            tagSub.text = sub;
            bool two = !string.IsNullOrEmpty(sub);
            ticketBackT.localScale = new Vector3(1f, two ? 2.5f : 1f, 1f);
            ticketBackT.localPosition = new Vector3(0, two ? 0.58f : 0f, 0);
            tagMain.transform.localPosition = new Vector3(0, two ? 0.98f : 0.02f, -0.1f);
            tagSub.transform.localPosition = new Vector3(0, two ? 0.28f : 0.6f, -0.1f);
            ticketBack.sharedMaterial = mode == 1 ? mats.ticketOk : mode == 2 ? mats.ticketBad : mats.ticket;
            var txt = mode == 1 ? mats.ink : Color.white;
            tagMain.color = txt; tagSub.color = txt;
        }

        public void SetGauge(float frac, bool visible)
        {
            frac = Mathf.Clamp01(frac);
            gaugeFillT.gameObject.SetActive(visible && frac > 0.001f);
            float w = PlateW - 0.7f;
            gaugeFillT.localScale = new Vector3(w * frac, 0.12f, 1f);
            gaugeFillT.localPosition = new Vector3(-w * 0.5f + w * frac * 0.5f, -PlateH * 0.5f - 0.24f, -0.09f);
            var c = frac > 0.33f ? new Color(0.95f, 0.76f, 0.31f, 1f) : new Color(1f, 0.36f, 0.48f, 1f);
            SetQuadColor(c);
        }

        Color lastGauge = Color.clear;
        Mesh ownGauge;
        void SetQuadColor(Color c)
        {
            if (c == lastGauge) return;
            lastGauge = c;
            var mf = gaugeFillT.GetComponent<MeshFilter>();
            if (!ownGauge) { ownGauge = Object.Instantiate(ColoredQuad()); mf.sharedMesh = ownGauge; }
            ownGauge.SetColors(new List<Color> { c, c, c, c });
        }

        /// <summary>false: 통짜 반죽(평상시) / true: 이등분선으로 나뉜 두 조각(봉합 시범).</summary>
        public void SetSplit(bool split)
        {
            wholeT.gameObject.SetActive(!split);
            halfLT.gameObject.SetActive(split);
            halfRT.gameObject.SetActive(split);
        }

        public void Fold(float f)
        {
            var apexP = vpos[prob.apex];
            var axis = new Vector3(footD.x - apexP.x, footD.y - apexP.y, 0).normalized;
            pivot.localRotation = Quaternion.AngleAxis(foldSign * 180f * f, axis);
        }

        public void SetLabelsVisible(bool on)
        {
            for (int k = 0; k < 3; k++) { nameT[k].gameObject.SetActive(on); valT[k].gameObject.SetActive(on); }
        }

        public void SetStaticVisible(bool on)
        {
            content.Find("Overlay").gameObject.SetActive(on);
        }

        /// <summary>월드 세로 기둥 길이를 판 높이에 맞춘다.</summary>
        public void SetHeight(float h)
        {
            face.localPosition = new Vector3(0, h, 0);
            post.localPosition = new Vector3(0, h * 0.5f, 0.2f);
            post.localScale = new Vector3(0.2f, h * 0.5f, 0.2f);
        }

        public Vector3 DoughCenterWorld()
        {
            var c = (vpos[0] + vpos[1] + vpos[2]) / 3f;
            return content.TransformPoint(new Vector3(c.x, c.y, -0.3f));
        }

        public Vector3 TargetWorld(int k)
        {
            return content.TransformPoint(new Vector3(tpos[k].x, tpos[k].y, -0.3f));
        }

        /// <summary>날아가는 만두 모양용: 접힌 반쪽(꼭지각·밑각·D) 다각형.</summary>
        public Vector2[] FoldedPolygon()
        {
            var apexP = vpos[prob.apex];
            var c = (apexP + vpos[nLeft] + footD) / 3f;
            return new[] { apexP - c, vpos[nLeft] - c, footD - c };
        }

        public Transform Plate => plateT;
    }
}
