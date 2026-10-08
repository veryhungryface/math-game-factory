using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mgf;

namespace Mgf.DiceCaravan
{
    public partial class DiceCaravanGame
    {
        static readonly Color Salt = new Color32(239, 232, 211, 255);
        static readonly Color Indigo = new Color32(47, 66, 82, 255);
        static readonly Color Ice = new Color32(190, 218, 207, 255);
        static readonly Color Brass = new Color32(174, 125, 75, 255);
        static readonly Color Night = new Color32(25, 36, 45, 255);
        static readonly Color Error = new Color32(194, 99, 96, 255);
        static readonly Color Lake = new Color32(13, 32, 48, 255);
        static readonly Color Moon = new Color32(202, 231, 224, 255);
        static readonly Color Terracotta = new Color32(170, 76, 55, 255);
        static readonly Color Saffron = new Color32(232, 171, 72, 255);
        static readonly Color MarketBlue = new Color32(32, 79, 101, 255);

        Camera cam;
        Transform worldRoot, caravanRoot, lumaWorld;
        readonly Transform[] sails = new Transform[6];
        readonly Vector3[] sailBaseScale = new Vector3[6];
        readonly Transform[] wheels = new Transform[12];
        readonly Transform[] saltFlows = new Transform[22];
        readonly Vector3[] saltFlowStart = new Vector3[22];
        readonly Renderer[] lampGlass = new Renderer[3];
        readonly Renderer[] routeGateLights = new Renderer[5];
        readonly Transform[] marketFigures = new Transform[14];
        readonly Vector3[] marketFigureBase = new Vector3[14];
        Material saltMat, indigoMat, brassMat, iceMat, sailMat, glassMat, lampOnMat, lampOffMat, lakeMat, flowMat;
        Material terracottaMat, saffronMat, marketBlueMat, darkMarketMat;
        Vector3 caravanBase;
        Transform routeGateRoot, routeGateArm;
        float caravanTravel, caravanTarget, caravanBranch, caravanBranchTarget, worldTime, sailPulse, wrongPulse, cameraPulse;
        float routeEvent, lumaRun;

        RectTransform uiRoot, titleRt, gameRt, endRt, boardRt, cordRt, titleCoverRt, restartRt;
        RectTransform promptRt, revealRt, toastRt, guideRt, rippleRt, lumaRt;
        RectTransform practiceHintRt, practiceTrailRt;
        RectTransform titleCoachRt, titleCoachArrowRt, titleCoachTargetRt;
        readonly RectTransform[] titleCoachDots = new RectTransform[7];
        readonly RectTransform[] practiceTrailDots = new RectTransform[7];
        CanvasGroup titleG, gameG, endG, revealG, toastG;
        CanvasGroup titleCoachG;
        CanvasScaler canvasScaler;
        Image titleCoverImage, boardPlate, cordImage, revealPlate, guideImage, rippleImage, practiceHintPlate;
        TextMeshProUGUI titleLogo, titleTag, titleMeta, titleCoverText;
        TextMeshProUGUI scoreText, livesText, progressText, timerText, bandText, goalText, promptText, selectedText;
        TextMeshProUGUI revealText, toastText, endTitle, endStats, endCta, practiceHintText;
        readonly RectTransform[] pinRt = new RectTransform[36];
        readonly Image[] pinImages = new Image[36];
        readonly TextMeshProUGUI[] pinTexts = new TextMeshProUGUI[36];
        readonly RectTransform[] threadRt = new RectTransform[35];
        readonly Image[] threadImages = new Image[35];
        Sprite roundSprite, ringSprite, panelSprite, softSprite, tagSprite;
        bool layoutWide;
        int activePins;
        float rippleClock = 9f, toastClock = 9f, guideBoost, cordPull, refuseClock = 9f, titleCoachClock = 9f;
        string practiceHintCache = "";

        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        Image Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>(); image.raycastTarget = false; image.color = color;
            image.sprite = sprite; if (sprite && sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
            return image;
        }

