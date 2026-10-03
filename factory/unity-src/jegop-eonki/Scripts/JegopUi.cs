// 제곱 얹기 — 화면 UI. 먹줄·삼나무·놋쇠. 스티커 그림자·광택 없음.
// 세로: 위 지그 · 아래 기왓장 독. 가로 1280: 중앙 지그 확대, 좌 폐기/목숨 · 우 독.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.JegopEonki
{
    public partial class JegopEonkiGame
    {
        CanvasGroup hudG, titleG, endG, toastG, skipG;
        TextMeshProUGUI timeTxt, scoreTxt, comboTxt, promptTxt, goalTxt, bestTxt, tagTxt, badgeTxt, toastTxt;
        TextMeshProUGUI endHead, endStats, endCtaTxt, ctaTxt, skipTxt, lockTxt, lifeCap;
        readonly TextMeshProUGUI[] logo = new TextMeshProUGUI[3];
        Image toastBg, timeFill, sheetFill, ctaPlate, endPlate;
        RectTransform titleRt, endRt, ctaRt, hudRt, skipRt, bandRt, livesRt;
        Image paperImg;
        Sprite roundSpr, ringSpr;
        readonly Image[] ripples = new Image[4];
        readonly float[] rippleT = { 9, 9, 9, 9 };
        int rippleNext;
        readonly Image[] lifeImg = new Image[3];
        float ctaPress;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }
        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color c, Sprite s = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = s; im.color = c; im.raycastTarget = false;
            if (s && s.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }
        TextMeshProUGUI Txt(Transform parent, string s, Vector2 anchor, Vector2 pos, float size, Color c, float width = 360, TextAlignmentOptions al = TextAlignmentOptions.Center)
        {
            var rt = R("T", parent, anchor, pos, new Vector2(width, size * 1.55f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = c; t.alignment = al; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; t.text = s;
            return t;
        }

        static Sprite RoundSprite(int S, float rad, bool ring)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = ring ? "Ring" : "Round", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float a;
                if (ring)
                {
                    float dx = x + 0.5f - S / 2f, dy = y + 0.5f - S / 2f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    float r = S / 2f - 2, w = S * 0.06f;
                    a = Mathf.Clamp01(Mathf.Clamp01(1 - Mathf.Abs(d - (r - w)) / w) * 2.2f);
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(x + 0.5f - S / 2f) - (S / 2f - rad), 0);
                    float qy = Mathf.Max(Mathf.Abs(y + 0.5f - S / 2f) - (S / 2f - rad), 0);
                    a = Mathf.Clamp01(rad - Mathf.Sqrt(qx * qx + qy * qy) + 0.5f);
                }
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px); t.Apply(false, true);
            float bd = ring ? 0 : rad + 2;
            return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(bd, bd, bd, bd));
        }

        static Sprite TileSprite(int S)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "HudTile", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float m = 6f;
                bool g = x < m || y < m || x > S - m || y > S - m || (x % (S / 3) < 2) || (y % (S / 3) < 2);
                float a = g ? 1f : 0.92f;
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100);
        }

        void BuildUi()
        {
            var canvas = MgfText.Canvas;
            var ct = canvas.transform;
            roundSpr = RoundSprite(48, 6, false);
            ringSpr = RoundSprite(96, 0, true);
            var tileSpr = TileSprite(48);

            hudRt = R("Hud", ct, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0, 48));
            hudRt.anchorMin = new Vector2(0, 1); hudRt.anchorMax = new Vector2(1, 1); hudRt.pivot = new Vector2(0.5f, 1f);
            hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            var tb = Img(hudRt, "TimeBar", new Vector2(0.5f, 1f), new Vector2(0, -6), new Vector2(0, 8), new Color(InkC.r, InkC.g, InkC.b, 0.18f), roundSpr);
            tb.rectTransform.anchorMin = new Vector2(0, 1); tb.rectTransform.anchorMax = new Vector2(1, 1); tb.rectTransform.sizeDelta = new Vector2(-28, 8);
            timeFill = Img(tb.transform, "Fill", new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, Terra, roundSpr);
            var tfr = timeFill.rectTransform; tfr.anchorMin = Vector2.zero; tfr.anchorMax = Vector2.one; tfr.pivot = new Vector2(0, 0.5f); tfr.sizeDelta = Vector2.zero;
            timeTxt = Txt(hudRt, "90", new Vector2(0f, 1f), new Vector2(48, -30), 26, InkC, 80, TextAlignmentOptions.Left);
            timeTxt.fontStyle = FontStyles.Bold;
            // 음소거 버튼(우상단 ~58u) 아래로 점수가 숨지 않게 왼쪽으로.
            scoreTxt = Txt(hudRt, "0", new Vector2(1f, 1f), new Vector2(-118, -26), 24, InkC, 100, TextAlignmentOptions.Right);
            scoreTxt.fontStyle = FontStyles.Bold;
            comboTxt = Txt(hudRt, "", new Vector2(1f, 1f), new Vector2(-118, -48), 13, Terra, 110, TextAlignmentOptions.Right);
            lockTxt = Txt(hudRt, "0 / 10", new Vector2(0.5f, 1f), new Vector2(0, -18), 14, InkC, 140);

            bandRt = R("Band", ct, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0, 132));
            paperImg = Img(bandRt, "Paper", new Vector2(0.5f, 1f), new Vector2(0, -72), new Vector2(340, 72), new Color(Cream.r, Cream.g, Cream.b, 0.92f), roundSpr);
            paperImg.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            promptTxt = Txt(bandRt, "", new Vector2(0.5f, 1f), new Vector2(0, -70), 13, InkC, 320);
            promptTxt.fontStyle = FontStyles.Bold;
            promptTxt.enableAutoSizing = true;
            promptTxt.fontSizeMin = 11;
            promptTxt.fontSizeMax = 15;
            promptTxt.overflowMode = TextOverflowModes.Ellipsis;
            promptTxt.lineSpacing = -6f;
            goalTxt = Txt(bandRt, "넓이판을 변에 얹고 잠가라", new Vector2(0.5f, 1f), new Vector2(0, -122), 13, Terra, 320);
            goalTxt.fontStyle = FontStyles.Bold;

            livesRt = R("Lives", ct, new Vector2(0.5f, 0f), new Vector2(0, 36), new Vector2(220, 70));
            lifeCap = Txt(livesRt, "금 간 기와", new Vector2(0.5f, 1f), new Vector2(0, -6), 11, InkC, 140);
            for (int i = 0; i < 3; i++)
                lifeImg[i] = Img(livesRt, "L" + i, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 48f, -8), new Vector2(34, 34), Terra, tileSpr);
            var sheetBg = Img(livesRt, "SheetBg", new Vector2(0.5f, 1f), new Vector2(0, 16), new Vector2(200, 10), new Color(InkC.r, InkC.g, InkC.b, 0.2f), roundSpr);
            sheetFill = Img(sheetBg.transform, "Fill", new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, Brass, roundSpr);
            var sfr = sheetFill.rectTransform; sfr.anchorMin = Vector2.zero; sfr.anchorMax = Vector2.one; sfr.pivot = new Vector2(0, 0.5f); sfr.sizeDelta = Vector2.zero;

            skipRt = R("Skip", ct, new Vector2(0.5f, 0f), new Vector2(0, 108), new Vector2(220, 52));
            skipG = skipRt.gameObject.AddComponent<CanvasGroup>(); skipG.alpha = 0; skipG.blocksRaycasts = false;
            Img(skipRt, "P", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 52), Cedar, roundSpr).raycastTarget = false;
            skipTxt = Txt(skipRt, "본판으로", new Vector2(0.5f, 0.5f), Vector2.zero, 20, Cream, 200);
            skipTxt.fontStyle = FontStyles.Bold;

            toastBg = Img(ct, "Toast", new Vector2(0.5f, 0f), new Vector2(0, 168), new Vector2(340, 52), new Color(InkC.r, InkC.g, InkC.b, 0.90f), roundSpr);
            toastG = toastBg.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0;
            toastTxt = Txt(toastBg.transform, "", new Vector2(0.5f, 0.5f), Vector2.zero, 14, Cream, 320);

            for (int i = 0; i < ripples.Length; i++)
            {
                ripples[i] = Img(ct, "Rip" + i, new Vector2(0, 0), Vector2.zero, new Vector2(48, 48), new Color(InkC.r, InkC.g, InkC.b, 0.35f), ringSpr);
                ripples[i].rectTransform.pivot = new Vector2(0.5f, 0.5f);
                ripples[i].enabled = false;
            }

            titleG = R("Title", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<CanvasGroup>();
            titleRt = titleG.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one; titleRt.sizeDelta = Vector2.zero;
            // 로고: 기와골 먹줄 세 겹 — 그림자 · 테라코타 면 · 먹 획. 낱글자 필 템플릿 아님.
            logo[0] = Txt(titleRt, "제곱 얹기", new Vector2(0.5f, 0.93f), new Vector2(3, -4), 46, new Color(0.18f, 0.10f, 0.08f, 0.45f), 420);
            logo[1] = Txt(titleRt, "제곱 얹기", new Vector2(0.5f, 0.93f), new Vector2(-1, 2), 46, Terra, 420);
            logo[2] = Txt(titleRt, "제곱 얹기", new Vector2(0.5f, 0.93f), Vector2.zero, 46, InkC, 420);
            for (int i = 0; i < 3; i++) logo[i].fontStyle = FontStyles.Bold;
            tagTxt = Txt(titleRt, "넓이판을 변에 얹어라", new Vector2(0.5f, 0.85f), Vector2.zero, 17, Cedar, 360);
            tagTxt.fontStyle = FontStyles.Bold;
            badgeTxt = Txt(titleRt, "중2  ·  피타고라스 정리", new Vector2(0.5f, 0.80f), Vector2.zero, 13, InkC, 360);
            bestTxt = Txt(titleRt, "", new Vector2(0.5f, 0.15f), Vector2.zero, 13, InkC, 360);
            ctaRt = R("Cta", titleRt, new Vector2(0.5f, 0.07f), Vector2.zero, new Vector2(280, 68));
            ctaPlate = Img(ctaRt, "Plate", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 68), Cedar, roundSpr);
            ctaPlate.raycastTarget = true;
            ctaTxt = Txt(ctaRt, "작업대 열기", new Vector2(0.5f, 0.5f), new Vector2(0, 2), 24, Cream, 260);
            ctaTxt.fontStyle = FontStyles.Bold;

            endG = R("End", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<CanvasGroup>();
            endRt = endG.GetComponent<RectTransform>();
            endRt.anchorMin = Vector2.zero; endRt.anchorMax = Vector2.one; endRt.sizeDelta = Vector2.zero;
            Img(endRt, "Sheet", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(320, 280), new Color(Cream.r, Cream.g, Cream.b, 0.96f), roundSpr);
            endHead = Txt(endRt, "", new Vector2(0.5f, 0.64f), Vector2.zero, 32, InkC, 300);
            endHead.fontStyle = FontStyles.Bold;
            endStats = Txt(endRt, "", new Vector2(0.5f, 0.50f), Vector2.zero, 17, InkC, 300);
            var endCta = R("EndCta", endRt, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(260, 64));
            endPlate = Img(endCta, "P", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 64), Cedar, roundSpr);
            endPlate.raycastTarget = true;
            endCtaTxt = Txt(endCta, "다시 얹기", new Vector2(0.5f, 0.5f), Vector2.zero, 24, Cream, 240);
            endCtaTxt.fontStyle = FontStyles.Bold;

            LayoutHud();
        }

        void LayoutHud()
        {
            if (bandRt == null || promptTxt == null) return;
            var pr = promptTxt.rectTransform;
            var gr = goalTxt.rectTransform;
            if (land)
            {
                // 캔버스 기준 390폭 — 가로에서 96u 카드는 화면의 40%를 먹는다. 짧은 띠만.
                bandRt.anchorMin = new Vector2(0f, 1f);
                bandRt.anchorMax = new Vector2(0f, 1f);
                bandRt.pivot = new Vector2(0f, 1f);
                // 타이머 숫자(좌상 8~88u) 오른쪽에서 시작 — 카드가 남은 시간을 덮지 않게. 발문은 두 줄까지.
                bandRt.anchoredPosition = new Vector2(92f, -16f);
                bandRt.sizeDelta = new Vector2(236f, 52f);
                if (paperImg)
                {
                    paperImg.enabled = true;
                    paperImg.rectTransform.anchorMin = new Vector2(0f, 1f);
                    paperImg.rectTransform.anchorMax = new Vector2(1f, 1f);
                    paperImg.rectTransform.pivot = new Vector2(0f, 1f);
                    paperImg.rectTransform.anchoredPosition = Vector2.zero;
                    paperImg.rectTransform.sizeDelta = new Vector2(0f, 52f);
                }
                pr.anchorMin = new Vector2(0f, 1f); pr.anchorMax = new Vector2(1f, 1f);
                pr.pivot = new Vector2(0.5f, 1f);
                pr.anchoredPosition = new Vector2(0f, -4f);
                pr.sizeDelta = new Vector2(-14f, 32f);
                promptTxt.alignment = TextAlignmentOptions.TopLeft;
                promptTxt.fontSizeMax = 10;
                promptTxt.fontSizeMin = 7;
                gr.anchorMin = new Vector2(0f, 0f); gr.anchorMax = new Vector2(1f, 0f);
                gr.pivot = new Vector2(0.5f, 0f);
                gr.anchoredPosition = new Vector2(0f, 3f);
                gr.sizeDelta = new Vector2(-16f, 14f);
                goalTxt.alignment = TextAlignmentOptions.Left;
                goalTxt.fontSize = 11;
                if (livesRt)
                {
                    // 하단 중앙 빈 작업대 — 왼쪽 폐기 홈 위에 HUD가 올라타지 않게.
                    livesRt.anchorMin = livesRt.anchorMax = new Vector2(0.5f, 0f);
                    livesRt.pivot = new Vector2(0.5f, 0f);
                    livesRt.anchoredPosition = new Vector2(-40f, -4f);
                    livesRt.sizeDelta = new Vector2(180f, 64f);
                }
                if (skipRt)
                {
                    skipRt.anchorMin = skipRt.anchorMax = new Vector2(1f, 0f);
                    skipRt.pivot = new Vector2(1f, 0f);
                    skipRt.anchoredPosition = new Vector2(-24f, 18f);
                }
            }
            else
            {
                if (paperImg)
                {
                    paperImg.enabled = true;
                    paperImg.rectTransform.anchorMin = paperImg.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    paperImg.rectTransform.pivot = new Vector2(0.5f, 1f);
                    paperImg.rectTransform.anchoredPosition = new Vector2(0, -72f);
                    paperImg.rectTransform.sizeDelta = new Vector2(340f, 72f);
                }
                bandRt.anchorMin = new Vector2(0f, 1f);
                bandRt.anchorMax = new Vector2(1f, 1f);
                bandRt.pivot = new Vector2(0.5f, 1f);
                bandRt.anchoredPosition = Vector2.zero;
                bandRt.sizeDelta = new Vector2(0f, 132f);
                pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 1f);
                pr.pivot = new Vector2(0.5f, 1f);
                pr.anchoredPosition = new Vector2(0, -70f);
                pr.sizeDelta = new Vector2(320f, 56f);
                promptTxt.alignment = TextAlignmentOptions.Center;
                promptTxt.fontSizeMax = 14;
                gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 1f);
                gr.pivot = new Vector2(0.5f, 1f);
                gr.anchoredPosition = new Vector2(0, -122f);
                gr.sizeDelta = new Vector2(320f, 28f);
                goalTxt.alignment = TextAlignmentOptions.Center;
                goalTxt.fontSize = 13;
                if (livesRt)
                {
                    // 상단 타이머 옆 — 독 타일·16 숫자와 겹치지 않게.
                    livesRt.anchorMin = livesRt.anchorMax = new Vector2(0f, 1f);
                    livesRt.pivot = new Vector2(0f, 1f);
                    livesRt.anchoredPosition = new Vector2(86f, -32f);
                    livesRt.sizeDelta = new Vector2(108f, 28f);
                }
                if (skipRt)
                {
                    skipRt.anchorMin = skipRt.anchorMax = new Vector2(1f, 0f);
                    skipRt.pivot = new Vector2(1f, 0f);
                    skipRt.anchoredPosition = new Vector2(-16f, 16f);
                }
            }
            LayoutLivesChildren();
        }

        void LayoutLivesChildren()
        {
            if (lifeCap)
            {
                lifeCap.gameObject.SetActive(land);
                if (land)
                {
                    var rt = lifeCap.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0, -6);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                if (!lifeImg[i]) continue;
                var rt = lifeImg[i].rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                if (land)
                {
                    rt.anchoredPosition = new Vector2((i - 1) * 48f, -8f);
                    rt.sizeDelta = new Vector2(34f, 34f);
                }
                else
                {
                    rt.anchoredPosition = new Vector2((i - 1) * 36f, -4f);
                    rt.sizeDelta = new Vector2(26f, 26f);
                }
            }
            if (sheetFill && sheetFill.transform.parent is RectTransform bg)
            {
                if (land)
                {
                    bg.anchorMin = new Vector2(0.5f, 1f);
                    bg.anchorMax = new Vector2(0.5f, 1f);
                    bg.anchoredPosition = new Vector2(0, 16);
                    bg.sizeDelta = new Vector2(200, 10);
                }
                else
                {
                    bg.anchorMin = new Vector2(0.5f, 0f);
                    bg.anchorMax = new Vector2(0.5f, 0f);
                    bg.pivot = new Vector2(0.5f, 0.5f);
                    bg.anchoredPosition = new Vector2(0, 4);
                    bg.sizeDelta = new Vector2(110, 6);
                }
            }
        }

        void SetVisible()
        {
            if (titleG) titleG.alpha = ph == Ph.Title ? 1 : 0;
            if (titleG) titleG.blocksRaycasts = ph == Ph.Title;
            if (endG) endG.alpha = ph == Ph.End ? 1 : 0;
            if (endG) endG.blocksRaycasts = ph == Ph.End;
            if (hudG) hudG.alpha = (ph == Ph.Play || ph == Ph.Practice) ? 1 : 0;
            if (bandRt) bandRt.gameObject.SetActive(ph == Ph.Play || ph == Ph.Practice);
            if (livesRt) livesRt.gameObject.SetActive(ph == Ph.Play || ph == Ph.Practice);
            if (ph == Ph.End) FillEnd();
            LayoutHud();
            RefreshHud();
        }

        void RefreshHud()
        {
            if (promptTxt && cur != null && ph != Ph.Title)
            {
                string p = Words.Prompt(cur);
                if (promptTxt.text != p) promptTxt.text = p;
            }
            if (goalTxt && ph == Ph.Practice && revealT <= 0)
                goalTxt.text = cur != null ? Words.Goal(cur) : "두 넓이판을 빗변 쟁반에 얹고 잠가라";
            else if (goalTxt && cur != null && revealT <= 0) goalTxt.text = Words.Goal(cur);
            if (lockTxt) lockTxt.text = st.solved + " / " + Rules.Locks;
            if (comboTxt) comboTxt.text = st.combo >= 2 ? "×" + Rules.Mult(st.combo) : "";
            if (scoreTxt && shownScoreInt < 0) scoreTxt.text = st.score.ToString();
            for (int i = 0; i < 3; i++)
                if (lifeImg[i]) lifeImg[i].color = i < st.lives ? Terra : new Color(0.55f, 0.5f, 0.45f, 0.35f);
        }

        void FillEnd()
        {
            bool clear = endReason == "clear";
            if (endHead) endHead.text = clear ? "가마 닫힘" : (endReason == "time" ? "공방 닫힘" : "금 간 기와 소진");
            if (endStats)
                endStats.text = "잠금  " + st.solved + "\n점수  " + st.score + "\n최장 콤보  " + st.maxCombo
                    + (bestScore > 0 ? "\n최고  " + bestScore : "");
            if (endCtaTxt) endCtaTxt.text = "다시 얹기";
        }

        void toast(string s, float dur)
        {
            if (string.IsNullOrEmpty(s)) return;
            toastDur = dur; toastT = dur;
            if (toastTxt) toastTxt.text = s;
            if (toastG) toastG.alpha = 1;
        }
        void HideToast() { toastT = 0; if (toastG) toastG.alpha = 0; }

        void SpawnRipple(Vector2 screen)
        {
            int i = rippleNext++ % ripples.Length;
            rippleT[i] = 0;
            if (!ripples[i]) return;
            ripples[i].enabled = true;
            var rt = ripples[i].rectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)MgfText.Canvas.transform, screen, null, out var lp);
            rt.anchoredPosition = lp;
            rt.sizeDelta = new Vector2(36, 36);
        }

        void TickRipples(float dt)
        {
            for (int i = 0; i < ripples.Length; i++)
            {
                if (!ripples[i] || !ripples[i].enabled) continue;
                rippleT[i] += dt;
                float u = rippleT[i] / 0.45f;
                if (u >= 1f) { ripples[i].enabled = false; continue; }
                float s = 36f + 70f * u;
                ripples[i].rectTransform.sizeDelta = new Vector2(s, s);
                var c = ripples[i].color; c.a = 0.4f * (1f - u); ripples[i].color = c;
            }
        }
    }
}
