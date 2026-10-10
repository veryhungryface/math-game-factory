// 만두 묶기 — 화면 UI(차양 위 HUD·목표 천·안내 토스트·타이틀 배지·결과 카드). 어두운 반투명 패널로 세계를 덮지 않는다.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.ManduMukgi
{
    public partial class ManduMukgiGame
    {
        static readonly Color CInk = new Color32(0x20, 0x24, 0x2B, 255);
        static readonly Color CSteel = new Color32(0xDD, 0xE9, 0xEE, 255);
        static readonly Color CCobalt = new Color32(0x16, 0x4A, 0x7A, 255);
        static readonly Color CGold = new Color32(0xF2, 0xC1, 0x4E, 255);
        static readonly Color CCoral = new Color32(0xFF, 0x5C, 0x7A, 255);

        Sprite round, ringSprite;
        RectTransform scoreRt;
        RectTransform canvasRt, hudRt, goalRt, bannerRt, toastRt, titleRt, endRt, ctaRt, restartRt, badgeRt;
        TextMeshProUGUI timerT, scoreT, firstT, goalT, bannerT, toastT, taglineT, ctaT, bestT, endTitleT, endScoreT, endRowsT, endCoachT, restartT, timerCap;
        readonly Image[] sealImg = new Image[3];
        readonly TextMeshProUGUI[] sealTxt = new TextMeshProUGUI[3];
        Image ctaRing, timerBox;
        readonly List<RectTransform> uiRipples = new List<RectTransform>();
        readonly List<float> uiRippleT = new List<float>();
        float shownScore, scorePunch, toastT_, bannerT_, goalPunch, endT, ctaPulse, restartPulse, timerFlip;
        int lastTimerShown = -1, lastScoreTarget;
        bool endShown;

        Sprite RoundSprite()
        {
            if (round) return round;
            const int S = 64, R = 22;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Round" };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(R - x - 0.5f, x + 0.5f - (S - R))), dy = Mathf.Max(0, Mathf.Max(R - y - 0.5f, y + 0.5f - (S - R)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(R - d + 0.5f);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            round = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R + 2, R + 2, R + 2, R + 2));
            return round;
        }

        Sprite RingSprite()
        {
            if (ringSprite) return ringSprite;
            const int S = 96;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Ring" };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = x + 0.5f - S / 2f, dy = y + 0.5f - S / 2f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 40f) / 5f);
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            ringSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
            return ringSprite;
        }

        RectTransform Panel(string n, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color col, bool sliced = true)
        {
            var go = new GameObject(n, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = RoundSprite(); img.type = sliced ? Image.Type.Sliced : Image.Type.Simple; img.color = col;
            img.raycastTarget = false;
            return rt;
        }

        TextMeshProUGUI Label(string s, Transform parent, Vector2 pos, float size, Color col, float width, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = MgfText.Ui(s, new Vector2(0.5f, 0.5f), pos, size, col, width);
            t.transform.SetParent(parent, false);
            var rt = t.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos;
            t.alignment = align; t.fontStyle = FontStyles.Bold;
            return t;
        }

        void BuildUi()
        {
            var canvas = MgfText.Canvas;
            canvasRt = (RectTransform)canvas.transform;

            // HUD(차양 위)
            hudRt = new GameObject("Hud", typeof(RectTransform)).GetComponent<RectTransform>();
            hudRt.SetParent(canvasRt, false);
            hudRt.anchorMin = new Vector2(0, 1); hudRt.anchorMax = new Vector2(1, 1); hudRt.pivot = new Vector2(0.5f, 1); hudRt.sizeDelta = new Vector2(0, 70); hudRt.anchoredPosition = Vector2.zero;
            var tb = Panel("TimerBox", hudRt, new Vector2(0, 1), new Vector2(50, -30), new Vector2(76, 46), new Color(0.08f, 0.1f, 0.13f, 0.92f));
            timerBox = tb.GetComponent<Image>();
            timerT = Label("90", tb, new Vector2(-6, 1), 28, CGold, 70);
            timerCap = Label("초", tb, new Vector2(25, -8), 12, CSteel, 20);
            var sc = new GameObject("Score", typeof(RectTransform)).GetComponent<RectTransform>();
            sc.SetParent(hudRt, false); sc.anchorMin = sc.anchorMax = new Vector2(0.5f, 1); sc.anchoredPosition = new Vector2(-42, -26); sc.sizeDelta = new Vector2(130, 40);
            scoreRt = sc;
            scoreT = Label("0", sc, new Vector2(0, 0), 30, Color.white, 130);
            scoreT.outlineWidth = 0.22f; scoreT.outlineColor = new Color32(0x0B, 0x22, 0x3C, 255);
            firstT = Label("첫 판단 0/12", sc, new Vector2(0, -26), 12.5f, CSteel, 200);
            for (int i = 0; i < 3; i++)
            {
                var s = Panel("Seal" + i, hudRt, new Vector2(1, 1), new Vector2(-150 + i * 36, -30), new Vector2(32, 32), new Color(0.96f, 0.94f, 0.92f, 1f), false);
                sealImg[i] = s.GetComponent<Image>();
                sealTxt[i] = Label("봉", s, Vector2.zero, 15, CCoral, 30);
            }

            // 목표 천(첫 성공 전까지 상시) — 밝은 천 위 진한 글자
            goalRt = Panel("Goal", canvasRt, new Vector2(0.5f, 1), new Vector2(0, -78), new Vector2(360, 30), CSteel);
            goalT = Label("", goalRt, Vector2.zero, 13.5f, CInk, 346);
            goalT.enableAutoSizing = true; goalT.fontSizeMin = 10; goalT.fontSizeMax = 14;

            // 단계 배너(잠깐)
            bannerRt = Panel("Banner", canvasRt, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(250, 36), CCobalt);
            bannerT = Label("", bannerRt, Vector2.zero, 15, Color.white, 240);
            bannerRt.gameObject.SetActive(false);

            // 거절·안내 토스트(조작 불가 이유 한 문장)
            toastRt = Panel("Toast", canvasRt, new Vector2(0.5f, 0), new Vector2(0, 160), new Vector2(330, 34), new Color(0.97f, 0.98f, 0.99f, 0.97f));
            toastT = Label("", toastRt, Vector2.zero, 13, CInk, 316);
            toastT.enableAutoSizing = true; toastT.fontSizeMin = 9; toastT.fontSizeMax = 13;
            toastRt.gameObject.SetActive(false);

            // 입력 물결(밀가루 원형 자국)
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject("Ripple", typeof(RectTransform));
                go.transform.SetParent(canvasRt, false);
                var img = go.AddComponent<Image>(); img.sprite = RingSprite(); img.raycastTarget = false; img.color = new Color(1, 1, 1, 0);
                var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(70, 70);
                uiRipples.Add(rt); uiRippleT.Add(-1f);
            }

            // 타이틀
            titleRt = new GameObject("Title", typeof(RectTransform)).GetComponent<RectTransform>();
            titleRt.SetParent(canvasRt, false); titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one; titleRt.sizeDelta = Vector2.zero;
            var tag = Panel("Tagline", titleRt, new Vector2(0.5f, 1), new Vector2(0, -246), new Vector2(210, 34), CGold);
            taglineT = Label("같은 각을 이어라", tag, Vector2.zero, 17, CInk, 200);
            ctaRt = new GameObject("Cta", typeof(RectTransform)).GetComponent<RectTransform>();
            ctaRt.SetParent(titleRt, false); ctaRt.anchorMin = ctaRt.anchorMax = Vector2.zero; ctaRt.sizeDelta = new Vector2(64, 64);
            ctaRing = ctaRt.gameObject.AddComponent<Image>(); ctaRing.sprite = RingSprite(); ctaRing.color = CGold; ctaRing.raycastTarget = false;
            var ctaLab = Panel("CtaLabel", ctaRt, new Vector2(0.5f, 0.5f), new Vector2(0, -52), new Vector2(178, 32), CCoral);
            ctaT = Label("여기를 눌러 시작", ctaLab, Vector2.zero, 15, Color.white, 170);
            badgeRt = new GameObject("Badges", typeof(RectTransform)).GetComponent<RectTransform>();
            badgeRt.SetParent(titleRt, false); badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(0, 0); badgeRt.pivot = new Vector2(0, 0);
            badgeRt.anchoredPosition = new Vector2(10, 12); badgeRt.sizeDelta = new Vector2(220, 92);
            var b1 = Panel("B1", badgeRt, new Vector2(0, 0), new Vector2(84, 78), new Vector2(160, 26), CCobalt);
            Label("중2 · 삼각형의 성질", b1, Vector2.zero, 12, Color.white, 156);
            var b2 = Panel("B2", badgeRt, new Vector2(0, 0), new Vector2(96, 47), new Vector2(184, 26), CSteel);
            Label("실전 90초 · 첫 판단 10/12", b2, Vector2.zero, 12, CInk, 180);
            var b3 = Panel("B3", badgeRt, new Vector2(0, 0), new Vector2(70, 16), new Vector2(132, 26), new Color(0.08f, 0.1f, 0.13f, 0.9f));
            bestT = Label("최고 기록 0점", b3, Vector2.zero, 12, CGold, 128);

            // 결과 카드(밝은 스테인리스 카드)
            endRt = Panel("End", canvasRt, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(330, 360), CSteel);
            var head = Panel("Head", endRt, new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(330, 68), CCobalt);
            endTitleT = Label("", head, new Vector2(0, 0), 20, Color.white, 310);
            endTitleT.enableAutoSizing = true; endTitleT.fontSizeMin = 13; endTitleT.fontSizeMax = 21;
            endScoreT = Label("0", endRt, new Vector2(0, 92), 46, CCobalt, 300);
            Label("점", endRt, new Vector2(0, 58), 13, CInk, 100);
            endRowsT = Label("", endRt, new Vector2(0, 6), 14.5f, CInk, 300);
            endRowsT.textWrappingMode = TextWrappingModes.Normal; endRowsT.rectTransform.sizeDelta = new Vector2(300, 90); endRowsT.lineSpacing = 8;
            endCoachT = Label("", endRt, new Vector2(0, -70), 12.5f, new Color32(0x35, 0x40, 0x4C, 255), 300);
            endCoachT.textWrappingMode = TextWrappingModes.Normal; endCoachT.rectTransform.sizeDelta = new Vector2(300, 44);
            restartRt = Panel("Restart", endRt, new Vector2(0.5f, 0), new Vector2(0, 42), new Vector2(210, 56), CCoral);
            restartT = Label("다시 묶기", restartRt, Vector2.zero, 21, Color.white, 200);
            endRt.gameObject.SetActive(false);
            LayoutUi();
        }

        void LayoutUi()
        {
            if (!hudRt) return;
            float aspect = lastW / (float)Mathf.Max(1, lastH);
            bool wide = aspect >= 1.1f;
            goalRt.sizeDelta = new Vector2(wide ? 470 : 360, 30);
            goalRt.anchorMin = goalRt.anchorMax = wide ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 1f);
            scoreRt.anchoredPosition = new Vector2(wide ? 0 : -42, -26);
            goalRt.anchoredPosition = new Vector2(0, wide ? 22 : -80);
            toastRt.anchoredPosition = new Vector2(0, wide ? 64 : 150);
            var tagRt = (RectTransform)taglineT.transform.parent;
            tagRt.anchorMin = tagRt.anchorMax = wide ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f);
            tagRt.anchoredPosition = wide ? new Vector2(150, -190) : new Vector2(0, -248);
            badgeRt.anchoredPosition = wide ? new Vector2(12, 10) : new Vector2(8, 10);
        }

        // ───────────────────────── 화면 전환 ─────────────────────────

        void SetScreenTitle()
        {
            titleRt.gameObject.SetActive(true);
            hudRt.gameObject.SetActive(false);
            goalRt.gameObject.SetActive(false);
            endRt.gameObject.SetActive(false);
            endShown = false;
            titleT = 0f; for (int i = 0; i < 4; i++) flipped[i] = false;
            bestT.text = "최고 기록 " + bestScore + "점";
        }

        void SetScreenPlay()
        {
            titleRt.gameObject.SetActive(false);
            hudRt.gameObject.SetActive(true);
            goalRt.gameObject.SetActive(true);
            endRt.gameObject.SetActive(false);
            endShown = false;
            shownScore = 0; lastTimerShown = -1;
        }

        void SetScreenEnd(string reason)
        {
            endRt.gameObject.SetActive(true);
            goalRt.gameObject.SetActive(false);
            endT = -1.1f; endShown = true;   // 마지막 반죽의 근거 공개를 잠깐 보여 준 뒤 카드가 올라온다
            endRt.localScale = Vector3.zero;
            string title = reason == "mastered" ? "숙련 봉합! 만두 12개 완성"
                : reason == "unmastered" ? "봉합 완료 · 첫 판단 기준 미달"
                : reason == "seals" ? "봉인 3개가 모두 깨졌다"
                : "시간 종료 · 실전 90초가 지났다";
            endTitleT.text = title;
            endRt.GetChild(0).GetComponent<Image>().color = reason == "mastered" ? CGold : CCobalt;
            endTitleT.color = reason == "mastered" ? CInk : Color.white;
            var sb = new System.Text.StringBuilder();
            sb.Append("첫 판단 ").Append(st.firstAttemptCorrect).Append("/12  ·  봉합 ").Append(st.solved).Append("/12\n");
            sb.Append("단계별 첫 판단  ").Append(st.firstByBand[0]).Append("/4 · ").Append(st.firstByBand[1]).Append("/4 · ").Append(st.firstByBand[2]).Append("/4\n");
            sb.Append("최고 기록 ").Append(bestScore).Append("점");
            endRowsT.text = sb.ToString();
            endCoachT.text = Coach();
            endScoreT.text = "0";
        }

        string Coach()
        {
            // 다음 판에 바꿔 볼 읽기 순서(단계별 첫 판단 기록에서)
            int weak = 0; float worst = 2f;
            for (int b = 0; b < 3; b++)
            {
                if (st.resolvedByBand[b] == 0) continue;
                float r = st.firstByBand[b] / (float)st.resolvedByBand[b];
                if (r < worst) { worst = r; weak = b; }
            }
            if (st.firstAttemptResolved == 0) return "반죽의 같은 두 변을 먼저 찾고, 그 맞은편 두 각을 이어 보시오.";
            if (st.mastered) return "같은 변 → 맞은편 각, 같은 각 → 맞은편 변을 모두 읽었다.";
            if (weak == 0) return "같은 길이의 두 변이 만나는 곳이 꼭지각이다. 그 맞은편 두 각을 잇는다.";
            if (weak == 1) return "같은 두 각을 찾았으면 그 각이 있는 곳이 아니라 맞은편 변으로 시선을 옮긴다.";
            return "이미 있던 연결을 믿지 말고 수치를 다시 읽어 맞는 두 곳을 새로 잇는다.";
        }

        void SetGoal(string s)
        {
            goalT.text = s;
            goalPunch = 1f;
        }

        void SetBanner(string s)
        {
            bannerT.text = s;
            bannerT_ = 1.7f;
            bannerRt.gameObject.SetActive(true);
        }

        void Refuse(string msg, int lane)
        {
            toastT.text = msg;
            toastT_ = 1.6f;
            toastRt.gameObject.SetActive(true);
            ManduSfx.Play("refuse", 0.3f);
            if (lane >= 0 && lane < trays.Length && trays[lane].phase != Tray.Phase.Hidden) wiggleT[lane] = 0.35f;
        }

        void Ripple(Vector2 screen)
        {
            int best = 0; float oldest = -2f;
            for (int i = 0; i < uiRipples.Count; i++) { if (uiRippleT[i] < 0f) { best = i; break; } if (uiRippleT[i] > oldest) { oldest = uiRippleT[i]; best = i; } }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var lp);
            uiRipples[best].anchorMin = uiRipples[best].anchorMax = new Vector2(0.5f, 0.5f);
            uiRipples[best].anchoredPosition = lp;
            uiRippleT[best] = 0f;
            ManduSfx.Play("tick", 0.18f);
        }

        bool HitRestart(Vector2 screen)
        {
            if (!endRt.gameObject.activeSelf || endT < 0.5f) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(restartRt, screen, null);
        }

        void PulseRestart() { restartPulse = 1f; }
        void PulseTitleCta() { ctaPulse = 1f; }

        // ───────────────────────── 매 프레임 ─────────────────────────

        void UpdateUi(float dt)
        {
            float udt = Time.unscaledDeltaTime;
            // 점수 카운트업 + 스케일 펀치
            if (st.score != lastScoreTarget) { lastScoreTarget = st.score; scorePunch = 1f; }
            shownScore = Mathf.MoveTowards(shownScore, st.score, Mathf.Max(60f, Mathf.Abs(st.score - shownScore) * 6f) * udt);
            if (hudRt.gameObject.activeSelf)
            {
                int ss = Mathf.RoundToInt(shownScore);
                if (scoreT.text != ss.ToString()) scoreT.text = ss.ToString();
                scorePunch = Mathf.Max(0f, scorePunch - udt * 3f);
                scoreT.transform.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(scorePunch * Mathf.PI));
                int sec = stage == Stage.Practice ? -2 : Mathf.CeilToInt(runClock);
                if (sec != lastTimerShown)
                {
                    lastTimerShown = sec; timerFlip = 1f;
                    timerT.text = sec == -2 ? "연습" : sec.ToString();
                    timerT.fontSize = sec == -2 ? 18 : 28;
                    timerT.rectTransform.anchoredPosition = new Vector2(sec == -2 ? 0 : -6, 1);
                    timerCap.gameObject.SetActive(sec != -2);
                    timerT.color = sec >= 0 && sec <= 15 ? CCoral : CGold;
                    if (sec >= 0 && sec <= 10 && stage == Stage.Play) ManduSfx.Play("tick", 0.2f, 0.8f);
                }
                timerFlip = Mathf.Max(0f, timerFlip - udt * 5f);
                timerT.transform.localScale = new Vector3(1f, 1f - 0.6f * Mathf.Sin(timerFlip * Mathf.PI), 1f);
                string ft = "첫 판단 " + st.firstAttemptCorrect + "/" + st.firstAttemptResolved + (st.combo >= 2 ? "  ·  연속 " + st.combo : "");
                if (firstT.text != ft) firstT.text = ft;
                for (int i = 0; i < 3; i++)
                {
                    bool alive = i < st.lives;
                    var want = alive ? new Color(0.96f, 0.94f, 0.92f, 1f) : new Color(0.29f, 0.33f, 0.38f, 1f);
                    if (sealImg[i].color != want) { sealImg[i].color = want; sealTxt[i].text = alive ? "봉" : "×"; sealTxt[i].color = alive ? CCoral : CSteel; MgfFx.Punch(sealImg[i].transform, 0.4f, 0.3f); }
                }
            }
            // 목표 천: 바뀔 때 살짝 튄다
            goalPunch = Mathf.Max(0f, goalPunch - udt * 3f);
            goalRt.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(goalPunch * Mathf.PI));
            // 단계 배너
            if (bannerT_ > 0f)
            {
                bannerT_ -= udt;
                float k = Mathf.Clamp01(bannerT_ / 0.3f) * Mathf.Clamp01((1.7f - bannerT_) / 0.2f);
                bannerRt.localScale = Vector3.one * (0.85f + 0.15f * k);
                var c = bannerRt.GetComponent<Image>().color; c.a = k; bannerRt.GetComponent<Image>().color = c;
                bannerT.alpha = k;
                bannerRt.anchoredPosition = new Vector2(0, land ? 120 : 210);
                if (bannerT_ <= 0f) bannerRt.gameObject.SetActive(false);
            }
            // 토스트(좌우로 한 번 흔들림)
            if (toastT_ > 0f)
            {
                toastT_ -= udt;
                float e = 1.6f - toastT_;
                toastRt.localRotation = Quaternion.identity;
                float x = e < 0.3f ? Mathf.Sin(e * 50f) * 6f * (1f - e / 0.3f) : 0f;
                toastRt.anchoredPosition = new Vector2(x, toastRt.anchoredPosition.y);
                if (toastT_ <= 0f) toastRt.gameObject.SetActive(false);
            }
            for (int i = 0; i < uiRipples.Count; i++)
            {
                if (uiRippleT[i] < 0f) continue;
                uiRippleT[i] += udt;
                float k = uiRippleT[i] / 0.35f;
                var img = uiRipples[i].GetComponent<Image>();
                if (k >= 1f) { uiRippleT[i] = -1f; img.color = new Color(1, 1, 1, 0); continue; }
                uiRipples[i].localScale = Vector3.one * (0.4f + k * 0.9f);
                img.color = new Color(0.98f, 0.97f, 0.93f, 0.85f * (1f - k));
            }
            // 타이틀 CTA: 첫 반죽의 B 모서리(호) 위 맥동 고리
            if (titleRt.gameObject.activeSelf)
            {
                var t0 = trays[0];
                bool show = t0.phase == Tray.Phase.Ready && t0.prob != null;
                ctaRt.gameObject.SetActive(show);
                if (show)
                {
                    var sp = t0.LocalToScreen(cam, t0.tpos[1]);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(titleRt, sp, null, out var lp);
                    ctaRt.anchorMin = ctaRt.anchorMax = new Vector2(0.5f, 0.5f);
                    ctaRt.anchoredPosition = lp;
                    ctaPulse = Mathf.Max(0f, ctaPulse - udt * 2f);
                    float pulse = 1f + 0.15f * Mathf.Sin(Time.unscaledTime * 5f) + 0.4f * ctaPulse;
                    ctaRt.localScale = Vector3.one * pulse;
                    ctaRt.GetChild(0).localScale = Vector3.one / pulse;
                }
            }
            // 결과 카드: 점수 카운트업
            if (endShown)
            {
                endT += udt;
                float k = Mathf.Clamp01(endT / 0.35f);
                endRt.localScale = Vector3.one * (endT < 0f ? 0f : EaseOutBack(k));
                int sc = Mathf.RoundToInt(st.score * Mathf.Clamp01((endT - 0.3f) / 1.0f));
                if (endScoreT.text != sc.ToString()) { endScoreT.text = sc.ToString(); }
                restartPulse = Mathf.Max(0f, restartPulse - udt * 2f);
                restartRt.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.unscaledTime * 4f) + 0.2f * restartPulse);
            }
        }
    }
}
