// 꿀 도둑 — 코드로 만든 밀랍 벌통 디오라마와 화면 UI.
// 배경 원화를 깔지 않고 같은 세계를 타이틀·플레이·결과에 계속 유지한다.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mgf;

namespace Mgf.KkulDoduk
{
    public partial class KkulDodukGame
    {
        static readonly Color Honey = new Color32(211, 137, 31, 255);
        static readonly Color HoneyLight = new Color32(255, 203, 70, 255);
        static readonly Color Amber = new Color32(83, 43, 19, 255);
        static readonly Color Raspberry = new Color32(188, 45, 69, 255);
        static readonly Color Cream = new Color32(255, 244, 201, 255);
        static readonly Color Ink = new Color32(65, 76, 53, 255);
        static readonly Color Leaf = new Color32(111, 128, 69, 255);
        static readonly Color Error = new Color32(153, 54, 64, 255);
        static readonly Color Metal = new Color32(55, 67, 74, 255);
        static readonly Color MetalLight = new Color32(214, 229, 224, 255);

        Camera cam;
        Vector3 cameraBase;
        Transform worldRoot, archRoot;
        readonly Transform[] bees = new Transform[6];
        readonly Transform[] beeWingsL = new Transform[6];
        readonly Transform[] beeWingsR = new Transform[6];
        readonly Vector3[] beeHomes = new Vector3[6];
        readonly Transform[] honeyDrops = new Transform[18];
        readonly Vector3[] dropHomes = new Vector3[18];
        Material waxMat, waxLightMat, amberMat, creamMat, raspberryMat, inkMat, leafMat, glowMat, metalMat;
        float worldClock, correctFxClock = 9f, wrongFxClock = 9f, hitStopClock;
        float beeReactClock = 9f;

