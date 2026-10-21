// 지붕 갈라 — 코드 부트스트랩 2.5D 고양이 접이집 + 왜곡 없는 정면 작도 UI.
using System;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.JibungGalla
{
    /// <summary>학생의 실제 절단선으로 두 도자 조각을 나누어 그리는 런타임 UI 메시.</summary>
    public sealed class CeramicRoofGraphic : MaskableGraphic
    {
        public int HalfAngle = 30;
        public int CutAngle = 30;
        public float Split;
        public float Flash;

        public void Configure(int halfAngle, int cutAngle, float split, float flash)
        {
            HalfAngle = halfAngle; CutAngle = cutAngle; Split = split; Flash = flash;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            float pad = 8f;
            Vector2 a = new Vector2(0f, r.yMax - pad);
            Vector2 b = new Vector2(r.xMin + pad, r.yMin + pad);
            Vector2 c = new Vector2(r.xMax - pad, r.yMin + pad);
            int cut = Mathf.Clamp(CutAngle, 1, HalfAngle * 2 - 1);
            float ray = (-90f - HalfAngle + cut) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ray), Mathf.Sin(ray));
            float travel = (b.y - a.y) / Mathf.Min(-.001f, dir.y);
            Vector2 d = a + dir * travel;
            d.x = Mathf.Clamp(d.x, b.x, c.x);
            Color baseColor = Color.Lerp(color, Color.white, Mathf.Clamp01(Flash) * .36f);
            Color left = Color.Lerp(baseColor, new Color(.14f, .24f, .55f, 1f), .12f);
            Color right = Color.Lerp(baseColor, new Color(.32f, .52f, 1f, 1f), .12f);
            Vector2 lo = new Vector2(-Split, Split * .08f);
            Vector2 ro = new Vector2(Split, -Split * .04f);
            AddTri(vh, a + lo, b + lo, d + lo, left);
            AddTri(vh, a + ro, d + ro, c + ro, right);
        }

        static void AddTri(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, col, new Vector2(.5f, 1));
            vh.AddVert(b, col, Vector2.zero);
            vh.AddVert(c, col, Vector2.one);
            vh.AddTriangle(i, i + 1, i + 2);
        }
    }

    public sealed class JibungView
    {
        readonly JibungGallaGame game;
        readonly Color cobalt = MgfLook.Hex("#315ED0");
        readonly Color cobaltDeep = MgfLook.Hex("#183474");
        readonly Color ivory = MgfLook.Hex("#F1E9D6");
        readonly Color amber = MgfLook.Hex("#E6A249");
        readonly Color ink = MgfLook.Hex("#202A38");
        readonly Color sage = MgfLook.Hex("#86B9AD");
        readonly Color coral = MgfLook.Hex("#D9584B");
        Camera cam;
        Transform world, titleHouse, lift, titleCat;
        readonly List<Transform> cats = new List<Transform>();
        readonly List<Transform> tails = new List<Transform>();
        readonly List<Transform> roomDoors = new List<Transform>();
        readonly List<Transform> windowProps = new List<Transform>();
        float worldClock;

        RectTransform canvasRt, roofRt, titleRoofRt, titleStartRt, restartRt, promptRt, goalRt;
        CanvasGroup titleGroup, playGroup, revealGroup, endGroup, guideGroup;
        CeramicRoofGraphic roof, titleRoof;
        Image apexHandle, apexRing, dragLine, errorLine, ripple, toastBack;
        readonly Image[] edgeLines = new Image[3];
        readonly Image[] equalTicks = new Image[4];
        readonly Image[] trail = new Image[5];
        readonly Image[] guideDots = new Image[8];
        readonly Image[] belts = new Image[3];
        readonly Image[] roomPips = new Image[10];
        readonly Image[] ceramicGlints = new Image[12];
        TMP_Text labelA, labelB, labelC, givenText, promptText, angleText, goalText;
        TMP_Text hudProgress, hudFirst, hudTime, hudScore, toastText, revealTitle, revealBody;
        TMP_Text endTitle, endStats, endCounter, titleBest, roomNumber;
        bool wide, revealing, revealCorrect, guideStrong;
        int lastW, lastH, shownLives = -1, shownSolved = -1, shownFirst = -1;
        int shownScore = -1;
        float shownScoreFloat, toastClock, rippleClock, guideClock, revealClock, endClock;
        float roofSide, roofHeight, roofBase, targetSplit, hitStop;
        Vector2 localA, localB, localC, lastDragLocal;
        RoofProblem shownProblem;
        static Sprite softSprite;

        static Sprite SoftSprite
        {
            get
            {
                if (!softSprite) softSprite = Sprite.Create(MgfLook.SoftDot, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64f);
                return softSprite;
            }
        }

        public JibungView(JibungGallaGame game)
        {
            this.game = game;
            BuildWorld();
            BuildUi();
            Relayout(true);
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#BFD4FF"), MgfLook.Hex("#F1E9D6"), MgfLook.Hex("#D9BD82"), .82f);
            MgfLook.Sun(new Vector3(42, -34, -18), new Color(1f, .84f, .58f), 1.22f, .38f);
            cam = MgfLook.Camera(new Vector3(0, 8.8f, -20f), new Vector3(0, 2.8f, 2.6f), 34f);
            cam.orthographic = true; cam.orthographicSize = 8.4f; cam.backgroundColor = MgfLook.Hex("#D9E4F5");
            world = new GameObject("CobaltCeramicCatFoldhouse").transform;
            var back = MgfLook.Prim(PrimitiveType.Quad, "IvoryDayBackdrop", new Vector3(0, 7.5f, 15f), new Vector3(40, 24, 1), MgfLook.Unlit(MgfLook.Hex("#E8E1CF")), world, false);
            back.transform.rotation = Quaternion.Euler(0, 180, 0);
            var blue = MgfLook.Lit(cobalt, .78f, .08f, MgfLook.Hex("#0B1E55"));
            var pale = MgfLook.Lit(ivory, .42f, .01f);
            var wood = MgfLook.Lit(MgfLook.Hex("#C88D49"), .3f, 0f);
            var mint = MgfLook.Lit(sage, .52f, .02f);
            var dark = MgfLook.Lit(ink, .24f, .02f);

            MgfLook.Block("HouseFoundation", new Vector3(0, -.3f, 4.5f), new Vector3(18.6f, .7f, 8.7f), .55f, pale, world);
            MgfLook.Block("CobaltBackWall", new Vector3(0, 4.8f, 8.0f), new Vector3(18.2f, 10.2f, .7f), .45f, blue, world);
            for (int i = 0; i < 10; i++)
            {
                int row = i / 5, col = i % 5;
                float x = -6.2f + col * 3.1f;
                float y = 1.25f + row * 3.6f;
                var room = new GameObject("CutawayRoom" + (i + 1)).transform; room.SetParent(world, false); room.localPosition = new Vector3(x, y, 5.35f - row * .15f);
                MgfLook.Block("IvoryRoom", Vector3.zero, new Vector3(2.75f, 2.75f, 2.8f), .22f, pale, room);
                var door = MgfLook.Block("CobaltFoldDoor", new Vector3(0, 0, -1.48f), new Vector3(2.55f, 2.55f, .14f), .18f, blue, room).transform;
                roomDoors.Add(door);
                MgfLook.Block("WindowShelf", new Vector3(0, -.82f, -1.62f), new Vector3(2.3f, .15f, .28f), .05f, wood, room);
                MgfLook.Block("Bed", new Vector3(-.55f, -.68f, -.98f), new Vector3(1.0f, .28f, .8f), .14f, i % 2 == 0 ? mint : wood, room);
                MgfLook.Prim(PrimitiveType.Cylinder, "WaterBowl", new Vector3(.68f, -.72f, -1.18f), new Vector3(.28f, .09f, .28f), dark, room, false);
                var prop = MgfLook.Prim(PrimitiveType.Capsule, "WindowCloth", new Vector3(.72f, .13f, -1.63f), new Vector3(.12f, .36f, .08f), i % 2 == 0 ? wood : mint, room, false).transform;
                prop.localRotation = Quaternion.Euler(0, 0, 18); windowProps.Add(prop);
                var cat = BuildCat(room, new Vector3(.1f, -.28f, -1.62f), .72f, i);
                cats.Add(cat); tails.Add(cat.Find("Tail")); cat.gameObject.SetActive(i < 3);
            }
            for (int col = 0; col < 5; col++)
            {
                float x = -6.2f + col * 3.1f;
                var cap = MgfLook.Block("GlazedRoofCap", new Vector3(x, 7.25f, 5.35f), new Vector3(2.86f, .32f, 3.1f), .15f, blue, world);
                cap.transform.rotation = Quaternion.Euler(0, 0, col % 2 == 0 ? -3 : 3);
            }
            lift = new GameObject("RoofElevator").transform; lift.SetParent(world, false); lift.localPosition = new Vector3(7.65f, 2.1f, 2.1f);
            MgfLook.Block("LiftCar", Vector3.zero, new Vector3(2.1f, .35f, 2.2f), .18f, wood, lift);
            for (int side = -1; side <= 1; side += 2)
            {
                MgfLook.Prim(PrimitiveType.Cylinder, "LiftCable", new Vector3(side * .75f, 2.9f, .4f), new Vector3(.045f, 3.1f, .045f), dark, lift, false);
                MgfLook.Prim(PrimitiveType.Cylinder, "Pulley", new Vector3(side * .75f, 5.95f, .4f), new Vector3(.34f, .12f, .34f), wood, lift, false).transform.rotation = Quaternion.Euler(90, 0, 0);
            }
            titleHouse = world;
            titleCat = cats[0];
        }

        Transform BuildCat(Transform parent, Vector3 pos, float scale, int variant)
        {
            var root = new GameObject("PlumpWindowCat").transform; root.SetParent(parent, false); root.localPosition = pos; root.localScale = Vector3.one * scale;
            var fur = MgfLook.Lit(variant % 3 == 0 ? ivory : amber, .58f, .02f);
            var accent = MgfLook.Lit(variant % 3 == 0 ? amber : ivory, .62f, .02f);
            var eye = MgfLook.Lit(ink, .72f, .02f);
            MgfLook.Prim(PrimitiveType.Sphere, "RoundBody", new Vector3(0, .34f, 0), new Vector3(1.05f, .92f, .72f), fur, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0, 1.0f, -.08f), new Vector3(.82f, .7f, .67f), fur, root, false);
            for (int side = -1; side <= 1; side += 2)
            {
                var ear = MgfLook.Block("AmberEar", new Vector3(side * .27f, 1.43f, -.04f), new Vector3(.28f, .38f, .22f), .08f, accent, root);
                ear.transform.rotation = Quaternion.Euler(0, 0, side * -18f);
                MgfLook.Prim(PrimitiveType.Sphere, "Eye", new Vector3(side * .2f, 1.08f, -.39f), Vector3.one * .105f, eye, root, false);
                MgfLook.Prim(PrimitiveType.Sphere, "BigPaw", new Vector3(side * .32f, .04f, -.34f), new Vector3(.38f, .25f, .3f), accent, root, false);
            }
            MgfLook.Prim(PrimitiveType.Sphere, "Nose", new Vector3(0, .91f, -.48f), Vector3.one * .09f, accent, root, false);
            var tail = MgfLook.Prim(PrimitiveType.Capsule, "Tail", new Vector3(.62f, .43f, .1f), new Vector3(.16f, .62f, .16f), accent, root, false).transform;
            tail.localRotation = Quaternion.Euler(0, 0, -48f);
            return root;
        }

        void BuildUi()
        {
            canvasRt = (RectTransform)MgfText.Canvas.transform;
            titleGroup = Group("TitleUI"); playGroup = Group("PlayUI"); revealGroup = Group("RevealUI"); endGroup = Group("EndUI"); guideGroup = Group("GuideUI");
            BuildTitleUi(); BuildPlayUi(); BuildRevealUi(); BuildEndUi(); BuildFeedbackUi();
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
            var im = go.GetComponent<Image>(); im.color = color; im.raycastTarget = false; if (soft) im.sprite = SoftSprite;
            return im;
        }

        TMP_Text Txt(string name, Transform parent, string text, float size, Color color, Vector2 box, Vector2 anchor, Vector2 pos, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.sizeDelta = box; rt.anchoredPosition = pos;
            var t = go.GetComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.fontStyle = FontStyles.Bold; t.color = color; t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text;
            return t;
        }

        CeramicRoofGraphic Roof(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer)); go.transform.SetParent(parent, false);
            var g = go.AddComponent<CeramicRoofGraphic>(); g.color = cobalt; g.raycastTarget = false; return g;
        }

        void BuildTitleUi()
        {
            titleRoof = Roof("TitleCeramicRoof", titleGroup.transform); titleRoofRt = titleRoof.rectTransform;
            titleRoofRt.anchorMin = titleRoofRt.anchorMax = new Vector2(.5f, .57f); titleRoofRt.sizeDelta = new Vector2(370, 245);
            titleRoof.Configure(34, 34, 3, 0);
            var shadow = Txt("TitleShadow", titleRoofRt, "지붕\n갈라", 58, new Color(.06f, .09f, .18f, .42f), new Vector2(330, 142), new Vector2(.5f, .58f), new Vector2(4, -4));
            shadow.characterSpacing = -2;
            var logo = Txt("CeramicLogo", titleRoofRt, "지붕\n갈라", 58, ivory, new Vector2(330, 142), new Vector2(.5f, .58f), Vector2.zero);
            logo.characterSpacing = -2; logo.outlineColor = cobaltDeep; logo.outlineWidth = .28f;
            Txt("RoofRidgeTag", titleRoofRt, "지붕을 반으로 갈라라", 15, amber, new Vector2(250, 32), new Vector2(.5f, .14f), Vector2.zero);
            var startOuter = Img("CatRoofStart", titleGroup.transform, cobaltDeep, new Vector2(146, 98), new Vector2(.5f, .26f), Vector2.zero); titleStartRt = startOuter.rectTransform;
            Img("AmberPaw", startOuter.transform, amber, new Vector2(68, 54), new Vector2(.32f, .5f), Vector2.zero, true);
            Txt("StartVerb", startOuter.transform, "지붕\n가르기", 17, ivory, new Vector2(76, 58), new Vector2(.68f, .5f), Vector2.zero);
            Txt("TitleUnit", titleGroup.transform, "중학교 2학년 · 삼각형의 성질", 13, ink, new Vector2(330, 30), new Vector2(.5f, .09f), Vector2.zero);
            titleBest = Txt("TitleBest", titleGroup.transform, "", 12, cobaltDeep, new Vector2(280, 28), new Vector2(.5f, .145f), Vector2.zero);
            var ring = Img("TitleIconRipple", titleGroup.transform, new Color(.93f, .64f, .2f, .42f), new Vector2(160, 112), new Vector2(.5f, .26f), Vector2.zero, true);
            ring.transform.SetAsFirstSibling();
        }

        void BuildPlayUi()
        {
            var hud = Img("IvoryHudBeam", playGroup.transform, new Color(.95f, .91f, .82f, .98f), new Vector2(382, 64), new Vector2(.5f, 1), new Vector2(0, -34));
            hudProgress = Txt("Progress", hud.transform, "지붕 0/10", 13, ink, new Vector2(90, 40), new Vector2(.13f, .5f), Vector2.zero);
            hudFirst = Txt("Mastery", hud.transform, "첫 절단 0/0", 13, ink, new Vector2(110, 40), new Vector2(.4f, .5f), Vector2.zero);
            hudTime = Txt("Time", hud.transform, "110초", 13, ink, new Vector2(70, 40), new Vector2(.66f, .5f), Vector2.zero);
            hudScore = Txt("Score", hud.transform, "0", 14, cobaltDeep, new Vector2(70, 40), new Vector2(.88f, .5f), Vector2.zero);
            for (int i = 0; i < 3; i++) belts[i] = Img("SafetyBelt" + i, hud.transform, i == 0 ? amber : sage, new Vector2(13, 34), new Vector2(.72f + i * .045f, .5f), Vector2.zero, true);
            var goal = Img("PermanentGoal", playGroup.transform, new Color(.12f, .16f, .22f, .92f), new Vector2(376, 40), new Vector2(.5f, 1), new Vector2(0, -86)); goalRt = goal.rectTransform;
            goalText = Txt("GoalText", goal.transform, "꼭지각을 반으로 갈라 방을 여시오", 14, ivory, new Vector2(360, 34), new Vector2(.5f, .5f), Vector2.zero);
            var prompt = Img("PromptIvory", playGroup.transform, new Color(.96f, .92f, .84f, .98f), new Vector2(376, 90), new Vector2(.5f, .82f), Vector2.zero); promptRt = prompt.rectTransform;
            promptText = Txt("Prompt", prompt.transform, "", 15, ink, new Vector2(352, 78), new Vector2(.5f, .5f), Vector2.zero);

            roof = Roof("InteractiveCeramicRoof", playGroup.transform); roofRt = roof.rectTransform;
            roofRt.anchorMin = roofRt.anchorMax = new Vector2(.5f, .47f); roofRt.sizeDelta = new Vector2(350, 230);
            for (int i = 0; i < 3; i++) edgeLines[i] = Img("RoofEdge" + i, roofRt, ivory, new Vector2(100, 4), new Vector2(.5f, .5f), Vector2.zero);
            for (int i = 0; i < 4; i++) equalTicks[i] = Img("EqualSideTick" + i, roofRt, ivory, new Vector2(4, 19), new Vector2(.5f, .5f), Vector2.zero);
            labelA = Txt("A", roofRt, "A", 20, ivory, new Vector2(34, 30), new Vector2(.5f, .5f), Vector2.zero); labelA.gameObject.SetActive(false);
            labelB = Txt("B", roofRt, "B", 20, ink, new Vector2(34, 30), new Vector2(.5f, .5f), Vector2.zero);
            labelC = Txt("C", roofRt, "C", 20, ink, new Vector2(34, 30), new Vector2(.5f, .5f), Vector2.zero);
            givenText = Txt("GivenInk", roofRt, "AB = AC", 15, ink, new Vector2(250, 34), new Vector2(.5f, .5f), Vector2.zero);
            apexRing = Img("ApexRipple", roofRt, new Color(1f, .78f, .24f, .42f), new Vector2(86, 86), new Vector2(.5f, .5f), Vector2.zero, true);
            apexHandle = Img("ApexHandle", roofRt, amber, new Vector2(58, 58), new Vector2(.5f, .5f), Vector2.zero, true);
            Txt("ApexHandleA", apexHandle.transform, "A", 20, ink, new Vector2(44, 36), new Vector2(.5f, .5f), Vector2.zero);
            dragLine = Img("StudentCutTrail", roofRt, ivory, new Vector2(4, 100), new Vector2(.5f, .5f), Vector2.zero); dragLine.gameObject.SetActive(false);
            errorLine = Img("WrongLineAE", roofRt, new Color(.15f, .17f, .2f, .62f), new Vector2(3, 100), new Vector2(.5f, .5f), Vector2.zero); errorLine.gameObject.SetActive(false);
            angleText = Txt("SelectedAngle", roofRt, "", 20, ivory, new Vector2(110, 36), new Vector2(.5f, .5f), Vector2.zero); angleText.gameObject.SetActive(false);
            for (int i = 0; i < trail.Length; i++) { trail[i] = Img("CutAfterimage" + i, roofRt, new Color(1f, .92f, .72f, .15f), new Vector2(2 + i, 90), new Vector2(.5f, .5f), Vector2.zero); trail[i].gameObject.SetActive(false); }
            roomNumber = Txt("RoomNumber", playGroup.transform, "1", 20, cobaltDeep, new Vector2(54, 54), new Vector2(.12f, .19f), Vector2.zero); roomNumber.gameObject.SetActive(false);
            for (int i = 0; i < roomPips.Length; i++) roomPips[i] = Img("RoomPip" + (i + 1), playGroup.transform, new Color(.95f, .91f, .82f, .92f), new Vector2(20, 20), new Vector2(.19f + i * .07f, .08f), Vector2.zero, true);
        }

        void BuildRevealUi()
        {
            var card = Img("FoldEvidence", revealGroup.transform, new Color(.96f, .92f, .84f, .98f), new Vector2(358, 146), new Vector2(.5f, .22f), Vector2.zero);
            revealTitle = Txt("RevealTitle", card.transform, "", 23, cobaltDeep, new Vector2(332, 42), new Vector2(.5f, .74f), Vector2.zero);
            revealBody = Txt("RevealBody", card.transform, "", 15, ink, new Vector2(330, 86), new Vector2(.5f, .36f), Vector2.zero);
        }

        void BuildEndUi()
        {
            var card = Img("OpenedHouseResult", endGroup.transform, new Color(.96f, .92f, .84f, .98f), new Vector2(360, 330), new Vector2(.5f, .5f), Vector2.zero);
            endTitle = Txt("EndTitle", card.transform, "", 30, cobaltDeep, new Vector2(330, 66), new Vector2(.5f, .82f), Vector2.zero);
            endCounter = Txt("ResultCounter", card.transform, "0방", 42, amber, new Vector2(250, 62), new Vector2(.5f, .6f), Vector2.zero);
            endStats = Txt("EndStats", card.transform, "", 17, ink, new Vector2(320, 94), new Vector2(.5f, .38f), Vector2.zero);
            var paw = Img("RestartCatPaw", card.transform, amber, new Vector2(98, 76), new Vector2(.5f, .09f), Vector2.zero, true); restartRt = paw.rectTransform;
            Txt("RestartLabel", paw.transform, "다시\n가르기", 15, ink, new Vector2(86, 58), new Vector2(.5f, .5f), Vector2.zero);
        }

        void BuildFeedbackUi()
        {
            toastBack = Img("RefusalToast", canvasRt, new Color(.12f, .16f, .22f, .94f), new Vector2(350, 54), new Vector2(.5f, .13f), Vector2.zero);
            toastText = Txt("Toast", toastBack.transform, "", 14, ivory, new Vector2(330, 46), new Vector2(.5f, .5f), Vector2.zero); toastBack.gameObject.SetActive(false);
            ripple = Img("InputRipple", canvasRt, new Color(1f, .75f, .25f, 0), new Vector2(62, 62), new Vector2(.5f, .5f), Vector2.zero, true); rippleClock = 0;
            for (int i = 0; i < guideDots.Length; i++) guideDots[i] = Img("GhostCutPath" + i, guideGroup.transform, new Color(1f, .75f, .25f, .78f), new Vector2(12, 12), new Vector2(.5f, .5f), Vector2.zero, true);
            for (int i = 0; i < ceramicGlints.Length; i++) { ceramicGlints[i] = Img("CeramicSeamGlint" + i, canvasRt, new Color(1f, .94f, .74f, 0), new Vector2(10, 10), new Vector2(.5f, .5f), Vector2.zero, true); }
        }

        public void ShowTitle()
        {
            SetGroup(titleGroup, true); SetGroup(playGroup, false); SetGroup(revealGroup, false); SetGroup(endGroup, false); SetGroup(guideGroup, false);
            int best = PlayerPrefs.GetInt("jibung-galla-best", 0); titleBest.text = best > 0 ? "최고 기록 " + best + "점" : "";
            for (int i = 0; i < cats.Count; i++) cats[i].gameObject.SetActive(i < 3);
            shownProblem = null; revealing = false; endClock = 0; targetSplit = 3;
        }

        public void ShowPlay(bool practice, bool fromTest)
        {
            SetGroup(titleGroup, false); SetGroup(playGroup, true); SetGroup(revealGroup, false); SetGroup(endGroup, false); SetGroup(guideGroup, practice && !fromTest);
            goalText.text = practice ? "꼭지각을 반으로 갈라 방을 여시오 · 연습" : "지붕 10장 · 첫 절단 8장 · 안전벨트 3개";
            guideStrong = practice; guideClock = 0; roof.Split = 0; targetSplit = 0; showingResult(false);
            Relayout(true); RefreshHud(true);
        }

        public void LoadProblem(RoofProblem p, bool practice, int rotation)
        {
            shownProblem = p; promptText.text = p.prompt;
            givenText.text = p.band == 1 ? "AB = AC  ·  ∠A = " + p.apex + "°" : p.band == 2 ? "AB = AC  ·  ∠B = " + p.baseAngle + "°" : "AB = AC  ·  ∠B = " + p.baseAngle + "°  ·  ∠BAE = " + p.errorAngle + "°";
            roofRt.localRotation = Quaternion.Euler(0, 0, rotation);
            roof.Configure(p.h, p.h, 0, 0); dragLine.gameObject.SetActive(false); angleText.gameObject.SetActive(false);
            errorLine.gameObject.SetActive(p.band == 3); revealing = false; revealClock = 0; targetSplit = 0;
            Relayout(true); DrawGeometry(); SetOpenRooms(game.State.solved);
            if (practice) { guideStrong = true; guideClock = 0; SetGroup(guideGroup, true); }
            RefreshHud(true);
        }

        public void BeginDrag()
        {
            angleText.gameObject.SetActive(true); dragLine.gameObject.SetActive(true);
            apexHandle.rectTransform.localScale = new Vector3(1.18f, .82f, 1); MgfFx.Punch(apexHandle.transform, .1f, .16f); MgfSfx.Play("tap", .16f);
            guideStrong = false; SetGroup(guideGroup, false);
        }

        public void DragTo(Vector2 screen, int selected)
        {
            if (!ScreenToRoof(screen, out var local)) return;
            lastDragLocal = local; angleText.text = selected + "°"; angleText.rectTransform.anchoredPosition = localA + new Vector2(72, -42);
            SetLine(dragLine.rectTransform, localA, local, 5f);
            for (int i = 0; i < trail.Length; i++)
            {
                trail[i].gameObject.SetActive(true); trail[i].color = new Color(1f, .92f, .72f, .12f + i * .045f);
                Vector2 end = Vector2.Lerp(localA, local, .72f + i * .055f); SetLine(trail[i].rectTransform, localA, end, 2f + i * .5f);
            }
            roof.Configure(shownProblem.h, Mathf.Clamp(selected, 1, shownProblem.apex - 1), roof.Split, roof.Flash);
        }

        public void EndDrag()
        {
            apexHandle.rectTransform.localScale = Vector3.one;
            for (int i = 0; i < trail.Length; i++) trail[i].gameObject.SetActive(false);
        }

        public void Refuse(string message, Vector2 screen, bool pointAtA)
        {
            toastText.text = message; toastBack.gameObject.SetActive(true); toastClock = 2.2f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var local); ripple.rectTransform.anchoredPosition = local;
            rippleClock = .46f; ripple.color = new Color(1f, .75f, .25f, .88f); ripple.rectTransform.localScale = Vector3.one * .45f;
            if (pointAtA) { guideStrong = true; guideClock = 0; SetGroup(guideGroup, true); MgfFx.Punch(apexHandle.transform, .22f, .32f); }
            MgfFx.Punch(toastBack.transform, .08f, .2f); MgfSfx.Play("tap", .1f);
        }

        public void BeginReveal(bool correct, RoofProblem p, int selected, bool first)
        {
            revealing = true; revealCorrect = correct; revealClock = 0; hitStop = correct ? .075f : 0; targetSplit = 8f;
            roof.Configure(p.h, selected, 0, 1); SetGroup(revealGroup, true); SetGroup(guideGroup, false);
            if (correct)
            {
                revealTitle.text = first ? "한 번에 같은 두 조각" : "수리한 이음매가 맞았습니다";
                revealBody.text = p.evidence + "\nBD=DC · AD⊥BC · 두 지붕 조각이 포개집니다.";
                revealTitle.color = cobaltDeep; MgfSfx.Play("correct", .5f);
                Vector3 glow = cam.ScreenToWorldPoint(new Vector3(Screen.width * .5f, Screen.height * .52f, 18f)); MgfFx.Glow(glow, amber, 12 + game.State.combo * 2, .32f);
                roomNumber.text = game.State.solved.ToString(); roomNumber.gameObject.SetActive(true); roomNumber.transform.localScale = Vector3.one * .25f;
                SetOpenRooms(game.State.solved);
            }
            else
            {
                int other = p.apex - selected;
                revealTitle.text = "두 반각이 다릅니다";
                revealBody.text = "∠BAD=" + selected + "°, ∠CAD=" + other + "°\n" + Misconception(game.State.misconceptionId) + "\n정답 상태는 같은 두 각입니다.";
                revealTitle.color = coral; MgfSfx.Play("wrong", .42f); MgfFx.Shake(cam, .075f, .24f);
            }
            MgfFx.Punch(revealTitle.transform, .18f, .3f); SpawnSeamGlints(correct);
            RefreshHud(true);
        }

        string Misconception(string id)
        {
            switch (id)
            {
                case "whole_apex": return "꼭지각 전체를 그대로 쓰지 말고 반으로 나누시오.";
                case "base_as_half": return "밑각과 꼭지각의 반각을 구분하시오.";
                case "one_base_then_half": return "두 밑각이 같다는 성질을 먼저 적용하시오.";
                case "trace_error_line": return "잘못 그은 AE를 따라 긋지 말고 성질로 고치시오.";
                case "cut_too_close_to_ab": return "선이 AB 쪽에 치우쳤습니다.";
                default: return "선이 AC 쪽에 치우쳤습니다.";
            }
        }

        void SpawnSeamGlints(bool correct)
        {
            Vector2 a = ScreenApex(); Vector2 d = ScreenPointLocal(new Vector2(0, localB.y - 14));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, a, null, out var al);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, d, null, out var dl);
            for (int i = 0; i < ceramicGlints.Length; i++)
            {
                float t = (i + 1f) / (ceramicGlints.Length + 1f); var im = ceramicGlints[i];
                im.rectTransform.anchoredPosition = Vector2.Lerp(al, dl, t) + new Vector2((i % 2 == 0 ? -1 : 1) * (correct ? 5 : 12), 0);
                im.rectTransform.localScale = Vector3.one * (correct ? .5f : .8f); im.color = new Color(correct ? 1f : .85f, correct ? .9f : .25f, .25f, .88f);
            }
        }

        public void FinishReveal()
        {
            revealing = false; SetGroup(revealGroup, false); roomNumber.gameObject.SetActive(false); roof.Split = 0; targetSplit = 0; roof.Flash = 0;
            for (int i = 0; i < ceramicGlints.Length; i++) ceramicGlints[i].color = new Color(1, 1, 1, 0);
        }

        public void ShowEnd(bool clear, string reason)
        {
            SetGroup(playGroup, false); SetGroup(revealGroup, false); SetGroup(guideGroup, false); SetGroup(endGroup, true);
            endClock = 0; endTitle.text = clear ? "열 방이 모두 열렸습니다" : reason == "time" ? "절단 시간이 끝났습니다" : reason == "belts" ? "안전벨트를 모두 썼습니다" : "첫 절단 기준에 닿지 못했습니다";
            endStats.text = "지붕 " + game.State.solved + "/10\n첫 절단 " + game.State.firstCorrect + "/" + game.State.firstAttemptCount + " · 안전벨트 " + game.State.lives + "/3\n점수 " + game.State.score;
            endCounter.text = "0방"; SetOpenRooms(game.State.solved);
            if (clear)
            {
                int best = PlayerPrefs.GetInt("jibung-galla-best", 0); if (game.State.score > best) { PlayerPrefs.SetInt("jibung-galla-best", game.State.score); PlayerPrefs.Save(); }
            }
        }

        public bool HitStart(Vector2 p) => Contains(titleStartRt, p, 18) || Contains(titleRoofRt, p, 0);
        public bool HitRestart(Vector2 p) => Contains(restartRt, p, 18);
        public bool HitApex(Vector2 p) => Vector2.Distance(p, ScreenApex()) <= Mathf.Max(38f, Mathf.Min(Screen.width, Screen.height) * .065f);

        public bool SelectionFromScreen(Vector2 screen, out int selected, out bool beyond, out bool inside)
        {
            selected = 0; beyond = false; inside = false;
            if (shownProblem == null || !ScreenToRoof(screen, out var local)) return false;
            Vector2 v = local - localA;
            if (v.sqrMagnitude < 64f) return true;
            float angle = Vector2.SignedAngle(localB - localA, v);
            selected = Mathf.RoundToInt(angle);
            inside = selected > 0 && selected < shownProblem.apex;
            beyond = local.y <= localB.y - 7f;
            return true;
        }

        bool ScreenToRoof(Vector2 screen, out Vector2 local) => RectTransformUtility.ScreenPointToLocalPointInRectangle(roofRt, screen, null, out local);
        Vector2 ScreenApex() => RectTransformUtility.WorldToScreenPoint(null, roofRt.TransformPoint(localA));
        Vector2 ScreenPointLocal(Vector2 local) => RectTransformUtility.WorldToScreenPoint(null, roofRt.TransformPoint(local));

        public void SyncStateGeometry(JibungGallaGame.GameState s)
        {
            if (shownProblem == null || !roofRt.gameObject.activeInHierarchy) return;
            Vector2 a = ScreenApex(), b = ScreenPointLocal(localB), c = ScreenPointLocal(localC);
            s.apexX = Mathf.RoundToInt(a.x); s.apexY = Mathf.RoundToInt(a.y);
            s.bX = Mathf.RoundToInt(b.x); s.bY = Mathf.RoundToInt(b.y);
            s.cX = Mathf.RoundToInt(c.x); s.cY = Mathf.RoundToInt(c.y);
        }

        public void Tick(float dt)
        {
            if (Screen.width != lastW || Screen.height != lastH) Relayout(true);
            worldClock += dt;
            float ambient = Mathf.Sin(worldClock * 1.7f);
            if (titleHouse) titleHouse.localPosition = new Vector3(0, Mathf.Sin(worldClock * .45f) * .03f, 0);
            if (lift) lift.localPosition = new Vector3(7.65f, 2.1f + Mathf.Sin(worldClock * .8f) * .18f, 2.1f);
            for (int i = 0; i < tails.Count; i++) if (tails[i] && tails[i].gameObject.activeInHierarchy) tails[i].localRotation = Quaternion.Euler(0, 0, -48f + Mathf.Sin(worldClock * 2.1f + i) * 12f);
            for (int i = 0; i < cats.Count; i++) if (cats[i].gameObject.activeInHierarchy) cats[i].localScale = Vector3.one * (.72f * (1f + Mathf.Sin(worldClock * 1.4f + i * .7f) * .025f));
            for (int i = 0; i < windowProps.Count; i++) windowProps[i].localRotation = Quaternion.Euler(0, 0, 18f + ambient * (i % 2 == 0 ? 4 : -4));
            if (titleGroup.gameObject.activeSelf)
            {
                titleRoof.Split = 3f + Mathf.Sin(worldClock * 1.6f) * 2f; titleRoof.SetVerticesDirty();
                float p = .98f + Mathf.Sin(worldClock * 3.2f) * .045f; titleStartRt.localScale = new Vector3(p, p, 1);
            }
            if (hitStop > 0) hitStop -= dt;
            else roof.Split = Mathf.Lerp(roof.Split, targetSplit, 1f - Mathf.Exp(-dt * (revealCorrect ? 9f : 15f)));
            roof.Flash = Mathf.MoveTowards(roof.Flash, 0, dt * 2.3f); roof.SetVerticesDirty();
            if (revealing)
            {
                revealClock += dt;
                if (roomNumber.gameObject.activeSelf)
                {
                    float k = Mathf.Clamp01(revealClock / .42f); float spring = 1f + Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k) * .35f;
                    roomNumber.transform.localScale = Vector3.one * Mathf.Lerp(.2f, spring, k);
                }
                for (int i = 0; i < ceramicGlints.Length; i++)
                {
                    var im = ceramicGlints[i]; Color c = im.color; c.a = Mathf.Max(0, c.a - dt * .62f); im.color = c;
                    im.rectTransform.anchoredPosition += new Vector2((i % 2 == 0 ? -1 : 1) * dt * 8f, dt * 18f);
                }
            }
            if (toastClock > 0) { toastClock -= dt; if (toastClock <= 0) toastBack.gameObject.SetActive(false); }
            if (rippleClock > 0)
            {
                rippleClock -= dt; float k = 1f - rippleClock / .46f; ripple.rectTransform.localScale = Vector3.one * Mathf.Lerp(.45f, 1.8f, k); Color c = ripple.color; c.a = 1f - k; ripple.color = c;
            }
            if (guideGroup.gameObject.activeSelf && shownProblem != null)
            {
                guideClock += dt; float pulse = .8f + Mathf.Sin(guideClock * (guideStrong ? 7f : 4f)) * .2f; apexRing.rectTransform.localScale = Vector3.one * pulse;
                Vector2 a = ScreenApex(); float ray = (-90f - shownProblem.h + shownProblem.h + game.State.roofRotation) * Mathf.Deg2Rad;
                Vector2 end = a + new Vector2(Mathf.Cos(ray), Mathf.Sin(ray)) * Mathf.Max(140f, Vector2.Distance(a, ScreenPointLocal(localB)) * 1.18f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, a, null, out var al);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, end, null, out var el);
                float flow = (guideClock * .7f) % 1f;
                for (int i = 0; i < guideDots.Length; i++) guideDots[i].rectTransform.anchoredPosition = Vector2.Lerp(al, el, ((i + flow) % guideDots.Length) / (guideDots.Length - 1f));
            }
            if (endGroup.gameObject.activeSelf)
            {
                endClock += dt; int shown = Mathf.RoundToInt(Mathf.Lerp(0, game.State.solved, Mathf.Clamp01(endClock / 1.15f))); endCounter.text = shown + "방";
            }
            RefreshHud(false); SyncStateGeometry(game.State);
        }

        void RefreshHud(bool force)
        {
            if (!playGroup.gameObject.activeSelf) return;
            if (force || shownSolved != game.State.solved) { shownSolved = game.State.solved; hudProgress.text = "지붕 " + shownSolved + "/10"; for (int i = 0; i < roomPips.Length; i++) roomPips[i].color = i < shownSolved ? amber : new Color(.95f, .91f, .82f, .9f); }
            if (force || shownFirst != game.State.firstCorrect) { shownFirst = game.State.firstCorrect; hudFirst.text = "첫 절단 " + shownFirst + "/" + game.State.firstAttemptCount; MgfFx.Punch(hudFirst.transform, .08f, .18f); }
            int sec = Mathf.CeilToInt(game.TimeLeft); hudTime.text = game.IsPractice ? "연습" : sec + "초";
            if (force || shownLives != game.State.lives) { shownLives = game.State.lives; for (int i = 0; i < belts.Length; i++) belts[i].color = i < shownLives ? amber : new Color(.25f, .27f, .3f, .24f); }
            shownScoreFloat = Mathf.Lerp(shownScoreFloat, game.State.score, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 9f)); int value = Mathf.RoundToInt(shownScoreFloat);
            if (force || value != shownScore) { shownScore = value; hudScore.text = shownScore.ToString(); }
        }

        void SetOpenRooms(int solved)
        {
            for (int i = 0; i < roomDoors.Count; i++)
            {
                bool open = i < solved;
                roomDoors[i].localRotation = Quaternion.Euler(0, open ? -72f : 0, 0);
                cats[i].gameObject.SetActive(open || (titleGroup != null && titleGroup.gameObject.activeSelf && i < 3));
                if (open) cats[i].localPosition = new Vector3(.1f, -.28f, -1.7f);
            }
        }

        void Relayout(bool force)
        {
            lastW = Screen.width; lastH = Screen.height; wide = (Screen.width >= Screen.height && Screen.width >= 640) || Screen.width >= 1024;
            if (cam) { cam.orthographicSize = wide ? 7.2f : 9.7f; cam.transform.position = wide ? new Vector3(0, 8.4f, -20f) : new Vector3(0, 10.2f, -22f); MgfLook.FitWidth(cam, wide ? 34f : 43f, .72f); }
            if (titleRoofRt)
            {
                titleRoofRt.anchorMin = titleRoofRt.anchorMax = wide ? new Vector2(.5f, .6f) : new Vector2(.5f, .58f);
                titleRoofRt.sizeDelta = wide ? new Vector2(520, 300) : new Vector2(370, 245);
            }
            if (!roofRt || shownProblem == null) return;
            roofSide = wide ? 250f : 198f;
            float rad = shownProblem.h * Mathf.Deg2Rad; roofBase = Mathf.Sin(rad) * roofSide; roofHeight = Mathf.Cos(rad) * roofSide;
            roofRt.sizeDelta = new Vector2(roofBase * 2 + 22, roofHeight + 22);
            roofRt.anchorMin = roofRt.anchorMax = wide ? new Vector2(.65f, .49f) : new Vector2(.5f, .36f);
            roofRt.anchoredPosition = wide ? new Vector2(40, -2) : new Vector2(0, -12);
            promptRt.anchorMin = promptRt.anchorMax = wide ? new Vector2(.65f, .84f) : new Vector2(.5f, .78f);
            promptRt.sizeDelta = wide ? new Vector2(650, 80) : new Vector2(376, 94);
            revealGroup.transform.localPosition = wide ? new Vector3(-220, 0, 0) : Vector3.zero;
            DrawGeometry();
        }

        void DrawGeometry()
        {
            if (shownProblem == null) return;
            Rect r = roofRt.rect; localA = new Vector2(0, r.yMax - 8); localB = new Vector2(r.xMin + 8, r.yMin + 8); localC = new Vector2(r.xMax - 8, r.yMin + 8);
            SetLine(edgeLines[0].rectTransform, localA, localB, 4); SetLine(edgeLines[1].rectTransform, localA, localC, 4); SetLine(edgeLines[2].rectTransform, localB, localC, 4);
            labelA.rectTransform.anchoredPosition = localA + new Vector2(0, 26); labelB.rectTransform.anchoredPosition = localB + new Vector2(-14, -18); labelC.rectTransform.anchoredPosition = localC + new Vector2(14, -18);
            apexHandle.rectTransform.anchoredPosition = localA; apexRing.rectTransform.anchoredPosition = localA;
            givenText.rectTransform.anchoredPosition = new Vector2(0, localB.y - 28);
            Vector2 lmid = Vector2.Lerp(localA, localB, .54f), rmid = Vector2.Lerp(localA, localC, .54f);
            PlaceTick(equalTicks[0], lmid, localB - localA, -8); PlaceTick(equalTicks[1], lmid, localB - localA, 8);
            PlaceTick(equalTicks[2], rmid, localC - localA, -8); PlaceTick(equalTicks[3], rmid, localC - localA, 8);
            if (shownProblem.band == 3)
            {
                Vector2 end = RayToBase(shownProblem.errorAngle); SetLine(errorLine.rectTransform, localA, end, 3); errorLine.gameObject.SetActive(true);
            }
            roof.Configure(shownProblem.h, Mathf.Clamp(game.State.selectedAngle == 0 ? shownProblem.h : game.State.selectedAngle, 1, shownProblem.apex - 1), roof.Split, roof.Flash);
            SyncStateGeometry(game.State);
        }

        Vector2 RayToBase(int angle)
        {
            float ray = (-90f - shownProblem.h + angle) * Mathf.Deg2Rad; Vector2 d = new Vector2(Mathf.Cos(ray), Mathf.Sin(ray));
            float t = (localB.y - localA.y) / Mathf.Min(-.001f, d.y); return localA + d * t;
        }

        static void SetLine(RectTransform rt, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 d = b - a; rt.anchoredPosition = (a + b) * .5f; rt.sizeDelta = new Vector2(thickness, d.magnitude); rt.localRotation = Quaternion.Euler(0, 0, -Vector2.SignedAngle(Vector2.up, d));
        }

        static void PlaceTick(Image im, Vector2 center, Vector2 edge, float along)
        {
            Vector2 d = edge.normalized; im.rectTransform.anchoredPosition = center + d * along; im.rectTransform.localRotation = Quaternion.Euler(0, 0, -Vector2.SignedAngle(Vector2.up, new Vector2(-d.y, d.x)));
        }

        static bool Contains(RectTransform rt, Vector2 p, float pad)
        {
            if (!rt || !rt.gameObject.activeInHierarchy) return false;
            Rect r = ScreenRect(rt); r.xMin -= pad; r.xMax += pad; r.yMin -= pad; r.yMax += pad; return r.Contains(p);
        }

        static Rect ScreenRect(RectTransform rt)
        {
            var corners = new Vector3[4]; rt.GetWorldCorners(corners); Vector2 a = RectTransformUtility.WorldToScreenPoint(null, corners[0]), b = RectTransformUtility.WorldToScreenPoint(null, corners[2]); return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }

        static void SetGroup(CanvasGroup g, bool on)
        {
            g.gameObject.SetActive(on); g.alpha = on ? 1 : 0; g.interactable = on; g.blocksRaycasts = false;
        }

        void showingResult(bool on) { roomNumber.gameObject.SetActive(on); }
    }
}
