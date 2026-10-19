// 불꽃 점검 — 분홍 구름 옥상 2.5D 월드, 명단 고리 UI, 참새·발사 연출.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.BulkkotJeomgeom
{
    public partial class BulkkotJeomgeomGame
    {
        static readonly Color RoseSky = MgfLook.Hex("#E8B6D1");
        static readonly Color RocketRed = MgfLook.Hex("#D94345");
        static readonly Color RoofGray = MgfLook.Hex("#77705F");
        static readonly Color Paper = MgfLook.Hex("#FFF0CA");
        static readonly Color Soot = MgfLook.Hex("#262124");
        static readonly Color Peach = MgfLook.Hex("#F4A7A0");
        static readonly Color Brass = MgfLook.Hex("#D5A43D");
        static readonly Color Mint = MgfLook.Hex("#72C7AA");

        Camera cam;
        Transform worldRoot, cloudRoot, roofRoot, birdRoot, birdWingL, birdWingR, birdHead, pinwheel;
        readonly Transform[] clouds = new Transform[9];
        readonly Transform[] laundry = new Transform[8];
        readonly Transform[] worldRockets = new Transform[12];
        readonly Transform[] trails = new Transform[12];
        readonly Transform[] bloomRoots = new Transform[12];
        Vector3 birdHome, cameraHome;
        Material roofMat, redMat, paperMat, sootMat, brassMat, cloudMat, mintMat, glowMat;
        float worldClock, launchClock, wrongShake, birdHop, titlePress;
        bool worldLaunch, worldWrong;

        CanvasGroup titleG, hudG, rosterG, revealG, endG, toastG;
        RectTransform rootRt, titleBannerRt, startSealRt, titleBirdBadgeRt, hudRt, conditionRt, binsRt, rosterRt, fusePanelRt, fuseHandleRt;
        RectTransform guideRingRt, guidePathRt, rippleRt, revealRt, endRt, toastRt;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleBest, startTxt;
        TextMeshProUGUI hudProgress, hudLives, hudTime, hudFirst, goalTxt, conditionTxt, binATxt, binBTxt;
        TextMeshProUGUI fuseTxt, guideTxt, revealTitle, revealFormula, endTitle, endScore, endStats, endTap, toastTxt;
        Image fuseCordImg, fuseHandleImg, guideRingImg, guidePathImg, rippleImg, toastBg;
        readonly RectTransform[] cardRt = new RectTransform[12];
        readonly RectTransform[,] ringRt = new RectTransform[12, 2];
        readonly Image[] cardBg = new Image[12];
        readonly Image[,] ringBg = new Image[12, 2];
        readonly TextMeshProUGUI[,] ringTxt = new TextMeshProUGUI[12, 2];
        readonly TextMeshProUGUI[] cardNum = new TextMeshProUGUI[12];
        readonly float[] cardPulse = new float[12];
        Sprite roundSprite, ringSprite, thinSprite, circleSprite;
        int layoutW = -1, layoutH = -1, shownSecond = -1, shownScore = -1;
        float displayedScore, endDisplayedScore, guideClock, guideBoost, rippleClock, toastClock, revealUiClock;
        bool landscape, guideFuse;

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
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 2.0f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>(); t.font = MgfText.Font; t.fontSize = size; t.color = color;
            t.alignment = align; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text;
            return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = ring ? "FireRing" : "FirePaper", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                if (ring)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - size * .39f) / (size * .07f));
                }
                else
                {
                    float qx = Mathf.Max(Mathf.Abs(fx - size * .5f) - (size * .5f - radius), 0);
                    float qy = Mathf.Max(Mathf.Abs(fy - size * .5f) - (size * .5f - radius), 0);
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(qx * qx + qy * qy) + .8f);
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            float b = ring ? 0 : radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        static Sprite MakeCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "RoundPaperSeal", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = (x + .5f) / size * 2f - 1f, dy = (y + .5f) / size * 2f - 1f;
                float a = Mathf.Clamp01((1f - Mathf.Sqrt(dx * dx + dy * dy)) * size * .7f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100);
        }

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("#D896BD"), RoseSky, MgfLook.Hex("#F7C9B4"), .86f);
            MgfLook.Sun(new Vector3(46, -38, 18), MgfLook.Hex("#FFE8C1"), 1.18f, .62f);
            cam = MgfLook.Camera(new Vector3(10.2f, 10.5f, -14.2f), new Vector3(0, .8f, 2.2f), 34f);
            cam.orthographic = true; cam.orthographicSize = 8.6f; cameraHome = cam.transform.position;
            roofMat = MgfLook.Lit(RoofGray, .16f, .03f);
            redMat = MgfLook.Lit(RocketRed, .22f, .01f);
            paperMat = MgfLook.Lit(Paper, .18f, .01f);
            sootMat = MgfLook.Lit(Soot, .09f, .02f);
            brassMat = MgfLook.Lit(Brass, .55f, .36f);
            cloudMat = MgfLook.Lit(MgfLook.Hex("#F5D5E1"), .04f, 0f);
            mintMat = MgfLook.Lit(Mint, .16f, .01f);
            glowMat = MgfLook.Lit(MgfLook.Hex("#FFD66B"), .55f, .02f, MgfLook.Hex("#F29F3D") * .7f);

            worldRoot = new GameObject("RoseRooftopCity").transform;
            cloudRoot = new GameObject("RoundedCloudField").transform; cloudRoot.SetParent(worldRoot, false);
            roofRoot = new GameObject("DenseToyRooftops").transform; roofRoot.SetParent(worldRoot, false);
            BuildClouds(); BuildRooftops(); BuildLaunchers(); BuildSootSparrow(); BuildPinwheelAndLaundry();
        }

        void BuildClouds()
        {
            for (int i = 0; i < clouds.Length; i++)
            {
                float x = -8.5f + (i % 5) * 4.2f, y = 3.8f + (i % 3) * .55f, z = 7.5f + (i / 5) * 3f;
                var root = new GameObject("RoseCloud" + i).transform; root.SetParent(cloudRoot, false); root.localPosition = new Vector3(x, y, z);
                clouds[i] = root;
                for (int k = 0; k < 4; k++)
                    MgfLook.Prim(PrimitiveType.Sphere, "CloudPuff", new Vector3((k - 1.5f) * .65f, (k & 1) * .25f, 0),
                        new Vector3(1.2f + (k % 2) * .25f, .65f, .75f), cloudMat, root, false);
            }
        }

        void BuildRooftops()
        {
            MgfLook.Block("CityBase", new Vector3(0, -1.8f, 3.3f), new Vector3(19, 1.2f, 17), .65f, roofMat, roofRoot);
            for (int i = 0; i < 14; i++)
            {
                float x = -7.5f + (i % 7) * 2.45f, z = -1.4f + (i / 7) * 5.2f + (i % 2) * .5f;
                float h = 1.15f + (i % 4) * .38f;
                var mat = i % 3 == 0 ? paperMat : i % 3 == 1 ? roofMat : redMat;
                MgfLook.Block("ToyRoof" + i, new Vector3(x, -1.0f + h * .5f, z), new Vector3(2.05f, h, 2.0f), .32f, mat, roofRoot);
                var cap = MgfLook.Prim(PrimitiveType.Cylinder, "CeramicChimney" + i, new Vector3(x + .45f, h - .55f, z + .25f),
                    new Vector3(.46f, .72f, .46f), i % 2 == 0 ? paperMat : roofMat, roofRoot, false);
                cap.transform.localRotation = Quaternion.Euler(0, 22.5f, 0);
            }
            MgfLook.Block("BannerChimney", new Vector3(0, .3f, 4.8f), new Vector3(5.8f, 2.0f, 2.3f), .55f, roofMat, roofRoot);
            MgfLook.Block("CurvedBanner", new Vector3(0, 1.35f, 3.55f), new Vector3(5.2f, 1.15f, .18f), .42f, redMat, roofRoot);
        }

        void BuildLaunchers()
        {
            for (int i = 0; i < 12; i++)
            {
                var root = new GameObject("PaperFirework" + i).transform; root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(-5.4f + (i % 6) * 2.15f, .35f, -.4f + (i / 6) * 3.1f);
                worldRockets[i] = root;
                MgfLook.Prim(PrimitiveType.Cylinder, "Sleeve", Vector3.zero, new Vector3(.46f, 1.15f, .46f), redMat, root, false);
                var nose = MgfLook.Prim(PrimitiveType.Cylinder, "FoldedCap", new Vector3(0, .86f, 0), new Vector3(.58f, .42f, .58f), paperMat, root, false);
                nose.transform.localRotation = Quaternion.Euler(0, 22.5f, 0);
                MgfLook.Prim(PrimitiveType.Cylinder, "UpperBand", new Vector3(0, .27f, 0), new Vector3(.54f, .18f, .54f), brassMat, root, false);
                MgfLook.Prim(PrimitiveType.Cylinder, "LowerBand", new Vector3(0, -.27f, 0), new Vector3(.54f, .18f, .54f), paperMat, root, false);
                trails[i] = MgfLook.Prim(PrimitiveType.Cube, "PaperTrail", new Vector3(0, -1.0f, 0), new Vector3(.09f, .9f, .09f), glowMat, root, false).transform;
                trails[i].gameObject.SetActive(false);
                var bloom = new GameObject("CaseBloom" + i).transform; bloom.SetParent(worldRoot, false); bloomRoots[i] = bloom;
                bloom.localPosition = new Vector3(-4.8f + (i % 6) * 1.9f, 5.0f + (i % 3) * .55f, 2.4f + (i / 6));
                for (int k = 0; k < 6; k++)
                {
                    float a = k * Mathf.PI * 2f / 6f;
                    var petal = MgfLook.Block("PaperPetal", new Vector3(Mathf.Cos(a) * .64f, Mathf.Sin(a) * .64f, 0),
                        new Vector3(.56f, .18f, .12f), .06f, i % 2 == 0 ? paperMat : redMat, bloom);
                    Destroy(petal.GetComponent<Collider>()); petal.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                }
                bloom.gameObject.SetActive(false);
            }
        }

        void BuildSootSparrow()
        {
            birdRoot = new GameObject("SutkongSootSparrow").transform; birdRoot.SetParent(worldRoot, false); birdRoot.localPosition = new Vector3(4.8f, .45f, 1.0f); birdHome = birdRoot.localPosition;
            MgfLook.Prim(PrimitiveType.Sphere, "RoundBody", new Vector3(0, .75f, 0), new Vector3(1.4f, 1.55f, 1.12f), sootMat, birdRoot, false);
            birdHead = MgfLook.Prim(PrimitiveType.Sphere, "RoundHead", new Vector3(0, 1.75f, -.08f), new Vector3(1.18f, 1.08f, 1.0f), sootMat, birdRoot, false).transform;
            MgfLook.Prim(PrimitiveType.Sphere, "IvoryHood", new Vector3(0, 1.98f, .12f), new Vector3(1.30f, .62f, 1.10f), paperMat, birdRoot, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "ShortBeak", new Vector3(0, 1.66f, -.64f), new Vector3(.20f, .22f, .20f), brassMat, birdRoot, false).transform.localRotation = Quaternion.Euler(90, 0, 0);
            for (int s = -1; s <= 1; s += 2)
            {
                MgfLook.Prim(PrimitiveType.Sphere, "Eye" + s, new Vector3(s * .29f, 1.86f, -.54f), new Vector3(.11f, .14f, .09f), paperMat, birdRoot, false);
                var wing = MgfLook.Prim(PrimitiveType.Sphere, "Wing" + s, new Vector3(s * .73f, .92f, 0), new Vector3(.42f, .88f, .34f), sootMat, birdRoot, false).transform;
                wing.localRotation = Quaternion.Euler(0, 0, s * -26f); if (s < 0) birdWingL = wing; else birdWingR = wing;
                MgfLook.Prim(PrimitiveType.Capsule, "Foot" + s, new Vector3(s * .32f, -.08f, 0), new Vector3(.10f, .34f, .10f), brassMat, birdRoot, false);
            }
        }

        void BuildPinwheelAndLaundry()
        {
            pinwheel = new GameObject("RooftopPinwheel").transform; pinwheel.SetParent(worldRoot, false); pinwheel.localPosition = new Vector3(-5.9f, 2.0f, 3.1f);
            MgfLook.Block("PinPost", new Vector3(0, -1.0f, 0), new Vector3(.10f, 2.1f, .10f), .03f, roofMat, pinwheel);
            for (int i = 0; i < 4; i++)
            {
                var blade = MgfLook.Block("FoldedBlade", new Vector3(.55f, 0, 0), new Vector3(1.05f, .34f, .12f), .08f, i % 2 == 0 ? redMat : paperMat, pinwheel);
                Destroy(blade.GetComponent<Collider>()); blade.transform.localRotation = Quaternion.Euler(0, 0, i * 90f);
            }
            for (int i = 0; i < laundry.Length; i++)
            {
                float x = -3.7f + i * 1.05f;
                laundry[i] = MgfLook.Block("TinyLaundry" + i, new Vector3(x, 2.1f + (i % 2) * .12f, 6.1f), new Vector3(.75f, .62f, .08f), .08f,
                    i % 2 == 0 ? paperMat : mintMat, worldRoot).transform;
                Destroy(laundry[i].GetComponent<Collider>());
            }
        }

        void BuildUi()
        {
            roundSprite = MakeSprite(64, 14, false); thinSprite = MakeSprite(32, 7, false); ringSprite = MakeSprite(64, 8, true); circleSprite = MakeCircleSprite(64);
            var canvas = MgfText.Canvas;
            rootRt = R("BulkkotUiRoot", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one; rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;
            titleG = Group("TitleLayer", rootRt); hudG = Group("HudLayer", rootRt); rosterG = Group("RosterLayer", rootRt);
            revealG = Group("RevealLayer", rootRt); endG = Group("EndLayer", rootRt); toastG = Group("ToastLayer", rootRt);

            BuildTitleUi(); BuildHudUi(); BuildRosterUi(); BuildRevealUi(); BuildEndUi(); BuildTransientUi();
            ShowPhaseUi();
        }

        CanvasGroup Group(string name, Transform parent)
        {
            var rt = R(name, parent, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt.gameObject.AddComponent<CanvasGroup>();
        }

        void BuildTitleUi()
        {
            titleBannerRt = Img(titleG.transform, "ChimneyBannerUi", new Vector2(.5f, .72f), Vector2.zero, new Vector2(340, 150), new Color(RocketRed.r, RocketRed.g, RocketRed.b, .95f), roundSprite).rectTransform;
            titleLogo = Txt(titleBannerRt, "불꽃 점검", new Vector2(.5f, .62f), Vector2.zero, 48, Paper, 320);
            titleLogo.fontStyle = FontStyles.Bold; titleLogo.outlineWidth = .22f; titleLogo.outlineColor = Soot;
            titleTag = Txt(titleBannerRt, "중복 고리를 돌려 빠진 경우를 채워라", new Vector2(.5f, .22f), Vector2.zero, 17, Paper, 310);
            titleMeta = Txt(titleG.transform, "중학교 2학년 · 경우의 수", new Vector2(.5f, .55f), Vector2.zero, 16, Soot, 320);
            titleBirdBadgeRt = Img(titleG.transform, "SparrowBadge", new Vector2(.5f, .43f), new Vector2(112, 0), new Vector2(88, 58), new Color(Paper.r, Paper.g, Paper.b, .88f), roundSprite).rectTransform;
            Txt(titleBirdBadgeRt, "숯콩 점검대", new Vector2(.5f, .5f), Vector2.zero, 14, Soot, 84);
            startSealRt = Img(titleG.transform, "TearSeal", new Vector2(.5f, .30f), Vector2.zero, new Vector2(136, 136), Paper, ringSprite).rectTransform;
            var disk = Img(startSealRt, "SealPaper", new Vector2(.5f, .5f), Vector2.zero, new Vector2(112, 112), RocketRed, circleSprite);
            startTxt = Txt(disk.transform, "점검 시작", new Vector2(.5f, .5f), Vector2.zero, 20, Paper, 102); startTxt.fontStyle = FontStyles.Bold;
            titleBest = Txt(titleG.transform, "6판 · 105초 · 첫 제출 5판 이상", new Vector2(.5f, .17f), Vector2.zero, 14, Soot, 340);
        }

        void BuildHudUi()
        {
            hudRt = Img(hudG.transform, "PaperRopeHud", new Vector2(.5f, .96f), Vector2.zero, new Vector2(370, 58), new Color(Paper.r, Paper.g, Paper.b, .94f), roundSprite).rectTransform;
            hudProgress = Txt(hudRt, "1/6", new Vector2(.13f, .5f), Vector2.zero, 16, Soot, 84);
            hudFirst = Txt(hudRt, "첫 제출 0/0", new Vector2(.43f, .5f), Vector2.zero, 15, Soot, 150);
            hudLives = Txt(hudRt, "안전등 ●●●", new Vector2(.74f, .5f), Vector2.zero, 15, RocketRed, 130);
            hudTime = Txt(hudRt, "105초", new Vector2(.93f, .5f), Vector2.zero, 15, Soot, 72);
            goalTxt = Txt(hudG.transform, "빠진 경우를 채우고 도화선을 당기시오", new Vector2(.5f, .885f), Vector2.zero, 18, Soot, 370);
            goalTxt.fontStyle = FontStyles.Bold;
            conditionRt = Img(hudG.transform, "ConditionPaper", new Vector2(.5f, .80f), Vector2.zero, new Vector2(370, 82), new Color(Paper.r, Paper.g, Paper.b, .94f), roundSprite).rectTransform;
            conditionTxt = Txt(conditionRt, "", new Vector2(.5f, .58f), Vector2.zero, 14, Soot, 350);
            binsRt = Img(hudG.transform, "PartsRack", new Vector2(.5f, .72f), Vector2.zero, new Vector2(370, 60), new Color(Peach.r, Peach.g, Peach.b, .93f), roundSprite).rectTransform;
            binATxt = Txt(binsRt, "", new Vector2(.25f, .5f), Vector2.zero, 13, Soot, 175);
            binBTxt = Txt(binsRt, "", new Vector2(.75f, .5f), Vector2.zero, 13, Soot, 175);
        }

        void BuildRosterUi()
        {
            rosterRt = R("FireworkRoster", rosterG.transform, new Vector2(.5f, .43f), Vector2.zero, new Vector2(370, 380));
            for (int i = 0; i < cardRt.Length; i++)
            {
                cardBg[i] = Img(rosterRt, "ShellCard" + i, new Vector2(.5f, .5f), Vector2.zero, new Vector2(108, 82), new Color(RoofGray.r, RoofGray.g, RoofGray.b, .94f), roundSprite);
                cardRt[i] = cardBg[i].rectTransform;
                cardNum[i] = Txt(cardRt[i], (i + 1).ToString("00"), new Vector2(.12f, .5f), Vector2.zero, 11, Paper, 24);
                for (int r = 0; r < 2; r++)
                {
                    ringBg[i, r] = Img(cardRt[i], "Ring" + r, new Vector2(.58f, r == 0 ? .70f : .30f), Vector2.zero, new Vector2(82, 32),
                        r == 0 ? RocketRed : Paper, thinSprite);
                    ringRt[i, r] = ringBg[i, r].rectTransform;
                    ringTxt[i, r] = Txt(ringRt[i, r], "", new Vector2(.5f, .5f), Vector2.zero, 13, r == 0 ? Paper : Soot, 78);
                    ringTxt[i, r].fontStyle = FontStyles.Bold;
                }
                cardRt[i].gameObject.SetActive(false);
            }
            fusePanelRt = Img(rosterG.transform, "FuseBoard", new Vector2(.5f, .105f), Vector2.zero, new Vector2(250, 116), new Color(Paper.r, Paper.g, Paper.b, .96f), roundSprite).rectTransform;
            fuseTxt = Txt(fusePanelRt, "명단 점검 완료", new Vector2(.38f, .72f), Vector2.zero, 15, Soot, 176);
            fuseCordImg = Img(fusePanelRt, "PaleFuseCord", new Vector2(.38f, .35f), Vector2.zero, new Vector2(150, 10), MgfLook.Hex("#E2BC58"), thinSprite);
            fuseHandleImg = Img(fusePanelRt, "FuseHandle", new Vector2(.82f, .50f), Vector2.zero, new Vector2(62, 74), RocketRed, roundSprite);
            fuseHandleRt = fuseHandleImg.rectTransform; fuseTxt = Txt(fuseHandleRt, "당겨\n발사", new Vector2(.5f, .5f), Vector2.zero, 14, Paper, 58);

            guideRingImg = Img(rosterG.transform, "GuideRing", new Vector2(.5f, .5f), Vector2.zero, new Vector2(100, 54), new Color(Paper.r, Paper.g, Paper.b, .9f), ringSprite);
            guideRingRt = guideRingImg.rectTransform;
            guidePathImg = Img(rosterG.transform, "GuidePath", new Vector2(.5f, .5f), Vector2.zero, new Vector2(118, 28), new Color(Paper.r, Paper.g, Paper.b, .85f), thinSprite);
            guidePathRt = guidePathImg.rectTransform;
            guideTxt = Txt(guidePathRt, "←  밀기  →", new Vector2(.5f, .5f), Vector2.zero, 14, Soot, 114);
            guideRingRt.gameObject.SetActive(false); guidePathRt.gameObject.SetActive(false);
        }

        void BuildRevealUi()
        {
            revealRt = Img(revealG.transform, "RevealPaper", new Vector2(.5f, .50f), Vector2.zero, new Vector2(350, 190), new Color(Paper.r, Paper.g, Paper.b, .97f), roundSprite).rectTransform;
            revealTitle = Txt(revealRt, "", new Vector2(.5f, .68f), Vector2.zero, 28, Soot, 330); revealTitle.fontStyle = FontStyles.Bold;
            revealFormula = Txt(revealRt, "", new Vector2(.5f, .34f), Vector2.zero, 17, Soot, 322);
        }

        void BuildEndUi()
        {
            endRt = Img(endG.transform, "EndBanner", new Vector2(.5f, .51f), Vector2.zero, new Vector2(360, 360), new Color(Paper.r, Paper.g, Paper.b, .97f), roundSprite).rectTransform;
            endTitle = Txt(endRt, "", new Vector2(.5f, .77f), Vector2.zero, 38, RocketRed, 340); endTitle.fontStyle = FontStyles.Bold;
            endScore = Txt(endRt, "0점", new Vector2(.5f, .56f), Vector2.zero, 42, Soot, 330); endScore.fontStyle = FontStyles.Bold;
            endStats = Txt(endRt, "", new Vector2(.5f, .35f), Vector2.zero, 18, Soot, 330);
            endTap = Txt(endRt, "종이 봉인을 눌러 다시 점검", new Vector2(.5f, .12f), Vector2.zero, 17, RocketRed, 330);
        }

        void BuildTransientUi()
        {
            toastBg = Img(toastG.transform, "Toast", new Vector2(.5f, .63f), Vector2.zero, new Vector2(350, 74), new Color(Soot.r, Soot.g, Soot.b, .92f), roundSprite);
            toastRt = toastBg.rectTransform; toastTxt = Txt(toastRt, "", new Vector2(.5f, .5f), Vector2.zero, 15, Paper, 332);
            toastG.alpha = 0;
            rippleImg = Img(toastG.transform, "TapRipple", new Vector2(.5f, .5f), Vector2.zero, new Vector2(70, 70), new Color(Paper.r, Paper.g, Paper.b, .78f), ringSprite);
            rippleRt = rippleImg.rectTransform; rippleRt.gameObject.SetActive(false);
        }

        void ShowPhaseUi()
        {
            titleG.alpha = phase == Phase.Title ? 1 : 0; titleG.blocksRaycasts = phase == Phase.Title;
            bool play = phase == Phase.Practice || phase == Phase.Playing || phase == Phase.Reveal;
            hudG.alpha = play ? 1 : 0; rosterG.alpha = play ? 1 : 0;
            revealG.alpha = phase == Phase.Reveal ? 1 : 0;
            endG.alpha = phase == Phase.End ? 1 : 0;
            if (play && current != null) RefreshProblemUi();
            if (phase != Phase.Reveal) revealG.blocksRaycasts = false;
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            hudProgress.text = phase == Phase.Practice ? "연습" : (roundIndex + 1) + "/6";
            hudFirst.text = phase == Phase.Practice ? "시간 정지" : "첫 제출 " + st.firstCorrect + "/" + st.firstAttempts;
            hudLives.text = "안전등 " + new string('●', st.lives) + new string('○', BulkkotRules.StartLives - st.lives);
            goalTxt.text = phase == Phase.Practice ? "같은 별 하나를 원으로 바꾸고 도화선을 당기시오" : "빠진 경우를 채우고 도화선을 당기시오";
            conditionTxt.text = current.product ? "모양과 꼬리에서 각각 하나씩 · 모든 순서쌍을 한 번씩" : "A통 또는 B통 중 한 통에서 하나 · 같은 경우는 한 번만";
            binATxt.text = current.product ? "모양  " + Names(current.aItems, BulkkotRules.Shapes) : "A통  " + Names(current.aItems, BulkkotRules.Shapes);
            binBTxt.text = current.product ? "꼬리  " + Names(current.bItems, BulkkotRules.Tails) : "B통  " + Names(current.bItems, BulkkotRules.Tails);
            RefreshAllCards();
        }

        static string Names(int[] items, string[] labels)
        {
            if (items == null) return ""; string s = "";
            for (int i = 0; i < items.Length; i++) s += (i == 0 ? "" : "·") + labels[items[i]];
            return s;
        }

        void PrepareRosterVisual(bool practice)
        {
            revealUiClock = 0f;
            for (int i = 0; i < cardRt.Length; i++) cardPulse[i] = i < roster.Count ? -.07f * i : 0f;
            RefreshAllCards(); LayoutUi(true);
            for (int i = 0; i < worldRockets.Length; i++)
            {
                worldRockets[i].gameObject.SetActive(i < roster.Count); worldRockets[i].localPosition = new Vector3(-5.4f + (i % 6) * 2.15f, .35f, -.4f + (i / 6) * 3.1f);
                trails[i].gameObject.SetActive(false); bloomRoots[i].gameObject.SetActive(false);
            }
            if (practice) ShowRingGuide(1, 1, false); else { guideRingRt.gameObject.SetActive(false); guidePathRt.gameObject.SetActive(false); }
        }

        void RefreshAllCards()
        {
            for (int i = 0; i < cardRt.Length; i++)
            {
                cardRt[i].gameObject.SetActive(i < roster.Count);
                if (i < roster.Count) RefreshCard(i, false);
            }
            LayoutUi(false);
        }

        void RefreshCard(int i, bool punch)
        {
            if (current == null || i < 0 || i >= roster.Count) return;
            bool product; int a, b; BulkkotRules.TryDecode(roster[i], out product, out a, out b);
            if (current.product)
            {
                ringTxt[i, 0].text = "모양 · " + BulkkotRules.Shapes[a];
                ringTxt[i, 1].text = "꼬리 · " + BulkkotRules.Tails[b];
            }
            else
            {
                ringTxt[i, 0].text = a == 0 ? "A통" : "B통";
                ringTxt[i, 1].text = a == 0 ? BulkkotRules.Shapes[b] : BulkkotRules.Tails[b];
            }
            if (punch) cardPulse[i] = .28f;
        }

        void LayoutUi(bool force)
        {
            if (!rootRt || current == null) return;
            float w = rootRt.rect.width, h = rootRt.rect.height; landscape = w / Mathf.Max(1f, h) >= 1.15f;
            if (!force && layoutW == Screen.width && layoutH == Screen.height) return;
            layoutW = Screen.width; layoutH = Screen.height;
            if (!landscape)
            {
                hudRt.anchorMin = hudRt.anchorMax = new Vector2(.5f, .96f); hudRt.sizeDelta = new Vector2(Mathf.Min(370, w - 16), 58);
                goalTxt.rectTransform.anchorMin = goalTxt.rectTransform.anchorMax = new Vector2(.5f, .885f);
                conditionRt.anchorMin = conditionRt.anchorMax = new Vector2(.5f, .80f); conditionRt.sizeDelta = new Vector2(Mathf.Min(370, w - 16), 82);
                binsRt.anchorMin = binsRt.anchorMax = new Vector2(.5f, .715f); binsRt.sizeDelta = new Vector2(Mathf.Min(370, w - 16), 58);
                rosterRt.anchorMin = rosterRt.anchorMax = new Vector2(.5f, .425f); rosterRt.sizeDelta = new Vector2(Mathf.Min(374, w - 10), h * .43f);
                fusePanelRt.anchorMin = fusePanelRt.anchorMax = new Vector2(.5f, .105f); fusePanelRt.sizeDelta = new Vector2(250, 110);
                int cols = 3, rows = Mathf.CeilToInt(roster.Count / 3f); float cw = Mathf.Min(112, (rosterRt.rect.width - 12) / cols), ch = Mathf.Min(86, (rosterRt.rect.height - 8) / Mathf.Max(1, rows));
                for (int i = 0; i < roster.Count; i++)
                {
                    int row = i / cols, col = i % cols;
                    cardRt[i].sizeDelta = new Vector2(cw - 5, ch - 4);
                    cardRt[i].anchoredPosition = new Vector2((col - 1) * cw, (rows - 1) * ch * .5f - row * ch);
                    for (int r = 0; r < 2; r++) ringRt[i, r].sizeDelta = new Vector2(cw - 30, Mathf.Max(30, ch * .39f));
                }
            }
            else
            {
                hudRt.anchorMin = hudRt.anchorMax = new Vector2(.78f, .93f); hudRt.sizeDelta = new Vector2(w * .40f, 54);
                goalTxt.rectTransform.anchorMin = goalTxt.rectTransform.anchorMax = new Vector2(.80f, .82f); goalTxt.rectTransform.sizeDelta = new Vector2(w * .35f, 54);
                conditionRt.anchorMin = conditionRt.anchorMax = new Vector2(.81f, .68f); conditionRt.sizeDelta = new Vector2(w * .34f, 96);
                binsRt.anchorMin = binsRt.anchorMax = new Vector2(.81f, .53f); binsRt.sizeDelta = new Vector2(w * .34f, 72);
                rosterRt.anchorMin = rosterRt.anchorMax = new Vector2(.34f, .49f); rosterRt.sizeDelta = new Vector2(w * .64f, h * .82f);
                fusePanelRt.anchorMin = fusePanelRt.anchorMax = new Vector2(.81f, .30f); fusePanelRt.sizeDelta = new Vector2(w * .30f, 128);
                int cols = 4, rows = Mathf.CeilToInt(roster.Count / 4f); float cw = Mathf.Min(160, (rosterRt.rect.width - 16) / cols), ch = Mathf.Min(126, (rosterRt.rect.height - 12) / Mathf.Max(1, rows));
                for (int i = 0; i < roster.Count; i++)
                {
                    int row = i / cols, col = i % cols;
                    cardRt[i].sizeDelta = new Vector2(cw - 8, ch - 8);
                    cardRt[i].anchoredPosition = new Vector2((col - 1.5f) * cw, (rows - 1) * ch * .5f - row * ch);
                    for (int r = 0; r < 2; r++) ringRt[i, r].sizeDelta = new Vector2(cw - 38, Mathf.Max(38, ch * .38f));
                }
            }
            if (phase == Phase.Practice) ShowRingGuide(1, 1, false);
        }

        void UpdateUi(float dt)
        {
            if (layoutW != Screen.width || layoutH != Screen.height) LayoutUi(true);
            int sec = Mathf.CeilToInt(timeLeft);
            if (sec != shownSecond && hudTime) { shownSecond = sec; hudTime.text = phase == Phase.Practice ? "시간 정지" : sec + "초"; }
            displayedScore = Mathf.MoveTowards(displayedScore, st.score, dt * 700f);
            if (shownScore != Mathf.RoundToInt(displayedScore)) shownScore = Mathf.RoundToInt(displayedScore);
            for (int i = 0; i < roster.Count; i++)
            {
                cardPulse[i] += dt;
                float reveal = Mathf.Clamp01((cardPulse[i] + .22f) / .22f);
                float p = reveal < 1 ? 1f - Mathf.Pow(1f - reveal, 3f) : 1f;
                float bump = cardPulse[i] > 0f && cardPulse[i] < .28f ? Mathf.Sin(cardPulse[i] / .28f * Mathf.PI) * .10f : 0f;
                cardRt[i].localScale = Vector3.one * ((.88f + .12f * p) + bump);
                cardBg[i].color = Color.Lerp(new Color(RoofGray.r, RoofGray.g, RoofGray.b, .72f), new Color(RoofGray.r, RoofGray.g, RoofGray.b, .96f), p);
            }
            if (guideRingRt.gameObject.activeSelf)
            {
                guideClock += dt * (guideBoost > 0 ? 2.0f : 1f); guideBoost = Mathf.Max(0, guideBoost - dt);
                float pulse = .5f + .5f * Mathf.Sin(guideClock * 5.2f);
                guideRingRt.localScale = Vector3.one * (1f + pulse * .14f);
                guidePathRt.anchoredPosition += guideFuse
                    ? new Vector2(0, Mathf.Sin(guideClock * 4f) * .25f)
                    : new Vector2(Mathf.Sin(guideClock * 4f) * .25f, 0);
            }
            if (rippleClock > 0f)
            {
                rippleClock -= dt; float u = 1f - rippleClock / .42f; rippleRt.localScale = Vector3.one * Mathf.Lerp(.35f, 1.8f, u);
                rippleImg.color = new Color(Paper.r, Paper.g, Paper.b, Mathf.Lerp(.8f, 0, u)); if (rippleClock <= 0) rippleRt.gameObject.SetActive(false);
            }
            if (toastClock > 0f) { toastClock -= dt; toastG.alpha = Mathf.Clamp01(Mathf.Min((toastClock + .15f) * 5f, 1f)); } else toastG.alpha = 0;
            if (phase == Phase.Title)
            {
                float breathe = 1f + Mathf.Sin(worldClock * 2.2f) * .025f; titleBannerRt.localScale = new Vector3(breathe, breathe, 1);
                startSealRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(worldClock * 1.6f) * 3f);
                startSealRt.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 3.1f) * .035f - titlePress * .08f);
            }
            if (phase == Phase.Reveal)
            {
                revealUiClock += dt; float u = Mathf.Clamp01(revealUiClock / .28f);
                revealRt.localScale = Vector3.one * (1f + Mathf.Sin(u * Mathf.PI) * .08f);
            }
            if (phase == Phase.End)
            {
                endDisplayedScore = Mathf.MoveTowards(endDisplayedScore, st.score, dt * 950f); endScore.text = Mathf.RoundToInt(endDisplayedScore) + "점";
                endRt.localScale = Vector3.one * (1f + Mathf.Sin(worldClock * 1.8f) * .012f);
            }
        }

        void AnimateWorld(float dt)
        {
            worldClock += dt;
            for (int i = 0; i < clouds.Length; i++) clouds[i].localPosition += new Vector3(Mathf.Sin(worldClock * .22f + i) * .0007f, Mathf.Sin(worldClock * .65f + i) * .0012f, 0);
            if (pinwheel) pinwheel.localRotation = Quaternion.Euler(0, 0, -worldClock * 48f);
            for (int i = 0; i < laundry.Length; i++) laundry[i].localRotation = Quaternion.Euler(Mathf.Sin(worldClock * 2f + i) * 5f, 0, Mathf.Sin(worldClock * 1.2f + i) * 3f);
            float breathe = 1f + Mathf.Sin(worldClock * 2.4f) * .035f;
            birdRoot.localScale = new Vector3(1f / breathe, breathe, 1f);
            birdWingL.localRotation = Quaternion.Euler(0, 0, 26f + Mathf.Sin(worldClock * 2.2f) * 4f - birdHop * 25f);
            birdWingR.localRotation = Quaternion.Euler(0, 0, -26f - Mathf.Sin(worldClock * 2.2f) * 4f + birdHop * 25f);
            birdHop = Mathf.MoveTowards(birdHop, 0, dt * 2.4f);
            birdRoot.localPosition = birdHome + Vector3.up * (Mathf.Abs(Mathf.Sin(worldClock * 1.2f)) * .05f + birdHop * 1.1f);
            if (worldLaunch)
            {
                launchClock += dt;
                for (int i = 0; i < roster.Count; i++)
                {
                    float u = Mathf.Clamp01((launchClock - i * .075f) / .72f);
                    if (u > 0)
                    {
                        trails[i].gameObject.SetActive(u < .94f); worldRockets[i].localPosition = new Vector3(-5.4f + (i % 6) * 2.15f, .35f + u * 7f, -.4f + (i / 6) * 3.1f);
                        float squash = 1f + Mathf.Sin(Mathf.Clamp01(u * 4f) * Mathf.PI) * .18f;
                        worldRockets[i].localScale = new Vector3(1f / squash, squash, 1f / squash);
                        if (u >= .92f) bloomRoots[i].gameObject.SetActive(true);
                        if (bloomRoots[i].gameObject.activeSelf) bloomRoots[i].localScale = Vector3.one * Mathf.Clamp01((u - .90f) * 10f);
                    }
                }
            }
            if (wrongShake > 0f)
            {
                wrongShake -= dt; float a = wrongShake / .34f; cam.transform.position = cameraHome + new Vector3(Mathf.Sin(worldClock * 45f) * .10f * a, 0, 0);
            }
            else cam.transform.position = Vector3.Lerp(cam.transform.position, cameraHome, dt * 8f);
        }

        void FitCamera()
        {
            if (!cam) return; float a = Screen.width / Mathf.Max(1f, Screen.height);
            cam.orthographicSize = a < .68f ? 9.4f : a < 1.15f ? 7.9f : 6.5f;
        }

        void ResetWorldForTitle()
        {
            worldLaunch = worldWrong = false; launchClock = 0; wrongShake = 0; birdHop = 0; cameraHome = new Vector3(10.2f, 10.5f, -14.2f); cam.transform.position = cameraHome;
            for (int i = 0; i < 12; i++) { worldRockets[i].gameObject.SetActive(i < 8); worldRockets[i].localPosition = new Vector3(-5.4f + (i % 6) * 2.15f, .35f, -.4f + (i / 6) * 3.1f); worldRockets[i].localScale = Vector3.one; trails[i].gameObject.SetActive(false); bloomRoots[i].gameObject.SetActive(false); }
        }

        void ResetWorldForRun() { ResetWorldForTitle(); }

        void BeginRevealVisual(bool correct, bool wasFirst)
        {
            revealUiClock = 0; worldLaunch = correct; worldWrong = !correct; launchClock = 0;
            if (correct)
            {
                revealTitle.text = wasFirst ? "첫 점검 정확" : "수리 완료";
                revealTitle.color = Mint; revealFormula.text = (current.product ? current.aItems.Length + " × " + current.bItems.Length : current.aItems.Length + " + " + current.bItems.Length)
                    + " = " + current.Count + "가지\n학생이 만든 모든 경우를 한 번씩 발사합니다";
                birdHop = 1f; MgfSfx.Play("correct", .45f);
                for (int i = 0; i < roster.Count; i++) bloomRoots[i].gameObject.SetActive(false);
            }
            else
            {
                int d = BulkkotRules.DuplicateCount(roster), m = BulkkotRules.MissingCount(current, roster);
                revealTitle.text = "도화선이 갈라졌습니다"; revealTitle.color = RocketRed;
                revealFormula.text = "중복 " + d + "개는 갈라진 도화선 · 누락 " + m + "개는 빈 발사대\n전체 개수만 같아도 통과할 수 없습니다";
                wrongShake = .34f; birdHop = .35f; MgfSfx.Play("wrong", .35f);
                for (int i = 0; i < roster.Count; i++) ringBg[i, 0].color = new Color(.54f, .35f, .34f, 1f);
            }
        }

        void CompleteRevealVisual(bool correct)
        {
            worldLaunch = false; worldWrong = false;
            for (int i = 0; i < 12; i++) { trails[i].gameObject.SetActive(false); bloomRoots[i].gameObject.SetActive(false); worldRockets[i].localScale = Vector3.one; }
            for (int i = 0; i < roster.Count; i++) ringBg[i, 0].color = RocketRed;
        }

        void ShowEnd(string reason)
        {
            bool clear = reason == "clear"; endDisplayedScore = 0;
            endTitle.text = clear ? "옥상 점검 합격" : reason == "time" ? "시간 종료" : reason == "mastery-impossible" ? "첫 제출 목표 종료" : reason == "mastery" ? "첫 제출 기록 부족" : "안전등 소진";
            endTitle.color = clear ? Mint : RocketRed;
            endStats.text = "완료 " + st.solved + "/6  ·  첫 제출 " + st.firstCorrect + "/" + st.firstAttempts + "\n안전등 " + st.lives + "개  ·  " + (clear ? "모든 경우가 밤하늘에 펼쳐졌습니다" : "중복 없이 빠짐없이 다시 점검해 보세요");
            endTap.text = "종이 봉인을 눌러 다시 점검";
        }

        void BeginTitlePress() { titlePress = 1f; }
        void EndTitlePress() { titlePress = 0f; }
        void PunchRestart() { endRt.localScale = Vector3.one * .94f; MgfSfx.Play("tap", .2f); }

        bool HitRing(Vector2 screen, out int card, out int ring)
        {
            card = ring = -1;
            for (int i = 0; i < roster.Count; i++) for (int r = 0; r < 2; r++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(ringRt[i, r], screen, null)) { card = i; ring = r; return true; }
            }
            return false;
        }

        bool HitFuse(Vector2 screen) => RectTransformUtility.RectangleContainsScreenPoint(fuseHandleRt, screen, null);

        void BeginRingDrag(int card, int ring) { cardPulse[card] = .06f; ShowRingGuide(card, ring, false); }
        void UpdateRingDrag(int card, int ring, float dx) { if (card >= 0) ringRt[card, ring].anchoredPosition = new Vector2(Mathf.Clamp(dx * .38f, -28f, 28f), ringRt[card, ring].anchoredPosition.y); }
        void EndRingDrag(int card, int ring) { if (card >= 0) ringRt[card, ring].anchoredPosition = Vector2.zero; }
        void BeginFuseDrag() { fuseHandleRt.localScale = new Vector3(.92f, 1.08f, 1); MgfSfx.Play("tap", .14f); }
        void UpdateFuseDrag(float dy) { fuseHandleRt.anchoredPosition = new Vector2(0, -Mathf.Clamp(dy, 0, 70)); }
        void EndFuseDrag() { fuseHandleRt.anchoredPosition = Vector2.zero; fuseHandleRt.localScale = Vector3.one; }

        void ShowRingGuide(int card, int ring, bool boosted)
        {
            if (card < 0 || card >= roster.Count) return;
            guideFuse = false; guideRingRt.gameObject.SetActive(true); guidePathRt.gameObject.SetActive(true); guideBoost = boosted ? 1.2f : 0;
            guideRingRt.sizeDelta = new Vector2(100, 54);
            guideTxt.text = "←  밀기  →";
            Vector3 world = ringRt[card, ring].TransformPoint(ringRt[card, ring].rect.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, screen, null, out Vector2 local);
            guideRingRt.anchoredPosition = local; guidePathRt.anchoredPosition = local + new Vector2(0, -42);
        }

        void ShowFuseGuide(bool boosted)
        {
            guideFuse = true; guideRingRt.gameObject.SetActive(true); guidePathRt.gameObject.SetActive(true); guideBoost = boosted ? 1.2f : 0;
            guideTxt.text = "↓  아래로 당기기";
            Vector3 world = fuseHandleRt.TransformPoint(fuseHandleRt.rect.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, screen, null, out Vector2 local);
            guideRingRt.anchoredPosition = local; guideRingRt.sizeDelta = new Vector2(82, 96);
            guidePathRt.anchoredPosition = local + new Vector2(0, -70);
        }

        void ReplayGuide(bool strong)
        {
            if (phase == Phase.Practice && BulkkotRules.ValidateRoster(current, roster)) ShowFuseGuide(strong);
            else if (phase == Phase.Practice) ShowRingGuide(1, 1, strong);
            else if (selectedShell >= 0) ShowRingGuide(selectedShell, Mathf.Max(0, selectedRing), strong);
            else PulseFuse();
        }

        void PulseFuse() { if (roster.Count > 0) cardPulse[0] = .08f; fuseHandleRt.localScale = Vector3.one * 1.12f; ShowFuseGuide(true); }

        void RefuseInput(string message)
        {
            ShowToast(message, 2.2f); MgfSfx.Play("wrong", .13f);
            if (selectedShell >= 0 && selectedShell < roster.Count) cardPulse[selectedShell] = .12f;
        }

        void ShowToast(string message, float seconds)
        {
            toastTxt.text = message; toastClock = seconds; toastG.alpha = 1;
        }

        void SpawnTapRipple(Vector2 screen)
        {
            rippleRt.gameObject.SetActive(true); rippleClock = .42f; rippleRt.localScale = Vector3.one * .35f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRt, screen, null, out Vector2 local); rippleRt.anchoredPosition = local;
        }

        void FillScreenProbe()
        {
            st.screenW = Screen.width; st.screenH = Screen.height; st.cardPx = new int[roster.Count * 3];
            for (int i = 0; i < roster.Count; i++)
            {
                st.cardPx[i * 3] = Mathf.RoundToInt(CenterScreen(cardRt[i]).x);
                st.cardPx[i * 3 + 1] = Mathf.RoundToInt(CenterScreen(ringRt[i, 0]).y);
                st.cardPx[i * 3 + 2] = Mathf.RoundToInt(CenterScreen(ringRt[i, 1]).y);
            }
            Vector2 f = CenterScreen(fuseHandleRt); st.fusePx[0] = Mathf.RoundToInt(f.x); st.fusePx[1] = Mathf.RoundToInt(f.y);
        }

        static Vector2 CenterScreen(RectTransform rt) => RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
    }
}
