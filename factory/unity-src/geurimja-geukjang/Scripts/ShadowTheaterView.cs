// 그림자 극장 — 열린 코발트 채광 마당·도자기 조리개의 2.5D 무대와 화면 UI.
using System;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.GeurimjaGeukjang
{
    public partial class ShadowTheaterGame
    {
        static readonly Color Ink = MgfLook.Hex("17223B");
        static readonly Color Paper = MgfLook.Hex("DDE9F2");
        static readonly Color Ivory = MgfLook.Hex("F4F1E6");
        static readonly Color Brass = MgfLook.Hex("7C5C3A");
        static readonly Color Vermilion = MgfLook.Hex("F2D64B");
        static readonly Color Ash = MgfLook.Hex("243B9B");

        Camera cam;
        Transform worldRoot;
        Transform puppetRoot;
        Transform puppetBody;
        Transform shadowRoot;
        Transform lampRoot;
        Transform[] paperLayers = new Transform[6];
        Quaternion[] paperLayerBaseRotations = new Quaternion[6];
        Transform[] camWheels = new Transform[4];
        GameObject[] completedLayers = new GameObject[ShadowRules.TargetActs];
        Transform[] fuses = new Transform[ShadowRules.StartLives];
        LineRenderer rayTop, rayBottom, puppetTrail;
        GameObject wrongEdge;
        Light lampLight;
        Material screenMat, ivoryMat, brassMat, blackMat, paperMat, vermilionMat, ashMat;
        Vector3 puppetBaseScale;
        const float ShadowBaseScale = .28f;
        int activeLayerAnim = -1;
        float layerAnimClock;
        float finaleClock = -1;
        float worldClock;

        GameObject titleRoot, playRoot, endRoot;
        RawImage titleArt;
        RectTransform titleArtRt;
        TextMeshProUGUI titleLogo, titleLogoShadow, titleTag, titleMeta, titleLeverText;
        RectTransform titleLeverStem, titleLeverKnob;
        Image titleLeverStemImage, titleLeverKnobImage;

        TextMeshProUGUI goalUi, promptUi, hudUi, distanceUi, evidenceUi;
        TextMeshProUGUI toastUi, revealUi, leverUi;
        RectTransform goalRt, promptRt, hudRt, distanceRt, evidenceRt, toastRt, revealRt;
        RectTransform curtainStemRt, curtainHandleRt, railRt, railCursorRt;
        Image curtainStemImage, curtainHandleImage, railImage, railCursorImage;
        Image[] tickImages = new Image[9];
        TextMeshProUGUI[] tickLabels = new TextMeshProUGUI[9];
        Image[] fuseImages = new Image[3];
        RectTransform guideHandRt, guideLineRt, guideArrowRt;
        Image guideHand, guideLine, guideArrow;
        RectTransform rippleRt;
        Image rippleImage;
        CanvasGroup toastGroup, revealGroup;

        TextMeshProUGUI endTitleUi, endStatsUi, endCtaUi;
        RectTransform endPanelRt, endCtaStemRt, endCtaKnobRt;
        Image endPanelImage, endCtaStemImage, endCtaKnobImage;

        float displayScore;
        float endDisplayScore;
        string endWhy = "";
        float toastClock;
        float toastDuration;
        float guideClock;
        float guideDuration;
        int lastTimer = -1, lastHudScore = -1, lastHudLives = -1, lastHudSolved = -1;
        int lastScreenW = -1, lastScreenH = -1;
        float lastCanvasW = -1, lastCanvasH = -1;
        bool land;

        void BuildWorld()
        {
            MgfLook.Sky(MgfLook.Hex("4A9BD4"), MgfLook.Hex("B9DCF1"), MgfLook.Hex("E6F0F5"), .82f);
            MgfLook.Sun(new Vector3(38f, -28f, -18f), MgfLook.Hex("FFF2C8"), 1.16f, .82f);
            cam = MgfLook.Camera(new Vector3(0f, 6.8f, -17.8f), new Vector3(0f, 1.8f, .5f), 32f);
            worldRoot = new GameObject("OpenAirShadowCourtyard").transform;

            blackMat = MgfLook.Lit(Ink, .52f, .12f);
            paperMat = MgfLook.Lit(Paper, .10f);
            ivoryMat = MgfLook.Lit(Ivory, .20f);
            brassMat = MgfLook.Lit(Brass, .24f, .05f);
            vermilionMat = MgfLook.Lit(Vermilion, .36f, .05f, Vermilion * .08f);
            ashMat = MgfLook.Lit(Ash, .66f, .08f);
            screenMat = MgfLook.Lit(Ivory, .22f, 0f, Color.black);

            // 밝은 종이 작업면 대신 하늘이 열린 코발트 채광 마당을 만든다. 낮은 콘크리트 벽,
            // 깊은 차양 기둥, 도자기 조리개가 세계를 채워 neoguri-oncheon의 종이/슬롯 문법과 가른다.
            MgfLook.Block("CourtyardFloor", new Vector3(0f, -.72f, 1.2f), new Vector3(25f, .55f, 8.6f), .12f, ivoryMat, worldRoot);
            MgfLook.Block("BlueConcreteWall", new Vector3(0f, 1.25f, 4.5f), new Vector3(24f, 4.4f, .55f), .08f, paperMat, worldRoot);
            MgfLook.Block("OpenSkyLintel", new Vector3(0f, 5.65f, 4.35f), new Vector3(21f, .38f, .62f), .09f, ashMat, worldRoot);
            for (int i = 0; i < 5; i++)
            {
                float x = -8.2f + i * 4.1f;
                MgfLook.Block("CourtyardColumn" + i, new Vector3(x, 3.15f, 4.12f), new Vector3(.42f, 5.0f, .72f), .12f, i % 2 == 0 ? ashMat : ivoryMat, worldRoot);
                MgfLook.Block("SunNotch" + i, new Vector3(x + .48f, 1.0f + (i % 2) * .35f, 3.78f), new Vector3(.38f, 1.4f, .18f), .10f, vermilionMat, worldRoot);
            }
            for (int i = 0; i < paperLayers.Length; i++)
            {
                var layer = new GameObject("CeramicShadeFin" + i).transform;
                layer.SetParent(worldRoot, false);
                float side = i < 3 ? -1f : 1f;
                int j = i % 3;
                layer.localPosition = new Vector3(side * (7.0f + j * 1.1f), 2.35f + j * .18f, 2.6f + j * .40f);
                layer.localRotation = Quaternion.Euler(0f, side * (13f + j * 7f), side * (j - 1) * 1.5f);
                MgfLook.Block("GlazedFin", Vector3.zero, new Vector3(1.35f + j * .28f, 6.1f - j * .45f, .30f), .14f, i % 2 == 0 ? ashMat : ivoryMat, layer);
                for (int k = 0; k < 4; k++)
                    MgfLook.Block("SunTile", new Vector3(side * (.34f + j * .06f), -1.9f + k * 1.25f, -.19f), new Vector3(.22f, .50f, .07f), .08f, k % 2 == 0 ? vermilionMat : brassMat, layer);
                paperLayers[i] = layer;
                paperLayerBaseRotations[i] = layer.localRotation;
            }

            // 백색 콘크리트 그림자 벽과 무광 목재 레일.
            MgfLook.Block("WhiteShadowWall", new Vector3(4.15f, 2.45f, 1.6f), new Vector3(3.65f, 5.6f, .24f), .10f, screenMat, worldRoot);
            MgfLook.Block("WallCoping", new Vector3(4.15f, 5.35f, 1.52f), new Vector3(3.95f, .22f, .34f), .08f, ashMat, worldRoot);
            MgfLook.Block("OpticalRail", new Vector3(0f, .25f, -.55f), new Vector3(10.8f, .28f, .72f), .06f, brassMat, worldRoot);
            MgfLook.Block("RailShadow", new Vector3(0f, -.02f, -.35f), new Vector3(11.6f, .42f, 1.1f), .04f, blackMat, worldRoot);
            for (int d = ShadowRules.MinD; d <= ShadowRules.MaxD; d++)
            {
                float x = WorldXForD(d);
                MgfLook.Block("Tick" + d, new Vector3(x, .49f, -.78f), new Vector3(.07f, .36f, .36f), .015f, d == 6 ? vermilionMat : ivoryMat, worldRoot);
                var tx = MgfText.World(d.ToString(), new Vector3(x, .76f, -.88f), .58f, d == 6 ? Vermilion : Ivory, worldRoot);
                tx.alignment = TextAlignmentOptions.Center;
            }

            BuildLamp();
            BuildPuppet();
            BuildShadow();
            BuildRays();
            BuildCamsAndFuses();
            BuildRewardLayers();

            wrongEdge = MgfLook.Block("VermilionRegistrationBurn", new Vector3(4.35f, 2.35f, 1.26f), new Vector3(.10f, 3.2f, .09f), .01f, vermilionMat, worldRoot);
            wrongEdge.SetActive(false);
        }

        void BuildLamp()
        {
            lampRoot = new GameObject("CeramicSunProjector").transform; lampRoot.SetParent(worldRoot, false);
            lampRoot.localPosition = new Vector3(-4.55f, 1.5f, -.45f);
            MgfLook.Block("LampBody", Vector3.zero, new Vector3(.86f, .86f, .86f), .30f, ashMat, lampRoot);
            MgfLook.Block("LensGlow", new Vector3(.58f, 0f, 0f), new Vector3(.45f, .62f, .62f), .20f, MgfLook.Lit(MgfLook.Hex("FFD77E"), .5f, 0f, MgfLook.Hex("FFB533")), lampRoot);
            MgfLook.Block("LampStand", new Vector3(-.2f, -.88f, .1f), new Vector3(.34f, 1.55f, .45f), .08f, blackMat, lampRoot);
            lampLight = new GameObject("LampPoint").AddComponent<Light>();
            lampLight.transform.SetParent(lampRoot, false); lampLight.transform.localPosition = new Vector3(.62f, .1f, -.45f);
            lampLight.type = LightType.Point; lampLight.color = MgfLook.Hex("FFD99B"); lampLight.range = 9f; lampLight.intensity = 2.5f; lampLight.shadows = LightShadows.None;
        }

        void BuildPuppet()
        {
            puppetRoot = new GameObject("SlidingWoodPuppet").transform; puppetRoot.SetParent(worldRoot, false);
            MgfLook.Block("Carriage", new Vector3(0f, .12f, 0f), new Vector3(1.05f, .42f, .74f), .06f, brassMat, puppetRoot);
            MgfLook.Block("WheelA", new Vector3(-.34f, -.12f, -.38f), new Vector3(.34f, .34f, .18f), .15f, blackMat, puppetRoot);
            MgfLook.Block("WheelB", new Vector3(.34f, -.12f, -.38f), new Vector3(.34f, .34f, .18f), .15f, blackMat, puppetRoot);
            puppetBody = new GameObject("ArticulatedBird").transform; puppetBody.SetParent(puppetRoot, false); puppetBody.localPosition = new Vector3(0f, .72f, -.08f);
            MakeBird(puppetBody, blackMat, .78f, false);
            puppetBaseScale = puppetBody.localScale;
        }

        void MakeBird(Transform root, Material mat, float scale, bool shadow)
        {
            root.localScale = Vector3.one * scale;
            var body = MgfLook.Block("BirdBody", new Vector3(0f, .62f, 0f), new Vector3(1.15f, 1.05f, shadow ? .06f : .16f), .04f, mat, root);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 38f);
            var neck = MgfLook.Block("BirdNeck", new Vector3(.48f, 1.48f, 0f), new Vector3(.28f, 1.5f, shadow ? .06f : .15f), .03f, mat, root);
            neck.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            var beak = MgfLook.Block("BirdBeak", new Vector3(1.0f, 2.12f, 0f), new Vector3(1.0f, .18f, shadow ? .06f : .14f), .02f, mat, root);
            beak.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            for (int i = 0; i < 5; i++)
            {
                var wing = MgfLook.Block("WingRay" + i, new Vector3(-.55f, .9f - i * .08f, .02f), new Vector3(1.45f - i * .1f, .22f, shadow ? .05f : .13f), .02f, mat, root);
                wing.transform.localRotation = Quaternion.Euler(0f, 0f, 18f + i * 14f);
            }
            for (int i = 0; i < 2; i++)
            {
                var leg = MgfLook.Block("Leg" + i, new Vector3(-.12f + i * .4f, -.2f, 0f), new Vector3(.16f, 1.05f, shadow ? .05f : .13f), .02f, mat, root);
                leg.transform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 12f : -12f);
            }
        }

        void BuildShadow()
        {
            shadowRoot = new GameObject("LiveShadow").transform; shadowRoot.SetParent(worldRoot, false);
            shadowRoot.localPosition = new Vector3(4.34f, 1.75f, 1.26f);
            MakeBird(shadowRoot, MgfLook.Unlit(new Color(.04f, .035f, .03f, 1f)), ShadowBaseScale, true);
        }

        LineRenderer MakeRay(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(worldRoot, false);
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.positionCount = 3;
            line.widthMultiplier = .045f; line.numCapVertices = 2; line.sharedMaterial = MgfLook.Unlit(new Color(Brass.r, Brass.g, Brass.b, .78f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }

        void BuildRays()
        {
            rayTop = MakeRay("RayOAC"); rayBottom = MakeRay("RayOBD");
            var trailGo = new GameObject("PaperDragTrail"); trailGo.transform.SetParent(worldRoot, false);
            puppetTrail = trailGo.AddComponent<LineRenderer>(); puppetTrail.useWorldSpace = true; puppetTrail.positionCount = 8;
            puppetTrail.widthMultiplier = .09f; puppetTrail.sharedMaterial = MgfLook.Unlit(new Color(Vermilion.r, Vermilion.g, Vermilion.b, .58f)); puppetTrail.enabled = false;
        }

        void BuildCamsAndFuses()
        {
            for (int i = 0; i < camWheels.Length; i++)
            {
                var root = new GameObject("ExposedCam" + i).transform; root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(-1.9f + i * 1.35f, -.55f, -.2f);
                MgfLook.Block("ApertureWheel", Vector3.zero, new Vector3(1.08f, 1.08f, .24f), .46f, ashMat, root);
                MgfLook.Block("ApertureLobe", new Vector3(.32f, .0f, -.18f), new Vector3(.62f, .22f, .24f), .06f, ivoryMat, root);
                camWheels[i] = root;
            }
            for (int i = 0; i < fuses.Length; i++)
            {
                var root = new GameObject("CeramicLens" + i).transform; root.SetParent(worldRoot, false);
                root.localPosition = new Vector3(2.4f + i * .65f, -.55f, -.55f);
                MgfLook.Block("LensBody", Vector3.zero, new Vector3(.42f, .56f, .18f), .14f, ashMat, root);
                MgfLook.Block("LensMark", new Vector3(0f, .02f, -.12f), new Vector3(.20f, .30f, .04f), .08f, vermilionMat, root);
                fuses[i] = root;
            }
        }

        void BuildRewardLayers()
        {
            for (int i = 0; i < completedLayers.Length; i++)
            {
                var tile = new GameObject("CyanotypeMosaic" + i); tile.transform.SetParent(worldRoot, false);
                tile.transform.localPosition = new Vector3(RewardTargetX(i), 4.62f, 3.82f - i * .01f);
                MgfLook.Block("BlueTile", Vector3.zero, new Vector3(.72f, .52f, .10f), .10f, i % 2 == 0 ? ashMat : blackMat, tile.transform);
                MgfLook.Block("SunPrint", new Vector3(0f, 0f, -.075f), new Vector3(.42f, .09f, .035f), .035f, ivoryMat, tile.transform);
                MgfLook.Block("RayMark", new Vector3((i % 3 - 1) * .10f, .13f, -.08f), new Vector3(.10f, .22f, .035f), .035f, vermilionMat, tile.transform);
                tile.SetActive(false); completedLayers[i] = tile;
            }
        }

        float RewardTargetX(int index) { return -3.15f + index * .86f; }

        float WorldXForD(int d) => Mathf.Lerp(-3.05f, 3.05f, (d - ShadowRules.MinD) / 8f);

        void UpdatePuppetPosition(int d, bool snapped)
        {
            if (!puppetRoot) return;
            puppetRoot.localPosition = new Vector3(WorldXForD(d), .28f, -.66f);
            float ratio = (float)ShadowRules.L / d;
            shadowRoot.localScale = Vector3.one * (ShadowBaseScale * ratio);
            UpdateRays();
            if (snapped)
            {
                MgfFx.Punch(puppetBody, .08f, .14f);
                SetRailCursor(d);
            }
        }

        void UpdateRays()
        {
            if (!rayTop || !puppetRoot) return;
            Vector3 lamp = new Vector3(-3.95f, 1.5f, -.68f);
            float px = puppetRoot.localPosition.x;
            float scale = puppetBody ? puppetBody.localScale.y : .78f;
            Vector3 top = new Vector3(px, 3.05f * scale + .4f, -.7f);
            Vector3 bottom = new Vector3(px, .72f, -.7f);
            float k = (4.34f - lamp.x) / Mathf.Max(.25f, px - lamp.x);
            Vector3 topScreen = new Vector3(4.34f, lamp.y + (top.y - lamp.y) * k, 1.22f);
            Vector3 bottomScreen = new Vector3(4.34f, lamp.y + (bottom.y - lamp.y) * k, 1.22f);
            topScreen.y = Mathf.Clamp(topScreen.y, -.1f, 5.15f); bottomScreen.y = Mathf.Clamp(bottomScreen.y, -.1f, 5.15f);
            rayTop.SetPosition(0, lamp); rayTop.SetPosition(1, top); rayTop.SetPosition(2, topScreen);
            rayBottom.SetPosition(0, lamp); rayBottom.SetPosition(1, bottom); rayBottom.SetPosition(2, bottomScreen);
        }

        void ResetWorldForRun()
        {
            for (int i = 0; i < completedLayers.Length; i++) if (completedLayers[i])
            {
                completedLayers[i].SetActive(false);
                completedLayers[i].transform.localPosition = new Vector3(RewardTargetX(i), 4.62f, 3.82f - i * .01f);
                completedLayers[i].transform.localScale = Vector3.one;
                completedLayers[i].transform.localRotation = Quaternion.identity;
            }
            for (int i = 0; i < fuses.Length; i++) if (fuses[i]) { fuses[i].gameObject.SetActive(true); fuses[i].localRotation = Quaternion.identity; }
            activeLayerAnim = -1; finaleClock = -1;
            if (shadowRoot) { shadowRoot.localPosition = new Vector3(4.34f, 1.75f, 1.26f); shadowRoot.localRotation = Quaternion.identity; }
            if (wrongEdge) wrongEdge.SetActive(false);
            if (screenMat) screenMat.SetColor("_EmissionColor", Color.black);
            SetSelectedD(3, false);
        }

        void ResetWorldForProblem(ShadowProblem p)
        {
            if (wrongEdge) wrongEdge.SetActive(false);
            if (puppetBody) puppetBody.localRotation = Quaternion.Euler(0f, p != null && p.rotated ? 180f : 0f, p != null && p.rotated ? 7f : -4f);
            if (screenMat) screenMat.SetColor("_EmissionColor", Color.black);
            UpdatePuppetPosition(st.selectedD, false);
        }

        void BeginPuppetGrab()
        {
            puppetTrail.enabled = true;
            for (int i = 0; i < 8; i++) puppetTrail.SetPosition(i, puppetRoot.position);
            puppetBody.localScale = Vector3.Scale(puppetBaseScale, new Vector3(1.1f, .92f, 1f));
        }

        void UpdatePuppetTrail()
        {
            if (!puppetTrail.enabled) return;
            for (int i = 0; i < 7; i++) puppetTrail.SetPosition(i, puppetTrail.GetPosition(i + 1));
            puppetTrail.SetPosition(7, puppetRoot.position + Vector3.up * .55f);
        }

        void EndPuppetGrab()
        {
            puppetTrail.enabled = false;
            puppetBody.localScale = puppetBaseScale;
            MgfFx.Punch(puppetRoot, .05f, .16f);
        }

        void BeginCurtainGrab() { MgfFx.Punch(curtainHandleRt, .08f, .18f); }
        void EndCurtainGrab() { }

        void BeginRevealVisual(bool correct, bool practice, bool first)
        {
            revealUi.text = correct ? current.revealFormula : WrongRevealText();
            revealGroup.alpha = 1; revealClock = 0;
            if (correct)
            {
                wrongEdge.SetActive(false);
                screenMat.SetColor("_EmissionColor", MgfLook.Hex("243B9B") * .48f);
                activeLayerAnim = Math.Max(0, Math.Min(ShadowRules.TargetActs - 1, practice ? 0 : st.solved - 1));
                completedLayers[activeLayerAnim].SetActive(true);
                completedLayers[activeLayerAnim].transform.localPosition = new Vector3(RewardTargetX(activeLayerAnim), 4.62f, 3.82f - activeLayerAnim * .01f);
                completedLayers[activeLayerAnim].transform.localScale = new Vector3(.04f, .18f, 1f);
                layerAnimClock = 0;
                MgfSfx.Play("correct", first ? .52f : .38f);
            }
            else
            {
                wrongEdge.SetActive(true);
                float scale = Mathf.Clamp((float)ShadowRules.L / st.selectedD, .9f, 2.5f);
                wrongEdge.transform.localScale = new Vector3(1f, scale, 1f);
                if (!practice) BreakFuse(st.lives);
                MgfSfx.Play("wrong", practice ? .2f : .42f);
            }
        }

        string WrongRevealText()
        {
            string label = current.kind == ShadowKind.AreaRatio ? "선택한 넓이의 비 " : "선택한 길이의 비 ";
            return label + ShadowRules.ActualRatio(current, st.selectedD) + "  ≠  목표 " + current.targetRatio + "\n" + WrongReason(st.misconceptionId);
        }

        void CompleteRevealVisual(bool correct)
        {
            if (screenMat) screenMat.SetColor("_EmissionColor", Color.black);
            if (correct && activeLayerAnim >= 0)
            {
                completedLayers[activeLayerAnim].transform.localScale = Vector3.one;
                completedLayers[activeLayerAnim].transform.localRotation = Quaternion.identity;
            }
            activeLayerAnim = -1;
            UpdateCurtainPull(0f);
        }

        void BreakFuse(int livesAfter)
        {
            int index = Math.Max(0, Math.Min(fuses.Length - 1, livesAfter));
            if (!fuses[index]) return;
            fuses[index].localRotation = Quaternion.Euler(0f, 0f, -28f);
            MgfFx.Punch(fuses[index], .16f, .24f);
            if (index < fuseImages.Length && fuseImages[index]) fuseImages[index].color = new Color(.20f, .18f, .16f, 1f);
        }

        void BeginFinale()
        {
            finaleClock = 0;
            for (int i = 0; i < completedLayers.Length; i++)
            {
                completedLayers[i].SetActive(true);
                completedLayers[i].transform.localScale = Vector3.one;
                completedLayers[i].transform.localRotation = Quaternion.identity;
            }
            screenMat.SetColor("_EmissionColor", MgfLook.Hex("243B9B") * .34f);
        }

        void UpdateWorld(float dt)
        {
            worldClock += dt;
            float t = worldClock;
            if (lampLight) lampLight.intensity = 2.35f + Mathf.Sin(t * 3.4f) * .12f;
            for (int i = 0; i < paperLayers.Length; i++)
                if (paperLayers[i]) paperLayers[i].localRotation = paperLayerBaseRotations[i] * Quaternion.Euler(0f, Mathf.Sin(t * .42f + i) * 1.6f, 0f);
            for (int i = 0; i < camWheels.Length; i++)
                if (camWheels[i]) camWheels[i].localRotation = Quaternion.Euler(0f, 0f, t * (5f + i * 1.4f) * (i % 2 == 0 ? 1f : -1f));
            if (puppetBody && !puppetDragging && gamePhase != GamePhase.Reveal)
                puppetBody.localScale = puppetBaseScale * (1f + Mathf.Sin(t * 1.8f) * .018f);

            if (activeLayerAnim >= 0)
            {
                layerAnimClock += dt;
                float u = Mathf.Clamp01(Mathf.Max(0f, layerAnimClock - .08f) / .62f); // 80ms hit-stop
                float eased = 1f - Mathf.Pow(1f - u, 3f);
                float overshoot = Mathf.Sin(u * Mathf.PI) * .10f;
                completedLayers[activeLayerAnim].transform.localScale = new Vector3(Mathf.Min(1.08f, eased + overshoot), Mathf.Lerp(.18f, 1f, eased), 1f);
                completedLayers[activeLayerAnim].transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(u * Mathf.PI) * (activeLayerAnim % 2 == 0 ? 4f : -4f));
            }
            if (finaleClock >= 0)
            {
                finaleClock += dt;
                for (int i = 0; i < completedLayers.Length; i++)
                {
                    Vector3 p = completedLayers[i].transform.localPosition;
                    p.y = 4.62f + Mathf.Sin(finaleClock * (1.05f + i * .08f) + i) * .08f;
                    p.x = RewardTargetX(i) + Mathf.Sin(finaleClock * .7f + i * .55f) * .025f;
                    completedLayers[i].transform.localPosition = p;
                    completedLayers[i].transform.localScale = Vector3.one;
                }
                shadowRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(finaleClock * 1.4f) * 3f);
            }
            UpdateResponsiveLayout();
        }

        // ── UI ─────────────────────────────────────────────────────────────
        void BuildUi()
        {
            Canvas canvas = MgfText.Canvas;
            titleRoot = Root("TitleUI", canvas.transform);
            playRoot = Root("PlayUI", canvas.transform);
            endRoot = Root("EndUI", canvas.transform);
            BuildTitleUi(); BuildPlayUi(); BuildEndUi();
        }

        void BuildTitleUi()
        {
            var artGo = new GameObject("TitleKeyArt", typeof(RectTransform), typeof(RawImage)); artGo.transform.SetParent(titleRoot.transform, false);
            titleArt = artGo.GetComponent<RawImage>(); titleArtRt = (RectTransform)artGo.transform; Stretch(titleArtRt);
            titleArt.texture = Resources.Load<Texture2D>("GeurimjaGeukjang/title");
            // 생성 키아트 대신 실제 플레이 무대를 그대로 타이틀 세계로 보여 준다.
            titleArt.color = Color.clear;
            titleArt.raycastTarget = false;
            // 로고는 스크린의 음영 빈 공간에 투사된 이중 판화처럼 쌓는다.
            titleLogoShadow = Txt(titleRoot.transform, "그림자 극장", new Vector2(.5f, .77f), new Vector2(7f, -7f), 48f, Brass, 360f);
            titleLogo = Txt(titleRoot.transform, "그림자 극장", new Vector2(.5f, .77f), Vector2.zero, 48f, Ink, 360f);
            titleLogo.fontStyle = FontStyles.Bold; titleLogo.outlineWidth = .22f; titleLogo.outlineColor = new Color32(242,232,207,255);
            titleLogoShadow.fontStyle = FontStyles.Bold; titleLogoShadow.outlineWidth = .12f; titleLogoShadow.outlineColor = new Color32(17,17,17,220);
            titleTag = Txt(titleRoot.transform, "인형을 밀어 비를 맞춰", new Vector2(.5f, .68f), Vector2.zero, 18f, Ivory, 340f);
            titleTag.outlineWidth = .2f; titleTag.outlineColor = new Color32(17,17,17,240);
            int best = PlayerPrefs.GetInt("geurimja-geukjang.best", 0);
            titleMeta = Txt(titleRoot.transform, "중학교 2학년 · 도형의 닮음" + (best > 0 ? " · 최고 첫 시도 " + best + "/8" : ""), new Vector2(.5f, .94f), Vector2.zero, 14f, Ivory, 360f);
            titleMeta.outlineWidth = .18f; titleMeta.outlineColor = new Color32(17,17,17,245);

            var stem = Panel("TitleDimmerStem", titleRoot.transform, new Color32(181,138,69,245), out titleLeverStemImage); titleLeverStemRt(stem);
            var knob = Panel("TitleDimmerKnob", titleRoot.transform, new Color32(211,74,54,255), out titleLeverKnobImage); titleLeverKnob = (RectTransform)knob.transform;
            var outline = knob.AddComponent<Outline>(); outline.effectColor = new Color32(17,17,17,255); outline.effectDistance = new Vector2(4f,-4f);
            titleLeverText = Txt(titleRoot.transform, "화면을 눌러 등록등을 켜시오", new Vector2(.5f, .075f), Vector2.zero, 16f, Ink, 360f);
            titleLeverText.outlineWidth=.2f;titleLeverText.outlineColor=new Color32(17,17,17,245);
        }

        void titleLeverStemRt(GameObject stem) { titleLeverStem = (RectTransform)stem.transform; }

        void BuildPlayUi()
        {
            var goal = Panel("GoalBanner", playRoot.transform, new Color32(23,34,59,238), out _); goalRt = (RectTransform)goal.transform;
            goalUi = Txt(goal.transform, "등불에서 인형까지의 거리를 정해 주문한 비의 그림자를 만드시오.", new Vector2(.5f,.5f), Vector2.zero, 15.5f, Ivory, 350f); StretchInset(goalUi.rectTransform, 7f);
            var go = goal.AddComponent<Outline>(); go.effectColor = new Color32(242,214,75,235); go.effectDistance = new Vector2(0,-2);

            var prompt = Panel("OrderCard", playRoot.transform, new Color32(244,241,230,246), out _); promptRt=(RectTransform)prompt.transform;
            promptUi = Txt(prompt.transform, "", new Vector2(.5f,.5f), Vector2.zero, 16f, Ink, 350f); promptUi.textWrappingMode=TextWrappingModes.Normal; StretchInset(promptUi.rectTransform, 9f);
            var po=prompt.AddComponent<Outline>();po.effectColor=new Color32(36,59,155,235);po.effectDistance=new Vector2(3,-3);

            hudUi = Txt(playRoot.transform, "", new Vector2(.5f,.5f), Vector2.zero, 13f, Ink, 350f); hudRt=hudUi.rectTransform;
            hudUi.textWrappingMode=TextWrappingModes.Normal;hudUi.overflowMode=TextOverflowModes.Overflow;
            hudUi.outlineWidth=.14f;hudUi.outlineColor=new Color32(244,241,230,245);
            distanceUi = Txt(playRoot.transform, "OA = 3 cm", new Vector2(.5f,.5f), Vector2.zero, 22f, Ivory, 190f); distanceRt=distanceUi.rectTransform;
            distanceUi.outlineWidth=.2f;distanceUi.outlineColor=new Color32(17,17,17,255);

            var evidence=Panel("RayDiagramCard",playRoot.transform,new Color32(23,34,59,238),out _);evidenceRt=(RectTransform)evidence.transform;
            evidenceUi=Txt(evidence.transform,"등불 O · OA=d cm · 인형 AB  |  OC=12 cm · 스크린 CD\n△OAB∽△OCD",new Vector2(.5f,.5f),Vector2.zero,14.5f,Ivory,350f);
            evidenceUi.textWrappingMode=TextWrappingModes.Normal;StretchInset(evidenceUi.rectTransform,6f);

            var rail=Panel("UiRail",playRoot.transform,new Color32(181,138,69,235),out railImage);railRt=(RectTransform)rail.transform;
            var cursor=Panel("RailCursor",playRoot.transform,new Color32(211,74,54,255),out railCursorImage);railCursorRt=(RectTransform)cursor.transform;
            for(int i=0;i<9;i++)
            {
                var tick=Panel("UiTick"+(i+2),playRoot.transform,new Color32(242,232,207,225),out tickImages[i]);
                tickLabels[i]=Txt(playRoot.transform,(i+2).ToString(),new Vector2(.5f,.5f),Vector2.zero,17f,Ivory,32f);
                tickLabels[i].outlineWidth=.17f;tickLabels[i].outlineColor=new Color32(17,17,17,255);
            }

            var stem=Panel("CurtainCable",playRoot.transform,new Color32(181,138,69,240),out curtainStemImage);curtainStemRt=(RectTransform)stem.transform;
            var handle=Panel("CurtainHandle",playRoot.transform,new Color32(211,74,54,255),out curtainHandleImage);curtainHandleRt=(RectTransform)handle.transform;
            var co=handle.AddComponent<Outline>();co.effectColor=new Color32(17,17,17,255);co.effectDistance=new Vector2(4,-4);
            leverUi=Txt(playRoot.transform,"막",new Vector2(.5f,.5f),Vector2.zero,16f,Ivory,52f);leverUi.outlineWidth=.18f;leverUi.outlineColor=new Color32(17,17,17,255);

            for(int i=0;i<3;i++)
            {
                var fuse=Panel("FuseUi"+i,playRoot.transform,new Color32(211,74,54,255),out fuseImages[i]);
                var frt=(RectTransform)fuse.transform;Rect(frt,new Vector2(.5f,.5f),Vector2.zero,new Vector2(18f,38f));
                fuseImages[i].transform.localRotation=Quaternion.Euler(0,0,i%2==0?4f:-4f);
            }

            var toast=Panel("Toast",playRoot.transform,new Color32(23,34,59,246),out _);toastRt=(RectTransform)toast.transform;toastGroup=toast.AddComponent<CanvasGroup>();
            toastUi=Txt(toast.transform,"",new Vector2(.5f,.5f),Vector2.zero,14f,Ivory,350f);toastUi.textWrappingMode=TextWrappingModes.Normal;StretchInset(toastUi.rectTransform,6f);toastGroup.alpha=0;
            var reveal=Panel("MathReveal",playRoot.transform,new Color32(244,241,230,250),out _);revealRt=(RectTransform)reveal.transform;revealGroup=reveal.AddComponent<CanvasGroup>();
            revealUi=Txt(reveal.transform,"",new Vector2(.5f,.5f),Vector2.zero,19f,Ink,350f);revealUi.textWrappingMode=TextWrappingModes.Normal;StretchInset(revealUi.rectTransform,8f);revealGroup.alpha=0;
            var ro=reveal.AddComponent<Outline>();ro.effectColor=new Color32(36,59,155,245);ro.effectDistance=new Vector2(4,-4);

            var hand=Panel("GuideHand",playRoot.transform,new Color32(242,232,207,245),out guideHand);guideHandRt=(RectTransform)hand.transform;
            var line=Panel("GuidePath",playRoot.transform,new Color32(211,74,54,185),out guideLine);guideLineRt=(RectTransform)line.transform;
            var arrow=Panel("GuideArrow",playRoot.transform,new Color32(211,74,54,245),out guideArrow);guideArrowRt=(RectTransform)arrow.transform;guideArrowRt.localRotation=Quaternion.Euler(0,0,45f);
            hand.SetActive(false);line.SetActive(false);arrow.SetActive(false);
            var ripple=Panel("TapRipple",playRoot.transform,new Color32(211,74,54,220),out rippleImage);rippleRt=(RectTransform)ripple.transform;ripple.SetActive(false);
        }

        void BuildEndUi()
        {
            var panel=Panel("EndPlacard",endRoot.transform,new Color32(242,232,207,248),out endPanelImage);endPanelRt=(RectTransform)panel.transform;
            var outline=panel.AddComponent<Outline>();outline.effectColor=new Color32(17,17,17,255);outline.effectDistance=new Vector2(7,-7);
            endTitleUi=Txt(panel.transform,"",new Vector2(.5f,.72f),Vector2.zero,36f,Ink,360f);endTitleUi.outlineWidth=.12f;endTitleUi.outlineColor=new Color32(181,138,69,220);
            endStatsUi=Txt(panel.transform,"",new Vector2(.5f,.42f),Vector2.zero,18f,Ink,350f);endStatsUi.textWrappingMode=TextWrappingModes.Normal;
            var stem=Panel("RestartCable",endRoot.transform,new Color32(181,138,69,245),out endCtaStemImage);endCtaStemRt=(RectTransform)stem.transform;
            var knob=Panel("RestartKnob",endRoot.transform,new Color32(211,74,54,255),out endCtaKnobImage);endCtaKnobRt=(RectTransform)knob.transform;
            var ko=knob.AddComponent<Outline>();ko.effectColor=new Color32(17,17,17,255);ko.effectDistance=new Vector2(5,-5);
            endCtaUi=Txt(endRoot.transform,"다시 공연하기",new Vector2(.5f,.11f),Vector2.zero,20f,Ivory,280f);endCtaUi.outlineWidth=.2f;endCtaUi.outlineColor=new Color32(17,17,17,255);
        }

        void RefreshProblemUi()
        {
            if(current==null)return;
            promptUi.text=(gamePhase==GamePhase.Practice?"연습 · ":"")+current.prompt;
            distanceUi.text="OA = "+st.selectedD+" cm";
            evidenceUi.text="등불 O · OA="+st.selectedD+" cm · 인형 AB  |  OC=12 cm · 스크린 CD\n△OAB∽△OCD · 대응 순서 A↔C, B↔D";
            SetRailCursor(st.selectedD);
        }

        void RefreshDistanceUi()
        {
            if(distanceUi)distanceUi.text="OA = "+st.selectedD+" cm";
            if(evidenceUi)evidenceUi.text="등불 O · OA="+st.selectedD+" cm · 인형 AB  |  OC=12 cm · 스크린 CD\n△OAB∽△OCD · 대응 순서 A↔C, B↔D";
            SetRailCursor(st.selectedD);
        }

        void ShowWrongReason(string id)
        {
            ShowToast(WrongReason(id),2.4f);
        }

        string WrongReason(string id)
        {
            if(id=="area_ratio_used_as_length_ratio")return "넓이의 비를 길이의 비에 그대로 쓰지 말고, 길이비를 제곱한 값인지 확인하시오.";
            if(id=="distance_equals_ratio_front")return "비의 앞항은 거리가 아니다. 12:d와 목표 비를 대응시키시오.";
            if(id=="inverse_scale_direction")return "거리가 멀어질수록 그림자는 작아진다. 이동 방향을 반대로 바꾸시오.";
            if(id=="one_tick_estimate")return "한 눈금 추측하지 말고 현재 비와 목표 비를 정수식으로 비교하시오.";
            string kind=current!=null&&current.kind==ShadowKind.AreaRatio?"넓이의 비 ":"길이의 비 ";
            bool tooLarge=ShadowRules.CompareActualToTarget(current,st.selectedD)>0;
            string relation=tooLarge?"보다 크다":"보다 작다";
            string move=tooLarge?"인형을 등불에서 더 멀리 옮기시오.":"인형을 등불 쪽으로 더 가까이 옮기시오.";
            return "현재 "+kind+ShadowRules.ActualRatio(current,st.selectedD)+"는 목표 "+current.targetRatio+relation+". "+move;
        }

        void ShowToast(string text,float sec)
        {
            toastUi.text=text;toastDuration=sec;toastClock=sec;toastGroup.alpha=1;
        }

        void RefuseInput(string text)
        {
            ShowToast(text,1.55f);MgfSfx.Play("wrong",.12f);
            if(curtainHandleRt)MgfFx.Punch(curtainHandleRt,.07f,.2f);
        }

        void ReplayGuide(bool strong)
        {
            guideDuration=strong?3.2f:4.2f;guideClock=guideDuration;
            guideHand.gameObject.SetActive(true);guideLine.gameObject.SetActive(true);guideArrow.gameObject.SetActive(true);
            float scale=strong?1.35f:1f;guideHandRt.localScale=Vector3.one*scale;
        }

        void BeginTitlePress(){titleLeverKnobImage.color=Ivory;MgfFx.Punch(titleLeverKnob,.10f,.2f);}
        void UpdateTitlePress(Vector2 pos){float y=Mathf.Clamp((Screen.height*.34f-pos.y)/Mathf.Max(64f,Screen.height*.12f),0,1);titleLeverKnob.anchoredPosition+=Vector2.down*y*.2f;}
        void EndTitlePress(){titleLeverKnobImage.color=Vermilion;}
        void UpdateCurtainPull(float pull){if(curtainHandleRt)curtainHandleRt.anchoredPosition=new Vector2(curtainHandleRt.anchoredPosition.x,curtainHandleBaseY()-pull*76f);}

        float curtainHandleBaseY(){return land?-10f:0f;}

        void SpawnTapRipple(Vector2 screen)
        {
            Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)playRoot.transform,screen,null,out local);
            rippleRt.anchoredPosition=local;rippleRt.sizeDelta=new Vector2(34f,34f);rippleRt.localScale=Vector3.one*.35f;rippleImage.color=new Color(Vermilion.r,Vermilion.g,Vermilion.b,.9f);rippleImage.gameObject.SetActive(true);
        }

        void ShowEnd(string reason)
        {
            string title=reason=="clear"?"앙코르 · 햇빛 새 비행":reason=="mastery"?"청사진은 완성됐지만 앙코르 보류":"폐막";
            string why=reason=="time"?"110초가 지나 채광벽이 닫혔다.":reason=="fuses"?"도자기 렌즈 세 개가 모두 깨졌다.":reason=="mastery"?"첫 시도 6/8, 마지막 3막 2/3을 채우면 앙코르가 열린다.":"여덟 청사진 패널이 햇빛 벽에 현상됐다.";
            endTitleUi.text=title;
            endWhy=why;endDisplayScore=0;RefreshEndStats(0);
            endPanelImage.color=reason=="clear"?new Color32(242,232,207,248):new Color32(206,194,171,248);
        }

        void RefreshEndStats(int shownScore)
        {
            endStatsUi.text=endWhy+"\n\n완성 막 "+st.solved+"/8  ·  첫 시도 "+st.firstAttemptCorrect+"/8\n후반 첫 시도 "+st.lateFirstCorrect+"/3  ·  점수 "+shownScore;
        }

        void SetPhaseVisuals()
        {
            bool title=gamePhase==GamePhase.Title;
            bool play=gamePhase==GamePhase.Practice||gamePhase==GamePhase.Playing||gamePhase==GamePhase.Reveal;
            bool end=gamePhase==GamePhase.End;
            titleRoot.SetActive(title);playRoot.SetActive(play);endRoot.SetActive(end);
            if(play&&current!=null)RefreshProblemUi();
            UpdateResponsiveLayout(true);
        }

        void UpdateUi(float dt)
        {
            displayScore=Mathf.MoveTowards(displayScore,st.score,Mathf.Max(80f,Mathf.Abs(st.score-displayScore)*5f)*dt);
            int timer=Mathf.CeilToInt(runLeft),score=Mathf.RoundToInt(displayScore);
            if(score!=lastHudScore||timer!=lastTimer||st.lives!=lastHudLives||st.solved!=lastHudSolved)
            {
                lastHudScore=score;lastTimer=timer;lastHudLives=st.lives;lastHudSolved=st.solved;
                if(hudUi)hudUi.text=(gamePhase==GamePhase.Practice?"연습 · 정지":"막 "+Math.Min(8,st.solved+1)+"/8 · "+timer+"초")+"\n"+score+"점 · 렌즈 "+Math.Max(0,st.lives);
                for(int i=0;i<fuseImages.Length;i++)if(fuseImages[i])fuseImages[i].color=i<st.lives?Vermilion:new Color(.20f,.18f,.16f,1f);
            }
            if(toastClock>0){toastClock-=dt;float life=Mathf.Max(.001f,toastDuration);toastGroup.alpha=Mathf.Clamp01(Mathf.Min(toastClock*5f,(life-toastClock)*8f));}else toastGroup.alpha=0;
            if(gamePhase==GamePhase.Reveal)revealGroup.alpha=Mathf.Clamp01(Mathf.Min(revealClock*8f,6f-(revealClock*3f)));else revealGroup.alpha=0;
            if(guideClock>0)
            {
                guideClock-=dt;float u=1f-(guideClock/Mathf.Max(.01f,guideDuration));float loop=(u*2.4f)%1f;
                bool curtainGuide=gamePhase==GamePhase.Practice&&practiceRailDone;
                Vector2 anchor=curtainGuide?new Vector2(.5f,.5f):new Vector2(.5f,0f);
                guideHandRt.anchorMin=guideHandRt.anchorMax=anchor;guideLineRt.anchorMin=guideLineRt.anchorMax=anchor;guideArrowRt.anchorMin=guideArrowRt.anchorMax=anchor;
                guideHandRt.sizeDelta=new Vector2(30f,30f);guideHandRt.localRotation=Quaternion.Euler(0f,0f,45f);
                if(curtainGuide)
                {
                    float x=curtainHandleRt.anchoredPosition.x,sy=curtainHandleBaseY()+40f,ey=curtainHandleBaseY()-92f;
                    guideHandRt.anchoredPosition=new Vector2(x+Mathf.Sin(loop*Mathf.PI)*9f,Mathf.Lerp(sy,ey,Smooth(loop)));
                    guideLineRt.anchoredPosition=new Vector2(x,(sy+ey)*.5f);guideLineRt.sizeDelta=new Vector2(4f,Mathf.Abs(ey-sy));
                    guideArrowRt.anchoredPosition=new Vector2(x,ey);guideArrowRt.sizeDelta=new Vector2(24f,24f);
                }
                else
                {
                    float sx=LogicalXForD(3),ex=LogicalXForD(6),y=RailLogicalY()+34f;
                    guideHandRt.anchoredPosition=new Vector2(Mathf.Lerp(sx,ex,Smooth(loop)),y+Mathf.Sin(loop*Mathf.PI)*13f);
                    guideLineRt.anchoredPosition=new Vector2((sx+ex)*.5f,y-7f);guideLineRt.sizeDelta=new Vector2(Mathf.Abs(ex-sx),4f);
                    guideArrowRt.anchoredPosition=new Vector2(ex,y-7f);guideArrowRt.sizeDelta=new Vector2(24f,24f);
                }
                float pulse=1f+.12f*Mathf.Sin(titleClock*6f);guideHandRt.localScale=Vector3.one*pulse*(guideDuration>3.5f?1f:1.25f);
            }
            else if(guideHand&&guideHand.gameObject.activeSelf){guideHand.gameObject.SetActive(false);guideLine.gameObject.SetActive(false);guideArrow.gameObject.SetActive(false);}
            if(rippleImage&&rippleImage.gameObject.activeSelf)
            {
                Color c=rippleImage.color;c.a=Mathf.Max(0,c.a-dt*2.5f);rippleImage.color=c;rippleRt.localScale+=Vector3.one*dt*2.8f;if(c.a<=.01f)rippleImage.gameObject.SetActive(false);
            }
            if(gamePhase==GamePhase.Title)
            {
                float p=.5f+.5f*Mathf.Sin(titleClock*2.2f);titleLeverKnob.localScale=Vector3.one*(1f+p*.045f);
                if(titleArt&&titleArt.texture)UpdateTitleUv();
            }
            // 레일 연습을 마치면 손잡이 자체가 반복해서 아래로 움직인다. 가이드 손만 움직여
            // 별개의 장식처럼 보였던 1차 수정의 학습 경로 불일치를 없앤다.
            if(gamePhase==GamePhase.Practice&&practiceRailDone&&!curtainDragging)
            {
                float demo=.18f+.62f*(.5f+.5f*Mathf.Sin(titleClock*4.2f));
                float y=curtainHandleBaseY()-demo*76f;
                curtainHandleRt.anchoredPosition=new Vector2(curtainHandleRt.anchoredPosition.x,y);
                leverUi.rectTransform.anchoredPosition=new Vector2(leverUi.rectTransform.anchoredPosition.x,y);
                curtainHandleRt.localScale=Vector3.one*(1.08f+.10f*Mathf.Sin(titleClock*4.2f));
            }
            else if(curtainHandleRt) curtainHandleRt.localScale=Vector3.one;
            if(gamePhase==GamePhase.End)
            {
                int before=Mathf.RoundToInt(endDisplayScore);
                endDisplayScore=Mathf.MoveTowards(endDisplayScore,st.score,Mathf.Max(120f,st.score*1.8f)*dt);
                int shown=Mathf.RoundToInt(endDisplayScore);if(shown!=before)RefreshEndStats(shown);
            }
        }

        static float Smooth(float x){return x*x*(3f-2f*x);}

        void UpdateResponsiveLayout(bool force=false)
        {
            RectTransform canvasRt=(RectTransform)MgfText.Canvas.transform;
            float w=Mathf.Max(1f,canvasRt.rect.width),h=Mathf.Max(1f,canvasRt.rect.height);
            if(!force&&Screen.width==lastScreenW&&Screen.height==lastScreenH&&Mathf.Abs(w-lastCanvasW)<.5f&&Mathf.Abs(h-lastCanvasH)<.5f)return;
            lastScreenW=Screen.width;lastScreenH=Screen.height;lastCanvasW=w;lastCanvasH=h;
            float aspect=Mathf.Max(.1f,(float)Screen.width/Mathf.Max(1,Screen.height));land=aspect>=1.2f;
            if(land)
            {
                cam.transform.position=new Vector3(0f,6.5f,-18.2f);cam.transform.LookAt(new Vector3(0f,1.65f,.55f));cam.fieldOfView=32f;
                worldRoot.localScale=Vector3.one;worldRoot.localPosition=new Vector3(0f,-.15f,0f);
                float safeW=Mathf.Min(1340f,w-150f);
                goalUi.fontSize=14.5f;promptUi.fontSize=15.5f;hudUi.fontSize=12.5f;
                // 상단 anchor 비율이 아니라 실제 카드 반높이+8 px를 safe top에서 뺀다.
                Rect(goalRt,new Vector2(.5f,1f),new Vector2(-safeW*.37f,-42f),new Vector2(Mathf.Min(370f,safeW*.28f),68f));
                Rect(promptRt,new Vector2(.5f,1f),new Vector2(0f,-56f),new Vector2(Mathf.Min(600f,safeW*.43f),96f));
                Rect(hudRt,new Vector2(.5f,1f),new Vector2(safeW*.35f,-39f),new Vector2(Mathf.Min(320f,safeW*.27f),62f));
                Rect(distanceRt,new Vector2(.50f,.31f),Vector2.zero,new Vector2(220f,46f));
                Rect(evidenceRt,new Vector2(.5f,.075f),Vector2.zero,new Vector2(Mathf.Min(760f,w*.62f),72f));
                Rect(toastRt,new Vector2(.5f,.54f),Vector2.zero,new Vector2(Mathf.Min(620f,w*.52f),64f));
                Rect(revealRt,new Vector2(.5f,.57f),Vector2.zero,new Vector2(Mathf.Min(700f,w*.55f),100f));
            }
            else
            {
                cam.transform.position=new Vector3(0f,7.8f,-18.3f);cam.transform.LookAt(new Vector3(0f,1.55f,.55f));
                MgfLook.FitWidth(cam,32f,1.42f);worldRoot.localScale=Vector3.one*.95f;worldRoot.localPosition=new Vector3(0f,-.05f,0f);
                goalUi.fontSize=15.5f;promptUi.fontSize=16f;hudUi.fontSize=13f;
                Rect(goalRt,new Vector2(.5f,.955f),new Vector2(-23f,0f),new Vector2(Mathf.Min(330f,w-62f),56f));
                Rect(promptRt,new Vector2(.5f,.855f),Vector2.zero,new Vector2(Mathf.Min(374f,w-16f),94f));
                Rect(hudRt,new Vector2(.5f,.775f),Vector2.zero,new Vector2(Mathf.Min(350f,w-38f),38f));
                Rect(distanceRt,new Vector2(.5f,.36f),Vector2.zero,new Vector2(200f,48f));
                Rect(evidenceRt,new Vector2(.5f,.07f),Vector2.zero,new Vector2(Mathf.Min(374f,w-16f),90f));
                Rect(toastRt,new Vector2(.5f,.55f),Vector2.zero,new Vector2(Mathf.Min(374f,w-16f),74f));
                Rect(revealRt,new Vector2(.5f,.56f),Vector2.zero,new Vector2(Mathf.Min(374f,w-16f),120f));
            }
            LayoutRailAndLever(w,h);LayoutTitle(w,h);LayoutEnd(w,h);UpdateTitleUv();AuditUiBounds();
        }

        void AuditUiBounds()
        {
            UnityEngine.Rect a=ScreenBounds(goalRt),b=ScreenBounds(promptRt),c=ScreenBounds(hudRt);
            bool inside=InsideScreen(a,6f)&&InsideScreen(b,6f)&&InsideScreen(c,6f);
            bool separated=!a.Overlaps(b)&&!b.Overlaps(c)&&!a.Overlaps(c);
            st.layoutSafe=inside&&(!land||separated);
            st.layoutInfo=Screen.width+"x"+Screen.height+" goal="+BoundsText(a)+" prompt="+BoundsText(b)+" hud="+BoundsText(c);
        }

        UnityEngine.Rect ScreenBounds(RectTransform rt)
        {
            Vector3[] corners=new Vector3[4];rt.GetWorldCorners(corners);
            Vector2 p0=RectTransformUtility.WorldToScreenPoint(null,corners[0]);
            Vector2 p2=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
            return UnityEngine.Rect.MinMaxRect(Mathf.Min(p0.x,p2.x),Mathf.Min(p0.y,p2.y),Mathf.Max(p0.x,p2.x),Mathf.Max(p0.y,p2.y));
        }

        bool InsideScreen(UnityEngine.Rect r,float margin)
        {return r.xMin>=margin&&r.yMin>=margin&&r.xMax<=Screen.width-margin&&r.yMax<=Screen.height-margin;}
        string BoundsText(UnityEngine.Rect r)
        {return Mathf.RoundToInt(r.xMin)+","+Mathf.RoundToInt(r.yMin)+","+Mathf.RoundToInt(r.xMax)+","+Mathf.RoundToInt(r.yMax);}

        void LayoutRailAndLever(float w,float h)
        {
            float railW=Mathf.Min(land?720f:w-58f,land?w*.58f:w-58f);float railY=land?h*.205f:h*.235f;
            Rect(railRt,new Vector2(.5f,0f),new Vector2(land?-w*.02f:0f,railY),new Vector2(railW,8f));
            float start=railRt.anchoredPosition.x-railW*.5f;
            for(int i=0;i<9;i++)
            {
                float x=start+railW*i/8f;
                Rect((RectTransform)tickImages[i].transform,new Vector2(.5f,0f),new Vector2(x,railY+6f),new Vector2(4f,i==4?28f:20f));
                Rect(tickLabels[i].rectTransform,new Vector2(.5f,0f),new Vector2(x,railY-22f),new Vector2(34f,32f));
            }
            Rect(railCursorRt,new Vector2(.5f,0f),new Vector2(LogicalXForD(st.selectedD),railY+22f),new Vector2(8f,46f));
            float leverX=land?w*.43f:w*.43f;
            Rect(curtainStemRt,new Vector2(.5f,.5f),new Vector2(leverX,land?-4f:-8f),new Vector2(8f,144f));
            Rect(curtainHandleRt,new Vector2(.5f,.5f),new Vector2(leverX,curtainHandleBaseY()-curtainPull*76f),new Vector2(68f,68f));
            curtainHandleRt.localRotation=Quaternion.Euler(0,0,45f);
            Rect(leverUi.rectTransform,new Vector2(.5f,.5f),new Vector2(leverX,curtainHandleBaseY()-curtainPull*76f),new Vector2(60f,48f));
            for(int i=0;i<3;i++)Rect((RectTransform)fuseImages[i].transform,new Vector2(.5f,.5f),new Vector2(leverX-50f+i*26f,land?88f:105f),new Vector2(18f,38f));
        }

        float RailLogicalY(){return railRt?railRt.anchoredPosition.y:180f;}
        float LogicalXForD(int d)
        {
            if(!railRt)return 0;float w=railRt.sizeDelta.x;return railRt.anchoredPosition.x-w*.5f+w*(d-2)/8f;
        }
        void SetRailCursor(int d){if(railCursorRt)railCursorRt.anchoredPosition=new Vector2(LogicalXForD(d),RailLogicalY()+22f);}

        void LayoutTitle(float w,float h)
        {
            float logoSize=land?58f:48f;titleLogo.fontSize=logoSize;titleLogoShadow.fontSize=logoSize;
            Rect(titleLogo.rectTransform,new Vector2(.5f,.77f),Vector2.zero,new Vector2(Mathf.Min(560f,w*.82f),80f));
            Rect(titleLogoShadow.rectTransform,new Vector2(.5f,.77f),new Vector2(7f,-7f),new Vector2(Mathf.Min(560f,w*.82f),80f));
            Rect(titleTag.rectTransform,new Vector2(.5f,.68f),Vector2.zero,new Vector2(Mathf.Min(420f,w*.82f),46f));
            Rect(titleMeta.rectTransform,new Vector2(.5f,.94f),Vector2.zero,new Vector2(Mathf.Min(460f,w-70f),34f));
            Rect(titleLeverStem,new Vector2(.5f,.18f),Vector2.zero,new Vector2(8f,116f));
            Rect(titleLeverKnob,new Vector2(.5f,.135f),Vector2.zero,new Vector2(82f,82f));titleLeverKnob.localRotation=Quaternion.Euler(0,0,45f);
            Rect(titleLeverText.rectTransform,new Vector2(.5f,.055f),Vector2.zero,new Vector2(Mathf.Min(380f,w-32f),42f));
        }

        void LayoutEnd(float w,float h)
        {
            Rect(endPanelRt,new Vector2(.5f,.58f),Vector2.zero,new Vector2(Mathf.Min(570f,w-28f),Mathf.Min(390f,h*.48f)));
            Rect(endCtaStemRt,new Vector2(.5f,.20f),Vector2.zero,new Vector2(8f,100f));
            Rect(endCtaKnobRt,new Vector2(.5f,.16f),Vector2.zero,new Vector2(76f,76f));endCtaKnobRt.localRotation=Quaternion.Euler(0,0,45f);
            Rect(endCtaUi.rectTransform,new Vector2(.5f,.075f),Vector2.zero,new Vector2(300f,46f));
        }

        void UpdateTitleUv()
        {
            if(!titleArt||!titleArt.texture)return;
            float view=(float)Mathf.Max(1,Screen.width)/Mathf.Max(1,Screen.height);
            float img=(float)titleArt.texture.width/titleArt.texture.height;
            if(view<img){float uw=view/img;titleArt.uvRect=new Rect((1f-uw)*.5f,0f,uw,1f);}else{float uh=img/view;titleArt.uvRect=new Rect(0f,(1f-uh)*.5f,1f,uh);}
        }

        bool IsTitleStartZone(Vector2 p){return p.x>=0f&&p.x<=Screen.width&&p.y>=0f&&p.y<=Screen.height;}
        bool IsEndRestartZone(Vector2 p){return p.x>=Screen.width*.16f&&p.x<=Screen.width*.84f&&p.y<=Screen.height*.34f;}
        bool IsCurtainZone(Vector2 p){return p.x>=Screen.width*.78f&&p.y>=Screen.height*.20f&&p.y<=Screen.height*.72f;}
        bool IsRailZone(Vector2 p){return p.x>=Screen.width*.06f&&p.x<=Screen.width*.84f&&p.y>=Screen.height*.12f&&p.y<=Screen.height*.58f;}
        int DFromPointer(Vector2 p){float u=Mathf.InverseLerp(Screen.width*.12f,Screen.width*.82f,p.x);return Math.Max(2,Math.Min(10,2+Mathf.RoundToInt(u*8f)));}

        GameObject Root(string name,Transform parent){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);Stretch((RectTransform)go.transform);return go;}
        GameObject Panel(string name,Transform parent,Color color,out Image image){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return go;}
        TextMeshProUGUI Txt(Transform parent,string text,Vector2 anchor,Vector2 offset,float size,Color color,float width)
        {var go=new GameObject("Text",typeof(RectTransform));go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;Rect(rt,anchor,offset,new Vector2(width,size*2.2f));var t=go.AddComponent<TextMeshProUGUI>();t.font=MgfText.Font;t.text=text;t.fontSize=size;t.fontStyle=FontStyles.Bold;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;}
        static void Stretch(RectTransform rt){rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;}
        static void StretchInset(RectTransform rt,float inset){rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=Vector2.one*inset;rt.offsetMax=Vector2.one*-inset;}
        static void Rect(RectTransform rt,Vector2 anchor,Vector2 offset,Vector2 size){rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=offset;rt.sizeDelta=size;}
    }
}
