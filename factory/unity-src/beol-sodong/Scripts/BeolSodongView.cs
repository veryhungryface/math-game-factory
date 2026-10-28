using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.BeolSodong
{
    public sealed partial class BeolSodongGame
    {
        static readonly Color Limestone = MgfLook.Hex("#E7D8B1");
        static readonly Color LimestoneLight = MgfLook.Hex("#F3EFE2");
        static readonly Color Ebony = MgfLook.Hex("#2C211B");
        static readonly Color Brass = MgfLook.Hex("#C78A35");
        static readonly Color Cobalt = MgfLook.Hex("#315B86");
        static readonly Color Silver = MgfLook.Hex("#C9D2D3");
        static readonly Color Ink = MgfLook.Hex("#3A2B23");
        static readonly Color WarmWhite = MgfLook.Hex("#FFF9E9");
        static readonly Color WrongMilk = MgfLook.Hex("#E9B9AE");

        Camera cam;
        Transform worldRoot, clockRoot, boardRoot, beeRoot, plateRoot;
        Transform[] bees = new Transform[9];
        Transform[] beeHeads = new Transform[9];
        Transform[] leftWings = new Transform[9];
        Transform[] rightWings = new Transform[9];
        Transform[] plates = new Transform[9];
        Transform[] plateNegatives = new Transform[9];
        LineRenderer[] sunBeams = new LineRenderer[9];
        Vector3[] beeHome = new Vector3[9];
        float[] beePhase = new float[9];
        LineRenderer strokeLine, strokeGlow, guideLine;
        readonly Vector3[] strokePoints = new Vector3[72];
        int strokePointCount;
        Material brassLineMat, brassGlowMat, guideMat;
        Light sun;

        Canvas canvas;
        GameObject titleUi, gameUi, endUi;
        RectTransform titlePlateRt, restartRt, cancelRt, hudLeftRt, hudRightRt;
        TextMeshProUGUI logoTxt, titleSubTxt, titlePlateTxt, titleBadgeTxt;
        TextMeshProUGUI goalTxt, statusTxt, promptTxt, definitionTxt, toastTxt, evidenceTxt;
        TextMeshProUGUI endTitleTxt, endStatsTxt, restartTxt;
        TextMeshProUGUI[] numberTxt = new TextMeshProUGUI[9];
        RectTransform[] numberRt = new RectTransform[9];
        Image[] numberBg = new Image[9];
        Image[] lifeIcons = new Image[3];
        Image[] photoIcons = new Image[9];
        Image screenFlash;
        RectTransform guideHand;
        RectTransform guideStartRing;
        readonly TextMeshProUGUI[] guideArrows = new TextMeshProUGUI[3];
        Image guideHandImage;
        Sprite whiteSprite, softCircleSprite, ringSprite;

        float ambientT, titleT, toastT, evidenceT, flashT, guideT, refuseT;
        float displayedScore;
        bool guideStrong, revealActive, revealWasCorrect, wide;
        string toastMessage = "";
        Vector3 cameraBasePosition;

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#5F8BB4"), MgfLook.Hex("#B9D6D9"), MgfLook.Hex("#D4BD8F"), .78f);
            cam = MgfLook.Camera(new Vector3(9.2f, 7.9f, -18.2f), new Vector3(-.15f, 3.2f, 0), 31f);
            cam.orthographic = true; cam.orthographicSize = 7.2f; cam.nearClipPlane = .1f; cam.farClipPlane = 80f;
            cameraBasePosition = cam.transform.position;
            sun = MgfLook.Sun(new Vector3(48f, -38f, 12f), MgfLook.Hex("#FFE3A4"), 1.32f, .68f);

            worldRoot = new GameObject("Limestone Rapeseed Clock World").transform;
            clockRoot = new GameObject("Broken Limestone Clocktower Facade").transform; clockRoot.SetParent(worldRoot, false);
            boardRoot = new GameObject("Nine Staggered Iron Balconies").transform; boardRoot.SetParent(worldRoot, false);
            beeRoot = new GameObject("Ebony Articulated Bees").transform; beeRoot.SetParent(worldRoot, false);
            plateRoot = new GameObject("Silver Plate Side Archive").transform; plateRoot.SetParent(worldRoot, false);

            Material stone = MgfLook.Lit(Limestone, .18f, .03f);
            Material stoneLight = MgfLook.Lit(LimestoneLight, .38f, .02f);
            Material skyCut = MgfLook.Lit(Cobalt, .2f, 0f);
            Material brass = MgfLook.Lit(Brass, .58f, .72f);
            Material silver = MgfLook.Lit(Silver, .72f, .68f);
            Material ebony = MgfLook.Lit(Ebony, .31f, .08f);
            Material mica = MgfLook.Lit(new Color(.76f, .82f, .76f, 1f), .82f, .18f);
            Material ivory = MgfLook.Lit(WarmWhite, .48f, .03f);
            Material exposed = MgfLook.Lit(new Color(.16f, .2f, .2f, 1f), .28f, .12f, new Color(.12f, .18f, .18f));

            // 중앙 원판 대신 화면을 세로로 가르는 비대칭 시계탑 파사드다. 깨진 석회 부조,
            // 얇은 흑철 절판, 노출 황동 핀이 각 층을 만든다.
            MgfLook.Block("Tall fractured limestone body", new Vector3(-.45f, 3.45f, 1.15f), new Vector3(9.35f, 7.55f, 1.05f), .045f, stone, clockRoot);
            MgfLook.Block("Left stepped buttress", new Vector3(-5.05f, 2.6f, 1.42f), new Vector3(2.2f, 5.3f, 1.45f), .035f, stoneLight, clockRoot);
            MgfLook.Block("Right broken stair tower", new Vector3(4.55f, 4.05f, 1.35f), new Vector3(1.75f, 5.7f, 1.25f), .035f, stoneLight, clockRoot);
            MgfLook.Block("Cobalt vertical breach", new Vector3(3.98f, 2.05f, .58f), new Vector3(1.05f, 2.6f, .34f), .02f, skyCut, clockRoot);
            MgfLook.Block("Suspended archive sign", new Vector3(-2.55f, 7.35f, -.02f), new Vector3(4.15f, .82f, .18f), .025f, exposed, clockRoot);
            for (int pin = -1; pin <= 1; pin += 2)
                MgfLook.Prim(PrimitiveType.Cylinder, "Exposed sign pin", new Vector3(-2.55f + pin * 1.72f, 7.76f, -.08f), new Vector3(.1f, .46f, .1f), brass, clockRoot, false);

            for (int tier = 0; tier < 3; tier++)
            {
                float y = 5.65f - tier * 2.12f;
                float shift = tier == 0 ? .5f : tier == 1 ? -.32f : .28f;
                MgfLook.Block("Chipped limestone course " + (tier + 1), new Vector3(shift, y - .44f, .28f), new Vector3(8.8f, .78f, 1.1f), .025f, tier % 2 == 0 ? stoneLight : stone, clockRoot);
                MgfLook.Block("Thin black iron ledger " + (tier + 1), new Vector3(shift, y - .07f, -.34f), new Vector3(8.45f, .13f, .5f), .008f, exposed, clockRoot);
                for (int col = 0; col < 3; col++)
                {
                    float x = (col - 1) * 2.72f + shift;
                    MgfLook.Block("Iron balcony shelf " + tier + "-" + col, new Vector3(x, y - .25f, -.68f), new Vector3(2.05f, .16f, 1.85f), .012f, exposed, boardRoot);
                    for (int side = -1; side <= 1; side += 2)
                        MgfLook.Prim(PrimitiveType.Cylinder, "Balcony brass pin", new Vector3(x + side * .78f, y - .09f, -.93f), new Vector3(.085f, .24f, .085f), brass, boardRoot, false);
                }
            }
            for (int i = 0; i < 7; i++)
            {
                float x = -4.5f + i * 1.5f;
                float y = 6.55f + Mathf.Sin(i * 1.7f) * .18f;
                var relief = MgfLook.Block("Broken limestone crown " + i, new Vector3(x, y, .5f), new Vector3(1.2f, 1.1f + (i % 3) * .22f, .7f), .025f, i % 2 == 0 ? stone : stoneLight, clockRoot);
                relief.transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? -1 : 1) * (3f + i));
            }

            Vector3[] slots = {
                new Vector3(-2.22f,5.62f,-.84f), new Vector3(.50f,5.62f,-.84f), new Vector3(3.22f,5.62f,-.84f),
                new Vector3(-3.04f,3.50f,-.84f), new Vector3(-.32f,3.50f,-.84f), new Vector3(2.40f,3.50f,-.84f),
                new Vector3(-2.44f,1.38f,-.84f), new Vector3(.28f,1.38f,-.84f), new Vector3(3.00f,1.38f,-.84f)
            };
            for (int i = 0; i < 9; i++)
            {
                beeHome[i] = slots[i]; beePhase[i] = i * .77f;
                MgfLook.Prim(PrimitiveType.Cylinder, "Brass recess " + (i + 1), new Vector3(slots[i].x, slots[i].y - .18f, slots[i].z), new Vector3(1.22f, .09f, 1.22f), brass, boardRoot, false);
                bees[i] = MakeBee(i, slots[i], ebony, brass, mica, ivory);
            }

            for (int i = 0; i < 9; i++)
            {
                var p = new GameObject("Silver gelatin architecture plate " + (i + 1)).transform;
                p.SetParent(plateRoot, false);
                int plateSide = i < 5 ? -1 : 1; int row = i < 5 ? i : i - 5;
                p.localPosition = new Vector3(plateSide < 0 ? -5.65f : 5.12f, 1.18f + row * 1.28f, .05f);
                p.localRotation = Quaternion.Euler(90f, plateSide < 0 ? -7f : 7f, plateSide < 0 ? -3f : 3f);
                MgfLook.Block("Brass plate frame", new Vector3(0, 0, 0), new Vector3(1.5f, .11f, 1.82f), .025f, brass, p);
                MgfLook.Block("Silver glass emulsion", new Vector3(0, .08f, 0), new Vector3(1.22f, .08f, 1.52f), .018f, silver, p);
                Transform neg = MgfLook.Block("Developed route shadow", new Vector3(0, .145f, 0), new Vector3(.78f, .025f, 1.04f), .01f, exposed, p).transform;
                neg.localRotation = Quaternion.Euler(0, 18f + i * 11f, 0); neg.gameObject.SetActive(false); plateNegatives[i] = neg;
                for (int pinSide = -1; pinSide <= 1; pinSide += 2)
                    MgfLook.Prim(PrimitiveType.Cylinder, "Plate hinge pin", new Vector3(pinSide * .64f, .18f, -.69f), new Vector3(.08f, .18f, .08f), brass, p, false);
                p.gameObject.SetActive(false); plates[i] = p;

                LineRenderer beam = MakeLine("Developed sunlight path " + (i + 1), MgfLook.Additive(new Color(1f, .75f, .24f, .5f), MgfLook.SoftDot), .12f, 3);
                beam.positionCount = 3;
                beam.SetPosition(0, p.position + Vector3.up * .28f);
                beam.SetPosition(1, Vector3.Lerp(p.position, new Vector3(-.2f, 3.5f, -.8f), .52f) + Vector3.back * .35f);
                beam.SetPosition(2, new Vector3(-.2f, 3.5f, -.9f));
                beam.gameObject.SetActive(false); sunBeams[i] = beam;
            }

            brassLineMat = MgfLook.Unlit(Brass); brassGlowMat = MgfLook.Additive(new Color(1f, .72f, .2f, .42f), MgfLook.SoftDot);
            guideMat = MgfLook.Additive(new Color(1f, .54f, .08f, .98f), MgfLook.SoftDot);
            strokeGlow = MakeLine("Warm brass trail glow", brassGlowMat, .23f, 72);
            strokeLine = MakeLine("Student brass inlay", brassLineMat, .095f, 72);
            guideLine = MakeLine("Persistent brass onboarding route", guideMat, .19f, 18);
            guideLine.textureMode = LineTextureMode.Tile; guideLine.gameObject.SetActive(false);
        }

        Transform MakeBee(int index, Vector3 pos, Material ebony, Material brass, Material mica, Material ivory)
        {
            Transform root = new GameObject("Articulated ebony bee " + (index + 1)).transform;
            root.SetParent(beeRoot, false); root.localPosition = pos;
            MgfLook.Block("Faceted ebony thorax", new Vector3(0, .39f, 0), new Vector3(.62f, .5f, .66f), .045f, ebony, root);
            Transform abdomen = MgfLook.Block("Segmented ebony abdomen", new Vector3(0, .37f, -.61f), new Vector3(.7f, .45f, .82f), .035f, ebony, root).transform;
            abdomen.localRotation = Quaternion.Euler(0, 0, 0);
            for (int band = -1; band <= 1; band++)
                MgfLook.Block("Abdomen brass band", new Vector3(0, .39f, -.61f + band * .22f), new Vector3(.73f, .07f, .06f), .01f, brass, root);
            Transform head = MgfLook.Block("Angular jointed head", new Vector3(0, .45f, .64f), new Vector3(.52f, .46f, .5f), .025f, ebony, root).transform;
            beeHeads[index] = head;
            for (int side = -1; side <= 1; side += 2)
            {
                MgfLook.Prim(PrimitiveType.Cylinder, "Brass shoulder pin", new Vector3(side * .34f, .42f, .02f), new Vector3(.13f, .09f, .13f), brass, root, false);
                Transform wing = MgfLook.Block("Thin mica wing plate", new Vector3(side * .58f, .58f, -.08f), new Vector3(.78f, .035f, .42f), .012f, mica, root).transform;
                wing.localRotation = Quaternion.Euler(0, side * 17f, side * 22f);
                if (side < 0) leftWings[index] = wing; else rightWings[index] = wing;
                for (int leg = -1; leg <= 1; leg++)
                {
                    Transform limb = MgfLook.Block("Ebony articulated leg", new Vector3(side * .47f, .2f, leg * .31f), new Vector3(.5f, .055f, .07f), .012f, ebony, root).transform;
                    limb.localRotation = Quaternion.Euler(0, side * (leg * 18f + 13f), side * 17f);
                    MgfLook.Prim(PrimitiveType.Cylinder, "Leg brass pin", new Vector3(side * .29f, .28f, leg * .29f), new Vector3(.07f, .07f, .07f), brass, root, false);
                }
            }
            for (int side = -1; side <= 1; side += 2)
            {
                Transform feeler = MgfLook.Block("Ebony antenna", new Vector3(side * .17f, .67f, .85f), new Vector3(.045f, .045f, .48f), .008f, ebony, root).transform;
                feeler.localRotation = Quaternion.Euler(0, side * 18f, 0);
            }
            MgfLook.Block("Ivory numeral plaque", new Vector3(0, .93f, .03f), new Vector3(.92f, .085f, .62f), .025f, ivory, root);
            return root;
        }

        LineRenderer MakeLine(string name, Material material, float width, int cap)
        {
            var go = new GameObject(name); go.transform.SetParent(worldRoot, false);
            var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = material; lr.useWorldSpace = true;
            lr.positionCount = 0; lr.startWidth = lr.endWidth = width; lr.numCapVertices = 6; lr.numCornerVertices = 5;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
            return lr;
        }

        void BuildUi()
        {
            canvas = MgfText.Canvas;
            whiteSprite = MakeSolidSprite("White pixel", Color.white, 2);
            softCircleSprite = MakeCircleSprite("Soft ivory disc", false);
            ringSprite = MakeCircleSprite("Brass ring", true);

            titleUi = new GameObject("Title world overlay", typeof(RectTransform)); titleUi.transform.SetParent(canvas.transform, false); Stretch((RectTransform)titleUi.transform);
            gameUi = new GameObject("Play overlay", typeof(RectTransform)); gameUi.transform.SetParent(canvas.transform, false); Stretch((RectTransform)gameUi.transform);
            endUi = new GameObject("Result plate spread", typeof(RectTransform)); endUi.transform.SetParent(canvas.transform, false); Stretch((RectTransform)endUi.transform);

            logoTxt = Text("벌 소동", titleUi.transform, new Vector2(.27f, .82f), new Vector2(0, 0), 56, LimestoneLight, 390, FontStyles.Bold);
            logoTxt.outlineWidth = .28f; logoTxt.outlineColor = Ebony; logoTxt.characterSpacing = 3f;
            titleSubTxt = Text("흑철 발코니의 벌을 한 획으로 이어라", titleUi.transform, new Vector2(.27f, .735f), Vector2.zero, 18, WarmWhite, 410, FontStyles.Bold);
            titleSubTxt.outlineWidth = .16f; titleSubTxt.outlineColor = Ebony;
            titlePlateRt = Panel("Suspended start glass plate", titleUi.transform, new Vector2(.73f, .48f), Vector2.zero, new Vector2(188, 92), new Color(Silver.r, Silver.g, Silver.b, .94f), whiteSprite);
            Panel("Plate left brass clamp", titlePlateRt, new Vector2(.08f, .5f), Vector2.zero, new Vector2(9, 108), Brass, whiteSprite).SetAsFirstSibling();
            Panel("Plate right brass clamp", titlePlateRt, new Vector2(.92f, .5f), Vector2.zero, new Vector2(9, 108), Brass, whiteSprite).SetAsFirstSibling();
            titlePlateTxt = Text("빈 건판 걸기\n눌러서 탑 열기", titlePlateRt, new Vector2(.5f, .5f), Vector2.zero, 17, Ink, 168, FontStyles.Bold);
            titleBadgeTxt = Text("중1 · 자료의 정리와 해석   최고 첫 제출 " + SafeBest() + "/9", titleUi.transform, new Vector2(.28f, .12f), Vector2.zero, 14, WarmWhite, 500, FontStyles.Bold);
            titleBadgeTxt.outlineWidth = .16f; titleBadgeTxt.outlineColor = Ebony;

            hudLeftRt = Panel("Status limestone rail", gameUi.transform, new Vector2(.5f, 1f), new Vector2(-24, -45), new Vector2(330, 80), new Color(.95f, .91f, .79f, .96f), whiteSprite);
            goalTxt = Text("최빈값의 벌을 모두 이으면 건판 현상", hudLeftRt, new Vector2(.5f, .72f), Vector2.zero, 13, Ink, 308, FontStyles.Bold);
            statusTxt = Text("", hudLeftRt, new Vector2(.5f, .27f), Vector2.zero, 12, Cobalt, 308, FontStyles.Bold);

            hudRightRt = Panel("Photo plate rail", gameUi.transform, new Vector2(.5f, 0f), new Vector2(0, 49), new Vector2(382, 88), new Color(.19f, .13f, .1f, .88f), whiteSprite);
            definitionTxt = Text("최빈값 = 가장 많이 나타난 값 · 여러 개이면 모두", hudRightRt, new Vector2(.5f, .75f), Vector2.zero, 13, LimestoneLight, 360, FontStyles.Bold);
            promptTxt = Text("", hudRightRt, new Vector2(.5f, .34f), Vector2.zero, 13, WarmWhite, 356, FontStyles.Bold);
            promptTxt.textWrappingMode = TextWrappingModes.Normal;

            for (int i = 0; i < 9; i++)
            {
                RectTransform rt = Panel("Ivory number tag " + (i + 1), gameUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(62, 50), WarmWhite, whiteSprite);
                numberRt[i] = rt; numberBg[i] = rt.GetComponent<Image>();
                numberTxt[i] = Text("0", rt, new Vector2(.5f, .5f), Vector2.zero, 26, Ink, 58, FontStyles.Bold);
            }

            for (int i = 0; i < 3; i++)
            {
                RectTransform rt = Panel("Spare plate " + (i + 1), hudLeftRt, new Vector2(.10f + i * .055f, .06f), Vector2.zero, new Vector2(13, 12), Silver, whiteSprite);
                lifeIcons[i] = rt.GetComponent<Image>();
            }
            for (int i = 0; i < 9; i++)
            {
                RectTransform rt = Panel("Developed photo " + (i + 1), hudRightRt, new Vector2(.22f + i * .07f, .04f), Vector2.zero, new Vector2(17, 12), new Color(Silver.r, Silver.g, Silver.b, .26f), whiteSprite);
                photoIcons[i] = rt.GetComponent<Image>();
            }

            cancelRt = Panel("Unsubmitted stroke return groove", gameUi.transform, new Vector2(.5f, 0f), new Vector2(0, 101), new Vector2(116, 28), new Color(Cobalt.r, Cobalt.g, Cobalt.b, .92f), whiteSprite);
            Text("획 반납 홈", cancelRt, new Vector2(.5f, .5f), Vector2.zero, 12, WarmWhite, 105, FontStyles.Bold);
            toastTxt = Text("", gameUi.transform, new Vector2(.5f, .18f), Vector2.zero, 15, WarmWhite, 350, FontStyles.Bold);
            toastTxt.textWrappingMode = TextWrappingModes.Normal;
            evidenceTxt = Text("", gameUi.transform, new Vector2(.5f, .57f), Vector2.zero, 19, Ink, 340, FontStyles.Bold);
            evidenceTxt.textWrappingMode = TextWrappingModes.Normal;

            guideHand = Panel("Animated glass hand", gameUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(58, 58), new Color(1f, .92f, .56f, .95f), ringSprite);
            guideHandImage = guideHand.GetComponent<Image>(); guideHand.gameObject.SetActive(false);
            guideStartRing = Panel("Persistent start bee pulse", gameUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(86, 86), new Color(1f, .54f, .08f, .96f), ringSprite);
            guideStartRing.gameObject.SetActive(false);
            for (int i = 0; i < guideArrows.Length; i++)
            {
                guideArrows[i] = Text(">>", gameUi.transform, new Vector2(.5f, .5f), Vector2.zero, 22, Brass, 54, FontStyles.Bold);
                guideArrows[i].outlineWidth = .18f; guideArrows[i].outlineColor = Ebony; guideArrows[i].gameObject.SetActive(false);
            }

            screenFlash = Panel("Silver exposure flash", gameUi.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1600, 1600), new Color(1f, .96f, .82f, 0f), whiteSprite).GetComponent<Image>();
            screenFlash.transform.SetAsFirstSibling();

            RectTransform resultPanel = Panel("Dark silver result backing", endUi.transform, new Vector2(.5f, .58f), Vector2.zero, new Vector2(344, 276), new Color(.11f, .12f, .12f, .82f), whiteSprite);
            resultPanel.SetAsFirstSibling();
            endTitleTxt = Text("", endUi.transform, new Vector2(.5f, .72f), Vector2.zero, 45, LimestoneLight, 340, FontStyles.Bold);
            endTitleTxt.outlineWidth = .2f; endTitleTxt.outlineColor = Ebony;
            endStatsTxt = Text("", endUi.transform, new Vector2(.5f, .56f), Vector2.zero, 22, WarmWhite, 360, FontStyles.Bold);
            endStatsTxt.textWrappingMode = TextWrappingModes.Normal;
            restartRt = Panel("Restart glass plate", endUi.transform, new Vector2(.5f, .34f), Vector2.zero, new Vector2(138, 104), Silver, softCircleSprite);
            restartTxt = Text("새 건판\n시작", restartRt, new Vector2(.5f, .5f), Vector2.zero, 18, Ink, 116, FontStyles.Bold);
        }

        TextMeshProUGUI Text(string value, Transform parent, Vector2 anchor, Vector2 offset, float size, Color color, float width, FontStyles style)
        {
            var go = new GameObject("Text " + value, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = offset; rt.sizeDelta = new Vector2(width, Mathf.Max(40, size * 2.4f));
            var t = go.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.text = value; t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = offset; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = color; img.raycastTarget = false;
            return rt;
        }

        void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

        Sprite MakeSolidSprite(string name, Color color, int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name };
            var px = new Color[size * size]; for (int i = 0; i < px.Length; i++) px[i] = color; t.SetPixels(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        }

        Sprite MakeCircleSprite(string name, bool ring)
        {
            const int s = 96; var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { name = name };
            var p = new Color32[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float dx = (x + .5f) / s * 2f - 1f, dy = (y + .5f) / s * 2f - 1f, d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = ring ? Mathf.Clamp01(1f - Mathf.Abs(d - .82f) * 25f) : Mathf.Clamp01((1f - d) * 12f);
                p[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(p); t.Apply(false, true); return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(.5f, .5f), s);
        }

        int SafeBest() { try { return PlayerPrefs.GetInt("beol-sodong.best-first", 0); } catch (Exception) { return 0; } }

        // ───────────── 화면/배치 ─────────────
        void SetScreen()
        {
            titleUi.SetActive(flow == Flow.Title || flow == Flow.Transition);
            gameUi.SetActive(flow == Flow.Practice || flow == Flow.Playing || flow == Flow.Bridge || flow == Flow.Reveal);
            endUi.SetActive(flow == Flow.End);
            for (int i = 0; i < 9; i++) numberRt[i].gameObject.SetActive(gameUi.activeSelf);
            RefreshHud(); RefreshProblemText();
        }

        void ApplyResponsiveLayout()
        {
            // Screen.width는 WebGL DPR이 곱해진 backing-store 폭일 수 있다. CanvasScaler가 만든
            // 실제 논리 사각형을 기준으로 판형을 고르면 820×1180 DPR2가 와이드로 오판되지 않는다.
            Rect logicalRect = ((RectTransform)gameUi.transform).rect;
            Vector2 logical = new Vector2(Mathf.Abs(logicalRect.width), Mathf.Abs(logicalRect.height));
            bool nextWide = logical.x > logical.y * 1.08f;
            wide = nextWide;
            float aspect = Mathf.Max(.35f, logical.x / Mathf.Max(1f, logical.y));
            cam.orthographicSize = wide ? Mathf.Lerp(6.3f, 5.85f, Mathf.InverseLerp(1.35f, 2.05f, aspect)) : Mathf.Lerp(9.25f, 7.65f, Mathf.InverseLerp(.46f, .72f, aspect));
            Vector3 look = wide ? new Vector3(-.15f, 3.45f, -.1f) : new Vector3(-.15f, 3.35f, -.1f);
            cam.transform.position = wide ? new Vector3(10.8f, 7.5f, -18.8f) : new Vector3(8.9f, 8.1f, -18.4f);
            cam.transform.LookAt(look); cameraBasePosition = cam.transform.position;

            if (gameUi.activeSelf)
            {
                float sf = Mathf.Max(.01f, canvas.scaleFactor);
                for (int i = 0; i < 9; i++)
                {
                    Vector3 s = cam.WorldToScreenPoint(bees[i].position + Vector3.up * 1.52f);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)gameUi.transform, s, null, out Vector2 local);
                    numberRt[i].anchoredPosition = local / sf * sf; // 캔버스 로컬 좌표를 그대로 사용
                    numberRt[i].sizeDelta = wide ? new Vector2(48, 39) : new Vector2(62, 50);
                    numberTxt[i].fontSize = wide ? 21 : 26;
                    numberTxt[i].rectTransform.sizeDelta = wide ? new Vector2(44, 36) : new Vector2(58, 44);
                    numberRt[i].localScale = Vector3.one;
                }
                hudLeftRt.anchorMin = hudLeftRt.anchorMax = wide ? new Vector2(0, .5f) : new Vector2(.5f, 1f);
                hudLeftRt.pivot = wide ? new Vector2(0, .5f) : new Vector2(.5f, .5f);
                hudLeftRt.anchoredPosition = wide ? new Vector2(10, 20) : new Vector2(0, -45);
                hudLeftRt.sizeDelta = wide ? new Vector2(196, 166) : new Vector2(Mathf.Min(360, logical.x - 18), 80);
                hudRightRt.anchorMin = hudRightRt.anchorMax = wide ? new Vector2(1, .5f) : new Vector2(.5f, 0);
                hudRightRt.pivot = wide ? new Vector2(1, .5f) : new Vector2(.5f, .5f);
                hudRightRt.anchoredPosition = wide ? new Vector2(-10, -18) : new Vector2(0, 49);
                hudRightRt.sizeDelta = wide ? new Vector2(196, 166) : new Vector2(Mathf.Min(382, logical.x - 8), 88);
                cancelRt.anchorMin = cancelRt.anchorMax = wide ? new Vector2(.84f, .08f) : new Vector2(.5f, 0);
                cancelRt.anchoredPosition = wide ? new Vector2(0, 2) : new Vector2(0, 101);

                float portraitGoalWidth = logical.x < 420f ? Mathf.Max(246f, logical.x - 82f) : Mathf.Min(338f, logical.x - 72f);
                SetTextBox(goalTxt, wide ? 176 : portraitGoalWidth, wide ? 11 : (logical.x < 420f ? 11.5f : 13), wide ? 62 : (logical.x < 420f ? 58 : 44), true);
                SetTextBox(statusTxt, wide ? 176 : Mathf.Min(338, logical.x - 32), wide ? 9.5f : 12, wide ? 58 : 40, wide);
                goalTxt.rectTransform.anchoredPosition = wide ? new Vector2(0, 35) : Vector2.zero;
                statusTxt.rectTransform.anchoredPosition = wide ? new Vector2(0, -28) : Vector2.zero;
                goalTxt.rectTransform.anchorMin = goalTxt.rectTransform.anchorMax = !wide && logical.x < 420f ? new Vector2(.45f, .72f) : new Vector2(.5f, .72f);
                statusTxt.rectTransform.anchorMin = statusTxt.rectTransform.anchorMax = wide ? new Vector2(.5f, .32f) : new Vector2(.5f, .27f);
                SetTextBox(definitionTxt, wide ? 176 : Mathf.Min(360, logical.x - 28), wide ? 10.5f : 13, wide ? 58 : 40, wide);
                SetTextBox(promptTxt, wide ? 176 : Mathf.Min(356, logical.x - 30), wide ? 10.5f : 13, wide ? 78 : 44, true);
                definitionTxt.rectTransform.anchoredPosition = wide ? new Vector2(0, 36) : Vector2.zero;
                promptTxt.rectTransform.anchoredPosition = wide ? new Vector2(0, -20) : Vector2.zero;
            }
            if (titleUi.activeSelf)
            {
                logoTxt.rectTransform.anchorMin = logoTxt.rectTransform.anchorMax = wide ? new Vector2(.25f, .82f) : new Vector2(.5f, .83f);
                titleSubTxt.rectTransform.anchorMin = titleSubTxt.rectTransform.anchorMax = wide ? new Vector2(.25f, .735f) : new Vector2(.5f, .755f);
                titlePlateRt.anchorMin = titlePlateRt.anchorMax = wide ? new Vector2(.73f, .48f) : new Vector2(.5f, .44f);
                titleBadgeTxt.rectTransform.anchorMin = titleBadgeTxt.rectTransform.anchorMax = wide ? new Vector2(.28f, .12f) : new Vector2(.5f, .10f);
            }
        }

        void SetTextBox(TextMeshProUGUI text, float width, float fontSize, float height, bool wrap)
        {
            text.fontSize = fontSize;
            text.rectTransform.sizeDelta = new Vector2(width, height);
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.enableAutoSizing = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        void RefreshHud()
        {
            if (!statusTxt) return;
            int shownScore = Mathf.RoundToInt(displayedScore);
            string time = Mathf.CeilToInt(timeLeft).ToString();
            statusTxt.text = "예비 " + st.lives + "   현상 " + st.solved + "/9   첫 제출 " + st.firstAttemptCorrect + "/" + Mathf.Max(st.firstAttemptTotal, st.solved) + " · 통과 7   " + time + "초   " + shownScore + "점";
            for (int i = 0; i < 3; i++) if (lifeIcons[i]) lifeIcons[i].color = i < st.lives ? Silver : new Color(WrongMilk.r, WrongMilk.g, WrongMilk.b, .28f);
            for (int i = 0; i < 9; i++) if (photoIcons[i]) photoIcons[i].color = i < st.solved ? Silver : new Color(Silver.r, Silver.g, Silver.b, .22f);
        }

        void RefreshProblemText()
        {
            if (!promptTxt) return;
            if (flow == Flow.Practice) { goalTxt.text = "연습 · 숫자 2인 벌 세 마리를 한 획으로 이으시오"; promptTxt.text = "꽃 방문 횟수(회) · 2가 가장 많이 나타난다"; }
            else if (flow == Flow.Bridge) { goalTxt.text = "공동 최빈값 · 4와 7인 벌을 모두 이으시오"; promptTxt.text = "최빈값은 여러 개일 수 있다"; }
            else { goalTxt.text = "꽃 방문 횟수의 최빈값을 구하고, 그 벌을 모두 이으시오"; promptTxt.text = current == null ? "" : "자료(회) · " + string.Join("  ", current.values); }
            if (current != null) for (int i = 0; i < 9; i++) numberTxt[i].text = current.values[i].ToString();
        }

        // ───────────── 입력 좌표/획 ─────────────
        Vector2 BeeScreen(int index) => cam.WorldToScreenPoint(bees[index].position + Vector3.up * .75f);
        float BeeHitRadius()
        {
            float r = Mathf.Clamp(Screen.width * .072f, 28f, wide ? 48f : 42f);
            // 연습은 규칙 이해가 목적이다. 비대상 벌을 스치는 정밀도 시험이 되지 않게 한다.
            return flow == Flow.Practice ? r * .72f : r;
        }
        int HitBee(Vector2 screen)
        {
            float r = BeeHitRadius(); int best = -1; float dBest = r;
            for (int i = 0; i < 9; i++) { float d = Vector2.Distance(screen, BeeScreen(i)); if (d <= dBest) { dBest = d; best = i; } }
            return best;
        }
        float UiPx() => Mathf.Max(.75f, canvas ? canvas.scaleFactor : 1f);
        bool IsCancelZone(Vector2 p) => p.y <= Mathf.Max(68f, Screen.height * .13f) && Mathf.Abs(p.x - Screen.width * .5f) < Screen.width * .22f;

        bool ScreenToBoard(Vector2 p, out Vector3 world)
        {
            Ray ray = cam.ScreenPointToRay(p); var plane = new Plane(Vector3.forward, new Vector3(0, 0, -1.68f));
            if (plane.Raycast(ray, out float d)) { world = ray.GetPoint(d); return true; }
            world = Vector3.zero; return false;
        }

        void BeginStroke(Vector2 p)
        {
            strokePointCount = 0; revealActive = false; strokeLine.gameObject.SetActive(true); strokeGlow.gameObject.SetActive(true);
            AddStrokePoint(p); guideLine.gameObject.SetActive(false); guideHand.gameObject.SetActive(false); HideGuideMarkers();
        }
        void AddStrokePoint(Vector2 p)
        {
            if (!ScreenToBoard(p, out Vector3 w)) return;
            if (strokePointCount > 0 && Vector3.Distance(strokePoints[strokePointCount - 1], w) < .05f) return;
            if (strokePointCount >= strokePoints.Length)
            {
                for (int i = 1; i < strokePoints.Length; i++) strokePoints[i - 1] = strokePoints[i];
                strokePointCount = strokePoints.Length - 1;
            }
            strokePoints[strokePointCount++] = w;
            strokeLine.positionCount = strokeGlow.positionCount = strokePointCount;
            for (int i = 0; i < strokePointCount; i++) { strokeLine.SetPosition(i, strokePoints[i]); strokeGlow.SetPosition(i, strokePoints[i]); }
        }
        void EndStroke() { }
        void ResetStrokeVisual()
        {
            strokePointCount = 0; strokeLine.positionCount = strokeGlow.positionCount = 0;
            strokeLine.gameObject.SetActive(false); strokeGlow.gameObject.SetActive(false);
            for (int i = 0; i < 9; i++) { numberBg[i].color = WarmWhite; numberTxt[i].color = Ink; bees[i].localScale = Vector3.one; }
        }
        void MarkBeeSelected(int i, int rank)
        {
            numberBg[i].color = new Color(Brass.r, Brass.g, Brass.b, .98f); numberTxt[i].color = WarmWhite;
            MgfFx.Punch(bees[i], .14f, .18f); MgfSfx.Play("tap", .08f + rank * .012f);
        }
        void StrokeSound(bool start) { if (start) MgfSfx.Play("tap", .12f); else MgfSfx.Play("whoosh", .1f); }

        // ───────────── 연출 ─────────────
        void ResetRunVisual()
        {
            for (int i = 0; i < 9; i++)
            {
                if (plates[i]) plates[i].gameObject.SetActive(false);
                if (plateNegatives[i]) plateNegatives[i].gameObject.SetActive(false);
                if (sunBeams[i]) sunBeams[i].gameObject.SetActive(false);
                bees[i].localPosition = beeHome[i]; bees[i].localRotation = Quaternion.identity;
            }
            HideGuideMarkers();
            if (strokeLine) ResetStrokeVisual();
        }
        void ClearPlateRing()
        {
            for (int i = 0; i < 9; i++)
            {
                plates[i].gameObject.SetActive(false);
                plateNegatives[i].gameObject.SetActive(false);
                sunBeams[i].gameObject.SetActive(false);
            }
        }
        void ShowTitleVisual() { titleT = 0f; clockRoot.localRotation = Quaternion.identity; }
        void StartClockOpening() { }
        void ShowProblem(SwarmProblem p, bool enter)
        {
            for (int i = 0; i < 9; i++) { bees[i].localPosition = beeHome[i]; bees[i].localRotation = Quaternion.Euler(0, (i * 37 + (p.values[i] * 13)) % 26 - 13, 0); numberTxt[i].color = Ink; numberBg[i].color = WarmWhite; }
            RefreshProblemText(); if (enter) flashT = .24f;
        }
        void BeginRevealVisual(bool correct, bool practice, bool bridge, SwarmProblem p, int mask, bool first)
        {
            revealActive = true; revealWasCorrect = correct; flashT = correct ? .55f : .18f;
            int[] modes = p.modes;
            string modeText = string.Join(", ", modes);
            if (correct)
            {
                evidenceTxt.text = "최빈값 " + modeText + " · 나타난 횟수 " + p.maxFrequency + "회";
                evidenceT = 1.55f; strokeLine.sharedMaterial = brassLineMat; strokeLine.startWidth = strokeLine.endWidth = .13f;
                int plateIndex = practice || bridge ? Mathf.Min(st.solved, 8) : Mathf.Clamp(st.solved - 1, 0, 8);
                plates[plateIndex].gameObject.SetActive(true); plates[plateIndex].localScale = Vector3.one * .82f;
                plateNegatives[plateIndex].gameObject.SetActive(true);
                sunBeams[plateIndex].gameObject.SetActive(true);
                MgfFx.Glow(plates[plateIndex].position + Vector3.up * .2f, Silver, 10 + Mathf.Min(st.firstAttemptCorrect, 5) * 2, .28f);
                // 정답 사건의 주인공은 은염 음영이 현상되고 파사드의 햇빛 길이 바뀌는 건축 장치다.
                MgfFx.Punch(plates[plateIndex], .12f, .32f);
                MgfSfx.Play("correct", .4f);
            }
            else
            {
                evidenceTxt.text = BeolRules.WrongReason(p, mask) + "\n정답: 최빈값 " + modeText + " · " + p.maxFrequency + "회";
                evidenceT = 1.55f;
                for (int i = 0; i < 9; i++)
                {
                    if ((p.expectedMask & (1 << i)) != 0) numberBg[i].color = new Color(Silver.r, Silver.g, Silver.b, .98f);
                    if ((mask & (1 << i)) != 0) bees[i].localRotation *= Quaternion.Euler(0, 0, i % 2 == 0 ? 13 : -13);
                }
                MgfFx.Shake(cam, .07f + .025f * Mathf.Max(0, 3 - st.lives), .24f); MgfSfx.Play("wrong", .32f);
            }
        }
        void UpdateRevealVisual(float t, bool correct)
        {
            if (!revealActive) return;
            float k = Mathf.Clamp01(t / .55f);
            if (correct)
            {
                float s = k < .65f ? Mathf.Lerp(.82f, 1.04f, k / .65f) : Mathf.Lerp(1.04f, 1f, (k - .65f) / .35f);
                int idx = Mathf.Clamp(st.solved - 1, 0, 8); if (plates[idx].gameObject.activeSelf) plates[idx].localScale = Vector3.one * s;
                if (sunBeams[idx].gameObject.activeSelf)
                {
                    Color c = new Color(1f, .74f, .22f, Mathf.Lerp(.12f, .78f, k));
                    sunBeams[idx].startColor = sunBeams[idx].endColor = c;
                    sunBeams[idx].startWidth = sunBeams[idx].endWidth = Mathf.Lerp(.035f, .16f, k);
                }
            }
        }
        void CompleteRevealVisual(bool correct)
        {
            revealActive = false; strokeLine.startWidth = strokeLine.endWidth = .095f;
            for (int i = 0; i < 9; i++) beeHeads[i].localRotation = Quaternion.identity;
        }
        void ShowEnd(bool clear, string reason)
        {
            endTitleTxt.text = clear ? "꽃시계 현상 완료" : reason == "mastery_impossible" ? "첫 제출 통과 불가" : reason == "time" ? "햇빛이 저물었다" : "예비 건판 소진";
            endStatsTxt.text = "현상 " + st.solved + "/9\n첫 제출 " + st.firstAttemptCorrect + "/9 · 통과 7\n점수 " + st.score;
            float spread = clear ? 1.15f : .96f;
            for (int i = 0; i < 9; i++) if (plates[i].gameObject.activeSelf) plates[i].localPosition *= spread;
        }

        void ReplayGuide(bool strong)
        {
            guideStrong = strong; guideT = 0f;
            if (flow == Flow.Practice || flow == Flow.Bridge)
            {
                List<Vector2> safeRoute = BuildSafeGuideRoute(current.expectedMask);
                var points = new List<Vector3>(safeRoute.Count);
                for (int i = 0; i < safeRoute.Count; i++) if (ScreenToBoard(safeRoute[i], out Vector3 w)) points.Add(w + Vector3.back * .08f);
                guideLine.positionCount = points.Count;
                for (int i = 0; i < points.Count; i++) guideLine.SetPosition(i, points[i]);
                guideLine.startWidth = guideLine.endWidth = strong ? .34f : .26f;
                guideLine.gameObject.SetActive(true); guideHand.gameObject.SetActive(true);
                SetGuideMarkers(safeRoute);
                ShowToast(flow == Flow.Practice ? "주황 출발 링에서 시작해 화살표를 따라 숫자 2인 벌 세 마리를 이으시오." : "4와 7은 각각 3회다. 주황 경로로 두 무리를 모두 이으시오.", strong ? 3f : 2.2f);
            }
            else
            {
                ShowToast("아홉 숫자의 출현 횟수를 먼저 센 뒤, 최빈값의 벌만 이으시오.", 2.1f);
                for (int i = 0; i < 9; i++) MgfFx.Punch(numberRt[i], .035f, .22f + i * .02f);
            }
        }

        void SetGuideMarkers(List<Vector2> route)
        {
            HideGuideMarkers();
            if (route == null || route.Count < 2) return;
            RectTransform board = (RectTransform)gameUi.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(board, route[0], null, out Vector2 startLocal);
            guideStartRing.anchoredPosition = startLocal; guideStartRing.gameObject.SetActive(true);
            for (int i = 0; i < guideArrows.Length; i++)
            {
                float f = (i + 1f) / (guideArrows.Length + 1f) * (route.Count - 1);
                int a = Mathf.Clamp(Mathf.FloorToInt(f), 0, route.Count - 2); float u = f - a;
                Vector2 screen = Vector2.Lerp(route[a], route[a + 1], u);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(board, screen, null, out Vector2 local);
                guideArrows[i].rectTransform.anchoredPosition = local;
                Vector2 delta = route[a + 1] - route[a];
                guideArrows[i].rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                guideArrows[i].gameObject.SetActive(true);
            }
        }

        void HideGuideMarkers()
        {
            if (guideStartRing) guideStartRing.gameObject.SetActive(false);
            for (int i = 0; i < guideArrows.Length; i++) if (guideArrows[i]) guideArrows[i].gameObject.SetActive(false);
        }

        List<Vector2> BuildSafeGuideRoute(int targetMask)
        {
            var targets = new List<int>(6);
            for (int i = 0; i < 9; i++) if ((targetMask & (1 << i)) != 0) targets.Add(i);
            var route = new List<Vector2>(64);
            if (targets.Count == 0) return route;
            route.Add(BeeScreen(targets[0]));
            for (int i = 1; i < targets.Count; i++)
            {
                List<Vector2> leg = FindSafeScreenPath(route[route.Count - 1], BeeScreen(targets[i]), targetMask);
                if (leg.Count < 2) leg = new List<Vector2> { route[route.Count - 1], BeeScreen(targets[i]) };
                else leg = SimplifySafeRoute(leg, targetMask);
                for (int k = 1; k < leg.Count; k++) route.Add(leg[k]);
            }
            return route;
        }

        List<Vector2> FindSafeScreenPath(Vector2 start, Vector2 goal, int targetMask)
        {
            float step = Mathf.Clamp(Screen.width / 27f, 13f, 25f);
            float minX = step, maxX = Screen.width - step;
            float minY = Mathf.Max(step, Screen.height * .16f), maxY = Mathf.Min(Screen.height - step, Screen.height * .84f);
            int cols = Mathf.Max(2, Mathf.FloorToInt((maxX - minX) / step) + 1);
            int rows = Mathf.Max(2, Mathf.FloorToInt((maxY - minY) / step) + 1);
            int total = cols * rows;
            var prev = new int[total]; for (int i = 0; i < total; i++) prev[i] = -2;
            int Cell(Vector2 p)
            {
                int x = Mathf.Clamp(Mathf.RoundToInt((p.x - minX) / step), 0, cols - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt((p.y - minY) / step), 0, rows - 1);
                return y * cols + x;
            }
            Vector2 Point(int key) => new Vector2(minX + (key % cols) * step, minY + (key / cols) * step);
            bool Blocked(int key)
            {
                Vector2 p = Point(key); float clearance = BeeHitRadius() + step * .72f;
                for (int i = 0; i < 9; i++)
                    if ((targetMask & (1 << i)) == 0 && Vector2.Distance(p, BeeScreen(i)) < clearance) return true;
                return false;
            }

            int from = Cell(start), to = Cell(goal); prev[from] = -1;
            var queue = new Queue<int>(total); queue.Enqueue(from);
            int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };
            while (queue.Count > 0 && prev[to] == -2)
            {
                int cur = queue.Dequeue(), cx = cur % cols, cy = cur / cols;
                for (int d = 0; d < dx.Length; d++)
                {
                    int nx = cx + dx[d], ny = cy + dy[d];
                    if (nx < 0 || ny < 0 || nx >= cols || ny >= rows) continue;
                    int key = ny * cols + nx;
                    if (prev[key] != -2 || (key != to && Blocked(key))) continue;
                    prev[key] = cur; queue.Enqueue(key);
                }
            }
            if (prev[to] == -2) return new List<Vector2>();
            var reversed = new List<Vector2>();
            for (int key = to; key != -1; key = prev[key]) reversed.Add(Point(key));
            reversed.Reverse(); reversed[0] = start; reversed[reversed.Count - 1] = goal;
            return reversed;
        }

        List<Vector2> SimplifySafeRoute(List<Vector2> route, int targetMask)
        {
            if (route.Count < 3) return route;
            var result = new List<Vector2> { route[0] };
            int anchor = 0;
            while (anchor < route.Count - 1)
            {
                int far = route.Count - 1;
                for (; far > anchor + 1; far--)
                {
                    bool clear = true; float clearance = BeeHitRadius() + 4f;
                    for (int i = 0; i < 9; i++) if ((targetMask & (1 << i)) == 0 && DistanceToSegment(BeeScreen(i), route[anchor], route[far]) < clearance) { clear = false; break; }
                    if (clear) break;
                }
                result.Add(route[far]); anchor = far;
            }
            return result;
        }
        void SpawnPointerRipple(Vector2 p)
        {
            guideHand.gameObject.SetActive(true); RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)gameUi.transform, p, null, out Vector2 local); guideHand.anchoredPosition = local;
            guideHand.localScale = Vector3.one * .7f; guideT = -.35f;
        }
        void PointNearestBee(Vector2 p)
        {
            int nearest = 0; float best = float.MaxValue; for (int i = 0; i < 9; i++) { float d = Vector2.Distance(p, BeeScreen(i)); if (d < best) { best = d; nearest = i; } }
            guideLine.positionCount = 2; ScreenToBoard(p, out Vector3 a); guideLine.SetPosition(0, a); guideLine.SetPosition(1, bees[nearest].position + Vector3.up * 1.1f); guideLine.gameObject.SetActive(true);
            numberRt[nearest].localRotation = Quaternion.Euler(0, 0, -5f); refuseT = .35f;
        }
        void PointTitlePlate() { MgfFx.Punch(titlePlateRt, .12f, .28f); }
        void PressTitlePlate() { titlePlateRt.localScale = Vector3.one * .9f; MgfSfx.Play("tap", .16f); }
        void ReleaseTitlePlate() { titlePlateRt.localScale = Vector3.one; }
        bool HitTitlePlate(Vector2 p) => RectTransformUtility.RectangleContainsScreenPoint(titlePlateRt, p, null);
        bool HitRestart(Vector2 p) => RectTransformUtility.RectangleContainsScreenPoint(restartRt, p, null);
        void PulseRestart() { MgfFx.Punch(restartRt, .12f, .28f); }
        void Refuse(string message) { ShowToast(message, 2.2f); refuseT = .35f; MgfSfx.Play("wrong", .12f); }
        void ShowToast(string message, float seconds) { toastMessage = message; toastTxt.text = message; toastT = seconds; }

        void UpdateWorld(float dt, float raw)
        {
            ambientT += raw; titleT += raw;
            float open = flow == Flow.Transition ? Mathf.Sin(Mathf.Clamp01(transitionT / .52f) * Mathf.PI) * .055f : 0f;
            clockRoot.localPosition = new Vector3(0, open, 0);
            plateRoot.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(ambientT * .55f) * .45f);
            for (int i = 0; i < 9; i++)
            {
                bool touched = (selectedMask & (1 << i)) != 0;
                float flap = touched && drawing ? .25f : Mathf.Sin(ambientT * (5.1f + i * .08f) + beePhase[i]) * 19f;
                leftWings[i].localRotation = Quaternion.Euler(0, -17f, -24f - flap);
                rightWings[i].localRotation = Quaternion.Euler(0, 17f, 24f + flap);
                if (!revealActive) bees[i].localPosition = beeHome[i] + Vector3.up * (Mathf.Sin(ambientT * 1.55f + beePhase[i]) * .035f);
            }
            if (sun) sun.intensity = 1.27f + Mathf.Sin(ambientT * .4f) * .05f;
        }

        void UpdateUi(float raw)
        {
            displayedScore = Mathf.MoveTowards(displayedScore, st.score, raw * Mathf.Max(100f, Mathf.Abs(st.score - displayedScore) * 5f));
            if (toastT > 0f) { toastT -= raw; toastTxt.color = new Color(WarmWhite.r, WarmWhite.g, WarmWhite.b, Mathf.Clamp01(toastT * 2f)); }
            else toastTxt.text = "";
            if (evidenceT > 0f) { evidenceT -= raw; evidenceTxt.color = new Color(Ink.r, Ink.g, Ink.b, Mathf.Clamp01(evidenceT * 2f)); }
            else evidenceTxt.text = "";
            if (flashT > 0f) flashT -= raw;
            screenFlash.color = new Color(1f, .95f, .76f, Mathf.Clamp01(flashT) * .22f);

            if (guideHand.gameObject.activeSelf)
            {
                guideT += raw;
                if ((flow == Flow.Practice || flow == Flow.Bridge) && guideLine.gameObject.activeSelf && guideLine.positionCount > 1)
                {
                    float f = Mathf.Repeat(guideT / 1.2f, 1f) * (guideLine.positionCount - 1);
                    int a = Mathf.Clamp(Mathf.FloorToInt(f), 0, guideLine.positionCount - 2); float u = f - a;
                    Vector3 wp = Vector3.Lerp(guideLine.GetPosition(a), guideLine.GetPosition(a + 1), u);
                    Vector3 sp = cam.WorldToScreenPoint(wp); RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)gameUi.transform, sp, null, out Vector2 lp); guideHand.anchoredPosition = lp;
                    float pulse = 1f + Mathf.Sin(guideT * 8f) * (guideStrong ? .18f : .1f); guideHand.localScale = Vector3.one * pulse;
                }
                else if (guideT > .5f) guideHand.gameObject.SetActive(false);
            }
            if (guideStartRing && guideStartRing.gameObject.activeSelf)
            {
                float pulse = 1f + Mathf.Sin(ambientT * 6.5f) * .13f;
                guideStartRing.localScale = Vector3.one * pulse;
                for (int i = 0; i < guideArrows.Length; i++) if (guideArrows[i])
                    guideArrows[i].color = new Color(Brass.r, Brass.g, Brass.b, .72f + .28f * Mathf.Sin(ambientT * 5f + i));
            }
            if (refuseT > 0f)
            {
                refuseT -= raw; float r = Mathf.Sin(refuseT * 48f) * refuseT * 10f;
                for (int i = 0; i < 9; i++) if (numberRt[i].localRotation != Quaternion.identity) numberRt[i].localRotation = Quaternion.Euler(0, 0, r);
                if (refuseT <= 0f) for (int i = 0; i < 9; i++) numberRt[i].localRotation = Quaternion.identity;
            }
            float tp = 1f + Mathf.Sin(titleT * 2.4f) * .025f; if (titleUi.activeSelf) titlePlateRt.localScale = titlePressed ? Vector3.one * .9f : Vector3.one * tp;
            RefreshHud();
        }
    }
}
