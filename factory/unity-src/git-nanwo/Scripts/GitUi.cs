// 깃 나눠 — 화면 UI. 먹선 재단 주문서 + 놋쇠 강조. 스티커 그림자·광택 없음.
// 세로: 상단 주문서 한 줄, 하단 예비 깃. 가로 1280: 좌 삼각기 · 우 주문서+예비 깃+완성 더미.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.GitNanwo
{
    public partial class GitNanwoGame
    {
        CanvasGroup hudG, titleG, endG, toastG, skipG;
        TextMeshProUGUI timeTxt, scoreTxt, comboTxt, promptTxt, goalTxt, bestTxt, tagTxt, badgeTxt, toastTxt;
        TextMeshProUGUI endHead, endStats, endCtaTxt, ctaTxt, skipTxt, cutTxt, flagCap;
        readonly TextMeshProUGUI[] logo = new TextMeshProUGUI[3];
        Image toastBg, timeFill, sheetFill, ctaPlate, endPlate;
        RectTransform titleRt, endRt, ctaRt, hudRt, skipRt, bandRt, flagsRt, paneRt;
        Image paperImg, paneImg;
        Sprite roundSpr, ringSpr;
        readonly Image[] ripples = new Image[4];
        readonly float[] rippleT = { 9, 9, 9, 9 };
        int rippleNext;
        readonly Image[] flagImg = new Image[3];
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

        static Sprite PennantSprite(int S)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "PennantHud", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            float ax = S * 0.5f, ay = S * 0.90f;
            float bx = S * 0.10f, by = S * 0.12f;
            float cx = S * 0.90f, cy = S * 0.12f;
            float area = (by - ay) * (cx - ax) + (ax - bx) * (cy - ay);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float px0 = x + 0.5f, py0 = y + 0.5f;
                float w1 = ((by - ay) * (px0 - ax) + (ax - bx) * (py0 - ay)) / area;
                float w2 = ((ay - cy) * (px0 - ax) + (cx - ax) * (py0 - ay)) / area;
                float w0 = 1f - w1 - w2;
                float a = (w0 >= -0.02f && w1 >= -0.02f && w2 >= -0.02f) ? 1f : 0f;
                if (py0 < S * 0.20f && a > 0) a = 0.85f;
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

            paneRt = R("Pane", ct, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(380f, 0f));
            paneRt.anchorMin = new Vector2(1f, 0f); paneRt.anchorMax = new Vector2(1f, 1f); paneRt.pivot = new Vector2(1f, 0.5f);
            paneImg = paneRt.gameObject.AddComponent<Image>();
            paneImg.color = new Color(Cream.r, Cream.g, Cream.b, 0.97f);
            paneImg.raycastTarget = false;
            paneRt.gameObject.SetActive(false);

            hudRt = R("Hud", ct, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0, 48));
            hudRt.anchorMin = new Vector2(0, 1); hudRt.anchorMax = new Vector2(1, 1); hudRt.pivot = new Vector2(0.5f, 1f);
            hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            var tb = Img(hudRt, "TimeBar", new Vector2(0.5f, 1f), new Vector2(0, -6), new Vector2(0, 8), new Color(InkC.r, InkC.g, InkC.b, 0.18f), roundSpr);
            tb.rectTransform.anchorMin = new Vector2(0, 1); tb.rectTransform.anchorMax = new Vector2(1, 1); tb.rectTransform.sizeDelta = new Vector2(-28, 8);
            timeFill = Img(tb.transform, "Fill", new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, Brass, roundSpr);
            var tfr = timeFill.rectTransform; tfr.anchorMin = Vector2.zero; tfr.anchorMax = Vector2.one; tfr.pivot = new Vector2(0, 0.5f); tfr.sizeDelta = Vector2.zero;
            timeTxt = Txt(hudRt, "90", new Vector2(0f, 1f), new Vector2(48, -30), 26, InkC, 80, TextAlignmentOptions.Left);
            timeTxt.fontStyle = FontStyles.Bold;
            scoreTxt = Txt(hudRt, "0", new Vector2(1f, 1f), new Vector2(-52, -26), 24, InkC, 100, TextAlignmentOptions.Right);
            scoreTxt.fontStyle = FontStyles.Bold;
            comboTxt = Txt(hudRt, "", new Vector2(1f, 1f), new Vector2(-52, -48), 13, Brass, 110, TextAlignmentOptions.Right);
            cutTxt = Txt(hudRt, "0 / 10", new Vector2(0.5f, 1f), new Vector2(0, -18), 14, InkC, 140);

            bandRt = R("Band", ct, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0, 132));
            paperImg = Img(bandRt, "Paper", new Vector2(0.5f, 1f), new Vector2(0, -72), new Vector2(340, 72), new Color(Cream.r, Cream.g, Cream.b, 0.94f), roundSpr);
            paperImg.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            promptTxt = Txt(bandRt, "", new Vector2(0.5f, 1f), new Vector2(0, -70), 13, InkC, 320);
            promptTxt.fontStyle = FontStyles.Bold;
            promptTxt.enableAutoSizing = true;
            promptTxt.fontSizeMin = 11;
            promptTxt.fontSizeMax = 14;
            promptTxt.overflowMode = TextOverflowModes.Ellipsis;
            promptTxt.lineSpacing = -6f;
            goalTxt = Txt(bandRt, "밀대를 눌러 길이의 비를 맞춰라", new Vector2(0.5f, 1f), new Vector2(0, -122), 13, Brass, 320);

            flagsRt = R("Flags", ct, new Vector2(0.5f, 0f), new Vector2(0, 36), new Vector2(220, 70));
            flagCap = Txt(flagsRt, "예비 깃", new Vector2(0.5f, 1f), new Vector2(0, -6), 11, InkC, 140);
            var flagSpr = PennantSprite(64);
            for (int i = 0; i < 3; i++)
                flagImg[i] = Img(flagsRt, "F" + i, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 48f, -8), new Vector2(30, 40), Celadon, flagSpr);
            var sheetBg = Img(flagsRt, "SheetBg", new Vector2(0.5f, 1f), new Vector2(0, 16), new Vector2(200, 10), new Color(InkC.r, InkC.g, InkC.b, 0.2f), roundSpr);
            sheetFill = Img(sheetBg.transform, "Fill", new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, Brass, roundSpr);
            var sfr = sheetFill.rectTransform; sfr.anchorMin = Vector2.zero; sfr.anchorMax = Vector2.one; sfr.pivot = new Vector2(0, 0.5f); sfr.sizeDelta = Vector2.zero;

            skipRt = R("Skip", ct, new Vector2(0.5f, 0f), new Vector2(0, 108), new Vector2(220, 52));
            skipG = skipRt.gameObject.AddComponent<CanvasGroup>(); skipG.alpha = 0; skipG.blocksRaycasts = false;
            Img(skipRt, "P", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 52), Brass, roundSpr).raycastTarget = false;
            skipTxt = Txt(skipRt, "본판으로", new Vector2(0.5f, 0.5f), Vector2.zero, 20, Cream, 200);
            skipTxt.fontStyle = FontStyles.Bold;

            toastBg = Img(ct, "Toast", new Vector2(0.5f, 0f), new Vector2(0, 156), new Vector2(340, 52), new Color(InkC.r, InkC.g, InkC.b, 0.88f), roundSpr);
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
            // 로고: 헴 스티치처럼 세 겹 — 그림자·옥색 면·먹 획. 낱글자 필 템플릿 아님.
            logo[0] = Txt(titleRt, "깃 나눠", new Vector2(0.5f, 0.96f), new Vector2(3, -3), 48, new Color(0.12f, 0.16f, 0.14f, 0.45f), 420);
            logo[1] = Txt(titleRt, "깃 나눠", new Vector2(0.5f, 0.96f), new Vector2(-1, 2), 48, Celadon, 420);
            logo[2] = Txt(titleRt, "깃 나눠", new Vector2(0.5f, 0.96f), Vector2.zero, 48, InkC, 420);
            for (int i = 0; i < 3; i++) logo[i].fontStyle = FontStyles.Bold;
            tagTxt = Txt(titleRt, "밀대를 눌러 멈춰라", new Vector2(0.5f, 0.88f), Vector2.zero, 18, Brass, 360);
            tagTxt.fontStyle = FontStyles.Bold;
            badgeTxt = Txt(titleRt, "중2  ·  평행선과 선분의 길이의 비", new Vector2(0.5f, 0.82f), Vector2.zero, 13, InkC, 360);
            bestTxt = Txt(titleRt, "", new Vector2(0.5f, 0.16f), Vector2.zero, 13, InkC, 360);
            ctaRt = R("Cta", titleRt, new Vector2(0.5f, 0.07f), Vector2.zero, new Vector2(280, 68));
            ctaPlate = Img(ctaRt, "Plate", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 68), Brass, roundSpr);
            ctaPlate.raycastTarget = true;
            ctaTxt = Txt(ctaRt, "시작하기", new Vector2(0.5f, 0.5f), new Vector2(0, 2), 26, Cream, 260);
            ctaTxt.fontStyle = FontStyles.Bold;

            endG = R("End", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<CanvasGroup>();
            endRt = endG.GetComponent<RectTransform>();
            endRt.anchorMin = Vector2.zero; endRt.anchorMax = Vector2.one; endRt.sizeDelta = Vector2.zero;
            Img(endRt, "Sheet", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(320, 280), new Color(Beech.r, Beech.g, Beech.b, 0.95f), roundSpr);
            endHead = Txt(endRt, "", new Vector2(0.5f, 0.64f), Vector2.zero, 32, InkC, 300);
            endHead.fontStyle = FontStyles.Bold;
            endStats = Txt(endRt, "", new Vector2(0.5f, 0.50f), Vector2.zero, 17, InkC, 300);
            var endCta = R("EndCta", endRt, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(260, 64));
            endPlate = Img(endCta, "P", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 64), Brass, roundSpr);
            endPlate.raycastTarget = true;
            endCtaTxt = Txt(endCta, "다시 재단", new Vector2(0.5f, 0.5f), Vector2.zero, 24, Cream, 240);
            endCtaTxt.fontStyle = FontStyles.Bold;

            LayoutHud();
        }

        void LayoutHud()
        {
            if (bandRt == null || promptTxt == null) return;
            var pr = promptTxt.rectTransform;
            var gr = goalTxt.rectTransform;
            if (paneRt) paneRt.gameObject.SetActive(land && (ph == Ph.Play || ph == Ph.Practice));
            if (land)
            {
                if (paneRt)
                {
                    paneRt.anchorMin = new Vector2(1f, 0f);
                    paneRt.anchorMax = new Vector2(1f, 1f);
                    paneRt.pivot = new Vector2(1f, 0.5f);
                    paneRt.sizeDelta = new Vector2(380f, 0f);
                    paneRt.anchoredPosition = Vector2.zero;
                }
                bandRt.anchorMin = new Vector2(1f, 1f);
                bandRt.anchorMax = new Vector2(1f, 1f);
                bandRt.pivot = new Vector2(1f, 1f);
                bandRt.anchoredPosition = new Vector2(0f, -48f);
                bandRt.sizeDelta = new Vector2(380f, 280f);
                if (paperImg)
                {
                    paperImg.enabled = false;
                }
                pr.anchorMin = new Vector2(0f, 1f); pr.anchorMax = new Vector2(1f, 1f);
                pr.pivot = new Vector2(0.5f, 1f);
                pr.anchoredPosition = new Vector2(0f, -16f);
                pr.sizeDelta = new Vector2(-36f, 200f);
                promptTxt.alignment = TextAlignmentOptions.TopLeft;
                promptTxt.enableAutoSizing = true;
                promptTxt.fontSizeMin = 14;
                promptTxt.fontSizeMax = 18;
                promptTxt.overflowMode = TextOverflowModes.Overflow;
                gr.anchorMin = new Vector2(0f, 1f); gr.anchorMax = new Vector2(1f, 1f);
                gr.pivot = new Vector2(0.5f, 1f);
                gr.anchoredPosition = new Vector2(0f, -224f);
                gr.sizeDelta = new Vector2(-36f, 48f);
                goalTxt.alignment = TextAlignmentOptions.TopLeft;
                if (flagsRt)
                {
                    flagsRt.anchorMin = flagsRt.anchorMax = new Vector2(1f, 0f);
                    flagsRt.pivot = new Vector2(1f, 0f);
                    flagsRt.anchoredPosition = new Vector2(-80f, 36f);
                }
                if (skipRt)
                {
                    skipRt.anchorMin = skipRt.anchorMax = new Vector2(1f, 0f);
                    skipRt.pivot = new Vector2(1f, 0f);
                    skipRt.anchoredPosition = new Vector2(-190f, 118f);
                }
            }
            else
            {
                if (paperImg)
                {
                    paperImg.enabled = true;
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
                promptTxt.enableAutoSizing = true;
                promptTxt.fontSizeMin = 11;
                promptTxt.fontSizeMax = 14;
                promptTxt.overflowMode = TextOverflowModes.Ellipsis;
                gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 1f);
                gr.pivot = new Vector2(0.5f, 1f);
                gr.anchoredPosition = new Vector2(0, -122f);
                gr.sizeDelta = new Vector2(320f, 28f);
                goalTxt.alignment = TextAlignmentOptions.Center;
                if (flagsRt)
                {
                    flagsRt.anchorMin = flagsRt.anchorMax = new Vector2(0.5f, 0f);
                    flagsRt.pivot = new Vector2(0.5f, 0f);
                    flagsRt.anchoredPosition = new Vector2(0, 36f);
                }
                if (skipRt)
                {
                    skipRt.anchorMin = skipRt.anchorMax = new Vector2(0.5f, 0f);
                    skipRt.pivot = new Vector2(0.5f, 0f);
                    skipRt.anchoredPosition = new Vector2(0, 108f);
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
            if (flagsRt) flagsRt.gameObject.SetActive(ph == Ph.Play || ph == Ph.Practice);
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
            if (goalTxt && ph == Ph.Practice && revealT <= 0) goalTxt.text = "유령 밀대가 서는 칸에서 눌러라";
            else if (goalTxt && cur != null && revealT <= 0) goalTxt.text = Words.Goal(cur);
            if (cutTxt) cutTxt.text = st.solved + " / " + Rules.Cuts;
            if (comboTxt) comboTxt.text = st.combo >= 2 ? "×" + Rules.Mult(st.combo) : "";
            if (scoreTxt && shownScoreInt < 0) scoreTxt.text = st.score.ToString();
            for (int i = 0; i < 3; i++)
                if (flagImg[i]) flagImg[i].color = i < st.lives ? Celadon : new Color(0.55f, 0.5f, 0.45f, 0.4f);
            if (sheetFill && sheetTimed)
            {
                float u = Mathf.Clamp01(sheetLeft / Rules.SheetSec);
                sheetFill.rectTransform.anchorMax = new Vector2(u, 1f);
            }
            else if (sheetFill) sheetFill.rectTransform.anchorMax = new Vector2(1f, 1f);
        }

        void FillEnd()
        {
            bool clear = endReason == "clear";
            if (endHead) endHead.text = clear ? "재단 끝" : (endReason == "time" ? "시간 종료" : "예비 깃 소진");
            if (endStats)
                endStats.text = "정확 컷  " + st.solved + "\n점수  " + st.score + "\n최장 콤보  " + st.maxCombo
                    + (bestScore > 0 ? "\n최고  " + bestScore : "");
            if (endCtaTxt) endCtaTxt.text = "다시 재단";
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
            var r = ripples[i];
            r.enabled = true;
            r.rectTransform.anchorMin = r.rectTransform.anchorMax = new Vector2(
                screen.x / Mathf.Max(1, Screen.width), screen.y / Mathf.Max(1, Screen.height));
            r.rectTransform.anchoredPosition = Vector2.zero;
            r.rectTransform.sizeDelta = Vector2.one * 36f;
            rippleT[i] = 0;
        }

        void TickRipples(float dt)
        {
            for (int i = 0; i < ripples.Length; i++)
            {
                if (!ripples[i].enabled) continue;
                rippleT[i] += dt;
                float k = rippleT[i] / 0.45f;
                ripples[i].rectTransform.sizeDelta = Vector2.one * (36f + 90f * k);
                var c = ripples[i].color; c.a = 0.4f * (1f - k); ripples[i].color = c;
                if (k >= 1f) ripples[i].enabled = false;
            }
        }

        void TickCta(float dt)
        {
            if (!ctaRt) return;
            if (pulseTm > 0 && ph == Ph.Title)
            {
                ctaPress = 1f;
                float k = 1f - 0.12f * Mathf.Sin(pulseTm / 0.16f * Mathf.PI);
                ctaRt.localScale = new Vector3(k, k * 0.92f, 1f);
                ctaRt.anchoredPosition = new Vector2(0, -4f * (1f - k));
            }
            else
            {
                ctaPress = Mathf.MoveTowards(ctaPress, 0, dt * 6f);
                float k = 1f - 0.08f * ctaPress;
                ctaRt.localScale = Vector3.one * k;
            }
            // 타이틀 로고 미세 호흡
            if (ph == Ph.Title && logo[2])
            {
                float b = 1f + 0.015f * Mathf.Sin(titleT * 1.4f);
                logo[2].rectTransform.localScale = Vector3.one * b;
                logo[1].rectTransform.localScale = Vector3.one * b;
                logo[0].rectTransform.localScale = Vector3.one * b;
            }
        }
    }
}
