// 구슬 새총 — 황록 연못 2.5D 디오라마, 잎 UI, 움직이는 온보딩과 식물 피드백.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.GuseulSaechong
{
    public partial class GuseulSaechongGame
    {
        static readonly Color Pond = new Color32(205, 211, 75, 255);
        static readonly Color PondLight = new Color32(232, 226, 99, 255);
        static readonly Color Leaf = new Color32(73, 97, 61, 255);
        static readonly Color LeafLight = new Color32(122, 148, 69, 255);
        static readonly Color Violet = new Color32(120, 84, 175, 255);
        static readonly Color VioletLight = new Color32(185, 151, 220, 255);
        static readonly Color Ivory = new Color32(243, 238, 220, 255);
        static readonly Color Ink = new Color32(38, 58, 44, 255);
        static readonly Color Shell = new Color32(39, 43, 38, 255);
        static readonly Color WaterBlue = new Color32(132, 190, 170, 255);

        Camera cam;
        Vector3 cameraHome;
        Transform worldRoot, plantRoot, plantStem, plantLeafL, plantLeafR, pouchWorld;
        readonly Transform[] lilyPads = new Transform[18];
        readonly Vector3[] lilyHomes = new Vector3[18];
        readonly Transform[] seedPods = new Transform[6];
        readonly Vector3[] seedHomes = new Vector3[6];
        readonly Transform[] pathLeaves = new Transform[6];
        readonly Transform[] flyingMarbles = new Transform[8];
        readonly Transform[] waterRings = new Transform[6];
        Material pondMat, pondLightMat, leafMat, leafLightMat, violetMat, ivoryMat, inkMat, ringMat;
        float worldClock;
        float correctFxClock = 20f;
        float wrongFxClock = 20f;
        int podBeat = -1;

        RectTransform uiRoot, titleRoot, gameRoot, revealRoot, endRoot;
        CanvasGroup titleGroup, gameGroup, revealGroup, endGroup;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleCtaText;
        RectTransform titlePod, titleCta;
        TextMeshProUGUI hudText, goalText, promptText, countText, pullText, recordText, toastText;
        TextMeshProUGUI targetNum, targetDen, fractionLabel;
        Image targetBar;
        RectTransform hudLeaf, promptLeaf, countLeaf, pouchUi, recordsLeaf;
        readonly RectTransform[] marbleRt = new RectTransform[GuseulRules.MaxMarbles];
        readonly Image[] marbleImage = new Image[GuseulRules.MaxMarbles];
        readonly TextMeshProUGUI[] marbleNumber = new TextMeshProUGUI[GuseulRules.MaxMarbles];
        readonly float[] marblePunch = new float[GuseulRules.MaxMarbles];
        readonly Image[] safetyLeaves = new Image[GuseulRules.StartLives];
        Image guideRing, ghostFinger, guidePath, guideArrow, rippleImage, pouchImage, failureShell;
        RectTransform guideRingRt, ghostFingerRt, guidePathRt, guideArrowRt, rippleRt, failureShellRt;
        TextMeshProUGUI guideText, failureText;
        RectTransform evidenceLeaf;
        TextMeshProUGUI evidenceTitle, evidenceBody, evidenceEquals;
        TextMeshProUGUI evidenceLeftNum, evidenceLeftDen, evidenceRightNum, evidenceRightDen;
        Image evidenceLeftBar, evidenceRightBar;
        TextMeshProUGUI endTitle, endStats, endReasonText, restartText;
        RectTransform restartLeaf;

        Sprite leafSprite, leafDarkSprite, marblePurpleSprite, marbleIvorySprite, ringSprite, arrowSprite, softDiscSprite;
        bool layoutLand;
        int layoutW = -1, layoutH = -1;
        float guideClock, guideBoost, toastClock = 20f, rippleClock = 20f, refuseClock = 20f;
        float shownScore, shownEndScore;
        int lastTimerSecond = -1;
        readonly string[] recordLines = new string[GuseulRules.ProblemCount];
        string toastCache = "";

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        RectTransform Full(string name, Transform parent)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        Image Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite)
        {
            var rt = R(name, parent, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        TextMeshProUGUI Txt(string name, Transform parent, string text, Vector2 anchor, Vector2 pos,
            float size, Color color, float width, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R(name, parent, anchor, pos, new Vector2(width, size * 3.2f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        Sprite MakeLeafSprite(bool dark)
        {
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                name = dark ? "Failure shell cutout" : "Satin leaf cutout",
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[S * S];
            // 형태·잎맥만 텍스처에 굽고 색은 Image.tint가 정한다.
            // 색까지 두 번 곱하면 밝은 발문 잎도 검게 죽는다.
            Color baseColor = Color.white;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float nx = (x + .5f) / S * 2f - 1f;
                float ny = (y + .5f) / S * 2f - 1f;
                float width = Mathf.Clamp01(1f - Mathf.Abs(nx) * .68f);
                float shape = 1f - (nx * nx * .62f + ny * ny / Mathf.Max(.18f, width * width));
                float a = Mathf.Clamp01(shape * 12f);
                float vein = Mathf.Clamp01(1f - Mathf.Abs(ny - nx * .08f) * 55f) * .12f;
                float edge = Mathf.Clamp01(shape * 5f);
                Color c = Color.Lerp(baseColor * .82f, baseColor * 1.12f, edge + vein);
                px[y * S + x] = new Color(c.r, c.g, c.b, a);
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        Sprite MakeMarbleSprite(bool isPurple)
        {
            const int S = 96;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                name = isPurple ? "Dotted violet ceramic marble" : "Striped ivory ceramic marble",
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[S * S];
            Color body = isPurple ? Violet : Ivory;
            Color mark = isPurple ? Ivory : Leaf;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float nx = (x + .5f) / S * 2f - 1f;
                float ny = (y + .5f) / S * 2f - 1f;
                float rr = nx * nx + ny * ny;
                float a = Mathf.Clamp01((.94f - Mathf.Sqrt(rr)) * 18f);
                float shade = Mathf.Clamp01(.72f + (ny + .4f) * .18f - nx * .08f);
                Color c = body * shade;
                bool pattern;
                if (isPurple)
                {
                    float gx = Mathf.Repeat((x + y * .36f) / 19f, 1f) - .5f;
                    float gy = Mathf.Repeat((y - x * .18f) / 19f, 1f) - .5f;
                    pattern = gx * gx + gy * gy < .035f && rr < .72f;
                }
                else pattern = Mathf.Abs(Mathf.Sin((x + y * .42f) * .20f)) > .88f && rr < .72f;
                if (pattern) c = Color.Lerp(c, mark, .34f);
                float highlight = Mathf.Clamp01(1f - Mathf.Sqrt((nx + .34f) * (nx + .34f) + (ny - .34f) * (ny - .34f)) * 5f);
                c = Color.Lerp(c, Color.white, highlight * .72f);
                px[y * S + x] = new Color(c.r, c.g, c.b, a);
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        Sprite MakeRingSprite()
        {
            const int S = 96;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Leaf vein guide ring", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = (x + .5f) / S * 2f - 1f, dy = (y + .5f) / S * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((.96f - d) * 18f) * Mathf.Clamp01((d - .72f) * 18f);
                px[y * S + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        Sprite MakeArrowSprite()
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = "Leaf vein arrow", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float nx = (x + .5f) / S, ny = Mathf.Abs((y + .5f) / S - .5f) * 2f;
                bool shaft = nx < .62f && ny < .18f;
                bool head = nx > .38f && ny < (nx - .30f) * .88f;
                px[y * S + x] = new Color32(255, 255, 255, (byte)((shaft || head) ? 255 : 0));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        Sprite MakeSoftDiscSprite()
        {
            return Sprite.Create(MgfLook.SoftDot, new Rect(0, 0, MgfLook.SoftDot.width, MgfLook.SoftDot.height), new Vector2(.5f, .5f), 100f);
        }

        GameObject Pod(string name, Vector3 pos, Transform parent)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false); root.localPosition = pos;
            var stem = MgfLook.Prim(PrimitiveType.Cylinder, name + " stem", new Vector3(0, .62f, 0), new Vector3(.16f, .65f, .16f), leafMat, root, false);
            stem.transform.localRotation = Quaternion.Euler(-5f, 0, 7f);
            MgfLook.Prim(PrimitiveType.Sphere, name + " round pod", new Vector3(0, 1.42f, 0), new Vector3(.72f, .64f, .72f), leafLightMat, root, false);
            for (int i = 0; i < 5; i++)
            {
                var petal = MgfLook.Prim(PrimitiveType.Sphere, name + " sepal " + i,
                    new Vector3(Mathf.Cos(i * Mathf.PI * .4f) * .54f, 1.26f, Mathf.Sin(i * Mathf.PI * .4f) * .54f),
                    new Vector3(.42f, .14f, .72f), leafMat, root, false);
                petal.transform.localRotation = Quaternion.Euler(0, -i * 72f, 12f);
            }
            return root.gameObject;
        }

        void BuildWorld()
        {
            pondMat = MgfLook.Lit(Pond, .52f, 0f);
            pondLightMat = MgfLook.Lit(PondLight, .45f, 0f);
            leafMat = MgfLook.Lit(Leaf, .36f, 0f);
            leafLightMat = MgfLook.Lit(LeafLight, .52f, 0f);
            violetMat = MgfLook.Lit(Violet, .78f, .04f);
            ivoryMat = MgfLook.Lit(Ivory, .74f, .02f);
            inkMat = MgfLook.Lit(Ink, .22f, 0f);
            ringMat = MgfLook.Alpha(new Color(WaterBlue.r, WaterBlue.g, WaterBlue.b, .34f), MgfLook.SoftDot);

            MgfLook.Sky(new Color32(225, 227, 122, 255), new Color32(189, 207, 101, 255), new Color32(103, 143, 92, 255), .82f);
            MgfLook.Sun(new Vector3(48f, -34f, 18f), new Color(1f, .96f, .75f), 1.12f, .58f);
            cam = MgfLook.Camera(new Vector3(0, 17.5f, -17.6f), new Vector3(.8f, 0f, 2.5f), 38f);
            cam.orthographic = true; cam.orthographicSize = 10.6f; cameraHome = cam.transform.position;
            cam.backgroundColor = Pond;

            worldRoot = new GameObject("Chartreuse pond diorama").transform;
            MgfLook.Block("Whole pond water", new Vector3(.5f, -.72f, 3.6f), new Vector3(31f, .8f, 25f), .34f, pondMat, worldRoot);
            MgfLook.Block("Far moss bank", new Vector3(0, -.15f, 10.4f), new Vector3(29f, 1.3f, 3.5f), .62f, leafMat, worldRoot);

            for (int i = 0; i < lilyPads.Length; i++)
            {
                float x = -7.4f + (i % 6) * 3.05f + (i / 6) * .42f;
                float z = -1.1f + (i / 6) * 4.05f + (i % 2) * .38f;
                lilyHomes[i] = new Vector3(x, -.19f + (i % 3) * .025f, z);
                var pad = MgfLook.Prim(PrimitiveType.Cylinder, "Layered lily pad " + i, lilyHomes[i],
                    new Vector3(1.18f + (i % 3) * .14f, .07f, .84f + (i % 2) * .12f),
                    i % 3 == 0 ? leafLightMat : leafMat, worldRoot, false);
                pad.transform.localRotation = Quaternion.Euler(0, i * 37f, 0);
                lilyPads[i] = pad.transform;
            }

            plantRoot = new GameObject("Original living leaf slingshot").transform;
            plantRoot.SetParent(worldRoot, false); plantRoot.localPosition = new Vector3(-2.8f, -.05f, 1.15f);
            plantStem = MgfLook.Prim(PrimitiveType.Capsule, "Breathing green stem", new Vector3(0, 1.24f, 0),
                new Vector3(.58f, 1.42f, .58f), leafLightMat, plantRoot, false).transform;
            plantLeafL = MgfLook.Prim(PrimitiveType.Sphere, "Left sling leaf", new Vector3(-.82f, 2.54f, 0),
                new Vector3(1.22f, .25f, .72f), leafMat, plantRoot, false).transform;
            plantLeafL.localRotation = Quaternion.Euler(0, 16f, -28f);
            plantLeafR = MgfLook.Prim(PrimitiveType.Sphere, "Right sling leaf", new Vector3(.82f, 2.54f, 0),
                new Vector3(1.22f, .25f, .72f), leafMat, plantRoot, false).transform;
            plantLeafR.localRotation = Quaternion.Euler(0, -16f, 28f);
            pouchWorld = MgfLook.Prim(PrimitiveType.Sphere, "Flexible leaf pouch", new Vector3(0, 2.24f, -.18f),
                new Vector3(.78f, .24f, .72f), leafLightMat, plantRoot, false).transform;
            MgfLook.Prim(PrimitiveType.Sphere, "Tiny eye left", new Vector3(-.20f, 1.72f, -.52f), new Vector3(.10f, .12f, .10f), inkMat, plantRoot, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Tiny eye right", new Vector3(.20f, 1.72f, -.52f), new Vector3(.10f, .12f, .10f), inkMat, plantRoot, false);

            for (int i = 0; i < seedPods.Length; i++)
            {
                float x = -.2f + i * 1.35f;
                float z = 3.1f + (i % 2) * 1.05f + i * .18f;
                seedHomes[i] = new Vector3(x, -.22f, z);
                seedPods[i] = Pod("Sleeping seedpod " + (i + 1), seedHomes[i], worldRoot).transform;
                var path = MgfLook.Prim(PrimitiveType.Cylinder, "Permanent stepping leaf " + (i + 1),
                    new Vector3(-.05f + i * 1.30f, -.10f, 5.8f + (i % 2) * .58f),
                    new Vector3(.84f, .06f, .62f), leafLightMat, worldRoot, false);
                path.transform.localScale = Vector3.one * .001f;
                pathLeaves[i] = path.transform;
            }

            for (int i = 0; i < flyingMarbles.Length; i++)
            {
                var marble = MgfLook.Prim(PrimitiveType.Sphere, "Pooled flying marble " + i, Vector3.zero,
                    Vector3.one * .32f, i % 2 == 0 ? violetMat : ivoryMat, worldRoot, false);
                marble.SetActive(false); flyingMarbles[i] = marble.transform;
            }
            for (int i = 0; i < waterRings.Length; i++)
            {
                var ring = MgfLook.Prim(PrimitiveType.Quad, "Tactile water ring " + i,
                    new Vector3(-2f + i * 2f, -.06f, 1.2f + (i % 2) * 3f), new Vector3(1f, 1f, 1f), ringMat, worldRoot, false);
                ring.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                waterRings[i] = ring.transform;
            }
        }

        void BuildUi()
        {
            leafSprite = MakeLeafSprite(false);
            leafDarkSprite = MakeLeafSprite(true);
            marblePurpleSprite = MakeMarbleSprite(true);
            marbleIvorySprite = MakeMarbleSprite(false);
            ringSprite = MakeRingSprite();
            arrowSprite = MakeArrowSprite();
            softDiscSprite = MakeSoftDiscSprite();
            uiRoot = Full("Leaf pond UI", MgfText.Canvas.transform);

            titleRoot = Full("Title world gate", uiRoot); titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
            titlePod = Img("Hanging seedpod title board", titleRoot, new Vector2(.5f, .78f), Vector2.zero,
                new Vector2(360, 176), Leaf, leafSprite).rectTransform;
            titleLogo = Txt("Seedpod logo", titlePod, "구슬 새총", new Vector2(.5f, .60f), Vector2.zero, 54, Ivory, 340);
            titleLogo.outlineColor = Ink; titleLogo.outlineWidth = .25f;
            titleTag = Txt("Pond tagline", titlePod, "구슬을 섞어 쏴라", new Vector2(.5f, .20f), Vector2.zero, 18, PondLight, 320);
            titleMeta = Txt("Title grade and record", titleRoot, "중학교 2학년 · 확률", new Vector2(.5f, .59f), Vector2.zero, 14, Ink, 300);
            titleCta = Img("Living leaf start action", titleRoot, new Vector2(.5f, .17f), Vector2.zero,
                new Vector2(246, 116), LeafLight, leafSprite).rectTransform;
            titleCtaText = Txt("Single start action", titleCta, "연못 깨우기", new Vector2(.5f, .5f), Vector2.zero, 24, Ivory, 220);

            gameRoot = Full("Mixture play UI", uiRoot); gameGroup = gameRoot.gameObject.AddComponent<CanvasGroup>();
            hudLeaf = Img("Top leaf HUD", gameRoot, new Vector2(.5f, .955f), Vector2.zero, new Vector2(372, 58), Leaf, leafSprite).rectTransform;
            hudText = Txt("Animated score HUD", hudLeaf, "", new Vector2(.5f, .5f), Vector2.zero, 13.5f, Ivory, 354);
            goalText = Txt("Persistent learning goal", gameRoot, "구슬을 배합해 확률을 맞추면 잎길이 열린다",
                new Vector2(.5f, .905f), Vector2.zero, 14, Ink, 360);
            promptLeaf = Img("Problem lily leaf", gameRoot, new Vector2(.5f, .80f), Vector2.zero,
                new Vector2(370, 142), Ivory, leafSprite).rectTransform;
            promptText = Txt("Textbook prompt", promptLeaf, "", new Vector2(.43f, .5f), Vector2.zero, 15, Ink, 274);
            promptText.rectTransform.sizeDelta = new Vector2(274, 126);
            fractionLabel = Txt("Target fraction label", promptLeaf, "목표", new Vector2(.84f, .77f), Vector2.zero, 12, Leaf, 72);
            targetNum = Txt("Target fraction numerator", promptLeaf, "1", new Vector2(.84f, .57f), Vector2.zero, 25, Ink, 70);
            targetBar = Img("Target thick fraction bar", promptLeaf, new Vector2(.84f, .48f), Vector2.zero, new Vector2(54, 5), Ink, softDiscSprite);
            targetDen = Txt("Target fraction denominator", promptLeaf, "3", new Vector2(.84f, .30f), Vector2.zero, 25, Ink, 70);

            countLeaf = Img("Live raw count leaf", gameRoot, new Vector2(.5f, .585f), Vector2.zero,
                new Vector2(342, 64), PondLight, leafSprite).rectTransform;
            countText = Txt("Live purple ivory total counts", countLeaf, "", new Vector2(.5f, .5f), Vector2.zero, 18, Ink, 320);
            for (int i = 0; i < marbleRt.Length; i++)
            {
                marbleImage[i] = Img("Mixture marble " + (i + 1), gameRoot, new Vector2(.5f, .4f), Vector2.zero,
                    new Vector2(54, 54), Ivory, marbleIvorySprite);
                marbleRt[i] = marbleImage[i].rectTransform;
                marbleNumber[i] = Txt("Marble number " + (i + 1), marbleRt[i], (i + 1).ToString(),
                    new Vector2(.5f, .5f), new Vector2(0, -1), 11, Ink, 34);
                marbleNumber[i].outlineColor = Ivory; marbleNumber[i].outlineWidth = .14f;
            }
            pouchUi = Img("Actual leaf pouch control", gameRoot, new Vector2(.5f, .145f), Vector2.zero,
                new Vector2(194, 112), LeafLight, leafSprite).rectTransform;
            pouchImage = pouchUi.GetComponent<Image>();
            pullText = Txt("Pull instruction", pouchUi, "잡아 아래로 당겨 놓기", new Vector2(.5f, .5f), Vector2.zero, 17, Ivory, 180);
            guideRing = Img("Animated border trail guide", gameRoot, new Vector2(.5f, .4f), Vector2.zero,
                new Vector2(82, 82), PondLight, ringSprite);
            guideRingRt = guideRing.rectTransform;
            ghostFinger = Img("Ghost hand point", gameRoot, new Vector2(.5f, .4f), Vector2.zero,
                new Vector2(38, 38), Ivory, softDiscSprite);
            ghostFingerRt = ghostFinger.rectTransform;
            guidePath = Img("Flowing chevron path", gameRoot, new Vector2(.5f, .4f), Vector2.zero,
                new Vector2(10, 7), PondLight, softDiscSprite);
            guidePathRt = guidePath.rectTransform;
            guideArrow = Img("Destination arrowhead", gameRoot, new Vector2(.5f, .4f), Vector2.zero,
                new Vector2(38, 24), PondLight, arrowSprite);
            guideArrowRt = guideArrow.rectTransform;
            guideText = Txt("One sentence onboarding", gameRoot, "구슬 두 개를 쓸어 보라색으로 바꾸시오",
                new Vector2(.5f, .235f), Vector2.zero, 14, Ink, 360);

            recordsLeaf = Img("Relative frequency record lily", gameRoot, new Vector2(.5f, .055f), Vector2.zero,
                new Vector2(366, 86), new Color(Leaf.r, Leaf.g, Leaf.b, .94f), leafSprite).rectTransform;
            recordText = Txt("Theory and relative frequency records", recordsLeaf, "정답 뒤 8→24→72회 관찰이 여기에 남는다",
                new Vector2(.5f, .5f), Vector2.zero, 12, Ivory, 344);

            failureShell = Img("Black failure shell", gameRoot, new Vector2(.79f, .52f), Vector2.zero,
                new Vector2(218, 104), Shell, leafDarkSprite);
            failureShellRt = failureShell.rectTransform;
            failureText = Txt("Wrong mixture memory", failureShellRt, "", new Vector2(.5f, .5f), Vector2.zero, 13, Ivory, 198);
            failureShell.gameObject.SetActive(false);
            for (int i = 0; i < safetyLeaves.Length; i++)
            {
                safetyLeaves[i] = Img("Safety leaf " + (i + 1), gameRoot, new Vector2(.10f + i * .065f, .91f), Vector2.zero,
                    new Vector2(34, 24), LeafLight, leafSprite);
            }

            rippleImage = Img("No dead tap water ripple", uiRoot, new Vector2(.5f, .5f), Vector2.zero,
                new Vector2(72, 72), WaterBlue, ringSprite);
            rippleRt = rippleImage.rectTransform;
            rippleImage.color = new Color(WaterBlue.r, WaterBlue.g, WaterBlue.b, 0);
            toastText = Txt("Immediate refusal reason", uiRoot, "", new Vector2(.5f, .275f), Vector2.zero, 14, Ivory, 360);
            toastText.outlineColor = Ink; toastText.outlineWidth = .24f;

            revealRoot = Full("Evidence reveal", uiRoot); revealGroup = revealRoot.gameObject.AddComponent<CanvasGroup>();
            evidenceLeaf = Img("Math evidence leaf", revealRoot, new Vector2(.5f, .62f), Vector2.zero,
                new Vector2(362, 202), Ivory, leafSprite).rectTransform;
            evidenceTitle = Txt("Evidence title", evidenceLeaf, "", new Vector2(.5f, .82f), Vector2.zero, 22, Ink, 330);
            evidenceBody = Txt("Count evidence", evidenceLeaf, "", new Vector2(.5f, .23f), Vector2.zero, 13, Leaf, 320);
            evidenceLeftNum = Txt("Made numerator", evidenceLeaf, "2", new Vector2(.30f, .58f), Vector2.zero, 25, Ink, 72);
            evidenceLeftBar = Img("Made fraction bar", evidenceLeaf, new Vector2(.30f, .50f), Vector2.zero, new Vector2(58, 5), Ink, softDiscSprite);
            evidenceLeftDen = Txt("Made denominator", evidenceLeaf, "6", new Vector2(.30f, .39f), Vector2.zero, 25, Ink, 72);
            evidenceEquals = Txt("Fraction equality", evidenceLeaf, "=", new Vector2(.50f, .50f), Vector2.zero, 26, Leaf, 50);
            evidenceRightNum = Txt("Target numerator", evidenceLeaf, "1", new Vector2(.70f, .58f), Vector2.zero, 25, Ink, 72);
            evidenceRightBar = Img("Target evidence fraction bar", evidenceLeaf, new Vector2(.70f, .50f), Vector2.zero, new Vector2(58, 5), Ink, softDiscSprite);
            evidenceRightDen = Txt("Target denominator", evidenceLeaf, "3", new Vector2(.70f, .39f), Vector2.zero, 25, Ink, 72);

            endRoot = Full("Result pond", uiRoot); endGroup = endRoot.gameObject.AddComponent<CanvasGroup>();
            endTitle = Txt("Result seedpod title", endRoot, "", new Vector2(.5f, .75f), Vector2.zero, 46, Ivory, 370);
            endTitle.outlineColor = Ink; endTitle.outlineWidth = .25f;
            var statsLeaf = Img("Result leaf", endRoot, new Vector2(.5f, .52f), Vector2.zero, new Vector2(338, 190), Ivory, leafSprite);
            endStats = Txt("Counted result", statsLeaf.transform, "", new Vector2(.5f, .62f), Vector2.zero, 23, Ink, 314);
            endReasonText = Txt("Result reason", statsLeaf.transform, "", new Vector2(.5f, .26f), Vector2.zero, 15, Leaf, 306);
            restartLeaf = Img("One tap restart leaf", endRoot, new Vector2(.5f, .22f), Vector2.zero,
                new Vector2(224, 110), LeafLight, leafSprite).rectTransform;
            restartText = Txt("Restart action", restartLeaf, "새 연못 깨우기", new Vector2(.5f, .5f), Vector2.zero, 21, Ivory, 208);

            LayoutUi();
        }

        void SetPhaseUi()
        {
            bool title = phase == GamePhase.Title;
            bool end = phase == GamePhase.End;
            bool play = !title && !end;
            titleGroup.alpha = title ? 1f : 0f; titleGroup.blocksRaycasts = title;
            gameGroup.alpha = play ? 1f : 0f; gameGroup.blocksRaycasts = play;
            revealGroup.alpha = phase == GamePhase.Reveal ? 1f : 0f;
            endGroup.alpha = end ? 1f : 0f; endGroup.blocksRaycasts = end;
            if (title)
            {
                int best = PlayerPrefs.GetInt("guseul-saechong-best-first", 0);
                titleMeta.text = best > 0 ? "중학교 2학년 · 확률   최고 첫 제출 " + best + "/6" : "중학교 2학년 · 확률";
            }
            if (play)
            {
                RefreshProblemUi();
                RefreshMarbles(false);
            }
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            string fraction = new IntFraction(current.targetN, current.targetD).Display;
            string shown = current.prompt.Replace("확률이 " + fraction + "이 되도록", "확률이 오른쪽 목표와 같아지도록");
            promptText.text = shown;
            SetFraction(targetNum, targetBar, targetDen, current.targetN, current.targetD);
            countText.text = "● 보라 " + CountPurple() + "개   ▨ 상아 " + (current.N - CountPurple()) + "개   전체 " + current.N + "개";
            goalText.text = phase == GamePhase.Practice || st.onboarding
                ? "구슬을 배합해 확률을 맞추면 잎길이 열린다"
                : "6개 완주 · 첫 제출 4/6 이상 · 안전잎 3장";
            failureShell.gameObject.SetActive(lastWrongPurple >= 0 && phase != GamePhase.Reveal);
            if (lastWrongPurple >= 0)
            {
                failureText.text = "검은 실패 조개\n보라 " + lastWrongPurple + " · 상아 " + (current.N - lastWrongPurple)
                    + "\n제출 확률 " + st.submittedFraction;
            }
            RefreshRecords();
        }

        void SetFraction(TextMeshProUGUI num, Image bar, TextMeshProUGUI den, int n, int d)
        {
            num.text = n.ToString();
            bool whole = d == 1;
            bar.gameObject.SetActive(!whole);
            den.gameObject.SetActive(!whole);
            den.text = d.ToString();
            float normalY = num == targetNum ? .57f : .58f;
            Vector2 anchor = new Vector2(num.rectTransform.anchorMin.x, whole ? .48f : normalY);
            num.rectTransform.anchorMin = num.rectTransform.anchorMax = anchor;
        }

        void RefreshMarbles(bool animate)
        {
            if (current == null) return;
            for (int i = 0; i < marbleRt.Length; i++)
            {
                bool visible = i < current.N;
                marbleRt[i].gameObject.SetActive(visible);
                if (!visible) continue;
                marbleImage[i].sprite = purple[i] ? marblePurpleSprite : marbleIvorySprite;
                marbleImage[i].color = Color.white;
                marbleNumber[i].color = purple[i] ? Ivory : Ink;
                if (animate) marblePunch[i] = .22f;
            }
            countText.text = "● 보라 " + CountPurple() + "개   ▨ 상아 " + (current.N - CountPurple()) + "개   전체 " + current.N + "개";
        }

        void PulseMarble(int index)
        {
            if (index >= 0 && index < marblePunch.Length) marblePunch[index] = .24f;
        }

        void FocusMarble(int index)
        {
            if (index < 0 || current == null || index >= current.N) return;
            guideRingRt.anchorMin = guideRingRt.anchorMax = marbleRt[index].anchorMin;
            guideRingRt.anchoredPosition = marbleRt[index].anchoredPosition;
            guideBoost = 1f;
        }

        void SetPullVisual(float t)
        {
            if (pouchUi == null) return;
            pouchUi.anchoredPosition = new Vector2(0, -t * 66f);
            pouchUi.localScale = new Vector3(1f + t * .10f, 1f - t * .18f, 1f);
            pullText.text = t > .72f ? "놓아 발사" : "잡아 아래로 당겨 놓기";
        }

        int HitMarble(Vector2 screen)
        {
            if (current == null) return -1;
            for (int i = 0; i < current.N; i++)
                if (marbleRt[i].gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(marbleRt[i], screen, null)) return i;
            return -1;
        }

        bool InPouch(Vector2 screen)
        {
            if (pouchUi == null) return false;
            if (RectTransformUtility.RectangleContainsScreenPoint(pouchUi, screen, null)) return true;
            Vector2 c = RectTransformUtility.WorldToScreenPoint(null, pouchUi.position);
            return Mathf.Abs(screen.x - c.x) < Mathf.Max(70f, Screen.width * .18f)
                && Mathf.Abs(screen.y - c.y) < Mathf.Max(58f, Screen.height * .08f);
        }

        void TriggerRipple(Vector2 screen, bool strong)
        {
            PlaceAtScreen(rippleRt, screen);
            rippleClock = 0f;
            rippleRt.sizeDelta = Vector2.one * (strong ? 84f : 64f);
        }

        void Refuse(string reason)
        {
            ShowToast(reason, 1.9f);
            refuseClock = 0f;
            guideBoost = 1f;
        }

        void RingFailureShell()
        {
            failureShell.gameObject.SetActive(true);
            MgfFx.Punch(failureShellRt, .12f, .24f);
        }

        void ShowToast(string message, float seconds)
        {
            toastCache = message;
            toastClock = 0f;
            toastText.text = message;
            toastText.gameObject.SetActive(true);
        }

        void ReplayGuide(bool bigger)
        {
            guideClock = 0f;
            if (bigger) guideBoost = 1f;
        }

        void ShowEvidence(bool correct, int madePurple, IntFraction made, int trials, int events)
        {
            evidenceTitle.text = correct ? (revealPractice ? "첫 잎길이 열렸다" : "배합이 맞았다") : "배합을 다시 살펴보시오";
            evidenceTitle.color = correct ? Leaf : Shell;
            IntFraction target = new IntFraction(current.targetN, current.targetD);
            SetFraction(evidenceLeftNum, evidenceLeftBar, evidenceLeftDen, made.n, made.d);
            SetFraction(evidenceRightNum, evidenceRightBar, evidenceRightDen, target.n, target.d);
            evidenceEquals.text = correct ? "=" : "≠";
            if (correct)
            {
                evidenceBody.text = revealPractice
                    ? "보라 " + madePurple + "개 ÷ 전체 " + current.N + "개 · 직접 만든 배합"
                    : "이론 " + target.Display + " · 8회 " + lastEvent8 + "/8 → 24회 " + lastEvent24
                        + "/24 → 72회 " + lastEvent72 + "/72";
                if (!revealPractice && st.solved > 0)
                    recordLines[st.solved - 1] = (st.solved) + ". 이론 " + target.Display + " · 8회 " + lastEvent8
                        + "/8 → 24회 " + lastEvent24 + "/24 → 72회 " + lastEvent72 + "/72";
            }
            else
            {
                evidenceBody.text = "제출: 보라 " + madePurple + "개 · 상아 " + (current.N - madePurple)
                    + "개\n정답 상태: 보라 " + current.CorrectPurple + "개 · 상아 " + (current.N - current.CorrectPurple) + "개";
            }
            RefreshRecords();
        }

        void RefreshRecords()
        {
            var lines = new List<string>(3);
            int start = layoutLand ? 0 : Mathf.Max(0, st.solved - 2);
            for (int i = start; i < recordLines.Length; i++) if (!string.IsNullOrEmpty(recordLines[i])) lines.Add(recordLines[i]);
            if (lines.Count == 0) recordText.text = "정답 뒤 8→24→72회 관찰이 여기에 남는다";
            else
            {
                if (!layoutLand && lines.Count > 2) lines.RemoveRange(0, lines.Count - 2);
                recordText.text = string.Join("\n", lines.ToArray());
            }
        }

        void ShowEndResult(bool clear, string reason)
        {
            shownEndScore = 0f;
            endTitle.text = clear ? "수련잎 길 완성" : reason == "mastery" ? "완주했지만 기록 미달" : "잎길이 멈췄다";
            endStats.text = "점수 0\n완료 " + st.solved + "/6   첫 제출 " + st.firstAttemptCorrect + "/6";
            endReasonText.text = clear ? "확률을 읽고 직접 만든 여섯 배합" : reason == "lives" ? "안전잎 0장 · 오답 배합을 다시 비교하시오"
                : reason == "time" ? "120초가 끝났다 · 사건부터 확인하시오" : "첫 제출 정답 4개 이상이 필요하다";
        }

        void LaunchPlant(bool correct)
        {
            if (correct)
            {
                correctFxClock = 0f; podBeat = -1;
                for (int i = 0; i < flyingMarbles.Length; i++)
                {
                    flyingMarbles[i].gameObject.SetActive(i < Mathf.Min(8, current.N));
                    flyingMarbles[i].GetComponent<Renderer>().sharedMaterial = purple[i] ? violetMat : ivoryMat;
                }
                if (!revealPractice && st.solved > 0)
                {
                    int path = Mathf.Clamp(st.solved - 1, 0, pathLeaves.Length - 1);
                    pathLeaves[path].gameObject.SetActive(true);
                }
            }
            else wrongFxClock = 0f;
        }

        void DropSafetyLeaf(int leavesRemaining)
        {
            int index = Mathf.Clamp(leavesRemaining, 0, safetyLeaves.Length - 1);
            if (index >= 0 && index < safetyLeaves.Length) MgfFx.Punch(safetyLeaves[index].transform, .28f, .34f);
        }

        void WrongCameraNudge()
        {
            // 화면 흔들림은 오답에만 사용한다.
            MgfFx.Shake(cam, st.lives <= 1 ? .16f : .09f, st.lives <= 1 ? .34f : .22f);
        }

        void ResetWorldForProblem(bool practice)
        {
            correctFxClock = wrongFxClock = 20f;
            for (int i = 0; i < flyingMarbles.Length; i++) flyingMarbles[i].gameObject.SetActive(false);
            if (practice)
            {
                for (int i = 0; i < pathLeaves.Length; i++) pathLeaves[i].localScale = Vector3.one * .001f;
                for (int i = 0; i < recordLines.Length; i++) recordLines[i] = "";
            }
            RefreshMarbles(false);
        }

        void AnimateWorld(float dt)
        {
            worldClock += dt;
            float breathe = Mathf.Sin(worldClock * 1.15f) * .035f;
            if (plantStem) plantStem.localScale = new Vector3(.58f - breathe * .4f, 1.42f + breathe, .58f - breathe * .4f);
            if (plantLeafL) plantLeafL.localRotation = Quaternion.Euler(0, 16f, -28f + Mathf.Sin(worldClock * 1.32f) * 3f);
            if (plantLeafR) plantLeafR.localRotation = Quaternion.Euler(0, -16f, 28f - Mathf.Sin(worldClock * 1.22f) * 3f);
            for (int i = 0; i < lilyPads.Length; i++)
            {
                lilyPads[i].localPosition = lilyHomes[i] + Vector3.up * Mathf.Sin(worldClock * .72f + i * .83f) * .035f;
                lilyPads[i].localRotation = Quaternion.Euler(0, i * 37f + Mathf.Sin(worldClock * .28f + i) * 2.4f, 0);
            }
            for (int i = 0; i < waterRings.Length; i++)
            {
                float t = Mathf.Repeat(worldClock * .18f + i * .17f, 1f);
                waterRings[i].localScale = Vector3.one * Mathf.Lerp(.3f, 2.4f, t);
            }

            if (correctFxClock < 4f)
            {
                correctFxClock += dt;
                float c = correctFxClock;
                float plantT = Mathf.Clamp01(c / .52f);
                float overshoot = 1f + Mathf.Sin(plantT * Mathf.PI) * .18f;
                plantRoot.localScale = new Vector3(1f / overshoot, overshoot, 1f / overshoot);
                for (int i = 0; i < flyingMarbles.Length; i++)
                {
                    if (!flyingMarbles[i].gameObject.activeSelf) continue;
                    float t = Mathf.Clamp01((c - i * .055f) / .70f);
                    Vector3 a = plantRoot.TransformPoint(new Vector3(0, 2.45f, -.15f));
                    Vector3 b = seedPods[i % seedPods.Length].TransformPoint(new Vector3(0, 1.35f, 0));
                    Vector3 p = Vector3.Lerp(a, b, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * (2.5f + i * .08f);
                    flyingMarbles[i].position = p;
                    float squash = 1f + Mathf.Sin(t * Mathf.PI) * .26f;
                    flyingMarbles[i].localScale = new Vector3(.32f / squash, .32f * squash, .32f / squash);
                    if (t >= 1f) flyingMarbles[i].gameObject.SetActive(false);
                }
                for (int i = 0; i < seedPods.Length; i++)
                {
                    float local = Mathf.Clamp01((c - .38f - i * .08f) / .42f);
                    float rise = Mathf.Sin(local * Mathf.PI) * 1.05f + local * .22f;
                    seedPods[i].localPosition = seedHomes[i] + Vector3.up * rise;
                    seedPods[i].localScale = Vector3.one * (1f + Mathf.Sin(local * Mathf.PI) * .18f);
                    if (local > .06f && i > podBeat)
                    {
                        podBeat = i;
                        MgfSfx.Play("pop", .20f + i * .02f);
                    }
                }
                if (!revealPractice && st.solved > 0)
                {
                    int path = Mathf.Clamp(st.solved - 1, 0, pathLeaves.Length - 1);
                    float t = Mathf.Clamp01((c - .58f) / .48f);
                    float s = Mathf.Sin(t * Mathf.PI * .5f);
                    pathLeaves[path].localScale = new Vector3(.84f * s, .06f * s, .62f * s);
                }
            }
            else
            {
                plantRoot.localScale = Vector3.one;
                for (int i = 0; i < seedPods.Length; i++)
                {
                    seedPods[i].localPosition = seedHomes[i] + Vector3.up * (Mathf.Sin(worldClock * .9f + i * .6f) * .07f);
                    seedPods[i].localScale = Vector3.one;
                }
            }

            if (wrongFxClock < 1f)
            {
                wrongFxClock += dt;
                float t = Mathf.Clamp01(wrongFxClock / .65f);
                float droop = Mathf.Sin(t * Mathf.PI) * .30f;
                plantRoot.localScale = new Vector3(1f + droop * .12f, 1f - droop, 1f + droop * .12f);
            }
        }

        void UpdateUi(float dt)
        {
            shownScore = Mathf.MoveTowards(shownScore, st.score, Mathf.Max(140f, Mathf.Abs(st.score - shownScore) * 4.5f) * dt);
            if (phase == GamePhase.End)
            {
                shownEndScore = Mathf.MoveTowards(shownEndScore, st.score, Mathf.Max(160f, st.score * 1.5f) * dt);
                endStats.text = "점수 " + Mathf.RoundToInt(shownEndScore) + "\n완료 " + st.solved + "/6   첫 제출 " + st.firstAttemptCorrect + "/6";
            }
            int sec = Mathf.CeilToInt(remainingTime);
            if (sec != lastTimerSecond || phase == GamePhase.Reveal)
            {
                lastTimerSecond = sec;
                string timer = phase == GamePhase.Practice ? "연습" : sec + "초";
                hudText.text = "점수 " + Mathf.RoundToInt(shownScore) + "   " + timer + "   잎 " + st.lives
                    + "   첫 제출 " + st.firstAttemptCorrect + "/6   완료 " + st.solved + "/6";
            }
            for (int i = 0; i < safetyLeaves.Length; i++) safetyLeaves[i].color = i < st.lives ? LeafLight : new Color(Shell.r, Shell.g, Shell.b, .28f);

            for (int i = 0; i < marblePunch.Length; i++)
            {
                if (marblePunch[i] > 0f)
                {
                    marblePunch[i] -= dt;
                    float t = 1f - Mathf.Clamp01(marblePunch[i] / .24f);
                    float s = 1f + Mathf.Sin(t * Mathf.PI) * .18f;
                    marbleRt[i].localScale = new Vector3(1f / s, s, 1f);
                }
                else if (marbleRt[i]) marbleRt[i].localScale = Vector3.one;
            }

            if (rippleClock < .55f)
            {
                rippleClock += dt;
                float t = Mathf.Clamp01(rippleClock / .55f);
                rippleRt.localScale = Vector3.one * Mathf.Lerp(.35f, 2.2f, t);
                rippleImage.color = new Color(WaterBlue.r, WaterBlue.g, WaterBlue.b, (1f - t) * .82f);
            }
            else rippleImage.color = new Color(WaterBlue.r, WaterBlue.g, WaterBlue.b, 0);

            toastClock += dt;
            toastText.gameObject.SetActive(toastClock < 2.8f);
            if (toastText.gameObject.activeSelf)
            {
                float a = Mathf.Clamp01((2.8f - toastClock) * 2f);
                toastText.color = new Color(Ivory.r, Ivory.g, Ivory.b, a);
            }
            refuseClock += dt;
            if (refuseClock < .45f && pouchUi != null)
            {
                float x = Mathf.Sin(refuseClock * 56f) * (1f - refuseClock / .45f) * 8f;
                pouchUi.anchoredPosition = new Vector2(x, pouchUi.anchoredPosition.y);
            }

            UpdateGuide(dt);
            UpdateStateCoordinates();
            if (phase == GamePhase.Title)
            {
                float p = .5f + .5f * Mathf.Sin(worldClock * 2.0f);
                titleCta.localScale = Vector3.one * (1f + p * .025f);
                titlePod.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(worldClock * .75f) * 1.6f);
            }
        }

        void UpdateGuide(float dt)
        {
            bool visible = phase == GamePhase.Practice && current != null;
            guideRing.gameObject.SetActive(visible);
            ghostFinger.gameObject.SetActive(visible);
            guidePath.gameObject.SetActive(visible);
            guideArrow.gameObject.SetActive(visible);
            guideText.gameObject.SetActive(visible);
            if (!visible) return;
            guideClock += dt;
            guideBoost = Mathf.MoveTowards(guideBoost, 0f, dt * .7f);
            float cycle = Mathf.Repeat(guideClock, 1.65f) / 1.65f;
            bool needPaint = CountPurple() < 2;
            Vector2 a, b;
            if (needPaint)
            {
                int fromIndex = purple[0] ? 1 : 0;
                int toIndex = purple[1] ? Mathf.Min(2, current.N - 1) : 1;
                a = RectTransformUtility.WorldToScreenPoint(null, marbleRt[fromIndex].position);
                b = RectTransformUtility.WorldToScreenPoint(null, marbleRt[toIndex].position);
                guideText.text = "구슬 두 개를 쓸어 보라색으로 바꾸시오";
                guideRingRt.position = marbleRt[fromIndex].position;
            }
            else
            {
                a = RectTransformUtility.WorldToScreenPoint(null, pouchUi.position);
                b = a + Vector2.down * Mathf.Max(70f, Screen.height * .10f);
                guideText.text = "잎 주머니를 아래로 당겼다 놓으시오";
                guideRingRt.position = pouchUi.position;
            }
            float eased = cycle < .8f ? 1f - Mathf.Pow(1f - cycle / .8f, 3f) : 1f;
            Vector2 p = Vector2.Lerp(a, b, eased);
            PlaceAtScreen(ghostFingerRt, p);
            PlaceSegment(guidePathRt, a, b, 7f + guideBoost * 4f);
            PlaceAtScreen(guideArrowRt, b);
            guideArrowRt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            float pulse = 1f + (.12f + guideBoost * .10f) * Mathf.Sin(worldClock * 5.5f);
            guideRingRt.localScale = Vector3.one * pulse;
            ghostFingerRt.localScale = Vector3.one * (1f + guideBoost * .28f);
        }

        void ApplyResponsiveLayout()
        {
            bool land = Screen.width >= Screen.height;
            if (layoutW != Screen.width || layoutH != Screen.height || land != layoutLand)
            {
                layoutW = Screen.width; layoutH = Screen.height; layoutLand = land;
                LayoutUi();
                RefreshRecords();
            }
            cam.orthographicSize = layoutLand ? 8.1f : 10.8f;
            cam.transform.position = layoutLand ? new Vector3(1.0f, 14.8f, -17.4f) : new Vector3(0f, 17.5f, -17.6f);
            cam.transform.LookAt(layoutLand ? new Vector3(1.3f, 0f, 3.0f) : new Vector3(.8f, 0f, 2.5f));
            cameraHome = cam.transform.position;
        }

        static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos)
        {
            if (!rt) return;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
        }

        void LayoutUi()
        {
            layoutLand = Screen.width >= Screen.height;
            if (!layoutLand)
            {
                Anchor(titlePod, new Vector2(.5f, .78f), Vector2.zero);
                Anchor(titleMeta.rectTransform, new Vector2(.5f, .59f), Vector2.zero);
                Anchor(titleCta, new Vector2(.5f, .17f), Vector2.zero);
                Anchor(hudLeaf, new Vector2(.5f, .958f), Vector2.zero);
                Anchor(goalText.rectTransform, new Vector2(.5f, .905f), Vector2.zero);
                Anchor(promptLeaf, new Vector2(.5f, .795f), Vector2.zero);
                Anchor(countLeaf, new Vector2(.5f, .595f), Vector2.zero);
                Anchor(pouchUi, new Vector2(.5f, .155f), Vector2.zero);
                Anchor(recordsLeaf, new Vector2(.5f, .055f), Vector2.zero);
                Anchor(failureShellRt, new Vector2(.5f, .285f), Vector2.zero);
                Anchor(evidenceLeaf, new Vector2(.5f, .61f), Vector2.zero);
                Anchor(toastText.rectTransform, new Vector2(.5f, .275f), Vector2.zero);
                recordsLeaf.sizeDelta = new Vector2(366, 86);
                promptLeaf.sizeDelta = new Vector2(370, 142);
                for (int i = 0; i < marbleRt.Length; i++)
                {
                    int col = i % 6, row = i / 6;
                    Anchor(marbleRt[i], new Vector2(Mathf.Lerp(.13f, .87f, col / 5f), row == 0 ? .455f : .375f), Vector2.zero);
                    marbleRt[i].sizeDelta = new Vector2(54, 54);
                }
                for (int i = 0; i < safetyLeaves.Length; i++) safetyLeaves[i].gameObject.SetActive(false);
            }
            else
            {
                Anchor(titlePod, new Vector2(.50f, .76f), Vector2.zero);
                Anchor(titleMeta.rectTransform, new Vector2(.50f, .55f), Vector2.zero);
                Anchor(titleCta, new Vector2(.50f, .22f), Vector2.zero);
                // CanvasScaler grows UI with landscape height. Keep the wide
                // left information cards inside the safe area even at 1280px,
                // rather than anchoring their scaled centres too close to x=0.
                Anchor(hudLeaf, new Vector2(.31f, .93f), Vector2.zero);
                // The panorama's upper-left stack is dense after CanvasScaler
                // expansion. Keep the persistent goal in the open lower-right
                // water, where it remains visible without covering the prompt.
                Anchor(goalText.rectTransform, new Vector2(.75f, .12f), Vector2.zero);
                Anchor(promptLeaf, new Vector2(.31f, .70f), Vector2.zero);
                Anchor(countLeaf, new Vector2(.31f, .53f), Vector2.zero);
                Anchor(pouchUi, new Vector2(.205f, .13f), Vector2.zero);
                Anchor(recordsLeaf, new Vector2(.75f, .78f), Vector2.zero);
                Anchor(failureShellRt, new Vector2(.80f, .52f), Vector2.zero);
                Anchor(evidenceLeaf, new Vector2(.75f, .42f), Vector2.zero);
                Anchor(toastText.rectTransform, new Vector2(.50f, .18f), Vector2.zero);
                recordsLeaf.sizeDelta = new Vector2(352, 190);
                promptLeaf.sizeDelta = new Vector2(430, 154);
                for (int i = 0; i < marbleRt.Length; i++)
                {
                    int col = i % 6, row = i / 6;
                    Anchor(marbleRt[i], new Vector2(Mathf.Lerp(.07f, .34f, col / 5f), row == 0 ? .40f : .295f), Vector2.zero);
                    marbleRt[i].sizeDelta = new Vector2(58, 58);
                }
                // Lives are already stated in the HUD; separate icons collided
                // with the wide persistent-goal line at 1280–2000px.
                for (int i = 0; i < safetyLeaves.Length; i++) safetyLeaves[i].gameObject.SetActive(false);
            }
            SetPullVisual(0f);
            UpdateStateCoordinates();
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

        float ReferenceScale()
        {
            var scaler = MgfText.Canvas.GetComponent<CanvasScaler>();
            float sx = Screen.width / scaler.referenceResolution.x;
            float sy = Screen.height / scaler.referenceResolution.y;
            return Mathf.Sqrt(Mathf.Max(.0001f, sx * sy));
        }

        void UpdateStateCoordinates()
        {
            st.screenW = Screen.width; st.screenH = Screen.height;
            for (int i = 0; i < marbleRt.Length; i++)
            {
                Vector2 p = RectTransformUtility.WorldToScreenPoint(null, marbleRt[i].position);
                st.marblePx[i * 2] = Mathf.RoundToInt(p.x);
                st.marblePx[i * 2 + 1] = Mathf.RoundToInt(Screen.height - p.y);
            }
            Vector2 pouch = RectTransformUtility.WorldToScreenPoint(null, pouchUi.position);
            st.pouchPx[0] = Mathf.RoundToInt(pouch.x);
            st.pouchPx[1] = Mathf.RoundToInt(Screen.height - pouch.y);
            st.pouchPx[2] = st.pouchPx[0];
            st.pouchPx[3] = Mathf.Min(Screen.height - 8, st.pouchPx[1] + Mathf.RoundToInt(Mathf.Max(62f, Screen.height * .09f)));
            Vector2 restart = RectTransformUtility.WorldToScreenPoint(null, restartLeaf.position);
            st.restartPx[0] = Mathf.RoundToInt(restart.x);
            st.restartPx[1] = Mathf.RoundToInt(Screen.height - restart.y);
        }
    }
}
