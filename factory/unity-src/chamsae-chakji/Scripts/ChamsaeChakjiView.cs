// 참새 착지 — 비 젖은 지붕 디오라마, 직교 측정 레일, 참새 착지·서리 유리 피드백.
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.ChamsaeChakji
{
    public partial class ChamsaeChakjiGame
    {
        static readonly Color Roof = MgfLook.Hex("#A94F45");
        static readonly Color RoofDark = MgfLook.Hex("#743B38");
        static readonly Color Iron = MgfLook.Hex("#272B2C");
        static readonly Color Frost = MgfLook.Hex("#D9ECE9");
        static readonly Color Ivory = MgfLook.Hex("#F3E5CF");
        static readonly Color Sage = MgfLook.Hex("#6E8578");
        static readonly Color Chestnut = MgfLook.Hex("#77513C");
        static readonly Color Amber = MgfLook.Hex("#E9B85B");
        static readonly Color Error = MgfLook.Hex("#B9423A");

        Camera cam;
        Transform worldRoot, roofRoot, gutterRoot;
        readonly Transform[] birds = new Transform[4];
        readonly Transform[] birdBodies = new Transform[4];
        readonly Transform[] birdWingsL = new Transform[4];
        readonly Transform[] birdWingsR = new Transform[4];
        readonly Vector3[] birdBase = new Vector3[4];
        readonly Vector3[] birdFrom = new Vector3[4];
        readonly Vector3[] birdTo = new Vector3[4];
        readonly List<Transform> ribbons = new List<Transform>();
        readonly List<Transform> drops3d = new List<Transform>();
        Material roofMat, roofDarkMat, ironMat, frostMat, ivoryMat, sageMat, chestnutMat, amberMat, glassMat, rainMat;

        Canvas canvas;
        RectTransform canvasRt, titleGroup, playGroup, revealGroup, endGroup, toastGroup;
        Image titleGutter, titleChain, statusBg, goalBg, promptBg, definitionBg, targetMeanLine, targetMeanTag;
        TextMeshProUGUI titleLogo, titleTagline, titleHint, titleMeta, titleBest;
        TextMeshProUGUI statusText, goalText, promptText, definitionText, meanText, scoreText;
        TextMeshProUGUI revealTitle, revealBody, toastText, endTitle, endStats, endRestart;
        readonly Image[] rails = new Image[4];
        readonly Image[,] tickMarks = new Image[4, 9];
        readonly TextMeshProUGUI[] tickLabels = new TextMeshProUGUI[9];
        readonly Image[] handles = new Image[4];
        readonly Image[] handlePins = new Image[4];
        readonly TextMeshProUGUI[] handleValues = new TextMeshProUGUI[4];
        readonly TextMeshProUGUI[] rowLabels = new TextMeshProUGUI[4];
        readonly Image[] windowCells = new Image[6];
        readonly TextMeshProUGUI[] windowNumbers = new TextMeshProUGUI[6];
        readonly Image[] hookIcons = new Image[3];
        readonly Image[] guideDots = new Image[6];
        Image guideRing, guideHand, guideArrow, ripple;
        readonly Image[] glassSquares = new Image[4];
        readonly Image[] uiRain = new Image[12];
        Sprite roundedSprite, ringSprite, dotSprite;

        int lastW = -1, lastH = -1;
        bool wide;
        float railLeft = .09f, railRight = .91f;
        readonly float[] rowY = new float[4];
        int shownScore = -1;
        float animatedScore;
        float worldClock;
        float cameraBlend;
        float guideClock;
        float guideBoost;
        float rippleClock;
        float toastClock;
        float landingClock = 99f;
        float glassClock = 99f;
        float hitStop;
        float wrongKick;
        float titlePress;
        int landingNote;
        bool landingCorrect;
        bool showingProblem;
        bool glassLocked;
        int glassTargetCell;
        Vector2 rippleAnchor;

        RectTransform R(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f);
            return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent); rt.anchorMin = rt.anchorMax = anchor; rt.sizeDelta = size;
            var im = rt.gameObject.AddComponent<Image>(); im.color = color; im.sprite = sprite; im.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 size, float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent); rt.anchorMin = rt.anchorMax = anchor; rt.sizeDelta = size;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = fontSize; t.fontStyle = FontStyles.Bold;
            t.color = color; t.alignment = align; t.textWrappingMode = TextWrappingModes.Normal; t.raycastTarget = false; t.text = text;
            return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "RainRing" : "RainRound", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, alpha;
                if (ring)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(1f - Mathf.Abs(d - size * .40f) / (size * .055f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * .5f) - (size * .5f - radius), 0);
                    float qy = Mathf.Max(Mathf.Abs(fy - size * .5f) - (size * .5f - radius), 0);
                    alpha = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            float border = ring ? 0 : radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#D8E4DF"), MgfLook.Hex("#A8B7AF"), MgfLook.Hex("#4E5855"), .68f);
            MgfLook.Sun(new Vector3(42, -34, 16), MgfLook.Hex("#FFE3B8"), 1.28f, .72f);
            cam = MgfLook.Camera(new Vector3(0, 5.2f, -18.5f), new Vector3(0, 1.1f, 2.5f), 34f);
            roofMat = MgfLook.Lit(Roof, .24f, .02f);
            roofDarkMat = MgfLook.Lit(RoofDark, .18f, .02f);
            ironMat = MgfLook.Lit(Iron, .46f, .72f);
            frostMat = MgfLook.Lit(Frost, .78f, .04f, Frost * .05f);
            ivoryMat = MgfLook.Lit(Ivory, .34f, .01f);
            sageMat = MgfLook.Lit(Sage, .28f, .02f);
            chestnutMat = MgfLook.Lit(Chestnut, .30f, .01f);
            amberMat = MgfLook.Lit(Amber, .42f, .05f, Amber * .04f);
            glassMat = MgfLook.Alpha(new Color(Frost.r, Frost.g, Frost.b, .28f));
            rainMat = MgfLook.Additive(new Color(.72f, .92f, .94f, .68f), MgfLook.SoftDot);

            worldRoot = new GameObject("RainRoofCorridor").transform;
            roofRoot = new GameObject("VermilionRidges").transform; roofRoot.SetParent(worldRoot, false);
            for (int row = 0; row < 6; row++)
            for (int col = 0; col < 16; col++)
            {
                float x = -12.2f + col * 1.62f + (row % 2) * .80f;
                float z = 1.0f + row * 1.52f;
                float y = -.35f + row * .10f;
                var tile = MgfLook.Block("WetTile_" + row + "_" + col, new Vector3(x, y, z), new Vector3(1.48f, .32f, 1.76f), .20f,
                    (row + col) % 4 == 0 ? roofDarkMat : roofMat, roofRoot).transform;
                tile.localRotation = Quaternion.Euler(-7f, 0, (col % 2 == 0 ? -1 : 1) * 1.8f);
            }
            for (int i = 0; i < 11; i++)
            {
                float x = -10f + i * 2f;
                MgfLook.Prim(PrimitiveType.Cylinder, "RidgeCap" + i, new Vector3(x, .52f, 5.6f), new Vector3(.54f, 1.0f, .54f), roofDarkMat, roofRoot, false).transform.rotation = Quaternion.Euler(0, 0, 90);
            }
            gutterRoot = new GameObject("DiagonalRainGutter").transform; gutterRoot.SetParent(worldRoot, false);
            gutterRoot.localPosition = new Vector3(0, 1.2f, 1.1f); gutterRoot.localRotation = Quaternion.Euler(0, 0, -6f);
            MgfLook.Block("LongBlackGutter", Vector3.zero, new Vector3(24f, .42f, .76f), .18f, ironMat, gutterRoot);
            for (int i = 0; i < 15; i++)
            {
                var rivet = MgfLook.Prim(PrimitiveType.Sphere, "IvoryRivet" + i, new Vector3(-10.5f + i * 1.5f, .28f, -.42f), new Vector3(.11f, .11f, .11f), ivoryMat, gutterRoot, false).transform;
                drops3d.Add(rivet);
            }
            for (int i = 0; i < 7; i++)
            {
                var ribbon = MgfLook.Block("WindRibbon" + i, new Vector3(-9f + i * 3f, 2.0f + (i % 2) * .35f, 4.6f + (i % 3) * .55f), new Vector3(.10f, 1.45f, .42f), .05f, i % 2 == 0 ? sageMat : ivoryMat, worldRoot).transform;
                ribbon.localRotation = Quaternion.Euler(5, 0, -18 + i * 5); ribbons.Add(ribbon);
            }
            MgfLook.Block("SmallChimney", new Vector3(8.4f, 2.5f, 7.1f), new Vector3(2.0f, 3.8f, 1.8f), .34f, roofDarkMat, worldRoot);
            MgfLook.Block("ChimneyCap", new Vector3(8.4f, 4.4f, 7.1f), new Vector3(2.4f, .34f, 2.2f), .16f, ironMat, worldRoot);
            for (int i = 0; i < birds.Length; i++)
            {
                birdBase[i] = new Vector3(-5.7f + i * 3.8f, 2.25f + (i % 2) * .20f, 1.15f + (i % 2) * .25f);
                birds[i] = BuildSparrow(i, birdBase[i]);
            }
        }

        Transform BuildSparrow(int index, Vector3 pos)
        {
            var root = new GameObject("RoundSparrow" + index).transform; root.SetParent(worldRoot, false); root.localPosition = pos; root.localScale = Vector3.one * 1.18f;
            var body = MgfLook.Prim(PrimitiveType.Sphere, "ChestnutBody", new Vector3(0, .34f, 0), new Vector3(.68f, .76f, .62f), chestnutMat, root, false).transform;
            birdBodies[index] = body;
            MgfLook.Prim(PrimitiveType.Sphere, "IvoryChest", new Vector3(0, .27f, -.48f), new Vector3(.48f, .54f, .18f), ivoryMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(-.02f, .90f, -.02f), new Vector3(.47f, .45f, .45f), chestnutMat, root, false);
            var beak = MgfLook.Prim(PrimitiveType.Cube, "Beak", new Vector3(-.02f, .91f, -.48f), new Vector3(.16f, .10f, .24f), amberMat, root, false).transform;
            beak.localRotation = Quaternion.Euler(45, 0, 0);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeL", new Vector3(-.19f, 1.01f, -.34f), new Vector3(.08f, .08f, .06f), ironMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeR", new Vector3(.19f, 1.01f, -.34f), new Vector3(.08f, .08f, .06f), ironMat, root, false);
            birdWingsL[index] = MgfLook.Block("WingL", new Vector3(-.52f, .42f, .02f), new Vector3(.18f, .58f, .48f), .09f, roofDarkMat, root).transform;
            birdWingsR[index] = MgfLook.Block("WingR", new Vector3(.52f, .42f, .02f), new Vector3(.18f, .58f, .48f), .09f, roofDarkMat, root).transform;
            for (int t = -1; t <= 1; t++) MgfLook.Block("Tail" + t, new Vector3(t * .16f, .28f, .55f), new Vector3(.18f, .16f, .55f), .06f, roofDarkMat, root).transform.localRotation = Quaternion.Euler(-18, 0, t * 9);
            for (int f = -1; f <= 1; f += 2)
            {
                MgfLook.Prim(PrimitiveType.Capsule, "Foot" + f, new Vector3(f * .20f, -.25f, -.02f), new Vector3(.05f, .24f, .05f), amberMat, root, false);
                MgfLook.Block("Toe" + f, new Vector3(f * .20f, -.36f, -.12f), new Vector3(.26f, .04f, .08f), .02f, amberMat, root);
            }
            return root;
        }

        void BuildUi()
        {
            roundedSprite = MakeSprite(64, 14, false); ringSprite = MakeSprite(64, 20, true); dotSprite = MakeSprite(32, 12, false);
            canvas = MgfText.Canvas; canvasRt = (RectTransform)canvas.transform;
            titleGroup = R("TitleRoofGutter", canvas.transform); Stretch(titleGroup);
            playGroup = R("MeasurementDeck", canvas.transform); Stretch(playGroup);
            revealGroup = R("FrostEvidence", canvas.transform); Stretch(revealGroup);
            endGroup = R("EndRoof", canvas.transform); Stretch(endGroup);
            toastGroup = R("TapResponse", canvas.transform); Stretch(toastGroup);

            titleGutter = Img(titleGroup, "DiagonalTitleGutter", new Vector2(.5f, .44f), new Vector2(560, 104), new Color(Iron.r, Iron.g, Iron.b, .94f), roundedSprite);
            titleGutter.rectTransform.localRotation = Quaternion.Euler(0, 0, -7f);
            titleLogo = Txt(titleGroup, "참새 착지", new Vector2(.5f, .45f), new Vector2(520, 104), 58, Ivory);
            titleLogo.rectTransform.localRotation = Quaternion.Euler(0, 0, -7f);
            titleTagline = Txt(titleGroup, "횃대를 옮겨 착지시켜라", new Vector2(.5f, .32f), new Vector2(360, 56), 23, Iron);
            titleHint = Txt(titleGroup, "참새나 빗물 홈을 눌러 깨우기", new Vector2(.5f, .17f), new Vector2(350, 52), 19, Ivory);
            titleChain = Img(titleGroup, "RainChain", new Vector2(.5f, .23f), new Vector2(76, 76), new Color(Frost.r, Frost.g, Frost.b, .84f), ringSprite);
            titleMeta = Txt(titleGroup, "중3 · 산포도 · 여섯 번의 착지", new Vector2(.5f, .08f), new Vector2(350, 38), 15, new Color(Ivory.r, Ivory.g, Ivory.b, .90f));
            titleBest = Txt(titleGroup, "최고 첫 시도 0/6", new Vector2(.5f, .04f), new Vector2(300, 30), 13, Frost);

            statusBg = Img(playGroup, "IronEaveHud", new Vector2(.5f, .965f), new Vector2(370, 48), new Color(Iron.r, Iron.g, Iron.b, .93f), roundedSprite);
            statusText = Txt(playGroup, "", new Vector2(.5f, .965f), new Vector2(350, 45), 15, Ivory);
            scoreText = Txt(playGroup, "0", new Vector2(.92f, .965f), new Vector2(72, 42), 18, Amber);
            goalBg = Img(playGroup, "GoalGutter", new Vector2(.5f, .90f), new Vector2(370, 46), new Color(RoofDark.r, RoofDark.g, RoofDark.b, .95f), roundedSprite);
            goalText = Txt(playGroup, "횃대를 조건에 맞추면 참새가 착지한다", new Vector2(.5f, .90f), new Vector2(350, 44), 17, Ivory);
            promptBg = Img(playGroup, "TargetPlaque", new Vector2(.5f, .81f), new Vector2(366, 90), new Color(Frost.r, Frost.g, Frost.b, .94f), roundedSprite);
            promptText = Txt(playGroup, "", new Vector2(.5f, .81f), new Vector2(346, 84), 17, Iron);
            definitionBg = Img(playGroup, "DefinitionEtching", new Vector2(.5f, .18f), new Vector2(360, 46), new Color(Iron.r, Iron.g, Iron.b, .90f), roundedSprite);
            definitionText = Txt(playGroup, "편차 = 변량 − 평균", new Vector2(.5f, .18f), new Vector2(340, 44), 16, Ivory);

            for (int row = 0; row < 4; row++)
            {
                rails[row] = Img(playGroup, "IronRail" + row, new Vector2(.5f, .5f), new Vector2(320, 8), new Color(Iron.r, Iron.g, Iron.b, .95f), roundedSprite);
                rowLabels[row] = Txt(playGroup, "", new Vector2(.04f, .5f), new Vector2(76, 32), 13, Iron, TextAlignmentOptions.Left);
                for (int tick = 0; tick < 9; tick++)
                    tickMarks[row, tick] = Img(playGroup, "Tick" + row + "_" + tick, new Vector2(.5f, .5f), new Vector2(4, 18), row == 0 ? Iron : new Color(Iron.r, Iron.g, Iron.b, .56f), roundedSprite);
                handles[row] = Img(playGroup, "PerchHandle" + row, new Vector2(.5f, .5f), new Vector2(54, 54), Ivory, dotSprite);
                handlePins[row] = Img(handles[row].transform, "IronPin", new Vector2(.5f, .5f), new Vector2(17, 17), Iron, dotSprite);
                handleValues[row] = Txt(handles[row].transform, "0", new Vector2(.5f, 1.21f), new Vector2(64, 34), 19, Iron);
            }
            for (int tick = 0; tick < 9; tick++) tickLabels[tick] = Txt(playGroup, tick.ToString(), new Vector2(.5f, .5f), new Vector2(36, 28), 14, Iron);
            targetMeanLine = Img(playGroup, "TargetMeanLine", new Vector2(.5f, .5f), new Vector2(3, 330), new Color(Amber.r, Amber.g, Amber.b, .85f), roundedSprite);
            targetMeanTag = Img(playGroup, "TargetMeanTag", new Vector2(.5f, .5f), new Vector2(88, 28), new Color(Amber.r, Amber.g, Amber.b, .94f), roundedSprite);
            meanText = Txt(targetMeanTag.transform, "목표 평균", new Vector2(.5f, .5f), new Vector2(82, 26), 12, Iron);

            for (int i = 0; i < windowCells.Length; i++)
            {
                windowCells[i] = Img(playGroup, "FrostWindow" + i, new Vector2(.12f + i * .13f, .10f), new Vector2(42, 42), new Color(Frost.r, Frost.g, Frost.b, .20f), ringSprite);
                windowNumbers[i] = Txt(windowCells[i].transform, (i + 1).ToString(), new Vector2(.5f, .5f), new Vector2(34, 30), 13, Ivory);
            }
            for (int i = 0; i < hookIcons.Length; i++) hookIcons[i] = Img(playGroup, "RainHook" + i, new Vector2(.06f + i * .045f, .965f), new Vector2(20, 29), Amber, ringSprite);

            guideRing = Img(playGroup, "GuidePulse", new Vector2(.5f, .5f), new Vector2(76, 76), new Color(Amber.r, Amber.g, Amber.b, .88f), ringSprite);
            guideHand = Img(playGroup, "GuideHand", new Vector2(.5f, .5f), new Vector2(34, 44), Ivory, roundedSprite);
            guideArrow = Img(playGroup, "GuideArrow", new Vector2(.5f, .5f), new Vector2(30, 20), Amber, dotSprite);
            for (int i = 0; i < guideDots.Length; i++) guideDots[i] = Img(playGroup, "GuideChevron" + i, new Vector2(.5f, .5f), new Vector2(10, 10), Amber, dotSprite);
            for (int i = 0; i < glassSquares.Length; i++) glassSquares[i] = Img(revealGroup, "DeviationGlass" + i, new Vector2(.5f, .5f), new Vector2(34, 34), new Color(Frost.r, Frost.g, Frost.b, .72f), ringSprite);

            var revealPanel = Img(revealGroup, "EvidenceGlass", new Vector2(.5f, .12f), new Vector2(366, 128), new Color(Frost.r, Frost.g, Frost.b, .94f), roundedSprite);
            revealTitle = Txt(revealPanel.transform, "", new Vector2(.5f, .70f), new Vector2(340, 40), 20, Iron);
            revealBody = Txt(revealPanel.transform, "", new Vector2(.5f, .32f), new Vector2(340, 72), 14, Iron);

            var endShade = Img(endGroup, "EndFrost", new Vector2(.5f, .5f), new Vector2(380, 500), new Color(Iron.r, Iron.g, Iron.b, .92f), roundedSprite);
            endTitle = Txt(endShade.transform, "", new Vector2(.5f, .72f), new Vector2(340, 100), 31, Ivory);
            endStats = Txt(endShade.transform, "", new Vector2(.5f, .46f), new Vector2(330, 160), 19, Frost);
            endRestart = Txt(endShade.transform, "지붕을 눌러 다시 시작", new Vector2(.5f, .19f), new Vector2(320, 60), 18, Amber);

            var toastBg = Img(toastGroup, "Refusal", new Vector2(.5f, .25f), new Vector2(344, 54), new Color(Iron.r, Iron.g, Iron.b, .94f), roundedSprite);
            toastText = Txt(toastBg.transform, "", new Vector2(.5f, .5f), new Vector2(326, 48), 15, Ivory);
            ripple = Img(toastGroup, "SurfaceRipple", new Vector2(.5f, .5f), new Vector2(66, 66), new Color(Frost.r, Frost.g, Frost.b, .72f), ringSprite);
            for (int i = 0; i < uiRain.Length; i++) uiRain[i] = Img(titleGroup, "TitleRaindrop" + i, new Vector2(.05f + i * .08f, .72f), new Vector2(5, 18), new Color(Frost.r, Frost.g, Frost.b, .66f), roundedSprite);

            SetActive(playGroup, false); SetActive(revealGroup, false); SetActive(endGroup, false); SetActive(toastGroup, false);
            Relayout(true);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void SetActive(RectTransform rt, bool on) { if (rt) rt.gameObject.SetActive(on); }

        void ResetVisuals()
        {
            cameraBlend = 0; showingProblem = false; landingClock = glassClock = 99f; hitStop = 0; wrongKick = 0; guideBoost = 0;
            cam.orthographic = false; cam.transform.position = new Vector3(0, 5.2f, -18.5f); cam.transform.LookAt(new Vector3(0, 1.1f, 2.5f)); cam.fieldOfView = 34f;
            for (int i = 0; i < birds.Length; i++) { birds[i].localPosition = birdBase[i]; birds[i].localScale = Vector3.one * 1.18f; }
            SetActive(revealGroup, false); SetActive(endGroup, false); SetActive(toastGroup, false);
        }

        void ShowTitleVisual()
        {
            SetActive(titleGroup, true); SetActive(playGroup, false); SetActive(revealGroup, false); SetActive(endGroup, false);
            for (int i = 0; i < birds.Length; i++) birds[i].gameObject.SetActive(true);
            int best = PlayerPrefs.GetInt("chamsae-chakji-best", 0); titleBest.text = "최고 첫 시도 " + best + "/6";
        }

        void BeginOpeningVisual()
        {
            titlePress = 1f; cameraBlend = .01f; MgfFx.Punch(titleChain.transform, .28f, .34f);
            for (int i = 0; i < birds.Length; i++) { birdFrom[i] = birds[i].localPosition; birdTo[i] = birdBase[i] + new Vector3(0, .9f + i * .12f, .7f); }
            landingClock = 0; landingCorrect = true; landingNote = 0;
        }

        void ShowPlayVisual()
        {
            SetActive(titleGroup, false); SetActive(playGroup, true); SetActive(endGroup, false); showingProblem = true;
            cam.orthographic = true; cam.orthographicSize = wide ? 7.6f : 10.4f; cam.transform.position = new Vector3(0, 5.0f, -18.5f); cam.transform.LookAt(new Vector3(0, 1.0f, 3.6f));
            RefreshHud(); Relayout(true);
        }

        void LoadProblemVisual(LandingProblem p, bool practice)
        {
            promptText.text = p.prompt;
            definitionText.text = p.band == 1 ? "편차 = 변량 − 평균" : p.band == 2 ? "분산 = 편차²의 합 ÷ 4" : "표준편차 = √분산";
            goalText.text = practice ? "손잡이를 4에 놓아 편차 +1을 만드시오" : "횃대를 조건에 맞추면 참새가 착지한다";
            for (int i = 0; i < 4; i++) { glassSquares[i].gameObject.SetActive(false); }
            SetActive(revealGroup, false); guideBoost = practice ? 1f : 0f; showingProblem = true; glassLocked = false;
            // 측정 중 새 몸이 숫자 눈금과 겹치면 데이터 표식처럼 읽힌다. 제출 순간에만 네 마리가 날아든다.
            for (int i = 0; i < birds.Length; i++) birds[i].gameObject.SetActive(false);
            RefreshHud(); Relayout(true);
        }

        int[] DisplayValues()
        {
            if (current == null) return new[] { 0, 0, 0, 0 };
            if (current.band == 1) return new[] { current.fixedValues[0], current.fixedValues[1], current.fixedValues[2], placedA };
            return new[] { current.fixedValues[0], current.fixedValues[1], placedA, placedB };
        }

        void Relayout(bool force)
        {
            bool nextWide = MgfBridge.IsLandLayout;
            if (!force && Screen.width == lastW && Screen.height == lastH && nextWide == wide) return;
            lastW = Screen.width; lastH = Screen.height; wide = nextWide;
            railLeft = wide ? .30f : .10f; railRight = wide ? .84f : .90f;
            if (wide) { rowY[0] = .64f; rowY[1] = .51f; rowY[2] = .38f; rowY[3] = .25f; }
            else { rowY[0] = .64f; rowY[1] = .54f; rowY[2] = .44f; rowY[3] = .34f; }
            float railLogicalWidth = wide ? 650f : 316f;
            Vector2 promptAnchor = wide ? new Vector2(.145f, .56f) : new Vector2(.5f, .81f);
            promptBg.rectTransform.anchorMin = promptBg.rectTransform.anchorMax = promptAnchor;
            promptBg.rectTransform.sizeDelta = wide ? new Vector2(230, 178) : new Vector2(366, 90);
            promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = promptAnchor;
            promptText.rectTransform.sizeDelta = wide ? new Vector2(208, 164) : new Vector2(346, 84);
            promptText.fontSize = wide ? 15 : 16;
            goalBg.rectTransform.sizeDelta = wide ? new Vector2(620, 46) : new Vector2(370, 46);
            statusBg.rectTransform.sizeDelta = wide ? new Vector2(760, 48) : new Vector2(370, 48);
            definitionBg.rectTransform.anchorMin = definitionBg.rectTransform.anchorMax = wide ? new Vector2(.53f, .12f) : new Vector2(.5f, .22f);
            definitionText.rectTransform.anchorMin = definitionText.rectTransform.anchorMax = definitionBg.rectTransform.anchorMin;
            definitionBg.rectTransform.sizeDelta = wide ? new Vector2(560, 46) : new Vector2(360, 46);
            definitionText.rectTransform.sizeDelta = wide ? new Vector2(540, 44) : new Vector2(340, 44);
            for (int row = 0; row < 4; row++)
            {
                rails[row].rectTransform.anchorMin = rails[row].rectTransform.anchorMax = new Vector2((railLeft + railRight) * .5f, rowY[row]);
                rails[row].rectTransform.sizeDelta = new Vector2(railLogicalWidth, 8);
                rowLabels[row].rectTransform.anchorMin = rowLabels[row].rectTransform.anchorMax = new Vector2(wide ? .265f : .025f, rowY[row]);
                rowLabels[row].rectTransform.sizeDelta = new Vector2(wide ? 90 : 72, 32);
                for (int tick = 0; tick < 9; tick++)
                {
                    float u = Mathf.Lerp(railLeft, railRight, tick / 8f);
                    tickMarks[row, tick].rectTransform.anchorMin = tickMarks[row, tick].rectTransform.anchorMax = new Vector2(u, rowY[row]);
                }
            }
            for (int tick = 0; tick < 9; tick++)
            {
                float u = Mathf.Lerp(railLeft, railRight, tick / 8f);
                tickLabels[tick].rectTransform.anchorMin = tickLabels[tick].rectTransform.anchorMax = new Vector2(u, rowY[0] + (wide ? .052f : .038f));
            }
            if (current != null)
            {
                float meanU = Mathf.Lerp(railLeft, railRight, Mathf.Clamp01((current.mean - current.shift) / 8f));
                targetMeanLine.rectTransform.anchorMin = targetMeanLine.rectTransform.anchorMax = new Vector2(meanU, (rowY[0] + rowY[3]) * .5f);
                targetMeanLine.rectTransform.sizeDelta = new Vector2(3, wide ? 330 : 286);
                targetMeanTag.rectTransform.anchorMin = targetMeanTag.rectTransform.anchorMax = new Vector2(meanU, rowY[0] + (wide ? .10f : .075f));
                int[] values = DisplayValues();
                for (int row = 0; row < 4; row++)
                {
                    float u = Mathf.Lerp(railLeft, railRight, Mathf.Clamp01((values[row] - current.shift) / 8f));
                    handles[row].rectTransform.anchorMin = handles[row].rectTransform.anchorMax = new Vector2(u, rowY[row]);
                    bool movable = current.band == 1 ? row == 3 : row >= 2;
                    handles[row].color = movable ? Ivory : new Color(Iron.r, Iron.g, Iron.b, .92f);
                    handlePins[row].color = movable ? Iron : Amber;
                    handleValues[row].color = movable ? Iron : Ivory;
                    handleValues[row].text = values[row].ToString();
                    rowLabels[row].text = movable ? (current.band == 1 ? "이동" : "이동 " + (row - 1)) : "고정 " + (row + 1);
                }
                for (int tick = 0; tick < 9; tick++) tickLabels[tick].text = (current.shift + tick).ToString();
            }
            for (int i = 0; i < windowCells.Length; i++)
            {
                Vector2 a;
                if (wide) a = new Vector2(.88f, .70f - i * .105f);
                else a = new Vector2(.16f + i * .135f, .105f);
                windowCells[i].rectTransform.anchorMin = windowCells[i].rectTransform.anchorMax = a;
            }
            cam.orthographicSize = wide ? 7.5f : 10.6f;
        }

        public int HitMovableHandle(Vector2 screen)
        {
            if (current == null) return -1;
            int first = current.band == 1 ? 3 : 2;
            float radius = Mathf.Max(48f, Mathf.Min(Screen.width, Screen.height) * .075f);
            for (int row = first; row < 4; row++)
            {
                Vector2 c = RectTransformUtility.WorldToScreenPoint(null, handles[row].rectTransform.position);
                if (Vector2.Distance(screen, c) <= radius) return current.band == 1 ? 0 : row - 2;
            }
            return -1;
        }

        public int ValueFromScreen(Vector2 screen, int shift)
        {
            float t = Mathf.InverseLerp(Screen.width * railLeft, Screen.width * railRight, screen.x);
            return shift + Mathf.Clamp(Mathf.RoundToInt(t * 8f), 0, 8);
        }

        int HandleRow(int handle) => current != null && current.band == 1 ? 3 : 2 + handle;

        void BeginHandleDrag(int handle)
        {
            int row = HandleRow(handle); handles[row].rectTransform.localScale = new Vector3(1.18f, .86f, 1);
            guideBoost = 0; MgfFx.Punch(handles[row].transform, .14f, .20f);
        }

        void UpdateHandleDrag(int handle, int value)
        {
            int row = HandleRow(handle);
            float u = Mathf.Lerp(railLeft, railRight, Mathf.Clamp01((value - current.shift) / 8f));
            handles[row].rectTransform.anchorMin = handles[row].rectTransform.anchorMax = new Vector2(u, rowY[row]);
            handleValues[row].text = value.ToString();
            float stretch = 1f + Mathf.Abs(Mathf.Sin(worldClock * 11f)) * .08f;
            handles[row].rectTransform.localScale = new Vector3(.94f, 1.12f * stretch, 1);
        }

        void EndHandleDrag(int handle)
        {
            int row = HandleRow(handle); handles[row].rectTransform.localScale = Vector3.one;
        }

        void PinHandle(int handle)
        {
            int row = HandleRow(handle); handlePins[row].rectTransform.localScale = Vector3.one * 1.35f;
            MgfFx.Punch(handlePins[row].transform, .28f, .22f); MgfSfx.Play("pop", .26f);
        }

        void ShowSecondHandleCue()
        {
            int row = 3; guideBoost = .65f; guideRing.rectTransform.anchorMin = guideRing.rectTransform.anchorMax = handles[row].rectTransform.anchorMin;
            toastText.text = "둘째 횃대를 놓으면 네 위치를 제출합니다."; SetActive(toastGroup, true); toastClock = 2.2f;
        }

        void BoostGuide(bool strong)
        {
            guideBoost = strong ? 1f : .72f; guideClock = 0;
        }

        void RefuseVisual(string message, Vector2 screen, bool pointToHandle)
        {
            toastText.text = message; SetActive(toastGroup, true); toastClock = 2.25f;
            rippleAnchor = new Vector2(Mathf.Clamp01(screen.x / Mathf.Max(1, Screen.width)), Mathf.Clamp01(screen.y / Mathf.Max(1, Screen.height)));
            ripple.rectTransform.anchorMin = ripple.rectTransform.anchorMax = rippleAnchor; rippleClock = .48f;
            ripple.color = new Color(Frost.r, Frost.g, Frost.b, .88f); ripple.rectTransform.localScale = Vector3.one * .35f;
            if (pointToHandle) guideBoost = 1f;
            MgfFx.Punch(toastGroup.transform, .08f, .22f); MgfSfx.Play("wrong", .10f);
        }

        void BeginRevealVisual(bool correct, bool practice, LandingProblem p, int a, int b, bool first)
        {
            landingCorrect = correct; landingClock = 0; glassClock = 0; hitStop = correct ? .085f : 0; wrongKick = correct ? 0 : 1f; landingNote = 0;
            SetActive(revealGroup, true); SetActive(toastGroup, false); toastClock = 0; guideBoost = 0;
            int[] values = ChamsaeRules.Values(p, a, b);
            for (int i = 0; i < 4; i++)
            {
                birds[i].gameObject.SetActive(true);
                birdFrom[i] = new Vector3(-8.7f + i * .68f, 2.0f + (i % 2) * .18f, .8f);
                float local = values[i] - p.shift;
                birdTo[i] = new Vector3(-6.6f + local * 1.65f, 2.0f + (i % 2) * .16f, 1.10f + i * .18f);
                if (!correct && i == 3) birdTo[i] = new Vector3(-8.4f, 2.7f, 1.2f);
                birds[i].localPosition = birdFrom[i];
            }
            revealTitle.text = correct ? (practice ? "편차 +1, 착지 완료" : first ? "첫 착지에 잠겼습니다" : "수리한 착지가 맞았습니다") : "이 배치의 근거를 확인하시오";
            revealTitle.color = correct ? Sage : Error;
            revealBody.text = ChamsaeRules.Evidence(p, a, b) + (correct ? "" : "\n" + MisconceptionText(st.misconceptionId));
            int[] shown = DisplayValues();
            for (int i = 0; i < 4; i++)
            {
                glassSquares[i].gameObject.SetActive(true);
                float u = Mathf.Lerp(railLeft, railRight, Mathf.Clamp01((shown[i] - p.shift) / 8f));
                glassSquares[i].rectTransform.anchorMin = glassSquares[i].rectTransform.anchorMax = new Vector2(u, rowY[i]);
                int dev = Mathf.Abs(shown[i] - p.mean); float sz = 28 + dev * 9;
                glassSquares[i].rectTransform.sizeDelta = new Vector2(sz, sz);
                glassSquares[i].color = correct ? new Color(Frost.r, Frost.g, Frost.b, .84f) : new Color(Error.r, Error.g, Error.b, .48f);
            }
            glassTargetCell = Mathf.Clamp(st.solved - 1, 0, 5); glassLocked = false;
            if (correct) MgfSfx.Play("correct", .48f); else { MgfSfx.Play("wrong", .42f); MgfFx.Shake(cam, .065f, .22f); }
            MgfFx.Punch(revealTitle.transform, .16f, .30f); RefreshHud();
        }

        string MisconceptionText(string id)
        {
            switch (id)
            {
                case "deviation_reversed": return "편차는 평균−변량이 아니라 변량−평균입니다.";
                case "variance_zero_from_deviation_sum": return "편차의 합 0과 분산 0은 다릅니다. 편차를 제곱하시오.";
                case "standard_deviation_used_as_variance": return "표준편차를 제곱해 분산 조건으로 바꾸시오.";
                case "mean_center_symmetry": return "평균 양옆 대칭만으로는 고정 자료의 산포도를 맞출 수 없습니다.";
                case "dispersion_too_wide": return "평균 조건과 함께 횃대 사이를 더 좁혀 보시오.";
                case "dispersion_too_narrow": return "평균 조건을 유지하며 횃대 사이를 더 넓혀 보시오.";
                default: return "선택한 위치의 평균과 편차 제곱합을 함께 확인하시오.";
            }
        }

        void FinishRevealVisual(bool correct)
        {
            if (correct && glassTargetCell >= 0)
            {
                windowCells[glassTargetCell].color = new Color(Frost.r, Frost.g, Frost.b, .88f);
                windowNumbers[glassTargetCell].color = Iron;
                MgfFx.Punch(windowCells[glassTargetCell].transform, .22f, .30f);
            }
            for (int i = 0; i < glassSquares.Length; i++) glassSquares[i].gameObject.SetActive(false);
            for (int i = 0; i < birds.Length; i++) birds[i].gameObject.SetActive(false);
            SetActive(revealGroup, false); landingClock = glassClock = 99f;
        }

        void ShowRepairVisual()
        {
            goalText.text = "흐린 유리 흔적을 보고 같은 자료를 수리하시오";
            SetActive(revealGroup, false); guideBoost = .55f; Relayout(true);
        }

        void ShowEndVisual(bool clear, string reason)
        {
            SetActive(playGroup, false); SetActive(revealGroup, false); SetActive(titleGroup, false); SetActive(toastGroup, false); SetActive(endGroup, true);
            toastClock = 0;
            for (int i = 0; i < birds.Length; i++)
            {
                birds[i].gameObject.SetActive(true);
                birds[i].localPosition = new Vector3(-4.8f + i * 3.2f, 5.1f + (i % 2) * .18f, 1.2f);
                birds[i].localScale = Vector3.one;
            }
            endTitle.text = clear ? "빗물 측정창 완성" : reason == "time" ? "준비 시간이 끝났습니다" : reason == "hooks" ? "철제 걸쇠가 모두 부러졌습니다" : "첫 시도 기준에 닿을 수 없습니다";
            endStats.text = "착지 " + st.solved + "/6\n첫 시도 " + st.firstCorrect + "/6 · 후반 " + st.dispersionFirstCorrect + "/4\n점수 " + st.score;
            endRestart.text = clear ? "참새를 눌러 새 지붕 시작" : "지붕을 눌러 다시 수리";
            if (clear)
            {
                int best = PlayerPrefs.GetInt("chamsae-chakji-best", 0);
                if (st.firstCorrect > best) { PlayerPrefs.SetInt("chamsae-chakji-best", st.firstCorrect); PlayerPrefs.Save(); }
            }
        }

        void RefreshHud()
        {
            if (!playGroup || !playGroup.gameObject.activeSelf) return;
            int seconds = Mathf.CeilToInt(timeLeft);
            statusText.text = "착지 " + st.round + "/6   첫 시도 " + st.firstCorrect + "/" + st.attemptedRounds + "   후반 " + st.dispersionFirstCorrect + "/4   " + seconds + "초";
            if (shownScore < 0) { animatedScore = st.score; shownScore = st.score; }
            for (int i = 0; i < hookIcons.Length; i++)
            {
                bool alive = i < st.lives; hookIcons[i].color = alive ? Amber : new Color(Error.r, Error.g, Error.b, .38f);
                hookIcons[i].rectTransform.localRotation = Quaternion.Euler(0, 0, alive ? 0 : (i % 2 == 0 ? -28 : 28));
            }
            for (int i = 0; i < windowCells.Length; i++)
            {
                bool filled = i < st.solved;
                windowCells[i].color = filled ? new Color(Frost.r, Frost.g, Frost.b, .88f) : new Color(Frost.r, Frost.g, Frost.b, .20f);
                windowNumbers[i].color = filled ? Iron : Ivory;
            }
        }

        void TickView(float dt)
        {
            worldClock += dt;
            titlePress = Mathf.MoveTowards(titlePress, 0, dt * 2.8f);
            for (int i = 0; i < ribbons.Count; i++) ribbons[i].localRotation = Quaternion.Euler(5 + Mathf.Sin(worldClock * .8f + i) * 3f, 0, -18 + i * 5 + Mathf.Sin(worldClock * 1.3f + i) * 8f);
            for (int i = 0; i < drops3d.Count; i++)
            {
                float drift = Mathf.Repeat(worldClock * .42f + i * .083f, 1f);
                drops3d[i].localPosition = new Vector3(-10.5f + drift * 21f, .28f, -.42f);
                drops3d[i].localScale = Vector3.one * (.07f + Mathf.Sin(drift * Mathf.PI) * .06f);
            }
            if (titleGroup.gameObject.activeSelf)
            {
                float pulse = 1f + Mathf.Sin(worldClock * 3.1f) * .045f + titlePress * .12f;
                titleChain.rectTransform.localScale = Vector3.one * pulse;
                titleLogo.rectTransform.localPosition = new Vector3(Mathf.Sin(worldClock * .42f) * 3f, Mathf.Sin(worldClock * .67f) * 2f, 0);
                for (int i = 0; i < uiRain.Length; i++)
                {
                    float y = Mathf.Repeat(.82f - worldClock * (.08f + i * .003f) - i * .071f, .68f) + .18f;
                    uiRain[i].rectTransform.anchorMin = uiRain[i].rectTransform.anchorMax = new Vector2(.04f + i * .083f, y);
                }
            }

            if (landingClock < 10f)
            {
                landingClock += dt;
                for (int i = 0; i < birds.Length; i++)
                {
                    float start = i * .10f; float k = Mathf.Clamp01((landingClock - start) / .62f);
                    float eased = 1f - Mathf.Pow(1f - k, 3f);
                    Vector3 p = Vector3.Lerp(birdFrom[i], birdTo[i], eased); p.y += Mathf.Sin(k * Mathf.PI) * (1.1f + i * .10f);
                    birds[i].localPosition = p;
                    float flap = Mathf.Sin(k * Mathf.PI * 5f) * (1f - k) * 48f;
                    birdWingsL[i].localRotation = Quaternion.Euler(0, 0, flap); birdWingsR[i].localRotation = Quaternion.Euler(0, 0, -flap);
                    float squash = k > .84f ? Mathf.Sin((k - .84f) / .16f * Mathf.PI) * .16f : 0;
                    birds[i].localScale = new Vector3(1.18f * (1 + squash), 1.18f * (1 - squash), 1.18f);
                }
                while (landingNote < 4 && landingClock >= landingNote * .10f + .58f)
                {
                    MgfSfx.Play("tap", .16f + landingNote * .035f); landingNote++;
                }
            }
            else
            {
                for (int i = 0; i < birds.Length; i++)
                {
                    float bob = Mathf.Sin(worldClock * 2.2f + i * 1.4f) * .055f;
                    bool endIdle = flow == Flow.End;
                    Vector3 idleBase = endIdle ? new Vector3(-4.8f + i * 3.2f, 5.1f + (i % 2) * .18f, 1.2f)
                        : showingProblem ? new Vector3(-8.7f + i * .56f, 2.25f + (i % 2) * .11f, 5.2f + i * .08f) : birdBase[i];
                    float idleScale = endIdle ? 1f : showingProblem ? .82f : 1.18f;
                    birds[i].localPosition = idleBase + new Vector3(0, bob, 0);
                    birds[i].localScale = new Vector3(idleScale, idleScale + Mathf.Abs(bob) * .24f, idleScale);
                }
            }

            if (hitStop > 0) hitStop -= dt;
            else if (glassClock < 10f)
            {
                glassClock += dt;
                float k = Mathf.Clamp01((glassClock - .26f) / .76f);
                if (landingCorrect)
                {
                    Vector2 target = windowCells[Mathf.Clamp(glassTargetCell, 0, 5)].rectTransform.anchorMin;
                    int[] values = DisplayValues();
                    for (int i = 0; i < 4; i++)
                    {
                        float startU = Mathf.Lerp(railLeft, railRight, Mathf.Clamp01((values[i] - current.shift) / 8f));
                        Vector2 start = new Vector2(startU, rowY[i]);
                        float localK = Mathf.Clamp01(k * 1.25f - i * .08f);
                        float overshoot = localK < .86f ? localK / .86f : 1f + Mathf.Sin((localK - .86f) / .14f * Mathf.PI) * .10f;
                        Vector2 a = Vector2.LerpUnclamped(start, target, overshoot);
                        glassSquares[i].rectTransform.anchorMin = glassSquares[i].rectTransform.anchorMax = a;
                        glassSquares[i].rectTransform.localRotation = Quaternion.Euler(0, 0, localK * (i % 2 == 0 ? 92f : -92f));
                    }
                    if (k >= 1f && !glassLocked) { glassLocked = true; MgfSfx.Play("pop", .40f); }
                }
            }

            if (guideBoost > 0 && current != null && playGroup.gameObject.activeSelf)
            {
                guideClock += dt; guideBoost = Mathf.Max(.35f, guideBoost - dt * .025f);
                int handle = current.band > 1 && (st.handlePlacedMask & 1) != 0 ? 1 : 0;
                int row = HandleRow(handle);
                float fromU = handles[row].rectTransform.anchorMin.x;
                float toU = IsPractice ? Mathf.Lerp(railLeft, railRight, .5f) : Mathf.Clamp(fromU + (fromU > (railLeft + railRight) * .5f ? -.14f : .14f), railLeft, railRight);
                float flow = (guideClock * .62f) % 1f;
                guideRing.gameObject.SetActive(true); guideHand.gameObject.SetActive(true); guideArrow.gameObject.SetActive(true);
                guideRing.rectTransform.anchorMin = guideRing.rectTransform.anchorMax = new Vector2(fromU, rowY[row]);
                float pulse = .82f + Mathf.Sin(guideClock * 5.4f) * .18f; guideRing.rectTransform.localScale = Vector3.one * pulse * (1f + guideBoost * .16f);
                guideHand.rectTransform.anchorMin = guideHand.rectTransform.anchorMax = new Vector2(Mathf.Lerp(fromU, toU, Mathf.SmoothStep(0, 1, flow)), rowY[row] + .055f);
                guideArrow.rectTransform.anchorMin = guideArrow.rectTransform.anchorMax = new Vector2(toU, rowY[row]);
                for (int i = 0; i < guideDots.Length; i++)
                {
                    float u = Mathf.Repeat(flow + i / (float)guideDots.Length, 1f);
                    guideDots[i].gameObject.SetActive(true);
                    guideDots[i].rectTransform.anchorMin = guideDots[i].rectTransform.anchorMax = new Vector2(Mathf.Lerp(fromU, toU, u), rowY[row]);
                }
            }
            else
            {
                guideRing.gameObject.SetActive(false); guideHand.gameObject.SetActive(false); guideArrow.gameObject.SetActive(false);
                for (int i = 0; i < guideDots.Length; i++) guideDots[i].gameObject.SetActive(false);
            }

            if (toastClock > 0) { toastClock -= dt; if (toastClock <= 0) SetActive(toastGroup, false); }
            if (rippleClock > 0)
            {
                rippleClock -= dt; float k = 1f - rippleClock / .48f;
                ripple.rectTransform.localScale = Vector3.one * Mathf.Lerp(.35f, 1.9f, k); Color c = ripple.color; c.a = 1f - k; ripple.color = c;
            }
            animatedScore = Mathf.MoveTowards(animatedScore, st.score, dt * 420f);
            int scoreNow = Mathf.RoundToInt(animatedScore);
            if (scoreNow != shownScore) { shownScore = scoreNow; scoreText.text = scoreNow.ToString(); if (scoreNow == st.score) MgfFx.Punch(scoreText.transform, .13f, .22f); }
            wrongKick = Mathf.MoveTowards(wrongKick, 0, dt * 3.8f);
            if (wrongKick > 0 && current != null)
            {
                float x = Mathf.Sin(worldClock * 35f) * wrongKick * 5f;
                for (int row = current.band == 1 ? 3 : 2; row < 4; row++) handles[row].rectTransform.anchoredPosition = new Vector2(x, 0);
            }
            else if (current != null) for (int row = current.band == 1 ? 3 : 2; row < 4; row++) handles[row].rectTransform.anchoredPosition = Vector2.zero;
            RefreshHud();
        }

        void SyncStateGeometry(GameState s)
        {
            s.layoutLand = wide;
            s.railLeftPx = Mathf.RoundToInt(Screen.width * railLeft); s.railRightPx = Mathf.RoundToInt(Screen.width * railRight);
            if (s.rowYPx == null || s.rowYPx.Length != 4) s.rowYPx = new int[4];
            for (int i = 0; i < 4; i++) s.rowYPx[i] = Mathf.RoundToInt(Screen.height * rowY[i]);
        }
    }
}
