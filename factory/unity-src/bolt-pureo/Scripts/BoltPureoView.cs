// 볼트 풀어 — 밝은 빗물 조류정원 2.5D 장면, 도자 삼각판, 측정선, 모스와 UI.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Mgf;

namespace Mgf.BoltPureo
{
    public partial class BoltPureoGame
    {
        static readonly Color DeepTeal = new Color32(25, 75, 82, 255);
        static readonly Color Teal = new Color32(86, 190, 174, 255);
        static readonly Color Persimmon = new Color32(244, 139, 105, 255);
        static readonly Color Copper = new Color32(221, 154, 78, 255);
        static readonly Color Cream = new Color32(255, 246, 214, 255);
        static readonly Color Night = new Color32(151, 211, 214, 255);
        static readonly Color WetStone = new Color32(105, 159, 148, 255);
        static readonly Color WrongInk = new Color32(115, 65, 69, 255);

        Camera cam;
        Vector3 cameraBase;
        Transform worldRoot, titleGear, titleDriver, mossRoot, mossBody, mossTail;
        Transform[] plates = new Transform[3];
        Vector3[] plateHomes = new Vector3[3];
        Transform[] plateRails = new Transform[6];
        Transform[] boltHeads = new Transform[12];
        Vector3[] boltHomes = new Vector3[12];
        Transform[] rain = new Transform[18];
        Transform[] trayBolts = new Transform[3];
        Transform[] birdWings = new Transform[3];
        Transform[] bulbs = new Transform[8];
        Transform[] gardenBirds = new Transform[4];
        Vector3[] gardenBirdHomes = new Vector3[4];
        Transform[] gardenLeaves = new Transform[12];
        LineRenderer[] measureLines = new LineRenderer[3];
        LineRenderer[] constructionLines = new LineRenderer[3];
        LineRenderer[] rightAngleLines = new LineRenderer[6];
        LineRenderer guideRingWorld;
        TextMeshPro[,] vertexLabels = new TextMeshPro[3,3];
        Material enamelTealMat, enamelPersimmonMat, copperMat, creamMat, stoneMat, glowMat, darkCopperMat;

