// 조개 합창 — 햇빛 든 조가비 소리 골목의 절차적 2.5D 월드와 UI.
using TMPro;
using Mgf;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.JogaeHapchang
{
    public partial class JogaeHapchangGame
    {
        static readonly Color Limestone = MgfLook.Hex("#315D5A");
        static readonly Color Ivory = MgfLook.Hex("#FFF4D9");
        static readonly Color Terracotta = MgfLook.Hex("#D96F52");
        static readonly Color Petrol = MgfLook.Hex("#173F43");
        static readonly Color Ink = MgfLook.Hex("#263536");
        static readonly Color Gold = MgfLook.Hex("#F1B34C");

        Camera cam;
        Transform worldRoot, mathRoot, titleClam, titleUpper, titleLower;
        Transform mainBody, mainEyeL, mainEyeR, baseLip, upperPivot, upperLip, handle, tickRoot, bridgeMarker;
        Transform[] audience = new Transform[8];
        Transform[] audienceUpper = new Transform[8];
        Vector3[] audienceBase = new Vector3[8];
        Transform[] trailDots = new Transform[6];
        int[] trailSlots = new int[6];
        Transform guideRingA, guideRingB, ghostHand, wrongBreath;
        LineRenderer circleLine, paLine, pbLine, oaLine, obLine, centerArc, wrongTrace, soundWaveL, soundWaveR, ghostPath;
        LineRenderer ocLine, knownArcLine, pcLine, pdLine;
        TextMeshPro pLabel, aLabel, bLabel, oLabel, cLabel, dLabel;
        Material limestoneMat, ivoryMat, terracottaMat, petrolMat, inkMat, goldMat, pearlMat, faintMat;
        readonly Vector3[] circlePoints = new Vector3[73];
        readonly Vector3[] waveLeftPoints = new Vector3[32];
        readonly Vector3[] waveRightPoints = new Vector3[32];
        const float CircleRadius = 2.70f;
        const float HandleRadius = 4.18f;
        static readonly Vector3 PivotLocal = new Vector3(-CircleRadius, 0, 0);
        static readonly Vector3 AnchorLocal = new Vector3(CircleRadius, 0, 0);

        RectTransform titleRt, hudRt, revealRt, endRt, promptRt, goalRt, angleRt, rippleRt;
        RectTransform ledgerRt, statusBandRt, revealShellRt, endShellRt;
        CanvasGroup titleG, hudG, revealG, endG, toastG;
        Image titlePearl, rippleImg;
        TextMeshProUGUI titleLogo, titleTag, titleBest;
        TextMeshProUGUI scoreTxt, progressTxt, firstTxt, livesTxt, masteryTxt, timeTxt, goalTxt, promptTxt, angleTxt;
        TextMeshProUGUI revealTitleTxt, revealFormulaTxt, revealHintTxt, toastTxt;
        TextMeshProUGUI endTitleTxt, endScoreTxt, endStatsTxt, endTapTxt;
        Sprite roundSprite, ringSprite, scallopSprite;
        int lastW, lastH, shownSecond = -1, shownScore;
        bool land;
        float ambientClock, guideClock, guideBoost, titlePress, openingAnim;
        float correctAnim, wrongAnim, waveProgress, hitStop, toastClock, rippleClock;
        float endCounter;

        void BuildWorld()
        {
            string stage = "materials";
            try
            {
                limestoneMat = MgfLook.Lit(Limestone, .18f, .02f);
                ivoryMat = MgfLook.Lit(Ivory, .58f, .02f);
                terracottaMat = MgfLook.Lit(Terracotta, .24f, .01f);
                petrolMat = MgfLook.Lit(Petrol, .32f, .05f);
                inkMat = MgfLook.Lit(Ink, .18f, .01f);
                goldMat = MgfLook.Lit(Gold, .42f, .04f, Gold * .08f);
                pearlMat = MgfLook.Lit(Ivory, .82f, .12f, Ivory * .05f);
                faintMat = MgfLook.Alpha(new Color(Ivory.r, Ivory.g, Ivory.b, .28f));
                stage = "lighting";
                MgfLook.Sky(MgfLook.Hex("#8EC4B9"), MgfLook.Hex("#4D817A"), MgfLook.Hex("#173F43"), .88f);
                MgfLook.Sun(new Vector3(42, -28, 0), MgfLook.Hex("#FFF0C2"), 1.28f, .62f);
                cam = MgfLook.Camera(new Vector3(0, 7.4f, -18f), new Vector3(0, .8f, 0), 38f);
                cam.orthographic = true; cam.orthographicSize = 8.2f; cam.backgroundColor = Limestone;
                worldRoot = new GameObject("SunlitShellAlley").transform;
                stage = "alley"; BuildAlley();
                stage = "title-clam"; BuildTitleClam();
                stage = "audience"; BuildAudience();
                stage = "math-layer"; BuildMathLayer();
            }
            catch (System.Exception ex)
            {
                throw new System.Exception("BuildWorld/" + stage, ex);
            }
        }

        void BuildAlley()
        {
            // 둥근 단색 블록 협곡 대신, 세로로 절개된 조가비의 방사 골과 진주막이 화면을 채운다.
            MgfLook.Block("DeepResonanceWall", new Vector3(0, .35f, 3.7f), new Vector3(22f, 13f, 2.2f), .12f, petrolMat, worldRoot);
            MgfLook.Block("PearlDustFloor", new Vector3(0, -4.20f, 1.8f), new Vector3(22f, .55f, 10f), .12f, limestoneMat, worldRoot);
            BuildShellChamber("LeftShellCut", new Vector3(-5.65f, .15f, .5f), 1.25f, -1);
            BuildShellChamber("RightShellCut", new Vector3(5.65f, .15f, .5f), 1.25f, 1);
            BuildShellChamber("UpperLeftShellCut", new Vector3(-4.65f, 5.0f, 1.1f), .82f, 1);
            BuildShellChamber("UpperRightShellCut", new Vector3(4.65f, 5.0f, 1.1f), .82f, -1);
            for (int i = 0; i < 24; i++)
            {
                float x = -8.7f + i * .76f, y = -3.78f + (i % 4) * .09f;
                var shard = MgfLook.Block("ChalkShard", new Vector3(x, y, -.25f + (i % 5) * .22f), new Vector3(.20f + (i % 3) * .08f, .09f, .18f), .025f, i % 6 == 0 ? goldMat : pearlMat, worldRoot);
                shard.transform.localRotation = Quaternion.Euler(i * 13f, i * 29f, i * 41f);
            }
        }

        void BuildShellChamber(string name, Vector3 pos, float scale, int mirror)
        {
            var root = new GameObject(name).transform; root.SetParent(worldRoot, false); root.localPosition = pos; root.localScale = Vector3.one * scale;
            var membrane = MgfLook.Prim(PrimitiveType.Sphere, "PearlMembrane", Vector3.zero, new Vector3(4.5f, 6.1f, .55f), pearlMat, root, false);
            membrane.transform.localRotation = Quaternion.Euler(0, mirror * 8f, mirror * 5f);
            for (int i = -6; i <= 6; i++)
            {
                float angle = i * 8.5f;
                float length = 5.5f - Mathf.Abs(i) * .16f;
                var pivot = new GameObject("RibPivot").transform; pivot.SetParent(root, false);
                pivot.localPosition = new Vector3(0, -2.55f, -.62f - Mathf.Abs(i) * .015f);
                pivot.localRotation = Quaternion.Euler(0, mirror * 4f, -angle);
                MgfLook.Block("RadiatingShellRib", new Vector3(0, length * .5f, 0), new Vector3(.22f, length, .26f), .035f, i % 2 == 0 ? ivoryMat : goldMat, pivot);
            }
            MgfLook.Prim(PrimitiveType.Sphere, "ResonanceMouth", new Vector3(0, -1.95f, -.92f), new Vector3(1.08f, .58f, .16f), petrolMat, root, false);
            for (int i = 0; i < 5; i++)
                MgfLook.Prim(PrimitiveType.Sphere, "NacreLayer", new Vector3(0, -2.58f + i * .22f, -.74f - i * .02f), new Vector3(2.35f - i * .18f, .16f, .18f), i % 2 == 0 ? pearlMat : ivoryMat, root, false);
        }

        void BuildTitleClam()
        {
            titleClam = new GameObject("TitleChoirClam").transform; titleClam.SetParent(worldRoot, false); titleClam.localPosition = new Vector3(0, -.05f, -.1f);
            MgfLook.Prim(PrimitiveType.Sphere, "TitleBody", new Vector3(0, .25f, .1f), new Vector3(3.1f, 2.7f, 1.45f), terracottaMat, titleClam, false);
            titleLower = MgfLook.Prim(PrimitiveType.Sphere, "TitleLowerShell", new Vector3(0, -.85f, -.15f), new Vector3(4.9f, 2.0f, 1.15f), ivoryMat, titleClam, false).transform;
            titleUpper = MgfLook.Prim(PrimitiveType.Sphere, "TitleUpperShell", new Vector3(0, 1.22f, .05f), new Vector3(4.9f, 2.1f, 1.12f), goldMat, titleClam, false).transform;
            for (int i = -3; i <= 3; i++)
            {
                MgfLook.Prim(PrimitiveType.Sphere, "TitleRidge", new Vector3(i * .58f, 1.35f + Mathf.Abs(i) * .10f, -.55f), new Vector3(.38f, 1.52f, .20f), ivoryMat, titleClam, false);
            }
            for (int i = -5; i <= 5; i++)
            {
                float length = 2.55f - Mathf.Abs(i) * .12f;
                var pivot = new GameObject("TitleRibPivot").transform; pivot.SetParent(titleClam, false);
                pivot.localPosition = new Vector3(0, -.05f, -.72f); pivot.localRotation = Quaternion.Euler(0, 0, -i * 7f);
                MgfLook.Block("TitleFanRib", new Vector3(0, length * .5f, 0), new Vector3(.18f, length, .16f), .025f, i % 2 == 0 ? ivoryMat : goldMat, pivot);
            }
            MgfLook.Prim(PrimitiveType.Sphere, "TitleEyeL", new Vector3(-.50f, .64f, -1.24f), new Vector3(.19f, .22f, .14f), petrolMat, titleClam, false);
            MgfLook.Prim(PrimitiveType.Sphere, "TitleEyeR", new Vector3(.50f, .64f, -1.24f), new Vector3(.19f, .22f, .14f), petrolMat, titleClam, false);
        }

        void BuildAudience()
        {
            Vector3[] pos = {
                new Vector3(-6.5f,-2.4f,.1f), new Vector3(-5.4f,.2f,.5f), new Vector3(-6.8f,2.7f,.9f), new Vector3(-4.9f,4.6f,1.0f),
                new Vector3(6.4f,-2.1f,.2f), new Vector3(5.3f,.5f,.6f), new Vector3(6.8f,2.8f,.9f), new Vector3(5.0f,4.7f,1.1f)
            };
            for (int i = 0; i < audience.Length; i++) { audience[i] = BuildSmallClam("ChoirClam" + (i + 1), pos[i], .68f + (i % 3) * .07f, i % 2 == 0 ? -18f : 18f, out audienceUpper[i]); audienceBase[i] = pos[i]; }
        }

        Transform BuildSmallClam(string name, Vector3 pos, float scale, float yaw, out Transform upper)
        {
            var root = new GameObject(name).transform; root.SetParent(worldRoot, false); root.localPosition = pos; root.localScale = Vector3.one * scale; root.localRotation = Quaternion.Euler(0, yaw, 0);
            MgfLook.Prim(PrimitiveType.Sphere, "Body", new Vector3(0, .24f, 0), new Vector3(1.35f, 1.05f, .92f), terracottaMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "LowerShell", new Vector3(0, -.28f, -.15f), new Vector3(1.72f, .74f, .70f), ivoryMat, root, false);
            upper = MgfLook.Prim(PrimitiveType.Sphere, "UpperShell", new Vector3(0, .78f, -.05f), new Vector3(1.72f, .72f, .68f), goldMat, root, false).transform;
            MgfLook.Prim(PrimitiveType.Sphere, "EyeL", new Vector3(-.26f, .48f, -.78f), new Vector3(.14f, .17f, .10f), petrolMat, root, false);
            MgfLook.Prim(PrimitiveType.Sphere, "EyeR", new Vector3(.26f, .48f, -.78f), new Vector3(.14f, .17f, .10f), petrolMat, root, false);
            return root;
        }

        void BuildMathLayer()
        {
            mathRoot = new GameObject("PearlGeometryMembrane").transform;
            mathRoot.position = new Vector3(0, .15f, -.7f); mathRoot.rotation = cam.transform.rotation;
            circleLine = MakeLine("CircleO", mathRoot, Ivory, .055f, 73);
            for (int i = 0; i < circlePoints.Length; i++)
            {
                float t = i * Mathf.PI * 2f / (circlePoints.Length - 1);
                circlePoints[i] = new Vector3(Mathf.Cos(t) * CircleRadius, Mathf.Sin(t) * CircleRadius, 0);
            }
            circleLine.SetPositions(circlePoints);
            paLine = MakeLine("PA", mathRoot, Ink, .045f, 2);
            pbLine = MakeLine("PB", mathRoot, Petrol, .070f, 2);
            oaLine = MakeLine("OA", mathRoot, new Color(Ivory.r, Ivory.g, Ivory.b, .72f), .035f, 2);
            obLine = MakeLine("OB", mathRoot, new Color(Ivory.r, Ivory.g, Ivory.b, .72f), .035f, 2);
            centerArc = MakeLine("CenterArc", mathRoot, Gold, .045f, 18);
            ocLine = MakeLine("SplitRadiusOC", mathRoot, Gold, .040f, 2);
            knownArcLine = MakeLine("KnownArcCD", mathRoot, Gold, .090f, 25);
            pcLine = MakeLine("KnownRayPC", mathRoot, new Color(Gold.r, Gold.g, Gold.b, .72f), .035f, 2);
            pdLine = MakeLine("KnownRayPD", mathRoot, new Color(Gold.r, Gold.g, Gold.b, .72f), .035f, 2);
            wrongTrace = MakeLine("WrongLipTrace", mathRoot, new Color(Terracotta.r, Terracotta.g, Terracotta.b, .44f), .10f, 2);
            soundWaveL = MakeLine("ArcSoundFoldA", mathRoot, Ivory, .10f, 32);
            soundWaveR = MakeLine("ArcSoundFoldB", mathRoot, Gold, .08f, 32);
            ghostPath = MakeLine("GhostDragPath", mathRoot, new Color(Ivory.r, Ivory.g, Ivory.b, .55f), .065f, 26);
            soundWaveL.enabled = soundWaveR.enabled = wrongTrace.enabled = centerArc.enabled = ghostPath.enabled = false;
            ocLine.enabled = knownArcLine.enabled = pcLine.enabled = pdLine.enabled = false;

            baseLip = BuildLip("FixedLowerLip", mathRoot, PivotLocal, 5.35f, ivoryMat, false);
            upperPivot = new GameObject("MovingUpperLipPivot").transform; upperPivot.SetParent(mathRoot, false); upperPivot.localPosition = PivotLocal;
            upperLip = BuildLip("MovingUpperLip", upperPivot, Vector3.zero, HandleRadius, goldMat, true);
            handle = MgfLook.Prim(PrimitiveType.Sphere, "ShellHandle", new Vector3(HandleRadius, 0, -.06f), new Vector3(.58f, .58f, .34f), pearlMat, upperPivot, true).transform;
            handle.gameObject.layer = 0;
            mainBody = MgfLook.Prim(PrimitiveType.Sphere, "MainClamBody", PivotLocal + new Vector3(-.25f, -.55f, .28f), new Vector3(1.45f, 1.22f, .75f), terracottaMat, mathRoot, false).transform;
            mainEyeL = MgfLook.Prim(PrimitiveType.Sphere, "MainEyeL", PivotLocal + new Vector3(-.48f, .16f, -.32f), new Vector3(.18f, .21f, .12f), petrolMat, mathRoot, false).transform;
            mainEyeR = MgfLook.Prim(PrimitiveType.Sphere, "MainEyeR", PivotLocal + new Vector3(.02f, .16f, -.32f), new Vector3(.18f, .21f, .12f), petrolMat, mathRoot, false).transform;

            SetLine(paLine, PivotLocal, AnchorLocal);
            SetLine(oaLine, Vector3.zero, AnchorLocal);
            SetSelectedGeometry(0);
            pLabel = AddMathLabel("P", PivotLocal + new Vector3(-.30f, -.36f, 0), .36f, Ink);
            aLabel = AddMathLabel("A", AnchorLocal + new Vector3(.30f, -.12f, 0), .36f, Ink);
            bLabel = AddMathLabel("B", BForSlot(0) + new Vector3(.26f, .18f, 0), .36f, Ink);
            oLabel = AddMathLabel("O", new Vector3(.0f, -.34f, 0), .36f, Ink);
            cLabel = MgfText.World("C", Vector3.zero, .30f, Gold, mathRoot); cLabel.fontStyle = FontStyles.Bold; cLabel.gameObject.SetActive(false);
            dLabel = MgfText.World("D", Vector3.zero, .30f, Gold, mathRoot); dLabel.fontStyle = FontStyles.Bold; dLabel.gameObject.SetActive(false);

            tickRoot = new GameObject("AngleTicks").transform; tickRoot.SetParent(mathRoot, false);
            for (int slot = 0; slot < JogaeRules.SlotCount; slot++)
            {
                float a = JogaeRules.AngleForSlot(slot) * Mathf.Deg2Rad;
                Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                var tick = MgfLook.Prim(PrimitiveType.Sphere, "AngleTick" + slot, PivotLocal + d * 3.58f, new Vector3(.10f, .10f, .08f), slot % 4 == 0 ? petrolMat : ivoryMat, tickRoot, false);
                tick.transform.localScale *= slot % 4 == 0 ? 1.35f : 1f;
                if (slot == 0 || slot == 3 || slot == 7 || slot == 11 || slot == 15)
                {
                    var label = AddMathLabel(JogaeRules.AngleForSlot(slot) + "°", PivotLocal + d * 3.88f, .22f, Ink);
                    label.transform.SetParent(tickRoot, true);
                }
            }

            guideRingA = RingObject("GuideRippleA", handle, .84f, Ivory);
            guideRingB = RingObject("GuideRippleB", handle, 1.08f, Gold);
            ghostHand = MgfLook.Prim(PrimitiveType.Sphere, "GhostHand", PivotLocal, new Vector3(.34f, .34f, .12f), MgfLook.Alpha(new Color(Ivory.r, Ivory.g, Ivory.b, .66f)), mathRoot, false).transform;
            wrongBreath = MgfLook.Prim(PrimitiveType.Sphere, "LeakingBreath", PivotLocal, new Vector3(.30f, .15f, .12f), MgfLook.Alpha(new Color(Ivory.r, Ivory.g, Ivory.b, .78f)), mathRoot, false).transform;
            wrongBreath.gameObject.SetActive(false);
            bridgeMarker = MgfLook.Prim(PrimitiveType.Sphere, "MovablePointP", PivotLocal, new Vector3(.34f, .34f, .18f), terracottaMat, mathRoot, false).transform;
            bridgeMarker.gameObject.SetActive(false);
            for (int i = 0; i < trailDots.Length; i++)
            {
                trailDots[i] = MgfLook.Prim(PrimitiveType.Sphere, "PearlTrail" + i, PivotLocal, Vector3.one * (.16f - i * .015f), MgfLook.Alpha(new Color(Ivory.r, Ivory.g, Ivory.b, .36f - i * .035f)), mathRoot, false).transform;
                trailDots[i].gameObject.SetActive(false); trailSlots[i] = 0;
            }
        }

        Transform BuildLip(string name, Transform parent, Vector3 pos, float length, Material mat, bool ridges)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); root.localPosition = pos;
            var bar = MgfLook.Block("ChalkyShellEdge", new Vector3(length * .5f, 0, 0), new Vector3(length, .30f, .32f), .14f, mat, root);
            bar.transform.localRotation = Quaternion.identity;
            if (ridges)
                for (int i = 1; i < 7; i++) MgfLook.Prim(PrimitiveType.Sphere, "LipRidge", new Vector3(length * i / 7f, .02f, -.12f), new Vector3(.22f, .42f, .16f), i % 2 == 0 ? ivoryMat : goldMat, root, false);
            return root;
        }

        LineRenderer MakeLine(string name, Transform parent, Color color, float width, int count)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(parent, false);
            line.useWorldSpace = false; line.positionCount = count; line.widthMultiplier = width;
            line.sharedMaterial = MgfLook.Unlit(color); line.numCapVertices = 4; line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false; return line;
        }

        Transform RingObject(string name, Transform parent, float size, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = Vector3.zero;
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true; line.positionCount = 32; line.widthMultiplier = .055f; line.sharedMaterial = MgfLook.Unlit(color); line.numCornerVertices = 2;
            var pts = new Vector3[32]; for (int i = 0; i < pts.Length; i++) { float a = i * Mathf.PI * 2f / pts.Length; pts[i] = new Vector3(Mathf.Cos(a) * size, Mathf.Sin(a) * size, -.08f); } line.SetPositions(pts);
            return go.transform;
        }

        TextMeshPro AddMathLabel(string text, Vector3 pos, float size, Color color)
        {
            var t = MgfText.World(text, pos, size, color, mathRoot); t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center; return t;
        }

        void SetLine(LineRenderer line, Vector3 a, Vector3 b)
        {
            line.SetPosition(0, a); line.SetPosition(1, b);
        }

        Vector3 DirectionForSlot(int slot)
        {
            float a = JogaeRules.AngleForSlot(slot) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
        }

        Vector3 BForSlot(int slot)
        {
            float a = JogaeRules.AngleForSlot(slot) * 2f * Mathf.Deg2Rad;
            return new Vector3(CircleRadius * Mathf.Cos(a), CircleRadius * Mathf.Sin(a), 0);
        }

        void SetSelectedGeometry(int slot)
        {
            slot = Mathf.Clamp(slot, 0, JogaeRules.SlotCount - 1);
            Vector3 b = BForSlot(slot);
            upperPivot.localRotation = Quaternion.Euler(0, 0, JogaeRules.AngleForSlot(slot));
            SetLine(pbLine, PivotLocal, b); SetLine(obLine, Vector3.zero, b);
            if (bLabel) bLabel.transform.localPosition = b + new Vector3(.26f, .18f, 0);
            DrawCenterArc(slot, false);
        }

        void DrawCenterArc(int slot, bool enabled)
        {
            int count = centerArc.positionCount;
            float end = JogaeRules.AngleForSlot(slot) * 2f * Mathf.Deg2Rad;
            for (int i = 0; i < count; i++)
            {
                float a = end * i / (count - 1);
                centerArc.SetPosition(i, new Vector3(Mathf.Cos(a) * .70f, Mathf.Sin(a) * .70f, -.04f));
            }
            centerArc.enabled = enabled;
        }

        void BuildUi()
        {
            roundSprite = MakeSprite(96, 19, false, false); ringSprite = MakeSprite(96, 8, true, false); scallopSprite = MakeSprite(96, 22, false, true);
            var canvas = MgfText.Canvas;
            titleRt = R("TitleScreen", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            var titleShell = Img(titleRt, "EngravedShellSign", new Vector2(.5f, .70f), Vector2.zero, new Vector2(346, 200), new Color(Ivory.r, Ivory.g, Ivory.b, .94f), scallopSprite);
            var logoShadow = Txt(titleShell.transform, "조개\n합창", new Vector2(.5f, .58f), new Vector2(4, -5), 58, new Color(Ink.r, Ink.g, Ink.b, .65f), 330); logoShadow.fontStyle = FontStyles.Bold;
            titleLogo = Txt(titleShell.transform, "조개\n합창", new Vector2(.5f, .58f), Vector2.zero, 58, Terracotta, 330); titleLogo.fontStyle = FontStyles.Bold; titleLogo.outlineColor = Ivory; titleLogo.outlineWidth = .20f;
            titleTag = Txt(titleShell.transform, "조개 입을 벌려라", new Vector2(.5f, .13f), Vector2.zero, 17, Petrol, 320); titleTag.fontStyle = FontStyles.Bold;
            var meta = Txt(titleRt, "중학교 3학년 · 원주각 · 첫 시도 6/8", new Vector2(.5f, .54f), Vector2.zero, 14, Ink, 350); meta.fontStyle = FontStyles.Bold;
            titleBest = Txt(titleRt, "", new Vector2(.5f, .245f), Vector2.zero, 14, Ink, 320); titleBest.fontStyle = FontStyles.Bold;
            var pearl = Img(titleRt, "StartPearl", new Vector2(.5f, .135f), Vector2.zero, new Vector2(310, 82), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), scallopSprite); titlePearl = pearl;
            var start = Txt(pearl.transform, "중앙 조개를 눌러 합창 시작", new Vector2(.5f, .5f), Vector2.zero, 20, Petrol, 294); start.fontStyle = FontStyles.Bold;

            hudRt = R("GameHud", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); hudG = hudRt.gameObject.AddComponent<CanvasGroup>();
            ledgerRt = Img(hudRt, "PearlLedger", new Vector2(.46f, .958f), Vector2.zero, new Vector2(350, 58), new Color(Ivory.r, Ivory.g, Ivory.b, .96f), scallopSprite).rectTransform;
            scoreTxt = Txt(ledgerRt, "0", new Vector2(.12f, .54f), Vector2.zero, 19, Terracotta, 60); scoreTxt.fontStyle = FontStyles.Bold;
            progressTxt = Txt(ledgerRt, "소절 0/8", new Vector2(.37f, .54f), Vector2.zero, 14, Ink, 92); progressTxt.fontStyle = FontStyles.Bold;
            firstTxt = Txt(ledgerRt, "첫 0/0", new Vector2(.64f, .54f), Vector2.zero, 13, Petrol, 92); firstTxt.fontStyle = FontStyles.Bold;
            timeTxt = Txt(ledgerRt, "90초", new Vector2(.88f, .54f), Vector2.zero, 13, Ink, 58); timeTxt.fontStyle = FontStyles.Bold;
            statusBandRt = Img(hudRt, "FailureBudgetBand", new Vector2(.46f, .914f), Vector2.zero, new Vector2(360, 40), new Color(Petrol.r, Petrol.g, Petrol.b, .88f), roundSprite).rectTransform;
            livesTxt = Txt(hudRt, "숨 ●●●●", new Vector2(.20f, .914f), Vector2.zero, 14, Terracotta, 145); livesTxt.fontStyle = FontStyles.Bold;
            masteryTxt = Txt(hudRt, "첫 시도 6/8 필요 · 허용 실수 2", new Vector2(.67f, .914f), Vector2.zero, 12, Ivory, 238); masteryTxt.fontStyle = FontStyles.Bold;
            goalRt = Img(hudRt, "GoalLine", new Vector2(.5f, .875f), Vector2.zero, new Vector2(354, 44), new Color(Petrol.r, Petrol.g, Petrol.b, .95f), roundSprite).rectTransform;
            goalTxt = Txt(goalRt, "입을 맞게 벌리면 조개들이 노래한다", new Vector2(.5f, .5f), Vector2.zero, 14, Ivory, 338); goalTxt.fontStyle = FontStyles.Bold;
            promptRt = Img(hudRt, "ConditionPlaque", new Vector2(.5f, .795f), Vector2.zero, new Vector2(360, 92), new Color(Ivory.r, Ivory.g, Ivory.b, .97f), scallopSprite).rectTransform;
            promptTxt = Txt(promptRt, "", new Vector2(.5f, .5f), Vector2.zero, 14, Ink, 338); promptTxt.fontStyle = FontStyles.Bold; promptTxt.textWrappingMode = TextWrappingModes.Normal;
            angleRt = R("CurrentAnglePearl", hudRt, new Vector2(.53f, .235f), Vector2.zero, new Vector2(104, 58));
            var angleBg = angleRt.gameObject.AddComponent<Image>(); angleBg.sprite = scallopSprite; angleBg.color = new Color(Ivory.r, Ivory.g, Ivory.b, .96f); angleBg.raycastTarget = false;
            angleTxt = Txt(angleRt, "10°", new Vector2(.5f, .55f), Vector2.zero, 24, Petrol, 96); angleTxt.fontStyle = FontStyles.Bold;

            revealRt = R("MathReveal", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); revealG = revealRt.gameObject.AddComponent<CanvasGroup>();
            revealShellRt = Img(revealRt, "RevealShell", new Vector2(.5f, .19f), Vector2.zero, new Vector2(356, 156), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), scallopSprite).rectTransform;
            revealTitleTxt = Txt(revealShellRt, "", new Vector2(.5f, .72f), Vector2.zero, 22, Petrol, 334); revealTitleTxt.fontStyle = FontStyles.Bold;
            revealFormulaTxt = Txt(revealShellRt, "", new Vector2(.5f, .46f), Vector2.zero, 17, Ink, 334); revealFormulaTxt.fontStyle = FontStyles.Bold;
            revealHintTxt = Txt(revealShellRt, "", new Vector2(.5f, .19f), Vector2.zero, 12, Terracotta, 332); revealHintTxt.fontStyle = FontStyles.Bold;

            var toastRt = R("Toast", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); toastG = toastRt.gameObject.AddComponent<CanvasGroup>();
            Img(toastRt, "ToastShell", new Vector2(.5f, .27f), Vector2.zero, new Vector2(338, 54), new Color(Ink.r, Ink.g, Ink.b, .93f), roundSprite);
            toastTxt = Txt(toastRt, "", new Vector2(.5f, .27f), Vector2.zero, 13, Ivory, 322); toastTxt.fontStyle = FontStyles.Bold;
            rippleRt = R("InputRipple", hudRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(78, 78)); rippleImg = rippleRt.gameObject.AddComponent<Image>(); rippleImg.sprite = ringSprite; rippleImg.color = Gold; rippleImg.raycastTarget = false; rippleRt.gameObject.SetActive(false);

            endRt = R("EndScreen", canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844)); endG = endRt.gameObject.AddComponent<CanvasGroup>();
            endShellRt = Img(endRt, "ResultShell", new Vector2(.5f, .50f), Vector2.zero, new Vector2(358, 500), new Color(Ivory.r, Ivory.g, Ivory.b, .98f), scallopSprite).rectTransform;
            endTitleTxt = Txt(endShellRt, "", new Vector2(.5f, .76f), Vector2.zero, 34, Terracotta, 332); endTitleTxt.fontStyle = FontStyles.Bold;
            endScoreTxt = Txt(endShellRt, "0", new Vector2(.5f, .57f), Vector2.zero, 58, Petrol, 330); endScoreTxt.fontStyle = FontStyles.Bold;
            endStatsTxt = Txt(endShellRt, "", new Vector2(.5f, .38f), Vector2.zero, 18, Ink, 318); endStatsTxt.fontStyle = FontStyles.Bold;
            var again = Img(endShellRt, "AgainPearl", new Vector2(.5f, .16f), Vector2.zero, new Vector2(298, 76), Petrol, scallopSprite);
            endTapTxt = Txt(again.transform, "다시 합창하기", new Vector2(.5f, .5f), Vector2.zero, 22, Ivory, 282); endTapTxt.fontStyle = FontStyles.Bold;
            SetScreen();
        }

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = offset; rt.sizeDelta = size; return rt;
        }

        Image Img(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size, Color color, Sprite sprite)
        {
            var rt = R(name, parent, anchor, offset, size); var image = rt.gameObject.AddComponent<Image>(); image.sprite = sprite; image.color = color; image.raycastTarget = false; return image;
        }

        TextMeshProUGUI Txt(Transform parent, string text, Vector2 anchor, Vector2 offset, float size, Color color, float width)
        {
            var rt = R("Text", parent, anchor, offset, new Vector2(width, size * 2.8f)); var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.text = text; t.textWrappingMode = TextWrappingModes.Normal; return t;
        }

        Sprite MakeSprite(int size, int radius, bool ring, bool scallop)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "JogaeUiShape", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size]; float c = (size - 1) * .5f;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c, ny = (y - c) / c;
                float ax = Mathf.Abs(x - c), ay = Mathf.Abs(y - c);
                float dx = Mathf.Max(0, ax - (c - radius)), dy = Mathf.Max(0, ay - (c - radius));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                bool inside = dist <= radius + .5f;
                if (scallop)
                {
                    float rr = Mathf.Sqrt(nx * nx + ny * ny);
                    float ang = Mathf.Atan2(ny, nx);
                    float edge = .94f + .045f * Mathf.Sin(ang * 12f);
                    inside = rr <= edge;
                }
                byte a = inside ? (byte)255 : (byte)0;
                if (ring && inside)
                {
                    float innerDx = Mathf.Max(0, ax - (c - radius - 5)), innerDy = Mathf.Max(0, ay - (c - radius - 5));
                    if (Mathf.Sqrt(innerDx * innerDx + innerDy * innerDy) <= radius - 5) a = 0;
                }
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        void ResetRunVisuals()
        {
            correctAnim = wrongAnim = waveProgress = hitStop = toastClock = rippleClock = 0f;
            guideClock = guideBoost = titlePress = openingAnim = 0f; endCounter = 0f; shownScore = 0; shownSecond = -1;
            if (wrongTrace) wrongTrace.enabled = false;
            if (soundWaveL) soundWaveL.enabled = soundWaveR.enabled = false;
            if (centerArc) centerArc.enabled = false;
            if (wrongBreath) wrongBreath.gameObject.SetActive(false);
            if (ghostHand) ghostHand.gameObject.SetActive(false);
            if (ghostPath) ghostPath.enabled = false;
            if (bridgeMarker) bridgeMarker.gameObject.SetActive(false);
            if (baseLip && upperPivot && mainBody && tickRoot) SetStandardGeometryVisible(true);
            for (int i = 0; i < trailDots.Length; i++) if (trailDots[i]) trailDots[i].gameObject.SetActive(false);
        }

        void PrepareProblemVisual(ShellProblem p, bool practice)
        {
            SetStandardGeometryVisible(true);
            st.selectedSlot = -1; st.selectedAngle = 0;
            practiceFreeGrab = false; practiceTapArmed = false; practiceAssistMoving = false; practiceAssistClock = 0f;
            upperPivot.localRotation = Quaternion.identity;
            SetLine(pbLine, PivotLocal, AnchorLocal); SetLine(obLine, Vector3.zero, AnchorLocal);
            bLabel.transform.localPosition = AnchorLocal + new Vector3(.26f, .18f, 0);
            wrongTrace.enabled = false; centerArc.enabled = false; soundWaveL.enabled = soundWaveR.enabled = false;
            ocLine.enabled = knownArcLine.enabled = pcLine.enabled = pdLine.enabled = false;
            cLabel.gameObject.SetActive(false); dLabel.gameObject.SetActive(false);
            wrongBreath.gameObject.SetActive(false); guideClock = 0f; guideBoost = practice ? 1f : 0f;
            ghostHand.gameObject.SetActive(practice); ghostPath.enabled = practice;
            for (int i = 0; i < trailDots.Length; i++) trailDots[i].gameObject.SetActive(false);
            if (!practice && p.kind == ShellProblemKind.SplitReverse) ShowSplitCondition(p);
            if (!practice && p.kind == ShellProblemKind.ArcRatio) ShowArcRatioCondition(p);
            if (practice) UpdateGhostPath();
        }

        void PrepareInvariantBridge()
        {
            SetStandardGeometryVisible(false);
            bridgeMarker.gameObject.SetActive(true);
            ghostHand.gameObject.SetActive(false); ghostPath.enabled = false; wrongTrace.enabled = false;
            centerArc.enabled = false; soundWaveL.enabled = soundWaveR.enabled = false;
            ocLine.enabled = knownArcLine.enabled = pcLine.enabled = pdLine.enabled = false;
            cLabel.gameObject.SetActive(false); dLabel.gameObject.SetActive(false);
            UpdateInvariantBridge(Screen.width * .32f);
            goalTxt.text = "점 P를 끌어도 40°가 유지되는지 확인하시오";
            promptTxt.text = "같은 호 AB에 대한 원주각은 점 P의 위치가 달라도 같습니다.";
            angleTxt.text = "40°";
            RefreshHudImmediate();
        }

        void UpdateInvariantBridge(float screenX)
        {
            float t = Mathf.InverseLerp(Screen.width * .15f, Screen.width * .85f, screenX);
            float angle = Mathf.Lerp(195f, 325f, t) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(angle) * CircleRadius, Mathf.Sin(angle) * CircleRadius, 0);
            Vector3 a = AnchorLocal;
            Vector3 b = new Vector3(Mathf.Cos(80f * Mathf.Deg2Rad) * CircleRadius, Mathf.Sin(80f * Mathf.Deg2Rad) * CircleRadius, 0);
            SetLine(paLine, p, a); SetLine(pbLine, p, b); SetLine(oaLine, Vector3.zero, a); SetLine(obLine, Vector3.zero, b);
            pLabel.transform.localPosition = p + new Vector3(-.22f, -.26f, 0);
            bridgeMarker.localPosition = p;
            aLabel.transform.localPosition = a + new Vector3(.30f, -.12f, 0);
            bLabel.transform.localPosition = b + new Vector3(.22f, .22f, 0);
            oLabel.transform.localPosition = new Vector3(0, -.34f, 0);
        }

        void RestoreAfterInvariantBridge()
        {
            bridgeMarker.gameObject.SetActive(false);
            SetStandardGeometryVisible(true);
            pLabel.transform.localPosition = PivotLocal + new Vector3(-.30f, -.36f, 0);
            aLabel.transform.localPosition = AnchorLocal + new Vector3(.30f, -.12f, 0);
            oLabel.transform.localPosition = new Vector3(0, -.34f, 0);
            SetSelectedGeometry(0);
        }

        void SetStandardGeometryVisible(bool visible)
        {
            baseLip.gameObject.SetActive(visible); upperPivot.gameObject.SetActive(visible);
            mainBody.gameObject.SetActive(visible); mainEyeL.gameObject.SetActive(visible); mainEyeR.gameObject.SetActive(visible);
            tickRoot.gameObject.SetActive(visible);
        }

        void ShowSplitCondition(ShellProblem p)
        {
            float a = p.splitX * Mathf.Deg2Rad;
            Vector3 c = new Vector3(Mathf.Cos(a) * CircleRadius, Mathf.Sin(a) * CircleRadius, -.03f);
            SetLine(ocLine, Vector3.zero, c); ocLine.enabled = true;
            cLabel.transform.localPosition = c + new Vector3(.18f, .18f, 0); cLabel.gameObject.SetActive(true);
        }

        void ShowArcRatioCondition(ShellProblem p)
        {
            float start = 200f * Mathf.Deg2Rad;
            float known = (float)p.knownAngleN / p.knownAngleD;
            float end = (200f + known * 2f) * Mathf.Deg2Rad;
            for (int i = 0; i < knownArcLine.positionCount; i++)
            {
                float a = Mathf.Lerp(start, end, i / (float)(knownArcLine.positionCount - 1));
                knownArcLine.SetPosition(i, new Vector3(Mathf.Cos(a) * CircleRadius, Mathf.Sin(a) * CircleRadius, -.04f));
            }
            Vector3 c = new Vector3(Mathf.Cos(start) * CircleRadius, Mathf.Sin(start) * CircleRadius, -.03f);
            Vector3 d = new Vector3(Mathf.Cos(end) * CircleRadius, Mathf.Sin(end) * CircleRadius, -.03f);
            SetLine(pcLine, PivotLocal, c); SetLine(pdLine, PivotLocal, d);
            knownArcLine.enabled = pcLine.enabled = pdLine.enabled = true;
            cLabel.transform.localPosition = c + new Vector3(-.16f, -.22f, 0); dLabel.transform.localPosition = d + new Vector3(.18f, -.18f, 0);
            cLabel.gameObject.SetActive(true); dLabel.gameObject.SetActive(true);
        }

        void RefreshProblemUi()
        {
            if (current == null) return;
            promptTxt.text = current.prompt;
            angleTxt.text = st.selectedAngle > 0 ? st.selectedAngle + "°" : "-";
            if (flow == Flow.Practice)
                goalTxt.text = "윗입술을 40° 눈금까지 끌어서 놓으시오";
            else if (current.kind == ShellProblemKind.Direct)
                goalTxt.text = "중심각의 절반으로 조개 입을 벌리시오";
            else if (current.kind == ShellProblemKind.ErrorFind)
                goalTxt.text = "잘못 적힌 값을 입술 각으로 고치시오";
            else if (current.kind == ShellProblemKind.SplitReverse)
                goalTxt.text = "두 중심각을 더한 뒤 절반을 구성하시오";
            else goalTxt.text = "호의 길이 비와 같은 각의 비를 구성하시오";
            RefreshHudImmediate();
        }

        void RefreshHudImmediate()
        {
            progressTxt.text = "소절 " + st.solved + "/8";
            firstTxt.text = "첫 " + st.firstCorrect + "/" + st.firstAttempted;
            string dots = ""; for (int i = 0; i < JogaeRules.StartLives; i++) dots += i < st.lives ? "●" : "○";
            livesTxt.text = "숨 " + dots;
            int allowed = Mathf.Max(0, JogaeRules.AllowedFirstMistakes - (st.firstAttempted - st.firstCorrect));
            masteryTxt.text = "첫 시도 6/8 필요 · 허용 실수 " + allowed;
        }

        void BeginTitleSplit()
        {
            titlePress = 1f; openingAnim = 0f;
            MgfFx.Punch(titleClam, .09f, .32f);
        }

        void PulseTitleClam()
        {
            titlePress = 1f; MgfFx.Punch(titlePearl.transform, .12f, .24f); MgfSfx.Play("pop", .28f);
        }

        void BeginDragVisual()
        {
            guideBoost = 0f; ghostHand.gameObject.SetActive(false); ghostPath.enabled = false;
            mainBody.localScale = new Vector3(1.12f, .92f, 1f);
            MgfFx.Punch(handle, .18f, .20f);
        }

        void UpdateSelectedVisual(int slot)
        {
            for (int i = trailDots.Length - 1; i > 0; i--) trailSlots[i] = trailSlots[i - 1];
            trailSlots[0] = slot;
            for (int i = 0; i < trailDots.Length; i++)
            {
                Vector3 d = DirectionForSlot(trailSlots[i]); trailDots[i].localPosition = PivotLocal + d * HandleRadius + new Vector3(0, 0, .05f + i * .01f);
                trailDots[i].gameObject.SetActive(i < 4);
            }
            SetSelectedGeometry(slot); angleTxt.text = JogaeRules.AngleForSlot(slot) + "°";
            mainBody.localScale = new Vector3(1.12f + slot * .008f, .92f - slot * .006f, 1f);
        }

        void EndDragWithoutSubmit()
        {
            mainBody.localScale = Vector3.one;
        }

        void ArmPracticeTapAssist()
        {
            practiceTapArmed = true; guideClock = 0f; guideBoost = 1f;
            ghostHand.gameObject.SetActive(true); ghostPath.enabled = true; UpdateGhostPath();
            goalTxt.text = "손잡이를 붙잡았습니다 · 빛나는 40° 지점을 누르시오";
            toastTxt.text = "한 번 더 누르면 40°까지 입술이 움직입니다"; toastClock = 1.8f;
            MgfFx.Punch(handle, .16f, .22f); MgfSfx.Play("tap", .18f);
        }

        void BeginPracticeTapMove()
        {
            practiceAssistMoving = true; practiceAssistClock = 0f; st.dragging = true;
            ghostHand.gameObject.SetActive(false); guideRingA.gameObject.SetActive(false); guideRingB.gameObject.SetActive(false);
            goalTxt.text = "40° 눈금까지 조개 입술이 움직이는 중";
            MgfSfx.Play("whoosh", .24f);
        }

        void BeginRevealVisual(bool correct, bool practice, ShellProblem p, int slot, string misconception)
        {
            mainBody.localScale = Vector3.one; DrawCenterArc(slot, true);
            wrongTrace.enabled = !correct; if (!correct) SetLine(wrongTrace, PivotLocal, PivotLocal + DirectionForSlot(slot) * HandleRadius);
            if (correct)
            {
                correctAnim = .0001f; waveProgress = 0f; hitStop = .085f;
                soundWaveL.enabled = soundWaveR.enabled = true;
                MgfFx.Glow(mathRoot.TransformPoint(BForSlot(slot)), Ivory, practice ? 12 : 18, .34f);
                MgfSfx.Play("correct", .42f);
                revealTitleTxt.text = practice ? "첫 마디가 열렸습니다" : "호가 울렸습니다";
                revealFormulaTxt.text = "중심각 " + p.centerAngle + "° = 원주각 " + p.answerAngle + "° × 2";
                revealHintTxt.text = practice ? "같은 호에 대한 원주각은 중심각의 절반입니다." : (firstAttempt ? "첫 시도 정답" : "입술 자국을 보고 바로잡았습니다.");
            }
            else
            {
                wrongAnim = .0001f; wrongBreath.gameObject.SetActive(true); MgfSfx.Play("wrong", .36f);
                int chosenCenter = JogaeRules.AngleForSlot(slot) * 2;
                int deltaSlot = Mathf.Abs(slot - p.answerSlot);
                string direction = slot > p.answerSlot ? deltaSlot + "눈금 큼" : deltaSlot + "눈금 작음";
                revealTitleTxt.text = practice ? "숨이 샜습니다" : "입술 각을 다시 맞추시오";
                int possible = st.firstCorrect + st.remainingUnattempted;
                revealFormulaTxt.text = "현재 중심각 " + chosenCenter + "° · 목표 " + p.centerAngle + "° · " + direction;
                revealHintTxt.text = possible < JogaeRules.MasteryFirstCorrect
                    ? "통과 가능 " + possible + "/8 — 첫 시도 6개가 불가능해 이번 판이 끝납니다."
                    : JogaeRules.MisconceptionMessage(misconception) + " · 허용 실수 " + st.allowedFirstMistakes;
            }
            RefreshHudImmediate();
        }

        void CompleteRevealVisual(bool correct)
        {
            soundWaveL.enabled = soundWaveR.enabled = false; centerArc.enabled = false; wrongBreath.gameObject.SetActive(false);
            if (correct) MgfFx.Punch(mainBody, .16f, .28f);
        }

        void ShowWrongReason(string misconception)
        {
            toastTxt.text = JogaeRules.MisconceptionMessage(misconception); toastClock = 2.6f;
        }

        void ShowEndVisual(string reason)
        {
            endCounter = 0f;
            if (reason == "clear")
            {
                endTitleTxt.text = "여덟 조개 합창";
                endStatsTxt.text = "첫 시도 " + st.firstCorrect + "/8\n숨구슬 " + st.lives + "개 남음";
                endTapTxt.text = "다시 합창하기";
                correctAnim = .0001f; waveProgress = 0f; soundWaveL.enabled = soundWaveR.enabled = true;
                for (int i = 0; i < audience.Length; i++) MgfFx.Glow(audience[i].position + Vector3.up, i % 2 == 0 ? Ivory : Gold, 5, .28f);
            }
            else
            {
                endTitleTxt.text = reason == "timer" ? "소리가 멎었습니다" : reason == "mastery" ? "첫 마디 기준 미달" : "숨구슬이 비었습니다";
                endStatsTxt.text = "완성 " + st.solved + "/8\n첫 시도 " + st.firstCorrect + "/" + st.firstAttempted;
                endTapTxt.text = "다시 맞춰 보기";
            }
        }

        void RefuseInput(string message)
        {
            toastTxt.text = message; toastClock = 1.15f; rippleClock = .42f; rippleRt.gameObject.SetActive(true);
            Vector2 logical = ScreenToCanvas(MgfPointer.Position); rippleRt.anchoredPosition = logical;
            wrongAnim = Mathf.Max(wrongAnim, .0001f); MgfFx.Punch(handle, .16f, .22f);
        }

        void BoostGuide()
        {
            guideBoost = 1f; guideClock = 0f; ghostHand.gameObject.SetActive(true); ghostPath.enabled = true; UpdateGhostPath();
        }

        void UpdateGhostPath()
        {
            int count = ghostPath.positionCount; float end = 40f * Mathf.Deg2Rad;
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(0f, end, i / (float)(count - 1));
                ghostPath.SetPosition(i, PivotLocal + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * HandleRadius + new Vector3(0, 0, .08f));
            }
        }

        bool PointerNearHandle(Vector2 screen)
        {
            Vector3 hs = cam.WorldToScreenPoint(handle.position);
            float radius = Mathf.Max(72f, Screen.width * .15f);
            return Vector2.Distance(screen, new Vector2(hs.x, hs.y)) <= radius;
        }

        int SlotFromPointer(Vector2 screen)
        {
            Vector3 ps3 = cam.WorldToScreenPoint(mathRoot.TransformPoint(PivotLocal));
            Vector3 as3 = cam.WorldToScreenPoint(mathRoot.TransformPoint(AnchorLocal));
            Vector2 p = new Vector2(ps3.x, ps3.y), baseDir = new Vector2(as3.x, as3.y) - p, v = screen - p;
            if (v.sqrMagnitude < 16f) return st.selectedSlot;
            float angle = Vector2.SignedAngle(baseDir, v);
            angle = Mathf.Clamp(angle, JogaeRules.MinAngle, JogaeRules.AngleForSlot(JogaeRules.SlotCount - 1));
            int snapped = Mathf.RoundToInt((angle - JogaeRules.MinAngle) / JogaeRules.StepAngle);
            return Mathf.Clamp(snapped, 0, JogaeRules.SlotCount - 1);
        }

        void UpdateScreenState(State s)
        {
            if (!cam || !mathRoot || !handle) return;
            Vector3 p = cam.WorldToScreenPoint(mathRoot.TransformPoint(PivotLocal));
            Vector3 a = cam.WorldToScreenPoint(mathRoot.TransformPoint(AnchorLocal));
            Vector3 h = cam.WorldToScreenPoint(handle.position);
            s.pivotX = p.x; s.pivotY = p.y; s.anchorX = a.x; s.anchorY = a.y; s.handleX = h.x; s.handleY = h.y;
        }

        Vector2 ScreenToCanvas(Vector2 screen)
        {
            var canvasRt = (RectTransform)MgfText.Canvas.transform; Vector2 size = canvasRt.rect.size;
            return new Vector2((screen.x / Mathf.Max(1, Screen.width) - .5f) * size.x, (screen.y / Mathf.Max(1, Screen.height) - .5f) * size.y);
        }

        void UpdateWorld(float dt)
        {
            RelayoutWorld(false);
            ambientClock += dt; guideClock += dt;
            if (hitStop > 0f) { hitStop -= dt; return; }
            float breath = 1f + Mathf.Sin(ambientClock * 2.1f) * .035f;
            if (mainBody) mainBody.localScale = st.dragging ? mainBody.localScale : new Vector3(breath, 1f / breath, 1f);
            for (int i = 0; i < audience.Length; i++)
            {
                if (!audience[i]) continue;
                float phase = ambientClock * (1.05f + i * .035f) + i * .72f;
                audience[i].localPosition = audienceBase[i] + Vector3.up * (Mathf.Sin(phase) * .055f);
                float sing = correctAnim > 0f ? Mathf.Clamp01(correctAnim * 2.8f - i * .17f) * Mathf.Clamp01(2.2f - correctAnim) : 0f;
                audienceUpper[i].localRotation = Quaternion.Euler(-8f - Mathf.Sin(phase) * 3f - sing * 28f, 0, 0);
            }
            if (flow == Flow.Title || flow == Flow.Opening)
            {
                float idle = Mathf.Sin(ambientClock * 1.5f) * 4f;
                titleUpper.localRotation = Quaternion.Euler(-7f - idle - openingAnim * 36f, 0, 0);
                titleLower.localRotation = Quaternion.Euler(4f + openingAnim * 18f, 0, 0);
                titleClam.localScale = Vector3.one * (1f + Mathf.Sin(ambientClock * 1.25f) * .018f);
                if (flow == Flow.Opening) openingAnim = Mathf.SmoothStep(0, 1, Mathf.Clamp01(openingClock / .62f));
            }
            UpdateGuide();
            UpdateRevealFx(dt);
        }

        void UpdateGuide()
        {
            bool active = (flow == Flow.Practice || guideBoost > 0f) && !st.dragging;
            guideRingA.gameObject.SetActive(active); guideRingB.gameObject.SetActive(active);
            if (!active) return;
            float cycle = (guideClock % 1.2f) / 1.2f;
            guideRingA.localScale = Vector3.one * (.80f + cycle * .72f);
            guideRingB.localScale = Vector3.one * (.72f + ((cycle + .5f) % 1f) * .78f);
            float t = (guideClock % 1.2f) / 1.2f;
            float guideAngle = practiceTapArmed ? 40f : Mathf.Lerp(0f, 40f, Mathf.SmoothStep(0, 1, t));
            float a = guideAngle * Mathf.Deg2Rad;
            ghostHand.localPosition = PivotLocal + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * HandleRadius + new Vector3(0, 0, -.12f);
            if (practiceTapArmed) ghostHand.localScale = Vector3.one * (.95f + Mathf.Sin(guideClock * 7f) * .18f);
            else ghostHand.localScale = Vector3.one;
            if (guideBoost > 0f) guideBoost = Mathf.Max(0, guideBoost - Time.deltaTime * .18f);
        }

        void UpdateRevealFx(float dt)
        {
            if (correctAnim > 0f)
            {
                correctAnim += dt; waveProgress = Mathf.Clamp01(correctAnim / .82f); DrawSoundWaves(waveProgress);
                float fold = 1f + Mathf.Sin(Mathf.Clamp01(correctAnim / .5f) * Mathf.PI) * .18f;
                mainBody.localScale = new Vector3(1f / fold, fold, 1f);
                if (correctAnim > 2.2f) { correctAnim = 0f; mainBody.localScale = Vector3.one; }
            }
            if (wrongAnim > 0f)
            {
                wrongAnim += dt; float k = Mathf.Clamp01(1f - wrongAnim / .50f);
                upperPivot.localRotation *= Quaternion.Euler(0, 0, Mathf.Sin(wrongAnim * 52f) * 4.5f * k);
                wrongBreath.localPosition = PivotLocal + new Vector3(-.65f - wrongAnim * 1.2f, -.18f + Mathf.Sin(wrongAnim * 20f) * .12f, -.12f);
                wrongBreath.localScale = new Vector3(.4f + wrongAnim, .18f, .12f) * k;
                if (wrongAnim > .52f) { wrongAnim = 0f; SetSelectedGeometry(st.selectedSlot); wrongBreath.gameObject.SetActive(false); }
            }
        }

        void DrawSoundWaves(float progress)
        {
            int slot = st.lastSubmittedSlot >= 0 ? st.lastSubmittedSlot : st.selectedSlot;
            float end = JogaeRules.AngleForSlot(slot) * 2f * Mathf.Deg2Rad;
            float half = end * .5f;
            for (int i = 0; i < 32; i++)
            {
                float u = i / 31f;
                float a1 = Mathf.Lerp(0, half * progress, u), a2 = Mathf.Lerp(end, end - half * progress, u);
                float ripple = Mathf.Sin(u * Mathf.PI * 7f - progress * 18f) * .055f;
                waveLeftPoints[i] = new Vector3(Mathf.Cos(a1) * (CircleRadius + ripple), Mathf.Sin(a1) * (CircleRadius + ripple), -.10f);
                waveRightPoints[i] = new Vector3(Mathf.Cos(a2) * (CircleRadius - ripple), Mathf.Sin(a2) * (CircleRadius - ripple), -.11f);
            }
            soundWaveL.SetPositions(waveLeftPoints); soundWaveR.SetPositions(waveRightPoints);
        }

        void UpdateUi(float dt)
        {
            bool title = flow == Flow.Title || flow == Flow.Opening, game = flow == Flow.Practice || flow == Flow.Playing || flow == Flow.Bridge || flow == Flow.Reveal, end = flow == Flow.End;
            titleG.alpha = title ? 1 : 0; hudG.alpha = game ? 1 : 0; revealG.alpha = flow == Flow.Reveal ? 1 : 0; endG.alpha = end ? 1 : 0;
            toastClock = Mathf.Max(0, toastClock - dt); toastG.alpha = toastClock > 0 && game ? Mathf.Clamp01(toastClock * 4f) : 0;
            if (rippleClock > 0)
            {
                rippleClock -= dt; float p = 1f - Mathf.Clamp01(rippleClock / .42f); rippleRt.localScale = Vector3.one * (.6f + p * 1.2f); rippleImg.color = new Color(Gold.r, Gold.g, Gold.b, 1f - p);
            }
            else rippleRt.gameObject.SetActive(false);
            if (shownScore != st.score)
            {
                shownScore = Mathf.RoundToInt(Mathf.MoveTowards(shownScore, st.score, Mathf.Max(1, Mathf.Abs(st.score - shownScore) * dt * 7f)));
                scoreTxt.text = shownScore.ToString(); scoreTxt.transform.localScale = Vector3.one * 1.12f;
            }
            else scoreTxt.transform.localScale = Vector3.MoveTowards(scoreTxt.transform.localScale, Vector3.one, dt * 2.8f);
            int sec = Mathf.Max(0, Mathf.CeilToInt(sessionLeft));
            if (shownSecond != sec) { shownSecond = sec; timeTxt.text = sec + "초"; }
            if (title)
            {
                int best = PlayerPrefs.GetInt("jogae-hapchang-best", 0); titleBest.text = best > 0 ? "최고 첫 시도 " + best + "/8" : "첫 합창을 기다리는 골목";
                float pulse = 1f + Mathf.Max(0, Mathf.Sin(ambientClock * 2.4f)) * .025f; titlePearl.rectTransform.localScale = Vector3.one * pulse;
            }
            if (end)
            {
                endCounter = Mathf.Min(st.score, endCounter + dt * Mathf.Max(180f, st.score * 2.5f)); endScoreTxt.text = Mathf.RoundToInt(endCounter).ToString();
            }
            angleRt.localScale = Vector3.MoveTowards(angleRt.localScale, Vector3.one, dt * 3f);
        }

        void SetScreen()
        {
            bool title = flow == Flow.Title || flow == Flow.Opening, game = flow == Flow.Practice || flow == Flow.Playing || flow == Flow.Bridge || flow == Flow.Reveal, end = flow == Flow.End;
            titleRt.gameObject.SetActive(title); hudRt.gameObject.SetActive(game); revealRt.gameObject.SetActive(flow == Flow.Reveal); endRt.gameObject.SetActive(end);
            titleClam.gameObject.SetActive(title); mathRoot.gameObject.SetActive(game || end);
            titleG.alpha = title ? 1 : 0; hudG.alpha = game ? 1 : 0; revealG.alpha = flow == Flow.Reveal ? 1 : 0; endG.alpha = end ? 1 : 0;
            RefreshHudImmediate(); RelayoutWorld(true);
        }

        void RelayoutWorld(bool force)
        {
            bool nowLand = MgfBridge.IsLandLayout;
            if (!force && Screen.width == lastW && Screen.height == lastH && nowLand == land) return;
            land = nowLand; lastW = Screen.width; lastH = Screen.height;
            bool tabletPortrait = !land && Screen.width / (float)Mathf.Max(1, Screen.height) > .56f;
            if (land)
            {
                titleRt.localScale = Vector3.one * .70f;
                hudRt.sizeDelta = revealRt.sizeDelta = endRt.sizeDelta = new Vector2(760, 420);
                cam.orthographicSize = 6.15f; cam.transform.position = new Vector3(0, 7.2f, -18f); cam.transform.LookAt(new Vector3(0, .5f, 0));
                mathRoot.position = new Vector3(.7f, .15f, -.7f); mathRoot.localScale = Vector3.one * .92f;
                titleClam.localPosition = new Vector3(0, -.35f, -.1f); titleClam.localScale = Vector3.one * 1.12f;
                ledgerRt.anchorMin = ledgerRt.anchorMax = new Vector2(.25f, .89f); ledgerRt.sizeDelta = new Vector2(350, 54);
                statusBandRt.gameObject.SetActive(true); statusBandRt.anchorMin = statusBandRt.anchorMax = new Vector2(.29f, .77f); statusBandRt.sizeDelta = new Vector2(410, 42);
                livesTxt.rectTransform.anchorMin = livesTxt.rectTransform.anchorMax = new Vector2(.13f, .77f); livesTxt.rectTransform.sizeDelta = new Vector2(150, 38);
                masteryTxt.rectTransform.anchorMin = masteryTxt.rectTransform.anchorMax = new Vector2(.39f, .77f); masteryTxt.rectTransform.sizeDelta = new Vector2(240, 38);
                goalRt.anchorMin = goalRt.anchorMax = new Vector2(.75f, .88f); goalRt.sizeDelta = new Vector2(330, 44);
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(.75f, .67f); promptRt.sizeDelta = new Vector2(350, 112);
                angleRt.anchorMin = angleRt.anchorMax = new Vector2(.52f, .12f);
                revealShellRt.anchorMin = revealShellRt.anchorMax = new Vector2(.75f, .22f); revealShellRt.sizeDelta = new Vector2(350, 142);
                endShellRt.anchorMin = endShellRt.anchorMax = new Vector2(.5f, .5f); endShellRt.sizeDelta = new Vector2(500, 360);
            }
            else
            {
                titleRt.localScale = Vector3.one;
                float portraitHeight = tabletPortrait ? 660f : 844f;
                hudRt.sizeDelta = revealRt.sizeDelta = endRt.sizeDelta = new Vector2(390, portraitHeight);
                cam.orthographicSize = 8.25f; cam.transform.position = new Vector3(0, 7.4f, -18f); cam.transform.LookAt(new Vector3(0, .8f, 0));
                mathRoot.position = new Vector3(0, -.30f, -.7f); mathRoot.localScale = Vector3.one * .93f;
                titleClam.localPosition = new Vector3(0, -.05f, -.1f); titleClam.localScale = Vector3.one;
                ledgerRt.anchorMin = ledgerRt.anchorMax = new Vector2(.46f, tabletPortrait ? .93f : .958f); ledgerRt.sizeDelta = new Vector2(350, 58);
                statusBandRt.gameObject.SetActive(tabletPortrait);
                statusBandRt.anchorMin = statusBandRt.anchorMax = new Vector2(.46f, tabletPortrait ? .84f : .914f); statusBandRt.sizeDelta = new Vector2(360, 40);
                livesTxt.rectTransform.anchorMin = livesTxt.rectTransform.anchorMax = new Vector2(.20f, tabletPortrait ? .84f : .914f); livesTxt.rectTransform.sizeDelta = new Vector2(145, 39.2f);
                masteryTxt.rectTransform.anchorMin = masteryTxt.rectTransform.anchorMax = new Vector2(.67f, tabletPortrait ? .84f : .914f); masteryTxt.rectTransform.sizeDelta = new Vector2(238, 33.6f);
                goalRt.anchorMin = goalRt.anchorMax = new Vector2(.5f, tabletPortrait ? .75f : .875f); goalRt.sizeDelta = new Vector2(354, 44);
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(.5f, tabletPortrait ? .61f : .795f); promptRt.sizeDelta = new Vector2(360, 92);
                angleRt.anchorMin = angleRt.anchorMax = new Vector2(.53f, tabletPortrait ? .12f : .235f);
                revealShellRt.anchorMin = revealShellRt.anchorMax = new Vector2(.5f, tabletPortrait ? .15f : .19f); revealShellRt.sizeDelta = new Vector2(356, 156);
                endShellRt.anchorMin = endShellRt.anchorMax = new Vector2(.5f, .5f); endShellRt.sizeDelta = new Vector2(358, 500);
            }
            mathRoot.rotation = cam.transform.rotation;
        }
    }
}