        TextMeshProUGUI Txt(string name, Transform parent, string text, Vector2 anchor, Vector2 pos, float size, Color color, float width = 360f, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = R(name, parent, anchor, pos, new Vector2(width, size * 2.4f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.fontStyle = FontStyles.Bold; t.color = color;
            t.alignment = align; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false; t.text = text; return t;
        }

        Sprite MakeSprite(bool ring)
        {
            const int S = 96;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = ring ? "StarRing" : "GlassDisc" };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = (x + .5f) / S * 2f - 1f, dy = (y + .5f) / S * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = ring ? Mathf.Clamp01((.95f - d) * 18f) * Mathf.Clamp01((d - .69f) * 20f) : Mathf.Clamp01((.96f - d) * 12f);
                if (!ring) a *= Mathf.Lerp(.78f, 1f, Mathf.Clamp01((-.2f - dy) * .7f));
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        Sprite MakePanelSprite()
        {
            const int S = 96;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "EnamelPanel" };
            var px = new Color32[S * S];
            const float radius = 18f;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + .5f - S * .5f) - (S * .5f - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + .5f - S * .5f) - (S * .5f - radius), 0f);
                float a = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px); t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(22, 22, 22, 22));
        }

        Sprite MakeTagSprite()
        {
            const int S = 96;
            var texture = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Cut-corner route tag" };
            var pixels = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float ax = Mathf.Abs((x + .5f) / S * 2f - 1f);
                float ay = Mathf.Abs((y + .5f) / S * 2f - 1f);
                float edge = Mathf.Max(ax, ay) + Mathf.Min(ax, ay) * .34f;
                float alpha = Mathf.Clamp01((1f - edge) * 18f);
                pixels[y * S + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f);
        }

        Texture2D MakeSurfaceTexture(Color32 baseColor, int pattern)
        {
            const int S = 64;
            var texture = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                name = pattern == 0 ? "Woven indigo" : pattern == 1 ? "Cracked ceramic" : "Oxidized brass",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float tone;
                if (pattern == 0)
                {
                    bool warp = x % 5 == 0, weft = y % 4 == 0;
                    tone = warp && weft ? 1.24f : warp || weft ? .86f : 1f;
                }
                else if (pattern == 1)
                {
                    int vein = (x * 11 + y * 17 + (x / 9) * 23) % 47;
                    tone = vein < 2 || Mathf.Abs((x % 19) - (y % 19)) < 1 ? .55f : .98f + ((x + y) % 7) * .008f;
                }
                else
                {
                    float blotch = Mathf.Sin(x * .29f) * Mathf.Cos(y * .23f) + Mathf.Sin((x + y) * .13f);
                    tone = blotch > .72f ? .48f : blotch < -.82f ? 1.16f : .91f;
                }
                pixels[y * S + x] = new Color32(
                    (byte)Mathf.Clamp(baseColor.r * tone, 0, 255),
                    (byte)Mathf.Clamp(baseColor.g * tone, 0, 255),
                    (byte)Mathf.Clamp(baseColor.b * tone, 0, 255), 255);
            }
            texture.SetPixels32(pixels); texture.Apply(false, true); return texture;
        }

        void BuildWorld()
        {
            // CreatePrimitive(Cylinder) adds CapsuleCollider by name internally. Explicit references keep
            // collider types from being stripped out of the WebGL player.
            var colliderTypes = new GameObject("Collider type keeper");
            colliderTypes.AddComponent<CapsuleCollider>(); colliderTypes.AddComponent<SphereCollider>(); colliderTypes.AddComponent<BoxCollider>(); colliderTypes.AddComponent<MeshCollider>();
            Object.Destroy(colliderTypes);
            MgfLook.Sky(new Color32(5, 12, 30, 255), new Color32(22, 61, 78, 255), new Color32(42, 91, 94, 255), .66f);
            MgfLook.Sun(new Vector3(36, -28, 12), new Color(.70f, .89f, 1f), 1.18f, .76f);
            RenderSettings.ambientLight = new Color(.16f, .25f, .31f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.035f, .12f, .18f); RenderSettings.fogDensity = .010f;
            cam = MgfLook.Camera(new Vector3(9.6f, 9.8f, -14.6f), new Vector3(0, .5f, 0), 40f);
            cam.backgroundColor = Night;

            // A tactile miniature made from chalky salt, woven cloth and pressed
            // ceramic.  There is intentionally no emissive transparent-glass
            // material: that was the catalogue-family collision called out in review.
            var weaveTex = MakeSurfaceTexture(new Color32(83, 115, 135, 255), 0);
            var ceramicTex = MakeSurfaceTexture(new Color32(221, 213, 183, 255), 1);
            var oxideTex = MakeSurfaceTexture(new Color32(174, 125, 75, 255), 2);
            saltMat = MgfLook.Lit(Color.white, .24f, .01f, null, ceramicTex);
            indigoMat = MgfLook.Lit(new Color32(25, 54, 69, 255), .28f, .07f);
            brassMat = MgfLook.Lit(Color.white, .36f, .32f, null, oxideTex);
            iceMat = MgfLook.Lit(Ice, .22f, .01f);
            sailMat = MgfLook.Lit(Color.white, .12f, .01f, null, weaveTex);
            glassMat = MgfLook.Lit(Color.white, .28f, .02f, null, ceramicTex);
            lampOnMat = MgfLook.Lit(new Color32(239, 185, 96, 255), .42f, .08f, new Color(.14f, .065f, .01f));
            lampOffMat = MgfLook.Lit(new Color32(72, 76, 83, 255), .45f, .25f);
            lakeMat = MgfLook.Lit(Lake, .86f, .18f, new Color(.006f, .02f, .03f));
            flowMat = MgfLook.Lit(Moon, .52f, .08f, new Color(.025f, .08f, .075f));
            terracottaMat = MgfLook.Lit(Terracotta, .24f, .04f);
            saffronMat = MgfLook.Lit(Saffron, .31f, .05f, new Color(.035f, .018f, .002f));
            marketBlueMat = MgfLook.Lit(MarketBlue, .27f, .06f);
            darkMarketMat = MgfLook.Lit(new Color32(17, 27, 35, 255), .38f, .12f);

            worldRoot = new GameObject("MoonlitSaltWorld").transform;
            var ground = MgfLook.Block("Reflective midnight brine", new Vector3(0, -.50f, 1.5f), new Vector3(38, .26f, 24), .12f, lakeMat, worldRoot);
            ground.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = -5; i <= 5; i++)
            {
                float x = i * 4.15f;
                var island = MgfLook.Block("Broken salt horizon", new Vector3(x, .08f + (Mathf.Abs(i) % 3) * .08f, 10.4f + Mathf.Sin(i * 1.7f) * .55f),
                    new Vector3(3.35f + (Mathf.Abs(i) % 2) * .7f, .28f + (Mathf.Abs(i) % 3) * .12f, 1.0f + (Mathf.Abs(i) % 4) * .18f), .13f, saltMat, worldRoot);
                island.transform.rotation = Quaternion.Euler(0, i * 7f, i % 2 == 0 ? 1.5f : -1.5f);
            }
            var moon = MgfLook.Prim(PrimitiveType.Sphere, "Low salt moon", new Vector3(-13.5f, 9.8f, 8.5f), Vector3.one * 1.7f,
                MgfLook.Lit(Moon, .64f, .02f, new Color(.22f, .32f, .28f)), worldRoot, false);
            moon.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int i = 0; i < saltFlows.Length; i++)
            {
                float x = -17f + (i * 47 % 340) / 10f;
                float z = -7f + (i * 71 % 175) / 10f;
                var streak = MgfLook.Block("Drifting salt current", new Vector3(x, -.28f + (i % 3) * .025f, z),
                    new Vector3(.07f + (i % 4) * .035f, .025f, .24f + (i % 6) * .095f), .02f, flowMat, worldRoot);
                streak.transform.rotation = Quaternion.Euler(i % 3 - 1f, i * 47f % 180f, i % 2 == 0 ? 5f : -6f);
                saltFlows[i] = streak.transform; saltFlowStart[i] = streak.transform.localPosition;
            }

            BuildSaltMarket();

            caravanRoot = new GameObject("Caravan procession").transform; caravanRoot.SetParent(worldRoot, false);
            caravanBase = new Vector3(-2.4f, 0, 1.4f); caravanRoot.position = caravanBase;
            for (int i = 0; i < 3; i++) BuildWagon(i, new Vector3((i - 1) * 4.2f, 0, (i % 2) * .45f));
            BuildLuma();
        }

        void BuildSaltMarket()
        {
            // A layered, inhabited dispatch yard replaces the empty floating
            // plane shared by earlier middle-school dioramas.
            for (int level = 0; level < 3; level++)
            {
                float z = 5.2f + level * 1.85f;
                float y = .10f + level * .72f;
                MgfLook.Block("Terraced salt quay " + level, new Vector3(0, y, z), new Vector3(25f - level * 2.7f, .58f, 1.55f), .09f,
                    level % 2 == 0 ? terracottaMat : marketBlueMat, worldRoot);
                for (int stall = 0; stall < 5; stall++)
                {
                    float x = -8f + stall * 4f + (level % 2) * .65f;
                    var root = new GameObject("Layered market stall " + level + "-" + stall).transform;
                    root.SetParent(worldRoot, false); root.localPosition = new Vector3(x, y + .52f, z - .1f);
                    MgfLook.Block("Angular stall body", Vector3.zero, new Vector3(2.15f, .78f, 1.08f), .055f, darkMarketMat, root);
                    var roof = MgfLook.Block("Folded enamel awning", new Vector3(0, .72f, -.02f), new Vector3(2.38f, .18f, 1.3f), .025f,
                        (stall + level) % 3 == 0 ? saffronMat : (stall + level) % 3 == 1 ? terracottaMat : marketBlueMat, root);
                    roof.transform.localRotation = Quaternion.Euler(0, 0, stall % 2 == 0 ? 4f : -4f);
                    for (int crate = 0; crate < 2; crate++)
                        MgfLook.Block("Route parcel", new Vector3(-.55f + crate * 1.1f, .45f, -.7f), new Vector3(.55f, .55f + crate * .13f, .45f), .035f,
                            crate == 0 ? brassMat : saltMat, root);
                }
            }

            // Twin rails and a branching gate make the correct event a route
            // opening rather than another piece of cloth unfolding.
            for (int rail = 0; rail < 2; rail++)
            {
                float z = -.55f + rail * 1.65f;
                MgfLook.Block("Dispatch rail " + rail, new Vector3(0, -.27f, z), new Vector3(29f, .09f, .12f), .025f, brassMat, worldRoot);
                var branch = MgfLook.Block("Branch rail " + rail, new Vector3(6.7f, -.25f, z + .72f), new Vector3(9.5f, .08f, .11f), .02f, brassMat, worldRoot);
                branch.transform.rotation = Quaternion.Euler(0, rail == 0 ? -12f : 12f, 0);
            }
            for (int sleeper = 0; sleeper < 22; sleeper++)
                MgfLook.Block("Salt sleeper " + sleeper, new Vector3(-13f + sleeper * 1.25f, -.30f, .27f), new Vector3(.11f, .07f, 2.45f), .015f,
                    sleeper % 2 == 0 ? terracottaMat : darkMarketMat, worldRoot);

            var gate = new GameObject("Five-lamp dispatch gate").transform; gate.SetParent(worldRoot, false); gate.localPosition = new Vector3(.8f, 0, .25f);
            routeGateRoot = gate;
            MgfLook.Block("Gate left", new Vector3(-1.8f, 1.45f, 0), new Vector3(.28f, 3f, .35f), .04f, darkMarketMat, gate);
            MgfLook.Block("Gate right", new Vector3(1.8f, 1.45f, 0), new Vector3(.28f, 3f, .35f), .04f, darkMarketMat, gate);
            MgfLook.Block("Gate header", new Vector3(0, 2.75f, 0), new Vector3(3.9f, .38f, .48f), .05f, terracottaMat, gate);
            routeGateArm = MgfLook.Block("Route semaphore", new Vector3(-1.55f, 1.4f, -.25f), new Vector3(3.15f, .16f, .22f), .03f, saffronMat, gate).transform;
            routeGateArm.localRotation = Quaternion.Euler(0, 0, -4f);
            for (int i = 0; i < routeGateLights.Length; i++)
            {
                var light = MgfLook.Prim(PrimitiveType.Cube, "Route lamp " + (i + 1), new Vector3(-1.2f + i * .6f, 2.75f, -.3f),
                    new Vector3(.34f, .34f, .16f), lampOffMat, gate, false);
                light.transform.localRotation = Quaternion.Euler(0, 0, 45f); routeGateLights[i] = light.GetComponent<Renderer>();
            }

            for (int i = 0; i < marketFigures.Length; i++)
            {
                float x = -9.2f + (i * 29 % 185) / 10f;
                float z = 4.4f + (i % 3) * 1.82f;
                float y = .55f + (i % 3) * .72f;
                var figure = new GameObject("Salt-market porter " + i).transform; figure.SetParent(worldRoot, false);
                figure.localPosition = new Vector3(x, y, z);
                MgfLook.Prim(PrimitiveType.Capsule, "Porter body", Vector3.zero, new Vector3(.24f, .38f, .24f),
                    i % 3 == 0 ? saffronMat : i % 3 == 1 ? terracottaMat : marketBlueMat, figure, false);
                MgfLook.Prim(PrimitiveType.Sphere, "Porter head", new Vector3(0, .48f, 0), Vector3.one * .25f, saltMat, figure, false);
                marketFigures[i] = figure; marketFigureBase[i] = figure.localPosition;
            }
        }

        void BuildWagon(int wagon, Vector3 pos)
        {
            var root = new GameObject("Star wagon " + (wagon + 1)).transform; root.SetParent(caravanRoot, false); root.localPosition = pos;
            MgfLook.Block("Cracked ceramic cart", new Vector3(0, .55f, 0), new Vector3(3.05f, .78f, 1.85f), .16f, indigoMat, root);
            MgfLook.Block("Oxidized brass rim", new Vector3(0, 1.02f, 0), new Vector3(3.18f, .12f, 1.98f), .035f, brassMat, root);
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? -1f : 1f;
                for (int j = 0; j < 2; j++)
                {
                    int wi = wagon * 4 + side * 2 + j;
                    var wheel = MgfLook.Prim(PrimitiveType.Cylinder, "Pressed ceramic wheel", new Vector3(j == 0 ? -1f : 1f, .25f, z), new Vector3(.62f, .16f, .62f), glassMat, root);
                    wheel.transform.localRotation = Quaternion.Euler(90, 0, 0); wheels[wi] = wheel.transform;
                }
            }
            MgfLook.Block("Mast", new Vector3(-.6f, 2.6f, 0), new Vector3(.12f, 3.4f, .12f), .04f, brassMat, root);
            var sail = MgfLook.Prim(PrimitiveType.Quad, "Woven constellation sail", new Vector3(.45f, 2.8f, 0), new Vector3(2.25f, 2.55f, 1), sailMat, root, false);
            sail.transform.localRotation = Quaternion.Euler(0, 0, -7f); sails[wagon] = sail.transform; sailBaseScale[wagon] = sail.transform.localScale;
            var reverse = MgfLook.Prim(PrimitiveType.Quad, "Sail reverse", new Vector3(.44f, 2.8f, .012f), new Vector3(2.25f, 2.55f, 1), sailMat, root, false);
            reverse.transform.localRotation = Quaternion.Euler(0, 180, 7f); sails[wagon + 3] = reverse.transform; sailBaseScale[wagon + 3] = reverse.transform.localScale;
            for (int s = 0; s < 6; s++)
            {
                float a = s / 6f * Mathf.PI * 2;
                MgfLook.Prim(PrimitiveType.Sphere, "Sail star", new Vector3(.35f + Mathf.Cos(a) * .62f, 2.78f + Mathf.Sin(a) * .82f, -.035f), Vector3.one * .11f, glassMat, root, false);
            }
            var lamp = MgfLook.Prim(PrimitiveType.Sphere, "Wind lamp", new Vector3(-1.15f + wagon * .05f, 1.45f, -.9f), Vector3.one * .32f, lampOnMat, root, false);
            lampGlass[wagon] = lamp.GetComponent<Renderer>();
            MgfLook.Block("Wind bell", new Vector3(1.25f, 1.42f, -.93f), new Vector3(.18f, .44f, .18f), .08f, brassMat, root);
        }

        void BuildLuma()
        {
            lumaWorld = new GameObject("Luma star gecko").transform; lumaWorld.SetParent(caravanRoot, false); lumaWorld.localPosition = new Vector3(-.2f, 1.68f, -.98f); lumaWorld.localScale = Vector3.one * 1.22f;
            var body = MgfLook.Prim(PrimitiveType.Sphere, "Luma body", Vector3.zero, new Vector3(.68f, .5f, .45f), indigoMat, lumaWorld, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Luma head", new Vector3(.5f, .2f, -.03f), new Vector3(.48f, .43f, .42f), indigoMat, lumaWorld, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Luma eye", new Vector3(.69f, .28f, -.36f), Vector3.one * .11f, glassMat, lumaWorld, false);
            for (int i = 0; i < 4; i++) MgfLook.Prim(PrimitiveType.Sphere, "Tail star", new Vector3(-.5f - i * .28f, .03f + Mathf.Sin(i) * .1f, 0), Vector3.one * (.34f - i * .04f), i % 2 == 0 ? iceMat : indigoMat, lumaWorld, false);
            body.transform.localRotation = Quaternion.Euler(0, 12, 0);
        }

        void BuildUi()
        {
            roundSprite = MakeSprite(false); ringSprite = MakeSprite(true); panelSprite = MakePanelSprite();
            tagSprite = MakeTagSprite();
            softSprite = Sprite.Create(MgfLook.SoftDot, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64f);
            uiRoot = R("DiceCaravan UI", MgfText.Canvas.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(390, 844));
            uiRoot.anchorMin = Vector2.zero; uiRoot.anchorMax = Vector2.one; uiRoot.offsetMin = uiRoot.offsetMax = Vector2.zero;
            canvasScaler = MgfText.Canvas.GetComponent<CanvasScaler>();
            BuildTitleUi(); BuildGameUi(); BuildEndUi(); BuildOverlays();
        }

        void BuildTitleUi()
        {
            titleRt = R("Title world gate", uiRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one; titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;
            titleG = titleRt.gameObject.AddComponent<CanvasGroup>();
            // A narrow depot sign hangs over the inhabited market.  It reads as
            // route hardware, not the familiar floating title-card template.
            var depotSign = Img("Salt market route sign", titleRt, new Vector2(.5f, .86f), Vector2.zero, new Vector2(326, 96),
                new Color(Terracotta.r, Terracotta.g, Terracotta.b, .94f), tagSprite);
            Img("Route sign rail", depotSign.transform, new Vector2(.5f, .96f), Vector2.zero, new Vector2(278, 6), Saffron, softSprite);
            Txt("Dispatch bay", depotSign.transform, "소금시장 7번 배차구", new Vector2(.5f, .76f), Vector2.zero, 12, new Color32(255, 222, 144, 255), 280);
            titleLogo = Txt("Constellation logo", depotSign.transform, "다이스 캐러밴", new Vector2(.5f, .39f), Vector2.zero, 31, Salt, 310);
            titleLogo.characterSpacing = 2; titleLogo.outlineColor = Night; titleLogo.outlineWidth = .17f;
            titleTag = Txt("Tagline", titleRt, "조건에 맞는 핀을 모두 꽂아라", new Vector2(.5f, .755f), Vector2.zero, 16, Moon, 330);
            titleTag.outlineColor = Night; titleTag.outlineWidth = .14f;
            titleMeta = Txt("Unit and record", titleRt, "중2 · 경우의 수   ◇   최고 첫 시도 0/6", new Vector2(.5f, .705f), Vector2.zero, 13, Salt, 350);
            titleMeta.outlineColor = Night; titleMeta.outlineWidth = .14f;

            // The naive first-play harness taps the screen centre.  Place the
            // visible physical cover there so the first intended gesture and
            // its hit target are the same object.
            titleCoverImage = Img("Actual dispatch-board cover", titleRt, new Vector2(.5f, .49f), Vector2.zero, new Vector2(224, 138), new Color(MarketBlue.r, MarketBlue.g, MarketBlue.b, .97f), tagSprite);
            titleCoverRt = titleCoverImage.rectTransform;
            Img("Cover brass track", titleCoverRt, new Vector2(.5f, .72f), Vector2.zero, new Vector2(174, 8), Saffron, softSprite);
            Img("Cover brass track lower", titleCoverRt, new Vector2(.5f, .28f), Vector2.zero, new Vector2(174, 8), Saffron, softSprite);
            titleCoverText = Txt("Cover instruction", titleCoverRt, "배차 성도판\n덮개 밀어 열기", new Vector2(.5f, .5f), Vector2.zero, 19, Salt, 196);
            titleCoverText.rectTransform.sizeDelta = new Vector2(190, 86); titleCoverText.overflowMode = TextOverflowModes.Overflow;
        }

        void BuildGameUi()
        {
            gameRt = R("Play overlay", uiRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            gameRt.anchorMin = Vector2.zero; gameRt.anchorMax = Vector2.one; gameRt.offsetMin = gameRt.offsetMax = Vector2.zero;
            gameG = gameRt.gameObject.AddComponent<CanvasGroup>();

            var hud = Img("Moon gauge", gameRt, new Vector2(.5f, .955f), Vector2.zero, new Vector2(340, 54), new Color(Night.r, Night.g, Night.b, .84f), panelSprite).rectTransform;
            scoreText = Txt("Voyage clock", hud, "전체 1:30", new Vector2(.14f, .5f), Vector2.zero, 13, Salt, 84);
            livesText = Txt("Wind lamps", hud, "◆ ◆ ◆", new Vector2(.43f, .5f), Vector2.zero, 18, Brass, 96);
            progressText = Txt("Route", hud, "항로 0/6", new Vector2(.67f, .5f), Vector2.zero, 17, Ice, 88);
            timerText = Txt("Problem clock", hud, "문항 26초", new Vector2(.88f, .5f), Vector2.zero, 12, Salt, 72);
            bandText = Txt("Band", gameRt, "별지도 연습", new Vector2(.5f, .895f), Vector2.zero, 14, Brass, 340);

            var promptPlate = Img("Woven prompt sail", gameRt, new Vector2(.5f, .795f), Vector2.zero, new Vector2(370, 146), new Color(Lake.r, Lake.g, Lake.b, .94f), panelSprite);
            promptRt = promptPlate.rectTransform;
            Img("Prompt sail stitch", promptRt, new Vector2(.5f, .04f), Vector2.zero, new Vector2(330, 4), new Color(Brass.r, Brass.g, Brass.b, .74f), softSprite);
            goalText = Txt("Always visible goal", promptRt, "조건에 맞는 핀을 모두 꽂으시오", new Vector2(.5f, .80f), Vector2.zero, 14, Brass, 350);
            promptText = Txt("Problem", promptRt, "", new Vector2(.5f, .37f), Vector2.zero, 15, Salt, 348);
            promptText.lineSpacing = -6; promptText.overflowMode = TextOverflowModes.Overflow; promptText.maxVisibleLines = 4;

            boardPlate = Img("Angular dispatch manifest", gameRt, new Vector2(.5f, .425f), Vector2.zero, new Vector2(346, 346), new Color(Indigo.r, Indigo.g, Indigo.b, .98f), tagSprite);
            boardRt = boardPlate.rectTransform;
            Img("Manifest top rail", boardRt, new Vector2(.5f, .965f), Vector2.zero, new Vector2(292, 7), new Color(Saffron.r, Saffron.g, Saffron.b, .86f), softSprite);
            Img("Manifest bottom rail", boardRt, new Vector2(.5f, .035f), Vector2.zero, new Vector2(292, 7), new Color(Saffron.r, Saffron.g, Saffron.b, .86f), softSprite);

            for (int i = 0; i < threadRt.Length; i++)
            {
                threadImages[i] = Img("Star thread " + i, boardRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(4, 20), new Color(Ice.r, Ice.g, Ice.b, .82f), softSprite);
                threadRt[i] = threadImages[i].rectTransform; threadRt[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < 36; i++)
            {
                pinImages[i] = Img("Enamel route tag " + i, boardRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(50, 50), new Color(Salt.r, Salt.g, Salt.b, .96f), tagSprite);
                pinRt[i] = pinImages[i].rectTransform;
                Img("Tag route slot", pinRt[i], new Vector2(.5f, .88f), Vector2.zero, new Vector2(34, 5), new Color(Brass.r, Brass.g, Brass.b, .90f), softSprite);
                pinTexts[i] = Txt("Pin label", pinRt[i], "", new Vector2(.5f, .5f), Vector2.zero, 13, Indigo, 46);
            }

            selectedText = Txt("Selected pin count", gameRt, "핀 0개", new Vector2(.5f, .18f), Vector2.zero, 15, Ice, 160);
            cordImage = Img("Physical dispatch handle", gameRt, new Vector2(.5f, .09f), Vector2.zero, new Vector2(84, 92), new Color(Terracotta.r, Terracotta.g, Terracotta.b, .98f), tagSprite);
            cordRt = cordImage.rectTransform;
            Img("Cord stem", cordRt, new Vector2(.5f, 1f), new Vector2(0, 35), new Vector2(10, 88), Brass, softSprite);
            Txt("Cord label", cordRt, "당겨\n제출", new Vector2(.5f, .5f), Vector2.zero, 17, Indigo, 68);

            var lumaBadge = Img("Luma route badge", gameRt, new Vector2(.16f, .20f), Vector2.zero, new Vector2(82, 68), new Color(Saffron.r, Saffron.g, Saffron.b, .92f), tagSprite);
            lumaRt = lumaBadge.rectTransform;
            Txt("Luma star mark", lumaRt, "별", new Vector2(.5f, .5f), Vector2.zero, 23, Indigo, 58);
        }

        void BuildEndUi()
        {
            endRt = R("Result sail", uiRoot, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            endRt.anchorMin = Vector2.zero; endRt.anchorMax = Vector2.one; endRt.offsetMin = endRt.offsetMax = Vector2.zero;
            endG = endRt.gameObject.AddComponent<CanvasGroup>();
            Img("Result moon veil", endRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(440, 920), new Color(Night.r, Night.g, Night.b, .80f), softSprite);
            var sail = Img("Dispatch result card", endRt, new Vector2(.5f, .52f), Vector2.zero, new Vector2(356, 500), new Color(Indigo.r, Indigo.g, Indigo.b, .98f), tagSprite);
            Img("Result route seal", sail.transform, new Vector2(.5f, .82f), Vector2.zero, new Vector2(112, 112), new Color(Saffron.r, Saffron.g, Saffron.b, .76f), tagSprite);
            endTitle = Txt("Result title", sail.transform, "항해 완료", new Vector2(.5f, .82f), Vector2.zero, 31, Salt, 330);
            var statsPlate = Img("Opaque result manifest", sail.transform, new Vector2(.5f, .48f), Vector2.zero, new Vector2(314, 224), new Color(Night.r, Night.g, Night.b, .98f), panelSprite);
            endStats = Txt("Result stats", statsPlate.transform, "", new Vector2(.5f, .5f), Vector2.zero, 17, Ice, 286);
            endStats.rectTransform.sizeDelta = new Vector2(286, 196); endStats.overflowMode = TextOverflowModes.Overflow; endStats.lineSpacing = 7;
            var restartImage = Img("Restart dispatch board", sail.transform, new Vector2(.5f, .13f), Vector2.zero, new Vector2(280, 78), Terracotta, tagSprite);
            restartRt = restartImage.rectTransform;
            endCta = Txt("Restart label", restartRt, "배차판 다시 열기", new Vector2(.5f, .5f), Vector2.zero, 20, Indigo, 260);
        }

        void BuildOverlays()
        {
            revealPlate = Img("Math reveal sail", uiRoot, new Vector2(.5f, .54f), Vector2.zero, new Vector2(360, 150), new Color(Indigo.r, Indigo.g, Indigo.b, .96f), panelSprite);
            revealRt = revealPlate.rectTransform; revealG = revealRt.gameObject.AddComponent<CanvasGroup>(); revealG.alpha = 0;
            revealText = Txt("Reveal text", revealRt, "", new Vector2(.5f, .5f), Vector2.zero, 18, Salt, 336);
            revealText.rectTransform.sizeDelta = new Vector2(336, 116); revealText.overflowMode = TextOverflowModes.Overflow;

            toastRt = Img("Refusal glint", uiRoot, new Vector2(.5f, .69f), Vector2.zero, new Vector2(356, 68), new Color(Night.r, Night.g, Night.b, .94f), panelSprite).rectTransform;
            toastG = toastRt.gameObject.AddComponent<CanvasGroup>(); toastG.alpha = 0;
            toastText = Txt("Refusal text", toastRt, "", new Vector2(.5f, .5f), Vector2.zero, 16, Salt, 336);

            guideImage = Img("Guide moon ring", uiRoot, new Vector2(.5f, .5f), Vector2.zero, new Vector2(72, 72), Brass, ringSprite);
            guideRt = guideImage.rectTransform; guideRt.gameObject.SetActive(false);
            rippleImage = Img("Touch ripple", uiRoot, new Vector2(.5f, .5f), Vector2.zero, new Vector2(70, 70), Ice, ringSprite);
            rippleRt = rippleImage.rectTransform; rippleRt.gameObject.SetActive(false);

            practiceHintPlate = Img("Practice equation card", gameRt, new Vector2(.5f, .665f), Vector2.zero, new Vector2(356, 58),
                new Color(Terracotta.r, Terracotta.g, Terracotta.b, .98f), tagSprite);
            practiceHintRt = practiceHintPlate.rectTransform;
            practiceHintText = Txt("Practice equation", practiceHintRt, "", new Vector2(.5f, .5f), Vector2.zero, 15, Salt, 334);
            practiceHintText.rectTransform.sizeDelta = new Vector2(334, 48); practiceHintText.overflowMode = TextOverflowModes.Overflow;
            practiceTrailRt = R("Practice pointer route", gameRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            practiceTrailRt.anchorMin = Vector2.zero; practiceTrailRt.anchorMax = Vector2.one; practiceTrailRt.offsetMin = practiceTrailRt.offsetMax = Vector2.zero;
            for (int i = 0; i < practiceTrailDots.Length; i++)
                practiceTrailDots[i] = Img("Practice moving pointer " + i, practiceTrailRt, new Vector2(.5f, .5f), Vector2.zero,
                    Vector2.one * (10f + i), new Color(Saffron.r, Saffron.g, Saffron.b, .9f), tagSprite).rectTransform;

            // Invalid title taps leave a persistent spatial instruction: dim
            // the irrelevant world and draw a dotted route from the tap to the
            // actual cover.  Text alone was not enough in first-play evidence.
            titleCoachRt = R("Title tap route", titleRt, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            titleCoachRt.anchorMin = Vector2.zero; titleCoachRt.anchorMax = Vector2.one; titleCoachRt.offsetMin = titleCoachRt.offsetMax = Vector2.zero;
            titleCoachG = titleCoachRt.gameObject.AddComponent<CanvasGroup>(); titleCoachG.alpha = 0;
            var veil = Img("Title focus veil", titleCoachRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(430, 920), new Color(2f / 255f, 10f / 255f, 22f / 255f, .34f), panelSprite);
            veil.rectTransform.anchorMin = Vector2.zero; veil.rectTransform.anchorMax = Vector2.one; veil.rectTransform.offsetMin = veil.rectTransform.offsetMax = Vector2.zero;
            for (int i = 0; i < titleCoachDots.Length; i++)
                titleCoachDots[i] = Img("Route chevron " + i, titleCoachRt, new Vector2(.5f, .5f), Vector2.zero,
                    Vector2.one * (12f + i * 1.4f), new Color(Brass.r, Brass.g, Brass.b, .94f), roundSprite).rectTransform;
            titleCoachTargetRt = Img("Cover target halo", titleCoachRt, new Vector2(.5f, .5f), Vector2.zero, new Vector2(238, 214), Moon, ringSprite).rectTransform;
            titleCoachArrowRt = Txt("Cover route arrow", titleCoachRt, "◇  여기", new Vector2(.5f, .5f), Vector2.zero, 22, Moon, 150).rectTransform;
            titleCoachRt.gameObject.SetActive(false);
        }

        void SetScreen()
        {
            bool title = phase == RunPhase.Title, end = phase == RunPhase.End;
            titleRt.gameObject.SetActive(title); gameRt.gameObject.SetActive(!title && !end); endRt.gameObject.SetActive(end);
            if (title) { titleG.alpha = 1; UpdateBestText(); }
            if (!title && !end) { gameG.alpha = 1; RefreshProblemUi(); RefreshHud(); }
            guideRt.gameObject.SetActive(!title && !end && (phase == RunPhase.Practice || phase == RunPhase.Playing));
            if (practiceHintRt) practiceHintRt.gameObject.SetActive(phase == RunPhase.Practice);
            if (practiceTrailRt) practiceTrailRt.gameObject.SetActive(phase == RunPhase.Practice);
        }

        void UpdateBestText()
        {
            int best = PlayerPrefs.GetInt("dice-caravan-best-first", 0);
            titleMeta.text = "중2 · 경우의 수   ◇   최고 첫 시도 " + best + "/6";
        }

        void RefreshProblemUi()
        {
            if (current == null || !gameRt || !gameRt.gameObject.activeSelf) return;
            promptText.text = WrapPrompt(current.prompt);
            bandText.text = phase == RunPhase.Practice ? "별지도 연습 · 시간과 바람등이 멈춰 있다" :
                st.solved < 2 ? "1항로 · 한 사건과 합의 법칙" : st.solved < 4 ? "2항로 · 곱과 순서쌍" : "3항로 · 0 카드와 대표 뽑기";
            activePins = current.answerSet.Length;
            for (int i = 0; i < 36; i++)
            {
                bool on = i < activePins && current.cellLabels[i] != "·";
                pinRt[i].gameObject.SetActive(on);
                if (on) pinTexts[i].text = current.cellLabels[i];
            }
            LayoutBoard(); RefreshAllPins(false); RebuildThreads(); UpdatePracticeHint();
        }

        void LayoutBoard()
        {
            if (current == null) return;
            float area = layoutWide ? 360f : 320f;
            float cell = Mathf.Min(layoutWide ? 58f : 52f, Mathf.Min(area / current.cols, area / current.rows));
            float gap = cell * .06f;
            float totalW = current.cols * cell + (current.cols - 1) * gap;
            float totalH = current.rows * cell + (current.rows - 1) * gap;
            for (int y = 0; y < current.rows; y++) for (int x = 0; x < current.cols; x++)
            {
                int i = y * current.cols + x;
                pinRt[i].anchoredPosition = new Vector2(-totalW * .5f + cell * .5f + x * (cell + gap), totalH * .5f - cell * .5f - y * (cell + gap));
                pinRt[i].sizeDelta = Vector2.one * (cell * .91f);
                pinTexts[i].fontSize = current.cols >= 6 ? 12 : 15;
            }
        }

        static string WrapPrompt(string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            var words = source.Split(' ');
            var sb = new System.Text.StringBuilder(source.Length + 4);
            int line = 0;
            for (int i = 0; i < words.Length; i++)
            {
                int next = words[i].Length + (line > 0 ? 1 : 0);
                if (line > 0 && line + next > 20) { sb.Append('\n'); line = 0; }
                else if (line > 0) { sb.Append(' '); line++; }
                sb.Append(words[i]); line += words[i].Length;
            }
            return sb.ToString();
        }

        void RefreshAllPins(bool feedback)
        {
            if (current == null) return;
            for (int i = 0; i < activePins; i++) SetPinVisual(i, feedback);
            selectedText.text = "핀 " + st.selectedPins + "개";
        }

        void SetPinVisual(int i, bool feedback)
        {
            if (i < 0 || i >= activePins || !pinImages[i]) return;
            Color c = selected[i] ? new Color(Ice.r, Ice.g, Ice.b, 1f) : new Color(Salt.r, Salt.g, Salt.b, .88f);
            Color tc = Indigo;
            if (feedbackExtra[i]) { c = Error; tc = Color.white; }
            if (feedbackMissing[i]) { c = Brass; tc = Night; }
            pinImages[i].color = c; pinTexts[i].color = tc;
            pinRt[i].localScale = selected[i] ? Vector3.one * 1.04f : Vector3.one;
            if (phase == RunPhase.Practice && current.kind == BoardKind.DiceGrid && i < current.cellLabels.Length)
            {
                int a = i / current.cols + 1, b = i % current.cols + 1;
                pinTexts[i].text = selected[i] ? a + "+" + b + "=" + (a + b) : current.cellLabels[i];
                pinTexts[i].fontSize = selected[i] ? 10.5f : 12f;
            }
        }

        void RebuildThreads()
        {
            int line = 0, previous = -1;
            for (int i = 0; i < activePins; i++)
            {
                if (!selected[i]) continue;
                if (previous >= 0 && line < threadRt.Length)
                {
                    Vector2 a = pinRt[previous].anchoredPosition, b = pinRt[i].anchoredPosition;
                    Vector2 d = b - a; threadRt[line].anchoredPosition = (a + b) * .5f;
                    threadRt[line].sizeDelta = new Vector2(4f, d.magnitude);
                    threadRt[line].localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg);
                    threadRt[line].gameObject.SetActive(true); line++;
                }
                previous = i;
            }
            for (; line < threadRt.Length; line++) threadRt[line].gameObject.SetActive(false);
        }

        void RefreshHud()
        {
            livesText.text = st.lives == 3 ? "◆ ◆ ◆" : st.lives == 2 ? "◆ ◆ ◇" : st.lives == 1 ? "◆ ◇ ◇" : "◇ ◇ ◇";
            progressText.text = "항로 " + st.solved + "/6";
            selectedText.text = "핀 " + st.selectedPins + "개" + (st.windKnot > 0 ? " · 순풍 매듭" : "");
        }

        void ResetWorldForRun()
        {
            caravanTravel = caravanTarget = 0f; sailPulse = wrongPulse = cameraPulse = 0f;
            caravanBranch = caravanBranchTarget = 0f; routeEvent = lumaRun = 0f;
            if (caravanRoot) caravanRoot.position = caravanBase;
            if (routeGateRoot) routeGateRoot.position = caravanBase + new Vector3(3.2f, 0, 0);
            for (int i = 0; i < 3; i++) if (lampGlass[i]) lampGlass[i].sharedMaterial = lampOnMat;
            for (int i = 0; i < routeGateLights.Length; i++) if (routeGateLights[i]) routeGateLights[i].sharedMaterial = lampOffMat;
            if (routeGateArm) routeGateArm.localRotation = Quaternion.Euler(0, 0, -4f);
        }

        void ResetWorldForProblem()
        {
            wrongPulse = 0f; sailPulse = .12f; routeEvent = 0f; lumaRun = 0f; RefreshHud();
            for (int i = 0; i < routeGateLights.Length; i++) if (routeGateLights[i]) routeGateLights[i].sharedMaterial = lampOffMat;
            if (routeGateArm) routeGateArm.localRotation = Quaternion.Euler(0, 0, -4f);
        }

        void CorrectRouteEvent(int pinCount, int combo)
        {
            sailPulse = 1f; cameraPulse = 1f; routeEvent = 1f; lumaRun = 1f;
            caravanTarget += 2.25f + Mathf.Min(.75f, combo * .10f);
            caravanBranchTarget = (st.solved % 2 == 0 ? -.72f : .72f);
            if (routeGateRoot) routeGateRoot.position = caravanRoot.position + new Vector3(2.7f, 0, 0);
            MgfFx.Glow(routeGateRoot ? routeGateRoot.position + Vector3.up * 2.75f : caravanRoot.position + Vector3.up * 2.75f,
                Saffron, 10 + pinCount * 2, .34f + combo * .03f);
            if (routeGateArm) MgfFx.Punch(routeGateArm, .16f, .44f);
            if (lumaWorld) MgfFx.Punch(lumaWorld, .30f, .48f);
        }

        void WrongLeakEvent()
        {
            wrongPulse = 1f;
            for (int i = 0; i < 3; i++) if (lampGlass[i]) lampGlass[i].sharedMaterial = i < st.lives ? lampOnMat : lampOffMat;
            if (lumaWorld) MgfFx.Punch(lumaWorld, -.16f, .34f);
        }

        void UpdateWorld(float dt)
        {
            worldTime += dt;
            caravanTravel = Mathf.SmoothStep(caravanTravel, caravanTarget, 1f - Mathf.Exp(-dt * 2.3f));
            caravanBranch = Mathf.Lerp(caravanBranch, caravanBranchTarget, 1f - Mathf.Exp(-dt * 2.2f));
            if (caravanRoot) caravanRoot.position = caravanBase + new Vector3(caravanTravel, 0, caravanBranch + Mathf.Sin(worldTime * .3f) * .08f);
            for (int i = 0; i < saltFlows.Length; i++) if (saltFlows[i])
            {
                Vector3 p = saltFlows[i].localPosition;
                p.x -= dt * (.28f + (i % 5) * .055f);
                if (p.x < -18f) p.x += 36f;
                p.y = saltFlowStart[i].y + Mathf.Sin(worldTime * .55f + i) * .012f;
                saltFlows[i].localPosition = p;
            }
            for (int i = 0; i < wheels.Length; i++) if (wheels[i]) wheels[i].Rotate(Vector3.up, (8f + Mathf.Abs(caravanTarget - caravanTravel) * 100f) * dt, Space.Self);
            for (int i = 0; i < sails.Length; i++) if (sails[i])
            {
                float wave = Mathf.Sin(worldTime * 1.7f + i * .8f) * 1.8f;
                float leak = wrongPulse > 0 ? Mathf.Sin(worldTime * 18f + i) * 7f * wrongPulse : 0f;
                sails[i].localScale = sailBaseScale[i] * .58f;
                sails[i].localRotation = Quaternion.Euler(0, i >= 3 ? 180 : 0, (i >= 3 ? 7f : -7f) + wave + leak);
            }
            if (lumaWorld)
            {
                lumaRun = Mathf.Max(0f, lumaRun - dt * .58f);
                float runPhase = routeEvent > 0f ? 1f - lumaRun : 0f;
                lumaWorld.localPosition = new Vector3(-.9f + runPhase * 2.4f, 1.58f + Mathf.Sin(worldTime * (routeEvent > 0f ? 11f : 2.3f)) * (routeEvent > 0f ? .14f : .07f), -.98f);
                lumaWorld.localRotation = Quaternion.Euler(0, routeEvent > 0f ? -24f : Mathf.Sin(worldTime * .9f) * 9f, wrongPulse > 0 ? -12f * wrongPulse : 0);
            }
            if (routeEvent > 0f)
            {
                routeEvent = Mathf.Max(0f, routeEvent - dt * .48f);
                float gateProgress = Mathf.SmoothStep(0f, 1f, 1f - routeEvent);
                int lit = Mathf.Clamp(Mathf.FloorToInt(gateProgress * 6f), 0, routeGateLights.Length);
                for (int i = 0; i < routeGateLights.Length; i++) if (routeGateLights[i]) routeGateLights[i].sharedMaterial = i < lit ? lampOnMat : lampOffMat;
                if (routeGateArm) routeGateArm.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-4f, 68f, gateProgress));
            }
            for (int i = 0; i < marketFigures.Length; i++) if (marketFigures[i])
            {
                Vector3 p = marketFigureBase[i];
                p.x += Mathf.Sin(worldTime * (.38f + i * .017f) + i) * .20f;
                p.y += Mathf.Abs(Mathf.Sin(worldTime * (1.2f + i * .03f) + i)) * .055f;
                marketFigures[i].localPosition = p;
                marketFigures[i].localRotation = Quaternion.Euler(0, Mathf.Sin(worldTime * .7f + i) * 22f, 0);
            }
            sailPulse = Mathf.Max(0, sailPulse - dt * 1.4f); wrongPulse = Mathf.Max(0, wrongPulse - dt * 1.55f); cameraPulse = Mathf.Max(0, cameraPulse - dt * 2f);
            if (cam)
            {
                MgfLook.FitWidth(cam, 40f - cameraPulse * 2.4f, .62f);
                Vector3 target = layoutWide ? new Vector3(-1.8f + caravanTravel * .18f, .8f, .8f) : new Vector3(caravanTravel * .12f, 1f, 1.1f);
                cam.transform.LookAt(target);
            }
        }

        void UpdateUi(float dt)
        {
            bool wide = Screen.width >= 900 && Screen.width > Screen.height * 1.18f;
            if (wide != layoutWide) { layoutWide = wide; ApplyLayout(); }

            if (titleRt.gameObject.activeSelf)
            {
                titleCoverRt.localScale = Vector3.one * (1f + Mathf.Sin(worldTime * 2.1f) * .025f);
                titleLogo.color = Color.Lerp(Salt, Ice, .5f + .5f * Mathf.Sin(worldTime * 1.15f));
            }
            if (gameRt.gameObject.activeSelf)
            {
                int voyageLeft = Mathf.Max(0, 90 - Mathf.CeilToInt(runClock));
                scoreText.text = st.onboarding ? "전체 정지" : "전체 " + (voyageLeft / 60) + ":" + (voyageLeft % 60).ToString("00");
                bool voyageWarning = !st.onboarding && voyageLeft <= 15;
                scoreText.color = voyageWarning ? Error : Salt;
                scoreText.rectTransform.localScale = voyageWarning ? Vector3.one * (1f + .06f * (.5f + .5f * Mathf.Sin(worldTime * 7f))) : Vector3.one;
                if (phase == RunPhase.Playing) timerText.text = "문항 " + Mathf.Max(0, Mathf.CeilToInt(problemLimit - problemClock)) + "초";
                else timerText.text = st.onboarding ? "문항 정지" : "문항 판정";
                if (cordRt) cordRt.anchoredPosition = new Vector2(cordRt.anchoredPosition.x, -cordPull);
                if (lumaRt) lumaRt.localScale = Vector3.one * (1f + Mathf.Sin(worldTime * 2.8f) * .025f + sailPulse * .12f);
                UpdatePracticeTrail();
            }
            cordPull = Mathf.Lerp(cordPull, draggingCord ? cordPull : 0f, 1f - Mathf.Exp(-dt * 12f));
            UpdateGuide(dt); UpdateOverlays(dt);
        }

        void ApplyLayout()
        {
            // The kit's default 0.5 match makes the virtual height collapse on
            // ultra-wide screens, so fixed-size board/prompt rectangles overlap.
            // Wide layouts match the reference height instead: the left world
            // and right interaction zone remain independent at 1280..2000 px.
            if (canvasScaler) canvasScaler.matchWidthOrHeight = layoutWide ? 1f : .5f;
            if (layoutWide)
            {
                boardRt.anchorMin = boardRt.anchorMax = new Vector2(.76f, .43f); boardRt.sizeDelta = new Vector2(360, 360);
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(.76f, .79f); promptRt.sizeDelta = new Vector2(500, 136);
                promptText.rectTransform.sizeDelta = new Vector2(470, 106); goalText.rectTransform.sizeDelta = new Vector2(470, 34);
                cordRt.anchorMin = cordRt.anchorMax = new Vector2(.76f, .11f);
                selectedText.rectTransform.anchorMin = selectedText.rectTransform.anchorMax = new Vector2(.76f, .17f);
                lumaRt.anchorMin = lumaRt.anchorMax = new Vector2(.50f, .20f);
                practiceHintRt.anchorMin = practiceHintRt.anchorMax = new Vector2(.76f, .675f); practiceHintRt.sizeDelta = new Vector2(430, 54);
                cam.transform.position = new Vector3(9.8f, 8.6f, -14.8f);
            }
            else
            {
                boardRt.anchorMin = boardRt.anchorMax = new Vector2(.5f, .425f); boardRt.sizeDelta = new Vector2(346, 346);
                promptRt.anchorMin = promptRt.anchorMax = new Vector2(.5f, .795f); promptRt.sizeDelta = new Vector2(370, 146);
                promptText.rectTransform.sizeDelta = new Vector2(348, 104); goalText.rectTransform.sizeDelta = new Vector2(350, 34);
                cordRt.anchorMin = cordRt.anchorMax = new Vector2(.5f, .09f);
                selectedText.rectTransform.anchorMin = selectedText.rectTransform.anchorMax = new Vector2(.5f, .18f);
                lumaRt.anchorMin = lumaRt.anchorMax = new Vector2(.16f, .20f);
                practiceHintRt.anchorMin = practiceHintRt.anchorMax = new Vector2(.5f, .665f); practiceHintRt.sizeDelta = new Vector2(356, 58);
                cam.transform.position = new Vector3(9.6f, 9.8f, -14.6f);
            }
            LayoutBoard(); RebuildThreads();
        }

        void StartGuide() { guideBoost = 1f; PositionGuide(); }
        void BoostGuide() { guideBoost = 2f; PositionGuide(); MgfSfx.Play("pop", .14f); }
        void PointToBoard() { guideBoost = 2f; PositionGuide(true); }
        void PointToTitleCover(Vector2 tapScreen)
        {
            MgfFx.Punch(titleCoverRt, .15f, .36f);
            if (!titleCoachRt) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(titleRt, tapScreen, null, out Vector2 from);
            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(null, titleCoverRt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(titleRt, targetScreen, null, out Vector2 to);
            for (int i = 0; i < titleCoachDots.Length; i++)
            {
                float t = (i + 1f) / (titleCoachDots.Length + 1f);
                Vector2 bend = Vector2.up * Mathf.Sin(t * Mathf.PI) * 34f;
                titleCoachDots[i].anchoredPosition = Vector2.Lerp(from, to, t) + bend;
            }
            titleCoachTargetRt.anchoredPosition = to;
            titleCoachArrowRt.anchoredPosition = to + Vector2.up * 88f;
            titleCoachClock = 0f; titleCoachG.alpha = 1f; titleCoachRt.gameObject.SetActive(true);
        }
        void PulseRestart() { MgfFx.Punch(restartRt, .15f, .36f); }

        void PositionGuide(bool boardOnly = false)
        {
            if (!guideRt || !gameRt.gameObject.activeSelf) return;
            RectTransform target = boardRt;
            if (!boardOnly && phase == RunPhase.Practice)
            {
                int practiceTarget = NextPracticeGuidePin();
                target = practiceTarget >= 0 ? pinRt[practiceTarget] : cordRt;
            }
            else if (!boardOnly && st.selectedPins > 0) target = cordRt;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, target.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, screen, null, out Vector2 local);
            guideRt.anchoredPosition = local; guideRt.gameObject.SetActive(true);
        }

        void UpdatePracticeHint()
        {
            if (!practiceHintRt || phase != RunPhase.Practice || current == null)
            {
                if (practiceHintRt) practiceHintRt.gameObject.SetActive(false);
                if (practiceTrailRt) practiceTrailRt.gameObject.SetActive(false);
                return;
            }
            practiceHintRt.gameObject.SetActive(true); practiceTrailRt.gameObject.SetActive(true);
            int target = NextPracticeGuidePin();
            string message;
            bool removing = target >= 0 && selected[target] && !current.answerSet[target];
            if (target < 0) message = "⑤ 네 핀 완성: 손잡이를 아래로 당기기";
            else
            {
                int a = target / current.cols + 1, b = target % current.cols + 1;
                if (removing) message = a + "+" + b + "=" + (a + b) + " → 이 표식을 다시 눌러 빼기";
                else
                {
                    int[] order = { 3, 8, 13, 18 };
                    int step = 1;
                    for (int i = 0; i < order.Length; i++) if (order[i] == target) { step = i + 1; break; }
                    message = StepMark(step) + " (" + a + "," + b + "): " + a + "+" + b + "=5, 누르기";
                }
            }
            if (practiceHintCache != message)
            {
                practiceHintCache = message; practiceHintText.text = message;
                practiceHintPlate.color = removing ? new Color(Error.r, Error.g, Error.b, .99f) : new Color(Terracotta.r, Terracotta.g, Terracotta.b, .99f);
                MgfFx.Punch(practiceHintRt, .06f, .2f);
            }
        }

        static string StepMark(int step)
        {
            return step + "단계";
        }

        void UpdatePracticeTrail()
        {
            if (!practiceTrailRt || !practiceTrailRt.gameObject.activeSelf || phase != RunPhase.Practice) return;
            int targetPin = NextPracticeGuidePin();
            RectTransform target = targetPin >= 0 ? pinRt[targetPin] : cordRt;
            Vector2 fromScreen;
            Vector2 toScreen;
            if (targetPin >= 0)
            {
                fromScreen = RectTransformUtility.WorldToScreenPoint(null, practiceHintRt.position);
                toScreen = RectTransformUtility.WorldToScreenPoint(null, target.position);
            }
            else
            {
                fromScreen = RectTransformUtility.WorldToScreenPoint(null, cordRt.position);
                toScreen = fromScreen + Vector2.down * Mathf.Max(70f, Screen.height * .085f);
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(practiceTrailRt, fromScreen, null, out Vector2 from);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(practiceTrailRt, toScreen, null, out Vector2 to);
            float wave = Mathf.Repeat(worldTime, 1.2f) / 1.2f;
            for (int i = 0; i < practiceTrailDots.Length; i++)
            {
                float t = Mathf.Repeat(wave + i / (float)practiceTrailDots.Length, 1f);
                Vector2 bend = targetPin >= 0 ? Vector2.right * Mathf.Sin(t * Mathf.PI) * 13f : Vector2.zero;
                practiceTrailDots[i].anchoredPosition = Vector2.Lerp(from, to, t) + bend;
                practiceTrailDots[i].localScale = Vector3.one * Mathf.Lerp(.55f, 1.05f, t);
            }
        }

        void UpdateGuide(float dt)
        {
            if (!guideRt || !guideRt.gameObject.activeSelf) return;
            guideBoost = Mathf.Max(0, guideBoost - dt * .18f);
            float p = .5f + .5f * Mathf.Sin(worldTime * (guideBoost > 1f ? 5.5f : 3.2f));
            float size = guideBoost > 1f ? 72f : 58f;
            guideRt.sizeDelta = Vector2.one * (size + p * 14f);
            guideImage.color = new Color(Brass.r, Brass.g, Brass.b, .45f + p * .5f);
            PositionGuide();
        }

        void SpawnRipple(Vector2 screen)
        {
            if (!rippleRt) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, screen, null, out Vector2 local);
            rippleRt.anchoredPosition = local; rippleRt.localScale = Vector3.one * .45f;
            rippleImage.color = Ice; rippleRt.gameObject.SetActive(true); rippleClock = 0f;
        }

        void UpdateOverlays(float dt)
        {
            if (rippleClock < 1f)
            {
                rippleClock += dt * 2.7f; rippleRt.localScale = Vector3.one * Mathf.Lerp(.45f, 1.65f, rippleClock);
                rippleImage.color = new Color(Ice.r, Ice.g, Ice.b, 1f - rippleClock);
                if (rippleClock >= 1f) rippleRt.gameObject.SetActive(false);
            }
            if (toastClock < 1f)
            {
                toastClock += dt * 1.55f; toastG.alpha = Mathf.Sin(Mathf.Clamp01(toastClock) * Mathf.PI);
                float nudge = refuseClock < .28f ? Mathf.Sin(refuseClock * 55f) * (1f - refuseClock / .28f) * 8f : 0f;
                toastRt.anchoredPosition = new Vector2(nudge, 0); refuseClock += dt;
            }
            else toastG.alpha = 0;
            if (titleCoachRt && titleCoachRt.gameObject.activeSelf)
            {
                titleCoachClock += dt;
                titleCoachG.alpha = titleCoachClock < 4.2f ? 1f : Mathf.Clamp01(1f - (titleCoachClock - 4.2f) / .8f);
                float pulse = 1f + Mathf.Sin(worldTime * 5.5f) * .055f;
                titleCoachTargetRt.localScale = Vector3.one * pulse;
                if (titleCoachClock >= 5f) titleCoachRt.gameObject.SetActive(false);
            }
            if (revealG.alpha > 0) revealRt.localScale = Vector3.Lerp(revealRt.localScale, Vector3.one, 1f - Mathf.Exp(-dt * 12f));
        }

        void Refuse(string message)
        {
            toastText.text = message; toastClock = 0f; refuseClock = 0f; toastG.alpha = 1;
            MgfSfx.Play("wrong", .12f);
        }

        void ShowReveal(string message, bool correct)
        {
            revealText.text = (correct ? "◇ 배차 경로 개통 ◇\n" : "△ 잘못된 노선 표식 확인 △\n") + message;
            revealPlate.color = correct ? new Color(Indigo.r, Indigo.g, Indigo.b, .97f) : new Color(.34f, .23f, .28f, .97f);
            revealG.alpha = 1; revealRt.localScale = Vector3.one * .72f; revealRt.gameObject.SetActive(true);
        }
        void HideReveal() { revealG.alpha = 0; }

        void SetEndScreen(bool win, string reason)
        {
            SetScreen();
            endTitle.text = win ? "달길 완주" : "염호 정박";
            endTitle.color = win ? Ice : Salt;
            endStats.text = reason + "\n\n통과한 항로  " + st.solved + "/6\n첫 시도 정답  " + st.firstAttemptCorrect + "/" + st.firstAttemptTotal + "\n배차 점수  " + st.score.ToString("N0");
            if (win)
            {
                int best = Mathf.Max(PlayerPrefs.GetInt("dice-caravan-best-first", 0), st.firstAttemptCorrect);
                PlayerPrefs.SetInt("dice-caravan-best-first", best); PlayerPrefs.Save();
            }
            MgfFx.Punch(restartRt, .1f, .45f);
        }

        void PinPressed(int index, bool on)
        {
            MgfFx.Punch(pinRt[index], on ? .18f : -.08f, .22f);
            if (on && index < activePins)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, pinRt[index].position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot, screen, null, out Vector2 local);
                rippleRt.anchoredPosition = local; rippleClock = 0f; rippleRt.gameObject.SetActive(true);
            }
            UpdatePracticeHint(); PositionGuide();
        }

        int NextPracticeGuidePin()
        {
            if (current == null) return -1;
            // Remove an extra pin before asking for a missing one.
            for (int i = 0; i < current.answerSet.Length; i++)
                if (selected[i] && !current.answerSet[i]) return i;

            // The fixed sum-5 practice walks the same descending diagonal the
            // learner sees: (1,4),(2,3),(3,2),(4,1).  Only after all four are
            // present does the guide move to the submission cord.
            int[] practiceOrder = { 3, 8, 13, 18 };
            for (int i = 0; i < practiceOrder.Length; i++)
            {
                int pin = practiceOrder[i];
                if (pin < current.answerSet.Length && current.answerSet[pin] && !selected[pin]) return pin;
            }
            for (int i = 0; i < current.answerSet.Length; i++)
                if (current.answerSet[i] && !selected[i]) return i;
            return -1;
        }

        void AnticipateCord() { cordRt.localScale = new Vector3(.92f, 1.10f, 1f); MgfSfx.Play("tap", .16f); }
        void PullCord(float pixels) { cordPull = Mathf.Clamp(pixels / Mathf.Max(.5f, MgfText.Canvas.scaleFactor), 0f, 86f); cordRt.localScale = new Vector3(1f + cordPull / 700f, 1f - cordPull / 900f, 1f); }
        void ReleaseCord() { cordRt.localScale = Vector3.one; MgfFx.Punch(cordRt, .14f, .28f); MgfSfx.Play("whoosh", .22f); }
        void LooseCord() { cordRt.localRotation = Quaternion.Euler(0, 0, -9f); MgfFx.Punch(cordRt, -.1f, .3f); }

        bool HitTitleCover(Vector2 p)
        {
            if (!titleCoverRt || !RectTransformUtility.ScreenPointToLocalPointInRectangle(titleCoverRt, p, null, out Vector2 local)) return false;
            Rect hit = titleCoverRt.rect; hit.xMin -= 18f; hit.xMax += 18f; hit.yMin -= 14f; hit.yMax += 14f;
            return hit.Contains(local);
        }
        bool HitRestart(Vector2 p) { return restartRt && RectTransformUtility.RectangleContainsScreenPoint(restartRt, p, null); }
        bool HitCord(Vector2 p) { return cordRt && RectTransformUtility.RectangleContainsScreenPoint(cordRt, p, null); }
        int HitPin(Vector2 p)
        {
            for (int i = 0; i < activePins; i++) if (pinRt[i].gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(pinRt[i], p, null)) return i;
            return -1;
        }
    }
}
