// 배율 밀어 — 화면 UI. 디자인 시스템: 「활판 교정 전표」 — 미색 면지 카드에 먹 겹줄 테두리(바깥 굵은 줄 + 안쪽 가는 줄),
// 강조는 주홍 교정 잉크 한 색. 광택·스티커 그림자 없음. 글자는 먹색, 수치는 굵게.
// 세로(390×844): 위 = HUD + 주문 전표 + 현재 비, 아래 = 예비 인화지 랙. 가로(1280×800): 왼쪽 열(전표·현재 비·랙) · 오른쪽 교정대.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.BaeyulMireo
{
    public partial class BaeyulGame
    {
        Canvas canvas;
        RectTransform canvasRt, hudRt, ticketRt, rackRt, toastRt, cardRt, titleRt, endRt, ctaRt, ctaFace, ctaShine, endCtaRt, timeBarFill, sheetBarFill, readRt;
        TextMeshProUGUI timeTxt, counterTxt, scoreTxt, comboTxt, ticketHead, orderTxt, dataTxt, hintTxt, footTxt, toastTxt, cardBig, cardSub, readTxt, readHead;
        TextMeshProUGUI tagTxt, bestTxt, badgeTxt, ctaTxt, endHead, endStats, endBest, endCtaTxt, rackLbl, stampNo;
        readonly TextMeshProUGUI[] logo = new TextMeshProUGUI[4], logoGhost = new TextMeshProUGUI[4];
        readonly RectTransform[] spare = new RectTransform[3];
        readonly Image[] spareCrumple = new Image[3];
        readonly Image[] ripples = new Image[5];
        readonly float[] rippleT = { 9, 9, 9, 9, 9 };
        int rippleNext;
        Image hintBg, logoInk;
        CanvasGroup toastG, cardG, endG, readG;
        Sprite roundSpr, ringSpr, blotSpr;
        bool land, titleLand;
        float ctaBase = 1f, titlePosterW = 420;
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
        static void Fill(RectTransform rt, float inset) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset); }

        /// <summary>교정 전표 = 먹 굵은 줄 · 면지 · 먹 가는 줄 · 면지(겹줄 테두리).</summary>
        RectTransform Slip(string name, Transform parent, Color face)
        {
            var edge = Img(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100), InkC, roundSpr);
            var f = Img("Face", edge.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, face, roundSpr); Fill(f.rectTransform, 2f);
            var hl = Img("Rule", edge.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, C(InkC, 0.55f), roundSpr); Fill(hl.rectTransform, 5f);
            var f2 = Img("Face2", edge.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, face, roundSpr); Fill(f2.rectTransform, 6f);
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

        /// <summary>잉크 얼룩(로고 뒤 번짐) — 불규칙한 가장자리의 부드러운 덩어리.</summary>
        static Sprite BlotSprite()
        {
            const int S = 256;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Blot", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            var rnd = new System.Random(21);
            var k = new float[9]; var ph = new float[9];
            for (int i = 0; i < 9; i++) { k[i] = (float)rnd.NextDouble() * 0.06f; ph[i] = (float)rnd.NextDouble() * 6.28f; }
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / S * 2 - 1, dy = (y + 0.5f) / S * 2 - 1;
                    float ang = Mathf.Atan2(dy, dx), d = Mathf.Sqrt(dx * dx * 0.55f + dy * dy * 1.9f);
                    float rr = 0.78f;
                    for (int i = 0; i < 9; i++) rr += k[i] * Mathf.Sin(ang * (i + 2) + ph[i]);
                    float a = Mathf.Clamp01((rr - d) * 18f);
                    float speck = rnd.NextDouble() < 0.004 && d < rr + 0.15f ? 0.8f : 0f;
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(Mathf.Max(a * (0.85f + 0.15f * (float)rnd.NextDouble()), speck) * 255));
                }
            t.SetPixels32(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
        }

        static Sprite crumpleSpr;
        static Sprite CrumpleSprite()
        {
            if (crumpleSpr) return crumpleSpr;
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
                    for (int oy = 0; oy <= 1; oy++) for (int ox = 0; ox <= 1; ox++)
                        { int xx = x + ox, yy = y + oy; if (xx >= 0 && xx < W && yy >= 0 && yy < H) px[yy * W + xx] = new Color32(63, 58, 54, 200); }
                }
            }
            // 구김 주름: 가로지르는 꺾은선 몇 줄
            for (int k = 0; k < 6; k++)
            {
                float y = 8 + k * 12 + (float)r.NextDouble() * 6, x = 0;
                float py = y;
                while (x < W) { float nx = x + 8 + (float)r.NextDouble() * 10, ny = y + ((float)r.NextDouble() - 0.5f) * 16; Line(x, py, nx, ny); x = nx; py = ny; }
            }
            t.SetPixels32(px); t.Apply(false, true);
            crumpleSpr = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f));
            return crumpleSpr;
        }

        void BuildUi()
        {
            canvas = MgfText.Canvas;
            canvasRt = (RectTransform)canvas.transform;
            roundSpr = RoundSprite(48, 5, false);
            ringSpr = RoundSprite(128, 0, true);
            blotSpr = BlotSprite();
            var ct = canvas.transform;

            // ── HUD (시간 · 주문 번호 · 점수)
            hudRt = R("Hud", ct, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0, 46)); hudRt.anchorMin = new Vector2(0, 1); hudRt.anchorMax = new Vector2(1, 1); hudRt.pivot = new Vector2(0.5f, 1f);
            var tb = Img("TimeBar", hudRt, new Vector2(0.5f, 1f), new Vector2(0, -3), new Vector2(0, 4), C(InkC, 0.18f));
            tb.rectTransform.anchorMin = new Vector2(0, 1); tb.rectTransform.anchorMax = new Vector2(1, 1); tb.rectTransform.sizeDelta = new Vector2(-20, 4);
            var tf = Img("Fill", tb.transform, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, InkC);
            timeBarFill = tf.rectTransform; timeBarFill.anchorMin = Vector2.zero; timeBarFill.anchorMax = Vector2.one; timeBarFill.pivot = new Vector2(0, 0.5f); timeBarFill.sizeDelta = Vector2.zero;
            timeTxt = Txt(hudRt, "90", new Vector2(0f, 1f), new Vector2(56, -27), 30, InkC, 96, TextAlignmentOptions.Left);
            timeTxt.fontStyle = FontStyles.Bold;
            counterTxt = Txt(hudRt, "", new Vector2(0.5f, 1f), new Vector2(0, -27), 16, InkC, 160);
            scoreTxt = Txt(hudRt, "0", new Vector2(1f, 1f), new Vector2(-122, -22), 22, InkC, 110, TextAlignmentOptions.Right);
            scoreTxt.fontStyle = FontStyles.Bold;
            comboTxt = Txt(hudRt, "", new Vector2(1f, 1f), new Vector2(-122, -42), 13, Verm, 110, TextAlignmentOptions.Right);

            // ── 주문 전표
            ticketRt = Slip("Ticket", ct, Card);
            var stamp = Img("StampBox", ticketRt, new Vector2(1f, 1f), new Vector2(-34, -30), new Vector2(46, 30), C(Verm, 0.0f), roundSpr);
            var sb2 = Img("StampRule", stamp.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Verm, roundSpr); Fill(sb2.rectTransform, 0);
            var sb3 = Img("StampFace", stamp.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Card, roundSpr); Fill(sb3.rectTransform, 2);
            stampNo = Txt(stamp.transform, "01", new Vector2(0.5f, 0.5f), Vector2.zero, 17, Verm, 46); stampNo.fontStyle = FontStyles.Bold;
            stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, 6);
            ticketHead = Txt(ticketRt, "", new Vector2(0f, 1f), new Vector2(0, -16), 12, C(InkC, 0.72f), 300, TextAlignmentOptions.Left);
            orderTxt = Txt(ticketRt, "", new Vector2(0f, 1f), new Vector2(0, -44), 17, InkC, 300, TextAlignmentOptions.TopLeft);
            orderTxt.fontStyle = FontStyles.Bold;
            dataTxt = Txt(ticketRt, "", new Vector2(0f, 1f), new Vector2(0, -80), 14, InkC, 300, TextAlignmentOptions.TopLeft);
            hintBg = Img("HintBg", ticketRt, new Vector2(0f, 1f), Vector2.zero, new Vector2(100, 24), C(Verm, 0.12f), roundSpr);
            hintTxt = Txt(ticketRt, "", new Vector2(0f, 1f), new Vector2(0, -104), 14, Hex("8E1414"), 300, TextAlignmentOptions.TopLeft);
            hintTxt.fontStyle = FontStyles.Bold;
            footTxt = Txt(ticketRt, "", new Vector2(0f, 0f), new Vector2(0, 14), 11, C(InkC, 0.65f), 300, TextAlignmentOptions.Left);
            var sbar = Img("SheetBar", ticketRt, new Vector2(0.5f, 0f), new Vector2(0, 7), new Vector2(0, 3), C(InkC, 0.14f));
            sbar.rectTransform.anchorMin = new Vector2(0, 0); sbar.rectTransform.anchorMax = new Vector2(1, 0); sbar.rectTransform.sizeDelta = new Vector2(-18, 3);
            var sf = Img("Fill", sbar.transform, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, Verm);
            sheetBarFill = sf.rectTransform; sheetBarFill.anchorMin = Vector2.zero; sheetBarFill.anchorMax = Vector2.one; sheetBarFill.pivot = new Vector2(0, 0.5f); sheetBarFill.sizeDelta = Vector2.zero;

            // ── 현재 비(1단계만) — 놋쇠 띠
            var rb = Img("Readout", ct, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(300, 34), Hex("E9D8AE"), roundSpr);
            readRt = rb.rectTransform;
            var rbe = Img("Edge", readRt, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, Brass); rbe.rectTransform.anchorMin = new Vector2(0, 0); rbe.rectTransform.anchorMax = new Vector2(1, 0); rbe.rectTransform.sizeDelta = new Vector2(-8, 3); rbe.rectTransform.anchoredPosition = new Vector2(0, 1.5f);
            readG = readRt.gameObject.AddComponent<CanvasGroup>();
            readHead = Txt(readRt, "현재 비", new Vector2(0f, 0.5f), new Vector2(34, 0), 11, Hex("7A5A1E"), 60);
            readTxt = Txt(readRt, "", new Vector2(0f, 0.5f), new Vector2(0, 0), 15, InkC, 250, TextAlignmentOptions.Left);
            readTxt.fontStyle = FontStyles.Bold;
            readTxt.textWrappingMode = TextWrappingModes.NoWrap; readTxt.enableAutoSizing = true; readTxt.fontSizeMin = 10; readTxt.fontSizeMax = 15;

            // ── 예비 인화지 랙
            rackRt = R("Rack", ct, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(200, 64));
            for (int i = 0; i < 3; i++)
            {
                var e = Slip("Spare" + i, rackRt, Paper);
                e.sizeDelta = new Vector2(30, 40);
                e.pivot = new Vector2(0.5f, 0f);
                spare[i] = e;
                spareCrumple[i] = Img("Crumple", e, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 38), Color.white, CrumpleSprite());
                spareCrumple[i].enabled = false;
            }
            rackLbl = Txt(rackRt, "예비 인화지", new Vector2(0.5f, 0.5f), Vector2.zero, 12, C(InkC, 0.8f), 120);

            // ── 알림 띠(드러난 수학 · 거절 사유)
            var tbg = Img("Toast", ct, new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(360, 62), C(InkC, 0.95f), roundSpr);
            toastRt = tbg.rectTransform;
            var tv = Img("Verm", toastRt, new Vector2(0f, 0.5f), new Vector2(4, 0), new Vector2(5, 40), Verm, roundSpr);
            tv.rectTransform.anchorMin = new Vector2(0, 0); tv.rectTransform.anchorMax = new Vector2(0, 1); tv.rectTransform.sizeDelta = new Vector2(5, -12);
            toastG = toastRt.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0;
            toastTxt = Txt(toastRt, "", new Vector2(0.5f, 0.5f), Vector2.zero, 15, Paper, 344);
            toastTxt.rectTransform.sizeDelta = new Vector2(344, 58); toastTxt.alignment = TextAlignmentOptions.Center;
            toastTxt.enableAutoSizing = true; toastTxt.fontSizeMin = 9; toastTxt.fontSizeMax = 15;

            // ── 안내 카드
            cardRt = Slip("InfoCard", ct, Card);
            cardRt.sizeDelta = new Vector2(320, 150);
            cardG = cardRt.gameObject.AddComponent<CanvasGroup>(); cardG.alpha = 0;
            cardBig = Txt(cardRt, "", new Vector2(0.5f, 0.5f), new Vector2(0, 28), 22, InkC, 290); cardBig.fontStyle = FontStyles.Bold;
            cardSub = Txt(cardRt, "", new Vector2(0.5f, 0.5f), new Vector2(0, -22), 14, C(InkC, 0.85f), 290);
            cardSub.rectTransform.sizeDelta = new Vector2(290, 64);

            // ── 타이틀
            titleRt = R("Title", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Fill(titleRt, 0);
            logoInk = Img("LogoInk", titleRt, new Vector2(0.5f, 1f), new Vector2(0, -122), new Vector2(330, 150), C(Verm, 0.16f), blotSpr);
            string[] g = { "배", "율", "밀", "어" };
            for (int i = 0; i < 4; i++)
            {
                logoGhost[i] = Txt(titleRt, g[i], new Vector2(0.5f, 1f), Vector2.zero, 84, C(Verm, i < 2 ? 0.0f : 0.5f), 100);
                logoGhost[i].fontStyle = FontStyles.Bold;
                logo[i] = Txt(titleRt, g[i], new Vector2(0.5f, 1f), Vector2.zero, 84, i < 2 ? InkC : Verm, 100);
                logo[i].fontStyle = FontStyles.Bold;
                logo[i].outlineWidth = 0.14f; logo[i].outlineColor = C32(Paper);
                logo[i].rectTransform.localScale = Vector3.zero; logoGhost[i].rectTransform.localScale = Vector3.zero;
            }
            tagTxt = Txt(titleRt, "손잡이를 밀어 찍어라", new Vector2(0.5f, 1f), new Vector2(0, -210), 18, InkC, 320);
            tagTxt.characterSpacing = 5;
            // CTA: 주홍 잉크 패드(눌림 깊이 + 지나가는 광택)
            ctaRt = R("Cta", titleRt, new Vector2(0.5f, 0f), new Vector2(0, 130), new Vector2(232, 70));
            var depth = Img("Depth", ctaRt, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Hex("6E1010"), roundSpr); Fill(depth.rectTransform, 0);
            var faceImg = Img("Face", ctaRt, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Verm, roundSpr);
            ctaFace = faceImg.rectTransform; ctaFace.anchorMin = Vector2.zero; ctaFace.anchorMax = Vector2.one; ctaFace.offsetMin = new Vector2(0, 7); ctaFace.offsetMax = Vector2.zero;
            ctaFace.gameObject.AddComponent<RectMask2D>();
            var shine = Img("Shine", ctaFace, new Vector2(0f, 0.5f), new Vector2(-60, 0), new Vector2(26, 120), new Color(1f, 0.93f, 0.85f, 0.28f));
            ctaShine = shine.rectTransform; ctaShine.localRotation = Quaternion.Euler(0, 0, -22);
            ctaTxt = Txt(ctaFace, "찍어 보기", new Vector2(0.5f, 0.5f), new Vector2(0, 1), 27, Paper, 220); ctaTxt.fontStyle = FontStyles.Bold;
            bestTxt = Txt(titleRt, "", new Vector2(0.5f, 0f), new Vector2(0, 80), 14, C(InkC, 0.85f), 360);
            var bb = Img("BadgeBg", titleRt, new Vector2(0.5f, 0f), new Vector2(0, 50), new Vector2(236, 28), InkC, roundSpr);
            badgeTxt = Txt(titleRt, "중학교 2학년 · 도형의 닮음", new Vector2(0.5f, 0f), new Vector2(0, 50), 14, Paper, 300);

            // ── 끝 화면
            endRt = Slip("End", ct, Card);
            endRt.sizeDelta = new Vector2(330, 370);
            endG = endRt.gameObject.AddComponent<CanvasGroup>(); endG.alpha = 0;
            endHead = Txt(endRt, "", new Vector2(0.5f, 1f), new Vector2(0, -44), 30, InkC, 300); endHead.fontStyle = FontStyles.Bold;
            endStats = Txt(endRt, "", new Vector2(0.5f, 1f), new Vector2(0, -160), 16, InkC, 290, TextAlignmentOptions.Top);
            endStats.rectTransform.sizeDelta = new Vector2(290, 160); endStats.lineSpacing = 10;
            endBest = Txt(endRt, "", new Vector2(0.5f, 0f), new Vector2(0, 104), 13, C(InkC, 0.75f), 290);
            endCtaRt = R("EndCta", endRt, new Vector2(0.5f, 0f), new Vector2(0, 52), new Vector2(200, 58));
            var ed = Img("Depth", endCtaRt, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Hex("6E1010"), roundSpr); Fill(ed.rectTransform, 0);
            var ef = Img("Face", endCtaRt, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Verm, roundSpr);
            ef.rectTransform.anchorMin = Vector2.zero; ef.rectTransform.anchorMax = Vector2.one; ef.rectTransform.offsetMin = new Vector2(0, 6); ef.rectTransform.offsetMax = Vector2.zero;
            endCtaTxt = Txt(ef.transform, "다시 찍기", new Vector2(0.5f, 0.5f), new Vector2(0, 1), 23, Paper, 180); endCtaTxt.fontStyle = FontStyles.Bold;

            for (int i = 0; i < ripples.Length; i++)
            {
                ripples[i] = Img("Ripple", ct, new Vector2(0, 0), Vector2.zero, new Vector2(60, 60), C(InkC, 0.7f), ringSpr);
                ripples[i].enabled = false;
            }
            Layout(true);
        }

        static Color C(Color c, float a) { c.a = a; return c; }

        /// <summary>세로/가로 판형 배치. 화면 크기가 바뀔 때만.</summary>
        void Layout(bool force = false)
        {
            var scr = canvasRt.rect.size;
            if (!force && scr == lastScreen) return;
            lastScreen = scr;
            float W = canvasRt.rect.width, H = canvasRt.rect.height;
            bool wasLand = land;
            land = (W >= H && W >= 640) || W >= 1024;
            if (!land)
            {
                float tw = Mathf.Min(W - 16, 520), th = 138;
                ticketRt.anchorMin = ticketRt.anchorMax = new Vector2(0.5f, 1f); ticketRt.pivot = new Vector2(0.5f, 1f);
                ticketRt.anchoredPosition = new Vector2(0, -48); ticketRt.sizeDelta = new Vector2(tw, th);
                LayoutTicket(tw, th);
                readRt.anchorMin = readRt.anchorMax = new Vector2(0.5f, 1f); readRt.pivot = new Vector2(0.5f, 1f);
                readRt.anchoredPosition = new Vector2(0, -48 - th - 4); readRt.sizeDelta = new Vector2(tw, 30);
                readHead.rectTransform.anchoredPosition = new Vector2(32, 0);
                readTxt.rectTransform.anchorMin = readTxt.rectTransform.anchorMax = new Vector2(0, 0.5f); readTxt.rectTransform.pivot = new Vector2(0, 0.5f);
                readTxt.rectTransform.anchoredPosition = new Vector2(64, 0); readTxt.rectTransform.sizeDelta = new Vector2(tw - 72, 28);
                rackRt.anchorMin = rackRt.anchorMax = new Vector2(0.5f, 0f); rackRt.pivot = new Vector2(0.5f, 0f);
                rackRt.anchoredPosition = new Vector2(0, 6); rackRt.sizeDelta = new Vector2(W - 20, 58);
                for (int i = 0; i < 3; i++) spare[i].anchoredPosition = new Vector2(20 + i * 36, -22);
                rackLbl.rectTransform.anchoredPosition = new Vector2(-70, -6); rackLbl.alignment = TextAlignmentOptions.Right;
                insetTop = 48 + th + 38; insetBottom = 60; insetLeft = 4; insetRight = 4;
                toastRt.anchorMin = toastRt.anchorMax = new Vector2(0.5f, 0);
                toastRt.anchoredPosition = new Vector2(0, 98); toastRt.sizeDelta = new Vector2(Mathf.Min(W - 16, 440), 62);
                toastTxt.rectTransform.sizeDelta = new Vector2(Mathf.Min(W - 16, 440) - 22, 58);
            }
            else
            {
                float tw = Mathf.Clamp(W * 0.3f, 250, 380), th = Mathf.Min(H * 0.38f, 250);
                ticketRt.anchorMin = ticketRt.anchorMax = new Vector2(0f, 1f); ticketRt.pivot = new Vector2(0f, 1f);
                ticketRt.anchoredPosition = new Vector2(12, -52); ticketRt.sizeDelta = new Vector2(tw, th);
                LayoutTicket(tw, th);
                readRt.anchorMin = readRt.anchorMax = new Vector2(0f, 1f); readRt.pivot = new Vector2(0f, 1f);
                readRt.anchoredPosition = new Vector2(12, -52 - th - 6); readRt.sizeDelta = new Vector2(tw, 32);
                readHead.rectTransform.anchoredPosition = new Vector2(32, 0);
                readTxt.rectTransform.anchorMin = readTxt.rectTransform.anchorMax = new Vector2(0, 0.5f); readTxt.rectTransform.pivot = new Vector2(0, 0.5f);
                readTxt.rectTransform.anchoredPosition = new Vector2(64, 0); readTxt.rectTransform.sizeDelta = new Vector2(tw - 72, 30);
                rackRt.anchorMin = rackRt.anchorMax = new Vector2(0f, 1f); rackRt.pivot = new Vector2(0f, 1f);
                rackRt.anchoredPosition = new Vector2(12, -52 - th - 46); rackRt.sizeDelta = new Vector2(tw, 60);
                for (int i = 0; i < 3; i++) spare[i].anchoredPosition = new Vector2(-tw * 0.5f + 118 + i * 36, -22);
                rackLbl.rectTransform.anchoredPosition = new Vector2(-tw * 0.5f + 52, -6); rackLbl.alignment = TextAlignmentOptions.Left;
                insetTop = 46; insetBottom = 6; insetLeft = tw + 20; insetRight = 6;
                toastRt.anchorMin = toastRt.anchorMax = new Vector2(0, 1);
                toastRt.anchoredPosition = new Vector2(12 + tw * 0.5f, -52 - th - 150); toastRt.sizeDelta = new Vector2(tw, 92);
                toastTxt.rectTransform.sizeDelta = new Vector2(tw - 22, 86);
            }
            if (world && (force || wasLand != land)) { WorldLayout(land); if (cur != null) RefreshSheet(true); camInit = true; }
            // 타이틀: 세로 = 위 로고 · 가운데 데모 · 아래 찍어 보기 / 가로 = 왼쪽 포스터 · 오른쪽 데모 교정대
            var bbg = titleRt.Find("BadgeBg").GetComponent<RectTransform>();
            titleLand = W > H * 0.9f;
            float gap = 76;
            if (!titleLand)
            {
                for (int i = 0; i < 4; i++) { logo[i].fontSize = 84; logoGhost[i].fontSize = 84; SetA(logo[i].rectTransform, new Vector2(0.5f, 1f), new Vector2((i - 1.5f) * gap + (i >= 2 ? 14 : -14), -118)); SetA(logoGhost[i].rectTransform, new Vector2(0.5f, 1f), new Vector2((i - 1.5f) * gap + (i >= 2 ? 14 : -14) + 5, -122)); }
                SetA(logoInk.rectTransform, new Vector2(0.5f, 1f), new Vector2(40, -120)); logoInk.rectTransform.sizeDelta = new Vector2(340, 150);
                ctaBase = 1f; tagTxt.fontSize = 18;
                SetA(tagTxt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -196));
                SetA(ctaRt, new Vector2(0.5f, 0f), new Vector2(0, 132));
                SetA(bestTxt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 82));
                SetA(badgeTxt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 50)); SetA(bbg, new Vector2(0.5f, 0f), new Vector2(0, 50));
            }
            else
            {
                // 좁은 가로(정사각 공유 이미지 등)에서는 포스터 전체를 줄인다
                float f = Mathf.Clamp(W / 1000f, 0.6f, 1f);
                titlePosterW = 380 * f + 40;
                float lx = Mathf.Max(W * 0.25f, titlePosterW * 0.5f + 8);
                lx = Mathf.Min(lx, 300);
                for (int i = 0; i < 4; i++) { logo[i].fontSize = 96 * f; logoGhost[i].fontSize = 96 * f; SetA(logo[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(lx + ((i - 1.5f) * 84 + (i >= 2 ? 14 : -14)) * f, 120 * f)); SetA(logoGhost[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(lx + ((i - 1.5f) * 84 + (i >= 2 ? 14 : -14) + 5) * f, 115 * f)); }
                SetA(logoInk.rectTransform, new Vector2(0f, 0.5f), new Vector2(lx + 40 * f, 118 * f)); logoInk.rectTransform.sizeDelta = new Vector2(380, 170) * f;
                SetA(tagTxt.rectTransform, new Vector2(0f, 0.5f), new Vector2(lx, 38 * f)); tagTxt.fontSize = 18 * Mathf.Max(0.8f, f);
                SetA(ctaRt, new Vector2(0f, 0.5f), new Vector2(lx, -44 * f)); ctaBase = Mathf.Max(0.8f, f);
                SetA(bestTxt.rectTransform, new Vector2(0f, 0.5f), new Vector2(lx, -104 * f - (1 - f) * 50));
                SetA(badgeTxt.rectTransform, new Vector2(0f, 0.5f), new Vector2(lx, -140 * f - (1 - f) * 60)); SetA(bbg, new Vector2(0f, 0.5f), new Vector2(lx, -140 * f - (1 - f) * 60));
            }
            endRt.sizeDelta = new Vector2(330, Mathf.Min(370, H - 30));
            ReflowTicket();
        }

        static void SetA(RectTransform rt, Vector2 anchor, Vector2 pos) { rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = pos; }

        /// <summary>한국어 낱말 단위 줄바꿈(TMP 는 한글을 글자마다 끊는다). 폭은 글자 폭 추정(한글 1em, 기타 0.58em).</summary>
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

        float tkW = 300, tkH = 138;
        bool tkTall;

        void LayoutTicket(float tw, float th)
        {
            tkW = tw; tkH = th; tkTall = th > 170;
            float x = 16, w = tw - 32;
            void Put(TextMeshProUGUI t) { var rt = t.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = new Vector2(x, 0); rt.sizeDelta = new Vector2(w, 20); t.enableAutoSizing = false; t.overflowMode = TextOverflowModes.Overflow; }
            var hr = ticketHead.rectTransform; hr.anchorMin = hr.anchorMax = new Vector2(0, 1); hr.pivot = new Vector2(0, 1); hr.anchoredPosition = new Vector2(x, -11); hr.sizeDelta = new Vector2(w - 50, 16);
            ticketHead.enableAutoSizing = true; ticketHead.fontSizeMax = 12; ticketHead.fontSizeMin = 9; ticketHead.textWrappingMode = TextWrappingModes.NoWrap;
            ticketTextW = w - 44;
            Put(orderTxt); Put(dataTxt); Put(hintTxt);
            orderTxt.lineSpacing = -6; dataTxt.lineSpacing = -4; hintTxt.lineSpacing = -4;
            var fr = footTxt.rectTransform; fr.anchorMin = fr.anchorMax = new Vector2(0, 0); fr.pivot = new Vector2(0, 0); fr.anchoredPosition = new Vector2(x, 11); fr.sizeDelta = new Vector2(w, 14);
            footTxt.enableAutoSizing = true; footTxt.fontSizeMax = 11; footTxt.fontSizeMin = 8; footTxt.textWrappingMode = TextWrappingModes.NoWrap;
            footTxt.gameObject.SetActive(tkTall);
            FitTicket();
        }

        /// <summary>전표 글 쌓기 — 발문 · 자료 · 힌트를 실제 글 높이(TMP 측정)대로 위에서부터 쌓고,
        /// 전표 높이를 넘으면 글자 크기를 한 단계씩 줄인다. 고정 슬롯에 넣어 네 줄 발문이 자료 줄과 겹치던 결함(「채오사8.5 cm」)의 수정.</summary>
        void FitTicket()
        {
            if (!ticketRt) return;
            float x = 16, w = tkW - 32, top = 30, bottom = tkTall ? 30 : 14;
            float avail = tkH - top - bottom;
            bool hasHint = !string.IsNullOrEmpty(rawHint);
            float fsMax = tkTall ? 19 : 16;
            for (float fs = fsMax; ; fs -= 1f)
            {
                float dfs = Mathf.Min(14f, fs - 2f), hfs = Mathf.Min(13f, fs - 3f);
                orderTxt.fontSize = fs; dataTxt.fontSize = dfs; hintTxt.fontSize = hfs;
                string o = Wrap(rawOrder, ticketTextW, fs * 1.06f);
                string d = WrapItems(rawData, w, dfs * 1.06f);
                string h = hasHint ? Wrap(rawHint, w, hfs * 1.06f) : "";
                float oh = orderTxt.GetPreferredValues(o, ticketTextW, 0).y;
                float dh = string.IsNullOrEmpty(d) ? 0 : dataTxt.GetPreferredValues(d, w, 0).y;
                float hh = hasHint ? hintTxt.GetPreferredValues(h, w, 0).y : 0;
                float total = oh + 4 + dh + (hasHint ? 6 + hh : 0);
                if (total <= avail || fs <= 11f)
                {
                    orderTxt.text = o; dataTxt.text = d; hintTxt.text = h;
                    float y = -top + 2;
                    Place(orderTxt, y, ticketTextW, oh); y -= oh + 4;
                    Place(dataTxt, y, w, dh); y -= dh + 6;
                    Place(hintTxt, y, w, hh);
                    var hb = hintBg.rectTransform; hb.anchorMin = hb.anchorMax = new Vector2(0, 1); hb.pivot = new Vector2(0, 1);
                    hb.anchoredPosition = new Vector2(x - 6, y + 2); hb.sizeDelta = new Vector2(w + 12, hh + 4);
                    hintBg.enabled = hasHint;
                    return;
                }
            }
            void Place(TextMeshProUGUI t, float y, float ww, float hh) { var rt = t.rectTransform; rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(ww, Mathf.Max(hh, 1)); }
        }

        /// <summary>자료 줄 줄바꿈 — 「AB = 3.5 cm」 같은 항목 단위로만 끊는다(항목 가운데서 끊기면 「CA =」에서 잘려 읽힌다).</summary>
        static string WrapItems(string s, float widthPx, float fontPx)
        {
            if (string.IsNullOrEmpty(s)) return s;
            float max = widthPx / fontPx;
            var sb = new System.Text.StringBuilder();
            foreach (var para in s.Split('\n'))
            {
                if (sb.Length > 0) sb.Append('\n');
                float line = 0;
                var items = para.Split(new[] { ", " }, System.StringSplitOptions.None);
                for (int i = 0; i < items.Length; i++)
                {
                    string it = items[i] + (i < items.Length - 1 ? "," : "");
                    float ww = 0; foreach (char c in it) ww += c >= 0x1100 ? 1f : c == ' ' ? 0.3f : 0.6f;
                    if (line > 0 && line + 0.3f + ww > max) { sb.Append('\n'); line = 0; }
                    else if (line > 0) { sb.Append(' '); line += 0.3f; }
                    sb.Append(it); line += ww;
                }
            }
            return sb.ToString();
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