        RectTransform uiRoot, titleRoot, gameRoot, endRoot, revealRoot;
        CanvasGroup titleGroup, gameGroup, endGroup, revealGroup;
        RectTransform titleSealL, titleSealR, titleSealRing, titleKnifeRail, titleKnifeBlade, restartSeal;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleSealText;
        TextMeshProUGUI hudText, goalText, promptText, leftShelfText, rightShelfText;
        TextMeshProUGUI selectedText, instructionText, recordText, toastText;
        TextMeshProUGUI revealTitle, revealText, endTitle, endStats, endReasonText, restartText;
        Image handleImage, bladeImage, guideRing, rippleImage, revealPlate;
        Image ghostFingerImage, ghostBladeImage, ghostMovePathImage, ghostCutPathImage, ghostArrowImage, wrongMarkerImage;
        RectTransform handleRt, bladeRt, guideRt, rippleRt, cutTrailRt, recordPlateRt;
        RectTransform hudPlateRt, promptPlateRt, leftPlateRt, rightPlateRt, ghostFingerRt, ghostBladeRt;
        RectTransform ghostMovePathRt, ghostCutPathRt, ghostArrowRt, wrongMarkerRt;
        TextMeshProUGUI ghostGuideText, wrongMarkerText;
        readonly RectTransform[] cellRt = new RectTransform[24];
        readonly Image[] cellImage = new Image[24];
        readonly TextMeshProUGUI[] tickText = new TextMeshProUGUI[24];
        readonly RectTransform[] sampleCards = new RectTransform[8];
        readonly Image[] sampleImages = new Image[8];
        Sprite softDisc, ringSprite, waxPanel, hexCellSprite, arrowSprite;
        bool layoutLand;
        int layoutW = -1, layoutH = -1;
        float rippleClock = 9f, toastClock = 9f, guideBoost, handlePunch, cutTrailClock = 9f, practiceGuideClock;
        Vector2 rippleScreen;
        string toastCache = "";
        float shownScore;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        RectTransform Full(string name, Transform parent)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        Image Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            image.sprite = sprite; if (sprite != null && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        TextMeshProUGUI Txt(string name, Transform parent, string text, Vector2 anchor, Vector2 pos,
            float size, Color color, float width, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R(name, parent, anchor, pos, new Vector2(width, size * 3.1f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.fontStyle = FontStyles.Bold; t.color = color;
            t.alignment = align; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false; t.text = text; return t;
        }

        Sprite MakeDisc(bool ring)
        {
            const int S = 96;
            var texture = new Texture2D(S, S, TextureFormat.RGBA32, false) {
                name = ring ? "Wax ripple ring" : "Soft wax disc", wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = (x + .5f) / S * 2f - 1f, dy = (y + .5f) / S * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = ring
                    ? Mathf.Clamp01((.96f - d) * 18f) * Mathf.Clamp01((d - .66f) * 18f)
                    : Mathf.Clamp01((.96f - d) * 14f);
                pixels[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        Sprite MakeWaxPanel()
        {
            const int S = 96;
            var texture = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Precision cut wax plate", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[S * S];
            const float radius = 5f;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - S * .5f) - (S * .5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + .5f - S * .5f) - (S * .5f - radius), 0f);
                float a = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                float grain = ((x * 13 + y * 7) % 19 == 0) ? .93f : 1f;
                pixels[y * S + x] = new Color(1f * grain, .98f * grain, .88f * grain, a);
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
        }

        Sprite MakeHexCell()
        {
            const int S = 96;
            var texture = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Faceted hex cell", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float px = Mathf.Abs((x + .5f) / S * 2f - 1f);
                float py = Mathf.Abs((y + .5f) / S * 2f - 1f);
                float edge = Mathf.Max(px * .8660254f + py * .5f, py);
                float a = Mathf.Clamp01((.94f - edge) * 22f);
                float facet = x < S / 2 ? .95f : 1f;
                pixels[y * S + x] = new Color(1f * facet, .98f * facet, .88f * facet, a);
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        Sprite MakeArrow()
        {
            const int S = 64;
            var texture = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Cut direction arrow", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float nx = (x + .5f) / S, ny = Mathf.Abs((y + .5f) / S - .5f) * 2f;
                bool shaft = nx < .58f && ny < .23f;
                bool head = nx >= .38f && ny < (nx - .30f) * .82f;
                pixels[y * S + x] = new Color32(255, 255, 255, (byte)((shaft || head) ? 255 : 0));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        static void Quad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int s = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            triangles.Add(s); triangles.Add(s + 1); triangles.Add(s + 2);
            triangles.Add(s); triangles.Add(s + 2); triangles.Add(s + 3);
        }

        void BuildAngularHoneycomb()
        {
            var vertices = new List<Vector3>(2304);
            var triangles = new List<int>(3456);
            for (int row = 0; row < 6; row++) for (int col = 0; col < 7; col++)
            {
                float cx = -8.0f + col * 2.64f + (row % 2) * 1.32f;
                float cy = .75f + row * 1.58f;
                float front = 4.02f - ((row + col) % 3) * .10f;
                float back = front + .38f;
                const float outer = 1.18f, inner = .84f;
                for (int side = 0; side < 6; side++)
                {
                    float a0 = Mathf.Deg2Rad * (30f + side * 60f);
                    float a1 = Mathf.Deg2Rad * (30f + (side + 1) * 60f);
                    Vector3 of0 = new Vector3(cx + Mathf.Cos(a0) * outer, cy + Mathf.Sin(a0) * outer, front);
                    Vector3 of1 = new Vector3(cx + Mathf.Cos(a1) * outer, cy + Mathf.Sin(a1) * outer, front);
                    Vector3 inf0 = new Vector3(cx + Mathf.Cos(a0) * inner, cy + Mathf.Sin(a0) * inner, front - .03f);
                    Vector3 inf1 = new Vector3(cx + Mathf.Cos(a1) * inner, cy + Mathf.Sin(a1) * inner, front - .03f);
                    Vector3 ob0 = new Vector3(of0.x, of0.y, back), ob1 = new Vector3(of1.x, of1.y, back);
                    Vector3 inb0 = new Vector3(inf0.x, inf0.y, back), inb1 = new Vector3(inf1.x, inf1.y, back);
                    Quad(vertices, triangles, of0, of1, inf1, inf0);
                    Quad(vertices, triangles, ob1, ob0, inb0, inb1);
                    Quad(vertices, triangles, of1, ob1, inb1, inf1);
                    Quad(vertices, triangles, ob0, of0, inf0, inb0);
                }
            }
            var mesh = new Mesh { name = "Single mesh angular wax cross-section" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject("Angular honeycomb cross-section", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(worldRoot, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = waxLightMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        void BuildWorld()
        {
            // CreatePrimitive가 내부에서 이름으로 콜라이더를 붙인다. WebGL 스트리핑이 타입을
            // 지우지 않도록 명시 참조를 남긴다(실제 입력 판정은 화면 좌표 기반이다).
            var colliderTypes = new GameObject("Collider type keeper");
            colliderTypes.AddComponent<CapsuleCollider>(); colliderTypes.AddComponent<SphereCollider>();
            colliderTypes.AddComponent<BoxCollider>(); colliderTypes.AddComponent<MeshCollider>();
            Object.Destroy(colliderTypes);
            MgfLook.Sky(MgfLook.Hex("#2A160C"), MgfLook.Hex("#713A17"), MgfLook.Hex("#D8891F"), .90f);
            MgfLook.Sun(new Vector3(-27, -28, 11), Cream, 1.28f, .72f);
            RenderSettings.ambientLight = new Color(.25f, .14f, .07f);
            // 열린 탁자형 무대가 아니라 밀랍 단면 안으로 카메라를 밀어 넣은 직교 매크로 시점.
            cam = MgfLook.Camera(new Vector3(6.4f, 5.7f, -14.2f), new Vector3(0, 2.8f, 1.6f), 31f);
            cam.orthographic = true; cam.orthographicSize = 7.5f;
            cam.backgroundColor = new Color32(42, 22, 12, 255); cameraBase = cam.transform.position;

            waxMat = MgfLook.Lit(Honey, .26f, .01f, Honey * .035f);
            waxLightMat = MgfLook.Lit(HoneyLight, .38f, .02f, HoneyLight * .055f);
            amberMat = MgfLook.Lit(Amber, .20f, .01f);
            creamMat = MgfLook.Lit(Cream, .35f, .01f);
            raspberryMat = MgfLook.Lit(Raspberry, .45f, .04f, Raspberry * .05f);
            inkMat = MgfLook.Lit(Ink, .14f, .01f);
            leafMat = MgfLook.Lit(Leaf, .18f, .01f);
            glowMat = MgfLook.Lit(new Color32(255, 216, 88, 255), .55f, .05f, new Color(.16f, .08f, .005f));
            metalMat = MgfLook.Lit(Metal, .82f, .72f, MetalLight * .018f);

            worldRoot = new GameObject("Macro wax cross-section tunnel").transform;
            MgfLook.Block("Deep wax core", new Vector3(0, 3.2f, 5.3f), new Vector3(28, 14.5f, 1.1f), .10f, amberMat, worldRoot);
            // 화면 가장자리까지 이어지는 날카로운 리브가 빈 바닥/무대를 없애고 단면 내부임을 고정한다.
            for (int i = 0; i < 8; i++)
            {
                float y = -2.0f + i * 1.75f;
                var leftRib = MgfLook.Block("Left cut rib " + i, new Vector3(-8.3f, y, 1.55f - i * .08f),
                    new Vector3(4.8f, .58f, 2.5f), .03f, i % 2 == 0 ? waxLightMat : waxMat, worldRoot);
                var rightRib = MgfLook.Block("Right cut rib " + i, new Vector3(8.3f, y, 1.55f - i * .08f),
                    new Vector3(4.8f, .58f, 2.5f), .03f, i % 2 == 0 ? waxMat : waxLightMat, worldRoot);
                leftRib.transform.localRotation = Quaternion.Euler(0, 10f, 7f);
                rightRib.transform.localRotation = Quaternion.Euler(0, -10f, -7f);
            }
            MgfLook.Block("Lower metal cutting bed", new Vector3(0, -2.15f, .25f), new Vector3(25, .48f, 4.8f), .02f, metalMat, worldRoot);
            MgfLook.Block("Upper metal cutting bed", new Vector3(0, 8.9f, 1.8f), new Vector3(25, .34f, 3.4f), .02f, metalMat, worldRoot);

            // 각진 밀랍 단면과 하나의 육각 격자 메시가 화면 중심까지 이어진다.
            BuildAngularHoneycomb();
            for (int i = 0; i < 7; i++)
            {
                float side = i < 4 ? -1f : 1f;
                float x = side * (7.9f - (i % 4) * .38f);
                float y = .25f + (i % 4) * 2.05f;
                var layer = MgfLook.Block("Faceted wax stratum " + i, new Vector3(x, y, 2.55f - i * .06f),
                    new Vector3(3.7f, 1.18f, 2.25f), .08f, i % 2 == 0 ? amberMat : waxMat, worldRoot);
                layer.transform.localRotation = Quaternion.Euler(0, side * (8f + i), side * (6f + i * 2f));
            }
            for (int i = 0; i < 5; i++)
            {
                float x = -5.4f + i * 2.7f;
                MgfLook.Block("Metal cutting guide " + i, new Vector3(x, .42f + (i % 2) * .22f, -.25f),
                    new Vector3(2.25f, .16f, .34f), .03f, metalMat, worldRoot);
            }

            archRoot = new GameObject("Carved entrance arch").transform; archRoot.SetParent(worldRoot, false);
            MgfLook.Block("Arch left", new Vector3(-5.9f, 2.8f, .3f), new Vector3(2.0f, 7.4f, 2.2f), .95f, waxLightMat, archRoot);
            MgfLook.Block("Arch right", new Vector3(5.9f, 2.8f, .3f), new Vector3(2.0f, 7.4f, 2.2f), .95f, waxLightMat, archRoot);
            MgfLook.Block("Arch crown", new Vector3(0, 6.2f, .3f), new Vector3(10.6f, 1.8f, 2.2f), .9f, waxLightMat, archRoot);
            for (int i = 0; i < 9; i++)
            {
                float x = -4.6f + i * 1.15f;
                var facet = MgfLook.Prim(PrimitiveType.Cylinder, "Crown hex bolt " + i,
                    new Vector3(x, 6.35f + Mathf.Cos(i * .7f) * .20f, -1.0f),
                    new Vector3(.22f, .09f, .22f), i % 3 == 0 ? raspberryMat : metalMat, archRoot, false);
                facet.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }

            // 매달린 꽃 선반과 꿀방울 배관.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 6.7f;
                MgfLook.Block("Hanging sample shelf", new Vector3(x, 4.4f, -1.2f), new Vector3(3.2f, .32f, 1.8f), .22f, amberMat, worldRoot);
                MgfLook.Prim(PrimitiveType.Cylinder, "Honey pipe", new Vector3(x, 6.2f, -1.0f), new Vector3(.12f, 2.0f, .12f), amberMat, worldRoot, false);
                for (int j = 0; j < 4; j++)
                {
                    float sx = x + (j - 1.5f) * .62f;
                    MgfLook.Prim(PrimitiveType.Sphere, "Flower sample", new Vector3(sx, 4.86f, -1.28f),
                        new Vector3(.32f, .22f, .32f), j % 2 == 0 ? raspberryMat : creamMat, worldRoot, false);
                    MgfLook.Prim(PrimitiveType.Cylinder, "Sample stem", new Vector3(sx, 4.62f, -1.22f),
                        new Vector3(.035f, .26f, .035f), leafMat, worldRoot, false);
                }
            }

            for (int i = 0; i < honeyDrops.Length; i++)
            {
                float x = -7.2f + (i % 9) * 1.8f;
                float y = 7.2f - (i / 9) * 2.4f - (i % 3) * .42f;
                dropHomes[i] = new Vector3(x, y, 3.15f);
                honeyDrops[i] = MgfLook.Prim(PrimitiveType.Sphere, "Slow honey drop " + i, dropHomes[i],
                    new Vector3(.13f, .30f, .13f), glowMat, worldRoot, false).transform;
            }

            for (int i = 0; i < bees.Length; i++)
            {
                beeHomes[i] = new Vector3(-5.2f + (i % 3) * 5.2f, .65f + (i / 3) * 2.45f, -1.2f + (i % 2) * .45f);
                bees[i] = BuildBee(i, beeHomes[i]);
            }
        }

        Transform BuildBee(int index, Vector3 position)
        {
            var root = new GameObject("Plump tasting bee " + index).transform;
            root.SetParent(worldRoot, false); root.localPosition = position;
            var body = MgfLook.Prim(PrimitiveType.Sphere, "Amber body", Vector3.zero,
                new Vector3(.72f, .55f, .58f), waxLightMat, root, false);
            body.transform.localPosition = Vector3.zero;
            MgfLook.Block("Dark stripe 1", new Vector3(-.16f, 0, -.52f), new Vector3(.12f, .55f, .09f), .05f, inkMat, root);
            MgfLook.Block("Dark stripe 2", new Vector3(.18f, 0, -.52f), new Vector3(.12f, .53f, .09f), .05f, inkMat, root);
            MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0, .05f, -.55f), new Vector3(.43f, .42f, .39f), amberMat, root, false);
            for (int side = -1; side <= 1; side += 2)
            {
                MgfLook.Prim(PrimitiveType.Sphere, "Eye", new Vector3(side * .14f, .13f, -.88f),
                    new Vector3(.055f, .07f, .045f), inkMat, root, false);
                MgfLook.Prim(PrimitiveType.Sphere, "Cheek", new Vector3(side * .23f, -.04f, -.86f),
                    new Vector3(.09f, .055f, .035f), raspberryMat, root, false);
            }
            beeWingsL[index] = MgfLook.Prim(PrimitiveType.Sphere, "Cream wing L", new Vector3(-.55f, .28f, .04f),
                new Vector3(.48f, .12f, .30f), creamMat, root, false).transform;
            beeWingsR[index] = MgfLook.Prim(PrimitiveType.Sphere, "Cream wing R", new Vector3(.55f, .28f, .04f),
                new Vector3(.48f, .12f, .30f), creamMat, root, false).transform;
            return root;
        }

        void BuildUi()
        {
            softDisc = MakeDisc(false); ringSprite = MakeDisc(true); waxPanel = MakeWaxPanel();
            hexCellSprite = MakeHexCell(); arrowSprite = MakeArrow();
            uiRoot = Full("Honey UI", MgfText.Canvas.transform);

            titleRoot = Full("Title", uiRoot); titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
            titleLogo = Txt("Carved logo", titleRoot, "꿀  도 둑", new Vector2(.5f, .76f), Vector2.zero, 55, Cream, 360);
            titleLogo.outlineColor = Amber; titleLogo.outlineWidth = .24f;
            titleTag = Txt("Tagline", titleRoot, "눈금을 맞춰 벌집을 베어라", new Vector2(.5f, .67f), Vector2.zero, 19, Cream, 340);
            var metaPlate = Img("Specimen tag", titleRoot, new Vector2(.5f, .57f), Vector2.zero, new Vector2(292, 72), Cream, waxPanel);
            titleMeta = Txt("Meta", metaPlate.transform, "중학교 2학년 · 경우의 수\n8회 시식 · 첫 절단 6회", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 270);
            titleSealL = Img("Seal half L", titleRoot, new Vector2(.5f, .24f), new Vector2(-39, 0), new Vector2(78, 128), Raspberry, waxPanel).rectTransform;
            titleSealR = Img("Seal half R", titleRoot, new Vector2(.5f, .24f), new Vector2(39, 0), new Vector2(78, 128), Raspberry, waxPanel).rectTransform;
            titleSealRing = Img("Seal ring", titleRoot, new Vector2(.5f, .24f), Vector2.zero, new Vector2(176, 176), Cream, ringSprite).rectTransform;
            titleSealText = Txt("Seal CTA", titleRoot, "봉인 열기", new Vector2(.5f, .24f), Vector2.zero, 21, Cream, 155);
            titleKnifeRail = Img("Title knife scoring rail", titleRoot, new Vector2(.5f, .24f), new Vector2(0, -102),
                new Vector2(228, 6), new Color(MetalLight.r, MetalLight.g, MetalLight.b, .72f), waxPanel).rectTransform;
            titleKnifeBlade = Img("Title moving knife", titleRoot, new Vector2(.5f, .24f), new Vector2(-92, -102),
                new Vector2(54, 22), MetalLight, arrowSprite).rectTransform;

            gameRoot = Full("Game", uiRoot); gameGroup = gameRoot.gameObject.AddComponent<CanvasGroup>();
            var hudPlate = Img("Wax HUD", gameRoot, new Vector2(.5f, .955f), Vector2.zero, new Vector2(326, 52), Cream, waxPanel);
            hudPlateRt = hudPlate.rectTransform;
            hudText = Txt("HUD text", hudPlate.transform, "", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 306);
            goalText = Txt("Persistent goal", gameRoot, "8회 시식 · 첫 절단 6회 · 봉인 3개", new Vector2(.5f, .895f), Vector2.zero, 14, Cream, 350);
            var promptPlate = Img("Prompt tag", gameRoot, new Vector2(.5f, .80f), Vector2.zero, new Vector2(354, 108), Cream, waxPanel);
            promptPlateRt = promptPlate.rectTransform;
            promptText = Txt("Prompt", promptPlate.transform, "", new Vector2(.5f, .5f), Vector2.zero, 15, Ink, 330);
            promptText.rectTransform.sizeDelta = new Vector2(330, 94);
            var leftPlate = Img("Left shelf tag", gameRoot, new Vector2(.24f, .69f), Vector2.zero, new Vector2(150, 62), HoneyLight, waxPanel);
            var rightPlate = Img("Right shelf tag", gameRoot, new Vector2(.76f, .69f), Vector2.zero, new Vector2(150, 62), HoneyLight, waxPanel);
            leftPlateRt = leftPlate.rectTransform; rightPlateRt = rightPlate.rectTransform;
            leftShelfText = Txt("Left shelf", leftPlate.transform, "", new Vector2(.5f, .5f), Vector2.zero, 14, Ink, 138);
            rightShelfText = Txt("Right shelf", rightPlate.transform, "", new Vector2(.5f, .5f), Vector2.zero, 14, Ink, 138);

            for (int i = 0; i < 24; i++)
            {
                cellImage[i] = Img("Honeycomb ruler cell " + (i + 1), gameRoot, new Vector2(.5f, .3f), Vector2.zero,
                    new Vector2(44, 28), i % 2 == 0 ? HoneyLight : Honey, hexCellSprite);
                cellRt[i] = cellImage[i].rectTransform;
                tickText[i] = Txt("Tick " + (i + 1), gameRoot, (i + 1).ToString(), new Vector2(.5f, .3f), Vector2.zero, 10, Cream, 34);
                tickText[i].gameObject.SetActive((i + 1) == 1 || (i + 1) % 4 == 0);
            }
            bladeImage = Img("Metal knife blade", gameRoot, new Vector2(.5f, .3f), Vector2.zero, new Vector2(174, 10), MetalLight, waxPanel);
            bladeRt = bladeImage.rectTransform;
            handleImage = Img("Faceted metal knife carriage", gameRoot, new Vector2(.5f, .3f), Vector2.zero, new Vector2(78, 64), Metal, hexCellSprite);
            handleRt = handleImage.rectTransform;
            guideRing = Img("Guide on actual handle", gameRoot, new Vector2(.5f, .3f), Vector2.zero, new Vector2(102, 102), Cream, ringSprite);
            guideRt = guideRing.rectTransform;
            selectedText = Txt("Immediate integer lens", gameRoot, "1칸", new Vector2(.5f, .3f), Vector2.zero, 21, Ink, 80);
            selectedText.outlineColor = Cream; selectedText.outlineWidth = .18f;
            cutTrailRt = Img("Knife honey trail", gameRoot, new Vector2(.5f, .3f), Vector2.zero, new Vector2(10, 10),
                new Color(Raspberry.r, Raspberry.g, Raspberry.b, .65f), waxPanel).rectTransform;
            cutTrailRt.gameObject.SetActive(false);

            ghostMovePathImage = Img("Practice move path", gameRoot, new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(10, 7), new Color(Cream.r, Cream.g, Cream.b, .64f), waxPanel);
            ghostMovePathRt = ghostMovePathImage.rectTransform;
            ghostCutPathImage = Img("Practice cut path", gameRoot, new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(10, 7), new Color(Raspberry.r, Raspberry.g, Raspberry.b, .82f), waxPanel);
            ghostCutPathRt = ghostCutPathImage.rectTransform;
            ghostBladeImage = Img("Ghost metal knife", gameRoot, new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(88, 7), new Color(MetalLight.r, MetalLight.g, MetalLight.b, .78f), waxPanel);
            ghostBladeRt = ghostBladeImage.rectTransform;
            ghostFingerImage = Img("Ghost finger", gameRoot, new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(36, 36), new Color(Cream.r, Cream.g, Cream.b, .82f), softDisc);
            ghostFingerRt = ghostFingerImage.rectTransform;
            ghostArrowImage = Img("Persistent cut direction arrow", gameRoot, new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(40, 22), new Color(Raspberry.r, Raspberry.g, Raspberry.b, .92f), arrowSprite);
            ghostArrowRt = ghostArrowImage.rectTransform;
            ghostGuideText = Txt("Animated input instruction", gameRoot, "손잡이부터: 눈금 이동 → 가로질러 베기",
                new Vector2(.5f, .54f), Vector2.zero, 14, Cream, 360);
            ghostGuideText.outlineColor = Amber; ghostGuideText.outlineWidth = .24f;

            var recordPlate = Img("Cut record tray", gameRoot, new Vector2(.5f, .12f), Vector2.zero, new Vector2(350, 100), Cream, waxPanel);
            recordPlateRt = recordPlate.rectTransform;
            recordText = Txt("First cut record", recordPlate.transform, "", new Vector2(.5f, .64f), Vector2.zero, 14, Ink, 328);
            instructionText = Txt("One action instruction", recordPlate.transform, "손잡이를 눈금으로 옮기고, 띠를 가로질러 베시오.",
                new Vector2(.5f, .26f), Vector2.zero, 13, Amber, 330);

            revealRoot = Full("Reveal", uiRoot); revealGroup = revealRoot.gameObject.AddComponent<CanvasGroup>();
            revealPlate = Img("Evidence tag", revealRoot, new Vector2(.5f, .18f), Vector2.zero, new Vector2(356, 136), Cream, waxPanel);
            revealTitle = Txt("Reveal heading", revealPlate.transform, "", new Vector2(.5f, .72f), Vector2.zero, 20, Ink, 330);
            revealText = Txt("Reveal formula", revealPlate.transform, "", new Vector2(.5f, .37f), Vector2.zero, 15, Amber, 330);
            wrongMarkerImage = Img("Correct answer ghost knife", revealRoot, new Vector2(.5f, .3f), Vector2.zero,
                new Vector2(104, 104), new Color(Cream.r, Cream.g, Cream.b, 0), ringSprite);
            wrongMarkerRt = wrongMarkerImage.rectTransform;
            wrongMarkerText = Txt("Correct answer marker", revealRoot, "", new Vector2(.5f, .3f), Vector2.zero, 16, Cream, 128);
            wrongMarkerText.color = new Color(Cream.r, Cream.g, Cream.b, 0);
            for (int i = 0; i < sampleCards.Length; i++)
            {
                sampleImages[i] = Img("Spread specimen " + i, revealRoot, new Vector2(.5f, .31f), Vector2.zero,
                    new Vector2(34, 42), i % 2 == 0 ? Raspberry : HoneyLight, waxPanel);
                sampleCards[i] = sampleImages[i].rectTransform;
            }

            rippleImage = Img("Tap wax ripple", uiRoot, new Vector2(.5f, .5f), Vector2.zero, new Vector2(70, 70), Cream, ringSprite);
            rippleRt = rippleImage.rectTransform; rippleImage.color = new Color(Cream.r, Cream.g, Cream.b, 0);
            toastText = Txt("Reason toast", uiRoot, "", new Vector2(.5f, .08f), Vector2.zero, 14, Cream, 350);

            endRoot = Full("End", uiRoot); endGroup = endRoot.gameObject.AddComponent<CanvasGroup>();
            endTitle = Txt("Result title", endRoot, "", new Vector2(.5f, .75f), Vector2.zero, 48, Cream, 370);
            endTitle.outlineColor = Amber; endTitle.outlineWidth = .20f;
            var resultPlate = Img("Result medallion", endRoot, new Vector2(.5f, .52f), Vector2.zero, new Vector2(328, 176), Cream, waxPanel);
            endStats = Txt("Animated result stats", resultPlate.transform, "", new Vector2(.5f, .64f), Vector2.zero, 22, Ink, 300);
            endReasonText = Txt("End reason", resultPlate.transform, "", new Vector2(.5f, .27f), Vector2.zero, 15, Amber, 300);
            restartSeal = Img("Restart wax seal", endRoot, new Vector2(.5f, .23f), Vector2.zero, new Vector2(168, 128), Raspberry, waxPanel).rectTransform;
            restartText = Txt("Restart CTA", endRoot, "다시 봉인 열기", new Vector2(.5f, .23f), Vector2.zero, 19, Cream, 160);

            LayoutUi();
        }

        bool UpdateLayoutIfNeeded()
        {
            bool land = IsLand();
            if (layoutW == Screen.width && layoutH == Screen.height && layoutLand == land) return false;
            layoutW = Screen.width; layoutH = Screen.height; layoutLand = land; LayoutUi();
            return true;
        }

        static void Anchor(RectTransform rt, Vector2 anchor)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
        }

        void LayoutUi()
        {
            if (uiRoot == null) return;
            layoutLand = IsLand();
            if (!layoutLand)
            {
                Anchor(titleLogo.rectTransform, new Vector2(.5f, .76f));
                Anchor(titleTag.rectTransform, new Vector2(.5f, .67f));
                Anchor(titleSealL, new Vector2(.5f, .41f)); Anchor(titleSealR, new Vector2(.5f, .41f));
                Anchor(titleSealRing, new Vector2(.5f, .41f)); Anchor(titleSealText.rectTransform, new Vector2(.5f, .41f));
                Anchor(titleKnifeRail, new Vector2(.5f, .41f)); Anchor(titleKnifeBlade, new Vector2(.5f, .41f));
                Anchor(hudPlateRt, new Vector2(.5f, .955f)); Anchor(goalText.rectTransform, new Vector2(.5f, .895f));
                hudPlateRt.sizeDelta = new Vector2(326, 52); goalText.gameObject.SetActive(true);
                Anchor(promptPlateRt, new Vector2(.5f, .80f));
                Anchor(leftPlateRt, new Vector2(.24f, .69f)); Anchor(rightPlateRt, new Vector2(.76f, .69f));
                Anchor(recordPlateRt, new Vector2(.5f, .12f)); Anchor(toastText.rectTransform, new Vector2(.5f, .08f));
                Anchor(ghostGuideText.rectTransform, new Vector2(.5f, .54f));
                Anchor(revealPlate.rectTransform, new Vector2(.5f, .18f));
                Anchor(restartSeal, new Vector2(.5f, .23f)); Anchor(restartText.rectTransform, new Vector2(.5f, .23f));
            }
            else
            {
                Anchor(titleLogo.rectTransform, new Vector2(.5f, .79f)); Anchor(titleTag.rectTransform, new Vector2(.5f, .68f));
                Anchor(titleSealL, new Vector2(.5f, .34f)); Anchor(titleSealR, new Vector2(.5f, .34f));
                Anchor(titleSealRing, new Vector2(.5f, .34f)); Anchor(titleSealText.rectTransform, new Vector2(.5f, .34f));
                Anchor(titleKnifeRail, new Vector2(.5f, .34f)); Anchor(titleKnifeBlade, new Vector2(.5f, .34f));
                // 2000×1045에서도 기하 평균 UI 스케일을 고려해 상·하단 반높이만큼 안쪽으로 둔다.
                Anchor(hudPlateRt, new Vector2(.5f, .90f)); hudPlateRt.sizeDelta = new Vector2(326, 66);
                Anchor(goalText.rectTransform, new Vector2(.5f, .84f)); goalText.gameObject.SetActive(false);
                Anchor(promptPlateRt, new Vector2(.5f, .64f));
                Anchor(leftPlateRt, new Vector2(.22f, .49f)); Anchor(rightPlateRt, new Vector2(.78f, .49f));
                Anchor(recordPlateRt, new Vector2(.5f, .16f)); Anchor(toastText.rectTransform, new Vector2(.5f, .19f));
                Anchor(ghostGuideText.rectTransform, new Vector2(.5f, .48f));
                Anchor(revealPlate.rectTransform, new Vector2(.5f, .18f));
                Anchor(restartSeal, new Vector2(.5f, .30f)); Anchor(restartText.rectTransform, new Vector2(.5f, .30f));
            }
            titleKnifeRail.anchoredPosition = new Vector2(0, -102);
            PositionTrack();
        }

        void PositionTrack()
        {
            if (cellRt[0] == null) return;
            for (int i = 0; i < 24; i++)
            {
                float t = i / 23f;
                if (layoutLand)
                {
                    Vector2 a = new Vector2(Mathf.Lerp(.22f, .78f, t), .36f);
                    cellRt[i].anchorMin = cellRt[i].anchorMax = a; cellRt[i].sizeDelta = new Vector2(44, 40);
                    tickText[i].rectTransform.anchorMin = tickText[i].rectTransform.anchorMax = new Vector2(a.x, .29f);
                }
                else
                {
                    Vector2 a = new Vector2(.5f, Mathf.Lerp(.22f, .67f, t));
                    cellRt[i].anchorMin = cellRt[i].anchorMax = a; cellRt[i].sizeDelta = new Vector2(70, 28);
                    tickText[i].rectTransform.anchorMin = tickText[i].rectTransform.anchorMax = new Vector2(.37f, a.y);
                }
                cellRt[i].anchoredPosition = Vector2.zero; cellRt[i].localRotation = Quaternion.identity;
                tickText[i].rectTransform.anchoredPosition = Vector2.zero;
            }
            MoveHandleVisual(st.selectedCellCount <= 0 ? 1 : st.selectedCellCount);
        }

        void MoveHandleVisual(int value)
        {
            if (handleRt == null) return;
            float t = (Mathf.Clamp(value, 1, 24) - 1) / 23f;
            Vector2 a = layoutLand ? new Vector2(Mathf.Lerp(.22f, .78f, t), .36f) : new Vector2(.5f, Mathf.Lerp(.22f, .67f, t));
            handleRt.anchorMin = handleRt.anchorMax = a; handleRt.anchoredPosition = Vector2.zero;
            bladeRt.anchorMin = bladeRt.anchorMax = a; bladeRt.anchoredPosition = Vector2.zero;
            guideRt.anchorMin = guideRt.anchorMax = a; guideRt.anchoredPosition = Vector2.zero;
            selectedText.rectTransform.anchorMin = selectedText.rectTransform.anchorMax = a;
            selectedText.rectTransform.anchoredPosition = layoutLand ? new Vector2(0, 62) : new Vector2(77, 0);
            if (layoutLand) { bladeRt.sizeDelta = new Vector2(10, 142); bladeRt.localRotation = Quaternion.identity; }
            else { bladeRt.sizeDelta = new Vector2(174, 10); bladeRt.localRotation = Quaternion.identity; }
            selectedText.text = value + "칸"; handlePunch = .16f;
        }

        void PlaceAtScreen(RectTransform rt, Vector2 screen)
        {
            float scale = Mathf.Max(.01f, ReferenceScale());
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.anchoredPosition = new Vector2((screen.x - Screen.width * .5f) / scale, (screen.y - Screen.height * .5f) / scale);
        }

        void PlaceSegment(RectTransform rt, Vector2 a, Vector2 b, float thickness)
        {
            float scale = Mathf.Max(.01f, ReferenceScale());
            Vector2 delta = (b - a) / scale;
            PlaceAtScreen(rt, (a + b) * .5f);
            rt.sizeDelta = new Vector2(Mathf.Max(8f, delta.magnitude), thickness);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        void UpdatePracticeGuide(float dt)
        {
            // 연습 뒤 첫 본 의뢰까지 같은 조작 문법을 유지한다. 본판에서는 정답 눈금을
            // 가리키지 않고 현재 손잡이에서 가까운 눈금으로만 움직여 정답 누설을 막는다.
            bool firstMainRequest = phase == GamePhase.Playing && st.solved == 0 && st.firstAttempted == 0;
            bool visible = (phase == GamePhase.Practice || firstMainRequest) && current != null && !dragging;
            ghostFingerRt.gameObject.SetActive(visible); ghostBladeRt.gameObject.SetActive(visible);
            ghostMovePathRt.gameObject.SetActive(visible); ghostCutPathRt.gameObject.SetActive(visible);
            ghostArrowRt.gameObject.SetActive(visible);
            ghostGuideText.gameObject.SetActive(visible);
            if (!visible) return;

            practiceGuideClock += dt;
            float cycle = Mathf.Repeat(practiceGuideClock, 1.8f) / 1.8f;
            int guideFrom = phase == GamePhase.Practice ? 1 : st.selectedCellCount;
            int guideTo = phase == GamePhase.Practice ? current.answer : Mathf.Clamp(guideFrom + (guideFrom > 20 ? -3 : 3), 1, 24);
            Vector2 from = HandleScreenPosition(guideFrom);
            Vector2 target = HandleScreenPosition(guideTo);
            Vector2 cutEnd = layoutLand
                ? target + Vector2.down * (88f * ReferenceScale())
                : target + Vector2.right * (88f * ReferenceScale());
            Vector2 p;
            if (cycle < .58f) p = Vector2.Lerp(from, target, 1f - Mathf.Pow(1f - cycle / .58f, 3f));
            else if (cycle < .72f) p = target;
            else p = Vector2.Lerp(target, cutEnd, (cycle - .72f) / .28f);
            PlaceAtScreen(ghostFingerRt, p); PlaceAtScreen(ghostBladeRt, p);
            PlaceAtScreen(ghostArrowRt, Vector2.Lerp(target, cutEnd, .72f));
            if (layoutLand) ghostBladeRt.sizeDelta = new Vector2(7, 84);
            else ghostBladeRt.sizeDelta = new Vector2(88, 7);
            ghostArrowRt.localRotation = Quaternion.Euler(0, 0, layoutLand ? -90f : 0f);
            PlaceSegment(ghostMovePathRt, from, target, 6f);
            PlaceSegment(ghostCutPathRt, target, cutEnd, 7f);
            float alpha = .62f + Mathf.Sin(cycle * Mathf.PI) * .30f;
            ghostFingerImage.color = new Color(Cream.r, Cream.g, Cream.b, alpha);
            ghostBladeImage.color = new Color(MetalLight.r, MetalLight.g, MetalLight.b, alpha);
            ghostGuideText.text = phase == GamePhase.Practice
                ? (layoutLand ? "① 5칸으로 이동   ② 아래로 베기" : "① 5칸으로 이동   ② 옆으로 베기")
                : (layoutLand ? "손잡이부터: 눈금 이동 → 아래로 베기" : "손잡이부터: 눈금 이동 → 옆으로 베기");
        }

        void RestartPracticeGuide()
        {
            if (phase == GamePhase.Practice || (phase == GamePhase.Playing && st.solved == 0 && st.firstAttempted == 0))
                practiceGuideClock = 0f;
        }

        void PlaceWrongMarker(int value)
        {
            float t = (Mathf.Clamp(value, 1, 24) - 1) / 23f;
            Vector2 a = layoutLand ? new Vector2(Mathf.Lerp(.22f, .78f, t), .36f) : new Vector2(.5f, Mathf.Lerp(.22f, .67f, t));
            Anchor(wrongMarkerRt, a); Anchor(wrongMarkerText.rectTransform, a);
            wrongMarkerText.rectTransform.anchoredPosition = layoutLand ? new Vector2(0, 68) : new Vector2(92, 0);
        }

        void ResetWorldForRun()
        {
            correctFxClock = wrongFxClock = beeReactClock = 9f; hitStopClock = 0f; shownScore = 0;
            if (cam) { cam.transform.position = cameraBase; cam.transform.rotation = Quaternion.LookRotation(new Vector3(0, 2.1f, 1.5f) - cameraBase); }
            for (int i = 0; i < bees.Length; i++) if (bees[i]) bees[i].localPosition = beeHomes[i];
        }

        void ShowTitleVisual()
        {
            titleSealL.anchoredPosition = new Vector2(-39, 0); titleSealR.anchoredPosition = new Vector2(39, 0);
            titleSealL.localRotation = titleSealR.localRotation = Quaternion.identity;
            titleKnifeBlade.anchoredPosition = new Vector2(-92, -102);
            titleSealText.text = "봉인 열기"; guideBoost = 1f;
        }

        void BeginSealSplit()
        {
            titleSealText.text = "벌통 여는 중"; guideBoost = 1.4f; ReactBees(true);
        }

        void StartProblemVisual(HoneyProblem p, bool practice, int selected)
        {
            // Practice()와 실전 모두 문제 데이터의 정본 발문을 그대로 사용한다.
            promptText.text = p.prompt;
            leftShelfText.text = p.leftName + "\n" + p.m + "종";
            rightShelfText.text = p.rightName + "\n" + p.n + "종";
            goalText.text = practice ? "압박 없음 · 정답을 낼 때까지 연습" : "8회 시식 · 첫 절단 6회 · 봉인 " + st.lives + "개";
            instructionText.text = layoutLand
                ? "분홍 손잡이를 눈금으로 옮기고 아래로 그어 베시오."
                : "분홍 손잡이를 눈금으로 옮기고 옆으로 그어 베시오.";
            for (int i = 0; i < 24; i++)
            {
                cellImage[i].color = i % 2 == 0 ? HoneyLight : Honey;
                cellRt[i].anchoredPosition = Vector2.zero; cellRt[i].localRotation = Quaternion.identity;
            }
            wrongMarkerImage.color = new Color(Cream.r, Cream.g, Cream.b, 0);
            wrongMarkerText.color = new Color(Cream.r, Cream.g, Cream.b, 0);
            practiceGuideClock = 0f;
            MoveHandleVisual(selected); guideBoost = practice ? 1.3f : .45f; RefreshHud();
        }

        void RefreshHud()
        {
            if (hudText == null) return;
            int sec = Mathf.Max(0, Mathf.CeilToInt(sessionLeft));
            string status = "의뢰 " + Mathf.Min(KkulDodukRules.Goal, st.solved + 1) + "/8   봉인 " + st.lives + "   " + sec + "초";
            hudText.text = layoutLand
                ? status + "\n" + (phase == GamePhase.Practice ? "압박 없음 · 정답까지 연습" : "첫 절단 목표 6회")
                : status;
            recordText.text = "첫 절단 " + st.firstCorrect + "/" + st.firstAttempted + "  ·  정확도 " + st.firstAccuracy + "%  ·  점수 " + Mathf.RoundToInt(shownScore);
        }

        void SetScreen()
        {
            if (titleGroup == null) return;
            titleGroup.alpha = phase == GamePhase.Title ? 1f : 0f;
            gameGroup.alpha = (phase == GamePhase.Practice || phase == GamePhase.Playing || phase == GamePhase.Reveal) ? 1f : 0f;
            revealGroup.alpha = phase == GamePhase.Reveal ? 1f : 0f;
            endGroup.alpha = phase == GamePhase.End ? 1f : 0f;
            titleRoot.gameObject.SetActive(titleGroup.alpha > 0);
            gameRoot.gameObject.SetActive(gameGroup.alpha > 0);
            revealRoot.gameObject.SetActive(revealGroup.alpha > 0);
            endRoot.gameObject.SetActive(endGroup.alpha > 0);
            if (recordPlateRt != null) recordPlateRt.gameObject.SetActive(phase != GamePhase.Reveal);
        }

        void BeginHandleDrag()
        {
            handlePunch = .24f; guideBoost = .2f;
        }

        void CancelHandleDrag()
        {
            handlePunch = .20f; cutTrailClock = .12f;
        }

        void UpdateCutTrail(Vector2 start, Vector2 now, bool ended)
        {
            if (cutTrailRt == null) return;
            // Screen 좌표를 Overlay Rect 좌표로 변환한다. CanvasScaler와 같은 기하 평균 스케일을 쓴다.
            float scale = Mathf.Max(.01f, ReferenceScale());
            Vector2 delta = (now - start) / scale;
            float len = delta.magnitude;
            cutTrailRt.gameObject.SetActive(len > 5f);
            cutTrailRt.anchorMin = cutTrailRt.anchorMax = new Vector2(.5f, .5f);
            Vector2 midScreen = (start + now) * .5f;
            cutTrailRt.anchoredPosition = new Vector2((midScreen.x - Screen.width * .5f) / scale, (midScreen.y - Screen.height * .5f) / scale);
            cutTrailRt.sizeDelta = new Vector2(Mathf.Max(8, len), 9);
            cutTrailRt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            cutTrailClock = ended ? .18f : 0f;
        }

        void BeginCorrectReveal(HoneyProblem p, int submitted, bool practice)
        {
            revealTitle.text = practice ? "첫 절단 성공" : "빈 칸 없이 시식 완료";
            revealText.text = p.formula + "가지\n" + (p.kind == HoneyKind.Sum ? "한 종류만 골라 두 군의 수를 더한다." : "각각 하나씩 골라 묶음 수를 곱한다.");
            correctFxClock = 0f; hitStopClock = .075f; guideBoost = 0f;
            wrongMarkerImage.color = new Color(Cream.r, Cream.g, Cream.b, 0);
            wrongMarkerText.color = new Color(Cream.r, Cream.g, Cream.b, 0);
            for (int i = 0; i < sampleCards.Length; i++)
            {
                sampleCards[i].gameObject.SetActive(i < Mathf.Min(8, p.answer));
                sampleCards[i].anchoredPosition = Vector2.zero; sampleCards[i].localScale = Vector3.one * .35f;
            }
            // 정답 사건의 주인공은 칼날·분리되는 밀랍층·채워지는 셀이다. 벌은 제자리에 둔다.
            handlePunch = .34f;
        }

        void BeginWrongReveal(HoneyProblem p, int submitted, string misconception, bool practice)
        {
            revealTitle.text = practice ? "봉인은 그대로 · 다시 해 보시오" : "" + submitted + "칸에는 견본이 남는다";
            string reason;
            switch (misconception)
            {
                case "sum_used_product": reason = "한 종류만 고르므로 두 군의 수를 더한다."; break;
                case "product_used_sum": reason = "각각 하나씩 고르므로 " + p.m + "묶음마다 " + p.n + "가지를 센다."; break;
                case "one_group_only": reason = p.kind == HoneyKind.Sum
                    ? "겹치지 않는 두 군의 모든 종류를 더한다."
                    : "한 묶음만 세지 말고 모든 꿀과 용기의 짝을 센다."; break;
                case "roles_merged": reason = "꿀과 용기는 역할이 다르므로 바뀐 짝도 따로 센다."; break;
                case "difference_only": reason = "두 군의 차가 아니라 겹치지 않는 모든 종류를 센다."; break;
                default: reason = "선택 조건을 다시 확인한다."; break;
            }
            revealText.text = reason + "\n정답 상태: " + p.formula + "가지 → " + p.answer + "칸";
            wrongFxClock = 0f; guideBoost = .5f; ReactBees(false); handlePunch = .25f;
            PlaceWrongMarker(p.answer);
            wrongMarkerImage.color = new Color(Cream.r, Cream.g, Cream.b, .95f);
            wrongMarkerText.text = "정답 " + p.answer + "칸";
            wrongMarkerText.color = Cream;
            for (int i = 0; i < sampleCards.Length; i++) sampleCards[i].gameObject.SetActive(false);
        }

        void CompleteRevealVisual(bool correct)
        {
            for (int i = 0; i < sampleCards.Length; i++) sampleCards[i].gameObject.SetActive(false);
            wrongMarkerImage.color = new Color(Cream.r, Cream.g, Cream.b, 0);
            wrongMarkerText.color = new Color(Cream.r, Cream.g, Cream.b, 0);
            if (!correct) for (int i = 0; i < 24; i++) cellImage[i].color = i % 2 == 0 ? HoneyLight : Honey;
        }

        void ResumeProblemAfterWrong()
        {
            revealGroup.alpha = 0; revealRoot.gameObject.SetActive(false);
            goalText.text = phase == GamePhase.Practice ? "정답을 낼 때까지 봉인은 줄지 않는다" :
                "같은 의뢰 수정 중 · 봉인 " + st.lives + "개 · 첫 절단 기록은 유지";
            instructionText.text = "조건을 다시 보고 손잡이를 옮긴 뒤 가로질러 베시오.";
        }

        void ShowEndVisual(bool clear, string reason)
        {
            endTitle.text = clear ? "꿀빛 통로 완성" : "봉인 통로가 멈췄다";
            endStats.text = "시식 " + st.solved + "/8\n첫 절단 " + st.firstCorrect + "/" + st.firstAttempted + " · " + st.firstAccuracy + "%";
            endReasonText.text = reason + "\n점수 " + st.score;
            shownScore = 0f; restartText.text = "다시 봉인 열기"; ReactBees(clear);
        }

        void ShowWrongReason(string text) { RefuseAt(HandleScreenPosition(st.selectedCellCount), text); }

        void BoostGuide() { guideBoost = Mathf.Max(guideBoost, 1.25f); RestartPracticeGuide(); }
        void PointToHandle() { guideBoost = Mathf.Max(guideBoost, 1.1f); RestartPracticeGuide(); }

        void RippleAt(Vector2 screen)
        {
            rippleScreen = screen; rippleClock = 0f;
        }

        void RefuseAt(Vector2 screen, string message)
        {
            RippleAt(screen); toastCache = message; toastText.text = message; toastClock = 0f;
            if (phase == GamePhase.Playing && st.solved == 0 && st.firstAttempted == 0)
                instructionText.text = layoutLand
                    ? "손잡이부터 잡아 눈금으로 이동 → 아래로 베시오."
                    : "손잡이부터 잡아 눈금으로 이동 → 옆으로 베시오.";
            handlePunch = .16f; RestartPracticeGuide(); MgfSfx.Play("tap", .09f);
        }

        void ReactBees(bool happy)
        {
            beeReactClock = happy ? 0f : -.01f;
        }

        void UpdateWorldVisual(float dt)
        {
            if (hitStopClock > 0f) { hitStopClock -= dt; return; }
            worldClock += dt;
            for (int i = 0; i < bees.Length; i++)
            {
                if (!bees[i]) continue;
                float phaseOffset = i * .83f;
                float react = beeReactClock < .7f ? Mathf.Sin(Mathf.Clamp01(beeReactClock / .7f) * Mathf.PI) : 0f;
                float sad = beeReactClock < 0f ? 1f : 0f;
                bees[i].localPosition = beeHomes[i] + new Vector3(Mathf.Sin(worldClock * .9f + phaseOffset) * .14f,
                    Mathf.Sin(worldClock * 1.7f + phaseOffset) * .16f + react * (sad > 0 ? -.22f : .35f), 0);
                float wing = Mathf.Sin(worldClock * 14f + phaseOffset) * 24f;
                beeWingsL[i].localRotation = Quaternion.Euler(0, 0, 18f + wing);
                beeWingsR[i].localRotation = Quaternion.Euler(0, 0, -18f - wing);
            }
            if (beeReactClock < 9f) beeReactClock += dt;
            for (int i = 0; i < honeyDrops.Length; i++)
            {
                float y = dropHomes[i].y - Mathf.Repeat(worldClock * (.16f + (i % 3) * .025f) + i * .21f, 1.25f);
                honeyDrops[i].localPosition = new Vector3(dropHomes[i].x, y, dropHomes[i].z);
            }
            if (wrongFxClock < .55f)
            {
                wrongFxClock += dt;
                float amp = (1f - wrongFxClock / .55f) * .14f;
                cam.transform.position = cameraBase + new Vector3(Mathf.Sin(wrongFxClock * 70f) * amp, 0, 0);
            }
            else if (cam) cam.transform.position = Vector3.Lerp(cam.transform.position, cameraBase, Mathf.Clamp01(dt * 9f));
            if (correctFxClock < 9f) correctFxClock += dt;
        }

        void UpdateUiVisual(float dt)
        {
            if (phase == GamePhase.Title && !titleOpening)
            {
                float preview = Mathf.PingPong(Time.unscaledTime * .72f, 1f);
                titleKnifeBlade.anchoredPosition = new Vector2(Mathf.Lerp(-92f, 92f, preview), -102f);
                titleKnifeBlade.localScale = new Vector3(1f + Mathf.Sin(preview * Mathf.PI) * .12f, 1f, 1f);
            }
            if (titleOpening)
            {
                float t = Mathf.Clamp01(titleClock / .38f);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                titleSealL.anchoredPosition = new Vector2(-39 - ease * 78, Mathf.Sin(t * Mathf.PI) * 9);
                titleSealR.anchoredPosition = new Vector2(39 + ease * 78, Mathf.Sin(t * Mathf.PI) * 9);
                titleSealL.localRotation = Quaternion.Euler(0, 0, -ease * 18);
                titleSealR.localRotation = Quaternion.Euler(0, 0, ease * 18);
            }
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * (guideBoost > 1 ? 8f : 4f)) * (.06f + guideBoost * .025f);
            guideRt.localScale = Vector3.one * pulse;
            guideRing.color = new Color(Cream.r, Cream.g, Cream.b, phase == GamePhase.Practice ? .82f : Mathf.Clamp01(.16f + guideBoost * .40f));
            guideBoost = Mathf.Max(0f, guideBoost - dt * .22f);
            UpdatePracticeGuide(dt);

            if (handlePunch > 0f) handlePunch = Mathf.Max(0f, handlePunch - dt);
            float hp = handlePunch > 0f ? Mathf.Sin((1f - handlePunch / .34f) * Mathf.PI) : 0f;
            handleRt.localScale = dragging
                ? new Vector3(layoutLand ? 1.18f : .88f, layoutLand ? .88f : 1.18f, 1)
                : new Vector3(1f + hp * .18f, 1f - hp * .10f, 1);

            if (rippleClock < .28f)
            {
                rippleClock += dt;
                float t = Mathf.Clamp01(rippleClock / .28f), scale = ReferenceScale();
                rippleRt.anchorMin = rippleRt.anchorMax = new Vector2(.5f, .5f);
                rippleRt.anchoredPosition = new Vector2((rippleScreen.x - Screen.width * .5f) / scale, (rippleScreen.y - Screen.height * .5f) / scale);
                rippleRt.localScale = Vector3.one * Mathf.Lerp(.35f, 1.65f, t);
                rippleImage.color = new Color(Cream.r, Cream.g, Cream.b, (1f - t) * .86f);
            }
            else rippleImage.color = new Color(Cream.r, Cream.g, Cream.b, 0);

            if (toastClock < 4.3f)
            {
                toastClock += dt;
                toastText.text = toastCache;
                toastText.color = new Color(Cream.r, Cream.g, Cream.b, toastClock < 3.8f ? 1f : 1f - (toastClock - 3.8f) / .5f);
            }
            else toastText.color = new Color(Cream.r, Cream.g, Cream.b, 0);

            if (cutTrailClock < .22f)
            {
                cutTrailClock += dt;
                var img = cutTrailRt.GetComponent<Image>();
                if (img) img.color = new Color(Raspberry.r, Raspberry.g, Raspberry.b, Mathf.Clamp01(1f - cutTrailClock / .22f) * .72f);
                if (cutTrailClock >= .22f) cutTrailRt.gameObject.SetActive(false);
            }

            if (phase == GamePhase.Reveal && revealCorrect && current != null)
            {
                float t = Mathf.Clamp01(correctFxClock / 1.15f);
                int filled = Mathf.Min(current.answer, Mathf.FloorToInt(t * (current.answer + 2)));
                float separate = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((correctFxClock - .18f) / .62f));
                for (int i = 0; i < 24; i++)
                {
                    cellImage[i].color = i < filled
                        ? (i % 2 == 0 ? new Color32(255, 226, 91, 255) : new Color32(255, 178, 35, 255))
                        : (i % 2 == 0 ? HoneyLight : Honey);
                    if (i >= current.answer) continue;
                    float groupOffset;
                    if (current.kind == HoneyKind.Sum)
                        groupOffset = i < current.m ? -13f : 13f;
                    else
                    {
                        int group = i / Mathf.Max(1, current.n);
                        groupOffset = (group - (current.m - 1) * .5f) * 5.5f;
                    }
                    cellRt[i].anchoredPosition = layoutLand
                        ? new Vector2(0, groupOffset * separate)
                        : new Vector2(groupOffset * separate, 0);
                    cellRt[i].localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? -1f : 1f) * separate * 3.5f);
                }
                for (int i = 0; i < sampleCards.Length; i++)
                {
                    float local = Mathf.Clamp01((correctFxClock - i * .055f) / .30f);
                    float eased = 1f - Mathf.Pow(1f - local, 3f);
                    sampleCards[i].anchoredPosition = new Vector2((i - 3.5f) * 38f * eased, Mathf.Sin(i * 1.7f) * 7f);
                    sampleCards[i].localScale = Vector3.one * Mathf.Lerp(.35f, 1f + Mathf.Sin(local * Mathf.PI) * .14f, local);
                }
            }
            else if (phase == GamePhase.Reveal && !revealCorrect && current != null)
            {
                // 1.65초 오답 근거 동안 0.8초 이상 정답 칸 묶음과 유령 칼을 유지한다.
                bool showAnswerState = revealClock >= .18f;
                for (int i = 0; i < 24; i++)
                    cellImage[i].color = showAnswerState && i < current.answer
                        ? (i == current.answer - 1 ? Raspberry : MetalLight)
                        : (i % 2 == 0 ? HoneyLight : Honey);
                wrongMarkerImage.color = new Color(Cream.r, Cream.g, Cream.b, showAnswerState ? .95f : 0f);
                wrongMarkerText.color = new Color(Cream.r, Cream.g, Cream.b, showAnswerState ? 1f : 0f);
            }

            if (phase == GamePhase.End)
            {
                shownScore = Mathf.MoveTowards(shownScore, st.score, Mathf.Max(35f, st.score * 1.8f) * dt);
                endStats.text = "시식 " + st.solved + "/8\n첫 절단 " + st.firstCorrect + "/" + st.firstAttempted + " · " + st.firstAccuracy + "%";
            }
            else shownScore = Mathf.MoveTowards(shownScore, st.score, Mathf.Max(20f, st.score * 2.5f) * dt);

            if (phase == GamePhase.Playing || phase == GamePhase.Practice) RefreshHud();
        }
    }
}
