// 도토리 낙하 — 지하 뿌리 저장고 절개면 2.5D 디오라마, 무게표·정렬 뿌리·수납 레일 UI.
// 3D 배치는 화면 비율 좌표(At)에서 역산한다: UI 줄(꾸러미 줄·정렬 가지·레일)과 3D 가지가 세로·가로 화면 모두에서 겹친다.
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.DotoriNakha
{
    public partial class DotoriNakhaGame
    {
        static readonly Color LeafLight = MgfLook.Hex("#6D594A");
        static readonly Color Bark = MgfLook.Hex("#A95F42");
        static readonly Color BarkDark = MgfLook.Hex("#1C1925");
        static readonly Color Acorn = MgfLook.Hex("#E7A84B");
        static readonly Color Paper = MgfLook.Hex("#F3E7C4");
        static readonly Color Ink = MgfLook.Hex("#211B24");
        static readonly Color Good = MgfLook.Hex("#3D9D84");
        static readonly Color Bad = MgfLook.Hex("#D64C4C");

        // 깊이(카메라 정면 거리). 직교 카메라라 크기는 깊이와 무관하고 겹침·그림자만 달라진다.
        const float DCanopy = 27f, DTrunk = 23f, DBranch = 18.5f, DPouch = 18f, DBasket = 17.2f, DFront = 13f;

        Camera cam;
        Light sun;
        Transform worldRoot, basketT, basketBody, squirrelT, squirrelBody, squirrelTail, heldAcorn, pileT;
        Transform rootCore, rootSplitLeft, rootSplitRight, recordTablet;
        MeshFilter canopyMf, branchesMf, pileMf;
        readonly Transform[] pouches = new Transform[8];
        readonly Transform[] acornDrops = new Transform[9];
        readonly Transform[] latches = new Transform[3];
        readonly Transform[] huts = new Transform[8];
        readonly Transform[] shutters = new Transform[8];
        readonly Transform[] ropes = new Transform[8];
        readonly Transform[] birds = new Transform[2];
        readonly Transform[] birdWings = new Transform[4];
        readonly Transform[] fallingLeaves = new Transform[6];
        readonly Transform[] flecks = new Transform[5];
        readonly Transform[] fronds = new Transform[4];
        readonly Vector3[] frondBase = new Vector3[4];
        readonly Mesh[] pileMeshes = new Mesh[7];
        Mesh acornMesh;
        readonly LP lp = new LP();

        float hitStop, clearRun = -1f;
        float clock, titleDrop, feedbackClock = 9f, guideBoost, rippleClock, toastClock, shownScore, shownEndScore, spreadClock = 9f, signSwing;
        int shownSecond = -1, lastScoreShown = -1, lastEndShown = -1, lastRailShown = -1;
        int lastW = -1, lastH = -1;
        float lastScale = -1f;
        bool land, feedbackGood;
        int brokenLatch = -1, pileShown = -1, dropFrom;
        Vector3 birdHomeA, birdHomeB;

        CanvasGroup titleG, hudG, revealG, endG, toastG, defG;
        RectTransform titleRt, hudRt, endRt, signRt, knotRt, knotRingRt, handleRt, rippleRt, guidePathRt, guideRingRt, guideHandRt, guideTargetRt, revealRt, defRt, toastRt, endPanelRt;
        Image knotRingImg, guideTargetImg;
        TextMeshProUGUI titleLogo, titleLogoShadow, titleTag, titleMeta, titleCta;
        TextMeshProUGUI scoreTxt, roundTxt, firstTxt, timeTxt, goalTxt, definitionTxt, sortLabelTxt, railValueTxt, grabHintTxt, toastTxt;
        TextMeshProUGUI revealTitleTxt, revealEvidenceTxt, endTitleTxt, endScoreTxt, endStatsTxt, endTapTxt;
        Image statusBg, goalBg, railImg, handleImg, guidePath, guideRing, guideHand, rippleImg, revealBg, definitionBg;
        readonly Image[] lifeImgs = new Image[3];
        readonly Image[] rawCards = new Image[8];
        readonly Image[] rawStrings = new Image[8];
        readonly TextMeshProUGUI[] rawTexts = new TextMeshProUGUI[8];
        readonly Image[] slotBgs = new Image[8];
        readonly TextMeshProUGUI[] slotOrdinals = new TextMeshProUGUI[8];
        readonly Image[] sortCards = new Image[8];
        readonly TextMeshProUGUI[] sortTexts = new TextMeshProUGUI[8];
        readonly Image[] ticks = new Image[23];
        readonly TextMeshProUGUI[] tickLabels = new TextMeshProUGUI[12];
        readonly Image[] trail = new Image[6];
        readonly float[] trailU = new float[6];
        Sprite roundedSprite, ringSprite, pillSprite, tagSprite;
        Vector2 logicalSize = new Vector2(390, 844);

        // ───────────────────────── 레이아웃(화면 비율, 아래가 0) ─────────────────────────
        float StatusY => land ? .95f : .957f;
        float GoalY => land ? .885f : .905f;
        float PouchBranchY => land ? .835f : .855f;
        float RawTagY => land ? .665f : .715f;
        float SortLabelY => land ? .585f : .628f;
        float DefY => land ? .50f : .455f;
        float DefX => land ? .835f : .5f;
        float RawX(int i, int count)
        {
            float u = count <= 1 ? .5f : i / (float)(count - 1);
            return land ? .09f + .82f * u : .085f + .83f * u;
        }

        const float SqScale = .9f;

        Vector3 At(float x01, float y01, float depth) => cam.ViewportToWorldPoint(new Vector3(x01, y01, depth));

        // ───────────────────────── UI 헬퍼 ─────────────────────────
        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size); var image = rt.gameObject.AddComponent<Image>();
            image.color = color; image.sprite = sprite; image.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color, float width = 360f)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 2.6f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color;
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text; t.fontStyle = FontStyles.Bold;
            return t;
        }

        static Sprite MakeSprite(int size, float radius, int kind)
        {
            // kind 0 둥근 사각, 1 고리, 2 씨앗 종이 꼬리표(둥근 사각 + 끈 구멍)
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, alpha;
                if (kind == 1)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(1f - Mathf.Abs(d - size * .40f) / (size * .07f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * .5f) - (size * .5f - radius), 0);
                    float qy = Mathf.Max(Mathf.Abs(fy - size * .5f) - (size * .5f - radius), 0);
                    alpha = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                    if (kind == 2)
                    {
                        float hx = fx - size * .5f, hy = fy - size * .84f;
                        if (hx * hx + hy * hy < size * size * .003f) alpha = 0f;
                    }
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            tex.SetPixels32(pixels); tex.Apply(false, true);
            float border = kind == 0 ? radius + 2 : 0;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static Mesh QuadMesh()
        {
            var m = new Mesh { name = "DotoriQuad" };
            m.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            return m;
        }

        Transform MeshObj(string name, Transform parent, Mesh mesh, Material mat, bool shadows = true)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        // ───────────────────────── 3D 세계 ─────────────────────────
        void BuildWorld()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = MgfLook.Hex("#67516B") * .58f;
            RenderSettings.ambientEquatorColor = MgfLook.Hex("#3E6B65") * .48f;
            RenderSettings.ambientGroundColor = MgfLook.Hex("#241D2A") * .72f;
            sun = MgfLook.Sun(new Vector3(39, -26, 0), MgfLook.Hex("#FFD58A"), 1.08f, .62f);
            cam = MgfLook.Camera(new Vector3(0, 9f, -24f), Vector3.zero, 30f);
            cam.transform.rotation = Quaternion.Euler(24f, -6f, 0f);
            cam.orthographic = true; cam.orthographicSize = 10f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = MgfLook.Hex("#201D2C");
            cam.nearClipPlane = .1f; cam.farClipPlane = 60f;

            worldRoot = new GameObject("OakCanopyStorehouse").transform;
            canopyMf = MeshObj("CanopyAndBark", worldRoot, new Mesh { name = "Canopy" }, Pal.Lit, false).GetComponent<MeshFilter>();
            branchesMf = MeshObj("Branches", worldRoot, new Mesh { name = "Branches" }, Pal.Lit, true).GetComponent<MeshFilter>();

            // 도토리(씨앗+깍정이+꼭지) 한 메시
            lp.Clear();
            lp.Sphere(LP.M(new Vector3(0, -.08f, 0), new Vector3(.62f, .80f, .62f)), Pal.Acorn, 1, false);
            lp.Sphere(LP.M(new Vector3(0, .24f, 0), new Vector3(.72f, .34f, .72f)), Pal.AcornCap, 1, true, .06f, 3);
            lp.Cyl(LP.M(new Vector3(.02f, .44f, 0), new Vector3(0, 0, -14f), new Vector3(.07f, .18f, .07f)), Pal.BarkDark, 5);
            acornMesh = lp.ToMesh("Acorn");

            // 꾸러미(볼록한 자루 + 묶음 + 엿보이는 도토리 둘 + 줄). 원점 = 묶음 매듭
            lp.Clear();
            lp.Sphere(LP.M(new Vector3(0, -.62f, 0), new Vector3(1.05f, 1.0f, .95f)), Pal.Sack, 1, true, .07f, 11);
            lp.Cyl(LP.M(new Vector3(0, -.08f, 0), new Vector3(.36f, .22f, .36f)), Pal.SackDark, 7, .5f, .62f);
            lp.Sphere(LP.M(new Vector3(-.18f, .12f, -.08f), new Vector3(.34f, .42f, .34f)), Pal.Acorn, 1, false);
            lp.Sphere(LP.M(new Vector3(-.18f, .30f, -.08f), new Vector3(.38f, .18f, .38f)), Pal.AcornCap, 1, true);
            lp.Sphere(LP.M(new Vector3(.20f, .08f, .02f), new Vector3(.30f, .38f, .30f)), Pal.Acorn, 1, false);
            lp.Sphere(LP.M(new Vector3(.20f, .24f, .02f), new Vector3(.34f, .16f, .34f)), Pal.AcornCap, 1, true);
            lp.Tube(new Vector3(0, .05f, 0), new Vector3(0, 1.6f, 0), .035f, .035f, Pal.Flax, 5);
            var pouchMesh = lp.ToMesh("Pouch");
            for (int i = 0; i < pouches.Length; i++) pouches[i] = MeshObj("HangingAcornPouch" + i, worldRoot, pouchMesh, Pal.Lit);

            BuildBasket();
            BuildSquirrel();
            squirrelT.gameObject.SetActive(false); // 야외 동물 반응을 제거하고 비생물 뿌리 사건이 주연이 된다.

            // 중앙값 위치에서 갈라지는 뿌리층과 새로 자라는 기록판.
            lp.Clear();
            lp.Sphere(LP.M(Vector3.zero, new Vector3(.72f, .72f, .72f)), Pal.Good, 1, true, .05f, 91);
            lp.Sphere(LP.M(Vector3.zero, new Vector3(.36f, .36f, .36f)), Pal.Sunny, 1, false);
            rootCore = MeshObj("MedianRootCore", worldRoot, lp.ToMesh("MedianRootCore"), Pal.Soft, false);
            lp.Clear(); lp.Box(LP.M(Vector3.zero, new Vector3(1.45f, .58f, .58f)), Pal.Bark);
            var splitMesh = lp.ToMesh("RootLayerHalf");
            rootSplitLeft = MeshObj("RootLayerSplitLeft", worldRoot, splitMesh, Pal.Lit);
            rootSplitRight = MeshObj("RootLayerSplitRight", worldRoot, splitMesh, Pal.Lit);
            lp.Clear();
            lp.Box(LP.M(Vector3.zero, new Vector3(1.25f, .84f, .16f)), Pal.Paper);
            lp.Sphere(LP.M(new Vector3(0, .10f, -.12f), new Vector3(.34f, .34f, .12f)), Pal.Acorn, 1, true);
            recordTablet = MeshObj("MedianWeightRecord", worldRoot, lp.ToMesh("MedianWeightRecord"), Pal.Lit, false);
            rootSplitLeft.gameObject.SetActive(false); rootSplitRight.gameObject.SetActive(false); recordTablet.gameObject.SetActive(false);

            for (int i = 0; i < acornDrops.Length; i++)
            {
                acornDrops[i] = MeshObj("FallingAcorn" + i, worldRoot, acornMesh, Pal.Soft);
                acornDrops[i].localScale = Vector3.one * .62f; acornDrops[i].gameObject.SetActive(false);
            }

            // 저장고 오두막(정적 몸체는 Branches 메시에), 셔터·밧줄은 상태에 따라 움직이므로 따로
            lp.Clear(); lp.Box(LP.M(Vector3.zero, new Vector3(.62f, .58f, .1f)), Pal.BarkDark);
            lp.Cyl(LP.M(new Vector3(.18f, 0, -.07f), new Vector3(90, 0, 0), new Vector3(.12f, .04f, .12f)), Pal.Acorn, 6);
            var shutterMesh = lp.ToMesh("Shutter");
            lp.Clear(); lp.Tube(Vector3.left * .5f, Vector3.right * .5f, .07f, .07f, Pal.Flax, 6);
            lp.Sphere(LP.M(Vector3.zero, new Vector3(.2f, .2f, .2f)), Pal.FlaxDark, 0);
            var ropeMesh = lp.ToMesh("Rope");
            for (int i = 0; i < 8; i++)
            {
                huts[i] = new GameObject("StoreHut" + i).transform; huts[i].SetParent(worldRoot, false);
                shutters[i] = MeshObj("StoreShutter" + i, huts[i], shutterMesh, Pal.Lit);
                ShutterPose(i, 0f);
                ropes[i] = MeshObj("BraidedRoute" + i, worldRoot, ropeMesh, Pal.Lit); ropes[i].gameObject.SetActive(false);
            }

            // 이전 수관판의 새는 빌드 호환을 위해 만들되 지하판에서는 표시하지 않는다.
            lp.Clear();
            lp.Sphere(LP.M(Vector3.zero, new Vector3(.62f, .46f, .44f)), Pal.Cream, 1, false);
            lp.Sphere(LP.M(new Vector3(.22f, .22f, 0), new Vector3(.36f, .34f, .34f)), Pal.Cream, 1, false);
            lp.Cyl(LP.M(new Vector3(.44f, .22f, 0), new Vector3(0, 0, -90), new Vector3(.12f, .16f, .12f)), Pal.Acorn, 5, .5f, 0f);
            lp.Sphere(LP.M(new Vector3(.30f, .28f, -.15f), new Vector3(.07f, .08f, .05f)), Pal.Ink, 0);
            lp.Sphere(LP.M(new Vector3(.30f, .28f, .15f), new Vector3(.07f, .08f, .05f)), Pal.Ink, 0);
            lp.Sphere(LP.M(new Vector3(-.36f, .06f, 0), new Vector3(.34f, .12f, .24f)), Pal.LeafDeep, 0);
            var birdMesh = lp.ToMesh("Bird");
            lp.Clear(); lp.Sphere(LP.M(Vector3.zero, new Vector3(.40f, .08f, .26f)), Pal.Good, 1, true);
            var wingMesh = lp.ToMesh("Wing");
            for (int i = 0; i < 2; i++)
            {
                birds[i] = MeshObj("CanopyBird" + i, worldRoot, birdMesh, Pal.Soft);
                birds[i].gameObject.SetActive(false);
                for (int k = 0; k < 2; k++)
                {
                    var w = MeshObj("Wing" + k, birds[i], wingMesh, Pal.Lit, false);
                    w.localPosition = new Vector3(-.02f, .12f, k == 0 ? -.2f : .2f); birdWings[i * 2 + k] = w;
                }
            }

            // 부유하는 뿌리 포자, 광물 반사, 전경 뿌리혹.
            lp.Clear(); lp.Sphere(LP.M(Vector3.zero, new Vector3(.55f, .06f, .30f)), Pal.LeafLight, 0);
            var leafMesh = lp.ToMesh("FallingLeaf");
            for (int i = 0; i < fallingLeaves.Length; i++) fallingLeaves[i] = MeshObj("FallingLeaf" + i, worldRoot, leafMesh, Pal.Lit, false);
            var quad = QuadMesh();
            var fleckMat = MgfLook.Additive(new Color(1f, .93f, .62f, .30f), MgfLook.SoftDot);
            for (int i = 0; i < flecks.Length; i++) flecks[i] = MeshObj("SunFleck" + i, worldRoot, quad, fleckMat, false);
            for (int i = 0; i < fronds.Length; i++)
            {
                lp.Clear();
                for (int k = 0; k < 5; k++)
                {
                    float a = k * 1.3f + i;
                    lp.Sphere(LP.M(new Vector3(Mathf.Cos(a) * .9f, Mathf.Sin(a * 1.7f) * .6f, k * .05f), new Vector3(1.9f, 1.25f, .8f)),
                        k % 2 == 0 ? Pal.Canopy : Pal.LeafLight, 1, true, .14f, i * 7 + k);
                }
                fronds[i] = MeshObj("ForegroundLeaves" + i, worldRoot, lp.ToMesh("Frond" + i), Pal.Lit, false);
            }
        }

        void BuildBasket()
        {
            basketT = new GameObject("CatchBasket").transform; basketT.SetParent(worldRoot, false);
            lp.Clear();
            // 땋은 아마 바구니: 원뿔대 몸통 + 세 줄 땋은 띠 + 테두리 + 어두운 입구
            lp.Cyl(LP.M(new Vector3(0, .42f, 0), new Vector3(2.0f, .84f, 1.55f)), Pal.Flax, 12, .38f, .52f, true);
            for (int k = 0; k < 3; k++)
                lp.Cyl(LP.M(new Vector3(0, .16f + k * .25f, 0), new Vector3(2.0f, .07f, 1.55f)), Pal.FlaxDark, 12, .395f + k * .045f, .405f + k * .045f, false);
            lp.Arc(LP.M(new Vector3(0, .86f, 0), new Vector3(90, 0, 0), new Vector3(1.04f, .80f, 1f)), Pal.FlaxDark, 1.0f, .09f, 0, Mathf.PI * 2f, 18, 6);
            lp.Cyl(LP.M(new Vector3(0, .80f, 0), new Vector3(1.86f, .04f, 1.40f)), Pal.SackDark, 12, .5f, .5f, true);
            basketBody = MeshObj("BraidedFlaxBasket", basketT, lp.ToMesh("Basket"), Pal.Lit);

            for (int n = 0; n < pileMeshes.Length; n++)
            {
                lp.Clear();
                for (int k = 0; k < n * 2; k++)
                {
                    float a = k * 2.399f, r = .22f + (k % 3) * .2f;
                    var p = new Vector3(Mathf.Cos(a) * r * 1.2f, .84f + (k / 5) * .14f, Mathf.Sin(a) * r * .8f);
                    lp.Sphere(LP.M(p, new Vector3(k * 23, 0, k * 41), new Vector3(.26f, .32f, .26f)), Pal.Acorn, 1, false);
                    lp.Sphere(LP.M(p + new Vector3(0, .12f, 0), new Vector3(k * 23, 0, k * 41), new Vector3(.29f, .13f, .29f)), Pal.AcornCap, 0);
                }
                pileMeshes[n] = lp.ToMesh("Pile" + n);
            }
            pileT = MeshObj("AcornPile", basketT, pileMeshes[0], Pal.Soft); pileMf = pileT.GetComponent<MeshFilter>();

            lp.Clear(); lp.Box(LP.M(Vector3.zero, new Vector3(.22f, .34f, .1f)), Pal.Acorn);
            lp.Cyl(LP.M(new Vector3(0, .1f, -.06f), new Vector3(90, 0, 0), new Vector3(.08f, .03f, .08f)), Pal.BarkDark, 6);
            var latchMesh = lp.ToMesh("Latch");
            for (int i = 0; i < latches.Length; i++)
            {
                var pivot = new GameObject("LatchPivot" + i).transform; pivot.SetParent(basketT, false);
                pivot.localPosition = new Vector3(-.5f + i * .5f, .80f, -.80f);
                var l = MeshObj("BasketLatch" + i, pivot, latchMesh, Pal.Lit); l.localPosition = new Vector3(0, -.16f, 0);
                latches[i] = pivot;
            }
        }

        void BuildSquirrel()
        {
            squirrelT = new GameObject("StoreSquirrel").transform; squirrelT.SetParent(basketT, false);
            lp.Clear();
            lp.Sphere(LP.M(new Vector3(0, .78f, 0), new Vector3(1.0f, 1.18f, .92f)), Pal.Fur, 2, false);
            lp.Sphere(LP.M(new Vector3(0, .74f, -.30f), new Vector3(.64f, .86f, .46f)), Pal.Belly, 2, false);
            lp.Sphere(LP.M(new Vector3(0, 1.62f, -.04f), new Vector3(1.0f, .88f, .88f)), Pal.Fur, 2, false);
            lp.Sphere(LP.M(new Vector3(0, 1.47f, -.36f), new Vector3(.56f, .38f, .34f)), Pal.Belly, 2, false);
            lp.Sphere(LP.M(new Vector3(0, 1.56f, -.53f), new Vector3(.13f, .10f, .09f)), Pal.Roof, 1, false);
            for (int s = -1; s <= 1; s += 2)
            {
                lp.Sphere(LP.M(new Vector3(s * .22f, 1.72f, -.36f), new Vector3(.15f, .18f, .09f)), Pal.Ink, 1, false);
                lp.Sphere(LP.M(new Vector3(s * .19f, 1.77f, -.41f), new Vector3(.05f, .05f, .03f)), Pal.White, 0, false);
                lp.Cyl(LP.M(new Vector3(s * .30f, 2.13f, .02f), new Vector3(0, 0, -s * 12f), new Vector3(.34f, .44f, .22f)), Pal.Fur, 7, .5f, .06f);
                lp.Cyl(LP.M(new Vector3(s * .30f, 2.10f, -.06f), new Vector3(0, 0, -s * 12f), new Vector3(.18f, .30f, .08f)), Pal.Pink, 6, .5f, .06f);
                lp.Sphere(LP.M(new Vector3(s * .30f, 1.48f, -.30f), new Vector3(.16f, .12f, .06f)), Pal.Pink, 0, false);
                lp.Sphere(LP.M(new Vector3(s * .22f, 1.02f, -.48f), new Vector3(.24f, .30f, .24f)), Pal.Fur, 1, false);
                lp.Sphere(LP.M(new Vector3(s * .30f, .14f, -.30f), new Vector3(.34f, .2f, .42f)), Pal.FurDark, 1, false);
            }
            squirrelBody = MeshObj("SquirrelBody", squirrelT, lp.ToMesh("Squirrel"), Pal.Soft);

            lp.Clear();
            // 몸 옆으로 휘어 올라가 끝이 바깥으로 말리는 굵은 꼬리
            lp.Sphere(LP.M(new Vector3(-.15f, .25f, .10f), new Vector3(.78f, .9f, .7f)), Pal.Fur, 2, false);
            lp.Sphere(LP.M(new Vector3(-.45f, 1.0f, .25f), new Vector3(.95f, 1.1f, .78f)), Pal.Fur, 2, false);
            lp.Sphere(LP.M(new Vector3(-.62f, 1.75f, .18f), new Vector3(.9f, .88f, .76f)), Pal.FurDark, 2, false);
            lp.Sphere(LP.M(new Vector3(-.98f, 2.02f, .05f), new Vector3(.62f, .55f, .58f)), Pal.Fur, 2, false);
            lp.Sphere(LP.M(new Vector3(-1.2f, 1.9f, -.02f), new Vector3(.36f, .34f, .36f)), Pal.Belly, 1, false);
            squirrelTail = MeshObj("BentThickTail", squirrelT, lp.ToMesh("Tail"), Pal.Soft);
            squirrelTail.localPosition = new Vector3(-.2f, .2f, .38f);

            heldAcorn = MeshObj("RolledAcorn", squirrelT, acornMesh, Pal.Soft); heldAcorn.localPosition = new Vector3(0, 1.05f, -.68f); heldAcorn.localScale = Vector3.one * .42f;
            squirrelT.localPosition = new Vector3(.36f, .25f, .30f); squirrelT.localScale = Vector3.one * SqScale;
            squirrelT.localRotation = Quaternion.Euler(0, -18f, 0);
        }

        /// <summary>창 크기가 바뀔 때만: 지층·굵은 뿌리·기록 저장칸을 현재 화면 비율에 맞게 다시 짓는다.</summary>
        void RebuildWorldForLayout()
        {
            float worldW = cam.orthographicSize * 2f * Mathf.Max(.3f, cam.aspect);

            // 지하 절개면: 화면 전체를 덮는 비정형 흙·광물층.
            lp.Clear();
            int cols = Mathf.CeilToInt(worldW / 2.5f) + 2, rows = 10;
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++)
            {
                int k = r * 37 + c * 11;
                if ((k * 7 + 3) % 9 == 0) continue;
                float x01 = (c - .5f + ((k * 13) % 7) / 14f) / (cols - 2);
                float y01 = (r - .2f + ((k * 5) % 6) / 12f) / (rows - 1.6f);
                var p = At(x01, y01, DCanopy + ((k * 3) % 5) * .7f);
                float s = 2.6f + ((k * 17) % 9) * .2f;
                int col = (k % 7 == 0) ? Pal.Moss : (k % 3 == 0 ? Pal.LeafDark : Pal.Canopy);
                lp.Sphere(LP.M(p, new Vector3(k * 11 % 40, k * 7 % 90, k * 3 % 30), new Vector3(s * 1.32f, s * .70f, s)), col, 1, true, .20f, k);
            }
            // 양쪽 굵은 뿌리 기둥이 절개면을 잡는다.
            float tw = land ? 2.3f : 1.6f;
            Vector3 lb = At(land ? .015f : -.03f, -.05f, DTrunk), lt = At(land ? .05f : .0f, 1.05f, DTrunk);
            Vector3 rb = At(land ? .985f : 1.03f, -.05f, DTrunk), rt = At(land ? .955f : 1.0f, 1.05f, DTrunk);
            lp.Tube(lb, lt, tw, tw * .78f, Pal.Bark, 9); lp.Tube(rb, rt, tw * 1.05f, tw * .8f, Pal.Bark, 9);
            for (int k = 0; k < 6; k++)
            {
                Vector3 a = Vector3.Lerp(lb, lt, .12f + k * .15f), b = Vector3.Lerp(rb, rt, .08f + k * .16f);
                lp.Tube(a + new Vector3(.1f, 0, -tw * .9f), a + new Vector3(.4f, .9f, -tw * .9f), .09f, .05f, Pal.BarkDark, 5);
                lp.Tube(b + new Vector3(-.1f, 0, -tw * .9f), b + new Vector3(-.3f, 1.1f, -tw * .9f), .09f, .05f, Pal.BarkDark, 5);
            }
            canopyMf.sharedMesh = lp.ToMesh("Canopy", canopyMf.sharedMesh);

            // 뿌리 3줄: 표본 캡슐, 정렬 홈, 중앙값 수납 레일 + 기록 저장칸.
            lp.Clear();
            Branch(PouchBranchY, -.05f, 1.05f, .48f, .012f, 3);
            Branch(SortRowY() - (land ? .045f : .035f), land ? SortSlotX(0, 8) - .07f : -.05f, land ? SortSlotX(7, 8) + .07f : 1.05f, .30f, .006f, 5);
            Branch(RailYFraction(), -.05f, 1.05f, .42f, .004f, 7);
            for (int k = 0; k < 9; k++)
            {
                var p = At(.06f + k * .11f, PouchBranchY + .02f, DBranch - .2f);
                lp.Sphere(LP.M(p + Vector3.up * .25f, new Vector3(.7f, .35f, .55f)), k % 2 == 0 ? Pal.Good : Pal.BarkLight, 1, true, .2f, k + 50);
            }
            float storageY01 = land ? .07f : .085f;
            for (int i = 0; i < 8; i++)
            {
                var p = At(HutX(i), storageY01, DBranch + .5f);
                huts[i].position = p;
                lp.Box(LP.M(p + new Vector3(0, .32f, .05f), new Vector3(.95f, .78f, .85f)), Pal.LeafDeep);
                lp.Cyl(LP.M(p + new Vector3(0, .95f, .05f), new Vector3(0, 45, 0), new Vector3(1.45f, .55f, 1.3f)), Pal.Bark, 4, .5f, .04f);
                lp.Box(LP.M(p + new Vector3(0, .32f, -.38f), new Vector3(.66f, .6f, .04f)), Pal.BarkDark);
                lp.Sphere(LP.M(p + new Vector3(0, .26f, -.30f), new Vector3(.5f, .4f, .2f)), Pal.Acorn, 0);
                lp.Tube(p + new Vector3(0, -.08f, 0), p + new Vector3(0, -.9f, 0), .12f, .05f, Pal.Bark, 5);
                Vector3 q = i < 7 ? At(HutX(i + 1), storageY01, DBranch + .5f) : p + new Vector3(1.6f, 0, 0);
                ropes[i].position = (p + q) * .5f + new Vector3(0, .45f, -.3f);
                ropes[i].rotation = Quaternion.FromToRotation(Vector3.right, (q - p).normalized);
                ropes[i].localScale = new Vector3(Mathf.Max(.3f, (q - p).magnitude - .9f), 1f, 1f);
            }
            branchesMf.sharedMesh = lp.ToMesh("Branches", branchesMf.sharedMesh);

            birdHomeA = At(land ? .06f : .07f, PouchBranchY, DBranch - .6f) + Vector3.up * .55f;
            birdHomeB = At(land ? .93f : .92f, PouchBranchY, DBranch - .6f) + Vector3.up * .55f;
            frondBase[0] = At(land ? .02f : -.02f, .99f, DFront); frondBase[1] = At(land ? .98f : 1.02f, .99f, DFront);
            frondBase[2] = At(land ? .0f : -.07f, .40f, DFront); frondBase[3] = At(land ? 1.0f : 1.07f, .44f, DFront);
            for (int i = 0; i < fronds.Length; i++) { fronds[i].position = frondBase[i]; fronds[i].localScale = Vector3.one * (land ? 1.25f : .95f); }
            for (int i = 0; i < flecks.Length; i++) { flecks[i].rotation = cam.transform.rotation; flecks[i].localScale = Vector3.one * (3.5f + i * .6f); }
            if (flow == Flow.Title || flow == Flow.Opening) PositionPouchesForTitle(); else PositionPouches();
            basketT.position = At(.5f, RailYFraction(), DBasket) + Vector3.up * .32f;
            rootCore.position = basketT.position + Vector3.up * 1.65f;
        }

        float HutX(int i) => land ? .30f + i * .068f : .08f + i * .12f;

        void Branch(float y01, float x0, float x1, float radius, float sag, int seed)
        {
            const int segs = 6;
            Vector3 prev = At(x0, y01, DBranch);
            for (int i = 1; i <= segs; i++)
            {
                float u = i / (float)segs;
                float y = y01 - Mathf.Sin(u * Mathf.PI) * sag + ((i * seed) % 3 - 1) * .003f;
                Vector3 p = At(Mathf.Lerp(x0, x1, u), y, DBranch);
                lp.Tube(prev, p, radius * (1.05f - u * .15f), radius * (1.0f - u * .15f), i % 2 == 0 ? Pal.Bark : Pal.BarkLight, 8);
                if (i % 2 == 1 && i < segs)
                {
                    lp.Sphere(LP.M(p, new Vector3(radius * 2.4f, radius * 2.2f, radius * 2.3f)), Pal.Bark, 0, true, .15f, seed * 10 + i);
                    lp.Tube(p, p + new Vector3(.35f, .85f, .25f), radius * .38f, radius * .14f, Pal.BarkLight, 5);
                    lp.Sphere(LP.M(p + new Vector3(.45f, 1.1f, .3f), new Vector3(.9f, .55f, .7f)), Pal.LeafLight, 1, true, .2f, seed * 13 + i);
                }
                prev = p;
            }
        }

        void PositionPouches()
        {
            int count = current == null ? 5 : current.raw.Length;
            for (int i = 0; i < pouches.Length; i++)
            {
                bool on = i < count; pouches[i].gameObject.SetActive(on);
                if (!on) continue;
                float x = RawX(i, count);
                var knot = At(x, RawTagY + (land ? .085f : .07f) + ZigzagY(i, count), DPouch);
                pouches[i].position = knot; pouches[i].localScale = Vector3.one * .8f;
                pouches[i].localRotation = Quaternion.identity;
            }
        }

        void PositionPouchesForTitle()
        {
            for (int i = 0; i < pouches.Length; i++)
            {
                bool on = land ? i < 5 : (i < 6 && i != 2 && i != 3);
                pouches[i].gameObject.SetActive(on); if (!on) continue;
                float x = land ? .62f + i * .075f : .09f + i * .165f;
                pouches[i].position = At(x, PouchBranchY - .07f - (i % 2) * .03f, DPouch); pouches[i].localScale = Vector3.one * .9f;
            }
        }

        float ZigzagY(int i, int count) => (!land && count > 6 && i % 2 == 1) ? -.03f : 0f;

        // ───────────────────────── UI ─────────────────────────
        void BuildUi()
        {
            roundedSprite = MakeSprite(64, 16, 0);
            pillSprite = MakeSprite(64, 30, 0);
            ringSprite = MakeSprite(64, 0, 1);
            tagSprite = MakeSprite(64, 14, 2);
            var canvas = MgfText.Canvas;

            // 타이틀 — 지층에 박힌 뿌리 기록판 + 빛나는 씨앗 소켓
            titleRt = R("TitleWorldUi", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            signRt = R("OrchardSignPivot", titleRt, new Vector2(.5f, .855f), Vector2.zero, new Vector2(10, 10));
            Img(signRt, "SignRopeL", new Vector2(.5f, .5f), new Vector2(-96, -40), new Vector2(5, 80), Pal.C(Pal.Flax), roundedSprite);
            Img(signRt, "SignRopeR", new Vector2(.5f, .5f), new Vector2(96, -40), new Vector2(5, 80), Pal.C(Pal.Flax), roundedSprite);
            var board = Img(signRt, "OrchardSign", new Vector2(.5f, .5f), new Vector2(0, -138), new Vector2(316, 132), BarkDark, roundedSprite);
            Img(board.transform, "SignGrain", new Vector2(.5f, .5f), new Vector2(0, 3), new Vector2(302, 118), Bark, roundedSprite);
            Img(board.transform, "SignKnotL", new Vector2(.5f, .5f), new Vector2(-96, 50), new Vector2(16, 16), Pal.C(Pal.Flax), pillSprite);
            Img(board.transform, "SignKnotR", new Vector2(.5f, .5f), new Vector2(96, 50), new Vector2(16, 16), Pal.C(Pal.Flax), pillSprite);
            titleLogoShadow = Txt(board.transform, "도토리 낙하", new Vector2(.5f, .5f), new Vector2(2, 5), 52, BarkDark, 300);
            titleLogo = Txt(board.transform, "<color=#F4E3A6>도토리</color> <color=#F3B53B>낙하</color>", new Vector2(.5f, .5f), new Vector2(0, 11), 52, Paper, 300);
            titleLogo.outlineWidth = .2f; titleLogo.outlineColor = BarkDark;
            titleTag = Txt(board.transform, "중앙값으로 뿌리 금고를 열어라", new Vector2(.5f, .5f), new Vector2(0, -38), 16, Paper, 280);
            knotRt = R("StartKnotTag", titleRt, new Vector2(.5f, .2f), Vector2.zero, new Vector2(176, 64));
            knotRingImg = Img(knotRt, "KnotPulse", new Vector2(.5f, .5f), Vector2.zero, new Vector2(230, 120), Paper, ringSprite); knotRingRt = knotRingImg.rectTransform;
            Img(knotRt, "KnotTagBack", new Vector2(.5f, .5f), new Vector2(0, -3), new Vector2(178, 62), Pal.C(Pal.SackDark), pillSprite);
            Img(knotRt, "KnotTag", new Vector2(.5f, .5f), new Vector2(0, 1), new Vector2(172, 58), Paper, pillSprite);
            Img(knotRt, "KnotTie", new Vector2(.5f, .5f), new Vector2(-66, 1), new Vector2(20, 20), Pal.C(Pal.FlaxDark), pillSprite);
            titleCta = Txt(knotRt, "씨앗 투입", new Vector2(.5f, .5f), new Vector2(10, 1), 24, Ink, 150);
            titleMeta = Txt(titleRt, "", new Vector2(.5f, .03f), Vector2.zero, 13, Paper, 380);
            titleMeta.outlineWidth = .28f; titleMeta.outlineColor = BarkDark;

            // HUD
            hudRt = R("PlayHud", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            statusBg = Img(hudRt, "SeedStatusStrip", new Vector2(.5f, .957f), Vector2.zero, new Vector2(320, 48), new Color(Paper.r, Paper.g, Paper.b, .97f), pillSprite);
            scoreTxt = Txt(statusBg.transform, "0", new Vector2(.10f, .5f), Vector2.zero, 20, Ink, 60);
            roundTxt = Txt(statusBg.transform, "연습", new Vector2(.32f, .5f), Vector2.zero, 14, Ink, 92);
            firstTxt = Txt(statusBg.transform, "첫 0/0", new Vector2(.53f, .5f), Vector2.zero, 14, Ink, 76);
            for (int i = 0; i < lifeImgs.Length; i++) lifeImgs[i] = Img(statusBg.transform, "LatchLife" + i, new Vector2(.69f + i * .045f, .5f), Vector2.zero, new Vector2(11, 22), Acorn, pillSprite);
            timeTxt = Txt(statusBg.transform, "연습", new Vector2(.885f, .5f), Vector2.zero, 16, Bad, 70);

            goalBg = Img(hudRt, "GoalBanner", new Vector2(.5f, .905f), Vector2.zero, new Vector2(366, 38), new Color(BarkDark.r, BarkDark.g, BarkDark.b, .92f), pillSprite);
            goalTxt = Txt(goalBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 15, Paper, 350);
            goalTxt.enableAutoSizing = true; goalTxt.fontSizeMin = 11; goalTxt.fontSizeMax = 16;

            for (int i = 0; i < 8; i++)
            {
                rawStrings[i] = Img(hudRt, "TagString" + i, new Vector2(.5f, .5f), Vector2.zero, new Vector2(2, 26), Pal.C(Pal.SackDark), roundedSprite);
                rawCards[i] = Img(hudRt, "SeedPaperWeightTag" + i, new Vector2(.5f, .5f), Vector2.zero, new Vector2(48, 46), Paper, tagSprite);
                rawTexts[i] = Txt(rawCards[i].transform, "", new Vector2(.5f, .40f), Vector2.zero, 21, Ink, 52);
            }
            sortLabelTxt = Txt(hudRt, "", new Vector2(.5f, .628f), Vector2.zero, 13, Paper, 380);
            sortLabelTxt.outlineWidth = .28f; sortLabelTxt.outlineColor = BarkDark;
            for (int i = 0; i < 8; i++)
            {
                slotBgs[i] = Img(hudRt, "SortPeg" + i, new Vector2(.5f, .5f), Vector2.zero, new Vector2(44, 52), new Color(BarkDark.r, BarkDark.g, BarkDark.b, .5f), roundedSprite);
                slotOrdinals[i] = Txt(hudRt, (i + 1) + "번째", new Vector2(.5f, .5f), Vector2.zero, 10, Paper, 44);
                slotOrdinals[i].outlineWidth = .3f; slotOrdinals[i].outlineColor = BarkDark;
            }
            for (int i = 0; i < 8; i++)
            {
                sortCards[i] = Img(hudRt, "SortTag" + i, new Vector2(.5f, .5f), Vector2.zero, new Vector2(40, 46), Paper, roundedSprite);
                sortTexts[i] = Txt(sortCards[i].transform, "", new Vector2(.5f, .5f), Vector2.zero, 21, Ink, 40);
            }

            defRt = R("MedianDefinition", hudRt, new Vector2(.5f, .455f), Vector2.zero, new Vector2(360, 46)); defG = defRt.gameObject.AddComponent<CanvasGroup>();
            definitionBg = Img(defRt, "DefinitionLeaf", new Vector2(.5f, .5f), Vector2.zero, new Vector2(360, 46), new Color(LeafLight.r, LeafLight.g, LeafLight.b, .97f), pillSprite);
            definitionTxt = Txt(definitionBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 13, Ink, 340);
            definitionTxt.enableAutoSizing = true; definitionTxt.fontSizeMin = 10; definitionTxt.fontSizeMax = 15;

            railImg = Img(hudRt, "HalfGramRuler", new Vector2(.5f, .27f), Vector2.zero, new Vector2(334, 5), new Color(Paper.r, Paper.g, Paper.b, .85f), roundedSprite);
            for (int i = 0; i < ticks.Length; i++)
            {
                bool major = i % 2 == 0;
                ticks[i] = Img(hudRt, "RailTick" + i, new Vector2(.5f, .27f), Vector2.zero, new Vector2(major ? 4 : 2, major ? 22 : 12), major ? Paper : new Color(Paper.r, Paper.g, Paper.b, .8f), roundedSprite);
                if (major)
                {
                    tickLabels[i / 2] = Txt(hudRt, (1 + i / 2).ToString(), new Vector2(.5f, .235f), Vector2.zero, 17, Paper, 34);
                    tickLabels[i / 2].outlineWidth = .3f; tickLabels[i / 2].outlineColor = BarkDark;
                }
            }
            for (int i = 0; i < trail.Length; i++)
                trail[i] = Img(hudRt, "FlaxAfterimage" + i, new Vector2(.5f, .27f), Vector2.zero, new Vector2(48 - i * 5, 40 - i * 4), new Color(Acorn.r, Acorn.g, Acorn.b, .36f - i * .05f), pillSprite);
            handleImg = Img(hudRt, "BasketGrip", new Vector2(.5f, .27f), Vector2.zero, new Vector2(66, 52), Acorn, pillSprite); handleRt = handleImg.rectTransform;
            Img(handleImg.transform, "GripInner", new Vector2(.5f, .5f), new Vector2(0, 1), new Vector2(58, 44), Paper, pillSprite);
            railValueTxt = Txt(handleImg.transform, "1 g", new Vector2(.5f, .5f), new Vector2(0, 1), 21, Ink, 66);
            grabHintTxt = Txt(handleImg.transform, "← 잡고 끌기", new Vector2(2.0f, .5f), Vector2.zero, 13, Paper, 120);
            grabHintTxt.outlineWidth = .3f; grabHintTxt.outlineColor = BarkDark;
            guidePath = Img(hudRt, "GhostDragPath", new Vector2(.5f, .27f), Vector2.zero, new Vector2(220, 6), new Color(Paper.r, Paper.g, Paper.b, .9f), roundedSprite); guidePathRt = guidePath.rectTransform;
            guideRing = Img(hudRt, "GripPulse", new Vector2(.5f, .27f), Vector2.zero, new Vector2(110, 96), Paper, ringSprite); guideRingRt = guideRing.rectTransform;
            guideHand = Img(hudRt, "GhostTouch", new Vector2(.5f, .27f), Vector2.zero, new Vector2(30, 30), new Color(1f, 1f, 1f, .85f), pillSprite); guideHandRt = guideHand.rectTransform;
            Img(guideHand.transform, "GhostTouchRing", new Vector2(.5f, .5f), Vector2.zero, new Vector2(46, 46), new Color(BarkDark.r, BarkDark.g, BarkDark.b, .8f), ringSprite);
            guideTargetImg = Img(hudRt, "PracticeTargetBasket", new Vector2(.5f, .27f), Vector2.zero, new Vector2(86, 66), new Color(Good.r, Good.g, Good.b, .44f), pillSprite); guideTargetRt = guideTargetImg.rectTransform;
            Img(guideTargetImg.transform, "PracticeTargetRing", new Vector2(.5f, .5f), Vector2.zero, new Vector2(104, 84), new Color(Paper.r, Paper.g, Paper.b, .78f), ringSprite);
            var targetTxt = Txt(guideTargetImg.transform, "여기에 놓기", new Vector2(.5f, 1.05f), Vector2.zero, 12, Paper, 116);
            targetTxt.outlineWidth = .28f; targetTxt.outlineColor = BarkDark;

            revealRt = R("MedianEvidence", hudRt, new Vector2(.5f, .455f), Vector2.zero, new Vector2(360, 118)); revealG = revealRt.gameObject.AddComponent<CanvasGroup>();
            revealBg = Img(revealRt, "EvidenceSeedPaper", new Vector2(.5f, .5f), Vector2.zero, new Vector2(360, 118), new Color(Paper.r, Paper.g, Paper.b, .98f), roundedSprite);
            revealTitleTxt = Txt(revealBg.transform, "", new Vector2(.5f, .78f), Vector2.zero, 18, Good, 340);
            revealTitleTxt.enableAutoSizing = true; revealTitleTxt.fontSizeMin = 12; revealTitleTxt.fontSizeMax = 19;
            revealEvidenceTxt = Txt(revealBg.transform, "", new Vector2(.5f, .34f), Vector2.zero, 14, Ink, 340);
            revealEvidenceTxt.enableAutoSizing = true; revealEvidenceTxt.fontSizeMin = 10; revealEvidenceTxt.fontSizeMax = 15;

            toastRt = R("InputToast", hudRt, new Vector2(.5f, .455f), Vector2.zero, new Vector2(350, 46)); toastG = toastRt.gameObject.AddComponent<CanvasGroup>();
            var toastBg = Img(toastRt, "ToastBark", new Vector2(.5f, .5f), Vector2.zero, new Vector2(350, 46), new Color(BarkDark.r, BarkDark.g, BarkDark.b, .95f), pillSprite);
            toastTxt = Txt(toastBg.transform, "", new Vector2(.5f, .5f), Vector2.zero, 13, Paper, 330);
            toastTxt.enableAutoSizing = true; toastTxt.fontSizeMin = 10; toastTxt.fontSizeMax = 14;

            rippleImg = Img(canvas.transform, "TapRipple", Vector2.zero, Vector2.zero, new Vector2(72, 72), Paper, ringSprite); rippleRt = rippleImg.rectTransform; rippleRt.gameObject.SetActive(false);

            // 결과 — 가지에 매달린 저장고 문판
            endRt = R("StorehouseResult", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); endG = endRt.gameObject.AddComponent<CanvasGroup>();
            endPanelRt = R("ResultPivot", endRt, new Vector2(.5f, .86f), Vector2.zero, new Vector2(10, 10));
            Img(endPanelRt, "ResultRopeL", new Vector2(.5f, .5f), new Vector2(-110, -40), new Vector2(5, 80), Pal.C(Pal.Flax), roundedSprite);
            Img(endPanelRt, "ResultRopeR", new Vector2(.5f, .5f), new Vector2(110, -40), new Vector2(5, 80), Pal.C(Pal.Flax), roundedSprite);
            var door = Img(endPanelRt, "StorehouseDoor", new Vector2(.5f, .5f), new Vector2(0, -250), new Vector2(330, 350), Bark, roundedSprite);
            var doorIn = Img(door.transform, "DoorPaper", new Vector2(.5f, .5f), new Vector2(0, 12), new Vector2(306, 300), new Color(Paper.r, Paper.g, Paper.b, .98f), roundedSprite);
            endTitleTxt = Txt(doorIn.transform, "", new Vector2(.5f, .87f), Vector2.zero, 24, BarkDark, 290);
            endScoreTxt = Txt(doorIn.transform, "0", new Vector2(.5f, .64f), Vector2.zero, 54, Good, 290);
            endStatsTxt = Txt(doorIn.transform, "", new Vector2(.5f, .30f), Vector2.zero, 15, Ink, 290);
            endStatsTxt.enableAutoSizing = true; endStatsTxt.fontSizeMin = 11; endStatsTxt.fontSizeMax = 16;
            endStatsTxt.rectTransform.sizeDelta = new Vector2(290, 110);
            var restart = Img(door.transform, "RestartTag", new Vector2(.5f, 0f), new Vector2(0, 4), new Vector2(240, 60), Acorn, pillSprite);
            endTapTxt = Txt(restart.transform, "새 꾸러미 받기", new Vector2(.5f, .5f), Vector2.zero, 21, Ink, 230);
        }

        void Relayout(bool force)
        {
            float scale = Mathf.Max(.01f, MgfText.Canvas.scaleFactor);
            if (!force && lastW == Screen.width && lastH == Screen.height && Mathf.Abs(lastScale - scale) < .001f) return;
            lastW = Screen.width; lastH = Screen.height; lastScale = scale; land = MgfBridge.IsLandLayout;
            Vector2 logical = new Vector2(Screen.width / scale, Screen.height / scale);
            if (logical.x < 100 || logical.y < 100) logical = new Vector2(390, 844);
            logicalSize = logical; titleRt.sizeDelta = logical; hudRt.sizeDelta = logical; endRt.sizeDelta = logical;
            // 3D 한 단위가 UI 와 같은 비율로 보이도록 직교 크기를 캔버스 배율에 맞춘다(가로 화면은 UI 가 커지므로 세계도 확대).
            cam.orthographicSize = Mathf.Clamp(10f * (logical.y / Mathf.Max(1f, Screen.height)) * (Screen.height / 844f), 6.2f, 10f);

            RebuildWorldForLayout();

            float statusW = Mathf.Min(land ? 620f : 330f, logical.x - 64f);
            SetAnchor(statusBg.rectTransform, new Vector2(land ? .5f : (statusW * .5f + 8f) / logical.x, StatusY)); statusBg.rectTransform.sizeDelta = new Vector2(statusW, 48);
            float goalW = Mathf.Min(land ? 680f : 374f, logical.x - 16f);
            SetAnchor(goalBg.rectTransform, new Vector2(.5f, GoalY)); goalBg.rectTransform.sizeDelta = new Vector2(goalW, 38); goalTxt.rectTransform.sizeDelta = new Vector2(goalW - 22, 36);
            SetAnchor(sortLabelTxt.rectTransform, new Vector2(land ? (SortSlotX(0, 8) + SortSlotX(7, 8)) * .5f : .5f, SortLabelY));
            float sideW = Mathf.Min(300f, logical.x * .31f);
            float defW = land ? sideW : Mathf.Min(360f, logical.x - 30f);
            float defH = land ? 92f : 46f;
            SetAnchor(defRt, new Vector2(DefX, DefY)); defRt.sizeDelta = new Vector2(defW, defH); definitionBg.rectTransform.sizeDelta = new Vector2(defW, defH);
            definitionTxt.rectTransform.sizeDelta = new Vector2(defW - 26, defH - 8);
            float revW = land ? sideW : Mathf.Min(366f, logical.x - 20f);
            float revH = land ? 170f : 118f;
            SetAnchor(revealRt, new Vector2(DefX, DefY)); revealRt.sizeDelta = new Vector2(revW, revH); revealBg.rectTransform.sizeDelta = revealRt.sizeDelta;
            revealTitleTxt.rectTransform.sizeDelta = new Vector2(revW - 20, 40); revealEvidenceTxt.rectTransform.sizeDelta = new Vector2(revW - 24, revH * .55f);
            SetAnchor(toastRt, new Vector2(DefX, DefY));
            toastRt.sizeDelta = new Vector2(land ? sideW : Mathf.Min(350f, logical.x - 24f), land ? 64f : 46f);
            toastRt.GetChild(0).GetComponent<RectTransform>().sizeDelta = toastRt.sizeDelta;
            toastTxt.rectTransform.sizeDelta = toastRt.sizeDelta - new Vector2(20, 6);

            SetAnchor(railImg.rectTransform, new Vector2((RailLeftFraction() + RailRightFraction()) * .5f, RailYFraction()));
            railImg.rectTransform.sizeDelta = new Vector2(logical.x * (RailRightFraction() - RailLeftFraction()), 5);
            for (int i = 0; i < ticks.Length; i++)
            {
                float x = Mathf.Lerp(RailLeftFraction(), RailRightFraction(), i / 22f);
                SetAnchor(ticks[i].rectTransform, new Vector2(x, RailYFraction()));
                if (i % 2 == 0) SetAnchor(tickLabels[i / 2].rectTransform, new Vector2(x, RailYFraction() - (land ? .048f : .038f)));
            }

            // 타이틀 표찰은 꾸러미 가지에, 매듭 꼬리표는 바구니 앞에
            SetAnchor(signRt, new Vector2(land ? Mathf.Max(.27f, 172f / logical.x) : .5f, PouchBranchY));
            SetAnchor(knotRt, new Vector2(.5f, RailYFraction() - (land ? .085f : .07f)));
            SetAnchor(endPanelRt, new Vector2(.5f, PouchBranchY));
            PositionTags(); UpdateBasketUi();
        }

        void PositionTags()
        {
            int count = current == null || flow == Flow.Title ? 0 : current.raw.Length;
            float spread = Mathf.Clamp01(spreadClock / .16f);
            float ease = 1f - (1f - spread) * (1f - spread);
            for (int i = 0; i < 8; i++)
            {
                bool active = i < count;
                rawCards[i].gameObject.SetActive(active); rawStrings[i].gameObject.SetActive(active);
                sortCards[i].gameObject.SetActive(active); slotBgs[i].gameObject.SetActive(active); slotOrdinals[i].gameObject.SetActive(active);
                if (!active) continue;
                float rx = RawX(i, count), ry = RawTagY + ZigzagY(i, count);
                var a = new Vector2(Mathf.Lerp(.5f, rx, ease), ry);
                SetAnchor(rawCards[i].rectTransform, a);
                rawCards[i].rectTransform.localRotation = Quaternion.Euler(0, 0, (1f - ease) * (i - count * .5f) * 9f + Mathf.Sin(clock * 1.4f + i) * 1.5f);
                SetAnchor(rawStrings[i].rectTransform, new Vector2(a.x, ry + (land ? .04f : .032f)));
                float sx = SortSlotX(i, count), sy = SortRowY();
                SetAnchor(slotBgs[i].rectTransform, new Vector2(sx, sy));
                SetAnchor(slotOrdinals[i].rectTransform, new Vector2(sx, sy - (land ? .052f : .04f)));
                if (!(draggingTag && i == draggedTag)) SetAnchor(sortCards[i].rectTransform, new Vector2(sx, sy));
            }
        }

        static void SetAnchor(RectTransform rt, Vector2 anchor)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = Vector2.zero;
        }

        void ResetVisuals()
        {
            titleDrop = 0f; feedbackClock = 9f; guideBoost = 0f; clearRun = -1f; hitStop = 0f; rippleClock = toastClock = 0f; signSwing = 0f;
            shownScore = shownEndScore = 0f; shownSecond = -1; lastScoreShown = -1; brokenLatch = -1; pileShown = -1;
            for (int i = 0; i < ropes.Length; i++) ropes[i].gameObject.SetActive(false);
            for (int i = 0; i < shutters.Length; i++) ShutterPose(i, 0f);
            for (int i = 0; i < acornDrops.Length; i++) acornDrops[i].gameObject.SetActive(false);
            for (int i = 0; i < latches.Length; i++) latches[i].localRotation = Quaternion.identity;
            if (pileMf) pileMf.sharedMesh = pileMeshes[0];
        }

        void ShutterPose(int i, float open01)
        {
            // 왼쪽 경첩을 축으로 문짝을 연다.
            var hinge = new Vector3(-.31f, .36f, -.42f);
            var rot = Quaternion.Euler(0, -open01 * 105f, 0);
            shutters[i].localRotation = rot;
            shutters[i].localPosition = hinge + rot * new Vector3(.31f, 0, 0);
        }

        void ShowScreen()
        {
            bool title = flow == Flow.Title || flow == Flow.Opening;
            bool end = flow == Flow.End;
            if (flow == Flow.Title) titleG.alpha = 1f;
            else if (flow != Flow.Opening) { titleG.alpha = 0f; titleDrop = 0f; signRt.anchoredPosition = Vector2.zero; }
            titleG.blocksRaycasts = false;
            hudG.alpha = !title && !end ? 1f : 0f;
            revealG.alpha = flow == Flow.Reveal ? 1f : 0f;
            endG.alpha = end ? 1f : 0f;
            int best = PlayerPrefs.GetInt("dotori-nakha-best-first", 0);
            titleMeta.text = "중학교 1학년 · 자료의 정리와 해석 · " + (best > 0 ? "최고 첫 시도 " + best + "/8" : "저장고 8칸을 채워라");
            if (flow == Flow.Title) { PositionTags(); PositionPouchesForTitle(); }
            if (!title && !end) UpdateHudText();
        }

        void BeginTitleDrop()
        {
            titleDrop = .0001f; MgfFx.Punch(knotRt, .2f, .22f); MgfFx.Punch(basketT, .12f, .25f);
        }

        void LoadProblemVisual(DotoriProblem problem, bool practice)
        {
            spreadClock = 0f;
            if (practice)
                goalTxt.text = problem.id == "practice-odd"
                    ? "연습 · 크기순 1, 2, <color=#F3B53B>5</color>, 7, 9 → 바구니를 5 g에 끌어 놓으시오"
                    : "연습 · 가운데 두 값 4와 7의 평균 (4+7)÷2 → 5.5 g에 놓으시오";
            else goalTxt.text = "무게 자료의 중앙값을 구하시오.  <size=80%><color=#F3D98B>꾸러미 " + problem.raw.Length + "개 · 단위 g</color></size>";
            sortLabelTxt.text = practice && problem.id == "practice-even" ? "정렬 가지 · 이미 크기순으로 놓여 있다" : "정렬 가지 · 무게표를 끌어 직접 크기순으로";
            for (int i = 0; i < problem.raw.Length; i++)
            {
                rawTexts[i].text = problem.raw[i] + "<size=55%> g</size>";
                sortTexts[i].text = st.studentOrder[i].ToString();
            }
            revealG.alpha = 0f; guideBoost = practice ? 1.8f : 1.0f;
            PositionPouches(); PositionTags(); UpdateBasketUi(); UpdateHudText();
            for (int i = 0; i < problem.raw.Length; i++) MgfFx.Punch(pouches[i], .10f, .22f);
        }

        void UpdateHudText()
        {
            roundTxt.text = IsPractice ? "연습" : "꾸러미 " + Mathf.Min(8, st.solved + 1) + "/8";
            firstTxt.text = "첫 " + st.firstCorrect + "/" + st.attemptedRounds;
            string def = st.level >= 2 && !(IsPractice && current != null && current.id == "practice-odd")
                ? (land ? "중앙값: 크기순 가운데 값\n짝수 개이면 가운데 두 값의 평균" : "중앙값: 크기순 가운데 값 · 짝수 개이면 가운데 두 값의 평균")
                : "중앙값: 크기순으로 늘어놓았을 때 가운데 값";
            if (definitionTxt.text != def) definitionTxt.text = def;
            defG.alpha = st.level >= 3 && !IsPractice ? 0f : 1f;
            for (int i = 0; i < lifeImgs.Length; i++) lifeImgs[i].color = i < st.lives ? Acorn : new Color(Bad.r, Bad.g, Bad.b, .30f);
            for (int i = 0; i < latches.Length; i++) latches[i].localRotation = i < st.lives ? Quaternion.identity : Quaternion.Euler(-70f, 0, 0);
            for (int i = 0; i < ropes.Length; i++) ropes[i].gameObject.SetActive(i < st.ropeSegments);
            for (int i = 0; i < shutters.Length; i++) ShutterPose(i, i < st.solved && !IsPractice ? 1f : 0f);
            int pile = Mathf.Clamp(IsPractice ? 1 : st.solved, 0, 6);
            if (pile != pileShown) { pileShown = pile; pileMf.sharedMesh = pileMeshes[pile]; }
        }

        void BoostGuide(bool strong)
        {
            guideBoost = strong ? 2.0f : 1.1f; toastClock = 1.6f;
            toastTxt.text = IsPractice ? "바구니 손잡이를 잡고 목표 눈금까지 끌어 놓으시오" : "중앙값을 정했으면 손잡이를 끌어 그 눈금에서 놓으시오";
        }

        void PointAtSortRow()
        {
            for (int i = 0; i < st.studentOrder.Length && i < sortCards.Length; i++) MgfFx.Punch(sortCards[i].rectTransform, .10f, .22f);
        }

        void BeginRevealVisual(bool correct, bool practice, int value2, string misconception)
        {
            feedbackClock = 0f; feedbackGood = correct; guideBoost = 0f; toastClock = 0f;
            revealTitleTxt.text = correct ? (practice ? "중앙값 뿌리가 연결됐다" : "새 기록 칸 생성!") : "걸쇠가 꺾였다 · " + DotoriRules.FormatHalf(value2) + " g는 중앙값이 아니다";
            revealTitleTxt.color = correct ? Good : Bad;
            revealEvidenceTxt.text = correct
                ? DotoriRules.Evidence(current)
                : DotoriRules.MisconceptionKorean(misconception) + "\n" + DotoriRules.Evidence(current);
            float bx = RailXFor(value2);
            int count = current.raw.Length; float best = 9f; dropFrom = 0;
            for (int i = 0; i < count; i++) { float d = Mathf.Abs(RawX(i, count) - bx); if (d < best) { best = d; dropFrom = i; } }
            if (correct)
            {
                MgfSfx.Play("correct", .46f); MgfFx.Punch(handleRt, .16f, .28f);
                for (int i = 0; i < acornDrops.Length; i++) acornDrops[i].gameObject.SetActive(true);
                MgfFx.Punch(pouches[dropFrom], .25f, .3f);
                Vector3 splitAt = At(bx, RailYFraction(), DBasket - .2f) + Vector3.up * 1.18f;
                rootSplitLeft.position = splitAt + Vector3.left * .72f; rootSplitRight.position = splitAt + Vector3.right * .72f;
                rootSplitLeft.localScale = rootSplitRight.localScale = Vector3.one;
                recordTablet.position = splitAt + Vector3.back * .28f; recordTablet.localScale = Vector3.zero;
                rootSplitLeft.gameObject.SetActive(true); rootSplitRight.gameObject.SetActive(true); recordTablet.gameObject.SetActive(true);
            }
            else
            {
                brokenLatch = practice ? -1 : Mathf.Clamp(st.lives, 0, 2);
                MgfSfx.Play("wrong", .30f); if (!practice) MgfFx.Shake(cam, .10f, .22f);
            }
            UpdateHudText();
            if (!correct && brokenLatch >= 0) latches[brokenLatch].localRotation = Quaternion.identity;
        }

        void FinishRevealVisual(bool correct)
        {
            feedbackClock = 9f; basketBody.localScale = Vector3.one; squirrelT.localScale = Vector3.one * SqScale; squirrelBody.localPosition = Vector3.zero;
            for (int i = 0; i < acornDrops.Length; i++) acornDrops[i].gameObject.SetActive(false);
            rootSplitLeft.gameObject.SetActive(false); rootSplitRight.gameObject.SetActive(false); recordTablet.gameObject.SetActive(false);
            UpdateHudText();
        }

        void RestoreAfterWrong() { revealG.alpha = 0f; guideBoost = .6f; UpdateBasketUi(); }

        void BeginBasketDrag()
        {
            for (int i = 0; i < trailU.Length; i++) trailU[i] = ValueU(SelectedValue2);
            handleRt.localScale = new Vector3(1.12f, .88f, 1f); MgfSfx.Play("tap", .18f); UpdateBasketUi();
        }

        void UpdateBasketDrag()
        {
            for (int i = trailU.Length - 1; i > 0; i--) trailU[i] = trailU[i - 1];
            trailU[0] = ValueU(SelectedValue2); UpdateBasketUi();
            MgfSfx.Play("tap", .05f);
        }

        void EndBasketDrag() { handleRt.localScale = Vector3.one; }

        void BeginTagDrag(int index)
        {
            if (index < 0) return;
            sortCards[index].rectTransform.localScale = Vector3.one * 1.18f; sortCards[index].transform.SetAsLastSibling();
        }

        void UpdateStudentOrder()
        {
            for (int i = 0; i < st.studentOrder.Length; i++) sortTexts[i].text = st.studentOrder[i].ToString();
            for (int i = 0; i < sortCards.Length; i++) sortCards[i].rectTransform.localScale = i == draggedTag ? Vector3.one * 1.18f : Vector3.one;
            if (draggedTag >= 0) sortCards[draggedTag].transform.SetAsLastSibling();
            PositionTags();
        }

        void EndTagDrag(int index)
        {
            for (int i = 0; i < sortCards.Length; i++) sortCards[i].rectTransform.localScale = Vector3.one;
            PositionTags();
        }

        void Ripple(Vector2 screen, string message)
        {
            float scale = Mathf.Max(.01f, MgfText.Canvas.scaleFactor);
            rippleRt.anchoredPosition = screen / scale;
            rippleRt.localScale = Vector3.one * .35f; rippleRt.gameObject.SetActive(true); rippleClock = .30f;
            toastClock = 1.45f; toastTxt.text = message;
        }

        void ShowEndVisual(bool clear, string reason)
        {
            endTitleTxt.text = clear ? "뿌리 기록고 완성" : reason == "latches" ? "걸쇠가 모두 꺾였다" : reason == "time" ? "기록 시간이 끝났다" : "첫 시도 6번이 필요하다";
            endScoreTxt.text = "0"; shownEndScore = 0f; lastEndShown = 0;
            string mis = MostCommonMistake();
            endStatsTxt.text = "꾸러미 " + st.solved + "/8 · 첫 시도 " + st.firstCorrect + "/" + st.attemptedRounds + " · 걸쇠 " + st.lives
                + (mis.Length > 0 ? "\n<color=#A63E2D>되짚기: " + mis + "</color>" : st.attemptedRounds > 0 ? "\n<color=#4E7A2C>크기순 가운데를 정확히 잡았다</color>" : "");
            signSwing = 1f;
            clearRun = clear ? 0f : -1f;
            if (clear) { for (int i = 0; i < ropes.Length; i++) ropes[i].gameObject.SetActive(true); acornDrops[0].gameObject.SetActive(true); }
        }

        // 클리어: 도토리 하나가 이번 판에 이은 운반 밧줄 전 구간을 굴러가며 셔터를 차례로 연다.
        void TickClearRun(float dt)
        {
            if (clearRun < 0f) return;
            clearRun += dt;
            float u = Mathf.Clamp01(clearRun / 2.4f) * 7f;
            int seg = Mathf.Min(6, Mathf.FloorToInt(u));
            float f = u - seg;
            Vector3 a = huts[seg].position + new Vector3(0, .75f, -.35f), b = huts[seg + 1].position + new Vector3(0, .75f, -.35f);
            acornDrops[0].position = Vector3.Lerp(a, b, f) + Vector3.up * Mathf.Abs(Mathf.Sin(f * Mathf.PI)) * .45f;
            acornDrops[0].localRotation = Quaternion.Euler(0, 0, -clearRun * 540f);
            for (int i = 0; i < shutters.Length; i++) ShutterPose(i, Mathf.Clamp01(u - i + .5f));
            if (clearRun > 2.6f) { clearRun = -1f; acornDrops[0].gameObject.SetActive(false); MgfSfx.Play("win", .3f); }
        }

        void TickView(float dt)
        {
            clock += dt;
            TickClearRun(dt);
            if (spreadClock < 1f) { spreadClock += dt; PositionTags(); }
            else if (flow != Flow.Title) for (int i = 0; i < 8; i++) if (rawCards[i].gameObject.activeSelf)
                rawCards[i].rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(clock * 1.4f + i) * 1.5f);
            if (draggingTag && draggedTag >= 0 && draggedTag < sortCards.Length)
                SetAnchor(sortCards[draggedTag].rectTransform, new Vector2(Mathf.Clamp01(MgfPointer.Position.x / Mathf.Max(1f, Screen.width)), SortRowY() + .012f));

            // 타이틀: 표찰 흔들림, 매듭 맥동, 첫 탭 뒤 표찰이 가지 위로 감겨 올라감
            if (flow == Flow.Title || flow == Flow.Opening)
            {
                signRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(clock * 1.3f) * 2.2f);
                float pr = Mathf.Repeat(clock * .9f, 1f);
                knotRingRt.localScale = Vector3.one * (.8f + .3f * pr);
                knotRingImg.color = new Color(1f, .96f, .8f, 1f - pr);
                knotRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(clock * 2.1f) * 3f);
            }
            if (titleDrop > 0f)
            {
                titleDrop += dt; float k = Mathf.Clamp01(titleDrop / .30f);
                titleG.alpha = 1f - k; signRt.anchoredPosition = new Vector2(0, k * k * 260f);
                if (k >= 1f) { titleDrop = 0f; signRt.anchoredPosition = Vector2.zero; titleG.alpha = 0f; }
            }
            if (flow == Flow.End)
            {
                signSwing = Mathf.Max(0f, signSwing - dt * .8f);
                endPanelRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(clock * 5f) * 6f * signSwing);
            }

            // 지하 절개면의 상시 움직임: 전경 뿌리혹, 포자, 광물 반사.
            for (int i = 0; i < fronds.Length; i++)
            {
                float a = clock * (.6f + i * .07f) + i * 1.9f;
                fronds[i].position = frondBase[i] + new Vector3(Mathf.Sin(a) * .18f, Mathf.Cos(a * 1.3f) * .1f, 0);
                fronds[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(a * .8f) * 3f);
            }
            for (int i = 0; i < fallingLeaves.Length; i++)
            {
                float period = 7f + i * 1.3f, u = Mathf.Repeat(clock / period + i * .23f, 1f);
                float x01 = Mathf.Repeat(.12f + i * .17f + Mathf.Sin(u * 6.2f + i) * .04f, 1f);
                fallingLeaves[i].position = At(x01, 1.05f - u * 1.15f, DFront + 1f);
                fallingLeaves[i].localRotation = Quaternion.Euler(Mathf.Sin(u * 9f + i) * 40f, u * 300f + i * 60f, Mathf.Cos(u * 7f) * 35f);
            }
            for (int i = 0; i < flecks.Length; i++)
            {
                float a = clock * .07f + i * 1.7f;
                flecks[i].position = At(Mathf.Repeat(.1f + i * .21f + Mathf.Sin(a) * .05f, 1f), .2f + Mathf.Repeat(i * .29f + Mathf.Cos(a * .7f) * .05f, .8f), DCanopy - 1.2f);
            }
            rootCore.position = basketT.position + Vector3.up * 1.65f;
            float corePulse = .82f + Mathf.Sin(clock * 2.8f) * .08f;
            rootCore.localScale = Vector3.one * corePulse;
            rootCore.localRotation = Quaternion.Euler(clock * 17f, clock * 23f, 0f);

            if (feedbackClock < 1.4f)
            {
                // 도토리가 바구니에 닿는 순간 70ms 멈춤(hit-stop) 뒤 탄성 복원
                if (hitStop > 0f) hitStop -= dt; else feedbackClock += dt;
                if (feedbackGood)
                {
                    // 수납 테두리 오므라듦 → 씨앗 낙하 → 뿌리층 분리와 기록판 성장.
                    float k = feedbackClock;
                    float squeeze = k < .15f ? Mathf.SmoothStep(0, 1, k / .15f) : Mathf.Clamp01(1f - (k - .15f) / .25f);
                    float spring = k > .62f ? Mathf.Sin((k - .62f) * 22f) * Mathf.Exp(-(k - .62f) * 6f) : 0f;
                    basketBody.localScale = new Vector3(1f - squeeze * .07f + spring * .05f, 1f - spring * .08f + (k > .58f && k < .66f ? -.06f : 0f), 1f - squeeze * .07f);
                    float split = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - .42f) / .34f));
                    Vector3 center = (rootSplitLeft.position + rootSplitRight.position) * .5f;
                    rootSplitLeft.position = center + Vector3.left * (.72f + split * .72f);
                    rootSplitRight.position = center + Vector3.right * (.72f + split * .72f);
                    float grow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((k - .56f) / .34f));
                    recordTablet.localScale = new Vector3(grow, grow, grow);
                    recordTablet.position += Vector3.up * dt * (grow < 1f ? .35f : 0f);
                    Vector3 from = pouches[dropFrom].position + Vector3.down * .5f, to = basketT.position + Vector3.up * .9f;
                    for (int i = 0; i < acornDrops.Length; i++)
                    {
                        float u = Mathf.Clamp01((k - .10f - i * .03f) / .42f);
                        Vector3 off = new Vector3(((i % 3) - 1) * .32f, (i / 3) * .1f, ((i % 2) - .5f) * .3f);
                        acornDrops[i].position = Vector3.Lerp(from + off * .4f, to + off, u * u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * .8f;
                        acornDrops[i].localRotation = Quaternion.Euler(u * 300f + i * 40f, 0, u * 520f + i * 19f);
                        bool vis = u < 1f;
                        if (acornDrops[i].gameObject.activeSelf != vis) acornDrops[i].gameObject.SetActive(vis);
                    }
                    if (k - dt < .52f && k >= .52f && hitStop <= 0f) { hitStop = .07f; MgfSfx.Play("pop", .35f); MgfFx.Glow(basketT.position + Vector3.up * 1.0f, Pal.C(Pal.Sunny), 10, .45f); }
                    if (k - dt < .62f && k >= .62f) { pileShown = -1; UpdateHudText(); }
                }
                else
                {
                    // 오답: 낙하 전 걸쇠 하나만 실제로 꺾인다(흔들림은 오답 전용).
                    float k = Mathf.Clamp01(feedbackClock / .45f);
                    if (brokenLatch >= 0) latches[brokenLatch].localRotation = Quaternion.Euler(-Mathf.Lerp(0f, 70f, Mathf.SmoothStep(0, 1, k)), 0, Mathf.Sin(k * 20f) * 8f * (1f - k));
                }
            }

            shownScore = Mathf.MoveTowards(shownScore, st.score, dt * Mathf.Max(160f, Mathf.Abs(st.score - shownScore) * 6f));
            int shownScoreInt = Mathf.RoundToInt(shownScore);
            if (shownScoreInt != lastScoreShown) { lastScoreShown = shownScoreInt; scoreTxt.text = shownScoreInt.ToString(); }
            if (flow == Flow.Playing || (flow == Flow.Reveal && !revealPractice))
            {
                int sec = Mathf.CeilToInt(timeLeft);
                if (sec != shownSecond) { shownSecond = sec; timeTxt.text = sec + "초"; timeTxt.color = sec <= 20 ? Bad : Ink; }
            }
            else if (IsPractice && shownSecond != -2) { shownSecond = -2; timeTxt.text = "멈춤"; timeTxt.color = Good; }
            if (flow == Flow.End)
            {
                shownEndScore = Mathf.MoveTowards(shownEndScore, st.score, dt * Mathf.Max(120f, st.score * 1.6f));
                int e = Mathf.RoundToInt(shownEndScore);
                if (e != lastEndShown) { lastEndShown = e; endScoreTxt.text = e.ToString(); }
            }

            // 안내: 연습은 고정 목표까지 경로, 실전은 손잡이 좌우 흔들기만(정답 위치 누설 금지)
            bool practiceGuide = IsPractice;
            guideBoost = practiceGuide ? Mathf.Max(.4f, guideBoost - dt * .15f) : Mathf.Max(0f, guideBoost - dt * .5f);
            bool show = guideBoost > 0f && !draggingBasket && !draggingTag && (flow == Flow.Playing || practiceGuide);
            if (guideRing.gameObject.activeSelf != show) { guideRing.gameObject.SetActive(show); guidePath.gameObject.SetActive(show); guideHand.gameObject.SetActive(show); }
            bool hint = show && practiceGuide;
            if (grabHintTxt.gameObject.activeSelf != hint) grabHintTxt.gameObject.SetActive(hint);
            if (guideTargetImg.gameObject.activeSelf != practiceGuide) guideTargetImg.gameObject.SetActive(practiceGuide);
            if (practiceGuide && current != null)
            {
                SetAnchor(guideTargetRt, new Vector2(RailXFor(current.answer2), RailYFraction()));
                float targetPulse = 1f + .06f * Mathf.Sin(clock * 5f);
                guideTargetRt.localScale = Vector3.one * targetPulse;
            }
            if (show)
            {
                float from = RailXFor(SelectedValue2);
                float to = practiceGuide && current != null ? RailXFor(current.answer2)
                    : Mathf.Clamp(from + (from > .5f ? -.12f : .12f), RailLeftFraction(), RailRightFraction());
                float pulse = .9f + .16f * Mathf.Sin(clock * 5f);
                SetAnchor(guideRingRt, new Vector2(from, RailYFraction())); guideRingRt.localScale = Vector3.one * pulse * Mathf.Clamp(guideBoost, .8f, 1.3f);
                float travel = practiceGuide ? Mathf.SmoothStep(0, 1, Mathf.Clamp01(Mathf.Repeat(clock * .7f, 1.25f))) : .5f - .5f * Mathf.Cos(clock * 3.2f);
                SetAnchor(guideHandRt, new Vector2(Mathf.Lerp(from, to, travel), RailYFraction() - (land ? .075f : .062f)));
                guideHandRt.localScale = Vector3.one * Mathf.Clamp(guideBoost, 1f, 1.5f);
                SetAnchor(guidePathRt, new Vector2((from + to) * .5f, RailYFraction() + (land ? .028f : .022f)));
                guidePathRt.sizeDelta = new Vector2(Mathf.Max(12f, Mathf.Abs(to - from) * logicalSize.x), 6);
                guidePath.color = new Color(Paper.r, Paper.g, Paper.b, .55f + .35f * Mathf.Sin(clock * 6f));
            }

            if (rippleClock > 0f)
            {
                rippleClock -= dt; float k = 1f - rippleClock / .30f;
                rippleRt.localScale = Vector3.one * Mathf.Lerp(.35f, 1.6f, k); rippleImg.color = new Color(Paper.r, Paper.g, Paper.b, 1f - k);
                if (rippleClock <= 0f) rippleRt.gameObject.SetActive(false);
            }
            toastG.alpha = toastClock > 0f ? Mathf.Clamp01(toastClock * 3f) : 0f;
            if (toastClock > 0f) toastClock -= dt;
            bool defOn = flow != Flow.Reveal && toastClock <= 0f;
            if (defRt.gameObject.activeSelf != defOn) defRt.gameObject.SetActive(defOn);
            UpdateBasketUi();
        }

        float DragLean()
        {
            float target = RailXFor(SelectedValue2), now = Mathf.Lerp(RailLeftFraction(), RailRightFraction(), trailU[1]);
            return Mathf.Clamp((now - target) * 120f, -10f, 10f);
        }

        void UpdateBasketUi()
        {
            float x = RailXFor(SelectedValue2); SetAnchor(handleRt, new Vector2(x, RailYFraction()));
            if (SelectedValue2 != lastRailShown) { lastRailShown = SelectedValue2; railValueTxt.text = DotoriRules.FormatHalf(SelectedValue2) + "<size=60%> g</size>"; }
            if (!draggingBasket) for (int i = 0; i < trailU.Length; i++) trailU[i] = ValueU(SelectedValue2);
            for (int i = 0; i < trail.Length; i++)
            {
                SetAnchor(trail[i].rectTransform, new Vector2(Mathf.Lerp(RailLeftFraction(), RailRightFraction(), trailU[i]), RailYFraction()));
                if (trail[i].gameObject.activeSelf != draggingBasket) trail[i].gameObject.SetActive(draggingBasket);
            }
            bool showGrip = flow != Flow.Title && flow != Flow.Opening && flow != Flow.End;
            if (handleImg.gameObject.activeSelf != showGrip) handleImg.gameObject.SetActive(showGrip);
            if (basketT && cam)
            {
                float bx = flow == Flow.Title || flow == Flow.Opening ? .5f : x;
                bool titleish = flow == Flow.Title || flow == Flow.Opening;
                Vector3 target = At(bx, RailYFraction(), DBasket) + Vector3.up * (titleish ? .45f : .62f);
                if (flow != Flow.Reveal) basketT.position = Vector3.Lerp(basketT.position, target, draggingBasket ? .6f : .35f);
                float bs = titleish ? 1.75f : 1.12f;
                basketT.localScale = Vector3.one * Mathf.Lerp(basketT.localScale.x, bs, .2f);
                basketT.rotation = Quaternion.Euler(0, 0, draggingBasket ? DragLean() * .6f : 0f);
            }
        }

        float ValueU(int value2) => Mathf.Clamp01((value2 - 2) / 22f);
        float RailXFor(int value2) => Mathf.Lerp(RailLeftFraction(), RailRightFraction(), ValueU(value2));
    }
}
