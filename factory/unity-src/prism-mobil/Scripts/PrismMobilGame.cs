using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.PrismMobil
{
    public sealed class PrismMobilGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public string problemId = "";
            public int selectedNode = -1;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int attemptIndex;
            public int firstMisses;
            public int fuses = 3;
            public int tensionLines;
            public int pointerVersion;
            public bool onboarding;
            public string misconceptionId = "";
        }

        enum GamePhase { Title, Practice, Playing, Reveal, End }
        enum DragMode { None, Hook, TensionOne, TensionTwo }

        readonly State st = new State();
        readonly List<PrismProblem> problems = new List<PrismProblem>();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly List<PrismProblem> parallelDeck = new List<PrismProblem>();
        readonly List<PrismProblem> medianDeck = new List<PrismProblem>();
        readonly List<PrismProblem> centroidDeck = new List<PrismProblem>();

        GamePhase phase = GamePhase.Title;
        PrismProblem current;
        System.Random rng;
        int runSerial, parallelCursor, medianCursor, centroidCursor;
        bool firstAttempt = true;
        bool revealCorrect;
        float revealT, runSeconds, idleHintT, hitStopT, titlePressT, refuseT, guideClock;
        DragMode dragMode;
        Vector2 downNorm, hookNorm = new Vector2(.5f, .74f), trailNorm;
        string endReason = "";

        static readonly Color Aubergine = MgfLook.Hex("#170F2B");
        static readonly Color Magenta = MgfLook.Hex("#FF4FD8");
        static readonly Color Lime = MgfLook.Hex("#B8FF2C");
        static readonly Color Cyan = MgfLook.Hex("#35D9FF");
        static readonly Color Ivory = MgfLook.Hex("#F3E9FF");
        static readonly Color Ink = MgfLook.Hex("#07040E");

        Camera cam;
        Transform worldRoot, activePrism, activeHook;
        readonly Transform[] titleMobiles = new Transform[7];
        readonly Transform[] shadowPieces = new Transform[7];
        readonly Renderer[] fuseBulbs = new Renderer[3];
        readonly Material[] fuseOn = new Material[3];
        Material fuseOff, prismMat, prismEdgeMat, cyanMat, limeMat, magentaMat, threadMat;
        LineRenderer hookThread;
        float worldClock, prismTilt, prismGlow;

        Canvas canvas;
        RectTransform canvasRt;
        CanvasGroup titleGroup, hudGroup, endGroup, feedbackGroup;
        RawImage titleArt;
        RectTransform startHookRt, hookRt, trailRt, guideRt, feedbackRt, endCtaRt, diagramPlateRt;
        Image startHookGlow, hookImg, trailImg, guideImg, diagramPlate;
        TextMeshProUGUI logoTxt, taglineTxt, startTxt, unitTxt, bestTxt;
        TextMeshProUGUI progressTxt, fusesTxt, timeTxt, goalTxt, promptTxt, magnifierTxt, deviceTxt, feedbackTxt;
        TextMeshProUGUI endTitleTxt, endScoreTxt, endStatsTxt, endHintTxt, endCtaTxt;
        readonly Image[] nodeImgs = new Image[169];
        readonly RectTransform[] nodeRts = new RectTransform[169];
        readonly Image[] diagramLines = new Image[18];
        readonly RectTransform[] diagramLineRts = new RectTransform[18];
        readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[12];
        readonly RectTransform[] labelRts = new RectTransform[12];
        readonly Image[] tensionHandles = new Image[2];
        readonly RectTransform[] tensionHandleRts = new RectTransform[2];
        readonly Image[] guideDots = new Image[8];
        readonly RectTransform[] guideDotRts = new RectTransform[8];
        Sprite discSprite, ringSprite, hookSprite;
        int lastW = -1, lastH = -1, shownScore = -1;
        float displayedScore;
        bool landLayout, guideVisible;
        Rect stageRect = new Rect(.10f,.28f,.80f,.44f);
        Vector2 guideFromNorm, guideToNorm;

        void Awake()
        {
            MgfLook.Quality(30f);
            problems.AddRange(PrismMobilRules.BuildBank());
            foreach (var p in problems)
            {
                bank.Add(PrismMobilRules.ToMgf(p));
                if (p.kind == PrismProblemKind.Parallel) parallelDeck.Add(p);
                else if (p.kind == PrismProblemKind.Median) medianDeck.Add(p);
                else centroidDeck.Add(p);
            }
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var s = new StringBuilder("프리즘모빌한점에걸어빛을맞춰라중학교2학년평행선선분길이비삼각형중선무게중심갈고리퓨즈공연중단완성꼭짓점으로부터찾으시오정답다시걸기장력실빛그림자첫시도자기식광축짐벌판자세고정정렬점등분");
            foreach (var p in bank) { s.Append(p.prompt); s.Append(p.answer); }
            MgfText.Prewarm(s.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();
            if (phase == GamePhase.Playing)
            {
                runSeconds += dt;
                if (runSeconds >= 105f) EndRun(false, "105초가 지나 공연이 멈췄습니다.");
            }
            else if (phase == GamePhase.Practice)
            {
                idleHintT += dt;
                if (idleHintT >= 8f) { idleHintT = 0; ConfigureGuide(hookNorm,LineNodeNorm(current.answerNode)); }
            }
            else if (phase == GamePhase.Reveal)
            {
                revealT += dt;
                if (revealT >= (revealCorrect ? 1.45f : 1.25f)) FinishReveal();
            }
            if (hitStopT > 0) hitStopT -= dt;
            else UpdateWorld(dt);
            UpdateUi(dt);
        }

        static Sprite MakeDisc(int size, bool ring, bool hook)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { name = hook ? "PrismHook" : ring ? "PrismRing" : "PrismDisc", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x + .5f) / size * 2f - 1f, ny = (y + .5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float a;
                if (hook)
                {
                    float arc = Mathf.Clamp01(1f - Mathf.Abs(d - .58f) / .16f);
                    float gap = ny > .1f && nx > -.25f ? 0f : 1f;
                    float stem = Mathf.Abs(nx + .38f) < .12f && ny > .15f ? 1f : 0f;
                    a = Mathf.Max(arc * gap, stem);
                }
                else a = ring ? Mathf.Clamp01(1f - Mathf.Abs(d - .70f) / .12f) : Mathf.Clamp01((1f - d) * 5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(.5f,.5f), 100f);
        }

        static Sprite MakeDichroicTriangle(int size)
        {
            var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){name="DichroicUiPlate",wrapMode=TextureWrapMode.Clamp};
            var px=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float u=(x+.5f)/size,v=(y+.5f)/size,half=(1-v)*.5f;
                float side=half-Mathf.Abs(u-.5f);if(side<0){px[y*size+x]=new Color32(0,0,0,0);continue;}
                Color c=Color.Lerp(Cyan,Magenta,u);c=Color.Lerp(c,Lime,(1-v)*.18f);
                float edge=Mathf.Clamp01(1-side*size/7f);float a=.10f+edge*.28f+(Mathf.Sin((u+v)*28f)*.5f+.5f)*.025f;
                px[y*size+x]=new Color32((byte)(c.r*255),(byte)(c.g*255),(byte)(c.b*255),(byte)(a*255));
            }
            tex.SetPixels32(px);tex.Apply(false,true);return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100f);
        }

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(.5f,.5f); rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        Image Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite)
        {
            var rt = R(name, parent, anchor, pos, size); var im = rt.gameObject.AddComponent<Image>();
            im.color = color; im.sprite = sprite; im.raycastTarget = false; return im;
        }

        TextMeshProUGUI Txt(string name, Transform parent, string text, Vector2 anchor, Vector2 pos, float size,
            Color color, float width = 360, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R(name, parent, anchor, pos, new Vector2(width, size * 2.15f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size;
            t.color = color; t.alignment = align; t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false; t.text = text; return t;
        }

        CanvasGroup Group(string name)
        {
            var rt = R(name, canvas.transform, new Vector2(.5f,.5f), Vector2.zero, new Vector2(390,844));
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            return rt.gameObject.AddComponent<CanvasGroup>();
        }

        void BuildUi()
        {
            canvas = MgfText.Canvas; canvasRt = (RectTransform)canvas.transform;
            discSprite = MakeDisc(64, false, false); ringSprite = MakeDisc(64, true, false); hookSprite = MakeDisc(96, false, true);

            titleGroup = Group("TitleWorldUI");
            var artRt = R("TitleKeyArt", titleGroup.transform, new Vector2(.5f,.5f), Vector2.zero, new Vector2(390,844));
            artRt.anchorMin = Vector2.zero; artRt.anchorMax = Vector2.one; artRt.sizeDelta = Vector2.zero;
            titleArt = artRt.gameObject.AddComponent<RawImage>();
            titleArt.texture = Resources.Load<Texture2D>("PrismMobil/title");
            titleArt.color = titleArt.texture ? Color.white : new Color(Aubergine.r,Aubergine.g,Aubergine.b,.02f);
            titleArt.raycastTarget = false;
            logoTxt = Txt("EtchedLogo", titleGroup.transform, "프리즘\n모빌", new Vector2(.5f,.82f), Vector2.zero, 54, Ivory, 330);
            logoTxt.fontStyle = FontStyles.Bold; logoTxt.characterSpacing = 5; logoTxt.outlineWidth = .18f; logoTxt.outlineColor = Magenta;
            taglineTxt = Txt("Tagline", titleGroup.transform, "한 점에 걸어 빛을 맞춰라", new Vector2(.5f,.68f), Vector2.zero, 17, Cyan, 340);
            unitTxt = Txt("Unit", titleGroup.transform, "중2 · 평행선과 무게중심", new Vector2(.5f,.12f), Vector2.zero, 13, Ivory, 340);
            bestTxt = Txt("Best", titleGroup.transform, "첫 시도 6개 이상이면 공연 완성", new Vector2(.5f,.085f), Vector2.zero, 12, Lime, 340);
            startHookGlow = Img("StartHalo", titleGroup.transform, new Vector2(.5f,.56f), Vector2.zero, new Vector2(104,104), new Color(Cyan.r,Cyan.g,Cyan.b,.28f), discSprite);
            var startHook = Img("StartHook", titleGroup.transform, new Vector2(.5f,.56f), Vector2.zero, new Vector2(82,82), Ivory, hookSprite);
            startHookRt = (RectTransform)startHook.transform;
            startTxt = Txt("StartVerb", titleGroup.transform, "갈고리를 눌러 공연 시작", new Vector2(.5f,.47f), Vector2.zero, 18, Ivory, 350);

            hudGroup = Group("Hud");
            progressTxt = Txt("Progress", hudGroup.transform, "MOBILE 0 / 7", new Vector2(.22f,.965f), Vector2.zero, 14, Cyan, 150, TextAlignmentOptions.Left);
            fusesTxt = Txt("Fuses", hudGroup.transform, "● ● ●", new Vector2(.78f,.965f), Vector2.zero, 16, Lime, 120);
            timeTxt = Txt("Time", hudGroup.transform, "105", new Vector2(.93f,.965f), Vector2.zero, 14, Ivory, 56);
            goalTxt = Txt("Goal", hudGroup.transform, "수학으로 한 점을 찾아 프리즘을 걸어라", new Vector2(.5f,.91f), Vector2.zero, 16, Ivory, 370);
            promptTxt = Txt("Prompt", hudGroup.transform, "", new Vector2(.5f,.80f), Vector2.zero, 17, Ivory, 360);
            promptTxt.fontStyle = FontStyles.Bold;
            promptTxt.rectTransform.sizeDelta = new Vector2(360, 112);

            diagramPlate = Img("DichroicDiagramPlate",hudGroup.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(300,300),Color.white,MakeDichroicTriangle(256));
            diagramPlateRt=(RectTransform)diagramPlate.transform;diagramPlateRt.SetAsFirstSibling();

            for (int i = 0; i < nodeImgs.Length; i++)
            {
                nodeImgs[i] = Img("Node" + i, hudGroup.transform, new Vector2(.5f,.5f), Vector2.zero,
                    new Vector2(9,9), new Color(Ivory.r,Ivory.g,Ivory.b,.72f), ringSprite);
                nodeRts[i] = (RectTransform)nodeImgs[i].transform; nodeImgs[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < diagramLines.Length; i++)
            {
                diagramLines[i] = Img("EtchLine" + i, hudGroup.transform, new Vector2(.5f,.5f), Vector2.zero,
                    new Vector2(20,2), i % 2 == 0 ? Cyan : Lime, discSprite);
                diagramLineRts[i] = (RectTransform)diagramLines[i].transform; diagramLines[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = Txt("DiagramLabel" + i, hudGroup.transform, "", new Vector2(.5f,.5f), Vector2.zero, 16, Ivory, 90);
                labelRts[i] = (RectTransform)labels[i].transform; labels[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < 2; i++)
            {
                tensionHandles[i] = Img("TensionHandle" + i, hudGroup.transform, new Vector2(.5f,.5f), Vector2.zero,
                    new Vector2(48,48), new Color(0,0,0,.78f), ringSprite);
                tensionHandleRts[i] = (RectTransform)tensionHandles[i].transform; tensionHandles[i].gameObject.SetActive(false);
            }
            trailImg = Img("HookTrail", hudGroup.transform, hookNorm, Vector2.zero, new Vector2(70,70), new Color(Magenta.r,Magenta.g,Magenta.b,.18f), hookSprite);
            trailRt = (RectTransform)trailImg.transform;
            hookImg = Img("AnswerHook", hudGroup.transform, hookNorm, Vector2.zero, new Vector2(72,72), Ivory, hookSprite);
            hookRt = (RectTransform)hookImg.transform;
            magnifierTxt = Txt("Magnifier", hudGroup.transform, "갈고리를 끌어 한 점에 놓으시오", new Vector2(.5f,.10f), Vector2.zero, 15, Cyan, 360);
            deviceTxt = Txt("OpticalRig", hudGroup.transform, "자기식 광축 짐벌이 판의 자세를 고정합니다", new Vector2(.5f,.055f), Vector2.zero, 11, Ivory, 360);
            guideImg = Img("GuideArrow", hudGroup.transform, new Vector2(.5f,.5f), Vector2.zero, new Vector2(80,80), new Color(Lime.r,Lime.g,Lime.b,.32f), ringSprite);
            guideRt = (RectTransform)guideImg.transform;
            for (int i = 0; i < guideDots.Length; i++)
            {
                guideDots[i] = Img("GuideStep" + i, hudGroup.transform, new Vector2(.5f,.5f), Vector2.zero,
                    new Vector2(15,15), new Color(Lime.r,Lime.g,Lime.b,.3f), discSprite);
                guideDotRts[i] = (RectTransform)guideDots[i].transform;
                guideDots[i].gameObject.SetActive(false);
            }

            feedbackGroup = Group("Feedback");
            feedbackRt = R("FeedbackPlate", feedbackGroup.transform, new Vector2(.5f,.19f), Vector2.zero, new Vector2(350,96));
            var feedBg = feedbackRt.gameObject.AddComponent<Image>(); feedBg.color = new Color(.05f,.02f,.10f,.91f); feedBg.sprite = null; feedBg.raycastTarget = false;
            feedbackTxt = Txt("FeedbackText", feedbackRt, "", new Vector2(.5f,.5f), Vector2.zero, 17, Ivory, 324);
            feedbackGroup.alpha = 0;

            endGroup = Group("End");
            var endGlow = Img("EndTriangleGlow", endGroup.transform, new Vector2(.5f,.57f), Vector2.zero, new Vector2(330,330), new Color(Magenta.r,Magenta.g,Magenta.b,.17f), ringSprite);
            endGlow.transform.localRotation = Quaternion.Euler(0,0,45);
            endTitleTxt = Txt("EndTitle", endGroup.transform, "공연 완성", new Vector2(.5f,.73f), Vector2.zero, 38, Ivory, 360);
            endTitleTxt.outlineWidth = .14f; endTitleTxt.outlineColor = Magenta;
            endScoreTxt = Txt("EndScore", endGroup.transform, "0", new Vector2(.5f,.58f), Vector2.zero, 64, Lime, 360);
            endStatsTxt = Txt("EndStats", endGroup.transform, "", new Vector2(.5f,.47f), Vector2.zero, 18, Cyan, 360);
            Img("EndReasonPlate",endGroup.transform,new Vector2(.5f,.35f),Vector2.zero,new Vector2(360,94),new Color(.035f,.012f,.075f,.84f),null);
            endHintTxt = Txt("EndHint", endGroup.transform, "", new Vector2(.5f,.35f), Vector2.zero, 15, Ivory, 350);
            endHintTxt.rectTransform.sizeDelta = new Vector2(350, 86);
            var ctaBg = Img("RestartRing", endGroup.transform, new Vector2(.5f,.16f), Vector2.zero, new Vector2(310,68), new Color(Cyan.r,Cyan.g,Cyan.b,.24f), discSprite);
            endCtaRt = (RectTransform)ctaBg.transform;
            endCtaTxt = Txt("Restart", endGroup.transform, "다시 모빌 걸기", new Vector2(.5f,.16f), Vector2.zero, 20, Ivory, 310);

            hudGroup.alpha = 0; endGroup.alpha = 0;
            ApplyResponsiveLayout();
        }

        void BuildWorld()
        {
            // GameObject.CreatePrimitive가 WebGL 관리 코드 스트리핑 뒤에도 콜라이더를 만들 수 있게 타입을 보존한다.
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<SphereCollider>();
            colliderTypes.AddComponent<CapsuleCollider>();
            Destroy(colliderTypes);
            MgfLook.Sky(MgfLook.Hex("#090512"), Aubergine, MgfLook.Hex("#09040E"), .42f);
            MgfLook.Sun(new Vector3(48,-26,18), MgfLook.Hex("#E6D3FF"), .74f, .72f);
            cam = MgfLook.Camera(new Vector3(11.5f,9.8f,-16f), new Vector3(0,2.0f,2.8f), 34f);
            cam.orthographic = true; cam.orthographicSize = 7.4f;
            worldRoot = new GameObject("PrismTheatre").transform;
            var tex = Resources.Load<Texture2D>("PrismMobil/dichroic-material");
            prismMat = MgfLook.Alpha(new Color(.34f,.14f,.54f,.38f), tex);
            prismEdgeMat = MgfLook.Lit(Magenta, .72f, .18f, Magenta * 1.65f);
            cyanMat = MgfLook.Additive(new Color(Cyan.r,Cyan.g,Cyan.b,.7f));
            limeMat = MgfLook.Additive(new Color(Lime.r,Lime.g,Lime.b,.64f));
            magentaMat = MgfLook.Additive(new Color(Magenta.r,Magenta.g,Magenta.b,.62f));
            threadMat = MgfLook.Unlit(Ink);
            fuseOff = MgfLook.Lit(MgfLook.Hex("#231B30"), .14f, .5f);
            fuseOn[0] = MgfLook.Lit(Cyan,.65f,.1f,Cyan*1.5f); fuseOn[1] = MgfLook.Lit(Magenta,.65f,.1f,Magenta*1.5f); fuseOn[2] = MgfLook.Lit(Lime,.65f,.1f,Lime*1.5f);

            MgfLook.Block("CycloramaFloor", new Vector3(0,-.65f,4.2f), new Vector3(22,1.1f,25), .85f,
                MgfLook.Lit(MgfLook.Hex("#130A23"),.54f,.15f), worldRoot);
            MgfLook.Block("CurvedBack", new Vector3(0,4.1f,12.4f), new Vector3(22,10,.65f), .25f,
                MgfLook.Lit(MgfLook.Hex("#210F35"),.32f,.08f), worldRoot);
            for (int s = -1; s <= 1; s++)
            {
                var spot = new GameObject("PrismSpot" + s).AddComponent<Light>();
                spot.transform.SetParent(worldRoot, false); spot.transform.localPosition = new Vector3(s * 5.7f, 7.8f, -1.5f);
                spot.transform.LookAt(new Vector3(s * 1.8f, 0, 3.5f)); spot.type = LightType.Spot; spot.range = 22f;
                spot.spotAngle = 34f; spot.intensity = .58f; spot.color = s < 0 ? Cyan : s > 0 ? Lime : Magenta;
                spot.shadows = LightShadows.None; spot.renderMode = LightRenderMode.ForcePixel;
            }
            for (int i = 0; i < 3; i++)
            {
                var bulb = MgfLook.Prim(PrimitiveType.Sphere,"FuseBulb"+i,new Vector3(-1.6f+i*1.6f,6.35f,7.4f),new Vector3(.28f,.28f,.28f),fuseOn[i],worldRoot);
                fuseBulbs[i] = bulb.GetComponent<Renderer>();
                MgfLook.Block("FuseSocket"+i,new Vector3(-1.6f+i*1.6f,6.63f,7.4f),new Vector3(.5f,.22f,.5f),.09f,
                    MgfLook.Lit(MgfLook.Hex("#30283C"),.4f,.7f),worldRoot);
            }
            for (int i = 0; i < 7; i++)
            {
                float x = (i-3)*2.35f;
                titleMobiles[i] = MakePrism("SuspendedPrism"+i,new Vector3(x,3.8f+((i%2)*.8f),6.8f+Mathf.Abs(i-3)*.28f),.72f+(i%3)*.13f);
                titleMobiles[i].localRotation = Quaternion.Euler(0,(i-3)*12f,0);
                shadowPieces[i] = MakeFlatTriangle("Caustic"+i,new Vector3((i-3)*1.15f,-.03f,2.6f+(i%2)*1.35f),2.0f,
                    i%3==0?magentaMat:i%3==1?cyanMat:limeMat);
                shadowPieces[i].localScale = Vector3.zero;
            }
            activePrism = MakePrism("ActivePrism",new Vector3(0,2.65f,1.5f),2.15f);
            activeHook = MgfLook.Prim(PrimitiveType.Cylinder,"HookAnchor",new Vector3(0,5.35f,1.5f),new Vector3(.34f,.18f,.34f),
                MgfLook.Lit(Ivory,.72f,.72f),worldRoot).transform;
            MgfLook.Block("OpticalGimbalX",new Vector3(0,5.18f,1.5f),new Vector3(1.25f,.10f,.12f),.05f,
                MgfLook.Lit(Cyan,.76f,.65f,Cyan*.45f),worldRoot);
            MgfLook.Block("OpticalGimbalZ",new Vector3(0,5.18f,1.5f),new Vector3(.12f,.10f,1.25f),.05f,
                MgfLook.Lit(Magenta,.76f,.65f,Magenta*.45f),worldRoot);
            hookThread = new GameObject("BlackTensionThread").AddComponent<LineRenderer>();
            hookThread.transform.SetParent(worldRoot,false); hookThread.sharedMaterial=threadMat; hookThread.positionCount=2;
            hookThread.startWidth=hookThread.endWidth=.055f; hookThread.useWorldSpace=true;
            hookThread.SetPosition(0,new Vector3(0,8,1.5f)); hookThread.SetPosition(1,activeHook.position);
        }

        Transform MakePrism(string name, Vector3 pos, float scale)
        {
            var root = new GameObject(name).transform; root.SetParent(worldRoot,false); root.localPosition=pos;
            var face = MakeTriangleObject(name+"Face",Vector3.zero,scale,prismMat,root,.08f);
            face.transform.localRotation = Quaternion.Euler(90,0,0);
            Vector3[] a={new Vector3(0,1.25f,0),new Vector3(-1.08f,-.72f,0),new Vector3(1.08f,-.72f,0)};
            for(int i=0;i<3;i++) MakeWorldLine(name+"Edge"+i,a[i]*scale,a[(i+1)%3]*scale,.065f,prismEdgeMat,root);
            MakeWorldLine(name+"Median",a[0]*scale,(a[1]+a[2])*.5f*scale,.025f,cyanMat,root);
            return root;
        }

        GameObject MakeTriangleObject(string name, Vector3 pos, float scale, Material mat, Transform parent, float depth)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=pos;
            var mesh=new Mesh{name=name+"Mesh"};
            mesh.vertices=new[]{new Vector3(0,1.25f,0)*scale,new Vector3(-1.08f,-.72f,0)*scale,new Vector3(1.08f,-.72f,0)*scale};
            mesh.triangles=new[]{0,1,2}; mesh.uv=new[]{new Vector2(.5f,1),new Vector2(0,0),new Vector2(1,0)}; mesh.RecalculateNormals();
            go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=mat;
            return go;
        }

        Transform MakeFlatTriangle(string name, Vector3 pos, float scale, Material mat)
        {
            var go=MakeTriangleObject(name,pos,scale,mat,worldRoot,.02f); go.transform.localRotation=Quaternion.Euler(90,0,0); return go.transform;
        }

        void MakeWorldLine(string name, Vector3 a, Vector3 b, float width, Material mat, Transform parent)
        {
            var lr=new GameObject(name).AddComponent<LineRenderer>(); lr.transform.SetParent(parent,false); lr.sharedMaterial=mat;
            lr.useWorldSpace=false; lr.positionCount=2; lr.startWidth=lr.endWidth=width; lr.SetPosition(0,a); lr.SetPosition(1,b);
        }

        GameObject MakeCone(string name, Vector3 top, Vector3 bottom, float radius, Material mat)
        {
            const int seg=18; var v=new Vector3[seg+1]; var tri=new int[seg*3]; v[0]=top;
            for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg;v[i+1]=bottom+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);tri[i*3]=0;tri[i*3+1]=i+1;tri[i*3+2]=(i+1)%seg+1;}
            var mesh=new Mesh{name=name+"Mesh",vertices=v,triangles=tri};mesh.RecalculateNormals();
            var go=new GameObject(name);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;return go;
        }

        void ShowTitle()
        {
            phase=GamePhase.Title; current=PrismMobilRules.Practice(); dragMode=DragMode.None; runSeconds=0; endReason="";
            st.score=0;st.lives=3;st.level=1;st.phase="title";st.solved=0;st.problemId=current.id;st.selectedNode=-1;
            st.firstAttemptTotal=0;st.firstAttemptCorrect=0;st.attemptIndex=0;st.firstMisses=0;st.fuses=3;st.tensionLines=0;
            st.pointerVersion=0;st.onboarding=false;st.misconceptionId="";
            for(int i=0;i<7;i++){shadowPieces[i].localScale=Vector3.zero;titleMobiles[i].gameObject.SetActive(true);}
            activePrism.gameObject.SetActive(false);activeHook.gameObject.SetActive(false);hookThread.gameObject.SetActive(false);
            SetGroups(1,0,0,0); RefreshDiagram(); MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++; rng=new System.Random(unchecked(Environment.TickCount+runSerial*104729));
            parallelCursor=rng.Next(parallelDeck.Count);medianCursor=rng.Next(medianDeck.Count);centroidCursor=rng.Next(centroidDeck.Count);
            st.score=0;st.lives=3;st.level=1;st.solved=0;st.firstAttemptTotal=0;st.firstAttemptCorrect=0;
            st.attemptIndex=0;st.firstMisses=0;st.fuses=3;st.tensionLines=0;st.selectedNode=-1;st.misconceptionId="";
            firstAttempt=true;runSeconds=0;shownScore=-1;displayedScore=0;
            for(int i=0;i<7;i++){shadowPieces[i].localScale=Vector3.zero;titleMobiles[i].gameObject.SetActive(false);}
            activePrism.gameObject.SetActive(true);activeHook.gameObject.SetActive(true);hookThread.gameObject.SetActive(true);
            RefreshFuses();
        }

        void StartPractice()
        {
            ResetRun(); phase=GamePhase.Practice; current=PrismMobilRules.Practice(); firstAttempt=true;idleHintT=0;
            st.phase="playing";st.onboarding=true;st.problemId=current.id;st.selectedNode=-1;st.tensionLines=0;
            hookNorm=new Vector2(.5f,.70f);trailNorm=hookNorm;guideRt.gameObject.SetActive(true);
            SetGroups(0,1,0,0);RefreshDiagram();ConfigureGuide(hookNorm,LineNodeNorm(current.answerNode));MgfSfx.Play("whoosh",.34f);MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun(); phase=GamePhase.Playing;st.phase="playing";st.onboarding=false;SetGroups(0,1,0,0);NextProblem();
        }

        void BeginRunAfterPractice()
        {
            st.score=0;st.solved=0;st.firstAttemptTotal=0;st.firstAttemptCorrect=0;st.firstMisses=0;st.fuses=3;st.lives=3;
            phase=GamePhase.Playing;st.phase="playing";st.onboarding=false;runSeconds=0;NextProblem();
        }

        void NextProblem()
        {
            if(st.solved>=7){EndRun(true,"일곱 장의 빛이 하나의 삼각형이 되었습니다.");return;}
            int band=st.solved<2?1:st.solved<5?2:3;st.level=band;st.attemptIndex=0;st.selectedNode=-1;st.misconceptionId="";st.tensionLines=0;firstAttempt=true;
            if(band==1)
            {
                // 2..22 정답 눈금을 21회마다 정확히 한 번씩 쓰는 균형 순환 풀.
                // 시간 시드 독립 표본은 200회 게이트에서 우연한 편향이 커져, 시각·초기 갈고리와 독립인
                // 이 순열을 사용한다. 2는 21과 서로소라 모든 후보를 방문하며 위치 암기용 짧은 주기가 없다.
                int k = 2 + PositiveMod(runSerial * 2 + 16 + st.solved * 7, 21);
                int scaleIndex = PositiveMod(runSerial * 5 + st.solved * 3, 8);
                current = parallelDeck[(k - 2) * 8 + scaleIndex];
                parallelCursor++;
            }
            else if(band==2)current=medianDeck[(medianCursor++*11+runSerial*5)%medianDeck.Count];
            else current=centroidDeck[(centroidCursor++*13+runSerial*3)%centroidDeck.Count];
            st.problemId=current.id;hookNorm=landLayout?Stage(.52f,.92f):new Vector2(.5f,.74f);trailNorm=hookNorm;dragMode=DragMode.None;idleHintT=0;
            HideGuide();
            RefreshDiagram();MgfBridge.NotifyChanged();
        }

        static int PositiveMod(int value,int mod){int r=value%mod;return r<0?r+mod:r;}

        void HandleInput()
        {
            if(!MgfPointer.Down&&!MgfPointer.Held&&!MgfPointer.Up)return;
            Vector2 n=new Vector2(MgfPointer.Position.x/Mathf.Max(1,Screen.width),MgfPointer.Position.y/Mathf.Max(1,Screen.height));
            if(MgfPointer.Down)
            {
                st.pointerVersion++;MgfBridge.NotifyChanged();downNorm=n;MgfSfx.Play("tap",.18f);
                if(phase==GamePhase.Title)
                {
                    titlePressT=.28f;MgfFx.Punch(startHookRt,.16f,.22f);
                    if(n.x>.22f&&n.x<.78f&&n.y>.30f&&n.y<.82f)StartPractice();
                    return;
                }
                if(phase==GamePhase.End)
                {
                    if(n.y>.08f&&n.y<.25f)StartPractice();
                    else { MgfFx.Punch(endCtaRt,.10f,.18f); MgfSfx.Play("tap",.12f); }
                    return;
                }
                if(phase!=GamePhase.Practice&&phase!=GamePhase.Playing)return;
                if(current.kind==PrismProblemKind.Centroid&&st.tensionLines<2)
                {
                    Vector2 h1,h2,t1,t2;CentroidConstruction(out h1,out t1,out h2,out t2);
                    if(st.tensionLines==0&&Vector2.Distance(n,h1)<.10f){dragMode=DragMode.TensionOne;HideGuide();return;}
                    if(st.tensionLines==1&&Vector2.Distance(n,h2)<.10f){dragMode=DragMode.TensionTwo;HideGuide();return;}
                    Refuse(n,"검은 고리에서 꼭짓점까지 장력실 두 줄을 먼저 그으세요.");return;
                }
                if(IsInsideStage(n)){dragMode=DragMode.Hook;HideGuide();hookNorm=n;MgfFx.Punch(hookRt,.12f,.18f);}
                else Refuse(n,"갈고리를 프리즘의 눈금 위로 끌어 놓으세요.");
            }
            if(MgfPointer.Held)
            {
                if(dragMode==DragMode.Hook){trailNorm=Vector2.Lerp(trailNorm,hookNorm,.22f);hookNorm=n;idleHintT=0;}
                else if(dragMode==DragMode.TensionOne||dragMode==DragMode.TensionTwo){hookNorm=n;}
            }
            if(MgfPointer.Up)
            {
                if(dragMode==DragMode.Hook){dragMode=DragMode.None;ReleaseHook(n);}
                else if(dragMode==DragMode.TensionOne||dragMode==DragMode.TensionTwo){var m=dragMode;dragMode=DragMode.None;ReleaseTension(m,n);}
            }
        }

        void ReleaseTension(DragMode mode,Vector2 n)
        {
            Vector2 h1,h2,t1,t2;CentroidConstruction(out h1,out t1,out h2,out t2);
            Vector2 target=mode==DragMode.TensionOne?t1:t2;
            if(Vector2.Distance(n,target)<.16f)
            {
                st.tensionLines=Mathf.Min(2,st.tensionLines+1);MgfSfx.Play("pop",.30f);RefreshDiagram();MgfBridge.NotifyChanged();
                if(st.tensionLines==2)magnifierTxt.text="두 중선의 교점에 갈고리를 놓으시오";
            }
            else Refuse(n,"실 끝을 반대편 꼭짓점 고리까지 당기세요.");
        }

        void ReleaseHook(Vector2 n)
        {
            if(!IsInsideStage(n,.05f)){Refuse(n,"판 밖입니다. 점선이 닿는 가까운 눈금에 놓으세요.");return;}
            int selected;
            if(current.kind==PrismProblemKind.Centroid)
            {
                Vector2 local=StageInverse(n);
                int x=Mathf.Clamp(Mathf.RoundToInt((local.x-.05f)/.90f*12f),0,12);
                int y=Mathf.Clamp(Mathf.RoundToInt((local.y-.08f)/.84f*12f),0,12);
                selected=x*13+y;hookNorm=GridNorm(x,y);
            }
            else
            {
                Vector2 a=LineNodeNorm(0),b=LineNodeNorm(current.maxNode);float t,dist;
                ProjectToSegment(n,a,b,out t,out dist);
                if(dist>(landLayout?.09f:.105f)){Refuse(n,"눈금선 가까이에 갈고리를 놓으세요.");return;}
                selected=Mathf.Clamp(Mathf.RoundToInt(t*current.maxNode),0,current.maxNode);
                hookNorm=LineNodeNorm(selected);
            }
            trailNorm=hookNorm;st.selectedNode=selected;JudgeSelection();
        }

        void JudgeSelection()
        {
            if(phase!=GamePhase.Practice&&phase!=GamePhase.Playing)return;
            revealCorrect=PrismMobilRules.IsCorrect(current,st.selectedNode);bool wasPractice=phase==GamePhase.Practice;
            if(wasPractice)
            {
                if(revealCorrect){st.score=100;st.solved=1;st.misconceptionId="";}
                else{st.misconceptionId=PrismMobilRules.Diagnose(current,st.selectedNode);firstAttempt=false;st.attemptIndex++;}
            }
            else
            {
                if(firstAttempt){st.firstAttemptTotal++;if(revealCorrect)st.firstAttemptCorrect++;else st.firstMisses++;}
                if(revealCorrect){st.score+=firstAttempt?140:45;st.solved++;st.misconceptionId="";ShowSolvedPiece(st.solved-1);}
                else
                {
                    st.fuses--;st.lives=st.fuses;st.attemptIndex++;st.misconceptionId=PrismMobilRules.Diagnose(current,st.selectedNode);
                    firstAttempt=false;RefreshFuses();
                }
            }
            phase=GamePhase.Reveal;st.phase="paused";revealT=0;hitStopT=revealCorrect?.10f:0;
            feedbackTxt.text=FeedbackFor(revealCorrect);feedbackTxt.color=revealCorrect?Lime:Ivory;feedbackGroup.alpha=1;
            magnifierTxt.alpha=0;
            if(revealCorrect){MgfSfx.Play(wasPractice?"correct":"correct",.48f);MgfFx.Punch(hookRt,.22f,.34f);prismGlow=1;}
            else{MgfSfx.Play("wrong",.38f);MgfFx.Shake(cam,.085f,.26f);prismTilt=st.selectedNode%2==0?-11f:11f;}
            RefreshDiagram();MgfBridge.NotifyChanged();
        }

        string FeedbackFor(bool correct)
        {
            if(correct)
            {
                if(current.kind==PrismProblemKind.Median)return "AG:GD=2:1 · 꼭짓점 A에서 "+current.answerNumeric+" cm";
                if(current.kind==PrismProblemKind.Parallel)return "AD:DB=AE:EC · "+current.answer+"에 빛이 잠겼습니다.";
                return "두 중선의 교점 G="+current.answer+" · 그림자가 이어졌습니다.";
            }
            if(st.misconceptionId==PrismMobilRules.MedianMidpoint)return "중점이 아닙니다. A→D를 3등분해 꼭짓점 쪽 두 구간을 보세요.";
            if(st.misconceptionId==PrismMobilRules.ReverseTwoToOne)return "2:1의 순서가 반대입니다. AG가 GD의 2배입니다.";
            if(st.misconceptionId==PrismMobilRules.MixPartWhole)return "부분:부분과 부분:전체가 섞였습니다. AD:DB=AE:EC입니다.";
            if(st.misconceptionId==PrismMobilRules.ShapeScreenCenter)return "화면 가운데가 아닙니다. 두 중선이 만나는 점을 찾으세요.";
            return current.kind==PrismProblemKind.Centroid?"선택점에서 그림자가 갈라졌습니다. 두 중선의 교점을 확인하세요.":"선택한 눈금에서 판이 기울었습니다. 표시된 비를 다시 확인하세요.";
        }

        void FinishReveal()
        {
            feedbackGroup.alpha=0;magnifierTxt.alpha=1;prismTilt=0;
            if(st.onboarding)
            {
                if(revealCorrect){BeginRunAfterPractice();return;}
                phase=GamePhase.Practice;st.phase="playing";guideRt.gameObject.SetActive(true);RefreshDiagram();return;
            }
            if(!revealCorrect)
            {
                if(st.firstMisses>=2){EndRun(false,"첫 시도 실패가 2회가 되어 6/7 기준에 닿을 수 없습니다.");return;}
                if(st.fuses<=0){EndRun(false,"세 개의 무대 퓨즈가 모두 꺼졌습니다.");return;}
                phase=GamePhase.Playing;st.phase="playing";RefreshDiagram();MgfBridge.NotifyChanged();return;
            }
            phase=GamePhase.Playing;st.phase="playing";NextProblem();
        }

        void EndRun(bool clear,string reason)
        {
            phase=GamePhase.End;st.phase=clear?"clear":"gameover";endReason=reason;dragMode=DragMode.None;
            endTitleTxt.text=clear?"빛의 모빌 완성":"공연 중단";endTitleTxt.color=clear?Ivory:Magenta;
            endStatsTxt.text="첫 시도 "+st.firstAttemptCorrect+" / "+Mathf.Max(1,st.firstAttemptTotal)+"   ·   프리즘 "+st.solved+" / 7";
            endHintTxt.text=reason;endCtaTxt.text=clear?"한 번 더 맞추기":"다시 모빌 걸기";
            SetGroups(0,0,1,0);if(clear){MgfSfx.Play("win",.55f);prismGlow=2;}else MgfSfx.Play("lose",.45f);
            activePrism.gameObject.SetActive(true);activeHook.gameObject.SetActive(true);hookThread.gameObject.SetActive(true);
            MgfBridge.NotifyChanged();
        }

        void Refuse(Vector2 n,string msg)
        {
            refuseT=.72f;feedbackTxt.text=msg;feedbackTxt.color=Ivory;feedbackGroup.alpha=1;
            ConfigureGuide(n,hookNorm);MgfSfx.Play("wrong",.16f);MgfFx.Punch(hookRt,.08f,.20f);
        }

        Vector2 Stage(float x,float y)=>new Vector2(stageRect.x+stageRect.width*x,stageRect.y+stageRect.height*y);
        Vector2 StageInverse(Vector2 n)=>new Vector2((n.x-stageRect.x)/stageRect.width,(n.y-stageRect.y)/stageRect.height);
        bool IsInsideStage(Vector2 n,float pad=0f)=>n.x>=stageRect.x-pad&&n.x<=stageRect.xMax+pad&&n.y>=stageRect.y-pad&&n.y<=stageRect.yMax+pad;
        Vector2 GridNorm(int x,int y)=>Stage(.05f+.90f*x/12f,.08f+.84f*y/12f);

        Vector2 LineNodeNorm(int index)
        {
            float t=index/(float)Mathf.Max(1,current.maxNode);
            if(current.kind==PrismProblemKind.Parallel)return Vector2.Lerp(Stage(.50f,.90f),Stage(.10f,.10f),t);
            return Vector2.Lerp(Stage(.50f,.90f),Stage(.50f,.10f),t);
        }

        static void ProjectToSegment(Vector2 p,Vector2 a,Vector2 b,out float t,out float distance)
        {
            Vector2 ab=b-a;float d=Mathf.Max(.0001f,Vector2.Dot(ab,ab));
            t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/d);distance=Vector2.Distance(p,a+ab*t);
        }

        void CentroidConstruction(out Vector2 mBC,out Vector2 a,out Vector2 mAC,out Vector2 b)
        {
            a=GridNorm(current.ax,current.ay);b=GridNorm(current.bx,current.by);Vector2 c=GridNorm(current.cx,current.cy);
            mBC=(b+c)*.5f;mAC=(a+c)*.5f;
        }

        void ConfigureGuide(Vector2 from,Vector2 to)
        {
            guideVisible=true;guideClock=0;guideFromNorm=from;guideToNorm=to;guideRt.gameObject.SetActive(true);
            guideRt.anchorMin=guideRt.anchorMax=from;
            for(int i=0;i<guideDots.Length;i++)guideDots[i].gameObject.SetActive(true);
        }

        void HideGuide()
        {
            guideVisible=false;guideRt.gameObject.SetActive(false);
            for(int i=0;i<guideDots.Length;i++)guideDots[i].gameObject.SetActive(false);
        }

        void ApplyResponsiveLayout()
        {
            landLayout=Screen.width>Screen.height*1.16f;
            if(landLayout)
            {
                stageRect=new Rect(.34f,.13f,.48f,.76f);
                goalTxt.rectTransform.anchorMin=goalTxt.rectTransform.anchorMax=new Vector2(.165f,.88f);
                promptTxt.rectTransform.anchorMin=promptTxt.rectTransform.anchorMax=new Vector2(.165f,.65f);
                magnifierTxt.rectTransform.anchorMin=magnifierTxt.rectTransform.anchorMax=new Vector2(.165f,.32f);
                deviceTxt.rectTransform.anchorMin=deviceTxt.rectTransform.anchorMax=new Vector2(.165f,.21f);
                float leftW=250f;
                goalTxt.rectTransform.sizeDelta=new Vector2(leftW,72);promptTxt.rectTransform.sizeDelta=new Vector2(leftW,230);
                magnifierTxt.rectTransform.sizeDelta=new Vector2(leftW,92);deviceTxt.rectTransform.sizeDelta=new Vector2(leftW,64);
                goalTxt.fontSize=12;promptTxt.fontSize=13;magnifierTxt.fontSize=11;deviceTxt.fontSize=9;
                goalTxt.alignment=promptTxt.alignment=magnifierTxt.alignment=deviceTxt.alignment=TextAlignmentOptions.Left;
                progressTxt.rectTransform.anchorMin=progressTxt.rectTransform.anchorMax=new Vector2(.91f,.88f);
                fusesTxt.rectTransform.anchorMin=fusesTxt.rectTransform.anchorMax=new Vector2(.91f,.78f);
                timeTxt.rectTransform.anchorMin=timeTxt.rectTransform.anchorMax=new Vector2(.91f,.68f);
                feedbackRt.anchorMin=feedbackRt.anchorMax=new Vector2(.165f,.11f);
                feedbackRt.sizeDelta=new Vector2(leftW,100);
            }
            else
            {
                stageRect=new Rect(.10f,.27f,.80f,.45f);
                goalTxt.rectTransform.anchorMin=goalTxt.rectTransform.anchorMax=new Vector2(.5f,.91f);
                promptTxt.rectTransform.anchorMin=promptTxt.rectTransform.anchorMax=new Vector2(.5f,.80f);
                magnifierTxt.rectTransform.anchorMin=magnifierTxt.rectTransform.anchorMax=new Vector2(.5f,.105f);
                deviceTxt.rectTransform.anchorMin=deviceTxt.rectTransform.anchorMax=new Vector2(.5f,.055f);
                goalTxt.rectTransform.sizeDelta=new Vector2(370,60);promptTxt.rectTransform.sizeDelta=new Vector2(360,112);
                magnifierTxt.rectTransform.sizeDelta=new Vector2(360,64);deviceTxt.rectTransform.sizeDelta=new Vector2(360,48);
                goalTxt.fontSize=16;promptTxt.fontSize=17;magnifierTxt.fontSize=15;deviceTxt.fontSize=11;
                goalTxt.alignment=promptTxt.alignment=magnifierTxt.alignment=deviceTxt.alignment=TextAlignmentOptions.Center;
                progressTxt.rectTransform.anchorMin=progressTxt.rectTransform.anchorMax=new Vector2(.21f,.965f);
                fusesTxt.rectTransform.anchorMin=fusesTxt.rectTransform.anchorMax=new Vector2(.64f,.965f);
                timeTxt.rectTransform.anchorMin=timeTxt.rectTransform.anchorMax=new Vector2(.78f,.965f);
                feedbackRt.anchorMin=feedbackRt.anchorMax=new Vector2(.5f,.19f);feedbackRt.sizeDelta=new Vector2(350,96);
            }
            diagramPlateRt.anchorMin=new Vector2(stageRect.x,stageRect.y);diagramPlateRt.anchorMax=new Vector2(stageRect.xMax,stageRect.yMax);
            diagramPlateRt.anchoredPosition=Vector2.zero;diagramPlateRt.sizeDelta=Vector2.zero;
        }

        void SetGroups(float title,float hud,float end,float feedback)
        {
            titleGroup.alpha=title;titleGroup.blocksRaycasts=title>0;hudGroup.alpha=hud;hudGroup.blocksRaycasts=hud>0;
            endGroup.alpha=end;endGroup.blocksRaycasts=end>0;feedbackGroup.alpha=feedback;
        }

        void RefreshDiagram()
        {
            for(int i=0;i<169;i++)nodeImgs[i].gameObject.SetActive(false);
            for(int i=0;i<18;i++)diagramLines[i].gameObject.SetActive(false);
            for(int i=0;i<12;i++)labels[i].gameObject.SetActive(false);
            for(int i=0;i<2;i++)tensionHandles[i].gameObject.SetActive(false);
            if(current==null)return;
            promptTxt.text=current.prompt;
            goalTxt.text=st.onboarding?"첫 연습 · 갈고리를 4 cm 정렬점으로 끌어라":"광축 짐벌에서 수학으로 정렬점 하나를 찾아라";
            if(current.kind==PrismProblemKind.Centroid)SetupCentroidDiagram();else SetupLineDiagram();
            hookRt.anchorMin=hookRt.anchorMax=hookNorm;trailRt.anchorMin=trailRt.anchorMax=trailNorm;
        }

        void SetupLineDiagram()
        {
            int max=current.maxNode;
            Vector2 a=LineNodeNorm(0),dOrB=LineNodeNorm(max);
            for(int i=0;i<=max&&i<169;i++)
            {
                Vector2 point=LineNodeNorm(i);nodeImgs[i].gameObject.SetActive(true);nodeRts[i].anchorMin=nodeRts[i].anchorMax=point;
                float size=(i==0||i==max||i==current.answerNode&&phase==GamePhase.Reveal)?18:(i%(Mathf.Max(1,max/3))==0?13:8);
                nodeRts[i].sizeDelta=new Vector2(size,size);nodeImgs[i].color=phase==GamePhase.Reveal&&i==current.answerNode?Lime:new Color(Ivory.r,Ivory.g,Ivory.b,.72f);
            }
            if(current.kind==PrismProblemKind.Parallel)
            {
                Vector2 c=Stage(.90f,.10f),e=Vector2.Lerp(a,c,current.answerNode/(float)max);
                SetLine(0,a,dOrB,Cyan,4);SetLine(1,a,c,Magenta,4);SetLine(2,dOrB,c,Lime,4);
                SetLabel(0,"A",a+new Vector2(0,.035f));SetLabel(1,"B",dOrB+new Vector2(-.025f,-.025f));SetLabel(2,"C",c+new Vector2(.025f,-.025f));
                SetLabel(3,"AE="+current.aeLength+" cm",Vector2.Lerp(a,e,.48f)+new Vector2(.035f,0));
                SetLabel(4,"EC="+current.ecLength+" cm",Vector2.Lerp(e,c,.52f)+new Vector2(.035f,0));
                SetLabel(5,"DE∥BC",Stage(.70f,.03f));
                if(phase==GamePhase.Reveal)
                {
                    Vector2 answer=LineNodeNorm(current.answerNode);SetLine(3,answer,e,Cyan,5);
                    SetLabel(6,"D",answer+new Vector2(-.025f,.018f));SetLabel(7,"E",e+new Vector2(.025f,.018f));
                }
                magnifierTxt.text="AE:EC를 이용해 AB의 24등분 눈금에서 D를 찾으시오";
            }
            else
            {
                Vector2 b=Stage(.10f,.10f),c=Stage(.90f,.10f);
                SetLine(0,a,b,Magenta,3);SetLine(1,a,c,Magenta,3);SetLine(2,b,c,Lime,3);SetLine(3,a,dOrB,Cyan,5);
                SetLabel(0,"A",a+new Vector2(0,.035f));SetLabel(1,"D",dOrB+new Vector2(0,-.03f));
                SetLabel(2,"AD="+current.totalLength+" cm",Stage(.65f,.52f));
                magnifierTxt.text="AG:GD=2:1 · 갈고리를 계산한 cm 눈금에 놓으시오";
            }
            if(st.onboarding&&phase==GamePhase.Practice&&refuseT<=0&&!guideVisible)ConfigureGuide(hookNorm,LineNodeNorm(current.answerNode));
        }

        void SetupCentroidDiagram()
        {
            int n=0;for(int y=0;y<=12;y++)for(int x=0;x<=12;x++)
            {
                nodeImgs[n].gameObject.SetActive(true);nodeRts[n].anchorMin=nodeRts[n].anchorMax=GridNorm(x,y);nodeRts[n].sizeDelta=(x%3==0&&y%3==0)?new Vector2(10,10):new Vector2(6,6);
                nodeImgs[n].color=phase==GamePhase.Reveal&&x==current.gx&&y==current.gy?Lime:new Color(Ivory.r,Ivory.g,Ivory.b,.38f);n++;
            }
            Vector2 a=GridNorm(current.ax,current.ay),b=GridNorm(current.bx,current.by),c=GridNorm(current.cx,current.cy);
            SetLine(0,a,b,Magenta,3);SetLine(1,b,c,Cyan,3);SetLine(2,c,a,Lime,3);
            SetLabel(0,"A",a+new Vector2(0,.035f));SetLabel(1,"B",b+new Vector2(.02f,-.03f));SetLabel(2,"C",c+new Vector2(-.02f,-.03f));
            Vector2 m1,t1,m2,t2;CentroidConstruction(out m1,out t1,out m2,out t2);
            tensionHandles[0].gameObject.SetActive(st.tensionLines<1);tensionHandleRts[0].anchorMin=tensionHandleRts[0].anchorMax=m1;
            tensionHandles[1].gameObject.SetActive(st.tensionLines==1);tensionHandleRts[1].anchorMin=tensionHandleRts[1].anchorMax=m2;
            if(st.tensionLines>=1)SetLine(3,a,m1,Ink,5);if(st.tensionLines>=2)SetLine(4,b,m2,Ink,5);
            magnifierTxt.text=st.tensionLines<2?"검은 중점 고리에서 반대 꼭짓점까지 장력실을 당기시오":"두 중선의 교점에 갈고리를 놓으시오";
            if(st.tensionLines<2&&refuseT<=0)ConfigureGuide(st.tensionLines==0?m1:m2,st.tensionLines==0?t1:t2);
        }

        void SetLine(int index,Vector2 a,Vector2 b,Color color,float thick)
        {
            if(index<0||index>=diagramLines.Length)return;var rt=diagramLineRts[index];diagramLines[index].gameObject.SetActive(true);diagramLines[index].color=color;
            float w=canvasRt.rect.width,h=canvasRt.rect.height;Vector2 ap=new Vector2((a.x-.5f)*w,(a.y-.5f)*h),bp=new Vector2((b.x-.5f)*w,(b.y-.5f)*h);
            Vector2 d=bp-ap;rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.anchoredPosition=(ap+bp)*.5f;rt.sizeDelta=new Vector2(d.magnitude,thick);rt.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
        }

        void SetLabel(int index,string text,Vector2 at)
        {
            labels[index].gameObject.SetActive(true);labels[index].text=text;labelRts[index].anchorMin=labelRts[index].anchorMax=at;
        }

        void RefreshFuses()
        {
            fusesTxt.text=st.fuses==3?"● ● ●":st.fuses==2?"● ● ○":st.fuses==1?"● ○ ○":"○ ○ ○";
            for(int i=0;i<3;i++)fuseBulbs[i].sharedMaterial=i<st.fuses?fuseOn[i]:fuseOff;
        }

        void ShowSolvedPiece(int index)
        {
            if(index<0||index>=shadowPieces.Length)return;shadowPieces[index].localScale=Vector3.one*.01f;
            titleMobiles[index].gameObject.SetActive(true);
            MgfFx.Glow(shadowPieces[index].position,index%3==0?Magenta:index%3==1?Cyan:Lime,10,.35f);
        }

        void UpdateWorld(float dt)
        {
            worldClock+=dt;MgfLook.FitWidth(cam,34f,.72f);
            for(int i=0;i<7;i++)
            {
                float sway=Mathf.Sin(worldClock*(.35f+i*.025f)+i)*2.5f;titleMobiles[i].localRotation=Quaternion.Euler(Mathf.Sin(worldClock*.3f+i)*1.5f,sway+(i-3)*12f,Mathf.Sin(worldClock*.42f+i)*1.8f);
                if(shadowPieces[i].localScale.x>0&&shadowPieces[i].localScale.x<1)shadowPieces[i].localScale=Vector3.Lerp(shadowPieces[i].localScale,Vector3.one,dt*5.5f);
            }
            if(activePrism)
            {
                float idle=phase==GamePhase.Title?Mathf.Sin(worldClock*.55f)*5f:Mathf.Sin(worldClock*.7f)*1.1f;
                activePrism.localRotation=Quaternion.Slerp(activePrism.localRotation,Quaternion.Euler(0,idle,prismTilt),dt*(phase==GamePhase.Reveal?4f:2f));
            }
            if(prismGlow>0)prismGlow=Mathf.Max(0,prismGlow-dt*1.2f);
            float pulse=.5f+.5f*Mathf.Sin(worldClock*2.3f);startHookGlow.color=new Color(Cyan.r,Cyan.g,Cyan.b,.17f+.18f*pulse);
            startHookRt.localRotation=Quaternion.Euler(0,0,Mathf.Sin(worldClock*1.4f)*5f);startHookRt.localScale=Vector3.one*(1+.035f*pulse);
            titleArt.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * .27f) * .008f);
        }

        void UpdateUi(float dt)
        {
            if(Screen.width!=lastW||Screen.height!=lastH){lastW=Screen.width;lastH=Screen.height;ApplyResponsiveLayout();RefreshDiagram();}
            if(hudGroup.alpha>0)
            {
                hookRt.anchorMin=hookRt.anchorMax=hookNorm;trailRt.anchorMin=trailRt.anchorMax=trailNorm;trailNorm=Vector2.Lerp(trailNorm,hookNorm,dt*8f);
                trailImg.color=new Color(Magenta.r,Magenta.g,Magenta.b,dragMode==DragMode.Hook?.20f:.04f);
                progressTxt.text="MOBILE "+st.solved+" / 7";timeTxt.text=phase==GamePhase.Practice?"연습":Mathf.Max(0,105-Mathf.FloorToInt(runSeconds)).ToString();
                if(st.fuses>=0)RefreshFuses();
            }
            if(st.score!=shownScore)
            {
                displayedScore=Mathf.MoveTowards(displayedScore,st.score,dt*520f);
                if(Mathf.Abs(displayedScore-st.score)<.5f){displayedScore=st.score;shownScore=st.score;}
            }
            if(endGroup.alpha>0)endScoreTxt.text=Mathf.RoundToInt(displayedScore).ToString("0000");
            if(refuseT>0)
            {
                refuseT-=dt;
                if(refuseT<=0&&phase!=GamePhase.Reveal)
                {
                    feedbackGroup.alpha=0;
                    if(phase==GamePhase.Practice)ConfigureGuide(hookNorm,LineNodeNorm(current.answerNode));else HideGuide();
                }
            }
            if(titlePressT>0)titlePressT-=dt;
            if(guideVisible)
            {
                guideClock+=dt;float p=.5f+.5f*Mathf.Sin(guideClock*5f);guideRt.localScale=Vector3.one*(1+.15f*p);guideImg.color=new Color(Lime.r,Lime.g,Lime.b,.18f+.20f*p);
                for(int i=0;i<guideDots.Length;i++)
                {
                    float t=(i+1f)/(guideDots.Length+1f);Vector2 at=Vector2.Lerp(guideFromNorm,guideToNorm,t);
                    guideDotRts[i].anchorMin=guideDotRts[i].anchorMax=at;
                    float wave=.5f+.5f*Mathf.Sin(guideClock*6f-i*.75f);guideDots[i].color=new Color(Lime.r,Lime.g,Lime.b,.14f+.66f*wave);
                    guideDotRts[i].localScale=Vector3.one*(.72f+.38f*wave);
                }
            }
        }

        public void TestStart(){StartRunDirect();}
        public void TestAnswerCorrect()
        {
            if(phase==GamePhase.Reveal)FinishReveal();
            if(phase==GamePhase.Title||phase==GamePhase.End)StartRunDirect();
            if(phase!=GamePhase.Playing&&phase!=GamePhase.Practice)return;
            if(current.kind==PrismProblemKind.Centroid)st.tensionLines=2;
            st.selectedNode=current.answerNode;JudgeSelection();
        }
        public void TestAnswerWrong()
        {
            if(phase==GamePhase.Reveal)FinishReveal();
            if(phase==GamePhase.Title||phase==GamePhase.End)StartRunDirect();
            if(phase!=GamePhase.Playing&&phase!=GamePhase.Practice)return;
            if(current.kind==PrismProblemKind.Centroid)st.tensionLines=2;
            st.selectedNode=(current.answerNode+1)%Math.Max(2,current.maxNode);if(st.selectedNode==current.answerNode)st.selectedNode=0;JudgeSelection();
        }
        public string StateJson()=>JsonUtility.ToJson(st);
        public string ProblemBankJson()=>MgfJson.Bank(bank);
    }
}
