// 석회 착지 — 화면 UI. 콘크리트 위 석회 흰 글자. 스티커 그림자·광택 없음.
// 세로: 상단 주문+시간, 하단 노란 카드 3장(삼각 밖). 가로 1280: 좌 고정 패널(잘림 없는 폭) · 우 카드.
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.SeokhoeChakji
{
    public partial class SeokhoeChakjiGame
    {
        CanvasGroup hudG, titleG, endG, toastG, skipG;
        TextMeshProUGUI timeTxt, scoreTxt, comboTxt, promptTxt, goalTxt, bestTxt, tagTxt, badgeTxt, toastTxt;
        TextMeshProUGUI endHead, endStats, endCtaTxt, ctaTxt, logoUi, cardCap, skipTxt;
        readonly Image[] cardImg = new Image[3];
        Image toastBg, timeFill, sheetFill, ctaPlate, bandDim;
        RectTransform titleRt, endRt, bandRt, ctaRt, hudRt, cardsRt, skipRt;
        Sprite roundSpr;
        string lastPrompt;

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

        static Sprite RoundSprite(int S, float rad)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Round", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float qx = Mathf.Max(Mathf.Abs(x + 0.5f - S / 2f) - (S / 2f - rad), 0);
                float qy = Mathf.Max(Mathf.Abs(y + 0.5f - S / 2f) - (S / 2f - rad), 0);
                float a = Mathf.Clamp01(rad - Mathf.Sqrt(qx * qx + qy * qy) + 0.5f);
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(rad + 2, rad + 2, rad + 2, rad + 2));
        }

        void BuildUi()
        {
            var canvas = MgfText.Canvas;
            var ct = canvas.transform;
            roundSpr = RoundSprite(48, 8);

            hudRt = R("Hud", ct, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0, 48));
            hudRt.anchorMin = new Vector2(0, 1); hudRt.anchorMax = new Vector2(1, 1); hudRt.pivot = new Vector2(0.5f, 1f);
            hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            var tb = Img(hudRt, "TimeBar", new Vector2(0.5f, 1f), new Vector2(0, -4), new Vector2(0, 6), new Color(InkC.r, InkC.g, InkC.b, 0.28f), roundSpr);
            tb.rectTransform.anchorMin = new Vector2(0, 1); tb.rectTransform.anchorMax = new Vector2(1, 1); tb.rectTransform.sizeDelta = new Vector2(-24, 6);
            timeFill = Img(tb.transform, "Fill", new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, Cream, roundSpr);
            var tfr = timeFill.rectTransform; tfr.anchorMin = Vector2.zero; tfr.anchorMax = Vector2.one; tfr.pivot = new Vector2(0, 0.5f); tfr.sizeDelta = Vector2.zero;
            timeTxt = Txt(hudRt, "90", new Vector2(0f, 1f), new Vector2(52, -28), 28, Cream, 90, TextAlignmentOptions.Left);
            timeTxt.fontStyle = FontStyles.Bold;
            scoreTxt = Txt(hudRt, "0", new Vector2(1f, 1f), new Vector2(-56, -24), 24, Cream, 110, TextAlignmentOptions.Right);
            scoreTxt.fontStyle = FontStyles.Bold;
            comboTxt = Txt(hudRt, "", new Vector2(1f, 1f), new Vector2(-56, -46), 14, Yel, 110, TextAlignmentOptions.Right);

            bandRt = R("Band", ct, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0, 150));
            bandDim = Img(bandRt, "Dim", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.12f, 0.10f, 0.08f, 0.38f));
            bandDim.rectTransform.anchorMin = Vector2.zero;
            bandDim.rectTransform.anchorMax = Vector2.one;
            bandDim.rectTransform.sizeDelta = Vector2.zero;
            bandDim.enabled = false;
            promptTxt = Txt(bandRt, "", new Vector2(0.5f, 1f), new Vector2(0, -72), 16, Cream, 360);
            promptTxt.fontStyle = FontStyles.Bold;
            goalTxt = Txt(bandRt, "칸 탭 = hop  ·  쐐기 탭 = 착지", new Vector2(0.5f, 1f), new Vector2(0, -128), 15, Yel, 340);

            cardsRt = R("Cards", ct, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(220, 86));
            cardCap = Txt(cardsRt, "주의 카드", new Vector2(0.5f, 1f), new Vector2(0, -8), 11, Cream, 160);
            for (int i = 0; i < 3; i++)
            {
                cardImg[i] = Img(cardsRt, "Card" + i, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 52f, -8), new Vector2(36, 22), Yel, roundSpr);
                cardImg[i].rectTransform.localRotation = Quaternion.Euler(0, 0, (i - 1) * 8f);
            }
            skipRt = R("Skip", ct, new Vector2(0.5f, 0f), new Vector2(0, 96), new Vector2(240, 56));
            skipG = skipRt.gameObject.AddComponent<CanvasGroup>(); skipG.alpha = 0; skipG.blocksRaycasts = false;
            Img(skipRt, "P", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 56), Yel, roundSpr).raycastTarget = false;
            skipTxt = Txt(skipRt, "본판으로", new Vector2(0.5f, 0.5f), Vector2.zero, 22, InkC, 220);
            skipTxt.fontStyle = FontStyles.Bold;
            var sheetBg = Img(cardsRt, "SheetBg", new Vector2(0.5f, 1f), new Vector2(0, 22), new Vector2(200, 14), new Color(InkC.r, InkC.g, InkC.b, 0.45f), roundSpr);
            sheetFill = Img(sheetBg.transform, "Fill", new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, Yel, roundSpr);
            var sfr = sheetFill.rectTransform; sfr.anchorMin = Vector2.zero; sfr.anchorMax = Vector2.one; sfr.pivot = new Vector2(0, 0.5f); sfr.sizeDelta = Vector2.zero;

            toastBg = Img(ct, "Toast", new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(340, 56), new Color(InkC.r, InkC.g, InkC.b, 0.9f), roundSpr);
            toastG = toastBg.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0;
            toastTxt = Txt(toastBg.transform, "", new Vector2(0.5f, 0.5f), Vector2.zero, 14, Cream, 320);

            titleG = R("Title", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<CanvasGroup>();
            titleRt = titleG.GetComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one; titleRt.sizeDelta = Vector2.zero;
            logoUi = Txt(titleRt, "석회 착지", new Vector2(0.5f, 0.88f), Vector2.zero, 36, Cream, 420);
            logoUi.fontStyle = FontStyles.Bold;
            tagTxt = Txt(titleRt, "칸을 눌러 내려가, 맞는 칸에서 쐐기를 눌러라", new Vector2(0.5f, 0.78f), Vector2.zero, 15, Yel, 380);
            badgeTxt = Txt(titleRt, "중2  ·  평행선과 선분의 길이의 비", new Vector2(0.5f, 0.72f), Vector2.zero, 13, Cream, 340);
            bestTxt = Txt(titleRt, "", new Vector2(0.5f, 0.18f), Vector2.zero, 13, Cream, 340);
            ctaRt = R("Cta", titleRt, new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(280, 68));
            ctaPlate = Img(ctaRt, "Plate", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 68), Yel, roundSpr);
            ctaPlate.raycastTarget = true;
            ctaTxt = Txt(ctaRt, "시작하기", new Vector2(0.5f, 0.5f), Vector2.zero, 26, InkC, 260);
            ctaTxt.fontStyle = FontStyles.Bold;

            endG = R("End", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<CanvasGroup>();
            endRt = endG.GetComponent<RectTransform>();
            endRt.anchorMin = Vector2.zero; endRt.anchorMax = Vector2.one; endRt.sizeDelta = Vector2.zero;
            endHead = Txt(endRt, "", new Vector2(0.5f, 0.62f), Vector2.zero, 34, Cream, 340);
            endHead.fontStyle = FontStyles.Bold;
            endStats = Txt(endRt, "", new Vector2(0.5f, 0.5f), Vector2.zero, 18, Cream, 340);
            var endCta = R("EndCta", endRt, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(260, 64));
            Img(endCta, "P", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 64), Yel, roundSpr).raycastTarget = true;
            endCtaTxt = Txt(endCta, "다시 착지", new Vector2(0.5f, 0.5f), Vector2.zero, 24, InkC, 240);
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
                bandRt.anchorMin = new Vector2(0f, 0f);
                bandRt.anchorMax = new Vector2(0f, 1f);
                bandRt.pivot = new Vector2(0f, 0.5f);
                bandRt.anchoredPosition = Vector2.zero;
                bandRt.sizeDelta = new Vector2(400f, 0f);
                if (bandDim) bandDim.enabled = true;
                pr.anchorMin = pr.anchorMax = new Vector2(0f, 1f);
                pr.pivot = new Vector2(0f, 1f);
                pr.anchoredPosition = new Vector2(18f, -52f);
                pr.sizeDelta = new Vector2(364f, 320f);
                promptTxt.alignment = TextAlignmentOptions.TopLeft;
                promptTxt.fontSize = 16;
                promptTxt.textWrappingMode = TextWrappingModes.Normal;
                promptTxt.overflowMode = TextOverflowModes.Overflow;
                gr.anchorMin = gr.anchorMax = new Vector2(0f, 1f);
                gr.pivot = new Vector2(0f, 1f);
                gr.anchoredPosition = new Vector2(18f, -380f);
                gr.sizeDelta = new Vector2(364f, 72f);
                goalTxt.alignment = TextAlignmentOptions.TopLeft;
                if (scoreTxt) scoreTxt.rectTransform.anchoredPosition = new Vector2(-56f, -24f);
                if (cardsRt)
                {
                    cardsRt.anchorMin = cardsRt.anchorMax = new Vector2(1f, 0f);
                    cardsRt.pivot = new Vector2(1f, 0f);
                    cardsRt.anchoredPosition = new Vector2(-24f, 24f);
                }
            }
            else
            {
                bandRt.anchorMin = new Vector2(0f, 1f);
                bandRt.anchorMax = new Vector2(1f, 1f);
                bandRt.pivot = new Vector2(0.5f, 1f);
                bandRt.anchoredPosition = Vector2.zero;
                bandRt.sizeDelta = new Vector2(0f, 176f);
                if (bandDim) bandDim.enabled = false;
                pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 1f);
                pr.pivot = new Vector2(0.5f, 0.5f);
                pr.anchoredPosition = new Vector2(0f, -86f);
                pr.sizeDelta = new Vector2(360f, 88f);
                promptTxt.alignment = TextAlignmentOptions.Center;
                promptTxt.fontSize = 16;
                gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 1f);
                gr.pivot = new Vector2(0.5f, 0.5f);
                gr.anchoredPosition = new Vector2(0f, -150f);
                gr.sizeDelta = new Vector2(340f, 48f);
                goalTxt.alignment = TextAlignmentOptions.Center;
                if (cardsRt)
                {
                    cardsRt.anchorMin = cardsRt.anchorMax = new Vector2(0.5f, 0f);
                    cardsRt.pivot = new Vector2(0.5f, 0f);
                    cardsRt.anchoredPosition = new Vector2(0f, 18f);
                }
            }
        }

        void RefreshHud()
        {
            if (timeTxt)
            {
                int sec = Mathf.CeilToInt(Mathf.Max(0, ph == Ph.Play ? runLeft : Rules.RunSec));
                if (sec != lastTimeInt) { timeTxt.text = sec.ToString(); lastTimeInt = sec; }
            }
            if (timeFill)
            {
                float u = ph == Ph.Play ? Mathf.Clamp01(runLeft / Rules.RunSec) : 1f;
                timeFill.rectTransform.anchorMax = new Vector2(u, 1f);
            }
            if (shownScore < st.score) shownScore = Mathf.MoveTowards(shownScore, st.score, Mathf.Max(40f, (st.score - shownScore) * 4f) * Time.deltaTime);
            int si = Mathf.RoundToInt(shownScore);
            if (si != shownScoreInt && scoreTxt) { scoreTxt.text = si.ToString(); shownScoreInt = si; }
            if (comboTxt) comboTxt.text = st.combo >= 2 ? "×" + Rules.Mult(st.combo) : "";
            for (int i = 0; i < 3; i++)
            {
                if (!cardImg[i]) continue;
                bool on = i < st.lives;
                cardImg[i].color = on ? Yel : new Color(0.25f, 0.18f, 0.12f, 0.45f);
                float slide = (!on && cardSlide > 0 && i == st.lives) ? (1f - cardSlide / 0.55f) * 14f : 0;
                cardImg[i].rectTransform.anchoredPosition = new Vector2((i - 1) * 52f, -8f - slide);
            }
            if (promptTxt && (ph == Ph.Play || ph == Ph.Practice))
            {
                string p = cur != null ? Words.Prompt(cur) : "";
                string foot = cur != null ? Words.Foot(cur) : "";
                string all = string.IsNullOrEmpty(foot) ? p : p + "\n" + foot;
                if (all != lastPrompt) { promptTxt.text = all; lastPrompt = all; }
            }
            if (sheetFill)
            {
                bool show = ph == Ph.Play && !lockedThis && sheetTimed;
                sheetFill.enabled = show;
                if (sheetFill.transform.parent)
                {
                    var bg = sheetFill.transform.parent.GetComponent<Image>();
                    if (bg) bg.enabled = show;
                }
                if (show)
                {
                    float u = Mathf.Clamp01(sheetLeft / Rules.SheetSec);
                    sheetFill.rectTransform.anchorMax = new Vector2(u, 1f);
                }
            }
        }

        void SetVisible()
        {
            if (hudG) hudG.alpha = (ph == Ph.Play || ph == Ph.Practice) ? 1 : 0;
            if (titleG) titleG.alpha = ph == Ph.Title ? 1 : 0;
            if (endG) endG.alpha = ph == Ph.End ? 1 : 0;
            if (logoW) logoW.gameObject.SetActive(false);
            if (logoUi) logoUi.gameObject.SetActive(ph == Ph.Title);
            if (fingerT) fingerT.gameObject.SetActive(ph == Ph.Practice);
            if (bandRt) bandRt.gameObject.SetActive(ph == Ph.Play || ph == Ph.Practice);
            if (cardsRt) cardsRt.gameObject.SetActive(ph == Ph.Play);
            if (skipG)
            {
                skipG.alpha = (ph == Ph.Practice && idleT > 12f) ? 1f : 0f;
                skipG.blocksRaycasts = skipG.alpha > 0.5f;
            }
            if (ph == Ph.Practice && goalTxt) goalTxt.text = "4 cm 칸을 눌러 착지";
            if (ph == Ph.Play && goalTxt) goalTxt.text = Words.Goal(cur);
            if (ph == Ph.End)
            {
                if (endReason == "clear") endHead.text = "10회 착지";
                else if (endReason == "time") endHead.text = "휘슬";
                else endHead.text = "노란 카드 3장";
                endStats.text = "점수  " + st.score + "   착지  " + st.solved + " / 10   콤보  " + st.maxCombo;
            }
            RefreshHud();
        }

        void AnimateUi(float dt)
        {
            if (toastG)
            {
                toastT += dt;
                toastG.alpha = toastT < toastDur ? 1f : Mathf.MoveTowards(toastG.alpha, 0, dt * 4f);
            }
            if (ph == Ph.Title && ctaRt)
            {
                ctaRt.localRotation = Quaternion.Euler(0, 0, swingAmp * 0.35f);
                float s = 1f + 0.03f * Mathf.Sin(titleT * 2.2f);
                ctaRt.localScale = new Vector3(s, s, 1f);
            }
            if (pulseGo)
            {
                bool show = pulseTm > 0 || ph == Ph.Practice;
                pulseGo.gameObject.SetActive(show && cur != null);
                if (show && cur != null)
                {
                    int aim = ph == Ph.Practice ? (obStep == 0 ? 4 : cur.ad) : cur.ad;
                    var p = RungMid(cur, Mathf.Clamp(aim, cur.adLo, cur.adHi));
                    pulseGo.position = W(p, 0.12f);
                    float k = 1f + 0.18f * Mathf.Sin(Time.time * 6f);
                    pulseGo.localScale = new Vector3(k, 0.02f, k) * ghostScale;
                }
            }
            if (ph == Ph.Practice && fingerT && cur != null)
            {
                fingerT.gameObject.SetActive(true);
                float loop = (ghostT % 1.2f) / 1.2f;
                int a = loop < 0.4f ? 1 : loop < 0.7f ? 2 : 4;
                var p = RungMid(cur, a);
                fingerT.position = W(p, 0.55f + 0.08f * Mathf.Sin(ghostT * 8f));
                if (cam) fingerT.LookAt(cam.transform.position);
            }
            if (hopHint)
            {
                bool keep = ph == Ph.Practice || (ph == Ph.Play && playSheetN <= 2);
                bool onCell = cur != null && cur.ad != cur.ad0 && cur.ad != cur.adHi;
                bool show = keep && cur != null && blockT && (ph == Ph.Practice || onCell || hopAnim > 0);
                hopHint.gameObject.SetActive(show);
                if (show)
                {
                    hopHint.text = ph == Ph.Practice
                        ? (onCell && cur.ad == cur.target ? "이 칸을 눌러 착지" : "4 cm 칸을 눌러")
                        : (onCell ? "쐐기 탭 = 착지" : "칸 탭 = hop");
                    hopHint.transform.position = blockT.position + new Vector3(0, 0.95f, 0);
                    if (cam) hopHint.transform.rotation = Quaternion.LookRotation(hopHint.transform.position - cam.transform.position, Vector3.up);
                }
            }
            if (ph != Ph.Title) RefreshHud();
            if (logoUi)
            {
                float k = 1f + 0.015f * Mathf.Sin(titleT * 0.9f);
                logoUi.rectTransform.localScale = new Vector3(k, k, 1f);
            }
        }
    }
}
