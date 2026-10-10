// 딱 맞는 방울 — 화면 UI(HUD·발문 카드·거리 막대·안내·결과), 반응형 레이아웃(390×844 세로 ↔ 1280×800 가로), 화면↔판 좌표.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.TtakMatneunBangul
{
    public partial class TtakMatneunBangulGame
    {
        // ───────────────────────────── 레이아웃(stage 좌표)
        sealed class Spots
        {
            public Rect play, title;
            public Vector2 mirror, mirrorSize, faucet, seedRest, stripRoll, waitSpot, luluIdle, luluIdleR, towel, bars, toast, reveal, startHint;
            public Vector2[] niches = new Vector2[6];
            public Vector2[] leaves = new Vector2[6];
            public Vector2[] plants = new Vector2[3];
            public Vector2 bottle, sponge;
            public float waterLine, rimV, nicheScale = 1f;
            public bool stripVertical;
        }

        Spots spotsLand, spotsPort, S;
        bool land, layoutReady;
        int lastW = -1, lastH = -1;
        float camCu, camCv, camOrtho = 6f, tgtCu, tgtCv, tgtOrtho = 6f;
        float viewU0 = -5, viewU1 = 5, viewV0 = -5, viewV1 = 5;
        float zoomPunch; Vector2 zoomFocus;
        bool coverMode;
        float waterLineV = -4f;
        Vector2 stripRollStage;

        Spots MakeLand()
        {
            var s = new Spots();
            s.play = Rect.MinMaxRect(-8.4f, -5.1f, 10.6f, 6.0f);
            s.title = Rect.MinMaxRect(-8.4f, -4.5f, 10.6f, 9.9f);
            s.mirror = new Vector2(0f, 7.75f); s.mirrorSize = new Vector2(9.2f, 3.6f);
            s.faucet = new Vector2(-6.4f, 4.4f);
            s.seedRest = new Vector2(-6.4f, -2.0f);
            s.stripRoll = new Vector2(-6.4f, 1.0f); s.stripVertical = true;
            s.waitSpot = new Vector2(-6.6f, -4.45f);
            s.luluIdle = new Vector2(-4.6f, -3.0f); s.luluIdleR = new Vector2(.6f, .25f);
            s.towel = new Vector2(11.6f, 3.9f);
            s.plants[0] = new Vector2(-7.9f, -3.75f); s.plants[1] = new Vector2(9.9f, -3.75f); s.plants[2] = new Vector2(4.95f, -3.95f);
            s.bottle = new Vector2(8.3f, -3.55f); s.sponge = new Vector2(-4.85f, -3.95f);
            for (int i = 0; i < 6; i++) s.niches[i] = new Vector2(6.25f + (i % 3) * 1.7f, 1.25f - (i / 3) * 1.95f);
            s.nicheScale = 1.08f;
            s.leaves[0] = new Vector2(-8.1f, -3.9f); s.leaves[1] = new Vector2(-7.6f, -4.1f); s.leaves[2] = new Vector2(10.2f, -4.0f);
            s.leaves[3] = new Vector2(10.55f, -3.7f); s.leaves[4] = new Vector2(4.75f, -4.1f); s.leaves[5] = new Vector2(-8.2f, 3.2f);
            s.waterLine = -4.15f; s.rimV = -5.6f;
            s.bars = new Vector2(-6.4f, 1.2f);
            s.toast = new Vector2(0f, -4.5f);
            s.reveal = new Vector2(0f, 2.6f);
            s.startHint = new Vector2(-6.4f, 1.2f);
            return s;
        }

        Spots MakePort()
        {
            var s = new Spots();
            s.play = Rect.MinMaxRect(-4.6f, -11.8f, 4.6f, 8.1f);
            s.title = Rect.MinMaxRect(-4.6f, -7.4f, 4.6f, 12.5f);
            s.mirror = new Vector2(0f, 10.25f); s.mirrorSize = new Vector2(8.6f, 3.9f);
            s.faucet = new Vector2(3.1f, 5.3f);
            s.seedRest = new Vector2(2.9f, -4.75f);
            s.stripRoll = new Vector2(-2.7f, -4.75f); s.stripVertical = false;
            s.waitSpot = new Vector2(2.6f, -10.6f);
            s.luluIdle = new Vector2(-1.3f, -10.5f); s.luluIdleR = new Vector2(.7f, .18f);
            s.towel = new Vector2(-5.3f, 6.2f);
            s.plants[0] = new Vector2(-4.25f, -9.35f); s.plants[1] = new Vector2(4.3f, -9.4f); s.plants[2] = new Vector2(-4.4f, 4.25f);
            s.bottle = new Vector2(-9f, 0f); s.sponge = new Vector2(4.6f, 4.5f);
            for (int i = 0; i < 6; i++) s.niches[i] = new Vector2(-2.9f + (i % 3) * 2.9f, -6.55f - (i / 3) * 1.95f);
            s.nicheScale = 1.25f;
            s.leaves[0] = new Vector2(-4.4f, -9.6f); s.leaves[1] = new Vector2(-4.05f, -9.8f); s.leaves[2] = new Vector2(4.4f, -9.7f);
            s.leaves[3] = new Vector2(4.1f, -9.5f); s.leaves[4] = new Vector2(-4.6f, 4.4f); s.leaves[5] = new Vector2(4.65f, 3.9f);
            s.waterLine = -9.75f; s.rimV = -13.8f;
            s.bars = new Vector2(-2.25f, -4.75f);
            s.toast = new Vector2(0f, 4.5f);
            s.reveal = new Vector2(0f, 2.75f);
            s.startHint = new Vector2(0f, -4.75f);
            return s;
        }

        void ApplyLayout(float dt)
        {
            int W = Screen.width, H = Screen.height;
            if (W != lastW || H != lastH || !layoutReady)
            {
                lastW = W; lastH = H;
                if (spotsLand == null) { spotsLand = MakeLand(); spotsPort = MakePort(); }
                land = (float)W / Mathf.Max(1, H) >= 1.15f;
                S = land ? spotsLand : spotsPort;
                RelayoutWorld();
                ComputeCamTarget();
                if (!layoutReady) { camCu = tgtCu; camCv = tgtCv; camOrtho = tgtOrtho; }
                layoutReady = true;
                RelayoutUi();
                UpdateLayoutState();
            }
            float k = 1f - Mathf.Exp(-dt * 4.5f);
            camCu = Mathf.Lerp(camCu, tgtCu, k); camCv = Mathf.Lerp(camCv, tgtCv, k); camOrtho = Mathf.Lerp(camOrtho, tgtOrtho, k);
            zoomPunch = Mathf.MoveTowards(zoomPunch, 0f, dt * 1.6f);
            float zp = Mathf.Sin(Mathf.Clamp01(zoomPunch) * Mathf.PI) * .05f;
            Vector2 zc = Vector2.Lerp(new Vector2(camCu, camCv), zoomFocus, zp * 4f);
            cam.transform.rotation = stage.rotation;
            cam.transform.position = stage.TransformPoint(zc.x, zc.y, -CamDist);
            cam.orthographicSize = camOrtho * (1f - zp);
            float a = (float)W / Mathf.Max(1, H);
            viewU0 = camCu - camOrtho * a; viewU1 = camCu + camOrtho * a; viewV0 = camCv - camOrtho; viewV1 = camCv + camOrtho;
        }

        void ComputeCamTarget()
        {
            if (S == null) return;
            float a = (float)Screen.width / Mathf.Max(1, Screen.height);
            Rect r = phase == Phase.Title ? S.title : S.play;
            if (phase == Phase.Title && coverMode)
            {
                // 표지 캡처 전용 구도: 김 서린 거울(제목) + 꼭 맞은 방울 판만 꽉 차게
                float top = S.mirror.y + S.mirrorSize.y * .5f + .35f;
                r = Rect.MinMaxRect(-5.2f, -4.1f, 5.2f, top);
                if (a > r.width / r.height) { float w = r.height * a; r = new Rect(r.center.x - w / 2f, r.y, w, r.height); }
            }
            tgtOrtho = Mathf.Max(r.height * .5f, r.width * .5f / a);
            tgtCu = r.center.x; tgtCv = r.center.y;
            // 화면이 남으면(세로로 길쭉한 폰) 판이 위쪽 58% 안에 머물도록 아래 여백을 물 쪽으로 준다
            if (!land) tgtCv = r.yMax - tgtOrtho;
        }

        void SetScreen()
        {
            ComputeCamTarget();
            if (S != null) RelayoutUi();
            titleRoot.SetActive(phase == Phase.Title && !coverMode);
            hudRoot.SetActive(phase == Phase.Practice || phase == Phase.Play);
            endRoot.SetActive(phase == Phase.End);
            if (phase != Phase.Practice && phase != Phase.Play) { HideGuide(); goalRoot.SetActive(false); }
            if (phase == Phase.Title)
            {
                board = BangulRules.Practice();
                ApplyBoardVisuals(true);
                frameGrow = 1f;
                // 타이틀 주인공 장면: 세 변에 꼭 맞은 방울과 그 둘레를 도는 루루
                bubbleC = G2S(board.Ix, board.Iy);
                bubbleR = bubbleTargetR = (float)(board.r / BangulRules.Sub);
                bubbleOn = true; bubbleMat.color = Color.white;
                bubble.gameObject.SetActive(true); bubbleShadow.gameObject.SetActive(true);
                squash = 0f;
                logoReveal = 0f;
                for (int i = 0; i < 6; i++) marks[i].gameObject.SetActive(false);
                RefreshTitleUi();
                if (coverMode)
                {
                    // 표지: 이미 구조한 친구 셋 + 뗏목 위에서 기다리는 친구
                    for (int i = 0; i < 3; i++)
                    {
                        creatureState[i] = 3; creatures[i].gameObject.SetActive(true); nicheGhost[i].gameObject.SetActive(false);
                        nicheStamp[i].text = i == 0 ? "내심 I" : "외심 O"; nicheStamp[i].color = Deep;
                    }
                    ShowCreatureWaiting(3);
                }
            }
            if (phase == Phase.Practice) ShowCreatureWaiting(0);
            stripRoot.gameObject.SetActive(phase == Phase.Play && board != null && board.strips);
            UpdateLayoutState();
        }

        void RelayoutWorld()
        {
            if (coverMode) { S.mirror = new Vector2(0f, 5.95f); S.mirrorSize = new Vector2(8.4f, 3.5f); }
            mirror.localPosition = new Vector3(S.mirror.x, S.mirror.y, .5f);
            mirror.localScale = new Vector3(S.mirrorSize.x, S.mirrorSize.y, 1f);
            float logoH = .9f * S.mirrorSize.x / S.mirrorSize.y / 2.75f;
            logoQ.localScale = new Vector3(.9f, logoH, 1f);
            logoShadowQ.localScale = new Vector3(.9f, logoH, 1f);
            if (coverMode && !land) { S.faucet = new Vector2(5.55f, 2.6f); S.towel = new Vector2(-5.75f, 2.4f); S.plants[2] = new Vector2(-5.2f, -3.9f); S.sponge = new Vector2(5.5f, -3.6f); }
            faucet.localPosition = new Vector3(S.faucet.x, S.faucet.y, .2f);
            seedRest = S.seedRest;
            stripRollStage = S.stripRoll;
            stripRoot.localPosition = new Vector3(S.stripRoll.x, S.stripRoll.y, -.2f);
            for (int i = 0; i < 3; i++)
            {
                Vector2 o = S.stripVertical ? new Vector2(0f, (i - 1) * .55f) : new Vector2((i - 1) * .62f, 0f);
                stripRolls[i].localPosition = new Vector3(o.x, o.y, -.15f);
                stripRolls[i].localRotation = Quaternion.Euler(0f, 0f, S.stripVertical ? 90f : 0f);
            }
            stripCountLabel.transform.localPosition = S.stripVertical ? new Vector3(0f, -1.15f, -.4f) : new Vector3(1.35f, 0f, -.4f);
            seedTray.localPosition = new Vector3(S.seedRest.x, S.seedRest.y - .32f, -.1f);
            waitSpot = S.waitSpot;
            luluIdle = S.luluIdle; luluIdleR = S.luluIdleR;
            towel.localPosition = new Vector3(S.towel.x, S.towel.y, .1f);
            for (int i = 0; i < 6; i++) { niches[i].localPosition = new Vector3(S.niches[i].x, S.niches[i].y, .4f); niches[i].localScale = Vector3.one * S.nicheScale; }
            for (int i = 0; i < 3; i++) plants[i].localPosition = new Vector3(S.plants[i].x, S.plants[i].y, -.1f);
            bottle.localPosition = new Vector3(S.bottle.x, S.bottle.y, -.1f);
            sponge.localPosition = new Vector3(S.sponge.x, S.sponge.y, -.1f);
            waterLineV = S.waterLine;
            SetWaterLevel(S.waterLine, S.rimV);
            beam.localPosition = new Vector3(land ? 2f : 0f, land ? 1.5f : 0f, -1.6f);
        }

        // ───────────────────────────── 화면 ↔ stage
        Vector2 ScreenToStage(Vector2 sp)
        {
            float W = Mathf.Max(1, Screen.width), H = Mathf.Max(1, Screen.height), a = W / H;
            return new Vector2(camCu + (sp.x / W - .5f) * 2f * camOrtho * a, camCv + (sp.y / H - .5f) * 2f * camOrtho);
        }

        Vector2 StageToScreen(Vector2 s) => StageToScreen(s, camCu, camCv, camOrtho);

        Vector2 StageToScreen(Vector2 s, float cu, float cv, float ortho)
        {
            float W = Mathf.Max(1, Screen.width), H = Mathf.Max(1, Screen.height), a = W / H;
            return new Vector2(((s.x - cu) / (2f * ortho * a) + .5f) * W, ((s.y - cv) / (2f * ortho) + .5f) * H);
        }

        float PxPerUnit() => Screen.height / (2f * camOrtho);

        bool InBoard(Vector2 sp)
        {
            var s = ScreenToStage(sp);
            return Mathf.Abs(s.x) <= 4.35f && Mathf.Abs(s.y) <= 3.65f;
        }

        bool NearSeed(Vector2 sp) => Vector2.Distance(StageToScreen(seedRest), sp) < Mathf.Max(56f, .9f * PxPerUnit());

        bool NearStripRoll(Vector2 sp)
        {
            if (!stripRoot.gameObject.activeSelf) return false;
            var c = StageToScreen(stripRollStage);
            float rx = (S.stripVertical ? .6f : 1.25f) * PxPerUnit(), ry = (S.stripVertical ? 1.25f : .6f) * PxPerUnit();
            rx = Mathf.Max(rx, 40f); ry = Mathf.Max(ry, 40f);
            return Mathf.Abs(sp.x - c.x) < rx && Mathf.Abs(sp.y - c.y) < ry;
        }

        int TargetAt(Vector2 sp)
        {
            if (board == null) return -1;
            float best = Mathf.Max(30f, .42f * PxPerUnit());
            int bi = -1;
            for (int t = 0; t < 9; t++)
            {
                var p = StageToScreen(TargetStage(t));
                float d = Vector2.Distance(p, sp);
                if (d < best) { best = d; bi = t; }
            }
            return bi;
        }

        /// <summary>QA·포인터 봇용 화면 정보(목표 카메라 기준, 화면 비율 ×10000, 왼쪽 위 원점). 입력 없이 변하지 않는다.</summary>
        void UpdateLayoutState()
        {
            if (S == null || cam == null) return;
            int W = Mathf.Max(1, Screen.width), H = Mathf.Max(1, Screen.height);
            st.screenW = W; st.screenH = H;
            Func<Vector2, int[]> n = s =>
            {
                var p = StageToScreen(s, tgtCu, tgtCv, tgtOrtho);
                return new[] { Mathf.RoundToInt(p.x / W * 10000f), Mathf.RoundToInt((1f - p.y / H) * 10000f) };
            };
            if (board != null && phase != Phase.Title)
            {
                st.triPx = new int[6];
                for (int i = 0; i < 3; i++) { var q = n(G2S(board.V[i])); st.triPx[i * 2] = q[0]; st.triPx[i * 2 + 1] = q[1]; }
                st.targetPx = new int[18];
                for (int t = 0; t < 9; t++) { var q = n(TargetStage(t)); st.targetPx[t * 2] = q[0]; st.targetPx[t * 2 + 1] = q[1]; }
            }
            else { st.triPx = new int[0]; st.targetPx = new int[0]; }
            var b0 = n(new Vector2(-4.2f, 3.5f)); var b1 = n(new Vector2(4.2f, -3.5f));
            st.boardPx = new[] { b0[0], b0[1], b1[0], b1[1] };
            st.seedPx = n(seedRest);
            st.stripPx = n(stripRollStage);
        }

        // ───────────────────────────── UI 생성
        Sprite roundSpr, circleSpr, ringSpr;
        GameObject titleRoot, hudRoot, endRoot, goalRoot;
        RectTransform canvasRt, promptRt, toastRt, barsRt, revealRt, hudRowRt, goalRt, endCardRt, replayRt, startHintRt, badgeRt, bestRt, guideRingRt;
        TextMeshProUGUI promptUi, describeUi, heartsUi, counterUi, timerUi, toastUi, goalUi, revealUi, revealSubUi;
        TextMeshProUGUI badgeUi, startHintUi, bestUi, endTitleUi, endStatsUi, endNoteUi, replayUi;
        readonly TextMeshProUGUI[] barLabel = new TextMeshProUGUI[3];
        readonly RectTransform[] barFill = new RectTransform[3];
        readonly RectTransform[] barTrack = new RectTransform[3];
        Image timerFill, guideRing, replayImg;
        readonly RectTransform[] chevrons = new RectTransform[3];
        readonly Image[] uiRipples = new Image[6];
        readonly float[] uiRippleLife = new float[6];
        int uiRippleCursor;
        float toastLeft, revealLeft, guideBig, heartPunch, displayScore;
        bool guideOn;
        int lastSecond = -1;
        Transform stripRoot, seedTray;
        float endT;
        string endReasonShown = "";

        void BuildUi()
        {
            roundSpr = RoundSprite(64, 22f, false);
            circleSpr = RoundSprite(64, 31f, false);
            ringSpr = RoundSprite(64, 31f, true);
            var canvas = MgfText.Canvas;
            canvasRt = (RectTransform)canvas.transform;
            titleRoot = Root("TitleUI"); hudRoot = Root("HudUI"); endRoot = Root("EndUI");
            var goalGo = Root("GoalUI"); goalRoot = goalGo;

            // ── HUD(맨 위 한 줄) + 발문 카드
            hudRowRt = Panel("HudRow", hudRoot.transform, new Color(1f, 1f, 1f, .0f));
            heartsUi = Txt("♥♥♥", hudRowRt, 18f, Coral, TextAlignmentOptions.Left);
            heartsUi.characterSpacing = -4f;
            heartsUi.outlineWidth = .2f; heartsUi.outlineColor = new Color32(255, 255, 255, 255);
            counterUi = Txt("", hudRowRt, 15f, Deep, TextAlignmentOptions.Center);
            counterUi.outlineWidth = .22f; counterUi.outlineColor = new Color32(255, 250, 230, 255);
            timerUi = Txt("2:00", hudRowRt, 16f, Deep, TextAlignmentOptions.Right);
            timerUi.outlineWidth = .22f; timerUi.outlineColor = new Color32(255, 250, 230, 255);
            var tb = Panel("TimerTrack", hudRowRt, new Color(Teal.r, Teal.g, Teal.b, .22f));
            Place(tb, new Vector2(1f, .5f), new Vector2(-46f, -15f), new Vector2(80f, 6f));
            var tf = Panel("TimerFill", tb, Teal); timerFill = tf.GetComponent<Image>();
            tf.anchorMin = new Vector2(0f, 0f); tf.anchorMax = new Vector2(1f, 1f); tf.pivot = new Vector2(0f, .5f); tf.offsetMin = tf.offsetMax = Vector2.zero;

            promptRt = Panel("PromptCard", hudRoot.transform, new Color(1f, 1f, 1f, .94f));
            var po = promptRt.gameObject.AddComponent<Outline>(); po.effectColor = new Color(Teal.r, Teal.g, Teal.b, .55f); po.effectDistance = new Vector2(2f, -2f);
            promptUi = Txt("", promptRt, 17f, Deep, TextAlignmentOptions.Center);
            describeUi = Txt("", promptRt, 12.5f, Teal, TextAlignmentOptions.Center);
            promptUi.enableAutoSizing = true; promptUi.fontSizeMin = 12f; promptUi.fontSizeMax = 17f;
            describeUi.enableAutoSizing = true; describeUi.fontSizeMin = 9f; describeUi.fontSizeMax = 12.5f;

            // ── 거리 막대(R1~R2): 세 길이를 직접 비교(같은 흰색, 정답 근접 신호 없음)
            barsRt = Panel("DistanceBars", hudRoot.transform, new Color(Teal.r * .8f, Teal.g * .8f, Teal.b * .8f, .78f));
            var bt = Txt("씨앗에서 …까지", barsRt, 11f, Color.white, TextAlignmentOptions.Center);
            Place(bt.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -10f), new Vector2(170f, 18f));
            barsTitle = bt;
            for (int i = 0; i < 3; i++)
            {
                barLabel[i] = Txt("BC", barsRt, 12f, Color.white, TextAlignmentOptions.Left);
                Place(barLabel[i].rectTransform, new Vector2(0f, 1f), new Vector2(26f, -32f - i * 19f), new Vector2(40f, 18f));
                var track = Panel("Track" + i, barsRt, new Color(1f, 1f, 1f, .18f));
                Place(track, new Vector2(0f, 1f), new Vector2(92f, -32f - i * 19f), new Vector2(100f, 9f));
                barTrack[i] = track;
                var fill = Panel("Fill" + i, track, Color.white);
                fill.anchorMin = new Vector2(0f, 0f); fill.anchorMax = new Vector2(0f, 1f); fill.pivot = new Vector2(0f, .5f);
                fill.anchoredPosition = Vector2.zero; fill.sizeDelta = new Vector2(0f, 0f);
                barFill[i] = fill;
            }

            // ── 토스트·풀이 카드
            toastRt = Panel("Toast", hudRoot.transform, new Color(Deep.r, Deep.g, Deep.b, .9f));
            toastUi = Txt("", toastRt, 13.5f, Color.white, TextAlignmentOptions.Center);
            toastUi.textWrappingMode = TextWrappingModes.Normal;
            Stretch(toastUi.rectTransform, 8f);
            toastRt.gameObject.SetActive(false);
            revealRt = Panel("RevealCard", hudRoot.transform, new Color(1f, .99f, .93f, .96f));
            var ro = revealRt.gameObject.AddComponent<Outline>(); ro.effectColor = new Color(.96f, .78f, .25f, .9f); ro.effectDistance = new Vector2(2.5f, -2.5f);
            revealUi = Txt("", revealRt, 15f, Deep, TextAlignmentOptions.Center);
            revealSubUi = Txt("", revealRt, 13.5f, Teal, TextAlignmentOptions.Center);
            Place(revealUi.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -18f), new Vector2(350f, 24f));
            Place(revealSubUi.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, 17f), new Vector2(350f, 22f));
            revealRt.gameObject.SetActive(false);

            // ── 목표 배너(연습)
            goalRt = Panel("GoalBanner", goalGo.transform, new Color(1f, .9f, .5f, .97f));
            var go2 = goalRt.gameObject.AddComponent<Outline>(); go2.effectColor = new Color(Deep.r, Deep.g, Deep.b, .5f); go2.effectDistance = new Vector2(2f, -2f);
            goalUi = Txt("", goalRt, 15f, Deep, TextAlignmentOptions.Center);
            Stretch(goalUi.rectTransform, 4f);
            goalRoot.SetActive(false);

            // ── 안내: 씨앗 맥동 고리 + 셰브론 경로
            var gr = Panel("GuideRing", goalGo.transform.parent, Color.white);
            guideRing = gr.GetComponent<Image>(); guideRing.sprite = ringSpr; guideRing.color = new Color(Teal.r, Teal.g, Teal.b, .95f);
            guideRingRt = gr; gr.anchorMin = gr.anchorMax = Vector2.zero; gr.gameObject.SetActive(false);
            for (int i = 0; i < chevrons.Length; i++)
            {
                var ch = Panel("GuideDot" + i, goalGo.transform.parent, new Color(Teal.r, Teal.g, Teal.b, .95f));
                ch.GetComponent<Image>().sprite = circleSpr; ch.GetComponent<Image>().type = Image.Type.Simple;
                var cho = ch.gameObject.AddComponent<Outline>(); cho.effectColor = Color.white; cho.effectDistance = new Vector2(1.5f, -1.5f);
                chevrons[i] = ch; chevrons[i].anchorMin = chevrons[i].anchorMax = Vector2.zero; chevrons[i].sizeDelta = new Vector2(13f, 13f);
                chevrons[i].gameObject.SetActive(false);
            }
            ghostFingerRt = Panel("GhostFinger", goalGo.transform.parent, new Color(1f, 1f, 1f, .85f));
            ghostFingerRt.GetComponent<Image>().sprite = circleSpr;
            ghostFingerRt.anchorMin = ghostFingerRt.anchorMax = Vector2.zero; ghostFingerRt.sizeDelta = new Vector2(34f, 34f);
            var gfo = ghostFingerRt.gameObject.AddComponent<Outline>(); gfo.effectColor = new Color(Deep.r, Deep.g, Deep.b, .7f); gfo.effectDistance = new Vector2(2f, -2f);
            ghostFingerRt.gameObject.SetActive(false);
            for (int i = 0; i < uiRipples.Length; i++)
            {
                var r = Panel("TapRipple" + i, goalGo.transform.parent, Color.white);
                uiRipples[i] = r.GetComponent<Image>(); uiRipples[i].sprite = ringSpr;
                r.anchorMin = r.anchorMax = Vector2.zero; r.gameObject.SetActive(false);
            }

            // ── 타이틀
            badgeRt = Panel("UnitBadge", titleRoot.transform, new Color(Deep.r, Deep.g, Deep.b, .88f));
            badgeUi = Txt("중2 · 삼각형의 성질 · 내심과 외심", badgeRt, 12.5f, Color.white, TextAlignmentOptions.Center);
            Stretch(badgeUi.rectTransform, 4f);
            startHintRt = Panel("StartHint", titleRoot.transform, new Color(Deep.r, Deep.g, Deep.b, .93f));
            var sho = startHintRt.gameObject.AddComponent<Outline>(); sho.effectColor = new Color(1f, 1f, 1f, .9f); sho.effectDistance = new Vector2(2f, -2f);
            startHintUi = Txt("방울 씨앗을 눌러 시작", startHintRt, 17f, Color.white, TextAlignmentOptions.Center);
            Stretch(startHintUi.rectTransform, 0f);
            bestRt = Panel("Best", titleRoot.transform, new Color(1f, 1f, 1f, 0f));
            bestUi = Txt("", bestRt, 12.5f, Teal, TextAlignmentOptions.Center);
            bestUi.outlineWidth = .22f; bestUi.outlineColor = new Color32(255, 255, 255, 255);
            Stretch(bestUi.rectTransform, 0f);

            // ── 결과
            endCardRt = Panel("ResultCard", endRoot.transform, new Color(1f, .99f, .94f, .97f));
            var eo = endCardRt.gameObject.AddComponent<Outline>(); eo.effectColor = new Color(.96f, .78f, .25f, 1f); eo.effectDistance = new Vector2(3f, -3f);
            endTitleUi = Txt("", endCardRt, 24f, Deep, TextAlignmentOptions.Center);
            endStatsUi = Txt("", endCardRt, 16f, Deep, TextAlignmentOptions.Center);
            endStatsUi.textWrappingMode = TextWrappingModes.Normal;
            endNoteUi = Txt("", endCardRt, 12.5f, Teal, TextAlignmentOptions.Center);
            endNoteUi.textWrappingMode = TextWrappingModes.Normal;
            replayRt = Panel("Replay", endCardRt, Color.white);
            replayImg = replayRt.GetComponent<Image>(); replayImg.sprite = circleSpr; replayImg.type = Image.Type.Simple; replayImg.color = new Color(.75f, .95f, 1f, 1f);
            var ro2 = replayRt.gameObject.AddComponent<Outline>(); ro2.effectColor = new Color(Teal.r, Teal.g, Teal.b, .9f); ro2.effectDistance = new Vector2(3f, -3f);
            replayUi = Txt("다시\n구조", replayRt, 16f, Deep, TextAlignmentOptions.Center);
            Stretch(replayUi.rectTransform, 2f);
            Place(endTitleUi.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -34f), new Vector2(330f, 40f));
            Place(endStatsUi.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -112f), new Vector2(320f, 100f));
            Place(endNoteUi.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -190f), new Vector2(320f, 46f));
            Place(replayRt, new Vector2(.5f, 0f), new Vector2(0f, 62f), new Vector2(92f, 92f));
            endRoot.SetActive(false);

            BuildStrips();
        }

        TextMeshProUGUI barsTitle;
        RectTransform ghostFingerRt;

        void BuildStrips()
        {
            stripRoot = Child("StripTray", stage, Vector3.zero);
            var trayMat = MgfLook.Lit(MgfLook.Hex("FFFFFF"), .8f, 0f, MgfLook.Hex("15150C"));
            var ribbon = MgfLook.Lit(MgfLook.Hex("8FE0C8"), .5f, 0f, MgfLook.Hex("10382C"));
            var ribbon2 = MgfLook.Lit(MgfLook.Hex("FFF1A8"), .5f, 0f, MgfLook.Hex("3A3010"));
            for (int i = 0; i < 3; i++)
            {
                var h = Child("Roll" + i, stripRoot, Vector3.zero);
                BlockAt("Coil", h, Vector3.zero, new Vector3(.95f, .42f, .42f), .2f, ribbon);
                BlockAt("Band", h, new Vector3(0f, 0f, -.02f), new Vector3(.22f, .46f, .46f), .08f, ribbon2);
                stripRolls[i] = h;
            }
            stripCountLabel = WorldText("접기 띠 3", stripRoot, Vector3.zero, 3.4f, Deep, 30);
            stripCountLabel.outlineWidth = .2f; stripCountLabel.outlineColor = new Color32(255, 255, 255, 255);
            stripRoot.gameObject.SetActive(false);
            // 씨앗 받침(분홍 조개 접시)
            seedTray = Child("SeedDish", stage, Vector3.zero);
            BlockAt("Dish", seedTray, Vector3.zero, new Vector3(1.2f, .3f, .7f), .14f, MgfLook.Lit(MgfLook.Hex("FFC2CE"), .7f, 0f, MgfLook.Hex("3A1820")));
        }

        GameObject Root(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(MgfText.Canvas.transform, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        RectTransform Panel(string name, Transform parent, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = roundSpr; img.type = Image.Type.Sliced; img.color = c; img.raycastTarget = false;
            return (RectTransform)go.transform;
        }

        TextMeshProUGUI Txt(string s, Transform parent, float size, Color c, TextAlignmentOptions al)
        {
            var go = new GameObject("T", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = c; t.alignment = al; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.fontStyle = FontStyles.Bold;
            t.text = s;
            return t;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(.5f, .5f);
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        static Sprite RoundSprite(int S, float rad, bool ring)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = ring ? "Ring" : "Round" };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float cx = Mathf.Clamp(x + .5f, rad, S - rad), cy = Mathf.Clamp(y + .5f, rad, S - rad);
                    float d = Mathf.Sqrt((x + .5f - cx) * (x + .5f - cx) + (y + .5f - cy) * (y + .5f - cy));
                    float a = Mathf.Clamp01(rad - d + .5f);
                    if (ring) a *= Mathf.Clamp01(d - (rad - 5f) + .5f);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply(false, true);
            float b = ring ? 0f : rad;
            return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        float UiScale() => Mathf.Sqrt((Screen.width / 390f) * (Screen.height / 844f));

        Vector2 ScreenToCanvas(Vector2 sp) => sp / Mathf.Max(.01f, UiScale());

        void PlaceAtStage(RectTransform rt, Vector2 stagePos, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(.5f, .5f);
            var p = StageToScreen(stagePos, tgtCu, tgtCv, tgtOrtho);
            rt.anchoredPosition = ScreenToCanvas(p) + offset; rt.sizeDelta = size;
        }

        void RelayoutUi()
        {
            if (S == null) return;
            float cw = Screen.width / Mathf.Max(.01f, UiScale());
            if (land)
            {
                var bc = ScreenToCanvas(StageToScreen(Vector2.zero, tgtCu, tgtCv, tgtOrtho));
                Place(hudRowRt, new Vector2(0f, 1f), new Vector2(bc.x, -18f), new Vector2(420f, 30f));
                Place(promptRt, new Vector2(0f, 1f), new Vector2(bc.x, -58f), new Vector2(410f, 52f));
                Place(goalRt, new Vector2(0f, 1f), new Vector2(bc.x, -18f), new Vector2(360f, 28f));
            }
            else
            {
                // 오른쪽 위 64px 는 HTML 음소거 버튼 자리
                Place(hudRowRt, new Vector2(0f, 1f), new Vector2(8f + 150f, -22f), new Vector2(300f, 30f));
                Place(promptRt, new Vector2(.5f, 1f), new Vector2(0f, -92f), new Vector2(Mathf.Min(374f, cw - 14f), 58f));
                Place(goalRt, new Vector2(0f, 1f), new Vector2(8f + 150f, -22f), new Vector2(300f, 28f));
            }
            Place(heartsUi.rectTransform, new Vector2(0f, .5f), new Vector2(30f, 0f), new Vector2(60f, 28f));
            Place(counterUi.rectTransform, new Vector2(.5f, .5f), new Vector2(8f, 0f), new Vector2(170f, 28f));
            Place(timerUi.rectTransform, new Vector2(1f, .5f), new Vector2(-24f, 3f), new Vector2(50f, 24f));
            counterUi.fontSize = 13f;
            float pw = promptRt.sizeDelta.x - 16f;
            Place(promptUi.rectTransform, new Vector2(.5f, 1f), new Vector2(0f, -17f), new Vector2(pw, 24f));
            Place(describeUi.rectTransform, new Vector2(.5f, 0f), new Vector2(0f, 13f), new Vector2(pw, 20f));
            PlaceAtStage(barsRt, S.bars, new Vector2(land ? 150f : 176f, 92f), Vector2.zero);
            for (int i = 0; i < 3; i++) { barTrack[i].anchoredPosition = new Vector2(land ? 84f : 100f, -32f - i * 19f); barTrack[i].sizeDelta = new Vector2(land ? 88f : 116f, 9f); }
            PlaceAtStage(toastRt, S.toast, new Vector2(330f, 44f), Vector2.zero);
            PlaceAtStage(revealRt, S.reveal, new Vector2(366f, 70f), Vector2.zero);
            // 타이틀
            if (land)
            {
                PlaceAtStage(badgeRt, new Vector2(S.mirror.x, S.mirror.y - S.mirrorSize.y * .5f - .42f), new Vector2(230f, 24f), Vector2.zero);
                PlaceAtStage(startHintRt, S.startHint, new Vector2(124f, 56f), Vector2.zero);
                startHintUi.text = "방울 씨앗을\n눌러 시작";
                PlaceAtStage(bestRt, S.startHint + new Vector2(0f, -1.55f), new Vector2(210f, 22f), Vector2.zero);
                bestUi.textWrappingMode = TextWrappingModes.NoWrap;
                startHintUi.textWrappingMode = TextWrappingModes.Normal;
            }
            else
            {
                PlaceAtStage(badgeRt, new Vector2(0f, S.mirror.y - S.mirrorSize.y * .5f - .5f), new Vector2(240f, 24f), Vector2.zero);
                PlaceAtStage(startHintRt, S.startHint, new Vector2(230f, 40f), Vector2.zero);
                startHintUi.text = "방울 씨앗을 눌러 시작";
                PlaceAtStage(bestRt, S.startHint + new Vector2(0f, -.9f), new Vector2(300f, 22f), Vector2.zero);
            }
            Place(endCardRt, new Vector2(.5f, .5f), new Vector2(0f, land ? 0f : 40f), new Vector2(350f, 330f));
        }

        // ───────────────────────────── UI 갱신
        void RefreshPromptUi()
        {
            if (board == null) return;
            promptUi.text = board.Instruction;
            string d = board.describe;
            if (board.lens != null) d += "  (BC=" + board.lens[0] + " cm, CA=" + board.lens[1] + " cm, AB=" + board.lens[2] + " cm)";
            describeUi.text = d;
            for (int i = 0; i < 3; i++) barLabel[i].text = board.kind == Kind.In ? BangulRules.SideNames[i] : BangulRules.Names[i];
            barsTitle.text = board.kind == Kind.In ? "씨앗에서 세 변까지" : "씨앗에서 세 꼭짓점까지";
        }

        void RefreshHud()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < BangulRules.StartHearts; i++) sb.Append(i < st.lives ? "♥" : "♡");
            heartsUi.text = sb.ToString();
            stripCountLabel.text = "접기 띠 " + st.strips;
            for (int i = 0; i < 3; i++) stripRolls[i].gameObject.SetActive(i < st.strips);
        }

        void RefreshTitleUi()
        {
            int best = 0, bestScore = 0;
            try { best = PlayerPrefs.GetInt("ttak.bestFirst", 0); bestScore = PlayerPrefs.GetInt("ttak.bestScore", 0); } catch (Exception) { }
            bestUi.text = best > 0 || bestScore > 0 ? "최고 첫 시도 " + best + "/6 · 점수 " + bestScore : "한 판 2분 · 비누 친구 6마리";
        }

        void ShowGoal(string s)
        {
            if (string.IsNullOrEmpty(s)) { goalRoot.SetActive(false); hudRowRt.gameObject.SetActive(true); return; }
            goalUi.text = s;
            goalRoot.SetActive(true);
            hudRowRt.gameObject.SetActive(phase != Phase.Practice);
            MgfFx.Punch(goalRt, .08f, .25f);
        }

        void ShowToast(string s, float dur)
        {
            toastUi.text = s;
            toastLeft = dur;
            toastRt.gameObject.SetActive(true);
            MgfFx.Punch(toastRt, .06f, .2f);
        }

        void ShowReveal()
        {
            string a, b = "";
            if (board.kind == Kind.In)
            {
                a = "세 변까지 거리가 같다 → 내심 I";
                if (board.ang[0] > 0)
                {
                    int twice = 180 + board.ang[0];   // 2·(90 + A/2)
                    b = "∠BIC = 90° + ½∠A = " + (twice % 2 == 0 ? (twice / 2).ToString() : (twice / 2) + ".5") + "°";
                }
            }
            else
            {
                a = "세 꼭짓점까지 거리가 같다 → 외심 O";
                if (board.shape == Shape.Right) b = "직각삼각형의 외심 = 빗변의 중점 · OA=OB=OC=" + Half(board.lens[2]) + " cm";
                else if (board.ang[0] >= 90 || board.ang[1] >= 90 || board.ang[2] >= 90) b = "둔각삼각형의 외심은 삼각형의 바깥에 있다";
                else b = "∠BOC = 2∠A = " + (2 * board.ang[0]) + "°";
            }
            revealUi.text = a; revealSubUi.text = b;
            revealLeft = 0f;
            revealPending = 1.35f;
        }

        static string Half(int v) => v % 2 == 0 ? (v / 2).ToString() : (v / 2) + ".5";

        float revealPending = -1f;

        void HeartBreakFx()
        {
            if (phase != Phase.Play) return;
            heartPunch = 1f;
            MgfFx.Punch(heartsUi.rectTransform, .35f, .35f);
        }

        void ShowGuide()
        {
            guideOn = true; guideBig = 0f;
        }

        void HideGuide()
        {
            guideOn = false;
            if (guideRing) guideRing.gameObject.SetActive(false);
            for (int i = 0; i < chevrons.Length; i++) if (chevrons[i]) chevrons[i].gameObject.SetActive(false);
        }

        void ReplayGuide(bool big)
        {
            if (phase != Phase.Practice && !(phase == Phase.Play && st.firstTryTotal == 0)) { if (phase == Phase.Play) PointAtSeed(); return; }
            guideOn = true; guideBig = big ? 1f : 0f;
        }

        void PointAtSeed()
        {
            guideOn = true; guideBig = .6f;
            guideAutoHide = 2.2f;
        }

        float guideAutoHide = -1f;

        void PointAtReplay() { MgfFx.Punch(replayRt, .2f, .3f); }

        void SetGhostFinger(Vector2 stagePos, bool pressed)
        {
            ghostFingerRt.gameObject.SetActive(true);
            ghostFingerRt.anchoredPosition = ScreenToCanvas(StageToScreen(stagePos)) + new Vector2(10f, -14f);
            float s = pressed ? 30f : 38f;
            ghostFingerRt.sizeDelta = new Vector2(s, s);
        }

        void Ripple(Vector2 sp)
        {
            int i = uiRippleCursor++ % uiRipples.Length;
            uiRippleLife[i] = .45f;
            var rt = uiRipples[i].rectTransform;
            rt.anchoredPosition = ScreenToCanvas(sp);
            rt.sizeDelta = new Vector2(20f, 20f);
            uiRipples[i].gameObject.SetActive(true);
            Sfx.Play("tap", .08f);
        }

        void TitleTouch(Vector2 sp)
        {
            Ripple(sp);
            titleGrab = true;
            Sfx.Play("pop", .25f);
            SpawnRingAtStage(G2S(BangulRules.Practice().Ix, BangulRules.Practice().Iy), 2.4f);
        }

        bool titleGrab;

        bool InReplay(Vector2 sp)
        {
            if (!endRoot.activeSelf) return false;
            var c = new Vector2(replayRt.position.x, replayRt.position.y);
            float r = 46f * UiScale() + 12f;
            return Vector2.Distance(c, sp) < r;
        }

        void PressReplay()
        {
            Sfx.Play("pop", .3f);
            Ripple(new Vector2(replayRt.position.x, replayRt.position.y));
        }

        void ShowEnd(string reason)
        {
            endReasonShown = reason;
            endT = 0f;
            bool done = reason == "done";
            endTitleUi.text = done ? (st.mastery ? "구조 완료 · 숙련!" : "구조 완료") : (reason == "time" ? "시간 종료 · 구조 중단" : "하트 소진 · 구조 중단");
            endTitleUi.color = done ? Deep : MgfLook.Hex("B5465C");
            string note;
            if (done && !st.mastery) note = "첫 판단 기준 미달 — 첫 시도 " + BangulRules.MasteryFirst + "/6 이상이 숙련이다.";
            else if (done) note = "세 변·세 꼭짓점까지의 같은 거리를 첫눈에 읽었다.";
            else note = "남은 생물은 다음 판에 다시 구조하자.";
            string log = MostMissed();
            if (log != "") note += "\n루루의 김서림 기록: " + log;
            endNoteUi.text = note;
            displayScore = 0f;
            MgfFx.Punch(endCardRt, .08f, .35f);
        }

        string MostMissed()
        {
            if (missIds.Count == 0) return "";
            string best = ""; int bc = 0;
            foreach (var id in missIds)
            {
                int c = 0; foreach (var j in missIds) if (j == id) c++;
                if (c > bc) { bc = c; best = id; }
            }
            return BangulRules.MisconceptionLabel(best) + " " + bc + "회";
        }

        void UpdateUi(float dt)
        {
            // HUD
            if (phase == Phase.Play || phase == Phase.Practice)
            {
                int sec = Mathf.CeilToInt(timeLeft);
                if (sec != lastSecond)
                {
                    lastSecond = sec;
                    timerUi.text = phase == Phase.Practice ? "연습" : (sec / 60) + ":" + (sec % 60).ToString("00");
                    timerUi.color = sec <= 15 && phase == Phase.Play ? MgfLook.Hex("C9445E") : Deep;
                    if (sec <= 10 && sec > 0 && phase == Phase.Play && seq == Seq.None) Sfx.Play("tick", .12f);
                }
                var tf = timerFill.rectTransform;
                tf.anchorMax = new Vector2(Mathf.Clamp01(timeLeft / BangulRules.RunSeconds), 1f);
                displayScore = Mathf.MoveTowards(displayScore, st.score, Mathf.Max(60f, Mathf.Abs(st.score - displayScore) * 6f) * dt);
                string c = phase == Phase.Practice ? "연습 판 · 하트·시간 잠김" : "구조 " + st.rescued + "/6 · 첫 시도 " + st.firstTryCorrect + " · " + Mathf.RoundToInt(displayScore) + "점";
                if (counterUi.text != c) counterUi.text = c;
                barsRt.gameObject.SetActive(board != null && board.bars && barsVisible);
                if (barsRt.gameObject.activeSelf)
                {
                    float maxRef = 0.01f;
                    for (int i = 0; i < 3; i++) maxRef = Mathf.Max(maxRef, barValue[i]);
                    float tw = barTrack[0].sizeDelta.x;
                    float scale = tw / Mathf.Max(2.6f, maxRef);
                    for (int i = 0; i < 3; i++) barFill[i].sizeDelta = new Vector2(Mathf.Min(tw, barValue[i] * scale), 0f);
                }
                promptRt.gameObject.SetActive(board != null);
            }
            if (heartPunch > 0f) { heartPunch -= dt * 2f; heartsUi.color = Color.Lerp(Coral, MgfLook.Hex("C9445E"), Mathf.Clamp01(heartPunch)); }
            // 토스트
            if (toastLeft > 0f) { toastLeft -= dt; if (toastLeft <= 0f) toastRt.gameObject.SetActive(false); }
            // 풀이 카드
            if (revealPending > 0f)
            {
                revealPending -= dt;
                if (revealPending <= 0f) { revealRt.gameObject.SetActive(true); revealLeft = 2.2f; MgfFx.Punch(revealRt, .1f, .3f); Sfx.Play("tok", .2f); }
            }
            if (revealLeft > 0f) { revealLeft -= dt; if (revealLeft <= 0f) revealRt.gameObject.SetActive(false); }
            // 안내
            if (guideOn && (phase == Phase.Practice || phase == Phase.Play) && seq == Seq.None && drag == Drag.None)
            {
                if (guideAutoHide > 0f) { guideAutoHide -= dt; if (guideAutoHide <= 0f) { HideGuide(); } }
                var sp = ScreenToCanvas(StageToScreen(seedRest));
                float pulse = .5f + .5f * Mathf.Sin(worldT * 6f);
                float sz = (64f + 22f * pulse) * (1f + guideBig * .6f);
                guideRing.gameObject.SetActive(guideOn);
                guideRingRt.anchoredPosition = sp; guideRingRt.sizeDelta = new Vector2(sz, sz);
                guideRing.color = new Color(Teal.r, Teal.g, Teal.b, .55f + .4f * pulse);
                // 씨앗에서 판 쪽으로 가는 짧은 경로(정답 위치가 아니라 판 한가운데 쪽, 45% 지점까지)
                var to = ScreenToCanvas(StageToScreen(Vector2.zero));
                Vector2 dir = (to - sp);
                float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                for (int i = 0; i < chevrons.Length; i++)
                {
                    float f = Mathf.Repeat(worldT * .9f + i / 3f, 1f);
                    chevrons[i].gameObject.SetActive(guideOn);
                    chevrons[i].anchoredPosition = sp + dir * (.12f + f * .33f);
                    var dimg = chevrons[i].GetComponent<Image>();
                    dimg.color = new Color(Teal.r, Teal.g, Teal.b, Mathf.Sin(f * Mathf.PI));
                    chevrons[i].sizeDelta = Vector2.one * (10f + 6f * Mathf.Sin(f * Mathf.PI));
                }
            }
            else if (guideRing.gameObject.activeSelf && (seq != Seq.None || drag != Drag.None))
            {
                guideRing.gameObject.SetActive(false);
                for (int i = 0; i < chevrons.Length; i++) chevrons[i].gameObject.SetActive(false);
            }
            if (seq != Seq.Demo && ghostFingerRt.gameObject.activeSelf) ghostFingerRt.gameObject.SetActive(false);
            // 탭 잔물결
            for (int i = 0; i < uiRipples.Length; i++)
            {
                if (uiRippleLife[i] <= 0f) continue;
                uiRippleLife[i] -= dt;
                float k = 1f - uiRippleLife[i] / .45f;
                uiRipples[i].rectTransform.sizeDelta = Vector2.one * (20f + k * 70f);
                uiRipples[i].color = new Color(Teal.r, Teal.g, Teal.b, (1f - k) * .9f);
                if (uiRippleLife[i] <= 0f) uiRipples[i].gameObject.SetActive(false);
            }
            // 타이틀 숨쉬기
            if (phase == Phase.Title)
            {
                float p = .5f + .5f * Mathf.Sin(worldT * 3f);
                startHintRt.localScale = Vector3.one * (1f + p * .06f);
                startHintUi.alpha = .75f + .25f * p;
            }
            // 결과 카운트업
            if (phase == Phase.End)
            {
                endT += dt;
                float k = Mathf.Clamp01(endT / 1.1f);
                int sc = Mathf.RoundToInt(st.score * EaseOutCubic(k));
                int resc = Mathf.RoundToInt(st.rescued * EaseOutCubic(Mathf.Clamp01(endT / .7f)));
                int ft = Mathf.RoundToInt(st.firstTryCorrect * EaseOutCubic(Mathf.Clamp01((endT - .2f) / .7f)));
                string s = "구조 " + resc + "/6\n첫 시도 정답 " + ft + "/" + Mathf.Max(st.firstTryTotal, 0) + "\n점수 " + sc;
                if (endStatsUi.text != s) endStatsUi.text = s;
                float p = .5f + .5f * Mathf.Sin(worldT * 3.2f);
                replayRt.localScale = Vector3.one * (1f + p * .05f);
            }
        }
    }

    /// <summary>합성 효과음(에셋 없음). 레몬 욕조의 물·유리·비누막 소리. 첫 사용자 제스처 뒤에만 들린다(브라우저 정책).</summary>
    public static class Sfx
    {
        static AudioSource src;
        static readonly System.Collections.Generic.Dictionary<string, AudioClip> clips = new System.Collections.Generic.Dictionary<string, AudioClip>();
        const int Rate = 44100;

        public static void Play(string kind, float vol)
        {
            if (!src)
            {
                var go = new GameObject("BangulSfx");
                UnityEngine.Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>();
                src.playOnAwake = false; src.spatialBlend = 0f;
            }
            AudioClip c;
            if (!clips.TryGetValue(kind, out c)) clips[kind] = c = Make(kind);
            if (c) src.PlayOneShot(c, vol);
        }

        static AudioClip Make(string kind)
        {
            switch (kind)
            {
                case "grow": return Notes(kind, new[] { 784f, 988f, 1319f }, .1f, .03f, 3f);
                case "tok": return Drop(kind, 1250f, 620f, .09f);
                case "harp": return Notes(kind, new[] { 523f, 659f, 784f, 988f, 1175f, 1319f, 1568f, 2093f }, .05f, .0f, 6f);
                case "rub": return Rub(kind, .38f);
                case "pop": return Pop(kind);
                case "fold": return Swish(kind, .22f);
                case "refuse": return Notes(kind, new[] { 330f, 262f }, .07f, .02f, 1f);
                case "tap": return Drop(kind, 900f, 700f, .04f);
                case "tick": return Drop(kind, 1600f, 1500f, .03f);
                case "whoosh": return Swish(kind, .35f);
                case "win": return Notes(kind, new[] { 523f, 659f, 784f, 1047f, 1319f }, .11f, .02f, 2f);
                case "lose": return Notes(kind, new[] { 523f, 440f, 349f }, .16f, .03f, 1f);
                default: return null;
            }
        }

        static AudioClip Notes(string name, float[] f, float each, float gap, float bright)
        {
            int per = (int)(Rate * (each + gap));
            int tail = (int)(Rate * .25f);
            var d = new float[per * f.Length + tail];
            for (int n = 0; n < f.Length; n++)
            {
                int len = (int)(Rate * (each + .22f));
                for (int i = 0; i < len && n * per + i < d.Length; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Min(1f, i / (Rate * .003f)) * Mathf.Exp(-t * 9f);
                    float ph = 2f * Mathf.PI * f[n] * t;
                    float w = Mathf.Sin(ph) + .18f * bright * .1f * Mathf.Sin(ph * 3f) + .3f * Mathf.Sin(ph * 2f);
                    d[n * per + i] += w * env * .22f;
                }
            }
            return Clip(name, d);
        }

        static AudioClip Drop(string name, float f0, float f1, float dur)
        {
            int len = (int)(Rate * dur);
            var d = new float[len];
            float ph = 0f;
            for (int i = 0; i < len; i++)
            {
                float k = (float)i / len;
                float f = Mathf.Lerp(f0, f1, Mathf.Sqrt(k));
                ph += 2f * Mathf.PI * f / Rate;
                d[i] = Mathf.Sin(ph) * Mathf.Exp(-k * 5f) * Mathf.Min(1f, i / 40f) * .5f;
            }
            return Clip(name, d);
        }

        static AudioClip Rub(string name, float dur)
        {
            int len = (int)(Rate * dur);
            var d = new float[len];
            var rng = new System.Random(3);
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float k = (float)i / len;
                lp += (((float)rng.NextDouble() * 2f - 1f) - lp) * .04f;
                float thud = Mathf.Sin(2f * Mathf.PI * 95f * i / Rate) * Mathf.Exp(-k * 9f) * .5f;
                d[i] = (lp * 2.2f * Mathf.Sin(Mathf.PI * k) + thud) * .45f;
            }
            return Clip(name, d);
        }

        static AudioClip Pop(string name)
        {
            int len = (int)(Rate * .07f);
            var d = new float[len];
            var rng = new System.Random(9);
            for (int i = 0; i < len; i++)
            {
                float k = (float)i / len;
                d[i] = (((float)rng.NextDouble() * 2f - 1f) * .6f + Mathf.Sin(2f * Mathf.PI * 1800f * i / Rate) * .4f) * Mathf.Exp(-k * 9f) * .5f;
            }
            return Clip(name, d);
        }

        static AudioClip Swish(string name, float dur)
        {
            int len = (int)(Rate * dur);
            var d = new float[len];
            var rng = new System.Random(7);
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float k = (float)i / len;
                lp += (((float)rng.NextDouble() * 2f - 1f) - lp) * Mathf.Lerp(.05f, .35f, k);
                d[i] = lp * Mathf.Sin(Mathf.PI * k) * .4f;
            }
            return Clip(name, d);
        }

        static AudioClip Clip(string name, float[] d)
        {
            var c = AudioClip.Create("bangul-" + name, d.Length, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }
    }
}
