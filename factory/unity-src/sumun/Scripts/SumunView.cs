// 수문 — 월드·UI. 한낮 삼각 여수로: 젖은 댐 콘크리트 + 녹슨 수평 철 수문.
// 회화 배경 없음. 최빈 픽셀은 #87A0B0(부팅 1회 베이크한 골재+거푸집). 정답에 화면 흔들림 없음.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace Mgf.Sumun
{
    class Ink
    {
        public readonly Mesh mesh = new Mesh();
        readonly List<Vector3> v = new List<Vector3>(1024);
        readonly List<Color32> c = new List<Color32>(1024);
        readonly List<int> t = new List<int>(2048);
        public float z;
        public Ink(float z) { this.z = z; mesh.MarkDynamic(); }
        public void Clear() { v.Clear(); c.Clear(); t.Clear(); }
        Vector3 P(Vector2 p) => new Vector3(p.x, p.y, z);
        public void Seg(Vector2 a, Vector2 b, float w, Color32 col)
        {
            var d = b - a; float L = d.magnitude; if (L < 1e-4f) return;
            var n = new Vector2(-d.y, d.x) / L * (w * 0.5f);
            int i = v.Count;
            v.Add(P(a - n)); v.Add(P(a + n)); v.Add(P(b + n)); v.Add(P(b - n));
            c.Add(col); c.Add(col); c.Add(col); c.Add(col);
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }
        public void Tri(Vector2 a, Vector2 b, Vector2 d, Color32 col)
        {
            int i = v.Count;
            v.Add(P(a)); v.Add(P(b)); v.Add(P(d)); c.Add(col); c.Add(col); c.Add(col);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }
        public void Disc(Vector2 o, float r, Color32 col, int n = 16)
        {
            int i0 = v.Count; v.Add(P(o)); c.Add(col);
            for (int k = 0; k <= n; k++) { float a = k * Mathf.PI * 2 / n; v.Add(P(o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r)); c.Add(col); }
            for (int k = 0; k < n; k++) { t.Add(i0); t.Add(i0 + 1 + k); t.Add(i0 + 2 + k); }
        }
        public void Dashed(Vector2 a, Vector2 b, float dash, float gap, float w, Color32 col, float phase)
        {
            var d = b - a; float L = d.magnitude; if (L < 1e-3f) return; d /= L;
            float per = dash + gap, s = -((phase % per) + per) % per;
            for (; s < L; s += per) { float s0 = Mathf.Max(0, s), s1 = Mathf.Min(L, s + dash); if (s1 > s0) Seg(a + d * s0, a + d * s1, w, col); }
        }
        public void Chevron(Vector2 at, Vector2 dir, float size, float w, Color32 col)
        {
            dir.Normalize(); var n = new Vector2(-dir.y, dir.x);
            Seg(at - dir * size * 0.5f + n * size * 0.55f, at + dir * size * 0.5f, w, col);
            Seg(at - dir * size * 0.5f - n * size * 0.55f, at + dir * size * 0.5f, w, col);
        }
        public void Apply()
        {
            mesh.Clear();
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(80, 80, 20));
        }
    }

    public partial class SumunGame
    {
        static readonly Color Conc = Hex("87A0B0"), Rust = Hex("8C4A32"), WaterC = Hex("1F5A78"), FoamC = Hex("D8E6EE"), Iron = Hex("5C5854"), Sand = Hex("E8E0D0");
        static Color Hex(string h) => MgfLook.Hex(h);
        static Color32 C32(Color c, float a = 1f) { var l = c.linear; return new Color32((byte)(l.r * 255), (byte)(l.g * 255), (byte)(l.b * 255), (byte)(Mathf.Clamp01(a) * 255)); }

        Camera cam;
        Transform world, sluiceT, ghostTform, fixedT, weightT, dumpTform, fingerT, pulseGo, playCol;
        Transform[] foam = new Transform[8];
        Transform pinL, pinR;
        readonly Transform[] hudPins = new Transform[3];
        MeshFilter tickF, waterF, dumpF, guideF;
        Ink ticks, waterInk, dumpInk, guideInk;
        Material matConc, matRust, matWater, matDump, matInk, matAdd;
        Texture2D concTex, sluiceTex;
        TextMeshPro[] vLbl = new TextMeshPro[6];
        TextMeshPro ratioLbl, lenLbl, logoW;
        Vector3 A, B, C, D, E, Gpt, midD;
        Vector3 sluiceBaseScale = new Vector3(1, 0.38f, 0.7f);

        CanvasGroup hudG, titleG, endG, toastG;
        TextMeshProUGUI timeTxt, scoreTxt, comboTxt, promptTxt, goalTxt, bestTxt, tagTxt, badgeTxt, hintTxt, toastTxt;
        TextMeshProUGUI endHead, endStats, endCtaTxt, logoUi;
        readonly Image[] pinImg = new Image[3];
        Image toastBg, timeFill, sheetFill, ctaPlate;
        RectTransform titleRt, endRt, bandRt;

        bool demo;
        float demoPhase, pinSpin, dumpY, sluiceSquash = 1f, ghostAlpha;
        Vector3 camHome, lookHome;
        int brokenVis;

        void BuildWorld()
        {
            MgfLook.Quality(28f);
            MgfLook.Sky(Hex("9AADBA"), Hex("C5D0D6"), Hex("6E8490"), 0.95f);
            MgfLook.Sun(new Vector3(52f, -18f, 0), Hex("F2EDE4"), 1.05f, 0.48f);
            cam = MgfLook.Camera(new Vector3(0, 4.2f, -13.2f), new Vector3(0, 3.4f, 0), 32f);
            camHome = cam.transform.position; lookHome = new Vector3(0, 3.4f, 0);

            concTex = BakeConcrete();
            sluiceTex = Resources.Load<Texture2D>("Sumun/sluice");
            matConc = MgfLook.Lit(Conc, 0.12f, 0.02f, Hex("1A2428"), concTex);
            matRust = sluiceTex
                ? MgfLook.Lit(new Color(1f, 0.95f, 0.9f), 0.28f, 0.45f, Hex("2A1208"), sluiceTex)
                : MgfLook.Lit(Rust, 0.28f, 0.5f, Hex("2A1208"));
            matWater = MgfLook.Alpha(new Color(WaterC.r, WaterC.g, WaterC.b, 0.55f));
            matDump = MgfLook.Alpha(new Color(WaterC.r * 1.1f, WaterC.g * 1.15f, WaterC.b, 0.72f));
            matInk = MgfLook.Unlit(Color.white);
            matAdd = MgfLook.Additive(FoamC);

            world = new GameObject("World").transform;
            MgfLook.Block("Apron", new Vector3(0, -0.55f, 2.2f), new Vector3(22, 0.7f, 10), 0.12f, matConc, world);
            MgfLook.Block("Basin", new Vector3(0, -0.15f, 0.6f), new Vector3(14, 0.35f, 4.2f), 0.08f, MgfLook.Lit(Hex("6F8796"), 0.18f, 0.04f), world);
            MgfLook.Block("Silt", new Vector3(0, -0.02f, 1.4f), new Vector3(11, 0.12f, 2.4f), 0.05f, MgfLook.Lit(Sand, 0.08f), world);
            MgfLook.Block("Back", new Vector3(0, 5.6f, 3.4f), new Vector3(16, 8, 0.8f), 0.1f, matConc, world);
            MgfLook.Block("WingL", new Vector3(-8.4f, 3.2f, 1.6f), new Vector3(3.2f, 7, 5), 0.1f, matConc, world);
            MgfLook.Block("WingR", new Vector3(8.4f, 3.2f, 1.6f), new Vector3(3.2f, 7, 5), 0.1f, matConc, world);

            sluiceT = MgfLook.Block("Sluice", Vector3.zero, new Vector3(4, 0.38f, 0.7f), 0.05f, matRust, world).transform;
            sluiceBaseScale = sluiceT.localScale;
            ghostTform = MgfLook.Block("SluiceGhost", Vector3.zero, new Vector3(4, 0.28f, 0.5f), 0.04f, MgfLook.Alpha(new Color(1, 1, 1, 0.35f)), world).transform;
            ghostTform.gameObject.SetActive(false);
            fixedT = MgfLook.Block("SluiceFixed", Vector3.zero, new Vector3(4, 0.32f, 0.6f), 0.04f, MgfLook.Lit(Hex("6A3A28"), 0.2f, 0.4f), world).transform;
            fixedT.gameObject.SetActive(false);
            pinL = MgfLook.Prim(PrimitiveType.Cylinder, "PinL", new Vector3(-1.6f, 0, -0.4f), new Vector3(0.12f, 0.28f, 0.12f), MgfLook.Lit(Hex("7A4A32"), 0.35f, 0.6f), sluiceT).transform;
            pinR = MgfLook.Prim(PrimitiveType.Cylinder, "PinR", new Vector3(1.6f, 0, -0.4f), new Vector3(0.12f, 0.28f, 0.12f), MgfLook.Lit(Hex("7A4A32"), 0.35f, 0.6f), sluiceT).transform;
            weightT = MgfLook.Prim(PrimitiveType.Sphere, "Weight", Vector3.zero, Vector3.one * 0.55f, MgfLook.Lit(Hex("4A3028"), 0.4f, 0.7f, Hex("1A0C08")), world).transform;
            weightT.gameObject.SetActive(false);

            ticks = new Ink(-0.04f); waterInk = new Ink(0.02f); dumpInk = new Ink(0.06f); guideInk = new Ink(-0.02f);
            tickF = MakeMr("Ticks", ticks.mesh, matInk, world);
            waterF = MakeMr("Water", waterInk.mesh, matWater, world);
            dumpF = MakeMr("Dump", dumpInk.mesh, matDump, world);
            guideF = MakeMr("Guide", guideInk.mesh, matInk, world);
            dumpTform = dumpF.transform;

            for (int i = 0; i < foam.Length; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Foam" + i;
                Object.Destroy(q.GetComponent<Collider>());
                q.GetComponent<Renderer>().sharedMaterial = matAdd;
                q.transform.SetParent(world, false);
                q.transform.localScale = Vector3.one * (0.35f + (i % 3) * 0.12f);
                foam[i] = q.transform;
            }

            fingerT = MakeFinger();
            pulseGo = MgfLook.Prim(PrimitiveType.Cylinder, "Pulse", Vector3.zero, new Vector3(0.9f, 0.02f, 0.9f), MgfLook.Alpha(new Color(1, 1, 1, 0.4f)), world).transform;
            pulseGo.gameObject.SetActive(false);

            playCol = MgfLook.Block("Play", new Vector3(0, 3.2f, 0.2f), new Vector3(12, 9, 1.2f), 0.05f, MgfLook.Alpha(new Color(0, 0, 0, 0)), world).transform;
            var pc = playCol.GetComponent<Renderer>(); if (pc) pc.enabled = false;

            string[] names = { "A", "B", "C", "D", "E", "G" };
            for (int i = 0; i < 6; i++)
            {
                vLbl[i] = MgfText.World(names[i], Vector3.zero, 3.4f, Sand, world);
                vLbl[i].fontStyle = FontStyles.Bold;
                vLbl[i].outlineWidth = 0.18f; vLbl[i].outlineColor = new Color(0.12f, 0.14f, 0.16f, 0.9f);
            }
            ratioLbl = MgfText.World("1:1", Vector3.zero, 3.2f, Sand, world);
            ratioLbl.fontStyle = FontStyles.Bold;
            ratioLbl.outlineWidth = 0.22f; ratioLbl.outlineColor = new Color(0.1f, 0.08f, 0.06f, 0.95f);
            ratioLbl.alignment = TextAlignmentOptions.Center;
            lenLbl = MgfText.World("", Vector3.zero, 2.35f, Sand, world);
            lenLbl.alignment = TextAlignmentOptions.Center;
            logoW = MgfText.World("수문", Vector3.zero, 7.2f, Hex("E8E0D0"), world);
            logoW.fontStyle = FontStyles.Bold;
            logoW.outlineWidth = 0.28f; logoW.outlineColor = new Color(0.12f, 0.06f, 0.04f, 1f);
        }

        static MeshFilter MakeMr(string n, Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject(n);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
            return mf;
        }

        Transform MakeFinger()
        {
            var root = new GameObject("Finger").transform;
            root.SetParent(world, false);
            var palm = MgfLook.Prim(PrimitiveType.Sphere, "Palm", new Vector3(0, 0, 0), Vector3.one * 0.42f, MgfLook.Lit(Hex("E8D5C4"), 0.45f), root);
            MgfLook.Prim(PrimitiveType.Capsule, "Idx", new Vector3(0.05f, 0.38f, 0), new Vector3(0.16f, 0.28f, 0.16f), MgfLook.Lit(Hex("E8D5C4"), 0.45f), root);
            root.gameObject.SetActive(false);
            return root;
        }

        static Texture2D BakeConcrete()
        {
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGB24, false) { name = "Conc", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[S * S];
            var rng = new System.Random(11);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float n = (float)rng.NextDouble();
                    float agg = n * 0.09f;
                    bool seam = (x % 64 < 2) || (y % 88 < 2);
                    float k = seam ? 0.82f : 0.96f + agg;
                    px[y * S + x] = new Color(0.529f * k, 0.627f * k, 0.690f * k);
                }
            tex.SetPixels(px); tex.Apply(false, true);
            return tex;
        }

        void BuildUi()
        {
            var canvas = MgfText.Canvas;
            hudG = Cg("Hud", canvas.transform, 1);
            titleG = Cg("Title", canvas.transform, 0);
            endG = Cg("End", canvas.transform, 0);
            toastG = Cg("Toast", canvas.transform, 0);

            var bandGo = new GameObject("Pband", typeof(RectTransform));
            bandGo.transform.SetParent(hudG.transform, false);
            bandRt = (RectTransform)bandGo.transform;
            bandRt.anchorMin = new Vector2(0f, 1f);
            bandRt.anchorMax = new Vector2(1f, 1f);
            bandRt.pivot = new Vector2(0.5f, 1f);
            bandRt.anchoredPosition = Vector2.zero;
            bandRt.sizeDelta = new Vector2(0, 172);
            var bandImg = bandGo.AddComponent<Image>();
            bandImg.color = new Color(0.12f, 0.14f, 0.16f, 0.9f);
            bandImg.raycastTarget = false;

            timeTxt = Ui(hudG.transform, "90", new Vector2(0.5f, 1), new Vector2(0, -28), 20, Sand, 120);
            scoreTxt = Ui(hudG.transform, "0", new Vector2(0f, 1), new Vector2(70, -28), 20, Sand, 140, TextAlignmentOptions.Left);
            comboTxt = Ui(hudG.transform, "", new Vector2(1f, 1), new Vector2(-70, -56), 16, Rust, 120, TextAlignmentOptions.Right);
            promptTxt = Ui(hudG.transform, "", new Vector2(0.5f, 1), new Vector2(0, -84), 16, Sand, 370);
            promptTxt.textWrappingMode = TextWrappingModes.Normal;
            promptTxt.rectTransform.sizeDelta = new Vector2(370, 68);
            promptTxt.overflowMode = TextOverflowModes.Overflow;
            promptTxt.lineSpacing = -8f;
            goalTxt = Ui(hudG.transform, "수문을 비에 맞춰 잠가라", new Vector2(0.5f, 1), new Vector2(0, -148), 15, Hex("D8E6EE"), 340);
            for (int i = 0; i < 3; i++)
            {
                var im = Img(hudG.transform, "PinH" + i, new Vector2(1, 1), new Vector2(-28 - i * 22, -26), new Vector2(16, 28), Rust);
                pinImg[i] = im;
            }
            timeFill = Img(hudG.transform, "Tfill", new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(160, 4), WaterC);
            sheetFill = Img(hudG.transform, "Sfill", new Vector2(0.5f, 1), new Vector2(0, -166), new Vector2(340, 8), Rust);
            var sImg = sheetFill;
            sImg.type = Image.Type.Filled;
            sImg.fillMethod = Image.FillMethod.Horizontal;
            sImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            sImg.fillAmount = 1f;

            logoUi = Ui(titleG.transform, "", new Vector2(0.5f, 0.18f), new Vector2(0, 48), 18, Sand, 280);
            logoUi.gameObject.SetActive(false);
            tagTxt = Ui(titleG.transform, "수문을 밀어 잠가라", new Vector2(0.5f, 0.18f), new Vector2(0, 22), 16, FoamC, 300);
            badgeTxt = Ui(titleG.transform, "중2 · 평행선과 선분의 길이의 비", new Vector2(0.5f, 0.18f), new Vector2(0, 2), 13, Hex("C5D0D6"), 320);
            bestTxt = Ui(titleG.transform, "", new Vector2(0.5f, 0.18f), new Vector2(0, -18), 14, Sand, 300);
            hintTxt = Ui(titleG.transform, "탭하거나 수문을 아래로 끌어 시작", new Vector2(0.5f, 0.08f), new Vector2(0, 8), 16, Hex("E8E0D0"), 320);

            endHead = Ui(endG.transform, "", new Vector2(0.5f, 0.42f), Vector2.zero, 32, Sand, 340);
            endHead.fontStyle = FontStyles.Bold;
            endStats = Ui(endG.transform, "", new Vector2(0.5f, 0.32f), Vector2.zero, 20, FoamC, 340);
            endCtaTxt = Ui(endG.transform, "다시 잠그기", new Vector2(0.5f, 0.18f), Vector2.zero, 22, Sand, 280);
            endCtaTxt.fontStyle = FontStyles.Bold;
            ctaPlate = Img(endG.transform, "Cta", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(220, 52), new Color(Rust.r, Rust.g, Rust.b, 0.95f));
            endCtaTxt.transform.SetAsLastSibling();

            toastBg = Img(toastG.transform, "Tb", new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(340, 52), new Color(Iron.r, Iron.g, Iron.b, 0.88f));
            toastTxt = Ui(toastG.transform, "", new Vector2(0.5f, 0.22f), Vector2.zero, 15, Sand, 320);
            toastG.alpha = 0;
            LayoutHud();
        }

        void LayoutHud()
        {
            if (bandRt == null || promptTxt == null || goalTxt == null) return;
            var pr = promptTxt.rectTransform;
            var gr = goalTxt.rectTransform;
            if (land)
            {
                bandRt.anchorMin = new Vector2(0f, 0f);
                bandRt.anchorMax = new Vector2(0f, 1f);
                bandRt.pivot = new Vector2(0f, 0.5f);
                bandRt.anchoredPosition = Vector2.zero;
                bandRt.sizeDelta = new Vector2(248f, 0f);

                pr.anchorMin = pr.anchorMax = new Vector2(0f, 1f);
                pr.pivot = new Vector2(0f, 1f);
                pr.anchoredPosition = new Vector2(14f, -52f);
                pr.sizeDelta = new Vector2(220f, 280f);
                promptTxt.alignment = TextAlignmentOptions.TopLeft;
                promptTxt.fontSize = 15;
                promptTxt.maxVisibleLines = 10;

                gr.anchorMin = gr.anchorMax = new Vector2(0f, 1f);
                gr.pivot = new Vector2(0f, 1f);
                gr.anchoredPosition = new Vector2(14f, -344f);
                gr.sizeDelta = new Vector2(220f, 56f);
                goalTxt.alignment = TextAlignmentOptions.TopLeft;
                goalTxt.fontSize = 14;

                if (sheetFill)
                {
                    var sr = sheetFill.rectTransform;
                    sr.anchorMin = sr.anchorMax = new Vector2(0f, 0f);
                    sr.pivot = new Vector2(0f, 0f);
                    sr.anchoredPosition = new Vector2(14f, 18f);
                    sr.sizeDelta = new Vector2(220f, 8f);
                }

                if (scoreTxt) scoreTxt.rectTransform.anchoredPosition = new Vector2(274f, -22f);
                if (timeTxt)
                {
                    timeTxt.rectTransform.anchorMin = timeTxt.rectTransform.anchorMax = new Vector2(0.64f, 1f);
                    timeTxt.rectTransform.anchoredPosition = new Vector2(0f, -22f);
                }
                if (timeFill)
                {
                    timeFill.rectTransform.anchorMin = timeFill.rectTransform.anchorMax = new Vector2(0.64f, 1f);
                    timeFill.rectTransform.anchoredPosition = new Vector2(0f, -38f);
                }
            }
            else
            {
                bandRt.anchorMin = new Vector2(0f, 1f);
                bandRt.anchorMax = new Vector2(1f, 1f);
                bandRt.pivot = new Vector2(0.5f, 1f);
                bandRt.anchoredPosition = Vector2.zero;
                bandRt.sizeDelta = new Vector2(0f, 172f);

                pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 1f);
                pr.pivot = new Vector2(0.5f, 0.5f);
                pr.anchoredPosition = new Vector2(0f, -82f);
                pr.sizeDelta = new Vector2(370f, 64f);
                promptTxt.alignment = TextAlignmentOptions.Center;
                promptTxt.fontSize = 16;
                promptTxt.maxVisibleLines = 4;

                gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 1f);
                gr.pivot = new Vector2(0.5f, 0.5f);
                gr.anchoredPosition = new Vector2(0f, -140f);
                gr.sizeDelta = new Vector2(340f, 28f);
                goalTxt.alignment = TextAlignmentOptions.Center;
                goalTxt.fontSize = 15;

                if (sheetFill)
                {
                    var sr = sheetFill.rectTransform;
                    sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 1f);
                    sr.pivot = new Vector2(0.5f, 0.5f);
                    sr.anchoredPosition = new Vector2(0f, -166f);
                    sr.sizeDelta = new Vector2(340f, 8f);
                }

                if (scoreTxt) scoreTxt.rectTransform.anchoredPosition = new Vector2(70f, -28f);
                if (timeTxt)
                {
                    timeTxt.rectTransform.anchorMin = timeTxt.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    timeTxt.rectTransform.anchoredPosition = new Vector2(0f, -28f);
                }
                if (timeFill)
                {
                    timeFill.rectTransform.anchorMin = timeFill.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                    timeFill.rectTransform.anchoredPosition = new Vector2(0f, -44f);
                }
            }
        }

        static CanvasGroup Cg(string n, Transform p, float a)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(p, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var g = go.GetComponent<CanvasGroup>(); g.alpha = a; g.blocksRaycasts = false; g.interactable = false;
            return g;
        }
        static TextMeshProUGUI Ui(Transform p, string s, Vector2 a, Vector2 pos, float size, Color c, float w, TextAlignmentOptions al = TextAlignmentOptions.Center)
        {
            var t = MgfText.Ui(s, a, pos, size, c, w);
            t.transform.SetParent(p, false);
            var rt = t.rectTransform; rt.anchorMin = rt.anchorMax = a; rt.anchoredPosition = pos;
            t.alignment = al; t.raycastTarget = false;
            return t;
        }
        static Image Img(Transform p, string n, Vector2 a, Vector2 pos, Vector2 size, Color c)
        {
            var go = new GameObject(n, typeof(RectTransform));
            go.transform.SetParent(p, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = a; rt.anchoredPosition = pos; rt.sizeDelta = size;
            var im = go.AddComponent<Image>(); im.color = c; im.raycastTarget = false;
            return im;
        }

        void SetupSheet(Sheet s, bool asDemo)
        {
            demo = asDemo;
            demoPhase = 0;
            lockedThis = false;
            hintedMatch = false;
            LayoutTri(s);
            adVis = s.ad;
            PlaceGate(s, true);
            DrawStatic(s);
            RefreshGate(true);
            HideGhost();
            if (logoW) logoW.gameObject.SetActive(asDemo || ph == Ph.Title);
            if (fixedT) fixedT.gameObject.SetActive(s.kind == Kind.Three && s.fAd > 0 && !asDemo);
            if (weightT) weightT.gameObject.SetActive(s.isPlumb && !asDemo);
            if (promptTxt)
            {
                if (asDemo) promptTxt.text = "";
                else promptTxt.text = Words.Prompt(s);
            }
            if (goalTxt && !asDemo) goalTxt.text = Words.Goal(s);
            LayoutHud();
            FrameCam();
        }

        void LayoutTri(Sheet s)
        {
            float ab, ac, bc;
            if (s.kind == Kind.Area) { ab = 11f; ac = 11f; bc = 8f; }
            else { ab = Mathf.Max(3, s.ab); ac = Mathf.Max(3, s.ac); bc = Mathf.Max(3, s.bc); }
            float x = (ab * ab - ac * ac + bc * bc) / (2f * bc);
            float y = Mathf.Sqrt(Mathf.Max(0.4f, ab * ab - x * x));
            B = new Vector3(-bc * 0.5f, 0f, 0f);
            C = new Vector3(bc * 0.5f, 0f, 0f);
            A = new Vector3(-bc * 0.5f + x, y, 0f);
            bool wide = (float)Screen.width / Mathf.Max(1, Screen.height) >= 1.2f;
            float sc = 10.6f / Mathf.Max(bc, 6f);
            if (wide) sc *= 1.18f;
            float lift = 0.55f;
            float maxA = wide ? 4.2f : 3.7f;
            float ay = y * sc + lift;
            if (ay > maxA) sc *= (maxA - lift) / Mathf.Max(0.4f, y * sc);
            A *= sc; B *= sc; C *= sc;
            A.y += lift; B.y += lift; C.y += lift;
            float midX = (Mathf.Min(A.x, Mathf.Min(B.x, C.x)) + Mathf.Max(A.x, Mathf.Max(B.x, C.x))) * 0.5f;
            A.x -= midX; B.x -= midX; C.x -= midX;
            midD = (B + C) * 0.5f;
        }

        void FrameCam()
        {
            if (cam == null) return;
            float minX = Mathf.Min(A.x, Mathf.Min(B.x, C.x));
            float maxX = Mathf.Max(A.x, Mathf.Max(B.x, C.x));
            float minY = Mathf.Min(A.y, Mathf.Min(B.y, C.y));
            float maxY = Mathf.Max(A.y, Mathf.Max(B.y, C.y));
            minX -= 1.35f; maxX += 1.35f;
            minY -= 1.45f; maxY += 0.95f;
            if (sluiceT)
            {
                minX = Mathf.Min(minX, sluiceT.position.x - 2.4f);
                maxX = Mathf.Max(maxX, sluiceT.position.x + 2.4f);
                minY = Mathf.Min(minY, sluiceT.position.y - 1.15f);
                maxY = Mathf.Max(maxY, sluiceT.position.y + 1.05f);
            }
            float cx = (minX + maxX) * 0.5f;
            float cy = (minY + maxY) * 0.5f;
            float width = Mathf.Max(4.2f, maxX - minX);
            float height = Mathf.Max(3.2f, maxY - minY);

            float aspect = Mathf.Max(0.01f, (float)Screen.width / Mathf.Max(1, Screen.height));
            float baseFov = land ? 28f : 32f;
            const float minA = 0.72f;
            float vfov = baseFov;
            if (aspect < minA)
            {
                float h = Mathf.Tan(baseFov * 0.5f * Mathf.Deg2Rad) * minA / aspect;
                vfov = Mathf.Min(100f, 2f * Mathf.Atan(h) * Mathf.Rad2Deg);
            }
            float halfH = Mathf.Tan(vfov * 0.5f * Mathf.Deg2Rad);
            float halfW = halfH * aspect;
            float useW = (land && ph != Ph.Title) ? 0.56f : 0.90f;
            float useH = land ? 0.86f : 0.70f;
            float distW = (width * 0.5f) / Mathf.Max(0.04f, halfW * useW);
            float distH = (height * 0.5f) / Mathf.Max(0.04f, halfH * useH);
            float dist = Mathf.Clamp(Mathf.Max(distW, distH) * 1.14f, 11f, 24f);
            float worldHalfW = halfW * dist;
            float worldHalfH = halfH * dist;

            lookHome = new Vector3(cx, cy, 0f);
            if (land && ph != Ph.Title) lookHome.x = cx - 0.32f * worldHalfW;
            else lookHome.y = cy + 0.14f * worldHalfH;
            camHome = new Vector3(lookHome.x, lookHome.y + 0.35f, lookHome.z - dist);
            if (cam)
            {
                cam.transform.position = camHome;
                cam.transform.LookAt(lookHome);
            }
        }

        Vector3 OnAB(float t) => Vector3.Lerp(A, B, t);
        Vector3 OnAC(float t) => Vector3.Lerp(A, C, t);

        void PlaceGate(Sheet s, bool snap)
        {
            int span = Mathf.Max(1, s.Span);
            float t = Mathf.Clamp01((snap ? s.ad : adVis) / (float)span);
            if (s.isPlumb)
            {
                Gpt = Vector3.Lerp(A, midD, t);
                D = midD; E = midD;
                if (weightT)
                {
                    weightT.gameObject.SetActive(ph != Ph.Title);
                    weightT.position = Gpt + new Vector3(0, 0, -0.15f);
                }
                sluiceT.position = Gpt + new Vector3(0, 0, -0.2f);
                sluiceT.rotation = Quaternion.identity;
                sluiceT.localScale = new Vector3(0.7f, 0.7f, 0.7f) * sluiceSquash;
            }
            else
            {
                D = OnAB(t); E = OnAC(t);
                var mid = (D + E) * 0.5f;
                var dir = E - D; float L = Mathf.Max(0.4f, dir.magnitude);
                sluiceT.position = mid + new Vector3(0, 0, -0.18f);
                float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                sluiceT.rotation = Quaternion.Euler(0, 0, ang);
                sluiceT.localScale = new Vector3(L / 4f, sluiceSquash, 1f);
                if (s.kind == Kind.Three && s.fAd > 0)
                {
                    float tf = Mathf.Clamp01(s.fAd / (float)span);
                    var fd = OnAB(tf); var fe = OnAC(tf);
                    var fm = (fd + fe) * 0.5f;
                    fixedT.position = fm + new Vector3(0, 0, -0.12f);
                    fixedT.rotation = Quaternion.Euler(0, 0, Mathf.Atan2((fe - fd).y, (fe - fd).x) * Mathf.Rad2Deg);
                    fixedT.localScale = new Vector3(Mathf.Max(0.4f, (fe - fd).magnitude) / 4f, 0.85f, 0.9f);
                    fixedT.gameObject.SetActive(true);
                }
            }
            if (pinL) pinL.localRotation = Quaternion.Euler(90, 0, pinSpin * 80f);
            if (pinR) pinR.localRotation = Quaternion.Euler(90, 0, -pinSpin * 80f);
        }

        void DrawStatic(Sheet s)
        {
            ticks.Clear();
            var ab = Xy(A); var bb = Xy(B); var cb = Xy(C);
            var edge = C32(Iron, 0.95f);
            ticks.Seg(ab, bb, 0.07f, edge); ticks.Seg(ab, cb, 0.07f, edge); ticks.Seg(bb, cb, 0.08f, C32(WaterC, 0.9f));
            int span = Mathf.Max(1, s.Span);
            for (int i = 1; i < span; i++)
            {
                float t = i / (float)span;
                bool blocked = i > span - 1 - (Rules.Pins - st.lives) && ph == Ph.Play;
                var col = blocked ? C32(Rust, 0.7f) : C32(Sand, 0.55f);
                var n = (bb - ab);
                float nl = n.magnitude; if (nl < 1e-4f) continue;
                n /= nl; var n2 = new Vector2(-n.y, n.x);
                ticks.Seg(Xy(OnAB(t)) - n2 * 0.12f, Xy(OnAB(t)) + n2 * 0.12f, 0.035f, col);
                var m = (cb - ab);
                float ml = m.magnitude; if (ml < 1e-4f) continue;
                m /= ml; var m2 = new Vector2(-m.y, m.x);
                ticks.Seg(Xy(OnAC(t)) - m2 * 0.12f, Xy(OnAC(t)) + m2 * 0.12f, 0.035f, col);
            }
            // 평행 화살깃 on DE and BC (조건이지 정답 누설 아님)
            ticks.Apply();

            float aLift = Vector3.Distance(A, sluiceT ? sluiceT.position : A) < 1.35f ? 0.72f : 0.46f;
            vLbl[0].transform.position = A + new Vector3(0f, aLift, -0.1f); vLbl[0].text = "A";
            vLbl[1].transform.position = B + new Vector3(-0.18f, -0.38f, -0.1f); vLbl[1].text = "B";
            vLbl[2].transform.position = C + new Vector3(0.18f, -0.38f, -0.1f); vLbl[2].text = "C";
            vLbl[3].text = s.isPlumb ? "D" : "D";
            vLbl[4].text = s.isPlumb ? "" : "E";
            vLbl[5].text = s.isPlumb ? "G" : "";
            vLbl[4].gameObject.SetActive(!s.isPlumb);
            vLbl[5].gameObject.SetActive(s.isPlumb);
        }

        static Vector2 Xy(Vector3 p) => new Vector2(p.x, p.y);

        void RefreshGate(bool snap)
        {
            if (cur == null) return;
            PlaceGate(cur, snap);
            Vector3 inwardD = ((B + C) * 0.5f) - D; inwardD.z = 0f;
            if (inwardD.sqrMagnitude < 0.01f) inwardD = Vector3.right;
            vLbl[3].transform.position = D + inwardD.normalized * 0.22f + new Vector3(0f, 0.18f, -0.12f);
            if (!cur.isPlumb)
            {
                Vector3 inwardE = ((B + C) * 0.5f) - E; inwardE.z = 0f;
                if (inwardE.sqrMagnitude < 0.01f) inwardE = Vector3.left;
                vLbl[4].transform.position = E + inwardE.normalized * 0.22f + new Vector3(0f, 0.18f, -0.12f);
            }
            if (cur.isPlumb) vLbl[5].transform.position = Gpt + new Vector3(0.4f, 0.15f, -0.12f);

            waterInk.Clear();
            if (!cur.isPlumb)
            {
                var d = Xy(D); var e = Xy(E); var b = Xy(B); var c = Xy(C);
                waterInk.Tri(d, b, c, C32(WaterC, 0.38f));
                waterInk.Tri(d, c, e, C32(WaterC, 0.38f));
            }
            else
            {
                waterInk.Tri(Xy(Gpt), Xy(B), Xy(C), C32(WaterC, 0.32f));
            }
            waterInk.Apply();

            bool match = cur.ad == cur.target;
            Color rc = match ? Hex("D8E6EE") : Sand;
            bool titleHide = ph == Ph.Title || demo;
            if (ratioLbl) ratioLbl.gameObject.SetActive(!titleHide);
            if (lenLbl) lenLbl.gameObject.SetActive(!titleHide);
            if (cur.isPlumb)
            {
                int gd = Mathf.Max(0, cur.median - cur.ad);
                ratioLbl.text = "AG:GD = " + cur.ad + ":" + gd;
                ratioLbl.color = rc;
                lenLbl.text = "AG " + cur.ad + " cm";
            }
            else if (cur.hideRatio) { ratioLbl.text = ""; lenLbl.text = LenLine(cur); }
            else if (cur.showWhole)
            {
                Judge.Ratio(cur.ad, cur.ab, out int m1, out int n1);
                int de = Judge.De(cur, cur.ad);
                string des = de > 0 ? de.ToString() : "·";
                ratioLbl.text = "AD:AB = " + m1 + ":" + n1 + "\nDE:BC = " + des + ":" + cur.bc;
                ratioLbl.color = rc;
                lenLbl.text = LenLine(cur);
            }
            else
            {
                int db = cur.ab - cur.ad;
                Judge.Ratio(cur.ad, db, out int m, out int n);
                if (dumpT > 0 && ph == Ph.Practice && match)
                    ratioLbl.text = "AD:DB = AE:EC = " + m + ":" + n;
                else
                    ratioLbl.text = "AD:DB = " + m + ":" + n;
                ratioLbl.color = (dumpT > 0 && ph == Ph.Practice) ? FoamC : rc;
                lenLbl.text = LenLine(cur);
            }
            PlaceRatioLabels();
            lenLbl.color = (dumpT > 0 && ph == Ph.Practice && !cur.isPlumb) ? FoamC : Sand;
            if (logoW && (ph == Ph.Title || demo))
                logoW.transform.position = sluiceT.position + new Vector3(0, 0.04f, -0.42f);
        }

        void PlaceRatioLabels()
        {
            if (!sluiceT || !ratioLbl || !lenLbl) return;
            Vector3 mid = (B + C) * 0.5f;
            Vector3 sluice = sluiceT.position;
            Vector3 down = mid - sluice; down.z = 0f;
            if (down.sqrMagnitude < 0.04f) down = Vector3.down;
            down.Normalize();

            // 수문 메시·꼭짓점 A·스텐실과 분리: 사면 안쪽(밑변 쪽)에 둔다.
            float gap = 0.88f;
            var rpos = sluice + down * gap;
            float toBase = Vector3.Distance(new Vector3(sluice.x, sluice.y, 0f), mid);
            float toA = Vector3.Distance(new Vector3(sluice.x, sluice.y, 0f), A);
            if (toBase < 1.15f && toA > 1.5f)
                rpos = sluice - down * 0.70f;
            if ((rpos - A).sqrMagnitude < 0.64f)
                rpos += down * 0.55f;
            rpos.z = -0.30f;
            var lpos = rpos + down * 0.52f;
            lpos.z = -0.30f;
            ratioLbl.transform.position = rpos;
            lenLbl.transform.position = lpos;
            if (vLbl[0])
            {
                float aLift = Vector3.Distance(A, sluice) < 1.35f ? 0.72f : 0.46f;
                vLbl[0].transform.position = A + new Vector3(0f, aLift, -0.1f);
            }
        }

        static string LenLine(Sheet s)
        {
            if (s.isPlumb)
                return "AG " + s.ad + " cm  GD " + Mathf.Max(0, s.median - s.ad) + " cm";
            var sb = new System.Text.StringBuilder();
            sb.Append("AD ").Append(s.ad).Append(" cm");
            sb.Append("  DB ").Append(s.ab - s.ad).Append(" cm");
            int ae = Judge.Ae(s, s.ad);
            if (ae > 0) sb.Append("  AE ").Append(ae).Append(" cm  EC ").Append(s.ac - ae).Append(" cm");
            int de = Judge.De(s, s.ad);
            if (de > 0) sb.Append("  DE ").Append(de).Append(" cm");
            return sb.ToString();
        }

        void RefreshHud()
        {
            if (scoreTxt == null) return;
            int t = ph == Ph.Play ? Mathf.CeilToInt(Mathf.Max(0, runLeft)) : (ph == Ph.Practice ? -1 : 90);
            if (t != lastTimeInt && t >= 0) { timeTxt.text = t.ToString(); lastTimeInt = t; }
            if (ph == Ph.Practice) timeTxt.text = "연습";
            float u = ph == Ph.Play ? Mathf.Clamp01(runLeft / Rules.RunSec) : 1f;
            if (timeFill) timeFill.rectTransform.sizeDelta = new Vector2(160f * u, 4f);
            if (sheetFill)
            {
                bool show = ph == Ph.Play && !lockedThis;
                sheetFill.enabled = show;
                if (show)
                {
                    float su = Mathf.Clamp01(sheetLeft / Rules.SheetSec);
                    sheetFill.fillAmount = su;
                    sheetFill.color = su < 0.28f ? Rust : WaterC;
                }
            }
            if (st.combo >= 2) comboTxt.text = "×" + Rules.Mult(st.combo); else comboTxt.text = "";
            for (int i = 0; i < 3; i++) if (pinImg[i]) pinImg[i].color = i < st.lives ? Rust : new Color(0.3f, 0.28f, 0.26f, 0.4f);
            if (promptTxt && ph == Ph.Play) promptTxt.text = cur != null ? Words.Prompt(cur) : "";
        }

        void SetVisible()
        {
            if (hudG) hudG.alpha = (ph == Ph.Play || ph == Ph.Practice) ? 1 : 0;
            if (titleG) titleG.alpha = ph == Ph.Title ? 1 : 0;
            if (endG) endG.alpha = ph == Ph.End ? 1 : 0;
            if (logoW) logoW.gameObject.SetActive(ph == Ph.Title);
            if (fingerT) fingerT.gameObject.SetActive(ph == Ph.Title || ph == Ph.Practice);
            if (ph == Ph.End)
            {
                if (endReason == "clear") endHead.text = "10문 잠금";
                else if (endReason == "time") endHead.text = "시간 종료";
                else endHead.text = "잠금핀 소진";
                endStats.text = "점수  " + st.score + "   잠금  " + st.solved + " / 10";
            }
            RefreshHud();
        }

        void ShowGhost(int ad)
        {
            if (!ghostTform || cur == null) return;
            int span = Mathf.Max(1, cur.Span);
            float t = Mathf.Clamp01(ad / (float)span);
            if (cur.isPlumb)
            {
                var p = Vector3.Lerp(A, midD, t);
                ghostTform.position = p + new Vector3(0, 0, -0.22f);
                ghostTform.localScale = Vector3.one * 0.6f;
            }
            else
            {
                var d = OnAB(t); var e = OnAC(t);
                var mid = (d + e) * 0.5f;
                ghostTform.position = mid + new Vector3(0, 0, -0.22f);
                ghostTform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2((e - d).y, (e - d).x) * Mathf.Rad2Deg);
                ghostTform.localScale = new Vector3(Mathf.Max(0.4f, (e - d).magnitude) / 4f, 0.8f, 0.8f);
            }
            ghostTform.gameObject.SetActive(true);
            ghostAlpha = 1f;
        }
        void HideGhost() { if (ghostTform) ghostTform.gameObject.SetActive(false); ghostAlpha = 0; }

        void PunchGate() { sluiceSquash = 1.18f; }
        void JiggleGate() { jiggleT = 0.36f; }
        void SpinPinsHint() { pinSpin = 1f; }
        void PointAtGate() { pulseGo.gameObject.SetActive(true); pulseTm = 0.6f; }

        void SpawnGhostFinger()
        {
            idleT = 0;
            ghostT = 0;
            if (fingerT) fingerT.gameObject.SetActive(true);
        }

        void BreakPin()
        {
            brokenVis = Rules.Pins - st.lives;
        }
        void RestorePins() { brokenVis = 0; for (int i = 0; i < 3; i++) if (pinImg[i]) pinImg[i].color = Rust; }

        void Animate(float dt)
        {
            titleT += dt; foamT += dt; ghostT += dt;
            if (jiggleT > 0) jiggleT -= dt;
            if (pinSpin > 0) pinSpin = Mathf.MoveTowards(pinSpin, 0, dt * 2.2f);
            if (sluiceSquash > 1f) sluiceSquash = Mathf.MoveTowards(sluiceSquash, 1f, dt * 2.4f);
            if (revealT > 0) { revealT -= dt; if (revealT <= 0) HideGhost(); }
            if (dumpT > 0) dumpT -= dt;
            if (slideT > 0) slideT -= dt;
            if (toastT < toastDur) { toastT += dt; if (toastG) toastG.alpha = toastT > toastDur - 0.2f ? (toastDur - toastT) / 0.2f : 1f; }
            else if (toastG) toastG.alpha = 0;
            if (shimmerT > 0) shimmerT -= dt;

            shownScore = Mathf.MoveTowards(shownScore, st.score, dt * 420f);
            int si = Mathf.RoundToInt(shownScore);
            if (si != shownScoreInt) { shownScoreInt = si; if (scoreTxt) scoreTxt.text = si.ToString(); }

            // 타이틀 데모: 수문이 두 칸 미끄러이다가 2:1에서 잠김
            if (ph == Ph.Title && cur != null && press == Press.None)
            {
                demoPhase += dt;
                float loop = demoPhase % 3.2f;
                float want = loop < 1.6f ? Mathf.Lerp(1, 6, Mathf.SmoothStep(0, 1, loop / 1.6f)) : (loop < 2.3f ? 6 : Mathf.Lerp(6, 1, (loop - 2.3f) / 0.9f));
                cur.ad = Mathf.Clamp(Mathf.RoundToInt(want), 1, 8);
                adVis = want;
                PlaceGate(cur, false);
                RefreshGate(false);
                if (loop > 1.55f && loop < 1.7f && dumpT <= 0) { dumpT = 0.7f; SumunSound.Play("clang", 0.35f); }
            }

            PlaceGate(cur, false);
            DrawDump();
            DrawGuide();
            AnimateFinger(dt);
            AnimateFoam();
            Billboard();

            float jx = jiggleT > 0 ? Mathf.Sin(Time.time * 18f) * 0.11f * (jiggleT / 0.36f) : 0;
            if (sluiceT && jiggleT > 0) sluiceT.position += new Vector3(jx, 0, 0);
            if (slideT > 0 && sluiceT)
                sluiceT.position += new Vector3(Mathf.Sin(slideT * 20f) * 0.25f * slideT, -0.15f * (1f - slideT / 0.55f), 0);

            float fov = land ? 28f : 32f;
            MgfLook.FitWidth(cam, fov, 0.72f);
            if (cam)
            {
                cam.transform.position = Vector3.Lerp(cam.transform.position, camHome, 0.28f);
                cam.transform.LookAt(lookHome);
            }

            if (ph == Ph.Play) RefreshHud();
            if (ph == Ph.End && endStats)
            {
                endT += dt;
                int cs = Mathf.RoundToInt(Mathf.Lerp(0, st.score, Mathf.Clamp01(endT / 0.7f)));
                endStats.text = "점수  " + cs + "   잠금  " + st.solved + " / 10";
                if (endReason != "clear") endHead.color = Sand;
            }
        }

        void DrawDump()
        {
            dumpInk.Clear();
            float dur = dumpDur > 0.2f ? dumpDur : 1.15f;
            if (dumpT > 0 && cur != null && !cur.isPlumb)
            {
                float k = 1f - dumpT / dur;
                var a = Xy(A);
                var d = Xy(OnAB(cur.target / (float)Mathf.Max(1, cur.Span)));
                var e = Xy(OnAC(cur.target / (float)Mathf.Max(1, cur.Span)));
                var fall = new Vector2(0, -k * 2.2f);
                dumpInk.Tri(a + fall * 0.2f, d + fall, e + fall, C32(WaterC, 0.72f * (1f - k * 0.55f)));
            }
            else if (dumpT > 0 && cur != null && cur.isPlumb)
            {
                float k = 1f - dumpT / dur;
                dumpInk.Tri(Xy(A), Xy(B), Xy(C), C32(WaterC, 0.28f * (1f - k * 0.5f)));
            }
            dumpInk.Apply();
        }

        void DrawGuide()
        {
            guideInk.Clear();
            bool match = cur != null && cur.ad == cur.target && !lockedThis;
            if ((ph == Ph.Practice || ph == Ph.Title) && cur != null && !cur.isPlumb)
            {
                float t0 = 1f / Mathf.Max(1, cur.Span);
                float t1 = (ph == Ph.Practice ? 3 : 6) / (float)Mathf.Max(1, cur.Span);
                var a = Xy((OnAB(t0) + OnAC(t0)) * 0.5f);
                var b = Xy((OnAB(t1) + OnAC(t1)) * 0.5f);
                guideInk.Dashed(a, b, 0.18f, 0.12f, 0.045f, C32(FoamC, 0.7f), ghostT * 1.4f);
                guideInk.Chevron(b, b - a, 0.28f, 0.05f, C32(FoamC, 0.85f));
                // 가로 힌트·핀 유령은 비가 목표와 같을 때만(또는 타이틀 데모).
                if (ph == Ph.Title || match)
                {
                    var mid = (OnAB(t1) + OnAC(t1)) * 0.5f;
                    if (match && sluiceT) mid = sluiceT.position;
                    var r = Xy(mid) + new Vector2(1.15f, 0);
                    guideInk.Dashed(Xy(mid), r, 0.16f, 0.1f, 0.045f, C32(Sand, 0.8f), ghostT * 1.6f);
                    guideInk.Chevron(r, Vector2.right, 0.26f, 0.05f, C32(Sand, 0.9f));
                    pinSpin = Mathf.Max(pinSpin, 0.85f);
                }
            }
            else if (match && ph == Ph.Play && cur != null && !cur.isPlumb && sluiceT)
            {
                var mid = sluiceT.position;
                var r = Xy(mid) + new Vector2(1.15f, 0);
                guideInk.Dashed(Xy(mid), r, 0.16f, 0.1f, 0.045f, C32(Sand, 0.75f), ghostT * 1.6f);
                guideInk.Chevron(r, Vector2.right, 0.26f, 0.05f, C32(Sand, 0.85f));
                pinSpin = Mathf.Max(pinSpin, 0.7f);
            }
            if ((pulseTm > 0 || (match && ph != Ph.Title && !lockedThis)) && sluiceT)
            {
                if (pulseTm > 0) pulseTm -= Time.deltaTime;
                float wave = match ? 0.55f + 0.25f * Mathf.Sin(Time.time * 4.2f) : (0.7f + (1f - pulseTm / 0.6f) * 0.8f);
                pulseGo.gameObject.SetActive(true);
                pulseGo.position = sluiceT.position + new Vector3(0, 0, -0.3f);
                pulseGo.localScale = new Vector3(wave, 0.02f, wave);
                if (pulseTm <= 0 && !match) pulseGo.gameObject.SetActive(false);
            }
            else if (pulseGo && pulseTm <= 0) pulseGo.gameObject.SetActive(false);
            if (refuseT > 0)
            {
                refuseT -= Time.deltaTime;
                float life = 0.32f;
                var ray = cam.ScreenPointToRay(refuseAt);
                if (new Plane(Vector3.forward, Vector3.zero).Raycast(ray, out float dist))
                {
                    var p = ray.GetPoint(dist);
                    float k = Mathf.Clamp01(refuseT / life);
                    guideInk.Disc(Xy(p), 0.55f * (1f - k), C32(FoamC, k * 0.85f), 16);
                    guideInk.Disc(Xy(p), 0.28f * (1f - k * 0.5f), C32(Sand, k * 0.5f), 12);
                }
            }
            guideInk.Apply();
        }

        void AnimateFinger(float dt)
        {
            bool show = (ph == Ph.Title || ph == Ph.Practice) && press == Press.None;
            if (fingerT) fingerT.gameObject.SetActive(show);
            if (!show || cur == null) return;
            float loop = (ghostT % 1.4f) / 1.4f;
            int span = Mathf.Max(1, cur.Span);
            float tA = 1f / span, tB = (ph == Ph.Practice ? 3f : 6f) / span;
            Vector3 p0 = (OnAB(tA) + OnAC(tA)) * 0.5f + new Vector3(0, 0, -0.5f);
            Vector3 p1 = (OnAB(tB) + OnAC(tB)) * 0.5f + new Vector3(0, 0, -0.5f);
            float swipe = cur.isPlumb ? 1.4f : Mathf.Max(1.35f, (E - D).magnitude * 0.55f);
            Vector3 p2 = p1 + new Vector3(swipe, 0, 0);
            Vector3 p;
            if (loop < 0.50f) p = Vector3.Lerp(p0, p1, loop / 0.50f);
            else if (loop < 0.82f)
            {
                p = Vector3.Lerp(p1, p2, (loop - 0.50f) / 0.32f);
                pinSpin = 1f;
            }
            else p = Vector3.Lerp(p2, p0, (loop - 0.82f) / 0.18f);
            fingerT.position = p;
            float sc = ghostScale * (0.85f + 0.15f * Mathf.Sin(ghostT * 6f));
            fingerT.localScale = Vector3.one * sc;
        }

        void AnimateFoam()
        {
            for (int i = 0; i < foam.Length; i++)
            {
                if (!foam[i]) continue;
                float x = Mathf.Lerp(B.x + 0.4f, C.x - 0.4f, (i + 0.5f) / foam.Length);
                float y = B.y + 0.12f + 0.06f * Mathf.Sin(foamT * 1.3f + i * 0.9f);
                foam[i].position = new Vector3(x, y, 0.35f);
                foam[i].rotation = Quaternion.Euler(70, 0, 0);
            }
        }

        void Billboard()
        {
            if (!cam) return;
            var r = cam.transform.rotation;
            for (int i = 0; i < vLbl.Length; i++) if (vLbl[i]) vLbl[i].transform.rotation = r;
            if (ratioLbl) ratioLbl.transform.rotation = r;
            if (lenLbl) lenLbl.transform.rotation = r;
            if (logoW) logoW.transform.rotation = r;
        }
    }
}
