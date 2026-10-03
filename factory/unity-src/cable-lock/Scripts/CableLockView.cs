// 케이블 록 — 절개형 드라이도크 2.5D 계측 무대, 평면 측량도, 케이블 길이 릴 UI.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.CableLock
{
    public partial class CableLockGame
    {
        static readonly Color Chalk = MgfLook.Hex("#566068");
        static readonly Color Blue = MgfLook.Hex("#163A55");
        static readonly Color Orange = MgfLook.Hex("#F06A2B");
        static readonly Color Water = MgfLook.Hex("#64B9C2");
        static readonly Color Ink = MgfLook.Hex("#081722");
        static readonly Color Peach = MgfLook.Hex("#A43F2D");
        static readonly Color Cream = MgfLook.Hex("#E8E3D8");
        static readonly Color Steel = MgfLook.Hex("#273640");
        static readonly Color Grid = MgfLook.Hex("#315466");

        Camera cam;
        Transform capstanRoot, capstanDrum, craneHook, cargo, shipRoot;
        Vector3 capstanBasePosition;
        Transform[] ratchets = new Transform[3];
        Transform[] hatchLeft = new Transform[6];
        Transform[] hatchRight = new Transform[6];
        Transform[] waterLayers = new Transform[6];
        Renderer[] ratchetRenderers = new Renderer[3];
        LineRenderer cableLine, cableTrail, cableBraidA, cableBraidB;
        Material chalkMat, blueMat, orangeMat, waterMat, inkMat, creamMat, peachMat, glowOrangeMat;
        float worldClock;
        float reelVisual;
        float revealVisualT;
        float wrongJolt;
        float hitStop;
        int waterShown;
        bool land;

        CanvasGroup titleG, hudG, endG, toastG;
        RectTransform titleRt, hudRt, endRt, ctaRt, endCtaRt;
        RawImage titleArt;
        TextMeshProUGUI[] logo = new TextMeshProUGUI[3];
        TextMeshProUGUI tagTxt, badgeTxt, bestTxt, ctaTxt;
        TextMeshProUGUI scoreTxt, progressTxt, livesTxt, goalTxt, promptTxt, formulaTxt, rolesTxt;
        TextMeshProUGUI triATxt, triBTxt, triCTxt, angleTxt;
        TextMeshProUGUI lengthTxt, feedbackTxt, deckTxt;
        RectTransform goalRt, promptRt, diagramRt, reelRt, trackRt, handleRt, practiceTargetRt, guideFingerRt, magnifierRt;
        Image handleImg, practiceTargetImg, guideFingerImg, cableFillImg;
        Image triBase, triVert, triHyp, rightAngleH, rightAngleV;
        readonly RectTransform[] ghostRt = new RectTransform[3];
        readonly TextMeshProUGUI[] ghostTxt = new TextMeshProUGUI[3];
        readonly TextMeshProUGUI[] magnifierTxt = new TextMeshProUGUI[5];
        readonly Image[] lifeStrands = new Image[3];
        readonly Image[] ripples = new Image[6];
        readonly float[] rippleT = { 9, 9, 9, 9, 9, 9 };
        int rippleNext;
        RectTransform toastRt;
        TextMeshProUGUI toastTxt;
        TextMeshProUGUI endTitle, endCount, endStats, endHint, endCtaTxt;
        Sprite roundSprite, ringSprite;
        float toastT;
        float guideBoost;
        float displayedScore;
        int displayedScoreInt = -1;
        int layoutW = -1, layoutH = -1;

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var im = rt.gameObject.AddComponent<Image>();
            im.color = color; im.sprite = sprite; im.raycastTarget = false;
            if (sprite && sprite.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color,
            float width = 360f, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R("Text", parent, anchor, pos, new Vector2(width, size * 1.7f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.text = text;
            return t;
        }

        static Sprite MakeSprite(int size, float radius, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { name = ring ? "CableRing" : "CableRound", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + .5f, fy = y + .5f, a;
                if (ring)
                {
                    float dx = fx - size * .5f, dy = fy - size * .5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(1f - Mathf.Abs(d - size * .42f) / (size * .06f));
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
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect,
                new Vector4(b, b, b, b));
        }

        static Texture2D MakeSurfaceTexture(string name, int seed, float dark, float light, bool fibers)
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
                { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[S * S];
            uint h = (uint)seed;
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                h = h * 1664525u + 1013904223u;
                float n = ((h >> 8) & 1023) / 1023f;
                float grain = Mathf.Lerp(dark, light, n);
                if (fibers)
                {
                    float diagonal = Mathf.Abs(Mathf.Sin((x + y * 1.7f) * .62f));
                    grain *= Mathf.Lerp(.70f, 1.08f, diagonal);
                }
                else if (((x + seed) % 17 == 0 && y % 7 < 2) || ((y + seed) % 23 == 0 && x % 9 == 0))
                    grain *= .55f;
                byte c = (byte)(Mathf.Clamp01(grain) * 255);
                px[y * S + x] = new Color32(c, c, c, 255);
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return tex;
        }

        void BuildWorld()
        {
            // WebGL stripping에서 primitive collider 타입을 보존한다.
            var keep = new GameObject("PrimitiveColliderTypes");
            keep.AddComponent<CapsuleCollider>(); keep.AddComponent<SphereCollider>();
            Destroy(keep);

            // bit-sasu의 밝은 해안 파스텔과 분리된, 어두운 절개 도면/중장비 무대.
            MgfLook.Sky(MgfLook.Hex("#07131C"), MgfLook.Hex("#142B38"), MgfLook.Hex("#0A1118"), .48f);
            MgfLook.Sun(new Vector3(52, -38, 18), MgfLook.Hex("#D8E6DF"), 1.05f, .78f);
            cam = MgfLook.Camera(new Vector3(11.8f, 10.8f, -14.2f), new Vector3(0, .4f, 3.5f), 35f);
            cam.orthographic = true; cam.orthographicSize = 7.4f;

            var concreteTex = MakeSurfaceTexture("PittedConcrete", 17, .62f, .96f, false);
            var powderTex = MakeSurfaceTexture("PowderSteel", 41, .78f, 1f, false);
            var fiberTex = MakeSurfaceTexture("WovenFiber", 73, .67f, 1f, true);
            chalkMat = MgfLook.Lit(Chalk, .07f, .02f, null, concreteTex);
            blueMat = MgfLook.Lit(Blue, .20f, .58f, null, powderTex);
            orangeMat = MgfLook.Lit(Orange, .12f, .02f, null, fiberTex);
            waterMat = MgfLook.Lit(MgfLook.Hex("#0B5665"), .48f, .10f, MgfLook.Hex("#092D38"));
            inkMat = MgfLook.Lit(Ink, .12f, .48f, null, powderTex);
            creamMat = MgfLook.Lit(MgfLook.Hex("#AEB7B8"), .10f, .18f, null, concreteTex);
            peachMat = MgfLook.Lit(Peach, .09f, .14f, null, powderTex);
            glowOrangeMat = MgfLook.Lit(Orange, .45f, .08f, Orange * 1.15f);
            chalkMat.SetTextureScale("_MainTex", new Vector2(9, 9));
            blueMat.SetTextureScale("_MainTex", new Vector2(5, 5));
            orangeMat.SetTextureScale("_MainTex", new Vector2(6, 2));

            var baseFloor = MgfLook.Block("CutawayDockBase", new Vector3(0, -.65f, 4), new Vector3(22, 1.1f, 25), .06f, chalkMat);
            Destroy(baseFloor.GetComponent<Collider>());
            for (int i = 0; i < 4; i++)
            {
                float y = -.02f + i * .38f;
                float x = 8.7f - i * .72f;
                MgfLook.Block("DockStepL" + i, new Vector3(-x, y, 4), new Vector3(2.1f, .55f, 22), .04f, chalkMat);
                MgfLook.Block("DockStepR" + i, new Vector3(x, y, 4), new Vector3(2.1f, .55f, 22), .04f, chalkMat);
            }
            for (int i = 0; i < 12; i++)
            {
                var line = MgfLook.Block("BlueprintGrid" + i, new Vector3(-8.2f + i * 1.48f, .02f, 5.5f),
                    new Vector3(.035f, .025f, 18), .005f, MgfLook.Lit(Grid, .05f, .05f, Grid * .12f));
                line.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            for (int i = 0; i < 9; i++)
            {
                var line = MgfLook.Block("BlueprintCross" + i, new Vector3(0, .025f, -1.2f + i * 1.75f),
                    new Vector3(17, .025f, .035f), .005f, MgfLook.Lit(Grid, .05f, .05f, Grid * .12f));
                line.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            // 화면 오른쪽을 실제 공간이 아니라 절개 모형으로 읽히게 하는 검은 단면과 주황 안전 표식.
            MgfLook.Block("CutFace", new Vector3(9.75f, .5f, 4), new Vector3(.55f, 2.6f, 22), .02f, inkMat);
            for (int i = 0; i < 12; i++)
            {
                var hazard = MgfLook.Block("Hazard" + i, new Vector3(9.43f, 1.65f, -5.3f + i * 1.75f),
                    new Vector3(.08f, .28f, .82f), .01f, i % 2 == 0 ? orangeMat : inkMat);
                hazard.transform.localRotation = Quaternion.Euler(0, 18, 0);
            }

            shipRoot = new GameObject("IvoryCargoShip").transform;
            shipRoot.position = new Vector3(3.0f, .25f, 5.7f);
            MgfLook.Block("ShipHull", Vector3.zero, new Vector3(7.8f, 1.0f, 10.5f), .08f, creamMat, shipRoot);
            MgfLook.Block("ShipBlueStripe", new Vector3(0, .13f, -5.22f), new Vector3(6.8f, .28f, .14f), .06f, blueMat, shipRoot);
            for (int i = 0; i < 6; i++)
            {
                float z = -3.6f + i * 1.45f;
                hatchLeft[i] = MgfLook.Block("HatchL" + i, new Vector3(-1.55f, .64f, z), new Vector3(2.9f, .22f, 1.18f), .09f, creamMat, shipRoot).transform;
                hatchRight[i] = MgfLook.Block("HatchR" + i, new Vector3(1.55f, .64f, z), new Vector3(2.9f, .22f, 1.18f), .09f, creamMat, shipRoot).transform;
                hatchLeft[i].localRotation = Quaternion.Euler(0, 0, -7);
                hatchRight[i].localRotation = Quaternion.Euler(0, 0, 7);
            }

            capstanRoot = new GameObject("UltramarineCapstan").transform;
            capstanRoot.position = new Vector3(-3.8f, .35f, .2f);
            capstanBasePosition = capstanRoot.position;
            MgfLook.Prim(PrimitiveType.Cylinder, "CapstanBase", Vector3.zero, new Vector3(1.85f, .25f, 1.85f), blueMat, capstanRoot).transform.localScale = new Vector3(1.85f, .25f, 1.85f);
            capstanDrum = MgfLook.Prim(PrimitiveType.Cylinder, "CapstanDrum", new Vector3(0, 1.05f, 0), new Vector3(1.1f, 1.25f, 1.1f), blueMat, capstanRoot).transform;
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2f / 16f;
                var tooth = MgfLook.Block("GearTooth" + i,
                    new Vector3(Mathf.Cos(a) * 1.18f, 1.05f, Mathf.Sin(a) * 1.18f),
                    new Vector3(.18f, .36f, .42f), .025f, inkMat, capstanRoot);
                tooth.transform.localRotation = Quaternion.Euler(0, -i * 22.5f, 0);
            }
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                ratchets[i] = MgfLook.Block("Ratchet" + i, new Vector3(Mathf.Cos(a) * 1.1f, 1.15f, Mathf.Sin(a) * 1.1f),
                    new Vector3(.42f, .25f, .7f), .1f, orangeMat, capstanRoot).transform;
                ratchets[i].localRotation = Quaternion.Euler(0, -i * 120, 0);
                ratchetRenderers[i] = ratchets[i].GetComponent<Renderer>();
            }

            var crane = new GameObject("BlueCrane").transform;
            crane.position = new Vector3(-5.5f, 0, 5.7f);
            MgfLook.Block("CraneMast", new Vector3(0, 3.2f, 0), new Vector3(.65f, 6.4f, .65f), .12f, blueMat, crane);
            var boom = MgfLook.Block("CraneBoom", new Vector3(2.5f, 5.75f, 1.8f), new Vector3(6.3f, .46f, .5f), .12f, blueMat, crane);
            boom.transform.localRotation = Quaternion.Euler(0, -20, -10);
            craneHook = new GameObject("HookRig").transform;
            craneHook.position = new Vector3(-.2f, 4.9f, 5.2f);
            MgfLook.Prim(PrimitiveType.Cylinder, "HookPulley", Vector3.zero, new Vector3(.38f, .20f, .38f), inkMat, craneHook).transform.localRotation = Quaternion.Euler(90, 0, 0);
            cargo = MgfLook.Block("Cargo", new Vector3(0, -1.0f, 0), new Vector3(2.5f, 1.15f, 1.55f), .18f, orangeMat, craneHook).transform;

            var cableTex = MakeSurfaceTexture("CableDiagonalBraid", 109, .58f, 1f, true);
            var cableMat = MgfLook.Unlit(Orange, cableTex);
            cableMat.SetTextureScale("_MainTex", new Vector2(12, 1));
            cableLine = MakeLine("WovenCableCore", cableMat, .18f);
            cableBraidA = MakeLine("WovenCableStrandA", MgfLook.Unlit(MgfLook.Hex("#FF9B48"), cableTex), .075f);
            cableBraidB = MakeLine("WovenCableStrandB", MgfLook.Unlit(MgfLook.Hex("#A83A1E"), cableTex), .065f);
            cableTrail = MakeLine("CableAfterimage", MgfLook.Alpha(new Color(Orange.r, Orange.g, Orange.b, .22f)), .34f);
            cableLine.positionCount = cableBraidA.positionCount = cableBraidB.positionCount = cableTrail.positionCount = 5;

            for (int i = 0; i < waterLayers.Length; i++)
            {
                var w = MgfLook.Block("WaterLayer" + i, new Vector3(0, -.45f + i * .10f, 2.2f + i * .55f),
                    new Vector3(15.2f, .13f, 2.0f + i * .35f), .06f, waterMat);
                Destroy(w.GetComponent<Collider>());
                waterLayers[i] = w.transform;
                w.SetActive(false);
            }
            SetCableLength(5);
        }

        LineRenderer MakeLine(string name, Material mat, float width)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat; lr.startWidth = lr.endWidth = width; lr.useWorldSpace = true;
            lr.numCapVertices = 5; lr.numCornerVertices = 4;
            lr.textureMode = LineTextureMode.Tile;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false;
            return lr;
        }

        void BuildUi()
        {
            var canvas = MgfText.Canvas;
            var root = canvas.transform;
            roundSprite = MakeSprite(64, 13, false);
            ringSprite = MakeSprite(96, 0, true);

            titleRt = R("Title", root, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one; titleRt.sizeDelta = Vector2.zero;
            titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            var artRt = R("TitleKeyArt", titleRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            artRt.anchorMin = Vector2.zero; artRt.anchorMax = Vector2.one; artRt.sizeDelta = Vector2.zero;
            titleArt = artRt.gameObject.AddComponent<RawImage>();
            // 타이틀도 실제 런타임 절개 도크를 그대로 쓴다. 생성 이미지와 인게임의 재질 불일치를 만들지 않는다.
            titleArt.texture = null;
            titleArt.color = new Color(1, 1, 1, 0);
            titleArt.uvRect = new Rect(0, 0, 1, 1); titleArt.raycastTarget = false;
            var dawn = Img(titleRt, "CutawayShade", new Vector2(.5f, .5f), Vector2.zero, Vector2.zero,
                new Color(Ink.r, Ink.g, Ink.b, .34f));
            dawn.rectTransform.anchorMin = Vector2.zero; dawn.rectTransform.anchorMax = Vector2.one; dawn.rectTransform.sizeDelta = Vector2.zero;

            logo[0] = Txt(titleRt, "케이블 록", new Vector2(.5f, .83f), new Vector2(4, -5), 55, Ink, 360);
            logo[1] = Txt(titleRt, "케이블 록", new Vector2(.5f, .83f), new Vector2(2, -2), 55, Orange, 360);
            logo[2] = Txt(titleRt, "케이블 록", new Vector2(.5f, .83f), Vector2.zero, 55, Cream, 360);
            for (int i = 0; i < logo.Length; i++) { logo[i].fontStyle = FontStyles.Bold; logo[i].characterSpacing = 5; }
            tagTxt = Txt(titleRt, "길이를 감아 화물을 잠가라", new Vector2(.5f, .745f), Vector2.zero, 18, Cream, 350);
            tagTxt.fontStyle = FontStyles.Bold;
            badgeTxt = Txt(titleRt, "중3 · 삼각비 · 48칸 정밀 릴", new Vector2(.5f, .695f), Vector2.zero, 13, Water, 330);
            bestTxt = Txt(titleRt, "", new Vector2(.5f, .17f), Vector2.zero, 13, Cream, 330);
            ctaRt = R("TitleCta", titleRt, new Vector2(.5f, .09f), Vector2.zero, new Vector2(304, 72));
            Img(ctaRt, "CableClamp", new Vector2(.5f, .5f), Vector2.zero, new Vector2(304, 72), Orange);
            Img(ctaRt, "SafetyInsert", new Vector2(.5f, .5f), Vector2.zero, new Vector2(286, 54), Ink);
            ctaTxt = Txt(ctaRt, "계측 시작", new Vector2(.5f, .5f), Vector2.zero, 23, Cream, 270);
            ctaTxt.fontStyle = FontStyles.Bold;

            hudRt = R("Hud", root, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            hudRt.anchorMin = Vector2.zero; hudRt.anchorMax = Vector2.one; hudRt.sizeDelta = Vector2.zero;
            hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            var topRail = Img(hudRt, "TopInstrumentRail", new Vector2(.5f, 1f), new Vector2(0, -25), new Vector2(10, 50), new Color(Ink.r, Ink.g, Ink.b, .94f));
            topRail.rectTransform.anchorMin = new Vector2(0, 1); topRail.rectTransform.anchorMax = new Vector2(1, 1); topRail.rectTransform.sizeDelta = new Vector2(0, 50);
            scoreTxt = Txt(hudRt, "0", new Vector2(.035f, .95f), Vector2.zero, 23, Orange, 110, TextAlignmentOptions.Left);
            scoreTxt.rectTransform.pivot = new Vector2(0, .5f); scoreTxt.fontStyle = FontStyles.Bold;
            progressTxt = Txt(hudRt, "잠금 0/6 · 첫 시도 0 · 예비 3", new Vector2(.47f, .95f), Vector2.zero, 13, Cream, 260);
            progressTxt.enableAutoSizing = true; progressTxt.fontSizeMin = 9; progressTxt.fontSizeMax = 13;
            livesTxt = Txt(hudRt, "예비 케이블", new Vector2(.965f, .95f), new Vector2(-92, 0), 11, Water, 100, TextAlignmentOptions.Right);
            livesTxt.rectTransform.pivot = new Vector2(1, .5f);
            for (int i = 0; i < lifeStrands.Length; i++)
                lifeStrands[i] = Img(hudRt, "Strand" + i, new Vector2(.965f, .95f), new Vector2(-56 + i * 25, 0),
                    new Vector2(19, 9), Orange, roundSprite);

            var goal = Img(hudRt, "Goal", new Vector2(.5f, .91f), Vector2.zero, new Vector2(356, 36),
                new Color(Ink.r, Ink.g, Ink.b, .96f));
            goalRt = goal.rectTransform;
            Img(goal.transform, "GoalStripe", new Vector2(0, .5f), new Vector2(3, 0), new Vector2(6, 36), Orange);
            goalTxt = Txt(goal.transform, "계산한 길이만큼 케이블을 감아라", new Vector2(.5f, .5f), Vector2.zero, 14, Cream, 336);
            goalTxt.fontStyle = FontStyles.Bold;

            promptRt = R("PromptPanel", hudRt, new Vector2(.5f, .81f), Vector2.zero, new Vector2(356, 106));
            Img(promptRt, "Paper", new Vector2(.5f, .5f), Vector2.zero, new Vector2(356, 106), new Color(Ink.r, Ink.g, Ink.b, .96f));
            Img(promptRt, "PromptGauge", new Vector2(0, .5f), new Vector2(3, 0), new Vector2(6, 106), Orange);
            promptTxt = Txt(promptRt, "", new Vector2(.5f, .5f), Vector2.zero, 13.2f, Cream, 334);
            promptTxt.fontStyle = FontStyles.Bold; promptTxt.enableAutoSizing = true; promptTxt.fontSizeMin = 10; promptTxt.fontSizeMax = 13.2f;
            promptTxt.rectTransform.sizeDelta = new Vector2(334, 90);

            diagramRt = R("SurveyDiagram", hudRt, new Vector2(.5f, .66f), Vector2.zero, new Vector2(356, 126));
            Img(diagramRt, "SurveyPaper", new Vector2(.5f, .5f), Vector2.zero, new Vector2(356, 126), new Color(Steel.r, Steel.g, Steel.b, .98f));
            Txt(diagramRt, "정면 측량도  △ABC · ∠C=90°", new Vector2(.5f, .86f), Vector2.zero, 12, Water, 330).fontStyle = FontStyles.Bold;
            // 카메라 원근 없는 평면도이며, 문항의 30°·45°·60°에 맞춰 세 점을 매번 다시 배치한다.
            triBase = MakeUiLine(diagramRt, new Vector2(-145, -31), new Vector2(-54, -31), 5, Water);
            triVert = MakeUiLine(diagramRt, new Vector2(-54, -31), new Vector2(-54, 27), 5, Orange);
            triHyp = MakeUiLine(diagramRt, new Vector2(-145, -31), new Vector2(-54, 27), 5, Cream);
            rightAngleH = MakeUiLine(diagramRt, new Vector2(-66, -21), new Vector2(-56, -21), 2, Cream);
            rightAngleV = MakeUiLine(diagramRt, new Vector2(-66, -31), new Vector2(-66, -21), 2, Cream);
            triATxt = Txt(diagramRt, "A", new Vector2(.5f, .5f), Vector2.zero, 13, Cream, 30);
            triCTxt = Txt(diagramRt, "C", new Vector2(.5f, .5f), Vector2.zero, 13, Cream, 30);
            triBTxt = Txt(diagramRt, "B", new Vector2(.5f, .5f), Vector2.zero, 13, Cream, 30);
            angleTxt = Txt(diagramRt, "45°", new Vector2(.5f, .5f), Vector2.zero, 10, Orange, 44);
            formulaTxt = Txt(diagramRt, "tan A = 대변 / 이웃변", new Vector2(.68f, .60f), Vector2.zero, 12.5f, Cream, 205, TextAlignmentOptions.Left);
            formulaTxt.fontStyle = FontStyles.Bold;
            formulaTxt.enableAutoSizing = true; formulaTxt.fontSizeMin = 9.5f; formulaTxt.fontSizeMax = 12.5f;
            formulaTxt.rectTransform.sizeDelta = new Vector2(205, 54);
            rolesTxt = Txt(diagramRt, "", new Vector2(.68f, .25f), Vector2.zero, 10.2f, Cream, 205, TextAlignmentOptions.Left);
            rolesTxt.enableAutoSizing = true; rolesTxt.fontSizeMin = 8.5f; rolesTxt.fontSizeMax = 10.2f;
            rolesTxt.rectTransform.sizeDelta = new Vector2(205, 38);

            reelRt = R("CableReel", hudRt, new Vector2(.5f, .32f), Vector2.zero, new Vector2(300, 390));
            deckTxt = Txt(reelRt, "1 m 눈금 · 48칸 · 놓으면 잠금", new Vector2(.5f, .98f), Vector2.zero, 12, Water, 300);
            deckTxt.fontStyle = FontStyles.Bold;
            trackRt = R("CableTrack", reelRt, new Vector2(.5f, .48f), Vector2.zero, new Vector2(18, 300));
            Img(trackRt, "Rail", new Vector2(.5f, .5f), Vector2.zero, new Vector2(18, 300), Steel);
            cableFillImg = Img(trackRt, "OrangeCable", new Vector2(.5f, 0), Vector2.zero, new Vector2(10, 20), Orange, roundSprite);
            cableFillImg.rectTransform.pivot = new Vector2(.5f, 0);
            cableFillImg.rectTransform.anchorMin = cableFillImg.rectTransform.anchorMax = new Vector2(.5f, 0);
            for (int i = CableLockRules.MinLength; i <= CableLockRules.MaxLength; i++)
            {
                float y = -150 + (i - CableLockRules.MinLength) / (float)(CableLockRules.MaxLength - CableLockRules.MinLength) * 300;
                float w = i % 6 == 0 || i == 1 ? 56 : 24;
                Img(trackRt, "Tick" + i, new Vector2(.5f, .5f), new Vector2(-w * .5f - 16, y), new Vector2(w, 2),
                    new Color(Cream.r, Cream.g, Cream.b, i % 6 == 0 ? .78f : .30f));
                if (i == 1 || i % 6 == 0)
                    Txt(trackRt, i.ToString(), new Vector2(.5f, .5f), new Vector2(-66, y), 9.5f, Cream, 34, TextAlignmentOptions.Right);
            }
            // 전체 48칸은 위치 탐색용이고, 손을 댄 뒤에는 이 확대창에서 22단위당 1 m로
            // 자석 스냅한다. 현재값 주변 다섯 칸을 크게 보여 손가락이 눈금을 가리지 않는다.
            magnifierRt = R("SnapMagnifier", trackRt, new Vector2(.5f, .5f), new Vector2(-118, 0), new Vector2(82, 132));
            Img(magnifierRt, "MagnifierPlate", new Vector2(.5f, .5f), Vector2.zero, new Vector2(82, 132),
                new Color(Ink.r, Ink.g, Ink.b, .96f), roundSprite);
            Txt(magnifierRt, "확대", new Vector2(.5f, .91f), Vector2.zero, 8.5f, Water, 70).fontStyle = FontStyles.Bold;
            for (int i = 0; i < magnifierTxt.Length; i++)
            {
                magnifierTxt[i] = Txt(magnifierRt, "", new Vector2(.5f, .5f), new Vector2(0, 42 - i * 21),
                    i == 2 ? 14f : 10.5f, i == 2 ? Orange : Cream, 70);
                magnifierTxt[i].fontStyle = FontStyles.Bold;
            }
            magnifierRt.gameObject.SetActive(false);
            practiceTargetRt = R("PracticeTarget", trackRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(92, 48));
            practiceTargetImg = practiceTargetRt.gameObject.AddComponent<Image>(); practiceTargetImg.sprite = ringSprite; practiceTargetImg.color = Blue; practiceTargetImg.raycastTarget = false;
            var targetLabel = Txt(practiceTargetRt, "6 m", new Vector2(.5f, .5f), Vector2.zero, 12, Blue, 60); targetLabel.fontStyle = FontStyles.Bold;
            handleRt = R("OrangeHandle", trackRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(82, 66));
            handleImg = handleRt.gameObject.AddComponent<Image>(); handleImg.sprite = roundSprite; handleImg.color = Orange; handleImg.raycastTarget = false;
            Img(handleRt, "FiberBand", new Vector2(.5f, .5f), Vector2.zero, new Vector2(58, 12), Cream, roundSprite);
            guideFingerRt = R("GuideFinger", trackRt, new Vector2(.5f, .5f), new Vector2(54, 0), new Vector2(42, 54));
            guideFingerImg = guideFingerRt.gameObject.AddComponent<Image>(); guideFingerImg.sprite = roundSprite; guideFingerImg.color = new Color(Blue.r, Blue.g, Blue.b, .88f); guideFingerImg.raycastTarget = false;
            Txt(guideFingerRt, "↑", new Vector2(.5f, .5f), Vector2.zero, 27, Cream, 38).fontStyle = FontStyles.Bold;
            lengthTxt = Txt(reelRt, "5 m", new Vector2(.77f, .50f), Vector2.zero, 38, Orange, 130);
            lengthTxt.fontStyle = FontStyles.Bold;
            feedbackTxt = Txt(reelRt, "주황 손잡이를 위아래로 끌어 놓으시오", new Vector2(.5f, .03f), Vector2.zero, 12.5f, Cream, 300);

            for (int i = 0; i < ghostRt.Length; i++)
            {
                ghostRt[i] = R("GhostKnot" + i, trackRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(72, 28));
                var g = ghostRt[i].gameObject.AddComponent<Image>(); g.sprite = roundSprite; g.color = new Color(Orange.r, Orange.g, Orange.b, .30f); g.raycastTarget = false;
                ghostTxt[i] = Txt(ghostRt[i], "", new Vector2(.5f, .5f), Vector2.zero, 10, Ink, 66);
                ghostRt[i].gameObject.SetActive(false);
            }

            toastRt = R("Toast", hudRt, new Vector2(.5f, .12f), Vector2.zero, new Vector2(344, 58));
            toastG = toastRt.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0;
            Img(toastRt, "ToastBg", new Vector2(.5f, .5f), Vector2.zero, new Vector2(344, 58), Ink);
            toastTxt = Txt(toastRt, "", new Vector2(.5f, .5f), Vector2.zero, 13, Cream, 322); toastTxt.fontStyle = FontStyles.Bold;
            for (int i = 0; i < ripples.Length; i++)
            {
                ripples[i] = Img(hudRt, "TapRipple" + i, new Vector2(0, 0), Vector2.zero, new Vector2(58, 58), new Color(Blue.r, Blue.g, Blue.b, 0), ringSprite);
                ripples[i].rectTransform.pivot = new Vector2(.5f, .5f);
            }

            endRt = R("End", root, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            endRt.anchorMin = Vector2.zero; endRt.anchorMax = Vector2.one; endRt.sizeDelta = Vector2.zero;
            endG = endRt.gameObject.AddComponent<CanvasGroup>();
            var endPanel = Img(endRt, "Manifest", new Vector2(.5f, .54f), Vector2.zero, new Vector2(354, 390), new Color(Ink.r, Ink.g, Ink.b, .97f));
            Img(endPanel.transform, "ManifestStripe", new Vector2(.5f, 1), new Vector2(0, -4), new Vector2(354, 8), Orange);
            endTitle = Txt(endPanel.transform, "출항 준비 완료", new Vector2(.5f, .82f), Vector2.zero, 31, Cream, 320); endTitle.fontStyle = FontStyles.Bold;
            endCount = Txt(endPanel.transform, "0", new Vector2(.5f, .61f), Vector2.zero, 64, Orange, 260); endCount.fontStyle = FontStyles.Bold;
            endStats = Txt(endPanel.transform, "", new Vector2(.5f, .42f), Vector2.zero, 17, Water, 320); endStats.fontStyle = FontStyles.Bold;
            endHint = Txt(endPanel.transform, "", new Vector2(.5f, .24f), Vector2.zero, 13, Cream, 310);
            endHint.rectTransform.sizeDelta = new Vector2(310, 68);
            endCtaRt = R("EndCta", endRt, new Vector2(.5f, .20f), Vector2.zero, new Vector2(304, 70));
            Img(endCtaRt, "EndClamp", new Vector2(.5f, .5f), Vector2.zero, new Vector2(304, 70), Orange);
            Img(endCtaRt, "EndInsert", new Vector2(.5f, .5f), Vector2.zero, new Vector2(286, 52), Ink);
            endCtaTxt = Txt(endCtaRt, "다시 감기", new Vector2(.5f, .5f), Vector2.zero, 23, Cream, 280); endCtaTxt.fontStyle = FontStyles.Bold;
        }

        Image MakeUiLine(Transform parent, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = b - a;
            var im = Img(parent, "SurveyLine", new Vector2(.5f, .5f), (a + b) * .5f, new Vector2(d.magnitude, width), color, roundSprite);
            im.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            return im;
        }

        void PlaceUiLine(Image im, Vector2 a, Vector2 b, float width)
        {
            Vector2 d = b - a;
            im.rectTransform.anchoredPosition = (a + b) * .5f;
            im.rectTransform.sizeDelta = new Vector2(d.magnitude, width);
            im.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        void UpdateDiagram()
        {
            if (current == null || triBase == null) return;
            float radians = current.angleDegrees * Mathf.Deg2Rad;
            float rawHeight = Mathf.Tan(radians);
            float scale = Mathf.Min(94f, 75f / rawHeight);
            Vector2 a = new Vector2(-150, -38);
            Vector2 c = a + new Vector2(scale, 0);
            Vector2 b = c + new Vector2(0, rawHeight * scale);
            PlaceUiLine(triBase, a, c, 5);
            PlaceUiLine(triVert, c, b, 5);
            PlaceUiLine(triHyp, a, b, 5);
            PlaceUiLine(rightAngleH, c + new Vector2(-12, 11), c + new Vector2(-2, 11), 2);
            PlaceUiLine(rightAngleV, c + new Vector2(-12, 1), c + new Vector2(-12, 11), 2);
            triATxt.rectTransform.anchoredPosition = a + new Vector2(-10, -4);
            triCTxt.rectTransform.anchoredPosition = c + new Vector2(8, -6);
            triBTxt.rectTransform.anchoredPosition = b + new Vector2(8, 2);
            angleTxt.text = current.angleDegrees + "°";
            angleTxt.rectTransform.anchoredPosition = a + new Vector2(25, 12);
        }

        void LayoutForAspect()
        {
            int w = Screen.width, h = Screen.height;
            if (w == layoutW && h == layoutH) return;
            layoutW = w; layoutH = h; land = w >= 1024 && w > h;
            if (land)
            {
                // CanvasScaler 기준 좌표에서 위·왼쪽 경계를 직접 고정한다(비율 앵커 금지).
                // 가로판 캔버스 높이는 1280×800·1440×900에서 약 453, 2000×1040에서 약 414 단위다.
                // 상단 레일 50 아래로 목표 36 → 발문 118 → 측량도 144를 쌓아 총 384 단위 안에 두므로 어느 가로판에서도 잘리지 않는다.
                goalRt.anchorMin = goalRt.anchorMax = new Vector2(0, 1); goalRt.pivot = new Vector2(0, 1);
                goalRt.anchoredPosition = new Vector2(24, -58);
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(0, 1); promptRt.pivot = new Vector2(0, 1);
                promptRt.anchoredPosition = new Vector2(24, -102); promptRt.sizeDelta = new Vector2(356, 118);
                promptTxt.rectTransform.sizeDelta = new Vector2(334, 96);
                diagramRt.anchorMin = diagramRt.anchorMax = new Vector2(0, 1); diagramRt.pivot = new Vector2(0, 1);
                diagramRt.anchoredPosition = new Vector2(24, -228); diagramRt.sizeDelta = new Vector2(356, 144);
                // 가로판 논리 높이는 1280×800에서 약 453이다. 500 높이 릴은 상단의
                // 「1 m 눈금」 문구가 화면 밖으로 나가므로 390 안에 트랙·안내를 모두 넣는다.
                reelRt.anchorMin = reelRt.anchorMax = new Vector2(.66f, .48f); reelRt.sizeDelta = new Vector2(330, 390);
                trackRt.sizeDelta = new Vector2(18, 300);
                cam.orthographicSize = 7.25f;
                cam.transform.position = new Vector3(12.5f, 10.6f, -14.5f);
                cam.transform.LookAt(new Vector3(2.0f, .6f, 4.3f));
                livesTxt.gameObject.SetActive(true);
                livesTxt.rectTransform.anchoredPosition = new Vector2(-108, 0);
                for (int i = 0; i < lifeStrands.Length; i++)
                    lifeStrands[i].rectTransform.anchoredPosition = new Vector2(-72 + i * 25, 0);
            }
            else
            {
                goalRt.anchorMin = goalRt.anchorMax = new Vector2(.5f, .91f); goalRt.pivot = new Vector2(.5f, .5f);
                goalRt.anchoredPosition = Vector2.zero;
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(.5f, .81f); promptRt.pivot = new Vector2(.5f, .5f);
                promptRt.anchoredPosition = Vector2.zero; promptRt.sizeDelta = new Vector2(356, 106);
                promptTxt.rectTransform.sizeDelta = new Vector2(334, 90);
                diagramRt.anchorMin = diagramRt.anchorMax = new Vector2(.5f, .66f); diagramRt.pivot = new Vector2(.5f, .5f);
                diagramRt.anchoredPosition = Vector2.zero; diagramRt.sizeDelta = new Vector2(356, 126);
                reelRt.anchorMin = reelRt.anchorMax = new Vector2(.5f, .32f); reelRt.sizeDelta = new Vector2(300, 390);
                trackRt.sizeDelta = new Vector2(18, 300);
                cam.orthographicSize = 11.2f;
                cam.transform.position = new Vector3(11.8f, 11.4f, -14.8f);
                cam.transform.LookAt(new Vector3(0, .2f, 3.8f));
                livesTxt.gameObject.SetActive(false);
                // 모바일 음소거 버튼과 겹치지 않도록 예비 케이블 표시를 한 칸 왼쪽으로 당긴다.
                for (int i = 0; i < lifeStrands.Length; i++)
                    lifeStrands[i].rectTransform.anchoredPosition = new Vector2(-92 + i * 25, 0);
            }
            if (titleArt && titleArt.texture)
            {
                float screenAspect = Mathf.Max(.01f, (float)w / h);
                float imageAspect = (float)titleArt.texture.width / titleArt.texture.height;
                if (screenAspect < imageAspect)
                {
                    float uw = screenAspect / imageAspect;
                    titleArt.uvRect = new Rect((1f - uw) * .5f, 0, uw, 1);
                }
                else
                {
                    float uh = imageAspect / screenAspect;
                    titleArt.uvRect = new Rect(0, (1f - uh) * .5f, 1, uh);
                }
            }
            PositionHandle(false);
        }

        void ResetWorldForRun()
        {
            waterShown = 0; reelVisual = 0; revealVisualT = 0; wrongJolt = 0; hitStop = 0;
            shipRoot.localPosition = new Vector3(3.0f, .25f, 5.7f);
            craneHook.position = new Vector3(-.2f, 4.9f, 5.2f);
            cargo.localPosition = new Vector3(0, -1.0f, 0);
            for (int i = 0; i < waterLayers.Length; i++) waterLayers[i].gameObject.SetActive(false);
            for (int i = 0; i < hatchLeft.Length; i++)
            {
                hatchLeft[i].localPosition = new Vector3(-1.55f, .64f, -3.6f + i * 1.45f);
                hatchRight[i].localPosition = new Vector3(1.55f, .64f, -3.6f + i * 1.45f);
                hatchLeft[i].localRotation = Quaternion.Euler(0, 0, -7); hatchRight[i].localRotation = Quaternion.Euler(0, 0, 7);
            }
            for (int i = 0; i < ratchets.Length; i++) ratchetRenderers[i].sharedMaterial = orangeMat;
        }

        void ResetWorldForProblem()
        {
            revealVisualT = 0; wrongJolt = 0;
            cargo.localPosition = new Vector3(0, -1.0f, 0);
            for (int i = 0; i < ratchets.Length; i++) ratchetRenderers[i].sharedMaterial = orangeMat;
        }

        void SetCableLength(int length)
        {
            float f = (length - CableLockRules.MinLength) / (float)(CableLockRules.MaxLength - CableLockRules.MinLength);
            var a = capstanRoot.position + new Vector3(0, 1.22f, 0);
            var b = new Vector3(-2.25f, 2.1f + f * .25f, 1.1f);
            var c = new Vector3(-1.25f, 3.55f, 3.1f);
            var d = craneHook.position + new Vector3(0, .1f, 0);
            var e = cargo.position + new Vector3(0, .65f, 0);
            Vector3[] pts = { a, b, c, d, e };
            for (int i = 0; i < pts.Length; i++)
            {
                cableLine.SetPosition(i, pts[i]);
                cableBraidA.SetPosition(i, pts[i] + new Vector3(.035f, .035f, -.025f));
                cableBraidB.SetPosition(i, pts[i] + new Vector3(-.035f, -.025f, .025f));
                cableTrail.SetPosition(i, pts[i] + Vector3.back * .08f);
            }
            capstanDrum.localRotation = Quaternion.Euler(0, length * 15f, 0);
        }

        void UpdateWorld(float dt)
        {
            LayoutForAspect();
            worldClock += dt;
            if (hitStop > 0) { hitStop -= dt; return; }
            reelVisual = Mathf.MoveTowards(reelVisual, st.selectedLength, dt * 18f);
            float idle = Mathf.Sin(worldClock * .75f) * .025f;
            capstanRoot.localScale = new Vector3(1f + idle, 1f - idle * .4f, 1f + idle);
            cableTrail.startColor = cableTrail.endColor = new Color(Orange.r, Orange.g, Orange.b, .14f + .08f * Mathf.Sin(worldClock * 4f));

            if (phase == Phase.Title)
            {
                capstanDrum.Rotate(0, dt * 18f, 0, Space.Self);
                craneHook.position = new Vector3(-.2f + Mathf.Sin(worldClock * .42f) * .55f, 4.9f, 5.2f);
            }

            if (phase == Phase.Reveal)
            {
                revealVisualT += dt;
                if (revealCorrect)
                {
                    float t = Mathf.Clamp01(revealVisualT / 1.35f);
                    // 래칫 3단 → 화물 하강 → 해치 닫힘 → 수위 상승.
                    for (int i = 0; i < 3; i++)
                    {
                        float q = Mathf.Clamp01((t * 5f - i * .52f));
                        float pop = 1f + Mathf.Sin(q * Mathf.PI) * .30f;
                        ratchets[i].localScale = Vector3.one * pop;
                        if (q > .2f) ratchetRenderers[i].sharedMaterial = glowOrangeMat;
                    }
                    float down = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .28f) / .36f));
                    cargo.localPosition = new Vector3(0, -1f - down * 2.15f, 0);
                    int h = Math.Max(0, Math.Min(5, st.solved - 1));
                    float close = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - .61f) / .24f));
                    hatchLeft[h].localPosition = new Vector3(-1.55f + close * 1.45f, .64f, -3.6f + h * 1.45f);
                    hatchRight[h].localPosition = new Vector3(1.55f - close * 1.45f, .64f, -3.6f + h * 1.45f);
                    hatchLeft[h].localRotation = Quaternion.Euler(0, 0, -7 + close * 7); hatchRight[h].localRotation = Quaternion.Euler(0, 0, 7 - close * 7);
                    if (t > .75f && h >= waterShown)
                    {
                        waterLayers[h].gameObject.SetActive(true);
                        waterLayers[h].localScale = new Vector3(1, 1, Mathf.SmoothStep(.08f, 1f, (t - .75f) / .25f));
                        waterShown = h + 1;
                    }
                    shipRoot.localPosition = new Vector3(3.0f, .25f + st.solved * .065f * Mathf.SmoothStep(0, 1, t), 5.7f);
                    if (st.solved >= 6) cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, land ? 7.95f : 12.1f, dt * 2.4f);
                }
                else
                {
                    wrongJolt = Mathf.Max(0, 1f - revealVisualT / .42f);
                    float j = Mathf.Sin(revealVisualT * 45f) * wrongJolt;
                    capstanRoot.position = capstanBasePosition + new Vector3(j * .12f, 0, 0);
                    cableLine.widthMultiplier = .72f + .28f * Mathf.Abs(Mathf.Sin(revealVisualT * 25f));
                    cableBraidA.widthMultiplier = cableBraidB.widthMultiplier = cableLine.widthMultiplier;
                }
            }
            else
            {
                capstanRoot.position = capstanBasePosition;
                cableLine.widthMultiplier = 1f;
                cableBraidA.widthMultiplier = cableBraidB.widthMultiplier = 1f;
            }
        }

        void BeginRevealVisual(bool correct, bool first)
        {
            revealVisualT = 0;
            if (correct)
            {
                hitStop = .075f;
                MgfSfx.Play("correct", .42f);
                formulaTxt.text = current.formula;
                feedbackTxt.text = first ? "첫 시도 잠금 · 화물 안착" : "재계산 잠금 · 화물 안착";
                goalTxt.text = current.band == 0 ? "tan 45° = 6 / 6 = 1" : "계산 근거 확인 · 다음 계약 준비";
            }
            else
            {
                MgfSfx.Play("wrong", .34f);
                formulaTxt.text = current.formula;
                feedbackTxt.text = st.selectedLength + " m는 " + (st.selectedLength < current.answer ? "짧다" : "길다") + " · 정답 " + current.answer + " m";
                goalTxt.text = "식과 고스트 매듭을 보고 다시 감아라";
            }
        }

        void CompleteRevealVisual(bool correct)
        {
            for (int i = 0; i < ratchets.Length; i++) { ratchets[i].localScale = Vector3.one; ratchetRenderers[i].sharedMaterial = orangeMat; }
            capstanRoot.position = capstanBasePosition;
            cableLine.widthMultiplier = cableBraidA.widthMultiplier = cableBraidB.widthMultiplier = 1f;
            if (correct) MgfSfx.Play(st.solved >= 6 ? "win" : "pop", .32f);
        }

        void TickCable()
        {
            MgfSfx.Play("tap", .16f);
            handleRt.localScale = new Vector3(1.09f, .91f, 1);
        }

        void HandleGrab(bool held)
        {
            handleImg.color = held ? Cream : Orange;
            if (magnifierRt)
            {
                magnifierRt.gameObject.SetActive(held);
                if (held) UpdateMagnifier();
            }
            if (held) guideBoost = 0;
        }

        void UpdateCableTrail() { cableTrail.widthMultiplier = 1f + Mathf.Abs(Mathf.Sin(worldClock * 8f)) * .35f; }

        void RefreshProblemUi()
        {
            if (current == null) return;
            promptTxt.text = current.prompt;
            formulaTxt.text = DefinitionFor(current.template);
            rolesTxt.text = current.roles;
            UpdateDiagram();
            goalTxt.text = current.band == 0 ? "5 m 손잡이를 6 m까지 한 칸 끌어 놓으시오" : "계산한 길이만큼 케이블을 감아라";
            feedbackTxt.text = current.band == 0 ? "유령 손을 따라 6 m 눈금에서 놓으시오" : "주황 손잡이를 위아래로 끌어 놓으시오";
            practiceTargetRt.gameObject.SetActive(current.band == 0);
            guideFingerRt.gameObject.SetActive(current.band == 0);
            PositionHandle(false);
            RefreshHudImmediate();
        }

        string DefinitionFor(CableTemplate t)
        {
            if (t == CableTemplate.Sin30Opp || t == CableTemplate.Sin45Opp) return "sin A = 대변 / 빗변";
            if (t == CableTemplate.Cos60Adj || t == CableTemplate.Cos45Adj) return "cos A = 이웃변 / 빗변";
            return "tan A = 대변 / 이웃변";
        }

        void RefreshHudImmediate()
        {
            progressTxt.text = phase == Phase.Practice ? "연습 계약 · 예비 3"
                : "잠금 " + st.solved + "/6 · 첫 시도 " + st.firstAttemptCorrect
                    + " · 예비 " + Math.Max(0, st.lives);
            for (int i = 0; i < lifeStrands.Length; i++)
            {
                bool intact = i < st.lives;
                lifeStrands[i].color = intact ? Orange : new Color(Ink.r, Ink.g, Ink.b, .22f);
                lifeStrands[i].rectTransform.localRotation = Quaternion.Euler(0, 0, intact ? 0 : (i - 1) * 15f);
            }
        }

        void RefreshLengthUi(bool punch)
        {
            lengthTxt.text = st.selectedLength + " m";
            PositionHandle(punch);
            if (punch) lengthTxt.rectTransform.localScale = new Vector3(1.18f, 1.18f, 1);
        }

        float TrackHeight() => Mathf.Max(20, trackRt.rect.height);

        float LengthY(int length) => -TrackHeight() * .5f
            + (length - CableLockRules.MinLength) / (float)(CableLockRules.MaxLength - CableLockRules.MinLength) * TrackHeight();

        void PositionHandle(bool punch)
        {
            if (!handleRt || !trackRt) return;
            float y = LengthY(st.selectedLength);
            handleRt.anchoredPosition = new Vector2(0, y);
            cableFillImg.rectTransform.sizeDelta = new Vector2(10, y + TrackHeight() * .5f);
            if (practiceTargetRt) practiceTargetRt.anchoredPosition = new Vector2(0, LengthY(6));
            if (guideFingerRt && phase == Phase.Practice)
            {
                float u = (Mathf.Sin(worldClock * 4.2f) + 1f) * .5f;
                guideFingerRt.anchoredPosition = new Vector2(58, Mathf.Lerp(LengthY(5), LengthY(6), u));
            }
            UpdateMagnifier();
            if (punch) handleRt.localScale = new Vector3(1.12f, .88f, 1);
        }

        void UpdateMagnifier()
        {
            if (!magnifierRt) return;
            const float panelHalf = 66f;
            magnifierRt.anchoredPosition = new Vector2(-118,
                Mathf.Clamp(LengthY(st.selectedLength), -TrackHeight() * .5f + panelHalf, TrackHeight() * .5f - panelHalf));
            for (int i = 0; i < magnifierTxt.Length; i++)
            {
                int value = st.selectedLength + 2 - i;
                bool valid = value >= CableLockRules.MinLength && value <= CableLockRules.MaxLength;
                magnifierTxt[i].gameObject.SetActive(valid);
                if (valid) magnifierTxt[i].text = i == 2 ? "▶ " + value + " m" : value + " m";
            }
        }

        bool IsCableZone(Vector2 screen)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRt, screen, null, out var p)) return false;
            return Mathf.Abs(p.x) <= 105f && p.y >= -TrackHeight() * .5f - 45f && p.y <= TrackHeight() * .5f + 45f;
        }

        int LengthFromPointer(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRt, screen, null, out var p);
            float t = Mathf.Clamp01((p.y + TrackHeight() * .5f) / TrackHeight());
            int span = CableLockRules.MaxLength - CableLockRules.MinLength;
            return CableLockRules.MinLength + Mathf.RoundToInt(t * span);
        }

        float PointerLocalY(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRt, screen, null, out var p);
            return p.y;
        }

        int FineLengthFromPointer(Vector2 screen)
        {
            const float UnitsPerMetre = 22f;
            int delta = Mathf.RoundToInt((PointerLocalY(screen) - dragAnchorY) / UnitsPerMetre);
            return Mathf.Clamp(dragAnchorLength + delta, CableLockRules.MinLength, CableLockRules.MaxLength);
        }

        void ShowGhosts(int[] values)
        {
            if (ghostRt[0] == null) return;
            for (int i = 0; i < ghostRt.Length; i++)
            {
                int v = values[i];
                ghostRt[i].gameObject.SetActive(v > 0);
                if (v <= 0) continue;
                ghostRt[i].anchoredPosition = new Vector2(54 + i * 5, LengthY(v));
                ghostTxt[i].text = v + " m";
            }
        }

        void ReplayGuide(bool boosted)
        {
            guideBoost = boosted ? 1.4f : 1f;
            if (guideFingerRt) guideFingerRt.localScale = Vector3.one * guideBoost;
            ShowToast("손잡이를 5 m에서 6 m로 한 칸 끌어 놓으시오", 2.2f);
        }

        void RefuseInput(string msg)
        {
            wrongJolt = .65f; guideBoost = 1.35f;
            ShowToast(msg, 1.4f);
            MgfSfx.Play("wrong", .18f);
        }

        void ShowWrongReason(string id)
        {
            string msg = id == "swap_sin_cos" ? "기준각 A에서 대변과 이웃변을 다시 확인하시오"
                : id == "reciprocal_ratio" ? "tan 30°와 tan 60°의 역수 관계를 다시 확인하시오"
                : id == "multiply_instead_of_divide" ? "미지수가 분모 쪽이면 나눗셈을 확인하시오"
                : id == "cable_too_short" ? "고스트보다 늘릴 길이를 식에서 확인하시오"
                : "고스트보다 줄일 길이를 식에서 확인하시오";
            ShowToast(msg, 2.1f);
        }

        void ShowToast(string msg, float duration)
        {
            toastTxt.text = msg; toastT = duration; toastG.alpha = 1;
            toastRt.localScale = new Vector3(.94f, .94f, 1);
        }

        void SpawnTapRipple(Vector2 screen)
        {
            if (ripples[0] == null) return;
            var r = ripples[rippleNext]; rippleT[rippleNext] = 0; rippleNext = (rippleNext + 1) % ripples.Length;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRt, screen, null, out var local);
            r.rectTransform.anchoredPosition = local; r.rectTransform.localScale = Vector3.one * .35f;
            r.color = new Color(Blue.r, Blue.g, Blue.b, .55f);
        }

        void PressTitle()
        {
            ctaRt.localScale = new Vector3(.94f, .88f, 1);
            StartPractice();
        }

        void PressEnd()
        {
            endCtaRt.localScale = new Vector3(.94f, .88f, 1);
            StartPractice();
        }

        void ShowEnd(string reason)
        {
            bool clear = reason == "clear";
            bool precision = st.firstAttemptCorrect >= CableLockRules.PrecisionBonusFirst;
            endTitle.text = clear ? (precision ? "정밀 출항" : "드라이도크 출항") : "안전 브레이크 잠김";
            endCount.text = st.firstAttemptCorrect + " / 6";
            endStats.text = "첫 시도 정답  ·  점수 " + st.score;
            endHint.text = clear ? (precision
                    ? "여섯 해치를 잠갔다. 첫 시도 5개 이상 정밀 보너스 달성."
                    : "재계산을 포함해 여섯 해치를 잠갔다. 다음 출항은 첫 시도 5개에 도전하시오.")
                : "예비 케이블 3가닥이 끊어졌다. 기준각과 세 변의 역할을 다시 확인하시오.";
        }

        void SetScreen()
        {
            bool title = phase == Phase.Title, end = phase == Phase.End;
            titleG.alpha = title ? 1 : 0; titleG.blocksRaycasts = title;
            hudG.alpha = !title && !end ? 1 : 0; hudG.blocksRaycasts = false;
            endG.alpha = end ? 1 : 0; endG.blocksRaycasts = end;
            if (titleArt) titleArt.gameObject.SetActive(title);
            RefreshHudImmediate();
        }

        void UpdateUi(float dt)
        {
            if (displayedScore != st.score)
            {
                displayedScore = Mathf.MoveTowards(displayedScore, st.score, dt * 420f);
                int v = Mathf.RoundToInt(displayedScore);
                if (v != displayedScoreInt) { displayedScoreInt = v; scoreTxt.text = v.ToString(); }
            }
            scoreTxt.rectTransform.localScale = Vector3.Lerp(scoreTxt.rectTransform.localScale, Vector3.one, dt * 10f);
            lengthTxt.rectTransform.localScale = Vector3.Lerp(lengthTxt.rectTransform.localScale, Vector3.one, dt * 11f);
            handleRt.localScale = Vector3.Lerp(handleRt.localScale, Vector3.one, dt * 12f);
            ctaRt.localScale = Vector3.Lerp(ctaRt.localScale, Vector3.one * (1f + Mathf.Max(0, Mathf.Sin(worldClock * 2.3f)) * .018f), dt * 6f);
            endCtaRt.localScale = Vector3.Lerp(endCtaRt.localScale, Vector3.one, dt * 9f);
            if (titleArt && phase == Phase.Title)
            {
                float z = 1.02f + Mathf.Sin(worldClock * .42f) * .016f;
                titleArt.rectTransform.localScale = new Vector3(z, z, 1);
            }
            if (phase == Phase.Practice)
            {
                PositionHandle(false);
                float pulse = .76f + .24f * Mathf.Sin(worldClock * 5f);
                practiceTargetImg.color = new Color(Blue.r, Blue.g, Blue.b, pulse);
                guideFingerRt.localScale = Vector3.Lerp(guideFingerRt.localScale, Vector3.one * (guideBoost + .08f * Mathf.Sin(worldClock * 6f)), dt * 9f);
                guideBoost = Mathf.MoveTowards(guideBoost, 1f, dt * .55f);
            }
            if (toastT > 0)
            {
                toastT -= dt; toastG.alpha = Mathf.MoveTowards(toastG.alpha, toastT < .25f ? 0 : 1, dt * 7f);
                toastRt.localScale = Vector3.Lerp(toastRt.localScale, Vector3.one, dt * 11f);
            }
            else toastG.alpha = Mathf.MoveTowards(toastG.alpha, 0, dt * 7f);

            for (int i = 0; i < ripples.Length; i++)
            {
                if (rippleT[i] > 1) continue;
                rippleT[i] += dt * 2.8f;
                float t = Mathf.Clamp01(rippleT[i]);
                ripples[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(.35f, 1.7f, t);
                ripples[i].color = new Color(Blue.r, Blue.g, Blue.b, (1f - t) * .46f);
            }
        }
    }
}
