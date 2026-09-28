// 유리칼 — 화면 UI. 디자인 시스템: 「먹선 인쇄 + 유백 아크릴 카드」 — 형광 매트 위에 인쇄된 듯한 먹색 글자,
// 카드는 유백 면 + 2 px 먹 테두리(스티커 그림자·광택 없음), 강조는 주황(유리칼 손잡이)·마젠타(자투리) 두 색만.
// 세로(390×844): 위 = HUD + 작업 티켓 띠, 아래 = 예비 랙 띠. 가로(1280×800): 왼쪽 티켓 · 오른쪽 랙 · 가운데 매트 전폭.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.YuriKal
{
    public partial class YuriKalGame
    {
        Canvas canvas;
        RectTransform canvasRt, hudRt, ticketRt, rackRt, toastRt, cardRt, titleRt, endRt, ctaRt, endCtaRt, timeBarFill, sheetBarFill;
        TextMeshProUGUI timeTxt, counterTxt, scoreTxt, comboTxt, ticketHead, orderTxt, dataTxt, hintTxt, footTxt, toastTxt, cardBig, cardSub;
        TextMeshProUGUI tagTxt, bestTxt, badgeTxt, ctaTxt, endHead, endStats, endBest, endCtaTxt, rackLbl;
        readonly TextMeshProUGUI[] logo = new TextMeshProUGUI[3];
        readonly RectTransform[] slab = new RectTransform[3];
        readonly Image[] slabCrack = new Image[3];
        readonly Image[] ripples = new Image[4];
        readonly float[] rippleT = { 9, 9, 9, 9 };
        int rippleNext;
        Image toastBg, hintBg;
        RectTransform rail;
        CanvasGroup toastG, cardG, titleG, endG, hudG, ticketG, rackG;
        Sprite roundSpr, ringSpr;
        bool land, titleLand;
        Vector2 lastScreen;
        float insetTop, insetBottom, insetLeft, insetRight, ticketTextW = 300;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }
        Image Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color c, Sprite s = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = s; im.color = c; im.raycastTarget = false;
            if (s && s.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }
        TextMeshProUGUI Txt(Transform parent, string s, Vector2 anchor, Vector2 pos, float size, Color c, float width = 360, TextAlignmentOptions al = TextAlignmentOptions.Center)
        {
            var rt = R("T", parent, anchor, pos, new Vector2(width, size * 1.5f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = c; t.alignment = al; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; t.text = s;
            return t;
        }
        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero; }

        /// <summary>유백 카드 = 먹 테두리 판 + 안쪽 유백 판(그림자·광택 없음).</summary>
        RectTransform Card(string name, Transform parent, Color face)
        {
            var edge = Img(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100), InkC, roundSpr);
            var f = Img("Face", edge.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, face, roundSpr);
            var frt = f.rectTransform; frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = new Vector2(2.5f, 2.5f); frt.offsetMax = new Vector2(-2.5f, -2.5f);
            return edge.rectTransform;
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
                        float r = S / 2f - 2, w = S * 0.05f;
                        a = Mathf.Clamp01(Mathf.Clamp01(1 - Mathf.Abs(d - (r - w)) / w) * 2.2f);
                    }
                    else
                    {
                        float qx = Mathf.Max(Mathf.Abs(x + 0.5f - S / 2f) - (S / 2f - rad), 0), qy = Mathf.Max(Mathf.Abs(y + 0.5f - S / 2f) - (S / 2f - rad), 0);
                        a = Mathf.Clamp01(rad - Mathf.Sqrt(qx * qx + qy * qy) + 0.5f);
                    }
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply(false, true);
            float bd = ring ? 0 : rad + 2;
            return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(bd, bd, bd, bd));
        }

        void BuildUi()
        {
            canvas = MgfText.Canvas;
            canvasRt = (RectTransform)canvas.transform;
            roundSpr = RoundSprite(48, 8, false);
            ringSpr = RoundSprite(128, 0, true);
            var ct = canvas.transform;

            // ── HUD (시간 · 주문 번호 · 점수)
            hudRt = R("Hud", ct, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0, 44)); hudRt.anchorMin = new Vector2(0, 1); hudRt.anchorMax = new Vector2(1, 1); hudRt.pivot = new Vector2(0.5f, 1f);
            hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            var tb = Img("TimeBar", hudRt, new Vector2(0.5f, 1f), new Vector2(0, -3), new Vector2(0, 5), C(InkC, 0.25f));
            tb.rectTransform.anchorMin = new Vector2(0, 1); tb.rectTransform.anchorMax = new Vector2(1, 1); tb.rectTransform.sizeDelta = new Vector2(-20, 5);
            var tf = Img("Fill", tb.transform, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, InkC);
            timeBarFill = tf.rectTransform; timeBarFill.anchorMin = Vector2.zero; timeBarFill.anchorMax = Vector2.one; timeBarFill.pivot = new Vector2(0, 0.5f); timeBarFill.sizeDelta = Vector2.zero;
            timeTxt = Txt(hudRt, "90", new Vector2(0f, 1f), new Vector2(52, -26), 30, InkC, 90, TextAlignmentOptions.Left);
            timeTxt.fontStyle = FontStyles.Bold;
            counterTxt = Txt(hudRt, "", new Vector2(0.5f, 1f), new Vector2(0, -26), 17, InkC, 160);
            scoreTxt = Txt(hudRt, "0", new Vector2(1f, 1f), new Vector2(-122, -22), 22, InkC, 110, TextAlignmentOptions.Right);
            scoreTxt.fontStyle = FontStyles.Bold;
            comboTxt = Txt(hudRt, "", new Vector2(1f, 1f), new Vector2(-122, -42), 13, Mag, 110, TextAlignmentOptions.Right);

            // ── 작업 티켓
            ticketRt = Card("Ticket", ct, Milk);
            ticketG = ticketRt.gameObject.AddComponent<CanvasGroup>();
            var clip = Img("Clip", ticketRt, new Vector2(0.5f, 1f), new Vector2(0, 2), new Vector2(54, 12), Orange, roundSpr);
            clip.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ticketHead = Txt(ticketRt, "", new Vector2(0f, 1f), new Vector2(0, -16), 13, C(InkC, 0.75f), 300, TextAlignmentOptions.Left);
            orderTxt = Txt(ticketRt, "", new Vector2(0f, 1f), new Vector2(0, -44), 19, InkC, 300, TextAlignmentOptions.TopLeft);
            orderTxt.fontStyle = FontStyles.Bold;
            dataTxt = Txt(ticketRt, "", new Vector2(0f, 1f), new Vector2(0, -80), 15, InkC, 300, TextAlignmentOptions.TopLeft);
            hintBg = Img("HintBg", ticketRt, new Vector2(0f, 1f), Vector2.zero, new Vector2(100, 24), C(Orange, 0.14f), roundSpr);
            hintTxt = Txt(ticketRt, "", new Vector2(0f, 1f), new Vector2(0, -104), 14, Hex("B8480A"), 300, TextAlignmentOptions.TopLeft);
            hintTxt.fontStyle = FontStyles.Bold;
            footTxt = Txt(ticketRt, "", new Vector2(0f, 0f), new Vector2(0, 14), 11, C(InkC, 0.65f), 300, TextAlignmentOptions.Left);
            var sb = Img("SheetBar", ticketRt, new Vector2(0.5f, 0f), new Vector2(0, 4), new Vector2(0, 3), C(InkC, 0.15f));
            sb.rectTransform.anchorMin = new Vector2(0, 0); sb.rectTransform.anchorMax = new Vector2(1, 0); sb.rectTransform.sizeDelta = new Vector2(-16, 3);
            var sf = Img("Fill", sb.transform, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, Orange);
            sheetBarFill = sf.rectTransform; sheetBarFill.anchorMin = Vector2.zero; sheetBarFill.anchorMax = Vector2.one; sheetBarFill.pivot = new Vector2(0, 0.5f); sheetBarFill.sizeDelta = Vector2.zero;

            // ── 예비 아크릴 랙
            rackRt = R("Rack", ct, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(200, 70));
            rackG = rackRt.gameObject.AddComponent<CanvasGroup>();
            rail = Img("Rail", rackRt, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(170, 6), Hex("5E666E"), roundSpr).rectTransform;
            for (int i = 0; i < 3; i++)
            {
                var e = Card("Slab" + i, rackRt, new Color(0.96f, 0.97f, 0.93f, 0.95f));
                e.sizeDelta = new Vector2(34, 46);
                e.pivot = new Vector2(0.5f, 0f);
                slab[i] = e;
                slabCrack[i] = Img("Crack", e, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 40), Color.white, CrackSprite());
                slabCrack[i].enabled = false;
            }
            rackLbl = Txt(rackRt, "예비 아크릴", new Vector2(0.5f, 0.5f), Vector2.zero, 12, C(InkC, 0.8f), 120);

            // ── 알림 띠(드러난 수학 · 거절 사유)
            toastBg = Img("Toast", ct, new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(360, 58), C(InkC, 0.93f), roundSpr);
            toastRt = toastBg.rectTransform;
            toastG = toastRt.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0;
            toastTxt = Txt(toastRt, "", new Vector2(0.5f, 0.5f), Vector2.zero, 15, Milk, 344);
            toastTxt.rectTransform.sizeDelta = new Vector2(344, 54); toastTxt.alignment = TextAlignmentOptions.Center;
            toastTxt.enableAutoSizing = true; toastTxt.fontSizeMin = 10; toastTxt.fontSizeMax = 15;

            // ── 카드(연습 끝 · 다리 단계)
            cardRt = Card("InfoCard", ct, Milk);
            cardRt.sizeDelta = new Vector2(320, 150);
            cardG = cardRt.gameObject.AddComponent<CanvasGroup>(); cardG.alpha = 0;
            cardBig = Txt(cardRt, "", new Vector2(0.5f, 0.5f), new Vector2(0, 26), 24, InkC, 300); cardBig.fontStyle = FontStyles.Bold;
            cardSub = Txt(cardRt, "", new Vector2(0.5f, 0.5f), new Vector2(0, -24), 15, C(InkC, 0.85f), 290);
            cardSub.rectTransform.sizeDelta = new Vector2(290, 60);

            // ── 타이틀
            titleRt = R("Title", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Stretch(titleRt);
            titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            string[] g = { "유", "리", "칼" };
            for (int i = 0; i < 3; i++)
            {
                logo[i] = Txt(titleRt, g[i], new Vector2(0.5f, 1f), new Vector2((i - 1) * 88, -120), 96, i == 2 ? Orange : InkC, 110);
                logo[i].fontStyle = FontStyles.Bold;
                logo[i].outlineWidth = 0.12f; logo[i].outlineColor = C32(Milk);
                logo[i].rectTransform.localScale = Vector3.zero;
            }
            tagTxt = Txt(titleRt, "교점을 밀어 쪼개라", new Vector2(0.5f, 1f), new Vector2(0, -196), 19, InkC, 320);
            tagTxt.characterSpacing = 6;
            var cta = Card("Cta", titleRt, Milk);
            ctaRt = cta; ctaRt.anchorMin = ctaRt.anchorMax = new Vector2(0.5f, 0f); ctaRt.anchoredPosition = new Vector2(0, 128); ctaRt.sizeDelta = new Vector2(230, 66);
            Img("CtaTab", ctaRt, new Vector2(0f, 0.5f), new Vector2(18, 0), new Vector2(10, 40), Orange, roundSpr);
            ctaTxt = Txt(ctaRt, "시작하기", new Vector2(0.5f, 0.5f), new Vector2(6, 0), 28, InkC, 200); ctaTxt.fontStyle = FontStyles.Bold;
            bestTxt = Txt(titleRt, "", new Vector2(0.5f, 0f), new Vector2(0, 78), 14, C(InkC, 0.85f), 360);
            badgeTxt = Txt(titleRt, "중학교 2학년 · 사각형의 성질", new Vector2(0.5f, 0f), new Vector2(0, 50), 14, Milk, 300);
            var bb = Img("BadgeBg", titleRt, new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(236, 28), InkC, roundSpr);
            bb.transform.SetSiblingIndex(badgeTxt.transform.GetSiblingIndex());

            // ── 끝 화면
            endRt = Card("End", ct, Milk);
            endRt.sizeDelta = new Vector2(330, 360);
            endG = endRt.gameObject.AddComponent<CanvasGroup>(); endG.alpha = 0;
            endHead = Txt(endRt, "", new Vector2(0.5f, 1f), new Vector2(0, -42), 30, InkC, 300); endHead.fontStyle = FontStyles.Bold;
            endStats = Txt(endRt, "", new Vector2(0.5f, 1f), new Vector2(0, -150), 17, InkC, 280, TextAlignmentOptions.Top);
            endStats.rectTransform.sizeDelta = new Vector2(280, 150); endStats.lineSpacing = 12;
            endBest = Txt(endRt, "", new Vector2(0.5f, 0f), new Vector2(0, 100), 13, C(InkC, 0.75f), 290);
            endCtaRt = Card("EndCta", endRt, Milk);
            endCtaRt.anchorMin = endCtaRt.anchorMax = new Vector2(0.5f, 0f); endCtaRt.anchoredPosition = new Vector2(0, 50); endCtaRt.sizeDelta = new Vector2(200, 56);
            Img("Tab", endCtaRt, new Vector2(0f, 0.5f), new Vector2(16, 0), new Vector2(9, 34), Orange, roundSpr);
            endCtaTxt = Txt(endCtaRt, "다시 하기", new Vector2(0.5f, 0.5f), new Vector2(6, 0), 23, InkC, 180); endCtaTxt.fontStyle = FontStyles.Bold;

            for (int i = 0; i < ripples.Length; i++)
            {
                ripples[i] = Img("Ripple", ct, new Vector2(0, 0), Vector2.zero, new Vector2(60, 60), C(InkC, 0.7f), ringSpr);
                ripples[i].enabled = false;
            }
            Layout(true);
        }

        static Color C(Color c, float a) { c.a = a; return c; }

        static Sprite crackSpr;
        static Sprite CrackSprite()
        {
            if (crackSpr) return crackSpr;
            const int W = 60, H = 80;
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            var r = new System.Random(9);
            void Line(float x0, float y0, float x1, float y1)
            {
                int n = (int)(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) * 2) + 1;
                for (int k = 0; k <= n; k++)
                {
                    int x = (int)Mathf.Lerp(x0, x1, (float)k / n), y = (int)Mathf.Lerp(y0, y1, (float)k / n);
                    for (int oy = -1; oy <= 1; oy++) for (int ox = -1; ox <= 1; ox++)
                        { int xx = x + ox, yy = y + oy; if (xx >= 0 && xx < W && yy >= 0 && yy < H) px[yy * W + xx] = new Color32(43, 47, 54, 230); }
                }
            }
            float cx = 26, cy = 44;
            for (int k = 0; k < 7; k++)
            {
                float a = k * 0.9f + (float)r.NextDouble() * 0.4f, L = 18 + (float)r.NextDouble() * 22;
                float mx = cx + Mathf.Cos(a) * L * 0.5f + (float)(r.NextDouble() - 0.5) * 6, my = cy + Mathf.Sin(a) * L * 0.5f;
                Line(cx, cy, mx, my); Line(mx, my, cx + Mathf.Cos(a + 0.2f) * L, cy + Mathf.Sin(a + 0.2f) * L);
            }
            t.SetPixels32(px); t.Apply(false, true);
            crackSpr = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f));
            return crackSpr;
        }

        /// <summary>세로/가로 판형 배치. 화면 크기가 바뀔 때만.</summary>
        void Layout(bool force = false)
        {
            var scr = canvasRt.rect.size;
            if (!force && scr == lastScreen) return;
            lastScreen = scr;
            float W = canvasRt.rect.width, H = canvasRt.rect.height;
            land = W > H * 1.05f;
            if (!land)
            {
                float tw = Mathf.Min(W - 20, 520), th = 150;
                ticketRt.anchorMin = ticketRt.anchorMax = new Vector2(0.5f, 1f); ticketRt.pivot = new Vector2(0.5f, 1f);
                ticketRt.anchoredPosition = new Vector2(0, -50); ticketRt.sizeDelta = new Vector2(tw, th);
                LayoutTicket(tw, th);
                rackRt.anchorMin = rackRt.anchorMax = new Vector2(0.5f, 0f); rackRt.pivot = new Vector2(0.5f, 0f);
                rackRt.anchoredPosition = new Vector2(0, 8); rackRt.sizeDelta = new Vector2(W - 20, 70);
                for (int i = 0; i < 3; i++) slab[i].anchoredPosition = new Vector2(10 + i * 40, -18);
                rackLbl.rectTransform.anchoredPosition = new Vector2(-90, -8);
                rackLbl.alignment = TextAlignmentOptions.Right;
                rail.anchoredPosition = new Vector2(50, -20); rail.sizeDelta = new Vector2(140, 6);
                insetTop = 50 + th + 6; insetBottom = 80; insetLeft = 6; insetRight = 6;
                toastRt.anchorMin = toastRt.anchorMax = new Vector2(0.5f, 0);
                toastRt.anchoredPosition = new Vector2(0, 92); toastRt.sizeDelta = new Vector2(Mathf.Min(W - 20, 420), 58);
                toastTxt.rectTransform.sizeDelta = new Vector2(Mathf.Min(W - 20, 420) - 16, 54);
            }
            else
            {
                float tw = Mathf.Clamp(W * 0.27f, 190, 280), th = Mathf.Min(H - 60, 250);
                ticketRt.anchorMin = ticketRt.anchorMax = new Vector2(0f, 1f); ticketRt.pivot = new Vector2(0f, 1f);
                ticketRt.anchoredPosition = new Vector2(10, -52); ticketRt.sizeDelta = new Vector2(tw, th);
                LayoutTicket(tw, th);
                float rw = Mathf.Clamp(W * 0.14f, 104, 160);
                rackRt.anchorMin = rackRt.anchorMax = new Vector2(1f, 1f); rackRt.pivot = new Vector2(1f, 1f);
                rackRt.anchoredPosition = new Vector2(-10, -52); rackRt.sizeDelta = new Vector2(rw, 150);
                for (int i = 0; i < 3; i++) slab[i].anchoredPosition = new Vector2((i - 1) * 32, -10);
                rackLbl.rectTransform.anchoredPosition = new Vector2(0, 50);
                rackLbl.alignment = TextAlignmentOptions.Center;
                rail.anchoredPosition = new Vector2(0, -12); rail.sizeDelta = new Vector2(rw - 10, 6);
                insetTop = 46; insetBottom = 4; insetLeft = tw + 16; insetRight = rw + 16;
                // 알림 띠: 왼쪽 티켓 아래(매트·고철 통을 가리지 않게)
                toastRt.anchorMin = toastRt.anchorMax = new Vector2(0, 1);
                toastRt.anchoredPosition = new Vector2(10 + tw * 0.5f, -52 - th - 44); toastRt.sizeDelta = new Vector2(tw, 76);
                toastTxt.rectTransform.sizeDelta = new Vector2(tw - 14, 70);
            }
            // 타이틀: 세로 = 위 로고 · 가운데 데모 · 아래 시작 / 가로 = 왼쪽 포스터(로고·시작) · 오른쪽 데모 매트
            var bbg = titleRt.Find("BadgeBg").GetComponent<RectTransform>();
            titleLand = W > H * 0.9f;   // 정사각(1080×1080)·가로는 포스터형
            if (!titleLand)
            {
                for (int i = 0; i < 3; i++) { logo[i].fontSize = 96; SetA(logo[i].rectTransform, new Vector2(0.5f, 1f), new Vector2((i - 1) * 88, -120)); }
                SetA(tagTxt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -196));
                SetA(ctaRt, new Vector2(0.5f, 0f), new Vector2(0, 128));
                SetA(bestTxt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 78));
                SetA(badgeTxt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 50)); SetA(bbg, new Vector2(0.5f, 0f), new Vector2(0, 50));
            }
            else
            {
                float lx = W * 0.26f;
                for (int i = 0; i < 3; i++) { logo[i].fontSize = 92; SetA(logo[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(lx + (i - 1) * 84, 118)); }
                SetA(tagTxt.rectTransform, new Vector2(0f, 0.5f), new Vector2(lx, 44));
                SetA(ctaRt, new Vector2(0f, 0.5f), new Vector2(lx, -40));
                SetA(bestTxt.rectTransform, new Vector2(0f, 0.5f), new Vector2(lx, -100));
                SetA(badgeTxt.rectTransform, new Vector2(0f, 0.5f), new Vector2(lx, -136)); SetA(bbg, new Vector2(0f, 0.5f), new Vector2(lx, -136));
            }
            endRt.sizeDelta = new Vector2(330, Mathf.Min(360, H - 30));
            ReflowTicket();
        }

        static void SetA(RectTransform rt, Vector2 anchor, Vector2 pos) { rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = pos; }

        /// <summary>한국어 낱말 단위 줄바꿈(TMP 는 한글을 글자마다 끊는다 — 「평행사/변형」 방지). 폭은 글자 폭 추정(한글 1em, 기타 0.56em).</summary>
        static string Wrap(string s, float widthPx, float fontPx)
        {
            if (string.IsNullOrEmpty(s)) return s;
            float max = widthPx / fontPx;
            var sb = new System.Text.StringBuilder();
            foreach (var para in s.Split('\n'))
            {
                if (sb.Length > 0) sb.Append('\n');
                float line = 0;
                foreach (var w in para.Split(' '))
                {
                    float ww = 0; foreach (char c in w) ww += c >= 0x1100 ? 1f : 0.58f;
                    if (line > 0 && line + 0.3f + ww > max) { sb.Append('\n'); line = 0; }
                    else if (line > 0) { sb.Append(' '); line += 0.3f; }
                    sb.Append(w); line += ww;
                }
            }
            return sb.ToString();
        }

        void LayoutTicket(float tw, float th)
        {
            float x = 14, w = tw - 28;
            void Put(TextMeshProUGUI t, float y, float h) { var rt = t.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h); }
            bool tall = th > 180;
            Put(ticketHead, -10, 18);
            ticketTextW = w;
            Put(orderTxt, -30, tall ? 84 : 48);
            orderTxt.fontSize = tall ? 20 : 18;
            Put(dataTxt, tall ? -118 : -80, tall ? 44 : 22);
            dataTxt.fontSize = 15;
            Put(hintTxt, tall ? -166 : -104, tall ? 44 : 22);
            hintTxt.enableAutoSizing = true; hintTxt.fontSizeMax = 14; hintTxt.fontSizeMin = 10;
            orderTxt.enableAutoSizing = true; orderTxt.fontSizeMin = 13; orderTxt.fontSizeMax = tall ? 20 : 18;
            dataTxt.enableAutoSizing = true; dataTxt.fontSizeMin = 10; dataTxt.fontSizeMax = 15;
            var hb = hintBg.rectTransform; hb.anchorMin = hb.anchorMax = new Vector2(0, 1); hb.pivot = new Vector2(0, 1);
            hb.anchoredPosition = new Vector2(x - 6, (tall ? -166 : -104) + 3); hb.sizeDelta = new Vector2(w + 12, tall ? 48 : 24);
            var fr = footTxt.rectTransform; fr.anchorMin = fr.anchorMax = new Vector2(0, 0); fr.pivot = new Vector2(0, 0); fr.anchoredPosition = new Vector2(x, 10); fr.sizeDelta = new Vector2(w, 16);
            footTxt.enableAutoSizing = true; footTxt.fontSizeMax = 11; footTxt.fontSizeMin = 8; footTxt.textWrappingMode = TextWrappingModes.NoWrap;
        }

        void Ripple(Vector2 screen, Color c)
        {
            var im = ripples[rippleNext]; rippleT[rippleNext] = 0; rippleNext = (rippleNext + 1) % ripples.Length;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var lp);
            var rt = im.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.anchoredPosition = lp;
            im.color = c; im.enabled = true;
        }

        void UpdateRipples(float dt)
        {
            for (int i = 0; i < ripples.Length; i++)
            {
                if (!ripples[i].enabled) continue;
                rippleT[i] += dt;
                float k = rippleT[i] / 0.45f;
                if (k >= 1) { ripples[i].enabled = false; continue; }
                ripples[i].rectTransform.localScale = Vector3.one * (0.4f + k * 1.2f);
                var c = ripples[i].color; c.a = (1 - k) * 0.8f; ripples[i].color = c;
            }
        }

        bool Hit(RectTransform rt, Vector2 screen) => rt && rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, screen, null);
    }
}
