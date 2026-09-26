// 원 찍어 v3 — 다리 단계 「거리가 같은 점들은 한 줄로 선다」 (WonJjigeoGame 의 partial)
//
// DESIGN-v3.md: 1단계(거리)와 2단계(작도) 사이의 개념 다리. 외심 다리·내심 다리 각 1판.
//   · 외심 다리: A·B 두 꼭짓점만 강조(C 는 흐리게). 핀 P 에서 PA·PB 두 선만 실시간. PA=PB(초록)인 격자점에서 「점 찍기」 → 발자국.
//   · 내심 다리: 변 AB·AC 만 강조. 두 변에 내린 수선만 실시간. ∠A 안쪽에서 두 길이가 같으면 초록 → 「점 찍기」 → 발자국.
//   · 발자국 3개가 모이면 그 셋을 지나는 직선이 판 끝까지 자라나며 「변 AB의 수직이등분선」(중점 같은 길이 눈금 + 직각) /
//     「∠A의 이등분선」(같은 각 호 2개) 라벨이 붙고, 교과서 성질 문장과 2단계 예고가 뜬다.
//   · 잘못 찍으면(두 길이가 다르면) 발자국이 안 남고 두 길이가 흔들린다. 학습 단계라 핀(목숨)은 잃지 않고 제한 시간도 없다.
// 판정: Prob.BridgeOk(정수 — 거리² 비교 / 변까지 거리 교차곱 + ∠A 안쪽 부호). 발자국 3개가 그 직선(BridgeLine, 정수 계수) 위에 있고
// 한 직선 위(교차곱 0)인지를 드러내기 직전에 정수로 다시 확인한다.
using System.Collections;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.WonJjigeo
{
    public partial class WonJjigeoGame
    {
        readonly List<IP> dots = new List<IP>();
        readonly Transform[] dotDisc = new Transform[3];
        readonly LineRenderer[] dotRing = new LineRenderer[3];
        readonly TextMeshPro[] dotNum = new TextMeshPro[3];
        readonly float[] dotT = { 9, 9, 9 };
        readonly LineRenderer[] hl = new LineRenderer[3];   // 외심 다리: A·B 둘레 고리 / 내심 다리: 변 AB·AC 덧선
        bool bridgeDone, bridgeGreen, bridgeOutside;
        int bridgeIdx;
        float wobT = 9f;
        Coroutine bridgeCo;
        Material dotMat;

        void BuildBridgeFx()
        {
            dotMat = new Material(MgfLook.Shader("MgfAlpha")) { name = "WonDot", mainTexture = DiscTex() };
            dotMat.SetColor("_Color", new Color(0.2f, 0.85f, 0.62f, 0.95f));
            for (int i = 0; i < 3; i++)
            {
                var d = MgfLook.Prim(PrimitiveType.Quad, "Dot" + i, Vector3.zero, Vector3.one * 0.62f, dotMat, world, false);
                d.transform.rotation = Quaternion.Euler(90, 0, 0);
                d.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                d.SetActive(false); dotDisc[i] = d.transform;
                dotRing[i] = Line("DotRing" + i, 0.055f, 25, true);
                dotNum[i] = FlatText((i + 1).ToString(), 2.6f, Cream);
                hl[i] = Line("BridgeHl" + i, 0.1f, 25, true);
            }
        }

        static Texture2D DiscTex()
        {
            const int S = 64;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Disc", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = x + 0.5f - S / 2f, dy = y + 0.5f - S / 2f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(S / 2f - 1.5f - d) * 255));
                }
            t.SetPixels32(px); t.Apply(false, true);
            return t;
        }

        // ─────────────────────────────── 진입
        void StartBridge()
        {
            step = Step.Bridge; st.stage = "bridge"; st.level = 2; bridgeIdx = 0;
            ShowCard("1단계 통과", "거리가 같은 점들은 어디에 모일까?");
            SpawnPlate(gen.MakeBridge(Mission.Circum, rng));
        }

        /// <summary>SpawnPlate 에서 부른다: 강조·흐림 표시와 발자국 초기화.</summary>
        void SetupBridgePlate()
        {
            dots.Clear(); st.dots = 0; bridgeDone = false; bridgeGreen = false; bridgeOutside = false; wobT = 9f;
            for (int i = 0; i < 3; i++) { dotDisc[i].gameObject.SetActive(false); dotRing[i].gameObject.SetActive(false); dotNum[i].gameObject.SetActive(false); dotT[i] = 9; }
            HideCons(viz[3]);
            var em = new Color(1f, 0.93f, 0.62f, 0.95f);
            if (cur.m == Mission.Circum)
            {
                for (int i = 0; i < 2; i++) { hl[i].loop = true; hl[i].positionCount = 25; CirclePts(hl[i], IW(cur.V[i], LineY + 0.01f), 0.32f, 25); SetColor(hl[i], em); hl[i].gameObject.SetActive(true); }
                hl[2].gameObject.SetActive(false);
            }
            else
            {
                // 변 AB(변 0)·변 AC(변 2) 덧선
                int[] sides = { 0, 2 };
                for (int k = 0; k < 2; k++)
                {
                    int s = sides[k]; var l = hl[k];
                    l.loop = false; l.positionCount = 2;
                    l.SetPosition(0, IW(cur.V[s], LineY + 0.008f)); l.SetPosition(1, IW(cur.V[(s + 1) % 3], LineY + 0.008f));
                    SetColor(l, new Color(em.r, em.g, em.b, 0.8f)); l.gameObject.SetActive(true);
                }
                hl[2].gameObject.SetActive(false);
            }
            plantTxt.text = "점 찍기";
        }

        void HideBridge()
        {
            if (bridgeCo != null) { StopCoroutine(bridgeCo); bridgeCo = null; }
            for (int i = 0; i < 3; i++) { dotDisc[i].gameObject.SetActive(false); dotRing[i].gameObject.SetActive(false); dotNum[i].gameObject.SetActive(false); hl[i].gameObject.SetActive(false); }
            HideCons(viz[3]);
            dots.Clear(); st.dots = 0;
        }

        /// <summary>핀이 설 때: 두 길이가 같은가(정수) · 내심 다리는 ∠A 안쪽인가.</summary>
        void UpdateBridgeEq()
        {
            bool eq = cur.EqualPair(pinPos, mIdx[0], mIdx[1], cur.m);
            bool inside = cur.m == Mission.Circum || (pinPos.exact && cur.InsideAngleA(pinPos));
            bool green = eq && inside;
            if (green && !bridgeGreen && !bridgeDone)
            {
                Play(clMatch, 0.45f);
                MgfFx.Glow(PinTipWorld() + Vector3.up * 0.1f, Pair, 5, 0.3f);
                MgfFx.Punch(gaugeRt, 0.05f, 0.2f);
            }
            bridgeGreen = green; bridgeOutside = eq && !inside;
        }

        // ─────────────────────────────── 「점 찍기」
        void Stamp()
        {
            if (st.phase != "playing" || cur == null || curPin == null || !pinReady) return;
            if (busy) { Refuse(null, "직선이 드러나는 중이다"); return; }
            if (bridgeDone) { NextBridge(); return; }
            if (stepping) stepping = false;
            pinTarget = pinPos; targetRing.gameObject.SetActive(false);
            if (!pinPos.IsLattice) return;
            var q = pinPos.Lattice;
            foreach (var d in dots) if (d.Same(q)) { Refuse(dotDisc[0], "이미 찍은 점이다 · 다른 점을 찾아라"); return; }
            if (cur.BridgeOk(pinPos))
            {
                int k = dots.Count;
                dots.Add(q); st.dots = dots.Count;
                st.score += 20;
                var p = IW(q, LineY + 0.006f);
                dotDisc[k].position = p; dotDisc[k].gameObject.SetActive(true); dotT[k] = 0;
                CirclePts(dotRing[k], p + Vector3.up * 0.004f, 0.3f, 25); SetColor(dotRing[k], new Color(0.06f, 0.3f, 0.22f, 0.95f)); dotRing[k].gameObject.SetActive(true);
                dotNum[k].transform.position = p + new Vector3(0.34f, 0.01f, 0.3f); dotNum[k].gameObject.SetActive(true);
                Play(clClang, 0.45f); Play(clPing, 0.5f);
                MgfFx.Burst(p + Vector3.up * 0.08f, Pair, 10, 1.2f, 0.08f);
                hintTxt.text = dots.Count < Rules.BridgeDots ? $"발자국 {dots.Count}개 · {Rules.BridgeDots - dots.Count}개 더" : "";
                if (dots.Count >= Rules.BridgeDots) bridgeCo = StartCoroutine(BridgeRevealCo());
                PaintMeasure();
            }
            else
            {
                wobT = 0f;
                Play(clRefuse, 0.5f);
                hintTxt.text = bridgeOutside ? "∠A의 바깥이다 · 두 변 사이(각의 안쪽)에서 찾아라"
                    : cur.m == Mission.Circum ? "PA와 PB가 다르다 · 두 길이가 같은 점에만 발자국이 남는다"
                    : "두 변까지의 거리가 다르다 · 같은 점에만 발자국이 남는다";
                MgfFx.Punch(gaugeRt, 0.08f, 0.25f);
            }
            MgfBridge.NotifyChanged();
        }

        IEnumerator BridgeRevealCo()
        {
            busy = true;
            // 정수 확인: 발자국 셋이 한 직선 위(교차곱 0)이고 그 직선이 이등분선(정수 계수)이다
            var L = cur.BridgeLine();
            bool ok = Geo.Cross(dots[1] - dots[0], dots[2] - dots[0]) == 0;
            foreach (var d in dots) ok &= L.Through(RP.Of(d));
            if (!ok) Debug.LogWarning("[won] 다리 단계: 발자국이 이등분선 위에 있지 않다(생성기 점검 필요)");
            yield return new WaitForSeconds(0.35f);
            HideMeasure();
            bool perp = cur.m == Mission.Circum;
            ShowCons(viz[3], perp, 0, perp ? "변 AB의 수직이등분선" : "∠A의 이등분선");
            Play(clChime, 0.6f); camPush = 1f;
            for (int i = 0; i < 3; i++) MgfFx.Glow(IW(dots[i], LineY + 0.1f), Pair, 5, 0.3f);
            yield return new WaitForSeconds(0.45f);
            ShowBanner(perp ? "A, B에서 거리가 같은 점은 모두\nAB의 수직이등분선 위에 있다" : "두 변에서 거리가 같은 점은\n그 각의 이등분선 위에 있다", 0);
            GaugeList(true);
            gaugeHead.text = perp ? "PA=PB인 점 3개 → 한 직선 위" : "두 변까지 거리가 같은 점 3개 → 한 직선 위";
            listTxt[0].text = (perp ? "변 AB의 수직이등분선" : "∠A의 이등분선") + "  ✓"; listTxt[0].color = Cream; listBar[0].color = perp ? PerpCol : BisCol;
            listTxt[1].text = ""; listTxt[2].text = ""; listBar[1].color = new Color(1, 1, 1, 0); listBar[2].color = new Color(1, 1, 1, 0);
            yield return new WaitForSeconds(1.0f);
            listTxt[1].text = perp ? "그러면 세 꼭짓점에서 거리가 같은 외심은?" : "그러면 세 변에서 거리가 같은 내심은?";
            listTxt[1].color = new Color(1, 1, 1, 0.75f);
            listTxt[2].text = perp ? "→ 세 변의 수직이등분선이 만나는 점" : "→ 세 내각의 이등분선이 만나는 점";
            listTxt[2].color = perp ? PerpCol : BisCol;
            MgfFx.Punch(gaugeRt, 0.06f, 0.25f);
            FinishBridgeReveal();
        }

        /// <summary>드러내기를 끝낸 상태로(QA 훅이 연출 도중 오면 즉시 이 상태로 건너뛴다).</summary>
        void FinishBridgeReveal()
        {
            if (step != Step.Bridge || dots.Count < Rules.BridgeDots || bridgeDone) return;
            if (bridgeCo != null) { StopCoroutine(bridgeCo); bridgeCo = null; }
            bool perp = cur.m == Mission.Circum;
            if (!viz[3].on) { HideMeasure(); ShowCons(viz[3], perp, 0, perp ? "변 AB의 수직이등분선" : "∠A의 이등분선"); }
            if (!bannerBg.gameObject.activeSelf || !bannerTxt.text.Contains("위에 있다"))
                ShowBanner(perp ? "A, B에서 거리가 같은 점은 모두\nAB의 수직이등분선 위에 있다" : "두 변에서 거리가 같은 점은\n그 각의 이등분선 위에 있다", 0);
            GaugeList(true);
            gaugeHead.text = perp ? "PA=PB인 점 3개 → 한 직선 위" : "두 변까지 거리가 같은 점 3개 → 한 직선 위";
            listTxt[0].text = (perp ? "변 AB의 수직이등분선" : "∠A의 이등분선") + "  ✓"; listTxt[0].color = Cream; listBar[0].color = perp ? PerpCol : BisCol;
            listTxt[1].text = perp ? "그러면 세 꼭짓점에서 거리가 같은 외심은?" : "그러면 세 변에서 거리가 같은 내심은?";
            listTxt[1].color = new Color(1, 1, 1, 0.75f);
            listTxt[2].text = perp ? "→ 세 변의 수직이등분선이 만나는 점" : "→ 세 내각의 이등분선이 만나는 점";
            listTxt[2].color = perp ? PerpCol : BisCol;
            listBar[1].color = new Color(1, 1, 1, 0); listBar[2].color = new Color(1, 1, 1, 0);
            plantTxt.text = bridgeIdx == 0 ? "다음 →" : "2단계로 →";
            bridgeDone = true; busy = false;
            st.solved++;
            MgfBridge.NotifyChanged();
        }

        void NextBridge()
        {
            HideBridge();
            if (curPlate != null) curPlate.go.SetActive(false);
            curPlate = null; curPin = null;
            PlacePinsInTray();
            bannerBg.gameObject.SetActive(false);
            if (bridgeIdx == 0) { bridgeIdx = 1; SpawnPlate(gen.MakeBridge(Mission.In, rng)); }
            else StartBuild();
            MgfBridge.NotifyChanged();
        }

        void UpdateBridgeFx(float dt)
        {
            for (int i = 0; i < 3; i++)
            {
                if (dotT[i] > 0.5f) continue;
                dotT[i] += dt;
                float k = Mathf.Clamp01(dotT[i] / 0.35f);
                float s = k < 1 ? 1f + Mathf.Sin(k * Mathf.PI) * 0.6f : 1f;
                dotDisc[i].localScale = Vector3.one * 0.62f * s;
            }
            if (wobT < 0.6f) wobT += dt;
            if (step == Step.Bridge && cur != null && cur.m == Mission.Circum && !bridgeDone)
            {
                float a = 0.7f + 0.25f * Mathf.Sin(Time.time * 4f);
                for (int i = 0; i < 2; i++) SetColor(hl[i], new Color(1f, 0.93f, 0.62f, a));
            }
        }
    }
}
