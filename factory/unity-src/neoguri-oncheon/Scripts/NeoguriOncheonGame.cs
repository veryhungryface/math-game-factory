using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.NeoguriOncheon
{
    /// <summary>
    /// 너구리 온천. 답 입력은 황동 받침을 1~24 눈금으로 끌어 놓는 경로 하나뿐이다.
    /// 표현 함정: 수와 단위는 띄어 쓰며(6 cm), 무게중심은 AG:GD 순서를 명시한다.
    /// 중2 범위이므로 근호와 넓이 문항은 생성하지 않는다.
    /// </summary>
    public sealed class NeoguriOncheonGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int attempt;
            public int currentValue;
            public int pointerVersion;
            public int openedBaths;
            public string problemId = "";
            public string prompt = "";
            public string misconceptionId = "";
            public bool onboarding;
            public bool dragging;
        }

        sealed class Problem
        {
            public string id, prompt, concept, equation;
            public int answer, kind, band, v1, v2, v3;
            public bool asksAG;
            public int wrong1 = -1, wrong2 = -1;
            public string wrong1Id = "", wrong2Id = "";
        }

        enum Mode { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<Problem> bank = new List<Problem>(420);
        readonly List<Problem> deck = new List<Problem>(9);
        readonly System.Random rng = new System.Random(2026100623);
        Mode mode;
        Problem current;
        Camera cam;
        GameObject cover, titleScenery, book, railBed, railTouchZone, slider, water, channel, raccoon, guide, guideHand, guideArrow, goalGhost, diagramRoot, correctionRoot;
        GameObject popupValley, magnifierRoot, dropMarker;
        readonly GameObject[] guideDots = new GameObject[9];
        readonly GameObject[] baths = new GameObject[9];
        readonly GameObject[] tickets = new GameObject[3];
        readonly GameObject[] steamPuffs = new GameObject[12];
        readonly TextMeshPro[] magnifierValues = new TextMeshPro[5];
        readonly Vector3[] bathBase = new Vector3[9];
        // 빈 곳 탭 피드백: 탭 자리의 종이 물결 + 받침까지 이어지는 점선(풀링, 매 탭 할당 없음).
        readonly GameObject[] ripples = new GameObject[4];
        readonly float[] rippleAge = { 9f, 9f, 9f, 9f };
        readonly GameObject[] trailDots = new GameObject[10];
        Vector3 trailFrom;
        float trailAge = 9f;
        int rippleNext, invalidTaps, hudKey = -1, lastHudScore = -1;
        bool rescueArmed, pendingFree, slowSlide;
        float practiceIdle, playPulse, camElev = 30f, diagramT = .53f;
        TextMeshProUGUI titleUi, subtitleUi, promptUi, goalUi, hudUi, valueUi, feedbackUi;
        TextMeshPro valueWorld; // 선택값 꼬리표: 받침 바로 아래 종이에 찍혀 화면비와 무관하게 레일을 따라간다.
        Material ivory, paperPattern, teal, vermilion, navy, brass, wet, darkPaper;
        bool dragging, firstAttempt, revealCorrect, revealPractice, landscapeLayout;
        int selected = 12, deckIndex, runSerial, practiceMisses, lastMagnifierValue = -99;
        float revealClock, sessionLeft, guideClock, guideFlash, guidePersist, waterFlow, scoreShown, raccoonCheer;
        Vector3 guideOrigin;
        Vector2 pointerDownScreen;
        Vector3 downPoint;
        static readonly Vector3 TriA = new Vector3(0, .38f, 2.62f), TriB = new Vector3(-1.75f, .38f, .55f), TriC = new Vector3(1.75f, .38f, .55f);
        const float RailMinX = -2.72f, RailMaxX = 2.72f, SliderY = 0.72f, SliderZ = -1.65f;

        void Awake()
        {
            MgfLook.Quality(28f);
            MgfLook.Sky(MgfLook.Hex("8EB9AC"), MgfLook.Hex("EAD7A8"), MgfLook.Hex("304A55"), .78f);
            MgfLook.Sun(new Vector3(48, -35, -18), MgfLook.Hex("FFF1D0"), 1.25f, .72f);
            cam = MgfLook.Camera(new Vector3(0, 7.6f, -12.2f), new Vector3(0, .55f, .35f), 38f);
            cam.orthographic = true;
            cam.orthographicSize = 7.8f;
            Texture2D paper = Resources.Load<Texture2D>("NeoguriOncheon/paper-textures");
            ivory = MgfLook.Lit(MgfLook.Hex("F4E8CF"), .15f);
            paperPattern = MgfLook.Lit(MgfLook.Hex("F4E8CF"), .15f, 0f, Color.black, paper);
            teal = MgfLook.Lit(MgfLook.Hex("178C8C"), .35f, 0f, MgfLook.Hex("063A3A"));
            vermilion = MgfLook.Lit(MgfLook.Hex("D9533F"), .22f);
            navy = MgfLook.Lit(MgfLook.Hex("2C3550"), .2f);
            brass = MgfLook.Lit(MgfLook.Hex("D2A94B"), .72f, .68f, MgfLook.Hex("382503"));
            wet = MgfLook.Lit(MgfLook.Hex("6C8A8A"), .06f);
            darkPaper = MgfLook.Lit(MgfLook.Hex("635946"), .08f);
            BuildBank();
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void BuildWorld()
        {
            // 화면 전체가 펼친 책과 팝업 계곡이 되도록 18 world-unit 너비의 종이극장을 만든다.
            popupValley = new GameObject("Full width popup valley");
            MgfLook.Block("Theatre backing", new Vector3(0, -.82f, 1.25f), new Vector3(18f, .7f, 9.8f), .28f, navy, popupValley.transform);
            for (int side = -1; side <= 1; side += 2)
            {
                float sx = side * 5.35f;
                var wing = MgfLook.Block(side < 0 ? "Left accordion page" : "Right accordion page", new Vector3(sx, -.34f, .75f), new Vector3(4.15f, .24f, 7.9f), .16f, paperPattern, popupValley.transform);
                wing.transform.rotation = Quaternion.Euler(0, side * -8f, side * 2.2f);
                for (int fold = 0; fold < 4; fold++)
                {
                    float x = sx + side * (-1.24f + fold * .72f);
                    float z = 3.45f - fold * .69f;
                    var cliff = MgfLook.Block("Die cut valley wing", new Vector3(x, .2f + fold * .18f, z), new Vector3(.92f, 2.0f + fold * .42f, .15f), .11f, fold % 3 == 0 ? vermilion : (fold % 3 == 1 ? teal : ivory), popupValley.transform);
                    cliff.transform.rotation = Quaternion.Euler(0, side * 5f, side * (6f - fold));
                    MgfLook.Block("Visible fold tab", new Vector3(x - side * .27f, -.05f, z - .24f), new Vector3(.42f, .08f, .62f), .025f, brass, popupValley.transform);
                    MgfLook.Block("Tiny brass fastener", new Vector3(x - side * .25f, .58f + fold * .18f, z - .1f), new Vector3(.14f, .12f, .14f), .07f, brass, popupValley.transform);
                }
                for (int i = 0; i < 4; i++)
                {
                    float x = sx + side * (-1.05f + i * .66f);
                    float z = .4f + i * .72f;
                    MgfLook.Block("Side bath paper ring", new Vector3(x, .12f + i * .08f, z), new Vector3(.8f, .12f, .8f), .3f, i % 2 == 0 ? teal : vermilion, popupValley.transform);
                    MgfLook.Block("Cellophane side water", new Vector3(x, .21f + i * .08f, z), new Vector3(.55f, .025f, .55f), .25f, MgfLook.Alpha(new Color(.05f, .75f, .78f, .54f)), popupValley.transform);
                }
            }
            // 중앙 책은 얇은 종이 절단면과 접힘 탭이 보이는 실제 조작 무대다.
            book = MgfLook.Block("Open popup book", new Vector3(0, -.52f, .55f), new Vector3(7.8f, .38f, 7.5f), .13f, paperPattern);
            MgfLook.Block("Spine", new Vector3(0, -.22f, .52f), new Vector3(.18f, .25f, 7.2f), .07f, brass);
            for (int i = 0; i < 6; i++)
            {
                float z = 2.95f - i * .47f;
                float w = 6.25f - i * .22f;
                var layer = MgfLook.Block("Die cut cliff " + i, new Vector3((i % 2 == 0 ? -.15f : .15f), -.18f + i * .09f, z), new Vector3(w, .13f, .38f), .05f, i % 3 == 1 ? teal : (i % 3 == 2 ? vermilion : ivory));
                layer.transform.rotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? -1.5f : 1.5f));
            }
            diagramRoot = new GameObject("Problem diagram");
            diagramRoot.transform.position = new Vector3(0, .26f, 0); // 종이 절벽층(.34) 위로 도해 카드를 올려 가리지 않게 한다.
            channel = MgfLook.Block("Folded channel", new Vector3(.85f, .34f, -.42f), new Vector3(2.6f, .16f, .42f), .07f, teal);
            water = MgfLook.Block("Cellophane water", new Vector3(.85f, .46f, -.44f), new Vector3(.18f, .035f, .31f), .015f, MgfLook.Alpha(new Color(.05f, .75f, .78f, .62f)));
            // 원형 종이 탕 9개가 정답마다 솟는다.
            for (int i = 0; i < 9; i++)
            {
                int row = i / 5, col = i % 5;
                float x = -1.6f + col * .68f + row * .34f;
                float z = 3.35f + row * .6f;
                bathBase[i] = new Vector3(x, -.25f, z);
                baths[i] = MgfLook.Block("Paper bath " + i, bathBase[i], new Vector3(.78f, .12f, .78f), .22f, i % 2 == 0 ? vermilion : teal);
            }
            // 종이 인형: 둥근 귀·배·줄무늬 꼬리를 로우폴리 종이 조각으로 표현.
            raccoon = new GameObject("Tani paper raccoon");
            raccoon.transform.position = new Vector3(2.45f, .35f, 3.05f);
            MgfLook.Block("body", Vector3.zero, new Vector3(.72f, 1.02f, .08f), .18f, darkPaper, raccoon.transform);
            MgfLook.Block("apron", new Vector3(0, -.05f, -.07f), new Vector3(.58f, .68f, .035f), .12f, teal, raccoon.transform);
            MgfLook.Block("head", new Vector3(0, .64f, 0), new Vector3(.82f, .76f, .09f), .16f, darkPaper, raccoon.transform);
            MgfLook.Block("earL", new Vector3(-.3f, .98f, .02f), new Vector3(.27f, .27f, .1f), .045f, vermilion, raccoon.transform);
            MgfLook.Block("earR", new Vector3(.3f, .98f, .02f), new Vector3(.27f, .27f, .1f), .045f, vermilion, raccoon.transform);
            MgfLook.Block("tail", new Vector3(.52f, -.05f, .05f), new Vector3(.24f, .78f, .12f), .1f, teal, raccoon.transform).transform.rotation = Quaternion.Euler(0, 0, -35);
            MgfLook.Block("muzzle", new Vector3(0, .55f, -.09f), new Vector3(.34f, .24f, .035f), .1f, ivory, raccoon.transform);
            MgfLook.Block("eyeL", new Vector3(-.18f, .72f, -.105f), new Vector3(.12f, .16f, .025f), .05f, navy, raccoon.transform);
            MgfLook.Block("eyeR", new Vector3(.18f, .72f, -.105f), new Vector3(.12f, .16f, .025f), .05f, navy, raccoon.transform);
            for (int i = 0; i < 4; i++) MgfLook.Block("Tail screenprint stripe", new Vector3(.48f + i * .035f, -.28f + i * .18f, -.02f), new Vector3(.25f, .065f, .14f), .025f, i % 2 == 0 ? ivory : navy, raccoon.transform).transform.rotation = Quaternion.Euler(0, 0, -35);

            // 24칸 실제 입력 레일. 답 위치 색은 모두 동일해 정답을 누설하지 않는다.
            railBed = MgfLook.Block("Rail bed", new Vector3(0, .42f, SliderZ), new Vector3(6.0f, .22f, .72f), .16f, navy);
            railTouchZone = new GameObject("Broad rail touch zone");
            railTouchZone.transform.position = new Vector3(0, .63f, SliderZ);
            var railZoneCollider = railTouchZone.AddComponent<BoxCollider>();
            railZoneCollider.size = new Vector3(7.2f, .9f, 1.75f);
            for (int i = 1; i <= 24; i++)
            {
                float x = XFor(i);
                MgfLook.Block("Tick " + i, new Vector3(x, .59f, SliderZ - .03f), new Vector3(.025f, i % 5 == 0 ? .23f : .14f, .47f), .01f, ivory);
                var t = MgfText.World(i.ToString(), new Vector3(x, .86f, SliderZ - .2f), i < 10 ? 1.9f : 1.5f, MgfLook.Hex("2C3550"));
                t.transform.rotation = Quaternion.Euler(48, 0, 0);
            }
            slider = MgfLook.Block("Paper fastener slider", new Vector3(XFor(selected), SliderY, SliderZ), new Vector3(.72f, .62f, .9f), .18f, vermilion);
            MgfLook.Block("Slider tab", new Vector3(0, .46f, 0), new Vector3(.22f, .72f, .5f), .1f, brass, slider.transform);
            guide = MgfLook.Block("Guide ring", slider.transform.position + Vector3.up * .08f, new Vector3(1.15f, .035f, 1.15f), .015f, MgfLook.Additive(new Color(1f, .82f, .25f, .42f)));
            guideHand = MgfLook.Block("Guide hand", slider.transform.position + new Vector3(0, .9f, 0), new Vector3(.5f, .58f, .12f), .2f, ivory);
            MgfLook.Block("Guide finger", new Vector3(.18f, -.37f, 0), new Vector3(.17f, .55f, .12f), .08f, ivory, guideHand.transform);
            for (int i = 0; i < guideDots.Length; i++)
                guideDots[i] = MgfLook.Block("Guide dot " + i, Vector3.zero, new Vector3(.12f, .05f, .12f), .06f, teal);
            guideArrow = MgfLook.Block("Guide arrow", Vector3.zero, new Vector3(.34f, .06f, .34f), .05f, vermilion);
            guideArrow.transform.rotation = Quaternion.Euler(0, 45, 0);
            goalGhost = MgfLook.Block("Practice ghost", new Vector3(XFor(2), SliderY, SliderZ), new Vector3(.7f, .05f, 1.0f), .18f, MgfLook.Alpha(new Color(1f, .9f, .45f, .38f)));
            dropMarker = MgfLook.Block("Release marker", new Vector3(XFor(2), 1.18f, SliderZ), new Vector3(.46f, .055f, .46f), .2f, brass);
            var dropText = MgfText.World("2 cm\n여기서 놓기", new Vector3(XFor(2) + .75f, 2.05f, SliderZ + .1f), 2.3f, MgfLook.Hex("2C3550"));
            dropText.alignment = TextAlignmentOptions.Center; dropText.transform.rotation = Quaternion.Euler(48, 0, 0); dropText.transform.SetParent(dropMarker.transform, true);

            valueWorld = MgfText.World("12 cm", new Vector3(0, -.2f, SliderZ - .95f), 5.2f, MgfLook.Hex("2C3550"));
            valueWorld.alignment = TextAlignmentOptions.Center; valueWorld.transform.rotation = Quaternion.Euler(48, 0, 0);
            valueWorld.outlineWidth = .12f; valueWorld.outlineColor = MgfLook.Hex("F4E8CF");
            magnifierRoot = new GameObject("Five tick paper magnifier");
            MgfLook.Block("Magnifier paper", Vector3.zero, new Vector3(2.85f, .075f, .83f), .16f, ivory, magnifierRoot.transform);
            MgfLook.Block("Magnifier snap line", new Vector3(0, .09f, 0), new Vector3(.08f, .12f, .92f), .025f, vermilion, magnifierRoot.transform);
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * .52f;
                MgfLook.Block("Magnifier tick", new Vector3(x, .075f, .19f), new Vector3(i == 2 ? .08f : .035f, .07f, .5f), .015f, i == 2 ? vermilion : navy, magnifierRoot.transform);
                magnifierValues[i] = MgfText.World("", new Vector3(x, .12f, -.16f), i == 2 ? 3.0f : 2.1f, MgfLook.Hex(i == 2 ? "D9533F" : "2C3550"), magnifierRoot.transform);
                magnifierValues[i].alignment = TextAlignmentOptions.Center;
                magnifierValues[i].transform.rotation = Quaternion.Euler(48, 0, 0);
            }
            magnifierRoot.SetActive(false);
            magnifierRoot.transform.localScale = Vector3.one * 1.45f;
            for (int i = 0; i < ripples.Length; i++)
            {
                ripples[i] = MgfLook.Block("Tap paper ripple " + i, Vector3.zero, new Vector3(1.2f, .03f, 1.2f), .6f, vermilion);
                MgfLook.Block("Ripple paper hole", new Vector3(0, .02f, 0), new Vector3(.9f, .03f, .9f), .45f, ivory, ripples[i].transform);
                foreach (var c in ripples[i].GetComponentsInChildren<Collider>()) Destroy(c);
                ripples[i].SetActive(false);
            }
            for (int i = 0; i < trailDots.Length; i++)
            {
                trailDots[i] = MgfLook.Block("Tap trail dot " + i, Vector3.zero, new Vector3(.22f, .08f, .22f), .11f, vermilion);
                Destroy(trailDots[i].GetComponent<Collider>());
                trailDots[i].SetActive(false);
            }
            cover = MgfLook.Block("Bathhouse hanging sign", new Vector3(0, 1.48f, 2.28f), new Vector3(4.65f, 1.25f, .18f), .22f, vermilion);
            cover.transform.rotation = Quaternion.Euler(48, 0, 0);
            var coverText = MgfText.World("너구리 온천", new Vector3(0, .05f, -.12f), 7.2f, MgfLook.Hex("F4E8CF"), cover.transform);
            coverText.alignment = TextAlignmentOptions.Center; coverText.outlineWidth = .16f;
            titleScenery = new GameObject("Title popup scenery");
            cover.transform.SetParent(titleScenery.transform, true);
            for (int i = 0; i < 5; i++)
            {
                float x = -2.55f + i * 1.28f;
                float height = i == 2 ? 3.7f : (i % 2 == 0 ? 3.05f : 2.55f);
                var mountain = MgfLook.Block("Tall paper mountain " + i, new Vector3(x, height * .5f - .05f, 3.12f + Mathf.Abs(i - 2) * .08f), new Vector3(1.42f, height, .24f), .17f, i % 2 == 0 ? navy : teal, titleScenery.transform);
                mountain.transform.rotation = Quaternion.Euler(0, 0, (i - 2) * 4f);
            }
            for (int i = 0; i < 7; i++)
            {
                float x = -2.45f + i * .82f;
                var lantern = MgfLook.Block("Bathhouse lantern " + i, new Vector3(x, .88f + (i % 2) * .22f, 2.46f), new Vector3(.3f, .5f, .16f), .1f, vermilion, titleScenery.transform);
                MgfLook.Block("Lantern cap", new Vector3(0, .31f, 0), new Vector3(.38f, .1f, .2f), .04f, brass, lantern.transform);
            }
            for (int i = 0; i < 8; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = .95f + (i / 2) * .54f;
                var tree = MgfLook.Block("Die cut tree " + i, new Vector3(side * (3.3f - (i / 2) * .08f), .18f + (i % 3) * .07f, z), new Vector3(.34f, .72f, .16f), .15f, i % 3 == 0 ? vermilion : navy);
                tree.transform.rotation = Quaternion.Euler(0, 0, side * 8f);
            }
            for (int i = 0; i < steamPuffs.Length; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                steamPuffs[i] = MgfLook.Block("Screenprint steam puff " + i, new Vector3(side * (3.5f + (i % 3) * .75f), .65f + (i % 4) * .52f, 1.0f + (i / 4) * 1.2f), new Vector3(.34f + (i % 3) * .1f, .42f, .055f), .16f, ivory, popupValley.transform);
                steamPuffs[i].transform.rotation = Quaternion.Euler(0, 0, side * (8f + i));
            }
            for (int i = 0; i < 3; i++)
                tickets[i] = MgfLook.Block("Steam ticket " + i, new Vector3(-4.7f + i * .62f, .65f, 3.25f), new Vector3(.46f, .72f, .08f), .05f, vermilion);
        }

        GameObject MakeLine(Vector3 a, Vector3 b, float width, Material mat, string name, Transform parent = null)
        {
            Vector3 d = b - a, m = (a + b) * .5f;
            // parent 가 있으면 a·b 는 부모 기준 좌표다(도해 루트는 회전 없이 위로만 올라가 있다).
            var o = MgfLook.Block(name, m, new Vector3(width, width, d.magnitude), width * .35f, mat, parent);
            o.transform.localRotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            return o;
        }

        void BuildProblemDiagram(Problem p)
        {
            for (int i = diagramRoot.transform.childCount - 1; i >= 0; i--) Destroy(diagramRoot.transform.GetChild(i).gameObject);
            correctionRoot = null;
            diagramRoot.SetActive(true);
            MgfLook.Block("Die cut diagram card", new Vector3(0, .11f, 1.38f), new Vector3(5.85f, .08f, 2.95f), .16f, paperPattern, diagramRoot.transform);
            // 도해 비율을 문항 수치에 맞춘다: AD:DB(또는 AD:AB)의 실제 비, 중점은 1/2. 극단 비는 글자가 겹치지 않게만 묶는다.
            if (p.kind == 0) diagramT = Mathf.Clamp((float)p.v1 / (p.v1 + p.v2), .14f, .88f);
            else if (p.kind == 1) diagramT = Mathf.Clamp((float)p.v1 / p.v2, .14f, .88f);
            else diagramT = .5f;
            if (p.kind == 3) BuildParallelDiagram(p); else BuildTriangleDiagram(p);
        }

        void BuildTriangleDiagram(Problem p)
        {
            Vector3 a = TriA, b = TriB, c = TriC;
            Vector3 d = Vector3.Lerp(a, b, diagramT), e = Vector3.Lerp(a, c, diagramT), mid = (b + c) * .5f;
            // 길이 글자는 변의 바깥 법선 방향으로 띄워 꼭짓점 글자와 겹치지 않게 한다. 크기는 레일 눈금 숫자보다 크다.
            Vector3 nl = new Vector3(-.76f * .35f - .62f, .08f, .65f * .35f), nr = new Vector3(.76f * .35f + .62f, .08f, .65f * .35f);
            const float Num = 2.7f, Ask = 3.1f, Vert = 3.3f;
            MakeLine(a, b, .065f, navy, "AB", diagramRoot.transform);
            MakeLine(a, c, .065f, navy, "AC", diagramRoot.transform);
            MakeLine(b, c, .065f, navy, "BC", diagramRoot.transform);
            DiagramLabel("A", a + new Vector3(0, .08f, .26f), Vert, navy);
            DiagramLabel("B", b + new Vector3(-.2f, .08f, -.12f), Vert, navy);
            DiagramLabel("C", c + new Vector3(.2f, .08f, -.12f), Vert, navy);

            if (p.kind == 0 || p.kind == 1)
            {
                MakeLine(d, e, .085f, teal, "DE parallel", diagramRoot.transform);
                DiagramLabel("D", d + new Vector3(-.22f, .08f, .04f), Vert * .9f, navy);
                DiagramLabel("E", e + new Vector3(.22f, .08f, .04f), Vert * .9f, navy);
                ParallelMarks(d, e);
                if (p.kind == 0)
                {
                    DiagramLabel("AD " + p.v1 + " cm", Vector3.Lerp(a, d, .5f) + nl, Num, navy);
                    DiagramLabel("DB " + p.v2 + " cm", Vector3.Lerp(d, b, .5f) + nl, Num, navy);
                    DiagramLabel("AE " + p.v3 + " cm", Vector3.Lerp(a, e, .5f) + nr, Num, navy);
                    DiagramLabel("EC ?", Vector3.Lerp(e, c, .5f) + nr, Ask, vermilion);
                }
                else
                {
                    DiagramLabel("AD " + p.v1 + " cm", Vector3.Lerp(a, d, .5f) + nl, Num, navy);
                    DiagramLabel("AB " + p.v2 + " cm", b + new Vector3(-.15f, .08f, -.45f), Num, navy);
                    DiagramLabel("BC " + p.v3 + " cm", mid + new Vector3(.55f, .08f, -.45f), Num, navy);
                    DiagramLabel("DE ?", (d + e) * .5f + new Vector3(0, .08f, -.24f), Ask, vermilion);
                }
            }
            else if (p.kind == 2)
            {
                MakeLine(d, e, .085f, teal, "MN midpoint connector", diagramRoot.transform);
                DiagramLabel("M", d + new Vector3(-.24f, .08f, .04f), Vert * .9f, navy);
                DiagramLabel("N", e + new Vector3(.24f, .08f, .04f), Vert * .9f, navy);
                DiagramLabel("MN ?", (d + e) * .5f + new Vector3(0, .08f, .26f), Ask, vermilion);
                DiagramLabel("BC " + p.v1 + " cm", mid + new Vector3(0, .08f, -.42f), Num, navy);
                MidpointMarks(a, d, b); MidpointMarks(a, e, c);
            }
            else
            {
                MakeLine(a, mid, .085f, teal, "Median AD", diagramRoot.transform);
                Vector3 g = Vector3.Lerp(a, mid, 2f / 3f);
                MgfLook.Block("Centroid G", g + Vector3.up * .04f, new Vector3(.18f, .12f, .18f), .09f, vermilion, diagramRoot.transform);
                DiagramLabel("D", mid + new Vector3(-.22f, .08f, -.22f), Vert * .9f, navy);
                DiagramLabel("G", g + new Vector3(.3f, .08f, 0), Vert * .9f, vermilion);
                DiagramLabel("AD " + p.v1 + " cm", Vector3.Lerp(a, mid, .38f) + new Vector3(.95f, .08f, 0), Num, navy);
                DiagramLabel(p.asksAG ? "AG = ?   (AG : GD = 2 : 1)" : "GD = ?   (AG : GD = 2 : 1)", mid + new Vector3(0, .08f, -.5f), Num * .9f, vermilion);
                MidpointMarks(b, mid, c);
            }
        }

        void BuildParallelDiagram(Problem p)
        {
            // l–m, m–n 간격을 한 직선의 두 구간 비 v1:v2에 맞춘다(두 직선 모두 같은 비로 잘린다).
            float ratio = Mathf.Clamp((float)p.v1 / (p.v1 + p.v2), .2f, .8f);
            float[] z = { 2.5f, 2.5f - 2.0f * ratio, .5f };
            for (int i = 0; i < 3; i++)
            {
                MakeLine(new Vector3(-2.3f, .38f, z[i]), new Vector3(2.3f, .38f, z[i]), .075f, i == 1 ? teal : navy, "parallel " + i, diagramRoot.transform);
                DiagramLabel(i == 0 ? "l" : (i == 1 ? "m" : "n"), new Vector3(-2.6f, .46f, z[i]), 3.1f, navy);
            }
            Vector3 a = new Vector3(-1.75f, .4f, 2.7f), b = new Vector3(-1.05f, .4f, .3f);
            Vector3 c = new Vector3(.45f, .4f, 2.7f), d = new Vector3(1.45f, .4f, .3f);
            MakeLine(a, b, .065f, vermilion, "transversal left", diagramRoot.transform);
            MakeLine(c, d, .065f, vermilion, "transversal right", diagramRoot.transform);
            float zTop = (z[0] + z[1]) * .5f, zBottom = (z[1] + z[2]) * .5f;
            // 왼쪽 직선의 구간 글자는 두 직선 사이, 오른쪽 직선의 글자는 바깥에 둔다.
            DiagramLabel(p.v1 + " cm", new Vector3(XOnLine(a, b, zTop) + .62f, .5f, zTop), 2.7f, navy);
            DiagramLabel(p.v2 + " cm", new Vector3(XOnLine(a, b, zBottom) + .62f, .5f, zBottom), 2.7f, navy);
            DiagramLabel(p.v3 + " cm", new Vector3(XOnLine(c, d, zTop) + .68f, .5f, zTop), 2.7f, navy);
            DiagramLabel("x cm", new Vector3(XOnLine(c, d, zBottom) + .64f, .5f, zBottom), 3.1f, vermilion);
        }

        static float XOnLine(Vector3 a, Vector3 b, float z) { return Mathf.Lerp(a.x, b.x, Mathf.InverseLerp(a.z, b.z, z)); }

        void DiagramLabel(string text, Vector3 pos, float size, Material colorMaterial, Transform parent = null)
        {
            Color color = colorMaterial == vermilion ? MgfLook.Hex("D9533F") : MgfLook.Hex("2C3550");
            var label = MgfText.World(text, pos, size, color, parent != null ? parent : diagramRoot.transform);
            label.alignment = TextAlignmentOptions.Center;
            label.outlineWidth = .18f; label.outlineColor = MgfLook.Hex("F4E8CF"); // 무늬 종이 위에서도 글자가 먼저 읽히게
            label.transform.rotation = Quaternion.Euler(48, 0, 0);
        }

        void ParallelMarks(Vector3 d, Vector3 e)
        {
            Vector3 mid = (d + e) * .5f;
            MakeLine(mid + new Vector3(-.12f, .02f, -.1f), mid + new Vector3(-.12f, .02f, .1f), .035f, vermilion, "parallel mark 1", diagramRoot.transform);
            MakeLine(mid + new Vector3(.12f, .02f, -.1f), mid + new Vector3(.12f, .02f, .1f), .035f, vermilion, "parallel mark 2", diagramRoot.transform);
        }

        void MidpointMarks(Vector3 a, Vector3 m, Vector3 b)
        {
            Vector3 p = Vector3.Lerp(a, m, .68f), q = Vector3.Lerp(m, b, .32f);
            MgfLook.Block("midpoint mark", p + Vector3.up * .05f, new Vector3(.11f, .1f, .11f), .04f, vermilion, diagramRoot.transform);
            MgfLook.Block("midpoint mark", q + Vector3.up * .05f, new Vector3(.11f, .1f, .11f), .04f, vermilion, diagramRoot.transform);
        }

        void ShowDiagramCorrection(string misconception)
        {
            if (correctionRoot != null) Destroy(correctionRoot);
            correctionRoot = new GameObject("Ratio correction brackets"); correctionRoot.transform.SetParent(diagramRoot.transform, false);
            Material mat = vermilion;
            if (current.kind == 0 || current.kind == 1)
            {
                Vector3 a = TriA + Vector3.up * .1f, b = TriB + Vector3.up * .1f, d = Vector3.Lerp(a, b, diagramT);
                MakeLine(a, current.kind == 1 ? b : d, .13f, mat, "correct bracket left", correctionRoot.transform);
                DiagramLabel(misconception == "mix_part_and_whole" ? "부분끼리 · 전체끼리 대응" : "같은 위치의 선분을 대응", new Vector3(0, .62f, 2.98f), 2.5f, mat, correctionRoot.transform);
            }
            else DiagramLabel("표시된 대응 선분과 2:1 순서를 확인", new Vector3(0, .62f, 2.98f), 2.5f, mat, correctionRoot.transform);
        }

        void BuildUi()
        {
            titleUi = MgfText.Ui("너구리 온천", new Vector2(.5f, .76f), Vector2.zero, 54, MgfLook.Hex("F4E8CF"), 370);
            titleUi.outlineWidth = .22f; titleUi.outlineColor = MgfLook.Hex("2C3550");
            subtitleUi = MgfText.Ui("표지의 황동 받침을 밀어 책을 펼치시오", new Vector2(.5f, .25f), Vector2.zero, 20, MgfLook.Hex("F4E8CF"), 365);
            promptUi = MgfText.Ui("", new Vector2(.5f, 1f), new Vector2(0, -114), 19, MgfLook.Hex("2C3550"), 376);
            promptUi.textWrappingMode = TextWrappingModes.Normal; promptUi.rectTransform.sizeDelta = new Vector2(376, 78);
            promptUi.outlineWidth = .10f; promptUi.outlineColor = MgfLook.Hex("F4E8CF");
            goalUi = MgfText.Ui("", new Vector2(.5f, 1f), new Vector2(-16, -48), 16, MgfLook.Hex("2C3550"), 330);
            goalUi.outlineWidth = .08f; goalUi.outlineColor = MgfLook.Hex("F4E8CF");
            hudUi = MgfText.Ui("", new Vector2(.5f, 0f), new Vector2(0, 42), 16, MgfLook.Hex("2C3550"), 375);
            hudUi.outlineWidth = .08f; hudUi.outlineColor = MgfLook.Hex("F4E8CF");
            valueUi = MgfText.Ui("", new Vector2(.5f, .5f), new Vector2(0, -118), 28, MgfLook.Hex("2C3550"), 180);
            feedbackUi = MgfText.Ui("", new Vector2(.5f, .5f), new Vector2(0, 92), 22, MgfLook.Hex("2C3550"), 370);
            feedbackUi.textWrappingMode = TextWrappingModes.Normal; feedbackUi.rectTransform.sizeDelta = new Vector2(370, 78);
            feedbackUi.outlineWidth = .09f; feedbackUi.outlineColor = MgfLook.Hex("F4E8CF");
            ApplyLayout(true);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("너구리온천받침을계산한눈금까지밀어물길을열어라평행선과선분의길이의비중점연결정리무게중심증기표영업중단다시펼치기정답오답부분전체꼭짓점으로부터");
            foreach (var p in bank) { sb.Append(p.prompt); sb.Append(p.equation); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            ApplyLayout(false);
            float aspect = Mathf.Max(.35f, cam.aspect);
            // 세로 플레이에서는 책을 더 내려다봐(약 52°) 도해와 레일이 화면 높이를 채우게 한다. 타이틀은 낮은 각의 무대.
            float targetElev = mode == Mode.Title ? 30f : (landscapeLayout ? 42f : 52f);
            camElev = Mathf.MoveTowards(camElev, targetElev, dt * 34f);
            float k = Mathf.InverseLerp(30f, landscapeLayout ? 42f : 52f, camElev);
            if (landscapeLayout)
            {
                // 가로 플레이: 책 가운데(도해·레일)를 더 크게, 날개 페이지는 가장자리를 채운다.
                cam.orthographicSize = Mathf.Lerp(aspect > 2f ? 5.35f : 5.75f, aspect > 2f ? 4.55f : 4.85f, k);
                Vector3 target = Vector3.Lerp(new Vector3(0, .42f, .85f), new Vector3(0, .42f, .45f), k);
                float r = camElev * Mathf.Deg2Rad;
                cam.transform.position = target + new Vector3(0, Mathf.Sin(r), -Mathf.Cos(r)) * 15.2f;
                cam.transform.LookAt(target);
            }
            else
            {
                cam.orthographicSize = Mathf.Lerp(Mathf.Clamp(3.55f / aspect, 7.25f, 8.55f), Mathf.Clamp(3.2f / aspect, 6.3f, 8.55f), k);
                Vector3 target = Vector3.Lerp(new Vector3(0, .48f, .45f), new Vector3(0, .45f, .1f), k);
                float r = camElev * Mathf.Deg2Rad;
                cam.transform.position = target + new Vector3(0, Mathf.Sin(r), -Mathf.Cos(r)) * 16.1f;
                cam.transform.LookAt(target);
            }
            AnimateWorld(dt);
            HandleInput();
            if (mode == Mode.Practice)
            {
                guideClock += dt; if (guideClock > 8f) { guideClock = 0; MgfSfx.Play("tap", .18f); }
                // 10초 동안 아무 진행이 없으면 받침을 2 cm로 직접 옮겨 보여 주고, 다음 탭 하나로 확정하게 한다.
                practiceIdle += dt; if (!rescueArmed && !dragging && practiceIdle > 10f) ArmRescue();
            }
            else if (mode == Mode.Playing) { sessionLeft -= dt; if (sessionLeft <= 0) EndRun("시간 종료"); }
            else if (mode == Mode.Reveal) { revealClock += dt; if (revealClock >= (revealCorrect ? 1.25f : 1.4f)) FinishReveal(); }
            float targetScore = st.score;
            scoreShown = Mathf.MoveTowards(scoreShown, targetScore, dt * 620f);
            RefreshHud();
        }

        void ApplyLayout(bool force)
        {
            bool land = Screen.width > Screen.height * 1.18f;
            if (!force && land == landscapeLayout) return;
            landscapeLayout = land;
            if (land)
            {
                promptUi.fontSize = 19;
                feedbackUi.fontSize = 19;
                // 가로: 글자는 화면 위·아래 띠에 모아 팝업 날개와 겹치지 않게 한다.
                SetUi(goalUi, new Vector2(.5f, 1f), new Vector2(0, -30), new Vector2(760, 40));
                SetUi(promptUi, new Vector2(.5f, 1f), new Vector2(0, -78), new Vector2(820, 60));
                SetUi(hudUi, new Vector2(.5f, 0f), new Vector2(0, 26), new Vector2(760, 40));
                SetUi(feedbackUi, new Vector2(.5f, 0f), new Vector2(0, 76), new Vector2(760, 60));
                SetUi(subtitleUi, new Vector2(.5f, .11f), Vector2.zero, new Vector2(620, 64));
            }
            else
            {
                promptUi.fontSize = 19;
                feedbackUi.fontSize = 18;
                SetUi(promptUi, new Vector2(.5f, 1f), new Vector2(0, -114), new Vector2(376, 78));
                SetUi(goalUi, new Vector2(.5f, 1f), new Vector2(-16, -48), new Vector2(330, 42));
                SetUi(hudUi, new Vector2(.5f, 0f), new Vector2(0, 42), new Vector2(375, 44));
                SetUi(feedbackUi, new Vector2(.5f, 0f), new Vector2(0, 150), new Vector2(370, 92));
                SetUi(subtitleUi, new Vector2(.5f, .25f), Vector2.zero, new Vector2(365, 64));
            }
        }

        void SetUi(TextMeshProUGUI text, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = offset; rt.sizeDelta = size;
        }

        void AnimateWorld(float dt)
        {
            float t = Time.time;
            if (titleScenery.activeSelf)
            {
                float sway = Mathf.Sin(t * .8f) * .7f;
                titleScenery.transform.localRotation = Quaternion.Euler(0, sway, 0);
            }
            for (int i = 0; i < steamPuffs.Length; i++) if (steamPuffs[i] != null)
            {
                Vector3 s = steamPuffs[i].transform.localScale;
                s.x = .86f + Mathf.Sin(t * .75f + i * .63f) * .13f;
                steamPuffs[i].transform.localScale = s;
                steamPuffs[i].transform.localRotation = Quaternion.Euler(0, Mathf.Sin(t * .42f + i) * 4f, (i % 2 == 0 ? -1f : 1f) * (8f + i) + Mathf.Sin(t + i) * 3f);
            }
            raccoonCheer = Mathf.MoveTowards(raccoonCheer, 0, dt * 1.8f);
            raccoon.transform.localRotation = Quaternion.Euler((camElev - 30f) * .85f, Mathf.Sin(t * 1.4f) * (5f + raccoonCheer * 16f), Mathf.Sin(t * 2f) * (2.2f + raccoonCheer * 7f));
            Vector3 raccoonTarget = titleScenery.activeSelf ? new Vector3(2.15f, .35f, .55f) : new Vector3(2.45f, .35f, 3.05f);
            raccoon.transform.position = Vector3.Lerp(raccoon.transform.position, raccoonTarget, 1f - Mathf.Exp(-dt * 6f));
            if (slider.activeSelf)
            {
                Vector3 sp = slider.transform.position; float tx = XFor(selected);
                sp.x = slowSlide ? Mathf.MoveTowards(sp.x, tx, dt * 2.4f) : Mathf.Lerp(sp.x, tx, 1f - Mathf.Exp(-dt * 30f));
                if (slowSlide && Mathf.Abs(sp.x - tx) < .001f) slowSlide = false;
                slider.transform.position = sp;
                valueWorld.transform.position = new Vector3(Mathf.Clamp(sp.x, -2.3f, 2.3f), -.2f, SliderZ - .95f);
            }
            for (int i = 0; i < ripples.Length; i++)
            {
                if (rippleAge[i] >= .75f) continue;
                rippleAge[i] += dt;
                if (rippleAge[i] >= .75f) { ripples[i].SetActive(false); continue; }
                float u = rippleAge[i] / .75f, sc = Mathf.Lerp(.3f, 1.5f, u) * (1f - u * u * .9f);
                ripples[i].transform.localScale = new Vector3(sc, 1f, sc);
            }
            if (trailAge < 1.5f)
            {
                trailAge += dt;
                Vector3 to = slider.transform.position + Vector3.up * .3f;
                for (int i = 0; i < trailDots.Length; i++)
                {
                    float u = (i + 1f) / (trailDots.Length + 1f);
                    bool on = trailAge < 1.5f && trailAge * 16f > i;
                    if (trailDots[i].activeSelf != on) trailDots[i].SetActive(on);
                    if (!on) continue;
                    Vector3 pos = Vector3.Lerp(trailFrom, to, u); pos.y += Mathf.Sin(u * Mathf.PI) * .55f;
                    trailDots[i].transform.position = pos;
                    trailDots[i].transform.localScale = Vector3.one * (trailAge > 1.1f ? Mathf.Max(0f, (1.5f - trailAge) / .4f) : 1f);
                }
            }
            raccoon.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * 11f) * raccoonCheer * .12f);
            if (mode == Mode.Practice)
            {
                guideFlash = Mathf.MoveTowards(guideFlash, 0, dt * 1.4f);
                guidePersist = Mathf.MoveTowards(guidePersist, 0, dt);
                float pulse = 1f + (.16f + guideFlash * .18f) * (Mathf.Sin(t * 5f) * .5f + .5f);
                guide.transform.position = slider.transform.position + Vector3.up * .08f;
                guide.transform.localScale = new Vector3(pulse, .15f, pulse);
                float demo = Mathf.SmoothStep(0, 1, Mathf.PingPong(t * .36f, 1f));
                float handX = dragging ? slider.transform.position.x : Mathf.Lerp(slider.transform.position.x, XFor(2), demo);
                guideHand.transform.position = new Vector3(handX, SliderY + .94f + Mathf.Sin(t * 4.5f) * .1f, SliderZ);
                float start = guidePersist > 0 ? guideOrigin.x : slider.transform.position.x, end = XFor(2);
                for (int i = 0; i < guideDots.Length; i++)
                {
                    float u = (i + 1f) / (guideDots.Length + 1f);
                    float wave = Mathf.Repeat(u + t * .55f, 1f);
                    float pathX = guidePersist > 0 && wave < .45f ? Mathf.Lerp(start, slider.transform.position.x, wave / .45f) : Mathf.Lerp(slider.transform.position.x, end, guidePersist > 0 ? (wave - .45f) / .55f : wave);
                    guideDots[i].transform.position = new Vector3(pathX, SliderY + .17f + Mathf.Sin(wave * Mathf.PI) * .14f, SliderZ);
                    guideDots[i].transform.localScale = Vector3.one * (.75f + guideFlash * .55f);
                }
                guideArrow.transform.position = new Vector3(end, SliderY + .22f, SliderZ);
                goalGhost.SetActive(true);
                dropMarker.SetActive(true);
            }
            else if (playPulse > 0 && mode == Mode.Playing && !dragging)
            {
                // 본판 첫 문항: 받침 맥동과 손 시늉 한 번으로 '끌어 놓기'를 다시 상기시킨다(정답 위치는 가리키지 않는다).
                playPulse -= dt;
                bool on = playPulse > 0;
                if (guide.activeSelf != on) { guide.SetActive(on); guideHand.SetActive(on); }
                float pulse = 1f + .3f * (Mathf.Sin(t * 6f) * .5f + .5f);
                guide.transform.position = slider.transform.position + Vector3.up * .08f;
                guide.transform.localScale = new Vector3(pulse, .15f, pulse);
                guideHand.transform.position = slider.transform.position + new Vector3(Mathf.Sin(t * 3f) * .35f, .94f + Mathf.Sin(t * 4.5f) * .1f, 0);
            }
            else SetGuideVisible(false);
            if (magnifierRoot.activeSelf)
                magnifierRoot.transform.position = new Vector3(Mathf.Clamp(slider.transform.position.x, -1.6f, 1.6f), 1.95f, SliderZ + .3f);
            if (water.activeSelf)
            {
                waterFlow = Mathf.MoveTowards(waterFlow, 3.35f, dt * 5.5f);
                water.transform.localScale = new Vector3(waterFlow, 1f, 1f);
                water.transform.position = new Vector3(.0f + waterFlow * .5f, .46f + Mathf.Sin(t * 6f) * .018f, -.44f);
            }
            for (int i = 0; i < baths.Length; i++)
            {
                float open = i < st.openedBaths ? 1f : 0f;
                Vector3 p = bathBase[i]; p.y += open * (.58f + Mathf.Sin(t * 1.8f + i) * .035f);
                baths[i].transform.position = Vector3.Lerp(baths[i].transform.position, p, dt * 7f);
            }
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                if (mode == Mode.Title || mode == Mode.End) { StartPractice(); return; }
                RaycastHit hit;
                bool rayHit = MgfPointer.DownHit(cam, out hit);
                Vector3 p;
                bool onPlane = MgfPointer.OnPlane(cam, SliderY, out p);
                downPoint = rayHit ? hit.point + Vector3.up * .04f : (onPlane ? p : slider.transform.position);
                // 책 바깥(하늘·무대 뒤)을 누른 경우 물결은 책 가장자리에 남긴다.
                downPoint.x = Mathf.Clamp(downPoint.x, -3.5f, 3.5f); downPoint.z = Mathf.Clamp(downPoint.z, -3.0f, 3.9f);
                if (mode == Mode.Reveal) { SpawnRipple(downPoint); Notify(); return; }
                practiceIdle = 0;
                pointerDownScreen = MgfPointer.Position;
                if (rescueArmed)
                {
                    // 구조 상태: 받침은 2 cm에 잠겨 있고 어디를 눌렀다 떼도(끌어도) 그 값으로 확정한다.
                    BeginGrab(); feedbackUi.text = "2 cm에 잠겼습니다  ·  손을 떼면 확정"; return;
                }
                bool hitRail = (rayHit && IsRailGrab(hit.collider.gameObject)) || (onPlane && IsRailPoint(p));
                if (hitRail)
                {
                    if (onPlane) SetSelectedFromWorldX(p.x);
                    BeginGrab();
                    if (mode == Mode.Practice) feedbackUi.text = "잡았습니다  ·  2 cm까지 끌고 손을 떼시오";
                }
                else pendingFree = true; // 빈 곳: 끌기로 이어지면 받침을 잡고, 그냥 떼면 무효 탭이다.
            }
            if (pendingFree && MgfPointer.Held && Vector2.Distance(pointerDownScreen, MgfPointer.Position) >= 34f)
            {
                pendingFree = false; BeginGrab();
            }
            if (pendingFree && MgfPointer.Up) { pendingFree = false; InvalidTap(); return; }
            if (dragging && MgfPointer.Held && !rescueArmed)
            {
                Vector3 p;
                if (MgfPointer.OnPlane(cam, SliderY, out p))
                {
                    int before = selected;
                    SetSelectedFromWorldX(p.x);
                    if (selected != before) { st.currentValue = selected; st.pointerVersion++; Notify(); UpdateMagnifier(false); }
                }
            }
            if (dragging && MgfPointer.Up)
            {
                dragging = false; st.dragging = false; st.pointerVersion++;
                magnifierRoot.SetActive(false);
                Submit(selected);
            }
        }

        void BeginGrab()
        {
            dragging = true; st.dragging = true; st.currentValue = selected;
            MgfFx.Punch(slider.transform, .12f, .16f); MgfSfx.Play("tap", .28f);
            magnifierRoot.SetActive(true); UpdateMagnifier(true); Notify();
        }

        // 빈 곳을 눌렀다 뗀 탭: 탭 자리에 물결, 받침까지 점선, 횟수가 들어간 안내로 매번 다른 화면을 만든다.
        void InvalidTap()
        {
            invalidTaps++;
            SpawnRipple(downPoint); trailFrom = downPoint; trailAge = 0;
            guideFlash = 1f; guideClock = 0; guidePersist = 2.2f;
            guideOrigin = new Vector3(Mathf.Clamp(downPoint.x, RailMinX, RailMaxX), SliderY, SliderZ);
            MgfFx.Punch(slider.transform, .1f, .22f); MgfSfx.Play("tap", .2f);
            if (mode == Mode.Practice)
            {
                feedbackUi.text = "빈 곳을 눌렀습니다 (" + invalidTaps + ")  ·  레일의 2 눈금을 누르거나 받침을 2까지 끄시오";
                if (invalidTaps >= 2) ArmRescue();
            }
            else feedbackUi.text = "빈 곳 " + invalidTaps + "번  ·  레일 눈금을 누르거나 빨간 받침을 끌어 제출하시오";
            Notify();
        }

        void ArmRescue()
        {
            rescueArmed = true; selected = 2; slowSlide = true; st.currentValue = 2; guideFlash = 1f;
            feedbackUi.text = "받침을 2 cm로 옮겼습니다  ·  화면 아무 곳이나 눌렀다 떼면 확정";
            Notify();
        }

        void SpawnRipple(Vector3 at)
        {
            int i = rippleNext; rippleNext = (rippleNext + 1) % ripples.Length;
            ripples[i].transform.position = at; ripples[i].transform.localScale = new Vector3(.3f, 1f, .3f);
            ripples[i].SetActive(true); rippleAge[i] = 0;
        }

        bool IsRailPoint(Vector3 p)
        {
            return p.x > RailMinX - .55f && p.x < RailMaxX + .55f && p.z > SliderZ - 1.0f && p.z < SliderZ + 1.15f;
        }

        void SetSelectedFromWorldX(float worldX)
        {
            float x = Mathf.Clamp(worldX, RailMinX, RailMaxX);
            selected = Mathf.Clamp(Mathf.RoundToInt((x - RailMinX) / (RailMaxX - RailMinX) * 23f) + 1, 1, 24);
            slowSlide = false;
        }

        void UpdateMagnifier(bool force)
        {
            if (!force && selected == lastMagnifierValue) return;
            lastMagnifierValue = selected;
            for (int i = 0; i < magnifierValues.Length; i++)
            {
                int value = selected + i - 2;
                magnifierValues[i].text = value >= 1 && value <= 24 ? value.ToString() : "·";
            }
        }

        bool IsRailGrab(GameObject hit)
        {
            if (hit == slider || hit.transform.IsChildOf(slider.transform) || hit == railBed || hit == railTouchZone) return true;
            return hit.name.StartsWith("Tick ", StringComparison.Ordinal);
        }

        void ShowTitle()
        {
            mode = Mode.Title; st.phase = "title"; st.score = 0; st.lives = 3; st.level = 1; st.solved = 0;
            st.openedBaths = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.onboarding = false;
            titleScenery.SetActive(true); cover.SetActive(true); book.SetActive(true); channel.SetActive(true); raccoon.SetActive(true); slider.SetActive(false); diagramRoot.SetActive(false);
            water.SetActive(true); waterFlow = 1.7f; SetGuideVisible(false); magnifierRoot.SetActive(false); feedbackUi.text = "";
            promptUi.gameObject.SetActive(false); goalUi.gameObject.SetActive(false); hudUi.gameObject.SetActive(false); valueUi.gameObject.SetActive(false); valueWorld.gameObject.SetActive(false); feedbackUi.gameObject.SetActive(false);
            titleUi.gameObject.SetActive(false); subtitleUi.text = "물길의 황동 할핀을 눌러 팝업북을 펼치시오"; subtitleUi.gameObject.SetActive(true); RefreshHud(); Notify();
        }

        void StartPractice()
        {
            ResetRun(); mode = Mode.Practice; st.phase = "playing"; st.onboarding = true;
            current = new Problem { id = "practice", kind = 0, band = 1, answer = 2, v1 = 6, v2 = 3, v3 = 4, concept = "삼각형에서 평행선과 선분의 길이의 비", prompt = "△ABC에서 DE∥BC, AD=6 cm, DB=3 cm, AE=4 cm일 때, EC의 길이를 구하시오.", equation = "6:3 = 4:2" };
            firstAttempt = true; selected = 12; practiceMisses = 0; invalidTaps = 0; rescueArmed = false; pendingFree = false; practiceIdle = 0; playPulse = 0; PositionSlider(); guideClock = 0; guideFlash = 1f; guidePersist = 1.2f; guideOrigin = slider.transform.position;
            titleScenery.SetActive(false); cover.SetActive(false); SetPlayVisible(true); titleUi.gameObject.SetActive(false); subtitleUi.gameObject.SetActive(false); BeginProblem(); MgfSfx.Play("whoosh", .42f); Notify();
        }

        void StartRunDirect()
        {
            ResetRun(); mode = Mode.Playing; st.phase = "playing"; st.onboarding = false; rescueArmed = false; invalidTaps = 0; playPulse = 2.8f; BuildDeck(); NextProblem(); Notify();
        }

        void ResetRun()
        {
            runSerial++; deckIndex = 0; st.score = 0; scoreShown = 0; st.lives = 3; st.level = 1; st.solved = 0; st.openedBaths = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.attempt = 0; st.misconceptionId = ""; st.currentValue = 12;
            sessionLeft = 120f; waterFlow = 0; water.SetActive(false); magnifierRoot.SetActive(false); SetPlayVisible(true);
            for (int i = 0; i < tickets.Length; i++) { tickets[i].SetActive(true); tickets[i].GetComponent<Renderer>().sharedMaterial = vermilion; }
        }

        void BuildDeck()
        {
            deck.Clear();
            AddKinds(0, 3); AddKinds(1, 2); AddKinds(2, 1); AddKinds(3, 1); AddKinds(4, 2);
        }

        void AddKinds(int kind, int count)
        {
            int start = (runSerial * 37 + kind * 53) % bank.Count, added = 0;
            for (int step = 0; step < bank.Count && added < count; step++)
            {
                var p = bank[(start + step) % bank.Count];
                if (p.kind == kind) { deck.Add(p); added++; }
            }
        }

        void NextProblem()
        {
            if (st.solved >= 9) { EndRun(st.firstAttemptCorrect >= 7 ? "온천 거리 완성" : "첫 제출 기준 미달"); return; }
            current = deck[Mathf.Min(deckIndex++, deck.Count - 1)]; firstAttempt = true; st.attempt = 0; st.misconceptionId = "";
            selected = 1 + ((runSerial * 11 + st.solved * 7) % 24); if (selected == current.answer) selected = selected % 24 + 1;
            PositionSlider(); BeginProblem();
        }

        void BeginProblem()
        {
            st.problemId = current.id; st.prompt = current.prompt; st.currentValue = selected; st.level = current.band;
            promptUi.text = current.prompt; feedbackUi.text = ""; water.SetActive(false); waterFlow = 0; channel.GetComponent<Renderer>().sharedMaterial = teal;
            BuildProblemDiagram(current);
            if (mode == Mode.Practice) SetGuideVisible(true);
            Notify();
        }

        void Submit(int value)
        {
            if (mode != Mode.Practice && mode != Mode.Playing) return;
            revealPractice = mode == Mode.Practice; revealCorrect = value == current.answer;
            if (!revealPractice && firstAttempt)
            {
                st.firstAttemptTotal++; if (revealCorrect) st.firstAttemptCorrect++;
            }
            if (revealCorrect)
            {
                if (revealPractice) { st.score = 50; feedbackUi.text = current.equation + "  ·  물길이 열렸습니다"; }
                else
                {
                    st.score += firstAttempt ? 140 + st.solved * 12 : 70; st.solved++; st.openedBaths = st.solved;
                    feedbackUi.text = current.equation + "  ·  " + current.answer + " cm";
                }
                st.misconceptionId = ""; channel.GetComponent<Renderer>().sharedMaterial = brass; water.SetActive(true);
                raccoonCheer = 1f; SetGuideVisible(false); MgfFx.Punch(channel.transform, .16f, .32f); MgfFx.Burst(raccoon.transform.position + Vector3.up, MgfLook.Hex("74E6D8"), 12, .34f); MgfSfx.Play("correct", .55f);
            }
            else
            {
                if (revealPractice) practiceMisses++;
                st.attempt++; st.misconceptionId = Diagnose(value); firstAttempt = false;
                if (!revealPractice) { st.lives--; if (st.lives >= 0 && st.lives < 3) tickets[st.lives].GetComponent<Renderer>().sharedMaterial = wet; }
                channel.GetComponent<Renderer>().sharedMaterial = wet;
                // 오답에서 완성 비례식을 보여 주면 다음 입력 전에 답을 누설한다.
                feedbackUi.text = WrongMessage(st.misconceptionId) + "  같은 도형에서 다시 시도하시오.";
                ShowDiagramCorrection(st.misconceptionId);
                MgfFx.Shake(cam, .055f, .24f); MgfSfx.Play("wrong", .38f);
            }
            mode = Mode.Reveal; st.phase = "playing"; revealClock = 0; Notify();
        }

        string Diagnose(int v)
        {
            if (v == current.wrong1) return current.wrong1Id;
            if (v == current.wrong2) return current.wrong2Id;
            if (v < current.answer) return "answer_too_small";
            return "answer_too_large";
        }

        string WrongMessage(string id)
        {
            if (id == "mix_part_and_whole") return "부분과 전체를 같은 비에 섞지 마시오.";
            if (id == "ratio_reversed") return "대응 선분의 위·아래 순서를 뒤집었습니다.";
            if (id == "copy_given_segment") return "주어진 한 선분을 그대로 옮기지 말고 비를 적용하시오.";
            if (id == "use_other_part") return "AD 대신 DB를 썼습니다. 전체 AB와 AD를 대응하시오.";
            if (id == "midpoint_equals_base") return "중점연결선분은 나머지 한 변의 절반입니다.";
            if (id == "midpoint_halved_twice") return "중점 표시는 한 번만 절반으로 만듭니다.";
            if (id == "parallel_copy_lower") return "다른 직선의 아래 구간을 그대로 복사하지 마시오.";
            if (id == "parallel_add_difference") return "길이의 차가 아니라 대응 구간의 비를 사용하시오.";
            if (id == "centroid_ratio_reversed") return "무게중심은 꼭짓점으로부터 2:1로 나눕니다.";
            if (id == "centroid_midpoint") return "무게중심은 중점이 아닙니다. 중선을 2:1로 나눕니다.";
            if (id == "centroid_copy_ratio") return "비의 수 2를 길이로 그대로 쓰지 마시오.";
            if (id == "answer_too_small") return "선택한 길이가 계산값보다 작습니다. 비의 순서를 확인하시오.";
            return "선택한 길이가 계산값보다 큽니다. 같은 위치의 선분을 대응하시오.";
        }

        void FinishReveal()
        {
            if (revealPractice)
            {
                if (revealCorrect) { mode = Mode.Playing; st.onboarding = false; st.score = 0; rescueArmed = false; invalidTaps = 0; playPulse = 2.8f; BuildDeck(); NextProblem(); }
                else
                {
                    mode = Mode.Practice; practiceIdle = 0;
                    if (practiceMisses < 2) selected = 12;
                    PositionSlider(); BeginProblem(); SetGuideVisible(true);
                    guidePersist = 2.2f; guideOrigin = slider.transform.position;
                    // 두 번 틀리면 틀린 자리에서 2 cm까지 받침이 천천히 미끄러지는 시범을 보이고 잠근다.
                    if (practiceMisses >= 2) ArmRescue();
                    else feedbackUi.text = "다시  ·  레일의 2 눈금을 누르거나 받침을 2까지 끄시오";
                }
            }
            else if (revealCorrect) { mode = Mode.Playing; NextProblem(); }
            else if (st.lives <= 0 || st.firstAttemptTotal - st.firstAttemptCorrect >= 3) EndRun("증기 표 소진");
            else { mode = Mode.Playing; BeginProblem(); }
            Notify();
        }

        void EndRun(string reason)
        {
            mode = Mode.End; st.phase = (st.solved >= 9 && st.firstAttemptCorrect >= 7) ? "clear" : "gameover";
            feedbackUi.text = st.phase == "clear" ? "온천 거리 완성  ·  " + st.firstAttemptCorrect + "/9 첫 제출 정답" : "영업 중단  ·  " + reason + "\n책을 눌러 다시 펼치시오";
            titleUi.text = st.phase == "clear" ? "아홉 탕 완성" : "오늘 영업 종료"; titleUi.gameObject.SetActive(true); titleUi.rectTransform.anchoredPosition = new Vector2(0, 12);
            subtitleUi.text = "완성한 탕 " + st.solved + "개  ·  점수 " + st.score; subtitleUi.gameObject.SetActive(true);
            if (st.phase == "clear") { for (int i = 0; i < 9; i++) MgfFx.Glow(baths[i].transform.position + Vector3.up, MgfLook.Hex("FBE087"), 5, .25f); MgfSfx.Play("win", .65f); }
            else MgfSfx.Play("lose", .45f);
            Notify();
        }

        void PositionSlider() { st.currentValue = selected; slider.transform.position = new Vector3(XFor(selected), SliderY, SliderZ); }
        float XFor(int v) { return Mathf.Lerp(RailMinX, RailMaxX, (v - 1) / 23f); }
        void SetPlayVisible(bool on)
        {
            book.SetActive(on); slider.SetActive(on); channel.SetActive(on); raccoon.SetActive(on);
            diagramRoot.SetActive(on);
            promptUi.gameObject.SetActive(on); goalUi.gameObject.SetActive(on); hudUi.gameObject.SetActive(on); valueUi.gameObject.SetActive(false); valueWorld.gameObject.SetActive(on); feedbackUi.gameObject.SetActive(on);
        }

        void SetGuideVisible(bool on)
        {
            guide.SetActive(on); guideHand.SetActive(on); guideArrow.SetActive(on); goalGhost.SetActive(on); dropMarker.SetActive(on);
            for (int i = 0; i < guideDots.Length; i++) guideDots[i].SetActive(on);
        }

        void RefreshHud()
        {
            if (mode == Mode.Title) return;
            bool timed = !st.onboarding && (mode == Mode.Playing || mode == Mode.Reveal);
            int secs = timed ? Mathf.CeilToInt(Mathf.Max(0f, sessionLeft)) : -1, score = Mathf.RoundToInt(scoreShown);
            // 값이 바뀔 때만 문자열을 만든다(매 프레임 할당 방지).
            int key = ((((int)mode * 10 + st.solved) * 4 + Mathf.Clamp(st.lives, 0, 3)) * 200 + secs + 1) * 100 + selected;
            if (key == hudKey && score == lastHudScore) return;
            hudKey = key; lastHudScore = score;
            goalUi.text = mode == Mode.Practice ? "연습  ·  받침을 2 cm까지 밀어 물길을 여시오" : "받침을 계산한 눈금까지 밀어 물길을 여시오";
            valueWorld.text = selected + " cm";
            string time = mode == Mode.End ? "" : secs < 0 ? "연습 중" : (secs <= 20 ? "<color=#D9533F>남은 " + secs + "초</color>" : "남은 " + secs + "초");
            hudUi.text = "탕 " + st.solved + "/9   증기 표 " + Mathf.Max(0, st.lives) + "/3   " + time + "   점수 " + score;
        }

        void Notify() { MgfBridge.NotifyChanged(); }

        // 실제 생성기와 실제 제출 판정 경로를 훅도 그대로 사용한다.
        public void TestStart() { titleScenery.SetActive(false); cover.SetActive(false); titleUi.gameObject.SetActive(false); subtitleUi.gameObject.SetActive(false); StartRunDirect(); }
        public void TestAnswerCorrect() { if (mode != Mode.Playing) StartRunDirect(); selected = current.answer; PositionSlider(); Submit(selected); }
        public void TestAnswerWrong()
        {
            if (mode != Mode.Playing) StartRunDirect();
            int v = current.answer == 1 ? 2 : 1; selected = v; PositionSlider(); Submit(v);
        }
        public string StateJson() { return JsonUtility.ToJson(st); }
        public string ProblemBankJson()
        {
            var list = new List<MgfProblem>(bank.Count);
            foreach (var p in bank)
                list.Add(new MgfProblem { id = p.id, prompt = p.prompt, choices = null, answer = p.answer.ToString(), answerNumeric = p.answer, unitConcept = p.concept });
            return MgfJson.Bank(list);
        }

        void BuildBank()
        {
            // 제약 만족 풀: 답 k를 먼저 정하고, 정수 비가 성립하는 인수만 골라 데이터를 역생성한다.
            var seen = new HashSet<string>(); int serial = 0;
            for (int kind = 0; kind < 5; kind++)
            for (int k = 1; k <= 24; k++)
            for (int variant = 0; variant < 5; variant++)
            {
                Problem p = MakeProblem(kind, k, variant);
                if (p != null && seen.Add(p.prompt + "|" + p.answer)) { p.id = "n" + (++serial); bank.Add(p); }
            }
            // 300개 미만이면 변형 계수를 넓힌다. 고정 상한 루프라 무한루프가 없다.
            for (int variant = 5; bank.Count < 360 && variant < 40; variant++)
            for (int kind = 0; kind < 5 && bank.Count < 360; kind++)
            for (int k = 1; k <= 24 && bank.Count < 360; k++)
            {
                Problem p = MakeProblem(kind, k, variant);
                if (p != null && seen.Add(p.prompt + "|" + p.answer)) { p.id = "n" + (++serial); bank.Add(p); }
            }
            // 실제 판정과 같은 문항 객체가 서로 다른 대표 오개념 두 개를 가진 경우만 은행에 들어온다.
            foreach (var p in bank)
                if (p.wrong1 < 1 || p.wrong2 < 1 || p.wrong1 == p.wrong2 || p.wrong1 == p.answer || p.wrong2 == p.answer)
                    Debug.LogError("invalid misconception map " + p.id);
        }

        Problem MakeProblem(int kind, int k, int v)
        {
            int a, b, s, t;
            if (kind == 0)
            {
                b = Divisor(k, 1 + v % 6); t = k / b; a = 1 + (v * 2 + k) % 7; if (a == b) a = a % 7 + 1; s = 1 + (v + k) % 4;
                int ad = a * s, db = b * s, ae = a * t;
                var p = new Problem { kind=0, band=1, answer=k, v1=ad, v2=db, v3=ae, concept="삼각형에서 평행선과 선분의 길이의 비", prompt=$"△ABC에서 DE∥BC, AD={ad} cm, DB={db} cm, AE={ae} cm일 때, EC의 길이를 구하시오.", equation=$"{ad}:{db} = {ae}:{k}" };
                return AddMisconceptions(p,
                    ExactWrong(ae*(ad+db), ad, k), "mix_part_and_whole",
                    ExactWrong(ae*ad, db, k), "ratio_reversed",
                    ae, "copy_given_segment", db, "copy_given_segment");
            }
            if (kind == 1)
            {
                a = Divisor(k, 1 + v % 6); t = k / a; b = 1 + (v * 3 + k) % 7; s = 1 + (v + 2*k) % 4;
                int ad=a*s, ab=(a+b)*s, bc=(a+b)*t;
                var p = new Problem { kind=1, band=2, answer=k, v1=ad, v2=ab, v3=bc, concept="평행선으로 생기는 닮음", prompt=$"△ABC에서 DE∥BC, AD={ad} cm, AB={ab} cm, BC={bc} cm일 때, DE의 길이를 구하시오.", equation=$"{ad}:{ab} = {k}:{bc}" };
                return AddMisconceptions(p,
                    ExactWrong(bc*ad, ab-ad, k), "mix_part_and_whole",
                    ExactWrong(bc*(ab-ad), ab, k), "use_other_part",
                    ad, "copy_given_segment", bc, "copy_given_segment");
            }
            if (kind == 2)
            {
                int bc=2*k;
                var p = new Problem { kind=2, band=2, answer=k, v1=bc, concept="삼각형의 중점연결정리", prompt=$"△ABC에서 M, N은 각각 AB, AC의 중점이고 BC={bc} cm일 때, MN의 길이를 구하시오.", equation=$"MN = {bc}÷2 = {k}" };
                return AddMisconceptions(p, bc, "midpoint_equals_base", ExactWrong(k, 2, k), "midpoint_halved_twice", 2, "copy_given_segment", 4, "copy_given_segment");
            }
            if (kind == 3)
            {
                int q=Divisor(k,1+v%6), mult=k/q; int p=1+(v*3+k)%8, r=p*mult;
                var problem = new Problem { kind=3, band=3, answer=k, v1=p, v2=q, v3=r, concept="평행선 사이의 선분의 길이의 비", prompt=$"세 평행선 l, m, n이 두 직선과 만난다. 한 직선의 두 구간이 {p} cm, {q} cm이고 다른 직선의 대응 구간이 {r} cm, x cm일 때, x의 값을 구하시오.", equation=$"{p}:{q} = {r}:{k}" };
                return AddMisconceptions(problem, q, "parallel_copy_lower", r, "copy_given_segment", q + r - p, "parallel_add_difference", ExactWrong(r*p, q, k), "ratio_reversed");
            }
            // 무게중심: AG를 묻는 경우는 짝수 k만, 홀수 k는 GD를 물어 모든 답을 1~24에서 유지한다.
            if (k % 2 == 0)
            {
                int u=k/2, ad=3*u;
                var p = new Problem { kind=4, band=3, answer=k, v1=ad, asksAG=true, concept="삼각형의 중선과 무게중심", prompt=$"점 G는 △ABC의 무게중심이고 중선 AD={ad} cm일 때, AG의 길이를 구하시오.", equation=$"AG:GD=2:1,  AG={ad}×2÷3={k}" };
                return AddMisconceptions(p, u, "centroid_ratio_reversed", ExactWrong(ad, 2, k), "centroid_midpoint", 2, "centroid_copy_ratio", 1, "centroid_copy_ratio");
            }
            else
            {
                int ad=3*k;
                var p = new Problem { kind=4, band=3, answer=k, v1=ad, asksAG=false, concept="삼각형의 중선과 무게중심", prompt=$"점 G는 △ABC의 무게중심이고 중선 AD={ad} cm일 때, GD의 길이를 구하시오.", equation=$"AG:GD=2:1,  GD={ad}÷3={k}" };
                return AddMisconceptions(p, 2*k, "centroid_ratio_reversed", ExactWrong(ad, 2, k), "centroid_midpoint", 2, "centroid_copy_ratio", 1, "centroid_copy_ratio");
            }
        }

        Problem AddMisconceptions(Problem p, params object[] candidates)
        {
            for (int i = 0; i + 1 < candidates.Length; i += 2)
            {
                int value = (int)candidates[i]; string id = (string)candidates[i + 1];
                if (value < 1 || value > 24 || value == p.answer || value == p.wrong1) continue;
                if (p.wrong1 < 0) { p.wrong1 = value; p.wrong1Id = id; }
                else { p.wrong2 = value; p.wrong2Id = id; break; }
            }
            return p.wrong1 > 0 && p.wrong2 > 0 ? p : null;
        }

        int ExactWrong(int numerator, int denominator, int answer)
        {
            if (denominator <= 0 || numerator % denominator != 0) return -1;
            int value = numerator / denominator;
            return value >= 1 && value <= 24 && value != answer ? value : -1;
        }

        int Divisor(int n, int preferred)
        {
            int[] d = {1,2,3,4,5,6,8,12,24};
            for (int step=0; step<d.Length; step++) { int x=d[(preferred+step)%d.Length]; if (x<=n && n%x==0) return x; }
            return 1;
        }
    }
}
