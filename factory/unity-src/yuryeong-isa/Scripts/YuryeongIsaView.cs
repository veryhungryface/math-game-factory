// 유령 이사 — 테라코타 연립주택 단면, 집유령 주민, 화면 UI와 재질별 피드백.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.YuryeongIsa
{
    public partial class YuryeongIsaGame
    {
        static readonly Color Terracotta = MgfLook.Hex("#D66045");
        static readonly Color Apricot = MgfLook.Hex("#FFCB9B");
        static readonly Color Ivory = MgfLook.Hex("#FFF7DD");
        static readonly Color Charcoal = MgfLook.Hex("#26323F");
        static readonly Color Brick = MgfLook.Hex("#8D392C");
        static readonly Color Copper = MgfLook.Hex("#B96A43");
        static readonly Color Dusk = MgfLook.Hex("#7C5262");
        static readonly Color Success = MgfLook.Hex("#F6C95F");
        static readonly Color Error = MgfLook.Hex("#A53F38");

        Camera cam;
        Transform worldRoot, alleyRoot, signRoot, dragPair;
        readonly Transform[] buildings = new Transform[4];
        readonly Transform[] pairHandles = new Transform[4];
        readonly Transform[,] residents = new Transform[4, 28];
        readonly Transform[,] curtains = new Transform[4, 2];
        readonly Transform[] chimneys = new Transform[4];
        readonly Transform[] smoke = new Transform[12];
        readonly Transform[] recordLights = new Transform[4];
        readonly Transform[] trail = new Transform[8];
        readonly Vector3[] buildingBase = new Vector3[4];
        readonly Vector3[] recordBase = new Vector3[4];
        Material brickMat, brickDarkMat, plasterMat, ivoryMat, charcoalMat, copperMat, windowMat, glowMat, smokeMat, greenMat;

        Canvas canvas;
        RectTransform titleGroup, playGroup, revealGroup, endGroup, toastGroup;
        Image titleSign, titleTag, hudBg, goalBg, mathBoard, revealBoard, endBoard, ripple, guideRing, guideLine, guideArrow;
        TextMeshProUGUI titleLogo, titleTagline, titleMeta, titleBest, titleCta;
        TextMeshProUGUI hudText, scoreText, goalText, promptText, sourceText, definitionText, targetText;
        TextMeshProUGUI revealTitle, revealText, endTitle, endStats, endCta, toastText;
        readonly Image[] housePlaques = new Image[4];
        readonly TextMeshProUGUI[] houseNumbers = new TextMeshProUGUI[4];
        readonly Image[] sealIcons = new Image[3];
        Sprite enamelSprite, ringSprite, dotSprite;

        bool wide;
        int lastW = -1, lastH = -1;
        float worldClock, titlePress, rippleClock, toastClock, guideBoost, animatedScore;
        int shownScore = -1;
        Vector2 ripplePosition;
        bool guideVisible;
        int guideFrom = 3, guideTo = 0;
        bool flightActive;
        Vector3 flightStart, flightEnd;
        float flightClock, flightDuration;
        int flightFrom = -1, flightTo = -1;
        float feedbackVisualClock = 99f;
        bool feedbackVisualCorrect;
        int feedbackFrom = -1, feedbackTo = -1;

        RectTransform R(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f);
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent); rt.anchorMin = rt.anchorMax = anchor; rt.sizeDelta = size;
            var image = rt.gameObject.AddComponent<Image>(); image.color = color; image.sprite = sprite; image.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 size, float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent); rt.anchorMin = rt.anchorMax = anchor; rt.sizeDelta = size;
            var label = rt.gameObject.AddComponent<TextMeshProUGUI>(); label.font = MgfText.Font; label.fontSize = fontSize; label.fontStyle = FontStyles.Bold;
            label.color = color; label.alignment = align; label.textWrappingMode = TextWrappingModes.Normal; label.raycastTarget = false; label.text = text;
            return label;
        }

        static Sprite MakeSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "GhostRing" : "EnamelPlate", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, alpha;
                if (ring)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(1f - Mathf.Abs(d - size * .39f) / (size * .055f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * .5f) - (size * .5f - radius), 0);
                    float qy = Mathf.Max(Mathf.Abs(fy - size * .5f) - (size * .5f - radius), 0);
                    alpha = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            tex.SetPixels32(pixels); tex.Apply(false, true);
            float border = ring ? 0 : radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#B77872"), MgfLook.Hex("#E3A270"), MgfLook.Hex("#5C4450"), .72f);
            MgfLook.Sun(new Vector3(38, -34, 12), MgfLook.Hex("#FFD9A4"), 1.32f, .72f);
            cam = MgfLook.Camera(new Vector3(0, 10.8f, -22f), new Vector3(0, 3.8f, 0), 35f);
            cam.orthographic = true;

            brickMat = MgfLook.Lit(Terracotta, .20f, .01f);
            brickDarkMat = MgfLook.Lit(Brick, .17f, .01f);
            plasterMat = MgfLook.Lit(Apricot, .28f, .01f);
            ivoryMat = MgfLook.Lit(Ivory, .40f, .01f, Ivory * .025f);
            charcoalMat = MgfLook.Lit(Charcoal, .34f, .06f);
            copperMat = MgfLook.Lit(Copper, .60f, .30f, Copper * .02f);
            windowMat = MgfLook.Lit(MgfLook.Hex("#423543"), .42f, .02f, MgfLook.Hex("#392337") * .06f);
            glowMat = MgfLook.Lit(Success, .62f, .06f, Success * .28f);
            smokeMat = MgfLook.Alpha(new Color(.90f, .77f, .70f, .28f), MgfLook.SoftDot);
            greenMat = MgfLook.Lit(MgfLook.Hex("#6F7D52"), .24f, .01f);

            worldRoot = new GameObject("TerracottaGhostRow").transform;
            alleyRoot = new GameObject("WarmBrickAlley").transform; alleyRoot.SetParent(worldRoot, false);
            MgfLook.Block("AlleyGround", new Vector3(0, -.55f, 2.8f), new Vector3(26f, .70f, 10f), .28f, brickDarkMat, alleyRoot);
            for (int i = 0; i < 13; i++)
            {
                var stone = MgfLook.Block("AlleyStone" + i, new Vector3(-11.5f + i * 1.9f, -.15f, -1.2f + (i % 3) * .75f), new Vector3(1.55f, .15f, .9f), .18f, i % 2 == 0 ? plasterMat : brickMat, alleyRoot);
                stone.transform.localRotation = Quaternion.Euler(0, (i % 2 == 0 ? -1 : 1) * 5f, 0);
            }
            for (int i = 0; i < 4; i++) buildings[i] = BuildBuilding(i);

            signRoot = new GameObject("HangingMoveSign").transform; signRoot.SetParent(worldRoot, false);
            MgfLook.Block("SignBeam", new Vector3(0, 9.45f, -1.2f), new Vector3(5.6f, .24f, .38f), .08f, charcoalMat, signRoot);
            MgfLook.Block("SignBoard", new Vector3(0, 8.78f, -1.35f), new Vector3(4.9f, 1.02f, .24f), .15f, ivoryMat, signRoot);
            for (int s = -1; s <= 1; s += 2) MgfLook.Block("SignChain" + s, new Vector3(s * 1.9f, 9.12f, -1.26f), new Vector3(.08f, .62f, .08f), .03f, copperMat, signRoot);

            for (int i = 0; i < 4; i++)
            {
                recordBase[i] = new Vector3(-2.7f + i * 1.8f, .65f, -2.5f);
                recordLights[i] = MgfLook.Prim(PrimitiveType.Sphere, "BalconyRecordLamp" + i, recordBase[i], new Vector3(.32f, .32f, .32f), glowMat, worldRoot, false).transform;
            }

            dragPair = BuildGhostPair("MovingPair", Vector3.zero, worldRoot, 1.18f);
            dragPair.gameObject.SetActive(false);
            for (int i = 0; i < trail.Length; i++)
            {
                trail[i] = MgfLook.Prim(PrimitiveType.Sphere, "GhostTrail" + i, Vector3.zero, Vector3.one * (.26f - i * .018f),
                    MgfLook.Alpha(new Color(Ivory.r, Ivory.g, Ivory.b, .20f - i * .015f), MgfLook.SoftDot), worldRoot, false).transform;
                trail[i].gameObject.SetActive(false);
            }
        }

        Transform BuildBuilding(int index)
        {
            var root = new GameObject("ApartmentHouse" + (index + 1)).transform; root.SetParent(worldRoot, false);
            Material facade = index % 2 == 0 ? brickMat : plasterMat;
            Material trim = index % 2 == 0 ? plasterMat : brickMat;
            MgfLook.Block("Facade", new Vector3(0, 3.75f, .3f), new Vector3(2.7f, 7.5f, 2.25f), .22f, facade, root);
            MgfLook.Block("SideDepth", new Vector3(1.23f, 3.65f, 1.42f), new Vector3(.32f, 7.15f, 1.05f), .10f, brickDarkMat, root);
            MgfLook.Block("WindowRecess", new Vector3(0, 4.15f, -1.02f), new Vector3(2.25f, 5.35f, .25f), .18f, windowMat, root);
            for (int floor = 0; floor < 8; floor++)
            {
                float y = 1.77f + floor * .65f;
                MgfLook.Block("Balcony" + floor, new Vector3(0, y - .27f, -1.35f), new Vector3(2.42f, .12f, .50f), .05f, trim, root);
                for (int col = 0; col < 4; col++)
                {
                    int slot = floor * 4 + col;
                    if (slot >= 28) continue;
                    float x = -.82f + col * .55f;
                    residents[index, slot] = BuildResident("Resident_" + index + "_" + slot, new Vector3(x, y, -1.36f), root, index, slot);
                }
            }
            for (int rail = -1; rail <= 1; rail += 2) MgfLook.Block("SideRail" + rail, new Vector3(rail * 1.02f, 4.1f, -1.46f), new Vector3(.08f, 5.2f, .08f), .03f, copperMat, root);
            curtains[index, 0] = MgfLook.Block("CurtainL", new Vector3(-.82f, 5.8f, -1.51f), new Vector3(.45f, 1.0f, .10f), .08f, trim, root).transform;
            curtains[index, 1] = MgfLook.Block("CurtainR", new Vector3(.82f, 5.8f, -1.51f), new Vector3(.45f, 1.0f, .10f), .08f, trim, root).transform;
            MgfLook.Block("Roof", new Vector3(0, 7.65f, .25f), new Vector3(3.05f, .48f, 2.72f), .16f, brickDarkMat, root);
            chimneys[index] = MgfLook.Block("Chimney", new Vector3(.78f, 8.55f, .55f), new Vector3(.62f, 1.65f, .72f), .14f, brickDarkMat, root).transform;
            MgfLook.Block("ChimneyCap", new Vector3(.78f, 9.40f, .55f), new Vector3(.85f, .20f, .92f), .07f, charcoalMat, root);
            pairHandles[index] = BuildGhostPair("RoofGhostPair" + index, new Vector3(0, 8.16f, -1.2f), root, .82f);
            MgfLook.Block("PairTag" + index, new Vector3(0, 7.65f, -1.35f), new Vector3(1.22f, .30f, .12f), .08f, ivoryMat, root);
            for (int p = 0; p < 3; p++)
            {
                int smokeIndex = index * 3 + p;
                smoke[smokeIndex] = MgfLook.Prim(PrimitiveType.Sphere, "ChimneySmoke" + smokeIndex, new Vector3(.78f, 9.7f + p * .58f, .55f),
                    Vector3.one * (.34f + p * .10f), smokeMat, root, false).transform;
            }
            MgfLook.Prim(PrimitiveType.Cylinder, "PlantPot", new Vector3(-.92f, .26f, -1.28f), new Vector3(.28f, .26f, .28f), copperMat, root, false);
            for (int leaf = 0; leaf < 3; leaf++)
            {
                var l = MgfLook.Prim(PrimitiveType.Sphere, "PlantLeaf" + leaf, new Vector3(-1.08f + leaf * .16f, .66f + (leaf % 2) * .18f, -1.28f), new Vector3(.18f, .42f, .13f), greenMat, root, false).transform;
                l.localRotation = Quaternion.Euler(0, 0, -28 + leaf * 28);
            }
            return root;
        }

        Transform BuildResident(string name, Vector3 local, Transform parent, int house, int slot)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); root.localPosition = local;
            MgfLook.Prim(PrimitiveType.Sphere, "IvoryResident", Vector3.zero, new Vector3(.29f, .31f, .22f), ivoryMat, root, false);
            if (slot % 7 == house % 3) MgfLook.Block("SquareTail", new Vector3(.12f, -.18f, .02f), new Vector3(.18f, .15f, .12f), .04f, ivoryMat, root);
            return root;
        }

        Transform BuildGhostPair(string name, Vector3 local, Transform parent, float scale)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); root.localPosition = local; root.localScale = Vector3.one * scale;
            for (int g = -1; g <= 1; g += 2)
            {
                var ghost = new GameObject("HouseGhost" + g).transform; ghost.SetParent(root, false); ghost.localPosition = new Vector3(g * .34f, 0, 0);
                MgfLook.Prim(PrimitiveType.Sphere, "RoundBody", new Vector3(0, .18f, 0), new Vector3(.56f, .62f, .44f), ivoryMat, ghost, false);
                MgfLook.Block("SquareTail", new Vector3(g * -.08f, -.18f, .04f), new Vector3(.28f, .24f, .24f), .07f, ivoryMat, ghost);
                MgfLook.Prim(PrimitiveType.Sphere, "EyeL", new Vector3(-.11f, .27f, -.39f), new Vector3(.065f, .08f, .045f), charcoalMat, ghost, false);
                MgfLook.Prim(PrimitiveType.Sphere, "EyeR", new Vector3(.11f, .27f, -.39f), new Vector3(.065f, .08f, .045f), charcoalMat, ghost, false);
                MgfLook.Block("Arm", new Vector3(-g * .30f, .12f, -.02f), new Vector3(.30f, .10f, .10f), .05f, ivoryMat, ghost).transform.localRotation = Quaternion.Euler(0, 0, g * 22f);
            }
            MgfLook.Block("MovingRibbon", new Vector3(0, -.39f, .06f), new Vector3(1.10f, .11f, .11f), .04f, copperMat, root);
            return root;
        }

        void BuildUi()
        {
            enamelSprite = MakeSprite(72, 9, false); ringSprite = MakeSprite(72, 22, true);
            dotSprite = Sprite.Create(MgfLook.SoftDot, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 100);
            canvas = MgfText.Canvas;
            titleGroup = R("TitleApartmentSign", canvas.transform); Stretch(titleGroup);
            playGroup = R("MovingLedger", canvas.transform); Stretch(playGroup);
            revealGroup = R("VarianceReveal", canvas.transform); Stretch(revealGroup);
            endGroup = R("EndApartmentSign", canvas.transform); Stretch(endGroup);
            toastGroup = R("PointerResponse", canvas.transform); Stretch(toastGroup);

            titleSign = Img(titleGroup, "CreamHangingSign", new Vector2(.5f, .69f), new Vector2(340, 144), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), enamelSprite);
            titleSign.rectTransform.localRotation = Quaternion.Euler(0, 0, -2f);
            titleLogo = Txt(titleGroup, "유령 이사", new Vector2(.5f, .72f), new Vector2(330, 82), 52, Brick);
            titleLogo.outlineColor = Charcoal; titleLogo.outlineWidth = .13f; titleLogo.characterSpacing = 3;
            titleTagline = Txt(titleGroup, "두 유령을 옮겨 산포도를 맞춰라", new Vector2(.5f, .63f), new Vector2(330, 48), 17, Charcoal);
            titleTag = Img(titleGroup, "MovingLuggageTag", new Vector2(.5f, .20f), new Vector2(280, 72), new Color(Brick.r, Brick.g, Brick.b, .96f), enamelSprite);
            titleCta = Txt(titleGroup, "이삿짐 꼬리표를 눌러 시작", new Vector2(.5f, .20f), new Vector2(270, 62), 20, Ivory);
            titleMeta = Txt(titleGroup, "중학교 3학년 · 산포도 · 여덟 번의 이사", new Vector2(.5f, .11f), new Vector2(360, 34), 13, Ivory);
            titleBest = Txt(titleGroup, "최고 첫 시도 0/8", new Vector2(.5f, .07f), new Vector2(320, 32), 13, Success);

            hudBg = Img(playGroup, "CopperEaveHud", new Vector2(.5f, .965f), new Vector2(360, 58), new Color(Charcoal.r, Charcoal.g, Charcoal.b, .95f), enamelSprite);
            hudText = Txt(playGroup, "", new Vector2(.47f, .965f), new Vector2(300, 52), 13, Ivory);
            scoreText = Txt(playGroup, "0", new Vector2(.88f, .965f), new Vector2(62, 50), 18, Success);
            for (int i = 0; i < 3; i++) sealIcons[i] = Img(playGroup, "WaxSeal" + i, new Vector2(.78f + i * .055f, .936f), new Vector2(16, 16), i < st.lives ? Copper : Charcoal, dotSprite);
            goalBg = Img(playGroup, "GoalLintel", new Vector2(.5f, .89f), new Vector2(368, 48), new Color(Brick.r, Brick.g, Brick.b, .96f), enamelSprite);
            goalText = Txt(playGroup, "두 유령을 옮겨 목표 산포도를 만드시오", new Vector2(.5f, .89f), new Vector2(350, 42), 16, Ivory);

            mathBoard = Img(playGroup, "CreamMathNotice", new Vector2(.5f, .145f), new Vector2(364, 205), new Color(Ivory.r, Ivory.g, Ivory.b, .97f), enamelSprite);
            promptText = Txt(playGroup, "", new Vector2(.5f, .205f), new Vector2(342, 72), 15, Charcoal);
            promptText.enableAutoSizing = true; promptText.fontSizeMin = 11; promptText.fontSizeMax = 15;
            sourceText = Txt(playGroup, "", new Vector2(.5f, .155f), new Vector2(342, 36), 14, Brick);
            targetText = Txt(playGroup, "", new Vector2(.5f, .112f), new Vector2(342, 42), 20, Error);
            definitionText = Txt(playGroup, "분산 = 편차²의 합 ÷ 4", new Vector2(.5f, .072f), new Vector2(342, 34), 13, Charcoal);

            for (int i = 0; i < 4; i++)
            {
                housePlaques[i] = Img(playGroup, "HouseNumberPlaque" + i, new Vector2(.2f + i * .2f, .48f), new Vector2(64, 58), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), enamelSprite);
                houseNumbers[i] = Txt(playGroup, "0", new Vector2(.2f + i * .2f, .48f), new Vector2(58, 52), 25, Charcoal);
                houseNumbers[i].outlineColor = new Color(Brick.r, Brick.g, Brick.b, .25f); houseNumbers[i].outlineWidth = .05f;
            }

            revealBoard = Img(revealGroup, "LitEvidenceBoard", new Vector2(.5f, .50f), new Vector2(360, 184), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), enamelSprite);
            revealTitle = Txt(revealGroup, "", new Vector2(.5f, .555f), new Vector2(338, 54), 23, Brick);
            revealText = Txt(revealGroup, "", new Vector2(.5f, .475f), new Vector2(334, 104), 14, Charcoal);

            endBoard = Img(endGroup, "FinalHouseSign", new Vector2(.5f, .52f), new Vector2(356, 330), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), enamelSprite);
            endTitle = Txt(endGroup, "", new Vector2(.5f, .62f), new Vector2(336, 82), 39, Brick);
            endStats = Txt(endGroup, "", new Vector2(.5f, .49f), new Vector2(326, 150), 18, Charcoal);
            endCta = Txt(endGroup, "간판을 눌러 다시 이사", new Vector2(.5f, .36f), new Vector2(300, 58), 19, Ivory);
            var endTag = Img(endGroup, "RestartTag", new Vector2(.5f, .36f), new Vector2(300, 64), new Color(Brick.r, Brick.g, Brick.b, .97f), enamelSprite);
            endTag.transform.SetAsFirstSibling();

            guideLine = Img(playGroup, "GhostFlightPath", new Vector2(.5f, .55f), new Vector2(120, 6), new Color(Success.r, Success.g, Success.b, .72f), enamelSprite);
            guideRing = Img(playGroup, "GhostHandlePulse", new Vector2(.5f, .55f), new Vector2(82, 82), new Color(Success.r, Success.g, Success.b, .9f), ringSprite);
            guideArrow = Img(playGroup, "DestinationArrow", new Vector2(.5f, .55f), new Vector2(30, 30), Success, dotSprite);

            ripple = Img(toastGroup, "DustRipple", new Vector2(.5f, .5f), new Vector2(20, 20), new Color(Apricot.r, Apricot.g, Apricot.b, 0), ringSprite);
            var toastBg = Img(toastGroup, "RefusalRibbon", new Vector2(.5f, .27f), new Vector2(348, 48), new Color(Charcoal.r, Charcoal.g, Charcoal.b, .94f), enamelSprite);
            toastText = Txt(toastGroup, "", new Vector2(.5f, .27f), new Vector2(332, 42), 14, Ivory);
            toastBg.gameObject.SetActive(false); toastText.gameObject.SetActive(false);

            SetGroup(titleGroup, true); SetGroup(playGroup, false); SetGroup(revealGroup, false); SetGroup(endGroup, false);
        }

        static void SetGroup(RectTransform group, bool value) { if (group) group.gameObject.SetActive(value); }

        void ResetWorldForTitle()
        {
            int[] titleValues = { 8, 12, 18, 24 };
            SyncResidents(titleValues, false); feedbackVisualClock = 99f; flightActive = false; dragPair.gameObject.SetActive(false);
            for (int i = 0; i < recordLights.Length; i++) { recordLights[i].localPosition = recordBase[i]; recordLights[i].localScale = Vector3.one * .42f; }
            Relayout(true);
        }

        void LoadProblemWorld(TransferProblem problem, bool practice)
        {
            SyncResidents(problem.values, false); feedbackVisualClock = 99f; flightActive = false; dragPair.gameObject.SetActive(false);
            for (int i = 0; i < recordLights.Length; i++) { recordLights[i].localPosition = recordBase[i]; recordLights[i].localScale = Vector3.one * .30f; }
            Relayout(true);
        }

        void ShowTitleUi()
        {
            SetGroup(titleGroup, true); SetGroup(playGroup, false); SetGroup(revealGroup, false); SetGroup(endGroup, false);
            if (signRoot) signRoot.gameObject.SetActive(true);
            titleBest.text = "최고 첫 시도 " + PlayerPrefs.GetInt("yuryeong-isa-best-first", 0) + "/8";
        }

        void ShowPlayUi()
        {
            SetGroup(titleGroup, false); SetGroup(playGroup, true); SetGroup(endGroup, false);
            if (signRoot) signRoot.gameObject.SetActive(false);
            if (flow != Flow.Feedback) SetGroup(revealGroup, false);
            guideVisible = flow == Flow.Practice || flow == Flow.Repair;
            if (flow == Flow.Practice) { guideFrom = 3; guideTo = 0; }
            if (flow == Flow.Repair) { guideFrom = wrongTo; guideTo = wrongFrom; }
            RefreshAllUi();
        }

        void ShowEndUi(bool clear, string reason)
        {
            SetGroup(titleGroup, false); SetGroup(playGroup, false); SetGroup(revealGroup, false); SetGroup(endGroup, true);
            if (signRoot) signRoot.gameObject.SetActive(false);
            endTitle.text = clear ? "이사 완료" : "봉인된 골목";
            endTitle.color = clear ? Brick : Error;
            endStats.text = "첫 시도 " + st.firstAttemptCorrectCount + "/8\n완료한 이사 " + st.solved + "/8\n남은 봉인 " + st.lives + "/3\n\n" + reason;
            endCta.text = "간판을 눌러 다시 이사";
        }

        void RefreshAllUi()
        {
            if (!playGroup || !playGroup.gameObject.activeSelf || current == null) return;
            hudText.text = (flow == Flow.Practice ? "연습" : st.round + "/8") + "   첫 " + st.firstAttemptCorrectCount + "/" + st.firstAttemptTotal + "   " + Mathf.CeilToInt(remainingTime) + "초";
            for (int i = 0; i < 3; i++) sealIcons[i].color = i < st.lives ? Copper : new Color(Charcoal.r, Charcoal.g, Charcoal.b, .55f);
            goalText.text = flow == Flow.Repair ? "방금 옮긴 두 유령을 원래 집으로 돌려보내시오" : flow == Flow.Practice ? "20인 집에서 2인 집으로 두 유령을 옮기시오" : "두 유령을 옮겨 목표 산포도를 만드시오";
            promptText.text = flow == Flow.Practice ? "주민 수는 네 집의 변량입니다. 평균은 이동해도 변하지 않습니다." : current.prompt;
            sourceText.text = "원자료 " + YuryeongIsaRules.Join(st.originalValues) + "   평균 " + st.mean + "   시작 분산 " + st.startVariance;
            targetText.text = current.band < 3 ? "목표 분산  " + current.targetVariance : "목표 표준편차  " + YuryeongIsaRules.SqrtForm(current.targetVariance);
            definitionText.text = current.band == 1 ? "편차 = 변량 − 평균   ·   분산 = 편차²의 합 ÷ 4" : current.band == 2 ? "바뀌는 두 편차²만 다시 계산하시오" : "표준편차 = √분산   →   목표 분산 " + current.targetVariance;
            if (st.currentValues != null && st.currentValues.Length == 4)
                for (int i = 0; i < 4; i++) houseNumbers[i].text = st.currentValues[i].ToString();
        }

        void TickUi(float dt)
        {
            if (st.score != shownScore)
            {
                animatedScore = Mathf.MoveTowards(animatedScore, st.score, Mathf.Max(90f, Mathf.Abs(st.score - animatedScore) * 7f) * dt);
                int next = Mathf.RoundToInt(animatedScore); scoreText.text = next.ToString();
                if (next == st.score) { shownScore = st.score; MgfFx.Punch(scoreText.transform, .12f, .22f); }
            }
            if (playGroup && playGroup.gameObject.activeSelf)
            {
                hudText.text = (flow == Flow.Practice ? "연습" : st.round + "/8") + "   첫 " + st.firstAttemptCorrectCount + "/" + st.firstAttemptTotal + "   " + Mathf.CeilToInt(remainingTime) + "초";
                PositionHousePlaques();
            }
            titlePress = Mathf.Max(0, titlePress - dt * 3f);
            if (titleTag) titleTag.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 3.0f) * .025f + titlePress * .12f);
            if (titleSign) titleSign.rectTransform.localRotation = Quaternion.Euler(0, 0, -2f + Mathf.Sin(worldClock * .8f) * .8f);

            if (guideVisible && playGroup.gameObject.activeSelf)
            {
                PositionGuide(guideFrom, guideTo);
                float pulse = 1f + Mathf.Sin(worldClock * 5.2f) * .12f + guideBoost * .22f;
                guideRing.rectTransform.localScale = Vector3.one * pulse;
                guideRing.color = new Color(Success.r, Success.g, Success.b, .70f + .25f * Mathf.Sin(worldClock * 4.2f));
                guideBoost = Mathf.MoveTowards(guideBoost, 0f, dt * .4f);
            }
            guideLine.gameObject.SetActive(guideVisible && playGroup.gameObject.activeSelf);
            guideRing.gameObject.SetActive(guideVisible && playGroup.gameObject.activeSelf);
            guideArrow.gameObject.SetActive(guideVisible && playGroup.gameObject.activeSelf);

            if (rippleClock > 0)
            {
                rippleClock -= dt; float t = 1f - Mathf.Clamp01(rippleClock / .50f);
                ripple.rectTransform.anchorMin = ripple.rectTransform.anchorMax = ScreenToAnchor(ripplePosition);
                ripple.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(18f, 92f, t);
                ripple.color = new Color(Apricot.r, Apricot.g, Apricot.b, (1f - t) * .76f);
            }
            if (toastClock > 0)
            {
                toastClock -= dt;
                if (toastClock <= 0)
                {
                    toastText.gameObject.SetActive(false);
                    var bg = toastGroup.Find("RefusalRibbon"); if (bg) bg.gameObject.SetActive(false);
                }
            }
        }

        void TickWorld(float dt)
        {
            worldClock += dt;
            for (int i = 0; i < smoke.Length; i++)
            {
                if (!smoke[i]) continue;
                int p = i % 3; float phase = (worldClock * (.18f + p * .025f) + i * .23f) % 1f;
                smoke[i].localPosition = new Vector3(.78f + Mathf.Sin(worldClock + i) * .09f, 9.72f + phase * 1.85f, .55f);
                float s = .30f + phase * .38f; smoke[i].localScale = Vector3.one * s;
            }
            for (int h = 0; h < 4; h++)
            {
                if (pairHandles[h] && pairHandles[h].gameObject.activeSelf)
                {
                    float bob = Mathf.Sin(worldClock * 1.8f + h * 1.3f) * .08f;
                    pairHandles[h].localPosition = new Vector3(0, 8.16f + bob, -1.2f);
                    pairHandles[h].localRotation = Quaternion.Euler(0, Mathf.Sin(worldClock * 1.1f + h) * 4f, Mathf.Sin(worldClock * 1.6f + h) * 2f);
                }
                for (int r = 0; r < 28; r++) if (residents[h, r] && residents[h, r].gameObject.activeSelf)
                {
                    float baseY = 1.77f + (r / 4) * .65f;
                    var p = residents[h, r].localPosition; p.y = baseY + Mathf.Sin(worldClock * 1.25f + r * .51f + h) * .035f; residents[h, r].localPosition = p;
                }
            }
            if (flightActive)
            {
                flightClock += dt; float t = Mathf.Clamp01(flightClock / flightDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                dragPair.position = Vector3.Lerp(flightStart, flightEnd, eased) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.5f;
                float squash = 1f + Mathf.Sin(t * Mathf.PI) * .18f;
                dragPair.localScale = new Vector3(1.18f / squash, 1.18f * squash, 1.18f);
                UpdateTrail(dragPair.position, true);
                if (t >= 1f)
                {
                    flightActive = false; dragPair.gameObject.SetActive(false); UpdateTrail(Vector3.zero, false);
                    SyncResidents(st.currentValues, false);
                }
            }
            if (feedbackVisualClock < 4f)
            {
                feedbackVisualClock += dt;
                if (feedbackVisualCorrect)
                {
                    float t = Mathf.Clamp01((feedbackVisualClock - .18f) / .72f);
                    for (int i = 0; i < 4; i++)
                    {
                        float targetX = (st.currentValues[i] - st.mean) * .22f;
                        recordLights[i].localPosition = Vector3.Lerp(recordBase[i], new Vector3(targetX, .75f + Mathf.Abs(st.currentValues[i] - st.mean) * .025f, -2.5f), t);
                        recordLights[i].localScale = Vector3.one * (0.30f + Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * .24f);
                    }
                    if (feedbackFrom >= 0 && feedbackTo >= 0)
                    {
                        float open = Mathf.Sin(Mathf.Clamp01(feedbackVisualClock / .8f) * Mathf.PI) * .38f;
                        curtains[feedbackFrom, 0].localPosition = new Vector3(-.82f - open, 5.8f, -1.51f);
                        curtains[feedbackFrom, 1].localPosition = new Vector3(.82f + open, 5.8f, -1.51f);
                        curtains[feedbackTo, 0].localPosition = new Vector3(-.82f - open * .75f, 5.8f, -1.51f);
                        curtains[feedbackTo, 1].localPosition = new Vector3(.82f + open * .75f, 5.8f, -1.51f);
                    }
                }
            }
        }

        void Relayout(bool force)
        {
            bool nextWide = MgfBridge.IsLandLayout;
            if (!force && Screen.width == lastW && Screen.height == lastH && nextWide == wide) return;
            lastW = Screen.width; lastH = Screen.height; wide = nextWide; st.layoutLand = wide;
            float[] xs = wide ? new[] { -7.1f, -3.75f, -.40f, 2.95f } : new[] { -4.05f, -1.35f, 1.35f, 4.05f };
            for (int i = 0; i < 4; i++)
            {
                buildingBase[i] = new Vector3(xs[i], 0, i % 2 == 0 ? .25f : .55f);
                buildings[i].localPosition = buildingBase[i]; buildings[i].localScale = new Vector3(wide ? 1.10f : .96f, 1, 1);
            }
            cam.transform.position = wide ? new Vector3(-1.5f, 10.8f, -22f) : new Vector3(0, 10.8f, -22f);
            cam.transform.LookAt(wide ? new Vector3(-1.8f, 3.7f, .4f) : new Vector3(0, 4.0f, .4f));
            float aspect = Mathf.Max(.1f, (float)Screen.width / Screen.height);
            cam.orthographicSize = wide ? 7.2f : Mathf.Max(10.5f, 5.7f / aspect);

            float logicalWidth = Screen.width / Mathf.Max(.01f, canvas.scaleFactor);
            if (wide)
            {
                hudBg.rectTransform.anchorMin = hudBg.rectTransform.anchorMax = new Vector2(.42f, .958f);
                hudBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(620, logicalWidth * .70f), 54);
                hudText.rectTransform.anchorMin = hudText.rectTransform.anchorMax = new Vector2(.40f, .958f);
                scoreText.rectTransform.anchorMin = scoreText.rectTransform.anchorMax = new Vector2(.70f, .958f);
                goalBg.rectTransform.anchorMin = goalBg.rectTransform.anchorMax = new Vector2(.38f, .875f);
                goalText.rectTransform.anchorMin = goalText.rectTransform.anchorMax = new Vector2(.38f, .875f);
                mathBoard.rectTransform.anchorMin = mathBoard.rectTransform.anchorMax = new Vector2(.865f, .51f);
                mathBoard.rectTransform.sizeDelta = new Vector2(Mathf.Min(340, logicalWidth * .28f), 330);
                promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = new Vector2(.865f, .66f);
                promptText.rectTransform.sizeDelta = new Vector2(Mathf.Min(315, logicalWidth * .255f), 120);
                sourceText.rectTransform.anchorMin = sourceText.rectTransform.anchorMax = new Vector2(.865f, .48f);
                sourceText.rectTransform.sizeDelta = new Vector2(Mathf.Min(315, logicalWidth * .255f), 70);
                targetText.rectTransform.anchorMin = targetText.rectTransform.anchorMax = new Vector2(.865f, .35f);
                definitionText.rectTransform.anchorMin = definitionText.rectTransform.anchorMax = new Vector2(.865f, .24f);
                targetText.rectTransform.sizeDelta = definitionText.rectTransform.sizeDelta = new Vector2(Mathf.Min(315, logicalWidth * .255f), 62);
            }
            else
            {
                hudBg.rectTransform.anchorMin = hudBg.rectTransform.anchorMax = new Vector2(.5f, .965f); hudBg.rectTransform.sizeDelta = new Vector2(360, 58);
                hudText.rectTransform.anchorMin = hudText.rectTransform.anchorMax = new Vector2(.47f, .965f);
                scoreText.rectTransform.anchorMin = scoreText.rectTransform.anchorMax = new Vector2(.88f, .965f);
                goalBg.rectTransform.anchorMin = goalBg.rectTransform.anchorMax = new Vector2(.5f, .89f);
                goalText.rectTransform.anchorMin = goalText.rectTransform.anchorMax = new Vector2(.5f, .89f);
                mathBoard.rectTransform.anchorMin = mathBoard.rectTransform.anchorMax = new Vector2(.5f, .145f); mathBoard.rectTransform.sizeDelta = new Vector2(364, 205);
                promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = new Vector2(.5f, .205f); promptText.rectTransform.sizeDelta = new Vector2(342, 72);
                sourceText.rectTransform.anchorMin = sourceText.rectTransform.anchorMax = new Vector2(.5f, .155f); sourceText.rectTransform.sizeDelta = new Vector2(342, 36);
                targetText.rectTransform.anchorMin = targetText.rectTransform.anchorMax = new Vector2(.5f, .112f); targetText.rectTransform.sizeDelta = new Vector2(342, 42);
                definitionText.rectTransform.anchorMin = definitionText.rectTransform.anchorMax = new Vector2(.5f, .072f); definitionText.rectTransform.sizeDelta = new Vector2(342, 34);
            }
            PositionHousePlaques();
        }

        void PositionHousePlaques()
        {
            if (!cam || !playGroup || !playGroup.gameObject.activeSelf) return;
            for (int i = 0; i < 4; i++)
            {
                Vector3 world = buildings[i].TransformPoint(new Vector3(0, 3.65f, -1.55f));
                Vector3 screen = cam.WorldToScreenPoint(world); Vector2 anchor = new Vector2(screen.x / Screen.width, screen.y / Screen.height);
                housePlaques[i].rectTransform.anchorMin = housePlaques[i].rectTransform.anchorMax = anchor;
                houseNumbers[i].rectTransform.anchorMin = houseNumbers[i].rectTransform.anchorMax = anchor;
            }
            for (int i = 0; i < 4; i++) st.houseXPx[i] = Mathf.RoundToInt(GetHandleScreen(i).x);
            st.handleYPx = Mathf.RoundToInt(GetHandleScreen(0).y); st.dropYPx = Mathf.RoundToInt(GetDropScreen(0).y);
        }

        Vector2 GetHandleScreen(int house)
        {
            Vector3 p = pairHandles[Mathf.Clamp(house, 0, 3)].position;
            Vector3 s = cam.WorldToScreenPoint(p); return new Vector2(s.x, s.y);
        }

        Vector2 GetDropScreen(int house)
        {
            Vector3 p = buildings[Mathf.Clamp(house, 0, 3)].TransformPoint(new Vector3(0, 5.25f, -1.62f));
            Vector3 s = cam.WorldToScreenPoint(p); return new Vector2(s.x, s.y);
        }

        public int HitHouseHandle(Vector2 screen)
        {
            float radius = Mathf.Max(52f, Mathf.Min(Screen.width, Screen.height) * .082f);
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                float d = Vector2.Distance(screen, GetHandleScreen(i));
                if (d < radius && d < bestD) { best = i; bestD = d; }
            }
            return best;
        }

        public int HitHouseDrop(Vector2 screen)
        {
            float radius = Mathf.Max(58f, Mathf.Min(Screen.width, Screen.height) * .095f);
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                float d = Vector2.Distance(screen, GetDropScreen(i));
                if (d < radius && d < bestD) { best = i; bestD = d; }
            }
            return best;
        }

        void PositionGuide(int from, int to)
        {
            Vector2 a = GetHandleScreen(from) / Mathf.Max(.01f, canvas.scaleFactor);
            Vector2 b = GetDropScreen(to) / Mathf.Max(.01f, canvas.scaleFactor);
            float canvasW = Screen.width / Mathf.Max(.01f, canvas.scaleFactor), canvasH = Screen.height / Mathf.Max(.01f, canvas.scaleFactor);
            a -= new Vector2(canvasW * .5f, canvasH * .5f); b -= new Vector2(canvasW * .5f, canvasH * .5f);
            Vector2 delta = b - a; float len = delta.magnitude;
            guideRing.rectTransform.anchorMin = guideRing.rectTransform.anchorMax = new Vector2(.5f, .5f); guideRing.rectTransform.anchoredPosition = a;
            guideLine.rectTransform.anchorMin = guideLine.rectTransform.anchorMax = new Vector2(.5f, .5f); guideLine.rectTransform.anchoredPosition = (a + b) * .5f;
            guideLine.rectTransform.sizeDelta = new Vector2(len, 6); guideLine.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            guideArrow.rectTransform.anchorMin = guideArrow.rectTransform.anchorMax = new Vector2(.5f, .5f); guideArrow.rectTransform.anchoredPosition = b;
            guideArrow.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 5f) * .16f);
        }

        Vector2 ScreenToAnchor(Vector2 screen) => new Vector2(screen.x / Mathf.Max(1, Screen.width), screen.y / Mathf.Max(1, Screen.height));

        void SyncResidents(int[] values, bool punch)
        {
            if (values == null || values.Length != 4) return;
            for (int h = 0; h < 4; h++)
            {
                int gridCount = Mathf.Clamp(values[h] - 2, 0, 28);
                for (int slot = 0; slot < 28; slot++) residents[h, slot].gameObject.SetActive(slot < gridCount);
                bool hideForMotion = (dragFrom == h) || (flightActive && (flightFrom == h || flightTo == h));
                pairHandles[h].gameObject.SetActive(values[h] >= 2 && !hideForMotion);
                if (punch && houseNumbers[h] && st.currentValues != null && h < st.currentValues.Length)
                {
                    houseNumbers[h].text = st.currentValues[h].ToString();
                    MgfFx.Punch(houseNumbers[h].transform, .16f, .22f);
                }
            }
        }

        void BeginGhostDrag(int house, Vector2 screen)
        {
            pairHandles[house].gameObject.SetActive(false); dragPair.gameObject.SetActive(true); dragPair.localScale = Vector3.one * .88f;
            MoveGhostDrag(screen, -1); guideVisible = false;
            for (int i = 0; i < trail.Length; i++) trail[i].gameObject.SetActive(true);
            UpdateTrail(dragPair.position, true);
        }

        void MoveGhostDrag(Vector2 screen, int hoverHouse)
        {
            float z = cam.WorldToScreenPoint(buildings[0].TransformPoint(new Vector3(0, 5.5f, -1.7f))).z;
            dragPair.position = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
            float pulse = hoverHouse >= 0 ? 1.10f : 1f;
            dragPair.localScale = Vector3.Lerp(dragPair.localScale, new Vector3(.94f / pulse, .94f * pulse, .94f), .28f);
            UpdateTrail(dragPair.position, true);
        }

        void CancelGhostDrag(int from)
        {
            dragPair.gameObject.SetActive(false); UpdateTrail(Vector3.zero, false);
            if (from >= 0 && from < 4 && st.currentValues.Length == 4 && st.currentValues[from] >= 2) pairHandles[from].gameObject.SetActive(true);
        }

        void PlayDropFlight(int from, int to, bool correct)
        {
            flightActive = true; flightClock = 0f; flightDuration = correct ? .55f : .42f; flightFrom = from; flightTo = to;
            flightStart = dragPair.gameObject.activeSelf ? dragPair.position : pairHandles[from].position;
            flightEnd = buildings[to].TransformPoint(new Vector3(0, 5.35f, -1.72f));
            dragPair.gameObject.SetActive(true); pairHandles[from].gameObject.SetActive(false); pairHandles[to].gameObject.SetActive(false);
            MgfSfx.Play("whoosh", .30f);
        }

        void PlayRepairFlight(int from, int to)
        {
            dragPair.position = buildings[from].TransformPoint(new Vector3(0, 5.35f, -1.72f));
            PlayDropFlight(from, to, true);
            feedbackVisualCorrect = false; feedbackVisualClock = 0f;
        }

        void UpdateTrail(Vector3 head, bool active)
        {
            for (int i = 0; i < trail.Length; i++)
            {
                trail[i].gameObject.SetActive(active);
                if (!active) continue;
                float lag = (i + 1) * .085f;
                trail[i].position = Vector3.Lerp(trail[i].position, head - Vector3.up * lag, .28f);
            }
        }

        void PlayCorrectChain(TransferProblem problem, int[] before, int[] after, bool practice)
        {
            feedbackVisualCorrect = true; feedbackVisualClock = 0f; feedbackFrom = problem.from; feedbackTo = problem.to;
            SetGroup(revealGroup, true); revealTitle.text = practice ? "평균은 그대로, 간격은 달라짐" : "목표 산포도 완성";
            int from = -1, to = -1;
            for (int i = 0; i < 4; i++) { if (after[i] == before[i] - 2) from = i; if (after[i] == before[i] + 2) to = i; }
            int oldA = from >= 0 ? before[from] - problem.mean : 0, newA = from >= 0 ? after[from] - problem.mean : 0;
            int oldB = to >= 0 ? before[to] - problem.mean : 0, newB = to >= 0 ? after[to] - problem.mean : 0;
            revealText.text = practice
                ? "총수 40 · 평균 10 고정\n20→18, 2→4\n100→64, 64→36\n(36+16+4+64)÷4=30"
                : "평균 " + problem.mean + " 고정\n" + Signed(oldA) + "²→" + Signed(newA) + "²   ·   " + Signed(oldB) + "²→" + Signed(newB) + "²\n결과 " + YuryeongIsaRules.Join(after) + "   분산 " + problem.targetVariance;
            MgfFx.Punch(revealBoard.transform, .10f, .34f);
        }

        void PlayWrongChain(int from, int to, string misconception)
        {
            feedbackVisualCorrect = false; feedbackVisualClock = 0f; feedbackFrom = from; feedbackTo = to;
            SetGroup(revealGroup, true); revealTitle.text = "봉인 하나가 찢어졌습니다"; revealTitle.color = Error;
            int result = YuryeongIsaRules.Variance(st.currentValues);
            revealText.text = "현재 자료 " + YuryeongIsaRules.Join(st.currentValues) + " · 분산 " + result + "\n" + YuryeongIsaRules.MisconceptionFeedback(misconception) + "\n두 유령을 원래 집으로 직접 돌려보내시오.";
            MgfFx.Shake(cam, .075f, .22f); RefuseHouse(to);
        }

        void PlayPracticeWrong(TransferProblem problem, int from, int to)
        {
            feedbackVisualCorrect = false; feedbackVisualClock = 0f; feedbackFrom = from; feedbackTo = to;
            SetGroup(revealGroup, true); revealTitle.text = "연습 목표를 다시 확인"; revealTitle.color = Error;
            revealText.text = "현재 분산 " + YuryeongIsaRules.Variance(st.currentValues) + "\n정답 상태 [4,6,12,18] · 분산 30\n20인 집에서 2인 집으로 옮기시오.";
            MgfFx.Shake(cam, .055f, .18f);
        }

        void StopFeedbackVisuals()
        {
            SetGroup(revealGroup, false); revealTitle.color = Brick;
            for (int h = 0; h < 4; h++)
            {
                curtains[h, 0].localPosition = new Vector3(-.82f, 5.8f, -1.51f);
                curtains[h, 1].localPosition = new Vector3(.82f, 5.8f, -1.51f);
            }
        }

        static string Signed(int value) => value < 0 ? "−" + (-value) : value > 0 ? "+" + value : "0";

        void OpenShutters()
        {
            for (int h = 0; h < 4; h++)
            {
                curtains[h, 0].localPosition += Vector3.left * .28f;
                curtains[h, 1].localPosition += Vector3.right * .28f;
            }
        }

        void PressTitleTag() { titlePress = 1f; MgfFx.Punch(titleTag.transform, .14f, .26f); }
        void BoostGuide(bool strong) { guideVisible = true; guideBoost = strong ? 1f : .55f; MgfSfx.Play("tap", .08f); }
        void PointRepairPath(int from, int to) { guideFrom = from; guideTo = to; guideVisible = true; guideBoost = 1f; }

        void PointToNearestHandle(Vector2 screen)
        {
            int best = 0; float d = float.MaxValue;
            for (int i = 0; i < 4; i++) { float next = Vector2.Distance(screen, GetHandleScreen(i)); if (next < d) { d = next; best = i; } }
            guideFrom = best; guideTo = (best + 1) % 4; guideVisible = true; guideBoost = .65f;
        }

        void PointToNearestDrop(Vector2 screen)
        {
            int best = 0; float d = float.MaxValue;
            for (int i = 0; i < 4; i++) { float next = Vector2.Distance(screen, GetDropScreen(i)); if (next < d) { d = next; best = i; } }
            guideFrom = st.dragFrom >= 0 ? st.dragFrom : (best + 1) % 4; guideTo = best; guideVisible = true; guideBoost = .55f;
        }

        void RefuseHouse(int house)
        {
            if (house < 0 || house > 3) return;
            var root = buildings[house];
            MgfFx.Punch(root, .07f, .18f);
        }

        void SpawnTapRipple(Vector2 screen) { ripplePosition = screen; rippleClock = .50f; }

        void RefuseInput(string message, Vector2 screen)
        {
            SpawnTapRipple(screen); toastClock = 1.25f; toastText.text = message; toastText.gameObject.SetActive(true);
            var bg = toastGroup.Find("RefusalRibbon"); if (bg) bg.gameObject.SetActive(true);
            MgfSfx.Play("tap", .08f);
        }

        void PlayEndWorld(bool clear)
        {
            if (clear)
            {
                for (int i = 0; i < 4; i++) { recordLights[i].localScale = Vector3.one * .55f; MgfFx.Punch(pairHandles[i], .15f, .45f); }
            }
            else MgfFx.Shake(cam, .09f, .28f);
        }
    }
}