        RectTransform uiRoot, titleRoot, gameRoot, endRoot, revealRoot;
        CanvasGroup titleGroup, gameGroup, endGroup, revealGroup;
        TextMeshProUGUI titleLogo, titleTag, titleStart, titleUnit;
        TextMeshProUGUI hudText, goalText, promptText, evidenceTitle, evidenceText, trayText, toastText;
        TextMeshProUGUI revealTitle, revealReason, endTitle, endStats, endReasonText, restartText;
        RectTransform evidencePlate, revealPlate, endPlate, ghostFingerRt, rippleRt;
        RectTransform hudPlate, promptPlate;
        Image ghostFinger, rippleImage, evidenceImage, revealImage, endImage;
        Sprite discSprite, ringSprite, panelSprite;
        int layoutW = -1, layoutH = -1;
        bool layoutLand;
        float worldClock, correctClock = 99f, wrongClock = 99f, plateClock = 99f;
        float rippleClock = 99f, toastClock = 99f, guideBoost, endClock = 99f;
        float shownScore;
        int fxBolt = -1, fxPlate = -1;
        bool finalBird;
        bool practiceDemo;
        float practiceDemoClock;
        Vector2 rippleScreen;
        string toastCache = "";

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
            image.sprite = sprite;
            if (sprite != null && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        TextMeshProUGUI Txt(string name, Transform parent, string text, Vector2 anchor, Vector2 pos,
            float size, Color color, float width, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R(name, parent, anchor, pos, new Vector2(width, size * 3f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.fontStyle = FontStyles.Bold; t.color = color;
            t.alignment = align; t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis; t.raycastTarget = false; t.text = text;
            return t;
        }

        Sprite MakeDisc(bool ring)
        {
            const int S = 96;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = ring ? "engraved ring" : "soft disc", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = (x + .5f) / S * 2f - 1f, dy = (y + .5f) / S * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = ring ? Mathf.Clamp01((.95f - d) * 17f) * Mathf.Clamp01((d - .69f) * 17f)
                    : Mathf.Clamp01((.96f - d) * 15f);
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        Sprite MakePanel()
        {
            const int S = 96, radius = 9;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "enamel label plate", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - S * .5f) - (S * .5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + .5f - S * .5f) - (S * .5f - radius), 0f);
                float a = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
        }

        Vector3 LocalPoint(TriangleGeometry g, GeoPoint p, float z)
        {
            float s = g.renderMilli / 1000f;
            return new Vector3(p.x * s, p.y * s, z);
        }

        Mesh TrianglePrism(TriangleGeometry g, float depth)
        {
            var m = new Mesh { name = "Enamel triangle prism" };
            SetTriangleMesh(m, g, depth); return m;
        }

        void SetTriangleMesh(Mesh m, TriangleGeometry g, float depth)
        {
            float z = depth * .5f;
            Vector3 a0 = LocalPoint(g, g.vertices[0], -z), b0 = LocalPoint(g, g.vertices[1], -z), c0 = LocalPoint(g, g.vertices[2], -z);
            Vector3 a1 = LocalPoint(g, g.vertices[0], z), b1 = LocalPoint(g, g.vertices[1], z), c1 = LocalPoint(g, g.vertices[2], z);
            var v = new[] {
                a0, b0, c0, a1, b1, c1
            };
            var t = new[] {
                0,2,1, 3,4,5,
                0,1,4, 0,4,3,
                1,2,5, 1,5,4,
                2,0,3, 2,3,5
            };
            m.Clear(); m.vertices = v; m.triangles = t; m.RecalculateNormals(); m.RecalculateBounds();
        }

        GameObject TriangleObject(string name, Vector3 pos, Color color, TriangleGeometry g, Transform parent)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            var mesh = TrianglePrism(g, .32f);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = color == Teal ? enamelTealMat : enamelPersimmonMat;
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            go.GetComponent<MeshRenderer>().receiveShadows = true;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        Transform Gear(string name, Vector3 pos, float radius, int teeth, Material mat, Transform parent)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); root.localPosition = pos;
            var hub = MgfLook.Prim(PrimitiveType.Cylinder, name + " hub", Vector3.zero,
                new Vector3(radius * 2f, .22f, radius * 2f), mat, root, false);
            hub.transform.localRotation = Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < teeth; i++)
            {
                float a = i * Mathf.PI * 2f / teeth;
                var tooth = MgfLook.Block(name + " tooth", new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0),
                    new Vector3(.38f, .82f, .28f), .05f, mat, root);
                tooth.transform.localRotation = Quaternion.Euler(0, 0, -i * 360f / teeth);
            }
            return root;
        }

        void BuildWorld()
        {
            // CreatePrimitive가 내부에서 붙이는 collider 타입을 WebGL 스트리핑에서 보존한다.
            // 입력 판정은 화면 좌표 기반이지만 Sphere/Cylinder 생성 시 타입이 없으면 콘솔 오류가 난다.
            var colliderKeeper = new GameObject("primitive collider type keeper");
            colliderKeeper.AddComponent<SphereCollider>(); colliderKeeper.AddComponent<CapsuleCollider>();
            colliderKeeper.AddComponent<BoxCollider>(); colliderKeeper.AddComponent<MeshCollider>();
            Object.Destroy(colliderKeeper);
            MgfLook.Sky(new Color32(118, 192, 211, 255), new Color32(199, 229, 208, 255),
                new Color32(90, 145, 135, 255), .78f);
            MgfLook.Sun(new Vector3(-38, -24, 12), new Color32(255, 244, 200, 255), 1.28f, .64f);
            cam = MgfLook.Camera(new Vector3(8.8f, 7.7f, -19.5f), new Vector3(0, 1.2f, 0), 32f);
            cam.orthographic = true; cam.orthographicSize = 8.2f; cam.backgroundColor = Night; cameraBase = cam.transform.position;

            // 공업용 청동 대신 무광 도자·채색 목재를 주재료로 쓴다. 금속성은 볼트의
            // 작은 강조에만 남겨 기존 어두운 하드서피스 계열과 물성을 분리한다.
            enamelTealMat = MgfLook.Lit(Teal, .34f, .02f, Teal * .025f);
            enamelPersimmonMat = MgfLook.Lit(Persimmon, .30f, .01f, Persimmon * .02f);
            copperMat = MgfLook.Lit(Copper, .27f, .06f, Copper * .018f);
            darkCopperMat = MgfLook.Lit(new Color32(75, 119, 112, 255), .22f, .02f);
            creamMat = MgfLook.Lit(Cream, .26f, .01f, Cream * .035f);
            stoneMat = MgfLook.Lit(new Color32(132, 184, 169, 255), .16f, .01f);
            glowMat = MgfLook.Lit(new Color32(255, 215, 112, 255), .42f, .04f, new Color(.18f, .10f, .025f));

            worldRoot = new GameObject("Rain garden aviary cutaway").transform;
            MgfLook.Block("pale rain garden wall", new Vector3(0, 1.5f, 4.8f), new Vector3(30, 19, 1.2f), .18f, stoneMat, worldRoot);
            // 화면을 평평한 기계실이 아니라 여러 층의 밝은 조류정원 단면으로 채운다.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 7.4f;
                MgfLook.Block("painted garden column", new Vector3(x, 1.5f, 1.6f), new Vector3(.72f, 19f, .78f), .24f, creamMat, worldRoot);
                for (int k = 0; k < 7; k++)
                {
                    var brace = MgfLook.Block("wood trellis branch", new Vector3(x - side * 1.3f, -4.5f + k * 2.1f, 2.0f),
                        new Vector3(3.0f, .24f, .30f), .04f, copperMat, worldRoot);
                    brace.transform.localRotation = Quaternion.Euler(0, 0, side * (k % 2 == 0 ? 34 : -34));
                }
            }
            for (int i = 0; i < 18; i++)
            {
                float x = -13f + i * 1.55f;
                var rod = MgfLook.Block("sunlit rain line", new Vector3(x, -7f + (i * 1.47f) % 15f, 3.9f),
                    new Vector3(.035f, 1.3f + (i % 4) * .28f, .03f), .008f, MgfLook.Unlit(new Color(.80f,.96f,1f,.58f)), worldRoot);
                rod.transform.localRotation = Quaternion.Euler(0, 0, -12f); rain[i] = rod.transform;
            }
            for (int i = 0; i < 8; i++)
            {
                bulbs[i] = MgfLook.Prim(PrimitiveType.Sphere, "paper lantern seed", new Vector3(i % 2 == 0 ? -6.55f : 6.55f, -5.5f + i * 1.55f, .9f),
                    Vector3.one * .28f, glowMat, worldRoot, false).transform;
            }
            // 좌우 층계·새집이 와이드 여백까지 실제 세계로 쓴다.
            MgfLook.Block("left greenhouse stair", new Vector3(-9.4f, 1.4f, 1.4f), new Vector3(3.2f, 15.5f, .6f), .24f, creamMat, worldRoot);
            Gear("rain wheel flower", new Vector3(-9.4f, 5.4f, .7f), 1.25f, 12, enamelPersimmonMat, worldRoot);
            MgfLook.Block("right ceramic bird houses", new Vector3(9.5f, .6f, 1.4f), new Vector3(4.1f, 14.2f, .6f), .32f, darkCopperMat, worldRoot);
            BuildGardenDetails();

            var initialProblem = BoltPureoRules.Practice();
            var initialGeometry = BoltPureoRules.GeometryFor(initialProblem);
            var initialCandidates = BoltPureoRules.Candidates(initialProblem, 0, 0);
            plateHomes[0] = new Vector3(-.55f, 4.35f, .35f);
            plateHomes[1] = new Vector3(.45f, 1.05f, .20f);
            plateHomes[2] = new Vector3(-.30f, -2.25f, .05f);
            for (int p = 0; p < 3; p++)
            {
                plates[p] = TriangleObject("enamel triangle shutter " + p, plateHomes[p], p == 1 ? Persimmon : Teal, initialGeometry, worldRoot).transform;
                plates[p].localRotation = Quaternion.Euler(0, 0, p == 0 ? -4f : (p == 1 ? 5f : -2f));
                plateRails[p*2] = MgfLook.Block("left rail " + p, plateHomes[p] + new Vector3(-3.1f, 0, .48f), new Vector3(.22f, 3.45f, .24f), .04f, copperMat, worldRoot).transform;
                plateRails[p*2+1] = MgfLook.Block("right rail " + p, plateHomes[p] + new Vector3(3.1f, 0, .48f), new Vector3(.22f, 3.45f, .24f), .04f, copperMat, worldRoot).transform;
                for (int k = 0; k < 4; k++)
                {
                    int idx = p * 4 + k;
                    Vector3 local = LocalPoint(initialGeometry, initialCandidates[idx].Point, -.31f);
                    var b = MgfLook.Prim(PrimitiveType.Cylinder, "identical brass bolt " + idx, local,
                        new Vector3(.34f, .22f, .34f), copperMat, plates[p], false);
                    b.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    boltHeads[idx] = b.transform; boltHomes[idx] = local;
                    var slot = MgfLook.Block("bolt slot " + idx, local + new Vector3(0,0,-.19f), new Vector3(.42f,.075f,.08f), .018f, darkCopperMat, b.transform);
                    slot.transform.localPosition = new Vector3(0,0,-.19f);
                    slot.transform.localRotation = Quaternion.Euler(0, 0, (idx % 3) * 60f);
                }
                for (int v = 0; v < 3; v++)
                {
                    vertexLabels[p,v] = MgfText.World(initialGeometry.labels[v], Vector3.zero, 2.2f, Cream, plates[p]);
                    vertexLabels[p,v].alignment = TextAlignmentOptions.Center;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                measureLines[i] = NewLine("neutral measure " + i, Cream, .075f, 2);
                measureLines[i].gameObject.SetActive(false);
                constructionLines[i] = NewLine("construction trace " + i, new Color(Cream.r,Cream.g,Cream.b,.58f), .035f, 2);
                constructionLines[i].gameObject.SetActive(false);
                trayBolts[i] = MgfLook.Prim(PrimitiveType.Cylinder, "tray bolt " + i,
                    new Vector3(-1.4f + i * 1.4f, -6.15f, -.2f), new Vector3(.42f,.28f,.42f), copperMat, worldRoot, false).transform;
                trayBolts[i].localRotation = Quaternion.Euler(90,0,0); trayBolts[i].gameObject.SetActive(false);
                birdWings[i] = TriangleObject("mechanical bird wing " + i,
                    new Vector3(8.8f, -.4f + i * .15f, -.2f), i == 1 ? Persimmon : Teal, initialGeometry, worldRoot).transform;
                birdWings[i].localScale = new Vector3(.72f,.72f,.72f);
                birdWings[i].localRotation = Quaternion.Euler(0,0,-120 + i * 120);
                birdWings[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < rightAngleLines.Length; i++)
            {
                rightAngleLines[i] = NewLine("right angle marker " + i, Cream, .055f, 2);
                rightAngleLines[i].gameObject.SetActive(false);
            }
            guideRingWorld = NewLine("white mechanical guide ring", Color.white, .065f, 49);
            guideRingWorld.loop = true; guideRingWorld.gameObject.SetActive(false);

            BuildMoss();
            titleGear = Gear("off-center rain wheel sign", new Vector3(4.7f, 4.15f, -.55f), 1.75f, 14, enamelPersimmonMat, worldRoot);
            titleDriver = new GameObject("Moss screwdriver start handle").transform; titleDriver.SetParent(worldRoot, false); titleDriver.localPosition = new Vector3(0,-4.6f,-.65f);
            MgfLook.Prim(PrimitiveType.Cylinder, "driver copper shaft", new Vector3(0,.72f,0), new Vector3(.18f,1.25f,.18f), copperMat, titleDriver, false);
            MgfLook.Block("driver enamel handle", new Vector3(0,-.15f,0), new Vector3(1.55f,.68f,.62f), .24f, enamelPersimmonMat, titleDriver);
        }

        void BuildGardenDetails()
        {
            // 작은 새·잎·새집이 층마다 상주해 빈 장식 기둥 대신 살아 있는 컷어웨이를 만든다.
            for (int level = 0; level < 4; level++)
            {
                float y = -5.1f + level * 3.25f;
                float side = level % 2 == 0 ? -1f : 1f;
                MgfLook.Block("garden balcony " + level, new Vector3(side * 9.1f, y, .55f),
                    new Vector3(4.4f, .34f, 1.05f), .14f, creamMat, worldRoot);
                MgfLook.Block("ceramic bird door " + level, new Vector3(side * 9.35f, y + .85f, .20f),
                    new Vector3(1.25f, 1.35f, .48f), .38f, level % 2 == 0 ? enamelTealMat : enamelPersimmonMat, worldRoot);
            }

            for (int i = 0; i < gardenLeaves.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float y = -5.8f + (i / 2) * 2.15f;
                var leaf = MgfLook.Prim(PrimitiveType.Sphere, "round garden leaf " + i,
                    new Vector3(side * (7.9f + (i % 3) * .55f), y, .05f),
                    new Vector3(.72f, .34f, .18f), i % 3 == 0 ? enamelPersimmonMat : enamelTealMat,
                    worldRoot, false);
                leaf.transform.localRotation = Quaternion.Euler(0, 0, side * (18f + (i % 4) * 9f));
                gardenLeaves[i] = leaf.transform;
            }

            for (int i = 0; i < gardenBirds.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Vector3 home = new Vector3(side * (8.0f + (i / 2) * 1.15f), -3.9f + i * 2.55f, -.55f);
                var bird = new GameObject("resident rain bird " + i).transform;
                bird.SetParent(worldRoot, false); bird.localPosition = home;
                Material bodyMat = i % 2 == 0 ? enamelTealMat : enamelPersimmonMat;
                MgfLook.Prim(PrimitiveType.Sphere, "bird round body", Vector3.zero,
                    new Vector3(.72f, .46f, .42f), bodyMat, bird, false);
                MgfLook.Prim(PrimitiveType.Sphere, "bird head", new Vector3(side * .39f, .20f, -.02f),
                    new Vector3(.42f, .40f, .40f), bodyMat, bird, false);
                var beak = MgfLook.Block("bird paper beak", new Vector3(side * .68f, .16f, -.02f),
                    new Vector3(.28f, .16f, .22f), .06f, glowMat, bird);
                beak.transform.localRotation = Quaternion.Euler(0, 0, side < 0 ? 18f : -18f);
                MgfLook.Prim(PrimitiveType.Sphere, "bird eye", new Vector3(side * .52f, .30f, -.34f),
                    Vector3.one * .08f, darkCopperMat, bird, false);
                gardenBirds[i] = bird; gardenBirdHomes[i] = home;
            }
        }

        LineRenderer NewLine(string name, Color color, float width, int count)
        {
            var go = new GameObject(name); go.transform.SetParent(worldRoot, false);
            var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = MgfLook.Unlit(color);
            lr.startColor = lr.endColor = color; lr.startWidth = lr.endWidth = width;
            lr.positionCount = count; lr.useWorldSpace = true;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false; return lr;
        }

        void BuildMoss()
        {
            mossRoot = new GameObject("rain garden salamander Moss").transform; mossRoot.SetParent(worldRoot, false);
            mossRoot.localPosition = new Vector3(-5.6f, -.6f, -.9f);
            mossBody = MgfLook.Prim(PrimitiveType.Sphere, "Moss ceramic body", Vector3.zero, new Vector3(1.72f,.80f,.74f), enamelTealMat, mossRoot, false).transform;
            MgfLook.Prim(PrimitiveType.Sphere, "Moss head", new Vector3(.98f,.23f,0), new Vector3(.98f,.88f,.82f), enamelTealMat, mossRoot, false);
            for (int i = 0; i < 4; i++)
            {
                float sx = i < 2 ? -.46f : .47f, sy = i % 2 == 0 ? -.41f : .41f;
                var foot = MgfLook.Prim(PrimitiveType.Sphere, "Moss foot", new Vector3(sx,sy,.05f), new Vector3(.54f,.26f,.23f), copperMat, mossRoot, false);
                foot.transform.localRotation = Quaternion.Euler(0,0,sy > 0 ? 24 : -24);
            }
            mossTail = MgfLook.Prim(PrimitiveType.Cylinder, "Moss painted tail", new Vector3(-1.30f,.08f,0),
                new Vector3(.13f,1.16f,.13f), copperMat, mossRoot, false).transform;
            mossTail.localRotation = Quaternion.Euler(0,0,74f);
            MgfLook.Prim(PrimitiveType.Sphere, "Moss eye L", new Vector3(1.34f,.42f,-.34f), Vector3.one*.14f, creamMat, mossRoot, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Moss eye R", new Vector3(1.34f,.42f,.34f), Vector3.one*.14f, creamMat, mossRoot, false);
        }

        void BuildUi()
        {
            discSprite = MakeDisc(false); ringSprite = MakeDisc(true); panelSprite = MakePanel();
            uiRoot = (RectTransform)MgfText.Canvas.transform;
            titleRoot = Full("Title screen", uiRoot); titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
            gameRoot = Full("Game screen", uiRoot); gameGroup = gameRoot.gameObject.AddComponent<CanvasGroup>();
            revealRoot = Full("Reveal screen", uiRoot); revealGroup = revealRoot.gameObject.AddComponent<CanvasGroup>();
            endRoot = Full("End screen", uiRoot); endGroup = endRoot.gameObject.AddComponent<CanvasGroup>();

            titleLogo = Txt("gear engraved logo", titleRoot, "볼트\n풀어", new Vector2(.5f,.69f), Vector2.zero, 58, Cream, 350);
            titleLogo.outlineColor = DeepTeal; titleLogo.outlineWidth = .20f;
            titleLogo.characterSpacing = 4f;
            titleTag = Txt("title verb", titleRoot, "빗물 정원의 삼각판을 해체하라", new Vector2(.5f,.49f), Vector2.zero, 19, Cream, 350);
            titleUnit = Txt("title unit", titleRoot, "중2 · 삼각형의 외심과 내심", new Vector2(.5f,.91f), Vector2.zero, 14, new Color(Cream.r,Cream.g,Cream.b,.78f), 330);
            titleStart = Txt("world start instruction", titleRoot, "모스의 드라이버 손잡이를 잡아 시작", new Vector2(.5f,.13f), Vector2.zero, 18, Cream, 360);

            hudPlate = (RectTransform)Img("copper hud plate", gameRoot, new Vector2(.5f,.955f), Vector2.zero, new Vector2(360,52), new Color(DeepTeal.r,DeepTeal.g,DeepTeal.b,.92f), panelSprite).transform;
            hudText = Txt("hud", hudPlate.transform, "", new Vector2(.5f,.5f), Vector2.zero, 15, Cream, 340);
            goalText = Txt("goal line", gameRoot, "조건에 맞는 볼트를 풀어 삼각판을 떨궈라", new Vector2(.5f,.895f), Vector2.zero, 16, Cream, 370);
            goalText.overflowMode = TextOverflowModes.Overflow; goalText.enableAutoSizing = true;
            goalText.fontSizeMin = 11; goalText.fontSizeMax = 16;
            promptPlate = (RectTransform)Img("enamel order gear", gameRoot, new Vector2(.5f,.81f), Vector2.zero, new Vector2(360,82), new Color(Cream.r,Cream.g,Cream.b,.96f), panelSprite).transform;
            promptText = Txt("order prompt", promptPlate.transform, "", new Vector2(.5f,.5f), Vector2.zero, 17, DeepTeal, 336);
            promptText.overflowMode = TextOverflowModes.Overflow;
            promptText.enableAutoSizing = true; promptText.fontSizeMin = 11; promptText.fontSizeMax = 17;

            evidenceImage = Img("neutral evidence plate", gameRoot, new Vector2(.5f,.205f), Vector2.zero, new Vector2(352,88), new Color(Cream.r,Cream.g,Cream.b,.96f), panelSprite);
            evidencePlate = (RectTransform)evidenceImage.transform;
            evidenceTitle = Txt("evidence title", evidencePlate, "", new Vector2(.5f,.68f), Vector2.zero, 15, DeepTeal, 326);
            evidenceText = Txt("evidence values", evidencePlate, "", new Vector2(.5f,.32f), Vector2.zero, 20, DeepTeal, 326);
            evidenceText.enableAutoSizing = true; evidenceText.fontSizeMin = 11; evidenceText.fontSizeMax = 20;
            trayText = Txt("physical tray", gameRoot, "임시 트레이  ○  ○  ○", new Vector2(.5f,.105f), Vector2.zero, 17, Cream, 360);
            toastText = Txt("local refusal", gameRoot, "", new Vector2(.5f,.285f), Vector2.zero, 14, Cream, 350);

            ghostFinger = Img("ghost long press finger", gameRoot, new Vector2(.5f,.5f), Vector2.zero, new Vector2(66,66), new Color(1,1,1,.85f), ringSprite);
            ghostFingerRt = (RectTransform)ghostFinger.transform;
            rippleImage = Img("input ripple", gameRoot, new Vector2(.5f,.5f), Vector2.zero, new Vector2(38,38), new Color(1,1,1,.7f), ringSprite);
            rippleRt = (RectTransform)rippleImage.transform; rippleImage.gameObject.SetActive(false);

            revealImage = Img("measurement reveal plate", revealRoot, new Vector2(.5f,.46f), Vector2.zero, new Vector2(360,180), new Color(DeepTeal.r,DeepTeal.g,DeepTeal.b,.96f), panelSprite);
            revealPlate = (RectTransform)revealImage.transform;
            revealTitle = Txt("reveal title", revealPlate, "", new Vector2(.5f,.70f), Vector2.zero, 29, Cream, 330);
            revealReason = Txt("reveal reason", revealPlate, "", new Vector2(.5f,.34f), Vector2.zero, 17, Cream, 330);

            endImage = Img("result enamel plate", endRoot, new Vector2(.5f,.46f), Vector2.zero, new Vector2(366,350), new Color(DeepTeal.r,DeepTeal.g,DeepTeal.b,.97f), panelSprite);
            endPlate = (RectTransform)endImage.transform;
            endTitle = Txt("end title", endPlate, "", new Vector2(.5f,.78f), Vector2.zero, 36, Cream, 340);
            endStats = Txt("end stats", endPlate, "", new Vector2(.5f,.53f), Vector2.zero, 20, Cream, 330);
            endStats.rectTransform.sizeDelta = new Vector2(330, 118);
            endReasonText = Txt("end reason", endPlate, "", new Vector2(.5f,.29f), Vector2.zero, 16, Cream, 325);
            restartText = Txt("restart crank", endPlate, "태엽 손잡이를 눌러 다시 해체", new Vector2(.5f,.10f), Vector2.zero, 17, new Color32(255,210,122,255), 330);
        }

        bool IsLand() => Screen.width >= 1024 && Screen.width > Screen.height;

        void UpdateLayoutIfNeeded()
        {
            if (layoutW == Screen.width && layoutH == Screen.height) return;
            layoutW = Screen.width; layoutH = Screen.height; layoutLand = IsLand(); st.layoutLand = layoutLand;
            cam.orthographicSize = 8.25f;
            if (layoutLand)
            {
                // 가로: 위 띠(HUD·목표·주문판) / 가운데 띠(삼각판 3장 전폭) / 아래 띠(증거판·트레이).
                // 판과 글 판이 같은 높이대를 나눠 쓰지 않아 겹침이 생기지 않는다.
                cam.orthographicSize = 7.0f;
                cam.transform.position = new Vector3(8.8f,6.5f,-19.5f); cam.transform.LookAt(new Vector3(0,.75f,0));
                plateHomes[0] = new Vector3(-5.6f, .35f, .35f);
                plateHomes[1] = new Vector3(0f, .35f, .20f);
                plateHomes[2] = new Vector3(5.6f, .35f, .05f);
                PlaceUi(hudPlate, new Vector2(.5f,.955f), new Vector2(300,40));
                hudText.rectTransform.sizeDelta = new Vector2(284,36);
                PlaceUi(goalText.rectTransform, new Vector2(.5f,.885f), new Vector2(560,28));
                goalText.fontSizeMax = 15;
                PlaceUi(promptPlate, new Vector2(.5f,.785f), new Vector2(560,58));
                promptText.rectTransform.sizeDelta = new Vector2(540,50);
                promptText.fontSizeMin = 10; promptText.fontSizeMax = 16;
                PlaceUi(evidencePlate, new Vector2(.30f,.115f), new Vector2(300,72));
                evidenceTitle.rectTransform.sizeDelta = new Vector2(284,26);
                evidenceText.rectTransform.sizeDelta = new Vector2(284,36);
                PlaceUi(trayText.rectTransform, new Vector2(.74f,.105f), new Vector2(260,44));
                PlaceUi(toastText.rectTransform, new Vector2(.5f,.215f), new Vector2(520,34));
            }
            else
            {
                cam.transform.position = new Vector3(8.8f,7.7f,-19.5f); cam.transform.LookAt(new Vector3(0,1.2f,0));
                plateHomes[0] = new Vector3(-.55f, 4.35f, .35f);
                plateHomes[1] = new Vector3(.45f, 1.05f, .20f);
                plateHomes[2] = new Vector3(-.30f, -2.25f, .05f);
                PlaceUi(hudPlate, new Vector2(.5f,.955f), new Vector2(360,52));
                hudText.rectTransform.sizeDelta = new Vector2(340,46);
                PlaceUi(goalText.rectTransform, new Vector2(.5f,.895f), new Vector2(370,58));
                goalText.fontSizeMax = 16;
                PlaceUi(promptPlate, new Vector2(.5f,.81f), new Vector2(360,82));
                promptText.rectTransform.sizeDelta = new Vector2(336,72);
                promptText.fontSizeMin = 11; promptText.fontSizeMax = 17;
                // 증거판은 세 번째 판 아래 띠에 둔다 — 판 위에 겹치면 그 판 볼트를 가린다.
                PlaceUi(evidencePlate, new Vector2(.5f,.132f), new Vector2(352,74));
                evidenceTitle.rectTransform.sizeDelta = new Vector2(326,26);
                evidenceText.rectTransform.sizeDelta = new Vector2(326,36);
                PlaceUi(trayText.rectTransform, new Vector2(.5f,.058f), new Vector2(360,44));
                PlaceUi(toastText.rectTransform, new Vector2(.5f,.285f), new Vector2(350,54));
            }
            PositionPlateAssembly();
            if (current != null && candidates != null) { UpdateGeometryVisual(); UpdateHitMap(); }
            cameraBase = cam.transform.position;
        }

        void PlaceUi(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
        }

        void PositionPlateAssembly()
        {
            for (int p = 0; p < plates.Length; p++)
            {
                if (plates[p] != null) plates[p].localPosition = plateHomes[p];
                if (plateRails[p*2] != null) plateRails[p*2].localPosition = plateHomes[p] + new Vector3(-3.1f,0,.48f);
                if (plateRails[p*2+1] != null) plateRails[p*2+1].localPosition = plateHomes[p] + new Vector3(3.1f,0,.48f);
            }
        }

        void SetScreen()
        {
            titleGroup.alpha = phase == GamePhase.Title ? 1 : 0; titleGroup.blocksRaycasts = false;
            gameGroup.alpha = phase == GamePhase.Practice || phase == GamePhase.Playing ? 1 : 0; gameGroup.blocksRaycasts = false;
            revealGroup.alpha = phase == GamePhase.Reveal ? 1 : 0; revealGroup.blocksRaycasts = false;
            endGroup.alpha = phase == GamePhase.End ? 1 : 0; endGroup.blocksRaycasts = false;
            titleGear.gameObject.SetActive(phase == GamePhase.Title);
            titleDriver.gameObject.SetActive(phase == GamePhase.Title);
            evidenceImage.gameObject.SetActive((phase == GamePhase.Practice || phase == GamePhase.Playing) && !string.IsNullOrEmpty(st.evidence));
        }

        void ShowTitleVisual()
        {
            titleGear.gameObject.SetActive(true); titleDriver.gameObject.SetActive(true);
            mossRoot.localPosition = new Vector3(-1.15f,-3.95f,-.95f);
            mossRoot.localRotation = Quaternion.Euler(0,0,8f);
            guideRingWorld.gameObject.SetActive(false);
            HideMeasureLines();
            for (int i = 0; i < plates.Length; i++) plates[i].gameObject.SetActive(true);
            for (int i = 0; i < boltHeads.Length; i++) boltHeads[i].gameObject.SetActive(true);
        }

        void ResetWorldForRun()
        {
            finalBird = false; correctClock = wrongClock = plateClock = 99f; fxBolt = fxPlate = -1;
            practiceDemo = false; practiceDemoClock = 0f;
            for (int p = 0; p < plates.Length; p++)
            {
                plates[p].gameObject.SetActive(true); plates[p].localPosition = plateHomes[p];
                plates[p].localScale = Vector3.one;
            }
            for (int i = 0; i < boltHeads.Length; i++)
            {
                boltHeads[i].gameObject.SetActive(true); boltHeads[i].localPosition = boltHomes[i];
                boltHeads[i].localScale = new Vector3(.34f,.22f,.34f);
            }
            for (int i = 0; i < trayBolts.Length; i++) trayBolts[i].gameObject.SetActive(false);
            for (int i = 0; i < birdWings.Length; i++) birdWings[i].gameObject.SetActive(false);
            mossRoot.localScale = Vector3.one;
            // 가로에선 판 3장이 전폭을 쓰므로 모스는 판 아래 레일 끝에 선다(볼트를 가리지 않게).
            mossRoot.localPosition = layoutLand ? new Vector3(-7.9f,-3.1f,-.9f) : new Vector3(-5.6f,-.6f,-.9f);
            HideMeasureLines(); guideRingWorld.gameObject.SetActive(false);
        }

        void ApplyRoundVisual(bool practice)
        {
            titleGear.gameObject.SetActive(false); titleDriver.gameObject.SetActive(false);
            for (int p = 0; p < plates.Length; p++)
            {
                plates[p].gameObject.SetActive(true);
                plates[p].localPosition = plateHomes[p];
                plates[p].localScale = Vector3.one;
                plates[p].localRotation = Quaternion.Euler(0, 0, layoutLand ? 0f : (p == 0 ? -4f : (p == 1 ? 5f : -2f)));
            }
            UpdateGeometryVisual();
            for (int i = 0; i < boltHeads.Length; i++)
            {
                boltHeads[i].gameObject.SetActive(true); boltHeads[i].localPosition = boltHomes[i];
                boltHeads[i].localScale = new Vector3(.34f,.22f,.34f);
                boltHeads[i].localRotation = Quaternion.Euler(90,0,(i*29 + current.rotation)%360);
            }
            promptText.text = current.prompt;
            evidenceImage.gameObject.SetActive(false); st.evidence = "";
            if (practice) PositionGuide(5, true); else PositionGuide(-1, false);
            HideMeasureLines();
            UpdateHitMap();
        }

        void UpdateGeometryVisual()
        {
            if (current == null || candidates == null) return;
            var g = BoltPureoRules.GeometryFor(current);
            Vector2 centroid = new Vector2(
                (g.vertices[0].x + g.vertices[1].x + g.vertices[2].x) / 3f,
                (g.vertices[0].y + g.vertices[1].y + g.vertices[2].y) / 3f);
            for (int p = 0; p < plates.Length; p++)
            {
                var mesh = plates[p].GetComponent<MeshFilter>().sharedMesh;
                SetTriangleMesh(mesh, g, .32f);
                plates[p].GetComponent<MeshCollider>().sharedMesh = null;
                plates[p].GetComponent<MeshCollider>().sharedMesh = mesh;
                for (int v = 0; v < 3; v++)
                {
                    Vector2 q = new Vector2(g.vertices[v].x, g.vertices[v].y);
                    Vector2 away = (q - centroid).normalized;
                    Vector3 pos = LocalPoint(g, g.vertices[v], -.38f) + new Vector3(away.x,away.y,0) * .34f;
                    vertexLabels[p,v].text = g.labels[v]; vertexLabels[p,v].transform.localPosition = pos;
                    vertexLabels[p,v].gameObject.SetActive(true);
                }
                UpdateRightAngleMarker(p, g);
            }
            for (int i = 0; i < boltHeads.Length; i++)
            {
                int p = i / 4;
                boltHomes[i] = LocalPoint(g, candidates[i].Point, -.31f);
                boltHeads[i].SetParent(plates[p], false);
                boltHeads[i].localPosition = boltHomes[i];
            }
            st.triangleGeometry = GeometrySummary(g);
            var parts = new System.Text.StringBuilder();
            for (int i = 0; i < candidates.Length; i++)
            {
                if (i > 0) parts.Append(';');
                parts.Append(i).Append(':').Append(candidates[i].x).Append(',').Append(candidates[i].y)
                    .Append('/').Append(candidates[i].evidenceMode == CenterKind.Circumcenter ? 'O' : 'I')
                    .Append('/').Append(candidates[i].evidenceA).Append(',').Append(candidates[i].evidenceB).Append(',').Append(candidates[i].evidenceC)
                    .Append('/').Append(candidates[i].matches ? '1' : '0');
            }
            st.candidateGeometry = parts.ToString();
        }

        string GeometrySummary(TriangleGeometry g)
        {
            return g.id + ":" + g.labels[0] + g.vertices[0] + ";" + g.labels[1] + g.vertices[1] + ";" +
                g.labels[2] + g.vertices[2] + ";O" + g.circumcenter + ";I" + g.incenter;
        }

        void UpdateRightAngleMarker(int plateIndex, TriangleGeometry g)
        {
            int baseLine = plateIndex * 2;
            rightAngleLines[baseLine].gameObject.SetActive(false);
            rightAngleLines[baseLine+1].gameObject.SetActive(false);
            if (g.rightVertex < 0) return;
            int r = g.rightVertex, a = (r + 1) % 3, b = (r + 2) % 3;
            Vector3 vr = LocalPoint(g, g.vertices[r], -.39f);
            Vector3 va = LocalPoint(g, g.vertices[a], -.39f);
            Vector3 vb = LocalPoint(g, g.vertices[b], -.39f);
            Vector3 ua = (va - vr).normalized * .42f, ub = (vb - vr).normalized * .42f;
            Vector3 p0 = plates[plateIndex].TransformPoint(vr + ua);
            Vector3 corner = plates[plateIndex].TransformPoint(vr + ua + ub);
            Vector3 p1 = plates[plateIndex].TransformPoint(vr + ub);
            rightAngleLines[baseLine].gameObject.SetActive(true);
            rightAngleLines[baseLine].SetPosition(0,p0); rightAngleLines[baseLine].SetPosition(1,corner);
            rightAngleLines[baseLine+1].gameObject.SetActive(true);
            rightAngleLines[baseLine+1].SetPosition(0,corner); rightAngleLines[baseLine+1].SetPosition(1,p1);
        }

        void UpdateHitMap()
        {
            if (candidates == null) return;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < boltHeads.Length; i++)
            {
                if (i > 0) sb.Append(';');
                Vector3 p = cam.WorldToScreenPoint(boltHeads[i].position);
                sb.Append(Mathf.RoundToInt(p.x)).Append(',').Append(Mathf.RoundToInt(Screen.height-p.y));
            }
            st.hitMap = sb.ToString();
        }

        void UpdateWorldVisual(float dt)
        {
            worldClock += dt;
            if (titleGear.gameObject.activeSelf) titleGear.localRotation = Quaternion.Euler(0,0,worldClock*11f);
            for (int i = 0; i < rain.Length; i++)
            {
                var p = rain[i].localPosition; p.y -= dt * (2.8f + (i%5)*.28f);
                if (p.y < -8f) p.y += 17f; rain[i].localPosition = p;
            }
            for (int i = 0; i < bulbs.Length; i++)
            {
                float s = .92f + .12f * Mathf.Sin(worldClock * 2.2f + i * 1.7f);
                bulbs[i].localScale = Vector3.one * (.28f * s);
            }
            for (int i = 0; i < gardenBirds.Length; i++)
            {
                Vector3 p = gardenBirdHomes[i];
                p.y += Mathf.Sin(worldClock * (1.7f + i * .13f) + i * 1.4f) * .16f;
                p.x += Mathf.Sin(worldClock * .55f + i) * .08f;
                gardenBirds[i].localPosition = p;
                gardenBirds[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(worldClock * 2.1f + i) * 5f);
            }
            for (int i = 0; i < gardenLeaves.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                gardenLeaves[i].localRotation = Quaternion.Euler(0, 0,
                    side * (18f + (i % 4) * 9f) + Mathf.Sin(worldClock * 1.35f + i * .7f) * 8f);
            }
            if (practiceDemo && phase == GamePhase.Practice)
            {
                practiceDemoClock += dt;
                if (practiceDemoClock < 2.35f) ShowEvidenceLines(5);
                else if (practiceDemoClock < 2.45f) HideMeasureLines();
                else if (practiceDemoClock >= 3.2f)
                {
                    // 시범은 손가락 경로·측정선·릴리스 타이밍만 반복한다.
                    // SubmitBolt를 절대 호출하지 않으며 학생의 실제 홀드-릴리스만
                    // 연습을 끝낼 수 있다.
                    practiceDemoClock = 0f;
                    HideMeasureLines();
                }
            }
            // ambient idle motion: 모스의 호흡과 구리 꼬리, 빗물, 기어는 항상 살아 있다.
            float breathe = 1f + Mathf.Sin(worldClock * 2.8f) * .035f;
            if (correctClock > .9f && wrongClock > .6f) mossBody.localScale = new Vector3(1.72f*breathe,.80f/breathe,.74f);
            mossTail.localRotation = Quaternion.Euler(0,0,74f + Mathf.Sin(worldClock*3.2f)*9f);
            mossRoot.localPosition += Vector3.up * Mathf.Sin(worldClock * 1.7f) * .0008f;

            if (correctClock < 2f)
            {
                correctClock += dt;
                if (correctClock > .07f && fxBolt >= 0)
                {
                    float k = Mathf.Clamp01((correctClock-.07f)/.46f);
                    float e = 1f - Mathf.Pow(1f-k,3f);
                    boltHeads[fxBolt].localPosition = boltHomes[fxBolt] + new Vector3(0, .15f*e, -1.5f*e);
                    boltHeads[fxBolt].localRotation = Quaternion.Euler(90,0,e*1080f);
                    float squash = 1f + Mathf.Sin(k*Mathf.PI)*.34f;
                    mossBody.localScale = new Vector3(1.72f/squash,.80f*squash,.74f);
                }
                if (plateClock < 1.2f && fxPlate >= 0)
                {
                    plateClock += dt;
                    float k = Mathf.Clamp01(plateClock/.62f);
                    float overshoot = k < .8f ? k/.8f : 1f + Mathf.Sin((k-.8f)/.2f*Mathf.PI)*.08f;
                    plates[fxPlate].localPosition = plateHomes[fxPlate] + Vector3.down * (1.05f * overshoot);
                    plates[fxPlate].localRotation = Quaternion.Euler(0,0,(fxPlate%2==0?-1:1)*12f*overshoot);
                }
            }
            if (wrongClock < .8f)
            {
                wrongClock += dt;
                float k = wrongClock/.8f;
                mossRoot.localScale = new Vector3(1f+.08f*Mathf.Sin(k*10f), .55f+.45f*k, 1f);
            }
            if (finalBird)
            {
                for (int i = 0; i < birdWings.Length; i++)
                {
                    float flap = Mathf.Sin(worldClock*3f+i*2.1f)*8f;
                    birdWings[i].localRotation = Quaternion.Euler(0,0,-120+i*120+flap);
                }
                mossRoot.localPosition = Vector3.Lerp(mossRoot.localPosition, new Vector3(8.6f,-.2f,-1f), dt*1.4f);
            }
        }

        void UpdateUiVisual(float dt)
        {
            shownScore = Mathf.Lerp(shownScore, st.score, 1f - Mathf.Exp(-dt*9f));
            int sec = Mathf.Max(0, Mathf.CeilToInt(sessionLeft));
            hudText.text = "기어 " + sec + "초    기록 " + Mathf.RoundToInt(shownScore) + "    첫 시도 " + st.firstCorrect + "/" + st.firstAttempted;
            goalText.text = st.onboarding ? "흰 링을 누르고 거리² 25·25·25 뒤 손을 떼시오" : "조건에 맞는 볼트를 풀어 삼각판을 떨궈라";
            trayText.text = "임시 트레이  " + TraySymbol(0) + "  " + TraySymbol(1) + "  " + TraySymbol(2);
            evidenceTitle.text = st.heldBolt >= 0 ? "누르는 동안 열린 측정선" : "방금 확인한 측정 증거";
            if (!string.IsNullOrEmpty(st.evidence))
            {
                evidenceText.text = st.evidence;
            }
            if (toastClock < 1.5f)
            {
                toastClock += dt; toastText.alpha = Mathf.Clamp01(1f - Mathf.Max(0,toastClock-1.0f)/.5f);
            }
            else toastText.alpha = 0;
            if (rippleClock < .5f)
            {
                rippleClock += dt; float k = rippleClock/.5f;
                rippleRt.sizeDelta = Vector2.one * Mathf.Lerp(28,105,k);
                rippleImage.color = new Color(1,1,1,(1-k)*.75f);
            }
            else rippleImage.gameObject.SetActive(false);

            if (phase == GamePhase.Title)
            {
                float s = 1f + Mathf.Sin(worldClock*1.8f)*.025f;
                titleLogo.rectTransform.localScale = Vector3.one*s;
                titleStart.alpha = .68f + .32f*Mathf.Sin(worldClock*2.6f)*Mathf.Sin(worldClock*2.6f);
            }
            if (phase == GamePhase.End)
            {
                endClock += dt;
                float k = Mathf.Clamp01(endClock / 1.1f);
                float eased = 1f - Mathf.Pow(1f-k, 3f);
                endStats.text = "해체 " + Mathf.RoundToInt(st.solved*eased) + "/" + BoltPureoRules.Goal +
                    "\n첫 시도 " + Mathf.RoundToInt(st.firstCorrect*eased) + "/" + Mathf.RoundToInt(st.firstAttempted*eased) +
                    "\n기록 " + Mathf.RoundToInt(st.score*eased);
                endStats.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(k*Mathf.PI)*.06f);
            }
            // 흰 링 안내는 연습(정답이 발문에 이미 적힌 빗변 중점)에서만 쓴다.
            // 본판에서 링을 정답 볼트에 띄우면 판별 없이 링만 누르면 되는 정답 누설이다.
            if (phase == GamePhase.Practice && st.onboarding)
            {
                PositionGuide(5, true);
                guideBoost = Mathf.Max(0, guideBoost-dt);
            }
            else ghostFinger.gameObject.SetActive(false);
        }

        string TraySymbol(int slot) => slot < st.trayCount ? "●" : "○";

        void PositionGuide(int index, bool onboarding)
        {
            if (index < 0 || index >= boltHeads.Length || !boltHeads[index].gameObject.activeSelf) { ghostFinger.gameObject.SetActive(false); return; }
            Vector3 sp = cam.WorldToScreenPoint(boltHeads[index].position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, new Vector2(sp.x,sp.y), null, out Vector2 local);
            float cycle = (worldClock % 1.8f) / 1.8f;
            float pressOffset = cycle < .18f ? Mathf.Lerp(18f,0f,cycle/.18f) : (cycle > .78f ? Mathf.Lerp(0f,18f,(cycle-.78f)/.22f) : 0f);
            ghostFingerRt.anchoredPosition = local + Vector2.up * pressOffset;
            float pulse = cycle < .18f ? Mathf.Lerp(92f,68f,cycle/.18f) : (cycle < .78f ? 68f : Mathf.Lerp(68f,92f,(cycle-.78f)/.22f));
            if (practiceDemo) pulse += 16f;
            ghostFingerRt.sizeDelta = Vector2.one*pulse;
            ghostFinger.color = new Color(1,1,1,onboarding ? .92f : .42f);
            ghostFinger.gameObject.SetActive(true);
        }

        void BoostGuide() { guideBoost = 2.4f; }

        void PulseAt(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, screen, null, out Vector2 local);
            rippleRt.anchoredPosition = local; rippleClock = 0; rippleImage.gameObject.SetActive(true);
        }

        void PressTitleDriver()
        {
            MgfFx.Punch(titleDriver, .22f, .25f); MgfSfx.Play("whoosh", .35f);
        }

        bool RestartHit(Vector2 screen) => screen.y < Screen.height * .58f;

        void RefuseAt(Vector2 screen, string message)
        {
            toastCache = message; toastText.text = message; toastText.alpha = 1; toastClock = 0;
            PulseAt(screen); MgfSfx.Play("wrong", .16f);
            if (st.heldBolt >= 0) MgfFx.Punch(boltHeads[st.heldBolt], .10f, .20f);
        }

        int FindBoltAt(Vector2 screen)
        {
            if (boltHeads == null) return -1;
            float baseRadius = phase == GamePhase.Practice ? 82f : 58f;
            float threshold = Mathf.Max(72f, Screen.width/390f*baseRadius);
            int best = -1; float bestD = threshold*threshold;
            for (int i = 0; i < boltHeads.Length; i++)
            {
                if (!boltHeads[i].gameObject.activeSelf) continue;
                Vector3 p = cam.WorldToScreenPoint(boltHeads[i].position);
                float d = ((Vector2)p-screen).sqrMagnitude;
                if (d <= bestD) { bestD=d; best=i; }
            }
            return best;
        }

        void BeginHoldVisual(int index)
        {
            evidenceImage.gameObject.SetActive(true); ShowEvidenceLines(index);
            guideRingWorld.gameObject.SetActive(false); MgfFx.Punch(boltHeads[index], .12f, .24f);
        }

        void UpdateHoldVisual(int index, float progress)
        {
            if (index < 0) return;
            boltHeads[index].localRotation = Quaternion.Euler(90,0,(index*29+current.rotation)%360 + progress*540f);
            boltHeads[index].localPosition = boltHomes[index] + new Vector3(0,0,-.35f*progress);
            evidencePlate.localScale = Vector3.one*(.96f + .04f*progress);
        }

        void HoldReadyVisual(int index)
        {
            if (index < 0) return;
            toastText.text = "해제 준비 완료 — 손을 떼시오";
            toastText.alpha = 1; toastClock = 0;
            ghostFingerRt.sizeDelta = Vector2.one * 104f;
            MgfFx.Glow(boltHeads[index].position, Cream, 7, .12f);
            MgfSfx.Play("pop", .24f);
        }

        void BeginPracticeDemo(int index)
        {
            practiceDemo = true; practiceDemoClock = 0f; guideBoost = 3.2f;
            st.evidence = candidates[index].evidenceTitle + " " + candidates[index].evidenceValues;
            evidenceImage.gameObject.SetActive(true);
            toastText.text = "시범: 누르고 기다린 뒤 링이 커지면 손을 떼시오";
            toastText.alpha = 1; toastClock = 0;
            PositionGuide(index, true);
        }

        void CancelHoldVisual(int index)
        {
            if (index >= 0) { boltHeads[index].localPosition=boltHomes[index]; boltHeads[index].localScale=new Vector3(.34f,.22f,.34f); }
            HideMeasureLines();
        }

        void EndHoldVisual(int index, bool submitted)
        {
            if (!submitted && index >= 0) boltHeads[index].localPosition = boltHomes[index];
            HideMeasureLines();
        }

        void ShowEvidenceLines(int index)
        {
            HideMeasureLines();
            int p = index/4;
            var g = BoltPureoRules.GeometryFor(current);
            var picked = candidates[index];
            Vector3 startLocal = LocalPoint(g, picked.Point, -.42f);
            Vector3 start = plates[p].TransformPoint(startLocal);
            if (picked.evidenceMode == CenterKind.Circumcenter)
            {
                for (int i=0;i<3;i++)
                {
                    Vector3 vertex = plates[p].TransformPoint(LocalPoint(g,g.vertices[i],-.42f));
                    measureLines[i].gameObject.SetActive(true);
                    measureLines[i].SetPosition(0,start); measureLines[i].SetPosition(1,vertex);
                    int j=(i+1)%3;
                    Vector3 a=LocalPoint(g,g.vertices[i],-.425f), b=LocalPoint(g,g.vertices[j],-.425f);
                    Vector3 mid=(a+b)*.5f, side=(b-a).normalized;
                    Vector3 perp=new Vector3(-side.y,side.x,0)*1.45f;
                    constructionLines[i].gameObject.SetActive(true);
                    constructionLines[i].SetPosition(0,plates[p].TransformPoint(mid-perp));
                    constructionLines[i].SetPosition(1,plates[p].TransformPoint(mid+perp));
                }
            }
            else
            {
                Vector3 trueInner = LocalPoint(g,g.incenter,-.425f);
                for (int i=0;i<3;i++)
                {
                    int j=(i+1)%3;
                    Vector3 a=LocalPoint(g,g.vertices[i],-.42f), b=LocalPoint(g,g.vertices[j],-.42f);
                    Vector3 ab=b-a;
                    float t=Vector3.Dot(startLocal-a,ab)/Mathf.Max(.0001f,Vector3.Dot(ab,ab));
                    // 수선의 발은 변을 품은 직선 위의 실제 발이다(밖의 점이면 연장선 위).
                    Vector3 foot=a+ab*t;
                    measureLines[i].gameObject.SetActive(true);
                    measureLines[i].SetPosition(0,start); measureLines[i].SetPosition(1,plates[p].TransformPoint(foot));
                    constructionLines[i].gameObject.SetActive(true);
                    constructionLines[i].SetPosition(0,plates[p].TransformPoint(a));
                    constructionLines[i].SetPosition(1,plates[p].TransformPoint(trueInner));
                }
            }
        }

        void HideMeasureLines()
        {
            for (int i=0;i<measureLines.Length;i++) if (measureLines[i]) measureLines[i].gameObject.SetActive(false);
            for (int i=0;i<constructionLines.Length;i++) if (constructionLines[i]) constructionLines[i].gameObject.SetActive(false);
        }

        void PracticeWrong(int index, string reason)
        {
            WrongVisual(index, 0, reason); st.lives = BoltPureoRules.StartLives;
            revealTitle.text = "측정 증거를 다시 보시오"; revealReason.text = reason;
            toastText.text = reason; toastText.alpha=1; toastClock=0;
            BoostGuide();
        }

        void CorrectVisual(int index, bool practice, int solvedCount)
        {
            fxBolt=index; fxPlate=Mathf.Clamp((solvedCount-1)/2,0,2); correctClock=0; plateClock=0; wrongClock=99f;
            MgfFx.Glow(boltHeads[index].position, Cream, 10 + solvedCount*2, .18f + solvedCount*.015f);
            revealTitle.text = practice ? "빗변의 중점 = 외심" : st.requirement + " 볼트 해제";
            revealReason.text = practice
                ? "직각삼각형의 외심은 빗변의 중점이다.\nPA²=PB²=PC²=25"
                : (current.target==CenterKind.Circumcenter
                    ? "세 꼭짓점까지의 거리가 같으므로 외심이다."
                    : "세 변까지의 수선 거리가 같으므로 내심이다.");
            if (!practice && solvedCount%2==0)
            {
                int wing=Mathf.Clamp(solvedCount/2-1,0,2); birdWings[wing].gameObject.SetActive(true);
            }
        }

        void WrongVisual(int index, int trayCount, string reason)
        {
            fxBolt=index; wrongClock=0; correctClock=99f;
            if (trayCount>0 && trayCount<=3) trayBolts[trayCount-1].gameObject.SetActive(true);
            MgfFx.Shake(cam, trayCount>=4?.22f:.09f, trayCount>=4?.34f:.18f);
            MgfFx.Punch(boltHeads[index], .12f, .22f);
            revealTitle.text = trayCount>=4 ? "구동축 잠김" : "임시 트레이에 남았다";
            revealReason.text = reason;
        }

        void RestoreAfterWrong()
        {
            if (fxBolt>=0) { boltHeads[fxBolt].localPosition=boltHomes[fxBolt]; boltHeads[fxBolt].localScale=new Vector3(.34f,.22f,.34f); }
            mossRoot.localScale=Vector3.one; st.evidence=""; evidenceImage.gameObject.SetActive(false);
            HideMeasureLines(); BoostGuide();
        }

        void EndVisual(bool clear, string reason)
        {
            endClock = 0f;
            endTitle.text = clear ? "기계 새, 출격" : "기어가 멈췄다";
            endStats.text = "해체 0/" + BoltPureoRules.Goal + "\n첫 시도 0/0\n기록 0";
            endReasonText.text = reason;
            if (clear)
            {
                finalBird=true;
                for(int i=0;i<birdWings.Length;i++) birdWings[i].gameObject.SetActive(true);
                MgfFx.Glow(new Vector3(8.8f,0,-1f), Cream, 34, .36f);
            }
            else finalBird=false;
        }
    }
}
