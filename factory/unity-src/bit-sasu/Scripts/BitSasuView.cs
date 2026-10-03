// 빛 사수 — 2.5D 해안 광학 디오라마와 평면 수학 UI.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.BitSasu
{
    public partial class BitSasuGame
    {
        static readonly Color Pale = MgfLook.Hex("#A6DCF0");
        static readonly Color Cobalt = MgfLook.Hex("#164B9B");
        static readonly Color Vermilion = MgfLook.Hex("#EF573F");
        static readonly Color Ivory = MgfLook.Hex("#F3F5EA");
        static readonly Color Slate = MgfLook.Hex("#54768D");
        static readonly Color Ink = MgfLook.Hex("#12345C");

        Camera cam;
        Transform lensPivot, lensBody, handle, targetRoot;
        Transform[] petals = new Transform[3];
        Transform[] fog = new Transform[2];
        Transform[] guards = new Transform[3];
        Renderer[] guardRenderers = new Renderer[3];
        LineRenderer[] cracks = new LineRenderer[3];
        Renderer[] railing = new Renderer[9];
        Transform[] waterBands = new Transform[7];
        LineRenderer beamCore, beamTrail;
        Material cobaltMat, paleMat, ivoryMat, redMat, slateMat, glassMat, brightGlassMat, crackedMat, offLightMat, onLightMat;
        Vector3 lensBaseScale;
        float lensTargetVisual;
        float worldClock;
        float handleJolt;
        float lastTickSound;
        bool land;

        CanvasGroup titleG, hudG, endG, toastG, splitG;
        RectTransform titleRt, hudRt, endRt, ctaRt, endCtaRt, titlePlateRt;
        TextMeshProUGUI[] logo = new TextMeshProUGUI[3];
        TextMeshProUGUI tagTxt, badgeTxt, bestTxt, ctaTxt;
        TextMeshProUGUI scoreTxt, progressTxt, livesTxt, timerTxt, goalTxt, promptTxt, angleTxt, feedbackTxt;
        TextMeshProUGUI tableHead, tableFunctionTxt, tablePageTxt;
        readonly TextMeshProUGUI[] tableRows = new TextMeshProUGUI[10];
        RectTransform tableRt, tableFunctionRt, tablePageRt, diagramRt, ratioRt;
        RectTransform goalBgRt, promptBgRt, diagramBgRt, tableBgRt;
        readonly RectTransform[] ratioChipRt = new RectTransform[4];
        readonly Image[] ratioChipBg = new Image[4];
        readonly TextMeshProUGUI[] ratioChipTxt = new TextMeshProUGUI[4];
        TextMeshProUGUI ratioResultTxt, diagramTitle, diagramFormula, diagramLabels;
        RectTransform guideRt, guidePathRt, handleControlRt, handleTrackRt, handleTargetRt;
        Image guideRing, guideFinger;
        Image handleControl, handleTargetRing;
        TextMeshProUGUI handleTargetTxt;
        RectTransform toastRt;
        TextMeshProUGUI toastTxt;
        readonly Image[] ripples = new Image[5];
        readonly float[] rippleT = { 9, 9, 9, 9, 9 };
        int rippleNext;
        Image[] lifeGlass = new Image[3];
        TextMeshProUGUI endTitle, endCount, endStats, endHint, endCtaTxt;
        RectTransform splitTop, splitBottom;
        Sprite roundSprite, ringSprite;
        float toastT, toastDuration;
        float splitT = -1f;
        float guideBoost;
        float displayedScore;
        int displayedScoreInt = -1;
        float displayedResult;
        float resultTarget;
        bool resultCounting;
        int lastSecond = -1;
        int layoutW = -1, layoutH = -1;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var im = rt.gameObject.AddComponent<Image>();
            im.color = color;
            im.sprite = sprite;
            im.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color,
            float width = 360f, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 1.65f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.text = text;
            return t;
        }

        static Sprite MakeRoundSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "OpticRing" : "OpticRound", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a;
                float fx = x + 0.5f, fy = y + 0.5f;
                if (ring)
                {
                    float dx = fx - size * 0.5f, dy = fy - size * 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float rr = size * 0.42f, w = size * 0.055f;
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - rr) / w);
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * 0.5f) - (size * 0.5f - radius), 0f);
                    float qy = Mathf.Max(Mathf.Abs(fy - size * 0.5f) - (size * 0.5f - radius), 0f);
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + 0.75f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            float b = ring ? 0 : radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        void BuildWorld()
        {
            // GameObject.CreatePrimitive(Cylinder)는 내부적으로 CapsuleCollider를 문자열로
            // 요청한다. WebGL managed stripping이 타입을 제거하지 않도록 명시 참조한다.
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<SphereCollider>();
            Destroy(colliderTypes);

            MgfLook.Sky(MgfLook.Hex("#DDF5FB"), Pale, MgfLook.Hex("#75B9D5"), 0.88f);
            MgfLook.Sun(new Vector3(48, -32, 18), new Color(1f, 0.96f, 0.82f), 1.15f, 0.58f);
            cam = MgfLook.Camera(new Vector3(8.8f, 8.2f, -11.5f), new Vector3(0, 1.4f, 3.0f), 38f);
            cam.orthographic = true;
            cam.orthographicSize = 7.7f;

            // 식각 계측기처럼 무광 에나멜과 서리 유리를 쓴다. 장난감형 고광택은 피한다.
            cobaltMat = MgfLook.Lit(Cobalt, 0.30f, 0.52f);
            paleMat = MgfLook.Lit(Pale, 0.24f, 0.03f);
            ivoryMat = MgfLook.Lit(Ivory, 0.22f, 0.07f);
            redMat = MgfLook.Lit(Vermilion, 0.28f, 0.24f);
            slateMat = MgfLook.Lit(Slate, 0.18f, 0.30f);
            glassMat = MgfLook.Lit(new Color(0.63f, 0.91f, 0.97f), 0.48f, 0.08f, Pale * 0.12f);
            brightGlassMat = MgfLook.Lit(new Color(0.80f, 0.98f, 1f), 0.62f, 0.05f, new Color(0.45f, 0.95f, 1f) * 1.35f);
            crackedMat = MgfLook.Lit(new Color(0.55f, 0.68f, 0.73f), 0.25f, 0.1f, Vermilion * 0.08f);
            offLightMat = MgfLook.Lit(new Color(0.48f, 0.66f, 0.72f), 0.4f, 0.1f);
            onLightMat = MgfLook.Lit(Ivory, 0.8f, 0.05f, new Color(0.35f, 0.92f, 1f) * 2f);

            var floor = MgfLook.Block("CoastalFloor", new Vector3(0, -0.45f, 3.5f), new Vector3(18, 0.8f, 22), 0.35f, paleMat);
            Destroy(floor.GetComponent<Collider>());
            for (int i = 0; i < waterBands.Length; i++)
            {
                var w = MgfLook.Block("WaterReflection" + i, new Vector3(-7f + i * 2.25f, 0.02f, 6.4f + (i % 2) * 0.7f),
                    new Vector3(1.45f, 0.025f, 9.5f), 0.18f, i % 2 == 0 ? glassMat : ivoryMat);
                Destroy(w.GetComponent<Collider>());
                waterBands[i] = w.transform;
            }

            MgfLook.Prim(PrimitiveType.Cylinder, "LighthouseBase", new Vector3(0, 0.15f, 0), new Vector3(3.15f, 0.42f, 3.15f), ivoryMat);
            MgfLook.Prim(PrimitiveType.Cylinder, "EnamelDeck", new Vector3(0, 0.62f, 0), new Vector3(2.68f, 0.20f, 2.68f), cobaltMat);
            MgfLook.Prim(PrimitiveType.Cylinder, "OpticColumn", new Vector3(0, 1.35f, 0), new Vector3(1.72f, 0.72f, 1.72f), ivoryMat);
            for (int i = 0; i < 24; i++)
            {
                float a = i * 15f * Mathf.Deg2Rad;
                var tick = MgfLook.Block("EtchedDeckTick" + i,
                    new Vector3(Mathf.Cos(a) * 1.73f, 0.80f, Mathf.Sin(a) * 1.73f),
                    new Vector3(i % 3 == 0 ? 0.08f : 0.045f, 0.035f, i % 3 == 0 ? 0.30f : 0.20f),
                    0.01f, i % 6 == 0 ? redMat : ivoryMat);
                tick.transform.rotation = Quaternion.Euler(0, -i * 15f, 0);
                Destroy(tick.GetComponent<Collider>());
            }

            lensPivot = new GameObject("LensPivot").transform;
            lensPivot.position = new Vector3(0, 2.55f, 0.25f);
            lensBody = new GameObject("LensAssembly").transform;
            lensBody.SetParent(lensPivot, false);
            var outer = MgfLook.Prim(PrimitiveType.Cylinder, "CobaltOpticRing", Vector3.zero, new Vector3(1.75f, 0.36f, 1.75f), cobaltMat, lensBody);
            outer.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var inner = MgfLook.Prim(PrimitiveType.Cylinder, "FrostedLens", new Vector3(0, 0, 0.42f), new Vector3(1.28f, 0.18f, 1.28f), glassMat, lensBody);
            inner.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var rim = MgfLook.Prim(PrimitiveType.Cylinder, "IvoryRim", new Vector3(0, 0, -0.38f), new Vector3(1.95f, 0.11f, 1.95f), ivoryMat, lensBody);
            rim.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var h = MgfLook.Block("RedLensHandle", new Vector3(1.72f, 0.03f, 0.16f), new Vector3(0.48f, 1.15f, 0.42f), 0.18f, redMat, lensBody);
            handle = h.transform;
            lensBaseScale = lensBody.localScale;

            for (int i = 0; i < guards.Length; i++)
            {
                float x = (i - 1) * 0.78f;
                var g = MgfLook.Block("GuardPlate" + i, new Vector3(x, 0.93f, -1.65f), new Vector3(0.62f, 0.22f, 0.88f), 0.12f, ivoryMat);
                guards[i] = g.transform;
                guardRenderers[i] = g.GetComponent<Renderer>();
                cracks[i] = MakeLine("Crack" + i, MgfLook.Unlit(Ink), 0.045f);
                cracks[i].positionCount = 3;
                cracks[i].SetPosition(0, new Vector3(x - 0.18f, 1.05f, -1.92f));
                cracks[i].SetPosition(1, new Vector3(x + 0.02f, 1.16f, -1.94f));
                cracks[i].SetPosition(2, new Vector3(x + 0.2f, 1.01f, -1.95f));
                cracks[i].gameObject.SetActive(false);
            }

            fog[0] = MgfLook.Block("FogUpper", new Vector3(0, 4.0f, 4.9f), new Vector3(10.5f, 4.4f, 0.42f), 0.28f, paleMat).transform;
            fog[1] = MgfLook.Block("FogLower", new Vector3(0, -0.55f, 4.9f), new Vector3(10.5f, 4.4f, 0.42f), 0.28f, paleMat).transform;

            targetRoot = new GameObject("OpticalTarget").transform;
            targetRoot.position = new Vector3(0, 2.55f, 8.4f);
            MgfLook.Prim(PrimitiveType.Cylinder, "TargetHub", Vector3.zero, new Vector3(0.62f, 0.28f, 0.62f), cobaltMat, targetRoot).transform.localRotation = Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f;
                var petal = MgfLook.Block("TargetPetal" + i, new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 0.72f,
                    Mathf.Sin(a * Mathf.Deg2Rad) * 0.72f, 0), new Vector3(0.8f, 1.5f, 0.18f), 0.35f, ivoryMat, targetRoot);
                petal.transform.localRotation = Quaternion.Euler(0, 0, -a + 90f);
                petals[i] = petal.transform;
            }
            targetRoot.gameObject.SetActive(false);

            beamTrail = MakeLine("BeamTrail", MgfLook.Additive(new Color(0.35f, 0.92f, 1f, 0.28f)), 0.36f);
            beamCore = MakeLine("BeamCore", MgfLook.Additive(Ivory), 0.12f);
            beamTrail.positionCount = beamCore.positionCount = 2;
            beamTrail.gameObject.SetActive(false); beamCore.gameObject.SetActive(false);

            for (int i = 0; i < railing.Length; i++)
            {
                var l = MgfLook.Prim(PrimitiveType.Sphere, "RailingLight" + i,
                    new Vector3(-5.6f + i * 1.4f, 0.42f, 10.2f), new Vector3(0.18f, 0.35f, 0.18f), offLightMat, null, false);
                railing[i] = l.GetComponent<Renderer>();
            }
            SetSelectedAngle(25, false);
        }

        LineRenderer MakeLine(string name, Material mat, float width)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.startWidth = lr.endWidth = width;
            lr.useWorldSpace = true;
            lr.numCapVertices = 4;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        void BuildUi()
        {
            var canvas = MgfText.Canvas;
            var root = canvas.transform;
            roundSprite = MakeRoundSprite(64, 13, false);
            ringSprite = MakeRoundSprite(96, 0, true);

            titleRt = R("Title", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one; titleRt.sizeDelta = Vector2.zero;
            titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            // 타이틀도 인게임과 같은 실시간 광학 데크를 그대로 보여 준다.
            // 생성 회화형 풀블리드 이미지는 사용하지 않는다.
            var titleWash = Img(titleRt, "TitleWash", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(Pale.r, Pale.g, Pale.b, 0.12f));
            titleWash.rectTransform.anchorMin = Vector2.zero; titleWash.rectTransform.anchorMax = Vector2.one; titleWash.rectTransform.sizeDelta = Vector2.zero;
            titlePlateRt = Img(titleRt, "TitleInstrumentPlate", new Vector2(0.5f, 0.79f), Vector2.zero,
                new Vector2(354, 190), new Color(Ivory.r, Ivory.g, Ivory.b, 0.88f), roundSprite).rectTransform;
            logo[0] = Txt(titleRt, "빛 사수", new Vector2(0.16f, 0.82f), new Vector2(5, -5), 58, Ivory, 330, TextAlignmentOptions.Left);
            logo[1] = Txt(titleRt, "빛 사수", new Vector2(0.16f, 0.82f), new Vector2(2, -2), 58, Vermilion, 330, TextAlignmentOptions.Left);
            logo[2] = Txt(titleRt, "빛 사수", new Vector2(0.16f, 0.82f), Vector2.zero, 58, Cobalt, 330, TextAlignmentOptions.Left);
            for (int i = 0; i < logo.Length; i++) { logo[i].fontStyle = FontStyles.Bold; logo[i].characterSpacing = 8f; }
            tagTxt = Txt(titleRt, "등대를 돌려 비춰라", new Vector2(0.17f, 0.73f), Vector2.zero, 18, Ink, 320, TextAlignmentOptions.Left);
            tagTxt.fontStyle = FontStyles.Bold;
            badgeTxt = Txt(titleRt, "중3 · 삼각비", new Vector2(0.17f, 0.68f), Vector2.zero, 13, Slate, 300, TextAlignmentOptions.Left);
            bestTxt = Txt(titleRt, "", new Vector2(0.5f, 0.16f), Vector2.zero, 13, Ink, 330);
            ctaRt = R("TitleCta", titleRt, new Vector2(0.5f, 0.075f), Vector2.zero, new Vector2(292, 70));
            Img(ctaRt, "CtaOuter", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(292, 70), Cobalt, roundSprite);
            Img(ctaRt, "CtaInner", new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(276, 54), Ivory, roundSprite);
            ctaTxt = Txt(ctaRt, "빛 켜기", new Vector2(0.5f, 0.5f), new Vector2(0, 2), 25, Cobalt, 260);
            ctaTxt.fontStyle = FontStyles.Bold;

            hudRt = R("Hud", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            hudRt.anchorMin = Vector2.zero; hudRt.anchorMax = Vector2.one; hudRt.sizeDelta = Vector2.zero;
            hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            scoreTxt = Txt(hudRt, "0", new Vector2(0.06f, 0.97f), Vector2.zero, 22, Cobalt, 96, TextAlignmentOptions.Left);
            scoreTxt.fontStyle = FontStyles.Bold;
            scoreTxt.rectTransform.pivot = new Vector2(0, 0.5f);
            progressTxt = Txt(hudRt, "0 / 9", new Vector2(0.5f, 0.97f), Vector2.zero, 16, Ink, 150);
            timerTxt = Txt(hudRt, "연습", new Vector2(0.94f, 0.97f), Vector2.zero, 18, Ink, 90, TextAlignmentOptions.Right);
            timerTxt.rectTransform.pivot = new Vector2(1, 0.5f);
            livesTxt = Txt(hudRt, "보호판", new Vector2(0.5f, 0.925f), new Vector2(-62, 0), 11, Slate, 90, TextAlignmentOptions.Right);
            for (int i = 0; i < lifeGlass.Length; i++)
                lifeGlass[i] = Img(hudRt, "Life" + i, new Vector2(0.5f, 0.925f), new Vector2(8 + i * 29, 0), new Vector2(22, 18), Ivory, roundSprite);

            var goalBg = Img(hudRt, "Goal", new Vector2(0.5f, 0.875f), Vector2.zero, new Vector2(350, 38), new Color(Ivory.r, Ivory.g, Ivory.b, 0.94f), roundSprite);
            goalBgRt = goalBg.rectTransform;
            goalTxt = Txt(goalBg.transform, "등대를 돌려 표적에 빛을 보내라", new Vector2(0.5f, 0.5f), Vector2.zero, 14, Cobalt, 330);
            goalTxt.fontStyle = FontStyles.Bold;
            var promptBg = Img(hudRt, "Prompt", new Vector2(0.5f, 0.80f), Vector2.zero, new Vector2(354, 82), new Color(Pale.r, Pale.g, Pale.b, 0.94f), roundSprite);
            promptBgRt = promptBg.rectTransform;
            promptTxt = Txt(promptBg.transform, "", new Vector2(0.5f, 0.5f), Vector2.zero, 13, Ink, 334);
            promptTxt.fontStyle = FontStyles.Bold;
            promptTxt.enableAutoSizing = true; promptTxt.fontSizeMin = 10; promptTxt.fontSizeMax = 13;
            promptTxt.rectTransform.sizeDelta = new Vector2(334, 68);
            promptTxt.overflowMode = TextOverflowModes.Overflow;

            diagramRt = R("Diagram", hudRt, new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(354, 118));
            diagramBgRt = Img(diagramRt, "DiagramBg", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(354, 118), new Color(Ivory.r, Ivory.g, Ivory.b, 0.92f), roundSprite).rectTransform;
            diagramTitle = Txt(diagramRt, "정면 측량도", new Vector2(0.08f, 0.83f), Vector2.zero, 12, Cobalt, 150, TextAlignmentOptions.Left);
            diagramTitle.rectTransform.pivot = new Vector2(0, 0.5f);
            diagramFormula = Txt(diagramRt, "", new Vector2(0.5f, 0.54f), Vector2.zero, 17, Ink, 330);
            diagramFormula.fontStyle = FontStyles.Bold;
            diagramLabels = Txt(diagramRt, "", new Vector2(0.5f, 0.23f), Vector2.zero, 12, Slate, 330);

            tableRt = R("Table", hudRt, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(354, 210));
            tableBgRt = Img(tableRt, "TableBg", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(354, 210), new Color(Pale.r, Pale.g, Pale.b, 0.96f), roundSprite).rectTransform;
            tableHead = Txt(tableRt, "삼각비표 · 자동 추천 없음", new Vector2(0.5f, 0.90f), Vector2.zero, 12, Cobalt, 330);
            tableFunctionRt = R("TableFunction", tableRt, new Vector2(0.17f, 0.76f), Vector2.zero, new Vector2(92, 36));
            Img(tableFunctionRt, "F", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92, 36), Cobalt, roundSprite);
            tableFunctionTxt = Txt(tableFunctionRt, "tan 열", new Vector2(0.5f, 0.5f), Vector2.zero, 13, Ivory, 86);
            tablePageRt = R("TablePage", tableRt, new Vector2(0.79f, 0.76f), Vector2.zero, new Vector2(124, 36));
            Img(tablePageRt, "P", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(124, 36), Ivory, roundSprite);
            tablePageTxt = Txt(tablePageRt, "45°-74°", new Vector2(0.5f, 0.5f), Vector2.zero, 13, Cobalt, 118);
            for (int i = 0; i < tableRows.Length; i++)
            {
                tableRows[i] = Txt(tableRt, "", new Vector2(0.5f, 0.64f), new Vector2(0, -i * 13.8f), 10.5f, Ink, 330);
                tableRows[i].fontStyle = FontStyles.Bold;
                tableRows[i].characterSpacing = -1f;
            }

            ratioRt = R("RatioTool", diagramRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340, 110));
            for (int i = 0; i < ratioChipRt.Length; i++)
            {
                ratioChipRt[i] = R("RatioChip" + i, ratioRt, new Vector2(0.13f + i * 0.245f, 0.68f), Vector2.zero, new Vector2(76, 38));
                ratioChipBg[i] = Img(ratioChipRt[i], "B", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76, 38), Ivory, roundSprite);
                ratioChipTxt[i] = Txt(ratioChipRt[i], "", new Vector2(0.5f, 0.5f), Vector2.zero, 11, Ink, 70);
                ratioChipTxt[i].fontStyle = FontStyles.Bold;
            }
            ratioResultTxt = Txt(ratioRt, "선을 끌어 분자 ÷ 분모를 구성", new Vector2(0.5f, 0.20f), Vector2.zero, 12, Slate, 330);

            angleTxt = Txt(hudRt, "25°", new Vector2(0.5f, 0.16f), Vector2.zero, 42, Vermilion, 220);
            angleTxt.fontStyle = FontStyles.Bold;
            feedbackTxt = Txt(hudRt, "붉은 손잡이를 잡고 위아래로 끈 뒤 놓으시오", new Vector2(0.5f, 0.105f), Vector2.zero, 13, Ink, 350);

            handleTrackRt = R("LensHandleTrack", hudRt, new Vector2(0.5f, 0.24f), new Vector2(0, 44), new Vector2(8, 118));
            Img(handleTrackRt, "EtchedTrack", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 118),
                new Color(Cobalt.r, Cobalt.g, Cobalt.b, 0.52f), roundSprite);
            handleTargetRt = R("PracticeTarget45", hudRt, new Vector2(0.5f, 0.24f), new Vector2(0, 88), new Vector2(84, 50));
            handleTargetRing = Img(handleTargetRt, "TargetRing", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(50, 50), new Color(Vermilion.r, Vermilion.g, Vermilion.b, 0.82f), ringSprite);
            handleTargetTxt = Txt(handleTargetRt, "45° 놓기", new Vector2(0.5f, 0.5f), new Vector2(0, 38), 12, Cobalt, 92, TextAlignmentOptions.Center);
            handleTargetTxt.fontStyle = FontStyles.Bold;
            handleControlRt = R("LensHandleControl", hudRt, new Vector2(0.5f, 0.24f), Vector2.zero, new Vector2(68, 86));
            Img(handleControlRt, "HandleBezel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62, 82), Cobalt, roundSprite);
            handleControl = Img(handleControlRt, "RedHandle", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42, 66), Vermilion, roundSprite);

            guideRt = R("Guide", hudRt, new Vector2(0.5f, 0.24f), Vector2.zero, new Vector2(130, 180));
            guidePathRt = R("GuidePath", guideRt, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(5, 80));
            Img(guidePathRt, "Path", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5, 80), new Color(Vermilion.r, Vermilion.g, Vermilion.b, 0.55f), roundSprite);
            guideRing = Img(guideRt, "Ring", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(82, 82), new Color(Vermilion.r, Vermilion.g, Vermilion.b, 0.8f), ringSprite);
            guideFinger = Img(guideRt, "Finger", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 42), Ivory, roundSprite);

            toastRt = R("Toast", hudRt, new Vector2(0.5f, 0.06f), Vector2.zero, new Vector2(340, 46));
            Img(toastRt, "ToastBg", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340, 46), new Color(Cobalt.r, Cobalt.g, Cobalt.b, 0.94f), roundSprite);
            toastTxt = Txt(toastRt, "", new Vector2(0.5f, 0.5f), Vector2.zero, 13, Ivory, 320);
            toastG = toastRt.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0;

            for (int i = 0; i < ripples.Length; i++)
            {
                ripples[i] = Img(hudRt, "Ripple" + i, Vector2.zero, Vector2.zero, new Vector2(56, 56), new Color(Vermilion.r, Vermilion.g, Vermilion.b, 0.6f), ringSprite);
                ripples[i].rectTransform.pivot = new Vector2(0.5f, 0.5f);
                ripples[i].enabled = false;
            }

            endRt = R("End", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            endRt.anchorMin = Vector2.zero; endRt.anchorMax = Vector2.one; endRt.sizeDelta = Vector2.zero;
            endG = endRt.gameObject.AddComponent<CanvasGroup>();
            Img(endRt, "EndHalo", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(354, 440), new Color(Pale.r, Pale.g, Pale.b, 0.96f), roundSprite);
            endTitle = Txt(endRt, "", new Vector2(0.5f, 0.68f), Vector2.zero, 34, Cobalt, 330);
            endTitle.fontStyle = FontStyles.Bold;
            endCount = Txt(endRt, "0", new Vector2(0.5f, 0.56f), Vector2.zero, 72, Vermilion, 320);
            endCount.fontStyle = FontStyles.Bold;
            endStats = Txt(endRt, "", new Vector2(0.5f, 0.45f), Vector2.zero, 17, Ink, 320);
            endHint = Txt(endRt, "", new Vector2(0.5f, 0.34f), Vector2.zero, 14, Slate, 310);
            endCtaRt = R("EndCta", endRt, new Vector2(0.5f, 0.20f), Vector2.zero, new Vector2(286, 68));
            Img(endCtaRt, "O", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(286, 68), Cobalt, roundSprite);
            endCtaTxt = Txt(endCtaRt, "다시 조준", new Vector2(0.5f, 0.5f), Vector2.zero, 24, Ivory, 270);
            endCtaTxt.fontStyle = FontStyles.Bold;

            var splitRt = R("Split", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            splitRt.anchorMin = Vector2.zero; splitRt.anchorMax = Vector2.one; splitRt.sizeDelta = Vector2.zero;
            splitG = splitRt.gameObject.AddComponent<CanvasGroup>(); splitG.alpha = 0;
            splitTop = Img(splitRt, "TopShutter", new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(0, 0), Pale).rectTransform;
            splitTop.anchorMin = new Vector2(0, 0.5f); splitTop.anchorMax = Vector2.one; splitTop.sizeDelta = Vector2.zero;
            splitBottom = Img(splitRt, "BottomShutter", new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(0, 0), Pale).rectTransform;
            splitBottom.anchorMin = Vector2.zero; splitBottom.anchorMax = new Vector2(1, 0.5f); splitBottom.sizeDelta = Vector2.zero;

            LayoutUi(true);
            RefreshTable();
        }

        void LayoutUi(bool force = false)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform canvasRt = (RectTransform)MgfText.Canvas.transform;
            float logicalW = Mathf.Max(1f, canvasRt.rect.width);
            float logicalH = Mathf.Max(1f, canvasRt.rect.height);
            // Screen.width의 물리 픽셀은 고해상도 세로 태블릿을 가로로 오판한다.
            // CanvasScaler가 만든 논리 캔버스의 실제 종횡비만 사용한다.
            bool nextLand = logicalW / logicalH >= 1.05f;
            if (!force && nextLand == land && layoutW == Screen.width && layoutH == Screen.height) return;
            land = nextLand;
            layoutW = Screen.width; layoutH = Screen.height;
            if (land)
            {
                float cw = logicalW;
                float ch = logicalH;
                float panelW = Mathf.Clamp(cw * 0.29f, 210f, 300f);
                float diagramH = Mathf.Clamp(ch * 0.42f, 170f, 240f);
                float tableH = Mathf.Clamp(ch * 0.54f, 220f, 310f);
                float safeMargin = 18f;
                float panelX = cw * 0.5f - safeMargin - panelW * 0.5f;
                float middleGap = Mathf.Max(240f, panelX * 2f - panelW - 28f);
                float titleW = Mathf.Clamp(cw * 0.47f, 320f, 390f);
                float titleX = -cw * 0.5f + safeMargin + titleW * 0.5f;
                // 낮은 가로 화면(허브 카드 1200×630 등)에서도 판이 위로 잘리지 않게 실제 높이로 배치한다.
                float pc = Mathf.Min(0.78f * ch, ch - 18f - 92f);
                for (int i = 0; i < logo.Length; i++)
                {
                    logo[i].rectTransform.anchorMin = logo[i].rectTransform.anchorMax = new Vector2(0.5f, 0f);
                    logo[i].rectTransform.anchoredPosition = new Vector2(titleX + (i == 0 ? 5f : i == 1 ? 2f : 0f), pc + 30f + (i == 0 ? -5f : i == 1 ? -2f : 0f));
                    logo[i].rectTransform.sizeDelta = new Vector2(titleW - 30f, 98);
                    logo[i].alignment = TextAlignmentOptions.Left;
                }
                titlePlateRt.anchorMin = titlePlateRt.anchorMax = new Vector2(0.5f, 0f);
                titlePlateRt.anchoredPosition = new Vector2(titleX, pc);
                titlePlateRt.sizeDelta = new Vector2(titleW, 184);
                tagTxt.rectTransform.anchorMin = tagTxt.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                tagTxt.rectTransform.anchoredPosition = new Vector2(titleX, pc - 38f);
                tagTxt.rectTransform.sizeDelta = new Vector2(titleW - 30f, 36);
                tagTxt.alignment = TextAlignmentOptions.Left;
                badgeTxt.rectTransform.anchorMin = badgeTxt.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                badgeTxt.rectTransform.anchoredPosition = new Vector2(titleX, pc - 68f);
                badgeTxt.rectTransform.sizeDelta = new Vector2(titleW - 30f, 28);
                badgeTxt.alignment = TextAlignmentOptions.Left;
                bestTxt.rectTransform.anchorMin = bestTxt.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                bestTxt.rectTransform.anchoredPosition = new Vector2(0, 0.075f * ch + 35f + 14f);
                diagramRt.anchorMin = diagramRt.anchorMax = new Vector2(0.5f, 0.5f);
                diagramRt.anchoredPosition = new Vector2(-panelX, ch * 0.03f);
                diagramRt.sizeDelta = diagramBgRt.sizeDelta = new Vector2(panelW, diagramH);
                diagramTitle.rectTransform.anchoredPosition = new Vector2(14f, 0);
                diagramTitle.rectTransform.sizeDelta = new Vector2(panelW - 20f, 26f);
                diagramFormula.rectTransform.sizeDelta = new Vector2(panelW - 16f, 48f);
                diagramLabels.rectTransform.sizeDelta = new Vector2(panelW - 16f, 58f);
                ratioRt.sizeDelta = new Vector2(panelW - 8f, diagramH - 8f);
                float chipW = (panelW - 28f) / 4f;
                for (int i = 0; i < ratioChipRt.Length; i++)
                {
                    ratioChipRt[i].sizeDelta = new Vector2(chipW, 38f);
                    ratioChipBg[i].rectTransform.sizeDelta = new Vector2(chipW, 38f);
                    ratioChipTxt[i].rectTransform.sizeDelta = new Vector2(chipW - 4f, 26f);
                }
                ratioResultTxt.rectTransform.sizeDelta = new Vector2(panelW - 18f, 44f);
                tableRt.anchorMin = tableRt.anchorMax = new Vector2(0.5f, 0.5f);
                tableRt.anchoredPosition = new Vector2(panelX, ch * 0.02f);
                tableRt.sizeDelta = tableBgRt.sizeDelta = new Vector2(panelW, tableH);
                tableHead.rectTransform.sizeDelta = new Vector2(panelW - 12f, 26f);
                for (int i = 0; i < tableRows.Length; i++) tableRows[i].rectTransform.sizeDelta = new Vector2(panelW - 12f, 20f);
                float fnW = panelW * 0.33f, pgW = panelW * 0.42f;
                tableFunctionRt.sizeDelta = tableFunctionRt.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(fnW, 36f);
                tableFunctionRt.anchorMin = tableFunctionRt.anchorMax = new Vector2(0.22f, 0.76f);
                tableFunctionTxt.rectTransform.sizeDelta = new Vector2(fnW - 4f, 24f);
                tablePageRt.sizeDelta = tablePageRt.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(pgW, 36f);
                tablePageRt.anchorMin = tablePageRt.anchorMax = new Vector2(0.74f, 0.76f);
                tablePageTxt.rectTransform.sizeDelta = new Vector2(pgW - 4f, 24f);
                // 좌우 패널 사이 중앙 무대 폭 안에서만 발문을 조판한다.
                promptBgRt.sizeDelta = new Vector2(Mathf.Clamp(middleGap, 240f, 400f), 78f);
                promptTxt.rectTransform.sizeDelta = new Vector2(promptBgRt.sizeDelta.x - 24f, 64f);
                promptBgRt.anchorMin = promptBgRt.anchorMax = new Vector2(0.5f, 0.72f);
                promptBgRt.anchoredPosition = Vector2.zero;
                goalBgRt.anchorMin = goalBgRt.anchorMax = new Vector2(0.5f, 0.88f);
                progressTxt.rectTransform.anchorMin = progressTxt.rectTransform.anchorMax = new Vector2(0.42f, 0.97f);
                livesTxt.rectTransform.anchorMin = livesTxt.rectTransform.anchorMax = new Vector2(0.58f, 0.97f);
                for (int i = 0; i < lifeGlass.Length; i++) lifeGlass[i].rectTransform.anchorMin = lifeGlass[i].rectTransform.anchorMax = new Vector2(0.58f, 0.97f);
                angleTxt.rectTransform.anchorMin = angleTxt.rectTransform.anchorMax = new Vector2(0.5f, 0.13f);
                angleTxt.rectTransform.anchoredPosition = Vector2.zero;
                angleTxt.fontSize = 38f;
                feedbackTxt.rectTransform.anchorMin = feedbackTxt.rectTransform.anchorMax = new Vector2(0.5f, 0.035f);
                feedbackTxt.fontSize = 11.5f;
                // 손잡이는 등대 오른쪽, 오른쪽 삼각비표 패널 왼쪽 가장자리보다 안쪽에 둔다.
                float handleX = Mathf.Clamp(panelX - panelW * 0.5f - 48f, 90f, 150f);
                SetHandleUiAnchor(new Vector2(0.5f, 0.31f), new Vector2(handleX, 0));
            }
            else
            {
                for (int i = 0; i < logo.Length; i++)
                {
                    logo[i].rectTransform.anchorMin = logo[i].rectTransform.anchorMax = new Vector2(0.5f, 0.82f);
                    logo[i].rectTransform.anchoredPosition = new Vector2(i == 0 ? 5f : i == 1 ? 2f : 0f, i == 0 ? -5f : i == 1 ? -2f : 0f);
                    logo[i].rectTransform.sizeDelta = new Vector2(350, 98);
                    logo[i].alignment = TextAlignmentOptions.Center;
                }
                tagTxt.rectTransform.anchorMin = tagTxt.rectTransform.anchorMax = new Vector2(0.5f, 0.73f);
                tagTxt.rectTransform.anchoredPosition = Vector2.zero;
                tagTxt.rectTransform.sizeDelta = new Vector2(340, 36);
                tagTxt.alignment = TextAlignmentOptions.Center;
                badgeTxt.rectTransform.anchorMin = badgeTxt.rectTransform.anchorMax = new Vector2(0.5f, 0.68f);
                badgeTxt.rectTransform.anchoredPosition = Vector2.zero;
                badgeTxt.rectTransform.sizeDelta = new Vector2(340, 28);
                badgeTxt.alignment = TextAlignmentOptions.Center;
                titlePlateRt.anchorMin = titlePlateRt.anchorMax = new Vector2(0.5f, 0.78f);
                titlePlateRt.anchoredPosition = Vector2.zero;
                bestTxt.rectTransform.anchorMin = bestTxt.rectTransform.anchorMax = new Vector2(0.5f, 0.16f);
                bestTxt.rectTransform.anchoredPosition = Vector2.zero;
                titlePlateRt.sizeDelta = new Vector2(354, 205);
                // 목표 → 발문 → 도식을 위에서부터 실제 높이로 쌓는다(비율 앵커는 낮은 태블릿 캔버스에서 겹쳤다).
                float goalY = 0.875f * logicalH;
                float promptTop = goalY - 19f - 4f;
                float diagramTop = promptTop - 94f - 6f;
                diagramRt.anchorMin = diagramRt.anchorMax = new Vector2(0.5f, 0f);
                diagramRt.anchoredPosition = new Vector2(0, diagramTop - 50f);
                diagramRt.sizeDelta = new Vector2(354, 100);
                diagramBgRt.sizeDelta = new Vector2(354, 100);
                diagramTitle.rectTransform.anchoredPosition = Vector2.zero;
                diagramTitle.rectTransform.sizeDelta = new Vector2(320, 20);
                diagramFormula.rectTransform.sizeDelta = new Vector2(330, 28);
                diagramLabels.rectTransform.sizeDelta = new Vector2(330, 22);
                ratioRt.sizeDelta = new Vector2(340, 110);
                for (int i = 0; i < ratioChipRt.Length; i++)
                {
                    ratioChipRt[i].sizeDelta = new Vector2(76, 38);
                    ratioChipBg[i].rectTransform.sizeDelta = new Vector2(76, 38);
                    ratioChipTxt[i].rectTransform.sizeDelta = new Vector2(70, 18);
                }
                ratioResultTxt.rectTransform.sizeDelta = new Vector2(330, 20);
                // 삼각비표는 화면 아래 계기판으로 내려 가운데 창을 등대·표적 장면에 돌려준다.
                // 바닥 기준으로 붙여 태블릿처럼 논리 높이가 낮은 화면에서도 안내 문장과 겹치지 않게 한다.
                tableRt.anchorMin = tableRt.anchorMax = new Vector2(0.5f, 0f);
                tableRt.anchoredPosition = new Vector2(0, 0.055f * logicalH + 102f + 6f);
                tableRt.sizeDelta = new Vector2(354, 204);
                tableBgRt.sizeDelta = new Vector2(354, 204);
                tableHead.rectTransform.sizeDelta = new Vector2(330, 20);
                for (int i = 0; i < tableRows.Length; i++) tableRows[i].rectTransform.sizeDelta = new Vector2(330, 18);
                tableFunctionRt.sizeDelta = tableFunctionRt.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(92, 36);
                tableFunctionRt.anchorMin = tableFunctionRt.anchorMax = new Vector2(0.17f, 0.76f);
                tableFunctionTxt.rectTransform.sizeDelta = new Vector2(86, 22);
                tablePageRt.sizeDelta = tablePageRt.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(124, 36);
                tablePageRt.anchorMin = tablePageRt.anchorMax = new Vector2(0.79f, 0.76f);
                tablePageTxt.rectTransform.sizeDelta = new Vector2(118, 22);
                promptBgRt.sizeDelta = new Vector2(354, 94);
                promptTxt.rectTransform.sizeDelta = new Vector2(336, 84);
                promptBgRt.anchorMin = promptBgRt.anchorMax = new Vector2(0.5f, 0f);
                promptBgRt.anchoredPosition = new Vector2(0, promptTop - 47f);
                goalBgRt.anchorMin = goalBgRt.anchorMax = new Vector2(0.5f, 0.875f);
                progressTxt.rectTransform.anchorMin = progressTxt.rectTransform.anchorMax = new Vector2(0.5f, 0.97f);
                livesTxt.rectTransform.anchorMin = livesTxt.rectTransform.anchorMax = new Vector2(0.5f, 0.925f);
                for (int i = 0; i < lifeGlass.Length; i++) lifeGlass[i].rectTransform.anchorMin = lifeGlass[i].rectTransform.anchorMax = new Vector2(0.5f, 0.925f);
                angleTxt.rectTransform.anchorMin = angleTxt.rectTransform.anchorMax = new Vector2(0.22f, 0f);
                angleTxt.rectTransform.anchoredPosition = new Vector2(0, diagramTop - 100f - 30f);
                angleTxt.fontSize = 38f;
                feedbackTxt.rectTransform.anchorMin = feedbackTxt.rectTransform.anchorMax = new Vector2(0.5f, 0.035f);
                feedbackTxt.fontSize = 12f;
                float tableTop = 0.055f * logicalH + 210f;
                float handleY = Mathf.Max(0.385f * logicalH, tableTop + 58f);
                SetHandleUiAnchor(new Vector2(0.5f, handleY / logicalH), new Vector2(128f, 0));
            }
            scoreTxt.rectTransform.anchorMin = scoreTxt.rectTransform.anchorMax = new Vector2(0.025f, 0.97f);
            timerTxt.rectTransform.anchorMin = timerTxt.rectTransform.anchorMax = new Vector2(land ? 0.91f : 0.79f, 0.97f);
        }

        // 손잡이는 등대 옆(가로/세로 모두 오른쪽)에 둬서 렌즈 몸통을 가리지 않는다.
        void SetHandleUiAnchor(Vector2 anchor, Vector2 offset)
        {
            handleControlRt.anchorMin = handleControlRt.anchorMax = anchor;
            handleControlRt.anchoredPosition = offset;
            handleTrackRt.anchorMin = handleTrackRt.anchorMax = anchor;
            handleTrackRt.anchoredPosition = offset + new Vector2(0, 44f);
            handleTargetRt.anchorMin = handleTargetRt.anchorMax = anchor;
            handleTargetRt.anchoredPosition = offset + new Vector2(0, 88f);
            guideRt.anchorMin = guideRt.anchorMax = anchor;
            guideRt.anchoredPosition = offset;
        }

        void UpdateWorld(float dt)
        {
            worldClock += dt;
            LayoutUi();
            // 세로 플레이는 패널 사이의 가운데 창(화면 36~64%)에 등대와 표적이 함께 들어오도록
            // 직교 화각을 넓힌다. 패널이 장면 전체를 덮어 세계가 사라지던 결함의 수정이다.
            cam.orthographicSize = land ? 6.6f : (phase == Phase.Title ? 8.2f : 10.6f);
            cam.transform.position = land ? new Vector3(8.8f, 8.1f, -11.8f) : new Vector3(8.4f, 9.2f, -12.4f);
            cam.transform.LookAt(land ? new Vector3(0, 1.5f, 3.5f) : new Vector3(0, 1.5f, 3.1f));

            float visual = phase == Phase.Title ? 45f + Mathf.Sin(worldClock * 0.55f) * 3f : lensTargetVisual;
            float tilt = -(visual - 45f) * 0.62f;
            lensPivot.localRotation = Quaternion.Slerp(lensPivot.localRotation, Quaternion.Euler(tilt, 0, 0), 1f - Mathf.Exp(-dt * 14f));
            float j = handleJolt > 0 ? Mathf.Sin(handleJolt * 95f) * handleJolt * 0.45f : 0f;
            if (handleJolt > 0) handleJolt = Mathf.Max(0, handleJolt - dt);
            handle.localPosition = new Vector3(1.72f + j, 0.03f, 0.16f);

            for (int i = 0; i < waterBands.Length; i++)
            {
                var p = waterBands[i].position;
                p.y = 0.02f + Mathf.Sin(worldClock * 0.7f + i * 0.8f) * 0.018f;
                waterBands[i].position = p;
            }

            if (phase == Phase.Title)
            {
                beamTrail.gameObject.SetActive(true); beamCore.gameObject.SetActive(true);
                Vector3 from = lensPivot.position + lensPivot.forward * 1.6f;
                Vector3 end = new Vector3(Mathf.Sin(worldClock * 0.42f) * 1.15f,
                    2.65f + Mathf.Sin(worldClock * 0.57f) * 0.34f, 8.4f);
                beamTrail.SetPosition(0, from); beamTrail.SetPosition(1, end);
                beamCore.SetPosition(0, from); beamCore.SetPosition(1, end);
            }
            else if (phase == Phase.Shot)
            {
                float t = shotT;
                float extend = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.08f) / 0.28f));
                Vector3 from = lensPivot.position + lensPivot.forward * 1.6f;
                float miss = shotCorrect ? 0f : Mathf.Clamp((st.selectedAngle - current.angle) * 0.11f, -2.7f, 2.7f);
                Vector3 end = new Vector3(0, 2.55f + miss, 8.4f);
                Vector3 now = Vector3.Lerp(from, end, extend);
                beamTrail.SetPosition(0, from); beamTrail.SetPosition(1, now);
                beamCore.SetPosition(0, from); beamCore.SetPosition(1, now);
                float squash = t < 0.09f ? 0.86f : 1f + Mathf.Sin(Mathf.Clamp01((t - 0.09f) / 0.24f) * Mathf.PI) * 0.08f;
                lensBody.localScale = new Vector3(lensBaseScale.x * squash, lensBaseScale.y * (2f - squash), lensBaseScale.z);
                if (shotCorrect)
                {
                    float split = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.20f) / 0.36f));
                    fog[0].position = Vector3.Lerp(new Vector3(0, 4.0f, 4.9f), new Vector3(0, 7.8f, 4.9f), split);
                    fog[1].position = Vector3.Lerp(new Vector3(0, -0.55f, 4.9f), new Vector3(0, -4.5f, 4.9f), split);
                    targetRoot.gameObject.SetActive(t > 0.26f);
                    for (int i = 0; i < petals.Length; i++)
                    {
                        float u = Mathf.Clamp01((t - 0.34f - i * 0.12f) / 0.25f);
                        float overshoot = u < 0.8f ? Mathf.SmoothStep(0, 1.08f, u / 0.8f) : Mathf.Lerp(1.08f, 1f, (u - 0.8f) / 0.2f);
                        petals[i].localScale = new Vector3(1f, Mathf.Max(0.05f, overshoot), 1f);
                        petals[i].GetComponent<Renderer>().sharedMaterial = u > 0.5f ? brightGlassMat : ivoryMat;
                    }
                }
            }
            else
            {
                beamTrail.gameObject.SetActive(false); beamCore.gameObject.SetActive(false);
                lensBody.localScale = Vector3.Lerp(lensBody.localScale, lensBaseScale, 1f - Mathf.Exp(-dt * 12f));
                if (phase != Phase.Title)
                {
                    float breathe = Mathf.Sin(worldClock * 0.65f) * 0.04f;
                    fog[0].position = new Vector3(0, 4.0f + breathe, 4.9f);
                    fog[1].position = new Vector3(0, -0.55f - breathe, 4.9f);
                }
            }
        }

        void UpdateUi(float dt)
        {
            displayedScore = Mathf.MoveTowards(displayedScore, st.score, dt * 360f);
            int ds = Mathf.RoundToInt(displayedScore);
            if (ds != displayedScoreInt) { displayedScoreInt = ds; scoreTxt.text = ds.ToString(); }
            int sec = Mathf.Max(0, Mathf.CeilToInt(runLeft));
            if (sec != lastSecond)
            {
                lastSecond = sec;
                timerTxt.text = phase == Phase.Practice ? "연습" : sec + "초";
            }
            if (toastT > 0)
            {
                toastT -= dt;
                float fade = Mathf.Min(1f, Mathf.Min(toastT * 5f, (toastDuration - toastT) * 8f));
                toastG.alpha = fade;
            }
            else toastG.alpha = 0;

            for (int i = 0; i < ripples.Length; i++)
            {
                if (rippleT[i] > 1f || !ripples[i].enabled) continue;
                rippleT[i] += dt * 3.8f;
                float u = rippleT[i];
                ripples[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.35f, 2.2f, u);
                var c = ripples[i].color; c.a = 0.65f * (1f - u); ripples[i].color = c;
                if (u >= 1f) ripples[i].enabled = false;
            }

            if (splitT >= 0)
            {
                splitT += dt;
                splitG.alpha = 1f;
                float u = Mathf.SmoothStep(0, 1, Mathf.Clamp01(splitT / 0.45f));
                splitTop.anchoredPosition = new Vector2(0, u * 460f);
                splitBottom.anchoredPosition = new Vector2(0, -u * 460f);
                if (u >= 1f) { splitT = -1; splitG.alpha = 0; }
            }

            PositionGuide();
            if (guideRt.gameObject.activeSelf)
            {
                float pulse = 1f + (0.08f + guideBoost * 0.08f) * Mathf.Sin(worldClock * (4f + guideBoost * 2f));
                guideRing.rectTransform.localScale = Vector3.one * pulse;
                guideFinger.rectTransform.anchoredPosition = new Vector2(0, Mathf.PingPong(worldClock * 66f, 80f));
                handleTargetRing.rectTransform.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(worldClock * 4.8f));
                if (guideBoost > 0) guideBoost = Mathf.Max(0, guideBoost - dt * 0.18f);
            }

            if (resultCounting)
            {
                displayedResult = Mathf.MoveTowards(displayedResult, resultTarget, dt * 10f);
                endCount.text = Mathf.RoundToInt(displayedResult) + " / 9";
                if (Mathf.Abs(displayedResult - resultTarget) < 0.01f) resultCounting = false;
            }
            if (phase == Phase.Title)
            {
                float p = 1f + Mathf.Max(0, Mathf.Sin(worldClock * 2.2f - 1f)) * 0.025f;
                ctaRt.localScale = Vector3.one * p;
            }
        }

        void SetScreen()
        {
            bool title = phase == Phase.Title;
            bool end = phase == Phase.End;
            titleG.alpha = title ? 1 : 0; titleG.blocksRaycasts = title;
            hudG.alpha = !title && !end ? 1 : 0; hudG.blocksRaycasts = !title && !end;
            endG.alpha = end ? 1 : 0; endG.blocksRaycasts = end;
            guideRt.gameObject.SetActive(phase == Phase.Practice);
            handleControlRt.gameObject.SetActive(!title && !end);
            handleTrackRt.gameObject.SetActive(!title && !end);
            handleTargetRt.gameObject.SetActive(phase == Phase.Practice);
            ratioRt.gameObject.SetActive(!title && !end && current != null && current.band >= 2);
            diagramTitle.gameObject.SetActive(land || current == null || current.band < 2);
            diagramFormula.gameObject.SetActive(current == null || current.band < 2);
            diagramLabels.gameObject.SetActive(current == null || current.band < 2);
            tableRt.gameObject.SetActive(!title && !end);
            if (title)
                bestTxt.text = bestScore > 0 ? "최고 첫 시도 " + bestFirst + " / 9  ·  " + bestScore + "점" : "120초 · 표적 9개 · 보호판 3장";
            RefreshLives();
            LayoutUi(true);
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            promptTxt.text = current.prompt;
            progressTxt.text = st.solved + " / " + BitSasuRules.TargetCount;
            goalTxt.text = phase == Phase.Practice ? "45°로 맞추고 손을 놓아 빛을 보내라" : "등대를 돌려 표적에 빛을 보내라";
            feedbackTxt.text = phase == Phase.Practice ? "tan A≈1.000인 행을 찾고 붉은 손잡이를 45°까지 끄시오" : "현재 각도를 정한 뒤 손을 놓으면 한 발이 나간다";
            diagramTitle.text = current.band == 3
                ? "∠A = 렌즈 수평선 ~ 꼭대기 방향"
                : "△ABC · ∠C=90° · 기준각 A";
            if (current.band == 0)
            {
                diagramFormula.text = "tan A = BC / AC = 1";
                diagramLabels.text = "일반식  sin A=BC/AB   cos A=AC/AB   tan A=BC/AC";
            }
            else if (current.band == 1)
            {
                diagramFormula.text = BitSasuRules.TrigName(current.trig) + " A ≈ " + BitSasuRules.Thousand(current.tableValue);
                diagramLabels.text = "표에서 가장 가까운 1° 행을 찾으시오";
            }
            else
            {
                SetupRatioChips();
            }
            RefreshTable();
            RefreshAngleUi();
            RefreshHudImmediate();
        }

        void SetupRatioChips()
        {
            if (current.band == 2)
            {
                ratioChipTxt[0].text = "BC " + current.p * current.scale;
                ratioChipTxt[1].text = "AC " + current.q * current.scale;
                ratioChipTxt[2].text = "AB " + current.r * current.scale;
                ratioChipTxt[3].text = "";
                ratioChipRt[3].gameObject.SetActive(false);
                ratioResultTxt.text = BitSasuRules.TrigName(current.trig) + " A에 맞게 분자에서 분모로 선을 끄시오";
            }
            else
            {
                ratioChipTxt[0].text = "H " + current.totalHeight;
                ratioChipTxt[1].text = "e " + current.eye;
                ratioChipTxt[2].text = "H−e";
                ratioChipTxt[3].text = "d " + current.distance;
                ratioChipRt[3].gameObject.SetActive(true);
                ratioResultTxt.text = "먼저 H→e로 높이 차를 확인하고, (H−e)→d를 이으시오";
            }
            for (int i = 0; i < ratioChipBg.Length; i++) ratioChipBg[i].color = Ivory;
        }

        void RefreshHudImmediate()
        {
            progressTxt.text = st.solved + " / " + BitSasuRules.TargetCount;
            RefreshLives();
        }

        void RefreshLives()
        {
            if (lifeGlass[0] == null) return;
            for (int i = 0; i < lifeGlass.Length; i++)
            {
                bool cracked = i < st.crackedPlates;
                lifeGlass[i].color = cracked ? new Color(Vermilion.r, Vermilion.g, Vermilion.b, 0.62f) : Ivory;
                if (guards[i])
                {
                    guardRenderers[i].sharedMaterial = cracked ? crackedMat : ivoryMat;
                    cracks[i].gameObject.SetActive(cracked);
                }
            }
        }

        void RefreshAngleUi()
        {
            if (angleTxt) angleTxt.text = st.selectedAngle + "°";
        }

        void RefreshTable()
        {
            if (tableRows[0] == null) return;
            TrigKind kind = st.tableFunction == "sin" ? TrigKind.Sin : st.tableFunction == "cos" ? TrigKind.Cos : TrigKind.Tan;
            tableFunctionTxt.text = st.tableFunction + " 열";
            int start = st.tablePage == 0 ? 15 : 45;
            tablePageTxt.text = st.tablePage == 0 ? "15°-44°" : "45°-74°";
            for (int row = 0; row < 10; row++)
            {
                int a0 = start + row, a1 = start + 10 + row, a2 = start + 20 + row;
                tableRows[row].text = Cell(kind, a0) + "     " + Cell(kind, a1) + "     " + Cell(kind, a2);
            }
        }

        string Cell(TrigKind kind, int angle) => angle + "° " + BitSasuRules.Thousand(BitSasuRules.Table(kind, angle));

        void SetLensAngle(int angle)
        {
            lensTargetVisual = angle;
        }

        void TickLens()
        {
            if (Time.unscaledTime - lastTickSound > 0.045f)
            {
                lastTickSound = Time.unscaledTime;
                MgfSfx.Play("tap", 0.11f);
            }
        }

        public const float HandleUnitsPerDegree = 4.4f;

        // 붉은 손잡이 자체가 손가락을 따라 레일 위를 움직인다. 각도 변화량과 같은 비율이라
        // 연습에서 손잡이가 45° 눈금 링에 닿는 순간이 곧 45°다.
        void FollowHandle(int deltaDegrees)
        {
            float y = Mathf.Clamp(deltaDegrees * HandleUnitsPerDegree, -60f, 230f);
            handleControlRt.GetChild(0).localPosition = new Vector3(0, y, 0);
            handleControlRt.GetChild(1).localPosition = new Vector3(0, y, 0);
        }

        void HandleGrab(bool grabbed)
        {
            if (!grabbed) FollowHandle(0);
            if (grabbed)
            {
                handle.localScale = new Vector3(1.18f, 0.88f, 1.18f);
                guideRt.gameObject.SetActive(false);
            }
            else handle.localScale = Vector3.one;
        }

        void UpdateHandleTrail()
        {
            // 현재 각도는 즉시 갱신한다. 잔상은 렌즈 유리의 밝기 펄스로만 표현한다.
            lensBody.GetChild(1).GetComponent<Renderer>().sharedMaterial = brightGlassMat;
        }

        void BeginShotVisual(bool correct, bool wasFirst)
        {
            beamCore.gameObject.SetActive(true); beamTrail.gameObject.SetActive(true);
            targetRoot.gameObject.SetActive(false);
            lensBody.GetChild(1).GetComponent<Renderer>().sharedMaterial = brightGlassMat;
            MgfSfx.Play("whoosh", 0.46f);
            if (!correct)
            {
                MgfFx.Shake(cam, 0.075f, 0.18f);
                handleJolt = 0.22f;
                ShowToast(st.selectedAngle > current.angle ? "빛이 위로 지나갔다 · 기준각과 비를 확인하시오" : "빛이 아래로 지나갔다 · 표의 행을 확인하시오", 1.15f);
            }
            else
            {
                ShowToast(current.band == 0 ? "tan A=1, 표에서 A≈45°" : "적중 · " + ExplainCurrent(), 1.18f);
            }
        }

        string ExplainCurrent()
        {
            if (current.band == 1) return BitSasuRules.TrigName(current.trig) + " A≈" + BitSasuRules.Thousand(current.tableValue) + " → 약 " + current.angle + "°";
            if (current.band == 2)
            {
                BitSasuRules.RatioFor(current.trig, current.p, current.q, current.r, out int n, out int d);
                return BitSasuRules.TrigName(current.trig) + " A=" + n + "/" + d + " → 약 " + current.angle + "°";
            }
            return "tan A=(" + current.totalHeight + "−" + current.eye + ")/" + current.distance + " → 약 " + current.angle + "°";
        }

        void CompleteShotVisual(bool correct)
        {
            beamCore.gameObject.SetActive(false); beamTrail.gameObject.SetActive(false);
            lensBody.GetChild(1).GetComponent<Renderer>().sharedMaterial = glassMat;
            lensBody.localScale = lensBaseScale;
            if (correct)
            {
                MgfSfx.Play("correct", 0.52f);
                MgfFx.Glow(new Vector3(0, 2.7f, 8.2f), Pale, shotWasFirst ? 14 : 8, 0.38f);
            }
            else MgfSfx.Play("wrong", 0.38f);
            targetRoot.gameObject.SetActive(false);
            fog[0].position = new Vector3(0, 4.0f, 4.9f);
            fog[1].position = new Vector3(0, -0.55f, 4.9f);
        }

        void ResetWorldForRun()
        {
            for (int i = 0; i < railing.Length; i++) railing[i].sharedMaterial = offLightMat;
            for (int i = 0; i < guards.Length; i++) { guardRenderers[i].sharedMaterial = ivoryMat; cracks[i].gameObject.SetActive(false); }
            beamCore.gameObject.SetActive(false); beamTrail.gameObject.SetActive(false);
            targetRoot.gameObject.SetActive(false);
            displayedScore = 0; displayedScoreInt = -1;
        }

        void ResetWorldForProblem()
        {
            beamCore.gameObject.SetActive(false); beamTrail.gameObject.SetActive(false);
            targetRoot.gameObject.SetActive(false);
            for (int i = 0; i < petals.Length; i++) { petals[i].localScale = Vector3.one; petals[i].GetComponent<Renderer>().sharedMaterial = ivoryMat; }
        }

        void LightRailing(int tier)
        {
            int count = Mathf.Min(railing.Length, tier * 3);
            for (int i = 0; i < count; i++) railing[i].sharedMaterial = onLightMat;
            MgfSfx.Play("correct", 0.34f);
        }

        void ShowPracticeCorrection()
        {
            feedbackTxt.text = "tan A≈1.000인 행은 45° · 같은 손잡이를 다시 조준하시오";
            guideRt.gameObject.SetActive(true);
            guideBoost = 1f;
        }

        void ShowWrongReason(string id)
        {
            // 방어적으로도 1단계에는 화면에서 관찰 가능한 표 행 피드백만 남긴다.
            if (current != null && current.band == 1)
                id = st.selectedAngle > current.angle ? "aimed_too_high" : "aimed_too_low";
            string msg = id == "swap_opposite_adjacent" ? "기준각 A에서 대변과 이웃변을 바꾸어 잡았다"
                : id == "tan_uses_hypotenuse" ? "tan의 분모에는 빗변이 아니라 이웃변을 쓴다"
                : id == "uses_total_height" ? "전체 높이에서 렌즈 높이를 먼저 빼야 한다"
                : id == "aimed_too_high" ? "선택한 표의 행이 너무 크다"
                : "선택한 표의 행이 너무 작다";
            string correction = "올바른 표 행은 A≈" + current.angle + "°";
            feedbackTxt.text = msg + " · " + correction + " · 같은 표적을 다시 조준하시오";
            ShowToast(msg + " · " + correction, 1.1f);
        }

        void ShowEnd(string reason)
        {
            bool clear = reason == "clear";
            endTitle.text = clear ? "해안 광로 개방" : reason == "mastery" ? "표적은 열었지만" : "렌즈 보호 종료";
            resultTarget = st.firstAttemptCorrect;
            displayedResult = 0; resultCounting = true;
            endCount.text = "0 / 9";
            endStats.text = "첫 시도 적중  " + st.firstAttemptCorrect + " / " + st.firstAttemptTotal + "\n점수  " + st.score + "  ·  남은 보호판 " + Math.Max(0, st.lives);
            if (clear) endHint.text = "기준각을 먼저 확인한 조준이 정확했다";
            else if (st.misconceptionId == "uses_total_height") endHint.text = "다음 판: 전체 높이에서 렌즈 높이를 먼저 빼자";
            else if (st.misconceptionId == "swap_opposite_adjacent") endHint.text = "다음 판: 기준각 A의 대변·이웃변부터 확인하자";
            else endHint.text = "다음 판: 표를 찾기 전에 사용할 삼각비를 먼저 정하자";
            endCtaTxt.text = "다시 조준";
        }

        void PressTitle()
        {
            ctaRt.localScale = Vector3.one * 0.92f;
            MgfSfx.Play("whoosh", 0.35f);
            StartPractice();
        }

        void PressEnd()
        {
            endCtaRt.localScale = Vector3.one * 0.92f;
            MgfSfx.Play("tap", 0.32f);
            StartPractice();
        }

        void BeginSplitReveal()
        {
            splitT = 0f; splitG.alpha = 1f;
            splitTop.anchoredPosition = splitBottom.anchoredPosition = Vector2.zero;
        }

        void ReplayGuide()
        {
            idleGuide = 0;
            guideBoost = Mathf.Min(2f, guideBoost + 0.75f);
            guideRt.gameObject.SetActive(true);
            feedbackTxt.text = "붉은 손잡이를 잡아 위로 80px 끌고 45°에서 놓으시오";
        }

        void PositionGuide()
        {
            // 유령 손·경로·도착 눈금은 실제 입력 RectTransform과 같은 앵커를 공유한다.
            // 레이아웃이나 카메라가 바뀌어도 허공을 가리키지 않는다.
            if (!guideRt.gameObject.activeSelf || !handleControlRt) return;
            guideRt.anchorMin = guideRt.anchorMax = handleControlRt.anchorMin;
            guideRt.anchoredPosition = handleControlRt.anchoredPosition;
        }

        bool IsHandleZone(Vector2 screen)
        {
            if (handleControlRt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(handleControlRt, screen, null)) return true;
            float nx = screen.x / Mathf.Max(1, Screen.width), ny = screen.y / Mathf.Max(1, Screen.height);
            // 연습에서는 화면 아래 계측 데크 전체를 손잡이의 드래그 레일로 쓴다.
            // 탭만으로는 위 Game 코드가 발사하지 않으며, 실제 드래그가 있어야 제출된다.
            if (phase == Phase.Practice)
                return nx >= 0.12f && nx <= 0.88f && ny >= 0.08f && ny <= 0.62f;
            // 본 게임에서는 손잡이 주변(레일 포함)만 잡힌다. 위치는 실제 손잡이 RectTransform에서 읽는다.
            if (!handleControlRt.gameObject.activeInHierarchy) return false;
            Vector2 hp = RectTransformUtility.WorldToScreenPoint(null, handleControlRt.position);
            float k = MgfText.Canvas.scaleFactor;
            Vector2 d = (screen - hp) / Mathf.Max(0.01f, k);
            return Mathf.Abs(d.x) <= 64f && d.y >= -70f && d.y <= 110f;
        }

        bool HitTableFunction(Vector2 p) => tableFunctionRt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(tableFunctionRt, p, null);
        bool HitTablePage(Vector2 p) => tablePageRt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(tablePageRt, p, null);

        int HitRatioChip(Vector2 p)
        {
            if (!ratioRt.gameObject.activeInHierarchy) return -1;
            for (int i = 0; i < ratioChipRt.Length; i++)
                if (ratioChipRt[i].gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(ratioChipRt[i], p, null)) return i;
            return -1;
        }

        void BeginRatioDrag(int chip)
        {
            for (int i = 0; i < ratioChipBg.Length; i++) ratioChipBg[i].color = i == chip ? Vermilion : Ivory;
            ratioResultTxt.text = RatioChipId(chip) + "에서 다른 길이로 이어 보시오";
        }

        string RatioChipId(int chip)
        {
            if (current == null) return "";
            if (current.band == 2) return chip == 0 ? "BC" : chip == 1 ? "AC" : chip == 2 ? "AB" : "";
            return chip == 0 ? "H" : chip == 1 ? "e" : chip == 2 ? "H-e" : chip == 3 ? "d" : "";
        }

        int RatioChipValue(int chip)
        {
            if (current.band == 2) return chip == 0 ? current.p * current.scale : chip == 1 ? current.q * current.scale : current.r * current.scale;
            return chip == 0 ? current.totalHeight : chip == 1 ? current.eye : chip == 2 ? current.totalHeight - current.eye : current.distance;
        }

        void ShowRatioResult(int from, int to)
        {
            for (int i = 0; i < ratioChipBg.Length; i++) ratioChipBg[i].color = (i == from || i == to) ? Vermilion : Ivory;
            if (current.band == 3 && from == 0 && to == 1)
            {
                ratioResultTxt.text = "높이 차 H−e = " + current.totalHeight + "−" + current.eye + " = " + (current.totalHeight - current.eye) + " cm";
                return;
            }
            int n = RatioChipValue(from), d = RatioChipValue(to);
            int thousand = d == 0 ? 0 : (int)Math.Round((double)n * 1000 / d);
            ratioResultTxt.text = RatioChipId(from) + " / " + RatioChipId(to) + " = " + n + " / " + d + " ≈ " + BitSasuRules.Thousand(thousand);
        }

        void RefuseInput(string message)
        {
            SpawnRipple(MgfPointer.Position);
            if (handleControlRt && handleControlRt.gameObject.activeInHierarchy)
            {
                Vector2 hp = RectTransformUtility.WorldToScreenPoint(null, handleControlRt.position);
                SpawnRipple(hp);
            }
            handleJolt = 0.24f;
            guideRt.gameObject.SetActive(phase == Phase.Practice);
            guideBoost = Mathf.Min(2f, guideBoost + 0.3f);
            ShowToast(message, 0.85f);
            MgfSfx.Play("wrong", 0.16f);
        }

        void SpawnRipple(Vector2 screen)
        {
            var im = ripples[rippleNext++ % ripples.Length];
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hudRt, screen, null, out var local);
            im.rectTransform.anchoredPosition = local;
            im.rectTransform.localScale = Vector3.one * 0.35f;
            im.color = new Color(Vermilion.r, Vermilion.g, Vermilion.b, 0.65f);
            im.enabled = true;
            int idx = Array.IndexOf(ripples, im);
            rippleT[idx] = 0f;
        }

        void ShowToast(string message, float duration)
        {
            toastTxt.text = message;
            toastDuration = duration;
            toastT = duration;
            toastG.alpha = 1;
        }
    }
}
