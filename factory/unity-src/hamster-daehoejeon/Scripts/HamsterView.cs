// 햄스터 대회전 — 코드 부트스트랩 2.5D 리넨 프락시노스코프와 정면 각도계.
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.HamsterDaehoejeon
{
    static class LinenMesh
    {
        public static void Quad(VertexHelper vh, Vector2 a, Vector2 b, float thick, Color color)
        {
            Vector2 d = b - a; if (d.sqrMagnitude < .001f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * thick * .5f;
            int i = vh.currentVertCount;
            vh.AddVert(a - n, color, Vector2.zero); vh.AddVert(a + n, color, Vector2.up);
            vh.AddVert(b + n, color, Vector2.one); vh.AddVert(b - n, color, Vector2.right);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        public static void Poly(VertexHelper vh, Vector2 center, Vector2 radius, int segments, Color color, float phase = 0f)
        {
            int c = vh.currentVertCount; vh.AddVert(center, color, new Vector2(.5f, .5f));
            for (int i = 0; i <= segments; i++)
            {
                float a = phase + i * Mathf.PI * 2f / segments;
                vh.AddVert(center + new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y), color, new Vector2((Mathf.Cos(a) + 1) * .5f, (Mathf.Sin(a) + 1) * .5f));
            }
            for (int i = 0; i < segments; i++) vh.AddTriangle(c, c + i + 1, c + i + 2);
        }
    }

    public sealed class AngleDialGraphic : MaskableGraphic
    {
        public int Min = 20, Step = 1, Selected = 20;
        public bool Precision, Valid = true;
        public float Flash;
        public Vector2 Center => new Vector2(0, -rectTransform.rect.height * .31f);
        public float Radius => Mathf.Min(rectTransform.rect.width * .39f, rectTransform.rect.height * .53f) * (Precision ? 1.15f : 1f);

        public void Configure(int min, int step, int selected, bool precision, bool valid, float flash)
        {
            Min = min; Step = step; Selected = selected; Precision = precision; Valid = valid; Flash = flash; SetVerticesDirty();
        }

        public Vector2 HandleLocal(int selected)
        {
            int index = Mathf.Clamp((selected - Min) / Mathf.Max(1, Step), 0, 59);
            float degrees = Mathf.Lerp(160f, 20f, index / 59f) * Mathf.Deg2Rad;
            return Center + new Vector2(Mathf.Cos(degrees), Mathf.Sin(degrees)) * Radius;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Color ink = MgfLook.Hex("#232525"), linen = MgfLook.Hex("#F3EFE3"), red = MgfLook.Hex("#D74735"), blue = MgfLook.Hex("#88AAB3"), brass = MgfLook.Hex("#C9B68C");
            Color backing = new Color(linen.r, linen.g, linen.b, .94f);
            Rect r = rectTransform.rect;
            LinenMesh.Poly(vh, Center + new Vector2(0, 24), new Vector2(r.width * .48f, r.height * .55f), 28, backing, Mathf.PI * .5f);
            float radius = Radius;
            Vector2 prev = Vector2.zero;
            for (int i = 0; i <= 80; i++)
            {
                float a = Mathf.Lerp(160f, 20f, i / 80f) * Mathf.Deg2Rad;
                Vector2 p = Center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                if (i > 0) LinenMesh.Quad(vh, prev, p, Precision ? 5f : 3f, new Color(blue.r, blue.g, blue.b, .52f));
                prev = p;
            }
            int selectedIndex = Mathf.Clamp((Selected - Min) / Mathf.Max(1, Step), 0, 59);
            for (int i = 0; i < 60; i++)
            {
                float a = Mathf.Lerp(160f, 20f, i / 59f) * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float len = i % 10 == 0 ? 15f : i % 5 == 0 ? 11f : 7f;
                Color c = i == selectedIndex ? red : new Color(ink.r, ink.g, ink.b, i % 5 == 0 ? .72f : .38f);
                LinenMesh.Quad(vh, Center + d * (radius - len), Center + d * (radius + 2f), i == selectedIndex ? 4.5f : 1.6f, c);
            }
            // 학생이 실제로 만드는 두 레일 사이의 각. 화면 눈금 원호의 배치와 별개로 실제 선택각을 그대로 그린다.
            float fixedDeg = 168f;
            float movingDeg = fixedDeg - Selected;
            Vector2 fixedD = new Vector2(Mathf.Cos(fixedDeg * Mathf.Deg2Rad), Mathf.Sin(fixedDeg * Mathf.Deg2Rad));
            Vector2 movingD = new Vector2(Mathf.Cos(movingDeg * Mathf.Deg2Rad), Mathf.Sin(movingDeg * Mathf.Deg2Rad));
            LinenMesh.Quad(vh, Center, Center + fixedD * radius * .68f, 9f, ink);
            LinenMesh.Quad(vh, Center, Center + movingD * radius * .68f, 9f, Color.Lerp(red, Color.white, Flash * .28f));
            LinenMesh.Poly(vh, Center, Vector2.one * 12f, 18, brass);
            if (!Valid) LinenMesh.Poly(vh, HandleLocal(Selected), Vector2.one * 18f, 18, new Color(red.r, red.g, red.b, .18f));
        }
    }

    public sealed class TriangleDiagramGraphic : MaskableGraphic
    {
        public int Rotation;
        public int Band;
        public float Reveal;
        public bool Correct;
        public Vector2 A, B, C, D;

        public void Configure(int rotation, int band, bool correct, float reveal)
        {
            Rotation = rotation; Band = band; Correct = correct; Reveal = reveal; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            A = Rotate(new Vector2(0, r.height * .35f)); B = Rotate(new Vector2(-r.width * .32f, -r.height * .31f)); C = Rotate(new Vector2(r.width * .32f, -r.height * .31f)); D = (B + C) * .5f;
            Color ink = MgfLook.Hex("#232525"), red = MgfLook.Hex("#D74735"), blue = MgfLook.Hex("#88AAB3"), brass = MgfLook.Hex("#C9B68C");
            LinenMesh.Quad(vh, A, B, 4.5f, ink); LinenMesh.Quad(vh, A, C, 4.5f, ink); LinenMesh.Quad(vh, B, C, 4.5f, ink);
            Vector2 ab = (B - A).normalized, ac = (C - A).normalized;
            Vector2 lm = Vector2.Lerp(A, B, .53f), rm = Vector2.Lerp(A, C, .53f);
            Vector2 ln = new Vector2(-ab.y, ab.x), rn = new Vector2(-ac.y, ac.x);
            LinenMesh.Quad(vh, lm - ln * 7, lm + ln * 7, 3f, red); LinenMesh.Quad(vh, rm - rn * 7, rm + rn * 7, 3f, red);
            if (Band == 3)
            {
                LinenMesh.Quad(vh, A, D, 3.5f, Color.Lerp(blue, brass, Reveal));
                Vector2 bd = (B - D).normalized, da = (A - D).normalized;
                LinenMesh.Quad(vh, D, D + bd * 12, 2.5f, ink); LinenMesh.Quad(vh, D + bd * 12, D + bd * 12 + da * 12, 2.5f, ink);
            }
            if (Reveal > 0)
            {
                Color c = Correct ? Color.Lerp(blue, Color.white, .25f) : red;
                LinenMesh.Quad(vh, A, D, 5f + Reveal * 3f, new Color(c.r, c.g, c.b, .9f));
            }
        }

        Vector2 Rotate(Vector2 p)
        {
            float a = Rotation * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
        }
    }

    public sealed class FilmStripGraphic : MaskableGraphic
    {
        public int Solved;
        public float Clock;
        public float Align;
        public bool DoublePrint;

        public void Configure(int solved, float clock, float align, bool doublePrint)
        {
            Solved = solved; Clock = clock; Align = align; DoublePrint = doublePrint; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r = rectTransform.rect;
            Color linen = MgfLook.Hex("#F3EFE3"), ink = MgfLook.Hex("#232525"), red = MgfLook.Hex("#D74735"), birch = MgfLook.Hex("#C9B68C");
            float gap = 3f, cellW = (r.width - gap * 9) / 8f;
            for (int i = 0; i < 8; i++)
            {
                float x = r.xMin + gap + cellW * .5f + i * (cellW + gap);
                Rect cell = new Rect(x - cellW * .5f, r.yMin + 7, cellW, r.height - 14);
                Color paper = i < Solved ? Color.Lerp(linen, Color.white, .18f * Align) : new Color(linen.r, linen.g, linen.b, .36f);
                AddRect(vh, cell, paper);
                LinenMesh.Quad(vh, new Vector2(cell.xMin, cell.yMin), new Vector2(cell.xMax, cell.yMin), 3f, i < Solved ? red : new Color(birch.r, birch.g, birch.b, .36f));
                if (i < Mathf.Max(1, Solved)) DrawHamster(vh, new Vector2(x, cell.center.y), cellW, i, ink, DoublePrint && i == Mathf.Max(0, Solved - 1));
            }
        }

        static void AddRect(VertexHelper vh, Rect r, Color c)
        {
            int i = vh.currentVertCount; vh.AddVert(new Vector2(r.xMin, r.yMin), c, Vector2.zero); vh.AddVert(new Vector2(r.xMin, r.yMax), c, Vector2.up);
            vh.AddVert(new Vector2(r.xMax, r.yMax), c, Vector2.one); vh.AddVert(new Vector2(r.xMax, r.yMin), c, Vector2.right);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        void DrawHamster(VertexHelper vh, Vector2 c, float w, int frame, Color ink, bool ghost)
        {
            float gait = Mathf.Sin((frame + Clock * 6f) * Mathf.PI * .5f);
            Vector2 off = ghost ? new Vector2(3.5f, -1.5f) : Vector2.zero;
            Color col = ghost ? new Color(ink.r, ink.g, ink.b, .32f) : ink;
            LinenMesh.Poly(vh, c + off + new Vector2(-w * .04f, 0), new Vector2(w * .29f, w * .22f), 14, col);
            LinenMesh.Poly(vh, c + off + new Vector2(w * .22f, w * .08f), new Vector2(w * .17f, w * .16f), 12, col);
            LinenMesh.Poly(vh, c + off + new Vector2(w * .28f, w * .23f), Vector2.one * w * .06f, 8, col);
            Color belly = MgfLook.Hex("#F3EFE3"); LinenMesh.Poly(vh, c + off + new Vector2(w * .03f, -w * .01f), new Vector2(w * .13f, w * .10f), 10, belly);
            LinenMesh.Poly(vh, c + off + new Vector2(w * .31f, w * .11f), Vector2.one * w * .025f, 8, belly);
            LinenMesh.Quad(vh, c + off + new Vector2(-w * .14f, -w * .18f), c + off + new Vector2(-w * .22f + gait * 2, -w * .29f), 2.6f, col);
            LinenMesh.Quad(vh, c + off + new Vector2(w * .06f, -w * .18f), c + off + new Vector2(w * .16f - gait * 2, -w * .29f), 2.6f, col);
            if (ghost) return;
        }
    }

    public sealed class HamsterView
    {
        readonly HamsterDaehoejeonGame game;
        readonly Color linen = MgfLook.Hex("#F3EFE3"), ink = MgfLook.Hex("#232525"), red = MgfLook.Hex("#D74735"), birch = MgfLook.Hex("#C9B68C"), dawn = MgfLook.Hex("#88AAB3");
        Camera cam;
        Transform world, drum, titleBanner, windChimes;
        readonly List<Transform> mirrors = new List<Transform>();
        readonly List<Transform> filmPanels = new List<Transform>();
        readonly List<Transform> cords = new List<Transform>();
        readonly List<Quaternion> cordBases = new List<Quaternion>();
        float worldClock, drumVelocity = 7f, revealSpin, curtainClock, refuseClock, hitStop, scoreShown, endClock;
        int lastW, lastH;
        float lastCanvasW, lastCanvasH;
        bool wide, revealing, revealCorrect, dragging, validArc = true, doublePrint;
        bool playVisibilityPending;
        int playStableLayoutFrames;
        float playGateCanvasW = -1f, playGateCanvasH = -1f;

        RectTransform canvasRt, titleStartRt, restartRt, hudRt, goalRt, dialRt, handleRt, promptRt, triangleRt, guideHandRt, rippleRt, curtainLeftRt, curtainRightRt;
        CanvasGroup titleGroup, playGroup, revealGroup, endGroup, feedbackGroup;
        AngleDialGraphic dial;
        TriangleDiagramGraphic triangle;
        FilmStripGraphic film;
        Image handle, handleRing, titleRing, ripple, curtainLeft, curtainRight;
        readonly Image[] brakes = new Image[3];
        readonly Image[] framePips = new Image[8];
        readonly Image[] trail = new Image[7];
        TMP_Text titleBest, progressText, firstText, timeText, scoreText, promptText, conditionText, angleText, scaleText, goalText;
        TMP_Text revealTitle, revealBody, toastText, endTitle, endStats, endCounter;
        TMP_Text labelA, labelB, labelC, labelD;
        AngleProblem shown;
        float revealAmount, dialFlash, guideClock, rippleClock, toastClock;
        static Sprite softSprite;
        static Sprite SoftSprite => softSprite ? softSprite : softSprite = Sprite.Create(MgfLook.SoftDot, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64f);

        public HamsterView(HamsterDaehoejeonGame game)
        {
            this.game = game; BuildWorld(); BuildUi(); Relayout(true);
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#DDE7E7"), MgfLook.Hex("#F3EFE3"), MgfLook.Hex("#D8CFBA"), .92f);
            MgfLook.Sun(new Vector3(36, -28, -18), new Color(1f, .96f, .84f), 1.18f, .36f);
            cam = MgfLook.Camera(new Vector3(0, 9.8f, -19.5f), new Vector3(0, 2.5f, 2.8f), 35f);
            cam.orthographic = true; cam.orthographicSize = 8.6f; cam.backgroundColor = linen;
            world = new GameObject("WallFillingLinenPraxinoscope").transform;
            var clothMat = MgfLook.Lit(linen, .04f, 0f);
            var clothShade = MgfLook.Lit(MgfLook.Hex("#E2DCCB"), .03f, 0f);
            var birchMat = MgfLook.Lit(birch, .22f, 0f);
            var mirrorMat = MgfLook.Lit(dawn, .78f, .72f);
            var inkMat = MgfLook.Lit(ink, .08f, .04f);
            var redMat = MgfLook.Lit(red, .18f, 0f);
            var brassMat = MgfLook.Lit(MgfLook.Hex("#B89250"), .58f, .65f);

            for (int i = 0; i < 3; i++)
            {
                var panel = MgfLook.Block("LayeredNorthWindowCloth" + i, new Vector3((i - 1) * 7.8f, 5.0f + i * .25f, 10f + i * 2.1f), new Vector3(9.2f, 15.5f, .12f), .08f, i == 1 ? clothMat : clothShade, world).transform;
                panel.localRotation = Quaternion.Euler(0, (i - 1) * 5f, (i - 1) * 1.8f);
                cords.Add(panel);
                for (int f = 0; f < 5; f++) MgfLook.Block("ClothFold" + i + "_" + f, new Vector3((f - 2) * 1.55f, .2f, -.1f), new Vector3(.16f, 14.8f, .16f), .06f, clothShade, panel);
            }

            drum = new GameObject("EightFacetPraxinoscope").transform; drum.SetParent(world, false); drum.localPosition = new Vector3(0, 1.75f, 3.7f);
            MgfLook.Prim(PrimitiveType.Cylinder, "BirchLowerHoop", new Vector3(0, -.7f, 0), new Vector3(5.4f, .22f, 5.4f), birchMat, drum, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "BirchUpperHoop", new Vector3(0, 3.2f, 0), new Vector3(5.4f, .22f, 5.4f), birchMat, drum, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "InkAxle", new Vector3(0, 1.2f, 0), new Vector3(.24f, 4.8f, .24f), inkMat, drum, false);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad; Vector3 p = new Vector3(Mathf.Sin(a) * 2.1f, 1.2f, Mathf.Cos(a) * 2.1f);
                var facet = MgfLook.Block("NarrowMirrorFacet" + i, p, new Vector3(1.45f, 2.95f, .08f), .025f, mirrorMat, drum).transform;
                facet.localRotation = Quaternion.Euler(0, i * 45f, 0); mirrors.Add(facet);
                Vector3 fp = new Vector3(Mathf.Sin(a) * 3.0f, 1.15f, Mathf.Cos(a) * 3.0f);
                var panel = MgfLook.Block("LinenFilmFrame" + i, fp, new Vector3(1.35f, 2.2f, .10f), .035f, clothMat, drum).transform;
                panel.localRotation = Quaternion.Euler(0, i * 45f, 0); filmPanels.Add(panel);
                MgfLook.Block("CharcoalHamsterBody" + i, new Vector3(0, .08f + (i % 2) * .06f, -.08f), new Vector3(.52f, .39f, .04f), .18f, inkMat, panel);
                MgfLook.Prim(PrimitiveType.Sphere, "CharcoalHamsterHead" + i, new Vector3(.37f, .18f, -.13f), new Vector3(.30f, .28f, .06f), inkMat, panel, false);
                MgfLook.Prim(PrimitiveType.Sphere, "CreamBelly" + i, new Vector3(.02f, .02f, -.14f), new Vector3(.24f, .18f, .04f), clothMat, panel, false);
                var clip = MgfLook.Block("AgedBrassClip" + i, new Vector3(0, 1.28f, -.12f), new Vector3(.42f, .12f, .10f), .04f, brassMat, panel).transform;
                clip.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -8f : 8f);
            }
            titleBanner = MgfLook.Block("HangingLinenTitleBanner", new Vector3(0, 8.8f, 2.7f), new Vector3(8.2f, 2.0f, .12f), .11f, clothMat, world).transform;
            MgfLook.Prim(PrimitiveType.Cylinder, "VermilionTensionCord", new Vector3(0, 10.0f, 2.6f), new Vector3(.055f, 10.5f, .055f), redMat, world, false).transform.rotation = Quaternion.Euler(0, 0, 90);
            windChimes = new GameObject("TinyWindChimes").transform; windChimes.SetParent(world, false); windChimes.localPosition = new Vector3(5.2f, 7.7f, 2.2f);
            for (int i = 0; i < 3; i++)
            {
                var chime = MgfLook.Prim(PrimitiveType.Cylinder, "BrassChime" + i, new Vector3((i - 1) * .35f, -i * .18f, 0), new Vector3(.08f, .52f, .08f), brassMat, windChimes, false).transform;
                cords.Add(chime);
            }
            for (int i = 0; i < cords.Count; i++) cordBases.Add(cords[i].localRotation);
        }

        void BuildUi()
        {
            canvasRt = (RectTransform)MgfText.Canvas.transform;
            titleGroup = Group("TitleUI"); playGroup = Group("PlayUI"); revealGroup = Group("RevealUI"); endGroup = Group("EndUI"); feedbackGroup = Group("FeedbackUI");
            BuildTitle(); BuildPlay(); BuildReveal(); BuildEnd(); BuildFeedback();
        }

        CanvasGroup Group(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup)); go.transform.SetParent(canvasRt, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go.GetComponent<CanvasGroup>();
        }

        Image Img(string name, Transform parent, Color color, Vector2 size, Vector2 anchor, Vector2 pos, bool soft = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.sizeDelta = size; rt.anchoredPosition = pos;
            var im = go.GetComponent<Image>(); im.color = color; im.raycastTarget = false; if (soft) im.sprite = SoftSprite; return im;
        }

        TMP_Text Txt(string name, Transform parent, string text, float size, Color color, Vector2 box, Vector2 anchor, Vector2 pos, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.sizeDelta = box; rt.anchoredPosition = pos;
            var t = go.GetComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.fontStyle = FontStyles.Bold; t.color = color; t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text; return t;
        }

        void BuildTitle()
        {
            var banner = Img("LinenLogoBanner", titleGroup.transform, new Color(linen.r, linen.g, linen.b, .96f), new Vector2(356, 194), new Vector2(.5f, .72f), Vector2.zero);
            Img("VermilionTopCord", banner.transform, red, new Vector2(380, 7), new Vector2(.5f, .97f), Vector2.zero);
            var shadow = Txt("InkOffsetLogo", banner.transform, "햄스터\n대회전", 54, new Color(red.r, red.g, red.b, .78f), new Vector2(340, 132), new Vector2(.5f, .57f), new Vector2(3, -3)); shadow.characterSpacing = -3;
            var logo = Txt("CharcoalLogo", banner.transform, "햄스터\n대회전", 54, ink, new Vector2(340, 132), new Vector2(.5f, .57f), Vector2.zero); logo.characterSpacing = -3; logo.outlineColor = linen; logo.outlineWidth = .17f;
            Txt("Tagline", banner.transform, "레일을 돌려 달려라", 15, red, new Vector2(280, 28), new Vector2(.5f, .12f), Vector2.zero);
            titleRing = Img("TitleVermilionRipple", titleGroup.transform, new Color(red.r, red.g, red.b, .24f), new Vector2(172, 172), new Vector2(.5f, .31f), Vector2.zero, true);
            var knob = Img("TitleBlackCrank", titleGroup.transform, ink, new Vector2(132, 132), new Vector2(.5f, .31f), Vector2.zero, true); titleStartRt = knob.rectTransform;
            var titleGrip = Img("TitleCrankDiamond", knob.transform, ink, new Vector2(74, 74), new Vector2(.5f, .5f), Vector2.zero); titleGrip.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            Img("TitleBrassHub", knob.transform, birch, new Vector2(34, 34), new Vector2(.5f, .5f), Vector2.zero, true);
            Txt("TurnVerb", knob.transform, "돌리기", 13, Color.white, new Vector2(70, 25), new Vector2(.5f, .18f), Vector2.zero);
            Txt("StartOnObject", titleGroup.transform, "손잡이를 눌러 필름 돌리기", 16, ink, new Vector2(270, 32), new Vector2(.5f, .19f), Vector2.zero);
            Txt("TitleUnit", titleGroup.transform, "중학교 2학년 · 삼각형의 성질", 13, ink, new Vector2(330, 28), new Vector2(.5f, .055f), Vector2.zero);
            titleBest = Txt("TitleBest", titleGroup.transform, "최고 첫 제출 0/8", 12, red, new Vector2(240, 26), new Vector2(.5f, .105f), Vector2.zero);
        }

        void BuildPlay()
        {
            var hud = Img("LinenHud", playGroup.transform, new Color(linen.r, linen.g, linen.b, .98f), new Vector2(382, 62), new Vector2(.5f, 1f), new Vector2(0, -32)); hudRt = hud.rectTransform;
            progressText = Txt("Progress", hud.transform, "필름 0/8", 13, ink, new Vector2(86, 40), new Vector2(.12f, .5f), Vector2.zero);
            firstText = Txt("FirstAccuracy", hud.transform, "첫 제출 0/0", 13, ink, new Vector2(112, 40), new Vector2(.39f, .5f), Vector2.zero);
            timeText = Txt("Time", hud.transform, "100초", 13, ink, new Vector2(62, 40), new Vector2(.63f, .5f), Vector2.zero);
            scoreText = Txt("Score", hud.transform, "0", 14, red, new Vector2(66, 40), new Vector2(.91f, .5f), Vector2.zero);
            for (int i = 0; i < 3; i++) brakes[i] = Img("BrakeClip" + i, hud.transform, MgfLook.Hex("#B89250"), new Vector2(12, 31), new Vector2(.72f + i * .045f, .5f), Vector2.zero);
            var goal = Img("PermanentGoal", playGroup.transform, new Color(red.r, red.g, red.b, .96f), new Vector2(370, 38), new Vector2(.5f, 1f), new Vector2(0, -82)); goalRt = goal.rectTransform;
            goalText = Txt("Goal", goal.transform, "빈 각을 맞추면 햄스터가 달린다", 14, Color.white, new Vector2(350, 30), new Vector2(.5f, .5f), Vector2.zero);
            goalText.enableAutoSizing = true; goalText.fontSizeMin = 10f; goalText.fontSizeMax = 14f;

            var filmGo = new GameObject("EightFrameLinenFilm", typeof(RectTransform), typeof(CanvasRenderer)); filmGo.transform.SetParent(playGroup.transform, false);
            var filmRt = (RectTransform)filmGo.transform; filmRt.anchorMin = filmRt.anchorMax = new Vector2(.5f, .79f); filmRt.sizeDelta = new Vector2(360, 106);
            film = filmGo.AddComponent<FilmStripGraphic>(); film.color = Color.white; film.raycastTarget = false;
            for (int i = 0; i < 8; i++) framePips[i] = Img("FramePin" + i, filmGo.transform, i == 0 ? red : birch, new Vector2(7, 7), new Vector2((i + .5f) / 8f, .98f), Vector2.zero, true);

            var prompt = Img("LinenConditionPlate", playGroup.transform, new Color(linen.r, linen.g, linen.b, .98f), new Vector2(374, 100), new Vector2(.5f, .61f), Vector2.zero); promptRt = prompt.rectTransform;
            promptText = Txt("ProblemPrompt", prompt.transform, "", 13, ink, new Vector2(352, 82), new Vector2(.5f, .54f), Vector2.zero);
            promptText.enableAutoSizing = true; promptText.fontSizeMin = 10f; promptText.fontSizeMax = 13f;
            conditionText = Txt("ModelTag", prompt.transform, "모식도 · 각도는 정면 눈금에서 조절", 10, red, new Vector2(300, 20), new Vector2(.5f, .10f), Vector2.zero);
            conditionText.enableAutoSizing = true; conditionText.fontSizeMin = 8f; conditionText.fontSizeMax = 10f;

            var triGo = new GameObject("FrontFacingTriangleDiagram", typeof(RectTransform), typeof(CanvasRenderer)); triGo.transform.SetParent(playGroup.transform, false);
            triangleRt = (RectTransform)triGo.transform; triangleRt.anchorMin = triangleRt.anchorMax = new Vector2(.5f, .43f); triangleRt.sizeDelta = new Vector2(310, 190);
            triangle = triGo.AddComponent<TriangleDiagramGraphic>(); triangle.color = Color.white; triangle.raycastTarget = false;
            labelA = Txt("VertexA", triGo.transform, "A", 15, ink, new Vector2(34, 26), new Vector2(.5f, .5f), Vector2.zero);
            labelB = Txt("VertexB", triGo.transform, "B", 15, ink, new Vector2(34, 26), new Vector2(.5f, .5f), Vector2.zero);
            labelC = Txt("VertexC", triGo.transform, "C", 15, ink, new Vector2(34, 26), new Vector2(.5f, .5f), Vector2.zero);
            labelD = Txt("VertexD", triGo.transform, "D", 13, red, new Vector2(30, 24), new Vector2(.5f, .5f), Vector2.zero);

            var dialGo = new GameObject("SixtyDetentRotaryRail", typeof(RectTransform), typeof(CanvasRenderer)); dialGo.transform.SetParent(playGroup.transform, false);
            dialRt = (RectTransform)dialGo.transform; dialRt.anchorMin = dialRt.anchorMax = new Vector2(.5f, .17f); dialRt.sizeDelta = new Vector2(360, 250);
            dial = dialGo.AddComponent<AngleDialGraphic>(); dial.color = Color.white; dial.raycastTarget = false;
            handleRing = Img("HandleGuideRing", dialGo.transform, new Color(red.r, red.g, red.b, .24f), new Vector2(76, 76), new Vector2(.5f, .5f), Vector2.zero, true);
            handle = Img("RoundDragHandle", dialGo.transform, ink, new Vector2(58, 58), new Vector2(.5f, .5f), Vector2.zero, true); handleRt = handle.rectTransform;
            var grip = Img("CharcoalHandleDiamond", handle.transform, ink, new Vector2(34, 34), new Vector2(.5f, .5f), Vector2.zero); grip.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            Img("BrassHandleCore", handle.transform, birch, new Vector2(18, 18), new Vector2(.5f, .5f), Vector2.zero, true);
            angleText = Txt("SelectedAngle", dialGo.transform, "20°", 30, ink, new Vector2(122, 48), new Vector2(.5f, .28f), Vector2.zero);
            scaleText = Txt("ScaleCaption", dialGo.transform, "20~79° · 1° 눈금", 11, red, new Vector2(210, 24), new Vector2(.5f, .12f), Vector2.zero);
            for (int i = 0; i < trail.Length; i++) trail[i] = Img("CharcoalTrail" + i, dialGo.transform, new Color(ink.r, ink.g, ink.b, .18f), new Vector2(10, 10), new Vector2(.5f, .5f), Vector2.zero, true);
            guideHandRt = Img("GhostFinger", dialGo.transform, new Color(red.r, red.g, red.b, .78f), new Vector2(32, 44), new Vector2(.5f, .5f), Vector2.zero, true).rectTransform;
        }

        void BuildReveal()
        {
            var plate = Img("EvidenceLinen", revealGroup.transform, new Color(linen.r, linen.g, linen.b, .98f), new Vector2(354, 112), new Vector2(.5f, .56f), Vector2.zero);
            Img("EvidenceCord", plate.transform, red, new Vector2(330, 5), new Vector2(.5f, .94f), Vector2.zero);
            revealTitle = Txt("EvidenceTitle", plate.transform, "황동 클립 잠김", 19, ink, new Vector2(320, 34), new Vector2(.5f, .68f), Vector2.zero);
            revealBody = Txt("EvidenceBody", plate.transform, "", 14, ink, new Vector2(326, 56), new Vector2(.5f, .30f), Vector2.zero);
        }

        void BuildEnd()
        {
            var card = Img("FinalLinenReel", endGroup.transform, new Color(linen.r, linen.g, linen.b, .98f), new Vector2(360, 410), new Vector2(.5f, .48f), Vector2.zero);
            Img("FinalTensionCord", card.transform, red, new Vector2(370, 8), new Vector2(.5f, .94f), Vector2.zero);
            endTitle = Txt("EndTitle", card.transform, "여덟 프레임 완성", 31, ink, new Vector2(330, 70), new Vector2(.5f, .80f), Vector2.zero);
            endCounter = Txt("EndCounter", card.transform, "0", 68, red, new Vector2(240, 90), new Vector2(.5f, .57f), Vector2.zero);
            endStats = Txt("EndStats", card.transform, "", 17, ink, new Vector2(310, 108), new Vector2(.5f, .36f), Vector2.zero);
            var knob = Img("RestartCrank", card.transform, ink, new Vector2(92, 92), new Vector2(.5f, .13f), Vector2.zero, true); restartRt = knob.rectTransform;
            Img("RestartHub", knob.transform, birch, new Vector2(26, 26), new Vector2(.5f, .5f), Vector2.zero, true);
            Txt("RestartLabel", card.transform, "다시 돌리기", 14, ink, new Vector2(130, 26), new Vector2(.5f, .025f), Vector2.zero);
        }

        void BuildFeedback()
        {
            ripple = Img("InkRipple", feedbackGroup.transform, new Color(ink.r, ink.g, ink.b, 0), new Vector2(40, 40), new Vector2(.5f, .5f), Vector2.zero, true); rippleRt = ripple.rectTransform;
            var toast = Img("RefusalLinen", feedbackGroup.transform, new Color(linen.r, linen.g, linen.b, .98f), new Vector2(346, 44), new Vector2(.5f, .25f), Vector2.zero);
            toastText = Txt("RefusalText", toast.transform, "", 12, ink, new Vector2(328, 36), new Vector2(.5f, .5f), Vector2.zero); toast.gameObject.SetActive(false);
            curtainLeft = Img("SplitLinenCurtainLeft", feedbackGroup.transform, linen, new Vector2(220, 900), new Vector2(0, .5f), Vector2.zero); curtainLeftRt = curtainLeft.rectTransform; curtainLeftRt.pivot = new Vector2(0, .5f);
            curtainRight = Img("SplitLinenCurtainRight", feedbackGroup.transform, linen, new Vector2(220, 900), new Vector2(1, .5f), Vector2.zero); curtainRightRt = curtainRight.rectTransform; curtainRightRt.pivot = new Vector2(1, .5f);
            curtainLeft.gameObject.SetActive(false); curtainRight.gameObject.SetActive(false);
        }

        public void ShowTitle()
        {
            Set(titleGroup, true); Set(playGroup, false); Set(revealGroup, false); Set(endGroup, false); Set(feedbackGroup, true);
            playVisibilityPending = false;
            titleBest.text = "최고 첫 제출 " + PlayerPrefs.GetInt("hamster-daehoejeon-best", 0) + "/8";
            revealSpin = 0f; drumVelocity = 6f; doublePrint = false;
        }

        public void ShowPlay(bool practice, bool bridge, bool fromTest)
        {
            Set(titleGroup, false); Set(playGroup, true); Set(revealGroup, false); Set(endGroup, false); Set(feedbackGroup, true);
            // 비활성 상태였던 uGUI를 켠 직후 CanvasScaler가 이전 논리 폭을 한 프레임 유지할 수 있다.
            // 실제 캔버스 크기가 두 프레임 연속 안정될 때까지 투명하게 두어 좁게 찌그러진 UI를 노출하지 않는다.
            playGroup.alpha = 0f;
            playVisibilityPending = true;
            playStableLayoutFrames = 0;
            playGateCanvasW = playGateCanvasH = -1f;
            goalText.text = practice ? "빈 각을 맞추면 햄스터가 달린다 · 첫 연습" : bridge ? "이등분선이 만든 반각을 직접 맞추시오" : "8칸 수리 · 첫 제출 6칸 · 브레이크 3개";
            endClock = 0f; RefreshHud(true);
        }

        public void LoadProblem(AngleProblem p, bool training, bool bridge)
        {
            shown = p; revealing = false; revealAmount = 0f; dialFlash = 0f; validArc = true; doublePrint = false;
            promptText.text = p.prompt; conditionText.text = bridge ? "연결 연습 · AD는 ∠A의 이등분선 · 미채점" : "모식도 · 같은 두 변의 빗금을 먼저 확인";
            triangle.Configure(p.diagramRotation, p.band, true, 0f);
            labelA.text = p.apex; labelB.text = p.baseLeft; labelC.text = p.baseRight; labelD.text = p.divider; labelD.gameObject.SetActive(p.band == 3);
            PositionTriangleLabels();
            angleText.text = game.State.selectedAngle + "°";
            scaleText.text = p.allowedMin + "~" + p.allowedMax + "° · " + p.allowedStep + "° 눈금";
            dial.Configure(p.allowedMin, p.allowedStep, game.State.selectedAngle, false, true, 0f); PositionHandle();
            guideHandRt.gameObject.SetActive(training); guideClock = 0f;
            Relayout(true); RefreshHud(true); SyncGeometry();
        }

        public void BeginReveal(bool correct, AngleProblem p, int selected, bool first, object trainingKind)
        {
            revealing = true; revealCorrect = correct; revealAmount = .01f; dialFlash = correct ? 1f : 0f; doublePrint = !correct; hitStop = correct ? .075f : 0f;
            Set(revealGroup, true);
            revealTitle.text = correct ? "황동 클립 잠김 · " + selected + "°" : "레일이 " + selected + "°에 비뚤어졌습니다";
            revealBody.text = correct ? p.evidence : WrongReason(game.State.misconceptionId, p, selected);
            triangle.Configure(p.diagramRotation, p.band, correct, .01f);
            film.Configure(game.State.solved, worldClock, 0f, !correct);
            if (correct)
            {
                revealSpin = 1f + game.State.combo * .22f; drumVelocity = 34f + game.State.combo * 4f; MgfSfx.Play("correct", .48f);
                MgfFx.Punch(handleRt, .22f, .28f); MgfFx.Punch(scoreText.transform, .17f, .25f);
            }
            else
            {
                drumVelocity = 0f; MgfSfx.Play("wrong", .42f); refuseClock = .42f;
            }
            RefreshHud(true);
        }

        string WrongReason(string id, AngleProblem p, int selected)
        {
            switch (id)
            {
                case "base_sum_as_one": return selected + "°는 두 밑각의 합입니다. 같은 두 밑각으로 나누시오.";
                case "one_base_subtraction": return "밑각 하나만 빼지 말고 두 밑각 " + p.baseAngle + "°를 빼시오.";
                case "whole_apex_as_half": return selected + "°는 꼭지각 전체입니다. 이등분된 한 각을 만드시오.";
                case "half_base_angle": return "주어진 밑각을 반으로 나누지 말고 먼저 꼭지각을 구하시오.";
                case "base_as_apex": case "base_as_half": return "밑각과 꼭지각을 구분하시오. 같은 두 변 사이가 꼭지각입니다.";
                default: return "선택한 " + selected + "°와 식을 비교하고 같은 레일을 다시 조절하시오.";
            }
        }

        public void FinishReveal()
        {
            revealing = false; revealAmount = 0f; doublePrint = false; Set(revealGroup, false);
            if (triangle && shown != null) triangle.Configure(shown.diagramRotation, shown.band, true, 0f);
        }

        public void ShowEnd(bool clear, string reason)
        {
            Set(titleGroup, false); Set(playGroup, false); Set(revealGroup, false); Set(endGroup, true); Set(feedbackGroup, true); endClock = 0f;
            playVisibilityPending = false;
            endTitle.text = clear ? "여덟 프레임이 달립니다" : reason == "brakes" ? "브레이크가 모두 풀렸습니다" : reason == "time" ? "필름 시간이 끝났습니다" : "첫 제출 6칸이 필요합니다";
            endStats.text = "수리 " + game.State.solved + "/8\n첫 제출 " + game.State.firstCorrect + "/" + game.State.firstAttempts + "\n점수 " + game.State.score;
            endCounter.text = "0"; drumVelocity = clear ? 56f : 0f; revealSpin = clear ? 4f : 0f;
        }

        public void BeginDrag() { dragging = true; MgfFx.Punch(handleRt, -.12f, .18f); }
        public void DragTo(int selected, bool valid, bool precision)
        {
            validArc = valid; game.State.selectedAngle = selected; angleText.text = selected + "°"; scaleText.text = precision ? "정밀 눈금 · 모든 칸의 폭은 같습니다" : shown.allowedMin + "~" + shown.allowedMax + "° · " + shown.allowedStep + "° 눈금";
            dial.Configure(shown.allowedMin, shown.allowedStep, selected, precision, valid, dialFlash); PositionHandle();
            for (int i = trail.Length - 1; i > 0; i--) trail[i].rectTransform.anchoredPosition = trail[i - 1].rectTransform.anchoredPosition;
            trail[0].rectTransform.anchoredPosition = handleRt.anchoredPosition;
        }
        public void EndDrag() { dragging = false; }

        public void Refuse(string message, Vector2 screen, bool pointAtHandle)
        {
            toastText.transform.parent.gameObject.SetActive(true); toastText.text = message; toastClock = 1.45f; refuseClock = .42f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var local); rippleRt.anchoredPosition = local;
            rippleClock = .46f; ripple.color = new Color(ink.r, ink.g, ink.b, .45f); rippleRt.localScale = Vector3.one * .4f;
            if (pointAtHandle) guideHandRt.gameObject.SetActive(true);
        }

        public void SplitCurtain()
        {
            curtainClock = .42f; curtainLeft.gameObject.SetActive(true); curtainRight.gameObject.SetActive(true);
            curtainLeftRt.anchoredPosition = curtainRightRt.anchoredPosition = Vector2.zero;
        }

        public bool HitTitleStart(Vector2 p) => Contains(titleStartRt, p, 40f);
        public bool HitRestart(Vector2 p) => Contains(restartRt, p, 34f);
        public bool HitHandle(Vector2 p) => Vector2.Distance(p, HandleScreen()) <= Mathf.Max(54f, Mathf.Min(Screen.width, Screen.height) * .075f);
        public bool HitTrainingFunnel(Vector2 p)
        {
            if (!dialRt || !dialRt.gameObject.activeInHierarchy) return false;
            Rect r = ScreenRect(dialRt); r.xMin -= 24; r.xMax += 24; r.yMin -= 10; r.yMax += 34; return r.Contains(p);
        }

        public bool SelectionFromScreen(Vector2 screen, out int value, out bool valid, out bool precision)
        {
            value = shown == null ? 0 : shown.allowedMin; valid = false; precision = false;
            if (shown == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(dialRt, screen, null, out var local)) return false;
            Vector2 v = local - dial.Center; float radius = v.magnitude; float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; if (angle < 0) angle += 360f;
            float t = Mathf.Clamp01((160f - angle) / 140f); int index = Mathf.Clamp(Mathf.RoundToInt(t * 59f), 0, 59);
            value = shown.allowedMin + shown.allowedStep * index;
            float baseRadius = Mathf.Min(dialRt.rect.width * .39f, dialRt.rect.height * .53f);
            precision = radius >= baseRadius + 24f;
            valid = angle >= 12f && angle <= 168f && radius >= baseRadius * .48f && radius <= baseRadius * 1.65f;
            return true;
        }

        public void Tick(float dt)
        {
            if (playVisibilityPending)
            {
                Canvas.ForceUpdateCanvases();
                float cw = canvasRt.rect.width;
                float ch = canvasRt.rect.height;
                bool usable = Screen.width >= 360 && Screen.height >= 360 && cw >= 360f && ch >= 360f;
                bool stable = usable && playGateCanvasW > 0f && Mathf.Abs(cw - playGateCanvasW) <= .5f && Mathf.Abs(ch - playGateCanvasH) <= .5f;
                playStableLayoutFrames = stable ? playStableLayoutFrames + 1 : 0;
                playGateCanvasW = cw;
                playGateCanvasH = ch;
                Relayout(true);
                if (playStableLayoutFrames >= 2)
                {
                    playVisibilityPending = false;
                    playGroup.alpha = 1f;
                }
            }
            if (Screen.width != lastW || Screen.height != lastH || wide != MgfBridge.IsLandLayout ||
                Mathf.Abs(canvasRt.rect.width - lastCanvasW) > .5f || Mathf.Abs(canvasRt.rect.height - lastCanvasH) > .5f) Relayout(true);
            worldClock += dt;
            float ambient = Mathf.Sin(worldClock * .55f);
            if (hitStop > 0f) hitStop -= dt;
            else
            {
                float speed = Mathf.Lerp(7f, drumVelocity, Mathf.Clamp01(revealSpin));
                if (drum) drum.Rotate(0, speed * dt, 0, Space.Self);
            }
            drumVelocity = Mathf.Lerp(drumVelocity, revealing && revealCorrect ? 32f : 7f, 1f - Mathf.Exp(-dt * 2.2f));
            revealSpin = Mathf.MoveTowards(revealSpin, 0f, dt * .65f);
            for (int i = 0; i < filmPanels.Count; i++)
            {
                var t = filmPanels[i]; float alive = i < game.State.solved ? 1f : .22f;
                t.localScale = new Vector3(1f, 1f + Mathf.Sin(worldClock * 5f + i * .8f) * .025f * alive, 1f);
            }
            for (int i = 0; i < cords.Count; i++) if (cords[i]) cords[i].localRotation = cordBases[i] * Quaternion.Euler(0, 0, Mathf.Sin(worldClock * .8f + i) * (i < 3 ? 1.2f : 5f));
            if (titleBanner) titleBanner.localPosition = new Vector3(0, 8.8f + ambient * .06f, 2.7f);
            if (windChimes) windChimes.localRotation = Quaternion.Euler(0, 0, ambient * 5f + revealSpin * Mathf.Sin(worldClock * 11f) * 8f);

            if (titleGroup.gameObject.activeSelf)
            {
                float pulse = 1f + Mathf.Sin(worldClock * 2.8f) * .055f; titleStartRt.localScale = Vector3.one * pulse;
                titleRing.rectTransform.localScale = Vector3.one * (1.05f + Mathf.Sin(worldClock * 2.2f) * .10f);
            }
            if (shown != null && playGroup.gameObject.activeSelf)
            {
                guideClock += dt;
                if (game.IsTraining)
                {
                    int answerIndex = Mathf.Clamp((shown.answer - shown.allowedMin) / shown.allowedStep, 0, 59);
                    int i = Mathf.RoundToInt(Mathf.PingPong(guideClock * 16f, Mathf.Max(1, answerIndex)));
                    int ghostValue = shown.allowedMin + shown.allowedStep * i;
                    guideHandRt.anchoredPosition = dial.HandleLocal(ghostValue);
                    float gp = .88f + Mathf.Sin(guideClock * 5.5f) * .16f; guideHandRt.localScale = Vector3.one * gp;
                    handleRing.rectTransform.anchoredPosition = handleRt.anchoredPosition; handleRing.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(guideClock * 7f) * .14f);
                }
                else guideHandRt.gameObject.SetActive(false);
                if (refuseClock > 0f)
                {
                    refuseClock -= dt; float dx = Mathf.Sin(refuseClock * 72f) * 8f * Mathf.Clamp01(refuseClock * 4f); handleRt.anchoredPosition = dial.HandleLocal(game.State.selectedAngle) + new Vector2(dx, 0);
                }
                else PositionHandle();
                if (revealing)
                {
                    revealAmount = Mathf.MoveTowards(revealAmount, 1f, dt * 3.5f); dialFlash = Mathf.MoveTowards(dialFlash, 0f, dt * 1.8f);
                    triangle.Configure(shown.diagramRotation, shown.band, revealCorrect, revealAmount);
                }
                film.Configure(game.State.solved, worldClock, revealAmount, doublePrint);
                dial.Configure(shown.allowedMin, shown.allowedStep, game.State.selectedAngle, game.State.precision, validArc, dialFlash);
                PositionTriangleLabels();
                RefreshHud(false); SyncGeometry();
            }
            if (rippleClock > 0f)
            {
                rippleClock -= dt; float k = 1f - rippleClock / .46f; rippleRt.localScale = Vector3.one * Mathf.Lerp(.4f, 2.0f, k); Color c = ripple.color; c.a = 1f - k; ripple.color = c;
            }
            if (toastClock > 0f) { toastClock -= dt; if (toastClock <= 0f) toastText.transform.parent.gameObject.SetActive(false); }
            if (curtainClock > 0f)
            {
                curtainClock -= dt; float k = 1f - curtainClock / .42f; float eased = k * k * (3f - 2f * k);
                curtainLeftRt.anchoredPosition = new Vector2(-220f * eased, 0); curtainRightRt.anchoredPosition = new Vector2(220f * eased, 0);
                if (curtainClock <= 0f) { curtainLeft.gameObject.SetActive(false); curtainRight.gameObject.SetActive(false); }
            }
            if (endGroup.gameObject.activeSelf)
            {
                endClock += dt; int count = Mathf.RoundToInt(Mathf.Lerp(0, game.State.score, Mathf.Clamp01(endClock / 1.1f))); endCounter.text = count.ToString();
                float p = .96f + Mathf.Sin(worldClock * 3.2f) * .04f; restartRt.localScale = Vector3.one * p;
            }
        }

        void RefreshHud(bool force)
        {
            if (!playGroup.gameObject.activeSelf) return;
            progressText.text = "필름 " + game.State.solved + "/8";
            firstText.text = "첫 제출 " + game.State.firstCorrect + "/" + game.State.firstAttempts;
            timeText.text = game.IsTraining ? "연습" : Mathf.CeilToInt(game.Remaining) + "초";
            scoreShown = Mathf.Lerp(scoreShown, game.State.score, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 8f)); scoreText.text = Mathf.RoundToInt(scoreShown).ToString();
            for (int i = 0; i < brakes.Length; i++) brakes[i].color = i < game.State.lives ? MgfLook.Hex("#B89250") : new Color(ink.r, ink.g, ink.b, .17f);
            for (int i = 0; i < framePips.Length; i++) framePips[i].color = i < game.State.solved ? red : birch;
        }

        void PositionHandle()
        {
            if (!dial || shown == null) return;
            handleRt.anchoredPosition = dial.HandleLocal(game.State.selectedAngle); handleRing.rectTransform.anchoredPosition = handleRt.anchoredPosition;
            float sx = dragging ? 1.16f : 1f, sy = dragging ? .88f : 1f; handleRt.localScale = new Vector3(sx, sy, 1f);
        }

        void SyncGeometry()
        {
            if (!handleRt || !dialRt || !dialRt.gameObject.activeInHierarchy) return;
            Vector2 c = RectTransformUtility.WorldToScreenPoint(null, dialRt.TransformPoint(dial.Center));
            Vector2 h = HandleScreen();
            game.State.dialCenterX = Mathf.RoundToInt(c.x); game.State.dialCenterY = Mathf.RoundToInt(c.y);
            game.State.dialRadius = Mathf.RoundToInt(Vector2.Distance(c, RectTransformUtility.WorldToScreenPoint(null, dialRt.TransformPoint(dial.HandleLocal(shown.allowedMin)))));
            game.State.handleX = Mathf.RoundToInt(h.x); game.State.handleY = Mathf.RoundToInt(h.y);
            game.State.screenW = Screen.width; game.State.screenH = Screen.height; game.State.layoutLand = wide;
        }

        Vector2 HandleScreen() => RectTransformUtility.WorldToScreenPoint(null, handleRt.position);

        void Relayout(bool force)
        {
            lastW = Screen.width; lastH = Screen.height; wide = MgfBridge.IsLandLayout;
            lastCanvasW = canvasRt.rect.width; lastCanvasH = canvasRt.rect.height;
            if (cam)
            {
                cam.orthographicSize = wide ? 7.2f : 9.4f; cam.transform.position = wide ? new Vector3(0, 8.0f, -19f) : new Vector3(0, 10.8f, -21f); MgfLook.FitWidth(cam, wide ? 34f : 42f, .72f);
            }
            if (!dialRt) return;
            if (wide)
            {
                // CanvasScaler의 논리 캔버스는 같은 가로 화면이어도 높이에 따라 폭이 달라진다.
                // 화면 비율 앵커 대신 안전 폭 안에 좌/우 열을 만들고, 오른쪽을 발문-도형-다이얼 3구역으로 고정한다.
                float canvasW = canvasRt.rect.width;
                float canvasH = canvasRt.rect.height;
                float safeW = Mathf.Min(canvasW - 24f, 760f);
                float columnX = safeW * .32f;
                Vector2 center = new Vector2(.5f, .5f);

                SetBox(hudRt, center, new Vector2(270, 74), new Vector2(-columnX, canvasH * .5f - 45));
                SetBox(goalRt, center, new Vector2(270, 34), new Vector2(-columnX, canvasH * .5f - 101));
                SetBox(film.rectTransform, center, new Vector2(270, 140), new Vector2(-columnX, -25));

                SetBox(promptRt, center, new Vector2(270, 90), new Vector2(columnX, canvasH * .5f - 52));
                SetBox(triangleRt, center, new Vector2(250, 100), new Vector2(columnX, 40));
                SetBox(dialRt, center, new Vector2(270, 160), new Vector2(columnX, -canvasH * .5f + 90));

                SetBox(progressText.rectTransform, center, new Vector2(70, 32), new Vector2(-96, 10));
                SetBox(firstText.rectTransform, center, new Vector2(88, 32), new Vector2(-25, 10));
                SetBox(timeText.rectTransform, center, new Vector2(48, 32), new Vector2(50, 10));
                SetBox(scoreText.rectTransform, center, new Vector2(38, 32), new Vector2(106, 10));
                for (int i = 0; i < brakes.Length; i++) SetBox(brakes[i].rectTransform, center, new Vector2(12, 22), new Vector2(35 + i * 16, -22));
                SetBox(goalText.rectTransform, center, new Vector2(252, 28), Vector2.zero);
                SetBox(promptText.rectTransform, center, new Vector2(248, 64), new Vector2(0, 5));
                SetBox(conditionText.rectTransform, center, new Vector2(246, 18), new Vector2(0, -34));
                titleStartRt.anchorMin = titleStartRt.anchorMax = new Vector2(.5f, .30f);
            }
            else
            {
                SetBox(hudRt, new Vector2(.5f, 1f), new Vector2(382, 62), new Vector2(0, -32));
                SetBox(goalRt, new Vector2(.5f, 1f), new Vector2(370, 38), new Vector2(0, -82));
                SetBox(promptRt, new Vector2(.5f, .61f), new Vector2(374, 100), Vector2.zero);
                SetBox(triangleRt, new Vector2(.5f, .43f), new Vector2(310, 190), Vector2.zero);
                SetBox(dialRt, new Vector2(.5f, .17f), new Vector2(360, 250), Vector2.zero);
                SetBox(film.rectTransform, new Vector2(.5f, .79f), new Vector2(360, 106), Vector2.zero);
                SetBox(progressText.rectTransform, new Vector2(.12f, .5f), new Vector2(86, 40), Vector2.zero);
                SetBox(firstText.rectTransform, new Vector2(.39f, .5f), new Vector2(112, 40), Vector2.zero);
                SetBox(timeText.rectTransform, new Vector2(.63f, .5f), new Vector2(62, 40), Vector2.zero);
                SetBox(scoreText.rectTransform, new Vector2(.91f, .5f), new Vector2(66, 40), Vector2.zero);
                for (int i = 0; i < brakes.Length; i++) SetBox(brakes[i].rectTransform, new Vector2(.72f + i * .045f, .5f), new Vector2(12, 31), Vector2.zero);
                SetBox(goalText.rectTransform, new Vector2(.5f, .5f), new Vector2(350, 30), Vector2.zero);
                SetBox(promptText.rectTransform, new Vector2(.5f, .54f), new Vector2(352, 82), Vector2.zero);
                SetBox(conditionText.rectTransform, new Vector2(.5f, .10f), new Vector2(300, 20), Vector2.zero);
                titleStartRt.anchorMin = titleStartRt.anchorMax = new Vector2(.5f, .31f);
            }
            if (shown != null) { dial.SetVerticesDirty(); triangle.SetVerticesDirty(); PositionHandle(); SyncGeometry(); }
        }

        static void SetBox(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        void PositionTriangleLabels()
        {
            if (shown == null || triangleRt == null) return;
            Rect r = triangleRt.rect;
            Vector2 a = RotateDiagram(new Vector2(0, r.height * .35f));
            Vector2 b = RotateDiagram(new Vector2(-r.width * .32f, -r.height * .31f));
            Vector2 c = RotateDiagram(new Vector2(r.width * .32f, -r.height * .31f));
            labelA.rectTransform.anchoredPosition = a + (a.normalized == Vector2.zero ? Vector2.up : a.normalized) * 19f;
            labelB.rectTransform.anchoredPosition = b + (b.normalized == Vector2.zero ? Vector2.left : b.normalized) * 18f;
            labelC.rectTransform.anchoredPosition = c + (c.normalized == Vector2.zero ? Vector2.right : c.normalized) * 18f;
            labelD.rectTransform.anchoredPosition = (b + c) * .5f + Vector2.down * 17f;
        }

        Vector2 RotateDiagram(Vector2 p)
        {
            float a = shown.diagramRotation * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
        }

        static bool Contains(RectTransform rt, Vector2 p, float pad)
        {
            if (!rt || !rt.gameObject.activeInHierarchy) return false; Rect r = ScreenRect(rt); r.xMin -= pad; r.xMax += pad; r.yMin -= pad; r.yMax += pad; return r.Contains(p);
        }
        static Rect ScreenRect(RectTransform rt)
        {
            var corners = new Vector3[4]; rt.GetWorldCorners(corners); Vector2 a = RectTransformUtility.WorldToScreenPoint(null, corners[0]), b = RectTransformUtility.WorldToScreenPoint(null, corners[2]); return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }
        static void Set(CanvasGroup g, bool on) { g.gameObject.SetActive(on); g.alpha = on ? 1 : 0; g.blocksRaycasts = false; g.interactable = on; }
    }
}
