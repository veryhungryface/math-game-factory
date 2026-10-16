// 눈토끼 시소 — 살구빛 바늘펠트 축제 디오라마와 반응형 Overlay UI.
// 정답 사건은 「시소 감쇠 두 번 → 자수 걸쇠 → k×k×k 눈격자 → 가족 합류」이고,
// 오답 사건은 「한쪽 눈더미에 착지 → 몽실 거꾸로 → 실제 k³ 푯말」이다.
// 정답 경로에는 화면 흔들림·점수 팝업·범용 파티클 폭발을 쓰지 않는다.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.NuntokkiSiso
{
    public partial class NuntokkiSisoGame
    {
        static readonly Color Snow = MgfLook.Hex("#F4F7FF");
        static readonly Color Periwinkle = MgfLook.Hex("#91A9D6");
        static readonly Color Apricot = MgfLook.Hex("#F2A36B");
        static readonly Color Plum = MgfLook.Hex("#6E4D67");
        static readonly Color Cream = MgfLook.Hex("#FFF0D2");
        static readonly Color Ink = MgfLook.Hex("#34405F");
        static readonly Color Mint = MgfLook.Hex("#8FCBB4");
        static readonly Color Wet = MgfLook.Hex("#6578A8");
        static readonly Color Sunset = MgfLook.Hex("#FFC6A4");
        static readonly Color Gold = MgfLook.Hex("#FFD37A");

        Camera cam;
        Transform worldRoot, plazaRoot, seesawRoot, seesawBeam, leftTray, rightTray;
        Transform snowmanRoot, rabbitRoot, rabbitBody, earLeft, earRight, latchRoot, titleBanner;
        Transform miniCrateRoot, handleWorld, pinWorld, rightSnowPile, leftSnowPile;
        readonly Transform[] family = new Transform[7];
        readonly Transform[] flags = new Transform[18];
        readonly Transform[] smoke = new Transform[8];
        readonly Transform[] lanternGlow = new Transform[12];
        readonly Transform[] wrongPuddles = new Transform[3];
        readonly Transform[] gridRoots = new Transform[7];
        readonly bool[] wetTicks = new bool[7];
        readonly Vector3[] familyHome = new Vector3[7];
        readonly Vector3[] smokeHome = new Vector3[8];

        Material snowMat, blueFeltMat, apricotMat, plumMat, creamMat, inkMat, mintMat, wetMat, goldMat, gridMat, barkMat;
        float worldClock, guideBoost, toastClock, rippleClock, titlePulse;
        float handleOvershoot, pinKick, correctClock, wrongClock, hitStop;
        float displayScore, endDisplayScore;
        bool animCorrect, animWrong;
        int activeGridK;

        CanvasGroup titleG, hudG, revealG, endG, toastG, rippleG;
        RectTransform titleRt, hudRt, revealRt, endRt, toastRt, rippleRt;
        RectTransform goalRt, orderRt, statusRt, gaugeRt, knobRt, pinRt, guideRt, conditionRt, evidenceRt;
        TextMeshProUGUI logoShadowTxt, logoTxt, tagTxt, titleMetaTxt, titleCtaTxt;
        TextMeshProUGUI goalTxt, orderTxt, ratioTxt, statusTxt, scoreTxt, timeTxt, conditionTxt, evidenceTxt, guideTxt;
        TextMeshProUGUI revealTitleTxt, revealFormulaTxt, revealReasonTxt;
        TextMeshProUGUI endTitleTxt, endScoreTxt, endStatsTxt, endTapTxt, toastTxt, rippleTxt;
        readonly TextMeshProUGUI[] tickTxt = new TextMeshProUGUI[7];
        readonly Image[] tickMark = new Image[7];
        readonly Image[] guideDots = new Image[7];
        Image knobImg, pinRingImg, guideRingImg, latchFlashImg;
        Sprite softSprite, ringSprite, stitchSprite;
        int shownScore = -1, shownSecond = -1, shownLives = -1, shownSolved = -1, shownK = -1;
        int lastLayoutWidth = -1, lastLayoutHeight = -1;
        bool wideLayout;
        Vector2 handleScreen, pinScreen;
        Rect gaugeRect;
        readonly Vector3[] gaugeCorners = new Vector3[4];

        RectTransform R(string name, Transform parent, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = amin; rt.anchorMax = amax; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        Image Img(string name, Transform parent, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, amin, amax, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color; image.sprite = sprite; image.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        TextMeshProUGUI Txt(string text, Transform parent, Vector2 amin, Vector2 amax, Vector2 pos, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, amin, amax, pos, Vector2.zero);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = color; t.alignment = align; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis; t.text = text;
            return t;
        }

        static Sprite MakeSprite(int size, int kind)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "FeltUi" + kind, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size * 2 - 1, dy = (y + .5f) / size * 2 - 1;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a;
                if (kind == 1) a = Mathf.Clamp01(1 - Mathf.Abs(d - .76f) / .10f);
                else if (kind == 2)
                {
                    // 9-slice용 둥근 펠트 판. 이전 방사형 무늬는 카드를 늘렸을 때 소용돌이처럼
                    // 비어 보여 수치가 배경과 섞였다. 알파는 안쪽에서 완전히 불투명하게 둔다.
                    float qx = Mathf.Abs(dx) - .70f, qy = Mathf.Abs(dy) - .70f;
                    float outside = Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0));
                    float inside = Mathf.Min(Mathf.Max(qx, qy), 0);
                    float sd = outside + inside - .22f;
                    a = Mathf.Clamp01(.5f - sd * 12f);
                }
                else a = Mathf.Clamp01((1 - d) * 8f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            if (kind == 2)
                return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(22, 22, 22, 22));
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100);
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#FFB99C"), MgfLook.Hex("#FFD9C2"), MgfLook.Hex("#C8D7F4"), .84f);
            MgfLook.Sun(new Vector3(38, -28, 12), Cream, 1.06f, .50f);
            cam = MgfLook.Camera(new Vector3(13.8f, 12.4f, -20.5f), new Vector3(0, 1.7f, 2.9f), 32f);
            cam.transform.rotation = Quaternion.Euler(25f, -32f, 0f);
            cam.transform.LookAt(new Vector3(0, 1.7f, 2.9f));

            var felt = Resources.Load<Texture2D>("NuntokkiSiso/felt-normal");
            if (felt) felt.wrapMode = TextureWrapMode.Repeat;
            snowMat = MgfLook.Lit(Snow, .16f, 0f, Snow * .025f, felt);
            blueFeltMat = MgfLook.Lit(Periwinkle, .18f, 0f, Periwinkle * .035f, felt);
            apricotMat = MgfLook.Lit(Apricot, .20f, 0f, Apricot * .045f, felt);
            plumMat = MgfLook.Lit(Plum, .16f, 0f, Plum * .025f, felt);
            creamMat = MgfLook.Lit(Cream, .18f, 0f, Cream * .035f, felt);
            inkMat = MgfLook.Lit(Ink, .12f, 0f, Ink * .02f, felt);
            mintMat = MgfLook.Lit(Mint, .14f, 0f, Mint * .025f, felt);
            wetMat = MgfLook.Lit(Wet, .34f, .01f, Wet * .06f);
            goldMat = MgfLook.Lit(Gold, .45f, .02f, Gold * .15f);
            gridMat = MgfLook.Lit(MgfLook.Hex("#CBE9FF"), .52f, 0f, MgfLook.Hex("#9EDAFF") * .12f);
            barkMat = MgfLook.Lit(MgfLook.Hex("#72556B"), .12f, 0f, null, felt);

            worldRoot = new GameObject("ApricotFeltSnowFestival").transform;
            plazaRoot = new GameObject("SteppedFestivalPlaza").transform;
            plazaRoot.SetParent(worldRoot, false);

            // 비어 있는 탁자가 아니라 화면 끝까지 이어지는 원형 축제 광장과 계단.
            MgfLook.Prim(PrimitiveType.Cylinder, "SnowField", new Vector3(0, -.75f, 3.0f), new Vector3(25f, .75f, 25f), snowMat, plazaRoot, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "PlazaLower", new Vector3(0, -.05f, 3.0f), new Vector3(17.5f, .50f, 17.5f), blueFeltMat, plazaRoot, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "PlazaMiddle", new Vector3(0, .38f, 3.0f), new Vector3(14.4f, .35f, 14.4f), snowMat, plazaRoot, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "PlazaTop", new Vector3(0, .70f, 3.1f), new Vector3(11.8f, .28f, 11.8f), creamMat, plazaRoot, false);
            for (int i = 0; i < 22; i++)
            {
                float a = i / 22f * Mathf.PI * 2f;
                float r = 9.1f + (i % 3) * .45f;
                MgfLook.Prim(PrimitiveType.Sphere, "SoftSnowBank" + i,
                    new Vector3(Mathf.Cos(a) * r, -.05f + (i % 2) * .16f, 3f + Mathf.Sin(a) * r),
                    new Vector3(2.5f, .8f, 1.8f), i % 4 == 0 ? creamMat : snowMat, plazaRoot, false);
            }

            BuildFestivalStalls();
            BuildTreesAndFlags();

            // 타이틀의 제목은 세계 안의 자수 현수막 위에 놓인다.
            titleBanner = MgfLook.Block("StitchedTitleBunting", new Vector3(0, 8.7f, 8.0f), new Vector3(10.8f, 2.2f, .24f), .36f, creamMat, worldRoot).transform;
            for (int i = 0; i < 13; i++)
            {
                float x = -4.8f + i * .8f;
                MgfLook.Prim(PrimitiveType.Sphere, "BannerStitch" + i, new Vector3(x, 9.66f, 7.78f), new Vector3(.16f, .10f, .10f), apricotMat, worldRoot, false);
                MgfLook.Prim(PrimitiveType.Sphere, "BannerStitchBottom" + i, new Vector3(x, 7.74f, 7.78f), new Vector3(.16f, .10f, .10f), apricotMat, worldRoot, false);
            }

            seesawRoot = new GameObject("LongStitchedSeesaw").transform;
            seesawRoot.SetParent(worldRoot, false);
            seesawRoot.localPosition = new Vector3(0, 1.75f, 1.65f);
            MgfLook.Prim(PrimitiveType.Cylinder, "EmbroideredPivot", new Vector3(0, -.72f, 0), new Vector3(2.0f, .72f, 2.0f), blueFeltMat, seesawRoot, false).transform.localRotation = Quaternion.Euler(90, 0, 0);
            MgfLook.Block("PivotStand", new Vector3(0, -1.12f, .05f), new Vector3(1.5f, 1.5f, 1.3f), .30f, plumMat, seesawRoot);
            seesawBeam = new GameObject("SeesawMovingBeam").transform;
            seesawBeam.SetParent(seesawRoot, false);
            MgfLook.Block("FeltBeam", Vector3.zero, new Vector3(12.8f, .52f, 1.0f), .24f, apricotMat, seesawBeam);
            MgfLook.Block("BlueStitchStripe", new Vector3(0, .0f, -.54f), new Vector3(12.0f, .14f, .10f), .05f, blueFeltMat, seesawBeam);
            for (int i = 0; i < 15; i++)
                MgfLook.Prim(PrimitiveType.Sphere, "BeamStitch" + i, new Vector3(-5.8f + i * .83f, .30f, -.28f), new Vector3(.13f, .10f, .11f), creamMat, seesawBeam, false);
            leftTray = MgfLook.Block("LeftSnowTray", new Vector3(-5.0f, .58f, 0), new Vector3(3.2f, .25f, 2.6f), .20f, blueFeltMat, seesawBeam).transform;
            rightTray = MgfLook.Block("RightSnowTray", new Vector3(5.0f, .58f, 0), new Vector3(3.2f, .25f, 2.6f), .20f, blueFeltMat, seesawBeam).transform;

            miniCrateRoot = new GameObject("MiniSnowBundle").transform;
            miniCrateRoot.SetParent(leftTray, false);
            miniCrateRoot.localPosition = new Vector3(0, .40f, 0);
            for (int i = 0; i < 18; i++)
            {
                int x = i % 6, z = (i / 6) % 3;
                var ball = MgfLook.Prim(PrimitiveType.Sphere, "MiniSnowball" + i,
                    new Vector3((x - 2.5f) * .34f, .14f + (i % 2) * .06f, (z - 1) * .38f),
                    new Vector3(.31f, .31f, .31f), snowMat, miniCrateRoot, false);
                ball.transform.localRotation = Quaternion.Euler(0, i * 23f, 0);
            }
            MgfLook.Block("BundleApricotTie", new Vector3(0, .18f, 0), new Vector3(2.25f, .12f, .18f), .04f, apricotMat, miniCrateRoot);

            snowmanRoot = CreateSnowman("PlayerScaledSnowman", rightTray, new Vector3(0, .32f, 0), blueFeltMat, 1f);
            snowmanRoot.localScale = Vector3.one * (visualK * .30f);
            handleWorld = MgfLook.Block("HeightHandleWorld", new Vector3(7.05f, 3.05f, .60f), new Vector3(.62f, .62f, .62f), .22f, goldMat, worldRoot).transform;
            MgfLook.Block("HeightRailWorld", new Vector3(7.05f, 3.10f, .78f), new Vector3(.20f, 6.3f, .18f), .08f, plumMat, worldRoot);

            latchRoot = new GameObject("CenterEmbroideredLatch").transform;
            latchRoot.SetParent(worldRoot, false);
            latchRoot.localPosition = new Vector3(0, 2.15f, .35f);
            MgfLook.Prim(PrimitiveType.Sphere, "LatchFlower", Vector3.zero, new Vector3(.86f, .86f, .34f), goldMat, latchRoot, false);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                MgfLook.Prim(PrimitiveType.Sphere, "LatchPetal" + i, new Vector3(Mathf.Cos(a) * .54f, Mathf.Sin(a) * .54f, 0), new Vector3(.30f, .18f, .18f), creamMat, latchRoot, false);
            }

            rabbitRoot = BuildRabbit();
            rabbitRoot.SetParent(worldRoot, false);
            rabbitRoot.localPosition = new Vector3(.15f, .78f, -2.4f);
            pinWorld = new GameObject("RabbitPinRope").transform;
            pinWorld.SetParent(rabbitRoot, false);
            pinWorld.localPosition = new Vector3(1.05f, 1.2f, -.22f);
            MgfLook.Block("YarnRope", new Vector3(0, -.52f, 0), new Vector3(.18f, 1.25f, .16f), .07f, plumMat, pinWorld);
            var pinDisc = MgfLook.Prim(PrimitiveType.Cylinder, "PinRing", new Vector3(0, -1.18f, 0), new Vector3(.72f, .10f, .72f), apricotMat, pinWorld, false);
            pinDisc.transform.localRotation = Quaternion.Euler(90, 0, 0);
            MgfLook.Prim(PrimitiveType.Sphere, "PinRingCenter", new Vector3(0, -1.18f, -.10f), new Vector3(.34f, .34f, .12f), creamMat, pinWorld, false);

            rightSnowPile = MgfLook.Prim(PrimitiveType.Sphere, "RightLandingSnow", new Vector3(5.0f, .20f, 1.65f), new Vector3(4.2f, .35f, 3.0f), snowMat, worldRoot, false).transform;
            leftSnowPile = MgfLook.Prim(PrimitiveType.Sphere, "LeftLandingSnow", new Vector3(-5.0f, .20f, 1.65f), new Vector3(4.2f, .35f, 3.0f), snowMat, worldRoot, false).transform;

            for (int i = 0; i < family.Length; i++)
            {
                float a = Mathf.Lerp(-1.05f, 1.05f, i / 6f);
                float r = 4.9f;
                familyHome[i] = new Vector3(Mathf.Sin(a) * r, 1.18f + (i % 2) * .28f, 5.45f + Mathf.Cos(a) * 1.5f);
                family[i] = CreateSnowman("FestivalFamily" + i, worldRoot, familyHome[i], i % 2 == 0 ? apricotMat : blueFeltMat, .52f + i * .035f);
                family[i].gameObject.SetActive(false);
            }

            for (int k = NuntokkiRules.MinK; k <= NuntokkiRules.MaxK; k++)
            {
                var go = new GameObject("VolumeGrid" + k);
                go.transform.SetParent(worldRoot, false);
                go.transform.localPosition = new Vector3(5.0f, 2.25f, 1.65f);
                go.AddComponent<MeshFilter>().sharedMesh = BuildCubeGridMesh(k);
                var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = gridMat;
                renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
                go.transform.localScale = Vector3.one * Mathf.Lerp(.64f, .34f, (k - 2) / 6f);
                go.SetActive(false);
                gridRoots[k - 2] = go.transform;
            }

            for (int i = 0; i < wrongPuddles.Length; i++)
            {
                wrongPuddles[i] = MgfLook.Prim(PrimitiveType.Sphere, "FrozenWrongPuddle" + i,
                    new Vector3(-2.2f + i * 2.1f, .76f, 5.95f - (i % 2) * .6f), new Vector3(1.65f, .10f, 1.15f), wetMat, worldRoot, false).transform;
                wrongPuddles[i].gameObject.SetActive(false);
            }
        }

        void BuildFestivalStalls()
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 8.0f;
                MgfLook.Block("CocoaStallBase" + side, new Vector3(x, .65f, 5.8f), new Vector3(3.4f, 2.1f, 2.8f), .34f, side < 0 ? apricotMat : blueFeltMat, worldRoot);
                MgfLook.Block("CocoaAwning" + side, new Vector3(x, 2.25f, 5.5f), new Vector3(4.0f, .38f, 3.5f), .18f, creamMat, worldRoot).transform.localRotation = Quaternion.Euler(0, 0, side * 3f);
                for (int j = 0; j < 4; j++)
                {
                    MgfLook.Prim(PrimitiveType.Cylinder, "CocoaCup" + side + "_" + j,
                        new Vector3(x - .9f + j * .6f, 1.85f, 4.2f), new Vector3(.36f, .24f, .36f), plumMat, worldRoot, false);
                    int idx = (side < 0 ? 0 : 4) + j;
                    smokeHome[idx] = new Vector3(x - .9f + j * .6f, 2.35f, 4.2f);
                    smoke[idx] = MgfLook.Prim(PrimitiveType.Sphere, "CocoaSteam" + idx, smokeHome[idx], new Vector3(.20f, .30f, .20f), creamMat, worldRoot, false).transform;
                }
            }
            for (int i = 0; i < lanternGlow.Length; i++)
            {
                float a = i / (float)lanternGlow.Length * Mathf.PI * 2f;
                float r = 8.2f;
                float x = Mathf.Cos(a) * r, z = 3f + Mathf.Sin(a) * r;
                MgfLook.Block("LanternPost" + i, new Vector3(x, 1.75f, z), new Vector3(.20f, 3.6f, .20f), .07f, barkMat, worldRoot);
                lanternGlow[i] = MgfLook.Prim(PrimitiveType.Sphere, "WarmFeltLantern" + i, new Vector3(x, 3.55f, z), new Vector3(.58f, .72f, .58f), goldMat, worldRoot, false).transform;
            }
        }

        void BuildTreesAndFlags()
        {
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f + .2f;
                float r = 10.2f;
                float x = Mathf.Cos(a) * r, z = 3f + Mathf.Sin(a) * r;
                MgfLook.Prim(PrimitiveType.Cylinder, "PineTrunk" + i, new Vector3(x, .8f, z), new Vector3(.48f, 2.3f, .48f), barkMat, worldRoot, false);
                for (int tier = 0; tier < 3; tier++)
                    MgfLook.Prim(PrimitiveType.Cylinder, "FeltPineTier" + i + "_" + tier, new Vector3(x, 1.7f + tier * .85f, z), new Vector3(2.3f - tier * .45f, .85f, 2.3f - tier * .45f), tier % 2 == 0 ? mintMat : blueFeltMat, worldRoot, false);
            }
            for (int i = 0; i < flags.Length; i++)
            {
                float x = -8.4f + i * .98f;
                float y = 7.1f + Mathf.Sin(i * .55f) * .32f;
                flags[i] = MgfLook.Block("TinyFestivalFlag" + i, new Vector3(x, y, 7.65f), new Vector3(.72f, .62f, .10f), .08f,
                    i % 3 == 0 ? apricotMat : (i % 3 == 1 ? blueFeltMat : plumMat), worldRoot).transform;
            }
        }

        Transform CreateSnowman(string name, Transform parent, Vector3 pos, Material scarf, float baseScale)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false); root.localPosition = pos; root.localScale = Vector3.one * baseScale;
            MgfLook.Prim(PrimitiveType.Sphere, "Body", new Vector3(0, .76f, 0), new Vector3(1.48f, 1.38f, 1.34f), snowMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Chest", new Vector3(0, 1.76f, 0), new Vector3(1.13f, 1.06f, 1.02f), snowMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0, 2.62f, 0), new Vector3(.91f, .88f, .86f), snowMat, root, false);
            MgfLook.Block("Scarf", new Vector3(0, 2.13f, -.12f), new Vector3(1.08f, .28f, .80f), .13f, scarf, root);
            MgfLook.Block("ScarfTail", new Vector3(.38f, 1.73f, -.43f), new Vector3(.28f, .85f, .18f), .09f, scarf, root).transform.localRotation = Quaternion.Euler(0, 0, -12);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeL", new Vector3(-.20f, 2.76f, -.42f), new Vector3(.10f, .12f, .08f), inkMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeR", new Vector3(.20f, 2.76f, -.42f), new Vector3(.10f, .12f, .08f), inkMat, root, false);
            var nose = MgfLook.Prim(PrimitiveType.Capsule, "ApricotNose", new Vector3(0, 2.58f, -.53f), new Vector3(.12f, .32f, .12f), apricotMat, root, false);
            nose.transform.localRotation = Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < 3; i++)
                MgfLook.Prim(PrimitiveType.Sphere, "Button" + i, new Vector3(0, 1.74f - i * .42f, -.53f), new Vector3(.12f, .12f, .08f), plumMat, root, false);
            return root;
        }

        Transform BuildRabbit()
        {
            var root = new GameObject("MongsilOriginalFeltRabbit").transform;
            rabbitBody = MgfLook.Prim(PrimitiveType.Sphere, "MarshmallowBody", new Vector3(0, .74f, 0), new Vector3(1.25f, 1.35f, 1.05f), snowMat, root, false).transform;
            MgfLook.Prim(PrimitiveType.Sphere, "RabbitHead", new Vector3(0, 1.70f, -.04f), new Vector3(1.02f, .94f, .92f), snowMat, root, false);
            earLeft = MgfLook.Block("SoftEarLeft", new Vector3(-.31f, 2.65f, .02f), new Vector3(.38f, 1.42f, .34f), .17f, snowMat, root).transform;
            earRight = MgfLook.Block("SoftEarRight", new Vector3(.31f, 2.65f, .02f), new Vector3(.38f, 1.42f, .34f), .17f, snowMat, root).transform;
            earLeft.localRotation = Quaternion.Euler(0, 0, 8); earRight.localRotation = Quaternion.Euler(0, 0, -8);
            MgfLook.Block("ApricotScarf", new Vector3(0, 1.30f, -.20f), new Vector3(1.10f, .30f, .72f), .13f, apricotMat, root);
            MgfLook.Prim(PrimitiveType.Sphere, "RabbitEyeL", new Vector3(-.20f, 1.84f, -.47f), new Vector3(.10f, .12f, .08f), inkMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "RabbitEyeR", new Vector3(.20f, 1.84f, -.47f), new Vector3(.10f, .12f, .08f), inkMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "RabbitNose", new Vector3(0, 1.68f, -.53f), new Vector3(.11f, .09f, .08f), apricotMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "RabbitFootL", new Vector3(-.35f, .10f, -.16f), new Vector3(.56f, .28f, .62f), snowMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "RabbitFootR", new Vector3(.35f, .10f, -.16f), new Vector3(.56f, .28f, .62f), snowMat, root, false);
            return root;
        }

        Mesh BuildCubeGridMesh(int k)
        {
            var vertices = new List<Vector3>(k * k * k * 24);
            var normals = new List<Vector3>(k * k * k * 24);
            var triangles = new List<int>(k * k * k * 36);
            Vector3[] faceNormals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            Vector3[] faceU = { Vector3.forward, Vector3.back, Vector3.right, Vector3.right, Vector3.right, Vector3.left };
            Vector3[] faceV = { Vector3.up, Vector3.up, Vector3.back, Vector3.forward, Vector3.up, Vector3.up };
            float spacing = 1.06f, half = .45f;
            Vector3 origin = new Vector3(-(k - 1) * spacing * .5f, 0, -(k - 1) * spacing * .5f);
            for (int y = 0; y < k; y++)
            for (int z = 0; z < k; z++)
            for (int x = 0; x < k; x++)
            {
                Vector3 center = origin + new Vector3(x * spacing, y * spacing, z * spacing);
                for (int f = 0; f < 6; f++)
                {
                    int start = vertices.Count;
                    Vector3 n = faceNormals[f], u = faceU[f] * half, v = faceV[f] * half;
                    vertices.Add(center + n * half - u - v);
                    vertices.Add(center + n * half - u + v);
                    vertices.Add(center + n * half + u + v);
                    vertices.Add(center + n * half + u - v);
                    normals.Add(n); normals.Add(n); normals.Add(n); normals.Add(n);
                    Vector3 e1 = vertices[start + 1] - vertices[start];
                    Vector3 e2 = vertices[start + 2] - vertices[start];
                    if (Vector3.Dot(Vector3.Cross(e1, e2), n) >= 0)
                    {
                        triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
                    }
                    else
                    {
                        triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                        triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
                    }
                }
            }
            var mesh = new Mesh { name = "CombinedVolumeGrid" + k };
            if (vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }

        void BuildUi()
        {
            softSprite = MakeSprite(96, 0); ringSprite = MakeSprite(96, 1); stitchSprite = MakeSprite(96, 2);
            var canvas = MgfText.Canvas;

            titleRt = R("TitleWorldOverlay", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            Img("LogoBuntingPlate", titleRt, new Vector2(.19f, .72f), new Vector2(.81f, .92f), Vector2.zero, Vector2.zero, new Color(Cream.r, Cream.g, Cream.b, .95f), stitchSprite);
            logoShadowTxt = Txt("눈토끼 시소", titleRt, new Vector2(.08f, .72f), new Vector2(.92f, .91f), new Vector2(3, -5), 48, Plum);
            logoShadowTxt.fontStyle = FontStyles.Bold; logoShadowTxt.outlineWidth = .28f; logoShadowTxt.outlineColor = Ink;
            logoTxt = Txt("눈토끼 시소", titleRt, new Vector2(.08f, .72f), new Vector2(.92f, .91f), Vector2.zero, 48, Cream);
            logoTxt.fontStyle = FontStyles.Bold; logoTxt.outlineWidth = .19f; logoTxt.outlineColor = Apricot;
            tagTxt = Txt("부피만큼 키워 균형을 잡아라", titleRt, new Vector2(.14f, .62f), new Vector2(.86f, .71f), Vector2.zero, 18, Ink);
            tagTxt.fontStyle = FontStyles.Bold;
            var cta = Img("WorldPinCta", titleRt, new Vector2(.28f, .12f), new Vector2(.72f, .25f), Vector2.zero, Vector2.zero, new Color(Cream.r, Cream.g, Cream.b, .95f), stitchSprite);
            titleCtaTxt = Txt("털실 고정핀을 당겨 시작", cta.transform, new Vector2(.05f, .10f), new Vector2(.95f, .90f), Vector2.zero, 19, Plum);
            titleCtaTxt.fontStyle = FontStyles.Bold;
            titleMetaTxt = Txt("중2 · 도형의 닮음    최고 첫 시도 0/7", titleRt, new Vector2(.12f, .035f), new Vector2(.88f, .105f), Vector2.zero, 14, Ink);

            hudRt = R("Hud", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            var goalBg = Img("PersistentGoal", hudRt, new Vector2(.04f, .91f), new Vector2(.96f, .985f), Vector2.zero, Vector2.zero, new Color(Ink.r, Ink.g, Ink.b, .94f), stitchSprite);
            goalRt = goalBg.rectTransform;
            goalTxt = Txt("높이를 정한 뒤 핀을 당겨 균형을 확인하세요", goalBg.transform, new Vector2(.04f, .06f), new Vector2(.96f, .94f), Vector2.zero, 15, Cream);
            goalTxt.overflowMode = TextOverflowModes.Overflow;

            var orderBg = Img("SnowOrderBoard", hudRt, new Vector2(.035f, .69f), new Vector2(.67f, .90f), Vector2.zero, Vector2.zero, new Color(Cream.r, Cream.g, Cream.b, .97f), stitchSprite);
            orderRt = orderBg.rectTransform;
            orderTxt = Txt("눈축제 의뢰", orderBg.transform, new Vector2(.05f, .48f), new Vector2(.95f, .95f), Vector2.zero, 16, Ink, TextAlignmentOptions.MidlineLeft);
            ratioTxt = Txt("미니 8개분", orderBg.transform, new Vector2(.05f, .06f), new Vector2(.95f, .50f), Vector2.zero, 23, Plum, TextAlignmentOptions.MidlineLeft);
            ratioTxt.fontStyle = FontStyles.Bold;
            orderTxt.overflowMode = TextOverflowModes.Overflow;
            ratioTxt.overflowMode = TextOverflowModes.Overflow;

            statusRt = R("Status", hudRt, new Vector2(.025f, .02f), new Vector2(.68f, .15f), Vector2.zero, Vector2.zero);
            var statusBg = statusRt.gameObject.AddComponent<Image>(); statusBg.color = new Color(Cream.r, Cream.g, Cream.b, .91f); statusBg.sprite = stitchSprite; statusBg.raycastTarget = false;
            scoreTxt = Txt("0점", statusRt, new Vector2(.04f, .12f), new Vector2(.30f, .88f), Vector2.zero, 22, Apricot, TextAlignmentOptions.MidlineLeft); scoreTxt.fontStyle = FontStyles.Bold;
            timeTxt = Txt("140초", statusRt, new Vector2(.31f, .12f), new Vector2(.52f, .88f), Vector2.zero, 18, Ink);
            statusTxt = Txt("♡♡♡ · 0/7", statusRt, new Vector2(.52f, .12f), new Vector2(.97f, .88f), Vector2.zero, 18, Plum, TextAlignmentOptions.MidlineRight);

            var conditionBg = Img("PhysicsContract", hudRt, new Vector2(.04f, .17f), new Vector2(.67f, .27f), Vector2.zero, Vector2.zero, new Color(Periwinkle.r, Periwinkle.g, Periwinkle.b, .94f), stitchSprite);
            conditionRt = conditionBg.rectTransform;
            conditionTxt = Txt("같은 재료·같은 밀도\n양팔 길이 같음", conditionBg.transform, new Vector2(.04f, .08f), new Vector2(.96f, .92f), Vector2.zero, 13, Ink);
            conditionTxt.overflowMode = TextOverflowModes.Overflow;

            var evidenceBg = Img("LayerEvidence", hudRt, new Vector2(.04f, .28f), new Vector2(.67f, .37f), Vector2.zero, Vector2.zero, new Color(Snow.r, Snow.g, Snow.b, .94f), stitchSprite);
            evidenceRt = evidenceBg.rectTransform;
            evidenceTxt = Txt("", evidenceBg.transform, new Vector2(.04f, .08f), new Vector2(.96f, .92f), Vector2.zero, 15, Ink);
            evidenceTxt.overflowMode = TextOverflowModes.Overflow;

            var gaugeBg = Img("HeightGauge", hudRt, new Vector2(.70f, .20f), new Vector2(.97f, .88f), Vector2.zero, Vector2.zero, new Color(Cream.r, Cream.g, Cream.b, .97f), stitchSprite);
            gaugeRt = gaugeBg.rectTransform;
            Txt("높이", gaugeRt, new Vector2(.05f, .91f), new Vector2(.95f, .99f), Vector2.zero, 17, Plum).fontStyle = FontStyles.Bold;
            Img("GaugeRail", gaugeRt, new Vector2(.22f, .08f), new Vector2(.32f, .89f), Vector2.zero, Vector2.zero, new Color(Ink.r, Ink.g, Ink.b, .28f), softSprite);
            for (int i = 0; i < 7; i++)
            {
                float y = .11f + i * (.75f / 6f);
                tickMark[i] = Img("Tick" + (i + 2), gaugeRt, new Vector2(.30f, y), new Vector2(.47f, y), Vector2.zero, new Vector2(0, 3), Ink);
                tickTxt[i] = Txt(((i + 2) * 2) + " cm", gaugeRt, new Vector2(.46f, y - .046f), new Vector2(.96f, y + .046f), Vector2.zero, 17, Ink, TextAlignmentOptions.MidlineLeft);
                tickTxt[i].textWrappingMode = TextWrappingModes.NoWrap; tickTxt[i].overflowMode = TextOverflowModes.Overflow;
            }
            knobImg = Img("FeltHeightHandle", hudRt, new Vector2(.72f, .47f), new Vector2(.89f, .54f), Vector2.zero, Vector2.zero, Gold, stitchSprite);
            knobRt = knobImg.rectTransform;
            var knobLabel = Txt("10 cm", knobRt, new Vector2(.06f, .10f), new Vector2(.94f, .90f), Vector2.zero, 15, Ink, TextAlignmentOptions.Center);
            knobLabel.name = "KnobLabel";
            knobLabel.textWrappingMode = TextWrappingModes.NoWrap;
            knobLabel.overflowMode = TextOverflowModes.Overflow;

            var pinBg = Img("PinPullZone", hudRt, new Vector2(.26f, .02f), new Vector2(.70f, .165f), Vector2.zero, Vector2.zero, new Color(Apricot.r, Apricot.g, Apricot.b, .94f), stitchSprite);
            pinRt = pinBg.rectTransform;
            Img("PinRopeUi", pinRt, new Vector2(.47f, .34f), new Vector2(.53f, .92f), Vector2.zero, Vector2.zero, Plum, softSprite);
            pinRingImg = Img("PinRingUi", pinRt, new Vector2(.36f, .04f), new Vector2(.64f, .45f), Vector2.zero, Vector2.zero, Cream, ringSprite);
            Txt("아래로 당겨 시소 풀기", pinRt, new Vector2(-.10f, -.04f), new Vector2(1.10f, .25f), Vector2.zero, 14, Ink).fontStyle = FontStyles.Bold;

            guideRt = R("LiveGuide", hudRt, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(92, 92));
            guideRingImg = Img("GuidePulse", guideRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Gold, ringSprite);
            guideTxt = Txt("↕", guideRt, new Vector2(-.5f, -.48f), new Vector2(1.5f, -.02f), Vector2.zero, 24, Cream);
            for (int i = 0; i < guideDots.Length; i++)
                guideDots[i] = Img("GuidePath" + i, hudRt, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(18, 18), i % 2 == 0 ? Gold : Cream, softSprite);

            revealRt = R("Reveal", canvas.transform, new Vector2(.07f, .32f), new Vector2(.93f, .69f), Vector2.zero, Vector2.zero);
            revealG = revealRt.gameObject.AddComponent<CanvasGroup>();
            Img("RevealFelt", revealRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(Cream.r, Cream.g, Cream.b, .98f), stitchSprite);
            latchFlashImg = Img("LatchFlash", revealRt, new Vector2(.84f, .76f), new Vector2(.96f, .95f), Vector2.zero, Vector2.zero, Gold, stitchSprite);
            revealTitleTxt = Txt("수평 걸쇠 잠김", revealRt, new Vector2(.06f, .68f), new Vector2(.81f, .94f), Vector2.zero, 27, Plum); revealTitleTxt.fontStyle = FontStyles.Bold;
            revealFormulaTxt = Txt("2×2×2=8", revealRt, new Vector2(.05f, .30f), new Vector2(.95f, .69f), Vector2.zero, 24, Ink); revealFormulaTxt.fontStyle = FontStyles.Bold;
            revealReasonTxt = Txt("닮음비 1:2 → 부피의 비 1:8", revealRt, new Vector2(.06f, .06f), new Vector2(.94f, .31f), Vector2.zero, 16, Ink);
            revealTitleTxt.overflowMode = TextOverflowModes.Overflow;
            revealFormulaTxt.overflowMode = TextOverflowModes.Overflow;
            revealReasonTxt.overflowMode = TextOverflowModes.Overflow;

            endRt = R("End", canvas.transform, new Vector2(.07f, .18f), new Vector2(.93f, .82f), Vector2.zero, Vector2.zero);
            endG = endRt.gameObject.AddComponent<CanvasGroup>();
            Img("EndFestivalBanner", endRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(Cream.r, Cream.g, Cream.b, .98f), stitchSprite);
            endTitleTxt = Txt("눈축제 광장 완성", endRt, new Vector2(.06f, .72f), new Vector2(.94f, .94f), Vector2.zero, 33, Plum); endTitleTxt.fontStyle = FontStyles.Bold;
            endScoreTxt = Txt("0", endRt, new Vector2(.06f, .42f), new Vector2(.94f, .73f), Vector2.zero, 48, Apricot); endScoreTxt.fontStyle = FontStyles.Bold;
            endStatsTxt = Txt("", endRt, new Vector2(.08f, .15f), new Vector2(.92f, .44f), Vector2.zero, 18, Ink);
            endTapTxt = Txt("한 번 눌러 다시 시작", endRt, new Vector2(.08f, .03f), new Vector2(.92f, .16f), Vector2.zero, 17, Plum); endTapTxt.fontStyle = FontStyles.Bold;

            toastRt = R("Toast", canvas.transform, new Vector2(.12f, .38f), new Vector2(.88f, .49f), Vector2.zero, Vector2.zero);
            toastG = toastRt.gameObject.AddComponent<CanvasGroup>();
            Img("ToastFelt", toastRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(Ink.r, Ink.g, Ink.b, .94f), stitchSprite);
            toastTxt = Txt("", toastRt, new Vector2(.04f, .06f), new Vector2(.96f, .94f), Vector2.zero, 14, Cream);
            rippleRt = R("Ripple", canvas.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(100, 100));
            rippleG = rippleRt.gameObject.AddComponent<CanvasGroup>();
            Img("RippleRing", rippleRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Periwinkle, ringSprite);
            rippleTxt = Txt("", rippleRt, new Vector2(-1.2f, -1.0f), new Vector2(2.2f, -.22f), Vector2.zero, 13, Cream);

            ApplyResponsiveLayout(true);
        }

        void SetGroup(CanvasGroup group, bool on)
        {
            group.alpha = on ? 1 : 0; group.blocksRaycasts = false; group.interactable = false; group.gameObject.SetActive(on);
        }

        void SetScreen()
        {
            bool revealing = mode == Mode.Reveal;
            SetGroup(titleG, mode == Mode.Title);
            SetGroup(hudG, mode == Mode.Practice || mode == Mode.Playing || revealing);
            SetGroup(revealG, revealing);
            SetGroup(endG, mode == Mode.End);
            orderRt.gameObject.SetActive(!revealing);
            conditionRt.gameObject.SetActive(!revealing);
            evidenceRt.gameObject.SetActive(!revealing && current != null && (current.band <= 2 || mode == Mode.Practice));
            pinRt.gameObject.SetActive(!revealing);
            titleBanner.gameObject.SetActive(mode == Mode.Title);
            ApplyResponsiveLayout(true);
        }

        static void Anchors(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        }

        void ApplyResponsiveLayout(bool force = false)
        {
            if (!force && lastLayoutWidth == Screen.width && lastLayoutHeight == Screen.height) return;
            lastLayoutWidth = Screen.width; lastLayoutHeight = Screen.height;
            wideLayout = Screen.width / (float)Mathf.Max(1, Screen.height) >= 1.28f;
            st.layout = wideLayout ? "wide-plaza-three-zone" : "portrait-stacked-plaza";
            if (wideLayout)
            {
                Anchors(goalRt, .20f, .91f, .78f, .985f);
                Anchors(orderRt, .02f, .65f, .34f, .90f);
                Anchors(conditionRt, .02f, .51f, .34f, .63f);
                Anchors(evidenceRt, .02f, .38f, .34f, .50f);
                Anchors(gaugeRt, mode == Mode.Reveal ? .84f : .81f, .15f, .98f, .89f);
                Anchors(statusRt, .02f, .02f, .34f, .15f);
                Anchors(pinRt, .39f, .02f, .66f, .17f);
                Anchors(revealRt, .37f, .58f, .76f, .89f);
                Anchors(toastRt, .37f, .43f, .76f, .54f);
                orderTxt.fontSize = 16; ratioTxt.fontSize = 20; goalTxt.fontSize = 15; conditionTxt.fontSize = 14; evidenceTxt.fontSize = 14;
                revealTitleTxt.fontSize = 21; revealFormulaTxt.fontSize = 19; revealReasonTxt.fontSize = 13;
            }
            else
            {
                Anchors(goalRt, .04f, .91f, .70f, .985f);
                Anchors(orderRt, .035f, .70f, .62f, .90f);
                Anchors(conditionRt, .04f, .38f, .62f, .48f);
                Anchors(evidenceRt, .04f, .49f, .62f, .59f);
                Anchors(gaugeRt, mode == Mode.Reveal ? .74f : .70f, .20f, .97f, .88f);
                Anchors(statusRt, .025f, .02f, .62f, .13f);
                Anchors(pinRt, .18f, .145f, .62f, .285f);
                Anchors(revealRt, .04f, .56f, .66f, .89f);
                Anchors(toastRt, .08f, .43f, .64f, .53f);
                orderTxt.fontSize = 15; ratioTxt.fontSize = 22; goalTxt.fontSize = 13; conditionTxt.fontSize = 12; evidenceTxt.fontSize = 13;
                revealTitleTxt.fontSize = 21; revealFormulaTxt.fontSize = 18; revealReasonTxt.fontSize = 13;
            }
            SyncControlGeometry();
        }

        Vector2 ScreenPoint(RectTransform rt)
        {
            return RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
        }

        void SyncControlGeometry()
        {
            if (gaugeRt == null || pinRt == null) return;
            gaugeRt.GetWorldCorners(gaugeCorners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(null, gaugeCorners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(null, gaugeCorners[2]);
            gaugeRect = Rect.MinMaxRect(a.x, a.y + (b.y - a.y) * .10f, b.x, a.y + (b.y - a.y) * .86f);
            float t = Mathf.InverseLerp(NuntokkiRules.MinK, NuntokkiRules.MaxK, visualK);
            handleScreen = new Vector2(Mathf.Lerp(gaugeRect.xMin, gaugeRect.xMax, .22f), Mathf.Lerp(gaugeRect.yMin, gaugeRect.yMax, t));
            pinScreen = ScreenPoint(pinRt) + new Vector2(0, -pinRt.rect.height * .17f);
            st.handleX = Mathf.RoundToInt(handleScreen.x); st.handleY = Screen.height - Mathf.RoundToInt(handleScreen.y);
            st.pinX = Mathf.RoundToInt(pinScreen.x); st.pinY = Screen.height - Mathf.RoundToInt(pinScreen.y);
            st.gaugeTopY = Screen.height - Mathf.RoundToInt(gaugeRect.yMax);
            st.gaugeBottomY = Screen.height - Mathf.RoundToInt(gaugeRect.yMin);
        }

        void ResetWorldForRun()
        {
            animCorrect = animWrong = false; correctClock = wrongClock = hitStop = 0;
            seesawBeam.localRotation = Quaternion.identity;
            rabbitRoot.localRotation = Quaternion.identity;
            rabbitRoot.localPosition = new Vector3(.15f, .78f, -2.4f);
            for (int i = 0; i < family.Length; i++) family[i].gameObject.SetActive(false);
            for (int i = 0; i < wrongPuddles.Length; i++) wrongPuddles[i].gameObject.SetActive(false);
            for (int i = 0; i < wetTicks.Length; i++) wetTicks[i] = false;
            for (int i = 0; i < gridRoots.Length; i++) gridRoots[i].gameObject.SetActive(false);
            rightSnowPile.localScale = new Vector3(4.2f, .35f, 3.0f);
            leftSnowPile.localScale = new Vector3(4.2f, .35f, 3.0f);
            shownScore = shownSecond = shownLives = shownSolved = shownK = -1;
            displayScore = endDisplayScore = 0;
        }

        void ResetWorldForOrder()
        {
            animCorrect = animWrong = false; correctClock = wrongClock = hitStop = 0; activeGridK = 0;
            seesawBeam.localRotation = Quaternion.identity;
            rabbitRoot.localRotation = Quaternion.identity;
            rabbitRoot.localPosition = new Vector3(.15f, .78f, -2.4f);
            for (int i = 0; i < gridRoots.Length; i++) gridRoots[i].gameObject.SetActive(false);
            snowmanRoot.gameObject.SetActive(true);
            snowmanRoot.localScale = Vector3.one * (visualK * .30f);
            rightSnowPile.localScale = new Vector3(4.2f, .35f, 3.0f);
            leftSnowPile.localScale = new Vector3(4.2f, .35f, 3.0f);
            pinKick = 0; handleOvershoot = 0;
            for (int i = 0; i < wrongPuddles.Length; i++) wrongPuddles[i].gameObject.SetActive(i < Mathf.Min(st.wrongMarks, wrongPuddles.Length));
        }

        void RefreshOrderUi()
        {
            if (current == null) return;
            orderTxt.text = (mode == Mode.Practice ? "연습 · 손실 없음" : "의뢰 " + (orderIndex + 1) + " / 7") + "\n기준 " + current.baseHeight + " cm";
            ratioTxt.text = "미니 " + current.targetMini + "개분과 균형";
            conditionTxt.text = "같은 재료·같은 밀도\n양팔 길이 같음";
            evidenceTxt.gameObject.SetActive(current.band <= 2 || mode == Mode.Practice);
            UpdateEvidenceText();
            shownK = -1;
        }

        void UpdateEvidenceText()
        {
            if (current == null || evidenceTxt == null) return;
            if (mode == Mode.Practice || current.band == 1)
                evidenceTxt.text = "바닥 " + st.selectedK + "×" + st.selectedK + "\n높이 " + st.selectedK + "층";
            else if (current.band == 2)
                evidenceTxt.text = "바닥 " + st.selectedK + "×" + st.selectedK + "\n층 수는 닮음비로 판단";
            else evidenceTxt.text = "";
        }

        void UpdateWorld(float dt)
        {
            worldClock += dt;
            MgfLook.FitWidth(cam, 32f, .74f);
            float breathe = Mathf.Sin(worldClock * 1.8f);
            for (int i = 0; i < flags.Length; i++)
                flags[i].localRotation = Quaternion.Euler(0, Mathf.Sin(worldClock * 1.5f + i * .7f) * 8f, Mathf.Sin(worldClock * 2.0f + i) * 4f);
            for (int i = 0; i < smoke.Length; i++)
            {
                float phase = (worldClock * .22f + i * .17f) % 1f;
                smoke[i].localPosition = smokeHome[i] + new Vector3(Mathf.Sin(worldClock + i) * .10f, phase * 1.45f, 0);
                smoke[i].localScale = Vector3.one * Mathf.Lerp(.18f, .52f, phase) * (1f - phase * .25f);
            }
            for (int i = 0; i < lanternGlow.Length; i++)
                lanternGlow[i].localScale = Vector3.one * (.95f + .08f * Mathf.Sin(worldClock * 3f + i));
            earLeft.localRotation = Quaternion.Euler(0, 0, 8f + Mathf.Sin(worldClock * 2.2f) * 5f);
            earRight.localRotation = Quaternion.Euler(0, 0, -8f - Mathf.Sin(worldClock * 2.2f + .6f) * 5f);
            if (!animWrong) rabbitBody.localScale = new Vector3(1f + breathe * .025f, 1f - breathe * .018f, 1f);
            for (int i = 0; i < family.Length; i++) if (family[i].gameObject.activeSelf)
            {
                family[i].localPosition = familyHome[i] + Vector3.up * (Mathf.Sin(worldClock * 2f + i) * .08f);
                family[i].localRotation = Quaternion.Euler(0, Mathf.Sin(worldClock * 1.4f + i) * 5f, Mathf.Sin(worldClock * 2.1f + i) * 2f);
            }

            if (hitStop > 0) hitStop = Mathf.Max(0, hitStop - dt);
            else if (animCorrect) correctClock += dt;
            else if (animWrong) wrongClock += dt;

            float angle = 0;
            if (animCorrect)
            {
                angle = Mathf.Sin(correctClock * 12.4f) * Mathf.Exp(-correctClock * 2.25f) * 9.2f;
                bool showGrid = correctClock > .42f && correctClock < 1.28f;
                snowmanRoot.gameObject.SetActive(!showGrid);
                if (activeGridK >= 2) gridRoots[activeGridK - 2].gameObject.SetActive(showGrid);
                float glow = Mathf.Clamp01(1f - Mathf.Abs(correctClock - 1.05f) * 4f);
                latchRoot.localScale = Vector3.one * (1f + glow * .32f);
                if (st.ordersDone > 0)
                {
                    int index = Mathf.Clamp(st.ordersDone - 1, 0, family.Length - 1);
                    if (correctClock > .95f)
                    {
                        family[index].gameObject.SetActive(true);
                        float grow = Mathf.SmoothStep(0, 1, Mathf.Clamp01((correctClock - .95f) / .55f));
                        family[index].localScale = Vector3.one * (.52f + index * .035f) * grow;
                    }
                }
            }
            else if (animWrong)
            {
                int actual = NuntokkiRules.Cube(revealSelectedK);
                float direction = actual > current.targetMini ? -1f : 1f;
                float settle = 1f - Mathf.Exp(-wrongClock * 5f);
                angle = direction * 12f * settle + Mathf.Sin(wrongClock * 13f) * Mathf.Exp(-wrongClock * 4f) * 3f;
                Transform pile = direction < 0 ? rightSnowPile : leftSnowPile;
                pile.localScale = new Vector3(4.2f, .35f + settle * 1.1f, 3.0f);
                rabbitRoot.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, 172f * -direction, settle));
                rabbitRoot.localPosition = new Vector3(.15f, .78f - settle * .25f, -2.4f);
            }
            seesawBeam.localRotation = Quaternion.Euler(0, 0, angle);

            if (!animCorrect && !animWrong)
            {
                float scale = Mathf.Max(.55f, visualK * .30f);
                float squash = draggingHandle ? Mathf.Sin(worldClock * 11f) * .035f : 0f;
                float over = handleOvershoot > 0 ? Mathf.Sin((1f - handleOvershoot) * Mathf.PI) * .10f : 0f;
                snowmanRoot.localScale = new Vector3(scale * (1f - squash + over), scale * (1f + squash * 1.7f - over * .55f), scale * (1f - squash + over));
                if (handleOvershoot > 0) handleOvershoot = Mathf.Max(0, handleOvershoot - dt * 3.6f);
            }
            pinWorld.localPosition = new Vector3(1.05f, 1.2f - pinPullProgress / 70f, -.22f);
        }

        void UpdateUi(float dt)
        {
            if (titleG.gameObject.activeSelf)
            {
                float p = 1f + Mathf.Sin(worldClock * 1.7f) * .025f + titlePulse * .08f;
                logoTxt.rectTransform.localScale = Vector3.one * p; logoShadowTxt.rectTransform.localScale = Vector3.one * p;
                titlePulse = Mathf.Max(0, titlePulse - dt * 3f);
            }
            if (hudG.gameObject.activeSelf)
            {
                displayScore = Mathf.MoveTowards(displayScore, st.score, dt * Mathf.Max(160f, Mathf.Abs(st.score - displayScore) * 5f));
                int score = Mathf.RoundToInt(displayScore);
                if (shownScore != score) { shownScore = score; scoreTxt.text = score + "점"; MgfFx.Punch(scoreTxt.transform, .08f, .20f); }
                int second = Mathf.Max(0, Mathf.CeilToInt(sessionLeft));
                if (mode == Mode.Practice) second = 140;
                if (shownSecond != second) { shownSecond = second; timeTxt.text = mode == Mode.Practice ? "연습 · 시간 정지" : second + "초"; }
                if (shownLives != st.lives || shownSolved != st.solved)
                {
                    shownLives = st.lives; shownSolved = st.solved;
                    string hearts = st.lives == 3 ? "♥♥♥" : (st.lives == 2 ? "♥♥♡" : (st.lives == 1 ? "♥♡♡" : "♡♡♡"));
                    statusTxt.text = hearts + " · " + st.solved + "/7 · 첫 시도 " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal;
                    MgfFx.Punch(statusTxt.transform, .07f, .22f);
                }
                if (shownK != st.selectedK)
                {
                    shownK = st.selectedK;
                    UpdateEvidenceText();
                    var knobLabel = knobRt.GetComponentInChildren<TextMeshProUGUI>();
                    if (knobLabel) knobLabel.text = st.selectedHeight + " cm";
                }
                float t = Mathf.InverseLerp(NuntokkiRules.MinK, NuntokkiRules.MaxK, visualK);
                Vector2 local;
                var canvasRt = (RectTransform)MgfText.Canvas.transform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, handleScreen, null, out local);
                knobRt.anchorMin = knobRt.anchorMax = new Vector2(.5f, .5f); knobRt.anchoredPosition = local; knobRt.sizeDelta = wideLayout ? new Vector2(92, 58) : new Vector2(78, 58);
                knobRt.localScale = draggingHandle ? new Vector3(1.12f, .88f, 1) : Vector3.one * (1f + handleOvershoot * .08f);
                pinRingImg.rectTransform.anchoredPosition = new Vector2(0, -pinPullProgress * .62f);
                for (int i = 0; i < 7; i++)
                {
                    bool active = i + 2 == st.selectedK;
                    tickMark[i].color = wetTicks[i] ? Wet : (active ? Apricot : Ink);
                    tickTxt[i].gameObject.SetActive(!active);
                    tickTxt[i].color = wetTicks[i] ? Wet : (active ? Plum : Ink);
                    tickTxt[i].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                }
                TrackGuide();
            }
            if (revealG.gameObject.activeSelf)
            {
                float p = 1f + Mathf.Sin(revealClock * 8f) * .06f * Mathf.Exp(-revealClock * 2f);
                latchFlashImg.rectTransform.localScale = Vector3.one * p;
            }
            if (endG.gameObject.activeSelf)
            {
                endDisplayScore = Mathf.MoveTowards(endDisplayScore, st.score, dt * Mathf.Max(180f, st.score * 1.8f));
                endScoreTxt.text = Mathf.RoundToInt(endDisplayScore) + "점";
            }
            if (toastClock > 0)
            {
                toastClock -= dt; toastG.gameObject.SetActive(true); toastG.alpha = Mathf.Clamp01(toastClock * 2f);
                toastRt.localScale = Vector3.one * (1f + Mathf.Sin((1.4f - toastClock) * 7f) * .035f);
            }
            else toastG.gameObject.SetActive(false);
            if (rippleClock > 0)
            {
                rippleClock -= dt; rippleG.gameObject.SetActive(true); rippleG.alpha = Mathf.Clamp01(rippleClock * 2f);
                rippleRt.localScale = Vector3.one * Mathf.Lerp(.45f, 1.45f, 1f - rippleClock / .55f);
            }
            else rippleG.gameObject.SetActive(false);
            guideBoost = Mathf.Max(0, guideBoost - dt);
        }

        void TrackGuide()
        {
            if (mode != Mode.Practice && guideBoost <= 0) { guideRt.gameObject.SetActive(false); for (int i = 0; i < guideDots.Length; i++) guideDots[i].gameObject.SetActive(false); return; }
            bool pointPin = st.handleMoved;
            float pathPhase = (worldClock * .82f) % 1f;
            float easedPath = Mathf.SmoothStep(0f, 1f, pathPhase);
            Vector2 target = pointPin ? Vector2.Lerp(pinScreen + Vector2.up * 12f, pinScreen - Vector2.up * 88f, easedPath) : handleScreen;
            var canvasRt = (RectTransform)MgfText.Canvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, target, null, out var local);
            guideRt.gameObject.SetActive(true); guideRt.anchorMin = guideRt.anchorMax = new Vector2(.5f, .5f); guideRt.anchoredPosition = local;
            float pulse = (pointPin ? 1.20f : 1f) + .12f * Mathf.Sin(worldClock * 5f) + (guideBoost > 1f ? .18f : 0f);
            guideRt.localScale = Vector3.one * pulse; guideRt.SetAsLastSibling();
            guideTxt.text = pointPin ? "↓" : "↕";
            st.guidePhase = pointPin ? "pull_pin_down" : "drag_height_handle";
            Vector2 from = pointPin ? pinScreen + Vector2.up * 12f : handleScreen - Vector2.up * 75f;
            Vector2 to = pointPin ? pinScreen - Vector2.up * 88f : handleScreen + Vector2.up * 85f;
            for (int i = 0; i < guideDots.Length; i++)
            {
                guideDots[i].gameObject.SetActive(true);
                float u = (i + 1f) / (guideDots.Length + 1f);
                float flow = (worldClock * .55f) % 1f;
                u = (u + flow) % 1f;
                Vector2 s = Vector2.Lerp(from, to, u);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, s, null, out var dotLocal);
                guideDots[i].rectTransform.anchorMin = guideDots[i].rectTransform.anchorMax = new Vector2(.5f, .5f);
                guideDots[i].rectTransform.anchoredPosition = dotLocal;
            }
        }

        void BeginHandleDrag() { ShowToast("현재 높이만 보고 원하는 눈금에 놓으세요"); }
        void UpdateHandleDrag(float k) { handleWorld.localPosition = new Vector3(7.05f, Mathf.Lerp(.6f, 5.6f, Mathf.InverseLerp(2, 8, k)), .60f); }
        void EndHandleDrag() { handleOvershoot = 1; MgfSfx.Play("pop", .18f); }
        void SnapHandleVisual(int k, bool moved)
        {
            handleWorld.localPosition = new Vector3(7.05f, Mathf.Lerp(.6f, 5.6f, Mathf.InverseLerp(2, 8, k)), .60f);
            if (moved) { handleOvershoot = 1; MgfFx.Punch(knobRt, .12f, .24f); }
        }
        void BeginPinPull() { pinKick = 1; ShowToast("노란 손길을 따라 아래로 당기세요"); }
        void UpdatePinPull(float amount) { pinKick = amount / 110f; }
        void EndPinPull(bool submitted) { pinKick = submitted ? 1 : 0; }

        void BeginRevealVisual(bool correct, bool practice, int selectedK, int targetK, int targetMini, string misconception)
        {
            animCorrect = correct; animWrong = !correct; correctClock = wrongClock = 0; activeGridK = selectedK;
            toastClock = 0; toastG.gameObject.SetActive(false);
            pinPullProgress = 0; EndPinPull(true);
            int actual = NuntokkiRules.Cube(selectedK);
            if (correct)
            {
                hitStop = .075f;
                revealTitleTxt.text = "수평 걸쇠가 톡 잠겼습니다";
                revealTitleTxt.color = Plum;
                revealFormulaTxt.text = selectedK + "×" + selectedK + "×" + selectedK + "=" + actual;
                revealReasonTxt.text = "닮음비 1:" + selectedK + " → 부피의 비 1:" + actual;
                latchFlashImg.color = Gold;
                MgfSfx.Play("correct", .48f);
                MgfFx.Glow(latchRoot.position, Gold, 14, .42f);
            }
            else
            {
                wetTicks[Mathf.Clamp(selectedK - 2, 0, 6)] = true;
                revealTitleTxt.text = actual > targetMini ? "눈사람이 너무 큽니다" : "눈사람이 너무 작습니다";
                revealTitleTxt.color = Wet;
                revealFormulaTxt.text = "내 눈사람 " + actual + "개분\n목표 " + targetMini + "개분";
                revealReasonTxt.text = MisconceptionMessage(misconception, selectedK, targetK);
                latchFlashImg.color = Periwinkle;
                MgfSfx.Play("wrong", .42f);
                MgfFx.Shake(cam, .07f, .20f);
            }
            SetScreen();
        }

        string MisconceptionMessage(string id, int selectedK, int targetK)
        {
            if (id == "square_for_volume") return selectedK + "×" + selectedK + "은 넓이의 비입니다. 부피의 비는 k³입니다.";
            if (id == "linear_volume_as_length") return "부피의 비를 높이에 그대로 쓰지 말고 k³을 확인하세요.";
            if (id == "additive_growth") return "높이의 차가 아니라 닮음비를 세 번 곱합니다.";
            return selectedK < targetK ? "선택한 닮음비를 세 번 곱한 값이 목표보다 작습니다." : "선택한 닮음비를 세 번 곱한 값이 목표보다 큽니다.";
        }

        void CompleteRevealVisual()
        {
            animCorrect = animWrong = false;
            snowmanRoot.gameObject.SetActive(true);
            for (int i = 0; i < gridRoots.Length; i++) gridRoots[i].gameObject.SetActive(false);
            seesawBeam.localRotation = Quaternion.identity;
            latchRoot.localScale = Vector3.one;
            rabbitRoot.localRotation = Quaternion.identity;
            rabbitRoot.localPosition = new Vector3(.15f, .78f, -2.4f);
        }

        void ShowEndVisual(string reason)
        {
            bool clear = reason == "clear", mastery = reason == "not_mastered";
            endTitleTxt.text = clear ? "눈축제 광장 완성" : (mastery ? "완성했지만 숙련은 다음 판" : "햇살 하트가 모두 녹았습니다");
            endTitleTxt.color = clear ? Plum : Wet;
            endDisplayScore = 0;
            endStatsTxt.text = "첫 시도 정답 " + st.firstAttemptCorrect + " / " + st.firstAttemptTotal + " · 완성 눈사람 " + st.solved + " / 7\n" +
                (clear ? "5/7 이상 · 닮음비의 세제곱으로 균형을 잡았습니다" :
                 mastery ? "완주는 성공 · 축제 완성은 첫 시도 5/7 이상입니다" : "하트 3개 안에 일곱 의뢰를 완성하세요");
            endTapTxt.text = "한 번 눌러 새 축제 시작";
        }

        void PulseTitle()
        {
            titlePulse = 1;
            MgfSfx.Play("pop", .28f);
        }

        void BoostGuide()
        {
            guideBoost = 2.1f;
            if (guideRt) MgfFx.Punch(guideRt, .18f, .34f);
        }

        void ReplayPinGuide()
        {
            guideBoost = 3.6f;
            if (guideRt) MgfFx.Punch(guideRt, .24f, .42f);
        }

        void RefusePin(string message)
        {
            pinKick = 1; draggingPin = false; st.pinPulling = false; pinPullProgress = 0;
            ShowToast(message); BoostGuide(); MgfSfx.Play("wrong", .12f); MgfFx.Punch(pinRt, .10f, .24f);
        }

        void RefuseHandle(string message)
        {
            ShowToast(message); BoostGuide(); MgfSfx.Play("wrong", .10f); MgfFx.Punch(knobRt, .10f, .24f);
        }

        void GuideInvalidInput(Vector2 screen)
        {
            RippleAt(screen, st.handleMoved ? "고정핀 줄을 아래로 당기세요" : "오른쪽 높이 손잡이를 움직이세요");
            BoostGuide();
        }

        void ShowToast(string text)
        {
            toastTxt.text = text; toastClock = 1.4f; toastG.gameObject.SetActive(true); toastG.alpha = 1; toastRt.localScale = Vector3.one * .94f;
        }

        void RippleAt(Vector2 screen, string text)
        {
            rippleTxt.text = text; rippleClock = .55f; rippleG.gameObject.SetActive(true); rippleG.alpha = 1;
            var canvasRt = (RectTransform)MgfText.Canvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var local);
            rippleRt.anchorMin = rippleRt.anchorMax = new Vector2(.5f, .5f); rippleRt.anchoredPosition = local; rippleRt.localScale = Vector3.one * .5f;
        }
    }
}
