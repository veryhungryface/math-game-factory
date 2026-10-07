// 컷라인 — 흐린 도시 옥상 2.5D 디오라마, 재단기 UI, 피드백 연출.
// 정답은 체결·상승·주행의 어휘, 오답만 카메라 흔들림·폐자재 낙하를 쓴다.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.Keotlain
{
    public partial class KeotlainGame
    {
        static readonly Color Concrete = MgfLook.Hex("#D7D9D5");
        static readonly Color Ink = MgfLook.Hex("#20252A");
        static readonly Color Orange = MgfLook.Hex("#FF6B35");
        static readonly Color Cyan = MgfLook.Hex("#1FB7B1");
        static readonly Color Violet = MgfLook.Hex("#6A4CC2");
        static readonly Color Pale = MgfLook.Hex("#F3F4F0");
        static readonly Color Steel = MgfLook.Hex("#AEB7BE");
        static readonly Color Wrong = MgfLook.Hex("#C54C3D");

        Camera cam;
        Transform worldRoot, roofRoot, cutterRoot, stripRoot, bladeRoot, leverRoot, scrapRoot, titleRamp, cableRouteRoot;
        readonly Transform[] rampRoots = new Transform[3];
        readonly Transform[] skaters = new Transform[3];
        readonly Transform[] boards = new Transform[3];
        readonly Transform[] flags = new Transform[8];
        readonly Transform[] vents = new Transform[4];
        readonly Transform[] braces = new Transform[3];
        readonly Renderer[] rampFaces = new Renderer[3];
        readonly Material[] rampBaseMats = new Material[3];
        LineRenderer stripTrail, cutLine, skylineRoute;
        Material concreteMat, darkMat, blackMat, orangeMat, cyanMat, violetMat, steelMat, paleMat, wrongMat, glassMat;
        TextMeshPro titleGraffiti;
        float worldClock, feedbackClock, hitStop, wrongClock, guideClock, cameraKick;
        int feedbackSlot;
        bool feedbackSuccess;
        Vector3 camHome;

        CanvasGroup titleG, hudG, revealG, endG, toastG;
        RectTransform rootRt, titleRt, hudRt, revealRt, endRt, toastRt, rippleRt, guideRt, bladeUiRt, stripUiRt, leverUiRt;
        RectTransform topRt, promptRt, measureRt, tapeRt;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleCta, titleBest;
        TextMeshProUGUI scoreTxt, progressTxt, livesTxt, timeTxt, stockTxt, goalTxt, promptTxt, lengthTxt, squareTxt, cutHintTxt, toastTxt;
        readonly TextMeshProUGUI[] orderTxt = new TextMeshProUGUI[3];
        readonly Image[] orderBg = new Image[3];
        readonly TextMeshProUGUI[] tickTxt = new TextMeshProUGUI[7];
        TextMeshProUGUI revealTitleTxt, revealMathTxt, endTitleTxt, endScoreTxt, endStatsTxt, endTapTxt, guideTxt;
        Image revealBg, rippleImg, bladeUi, stripFillUi, leverUi, tapeUi;
        Sprite roundSprite, notchSprite, ringSprite;
        float shownScore, endShownScore, rippleClock, toastClock, guideBoost, bladeUiT;
        Vector2 bladeUiHome;
        int cachedScore = -1, cachedSecond = -1, layoutW = -1, layoutH = -1;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size); var im = rt.gameObject.AddComponent<Image>();
            im.color = color; im.sprite = sprite; im.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color,
            float width = 360f, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 2.25f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color;
            t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text; return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring, bool notches = false)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "CutRing" : "CutShape", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                if (ring)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - size * .40f) / (size * .06f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * .5f) - (size * .5f - radius), 0);
                    float qy = Mathf.Max(Mathf.Abs(fy - size * .5f) - (size * .5f - radius), 0);
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                    if (notches && y > size * .70f && ((x / 7) % 2 == 0)) a *= .35f;
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            float b = ring ? 0 : radius + 2f;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b,b,b,b));
        }

        void BuildWorld()
        {
            // GameObject.CreatePrimitive가 내부에서 문자열로 붙이는 프리미티브 콜라이더가
            // WebGL managed stripping에서 제거되지 않도록 명시적으로 참조한다.
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<SphereCollider>();
            colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<BoxCollider>();
            Destroy(colliderTypes);

            MgfLook.Sky(MgfLook.Hex("#AEBAC4"), MgfLook.Hex("#DDE1E3"), MgfLook.Hex("#8F9AA2"), .88f);
            MgfLook.Sun(new Vector3(48, -32, 18), MgfLook.Hex("#EAF4F4"), 1.06f, .62f);
            cam = MgfLook.Camera(new Vector3(16.2f, 15.8f, -18.5f), new Vector3(0, 1.1f, 3.1f), 32f);
            cam.orthographic = true; cam.orthographicSize = 8.1f; camHome = cam.transform.position;

            concreteMat = MgfLook.Lit(Concrete, .18f, .02f);
            darkMat = MgfLook.Lit(Ink, .24f, .12f);
            blackMat = MgfLook.Lit(MgfLook.Hex("#11161A"), .44f, .18f);
            orangeMat = MgfLook.Lit(Orange, .42f, .08f, Orange * .08f);
            cyanMat = MgfLook.Lit(Cyan, .48f, .05f, Cyan * .06f);
            violetMat = MgfLook.Lit(Violet, .40f, .04f, Violet * .05f);
            steelMat = MgfLook.Lit(Steel, .78f, .72f);
            paleMat = MgfLook.Lit(Pale, .22f, .01f);
            wrongMat = MgfLook.Lit(Wrong, .34f, .03f, Wrong * .05f);
            glassMat = MgfLook.Alpha(new Color(.22f, .32f, .38f, .34f));
            rampBaseMats[0] = cyanMat; rampBaseMats[1] = concreteMat; rampBaseMats[2] = violetMat;

            worldRoot = new GameObject("KeotlainRooftopWorld").transform;
            BuildCity();
            BuildRooftop();
            BuildRampsAndSkaters();
            BuildSkylineRoute();
            BuildCutter();
        }

        void BuildCity()
        {
            var city = new GameObject("DistantApartmentBlocks").transform; city.SetParent(worldRoot, false);
            Color[] cols = { MgfLook.Hex("#75818A"), MgfLook.Hex("#8A949B"), MgfLook.Hex("#66737D") };
            for (int i = 0; i < 18; i++)
            {
                float x = -22f + i * 2.65f;
                float h = 3.4f + (i * 7 % 9) * .42f;
                var b = MgfLook.Block("Apartment" + i, new Vector3(x, h * .5f - .6f, 14f + (i % 3) * 1.8f),
                    new Vector3(2.25f, h, 2.1f), .12f, MgfLook.Lit(cols[i % cols.Length], .12f), city);
                b.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                for (int w = 0; w < 3; w++)
                    MgfLook.Block("Window" + i + "_" + w, new Vector3(x - .6f + w * .6f, h * .55f, 12.91f + (i % 3) * 1.8f),
                        new Vector3(.22f, .28f, .04f), .02f, darkMat, city).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            // 안테나와 물탱크가 실루엣 리듬을 만든다.
            for (int i = 0; i < 7; i++)
            {
                float x = -15f + i * 5f;
                MgfLook.Block("Antenna" + i, new Vector3(x, 5.8f + (i%2), 13.2f), new Vector3(.10f, 4f, .10f), .02f, darkMat, city);
                MgfLook.Block("AntennaCross" + i, new Vector3(x, 6.8f + (i%2), 13.2f), new Vector3(1.2f, .08f, .08f), .02f, darkMat, city);
            }
        }

        void BuildRooftop()
        {
            roofRoot = new GameObject("FullRooftopDiorama").transform; roofRoot.SetParent(worldRoot, false);
            MgfLook.Block("RoofSlab", new Vector3(0, -.65f, 3.2f), new Vector3(25f, 1.2f, 18f), .48f, concreteMat, roofRoot);
            MgfLook.Block("ParapetBack", new Vector3(0, .30f, 11.6f), new Vector3(25f, 1.8f, .55f), .18f, concreteMat, roofRoot);
            MgfLook.Block("ParapetL", new Vector3(-12f, .32f, 3.2f), new Vector3(.55f, 1.85f, 17f), .16f, concreteMat, roofRoot);
            MgfLook.Block("ParapetR", new Vector3(12f, .32f, 3.2f), new Vector3(.55f, 1.85f, 17f), .16f, concreteMat, roofRoot);

            for (int i = 0; i < vents.Length; i++)
            {
                var v = new GameObject("RooftopVent" + i).transform; v.SetParent(roofRoot, false);
                v.localPosition = new Vector3(-9f + i * 6f, .15f, 8.7f + (i%2)*.8f); vents[i] = v;
                MgfLook.Block("VentBase", Vector3.zero, new Vector3(1.35f, 1.25f, 1.35f), .18f, steelMat, v);
                for (int b = -2; b <= 2; b++) MgfLook.Block("VentSlat" + b, new Vector3(0, .05f + b*.17f, -.70f), new Vector3(1.05f, .07f, .05f), .02f, darkMat, v);
                MgfLook.Prim(PrimitiveType.Cylinder, "VentCap", new Vector3(0, .85f, 0), new Vector3(.82f, .18f, .82f), steelMat, v, false);
            }

            // 주황 안전 테이프: 화면을 가로지르는 독자적인 재질 리듬이자 ambient idle motion.
            for (int i = 0; i < flags.Length; i++)
            {
                var f = MgfLook.Block("SafetyTape" + i, new Vector3(-9.5f + i * 2.7f, 2.0f + (i%2)*.18f, 10.9f),
                    new Vector3(2.3f, .22f, .045f), .03f, orangeMat, roofRoot).transform;
                flags[i] = f;
            }
            MgfLook.Block("TapePostL", new Vector3(-10.7f, 1.05f, 10.9f), new Vector3(.12f, 2.5f, .12f), .03f, darkMat, roofRoot);
            MgfLook.Block("TapePostR", new Vector3(10.7f, 1.05f, 10.9f), new Vector3(.12f, 2.5f, .12f), .03f, darkMat, roofRoot);

            // 방수포와 그래피티 스텐실 조각.
            var tarp = MgfLook.Block("WindBlownTarp", new Vector3(-7.2f, .04f, 5.8f), new Vector3(5.4f, .08f, 3.2f), .20f, violetMat, roofRoot);
            tarp.transform.localRotation = Quaternion.Euler(0, -12, 0);
            for (int i = 0; i < 9; i++)
            {
                var decal = MgfLook.Block("CyanStencil" + i, new Vector3(-3.8f + i*.92f, -.015f, 7.0f + Mathf.Sin(i)*.4f),
                    new Vector3(.56f, .025f, .18f), .02f, i%2==0?cyanMat:violetMat, roofRoot);
                decal.transform.localRotation = Quaternion.Euler(0, i*17f, 0);
            }
        }

        void BuildRampsAndSkaters()
        {
            for (int i = 0; i < 3; i++)
            {
                var r = new GameObject("RampBay" + i).transform; r.SetParent(roofRoot, false);
                r.localPosition = new Vector3(-7f + i * 7f, 0, 3.5f + (i==1 ? .8f : 0)); rampRoots[i] = r;
                var deck = MgfLook.Block("RampSurface", new Vector3(0, 1.25f, 0), new Vector3(4.1f, .38f, 4.5f), .30f, rampBaseMats[i], r);
                deck.transform.localRotation = Quaternion.Euler(-16f, 0, 0); rampFaces[i] = deck.GetComponent<Renderer>();
                MgfLook.Block("SteelLip", new Vector3(0, 2.28f, 1.75f), new Vector3(4.22f, .16f, .22f), .05f, steelMat, r);
                MgfLook.Block("RampFootL", new Vector3(-1.7f, .45f, -.8f), new Vector3(.20f, 1.4f, .20f), .04f, steelMat, r);
                MgfLook.Block("RampFootR", new Vector3(1.7f, .45f, -.8f), new Vector3(.20f, 1.4f, .20f), .04f, steelMat, r);
                var brace = MgfLook.Block("CompositeBrace", new Vector3(0, .82f, -.35f), new Vector3(3.6f, .24f, .30f), .08f, blackMat, r).transform;
                brace.localRotation = Quaternion.Euler(0, 0, i==1 ? -20f : 20f); brace.gameObject.SetActive(false); braces[i] = brace;
                BuildRightAngleMark(r);
                skaters[i] = BuildSkater(r, i);
                boards[i] = skaters[i].Find("Board");
            }

            titleRamp = rampRoots[1];
            titleGraffiti = MgfText.World("컷 라 인", new Vector3(0, 2.70f, .25f), 7.5f, Pale, titleRamp);
            titleGraffiti.transform.localRotation = Quaternion.Euler(0, 0, -5f);
            titleGraffiti.transform.localScale = Vector3.one * .29f;
            titleGraffiti.outlineWidth = .28f; titleGraffiti.outlineColor = Ink;
        }

        void BuildSkylineRoute()
        {
            // 고정 작업대가 아니라 세 옥상을 잇는 장력 케이블 주행선이 세계의 중심이다.
            // 정답 사건은 잘린 조각의 전시가 아니라 이 선이 팽팽해지고 군중이 연속 주행하는 변화다.
            cableRouteRoot = new GameObject("SuspendedRooftopRunline").transform;
            cableRouteRoot.SetParent(roofRoot, false);
            for (int i = 0; i < 4; i++)
            {
                float x = -10.2f + i * 6.8f;
                MgfLook.Block("RunlinePylon" + i, new Vector3(x, 2.0f, 5.9f), new Vector3(.22f, 4.2f, .22f), .05f, blackMat, cableRouteRoot);
                MgfLook.Block("RunlineFlag" + i, new Vector3(x + .65f, 3.45f, 5.9f), new Vector3(1.25f, .32f, .06f), .05f,
                    i % 2 == 0 ? cyanMat : violetMat, cableRouteRoot);
            }
            skylineRoute = MakeLine("TensionCableRoute", MgfLook.Additive(new Color(Cyan.r, Cyan.g, Cyan.b, .82f)), .075f, 13);
            for (int i = 0; i < skylineRoute.positionCount; i++)
            {
                float t = i / 12f;
                skylineRoute.SetPosition(i, new Vector3(Mathf.Lerp(-10.2f, 10.2f, t), 3.25f + Mathf.Sin(t * Mathf.PI * 3f) * .42f, 5.9f));
            }
        }

        void BuildRightAngleMark(Transform parent)
        {
            MgfLook.Block("RightAngleA", new Vector3(-1.50f, .11f, -1.72f), new Vector3(.48f, .06f, .09f), .02f, orangeMat, parent);
            MgfLook.Block("RightAngleB", new Vector3(-1.70f, .11f, -1.52f), new Vector3(.09f, .06f, .48f), .02f, orangeMat, parent);
        }

        Transform BuildSkater(Transform parent, int index)
        {
            var root = new GameObject("Skater" + index).transform; root.SetParent(parent, false);
            root.localPosition = new Vector3(-1.4f + index * .25f, .42f, -2.15f);
            Color cloth = index==0 ? Cyan : index==1 ? Orange : Violet;
            var body = MgfLook.Block("Jacket", new Vector3(0, .92f, 0), new Vector3(.58f, .86f, .34f), .20f, MgfLook.Lit(cloth, .24f), root);
            body.transform.localRotation = Quaternion.Euler(5,0,index==1?-8:8);
            MgfLook.Prim(PrimitiveType.Sphere, "Helmet", new Vector3(0, 1.62f, 0), new Vector3(.52f,.48f,.52f), MgfLook.Lit(cloth, .68f,.12f), root, false);
            MgfLook.Block("Visor", new Vector3(0, 1.62f, -.27f), new Vector3(.35f,.14f,.06f), .04f, darkMat, root);
            for (int side=-1; side<=1; side+=2)
            {
                var leg = MgfLook.Block("Leg"+side, new Vector3(side*.19f,.30f,0), new Vector3(.18f,.68f,.18f), .08f, darkMat, root);
                leg.transform.localRotation = Quaternion.Euler(side*10,0,side*12);
                var arm = MgfLook.Block("Arm"+side, new Vector3(side*.42f,1.02f,0), new Vector3(.18f,.68f,.18f), .08f, MgfLook.Lit(cloth,.24f), root);
                arm.transform.localRotation = Quaternion.Euler(0,0,side*24);
            }
            var board = MgfLook.Block("Board", new Vector3(0,-.08f,0), new Vector3(1.35f,.10f,.34f), .16f, blackMat, root).transform;
            for (int side=-1; side<=1; side+=2)
                MgfLook.Prim(PrimitiveType.Cylinder, "Wheel"+side, new Vector3(side*.45f,-.15f,0), new Vector3(.14f,.10f,.14f), orangeMat, root, false).transform.localRotation=Quaternion.Euler(90,0,0);
            return root;
        }

        void BuildCutter()
        {
            cutterRoot = new GameObject("CableSleeveTensionDock").transform; cutterRoot.SetParent(roofRoot, false);
            cutterRoot.localPosition = new Vector3(0, .15f, -4.7f);
            // 넓은 재단대 면을 없애고 옥상 설비 사이의 가느다란 장력 도크로 바꾼다.
            MgfLook.Block("DockRail", new Vector3(0,.25f,0), new Vector3(16.8f,.24f,.72f), .12f, steelMat, cutterRoot);
            for (int i = -1; i <= 1; i += 2)
            {
                MgfLook.Block("DockFoot" + i, new Vector3(i * 7.5f,.02f,0), new Vector3(.45f,.65f,1.7f), .12f, darkMat, cutterRoot);
                MgfLook.Prim(PrimitiveType.Cylinder, "CableSpool" + i, new Vector3(i * 7.25f,.82f,0), new Vector3(1.1f,.55f,1.1f),
                    i < 0 ? violetMat : cyanMat, cutterRoot, false).transform.localRotation = Quaternion.Euler(90,0,0);
            }
            stripRoot = MgfLook.Block("PulledCompositeCableSleeve", new Vector3(-2.5f,.58f,0), new Vector3(8.5f,.16f,.38f), .08f, blackMat, cutterRoot).transform;
            MgfLook.Block("OrangeGrip", new Vector3(4.35f,.16f,0), new Vector3(.55f,.56f,.82f), .18f, orangeMat, stripRoot);

            bladeRoot = new GameObject("FixedBladeAndHandle").transform; bladeRoot.SetParent(cutterRoot,false); bladeRoot.localPosition=new Vector3(6.6f,.82f,0);
            MgfLook.Block("BladeTower", new Vector3(0,.85f,.65f), new Vector3(1.15f,2.5f,.38f), .16f, steelMat, bladeRoot);
            MgfLook.Block("Blade", new Vector3(0,.38f,0), new Vector3(.18f,1.30f,1.55f), .04f, darkMat, bladeRoot);
            MgfLook.Block("OrangeBladeHandle", new Vector3(0,1.85f,0), new Vector3(1.15f,.48f,.90f), .20f, orangeMat, bladeRoot);

            leverRoot = new GameObject("CartridgeLever").transform; leverRoot.SetParent(cutterRoot,false); leverRoot.localPosition=new Vector3(-7.4f,.78f,0);
            MgfLook.Block("LeverBase", Vector3.zero, new Vector3(1.15f,.38f,1.25f), .20f, blackMat, leverRoot);
            var lever = MgfLook.Block("Lever", new Vector3(0,.75f,0), new Vector3(.28f,1.45f,.28f), .10f, orangeMat, leverRoot); lever.transform.localRotation=Quaternion.Euler(0,0,-18f);

            scrapRoot = new GameObject("ScrapBin").transform; scrapRoot.SetParent(cutterRoot,false); scrapRoot.localPosition=new Vector3(8.3f,.15f,1.2f);
            MgfLook.Block("ScrapBinBody", Vector3.zero, new Vector3(2.2f,1.35f,2.0f), .28f, darkMat, scrapRoot);
            MgfLook.Block("ScrapPiece", new Vector3(0,1.1f,0), new Vector3(1.5f,.16f,.55f), .05f, wrongMat, scrapRoot).SetActive(false);

            stripTrail = MakeLine("CompositeAfterimage", MgfLook.Alpha(new Color(Cyan.r,Cyan.g,Cyan.b,.42f)), .10f, 6); stripTrail.enabled=false;
            cutLine = MakeLine("BladeCutFlash", MgfLook.Additive(new Color(Orange.r,Orange.g,Orange.b,.72f)), .12f, 2); cutLine.enabled=false;
        }

        LineRenderer MakeLine(string name, Material mat, float width, int count)
        {
            var go = new GameObject(name); go.transform.SetParent(worldRoot,false); var line=go.AddComponent<LineRenderer>();
            line.sharedMaterial=mat; line.widthMultiplier=width; line.positionCount=count; line.useWorldSpace=true;
            line.shadowCastingMode=ShadowCastingMode.Off; line.receiveShadows=false; return line;
        }

        void BuildUi()
        {
            roundSprite=MakeSprite(64,15,false); notchSprite=MakeSprite(64,10,false,false); ringSprite=MakeSprite(64,12,true);
            var canvas=MgfText.Canvas;
            rootRt=R("KeotlainUi",canvas.transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
            rootRt.anchorMin=Vector2.zero;rootRt.anchorMax=Vector2.one;rootRt.sizeDelta=Vector2.zero;
            BuildTitleUi(); BuildHudUi(); BuildRevealUi(); BuildEndUi();
            toastRt=Img(rootRt,"SprayChalkToast",new Vector2(.5f,0),new Vector2(0,286),new Vector2(352,54),new Color(Ink.r,Ink.g,Ink.b,.94f),roundSprite).rectTransform;
            toastG=toastRt.gameObject.AddComponent<CanvasGroup>();toastTxt=Txt(toastRt,"",new Vector2(.5f,.5f),Vector2.zero,13,Pale,330);
            rippleRt=Img(rootRt,"SprayChalkRipple",new Vector2(.5f,.5f),Vector2.zero,new Vector2(36,36),new Color(Cyan.r,Cyan.g,Cyan.b,0),ringSprite).rectTransform;
            rippleImg=rippleRt.GetComponent<Image>();
        }

        void BuildTitleUi()
        {
            titleRt=R("WorldFilledTitle",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);titleRt.anchorMin=Vector2.zero;titleRt.anchorMax=Vector2.one;titleRt.sizeDelta=Vector2.zero;
            titleG=titleRt.gameObject.AddComponent<CanvasGroup>();
            titleMeta=Txt(titleRt,"중학교 2학년 · 피타고라스 정리",new Vector2(.5f,1),new Vector2(0,-43),14,Ink,340);titleMeta.outlineWidth=.12f;titleMeta.outlineColor=Pale;
            titleLogo=Txt(titleRt,"컷라인",new Vector2(.5f,.73f),Vector2.zero,62,Pale,360);titleLogo.fontStyle=FontStyles.Bold|FontStyles.Italic;titleLogo.outlineWidth=.34f;titleLogo.outlineColor=Ink;
            titleTag=Txt(titleRt,"길이를 당겨 램프를 세워라",new Vector2(.5f,.66f),Vector2.zero,18,Ink,344);titleTag.outlineWidth=.10f;titleTag.outlineColor=Pale;
            titleBest=Txt(titleRt,"",new Vector2(.5f,0),new Vector2(0,102),13,Ink,330);
            var cta=Img(titleRt,"LiveStripStartZone",new Vector2(.5f,0),new Vector2(0,53),new Vector2(352,70),new Color(Ink.r,Ink.g,Ink.b,.94f),roundSprite);
            Img(cta.transform,"OrangeCutLine",new Vector2(.5f,1),new Vector2(0,-5),new Vector2(310,5),Orange,notchSprite);
            titleCta=Txt(cta.transform,"검은 스트립을 당겨 시작",new Vector2(.5f,.5f),Vector2.zero,20,Pale,326);titleCta.fontStyle=FontStyles.Bold;
        }

        void BuildHudUi()
        {
            hudRt=R("RooftopHud",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);hudRt.anchorMin=Vector2.zero;hudRt.anchorMax=Vector2.one;hudRt.sizeDelta=Vector2.zero;
            hudG=hudRt.gameObject.AddComponent<CanvasGroup>();
            var top=Img(hudRt,"StencilStatusBar",new Vector2(.5f,1),new Vector2(0,-28),new Vector2(374,48),new Color(Pale.r,Pale.g,Pale.b,.95f),notchSprite);
            topRt=top.rectTransform;
            scoreTxt=Txt(top.transform,"0",new Vector2(0,.5f),new Vector2(47,0),18,Ink,86,TextAlignmentOptions.Left);
            progressTxt=Txt(top.transform,"0 / 8",new Vector2(.5f,.5f),Vector2.zero,17,Violet,90);
            livesTxt=Txt(top.transform,"◆ ◆ ◆",new Vector2(1,.5f),new Vector2(-72,0),16,Orange,116,TextAlignmentOptions.Right);
            timeTxt=Txt(top.transform,"105",new Vector2(1,.5f),new Vector2(-19,0),13,Ink,48,TextAlignmentOptions.Right);

            goalTxt=Txt(hudRt,"필요한 버팀목 길이를 구해 스트립을 자르시오.",new Vector2(.5f,1),new Vector2(0,-72),14,Pale,370);
            goalTxt.outlineWidth=.24f;goalTxt.outlineColor=Ink;

            for(int i=0;i<3;i++)
            {
                var card=Img(hudRt,"WorldBlueprint"+i,new Vector2(.18f+i*.32f,1),new Vector2(0,-128),new Vector2(116,82),new Color(Ink.r,Ink.g,Ink.b,.90f),roundSprite);
                orderBg[i]=card;orderTxt[i]=Txt(card.transform,"",new Vector2(.5f,.5f),Vector2.zero,12,Pale,108);orderTxt[i].fontStyle=FontStyles.Bold;
            }
            var prompt=Img(hudRt,"StencilBlueprintDetail",new Vector2(.5f,1),new Vector2(0,-203),new Vector2(364,56),new Color(Concrete.r,Concrete.g,Concrete.b,.96f),notchSprite);
            promptRt=prompt.rectTransform;
            promptTxt=Txt(prompt.transform,"",new Vector2(.5f,.5f),Vector2.zero,13,Ink,344);promptTxt.fontStyle=FontStyles.Bold;

            stockTxt=Txt(hudRt,"",new Vector2(.5f,0),new Vector2(0,246),13,Pale,350);stockTxt.outlineWidth=.22f;stockTxt.outlineColor=Ink;
            var measure=Img(hudRt,"CutterReadout",new Vector2(.5f,0),new Vector2(-52,176),new Vector2(244,90),new Color(Ink.r,Ink.g,Ink.b,.96f),roundSprite);
            measureRt=measure.rectTransform;
            lengthTxt=Txt(measure.transform,"1 cm",new Vector2(.34f,.5f),new Vector2(0,7),31,Pale,134);lengthTxt.fontStyle=FontStyles.Bold;
            squareTxt=Txt(measure.transform,"1² = 1",new Vector2(.72f,.5f),new Vector2(0,7),17,Cyan,94);
            stripUiRt=Img(hudRt,"BlackStripTouchZone",new Vector2(.38f,0),new Vector2(0,105),new Vector2(270,42),Ink,roundSprite).rectTransform;
            stripFillUi=Img(stripUiRt,"CyanTravel",new Vector2(0,.5f),new Vector2(16,0),new Vector2(12,6),Cyan,roundSprite);
            stripFillUi.rectTransform.pivot=new Vector2(0,.5f);
            for(int i=0;i<7;i++) tickTxt[i]=Txt(hudRt,"",new Vector2(.12f+i*.085f,0),new Vector2(0,73),12,Pale,44);

            bladeUiRt=Img(hudRt,"OrangeBladeSwipe",new Vector2(.86f,0),new Vector2(0,139),new Vector2(78,118),new Color(Orange.r,Orange.g,Orange.b,.96f),roundSprite).rectTransform;
            bladeUiHome=bladeUiRt.anchoredPosition;
            bladeUi=bladeUiRt.GetComponent<Image>();
            cutHintTxt=Txt(bladeUiRt,"칼날\n↓ 쓸기",new Vector2(.5f,.5f),Vector2.zero,15,Pale,72);cutHintTxt.fontStyle=FontStyles.Bold;
            leverUiRt=Img(hudRt,"CartridgeLeverButton",new Vector2(.10f,0),new Vector2(0,40),new Vector2(68,62),new Color(Violet.r,Violet.g,Violet.b,.96f),roundSprite).rectTransform;
            leverUi=leverUiRt.GetComponent<Image>();Txt(leverUiRt,"교체\n레버",new Vector2(.5f,.5f),Vector2.zero,13,Pale,62);
            tapeUi=Img(hudRt,"SafetyTapeCount",new Vector2(.5f,0),new Vector2(0,35),new Vector2(176,12),Orange,notchSprite);
            tapeRt=tapeUi.rectTransform;

            guideRt=R("OnboardingMovingHand",hudRt,new Vector2(.5f,0),Vector2.zero,new Vector2(320,72));
            guideTxt=Txt(guideRt,"●  · · ·  >   5 cm    칼날 ↓",new Vector2(.5f,.5f),Vector2.zero,16,Orange,320);guideTxt.fontStyle=FontStyles.Bold;guideTxt.outlineWidth=.18f;guideTxt.outlineColor=Ink;
        }

        void BuildRevealUi()
        {
            revealRt=Img(rootRt,"CutResultStencil",new Vector2(.5f,.5f),new Vector2(0,16),new Vector2(360,178),new Color(Pale.r,Pale.g,Pale.b,.98f),roundSprite).rectTransform;
            revealG=revealRt.gameObject.AddComponent<CanvasGroup>();revealBg=revealRt.GetComponent<Image>();
            revealTitleTxt=Txt(revealRt,"",new Vector2(.5f,.5f),new Vector2(0,48),24,Ink,330);revealTitleTxt.fontStyle=FontStyles.Bold;
            revealMathTxt=Txt(revealRt,"",new Vector2(.5f,.5f),new Vector2(0,-22),15,Ink,328);
        }

        void BuildEndUi()
        {
            endRt=R("RooftopGateEnd",rootRt,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);endRt.anchorMin=Vector2.zero;endRt.anchorMax=Vector2.one;endRt.sizeDelta=Vector2.zero;
            endG=endRt.gameObject.AddComponent<CanvasGroup>();
            var banner=Img(endRt,"GraffitiEndBanner",new Vector2(.5f,.5f),new Vector2(0,15),new Vector2(360,430),new Color(Ink.r,Ink.g,Ink.b,.96f),roundSprite);
            Img(banner.transform,"EndOrangeRail",new Vector2(.5f,1),new Vector2(0,-10),new Vector2(316,8),Orange,notchSprite);
            endTitleTxt=Txt(banner.transform,"",new Vector2(.5f,.5f),new Vector2(0,122),30,Pale,326);endTitleTxt.fontStyle=FontStyles.Bold;
            endScoreTxt=Txt(banner.transform,"0",new Vector2(.5f,.5f),new Vector2(0,48),44,Cyan,300);endScoreTxt.fontStyle=FontStyles.Bold;
            endStatsTxt=Txt(banner.transform,"",new Vector2(.5f,.5f),new Vector2(0,-38),16,Pale,324);
            var again=Img(banner.transform,"RoofDoorRestart",new Vector2(.5f,0),new Vector2(0,48),new Vector2(314,66),Orange,roundSprite);
            endTapTxt=Txt(again.transform,"옥상 문을 눌러 다시",new Vector2(.5f,.5f),Vector2.zero,19,Pale,292);endTapTxt.fontStyle=FontStyles.Bold;
        }

        void ResetWorldForRun()
        {
            for(int i=0;i<3;i++)
            {
                braces[i].gameObject.SetActive(false); skaters[i].localPosition=new Vector3(-1.4f+i*.25f,.42f,-2.15f);
                skaters[i].localRotation=Quaternion.identity;skaters[i].localScale=Vector3.one;rampFaces[i].sharedMaterial=rampBaseMats[i];
            }
            stripRoot.localScale=Vector3.one;bladeRoot.localPosition=new Vector3(6.6f,.82f,0);leverRoot.localRotation=Quaternion.identity;
            stripTrail.enabled=false;cutLine.enabled=false;feedbackClock=wrongClock=hitStop=0;feedbackSuccess=false;
        }

        void ShowTitleVisual()
        {
            titleGraffiti.gameObject.SetActive(true); titleGraffiti.text="컷 라 인";
            for(int i=0;i<3;i++) braces[i].gameObject.SetActive(i==1);
        }

        void StartProblemVisual(bool tutorial)
        {
            titleGraffiti.gameObject.SetActive(false);
            for(int i=0;i<3;i++) { braces[i].gameObject.SetActive(false); rampFaces[i].sharedMaterial=i==selectedSlot?MgfLook.Lit(i==0?Cyan:i==1?Orange:Violet,.52f,.06f,(i==0?Cyan:i==1?Orange:Violet)*.12f):rampBaseMats[i]; }
            OnLengthChanged(st.selectedLength,false);ResetBladeVisual();
            if(tutorial) ReplayGuide(false); else guideRt.gameObject.SetActive(false);
        }

        void OnOrderSelected(int slot)
        {
            for(int i=0;i<3;i++) rampFaces[i].sharedMaterial=i==slot?MgfLook.Lit(i==0?Cyan:i==1?Orange:Violet,.52f,.06f,(i==0?Cyan:i==1?Orange:Violet)*.14f):rampBaseMats[i];
            MgfFx.Punch(rampRoots[slot],.045f,.20f);
        }

        void OnLengthChanged(int value,bool fromPointer)
        {
            float t=(value-1)/28f;
            stripRoot.localScale=new Vector3(.46f+.72f*t,1,1);
            stripRoot.localPosition=new Vector3(-4.7f+2.2f*t,.58f,0);
            if(fromPointer)
            {
                stripTrail.enabled=true;
                for(int i=0;i<stripTrail.positionCount;i++) stripTrail.SetPosition(i,stripRoot.TransformPoint(new Vector3(-4f-i*.22f,.20f,0)));
            }
        }

        void StartStripDragVisual(){stripRoot.localScale=new Vector3(stripRoot.localScale.x,1.24f,.86f);guideRt.gameObject.SetActive(false);}
        void EndStripDragVisual(){stripRoot.localScale=new Vector3(stripRoot.localScale.x,1,1);stripTrail.enabled=false;}
        void StartBladeVisual(){bladeUiT=0;cutHintTxt.text="아래로\n길게 쓸기";guideRt.gameObject.SetActive(false);}
        void MoveBladeVisual(float t){bladeUiT=t;bladeRoot.localPosition=new Vector3(6.6f,.82f-Mathf.Sin(t*Mathf.PI*.5f)*1.05f,0);bladeUiRt.anchoredPosition=bladeUiHome+new Vector2(0,-t*34f);}
        void ResetBladeVisual(){bladeUiT=0;bladeRoot.localPosition=new Vector3(6.6f,.82f,0);if(bladeUiRt)bladeUiRt.anchoredPosition=bladeUiHome;if(cutHintTxt)cutHintTxt.text="칼날\n↓ 쓸기";}
        void OnCartridgeChanged(){leverRoot.localRotation=Quaternion.Euler(0,0,24f);MgfFx.Punch(leverRoot,.16f,.28f);}

        void BeginRevealVisual(CutProblem p,int expected,int cut,bool correct,bool isPractice)
        {
            feedbackSlot=Mathf.Clamp(selectedSlot,0,2);feedbackClock=0;feedbackSuccess=correct;hitStop=correct?.085f:0f;wrongClock=correct?0f:.48f;
            cutLine.enabled=true;cutLine.SetPosition(0,bladeRoot.position+Vector3.up*.8f);cutLine.SetPosition(1,bladeRoot.position+Vector3.down*.9f);
            revealBg.color=correct?new Color(Cyan.r,Cyan.g,Cyan.b,.97f):new Color(Pale.r,Pale.g,Pale.b,.99f);
            revealTitleTxt.text=correct?(isPractice?"5 cm 체결 · 연습 완료":"정밀 절단 · 버팀목 체결"):("절단 " + cut + " cm · 필요한 길이 " + expected + " cm");
            revealTitleTxt.color=correct?Pale:Wrong;
            revealMathTxt.text=p.reveal+(correct?"":"\n직각의 대변이 빗변인지 확인하고 다시 자르시오.");
            revealMathTxt.color=correct?Pale:Ink;
            if(correct)
            {
                braces[feedbackSlot].gameObject.SetActive(true);braces[feedbackSlot].localScale=new Vector3(.12f,1.65f,.65f);
                MgfFx.Punch(braces[feedbackSlot],.22f,.34f);MgfFx.Glow(braces[feedbackSlot].position,Cyan,10,.34f);
                skylineRoute.widthMultiplier=.16f;
                MgfSfx.Play("correct",.62f);
            }
            else
            {
                var scrap=scrapRoot.Find("ScrapPiece");if(scrap){scrap.gameObject.SetActive(true);scrap.localPosition=new Vector3(0,1.3f,0);}
                MgfFx.Shake(cam,.09f,.28f);MgfSfx.Play("wrong",.55f);
            }
            ResetBladeVisual();
        }

        void FinishRevealVisual(bool correct)
        {
            cutLine.enabled=false;
            skylineRoute.widthMultiplier=.075f;
            var scrap=scrapRoot.Find("ScrapPiece");if(scrap)scrap.gameObject.SetActive(false);
            if(!correct)cameraKick=0;
        }

        void ShowEnd(string reason)
        {
            bool clear=reason=="clear";endShownScore=0;
            endTitleTxt.text=clear?"옥상 주행선 완성":"옥상 출입문 폐쇄";
            endTitleTxt.color=clear?Cyan:Orange;
            string why=reason=="time"?"105초가 지나 비구름이 도착했다":reason=="stock"?"남은 복합재로 주문을 완성할 수 없다":reason=="precision"?"첫 절단 7개 정확 목표에 도달할 수 없다":"안전 테이프 3장이 모두 찢어졌다";
            endStatsTxt.text=clear?("램프 "+st.solved+" / 8\n첫 절단 정확 "+st.firstAttemptCorrect+" / 8\n남은 복합재 "+RemainingStock()+" cm"):(why+"\n완성 램프 "+st.solved+" / 8 · 첫 절단 정확 "+st.firstAttemptCorrect+" / 8");
            for(int i=0;i<3;i++) if(clear)braces[i].gameObject.SetActive(true);
        }

        void SetScreen()
        {
            titleG.alpha=phase==Phase.Title?1:0;titleG.blocksRaycasts=false;
            hudG.alpha=(phase==Phase.Practice||phase==Phase.Playing||phase==Phase.Reveal)?1:0;hudG.blocksRaycasts=false;
            revealG.alpha=phase==Phase.Reveal?1:0;revealG.blocksRaycasts=false;
            endG.alpha=phase==Phase.End?1:0;endG.blocksRaycasts=false;
            if(phase==Phase.Title)guideRt.gameObject.SetActive(false);
        }

        void RefreshOrdersUi()
        {
            for(int i=0;i<3;i++)
            {
                bool on=i<active.Count;orderBg[i].gameObject.SetActive(on);if(!on)continue;
                var o=active[i];string kind=o.problem.mode==CutMode.Hypotenuse?"빗변":o.problem.mode==CutMode.Leg?"다른 변":"수선 2개";
                string nums=o.problem.mode==CutMode.Hypotenuse?(o.problem.a+"² + "+o.problem.b+"²"):
                    o.problem.mode==CutMode.Leg?(o.problem.c+"² − "+o.problem.a+"²"):("AD⊥BC\nBD "+(o.piece==0?"?":o.problem.pieces[0].ToString())+" · DC ?");
                orderTxt[i].text=(i==selectedSlot?"▶ ":"")+kind+"\n"+nums;
                orderBg[i].color=i==selectedSlot?new Color(Orange.r,Orange.g,Orange.b,.97f):new Color(Ink.r,Ink.g,Ink.b,.88f);
            }
            if(Current!=null)
            {
                promptTxt.text=Current.problem.boardLine;
                if(Current.problem.mode==CutMode.Double)promptTxt.text+="   ["+(Current.piece+1)+" / 2 절단]";
            }
        }

        void ReplayGuide(bool stronger)
        {
            guideBoost=stronger?1f:.35f;guideClock=0;guideRt.gameObject.SetActive(true);
            guideTxt.text=practiceStripTouched?"② 주황 칼날을 아래로 길게 쓸기 ↓":"① 검은 스트립 손잡이를 5 cm까지 당기기";
        }

        void OnTutorialStripTouched()
        {
            guideBoost=1f;guideClock=0;guideRt.gameObject.SetActive(true);
            guideTxt.text="② 주황 칼날을 아래로 길게 쓸기 ↓";
            bladeUi.color=Orange;MgfFx.Punch(bladeUiRt,.10f,.28f);
        }

        void SpawnTapRipple(Vector2 screen)
        {
            Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt,screen,null,out local);
            rippleRt.anchoredPosition=local;rippleClock=.44f;
        }

        void RefuseInput(string message)
        {
            toastTxt.text=message;toastClock=1.45f;MgfSfx.Play("wrong",.18f);
            // 화면 전체가 아니라 실제 대상만 좌우로 흔든다.
            stripUiRt.localRotation=Quaternion.Euler(0,0,3.5f);guideRt.gameObject.SetActive(phase==Phase.Practice);
            MgfBridge.NotifyChanged();
        }

        void UpdateWorld(float dt)
        {
            worldClock+=dt;
            float animDt=hitStop>0?0:dt;if(hitStop>0)hitStop-=dt;
            // ambient idle: 안전 테이프·환풍기·스케이터 모두 기준 자세에서 계산해 누적되지 않는다.
            for(int i=0;i<flags.Length;i++)flags[i].localRotation=Quaternion.Euler(Mathf.Sin(worldClock*2.0f+i*.7f)*6f,Mathf.Sin(worldClock*.8f+i)*3f,Mathf.Sin(worldClock*2.6f+i)*4f);
            for(int i=0;i<vents.Length;i++)vents[i].localRotation=Quaternion.Euler(0,Mathf.Sin(worldClock*.22f+i)*2f,0);
            if(phase==Phase.Title)
            {
                for(int i=0;i<3;i++)skaters[i].localPosition=new Vector3(-1.4f+i*.25f,.42f+Mathf.Sin(worldClock*2f+i)*.07f,-2.15f);
                titleGraffiti.transform.localScale=Vector3.one*(.29f+Mathf.Sin(worldClock*1.2f)*.008f);
            }
            if(skylineRoute)
            {
                float pulse=.075f+Mathf.Max(0,Mathf.Sin(worldClock*2.4f))*.018f;
                if(phase!=Phase.Reveal||!feedbackSuccess)skylineRoute.widthMultiplier=pulse;
            }
            if(feedbackClock>=0 && phase==Phase.Reveal)
            {
                feedbackClock+=animDt;
                if(feedbackSuccess)
                {
                    float t=Mathf.Clamp01(feedbackClock/.85f);float overshoot=1f+Mathf.Sin(t*Mathf.PI)*.12f;
                    braces[feedbackSlot].localScale=Vector3.Lerp(braces[feedbackSlot].localScale,Vector3.one*overshoot,.22f);
                    var s=skaters[feedbackSlot];float run=Mathf.Clamp01((feedbackClock-.18f)/.95f);
                    s.localPosition=new Vector3(Mathf.Lerp(-1.4f,2.5f,run),.42f+Mathf.Sin(run*Mathf.PI)*2.25f,Mathf.Lerp(-2.15f,1.1f,run));
                    s.localRotation=Quaternion.Euler(Mathf.Sin(run*Mathf.PI)*-18f,run*150f,Mathf.Sin(run*Mathf.PI)*12f);
                    boards[feedbackSlot].localRotation=Quaternion.Euler(0,0,Mathf.Sin(run*Mathf.PI)*15f);
                }
                else
                {
                    var scrap=scrapRoot.Find("ScrapPiece");if(scrap){float t=Mathf.Clamp01(feedbackClock/.55f);scrap.localPosition=new Vector3(Mathf.Sin(t*8f)*.18f,1.3f-t*1.05f,0);scrap.localRotation=Quaternion.Euler(t*210f,0,t*120f);}
                    if(flags.Length>feedbackSlot)flags[feedbackSlot].localScale=new Vector3(1f,Mathf.Lerp(1f,.25f,Mathf.Clamp01(feedbackClock/.4f)),1f);
                }
            }
            if(leverRoot.localRotation!=Quaternion.identity)leverRoot.localRotation=Quaternion.Slerp(leverRoot.localRotation,Quaternion.identity,.12f);
            if(stripUiRt.localRotation!=Quaternion.identity)stripUiRt.localRotation=Quaternion.Slerp(stripUiRt.localRotation,Quaternion.identity,.18f);
            LayoutCamera();
        }

        void LayoutCamera()
        {
            float aspect=(float)Screen.width/Mathf.Max(1,Screen.height);
            float target=aspect<.55f?12.6f:aspect<.82f?10.1f:8.05f;
            cam.orthographicSize=Mathf.Lerp(cam.orthographicSize,target,.10f);
            if(Screen.width==layoutW&&Screen.height==layoutH)return;
            layoutW=Screen.width;layoutH=Screen.height;
            bool land=Screen.width>=1024&&aspect>=1.15f;
            titleLogo.fontSize=land?64:62;titleLogo.rectTransform.anchoredPosition=Vector2.zero;
            float logicalH=rootRt.rect.height;
            topRt.anchoredPosition=new Vector2(0,-24);
            goalTxt.rectTransform.anchoredPosition=new Vector2(0,land?-62:-72);
            for(int i=0;i<3;i++)
            {
                var rt=orderBg[i].rectTransform;rt.sizeDelta=land?new Vector2(158,74):new Vector2(116,82);
                rt.anchorMin=rt.anchorMax=land?new Vector2(.15f+i*.35f,1):new Vector2(.18f+i*.32f,1);
                rt.anchoredPosition=new Vector2(0,land?-122:-128);
            }
            promptRt.sizeDelta=land?new Vector2(510,58):new Vector2(Mathf.Min(364,rootRt.rect.width-24),Current!=null&&Current.problem.mode==CutMode.Double?72:56);
            promptRt.anchoredPosition=new Vector2(0,land?-202:-203);
            promptTxt.rectTransform.sizeDelta=new Vector2(promptRt.sizeDelta.x-20,promptRt.sizeDelta.y-8);

            float bladeBottom=land?75:Mathf.Min(150,logicalH*.19f);
            bladeUiHome=new Vector2(0,bladeBottom);bladeUiRt.anchoredPosition=bladeUiHome;
            stockTxt.rectTransform.anchoredPosition=new Vector2(0,land?155:246);
            measureRt.anchoredPosition=new Vector2(land?-48:-52,land?100:176);
            stripUiRt.anchoredPosition=new Vector2(0,land?38:105);
            leverUiRt.anchoredPosition=new Vector2(0,land?42:40);
            tapeRt.anchoredPosition=new Vector2(0,land?18:35);
            for(int i=0;i<7;i++)tickTxt[i].rectTransform.anchoredPosition=new Vector2(0,land?10:73);
            toastRt.anchoredPosition=new Vector2(0,land?168:286);
        }

        void UpdateUi(float dt)
        {
            shownScore=Mathf.MoveTowards(shownScore,st.score,Mathf.Max(85f,Mathf.Abs(st.score-shownScore)*5f)*dt);
            int scoreNow=Mathf.RoundToInt(shownScore);if(scoreNow!=cachedScore){cachedScore=scoreNow;scoreTxt.text=scoreNow.ToString();scoreTxt.transform.localScale=Vector3.one*1.12f;}else scoreTxt.transform.localScale=Vector3.Lerp(scoreTxt.transform.localScale,Vector3.one,.18f);
            if(phase==Phase.Practice||phase==Phase.Playing||phase==Phase.Reveal)
            {
                int sec=phase==Phase.Practice?105:Mathf.Max(0,Mathf.CeilToInt(timeLeft));if(sec!=cachedSecond){cachedSecond=sec;timeTxt.text=phase==Phase.Practice?"연습":sec.ToString();}
                progressTxt.text=st.solved+" / 8";livesTxt.text=st.lives<=0?"◇ ◇ ◇":st.lives==1?"◆ ◇ ◇":st.lives==2?"◆ ◆ ◇":"◆ ◆ ◆";
                stockTxt.text="카트리지 "+st.cartridgeRemaining+" cm  ·  남은 통 "+st.cartridgesLeft+"  ·  첫 절단 "+st.firstAttemptCorrect+" / "+st.firstAttemptTotal;
                lengthTxt.text=st.selectedLength+" cm";squareTxt.text=st.selectedLength+"² = "+(st.selectedLength*st.selectedLength);
                float width=12+224*(st.selectedLength-1)/28f;stripFillUi.rectTransform.sizeDelta=new Vector2(width,6);
                for(int i=0;i<7;i++){int v=Mathf.Clamp(st.selectedLength+i-3,1,29);tickTxt[i].text=v.ToString();tickTxt[i].color=i==3?Orange:Pale;tickTxt[i].fontSize=i==3?17:12;}
                tapeUi.rectTransform.sizeDelta=new Vector2(176f*Mathf.Clamp01(st.lives/3f),12);
            }
            if(phase==Phase.Title)
            {
                int best=PlayerPrefs.GetInt("keotlain.best",0);titleBest.text=best>0?"최고 "+best+"점 · 옥상 스티커 해금":"29 cm 카트리지 4통 · 안전 테이프 3장";
                titleCta.transform.localScale=Vector3.one*(1f+Mathf.Max(0,Mathf.Sin(worldClock*2.2f))*.018f);
            }
            if(phase==Phase.End)
            {
                endShownScore=Mathf.MoveTowards(endShownScore,st.score,Mathf.Max(110f,st.score*1.4f)*dt);endScoreTxt.text=Mathf.RoundToInt(endShownScore)+"점";
                endScoreTxt.transform.localScale=Vector3.one*(1f+Mathf.Sin(worldClock*2.8f)*.025f);
            }
            if(phase==Phase.Reveal){float t=Mathf.Clamp01(revealClock/.36f);revealRt.localScale=Vector3.one*(1f+Mathf.Sin(t*Mathf.PI)*.08f);}else revealRt.localScale=Vector3.one;
            if(guideRt.gameObject.activeSelf)
            {
                guideClock+=dt;float t=(guideClock%1.35f)/1.35f;
                bool bladeStep=practiceStripTouched;
                guideRt.anchorMin=guideRt.anchorMax=bladeStep?new Vector2(.77f,0):new Vector2(.35f,0);
                guideRt.anchoredPosition=bladeStep?new Vector2(0,Mathf.Lerp(bladeUiHome.y+36,bladeUiHome.y-22,t)):new Vector2(Mathf.Lerp(-62,40,t),135+Mathf.Sin(t*Mathf.PI)*8);
                guideRt.localScale=Vector3.one*(1f+guideBoost*.12f*Mathf.Sin(t*Mathf.PI));
            }
            if(toastClock>0){toastClock-=dt;toastG.alpha=Mathf.Clamp01(Mathf.Min(toastClock*4f,(1.45f-toastClock)*7f));toastRt.anchoredPosition=new Vector2(0,286+Mathf.Sin((1.45f-toastClock)*Mathf.PI)*5);}else toastG.alpha=0;
            if(rippleClock>0){rippleClock-=dt;float t=1-rippleClock/.44f;rippleRt.sizeDelta=Vector2.one*Mathf.Lerp(28,108,t);rippleImg.color=new Color(Cyan.r,Cyan.g,Cyan.b,(1-t)*.65f);}else rippleImg.color=new Color(Cyan.r,Cyan.g,Cyan.b,0);
            bladeUi.color=new Color(Orange.r,Orange.g,Orange.b,.90f+.10f*Mathf.Sin(worldClock*3f));
        }
    }
}
